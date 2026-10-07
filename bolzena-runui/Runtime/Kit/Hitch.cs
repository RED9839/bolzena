using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Bolzena.RunUI
{
    // 멈춤 저울(-hitch, 2026-10-07 「처음 모험에 진입하면 응답 없음 · 검은 화면」) — 메인 스레드가 한 프레임에 오래 막힌 곳을 숫자로.
    //   · 프레임마다 걸린 시간을 세 몫으로 나눈다: 앞(장면 읽기 · Start · 입력 — 맨 앞 Update 전까지) · 스크립트(Update ~ LateUpdate) · 그리기(LateUpdate 뒤 ~ 프레임 끝)
    //   · 코드가 「using (Hitch.Span("이름"))」 으로 감싼 일은 그 프레임 몫에 이름별로 더한다 → 긴 프레임(기본 100ms 넘음)이면 [Hitch] 한 줄에 몫과 큰 이름들을 적는다
    //   · Hitch.Mark("구간") 은 구간 경계 — 앞 구간의 걸린 시간 · 최장 프레임 · 막힌 합(50ms 넘는 프레임의 합)을 [Hitch] 구간 줄로 남긴다
    //   꺼져 있으면(인자 없음) Span 은 아무것도 하지 않는다(할당 없음).
    public static class Hitch
    {
        public static readonly bool On = Array.IndexOf(Environment.GetCommandLineArgs(), "-hitch") >= 0;
        /// <summary>이 길이(ms)를 넘는 프레임을 적는다.</summary>
        public static double LogOver = 100;

        static readonly Stopwatch sw = Stopwatch.StartNew();
        public static double Now => sw.Elapsed.TotalMilliseconds;

        // 이번 프레임에 쌓인 이름별 시간
        static readonly Dictionary<string, double> frame = new Dictionary<string, double>();
        static readonly Dictionary<string, double> segSpans = new Dictionary<string, double>();
        static int depth;

        public struct Scope : IDisposable
        {
            readonly string name; readonly double t0;
            internal Scope(string n) { name = n; t0 = Now; depth++; }
            public void Dispose()
            {
                if (name == null) return;
                depth--;
                double d = Now - t0;
                frame[name] = (frame.TryGetValue(name, out var v) ? v : 0) + d;
                segSpans[name] = (segSpans.TryGetValue(name, out var s) ? s : 0) + d;
            }
        }

        /// <summary>이름 붙인 일 — using 으로 감싼다. 꺼져 있으면 빈 것.</summary>
        public static Scope Span(string name) => On ? new Scope(name) : default;

        // ── 구간 ──
        static string segName = "부팅";
        static double segAt, segWorst, segBlocked; static int segFrames, segOver500;
        static string segWorstWhat = "-";

        /// <summary>구간 경계 — 앞 구간을 닫아 적고 새 구간을 연다.</summary>
        public static void Mark(string name)
        {
            if (!On) return;
            Ensure();
            double now = Now;
            var top = string.Join(" · ", segSpans.OrderByDescending(k => k.Value).Take(6).Select(k => $"{k.Key} {k.Value:F0}"));
            Debug.Log($"[Hitch] 구간 「{segName}」 {now - segAt:F0}ms · 프레임 {segFrames} · 최장 {segWorst:F0}ms({segWorstWhat}) · 막힌 합 {segBlocked:F0}ms · 0.5초 넘음 {segOver500} · 이름 합 {(top.Length > 0 ? top : "-")}");
            segName = name; segAt = now; segWorst = segBlocked = 0; segFrames = segOver500 = 0; segWorstWhat = "-";
            segSpans.Clear();
            Debug.Log($"[Hitch] → {name}");
        }

        /// <summary>점검이 첫 입력을 기다리기 시작한 때(전투) — 한 번만 적는다.</summary>
        public static void FirstInput()
        {
            if (!On || firstInputDone) return;
            firstInputDone = true;
            Debug.Log("[Hitch] 첫 입력");
            Mark("전투(첫 입력 뒤)");
        }
        static bool firstInputDone;

        static Probe probe;
        public static void Ensure()
        {
            if (!On || probe != null) return;
            var go = new GameObject("Hitch probe");
            UnityEngine.Object.DontDestroyOnLoad(go);
            probe = go.AddComponent<Probe>();
            go.AddComponent<Late>();
            segAt = Now;
        }

        static double lastEnd = -1, updAt, lateAt;

        [DefaultExecutionOrder(-32000)]
        sealed class Probe : MonoBehaviour
        {
            void Start() { StartCoroutine(Eof()); }
            void Update() { updAt = Now; }

            IEnumerator Eof()
            {
                var w = new WaitForEndOfFrame();
                while (true)
                {
                    yield return w;
                    double end = Now;
                    if (lastEnd >= 0) Close(end);
                    lastEnd = end;
                    frame.Clear();
                }
            }
        }

        [DefaultExecutionOrder(32000)]
        sealed class Late : MonoBehaviour { void LateUpdate() { lateAt = Now; } }

        static void Close(double end)
        {
            double dt = end - lastEnd;
            segFrames++;
            if (dt > 50) segBlocked += dt;
            if (dt > 500) segOver500++;
            double pre = Math.Max(0, updAt - lastEnd), scr = Math.Max(0, lateAt - updAt), draw = Math.Max(0, end - lateAt);
            string top = frame.Count == 0 ? "-" : string.Join(" · ", frame.OrderByDescending(k => k.Value).Take(8).Select(k => $"{k.Key} {k.Value:F0}"));
            if (dt > segWorst) { segWorst = dt; segWorstWhat = frame.Count == 0 ? (pre > scr && pre > draw ? "앞" : draw > scr ? "그리기" : "스크립트") : frame.OrderByDescending(k => k.Value).First().Key; }
            if (dt > LogOver) Debug.Log($"[Hitch] 프레임 {dt:F0}ms 「{segName}」 앞 {pre:F0} · 스크립트 {scr:F0} · 그리기 {draw:F0} — {top}");
        }
    }
}
