#version 330 compatibility

uniform mat4 matView;
uniform mat4 matModel;
uniform mat3 matModelNorm;
uniform mat3 matViewNorm;

uniform vec4 mainColor;
uniform vec4 sideColor1;
uniform vec4 sideColor2;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in float side;

out vec4 color;
out vec3 fragPos;

out vec3 normModel;
out vec3 normView;

void main()
{
    vec4 pos = vec4(position, 1.0f);
    vec4 worldPos = matModel * pos;
    vec4 screenPos = matView * worldPos;
    gl_Position = screenPos;

    normModel = normalize(matModelNorm * normal);
    normView = normalize(matViewNorm * normModel);

    fragPos = worldPos.xyz;

    vec4 blendCol = sideColor1 * side + sideColor2 * (1 - side);
    vec4 vecCol = mainColor * (1 - blendCol.a) + blendCol * blendCol.a;
    vecCol.a = 1;

    color = sideColor1;
}