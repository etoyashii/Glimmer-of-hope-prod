void ReduceFog_float(float3 BaseColor, float3 PositionWS, float FogInfluence, out float3 Out)
{
    float dist = distance(_WorldSpaceCameraPos, PositionWS);

    float fogFactor = saturate(exp2(-(unity_FogParams.x * dist) * (unity_FogParams.x * dist)));

    float3 foggedColor = lerp(unity_FogColor.rgb, BaseColor, fogFactor);
    Out = lerp(BaseColor, foggedColor, FogInfluence);
}