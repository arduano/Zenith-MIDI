using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public abstract class DeviceInitiable : IDeviceInitiable
    {
        public bool Initialized { get; private set; } = false;
        public Device Device { get; private set; } = null;

        protected abstract void InitInternal();
        protected abstract void DisposeInternal();

        public void Dispose()
        {
            if (!Initialized) return;
            DisposeInternal();
            Device = null;
            Initialized = false;
        }

        public void Init(Device device)
        {
            if (Initialized) return;
            Device = device;
            InitInternal();
            Initialized = true;
        }
    }
}
