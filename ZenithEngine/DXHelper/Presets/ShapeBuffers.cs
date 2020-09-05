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
        public Flat2dShapeBuffer(int length) : base(length) { }

        public void Push(float x, float y, Color4 col) => Push(new Vert2D(new Vector2(x, y), col));
        public void Push(Vector2 pos, Color4 col) => Push(new Vert2D(pos, col));

        public void PushQuad(float left, float top, float right, float bottom, Color4 col)
        {
            Push(left, top, col);
            Push(right, top, col);
            Push(right, bottom, col);
            Push(left, bottom, col);
        }

        public void PushQuad(float left, float top, float right, float bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft)
        {
            Push(left, top, topLeft);
            Push(right, top, topRight);
            Push(right, bottom, bottomRight);
            Push(left, bottom, bottomLeft);
        }

        public void PushQuad(Vector2 topLeft, Vector2 bottomRight, Color4 col)
        {
            Push(topLeft.X, topLeft.Y, col);
            Push(bottomRight.X, topLeft.Y, col);
            Push(bottomRight.X, bottomRight.Y, col);
            Push(topLeft.X, bottomRight.Y, col);
        }
    }

    public class Textured2dShapeBuffer : ShapeBuffer<VertTex2D>
    {
        public Textured2dShapeBuffer(int length, PrimitiveTopology topology, ShapePresets preset) : base(length, topology, preset) { }
        public Textured2dShapeBuffer(int length, PrimitiveTopology topology, int[] indices) : base(length, topology, indices) { }
        public Textured2dShapeBuffer(int length, PrimitiveTopology topology) : base(length, topology) { }
        public Textured2dShapeBuffer(int length) : base(length) { }

        public void Push(float x, float y, float u, float v, Color4 col) => Push(new VertTex2D(new Vector2(x, y), new Vector2(u, v), col));
        public void Push(Vector2 pos, Vector2 uv, Color4 col) => Push(new VertTex2D(pos, uv, col));

        public void PushQuad(float left, float top, float right, float bottom, Color4 col)
        {
            Push(left, top, 0, 0, Color4.White);
            Push(right, top, 1, 0, Color4.White);
            Push(right, bottom, 1, 1, Color4.White);
            Push(left, bottom, 0, 1, Color4.White);
        }

        public void PushQuad(float left, float top, float right, float bottom)
        {
            PushQuad(left, top, right, bottom, Color4.White);
        }

        public void PushQuad(Vector2 topLeft, Vector2 bottomRight, Color4 col)
        {
            Push(topLeft.X, topLeft.Y, 0, 0, col);
            Push(bottomRight.X, topLeft.Y, 1, 0, col);
            Push(bottomRight.X, bottomRight.Y, 1, 1, col);
            Push(topLeft.X, bottomRight.Y, 0, 1, col);
        }

        public void PushQuad(Vector2 topLeft, Vector2 bottomRight)
        {
            PushQuad(topLeft, bottomRight, Color4.White);
        }
    }
}
