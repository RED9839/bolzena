using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 교주 능력치 — 크레파스 보드(코어 RunCrayon.cs · CrayonStore). 로비 「교주 능력치」 에서 연다.
    //   3열 × 4줄 보드 한 장: 1줄 공격 · 체력 · 방어 · 2줄 치명 확률 · 치명 피해 · 빈칸 · 3줄(상급) · 4줄(최상급).
    //   능력치 칸은 15단계 — 단계가 오를수록 드는 등급이 오른다(1~5 하급 · 6~10 중급 · 11~13 상급 · 14~15 최상급). 칸이 아래부터 그 색으로 차고
    //   아래에 등급 구간 색 막대 · 「지금 / 최대」, 모서리에 다음 단계 등급 아이콘. 그 밖의 칸은 한 번 칠하면 끝.
    //   칸을 누르면 오른쪽에 자세히(지금 · 다음 효과 · 드는 크레파스) · 「칠하기」. 칠하면 칸이 색으로 차오른다.
    //   크레파스 아이콘은 원작 Item_Crayon1~4(RunArt/Item — git 밖), 없으면 직접 그린 막대. 결: 트릭컬 UI(둥근 · 크림 · 갈색 테).
    public partial class Flow
    {
        /// <summary>마지막 판에서 받은 크레파스(등급별 — 끝 화면 표시).</summary>
        public int[] LastCrayons = new int[4];
        /// <summary>등급 색 — 원작 아이콘 색(하급 베이지 · 중급 파랑 · 상급 보라 · 최상급 금).</summary>
        public static readonly Color[] PastelColors = { Theme.Hex("D8B583"), Theme.Hex("4FA8F0"), Theme.Hex("9B55E8"), Theme.Hex("FFB21E") };
        int crayonSel = -1;

        /// <summary>크레파스 아이콘 — 원작 Item_Crayon(1~4), 없으면 비스듬한 등급 색 막대 · 크림 종이 띠 · 짙은 심.</summary>
        public static RectTransform PastelIcon(Transform parent, int tier, float d, string name = "pastel")
        {
            tier = Mathf.Clamp(tier, 0, 3);
            var holder = Ui.Rect(name, parent);
            holder.sizeDelta = new Vector2(d, d);
            var art = Theme.Art("Item/Item_Crayon" + (tier + 1));
            if (art != null) { var im = Ui.Img(holder, art, Color.white, "art"); im.rectTransform.Fill(); im.preserveAspect = true; return holder; }
            var c = PastelColors[tier];
            var stick = Ui.Rect("stick", holder);
            stick.anchorMin = stick.anchorMax = stick.pivot = new Vector2(0.5f, 0.5f);
            stick.sizeDelta = new Vector2(d * 0.34f, d * 0.92f);
            stick.localRotation = Quaternion.Euler(0, 0, -35);
            var edge = Ui.Img(stick, Theme.S("pill", 46), GradeBrownDeep.A(0.85f), "edge"); edge.rectTransform.Fill(-d * 0.04f, -d * 0.04f, -d * 0.04f, -d * 0.04f);
            var body = Ui.Img(stick, Theme.S("pill", 46), c, "body"); body.rectTransform.Fill();
            var tip = Ui.Img(stick, Theme.S("circle"), Color.Lerp(c, Color.black, 0.25f), "tip"); tip.rectTransform.At(0.5f, 1, 0, -d * 0.02f, d * 0.24f, d * 0.24f);
            var paper = Ui.Img(stick, Theme.White, GradeCream, "paper");
            paper.rectTransform.anchorMin = new Vector2(0, 0.28f); paper.rectTransform.anchorMax = new Vector2(1, 0.6f); paper.rectTransform.offsetMin = paper.rectTransform.offsetMax = Vector2.zero;
            return holder;
        }

        /// <summary>등급 아이콘 + 글 — 줄 배치 안에 넣는다.</summary>
        static void PastelCount(Transform row, int tier, string text, Color tc, float d)
        {
            var cell = Ui.Rect("t" + tier, row); cell.Pref(-1, d);
            Ui.Row(cell, 2, TextAnchor.MiddleLeft, null, false, false);
            var ic = PastelIcon(cell, tier, d); ic.Pref(d, d);
            var t = Ui.Title(cell, text, Theme.FsMd, tc, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.Pref(t.preferredWidth + 4, d);
            var fit = cell.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>크림 알약(갈색 테) — 바탕은 줄 배치에서 뺀다.</summary>
        static RectTransform CreamPill(Transform parent, string name)
        {
            var rim = Ui.Img(parent, Theme.S("pill", 46), GradeBrown, name).rectTransform;
            var body = Ui.Img(rim, Theme.S("pill", 46), GradeCream, "body"); body.rectTransform.Fill(3, 3, 3, 3);
            body.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return rim;
        }

        public void CrayonScreen()
        {
            Stage.SetBg("stage1_1", 0.55f);
            Stage.Show("crayon", BuildCrayon, 1.0f);
        }

        void BuildCrayon(RectTransform root)
        {
            var T = CrayonStore.Table; var Sv = CrayonStore.Save;
            var size = Stage.Size;
            bool compact = Theme.Compact;
            if (T.Cells.Count == 0) { Toast.Show("크레파스 표가 없습니다"); return; }
            if (crayonSel < 0 || crayonSel >= T.Cells.Count || T.Cells[crayonSel].Blank)
            {
                int pick = T.Cells.FindIndex(c => !c.Blank && Crayon.WhyNot(T, Sv, c.Id) == null);
                crayonSel = pick >= 0 ? pick : T.Cells.FindIndex(c => !c.Blank);
            }

            var back = Btn.Icon(root, Theme.S("ic_back"), Lobby, 56, "back");
            back.GetComponent<RectTransform>().At(0, 1, Theme.Gutter, -14, 56, 56);
            Stage.Hot["crayon.back"] = back;
            var title = Ui.Title(root, "교주 능력치", Theme.FsXl, Theme.Ink, TextAlignmentOptions.Top);
            title.rectTransform.Band(1, 40, 300, 300, -12); title.Outline(0.2f);
            var sub = Ui.Text(root, "크레파스로 보드를 칠하면 모든 모험에 계속 붙습니다 — 크레파스는 모험이 끝날 때 받습니다", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Top);
            sub.rectTransform.Band(1, 24, 300, 300, -54); sub.Outline(0.2f);
            sub.textWrappingMode = TextWrappingModes.NoWrap; sub.overflowMode = TextOverflowModes.Ellipsis;

            // 가진 크레파스
            var inv = CreamPill(root, "inventory");
            inv.At(0.5f, 1, 0, -88, 10, 52);
            Ui.Row(inv, 20, TextAnchor.MiddleCenter, new RectOffset(26, 26, 2, 2), false, false);
            inv.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lab = Ui.Title(inv, "가진 크레파스", Theme.FsSm, GradeBrown, TextAlignmentOptions.MidlineLeft); lab.textWrappingMode = TextWrappingModes.NoWrap; lab.Pref(lab.preferredWidth + 4, 40);
            for (int i = 0; i < 4; i++) PastelCount(inv, i, $"{Crayon.TIERS[i]} <b>{Sv.Have[i]}</b>", GradeBrownDeep, 40);
            Tw.Rise(inv, 0.05f, 12, 0.35f, Vector2.up);

            // 보드 — 갈색 테 · 크림 판 위 3×4 칸(줄 왼쪽에 그 줄 등급 아이콘)
            int cols = Mathf.Max(1, T.Cols), rows = (T.Cells.Count + cols - 1) / cols;
            float top = 152, bottom = compact ? 16 : 26, gap = 10;
            float bh = size.y - top - bottom;
            float cellH = (bh - 28 - gap * (rows - 1)) / rows, cellW = Mathf.Min(cellH * 1.45f, 220);
            float bw = cellW * cols + gap * (cols - 1) + 28;
            float panelW = Mathf.Min(520, size.x - Theme.Gutter * 2 - bw - 24);
            float x0 = (size.x - (bw + 24 + panelW)) / 2;
            var board = Ui.Img(root, Theme.S("round", 20), GradeBrown, "board").rectTransform;
            board.At(0, 1, x0, -top, bw, bh); board.pivot = new Vector2(0, 1);
            var paper = Ui.Img(board, Theme.S("round", 20), GradeCream, "paper"); paper.rectTransform.Fill(5, 5, 5, 5);
            var cellImgs = new Image[T.Cells.Count];
            for (int i = 0; i < T.Cells.Count; i++)
            {
                var c = T.Cells[i]; int ii = i;
                int cx = i % cols, cy = i / cols;
                var pos = new Vector2(14 + cx * (cellW + gap), -(14 + cy * (cellH + gap)));
                if (c.Blank)
                {
                    var bl = Ui.Img(board, Theme.S("round", 20), GradeBrown.A(0.18f), "blank"); bl.rectTransform.At(0, 1, pos.x, pos.y, cellW, cellH); bl.rectTransform.pivot = new Vector2(0, 1);
                    var xx = Ui.Img(bl.transform, Theme.S("ic_x"), GradeBrown.A(0.35f), "x"); xx.rectTransform.At(0.5f, 0.5f, 0, 0, cellH * 0.4f, cellH * 0.4f); xx.preserveAspect = true;
                    continue;
                }
                int lv = Sv.LevelOf(c.Id); bool done = lv >= c.Levels, sel = i == crayonSel;
                // 칸 색 = 마지막으로 칠한 단계의 등급(안 칠했으면 첫 단계) · 모서리 아이콘 = 다음 단계에 드는 등급
                int tierNow = c.Costs[Mathf.Clamp(lv - 1, 0, c.Levels - 1)].Tier, tierNext = c.Costs[Mathf.Clamp(lv, 0, c.Levels - 1)].Tier;
                var tc = PastelColors[tierNow];
                var b = Btn.Make(board, null, BtnStyle.Ghost, () => { crayonSel = ii; Stage.Show("crayon", BuildCrayon, 0.1f, true); }, 0, "cell" + i);
                var brt = b.GetComponent<RectTransform>(); brt.At(0, 1, pos.x, pos.y, cellW, cellH); brt.pivot = new Vector2(0, 1);
                var rim = Ui.Img(brt, Theme.S("round", 20), sel ? Theme.Hex("E8762B") : Color.Lerp(tc, GradeBrown, 0.35f), "rim"); rim.rectTransform.Fill(sel ? -3 : 0, sel ? -3 : 0, sel ? -3 : 0, sel ? -3 : 0);
                var well = Ui.Img(brt, Theme.S("round", 20), Theme.Hex("FFFBF2"), "well"); well.rectTransform.Fill(4, 4, 4, 4);
                // 칠한 몫 — 아래부터 크레파스 색으로 찬다
                var fill = Ui.Img(brt, Theme.S("round", 20), Color.Lerp(tc, Color.white, 0.35f), "fill"); fill.rectTransform.Fill(4, 4, 4, 4);
                fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Vertical; fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                fill.fillAmount = (float)lv / c.Levels;
                cellImgs[i] = fill;
                var ic = PastelIcon(brt, tierNext, 30, "tier"); ic.anchorMin = ic.anchorMax = ic.pivot = new Vector2(1, 1); ic.anchoredPosition = new Vector2(-6, -6);
                var nm = Ui.Title(brt, c.Name, compact ? Theme.FsMd : Theme.FsLg, GradeBrownDeep, TextAlignmentOptions.TopLeft);
                nm.rectTransform.Fill(12, cellH * 0.45f, 36, 8); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                string Short(double v) => Crayon.EffectOf(c.Kind, v).Replace("모든 사도 ", "").Replace("파티 ", "");
                var vt = Ui.Text(brt, lv > 0 ? Short(c.V * lv) : c.Levels > 1 ? $"단계마다 {Short(c.V)}" : Short(c.V), Theme.FsSm, GradeBrown, TextAlignmentOptions.TopLeft);
                vt.rectTransform.Fill(12, 22, 8, cellH * 0.5f); vt.enableAutoSizing = true; vt.fontSizeMin = 10; vt.fontSizeMax = Theme.FsSm * Settings.TextScale; vt.lineSpacing = -4;
                // 아래 — 단계 막대(등급 구간마다 그 색) · 「지금 / 최대」 · 칠함
                if (c.Levels > 1)
                {
                    float barW = cellW - 78, segW = barW / c.Levels;
                    var bar = Ui.Rect("lvbar", brt).At(0, 0, 12, 12, barW, 10); bar.pivot = new Vector2(0, 0);
                    var bg = Ui.Img(bar, Theme.S("pill", 46), GradeBrown.A(0.18f), "bg"); bg.rectTransform.Fill();
                    for (int k = 0; k < lv; k++)
                    {
                        var seg = Ui.Img(bar, Theme.White, Color.Lerp(PastelColors[c.Costs[k].Tier], Color.black, 0.12f), "s" + k);
                        seg.rectTransform.At(0, 0.5f, k * segW, 0, segW - 1, 10); seg.rectTransform.pivot = new Vector2(0, 0.5f);
                    }
                    var lt = Ui.Title(brt, $"{lv}/{c.Levels}", Theme.FsSm, GradeBrownDeep, TextAlignmentOptions.BottomRight);
                    lt.rectTransform.Fill(0, 6, 10, 0); lt.textWrappingMode = TextWrappingModes.NoWrap;
                }
                else if (done) { var ck = Ui.Title(brt, "칠함 ✓", Theme.FsSm, Color.Lerp(tc, Color.black, 0.35f), TextAlignmentOptions.BottomLeft); ck.rectTransform.Fill(12, 6, 8, 0); }
                Stage.Hot["crayon.cell" + i] = b;
            }
            Tw.Pop(board, 0.05f, 0.92f, 0.35f);

            // 오른쪽 — 고른 칸 자세히 · 칠하기
            var cs = T.Cells[Mathf.Clamp(crayonSel, 0, T.Cells.Count - 1)];
            var panel = Ui.Img(root, Theme.S("round", 20), GradeBrown, "detail").rectTransform;
            panel.At(0, 1, x0 + bw + 24, -top, panelW, bh); panel.pivot = new Vector2(0, 1);
            var pb = Ui.Img(panel, Theme.S("round", 20), GradeCream, "body"); pb.rectTransform.Fill(4, 4, 4, 4);
            int slv = Sv.LevelOf(cs.Id); bool sdone = slv >= cs.Levels;
            var nk = Crayon.NextCost(cs, Sv);
            int bigTier = nk != null ? nk.Tier : cs.Costs[cs.Levels - 1].Tier;
            var big = PastelIcon(panel, bigTier, 64, "tier"); big.anchorMin = big.anchorMax = big.pivot = new Vector2(0, 1); big.anchoredPosition = new Vector2(18, -16);
            var hn = Ui.Title(panel, cs.Name, Theme.FsXl, GradeBrownDeep, TextAlignmentOptions.MidlineLeft); hn.rectTransform.At(0, 1, 92, -14, panelW - 104, 40); hn.rectTransform.pivot = new Vector2(0, 1);
            string ladder = string.Join(" · ", Enumerable.Range(0, 4).Select(t => { var ks = Enumerable.Range(0, cs.Levels).Where(k => cs.Costs[k].Tier == t).ToList(); return ks.Count == 0 ? null : ks.Count == 1 ? $"{ks[0] + 1} {Crayon.TIERS[t]}" : $"{ks.First() + 1}~{ks.Last() + 1} {Crayon.TIERS[t]}"; }).Where(x => x != null));
            var ht = Ui.Text(panel, cs.Levels > 1 ? $"{slv} / {cs.Levels}단계 · 단계마다 등급 — {ladder}" : $"{Crayon.TIERS[bigTier]} 크레파스 칸 · {(sdone ? "칠함" : "한 번 칠하면 끝")}", Theme.FsSm, GradeBrown, TextAlignmentOptions.MidlineLeft);
            ht.enableAutoSizing = true; ht.fontSizeMin = 11; ht.fontSizeMax = Theme.FsSm * Settings.TextScale;
            ht.rectTransform.At(0, 1, 92, -52, panelW - 104, 26); ht.rectTransform.pivot = new Vector2(0, 1);
            string now = slv > 0 ? Crayon.EffectOf(cs.Kind, cs.V * slv) : "없음";
            string nxt = sdone ? "다 칠했습니다" : Crayon.EffectOf(cs.Kind, cs.V * (slv + 1));
            string bt = ColorUtility.ToHtmlStringRGB(GradeBrown);
            var dt = Ui.Text(panel, $"<color=#{bt}>지금</color>  {now}\n<color=#{bt}>{(sdone ? "" : "칠하면")}</color>  <b>{nxt}</b>", Theme.FsMd, GradeBrownDeep, TextAlignmentOptions.TopLeft);
            dt.rectTransform.Fill(22, 150, 18, 98); dt.enableAutoSizing = true; dt.fontSizeMin = 12; dt.fontSizeMax = Theme.FsMd * Settings.TextScale;
            int need = nk?.N ?? 0;
            bool can = nk != null && Sv.Have[nk.Tier] >= need;
            if (!sdone)
            {
                var costRow = Ui.Rect("cost", panel).At(0.5f, 0, 0, 86, panelW - 40, 46); costRow.pivot = new Vector2(0.5f, 0);
                Ui.Row(costRow, 8, TextAnchor.MiddleCenter, null, false, false);
                var cl = Ui.Text(costRow, "드는 크레파스", Theme.FsSm, GradeBrown, TextAlignmentOptions.MidlineRight); cl.textWrappingMode = TextWrappingModes.NoWrap; cl.Pref(cl.preferredWidth + 4, 40);
                PastelCount(costRow, nk.Tier, $"{Crayon.TIERS[nk.Tier]} ×{need}  <size=80%><color=#{(can ? "3E8E3E" : "C0504D")}>(가진 것 {Sv.Have[nk.Tier]})</color></size>", GradeBrownDeep, 44);
            }
            var paintBtn = Btn.Make(panel, sdone ? "다 칠함" : "칠하기", can ? BtnStyle.PillGold : BtnStyle.PillDark, () =>
            {
                var why = CrayonStore.Paint(cs.Id);
                if (why != null) { Toast.Show(why); return; }
                Sfx.Play("win");
                var fill = cellImgs[T.Cells.IndexOf(cs)];
                float from = fill ? fill.fillAmount : 0, to = (float)CrayonStore.Save.LevelOf(cs.Id) / cs.Levels;
                if (fill)
                {
                    var gc = PastelColors[bigTier];
                    fill.color = Color.Lerp(gc, Color.white, 0.35f);
                    var glow = Ui.Img(fill.transform.parent, Theme.S("soft"), gc.A(0), "paintglow"); glow.rectTransform.Fill(-24, -24, -24, -24);
                    Tw.Run(glow, 0.6f, k => { if (glow) glow.color = gc.A(0.7f * Mathf.Sin(k * Mathf.PI)); }, Tw.Linear);
                }
                Tw.Run(fill, Settings.ReduceMotion ? 0.12f : 0.55f, k => { if (fill) fill.fillAmount = Mathf.Lerp(from, to, k); }, Tw.OutCubic, 0, () => Tw.After(0.3f, () => Stage.Show("crayon", BuildCrayon, 0.1f, true)));
            }, Theme.FsLg, "paint");
            paintBtn.GetComponent<RectTransform>().At(0.5f, 0, 0, 20, Mathf.Min(280, panelW - 40), 58);
            if (!can) paintBtn.SetColor(new Color(1, 1, 1, 0.75f));
            Stage.Hot["crayon.paint"] = paintBtn;
            Tw.Rise(panel, 0.1f, 20, 0.35f, Vector2.right);
        }

        /// <summary>끝 화면 — 「받은 크레파스」 등급별(「로비로」 왼쪽).</summary>
        void EndCrayon(RectTransform root)
        {
            var got = LastCrayons ?? new int[4];
            var rim = CreamPill(root, "crayons");
            rim.At(0.5f, 0, -190, Theme.C(34, 22) + Theme.C(70, 64) / 2, 10, 54); rim.pivot = new Vector2(1, 0.5f);
            Ui.Row(rim, 14, TextAnchor.MiddleCenter, new RectOffset(22, 22, 2, 2), false, false);
            rim.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lab = Ui.Title(rim, "받은 크레파스", Theme.FsMd, GradeBrownDeep, TextAlignmentOptions.MidlineLeft); lab.textWrappingMode = TextWrappingModes.NoWrap; lab.Pref(lab.preferredWidth + 4, 40);
            for (int i = 0; i < 4; i++) PastelCount(rim, i, $"+{got[i]}", got[i] > 0 ? GradeBrownDeep : GradeBrown.A(0.45f), 40);
            Tw.Pop(rim, 0.9f, 0.6f, 0.4f);
        }
    }
}
