using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 마을 고르기(2026-10-21 사용자 — 모험 시작 때, 우주 지도에 행성 노드를 흩어 놓은 화면처럼): 모험 시작 → (단계 고르기) → 마을 고르기 → 마을 공개 → 편성.
    //   어두운 하늘 배경에 둥근 마을 노드 6 + 「무작위」 를 지그재그로 흩어 놓는다. 노드 = 그 마을 1층 전투 배경을 원으로 잘라 + 테두리 고리,
    //   옆에 종족 아이콘 · 이름 · 띠 둘(이 마을 기록 · 두 층 땅). 누르면 오른쪽(폰은 아래) 상세 판:
    //   마을 이름 · 한 줄 · 대표 아티팩트 2(GameData.VillageShowcase — 누르면 효과 판) · 나오는 엘리트 얼굴 · 보스(종족 사도 클론 — 모험마다 바뀐다) · 「이 마을로 출발」.
    //   「무작위」 = 물음표 문양(미확인 노드 모양) — 고르지 않으면 지금처럼 씨앗으로 하나(go(null)).
    //   처음부터 6곳 다 열림. hidden 에 든 마을은 「미확인」 노드(어두운 문양 · 「? 미확인 마을」, 누를 수 없음) — 나중 잠김 연출 자리(지금은 데모만 쓴다).
    public partial class Flow
    {
        /// <summary>마을 고르기 배경 — 원작 배경 가운데 가장 어두운 밤하늘(푸른 우주빛).</summary>
        const string VilPickBg = "stage19_1";
        /// <summary>마을 노드 차례 — 이 차례가 먼저, 데이터에 더 있으면 뒤에(id 차례).</summary>
        static readonly string[] VilOrder = { "erpien", "monatium", "furry", "spirit", "ghost", "dragon" };

        /// <summary>마을 고르기 — go(마을 id · 「무작위」면 null), back = 머리 뒤로.</summary>
        public void VillagePick(Action<string> go, Action back, ICollection<string> hidden = null)
        {
            var vils = P.Villages.OrderBy(v => { int i = Array.IndexOf(VilOrder, v.Id); return i < 0 ? 99 : i; }).ThenBy(v => v.Id, StringComparer.Ordinal).ToList();
            string pick = null;   // 고른 노드 — null = 「무작위」(처음 고른 것)
            Stage.SetBg(VilPickBg, 0.12f);
            Stage.Show("vilpick", root =>
            {
                // 마을만의 아티팩트 묶음이 콘텐츠에 없으면(지금 공개판) 풀이에서 「아티팩트」 를 뺀다 — 상세 판은 「모든 마을의 아티팩트가 나온다」 로 안내
                bool vilGear = vils.Any(v => P.Data.VillageShowcase(v.Id).Count > 0);
                ScreenHead(   // 설정은 머리 오른쪽 위 톱니
root, "마을 고르기", vilGear ? "마을마다 적 · 보스 · 아티팩트가 다르다" : "마을마다 적 · 보스가 다르다", back);
                bool phone = Theme.Compact;
                var size = Stage.Size;
                float g = Theme.Gutter;
                // 상세 판 — PC 는 오른쪽 기둥, 폰은 아래 띠
                float dw = phone ? size.x - g * 2 : Mathf.Min(480, size.x * 0.32f);
                float dh = phone ? 236 : size.y - FTop - g;
                var detail = FPanel(root, "detail");
                if (phone) detail.At(0.5f, 0, 0, g, dw, dh);
                else detail.At(1, 1, -g, -FTop, dw, dh);
                Tw.Rise(detail, 0.12f, 18, 0.4f, phone ? Vector2.down : Vector2.right);
                // 노드 지도 — 상세 판을 뺀 나머지
                float mw = phone ? size.x - g * 2 : size.x - g * 2 - dw - FGap, mh = phone ? size.y - FTop - g - dh - FGap : size.y - FTop - g;
                var map = Ui.Rect("map", root).At(0, 1, g, -FTop, mw, mh);

                float d = phone ? Mathf.Clamp(mh * 0.3f, 76, 100) : Mathf.Clamp(mh * 0.16f, 96, 126);   // 노드 지름
                float lw = phone ? 172 : 196;                                                       // 이름 · 띠 너비
                float bw = d + 12 + lw;
                var spots = NodeSpots(vils.Count + 1, new Vector2(mw, mh), bw, phone);
                // 「무작위」 = 지도 가운데에 가장 가까운 자리, 마을은 나머지 자리를 차례로
                int mid = 0;
                for (int i = 1; i < spots.Count; i++) if ((spots[i] - new Vector2(mw, mh) / 2).sqrMagnitude < (spots[mid] - new Vector2(mw, mh) / 2).sqrMagnitude) mid = i;
                var nodes = new List<(string id, RectTransform rt, Image rim, Image glow, TextMeshProUGUI name)>();

                void Select(string id)
                {
                    pick = id;
                    foreach (var n in nodes)
                    {
                        bool on = n.id == pick;
                        if (n.rim) n.rim.enabled = on;
                        if (n.name) n.name.color = on ? Theme.Gold : Theme.Ink;
                        if (n.glow) n.glow.color = n.glow.color.A(on ? 0.55f : 0.18f);
                    }
                    Ui.Clear(detail);
                    var goBtn = VilDetail(detail, pick != null ? P.Village(pick) : null, phone, vils, () => { Hitch.Mark("마을 고르기 " + (pick ?? "무작위")); go(pick); });
                    Stage.Hot["vilpick.go"] = goBtn;
                    Stage.Hot["go"] = goBtn;
                }

                int vi = 0;
                for (int i = 0; i < spots.Count; i++)
                {
                    bool rnd = i == mid;
                    var v = rnd ? null : vils[vi++];
                    bool hid = v != null && hidden != null && hidden.Contains(v.Id);
                    var n = VilNode(map, v, rnd, hid, d, lw, spots[i]);
                    string id = v?.Id;
                    if (!hid) n.btn.OnClick = () => Select(id);
                    else { n.btn.Interactable = false; n.btn.Why = "아직 알 수 없는 마을이다"; }
                    Stage.Hot[rnd ? "vilpick.random" : "vilpick." + v.Id] = n.btn;
                    nodes.Add((id, n.btn.GetComponent<RectTransform>(), n.rim, n.glow, n.name));
                    Tw.Pop(n.btn.GetComponent<RectTransform>(), 0.08f + i * 0.06f, 0.7f, 0.42f);
                }
                Select(null);
                StartCoroutine(VilDrift(nodes.Select(x => x.rt).ToList()));
            }, 1.0f);
        }

        /// <summary>
        /// 노드 자리(지도 왼쪽 위에서 블록 가운데까지, 아래로 +) — 지그재그. PC 는 3줄(가운데 줄이 하나 더 · 2-3-2), 폰은 2줄(4-3).
        /// 블록(노드 + 이름) 너비 bw 가 줄 안에서 겹치지 않게 펼친다. 줄이 짧으면 반 칸 밀어 엇갈린다.
        /// </summary>
        static List<Vector2> NodeSpots(int n, Vector2 area, float bw, bool phone)
        {
            var rows = new List<int>();
            if (phone) { int top = (n + 1) / 2; rows.Add(top); rows.Add(n - top); }
            else { int midRow = (n + 2) / 3, rest = n - midRow, top = rest / 2; rows.Add(top); rows.Add(midRow); rows.Add(rest - top); }
            rows.RemoveAll(k => k <= 0);
            int maxK = rows.Max();
            float free = Mathf.Max(0, area.x - bw);
            var o = new List<Vector2>();
            for (int r = 0; r < rows.Count; r++)
            {
                int k = rows[r];
                float y = area.y * (r + 0.5f) / rows.Count;
                for (int j = 0; j < k; j++)
                {
                    float fx = maxK <= 1 ? 0.5f : (j + (maxK - k) / 2f) / (maxK - 1);
                    o.Add(new Vector2(bw / 2 + free * fx, y));
                }
            }
            return o;
        }

        /// <summary>
        /// 노드 하나 — 둥근 그림(마을 1층 전투 배경 · 무작위 · 미확인은 물음표 문양) + 고리 + 옆 이름 · 띠 둘. 블록 전체가 단추(Btn — 올리면 커지고 밝아진다).
        /// center = 블록 가운데(지도 왼쪽 위 기준).
        /// </summary>
        (Btn btn, Image rim, Image glow, TextMeshProUGUI name) VilNode(RectTransform map, VillageDef v, bool rnd, bool hidden, float d, float lw, Vector2 center)
        {
            float bw = d + 12 + lw, bh = Mathf.Max(d, 96);
            var hit = Ui.Img(map, Theme.White, new Color(0, 0, 0, 0), rnd ? "node random" : "node " + v.Id, true);
            var rt = hit.rectTransform;
            rt.At(0, 1, center.x - bw / 2, -center.y + bh / 2, bw, bh);
            rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(center.x, -center.y);
            var b = hit.gameObject.AddComponent<Btn>();
            bool unknown = rnd || hidden;
            var accent = rnd ? Theme.Gold : hidden ? Theme.Dim : Theme.Edge;
            // 노드 둘레 빛 · 고른 노드 바깥 금빛 고리
            var glow = Ui.Img(rt, Theme.S("soft"), (rnd ? Theme.Gold : hidden ? new Color(0.45f, 0.4f, 0.75f) : Theme.Sky).A(0.18f), "glow");
            glow.rectTransform.At(0, 0.5f, -d * 0.35f, 0, d * 1.7f, d * 1.7f);
            var rim = Ui.Img(rt, Theme.S("ring"), Theme.Gold, "rim"); rim.rectTransform.At(0, 0.5f, -10, 0, d + 20, d + 20); rim.enabled = false;
            var disc = Ui.Img(rt, Theme.S("circle"), Theme.NavyWell, "disc"); disc.rectTransform.At(0, 0.5f, 0, 0, d, d);
            if (unknown) UnknownEmblem(disc.rectTransform, d, rnd);
            else
            {
                var clip = Ui.Img(disc.rectTransform, Theme.S("circle"), Color.white, "clip"); clip.rectTransform.Fill(3, 3, 3, 3);
                clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var sp = VilArt(v);
                if (sp != null)
                {
                    // 위쪽(하늘 · 풍경)이 보이게 1.5배로 키워 아래로 조금 민다
                    float ph = (d - 6) * 1.5f, pw = ph * sp.rect.width / sp.rect.height;
                    var pic = Ui.Img(clip.rectTransform, sp, Color.white, "pic"); pic.rectTransform.At(0.5f, 0.5f, 0, -(d - 6) * 0.2f, pw, ph);
                    b.Bg = pic; b.SetColor(Color.white);   // 올리면 그림이 밝아진다
                }
                var shade = Ui.Img(clip.rectTransform, Theme.S("vignette"), Color.white.A(0.55f), "shade"); shade.rectTransform.Fill();
            }
            var ring = Ui.Img(disc.rectTransform, Theme.S("ring"), accent.A(hidden ? 0.5f : 0.9f), "ring"); ring.rectTransform.Fill(-2, -2, -2, -2);
            if (b.Bg == null) { b.Bg = disc; b.SetColor(Theme.NavyWell); }

            // 이름 줄 — 종족 아이콘(무작위 · 미확인은 물음표) + 이름
            var lab = Ui.Rect("label", rt).Fill(d + 12, 0, 0, 0);
            float nameH = 34, bandH = Theme.C(26, 24);
            float top = (bh - (nameH + 6 + bandH * 2 + 4)) / 2;
            var ic = Ui.Img(lab, unknown ? Theme.S("ic_question") : Icon("종족_" + v.Race), unknown ? (rnd ? Theme.Gold : Theme.Dim) : Color.white, "race");
            ic.preserveAspect = true; ic.rectTransform.At(0, 1, 0, -top - 3, 28, 28);
            string nm = rnd ? "무작위" : hidden ? "미확인 마을" : v.Name;
            var t = Ui.Title(lab, nm, Theme.FsLg, hidden ? Theme.Dim : Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.At(0, 1, 34, -top, lw - 34, nameH); t.Outline(0.22f);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = Theme.FsLg;
            // 띠 둘 — 이 마을 기록 · 두 층 땅(무작위 = 6곳 가운데 하나 · 출발하면 정해진다)
            string b1, b2;
            if (rnd) { b1 = $"{P.Villages.Count}곳 가운데 하나"; b2 = "출발하면 정해진다"; }
            else if (hidden) { b1 = "알 수 없음"; b2 = "???"; }
            else { b1 = VilRecord(v.Id); b2 = string.Join(" · ", v.Floors.Select(f => f.Name)); }
            VilBand(lab, b1, top + nameH + 6, bandH, lw, hidden ? Theme.Dim : Theme.Gold);
            VilBand(lab, b2, top + nameH + 6 + bandH + 4, bandH, lw, hidden ? Theme.Dim : Theme.Sky);
            return (b, rim, glow, t);
        }

        /// <summary>노드 옆 띠 — 짙은 바탕 + 왼쪽 색 막대 + 작은 글(넘치면 줄인다).</summary>
        static void VilBand(RectTransform lab, string text, float y, float h, float w, Color accent)
        {
            var band = Ui.Img(lab, Theme.Round, Theme.NavyWell.A(0.82f), "band"); band.rectTransform.At(0, 1, 0, -y, w, h);
            var bar = Ui.Img(band.transform, Theme.Round, accent, "bar"); bar.rectTransform.At(0, 0.5f, 0, 0, 4, h - 6);
            var t = Ui.Text(band.transform, text, Theme.FsCap, Theme.Ink, TextAlignmentOptions.MidlineLeft); t.rectTransform.Fill(12, 0, 8, 0);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = Theme.FsCap;
        }

        /// <summary>
        /// 「미확인」 문양 — 어두운 원 안에 겹 고리 · 물음표 · 희미한 빛. 「무작위」(lit = 금빛)와 아직 모르는 마을(흐림)이 같은 모양을 쓴다.
        /// </summary>
        static void UnknownEmblem(RectTransform disc, float d, bool lit)
        {
            var c = lit ? Theme.Gold : Theme.Dim;
            var haze = Ui.Img(disc, Theme.S("soft"), (lit ? new Color(0.55f, 0.45f, 0.95f) : new Color(0.3f, 0.3f, 0.45f)).A(0.55f), "haze"); haze.rectTransform.Fill(d * 0.08f, d * 0.08f, d * 0.08f, d * 0.08f);
            var r1 = Ui.Img(disc, Theme.S("ring"), c.A(0.35f), "ring1"); r1.rectTransform.Fill(d * 0.12f, d * 0.12f, d * 0.12f, d * 0.12f);
            var r2 = Ui.Img(disc, Theme.S("ring"), c.A(0.2f), "ring2"); r2.rectTransform.Fill(d * 0.24f, d * 0.24f, d * 0.24f, d * 0.24f);
            var q = Ui.Img(disc, Theme.S("ic_question"), c.A(lit ? 0.95f : 0.7f), "q"); q.preserveAspect = true; q.rectTransform.Fill(d * 0.3f, d * 0.3f, d * 0.3f, d * 0.3f);
        }

        /// <summary>마을 그림 — 1층 전투 배경(콘텐츠 floors[0].bg.fight). 없으면 null.</summary>
        static Sprite VilArt(VillageDef v)
        {
            var f0 = v?.Floors.FirstOrDefault();
            return f0?.Bg != null && f0.Bg.TryGetValue("fight", out var bg) ? Theme.Bg(bg) : null;
        }

        /// <summary>이 마을 기록 한 줄 — 성장 켬: 「최고 단계 N」 · 끔: 「완주 n번」, 없으면 「기록 없음」(이 기기 기록 — PartyStore).</summary>
        static string VilRecord(string id)
        {
            if (ProgStore.On) { int t = PartyStore.VillageBest(id); return t > 0 ? $"최고 단계 {t}" : "기록 없음"; }
            int n = PartyStore.VillageClears(id);
            return n > 0 ? $"완주 {n}번" : "기록 없음";
        }

        /// <summary>노드가 천천히 떠다닌다(제자리에서 위아래 몇 픽셀 — 줄이기 설정이면 멈춤).</summary>
        IEnumerator VilDrift(List<RectTransform> nodes)
        {
            var base0 = nodes.Select(n => n.anchoredPosition).ToList();
            float t = 0;
            while (nodes.Count > 0 && nodes[0] != null)
            {
                t += Time.unscaledDeltaTime;
                if (!Settings.ReduceMotion)
                    for (int i = 0; i < nodes.Count; i++) if (nodes[i]) nodes[i].anchoredPosition = base0[i] + new Vector2(0, Mathf.Sin(t * 0.7f + i * 1.3f) * 4f);
                yield return null;
            }
        }

        // ── 상세 판 ──
        /// <summary>
        /// 상세 판을 채운다 — v 가 null 이면 「무작위」. PC 는 세로(그림 띠 · 이름 · 한 줄 · 기록 · 대표 아티팩트 · 엘리트 · 보스 · 출발),
        /// 폰은 가로 네 칸(이름 · 한 줄 | 아티팩트 | 엘리트 · 보스 | 출발). 돌려줌: 출발 단추.
        /// </summary>
        Btn VilDetail(RectTransform panel, VillageDef v, bool phone, List<VillageDef> all, Action go)
        {
            var size = panel.sizeDelta;
            float pad = phone ? 18 : FPad;
            string goLabel = v != null ? "이 마을로 출발" : "무작위로 출발";
            bool tiers = ProgStore.On;   // 성장 켬 — 상세 판 아래에 도전 사다리(난이도 단계) 줄(Flow.Prog.cs TierRow, 2026-10-22). 꺼짐이면 지금 판 그대로
            string boss = v != null ? $"{v.Race} 사도 클론 — 모험마다 바뀐다" : "마을 종족 사도 클론 — 모험마다 바뀐다";
            string line = v != null ? v.Line ?? "" : $"어느 마을일지 모른다 — 출발하면 {all.Count}곳 가운데 하나가 정해진다";
            var c = Ui.Rect("content", panel).Fill();
            Btn goBtn;
            if (!phone)
            {
                float y = 0;
                // 그림 띠 — 마을 배경(무작위는 물음표 문양) · 아래로 어둡게 · 그 위에 이름
                float banH = tiers ? 132 : 188;   // 단계 줄 자리를 내려고 그림 띠를 줄인다
                var ban = Ui.Img(c, Theme.Round, Theme.NavyWell, "banner").rectTransform; ban.Band(1, banH, 6, 6, -6);
                ban.gameObject.AddComponent<RectMask2D>();
                var sp = VilArt(v);
                if (sp != null)
                {
                    var pic = Ui.Img(ban, sp, Color.white, "pic"); pic.rectTransform.Fill();
                    var fit = pic.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = sp.rect.width / sp.rect.height;
                }
                else
                {
                    var em = Ui.Rect("emblem", ban).At(0.5f, 0.6f, 0, 0, 128, 128);
                    var disc = Ui.Img(em, Theme.S("circle"), Theme.NavyCell, "disc"); disc.rectTransform.Fill();
                    UnknownEmblem(disc.rectTransform, 128, true);
                }
                var fade = Ui.Img(ban, Theme.S("fade_down"), Theme.Night.A(0.92f), "fade"); fade.rectTransform.Band(0, 110);
                var ic = Ui.Img(ban, v != null ? Icon("종족_" + v.Race) : Theme.S("ic_question"), v != null ? Color.white : Theme.Gold, "race");
                ic.preserveAspect = true; ic.rectTransform.At(0, 0, pad - 6, 16, 40, 40);
                var nm = Ui.Title(ban, v?.Name ?? "무작위", Theme.FsXl + 4, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                nm.rectTransform.At(0, 0, pad + 42, 12, size.x - pad * 2 - 140, 48); nm.Outline(0.2f);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 18; nm.fontSizeMax = Theme.FsXl + 4;
                if (v != null)
                {
                    var (rb, _) = Ui.Chip(ban, null, v.Race ?? "", 32, Theme.NavyWell.A(0.85f), null, Theme.FsSm);
                    var rf = rb.gameObject.AddComponent<ContentSizeFitter>(); rf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    rb.rectTransform.At(1, 0, -pad + 6, 20, 80, 32);
                }
                y = 6 + banH + 12;
                var lt = Ui.Text(c, line, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft); lt.rectTransform.Band(1, 46, pad, pad, -y);
                lt.enableAutoSizing = true; lt.fontSizeMin = 12; lt.fontSizeMax = Theme.FsSm;
                y += 50;
                // 기록 · 두 층 땅
                var chips = Ui.Rect("chips", c).Band(1, 32, pad, pad, -y);
                Ui.Row(chips, 8, TextAnchor.MiddleLeft, null, false, false);
                if (v != null)
                {
                    DetailChip(chips, VilRecord(v.Id), Theme.Gold);
                    DetailChip(chips, string.Join(" · ", v.Floors.Select((f, i) => $"{i + 1}층 {f.Name}")), Theme.Sky);
                }
                else DetailChip(chips, $"{all.Count}곳 가운데 하나", Theme.Gold);
                y += 32 + 16;
                if (v != null)
                {
                    VLabel(c, "대표 아티팩트", pad, y, "ic_ring_slot"); y += 36;
                    y = ShowcaseRows(c, v, pad, y, 60) + 10;
                    VLabel(c, "나오는 엘리트", pad, y, "ic_skull"); y += 36;
                    EliteFaces(c, v, pad, y, 60, size.x - pad * 2); y += 60 + 14;
                }
                else
                {
                    VLabel(c, "나올 수 있는 마을", pad, y, "ic_flag"); y += 36;
                    y = VilMinis(c, all, pad, y, size.x - pad * 2) + 10;
                }
                VLabel(c, "보스", pad, y, "ic_crown"); y += 34;
                var bt = Ui.Text(c, boss, Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft); bt.rectTransform.Band(1, 28, pad, pad, -y);
                bt.textWrappingMode = TextWrappingModes.NoWrap; bt.enableAutoSizing = true; bt.fontSizeMin = 12; bt.fontSizeMax = Theme.FsSm;
                goBtn = Btn.Make(c, goLabel, BtnStyle.PillGold, go, Theme.FsLg, "vilpick.go");
                goBtn.GetComponent<RectTransform>().Band(0, Theme.BtnMainH, pad, pad, pad);
                if (tiers)
                {
                    var tr = Ui.Rect("tierrow", c); tr.Band(0, 112, pad, pad, pad + Theme.BtnMainH + 12);
                    var bg = Ui.Img(tr, Theme.Round, Theme.NavyWell.A(0.85f), "tierbg"); bg.rectTransform.Fill(-8, -6, -8, -6); bg.transform.SetAsFirstSibling();
                    TierRow(tr, size.x - pad * 2, false);
                }
            }
            else
            {
                // 폰 — 가로 네 칸
                float inner = size.x - pad * 2, gap = 14;
                float wa = inner * 0.32f, wb = inner * 0.27f, wd = Mathf.Min(250, inner * 0.19f), wc = inner - wa - wb - wd - gap * 3;
                var colA = Ui.Rect("info", c).At(0, 1, pad, -pad + 4, wa, size.y - pad * 2 + 8);
                var ic = Ui.Img(colA, v != null ? Icon("종족_" + v.Race) : Theme.S("ic_question"), v != null ? Color.white : Theme.Gold, "race");
                ic.preserveAspect = true; ic.rectTransform.At(0, 1, 0, -2, 34, 34);
                var nm = Ui.Title(colA, v?.Name ?? "무작위", Theme.FsXl, Theme.Ink, TextAlignmentOptions.MidlineLeft); nm.rectTransform.Band(1, 38, 42, 0, 0);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 16; nm.fontSizeMax = Theme.FsXl; nm.Outline(0.2f);
                var lt = Ui.Text(colA, line, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft); lt.rectTransform.Band(1, 76, 0, 0, -46);
                lt.enableAutoSizing = true; lt.fontSizeMin = 11; lt.fontSizeMax = Theme.FsSm;
                var chips = Ui.Rect("chips", colA).Band(1, 30, 0, 0, -128);
                Ui.Row(chips, 6, TextAnchor.MiddleLeft, null, false, false);
                if (v != null) { DetailChip(chips, VilRecord(v.Id), Theme.Gold, 30); DetailChip(chips, string.Join(" · ", v.Floors.Select(f => f.Name)), Theme.Sky, 30); }
                else DetailChip(chips, $"{all.Count}곳 가운데 하나", Theme.Gold, 30);
                var bt = Ui.Text(colA, $"<color={Theme.GoldTag}>보스</color>  {boss}", Theme.FsCap, Theme.Ink, TextAlignmentOptions.TopLeft); bt.rectTransform.Band(1, 24, 0, 0, -168);
                bt.textWrappingMode = TextWrappingModes.NoWrap; bt.enableAutoSizing = true; bt.fontSizeMin = 10; bt.fontSizeMax = Theme.FsCap;

                float xb = pad + wa + gap;
                var sepB = Ui.Img(c, null, Theme.Line, "sep"); sepB.rectTransform.At(0, 0.5f, xb - gap / 2, 0, 1, size.y - pad * 2);
                var colB = Ui.Rect("gear", c).At(0, 1, xb, -pad + 4, wb, size.y - pad * 2 + 8);
                var colC = Ui.Rect("elites", c).At(0, 1, xb + wb + gap, -pad + 4, wc, size.y - pad * 2 + 8);
                if (v != null)
                {
                    var sepC = Ui.Img(c, null, Theme.Line, "sep"); sepC.rectTransform.At(0, 0.5f, xb + wb + gap / 2, 0, 1, size.y - pad * 2);
                    VLabel(colB, "대표 아티팩트", 0, 0, "ic_ring_slot");
                    ShowcaseRows(colB, v, 0, 36, 54);
                    VLabel(colC, "나오는 엘리트", 0, 0, "ic_skull");
                    EliteFaces(colC, v, 0, 40, 56, wc);
                }
                else
                {
                    VLabel(colB, "나올 수 있는 마을", 0, 0, "ic_flag");
                    VilMinis(colB, all, 0, 36, wb + wc + gap, 50);
                }
                goBtn = Btn.Make(c, goLabel, BtnStyle.PillGold, go, Theme.FsMd + 2, "vilpick.go");
                if (tiers)
                {   // 폰 — 출발 단추 위에 「− 단계 칩 +」(맨 오른쪽 칸)
                    goBtn.GetComponent<RectTransform>().At(1, 0, -pad, pad, wd, Theme.BtnMainH);
                    var tr = Ui.Rect("tierrow", c).At(1, 1, -pad, -pad + 4, wd, size.y - pad * 2 - Theme.BtnMainH - 8);
                    TierRow(tr, wd, true);
                }
                else goBtn.GetComponent<RectTransform>().At(1, 0.5f, -pad, 0, wd, Theme.BtnMainH);
            }
            return goBtn;
        }

        /// <summary>상세 판 갈래 머리 — 금빛 아이콘 + 글(x · y 는 판 왼쪽 위에서).</summary>
        static void VLabel(RectTransform parent, string text, float x, float y, string icon)
        {
            var ic = Ui.Img(parent, Theme.S(icon), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 1, x, -(y + 3), 24, 24);
            var t = Ui.Title(parent, text, Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Band(1, 30, x + 32, x, -y); t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>상세 판 작은 칩 — 왼쪽 색 점 + 글.</summary>
        static void DetailChip(RectTransform row, string text, Color accent, float h = 32)
        {
            var (b, t) = Ui.Chip(row, Theme.S("dot"), text, h, Theme.NavyWell.A(0.9f), accent, Theme.FsCap + 1);
            t.color = Theme.Ink;
        }

        /// <summary>대표 아티팩트 줄(그림 · 이름 · 등급 · 종류) — 누르면 효과 판(TermPop). 없으면(마을 묶음 없음) 한 줄 안내. 돌려줌: 다음 y.</summary>
        float ShowcaseRows(RectTransform parent, VillageDef v, float x, float y, float rowH)
        {
            var ids = P.Data.VillageShowcase(v.Id);
            if (ids.Count == 0)
            {
                var t = Ui.Text(parent, "이 마을만의 아티팩트가 없다 — 모든 마을의 아티팩트가 나온다", Theme.FsSm, Theme.Dim, TextAlignmentOptions.TopLeft);
                t.rectTransform.Band(1, 44, x, x, -y); t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = Theme.FsSm;
                return y + 44;
            }
            for (int i = 0; i < ids.Count; i++)
            {
                var e = P.Data.Equip(ids[i]);
                var gc = Theme.GradeOf(e.Grade);
                var b = Btn.Make(parent, null, BtnStyle.Cell, null, 0, "equip " + e.Id);
                var rt = b.GetComponent<RectTransform>(); rt.Band(1, rowH, x, x, -y);
                Ui.Img(rt, Theme.Round, gc, "grade").rectTransform.At(0, 0.5f, 6, 0, 5, rowH - 14);
                var well = Ui.Img(rt, Theme.Round, Color.Lerp(Theme.NavyWell, gc, 0.25f), "well"); well.rectTransform.At(0, 0.5f, 16, 0, rowH - 10, rowH - 10);
                var pic = CardArt.Equip(e.Id);
                var ic = Ui.Img(well.transform, pic ?? W.SlotIcon(e.Slot), pic != null ? Color.white : gc, "icon"); ic.preserveAspect = true; ic.rectTransform.Fill(4, 4, 4, 4);
                var nt = Ui.Title(rt, e.Name, Theme.FsMd, Theme.Ink, TextAlignmentOptions.BottomLeft); nt.rectTransform.Fill(rowH + 16, rowH * 0.5f - 2, 34, 4);
                nt.textWrappingMode = TextWrappingModes.NoWrap; nt.enableAutoSizing = true; nt.fontSizeMin = 12; nt.fontSizeMax = Theme.FsMd;
                var st = Ui.Text(rt, $"<color=#{ColorUtility.ToHtmlStringRGB(gc)}>{e.Grade}</color> · {e.Slot}", Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft);
                st.rectTransform.Fill(rowH + 16, 4, 34, rowH * 0.5f + 2); st.textWrappingMode = TextWrappingModes.NoWrap;
                var ii = Ui.Img(rt, Theme.S("ic_info"), Theme.Dim, "info"); ii.preserveAspect = true; ii.rectTransform.At(1, 0.5f, -10, 0, 18, 18);
                var term = EquipTerm(e);
                b.OnClick = () => PopTerms(rt, new List<CardTerms.Term> { term });
                Stage.Hot["vilpick.equip" + i] = b;
                y += rowH + 8;
            }
            return y;
        }

        /// <summary>아티팩트 효과 판 — 능력치 · 효과 글(도감 상세와 같은 글).</summary>
        CardTerms.Term EquipTerm(EquipDef e)
        {
            var body = new List<string>();
            var st = W.StatLine(e.Stats);
            if (!string.IsNullOrEmpty(st)) body.Add($"<color={Theme.GoodTag}>{st}</color>");
            if (e.Effect != null && e.Effect.Count > 0) body.Add(P.Text.Passives(e.Effect));
            if (body.Count == 0) body.Add($"<color={Theme.SubTag}>능력치 · 효과 없음</color>");
            return new CardTerms.Term { Name = e.Name, Kind = $"{e.Grade} · {e.Slot} 아티팩트", Body = string.Join("\n", body) };
        }

        /// <summary>나오는 엘리트 얼굴(두 층 엘리트 칸의 「_elite」 적 · 같은 적은 한 번) — 둥근 초상만, 너비에 들어가는 만큼.</summary>
        void EliteFaces(RectTransform parent, VillageDef v, float x, float y, float s, float w)
        {
            var ids = v.Floors.SelectMany(f => f.Elites.SelectMany(g => g.Where(id => id.EndsWith("_elite")).DefaultIfEmpty(g.FirstOrDefault())))
                .Where(id => id != null).Distinct().ToList();
            int fit = Mathf.Max(1, Mathf.FloorToInt((w + 8) / (s + 8)));
            var row = Ui.Rect("elites", parent).Band(1, s, x, x, -y);
            Ui.Row(row, 8, TextAnchor.MiddleLeft, null, false, false);
            foreach (var id in ids.Take(fit))
            {
                var e = P.Data.Enemy(id);
                var holder = Ui.Img(row, Theme.S("circle"), Theme.NavyCell, "elite " + id); holder.Pref(s, s);
                holder.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var art = FoeArt(e, 1);
                var sp = art ?? FoeSilhouette(e);   // 그림이 없으면 같은 몬스터 다른 성격 그림을 검게(실루엣)
                if (sp != null)
                {
                    var im = Ui.Img(holder.transform, sp, art != null ? Color.white : Color.black, "pic"); im.preserveAspect = true; im.rectTransform.Fill(2, 2, 2, 2);
                }
                else { var sk = Ui.Img(holder.transform, Theme.S("ic_skull"), Theme.Dim, "pic"); sk.preserveAspect = true; sk.rectTransform.Fill(s * 0.25f, s * 0.25f, s * 0.25f, s * 0.25f); }
                var rg = Ui.Img(holder.transform, Theme.S("ring"), Theme.Bad.A(0.75f), "ring"); rg.rectTransform.Fill();
            }
            if (ids.Count == 0) { var t = Ui.Text(row, "엘리트 없음", Theme.FsSm, Theme.Dim); t.Pref(160, s); }
        }

        /// <summary>「무작위」 상세 — 나올 수 있는 마을 작은 원(그림 · 이름) 격자. 돌려줌: 다음 y.</summary>
        float VilMinis(RectTransform parent, List<VillageDef> all, float x, float y, float w, float s = 64)
        {
            float cellW = s + 44, cellH = s + 24;
            int cols = Mathf.Max(1, Mathf.FloorToInt(w / cellW));
            cellW = w / cols;
            for (int i = 0; i < all.Count; i++)
            {
                var v = all[i];
                float cx = x + cellW * (i % cols), cy = y + cellH * (i / cols);
                var cell = Ui.Rect("mini " + v.Id, parent).At(0, 1, cx, -cy, cellW, cellH);
                var disc = Ui.Img(cell, Theme.S("circle"), Theme.NavyWell, "disc"); disc.rectTransform.At(0.5f, 1, 0, 0, s, s);
                disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var sp = VilArt(v);
                if (sp != null) { float ph = s * 1.5f; var pic = Ui.Img(disc.transform, sp, Color.white, "pic"); pic.rectTransform.At(0.5f, 0.5f, 0, -s * 0.2f, ph * sp.rect.width / sp.rect.height, ph); }
                var rg = Ui.Img(disc.transform, Theme.S("ring"), Theme.Edge.A(0.8f), "ring"); rg.rectTransform.Fill();
                var t = Ui.Text(cell, v.Name, Theme.FsCap, Theme.Sub, TextAlignmentOptions.Top); t.rectTransform.Band(0, 22, 0, 0, 0);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 9; t.fontSizeMax = Theme.FsCap;
            }
            return y + cellH * Mathf.CeilToInt(all.Count / (float)cols);
        }
    }
}
