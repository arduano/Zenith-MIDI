#version 330 compatibility

uniform mat4 matView;
uniform mat4 matModel;
uniform mat3 matModelNorm;
uniform mat3 matViewNorm;

uniform float height;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in float side;
layout(location = 3) in vec3 corner;

layout(location = 4) in float left;
layout(location = 5) in float right;
layout(location = 6) in float start;
layout(location = 7) in float end;
layout(location = 8) in vec4 noteColorLeft;
layout(location = 9) in vec4 noteColorRight;
layout(location = 10) in float scale;
layout(location = 11) in float extraScale;

out vec4 color;
out vec3 fragPos;

out vec3 normModel;
out vec3 normView;

const float scaleBase = 0.01;

void main()
{
    vec4 pos = vec4(position, 1.0f);

    float scaleTotal = scaleBase * scale;

    pos.y += extraScale;
    pos.xyz *= scaleTotal * (extraScale + 1);

    float edge = 0.5 * scaleTotal;
    float bottom = 0.5 * scaleTotal;

    pos.x += (-edge + right) * corner.x + (edge + left) * (1 - corner.x);
    pos.z += (-edge - start) * corner.z + (edge - end) * (1 - corner.z);
    
    //pos.y += (bottom - height) * (1 - corner.x);

    vec4 worldPos = matModel * pos;
    vec4 screenPos = matView * worldPos;
    gl_Position = screenPos;

    normModel = normalize(matModelNorm * normal);
    normView = normalize(matViewNorm * normModel);

    fragPos = worldPos.xyz;

    color = noteColorLeft;//vec4(0, 0, 0, 1);
}