// 밀물/파도 스프라이트용 일렁임 셰이더 (SpriteRenderer 용)
// 1) UV를 흐르는 노이즈로 비틀어 스프라이트 형태 자체가 일렁이고 (가장자리가 살아 움직임)
// 2) 어두운 노이즈 얼룩이 표면 위를 흘러 탁하게 끓는 물 느낌을 만든다.
// 사용법: 머티리얼 생성 → 이 셰이더 선택 → 파도 SpriteRenderer의 Material에 연결.
// AdvancingTideWall 하위 파도 스프라이트, 갯벌 물가 데코 등에 공용 사용 가능.
Shader "Janggokri/Sprite/TideWave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Tint          ("Tint", Color)                    = (1, 1, 1, 1)

        [Header(Wobble)]
        _WobbleAmount  ("Wobble Amount", Range(0, 0.1))   = 0.025   // 형태가 일렁이는 세기 (UV 비틀림)
        _WobbleScale   ("Wobble Scale", Float)            = 4.0     // 일렁임 덩어리 크기 (클수록 잘게)
        _WobbleSpeed   ("Wobble Speed", Float)            = 0.6

        [Header(Noise)]
        _NoiseAmount   ("Noise Amount", Range(0, 1))      = 0.35    // 표면 얼룩 세기 (탁함)
        _NoiseScale    ("Noise Scale", Float)             = 7.0
        _NoiseSpeed    ("Noise Speed", Float)             = 0.25
        _NoiseDarkness ("Noise Darkness", Range(0, 1))    = 0.6     // 얼룩이 어두워지는 정도 (0=밝은 얼룩)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "TideWave"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Tint;
            float _WobbleAmount;
            float _WobbleScale;
            float _WobbleSpeed;
            float _NoiseAmount;
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseDarkness;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Tint;   // SpriteRenderer Color와 곱
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;

                // ── 1) UV 일렁임 — 서로 다른 방향으로 흐르는 노이즈 2겹으로 좌표를 비틈 ──
                float2 p = i.texcoord * _WobbleScale;
                float2 wobble = float2(
                    ValueNoise(p + float2(t * _WobbleSpeed, t * _WobbleSpeed * 0.7)) - 0.5,
                    ValueNoise(p * 1.3 - float2(t * _WobbleSpeed * 0.8, t * _WobbleSpeed * 0.5) + 17.3) - 0.5
                ) * (_WobbleAmount * 2.0);

                fixed4 col = tex2D(_MainTex, i.texcoord + wobble) * i.color;

                // ── 2) 표면 노이즈 얼룩 — 탁하게 끓는 물 (흐르는 두 겹의 간섭) ──
                float2 np = i.texcoord * _NoiseScale;
                float n1 = ValueNoise(np + float2(t * _NoiseSpeed, t * _NoiseSpeed * 0.6));
                float n2 = ValueNoise(np * 1.7 - float2(t * _NoiseSpeed * 0.7, t * _NoiseSpeed * 0.4) + 5.2);
                float stain = (n1 * 0.6 + n2 * 0.4);   // 0~1 얼룩 맵

                // 얼룩을 어두운 쪽으로 섞음 (_NoiseDarkness 0이면 밝은 얼룩)
                float shade = lerp(1.0 + _NoiseAmount * (stain - 0.5) * 2.0,   // 밝음/어둠 양방향
                                   1.0 - _NoiseAmount * stain,                  // 어두운 얼룩만
                                   _NoiseDarkness);
                col.rgb *= shade;

                return col;
            }
            ENDCG
        }
    }
}
