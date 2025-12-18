Shader "Custom/UI_Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlendColor ("Blend Color", Color) = (1,1,1,1)  // 오버레이할 색상
        _BlendIntensity ("Blend Intensity", Range(0, 1)) = 1.0
        
        // UI 마스킹 지원을 위한 필수 프로퍼티
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
            "PreviewType"="Plane"
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
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha  // 일반 알파 블렌딩
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BlendColor;
            float _BlendIntensity;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            // Overlay 블렌딩 함수
            inline fixed OverlayChannel(fixed base, fixed blend)
            {
                return (base < 0.5) 
                    ? (2.0 * base * blend) 
                    : (1.0 - 2.0 * (1.0 - base) * (1.0 - blend));
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 베이스 텍스처 색상
                fixed4 baseColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // Overlay 블렌딩 적용
                fixed3 overlayResult;
                overlayResult.r = OverlayChannel(baseColor.r, _BlendColor.r);
                overlayResult.g = OverlayChannel(baseColor.g, _BlendColor.g);
                overlayResult.b = OverlayChannel(baseColor.b, _BlendColor.b);
                
                // 강도에 따라 원본과 오버레이 결과 보간
                fixed4 finalColor;
                finalColor.rgb = lerp(baseColor.rgb, overlayResult, _BlendIntensity);
                finalColor.a = baseColor.a;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}