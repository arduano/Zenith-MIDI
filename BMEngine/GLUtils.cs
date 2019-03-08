using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace BMEngine
{
    public static class GLUtils
    {
        public static int MakeShaderProgram(string name)
        {
            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
            int statusCode;
            string info;

            GL.ShaderSource(_vertexObj, File.ReadAllText("Shaders\\" + name + ".vert"));
            GL.CompileShader(_vertexObj);
            info = GL.GetShaderInfoLog(_vertexObj);
            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            GL.ShaderSource(_fragObj, File.ReadAllText("Shaders\\" + name + ".frag"));
            GL.CompileShader(_fragObj);
            info = GL.GetShaderInfoLog(_fragObj);
            GL.GetShader(_fragObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            int shader = GL.CreateProgram();
            GL.AttachShader(shader, _fragObj);
            GL.AttachShader(shader, _vertexObj);
            GL.LinkProgram(shader);
            return shader;
        }

        public static int MakePostShaderProgram(string name)
        {
            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
            int statusCode;
            string info;

            GL.ShaderSource(_vertexObj, File.ReadAllText("Shaders\\Post\\post.vert"));
            GL.CompileShader(_vertexObj);
            info = GL.GetShaderInfoLog(_vertexObj);
            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            GL.ShaderSource(_fragObj, File.ReadAllText("Shaders\\Post\\" + name + ".frag"));
            GL.CompileShader(_fragObj);
            info = GL.GetShaderInfoLog(_fragObj);
            GL.GetShader(_fragObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            int shader = GL.CreateProgram();
            GL.AttachShader(shader, _fragObj);
            GL.AttachShader(shader, _vertexObj);
            GL.LinkProgram(shader);
            return shader;
        }

        public static void GenFrameBufferTexture(int width, int height, out int fbuffer, out int rtexture, bool depth = false)
        {
            fbuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbuffer);
            rtexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, rtexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.Byte, (IntPtr)0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            if (depth)
            {
                int depthrenderbuffer;
                depthrenderbuffer = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthrenderbuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, width, height);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthrenderbuffer);
            }

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, rtexture, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete) throw new Exception();
        }
    }
}
