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
    public class Compositor : DeviceInitiable
    {
        ModelBuffer<VertTex2D> buffer = new ModelBuffer<VertTex2D>(
            new[] {
                new VertTex2D(new Vector2(1, 0), new Vector2(1, 1), new Color4(1.0f)),
                new VertTex2D(new Vector2(1, 1), new Vector2(1, 0), new Color4(1.0f)),
                new VertTex2D(new Vector2(0, 1), new Vector2(0, 0), new Color4(1.0f)),
                new VertTex2D(new Vector2(0, 0), new Vector2(0, 1), new Color4(1.0f)),
            }, 
            new[] { 0, 2, 1, 0, 3, 2 }
        );

        protected override void InitInternal()
        {
            buffer.Init(Device);
        }

        protected override void DisposeInternal()
        {
            buffer.Dispose();
        }

        public void Composite(DeviceContext context, ITextureResource[] sources, ShaderProgram shader, IRenderSurface destination)
        {
            using (shader.UseOn(context))
            {
                context.OutputMerger.SetTargets(destination.RenderTarget);
                context.Rasterizer.SetViewport(new Viewport(0, 0, destination.Width, destination.Height, 0.0f, 1.0f));
                context.PixelShader.SetShaderResources(0, sources.Select(r => r.TextureResource).ToArray());
                buffer.BindAndDraw(context);
            }
        }

        public void Composite(DeviceContext context, ITextureResource source, ShaderProgram shader, IRenderSurface destination)
        {
            Composite(context, new[] { source }, shader, destination);
        }
    }
}
