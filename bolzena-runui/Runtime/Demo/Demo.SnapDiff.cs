using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 미리 구운 정지 그림 대조(-demo-snapdiff) — 사도마다 목록 카드 · 얼굴 칸 · 카드 그림을 그 자리에서 같은 크기로 굽고(StandingSnap.Bake — 예전 길),
    //   미리 구운 그림(빌드에 든 압축 그림)을 같은 크기 렌더 텍스처에 옮겨 읽어 픽셀마다 견준다. 사도별 평균 차이 · 99% 차이 · 최대를
    //   [SnapDiff] 줄로 · 종류별 합계(PSNR)를 낸다. -snapdiff-dump <폴더> 를 주면 둘을 나란히 PNG 로 쓴다.
    public partial class Demo
    {
        IEnumerator SnapDiff_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            string dump = Arg("-snapdiff-dump");
            if (dump != null) System.IO.Directory.CreateDirectory(dump);
            var kinds = new (string kind, float ratio, float frac, bool list, bool card)[]
            {
                ("list", 146f / 210f, 0.5f, true, false), ("face", 1f, 0.34f, false, false), ("card", StandingSnap.CardRatio, 0f, false, true),
            };
            var arts = StandingFit.Arts.Where(a => !a.StartsWith("_")).OrderBy(a => a, System.StringComparer.Ordinal).ToList();
            foreach (var k in kinds)
            {
                double sumSq = 0, sumAbs = 0; long px = 0; int n = 0, miss = 0, worstD = 0; string worst = "-";
                var per = new System.Text.StringBuilder();
                foreach (var art in arts)
                {
                    if (!StandingSnap.HasPrebaked(art, k.ratio, k.frac, k.list, k.card)) { miss++; continue; }
                    var pre = Resources.Load<Texture2D>(StandingSnap.SnapRoot + "/" + k.kind + "/" + art);
                    if (pre == null || !StandingSnap.CropRect(art, k.ratio, k.frac, out var r, k.list, k.card)) { miss++; continue; }
                    int w = pre.width, h = pre.height;
                    var live = StandingSnap.Bake(art, r, w, h, null, true);
                    if (live == null) { miss++; continue; }
                    var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    Graphics.Blit(pre, rt);
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var got = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                    got.ReadPixels(new Rect(0, 0, w, h), 0, 0, false); got.Apply();
                    RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                    var a = live.GetPixels32(); var b = got.GetPixels32();
                    double sq = 0, ab = 0; int mx = 0; var hist = new int[256];
                    for (int i = 0; i < a.Length; i++)
                    {
                        // 곧은 알파 — 보이는 색으로(알파를 곱해) 견준다(투명한 곳의 색은 보이지 않는다)
                        float fa = a[i].a / 255f, fb = b[i].a / 255f;
                        int dr = Mathf.Abs(Mathf.RoundToInt(a[i].r * fa - b[i].r * fb)), dg = Mathf.Abs(Mathf.RoundToInt(a[i].g * fa - b[i].g * fb)), db = Mathf.Abs(Mathf.RoundToInt(a[i].b * fa - b[i].b * fb)), da = Mathf.Abs(a[i].a - b[i].a);
                        int d = Mathf.Max(Mathf.Max(dr, dg), Mathf.Max(db, da));
                        sq += (dr * dr + dg * dg + db * db + da * da) / 4.0; ab += (dr + dg + db + da) / 4.0;
                        hist[d]++; if (d > mx) mx = d;
                    }
                    int p99 = 0; long acc = 0; for (int i = 0; i < 256; i++) { acc += hist[i]; if (acc >= a.Length * 0.99) { p99 = i; break; } }
                    sumSq += sq; sumAbs += ab; px += a.Length; n++;
                    double psnr = sq > 0 ? 10 * System.Math.Log10(255.0 * 255.0 / (sq / a.Length)) : 99;
                    per.Append($"  {k.kind}\t{art}\t{w}x{h}\tmean={ab / a.Length:F2}\tp99={p99}\tmax={mx}\tpsnr={psnr:F1}\n");
                    if (p99 > worstD) { worstD = p99; worst = art; }
                    if (dump != null && n <= 400)
                    {
                        var side = new Texture2D(w * 2 + 4, h, TextureFormat.RGBA32, false, false);
                        var bg = Enumerable.Repeat(new Color32(20, 24, 40, 255), side.width * side.height).ToArray(); side.SetPixels32(bg);
                        side.SetPixels32(0, 0, w, h, Flat(a)); side.SetPixels32(w + 4, 0, w, h, Flat(b)); side.Apply();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dump, $"{k.kind}_{art}.png"), side.EncodeToPNG());
                        Destroy(side);
                    }
                    Destroy(live); Destroy(got);
                    if (n % 6 == 0) { Resources.UnloadUnusedAssets(); yield return null; }
                }
                double all = px > 0 ? 10 * System.Math.Log10(255.0 * 255.0 / (sumSq / px)) : 0;
                Debug.Log($"[SnapDiff] {k.kind} 사도 {n} · 없음 {miss} · 평균 차이 {(px > 0 ? sumAbs / px : 0):F2}/255 · PSNR {all:F1}dB · 99% 차이가 가장 큰 사도 {worst}({worstD})\n" + per);
                yield return null;
            }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        // 곧은 알파 → 남색 바탕에 얹은 색(눈으로 보기용)
        static Color32[] Flat(Color32[] src)
        {
            var o = new Color32[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                float a = src[i].a / 255f;
                o[i] = new Color32((byte)(src[i].r * a + 20 * (1 - a)), (byte)(src[i].g * a + 24 * (1 - a)), (byte)(src[i].b * a + 40 * (1 - a)), 255);
            }
            return o;
        }
    }
}
