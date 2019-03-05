#version 330 core

in vec3 position;
in vec4 glColor;
in vec2 texCoordV;
out vec2 UV;

out vec4 color;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, position.z * 2 - 1, 1.0f);
	color = glColor;
    UV = vec2(position.x, position.y);
}