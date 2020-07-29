using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace ZenithEngine.DXHelper
{
    public enum DepthStencilPresets
    {
        None,
        Basic,
        Always
    }

    public class DepthStencilStateKeeper : DeviceInitiable
    {
        public static implicit operator DepthStencilState(DepthStencilStateKeeper keeper) => keeper.DepthStencilState;

        public DepthStencilState DepthStencilState { get; private set; }
        public DepthStencilStateDescription Description;

        public static DepthStencilStateDescription BasicStateDescription()
        {
            return DepthStencilStateDescription.Default();
        }

        public static DepthStencilStateDescription NoDepthStateDescription()
        {
            var desc = DepthStencilStateDescription.Default();
            desc.IsDepthEnabled = false;
            return desc;
        }

        public static DepthStencilStateDescription AlwaysDepthStateDescription()
        {
            var desc = DepthStencilStateDescription.Default();
            desc.DepthComparison = Comparison.Always;
            return desc;
        }

        public static DepthStencilStateDescription DescFromPreset(DepthStencilPresets preset) {
            switch (preset)
            {
                case DepthStencilPresets.Basic: return BasicStateDescription();
                case DepthStencilPresets.None: return NoDepthStateDescription();
                case DepthStencilPresets.Always: return AlwaysDepthStateDescription();
            }
            throw new NotImplementedException("Specified blend preset not implemented yet");
        }


        public DepthStencilStateKeeper() : this(BasicStateDescription()) { }
        public DepthStencilStateKeeper(DepthStencilStateDescription desc)
        {
            Description = desc;
        }
        public DepthStencilStateKeeper(DepthStencilPresets preset) : this(DescFromPreset(preset))
        {
        }

        protected override void InitInternal()
        {
            DepthStencilState = new DepthStencilState(Device, Description);
        }

        protected override void DisposeInternal()
        {
            DepthStencilState.Dispose();
        }

        public IDisposable UseOn(DeviceContext ctx) =>
            new Applier<DepthStencilState>(this, () => ctx.OutputMerger.DepthStencilState, val => ctx.OutputMerger.DepthStencilState = val);
    }
}
