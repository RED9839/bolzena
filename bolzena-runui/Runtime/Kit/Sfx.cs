using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 판 화면 효과음 — 원작 소리(Resources/RunArt/Sfx/<키> — 본 게임은 bolzena-unity Tools/copy_run_assets.py 가 넣는다)가 있으면 울리고 없으면 조용하다.
    //   크기 = 부른 값 × 설정 「효과음」(전체 소리는 AudioListener.volume 이 곱한다)
    public static class Sfx
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static bool told;

        public static void Play(string key, float vol = 1)
        {
            if (!clips.TryGetValue(key, out var c)) clips[key] = c = Resources.Load<AudioClip>("RunArt/Sfx/" + key);
            if (c == null)
            {
                if (!told) { told = true; Debug.LogWarning("[Audio] 판 화면 효과음 없음: RunArt/Sfx/" + key + " — copy_run_assets.py 를 돌렸는지"); }
                return;
            }
            if (src == null)
            {
                var go = new GameObject("RunUI.Sfx");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
            }
            src.PlayOneShot(c, vol * Settings.SfxVol);
        }

        public static void Click() => Play("click", 0.45f);   // 단추마다 울리므로 작게(웹판 ui.click 0.35 쯤)
    }
}
