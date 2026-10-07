using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bolzena
{
    // 셰이더 데우기 — 쓰는 재질만, 여러 프레임에 나눠(2026-10-07 「처음 모험 진입 때 응답 없음」).
    //   예전엔 전투를 열 때 Shader.WarmupAllShaders() 를 한 프레임에 불렀다 — 읽힌 셰이더의 모든 변형(66개 · 406 조합)을 만들어
    //   웹(헤드리스 크롬 · RTX 4080)에서 8.8초 동안 메인 스레드가 막혔다(브라우저 「응답 없음」 · 검은 화면). 폰은 몇 배.
    //   이제는 지금 읽혀 있는 재질(사도 · 적 스파인 · 이펙트 · 글꼴 · 스프라이트)과 코드가 그 자리에서 만드는 재질의 셰이더를
    //   (셰이더 · 키워드) 하나에 한 번씩 4×4 렌더 텍스처에 그려 데운다 — 실제로 쓰는 변형만 만들어지고, 한 프레임 예산(budgetMs)을 넘으면 다음 프레임으로.
    //   한 번 데운 변형은 프로세스 동안 기억한다(두 번째 전투부터는 거의 할 일이 없다).
    public static class ShaderWarm
    {
        // 코드가 Shader.Find · Res.NewMat 으로 그 자리에서 만드는 재질(전투 중 처음 쓰는 순간에 컴파일되던 것) — 이름 · 켜는 키워드
        static readonly (string shader, string[] kw)[] Named =
        {
            ("Bolzena/Sprite", null), ("Bolzena/FocusLines", null), ("Bolzena/Rays", null), ("Bolzena/Ring", null), ("Bolzena/FxParticle", null),
            ("Spine/Skeleton", null), ("Spine/Skeleton Fill", null), ("Spine/SkeletonGraphic", null), ("Spine/SkeletonGraphic", new[] { "_STRAIGHT_ALPHA_INPUT" }),
            ("Sprites/Default", null), ("UI/Default", null),
            ("TextMeshPro/Distance Field", null), ("TextMeshPro/Distance Field", new[] { "OUTLINE_ON" }), ("TextMeshPro/Distance Field", new[] { "UNDERLAY_ON" }),
            ("TextMeshPro/Sprite", null),
        };

        static readonly HashSet<string> done = new HashSet<string>();
        static Mesh quad;
        static readonly List<Material> temp = new List<Material>();

        /// <summary>데운 변형 수(점검용).</summary>
        public static int Warmed => done.Count;

        static string Key(Material m)
        {
            var kw = m.shaderKeywords;
            if (kw == null || kw.Length == 0) return m.shader.name;
            Array.Sort(kw, StringComparer.Ordinal);
            return m.shader.name + "|" + string.Join(" ", kw);
        }

        static bool Skip(Shader s) => s == null || !s.isSupported || s.name.StartsWith("Hidden/") || s.name.StartsWith("Legacy Shaders/") || s.name == "Standard";

        /// <summary>아직 데우지 않은 (셰이더 · 키워드) 마다 재질 하나 — 읽혀 있는 재질 + 코드가 만드는 재질.</summary>
        public static List<Material> Pending()
        {
            var o = new List<Material>();
            var seen = new HashSet<string>();
            void Add(Material m)
            {
                if (m == null || Skip(m.shader)) return;
                var k = Key(m);
                if (done.Contains(k) || !seen.Add(k)) return;
                o.Add(m);
            }
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>()) Add(m);
            foreach (var (name, kw) in Named)
            {
                var sh = Shader.Find(name);
                if (Skip(sh)) continue;
                var m = new Material(sh) { name = "warm " + name, hideFlags = HideFlags.DontSave };
                if (kw != null) foreach (var k in kw) m.EnableKeyword(k);
                temp.Add(m);
                Add(m);
            }
            return o;
        }

        /// <summary>재질들을 한 프레임에 budgetMs 까지 그려 데운다(적어도 하나씩). progress(0~1) 로 알린다.</summary>
        public static IEnumerator Run(List<Material> mats, double budgetMs, Action<float> progress = null)
        {
            if (mats == null || mats.Count == 0) { Clean(); yield break; }
            if (quad == null)
            {
                quad = new Mesh { name = "shader-warm" };
                quad.SetVertices(new List<Vector3> { new Vector3(-1, -1), new Vector3(1, -1), new Vector3(1, 1), new Vector3(-1, 1) });
                quad.SetUVs(0, new List<Vector2> { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                quad.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
                quad.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
                quad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            }
            var sw = new Stopwatch();
            int i = 0;
            while (i < mats.Count)
            {
                sw.Restart();
                var rt = RenderTexture.GetTemporary(4, 4, 16, RenderTextureFormat.ARGB32);
                do
                {
                    var m = mats[i++];
                    if (m == null || m.shader == null) continue;
                    var cb = new CommandBuffer { name = "shader warm" };
                    cb.SetRenderTarget(rt);
                    cb.ClearRenderTarget(true, true, Color.clear);
                    cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    // 패스마다 — 그리기 패스(UniversalForward · SRPDefaultUnlit · 이름 없음)만. 그림자 · 깊이 패스는 이 화면에서 안 쓴다
                    for (int p = 0; p < m.passCount; p++)
                    {
                        var lm = m.shader.FindPassTagValue(p, new ShaderTagId("LightMode")).name;
                        if (lm == "ShadowCaster" || lm == "DepthOnly" || lm == "DepthNormals" || lm == "DepthNormalsOnly" || lm == "Meta" || lm == "MotionVectors") continue;
                        cb.DrawMesh(quad, Matrix4x4.identity, m, 0, p);
                    }
                    try { Graphics.ExecuteCommandBuffer(cb); }
                    catch (Exception e) { UnityEngine.Debug.LogWarning("[Warm] " + m.shader.name + " — " + e.Message); }
                    cb.Release();
                    done.Add(Key(m));
                } while (i < mats.Count && sw.Elapsed.TotalMilliseconds < budgetMs);
                RenderTexture.ReleaseTemporary(rt);
                progress?.Invoke((float)i / mats.Count);
                yield return null;
            }
            Clean();
        }

        static void Clean()
        {
            foreach (var m in temp) if (m != null) UnityEngine.Object.Destroy(m);
            temp.Clear();
        }
    }
}
