using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ZenithEngine.DXHelper
{
    public class InstancedBufferFlusher<TM, TI> : BufferFlusher<TI>
        where TM : struct
        where TI : struct
    {
        public ModelBuffer<TM> Model { get; private set; }

        public InstancedBufferFlusher(int length, ModelBuffer<TM> model) : base(length, model.Topology, null)
        {
            Model = init.Add(model);
        }

        //public void UseModel(ModelBuffer<TM> model)
        //{
        //    Model = model;
        //}

        public override void FlushArray(DeviceContext context, TI[] verts, int count)
        {
            DataStream data;
            context.MapSubresource(Buffer, 0, MapMode.WriteDiscard, MapFlags.None, out data);
            data.Position = 0;
            data.WriteRange(verts, 0, count);
            context.UnmapSubresource(Buffer, 0);
            context.InputAssembler.PrimitiveTopology = Model.Topology;
            context.InputAssembler.SetVertexBuffers(0, Model, new VertexBufferBinding(Buffer, structByteSize, 0));
            if (Model.IndexBuffer != null)
            {
                context.InputAssembler.SetIndexBuffer(Model.IndexBuffer, Format.R32_UInt, 0);
                context.DrawIndexedInstanced(Model.IndexCount, count, 0, 0, 0);
            }
            else
            {
                context.DrawInstanced(Model.VertCount, count, 0, 0);
            }
            data.Dispose();
        }
    }
}
