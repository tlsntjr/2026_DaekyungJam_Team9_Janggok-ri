// 타이밍 미니게임 판정 존 글로우 (UI Image용)
// 스프라이트 없이 셰이더만으로: 중심은 진한 코어, 테두리는 투명 그라데이션으로 퍼지고
// 시간에 따라 은은하게 맥동(펄스). 가산 블렌드라 어두운 슬라이더 위에서 빛나는 느낌이 남.
// 사용법: 머티리얼 생성 → 이 셰이더 선택 → 판정 존 Image의 Material 슬롯에 연결 (Source Image는 None이어도 됨)
Shader "Janggokri/UI/HitZoneGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color        ("Glow Color", Color)              = (1.0, 0.85, 0.4, 1.0)
        _CoreSize     ("Core Size", Range(0, 1))         = 0.35   // 중심의 진한 영역 비율
        _EdgeSoftness ("Edge Softness", Range(0.01, 1))  = 0.6    // 테두리 그라데이션 폭
        _PulseSpeed   ("Pulse Speed", Float)             = 3.0
        _PulseAmount  ("Pulse Amount", Range(0, 1))      = 0.35   // 맥동 세기 (0이면 고정 밝기)

        // ===== UI 마스크/스텐실 표준 지원 (Mask, RectMask2D 호환) =====
        _StencilComp      ("Stencil Comparison", Float)  = 8
        _Stencil          ("Stencil ID", Float)          = 0
        _StencilOp        ("Stencil Operation", Float)   = 0
        _StencilWriteMask ("Stencil Write Mask", Float)  = 255
        _StencilReadMask  ("Stencil Read Mask", Float)   = 255
        _ColorMask        ("Color Mask", Float)          = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One          // 가산 블렌드 — "빛난다"는 느낌의 핵심
        ColorMask [_ColorMask]

        Pass
        {
            Name "HitZoneGlow"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _CoreSize;
            float _EdgeSoftness;
            float _PulseSpeed;
            float _PulseAmount;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;   // Image 컴포넌트 색과 곱해짐
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 사각형 중심 기준 좌표 (-1 ~ 1)
                float2 p = abs(IN.texcoord - 0.5) * 2.0;

                // 코어 경계 밖으로 나간 거리 — 모서리는 length 덕분에 자연스럽게 둥글어짐
                float d = length(max(p - _CoreSize, 0.0));
                float glow = 1.0 - saturate(d / _EdgeSoftness);
                glow *= glow;   // 제곱 감쇠 — 가장자리로 갈수록 더 부드럽게 사라짐

                // 은은한 맥동
                float pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);

                fixed4 col = IN.color * tex2D(_MainTex, IN.texcoord);
                col.a *= glow * pulse;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
