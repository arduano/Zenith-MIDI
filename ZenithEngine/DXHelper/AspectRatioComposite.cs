using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper.Presets;

namespace ZenithEngine.DXHelper
{
    public class AspectRatioComposite : DeviceInitiable
    {
        protected ShaderProgram solidShader;
        protected ShaderProgram textureShader;
        protected Flat2dShapeBuffer solidFill;
        protected Textured2dShapeBuffer texFill;

        protected TextureSampler sampler;
        protected BlendStateKeeper blendstate;

        public AspectRatioComposite()
        {
            solidShader = init.Add(Shaders.BasicFlat());
            textureShader = init.Add(Shaders.BasicTextured());
            sampler = init.Add(new TextureSampler());
            blendstate = init.Add(new BlendStateKeeper());
            solidFill = init.Add(new Flat2dShapeBuffer(16));
            texFill = init.Add(new Textured2dShapeBuffer(16));
        }

        public void Composite(DeviceContext context, ITextureResource source, IRenderSurface target, Color4 paddingColor)
        {
            float winAspect = (float)target.Width / target.Height;
            float fromAspect = (float)source.Width / source.Height;
            var size = (winAspect - fromAspect) / winAspect;
            var offset = size / 2;

            Vector2 tl = new Vector2(offset, 1);
            Vector2 br = new Vector2(1 - offset, 0);

            if (size < 0)
            {
                size = ((1 / winAspect) - (1 / fromAspect)) / (1 / winAspect);
                offset = size / 2;

                tl = new Vector2(0, 1 - offset);
                br = new Vector2(1, offset);
            }

            //using(raster.UseOn(context))
            using(blendstate.UseOn(context))
            using (target.UseViewAndClear(context))
            {
                using (solidShader.UseOn(context))
                {
                    solidFill.UseContext(context);
                    solidFill.PushQuad(0, 1, 1, 0, new Color4(0.5f, 0.5f, 0.5f, 1));
                    solidFill.PushQuad(tl, br, new Color4(0, 0, 0, 1));
                    //solidFill.PushQuad(tl, br, new Color4(1, 1, 1, 1));
                    solidFill.Flush();
                }

                using (textureShader.UseOn(context))
                using (sampler.UseOnPS(context))
                using (source.UseOnPS(context))
                {
                    texFill.UseContext(context);
                    texFill.PushQuad(tl, br);
                    texFill.Flush();
                }
            }
        }

        public void Composite(DeviceContext context, ITextureResource source, IRenderSurface target) =>
            Composite(context, source, target, new Color4(0.5f, 0.5f, 0.5f, 1));
    }
}
