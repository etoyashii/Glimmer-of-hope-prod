Shader "Custom/MagicFire"
{
    Properties
    {
        [Header(Colors)]
        _ColorCore     ("Couleur Coeur",       Color) = (1.0, 1.0, 1.0, 1.0)
        _ColorMid      ("Couleur Milieu",      Color) = (0.4, 0.1, 1.0, 1.0)
        _ColorOuter    ("Couleur Exterieur",   Color) = (0.0, 0.5, 1.0, 0.0)

        [Header(Fire Shape)]
        _NoiseScale    ("Echelle du bruit",    Float) = 3.0
        _NoiseSpeed    ("Vitesse du bruit",    Float) = 1.2
        _FireHeight    ("Hauteur du feu",      Float) = 1.5
        _Sharpness     ("Nettete",             Float) = 2.5
        _Distortion    ("Distorsion",          Float) = 0.25

        [Header(Emission)]
        _EmissionPower ("Puissance emission",  Float) = 3.0
        _AlphaCutoff   ("Seuil alpha",         Range(0,1)) = 0.05

        [Header(Particles)]
        _SparkCount    ("Particules actives",  Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MagicFirePass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //  Properties 
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorCore;
                float4 _ColorMid;
                float4 _ColorOuter;
                float  _NoiseScale;
                float  _NoiseSpeed;
                float  _FireHeight;
                float  _Sharpness;
                float  _Distortion;
                float  _EmissionPower;
                float  _AlphaCutoff;
                float  _SparkCount;
            CBUFFER_END

            // Structs 
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Noise functions 
            // Hash 2D → 1D
            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            // Value noise
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // FBM 
            float fbm(float2 p)
            {
                float v    = 0.0;
                float amp  = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 5; i++)
                {
                    v    += amp  * valueNoise(p * freq);
                    amp  *= 0.5;
                    freq *= 2.1;
                }
                return v;
            }

            // Vertex 
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            // Fragment
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float height = uv.y;

                float t       = _Time.y * _NoiseSpeed;
                float distort = (valueNoise(float2(height * 2.0, t * 0.7)) - 0.5)
                                * _Distortion * (1.0 - height);
                float2 uvD    = uv + float2(distort, 0.0);

                float2 noiseUV = uvD * _NoiseScale + float2(0.0, -t);
                float  noise   = fbm(noiseUV);

                float cx      = abs(uv.x - 0.5) * 2.0;            
                float sideMask= saturate(1.0 - pow(cx, 1.8));

                float heightMask = saturate(1.0 - pow(height / _FireHeight, _Sharpness));

                float shape = noise * sideMask * heightMask;

                float alpha = saturate(shape * 2.5);
                clip(alpha - _AlphaCutoff);

                float gradA = saturate(shape * 2.5 - 0.5);   
                float gradB = saturate(shape * 2.0);          

                float3 col = lerp(_ColorOuter.rgb, _ColorMid.rgb,  gradB);
                       col = lerp(col,             _ColorCore.rgb, gradA);

                col *= _EmissionPower;

                float flicker = 0.9 + 0.1 * sin(_Time.y * 17.3 + noise * 6.2);
                col *= flicker;

                if (_SparkCount > 0.0)
                {
                    float sparks = 0.0;
                    for (float k = 0.0; k < 6.0; k++)
                    {
                        if (k >= _SparkCount) break;
                        float  seed   = k * 1.732 + 0.5;
                        float  spX    = frac(seed * 0.3183) * 0.8 + 0.1;
                        float  speed  = 0.4 + frac(seed * 0.7654) * 0.6;
                        float  spY    = frac(_Time.y * speed + seed);
                        float2 spPos  = float2(spX, spY * _FireHeight);
                        float  spDist = length(uv - spPos) * 30.0;
                        sparks += exp(-spDist * spDist) * (1.0 - spY);
                    }
                    col   += _ColorCore.rgb * sparks * 2.0;
                    alpha  = saturate(alpha + sparks * 0.5);
                }

                return half4(col, alpha * IN.color.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
