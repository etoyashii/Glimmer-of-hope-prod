#include "./GrayZoneSystem.hlsl"

void GrayZoneDesaturate_float(float3 PositionWS, float3 Albedo, out float3 Out)
{
    float mask = GetGrayMask(PositionWS);
    float gray = dot(Albedo, float3(0.299, 0.587, 0.114));
    Out = lerp(Albedo, gray.xxx, mask);
}
