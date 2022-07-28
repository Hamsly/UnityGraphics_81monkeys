
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

/**
 * A constant the represents the project's asset pixel per unit value
 */
#define PIXELS_PER_UNIT 32

float2 GetScale(float3 OSPosition,float2 lightingUV)
{
    const float2 lp = ComputeScreenPos(TransformObjectToHClip(OSPosition + float3(1,-1,0))).xy;
    return (lp - lightingUV) / PIXELS_PER_UNIT;
}

float2 DecodeOffset(float4 color)
{
    int g = color.g * 128;
    float xx = ((color.r * 255) - 127);
    float yy = ((color.b * 255) - 127);

    xx += (g & 0xF) * 256 * sign(xx);
    yy += (g >> 4 & 0xF) * 256 * sign(yy);

    return float2(xx,yy);
}
