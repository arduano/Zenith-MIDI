﻿using System;
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
                new InputAssemblyPart(2, VertexAttribPointerType.Float, 0),
                new InputAssemblyPart(4, VertexAttribPointerType.Float, 8),
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
    }
}