using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 사도 목소리 찾기 — 기본은 Resources("Voice/<그림 키>"). 본 게임 웹 빌드는 목소리를 첫 로딩에서 빼 사도별 번들로 받으므로(bolzena-unity WebVoice)
    //   번들 층이 Find · Has · Want 를 꽂는다. 꽂지 않으면(PC · 에디터 · 시험 프로젝트) 예전과 똑같이 Resources 만 본다.
    //   받기 전이면 빈 배열을 주고 받기를 건다(이 빈 배열은 캐시하지 말 것 — Pending). 부르는 쪽은 Defer 로 「받아지면 바로 틀기」를 걸어 둔다
    //   (2026-10-10 「몇몇 사도는 편성에서 고를 때 목소리가 안 난다」 — 처음 칸에 넣는 순간 받는 중이라 버려졌다).
    public static class VoiceSource
    {
        /// <summary>받아 둔 번들에서 — 있으면 클립들, 없으면 null.</summary>
        public static System.Func<string, AudioClip[]> Find;
        /// <summary>번들 목록에 그 사도가 있는가(받기 전이어도).</summary>
        public static System.Func<string, bool> Has;
        /// <summary>받기를 건다(사도, 낮은 우선순위) — 이미 받았거나 받는 중이면 아무것도 안 한다.</summary>
        public static System.Action<string, bool> Want;

        static readonly AudioClip[] none = new AudioClip[0];

        /// <summary>번들 층이 꽂혀 있다 — 부르는 쪽은 자기 캐시를 쓰지 말고 늘 Load 한다(번들 층이 쓰임 순서로 내리고 올린다).</summary>
        public static bool Layered => Has != null;

        public static AudioClip[] Load(string art)
        {
            if (string.IsNullOrEmpty(art)) return none;
            var a = Find?.Invoke(art);
            if (a != null) return a;
            if (Has != null && Has(art)) { Want?.Invoke(art, false); return none; }
            return Resources.LoadAll<AudioClip>("Voice/" + art);
        }

        /// <summary>번들 목록에 있는데 아직 못 받았다 — 이때 받은 빈 배열은 붙들어 두지 않는다.</summary>
        public static bool Pending(string art) => Has != null && !string.IsNullOrEmpty(art) && Has(art) && Find?.Invoke(art) == null;

        /// <summary>미리 받기 — low 면 화면 목록용(급한 받기 뒤로). 번들 층이 없으면 아무것도 안 한다.</summary>
        public static void Prefetch(string art, bool low = false) { if (!string.IsNullOrEmpty(art) && Has != null && Has(art) && Find?.Invoke(art) == null) Want?.Invoke(art, low); }

        // ── 받아지면 바로 틀기 ──
        //   통로(편성 · 로비 = "run", 전투 = "battle")마다 하나만 — 새로 걸면 앞의 것은 버린다. 요청 뒤 Window 초 안에 받아졌고,
        //   그새 그 통로에서 다른 목소리가 나지 않았을 때(stamp 가 같을 때)만 다시 부른다.
        public const float Window = 1.5f;
        class Waiting { public string art; public float at; public int stamp; public System.Func<int> now; public System.Action replay; }
        static readonly Dictionary<string, Waiting> waiting = new Dictionary<string, Waiting>();

        public static void Defer(string channel, string art, int stamp, System.Func<int> now, System.Action replay)
        {
            waiting[channel] = new Waiting { art = art, at = Time.realtimeSinceStartup, stamp = stamp, now = now, replay = replay };
        }

        /// <summary>번들 층이 그 사도를 받았을 때 부른다.</summary>
        public static void NotifyReady(string art)
        {
            List<string> hit = null;
            foreach (var kv in waiting) if (kv.Value.art == art) (hit ??= new List<string>()).Add(kv.Key);
            if (hit == null) return;
            foreach (var ch in hit)
            {
                var w = waiting[ch];
                waiting.Remove(ch);
                float late = Time.realtimeSinceStartup - w.at;
                if (late <= Window && w.now() == w.stamp) { Debug.Log($"[Voice] 받아져서 이어 틀기 {art} ({ch} · {late:F2}s 뒤)"); w.replay(); }
                else Debug.Log($"[Voice] 받아졌지만 버림 {art} ({ch} · {late:F2}s 뒤{(w.now() != w.stamp ? " · 그새 다른 목소리" : "")})");
            }
        }
    }
}
