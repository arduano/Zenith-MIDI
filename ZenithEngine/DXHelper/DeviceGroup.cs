using SharpDX.Direct3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Direct3D11 = SharpDX.Direct3D11;
using Direct2D1 = SharpDX.Direct2D1;
using DirectWrite = SharpDX.DirectWrite;

namespace ZenithEngine.DXHelper
{
    public class DeviceGroup : IDisposable
    {
        public static implicit operator Direct3D11.Device(DeviceGroup dev) => dev.D3Device;
        public static implicit operator Direct2D1.Factory(DeviceGroup dev) => dev.D2Factory;
        public static implicit operator DirectWrite.Factory(DeviceGroup dev) => dev.DWFactory;

        public Direct3D11.Device D3Device { get; }
        public Direct2D1.Factory D2Factory { get; }
        public DirectWrite.Factory DWFactory { get; }

        public DeviceGroup()
        {
            D3Device = new Direct3D11.Device(DriverType.Hardware, 
                Direct3D11.DeviceCreationFlags.BgraSupport | 
                Direct3D11.DeviceCreationFlags.DisableGpuTimeout);
            D2Factory = new Direct2D1.Factory();
            DWFactory = new DirectWrite.Factory();
        }

        public void Dispose()
        {
            try
            {
                D3Device.ImmediateContext.ClearState();
                D3Device.ImmediateContext.Flush();
            }
            catch { }
            try
            {
                D3Device.Dispose();
            }
            catch { }
            try
            {
                D2Factory.Dispose();
            }
            catch { }
            try
            {
                DWFactory.Dispose();
            }
            catch { }
        }
    }
}
