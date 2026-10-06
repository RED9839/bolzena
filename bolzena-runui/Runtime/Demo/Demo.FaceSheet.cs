using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 얼굴 칸 · 사도 상세 스탠딩 전수 시트(2026-10-07 「초상 줄이 너무 확대」 · 「쥬비는 날고 있어야 하는데 땅에 붙어 있다」).
    //   -demo-facesheet   : 135명 얼굴 칸(사도 상세 초상 줄과 같은 둥근 칸 · StandingSnap 얼굴 자르기)을 한 장에 70명씩 + 짚은 다섯 명 전후 판.
    //                       -oldface 면 예전 자르기. [FaceSheet] 줄 — 사도마다 머리 위 · 얼굴 가운데 · 턱의 자리(칸 높이에 대한 위에서부터의 몫) · 머리 키 몫 · 판정.
    //   -demo-detailsheet : 135명 사도 상세 가운데 스탠딩(같은 칸 비율 · 기울어진 판 · 바닥 그늘)을 한 장에 14명씩. -nolift 면 공중 높이 없이.
    //                       [DetailSheet] 줄 — 발(몸 아래 끝) 높이 · 머리 위 끝(칸 높이 몫) · 판정(위로 나감 · 아래로 나감).
    public partial class Demo
    {
        static readonly string[] FaceAsk = { "vela", "ayla", "maestromk2", "bigwood", "jubee" };

        IEnumerator FaceSheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            var heroes = Roster.All.Where(h => h.art != null && StandingFit.Has(h.art)).OrderBy(h => h.art, System.StringComparer.Ordinal).ToList();
            var size = f.Stage.Size;
            const int cols = 14, rows = 5, per = cols * rows;
            float cw = Mathf.Floor((size.x - 20) / cols), s = Mathf.Min(cw - 22, 96), ch = s + 34;
            string tag = StandingSnap.OldFaceCrop ? "before" : "after";
            int bad = 0;
            var log = new System.Text.StringBuilder($"[FaceSheet] {tag} 사도 {heroes.Count} — 이름 · 머리 위/얼굴 가운데/턱(위에서 몫) · 머리 키 몫 · 얼굴 가로 · 판정\n");
            for (int p = 0; p * per < heroes.Count; p++)
            {
                var layer = Ui.Rect("modal facesheet", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
                var made = new List<Image>();
                var page = heroes.Skip(p * per).Take(per).ToList();
                for (int i = 0; i < page.Count; i++)
                {
                    var h = page[i];
                    float x = 10 + (i % cols) * cw + (cw - s) / 2, y = -10 - (i / cols) * ch;
                    // 초상 줄과 같은 꼴 — 둥근 칸(성격 색) · 안쪽 둥근 가림 · 테두리
                    var cell = Ui.Img(layer, Theme.Round, Color.Lerp(Theme.NavyWell, Theme.NatureOf(h.nature), 0.35f), "cell " + h.art);
                    cell.rectTransform.At(0, 1, x, y, s, s);
                    var clip = Ui.Img(cell.rectTransform, Theme.Round, Color.white, "clip"); clip.rectTransform.Fill(3, 3, 3, 3); clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                    var still = Ui.Img(clip.rectTransform, null, Color.clear, "snap"); still.rectTransform.Fill();
                    var ls = clip.gameObject.AddComponent<LiveStanding>();
                    ls.Art = h.art; ls.W = s - 6; ls.H = s - 6; ls.Frac = 0.34f; ls.Still = still;
                    made.Add(still);
                    var rim = Ui.Img(cell.rectTransform, Theme.Frame, Theme.NatureOf(h.nature).A(0.7f), "rim"); rim.rectTransform.Fill();
                    string verdict = FaceVerdict(h.art, StandingSnap.OldFaceCrop, out string nums);
                    if (verdict != "ok") bad++;
                    log.Append($"  {h.art}\t{h.ko}\t{nums}\t{verdict}\n");
                    var t = Ui.Text(layer, $"{h.ko}\n<size=80%><color={Theme.SubTag}>{h.art}</color></size>", 11, verdict == "ok" ? Theme.Ink : Theme.Bad, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x - (cw - s) / 2, y - s - 1, cw, 32); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 7; t.fontSizeMax = 11;
                }
                float t0 = 0;
                yield return null;
                while (t0 < 60 && (StandingSnap.Pending > 0 || made.Any(im => im.sprite == null && !Baked(im)))) { t0 += Time.unscaledDeltaTime; yield return null; }
                yield return Wait(1.0f);
                M("facesheet_" + p);
                yield return Shot($"facesheet_{tag}_{p + 1}");
                Destroy(layer.gameObject);
                yield return Wait(0.3f);
            }
            log.Append($"[FaceSheet] {tag} 판정 걸림 {bad}명");
            Debug.Log(log.ToString());

            // 짚은 다섯 명 — 예전 · 지금 자르기를 나란히(실제 크기 64 와 크게 160)
            {
                var layer = Ui.Rect("modal faceask", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
                var head = Ui.Title(layer, "얼굴 칸 전후 — 왼쪽 예전 · 오른쪽 지금 (큰 칸 160 · 실제 칸 64)", Theme.FsMd, Theme.Gold, TMPro.TextAlignmentOptions.MidlineLeft);
                head.rectTransform.At(0, 1, 24, -10, size.x - 48, 36);
                float gw = (size.x - 40) / FaceAsk.Length;
                for (int i = 0; i < FaceAsk.Length; i++)
                {
                    var a = FaceAsk[i];
                    float gx = 20 + i * gw;
                    for (int v = 0; v < 2; v++)
                    {
                        bool old = v == 0;
                        if (!StandingSnap.FaceCropRect(a, old, out var r)) continue;
                        var tex = StandingSnap.Bake(a, r, 320, 320);
                        var sp = tex != null ? Sprite.Create(tex, new Rect(0, 0, 320, 320), new Vector2(0.5f, 0.5f), 100) : null;
                        foreach (var (sz, yy) in new[] { (140f, -70f), (64f, -230f) })
                        {
                            float xx = gx + 8 + v * (gw / 2) + (gw / 2 - 16 - sz) / 2;
                            var cell = Ui.Img(layer, Theme.Round, old ? new Color(0.35f, 0.2f, 0.25f) : new Color(0.2f, 0.3f, 0.35f), "cell " + a);
                            cell.rectTransform.At(0, 1, xx, yy, sz, sz);
                            var clip = Ui.Img(cell.rectTransform, Theme.Round, Color.white, "clip"); clip.rectTransform.Fill(3, 3, 3, 3); clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                            var im = Ui.Img(clip.rectTransform, sp, Color.white, "pic"); im.rectTransform.Fill();
                        }
                        var lb = Ui.Text(layer, old ? "예전" : "지금", 13, old ? Theme.Bad : Theme.Good, TMPro.TextAlignmentOptions.Top);
                        lb.rectTransform.At(0, 1, gx + 8 + v * (gw / 2), -300, gw / 2 - 16, 20);
                    }
                    var nm = Roster.All.FirstOrDefault(h => h.art == a);
                    var t = Ui.Title(layer, $"{nm?.ko ?? a} <size=70%><color={Theme.SubTag}>{a}</color></size>", Theme.FsMd, Theme.Ink, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, gx, -330, gw, 30);
                }
                yield return Wait(1.0f);
                yield return Shot("faceask_before_after");
                Destroy(layer.gameObject);
            }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        /// <summary>얼굴 칸 판정(2026-10-07 「얼굴이 가운데쯤 · 머리 전체 + 어깨 살짝 · 모자 · 뿔은 잘려도 됨」) — 얼굴 가운데가 위에서 0.36~0.60 · 가로 0.3~0.7(빗나감),
        /// 얼굴 상자 키가 칸의 0.45~0.78(작음 · 큼), 몸 꼭대기(머리카락 위, 뿔 · 장신구 뺌)가 칸 위로 높이의 6% 넘게 나가면 「머리 잘림」. 손보정 사도는 표의 얼굴 상자가 틀려 가운데만.</summary>
        static string FaceVerdict(string art, bool old, out string nums)
        {
            nums = "-";
            if (!StandingSnap.FaceCropRect(art, old, out var r) || !StandingFit.TryGet(art, out var fit)) return "표 없음";
            float Fy(float yy) => (r.yMax - yy) / r.height;
            float Fx(float xx) => (xx - r.xMin) / r.width;
            float faceC = StandingSnap.IconFace(fit, out bool manual);
            float ht = Fy(fit.TopY), fc = Fy(faceC), chin = Fy(fit.FaceBox.yMin), fx = Fx(fit.FaceCx);
            float size = fit.FaceBox.height / r.height;
            nums = $"{ht:0.00}/{fc:0.00}/{chin:0.00}\t얼굴 {size:0.00}\t가로 {fx:0.00}{(manual ? "\t손보정" : "")}";
            var why = new List<string>();
            if (fc < 0.36f || fc > 0.60f || fx < 0.3f || fx > 0.7f) why.Add("빗나감");
            if (!manual && size < 0.45f) why.Add("작음");
            if (!manual && size > 0.78f) why.Add("큼");
            if (!manual && ht < -0.06f) why.Add("머리 잘림");
            return why.Count == 0 ? "ok" : string.Join("·", why);
        }

        IEnumerator DetailSheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            var heroes = Roster.All.Where(h => h.art != null && StandingFit.Has(h.art)).OrderBy(h => h.art, System.StringComparer.Ordinal).ToList();
            var only = Arg("-only");
            if (!string.IsNullOrEmpty(only)) { var set = only.Split(','); heroes = heroes.Where(h => set.Contains(h.art)).ToList(); }
            var size = f.Stage.Size;
            // 사도 상세(DetailStats)와 같은 칸 — 가운데 칸 figW0 × stageH, 기울어진 판 380 × min(620, stageH − 40), 바닥 그늘 90, 발 여백 4
            float panelW = Theme.C(520, 480);
            float stageH = size.y - 92 - Theme.Gutter;
            float figW0 = Mathf.Max(420, size.x - (Theme.Gutter + 84 + 14 + 150 + 20) - Theme.Gutter - panelW - 20);
            const int cols = 10, rows = 3, per = cols * rows;
            float cw = Mathf.Floor((size.x - 20) / cols), lab = 34;
            float sh = Mathf.Floor((size.y - 20) / rows - lab - 10), sc = sh / stageH, sw = cw - 6;   // 칸 높이 비율 그대로(너비는 판이 들어갈 만큼)
            string tag = StandingFit.NoLift ? "nolift" : "lift";
            int bad = 0;
            var log = new System.Text.StringBuilder($"[DetailSheet] {tag} 사도 {heroes.Count} (칸 {figW0:0}×{stageH:0}) — 이름 · 공중 높이 · 발(아래 끝) 높이 · 몸 아래 끝 · 머리 위 끝(칸 높이 몫, 0 = 바닥) · 판정\n");
            for (int p = 0; p * per < heroes.Count; p++)
            {
                var layer = Ui.Rect("modal detailsheet", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
                var page = heroes.Skip(p * per).Take(per).ToList();
                for (int i = 0; i < page.Count; i++)
                {
                    var h = page[i];
                    float x = 10 + (i % cols) * cw + 3, y = -10 - (i / cols) * (sh + lab + 10);
                    var box = Ui.Img(layer, Theme.White, new Color(0.08f, 0.1f, 0.16f), "slot " + h.art); box.rectTransform.At(0, 1, x, y, sw, sh);
                    var fig = box.rectTransform;
                    var nc = Theme.NatureOf(h.nature);
                    var plate2 = Ui.Img(fig, Theme.Round, Theme.NavyCell.A(0.9f), "plate2"); plate2.rectTransform.At(0.5f, 0.5f, 70 * sc, -10 * sc, 300 * sc, Mathf.Min(560, stageH - 80) * sc);
                    plate2.rectTransform.localRotation = Quaternion.Euler(0, 0, -9);
                    var plate = Ui.Img(fig, Theme.Round, nc.A(0.55f), "plate"); plate.rectTransform.At(0.5f, 0.5f, 0, 0, 380 * sc, Mathf.Min(620, stageH - 40) * sc);
                    plate.rectTransform.localRotation = Quaternion.Euler(0, 0, -9);
                    var live = SpineUi.Standing(fig, h.art, sw, sh, StandMode.Full, 0, 4 * sc, "detail");
                    var shade = Ui.Img(fig, Theme.S("fade_down"), Theme.Night.A(0.75f), "foot"); shade.rectTransform.Band(0, 90 * sc, -40 * sc, -40 * sc, -6 * sc);
                    var floorLn = Ui.Img(fig, Theme.White, Theme.Gold.A(0.5f), "floor"); floorLn.rectTransform.At(0, 0, 0, 4 * sc, sw, 1);
                    string verdict = "-";
                    if (StandingFit.Solve(h.art, new Rect(0, 0, sw, sh), StandMode.Full, 0, 4 * sc, out var k, out var o, "detail") && StandingFit.TryGet(h.art, out var fit))
                    {
                        float foot = (o.y + fit.FootY * k) / sh, low = (o.y + fit.Bounds.yMin * k) / sh, top = (o.y + fit.HairTop * k) / sh, topAll = (o.y + fit.Bounds.yMax * k) / sh;
                        var why = new List<string>();
                        if (topAll > 1.06f) why.Add("위로 나감");
                        if (low < -0.08f) why.Add("아래로 나감");
                        verdict = why.Count == 0 ? "ok" : string.Join("·", why);
                        if (why.Count > 0) bad++;
                        log.Append($"  {h.art}\t{h.ko}\t띄움 {fit.Lift:0}\t발 {foot:0.00}\t아래 끝 {low:0.00}\t머리 {top:0.00}\t전체 위 {topAll:0.00}\t{verdict}\n");
                    }
                    var t = Ui.Text(layer, $"{h.ko} <size=80%><color={Theme.SubTag}>{h.art}</color></size>", 12, verdict == "ok" || verdict == "-" ? Theme.Ink : Theme.Bad, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x, y - sh - 2, sw, lab); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = 12;
                }
                foreach (var tx in layer.GetComponentsInChildren<TMPro.TextMeshProUGUI>()) tx.transform.SetAsLastSibling();
                yield return Wait(1.2f);
                M("detailsheet_" + p);
                yield return Shot($"detailsheet_{tag}_{p + 1}");
                Destroy(layer.gameObject);
                yield return Wait(0.4f);
            }
            log.Append($"[DetailSheet] {tag} 판정 걸림 {bad}명");
            Debug.Log(log.ToString());
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }
    }
}
