using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public class BasicRenderSurface : RenderSurface, IDisposable
    {
        public virtual int BufferID { get; private set; }
        public virtual int TextureID { get; private set; }
        public virtual int DepthBufferID { get; private set; } = -1;

        public BasicRenderSurface(int width, int height, bool depth)
        {
            int fbuffer = GL.GenFramebuffer();
            int rtexture = GL.GenTexture();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbuffer);
            GL.BindTexture(TextureTarget.Texture2D, rtexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.Float, (IntPtr)0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, rtexture, 0);

            if (depth)
            {
                int depthrenderbuffer = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthrenderbuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, width, height);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthrenderbuffer);
                DepthBufferID = depthrenderbuffer;
            }

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete) throw new Exception();
            Width = width;
            Height = height;
            BufferID = fbuffer;
            TextureID = rtexture;
        }

        public override void BindSurfaceNoViewport()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, BufferID);
        }

        public override void BindTextureNoSwitch()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureID);
        }

        public override void Dispose()
        {
            GL.DeleteFramebuffer(BufferID);
            GL.DeleteTexture(TextureID);
            if(DepthBufferID != -1) GL.DeleteTexture(DepthBufferID);
        }
    }
}
