Texture2D<float4> Textures[COUNT];
sampler Sampler : register(s0);

void VS(in float2 pos : POSITION, inout float2 uv : UV, inout float4 col : COLOR, out int tex: TEX, out float4 outPos : SV_POSITION)
{
    outPos = float4(pos.x * 2 - 1, (pos.y * 2 - 1), 0, 1);
}

float4 PS(in float2 uv : UV, in float4 col : COLOR, in int tex : TEX) : SV_Target
{
    return col * Textures[tex].Sample(Sampler, uv);
}