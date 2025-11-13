Shader "UI/IconFX-UI (URP)"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 效果参数（与脚本字段一致）
        _Invert ("Invert", Range(0,1)) = 0
        _FlashStrength ("Flash Strength", Range(0,2)) = 0
        _FlashSpeed ("Flash Speed", Range(0,20)) = 8

        // —— UI/Mask 需要的标准属性 —— //
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
        [HideInInspector]_UIMaskSoftnessX ("UI Mask Softness X", Float) = 0
        [HideInInspector]_UIMaskSoftnessY ("UI Mask Softness Y", Float) = 0
        [HideInInspector]_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        [HideInInspector]_UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Stencil{
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI"
            Tags{ "LightMode"="SRPDefaultUnlit" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float  _Invert;
            float  _FlashStrength;
            float  _FlashSpeed;

            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            float _UseUIAlphaClip;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.worldPosition = v.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // 反色混合
                fixed3 inv = 1.0 - col.rgb;
                col.rgb = lerp(col.rgb, inv, saturate(_Invert));

                // 闪烁：|sin| * 强度
                fixed flash = abs(sin(_Time.y * _FlashSpeed)) * _FlashStrength;
                col.rgb = saturate(col.rgb + flash);

                // RectMask2D 裁剪
                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                // 软裁剪
                #ifdef UNITY_UI_ALPHACLIP
                    clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
