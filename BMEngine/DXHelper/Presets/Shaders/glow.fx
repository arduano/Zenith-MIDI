Texture2D<float4> Texture : register(t0);
sampler Sampler : register(s0);

#ifndef WIDTH
#define WIDTH 9
#endif

#ifndef MAXSIGMA
#define MAXSIGMA 1
#endif

int pixels;
bool horizontal;
float strength;
float brightness;

static float pi = 3.141592;
static float halfWidthPixels = WIDTH / 2.0 - 0.5;

static float gaussFac = sqrt(2 * pi);

float gauss(float x, float sigma) {
    float x2 = x / sigma;
    return exp(x2 * x2 / -2) / (sigma * gaussFac);
}

float addFromStrength(float strength) {
    return max(strength, 0);
}

float3 addFromColor(float3 color) {
    float extra = addFromStrength(color.r) + addFromStrength(color.g) + addFromStrength(color.b);
    extra *= strength;
    extra = sqrt(extra + 1) - 1;
    return extra / (extra + 1);
}

float4 filterPixel(float4 pixel, int x){
    pixel.rgb *= sqrt(pixel.a);
    float extra = addFromColor(pixel.rgb);
    if(extra < 0.01) return float4(0, 0, 0, 0);
    float fac = gauss(x, extra * MAXSIGMA);
    return float4(pixel.rgb * fac * brightness, pixel.a * fac);
}

void VS(in float2 pos : POSITION, inout float2 uv : UV, inout float4 col : COLOR, out float4 outPos : SV_POSITION)
{
    outPos = float4(pos.x * 2 - 1, (pos.y * 2 - 1), 0, 1);
}


float4 PS(in float2 uv : UV, in float4 col : COLOR) : SV_Target
{
    float step = 1.0 / pixels;
    float4 sum = float4(0, 0, 0, 0);
    float halfWidth = step * halfWidthPixels; 
    if (horizontal){
        for(int i = 0; i < WIDTH; i++){
            sum += filterPixel(Texture.Sample(Sampler, uv + float2(i * step - halfWidth, 0)), i - halfWidthPixels);
        }
    }
    else{
        for(int i = 0; i < WIDTH; i++){
            sum += filterPixel(Texture.Sample(Sampler, uv + float2(0, i * step - halfWidth)), i - halfWidthPixels);
        }
    }
    if(sum.a == 0) return float4(0, 0, 0, 0);
    return float4(sum.rgb / sum.a, sum.a);
}