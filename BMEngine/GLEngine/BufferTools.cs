using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public enum ShapeTypes
    {
        Points,
        Lines,
        Triangles
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

    public static class BufferTools
    {
        public static void BindBufferParts(InputAssemblyPart[] inputParts, int structByteSize)
        {
            int i = 0;
            foreach (var part in inputParts)
            {
                GL.VertexAttribPointer(i++, part.Size, part.Type, false, structByteSize, part.Offset);
            }
        }

        public static void BindData<T>(T[] data, int length, int structByteSize)
            where T : struct
        {
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(length * structByteSize),
                data,
                BufferUsageHint.DynamicDraw);
        }

        public static void BindIndices<T>(T[] data, int length, int structByteSize)
            where T : struct
        {
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(length * structByteSize),
                data,
                BufferUsageHint.StaticDraw);
        }

        public static PrimitiveType ShapeType(ShapeTypes type)
        {
            if (type == ShapeTypes.Lines) return PrimitiveType.Lines;
            if (type == ShapeTypes.Points) return PrimitiveType.Points;
            if (type == ShapeTypes.Triangles) return PrimitiveType.Triangles;
            throw new Exception();
        }
    }
}
