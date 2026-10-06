using UnityEngine;
using UnityEngine.Profiling;

namespace Bolzena.RunUI
{
    // 메모리 한 줄(재기용) — 「[Mem] 이름 mono=… alloc=… reserved=…」. 웹 하네스(run_mem.mjs)는 이 줄을 보면 WASM 힙 · JS 힙을 같이 적는다.
    //   mono = 관리 힙에서 쓰는 몫(GC.GetTotalMemory) · monoHeap = 관리 힙 크기 · alloc/reserved = 유니티 네이티브 할당 · st = 붙든 스탠딩 데이터 · baked = 구운 수 · queue = 굽기 대기열
    //   PC 프로세스 전체(개인 메모리)는 하네스(pc_mem.ps1)가 PID 로 잰다.
    public static class MemLog
    {
        const float MB = 1024f * 1024f;

        public static void Log(string tag)
        {
            Debug.Log($"[Mem] {tag} mono={System.GC.GetTotalMemory(false) / MB:0} monoHeap={Profiler.GetMonoHeapSizeLong() / MB:0} alloc={Profiler.GetTotalAllocatedMemoryLong() / MB:0} reserved={Profiler.GetTotalReservedMemoryLong() / MB:0} st={SpineUi.Held} baked={StandingSnap.Baked} queue={StandingSnap.Pending}");
        }
    }
}
