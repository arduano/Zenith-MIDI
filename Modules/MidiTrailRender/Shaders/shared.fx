
struct FullColor {
    float4 diffuseColor;
    float4 emitColor;
    float4 specularColor;
    float4 waterColor;
};

struct ColorSides {
    FullColor left;
    FullColor right;
};

struct ColorConfig {
    float diffuse;
    float emit;
    float spec;
    float water;
};

struct ColorAdjust {
    ColorConfig unpress;
    ColorConfig press;
};

struct Frag {
    float4 pos : SV_POSITION;
    float3 worldPos : POS;
    float3 waterPos : WATER_POS;
    float3 normModel : NORM_MODEL;
    float3 normView : NORM_VIEW;
    float side : SIDE;
    FullColor color: COLOR;
};

float4 adjustDiffuse(float4 col, float fac){
    col.rgb *= fac;
    return col;
}

float4 adjustSpec(float fac){
    return float4(1, 1, 1, fac);
}

float4 adjustEmit(float4 col, float fac){
    col.a *= fac;
    return col;
}

FullColor colorFromSides(float4 left, float4 right, float side, float trigger, ColorAdjust adjust) {
    FullColor col = (FullColor)0;
    
    #ifdef WATER_SECONDARY
    float4 basecol = left;
    float4 watercol = right;
    #else
    float4 basecol = lerp(left, right, side);
    float4 watercol = basecol;
    #endif

    col.diffuseColor = adjustDiffuse(basecol, lerp(adjust.unpress.diffuse, adjust.press.diffuse, trigger));
    col.emitColor = adjustEmit(basecol, lerp(adjust.unpress.emit, adjust.press.emit, trigger));
    col.specularColor = adjustSpec(lerp(adjust.unpress.spec, adjust.press.spec, trigger));
    col.waterColor = adjustEmit(basecol, lerp(adjust.unpress.water, adjust.press.water, trigger));

    return col;
}

float pnoise(float4 n)
{
	return snoise(n) + snoise(n * 4) / 2;
}

float getTurbulent(float3 pos, float time, float scale) {
	float4 noisepos = float4(pos * scale, 0) + float4(0, 0, 0, time / 300);
	float noise1 = pnoise(noisepos * 100);
	float noise2 = pnoise((noisepos + float4(0, 10, 0, 0)) * 100);
	float noise3 = pnoise((noisepos + float4(0, 0, 10, 0)) * 100);
	float noise = pnoise(noisepos + float4(noise1, noise2, noise3, 0) * 2);
	noise = clamp(noise, 0, 1);
    return noise;
}

float getWater(float3 pos, float time, float scale) {
    #ifdef NO_WATER
    return 0;
    #endif

    float noise = 0;
    noise += getTurbulent(pos, time, scale);
    noise += getTurbulent(pos + float3(100, 100, 100), time, scale);
	noise = clamp(noise, 0, 1);
    return noise;
}

FullColor lerpColor(FullColor a, FullColor b, float side) {
    FullColor col = (FullColor)0;
    col.diffuseColor = lerp(a.diffuseColor, b.diffuseColor, side);
    col.emitColor = lerp(a.emitColor, b.emitColor, side);
    col.specularColor = lerp(a.specularColor, b.specularColor, side);
    col.waterColor = lerp(a.waterColor, b.waterColor, side);
    return col;
}

float4 parseColor(FullColor col, float diffuseStrength, float specularStrength, float emitStrength, float waterStength) {
    float4 diffuseColor = col.diffuseColor;
    float4 emitColor = col.emitColor;
    float4 specularColor = col.specularColor;
    float4 waterColor = col.waterColor;

    float3 diffuse = diffuseColor.rgb * diffuseStrength;
    float3 specular = specularColor.rgb * specularColor.a * specularStrength;
    float3 emit = emitColor.rgb * emitColor.a * emitStrength;
    float3 water = waterColor.rgb * waterColor.a * waterStength;
    return float4(diffuse + specular + emit + water, diffuseColor.a);
}
