using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Threading.Tasks;
using ZenithEngine.GLEngine.Types;
using OpenTK.Graphics;
using OpenTK;

namespace ZenithEngine.GLEngine
{
    public class BasicShapeBuffer : ShapeBuffer<Vertex2d>
    {
        public static ShaderProgram GetBasicShader() => Vertex2d.GetBasicShader();

        public BasicShapeBuffer(int length, ShapePresets preset)
            : base(length, ShapeTypes.Triangles, preset, new[] {
                new InputAssemblyPart(2, VertexAttribPointerType.Float),
                new InputAssemblyPart(4, VertexAttribPointerType.Float),
            })
        { }

        public void PushVertex(float x, float y, Color4 col)
        {
            PushVertex(new Vertex2d(x, y, col));
        }

        public void PushVertex(Vector2 pos, Color4 col)
        {
            PushVertex(new Vertex2d(pos, col));
        }

        public void PushQuad(float left, float top, float right, float bottom, Color4 col)
        {
            PushVertex(left, top, col);
            PushVertex(right, top, col);
            PushVertex(right, bottom, col);
            PushVertex(left, bottom, col);
        }

        public void PushQuad(float left, float top, float right, float bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft)
        {
            PushVertex(left, top, topLeft);
            PushVertex(right, top, topRight);
            PushVertex(right, bottom, bottomRight);
            PushVertex(left, bottom, bottomLeft);
        }
    }
}
