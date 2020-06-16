#version 330 compatibility

uniform mat4 view;
uniform mat4 model;

in vec4 color;
 
out vec4 outputF;

void main()
{
    outputF = color;
}