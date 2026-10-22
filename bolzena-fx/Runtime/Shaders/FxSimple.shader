// 원작 파티클 그대로 조립(FxPsBuild)용 단순 입자 셰이더 — 원작 Mobile/Particles · 아틀라스 재질과 같은 셈: 색 = 그림 × 꼭짓점 색.
//   _Add = 1 이면 더하기(One, One), 아니면 알파 섞기. 미리 곱한 알파 하나로 섞는다(우리 다른 셰이더와 같은 판에서 차례가 섞여도 같게)
Shader "Bolzena/FxSimple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Add ("Additive", Float) = 0
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
            sampler2D _MainTex; float _Add;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            float4 frag (v2f i) : SV_Target
            {
                float4 x = tex2D(_MainTex, i.uv) * i.color;
                float a = saturate(x.a);
                return float4(x.rgb * a, a * (1.0 - _Add));
            }
            ENDCG
        }
    }
}
