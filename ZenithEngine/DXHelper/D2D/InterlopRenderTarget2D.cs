using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;

namespace ZenithEngine.DXHelper.D2D
{
    public class InterlopRenderTarget2D : DeviceInitiable
    {
        public class Disposer : IDisposable
        {
            InterlopRenderTarget2D disp;

            internal Disposer(InterlopRenderTarget2D disp)
            {
                this.disp = disp;
            }

            public void Dispose()
            {
                disp.RenderTarget.EndDraw();
            }
        }

        public static implicit operator RenderTarget(InterlopRenderTarget2D r) => r.RenderTarget;

        IRenderSurface targetSurface;

        public InterlopRenderTarget2D(IRenderSurface targetSurface)
        {
            this.targetSurface = targetSurface;
        }

        public RenderTarget RenderTarget { get; private set; }

        protected override void InitInternal()
        {
            var dxgiSurface = targetSurface.Texture.QueryInterface<DXGI.Surface>();
            var renderTargetProperties = new RenderTargetProperties(new PixelFormat(DXGI.Format.R32G32B32A32_Float, AlphaMode.Premultiplied));
            RenderTarget = dispose.Add(new RenderTarget(Device, dxgiSurface, renderTargetProperties));
        }

        public IDisposable BeginDraw()
        {
            RenderTarget.BeginDraw();
            return new Disposer(this);
        }
    }
}
