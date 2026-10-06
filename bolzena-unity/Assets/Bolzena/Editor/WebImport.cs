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
    public class WebImport : AssetPostprocessor
    {
        const int Quality = 50;
        public override int GetPostprocessOrder() => 1000;
        public override uint GetVersion() => 2;   // 규칙이 바뀌면 올린다

        enum Kind { None, Res, Baked, Fx }

        static bool Web => EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
        static bool Pow2(int n) => n > 0 && (n & (n - 1)) == 0;

        /// <summary>웹 빌드 동안 Resources 밖(WebBundleBuild.Stage)으로 옮겨 둔 그림의 원래 경로 — 가져오기 규칙이 같은 설정을 주게.</summary>
        public static string Original(string assetPath)
        {
            var p = assetPath.Replace('\\', '/');
            if (p.StartsWith(WebBundleBuild.Stage + "/Spine/")) return "Assets/Bolzena/Resources/Spine/" + p.Substring(WebBundleBuild.Stage.Length + "/Spine/".Length);
            if (p.StartsWith(WebBundleBuild.Stage + "/RunSpine/")) return "Assets/Resources/Spine/" + p.Substring(WebBundleBuild.Stage.Length + "/RunSpine/".Length);
            return p;
        }

        static Kind KindOf(string p)
        {
            if (p.StartsWith("Assets/BolzenaFxData/Src/")) return p.Contains("/baked/") ? Kind.Baked : Kind.Fx;
            if (!(p.StartsWith("Assets/Bolzena/Resources/") || p.StartsWith("Assets/Resources/"))) return Kind.None;
            if (p.Contains("/Resources/UI/") || p.Contains("/Resources/RunUI/")) return Kind.None;
            return Kind.Res;
        }

        void OnPreprocessTexture()
        {
            var p = Original(assetPath);
            var k = KindOf(p);
            if (k == Kind.None) return;
            var ti = (TextureImporter)assetImporter;
            var w = ti.GetPlatformTextureSettings("WebGL");
            w.overridden = true;
            w.compressionQuality = Quality;
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
