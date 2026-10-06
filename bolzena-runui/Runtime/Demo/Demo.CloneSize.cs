using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 클론 크기 점검(-demo-clonesize, 2026-10-06 「편성 화면에서 적 클론 화면 띄울 때 해상도에 따라 작아져야 하는데 안 작아짐」)
    //   마을 공개 → 편성(나오는 적 · 보스 칸) → 보스 클론 칸을 눌러 뜨는 적 상세 → 적 도감 보스 상세. 해상도마다 같은 이름으로 찍어 나란히 비교한다.
    public partial class Demo
    {
        IEnumerator CloneSize_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(2.0f);
            yield return Shot("clone_village");
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.8f);
            yield return Wait(0.6f);
            yield return Shot("clone_party");
            // 보스 클론 칸 — 적 정의에 Clone 이 있는 것
            var key = f.Stage.Hot.Keys.Where(k => k.StartsWith("partyfoe:") && f.Stage.Hot[k] != null)
                .FirstOrDefault(k => f.P.Data.Enemy(k.Substring("partyfoe:".Length))?.Clone != null);
            Expect(key != null, "편성 — 보스 클론 칸이 있다 " + (key ?? "없음"));
            if (key != null)
            {
                yield return Press(key, 1.0f);
                yield return Shot("clone_detail");
                // 그림 칸이 화면 높이 안 · 오른쪽 글을 가리지 않는다
                var well = f.Stage.ModalLayer.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "well");
                var root = f.Stage.Root;
                if (well != null)
                {
                    var c = new Vector3[4]; well.GetWorldCorners(c);
                    float hh = (root.InverseTransformPoint(c[1]).y - root.InverseTransformPoint(c[0]).y);
                    Expect(hh <= root.rect.height * 0.9f, $"클론 상세 그림 높이 {hh:0} / 캔버스 {root.rect.height:0}");
                }
                yield return Press("zoom.close", 0.6f);
            }
            // 지도 — 일반 · 엘리트 칸에 나오는 적 미리보기가 없다(2026-10-06 사용자: 모르고 들어가야 재미있다) · 보스 칸만 그 층 보스 클론 얼굴(같은 날 정정 「보스 클론은 보여 줘」) · 화면의 글 어느 것도 잘리지 않는다
            yield return Press("party.go");
            yield return Screen_("map", 1.6f);
            var cut = f.Stage.ScreenLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false).Where(t => t.isActiveAndEnabled && t.text.Length > 0).Where(t => { t.ForceMeshUpdate(); return t.isTextTruncated; }).Select(t => t.text).ToList();
            Expect(cut.Count == 0, $"지도 — 말줄임으로 잘린 글 {cut.Count}개 {string.Join(" / ", cut.Take(4))}");
            var mapAll = f.Stage.ScreenLayer.GetComponentsInChildren<RectTransform>(true);
            int icons = mapAll.Count(t => t.name == "foes" || t.name.StartsWith("foe "));
            var foeNames = new System.Collections.Generic.HashSet<string>(f.P.Data.Enemies.Values.Select(e => e.Name).Where(x => !string.IsNullOrEmpty(x)));
            int named = f.Stage.ScreenLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Count(t => foeNames.Contains(t.text));
            Expect(icons == 0 && named == 0, $"지도 — 일반 칸 적 미리보기 없음 (적 그림 {icons} · 적 이름 글 {named})");
            var faces = mapAll.Where(t => t.name == "bossface").ToList();
            var bossNodes = f.P.Map().Rows.SelectMany(r => r).Where(n => n.Type == "boss").Select(n => n.Id).ToList();
            Expect(faces.Count == bossNodes.Count && faces.All(t => t.parent != null && bossNodes.Contains(t.parent.name)), $"지도 — 보스 칸만 클론 얼굴 ({faces.Count} / 보스 칸 {bossNodes.Count})");
            Expect(f.Stage.ScreenLayer.GetComponentsInChildren<RectTransform>(true).Any(t => t.name == "hud.foenature") || f.Stage.Root.GetComponentsInChildren<RectTransform>(true).Any(t => t.name == "hud.foenature"), "지도 — 머리에 「이번 모험의 적 속성」");
            yield return Shot("clone_map");
            // 지도 끝(보스 칸 — 클론 얼굴)까지 밀어 한 장 더
            var msr = f.Stage.ScreenLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            if (msr != null) { msr.horizontalNormalizedPosition = 1f; yield return Wait(0.5f); yield return Shot("clone_map_boss"); }
            Debug.Log($"[Demo] 캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0} · 화면 {Screen.width}x{Screen.height} · Compact {Theme.Compact}");
            Debug.Log($"[Demo] 클론 크기 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }
    }
}
