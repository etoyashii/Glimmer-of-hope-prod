Shader "Custom/StencilWrite"
{
    Properties
    {
        _PlayerPos ("Player Position", Vector) = (0,0,0,0)
        _Radius ("Radius", float) = 2
        _IsActive ("IsActive", float) = 1
    }

   SubShader
   {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry-1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StencilMask"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            ColorMask 0
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes 
            { 
                float4 positionOS : POSITION;
            };
            
            struct Varyings 
            { 
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PlayerPos;
                float _Radius;
                float _IsActive;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target 
            {
                clip(_IsActive - 0.5);

                float dist = distance(IN.positionWS, _PlayerPos.xyz);
                clip(_Radius - dist);

                return 0;    
            }
            ENDHLSL
        }
   }
}
