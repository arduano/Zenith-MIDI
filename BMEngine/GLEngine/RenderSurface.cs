using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public abstract class RenderSurface : IDisposable
    {
        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public static RenderSurface BasicFrame(int width, int height)
        {
            return new BasicRenderSurface(width, height, false);
        }

        public virtual void BindSurfaceNoViewport()
        {
            throw new NotImplementedException();
        }

        public void BindSurface()
        {
            BindSurfaceNoViewport();
            GL.Viewport(0, 0, Width, Height);
        }

        public void BindSurfaceAndClear()
        {
            BindSurface();
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }
        public void BindTexture()
        {
            BindTexture(0);
        }

        public void BindTexture(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            BindTextureNoSwitch();
        }

        public virtual void BindTextureNoSwitch()
        {
            throw new NotImplementedException();
        }

        public void BindTexture(int unit)
        {
            BindTexture(TextureUnit.Texture0 + unit);
        }

        public abstract void Dispose();
    }
}
