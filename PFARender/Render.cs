using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using ZenithEngine;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using ZenithEngine.GLEngine;
using System.Runtime.InteropServices;
using ZenithEngine.ModuleUtil;
using ZenithEngine.Modules;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUI;

namespace PFARender
{
    public class Render : IModuleRender
    {
        #region Info
        public string Name => "PFA+";
        public string Description => "A replica of PFA with some special extra features";
        public ImageSource PreviewImage { get; } = ModuleUtils.BitmapToImageSource(Properties.Resources.preview);
        public bool Initialized { get; private set; } = false;
        public string LanguageDictName { get; } = "pfa";
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

        RenderStatus renderStatus;

        UI settings = new UI();

        public FrameworkElement SettingsControl => settings;

        public double StartOffset => settings.noteScreenTime.Value;

        BasicShapeBuffer quadBuffer;
        ShaderProgram flatShader;

        MidiPlayback midi = null;

        DisposeGroup disposer;

        public Render()
        {
            settings.Palette.PaletteChanged += ReloadTrackColors;
        }

        public void Dispose()
        {
            if (!Initialized) return;
            midi = null;
            disposer.Dispose();
            Initialized = false;
        }

        public void Init(MidiPlayback midi, RenderStatus status)
        {
            this.midi = midi;

            disposer = new DisposeGroup();
            renderStatus = status;

            quadBuffer = disposer.Add(new BasicShapeBuffer(1024 * 64, ShapePresets.Quads));
            flatShader = disposer.Add(BasicShapeBuffer.GetBasicShader());

            ReloadTrackColors();

            Initialized = true;
        }

        Color4 MultCol(Color4 col, float fac)
        {
            col.R *= fac;
            col.G *= fac;
            col.B *= fac;
            return col;
        }

        Color4 AddCol(Color4 col, float fac)
        {
            col.R += fac;
            col.G += fac;
            col.B += fac;
            return col;
        }

        public void RenderFrame(RenderSurface renderSurface)
        {
            double screenTime = settings.noteScreenTime;

            midi.CheckParseDistance(screenTime);

            using (new GLEnabler().Enable(EnableCap.Blend))
            {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                renderSurface.BindSurfaceAndClear();

                flatShader.Bind();

                var midiTime = midi.PlayerPosition;
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
                pianoHeight = pianoHeight / (1920.0f / 1080.0f) * renderStatus.AspectRatio;

                double notePosFactor = 1 / screenTime * (1 - pianoHeight);

                float paddingx = 0.001f * 1;
                float paddingy = paddingx * renderStatus.OutputWidth / renderStatus.OutputHeight;

                double renderCutoff = midiTime + screenTime;
                foreach (var n in midi.IterateNotes(renderCutoff).BlackNotesAbove(!sameWidth))
                {
                    if (n.start >= renderCutoff) break;
                    if (n.key < firstNote || n.key >= lastNote) continue;

                    if (n.start < midiTime)
                    {
                        keyboard.BlendNote(n.key, n.color);
                        keyboard.PressKey(n.key);
                    }

                    float left = (float)keyboard.Notes[n.key].Left;
                    float right = (float)keyboard.Notes[n.key].Right;
                    float end = (float)(1 - (renderCutoff - n.end) * notePosFactor);
                    float start = (float)(1 - (renderCutoff - n.start) * notePosFactor);
                    if (!n.hasEnded)
                        end = 1.2f;

                    var leftCol = MultCol(n.color.Left, 0.2f);
                    var rightCol = MultCol(n.color.Right, 0.2f);
                    quadBuffer.PushQuad(left, end, right, start, leftCol, rightCol, rightCol, leftCol);

                    if (end - start > paddingy * 2)
                    {
                        end -= paddingy;
                        start += paddingy;
                        right -= paddingx;
                        left += paddingx;

                        leftCol = MultCol(n.color.Left, 0.5f);
                        rightCol = n.color.Right;
                        quadBuffer.PushQuad(left, end, right, start, leftCol, rightCol, rightCol, leftCol);
                    }
                }

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

                float sepwdth = (float)Math.Round(keyboard.WhiteKeyWidth * renderStatus.OutputWidth / 20);
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

                        var scleft = (float)Math.Floor(left * renderStatus.OutputWidth - sepwdth / 2);
                        var scright = (float)Math.Floor(left * renderStatus.OutputWidth + sepwdth / 2);
                        if (scleft == scright) scright++;
                        scleft /= renderStatus.OutputWidth;
                        scright /= renderStatus.OutputWidth;


                        col1 = new Color4(.0431f, .0431f, .0431f, 1);
                        col2 = new Color4(.556f, .556f, .556f, 1);
                        quadBuffer.PushQuad(scleft, topBarEnd, scright, 0, col1, col2, col2, col1);
                    }
                }

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
                            (leftCol.R + rightCol.R) / 2,
                            (leftCol.G + rightCol.G) / 2,
                            (leftCol.B + rightCol.B) / 2,
                            (leftCol.A + rightCol.A) / 2
                            );

                        if (!keyboard.Pressed[i])
                        {
                            col1 = AddCol(rightCol, 0.25f);
                            col2 = AddCol(leftCol, 0.15f);
                            col3 = AddCol(leftCol, 0.0f);
                            col4 = AddCol(leftCol, 0.3f);

                            quadBuffer.PushVertex(ileft, bKeyUSplitLT, col1);
                            quadBuffer.PushVertex(iright, bKeyUSplitRT, col1);
                            quadBuffer.PushVertex(iright, bKeyUpT, col2);
                            quadBuffer.PushVertex(ileft, bKeyUpT, col2);

                            quadBuffer.PushVertex(ileft, bKeyUSplitLB, col3);
                            quadBuffer.PushVertex(iright, bKeyUSplitRB, col3);
                            quadBuffer.PushVertex(iright, bKeyUSplitRT, col1);
                            quadBuffer.PushVertex(ileft, bKeyUSplitLT, col1);

                            quadBuffer.PushVertex(ileft, bKeyUpB, col3);
                            quadBuffer.PushVertex(iright, bKeyUpB, col3);
                            quadBuffer.PushVertex(iright, bKeyUSplitRB, col3);
                            quadBuffer.PushVertex(ileft, bKeyUSplitLB, col3);

                            quadBuffer.PushVertex(left, bKeyEnd, col3);
                            quadBuffer.PushVertex(ileft, bKeyUpB, col4);
                            quadBuffer.PushVertex(ileft, bKeyUpT, col4);
                            quadBuffer.PushVertex(left, topBarEnd, col3);

                            quadBuffer.PushVertex(right, bKeyEnd, col3);
                            quadBuffer.PushVertex(iright, bKeyUpB, col4);
                            quadBuffer.PushVertex(iright, bKeyUpT, col4);
                            quadBuffer.PushVertex(right, topBarEnd, col3);

                            quadBuffer.PushVertex(left, bKeyEnd, col3);
                            quadBuffer.PushVertex(right, bKeyEnd, col3);
                            quadBuffer.PushVertex(iright, bKeyUpB, col4);
                            quadBuffer.PushVertex(ileft, bKeyUpB, col4);
                        }
                        else
                        {
                            col1 = MultCol(middleCol, 0.85f);
                            col2 = MultCol(rightCol, 0.85f);
                            col3 = MultCol(middleCol, 0.7f);
                            col4 = MultCol(leftCol, 0.7f);

                            quadBuffer.PushVertex(ileft, bKeyUSplitLT, col1);
                            quadBuffer.PushVertex(iright, bKeyUSplitRT, col1);
                            quadBuffer.PushVertex(iright, bKeyDownT, col2);
                            quadBuffer.PushVertex(ileft, bKeyDownT, col2);

                            quadBuffer.PushVertex(ileft, bKeyUSplitLB, col3);
                            quadBuffer.PushVertex(iright, bKeyUSplitRB, col3);
                            quadBuffer.PushVertex(iright, bKeyUSplitRT, col1);
                            quadBuffer.PushVertex(ileft, bKeyUSplitLT, col1);

                            quadBuffer.PushVertex(ileft, bKeyDownB, col4);
                            quadBuffer.PushVertex(iright, bKeyDownB, col4);
                            quadBuffer.PushVertex(iright, bKeyUSplitRB, col3);
                            quadBuffer.PushVertex(ileft, bKeyUSplitLB, col3);

                            col1 = MultCol(leftCol, 0.7f);
                            col2 = MultCol(rightCol, 0.7f);

                            quadBuffer.PushVertex(left, bKeyEnd, col1);
                            quadBuffer.PushVertex(ileft, bKeyDownB, leftCol);
                            quadBuffer.PushVertex(ileft, bKeyDownT, rightCol);
                            quadBuffer.PushVertex(left, topBarEnd, col2);

                            quadBuffer.PushVertex(right, bKeyEnd, col1);
                            quadBuffer.PushVertex(iright, bKeyDownB, leftCol);
                            quadBuffer.PushVertex(iright, bKeyDownT, rightCol);
                            quadBuffer.PushVertex(right, topBarEnd, col2);

                            quadBuffer.PushVertex(left, bKeyEnd, col1);
                            quadBuffer.PushVertex(right, bKeyEnd, col1);
                            quadBuffer.PushVertex(iright, bKeyDownB, leftCol);
                            quadBuffer.PushVertex(ileft, bKeyDownB, leftCol);
                        }
                    }
                }

                quadBuffer.Flush();
            }
        }

        public void ReloadTrackColors()
        {
            var cols = settings.Palette.GetColors(midi.TrackCount);
            midi.ApplyColors(cols);
        }
    }
}
