using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 카드 그림 시트(-demo-cardsheet, 2026-10-07 「사도들 위에 글자랑 공격 · 스킬 이 글자에 안 가리게」) — 135명 사도의 대표 카드(시작 카드 첫 장)를
    //   실제 카드 꼴(W.Card — 비용 · 이름 · 종류 알약 · 효과 판)로 한 장에 27명씩(카드 폭 ≈ 200 — 실제 크기) 찍는다. -oldcardcrop 이면 예전 자르기(frac 0.56).
    //   안내선: 하늘 = 위 글 · 칩 끝(전투 카드 26%), 주황 = 아래 효과 판 위 끝(전투 확대 61%), 분홍 상자 = 표의 얼굴 상자.
    //   [CardSheet] 줄 — 사도마다 얼굴 상자 위 · 가운데 · 아래 · 머리 꼭대기의 자리(카드 그림 높이에 대한 위에서부터의 몫)와 판정.
    public partial class Demo
    {
        public const float SafeTop = 0.26f, SafeBottom = 0.61f;

        IEnumerator CardSheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            var heroes = Roster.All.Where(h => h.art != null).OrderBy(h => h.art, System.StringComparer.Ordinal).ToList();
            var size = f.Stage.Size;
            const int cols = 9, rows = 3, per = cols * rows;
            float cw = Mathf.Floor((size.x - 20) / cols), w = cw - 8, ch = w * 1.4f, lab = 26;
            string tag = StandingSnap.OldCardCrop ? "before" : "after";
            int bad = 0, cut = 0;
            var log = new System.Text.StringBuilder($"[CardSheet] {tag} 사도 {heroes.Count} — 이름 · 얼굴 위/가운데/아래 · 머리 꼭대기(위에서 몫) · 판정\n");
            for (int p = 0; p * per < heroes.Count; p++)
            {
                var layer = Ui.Rect("modal cardsheet", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
                var page = heroes.Skip(p * per).Take(per).ToList();
                for (int i = 0; i < page.Count; i++)
                {
                    var h = page[i];
                    float x = 10 + (i % cols) * cw + 4, y = -8 - (i / cols) * (ch + lab + 8);
                    var d = h.CoreId != null ? f.P.Data.Hero(h.CoreId) : null;
                    string id = d?.Starter?.FirstOrDefault();
                    if (id == null) continue;
                    var card = W.Card(layer, f, id, w, "card " + h.art);
                    card.At(0, 1, x, y, w, ch);
                    // 안내선 · 얼굴 상자(그림 창 = 카드 안쪽 2k)
                    float k = w / 200f, ax = 2 * k, ay = 2 * k, aw = w - 4 * k, ah = ch - 4 * k;
                    void Line(float frac, Color c) { var ln = Ui.Img(card, Theme.White, c, "guide"); ln.rectTransform.At(0, 1, ax, -(ay + ah * frac), aw, 1); }
                    Line(SafeTop, Theme.Sky.A(0.8f));
                    Line(SafeBottom, new Color(1f, 0.6f, 0.2f, 0.8f));
                    string verdict = "-";
                    if (!StandingSnap.OldCardCrop ? StandingSnap.CardCropRect(h.art, out var r) : StandingSnap.CropRect(h.art, StandingSnap.CardRatio, 0.56f, out r))
                    {
                        if (StandingFit.TryGet(h.art, out var fit) && fit.HasHead)
                        {
                            float Fy(float yy) => (r.yMax - yy) / r.height;
                            float Fx(float xx) => (xx - r.xMin) / r.width;
                            var fb = fit.FaceBox;
                            float ft = Fy(fb.yMax), fbm = Fy(fb.yMin), fc = Fy(fit.CardFaceY != 0 ? fit.CardFaceY : fit.FaceY != 0 ? fit.FaceY : fb.center.y), ht = Fy(fit.HeadBox.yMax);
                            void Edge(float x0, float y0, float ww, float hh) { var e = Ui.Img(card, Theme.White, new Color(1f, 0.35f, 0.75f, 0.85f), "faceedge"); e.rectTransform.At(0, 1, ax + aw * x0, -(ay + ah * y0), ww, hh); }
                            float l = Fx(fb.xMin), rr = Fx(fb.xMax);
                            Edge(l, ft, aw * (rr - l), 1); Edge(l, fbm, aw * (rr - l), 1); Edge(l, ft, 1, ah * (fbm - ft)); Edge(rr, ft, 1, ah * (fbm - ft));
                            var dot = Ui.Img(card, Theme.S("round"), new Color(1f, 0.35f, 0.75f, 0.9f), "facedot"); dot.rectTransform.At(0, 1, ax + aw * Fx(fb.center.x) - 2, -(ay + ah * fc) + 2, 4, 4);
                            // 판정 — 얼굴 가운데(눈께)가 안전 구역 안 · 얼굴 상자가 위 글에 1/4 넘게 · 아래 판에 1/4 넘게 걸리지 않음
                            //   (얼굴 높이 손보정 사도는 표의 얼굴 상자가 틀려 가운데만) · 머리 둘레 위 끝(뿔 · 장식 포함)이 잘리면 「정수리」 로 따로 센다
                            float fh = Mathf.Max(0.01f, fbm - ft);
                            bool boxOk = fit.FaceY != 0 || fit.CardFaceY != 0 || ((SafeTop - ft) / fh <= 0.25f && (fbm - SafeBottom) / fh <= 0.25f);
                            bool ok = fc >= SafeTop + 0.04f && fc <= SafeBottom - 0.06f && boxOk;
                            verdict = ok ? "ok" : "가림";
                            if (!ok) bad++;
                            if (ht < -0.02f) cut++;
                            log.Append($"  {h.art}\t{h.ko}\t{ft:0.00}/{fc:0.00}/{fbm:0.00}\t머리 {ht:0.00}{(ht < -0.02f ? " 정수리" : "")}\t{verdict}\n");
                            if (!ok) { var warn = Ui.Img(card, Theme.White, new Color(1f, 0.2f, 0.2f, 0.9f), "bad"); warn.rectTransform.At(0, 0, 0, -4, w, 3); }
                        }
                    }
                    var t = Ui.Text(layer, $"{h.ko} <size=80%><color={Theme.SubTag}>{h.art}</color></size>", 11, verdict == "가림" ? Theme.Bad : Theme.Ink, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x - 4, y - ch - 4, cw, lab); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 7; t.fontSizeMax = 11;
                }
                float t0 = 0;
                yield return null;
                while (t0 < 60 && StandingSnap.Pending > 0) { t0 += Time.unscaledDeltaTime; yield return null; }
                yield return Wait(1.0f);
                M("cardsheet_" + p);
                yield return Shot($"cardsheet_{tag}_{p + 1}");
                Destroy(layer.gameObject);
                yield return Wait(0.3f);
            }
            log.Append($"[CardSheet] {tag} 가림 {bad}명 · 머리 둘레 위 끝 잘림 {cut}명");
            Debug.Log(log.ToString());
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }
    }
}
