using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.Modules;
using ZenithEngine.MIDI;
using ZenithEngine;
using ZenithEngine.MIDI.Disk;
using ZenithEngine.Output;
using ZenithEngine.GLEngine;
using ZenithEngine.GLEngine.Types;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Diagnostics;
using ZenithEngine.MIDI.Audio;

namespace Zenith
{
    public struct RenderProgress
    {

    }

    public class RenderPipeline : IDisposable
    {
        public RenderStatus Status { get; }
        public MidiPlayback Playback { get; }
        public ModuleManager Module { get; }

        public bool VSync { get; set; } = false;

        public bool Running => Status.Running;

        public event Action<bool> PauseToggled;
        private bool paused = false;
        public bool Paused
        {
            get => paused;
            set
            {
                bool change = paused != value;
                paused = value;
                if(change) PauseToggled?.Invoke(paused);
            }
        }

        public double PreviewSpeed { get; set; } = 1;

        public bool Rendering { get; }

        Task renderTask = null;

        public EventHandler RenderStarted;
        public EventHandler<RenderProgress> RenderProgress;
        public EventHandler RenderEnded;
        public EventHandler RenderErrored;

        public RenderPipeline(RenderStatus status, MidiPlayback playback, ModuleManager module, bool rendering)
        {
            Status = status;
            Playback = playback;
            Module = module;
            Rendering = rendering;
            Playback.PushPlaybackEvents = Status.PreviewAudioEnabled && !Rendering;
        }

        public void Start()
        {
            renderTask = Task.Run(Runner);
        }

        void Runner()
        {
            var win = new PreviewWindow(1280, 720, GraphicsMode.Default, "test", GameWindowFlags.Default, DisplayDevice.Default, 1, 0, GraphicsContextFlags.Default);
            win.Run();

            Module.StartRender(Playback, Status);

            var disposer = new DisposeGroup();

            var buff = disposer.Add(new TexturedShapeBuffer(100, ShapePresets.Quads));
            var shader = disposer.Add(ShaderProgram.Presets.BasicTextured());
            if (!Rendering) disposer.Add(new MIDIAudio(Playback, new KDMAPIOutput()));

            var comp = disposer.Add(new Compositor());

            var surface = disposer.Add(RenderSurface.BasicFrame(Status.OutputWidth, Status.OutputHeight));

            win.ProcessEvents();
            win.VSync = VSyncMode.Off;

            Stopwatch time = new Stopwatch();
            time.Start();

            while (true)
            {
                win.ProcessEvents();
                win.VSync = VSyncMode.Off;
                Playback.PushPlaybackEvents = Status.PreviewAudioEnabled && !Rendering;

                // Let the module handle frame rendering
                Module.RenderFrame(surface);

                using (new GLEnabler().Enable(EnableCap.Blend))
                {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                    // Transfer the rendered frame to the preview window
                    comp.Composite(surface, shader, win.RenderTarget);

                }

                win.SwapBuffers();

                if (!Paused)
                {
                    if (Status.RealtimePlayback)
                        Playback.AdvancePlayback(Math.Min(time.Elapsed.TotalSeconds, 5));
                    else
                        Playback.AdvancePlayback(1.0 / Status.FPS);
                }
                time.Reset();
                time.Start();

                if (Playback.PlayerPositionSeconds > Playback.Midi.SecondsLength + 5) 
                    break;
                if (!Status.Running) break;

                GC.Collect(2, GCCollectionMode.Optimized);
            }

            Playback.Dispose();
            Module.EndRender();

            disposer.Dispose();

            win.Close();
            win.Dispose();
        }

        public void Dispose()
        {
            Status.Running = false;
            renderTask.Wait();
        }
    }
}
