using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 판 화면 효과음 — 원작 소리(시험 프로젝트 Resources/RunArt/Sfx, copy_assets.py)가 있으면 울리고 없으면 조용하다.
    public static class Sfx
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static void Play(string key, float vol = 1)
        {
            if (!clips.TryGetValue(key, out var c)) clips[key] = c = Resources.Load<AudioClip>("RunArt/Sfx/" + key);
            if (c == null) return;
            if (src == null)
            {
                var go = new GameObject("RunUI.Sfx");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
            }
            src.PlayOneShot(c, vol * Settings.SfxVol);
        }

        public static void Click() => Play("click", 0.7f);
    }
}
