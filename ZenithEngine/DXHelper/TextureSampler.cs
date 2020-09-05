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

        protected override void InitInternal()
        {
            Sampler = dispose.Add(new SamplerState(Device, Description));
        }

        public IDisposable UseOnPS(DeviceContext ctx, int slot) =>
            new Applier<SamplerState>(this, () => ctx.PixelShader.GetSamplers(slot, 1)[0], val => ctx.PixelShader.SetSampler(slot, val));
        public IDisposable UseOnVS(DeviceContext ctx, int slot) =>
            new Applier<SamplerState>(this, () => ctx.VertexShader.GetSamplers(slot, 1)[0], val => ctx.VertexShader.SetSampler(slot, val));
        public IDisposable UseOnGS(DeviceContext ctx, int slot) =>
            new Applier<SamplerState>(this, () => ctx.GeometryShader.GetSamplers(slot, 1)[0], val => ctx.GeometryShader.SetSampler(slot, val));

        public IDisposable UseOnPS(DeviceContext ctx) =>
            UseOnPS(ctx, 0);
        public IDisposable UseOnVS(DeviceContext ctx) =>
            UseOnVS(ctx, 0);
        public IDisposable UseOnGS(DeviceContext ctx) =>
            UseOnGS(ctx, 0);

    }
}
