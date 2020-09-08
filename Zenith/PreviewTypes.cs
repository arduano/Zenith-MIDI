using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;

namespace Zenith
{
    public class PreviewState
    {
        Action stopAction;

        public bool VSync { get; set; } = false;

        public IRenderSurface RenderTarget { get; set; }

        public PreviewState(Action stopAction)
        {
            this.stopAction = stopAction;
        }

        public void Stop() =>
            stopAction();
    }

    public abstract class PreviewBase
    {
        public bool Running { get; private set; }

        public DeviceGroup Device { get; }

        public PreviewBase(DeviceGroup device)
        {
            Device = device;
        }

        protected abstract void StopInternal();
        protected abstract void RunInternal(Action<PreviewState> renderFrame, PreviewState state);

        void Stop()
        {
            if (!Running) return;
            StopInternal();
            Running = false;
        }

        public void Run(Action<PreviewState> renderFrame)
        {
            Running = true;
            var state = new PreviewState(Stop);
            RunInternal(renderFrame, state);
        }
    }

    public class WindowPreview : PreviewBase
    {
        ManagedRenderWindow window;
        
        public WindowPreview(DeviceGroup device) : base(device)
        { }

        protected override void RunInternal(Action<PreviewState> renderFrame, PreviewState state)
        {
            window = new ManagedRenderWindow(Device, 1280, 720);
            RenderLoop.Run(window, () =>
            {
                state.RenderTarget = window;
                renderFrame(state);
                window.Present(state.VSync);
            });
            window.Dispose();
        }

        protected override void StopInternal()
        {
            window.Close();
        }
    }
}
