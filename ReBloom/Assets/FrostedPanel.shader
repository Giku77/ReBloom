Shader "UI/Frosted Panel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [NoScaleOffset] _BlurTex("Blur Texture", 2D) = "white" {}

        _TintColor ("Tint Color", Color) = (1,1,1,0.5)

        _BlurStrength ("Blur Strength", Float) = 2.0

        // UGUI Mask / RectMask2D용 기본 프로퍼티
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "FrostedUI"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // RectMask2D 지원용
            #pragma multi_compile _ UNITY_UI_CLIP_RECT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float4 screenPos    : TEXCOORD1;
                float3 worldPos     : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BlurTex);
            SAMPLER(sampler_BlurTex);

            float4 _Color;
            float4 _TintColor;
            float  _BlurStrength;

            // RectMask2D가 세팅해주는 클립 영역
            float4 _ClipRect;

            // UnityUI.cginc 안에 있던 함수 그대로 복사
            inline float UnityGet2DClipping (float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                return inside.x * inside.y;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;

                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                // UI의 월드 좌표 (RectMask2D 클리핑용)
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos;

                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                // 화면 좌표 → 0~1
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // 화면 픽셀 크기 (Core.hlsl 에서 제공)
                // _ScreenParams.z = 1/width, .w = 1/height
                float2 texelSize = float2(_ScreenParams.z, _ScreenParams.w);
                float2 offset    = texelSize * _BlurStrength;

                // 간단한 9샘플 박스 블러
                float4 col = 0;
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2(-1, -1));
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2( 0, -1));
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2( 1, -1));

                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2(-1,  0));
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV);
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2( 1,  0));

                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2(-1,  1));
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2( 0,  1));
                col += SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, screenUV + offset * float2( 1,  1));

                col /= 9.0;

                // 틴트 + UI 색 섞기
                col *= _TintColor;
                col *= IN.color;

                // RectMask2D 클리핑
                #ifdef UNITY_UI_CLIP_RECT
                float mask = UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
                col.a *= mask;
                #endif

                return col;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
