// 원작 셰이더 그래프 「FX_Shockwave」(_Kind 1) · 「FX_Slash_Dissolve_Color」(_Kind 2) · 「FX_Slash_Dissolve_01」(_Kind 3) · 「FX_Dissolve_Edge」(_Kind 4) 재현(2026-10-16) — 원작 GLES3 소스 그대로.
//   꼭짓점: TEXCOORD0 = uv0, TEXCOORD1 = Custom1, TEXCOORD2 = Custom2, COLOR = 입자 색. 미리 곱한 알파 하나(_Add = 1 이면 더하기)
//   Shockwave 는 재질 섞기(_SrcBlend/_DstBlend), Slash_Dissolve_Color 는 원작 패스가 SrcAlpha / OneMinusSrcAlpha
Shader "Bolzena/FxShock"
{
    Properties
    {
        _Main_Tex ("Main", 2D) = "white" {}
        _Alpha_Tex ("Alpha", 2D) = "white" {}
        _Dissolve_Tex ("Dissolve", 2D) = "white" {}
        _Color_Tex ("Color", 2D) = "white" {}
        _Noise_Tex ("Noise", 2D) = "black" {}
        _Mask_Tex ("Mask", 2D) = "white" {}
        _Distort_Tex ("Distort", 2D) = "gray" {}
        _Color ("Color ramp (Slash)", 2D) = "white" {}
        _Dissolve_Tex_Add ("Dissolve add", 2D) = "black" {}
        _Main_Mask ("Main mask", 2D) = "white" {}
        _Main_Tex_ST2 ("", Vector) = (1,1,0,0)
        _Alpha_Tex_ST2 ("", Vector) = (1,1,0,0)
        _Dissolve_Tex_ST2 ("", Vector) = (1,1,0,0)
        _Noise_Tex_ST2 ("", Vector) = (1,1,0,0)
        _Mask_Tex_ST2 ("", Vector) = (1,1,0,0)
        _Main_Tex_Panning ("", Vector) = (0,0,0,0)
        _Alpha_Panning ("", Vector) = (0,0,0,0)
        _Noise_Panning ("", Vector) = (0,0,0,0)
        _Color_Tiling ("", Vector) = (1,1,0,0)
        _Color_Offset ("", Vector) = (0,0,0,0)
        _Tiling ("", Vector) = (1,1,0,0)
        _Offset ("", Vector) = (0,0,0,0)
        _Tiling_D ("", Vector) = (1,1,0,0)
        _Offset_D ("", Vector) = (0,0,0,0)
        _Main_Tex_Rotation ("", Float) = 0
        _Alpha_Tex_Rotation ("", Float) = 0
        _Color_Rotate ("", Float) = 0
        _Mask_Rotate ("", Float) = 0
        _Alpha_Int ("", Float) = 1
        _Alpha_Pow ("", Float) = 1
        _Color_Affect ("", Float) = 1
        _Color_Pow ("", Float) = 1
        _Color_Power ("", Float) = 1
        _Main_Tex_Pow ("", Float) = 1
        _Color_Gra ("", Float) = 0
        _Mask_Pow ("", Float) = 1
        _Mask_Str ("", Float) = 1
        _Dissolve_Smooth ("", Float) = 0
        _Use_DissolveTex ("", Float) = 0
        _Use_AlphaTex ("", Float) = 0
        _Affected_Noise ("", Float) = 0
        _Main_Alpha ("", Float) = 0
        _Add_Main ("", Float) = 0
        _Edge ("", Float) = 0.02
        Color_3f2a675068714c89b0bea054e216b557 ("Edge color", Color) = (1,1,1,0)
        Vector1_37492e6f4f974286867e7ab21900492e ("Edge mul", Float) = 1
        _Dissolve_Mask_Mul ("", Float) = 1
        _Dissolve_Mask_L_R ("", Float) = 1
        _Main_Tex_Move_Offest ("", Float) = 1
        _Main_Tex_Mul ("", Float) = 1
        _Main_Tex_Alpha ("", Float) = 1
        _Kind ("Kind", Float) = 1
        _Add ("Additive", Float) = 0
        _DstBlend ("원작 Dst (기록)", Float) = 10
        _Data_Gamma ("데이터 그림 감마 되돌림(Kind 3 · FxRun 이 요미 달빛 일렁임 판에만 1)", Float) = 0
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
            sampler2D _Main_Tex, _Alpha_Tex, _Dissolve_Tex, _Color_Tex, _Noise_Tex, _Mask_Tex, _Distort_Tex, _Color, _Dissolve_Tex_Add, _Main_Mask;
            float4 _Main_Tex_ST2, _Alpha_Tex_ST2, _Dissolve_Tex_ST2, _Noise_Tex_ST2, _Mask_Tex_ST2;
            float4 _Main_Tex_Panning, _Alpha_Panning, _Noise_Panning, _Color_Tiling, _Color_Offset, _Tiling, _Offset, _Tiling_D, _Offset_D;
            float _Main_Tex_Rotation, _Alpha_Tex_Rotation, _Color_Rotate, _Mask_Rotate, _Alpha_Int, _Alpha_Pow, _Color_Affect, _Color_Pow, _Color_Power;
            float4 Color_3f2a675068714c89b0bea054e216b557; float Vector1_37492e6f4f974286867e7ab21900492e, _Edge, _Dissolve_Mask_Mul, _Dissolve_Mask_L_R, _Main_Tex_Move_Offest, _Main_Tex_Mul, _Main_Tex_Alpha;
            float _Main_Tex_Pow, _Color_Gra, _Mask_Pow, _Mask_Str, _Dissolve_Smooth, _Use_DissolveTex, _Use_AlphaTex, _Affected_Noise, _Main_Alpha, _Add_Main, _Kind, _Add, _Data_Gamma;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 c1 : TEXCOORD1; float4 c2 : TEXCOORD2; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 c1 : TEXCOORD1; float4 c2 : TEXCOORD2; float4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.c1 = v.c1; o.c2 = v.c2; o.color = v.color; return o; }
            float2 Rot(float2 uv, float deg)
            {
                float r = deg * 0.0174532924, s = sin(r), c = cos(r);
                float2 d = uv - 0.5;
                return float2(dot(d, float2(c, s)), dot(d, float2(-s, c))) + 0.5;
            }
            float3 Pw(float3 x, float p) { return pow(max(x, 1e-5), p); }
            float Smooth(float src, float th, float sm)
            {
                float lo = th * (sm + 1.0) - sm, hi = th * (sm + 1.0);
                float k = saturate((src - lo) / max(1e-4, hi - lo));
                return min(k * k * (3.0 - 2.0 * k), 1.0);
            }
            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float3 col; float a; float add = 0;
                if (_Kind < 1.5)   // FX_Shockwave
                {
                    float2 nz = tex2D(_Noise_Tex, (i.uv + t * _Noise_Panning.xy) * _Noise_Tex_ST2.xy + _Noise_Tex_ST2.zw).xy;
                    float2 muv = (t * _Main_Tex_Panning.xy + i.c1.xy) + Rot(i.uv, _Main_Tex_Rotation) + nz * i.c2.z;
                    float4 m = tex2D(_Main_Tex, muv * _Main_Tex_ST2.xy + _Main_Tex_ST2.zw);
                    float2 auv0 = Rot(i.uv, _Alpha_Tex_Rotation) + t * _Alpha_Panning.xy + i.c2.xy;
                    float2 auv = _Affected_Noise != 0 ? auv0 + nz * i.c2.z : auv0;
                    float3 A = tex2D(_Alpha_Tex, auv * _Alpha_Tex_ST2.xy + _Alpha_Tex_ST2.zw).xyz;
                    float src = _Use_AlphaTex != 0 ? A.y : m.y;
                    float2 duv = (_Use_AlphaTex != 0 ? i.c2.xy : i.c1.xy) + i.uv;
                    float D = tex2D(_Dissolve_Tex, duv * _Dissolve_Tex_ST2.xy + _Dissolve_Tex_ST2.zw).x;
                    src = clamp(_Use_DissolveTex != 0 ? D : src, 1e-4, 1.0);
                    float k = Smooth(src, i.c1.z, _Dissolve_Smooth);
                    float3 Ap = Pw(A, _Alpha_Pow);
                    float ab = _Use_AlphaTex != 0 ? Ap.x * _Alpha_Int : (_Main_Alpha != 0 ? m.w : m.x);
                    float mask = pow(max(tex2D(_Mask_Tex, Rot(i.uv, _Mask_Rotate) * _Mask_Tex_ST2.xy + _Mask_Tex_ST2.zw).x, 1e-5), _Mask_Pow) * _Mask_Str;
                    a = saturate(k * ab * mask * i.color.a);
                    float3 rampU = Pw(tex2D(_Color_Tex, Rot(i.uv, _Color_Rotate) * _Color_Tiling.xy).rgb * _Color_Affect, _Color_Pow);
                    float x = _Add_Main != 0 ? A.x + m.x : m.x;
                    float g = max(min(pow(max(x * _Color_Affect, 1e-5), _Color_Pow), 1.0), 0.05);
                    float3 rampG = tex2D(_Color_Tex, float2(min(g, 0.9), 0.0)).rgb;
                    float3 base = Pw(m.rgb, _Main_Tex_Pow);
                    base = _Add_Main != 0 ? Ap * _Alpha_Int + base : base;
                    col = base * i.c1.w * (_Color_Gra != 0 ? rampG : rampU) * i.color.rgb;
                    add = _Add;
                }
                else if (_Kind > 3.5)   // FX_Dissolve_Edge — uv.x 쪽으로 사라지는 띠, Custom1.xy = 주 그림 밀기 · w = 문턱, 문턱 바로 앞 _Edge 폭은 가장자리 색
                {
                    float D = tex2D(_Dissolve_Tex, i.uv * _Dissolve_Tex_ST2.xy + _Dissolve_Tex_ST2.zw).x;
                    float msk = (_Dissolve_Mask_L_R != 0 ? i.uv.x : 1.0 - i.uv.x) * _Dissolve_Mask_Mul;
                    float on = D * msk + _Edge >= i.c1.w ? 1.0 : 0.0;
                    float edge = on - (D * msk >= i.c1.w ? 1.0 : 0.0);
                    float2 muv = (_Main_Tex_Move_Offest != 0 ? i.c1.xy : i.c1.xy * t) + i.uv;
                    float4 m = tex2D(_Main_Tex, muv * _Main_Tex_ST2.xy + _Main_Tex_ST2.zw);
                    float ma = _Main_Tex_Alpha != 0 ? m.x : m.w;
                    float3 base = m.rgb * _Main_Tex_Mul * i.color.rgb;
                    col = lerp(base, ma * edge * Color_3f2a675068714c89b0bea054e216b557.rgb * Vector1_37492e6f4f974286867e7ab21900492e, edge);
                    a = saturate(on * ma * i.color.a);
                    add = _Add;
                }
                else if (_Kind > 2.5)   // FX_Slash_Dissolve_01 — 값은 모두 입자 Custom 데이터. Custom1.x = 밝기 문턱 · y = 앞끝 거듭제곱 · z = 디졸브 · w = 밝기, Custom2.xy = 일그러뜨림 흐름 · z = 세기 · w = 꼬리 거듭제곱
                {
                    float w = min(pow(max(i.uv.x, 1e-5), i.c2.w), 1.0);
                    float d = tex2D(_Distort_Tex, frac(i.c2.xy * t) + i.uv).x;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    // (10-18 사용자 4차 요미 달 두 개) 데이터 노이즈가 sRGB 로 읽혀 가운데가 0.21 — 원작 식은 0.5 둘레를 가정(일그러뜨림이 한쪽으로 쏠려 달이 통째로 밀렸다). _Data_Gamma 판(요미 Moon_Wave)만
                    if (_Data_Gamma > 0.5) d = LinearToGammaSpaceExact(d);
                #endif
                    float on = d >= i.c1.z ? 1.0 : 0.0;
                    float m = tex2D(_Main_Tex, (d - 0.5) * i.c2.z * w + i.uv).x;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    if (_Data_Gamma > 0.5) m = LinearToGammaSpaceExact(m);   // 알파 그림(_alum)도 데이터 — 감마 펴짐으로 옅어졌다
                #endif
                    float hm = min(pow(max(1.0 - i.uv.x, 1e-5), i.c1.y), 1.0) * m;
                    a = saturate(on * hm * i.color.a);
                    float s = saturate((hm - i.c1.x) / max(1e-4, 1.0 - i.c1.x));
                    col = s * s * (3.0 - 2.0 * s) * i.c1.w * i.color.rgb;
                }
                else   // FX_Slash_Dissolve_Color — 휘는 베기(uv.x 가 베기 길이), Custom1.x = 꼬리 문턱 · y = 앞끝 거듭제곱 · z = 디졸브 · w = 밝기, Custom2 = 일그러뜨림
                {
                    float dsv = tex2D(_Dissolve_Tex, i.uv * _Tiling_D.xy + _Offset_D.xy).y + tex2D(_Dissolve_Tex_Add, i.uv).z;
                    float k = Smooth(dsv, i.c1.z, _Dissolve_Smooth);
                    float dist = (tex2D(_Distort_Tex, frac(i.c2.xy * t) + i.uv).x - 0.5) * i.c2.z;
                    float w = min(pow(max(i.uv.x, 1e-5), i.c2.w), 1.0);
                    float2 muv = (dist * w + i.uv) * _Tiling.xy + _Offset.xy;
                    float body = tex2D(_Main_Tex, muv).x * tex2D(_Main_Mask, i.uv).x;
                    float head = min(pow(max(1.0 - i.uv.x, 1e-5), i.c1.y), 1.0);
                    a = saturate(head * body * k * i.color.a);
                    float s = saturate((head - i.c1.x) / max(1e-4, 1.0 - i.c1.x));
                    s = s * s * (3.0 - 2.0 * s) * i.c1.w;
                    col = s * tex2D(_Color, i.uv * _Color_Tiling.xy + _Color_Offset.xy).rgb * _Color_Power * i.color.rgb;
                }
                return float4(col * a, a * (1.0 - add));
            }
            ENDCG
        }
    }
}
