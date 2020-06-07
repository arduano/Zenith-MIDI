using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;

namespace ZenithEngine.GLEngine.Types
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Vertex2d
    {
        public static ShaderProgram GetBasicShader() =>
            ShaderProgram.Presets.Basic();

        Vector2 pos;
        Color4 color;

        public Vertex2d(Vector2 pos, Color4 color)
        {
            this.pos = pos;
            this.color = color;
        }

        public Vertex2d(float x, float y, Color4 color) : this(new Vector2(x, y), color)
        { }
    }


    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct VertexTextured2d
    {
        public static ShaderProgram GetBasicShader() =>
             ShaderProgram.Presets.BasicTextured();

        Vector2 pos;
        Vector2 uv;
        Color4 color;

        public VertexTextured2d(Vector2 pos, Vector2 uv, Color4 color)
        {
            this.pos = pos;
            this.color = color;
            this.uv = uv;
        }

        public VertexTextured2d(float x, float y, float u, float v, Color4 color) : this(new Vector2(x, y), new Vector2(u, v), color)
        { }
    }
}
