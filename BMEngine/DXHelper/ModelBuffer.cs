using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D11;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;

namespace ZenithEngine.DXHelper
{
    public class ModelBuffer<T> : DeviceInitiable
        where T : struct
    {
        public static implicit operator VertexBufferBinding(ModelBuffer<T> model) => new VertexBufferBinding(model.Buffer, model.structSize, 0);

        T[] verts;

        int[] indices = null;

        int structSize;

        public PrimitiveTopology Topology { get; private set; } = PrimitiveTopology.TriangleList;

        public Buffer Buffer { get; private set; }
        public Buffer IndexBuffer { get; private set; }

        public ModelBuffer(T[] verts) : this(verts, null) { }
        public ModelBuffer(T[] verts, int[] indices)
        {
            this.verts = verts;
            this.indices = indices;
            structSize = Utilities.SizeOf<T>();
        }

        protected override void InitInternal()
        {
            Buffer = Buffer.Create(Device, BindFlags.VertexBuffer, verts);
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

        public void Bind(DeviceContext context, int slot = 0)
        {
            context.InputAssembler.SetVertexBuffers(slot, this);
            if (IndexBuffer != null)
            {
                context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
            }
        }

        public void BindAndDraw(DeviceContext context)
        {
            Bind(context);
            context.InputAssembler.PrimitiveTopology = Topology;
            if (IndexBuffer != null)
            {
                context.DrawIndexed(indices.Length, 0, 0);
            }
            else
            {
                context.Draw(verts.Length, 0);
            }
        }
    }
}
