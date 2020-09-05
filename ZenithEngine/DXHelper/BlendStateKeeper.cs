using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public enum BlendPreset
    {
        Blend,
        Add,
        PreserveColor,
    }

    public class BlendStateKeeper : DeviceInitiable
    {
        public static implicit operator BlendState(BlendStateKeeper keeper) => keeper.BlendState;

        public BlendState BlendState { get; private set; }
        public BlendStateDescription Description;

        public static RenderTargetBlendDescription BasicTargetBlendDescription()
        {
            var renderTargetDesc = new RenderTargetBlendDescription();
            renderTargetDesc.IsBlendEnabled = true;
            renderTargetDesc.SourceBlend = BlendOption.SourceAlpha;
            renderTargetDesc.DestinationBlend = BlendOption.InverseSourceAlpha;
            renderTargetDesc.BlendOperation = BlendOperation.Add;
            renderTargetDesc.SourceAlphaBlend = BlendOption.InverseDestinationAlpha;
            renderTargetDesc.DestinationAlphaBlend = BlendOption.One;
            renderTargetDesc.AlphaBlendOperation = BlendOperation.Add;
            renderTargetDesc.RenderTargetWriteMask = ColorWriteMaskFlags.All;
            return renderTargetDesc;
        }

        public static RenderTargetBlendDescription FullPreserveColorBlendDescription()
        {
            var renderTargetDesc = new RenderTargetBlendDescription();
            renderTargetDesc.IsBlendEnabled = true;
            renderTargetDesc.SourceBlend = BlendOption.One;
            renderTargetDesc.DestinationBlend = BlendOption.One;
            renderTargetDesc.BlendOperation = BlendOperation.Add;
            renderTargetDesc.SourceAlphaBlend = BlendOption.InverseDestinationAlpha;
            renderTargetDesc.DestinationAlphaBlend = BlendOption.One;
            renderTargetDesc.AlphaBlendOperation = BlendOperation.Add;
            renderTargetDesc.RenderTargetWriteMask = ColorWriteMaskFlags.All;
            return renderTargetDesc;
        }

        public static RenderTargetBlendDescription AddTargetBlendDescription()
        {
            var renderTargetDesc = new RenderTargetBlendDescription();
            renderTargetDesc.IsBlendEnabled = true;
            renderTargetDesc.SourceBlend = BlendOption.One;
            renderTargetDesc.DestinationBlend = BlendOption.One;
            renderTargetDesc.BlendOperation = BlendOperation.Add;
            renderTargetDesc.SourceAlphaBlend = BlendOption.InverseDestinationAlpha;
            renderTargetDesc.DestinationAlphaBlend = BlendOption.One;
            renderTargetDesc.AlphaBlendOperation = BlendOperation.Add;
            renderTargetDesc.RenderTargetWriteMask = ColorWriteMaskFlags.All;
            return renderTargetDesc;
        }

        public static BlendStateDescription BasicBlendDescription(RenderTargetBlendDescription renderTargetDesc)
        {
            BlendStateDescription blendDesc = new BlendStateDescription();
            blendDesc.AlphaToCoverageEnable = false;
            blendDesc.IndependentBlendEnable = false;
            for(int i = 0; i < blendDesc.RenderTarget.Length; i++)
                blendDesc.RenderTarget[i] = renderTargetDesc;

            return blendDesc;
        }

        public static RenderTargetBlendDescription TargetBlendDescriptionFromPreset(BlendPreset preset)
        {
            if (preset == BlendPreset.Blend) return BasicTargetBlendDescription();
            if (preset == BlendPreset.Add) return AddTargetBlendDescription();
            if (preset == BlendPreset.PreserveColor) return FullPreserveColorBlendDescription();
            throw new NotImplementedException("Specified blend preset not implemented yet");
        }

        public BlendStateKeeper() : this(BasicBlendDescription(BasicTargetBlendDescription())) { }
        public BlendStateKeeper(BlendPreset preset) : this(BasicBlendDescription(TargetBlendDescriptionFromPreset(preset))) { }
        public BlendStateKeeper(BlendStateDescription desc)
        {
            Description = desc;
        }

        protected override void InitInternal()
        {
            BlendState = dispose.Add(new BlendState(Device, Description));
        }

        public IDisposable UseOn(DeviceContext ctx) =>
            new Applier<BlendState>(this, () => ctx.OutputMerger.BlendState, val => ctx.OutputMerger.BlendState = val);
    }
}
