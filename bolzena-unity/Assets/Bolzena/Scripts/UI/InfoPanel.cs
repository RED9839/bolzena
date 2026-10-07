using System;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 정보 창(배치는 카제나 상세 창을 따른다 · 그림 · 아이콘 · 글은 우리 것) — 화면 왼쪽 약 38% 의 세로 판, 싸움터는 오른쪽에 그대로 보이고
    // 고른 대상 발밑에 흰 타원 고리. 위에서 아래로 한 줄씩:
    //   적: 이름(크게) · 체력 구간 알약(건강 · 부상 · 위험) / 성격 점 · 이름 · ⓘ 약점 / HP 막대 · 방어 · 강인도
    //       → 다음 행동 칸(의도 마름모 + 갈래 + 회색 알약 「즉시 행동까지 N장」 + 효과 글, 수치 주황)
    //       → 상태마다 한 칸(가는 선으로 구분 · 아이콘 + 겹 수 · 이름 · 풀이, 해로운 칸은 위 테두리 붉게, 오른쪽에 건 사도 초상 알약)
    //       → 패시브(다 보이는 칸) · 소개
    //   사도: 파티 HP · 방어 · 파티 상태 칸들(강화 칩 포함) → 그 사도: 이름 · 성격 · 능력치 · 고학년(링 · 효과) → 고유 효과 → 패시브(core CardText.Traits) → 상태 칸
    //   「자세히」 · 접기 없음 — 글은 다 보이고(수치 · 대상 · 지속 · 제한), 판이 길면 판 안에서 내린다.
    // 내용이 길면 판 안에서 휠 · 끌기로 내린다. 바깥(싸움터)을 누르면 · 오른쪽 클릭 · Esc 로 닫힌다.
    public static partial class InfoPanel
    {
        static float Wd = 6f;
        const float Pad = 0.3f;
        const int OC = Modal.O + 5;

        class Stack
        {
            public Transform T;
            public float Y = -Pad;
            public Stack(Transform t) { T = t; }

            public TextMeshPro Para(string text, float x, float w, float size, Color c, bool body = true, float gapAfter = 0.06f, float y = float.NaN)
            {
                float top = float.IsNaN(y) ? Y : y;
                var t = Tone.Text("p", T, text, new Vector3(x, top, 0), size, OC, c, TextAlignmentOptions.TopLeft, body, w);
                t.textWrappingMode = TextWrappingModes.Normal;
                t.lineSpacing = body ? -2 : -4;
                t.rectTransform.sizeDelta = new Vector2(w, 10);
                t.ForceMeshUpdate();
                float h = t.preferredHeight;
                t.rectTransform.sizeDelta = new Vector2(w, h);
                if (float.IsNaN(y)) Y = top - h - gapAfter;
                return t;
            }

            public void Head(string text)
            {
                Y -= 0.1f;
                var t = Tone.Text("h", T, text, new Vector3(Pad, Y, 0), Tone.Sm, OC, Tone.Gold, TextAlignmentOptions.TopLeft, false, Wd);
                t.characterSpacing = 3;
                Y -= 0.3f;
            }

            public void Rule(Color? c = null, float th = 0.012f)
            {
                Y -= 0.06f;
                Make.Box("rule", T, Res.UI("white"), new Vector3(Wd / 2, Y, 0), new Vector2(Wd - Pad * 2, th), OC, c ?? Tone.Line);
                Y -= 0.1f;
            }
        }

        static string StripHead(string text, string id)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int d = text.IndexOf(" — ", StringComparison.Ordinal);
            return d >= 0 && d < id.Length + 8 ? text.Substring(d + 3) : text;
        }

        static string Plain(string rich) => System.Text.RegularExpressions.Regex.Replace(rich ?? "", "<[^>]+>", "");

        // 효과 글 — 수치 주황(카제나 상세 창처럼 값이 먼저 눈에 들어오게)
        static readonly System.Text.RegularExpressions.Regex num = new System.Text.RegularExpressions.Regex(@"(?<![#\w])([+\-]?\d+(?:\.\d+)?%?(?:\s?×\s?\d+)?)");
        static string Orange(string rich)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < rich.Length)
            {
                int lt = rich.IndexOf('<', i);
                string plain = lt < 0 ? rich.Substring(i) : rich.Substring(i, lt - i);
                sb.Append(num.Replace(plain, "<color=#FFB45A>$1</color>"));
                if (lt < 0) break;
                int gt = rich.IndexOf('>', lt);
                if (gt < 0) { sb.Append(rich.Substring(lt)); break; }
                sb.Append(rich, lt, gt - lt + 1);
                i = gt + 1;
            }
            return sb.ToString();
        }

        static string Stat(string ko, int baseV, int now, string unit = "")
        {
            var s = $"<color={Tone.SubTag}>{ko}</color> {baseV}{unit}";
            if (now != baseV) s += $"<color={(now > baseV ? Tone.GoodTag : Tone.BadTag)}>→{now}{unit}</color>";
            return s;
        }

        // 알약(둥근 꼬리표) — 오른쪽 끝을 x 로 두고 왼쪽으로 자란다. 폭을 돌려준다
        static float PillR(Transform T, float xRight, float cy, string text, Color bg, Color fg, float size = 0.14f)
        {
            var t = Tone.Text("pl", T, text, Vector3.zero, size, OC + 2, fg, TextAlignmentOptions.Center, true);
            t.ForceMeshUpdate();
            float w = t.preferredWidth + 0.26f, h = size + 0.14f;
            Make.Sliced("plb", T, Res.UI("bar_fill_9s"), new Vector3(xRight - w / 2, cy, 0), new Vector2(w, h), OC + 1, bg);
            t.transform.localPosition = new Vector3(xRight - w / 2, cy - 0.005f, 0);
            return w;
        }

        static void HpBar(Stack st, float x, float w, int hp, int max, int block, Color fillC, string label = null)
        {
            float h = 0.2f;
            float bw = block > 0 ? w - 0.5f : w;
            float cy = st.Y - h / 2;
            Make.Sliced("hpbg", st.T, Res.UI("bar_bg_9s"), new Vector3(x + bw / 2, cy, 0), new Vector2(bw, h + 0.04f), OC);
            float f = max > 0 ? Mathf.Clamp01((float)hp / max) : 0;
            if (f > 0) Make.Sliced("hpf", st.T, Res.UI("bar_fill_9s"), new Vector3(x + 0.02f + (bw - 0.04f) * f / 2, cy, 0), new Vector2(Mathf.Max(0.12f, (bw - 0.04f) * f), h - 0.04f), OC + 1, fillC);
            var t = Make.Text("hpt", st.T, (label != null ? $"<size=80%>{label}</size>  " : "") + $"{hp:N0}<size=80%><color=#d8deef> / {max:N0}</color></size>", new Vector3(x + bw / 2, cy - 0.005f, 0), 0.15f, OC + 2, Color.white);
            Make.Outline(t, 0.32f, Tone.Outline);
            if (block > 0)
            {
                Make.Box("blk", st.T, Res.UI("ic_shield"), new Vector3(x + w - 0.22f, cy, 0), new Vector2(0.4f, 0.4f), OC + 1);
                var bt = Make.Text("blkt", st.T, block.ToString(), new Vector3(x + w - 0.22f, cy - 0.01f, 0), 0.16f, OC + 2, Color.white);
                Make.Outline(bt, 0.4f, new Color(0, 0.08f, 0.25f));
            }
            st.Y -= h + 0.12f;
        }

        // 상태 칸 — 하나에 한 칸: 아이콘(겹 수) · 이름 · 남은 턴 / 풀이 / 오른쪽에 건 사도 초상 알약. 해로운 칸은 위 테두리 붉게
        static void StatusRows(Stack st, List<StatusChip> chips, string owner, Action redraw)
        {
            foreach (var c in chips)
            {
                bool bad = c.Kind == "debuff";
                var kc = ChipRow.KindColor(c.Kind);
                st.Rule(bad ? new Color(1f, 0.4f, 0.45f, 0.75f) : Tone.Line, bad ? 0.02f : 0.012f);
                float top = st.Y;
                // 아이콘 + 겹 수(작게)
                var ic = new Vector3(Pad + 0.2f, top - 0.2f, 0);
                Make.Box("cib", st.T, Res.UI("circle"), ic, new Vector2(0.38f, 0.38f), OC, new Color(kc.r * 0.25f, kc.g * 0.25f, kc.b * 0.25f, 0.95f));
                var icon = ChipRow.IconOf(c);
                if (icon != null) Make.Box("ci", st.T, ChipRow.IconSprite(icon), ic, new Vector2(0.26f, 0.26f), OC + 1);
                else if (!string.IsNullOrEmpty(c.Id)) Tone.Text("cil", st.T, c.Id.Substring(0, 1), ic, 0.16f, OC + 1, Color.Lerp(Color.white, kc, 0.5f));
                var sv = Tone.Text("cv", st.T, c.Value, ic + new Vector3(0.17f, -0.14f, 0), 0.12f, OC + 2, Color.white, TextAlignmentOptions.Center, false, 1f, 0.35f);
                // 오른쪽 — 건 사도 초상 알약(여럿이면 나란히)
                float right = Wd - Pad;
                if (c.From != null)
                    for (int k = c.From.Count - 1; k >= 0; k--)
                    {
                        var fc = new Vector3(right - 0.2f, top - 0.2f, 0);
                        Make.Sliced("fpb", st.T, Res.UI("bar_fill_9s"), fc, new Vector2(0.42f, 0.36f), OC, new Color(0.1f, 0.13f, 0.24f, 0.95f));
                        Tone.RoundFace("fp" + k, st.T, c.From[k], fc, 0.3f, OC + 2);
                        right -= 0.48f;
                    }
                float tx = Pad + 0.5f, tw = right - tx - 0.06f;
                string name = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Color.white, kc, 0.55f))}>{c.Id}</color>" + (c.Turns > 0 ? $"  <size=80%><color={Tone.SubTag}>{c.Turns}턴</color></size>" : "");
                st.Para(name, tx, tw, Tone.Body, Tone.Ink, false, 0.02f, top - 0.04f);
                st.Y = top - 0.34f;
                var desc = StripHead(Tone.StripDiff(c.Text), c.Id);   // 수치 · 지속이 다 든 한 가지 글 — 자르지 않는다(「자세히」 없음)
                if (!string.IsNullOrEmpty(desc)) st.Para(Orange(desc), tx, tw, Tone.Sm, Tone.Sub, true, 0.04f);
                st.Y = Mathf.Min(st.Y, top - 0.46f);
            }
        }

        static (string name, string text) Split(string line)
        {
            line = line ?? "";
            int d = line.IndexOf(" — ", StringComparison.Ordinal);
            if (d > 0 && d < 24) return (line.Substring(0, d), line.Substring(d + 3));
            int c = line.IndexOf(": ", StringComparison.Ordinal);
            if (c > 0 && c < 24) return (line.Substring(0, c), line.Substring(c + 2));
            return ("", line);
        }

        // 한 칸 — 이름 · 부제(작게 흐리게) · 본문(「· 계기 → 결과」 줄, 수치 주황). 접지 않는다(2026-10-05 사용자: 「자세히 보기를 없애고 수치가 다 보이게」)
        static void Entry(Stack st, string name, string sub, string body)
        {
            st.Rule();
            if (!string.IsNullOrEmpty(name)) st.Para(name, Pad, Wd - Pad * 2, Tone.Body, Tone.Ink, false, 0.02f);
            if (!string.IsNullOrEmpty(sub)) st.Para($"<color={Tone.SubTag}>{sub}</color>", Pad, Wd - Pad * 2, Tone.Cap, Tone.Sub, true, 0.03f);
            if (!string.IsNullOrEmpty(body)) st.Para(Orange(body), Pad + 0.06f, Wd - Pad * 2 - 0.06f, Tone.Sm, Tone.Ink, true, 0.08f);
        }

        // 고른 대상 발밑 — 흰 타원 고리(싸움터 위 · 유닛 아래)
        static void FootRing(Modal m, Vector3 feet, float w)
        {
            var r = Tone.Ring("footring", m.transform, feet, 1f, 0.06f, 3, new Color(1, 1, 1, 0.85f), new Color(0, 0, 0, 0));
            r.Set(1);
            r.transform.localScale = new Vector3(w, w * 0.3f, 1);
        }

        // 판 — 화면 왼쪽 38%(폰도 같은 비율, 글은 K 배), 위에서 아래로 쌓고 넘치면 판 안에서 내린다
        static Modal Side(Transform parent, string name, Vector3? feet, float ringW, Action<Stack, Action> build)
        {
            var m = Modal.Create(parent, name, new Vector2(6, 9), Vector3.zero, false, 0.12f);
            m.Panel.sprite = Res.UI("white");
            m.Panel.drawMode = SpriteDrawMode.Simple;
            m.Panel.color = new Color(0.03f, 0.04f, 0.09f, 1f);
            var edge = Make.Box("edge", m.transform, Res.UI("white"), Vector3.zero, new Vector2(0.014f, 30f), Modal.O + 3, new Color(1f, 0.86f, 0.55f, 0.35f));
            if (feet.HasValue) FootRing(m, feet.Value, ringW);
            var scroller = m.gameObject.AddComponent<SideScroll>();
            Action redraw = null;
            redraw = () =>
            {
                for (int i = m.Content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(m.Content.GetChild(i).gameObject);
                float k = Tone.K, hw = Tone.HalfW, hh = Tone.HalfH;
                float worldW = hw * 2 * 0.38f;
                Wd = worldW / k;
                var st = new Stack(m.Content);
                m.Content.localScale = Vector3.one;
                build(st, redraw);
                float contentH = (-st.Y + Pad) * k;
                var size = new Vector2(worldW, hh * 2);
                var center = new Vector3(-hw + worldW / 2, 0, 0);
                m.Panel.transform.localPosition = center;
                Make.Fit(m.Panel, size);
                edge.transform.localPosition = new Vector3(-hw + worldW, 0, 0);
                m.Fit(size, center, k);
                m.Panel.transform.localScale = new Vector3(size.x / m.Panel.sprite.bounds.size.x, size.y / m.Panel.sprite.bounds.size.y, 1);
                scroller.Setup(m, Mathf.Max(0, contentH - hh * 2), new Vector3(-hw, hh, 0));
            };
            redraw();
            return m;
        }

        // ── 사도(파티) ──
        public static Modal Hero(Transform parent, BattleSnapshot s, int i, Rect target)
        {
            var d = BattleDirector.I;
            Vector3? feet = d != null && i < d.Heroes.Count ? d.FieldRoot.TransformPoint(d.Heroes[i].Feet) : (Vector3?)null;
            return Side(parent, "hero", feet, 1.5f, (st, redraw) =>
            {
                var h = s.Heroes[i];
                // 파티 — HP · 방어 · 파티 상태
                st.Para("파티", Pad, Wd - Pad * 2, Tone.Lg, Tone.Ink, false, 0.02f);
                float pct = s.PartyMaxHp > 0 ? (float)s.PartyHp / s.PartyMaxHp : 0;
                PillR(st.T, Wd - Pad, st.Y + 0.2f, pct >= 0.7f ? "건강" : pct >= 0.3f ? "부상" : "위험", pct >= 0.7f ? new Color(0.2f, 0.55f, 0.36f) : pct >= 0.3f ? new Color(0.6f, 0.48f, 0.16f) : new Color(0.62f, 0.18f, 0.22f), Color.white);
                HpBar(st, Pad, Wd - Pad * 2, s.PartyHp, s.PartyMaxHp, s.PartyBlock, new Color(0.2f, 0.56f, 0.4f));
                StatusRows(st, s.PartyChips, "party", redraw);
                st.Rule(new Color(1f, 0.86f, 0.55f, 0.35f));
                st.Y -= 0.1f;

                // 그 사도 — 얼굴 · 이름 · 성격 · 역할 · 능력치
                float fd = 0.9f;
                var fc = new Vector3(Pad + fd / 2, st.Y - fd / 2, 0);
                Make.Box("rim", st.T, Res.UI("circle"), fc, new Vector2(fd + 0.06f, fd + 0.06f), OC, Color.Lerp(Tone.Nature(h.Nature), h.Tint, 0.35f));
                Make.Box("well", st.T, Res.UI("circle"), fc, new Vector2(fd, fd), OC + 1, Tone.Well);
                Tone.RoundFace("face", st.T, h.Key, fc, fd - 0.04f, OC + 3);
                float x = Pad + fd + 0.2f, w = Wd - x - Pad;
                float top = st.Y;
                st.Para(h.Name, x, w, Tone.Xl, Tone.Ink, false, 0.02f);
                Make.Box("nat", st.T, Res.UI("circle"), new Vector3(x + 0.08f, st.Y - 0.12f, 0), new Vector2(0.14f, 0.14f), OC, Tone.Nature(h.Nature));
                st.Para($"<color=#{ColorUtility.ToHtmlStringRGB(Tone.Nature(h.Nature))}>{h.Nature}</color><color={Tone.DimTag}>  ·  </color>{h.Role}", x + 0.22f, w - 0.22f, Tone.Sm, Tone.Sub, true, 0.04f);
                st.Para(Stat("공격", h.Atk, h.AtkNow) + "    " + Stat("방어", h.Def, h.DefNow) + "    " + Stat("치명", h.Crit, h.CritNow, "%"), x, w, Tone.Sm, Tone.Ink, true, 0.04f);
                st.Y = Mathf.Min(st.Y, top - fd - 0.08f);

                // 고학년
                st.Head("고학년");
                float ut = st.Y;
                float rd = 0.56f;
                var rc = new Vector3(Pad + rd / 2, ut - rd / 2, 0);
                bool ready = h.Ult >= h.UltMax;
                Make.Box("rb", st.T, Res.UI("circle"), rc, new Vector2(rd, rd), OC, new Color(0.04f, 0.06f, 0.13f, 0.95f));
                var ring = Tone.Ring("ring", st.T, rc, rd, 0.06f, OC + 1, ready ? new Color(1f, 0.86f, 0.5f) : new Color(0.78f, 0.68f, 0.48f), new Color(1, 1, 1, 0.14f));
                ring.Set(ready ? 1 : h.UltMax > 0 ? Mathf.Min(0.999f, (float)h.Ult / h.UltMax) : 0);
                // 가운데 — 그 사도의 원작 볼따구(원 안으로 오림). 덜 찼으면 회색. 그림이 없으면 번개
                var gface = Res.Sprite("Art/icon_graduateskill_" + h.Key);
                if (gface != null)
                {
                    var mk = Make.Node("rmask", st.T, rc);
                    var m = mk.gameObject.AddComponent<SpriteMask>();
                    m.sprite = Res.UI("circle");
                    m.isCustomRangeActive = true; m.frontSortingOrder = OC + 3; m.backSortingOrder = OC + 1;
                    var mb = m.sprite.bounds.size;
                    mk.localScale = new Vector3((rd - 0.12f) / mb.x, (rd - 0.12f) / mb.y, 1);
                    var gf = Make.Box("rface", st.T, gface, rc, new Vector2(rd - 0.12f, rd - 0.12f), OC + 2, ready ? Color.white : new Color(0.55f, 0.55f, 0.6f));
                    gf.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
                else Make.Box("rbolt", st.T, Res.UI("ic_bolt"), rc, new Vector2(0.26f, 0.26f), OC + 2, ready ? Color.white : new Color(0.7f, 0.7f, 0.75f));
                float ux = Pad + rd + 0.16f, uw = Wd - ux - Pad;
                st.Para($"<color={Tone.GoldTag}>{h.UltName}</color>", ux, uw - 1.6f, Tone.Lg, Tone.Ink, false, 0.03f);
                PillR(st.T, Wd - Pad, ut - 0.16f, ready ? "쓸 수 있음" : $"{h.Ult}% / {h.UltMax}%", ready ? new Color(0.2f, 0.5f, 0.34f) : new Color(0.22f, 0.25f, 0.34f), Color.white);
                st.Para(Orange(h.UltText), ux, uw, Tone.Sm, Tone.Ink, true, 0.06f);
                st.Y = Mathf.Min(st.Y, ut - rd - 0.08f);

                // 고유 효과 → 패시브 — core CardText.Traits(판 화면 사도 상세와 같은 차례 · 글). 접지 않고 다 보인다(판이 길면 휠 · 끌기로 내린다)
                foreach (var kind in new[] { "고유 효과", "패시브" })
                {
                    bool any = false;
                    foreach (var t in h.Traits)
                    {
                        if (t.Kind != kind) continue;
                        if (!any) { st.Head(kind); any = true; }
                        string stacks = kind == "고유 효과" && t.Name == h.KeywordName ? $"  <color={Tone.GoldTag}>{h.KeywordStacks}</color>" : "";
                        Entry(st, (kind == "고유 효과" ? $"<color={Tone.GoldTag}>「{t.Name}」</color>" : t.Name) + stacks, t.Sub, t.Body);
                    }
                }
                if (h.Chips.Count > 0) { st.Head("상태"); StatusRows(st, h.Chips, h.Key, redraw); }
            });
        }

        // ── 적 ──
        public static Modal Enemy(Transform parent, BattleSnapshot s, int i, Rect target)
        {
            var d = BattleDirector.I;
            Vector3? feet = d != null && i < d.Enemies.Count ? d.FieldRoot.TransformPoint(d.Enemies[i].Feet) : (Vector3?)null;
            return Side(parent, "enemy", feet, s.Enemies[i].Boss ? 2.6f : 1.8f, (st, redraw) =>
            {
                var e = s.Enemies[i];
                // 머리 — (보스) 꼬리표 · 이름 크게 · 체력 구간 알약 / 성격 점 · 이름 · ⓘ 약점
                float top = st.Y;
                float x = Pad;
                if (e.Boss)
                {
                    Make.Sliced("boss", st.T, Res.UI("bar_fill_9s"), new Vector3(Pad + 0.32f, top - 0.27f, 0), new Vector2(0.62f, 0.32f), OC, Tone.Rose);
                    Make.Text("bosst", st.T, "보스", new Vector3(Pad + 0.32f, top - 0.275f, 0), 0.16f, OC + 1, Color.white);
                    x += 0.74f;
                }
                st.Para(e.Name, x, Wd - x - Pad - 1.0f, Tone.Xl, Tone.Ink, false, 0.04f);
                float pct = e.MaxHp > 0 ? (float)e.Hp / e.MaxHp : 0;
                // 체력 구간 알약 — 격파는 여기 말고 강인도 줄의 「격파됨」 하나로(금색 「격파」 알약이 약점 속성 아이콘을 덮었다, 2026-10-07)
                PillR(st.T, Wd - Pad, top - 0.26f, e.Dead ? "쓰러짐" : pct >= 0.7f ? "건강" : pct >= 0.3f ? "부상" : "위험",
                    pct >= 0.7f ? new Color(0.2f, 0.55f, 0.36f) : pct >= 0.3f ? new Color(0.6f, 0.48f, 0.16f) : new Color(0.62f, 0.18f, 0.22f), Color.white, 0.15f);
                // 성격 · 약점 줄은 알약(아래 끝 top - 0.405) 밑으로 — 이름이 한 줄로 짧아도 겹치지 않게
                float ry = Mathf.Min(st.Y - 0.13f, top - 0.405f - 0.2f);
                EnemyHud.WeakBadge(Make.Node("nat", st.T, new Vector3(Pad + 0.11f, ry, 0)), e.Nature, 0.24f, OC);
                Tone.Text("natn", st.T, e.Nature, new Vector3(Pad + 0.28f, ry, 0), Tone.Body, OC, Tone.Ink, TextAlignmentOptions.Left, true);
                bool toughless = e.ToughMaxV <= 0.001f;
                if (e.Weak.Count > 0 && !toughless)
                {
                    float wx = Wd - Pad;
                    // 약점 속성 — 머리 위와 같은 성격 아이콘(판 화면 RunArt) + 이름
                    for (int k = e.Weak.Count - 1; k >= 0; k--)
                    {
                        var wn = Tone.Text("wkn" + k, st.T, e.Weak[k], new Vector3(wx, ry, 0), Tone.Body, OC, Tone.Nature(e.Weak[k]), TextAlignmentOptions.Right, true);
                        wn.ForceMeshUpdate();
                        wx -= wn.preferredWidth + 0.17f;
                        EnemyHud.WeakBadge(Make.Node("wk" + k, st.T, new Vector3(wx, ry, 0)), e.Weak[k], 0.26f, OC);
                        wx -= 0.2f;
                    }
                    var wt = Tone.Text("wkt", st.T, "약점 속성", new Vector3(wx - 0.02f, ry, 0), Tone.Sm, OC, Tone.Sub, TextAlignmentOptions.Right, true);
                    wt.ForceMeshUpdate();
                    TipZone.Add(wt, new Vector2(wt.preferredWidth + (Wd - Pad - wx), 0.3f), () => EnemyHud.WeakTip(e.Weak), 6, new Vector2((Wd - Pad - wx) / 2, 0));
                }
                st.Y = ry - 0.24f;
                HpBar(st, Pad, Wd - Pad * 2, e.Hp, e.MaxHp, e.Block, new Color(0.9f, 0.27f, 0.34f));
                // 강인도 — 머리 위와 같은 칸 막대(1/3 칸도), 오른쪽에 값, 밑에 격파 설명 한 줄
                float py = st.Y - 0.1f;
                float tmax = e.ToughMaxV;
                if (tmax > 0)
                {
                    float tw = Mathf.Min(Wd - Pad * 2 - 1.7f, 0.62f * Mathf.Ceil(tmax) + 0.5f);
                    var tb = ToughBar.Create(st.T, new Vector3(Pad + tw / 2, py, 0), tw, 0.11f, tmax, OC, false);
                    tb.Set(e.ToughV, false);
                    tb.SetBroken(e.Broken, false);
                    Tone.Text("tt", st.T, e.Broken ? "<color=#ffd65a>격파됨</color>  <size=85%>— 이번 차례를 쉽니다</size>"
                        : $"강인도 <color=#d9c9ff>{EnemyHud.Thirds(e.ToughV)}</color> <color={Tone.DimTag}>/ {EnemyHud.Thirds(tmax)}</color>",
                        new Vector3(Pad + tw + 0.14f, py, 0), Tone.Sm, OC, Tone.Sub, TextAlignmentOptions.Left, true);
                    py -= 0.24f;
                    Tone.Text("tn", st.T, e.Broken ? "다음 차례가 오면 일어나 강인도가 다시 가득 찹니다" : "격파되면 1턴 동안 행동하지 못합니다 · 격파한 쪽 AP +1",
                        new Vector3(Pad, py, 0), Tone.Cap, OC, Tone.Sub, TextAlignmentOptions.Left, true, Wd - Pad * 2);
                }
                else
                {
                    // 강인도가 없는 적(보스가 부르는 몹) — 막대 대신 한 줄
                    Tone.Text("tt", st.T, "강인도 없음 — 격파되지 않습니다", new Vector3(Pad, py, 0), Tone.Sm, OC, Tone.Dim, TextAlignmentOptions.Left, true, Wd - Pad * 2);
                }
                st.Y = py - 0.2f;

                // 다음 행동 칸
                st.Head("다음 행동");
                float it = st.Y;
                if (e.Dead) st.Para("쓰러졌습니다", Pad, Wd - Pad * 2, Tone.Body, Tone.Dim);
                else if (e.Broken || e.Intent == IntentKind.None) st.Para(e.Broken ? "격파 — 1턴 동안 행동하지 못한다(격파한 쪽 AP +1)." : "할 일 없음", Pad, Wd - Pad * 2, Tone.Body, Tone.Sub);
                else
                {
                    var k = e.Intent;
                    bool hit = k == IntentKind.Attack || k == IntentKind.Heavy;
                    var dc = new Vector3(Pad + 0.26f, it - 0.26f, 0);
                    Color rim = k == IntentKind.Heavy ? new Color(1f, 0.5f, 0.25f) : hit ? new Color(0.95f, 0.42f, 0.45f) : k == IntentKind.Defend ? new Color(0.45f, 0.7f, 1f) : new Color(0.72f, 0.55f, 1f);
                    Make.Box("ir", st.T, Res.UI("diamond"), dc, new Vector2(0.52f, 0.52f), OC, rim);
                    Make.Box("ig", st.T, Res.UI("diamond"), dc, new Vector2(0.43f, 0.43f), OC + 1, new Color(0.06f, 0.09f, 0.2f));
                    Make.Box("ii", st.T, Res.UI(k == IntentKind.Defend ? "ic_shield" : k == IntentKind.Buff ? "ic_up" : k == IntentKind.Debuff ? "ic_weak" : "ic_sword"), dc, new Vector2(0.27f, 0.27f), OC + 2);
                    float ix = Pad + 0.66f, iw = Wd - ix - Pad;
                    string kind = k == IntentKind.Heavy ? "큰 공격" : hit ? "공격" : k == IntentKind.Defend ? "막기" : k == IntentKind.Buff ? "강화" : "약화";
                    string v = hit || k == IntentKind.Defend ? $"  <color=#FFB45A>{e.IntentValue}{(hit && e.IntentHits > 1 ? $" × {e.IntentHits}" : "")}</color>" : "";
                    st.Para(kind + v, ix, iw, Tone.Lg, Tone.Ink, false, 0.04f);
                    int left = e.RushNeed - e.RushCnt;
                    string when = e.Sealed ? "봉인됨" : e.RushNeed <= 0 ? "적의 차례에" : e.RushedTurn ? "이번 턴에 즉시 행동함" : $"카드 {left}장 더 내면 즉시";
                    PillR(st.T, Wd - Pad, it - 0.18f, when, new Color(0.24f, 0.27f, 0.36f), new Color(0.86f, 0.88f, 0.95f), 0.13f);
                    st.Para(Orange(e.IntentText), ix, iw, Tone.Body, Tone.Ink, true, 0.04f);
                    if (!string.IsNullOrEmpty(e.IntentSay)) st.Para($"「{e.IntentSay}」", ix, iw, Tone.Sm, Tone.Dim, true, 0.04f);
                    st.Y = Mathf.Min(st.Y, it - 0.6f);
                }

                if (e.Chips.Count > 0) { st.Head("상태"); StatusRows(st, e.Chips, e.Id, redraw); }
                if (e.Passives.Count > 0)
                {
                    st.Head("패시브");
                    for (int p = 0; p < e.Passives.Count; p++) { var (n, t) = Split(e.Passives[p]); Entry(st, n, null, t); }
                }
                if (!string.IsNullOrEmpty(e.Blurb))
                {
                    st.Y -= 0.1f;
                    var b = st.Para(e.Blurb, Pad, Wd - Pad * 2, Tone.Sm, Tone.Dim, true, 0.02f);
                    b.fontStyle = FontStyles.Italic;
                }
            });
        }

        // 즉시 행동 — 「▶1/3 · 2장 뒤 즉시」
        public static string Rush(EnemyState e, bool longForm)
        {
            if (e.Sealed) return "<color=#a9a3b8>봉인됨</color>";
            if (e.RushNeed <= 0) return longForm ? Tip.Dim("▶ 즉시 행동으로 당겨지지 않습니다") : "";
            if (e.RushedTurn) return longForm ? "<color=#a9a3b8>▶ 이번 턴에는 이미 즉시 행동했습니다</color>" : "<color=#a9a3b8>▶끝</color>";
            int left = e.RushNeed - e.RushCnt;
            string col = left <= 1 ? "#ff7a6a" : "#ffe28a";
            return longForm
                ? $"<color={col}>▶{e.RushCnt}/{e.RushNeed}</color> — 카드를 {left}장 더 내면 이 수를 즉시 씁니다"
                : $"<color={col}>▶{e.RushCnt}/{e.RushNeed}</color>";
        }
    }

    // 정보 판 안 스크롤 — 휠 · 끌기(판 안에서만), 판 위 끝에서 흐리게 자른다(글은 마스크를 안 타므로 위로 밀린 것은 그냥 판 밖)
    public class SideScroll : MonoBehaviour
    {
        Modal m;
        float max, cur, dragY;
        bool drag;
        Vector3 topLeft;

        public void Setup(Modal modal, float scrollMax, Vector3 tl)
        {
            m = modal; max = scrollMax; topLeft = tl;
            cur = Mathf.Clamp(cur, 0, max);
            Apply();
        }

        void Apply()
        {
            if (m == null) return;
            var p = m.Content.localPosition;
            m.Content.localPosition = new Vector3(topLeft.x, topLeft.y + cur, 0);
            foreach (var r in m.Content.GetComponentsInChildren<Renderer>(true))
            {
                bool vis = r.bounds.max.y <= Tone.HalfH + 0.05f && r.bounds.min.y >= -Tone.HalfH - 0.05f;
                if (r.enabled != vis) r.enabled = vis;
            }
        }

        void Update()
        {
            if (m == null || max <= 0) return;
            var p = PointerInput.Pos;
            if (!m.Inside(p)) { drag = false; return; }
            float dlt = 0;
            if (!PointerInput.Simulated && UnityEngine.InputSystem.Mouse.current != null) dlt = -UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y * 0.01f;
            if (PointerInput.Down) { drag = true; dragY = p.y; }
            if (!PointerInput.Held) drag = false;
            if (drag && PointerInput.Moved) { dlt += p.y - dragY; dragY = p.y; }
            if (Mathf.Abs(dlt) < 1e-4f) return;
            cur = Mathf.Clamp(cur + dlt, 0, max);
            Apply();
        }
    }
}
