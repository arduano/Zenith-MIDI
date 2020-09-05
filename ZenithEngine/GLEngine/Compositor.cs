using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.GLEngine.Types;

namespace ZenithEngine.GLEngine
{
    public class Compositor : IDisposable
    {
        TexturedShapeBuffer verts = new TexturedShapeBuffer(4, ShapePresets.Quads);

        public static ShaderProgram GetDefaultShader => VertexTextured2d.GetBasicShader();

        public Compositor() { }


        public void Composite(RenderSurface input1, RenderSurface input2, RenderSurface input3, ShaderProgram shader, RenderSurface output, bool clear = true)
        {
            input3.BindTexture(2);
            Composite(input1, input2, shader, output, clear);
        }
        public void Composite(RenderSurface input1, RenderSurface input2, ShaderProgram shader, RenderSurface output, bool clear = true)
        {
            input2.BindTexture(1);
            Composite(input1, shader, output, clear);
        }
        public void Composite(RenderSurface input1, ShaderProgram shader, RenderSurface output, bool clear = true)
        {
            input1.BindTexture(0);
            Composite(shader, output, clear);
        }

        void Composite(ShaderProgram shader, RenderSurface output, bool clear = true)
        {
            if (clear) output.BindSurfaceAndClear();
            else output.BindSurface();

            shader.Bind();

            verts.PushVertex(0, 0, 0, 0, Color4.White);
            verts.PushVertex(1, 0, 1, 0, Color4.White);
            verts.PushVertex(1, 1, 1, 1, Color4.White);
            verts.PushVertex(0, 1, 0, 1, Color4.White);
            verts.Flush();
        }

        public void Dispose()
        {
            verts.Dispose();
        }
    }
}
