Shader "Hidden/URP/FullscreenBlur"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
        _BlurSize("Blur Size", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Overlay"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalRenderPipeline"
        }
        LOD 100

        Pass
        {
            Name "Blur"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;   // x = 1/width, y = 1/height
            float  _BlurSize;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 offset = _MainTex_TexelSize.xy * _BlurSize;

                float4 col = 0;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2(-1, -1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2( 0, -1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2( 1, -1));

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2(-1,  0));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2( 1,  0));

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2(-1,  1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2( 0,  1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * float2( 1,  1));

                return col / 9.0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
