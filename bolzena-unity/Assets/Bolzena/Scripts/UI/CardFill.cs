using System.Collections.Generic;
using Bolzena.Battle;
using UnityEngine;

namespace Bolzena.UI
{
    // 카드 격자를 여러 프레임에 나눠 만든다 — 카드 한 장이 부품 60개 안팎이라 덱 20여 장을 한 프레임에 만들면 190ms 쯤 멈췄다(2026-11-24).
    //   자리 · 차례는 Grid 가 먼저 정해 두고(스크롤 길이도 바로 안다) 위에서부터 한 프레임에 BUDGET_MS 만큼, 적어도 한 장씩 만든다.
    public class CardFill : MonoBehaviour
    {
        const double BUDGET_MS = 5;
        struct Slot { public CardInfo Info; public Vector3 Pos; public float Scale; public int Order; }
        readonly Queue<Slot> wait = new Queue<Slot>();
        List<CardView> made;
        System.Action onAdded;
        public int Pending => wait.Count;
        int frames, total; float since;   // -winperf — 다 찰 때까지

        public static CardFill On(Transform parent, List<CardView> made, System.Action onAdded)
        {
            var f = parent.gameObject.AddComponent<CardFill>();
            f.made = made; f.onAdded = onAdded; f.since = Time.realtimeSinceStartup;
            return f;
        }

        public void Add(CardInfo info, Vector3 pos, float scale, int order) => wait.Enqueue(new Slot { Info = info, Pos = pos, Scale = scale, Order = order });

        /// <summary>창을 다시 그릴 때 — 지워질 격자가 그 프레임에 새 목록(made)에 카드를 더 넣지 않게.</summary>
        public static void Stop(Transform cards)
        {
            if (cards == null) return;
            var f = cards.GetComponent<CardFill>();
            if (f != null) { f.wait.Clear(); f.enabled = false; }
        }

        void Update()
        {
            if (wait.Count == 0) { enabled = false; return; }
            frames++; int n0 = wait.Count;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            do
            {
                var s = wait.Dequeue();
                var cv = CardView.Create(transform, s.Info); cv.ShowPin = false;
                cv.TargetPos = s.Pos; cv.TargetScale = s.Scale; cv.TargetRot = 0; cv.Snap();
                cv.SetOrder(s.Order);
                made.Add(cv);
            }
            while (wait.Count > 0 && (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency < BUDGET_MS);
            onAdded?.Invoke();
            total += n0 - wait.Count;
            if (WinPerf.On && wait.Count == 0) Debug.Log($"[WinPerf] 다 참 · 카드 {total} · {frames}프레임 · {(Time.realtimeSinceStartup - since) * 1000f:0}ms");
        }
    }
}
