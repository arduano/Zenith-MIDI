#version 330 compatibility

uniform mat4 matView;
uniform mat4 matModel;
uniform mat3 matModelNorm;
uniform mat3 matViewNorm;

uniform vec3 viewPos;
uniform float pressDepth;

in vec4 color;
in vec3 fragPos;

in vec3 normModel;
in vec3 normView;
 
out vec4 outputF;

const float sunShadowStrength = 0.98;
const float specularPower = 16;
const float pressKeyGlow = 0;

void main()
{
    vec3 light = normalize(vec3(-1, 1, -1));

    vec3 viewDir = normalize(fragPos + viewPos);

    float viewShadow = dot(normView, vec3(0, 0, -1));
    viewShadow = max(viewShadow, 0);

    float lightShadow = dot(normModel, light);
    lightShadow += 0.2;
    lightShadow = max(lightShadow, 0);

    float specular = dot(reflect(-(light), normModel), -viewDir);
    specular = max(specular, 0);
    specular = pow(specular, specularPower);

    float lightShadowAdjust = ((1 - sunShadowStrength) + lightShadow * sunShadowStrength);
    float pressShadowGlow = (1 - pressDepth * pressKeyGlow);
    lightShadowAdjust = ((1 - pressShadowGlow) + lightShadowAdjust * pressShadowGlow);

    float specularAdjust = (1 - pressDepth * pressKeyGlow);

    vec3 compound = color.xyz;
    compound *= lightShadowAdjust;
    compound += color.xyz * viewShadow * 0.2;
    compound += (1 - color.xyz) * (1 - pow(viewShadow, 0.5)) * 0.05;
    compound += specular * specularAdjust;
    outputF = vec4(compound, color.a * 1);

    //float fac = specular + lightShadow;
    //outputF = vec4(fac, fac, fac, 1);
    //outputF = vec4(specular, specular, specular, 1);
}