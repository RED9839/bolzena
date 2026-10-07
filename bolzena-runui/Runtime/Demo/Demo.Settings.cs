using System.Collections;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 설정 창 캡처(-demo-settings) — 로비에서 설정 창을 열어 탭 셋을 찍고, 모험 중 모양(「로비로」 단추)도 찍는다.
    //   값은 하나도 바꾸지 않는다(탭만 누른다) · 저장도 지우지 않는다(Demo.Start 가 이 묶음에서는 건너뛴다).
    //   창 높이에 다 들어가는지: 탭마다 [Settings] 줄에 칸 수 · 내용 높이 · 보이는 높이를 남긴다(넘치면 스크롤 — 잘리면 안 된다).
    public partial class Demo
    {
        IEnumerator Settings_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.2f);
            foreach (var inRun in new[] { false, true })
            {
                f.SettingsPanel(inRun);
                yield return Wait(0.8f);
                for (int t = 0; t < 3; t++)
                {
                    yield return Press("settings.tab" + t, 0.7f);
                    LogFit(t);
                    yield return Shot($"settings{(inRun ? "_run" : "")}_tab{t}");
                    if (t == 1 && !inRun)
                    {
                        // 저사양 모드 — 한 번 뒤집어 잠금 · 풀림 모양을 찍고 되돌린다(값은 처음 그대로 남는다)
                        bool was = DisplayOptions.LowSpec;
                        yield return Press("disp.low", 0.8f);
                        Expect(DisplayOptions.LowSpec != was, "저사양 모드 스위치가 값을 바꿈");
                        Debug.Log($"[Settings] 저사양 {(DisplayOptions.LowSpec ? "켬" : "끔")} · 렌더 배율 {DisplayOptions.RenderScale:F2} · 프레임 {Application.targetFrameRate} · 화면 효과 {DisplayOptions.PostFxOn}");
                        yield return Shot($"settings_tab1_low{(DisplayOptions.LowSpec ? "On" : "Off")}");
                        yield return Press("disp.low", 0.8f);
                        Expect(DisplayOptions.LowSpec == was, "저사양 모드가 처음 값으로 돌아옴");
                        Debug.Log($"[Settings] 저사양 {(DisplayOptions.LowSpec ? "켬" : "끔")} · 렌더 배율 {DisplayOptions.RenderScale:F2} · 프레임 {Application.targetFrameRate} · 화면 효과 {DisplayOptions.PostFxOn}");
                    }
                }
                yield return Press("settings.close", 0.6f);
            }
            Debug.Log("[Demo] 설정 창 단언 실패 " + fails);
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        void LogFit(int tab)
        {
            var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            if (sr == null) { Expect(false, "설정 창 스크롤 칸 없음"); return; }
            float contentH = sr.content.rect.height, viewH = sr.viewport.rect.height;
            int rows = sr.content.childCount;
            Debug.Log($"[Settings] 탭 {tab} · 줄 {rows} · 내용 {contentH:0} · 보이는 {viewH:0} · {(contentH <= viewH + 1 ? "다 보임" : "스크롤")} · 화면 {Screen.width}×{Screen.height} · 캔버스 {f.Stage.Size.x:0}×{f.Stage.Size.y:0}");
            Expect(rows > 0, $"탭 {tab} 에 항목이 있음");
        }
    }
}
