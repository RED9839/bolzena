using System.Collections;
using System.Collections.Generic;
using Bolzena.RunUI;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    /// <summary>
    /// 축복 임팩트(시험판 · 2026-10-07) — 전투 신탁 창(EpiphanyWindow)에서 축복이 얹힌 선택지를 고른 순간. 판 화면 판(BlessFx.PlayUi)과 같은 시간표 · 같은 결:
    /// 위에서 내려오는 금빛 빛기둥 · 흩날리는 빛 깃털 · 꽃잎 · 카드 테두리를 한 바퀴 도는 금빛 · 「축복!」 도장. 약 1.1초.
    /// 저사양 · 움직임 줄이기면 세기 0.3배 · 깃털 없음. -oldbless 면 부르지 않는다(EpiphanyWindow).
    /// </summary>
    public static class BlessImpact
    {
        static bool Calm => Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;

        /// <summary>root 안 at 의 카드(cv — 지금 보이는 크기 cw×ch, 정렬 order)에 축복 임팩트.</summary>
        public static IEnumerator Play(Transform root, Vector3 at, float cw, float ch, int order, string blessName, CardView cv = null)
        {
            bool calm = Calm;
            float s = calm ? 0.3f : 1f;
            var gold = BlessFx.Gold;
            var add = Res.SpriteMat(true, calm ? 1.2f : 2.2f);
            var node = Make.Node("BlessImpact", root);
            float top = (Camera.main != null ? Camera.main.orthographicSize : 4.5f) + 0.3f;
            // ① 빛기둥 · 바닥 빛(카드 뒤)
            var bm = Make.Sprite("beam", node, BlessFx.Beam, new Vector3(at.x, top, 0), order - 4, gold.A(0), add);
            float beamUnits = BlessFx.Beam.bounds.size.y;
            var floor = Make.Box("floor", node, Res.UI("soft"), at, new Vector2(cw * 2.4f, ch * 1.3f), order - 5, gold.A(0), add);
            // 앞 — 테두리 금빛 · 깃털 · 도장
            const int TR = 9;
            var trail = new List<SpriteRenderer>();
            for (int i = 0; i < TR; i++) { float z = cw * (i == 0 ? 0.32f : 0.24f - 0.012f * i); trail.Add(Make.Box("trace", node, Res.UI("soft"), at, new Vector2(z, z), order + 7, gold.A(0), add)); }
            var bits = calm ? new List<BlessFx.Bit>() : BlessFx.RollBits(14, cw, ch);
            var bitSr = new List<SpriteRenderer>();
            foreach (var b in bits) { var sr = Make.Sprite("bit", node, BlessFx.Feather, at, order + 8, b.C.A(0), Res.SpriteMat(false, 1.4f)); sr.transform.localScale = Vector3.one * b.Size; bitSr.Add(sr); }
            var stampAt = at + new Vector3(cw * 0.36f, ch * 0.44f, 0);
            var stamp = Make.Node("stamp", node, stampAt);
            stamp.localEulerAngles = new Vector3(0, 0, -14);
            var band = Make.Sliced("band", stamp, Res.UI("panel_9s"), Vector3.zero, new Vector2(cw * 0.62f, cw * 0.26f), order + 13, new Color(0.55f, 0.4f, 0.92f, 0));
            var stt = Make.Text("t", stamp, "축복!", new Vector3(0, -0.005f, 0), cw * 0.13f, order + 14, Color.white);
            stt.alpha = 0;
            TextMeshPro nm = null;
            if (!string.IsNullOrEmpty(blessName))
            {
                nm = Make.Text("name", node, "축복", at + new Vector3(0, -ch / 2 - 1.05f, 0), 0.3f, order + 11, BlessFx.Light);
                Make.Outline(nm, 0.25f, new Color(0.15f, 0.06f, 0.3f));
                nm.alpha = 0;
            }
            // 표식이 박힌다 — 카드(CardView)의 장식 선 양 끝에 날개(받은 카드의 표식과 같은 자리)
            SpriteRenderer wl = null, wr = null; float ws = 0;
            var dc = cv ? cv.transform.Find("deco") : null;
            var dsr = dc ? dc.GetComponent<SpriteRenderer>() : null;
            var own = cv ? cv.transform.Find("blesswingr") : null;   // 이미 표식이 보이는 카드(받은 축복)면 덧달지 않는다
            if (dsr != null && dsr.sprite != null && !(own && own.GetComponent<SpriteRenderer>().enabled))
            {
                float half = dc.localScale.x * dsr.sprite.bounds.size.x / 2;
                ws = 0.3f / BlessFx.Wing.bounds.size.x;
                wr = Make.Sprite("blesswingfx", cv.transform, BlessFx.Wing, new Vector3(half + 0.01f, dc.localPosition.y - 0.02f, 0), cv.Order + 8, new Color(1, 1, 1, 0));
                wl = Make.Sprite("blesswingfx", cv.transform, BlessFx.Wing, new Vector3(-half - 0.01f, dc.localPosition.y - 0.02f, 0), cv.Order + 8, new Color(1, 1, 1, 0));
                wl.flipX = true;
            }
            bool thumped = false, wingPop = false;
            float t = 0, dur = Bolzena.RunUI.Settings.ReduceMotion ? BlessFx.Dur * 0.4f : BlessFx.Dur;
            while (t < dur)
            {
                if (!node) yield break;
                float tt = t / dur * BlessFx.Dur;
                BlessFx.BeamAt(tt, out var reach, out var ba, out var bw);
                float bh = (top - at.y) + ch * 0.1f;
                bm.transform.localScale = new Vector3(cw * 1.7f * bw, Mathf.Max(0.001f, bh * reach / beamUnits), 1);
                Make.Alpha(bm, 0.8f * s * ba);
                Make.Alpha(floor, 0.4f * s * ba * reach);
                float p = (tt - BlessFx.TraceFrom) / BlessFx.TraceDur;
                float ta = p < 0 ? 0 : p < 1 ? 1 : Mathf.Clamp01(1 - (p - 1) * 5);
                for (int i = 0; i < TR; i++)
                {
                    float q = Mathf.Clamp01(p - i * 0.022f);
                    trail[i].transform.localPosition = at + (Vector3)BlessFx.Perimeter(q, cw, ch);
                    trail[i].color = (i == 0 ? new Color(0.97f, 0.94f, 1f) : gold).A(ta * s * (i == 0 ? 1f : 0.8f * (1 - i / (float)TR)) * (p - i * 0.022f > 0 ? 1 : 0));
                }
                for (int i = 0; i < bits.Count; i++)
                {
                    var b = bits[i]; float u = (tt - b.T0) / b.Life;
                    if (u < 0 || u > 1) { Make.Alpha(bitSr[i], 0); continue; }
                    bitSr[i].transform.localPosition = at + new Vector3(b.X + b.Sway * Mathf.Sin(b.Phase + u * 6f), b.Y - b.Fall * u, 0);
                    bitSr[i].transform.localEulerAngles = new Vector3(0, 0, b.Rot0 + b.RotV * u);
                    Make.Alpha(bitSr[i], 0.9f * BlessFx.BitAlpha(u));
                }
                BlessFx.StampAtT(tt, out var sc, out var sa);
                stamp.localScale = Vector3.one * sc;
                Make.Alpha(band, sa * (calm ? 0.85f : 1f)); stt.alpha = sa;
                if (nm) nm.alpha = sa;
                if (wr != null)
                {
                    BlessFx.WingAtT(tt, out var wsc, out var wa);
                    float wf = tt < BlessFx.FadeFrom ? 1 : Mathf.Clamp01(1 - (tt - BlessFx.FadeFrom) / (BlessFx.Dur - BlessFx.FadeFrom) * 0.5f);   // 끝에는 반쯤만 옅게(창이 닫히며 카드가 나간다)
                    foreach (var w in new[] { wl, wr }) { if (!w) continue; w.transform.localScale = new Vector3(ws * wsc, ws * wsc, 1); Make.Alpha(w, Mathf.Clamp01(wa * 1.5f) * wf); }
                    if (!wingPop && tt >= BlessFx.WingAt + BlessFx.WingIn)
                    {
                        wingPop = true;
                        foreach (var w in new[] { wl, wr }) if (w) Vfx.Glow(w.transform.localPosition + new Vector3(w == wr ? 0.1f : -0.1f, 0.03f, 0), 0.45f, gold.A(0.8f * s), 0.25f, 2f, null, cv.Order + 9, cv.transform);
                    }
                }
                if (!thumped && tt >= BlessFx.StampAt + BlessFx.StampIn)
                {
                    thumped = true;
                    if (cv) cv.transform.localScale *= 0.965f;   // 도장이 찍히며 카드가 살짝 눌린다(Follow 가 되돌린다)
                    Vfx.Ring(stampAt, cw * 0.4f, cw * 1.2f, 0.3f, gold.A(0.9f * s), 2.5f * s + 0.5f, "FX_IN_Ring_ShockWave_01", order + 12, node, 1f, true);
                }
                t += Clock.UDt;
                yield return null;
            }
            if (node) Object.Destroy(node.gameObject);
            if (wl) Object.Destroy(wl.gameObject);
            if (wr) Object.Destroy(wr.gameObject);
        }
    }
}
