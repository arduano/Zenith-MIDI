using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public interface IDeviceInitiable : IDisposable
    {
        public void Init(DeviceGroup device);
    }
}
