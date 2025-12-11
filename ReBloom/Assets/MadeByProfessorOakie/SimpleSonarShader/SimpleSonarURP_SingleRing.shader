Shader "Universal Render Pipeline/SimpleSonarURP_SingleRing"
{
    Properties
    {
        _RingColor("Ring Color", Color) = (0.2, 1, 1, 1)
        _RingColorIntensity("Ring Color Intensity", Float) = 2
        _RingSpeed("Ring Speed", Float) = 8
        _RingWidth("Ring Width", Float) = 0.5
        _RingIntensity("Ring Range", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"
        }

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RingColor;
                float  _RingColorIntensity;
                float  _RingSpeed;
                float  _RingWidth;
                float  _RingIntensity;

                // xyz = 중심, w = 시작 시간
                float4 _RingOriginAndStartT;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 worldPos  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS   = TransformWorldToHClip(worldPos);
                OUT.worldPos     = worldPos;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 worldPos = IN.worldPos;

                float3 center   = _RingOriginAndStartT.xyz;
                float  startT   = _RingOriginAndStartT.w;

                float  d        = distance(center, worldPos);
                float  radius   = (_Time.y - startT) * _RingSpeed;

                // 링 범위 안에만 그리기
                if (radius <= 0.0 || d > radius || d < radius - _RingWidth)
                    return half4(0, 0, 0, 0);

                // 안쪽/바깥쪽에서 서서히 사라지는 느낌
                float t = (d - (radius - _RingWidth)) / max(_RingWidth, 0.0001);
                t = saturate(1.0 - abs(t - 0.5) * 2.0); // 가운데에서 가장 진하게

                // 거리 기반 감쇠
                float distFade = saturate(1.0 - (d / max(_RingIntensity, 0.0001)));

                float alpha = t * distFade;
                float3 col  = _RingColor.rgb * _RingColorIntensity * alpha;

                return half4(col, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
