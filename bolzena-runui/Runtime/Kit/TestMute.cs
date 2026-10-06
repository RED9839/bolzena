using UnityEngine;

namespace Bolzena.RunUI
{
    // 시험 실행은 소리 없이(2026-10 사용자 규칙) — 데모 · 점검 · 캡처 인자로 띄우면 AudioListener.volume = 0.
    //   소리를 확인하는 점검만 -sound 를 붙여 켠다. 사용자 설정(PlayerPrefs 볼륨)은 건드리지 않는다 — 실행 인자로만 정한다.
    //   판 화면 시험 프로젝트 · 본 게임이 이 패키지를 같이 쓰므로 둘 다 이것 하나로 된다(장면을 바꿔도 매 프레임 지킨다).
    public class TestMute : MonoBehaviour
    {
        // 이 인자가 있을 때만 음소거 — 배포판(웹 · PC)은 인자가 없으니 늘 소리가 난다. -mute 는 그 밖의 점검을 그냥 음소거로 띄울 때
        static readonly string[] TestArgs = { "-mute", "-demo","-battle", "-ultaudit", "-cutinaudit", "-artaudit", "-toughshots", "-demo-quick", "-demo-roster", "-perf", "-humanfight" };
        public static bool On { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var a = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(a, "-sound") >= 0) return;
            bool test = false;
            foreach (var t in TestArgs) if (System.Array.IndexOf(a, t) >= 0) { test = true; break; }
            if (!test) return;
            On = true;
            var go = new GameObject("TestMute");
            DontDestroyOnLoad(go);
            go.AddComponent<TestMute>();
            AudioListener.volume = 0;
            Debug.Log("[TestMute] 시험 실행 — 소리 0(-sound 로 켠다)");
        }

        void LateUpdate() { if (AudioListener.volume != 0) AudioListener.volume = 0; }
    }
}
