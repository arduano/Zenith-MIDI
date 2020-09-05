using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ZenithEngine.DXHelper
{
    public class CompositeRenderSurface : DeviceInitiable, IRenderSurface, ITextureResource, IDepthTextureResource
    {
        public ShaderResourceView TextureResource { get; private set; }
        public Texture2D Texture { get; private set; }
        public Texture2D DepthTexture { get; private set; }
        public ShaderResourceView DepthTextureResource { get; private set; }
        public RenderTargetView RenderTarget { get; private set; }
        public DepthStencilView RenderTargetDepth { get; private set; }

        public int Width { get; }
        public int Height { get; }

        public bool UseDepth { get; }
        public bool UseDepthResource { get; }

        public Format Format { get; }

        public CompositeRenderSurface(int width, int height, bool depth = false, bool depthResource = false, Format format = Format.R32G32B32A32_Float)
        {
            Width = width;
            Height = height;
            Format = format;
            UseDepth = depth;
            UseDepthResource = depthResource;
        }

        protected override void InitInternal()
        {
            Texture = dispose.Add(new Texture2D(Device, new Texture2DDescription()
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
            if (UseDepth)
            {
                DepthTexture = dispose.Add(new Texture2D(Device, new Texture2DDescription()
                {
                    Format = Format.D24_UNorm_S8_UInt,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = Width,
                    Height = Height,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = UseDepthResource ? BindFlags.DepthStencil | BindFlags.ShaderResource : BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.Shared
                }));
                RenderTargetDepth = dispose.Add(new DepthStencilView(Device, DepthTexture));
                if(UseDepthResource) DepthTextureResource = dispose.Add(new ShaderResourceView(Device, DepthTexture));
            }
            RenderTarget = dispose.Add(new RenderTargetView(Device, Texture));
            TextureResource = dispose.Add(new ShaderResourceView(Device, Texture));
        }
    }
}
