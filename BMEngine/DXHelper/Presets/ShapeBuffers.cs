using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;

namespace ZenithEngine.DXHelper.Presets
{
    public class Flat2dShapeBuffer : ShapeBuffer<Vert2D>
    {
        public Flat2dShapeBuffer(int length, PrimitiveTopology topology, ShapePresets preset) : base(length, topology, preset) { }
        public Flat2dShapeBuffer(int length, PrimitiveTopology topology, int[] indices) : base(length, topology, indices) { }
        public Flat2dShapeBuffer(int length, PrimitiveTopology topology) : base(length, topology) { }

        public void Push(float x, float y, Color4 col) => Push(new Vert2D(new Vector2(x, y), col));
    }

    public class Textured2dShapeBuffer : ShapeBuffer<VertTex2D>
    {
        public Textured2dShapeBuffer(int length, PrimitiveTopology topology, ShapePresets preset) : base(length, topology, preset) { }
        public Textured2dShapeBuffer(int length, PrimitiveTopology topology, int[] indices) : base(length, topology, indices) { }
        public Textured2dShapeBuffer(int length, PrimitiveTopology topology) : base(length, topology) { }

        public void Push(float x, float y, float u, float v, Color4 col) => Push(new VertTex2D(new Vector2(x, y), new Vector2(u, v), col));
    }
}
