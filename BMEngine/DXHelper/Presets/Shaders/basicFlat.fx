
void VS(in float2 pos : POSITION, inout float4 col : COLOR, out float4 outPos : SV_POSITION)
{
    outPos = float4(pos.x * 2 - 1, (pos.y * 2 - 1), 0, 1);
}

float4 PS(in float4 col : COLOR) : SV_Target
{
    return col;
}