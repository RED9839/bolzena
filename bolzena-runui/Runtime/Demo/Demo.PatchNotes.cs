using System.Collections;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 업데이트 내용 창 캡처(-demo-patch) — 로비 오른쪽 위 버전 알약 · 창 맨 위 · 맨 아래(지난 판)를 찍는다. 저장 · 설정은 건드리지 않는다.
    public partial class Demo
    {
        IEnumerator Patch_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.4f);
            yield return Shot("patch_lobby");
            Expect(f.Stage.Hot.ContainsKey("lobby.version"), "로비에 버전 알약이 있음");
            Expect(PatchNotes.All.Count > 0, "패치노트를 읽음");
            Debug.Log($"[Patch] 버전 {PatchNotes.Version} · 판 {PatchNotes.All.Count} · Application.version {Application.version}");
            yield return Press("lobby.version", 0.9f);
            var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            Expect(sr != null, "업데이트 내용 창이 열림");
            if (sr != null) Debug.Log($"[Patch] 내용 {sr.content.rect.height:0} · 보이는 {sr.viewport.rect.height:0} · 화면 {Screen.width}×{Screen.height} · 캔버스 {f.Stage.Size.x:0}×{f.Stage.Size.y:0}");
            yield return Shot("patch_top");
            if (sr != null) { sr.verticalNormalizedPosition = 0; yield return Wait(0.4f); }
            yield return Shot("patch_bottom");
            yield return Press("patch.close", 0.6f);
            Debug.Log("[Demo] 업데이트 내용 창 단언 실패 " + fails);
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }
    }
}
