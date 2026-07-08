Shader "Custom/CinematicBars"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float _BarSize;
    float _Offset;


    half4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        
        if (uv.y < _BarSize)
            return lerp(half4(0,0,0,1),color,((uv.y - (_BarSize - _Offset)) / _Offset));

         if(uv.y > 1.0 - _BarSize)
            return lerp(half4(0,0,0,1),color,((uv.y - ( 1 - (_BarSize - _Offset))) / -_Offset));
       
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always Blend Off

        Pass
        {
            Name "CinematicBars"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}