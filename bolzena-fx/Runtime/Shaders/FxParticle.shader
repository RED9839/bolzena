// 원작 이펙트 입자 — 웹판 js/fx-burst.js 의 WebGL 셰이더와 같은 셈.
// 미리 곱한 알파 하나로 섞는다(One, OneMinusSrcAlpha). 더하기 입자는 알파를 0 으로 내보내 One, One 이 된다(uv1.z = 더하기).
// uv1.xy 는 칸 안의 자리(0~1) — 가장자리 uv1.w 만큼(입자 0.05 · 구운 칸 0.22) 걷어 낸다(판에 감던 그림 · 가장자리가 찬 시트 칸이 네모로 보이지 않게). 0.5 면 그대로.
// 2026-10-13: 입자 폭은 그림마다(FxRun.EdgeOf — Tools/fx_edge.py 표, 0.05~0.25). uv1.w 가 음수면 내접 타원으로 걷는다. uv1.z = -1 은 밝기를 알파로(알파 없는 그림).
// 꼭짓점 색은 HDR(1 을 넘는다) — 블룸이 먹는다
Shader "Bolzena/FxParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Edge ("Edge", Float) = 0.05
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
            sampler2D _MainTex;
            float _Edge;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 l : TEXCOORD1; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 l : TEXCOORD1; float4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.l = v.l;
                o.color = v.color;
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float4 x = tex2D(_MainTex, i.uv);
                float add = saturate(i.l.z), lum = saturate(-i.l.z);
                // 알파 없는 그림(검은 바탕)을 알파 섞기로 — 밝기를 알파로, 색은 이미 곱한 것으로 읽는다(uv1.z = -1)
                float xa = lerp(x.a, max(x.r, max(x.g, x.b)), lum);
                float3 xc = lerp(x.rgb * x.a, x.rgb, lum);
                // 가장자리까지 남은 거리(가운데 0.5 · 끝 0) — 폭이 음수면 내접 타원 기준(둥근 불빛이 네모로 잘리지 않게)
                float2 q = min(i.l.xy, 1.0 - i.l.xy);
                float d = i.l.w < 0.0 ? 0.5 * (1.0 - length((i.l.xy - 0.5) * 2.0)) : min(q.x, q.y);
                float f = smoothstep(0.0, max(1e-4, abs(i.l.w)), d);
                float3 rgb = xc * i.color.rgb * i.color.a;
                float a = xa * i.color.a * (1.0 - add);
                return float4(rgb, a) * f;
            }
            ENDCG
        }
    }
}
