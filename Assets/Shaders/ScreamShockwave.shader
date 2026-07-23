// 인면어 괴성 충격파 (Full Screen Pass Renderer Feature 용)
// 괴성 발원지(뷰포트 좌표)에서 굴절 링이 퍼져나가는 몬헌식 포효 연출.
// 코드에서 구동하는 값:
//   _Center    : Camera.WorldToViewportPoint(인면어 위치) 의 xy
//   _Radius    : 0 → 1.5 정도로 시간에 따라 키움 (링이 화면 밖으로 빠져나갈 때까지)
//   _Intensity : 마스터 페이드 (거리 감쇠·연출 종료 시 0으로)
// 나머지는 머티리얼에서 미리 튜닝해두는 값.
// Renderer Feature 설정: Injection Point = After Rendering Post Processing,
//                        Fetch Color Buffer = ON
Shader "Janggokri/Fullscreen/ScreamShockwave"
{
    Properties
    {
        _Center     ("Center (viewport UV)", Vector)       = (0.5, 0.5, 0, 0)
        _Radius     ("Radius", Range(0, 2))                = 0
        _Intensity  ("Intensity (0-1)", Range(0, 1))       = 0
        _Width      ("Ring Width", Range(0.01, 0.5))       = 0.12
        _Strength   ("Distortion Strength", Range(0, 0.2)) = 0.06
        _Aberration ("Chromatic Aberration", Range(0, 2))  = 0.2
        _RingTint   ("Ring Brighten", Range(0, 0.5))       = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "ScreamShockwave"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _Center;
            float _Radius;
            float _Intensity;
            float _Width;
            float _Strength;
            float _Aberration;
            float _RingTint;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                if (_Intensity <= 0.001 || _Strength <= 0.0)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // 화면 비율 보정 — 링이 타원이 아니라 원으로 퍼지게
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 pos = (uv - _Center.xy) * float2(aspect, 1.0);
                float dist = length(pos);

                // 파면으로부터의 부호 있는 정규화 거리 (음수=파면 안쪽, 양수=바깥쪽)
                float x = (dist - _Radius) / max(_Width, 1e-4);
                float ring = exp(-x * x * 4.0);            // 파면 중심 1, 멀어질수록 0 (가우시안)

                // 유리 렌즈식 S자 굴절: 파면 앞쪽은 바깥으로 밀고 뒤쪽은 안으로 당김(압축-팽창).
                // 한 방향으로만 밀면 뿌연 띠처럼 보이지만, 부호가 바뀌면 물결이 지나가는 유리 느낌이 남.
                // 4.7 = 프로파일 피크 정규화 상수 (_Strength가 실제 최대 굴절량이 되도록)
                float wave = x * ring * _Strength * _Intensity * 4.7;

                float2 dir = dist > 1e-4 ? pos / dist : float2(0.0, 0.0);
                dir.x /= aspect;                            // uv 공간으로 복귀
                float2 offset = dir * wave;

                // 파면 뒤쪽을 끌어와 "밀려나는" 굴절 + 링 위에서만 RGB 분리
                float ab = 1.0 + _Aberration * ring;
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset * ab).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset / ab).b;

                half3 rgb = half3(r, g, b);
                rgb += ring * _RingTint * _Intensity;        // 파면을 살짝 밝혀 링을 읽히게
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
