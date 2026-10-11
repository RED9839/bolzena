using System;
using System.Collections;
using System.Collections.Generic;
using Bolzena.RunUI;
using UnityEngine;
using UnityEngine.Networking;

namespace Bolzena
{
    // 웹 목소리 — 웹(WebGL) 빌드는 사도 목소리(Resources/Voice/<그림 키>)를 첫 로딩(.data)에서 빼고 사도별 번들(WebBuild/Voice/voice_<키>.bundle)로 받는다
    //   (2026-10-10 perf_20261009 C안 — ProjectSetup.SetupAndBuildWeb · WebBundleBuild.BuildVoices).
    //   · 처음에 목록(Voice/voice.json)만 받고, 로비 메인 사도 목소리를 미리 받는다.
    //   · 그 밖은 처음 부를 때(편성 칸에 넣기 · 로비 터치 · 싸움 열기의 소리 미리 읽기) 받기를 건다 — 받는 동안은 소리 없이 진행(멈추지 않는다).
    //   · 붙드는 번들은 최근 Keep 명 — 넘치면 오래된 것을 Unload(false)(이미 꺼낸 클립은 쓰는 곳이 놓을 때까지 산다).
    //   · 실패 · 오프라인이면 조용히(소리 없음) — 몇 번 다시 해 보고, 그래도 안 되면 잠시 뒤 다시 부를 때 또 해 본다.
    //   목록이 없는 판(PC · 에디터 · 목소리를 .data 에 넣은 웹 빌드)은 아무것도 안 한다(VoiceSource 가 Resources 를 본다).
    public static class WebVoice
    {
        [Serializable] class Entry { public string art; public string file; public string hash; public long size; }
        [Serializable] class Manifest { public string version; public Entry[] voices; }

        // 붙드는 사도 24명(편성 목록 미리 받기 몫 포함, 번들 합 15MB 남짓). 번들은 붙든 동안 열어 둔다 — 소리 데이터가 번들 파일 안에 있어
        //   받자마자 Unload(false) 하면 재생 때 「FMOD::Sound … File not found」 로 소리가 안 났다(2026-10-10 시험)
        const int Keep = 24, Lanes = 2, Tries = 3;
        static readonly Dictionary<string, Entry> byArt = new Dictionary<string, Entry>();
        static readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
        static readonly LinkedList<string> order = new LinkedList<string>();
        static readonly HashSet<string> busy = new HashSet<string>();
        static readonly Dictionary<string, float> failedAt = new Dictionary<string, float>();
        static readonly Queue<string> queue = new Queue<string>(), lowQueue = new Queue<string>();   // 급한 것(소리 요청 · 시작) · 목록 미리 받기
        static readonly HashSet<string> queued = new HashSet<string>(), lowOnly = new HashSet<string>();   // lowOnly — 목록 미리 받기로만 걸린 것
        static readonly Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();
        static int running;
        static string baseUrl;
        static Host host;
        class Host : MonoBehaviour { }

        public static bool Active { get; private set; }
        /// <summary>점검 — 받은 사도 수 · 받은 바이트.</summary>
        public static int Fetched { get; private set; }
        public static long FetchedBytes { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer || host != null) return;
            var go = new GameObject("WebVoice");
            UnityEngine.Object.DontDestroyOnLoad(go);
            host = go.AddComponent<Host>();
            host.StartCoroutine(Boot());
        }

        static string BaseUrl()
        {
            var u = Application.absoluteURL ?? "";
            int q = u.IndexOfAny(new[] { '?', '#' });
            if (q >= 0) u = u.Substring(0, q);
            return u.Substring(0, u.LastIndexOf('/') + 1) + "Voice/";
        }

        static IEnumerator Boot()
        {
            baseUrl = BaseUrl();
            Manifest m = null;
            for (int attempt = 1; attempt <= Tries && m == null; attempt++)
            {
                using (var r = UnityWebRequest.Get(baseUrl + "voice.json?t=" + DateTime.UtcNow.Ticks))
                {
                    yield return r.SendWebRequest();
                    if (r.result == UnityWebRequest.Result.Success)
                    {
                        // 목소리를 .data 에 넣은 판 — Pages 가 없는 파일에 index.html 을 200 으로 줄 수 있다. JSON 이 아니면 번들 없음
                        try { m = JsonUtility.FromJson<Manifest>(r.downloadHandler.text); } catch (Exception) { m = null; }
                        if (m?.voices == null) { Debug.Log("[WebVoice] 목소리 번들 없는 판 — Resources 만 쓴다"); yield break; }
                        break;
                    }
                    if (r.responseCode == 404) { Debug.Log("[WebVoice] 목록 없음 — Resources 만 쓴다"); yield break; }
                    Debug.LogWarning($"[WebVoice] 목록 실패({attempt}) {r.error}");
                }
                yield return new WaitForSecondsRealtime(2f * attempt);
            }
            if (m?.voices == null) { Debug.LogWarning("[WebVoice] 목록을 못 받았다 — 목소리 없이 진행"); m = new Manifest { voices = new Entry[0] }; }
            long total = 0;
            foreach (var e in m.voices) { byArt[e.art] = e; total += e.size; }
            Active = true;
            VoiceSource.Find = Find;
            VoiceSource.Has = a => a != null && byArt.ContainsKey(a);
            VoiceSource.Want = Want;
            Debug.Log($"[WebVoice] 목소리 번들 {byArt.Count}명 · {total / 1e6:F1}MB · 판 {m.version}");
            // 점검(-demo) — 귀 대신 재생 기록: 실제로 튼 목소리(voice:) · 못 튼 것(!voice:)과 그때 번들을 받았는지
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo") >= 0)
            {
                Sfx.OnPlayed += x => { if (x.Contains("voice:")) Debug.Log($"[VoicePlay] 전투 {x} (받은 {Fetched})"); };
                HeroVoice.OnPlayed += x => Debug.Log($"[VoicePlay] 판 {x} (받은 {Fetched})");
            }
            // 로비 메인 사도 목소리부터
            var lobby = Roster.All.Find(h => h.key == Settings.LobbyHero);
            if (lobby != null) Want(lobby.art, false);
            // 마지막 편성 사도 셋도 시작 때
            foreach (var k in PartyStore.Recent) { var h = k != null ? Roster.ByKey(k) : null; if (h != null) Want(h.art, false); }
        }

        static AudioClip[] Find(string art)
        {
            if (art == null || !clips.TryGetValue(art, out var c)) return null;
            Touch(art);
            return c;
        }

        static readonly Dictionary<string, float> usedAt = new Dictionary<string, float>(), lowAt = new Dictionary<string, float>();
        static void Touch(string art)
        {
            var n = order.Find(art);
            if (n != null) order.Remove(n);
            order.AddLast(art);
            usedAt[art] = Time.realtimeSinceStartup;
            Trim();
        }

        static void Trim()
        {
            while (order.Count > Keep)
            {
                var old = order.First.Value;
                // 방금(10초 안) 쓴 것은 내리지 않는다 — 틀고 있는 소리의 번들을 내리면 소리가 끊긴다. 잠깐 상한을 넘겨도 다음에 내린다
                if (usedAt.TryGetValue(old, out var u) && Time.realtimeSinceStartup - u < 10f) break;
                order.RemoveFirst();
                usedAt.Remove(old);
                if (loaded.TryGetValue(old, out var ob) && ob != null) ob.Unload(false);   // 이미 꺼낸 클립은 쓰는 곳이 놓을 때까지 산다
                loaded.Remove(old);
                clips.Remove(old);
            }
        }

        static void Want(string art, bool low)
        {
            if (!Active || art == null || !byArt.ContainsKey(art) || clips.ContainsKey(art) || busy.Contains(art)) return;
            if (failedAt.TryGetValue(art, out var t) && Time.realtimeSinceStartup - t < 30f) return;   // 방금 실패 — 잠시 쉬었다가
            if (low)
            {
                // 목록은 누를 때마다 다시 그려 같은 사도를 또 건다 — 2분 안에 미리 받은 것은 다시 받지 않는다(상한에 밀려 내렸어도 브라우저 캐시에 있다)
                if (lowAt.TryGetValue(art, out var la) && Time.realtimeSinceStartup - la < 120f) return;
                lowAt[art] = Time.realtimeSinceStartup;
                if (queued.Add(art)) { lowQueue.Enqueue(art); lowOnly.Add(art); }
            }
            else { queued.Add(art); lowOnly.Remove(art); queue.Enqueue(art); Debug.Log($"[WebVoice] 받기 {art} (급함 · 대기 {queue.Count})"); }   // 목록 대기열에 있던 것이면 앞으로 당긴다(뒤의 것은 Lane 이 건너뛴다)
            while (running < Lanes && (queue.Count > 0 || lowQueue.Count > 0)) host.StartCoroutine(Lane());
            // 급한 것인데 두 줄이 다 목록 미리 받기에 묶여 있으면 한 줄 더(느린 회선에서 칸 넣기 목소리가 1.5초를 넘겨 버려졌다 — 지연 800ms 시험)
            if (!low && running < Lanes + 1 && queue.Count > 0) host.StartCoroutine(Lane());
        }

        static string Next()
        {
            while (queue.Count > 0 || lowQueue.Count > 0)
            {
                var a = queue.Count > 0 ? queue.Dequeue() : lowQueue.Dequeue();
                if (!clips.ContainsKey(a) && !busy.Contains(a)) return a;
            }
            return null;
        }

        static IEnumerator Lane()
        {
            running++;
            string art;
            while ((art = Next()) != null)
            {
                busy.Add(art);
                var e = byArt[art];
                float t0 = Time.realtimeSinceStartup;
                AssetBundle b = null;
                for (int attempt = 1; attempt <= Tries && b == null; attempt++)
                {
                    using (var r = UnityWebRequestAssetBundle.GetAssetBundle(baseUrl + e.file + "?h=" + e.hash, 0u))
                    {
                        yield return r.SendWebRequest();
                        if (r.result == UnityWebRequest.Result.Success) b = DownloadHandlerAssetBundle.GetContent(r);
                        if (b == null) Debug.LogWarning($"[WebVoice] {e.file} 실패({attempt}) {r.error}");
                    }
                    if (b == null && attempt < Tries) yield return new WaitForSecondsRealtime(1.5f * attempt);
                }
                busy.Remove(art);
                queued.Remove(art);
                if (b == null) { failedAt[art] = Time.realtimeSinceStartup; continue; }   // 조용히 — 소리 없이 진행
                loaded[art] = b;
                var got = clips[art] = b.LoadAllAssets<AudioClip>();
                bool wasLow = lowOnly.Remove(art);
                Fetched++; FetchedBytes += e.size;
                if (wasLow) { order.AddFirst(art); usedAt[art] = 0; Trim(); }   // 미리 받기는 가장 오래된 자리에 — 쓰던 사도(편성 · 로비)를 밀어내지 않는다
                else Touch(art);
                Debug.Log($"[WebVoice] 받음 {art} {got.Length}개 {e.size / 1e3:F0}KB {Time.realtimeSinceStartup - t0:F2}s (붙든 {clips.Count})");
                VoiceSource.NotifyReady(art);
            }
            running--;
        }
    }
}
