using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper.Presets
{
    public class PingPongGlow : DeviceInitiable
    {
        public int Width { get; }
        public int Height { get; }

        ShaderProgram<GlowShaderParams> glowShader;
        ShaderProgram basicShader;
        CompositeRenderSurface buffer;
        Compositor composite;
        Initiator init = new Initiator();

        float glowLowCutoff = 0.05f;
        public float GlowCutoff
        {
            get => glowLowCutoff;
            set
            {
                if (glowLowCutoff == value) return;
                glowLowCutoff = value;
                RebuildConstants();
            }
        }

        float glowSigma = 1;
        public float GlowSigma
        {
            get => glowSigma;
            set
            {
                if (glowSigma == value) return;
                glowSigma = value;
                RebuildConstants();
            }
        }

        public PingPongGlow(int width, int height)
        {
            Width = width;
            Height = height;

            glowShader = init.Add(Shaders.PingPongGlow());
            basicShader = init.Add(Shaders.BasicTextured());
            composite = init.Add(new Compositor());
            buffer = init.Add(new CompositeRenderSurface(width, height));

            RebuildConstants();
        }

        protected override void InitInternal()
        {
            init.Init(Device);
        }

        protected override void DisposeInternal()
        {
            init.Dispose();
        }

        double Gaussian(double x, double sigma)
        {
            return Math.Exp(Math.Pow(x / sigma, 2) / -2) / (sigma * Math.Sqrt(2 * Math.PI));
        }

        float[] GenGaussian(int width, float sigma)
        {
            double middle = width / 2.0 - 0.5;
            float[] data = new float[width];
            for (int i = 0; i < width; i++) data[i] = (float)Gaussian(i - middle, sigma);
            return data;
        }

        void RebuildConstants()
        {
            int w = 1;
            double cutoff = Gaussian(0, GlowSigma) * GlowCutoff;
            for (; w < 500; w++)
            {
                if (Gaussian(w, GlowSigma) < cutoff) break;
            }


            w = w * 2 + 1;
            var data = GenGaussian(w, GlowSigma);

            glowShader.SetDefine("WIDTH", w);
            glowShader.SetDefine("MAXSIGMA", GlowSigma);
        }

        public void ApplyOn(DeviceContext context, CompositeRenderSurface surface, float strength = 1, float brightness = 3)
        {
            glowShader.ConstData.Strength = strength;
            glowShader.ConstData.Brightness = 1;
            glowShader.ConstData.Horizontal = true;
            glowShader.ConstData.PixelCount = surface.Width;
            composite.Composite(context, surface, glowShader, buffer);

            glowShader.ConstData.Strength = strength;
            glowShader.ConstData.Brightness = brightness;
            glowShader.ConstData.Horizontal = false;
            glowShader.ConstData.PixelCount = surface.Height;
            composite.Composite(context, buffer, glowShader, surface);
        }
    }
}
