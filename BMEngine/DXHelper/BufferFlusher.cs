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

    public class BufferFlusher<T> : DeviceInitiable
        where T : struct
    {
        public Buffer Buffer { get; private set; }
        public Buffer IndexBuffer { get; private set; }

        public PrimitiveTopology Topology { get; private set; } = PrimitiveTopology.TriangleList;

        int[] indices;
        int indicesPerShape;
        protected int vertsCount;

        int structByteSize;

        int vertsPerShape = 1;

        protected static int[] IndicesFromPreset(ShapePresets preset)
        {
            if (preset == ShapePresets.Sequential)
                return null;
            if (preset == ShapePresets.Quads)
                return new[] { 0, 1, 2, 0, 2, 3 };
            throw new Exception("Unknown preset");
        }

        public BufferFlusher(int length, PrimitiveTopology topology, ShapePresets indicesType) : this(length, topology, IndicesFromPreset(indicesType)) { }
        public BufferFlusher(int length) : this(length, PrimitiveTopology.TriangleList, ShapePresets.Quads) { }
        public BufferFlusher(int length, PrimitiveTopology topology) : this(length, topology, null) { }
        public BufferFlusher(int length, PrimitiveTopology topology, int[] indices)
        {
            structByteSize = Utilities.SizeOf<T>();
            vertsCount = length;

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
                SizeInBytes = structByteSize * vertsCount,
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
        }

        protected void FlushArray(DeviceContext context, T[] verts, int count)
        {
            if (count == 0) return;
            if (count % vertsPerShape != 0) 
                throw new Exception("Incomplete shapes");

            DataStream data;
            context.MapSubresource(Buffer, 0, MapMode.WriteDiscard, MapFlags.None, out data);
            data.Position = 0;
            data.WriteRange(verts, 0, count);
            context.UnmapSubresource(Buffer, 0);
            context.InputAssembler.PrimitiveTopology = Topology;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(Buffer, structByteSize, 0));
            if (IndexBuffer != null)
            {
                context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
                context.DrawIndexed(count / vertsPerShape * indicesPerShape, 0, 0);
            }
            else
            {
                context.Draw(count, 0);
            }
            data.Dispose();
        }
    }
}
