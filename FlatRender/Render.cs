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

namespace FlatRender
{
    public class Render : IModuleRender
    {
        #region Info
        public string Name { get; } = "Flat";
        public string Description { get; } = "Flat renderer, requested by SquareWaveMidis for his channel";
        public bool Initialized { get; private set; } = false;
        public ImageSource PreviewImage { get; } = ModuleUtils.BitmapToImageSource(Properties.Resources.preview);
        public string LanguageDictName { get; } = "flat";
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
                    Label = "Left Key",
                    Min = 0,
                    Max = 255,
                    Value = 0,
                };

                [UIChild]
                public UINumber right = new UINumber()
                {
                    Label = "Right Key",
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
                Label = "Note Screen Time",
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
                Label = "Keyboard Height %",
                SliderMin = 0,
                SliderMax = 100,
                Min = 0,
                Max = 100,
                DecimalPoints = 2,
                Step = 1,
                Value = 16,
                SliderWidth = 200,
            };

            [UIChild]
            public UICheckbox sameWidthNotes = new UICheckbox()
            {
                Label = "Same Width Notes",
                IsChecked = true,
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

        public void Init(MidiPlayback file, RenderStatus status)
        {
            midi = file;

            disposer = new DisposeGroup();
            renderStatus = status;

            quadBuffer = disposer.Add(new BasicShapeBuffer(1024 * 64, ShapePresets.Quads));
            flatShader = disposer.Add(BasicShapeBuffer.GetBasicShader());

            ReloadTrackColors();

            Initialized = true;
        }

        public void Dispose()
        {
            if (!Initialized) return;
            midi = null;
            disposer.Dispose();
            Initialized = false;
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
                });

                int kbfirstNote = firstNote;
                int kblastNote = lastNote;
                if (keyboard.BlackKey[firstNote]) kbfirstNote--;
                if (keyboard.BlackKey[lastNote - 1]) kblastNote++;

                float pianoHeight = settings.kbHeight / 100;

                double notePosFactor = 1 / screenTime * (1 - pianoHeight);

                double renderCutoff = midiTime + screenTime;
                foreach (var n in midi.IterateNotes(renderCutoff).BlackNotesAbove(!sameWidth))
                {
                    if (n.start >= renderCutoff) break;
                    if (n.key < firstNote || n.key >= lastNote) continue;

                    if (n.start < midiTime)
                    {
                        keyboard.BlendNote(n.key, n.color);
                    }

                    float left = (float)keyboard.Notes[n.key].Left;
                    float right = (float)keyboard.Notes[n.key].Right;
                    float end = (float)(1 - (renderCutoff - n.end) * notePosFactor);
                    float start = (float)(1 - (renderCutoff - n.start) * notePosFactor);
                    if (!n.hasEnded)
                        end = 1;

                    quadBuffer.PushVertex(right, start, n.color.Left);
                    quadBuffer.PushVertex(right, end, n.color.Left);
                    quadBuffer.PushVertex(left, end, n.color.Right);
                    quadBuffer.PushVertex(left, start, n.color.Right);
                }

                for (int n = kbfirstNote; n < kblastNote; n++)
                {
                    if (keyboard.BlackKey[n]) continue;

                    float left = (float)(keyboard.Keys[n].Left);
                    float right = (float)(keyboard.Keys[n].Right);
                    var coll = keyboard.Colors[n].Left;
                    var colr = keyboard.Colors[n].Right;

                    quadBuffer.PushVertex(left, 0, coll);
                    quadBuffer.PushVertex(right, 0, coll);
                    quadBuffer.PushVertex(right, pianoHeight, colr);
                    quadBuffer.PushVertex(left, pianoHeight, colr);
                }
                for (int n = kbfirstNote; n < kblastNote; n++)
                {
                    if (!keyboard.BlackKey[n]) continue;

                    float left = (float)(keyboard.Keys[n].Left);
                    float right = (float)(keyboard.Keys[n].Right);
                    var coll = keyboard.Colors[n].Left;
                    var colr = keyboard.Colors[n].Right;
                    float keyBottom = (float)(pianoHeight / 10 * 3.7);

                    quadBuffer.PushVertex(left, keyBottom, coll);
                    quadBuffer.PushVertex(right, keyBottom, coll);
                    quadBuffer.PushVertex(right, pianoHeight, colr);
                    quadBuffer.PushVertex(left, pianoHeight, colr);
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
