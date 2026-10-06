using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bolzena.RunUI;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace Bolzena
{
    // 웹 번들 — 웹(WebGL) 빌드는 스파인(Assets/Bolzena/Resources/Spine 353 폴더)을 첫 로딩(.data)에서 빼고 폴더마다 번들(WebBuild/Bundles)로 받는다
    //   (BOLZENA_WEB_BUNDLES=1 로 빌드했을 때만 — 기본 빌드는 매니페스트가 없어 아무것도 안 한다). 전투 · 장면 사도 스파인은 첫 화면이 뜬 뒤에 받는다(ProjectSetup.SetupAndBuildWeb · WebBundleBuild).
    //   · boot  (스탠딩 st_* 135) — 판 화면이 처음부터 쓴다(로비 사도 · 목록 얼굴 · 카드 그림을 스탠딩에서 굽는다). 첫 화면 전에 받는다.
    //   · later (사도 SD · 적 218) — 전투 · 이벤트 · 캠프 장면에서만 쓴다. 첫 화면이 뜨자마자 뒤에서 받고, 전투에 들어갈 때 덜 받았으면 기다린다.
    //   받는 동안 · 실패는 index.html 의 로딩 화면(BzWeb.jslib → window.bzStatus · bzReady)으로 보여 주고, 실패하면 저절로 다시 받는다.
    //   웹 플레이어에는 Caching(IndexedDB)이 없다 — 주소에 해시(?h=)를 붙여 브라우저 HTTP 캐시가 같은 번들을 다시 쓰게 한다(바뀐 것만 새로).
    //   PC · 에디터 · 매니페스트 없는 웹 빌드는 아무것도 하지 않는다(Resources 그대로).
    public static class WebBundles
    {
        [Serializable] class Entry { public string folder; public string file; public string hash; public long size; public string group; }
        [Serializable] class Manifest { public string version; public Entry[] bundles; }

        static readonly Dictionary<string, Entry> byFolder = new Dictionary<string, Entry>();
        static readonly Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();
        static readonly List<Entry> later = new List<Entry>();
        static string baseUrl;
        static long laterBytes, laterDone;
        static bool laterRunning, waiting;
        static int lastFail;
        static Host host;

        /// <summary>번들 층이 켜졌다(웹 빌드 · 매니페스트를 읽었다).</summary>
        public static bool Active { get; private set; }

        class Host : MonoBehaviour { }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BzWebStatus(float p, string msg);
        [DllImport("__Internal")] static extern void BzWebReady();
        [DllImport("__Internal")] static extern int BzWebHasBundles();
#else
        static int BzWebHasBundles() => 0;
        static void BzWebStatus(float p, string msg) { }
        static void BzWebReady() { }
#endif

        static void Status(float p, string msg) { try { BzWebStatus(p, msg); } catch (Exception) { } }
        static void Ready() { try { BzWebReady(); } catch (Exception) { } }

        static SkeletonDataAsset Find(string folder)
        {
            if (folder == null || !loaded.TryGetValue(folder, out var b) || b == null) return null;
            var all = b.LoadAllAssets<SkeletonDataAsset>();
            return all.Length > 0 ? all[0] : null;
        }

        /// <summary>모두 받았는가(번들 층이 꺼져 있으면 늘 true).</summary>
        public static bool AllLoaded => !Active || loaded.Count >= byFolder.Count;

        static string BaseUrl()
        {
            var u = Application.absoluteURL ?? "";
            int q = u.IndexOfAny(new[] { '?', '#' });
            if (q >= 0) u = u.Substring(0, q);
            u = u.Substring(0, u.LastIndexOf('/') + 1);
            return u + "Bundles/";
        }

        static string Mb(long b) => (b / 1e6).ToString("F0");

        /// <summary>첫 장면(RunBoot)에서 — 매니페스트를 읽고 boot 묶음(스탠딩)을 다 받을 때까지 기다린 뒤, later 묶음을 뒤에서 받기 시작한다.</summary>
        public static IEnumerator Boot()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer || Active) yield break;
            if (host == null) { var go = new GameObject("WebBundles"); UnityEngine.Object.DontDestroyOnLoad(go); host = go.AddComponent<Host>(); }
            baseUrl = BaseUrl();
            if (BzWebHasBundles() == 0) yield break;   // 번들 없이 빌드한 판(기본) — index.html 이 알려 준다
            Manifest m = null;
            for (int attempt = 1; m == null; attempt++)
            {
                using (var r = UnityWebRequest.Get(baseUrl + "manifest.json?t=" + DateTime.UtcNow.Ticks))
                {
                    yield return r.SendWebRequest();
                    if (r.result == UnityWebRequest.Result.Success)
                    {
                        // 번들 없이 빌드한 판 — Pages 는 없는 파일에 index.html 을 200 으로 줄 수 있다. JSON 이 아니면 번들 없음으로
                        try { m = JsonUtility.FromJson<Manifest>(r.downloadHandler.text); } catch (Exception) { m = null; }
                        if (m?.bundles == null) { Debug.Log("[Bundles] 번들 없는 판 — Resources 만 쓴다"); Ready(); yield break; }
                        break;
                    }
                    if (r.responseCode == 404) { Debug.Log("[Bundles] 매니페스트 없음 — Resources 만 쓴다"); Ready(); yield break; }
                    Debug.LogWarning($"[Bundles] 매니페스트 실패({attempt}) {r.error}");
                    Status(-1, $"그림 목록을 받지 못했습니다 — 다시 시도하는 중({attempt})…");
                }
                yield return new WaitForSecondsRealtime(Mathf.Min(10f, 1.5f * attempt));
            }
            if (m?.bundles == null || m.bundles.Length == 0) { Ready(); yield break; }
            var boot = new List<Entry>();
            foreach (var e in m.bundles)
            {
                byFolder[e.folder] = e;
                if (e.group == "boot") boot.Add(e); else later.Add(e);
            }
            Active = true;
            SpineSource.Find = Find;
            SpineSource.Has = f => f != null && byFolder.ContainsKey(f);
            SpineSource.Pending = f => f != null && byFolder.ContainsKey(f) && !loaded.ContainsKey(f);
            long total = 0; foreach (var e in boot) total += e.size;
            float t0 = Time.realtimeSinceStartup;
            Debug.Log($"[Bundles] 번들 {m.bundles.Length}개(boot {boot.Count} · later {later.Count}) · 판 {m.version}");
            yield return Fetch(boot, 6, (done, cur) => { if (lastFail == 0) Status(total > 0 ? (float)(done + cur) / total : 1f, $"사도 그림을 받는 중… {Mb(done + cur)} / {Mb(total)}MB"); }, true);
            Debug.Log($"[Bundles] boot {boot.Count}개 {Mb(total)}MB — {Time.realtimeSinceStartup - t0:F1}s");
            Ready();
            foreach (var e in later) laterBytes += e.size;
            host.StartCoroutine(LaterCo());
        }

        static IEnumerator LaterCo()
        {
            laterRunning = true;
            float t0 = Time.realtimeSinceStartup;
            yield return Fetch(later, 3, (done, cur) => laterDone = done + cur, false);
            laterRunning = false;
            Debug.Log($"[Bundles] later {later.Count}개 {Mb(laterBytes)}MB — {Time.realtimeSinceStartup - t0:F1}s");
        }

        /// <summary>남은 번들(전투 · 장면 스파인)을 다 받을 때까지 기다린다 — 전투에 들어가기 전에(BattleBridge · -battle). 받는 동안 로딩 화면을 띄운다.</summary>
        public static IEnumerator WaitAll()
        {
            if (!Active || AllLoaded) yield break;
            float t0 = Time.realtimeSinceStartup;
            waiting = true;
            while (!AllLoaded)
            {
                if (lastFail == 0) Status(laterBytes > 0 ? (float)laterDone / laterBytes : 0f, $"전투 그림을 받는 중… {Mb(laterDone)} / {Mb(laterBytes)}MB");
                yield return null;
            }
            waiting = false;
            Ready();
            Debug.Log($"[Bundles] 전투 전 기다림 {Time.realtimeSinceStartup - t0:F1}s");
        }

        // n 줄로 나눠 받는다. 하나가 실패하면 그 줄이 기다렸다가 다시(끝없이 — 받는 화면에 횟수를 보인다)
        static IEnumerator Fetch(List<Entry> list, int lanes, Action<long, long> progress, bool loud)
        {
            int next = 0, running = 0;
            long done = 0;
            var cur = new Dictionary<Entry, UnityWebRequest>();
            IEnumerator Lane()
            {
                running++;
                while (next < list.Count)
                {
                    var e = list[next++];
                    if (loaded.ContainsKey(e.folder)) { done += e.size; continue; }
                    for (int attempt = 1; ; attempt++)
                    {
                        var url = baseUrl + e.file + "?h=" + e.hash;
                        using (var r = UnityWebRequestAssetBundle.GetAssetBundle(url, 0u))
                        {
                            cur[e] = r;
                            yield return r.SendWebRequest();
                            cur.Remove(e);
                            AssetBundle b = null;
                            if (r.result == UnityWebRequest.Result.Success) b = DownloadHandlerAssetBundle.GetContent(r);
                            if (b != null) { loaded[e.folder] = b; done += e.size; lastFail = 0; break; }
                            Debug.LogWarning($"[Bundles] {e.file} 실패({attempt}) {r.error}");
                            lastFail = attempt;
                            if (loud || waiting) Status(-1, $"그림을 받지 못했습니다 — 다시 시도하는 중({attempt})… 연결을 확인해 주세요");
                        }
                        yield return new WaitForSecondsRealtime(Mathf.Min(10f, 1.5f * attempt));
                    }
                }
                running--;
            }
            for (int i = 0; i < Mathf.Min(lanes, list.Count); i++) host.StartCoroutine(Lane());
            while (running > 0 || next < list.Count)
            {
                long c = 0;
                foreach (var kv in cur) c += (long)(kv.Key.size * Mathf.Clamp01(kv.Value.downloadProgress));
                progress(done, c);
                yield return null;
            }
            progress(done, 0);
        }
    }
}
