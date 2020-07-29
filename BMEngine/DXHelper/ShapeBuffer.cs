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
    public class ShapeBuffer<T> : DeviceInitiable
        where T : struct
    {
        public DeviceContext Context { get; private set; } = null;

        T[] verts;
        int pos = 0;

        IBufferFlusher<T> flusher;

        public ShapeBuffer(int length, PrimitiveTopology topology, ShapePresets indicesType) : this(length, topology, BufferFlusher<T>.IndicesFromPreset(indicesType)) { }
        public ShapeBuffer(int length) : this(length, PrimitiveTopology.TriangleList, ShapePresets.Quads) { }
        public ShapeBuffer(int length, PrimitiveTopology topology) : this(length, topology, null) { }
        public ShapeBuffer(int length, PrimitiveTopology topology, int[] indices)
        {
            verts = new T[length];
            flusher = init.Add(new BufferFlusher<T>(length, topology, indices));
        }

        public ShapeBuffer(IBufferFlusher<T> buffer)
        {
            verts = new T[buffer.Length];
            flusher = init.Add(buffer);
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

            flusher.FlushArray(Context, verts, pos);

            pos = 0;
        }
    }
}
