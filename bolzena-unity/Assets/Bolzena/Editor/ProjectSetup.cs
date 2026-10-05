using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Bolzena.EditorTools
{
    // 명령줄에서 부르는 손 — 에디터 화면 없이 프로젝트를 꾸리고 빌드한다.
    //   Unity.exe -batchmode -quit -projectPath . -executeMethod Bolzena.EditorTools.ProjectSetup.ImportTmp
    //   Unity.exe -batchmode -quit -projectPath . -executeMethod Bolzena.EditorTools.ProjectSetup.SetupAndBuild
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Bolzena/Scenes/Battle.unity";
        const string RunScenePath = "Assets/Bolzena/Scenes/Run.unity";
        const string BuildPath = "Build/Bolzena.exe";

        public static void ImportTmp()
        {
            var pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(pkg))
            {
                foreach (var d in Directory.GetDirectories("Library/PackageCache", "com.unity.ugui*"))
                {
                    var c = Path.Combine(d, "Package Resources", "TMP Essential Resources.unitypackage");
                    if (File.Exists(c)) pkg = Path.GetFullPath(c);
                }
            }
            Debug.Log("[Setup] TMP 필수 리소스: " + pkg);
            // 패키지 가져오기는 비동기다 — -quit 없이 불러 끝나면 나간다
            AssetDatabase.importPackageCompleted += _ => { Debug.Log("[Setup] TMP 가져옴"); AssetDatabase.Refresh(); if (Application.isBatchMode) EditorApplication.Exit(0); };
            AssetDatabase.importPackageFailed += (_, err) => { Debug.LogError("[Setup] TMP 실패 " + err); if (Application.isBatchMode) EditorApplication.Exit(1); };
            AssetDatabase.ImportPackage(pkg, false);
        }

        public static void Setup()
        {
            CoreData();
            AssetDatabase.Refresh();
            Recompress();
            SpineMaterials();
            AlwaysIncludedShaders();
            Player();
            Scene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] 끝");
        }

        public static void SetupAndBuild()
        {
            Setup();
            Build();
        }

        // 코어 데이터 — 콘텐츠 폴더(C:\projects\bolzena-content — 사도 135 · 마을 · 이벤트 · 장비, 폴더째 재귀)를 StreamingAssets/CoreData 로
        // (실행 중 GameData.FromFolder 로 읽는다 — 판 화면 RunPort 와 전투 화면이 같은 것을). 코어 샘플(Data/Sample)은 섞지 않는다(id 가 겹친다).
        // _ 로 시작하는 파일(적 그림 표 _그림.json)도 함께 옮긴다 — 로더는 건너뛰고 화면(Look)이 읽는다.
        // 콘텐츠는 다른 트랙이 쓰는 중이라 깨진 순간을 읽을 수 있다 — 임시 폴더에 복사해 읽기 · 검사(Validator)를 통과한 것만 갈아 끼우고,
        // 못 통과하면 지난번 스냅샷을 그대로 둔다(스냅샷도 없으면 빌드를 멈춘다). 다른 폴더를 쓰려면 환경 변수 BOLZENA_CONTENT.
        // 코어가 글로 읽는 API(GameData.Add)를 내면 Resources 의 TextAsset 으로 바꾼다 — 그래야 안드로이드 · 웹에서도 읽힌다
        static void CoreData()
        {
            var src = System.Environment.GetEnvironmentVariable("BOLZENA_CONTENT");
            if (string.IsNullOrEmpty(src)) src = Path.GetFullPath("../bolzena-content-v2");   // v2 콘텐츠(2026-10-05~)
            var dst = Path.GetFullPath("Assets/StreamingAssets/CoreData");
            var tmp = Path.GetFullPath("Temp/CoreDataNew");
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            CopyJson(src, tmp);
            string why = null;
            try
            {
                var data = Bolzena.Core.GameData.FromFolder(tmp);
                var v = Bolzena.Core.Validator.Check(data);
                if (!v.Ok) why = "검사 오류 " + v.Errors.Count + "개 — " + string.Join(" / ", v.Errors.GetRange(0, System.Math.Min(3, v.Errors.Count)));
                else if (data.Heroes.Count < 3 || data.Villages.Count == 0) why = $"판을 열 수 없다 — 사도 {data.Heroes.Count} · 마을 {data.Villages.Count}";
                else Debug.Log($"[Setup] 콘텐츠 — 사도 {data.Heroes.Count} · 카드 {data.Cards.Count} · 적 {data.Enemies.Count} · 마을 {data.Villages.Count} · 이벤트 {data.Events.Count} · 장비 {data.Equips.Count} · 주의 {v.Warnings.Count}");
            }
            catch (System.Exception e) { why = e.Message; }
            bool have = Directory.Exists(dst) && Directory.GetFiles(dst, "*.json", SearchOption.AllDirectories).Length > 0;
            if (why != null)
            {
                if (!have) throw new System.Exception("[Setup] 코어 데이터를 못 읽고 스냅샷도 없다: " + why);
                Debug.LogWarning("[Setup] 코어 데이터가 지금 깨져 있다 — 지난 스냅샷을 쓴다: " + why);
                return;
            }
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            CopyJson(tmp, dst);
            Debug.Log("[Setup] 코어 데이터 ← " + src + " (검사 통과)");
        }

        static void CopyJson(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from, "*.json", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(from, f);
                // _ 로 시작하는 폴더(_gen · _measure — 만드는 도구 · 잰 기록)는 게임 데이터가 아니다 — 옮기지 않는다(파일 _그림.json 은 옮긴다)
                var dirs = Path.GetDirectoryName(rel);
                if (!string.IsNullOrEmpty(dirs) && System.Array.Exists(dirs.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), x => x.StartsWith("_"))) continue;
                var d = Path.Combine(to, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(d));
                File.Copy(f, d, true);
            }
        }

        // 압축 규칙(BolzenaImport)이 바뀌었으면 그림을 다시 가져온다 — 한 번 돌면 이후엔 건너뛴다
        static void Recompress()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Bolzena/Resources" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool big = !path.Contains("/Resources/UI/");
                var pc = ti.GetPlatformTextureSettings("Standalone");
                if (big == pc.overridden && (!big || pc.format == TextureImporterFormat.BC7)) continue;
                ti.SaveAndReimport();
                n++;
            }
            if (n > 0) Debug.Log("[Setup] 다시 압축한 그림 " + n + "장");
        }

        // 스파인 — 그림은 곧은 알파(copy_assets.py 가 PMA 를 풀어 넣었다) · sRGB. 재질은 Straight Alpha,
        // 싸움터 유닛은 흰 번쩍임 · 잔상 실루엣을 위해 Spine/Skeleton Fill
        static void SpineMaterials()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Bolzena/Resources/Spine" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null || (ti.sRGBTexture && ti.alphaIsTransparency)) continue;
                ti.sRGBTexture = true;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            // 판 화면 스파인(Assets/Resources/Spine — runui copy_assets.py: 미니미 · 스탠딩) — 곧은 알파 · 밉맵 없이 · 늘리지 않게(미니미 아틀라스 4096×3076)
            if (Directory.Exists("Assets/Resources/Spine"))
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Spine" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null || (ti.sRGBTexture && ti.maxTextureSize >= 4096 && !ti.mipmapEnabled && ti.npotScale == TextureImporterNPOTScale.None)) continue;
                    ti.sRGBTexture = true;
                    ti.alphaIsTransparency = false;
                    ti.maxTextureSize = 4096;
                    ti.mipmapEnabled = false;
                    ti.npotScale = TextureImporterNPOTScale.None;
                    ti.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.SaveAndReimport();
                }
            var fill = Shader.Find("Spine/Skeleton Fill");
            if (Directory.Exists("Assets/Resources/Spine"))
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Resources/Spine" }))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (m == null) continue;
                    m.SetFloat("_StraightAlphaInput", 1);
                    m.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                    EditorUtility.SetDirty(m);
                }
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Bolzena/Resources/Spine" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) continue;
                if (!path.Contains("/st_")) m.shader = fill;
                m.SetFloat("_FillPhase", 0);
                m.SetFloat("_StraightAlphaInput", 1);
                m.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                EditorUtility.SetDirty(m);
            }
        }

        static void AlwaysIncludedShaders()
        {
            var names = new[] { "Bolzena/Sprite", "Bolzena/FocusLines", "Bolzena/Rays", "Bolzena/Ring", "TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field",
                                "Spine/Skeleton Fill", "Spine/Skeleton", "Spine/SkeletonGraphic", "UI/Default" };
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            var have = new HashSet<Object>();
            for (int i = 0; i < arr.arraySize; i++) have.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var n in names)
            {
                var sh = Shader.Find(n);
                if (sh == null) { Debug.LogWarning("[Setup] 셰이더 없음: " + n); continue; }
                if (have.Contains(sh)) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Player()
        {
            PlayerSettings.companyName = "sadodesk";
            PlayerSettings.productName = "Bolzena";
            // 기본 — 테두리 없는 창 모드(FullScreenWindow) · 모니터 해상도. 창 모드 · 해상도는 설정 창의 「화면」(DisplayOptions)에서 바꾼다.
            // 데모 · 시험 실행은 명령줄(-screen-fullscreen 0 -screen-width …)로 창에서 돈다
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            // 쓰지 않는 코드 걷기 — 코어 · Newtonsoft(리플렉션) · 스파인은 link.xml 로 지킨다
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Medium);
        }

        static void Scene()
        {
            Directory.CreateDirectory("Assets/Bolzena/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.allowHDR = true;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.AddComponent<AudioListener>();
            var game = new GameObject("Game");
            game.AddComponent<BattleDirector>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 첫 장면 — 판 화면(com.bolzena.runui Flow, 화면 덮개 캔버스). 싸움 칸에서 Battle 장면으로 넘어갔다 돌아온다(BattleBridge)
            var run = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rc = new GameObject("Main Camera");
            rc.tag = "MainCamera";
            var rcam = rc.AddComponent<Camera>();
            rcam.orthographic = true;
            rcam.clearFlags = CameraClearFlags.SolidColor;
            rcam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            rcam.transform.position = new Vector3(0, 0, -10);
            rc.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            rc.AddComponent<AudioListener>();
            new GameObject("Run").AddComponent<RunBoot>();
            EditorSceneManager.SaveScene(run, RunScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RunScenePath, true), new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ── 웹(WebGL) 빌드 — bolzena.pages.dev ──
        //   Unity.exe -batchmode -quit -buildTarget WebGL -projectPath . -executeMethod Bolzena.EditorTools.ProjectSetup.SetupAndBuildWeb
        //   산출물 WebBuild/(.gitignore). 그 뒤 Tools/web_post.py 가 큰 파일을 25MB 아래 조각으로 나누고 index.html 을 쓴다(Cloudflare Pages 한도).
        //   웹은 StreamingAssets 를 폴더로 못 읽는다 — 콘텐츠 JSON 을 Resources/CoreDataPack 한 벌로 묶어 넣는다(RunPort.CoreDataDir 가 푼다).
        const string WebPath = "WebBuild";

        public static void SetupAndBuildWeb()
        {
            CoreData();
            CoreDataPack();
            AssetDatabase.Refresh();
            Recompress();
            SpineMaterials();
            AlwaysIncludedShaders();
            Player();
            WebPlayer();
            Scene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] 웹 준비 끝");
            BuildWeb();
        }

        static void CoreDataPack()
        {
            var src = Path.GetFullPath("Assets/StreamingAssets/CoreData");
            var paths = new List<string>(); var texts = new List<string>();
            foreach (var f in Directory.GetFiles(src, "*.json", SearchOption.AllDirectories))
            {
                paths.Add(Path.GetRelativePath(src, f).Replace('\\', '/'));
                texts.Add(File.ReadAllText(f));
            }
            Directory.CreateDirectory("Assets/Resources");
            File.WriteAllText("Assets/Resources/CoreDataPack.json", JsonUtility.ToJson(new WebPack { paths = paths.ToArray(), texts = texts.ToArray() }));
            Debug.Log("[Setup] 웹 콘텐츠 묶음 — 파일 " + paths.Count + "개");
        }
        [System.Serializable] class WebPack { public string[] paths; public string[] texts; }

        static void WebPlayer()
        {
            var t = NamedBuildTarget.WebGL;
            PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(t, ManagedStrippingLevel.Medium);
            PlayerSettings.SetIl2CppCodeGeneration(t, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);
            // 압축 gzip + 풀기 대비(decompression fallback) — Pages 는 Content-Encoding 을 붙이지 않는다. 로더가 브라우저에서 푼다
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;              // 두 번째부터는 IndexedDB 에서
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = "APPLICATION:Minimal"; // index.html 은 web_post.py 가 다시 쓴다
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });   // WebGL2
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
        }

        public static void BuildWeb()
        {
            if (Directory.Exists(WebPath)) Directory.Delete(WebPath, true);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { RunScenePath, ScenePath },
                locationPathName = WebPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[Build] 웹 {s.result} — {s.totalSize / (1024 * 1024)}MB, {s.totalTime.TotalSeconds:F0}s, 오류 {s.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void Build()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { RunScenePath, ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[Build] {s.result} — {s.totalSize / (1024 * 1024)}MB, {s.totalTime.TotalSeconds:F0}s, 오류 {s.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
