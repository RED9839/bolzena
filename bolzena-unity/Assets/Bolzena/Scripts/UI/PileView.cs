using System.Collections.Generic;
using System.Text;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bolzena.UI
{
    // 더미 보기 — 화면 전체를 어둠으로 덮고(배치는 카제나 더미 창을 따른다 · 그림 · 아이콘 · 글은 우리 것):
    //   왼쪽 세로 탭 줄(뽑을 · 무덤 · 소멸 — 아이콘만, 고른 탭은 빛 + 세로선 위 점) │ 위 왼쪽 제목 · 오른쪽 위 큰 ×
    //   큰 그림 카드를 가로로 한 줄씩(넘치면 다음 줄 · 휠 / 끌어서 스크롤). 무덤 탭은 「버린 더미」 머리 띠 아래 카드.
    //   뽑을 더미는 차례를 숨기려고 코스트 · 이름 차례로 늘어놓는다.
    public static class PileView
    {
        static readonly string[] Titles = { "뽑을 더미", "무덤", "소멸" };
        static readonly string[] Icons = { "ic_pile_up", "ic_pile_down", "ic_skull" };
        static readonly string[] Bands = { null, "버린 더미", "사라진 카드" };
        static readonly string[] Why = { "차례는 감춥니다 — 사도별 기본 → 고유 차례", "덱이 바닥나면 섞여서 뽑을 더미로 돌아갑니다", "소멸했거나 손이 넘쳐 빠진 카드 — 이 전투에서 다시 나오지 않습니다" };

        public static Modal Show(Transform parent, BattleSnapshot s, int tab)
        {
            var m = Modal.Create(parent, "pile", new Vector2(60, 30), Vector3.zero, false, 0.965f);
            m.Panel.enabled = false;
            var ui = m.gameObject.AddComponent<PileUi>();
            ui.Init(m, s, tab);
            return m;
        }

        public static string CardTip(CardInfo c)
        {
            var sb = new StringBuilder();
            sb.Append(Tip.Head(c.Name)).Append("  ").Append(Tip.Dim($"{c.HeroName} · {c.TypeName} · 비용 {c.Cost}"));
            sb.Append('\n').Append(c.Text);
            if (c.Terms.Count > 0) sb.Append("\n\n").Append(Tip.Terms(c.Terms));
            return sb.ToString();
        }

        public static string Title(int tab) => Titles[tab];
        public static string Icon(int tab) => Icons[tab];
        public static string Band(int tab) => Bands[tab];
        public static string Note(int tab) => Why[tab];
    }

    public class PileUi : MonoBehaviour
    {
        Modal m;
        BattleSnapshot snap;
        int tab;
        Transform cards;
        readonly List<CardView> made = new List<CardView>();
        readonly List<Transform> heads = new List<Transform>();
        float scroll, scrollMax, top, dragY, thumbH, thumbTop, thumbRange;
        SpriteRenderer thumb;
        bool dragging;
        const int OC = Modal.O + 5;

        public void Init(Modal modal, BattleSnapshot s, int t)
        {
            m = modal; snap = s;
            Draw(t);
        }

        List<CardInfo> Pile(int t)
        {
            var l = new List<CardInfo>(t == 0 ? snap.DrawPile : t == 1 ? snap.DiscardPile : snap.GonePile);
            Order(l);
            return l;
        }

        // 카드 목록 공통 차례(사용자 규칙) — 사도별(편성 차례) 기본 카드 → 고유 카드, 맨 끝에 교주 카드 · 상태. 같은 칸 안은 코스트 · 이름.
        // 판 화면에 공용 정렬(CardOrder 류)이 생기면 그것으로 바꾼다
        // 카드 목록 공통 차례 — 판 화면 공용 정렬(Bolzena.RunUI.CardOrder): 사도별(편성 차례) 기본 → 고유 → 상태 · 저주 → 맨 끝 교주 카드.
        // 묶음마다 머리표(초상 + 이름, 교주는 왕관 · 상태는 해골)를 붙여 줄을 새로 시작한다.
        public static List<(Bolzena.RunUI.CardOrder.Group g, List<CardInfo> cards)> Grouped(List<CardInfo> l, BattleSnapshot s)
        {
            var res = new List<(Bolzena.RunUI.CardOrder.Group, List<CardInfo>)>();
            var data = Look.Data;
            if (data == null) { res.Add((null, l)); return res; }
            var party = s.Heroes.ConvertAll(h => h.Id);
            var pool = new Dictionary<string, Queue<CardInfo>>();
            foreach (var c in l) { if (!pool.TryGetValue(c.Id, out var q)) pool[c.Id] = q = new Queue<CardInfo>(); q.Enqueue(c); }
            foreach (var g in Bolzena.RunUI.CardOrder.Groups(l.ConvertAll(c => c.Id), data, party))
            {
                var cs = new List<CardInfo>();
                foreach (var id in g.Ids) if (pool.TryGetValue(id, out var q) && q.Count > 0) cs.Add(q.Dequeue());
                if (cs.Count > 0) res.Add((g, cs));
            }
            return res;
        }

        /// <summary>묶음 격자 — 머리표 + 줄마다 cols 장. 돌려주는 값은 맨 아래 y. 머리표는 heads 에 모은다(스크롤 때 자르기).</summary>
        public static float Grid(Transform parent, List<(Bolzena.RunUI.CardOrder.Group g, List<CardInfo> cards)> groups, BattleSnapshot s,
                                 float x0, float cw, float gap, float y, int cols, int order, float k, List<CardView> made, List<Transform> heads)
        {
            float ch = cw * CardView.H / CardView.W;
            int n = 0;
            foreach (var (g, cards) in groups)
            {
                if (g != null)
                {
                    var h = Make.Node("head", parent, new Vector3(x0, y - 0.22f * k, 0));
                    heads.Add(h);
                    string name = g.Title; string key = null;
                    if (g.Kind == Bolzena.RunUI.CardOrder.Kind.Hero)
                    {
                        var hs = s.Heroes.Find(x => x.Id == g.Hero);
                        key = hs != null ? hs.Key : Look.Hero(g.Hero).Art;
                        name = hs != null ? hs.Name : Look.Data?.Hero(g.Hero)?.Name ?? g.Hero;
                    }
                    float d = 0.36f * k;
                    if (key != null)
                    {
                        Make.Box("fr", h, Res.UI("circle"), new Vector3(d / 2, 0, 0), new Vector2(d + 0.04f, d + 0.04f), order, new Color(1, 1, 1, 0.5f));
                        Tone.RoundFace("f", h, key, new Vector3(d / 2, 0, 0), d, order + 2);
                    }
                    else
                    {
                        var ic = Bolzena.RunUI.Theme.S(g.Kind == Bolzena.RunUI.CardOrder.Kind.Leader ? "ic_crown" : "ic_skull");
                        if (ic != null) Make.Box("ic", h, ic, new Vector3(d / 2, 0, 0), new Vector2(d, d), order + 1, g.Kind == Bolzena.RunUI.CardOrder.Kind.Leader ? Tone.Gold : Tone.Sub);
                    }
                    Tone.Text("t", h, $"{name}  <size=75%><color={Tone.SubTag}>{cards.Count}장</color></size>", new Vector3(d + 0.14f, -0.01f, 0), Tone.Md * k, order + 1, Tone.Ink, TMPro.TextAlignmentOptions.Left);
                    Make.Box("ln", h, Res.UI("white"), new Vector3((cols * (cw + gap) - gap) / 2, -0.22f * k, 0), new Vector2(cols * (cw + gap) - gap, 0.012f), order, new Color(1, 1, 1, 0.14f));
                    y -= 0.52f * k;
                }
                for (int i = 0; i < cards.Count; i++)
                {
                    int r = i / cols, c = i % cols;
                    var cv = CardView.Create(parent, cards[i]);
                    cv.TargetPos = new Vector3(x0 + cw / 2 + c * (cw + gap), y - ch / 2 - r * (ch + gap), 0);
                    cv.TargetScale = cw / CardView.W; cv.TargetRot = 0; cv.Snap();
                    cv.SetOrder(order + 10 + n * 12);
                    made.Add(cv);
                    n++;
                }
                y -= Mathf.CeilToInt(cards.Count / (float)cols) * (ch + gap) + 0.06f * k;
            }
            return y;
        }

        public static void ClipHeads(List<Transform> heads, float top)
        {
            foreach (var h in heads) if (h) h.gameObject.SetActive(h.position.y < top - 0.1f);
        }

        public static void Order(List<CardInfo> l)
        {
            int Rank(CardInfo c) => c.Hero >= 0 ? c.Hero * 2 + (c.Unique ? 1 : 0) : c.Type == CardType.Status || c.Unplayable ? 1001 : 1000;
            l.Sort((a, b) =>
            {
                int r = Rank(a).CompareTo(Rank(b));
                if (r != 0) return r;
                return a.Cost != b.Cost ? a.Cost.CompareTo(b.Cost) : string.CompareOrdinal(a.Name, b.Name);
            });
        }

        void Draw(int t)
        {
            tab = t;
            scroll = 0;
            made.Clear();
            var T = m.Content;
            for (int i = T.childCount - 1; i >= 0; i--) Destroy(T.GetChild(i).gameObject);
            T.localPosition = Vector3.zero;
            T.localScale = Vector3.one;
            float hw = Tone.HalfW, hh = Tone.HalfH, k = Tone.K;
            // 탭 줄 — 왼쪽 세로, 오른쪽에 얇은 세로선
            float railX = -hw + 0.62f * k, lineX = -hw + 1.12f * k;
            Make.Box("rail", T, Res.UI("white"), new Vector3(lineX, 0, 0), new Vector2(0.012f, hh * 2), OC, new Color(1, 1, 1, 0.16f));
            for (int i = 0; i < 3; i++)
            {
                int ii = i;
                var at = new Vector3(railX, hh - (0.62f + i * 0.95f) * k, 0);
                bool on = i == t;
                if (on) Make.Box("tglow", T, Res.UI("soft"), at, new Vector2(1.2f, 1.2f) * k, OC, new Color(1f, 0.78f, 0.38f, 0.45f), Res.SpriteMat(true, 1.3f));
                var ic = Make.Box("tab" + i, T, Res.UI(PileView.Icon(i)), at, new Vector2(0.5f, 0.5f) * k, OC + 1, on ? Tone.Gold : new Color(0.62f, 0.66f, 0.78f, 0.8f));
                // 세로선 위 점 — 고른 탭(빛 고리) · 나머지(작은 점)
                Make.Box("tdot" + i, T, Res.UI("circle"), new Vector3(lineX, at.y, 0), Vector2.one * (on ? 0.16f : 0.06f) * k, OC + 2, on ? Tone.Gold : new Color(1, 1, 1, 0.35f));
                if (on) Make.Box("tdotr" + i, T, Res.UI("circle"), new Vector3(lineX, at.y, 0), Vector2.one * 0.28f * k, OC + 1, new Color(1f, 0.85f, 0.5f, 0.35f));
                var n = Pile(i).Count;
                Tone.Text("tn" + i, T, n.ToString(), at + new Vector3(0.26f, -0.24f, 0) * k, 0.13f * k, OC + 2, on ? Tone.Gold : Tone.Sub, TextAlignmentOptions.Center, false, 1f, 0.3f);
                var b = ic.gameObject.AddComponent<Button>();
                b.Size = Vector2.one * 0.9f * k / ic.transform.localScale.x;
                b.OnClick = () => { if (ii != tab) Draw(ii); };
                TipZone.Add(ic, b.Size, () => PileView.Title(ii) + $"  {Pile(ii).Count}장" + (ii == 0 ? Tip.Dim("  · A") : ii == 1 ? Tip.Dim("  · S") : ""), 120);
            }
            // 제목 · 닫기 · 머리 선
            float x0 = lineX + 0.42f * k;
            var title = Tone.Text("title", T, PileView.Title(t), new Vector3(x0, hh - 0.5f * k, 0), Tone.Xl * k, OC, Tone.Ink, TextAlignmentOptions.Left, false);
            title.ForceMeshUpdate();
            Tone.Text("cnt", T, $"{Pile(t).Count}장  ·  {PileView.Note(t)}", new Vector3(x0 + title.preferredWidth + 0.24f * k, hh - 0.52f * k, 0), Tone.Sm * k, OC, Tone.Sub, TextAlignmentOptions.Left, true);
            var cx = Make.Box("close", T, Res.UI("ic_close"), new Vector3(hw - 0.62f * k, hh - 0.5f * k, 0), new Vector2(0.52f, 0.52f) * k, OC + 1, new Color(0.95f, 0.95f, 0.98f));
            var cb = cx.gameObject.AddComponent<Button>();
            cb.Size = Vector2.one * 0.9f * k / cx.transform.localScale.x;
            cb.OnClick = m.Close;
            top = hh - 1.0f * k;
            Make.Box("headline", T, Res.UI("white"), new Vector3((lineX + hw - 0.3f) / 2, top, 0), new Vector2(hw - 0.3f - lineX, 0.012f), OC, new Color(1, 1, 1, 0.16f));
            Tooltip.Ceiling = top - 0.1f;

            // 카드 — 폭은 화면 폭의 11%(글이 작아지지 않게), 줄마다 가로로
            cards = Make.Node("cards", T);
            float areaL = lineX + 0.6f * k, areaR = hw - 0.5f * k;
            // 한 줄 다섯 장 — 폭은 화면 폭의 11% 를 넘지 않게, 사이는 넉넉히. 줄은 가운데로
            float gap = 0.34f * k;
            // 넓은 화면은 한 줄 5~8장(카드 폭은 그대로 — 판 화면 덱 보기와 같은 규칙, 2026-10-06 사용자). 폰은 전처럼 다섯
            float cw0 = Mathf.Clamp(Tone.HalfW * 2 * 0.11f, 1.7f, 2.4f) * Mathf.Max(1f, k * 0.92f);
            int cols = Tone.Compact ? 5 : Mathf.Clamp(Mathf.FloorToInt((areaR - areaL + gap) / (cw0 + gap)), 5, 8);
            float cw = Mathf.Min(cw0, (areaR - areaL - gap * (cols - 1)) / cols);
            float ch = cw * CardView.H / CardView.W;
            float rowW = cols * cw + (cols - 1) * gap;
            areaL = (areaL + areaR) / 2 - rowW / 2;
            float y = top - 0.3f * k;
            var list = Pile(t);
            if (PileView.Band(t) != null)
            {
                float bw = Mathf.Min(areaR - areaL, cols * (cw + gap) - gap);
                Make.Sliced("band", cards, Res.UI("cell_9s"), new Vector3(areaL + bw / 2, y - 0.25f * k, 0), new Vector2(bw, 0.5f * k), OC, new Color(1, 1, 1, 0.75f));
                Tone.Text("bandt", cards, PileView.Band(t), new Vector3(areaL + 0.24f * k, y - 0.26f * k, 0), Tone.Lg * k, OC + 1, Tone.Ink, TextAlignmentOptions.Left, false);
                y -= 0.5f * k + 0.2f * k;
            }
            if (list.Count == 0)
            {
                Tone.Text("none", cards, "비어 있습니다", new Vector3((areaL + areaR) / 2, y - 1.4f, 0), Tone.Lg * k, OC, Tone.Dim);
                scrollMax = 0;
                return;
            }
            heads.Clear();
            float bottom = Grid(cards, Grouped(list, snap), snap, areaL, cw, gap, y, cols, OC, k, made, heads);
            scrollMax = Mathf.Max(0, -hh + 0.3f - bottom);
            // 오른쪽 세로 스크롤 막대(넘칠 때만)
            if (scrollMax > 0)
            {
                float trackTop = top - 0.2f, trackBot = -hh + 0.3f, trackH = trackTop - trackBot;
                float sx = hw - 0.3f * k;
                Make.Box("track", T, Res.UI("white"), new Vector3(sx, (trackTop + trackBot) / 2, 0), new Vector2(0.03f, trackH), OC, new Color(1, 1, 1, 0.1f));
                thumbH = Mathf.Max(0.4f, trackH * trackH / (trackH + scrollMax));
                thumb = Make.Sliced("thumb", T, Res.UI("bar_fill_9s"), new Vector3(sx, trackTop - thumbH / 2, 0), new Vector2(0.06f, thumbH), OC + 1, new Color(1, 1, 1, 0.75f));
                thumbTop = trackTop; thumbRange = trackH - thumbH;
            }
            else thumb = null;
            Clip();
        }

        public static bool Open;
        void OnEnable() => Open = true;
        void OnDestroy() { Open = false; CardZoom.Hide(); }

        // 누른 카드 — 크게 띄우고 키워드 판(다시 누르면 · 다른 곳을 누르면 닫는다)
        void Taps()
        {
            if (!PointerInput.Tap) return;
            if (CardZoom.Shown) { CardZoom.Hide(); return; }
            var p = PointerInput.Pos;
            CardView hit = null;
            foreach (var c in made)
            {
                if (c == null || !c.gameObject.activeSelf) continue;
                var l = c.transform.InverseTransformPoint(p);
                if (Mathf.Abs(l.x) < CardView.W / 2 && Mathf.Abs(l.y) < CardView.H / 2) { hit = c; break; }
            }
            if (hit == null) return;
            float s = Mathf.Min(2.0f, (Tone.HalfH * 2 * 0.62f) / CardView.H);
            CardZoom.Show(m.transform.parent, hit.Info, new Vector3(-1.6f * Tone.K, 0, 0), s);
            Emit?.Invoke("pile_zoom");
        }
        public static System.Action<string> Emit;

        void Update()
        {
            if (m == null) return;
            Taps();
            if (scrollMax <= 0) return;
            float d = 0;
            if (!PointerInput.Simulated && Mouse.current != null) d = -Mouse.current.scroll.ReadValue().y * 0.01f;
            var p = PointerInput.Pos;
            if (PointerInput.Down) { dragging = true; dragY = p.y; }
            if (!PointerInput.Held) dragging = false;
            if (dragging && PointerInput.Moved) { d += p.y - dragY; dragY = p.y; }
            if (Mathf.Abs(d) < 1e-4f) return;
            scroll = Mathf.Clamp(scroll + d, 0, scrollMax);
            cards.localPosition = new Vector3(0, scroll, 0);
            foreach (var c in made) if (c) { c.TargetPos = c.transform.localPosition; }
            if (thumb != null) thumb.transform.localPosition = new Vector3(thumb.transform.localPosition.x, thumbTop - thumbH / 2 - thumbRange * scroll / scrollMax, 0);
            Clip();
        }

        // 머리 선 위로 올라간 줄은 흐리게 사라진다
        void Clip()
        {
            ClipHeads(heads, top);
            foreach (var c in made)
            {
                if (c == null) continue;
                float ct = c.transform.position.y + CardView.H * c.transform.lossyScale.y / 2;
                float a = Mathf.Clamp01(1 - (ct - top) / 0.6f);
                c.SetAlpha(a);
                c.gameObject.SetActive(a > 0.01f);

            }
        }
    }

}
