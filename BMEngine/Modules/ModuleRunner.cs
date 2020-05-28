using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.GLEngine;
using ZenithEngine.MIDI;

namespace ZenithEngine.Modules
{
    public class ModuleRunner : IDisposable
    {
        public IModuleRender CurrentModule { get; private set; } = null;

        Queue<IModuleRender> initQueue = new Queue<IModuleRender>();
        Queue<IModuleRender> disposeQueue = new Queue<IModuleRender>();

        MidiPlayback currentMidi = null;

        event EventHandler<IModuleRender> ModuleDisposed;
        event EventHandler<IModuleRender> ModuleInitialized;

        public ModuleRunner()
        {

        }

        public ModuleRunner(IModuleRender module) : this()
        {
            UseModule(module);
        }

        public ModuleRunner(IModuleRender module, MidiPlayback midi) : this(module)
        {
            StartRender(midi);
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
            initQueue.Enqueue(module);
            CurrentModule = module;
        }

        public void StartRender(MidiPlayback file)
        {
            currentMidi = file;
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
                m.Init(currentMidi);
                ModuleInitialized?.Invoke(this, m);
            }
        }

        public void RenderFrame(RenderSurface outputSurface)
        {
            InitGL();
            outputSurface.BindBuffer();
            CurrentModule.RenderFrame(outputSurface);
        }

        public void DisposeGL()
        {
            if (CurrentModule.Initialized) disposeQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        public void InitGL()
        {
            if (!CurrentModule.Initialized) initQueue.Enqueue(CurrentModule);
            ProcessQueues();
        }

        public void EndRender()
        {
            DisposeGL();
            currentMidi = null;
        }

        public void Dispose()
        {
            EndRender();
        }
    }
}
