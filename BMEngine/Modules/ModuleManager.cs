using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.GLEngine;
using ZenithEngine.MIDI;

namespace ZenithEngine.Modules
{
    public class ModuleLoadFailedException : Exception
    {
        public ModuleLoadFailedException(string msg) : base(msg) { }
    }

    public class ModuleManager : IDisposable
    {
        public IModuleRender CurrentModule { get; private set; } = null;

        Queue<IModuleRender> initQueue = new Queue<IModuleRender>();
        Queue<IModuleRender> disposeQueue = new Queue<IModuleRender>();

        MidiPlayback currentMidi = null;
        RenderStatus currentStatus = null;

        event EventHandler<IModuleRender> ModuleDisposed;
        event EventHandler<IModuleRender> ModuleInitialized;

        RenderSurface fullSizeFrame;
        ShaderProgram downscale;
        Compositor composite;
        DisposeGroup disposer;

        bool glRunning = false;

        public ModuleManager()
        {

        }

        public ModuleManager(IModuleRender module) : this()
        {
            UseModule(module);
        }

        public ModuleManager(IModuleRender module, MidiPlayback midi, RenderStatus status) : this(module)
        {
            StartRender(midi, status);
        }

        public void UseModule(IModuleRender module)
        {
            while(initQueue.Count != 0)
            {
                var m = initQueue.Dequeue();
                if(m.Initialized)
                {
                    disposeQueue.Enqueue(m);
                }
            }
            if(glRunning) initQueue.Enqueue(module);
            CurrentModule = module;
        }

        public void StartRender(MidiPlayback file, RenderStatus status)
        {
            currentMidi = file;
            currentStatus = status;
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
                m.Init(currentMidi, currentStatus);
                ModuleInitialized?.Invoke(this, m);
            }
        }

        public void RenderFrame(RenderSurface outputSurface)
        {
            if (CurrentModule == null) return;
            InitGL();

            fullSizeFrame.BindSurface();
            CurrentModule.RenderFrame(fullSizeFrame);

            composite.Composite(fullSizeFrame, downscale, outputSurface);
        }

        public void DisposeGL()
        {
            glRunning = false;

            if(disposer != null) disposer.Dispose();

            if (CurrentModule != null && CurrentModule.Initialized) disposeQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        public void InitGL()
        {
            if (!glRunning)
            {
                disposer = new DisposeGroup();
                fullSizeFrame = disposer.Add(RenderSurface.BasicFrame(currentStatus.RenderWidth, currentStatus.RenderHeight));
                downscale = disposer.Add(ShaderProgram.Presets.SSAA(currentStatus.OutputWidth, currentStatus.OutputHeight, currentStatus.SSAA));
                composite = disposer.Add(new Compositor());
            }

            glRunning = true;

            if (CurrentModule != null && !CurrentModule.Initialized) initQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        public void ClearModules()
        {
            DisposeGL();
            CurrentModule = null;
        }

        public void EndRender()
        {
            DisposeGL();
            currentMidi = null;
            currentStatus = null;
        }

        public void Dispose()
        {
            EndRender();
            CurrentModule = null;
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
            catch (Exception e)
            {
                throw new ModuleLoadFailedException("An error occured while bining " + name + "\n" + e.Message);
            }
            throw new ModuleLoadFailedException("An error occured while bining " + name);
        }
    }
}
