Shader "WorldBrush/Impostor/URP"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _Cull("Cull", Float) = 3
        _Surface("Surface", Float) = 1
        _AlphaClip("Alpha Clip", Float) = 1
        [HideInInspector] _ImpostorSize("Impostor Size", Vector) = (1,1,0,0)
        [HideInInspector] _ImpostorPivot("Impostor Pivot", Vector) = (0.5,0.5,0,0)
        [HideInInspector] _ImpostorFacingMode("Impostor Facing Mode", Float) = 0
        [HideInInspector] _WorldInstancingIndirectContractVersion("World Instancing Indirect Contract Version", Float) = 4
        [HideInInspector] _WorldInstancingIndirectLayoutVersion("World Instancing Indirect Layout Version", Float) = 4
        [HideInInspector] _WorldInstancingIndirectLayoutHash("World Instancing Indirect Layout Hash", Float) = 12012013
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }

        Cull [_Cull]
        ZWrite On

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:WorldInstancingIndirect_Setup

            #define HAS_INSTANCE_STATE_BUFFER 1
            #define HAS_CHUNK_STATE_BUFFER 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldInstancingIndirect.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float _Cutoff;
            float2 _ImpostorSize;
            float2 _ImpostorPivot;
            int _ImpostorFacingMode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float lodFade : TEXCOORD7;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint instanceIndex = WorldInstancingIndirect_GetInstanceIndex(v.instanceID);
                float3 positionWS = WorldInstancingIndirect_BuildImpostorPositionWS(v.positionOS.xy, _ImpostorSize, _ImpostorPivot, _ImpostorFacingMode, instanceIndex);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.lodFade = WorldInstancingIndirect_GetLodFade(v.instanceID);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                WorldInstancingIndirect_ApplyImpostorVisibility(i.lodFade);
                clip(c.a - _Cutoff);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:WorldInstancingIndirect_Setup

            #define HAS_INSTANCE_STATE_BUFFER 1
            #define HAS_CHUNK_STATE_BUFFER 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldInstancingIndirect.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float _Cutoff;
            float2 _ImpostorSize;
            float2 _ImpostorPivot;
            int _ImpostorFacingMode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float lodFade : TEXCOORD7;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint instanceIndex = WorldInstancingIndirect_GetInstanceIndex(v.instanceID);
                float3 positionWS = WorldInstancingIndirect_BuildImpostorPositionWS(v.positionOS.xy, _ImpostorSize, _ImpostorPivot, _ImpostorFacingMode, instanceIndex);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.lodFade = WorldInstancingIndirect_GetLodFade(v.instanceID);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                WorldInstancingIndirect_ApplyImpostorVisibility(i.lodFade);
                clip(c.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:WorldInstancingIndirect_Setup

            #define HAS_INSTANCE_STATE_BUFFER 1
            #define HAS_CHUNK_STATE_BUFFER 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldInstancingIndirect.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float _Cutoff;
            float2 _ImpostorSize;
            float2 _ImpostorPivot;
            int _ImpostorFacingMode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float lodFade : TEXCOORD7;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint instanceIndex = WorldInstancingIndirect_GetInstanceIndex(v.instanceID);
                float3 positionWS = WorldInstancingIndirect_BuildImpostorPositionWS(v.positionOS.xy, _ImpostorSize, _ImpostorPivot, _ImpostorFacingMode, instanceIndex);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.lodFade = WorldInstancingIndirect_GetLodFade(v.instanceID);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                WorldInstancingIndirect_ApplyImpostorVisibility(i.lodFade);
                clip(c.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:WorldInstancingIndirect_Setup

            #define HAS_INSTANCE_STATE_BUFFER 1
            #define HAS_CHUNK_STATE_BUFFER 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldInstancingIndirect.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float _Cutoff;
            float2 _ImpostorSize;
            float2 _ImpostorPivot;
            int _ImpostorFacingMode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float lodFade : TEXCOORD7;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint instanceIndex = WorldInstancingIndirect_GetInstanceIndex(v.instanceID);
                float3 positionWS = WorldInstancingIndirect_BuildImpostorPositionWS(v.positionOS.xy, _ImpostorSize, _ImpostorPivot, _ImpostorFacingMode, instanceIndex);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = normalize(_WorldSpaceCameraPos.xyz - positionWS);
                o.lodFade = WorldInstancingIndirect_GetLodFade(v.instanceID);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                WorldInstancingIndirect_ApplyImpostorVisibility(i.lodFade);
                clip(c.a - _Cutoff);
                return half4(normalize(i.normalWS) * 0.5h + 0.5h, 1.0h);
            }
            ENDHLSL
        }
    }
}
