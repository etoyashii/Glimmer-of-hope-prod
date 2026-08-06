Shader "Custom/FlameAura"
{
    Properties
    {
        // --- Mesh principal ---
        _MainColor ("Couleur du mesh", Color) = (0.2,0.2,0.2,1)

        // --- Aura / flammes ---
        _AuraColorInner ("Couleur flamme (coeur)", Color) = (1, 0.9, 0.2, 1) // jaune
        _AuraColorOuter ("Couleur flamme (bord)", Color) = (1, 0.15, 0, 1)   // rouge/orange
        _OutlineWidth ("Epaisseur de base", Range(0, 0.2)) = 0.03
        _NoiseTex ("Texture de bruit", 2D) = "white" {}
        _NoiseScale ("Echelle du bruit", Float) = 3
        _NoiseSpeed ("Vitesse du bruit", Float) = 1.0
        _FlickerStrength ("Force du flicker (pointes)", Range(0, 0.3)) = 0.08
        _EdgeBias ("Concentration aux bords (0=partout,1=pointes)", Range(0,1)) = 0.6
        _Alpha ("Opacite globale de l'aura", Range(0,1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        // ---------- PASSE 1 : le mesh lui-meme ----------
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _MainColor;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _MainColor;
            }
            ENDCG
        }

        // ---------- PASSE 2 : la coque extrudee = l'aura qui flame ----------
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Front      // on affiche l'interieur de la coque inversee
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _NoiseScale;
            float _NoiseSpeed;
            float _FlickerStrength;
            float _OutlineWidth;
            float _EdgeBias;
            fixed4 _AuraColorInner;
            fixed4 _AuraColorOuter;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float  noiseVal : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // Coordonnee utilisee pour lire le bruit : on scale l'UV + le temps
                float2 noiseUV = v.uv * _NoiseScale + float2(0, _Time.y * _NoiseSpeed);
                float n = tex2Dlod(_NoiseTex, float4(noiseUV, 0, 0)).r; // 0..1

                // Le bruit fait varier l'epaisseur d'extrusion -> effet de pointes de flamme
                // EdgeBias : on peut favoriser les zones qui bougent plus fort (pointes)
                float flicker = (n - 0.5) * 2.0; // -1..1
                float width = _OutlineWidth + flicker * _FlickerStrength;
                width = max(width, 0.001);

                float3 extruded = v.vertex.xyz + normalize(v.normal) * width;

                o.pos = UnityObjectToClipPos(float4(extruded, 1));
                o.uv = v.uv;
                o.noiseVal = n;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Degrade coeur (jaune) -> bord (rouge) pilote par le bruit
                fixed4 col = lerp(_AuraColorOuter, _AuraColorInner, saturate(i.noiseVal));
                col.a = _Alpha;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
