// 빛줄기 — 가운데서 퍼지는 도는 햇살(신탁 카드 뒤 · 보스 등장 · 승리). 사각형 하나, uv 0~1, 더하기
Shader "Bolzena/Rays"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.85,0.4,1)
        _Count ("Ray Count", Float) = 14
        _Spin ("Spin", Float) = 0.25
        _Sharp ("Sharpness", Float) = 6
        _Inner ("Core", Float) = 0.12
        _Outer ("Falloff", Float) = 0.5
        _Alpha ("Alpha", Range(0,1)) = 1
        _Boost ("Boost", Float) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Count, _Spin, _Sharp, _Inner, _Outer, _Alpha, _Boost;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                float r = length(p) * 2;
                float a = atan2(p.y, p.x);
                float t = _Time.y * _Spin;
                float r1 = pow(abs(sin(a * _Count * 0.5 + t * 6.2831)), _Sharp);
                float r2 = pow(abs(sin(a * _Count * 0.5 * 1.618 - t * 4.1)), _Sharp * 1.5);
                float rays = saturate(r1 * 0.7 + r2 * 0.5);
                float fall = 1 - smoothstep(_Inner, _Outer * 2, r);
                float core = 1 - smoothstep(0, _Inner * 1.6, r);
                float v = saturate(rays * fall + core * 0.8) * fall;
                return float4(_Color.rgb * _Boost, v * _Alpha * _Color.a);
            }
            ENDCG
        }
    }
}
