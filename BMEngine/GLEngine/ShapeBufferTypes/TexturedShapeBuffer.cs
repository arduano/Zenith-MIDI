using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.GLEngine.Types;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;

namespace ZenithEngine.GLEngine
{
    public class TexturedShapeBuffer : ShapeBuffer<VertexTextured2d>
    {
        public static ShaderProgram GetBasicShader() => VertexTextured2d.GetBasicShader();

        public TexturedShapeBuffer(int length, ShapePresets preset)
            : base(length, ShapeTypes.Triangles, preset, new[] {
                new InputAssemblyPart(2, VertexAttribPointerType.Float, 0),
                new InputAssemblyPart(2, VertexAttribPointerType.Float, 8),
                new InputAssemblyPart(4, VertexAttribPointerType.Float, 16),
            })
        { }

        public void PushVertex(float x, float y, float u, float v, Color4 col)
        {
            PushVertex(new VertexTextured2d(x, y, u, v, col));
        }

        public void PushQuad(float left, float top, float right, float bottom, Color4 col)
        {
            PushVertex(left, top, 0, 0, Color4.White);
            PushVertex(right, top, 1, 0, Color4.White);
            PushVertex(right, bottom, 1, 1, Color4.White);
            PushVertex(left, bottom, 0, 1, Color4.White);
        }

        public void PushQuad(float left, float top, float right, float bottom)
        {
            PushQuad(left, top, right, bottom, Color4.White);
        }

        public void PushQuad(Vector2 topLeft, Vector2 bottomRight, Color4 col)
        {
            PushVertex(topLeft.X, topLeft.Y, 0, 0, col);
            PushVertex(bottomRight.X, topLeft.Y, 1, 0, col);
            PushVertex(bottomRight.X, bottomRight.Y, 1, 1, col);
            PushVertex(topLeft.X, bottomRight.Y, 0, 1, col);
        }

        public void PushQuad(Vector2 topLeft, Vector2 bottomRight)
        {
            PushQuad(topLeft, bottomRight, Color4.White);
        }

        public void PushVertex(Vector2 pos, Vector2 uv, Color4 col)
        {
            PushVertex(new VertexTextured2d(pos, uv, col));
        }
    }
}
