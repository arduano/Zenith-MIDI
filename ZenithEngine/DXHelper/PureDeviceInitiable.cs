using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public class DeviceInitiable : PureDeviceInitiable
    {
        protected Initiator init { get; } = new Initiator();
        protected DisposeGroup dispose { get; private set; }

        public override void Dispose()
        {
            base.Dispose();
            dispose.Dispose();
        }

        public override void Init(DeviceGroup device)
        {
            dispose = new DisposeGroup();
            dispose.Add(init);
            init.Init(device);
            base.Init(device);
        }
    }
}
