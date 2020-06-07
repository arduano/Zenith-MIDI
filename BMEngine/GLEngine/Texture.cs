using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public class Texture : IDisposable
    {
        int texId = -1;

        void SetParams(TextureMinFilter filterMin, TextureMagFilter filterMag, TextureWrapMode edge)
        {
            Bind();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filterMin);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filterMag);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)edge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)edge);
        }

        public void LoadBitmap(Bitmap image)
        {
            Bind();

            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            image.UnlockBits(data);
        }

        public void LoadBitmap(string path)
        {
            var img = new Bitmap(path);
            LoadBitmap(img);
            img.Dispose();
        }

        public Texture()
        {
            texId = GL.GenTexture();
        }

        public Texture(Bitmap img, TextureMinFilter filterMin, TextureMagFilter filterMag, TextureWrapMode edge) : this()
        {
            LoadBitmap(img);
            SetParams(filterMin, filterMag, edge);
        }

        public Texture(string img, TextureMinFilter filterMin, TextureMagFilter filterMag, TextureWrapMode edge) : this()
        {
            LoadBitmap(img);
            SetParams(filterMin, filterMag, edge);
        }

        public Texture(Bitmap img) : this(img, TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Repeat) { }
        public Texture(string img) : this(img, TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Repeat) { }

        public Texture(Bitmap img, TextureMinFilter filterMin, TextureMagFilter filterMag) : this(img, filterMin, filterMag, TextureWrapMode.Repeat) { }
        public Texture(string img, TextureMinFilter filterMin, TextureMagFilter filterMag) : this(img, filterMin, filterMag, TextureWrapMode.Repeat) { }

        public Texture(Bitmap img, TextureWrapMode edge) : this(img, TextureMinFilter.Linear, TextureMagFilter.Linear, edge) { }
        public Texture(string img, TextureWrapMode edge) : this(img, TextureMinFilter.Linear, TextureMagFilter.Linear, edge) { }

        public void Bind()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texId);
        }

        public void Bind(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, texId);
        }

        public void Bind(int unit)
        {
            Bind(TextureUnit.Texture0 + unit);
        }

        public void Dispose()
        {
            if (texId != -1)
            {
                GL.DeleteTexture(texId);
                texId = -1;
            }
        }
    }
}
