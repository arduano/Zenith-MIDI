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
    public class ManagedRenderWindow : RenderForm, IRenderSurface, IDisposable
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

        public Texture2D Texture => backBuffer;
        public RenderTargetView RenderTarget => renderView;

        DisposeGroup dispose = new DisposeGroup();

        public new int Width
        {
            get => ClientSize.Width;
            set
            {
                ClientSize = new Size(value, ClientSize.Height);
            }
        }

        public new int Height
        {
            get => ClientSize.Height;
            set
            {
                ClientSize = new Size(ClientSize.Width, value);
            }
        }

        bool fullscreen = false;
        public bool Fullscreen
        {
            get => fullscreen;
            set
            {
                if (fullscreen == value) return;
                fullscreen = value;
                if (fullscreen)
                {
                    if (fullscreen)
                    {
                        BringToFront();
                        Focus();
                    }
                    else
                    {
                        WindowState = FormWindowState.Normal;
                    }
                }
                TestWindowState();
            }
        }

        void TestWindowState()
        {
            if (fullscreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                if (Focused)
                {
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    WindowState = FormWindowState.Minimized;
                }
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        public ManagedRenderWindow(int width, int height)
        {
            ClientSize = new Size(width, height);

            desc.ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
            desc.OutputHandle = Handle;

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
            dispose.Add(device);
            dispose.Add(swapChain);
            var context = device.ImmediateContext;

            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(Handle, WindowAssociationFlags.IgnoreAll);
            dispose.Add(factory);

            CheckResize();
            UserResized += (sender, args) => hasResized = true;

            GotFocus += (s, e) => TestWindowState();
            LostFocus += (s, e) => TestWindowState();
        }

        void CheckResize()
        {
            if (hasResized)
            {
                Utilities.Dispose(ref backBuffer);
                Utilities.Dispose(ref renderView);

                swapChain.ResizeBuffers(desc.BufferCount, ClientSize.Width, ClientSize.Height, Format.Unknown, SwapChainFlags.None);

                dispose.Replace(ref backBuffer, Texture2D.FromSwapChain<Texture2D>(swapChain, 0));
                dispose.Replace(ref renderView, new RenderTargetView(device, backBuffer));

                hasResized = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            dispose.Dispose();
        }

        public void Present(bool vsync)
        {
            swapChain.Present(vsync ? 1 : 0, PresentFlags.None);
            CheckResize();
        }
    }
}
