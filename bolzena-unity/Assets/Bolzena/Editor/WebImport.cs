using UnityEditor;

namespace Bolzena.EditorTools
{
    // 웹(WebGL) 그림 압축 — 다른 규칙(BolzenaImport · RunUiImport)이 다 정한 뒤에 WebGL 덮어쓰기만 얹는다(PC · 폰 설정은 건드리지 않는다).
    //   큰 그림(스파인 · 스탠딩 · 카드 · 아이콘 · 배경 · 효과)은 DXT 크런치 — 내려받는 크기가 BC7 의 1/4~1/6.
    //   데스크톱 브라우저는 모두 DXT(S3TC)를 읽는다. 폰 브라우저(ASTC 만)는 유니티가 풀어서 올린다 — 느리고 메모리를 더 먹는다.
    //   작은 화면 그림(UI · RunUI 스프라이트)은 압축하지 않은 그대로(가장자리 번짐 없이).
    public class WebImport : AssetPostprocessor
    {
        const int Quality = 50;
        public override int GetPostprocessOrder() => 1000;
        public override uint GetVersion() => 1;   // 규칙이 바뀌면 올린다

        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!(p.StartsWith("Assets/Bolzena/Resources/") || p.StartsWith("Assets/Resources/"))) return;
            if (p.Contains("/Resources/UI/") || p.Contains("/Resources/RunUI/")) return;
            var ti = (TextureImporter)assetImporter;
            var w = ti.GetPlatformTextureSettings("WebGL");
            w.overridden = true;
            w.format = p.EndsWith(".jpg") ? TextureImporterFormat.DXT1Crunched : TextureImporterFormat.DXT5Crunched;
            w.compressionQuality = Quality;
            w.crunchedCompression = true;
            w.maxTextureSize = p.Contains("/Spine/") ? 4096 : 2048;
            ti.SetPlatformTextureSettings(w);
        }
    }
}
