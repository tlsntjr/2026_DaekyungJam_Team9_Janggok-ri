// 방 단위 화면 마스크 (Full Screen Pass Renderer Feature 용)
// 이전 방(A)과 새 방(B), 두 세트의 사각형(각각 최대 4개 — ㄱ자 등 비정형 방은 여러 개 합집합)을
// 동시에 계산해 _RoomBlend(0=A, 1=B)로 크로스페이드한다.
// 도형 위치를 보간하지 않고 "두 유효한 마스크 사이의 투명도"만 섞으므로,
// 겹치는 영역(대부분의 화면)은 전환 내내 그대로 있고 문간(경계) 부분만 부드럽게 넓어져 보인다.
// 값들은 인스펙터가 아니라 RoomRevealDirector가 코드로 SetVectorArray/SetFloat.
// Renderer Feature 설정: Injection Point = Before Rendering Post Processing,
//                        Fetch Color Buffer = ON
Shader "Janggokri/Fullscreen/RoomMask"
{
    Properties
    {
        _MaskColor  ("Mask Color", Color)                  = (0, 0, 0, 1)
        _Softness   ("Edge Softness", Range(0.001, 0.3))   = 0.04
        _Intensity  ("Intensity (0-1)", Range(0, 1))       = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RoomMask"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_ROOM_RECTS 4

            half4 _MaskColor;
            float _Softness;
            float _Intensity;
            float _RoomBlend;   // 0 = 이전 방(A) 마스크, 1 = 새 방(B) 마스크

            // 이전 방(A) / 새 방(B) 각각의 사각형들 (뷰포트 좌표, xy만 사용)
            float4 _RoomMinA[MAX_ROOM_RECTS];
            float4 _RoomMaxA[MAX_ROOM_RECTS];
            float _RoomCountA;

            float4 _RoomMinB[MAX_ROOM_RECTS];
            float4 _RoomMaxB[MAX_ROOM_RECTS];
            float _RoomCountB;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (_Intensity <= 0.001)
                    return col;

                // A(이전 방) 사각형들의 합집합까지의 거리
                float distA = 1e5;
                int countA = (int)_RoomCountA;
                [unroll]
                for (int i = 0; i < MAX_ROOM_RECTS; i++)
                {
                    if (i >= countA) break;
                    float dx = max(_RoomMinA[i].x - uv.x, uv.x - _RoomMaxA[i].x);
                    float dy = max(_RoomMinA[i].y - uv.y, uv.y - _RoomMaxA[i].y);
                    distA = min(distA, max(dx, dy));
                }

                // B(새 방) 사각형들의 합집합까지의 거리
                float distB = 1e5;
                int countB = (int)_RoomCountB;
                [unroll]
                for (int j = 0; j < MAX_ROOM_RECTS; j++)
                {
                    if (j >= countB) break;
                    float dx = max(_RoomMinB[j].x - uv.x, uv.x - _RoomMaxB[j].x);
                    float dy = max(_RoomMinB[j].y - uv.y, uv.y - _RoomMaxB[j].y);
                    distB = min(distB, max(dx, dy));
                }

                float maskA = smoothstep(0.0, _Softness, distA);
                float maskB = smoothstep(0.0, _Softness, distB);
                float mask = lerp(maskA, maskB, saturate(_RoomBlend)) * _Intensity;

                col.rgb = lerp(col.rgb, _MaskColor.rgb, mask);
                return col;
            }
            ENDHLSL
        }
    }
}
