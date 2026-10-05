using UnityEngine;

namespace Bolzena.Demo
{
    // 자동 데모 지킴이 — 예외가 한 번이라도 나거나 시간(실제 시계)이 넘으면 오류 코드로 스스로 꺼진다.
    // 데모 창이 먹통으로 남지 않게(-demo 일 때 BattleDirector.Awake 가 가장 먼저 단다).
    //   종료 코드: 0 정상 · 3 예외 · 4 시간 초과
    public class DemoGuard : MonoBehaviour
    {
        public static float Limit = 300f;
        static bool quitting;

        public static void Install()
        {
            if (FindAnyObjectByType<DemoGuard>() != null) return;
            var go = new GameObject("DemoGuard");
            DontDestroyOnLoad(go);
            go.AddComponent<DemoGuard>();
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (quitting || type != LogType.Exception) return;
            quitting = true;
            Debug.LogError("[Demo] 예외 — 데모를 끝낸다(코드 3): " + msg);
            Application.Quit(3);
        }

        void Update()
        {
            if (quitting || Time.realtimeSinceStartup < Limit) return;
            quitting = true;
            Debug.LogError("[Demo] 시간 초과 " + Limit + "초 — 데모를 끝낸다(코드 4)");
            Application.Quit(4);
        }
    }
}
