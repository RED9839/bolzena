using UnityEngine;

namespace Bolzena.View
{
    // 저사양 모드(설정 창 「화면」 — DisplayOptions.LowSpec)의 이펙트 몫. 화면 효과 · 렌더 배율 · 프레임은 DisplayOptions 가 직접 건다.
    //   켜면: 원작 이펙트 입자 밀도 절반 · 동시 입자 상한 1500 → 700 · 큰 입자 겹침 상한 12 → 6장(FxRun),
    //         우리 터뜨림(Fx.Burst) 입자 수 절반, 내딛기 잔상 간격 두 배(잔상 하나가 스파인 하나라 무겁다).
    public static class LowSpecFx
    {
        public static bool On { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Bolzena.RunUI.DisplayOptions.LowSpecHook += Apply;
            Apply(Bolzena.RunUI.DisplayOptions.LowSpec);
        }

        static void Apply(bool on)
        {
            On = on;
            Bolzena.Fx.FxRun.Density = on ? 0.5f : 1f;
            Bolzena.Fx.FxRun.Cap = on ? 700 : 1500;
            Bolzena.Fx.FxRun.OverdrawBudget = on ? 6f : 12f;
        }

        /// <summary>터뜨림 입자 수 — 저사양이면 절반(최소 1).</summary>
        public static int Count(int n) => On ? Mathf.Max(1, Mathf.CeilToInt(n * 0.5f)) : n;

        /// <summary>잔상 간격(초) — 저사양이면 두 배.</summary>
        public static float GhostStep => On ? 0.09f : 0.045f;
    }
}
