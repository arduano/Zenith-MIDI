using DX.WPF;
using SharpDX.Direct3D11;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using ZenithEngine.DXHelper;
using Application = System.Windows.Application;

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
        public ManagedRenderWindow Window { get; private set; }

        public WindowPreview(DeviceGroup device) : base(device)
        { }

        protected override void RunInternal(Action<PreviewState> renderFrame, PreviewState state)
        {
            Window = new ManagedRenderWindow(Device, 1280, 720);
            Window.Text = "Zenith Preview";
            Window.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            Window.KeyDown += Window_KeyDown;
            RenderLoop.Run(Window, () =>
            {
                state.RenderTarget = Window;
                renderFrame(state);
                Window.Present(state.VSync);
            });
            Window.Dispose();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Window != null && e.KeyCode == Keys.Enter)
            {
                Window.Fullscreen = !Window.Fullscreen;
            }
        }

        protected override void StopInternal()
        {
            Window.Close();
        }
    }

    public class ElementPreview : PreviewBase
    {
        struct DXRenderTarget : IRenderSurface
        {
            public DXRenderTarget(D3D11 dX)
            {
                DX = dX;
            }

            public D3D11 DX { get; }

            public Texture2D Texture => DX.RenderTarget;
            public RenderTargetView RenderTarget => DX.RenderTargetView;
            public DepthStencilView RenderTargetDepth => null;

            public int Width => (int)DX.RenderSize.X;
            public int Height => (int)DX.RenderSize.Y;
        }

        protected static T UI<T>(Func<T> load)
        {
            T data = default;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                data = load();
            }).Wait();
            return data;
        }

        public DXElement Element { get; } = UI(() => new DXElement());

        public ElementPreview(DeviceGroup device) : base(device)
        { }

        bool ended = false;

        protected override void RunInternal(Action<PreviewState> renderFrame, PreviewState state)
        {
            var scene = new EventScene<D3D11>();
            var dx = new D3D11(Device.D3Device);
            dx.FPSLock = 0;

            bool stopped = false;

            Exception error = null;

            void Scene_OnRender(object sender, DrawEventArgs e)
            {
                try
                {
                    state.RenderTarget = new DXRenderTarget(dx);
                    renderFrame(state);
                }
                catch (OperationCanceledException)
                {
                    stopped = true;
                    return;
                }
                catch (Exception err)
                {
                    error = err;
                    stopped = true;
                    return;
                }
                if (ended)
                {
                    scene.OnRender -= Scene_OnRender;
                    stopped = true;
                }
            }

            scene.OnRender += Scene_OnRender;

            scene.Renderer = dx;
            UI(() => Element.Renderer = scene);
            dx.SingleThreadedRender = false;

            SpinWait.SpinUntil(() => stopped);

            if (error != null)
            {
                throw new AggregateException(error);
            }

            UI(() => Element.Renderer = null);
            scene.Renderer = null;
            dx.Dispose();
        }

        protected override void StopInternal()
        {
            ended = true;
        }
    }
}
