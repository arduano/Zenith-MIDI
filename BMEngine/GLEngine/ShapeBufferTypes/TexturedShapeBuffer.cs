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

        public void PushVertex(Vector2 pos, Vector2 uv, Color4 col)
        {
            PushVertex(new VertexTextured2d(pos, uv, col));
        }
    }
}
