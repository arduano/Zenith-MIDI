using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMEngine;
using OpenTK.Graphics.OpenGL;

namespace Black_Midi_Render
{
    class GLPostbuffer: IDisposable
    {
        public int BufferID { get; private set; }
        public int TextureID { get; private set; }

        public GLPostbuffer(int width, int height)
        {
            int b, t;
            GLUtils.GenFrameBufferTexture(width, height, out b, out t);
            BufferID = b;
            TextureID = t;
        }

        public void BindBuffer()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, BufferID);
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
