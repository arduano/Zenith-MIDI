Texture2D<float4> Texture : register(t0);
sampler Sampler : register(s0);

void VS(in float2 pos : POSITION, inout float2 uv : UV, inout float4 col : COLOR, out float4 outPos : SV_POSITION)
{
  outPos = float4(pos, 0, 1);
}

float4 PS(in float2 uv : UV, in float4 col : COLOR) : SV_Target
{
    return col * Texture.Sample(Sampler, uv);
}