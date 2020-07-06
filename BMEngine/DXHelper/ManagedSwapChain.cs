using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public class ManagedSwapChain : DeviceInitiable, IRenderSurface
    {
        public Texture2D Texture => throw new NotImplementedException();

        public RenderTargetView RenderTarget => throw new NotImplementedException();

        public int Width { get; private set; } = 1280;

        public int Height { get; private set; } = 720;

        SwapChainDescription desc;

        public ManagedSwapChain(RenderForm form)
        {
            desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(
                    Width,
                    Height,
                    new Rational(60, 1),
                    Format.R8G8B8A8_UNorm
                ),
                IsWindowed = false,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
            };
        }

        protected override void InitInternal()
        {

        }

        protected override void DisposeInternal()
        {

        }
    }
}
