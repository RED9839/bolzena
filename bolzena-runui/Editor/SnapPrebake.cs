using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bolzena.RunUI.EditorTools
{
    // 사도 정지 그림 미리 굽기(2026-10-07 「사도 목록 보는 게 너무 무겁다」) — 실행 중 굽기(StandingSnap.Bake)와 같은 코드 · 같은 자르기로
    //   사도마다 셋을 PNG 로 구워 Assets/Resources/RunArt/Snap/<kind>/<art>.png 에 둔다(원작 파생 — 프로젝트의 Assets/Resources 는 .gitignore):
    //     list = 사도 목록 카드(ListRect · 비율 146/210 · 256×368) · face = 얼굴 칸(비율 1 · frac 0.34 · 256×256, 밉맵) · card = 카드 그림(CardRect · 0.70 · 448×640)
    //   index.txt 에 줄마다 「kind  art  비율  frac  자르기 사각형(원본 단위)  원본 표시」 — 실행 중에는 지금 표로 잰 사각형이 적힌 것과 같을 때만 쓴다.
    //   다시 굽기: 그 사도의 줄(사각형 · 비율)이나 스파인 파일(이름 · 크기 · 시각) · 굽기 판(Version)이 바뀐 것만. 빌드 Setup(PC · 웹)이 부른다.
    //   PC 는 BC7(RunUiImport), 웹은 WebImport 가 데스크톱 DXT5 크런치 · 폰 ASTC 6×6 으로.
    public static class SnapPrebake
    {
        const int Version = 1;
        public const string Dir = "Assets/Resources/RunArt/Snap";
        public const float ListRatio = 146f / 210f;   // 목록 카드 그림 칸(150-4)/(214-4) — 작은 화면 · 로비 고르기(0.694 ~ 0.695)도 0.6% 안
        static readonly (string kind, float ratio, float frac, int w, int h)[] Kinds =
        {
            ("list", ListRatio, 0.5f, 256, 368),
            ("face", 1f, 0.34f, 256, 256),
            ("card", StandingSnap.CardRatio, 0f, 448, 640),
        };

        [MenuItem("Bolzena/사도 정지 그림 미리 굽기")]
        public static void RunMenu() => Run();

        /// <summary>바뀐 사도만 굽는다. 구운 수를 돌려준다(빌드 Setup 이 부른다 — 스파인 · 재질 준비 뒤).</summary>
        public static int Run(bool force = false)
        {
            var t0 = System.DateTime.Now;
            if (SpineUi.StraightMat == null || !SystemInfo.supportsRenderTextures) { Debug.LogWarning("[Snap] 곧은 알파 재질 · 렌더 텍스처 없음 — 미리 굽기 건너뜀(실행 중에 굽는다)"); return 0; }
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) { Debug.LogWarning("[Snap] 그래픽 장치 없음(-nographics) — 미리 굽기 건너뜀"); return 0; }
            foreach (var k in Kinds) Directory.CreateDirectory(Path.Combine(Dir, k.kind));
            var indexPath = Path.Combine(Dir, "index.txt");
            var old = new Dictionary<string, string>();
            if (File.Exists(indexPath))
                foreach (var line in File.ReadAllLines(indexPath))
                {
                    var p = line.Split('\t');
                    if (p.Length >= 9 && !line.StartsWith("#")) old[p[0] + "|" + p[1]] = line;
                }
            var arts = StandingFit.Arts.Where(a => !a.StartsWith("_")).OrderBy(a => a, System.StringComparer.Ordinal).ToList();
            var lines = new List<string> { $"#v{Version}\tkind\tart\tratio\tfrac\tx\ty\tw\th\tsrc" };
            int made = 0, kept = 0, missing = 0;
            var wrote = new List<string>();
            try
            {
                int n = 0;
                foreach (var art in arts)
                {
                    string src = SourceStamp(art);
                    if (src == null) { missing++; continue; }   // 스탠딩 스파인 없음 — 실행 중에도 못 굽는다
                    bool any = false;
                    foreach (var k in Kinds)
                    {
                        var body = StandingSnap.SnapLine(k.kind, art, k.ratio, k.frac);
                        if (body == null) continue;
                        var line = body + "\t" + $"v{Version}:{k.w}x{k.h}:{src}";
                        var png = Path.Combine(Dir, k.kind, art + ".png");
                        if (!force && old.TryGetValue(k.kind + "|" + art, out var was) && was == line && File.Exists(png)) { lines.Add(line); kept++; continue; }
                        var p = line.Split('\t');
                        var inv = System.Globalization.CultureInfo.InvariantCulture;
                        var r = new Rect(float.Parse(p[4], inv), float.Parse(p[5], inv), float.Parse(p[6], inv), float.Parse(p[7], inv));
                        var tex = StandingSnap.Bake(art, r, k.w, k.h, null, true);
                        if (tex == null) { Debug.LogWarning($"[Snap] 굽기 실패 {k.kind} {art}"); continue; }
                        File.WriteAllBytes(png, tex.EncodeToPNG());
                        Object.DestroyImmediate(tex);
                        wrote.Add(png.Replace('\\', '/'));
                        lines.Add(line);
                        made++; any = true;
                    }
                    SpineUi.Drop("st_" + art);   // 파싱한 스켈레톤을 놓는다(편집 모드 DestroyImmediate 는 StandUser.OnDestroy 를 안 부른다)
                    if (any && ++n % 12 == 0) EditorUtility.UnloadUnusedAssetsImmediate();
                }
            }
            finally { StandingSnap.ResetHost(); }
            // 표에서 빠진 사도의 그림은 지운다
            var keep = new HashSet<string>(lines.Skip(1).Select(l => { var p = l.Split('\t'); return (Path.Combine(Dir, p[0], p[1] + ".png")).Replace('\\', '/'); }));
            int removed = 0;
            foreach (var k in Kinds)
                foreach (var f in Directory.GetFiles(Path.Combine(Dir, k.kind), "*.png"))
                    if (!keep.Contains(f.Replace('\\', '/'))) { AssetDatabase.DeleteAsset(f.Replace('\\', '/')); removed++; }
            File.WriteAllText(indexPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
            AssetDatabase.Refresh();
            EditorUtility.UnloadUnusedAssetsImmediate();
            long bytes = Kinds.Sum(k => Directory.GetFiles(Path.Combine(Dir, k.kind), "*.png").Sum(f => new FileInfo(f).Length));
            Debug.Log($"[Snap] 사도 정지 그림 미리 굽기 — 새로 {made} · 그대로 {kept} · 지움 {removed} · 스파인 없음 {missing} · PNG 합 {bytes / 1048576f:F1}MB · {(System.DateTime.Now - t0).TotalSeconds:F0}s");
            return made;
        }

        // 스탠딩 스파인 파일 표시(이름 · 내용 MD5) — 바뀌면 다시 굽는다. Resources 두 곳(Spine/st_<art>) 가운데 있는 것
        static string SourceStamp(string art)
        {
            var sb = new StringBuilder();
            foreach (var root in new[] { "Assets/Bolzena/Resources/Spine", "Assets/Resources/Spine" })
            {
                var d = Path.Combine(root, "st_" + art);
                if (!Directory.Exists(d)) continue;
                // 원본만(뼈 · 아틀라스 글 · 그림) — 재질 · 자산(.mat · .asset)은 Setup 이 다시 저장해 시각이 바뀐다
                foreach (var f in Directory.GetFiles(d).Where(x => x.EndsWith(".bytes") || x.EndsWith(".txt") || x.EndsWith(".json") || x.EndsWith(".png")).OrderBy(x => x, System.StringComparer.Ordinal))
                {
                    // 내용으로(시각은 안 본다 — 웹 빌드의 pad4.py 가 같은 그림을 다시 써 시각만 바뀐다). 사도 135명 원본 합 수백 MB · 몇 초
                    using var fs = File.OpenRead(f);
                    using var md = System.Security.Cryptography.MD5.Create();
                    sb.Append(Path.GetFileName(f)).Append(':').Append(System.BitConverter.ToString(md.ComputeHash(fs), 0, 8)).Append(';');
                }
            }
            if (sb.Length == 0) return null;
            using var md5 = System.Security.Cryptography.MD5.Create();
            var h = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return System.BitConverter.ToString(h, 0, 8).Replace("-", "").ToLowerInvariant();
        }
    }
}
