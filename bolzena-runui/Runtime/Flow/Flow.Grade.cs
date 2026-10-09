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
    //   모양(2026-10-09 사용자 결정 — G2 「칭찬 스티커 공책」): 지도 HUD · 보상 화면 학점 · 진급 · 졸업이 같은 공책 한 장(GradeNotebook)을 크기만 바꿔 쓴다.
    //   지도: 파티 알약 밑 공책(「N학년 칭찬 스티커」 · 「N장 더 → 진급」 · 학점 칸 스티커 · 끝 칸 선물 상자). 누르면 학년 보상 표. 걸린 진급 보상은 마스킹테이프 쪽지.
    //   보상 화면: 이번 싸움 학점만큼 스티커가 「착」. 진급 · 졸업: 마지막 스티커 「착!」 → 선물 상자 열림 → 원작 「참!잘햇어요.」 도장 쾅 → 보상 고르기(졸업은 졸업장 쪽지). 졸업 HUD 끝 칸은 열린 선물 상자.
    //   배지(GradeBadge)는 학년 보상 표가 쓴다(전투 HUD 배지는 bolzena-unity PartyHud 제 것).
    public partial class Flow
    {
        // 크림 판 색 — 판 화면 공용(Theme.Cream · CreamRim · CreamInk · CreamFill · CreamWell)과 같은 값
        public static readonly Color GradeCream = Theme.Cream, GradeBrown = Theme.CreamRim, GradeBrownDeep = Theme.CreamInk,
            GradeFill = Theme.CreamFill, GradeWell = Theme.CreamWell;

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

        // ── 칭찬 스티커 공책(2026-10-09 사용자 결정 — G2) ── 지도 HUD · 보상 화면 학점 · 진급 · 졸업이 같은 공책 한 장을 크기만 바꿔 쓴다.
        //   스프링 고리 · 빨간 여백 줄 · 파란 공책 줄 · 「N학년 칭찬 스티커」 · 학점 칸 = 볼따구 스티커(받은 칸) / 점선 동그라미(빈 칸) · 끝 칸 선물 상자(다음 학년).
        //   그림은 모두 원작(2026-10-09 사용자 — 직접 그린 gr_* 를 걷어냄): RunArt/Ui/grade/(미션 도장 스티커 · 미션 빈 칸 · 앨범 선물 상자 · 반짝이 · 별 · 미션 집게 · 앨범 테이프 · 잉클 메모 종이 — Tools~/copy_assets.py GRADE_ART)
        //   + 「참!잘햇어요.」 도장(RunArt/Ui/perfect_stamp) · 교단 증명서(RunArt/Ui/order_cert). 색: Theme.NotePaper · NoteRule · NoteMargin · NoteSub · StampRed · BubbleRim.
        /// <summary>공책 원작 조각(RunArt/Ui/grade/이름 — Tools~/copy_assets.py 의 GRADE_ART). 없으면 null(경고 한 번 — 대신 그리지 않는다).</summary>
        static Sprite GArt(string name)
        {
            var s = Theme.Art("Ui/grade/" + name);
            if (s == null && !gartWarned) { gartWarned = true; Debug.LogWarning($"[Grade] 학년 공책 원작 조각 없음(RunArt/Ui/grade/{name} — Tools~/copy_assets.py)"); }
            return s;
        }
        static bool gartWarned;
        static Sprite GArt(string name, float border) => Theme.Art("Ui/grade/" + name, border) ?? GArt(name);
        /// <summary>학점 스티커 — 원작 미션 도장(사도 SD 얼굴) 12장을 칸마다 돌려 쓴다.</summary>
        static Sprite Sticker(int i) => GArt($"sticker{((i % 12) + 12) % 12 + 1:00}");
        /// <summary>메모 종이 9칸 테두리(원작 InkleLobby_Book_Memo 601×380 의 말린 모서리 · 테두리 무늬).</summary>
        const float MemoBorder = 84;
        static float StickerTilt(int i) => i % 2 == 0 ? -10 : 8;

        /// <summary>원작 「참!잘햇어요.」 도장(생활 재료 Icon_PerfectStamp — Tools~/copy_assets.py → RunArt/Ui/perfect_stamp). 없으면 null(경고 한 번).</summary>
        static Sprite PerfectStamp
        {
            get
            {
                var s = Theme.Art("Ui/perfect_stamp");
                if (s == null && !stampWarned) { stampWarned = true; Debug.LogWarning("[Grade] 원작 「참 잘했어요」 도장 그림 없음(RunArt/Ui/perfect_stamp — Tools~/copy_assets.py)"); }
                return s;
            }
        }
        static bool stampWarned;

        /// <summary>공책 한 장(조각들).</summary>
        sealed class Notebook
        {
            public RectTransform Paper;
            public Image PaperImg, Gift;
            public readonly List<RectTransform> Slots = new List<RectTransform>();
            public TextMeshProUGUI Title, Sub, GiftLabel;
            public float K;
        }

        /// <summary>
        /// 칭찬 스티커 공책 — 너비 w · 높이 h(기준 300×64 의 배율 K = h/64). span 칸 가운데 have 칸에 스티커, 끝에 선물 상자(grad 면 꽃 도장) · next 글.
        /// 자리(At)는 부르는 쪽이 잡는다. 종이는 누를 수 있게 raycast 를 켠다.
        /// </summary>
        static Notebook GradeNotebook(Transform parent, string name, float w, float h, string title, string sub, int span, int have, string next, bool grad, Color? tint = null)
        {
            var nb = new Notebook { K = h / 64f };
            float k = nb.K;
            // 종이 — 원작 메모(말린 모서리 · 테두리 무늬) 9칸. 모서리가 높이의 1/3 안팎이 되게 배율을 맞춘다
            nb.PaperImg = Ui.Img(parent, GArt("memo", MemoBorder), tint ?? Color.white, name, true);
            nb.PaperImg.pixelsPerUnitMultiplier = MemoBorder / Mathf.Max(8, h * 0.34f);
            var paper = nb.Paper = nb.PaperImg.rectTransform;
            paper.sizeDelta = new Vector2(w, h);
            // 집게 — 원작 미션 탭 집게(노랑) 하나가 왼쪽 위를 물고 있다
            var clip = Ui.Img(paper, GArt("clip"), Color.white, "clip"); clip.preserveAspect = true;
            clip.rectTransform.At(0, 1, 6 * k, 12 * k, 16 * k, 30 * k); clip.rectTransform.localRotation = Quaternion.Euler(0, 0, 18);
            // 제목 · 부제
            nb.Title = Ui.Title(paper, title, 18 * k, Theme.BubbleInk, TextAlignmentOptions.MidlineLeft, "gname");
            nb.Title.rectTransform.At(0, 1, 30 * k, -3 * k, w * 0.62f, 24 * k);
            nb.Title.textWrappingMode = TextWrappingModes.NoWrap; nb.Title.overflowMode = TextOverflowModes.Overflow;
            nb.Sub = Ui.Text(paper, sub, 12.5f * k, Theme.NoteSub, TextAlignmentOptions.MidlineRight, false, "toNext");
            nb.Sub.rectTransform.At(1, 1, -9 * k, -4 * k, w * 0.42f, 22 * k);
            nb.Sub.textWrappingMode = TextWrappingModes.NoWrap; nb.Sub.overflowMode = TextOverflowModes.Overflow;
            // 스티커 칸 · 선물 상자
            float s = 26 * k, gap = 5 * k, x0 = 30 * k, cy = 19 * k;
            for (int i = 0; i < span; i++)
            {
                bool on = i < have;
                var im = Ui.Img(paper, on ? Sticker(i) : GArt("slot"), Color.white, "slot" + i); im.preserveAspect = true;
                im.rectTransform.At(0, 0, x0 + s / 2 + i * (s + gap), cy, on ? s * 1.08f : s * 0.92f, on ? s * 1.08f : s * 0.92f);
                im.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (on) im.rectTransform.localRotation = Quaternion.Euler(0, 0, StickerTilt(i));
                nb.Slots.Add(im.rectTransform);
            }
            float gx = x0 + span * (s + gap) + 4 * k;
            nb.Gift = Ui.Img(paper, GArt("gift"), Color.white, "gift"); nb.Gift.preserveAspect = true;   // 원작 앨범 선물 상자(원작 「참 잘했어요」 도장은 128px 그림이라 진급 · 졸업 순간에 크게만 쓴다) nb.Gift.preserveAspect = true;
            nb.Gift.rectTransform.At(0, 0, gx + s * 0.6f, cy + 1 * k, s * 1.2f, s * 1.2f); nb.Gift.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            if (grad) GiftSparkle(nb, false);
            nb.GiftLabel = Ui.Title(paper, next, 14 * k, Theme.StampRed, TextAlignmentOptions.MidlineLeft, "giftLabel");
            nb.GiftLabel.rectTransform.At(0, 0, gx + s * 1.3f, cy - 10 * k, 90 * k, 20 * k);
            nb.GiftLabel.textWrappingMode = TextWrappingModes.NoWrap; nb.GiftLabel.overflowMode = TextOverflowModes.Overflow;
            return nb;
        }

        /// <summary>선물 상자가 열렸다 — 원작 감정표현 반짝이(Sparkle)를 상자 위에 얹는다(pop 이면 톡 튀어나오게).</summary>
        static Image GiftSparkle(Notebook nb, bool pop)
        {
            var spr = GArt("sparkle");
            if (spr == null || !nb.Gift) return null;
            var g = nb.Gift.rectTransform;
            var sp = Ui.Img(nb.Paper, spr, Color.white, "sparkle"); sp.preserveAspect = true;
            sp.rectTransform.anchorMin = sp.rectTransform.anchorMax = Vector2.zero; sp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            sp.rectTransform.anchoredPosition = g.anchoredPosition + new Vector2(g.sizeDelta.x * 0.42f, g.sizeDelta.y * 0.5f);
            sp.rectTransform.sizeDelta = g.sizeDelta * 0.8f;
            if (pop) Tw.Pop(sp.rectTransform, 0, 0.4f, 0.3f);
            return sp;
        }

        /// <summary>학년 공책 내용 — 학년 g 의 제목 · 칸 수 · 받은 칸 · 다음 학년 글. 졸업이면 칸은 다 차 있다.</summary>
        static (string title, string sub, int span, int have, string next, bool grad) GradePage(int g, int credits)
        {
            bool grad = g >= Core.Grades.GRAD;
            if (grad)
            {
                int sp = Core.Grades.Need(Core.Grades.GRAD) - Core.Grades.Need(Core.Grades.GRAD - 1);
                return ("<color=#" + ColorUtility.ToHtmlStringRGB(Theme.StampRed) + ">졸업</color> 칭찬 스티커", $"학점 {credits} · 다 모았어요", sp, sp, "졸업!", true);
            }
            int from = Core.Grades.Need(g), to = Core.Grades.Need(g + 1);
            int span = Mathf.Max(1, to - from), have = Mathf.Clamp(credits - from, 0, span);
            return ($"<color=#{ColorUtility.ToHtmlStringRGB(Theme.StampRed)}>{g}</color>학년 칭찬 스티커", $"{span - have}장 더 → 진급", span, have, Core.Grades.Name(g + 1), false);
        }

        /// <summary>지도 머리 띠 — 파티 알약 밑 학년 공책(스티커 칸 · 선물 상자). 누르면 학년 보상 표. 밑에 걸린 진급 보상 쪽지(마스킹테이프).</summary>
        void MapGrade(RectTransform root)
        {
            var run = P.Run;
            if (run == null) return;
            var pg = GradePage(run.Grade, run.S.Credits);
            // HUD 무게 — 종이를 살짝 눌러(채도 · 밝기) 어두운 지도에서 덜 튀게, 크기는 파티 알약 폭 안
            float h = Theme.GradeHudH, w = 320;   // 폰(1440×648 — 캔버스 0.72배)에서도 제목 · 스티커가 읽히게
            var nb = GradeNotebook(root, "hud.grade", w, h, pg.title, pg.sub, pg.span, pg.have, pg.next, pg.grad, new Color(0.93f, 0.91f, 0.87f));
            nb.Paper.At(0, 1, Theme.Gutter + 8, -Theme.GradeHudTop, w, h);
            nb.Paper.localRotation = Quaternion.Euler(0, 0, -0.8f);
            var b = nb.Paper.gameObject.AddComponent<Btn>();
            b.Bg = nb.PaperImg;
            b.SetColor(nb.PaperImg.color);
            b.OnClick = GradeTable;
            Stage.Hot["grade"] = b;
            // 원작 「교단 증명서」(재화 CurrencyIcon_0016 그대로 — RunArt/Ui/order_cert) — 공책 오른쪽 가장자리에 테이프로 반쯤 꽂아 둔다(학년 글 · 칸을 가리지 않게, 공책 크기는 그대로)
            var cert = Theme.Art("Ui/order_cert");
            if (cert != null)
            {
                var ci = Ui.Img(nb.Paper, cert, Color.white, "cert"); ci.preserveAspect = true;
                ci.rectTransform.At(1, 0.5f, 32, -4, 46, 46); ci.rectTransform.localRotation = Quaternion.Euler(0, 0, 9);
                var ct = Ui.Img(ci.rectTransform, GArt("tape"), Color.white, "tape"); ct.rectTransform.At(1, 1, 6, 4, 30, 10); ct.rectTransform.localRotation = Quaternion.Euler(0, 0, 28);
            }
            Tw.Rise(nb.Paper, 0.12f, 14, 0.4f, Vector2.left);
            // 걸려 있는 진급 보상 — 공책 밑 작은 쪽지(마스킹테이프), 남은 횟수
            var buffs = new List<string>();
            if (run.S.PrepLeft > 0) buffs.Add($"<b>예습 노트</b>  첫 손패 +{run.S.PrepHand} · 남은 {run.S.PrepLeft}전투");
            if (run.S.MockLeft > 0) buffs.Add($"<b>전술 교본</b>  사기 {run.S.MockMorale} · 다음 엘리트 · 보스 {run.S.MockLeft}번");
            for (int i = 0; i < buffs.Count; i++)
            {
                var note = Ui.Img(root, GArt("memo", MemoBorder), Color.white, "hud.gradebuff" + i).rectTransform;
                note.GetComponent<Image>().pixelsPerUnitMultiplier = MemoBorder / 10f;
                note.At(0, 1, Theme.Gutter + 26 + i * 10, -Theme.GradeHudTop - h - 6 - i * Theme.GradeBuffStep, 10, 30);
                note.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -1.5f : 1.2f);
                Ui.Row(note, 6, TextAnchor.MiddleLeft, new RectOffset(12, 12, 2, 2), false, false);
                note.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                var tx = Ui.Text(note, buffs[i], Theme.FsCap, Theme.BubbleInk, TextAlignmentOptions.MidlineLeft); tx.textWrappingMode = TextWrappingModes.NoWrap; tx.Pref(tx.preferredWidth + 2, 24);
                var tape = Ui.Img(note, GArt("tape"), Color.white, "tape"); tape.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                tape.rectTransform.At(0, 1, 6, 8, 40, 13); tape.rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
                Tw.Rise(note, 0.2f + i * 0.05f, 10, 0.35f, Vector2.left);
            }
        }

        /// <summary>보상 화면 「전투 끝」 칩 밑 — 이번 싸움 학점만큼 스티커가 하나씩 「착」 붙는 작은 공책(진급 연출은 지도에 돌아와서).</summary>
        void RewardCredit(RectTransform root, RectTransform under)
        {
            var run = P.Run;
            if (run == null || run.S.LastCredit <= 0) return;
            int before = run.S.Credits - run.S.LastCredit;
            int g0 = Mathf.Min(Core.Grades.Of(before), run.Grade);   // 싸움 앞 학년 — 이 싸움으로 진급했어도 그 학년 공책에 붙인다
            if (run.Grade >= Core.Grades.GRAD && before >= Core.Grades.Need(Core.Grades.GRAD)) g0 = Core.Grades.GRAD;
            var pg = GradePage(g0, before);
            bool grad = g0 >= Core.Grades.GRAD;
            int haveAfter = grad ? pg.span : Mathf.Min(pg.span, run.S.Credits - Core.Grades.Need(g0));
            float h = 58, w = 300;
            var nb = GradeNotebook(root, "credit", w, h, pg.title, $"학점 +{run.S.LastCredit}", pg.span, pg.have, haveAfter >= pg.span && !grad ? "진급!" : pg.next, grad);
            nb.Paper.At(0, 0.5f, Theme.Gutter + 22, under.anchoredPosition.y - 70, w, h);
            nb.Paper.localRotation = Quaternion.Euler(0, 0, -1f);
            nb.Sub.color = Theme.StampRed;
            Tw.Rise(nb.Paper, 0.3f, 20, 0.4f, Vector2.left);
            if (grad) return;
            // 새로 받은 칸 — 크게 떨어져 「착」(칸마다 조금씩 늦게)
            for (int i = pg.have; i < haveAfter; i++)
            {
                var slot = nb.Slots[i];
                var im = slot.GetComponent<Image>();
                int ii = i;
                float d = 0.75f + (i - pg.have) * 0.22f;
                var st = Ui.Img(nb.Paper, Sticker(ii), Color.white, "new" + ii).rectTransform;
                st.anchorMin = st.anchorMax = Vector2.zero; st.pivot = new Vector2(0.5f, 0.5f);
                st.anchoredPosition = slot.anchoredPosition; st.sizeDelta = slot.sizeDelta / 0.92f * 1.08f;
                var cg = st.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0;
                Tw.Run(st, 0.2f, k => { if (!st) return; float sc = Mathf.Lerp(2f, 1f, k); st.localScale = new Vector3(sc, sc, 1); st.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(24, StickerTilt(ii), k)); cg.alpha = Mathf.Clamp01(k * 3); }, Tw.OutCubic, d,
                    () => { if (im) im.enabled = false; if (nb.Paper) Tw.Run(nb.Paper, 0.12f, k => { if (nb.Paper) nb.Paper.localScale = Vector3.one * (1 + 0.04f * Mathf.Sin(k * Mathf.PI)); }, Tw.Linear); });
            }
            if (haveAfter >= pg.span && nb.Gift) Tw.Run(nb.Gift, 0.3f, k => { if (nb.Gift) nb.Gift.rectTransform.localScale = Vector3.one * (1 + 0.25f * Mathf.Sin(k * Mathf.PI)); }, Tw.Linear, 0.95f + (haveAfter - pg.have) * 0.22f);
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
        /// 진급 — 판이 쌓아 둔 새 학년(Run.PopGradeNews)을 차례로. 학년마다:
        ///   ① 도장(1초 안팎) — 줄 공책 종이 위에 빨간 「진급」 도장이 쾅 찍히고(학년 숫자), 리본 · 파티 사도 한 명의 한마디(HeroLines promote)
        ///   ② 2~5학년은 보상 고르기(셋 가운데 하나 — GradePick), 졸업은 졸업장 · 꽃가루 · 졸업 선물.
        /// 다 끝나면 then(지도로 — 주인 없는 교주 카드 · 받은 장비가 남았으면 지도가 이어서 묻는다).
        /// </summary>
        public void GradeUp(List<int> news, Action then = null)
        {
            if (news == null || news.Count == 0) { GradeOffers(then); return; }
            int g = news[0];
            var rest = news.Skip(1).ToList();
            Action next = () => GradeUp(rest, then);
            if (g >= Core.Grades.GRAD) Graduate(next);
            else Stamp(g, () => GradeOffers(next, g));
        }

        /// <summary>남은 진급 보상을 차례로 고른다(upTo 학년까지 — 이어하기로 남은 것도). 없으면 곧장 then.</summary>
        void GradeOffers(Action then, int upTo = 99)
        {
            var off = P.Run?.GradeOfferNow;
            if (off == null || off.Grade > upTo) { P.Save("map"); then?.Invoke(); return; }
            GradePick(off, () => GradeOffers(then, upTo));
        }

        /// <summary>한마디 — 파티에서 한 명(무작위), 그 사도의 말투로.</summary>
        (HeroInfo h, string line) GradeLine(string kind)
        {
            var keys = P.S.Party;
            var h = Roster.OfCore(keys[UnityEngine.Random.Range(0, keys.Count)]);
            return (h, HeroLines.Pick(h, kind));
        }

        /// <summary>말풍선 — 사도 말풍선 진입점(HeroBubble.Show) · 진급 · 졸업 꼴(화면 아래 가운데에 머문다).</summary>
        void GradeBubble(RectTransform parent, HeroInfo h, string line, float y, float delay, string kind)
            => HeroBubble.Show(parent, h, line, new Vector2(0.5f, 0), new Vector2(0, y), new Vector2(0.5f, 0), 0, delay, kind);

        /// <summary>
        /// 공책 마지막 칸 연출 — 큰 공책의 비어 있던 마지막 스티커가 크게 떨어져 「착!」, 이어 선물 상자가 열리고, 원작 「참!잘햇어요.」 도장이 쾅.
        /// 진급 · 졸업이 같이 쓴다. 돌려줌: 도장(그림이 없으면 null).
        /// </summary>
        RectTransform NotebookFinish(RectTransform layer, Notebook nb)
        {
            float k = nb.K;
            int last = nb.Slots.Count - 1;
            var slot = nb.Slots[last];
            var slotImg = slot.GetComponent<Image>();
            // ① 마지막 스티커 「착!」
            var st = Ui.Img(nb.Paper, Sticker(last), Color.white, "slap").rectTransform;
            st.anchorMin = st.anchorMax = Vector2.zero; st.pivot = new Vector2(0.5f, 0.5f);
            st.anchoredPosition = slot.anchoredPosition; st.sizeDelta = slot.sizeDelta / 0.92f * 1.08f;
            var scg = st.gameObject.AddComponent<CanvasGroup>(); scg.alpha = 0;
            var chak = Ui.Title(nb.Paper, "착!", 20 * k, Theme.Hex("FFD45C"), TextAlignmentOptions.Center, "chak");
            chak.rectTransform.anchorMin = chak.rectTransform.anchorMax = Vector2.zero;
            chak.rectTransform.anchoredPosition = slot.anchoredPosition + new Vector2(-4 * k, -27 * k);   // 스티커 바로 밑(제목을 가리지 않게) chak.rectTransform.sizeDelta = new Vector2(60 * k, 30 * k);
            chak.rectTransform.localRotation = Quaternion.Euler(0, 0, 12);
            chak.Outline(0.28f, Theme.BubbleRim);
            var ccg = chak.Group(); ccg.alpha = 0;
            Tw.Run(st, 0.22f, t => { if (!st) return; float sc = Mathf.Lerp(2.3f, 1f, t); st.localScale = new Vector3(sc, sc, 1); st.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(30, StickerTilt(last), t)); scg.alpha = Mathf.Clamp01(t * 3); }, Tw.OutCubic, 0.3f, () =>
            {
                if (slotImg) slotImg.enabled = false;
                Sfx.Play("step");
                if (nb.Paper) Tw.Run(nb.Paper, 0.16f, t => { if (nb.Paper) nb.Paper.anchoredPosition = new Vector2(Mathf.Sin(t * 30) * 5 * (1 - t), nb.Paper.anchoredPosition.y); }, Tw.Linear);
                if (chak) Tw.Run(chak, 0.5f, t => { if (ccg) ccg.alpha = t < 0.2f ? t * 5 : 1 - (t - 0.2f) / 0.8f * 0.15f; if (chak) chak.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, Mathf.Min(1, t * 3)); }, Tw.Linear);
            });
            // ② 선물 상자 열림 — 그림을 바꾸고 톡, 둘레로 종이 조각
            Tw.After(Settings.ReduceMotion ? 0.15f : 0.62f, () =>
            {
                if (!nb.Gift) return;
                GiftSparkle(nb, true);
                var grt = nb.Gift.rectTransform;
                Tw.Run(grt, 0.3f, t => { if (grt) grt.localScale = Vector3.one * (1 + 0.35f * Mathf.Sin(t * Mathf.PI)); }, Tw.Linear);
                if (DisplayOptions.LowSpec || Settings.ReduceMotion) return;
                var starSpr = GArt("star");
                if (starSpr == null) return;
                for (int i = 0; i < 10; i++)
                {
                    var c = Ui.Img(nb.Paper, starSpr, Color.white, "bit" + i).rectTransform;   // 원작 별(Popup_Star_01)이 상자에서 튀어 오른다
                    c.anchorMin = c.anchorMax = Vector2.zero; c.sizeDelta = new Vector2(12 * k, 12 * k);
                    Vector2 o = grt.anchoredPosition; float a = Mathf.Lerp(20, 160, i / 11f) * Mathf.Deg2Rad; float dist = UnityEngine.Random.Range(40f, 70f) * k;
                    float spin = UnityEngine.Random.Range(-400f, 400f);
                    var crt = c;
                    Tw.Run(crt, 0.7f, t => { if (!crt) return; crt.anchoredPosition = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist * t + Vector2.down * 30 * k * t * t; crt.localRotation = Quaternion.Euler(0, 0, spin * t); var im = crt.GetComponent<Image>(); if (im) im.color = im.color.A(1 - t * t); }, Tw.OutCubic, 0, () => { if (crt) Destroy(crt.gameObject); });
                }
            });
            // ③ 원작 「참!잘햇어요.」 도장(RunArt/Ui/perfect_stamp — 원작 생활 재료 Icon_PerfectStamp 그대로, 128px 이라 그보다 크게 늘리지 않는다)
            //   공책 오른쪽 아래에 살짝 기울여 「쾅」 + 원작 도장 소리(sfx_crayon_stamp). 그림이 없으면(복사 전) 건너뛴다 — 대신 그리지 않는다.
            var art = PerfectStamp;
            RectTransform stampRt = null;
            if (art != null)
            {
                float fd = Mathf.Min(128, nb.Paper.sizeDelta.y * 1.1f);
                var fim = Ui.Img(nb.Paper, art, Color.white, "perfectstamp"); fim.preserveAspect = true;
                stampRt = fim.rectTransform;
                stampRt.anchorMin = stampRt.anchorMax = new Vector2(1, 0); stampRt.pivot = new Vector2(0.5f, 0.5f);
                stampRt.anchoredPosition = new Vector2(-fd * 0.36f, fd * 0.16f); stampRt.sizeDelta = new Vector2(fd, fd);
                stampRt.localRotation = Quaternion.Euler(0, 0, -10);
                var fcg = stampRt.gameObject.AddComponent<CanvasGroup>(); fcg.alpha = 0;
                var fr = stampRt;
                Tw.Run(fr, 0.18f, t => { if (!fr) return; float sc = Mathf.Lerp(1.8f, 1f, t * t); fr.localScale = new Vector3(sc, sc, 1); fcg.alpha = Mathf.Clamp01(t * 2); }, Tw.Linear, 0.95f, () =>
                {
                    Sfx.Play("stamp");
                    if (nb.Paper) Tw.Run(nb.Paper, 0.14f, t => { if (nb.Paper) nb.Paper.localScale = Vector3.one * (1 - 0.025f * Mathf.Sin(t * Mathf.PI)); }, Tw.Linear);
                });
            }
            return stampRt;
        }

        /// <summary>
        /// ① 진급 — 칭찬 스티커 공책(옛 학년)이 크게 뜨고 마지막 스티커 「착!」 · 선물 상자 열림 · 원작 「참!잘햇어요.」 도장(NotebookFinish), 밑에 「N학년 진급!」 · 사도 한마디.
        /// 누르거나 2초 뒤 then(보상 고르기). 옛 줄 공책 종이 · 빨간 「진급 N」 도장 · 분홍 리본은 이 공책 하나로 합쳤다.
        /// </summary>
        void Stamp(int g, Action then)
        {
            var layer = Ui.Rect("modal gradeup", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0.04f, 0.03f, 0.02f, 0), "dim", true); dim.rectTransform.Fill();
            Tw.Run(dim, 0.18f, k => { if (dim) dim.color = new Color(0.04f, 0.03f, 0.02f, 0.6f * k); });
            float w = Mathf.Min(600, Stage.Size.x - 100), h = w * 64f / 300f;
            var pg = GradePage(g - 1, Core.Grades.Need(g) - 1);
            var nb = GradeNotebook(layer, "paper", w, h, pg.title, "다 모았어요!", pg.span, pg.span - 1, $"{g}학년!", false);
            nb.Paper.At(0.5f, 0.5f, 0, 70, w, h);
            nb.Paper.localRotation = Quaternion.Euler(0, 0, -1.5f);
            Tw.Pop(nb.Paper, 0, 0.85f, 0.25f);
            NotebookFinish(layer, nb);
            var head = Ui.Title(layer, $"{g}학년 진급!", Theme.Fs2xl, Color.white, TextAlignmentOptions.Center, "head");
            head.rectTransform.At(0.5f, 0.5f, 0, 70 - h / 2 - 46, 700, 56); head.Outline(0.24f, Theme.BubbleRim);
            var sub = Ui.Text(layer, g >= 4 ? "고학년 진급 — 보상이 더 좋아집니다" : "진급 보상을 하나 고릅니다", Theme.FsMd, Theme.Cream, TextAlignmentOptions.Center, false, "sub");
            sub.rectTransform.At(0.5f, 0.5f, 0, 70 - h / 2 - 88, 700, 30); sub.Outline(0.22f, Theme.BubbleRim);
            Tw.Rise(head.rectTransform, 1.1f, 16, 0.3f); Tw.Rise(sub.rectTransform, 1.2f, 12, 0.3f);
            var (hh, line) = GradeLine("promote");
            GradeBubble(layer, hh, line, Mathf.Max(16, Stage.Size.y / 2 + 70 - h / 2 - 110 - 130), 0.45f, "promote");
            bool gone = false;
            void Next()
            {
                if (gone) return; gone = true;
                var lg = layer.Group();
                Tw.Run(layer, 0.18f, k => { if (lg) lg.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); then?.Invoke(); });
            }
            var tap = dim.gameObject.AddComponent<Btn>(); tap.OnClick = Next;
            var tap2 = nb.Paper.gameObject.AddComponent<Btn>(); tap2.Bg = null; tap2.OnClick = Next;
            Stage.Hot["grade.ok"] = tap2;
            Tw.After(Settings.ReduceMotion ? 0.9f : 2.1f, () => { if (layer) Next(); });
        }

        /// <summary>원작 스펠 그림(RunArt/Item/SpellCardIcon_N — itemart.json 의 n_strawberry · n_soda · n_tactics 와 같은 그림).</summary>
        static Sprite GradeArt(string kind) => kind switch
        {
            "heal" => Theme.Art("Item/SpellCardIcon_25"), "gauge" => Theme.Art("Item/SpellCardIcon_26"), "mock" => Theme.Art("Item/SpellCardIcon_24"), _ => null,
        };

        static string GradeIcon(string kind) => kind switch
        {
            "heal" => "ic_heart", "gauge" => "ic_spark", "flash" => "ic_bless", "grace" => "ic_up", "remove" => "ic_trash", "equip" => "ic_sword", "neutral" => "ic_deck", "prep" => "ic_book", "mock" => "ic_swords", _ => "ic_plus",
        };

        /// <summary>② 진급 보상 고르기 — 크림 카드 셋(아이콘 · 이름 · 설명). 하나를 누르면 받고, 이어서 고를 것(신탁 · 뺄 카드 · 장비)을 묻는다.</summary>
        public void GradePick(Core.GradeOffer off, Action then)
        {
            var layer = Ui.Rect("modal gradepick", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0.04f, 0.03f, 0.02f, 0.7f), "dim", true); dim.rectTransform.Fill();
            int n = off.Choices.Count;
            float cw = Mathf.Min(300, (Stage.Size.x - 120 - (n - 1) * 30) / n), ch = Mathf.Min(360, Stage.Size.y - 300);
            var row = Ui.Rect("cards", layer).At(0.5f, 0.5f, 0, -40, cw * n + 30 * (n - 1), ch);
            // 제목 띠 — 카드 바로 위(머리 띠 HUD 와 겹치지 않게 화면 가운데 쪽에)
            var ribbon = Ui.Img(layer, Theme.S("pill", 46), Theme.Hex("F07C8E"), "ribbon").rectTransform;
            ribbon.At(0.5f, 0.5f, 0, -40 + ch / 2 + 44, 360, 52);
            var title = Ui.Title(ribbon, $"{off.Grade}학년 진급 보상 — 하나 고르기", Theme.FsLg, Color.white, TextAlignmentOptions.Center); title.rectTransform.Fill(10, 0, 10, 0); title.Outline(0.2f, Theme.Hex("A8394C"));
            title.textWrappingMode = TextWrappingModes.NoWrap; title.enableAutoSizing = true; title.fontSizeMin = 14; title.fontSizeMax = Theme.FsLg * Settings.TextScale;
            if (off.Choices.Any(c => c.Kind == "heal" || c.Kind == "gauge" || c.Kind == "mock"))
            {   // 스펠 보상(딸기맛 · 소다맛 캡슐 · 전술 교본)은 같은 이름 교주 카드와 달리 덱에 들지 않는다 — 카드 밑 공통 한 줄
                var note = Ui.Text(layer, "스펠 보상은 덱에 들어가지 않고 바로 효과를 냅니다", Theme.FsSm, Theme.Hex("FFF4DC"), TextAlignmentOptions.Center, false, "spellnote");
                note.rectTransform.At(0.5f, 0.5f, 0, -40 - ch / 2 - 26, 900, 28); note.Outline(0.22f, GradeBrownDeep);
                note.textWrappingMode = TextWrappingModes.NoWrap;
            }
            bool taken = false;
            for (int i = 0; i < n; i++)
            {
                var c = off.Choices[i]; int ii = i;
                var (nm, desc) = Core.Grades.Describe(c);
                var b = Btn.Make(row, null, BtnStyle.Ghost, () =>
                {
                    if (taken) return; taken = true;
                    var t = P.Run.TakeGrade(ii);
                    P.Save("map");
                    var lg = layer.Group();
                    Tw.Run(layer, 0.2f, k => { if (lg) lg.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); GradeFollow(t, then); });
                }, 0, "pick" + i);
                var brt = b.GetComponent<RectTransform>(); brt.At(0, 0.5f, i * (cw + 30), 0, cw, ch); brt.pivot = new Vector2(0, 0.5f);
                var rim = Ui.Img(brt, Theme.S("round", 20), GradeBrown, "rim"); rim.rectTransform.Fill();
                var body = Ui.Img(brt, Theme.S("round", 20), GradeCream, "body"); body.rectTransform.Fill(5, 5, 5, 5);
                var disc = Ui.Img(brt, Theme.S("circle"), GradeBrown, "disc"); disc.rectTransform.At(0.5f, 1, 0, -26, ch * 0.32f, ch * 0.32f); disc.rectTransform.pivot = new Vector2(0.5f, 1);
                var inner = Ui.Img(disc.transform, Theme.S("circle"), Theme.Hex("FFE2B0"), "in"); inner.rectTransform.Fill(5, 5, 5, 5);
                {
                    // 원작 그림이 있으면 그것(골드 재화 · 딸기맛 · 소다맛 캡슐 · 전술 교본 스펠 그림 — RunArt, git 밖), 없으면 판 화면 아이콘
                    var art = c.Kind == "gold" ? Theme.Icon("gold") : GradeArt(c.Kind);
                    var spr = art ?? Theme.S(GradeIcon(c.Kind));
                    var ic = Ui.Img(inner.transform, spr, art != null ? Color.white : GradeBrownDeep, "ic"); ic.rectTransform.Fill(ch * 0.07f, ch * 0.07f, ch * 0.07f, ch * 0.07f); ic.preserveAspect = true;
                }
                var t1 = Ui.Title(brt, nm, Theme.FsXl, GradeBrownDeep, TextAlignmentOptions.Center); t1.rectTransform.Band(1, 40, 10, 10, -30 - ch * 0.32f - 10);
                var t2 = Ui.Text(brt, desc, Theme.FsMd, GradeBrown, TextAlignmentOptions.Top); t2.rectTransform.Fill(18, 50, 18, 30 + ch * 0.32f + 56);
                t2.enableAutoSizing = true; t2.fontSizeMin = 12; t2.fontSizeMax = Theme.FsMd * Settings.TextScale;
                var tag = Ui.Img(brt, Theme.S("pill", 46), GradeFill, "pick"); tag.rectTransform.At(0.5f, 0, 0, 14, Mathf.Min(160, cw - 40), 38);
                var tt = Ui.Title(tag.transform, "고르기", Theme.FsMd, Color.white, TextAlignmentOptions.Center); tt.rectTransform.Fill(); tt.Outline(0.2f, GradeBrownDeep.A(0.8f));
                Stage.Hot["grade.pick" + i] = b;
                Tw.Pop(brt, 0.08f + i * 0.07f, 0.7f, 0.35f);
            }
        }

        /// <summary>보상 받은 뒤 이어서 — 신탁 고르기 · 은총 카드 보이기 · 뺄 카드 고르기 · 장비 정하기 · 크레파스 주기.</summary>
        void GradeFollow(Core.GradeTake t, Action then)
        {
            if (t == null) { then?.Invoke(); return; }
            if (t.Note != null) Toast.Show(t.Note, 3f);
            var c = t.Choice;
            switch (c.Kind)
            {
                case "gold": case "heal": case "gauge": case "prep": case "mock":
                    Toast.Show(Core.Grades.Describe(c).desc, 2.6f);
                    then?.Invoke(); return;
                case "flash":
                    if (t.Flash == null || t.Flash.Picks.Count == 0) { then?.Invoke(); return; }
                    {
                        var fo = P.Run.FlashOptions(t.Flash);
                        OracleReveal.Show(this, new RevealOpts
                        {
                            Title = "특별 수업!", Sub = "신탁 하나를 고릅니다 · 카드를 길게 누르면 자세히", Picks = RevealPick.Of(P.Data, t.Flash.CardId, fo), Cancel = false, Hot = "grade.flash",
                            Done = i => { P.Run.TakeFlash(t.Flash.CardId, fo[i].N); P.Save("map"); then?.Invoke(); },
                        });
                    }
                    return;
                case "grace": case "neutral":
                    if (t.Card == null) { then?.Invoke(); return; }
                    CardGain.Show(this, new List<string> { t.Card }, () => then?.Invoke(), c.Kind == "grace" ? "사도의 선물!" : "스펠");
                    return;
                case "remove":
                    if (!t.Remove) { then?.Invoke(); return; }
                    GradeRemovePick(then); return;
                case "equip":
                    Settle(() => then?.Invoke()); return;
            }
            then?.Invoke();
        }

        /// <summary>「정리 정돈」 — 덱에서 뺄 카드 하나를 고른다(금기 빼고). 상점 빼기와 같은 카드 칸.</summary>
        void GradeRemovePick(Action then)
        {
            float mw = Mathf.Min(1240, Stage.Size.x - 40);
            var (right, closeM, _) = Stage.ModalBox("gradeRemove", mw, Mathf.Min(680, Stage.Size.y - 24), "정리 정돈 — 덱에서 뺄 카드를 고릅니다", "진급 보상 · 값 없이 한 장", false, null, 0, false);
            var area = Ui.Rect("pick", right).Fill(4, 4, 4, 4);
            var content = Ui.Scroll(area, out _);
            var ids = P.S.Deck.Where(id => !P.View(id).IsTaboo).ToList();
            CardGroups(content, ids, 170, GridCols(mw - 80, 170, 12, Theme.Compact ? 3 : 6, 8), (cv, id, i) =>
            {
                var b = cv.gameObject.AddComponent<Btn>();
                b.OnClick = () =>
                {
                    var why = P.Run.GradeRemove(id);
                    if (why != null) { Toast.Show(why); return; }
                    Toast.Show($"「{P.Data.Card(Core.GameData.NoOwner(id))?.Name ?? id}」 을 뺐습니다");
                    P.Save("map");
                    closeM();
                    then?.Invoke();
                };
                Stage.Hot["grade.remove" + i] = b;
            });
        }

        /// <summary>
        /// 졸업 — 진급과 같은 칭찬 스티커 공책(5학년 마지막 칸 「착!」 · 선물 상자 · 원작 「참 잘했어요」 도장) 밑에 졸업장 쪽지(졸업 선물 · 회복 · 고학년 게이지) · 꽃가루 · 한마디. 「확인」 을 누르면 then.
        /// </summary>
        void Graduate(Action then)
        {
            var layer = Ui.Rect("modal graduate", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0.04f, 0.03f, 0.02f, 0.62f), "dim", true); dim.rectTransform.Fill();
            float w = Mathf.Min(600, Stage.Size.x - 100), h = w * 64f / 300f;
            int g5 = Core.Grades.GRAD - 1;
            var pg = GradePage(g5, Core.Grades.Need(Core.Grades.GRAD) - 1);
            float ny = Mathf.Min(150, Stage.Size.y / 2 - h / 2 - 70);
            var nb = GradeNotebook(layer, "paper", w, h, pg.title, "다 모았어요!", pg.span, pg.span - 1, "졸업!", false);
            nb.Paper.At(0.5f, 0.5f, 0, ny, w, h);
            nb.Paper.localRotation = Quaternion.Euler(0, 0, -1.5f);
            Tw.Pop(nb.Paper, 0.05f, 0.85f, 0.3f);
            NotebookFinish(layer, nb);
            // 졸업장 쪽지 — 같은 종이(테이프 둘), 공책 밑
            float dw = w * 0.92f, dh = Mathf.Min(176, Stage.Size.y / 2 + ny - h / 2 - 150);
            var dipImg = Ui.Img(layer, GArt("memo", MemoBorder), Color.white, "frame"); dipImg.pixelsPerUnitMultiplier = MemoBorder / 40f;
            var dip = dipImg.rectTransform;
            dip.At(0.5f, 0.5f, 0, ny - h / 2 - 22 - dh / 2, dw, dh); dip.localRotation = Quaternion.Euler(0, 0, 1f);
            for (int i = 0; i < 2; i++)
            {
                var tape = Ui.Img(dip, GArt("tape"), Color.white, "tape" + i);
                tape.rectTransform.At(i, 1, i == 0 ? 30 : -30, 8, 64, 20); tape.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? -10 : 9);
            }
            var t = Ui.Title(dip, "졸 업 장", Theme.FsXl, Theme.BubbleInk, TextAlignmentOptions.Center, "title"); t.rectTransform.Band(1, 40, 20, 20, -10); t.characterSpacing = 16;
            string gift = P.S.GradeGift != null ? $"졸업 선물 — 「{P.Data.Card(P.S.GradeGift)?.Name ?? P.S.GradeGift}」(은총)" : $"졸업 선물 — 골드 +{Core.Run.GRACE_GOLD}";
            var body = Ui.Text(dip, $"볼제나 학당의 모든 과정을 마쳤어요!\n<color=#{ColorUtility.ToHtmlStringRGB(Theme.NoteSub)}><size=88%>{gift}\n파티 HP {Core.Grades.GRAD_HEAL * 100:0}% 회복 · 그 뒤 전투 시작 고학년 게이지 +{Core.Grades.GRAD_GAUGE}%</size></color>",
                Theme.FsMd, Theme.BubbleInk, TextAlignmentOptions.Top);
            body.rectTransform.Fill(28, 62, 28, 52); body.enableAutoSizing = true; body.fontSizeMin = 11; body.fontSizeMax = Theme.FsMd * Settings.TextScale;
            Tw.Rise(dip, 1.15f, 18, 0.35f);
            var (h0, say) = GradeLine("graduate");
            GradeBubble(layer, h0, say, 12, 0.5f, "graduate");
            Sfx.Play("win");
            if (!DisplayOptions.LowSpec) StartCoroutine(Confetti(layer));
            bool gone = false;
            void Next()
            {
                if (gone) return; gone = true;
                var lg = layer.Group();
                Tw.Run(layer, 0.2f, k => { if (lg) lg.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); then?.Invoke(); });
            }
            var tap = dim.gameObject.AddComponent<Btn>(); tap.OnClick = Next;
            var ok = Btn.Make(dip, "확인", BtnStyle.PillGold, Next, Theme.FsLg, "gradeok"); ok.GetComponent<RectTransform>().At(0.5f, 0, 0, 10, 180, 46);
            Stage.Hot["grade.ok"] = ok;
        }
    }
}
