using System.Collections.Generic;
using UnityEngine;

namespace Bolzena
{
    // 효과음 · 목소리. 효과음은 Resources/Sfx/<키>, 사도 · 적 제 소리는 Sfx/hero|monster/<폴더>/<파일>, 목소리는 Voice/<사도>/<파일>.
    // 같은 소리가 너무 촘촘하면(45ms) 건너뛰고, 같은 소리를 잇달아 틀면 조금씩 작게(웹판 sfx.js 와 같은 생각).
    public class Sfx : MonoBehaviour
    {
        static Sfx inst;
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource voice;
        readonly Dictionary<string, float> lastAt = new Dictionary<string, float>();
        readonly Dictionary<string, AudioClip[]> voices = new Dictionary<string, AudioClip[]>();
        public static float Volume = 0.8f;
        /// <summary>점검(-ultaudit) — 실제로 튼 소리 이름(효과음 키 · 「voice:사도/클립」). 못 찾은 것은 「!」를 붙인다.</summary>
        public static System.Action<string> OnPlayed;

        static Sfx Ensure()
        {
            if (inst != null) return inst;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Sfx>();
            for (int i = 0; i < 16; i++) inst.pool.Add(go.AddComponent<AudioSource>());
            inst.voice = go.AddComponent<AudioSource>();
            foreach (var s in inst.pool) { s.playOnAwake = false; s.spatialBlend = 0; }
            inst.voice.playOnAwake = false;
            return inst;
        }

        public static void Play(string key, float vol = 1f, float pitch = 1f, bool jitter = false)
        {
            var me = Ensure();
            var clip = Res.Clip(key.Contains("/") ? "Sfx/" + key : "Sfx/" + key);
            OnPlayed?.Invoke(clip == null ? "!" + key : key);
            if (clip == null) return;
            float now = Clock.Now;
            if (me.lastAt.TryGetValue(key, out var t) && now - t < 0.045f) return;
            float k = 1f;
            if (me.lastAt.TryGetValue(key, out t) && now - t < 0.5f) k = 0.75f;
            me.lastAt[key] = now;
            AudioSource src = null;
            foreach (var s in me.pool) if (!s.isPlaying) { src = s; break; }
            if (src == null) src = me.pool[0];
            src.pitch = pitch * (jitter ? Random.Range(0.96f, 1.04f) : 1f);
            src.PlayOneShot(clip, vol * k * Volume);
        }

        // 따로 쥐는 소리 — 고학년 칸 소리처럼 몸짓이 끝나면 줄여 끊어야 하는 것(PlayOneShot 은 따로 못 줄인다). 못 틀면 null
        public static AudioSource PlayHeld(string key, float vol = 1f)
        {
            var me = Ensure();
            var clip = Res.Clip("Sfx/" + key);
            OnPlayed?.Invoke(clip == null ? "!" + key : key);
            if (clip == null) return null;
            var src = me.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0;
            src.clip = clip; src.volume = vol * Volume;
            src.Play();
            Destroy(src, clip.length + 0.1f);
            return src;
        }

        // 줄여 끊기 — sec 동안 0 으로(웹판 sfx.js 꼬리 규칙과 같은 생각). 이미 끝난 것은 그대로
        public static System.Collections.IEnumerator FadeOut(AudioSource s, float sec)
        {
            if (s == null || !s.isPlaying) yield break;
            float v0 = s.volume, t = 0;
            while (s != null && t < sec) { t += Time.unscaledDeltaTime; s.volume = v0 * (1 - t / sec); yield return null; }
            if (s != null) s.Stop();
        }

        // 미리 불러 두기 — 첫 고학년에서 목소리 묶음을 처음 풀며 멈칫하지 않게
        // 목소리 묶음은 최근 사도 몇만 — 넘치면 비운다(싸움을 열 때 파티 것을 다시 채운다). 다 붙들면 사도가 바뀔 때마다 쌓였다
        const int VoiceKeep = 12;
        static void KeepVoices(Sfx me) { if (me.voices.Count >= VoiceKeep) me.voices.Clear(); }

        public static void Preload(string hero)
        {
            var me = Ensure();
            if (!me.voices.ContainsKey(hero)) { KeepVoices(me); me.voices[hero] = Resources.LoadAll<AudioClip>("Voice/" + hero); }
            foreach (var c in me.voices[hero]) c.LoadAudioData();
            foreach (var c in Resources.LoadAll<AudioClip>("Sfx/hero/" + hero)) c.LoadAudioData();
        }

        // 사도 목소리 — 이름이 prefix 로 시작하는 것 중 하나(앞의 것이 없으면 다음 prefix)
        public static void Voice(string hero, params string[] prefixes)
        {
            var me = Ensure();
            if (!me.voices.TryGetValue(hero, out var all))
            {
                all = Resources.LoadAll<AudioClip>("Voice/" + hero);
                KeepVoices(me);
                me.voices[hero] = all;
            }
            foreach (var p in prefixes)
            {
                var hits = new List<AudioClip>();
                foreach (var c in all) if (c.name.StartsWith(p)) hits.Add(c);
                if (hits.Count == 0) continue;
                me.voice.Stop();
                me.voice.clip = hits[Random.Range(0, hits.Count)];
                me.voice.volume = 0.9f * Volume;
                me.voice.Play();
                OnPlayed?.Invoke("voice:" + hero + "/" + me.voice.clip.name);
                return;
            }
            OnPlayed?.Invoke("!voice:" + hero);
        }
    }
}
