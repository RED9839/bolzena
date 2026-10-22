// 원작 셰이더 그래프 「FX_TEX_Tint」 · 「FX_TEX_Tint_Color」 재현(2026-10-15) — 원작 GLES3 소스 그대로.
//   Tint      : 색 = 주 그림 RGB × Mul × 꼭짓점 색, 알파 = (Main_Tex_Alpha ? 주 그림 R : 주 그림 A) × 꼭짓점 알파, uv = uv0 × Tiling
//   Tint_Color: 주 그림 uv 를 Main_Tex_Rotate 만큼 돌리고, 색 그림(돌림 · ST) × Color_Tex_Mul 을 더 곱한다
//   원작 패스는 늘 SrcAlpha / OneMinusSrcAlpha(재질의 _DstBlend 는 안 씀) — 우리는 미리 곱한 알파로 같은 결과
Shader "Bolzena/FxTint"
{
    Properties
    {
        _Main_Tex ("Main", 2D) = "white" {}
        _Color_Tex ("Color", 2D) = "white" {}
        _Color_Tex_ST2 ("Color ST", Vector) = (1,1,0,0)
        _Main_Tex_Tiling ("Tiling", Vector) = (1,1,0,0)
        _Main_Tex_Mul ("Mul", Float) = 1
        _Main_Tex_Alpha ("R as alpha", Float) = 0
        _Main_Tex_Rotate ("Rotate", Float) = 0
        _Color_Tex_Mul ("Color Mul", Float) = 1
        _Color_Tex_Rotate ("Color Rotate", Float) = 0
        _UseColor ("Tint_Color", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _Main_Tex, _Color_Tex;
            float4 _Color_Tex_ST2, _Main_Tex_Tiling;
            float _Main_Tex_Mul, _Main_Tex_Alpha, _Main_Tex_Rotate, _Color_Tex_Mul, _Color_Tex_Rotate, _UseColor;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            float2 Rot(float2 uv, float deg)
            {
                float r = deg * 0.0174532924, s = sin(r), c = cos(r);
                float2 d = uv - 0.5;
                return float2(dot(d, float2(c, s)), dot(d, float2(-s, c))) + 0.5;
            }
            float4 frag (v2f i) : SV_Target
            {
                float2 muv = (_UseColor != 0 ? Rot(i.uv, _Main_Tex_Rotate) : i.uv) * _Main_Tex_Tiling.xy;
                float4 m = tex2D(_Main_Tex, muv);
                float3 col = m.rgb * _Main_Tex_Mul * i.color.rgb;
                if (_UseColor != 0) col *= tex2D(_Color_Tex, Rot(i.uv, _Color_Tex_Rotate) * _Color_Tex_ST2.xy + _Color_Tex_ST2.zw).rgb * _Color_Tex_Mul;
                float a = saturate((_Main_Tex_Alpha != 0 ? m.r : m.a) * i.color.a);
                return float4(col * a, a);
            }
            ENDCG
        }
    }
}
