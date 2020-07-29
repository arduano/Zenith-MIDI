using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper.Presets
{
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct GlowShaderParams
    {
        public int PixelCount;
        public bool Horizontal;
        public float Strength;
        public float Brightness;
    }

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

        public static ShaderProgram CompositeSSAA(int width, int height, int ssaa)
        {
            return new ShaderProgram(ReadShaderText("compositeSSAA.fx"), typeof(VertTex2D), "4_0", "VS", "PS")
                .SetDefine("WIDTH", width)
                .SetDefine("HEIGHT", height)
                .SetDefine("SSAA", ssaa);
        }

        public static ShaderProgram Colorspace()
        {
            return new ShaderProgram(ReadShaderText("colorspace.fx"), typeof(VertTex2D), "4_0", "VS", "PS");
        }

        public static ShaderProgram<GlowShaderParams> PingPongGlow()
        {
            return new ShaderProgram<GlowShaderParams>(ReadShaderText("glow.fx"), typeof(VertTex2D), "4_0", "VS", "PS");
        }

        public static ShaderProgram ColorCutoff()
        {
            return new ShaderProgram<GlowShaderParams>(ReadShaderText("cutoffColor.fx"), typeof(VertTex2D), "4_0", "VS", "PS");
        }

        public static ShaderProgram AlphaAddFix()
        {
            return new ShaderProgram<GlowShaderParams>(ReadShaderText("alphaAddFix.fx"), typeof(VertTex2D), "4_0", "VS", "PS");
        }

        public static ShaderProgram TransparencyMask(bool color)
        {
            return new ShaderProgram<GlowShaderParams>(ReadShaderText("alphaMask.fx"), typeof(VertTex2D), "4_0", "VS", "PS")
                .SetDefine(color ? "COLORCHANGE" : "MASK");
        }

        public static ShaderProgram MultiTexture(int texCount)
        {
            return new ShaderProgram<GlowShaderParams>(ReadShaderText("multiTexture.fx"), typeof(VertTex2D), "4_0", "VS", "PS")
                .SetDefine("COUNT", texCount);
        }
    }
}
