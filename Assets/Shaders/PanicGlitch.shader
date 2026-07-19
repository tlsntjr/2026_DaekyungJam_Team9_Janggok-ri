// 패닉/조우 순간용 글리치 (Full Screen Pass Renderer Feature 용)
// 색수차 + 필름 그레인 + 주사선 찢김 + 탈색을 _Intensity 하나로 묶어 구동.
// 점프스케어·기억의 균열·오염 3단계 진입 등 "확 덮치는" 순간에
// _Intensity 를 0 → 1 → 0 으로 짧게 튕겨주는 용도.
// Renderer Feature 설정: Injection Point = After Rendering Post Processing,
//                        Fetch Color Buffer = ON
Shader "Janggokri/Fullscreen/PanicGlitch"
{
    Properties
    {
        _Intensity    ("Intensity (0-1)", Range(0, 1))         = 0
        _Aberration   ("Chromatic Aberration", Range(0, 0.05)) = 0.012
        _GrainAmount  ("Grain Amount", Range(0, 0.5))          = 0.18
        _JitterAmount ("Line Jitter Amount", Range(0, 0.2))    = 0.04
        _LineDensity  ("Line Density", Float)                  = 90
        _Desaturation ("Desaturation", Range(0, 1))            = 0.6
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "PanicGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _Aberration;
            float _GrainAmount;
            float _JitterAmount;
            float _LineDensity;
            float _Desaturation;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                if (_Intensity <= 0.001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // ── 주사선 찢김: 일부 가로줄만, 짧은 간격으로 갱신되며 옆으로 밀림 ──
                float row = floor(uv.y * _LineDensity);
                float frame = floor(_Time.y * 24.0);                     // 24fps 느낌으로 끊어서 갱신
                float gate = step(0.75, Hash21(float2(row, frame)));     // 약 25%의 줄만 찢김
                float jitter = (Hash21(float2(row, frame + 31.7)) - 0.5)
                               * 2.0 * _JitterAmount * gate * _Intensity;
                uv.x = saturate(uv.x + jitter);

                // ── 중심 기준 방사형 색수차 ──
                float2 fromCenter = uv - 0.5;
                float2 ab = fromCenter * _Aberration * _Intensity * 30.0;
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + ab)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv - ab)).b;
                half3 rgb = half3(r, g, b);

                // ── 필름 그레인 ──
                float grain = Hash21(uv * _ScreenParams.xy + frac(_Time.y) * 61.7) - 0.5;
                rgb += grain * _GrainAmount * _Intensity;

                // ── 탈색: 패닉 순간 색이 빠지며 낡은 필름처럼 ──
                half lum = dot(rgb, half3(0.299, 0.587, 0.114));
                rgb = lerp(rgb, lum.xxx, _Desaturation * _Intensity);

                return half4(saturate(rgb), 1.0);
            }
            ENDHLSL
        }
    }
}
