#version 330
 
in vec4 color;
 
out vec4 outputF;
layout(location = 0) out vec4 texOut;

void main()
{
    outputF = color;
	texOut = outputF;
}