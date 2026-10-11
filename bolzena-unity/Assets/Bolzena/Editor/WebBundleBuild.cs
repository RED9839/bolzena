using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 웹 번들 만들기 — 웹 빌드 동안만 스파인 폴더를 Resources 밖(Assets/BolzenaWebStage)으로 옮겨 첫 로딩(.data)에서 빼고,
    //   폴더마다 번들을 WebBuild/Bundles 에 만든 뒤(+ manifest.json) 제자리로 돌려놓는다. 런타임은 WebBundles(Scripts/Core).
    //   · Assets/Bolzena/Resources/Spine/* (353) → 번들 spine_<폴더>. st_* = boot(첫 화면 전에), 나머지 = later(뒤에서 · 전투 전에 기다림).
    //   · Assets/Resources/Spine/st_* 중 같은 그림이 위에 있는 것(판 화면 사본 29 — 그림 · 뼈 · 설정이 같다)은 번들 없이 빼기만 한다.
    //   옮기기는 AssetDatabase.MoveAsset(GUID 그대로 — 참조가 끊기지 않는다). 빌드가 중간에 죽어 남았으면 다음 Setup(PC · 웹)이 먼저 Restore 한다.
    //   BOLZENA_WEB_BUNDLES=1 일 때만 한다(기본은 예전처럼 전부 .data — 아래 Enabled).
    public static class WebBundleBuild
    {
        public const string Stage = "Assets/BolzenaWebStage";
        const string SpineRes = "Assets/Bolzena/Resources/Spine";
        const string RunSpineRes = "Assets/Resources/Spine";
        const string StageSpine = Stage + "/Spine";
        const string StageRun = Stage + "/RunSpine";
        // 웹에서 빼기만 하는 것(번들 없이 · 늘 — 2026-10-09 perf_20261009): Off/<Assets/ 뒤 경로> 로 옮긴다(WebImport.Original 이 되돌려 읽는다).
        //   경로 속 「Resources」 폴더 이름은 「_Res」 로 바꿔 둔다 — 이름이 Resources 인 폴더는 어디 있든 유니티가 Resources 로 친다(첫 시도에서 그대로 들어갔다)
        public const string StageOff = Stage + "/Off";
        const string ExcludeList = "Tools/web_exclude.txt";
        const string Moves = "Library/BolzenaWebStage_moves.txt";   // 옮긴 것(돌려놓기용) — 원래 자리 \t 옮긴 자리. 애셋이 아니게 Library 에

        // 기본은 끔(2026-10-06): 유니티 6 WebGL 번들은 LZMA 를 주어도 LZ4HC 로 나오고, Pages 가 번들 파일에 gzip 을 씌우지 않아
        //   .data(바깥 gzip) 보다 오히려 35MB 크다. 첫 화면까지는 −74MB 지만 첫 전투 전까지 받는 양은 늘어난다 — web_size/result.md
        public static bool Enabled => System.Environment.GetEnvironmentVariable("BOLZENA_WEB_BUNDLES") == "1";

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Move(string from, string to, List<string> log)
        {
            var err = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(err)) throw new System.Exception($"[Bundles] 옮기기 실패 {from} → {to}: {err}");
            log.Add(from + "\t" + to);
        }

        /// <summary>스파인 폴더를 Resources 밖으로(웹 빌드 직전).</summary>
        public static void MoveOut()
        {
            Restore();
            if (!AssetDatabase.IsValidFolder(SpineRes)) throw new System.Exception("[Bundles] " + SpineRes + " 가 없다");
            Folder(Stage);
            Folder(StageRun);
            var log = new List<string>();
            try
            {
                Move(SpineRes, StageSpine, log);
                if (AssetDatabase.IsValidFolder(RunSpineRes))
                    foreach (var d in AssetDatabase.GetSubFolders(RunSpineRes))
                    {
                        var name = Path.GetFileName(d);
                        if (name.StartsWith("st_") && AssetDatabase.IsValidFolder(StageSpine + "/" + name)) Move(d, StageRun + "/" + name, log);
                    }
            }
            finally { File.WriteAllLines(Moves, log); }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Bundles] Resources 밖으로 — {log.Count}건");
        }

        /// <summary>「Resources」 폴더 이름을 「_Res」 로(옮긴 자리) · 거꾸로(원래 자리).</summary>
        public static string OffName(string rel) => string.Join("/", rel.Split('/').Select(x => x == "Resources" ? "_Res" : x));
        public static string OnName(string rel) => string.Join("/", rel.Split('/').Select(x => x == "_Res" ? "Resources" : x));

        // ── 목소리 번들(2026-10-10 C안 — 런타임 WebVoice) ──
        //   Resources/Voice 를 웹 빌드 동안 Stage/Voice 로 옮겨 .data 에서 빼고, 사도 폴더마다 압축 없는 번들(AAC 라 LZ4 · gzip 이득이 4% 남짓)과 voice.json 을 만든다.
        //   BOLZENA_WEB_VOICE=0 이면 하지 않는다(예전처럼 .data 에).
        public const string StageVoice = Stage + "/Voice";
        const string VoiceRes = "Assets/Bolzena/Resources/Voice";
        public static bool VoiceEnabled => System.Environment.GetEnvironmentVariable("BOLZENA_WEB_VOICE") != "0";

        public static void VoiceOut()
        {
            if (!VoiceEnabled || !AssetDatabase.IsValidFolder(VoiceRes) || AssetDatabase.IsValidFolder(StageVoice)) return;
            var log = File.Exists(Moves) ? File.ReadAllLines(Moves).ToList() : new List<string>();
            Folder(Stage);
            try { Move(VoiceRes, StageVoice, log); }
            finally { File.WriteAllLines(Moves, log); }
            AssetDatabase.SaveAssets();
            Debug.Log("[WebVoice] 목소리를 .data 에서 뺌 → " + StageVoice);
        }

        [System.Serializable] class VEntry { public string art; public string file; public string hash; public long size; }
        [System.Serializable] class VManifest { public string version; public VEntry[] voices; }

        /// <summary>옮겨 둔 목소리로 사도별 번들을 만든다 — outDir(WebBuild/Voice)에 voice_<키>.bundle · voice.json.</summary>
        public static bool BuildVoices(string outDir)
        {
            if (!AssetDatabase.IsValidFolder(StageVoice)) { Debug.Log("[WebVoice] 옮긴 목소리 없음 — 번들 안 만듦"); return true; }
            var builds = new List<AssetBundleBuild>();
            var artOf = new Dictionary<string, string>();
            foreach (var d in AssetDatabase.GetSubFolders(StageVoice).OrderBy(x => x, System.StringComparer.Ordinal))
            {
                var art = Path.GetFileName(d);
                var files = Directory.GetFiles(d).Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/')).OrderBy(f => f, System.StringComparer.Ordinal).ToArray();
                if (files.Length == 0) continue;
                var bn = "voice_" + art.ToLowerInvariant();
                artOf[bn] = art;
                builds.Add(new AssetBundleBuild { assetBundleName = bn, assetNames = files });
            }
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);
            float t0 = Time.realtimeSinceStartup;
            var man = BuildPipeline.BuildAssetBundles(outDir, builds.ToArray(), BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.StrictMode, BuildTarget.WebGL);
            if (man == null) { Debug.LogError("[WebVoice] 목소리 번들 빌드 실패"); return false; }
            var list = new List<VEntry>();
            foreach (var bn in man.GetAllAssetBundles().OrderBy(x => x, System.StringComparer.Ordinal))
            {
                var file = bn + ".bundle";
                File.Move(Path.Combine(outDir, bn), Path.Combine(outDir, file));
                list.Add(new VEntry { art = artOf[bn], file = file, hash = man.GetAssetBundleHash(bn).ToString(), size = new FileInfo(Path.Combine(outDir, file)).Length });
            }
            foreach (var f in Directory.GetFiles(outDir, "*.manifest")) File.Delete(f);
            var index = Path.Combine(outDir, Path.GetFileName(outDir));
            if (File.Exists(index)) File.Delete(index);
            File.WriteAllText(Path.Combine(outDir, "voice.json"), JsonUtility.ToJson(new VManifest { version = System.DateTime.Now.ToString("yyyyMMddHHmm"), voices = list.ToArray() }, true));
            Debug.Log($"[WebVoice] 목소리 번들 {list.Count}개 · {list.Sum(x => x.size) / 1e6:F1}MB · 가장 큰 것 {list.Max(x => x.size) / 1e6:F2}MB · {Time.realtimeSinceStartup - t0:F0}s");
            return true;
        }

        static bool Exists(string p) => AssetDatabase.IsValidFolder(p) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p, AssetPathToGUIDOptions.OnlyExistingAssets));

        /// <summary>
        /// 웹 빌드에서 빼기(번들과 상관없이 늘) — 판에서 안 쓰는데 Resources 에 있어 .data 에 들어가던 것을 Resources 밖(StageOff)으로 옮긴다. 원본은 지우지 않는다.
        ///   ① Assets/Resources/Spine/st_* 중 같은 이름이 Assets/Bolzena/Resources/Spine 에 있는 것(판 화면 사본 — 같은 Resources 경로라 하나만 읽힌다)
        ///   ② Tools/web_exclude.txt 목록(안 쓰는 적 스파인 · 겹친 글꼴). 콘텐츠(CoreDataPack)에 그 이름이 나오면 빼지 않고 경고한다.
        /// </summary>
        public static void ExcludeOut()
        {
            var log = File.Exists(Moves) ? File.ReadAllLines(Moves).ToList() : new List<string>();
            int n0 = log.Count;
            var pack = "Assets/Resources/CoreDataPack.json";
            var core = File.Exists(pack) ? File.ReadAllText(pack).ToLowerInvariant() : "";
            void Off(string from)
            {
                var to = StageOff + "/" + OffName(from.Substring("Assets/".Length));
                Folder(Path.GetDirectoryName(to).Replace('\\', '/'));
                Move(from, to, log);
            }
            try
            {
                if (AssetDatabase.IsValidFolder(RunSpineRes) && AssetDatabase.IsValidFolder(SpineRes))
                    foreach (var d in AssetDatabase.GetSubFolders(RunSpineRes))
                    {
                        var name = Path.GetFileName(d);
                        if (name.StartsWith("st_") && AssetDatabase.IsValidFolder(SpineRes + "/" + name)) Off(d);
                    }
                if (File.Exists(ExcludeList))
                    foreach (var raw in File.ReadAllLines(ExcludeList))
                    {
                        var line = raw.Split('#')[0].Trim();
                        if (line.Length == 0 || !Exists(line)) continue;
                        var token = Path.GetFileNameWithoutExtension(line).ToLowerInvariant();
                        if (System.Text.RegularExpressions.Regex.IsMatch(core, "(?<![a-z0-9_])" + System.Text.RegularExpressions.Regex.Escape(token) + "(?![a-z0-9_])"))
                        {
                            Debug.LogWarning($"[WebOff] 콘텐츠에 「{token}」 이 나온다 — 빼지 않는다: {line}");
                            continue;
                        }
                        Off(line);
                    }
            }
            finally { File.WriteAllLines(Moves, log); }
            AssetDatabase.SaveAssets();
            Debug.Log($"[WebOff] 웹 빌드에서 뺌 — {log.Count - n0}건(→ {StageOff})");
        }

        /// <summary>옮긴 것을 제자리로(빌드 뒤 · 다음 Setup 첫머리). 남은 것이 없으면 아무것도 안 한다.</summary>
        public static void Restore()
        {
            if (!File.Exists(Moves) && !AssetDatabase.IsValidFolder(Stage)) return;
            int n = 0;
            if (File.Exists(Moves))
            {
                foreach (var line in File.ReadAllLines(Moves).Reverse())
                {
                    var p = line.Split('\t');
                    if (p.Length != 2 || !Exists(p[1])) continue;
                    var err = AssetDatabase.MoveAsset(p[1], p[0]);
                    if (!string.IsNullOrEmpty(err)) throw new System.Exception($"[Bundles] 돌려놓기 실패 {p[1]} → {p[0]}: {err}");
                    n++;
                }
            }
            // 목록 없이 남은 것(만일) — 이름으로 돌려놓는다
            if (AssetDatabase.IsValidFolder(StageSpine) && !AssetDatabase.IsValidFolder(SpineRes)) { AssetDatabase.MoveAsset(StageSpine, SpineRes); n++; }
            if (AssetDatabase.IsValidFolder(StageRun))
                foreach (var d in AssetDatabase.GetSubFolders(StageRun))
                {
                    var to = RunSpineRes + "/" + Path.GetFileName(d);
                    if (!AssetDatabase.IsValidFolder(to)) { AssetDatabase.MoveAsset(d, to); n++; }
                }
            if (AssetDatabase.IsValidFolder(StageVoice) && !AssetDatabase.IsValidFolder(VoiceRes)) { AssetDatabase.MoveAsset(StageVoice, VoiceRes); n++; }
            bool offEmpty = !Directory.Exists(StageOff) || !Directory.EnumerateFiles(StageOff, "*", SearchOption.AllDirectories).Any(f => !f.EndsWith(".meta"));
            bool empty = offEmpty && !AssetDatabase.IsValidFolder(StageVoice) && !AssetDatabase.IsValidFolder(StageSpine) && (!AssetDatabase.IsValidFolder(StageRun) || AssetDatabase.GetSubFolders(StageRun).Length == 0);
            if (empty)
            {
                if (File.Exists(Moves)) File.Delete(Moves);
                AssetDatabase.DeleteAsset(Stage);
            }
            else Debug.LogError("[Bundles] 돌려놓지 못한 것이 남았다 — " + Stage + " 를 손으로 확인");
            AssetDatabase.SaveAssets();
            Debug.Log($"[Bundles] 제자리로 — {n}건");
        }

        [System.Serializable] class Entry { public string folder; public string file; public string hash; public long size; public string group; }
        [System.Serializable] class Manifest { public string version; public Entry[] bundles; }

        /// <summary>옮겨 둔 스파인으로 번들을 만든다(LZ4HC — 아래) — outDir(WebBuild/Bundles)에 번들 · manifest.json.</summary>
        public static bool Build(string outDir)
        {
            if (!AssetDatabase.IsValidFolder(StageSpine)) { Debug.LogError("[Bundles] 옮긴 스파인이 없다"); return false; }
            var builds = new List<AssetBundleBuild>();
            var folderOf = new Dictionary<string, string>();
            foreach (var d in AssetDatabase.GetSubFolders(StageSpine).OrderBy(x => x, System.StringComparer.Ordinal))
            {
                var name = Path.GetFileName(d);
                var files = Directory.GetFiles(d).Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/')).OrderBy(f => f, System.StringComparer.Ordinal).ToArray();
                if (files.Length == 0) continue;
                var bn = "spine_" + name.ToLowerInvariant();
                folderOf[bn] = name;
                builds.Add(new AssetBundleBuild { assetBundleName = bn, assetNames = files });
            }
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);
            float t0 = Time.realtimeSinceStartup;
            // 압축 옵션 기본(LZMA)을 주지만 유니티 6000.6 WebGL 은 LZ4HC 로 만든다(번들 머리 확인). 셰이더는 번들마다 제 변형(곧은 알파 등 · 8KB)을 품는다
            var man = BuildPipeline.BuildAssetBundles(outDir, builds.ToArray(), BuildAssetBundleOptions.StrictMode, BuildTarget.WebGL);
            if (man == null) { Debug.LogError("[Bundles] 번들 빌드 실패"); return false; }
            var list = new List<Entry>();
            long boot = 0, later = 0;
            foreach (var bn in man.GetAllAssetBundles().OrderBy(x => x, System.StringComparer.Ordinal))
            {
                var path = Path.Combine(outDir, bn);
                var file = bn + ".bundle";
                File.Move(path, Path.Combine(outDir, file));
                var e = new Entry { folder = folderOf[bn], file = file, hash = man.GetAssetBundleHash(bn).ToString(), size = new FileInfo(Path.Combine(outDir, file)).Length };
                e.group = e.folder.StartsWith("st_") ? "boot" : "later";
                if (e.group == "boot") boot += e.size; else later += e.size;
                list.Add(e);
            }
            // 번들 빌드가 남기는 .manifest 글 · 목록 번들은 쓰지 않는다
            foreach (var f in Directory.GetFiles(outDir, "*.manifest")) File.Delete(f);
            var index = Path.Combine(outDir, Path.GetFileName(outDir));
            if (File.Exists(index)) File.Delete(index);
            var m = new Manifest { version = System.DateTime.Now.ToString("yyyyMMddHHmm"), bundles = list.ToArray() };
            File.WriteAllText(Path.Combine(outDir, "manifest.json"), JsonUtility.ToJson(m, true));
            Debug.Log($"[Bundles] 번들 {list.Count}개 — boot {boot / 1e6:F1}MB · later {later / 1e6:F1}MB · 가장 큰 것 {list.Max(x => x.size) / 1e6:F1}MB · {Time.realtimeSinceStartup - t0:F0}s");
            return true;
        }
    }
}
