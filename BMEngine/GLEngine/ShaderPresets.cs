using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.GLEngine
{
    public partial class ShaderProgram
    {
        public static class Presets
        {
            static string basicVert = @"
#version 330 compatibility

layout(location = 0) in vec2 position;
layout(location = 1) in vec4 glColor;

out vec4 color;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, 1.0f, 1.0f);
    color = glColor;
}
";
            static string basicFrag = @"
#version 330 compatibility
 
in vec4 color;
 
out vec4 outputF;

void main()
{
    outputF = color;
}
";

            static string texturedVert = @"
#version 330 compatibility

layout(location = 0) in vec2 position;
layout(location = 1) in vec2 glUV;
layout(location = 2) in vec4 glColor;

out vec4 color;
out vec2 uv;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, 1.0f, 1.0f);
    color = glColor;
    uv = glUV;
}
";
            static string texturedFrag = @"
#version 330 compatibility
 
in vec4 color;
in vec2 uv;
 
uniform sampler2D texture;

out vec4 output;

void main()
{
    vec4 tex = texture2D( texture, uv );
    vec4 col = color * tex;
#ifdef COLFILTER
    col = COLFILTER;
#endif
    output = col;
}
";

            static string texturedSSAAFrag = @"
#version 330 compatibility
 
in vec4 color;
in vec2 uv;
 
uniform sampler2D texture;

out vec4 output;

void main()
{
    vec4 sum = vec4(0, 0, 0, 0);
    float stepX = 1.0 / WIDTH / SSAA;
    float stepY = 1.0 / HEIGHT / SSAA;
    for(int i = 0; i < SSAA; i += 1){
        for(int j = 0; j < SSAA; j += 1){
            sum += texture2D(texture, uv + vec2(i * stepX, j * stepY));
        }
    }
    output = sum / (SSAA * SSAA) * color;
}
";

            public static ShaderProgram BasicTextured() =>
                new ShaderProgram(texturedVert, texturedFrag);
            public static ShaderProgram BasicTextured(string colFilter) =>
                new ShaderProgram(texturedVert, texturedFrag).SetDefine("COLFILTER", colFilter);

            public static ShaderProgram Basic() =>
                new ShaderProgram(basicVert, basicFrag);

            public static ShaderProgram SSAA(int width, int height, int SSAA) =>
                new ShaderProgram(texturedVert, texturedSSAAFrag)
                    .SetDefine("WIDTH", width)
                    .SetDefine("HEIGHT", height)
                    .SetDefine("SSAA", SSAA);
        }
    }
}
