using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public class RenderSurface : IDisposable
    {
        public int BufferID { get; private set; }
        public int TextureID { get; private set; }

        public int Width { get; }
        public int Height { get; }

        private RenderSurface(int buffer, int texture, int width, int height)
        {
            BufferID = buffer;
            TextureID = texture;
            Width = width;
            Height = height;
        }

        public static RenderSurface BasicFrame(int width, int height)
        {
            int fbuffer = GL.GenFramebuffer();
            int rtexture = GL.GenTexture();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbuffer);
            GL.BindTexture(TextureTarget.Texture2D, rtexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.Byte, (IntPtr)0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, rtexture, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete) throw new Exception();
            return new RenderSurface(fbuffer, rtexture, width, height);
        }

        public void BindSurface()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, BufferID);
            GL.Viewport(0, 0, Width, Height);
        }

        public void BindSurfaceAndClear()
        {
            BindSurface();
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void BindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, TextureID);
        }

        public static void UnbindBuffers()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public static void UnbindTextures()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Dispose()
        {
            GL.DeleteFramebuffer(BufferID);
            GL.DeleteTexture(TextureID);
        }
    }
}
