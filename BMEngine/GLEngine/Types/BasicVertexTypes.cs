using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine.Types
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Vertex2d
    {
        public static ShaderProgram GetBasicShader() =>
            ShaderProgram.Presets.Basic();

        [AssemblyPart(2, VertexAttribPointerType.Float)]
        public Vector2 Pos;

        [AssemblyPart(4, VertexAttribPointerType.Float)]
        public Color4 Col;

        public Vertex2d(Vector2 pos, Color4 color)
        {
            Pos = pos;
            Col = color;
        }

        public Vertex2d(float x, float y, Color4 color) : this(new Vector2(x, y), color)
        { }
    }


    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct VertexTextured2d
    {
        public static ShaderProgram GetBasicShader() =>
             ShaderProgram.Presets.BasicTextured();

        [AssemblyPart(2, VertexAttribPointerType.Float)]
        public Vector2 Pos;

        [AssemblyPart(2, VertexAttribPointerType.Float)]
        public Vector2 UV;

        [AssemblyPart(4, VertexAttribPointerType.Float)]
        public Color4 Color;

        public VertexTextured2d(Vector2 pos, Vector2 uv, Color4 color)
        {
            Pos = pos;
            Color = color;
            UV = uv;
        }

        public VertexTextured2d(float x, float y, float u, float v, Color4 color) : this(new Vector2(x, y), new Vector2(u, v), color)
        { }
    }
}
