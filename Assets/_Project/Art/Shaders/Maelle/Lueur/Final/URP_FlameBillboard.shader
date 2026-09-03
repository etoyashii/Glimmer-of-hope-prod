Shader "Custom/URP_FlameBillboard"
{

    Properties
    {
        _NoiseTexA ("Noise Layer A (scroll)", 2D) = "white" {}
        _NoiseTexB ("Noise Layer B (distortion, scroll)", 2D) = "white" {}
        _ShapeAtlas ("Shape Atlas (N vignettes cote a cote)", 2D) = "white" {}
        [IntRange] _ShapeAtlasFrames ("Shape Atlas Frame Count", Range(1,32)) = 8

        _ColorCold ("Color Cold", Color) = (0.517, 0.344, 1.0, 1.0)
        _ColorMedium ("Color Medium", Color) = (1.0, 0.887, 0.0, 1.0)
        _ColorHot ("Color Hot", Color) = (1.0, 0.0, 0.0, 1.0)
        _Intensity ("Intensity", Range(0, 5)) = 1.97

        _ScrollSpeedA ("Scroll Speed A", Vector) = (0, 0.6, 0, 0)
        _ScrollSpeedB ("Scroll Speed B", Vector) = (0.15, 0.9, 0, 0)

        _Distortion ("Distortion Amount", Range(0, 0.5)) = 0.12
        _AlphaClip ("Alpha Clip Threshold", Range(0, 1)) = 0.3
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.08
        _FlameSize ("Flame Size (multiplier)", Vector) = (2, 2, 0, 0)

        _ShapeMaskStrength ("Shape Mask Strength", Range(0, 1)) = 1.0
        _MaskAngleOffset ("Mask Angle Offset (deg)", Range(-180, 180)) = 0
        _ShapeMaskScale ("Shape Mask Scale (independant de Flame Size)", Vector) = (1, 1, 0, 0)
        _ShapeMaskOffset ("Shape Mask Offset", Vector) = (0, 0, 0, 0)

        [IntRange] _StencilRef ("Stencil Ref (doit matcher le mesh)", Range(0,255)) = 1

        [Header(Rotation)]
        _RotationSpeedX ("Rotation Speed X (deg/s)", Float) = 0
        _RotationSpeedY ("Rotation Speed Y (deg/s)", Float) = 30
        _RotationSpeedZ ("Rotation Speed Z (deg/s)", Float) = 0
        [Toggle] _RandomizePerOrnament ("Phase aleatoire par ornement", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }        
        LOD 100

        Stencil
        {
            Ref [_StencilRef]
            Comp NotEqual
            Pass Keep
        }

        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float relativeAngle : TEXCOORD1; 
            };

            TEXTURE2D(_NoiseTexA); SAMPLER(sampler_NoiseTexA);
            TEXTURE2D(_NoiseTexB); SAMPLER(sampler_NoiseTexB);
            TEXTURE2D(_ShapeAtlas); SAMPLER(sampler_ShapeAtlas);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTexA_ST;
                float4 _NoiseTexB_ST;
                float4 _ScrollSpeedA;
                float4 _ScrollSpeedB;
                float _Distortion;
                float _AlphaClip;
                float _EdgeSoftness;
                float _Intensity;
                float4 _FlameSize;
                half4 _ColorCold;
                half4 _ColorMedium;
                half4 _ColorHot;
                float _ShapeMaskStrength;
                float _MaskAngleOffset;
                float _ShapeAtlasFrames;
                float4 _ShapeMaskScale;
                float4 _ShapeMaskOffset;
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

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = unity_ObjectToWorld._m03_m13_m23;

                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                );

                float3 worldUp = float3(0, 1, 0);
                float3 toCamera = _WorldSpaceCameraPos - worldPos;
                toCamera.y = 0.0;              
                toCamera = normalize(toCamera);

                float3 camRight = normalize(cross(worldUp, toCamera));
                float3 camUp    = worldUp;

                float3 vertexWorld = worldPos
                    + camRight * (IN.positionOS.x * scale.x * _FlameSize.x)
                    + camUp    * (IN.positionOS.y * scale.y * _FlameSize.y);

                OUT.positionHCS = TransformWorldToHClip(vertexWorld);
                OUT.uv = IN.uv;

                float3 seedBase = _RandomizePerOrnament > 0.5 ? worldPos : float3(0, 0, 0);
                float phaseY = Hash11(seedBase + 53.0) * TWO_PI;
                float angleY = radians(_RotationSpeedY) * _Time.y + phaseY;

                float angleCamera = atan2(toCamera.x, toCamera.z);
                OUT.relativeAngle = angleCamera - angleY + radians(_MaskAngleOffset);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uvA = IN.uv * _NoiseTexA_ST.xy + _NoiseTexA_ST.zw + _ScrollSpeedA.xy * _Time.y;
                float noiseA = SAMPLE_TEXTURE2D(_NoiseTexA, sampler_NoiseTexA, uvA).r;

                float2 uvB = IN.uv * _NoiseTexB_ST.xy + _NoiseTexB_ST.zw + _ScrollSpeedB.xy * _Time.y;
                float noiseB = SAMPLE_TEXTURE2D(_NoiseTexB, sampler_NoiseTexB, uvB).r;

                float2 distortedUV = uvA + (noiseB - 0.5) * _Distortion;
                float flameMask = SAMPLE_TEXTURE2D(_NoiseTexA, sampler_NoiseTexA, distortedUV).r;

                float verticalFalloff = saturate(1.0 - IN.uv.y * 0.8);
                float edgeFalloff = saturate(1.3 - abs(IN.uv.x - 0.5) * 2.0);
                float genericFalloff = lerp(0.6, 1.0, verticalFalloff) * edgeFalloff;

                float2 maskBaseUV = (IN.uv - 0.5) * _ShapeMaskScale.xy + 0.5 + _ShapeMaskOffset.xy;
                bool maskInBounds = maskBaseUV.x >= 0.0 && maskBaseUV.x <= 1.0
                                 && maskBaseUV.y >= 0.0 && maskBaseUV.y <= 1.0;

                float frames = max(_ShapeAtlasFrames, 1.0);
                static const float TAU = 6.28318530718; 
                float wrapped = fmod(IN.relativeAngle + TAU * 0.5, TAU);
                wrapped = wrapped < 0 ? wrapped + TAU : wrapped;
                float frameF = (wrapped / TAU) * frames;

                float frame0 = floor(frameF);
                float frame1 = fmod(frame0 + 1.0, frames);
                float frameBlend = frac(frameF);
                frame0 = fmod(frame0, frames);

                float2 atlasUV0 = float2((frame0 + maskBaseUV.x) / frames, maskBaseUV.y);
                float2 atlasUV1 = float2((frame1 + maskBaseUV.x) / frames, maskBaseUV.y);
                float sample0 = SAMPLE_TEXTURE2D(_ShapeAtlas, sampler_ShapeAtlas, atlasUV0).r;
                float sample1 = SAMPLE_TEXTURE2D(_ShapeAtlas, sampler_ShapeAtlas, atlasUV1).r;
                float shapeMaskSample = maskInBounds ? lerp(sample0, sample1, frameBlend) : 0.0;

                float shapeFalloff = lerp(genericFalloff, genericFalloff * shapeMaskSample, _ShapeMaskStrength);
                flameMask *= shapeFalloff;

                clip(flameMask - _AlphaClip);

                half3 ramp = lerp(_ColorCold.rgb, _ColorMedium.rgb, saturate(flameMask * 2.0));
                ramp = lerp(ramp, _ColorHot.rgb, saturate(flameMask * 2.0 - 1.0));

                half4 col;
                col.rgb = ramp * _Intensity;

                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
