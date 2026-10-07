using UnityEditor;

namespace Bolzena.EditorTools
{
    // 웹(WebGL) 그림 압축 — 다른 규칙(BolzenaImport · RunUiImport · FxImport)이 다 정한 뒤에 WebGL 덮어쓰기만 얹는다(PC · 폰 설정은 건드리지 않는다).
    //   큰 그림(스파인 · 스탠딩 · 카드 · 아이콘 · 배경 · 효과)은 DXT 크런치 — 내려받는 크기가 BC7 의 1/4~1/6.
    //   데스크톱 브라우저는 모두 DXT(S3TC)를 읽는다. 폰 브라우저(ASTC 만)는 유니티가 풀어서 올린다 — 느리고 메모리를 더 먹는다.
    //   작은 화면 그림(UI · RunUI 스프라이트)은 압축하지 않은 그대로(가장자리 번짐 없이 · 밉맵도 그대로 — 합쳐 4MB).
    //
    // v2(2026-10-06 웹 용량 줄이기 — bolzena-unity-tmp/web_size/plan.md · result.md):
    //   WebGL 은 「2의 거듭제곱이 아닌 크기 + 밉맵」이거나 「4의 배수가 아닌 크기」면 DXT 로 못 넣고 압축 안 된 RGBA32 로 떨어진다
    //   (설정이 DXT5 크런치여도 — 카드 그림 268장 · 구운 이펙트 19장 · 이펙트 원본 300여 장 · 적 아이콘 443장이 그랬다).
    //   그래서 WebGL 로 가져올 때만(EditorUserBuildSettings.activeBuildTarget — 그림 가져오기 결과는 플랫폼마다 따로 쌓인다):
    //     · 2의 거듭제곱이 아닌 그림은 밉맵을 끈다(카드 그림 364×512 · 판 아이콘 일부). 2의 거듭제곱(스탠딩 렌더 1024² 등)은 그대로.
    //     · 이펙트 원본(BolzenaFxData/Src/fx · fx2)은 밉맵을 끈다(입자라 크게 줄여 그릴 일이 적다) · 4의 배수가 아니면 가까운 2의 거듭제곱으로 늘린다
    //       (입자 UV 는 0~1 비율이라 그림 크기가 바뀌어도 같은 자리를 읽는다).
    //     · 구운 이펙트(baked)는 밉맵 끄고 DXT5(크런치 없이 — 날카로운 타격 시트가 무르지 않게), 원래 해상도.
    //     · 4의 배수가 아닌 원본(적 아이콘 202×202 · 구운 시트 · Fx/Sheets)은 Tools/pad4.py 가 원본 PNG 를 투명 픽셀로 채운다(웹 빌드 때 저절로).
    //       유니티는 원본 크기를 보고 형식을 정한다 — 가져온 뒤(OnPostprocessTexture)에 늘리면 RGBA32 그대로였다.
    //   PC(Standalone) · 폰 가져오기는 바뀌지 않는다.
    //
    // v3(2026-10-07 폰 ASTC 판): 웹 빌드를 두 벌 만든다(ProjectSetup.SetupAndBuildWeb — 유니티 문서 「Texture compression in Web」의 두 벌 빌드).
    //   데스크톱 = DXT(위 규칙 그대로), 폰 = ASTC(WebGLTextureSubtarget.ASTC — index.html 이 WEBGL_compressed_texture_astc 를 보고 .data 를 고른다).
    //   그림마다 WebGL 덮어쓰기를 걸어 두었으므로(빌드 설정의 압축 형식보다 덮어쓰기가 이긴다) 서브타깃을 읽어 형식을 직접 고른다.
    //   ASTC 일 때:
    //     · 스파인 아틀라스 · 카드 · 아이콘 · 배경 · Fx/Sheets = ASTC 6×6(3.56bpp — RGBA32 의 1/9). 스파인은 최대 2048(안드로이드 · iOS 덮어쓰기와 같은 한도 — 폰 화면엔 넉넉)
    //     · 구운 이펙트(baked) = ASTC 5×5(날카로운 타격 시트) · 원래 해상도(코드가 픽셀로 칸을 자른다)
    //     · 이펙트 원본(fx · fx2) = ASTC 8×8(흐린 입자라 덜 보인다 — 받는 크기 · 메모리 절반)
    //     · UI · RunUI 스프라이트 = 두 판 모두 압축 없이(아래 OnPreprocessTexture 의 Kind.Ui 풀이)
    //   서브타깃이 바뀌면 다시 가져오게 사용자 의존(Dep)을 건다 — ProjectSetup.SwitchWebTextures 가 서브타깃을 바꾸고 RegisterSubtarget() 을 부른 뒤,
    //   그래도 형식이 안 맞는 그림(의존을 걸기 전에 가져온 것)은 골라 다시 가져온다.
    public class WebImport : AssetPostprocessor
    {
        const int Quality = 50;
        // 미리 구운 사도 정지 그림(RunArt/Snap — runui SnapPrebake: 목록 카드 · 얼굴 칸 · 카드 그림)은 얼굴이 크게 보여 크런치 품질을 조금 높인다(폰 ASTC 6×6 은 그대로)
        const int SnapQuality = 70;
        public override int GetPostprocessOrder() => 1000;
        // 규칙이 바뀌면 올린다 — 단, 올리면 PC(Standalone) 그림까지 전부 다시 가져온다(20분 넘게). v3 의 ASTC 갈래는 DXT · PC 결과를 바꾸지 않아
        // 2 그대로 두고, ASTC 로 아직 안 가져온 그림은 ProjectSetup.SwitchWebTextures 가 골라 다시 가져온다
        public override uint GetVersion() => 2;

        internal enum Kind { None, Res, Baked, Fx, Ui }

        public const string Dep = "bolzena/webTextureSubtarget";

        static bool Web => EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
        public static bool Astc => EditorUserBuildSettings.webGLBuildSubtarget == WebGLTextureSubtarget.ASTC;

        /// <summary>지금 서브타깃을 사용자 의존 값으로 적는다 — 바뀌었으면 의존하는 그림이 다음 Refresh 때 다시 가져와진다.</summary>
        public static void RegisterSubtarget()
        {
            AssetDatabase.RegisterCustomDependency(Dep, UnityEngine.Hash128.Compute(Astc ? "astc" : "dxt"));
        }
        static bool Pow2(int n) => n > 0 && (n & (n - 1)) == 0;

        /// <summary>웹 빌드 동안 Resources 밖(WebBundleBuild.Stage)으로 옮겨 둔 그림의 원래 경로 — 가져오기 규칙이 같은 설정을 주게.</summary>
        public static string Original(string assetPath)
        {
            var p = assetPath.Replace('\\', '/');
            if (p.StartsWith(WebBundleBuild.Stage + "/Spine/")) return "Assets/Bolzena/Resources/Spine/" + p.Substring(WebBundleBuild.Stage.Length + "/Spine/".Length);
            if (p.StartsWith(WebBundleBuild.Stage + "/RunSpine/")) return "Assets/Resources/Spine/" + p.Substring(WebBundleBuild.Stage.Length + "/RunSpine/".Length);
            return p;
        }

        internal static Kind KindOf(string p)
        {
            if (p.StartsWith("Assets/BolzenaFxData/Src/")) return p.Contains("/baked/") ? Kind.Baked : Kind.Fx;
            if (!(p.StartsWith("Assets/Bolzena/Resources/") || p.StartsWith("Assets/Resources/"))) return Kind.None;
            if (p.Contains("/Resources/UI/") || p.Contains("/Resources/RunUI/")) return Kind.Ui;
            return Kind.Res;
        }

        void OnPreprocessTexture()
        {
            var p = Original(assetPath);
            var k = KindOf(p);
            if (k == Kind.None) return;
            var ti = (TextureImporter)assetImporter;
            if (k == Kind.Ui)
            {
                // UI 는 두 판 모두 압축 없이(덮어쓰기 없음 — 가장자리 번짐 없이 · 밉맵 그대로). 큰 것(카드 틀 760×1080 등)은 2의 거듭제곱이 아닌
                // 밉맵 그림이라 ASTC 로도 못 들어가 RGBA32 로 남고, 들어가는 작은 것은 합쳐 16MB 남짓이라 이득이 작다.
                // 한때 ASTC 판에서 건 덮어쓰기가 .meta 에 저장돼(SaveAssets) 데스크톱 판 UI 까지 ASTC 로 나갔다 — 남아 있으면 푼다
                var u = ti.GetPlatformTextureSettings("WebGL");
                if (u.overridden) { u.overridden = false; ti.SetPlatformTextureSettings(u); }
                return;
            }
            if (Web) context.DependsOnCustomDependency(Dep);   // PC 결과는 서브타깃과 상관없다
            if (Web && Astc) { PreprocessAstc(ti, p, k); return; }
            var w = ti.GetPlatformTextureSettings("WebGL");
            w.overridden = true;
            w.compressionQuality = p.Contains("/RunArt/Snap/") ? SnapQuality : Quality;
            switch (k)
            {
                case Kind.Res:
                    // 카드 원작 그림(CardPic)은 268장 모두 불투명 — 알파 없는 DXT1 이 DXT5 의 반
                    w.format = p.EndsWith(".jpg") || p.Contains("/RunArt/CardPic/") ? TextureImporterFormat.DXT1Crunched : TextureImporterFormat.DXT5Crunched;
                    w.crunchedCompression = true;
                    w.maxTextureSize = p.Contains("/Spine/") ? 4096 : 2048;
                    break;
                case Kind.Baked:
                    w.format = TextureImporterFormat.DXT5;
                    w.crunchedCompression = false;
                    w.maxTextureSize = 4096;   // 가장 큰 시트 2048×3852 — 원래 해상도
                    break;
                case Kind.Fx:
                    w.format = TextureImporterFormat.DXT5Crunched;
                    w.crunchedCompression = true;
                    w.maxTextureSize = 1024;   // FxImport 와 같게
                    break;
            }
            ti.SetPlatformTextureSettings(w);

            if (!Web) return;
            WebMips(ti, k);
        }

        // 폰(ASTC) 판 — 형식만 다르고 밉맵 규칙은 DXT 판과 같다
        void PreprocessAstc(TextureImporter ti, string p, Kind k)
        {
            var w = ti.GetPlatformTextureSettings("WebGL");
            w.overridden = true;
            w.compressionQuality = Quality;
            w.crunchedCompression = false;   // ASTC 에는 크런치가 없다
            switch (k)
            {
                case Kind.Baked:
                    w.format = TextureImporterFormat.ASTC_5x5;
                    w.maxTextureSize = 4096;
                    break;
                case Kind.Fx:
                    w.format = TextureImporterFormat.ASTC_8x8;
                    w.maxTextureSize = 1024;
                    break;
                default:
                    w.format = TextureImporterFormat.ASTC_6x6;
                    // 미리 구운 정지 그림(runui SnapPrebake)은 폰에서 작게 — 카드 그림 448×640 → 358×512 · 목록 카드 256×368 → 178×256
                    //   (폰 화면의 목록 카드는 200px 남짓 · 크게 본 카드도 500px 남짓). 정지 그림 셋 합이 첫 로딩 +20MB 안에 들게
                    w.maxTextureSize = p.Contains("/RunArt/Snap/card/") ? 512 : p.Contains("/RunArt/Snap/list/") ? 256 : 2048;
                    break;
            }
            ti.SetPlatformTextureSettings(w);
            WebMips(ti, k);
        }

        static void WebMips(TextureImporter ti, Kind k)
        {
            ti.GetSourceTextureWidthAndHeight(out int sw, out int sh);
            bool pot = Pow2(sw) && Pow2(sh);
            bool by4 = sw % 4 == 0 && sh % 4 == 0;
            bool sprite = ti.textureType == TextureImporterType.Sprite;
            switch (k)
            {
                case Kind.Fx:
                    ti.mipmapEnabled = false;
                    if (!by4 && !sprite) ti.npotScale = TextureImporterNPOTScale.ToNearest;
                    break;
                case Kind.Baked:
                    ti.mipmapEnabled = false;
                    break;
                default:
                    if (ti.mipmapEnabled && !pot && (sprite || ti.npotScale == TextureImporterNPOTScale.None)) ti.mipmapEnabled = false;
                    break;
            }
        }
    }
}
