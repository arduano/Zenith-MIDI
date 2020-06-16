#version 330 compatibility

uniform mat4 view;
uniform mat4 model;

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 glColor;
layout(location = 2) in float side;

out vec4 color;

void main()
{
    vec4 pos = vec4(position, 1.0f);
    pos = model * pos;
    pos = view * pos;
    gl_Position = pos;
    color = glColor;
}