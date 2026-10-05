using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Bolzena.RunUI.EditorTools
{
    // 명령줄에서 부르는 손 — 시험 프로젝트를 꾸리고 빌드한다(에디터 화면 없이).
    //   Unity.exe -batchmode -quit -projectPath <시험 프로젝트> -executeMethod Bolzena.RunUI.EditorTools.RunUiSetup.SetupAndBuild
    // 하는 일: 코어 샘플 데이터 → Assets/Resources/CoreData · 스파인 그림 설정 · 늘 넣을 셰이더 · 플레이어 설정 · 장면 · 빌드
    public static class RunUiSetup
    {
        const string ScenePath = "Assets/RunUITest/RunUI.unity";
        const string BuildPath = "Build/BolzenaRunUI.exe";

        public static void Setup()
        {
            CopyCoreData();
            AssetDatabase.Refresh();
            SpineTextures();
            AlwaysIncludedShaders();
            Player();
            Scene();
            AssetDatabase.SaveAssets();
            Debug.Log("[RunUiSetup] 끝");
        }

        public static void SetupAndBuild()
        {
            Setup();
            Build();
        }

        // 코어 패키지의 샘플 콘텐츠를 StreamingAssets/CoreData 로 떠 둔다(스냅샷) — 런타임은 코어의 GameData.FromFolder 로 그대로 읽는다
        // (폴더 꼴 · 파일 꼴이 바뀌어도 코어가 읽는 대로). 다른 에이전트가 쓰는 중에 깨진 순간을 뜰 수 있어서,
        // 먼저 원본을 코어로 읽어 보고 오류가 없을 때만 바꾼다 — 깨졌으면 앞의 스냅샷을 그대로 둔다.
        public static void CopyCoreData()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.bolzena.core");
            if (info == null) { Debug.LogError("[RunUiSetup] com.bolzena.core 없음"); return; }
            var src = Path.Combine(info.resolvedPath, "Data", "Sample");
            var dst = Path.Combine("Assets", "StreamingAssets", "CoreData");
            try
            {
                var d = Bolzena.Core.GameData.FromFolder(src);
                var v = Bolzena.Core.Validator.Check(d);
                if (d.Heroes.Count == 0 || d.Villages.Count == 0) throw new System.Exception($"사도 {d.Heroes.Count} · 마을 {d.Villages.Count}");
                if (v.Errors.Count > 0) Debug.LogWarning("[RunUiSetup] 데이터 검사 오류 " + v.Errors.Count + " — " + string.Join(" / ", v.Errors.GetRange(0, System.Math.Min(5, v.Errors.Count))));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[RunUiSetup] 코어 데이터를 읽지 못해 앞의 스냅샷을 씁니다 — " + e.Message);
                return;
            }
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            foreach (var f in Directory.GetFiles(src, "*.json", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(src, f);
                var to = Path.Combine(dst, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Copy(f, to, true);
            }
            // 옛 자리(Resources/CoreData)는 지운다
            if (Directory.Exists("Assets/Resources/CoreData")) AssetDatabase.DeleteAsset("Assets/Resources/CoreData");
            Debug.Log("[RunUiSetup] 코어 데이터 스냅샷 ← " + src);
        }

        // 스파인 그림 — 곧은 알파 · sRGB · 4096(미니미 아틀라스가 4096×3076)
        static void SpineTextures()
        {
            if (!Directory.Exists("Assets/Resources/Spine")) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Spine" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null || (ti.sRGBTexture && ti.maxTextureSize >= 4096 && !ti.mipmapEnabled && ti.npotScale == TextureImporterNPOTScale.None
                    && ti.textureCompression == TextureImporterCompression.Uncompressed)) continue;
                ti.sRGBTexture = true;
                ti.alphaIsTransparency = false;
                ti.maxTextureSize = 4096;
                ti.mipmapEnabled = false;
                ti.npotScale = TextureImporterNPOTScale.None;     // 원작 아틀라스는 2의 거듭제곱이 아니다 — 늘리면 흐려진다
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
        }

        static void AlwaysIncludedShaders()
        {
            var names = new[] { "TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field", "Spine/SkeletonGraphic", "UI/Default" };
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            var have = new HashSet<Object>();
            for (int i = 0; i < arr.arraySize; i++) have.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var n in names)
            {
                var sh = Shader.Find(n);
                if (sh == null) { Debug.LogWarning("[RunUiSetup] 셰이더 없음: " + n); continue; }
                if (have.Contains(sh)) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Player()
        {
            PlayerSettings.companyName = "sadodesk";
            PlayerSettings.productName = "BolzenaRunUI";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // 기본 = 테두리 없는 창 모드(DisplayOptions.DefaultMode). 데모는 -screen-fullscreen 0 으로 창
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        }

        static void Scene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            cam.transform.position = new Vector3(0, 0, -10);
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            camGo.AddComponent<AudioListener>();
            var game = new GameObject("RunUI");
            game.AddComponent<RunUiBoot>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        public static void Build()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
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

    // 화면 그림 가져오기 — 패키지 RunUI 그림 · 원작 RunArt: 밉맵 없이 · 투명 그대로 · 압축은 높은 품질(스탠딩 · 카드 아이콘만 밉맵)
    public class RunUiImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!(p.Contains("/Resources/RunUI/") || p.Contains("/Resources/RunArt/"))) return;
            var ti = (TextureImporter)assetImporter;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (p.Contains("/Sprites/")) ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            if (p.Contains("/RunArt/Standing/"))
            {
                // 사도 스탠딩 135장 — 목록 · 카드에서는 작게(밉맵 · 트라이리니어로 지글거리지 않게), 상세에서는 크게.
                // 크런치 압축으로 빌드가 크게 붓지 않게 한다(그림 결이 부드러워 품질 차이가 잘 안 보인다)
                ti.mipmapEnabled = true;
                ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                ti.filterMode = FilterMode.Trilinear;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.crunchedCompression = true;
                ti.compressionQuality = 70;
            }
            else if (p.Contains("/RunArt/Skill/") || p.Contains("/RunArt/Item/"))
            {
                ti.mipmapEnabled = !p.EndsWith("_blur.png");   // 아이콘은 카드가 작을 때 줄어든다 · 흐린 바탕은 늘려만 쓴다
                ti.filterMode = FilterMode.Trilinear;
            }
        }
    }
}
