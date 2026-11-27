using System.Collections;
using UnityEngine;

namespace Bolzena
{
    // -winperf(재기) — 카드가 많은 창(파티 창 카드 탭 · 더미)을 열 때: 여는 함수가 쓴 시간 · 그 뒤 20프레임 가운데 가장 느린 프레임 → 로그 [WinPerf]
    public static class WinPerf
    {
        public static readonly bool On = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-winperf") >= 0;
        public static long Begin() => On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

        public static void End(MonoBehaviour host, string what, int cards, long t0)
        {
            if (!On) return;
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            host.StartCoroutine(Watch(what, cards, ms));
        }

        static IEnumerator Watch(string what, int cards, double ms)
        {
            float worst = 0, sum = 0;
            for (int i = 0; i < 20; i++) { yield return null; float d = Time.unscaledDeltaTime * 1000f; sum += d; if (d > worst) worst = d; }
            Debug.Log($"[WinPerf] {what} · 카드 {cards} · 여는 함수 {ms:0.0}ms · 뒤 20프레임 최악 {worst:0.0}ms · 합 {sum:0}ms");
        }
    }
}
