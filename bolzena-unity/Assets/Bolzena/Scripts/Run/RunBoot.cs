using System;
using Bolzena.RunUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using RunDemo = Bolzena.RunUI.Demo;

namespace Bolzena
{
    // Run 장면(첫 장면)에 하나 — 판 화면(Flow)을 한 번만 세워 장면을 넘어 살게 하고, 싸움은 BattleBridge 로 Battle 장면에 넘긴다.
    // 전투에서 돌아와 Run 장면이 다시 열리면 이미 있는 Flow 를 그대로 쓴다.
    //   -demo           한 판 자동 데모(판 화면 데모 + 전투 봇)
    //   -battle         전투 시범만(Battle 장면 · 시범 파티) — -demo 와 같이 쓰면 예전 전투 데모
    //   -demo-timeout N 데모 지킴이 시간(초, 기본 1500)
    public class RunBoot : MonoBehaviour
    {
        void Awake()
        {
            Demo.InputProbe.Install();     // -inputprobe(사람 입력 재현)
            if (Flow.Me != null) return;   // 전투에서 돌아왔다
            DisplayOptions.RenderScaleHook = ApplyRenderScale;
            DisplayOptions.LoadAndApply(); // 화면 설정(창 모드 · 해상도 · 프레임 · 수직동기) — 판 · 전투가 같은 값. 명령줄 크기가 있으면 프레임만
            var args = Environment.GetCommandLineArgs();
            bool demo = Array.IndexOf(args, "-demo") >= 0;
            if (Array.IndexOf(args, "-battle") >= 0) { SceneManager.LoadScene(BattleBridge.BattleScene); return; }
            if (demo)
            {
                int i = Array.IndexOf(args, "-demo-timeout");
                Demo.DemoGuard.Limit = i >= 0 && i < args.Length - 1 && float.TryParse(args[i + 1], out var lim) ? lim + 30f : 1530f;
                Demo.DemoGuard.Install();
                RunDemo.Active = true;
            }
            var f = Flow.Boot();
            DontDestroyOnLoad(f.gameObject);
            f.FightScreen = new BattleBridge();
            if (demo) RunDemo.Attach(f);
        }

        // 테두리 없는 창 모드에서 고른 낮은 해상도 → URP 렌더 배율(백버퍼 · 판 화면 글은 모니터 해상도 그대로).
        // 품질 단계(PC · Mobile)마다 자산이 따로라 전부에 건다 — 전투가 PC 단계로 바꿔도 그대로. 1 미만이면 FSR 로 늘려 덜 뭉갠다.
        static void ApplyRenderScale(float k)
        {
            if (Application.isEditor) return;   // 에디터에서 바꾸면 자산 파일(.asset)에 남는다 — 플레이어에서만
            for (int i = 0; i < QualitySettings.count; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset a)
                {
                    a.renderScale = k;
                    a.upscalingFilter = k < 0.999f ? UnityEngine.Rendering.Universal.UpscalingFilterSelection.FSR : UnityEngine.Rendering.Universal.UpscalingFilterSelection.Auto;
                }
            Debug.Log($"[Display] 렌더 배율 {k:F2}");
        }
    }
}
