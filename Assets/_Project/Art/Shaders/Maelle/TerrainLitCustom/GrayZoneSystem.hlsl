#ifndef GRAY_ZONE_SYSTEM_INCLUDED
#define GRAY_ZONE_SYSTEM_INCLUDED

struct GrayZoneData
{
    float4x4 worldToLocal;
    float2 size;
    float threshold;
    float contrast;
    int maskIndex;
    float2 paintOrigin;
    float paintRadius;
};

StructuredBuffer<GrayZoneData> _GrayZones;
int _GrayZoneCount;

TEXTURE2D_ARRAY(_GrayZoneMasks);
SAMPLER(sampler_GrayZoneMasks);

float CheckSingleZone(float3 worldPosition, GrayZoneData zone)
{
    float3 localPosition = mul(zone.worldToLocal, float4(worldPosition, 1)).xyz;

    float2 uv;
    uv.x = localPosition.x / zone.size.x + 0.5;
    uv.y = localPosition.z / zone.size.y + 0.5;

    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
    {
        return 0;
    }

    float mask = SAMPLE_TEXTURE2D_ARRAY(
        _GrayZoneMasks,
        sampler_GrayZoneMasks,
        uv,
        zone.maskIndex
    ).r;

    float contrasted = saturate(mask);
    contrasted = pow(contrasted, (float) zone.contrast);

    if (zone.paintRadius > 0)
    {
        float distToPaint = distance(localPosition.xz, zone.paintOrigin);
        float paintEdge = max(zone.paintRadius * 0.15, 0.001);
        float painted = 1.0 - smoothstep(zone.paintRadius - paintEdge, zone.paintRadius, distToPaint);
        contrasted *= (1.0 - painted);
    }

    return contrasted;
}

float GetGrayMask(float3 positionWS)
{
    float result = 0;
    if (_GrayZoneCount <= 0)
        return result;

    for (int i = 0; i < _GrayZoneCount; i++)
    {
        float zoneValue = CheckSingleZone(positionWS, _GrayZones[i]);
        result = max(result, zoneValue);
    }
    return result;
}

#endif