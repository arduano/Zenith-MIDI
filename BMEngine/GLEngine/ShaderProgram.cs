using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public partial class ShaderProgram : IDisposable
    {
        string vert = null;
        string geo = null;
        string frag = null;

        int vertId = -1;
        int geoId = -1;
        int fragId = -1;

        int programId = -1;

        Dictionary<string, string> defines = new Dictionary<string, string>();
        Dictionary<string, int> uniforms = new Dictionary<string, int>();

        public int this[string uniform]
        {
            get => Uniform(uniform);
        }

        public int Uniform(string uniform)
        {
            if (!uniforms.ContainsKey(uniform))
            {
                var u = GL.GetUniformLocation(programId, uniform);
                uniforms.Add(uniform, u);
            }
            return uniforms[uniform];
        }

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

            string prepend = "";

            foreach(var k in defines)
            {
                prepend += $"#define {k.Key} {k.Value}\n";
            }

            void Check(string code, ShaderType type, ref int id)
            {
                if (code != null)
                {
                    var lines = code.Split('\n');
                    var versionLine = Array.FindIndex(lines, (l) => l.Contains("#version"));
                    if(versionLine == -1) throw new ApplicationException("Versin line missing in shader");

                    code = string.Join("\n", 
                        lines.Take(versionLine + 1)
                        .Concat(new[] { "", prepend, "" })
                        .Concat(lines.Skip(versionLine + 1))
                    );

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
            programId = -1;

            DeleteIfExists(ref vertId);
            DeleteIfExists(ref geoId);
            DeleteIfExists(ref fragId);
        }

        public ShaderProgram SetDefine(string define) => SetDefine(define, "");
        public ShaderProgram SetDefine(string define, int value) => SetDefine(define, value.ToString());
        public ShaderProgram SetDefine(string define, float value) => SetDefine(define, value.ToString());
        public ShaderProgram SetDefine(string define, string value)
        {
            if (defines.ContainsKey(define))
            {
                if (defines[define] == value) return this;
                defines[define] = value;
            }
            else
            {
                defines.Add(define, value);
            }

            Dispose();
            
            return this;
        }

        public ShaderProgram RemoveDefine(string define)
        {
            if (!defines.ContainsKey(define)) return this;
            defines.Remove(define);
            Dispose();
            return this;
        }


        public void Bind()
        {
            Compile();
            GL.UseProgram(programId);
        }
    }
}
