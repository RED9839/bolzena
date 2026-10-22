// 원작 셰이더 그래프 「Shader Graphs/FX_Master」 재현(2026-10-15) — 원작 꾸러미 안 GLES3 소스(투명 변형)를 그대로 옮긴 셈.
//   주 그림 UV = 회전((노이즈로 비튼) uv0) × Custom2.xy + 시간 × 흐름 + Custom1.xy
//   알파 = (Main_Tex_Alpha ? 주 그림 A : 주 그림 R) × 디졸브(Custom1.z 문턱 · smoothstep) × 가리개 R × 꼭짓점 알파
//   색   = (주 그림 ^ Pow, Flat 이면 1) × 색 램프 × Custom1.w × 꼭짓점 색
// 꼭짓점: TEXCOORD0 = uv0, TEXCOORD1 = Custom1, TEXCOORD2 = Custom2(쓰면), COLOR = 입자 색. 섞기는 우리 판과 같이 미리 곱한 알파 하나(_Add = 1 이면 더하기)
Shader "Bolzena/FxMaster"
{
    Properties
    {
        _Main_Tex ("Main", 2D) = "white" {}
        _Mask_Tex ("Mask", 2D) = "white" {}
        _Dissolve_Tex ("Dissolve", 2D) = "white" {}
        _Color_Tex ("Color", 2D) = "white" {}
        _Noise_Tex ("Noise", 2D) = "gray" {}
        _Main_Tex_ST2 ("Main ST", Vector) = (1,1,0,0)
        _Mask_Tex_ST2 ("Mask ST", Vector) = (1,1,0,0)
        _Dissolve_Tex_ST2 ("Dissolve ST", Vector) = (1,1,0,0)
        _Color_Tex_ST2 ("Color ST", Vector) = (1,1,0,0)
        _Noise_Tex_ST2 ("Noise ST", Vector) = (1,1,0,0)
        _Use_Custom2 ("", Float) = 0
        _Main_Tex_Gray ("", Float) = 0
        _Main_Tex_Pow ("", Float) = 1
        _Main_Tex_Alpha ("", Float) = 0
        _Main_FlatColor ("", Float) = 0
        _Main_Tex_Rotation ("", Float) = 0
        _Main_Tex_Panning ("", Vector) = (0,0,0,0)
        _Dissolve_Smooth ("", Float) = 0
        _Use_Dissovlve_Tex ("", Float) = 0
        _Dissolve_Tex_Rotation ("", Float) = 0
        _Color_Tiling ("", Vector) = (1,1,0,0)
        _Color_Tex_Rotation ("", Float) = 0
        _Color_Gra ("", Float) = 0
        _Color_Pow ("", Float) = 1
        _Color_Affect ("", Float) = 1
        _Use_Noise ("", Float) = 0
        _Noise_Polar ("", Float) = 0
        _Noise_Panner ("", Vector) = (0,0,0,0)
        _Mask_NoiseAffect ("", Float) = 0
        _Mask_Tex_Rotation ("", Float) = 0
        _Add ("Additive", Float) = 0
        _SrcBlend ("원작 Src (기록)", Float) = 5
        _DstBlend ("원작 Dst (기록 — 1 이면 _Add)", Float) = 10
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
            sampler2D _Main_Tex, _Mask_Tex, _Dissolve_Tex, _Color_Tex, _Noise_Tex;
            float4 _Main_Tex_ST2, _Mask_Tex_ST2, _Dissolve_Tex_ST2, _Color_Tex_ST2, _Noise_Tex_ST2;
            float _Use_Custom2, _Main_Tex_Gray, _Main_Tex_Pow, _Main_Tex_Alpha, _Main_FlatColor, _Main_Tex_Rotation;
            float4 _Main_Tex_Panning, _Color_Tiling, _Noise_Panner;
            float _Dissolve_Smooth, _Use_Dissovlve_Tex, _Dissolve_Tex_Rotation, _Color_Tex_Rotation, _Color_Gra, _Color_Pow, _Color_Affect;
            float _Use_Noise, _Noise_Polar, _Mask_NoiseAffect, _Mask_Tex_Rotation, _Add;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 c1 : TEXCOORD1; float4 c2 : TEXCOORD2; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 c1 : TEXCOORD1; float4 c2 : TEXCOORD2; float4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; o.c1 = v.c1; o.c2 = v.c2; o.color = v.color;
                return o;
            }
            // 가운데(0.5) 둘레로 도 단위 돌리기 — 원작 셈과 같은 방향
            float2 Rot(float2 uv, float deg)
            {
                float r = deg * 0.0174532924, s = sin(r), c = cos(r);
                float2 d = uv - 0.5;
                return float2(dot(d, float2(c, s)), dot(d, float2(-s, c))) + 0.5;   // 원작: x = d·(cos, sin), y = d·(−sin, cos)
            }
            float2 ST(float2 uv, float4 st) { return uv * st.xy + st.zw; }
            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                // 노이즈 — 극좌표(원작: 반지름 × 2, 각 / 2π)면 그 자리로 읽는다
                float2 d0 = i.uv - 0.5;
                float2 polar = float2(length(d0) * 2.0, atan2(d0.y, d0.x) * 0.159235656);
                float2 nuv = (_Noise_Polar != 0 ? polar : i.uv) + t * _Noise_Panner.xy;
                float2 nz = tex2D(_Noise_Tex, ST(nuv, _Noise_Tex_ST2)).xy * 2.0 - 1.0;
                float nAmt = _Use_Custom2 != 0 ? i.c2.z : 0.0;
                float2 tile = _Use_Custom2 != 0 ? i.c2.xy : float2(1, 1);
                float2 uvN = _Use_Noise != 0 ? (i.uv + nAmt * nz) : i.uv;
                float2 uvM = _Mask_NoiseAffect != 0 ? uvN : i.uv;
                float mask = tex2D(_Mask_Tex, ST(Rot(uvM, _Mask_Tex_Rotation), _Mask_Tex_ST2)).r;
                float dis = tex2D(_Dissolve_Tex, ST(Rot(uvN, _Dissolve_Tex_Rotation), _Dissolve_Tex_ST2)).r;
                float2 muv = Rot(uvN, _Main_Tex_Rotation) * tile + (t * _Main_Tex_Panning.xy + i.c1.xy);
                float4 m = tex2D(_Main_Tex, ST(muv, _Main_Tex_ST2));
                // 알파 — 디졸브 문턱(Custom1.z)을 smoothstep 으로
                float a0 = _Main_Tex_Alpha != 0 ? m.a : m.r;
                float src = _Use_Dissovlve_Tex != 0 ? dis : a0;
                float lo = i.c1.z * (_Dissolve_Smooth + 1.0) - _Dissolve_Smooth;
                float hi = i.c1.z * (_Dissolve_Smooth + 1.0);
                float k = saturate((src - lo) / max(1e-4, hi - lo));
                k = min(k * k * (3.0 - 2.0 * k), 1.0);
                float alpha = _Main_FlatColor != 0 ? k * ceil(a0) : a0 * k;
                alpha = saturate(alpha * i.color.a * mask);
                // 색 — 회색 · 거듭제곱 · 단색, 색 램프(Gra = 밝기로 램프를 읽음, 아니면 uv 로)
                float3 base = _Main_Tex_Gray != 0 ? dot(m.rgb, float3(0.3, 0.59, 0.11)).xxx : m.rgb;
                base = _Main_FlatColor != 0 ? float3(1, 1, 1) : pow(max(base, 1e-5), _Main_Tex_Pow);
                float g = max(min(pow(max(m.r * _Color_Affect, 1e-5), _Color_Pow), 1.0), 0.05);
                float3 rampG = tex2D(_Color_Tex, ST(Rot(float2(min(g, 0.9), 0.0), _Color_Tex_Rotation), _Color_Tex_ST2)).rgb;
                float3 rampU = tex2D(_Color_Tex, ST(Rot(i.uv * _Color_Tiling.xy, _Color_Tex_Rotation), _Color_Tex_ST2)).rgb;
                rampU = min(pow(max(rampU * _Color_Affect, 1e-5), _Color_Pow), 1.0);
                float3 col = base * (_Color_Gra != 0 ? rampG : rampU) * i.c1.w * i.color.rgb;
                // 미리 곱한 알파 하나 — 더하기는 알파를 0 으로(One, One)
                return float4(col * alpha, alpha * (1.0 - _Add));
            }
            ENDCG
        }
    }
}
