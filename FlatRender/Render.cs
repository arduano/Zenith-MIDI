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

namespace FlatRender
{
    public class Render : IModuleRender
    {
        #region Info
        public string Name { get; } = "Flat";
        public string Description { get; } = "Flat renderer, requested by SquareWaveMidis for his channel";
        public bool Initialized { get; private set; } = false;
        public ImageSource PreviewImage { get; private set; }
        public string LanguageDictName { get; } = "flat";
        #endregion

        RenderStatus renderStatus;
        Settings settings;

        SettingsCtrl settingsControl;

        public Control SettingsControl { get { return settingsControl; } }
        
        public double StartOffset => settings.deltaTimeOnScreen;

        BasicShapeBuffer quadBuffer;
        ShaderProgram flatShader;

        MidiPlayback midi = null;

        DisposeGroup disposer;

        public Render()
        {
            this.settings = new Settings();
            PreviewImage = PluginUtils.BitmapToImageSource(Properties.Resources.preview);
            settingsControl = new SettingsCtrl(this.settings);
            ((SettingsCtrl)SettingsControl).PaletteChanged += () => { ReloadTrackColors(); };
        }

        public void Init(MidiPlayback file, RenderStatus status)
        {
            midi = file;

            disposer = new DisposeGroup();

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
            midi.CheckParseDistance(settings.deltaTimeOnScreen);

            using (new GLEnabler().Enable(EnableCap.Blend))
            {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                renderSurface.BindSurfaceAndClear();

                flatShader.Bind();

                #region Vars
                var midiTime = midi.PlayerPosition;
                int firstNote = settings.firstNote;
                int lastNote = settings.lastNote;

                var keyboard = new KeyboardState(firstNote, lastNote, new KeyboardParams()
                {
                    SameWidthNotes = settings.sameWidthNotes,
                });

                int kbfirstNote = settings.firstNote;
                int kblastNote = settings.lastNote;
                if (keyboard.BlackKey[firstNote]) kbfirstNote--;
                if (keyboard.BlackKey[lastNote - 1]) kblastNote++;

                double screenTime = settings.deltaTimeOnScreen;
                float pianoHeight = (float)settings.pianoHeight;
                #endregion

                double notePosFactor = 1 / screenTime * (1 - pianoHeight);

                double renderCutoff = midiTime + screenTime;
                foreach (var n in midi.IterateNotes(renderCutoff))
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
            var cols = ((SettingsCtrl)SettingsControl).paletteList.GetColors(midi.TrackCount);
            midi.ApplyColors(cols);
        }
    }
}
