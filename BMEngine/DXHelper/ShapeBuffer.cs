using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ZenithEngine.DXHelper
{
    public enum ShapePresets
    {
        Sequential,
        Quads,
    }

    public class ShapeBuffer<T> : DeviceInitiable
        where T : struct
    {
        public Buffer Buffer { get; private set; }
        public Buffer IndexBuffer { get; private set; }

        public PrimitiveTopology Topology { get; private set; } = PrimitiveTopology.TriangleList;

        public DeviceContext Context { get; private set; } = null;

        int[] indices;
        int indicesPerShape;

        T[] verts;
        int structByteSize;
        int pos = 0;

        int vertsPerShape = 1;

        static int[] IndicesFromPreset(ShapePresets preset)
        {
            if (preset == ShapePresets.Sequential)
                return null;
            if (preset == ShapePresets.Quads)
                return new[] { 0, 1, 2, 0, 2, 3 };
            throw new Exception("Unknown preset");
        }

        public ShapeBuffer(int length, PrimitiveTopology topology, ShapePresets indicesType) : this(length, topology, IndicesFromPreset(indicesType)) { }
        public ShapeBuffer(int length) : this(length, PrimitiveTopology.TriangleList, ShapePresets.Quads) { }
        public ShapeBuffer(int length, PrimitiveTopology topology) : this(length, topology, null) { }
        public ShapeBuffer(int length, PrimitiveTopology topology, int[] indices)
        {
            structByteSize = Utilities.SizeOf<T>();
            verts = new T[length];

            Topology = topology;

            if (indices != null)
            {
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

                this.indices = indexArray;
            }
        }

        protected override void InitInternal()
        {
            Buffer = new Buffer(Device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = structByteSize * verts.Length,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });
            if (indices != null)
            {
                IndexBuffer = Buffer.Create(Device, BindFlags.IndexBuffer, indices);
            }
        }

        protected override void DisposeInternal()
        {
            Buffer.Dispose();
            IndexBuffer?.Dispose();
            Context = null;
        }

        public void UseContext(DeviceContext context) => Context = context;

        public void Push(T vert)
        {
            verts[pos++] = vert;
            if (pos >= verts.Length)
            {
                Flush();
            }
        }

        public unsafe void Flush()
        {
            if (Context == null) throw new Exception("Device context was not initialised on this shape buffer");

            if (pos == 0) return;
            if (pos % vertsPerShape != 0) throw new Exception("Incomplete shapes");

            DataStream data;
            Context.MapSubresource(Buffer, 0, MapMode.WriteDiscard, MapFlags.None, out data);
            data.Position = 0;
            data.WriteRange(verts, 0, pos);
            Context.UnmapSubresource(Buffer, 0);
            Context.InputAssembler.PrimitiveTopology = Topology;
            Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(Buffer, structByteSize, 0));
            if (IndexBuffer != null)
            {
                Context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
                Context.DrawIndexed(pos / vertsPerShape * indicesPerShape, 0, 0);
            }
            else
            {
                Context.Draw(pos, 0);
            }
            data.Dispose();

            pos = 0;
        }
    }
}
