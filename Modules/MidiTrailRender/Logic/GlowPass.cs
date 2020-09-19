using MIDITrailRender.Models;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;
using ZenithEngine.DXHelper.Presets;

namespace MIDITrailRender.Logic
{
    public class GlowPass : DeviceInitiable
    {
        ShaderProgram colorCutoffShader;
        ShaderProgram colorspaceShader;
        ShaderProgram plainShader;
        BlendStateKeeper addBlendState;
        Compositor compositor;
        PingPongGlow pingPongGlow;
        TextureSampler sampler;

        CompositeRenderSurface cutoffSurface;

        public GlowPass(int width, int height)
        {
            plainShader = init.Add(Shaders.BasicTextured());
            colorspaceShader = init.Add(Shaders.Colorspace());
            colorCutoffShader = init.Add(Shaders.ColorCutoff());
            compositor = init.Add(new Compositor());
            sampler = init.Add(new TextureSampler(SamplerPresets.Clip));
            addBlendState = init.Add(new BlendStateKeeper(BlendPreset.Add));
            init.Replace(ref pingPongGlow, new PingPongGlow(width, height));
            init.Replace(ref cutoffSurface, new CompositeRenderSurface(width, height));
        }

        public void ApplyGlow(DeviceContext context, GlowPassModel glowConfig, ITextureResource source, IRenderSurface dest)
        {
            compositor.Composite(context, source, colorCutoffShader, cutoffSurface);
            pingPongGlow.GlowSigma = (float)glowConfig.GlowSigma;
            using (sampler.UseOnPS(context))
                pingPongGlow.ApplyOn(context, cutoffSurface, (float)glowConfig.GlowStrength, (float)glowConfig.GlowBrightness);
            compositor.Composite(context, source, colorspaceShader, dest);
            using (addBlendState.UseOn(context))
                compositor.Composite(context, cutoffSurface, plainShader, dest, false);
        }
    }
}
