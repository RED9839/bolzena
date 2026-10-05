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

        // 미리 불러 두기 — 첫 고학년에서 목소리 묶음을 처음 풀며 멈칫하지 않게
        public static void Preload(string hero)
        {
            var me = Ensure();
            if (!me.voices.ContainsKey(hero)) me.voices[hero] = Resources.LoadAll<AudioClip>("Voice/" + hero);
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
                return;
            }
        }
    }
}
