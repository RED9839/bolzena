using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 덱 보기 · 카드 크게 · 사도별 카드 묶음(카드 제거 · 이벤트 카드 고르기가 같이 쓴다).
    // 순서는 언제나 CardOrder — 사도(편성 순서)마다 기본 카드 → 고유 카드, 그 뒤 상태 · 저주, 맨 끝 교주 카드.
    public partial class Flow
    {
        // ── 덱 보기 ── 카제나 「모든 덱」 의 배치만: 화면 하나를 덮는 창 · 왼쪽 세로 탭 줄 · 위 왼쪽 제목 · 위 오른쪽 정보 칸 둘 + 큰 ×
        //   묶음마다: 왼쪽 덱 아이콘 + 장수(05) + 세로 눈금 → 사도 카드(세로 초상 — 역할 · 성격 아이콘 · 별 · 이름) → 카드 한 줄 4장씩
        public void DeckView()
        {
            var size = Stage.Size;
            var layer = Ui.Rect("modal deck", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, Theme.Night, "dim", true); dim.rectTransform.Fill();   // 불투명(선형 색 공간에서는 조금만 비쳐도 밑 글이 읽힌다)
            var halo = Ui.Img(layer, Theme.S("soft"), Theme.NavyCell.A(0.6f), "halo"); halo.rectTransform.At(0.5f, 0.5f, 0, 0, 1800, 1100);
            var lg = layer.Group(); lg.alpha = 0;
            Tw.Run(layer, 0.22f, k => { if (lg) lg.alpha = k; });
            Action close = null;
            close = () => { var g = lg; Tw.Run(layer, 0.16f, k => { if (g) g.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); }); };

            // 왼쪽 세로 탭 줄 — 덱(지금) · 낀 장비 · 사도 한 장
            float railW = 84;
            var rail = Ui.Img(layer, Theme.White, Theme.NavyPanel.A(0.9f), "rail").rectTransform;
            rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.pivot = new Vector2(0, 0.5f); rail.sizeDelta = new Vector2(railW, 0); rail.anchoredPosition = Vector2.zero;
            var rline = Ui.Img(rail, Theme.White, Theme.Edge.A(0.35f), "edge"); rline.rectTransform.anchorMin = new Vector2(1, 0); rline.rectTransform.anchorMax = new Vector2(1, 1); rline.rectTransform.sizeDelta = new Vector2(1, 0);
            Btn Tab(int i, string icon, string label, bool on, Action go)
            {
                var b = Btn.Make(rail, null, BtnStyle.Ghost, go, 0, "tab " + label);
                b.Bg.sprite = Theme.Round; b.SetColor(on ? Theme.Gold.A(0.16f) : new Color(1, 1, 1, 0));
                var rt = b.GetComponent<RectTransform>(); rt.At(0.5f, 1, 0, -110 - i * 92, 68, 80);
                var ic = Ui.Img(rt, Theme.S(icon), on ? Theme.Gold : Theme.Sub, "ic"); ic.rectTransform.At(0.5f, 1, 0, -10, 34, 34); ic.preserveAspect = true;
                var t = Ui.Text(rt, label, Theme.FsCap, on ? Theme.Gold : Theme.Sub, TextAlignmentOptions.Center); t.rectTransform.Band(0, 22, 0, 0, 8);
                if (on) { var bar = Ui.Img(rail, Theme.Round, Theme.Gold, "on"); bar.rectTransform.At(0, 1, 0, -110 - i * 92 - 20, 4, 40); }
                return b;
            }
            Tab(0, "ic_deck", "덱", true, null);
            Stage.Hot["deck.gear"] = Tab(1, "ic_sword", "장비", false, () => { close(); GearView(); });

            // 위 — 제목 · 정보 칸 둘 · 큰 ×
            var (cnt, avg) = DeckInfo();
            var title = Ui.Title(layer, "모든 덱", Theme.Fs2xl, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.At(0, 1, railW + 34, -20, 400, 56);
            var x = Btn.Icon(layer, Theme.S("ic_x"), close, 60, "close");
            x.GetComponent<RectTransform>().At(1, 1, -Theme.Gutter, -18, 60, 60);
            Stage.Hot["deck.close"] = x;
            void Info(float right, string label, string val)
            {
                var box = Ui.Img(layer, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.9f), "info").rectTransform;
                box.At(1, 1, right, -22, 270, 54);
                var l = Ui.Text(box, label, Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineLeft); l.rectTransform.Fill(22, 0, 70, 0);
                var v = Ui.Title(box, val, Theme.FsXl, Theme.Gold, TextAlignmentOptions.MidlineRight); v.rectTransform.Fill(0, 0, 22, 0);
            }
            Info(-Theme.Gutter - 76, "고유 카드 평균 비용", avg < 0 ? "—" : avg.ToString("0.0"));
            Info(-Theme.Gutter - 76 - 282, "카드 개수", cnt.ToString());
            var rule = Ui.Img(layer, Theme.White, Theme.Line, "rule"); rule.rectTransform.Band(1, 1, railW + 24, Theme.Gutter, -92);

            // 묶음 목록(세로 스크롤 + 막대)
            var area = Ui.Rect("area", layer); area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(railW + 20, 12); area.offsetMax = new Vector2(-Theme.Gutter - 18, -100);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            Ui.Col(content, Theme.C(22, 16), TextAnchor.UpperLeft, new RectOffset(4, 4, 10, 30), true, false);

            float areaW = size.x - railW - 20 - Theme.Gutter - 18;
            float tickW = 64, headW = Theme.C(158, 140), gap = 12;
            float cw = Mathf.Clamp((areaW - tickW - headW - 24 - gap * 3 - 12) / 4f, 150, 232), ch = cw * 1.4f;
            float headH = headW * 1.42f;
            int gi = 0, ci = 0;
            foreach (var g in CardOrder.Groups(P.S.Deck, P.Data, P.S.Party))
            {
                int rows = Mathf.CeilToInt(g.Ids.Count / 4f);
                float gh = Mathf.Max(headH, rows * (ch + gap) - gap);
                var grp = Ui.Rect("group " + g.Title, content); grp.Pref(-1, gh);
                // 장수 + 세로 눈금
                var tick = Ui.Rect("tick", grp).At(0, 1, 0, 0, tickW, gh);
                var di = Ui.Img(tick, Theme.S("ic_deck"), Theme.Sub, "ic"); di.rectTransform.At(0.5f, 1, 0, -4, 26, 26); di.preserveAspect = true;
                var dn = Ui.Title(tick, g.Ids.Count.ToString("00"), Theme.FsXl, Theme.Ink, TextAlignmentOptions.Center); dn.rectTransform.At(0.5f, 1, 0, -32, tickW, 36);
                var vl = Ui.Img(tick, Theme.White, Theme.Edge.A(0.35f), "line"); vl.rectTransform.At(0.5f, 1, 0, -74, 2, gh - 78);
                for (float t = 0; t < gh - 90; t += 22) { var tk = Ui.Img(tick, Theme.White, Theme.Edge.A(0.35f), "t"); tk.rectTransform.At(0.5f, 1, 6, -80 - t, 10, 1); }
                // 머리 카드(사도 · 상태 · 교주)
                var head = GroupHead(grp, g, headW, headH);
                head.At(0, 1, tickW + 12, 0, headW, headH);
                // 카드
                var holder = Ui.Rect("cards", grp).At(0, 1, tickW + headW + 24, 0, cw * 4 + gap * 3, gh);
                for (int i = 0; i < g.Ids.Count; i++)
                {
                    var id = g.Ids[i];
                    var c = W.Card(holder, this, id, cw);
                    c.At(0, 1, (i % 4) * (cw + gap), -(i / 4) * (ch + gap), cw, ch);
                    var b = c.gameObject.AddComponent<Btn>();
                    b.OnClick = () => CardZoom(id);
                    Stage.Hot["deck.card" + ci] = b;
                    if (ci < 24) Tw.Pop(c, 0.02f * ci, 0.9f, 0.25f);
                    ci++;
                }
                gi++;
            }
        }

        /// <summary>묶음 머리 — 사도 카드(세로 초상 · 역할 · 성격 · 별 · 이름) · 상태/저주 · 교주.</summary>
        RectTransform GroupHead(RectTransform parent, CardOrder.Group g, float w, float h)
        {
            var hd = g.Hero != null ? P.Data.Hero(g.Hero) : null;
            var info = g.Hero != null ? Roster.OfCore(g.Hero) : null;
            Color tint = g.Kind == CardOrder.Kind.Leader ? Theme.LeaderCard : g.Kind == CardOrder.Kind.Status ? Theme.StatusCard : Theme.NatureCardOf(hd?.Nature ?? info?.nature);
            var bg = Ui.Img(parent, Theme.Cell, Color.white, "head");
            var rt = bg.rectTransform;
            var well = Ui.Img(rt, Theme.Round, Color.Lerp(Theme.NavyWell, tint, 0.25f), "well"); well.rectTransform.Fill(6, 6, 6, 6);
            well.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(well.transform, Theme.S("fade_top"), tint.A(0.5f), "glow"); glow.rectTransform.Fill();
            if (info?.Icon != null)
            {
                var im = Ui.Img(well.transform, info.Icon, Color.white, "portrait"); im.rectTransform.At(0.5f, 1, 6, 0, h * 0.95f, h * 0.95f); im.preserveAspect = true;
            }
            else
            {
                var gi = Ui.Img(well.transform, Theme.S(g.Kind == CardOrder.Kind.Leader ? "ic_crown" : "ic_skull"), tint, "glyph"); gi.rectTransform.At(0.5f, 0.58f, 0, 0, w * 0.45f, w * 0.45f); gi.preserveAspect = true;
            }
            var shade = Ui.Img(well.transform, Theme.S("fade_down"), Color.black.A(0.85f), "shade"); shade.rectTransform.Band(0, h * 0.42f);
            // 왼쪽 위 세로 아이콘 열 — 역할 · 성격 · 줄
            if (info != null)
            {
                var col = Ui.Rect("icons", rt).At(0, 1, 10, -10, 28, 100);
                Ui.Col(col, 4, TextAnchor.UpperLeft, null, false, false);
                foreach (var key in new[] { "역할_" + info.role, "성격_" + info.nature, "위치_" + info.RowKo })
                {
                    var sp = Theme.Icon(key);
                    if (sp == null) continue;
                    var ic = Ui.Img(col, sp, Color.white, key); ic.Pref(26, 26); ic.preserveAspect = true;
                }
            }
            var name = Ui.Title(rt, g.Kind == CardOrder.Kind.Leader ? "교주" : g.Kind == CardOrder.Kind.Status ? "상태 · 저주" : info?.ko ?? g.Hero, Theme.FsLg, Theme.Ink, TextAlignmentOptions.BottomLeft);
            name.rectTransform.Band(0, 30, 12, 8, 12);
            name.textWrappingMode = TextWrappingModes.NoWrap; name.enableAutoSizing = true; name.fontSizeMin = 12; name.fontSizeMax = Theme.FsLg; name.Outline(0.2f);
            var sub = Ui.Text(rt, g.Kind == CardOrder.Kind.Hero ? new string('★', Mathf.Clamp(info?.star ?? 3, 1, 5)) : g.Kind == CardOrder.Kind.Leader ? "교주 카드" : "적 · 이벤트가 넣은 카드", Theme.FsSm, g.Kind == CardOrder.Kind.Hero ? Theme.Gold : Theme.Sub, TextAlignmentOptions.BottomLeft);
            sub.rectTransform.Band(0, 20, 12, 8, 44); sub.Outline(0.2f);
            var frame = Ui.Img(rt, Theme.S("frame", 24), tint, "frame"); frame.rectTransform.Fill();
            return rt;
        }

        /// <summary>카드 크게 — 왼쪽 카드 한 장, 오른쪽 신탁 ①~⑤ · 겨우살이의 축복.</summary>
        public void CardZoom(string id)
        {
            var v = P.View(id);
            var def = P.Data.Card(id);
            var (body, close, _) = Stage.ModalBox("cardzoom", 1180, Theme.C(700, 690), v?.Name ?? id, def?.Blurb ?? (v?.Hero != null ? Roster.OfCore(v.Hero).ko + "의 카드" : "교주 카드"));
            float cw = Theme.C(330, 300);
            var card = W.Card(body, this, id, cw); card.At(0, 0.5f, 10, 0, cw, cw * 1.4f);
            var right = Ui.Rect("right", body).Fill(cw + 40, 0, 6, 0);
            var content = Ui.Scroll(right, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 10, 4, 10), true, false);
            void Line(string text, float size, Color c)
            {
                var t = Ui.Text(content, text, size, c, TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.Normal;
                var fit = t.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            if (def != null && def.Oracles.Count > 0)
            {
                W.Section(content, "신탁", "카드를 내면 빛나고 · 보상에서 하나를 얹는다", 36);
                for (int i = 0; i < def.Oracles.Count; i++)
                {
                    var o = def.Oracles[i];
                    bool cur = v?.Oracle == o;
                    Line($"<color={(cur ? Theme.GoodTag : Theme.GoldTag)}>{"①②③④⑤⑥"[Mathf.Min(i, 5)]} {o.Name}</color>{(cur ? "  <size=80%>(얹은 것)</size>" : "")}\n<size=90%>{P.Text.Oracle(def, o)}</size>", Theme.FsBody, Theme.Ink);
                }
            }
            if (def != null && def.Hero != null)
            {
                W.Section(content, "겨우살이의 축복", def.Blesses.Count > 0 ? "이 카드만의 축복" : "공용 축복에서", 36);
                if (def.Blesses.Count > 0)
                    foreach (var b in def.Blesses) Line($"<color={Theme.GoldTag}>{b.Name}</color>  <size=90%>{P.Text.Bless(b)}</size>", Theme.FsBody, Theme.Ink);
                else Line($"<color={Theme.SubTag}>이 카드만의 축복은 없습니다 — 축복을 받으면 공용 축복 가운데 하나가 붙습니다.</color>", Theme.FsSm, Theme.Sub);
            }
            if (def == null || (def.Oracles.Count == 0 && def.Hero == null))
                Line($"<color={Theme.SubTag}>신탁 · 축복이 없는 카드입니다.</color>", Theme.FsBody, Theme.Sub);
            Stage.Hot["zoom.close"] = Stage.Hot["modal.x"];
        }

        /// <summary>사도별 카드 묶음(작은 머리표 + 격자) — 카드 제거 · 이벤트 카드 고르기. stack 이면 같은 카드는 한 장 + ×N.</summary>
        void CardGroups(RectTransform content, IEnumerable<string> ids, float cw, int cols, bool stack, Action<RectTransform, string, int> each)
        {
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(8, 8, 4, 12), true, false);
            float ch = cw * 1.4f, gap = 12;
            int i = 0;
            foreach (var g in CardOrder.Groups(ids, P.Data, P.S?.Party))
            {
                var hrow = Ui.Rect("head " + g.Title, content); hrow.Pref(-1, 44);
                var info = g.Hero != null ? Roster.OfCore(g.Hero) : null;
                if (info != null) { var fc = W.Face(hrow, info, 38); fc.At(0, 0.5f, 0, 0, 38, 38); }
                else { var ic = Ui.Img(hrow, Theme.S(g.Kind == CardOrder.Kind.Leader ? "ic_crown" : "ic_skull"), g.Kind == CardOrder.Kind.Leader ? Theme.Gold : Theme.Sub, "ic"); ic.rectTransform.At(0, 0.5f, 4, 0, 30, 30); ic.preserveAspect = true; }
                var list = stack ? g.Ids.GroupBy(x => x).Select(x => (x.Key, x.Count())).ToList() : g.Ids.Select(x => (x, 1)).ToList();
                var t = Ui.Title(hrow, $"{(g.Kind == CardOrder.Kind.Leader ? "교주 카드" : g.Kind == CardOrder.Kind.Status ? "상태 · 저주" : info?.ko ?? g.Hero)}  <size=70%><color={Theme.SubTag}>{g.Ids.Count}장</color></size>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                t.rectTransform.Fill(48, 0, 0, 0);
                var line = Ui.Img(hrow, Theme.White, Theme.Line, "rule"); line.rectTransform.Band(0, 1, 48, 0, 0);
                int rows = Mathf.CeilToInt(list.Count / (float)cols);
                var holder = Ui.Rect("cards", content); holder.Pref(-1, rows * (ch + gap));
                for (int j = 0; j < list.Count; j++)
                {
                    var (id, n) = list[j];
                    var c = W.Card(holder, this, id, cw);
                    c.At(0, 1, (j % cols) * (cw + gap), -(j / cols) * (ch + gap), cw, ch);
                    if (n > 1) { var (cb, ct) = Ui.Chip(c, null, "×" + n, 30, Theme.Gold, null, 18); cb.rectTransform.At(1, 0, -6, 6, 50, 30); ct.color = Theme.Brown; }
                    each(c, id, i);
                    if (i < 30) Tw.Pop(c, 0.02f * i, 0.85f, 0.3f);
                    i++;
                }
            }
        }

        /// <summary>
        /// 신탁 고르기 한 줄 — 코어가 고른 후보(그 카드 신탁 다섯 가운데 무작위 셋 · 확률로 축복, Core.OracleOption)를 신탁을 얹은 카드 모습으로 늘어놓는다.
        /// 축복이 붙은 후보는 금빛 「축복!」 띠와 축복 이름 · 글. 이벤트 · 캠프(Run.FlashOptions) · 보상의 빛났던 카드(OracleOption.Of) 가 같이 쓴다.
        /// </summary>
        void OracleRow(RectTransform row, string cardId, List<Core.OracleOption> opts, float cw, Action<int> pick, string hotPrefix)
        {
            Ui.Row(row, 26, TextAnchor.MiddleCenter, null, false, false);
            for (int i = 0; i < opts.Count; i++)
            {
                int idx = i;
                var o = opts[i];
                var holder = Ui.Rect("oracle" + o.N, row); holder.Pref(cw, cw * 1.4f + 70);
                var view = P.Data.View(cardId, o.N);
                var face = W.Card(holder, this, cardId, cw, "card", view);
                face.At(0.5f, 1, 0, -6, cw, cw * 1.4f);
                var num = Ui.Img(face, Theme.S("circle"), Theme.Gold, "n"); num.rectTransform.At(0.5f, 1, 0, 14, 30, 30);
                var nt = Ui.Title(num.transform, "①②③④⑤⑥"[Mathf.Clamp(o.N - 1, 0, 5)].ToString(), Theme.FsMd, Theme.Brown, TextAlignmentOptions.Center); nt.rectTransform.Fill();
                if (o.Blessed)
                {
                    var rib = Ui.Img(face, Theme.Pill, Theme.Gold, "bless"); rib.rectTransform.At(0.5f, 0, 0, -16, cw * 0.62f, 30);
                    var rt2 = Ui.Title(rib.transform, "축복!", Theme.FsMd, Theme.Brown, TextAlignmentOptions.Center); rt2.rectTransform.Fill();
                    var glow = Ui.Img(holder, Theme.S("soft"), Theme.Gold.A(0.45f), "glow"); glow.rectTransform.At(0.5f, 1, 0, 30, cw * 1.5f, cw * 1.9f); glow.transform.SetAsFirstSibling();
                    Tw.Pulse(glow, 0.25f, 0.55f, 1.6f);
                    var bt = Ui.Text(holder, $"<color={Theme.GoldTag}>{o.BlessName}</color>  {o.BlessText}", Theme.FsCap, Theme.Ink, TextAlignmentOptions.Top);
                    bt.rectTransform.At(0.5f, 0, 0, 0, cw + 20, 48); bt.enableAutoSizing = true; bt.fontSizeMin = 10; bt.fontSizeMax = Theme.FsCap;
                }
                var b = face.gameObject.AddComponent<Btn>();
                b.OnClick = () => pick(idx);
                Stage.Hot[hotPrefix + i] = b;
                Tw.Pop(holder, 0.1f + i * 0.08f, 0.6f, 0.4f);
            }
        }

        /// <summary>세로 스크롤 막대(얇은 금빛 손잡이).</summary>
        static void ScrollBar(RectTransform area, ScrollRect sr)
        {
            var track = Ui.Img(area, Theme.Round, Color.white.A(0.08f), "scrollbar", true).rectTransform;
            track.anchorMin = new Vector2(1, 0); track.anchorMax = new Vector2(1, 1); track.pivot = new Vector2(0, 0.5f);
            track.sizeDelta = new Vector2(6, -8); track.anchoredPosition = new Vector2(8, 0);
            var slide = Ui.Rect("slide", track).Fill();
            var handle = Ui.Img(slide, Theme.Round, Theme.Gold.A(0.7f), "handle", true);
            handle.rectTransform.sizeDelta = Vector2.zero;
            var sb = track.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle.rectTransform; sb.targetGraphic = handle; sb.direction = Scrollbar.Direction.BottomToTop;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
    }
}
