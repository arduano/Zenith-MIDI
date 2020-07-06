using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public class TextureSampler : DeviceInitiable
    {
        public static implicit operator SamplerState(TextureSampler sampler) => sampler.Sampler;

        public SamplerState Sampler { get; private set; }
        public SamplerStateDescription Description { get; }

        public TextureSampler() : this(new SamplerStateDescription()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLodBias = 0,
            MaximumAnisotropy = 1,
            ComparisonFunction = Comparison.Always,
            MinimumLod = 0,
            MaximumLod = float.MaxValue
        })
        { }
        public TextureSampler(SamplerStateDescription desc)
        {
            Description = desc;
        }

        protected override void DisposeInternal()
        {
            Sampler.Dispose();
        }

        protected override void InitInternal()
        {
            Sampler = new SamplerState(Device, Description);
        }
    }
}
