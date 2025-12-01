Shader "Custom/UI/TwoTextureMultiply"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture 1", 2D) = "white" {}
        _AlphaTex ("Alpha Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _MainTexUV   ("Texture1 UV (ScaleX,ScaleY,OffsetX,OffsetY)", Vector) = (1,1,0,0)
        _AlphaTexUV  ("Alpha UV (ScaleX,ScaleY,OffsetX,OffsetY)",   Vector) = (1,1,0,0)

        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
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
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _AlphaTex;

            fixed4 _BaseColor;

            float4 _MainTexUV;
            float4 _AlphaTexUV;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uvMain   : TEXCOORD0;
                float2 uvAlpha  : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.color = v.color;

                o.uvMain   = v.uv * _MainTexUV.xy   + _MainTexUV.zw;
                o.uvAlpha  = v.uv * _AlphaTexUV.xy  + _AlphaTexUV.zw;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex,   i.uvMain);
                fixed  alphaMap = tex2D(_AlphaTex, i.uvAlpha).r; // User R color for alpha

                // Color = 1st tex * Base color * Vertex Color
                fixed3 rgb = tex.rgb * _BaseColor.rgb * i.color.rgb;

                // A = Base color A * Alpha map * Each texture A * Vertex Color A
                fixed a = _BaseColor.a * alphaMap * tex.a * i.color.a;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
