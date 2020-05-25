using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.GLEngine
{
    public enum ShapeTypes
    {
        Points,
        Lines,
        Triangles
    }

    public enum ShapePresets
    {
        Points,
        Lines,
        Triangles,
        Quads,
    }

    public struct InputAssemblyPart
    {
        public InputAssemblyPart(int size, VertexAttribPointerType type, int offset)
        {
            Size = size;
            Type = type;
            Offset = offset;
        }

        public int Size { get; }
        public VertexAttribPointerType Type { get; }
        public int Offset { get; }
    }

    public class ShapeBuffer<T> : IDisposable
        where T : struct
    {
        T[] verts;
        public ShapeTypes Shape { get; }
        int vertsPerShape;
        int indicesPerShape;

        int pos = 0;

        int bufferId;
        int indexBufferId;

        int structByteSize;

        InputAssemblyPart[] inputParts;

        static int[] IndicesFromPreset(ShapePresets preset)
        {
            if (preset == ShapePresets.Points)
                return new[] { 0 };
            if (preset == ShapePresets.Lines)
                return new[] { 0, 1 };
            if (preset == ShapePresets.Triangles)
                return new[] { 0, 1, 2 };
            if (preset == ShapePresets.Quads)
                return new[] { 0, 1, 3, 1, 3, 2 };
            throw new Exception("Unknown preset");
        }

        public ShapeBuffer(int length, ShapeTypes type, ShapePresets preset, InputAssemblyPart[] inputParts)
            : this(length, type, IndicesFromPreset(preset), inputParts) { }

        public ShapeBuffer(int length, ShapeTypes type, int[] indices, InputAssemblyPart[] inputParts)
        {
            structByteSize = Marshal.SizeOf(default(T));
            if (indices.Length == 0) throw new Exception("Indices must be longer than zero");
            if (indices.Min() != 0) throw new Exception("Smallest index must be zero");
            var max = indices.Max();
            if (max < 0) throw new Exception("Biggest index can't be smaller than 0");
            vertsPerShape = max + 1;
            indicesPerShape = indices.Length;
            var indexArray = new int[length * indicesPerShape];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < indicesPerShape; j++)
                {
                    indexArray[i * indicesPerShape + j] = i * vertsPerShape + indices[j];
                }
            }

            bufferId = GL.GenBuffer();
            indexBufferId = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexArray.Length * 4),
                indexArray,
                BufferUsageHint.StaticDraw);

            this.inputParts = inputParts;

            verts = new T[length];
        }

        public void PushVertex(T vert)
        {
            verts[pos++] = vert;
            if (pos >= verts.Length)
            {
                Flush();
            }
        }

        public unsafe void Flush()
        {
            if (pos == 0) return;
            if (pos % vertsPerShape != 0) throw new Exception("Incomplete shapes");
            using (new GLEnabler().UseAttribArrays(inputParts.Length))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, bufferId);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(pos * structByteSize),
                    verts,
                    BufferUsageHint.DynamicDraw);
                int i = 0;
                foreach (var part in inputParts)
                {
                    GL.VertexAttribPointer(i++, part.Size, part.Type, false, structByteSize, part.Offset);
                }
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                GL.IndexPointer(IndexPointerType.Int, 1, 0);
                GL.DrawElements(PrimitiveType.Triangles, pos * indicesPerShape, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }
            pos = 0;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(bufferId);
            GL.DeleteBuffer(indexBufferId);
        }
    }
}
