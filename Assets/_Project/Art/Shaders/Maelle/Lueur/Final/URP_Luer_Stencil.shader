Shader "Custom/URP_Luer_Stencil"
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

        [Header(Rotation)]
        _RotationSpeedX ("Rotation Speed X (deg/s)", Float) = 0
        _RotationSpeedY ("Rotation Speed Y (deg/s)", Float) = 30
        _RotationSpeedZ ("Rotation Speed Z (deg/s)", Float) = 0
        [Toggle] _RandomizePerOrnament ("Phase aleatoire par ornement", Float) = 1

        [IntRange] _StencilRef ("Stencil Ref (doit matcher le billboard)", Range(0,255)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "LuerStencilWrite"
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
                float _RotationSpeedX;
                float _RotationSpeedY;
                float _RotationSpeedZ;
                float _RandomizePerOrnament;
            CBUFFER_END

            float Hash11(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float3x3 RotateX(float a) { float s, c; sincos(a, s, c); return float3x3(1,0,0, 0,c,-s, 0,s,c); }
            float3x3 RotateY(float a) { float s, c; sincos(a, s, c); return float3x3(c,0,s, 0,1,0, -s,0,c); }
            float3x3 RotateZ(float a) { float s, c; sincos(a, s, c); return float3x3(c,-s,0, s,c,0, 0,0,1); }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 seedBase = _RandomizePerOrnament > 0.5 ? pivotWS : float3(0, 0, 0);

                float phaseX = Hash11(seedBase + 11.0) * TWO_PI;
                float phaseY = Hash11(seedBase + 53.0) * TWO_PI;
                float phaseZ = Hash11(seedBase + 97.0) * TWO_PI;

                float angleX = radians(_RotationSpeedX) * _Time.y + phaseX;
                float angleY = radians(_RotationSpeedY) * _Time.y + phaseY;
                float angleZ = radians(_RotationSpeedZ) * _Time.y + phaseZ;

                float3x3 rot = mul(RotateZ(angleZ), mul(RotateY(angleY), RotateX(angleX)));

                float3 rotatedPosOS = mul(rot, IN.positionOS.xyz);
                float3 rotatedNormalOS = mul(rot, IN.normalOS);

                OUT.positionHCS = TransformObjectToHClip(rotatedPosOS);
                OUT.positionWS  = TransformObjectToWorld(rotatedPosOS);
                OUT.normalWS    = TransformObjectToWorldNormal(rotatedNormalOS);
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
