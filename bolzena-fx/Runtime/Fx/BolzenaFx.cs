using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 원작 이펙트 틀기 — 쓰는 쪽은 이것만 부르면 된다.
    //   BolzenaFx.Play("fx_erpin_ultimate_ground_1", new FxPlayOptions { At = feet, Flip = false });
    //   BolzenaFx.PlayUlt(this, "에르핀", new UltOptions { From = casterFeet, To = targetFeet, Impact = 1300 });
    // 구운 낱장(FxSheet)이 그 이름에 있으면 그것을, 없으면 파티클 판(FxEffect)을 튼다.
    public static partial class BolzenaFx
    {
        // 원작 1단위 → 월드 단위. 웹판은 1단위 = 16px 에 유닛 키 150px 쯤 — 유닛 키가 1.5 월드인 싸움터면 0.16
        // 원작 1 단위 = 스파인 100px(SkeletonData scale 0.01) — 우리 유닛은 스파인 × 0.01 × UnitScale 0.3 이라 원작 1 단위 = 월드 0.3.
        //   0.16(웹판에서 눈으로 맞춘 값)이면 모든 이펙트가 몸에 비해 0.53배였다(10-15 원작 대조: 티그 칼빛 타원이 몸의 1.6배 ↔ 원작 2.9배). 원작 그대로면 0.3
        public static float UnitWorld => OriginalOnly ? 0.3f : 0.16f;
        // 시간 — 전투 시계(히트스톱 · 배속)를 쓰려면 바꿔 끼운다. Rate 는 배속(웹판 speed.js 의 rate)
        public static Func<float> DeltaTime = () => Time.deltaTime;
        public static float Rate = 1f;
        // 움직임 줄이기 — 켜면 아무것도 안 튼다
        public static bool Calm;
        // 원작 그대로(2026-10-15 사용자 「임의로 넣거나 재해석한 것 다 빼고 원작대로」) — 켜면 우리가 덧댄 것(장별 크기 배율 · 휜 호 흉내 · 화면 위 줄이기 ·
        //   더하기 밝기 덧배 · 고학년 번쩍/흔들림 표 · 저학년 흰 구슬 · 직접 그린 레이저 줄기 · 연계 빛/움찔)을 끄고 원작 이펙트만. 장 고르기 · 자리 · 시각 맞춤은 남는다
        public static bool OriginalOnly = true;
        // 원작 ParticleSystem 그대로(유니티 기본 파티클로 다시 조립한 프리팹, 2026-10-16) — 끄면 우리 시뮬레이터(FxRun 판 쌓기)
        public static bool UsePs = true;
        // 원작 카메라 기울기(도) — 원작은 싸움터를 20° 내려다본다. 세계 정렬 판 · 메시가 같은 모양으로 보이게 프리팹 뿌리를 그만큼 돌린다
        public static float CamTilt = float.TryParse(System.Environment.GetEnvironmentVariable("BOLZENA_CAMTILT"), out var ct) ? ct : 20f;   // (10-17 3차: +20 으로 되돌림 — 「빙글빙글」은 걷힐 때 SetParticles 가 각속도를 붙인 탓이었다, FxRun.PsFade) 원작 카메라 기울기 크기 20° — 땅에 눕힌 원판(X 90°, 정렬 Local)이 원작처럼 납작한 타원(높이/폭 ≈ 0.3)으로 보인다.
        //   방향은 −20(10-17 사용자 「네르 · 디아나 임팩트가 돈다」): +20 이면 칼빛 메시(네르 초승달 · 티그 고리)가 화면 평면에서 빙글빙글 돌고, −20 이면 가로로 휩쓴다. 바닥 원판은 어느 쪽이든 같다
        //   (10-16 0° 로 바꿔 봤다가 되돌림: 티그 베기 고리는 원작에 가까워졌지만 바닥 원판이 모로 서서 사라졌다 — 우로스 · 이드 · 요미 바닥 원 빠짐, 10-17 원작 대조). BOLZENA_CAMTILT 로 바꿔 볼 수 있다
        // 더하기 · 알파 입자의 HDR 배율 — 1 이면 웹판 색 그대로. 블룸 문턱(1.15) 위로 올라간 몫만 번진다
        public static float AddBoost = 1.25f, AlphaBoost = 1f;
        public static string ShaderName = "Bolzena/FxParticle";
        // 화면 가운데(월드) — 화면 전체 이펙트(카메라 · 배경 낱말)가 여기. 싸움터 카메라가 Camera.main 이 아니면 바꿔 끼운다
        public static Func<Vector3> ScreenCenter = () => { var c = Camera.main; return c != null ? new Vector3(c.transform.position.x, c.transform.position.y, 0) : Vector3.zero; };
        // 이펙트 위끝(월드 y) — 높이 짜인 이펙트가 화면 꼭대기로 나가지 않게. 기본은 Camera.main 위끝 - FxRules.TOP_PAD
        public static Func<float?> TopY = () => { var c = Camera.main; return c != null && c.orthographic ? c.transform.position.y + c.orthographicSize - FxRules.TOP_PAD : (float?)null; };

        static Shader shader;
        static Material baseMat;
        static readonly Dictionary<Texture, Material> mats = new Dictionary<Texture, Material>();

        public static int MatKeep = 384;
        // 가리개 그림이 붙은 재질 — (그림, 가리개) 짝마다 하나(10-15)
        static readonly Dictionary<(Texture, Texture), Material> maskMats = new Dictionary<(Texture, Texture), Material>();
        public static Material MaterialFor(Texture tex, Texture mask)
        {
            if (mask == null) return MaterialFor(tex);
            if (maskMats.TryGetValue((tex, mask), out var mm) && mm != null) return mm;
            mm = new Material(MaterialFor(tex)) { name = "fx:" + (tex ? tex.name : "none") + "+" + mask.name };
            mm.SetTexture("_MaskTex", mask);
            if (maskMats.Count >= MatKeep) maskMats.Clear();
            maskMats[(tex, mask)] = mm;
            return mm;
        }

        public static Material MaterialFor(Texture tex)
        {
            if (tex != null && mats.TryGetValue(tex, out var m) && m != null) return m;
            // 빌드에 셰이더가 실리도록 FxImport 가 Resources/<Root>/FxParticle.mat 을 둔다 — 그것을 본으로
            if (baseMat == null) baseMat = Resources.Load<Material>(FxLibrary.Root + "/FxParticle");
            if (baseMat == null && shader == null) shader = Shader.Find(ShaderName);
            m = baseMat != null ? new Material(baseMat) : new Material(shader);
            m.name = "fx:" + (tex ? tex.name : "none");
            m.mainTexture = tex;
            if (mats.Count >= MatKeep) mats.Clear();   // 상한 — 사도가 바뀌며 텍스처마다 재질이 끝없이 쌓이지 않게(쓰는 것은 그대로 산다)
            if (tex != null) mats[tex] = m;
            return m;
        }

        // 한 이펙트를 튼다. 못 찾으면 null(조용히)
        public static FxRun Play(string name, FxPlayOptions o)
        {
            if (Calm || string.IsNullOrEmpty(name) || o == null) return null;
            var sh = FxLibrary.Sheet(name);
            var fx = sh == null ? FxLibrary.Effect(name) : null;
            if (sh == null && fx == null) return null;
            return FxRun.Create(name, fx, sh, o);
        }

        public static FxRun Play(string name, Vector3 at, float scale = 1f, bool flip = false, int order = 300)
            => Play(name, new FxPlayOptions { At = at, Scale = scale, Flip = flip, Order = order });

        // 고학년 한 벌 — UltFx 의 차례대로(코루틴). host 는 코루틴을 돌릴 MonoBehaviour
        public static Coroutine PlayUlt(MonoBehaviour host, string heroKey, UltOptions o)
        {
            if (Calm || host == null || !FxLibrary.HasUlt(heroKey)) return null;
            return host.StartCoroutine(UltFx.Run(heroKey, o));
        }

        // ── 미리 데우기 ── 첫 재생의 스파이크(자산 읽기 · 텍스처 올리기 · 셰이더 준비)를 미리 치른다.
        // 싸움을 열 때 파티 사도마다(또는 컷인이 도는 동안) 부른다. 읽은 텍스처 수를 돌려준다
        static Mesh warmQuad;
        public static int Prewarm(string heroKey) => Prewarm(FxLibrary.Get(heroKey));
        public static int Prewarm(IEnumerable<string> names)
        {
            var texs = new HashSet<Texture>();
            foreach (var n in names)
            {
                var sh = FxLibrary.Sheet(n);
                if (sh != null) { if (sh.Pages != null) foreach (var t in sh.Pages) if (t) texs.Add(t); continue; }
                var fx = FxLibrary.Effect(n);
                if (fx != null) foreach (var e in fx.Em) if (!e.M3d && e.Tex) texs.Add(e.Tex);
            }
            if (texs.Count == 0) return 0;
            if (warmQuad == null)
            {
                warmQuad = new Mesh { name = "fx-warm" };
                warmQuad.SetVertices(new List<Vector3> { new Vector3(-1, -1), new Vector3(1, -1), new Vector3(1, 1), new Vector3(-1, 1) });
                warmQuad.SetUVs(0, new List<Vector2> { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                warmQuad.SetUVs(1, new List<Vector4> { new Vector4(0, 0, 0, 0.05f), new Vector4(1, 0, 0, 0.05f), new Vector4(1, 1, 0, 0.05f), new Vector4(0, 1, 0, 0.05f) });
                warmQuad.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
                warmQuad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            }
            // 작은 렌더 텍스처에 한 번씩 그린다 — 텍스처가 GPU 로 올라가고 셰이더 · 블렌드 상태가 만들어진다
            var rt = RenderTexture.GetTemporary(4, 4, 0, RenderTextureFormat.ARGBHalf);
            var cb = new UnityEngine.Rendering.CommandBuffer { name = "fx prewarm" };
            cb.SetRenderTarget(rt);
            cb.ClearRenderTarget(false, true, Color.clear);
            cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            foreach (var t in texs) cb.DrawMesh(warmQuad, Matrix4x4.identity, MaterialFor(t));
            Graphics.ExecuteCommandBuffer(cb);
            cb.Release();
            RenderTexture.ReleaseTemporary(rt);
            return texs.Count;
        }
    }
}
