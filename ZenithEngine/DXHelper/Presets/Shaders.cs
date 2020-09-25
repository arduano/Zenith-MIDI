using OpenTK.Graphics.ES11;
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

    public enum TextureShaderPreset
    {
        Darken,
        Lighten,
        Hybrid
    }

    public static class Shaders
    {
        public static string ReadShaderText(string shaderName)
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

        public static ShaderProgram MultiTexture(int texCount) =>
            MultiTexture(texCount);

        public static ShaderProgram MultiTexture(int texCount, TextureShaderPreset preset)
        {
            switch (preset)
            {
                case TextureShaderPreset.Darken:
                    return MultiTexture(texCount, @"
    col = col * tex;
");
                case TextureShaderPreset.Lighten:
                    return MultiTexture(texCount, @"
    tex.rgb = 1 - tex.rgb;
    col.rgb = 1 - col.rgb;
    col = tex * col;
    col.rgb = 1 - col.rgb;
");
                case TextureShaderPreset.Hybrid:
                    return MultiTexture(texCount, @"
    tex = tex * 2;
    float4 out_color;
    if(tex.r > 1){
        out_color.r = 1 - (2 - tex.r) * (1 - col.r);
    }
    else out_color.r = tex.r * col.r;
    if(tex.g > 1){
        out_color.g = 1 - (2 - tex.g) * (1 - col.g);
    }
    else out_color.g = tex.g * col.g;
    if(tex.b > 1){
        out_color.b = 1 - (2 - tex.b) * (1 - col.b);
    }
    else out_color.b = tex.b * col.b;
    out_color.a = tex.a * col.a;
    col = out_color;
");
                default:
                    throw new Exception("Shouldnt reach here");
            }
        }

        public static ShaderProgram MultiTexture(int texCount, string applyCol = null)
        {
            string defs = "";
            for(int i = 0; i < texCount; i++)
            {
                if (i != 0) defs += "\n";
                defs += $"        case {i}: return Textures[{i}].Sample(Sampler, uv);";
            }
            var shader = ReadShaderText("multiTexture.fx").Replace("CASES", defs).Replace("COUNT", texCount.ToString());

            if (applyCol != null)
            {
                shader = shader.Replace("COLAPPLY_CODE", applyCol);
            }

            var program = new ShaderProgram<GlowShaderParams>(shader, typeof(VertMultiTex2D), "4_0", "VS", "PS");

            if (applyCol != null)
            {
                program.SetDefine("COLAPPLY");
            }

            return program;
        }
    }
}
