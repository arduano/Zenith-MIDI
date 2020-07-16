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

        public void Composite(DeviceContext context, ITextureResource[] sources, ShaderProgram shader, IRenderSurface destination, bool clearDest = true)
        {
            using (shader.UseOn(context))
            using (clearDest ? destination.UseViewAndClear(context) : destination.UseView(context))
            {
                var srcDispose = sources.Select((s, i) => s.UseOnPS(context, i)).ToArray();
                buffer.BindAndDraw(context);
                foreach (var d in srcDispose) d.Dispose();
            }
        }

        public void Composite(DeviceContext context, ITextureResource source, ShaderProgram shader, IRenderSurface destination, bool clearDest = true)
        {
            Composite(context, new[] { source }, shader, destination, clearDest);
        }
    }
}
