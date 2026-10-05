// 집중선 — 한 점으로 모이는 빠른 선(고학년 · 큰 타격). 사각형 하나에 그린다, uv 0~1
Shader "Bolzena/FocusLines"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Center ("Center (uv)", Vector) = (0.5,0.5,0,0)
        _Aspect ("Aspect", Float) = 1.7778
        _Inner ("Inner Radius", Float) = 0.28
        _Density ("Line Count", Float) = 90
        _Speed ("Flicker Speed", Float) = 18
        _Alpha ("Alpha", Range(0,1)) = 1
        _Boost ("Boost", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4 _Center;
            float _Aspect, _Inner, _Density, _Speed, _Alpha, _Boost;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            float hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            float4 frag (v2f i) : SV_Target
            {
                float2 p = (i.uv - _Center.xy) * float2(_Aspect, 1);
                float r = length(p);
                float a = atan2(p.y, p.x) / 6.2831853 + 0.5;
                float n = a * _Density;
                float id = floor(n);
                float f = frac(n);
                float tick = floor(_Time.y * _Speed);
                float rnd = hash(id * 1.37 + tick * 7.13);
                float rnd2 = hash(id * 3.11 + tick * 1.7);
                // 가는 선 — 가운데로 갈수록 가늘어져 사라진다(만화 집중선)
                float inner = _Inner * (0.8 + 0.9 * rnd);
                float m = smoothstep(inner, inner + 0.45, r);
                float w = (0.03 + 0.16 * rnd2) * m;
                float ln = smoothstep(w, w * 0.3, abs(f - 0.5));
                float on = step(0.45, rnd);
                float alpha = ln * m * on * _Alpha * _Color.a;
                return float4(_Color.rgb * _Boost, alpha);
            }
            ENDCG
        }
    }
}
