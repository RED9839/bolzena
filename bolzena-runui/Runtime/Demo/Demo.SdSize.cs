using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 장면 SD 크기 점검(-demo-sdsize, 2026-10-06 「멜루나 이벤트 때 엄청 작게」)
    //   ① 비교 판 — 사도마다 [전투 배율 그대로(SpineUi.Battle) | 장면(SceneHero + 이벤트가 하는 ClampInto)] 같은 바닥선 · 머리 높이 선
    //   ② 휴식 — 그 사도들을 파티로 세운 모닥불 장면
    //   ③ 이벤트 M5(멜루나가 대상) — 파티 셋 + 오른쪽 대상
    public partial class Demo
    {
        // 경계가 몸보다 크게 넓거나 낮은 SD(투명 슬롯 · 펫 · 무기 이펙트) — sdscan 결과
        static readonly string[] SdRisk = { "멜루나", "죠안", "에피카", "아멜리아", "알레트", "키샤", "마요", "프리클", "비비_신성", "우이", "이드", "리스티" };

        IEnumerator SdSize_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.0f);

            // ① 비교 판
            var layer = Ui.Rect("modal sdsize", f.Stage.ModalLayer).Fill();
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            var size = f.Stage.Size;
            float body = size.y * 0.24f;
            var list = SdRisk.Select(Roster.ByKey).Where(h => h != null).ToList();
            int cols = Mathf.Min(6, list.Count), rows = Mathf.CeilToInt(list.Count / (float)cols);
            float cw = size.x / cols, rh = size.y / rows;
            float k = Mathf.Min(1f, rh / (body * 1.55f));
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                float cx = (i % cols + 0.5f) * cw, fy = size.y - (i / cols + 1) * rh + 30;
                var line = Ui.Img(layer, Theme.White, Theme.Line, "floor"); line.rectTransform.At(0, 0, cx - cw * 0.45f, fy, cw * 0.9f, 1);
                var top = Ui.Img(layer, Theme.White, Theme.Gold.A(0.5f), "body"); top.rectTransform.At(0, 0, cx - cw * 0.45f, fy + body * k, cw * 0.9f, 1);   // 몸 키(636 단위) 선
                var a = Ui.Rect("battle " + h.key, layer).At(0, 0, cx - cw * 0.22f, fy, 10, 10);
                var ga = SpineUi.Battle(a, h, body * k, true);
                var b = Ui.Rect("scene " + h.key, layer).At(0, 0, cx + cw * 0.22f, fy, 10, 10);
                var box = Ui.Rect("box", layer); box.anchorMin = box.anchorMax = box.pivot = Vector2.zero; box.sizeDelta = new Vector2(cw * 0.5f, rh - 40); box.anchoredPosition = new Vector2(cx, fy);
                var gb = SceneHero.Make(b, h, body * k, false, 0, false);
                float s = gb != null ? SpineUi.ClampInto(gb, box, f.Stage.Root, 0.7f) : -1;
                Expect(ga == null || gb == null || Mathf.Approximately(Mathf.Abs(ga.rectTransform.localScale.y), Mathf.Abs(gb.rectTransform.localScale.y)), $"장면 SD {h.ko} — 전투 배율과 같다(ClampInto {s:0.00})");
                var t = Ui.Text(layer, $"{h.ko}  <size=70%><color={Theme.SubTag}>전투 | 장면</color></size>", 15, Theme.Ink, TMPro.TextAlignmentOptions.Center);
                t.rectTransform.At(0, 0, cx - cw / 2, fy - 28, cw, 24);
            }
            yield return Wait(1.0f);
            yield return Shot("sd_compare");
            Destroy(layer.gameObject);

            // ② 휴식 — 위험 사도 셋씩 파티로
            var core = list.Select(h => h.CoreId).Where(x => x != null).ToList();
            var party0 = f.P.S.Party.ToList();
            for (int g = 0; g * 3 < core.Count && g < 2; g++)
            {
                var three = core.Skip(g * 3).Take(3).ToList();
                if (three.Count < 3) three.AddRange(party0.Where(x => !three.Contains(x)).Take(3 - three.Count));
                f.P.S.Party.Clear(); f.P.S.Party.AddRange(three);
                f.P.S.Camp = null;
                f.Camp("camp");
                yield return Screen_("camp", 1.4f);
                yield return Shot("sd_camp_" + g);
            }

            // ③ 이벤트 M5 — 대상 멜루나(+ 파티도 위험 사도)
            if (f.P.Data.Event("M5") != null)
            {
                string key = $"{f.P.S.Floor}:{(f.P.S.Map?.At ?? f.P.S.Node.ToString())}";
                f.P.S.Event = new Core.EventState { Key = key, Choices = new List<string> { "M5" }, Id = "M5" };
                f.EventStop();
                yield return Screen_("event", 1.6f);
                yield return Shot("sd_event_meluna");
            }
            else Debug.LogWarning("[Demo] 이벤트 M5 없음");
            f.P.S.Party.Clear(); f.P.S.Party.AddRange(party0);
            Debug.Log($"[Demo] 장면 SD 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }
    }
}
