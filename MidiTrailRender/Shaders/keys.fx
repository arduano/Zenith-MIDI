struct FullColor {
    float4 diffuseColor;
    float4 emitColor;
    float4 specularColor;
};

struct Frag {
    float4 pos : SV_POSITION;
    float4 worldPos : POS;
    float4 col : COLOR;
    float3 normModel : NORM_MODEL;
    float3 normView : NORM_VIEW;
    float side : SIDE;
};

cbuffer c {
    float4x4 matModel;
    float4x4 matView;
    float3 viewPos;
    FullColor colorLeft;
    FullColor colorRight;
}

Frag VS(KeyVert vert)
{
    Frag output = (Frag)0;
    output.worldPos = mul(matModel, float4(vert.pos, 1));
    output.pos = mul(matView, output.worldPos);
    output.col = float4(1, 1, 1, 1);

    output.side = vert.side;

    output.normModel = normalize(mul((float3x3)matModel, vert.normal));
    output.normView = normalize(mul((float3x3)matView, output.normModel));

    return output;
}

float4 PS(Frag pixel) : SV_Target
{
    float3 worldNorm = normalize(pixel.normModel); 
    float3 viewNorm = normalize(pixel.normView); 

    float3 viewDir = normalize(pixel.worldPos + viewPos);

    float3 light = normalize(float3(1, 1, 2));

    float shade = dot(worldNorm, light);
    shade = max(shade, 0);
    
    float specular = dot(reflect(-(light), worldNorm), -viewDir);
    specular = max(specular, 0);
    specular = pow(specular, 16);

    float viewShadow = dot(viewNorm, float3(0, 0, -1));
    viewShadow = max(viewShadow, 0);

    float diffuseStrength = lerp(0.2, 0, viewShadow) + lerp(0.1, 1, shade);
    float emitStrength = lerp(0.7, 1, viewShadow);

    float4 diffuseColor = lerp(colorLeft.diffuseColor, colorRight.diffuseColor, pixel.side);
    float4 emitColor = lerp(colorLeft.emitColor, colorRight.emitColor, pixel.side);
    float4 specularColor = lerp(colorLeft.specularColor, colorRight.specularColor, pixel.side);

    float4 outputColor = float4(diffuseColor.rgb * diffuseStrength + specularColor.rgb * specularColor.a * specular + emitColor.rgb * emitColor.a * emitStrength, diffuseColor.a);

    return outputColor;
}