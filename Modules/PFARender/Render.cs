using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using ZenithEngine;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using ZenithEngine.DXHelper;
using ZenithEngine.DXHelper.Presets;
using System.Runtime.InteropServices;
using ZenithEngine.ModuleUtil;
using ZenithEngine.Modules;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUI;
using SharpDX.Direct3D11;
using SharpDX;

namespace PFARender
{
    public class Render : PureModule
    {
        #region Info
        public override string Name => "PFA+";
        public override string Description => "A replica of PFA with some special extra features";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        public override string LanguageDictName { get; } = "pfa";
        #endregion

        #region UI
        class UI : UIDockWithPalettes
        {
            public class Keys : UIDock
            {
                public Keys() : base(Dock.Left) { }

                [UIChild]
                public UINumber left = new UINumber()
                {
                    Label = new DynamicResourceExtension("firstNote"),
                    Min = 0,
                    Max = 255,
                    Value = 0,
                };

                [UIChild]
                public UINumber right = new UINumber()
                {
                    Label = new DynamicResourceExtension("lastNote"),
                    Min = 1,
                    Max = 256,
                    Value = 128,
                };
            }

            [UIChild]
            public Keys keys = new Keys();

            [UIChild]
            public UINumberSlider noteScreenTime = new UINumberSlider()
            {
                Label = new DynamicResourceExtension("noteScreenTime"),
                SliderMin = 2,
                SliderMax = 4096,
                Min = 0.1,
                Max = 1000000,
                DecimalPoints = 2,
                Step = 1,
                Value = 400,
            };

            [UIChild]
            public UINumberSlider kbHeight = new UINumberSlider()
            {
                Label = new DynamicResourceExtension("pianoHeight"),
                SliderMin = 0,
                SliderMax = 2,
                Min = 0,
                Max = 2,
                DecimalPoints = 2,
                Step = 1,
                Value = 1,
                SliderWidth = 200,
            };

            [UIChild]
            public UICheckbox sameWidthNotes = new UICheckbox()
            {
                Label = new DynamicResourceExtension("sameWidthNotes"),
                IsChecked = false,
            };
        }
        #endregion

        UI settings = LoadUI(() => new UI());

        public override FrameworkElement SettingsControl => settings;

        public override double StartOffset => settings.noteScreenTime.Value;

        protected override NoteColorPalettePick PalettePicker => settings.Palette;

        Flat2dShapeBuffer quadBuffer;
        ShaderProgram flatShader;
        ThreadedKeysLoop<Vert2D> multithread;

        public Render()
        {
            settings.Palette.PaletteChanged += ReloadTrackColors;

            quadBuffer = init.Add(new Flat2dShapeBuffer(1024 * 64));
            flatShader = init.Add(Shaders.BasicFlat());
            multithread = init.Add(new ThreadedKeysLoop<Vert2D>(1 << 12));
        }

        Color4 MultCol(Color4 col, float fac)
        {
            col.Red *= fac;
            col.Green *= fac;
            col.Blue *= fac;
            return col;
        }

        Color4 AddCol(Color4 col, float fac)
        {
            col.Red += fac;
            col.Green += fac;
            col.Blue += fac;
            return col;
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            double screenTime = settings.noteScreenTime;

            Midi.CheckParseDistance(screenTime);

            using (flatShader.UseOn(context))
            {
                var midiTime = Midi.PlayerPosition;
                int firstNote = settings.keys.left;
                int lastNote = settings.keys.right;
                bool sameWidth = settings.sameWidthNotes;

                var keyboard = new KeyboardState(firstNote, lastNote, new KeyboardParams()
                {
                    SameWidthNotes = sameWidth,
                    BlackKey2setOffset = 0.4,
                    BlackKey3setOffset = 0.4,
                    BlackKeyScale = 0.64f
                });

                int kbfirstNote = firstNote;
                int kblastNote = lastNote;
                if (keyboard.BlackKey[firstNote]) kbfirstNote--;
                if (keyboard.BlackKey[lastNote - 1]) kblastNote++;

                float pianoHeight = 0.151f * settings.kbHeight;

                pianoHeight = pianoHeight / (settings.keys.right - settings.keys.left) * 128;
                pianoHeight = pianoHeight / (1920.0f / 1080.0f) * Status.AspectRatio;

                double notePosFactor = 1 / screenTime * (1 - pianoHeight);

                float paddingx = 0.001f * 1;
                float paddingy = paddingx * Status.OutputWidth / Status.OutputHeight;

                double renderCutoff = midiTime + screenTime;

                var keyed = Midi.IterateNotesKeyed(midiTime, renderCutoff);
                multithread.Render(context, firstNote, lastNote, !sameWidth, (key, push) =>
                {
                    foreach (var n in keyed[key])
                    {
                        void pushQuad(float left, float top, float right, float bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft)
                        {
                            push(new Vert2D(left, top, topLeft));
                            push(new Vert2D(right, top, topRight));
                            push(new Vert2D(right, bottom, bottomRight));
                            push(new Vert2D(left, bottom, bottomLeft));
                        }
                        
                        if (n.Start < midiTime)
                        {
                            keyboard.BlendNote(n.Key, n.Color);
                            keyboard.PressKey(n.Key);
                        }

                        float left = (float)keyboard.Notes[key].Left;
                        float right = (float)keyboard.Notes[key].Right;
                        float end = (float)(1 - (renderCutoff - n.End) * notePosFactor);
                        float start = (float)(1 - (renderCutoff - n.Start) * notePosFactor);
                        if (!n.HasEnded)
                            end = 1.2f;

                        var leftCol = MultCol(n.Color.Left, 0.2f);
                        var rightCol = MultCol(n.Color.Right, 0.2f);
                        pushQuad(left, end, right, start, leftCol, rightCol, rightCol, leftCol);

                        if (end - start > paddingy * 2)
                        {
                            end -= paddingy;
                            start += paddingy;
                            right -= paddingx;
                            left += paddingx;

                            leftCol = MultCol(n.Color.Left, 0.5f);
                            rightCol = n.Color.Right;
                            pushQuad(left, end, right, start, leftCol, rightCol, rightCol, leftCol);
                        }
                    }
                });

                float topRedStart = pianoHeight * .99f;
                float topRedEnd = pianoHeight * .94f;
                float topBarEnd = pianoHeight * .927f;

                float wEndUpT = pianoHeight * 0.03f + pianoHeight * 0.020f;
                float wEndUpB = pianoHeight * 0.03f + pianoHeight * 0.005f;
                float wEndDownT = pianoHeight * 0.01f;
                float bKeyEnd = pianoHeight * .345f;
                float bKeyDownT = topBarEnd + pianoHeight * 0.015f;
                float bKeyDownB = bKeyEnd + pianoHeight * 0.015f;
                float bKeyUpT = topBarEnd + pianoHeight * 0.045f;
                float bKeyUpB = bKeyEnd + pianoHeight * 0.045f;

                float bKeyUSplitLT = pianoHeight * 0.78f;
                float bKeyUSplitRT = pianoHeight * 0.71f;
                float bKeyUSplitLB = pianoHeight * 0.65f;
                float bKeyUSplitRB = pianoHeight * 0.58f;

                float sepwdth = (float)Math.Round(keyboard.WhiteKeyWidth * Status.OutputWidth / 20);
                if (sepwdth == 0) sepwdth = 1;

                Color4 col1;
                Color4 col2;
                Color4 col3;
                Color4 col4;

                col1 = new Color4(.086f, .086f, .086f, 1);
                col2 = new Color4(.0196f, .0196f, .0196f, 1);
                quadBuffer.PushQuad(0, pianoHeight, 1, topRedStart, col2, col2, col1, col1);

                float topBarR = .585f;
                float topBarG = .0392f;
                float topBarB = .0249f;

                col1 = new Color4(topBarR, topBarG, topBarB, 1);
                col2 = new Color4(topBarR / 2, topBarG / 2, topBarB / 2, 1);
                quadBuffer.PushQuad(0, topRedStart, 1, topRedEnd, col2, col2, col1, col1);

                quadBuffer.PushQuad(0, topRedEnd, 1, topBarEnd, new Color4(.239f, .239f, .239f, 1));

                for (int i = kbfirstNote; i < kblastNote; i++)
                {
                    if (!keyboard.BlackKey[i])
                    {
                        float left = (float)keyboard.Keys[i].Left;
                        float right = (float)keyboard.Keys[i].Right;

                        Color4 leftCol = new Color4(255, 255, 255, 255).BlendWith(keyboard.Colors[i].Left);
                        Color4 rightCol = new Color4(255, 255, 255, 255).BlendWith(keyboard.Colors[i].Right);

                        if (keyboard.Pressed[i])
                        {
                            col1 = MultCol(rightCol, 0.5f);
                            quadBuffer.PushQuad(left, topBarEnd, right, wEndDownT, col1, col1, leftCol, leftCol);

                            col1 = MultCol(leftCol, 0.6f);
                            quadBuffer.PushQuad(left, wEndDownT, right, 0, col1, col1, col1, col1);
                        }
                        else
                        {
                            col1 = MultCol(rightCol, 0.8f);
                            quadBuffer.PushQuad(left, topBarEnd, right, wEndUpT, col1, col1, rightCol, rightCol);

                            col1 = MultCol(leftCol, .529f);
                            col2 = MultCol(leftCol, .329f);
                            quadBuffer.PushQuad(left, wEndUpT, right, wEndUpB, col2, col2, col1, col1);

                            col1 = MultCol(leftCol, .615f);
                            col2 = MultCol(leftCol, .729f);
                            quadBuffer.PushQuad(left, wEndUpB, right, 0, col2, col2, col1, col1);
                        }

                        var scleft = (float)Math.Floor(left * Status.OutputWidth - sepwdth / 2);
                        var scright = (float)Math.Floor(left * Status.OutputWidth + sepwdth / 2);
                        if (scleft == scright) scright++;
                        scleft /= Status.OutputWidth;
                        scright /= Status.OutputWidth;


                        col1 = new Color4(.0431f, .0431f, .0431f, 1);
                        col2 = new Color4(.556f, .556f, .556f, 1);
                        quadBuffer.PushQuad(scleft, topBarEnd, scright, 0, col1, col2, col2, col1);
                    }
                }

                quadBuffer.UseContext(context);

                for (int i = kbfirstNote; i < kblastNote; i++)
                {
                    if (keyboard.BlackKey[i])
                    {
                        float left = (float)keyboard.Keys[i].Left;
                        float right = (float)keyboard.Keys[i].Right;

                        float ileft = left + (float)keyboard.BlackKeyWidth / 8;
                        float iright = right - (float)keyboard.BlackKeyWidth / 8;

                        Color4 leftCol = new Color4(0, 0, 0, 255).BlendWith(keyboard.Colors[i].Left);
                        Color4 rightCol = new Color4(0, 0, 0, 255).BlendWith(keyboard.Colors[i].Right);

                        Color4 middleCol = new Color4(
                            (leftCol.Red + rightCol.Red) / 2,
                            (leftCol.Green + rightCol.Green) / 2,
                            (leftCol.Blue + rightCol.Blue) / 2,
                            (leftCol.Alpha + rightCol.Alpha) / 2
                            );

                        if (!keyboard.Pressed[i])
                        {
                            col1 = AddCol(rightCol, 0.25f);
                            col2 = AddCol(leftCol, 0.15f);
                            col3 = AddCol(leftCol, 0.0f);
                            col4 = AddCol(leftCol, 0.3f);

                            quadBuffer.Push(ileft, bKeyUSplitLT, col1);
                            quadBuffer.Push(iright, bKeyUSplitRT, col1);
                            quadBuffer.Push(iright, bKeyUpT, col2);
                            quadBuffer.Push(ileft, bKeyUpT, col2);

                            quadBuffer.Push(ileft, bKeyUSplitLB, col3);
                            quadBuffer.Push(iright, bKeyUSplitRB, col3);
                            quadBuffer.Push(iright, bKeyUSplitRT, col1);
                            quadBuffer.Push(ileft, bKeyUSplitLT, col1);

                            quadBuffer.Push(ileft, bKeyUpB, col3);
                            quadBuffer.Push(iright, bKeyUpB, col3);
                            quadBuffer.Push(iright, bKeyUSplitRB, col3);
                            quadBuffer.Push(ileft, bKeyUSplitLB, col3);

                            quadBuffer.Push(left, bKeyEnd, col3);
                            quadBuffer.Push(ileft, bKeyUpB, col4);
                            quadBuffer.Push(ileft, bKeyUpT, col4);
                            quadBuffer.Push(left, topBarEnd, col3);

                            quadBuffer.Push(right, bKeyEnd, col3);
                            quadBuffer.Push(iright, bKeyUpB, col4);
                            quadBuffer.Push(iright, bKeyUpT, col4);
                            quadBuffer.Push(right, topBarEnd, col3);

                            quadBuffer.Push(left, bKeyEnd, col3);
                            quadBuffer.Push(right, bKeyEnd, col3);
                            quadBuffer.Push(iright, bKeyUpB, col4);
                            quadBuffer.Push(ileft, bKeyUpB, col4);
                        }
                        else
                        {
                            col1 = MultCol(middleCol, 0.85f);
                            col2 = MultCol(rightCol, 0.85f);
                            col3 = MultCol(middleCol, 0.7f);
                            col4 = MultCol(leftCol, 0.7f);

                            quadBuffer.Push(ileft, bKeyUSplitLT, col1);
                            quadBuffer.Push(iright, bKeyUSplitRT, col1);
                            quadBuffer.Push(iright, bKeyDownT, col2);
                            quadBuffer.Push(ileft, bKeyDownT, col2);

                            quadBuffer.Push(ileft, bKeyUSplitLB, col3);
                            quadBuffer.Push(iright, bKeyUSplitRB, col3);
                            quadBuffer.Push(iright, bKeyUSplitRT, col1);
                            quadBuffer.Push(ileft, bKeyUSplitLT, col1);

                            quadBuffer.Push(ileft, bKeyDownB, col4);
                            quadBuffer.Push(iright, bKeyDownB, col4);
                            quadBuffer.Push(iright, bKeyUSplitRB, col3);
                            quadBuffer.Push(ileft, bKeyUSplitLB, col3);

                            col1 = MultCol(leftCol, 0.7f);
                            col2 = MultCol(rightCol, 0.7f);

                            quadBuffer.Push(left, bKeyEnd, col1);
                            quadBuffer.Push(ileft, bKeyDownB, leftCol);
                            quadBuffer.Push(ileft, bKeyDownT, rightCol);
                            quadBuffer.Push(left, topBarEnd, col2);

                            quadBuffer.Push(right, bKeyEnd, col1);
                            quadBuffer.Push(iright, bKeyDownB, leftCol);
                            quadBuffer.Push(iright, bKeyDownT, rightCol);
                            quadBuffer.Push(right, topBarEnd, col2);

                            quadBuffer.Push(left, bKeyEnd, col1);
                            quadBuffer.Push(right, bKeyEnd, col1);
                            quadBuffer.Push(iright, bKeyDownB, leftCol);
                            quadBuffer.Push(ileft, bKeyDownB, leftCol);
                        }
                    }
                }

                quadBuffer.Flush();
            }
        }
    }
}
