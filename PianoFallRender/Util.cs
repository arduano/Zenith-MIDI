using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace PianoFallRender
{
    class Util : IDisposable
    {
        #region Shaders
        string postShaderVert = @"#version 330 compatibility

in vec3 position;
out vec2 UV;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, position.z * 2 - 1, 1.0f);
	//color = glColor;
    UV = vec2(position.x, position.y);
}
";
        string postShaderFrag = @"#version 330 compatibility

in vec2 UV;

out vec4 color;

uniform sampler2D myTextureSampler;

void main()
{
    color = texture2D( myTextureSampler, UV );
}
";

        int MakeShader(string vert, string frag)
        {
            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
            int statusCode;
            string info;

            GL.ShaderSource(_vertexObj, vert);
            GL.CompileShader(_vertexObj);
            info = GL.GetShaderInfoLog(_vertexObj);
            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            GL.ShaderSource(_fragObj, frag);
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
        #endregion
        
        int postShader;

        int screenQuadBuffer;
        int screenQuadIndexBuffer;
        double[] screenQuadArray = new double[] { 0, 0, 0, 1, 1, 1, 1, 0 };
        int[] screenQuadArrayIndex = new int[] { 0, 1, 2, 3 };

        public Util()
        {
            GL.GenBuffers(1, out screenQuadBuffer);
            GL.GenBuffers(1, out screenQuadIndexBuffer);

            GL.BindBuffer(BufferTarget.ArrayBuffer, screenQuadBuffer);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(screenQuadArray.Length * 8),
                screenQuadArray,
                BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenQuadIndexBuffer);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(screenQuadArrayIndex.Length * 4),
                screenQuadArrayIndex,
                BufferUsageHint.StaticDraw);

            postShader = MakeShader(postShaderVert, postShaderFrag);
        }

        public void DrawScreenQuad()
        {
            GL.UseProgram(postShader);
            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.Enable(EnableCap.Texture2D);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindBuffer(BufferTarget.ArrayBuffer, screenQuadBuffer);
            GL.VertexPointer(2, VertexPointerType.Double, 16, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenQuadIndexBuffer);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Quads, 4, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.Disable(EnableCap.Texture2D);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(2, new int[] { screenQuadBuffer, screenQuadIndexBuffer });
        }
    }
}
