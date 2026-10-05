using UnityEngine;

namespace Bolzena.RunUI
{
    // 설정 — PlayerPrefs 에 남긴다(웹판 settings.js 의 판 화면 몫: 소리 · 움직임 줄이기 · 글자 크게 · 로비 사도 · 전체화면).
    public static class Settings
    {
        public static float Master { get => PlayerPrefs.GetFloat("bz.master", 0.8f); set { PlayerPrefs.SetFloat("bz.master", value); AudioListener.volume = value; } }
        public static float SfxVol { get => PlayerPrefs.GetFloat("bz.sfx", 0.8f); set => PlayerPrefs.SetFloat("bz.sfx", value); }
        public static float Voice { get => PlayerPrefs.GetFloat("bz.voice", 0.8f); set => PlayerPrefs.SetFloat("bz.voice", value); }
        public static bool ReduceMotion { get => PlayerPrefs.GetInt("bz.calm", 0) == 1; set => PlayerPrefs.SetInt("bz.calm", value ? 1 : 0); }
        public static bool BigText { get => PlayerPrefs.GetInt("bz.big", 0) == 1; set => PlayerPrefs.SetInt("bz.big", value ? 1 : 0); }
        public static string LobbyHero { get => PlayerPrefs.GetString("bz.lobbyHero", "에르핀"); set => PlayerPrefs.SetString("bz.lobbyHero", value); }
        public static float TextScale => BigText ? 1.12f : 1f;

        public static void Apply()
        {
            AudioListener.volume = Master;
        }
    }
}
