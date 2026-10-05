using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 짧은 글 / 자세히(2026-10 사용자: 「기본은 한 줄, 누르면 자세히」) — core CardText.Short · ShortPassives · HeroShort = 기본, Detail · Hero = 펼침.
    //   · 줄 배치 안(사도 상세 고유 효과 탭 · 파티 정보 · 사도 한 장): ShortMore — 짧은 글 + 「자세히 ▼」, 누르면 그 자리에서 자세한 글로 바뀐다.
    //   · 크기가 정해진 칸(상세 카드 탭의 고학년 · 고유 효과 칸 · 편성 큰 카드 · 주인 고르기): TraitsChip — 작은 알약을 누르면
    //     낱말 판(TermPop)을 세로로 쌓아 띄운다(고유 효과 · 패시브 · 고학년, 판마다 「자세히」).
    //   글 속 낱말(키워드 · 생성 카드)은 여기서도 밑줄 · 설명 판(CardTerms.OfText).
    public partial class Flow
    {
        /// <summary>사도(core id)의 고유 효과 · 패시브 · 고학년을 낱말 판 목록으로(짧은 글 = 본문, 자세히 = 펼침).</summary>
        public List<CardTerms.Term> HeroTraitTerms(string coreId)
        {
            var d = coreId != null ? P.Data.Hero(coreId) : null;
            var o = new List<CardTerms.Term>();
            if (d == null) return o;
            foreach (var k in d.AllKeywords)
                o.Add(new CardTerms.Term { Name = k.Name, Body = P.Text.Short(k), Detail = P.Text.Detail(k), Hero = true, Kind = "고유 효과" });
            string last = null;
            foreach (var r in d.Passives)
            {
                if (r.Name != null && r.Name == last) { var prev = o[o.Count - 1]; prev.Detail = (prev.Detail ?? prev.Body) + "\n" + P.Text.Detail(r); continue; }
                o.Add(new CardTerms.Term { Name = r.Name ?? "패시브", Body = P.Text.Short(r), Detail = P.Text.Detail(r), Kind = "패시브" });
                last = r.Name;
            }
            if (d.Ult != null)
                o.Add(new CardTerms.Term { Name = d.Ult.Name, Body = P.Text.Short(d.Ult), Detail = P.Text.Detail(d.Ult), Kind = $"고학년 · {d.Ult.Cost}%" });
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

        /// <summary>작은 알약 「ⓘ 고유 효과」 — 누르면 그 사도의 고유 효과 · 패시브 · 고학년 판을 세로로(짧은 글 + 자세히).</summary>
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
        /// 줄 배치(세로 Col) 안에 짧은 글 + 「자세히 ▼」 — 누르면 그 자리에서 자세한 글로 바뀐다(다시 누르면 접기).
        /// 자세한 글이 없거나 같으면 단추 없이 짧은 글만. heroId 를 주면 글 속 그 사도 고유 효과 이름을 잡는다. terms=false 면 밑줄 · 설명 판 없이(소개 글).
        /// </summary>
        public TextMeshProUGUI ShortMore(RectTransform col, string shortText, string detail, float size, Color color, string heroId = null, string name = "shortmore", bool open = false, bool terms = true)
        {
            var box = Ui.Rect(name, col);
            Ui.Col(box, 4, TextAnchor.UpperLeft, null, true, false);
            var t = Ui.Text(box, "", size, color, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
            bool more = open;
            bool has = !string.IsNullOrEmpty(detail) && detail != shortText;
            Btn chip = null;
            void Set()
            {
                string s = more && has ? detail : shortText ?? "";
                var found = terms ? CardTerms.OfText(P.Data, P.Text, s, heroId) : new List<CardTerms.Term>();
                TermPop.MarkAndAttach(t, s, found, Stage.ToastLayer, (p, id, w) => W.Card(p, this, id, w, "termcard"));
                if (found.Count == 0) t.raycastTarget = false;
                t.color = more ? Theme.Sub : color;
                if (chip != null && chip.Label != null) chip.Label.text = more ? "접기 ▲" : "자세히 ▼";
                LayoutRebuilder.MarkLayoutForRebuild(col);
            }
            if (has)
            {
                var row = Ui.Rect("morerow", box); row.Pref(-1, 30);
                Ui.Row(row, 0, TextAnchor.MiddleLeft, null, false, false);
                chip = TermPop.MoreChip(row, more);
                chip.OnClick = () => { more = !more; Set(); };
            }
            Set();
            return t;
        }
    }
}
