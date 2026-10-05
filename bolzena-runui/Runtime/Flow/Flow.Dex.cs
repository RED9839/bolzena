using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 도감 — 사도 · 교주 카드 · 장비 · 적(Flow.DexFoes.cs)을 한 화면 틀(사도 목록과 같은 꼴)에서 탭으로 오간다.
    //   왼쪽 세로 필터 탭 · 카드 격자 · 위 오른쪽 정렬 + 도감 탭 · 아래 남색 띠(고른 것 · 「상세 정보」) · 누르면 바로 상세
    //   교주 카드: 교주 카드만(상태 · 저주 · 선물 · 토큰은 뺀다). 필터 = 등급(일반 · 고급 · 희귀 · 전설), 상세 = 카드 크게 + 신탁 · 축복(CardZoom)
    //   장비: 필터 = 종류(무기 · 방어구 · 장신구) + 등급, 상세 = 그림 · 등급 · 능력치 · 효과 · 애착(EquipDetail)
    //   정렬 기본 = 등급 순(전설 먼저) → 같은 등급 안 가나다순. 탭마다 필터 · 정렬 · 스크롤 자리를 기억한다.
    public partial class Flow
    {
        class ItemListState { public string Grade, Slot, Focus; public string Sort = "등급"; public float ScrollY; public bool Drawn; }
        ListState dexHeroes;
        ItemListState dexCards, dexEquips;
        static readonly string[] Grades = { "일반", "고급", "희귀", "전설" };

        /// <summary>도감 — 처음은 사도 탭. 탭을 오가도 같은 화면 안에서 다시 그린다.</summary>
        public void Dex(Action back)
        {
            dexHeroes = new ListState(); dexCards = new ItemListState(); dexEquips = new ItemListState(); dexFoes = new FoeListState();
            Stage.SetBg("stage1_1", 0.7f);
            Stage.Show("dex", root => DexTab(root, "사도", back));
        }

        void DexTab(RectTransform host, string tab, Action back)
        {
            if (tab == "사도") HeroList(null, -1, back, dexHeroes ??= new ListState(), host);
            else if (tab == "적") DexFoes(host, back, dexFoes ??= new FoeListState());
            else DexItems(host, tab, back, tab == "장비" ? dexEquips ??= new ItemListState() : dexCards ??= new ItemListState());
        }

        /// <summary>위 오른쪽 도감 탭 셋 — 지금 탭은 금 알약.</summary>
        void DexTabs(RectTransform row, RectTransform host, string current, Action back)
        {
            foreach (var t in new[] { "사도", "교주 카드", "장비", "적" })
            {
                bool on = t == current;
                var icon = t == "사도" ? "ic_book" : t == "장비" ? "ic_sword" : t == "적" ? "ic_skull" : "ic_crown";
                Btn b;
                if (on)
                {
                    b = Btn.Make(row, null, BtnStyle.PillGold, null, 0, "tab " + t);
                    var rt = b.GetComponent<RectTransform>();
                    Ui.Row(rt, 8, TextAnchor.MiddleCenter, new RectOffset(16, 22, 6, 6), false, false);
                    var ic = Ui.Img(rt, Theme.S(icon), Theme.Brown, "ic"); ic.preserveAspect = true; ic.Pref(24, 24);
                    var tl = Ui.Title(rt, t, Theme.FsBody, Theme.Brown); tl.textWrappingMode = TextWrappingModes.NoWrap;
                    b.Pref(-1, 52);
                }
                else b = NavyPill(row, icon, t, () => DexTab(host, t, back), "tab " + t);
                Stage.Hot["tab:" + t] = b;
            }
        }

        static int GradeOrder(string g) => g == "일반" ? 0 : g == "고급" ? 1 : g == "희귀" ? 2 : g == "전설" ? 3 : -1;

        // ── 교주 카드 · 장비 목록 ──
        void DexItems(RectTransform host, string tab, Action back, ItemListState ls)
        {
            Ui.Clear(host);
            bool eq = tab == "장비";
            RosterBg(host, 0.9f);
            void Rebuild() => DexItems(host, tab, back, ls);
            void Refilter() { ls.ScrollY = 0; Rebuild(); }
            RosterHead(host, eq ? "장비 도감" : "교주 카드 도감", back);

            // 위 오른쪽 — 정렬 · 도감 탭
            var tr = Ui.Rect("tools", host).At(1, 1, -Theme.Gutter, -20, 1240, 52);
            Ui.Row(tr, 10, TextAnchor.MiddleRight, null, false, true);
            var sorts = eq ? new[] { "등급", "이름", "종류" } : new[] { "등급", "이름", "비용" };
            if (Array.IndexOf(sorts, ls.Sort) < 0) ls.Sort = "등급";
            var sb = NavyPill(tr, "ic_refresh", $"<color={Theme.SubTag}>정렬</color>  {ls.Sort}", () => { ls.Sort = sorts[(Array.IndexOf(sorts, ls.Sort) + 1) % sorts.Length]; Refilter(); }, "sort", 168);
            Stage.Hot["list.sort"] = sb;
            DexTabs(tr, host, tab, back);

            // 왼쪽 세로 필터 탭
            float railW = 76;
            var rail = Ui.Rect("rail", host); rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.pivot = new Vector2(0, 1);
            rail.sizeDelta = new Vector2(railW, -110); rail.anchoredPosition = new Vector2(Theme.Gutter, -96);
            Ui.Col(rail, Theme.C(8, 4), TextAnchor.UpperCenter, null, false, false);
            float tabH = eq ? Theme.C(58, 44) : Theme.C(62, 50);
            void Tab(string label, bool on, Action go, Sprite icon = null, Color? dot = null, string hot = null)
            {
                var b = Btn.Make(rail, null, BtnStyle.Ghost, go, 0, "tab " + label);
                b.Bg.sprite = Theme.Round; b.SetColor(on ? Theme.Gold.A(0.16f) : Theme.NavyCell.A(0.6f));
                b.Pref(railW, tabH);
                if (icon != null && Theme.Compact)
                {
                    // 폰 — 칸이 낮아 글을 빼고 아이콘만
                    var ic = Ui.Img(b.transform, icon, on ? Theme.Gold : Theme.Sub, "ic"); ic.rectTransform.Fill(railW * 0.3f, tabH * 0.2f, railW * 0.3f, tabH * 0.2f); ic.preserveAspect = true;
                }
                else if (icon != null)
                {
                    var ic = Ui.Img(b.transform, icon, on ? Theme.Gold : Theme.Sub, "ic"); ic.rectTransform.At(0.5f, 1, 0, -7, 24, 24); ic.preserveAspect = true;
                    var t = Ui.Text(b.transform, label, Theme.FsCap, on ? Theme.Gold : Theme.Sub, TextAlignmentOptions.Bottom); t.rectTransform.Fill(2, 4, 2, 0); t.textWrappingMode = TextWrappingModes.NoWrap;
                }
                else
                {
                    var t = Ui.Title(b.transform, label, label == "ALL" ? Theme.FsLg : Theme.FsBody, on ? Theme.Gold : dot != null ? Color.Lerp(dot.Value, Color.white, 0.35f) : Theme.Sub, TextAlignmentOptions.Center); t.rectTransform.Fill();
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                }
                if (dot != null) { var d = Ui.Img(b.transform, Theme.S("circle"), dot.Value, "dot"); d.rectTransform.At(1, 1, -6, -6, 9, 9); }
                if (on)
                {
                    var br = Ui.Img(b.transform, Theme.Frame, Theme.Gold, "on"); br.rectTransform.Fill();
                    var bar = Ui.Img(b.transform, Theme.Round, Theme.Gold, "bar"); bar.rectTransform.At(0, 0.5f, -10, 0, 4, tabH * 0.55f);
                }
                Stage.Hot[hot ?? "filter:" + label] = b;
            }
            if (eq)
            {
                Tab("ALL", ls.Slot == null, () => { ls.Slot = null; Refilter(); });
                foreach (var s in RunPort.Slots) Tab(s, ls.Slot == s, () => { ls.Slot = s; Refilter(); }, W.SlotIcon(s));
                var gap = Ui.Img(rail, Theme.White, Theme.Edge.A(0.35f), "gap"); gap.Pref(railW - 20, 1);
                foreach (var g in Grades) Tab(g, ls.Grade == g, () => { ls.Grade = ls.Grade == g ? null : g; Refilter(); }, null, Theme.GradeOf(g));
            }
            else
            {
                Tab("ALL", ls.Grade == null, () => { ls.Grade = null; Refilter(); });
                foreach (var g in Grades) Tab(g, ls.Grade == g, () => { ls.Grade = g; Refilter(); }, null, Theme.GradeOf(g));
            }

            // 격자
            float footH = Theme.C(84, 72);
            var area = Ui.Rect("grid", host); area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Theme.Gutter + railW + 16, footH + 8); area.offsetMax = new Vector2(-Theme.Gutter, -92);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            float k = Theme.C(1f, 0.92f);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            float cw = 158 * k;
            grid.cellSize = eq ? new Vector2(cw, cw * 1.2f) : new Vector2(cw, cw * 1.4f);
            grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(6, 18, 8, 16); grid.childAlignment = TextAnchor.UpperLeft;

            var keys = new List<string>();
            string focusName = null, focusSub = null;
            int idx = 0;
            bool anim = !ls.Drawn;
            if (eq)
            {
                IEnumerable<Core.EquipDef> list = P.Data.Equips.Values.Where(e => (ls.Slot == null || e.Slot == ls.Slot) && (ls.Grade == null || e.Grade == ls.Grade));
                list = ls.Sort switch
                {
                    "이름" => list.OrderBy(e => e.Name, StringComparer.Ordinal),
                    "종류" => list.OrderBy(e => Array.IndexOf(RunPort.Slots, e.Slot)).ThenByDescending(e => GradeOrder(e.Grade)).ThenBy(e => e.Name, StringComparer.Ordinal),
                    _ => list.OrderByDescending(e => GradeOrder(e.Grade)).ThenBy(e => e.Name, StringComparer.Ordinal),
                };
                foreach (var e in list)
                {
                    keys.Add(e.Id);
                    EquipTile(content, e, ls, cw, idx++, anim, Rebuild);
                    if (ls.Focus == e.Id) { focusName = e.Name; focusSub = $"{e.Slot} · {e.Grade}" + (e.Affinity != null ? $" · 애착 {Roster.OfCore(e.Affinity).ko}" : ""); }
                }
            }
            else
            {
                IEnumerable<Core.CardDef> list = P.Data.Cards.Values.Where(c => c.Neutral)   // 교주 카드만(상태 · 저주 · 선물 · 토큰 빼고)
                    .Where(c => ls.Grade == null || c.Grade == ls.Grade);
                list = ls.Sort switch
                {
                    "이름" => list.OrderBy(c => c.Name, StringComparer.Ordinal),
                    "비용" => list.OrderBy(c => c.X ? 99 : c.Cost).ThenByDescending(c => GradeOrder(c.Grade)).ThenBy(c => c.Name, StringComparer.Ordinal),
                    _ => list.OrderByDescending(c => GradeOrder(c.Grade)).ThenBy(c => c.Name, StringComparer.Ordinal),
                };
                foreach (var c in list)
                {
                    keys.Add(c.Id);
                    var id = c.Id;
                    var card = W.Card(content, this, id, cw, "card " + id);
                    var b = card.gameObject.AddComponent<Btn>();
                    b.OnClick = () => { ls.Focus = id; Rebuild(); CardZoom(id); };
                    if (ls.Focus == id) { var fr = Ui.Img(card, Theme.S("frame_thick", 24), Theme.Sky, "focus"); fr.rectTransform.Fill(-3, -3, -3, -3); focusName = c.Name; focusSub = (c.Grade ?? "교주 카드") + " · " + W.TypeLabel(c.Type); }
                    Stage.Hot["dexcard:" + id] = b;
                    if (anim && idx < 30) Tw.Pop(card, 0.012f * idx, 0.88f, 0.28f);
                    idx++;
                }
            }
            if (keys.Count == 0) { var none = Ui.Text(content, "맞는 것이 없습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center); none.Pref(400, 80); }
            ls.Drawn = true;
            if (ls.ScrollY > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                float maxY = Mathf.Max(0, content.rect.height - ((RectTransform)sr.viewport).rect.height);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Min(ls.ScrollY, maxY));
                sr.StopMovement();
            }
            sr.onValueChanged.AddListener(_ => { if (content) ls.ScrollY = content.anchoredPosition.y; });

            // 아래 띠
            var fbg = Ui.Img(host, Theme.White, Theme.NavyPanel.A(0.96f), "footbg", true); fbg.rectTransform.Band(0, footH, 0, 0, 0);
            var line = Ui.Img(host, Theme.White, Theme.Edge.A(0.35f), "rule"); line.rectTransform.Band(0, 1, 0, 0, footH);
            var foot = Ui.Rect("foot", host).Band(0, footH, Theme.Gutter, Theme.Gutter, 0);
            var info = Ui.Text(foot, focusName != null ? $"<b>{focusName}</b>  <color={Theme.SubTag}>{focusSub}</color>" : $"{keys.Count}{(eq ? "개" : "장")} · 누르면 상세 정보", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.rectTransform.Fill(0, 0, 280, 0); info.textWrappingMode = TextWrappingModes.NoWrap; info.overflowMode = TextOverflowModes.Ellipsis;
            var detail = Btn.Make(foot, null, BtnStyle.PillDark, () => { if (ls.Focus == null) return; if (eq) EquipDetail(ls.Focus); else CardZoom(ls.Focus); }, 0, "detail");
            var drt = detail.GetComponent<RectTransform>(); drt.At(1, 0.5f, 0, 0, 250, 58);
            var dzi = Ui.Img(drt, Theme.S("ic_zoom"), Theme.Gold, "ic"); dzi.rectTransform.At(0, 0.5f, 22, 0, 24, 24); dzi.preserveAspect = true;
            var dl = Ui.Title(drt, "상세 정보", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight); dl.rectTransform.Fill(50, 0, 26, 0);
            detail.Interactable = focusName != null; detail.Why = eq ? "장비를 먼저 고르세요" : "카드를 먼저 고르세요";
            Stage.Hot["list.detail"] = detail;
            // 검색 칸 — 이름 · 초성(도감마다 따로 기억)
            DexSearch(tr, tab, content, area, n =>
            {
                if (eq && n.StartsWith("equip ")) { var e = P.Data.Equip(n.Substring(6)); return e != null ? new[] { e.Name } : null; }
                if (!eq && n.StartsWith("card ")) { var c = P.Data.Card(n.Substring(5)); return c != null ? new[] { c.Name } : null; }
                return null;
            }, info, n => $"{n}{(eq ? "개" : "장")} · 누르면 상세 정보");
        }

        /// <summary>장비 한 칸(도감) — 원작 아이콘 · 등급 띠 · 이름 · 종류. 누르면 바로 상세.</summary>
        void EquipTile(RectTransform content, Core.EquipDef e, ItemListState ls, float w, int idx, bool anim, Action rebuild)
        {
            var gc = Theme.GradeOf(e.Grade);
            var b = Btn.Make(content, null, BtnStyle.Ghost, () => { ls.Focus = e.Id; rebuild(); EquipDetail(e.Id); }, 0, "equip " + e.Id);
            b.Bg.sprite = Theme.Round; b.SetColor(Color.Lerp(Theme.NavyWell, gc, 0.18f));
            var rt = b.GetComponent<RectTransform>();
            var glow = Ui.Img(rt, Theme.S("soft"), gc.A(0.35f), "glow"); glow.rectTransform.At(0.5f, 1, 0, -w * 0.05f, w * 1.1f, w * 0.95f);
            var pic = CardArt.Equip(e.Id);
            var ic = Ui.Img(rt, pic ?? W.SlotIcon(e.Slot), pic != null ? Color.white : gc, "icon"); ic.preserveAspect = true;
            float s = pic != null ? w * 0.74f : w * 0.42f;
            ic.rectTransform.At(0.5f, 1, 0, -(w * 0.86f - s) / 2 - 6, s, s);
            var shade = Ui.Img(rt, Theme.S("fade_down"), Color.black.A(0.6f), "shade"); shade.rectTransform.Band(0, w * 0.42f, 2, 2, 2);
            var sl = Ui.Img(rt, W.SlotIcon(e.Slot), Theme.Sub, "slot"); sl.rectTransform.At(0, 1, 8, -8, 20, 20); sl.preserveAspect = true;
            var gp = Ui.Img(rt, Theme.Pill, gc.A(0.9f), "grade"); gp.rectTransform.At(1, 1, -6, -6, 46, 20);
            var gt = Ui.Title(gp.transform, e.Grade, Theme.FsCap - 1, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            var nm = Ui.Title(rt, e.Name, Theme.FsBody, Theme.Ink, TextAlignmentOptions.Bottom); nm.rectTransform.Band(0, 44, 6, 6, 8);
            nm.enableAutoSizing = true; nm.fontSizeMin = 11; nm.fontSizeMax = Theme.FsBody; nm.lineSpacing = -8; nm.Outline(0.2f);
            var band = Ui.Img(rt, Theme.Round, gc, "band"); band.rectTransform.Band(0, 3, 16, 16, 3);
            var rim = Ui.Img(rt, Theme.Frame, gc.A(0.75f), "rim"); rim.rectTransform.Fill();
            if (ls.Focus == e.Id) { var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Sky, "focus"); fr.rectTransform.Fill(-3, -3, -3, -3); }
            Stage.Hot["dexequip:" + e.Id] = b;
            if (anim && idx < 30) Tw.Pop(rt, 0.012f * idx, 0.88f, 0.28f);
        }

        /// <summary>장비 상세 — 왼쪽 큰 그림(등급 빛) · 오른쪽 등급 · 종류 · 능력치 · 효과 · 애착 · 이야기.</summary>
        public void EquipDetail(string id)
        {
            var e = P.Data.Equip(id);
            if (e == null) return;
            var gc = Theme.GradeOf(e.Grade);
            var (body, close, _) = Stage.ModalBox("equipzoom", 1080, Theme.C(620, 640), e.Name, $"{e.Grade} · {e.Slot}");
            float aw = Theme.C(340, 300);
            var well = Ui.Img(body, Theme.Round, Color.Lerp(Theme.NavyWell, gc, 0.22f), "well"); well.rectTransform.At(0, 0.5f, 10, 0, aw, aw * 1.2f);
            var glow = Ui.Img(well.transform, Theme.S("soft"), gc.A(0.5f), "glow"); glow.rectTransform.At(0.5f, 0.55f, 0, 0, aw * 1.2f, aw * 1.2f);
            var pic = CardArt.Equip(id);
            var ic = Ui.Img(well.transform, pic ?? W.SlotIcon(e.Slot), pic != null ? Color.white : gc, "icon"); ic.preserveAspect = true;
            ic.rectTransform.At(0.5f, 0.56f, 0, 0, aw * (pic != null ? 0.82f : 0.45f), aw * (pic != null ? 0.82f : 0.45f));
            var gp = Ui.Img(well.transform, Theme.Pill, gc, "grade"); gp.rectTransform.At(0.5f, 0, 0, 18, 140, 34);
            var gt = Ui.Title(gp.transform, $"{e.Grade} · {e.Slot}", Theme.FsMd, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            var rim = Ui.Img(well.transform, Theme.Frame, gc, "rim"); rim.rectTransform.Fill();
            var right = Ui.Rect("right", body).Fill(aw + 40, 0, 6, 0);
            var content = Ui.Scroll(right, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 10, 4, 10), true, false);
            void Line(string text, float size, Color c)
            {
                var t = Ui.Text(content, text, size, c, TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.Normal;
                t.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            W.Section(content, "능력치", null, 36);
            Line(string.IsNullOrEmpty(W.StatLine(e.Stats)) ? $"<color={Theme.SubTag}>능력치 없음</color>" : $"<color={Theme.GoodTag}>{W.StatLine(e.Stats)}</color>", Theme.FsLg, Theme.Ink);
            if (e.Effect.Count > 0) { W.Section(content, "효과", null, 36); Line(P.Text.Passives(e.Effect), Theme.FsBody, Theme.Ink); }
            if (e.Affinity != null)
            {
                W.Section(content, "애착", "주인 사도가 끼면 더", 36);
                var owner = Roster.OfCore(e.Affinity);
                var orow = Ui.Rect("owner", content); orow.Pref(-1, 56);
                var of = W.Face(orow, owner, 52); of.At(0, 0.5f, 4, 0, 52, 52);
                var on = Ui.Title(orow, $"{owner.ko}  <size=70%><color={Theme.SubTag}>{owner.nature} · {owner.role}</color></size>", Theme.FsLg, Theme.Gold, TextAlignmentOptions.MidlineLeft); on.rectTransform.Fill(68, 0, 0, 0);
                var aff = new List<string>();
                if (e.AffinityStats != null && !string.IsNullOrEmpty(W.StatLine(e.AffinityStats))) aff.Add($"<color={Theme.GoodTag}>{W.StatLine(e.AffinityStats)}</color>");
                if (e.AffinityEffect.Count > 0) aff.Add(P.Text.Passives(e.AffinityEffect));
                Line(aff.Count > 0 ? string.Join("\n", aff) : $"<color={Theme.SubTag}>더 붙는 것 없음</color>", Theme.FsBody, Theme.Ink);
            }
            if (!string.IsNullOrEmpty(e.Blurb)) { W.Section(content, "이야기", null, 36); Line($"<color={Theme.SubTag}>{e.Blurb}</color>", Theme.FsSm, Theme.Sub); }
            Stage.Hot["zoom.close"] = Stage.Hot["modal.x"];
        }
    }
}
