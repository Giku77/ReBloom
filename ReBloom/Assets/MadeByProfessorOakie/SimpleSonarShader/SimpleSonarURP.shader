Shader "Universal Render Pipeline/SimpleSonarURP"
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_RingTex); SAMPLER(sampler_RingTex);

            float4 _MainTex_ST;
            float4 _Color;
            float4 _RingColor;
            half   _RingColorIntensity;
            half   _RingSpeed;
            half   _RingWidth;
            half   _RingIntensityScale;

            // C#에서 세팅해주는 배열들
            half4 _hitPts[20];
            half  _Intensity[20];

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.worldPos = worldPos;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float3 worldPos = IN.worldPos;

                // 기본 디스크 색은 안 쓸 거라서 알파 0 기준으로 시작
                float4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;

                // 바닥은 안 보이게: 처음엔 완전히 검정, 알파 0
                float3 albedo  = 0;

                half diffFromRingCol = 9999;   // 처음엔 큰 값
                half ringAlpha       = 0;      // 최종 알파

                [unroll]
                for (int i = 0; i < 20; i++)
                {
                    half3 hitPos = _hitPts[i].xyz;
                    half  startT = _hitPts[i].w;

                    half  d         = distance(hitPos, worldPos);
                    half  intensity = _Intensity[i] * _RingIntensityScale;
                    half  val       = 1 - (d / max(intensity, 0.0001));

                    half ringCenter = (_Time.y - startT) * _RingSpeed;

                    if (d < ringCenter && d > ringCenter - _RingWidth && val > 0)
                    {
                        half posInRing = (d - (ringCenter - _RingWidth)) / _RingWidth;

                        float angle = acos(dot(normalize(worldPos - hitPos), float3(1,0,0)));
                        float ringSample = SAMPLE_TEXTURE2D(
                            _RingTex, sampler_RingTex,
                            float2(1 - posInRing, angle)
                        ).r;

                        val *= ringSample;

                        float3 tmp = _RingColor.rgb * val;

                        half tempDiff =
                            abs(tmp.r - _RingColor.r) +
                            abs(tmp.g - _RingColor.g) +
                            abs(tmp.b - _RingColor.b);

                        if (tempDiff < diffFromRingCol)
                        {
                            diffFromRingCol = tempDiff;
                            albedo = tmp * _RingColorIntensity;
                        }

                        // 링이 강할수록 알파도 크게
                        ringAlpha = max(ringAlpha, val);
                    }
                }

                // 알파는 0~1 로 클램프
                return half4(albedo, saturate(ringAlpha));
            }

            ENDHLSL
        }
    }

    FallBack Off
}
