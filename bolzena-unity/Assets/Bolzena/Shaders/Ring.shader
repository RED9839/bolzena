// 둥근 게이지 링 — 사각형(uv 0~1) 위에 고리를 그린다. 12시에서 시계 방향으로 _Fill 만큼 채우고, 나머지는 _Back.
// 가장자리는 화면 미분(fwidth)으로 매끈하게 — 폰에서도 4K 에서도 같은 굵기로 선명하다.
Shader "Bolzena/Ring"
{
    Properties
    {
        _Color ("Fill", Color) = (1,0.85,0.5,1)
        _Back ("Back", Color) = (0,0,0,0.5)
        _Fill ("Fill 0~1", Range(0,1)) = 0.5
        _Inner ("Inner radius 0~0.5", Range(0,0.5)) = 0.4
        _Outer ("Outer radius 0~0.5", Range(0,0.5)) = 0.5
        _Boost ("HDR Boost", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color, _Back;
            float _Fill, _Inner, _Outer, _Boost;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv - 0.5;
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float r = length(i.uv);
                float aa = max(fwidth(r), 1e-4);
                float ring = saturate((r - _Inner) / aa + 0.5) * saturate((_Outer - r) / aa + 0.5);
                // 12시 = 0, 시계 방향으로 1
                float a = atan2(i.uv.x, i.uv.y) / 6.2831853;
                a = a < 0 ? a + 1 : a;
                float da = max(fwidth(a), 1e-4);
                float on = _Fill >= 0.999 ? 1 : saturate((_Fill - a) / da + 0.5) * step(0.001, _Fill);
                float4 c = lerp(_Back, float4(_Color.rgb * _Boost, _Color.a), on);
                c.a *= ring;
                return c;
            }
            ENDCG
        }
    }
}
