using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Rectangle = System.Drawing.Rectangle;

namespace ZenithEngine.DXHelper
{
    public class RenderTexture : DeviceInitiable, ITextureResource
    {
        public static implicit operator Texture2D(RenderTexture texture) => texture.Texture;
        public static implicit operator ShaderResourceView(RenderTexture texture) => texture.TextureResource;

        public int Width { get; }
        public int Height { get; }
        public double AspectRatio => (double)Width / Height;

        byte[] internalBytes;

        public Texture2D Texture { get; private set; }
        public ShaderResourceView TextureResource { get; private set; }

        public RenderTexture(int width, int height, byte[] internalBytes)
        {
            Width = width;
            Height = height;
            this.internalBytes = internalBytes;
        }

        public static RenderTexture FromBitmap(Bitmap bitmap)
        {
            var locked = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var width = locked.Width;
            var height = locked.Height;
            var length = width * height;
            var bytes = new byte[length * 4];
            Marshal.Copy(locked.Scan0, bytes, 0, length * 4);
            bitmap.UnlockBits(locked);
            return new RenderTexture(width, height, bytes);
        }

        protected unsafe override void InitInternal()
        {
            fixed (byte* data = internalBytes)
            {
                Texture = dispose.Add(new Texture2D(Device, new Texture2DDescription()
                {
                    Width = Width,
                    Height = Height,
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    Usage = ResourceUsage.Immutable,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new DataRectangle((IntPtr)data, Width * 4)));
            }
            //TextureResource = dispose.Add(new ShaderResourceView(Device, Texture));
            TextureResource = dispose.Add(new ShaderResourceView(Device, Texture));
        }
    }
}
