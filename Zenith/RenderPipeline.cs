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
using System.Diagnostics;
using ZenithEngine.MIDI.Audio;
using System.Windows;
using ZenithEngine.DXHelper;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX;
using ZenithEngine.DXHelper.Presets;
using System.Threading;
using System.IO;
using DX.WPF;

namespace Zenith
{
    public struct RenderProgressData
    {
        public RenderProgressData(double seconds, long renderedNotes, long renderFrameNumber)
        {
            Seconds = seconds;
            RenderedNotes = renderedNotes;
            RenderFrameNumber = renderFrameNumber;
        }

        public double Seconds { get; }
        public long RenderedNotes { get; }
        public long RenderFrameNumber { get; }
    }

    public class OutputSettings
    {
        public bool UseMask => OutputMask != null;

        public string OutputVideo { get; } = null;
        public string OutputMask { get; } = null;
        public string Args { get; } = null;

        public OutputSettings(string args, string output)
        {
            OutputVideo = output;
            Args = args;
        }

        public OutputSettings(string args, string output, string mask)
        {
            OutputVideo = output;
            OutputMask = mask;
            Args = args;
        }
    }

    public class RenderPipeline : IDisposable
    {
        interface IPreview : IDeviceInitiable
        {
            void RenderFrame(DeviceContext context, IRenderSurface outputSurface);
            public CompositeRenderSurface RenderTarget { get; }
        }

        abstract class CompositeBase : DeviceInitiable, IPreview
        {
            protected RenderPipeline pipeline;
            protected Initiator init = new Initiator();
            protected DisposeGroup disposer = new DisposeGroup();

            Textured2dShapeBuffer buffer;

            ShaderProgram basicShader;
            ShaderProgram maskAlphaShader;
            ShaderProgram maskColorShader;

            protected BlendStateKeeper pureBlendState;

            public CompositeRenderSurface RenderTarget { get; }

            protected CompositeRenderSurface RenderedMain { get; }
            protected CompositeRenderSurface RenderedMask { get; }

            protected CompositeRenderSurface PreviewOutput { get; }

            protected AspectRatioComposite aspectComposite;
            protected Compositor composite;

            protected bool useMask;

            public CompositeBase(RenderPipeline pipeline, bool useMask)
            {
                this.useMask = useMask;
                this.pipeline = pipeline;
                RenderTarget = init.Add(new CompositeRenderSurface(pipeline.Status.OutputWidth, pipeline.Status.OutputHeight));
                pureBlendState = init.Add(new BlendStateKeeper(BlendPreset.PreserveColor));
                if (useMask)
                {
                    RenderedMain = init.Add(new CompositeRenderSurface(pipeline.Status.OutputWidth, pipeline.Status.OutputHeight));
                    RenderedMask = init.Add(new CompositeRenderSurface(pipeline.Status.OutputWidth, pipeline.Status.OutputHeight));
                    PreviewOutput = init.Add(new CompositeRenderSurface(pipeline.Status.OutputWidth, pipeline.Status.OutputHeight / 2));
                    composite = init.Add(new Compositor());
                    basicShader = init.Add(Shaders.BasicTextured());
                    maskAlphaShader = init.Add(Shaders.TransparencyMask(false));
                    maskColorShader = init.Add(Shaders.TransparencyMask(true));
                    buffer = init.Add(new Textured2dShapeBuffer(16));
                }
                else
                {
                    RenderedMain = RenderTarget;
                    PreviewOutput = RenderTarget;
                }
                aspectComposite = init.Add(new AspectRatioComposite());
            }

            protected override void InitInternal()
            {
                disposer = new DisposeGroup();
                init.Init(Device);
                disposer.Add(init);
            }

            protected override void DisposeInternal()
            {
                disposer.Dispose();
            }

            protected void ProcessFrame(DeviceContext context)
            {
                if (useMask)
                {
                    using (pureBlendState.UseOn(context))
                    {
                        composite.Composite(context, RenderTarget, maskColorShader, RenderedMain);
                        composite.Composite(context, RenderTarget, maskAlphaShader, RenderedMask);
                        buffer.UseContext(context);
                        using (PreviewOutput.UseViewAndClear(context))
                        using (basicShader.UseOn(context))
                        {
                            using (RenderedMain.UseOnPS(context))
                            {
                                buffer.PushQuad(0, 1, 0.5f, 0);
                                buffer.Flush();
                            }

                            using (RenderedMask.UseOnPS(context))
                            {
                                buffer.PushQuad(0.5f, 1, 1, 0);
                                buffer.Flush();
                            }
                        }
                    }
                }
            }

            public abstract void RenderFrame(DeviceContext context, IRenderSurface outputSurface);
        }

        class BasicComposite : CompositeBase
        {
            public BasicComposite(RenderPipeline pipeline) : base(pipeline, false) { }

            public override void RenderFrame(DeviceContext context, IRenderSurface outputSurface)
            {
                ProcessFrame(context);
                using (pureBlendState.UseOn(context))
                    aspectComposite.Composite(context, PreviewOutput, outputSurface);
            }
        }

        class RenderComposite : CompositeBase
        {
            FFMpegOutput output;
            FFMpegOutput outputMask;
            Logger outputLogger;
            Logger outputMaskLogger;
            public RenderComposite(RenderPipeline pipeline) : base(pipeline, pipeline.RenderArgs.UseMask)
            {
            }

            public override void Init(DeviceGroup device)
            {
                base.Init(device);

                var args = pipeline.RenderArgs;
                var status = pipeline.Status;
                outputLogger = Logs.GetFFMpegLogger(false);
                output = new FFMpegOutput(Device, status.RenderWidth, status.RenderHeight, status.FPS, args.Args, args.OutputVideo, outputLogger);
                output.Errored += (s, e) => outputLogger.OpenLogData();
                if (args.UseMask)
                {
                    outputMaskLogger = Logs.GetFFMpegLogger(true);
                    outputMask = new FFMpegOutput(Device, status.RenderWidth, status.RenderHeight, status.FPS, args.Args, args.OutputMask, outputLogger);
                    outputMask.Errored += (s, e) => outputMaskLogger.OpenLogData();
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                output?.Dispose();
                outputMask?.Dispose();
            }

            public override void RenderFrame(DeviceContext context, IRenderSurface outputSurface)
            {
                ProcessFrame(context);
                output.PushFrame(context, RenderedMain);
                outputMask?.PushFrame(context, RenderedMask);
                using (pureBlendState.UseOn(context))
                    aspectComposite.Composite(context, PreviewOutput, outputSurface);
            }
        }

        public RenderStatus Status { get; }
        public MidiPlayback Playback { get; }
        public ModuleManager Module { get; }

        public bool VSync { get; set; } = false;

        public bool Running => Status.Running;

        public double StartTime { get; }
        public double EndTime { get; }

        public event Action<bool> PauseToggled;
        private bool paused = false;
        public bool Paused
        {
            get => paused;
            set
            {
                bool change = paused != value;
                paused = value;
                if (midiAudio != null) midiAudio.Paused = paused;
                if (change) PauseToggled?.Invoke(paused);
            }
        }

        MIDIAudio midiAudio = null;

        public double PreviewSpeed
        {
            get => previewSpeed;
            set
            {
                previewSpeed = value;
                if (midiAudio != null) midiAudio.PlaybackSpeed = previewSpeed;
            }
        }

        public bool Rendering => RenderArgs != null;
        public OutputSettings RenderArgs { get; }

        Thread renderTask = null;

        public event EventHandler RenderStarted;
        public event EventHandler<RenderProgressData> RenderProgress;
        public event EventHandler RenderEnded;
        public event EventHandler RenderErrored;
        private double previewSpeed = 1;

        private DXElement OutputElement { get; }

        public RenderPipeline(RenderStatus status, MidiPlayback playback, ModuleManager module, OutputSettings renderSettings, DXElement outputElment = null)
        {
            Status = status;
            Playback = playback;
            Module = module;
            RenderArgs = renderSettings;
            Playback.PushPlaybackEvents = Status.PreviewAudioEnabled && !Rendering;

            OutputElement = outputElment;

            EndTime = Playback.Midi.SecondsLength + 5;
            StartTime = Playback.PlayerPositionSeconds;
        }

        public RenderPipeline(RenderStatus status, MidiPlayback playback, ModuleManager module) :
            this(status, playback, module, null, null)
        { }

        public Task Start(PreviewBase preview) => Start(preview, new CancellationTokenSource().Token);
        public Task Start(PreviewBase preview, CancellationToken cancel)
        {
#if DEBUG
            return Task.Run(() =>
            {
                renderTask = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        Runner(preview, cancel);
                    }
                    catch (OperationCanceledException)
                    { }
                }));
                renderTask.Start();
                renderTask.Join();
            });
#else
            return Task.Run(() =>
            {
                Runner(preview, cancel);
            });
#endif
        }

        void Runner(PreviewBase preview, CancellationToken cancel)
        {
            var dispose = new DisposeGroup();

            try
            {
                var device = preview.Device;

                var init = dispose.Add(new Initiator());

                IPreview previewComposite;
                if (Rendering) previewComposite = init.Add(new RenderComposite(this));
                else previewComposite = init.Add(new BasicComposite(this));

                Module.StartRender(device, Playback, Status);

                init.Init(device);

                Stopwatch time = new Stopwatch();

                long frameNum = 0;

                if (!Rendering) midiAudio = dispose.Add(new MIDIAudio(Playback, new KDMAPIOutput()));

                RenderStarted?.Invoke(this, new EventArgs());
                var context = device.D3Device.ImmediateContext;
                preview.Run(state =>
                {
                    context.ClearRenderTargetView(state.RenderTarget);
                    Module.RenderFrame(context, previewComposite.RenderTarget);

                    try
                    {
                        previewComposite.RenderFrame(context, state.RenderTarget);
                    }
                    catch (FFMpegException)
                    {
                        state.Stop();
                        return;
                    }

                    if (!Paused)
                    {
                        if (Status.RealtimePlayback)
                            Playback.AdvancePlayback(Math.Min(time.Elapsed.TotalSeconds, 5) * PreviewSpeed);
                        else
                            Playback.AdvancePlayback(1.0 / Status.FPS * PreviewSpeed);
                    }
                    time.Reset();
                    time.Start();

                    if (Playback.PlayerPositionSeconds > EndTime)
                        state.Stop();
                    if (!Status.Running) 
                        state.Stop();

                    RenderProgress?.Invoke(this, new RenderProgressData((double)Playback.PlayerPositionSeconds, (long)Playback.LastIterateNoteCount, frameNum++));

                    cancel.ThrowIfCancellationRequested();
                });
            }
            finally
            {
                dispose.Dispose();
                Module.EndRender();
            }

            RenderEnded?.Invoke(this, new EventArgs());

            //var win = new PreviewWindow(1280, 720, GraphicsMode.Default, "test", GameWindowFlags.Default, DisplayDevice.Default, 1, 0, GraphicsContextFlags.Default);
            //win.Run();

            //Module.StartRender(Playback, Status);

            //var disposer = new DisposeGroup();

            //FFMpeg outputVid = null;
            //FFMpeg outputMask = null;
            //ShaderProgram maskedVidShader = null;
            //ShaderProgram maskedMaskShader = null;
            //RenderSurface surfaceVidSurface = null;
            //RenderSurface surfaceMaskSurface = null;
            //RenderSurface surfaceMaskedVidComposite = null;
            //if (Rendering)
            //{
            //    outputVid = disposer.Add(new FFMpeg(Status.OutputWidth, Status.OutputHeight, Status.FPS, RenderArgs.Args, RenderArgs.OutputVideo));
            //    if (RenderArgs.UseMask)
            //    {
            //        maskedVidShader = disposer.Add(ShaderProgram.Presets.BasicTextured("vec4(col.r / col.a, col.g / col.a, col.b / col.a, 1)"));
            //        maskedMaskShader = disposer.Add(ShaderProgram.Presets.BasicTextured("vec4(col.a, col.a, col.a, 1)"));
            //        outputMask = disposer.Add(new FFMpeg(Status.OutputWidth, Status.OutputHeight, Status.FPS, RenderArgs.Args, RenderArgs.OutputMask));
            //        surfaceVidSurface = disposer.Add(RenderSurface.BasicFrame(Status.OutputWidth, Status.OutputHeight));
            //        surfaceMaskSurface = disposer.Add(RenderSurface.BasicFrame(Status.OutputWidth, Status.OutputHeight));
            //        surfaceMaskedVidComposite = disposer.Add(RenderSurface.BasicFrame(Status.OutputWidth, Status.OutputHeight / 2));
            //    }
            //}

            //var buff = disposer.Add(new TexturedShapeBuffer(16, ShapePresets.Quads));
            //var solidFill = disposer.Add(new BasicShapeBuffer(16, ShapePresets.Quads));
            //var texShader = disposer.Add(TexturedShapeBuffer.GetBasicShader());
            //var solidShader = disposer.Add(BasicShapeBuffer.GetBasicShader());

            //if (!Rendering) midiAudio = disposer.Add(new MIDIAudio(Playback, new KDMAPIOutput()));

            //var comp = disposer.Add(new Compositor());

            //var surface = disposer.Add(RenderSurface.BasicFrame(Status.OutputWidth, Status.OutputHeight));

            //void CompositeWithAspect(RenderSurface from)
            //{
            //    float winAspect = (float)win.Width / win.Height;
            //    float fromAspect = (float)from.Width / from.Height;
            //    var size = (winAspect - fromAspect) / winAspect;
            //    var offset = size / 2;

            //    Vector2 tl = new Vector2(offset, 0);
            //    Vector2 br = new Vector2(1 - offset, 1);

            //    if (size < 0)
            //    {
            //        size = ((1 / winAspect) - (1 / fromAspect)) / (1 / winAspect);
            //        offset = size / 2;

            //        tl = new Vector2(0, offset);
            //        br = new Vector2(1, 1 - offset);
            //    }

            //    win.RenderTarget.BindSurfaceAndClear();

            //    solidShader.Bind();
            //    solidFill.PushVertex(0, 0, Color4.Gray);
            //    solidFill.PushVertex(1, 0, Color4.Gray);
            //    solidFill.PushVertex(1, 1, Color4.Gray);
            //    solidFill.PushVertex(0, 1, Color4.Gray);
            //    solidFill.PushVertex(tl.X, tl.Y, Color4.Black);
            //    solidFill.PushVertex(br.X, tl.Y, Color4.Black);
            //    solidFill.PushVertex(br.X, br.Y, Color4.Black);
            //    solidFill.PushVertex(tl.X, br.Y, Color4.Black);
            //    solidFill.Flush();

            //    from.BindTexture();
            //    texShader.Bind();
            //    buff.PushQuad(tl, br);
            //    buff.Flush();
            //}


            //win.ProcessEvents();
            //win.VSync = VSyncMode.Off;

            //Stopwatch time = new Stopwatch();
            //time.Start();

            //while (true)
            //{
            //    win.ProcessEvents();
            //    win.VSync = VSyncMode.Off;
            //    Playback.PushPlaybackEvents = Status.PreviewAudioEnabled && !Rendering;

            //    try
            //    {
            //        // Let the module handle frame rendering
            //        Module.RenderFrame(surface);
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show("Module render call crashed!\n\n" + e.Message + "\n" + e.StackTrace, "Render crashed");
            //        break;
            //    }

            //    using (new GLEnabler().Enable(EnableCap.Blend))
            //    {
            //        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //        if (Rendering)
            //        {
            //            if (RenderArgs.UseMask)
            //            {
            //                comp.Composite(surface, maskedVidShader, surfaceVidSurface);
            //                comp.Composite(surface, maskedMaskShader, surfaceMaskSurface);

            //                surfaceMaskedVidComposite.BindSurfaceAndClear();
            //                texShader.Bind();
            //                surfaceVidSurface.BindTexture();
            //                buff.PushQuad(0, 0, 0.5f, 1);
            //                buff.Flush();
            //                surfaceMaskSurface.BindTexture();
            //                buff.PushQuad(0.5f, 0, 1, 1);
            //                buff.Flush();

            //                outputVid.WriteFrame(surfaceVidSurface);
            //                outputMask.WriteFrame(surfaceMaskSurface);

            //                CompositeWithAspect(surfaceMaskedVidComposite);
            //            }
            //            else
            //            {
            //                outputVid.WriteFrame(surface);
            //                CompositeWithAspect(surface);
            //            }
            //        }
            //        else
            //        {
            //            CompositeWithAspect(surface);
            //        }
            //    }

            //    if (!Paused)
            //    {
            //        if (Status.RealtimePlayback)
            //            Playback.AdvancePlayback(Math.Min(time.Elapsed.TotalSeconds, 5) * PreviewSpeed);
            //        else
            //            Playback.AdvancePlayback(1.0 / Status.FPS * PreviewSpeed);
            //    }
            //    time.Reset();
            //    time.Start();

            //    if (Playback.PlayerPositionSeconds > Playback.Midi.SecondsLength + 5)
            //        break;
            //    if (!Status.Running) break;

            //    GC.Collect(2, GCCollectionMode.Optimized);

            //    win.SwapBuffers();
            //}

            //Playback.Dispose();
            //Module.EndRender();

            //disposer.Dispose();

            //win.Close();
            //win.Dispose();
        }

        public void Dispose()
        {
            //Status.Running = false;
            //renderTask.Join();
        }
    }
}
