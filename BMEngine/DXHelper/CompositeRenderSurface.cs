using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ZenithEngine.DXHelper
{
    public class CompositeRenderSurface : DeviceInitiable, IRenderSurface
    {
        public ShaderResourceView TextureResource { get; private set; }
        public Texture2D Texture { get; private set; }
        public RenderTargetView RenderTarget { get; private set; }

        public int Width { get; }
        public int Height { get; }

        public Format Format { get; }

        public CompositeRenderSurface(int width, int height, Format format = Format.R32G32B32A32_Float)
        {
            Width = width;
            Height = height;
            Format = format;
        }

        DisposeGroup disposer;

        protected override void InitInternal()
        {
            disposer = new DisposeGroup();
            Texture = disposer.Add(new Texture2D(Device, new Texture2DDescription()
            {
                Width = Width,
                Height = Height,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                SampleDescription = new SampleDescription(1, 0),
            }));
            RenderTarget = disposer.Add(new RenderTargetView(Device, Texture));
            TextureResource = disposer.Add(new ShaderResourceView(Device, Texture));
        }

        protected override void DisposeInternal()
        {
            disposer.Dispose();
        }
    }
}
