using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public class RenderSurface : IDisposable, IRenderSurface
    {
        public RenderSurface(Device device, Texture2D texture) : this(texture, new RenderTargetView(device, texture)) { }
        public RenderSurface(Texture2D texture, RenderTargetView renderTarget)
        {
            Texture = texture;
            RenderTarget = renderTarget;
        }

        public DepthStencilView RenderTargetDepth => null;

        public Texture2D Texture { get; }

        public RenderTargetView RenderTarget { get; }

        public int Width => Texture.Description.Width;
        public int Height => Texture.Description.Height;

        public void Dispose()
        {
            Texture.Dispose();
            RenderTarget.Dispose();
        }
    }
}
