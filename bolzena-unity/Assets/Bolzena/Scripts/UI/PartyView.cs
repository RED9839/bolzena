using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bolzena.UI
{
    // 파티 창 — 전체 화면(배치는 카제나 파티 정보 창을 따른다 · 그림 · 아이콘 · 글은 우리 것). 더미 보기와 같은 틀:
    //   왼쪽 세로 탭 셋 ① 전투원 ② 효과 ③ 카드 · 위 왼쪽 제목 · 오른쪽 위 체력 알약 + 큰 ×.
    //   파티 HP 를 누르면 ② 효과 탭이 먼저, 사도 초상 · 싸움터 사도를 누르면 ① 전투원 탭.
    //   ② 효과 — 가운데 세로선으로 둘: 왼쪽 「적용 중인 효과」(파티 층 · 사도 개인 상태, 건 쪽 초상) · 오른쪽 「적용 중인 패시브」(고유 효과 · 패시브 · 장비)
    //   ① 전투원 — 사도 셋을 세로 열로: 가로 초상 판(왼쪽 위 성격 · 역할 · 자리 아이콘) · 이름 + 돋보기(고학년 · 패시브 펼치기) · 능력치 표 · 고유 효과 · 장비 칸 셋
    //   ③ 카드 — 이 전투의 덱 전부(사도별 기본 → 고유, 맨 끝 교주 · 상태). 누르면 크게.
    public static class PartyView
    {
        public static Modal Show(Transform parent, BattleSnapshot s, int tab, int hero)
        {
            var m = Modal.Create(parent, "party", new Vector2(60, 30), Vector3.zero, false, 0.965f);   // 선형 색 공간이라 알파가 덜 어둡게 보인다 — 0.96 이 눈으로 0.85 쯤
            m.Panel.enabled = false;
            m.gameObject.AddComponent<PartyUi>().Init(m, s, tab, hero);
            return m;
        }
    }

    public class PartyUi : MonoBehaviour
    {
        Modal m;
        BattleSnapshot s;
        int tab, focus;
        readonly HashSet<int> opened = new HashSet<int>();
        // 카드 탭 스크롤
        Transform cards;
        readonly List<CardView> made = new List<CardView>();
        readonly List<Transform> heads = new List<Transform>();
        float scroll, scrollMax, top, dragY;
        bool dragging;
        const int OC = Modal.O + 5;
        static readonly string[] Icons = { "ic_party", "ic_spark", "ic_cards" };
        static readonly string[] Titles = { "전투원", "효과", "카드" };
        public static System.Action<string> Emit;

        public void Init(Modal modal, BattleSnapshot snap, int t, int hero)
        {
            m = modal; s = snap; focus = hero;
            if (hero >= 0) opened.Add(hero);
            Draw(t);
        }

        void OnDestroy() => CardZoom.Hide();

        TextMeshPro T(string name, Transform p, string text, Vector3 at, float size, Color c, TextAlignmentOptions al = TextAlignmentOptions.Left, bool body = false, float w = 10f, int o = 0)
        {
            var t = Tone.Text(name, p, text, at, size, OC + 2 + o, c, al, body, w);
            return t;
        }

        void Draw(int t)
        {
            tab = t;
            scroll = 0; scrollMax = 0; made.Clear(); cards = null;
            CardZoom.Hide();
            var R = m.Content;
            for (int i = R.childCount - 1; i >= 0; i--) Destroy(R.GetChild(i).gameObject);
            R.localPosition = Vector3.zero; R.localScale = Vector3.one;
            float hw = Tone.HalfW, hh = Tone.HalfH, k = Tone.K;
            // 탭 줄
            float railX = -hw + 0.62f * k, lineX = -hw + 1.12f * k;
            Make.Box("rail", R, Res.UI("white"), new Vector3(lineX, 0, 0), new Vector2(0.012f, hh * 2), OC, new Color(1, 1, 1, 0.16f));
            for (int i = 0; i < 3; i++)
            {
                int ii = i;
                var at = new Vector3(railX, hh - (0.62f + i * 0.95f) * k, 0);
                bool on = i == t;
                if (on) Make.Box("tglow", R, Res.UI("soft"), at, new Vector2(1.2f, 1.2f) * k, OC, new Color(1f, 0.78f, 0.38f, 0.45f), Res.SpriteMat(true, 1.3f));
                var ic = Make.Box("tab" + i, R, Res.UI(Icons[i]), at, new Vector2(0.5f, 0.5f) * k, OC + 1, on ? Tone.Gold : new Color(0.62f, 0.66f, 0.78f, 0.8f));
                Make.Box("tdot" + i, R, Res.UI("circle"), new Vector3(lineX, at.y, 0), Vector2.one * (on ? 0.16f : 0.06f) * k, OC + 2, on ? Tone.Gold : new Color(1, 1, 1, 0.35f));
                var b = ic.gameObject.AddComponent<Button>();
                b.Size = Vector2.one * 0.9f * k / ic.transform.localScale.x;
                b.OnClick = () => { if (ii != tab) Draw(ii); };
                TipZone.Add(ic, b.Size, () => Titles[ii], 120);
            }
            float x0 = lineX + 0.42f * k;
            Make.Box("ticon", R, Res.UI(Icons[t]), new Vector3(x0 + 0.18f * k, hh - 0.5f * k, 0), new Vector2(0.34f, 0.34f) * k, OC + 1, Tone.Ink);
            T("title", R, Titles[t], new Vector3(x0 + 0.44f * k, hh - 0.5f * k, 0), Tone.Xl * k, Tone.Ink);
            var cx = Make.Box("close", R, Res.UI("ic_close"), new Vector3(hw - 0.62f * k, hh - 0.5f * k, 0), new Vector2(0.52f, 0.52f) * k, OC + 1, new Color(0.95f, 0.95f, 0.98f));
            var cb = cx.gameObject.AddComponent<Button>();
            cb.Size = Vector2.one * 0.9f * k / cx.transform.localScale.x;
            cb.OnClick = m.Close;
            // 체력 알약 — 「건강 · 체력 2,140(100%)」
            float pct = s.PartyMaxHp > 0 ? (float)s.PartyHp / s.PartyMaxHp : 0;
            string band = pct >= 0.7f ? "건강" : pct >= 0.3f ? "부상" : "위험";
            Color bc = pct >= 0.7f ? new Color(0.16f, 0.5f, 0.32f) : pct >= 0.3f ? new Color(0.58f, 0.45f, 0.14f) : new Color(0.6f, 0.16f, 0.2f);
            float pw = 4.0f * k, ph = 0.44f * k;
            var pc = new Vector3(hw - 1.3f * k - pw / 2, hh - 0.5f * k, 0);
            Make.Sliced("hpbg", R, Res.UI("bar_fill_9s"), pc, new Vector2(pw, ph), OC + 1, new Color(bc.r, bc.g, bc.b, 0.4f));
            Make.Sliced("hpf", R, Res.UI("bar_fill_9s"), pc + new Vector3(-pw / 2 + pw * pct / 2, 0, 0), new Vector2(Mathf.Max(0.3f, pw * pct), ph), OC + 1, bc);
            T("hpb", R, band, pc + new Vector3(-pw / 2 + 0.2f * k, 0, 0), Tone.Md * k, Color.white, TextAlignmentOptions.Left, false, 10, 1);
            T("hpv", R, $"HP {s.PartyHp:N0}({Mathf.RoundToInt(pct * 100)}%)" + (s.PartyBlock > 0 ? $"  <color={Tone.SkyTag}>방어 {s.PartyBlock}</color>" : ""),
                pc + new Vector3(pw / 2 - 0.2f * k, 0, 0), Tone.Md * k, Color.white, TextAlignmentOptions.Right, false, 10, 1);
            top = hh - 1.0f * k;
            Make.Box("headline", R, Res.UI("white"), new Vector3((lineX + hw - 0.3f) / 2, top, 0), new Vector2(hw - 0.3f - lineX, 0.012f), OC, new Color(1, 1, 1, 0.16f));
            Tooltip.Ceiling = top - 0.1f;
            float areaL = lineX + 0.5f * k, areaR = hw - 0.4f * k;
            if (t == 0) Members(R, areaL, areaR, top - 0.25f * k, k);
            else if (t == 1) Effects(R, areaL, areaR, top - 0.3f * k, k);
            else Deck(R, areaL, areaR, top - 0.3f * k, k);
            Emit?.Invoke("party_tab" + t);
        }

        // ── ② 효과 ──
        void Effects(Transform R, float l, float r, float y0, float k)
        {
            float mid = (l + r) / 2;
            Make.Box("div", R, Res.UI("white"), new Vector3(mid, (y0 - Tone.HalfH) / 2, 0), new Vector2(0.012f, y0 + Tone.HalfH - 0.2f), OC, new Color(1, 1, 1, 0.14f));
            // 왼쪽 — 적용 중인 효과
            var rows = new List<(StatusChip c, string who)>();
            foreach (var c in s.PartyChips) rows.Add((c, null));
            foreach (var h in s.Heroes) foreach (var c in h.Chips) if (c.Kind != "key") rows.Add((c, h.Key));
            Column(R, l + 0.2f, mid - 0.3f, y0, k, "적용 중인 효과", "적용 중인 효과가 없습니다.", rows.Count, (i, x, w, y) => EffectRow(R, rows[i].c, rows[i].who, x, w, y, k));
            // 오른쪽 — 적용 중인 패시브(고유 효과 · 패시브 · 장비)
            var pas = new List<(string head, string text, string who, bool gold)>();
            foreach (var h in s.Heroes)
            {
                if (!string.IsNullOrEmpty(h.KeywordName)) pas.Add(($"「{h.KeywordName}」 <color={Tone.GoldTag}>{h.KeywordStacks}</color>", h.KeywordText, h.Key, true));   // 고유 효과 글(CardText.Trait)
                foreach (var p in h.Passives) pas.Add((null, p, h.Key, false));
                foreach (var g in h.Gear) if (g.Id != null && !string.IsNullOrEmpty(g.Text)) pas.Add((g.Name, g.Text, h.Key, false));
            }
            Column(R, mid + 0.3f, r - 0.1f, y0, k, "적용 중인 패시브", "적용 중인 패시브가 없습니다.", pas.Count, (i, x, w, y) => PassiveRow(R, pas[i].head, pas[i].text, pas[i].who, x, w, y, k));
        }

        delegate float RowFn(int i, float x, float w, float y);

        void Column(Transform R, float x, float x1, float y, float k, string head, string empty, int n, RowFn row)
        {
            float w = x1 - x;
            T("ch", R, head, new Vector3(x, y - 0.15f * k, 0), Tone.Lg * k, Tone.Gold);
            y -= 0.5f * k;
            if (n == 0)
            {
                float cy = (y - Tone.HalfH) / 2 + 0.3f;
                Make.Box("eic", R, Res.UI("ic_spark"), new Vector3(x + w / 2, cy + 0.35f * k, 0), new Vector2(0.5f, 0.5f) * k, OC + 1, new Color(0.8f, 0.82f, 0.9f, 0.7f));
                T("et", R, empty, new Vector3(x + w / 2, cy - 0.15f * k, 0), Tone.Body * k, Tone.Sub, TextAlignmentOptions.Center, true);
                return;
            }
            for (int i = 0; i < n && y > -Tone.HalfH + 0.6f; i++) y = row(i, x, w, y);
        }

        float Face(Transform R, string key, Vector3 at, float d)
        {
            Make.Box("fr", R, Res.UI("circle"), at, new Vector2(d + 0.05f, d + 0.05f), OC + 1, new Color(1, 1, 1, 0.5f));
            Tone.RoundFace("f", R, key, at, d, OC + 3);
            return d;
        }

        float EffectRow(Transform R, StatusChip c, string who, float x, float w, float y, float k)
        {
            bool bad = c.Kind == "debuff";
            var kc = ChipRow.KindColor(c.Kind);
            Make.Box("rule", R, Res.UI("white"), new Vector3(x + w / 2, y, 0), new Vector2(w, bad ? 0.02f : 0.012f), OC, bad ? new Color(1f, 0.4f, 0.45f, 0.75f) : Tone.Line);
            float top = y - 0.08f * k;
            var ic = new Vector3(x + 0.24f * k, top - 0.24f * k, 0);
            Make.Box("cib", R, Res.UI("circle"), ic, new Vector2(0.44f, 0.44f) * k, OC + 1, new Color(kc.r * 0.25f, kc.g * 0.25f, kc.b * 0.25f, 0.95f));
            var icon = ChipRow.IconOf(c);
            if (icon != null) Make.Box("ci", R, ChipRow.IconSprite(icon), ic, new Vector2(0.3f, 0.3f) * k, OC + 2);
            T("cv", R, c.Value, ic + new Vector3(0.2f, -0.16f, 0) * k, 0.13f * k, Color.white, TextAlignmentOptions.Center, false, 1f, 2);
            float right = x + w;
            var from = c.From != null ? c.From : who != null ? new List<string> { who } : null;
            if (from != null) for (int i = from.Count - 1; i >= 0; i--) { Face(R, from[i], new Vector3(right - 0.2f * k, top - 0.24f * k, 0), 0.34f * k); right -= 0.46f * k; }
            float tx = x + 0.56f * k, tw = right - tx - 0.06f;
            T("cn", R, $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Color.white, kc, 0.55f))}>{c.Id}</color>" + (c.Turns > 0 ? $"  <size=80%><color={Tone.SubTag}>{c.Turns}턴</color></size>" : ""),
                new Vector3(tx, top - 0.12f * k, 0), Tone.Body * k, Tone.Ink);
            var d = T("cd", R, Strip(c.Text, c.Id), new Vector3(tx, top - 0.3f * k, 0), Tone.Sm * k, Tone.Sub, TextAlignmentOptions.TopLeft, true, tw);
            d.textWrappingMode = TextWrappingModes.Normal; d.maxVisibleLines = 2; d.overflowMode = TextOverflowModes.Ellipsis;
            d.rectTransform.sizeDelta = new Vector2(tw, 0.6f * k);
            d.ForceMeshUpdate();
            return top - Mathf.Max(0.56f * k, 0.32f * k + Mathf.Min(d.preferredHeight, 0.46f * k)) - 0.08f * k;
        }

        float PassiveRow(Transform R, string head, string text, string who, float x, float w, float y, float k)
        {
            Make.Box("rule", R, Res.UI("white"), new Vector3(x + w / 2, y, 0), new Vector2(w, 0.012f), OC, Tone.Line);
            float top = y - 0.08f * k;
            Face(R, who, new Vector3(x + 0.24f * k, top - 0.24f * k, 0), 0.4f * k);
            float tx = x + 0.56f * k, tw = x + w - tx;
            if (head == null) { int d0 = text.IndexOf(" — "); if (d0 > 0 && d0 < 24) { head = text.Substring(0, d0); text = text.Substring(d0 + 3); } }
            float ty = top - 0.12f * k;
            if (head != null) { T("ph", R, head, new Vector3(tx, ty, 0), Tone.Body * k, Tone.Ink); ty -= 0.18f * k; }
            var d = T("pd", R, text, new Vector3(tx, ty - 0.02f, 0), Tone.Sm * k, Tone.Sub, TextAlignmentOptions.TopLeft, true, tw);
            d.textWrappingMode = TextWrappingModes.Normal; d.maxVisibleLines = 2; d.overflowMode = TextOverflowModes.Ellipsis;
            d.rectTransform.sizeDelta = new Vector2(tw, 0.6f * k);
            d.ForceMeshUpdate();
            return Mathf.Min(top - 0.56f * k, ty - Mathf.Min(d.preferredHeight, 0.46f * k)) - 0.1f * k;
        }

        static string Strip(string text, string id)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int d = text.IndexOf(" — ", System.StringComparison.Ordinal);
            return d >= 0 && d < id.Length + 8 ? text.Substring(d + 3) : text;
        }

        // ── ① 전투원 ──
        void Members(Transform R, float l, float r, float y0, float k)
        {
            int n = s.Heroes.Count;
            float gap = 0.4f * k;
            float cw = Mathf.Min(4.4f * k, (r - l - gap * (n - 1)) / n);
            float x = (l + r) / 2 - (n * cw + (n - 1) * gap) / 2;
            for (int i = 0; i < n; i++)
            {
                MemberCol(R, s.Heroes[i], i, x + i * (cw + gap), cw, y0, k);
                if (i < n - 1) Make.Box("cdiv", R, Res.UI("white"), new Vector3(x + i * (cw + gap) + cw + gap / 2, (y0 - Tone.HalfH) / 2, 0), new Vector2(0.012f, y0 + Tone.HalfH - 0.3f), OC, new Color(1, 1, 1, 0.1f));
            }
        }

        void MemberCol(Transform R, HeroState h, int idx, float x, float w, float y, float k)
        {
            bool hot = idx == focus;
            // 가로 초상 판 — 얼굴을 가로로 오려 성격 빛 바탕 위에
            float ph = w * 0.48f;
            var pc = new Vector3(x + w / 2, y - ph / 2, 0);
            var tint = Tone.Nature(h.Nature);
            Make.Sliced("pbg", R, Res.UI("bar_fill_9s"), pc, new Vector2(w, ph), OC, new Color(tint.r * 0.55f, tint.g * 0.5f, tint.b * 0.45f, 1f));
            var maskN = Make.Node("pm", R, pc);
            var mask = maskN.gameObject.AddComponent<SpriteMask>();
            mask.sprite = Res.UI("card_mask"); mask.isCustomRangeActive = true; mask.backSortingOrder = OC; mask.frontSortingOrder = OC + 2;
            var mb = mask.sprite.bounds.size; maskN.localScale = new Vector3(w / mb.x, ph / mb.y, 1);
            var sp = CardView.Face(h.Key, w / ph);
            if (sp != null) { var f = Make.Box("face", R, sp, pc + new Vector3(w * 0.12f, 0, 0), new Vector2(w, ph), OC + 1); f.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; }
            if (hot) Frame(R, pc, new Vector2(w + 0.04f, ph + 0.04f), Tone.Gold, 0.035f, OC + 3);
            // 왼쪽 위 아이콘 열 — 성격 · 역할
            string[] ics = { "성격_" + h.Nature, "역할_" + h.Role };
            for (int i = 0; i < ics.Length; i++)
            {
                var sp2 = Bolzena.RunUI.Theme.Icon(ics[i]);
                var at = new Vector3(x + 0.26f * k, y - 0.26f * k - i * 0.42f * k, 0);
                Make.Box("ib", R, Res.UI("bar_fill_9s"), at, new Vector2(0.38f, 0.38f) * k, OC + 3, new Color(0.04f, 0.05f, 0.12f, 0.85f));
                if (sp2 != null) Make.Box("ic", R, sp2, at, new Vector2(0.3f, 0.3f) * k, OC + 4);
            }
            y -= ph + 0.12f * k;
            // 이름 + 돋보기(고학년 · 패시브 펼치기)
            T("name", R, h.Name, new Vector3(x + 0.05f, y - 0.2f * k, 0), Tone.Xl * k, Tone.Ink);
            var zoom = Make.Box("zoom", R, Res.UI("circle"), new Vector3(x + w - 0.22f * k, y - 0.2f * k, 0), new Vector2(0.36f, 0.36f) * k, OC + 1, opened.Contains(idx) ? Tone.Gold : Color.white);
            var zi = Bolzena.RunUI.Theme.S("ic_zoom");
            if (zi != null) Make.Box("zi", R, zi, zoom.transform.localPosition, new Vector2(0.22f, 0.22f) * k, OC + 2, Tone.Night);
            var zb = zoom.gameObject.AddComponent<Button>();
            zb.Size = Vector2.one * 0.5f * k / zoom.transform.localScale.x;
            // 돋보기 — 그 사도의 정보 창(고학년 → 고유 효과 → 패시브, 수치가 다 든 글 · 「자세히」 없음)
            zb.OnClick = () => { var par = m.transform.parent; var snap = s; InfoPanel.Hero(par, snap, idx, default); };
            y -= 0.44f * k;
            Make.Box("nl", R, Res.UI("white"), new Vector3(x + w / 2, y, 0), new Vector2(w, 0.012f), OC, new Color(1, 1, 1, 0.25f));
            y -= 0.08f * k;
            // 능력치 표 — 줄 교대 띠
            (string n, string v)[] stats =
            {
                ("공격력", Val(h.Atk, h.AtkNow)), ("방어력", Val(h.Def, h.DefNow)), ("치명", Val(h.Crit, h.CritNow, "%")),
            };
            for (int i = 0; i < stats.Length; i++)
            {
                float rh = 0.36f * k;
                if (i % 2 == 1) Make.Sliced("band", R, Res.UI("white"), new Vector3(x + w / 2, y - rh / 2, 0), new Vector2(w, rh), OC, new Color(1, 1, 1, 0.06f));
                T("sn", R, stats[i].n, new Vector3(x + 0.1f, y - rh / 2, 0), Tone.Body * k, Tone.Sub, TextAlignmentOptions.Left, true);
                T("sv", R, stats[i].v, new Vector3(x + w - 0.1f, y - rh / 2, 0), Tone.Md * k, Tone.Ink, TextAlignmentOptions.Right);
                y -= rh;
            }
            y -= 0.14f * k;
            // 고유 효과 · 고학년
            Band(R, x, w, ref y, k, "고유 효과");
            if (!string.IsNullOrEmpty(h.KeywordName))
            {
                T("kn", R, $"「{h.KeywordName}」", new Vector3(x + 0.1f, y - 0.18f * k, 0), Tone.Body * k, Tone.Gold);
                T("kv", R, h.KeywordStacks.ToString(), new Vector3(x + w - 0.1f, y - 0.18f * k, 0), Tone.Md * k, Tone.Ink, TextAlignmentOptions.Right);
            }
            else T("kn", R, "없음", new Vector3(x + 0.1f, y - 0.18f * k, 0), Tone.Body * k, Tone.Dim, TextAlignmentOptions.Left, true);
            y -= 0.42f * k;
            float floor = -Tone.HalfH + 0.25f + sz0(w, k) + 0.05f + 0.4f * k + 0.1f;
            if (opened.Contains(idx))
            {
                Band(R, x, w, ref y, k, "고학년 · " + h.UltName);
                var u = T("ut", R, h.UltText, new Vector3(x + 0.1f, y - 0.04f, 0), Tone.Sm * k, Tone.Ink, TextAlignmentOptions.TopLeft, true, w - 0.2f);
                u.textWrappingMode = TextWrappingModes.Normal; u.rectTransform.sizeDelta = new Vector2(w - 0.2f, 2); u.ForceMeshUpdate();
                y -= u.preferredHeight + 0.12f;
                // 고유 효과 · 패시브 글 전부는 돋보기 → 사도 정보 창(InfoPanel.Hero — core CardText.Traits, 길면 판 안에서 내린다). 여기서 줄을 자르지 않는다
                if (y - 0.3f * k >= floor)
                {
                    var pt = T("pt", R, "돋보기 — 고유 효과 · 패시브 글 전부(사도 정보 창)", new Vector3(x + 0.1f, y - 0.02f, 0), Tone.Cap * k, Tone.Dim, TextAlignmentOptions.TopLeft, true, w - 0.2f);
                    pt.textWrappingMode = TextWrappingModes.Normal; pt.rectTransform.sizeDelta = new Vector2(w - 0.2f, 2); pt.ForceMeshUpdate();
                    y -= pt.preferredHeight + 0.06f;
                }
                y -= 0.08f;
            }
            // 장비 — 칸 셋(무기 · 방어구 · 장신구), 판 바닥에 붙인다
            float sz = Mathf.Min(0.95f * k, (w - 0.3f) / 3);
            y = -Tone.HalfH + 0.25f + sz + 0.05f + 0.4f * k;
            Band(R, x, w, ref y, k, "장비");
            for (int i = 0; i < 3; i++)
            {
                var g = i < h.Gear.Count ? h.Gear[i] : new GearSlot { Slot = i == 0 ? "무기" : i == 1 ? "방어구" : "장신구" };
                var at = new Vector3(x + w / 2 + (i - 1) * (sz + 0.12f), y - sz / 2 - 0.05f, 0);
                bool has = g.Id != null;
                var gc = has ? Bolzena.RunUI.Theme.GradeOf(g.Grade) : new Color(1, 1, 1, 0.2f);
                Make.Sliced("gs", R, Res.UI(has ? "cell_on_9s" : "cell_9s"), at, new Vector2(sz, sz), OC, has ? gc : new Color(1, 1, 1, 0.8f));
                var si = Bolzena.RunUI.Theme.S(g.Slot == "무기" ? "ic_sword" : g.Slot == "방어구" ? "ic_shield" : "ic_ring_slot");
                if (si != null) Make.Box("gi", R, si, at, new Vector2(sz * 0.5f, sz * 0.5f), OC + 1, has ? gc : new Color(0.6f, 0.62f, 0.7f, 0.6f));
                if (has)
                {
                    var gt = T("gn", R, g.Name, at + new Vector3(0, -sz / 2 + 0.12f, 0), 0.11f * k, Color.white, TextAlignmentOptions.Center, true, sz);
                    gt.overflowMode = TextOverflowModes.Ellipsis; gt.rectTransform.sizeDelta = new Vector2(sz - 0.06f, 0.2f);
                    var z = Make.Node("gz", R, at);
                    var name = g.Name; var txt = g.Text; var grade = g.Grade;
                    TipZone.Add(z.GetComponent<Transform>(), new Vector2(sz, sz), () => Tip.Head(name) + Tip.Dim($"  {grade}") + (string.IsNullOrEmpty(txt) ? "" : "\n" + txt), 120);
                }
            }
        }

        static float sz0(float w, float k) => Mathf.Min(0.95f * k, (w - 0.3f) / 3);

        // 얇은 테(네 변) — 판을 칠하지 않고 고른 표시만
        static void Frame(Transform R, Vector3 c, Vector2 size, Color col, float th, int o)
        {
            Make.Box("ft", R, Res.UI("white"), c + new Vector3(0, size.y / 2, 0), new Vector2(size.x, th), o, col);
            Make.Box("fb", R, Res.UI("white"), c - new Vector3(0, size.y / 2, 0), new Vector2(size.x, th), o, col);
            Make.Box("fl", R, Res.UI("white"), c - new Vector3(size.x / 2, 0, 0), new Vector2(th, size.y), o, col);
            Make.Box("fr", R, Res.UI("white"), c + new Vector3(size.x / 2, 0, 0), new Vector2(th, size.y), o, col);
        }

        static string Val(int b, int now, string unit = "") => now == b ? b + unit : $"{b}{unit} <color={(now > b ? Tone.GoodTag : Tone.BadTag)}>→ {now}{unit}</color>";

        void Band(Transform R, float x, float w, ref float y, float k, string text)
        {
            float bh = 0.34f * k;
            Make.Sliced("band", R, Res.UI("bar_fill_9s"), new Vector3(x + w / 2, y - bh / 2, 0), new Vector2(w, bh), OC, new Color(1, 1, 1, 0.1f));
            T("bt", R, text, new Vector3(x + 0.1f, y - bh / 2, 0), Tone.Body * k, Tone.Ink);
            y -= bh + 0.06f * k;
        }

        // ── ③ 카드 ──
        void Deck(Transform R, float l, float r, float y, float k)
        {
            var list = new List<CardInfo>();
            list.AddRange(s.Hand); list.AddRange(s.DrawPile); list.AddRange(s.DiscardPile); list.AddRange(s.GonePile);

            T("dn", R, $"이 전투의 덱 {list.Count}장  ·  사도별 기본 → 고유, 맨 끝 교주 · 상태  ·  누르면 크게", new Vector3(l, y - 0.1f, 0), Tone.Sm * k, Tone.Sub, TextAlignmentOptions.Left, true);
            y -= 0.4f * k;
            cards = Make.Node("cards", R);
            float gap = 0.3f * k;
            float cw = Mathf.Min(Mathf.Clamp(Tone.HalfW * 2 * 0.11f, 1.7f, 2.4f) * Mathf.Max(1f, k * 0.92f), (r - l - gap * 4) / 5f);
            float ch = cw * CardView.H / CardView.W;
            int cols = 5;
            float x0 = (l + r) / 2 - (cols * cw + (cols - 1) * gap) / 2;
            heads.Clear();
            float bottom = PileUi.Grid(cards, PileUi.Grouped(list, s), s, x0, cw, gap, y, cols, OC, k, made, heads);
            scrollMax = Mathf.Max(0, -Tone.HalfH + 0.3f - bottom);
            top = y + 0.2f;
            Clip();
        }

        /// <summary>카드 목록을 맨 끝까지 내린다(데모 캡처 — 맨 끝 교주 카드 묶음).</summary>
        public void ScrollToEnd()
        {
            if (scrollMax <= 0 || cards == null) return;
            scroll = scrollMax;
            cards.localPosition = new Vector3(0, scroll, 0);
            foreach (var c in made) if (c) c.TargetPos = c.transform.localPosition;
            Clip();
        }

        void Clip()
        {
            PileUi.ClipHeads(heads, top);
            foreach (var c in made)
            {
                if (c == null) continue;
                float ct = c.transform.position.y + CardView.H * c.transform.lossyScale.y / 2;
                float a = Mathf.Clamp01(1 - (ct - top) / 0.6f);
                c.SetAlpha(a);
                c.gameObject.SetActive(a > 0.01f);
            }
        }

        void Update()
        {
            if (m == null || tab != 2) return;
            if (PointerInput.Tap)
            {
                if (CardZoom.Shown) CardZoom.Hide();
                else
                {
                    var p = PointerInput.Pos;
                    foreach (var c in made)
                    {
                        if (c == null || !c.gameObject.activeSelf) continue;
                        var l = c.transform.InverseTransformPoint(p);
                        if (Mathf.Abs(l.x) < CardView.W / 2 && Mathf.Abs(l.y) < CardView.H / 2)
                        {
                            CardZoom.Show(m.transform.parent, c.Info, new Vector3(-1.6f * Tone.K, 0, 0), Mathf.Min(2.0f, Tone.HalfH * 2 * 0.62f / CardView.H));
                            Emit?.Invoke("party_card_zoom");
                            break;
                        }
                    }
                }
            }
            if (scrollMax <= 0 || cards == null) return;
            float d = 0;
            if (!PointerInput.Simulated && Mouse.current != null) d = -Mouse.current.scroll.ReadValue().y * 0.01f;
            var pp = PointerInput.Pos;
            if (PointerInput.Down) { dragging = true; dragY = pp.y; }
            if (!PointerInput.Held) dragging = false;
            if (dragging && PointerInput.Moved) { d += pp.y - dragY; dragY = pp.y; }
            if (Mathf.Abs(d) < 1e-4f) return;
            scroll = Mathf.Clamp(scroll + d, 0, scrollMax);
            cards.localPosition = new Vector3(0, scroll, 0);
            foreach (var c in made) if (c) c.TargetPos = c.transform.localPosition;
            Clip();
        }
    }
}
