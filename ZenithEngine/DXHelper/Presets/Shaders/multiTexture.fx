Texture2D Textures[COUNT];
sampler Sampler : register(s0);

void VS(in float2 pos : POSITION, inout float2 uv : UV, inout float4 col : COLOR, inout int texid : TEX, out float4 outPos : SV_POSITION)
{
    outPos = float4(pos.x * 2 - 1, (pos.y * 2 - 1), 0, 1);
}

float4 colFromTex(int texid, float2 uv){
    switch(texid) {
CASES
        default: return float4(1, 1, 1, 1);
    }
}

float4 applyColor(float4 col, float4 tex)
{
    #ifdef COLAPPLY
COLAPPLY_CODE
    #else
    return col * tex;
    #endif
    return col;
}

float4 PS(in float2 uv : UV, in float4 col : COLOR, in int texid : TEX) : SV_Target
{
    float4 tex = colFromTex(texid, uv);
    return applyColor(col, tex);
}