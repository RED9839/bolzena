using System;
using System.Collections;
using System.IO;
using System.Linq;
using Bolzena.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 플레이 기록 보내기 — 판이 끝날 때(승리 · 패배 · 포기 · 메인으로) 한 번, 익명 기록 한 장(core RunRecord.Of)을 같은 사이트의 /api/record 로 보낸다.
    /// 형식 · 개인 정보 원칙 · 받는 쪽: bolzena-unity/Docs/플레이 기록.md.
    ///
    /// - 설정 「플레이 기록 보내기(익명)」(기본 켬, PlayerPrefs bz.rec.send) — 끄면 아무것도 보내지 않는다. 처음 실행 때 로비에서 한 줄 안내(bz.rec.noticed).
    /// - 익명 id(bz.rec.anon) — 이 기기에서 처음 만든 무작위 32자. 진행 코드 · 세이브와 무관하다(진행 코드에 안 들어간다).
    /// - 보내는 곳: 웹(WebGL) 배포판만(주소가 localhost · 127.0.0.1 · 파일이 아닐 때). 에디터 · PC 빌드 · 데모 · 시험 실행(-demo 따위 인자)은 보내지 않고
    ///   대신 파일로 남긴다(-recdir 폴더, 없으면 persistentDataPath/play-records — 최근 20장).
    /// - 실패(네트워크 · 429 · 5xx)는 persistentDataPath/rec-queue 에 5장까지 두었다가 다음 실행 때 다시 보낸다. 400 · 413 은 버린다(다시 보내도 같다).
    /// - 판 세이브(bolzena-run.json) · 진행 코드와 파일을 섞지 않는다.
    /// </summary>
    public sealed class PlayRecord : MonoBehaviour
    {
        const string KOn = "bz.rec.send", KNoticed = "bz.rec.noticed", KAnon = "bz.rec.anon";
        const int QUEUE_MAX = 5, LOCAL_MAX = 20;
        public const string NOTICE = "밸런스를 다듬으려고 판 기록을 익명으로 보냅니다. 개인 정보는 없고, 설정에서 끌 수 있습니다.";

        /// <summary>설정 — 보내기 켬(기본).</summary>
        public static bool On { get => PlayerPrefs.GetInt(KOn, 1) == 1; set { PlayerPrefs.SetInt(KOn, value ? 1 : 0); PlayerPrefs.Save(); } }

        /// <summary>전투 화면이 넣는 지금 배속(전투가 아니면 0) — 전투 시간 · 그 가운데 2배속 시간을 판 상태(RecState)에 더한다.</summary>
        public static Func<float> FightSpeed;

        static PlayRecord me;
        static string lastSent;   // 같은 판 · 같은 결과를 두 번 보내지 않게(「씨앗|결과」)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (me != null) return;
            var go = new GameObject("PlayRecord");
            DontDestroyOnLoad(go);
            me = go.AddComponent<PlayRecord>();
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(8f);   // 첫 로딩이 끝난 뒤에
            if (Allowed && On) yield return Flush();
        }

        void Update()
        {
            var s = Flow.Me != null ? Flow.Me.P?.S : null;
            float sp = FightSpeed != null ? FightSpeed() : 0;
            if (s?.Rec == null || sp <= 0) return;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0 || dt > 1f) return;   // 탭을 떠났다 온 큰 틈은 빼고
            s.Rec.FightSec += dt;
            if (sp > 1.01f) s.Rec.FastSec += dt;
        }

        /// <summary>판을 열었다 — 시작 시각(분)을 판 상태에 적는다(이어해도 남는다).</summary>
        public static void Began(RunPort p)
        {
            if (p?.S?.Rec != null) p.S.Rec.Began = RunRecord.Minute(DateTime.UtcNow);
        }

        // ── 언제 보내나 ──
        static bool TestRun
        {
            get
            {
                if (Demo.Active || TestMute.On) return true;
                var a = Environment.GetCommandLineArgs();
                return Array.IndexOf(a, "-norec") >= 0 || Array.IndexOf(a, "-recdir") >= 0;
            }
        }

        /// <summary>보내도 되는 곳인가 — 웹 배포판(localhost · 파일 주소 아님) · 시험 실행 아님.</summary>
        public static bool Allowed
        {
            get
            {
                if (Application.isEditor || Application.platform != RuntimePlatform.WebGLPlayer || TestRun) return false;
                if (!Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var u) || (u.Scheme != "https" && u.Scheme != "http")) return false;
                var h = u.Host.ToLowerInvariant();
                return h.Length > 0 && h != "localhost" && h != "127.0.0.1" && h != "[::1]" && !h.EndsWith(".local", StringComparison.Ordinal);
            }
        }

        static string Anon
        {
            get
            {
                var a = PlayerPrefs.GetString(KAnon, "");
                if (a.Length == 32) return a;
                a = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(KAnon, a); PlayerPrefs.Save();
                return a;
            }
        }

        static string BuildStamp
        {
            get { var t = Resources.Load<TextAsset>("RunUI/build_stamp"); return t != null ? t.text.Trim() : null; }
        }

        static int BoardCells()
        {
            try { var s = CrayonStore.Save; return CrayonStore.Table.Cells.Where(x => !x.Blank).Sum(x => Math.Min(x.Levels, s.LevelOf(x.Id))); }
            catch { return 0; }
        }

        /// <summary>
        /// 이 판의 기록을 보낸다(또는 파일로) — result: win · lose · abandon · quit. 판이 없으면 아무것도 않는다.
        /// 설정이 꺼져 있으면 보내지도 남기지도 않는다(시험 실행의 파일 남기기만은 한다).
        /// </summary>
        public static void Send(Run run, string result)
        {
            if (run?.S == null) return;
            string once = run.S.Seed + "|" + result + "|" + run.S.Step + "|" + run.S.Hist.Count;
            if (once == lastSent) return;
            lastSent = once;
            string json;
            try
            {
                var rec = RunRecord.Of(run, new RecordMeta
                {
                    Anon = Anon, Ver = Application.version, Build = BuildStamp,
                    Os = Application.isEditor ? "editor" : Application.platform == RuntimePlatform.WebGLPlayer ? "web" : "pc",
                    Phone = Application.isMobilePlatform, Low = DisplayOptions.LowSpec, Result = result, Board = BoardCells(), Ended = DateTime.UtcNow,
                });
                json = RunRecord.Json(rec);
                var why = RunRecord.Check(json);
                if (why != null) { Debug.LogWarning("[PlayRecord] 기록 검사 실패 — 보내지 않습니다: " + why); return; }
                Debug.Log($"[PlayRecord] 판 기록 {result} · {json.Length / 1024f:0.0}KB · 싸움 {rec.Fights.Count} · 선택 {rec.Picks?.Count ?? 0}{(rec.Trim > 0 ? $" · 줄임 {rec.Trim}" : "")}");
            }
            catch (Exception e) { Debug.LogWarning("[PlayRecord] 기록을 만들지 못했습니다: " + e.Message); return; }
            if (!Allowed) { if (TestRun || Application.isEditor || On) SaveLocal(json, run.S.Seed, result); return; }
            if (!On) return;
            if (me != null) me.StartCoroutine(Post(json, true)); else Enqueue(json);
        }

        public static void Send(RunPort p, string result) => Send(p?.Run, result);

        // ── 파일 · 줄 ──
        static string LocalDir
        {
            get
            {
                var a = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(a, "-recdir");
                return i >= 0 && i < a.Length - 1 ? a[i + 1] : Path.Combine(Application.persistentDataPath, "play-records");
            }
        }
        static string QueueDir => Path.Combine(Application.persistentDataPath, "rec-queue");

        static void SaveLocal(string json, long seed, string result)
        {
            try
            {
                var dir = LocalDir;
                Directory.CreateDirectory(dir);
                var f = Path.Combine(dir, $"볼제나-기록-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{seed}-{result}.json");
                File.WriteAllText(f, json);
                foreach (var old in new DirectoryInfo(dir).GetFiles("볼제나-기록-*.json").OrderByDescending(x => x.Name, StringComparer.Ordinal).Skip(LOCAL_MAX)) old.Delete();
                Debug.Log("[PlayRecord] 보내지 않는 실행 — 파일로 남김 " + f);
            }
            catch (Exception e) { Debug.LogWarning("[PlayRecord] 파일로 남기지 못했습니다: " + e.Message); }
        }

        static void Enqueue(string json)
        {
            try
            {
                Directory.CreateDirectory(QueueDir);
                File.WriteAllText(Path.Combine(QueueDir, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.json"), json);
                foreach (var old in new DirectoryInfo(QueueDir).GetFiles("*.json").OrderByDescending(x => x.Name, StringComparer.Ordinal).Skip(QUEUE_MAX)) old.Delete();
            }
            catch (Exception e) { Debug.LogWarning("[PlayRecord] 다시 보낼 줄에 못 넣었습니다: " + e.Message); }
        }

        static IEnumerator Flush()
        {
            FileInfo[] fs;
            try { fs = Directory.Exists(QueueDir) ? new DirectoryInfo(QueueDir).GetFiles("*.json").OrderBy(x => x.Name, StringComparer.Ordinal).ToArray() : new FileInfo[0]; }
            catch { yield break; }
            foreach (var f in fs)
            {
                string json;
                try { json = File.ReadAllText(f.FullName); f.Delete(); } catch { continue; }
                yield return Post(json, true);
            }
        }

        static string Endpoint => new Uri(new Uri(Application.absoluteURL), "/api/record").ToString();

        /// <summary>보낸다 — 실패하면(네트워크 · 429 · 5xx) 줄에 넣는다(again). 400 · 403 · 410 · 413 은 버린다.</summary>
        static IEnumerator Post(string json, bool again)
        {
            UnityWebRequest r;
            try
            {
                r = new UnityWebRequest(Endpoint, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)) { contentType = "application/json" },
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = 20,
                };
                r.SetRequestHeader("Content-Type", "application/json");
            }
            catch (Exception e) { Debug.LogWarning("[PlayRecord] 보내기 준비 실패: " + e.Message); if (again) Enqueue(json); yield break; }
            using (r)
            {
                yield return r.SendWebRequest();
                long code = r.responseCode;
                if (r.result == UnityWebRequest.Result.Success && code >= 200 && code < 300) { Debug.Log("[PlayRecord] 보냄"); yield break; }
                bool retry = code == 0 || code == 429 || code >= 500;
                Debug.LogWarning($"[PlayRecord] 보내기 실패 {code} {r.error}{(retry ? " — 다음 실행 때 다시" : " — 버림")}");
                if (retry && again) Enqueue(json);
            }
        }

        // ── 안내 ──
        /// <summary>처음 실행 때 로비에서 한 번 — 무엇을 왜 · 개인 정보 없음 · 설정에서 끄기.</summary>
        public static void NoticeOnce()
        {
            if (PlayerPrefs.GetInt(KNoticed, 0) == 1 || Demo.Active) return;
            PlayerPrefs.SetInt(KNoticed, 1); PlayerPrefs.Save();
            Toast.Show(NOTICE, 7f);
        }
    }
}
