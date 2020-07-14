Texture2D<float4> Texture : register(t0);
sampler Sampler : register(s0);

void VS(in float2 pos : POSITION, inout float2 uv : UV, inout float4 col : COLOR, out float4 outPos : SV_POSITION)
{
    outPos = float4(pos.x * 2 - 1, (pos.y * 2 - 1), 0, 1);
}

float4 PS(in float2 uv : UV, in float4 col : COLOR) : SV_Target
{
    return col * Texture.Sample(Sampler, uv);
    float4 sum = float4(0, 0, 0, 0);
    float stepX = 1.0 / WIDTH / SSAA;
    float stepY = 1.0 / HEIGHT / SSAA;
    for(int i = 0; i < SSAA; i += 1)
    {
        for(int j = 0; j < SSAA; j += 1)
        {
            sum += Texture.Sample(Sampler, uv + float2(i * stepX, j * stepY));
        }
    }
    return sum / (SSAA * SSAA) * col;
}