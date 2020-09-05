Texture2D<float4> Texture : register(t0);
sampler Sampler : register(s0);

void VS(in float2 pos : POSITION, inout float2 uv : UV, inout float4 col : COLOR, out float4 outPos : SV_POSITION)
{
    outPos = float4(pos.x * 2 - 1, (pos.y * 2 - 1), 0, 1);
}

float4 PS(in float2 uv : UV, in float4 col : COLOR) : SV_Target
{
    float4 color = Texture.Sample(Sampler, uv);
    #ifdef MASK
    return float4(color.a, color.a, color.a, 1);
    #endif
    #ifdef COLORCHANGE
    if(color.a < 0.001) return float4(1, 1, 1, 1);
    return float4(color.rgb / color.a, 1);
    #endif
}