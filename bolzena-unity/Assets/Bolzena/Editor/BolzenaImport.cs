using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 그림 가져오기 설정 — 경로로 정한다(손으로 고칠 일 없게)
    //   Resources/UI    스프라이트, 200px = 1 단위, *_9s 는 9칸(가장자리 고정 — make_ui.py 가 2배로 그려 400px = 1 단위 · 테두리도 2배).
    //                   밉맵을 켠다 — 폰처럼 작게 그릴 때 가장자리가 지글거리지 않게(4K 는 2배 그림이 맡는다)
    //   Resources/Art · Fx/Tex · Bg   스프라이트, 100px = 1 단위
    //   Resources/Fx/Sheets   낱장 묶음 — 그대로(크기 바꾸지 않음), 코드가 칸을 자른다
    //   압축 — 큰 그림(Art · Bg · Fx · 스파인)은 BC7(데스크톱 · 고품질), 화면 그림(UI · 9칸)은 작아서 그대로(가장자리 번짐 없이).
    //          모바일은 ASTC 6×6(안드로이드 · iOS 덮어쓰기). 빌드 260MB 의 대부분이 압축 안 한 RGBA32 였다
    public class BolzenaImport : AssetPostprocessor
    {
        public override uint GetVersion() => 3;   // 규칙이 바뀌면 올린다 — 이미 가져온 그림도 다시 가져온다

        static void Compress(TextureImporter ti, bool big)
        {
            ti.textureCompression = big ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            ti.crunchedCompression = false;
            var pc = ti.GetPlatformTextureSettings("Standalone");
            pc.overridden = big;
            pc.format = TextureImporterFormat.BC7;
            pc.maxTextureSize = 4096;
            ti.SetPlatformTextureSettings(pc);
            foreach (var plat in new[] { "Android", "iPhone" })
            {
                var m = ti.GetPlatformTextureSettings(plat);
                m.overridden = big;
                m.format = TextureImporterFormat.ASTC_6x6;
                m.maxTextureSize = 2048;
                ti.SetPlatformTextureSettings(m);
            }
        }

        // 사도 소리(고학년 효과음 · 목소리 — 135명, 원본 WAV) — 빌드가 붓지 않게 모노 · Vorbis 60%
        void OnPreprocessAudio()
        {
            var p = assetPath.Replace("\\", "/");
            if (!(p.Contains("/Resources/Voice/") || p.Contains("/Resources/Sfx/hero/"))) return;
            var ai = (AudioImporter)assetImporter;
            ai.forceToMono = true;
            var st = ai.defaultSampleSettings;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            st.quality = 0.6f;
            st.loadType = AudioClipLoadType.DecompressOnLoad;
            ai.defaultSampleSettings = st;
        }

        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.StartsWith("Assets/Bolzena/Resources/")) return;
            var ti = (TextureImporter)assetImporter;
            if (p.Contains("/Resources/Spine/st_"))
            {
                // 스탠딩 스파인(135명 · 아틀라스 2억 픽셀) — BC7 이면 빌드가 200MB 넘게 붓는다. 크런치(DXT5) · 최대 2048
                Compress(ti, true);
                var pc = ti.GetPlatformTextureSettings("Standalone");
                pc.overridden = false;
                ti.SetPlatformTextureSettings(pc);
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.crunchedCompression = true;
                ti.compressionQuality = 60;
                ti.maxTextureSize = 2048;
                return;
            }
            if (p.Contains("/Resources/Spine/")) { Compress(ti, true); return; }   // 스파인 아틀라스 — 설정은 spine-unity 가, 압축만 여기서
            bool ui = p.Contains("/Resources/UI/");
            ti.mipmapEnabled = ui && !p.EndsWith("/white.png");   // 단색 조각은 밉맵 없이(화면 크기로 늘려 덮개로 쓴다)
            if (ui) { ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter; ti.borderMipmap = false; ti.mipMapsPreserveCoverage = false; }
            ti.alphaIsTransparency = true;
            Compress(ti, !p.Contains("/Resources/UI/"));
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 4096;
            if (p.Contains("/Fx/Sheets/"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.npotScale = TextureImporterNPOTScale.None;
                return;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);
            bool nine = p.EndsWith("_9s.png");
            ti.spritePixelsPerUnit = ui ? (nine ? 400 : 200) : 100;
            if (nine)
            {
                float b = p.Contains("panel_9s") || p.Contains("btn_9s") || p.Contains("cell") ? 48 : p.Contains("pill_") ? 62 : 24;
                ti.spriteBorder = new Vector4(b, b, b, b);
            }
        }
    }
}
