// 바다 표면 일렁임 셰이더 (SpriteRenderer 용) — TideWave의 잔잔한 자매 버전.
// 1) UV를 흐르는 노이즈로 살짝 비틀어 수면 전체가 느리게 일렁이고
// 2) 큰 덩어리의 명암 물결(Swell)이 천천히 지나가며
// 3) 잘게 반짝이는 윤슬(Shimmer)이 수면 위를 흐른다.
// 사용법: 머티리얼 생성 → 이 셰이더 선택 → 바다 SpriteRenderer의 Material에 연결.
// Flow(수면 흐름)를 쓰려면 텍스처 Wrap Mode를 Repeat로 (기본 0이면 무관).
Shader "Janggokri/Sprite/SeaSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Tint            ("Tint", Color)                     = (1, 1, 1, 1)

        [Header(Wobble)]
        _WobbleAmount    ("Wobble Amount", Range(0, 0.1))    = 0.012   // 수면이 일렁이는 세기 (UV 비틀림 — 파도보다 잔잔하게)
        _WobbleScale     ("Wobble Scale", Float)             = 3.0     // 일렁임 덩어리 크기 (클수록 잘게)
        _WobbleSpeed     ("Wobble Speed", Float)             = 0.35

        [Header(Flow)]
        _FlowX           ("Flow X", Float)                   = 0.0     // 수면 전체가 흐르는 방향/속도 (Wrap Repeat 텍스처 전용)
        _FlowY           ("Flow Y", Float)                   = 0.0

        [Header(Swell)]
        _SwellAmount     ("Swell Amount", Range(0, 0.5))     = 0.12    // 큰 명암 물결 세기 (0 = 끔)
        _SwellScale      ("Swell Scale", Float)              = 1.4     // 물결 덩어리 크기
        _SwellSpeed      ("Swell Speed", Float)              = 0.12    // 아주 느리게

        [Header(Shimmer)]
        _ShimmerColor    ("Shimmer Color", Color)            = (0.75, 0.9, 1.0, 1)   // 윤슬 색 (밝은 물빛)
        _ShimmerAmount   ("Shimmer Amount", Range(0, 1))     = 0.35    // 윤슬 밝기 (0 = 끔)
        _ShimmerScale    ("Shimmer Scale", Float)            = 12.0    // 윤슬 입자 크기 (클수록 잘게)
        _ShimmerSpeed    ("Shimmer Speed", Float)            = 0.35
        _ShimmerThreshold("Shimmer Threshold", Range(0, 1))  = 0.52    // 높을수록 윤슬이 드물어짐
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
            Name "SeaSurface"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Tint;
            float _WobbleAmount;
            float _WobbleScale;
            float _WobbleSpeed;
            float _FlowX;
            float _FlowY;
            float _SwellAmount;
            float _SwellScale;
            float _SwellSpeed;
            fixed4 _ShimmerColor;
            float _ShimmerAmount;
            float _ShimmerScale;
            float _ShimmerSpeed;
            float _ShimmerThreshold;

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
                float2 flow = float2(_FlowX, _FlowY) * t;

                // ── 1) UV 일렁임 — 서로 다른 방향으로 흐르는 노이즈 2겹으로 좌표를 살짝 비틈 ──
                float2 p = i.texcoord * _WobbleScale;
                float2 wobble = float2(
                    ValueNoise(p + float2(t * _WobbleSpeed, t * _WobbleSpeed * 0.7)) - 0.5,
                    ValueNoise(p * 1.3 - float2(t * _WobbleSpeed * 0.8, t * _WobbleSpeed * 0.5) + 17.3) - 0.5
                ) * (_WobbleAmount * 2.0);

                fixed4 col = tex2D(_MainTex, i.texcoord + wobble + flow) * i.color;

                // ── 2) 큰 명암 물결 (Swell) — 넓은 덩어리가 천천히 지나가며 수면이 숨쉬는 느낌 ──
                float2 sp = i.texcoord * _SwellScale;
                float swell = ValueNoise(sp + float2(t * _SwellSpeed, t * _SwellSpeed * 0.6))
                            * 0.6
                            + ValueNoise(sp * 1.9 - float2(t * _SwellSpeed * 0.5, t * _SwellSpeed * 0.8) + 7.7)
                            * 0.4;
                col.rgb *= 1.0 + _SwellAmount * (swell - 0.5) * 2.0;

                // ── 3) 윤슬 (Shimmer) — 흐르는 노이즈 2겹의 간섭이 문턱을 넘는 곳만 반짝 ──
                float2 hp = i.texcoord * _ShimmerScale;
                float h1 = ValueNoise(hp + float2(t * _ShimmerSpeed, t * _ShimmerSpeed * 0.55));
                float h2 = ValueNoise(hp * 1.6 - float2(t * _ShimmerSpeed * 0.7, t * _ShimmerSpeed * 0.45) + 3.1);
                float glint = smoothstep(_ShimmerThreshold, _ShimmerThreshold + 0.18, h1 * h2);
                col.rgb += _ShimmerColor.rgb * (glint * _ShimmerAmount) * col.a;

                return col;
            }
            ENDCG
        }
    }
}
