using System;
using System.Collections;
using UnityEngine;

namespace Bolzena
{
    // 시간 — 히트스톱(잠깐 멈칫) · 슬로 모션은 Time.timeScale 로 건다(스파인 · 파티클 · 연출 코루틴이 함께 멈춘다).
    // 화면 흔들림 · 컷인처럼 멈춘 동안에도 움직여야 하는 것은 Unscaled 로 잰다.
    // 자동 데모는 Time.captureDeltaTime 을 걸어 한 프레임 = 1/30초로 고정한다 — 그래서 실제 시계(realtime)는 쓰지 않는다.
    public class Clock : MonoBehaviour
    {
        static Clock inst;
        float stopUntil, slowUntil, slowScale = 1f;
        public static float Now { get; private set; }          // 멈춤과 무관한 시간(초)
        public static float Speed = 1f;                         // 배속(1× · 2×) — 연출 시간이 모두 이만큼 빨라진다
        public static bool Paused;                              // 일시정지 — 싸움터 · 연출이 멈춘다(화면 UI 는 산다)
        public static float UDt => Paused ? 0f : Time.unscaledDeltaTime * Speed;   // 화면 시간(멈칫과 무관, 배속은 탄다)

        public static Clock Ensure()
        {
            if (inst != null) return inst;
            var go = new GameObject("Clock");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Clock>();
            return inst;
        }

        public static bool Asleep;                              // 전투 밖(판 화면) — timeScale 을 건드리지 않는다

        void Update()
        {
            if (Asleep) return;
            if (Paused) { Time.timeScale = 0f; return; }
            Now += Time.unscaledDeltaTime * Speed;
            float s = 1f;
            if (Now < slowUntil) s = slowScale;
            if (Now < stopUntil) s = 0.0001f;
            Time.timeScale = s * Speed;
        }

        // 맞는 순간 멈칫 — 겹치면 긴 쪽
        public static void HitStop(float sec)
        {
            Ensure();
            inst.stopUntil = Mathf.Max(inst.stopUntil, Now + sec / Mathf.Max(1f, Speed));   // 2× 면 멈칫도 반으로(배속 설정을 따른다)
            if (!Paused) Time.timeScale = 0.0001f;
        }

        public static void SlowMo(float scale, float sec)
        {
            Ensure();
            inst.slowScale = scale;
            inst.slowUntil = Mathf.Max(inst.slowUntil, Now + sec);
        }

        public static Coroutine Run(IEnumerator co) => Ensure().StartCoroutine(co);

        // 멈칫 · 슬로를 바로 푼다(입력을 기다리는 창이 열릴 때)
        public static void ClearStops() { if (inst != null) inst.stopUntil = inst.slowUntil = 0; if (!Paused) Time.timeScale = Speed; }

        // 전투 장면을 떠날 때 — 장면 밖에 사는 시계가 돌리던 연출(사라진 물체를 만지는)을 멈추고 시간을 되돌린다
        public static void Reset()
        {
            if (inst != null) { inst.StopAllCoroutines(); inst.stopUntil = inst.slowUntil = 0; }
            Paused = false;
            Asleep = true;
            Time.timeScale = 1f;
        }

        public static IEnumerator Wait(float sec)
        {
            float t = 0;
            while (t < sec) { yield return null; t += Time.deltaTime; }
        }

        public static IEnumerator WaitU(float sec)
        {
            float t = 0;
            while (t < sec) { yield return null; t += UDt; }
        }

        // 0→1 로 f 를 부른다
        public static IEnumerator Tween(float dur, Action<float> f, bool unscaled = false)
        {
            float t = 0;
            f(0);
            while (t < dur)
            {
                yield return null;
                t += unscaled ? UDt : Time.deltaTime;
                f(Mathf.Clamp01(t / dur));
            }
        }

        public static IEnumerator All(params IEnumerator[] cos)
        {
            int left = cos.Length;
            foreach (var c in cos) Run(Track(c, () => left--));
            while (left > 0) yield return null;
        }

        static IEnumerator Track(IEnumerator c, Action done)
        {
            yield return c;
            done();
        }
    }

    public static class Ease
    {
        public static float OutCubic(float t) { t = 1 - t; return 1 - t * t * t; }
        public static float InCubic(float t) => t * t * t;
        public static float InOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        public static float OutQuint(float t) { t = 1 - t; return 1 - t * t * t * t * t; }
        public static float OutExpo(float t) => t >= 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
        public static float InExpo(float t) => t <= 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
        public static float OutBack(float t, float s = 1.70158f) { t -= 1; return t * t * ((s + 1) * t + s) + 1; }
        public static float OutElastic(float t)
        {
            if (t <= 0 || t >= 1) return t;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
        }
        // 0→1→0 — 한 번 튀었다 돌아오기
        public static float Pulse(float t, float peak = 0.15f) => t < peak ? OutCubic(t / peak) : 1 - InOutCubic((t - peak) / (1 - peak));
    }
}
