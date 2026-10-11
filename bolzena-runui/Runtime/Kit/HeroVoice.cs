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
        static int played;   // 이 통로에서 실제로 튼 수 — 받아지면 틀기(VoiceSource.Defer)가 그새 다른 목소리가 났는지 본다
        /// <summary>점검 — 부른 소리 이름(「voice:키/클립」, 못 찾으면 「!voice:키」).</summary>
        public static System.Action<string> OnPlayed;

        /// <summary>재생했으면 true(목소리 설정 0 · 파일 없음이면 false — 부르는 쪽이 말풍선을 대신 띄운다).</summary>
        public static bool Speak(HeroInfo h, params string[] prefixes)
        {
            if (h == null || string.IsNullOrEmpty(h.art)) return false;
            if (prefixes == null || prefixes.Length == 0) prefixes = new[] { "decksetting", "greeting" };
            if (Settings.Voice <= 0.001f) { Debug.Log($"[Voice] {h.key} 목소리 설정 0 — 재생 안 함"); return false; }
            if (VoiceSource.Layered) cache.Remove(h.art);   // 웹 번들 층 — 늘 새로(내린 번들의 클립을 붙들지 않게)
            if (!cache.TryGetValue(h.art, out var all))
            {
                if (cache.Count >= Keep) cache.Clear();
                all = VoiceSource.Load(h.art);   // 웹은 사도별 번들 — 받는 중이면 빈 것(소리 없이), 그건 붙들지 않는다
                if (!VoiceSource.Pending(h.art)) cache[h.art] = all;
            }
            if (VoiceSource.Pending(h.art))
            {
                // 웹 — 아직 받는 중: 버리지 않고 받아지면 바로 튼다(1.5초 안 · 그새 다른 목소리가 없을 때만)
                var ps = prefixes;
                VoiceSource.Defer("run", h.art, played, () => played, () => Speak(h, ps));
                Debug.Log($"[Voice] {h.key}({h.art}) 받는 중 — 받아지면 틀기");
                return false;
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
                played++;
                Debug.Log($"[Voice] 재생 {h.key} → {pick.name} (볼륨 {src.volume:0.00} · 전체 {AudioListener.volume:0.00})");
                OnPlayed?.Invoke($"voice:{h.art}/{pick.name}");
                return true;
            }
            Debug.Log($"[Voice] {h.key}({h.art}) 편성 목소리 없음");
            OnPlayed?.Invoke("!voice:" + h.art);
            return false;
        }

        /// <summary>이름이 정확히 같은 클립들을 차례로 잇는다(꿀밤 dutchrubend1 → dutchrubend2). 첫 클립이 없으면 fallback 접두사로 Speak.</summary>
        public static bool SpeakSeq(HeroInfo h, string[] names, params string[] fallback)
        {
            if (h == null || string.IsNullOrEmpty(h.art) || names == null || names.Length == 0) return false;
            if (Settings.Voice <= 0.001f) return false;
            if (VoiceSource.Layered) cache.Remove(h.art);   // 웹 번들 층 — 늘 새로(내린 번들의 클립을 붙들지 않게)
            if (!cache.TryGetValue(h.art, out var all))
            {
                if (cache.Count >= Keep) cache.Clear();
                all = VoiceSource.Load(h.art);   // 웹은 사도별 번들 — 받는 중이면 빈 것(소리 없이), 그건 붙들지 않는다
                if (!VoiceSource.Pending(h.art)) cache[h.art] = all;
            }
            if (VoiceSource.Pending(h.art)) { VoiceSource.Defer("run", h.art, played, () => played, () => SpeakSeq(h, names, fallback)); return false; }
            var seq = new List<AudioClip>();
            foreach (var n in names) foreach (var c in all) if (c != null && c.name == n) { seq.Add(c); break; }
            if (seq.Count == 0) return Speak(h, fallback);
            // 첫 클립이 이미 긴 대사면(에르핀 3초 · 레테 2.4초 — 보통은 1초 안쪽의 「아야」) 두 클립이 같은 반응의 다른 판이다 — 하나만(2026-10-10 사용자)
            if (seq.Count > 1 && seq[0].length > 1.8f) return Speak(h, seq[UnityEngine.Random.Range(0, seq.Count)].name);
            bool ok = Speak(h, seq[0].name);
            if (ok && seq.Count > 1 && src != null) Runner().Queue(src, seq.GetRange(1, seq.Count - 1), h);
            return ok;
        }

        static SeqRunner runner;
        static SeqRunner Runner()
        {
            if (runner == null) runner = src.gameObject.AddComponent<SeqRunner>();
            return runner;
        }

        class SeqRunner : MonoBehaviour
        {
            Coroutine co;
            public void Queue(AudioSource s, List<AudioClip> rest, HeroInfo h)
            {
                if (co != null) StopCoroutine(co);
                co = StartCoroutine(Run(s, s.clip, rest, h));
            }
            public void Cancel() { if (co != null) StopCoroutine(co); co = null; }
            System.Collections.IEnumerator Run(AudioSource s, AudioClip first, List<AudioClip> rest, HeroInfo h)
            {
                var cur = first;
                foreach (var c in rest)
                {
                    while (s != null && s.isPlaying && s.clip == cur) yield return null;
                    if (s == null || s.clip != cur) yield break;   // 그사이 다른 목소리가 끼어들었다
                    s.clip = c; s.volume = 0.9f * Settings.Voice; s.Play(); cur = c;
                    OnPlayed?.Invoke($"voice:{h.art}/{c.name}");
                }
                co = null;
            }
        }

        public static void Stop() { if (runner != null) runner.Cancel(); if (src != null) src.Stop(); }
    }
}
