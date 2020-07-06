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
        
        public T Replace<T>(IDeviceInitiable prevItem, T newItem) where T : IDeviceInitiable
        {
            if (!items.Contains(prevItem)) throw new ArgumentException("Previous item not found in items array");
            items.Remove(prevItem);
            items.Add(newItem);

            if (Initialized)
            {
                prevItem.Dispose();
                newItem.Init(Device);
            }

            return newItem;
        }
    }
}
