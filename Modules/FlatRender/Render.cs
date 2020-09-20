using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace FlatRender
{
    public class Render : PureModule
    {
        #region Info
        public override string Name { get; } = "Flat";
        public override string Description { get; } = "Flat renderer, requested by SquareWaveMidis for his channel";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        #endregion

        UI settings = LoadUI(() => new UI());

        public override ISerializableContainer SettingsControl => settings;
        public override double StartOffset => settings.noteScreenTime;

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
                });

                int kbfirstNote = firstNote;
                int kblastNote = lastNote;
                if (keyboard.BlackKey[firstNote]) kbfirstNote--;
                if (keyboard.BlackKey[lastNote - 1]) kblastNote++;

                float pianoHeight = (float)settings.kbHeight / 100;

                double notePosFactor = 1 / screenTime * (1 - pianoHeight);

                quadBuffer.UseContext(context);

                double renderCutoff = midiTime + screenTime;

                var keyed = Midi.IterateNotesKeyed(midiTime, renderCutoff);
                multithread.Render(context, firstNote, lastNote, !sameWidth, (key, push) =>
                {
                    float left = (float)keyboard.Notes[key].Left;
                    float right = (float)keyboard.Notes[key].Right;
                    var minBottom = pianoHeight - 0.1f;
                    foreach (var n in keyed[key])
                    {
                        if (n.Start < midiTime)
                        {
                            keyboard.BlendNote(key, n.Color);
                        }

                        float end = (float)(1 - (renderCutoff - n.End) * notePosFactor);
                        float start = (float)(1 - (renderCutoff - n.Start) * notePosFactor);
                        if (!n.HasEnded)
                            end = 1;

                        end = Math.Min(end, 1.1f);
                        start = Math.Max(start, minBottom);

                        push(new Vert2D(left, start, n.Color.Right));
                        push(new Vert2D(left, end, n.Color.Right));
                        push(new Vert2D(right, end, n.Color.Left));
                        push(new Vert2D(right, start, n.Color.Left));
                    }
                });

                for (int n = kbfirstNote; n < kblastNote; n++)
                {
                    if (keyboard.BlackKey[n]) continue;

                    float left = (float)(keyboard.Keys[n].Left);
                    float right = (float)(keyboard.Keys[n].Right);
                    var coll = keyboard.Colors[n].Left;
                    var colr = keyboard.Colors[n].Right;

                    quadBuffer.Push(left, pianoHeight, colr);
                    quadBuffer.Push(right, pianoHeight, colr);
                    quadBuffer.Push(right, 0, coll);
                    quadBuffer.Push(left, 0, coll);
                }

                for (int n = kbfirstNote; n < kblastNote; n++)
                {
                    if (!keyboard.BlackKey[n]) continue;

                    float left = (float)(keyboard.Keys[n].Left);
                    float right = (float)(keyboard.Keys[n].Right);
                    var coll = keyboard.Colors[n].Left;
                    var colr = keyboard.Colors[n].Right;
                    float keyBottom = (float)(pianoHeight / 10 * 3.7);

                    quadBuffer.Push(left, pianoHeight, colr);
                    quadBuffer.Push(right, pianoHeight, colr);
                    quadBuffer.Push(right, keyBottom, coll);
                    quadBuffer.Push(left, keyBottom, coll);
                }
                quadBuffer.Flush();
            }
        }
    }
}
