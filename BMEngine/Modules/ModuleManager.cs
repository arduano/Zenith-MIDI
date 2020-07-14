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
        BlendStateKeeper blendState;
        TextureSampler sampler;

        ShaderProgram downscale;
        Compositor composite;

        Initiator init = new Initiator();

        RasterizerStateKeeper raster;

        Device device = null;

        public ModuleManager()
        {
            blendState = init.Add(new BlendStateKeeper());
            raster = init.Add(new RasterizerStateKeeper());
            composite = init.Add(new Compositor());
            sampler = init.Add(new TextureSampler());
        }

        public ModuleManager(IModuleRender module) : this()
        {
            UseModule(module);
        }

        public ModuleManager(IModuleRender module, Device device, MidiPlayback midi, RenderStatus status) : this(module)
        {
            StartRender(device, midi, status);
        }

        public void UseModule(IModuleRender module)
        {
            while (initQueue.Count != 0)
            {
                var m = initQueue.Dequeue();
                if (m.Initialized)
                {
                    disposeQueue.Enqueue(m);
                }
            }
            if (device != null) initQueue.Enqueue(module);
            CurrentModule = module;
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

        public void StartRender(Device device, MidiPlayback file, RenderStatus status)
        {
            init.Replace(ref fullSizeFrame, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref downscale, Shaders.CompositeSSAA(status.OutputWidth, status.OutputHeight, status.SSAA));
            currentMidi = file;
            currentStatus = status;
            Init(device);
        }

        void ProcessQueues()
        {
            while (disposeQueue.Count != 0)
            {
                var m = disposeQueue.Dequeue();
                m.Dispose();
                ModuleDisposed?.Invoke(this, m);
            }
            while (initQueue.Count != 0)
            {
                var m = initQueue.Dequeue();
                m.Init(device, currentMidi, currentStatus);
                ModuleInitialized?.Invoke(this, m);
            }
        }

        public void RenderFrame(DeviceContext context, IRenderSurface outputSurface)
        {
            if (CurrentModule == null) return;
            ProcessQueues();

            using (fullSizeFrame.UseViewAndClear(context))
            using (blendState.UseOn(context))
            using (raster.UseOn(context))
            using (sampler.UseOnPS(context))
            {
                CurrentModule.RenderFrame(context, fullSizeFrame);
                context.ClearRenderTargetView(outputSurface.RenderTarget);
                composite.Composite(context, fullSizeFrame, downscale, outputSurface);
            }

            //fullSizeFrame.BindSurface();
            //CurrentModule.RenderFrame(fullSizeFrame);

            //composite.Composite(fullSizeFrame, downscale, outputSurface);
        }

        void Dispose()
        {
            device = null;

            init.Dispose();

            if (CurrentModule != null && CurrentModule.Initialized) disposeQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        void Init(Device device)
        {
            this.device = device;

            init.Init(device);
            //fullSizeFrame = disposer.Add(RenderSurface.BasicFrame(currentStatus.RenderWidth, currentStatus.RenderHeight));
            //downscale = disposer.Add(ShaderProgram.Presets.SSAA(currentStatus.OutputWidth, currentStatus.OutputHeight, currentStatus.SSAA));
            //composite = disposer.Add(new Compositor());

            if (CurrentModule != null && !CurrentModule.Initialized) initQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        public void ClearModules()
        {
            Dispose();
            CurrentModule = null;
        }

        public void EndRender()
        {
            Dispose();
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
