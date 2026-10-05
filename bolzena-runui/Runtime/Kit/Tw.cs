using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 작은 트윈 — 화면 전환 페이드 · 등장 · 버튼 눌림. 대상이 사라지면 조용히 멈춘다.
    // 「움직임 줄이기」(설정)면 시간을 1/4 로 줄인다.
    public class Tw : MonoBehaviour
    {
        static Tw me;
        public static Tw Me
        {
            get
            {
                if (me == null)
                {
                    var go = new GameObject("RunUI.Tween");
                    DontDestroyOnLoad(go);
                    me = go.AddComponent<Tw>();
                }
                return me;
            }
        }

        public static float OutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
        public static float InOut(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        public static float Linear(float t) => t;

        /// <summary>dur 초 동안 f(0..1). owner 가 사라지면 멈춘다. 끝나면 done.</summary>
        public static Coroutine Run(UnityEngine.Object owner, float dur, Action<float> f, Func<float, float> ease = null, float delay = 0, Action done = null)
            => Me.StartCoroutine(Co(owner, dur, f, ease ?? OutCubic, delay, done));

        static IEnumerator Co(UnityEngine.Object owner, float dur, Action<float> f, Func<float, float> ease, float delay, Action done)
        {
            float k = Settings.ReduceMotion ? 0.25f : 1f;
            dur *= k; delay *= k;
            if (delay > 0)
            {
                f(ease(0));
                float w = 0;
                while (w < delay) { w += Time.unscaledDeltaTime; yield return null; if (owner == null) yield break; }
            }
            float t = 0;
            while (t < dur)
            {
                if (owner == null) yield break;
                f(ease(Mathf.Clamp01(t / dur)));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (owner == null) yield break;
            f(ease(1));
            done?.Invoke();
        }

        public static Coroutine After(float sec, Action a) => Me.StartCoroutine(AfterCo(sec, a));
        static IEnumerator AfterCo(float s, Action a) { float t = 0; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } a(); }

        // ── 자주 쓰는 것 ──
        public static void FadeIn(Component c, float dur = 0.3f, float delay = 0)
        {
            var g = c.Group();
            Run(c, dur, t => { if (g) g.alpha = t; }, OutCubic, delay);
        }

        /// <summary>아래(또는 dir)에서 떠오르며 나타난다.</summary>
        public static void Rise(RectTransform rt, float delay = 0, float dist = 26, float dur = 0.42f, Vector2? dir = null)
        {
            var g = rt.Group();
            var d = (dir ?? Vector2.down) * dist;
            var home = rt.anchoredPosition;
            g.alpha = 0;
            Run(rt, dur, t => { if (!rt) return; g.alpha = Mathf.Clamp01(t * 1.4f); rt.anchoredPosition = home + d * (1 - t); }, OutCubic, delay);
        }

        /// <summary>작게 시작해 튀어나온다.</summary>
        public static void Pop(RectTransform rt, float delay = 0, float from = 0.6f, float dur = 0.45f)
        {
            var g = rt.Group();
            g.alpha = 0;
            rt.localScale = Vector3.one * from;
            Run(rt, dur, t => { if (!rt) return; g.alpha = Mathf.Clamp01(t * 2.5f); rt.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1, t); }, OutBack, delay);
        }

        /// <summary>제자리에서 살짝 흔들린다(못 하는 동작).</summary>
        public static void Shake(RectTransform rt, float amp = 10)
        {
            var home = rt.anchoredPosition;
            Run(rt, 0.35f, t => { if (rt) rt.anchoredPosition = home + new Vector2(Mathf.Sin(t * 30) * amp * (1 - t), 0); }, Linear);
        }

        /// <summary>숨쉬기(끝없이) — 갈 수 있는 지도 칸 · 주 버튼 빛.</summary>
        public static void Breathe(Transform tr, float amp = 0.06f, float period = 1.6f, float phase = 0)
        {
            Me.StartCoroutine(BreatheCo(tr, amp, period, phase));
        }
        static IEnumerator BreatheCo(Transform tr, float amp, float period, float phase)
        {
            float t = phase;
            var baseS = tr.localScale;
            while (tr != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Settings.ReduceMotion ? 0 : amp;
                tr.localScale = baseS * (1 + Mathf.Sin(t / period * Mathf.PI * 2) * k);
                yield return null;
            }
        }

        public static void Pulse(UnityEngine.UI.Graphic g, float lo, float hi, float period = 1.4f, float phase = 0)
        {
            Me.StartCoroutine(PulseCo(g, lo, hi, period, phase));
        }
        static IEnumerator PulseCo(UnityEngine.UI.Graphic g, float lo, float hi, float period, float phase)
        {
            float t = phase;
            while (g != null)
            {
                t += Time.unscaledDeltaTime;
                var c = g.color;
                c.a = Mathf.Lerp(lo, hi, 0.5f + 0.5f * Mathf.Sin(t / period * Mathf.PI * 2));
                g.color = c;
                yield return null;
            }
        }
    }
}
