using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 학점제 학년(코어 RunGrade.cs · Grades) — 파티 전체가 함께 오른다. 원작에 학년별 외형이 없으니 스킨은 그대로, 배지 · 테 · 오라로만 보인다.
    //   지도: 머리 띠 왼쪽 파티 알약 밑에 학년 알약(둥근 배지 + 「N학년」 + 다음 학년까지 학점 막대). 누르면 학년 보상 표.
    //   진급: 지도에 돌아온 순간 짧은 연출(배지가 옛 학년 → 새 학년으로 뒤집히고 그 학년의 몫 안내). 졸업(6학년)은 금빛 테 · 오라.
    //   결: 트릭컬 UI(둥근 · 크림 바탕 · 갈색 테).
    public partial class Flow
    {
        public static readonly Color GradeCream = Theme.Hex("FFF4DC"), GradeBrown = Theme.Hex("7A4E2A"), GradeBrownDeep = Theme.Hex("4A2C14"),
            GradeFill = Theme.Hex("F5A93B"), GradeWell = Theme.Hex("E8D3AE");

        /// <summary>
        /// 학년 배지 — 갈색 테 · 크림 속 동그라미에 학년 숫자(졸업은 「졸」), 아래 작은 「학년」. 졸업이면 금빛 테 + 뒤에 숨쉬는 오라.
        /// 지도 알약 · 진급 연출이 같은 것을 쓴다. 돌려줌: 배지(부모 안 크기 d).
        /// </summary>
        public static RectTransform GradeBadge(Transform parent, int grade, float d, string name = "gradeBadge")
        {
            bool grad = grade >= Core.Grades.GRAD;
            var holder = Ui.Rect(name, parent);
            holder.sizeDelta = new Vector2(d, d);
            if (grad)
            {
                var aura = Ui.Img(holder, Theme.S("soft"), Theme.Gold.A(0.55f), "aura");
                aura.rectTransform.At(0.5f, 0.5f, 0, 0, d * 2.1f, d * 2.1f);
                Tw.Pulse(aura, 0.35f, 0.75f, 1.6f);
            }
            var rim = Ui.Img(holder, Theme.S("circle"), grad ? Theme.GoldDeep : GradeBrown, "rim");
            rim.rectTransform.Fill();
            var inner = Ui.Img(holder, Theme.S("circle"), GradeCream, "inner");
            inner.rectTransform.Fill(d * 0.09f, d * 0.09f, d * 0.09f, d * 0.09f);
            if (grad)
            {
                var ring = Ui.Img(holder, Theme.S("ring"), Theme.Gold, "goldring");
                ring.rectTransform.Fill(-d * 0.06f, -d * 0.06f, -d * 0.06f, -d * 0.06f);
            }
            var num = Ui.Title(holder, grad ? "졸업" : grade.ToString(), grad ? d * 0.3f : d * 0.5f, grad ? Theme.GoldDeep : GradeBrownDeep, TextAlignmentOptions.Center, "num");
            num.rectTransform.Fill(0, grad ? 0 : d * 0.22f, 0, grad ? 0 : d * 0.02f);
            num.textWrappingMode = TextWrappingModes.NoWrap; num.overflowMode = TextOverflowModes.Overflow;
            var cap = Ui.Title(holder, grad ? "" : "학년", d * 0.2f, GradeBrown, TextAlignmentOptions.Center, "cap");
            cap.rectTransform.At(0.5f, 0, 0, d * 0.13f, d, d * 0.26f);
            cap.textWrappingMode = TextWrappingModes.NoWrap; cap.overflowMode = TextOverflowModes.Overflow;
            return holder;
        }

        /// <summary>지도 머리 띠 — 파티 알약 밑 학년 알약(배지 · 학년 이름 · 학점 막대 · 숫자). 누르면 학년 보상 표.</summary>
        void MapGrade(RectTransform root)
        {
            var run = P.Run;
            if (run == null) return;
            int g = run.Grade;
            var (from, to) = run.GradeSpan;
            bool grad = g >= Core.Grades.GRAD;
            float w = 300, h = 38;
            var outer = Ui.Img(root, Theme.S("pill", 46), grad ? Theme.GoldDeep : GradeBrown, "hud.grade", true).rectTransform;
            outer.At(0, 1, Theme.Gutter + 26, -82, w, h);
            var body = Ui.Img(outer, Theme.S("pill", 46), GradeCream, "body"); body.rectTransform.Fill(3, 3, 3, 3);
            var badge = GradeBadge(outer, g, 52, "badge");
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = new Vector2(-w / 2 + 4, 0);
            var name = Ui.Title(outer, Core.Grades.Name(g), Theme.FsMd, GradeBrownDeep, TextAlignmentOptions.MidlineLeft, "gname");
            name.rectTransform.At(0, 0.5f, 34, 1, 74, h);
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Overflow;
            // 학점 막대 — 갈색 우물 위 귤빛 채움(졸업이면 가득 · 금빛)
            float bx = 110, bw = 120;
            var well = Ui.Img(outer, Theme.S("pill", 46), GradeWell, "well"); well.rectTransform.At(0, 0.5f, bx, 0, bw, 12);
            float k = grad ? 1 : Mathf.Clamp01((run.S.Credits - from) / (float)Mathf.Max(1, to - from));
            var fill = Ui.Img(well.transform, Theme.S("pill", 46), grad ? Theme.Gold : GradeFill, "fill");
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.08f, k), 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            if (k <= 0) fill.color = fill.color.A(0);
            var num = Ui.Title(outer, grad ? $"학점 {run.S.Credits}" : $"{run.S.Credits}<size=75%><color=#{ColorUtility.ToHtmlStringRGB(GradeBrown)}> / {to}</color></size>",
                Theme.FsSm, GradeBrownDeep, TextAlignmentOptions.MidlineRight, "credits");
            num.rectTransform.At(1, 0.5f, -12, 1, 64, h);
            num.textWrappingMode = TextWrappingModes.NoWrap; num.overflowMode = TextOverflowModes.Overflow;
            var b = outer.gameObject.AddComponent<Btn>();
            b.Bg = body;
            b.SetColor(GradeCream);
            b.OnClick = GradeTable;
            Stage.Hot["grade"] = b;
            Tw.Rise(outer, 0.12f, 14, 0.4f, Vector2.left);
        }

        /// <summary>보상 화면 「전투 끝」 칩 밑 — 이번 싸움 학점 「+N 학점」 과 지금 학년 막대(진급 연출은 지도에 돌아와서).</summary>
        void RewardCredit(RectTransform root, RectTransform under)
        {
            var run = P.Run;
            if (run == null || run.S.LastCredit <= 0) return;
            var (from, to) = run.GradeSpan;
            bool grad = run.Grade >= Core.Grades.GRAD;
            float w = 220, h = 40;
            var outer = Ui.Img(root, Theme.S("pill", 46), grad ? Theme.GoldDeep : GradeBrown, "credit").rectTransform;
            outer.At(0, 0.5f, Theme.Gutter + 8 + 14, under.anchoredPosition.y - 64, w, h);
            var body = Ui.Img(outer, Theme.S("pill", 46), GradeCream, "body"); body.rectTransform.Fill(3, 3, 3, 3);
            var badge = GradeBadge(outer, run.Grade, 50, "badge");
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(0.5f, 0.5f); badge.anchoredPosition = new Vector2(-w / 2 + 4, 0);
            var t = Ui.Title(outer, $"학점 +{run.S.LastCredit}  <size=80%><color=#{ColorUtility.ToHtmlStringRGB(GradeBrown)}>{(grad ? $"누적 {run.S.Credits}" : $"{run.S.Credits} / {to}")}</color></size>",
                Theme.FsMd, GradeBrownDeep, TextAlignmentOptions.MidlineLeft, "credittext");
            t.rectTransform.Fill(34, 0, 10, 0);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            Tw.Rise(outer, 0.3f, 20, 0.4f, Vector2.left);
        }

        /// <summary>학년 보상 표 — 학년마다 학점 · 받는 것. 지금 학년을 칠한다.</summary>
        void GradeTable()
        {
            var run = P.Run;
            if (run == null) return;
            var layer = Ui.Rect("modal grade", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0, 0, 0, 0.55f), "dim", true); dim.rectTransform.Fill();
            var close = dim.gameObject.AddComponent<Btn>(); close.OnClick = () => { if (layer) Destroy(layer.gameObject); };
            float pw = 640, ph = Mathf.Min(560, Stage.Size.y - 60);
            var frame = Ui.Img(layer, Theme.S("round", 20), GradeBrown, "frame", true).rectTransform;
            frame.At(0.5f, 0.5f, 0, 0, pw, ph);
            var paper = Ui.Img(frame, Theme.S("round", 20), GradeCream, "paper"); paper.rectTransform.Fill(5, 5, 5, 5);
            var head = Ui.Title(frame, $"학점제 학년 — 지금 {Core.Grades.Name(run.Grade)} · 학점 {run.S.Credits}", Theme.FsLg, GradeBrownDeep, TextAlignmentOptions.Center);
            head.rectTransform.Band(1, 40, 20, 20, -14);
            var sub = Ui.Text(frame, $"이긴 싸움마다 학점 — 일반 {Core.Grades.CreditOf("fight")} · 엘리트 {Core.Grades.CreditOf("elite")} · 층 보스 {Core.Grades.CreditOf("boss")}. 파티 전체가 함께 진급합니다.", Theme.FsSm, GradeBrown, TextAlignmentOptions.Center);
            sub.rectTransform.Band(1, 24, 20, 20, -54);
            float rowH = Mathf.Min(64, (ph - 100) / (Core.Grades.MAX - 1));
            for (int g = Core.Grades.FIRST + 1; g <= Core.Grades.MAX; g++)
            {
                bool now = g == run.Grade, got = g <= run.Grade;
                var row = Ui.Img(frame, Theme.S("round", 20), now ? GradeFill.A(0.28f) : got ? GradeWell.A(0.6f) : GradeWell.A(0.25f), "row" + g).rectTransform;
                row.Band(1, rowH - 6, 22, 22, -88 - (g - 2) * rowH);
                var bd = GradeBadge(row, g, rowH - 12, "b");
                bd.anchorMin = bd.anchorMax = bd.pivot = new Vector2(0, 0.5f); bd.anchoredPosition = new Vector2(8, 0);
                var need = Ui.Title(row, $"{Core.Grades.Need(g)}학점", Theme.FsMd, GradeBrownDeep, TextAlignmentOptions.MidlineLeft);
                need.rectTransform.At(0, 0.5f, rowH + 4, 0, 80, rowH);
                var perks = Core.Grades.PerksAt(g);
                var t = Ui.Text(row, string.Join("\n", perks.Skip(1).DefaultIfEmpty(perks.FirstOrDefault())), Theme.FsSm, GradeBrownDeep, TextAlignmentOptions.MidlineLeft);
                t.rectTransform.Fill(rowH + 92, 0, 12, 0);
                t.lineSpacing = -4;
                t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = Theme.FsSm * Settings.TextScale;
            }
            var foot = Ui.Text(frame, $"진급마다 파티 공격 · 방어 · 최대 HP +{Mathf.RoundToInt((float)Core.Grades.STAT_PCT * 100)}%(사도 기본치 기준, 누적)", Theme.FsCap, GradeBrown, TextAlignmentOptions.Center);
            foot.rectTransform.Band(0, 24, 20, 20, 8);
            Tw.Pop(frame, 0, 0.85f, 0.3f);
        }

        /// <summary>
        /// 진급 연출 — 판이 쌓아 둔 새 학년(Run.PopGradeNews)을 차례로: 어두운 막 위에 옛 배지가 뒤집혀 새 배지로, 「진급!」 · 그 학년의 몫.
        /// 누르면(또는 잠시 뒤) 다음 소식 → 다 보이면 then. 졸업은 금빛 오라 · 「졸업!」.
        /// </summary>
        public void GradeUp(List<int> news, Action then = null)
        {
            if (news == null || news.Count == 0) { then?.Invoke(); return; }
            int g = news[0];
            var rest = news.Skip(1).ToList();
            bool grad = g >= Core.Grades.GRAD;
            var layer = Ui.Rect("modal gradeup", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0.03f, 0.02f, 0.01f, 0), "dim", true); dim.rectTransform.Fill();
            Tw.Run(dim, 0.25f, k => { if (dim) dim.color = new Color(0.03f, 0.02f, 0.01f, 0.6f * k); });
            // 가운데 크림 판(갈색 테)
            float pw = Mathf.Min(560, Stage.Size.x - 40), ph = Mathf.Min(grad ? 440 : 400, Stage.Size.y - 40);
            var frame = Ui.Img(layer, Theme.S("round", 20), grad ? Theme.GoldDeep : GradeBrown, "frame").rectTransform;
            frame.At(0.5f, 0.5f, 0, 0, pw, ph);
            var paper = Ui.Img(frame, Theme.S("round", 20), GradeCream, "paper"); paper.rectTransform.Fill(5, 5, 5, 5);
            // 배지 — 옛 학년이 납작해졌다가 새 학년으로 펴진다
            float d = 150;
            var spot = Ui.Rect("spot", frame).At(0.5f, 1, 0, -18 - d / 2 - 6, d, d);
            spot.pivot = new Vector2(0.5f, 0.5f);
            var oldB = GradeBadge(spot, g - 1, d, "old"); oldB.anchorMin = oldB.anchorMax = oldB.pivot = new Vector2(0.5f, 0.5f); oldB.anchoredPosition = Vector2.zero;
            var newB = GradeBadge(spot, g, d, "new"); newB.anchorMin = newB.anchorMax = newB.pivot = new Vector2(0.5f, 0.5f); newB.anchoredPosition = Vector2.zero;
            newB.localScale = new Vector3(0, 1, 1);
            Tw.Run(oldB, 0.22f, k => { if (oldB) oldB.localScale = new Vector3(1 - k, 1, 1); }, null, 0.35f, () =>
            {
                if (oldB) oldB.gameObject.SetActive(false);
                Tw.Run(newB, 0.32f, k => { if (newB) { float s = Tw.OutBack(k); newB.localScale = new Vector3(s, s, 1); } });
                Sfx.Play("win");
            });
            var title = Ui.Title(frame, grad ? "졸업!" : $"진급! {Core.Grades.Name(g)}", Theme.Fs2xl, grad ? Theme.GoldDeep : GradeBrownDeep, TextAlignmentOptions.Center, "gtitle");
            title.rectTransform.Band(1, 52, 20, 20, -d - 34);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var perks = Core.Grades.PerksAt(g);
            var lines = Ui.Text(frame, string.Join("\n", perks.Select(p => "· " + p)), Theme.FsMd, GradeBrownDeep, TextAlignmentOptions.Top, false, "perks");
            lines.rectTransform.Fill(36, 70, 36, d + 92);
            lines.enableAutoSizing = true; lines.fontSizeMin = 12; lines.fontSizeMax = Theme.FsMd * Settings.TextScale;
            Tw.Rise(lines.rectTransform, 0.6f, 14, 0.35f);
            bool gone = false;
            void Next()
            {
                if (gone) return; gone = true;
                var cg = layer.Group();
                Tw.Run(layer, 0.2f, k => { if (cg) cg.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); GradeUp(rest, then); });
            }
            var ok = Btn.Make(frame, rest.Count > 0 ? "다음" : "좋아요", BtnStyle.PillGold, Next, Theme.FsLg, "gradeok");
            ok.GetComponent<RectTransform>().At(0.5f, 0, 0, 16, 220, 52);
            Stage.Hot["grade.ok"] = ok;
            var tap = dim.gameObject.AddComponent<Btn>(); tap.OnClick = Next;
            Tw.Pop(frame, 0.05f, 0.7f, 0.4f);
            if (Demo.Active) Tw.After(Settings.ReduceMotion ? 1f : 2.6f, () => { if (layer) Next(); });
        }
    }
}
