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
        // 고학년 보기 — 컷인 건너뛰기 · 늘 짧게(끄면 판에서 처음 쓰는 고학년만 길게, 두 번째부터 짧게)
        public static bool SkipCutin { get => PlayerPrefs.GetInt("bz.skipCutin", 0) == 1; set => PlayerPrefs.SetInt("bz.skipCutin", value ? 1 : 0); }
        public static bool UltShort { get => PlayerPrefs.GetInt("bz.ultShort", 0) == 1; set => PlayerPrefs.SetInt("bz.ultShort", value ? 1 : 0); }
        public static bool NoShake { get => PlayerPrefs.GetInt("bz.noShake", 0) == 1; set => PlayerPrefs.SetInt("bz.noShake", value ? 1 : 0); }
        // 사도 말풍선 — 판 진행 중(휴식 · 상점 · 보상 · 이벤트 · 전투 승리 · 엘리트/보스 시작 · 위기) 사도 한마디(2026-10-09, 기본 켬)
        public static bool HeroTalk { get => PlayerPrefs.GetInt("bz.heroTalk", 1) == 1; set => PlayerPrefs.SetInt("bz.heroTalk", value ? 1 : 0); }
        public static string LobbyHero { get => PlayerPrefs.GetString("bz.lobbyHero", "에르핀"); set => PlayerPrefs.SetString("bz.lobbyHero", value); }
        public static float TextScale => BigText ? 1.12f : 1f;

        public static void Apply()
        {
            AudioListener.volume = Master;
            // 소리 점검(헤드리스 하네스가 이 줄을 읽는다) — 배포판은 시험 음소거(TestMute)가 꺼져 있어야 한다
            Debug.Log($"[Audio] 전체 {Master:0.00} · 효과음 {SfxVol:0.00} · 목소리 {Voice:0.00} · AudioListener.volume {AudioListener.volume:0.00} · pause {AudioListener.pause} · 시험 음소거 {(TestMute.On ? "켬" : "꺼짐")}");
        }
    }
}
