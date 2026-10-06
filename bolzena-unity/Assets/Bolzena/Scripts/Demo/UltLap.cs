using System.Diagnostics;
using System.Text;

namespace Bolzena.Demo
{
    // 고학년 한 프레임 안의 단계별 시간(점검 -ultaudit 때만 켠다) — Begin → Lap("이름") … → End 가 로그 한 줄 [UltLap]
    public static class UltLap
    {
        static readonly Stopwatch sw = new Stopwatch();
        static StringBuilder log;
        static double total;
        static readonly bool on = UltAudit.On;    // 실행 인자는 한 번만 본다

        public static void Begin(string what)
        {
            if (!on) return;
            log = new StringBuilder(what).Append(" — ");
            total = 0;
            sw.Restart();
        }

        public static void Lap(string what)
        {
            if (log == null) return;
            double ms = sw.Elapsed.TotalMilliseconds;
            total += ms;
            log.Append(what).Append(' ').Append(ms.ToString("F1")).Append(" · ");
            sw.Restart();
        }

        public static void End()
        {
            if (log == null) return;
            Lap("끝");
            UnityEngine.Debug.Log("[UltLap] " + log.Append("합 ").Append(total.ToString("F1")).Append("ms"));
            log = null;
        }
    }
}
