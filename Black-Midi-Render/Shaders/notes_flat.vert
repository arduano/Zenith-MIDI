#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 glColor;

out vec4 color;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, 1.0f, 1.0f);
    color = glColor;
}
