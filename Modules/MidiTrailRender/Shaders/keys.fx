
cbuffer c {
    float4x4 matModel;
    float4x4 matView;
    float3 viewPos;
	float time;
    FullColor colorLeft;
    FullColor colorRight;
}

Frag VS(KeyVert vert)
{
    Frag output = (Frag)0;
    float4 worldPos = mul(matModel, float4(vert.pos, 1));
    output.pos = mul(matView, worldPos);
    output.worldPos = worldPos.xyz;
    output.waterPos = output.worldPos;
    
    output.side = vert.side;

    output.normModel = normalize(mul((float3x3)matModel, vert.normal));
    output.normView = normalize(mul((float3x3)matView, output.normModel));

    output.color = lerpColor(colorLeft, colorRight, vert.side);

    return output;
}

float4 PS(Frag pixel) : SV_Target
{
    float3 worldNorm = normalize(pixel.normModel); 
    float3 viewNorm = normalize(pixel.normView); 

    float3 viewDir = normalize(pixel.worldPos + viewPos).xyz;

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

    float water = getWater(pixel.waterPos, time, 0.1);

    float4 outputColor = parseColor(pixel.color, diffuseStrength, specular, emitStrength, water);

    return outputColor;
}