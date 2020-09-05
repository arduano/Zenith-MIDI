using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public interface ITextureIdStruct
    {
        void SetTexId(int id);
    }

    public class MultiTextureShapeBuffer<T> : DeviceInitiable
        where T : struct, ITextureIdStruct
    {
        public DeviceContext Context { get; private set; } = null;

        T[] verts;
        int pos = 0;

        IBufferFlusher<T> flusher;
        ShaderResourceView[] resources;

        public MultiTextureShapeBuffer(int length, int textureCount, PrimitiveTopology topology, ShapePresets indicesType) : this(length, textureCount, topology, BufferFlusher<T>.IndicesFromPreset(indicesType)) { }
        public MultiTextureShapeBuffer(int length, int textureCount) : this(length, textureCount, PrimitiveTopology.TriangleList, ShapePresets.Quads) { }
        public MultiTextureShapeBuffer(int length, int textureCount, PrimitiveTopology topology) : this(length, textureCount, topology, null) { }
        public MultiTextureShapeBuffer(int length, int textureCount, PrimitiveTopology topology, int[] indices)
        {
            verts = new T[length];
            flusher = init.Add(new BufferFlusher<T>(length, topology, indices));
            resources = new ShaderResourceView[textureCount];
        }

        public MultiTextureShapeBuffer(IBufferFlusher<T> buffer, int textureCount)
        {
            verts = new T[buffer.Length];
            flusher = init.Add(buffer);
            resources = new ShaderResourceView[textureCount];
        }

        public void UseContext(DeviceContext context) => Context = context;

        void ClearResources()
        {
            for(int i = 0; i < resources.Length; i++)
            {
                resources[i] = null;
            }
        }

        public void Push(T vert, ShaderResourceView resource)
        {
            int t = 0;
            while (resources[t] != null && resources[t] != resource) t++;
            resources[t] = resource;
            vert.SetTexId(t);
            verts[pos++] = vert;
            if (pos >= verts.Length || t == resources.Length - 1)
            {
                Flush();
            }
        }

        public unsafe void Flush()
        {
            if (Context == null) throw new Exception("Device context was not initialised on this shape buffer");

            int count = 0;
            while (resources[count] != null) count++;
            if (count == 0) return;
            Context.PixelShader.SetShaderResources(0, count, resources);
            Context.VertexShader.SetShaderResources(0, count, resources);

            flusher.FlushArray(Context, verts, pos);

            pos = 0;
        }
    }
}
