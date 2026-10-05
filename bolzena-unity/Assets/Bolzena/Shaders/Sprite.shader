// 스프라이트 · 파티클 공용 — 알파/더하기 섞기, HDR 세기(_Boost — 1 을 넘으면 블룸이 먹는다), 단색 채우기(_FillPhase — 흰 번쩍임)
Shader "Bolzena/Sprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Boost ("HDR Boost", Float) = 1
        _FillColor ("Fill Color", Color) = (1,1,1,1)
        _FillPhase ("Fill Phase", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Boost;
            fixed4 _FillColor;
            float _FillPhase;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb = lerp(c.rgb, _FillColor.rgb, _FillPhase);
                c.rgb *= _Boost;
                return c;
            }
            ENDCG
        }
    }
}
