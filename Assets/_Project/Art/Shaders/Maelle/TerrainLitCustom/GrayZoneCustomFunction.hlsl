#include "./GrayZoneSystem.hlsl"

void GrayZoneDesaturate_float(float3 PositionWS, float3 Albedo, out float3 Out, out float3 Emission)
{
    float mask = GetGrayMask(PositionWS);
    float gray = dot(Albedo, float3(0.299, 0.587, 0.114));

    Out = lerp(Albedo, float3(0, 0, 0), mask);

    Emission = gray.xxx * mask;
}
