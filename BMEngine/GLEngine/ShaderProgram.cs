using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public class ShaderProgram : IDisposable
    {
        string vert = null;
        string geo = null;
        string frag = null;

        int vertId = -1;
        int geoId = -1;
        int fragId = -1;

        int programId = -1;

        public ShaderProgram(string vert, string frag)
        {
            this.vert = vert;
            this.frag = frag;
        }

        public ShaderProgram(string vert, string geo, string frag) : this(vert, frag)
        {
            this.geo = geo;
        }

        int CompileSingleShader(string code, ShaderType type)
        {
            var shader = GL.CreateShader(type);
            GL.ShaderSource(shader, code);
            GL.CompileShader(shader);
            string info = GL.GetShaderInfoLog(shader);
            int statusCode;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);
            return shader;
        }

        void Compile()
        {
            if (programId != -1) return;

            programId = GL.CreateProgram();

            void Check(string code, ShaderType type, ref int id)
            {
                if (code != null)
                {
                    id = CompileSingleShader(code, type);
                    GL.AttachShader(programId, id);
                    GL.LinkProgram(id);
                }
            }

            Check(vert, ShaderType.VertexShader, ref vertId);
            Check(geo, ShaderType.GeometryShader, ref geoId);
            Check(frag, ShaderType.FragmentShader, ref fragId);

            GL.LinkProgram(programId);
        }

        public void Dispose()
        {
            if (programId == -1) return;

            void DeleteIfExists(ref int shader)
            {
                if (shader != -1)
                {
                    GL.DeleteShader(shader);
                    shader = -1;
                }
            }

            GL.DeleteProgram(programId);
            DeleteIfExists(ref vertId);
            DeleteIfExists(ref geoId);
            DeleteIfExists(ref fragId);
        }

        public void Bind()
        {
            Compile();
            GL.UseProgram(programId);
        }
    }
}
