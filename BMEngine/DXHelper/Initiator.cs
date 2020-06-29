using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public class Initiator : DeviceInitiable
    {
        List<IDeviceInitiable> items = new List<IDeviceInitiable>();

        public T Add<T>(T item) 
            where T : IDeviceInitiable
        {
            items.Add(item);
            return item;
        }

        protected override void DisposeInternal()
        {
            foreach (var i in items) i.Dispose();
        }

        protected override void InitInternal()
        {
            foreach (var i in items) i.Init(Device);
        }
    }
}
