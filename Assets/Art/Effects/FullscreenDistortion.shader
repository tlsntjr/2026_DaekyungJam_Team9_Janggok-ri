Shader "Janggokri/FullscreenDistortion"
{
    Properties
    {
        _Distortion ("Distortion", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FullscreenDistortion"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Distortion;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float blurSize = _Distortion * 0.3; // 화면 전체에 균일하게 적용 (진단용으로 세게)

                half4 color = 0;
                float2 offsets[9] = {
                    float2(-1, -1), float2(0, -1), float2(1, -1),
                    float2(-1,  0), float2(0,  0), float2(1,  0),
                    float2(-1,  1), float2(0,  1), float2(1,  1)
                };

                [unroll]
                for (int i = 0; i < 9; i++)
                    color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offsets[i] * blurSize);

                return color / 9.0;
            }
            ENDHLSL
        }
    }
}
