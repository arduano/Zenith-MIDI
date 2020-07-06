using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper.Presets
{
    public static class Shaders
    {
        static string ReadShaderText(string shaderName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var all = assembly.GetManifestResourceNames().ToArray();
            using (var stream = assembly.GetManifestResourceStream("ZenithEngine.DXHelper.Presets.Shaders." + shaderName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        public static ShaderProgram BasicFlat()
        {
            return new ShaderProgram(ReadShaderText("basicFlat.fx"), typeof(Vert2D), "4_0", "VS", "PS");
        }

        public static ShaderProgram BasicTextured()
        {
            return new ShaderProgram(ReadShaderText("basicTextured.fx"), typeof(VertTex2D), "4_0", "VS", "PS");
        }
    }
}
