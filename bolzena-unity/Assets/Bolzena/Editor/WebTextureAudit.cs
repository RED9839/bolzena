using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 웹 그림 점검 — WebGL 로 가져온 결과(형식 · 크기 · 밉맵)를 갈래별로 센다. 압축 안 된 채(RGBA32 등)로 남은 큰 그림을 「[WebTex] 안 눌림」 줄로 남긴다.
    //   같은 사고(「2의 거듭제곱 아님 + 밉맵」 · 「4의 배수 아님」이면 DXT 설정이어도 RGBA32 로 떨어진다 — WebImport v2) 재발 막기.
    //   웹 빌드(ProjectSetup.SetupAndBuildWeb) 전에 저절로 돈다. 손으로: Unity.exe -batchmode -quit -buildTarget WebGL -executeMethod Bolzena.EditorTools.WebTextureAudit.Run
    public static class WebTextureAudit
    {
        static readonly string[] Roots = { "Assets/Bolzena/Resources", "Assets/Resources", "Assets/BolzenaFxData/Src" };

        static bool Packed(TextureFormat f) => f.ToString().StartsWith("DXT") || f.ToString().StartsWith("BC") || f.ToString().StartsWith("ETC") || f.ToString().StartsWith("ASTC");

        static string Group(string p)
        {
            if (p.Contains("/BolzenaFxData/Src/baked/")) return "fx baked";
            if (p.Contains("/BolzenaFxData/Src/")) return "fx 원본";
            if (p.Contains("/Resources/UI/") || p.Contains("/Resources/RunUI/")) return "UI(일부러 그대로)";
            if (p.Contains("/RunArt/CardPic/")) return "카드 그림";
            if (p.Contains("/RunArt/CardObj/")) return "카드 사물 그림";
            if (p.Contains("/Spine/st_")) return "스탠딩 스파인";
            if (p.Contains("/Spine/")) return "스파인";
            if (p.Contains("/Art/Monster/")) return "적 아이콘";
            if (p.Contains("/RunArt/")) return "판 그림";
            if (p.Contains("/Fx/")) return "Fx";
            return "그 밖";
        }

        public static void Run()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                Debug.LogWarning("[WebTex] 지금 대상이 WebGL 이 아니다(" + EditorUserBuildSettings.activeBuildTarget + ") — 보이는 형식은 그 대상의 것");
            var stat = new Dictionary<string, Dictionary<string, (int n, long bytes)>>();
            var bad = new List<(string p, string f, int w, int h, int mips, long b)>();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", Roots))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (t == null) continue;
                long b = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                var gr = Group(p);
                var key = t.format + (t.mipmapCount > 1 ? "+mip" : "");
                if (!stat.TryGetValue(gr, out var d)) stat[gr] = d = new Dictionary<string, (int, long)>();
                d.TryGetValue(key, out var v);
                d[key] = (v.n + 1, v.bytes + b);
                if (!Packed(t.format) && !gr.StartsWith("UI") && t.width * t.height >= 64 * 64) bad.Add((p, t.format.ToString(), t.width, t.height, t.mipmapCount, b));
                Resources.UnloadAsset(t);
            }
            foreach (var kv in stat.OrderBy(x => x.Key))
                Debug.Log("[WebTex] " + kv.Key + " — " + string.Join(" · ", kv.Value.OrderByDescending(x => x.Value.bytes).Select(x => $"{x.Key} {x.Value.n}장 {x.Value.bytes / 1048576f:F1}MB")));
            foreach (var x in bad.OrderByDescending(x => x.b).Take(40))
                Debug.Log($"[WebTex] 안 눌림 {x.f} {x.w}×{x.h} 밉 {x.mips} {x.b / 1048576f:F2}MB {x.p}");
            Debug.Log($"[WebTex] 안 눌린 큰 그림 {bad.Count}장 · {bad.Sum(x => x.b) / 1048576f:F1}MB(메모리 기준)");
        }
    }
}
