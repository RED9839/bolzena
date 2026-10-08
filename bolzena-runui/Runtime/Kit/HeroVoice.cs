using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 판 화면의 사도 목소리 — 편성에서 사도를 칸에 넣을 때 한 번(웹판 party-screen: decksetting → greeting).
    //   파일은 Resources/Voice/<원작 키>/ (bolzena-unity Tools/copy_run_assets.py 가 넣는다). 없으면 조용하다.
    //   크기 = 0.9 × 설정 「목소리」(전체 소리 · 시험 음소거는 AudioListener.volume 이 곱한다). 앞 소리가 나던 중이면 끊고 새 소리만 낸다.
    public static class HeroVoice
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();
        const int Keep = 8;
        /// <summary>점검 — 부른 소리 이름(「voice:키/클립」, 못 찾으면 「!voice:키」).</summary>
        public static System.Action<string> OnPlayed;

        public static void Speak(HeroInfo h, params string[] prefixes)
        {
            if (h == null || string.IsNullOrEmpty(h.art)) return;
            if (prefixes == null || prefixes.Length == 0) prefixes = new[] { "decksetting", "greeting" };
            if (Settings.Voice <= 0.001f) { Debug.Log($"[Voice] {h.key} 목소리 설정 0 — 재생 안 함"); return; }
            if (!cache.TryGetValue(h.art, out var all))
            {
                if (cache.Count >= Keep) cache.Clear();
                cache[h.art] = all = Resources.LoadAll<AudioClip>("Voice/" + h.art);
            }
            if (src == null)
            {
                var go = new GameObject("RunUI.HeroVoice");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 0;
            }
            foreach (var p in prefixes)
            {
                var hits = new List<AudioClip>();
                foreach (var c in all) if (c != null && c.name.StartsWith(p)) hits.Add(c);
                if (hits.Count == 0) continue;
                var pick = hits[Random.Range(0, hits.Count)];
                src.Stop();
                src.clip = pick;
                src.volume = 0.9f * Settings.Voice;
                src.Play();
                Debug.Log($"[Voice] 재생 {h.key} → {pick.name} (볼륨 {src.volume:0.00} · 전체 {AudioListener.volume:0.00})");
                OnPlayed?.Invoke($"voice:{h.art}/{pick.name}");
                return;
            }
            Debug.Log($"[Voice] {h.key}({h.art}) 편성 목소리 없음");
            OnPlayed?.Invoke("!voice:" + h.art);
        }

        public static void Stop() { if (src != null) src.Stop(); }
    }
}
