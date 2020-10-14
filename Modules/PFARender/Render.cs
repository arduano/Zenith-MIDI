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
using ZenithEngine.UI;

namespace PFARender
{
    public class Render : PureModule
    {
        #region Info
        public override string Name => "PFA+";
        public override string Description => "A replica of PFA with some special extra features";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        #endregion

        #region UI
        class UI : UIDockWithPalettes
        {
            public class Keys : UIDock
            {
                public Keys() : base(Dock.Left) { }

                [UIChild]
                public UINumber left = new UINumber("leftKey", new LangText("mods.common.firstNote"), 0, 0, 254);

                [UIChild]
                public UINumber right = new UINumber("rightKey", new LangText("mods.common.lastNote"), 127, 1, 255);
            }

            [UIChild]
            public Keys keys = new Keys() { Margin = new Thickness(0) };

            [UIChild]
            public UINumberSlider noteScreenTime = new UINumberSlider(
                "noteScreenTime",
                new LangText("mods.common.noteScreenTime"),
                1000, 1, 4000, 0.1m, 1000000, true
            )
            { SliderWidth = 700 };

            [UIChild]
            public UINumberSlider kbHeight = new UINumberSlider(
                "keyboardHeight",
                new LangText("mods.common.pianoHeight"),
                1, 0, 4
            )
            { SliderWidth = 400 };

            [UIChild]
            public UICheckbox sameWidthNotes = new UICheckbox("sameWidthNotes", new LangText("mods.common.sameWidthNotes"), false);

            [UIChild]
            public UITextBox barColorHex = new UITextBox("barColorHex", new LangText("mods.pfa.barColorHex"), "950A06", 6, 75);

            [UIChild]
            public UICheckbox middleCSquare = new UICheckbox("middleCSquare", new LangText("mods.pfa.middleCSquare"), true);
        }
        #endregion

        UI settings = LoadUI(() => new UI());

        public override ISerializableContainer SettingsControl => settings;

        public override double StartOffset => settings.noteScreenTime.Value;

        protected override NoteColorPalettePick PalettePicker => settings.Palette;

        string lastBarColorText = "950A06";
        float topBarR = 149f / 255f;
        float topBarG = 10f / 255f;
        float topBarB = 6f / 255f;

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

            if (lastBarColorText != settings.barColorHex.Value && settings.barColorHex.Value.Length == 6)
            {
                lastBarColorText = settings.barColorHex.Value;
                try
                {
                    int col = int.Parse(lastBarColorText, System.Globalization.NumberStyles.HexNumber);
                    topBarR = ((col >> 16) & 0xFF) / 255.0f;
                    topBarG = ((col >> 8) & 0xFF) / 255.0f;
                    topBarB = ((col >> 0) & 0xFF) / 255.0f;
                }
                catch { }
            }

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

                float pianoHeight = 0.151f * (float)settings.kbHeight;

                pianoHeight = pianoHeight / (settings.keys.right - settings.keys.left) * 128;
                pianoHeight = pianoHeight / (1920.0f / 1080.0f) * Status.AspectRatio;

                double notePosFactor = 1 / screenTime * (1 - pianoHeight);

                float paddingx = 0.001f * 1;
                float paddingy = paddingx * Status.OutputWidth / Status.OutputHeight;

                double renderCutoff = midiTime + screenTime;

                var noteStreams = Midi.IterateNotesKeyed(midiTime, renderCutoff);
                multithread.Render(context, firstNote, lastNote, !sameWidth, (key, push) =>
                {
                    void pushQuad(float left, float top, float right, float bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft)
                    {
                        push(new Vert2D(left, top, topLeft));
                        push(new Vert2D(right, top, topRight));
                        push(new Vert2D(right, bottom, bottomRight));
                        push(new Vert2D(left, bottom, bottomLeft));
                    }

                    var minBottom = pianoHeight - 0.1f;

                    foreach (var n in noteStreams[key])
                    {
                        if (n.Start < midiTime)
                        {
                            keyboard.BlendNote(n.Key, n.Color);
                            keyboard.PressKey(n.Key);
                        }

                        float left = keyboard.Notes[key].Left;
                        float right = keyboard.Notes[key].Right;
                        float end = (float)(1 - (renderCutoff - n.End) * notePosFactor);
                        float start = (float)(1 - (renderCutoff - n.Start) * notePosFactor);
                        if (!n.HasEnded)
                            end = 1.1f;

                        end = Math.Min(end, 1.1f);
                        start = Math.Max(start, minBottom);

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

                col1 = new Color4(topBarR, topBarG, topBarB, 1);
                col2 = new Color4(topBarR / 2, topBarG / 2, topBarB / 2, 1);
                quadBuffer.PushQuad(0, topRedStart, 1, topRedEnd, col2, col2, col1, col1);

                quadBuffer.PushQuad(0, topRedEnd, 1, topBarEnd, new Color4(.239f, .239f, .239f, 1));

                foreach (var key in keyboard.IterateWhiteKeys())
                {
                    float left = key.Left;
                    float right = key.Right;

                    Color4 leftCol = key.Color.Left;
                    Color4 rightCol = key.Color.Right;

                    if (key.Pressed)
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

                    if (settings.middleCSquare.Value && key.Key == 60) // C4
                    {
                        // TODO (Khang): cleanup, this is one massive hack for accuracy
                        float rightSnap = (float)Math.Round(right * Status.RenderWidth);
                        float leftSnap = (float)Math.Round(left * Status.RenderWidth);
                        float gap = (float)Math.Round((rightSnap - leftSnap) * 0.25f);
                        float xPad = (rightSnap - leftSnap) * 0.5f;

                        float yLen = (float)Math.Round(gap * 2.0f * settings.kbHeight);
                        float yPos = 5.0f + (key.Pressed ? wEndDownT : wEndUpT) * Status.RenderHeight; // seems hardcoded to that in pfa too?

                        var col = MultCol(leftCol, (key.Pressed ? .5f : .8f));
                        quadBuffer.PushQuad((leftSnap + gap) / Status.RenderWidth, (yPos + yLen) / Status.RenderHeight,
                            (leftSnap + gap + xPad) / Status.RenderWidth, yPos / Status.RenderHeight, col, col, col, col);
                    }

                    col1 = new Color4(.0431f, .0431f, .0431f, 1);
                    col2 = new Color4(.556f, .556f, .556f, 1);
                    quadBuffer.PushQuad(scleft, topBarEnd, scright, 0, col1, col2, col2, col1);
                }

                quadBuffer.UseContext(context);

                foreach (var key in keyboard.IterateBlackKeys())
                {
                    float left = key.Left;
                    float right = key.Right;

                    Color4 leftCol = key.Color.Left;
                    Color4 rightCol = key.Color.Right;

                    float ileft = left + (float)keyboard.BlackKeyWidth / 8;
                    float iright = right - (float)keyboard.BlackKeyWidth / 8;

                    Color4 middleCol = new Color4(
                        (leftCol.Red + rightCol.Red) / 2,
                        (leftCol.Green + rightCol.Green) / 2,
                        (leftCol.Blue + rightCol.Blue) / 2,
                        (leftCol.Alpha + rightCol.Alpha) / 2
                        );

                    if (!key.Pressed)
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

                quadBuffer.Flush();
            }
        }
    }
}
