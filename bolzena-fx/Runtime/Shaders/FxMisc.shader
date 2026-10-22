// 원작 셰이더 그래프 몇 가지 재현(2026-10-15) — 원작 GLES3 소스 그대로. _Kind 로 갈래:
//   1 FX_Simple_Dissolve_Step       : t = lerp(C1.x, C1.y, C1.z), 보임 = (t >= 주 그림 R) ? 0 : 1, 색 = 보임 × C1.w × 꼭짓점 색
//   2 FX_Move_UV_Mask               : uv = 돌림(uv0) × Tiling + C1.xy, 알파 = 가리개 R(uv0) × C1.z × (Alpha ? R : A) × 꼭짓점 알파, 색 = RGB × C1.w × 꼭짓점 색
//   3 FX_Simple_Dissolve_Smoothstep : v = smoothstep((주 그림(또는 R) − t) / (1 − t)), 색 = v × C1.w × 꼭짓점 색, 알파 = v.r × 꼭짓점 알파
//   4 FX_UV_Offset_ab               : uv = uv0 × Tiling + 시간 × UV_Offset, 알파 = 가리개 R × (Alpha ? R : A) × 꼭짓점 알파, 색 = RGB × Power × 꼭짓점 색
// 1 · 2 · 4 는 원작 패스가 SrcAlpha / OneMinusSrcAlpha, 3 은 재질 섞기(_Add = 1 이면 더하기). 미리 곱한 알파로 같은 결과
Shader "Bolzena/FxMisc"
{
    Properties
    {
        _Main_Tex ("Main", 2D) = "white" {}
        _Mask_Tex ("Mask", 2D) = "white" {}
        _Main_Tex_ST2 ("Main ST", Vector) = (1,1,0,0)
        _Main_Tex_Tiling ("Tiling", Vector) = (1,1,0,0)
        _UV_Offset ("UV Offset", Vector) = (0,0,0,0)
        _Main_Tex_Alpha ("R as alpha", Float) = 0
        _Main_Tex_Rotate ("Rotate", Float) = 0
        _Main_Tex_Power ("Power", Float) = 1
        _Main_Tex_Color ("R only", Float) = 0
        _Pixel ("Pixel", Float) = 32
        _Main_Tex_Mul ("Mul", Float) = 1
        _OpacitySaturation ("Opacity Sat", Float) = 1
        _MinMaxSmoothStepDissolve ("MinMax", Vector) = (0,1,0,0)
        _Noise_Tex ("Noise", 2D) = "gray" {}
        _Noise_Tex_ST2 ("Noise ST", Vector) = (1,1,0,0)
        _Noise_Panner ("Noise Panner", Vector) = (0,0,0,0)
        _Noise_Mul ("Noise Mul", Float) = 0
        _Main_Tex_Scale ("Scale", Float) = 1
        _Main_Tex_RGBA_R ("RGBA", Float) = 1
        _Noise_Polar_Radial_Length ("Radial Length", Vector) = (1,1,0,0)
        _Kind ("Kind", Float) = 1
        _Add ("Additive", Float) = 0
        _DstBlend ("원작 Dst (기록)", Float) = 10
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
            sampler2D _Main_Tex, _Mask_Tex, _Noise_Tex;
            float4 _MinMaxSmoothStepDissolve, _Noise_Tex_ST2, _Noise_Panner, _Noise_Polar_Radial_Length;
            float _Pixel, _Main_Tex_Mul, _OpacitySaturation, _Noise_Mul, _Main_Tex_Scale, _Main_Tex_RGBA_R;
            float4 _Main_Tex_ST2, _Main_Tex_Tiling, _UV_Offset;
            float _Main_Tex_Alpha, _Main_Tex_Rotate, _Main_Tex_Power, _Main_Tex_Color, _Kind, _Add;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 c1 : TEXCOORD1; float4 c2 : TEXCOORD2; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 c1 : TEXCOORD1; float4 c2 : TEXCOORD2; float4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.c1 = v.c1; o.c2 = v.c2; o.color = v.color; return o; }
            float2 Rot(float2 uv, float deg)
            {
                float r = deg * 0.0174532924, s = sin(r), c = cos(r);
                float2 d = uv - 0.5;
                return float2(dot(d, float2(c, s)), dot(d, float2(-s, c))) + 0.5;
            }
            float4 frag (v2f i) : SV_Target
            {
                float3 col; float a; float add = 0;
                if (_Kind < 1.5)
                {
                    float r = tex2D(_Main_Tex, i.uv * _Main_Tex_ST2.xy + _Main_Tex_ST2.zw).r;
                    float t = lerp(i.c1.x, i.c1.y, i.c1.z);
                    float vis = t >= r ? 0.0 : 1.0;
                    col = vis * i.c1.w * i.color.rgb; a = vis * i.color.a;
                }
                else if (_Kind > 1.5 && _Kind < 2.5)
                {
                    float4 m = tex2D(_Main_Tex, Rot(i.uv, _Main_Tex_Rotate) * _Main_Tex_Tiling.xy + i.c1.xy);
                    float a0 = _Main_Tex_Alpha != 0 ? m.r : m.a;
                    col = m.rgb * i.c1.w * i.color.rgb;
                    a = tex2D(_Mask_Tex, i.uv).r * i.c1.z * a0 * i.color.a;
                }
                else if (_Kind > 2.5 && _Kind < 3.5)
                {
                    float3 m = tex2D(_Main_Tex, i.uv).rgb;
                    float3 b = _Main_Tex_Color != 0 ? m.rrr : m;
                    float t = lerp(i.c1.x, i.c1.y, i.c1.z);
                    float3 v = saturate((b - t) / max(1e-4, 1.0 - t));
                    v = v * v * (3.0 - 2.0 * v);
                    col = v * i.c1.w * i.color.rgb; a = v.r * i.color.a; add = _Add;
                }
                else if (_Kind > 4.5 && _Kind < 5.5)   // FX_Toon_Explosion_01 — R = 밝기, B = 문턱, C2.rgb = 불빛 색
                {
                    float2 rb = tex2D(_Main_Tex, i.uv).rb;
                    float hot = i.c1.y >= rb.y ? 1.0 : 0.0;
                    col = rb.x * i.c2.rgb * i.c1.w * hot + rb.x * i.color.rgb;
                    a = (lerp(i.c1.x, 1.0, rb.y) >= i.c1.z ? 1.0 : 0.0) * i.color.a;
                }
                else if (_Kind > 5.5 && _Kind < 6.5)   // FX_Toon_Dissolve_RB
                {
                    float2 rb = tex2D(_Main_Tex, i.uv).rb;
                    float t = lerp(i.c1.x, i.c1.y, rb.y);
                    col = rb.x * i.c1.w * i.color.rgb;
                    a = i.c1.z >= t ? 0.0 : i.color.a;
                }
                else if (_Kind > 6.5 && _Kind < 7.5)   // FX_Pixel
                {
                    float4 m = tex2D(_Main_Tex, floor(i.uv * _Pixel) / _Pixel);
                    col = m.rgb * _Main_Tex_Mul * i.color.rgb;
                    a = (_Main_Tex_Alpha != 0 ? m.r : m.a) * i.color.a;
                }
                else if (_Kind > 7.5 && _Kind < 8.5)   // FX_Simple_Dissolve_Gradient — 꼭짓점 알파가 디졸브
                {
                    float2 ra = tex2D(_Main_Tex, i.uv * _Main_Tex_ST2.xy + _Main_Tex_ST2.zw).ra;
                    float v = saturate((i.color.a * 2.0 + ra.x - 1.0 - _MinMaxSmoothStepDissolve.x) / max(1e-4, _MinMaxSmoothStepDissolve.y - _MinMaxSmoothStepDissolve.x));
                    v = v * v * (3.0 - 2.0 * v);
                    col = ra.x * _Main_Tex_Mul * i.color.rgb;
                    a = v * (_Main_Tex_Alpha != 0 ? ra.x : ra.y) * _OpacitySaturation;
                }
                else if (_Kind > 8.5 && _Kind < 9.5)   // FX_Water_Polar — 극좌표 노이즈로 일렁이는 물
                {
                    float2 d0 = i.uv - 0.5;
                    float2 polar = float2(length(d0) * 2.0 * _Noise_Polar_Radial_Length.x, atan2(d0.y, d0.x) * _Noise_Polar_Radial_Length.y * 0.159235656);
                    float n = tex2D(_Noise_Tex, (polar + _Time.y * _Noise_Panner.xy) * _Noise_Tex_ST2.xy + _Noise_Tex_ST2.zw).r;
                    float2 muv = i.uv * _Main_Tex_Scale + (0.5 - 0.5 * _Main_Tex_Scale) - 0.5 * _Noise_Mul + n * _Noise_Mul;
                    float4 m = tex2D(_Main_Tex, muv * _Main_Tex_ST2.xy + _Main_Tex_ST2.zw);
                    float3 c = _Main_Tex_RGBA_R != 0 ? m.rgb : m.rrr;
                    col = c * _Main_Tex_Mul * i.color.rgb;
                    a = (_Main_Tex_Alpha != 0 ? m.r : m.a) * i.color.a; add = _Add;
                }
                else if (_Kind > 9.5 && _Kind < 10.5)   // FX_Toon_Dissolve_RB_Outline — R = 밝기, B = 문턱, 문턱(Custom1.y) 넘는 곳은 흰색 · 아니면 Custom2 색(테두리)
                {
                    float2 rb = tex2D(_Main_Tex, i.uv).rb;
                    float3 edge = lerp(i.c2.rgb, 1.0, rb.y >= i.c1.y ? 1.0 : 0.0);
                    col = edge * rb.x * i.c1.w * i.color.rgb;
                    a = (rb.y * (1.0 - i.c1.x) + i.c1.x >= i.c1.z ? 1.0 : 0.0) * i.color.a;
                }
                else if (_Kind > 10.5 && _Kind < 11.5)   // FX_UV_Move_Dissolve_1 — UV 돌리기 · 늘리기 + Custom1.xy 밀기, 노이즈 G 가 Custom1.z 넘는 곳만, 밝기 Custom1.w
                {
                    float4 m = tex2D(_Main_Tex, Rot(i.uv, _Main_Tex_Rotate) * _Main_Tex_Tiling.xy + i.c1.xy);
                    float a0 = _Main_Tex_Alpha != 0 ? m.r : m.a;
                    col = m.rgb * i.c1.w * i.color.rgb;
                    a = tex2D(_Mask_Tex, i.uv).r * a0 * i.color.a * (tex2D(_Noise_Tex, i.uv).g >= i.c1.z ? 1.0 : 0.0); add = _Add;
                }
                else if (_Kind > 11.5 && _Kind < 12.5)   // FX_Master_Trail — 꼬리: UV 돌리기 · 흐름 · 노이즈 일그러뜨림, 알파 = R 과 A 사이(_Main_Tex_Alpha = 원작 _MainAlphaSwitch)
                {
                    float2 nz = tex2D(_Noise_Tex, (i.uv + _Time.y * _Noise_Panner.xy) * _Noise_Tex_ST2.xy + _Noise_Tex_ST2.zw).xy;
                    float2 muv = (Rot(i.uv, _Main_Tex_Rotate) + _Time.y * _UV_Offset.xy + nz * _Noise_Mul) * _Main_Tex_ST2.xy + _Main_Tex_ST2.zw;
                    float4 m = tex2D(_Main_Tex, muv);
                    col = pow(max(m.rgb, 1e-5), _Main_Tex_Power) * _Main_Tex_Mul * i.color.rgb;
                    a = lerp(m.r, m.a, _Main_Tex_Alpha) * i.color.a; add = _Add;
                }
                else   // 4 FX_UV_Offset_ab
                {
                    float4 m = tex2D(_Main_Tex, i.uv * _Main_Tex_Tiling.xy + _Time.y * _UV_Offset.xy);
                    float a0 = _Main_Tex_Alpha != 0 ? m.r : m.a;
                    col = m.rgb * _Main_Tex_Power * i.color.rgb;
                    a = tex2D(_Mask_Tex, i.uv).r * a0 * i.color.a;
                }
                a = saturate(a);
                return float4(col * a, a * (1.0 - add));
            }
            ENDCG
        }
    }
}
