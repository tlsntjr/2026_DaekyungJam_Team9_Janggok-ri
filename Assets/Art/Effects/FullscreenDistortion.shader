Shader "Hidden/Janggokri/FullscreenDistortion"
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
                float wave = sin((uv.y + _Time.y * 0.3) * 40.0) * 0.01 * _Distortion;
                uv.x += wave;
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
