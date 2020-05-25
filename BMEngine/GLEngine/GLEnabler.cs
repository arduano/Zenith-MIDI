using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public class GLEnabler : IDisposable
    {
        bool disabled = false;
        int attribArrays = 0;

        List<EnableCap> enabled = new List<EnableCap>();

        public GLEnabler UseAttribArrays(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GL.EnableVertexAttribArray(i);
            }
            attribArrays = count;
            return this;
        }

        public GLEnabler Enable(EnableCap enable)
        {
            GL.Enable(enable);
            enabled.Add(enable);
            return this;
        }

        public void Disable()
        {
            if (disabled) return;
            disabled = true;

            for (int i = 0; i < attribArrays; i++)
            {
                GL.DisableVertexAttribArray(i);
            }
            foreach (var e in enabled) GL.Disable(e);
        }

        public void Dispose()
        {
            Disable();
        }
    }
}
