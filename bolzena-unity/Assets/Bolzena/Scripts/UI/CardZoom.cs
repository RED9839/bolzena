using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 카드 확대(배치는 카제나 카드 확대를 따른다 · 그림 · 글은 우리 것) — 카드를 크게(화면 높이의 약 60%) 띄우고,
    // 오른쪽에 카드 글에 나온 키워드마다 설명 판을 세로로 쌓는다(판 화면 TermPop 톤 — 아이콘 + 이름 / 풀이, 수치 금빛, 해로운 것은 붉은 테).
    // 오른쪽에 자리가 없으면 왼쪽에. 판 묶음이 화면보다 길면 여러 줄(열)로 나누고, 그래도 자리가 없으면 휠 · 끌기로 내린다.
    // 전투 · 판 밖 카드 확대는 모두 이것 하나: 손패 길게 누르기(가운데 위) · 마우스 올려 두기(그 카드 위) · 고른 카드(왼쪽) · 더미 보기 · 편성 화면.
    public static class CardZoom
    {
        static Transform root;
        static CardView card;
        static string shownId;
        public static bool Shown => root != null;
        /// <summary>점검 — 확대 카드 가운데(월드) · 확대 배율 · 키워드 판이 카드 오른쪽인가 · 판 수.</summary>
        public static Vector3 At => root != null ? root.position : Vector3.zero;
        public static float Scale => root != null && card != null ? card.TargetScale : 0;
        public static bool PanelsRight { get { var pn = root != null ? root.Find("panels") : null; return pn == null || pn.localPosition.x > 0; } }
        public static int PanelCount => root != null ? boxes.Count : 0;
        public const int O = 1500;
        const float PanelW = 3.5f;

        /// <summary>at — 카드 가운데(월드). scale 0 이면 화면 높이에 맞춘다. 같은 카드면 자리만 옮긴다.</summary>
        public static void Show(Transform parent, CardInfo info, Vector3 at, float scale = 0, bool panelsLeft = false, string note = null)
        {
            float k = Tone.K;
            float s = scale > 0 ? scale : Mathf.Min(2.1f, (Tone.HalfH * 2 * 0.6f) / CardView.H);
            string key = info.Id + "|" + info.Text + "|" + note;
            if (root != null && shownId == key) { Place(at, s, panelsLeft); return; }
            Hide();
            shownId = key;
            root = Make.Node("CardZoom", parent);
            card = CardView.Create(root, info);
            card.ShowDesc = true; card.ShowPin = false; card.Playable = true;   // 확대 카드는 오른쪽 위 얼굴 배지 없이(2026-10-08 사용자)
            card.TargetScale = s; card.TargetRot = 0; card.TargetPos = Vector3.zero;
            card.Snap();
            card.SetOrder(O);
            Make.Box("shadow", root, Res.UI("soft"), new Vector3(0, -0.2f, 0), new Vector2(CardView.W * s * 1.5f, CardView.H * s * 1.2f), O - 5, new Color(0, 0, 0, 0.55f));

            // 키워드 판들 — 판 하나 = 마디 하나(Place 가 한 줄 또는 여러 줄로 세운다)
            var panels = Make.Node("panels", root);
            boxes.Clear(); boxH.Clear();
            float y = 0;
            var terms = new List<Term>(info.Terms ?? new List<Term>());
            if (note != null) terms.Insert(0, new Term("지금은 못 냅니다", note, "no"));
            for (int i = 0; i < terms.Count; i++)
            {
                var pn = Make.Node("p" + i, panels, new Vector3(0, y, 0));
                float h = Panel(pn, terms[i], k);
                boxes.Add(pn); boxH.Add(h);
                y -= h + Gap;
            }
            panelsH = -y;
            Place(at, s, panelsLeft);
        }

        static float panelsH;
        static readonly List<Transform> boxes = new List<Transform>();
        static readonly List<float> boxH = new List<float>();
        const float Gap = 0.08f, ColGap = 0.12f;

        // 낱말 판 하나(판 화면 TermPop.Box 와 같은 톤) — 남색 판 · 성격 테두리(해로운 것 붉게 · 고유 효과 · 신탁 금 · 생성 카드 하늘) ·
        // 머리줄 = 아이콘 + 이름(금) + 작은 갈래 글, 그 아래 수치가 다 든 글(수치 금빛). 돌려줌: 판 높이
        static float Panel(Transform pn, Term t, float k)
        {
            float pw = PanelW * k;
            bool isCard = t.Kind == "card" && t.Card != null;
            bool bad = t.Kind == "no" || (t.Kind == "status" && Bolzena.Core.R.IsBadSt(t.Word));
            bool form = t.Kind == "form";   // 사도 변신(성전 모드 · 맨주먹 전성기 …)
            bool hero = form || (t.Kind == "kw" && !Terms.Words.ContainsKey(t.Word));   // 엔진 화면 낱말이 아닌 「X」 = 사도 고유 효과
            Color edge = bad ? Tone.Bad : isCard ? Tone.Sky : (hero || t.Kind == "flash") ? Tone.Gold : Tone.Edge;
            string kind = form ? "변신" : hero ? "고유 효과" : isCard ? "카드" : t.Kind == "status" && bad ? "디버프" : null;
            Color nameC = bad ? Tone.Bad : isCard ? Tone.Sky : Tone.Gold;

            // 머리줄 — 아이콘 + 이름
            float ic = 0.24f * k, hx = 0.2f, hy = -0.14f;
            string stIc = t.Kind == "status" ? ChipRow.IconOf(t.Word, !bad) : null;   // 상태 — 적 머리 위 칩과 같은 그림
            Color tint = stIc == null ? edge : stIc.StartsWith("state:") ? Color.white : bad ? Tone.Bad : Tone.Good;
            if (t.Kind == "kw" || t.Kind == "tag") ic *= 0.7f;   // 키워드 · 태그는 작은 마름모
            var icon = Make.Box("ic", pn, stIc != null ? ChipRow.IconSprite(stIc) : IconFor(t), new Vector3(hx + 0.12f * k, hy - 0.13f * k, 0), new Vector2(ic, ic), O + 3, tint);
            ic = 0.24f * k;
            if (icon.sprite == null) icon.enabled = false;
            float nx = hx + (icon.enabled ? ic + 0.08f : 0);
            Tone.Text("h", pn, t.Word + (kind != null ? $"  <size=66%><color={Tone.SubTag}>{kind}</color></size>" : ""), new Vector3(nx, hy, 0), Tone.Md * k, O + 3, nameC, TextAlignmentOptions.TopLeft, false, pw - nx - 0.15f);
            float h;
            if (isCard)
            {
                // 생성 카드 — 머리줄 + 작은 글 + 작은 카드(코스트 · 이름 · 종류 · 효과 글)
                float cs = 0.62f * k, chh = CardView.H * cs;
                Tone.Text("s", pn, t.Text, new Vector3(hx, hy - 0.3f * k, 0), Tone.Cap * k, O + 3, Tone.Sub, TextAlignmentOptions.TopLeft, false, pw - 0.4f);
                float top = 0.14f + 0.3f * k + 0.26f * k;
                var mini = CardView.Create(pn, t.Card);
                mini.ShowDesc = true; mini.ShowPin = false; mini.Playable = true;
                mini.TargetScale = cs; mini.TargetRot = 0; mini.TargetPos = new Vector3(pw / 2, -top - chh / 2, 0);
                mini.Snap(); mini.SetOrder(O + 4);
                h = top + chh + 0.16f;
            }
            else
            {
                string bodyText = Hilite(t.Text);   // 어절 단위 · 이름 · 수치 덩이는 끊지 않음   // 수치 · 지속이 다 든 한 가지 글(고유 효과 = CardText.Trait · 엔진 낱말 = CardText.TIPS) — 「자세히」 없음
                var body = Tone.Text("b", pn, bodyText, new Vector3(hx, hy - 0.34f * k, 0), Tone.Sm * k, O + 3, Tone.Ink, TextAlignmentOptions.TopLeft, true, pw - 0.4f * k);
                body.textWrappingMode = TextWrappingModes.Normal;
                body.lineSpacing = -2;
                body.text = Bolzena.RunUI.CardTerms.FitFor(body, bodyText, pw - 0.4f * k, body.fontSize);   // 어절 · 덩이 · 화살표 규칙
                body.rectTransform.sizeDelta = new Vector2(pw - 0.4f * k, 10);
                body.ForceMeshUpdate();
                h = 0.14f + 0.34f * k + body.preferredHeight + 0.18f;
            }
            Make.Sliced("rim", pn, Res.UI("bar_fill_9s"), new Vector3(pw / 2, -h / 2, 0), new Vector2(pw + 0.03f, h + 0.03f), O, Tone.A(edge, bad || hero || t.Kind == "flash" || isCard ? 0.75f : 0.45f));
            Make.Sliced("bg", pn, Res.UI("bar_fill_9s"), new Vector3(pw / 2, -h / 2, 0), new Vector2(pw, h), O + 1, Tone.A(Tone.Navy, 0.97f));
            Make.Box("top", pn, Res.UI("white"), new Vector3(pw / 2, -0.03f, 0), new Vector2(pw - 0.16f, 0.03f), O + 2, Tone.A(edge, bad || hero || t.Kind == "flash" ? 1f : 0.6f));
            return h;
        }

        static Sprite IconFor(Term t)
        {
            if (t.Kind == "card") return Res.UI("ic_cards");
            if (t.Kind == "no") return Res.UI("ic_close");
            if (t.Kind == "flash")
            {
                if (t.Word.StartsWith("축복")) return Bolzena.RunUI.Theme.S("ic_bless");
                if (t.Word == "복제") return Bolzena.RunUI.Theme.S("ic_copy");
                return Bolzena.RunUI.Theme.S("ic_spark");
            }
            return Res.UI("diamond");
        }

        static readonly List<float> rightX = new List<float>(), leftX = new List<float>(), slots = new List<float>();
        static readonly List<Vector3> pos = new List<Vector3>();

        static void Place(Vector3 at, float s, bool left)
        {
            if (root == null) return;
            float k = Tone.K, hw = Tone.HalfW, hh = Tone.HalfH;
            float cw = CardView.W * s, ch = CardView.H * s;
            at.x = Mathf.Clamp(at.x, -hw + cw / 2 + 0.15f, hw - cw / 2 - 0.15f);
            at.y = Mathf.Clamp(at.y, -hh + ch / 2 + 0.1f, hh - ch / 2 - 0.1f);
            root.localPosition = at;
            var panels = root.Find("panels");
            float pw = PanelW * k;
            // 판 줄(열)이 설 자리 — 먼저 고른 쪽(보통 오른쪽)에서 바깥으로, 모자라면 반대쪽
            rightX.Clear(); leftX.Clear(); slots.Clear(); pos.Clear();   // 손패 확대는 매 프레임 부른다 — 목록을 새로 만들지 않는다
            for (float x = cw / 2 + 0.15f; at.x + x + pw <= hw - 0.1f; x += pw + ColGap) rightX.Add(x);
            for (float x = -cw / 2 - 0.15f - pw; at.x + x >= -hw + 0.1f; x -= pw + ColGap) leftX.Add(x);
            if (left) { slots.AddRange(leftX); slots.AddRange(rightX); } else { slots.AddRange(rightX); slots.AddRange(leftX); }
            if (slots.Count == 0) slots.Add(left ? -cw / 2 - 0.15f - pw : cw / 2 + 0.15f);
            float room = hh * 2 - 0.2f;
            float py = Mathf.Min(ch / 2 - 0.1f, hh - 0.1f - at.y);
            var sc = panels.GetComponent<ZoomScroll>() ?? panels.gameObject.AddComponent<ZoomScroll>();

            // 한 줄로 화면에 들면 한 줄(아래가 넘치면 위로 올린다)
            float oneH = panelsH - Gap;
            if (oneH > room && slots.Count > 1)
            {
                // 화면보다 길면 여러 줄로 나눈다 — 위는 화면 위에 맞추고, 판을 차례대로 채운다
                py = hh - 0.1f - at.y;
                int col = 0; float cy = 0; bool fits = true;
                for (int i = 0; i < boxes.Count; i++)
                {
                    if (cy > 0 && cy + boxH[i] > room) { col++; cy = 0; }
                    if (col >= slots.Count || boxH[i] > room) { fits = false; break; }
                    pos.Add(new Vector3(slots[col], -cy, 0));
                    cy += boxH[i] + Gap;
                }
                if (fits)
                {
                    for (int i = 0; i < boxes.Count; i++) boxes[i].localPosition = pos[i];
                    panels.localPosition = new Vector3(0, py, 0);
                    sc.Setup(panels.localPosition, 0, pw);
                    return;
                }
            }
            // 한 줄 — 판을 차례대로 쌓는다
            float y = 0;
            for (int i = 0; i < boxes.Count; i++) { boxes[i].localPosition = new Vector3(0, y, 0); y -= boxH[i] + Gap; }
            float px = slots[0];
            if (at.y + py - oneH < -hh + 0.1f) py = -hh + 0.1f - at.y + oneH;
            // 그래도 화면보다 길면(자리가 한 줄뿐인 좁은 화면) 위를 화면 위에 맞추고 휠 · 끌기로 내린다 — 글을 자르거나 접지 않는다
            if (oneH > room) py = hh - 0.1f - at.y;
            panels.localPosition = new Vector3(px, py, 0);
            sc.Setup(panels.localPosition, Mathf.Max(0, oneH - room), pw);
        }

        /// <summary>카드 확대 옆 낱말 판 묶음 — 화면보다 길면 휠 · 끌기로 내린다(화면 밖 조각은 숨긴다).</summary>
        public class ZoomScroll : MonoBehaviour
        {
            Vector3 home; float max, cur, w, dragY; bool drag;
            readonly List<Renderer> hidden = new List<Renderer>();   // 화면 밖이라 이 스크롤이 끈 조각만(카드가 일부러 끈 빛줄기 따위는 건드리지 않는다)
            public void Setup(Vector3 h, float m, float width) { home = h; max = m; w = width; cur = Mathf.Clamp(cur, 0, max); Apply(); }
            void Apply()
            {
                transform.localPosition = home + new Vector3(0, cur, 0);
                if (max <= 0)
                {
                    // 스크롤이 필요 없어졌다(여러 줄로 나눔) — 숨겼던 조각을 다시 켠다
                    if (hidden.Count > 0) { foreach (var r in hidden) if (r) r.enabled = true; hidden.Clear(); }
                    return;
                }
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    bool vis = r.bounds.max.y <= Tone.HalfH + 0.05f && r.bounds.min.y >= -Tone.HalfH - 0.05f;
                    if (!vis && r.enabled) { r.enabled = false; hidden.Add(r); }
                    else if (vis && !r.enabled && hidden.Remove(r)) r.enabled = true;
                }
            }
            void Update()
            {
                if (max <= 0) return;
                var p = PointerInput.Pos;
                var lp = transform.parent.InverseTransformPoint(p);
                bool inside = lp.x >= home.x && lp.x <= home.x + w;
                if (!inside) { drag = false; return; }
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

        public static void Hide()
        {
            if (root != null) Object.Destroy(root.gameObject);
            root = null; card = null; shownId = null;
        }

        // 풀이 글 — 수치 금빛
        static readonly System.Text.RegularExpressions.Regex num = new System.Text.RegularExpressions.Regex(@"(?<![#\w])([+\-]?\d+(?:\.\d+)?%?)");
        static string Hilite(string rich)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < rich.Length)
            {
                int lt = rich.IndexOf('<', i);
                string plain = lt < 0 ? rich.Substring(i) : rich.Substring(i, lt - i);
                sb.Append(num.Replace(plain, "<color=" + Tone.GoldTag + ">$1</color>"));
                if (lt < 0) break;
                int gt = rich.IndexOf('>', lt);
                if (gt < 0) { sb.Append(rich.Substring(lt)); break; }
                sb.Append(rich, lt, gt - lt + 1);
                i = gt + 1;
            }
            return sb.ToString();
        }
    }
}
