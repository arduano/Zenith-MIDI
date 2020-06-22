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

        List<int> instancedBuffers = new List<int>();

        public GLEnabler UseAttribArrays(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GL.EnableVertexAttribArray(i);
            }
            attribArrays = count;
            return this;
        }

        public GLEnabler UseInstancedBuffers(int start, int count)
        {
            for(int i = start; i < count + start; i++)
            {
                if (!instancedBuffers.Contains(i))
                {
                    instancedBuffers.Add(i);
                    GL.VertexAttribDivisor(i, 1);
                }
            }
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
            foreach (var i in instancedBuffers) GL.VertexAttribDivisor(i, 0);
        }

        public void Dispose()
        {
            Disable();
        }
    }
}
