using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 축복 임팩트(시험판 · 2026-10-07 사용자 「테스트로 축복 임팩트 만들어 볼래」) — 축복이 얹힌 신탁을 고른 순간에만.
    /// 신탁 연출(가운데서 퍼지는 빛살 · 흰 섬광 · 고리)과 다르게 「위에서 내려오는」 결:
    ///   ① 위에서 내려오는 부드러운 금빛 빛기둥 ② 흩날려 내려앉는 작은 빛 깃털 · 꽃잎 ③ 카드 테두리를 한 바퀴 도는 금빛 ④ 「축복!」 도장이 살짝 찍힘.
    /// 길이 약 1.1초. 저사양 · 움직임 줄이기면 세기 0.3배 · 입자(깃털) 없음.
    /// 점검: -oldbless 면 예전처럼(고른 뒤 축복만의 연출 없음). 전투 신탁 창(EpiphanyWindow)은 같은 결의 월드 판(Bolzena.UI.BlessImpact)을 쓴다.
    /// </summary>
    public static class BlessFx
    {
        public static bool Old => Array.IndexOf(Environment.GetCommandLineArgs(), "-oldbless") >= 0;
        public static bool Calm => Settings.ReduceMotion || DisplayOptions.LowSpec;
        /// <summary>연출 길이(초) — 신탁 창이 이만큼은 기다렸다 닫는다.</summary>
        public const float Dur = 1.1f;

        // 금빛 · 깃털 · 꽃잎 색
        public static readonly Color Gold = new Color(0.74f, 0.62f, 1f);   // 축복 톤 = 연보라(2026-10-08 사용자 · 이름은 예전 그대로 Gold)
        public static readonly Color Light = new Color(0.9f, 0.84f, 1f);
        public static readonly Color[] Bits = { new Color(0.78f, 0.66f, 1f), new Color(0.66f, 0.5f, 0.98f), new Color(0.9f, 0.76f, 1f), new Color(0.93f, 0.88f, 1f) };

        // ── 텍스처(한 번 굽는다) ──
        static Sprite beam, feather;
        /// <summary>빛기둥 — 가로는 부드러운 종 모양, 세로는 아래(내려앉는 끝)가 밝고 위로 옅어진다. 피벗은 위 가운데(아래로 늘린다). 너비 1 단위.</summary>
        public static Sprite Beam
        {
            get
            {
                if (beam) return beam;
                const int W = 64, H = 256;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "bless_beam" };
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (x + 0.5f) / W * 2 - 1, v = (y + 0.5f) / H;   // v 0 = 아래
                        float across = Mathf.Exp(-u * u / (2 * 0.16f)) * 0.75f + Mathf.Exp(-u * u / (2 * 0.02f)) * 0.35f;   // 넓은 빛 + 가운데 심
                        float along = Mathf.SmoothStep(0, 1, v / 0.12f) * Mathf.Lerp(1f, 0.25f, v) * Mathf.SmoothStep(0, 1, (1 - v) / 0.25f);
                        px[y * W + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(across * along) * 255));
                    }
                tex.SetPixels32(px); tex.Apply(false, true);
                beam = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 1f), W);
                return beam;
            }
        }

        /// <summary>깃털 · 꽃잎 — 끝이 살짝 휜 갸름한 잎꼴(가운데 심이 밝다). 너비 1 단위.</summary>
        public static Sprite Feather
        {
            get
            {
                if (feather) return feather;
                const int W = 32, H = 64;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "bless_feather" };
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float v = (y + 0.5f) / H * 2 - 1, u = (x + 0.5f) / W * 2 - 1;
                        u -= 0.25f * v * v;                                   // 휜 끝
                        float half = 0.85f * Mathf.Sqrt(Mathf.Max(0, 1 - v * v)) * (0.75f + 0.25f * v);
                        float d = half <= 0 ? 1 : Mathf.Abs(u) / half;
                        float a = Mathf.Clamp01((1 - d) * 3f) * 0.85f + Mathf.Clamp01(1 - Mathf.Abs(u) * 10) * 0.15f * Mathf.Clamp01(1 - Mathf.Abs(v));
                        px[y * W + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                    }
                tex.SetPixels32(px); tex.Apply(false, true);
                feather = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
                return feather;
            }
        }

        /// <summary>축복 표식 — 오른쪽 날개(깃 셋이 위로 부채꼴, 뿌리는 왼쪽 아래). 왼쪽은 뒤집어 쓴다. 너비 1 단위 · 피벗 뿌리(0.08, 0.3).
        /// 받은 카드의 장식 선 양 끝에 단다(신탁 별 줄과 다른 모양 — 2026-10-07 사용자 「축복 카드도 축복 전용 뭐 만들어서 달아 줘라」).</summary>
        public static Sprite Wing
        {
            get
            {
                if (wing) return wing;
                const int W = 96, H = 72;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "bless_wing", filterMode = FilterMode.Bilinear };
                var px = new Color32[W * H];
                var root = new Vector2(W * 0.08f, H * 0.3f);
                float[] ang = { 6f, 30f, 54f }, len = { W * 0.9f, W * 0.78f, W * 0.6f }, wid = { H * 0.2f, H * 0.2f, H * 0.18f };
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f) - root;
                        float a = 0, rim = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            float r = ang[i] * Mathf.Deg2Rad;
                            var d = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
                            float along = Vector2.Dot(p, d), across = Vector2.Dot(p, new Vector2(-d.y, d.x));
                            float u = along / len[i];
                            if (u < 0 || u > 1) continue;
                            float half = wid[i] * Mathf.Sqrt(Mathf.Max(0, Mathf.Sin(u * Mathf.PI))) * (u < 0.5f ? 1 : 1.05f - u * 0.1f);
                            float e = half <= 0.01f ? 1 : Mathf.Abs(across) / half;
                            float fa = Mathf.Clamp01((1 - e) * 4f);
                            if (fa > a) { a = fa; rim = Mathf.Clamp01((e - 0.55f) * 3f); }
                        }
                        // 깃 가장자리는 진한 금빛, 속은 밝은 금빛(가운데로 갈수록 희게)
                        var c = Color.Lerp(new Color(0.97f, 0.94f, 1f), new Color(0.6f, 0.42f, 0.95f), rim);
                        px[y * W + x] = new Color(c.r, c.g, c.b, a);
                    }
                tex.SetPixels32(px); tex.Apply(false, true);
                wing = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.08f, 0.3f), W);
                return wing;
            }
        }
        static Sprite wing;

        // ── 시간표(초) — 두 판(uGUI · 월드)이 같은 몫을 쓴다 ──
        /// <summary>빛기둥 — 길이(0~1, 아래로 뻗은 몫) · 세기(0~1) · 너비 배율.</summary>
        public static void BeamAt(float t, out float reach, out float a, out float width)
        {
            reach = Mathf.Clamp01(t / 0.22f); reach = 1 - (1 - reach) * (1 - reach) * (1 - reach);
            a = t < 0.22f ? reach : t < 0.6f ? 1f : Mathf.Clamp01(1 - (t - 0.6f) / 0.45f);
            width = Mathf.Lerp(1f, 0.75f, Mathf.Clamp01((t - 0.2f) / 0.8f));
        }

        /// <summary>테두리 금빛 머리의 자리(0~1 → 둘레 한 바퀴, 위 가운데에서 시계 방향).</summary>
        public static Vector2 Perimeter(float p, float w, float h)
        {
            p = Mathf.Repeat(p, 1f);
            float L = 2 * (w + h), d = p * L;
            float hw = w / 2, hh = h / 2;
            // 위 가운데 → 오른쪽 위 → 오른쪽 아래 → 왼쪽 아래 → 왼쪽 위 → 위 가운데
            if (d < hw) return new Vector2(d, hh); d -= hw;
            if (d < h) return new Vector2(hw, hh - d); d -= h;
            if (d < w) return new Vector2(hw - d, -hh); d -= w;
            if (d < h) return new Vector2(-hw, -hh + d); d -= h;
            return new Vector2(-hw + d, hh);
        }
        public const float TraceFrom = 0.12f, TraceDur = 0.55f, StampAt = 0.3f, StampIn = 0.12f, FadeFrom = 0.85f;

        /// <summary>깃털 하나의 설정(무작위) — 시작 시각 · 자리 · 크기 · 흔들림.</summary>
        public struct Bit { public float T0, Life, X, Y, Fall, Sway, Phase, Size, Rot0, RotV; public Color C; }
        public static List<Bit> RollBits(int n, float cw, float ch)
        {
            var l = new List<Bit>();
            for (int i = 0; i < n; i++)
                l.Add(new Bit
                {
                    T0 = 0.08f + 0.4f * i / Mathf.Max(1, n - 1) + UnityEngine.Random.Range(-0.04f, 0.04f),
                    Life = UnityEngine.Random.Range(0.55f, 0.7f),
                    X = UnityEngine.Random.Range(-cw * 0.85f, cw * 0.85f), Y = ch * UnityEngine.Random.Range(0.45f, 1.0f),
                    Fall = ch * UnityEngine.Random.Range(0.45f, 0.75f),
                    Sway = cw * UnityEngine.Random.Range(0.05f, 0.12f), Phase = UnityEngine.Random.Range(0, Mathf.PI * 2),
                    Size = cw * UnityEngine.Random.Range(0.06f, 0.1f),
                    Rot0 = UnityEngine.Random.Range(-60f, 60f), RotV = UnityEngine.Random.Range(-140f, 140f),
                    C = Bits[i % Bits.Length],
                });
            return l;
        }
        public static float BitAlpha(float u) => u < 0.15f ? u / 0.15f : u > 0.6f ? Mathf.Clamp01((1 - u) / 0.4f) : 1f;

        /// <summary>도장 — 크기 배율 · 세기(찍히는 순간 2.1 → 1, 뒤에 옅어짐).</summary>
        public static void StampAtT(float t, out float scale, out float a)
        {
            float u = (t - StampAt) / StampIn;
            if (u < 0) { scale = 2.1f; a = 0; return; }
            if (u < 1) { scale = Mathf.Lerp(2.1f, 1f, u * u); a = u; return; }
            float after = t - StampAt - StampIn;
            scale = 1f + 0.06f * Mathf.Exp(-after * 18) * Mathf.Sin(after * 40);   // 찍힌 뒤 살짝 출렁
            a = t < FadeFrom ? 1 : Mathf.Clamp01(1 - (t - FadeFrom) / (Dur - FadeFrom));
        }

        /// <summary>날개 표식이 카드에 박히는 때(도장 바로 뒤) — 크기 배율(1.9 → 1) · 세기.</summary>
        public const float WingAt = 0.4f, WingIn = 0.14f;
        public static void WingAtT(float t, out float scale, out float a)
        {
            float u = (t - WingAt) / WingIn;
            if (u < 0) { scale = 1.9f; a = 0; return; }
            if (u < 1) { scale = Mathf.Lerp(1.9f, 1f, u * u); a = u; return; }
            scale = 1f + 0.08f * Mathf.Exp(-(t - WingAt - WingIn) * 16) * Mathf.Sin((t - WingAt - WingIn) * 36); a = 1;   // 박힌 뒤로는 그대로(받은 표식)
        }

        // ── uGUI 판(휴식 수련 · 이벤트 신탁 · 보상 — OracleReveal) ──
        /// <summary>root 안 at 자리(가운데 기준 anchoredPosition)의 카드(지금 보이는 크기 cw×ch)에 축복 임팩트. 붙인 것은 끝나면 스스로 지운다.</summary>
        public static IEnumerator PlayUi(RectTransform root, Vector2 at, float cw, float ch, string blessName, RectTransform card = null)
        {
            bool calm = Calm;
            float s = calm ? 0.3f : 1f;
            var fx = Ui_Rect(root);
            var top = root.rect.height / 2;
            // ① 빛기둥(카드 뒤) + 바닥의 부드러운 빛
            var bm = Img(fx, Beam, Gold.A(0), "blessbeam");
            bm.rectTransform.At(0.5f, 0.5f, at.x, top, cw, 1);
            bm.rectTransform.pivot = new Vector2(0.5f, 1);
            bm.rectTransform.anchoredPosition = new Vector2(at.x, top);
            var floor = Img(fx, Theme.S("soft"), Gold.A(0), "blessfloor");
            floor.rectTransform.At(0.5f, 0.5f, at.x, at.y, cw * 2.4f, ch * 1.4f);
            if (card && card.parent == root) fx.SetSiblingIndex(card.GetSiblingIndex());   // 카드 바로 뒤
            // 앞쪽(카드 위) — 테두리 금빛 · 깃털 · 도장
            var front = Ui_Rect(root);
            const int TR = 9;
            var trail = new List<Image>();
            for (int i = 0; i < TR; i++) { var im = Img(front, Theme.S("soft"), Gold.A(0), "blesstrace"); float z = cw * (i == 0 ? 0.32f : 0.24f - 0.012f * i); im.rectTransform.At(0.5f, 0.5f, at.x, at.y, z, z); trail.Add(im); }
            var bits = calm ? new List<Bit>() : RollBits(14, cw, ch);
            var bitIm = new List<Image>();
            foreach (var b in bits) { var im = Img(front, Feather, b.C.A(0), "blessbit"); im.rectTransform.At(0.5f, 0.5f, at.x + b.X, at.y + b.Y, b.Size, b.Size * 2); bitIm.Add(im); }
            var stampAt = at + new Vector2(cw * 0.36f, ch * 0.44f);
            var st = Img(front, Theme.S("pill_gold", 46), new Color(1, 1, 1, 0), "blessstamp");
            st.rectTransform.At(0.5f, 0.5f, stampAt.x, stampAt.y, cw * 0.62f, cw * 0.26f);
            st.rectTransform.localEulerAngles = new Vector3(0, 0, -14);
            var stt = Ui.Title(st.transform, "축복!", cw * 0.15f, Theme.Brown, TextAlignmentOptions.Center);
            stt.rectTransform.Fill(); stt.alpha = 0;
            var ring = Img(front, Theme.S("ring"), Gold.A(0), "blessring");
            ring.rectTransform.At(0.5f, 0.5f, stampAt.x, stampAt.y, cw * 0.5f, cw * 0.5f);
            TextMeshProUGUI nm = null;
            if (!string.IsNullOrEmpty(blessName))
            {
                nm = Ui.Title(front, "축복", Mathf.Max(18, cw * 0.11f), Light, TextAlignmentOptions.Center);
                nm.rectTransform.At(0.5f, 0.5f, at.x, at.y - ch / 2 - cw * 0.48f, 1000, 44); nm.Outline(0.25f, new Color(0.15f, 0.06f, 0.3f)); nm.alpha = 0;
            }
            // 표식이 박힌다 — 카드(W.Card)의 장식 선 양 끝에 날개(받은 카드의 표식과 같은 자리 · 같은 함수)
            var wings = new List<Image>();
            var deco = card ? card.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "deco") : null;
            if (deco != null && deco.parent is RectTransform face && face.Find("blesswingr") == null)
            {
                float k = deco.sizeDelta.y / 1.4f;
                W.BlessWings(face, deco.anchoredPosition.y + 0.7f * k, deco.sizeDelta.x / 2, k);
                foreach (var nmw in new[] { "blesswingl", "blesswingr" }) { var im = face.Find(nmw)?.GetComponent<Image>(); if (im) { im.color = new Color(1, 1, 1, 0); wings.Add(im); } }
            }
            var wingS = wings.ConvertAll(w => w.rectTransform.localScale);
            bool thumped = false;
            float t = 0, dur = Settings.ReduceMotion ? Dur * 0.4f : Dur;
            while (t < dur)
            {
                if (!root) yield break;
                float tt = t / dur * Dur;   // 움직임 줄이기면 시간표를 줄여 돈다
                BeamAt(tt, out var reach, out var ba, out var bw);
                float bh = (top - at.y) + ch * 0.1f;
                bm.rectTransform.sizeDelta = new Vector2(cw * 1.7f * bw, bh * reach);
                bm.color = Gold.A(0.7f * s * ba);
                floor.color = Gold.A(0.4f * s * ba * reach);
                // ③ 테두리 한 바퀴
                float p = (tt - TraceFrom) / TraceDur;
                float ta = p < 0 ? 0 : p < 1 ? 1 : Mathf.Clamp01(1 - (p - 1) * 5);
                for (int i = 0; i < TR; i++)
                {
                    float q = Mathf.Clamp01(p - i * 0.022f);
                    trail[i].rectTransform.anchoredPosition = at + Perimeter(q, cw, ch);
                    trail[i].color = (i == 0 ? new Color(0.97f, 0.94f, 1f) : Gold).A(ta * s * (i == 0 ? 1f : 0.8f * (1 - i / (float)TR)) * (p - i * 0.022f > 0 ? 1 : 0));
                }
                // ② 깃털 · 꽃잎
                for (int i = 0; i < bits.Count; i++)
                {
                    var b = bits[i]; float u = (tt - b.T0) / b.Life;
                    if (u < 0 || u > 1) { bitIm[i].color = b.C.A(0); continue; }
                    bitIm[i].rectTransform.anchoredPosition = at + new Vector2(b.X + b.Sway * Mathf.Sin(b.Phase + u * 6f), b.Y - b.Fall * u);
                    bitIm[i].rectTransform.localEulerAngles = new Vector3(0, 0, b.Rot0 + b.RotV * u);
                    bitIm[i].color = b.C.A(0.9f * BitAlpha(u));
                }
                // ④ 도장
                StampAtT(tt, out var sc, out var sa);
                st.rectTransform.localScale = Vector3.one * sc;
                st.color = new Color(1, 1, 1, sa * (calm ? 0.85f : 1f)); stt.alpha = sa;
                if (nm) nm.alpha = sa;
                WingAtT(tt, out var wsc, out var wa);
                for (int i = 0; i < wings.Count; i++) if (wings[i]) { wings[i].rectTransform.localScale = new Vector3(wingS[i].x * wsc, wingS[i].y * wsc, 1); wings[i].color = new Color(1, 1, 1, Mathf.Clamp01(wa * 1.5f)); }
                if (!thumped && tt >= StampAt + StampIn)
                {
                    thumped = true;
                    if (card) { var c = card; var s0 = c.localScale; Tw.Run(c, 0.16f, k => { if (c) c.localScale = s0 * (1 - 0.035f * Mathf.Sin(k * Mathf.PI)); }, Tw.Linear); }
                    var r = ring; var rc = Gold.A(0.8f * s);
                    Tw.Run(r, 0.3f, k => { if (!r) return; r.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(cw * 0.5f, cw * 1.3f, k); r.color = rc.A(rc.a * (1 - k)); }, Tw.OutCubic);
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (fx) UnityEngine.Object.Destroy(fx.gameObject);
            if (front) UnityEngine.Object.Destroy(front.gameObject);
        }

        static RectTransform Ui_Rect(RectTransform root) { var r = Bolzena.RunUI.Ui.Rect("blessfx", root); r.Fill(); return r; }
        static Image Img(Transform p, Sprite sp, Color c, string name) => Bolzena.RunUI.Ui.Img(p, sp, c, name);
    }
}
