
struct Frag {
    float4 pos : SV_POSITION;
    float4 worldPos : POS;
    float4 col : COLOR;
    float3 normModel : NORM_MODEL;
    float3 normView : NORM_VIEW;
};

float4x4 matModel;
float4x4 matView;
float3 viewPos;
float pressDepth;

Frag VS(KeyVert vert)
{
    Frag output = (Frag)0;
    output.worldPos = mul(matModel, float4(vert.pos, 1));
    output.pos = mul(matView, output.worldPos);
    output.col = float4(1, 1, 1, 1);

    output.normModel = normalize(mul((float3x3)matModel, vert.normal));
    output.normView = normalize(mul((float3x3)matView, output.normModel));

    return output;
}

float4 PS(Frag pixel) : SV_Target
{
    float3 worldNorm = normalize(pixel.normModel); 
    float3 viewNorm = normalize(pixel.normView); 

    float3 light = normalize(float3(1, 1, 1));

    float fac = dot(worldNorm, light);

    return float4(fac, fac, fac, 1);
}