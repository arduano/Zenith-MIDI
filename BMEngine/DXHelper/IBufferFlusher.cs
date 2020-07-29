using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public interface IBufferFlusher<T> : IDeviceInitiable
        where T : struct
    {
        public int Length { get; }
        public void FlushArray(DeviceContext context, T[] verts, int count);
    }
}
