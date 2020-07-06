using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ZenithEngine.DXHelper
{
    class ManagedRenderWindow : RenderForm
    {
        private bool hasResized = true;

        SwapChainDescription desc = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription =
                new ModeDescription(1280, 720,
                                    new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = IntPtr.Zero,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };

        Device device;
        SwapChain swapChain;

        Texture2D backBuffer = null;
        RenderTargetView renderView = null;

        public Device Device => device;

        public ManagedRenderWindow(int width, int height)
        {
            ClientSize = new Size(width, height);

            desc.ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
            desc.OutputHandle = Handle;

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
            var context = device.ImmediateContext;

            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(Handle, WindowAssociationFlags.IgnoreAll);

            UserResized += (sender, args) => hasResized = true;
        }

        void CheckResize()
        {
            if (hasResized)
            {
                Utilities.Dispose(ref backBuffer);
                Utilities.Dispose(ref renderView);

                swapChain.ResizeBuffers(desc.BufferCount, ClientSize.Width, ClientSize.Height, Format.Unknown, SwapChainFlags.None);

                backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);

                renderView = new RenderTargetView(device, backBuffer);

                hasResized = false;
            }
        }

        public void Present(bool vsync)
        {
            swapChain.Present(vsync ? 1 : 0, PresentFlags.None);
            CheckResize();
        }
    }
}
