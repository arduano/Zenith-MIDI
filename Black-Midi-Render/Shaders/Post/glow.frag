#version 330 core

in vec2 UV;

uniform float u_textureSize;
uniform float u_sigma;
uniform int u_width;
uniform int u_pass;
uniform float u_strength;

out vec4 color;

uniform sampler2D textureSampler;

float CalcGauss( float x, float sigma ) 
{
    float coeff = 1.0 / (2.0 * 3.14157 * sigma);
    float expon = -(x*x) / (2.0 * sigma);
    return (coeff*exp(expon));
}

void main()
{
    float pixelstep = 1.0 / u_textureSize;
    color = vec4(0, 0, 0, 0);
    vec4 col;
    float sum = 0;
    float asum = 0;
    for(float i = 0; i < u_width; i++){
        vec2 actStep;
        if(u_pass == 0) actStep = vec2( i * pixelstep, 0.0 );
        else actStep = vec2( 0.0, i * pixelstep );

        float weight = (u_width-i)/u_width / u_width;//CalcGauss( i / float(u_width), u_sigma );
        col = texture( textureSampler, UV + actStep) * weight;
        if(col.w != 0) 
        {
            color += col;
            sum += weight;
            asum += col.w;
        }
        col = texture( textureSampler, UV - actStep) * weight;
        if(col.w != 0) 
        {
            color += col;
            sum += weight;
            asum += col.w;
        }
    }
    color.w /= sum;
    color.w *= u_strength;
    //color.w = asum / u_width * u_strength;
}