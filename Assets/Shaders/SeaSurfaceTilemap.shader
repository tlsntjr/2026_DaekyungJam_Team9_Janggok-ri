// 바다 표면 일렁임 셰이더 — 타일맵(TilemapRenderer) 전용 버전.
// SeaSurface(단일 스프라이트용)와 달리 모든 노이즈를 "월드 좌표" 기준으로 계산해서
// 무늬가 타일 경계를 무시하고 바다 전체에 하나로 이어진다.
// 일렁임은 UV 비틀기 대신 "버텍스(타일 꼭짓점) 흔들기"로 표현 —
// 인접 타일이 공유하는 꼭짓점은 같은 월드 좌표 → 같은 노이즈 값 → 이음새 없이 통째로 출렁인다.
// (UV 비틀기는 타일맵에선 아틀라스의 옆 타일 픽셀이 새어 들어와 경계 얼룩이 생기므로 쓰지 않음)
//
// 사용법: 머티리얼 생성 → 이 셰이더 선택 → 바다 Tilemap의 TilemapRenderer Material에 연결.
// 주의: 버텍스가 흔들리므로 바다 가장자리도 살짝 움직인다 — 부두/육지 타일 밑으로 바다를 한 칸 겹쳐 깔 것.
Shader "Janggokri/Tilemap/SeaSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Tint            ("Tint", Color)                     = (1, 1, 1, 1)

        [Header(Vertex Wave)]
        _WaveAmount      ("Wave Amount", Range(0, 0.2))      = 0.045   // 꼭짓점이 흔들리는 거리 (월드 유닛 — 너무 크면 가장자리 틈)
        _WaveScale       ("Wave Scale", Float)               = 0.6     // 출렁임 덩어리 크기 (클수록 잘게)
        _WaveSpeed       ("Wave Speed", Float)               = 0.5

        [Header(Swell)]
        _SwellAmount     ("Swell Amount", Range(0, 0.5))     = 0.14    // 큰 명암 물결 세기 (0 = 끔)
        _SwellScale      ("Swell Scale", Float)              = 0.35    // 물결 덩어리 크기 (월드 유닛 기준 — 0.35 ≈ 3칸짜리 물결)
        _SwellSpeed      ("Swell Speed", Float)              = 0.12

        [Header(Shimmer)]
        _ShimmerColor    ("Shimmer Color", Color)            = (0.75, 0.9, 1.0, 1)   // 윤슬 색 (밝은 물빛)
        _ShimmerAmount   ("Shimmer Amount", Range(0, 1))     = 0.35    // 윤슬 밝기 (0 = 끔)
        _ShimmerScale    ("Shimmer Scale", Float)            = 2.5     // 윤슬 입자 크기 (클수록 잘게)
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
            Name "SeaSurfaceTilemap"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Tint;
            float _WaveAmount;
            float _WaveScale;
            float _WaveSpeed;
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
                float2 worldPos : TEXCOORD1;   // 프래그먼트 노이즈용 (흔들기 전 원본 월드 좌표)
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
                float t = _Time.y;

                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xy;

                // ── 버텍스 출렁임 — 월드 좌표 기반이라 인접 타일의 공유 꼭짓점이 똑같이 움직여 이음새가 없다 ──
                float2 wp = world.xy * _WaveScale;
                float2 sway = float2(
                    ValueNoise(wp + float2(t * _WaveSpeed, t * _WaveSpeed * 0.7)) - 0.5,
                    ValueNoise(wp * 1.3 - float2(t * _WaveSpeed * 0.8, t * _WaveSpeed * 0.5) + 17.3) - 0.5
                ) * (_WaveAmount * 2.0);
                world.xy += sway;

                o.vertex = mul(UNITY_MATRIX_VP, world);
                o.texcoord = v.texcoord;
                o.color = v.color * _Tint;   // Tilemap Color와 곱
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;

                // 텍스처는 UV 그대로 샘플 — 비틀면 아틀라스 옆 타일이 새어 들어옴
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // ── 큰 명암 물결 (Swell) — 월드 좌표 기반이라 바다 전체에 하나로 이어진 물결이 지나간다 ──
                float2 sp = i.worldPos * _SwellScale;
                float swell = ValueNoise(sp + float2(t * _SwellSpeed, t * _SwellSpeed * 0.6))
                            * 0.6
                            + ValueNoise(sp * 1.9 - float2(t * _SwellSpeed * 0.5, t * _SwellSpeed * 0.8) + 7.7)
                            * 0.4;
                col.rgb *= 1.0 + _SwellAmount * (swell - 0.5) * 2.0;

                // ── 윤슬 (Shimmer) — 흐르는 노이즈 2겹의 간섭이 문턱을 넘는 곳만 반짝 ──
                float2 hp = i.worldPos * _ShimmerScale;
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
