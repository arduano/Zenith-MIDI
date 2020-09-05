using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public abstract class PureDeviceInitiable : IDeviceInitiable
    {
        public bool Initialized { get; private set; } = false;
        public Device Device { get; private set; } = null;

        protected virtual void InitInternal() { }
        protected virtual void DisposeInternal() { }

        public virtual void Dispose()
        {
            if (!Initialized) return;
            DisposeInternal();
            Device = null;
            Initialized = false;
        }

        public virtual void Init(Device device)
        {
            if (Initialized) return;
            Device = device;
            InitInternal();
            Initialized = true;
        }
    }
}
