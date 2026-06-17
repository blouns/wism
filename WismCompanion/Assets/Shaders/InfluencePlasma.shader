// Plasma heat shader for the spatial overlay (V2). Blit material: samples the packed influence
// field (R = tension remapped to 0..1, G = friendly, B = enemy) with a flowing domain-warp, maps
// it to a bright diverging palette, and writes straight RGBA so UI Toolkit can alpha-blend it over
// the map. Glow pushes hot cores into HDR so an optional URP Bloom volume makes them sparkle.
Shader "Wism/InfluencePlasma"
{
    Properties
    {
        _MainTex ("Field (R=tension01,G=friendly,B=enemy)", 2D) = "black" {}
        _Opacity ("Opacity", Range(0,1)) = 0.75
        _FlowSpeed ("Flow Speed", Float) = 0.12
        _FlowScale ("Flow Scale", Float) = 4.0
        _Warp ("Warp Amount", Float) = 0.06
        _Glow ("Glow", Float) = 1.2
        _Channel ("Channel (0=tension,1=friendly,2=enemy)", Float) = 0
        [HDR] _FriendlyA ("Friendly Near", Color) = (0.15, 0.85, 1.0, 1.0)
        [HDR] _FriendlyB ("Friendly Far", Color) = (0.25, 0.45, 1.0, 1.0)
        [HDR] _EnemyA ("Enemy Near", Color) = (1.0, 0.45, 0.1, 1.0)
        [HDR] _EnemyB ("Enemy Far", Color) = (1.0, 0.12, 0.12, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend Off
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Opacity, _FlowSpeed, _FlowScale, _Warp, _Glow, _Channel;
            float4 _FriendlyA, _FriendlyB, _EnemyA, _EnemyB;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float a = hash(i), b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1)), d = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                for (int k = 0; k < 4; k++) { s += a * vnoise(p); p *= 2.0; a *= 0.5; }
                return s;
            }

            float3 friendlyRamp(float m) { return lerp(_FriendlyA.rgb, _FriendlyB.rgb, m); }
            float3 enemyRamp(float m)    { return lerp(_EnemyA.rgb, _EnemyB.rgb, m); }

            float4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;

                // Domain-warp the sample point with flowing fbm so the field looks fluid.
                float2 flow = float2(
                    fbm(i.uv * _FlowScale + t * _FlowSpeed),
                    fbm(i.uv * _FlowScale - t * _FlowSpeed + 5.2));
                float2 uv = i.uv + (flow - 0.5) * _Warp;

                float4 f = tex2D(_MainTex, uv);
                float tension  = f.r * 2.0 - 1.0;   // unpack 0..1 -> -1..1
                float friendly = f.g;
                float enemy    = f.b;

                float mag;
                float3 col;
                if (_Channel < 0.5)      { mag = abs(tension); col = tension >= 0.0 ? friendlyRamp(mag) : enemyRamp(mag); }
                else if (_Channel < 1.5) { mag = friendly;     col = friendlyRamp(mag); }
                else                     { mag = enemy;        col = enemyRamp(mag); }

                float shimmer = 0.85 + 0.15 * sin(t * 2.0 + (i.uv.x + i.uv.y) * 20.0);
                float a = smoothstep(0.02, 0.6, mag) * _Opacity * shimmer;

                col *= _Glow;
                return float4(col, saturate(a));
            }
            ENDCG
        }
    }
    Fallback Off
}
