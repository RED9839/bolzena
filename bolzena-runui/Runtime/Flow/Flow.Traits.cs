using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 고학년 · 고유 효과 · 패시브 글 — 「자세히」 없이 한 가지 글(2026-10-05 사용자: 「자세히 보기를 없애고 수치를 직관적으로」 · core Docs/설명글.md).
    //   core CardText.Traits(사도) 가 칸을 만든다: 이름 · 부제(작은 글) · 본문(「계기 → 결과」 줄 목록, 수치 · 대상 · 지속 · 제한이 다 든다).
    //   고학년 · 고유 효과 · 패시브 세 칸은 늘 따로(2026-10-06 사용자 「패시브 칸 따로」) — 고유 효과 = 자원 · 표식 자체의 규칙, 패시브 = passives 전부(없으면 「없음」).
    //   · 줄 배치 안(파티 정보 · 사도 한 장): FullText — 글 하나를 그대로(넘치면 담은 칸이 스크롤).
    //   · 크기가 정해진 칸(편성 큰 카드 · 주인 고르기 · 파티 정보): TraitsChip — 작은 알약을 누르면 낱말 판(TermPop)을 세로로 쌓아 띄운다.
    //   글 속 낱말(키워드 · 생성 카드)은 여기서도 밑줄 · 설명 판(CardTerms.OfText).
    public partial class Flow
    {
        /// <summary>
        /// 사도(core id)의 고학년 → 고유 효과 → 패시브를 낱말 판 목록으로 — 처음부터 다 보이는 글.
        /// 사도 상세 카드 탭 오른쪽 판(Flow.Roster TraitsPanel)과 같은 글이다(core CardText.Traits).
        /// </summary>
        public List<CardTerms.Term> HeroTraitTerms(string coreId)
        {
            var d = coreId != null ? P.Data.Hero(coreId) : null;
            var o = new List<CardTerms.Term>();
            if (d == null) return o;
            foreach (var t in P.Text.Traits(d))
                o.Add(new CardTerms.Term
                {
                    Name = t.Name,
                    Body = CardTerms.WithSub(t.Kind == "고학년" ? null : t.Sub, t.Body),
                    Hero = t.Kind == "고유 효과",
                    Kind = t.Kind == "고학년" ? $"고학년 · {t.Sub}" : t.Kind,
                });
            return o;
        }

        /// <summary>anchor 옆에 낱말 판 여럿을 세로로 띄운다.</summary>
        public void PopTerms(RectTransform anchor, IList<CardTerms.Term> list)
        {
            if (anchor == null || list == null || list.Count == 0) return;
            var c = new Vector3[4];
            anchor.GetWorldCorners(c);
            var cam = Stage.Canvas.renderMode != RenderMode.ScreenSpaceOverlay ? Stage.Canvas.worldCamera : null;
            var sp = RectTransformUtility.WorldToScreenPoint(cam, (c[1] + c[2]) / 2);   // 위 가운데
            TermPop.ShowAt(Stage.ToastLayer, sp, list, (p, id, w) => W.Card(p, this, id, w, "termcard"), cam);
        }

        /// <summary>작은 알약 「ⓘ 고유 효과」 — 누르면 그 사도의 고학년 · 고유 효과 · 패시브 판을 세로로(글 전부 · 길면 판 안에서 스크롤).</summary>
        public Btn TraitsChip(Transform parent, string coreId, string label = "고유 효과", string name = "traits")
        {
            var b = Btn.Make(parent, null, BtnStyle.PillDark, null, 0, name);
            var rt = b.GetComponent<RectTransform>();
            var ic = Ui.Img(rt, Theme.S("ic_info"), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 12, 0, 18, 18);
            var t = Ui.Title(rt, label, Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Fill(36, 0, 12, 0); t.textWrappingMode = TextWrappingModes.NoWrap;
            b.Label = t;
            float w = t.GetPreferredValues(label).x + 52;
            rt.sizeDelta = new Vector2(w, 34);
            b.Pref(w, 34);
            b.OnClick = () => { if (TermPop.IsOpen) TermPop.Close(); else PopTerms(rt, HeroTraitTerms(coreId)); };
            return b;
        }

        /// <summary>
        /// 줄 배치(세로 Col) 안에 글 하나 — 수치가 다 든 글을 그대로 보인다(「자세히」 없음). heroId 를 주면 글 속 그 사도 고유 효과 이름을 잡는다.
        /// terms=false 면 밑줄 · 설명 판 없이(소개 글).
        /// </summary>
        public TextMeshProUGUI FullText(RectTransform col, string text, float size, Color color, string heroId = null, string name = "fulltext", bool terms = true)
        {
            var t = Ui.Text(col, "", size, color, TextAlignmentOptions.TopLeft, false, name);
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 2;
            string s = text ?? "";
            var found = terms ? CardTerms.OfText(P.Data, P.Text, s, heroId) : new List<CardTerms.Term>();
            TermPop.MarkAndAttach(t, s, found, Stage.ToastLayer, (p, id, w) => W.Card(p, this, id, w, "termcard"));
            if (found.Count == 0) t.raycastTarget = false;
            LayoutRebuilder.MarkLayoutForRebuild(col);
            return t;
        }
    }
}
