using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bolzena.Demo
{
    // 전투 들고 나기 시험의 저울(-leakprobe · 판 연속 싸움 -demo-loop N 이면 저절로) — 장면을 넘어 산다.
    //   ① 전투 진입 — 다리(BattleBridge.Open)가 부른 순간부터 전투 첫 화면(감독이 세워지고 그려진 프레임 끝)까지 ms,
    //      진입부터 첫 입력 대기(손패가 깔림)까지 가장 긴 프레임 ms · 그 프레임이 어느 단계였는지
    //   ② 나온 직후 — 판 화면(지도)이 다시 서면 Resources.FindObjectsOfTypeAll 로 종류별 개수를 센다
    //      → <Captures>/leak.tsv 한 줄씩 + 로그 [Leak]. 늘어난 이름은 앞 번과의 차이로 [Leak+] 에 적는다.
    //   -battle -ultaudit 처럼 Battle 장면만 거듭 열 때는 장면이 열릴 때마다 센다(판 화면이 없으니).
    public class EnterLeak : MonoBehaviour
    {
        public static bool On => Has("-leakprobe") || Has("-demo-loop");
        static bool Has(string a) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), a) >= 0;
        static EnterLeak me;

        // 세는 종류(이름 → 고르기). TMP 재질은 셰이더 이름으로 따로
        static readonly string[] Kinds =
        {
            "Texture2D", "RenderTexture", "Material", "TmpMaterial", "Mesh", "Sprite", "AudioClip", "SkeletonDataAsset", "AtlasAsset",
            "TMP_FontAsset", "Shader", "GameObject", "Transform", "MonoBehaviour", "AnimationClip", "ScriptableObject", "Object",
        };

        readonly System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        double openAt = -1, lastTick = -1;
        double firstMs, worstMs; string worstAt = "-";
        int stage;                       // 0 쉼 · 1 진입 중(첫 화면 전) · 2 첫 화면 뒤 · 입력 대기 전
        int no;                          // 몇 번째 진입
        string tsv;
        Dictionary<string, int> prevNames;
        Dictionary<string, int> firstCounts, prevCounts;
        UltProbe probe;
        double unloadMs = -1;
        readonly StringBuilder rows = new StringBuilder();

        public static void Install()
        {
            if (me != null || !On) return;
            var go = new GameObject("EnterLeak");
            DontDestroyOnLoad(go);
            me = go.AddComponent<EnterLeak>();
        }

        /// <summary>다리가 싸움을 열기 직전(입력이 전투로 넘어가는 순간).</summary>
        public static void MarkOpen()
        {
            if (me == null) return;
            me.openAt = me.sw.Elapsed.TotalMilliseconds;
            me.stage = 1;
            me.firstMs = me.worstMs = 0; me.worstAt = "-";
            me.no++;
        }

        void Start()
        {
            var dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            Directory.CreateDirectory(dir);
            tsv = Path.Combine(dir, Has("-ultaudit") ? "leak_ultaudit.tsv" : "leak.tsv");
            File.WriteAllText(tsv, "no\twhen\tenterMs\tworstMs\tworstAt\tunloadMs\t" + string.Join("\t", Kinds) + "\tmonoMB\n");
            if (Has("-ultprobe")) probe = new UltProbe();
            SceneManager.sceneLoaded += OnLoaded;
            Debug.Log("[Leak] 저울 켬 → " + tsv);
        }

        static string Arg(string n)
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, n);
            return i >= 0 && i < a.Length - 1 ? a[i + 1] : null;
        }

        void OnLoaded(Scene s, LoadSceneMode m)
        {
            if (s.name == BattleBridge.BattleScene)
            {
                if (stage == 0) MarkOpen();            // 전투만 거듭 여는 점검(-ultaudit) — 장면이 열린 때부터
                if (Has("-ultaudit") && no > 1) StartCoroutine(CountLater("battle", 2));
            }
            else if (s.name == BattleBridge.RunScene && no > 0) StartCoroutine(AfterReturn());
        }

        void Update()
        {
            double now = sw.Elapsed.TotalMilliseconds;
            double dt = lastTick < 0 ? 0 : now - lastTick;
            lastTick = now;
            probe?.Frame();
            if (stage == 0) return;
            if (dt > worstMs)
            {
                worstMs = dt;
                worstAt = stage == 2 ? "첫 화면 뒤" : "첫 화면 전";
                if (dt > 50) probe?.Dump(8);
            }
            var d = BattleDirector.I;
            if (stage == 1 && d != null && d.Hand != null && d.isActiveAndEnabled) StartCoroutine(FirstFrame());
            if (stage == 2 && d != null && (d.WaitingInput || d.Over)) stage = 0;
            if (stage != 0 && now - openAt > 20000) stage = 0;   // 막혔다
        }

        IEnumerator FirstFrame()
        {
            stage = 3;   // 기다리는 동안 다시 부르지 않게(가장 긴 프레임은 계속 잰다)
            yield return new WaitForEndOfFrame();
            firstMs = sw.Elapsed.TotalMilliseconds - openAt;
            stage = 2;
        }

        // 판으로 돌아와 지도가 서면 센다
        IEnumerator AfterReturn()
        {
            float t = 0;
            while (t < 20 && (RunUI.Flow.Me == null || RunUI.Flow.Me.Stage.Busy || RunUI.Flow.Me.Stage.Current != "map")) { t += Time.unscaledDeltaTime; yield return null; }
            yield return CountLater("map", 30);
        }

        IEnumerator CountLater(string when, int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
            Count(when);
        }

        void Count(string when)
        {
            var all = Resources.FindObjectsOfTypeAll<Object>();
            var c = Kinds.ToDictionary(k => k, k => 0);
            var names = new Dictionary<string, int>();
            foreach (var o in all)
            {
                if (o == null) continue;
                string k = Kind(o);
                c["Object"]++;
                if (k != null) c[k]++;
                if (o is MonoBehaviour) c["MonoBehaviour"]++;
                else if (o is ScriptableObject && k == null) c["ScriptableObject"]++;
                if (k == null || k == "Transform" || k == "Shader") continue;
                string key = k + ":" + (string.IsNullOrEmpty(o.name) ? "(이름 없음)" : o.name);
                if (o is Texture t) key += $" {t.width}x{t.height}";
                if (o is Material mm) key += $" [{(mm.HasProperty("_MainTex") && mm.mainTexture != null ? mm.mainTexture.name : "-")}]{(mm.hideFlags != 0 ? " " + mm.hideFlags : "")}";
                names[key] = names.TryGetValue(key, out var v) ? v + 1 : 1;
            }
            double mono = System.GC.GetTotalMemory(false) / 1048576.0;
            var line = $"{no}\t{when}\t{firstMs:F0}\t{worstMs:F0}\t{worstAt}\t{unloadMs:F0}\t" + string.Join("\t", Kinds.Select(k => c[k].ToString())) + $"\t{mono:F0}";
            File.AppendAllText(tsv, line + "\n");
            Debug.Log("[Leak] " + line.Replace('\t', '|') + $" · Res 붙듦 {Res.Held}");
            if (prevNames != null)
            {
                var grow = names.Select(kv => (kv.Key, d: kv.Value - (prevNames.TryGetValue(kv.Key, out var p) ? p : 0))).Where(x => x.d > 0).OrderByDescending(x => x.d).Take(25).ToList();
                if (grow.Count > 0) Debug.Log($"[Leak+] {no} 앞 번보다 늘어난 이름 — " + string.Join(" · ", grow.Select(x => $"{x.Key} +{x.d}")));
            }
            prevNames = names;
            firstCounts ??= c;
            prevCounts = c;
        }

        static string Kind(Object o)
        {
            switch (o)
            {
                case RenderTexture _: return "RenderTexture";
                case Texture2D _: return "Texture2D";
                case Material m: return m.shader != null && m.shader.name.StartsWith("TextMeshPro") ? "TmpMaterial" : "Material";
                case Mesh _: return "Mesh";
                case Sprite _: return "Sprite";
                case AudioClip _: return "AudioClip";
                case Spine.Unity.SkeletonDataAsset _: return "SkeletonDataAsset";
                case Spine.Unity.AtlasAssetBase _: return "AtlasAsset";
                case TMPro.TMP_FontAsset _: return "TMP_FontAsset";
                case Shader _: return "Shader";
                case GameObject _: return "GameObject";
                case Transform _: return "Transform";
                case AnimationClip _: return "AnimationClip";
            }
            return null;
        }
    }
}
