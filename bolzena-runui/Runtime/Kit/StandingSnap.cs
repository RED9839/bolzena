using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bolzena.RunUI
{
    // 정지 스탠딩 그림 — 스탠딩 스파인(Normal · Idle_1 첫 프레임)을 그 자리에서 한 번 구워 Sprite 로(2026-10 사용자: 「목록은 움직이지 않는 그림으로 ·
    //   기본 카드 · 편성표 사도 그림도 수정한 크기로」). 자르기는 스탠딩 맞춤 표(standing_fit.json — 중심 centerX · 몸 꼭대기 topY · 머리 headY)로:
    //     · 얼굴(frac < 0.45 — 초상 줄 · 머리표): 그 사도의 머리(topY − headY)로 머리 · 어깨가 차게
    //     · 그 밖(목록 상반신 · 카드 그림 · 마을 공개): 모두 같은 배율(안 B — 원작 키 그대로) — 자르는 높이 = frac × 기준 몸(705 단위), 위 끝 = 몸 꼭대기 위 조금
    //   웹판 정지 렌더(RunArt/Standing)는 프레임 · 테두리 자르기가 사도마다 달라 표와 맞지 않는다(몸이 비거나 엉뚱한 곳이 잘림) — 표가 있는 사도는 이것만 쓴다.
    //   그리기: 캔버스 없이 SkeletonGraphic 메시를 CommandBuffer 로 RenderTexture 에 그리고(곧은 알파 재질 — 흰 테두리 없음) 읽어 곧은 알파 Sprite 로.
    //   한 번 구운 것은 (그림 키 · 비율 · frac · 크기)로 붙잡아 둔다. 굽고 나면 스켈레톤 데이터는 캐시에서 뺀다.
    public static class StandingSnap
    {
        static RectTransform host;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static float[] lin;
        static int baked;

        /// <summary>얼굴이 아닌 자르기의 기준 몸(원본 단위) — frac 1 = 이 높이.</summary>
        public const float RefBody = 705f;

        static RectTransform Host
        {
            get
            {
                if (host != null) return host;
                var go = new GameObject("RunUI standing snap", typeof(RectTransform));
                Object.DontDestroyOnLoad(go);
                host = (RectTransform)go.transform;
                return host;
            }
        }

        /// <summary>그 사도의 자르기 사각형(원본 단위, y 위로). 표에 없으면 false.</summary>
        public static bool CropRect(string art, float ratio, float frac, out Rect r)
        {
            r = default;
            if (!StandingFit.TryGet(art, out var f)) return false;
            float h, top;
            bool face = frac < 0.45f;
            if (f.HasHead)
            {
                // 머리 상자 기준(standing_head.json) — 숙이거나 기운 포즈도 얼굴이 창 안에: 위 = 머리 둘레 상자 위 끝(머리카락 · 귀 · 뿔, 장신구 뺌),
                //   가로 = 얼굴 쪽(얼굴 칸은 얼굴 가운데 · 상반신은 몸과 얼굴 사이 얼굴 쪽 7할), 그래도 얼굴 상자가 옆으로 나가면 민다
                float R = Mathf.Clamp(f.TopY - f.HeadY, 100, 360);   // 머리 반지름은 몸 꼭대기(뿔 · 귀 뺌)까지 — 사슴뿔 · 큰 뿔이 얼굴을 밀어내지 않게
                h = face ? R * 1.35f * (frac / 0.34f) : frac * RefBody * f.Scale;
                // 위 = 머리 둘레 상자 위 끝(머리카락 · 귀 · 뿔)까지, 다만 몸 꼭대기 위로 창 높이의 8%(얼굴 칸 4%)까지만 — 뿔이 길면 뿔 끝이 잘린다(얼굴이 먼저)
                top = Mathf.Min(f.HeadBox.yMax, f.TopY + h * (face ? 0.04f : 0.08f)) + h * 0.03f;
                if (face && f.FaceY != 0) top = f.FaceY + h * 0.55f;   // 얼굴이 몸 위 끝이 아닌 사도(꿀벌 쥬비) — 얼굴을 칸 가운데 조금 위에
                float w0 = h * ratio;
                float cx = StandingFit.CropCenterX(f, StandMode.Upper, face);
                float m = w0 * 0.04f;
                if (f.FaceBox.xMin < cx - w0 / 2 + m) cx = f.FaceBox.xMin + w0 / 2 - m;
                else if (f.FaceBox.xMax > cx + w0 / 2 - m) cx = f.FaceBox.xMax - w0 / 2 + m;
                r = new Rect(cx - w0 / 2, top - h, w0, h);
                return true;
            }
            if (face)
            {
                float head = Mathf.Clamp(f.TopY - f.HeadY, 120, 320);
                h = head * (frac / 0.34f) * 1.55f;   // 머리 · 어깨(이등신이라 머리가 크다)
                top = f.TopY + h * 0.05f;
            }
            else
            {
                h = frac * RefBody * f.Scale;
                top = f.TopY + h * 0.06f;
            }
            // (머리 상자가 없는 사도) 머리카락 위 끝까지 담되, 상반신 창에서 머리 뼈가 창 위 62% 아래로 내려가지는 않게
            top = Mathf.Max(top, f.HairTop + h * 0.03f);
            if (!face) top = Mathf.Min(top, Mathf.Max(f.TopY + h * 0.02f, f.HeadY + h * 0.62f));
            float w = h * ratio;
            r = new Rect(f.CenterX - w / 2, top - h, w, h);
            return true;
        }

        /// <summary>
        /// 위쪽 자르기 Sprite — ratio = 가로/세로, frac = 얼마나 담을지(CardArt.Upper 와 같은 뜻 — 0.34 얼굴 · 0.5 상반신 · 0.56 카드).
        /// pxH = 구울 세로 픽셀(0 이면 얼굴 224 · 그 밖 640). 표 · 스파인이 없으면 null(부르는 쪽이 웹판 렌더로 대신).
        /// </summary>
        public static Sprite Upper(string art, float ratio, float frac, int pxH = 0)
        {
            if (string.IsNullOrEmpty(art) || ratio <= 0) return null;
            if (pxH <= 0) pxH = frac < 0.45f ? 224 : 640;
            string key = $"{art}|{ratio:F3}|{frac:F2}|{pxH}";
            if (cache.TryGetValue(key, out var s) && (s == null || s.texture != null)) return s;
            s = null;
            if (CropRect(art, ratio, frac, out var src))
            {
                int pxW = Mathf.Max(8, Mathf.RoundToInt(pxH * ratio));
                var tex = Bake(art, src, pxW, pxH);
                if (tex != null)
                {
                    s = Sprite.Create(tex, new Rect(0, 0, pxW, pxH), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
                    s.name = "snap " + art;
                }
            }
            if (cache.Count >= 160) cache.Clear();   // 상한 — 띄운 것은 화면이 쥐어 살고, 나머지 구운 그림은 다음 장면 정리 때 풀린다
            cache[key] = s;
            return s;
        }

        /// <summary>스탠딩 스파인을 src(원본 단위)만큼 w×h 픽셀 Texture2D(곧은 알파 · sRGB)로 굽는다. 스파인이 없으면 null.</summary>
        public static Texture2D Bake(string art, Rect src, int w, int h)
        {
            var mat0 = SpineUi.StraightMat;
            if (mat0 == null || !SystemInfo.supportsRenderTextures) return null;
            SkeletonGraphic g = SpineUi.NewStanding(Host, art);
            if (g == null) return null;
            RenderTexture rt = null;
            Material mat = null;
            try
            {
                g.Update(0);
                g.UpdateMesh();
                var mesh = g.GetLastMesh();
                var mainTex = g.mainTexture;
                if (mesh == null || mainTex == null) return null;
                // 메시 꼭짓점 = 원본 단위 × 데이터 배율 × meshScale(캔버스 없음 = 100)
                float u = (g.skeletonDataAsset != null ? g.skeletonDataAsset.scale : 0.01f) * g.MeshScale;
                mat = new Material(mat0) { mainTexture = mainTex };
                mat.SetVector("_ClipRect", new Vector4(-1e7f, -1e7f, 1e7f, 1e7f));
                mat.SetVector("_TextureSampleAdd", Vector4.zero);
                mat.SetColor("_Color", Color.white);
                rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.filterMode = FilterMode.Bilinear;
                var cb = new CommandBuffer { name = "RunUI standing snap" };
                cb.SetRenderTarget(rt);
                cb.ClearRenderTarget(false, true, Color.clear);
                var proj = Matrix4x4.Ortho(src.xMin * u, src.xMax * u, src.yMin * u, src.yMax * u, -1000f, 1000f);
                cb.SetViewProjectionMatrices(Matrix4x4.identity, proj);
                for (int sm = 0; sm < Mathf.Max(1, mesh.subMeshCount); sm++) cb.DrawMesh(mesh, Matrix4x4.identity, mat, sm, 0);
                Graphics.ExecuteCommandBuffer(cb);
                cb.Release();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { name = "snap " + art, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                RenderTexture.active = prev;
                Unpremultiply(tex);
                tex.Apply(false, true);   // 읽기 전용(시스템 메모리 사본 버림)
                return tex;
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (mat != null) Object.Destroy(mat);
                Object.Destroy(g.gameObject);
                SpineUi.Forget("st_" + art);
                // 스켈레톤 데이터 · 아틀라스는 무겁다 — 여럿 구운 뒤 한꺼번에 놓는다
                if (++baked % 16 == 0) Resources.UnloadUnusedAssets();
            }
        }

        // 그린 결과는 PMA(선형 공간에서 곱함 → sRGB 로 저장) — UI 기본 재질용 곧은 알파로: srgb(lin(c) / a)
        static void Unpremultiply(Texture2D t)
        {
            if (lin == null)
            {
                lin = new float[256];
                for (int i = 0; i < 256; i++) lin[i] = QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.GammaToLinearSpace(i / 255f) : i / 255f;
            }
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a == 0) { px[i] = new Color32(0, 0, 0, 0); continue; }
                if (c.a == 255) continue;
                float a = c.a / 255f;
                byte F(byte v)
                {
                    float x = Mathf.Min(1f, lin[v] / a);
                    if (linear) x = Mathf.LinearToGammaSpace(x);
                    return (byte)Mathf.Clamp(Mathf.RoundToInt(x * 255f), 0, 255);
                }
                px[i] = new Color32(F(c.r), F(c.g), F(c.b), c.a);
            }
            t.SetPixels32(px);
        }
    }
}
