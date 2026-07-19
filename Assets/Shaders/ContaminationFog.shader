// 오염도 비례 전역 안개 (Full Screen Pass Renderer Feature 용)
// 스크롤되는 노이즈 2겹을 겹쳐 "흐르는 해무" 느낌을 만든다.
// _Density 를 코드(ContaminationEffectsDirector 등)에서 0~1로 구동하면 됨.
// Renderer Feature 설정: Injection Point = Before Rendering Post Processing,
//                        Fetch Color Buffer = ON (꺼져 있으면 화면이 검게 나옴)
Shader "Janggokri/Fullscreen/ContaminationFog"
{
    Properties
    {
        _FogColor   ("Fog Color", Color)              = (0.58, 0.63, 0.57, 1)
        _Density    ("Density (0-1)", Range(0, 1))    = 0
        _NoiseScale ("Noise Scale", Float)            = 3.0
        _FlowSpeed  ("Flow Speed", Float)             = 0.04
        _Contrast   ("Noise Contrast", Range(0.5, 4)) = 1.6
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "ContaminationFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 _FogColor;
            float _Density;
            float _NoiseScale;
            float _FlowSpeed;
            float _Contrast;

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

            // 4옥타브 fbm — 층을 쌓아 구름 같은 큰 덩어리 + 잔결을 동시에 만든다
            float Fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    v += amp * ValueNoise(p);
                    p = p * 2.03 + 17.7;
                    amp *= 0.5;
                }
                return v;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (_Density <= 0.001)
                    return col;

                // 화면 비율 보정 (안 하면 노이즈가 가로로 눌린다)
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = uv * float2(aspect, 1.0) * _NoiseScale;
                float t = _Time.y * _FlowSpeed;

                // 서로 다른 방향·배율로 흐르는 2겹 — 단일 스크롤보다 "휘도는" 느낌이 남
                float n1 = Fbm(p + float2(t, t * 0.6));
                float n2 = Fbm(p * 1.7 - float2(t * 0.8, t * 0.3) + 5.2);
                float n = n1 * 0.65 + n2 * 0.35;

                n = saturate(pow(abs(n), _Contrast) * 1.6);
                float fog = saturate(n * _Density * 1.4);
                // 고밀도에선 노이즈 골짜기도 최소한 이만큼은 덮이도록 바닥값 보장
                fog = max(fog, _Density * _Density * 0.35);

                col.rgb = lerp(col.rgb, _FogColor.rgb, fog);
                return col;
            }
            ENDHLSL
        }
    }
}
