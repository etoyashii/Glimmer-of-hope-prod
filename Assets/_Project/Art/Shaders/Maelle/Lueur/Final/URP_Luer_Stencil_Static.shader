Shader "Custom/URP_Luer_Stencil_Static"
{
    Properties
    {
        _ColorCold ("Color Cold", Color) = (0.517, 0.344, 1.0, 1.0)
        _ColorMedium ("Color Medium", Color) = (1.0, 0.887, 0.0, 1.0)
        _ColorHot ("Color Hot", Color) = (1.0, 0.0, 0.0, 1.0)
        _Intensity ("Intensity", Range(0, 5)) = 1.97

        _RimPower ("Rim Power (forme du fresnel)", Range(0.1, 8)) = 2.0
        _Speed ("Pulse Speed", Vector) = (0, 2, 0, 0)
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.15

        [IntRange] _StencilRef ("Stencil Ref (doit matcher le billboard)", Range(0,255)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "LuerStencilWriteStatic"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCold;
                half4 _ColorMedium;
                half4 _ColorHot;
                float _Intensity;
                float _RimPower;
                float4 _Speed;
                float _PulseAmount;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - IN.positionWS);

                float fresnel = 1.0 - saturate(dot(normalWS, viewDir));
                fresnel = pow(fresnel, _RimPower);

                float pulse = sin(_Time.y * _Speed.y + _Speed.x) * 0.5 + 0.5;
                float factor = saturate(fresnel + pulse * _PulseAmount);

                half3 ramp = lerp(_ColorCold.rgb, _ColorMedium.rgb, saturate(factor * 2.0));
                ramp = lerp(ramp, _ColorHot.rgb, saturate(factor * 2.0 - 1.0));

                half3 col = ramp * _Intensity;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
