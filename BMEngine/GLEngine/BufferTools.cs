using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    public class AssemblyPart : Attribute
    {
        public InputAssemblyPart Part { get; }
        public int Order { get; }

        public AssemblyPart(int size, VertexAttribPointerType type, [CallerLineNumber]int order = 0)
        {
            Part = new InputAssemblyPart(size, type);
            Order = order;
        }
    }

    public struct InputAssemblyPart
    {
        public InputAssemblyPart(int size, VertexAttribPointerType type)
        {
            Size = size;
            Type = type;
        }

        public int Size { get; }
        public VertexAttribPointerType Type { get; }

        public int TypeSize
        {
            get
            {
                switch (Type)
                {
                    case VertexAttribPointerType.Byte: return 1;
                    case VertexAttribPointerType.Double: return 8;
                    case VertexAttribPointerType.Float: return 4;
                    case VertexAttribPointerType.Int: return 4;
                    case VertexAttribPointerType.Short: return 2;
                    case VertexAttribPointerType.UnsignedInt: return 4;
                    case VertexAttribPointerType.UnsignedShort: return 2;
                    case VertexAttribPointerType.UnsignedByte: return 1;
                    case VertexAttribPointerType.HalfFloat: return 2;
                    default: throw new Exception("Other vertex attribute types aren't implemented yet");
                }
            }
        }

        public int AttributeSize => TypeSize * Size;
    }

    public static class BufferTools
    {
        public static void BindBufferParts(InputAssemblyPart[] inputParts, int structByteSize) =>
            BindBufferParts(inputParts, structByteSize, 0);

        public static void BindBufferParts(InputAssemblyPart[] inputParts, int structByteSize, int start)
        {
            int i = start;
            int offset = 0;
            foreach (var part in inputParts)
            {
                GL.VertexAttribPointer(i++, part.Size, part.Type, false, structByteSize, offset);
                offset += part.AttributeSize;
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

        public static InputAssemblyPart[] GetAssemblyParts(Type type)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            members.AddRange(type.GetFields());
            members.AddRange(type.GetProperties());
            var parts = members.Where(p => Attribute.IsDefined(p, typeof(AssemblyPart)))
                .Select(p => (AssemblyPart)p.GetCustomAttributes(typeof(AssemblyPart), false).Single())
                .OrderBy(a => a.Order)
                .Select(a => a.Part)
                .ToArray();
            return parts;
        }
    }
}
