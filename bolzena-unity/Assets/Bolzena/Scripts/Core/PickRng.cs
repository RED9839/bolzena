using UnityEngine;

namespace Bolzena
{
    // 여럿 가운데 하나 고르기(고학년 갈래 · 카드 갈래 · 목소리) — 평소엔 UnityEngine.Random.
    //   점검(-ultaudit)이 씨앗을 걸면 「무엇을 고르나」(key)마다 값이 정해진다. 이펙트가 프레임마다 UnityEngine.Random 을 써서
    //   Random.InitState 로는 고르는 때의 값이 실행마다 달랐다(2026-11-17 같은 빌드 두 번 — 에피카 갈래 · 우이(기억) 목소리)
    public static class PickRng
    {
        static int? seed;
        public static void Seed(int s) => seed = s;
        public static void Clear() => seed = null;

        public static int Index(string key, int n)
        {
            if (seed == null) return Random.Range(0, n);
            unchecked
            {
                uint h = 2166136261;   // FNV-1a — string.GetHashCode 는 실행마다 다를 수 있다
                foreach (var c in key) h = (h ^ c) * 16777619;
                return new System.Random((int)((h ^ (uint)seed.Value * 2654435761u) & 0x7fffffff)).Next(n);
            }
        }
    }
}
