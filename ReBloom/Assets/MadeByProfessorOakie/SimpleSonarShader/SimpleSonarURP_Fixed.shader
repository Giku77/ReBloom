Shader "Universal Render Pipeline/SimpleSonarURP_Fixed"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _RingColor("Ring Color", Color) = (1,1,1,1)
        _RingColorIntensity("Ring Color Intensity", Float) = 2
        _RingSpeed("Ring Speed", Float) = 1
        _RingWidth("Ring Width", Float) = 0.1
        _RingIntensityScale("Ring Range", Float) = 1
        _RingTex("Ring Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"
        }
        LOD 200

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ----- 머터리얼 프로퍼티는 CBUFFER 안에 넣기 -----
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _RingColor;
                float  _RingColorIntensity;
                float  _RingSpeed;
                float  _RingWidth;
                float  _RingIntensityScale;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_RingTex); SAMPLER(sampler_RingTex);

            // 링 데이터용 버퍼
            CBUFFER_START(SonarData)
                float4 _hitPts[20];
                float  _Intensity[20];
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS  = TransformWorldToHClip(worldPos);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.worldPos    = worldPos;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 worldPos = IN.worldPos;

                float3 albedo    = 0;
                float  ringAlpha = 0;

                [unroll]
                for (int i = 0; i < 20; i++)
                {
                    float3 hitPos = _hitPts[i].xyz;
                    float  startT = _hitPts[i].w;

                    float  d         = distance(hitPos, worldPos);
                    float  intensity = _Intensity[i] * _RingIntensityScale;
                    if (intensity <= 0.0f)
                        continue;

                    float  ringCenter = (_Time.y - startT) * _RingSpeed;
                    if (d >= ringCenter || d <= ringCenter - _RingWidth)
                        continue;

                    float  val = 1.0f - (d / max(intensity, 0.0001f));
                    val = saturate(val);

                    float posInRing = (d - (ringCenter - _RingWidth)) / _RingWidth;

                    // 각도축은 일단 0.5로 고정해도 충분 (텍스처가 1D 라인이라면)
                    float ringSample = SAMPLE_TEXTURE2D(
                        _RingTex, sampler_RingTex,
                        float2(1.0f - posInRing, 0.5f)
                    ).r;

                    val *= ringSample;

                    float3 col = _RingColor.rgb * val;
                    albedo     = max(albedo, col);
                    ringAlpha  = max(ringAlpha, val);
                }

                return half4(albedo * _RingColorIntensity, saturate(ringAlpha));
            }

            ENDHLSL
        }
    }

    FallBack Off
}
