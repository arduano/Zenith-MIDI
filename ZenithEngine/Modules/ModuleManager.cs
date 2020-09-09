using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;
using ZenithEngine.DXHelper.Presets;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUI;

namespace ZenithEngine.Modules
{
    public class ModuleLoadFailedException : Exception
    {
        public ModuleLoadFailedException(string msg) : base(msg) { }
    }

    public class ModuleManager
    {
        public IModuleRender CurrentModule { get; private set; } = null;

        Queue<IModuleRender> initQueue = new Queue<IModuleRender>();
        Queue<IModuleRender> disposeQueue = new Queue<IModuleRender>();

        MidiPlayback currentMidi = null;
        RenderStatus currentStatus = null;

        event EventHandler<IModuleRender> ModuleDisposed;
        event EventHandler<IModuleRender> ModuleInitialized;

        CompositeRenderSurface fullSizeFrame;
        CompositeRenderSurface alphaFixFrame;
        BlendStateKeeper blendState;
        BlendStateKeeper pureBlendState;
        TextureSampler sampler;

        ShaderProgram downscale;
        ShaderProgram alphaFix;
        Compositor composite;

        Initiator init = new Initiator();

        RasterizerStateKeeper raster;

        DeviceGroup device = null;

        object queueLock = new object();

        public ModuleManager()
        {
            blendState = init.Add(new BlendStateKeeper());
            pureBlendState = init.Add(new BlendStateKeeper(BlendPreset.PreserveColor));
            raster = init.Add(new RasterizerStateKeeper());
            composite = init.Add(new Compositor());
            sampler = init.Add(new TextureSampler());
            alphaFix = init.Add(Shaders.AlphaAddFix());
        }

        public ModuleManager(IModuleRender module) : this()
        {
            UseModule(module);
        }

        public ModuleManager(IModuleRender module, DeviceGroup device, MidiPlayback midi, RenderStatus status) : this(module)
        {
            StartRender(device, midi, status);
        }

        public void UseModule(IModuleRender module)
        {
            lock (queueLock)
            {
                while (initQueue.Count != 0)
                {
                    var m = initQueue.Dequeue();
                    if (m.Initialized)
                    {
                        disposeQueue.Enqueue(m);
                    }
                }
                if (device != null && module != null) initQueue.Enqueue(module);
                if (CurrentModule != null && CurrentModule.Initialized)
                    disposeQueue.Enqueue(CurrentModule);
                CurrentModule = module;
            }
        }

        public JObject SerializeModule()
        {
            var contianer = CurrentModule?.SettingsControl as ISerializableContainer;
            if (contianer == null) return new JObject();
            else return contianer.Serialize();
        }

        public void ParseModule(JObject data)
        {
            var contianer = CurrentModule?.SettingsControl as ISerializableContainer;
            if (contianer == null) return;
            else contianer.Parse(data);
        }

        public void StartRender(DeviceGroup device, MidiPlayback file, RenderStatus status)
        {
            init.Replace(ref fullSizeFrame, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref alphaFixFrame, new CompositeRenderSurface(status.OutputWidth, status.OutputHeight));
            init.Replace(ref downscale, Shaders.CompositeSSAA(status.OutputWidth, status.OutputHeight, status.SSAA));
            currentMidi = file;
            currentStatus = status;
            Init(device);
        }

        void ProcessQueues()
        {
            lock (queueLock)
            {
                if (disposeQueue.Count != 0)
                {
                    currentMidi?.ClearNoteMeta();
                    while (disposeQueue.Count != 0)
                    {
                        var m = disposeQueue.Dequeue();
                        m.Dispose();
                        ModuleDisposed?.Invoke(this, m);
                    }
                }
                while (initQueue.Count != 0)
                {
                    var m = initQueue.Dequeue();
                    m.Init(device, currentMidi, currentStatus);
                    ModuleInitialized?.Invoke(this, m);
                }
            }
        }

        public void RenderFrame(DeviceContext context, IRenderSurface outputSurface)
        {
            if (CurrentModule == null) return;
            ProcessQueues();

            using (fullSizeFrame.UseViewAndClear(context))
            using (raster.UseOn(context))
            using (sampler.UseOnPS(context))
            {
                using (blendState.UseOn(context))
                {
                    CurrentModule.RenderFrame(context, fullSizeFrame);
                    context.ClearRenderTargetView(outputSurface);
                }

                using (pureBlendState.UseOn(context))
                {
                    composite.Composite(context, fullSizeFrame, downscale, alphaFixFrame);
                    composite.Composite(context, alphaFixFrame, alphaFix, outputSurface);
                }
            }

            //fullSizeFrame.BindSurface();
            //CurrentModule.RenderFrame(fullSizeFrame);

            //composite.Composite(fullSizeFrame, downscale, outputSurface);
        }

        void DisposeRender()
        {
            lock (queueLock)
            {
                device = null;

                init.Dispose();

                if (CurrentModule != null && CurrentModule.Initialized) disposeQueue.Enqueue(CurrentModule);
                ProcessQueues();
            }
        }

        void Init(DeviceGroup device)
        {
            this.device = device;

            init.Init(device);
            //fullSizeFrame = disposer.Add(RenderSurface.BasicFrame(currentStatus.RenderWidth, currentStatus.RenderHeight));
            //downscale = disposer.Add(ShaderProgram.Presets.SSAA(currentStatus.OutputWidth, currentStatus.OutputHeight, currentStatus.SSAA));
            //composite = disposer.Add(new Compositor());

            if (CurrentModule != null && !CurrentModule.Initialized) initQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        public void ClearModule()
        {
            DisposeRender();
            CurrentModule = null;
        }

        public void EndRender()
        {
            DisposeRender();
            currentMidi = null;
            currentStatus = null;
        }

        public static IModuleRender LoadModule(string path)
        {
            var DLL = Assembly.UnsafeLoadFrom(System.IO.Path.GetFullPath(path));
            return LoadModule(DLL);
        }

        public static IModuleRender LoadModule(Assembly dll)
        {
            string name = dll.FullName;
            try
            {
                bool hasClass = false;
                foreach (Type type in dll.GetExportedTypes())
                {
                    if (type.Name == "Render")
                    {
                        hasClass = true;
                        var instance = (IModuleRender)Activator.CreateInstance(type);
                        return instance;
                    }
                }
                if (!hasClass)
                {
                    throw new ModuleLoadFailedException("Could not load " + name + "\nDoesn't have render class");
                }
            }
            catch (RuntimeBinderException)
            {
                throw new ModuleLoadFailedException("Could not load " + name + "\nA binding error occured");
            }
            catch (InvalidCastException)
            {
                throw new ModuleLoadFailedException("Could not load " + name + "\nThe Render class was not a compatible with the interface");
            }
#if !DEBUG
            catch (Exception e)
            {
                throw new ModuleLoadFailedException("An error occured while binding " + name + "\n" + e.Message);
            }
#endif
            throw new ModuleLoadFailedException("An error occured while binding " + name);
        }
    }
}
