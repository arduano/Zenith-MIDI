using MIDITrailRender.Models;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUtil;

namespace MIDITrailRender.Logic
{
    public class NoteRenderer : DeviceInitiable
    {
        DepthStencilStateKeeper depthStencil;
        DepthStencilStateKeeper noDepthStencil;
        ShaderProgram<NoteShaderConstant> noteShader;

        NoteModelData noteModels;

        public NoteRenderer(FullModelData allModels)
        {
            depthStencil = init.Add(new DepthStencilStateKeeper(DepthStencilPresets.Basic));
            noDepthStencil = init.Add(new DepthStencilStateKeeper(DepthStencilPresets.Always));
            noteModels = init.Add(allModels.Notes);

            noteShader = init.Add(new ShaderProgram<NoteShaderConstant>(
                Util.ReadEmbed("MIDITrailRender.Shaders.notes.fx"),
                typeof(NoteVert),
                typeof(NoteInstance),
                "4_0",
                "VS",
                "PS"
            ));
        }

        public void RenderNotes(DeviceContext context, BaseModel settings, MidiPlayback playback, Camera camera, KeyboardState keyboard)
        {
            var startCutoff = (float)-settings.Camera.RenderDistBack;
            var endCutoff = (float)settings.Camera.RenderDistForward;
            var minLength = (float)keyboard.WhiteNoteWidth / 2;

            var time = playback.PlayerPosition;

            float noteScale = (float)settings.General.NoteScale;

            IEnumerable<int> filterBlack(IEnumerable<int> original)
            {
                if (settings.General.SameWidthNotes)
                {
                    foreach (var i in original)
                        yield return i;
                }
                else
                {
                    foreach (var i in original)
                        if (!keyboard.BlackKey[i])
                            yield return i;
                    foreach (var i in original)
                        if (keyboard.BlackKey[i])
                            yield return i;
                }
            }

            IEnumerable<NoteInstance> TransformNotes(IEnumerable<Note> notes, int key, bool modifyKeyboard)
            {
                var notePos = keyboard.Notes[key];
                var keyPos = keyboard.Keys[key];
                var left = (float)notePos.Left - 0.5f;
                var right = (float)notePos.Right - 0.5f;
                var middle = (float)(keyPos.Left + keyPos.Right) / 2 - 0.5f;
                //left -= middle;
                //right -= middle;

                foreach (var n in notes)
                {
                    if (modifyKeyboard)
                    {
                        if (n.Start < time && (n.End > time || !n.HasEnded))
                        {
                            keyboard.PressKey(n.Key);
                            keyboard.BlendNote(n.Key, n.Color);
                        }
                    }

                    var noteStart = (float)((n.Start - time) / noteScale);
                    var noteEnd = (float)((n.End - time) / noteScale);

                    if (!n.HasEnded || noteEnd > endCutoff) noteEnd = endCutoff;
                    if (noteStart < startCutoff) noteStart = startCutoff;

                    if (noteEnd - noteStart < minLength)
                    {
                        var noteMiddle = (noteStart + noteEnd) / 2;
                        noteStart = noteMiddle - minLength / 2;
                        noteEnd = noteMiddle + minLength / 2;
                    }

                    yield return new NoteInstance(
                        left,
                        right,
                        noteStart,
                        noteEnd,
                        n.Color.Left,
                        n.Color.Right,
                        (float)keyboard.BlackNoteWidth,
                        (float)keyboard.BlackNoteWidth
                    );
                }
            }

            var noteStreamPositions = keyboard.Keys.Select(k => new Vector3((float)(k.Left + k.Right) / 2 - 0.5f, 0, 0)).ToArray();
            var order = Enumerable.Range(keyboard.FirstKey, keyboard.LastKey)
                .OrderBy(k => -(noteStreamPositions[k] - camera.ViewLocation).Length())
                .ToArray();

            var notes = playback.IterateNotesKeyed(time + startCutoff * noteScale, time + endCutoff * noteScale);

            var noteParts = noteModels.Rounded;

            noteShader.ConstData.View = camera.ViewPerspective;
            noteShader.ConstData.ViewPos = camera.ViewLocation;
            noteShader.ConstData.Model = Matrix.Identity;

            using (noteShader.UseOn(context))
            {
                foreach (var k in filterBlack(order))
                {
                    using (noDepthStencil.UseOn(context))
                    {
                        var fluser = noteParts.Body;
                        fluser.UseContext(context);
                        foreach (var n in TransformNotes(notes[k], k, true))
                            fluser.Push(n);
                        fluser.Flush();
                    }
                    if (noteParts.HasCap)
                    {
                        using (depthStencil.UseOn(context))
                        {
                            var fluser = noteParts.Cap;
                        fluser.UseContext(context);
                            foreach (var n in TransformNotes(notes[k], k, false))
                                fluser.Push(n);
                            fluser.Flush();
                        }
                    }
                }
            }
        }
    }
}
