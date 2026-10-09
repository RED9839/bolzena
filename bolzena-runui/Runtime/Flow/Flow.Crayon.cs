using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 교주 보드 — 크레파스 보드(코어 RunCrayon.cs · CrayonStore). 로비 「교주 보드」 에서 연다.
    //   3열 × 4줄 보드 한 장: 1줄 공격 · 체력 · 방어 · 2줄 치명 확률 · 치명 피해 · 빈칸 · 3 · 4줄 모험 혜택 칸(용돈 주머니 · 별점 · 할인권 · 예습 노트 · 클로버 · 보물 지도).
    //   모든 칸이 여러 단계(능력치 15단계 · 모험 혜택 3~5단계 — 단계 수 · 등급은 crayon.json)이고, 단계가 오를수록 드는 등급이 오른다.
    //   줄마다 등급을 따로 표시하지 않는다 — 칸마다 구석에 「다음 단계에 드는 등급」 하나만.
    //   칸 모양(2026-10-09 사용자 「보드 아이콘 통일」): 12칸 모두 같은 틀(크림 칸 · 갈색 테) · 왼쪽 위에 그 능력의 원작 아이콘(RunArt/Board — BoardAbility) ·
    //   아래 같은 진행 막대 · 「지금 / 최대」 · 오른쪽 아래 구석에 다음 단계에 드는 크레파스 하나(등급은 그것으로만 — 칸 색은 바꾸지 않는다).
    //   칠한 몫은 등급과 상관없이 한 가지 크레파스빛으로 아래부터 차고, 다 칠한 칸은 「칠함」 도장.
    //   칸을 누르면 오른쪽에 자세히(능력 아이콘 + 구석 등급 · 지금 · 다음 효과 · 드는 크레파스) · 「칠하기」.
    //   크레파스는 원작 Item_Crayon1~4 만(RunArt/Item — git 밖). 「교주 보드」 대표 아이콘은 Theme.BoardIcon 하나. 결: 트릭컬 UI(둥근 · 크림 · 갈색 테).
    public partial class Flow
    {
        /// <summary>마지막 판에서 받은 크레파스(등급별 — 끝 화면 표시).</summary>
        public int[] LastCrayons = new int[4];
        /// <summary>등급 색 — 원작 아이콘 색(하급 베이지 · 중급 파랑 · 상급 보라 · 최상급 금).</summary>
        public static readonly Color[] PastelColors = { Theme.Hex("D8B583"), Theme.Hex("4FA8F0"), Theme.Hex("9B55E8"), Theme.Hex("FFB21E") };
        int crayonSel = -1;

        /// <summary>크레파스 아이콘 — 원작 Item_Crayon(1~4)만 쓴다(2026-10-09 사용자 「트릭컬 크레용 아이콘 써 줘, 임의 이미지 쓰지 마」).
        /// 원작 그림이 없으면(복사 전) 빈칸 + 경고 한 번 — 직접 그린 막대로 대신하지 않는다.</summary>
        public static RectTransform PastelIcon(Transform parent, int tier, float d, string name = "pastel")
        {
            tier = Mathf.Clamp(tier, 0, 3);
            var holder = Ui.Rect(name, parent);
            holder.sizeDelta = new Vector2(d, d);
            var art = PastelArt(tier);
            if (art != null) { var im = Ui.Img(holder, art, Color.white, "art"); im.rectTransform.Fill(); im.preserveAspect = true; }
            return holder;
        }

        /// <summary>칠한 몫 색 — 등급과 상관없이 한 가지(크레파스빛 귤색, 옅게).</summary>
        static readonly Color BoardPaint = Theme.CreamFill.A(0.2f);

        static bool boardWarned;
        /// <summary>보드 칸 능력 아이콘 — 원작 그림(RunArt/Board/칸 종류 — Tools~/copy_assets.py 의 BOARD 표). 없으면 빈칸 + 경고 한 번.</summary>
        public static RectTransform BoardAbility(Transform parent, string kind, float d, string name = "ability")
        {
            var holder = Ui.Rect(name, parent);
            holder.sizeDelta = new Vector2(d, d);
            var art = Theme.Art("Board/" + kind);
            if (art != null) { var im = Ui.Img(holder, art, Color.white, "art"); im.rectTransform.Fill(); im.preserveAspect = true; }
            else if (!boardWarned) { boardWarned = true; Debug.LogWarning($"[Crayon] 보드 능력 아이콘 없음(RunArt/Board/{kind} — Tools~/copy_assets.py)"); }
            return holder;
        }

        static bool pastelWarned;
        /// <summary>원작 크레파스 그림(RunArt/Item/Item_Crayon1~4 — Tools~/copy_assets.py). 없으면 null(경고 한 번).</summary>
        public static Sprite PastelArt(int tier)
        {
            var art = Theme.Art("Item/Item_Crayon" + (Mathf.Clamp(tier, 0, 3) + 1));
            if (art == null && !pastelWarned) { pastelWarned = true; Debug.LogWarning("[Crayon] 원작 크레파스 그림 없음(RunArt/Item/Item_Crayon1~4 — Tools~/copy_assets.py)"); }
            return art;
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
            var rim = Ui.Img(parent, Theme.S("pill", 46), Theme.CreamRim, name).rectTransform;
            var body = Ui.Img(rim, Theme.S("pill", 46), Theme.Cream, "body"); body.rectTransform.Fill(3, 3, 3, 3);
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
            var title = Ui.Title(root, "교주 보드", Theme.FsXl, Theme.Ink, TextAlignmentOptions.Top);
            title.rectTransform.Band(1, 40, 300, 300, -12); title.Outline(0.2f);
            { var bi = Ui.Img(root, Theme.BoardIcon, Color.white, "boardicon"); bi.preserveAspect = true; bi.rectTransform.At(0.5f, 1, -title.preferredWidth / 2 - 26, -12, 40, 40); bi.rectTransform.pivot = new Vector2(0.5f, 1); }
            var sub = Ui.Text(root, "크레파스로 보드를 칠하면 모든 모험에 계속 붙습니다 — 크레파스는 모험이 끝날 때 받습니다", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Top);
            sub.rectTransform.Band(1, 24, 300, 300, -54); sub.Outline(0.2f);
            sub.textWrappingMode = TextWrappingModes.NoWrap; sub.overflowMode = TextOverflowModes.Ellipsis;

            // 가진 크레파스
            var inv = CreamPill(root, "inventory");
            inv.At(0.5f, 1, 0, -88, 10, 52);
            Ui.Row(inv, 20, TextAnchor.MiddleCenter, new RectOffset(26, 26, 2, 2), false, false);
            inv.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lab = Ui.Title(inv, "가진 크레파스", Theme.FsSm, Theme.CreamRim, TextAlignmentOptions.MidlineLeft); lab.textWrappingMode = TextWrappingModes.NoWrap; lab.Pref(lab.preferredWidth + 4, 40);
            for (int i = 0; i < 4; i++) PastelCount(inv, i, $"{Crayon.TIERS[i]} <b>{Sv.Have[i]}</b>", Theme.CreamInk, 40);
            Tw.Rise(inv, 0.05f, 12, 0.35f, Vector2.up);

            // 보드 — 갈색 테 · 크림 판 위 3×4 칸(등급은 칸 구석의 다음 단계 크레파스로만)
            int cols = Mathf.Max(1, T.Cols), rows = (T.Cells.Count + cols - 1) / cols;
            float top = 152, bottom = compact ? 16 : 26, gap = 10;
            float bh = size.y - top - bottom;
            float cellH = (bh - 28 - gap * (rows - 1)) / rows, cellW = Mathf.Min(cellH * 1.45f, 220);
            float bw = cellW * cols + gap * (cols - 1) + 28;
            float panelW = Mathf.Min(520, size.x - Theme.Gutter * 2 - bw - 24);
            float x0 = (size.x - (bw + 24 + panelW)) / 2;
            var board = Ui.Img(root, Theme.S("round", 20), Theme.CreamRim, "board").rectTransform;
            board.At(0, 1, x0, -top, bw, bh); board.pivot = new Vector2(0, 1);
            var paper = Ui.Img(board, Theme.S("round", 20), Theme.Cream, "paper"); paper.rectTransform.Fill(5, 5, 5, 5);
            var cellImgs = new Image[T.Cells.Count];
            for (int i = 0; i < T.Cells.Count; i++)
            {
                var c = T.Cells[i]; int ii = i;
                int cx = i % cols, cy = i / cols;
                var pos = new Vector2(14 + cx * (cellW + gap), -(14 + cy * (cellH + gap)));
                if (c.Blank)
                {
                    var bl = Ui.Img(board, Theme.S("round", 20), Theme.CreamRim.A(0.18f), "blank"); bl.rectTransform.At(0, 1, pos.x, pos.y, cellW, cellH); bl.rectTransform.pivot = new Vector2(0, 1);
                    var xx = Ui.Img(bl.transform, Theme.S("ic_x"), Theme.CreamRim.A(0.35f), "x"); xx.rectTransform.At(0.5f, 0.5f, 0, 0, cellH * 0.4f, cellH * 0.4f); xx.preserveAspect = true;
                    continue;
                }
                int lv = Sv.LevelOf(c.Id); bool done = lv >= c.Levels, sel = i == crayonSel;
                int tierNext = c.Costs[Mathf.Clamp(lv, 0, c.Levels - 1)].Tier;
                var b = Btn.Make(board, null, BtnStyle.Ghost, () => { crayonSel = ii; Stage.Show("crayon", BuildCrayon, 0.1f, true); }, 0, "cell" + i);
                var brt = b.GetComponent<RectTransform>(); brt.At(0, 1, pos.x, pos.y, cellW, cellH); brt.pivot = new Vector2(0, 1);
                // 같은 틀 — 갈색 테 · 크림 칸(고른 칸만 귤빛 굵은 테)
                var rim = Ui.Img(brt, Theme.S("round", 20), sel ? Theme.Hex("E8762B") : Theme.CreamRim, "rim"); rim.rectTransform.Fill(sel ? -3 : 0, sel ? -3 : 0, sel ? -3 : 0, sel ? -3 : 0);
                var well = Ui.Img(brt, Theme.S("round", 20), Theme.Hex("FFFBF2"), "well"); well.rectTransform.Fill(3, 3, 3, 3);
                // 칠한 몫 — 등급과 상관없이 같은 크레파스빛으로 아래부터
                var fill = Ui.Img(brt, Theme.S("round", 20), BoardPaint, "fill"); fill.rectTransform.Fill(3, 3, 3, 3);
                fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Vertical; fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                fill.fillAmount = (float)lv / c.Levels;
                cellImgs[i] = fill;
                // 위 — 능력 아이콘 · 이름
                float icd = Mathf.Min(cellH * 0.36f, 46);
                var ab = BoardAbility(brt, c.Kind, icd); ab.At(0, 1, 10, -8, icd, icd);
                var nm = Ui.Title(brt, c.Name, compact ? Theme.FsMd : Theme.FsLg, Theme.CreamInk, TextAlignmentOptions.MidlineLeft);
                nm.rectTransform.At(0, 1, 16 + icd, -8, cellW - icd - 26, icd); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                string Short(double v) => Crayon.EffectOf(c.Kind, v).Replace("모든 사도 ", "").Replace("파티 ", "");
                var vt = Ui.Text(brt, lv > 0 ? Short(c.V * lv) : c.Levels > 1 ? $"단계마다 {Short(c.V)}" : Short(c.V), Theme.FsSm, Theme.CreamRim, TextAlignmentOptions.TopLeft);
                vt.rectTransform.Fill(12, 30, done ? 66 : 8, icd + 14); vt.enableAutoSizing = true; vt.fontSizeMin = 10; vt.fontSizeMax = Theme.FsSm * Settings.TextScale; vt.lineSpacing = -4;
                // 아래 — 칸마다 같은 진행 막대 · 「지금 / 최대」 · 구석 크레파스(다음 단계 등급) 또는 「칠함」 도장
                float barW = cellW - 12 - 92;
                var bar = Ui.Rect("lvbar", brt).At(0, 0, 12, 13, barW, 9); bar.pivot = new Vector2(0, 0);
                var bg = Ui.Img(bar, Theme.S("pill", 46), Theme.CreamRim.A(0.18f), "bg"); bg.rectTransform.Fill();
                if (lv > 0)
                {
                    var bf = Ui.Img(bar, Theme.S("pill", 46), Theme.CreamFill, "on");
                    bf.rectTransform.anchorMin = Vector2.zero; bf.rectTransform.anchorMax = new Vector2((float)lv / c.Levels, 1); bf.rectTransform.offsetMin = bf.rectTransform.offsetMax = Vector2.zero;
                }
                var lt = Ui.Title(brt, $"{lv}/{c.Levels}", Theme.FsSm, Theme.CreamInk, TextAlignmentOptions.BottomRight);
                lt.rectTransform.At(1, 0, -38, 6, 52, 22); lt.textWrappingMode = TextWrappingModes.NoWrap;
                if (!done) { var tic = PastelIcon(brt, tierNext, 26, "tier"); tic.At(1, 0, -7, 5, 26, 26); }
                else
                {
                    var st = Ui.Img(brt, Theme.S("ring"), Theme.StampRed.A(0.85f), "painted").rectTransform; st.At(1, 0.5f, -10, -2, 52, 52); st.localRotation = Quaternion.Euler(0, 0, -14);   // 오른쪽 가운데(이름 · 막대를 가리지 않게)
                    var stt = Ui.Title(st, "칠함", Theme.FsSm, Theme.StampRed, TextAlignmentOptions.Center); stt.rectTransform.Fill();
                }
                Stage.Hot["crayon.cell" + i] = b;
            }
            Tw.Pop(board, 0.05f, 0.92f, 0.35f);

            // 오른쪽 — 고른 칸 자세히 · 칠하기
            var cs = T.Cells[Mathf.Clamp(crayonSel, 0, T.Cells.Count - 1)];
            var panel = Ui.Img(root, Theme.S("round", 20), Theme.CreamRim, "detail").rectTransform;
            panel.At(0, 1, x0 + bw + 24, -top, panelW, bh); panel.pivot = new Vector2(0, 1);
            var pb = Ui.Img(panel, Theme.S("round", 20), Theme.Cream, "body"); pb.rectTransform.Fill(4, 4, 4, 4);
            int slv = Sv.LevelOf(cs.Id); bool sdone = slv >= cs.Levels;
            var nk = Crayon.NextCost(cs, Sv);
            int bigTier = nk != null ? nk.Tier : cs.Costs[cs.Levels - 1].Tier;
            var big = BoardAbility(panel, cs.Kind, 64, "ability"); big.At(0, 1, 18, -16, 64, 64);
            var bigTierIc = PastelIcon(panel, bigTier, 30, "tier"); bigTierIc.At(0, 1, 18 + 64 - 20, -16 - 64 + 22, 30, 30);
            var hn = Ui.Title(panel, cs.Name, Theme.FsXl, Theme.CreamInk, TextAlignmentOptions.MidlineLeft); hn.rectTransform.At(0, 1, 92, -14, panelW - 104, 40); hn.rectTransform.pivot = new Vector2(0, 1);
            string ladder = string.Join(" · ", Enumerable.Range(0, 4).Select(t => { var ks = Enumerable.Range(0, cs.Levels).Where(k => cs.Costs[k].Tier == t).ToList(); return ks.Count == 0 ? null : ks.Count == 1 ? $"{ks[0] + 1} {Crayon.TIERS[t]}" : $"{ks.First() + 1}~{ks.Last() + 1} {Crayon.TIERS[t]}"; }).Where(x => x != null));
            var ht = Ui.Text(panel, $"{slv} / {cs.Levels}단계{(sdone ? " · 다 칠함" : "")} · 단계마다 등급 — {ladder}", Theme.FsSm, Theme.CreamRim, TextAlignmentOptions.MidlineLeft);   // 모든 칸 같은 꼴(단계 수는 표 — crayon.json)
            ht.enableAutoSizing = true; ht.fontSizeMin = 11; ht.fontSizeMax = Theme.FsSm * Settings.TextScale;
            ht.rectTransform.At(0, 1, 92, -52, panelW - 104, 26); ht.rectTransform.pivot = new Vector2(0, 1);
            string now = slv > 0 ? Crayon.EffectOf(cs.Kind, cs.V * slv) : "아직 칠하지 않았습니다";
            string nxt = sdone ? "다 칠했습니다" : Crayon.EffectOf(cs.Kind, cs.V * (slv + 1));
            string bt = ColorUtility.ToHtmlStringRGB(Theme.CreamRim);
            var dt = Ui.Text(panel, $"<color=#{bt}>지금</color>  {now}\n<color=#{bt}>{(sdone ? "" : "칠하면")}</color>  <b>{nxt}</b>", Theme.FsMd, Theme.CreamInk, TextAlignmentOptions.TopLeft);
            dt.rectTransform.Fill(22, 150, 18, 98); dt.enableAutoSizing = true; dt.fontSizeMin = 12; dt.fontSizeMax = Theme.FsMd * Settings.TextScale;
            int need = nk?.N ?? 0;
            bool can = nk != null && Sv.Have[nk.Tier] >= need;
            if (!sdone)
            {
                var costRow = Ui.Rect("cost", panel).At(0.5f, 0, 0, 86, panelW - 40, 46); costRow.pivot = new Vector2(0.5f, 0);
                Ui.Row(costRow, 8, TextAnchor.MiddleCenter, null, false, false);
                var cl = Ui.Text(costRow, "드는 크레파스", Theme.FsSm, Theme.CreamRim, TextAlignmentOptions.MidlineRight); cl.textWrappingMode = TextWrappingModes.NoWrap; cl.Pref(cl.preferredWidth + 4, 40);
                PastelCount(costRow, nk.Tier, $"{Crayon.TIERS[nk.Tier]} ×{need}  <size=80%><color=#{(can ? "3E8E3E" : "C0504D")}>(가진 것 {Sv.Have[nk.Tier]})</color></size>", Theme.CreamInk, 44);
            }
            var paintBtn = Btn.Make(panel, sdone ? "완료" : "칠하기", can ? BtnStyle.PillGold : BtnStyle.PillDark, () =>
            {
                var why = CrayonStore.Paint(cs.Id);
                if (why != null) { Toast.Show(why); return; }
                Sfx.Play("win");
                var fill = cellImgs[T.Cells.IndexOf(cs)];
                float from = fill ? fill.fillAmount : 0, to = (float)CrayonStore.Save.LevelOf(cs.Id) / cs.Levels;
                if (fill)
                {
                    var gc = Theme.CreamFill;
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

        /// <summary>진행 코드 창의 칸(데모 · 시험이 읽는다).</summary>
        public TMP_InputField CodeShowField { get; private set; }
        public TMP_InputField CodeInField { get; private set; }

        /// <summary>코드 입력 칸 — 남색 판 · 여러 줄 · 웹은 HTML 입력이 겹쳐 붙여넣기 · 길게 눌러 복사가 된다(Kit/WebInput.cs).</summary>
        TMP_InputField CodeField(RectTransform parent, string placeholder, string text, float h)
        {
            var bg = Ui.Img(parent, Theme.S("round", 20), Theme.NavyWell, "codefield", true);
            bg.gameObject.SetActive(false);
            bg.rectTransform.Fill();
            var area = Ui.Rect("text area", bg.transform).Fill(16, 8, 16, 8);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Ui.Text(area, placeholder, Theme.FsSm, Theme.Dim, TextAlignmentOptions.TopLeft, false, "placeholder"); ph.rectTransform.Fill();
            var tx = Ui.Text(area, "", Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft, false, "text"); tx.rectTransform.Fill();
            tx.textWrappingMode = TextWrappingModes.Normal; tx.overflowMode = TextOverflowModes.Overflow; tx.richText = false;
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area; input.textComponent = tx; input.placeholder = ph;
            input.fontAsset = Theme.Body; input.pointSize = tx.fontSize;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.richText = false; input.characterLimit = 4000;
            input.customCaretColor = true; input.caretColor = Theme.Gold; input.caretWidth = 2;
            input.selectionColor = Theme.Gold.A(0.35f);
            input.shouldHideMobileInput = false;
            input.targetGraphic = bg;
            input.text = text ?? "";
            bg.gameObject.SetActive(true);
            WebInput.Attach(input);
            return input;
        }

        /// <summary>
        /// 진행 코드 창(설정 → 「진행 코드」) — 위: 내 코드(보이고 · 고를 수 있고) · 「복사」. 아래: 코드 붙여 넣는 칸 · 「클립보드에서」 · 「불러오기」.
        /// 웹은 브라우저 클립보드(WebClipboard — 실패하면 다음 터치 때 한 번 더, 그래도 안 되면 칸을 길게 눌러 직접 복사). 빈 보드여도 올바른 코드가 나온다.
        /// </summary>
        public void ProgressCodePanel()
        {
            var size = Stage.Size;
            float w = Mathf.Min(1000, size.x - 48), h = Mathf.Min(620, size.y - 24);
            var (body, close, foot) = Stage.ModalBox("progresscode", w, h, "진행 코드", "교주 보드 · 편성 프리셋 · 완주 기록을 다른 기기 · 브라우저로 옮깁니다(소리 · 화면 설정은 빼고)", true, null, 0);
            string code = CrayonStore.Export();
            var inner = Ui.Rect("inner", body).Fill(24, 48, 24, 8);
            RectTransform Half(string n, float y0, float y1) { var r = Ui.Rect(n, inner); r.anchorMin = new Vector2(0, y0); r.anchorMax = new Vector2(1, y1); r.offsetMin = r.offsetMax = Vector2.zero; return r; }
            var top = Half("mine", 0.52f, 1); var bot = Half("load", 0, 0.48f);
            // 내 코드
            var t1 = Ui.Title(top, "내 진행 코드", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); t1.rectTransform.Band(1, 30, 36, 0, 0);
            { var ci = Ui.Img(top, Theme.BoardIcon, Color.white, "boardicon"); ci.preserveAspect = true; ci.rectTransform.At(0, 1, 0, 0, 30, 30); }
            var showBox = Ui.Rect("show", top).Fill(0, 0, 200, 34);
            CodeShowField = CodeField(showBox, "", code, 0);
            var copy = Btn.Make(top, "복사", BtnStyle.PillGold, () =>
            {
                var now = CrayonStore.Export();
                if (CodeShowField) CodeShowField.text = now;
                WebClipboard.Copy(now, r =>
                {
                    if (r == "copied") Toast.Show("진행 코드를 복사했습니다");
                    else if (r == "retry") Toast.Show("화면을 한 번 더 누르면 복사합니다", 3f);
                    else Toast.Show("복사하지 못했습니다 — 위 칸을 길게 눌러(또는 Ctrl+A · Ctrl+C) 직접 복사하세요", 4f);
                });
            }, Theme.FsLg, "copy");
            copy.GetComponent<RectTransform>().At(1, 0.5f, 0, -17, 180, 60);
            Stage.Hot["code.copy"] = copy;
            // 불러오기
            var t2 = Ui.Title(bot, "코드 불러오기", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); t2.rectTransform.Band(1, 30, 0, 0, 0);
            var inBox = Ui.Rect("in", bot).Fill(0, 0, 200, 34);
            CodeInField = CodeField(inBox, "BZP1- 로 시작하는 코드를 여기에 붙여 넣으세요", "", 0);
            var paste = Btn.Make(bot, "클립보드에서", BtnStyle.PillDark, () => WebClipboard.Paste(t =>
            {
                if (string.IsNullOrWhiteSpace(t)) { Toast.Show("클립보드를 읽지 못했습니다 — 칸을 길게 눌러 붙여 넣으세요", 3.5f); return; }
                if (CodeInField) CodeInField.text = t.Trim();
            }), Theme.FsMd, "paste");
            paste.GetComponent<RectTransform>().At(1, 1, 0, -34, 180, 52);
            Stage.Hot["code.paste"] = paste;
            var load = Btn.Make(bot, "불러오기", BtnStyle.PillGold, () =>
            {
                var c = CodeInField ? CodeInField.text.Trim() : "";
                if (c.Length == 0) { Toast.Show("코드를 먼저 붙여 넣으세요"); return; }
                var chk = Crayon.ImportAll(c, CrayonStore.Table);
                if (chk.why != null) { Toast.Show("받지 않았습니다 — " + chk.why, 3.5f); return; }
                string party = chk.extra == null ? "편성 프리셋 · 완주 기록은 그대로(옛 코드)" : $"편성 프리셋 {Enumerable.Range(1, 5).Count(i => chk.extra.ContainsKey("preset" + i))}칸 · 완주 기록까지";
                Confirm("지금 진행을 덮어쓰겠습니까?", $"크레파스 {string.Join(" · ", chk.save.Have.Select((n, i) => Crayon.TIERS[i] + " " + n))} · 칠한 단계 {chk.save.Level.Values.Sum()} · {party}. 이 코드의 내용으로 바뀌고, 지금 진행은 사라집니다.", "덮어쓰기", () =>
                {
                    var why = CrayonStore.Import(c);
                    Toast.Show(why ?? "진행을 불러왔습니다");
                    if (why == null && CodeShowField) CodeShowField.text = CrayonStore.Export();
                }, true);
            }, Theme.FsLg, "load");
            load.GetComponent<RectTransform>().At(1, 0, 0, 0, 180, 60);
            Stage.Hot["code.load"] = load;
            var hint = Ui.Text(body, "코드에는 크레파스 · 칠한 단계 · 편성 프리셋 · 완주 기록과 체크섬이 들어 있습니다. 틀린 코드는 받지 않습니다.", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
            hint.rectTransform.Band(0, 30, 24, 24, 10);
            hint.textWrappingMode = TextWrappingModes.NoWrap; hint.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>끝 화면 — 「받은 크레파스」 등급별(「로비로」 왼쪽).</summary>
        void EndCrayon(RectTransform root)
        {
            var got = LastCrayons ?? new int[4];
            var rim = CreamPill(root, "crayons");
            rim.At(0.5f, 0, -190, Theme.C(34, 22) + Theme.C(70, 64) / 2, 10, 54); rim.pivot = new Vector2(1, 0.5f);
            Ui.Row(rim, 14, TextAnchor.MiddleCenter, new RectOffset(22, 22, 2, 2), false, false);
            rim.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bic = Ui.Img(rim, Theme.BoardIcon, Color.white, "boardicon"); bic.preserveAspect = true; bic.Pref(38, 38);
            var lab = Ui.Title(rim, "받은 크레파스", Theme.FsMd, Theme.CreamInk, TextAlignmentOptions.MidlineLeft); lab.textWrappingMode = TextWrappingModes.NoWrap; lab.Pref(lab.preferredWidth + 4, 40);
            for (int i = 0; i < 4; i++) PastelCount(rim, i, $"+{got[i]}", got[i] > 0 ? Theme.CreamInk : Theme.CreamRim.A(0.45f), 40);
            Tw.Pop(rim, 0.9f, 0.6f, 0.4f);
        }
    }
}
