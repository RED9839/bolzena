using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace Bolzena.Demo
{
    // 멈칫 원인 재기(-ultprobe, 개발 빌드에서) — 알려진 프로파일러 마커를 모두 ProfilerRecorder 로 걸어 두고,
    //   튀는 프레임이 오면 그 앞 프레임(Update 사이에 끝난 프레임)에서 시간을 가장 많이 먹은 마커를 로그에 적는다.
    //   마커는 중첩이라 합이 프레임보다 클 수 있다(부모 · 자식이 같이 나온다). GC 횟수 · 할당도 같이.
    public class UltProbe
    {
        readonly List<(string name, ProfilerRecorder r)> recs = new List<(string, ProfilerRecorder)>();
        readonly HashSet<string> seen = new HashSet<string>();
        ProfilerRecorder gcAlloc;
        int gc0, refreshIn;

        public UltProbe()
        {
            gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            gc0 = System.GC.CollectionCount(0);
            Refresh();
            Debug.Log($"[UltProbe] 마커 {recs.Count}개 기록 · 개발 빌드 {Debug.isDebugBuild}");
        }

        // 새로 생긴 마커도(처음 쓰는 기능이 마커를 늦게 등록한다) — 1초마다 다시 훑는다
        void Refresh()
        {
            var hs = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(hs);
            foreach (var h in hs)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                string key = d.Category.Name + "/" + d.Name;
                if (!seen.Add(key)) continue;
                var r = new ProfilerRecorder(h, 1, ProfilerRecorderOptions.SumAllSamplesInFrame | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                r.Start();
                recs.Add((d.Name, r));
            }
        }

        public void Frame()
        {
            if (--refreshIn <= 0) { refreshIn = 60; Refresh(); }
        }

        // 고학년이 시작할 때 읽혀 있던 텍스처 — 멈칫 때 그 뒤로 새로 읽힌 것(= GPU 로 새로 올린 것)을 적는다
        HashSet<Texture> texSeen;
        public void Mark()
        {
            texSeen = new HashSet<Texture>();
            foreach (var t in Resources.FindObjectsOfTypeAll<Texture>()) texSeen.Add(t);
        }

        public void Dump(int top = 14)
        {
            if (texSeen != null)
            {
                var nw = new List<string>();
                foreach (var t in Resources.FindObjectsOfTypeAll<Texture>())
                    if (texSeen.Add(t)) nw.Add($"{t.name}({t.GetType().Name} {t.width}x{t.height})");
                if (nw.Count > 0) Debug.Log($"[UltProbe] 새 텍스처 {nw.Count} — " + string.Join(", ", nw.Take(20)));
            }
            var list = new List<(string, double)>();
            foreach (var (n, r) in recs)
            {
                if (!r.Valid) continue;
                double ms = r.LastValue / 1e6;
                if (ms >= 0.5 && ms < 1000) list.Add((n, ms));   // 1초 넘는 값은 시간이 아닌 마커(잡 · 등록 수)
            }
            int gc = System.GC.CollectionCount(0);
            Debug.Log($"[UltProbe] GC {gc - gc0}회(누적) · 할당 {gcAlloc.LastValue / 1024}KB · " +
                      string.Join(" | ", list.OrderByDescending(x => x.Item2).Take(top).Select(x => $"{x.Item1} {x.Item2:F1}")));
        }
    }
}
