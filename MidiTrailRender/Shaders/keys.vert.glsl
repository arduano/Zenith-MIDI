#version 330 compatibility

uniform mat4 matView;
uniform mat4 matModel;
uniform mat3 matModelNorm;
uniform mat3 matViewNorm;

uniform vec3 mainColor;
uniform vec3 sideColor1;
uniform vec3 sideColor2;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in float side;

out vec4 color;

void main()
{
    vec4 pos = vec4(position, 1.0f);
    pos = matModel * pos;
    pos = matView * pos;
    gl_Position = pos;

    vec3 normModel = normalize(matModelNorm * normal);
    vec3 normView = normalize(matViewNorm * normModel);

    //float col = dot(normView, vec3(1, 1, 1));
    //float col = dot(normView, vec3(0, 0, -1));
    //color = vec4(col, col, col, 1.0f);
    color = vec4(abs(normView), 1.0f);
}