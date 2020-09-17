
cbuffer c {
    float4x4 matModel;
    float4x4 matView;
    float3 viewPos;
}

Frag VS(NoteVert vert)
{
    Frag output = (Frag)0;
    float3 pos = vert.pos;

    float scaleTotal = vert.scale;

    pos.xyz *= scaleTotal;

    float edge = 0.5 * scaleTotal;
    float bottom = 1 * scaleTotal;

    pos.x += (-edge + vert.right) * vert.corner.x + (edge + vert.left) * (1 - vert.corner.x);
    pos.z += (-edge - vert.start) * vert.corner.z + (edge - vert.end) * (1 - vert.corner.z);
    pos.y += (bottom - vert.height) * (1 - vert.corner.y);

    float4 worldPos = mul(matModel, float4(pos, 1));
    output.pos = mul(matView, worldPos);
    output.worldPos = worldPos.xyz;

    output.side = vert.side;

    output.normModel = normalize(mul((float3x3)matModel, vert.normal));
    output.normView = normalize(mul((float3x3)matView, output.normModel));

    output.color.diffuseColor = vert.colorleft * vert.side + vert.colorright * (1 - vert.side);
    output.color.specularColor = float4(1, 1, 1, 1);

    output.color.emitColor = output.color.diffuseColor;
    output.color.emitColor.xyz *= 0;

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

    float4 diffuseColor = pixel.color.diffuseColor;
    float4 emitColor = pixel.color.emitColor;
    float4 specularColor = pixel.color.specularColor;

    float4 outputColor = float4(diffuseColor.rgb * diffuseStrength + specularColor.rgb * specularColor.a * specular + emitColor.rgb * emitColor.a * emitStrength, diffuseColor.a);

    return outputColor;
}