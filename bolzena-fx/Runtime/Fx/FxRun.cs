using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 이펙트 한 번 틀기의 옵션 — 웹판 playFx(name, o) 의 o 와 같은 뜻. 자리는 월드 좌표, 그 유닛의 발밑
    public class FxPlayOptions
    {
        public Vector3 At;
        public float Scale = 1f;
        public bool Flip;                  // 왼쪽을 본다(대상이 왼쪽)
        public float Until = float.PositiveInfinity;   // 이 초까지 하고 FADE 동안 걷힌다
        public bool Center;                // 뿌리에서 멀리 짜인 이펙트를 At 에 가운데 맞춘다
        public float? Top;                 // 이펙트가 넘지 않을 위끝(월드 y)
        public Func<Vector3?> Track;       // 매 프레임 그 자리에 붙는다(총구 본)
        public bool Bare;                  // 구운 판의 TUNE dx · dy 를 안 쓴다(본에 붙일 때)
        public Vector3? MoveTo;            // 투사체 — At 에서 MoveTo 로 MoveDur 동안 날아간다
        public float MoveDur = 0.35f;
        public int Order = 300;            // 그리는 차례(sortingOrder)
        public string SortingLayer;
        public Transform Parent;
        public int[] Only;                 // 이미터 몇 개만(살펴볼 때)
        public Action<FxRun> Done;
        // 전체 공격 — 이펙트가 덮을 가로 폭(월드, 적 무리의 폭). 원작 이펙트가 대상 하나 기준으로 짜여 그보다 좁으면
        // 입자 자리를 가운데(At)에서 가로로 벌린다(입자 모양은 그대로, 최대 MaxSpread 배). 구운 낱장은 가로로 살짝 늘인다(최대 1.3 배). 0 이면 그대로
        public float SpreadWidth;
        public float MaxSpread = 2f;
    }

    // 원작 이펙트 한 번 — 웹판 js/fx-burst.js 의 run 을 그대로 옮겼다(파티클 흉내 · 구운 낱장).
    // 메시 하나에 입자를 사각형으로 쌓아 그리고(텍스처가 바뀌면 서브메시), 재질은 텍스처마다 하나(Bolzena/FxParticle — 미리 곱한 알파, 더하기는 알파 0).
    // 색은 HDR 그대로 넘겨 블룸이 먹는다(웹판은 캔버스라 1 에서 잘렸다)
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FxRun : MonoBehaviour
    {
        public const float FADE = 0.4f;
        const float MAX_DT = 1f / 20f;
        const float EDGE_OFF = 0.5f;
        public static float EDGE = 0.05f, SHEET_EDGE = 0.22f;

        public string Name;
        public bool IsDone { get; private set; }
        // 세대 — 무대는 풀에서 되쓰인다. 쥐고 있던 FxRun 의 Gen 이 바뀌었으면 그 재생은 이미 끝난 것
        public int Gen { get; private set; }
        static int gens;
        public float T => t;
        public Vector3 OriginWorld => transform.TransformPoint(origin);   // 지금 터지는 자리(월드) — 점검
        public float Duration => sheet != null ? sheet.Frames / Mathf.Max(1, sheet.Fps) : (fx != null ? fx.Dur : 0);

        FxEffect fx;
        FxSheet sheet;
        FxPlayOptions o;
        float t, k, flip, dx, dy, ys = 1, sx = 1, fade = 1, spread = 1;
        Vector3 origin, from, to;
        bool moving;
        Em[] ems;
        Mesh mesh;
        MeshRenderer mr;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Vector4> uv1 = new List<Vector4>();
        readonly List<Color> cols = new List<Color>();
        readonly List<List<int>> subs = new List<List<int>>();
        readonly List<Material> mats = new List<Material>();
        Material[] matArr = new Material[0];
        int matCount = -1;

        class Burst { public float T, N, Iv; public int Left; }
        class P
        {
            public float X, Y, Vx, Vy, Lvx, Lvy, Age, Life, Size, Rot, Spin;
            public Color C;
            public int Row, F0, Sf;
            public bool Fu;
        }
        class Em
        {
            public FxEmitter E;
            public float T0, Acc;
            public Burst[] Bursts;
            public readonly List<P> Parts = new List<P>();
        }

        static readonly Stack<P> pool = new Stack<P>();       // 입자 되쓰기(가비지 없이)
        public static int Live;                       // 모든 무대의 입자 수
        public static int Cap = 1500;
        // 겹침 상한(화면 넓이 몇 장 — 큰 입자의 넓이 × 알파 합). 0 이면 끄기. ScreenArea 는 월드 넓이(카메라에서 잰다)
        public static float OverdrawBudget = 12f;
        public static int Culled;
        static int frameStamp = -1;
        static float frameArea, screenArea = 144;
        static float ScreenArea { get { var c = Camera.main; return c != null && c.orthographic ? 4 * c.orthographicSize * c.orthographicSize * c.aspect : 16 * 9; } }                 // 화면에 동시에 뜨는 입자 수(웹판 600 — 유니티는 넉넉히)

        // ── 만들기 ──
        // 다 돈 무대는 꺼 두었다가 되쓴다(게임오브젝트 · 메시 · 재질 배열) — 고학년 한 벌이 이펙트 수십 개를 틀어도 만들기 · 부수기가 없다
        static readonly Stack<FxRun> idle = new Stack<FxRun>();
        public static int PoolMax = 64;
        public static int Pooled => idle.Count;

        internal static FxRun Create(string name, FxEffect fx, FxSheet sheet, FxPlayOptions o)
        {
            FxRun r = null;
            while (idle.Count > 0 && r == null) r = idle.Pop();   // 장면이 바뀌며 부서진 것은 건너뛴다
            GameObject go;
            if (r != null)
            {
                go = r.gameObject;
                go.name = "fx:" + name;
                go.transform.SetParent(o.Parent ? o.Parent : null, false);
                r.ResetState();
                go.SetActive(true);
            }
            else
            {
                go = new GameObject("fx:" + name);
                if (o.Parent) go.transform.SetParent(o.Parent, false);
                r = go.AddComponent<FxRun>();
                r.mesh = new Mesh();
                r.mesh.MarkDynamic();
                go.GetComponent<MeshFilter>().sharedMesh = r.mesh;
            }
            r.Name = name;
            r.Gen = ++gens;
            r.fx = fx;
            r.sheet = sheet;
            r.o = o;
            r.mesh.name = name;
            r.mr = go.GetComponent<MeshRenderer>();
            r.mr.sortingOrder = o.Order;
            if (!string.IsNullOrEmpty(o.SortingLayer)) r.mr.sortingLayerName = o.SortingLayer;
            else r.mr.sortingLayerID = 0;
            r.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.mr.receiveShadows = false;
            r.flip = o.Flip ? -1 : 1;
            r.origin = o.At;
            if (o.MoveTo.HasValue) { r.moving = true; r.from = o.At; r.to = o.MoveTo.Value; }
            if (sheet != null) r.InitSheet(); else r.InitFx();
            r.UpdateOrigin();
            r.Build();
            return r;
        }

        // 원작 셰이더 그래프([SG])로만 뜻이 있는 텍스처 — 우리 판(알파 · 가산)으로 그리면 색 검사판이 거대한 자홍 사각형으로 화면을 덮는다(키샤 강공 · 저학년, 카드 몸짓 대조 2026-10-06)
        //   가로 그라데이션 · 격자 · 결정 무늬도 같은 꼴(극좌표 · 마스크 셰이더로만 둥글게 보인다) — 레비(졸업) 갈색 네모 · 네르(빡침) 파란 판 · 마카샤 회색 네모.
        //   목록은 card_fix/edges.py(카드 장 이미터 가운데 텍스처 가장자리 알파가 100 넘는 [SG] 판)로 뽑았다
        static readonly string[] SHADER_ONLY_TEX = { "Colorchek", "Canta_Graygra_lum", "NerRage_Gra_lum", "Crystal_Diamond_lum", "Water_Tile_01_lum", "Iceground_lum", "FX_In_Grid", "Kishya_Attack_2_Back_1" };
        static bool ShaderOnly(FxEmitter e)
        {
            if (e.TexPath == null) return false;
            foreach (var s in SHADER_ONLY_TEX) if (e.TexPath.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        void InitFx()
        {
            k = BolzenaFx.UnitWorld * o.Scale;
            // 대상 쪽 이펙트가 뿌리에서 멀리 짜여 있으면 가로 가운데를 대상에 맞춘다
            if (o.Center)
            {
                var xs = new List<float>();
                foreach (var e in fx.Em) if (!e.M3d) xs.Add(e.Pos.x);
                xs.Sort();
                float m = xs.Count > 0 ? xs[xs.Count >> 1] : 0;
                if (Mathf.Abs(m) > 6) dx = -m;
            }
            // 가장 높이 짜인 이미터가 Top 위로 나가지 않게 이미터 높이만 줄인다
            if (o.Top.HasValue)
            {
                float hi = 0;
                foreach (var e in fx.Em) if (!e.M3d) hi = Mathf.Max(hi, e.Pos.y);
                float room = o.Top.Value - o.At.y;
                if (hi * k > room && room > 0) ys = room / (hi * k);
            }
            var list = new List<Em>();
            // 입체 메시 이미터만 있는 이펙트(디아나 shockwave · 로니 skill_1_1 · 루포 wind — 51장)는 다 빼면 아무것도 안 보인다 —
            // 그때만 메시 판을 사각형 한 장으로 옅게(0.6) 그린다(웹판 추출 주석 「옅게 그린다」). 2D 이미터가 하나라도 있으면 전처럼 뺀다
            bool only3d = true;
            foreach (var e0 in fx.Em) if (!e0.M3d && e0.Tex != null) { only3d = false; break; }
            for (int i = 0; i < fx.Em.Count; i++)
            {
                var e = fx.Em[i];
                if ((e.M3d && !only3d) || e.Tex == null) continue;
                if (ShaderOnly(e) && fx.name.IndexOf("ultimate", StringComparison.Ordinal) < 0) continue;   // 카드 장만(고학년 장은 이펙트 담당 판단에 둔다)
                if (o.Only != null && Array.IndexOf(o.Only, i) < 0) continue;
                var em = new Em { E = e, T0 = Rand(e.Delay) };
                int nb = e.Bursts == null ? 0 : e.Bursts.Length;
                em.Bursts = new Burst[nb];
                for (int b = 0; b < nb; b++)
                {
                    var v = e.Bursts[b];
                    em.Bursts[b] = new Burst { T = v.x, N = v.y, Left = v.z > 0 ? Mathf.RoundToInt(v.z) : 1, Iv = v.w > 0 ? v.w : 0.01f };
                }
                list.Add(em);
            }
            ems = list.ToArray();
            // 전체 공격 — 원작 이펙트의 가로 폭을 짐작해 무리 폭에 모자라면 벌린다
            if (o.SpreadWidth > 0 && ems.Length > 0)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var em in ems)
                {
                    var e = em.E;
                    float r = (e.HasShape ? e.Shape.R * Mathf.Abs(e.M[0]) : 0) + e.Size.y * 0.35f;
                    lo = Mathf.Min(lo, e.Pos.x + dx - r); hi = Mathf.Max(hi, e.Pos.x + dx + r);
                }
                float w = (hi - lo) * k;
                if (w > 1e-3f) spread = Mathf.Clamp(o.SpreadWidth / w, 1f, Mathf.Max(1f, o.MaxSpread));
            }
        }

        void InitSheet()
        {
            var b = sheet;
            ems = new Em[0];
            k = BolzenaFx.UnitWorld * o.Scale * b.Scale;
            dy = o.Bare ? 0 : b.Dy;
            if (o.Top.HasValue)
            {
                float hi = b.Anchor.y / b.Ppu + dy, room = o.Top.Value - o.At.y;
                if (hi * k > room && room > 0) k = room / hi;
            }
            dx = o.Bare ? 0 : b.Dx;
            if (o.Center && Mathf.Abs(b.Cx) > 6) dx -= b.Cx;
            if (b.Reach)
            {
                var cam = Camera.main;
                float left = -1e4f, right = 1e4f;
                if (cam != null && cam.orthographic)
                {
                    float hw = cam.orthographicSize * cam.aspect;
                    left = cam.transform.position.x - hw; right = cam.transform.position.x + hw;
                }
                float lx = o.At.x + flip * dx * k, have = (b.W - b.Anchor.x) * k / b.Ppu;
                float need = (o.Flip ? lx - left : right - lx) + 0.4f;
                if (have > 0 && need > have) sx = need / have;
            }
        }

        public void Stop(float fadeSec = FADE) { o.Until = Mathf.Min(o.Until, t + Mathf.Max(0, fadeSec)); }

        void OnDestroy()
        {
            if (ems != null) foreach (var em in ems) { Live -= em.Parts.Count; foreach (var q in em.Parts) pool.Push(q); em.Parts.Clear(); }
            if (mesh != null) Destroy(mesh);
        }

        // 재기 — 이 프레임에 모든 이펙트가 쓴 CPU 시간(ms). 시험 · 프로파일용
        public static double CpuMsThisFrame;
        static int cpuStamp = -1;
        static readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        void Update()
        {
            if (IsDone) return;
            if (cpuStamp != Time.frameCount) { cpuStamp = Time.frameCount; CpuMsThisFrame = 0; }
            sw.Restart();
            Tick();
            CpuMsThisFrame += sw.Elapsed.TotalMilliseconds;
        }

        void Tick()
        {
            float dt = Mathf.Min(MAX_DT, Mathf.Max(0, BolzenaFx.DeltaTime())) * BolzenaFx.Rate;
            fade = Mathf.Clamp01((o.Until - t) / FADE);
            bool busy = Step(dt);
            if (!(busy && t < Duration + 1.5f && t < o.Until))
            {
                Finish();
                return;
            }
            UpdateOrigin();
            Build();
        }

        void Finish()
        {
            IsDone = true;
            foreach (var em in ems) { Live -= em.Parts.Count; foreach (var q in em.Parts) pool.Push(q); em.Parts.Clear(); }
            try { o.Done?.Invoke(this); } catch (Exception ex) { Debug.LogException(ex); }
            if (idle.Count >= PoolMax) { Destroy(gameObject); return; }
            mesh.Clear();
            gameObject.SetActive(false);
            if (o.Parent) transform.SetParent(null, false);   // 부모가 부서져도 남게
            idle.Push(this);
        }

        void ResetState()
        {
            IsDone = false;
            t = 0; k = 0; dx = dy = 0; ys = sx = fade = spread = 1;
            moving = false; ems = null; matCount = -1;
            mesh.Clear();
        }

        /// <summary>장면을 닫을 때 — 쉬는 무대 목록을 비운다(장면과 함께 부서진 것이 이펙트 · 옵션 람다를 쥐고 남지 않게).</summary>
        public static void ClearPool() => idle.Clear();

        // 지금 도는 것을 모두 걷는다(장면을 닫을 때 · 시험). fadeSec 0 이면 다음 프레임에 걷힌다
        public static void StopAll(float fadeSec = 0)
        {
            foreach (var r in FindObjectsByType<FxRun>(FindObjectsSortMode.None)) if (!r.IsDone) r.Stop(fadeSec);
        }

        void UpdateOrigin()
        {
            if (o.Track != null) { var p = o.Track(); if (p.HasValue) origin = p.Value; }
            else if (moving)
            {
                float f = Mathf.Min(1, t / (o.MoveDur > 0 ? o.MoveDur : 0.35f));
                float e = f * f * (3 - 2 * f);
                origin = Vector3.LerpUnclamped(from, to, e);
            }
        }

        // ── 셈 ──
        static float Rand(Vector2 r) => r.x + UnityEngine.Random.value * (r.y - r.x);

        static float KeyAt(Vector2[] k, float t)
        {
            if (k == null || k.Length == 0) return 1;
            if (t <= k[0].x) return k[0].y;
            for (int i = 1; i < k.Length; i++)
                if (t <= k[i].x) { var a = k[i - 1]; var b = k[i]; float d = b.x - a.x; float f = (t - a.x) / (d != 0 ? d : 1); return a.y + (b.y - a.y) * f; }
            return k[k.Length - 1].y;
        }

        static Vector3 ColAt(Vector4[] k, float t)
        {
            if (k == null || k.Length == 0) return Vector3.one;
            Vector4 a = k[0], b = k[0];
            if (t > k[0].x)
            {
                b = k[k.Length - 1]; a = b;
                for (int i = 1; i < k.Length; i++) if (t <= k[i].x) { a = k[i - 1]; b = k[i]; break; }
            }
            float d = b.x - a.x;
            float f = a.Equals(b) ? 0 : (t - a.x) / (d != 0 ? d : 1);
            return new Vector3(a.y + (b.y - a.y) * f, a.z + (b.z - a.z) * f, a.w + (b.w - a.w) * f);
        }

        // 모양: 방출 공간의 (자리, 방향) — 유니티 기본은 +Z 로 쏜다
        static void ShapePoint(FxEmitter e, out Vector3 p, out Vector3 d)
        {
            if (!e.HasShape) { p = Vector3.zero; d = new Vector3(0, 0, 1); return; }
            var sh = e.Shape;
            float r = sh.R, th = sh.T;
            float rr = r * (1 - th * UnityEngine.Random.value);
            float arc = (sh.Arc > 0 ? sh.Arc : 360) * Mathf.Deg2Rad;
            switch (sh.K)
            {
                case FxShapeKind.Sphere:
                case FxShapeKind.Hemi:
                {
                    float u = UnityEngine.Random.value * 2 - 1, a = UnityEngine.Random.value * Mathf.PI * 2, q = Mathf.Sqrt(1 - u * u);
                    d = new Vector3(q * Mathf.Cos(a), q * Mathf.Sin(a), sh.K == FxShapeKind.Hemi ? Mathf.Abs(u) : u);
                    p = d * rr; break;
                }
                case FxShapeKind.Cone:
                {
                    float a = UnityEngine.Random.value * arc, f = th != 0 ? Mathf.Sqrt(UnityEngine.Random.value) * th + (1 - th) : 1;
                    float cx = Mathf.Cos(a), cy = Mathf.Sin(a), ang = sh.A * Mathf.Deg2Rad * f;
                    p = new Vector3(cx * r * f, cy * r * f, 0);
                    d = new Vector3(cx * Mathf.Sin(ang), cy * Mathf.Sin(ang), Mathf.Cos(ang)); break;
                }
                case FxShapeKind.Circle:
                case FxShapeKind.Donut:
                {
                    float a = UnityEngine.Random.value * arc, cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                    p = new Vector3(cx * rr, cy * rr, 0); d = new Vector3(cx, cy, 0); break;
                }
                case FxShapeKind.Edge: p = new Vector3((UnityEngine.Random.value * 2 - 1) * r, 0, 0); d = new Vector3(0, 1, 0); break;
                case FxShapeKind.Box:
                case FxShapeKind.Rect:
                    p = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, sh.K == FxShapeKind.Box ? UnityEngine.Random.value - 0.5f : 0);
                    d = new Vector3(0, 0, 1); break;
                default: p = Vector3.zero; d = new Vector3(0, 0, 1); break;
            }
            if (sh.Rnd != 0 || sh.Sph != 0)
            {
                Vector3 rd;
                if (sh.Sph != 0) { float l = p.magnitude; rd = l > 0 ? p / l : d; }
                else { float u = UnityEngine.Random.value * 2 - 1, a = UnityEngine.Random.value * Mathf.PI * 2, q = Mathf.Sqrt(1 - u * u); rd = new Vector3(q * Mathf.Cos(a), q * Mathf.Sin(a), u); }
                float kk = sh.Rnd != 0 ? sh.Rnd : sh.Sph;
                d = d * (1 - kk) + rd * kk;
            }
        }

        static Vector2 Proj(float[] m, Vector3 v) => new Vector2(m[0] * v.x + m[1] * v.y + m[2] * v.z, m[3] * v.x + m[4] * v.y + m[5] * v.z);
        static float EmitWindow(FxEmitter e) { float w = e.Loop ? Mathf.Min(e.Dur, 1.5f) : e.Dur; return w > 0 ? w : 0.01f; }
        static float AvgSc(FxEmitter e) { if (!e.HasSc) return 1; float a = (Mathf.Abs(e.Sc.x) + Mathf.Abs(e.Sc.y)) / 2; return a != 0 ? a : 1; }

        void Spawn(Em em, int count)
        {
            var e = em.E;
            for (int i = 0; i < count; i++)
            {
                if (Live >= Cap || em.Parts.Count >= (e.Max > 0 ? e.Max : 1000)) return;
                ShapePoint(e, out var sp, out var sd);
                float spd = Rand(e.Speed);
                var P = Proj(e.M, sp);
                var V = Proj(e.M, sd * spd);
                var p = pool.Count > 0 ? pool.Pop() : new P();
                p.X = e.Pos.x + P.x; p.Y = e.Pos.y * ys + P.y; p.Vx = V.x; p.Vy = V.y; p.Lvx = V.x; p.Lvy = V.y;
                p.Age = 0; p.Life = Mathf.Max(0.02f, Rand(e.Life)); p.Size = Rand(e.Size) * AvgSc(e);
                p.Rot = Rand(e.Rot); p.Spin = Rand(e.Spin); p.Fu = e.FlipU > 0 && UnityEngine.Random.value < e.FlipU;
                p.Row = 0; p.F0 = 0; p.Sf = 0;
                var cs = e.Col;
                if (cs == null || cs.Length == 0) p.C = Color.white;
                else if (e.ColLerp && cs.Length == 2) p.C = Color.LerpUnclamped(cs[0], cs[1], UnityEngine.Random.value);
                else p.C = cs[Mathf.Min(cs.Length - 1, (int)(UnityEngine.Random.value * cs.Length))];
                p.C = new Color(p.C.r * e.Tint.r, p.C.g * e.Tint.g, p.C.b * e.Tint.b, p.C.a * e.Tint.a);
                if (e.Radial != 0) { float l = P.magnitude; if (l == 0) l = 1; p.Vx += P.x / l * e.Radial; p.Vy += P.y / l * e.Radial; }
                if (e.HasSheet)
                {
                    var a = e.Sheet;
                    int F = a.Row != 0 ? a.Tx : a.Tx * a.Ty;
                    if (a.Row == 1) p.Row = (int)(UnityEngine.Random.value * a.Ty); else if (a.Row >= 2) p.Row = a.Row - 2;
                    if (a.HasF) p.F0 = Mathf.FloorToInt(Rand(a.F) * F);
                    if (a.HasSf) p.Sf = Mathf.FloorToInt(a.Sf.y <= 1 ? Rand(a.Sf) * F : Rand(a.Sf));
                }
                em.Parts.Add(p); Live++;
            }
        }

        bool Step(float dt)
        {
            t += dt;
            if (sheet != null) return t < Duration;
            bool busy = false;
            foreach (var em in ems)
            {
                var e = em.E;
                float te = t - em.T0;
                if (te < 0) { busy = true; continue; }
                if (e.Rate > 0 && te <= EmitWindow(e))
                {
                    busy = true;
                    em.Acc += e.Rate * dt;
                    int n = Mathf.FloorToInt(em.Acc);
                    em.Acc -= n;
                    if (n > 0) Spawn(em, n);
                }
                foreach (var b in em.Bursts)
                {
                    while (b.Left > 0 && te >= b.T)
                    {
                        int n = Mathf.FloorToInt(b.N) + (UnityEngine.Random.value < b.N % 1 ? 1 : 0);
                        Spawn(em, n);
                        b.Left--; b.T += b.Iv;
                    }
                    if (b.Left > 0) busy = true;
                }
                var ps = em.Parts;
                for (int i = ps.Count - 1; i >= 0; i--)
                {
                    var p = ps[i];
                    p.Age += dt;
                    if (p.Age >= p.Life) { pool.Push(p); ps[i] = ps[ps.Count - 1]; ps.RemoveAt(ps.Count - 1); Live--; continue; }
                    Move(e, p, dt);
                }
                if (ps.Count > 0) busy = true;
            }
            return busy;
        }

        static void Move(FxEmitter e, P p, float dt)
        {
            if (e.Grav != 0) p.Vy -= e.Grav * dt;
            if (e.HasForce)
            {
                var F = e.ForceW ? new Vector2(e.Force.x, e.Force.y) : Proj(e.HasMt ? e.Mt : e.M, e.Force);
                p.Vx += F.x * dt; p.Vy += F.y * dt;
            }
            if (e.Drag != 0) { float d = Mathf.Exp(-e.Drag * dt); p.Vx *= d; p.Vy *= d; }
            // 수명 속도는 매 프레임 더해지는 몫이라 쌓지 않는다. 속도 제한은 둘을 합친 것에 건다
            var V = e.HasVel ? (e.VelW ? new Vector2(e.Vel.x, e.Vel.y) : Proj(e.HasMt ? e.Mt : e.M, e.Vel)) : Vector2.zero;
            float vx = p.Vx + V.x, vy = p.Vy + V.y;
            if (e.HasLimit)
            {
                float lim = e.Limit * AvgSc(e), v = Mathf.Sqrt(vx * vx + vy * vy);
                if (v > lim)
                {
                    float r = (lim + (v - lim) * Mathf.Pow(1 - e.Damp, dt * 60)) / v;
                    vx *= r; vy *= r; p.Vx = vx - V.x; p.Vy = vy - V.y;
                }
            }
            if (e.SpdMul != 1) { vx *= e.SpdMul; vy *= e.SpdMul; }
            p.X += vx * dt; p.Y += vy * dt;
            p.Lvx = vx; p.Lvy = vy;
            p.Rot += p.Spin * dt;
        }

        // ── 그리기 ──
        static bool linear;
        static Color Lin(float r, float g, float b, float a)
        {
            if (!linear) return new Color(r, g, b, a);
            return new Color(L(r), L(g), L(b), a);
        }
        static float L(float c) => c <= 0 ? 0 : c <= 1 ? Mathf.GammaToLinearSpace(c) : c;

        int curSub = -1;
        Texture curTex;

        void BeginBuild()
        {
            verts.Clear(); uvs.Clear(); uv1.Clear(); cols.Clear();
            foreach (var s in subs) s.Clear();
            mats.Clear();
            curSub = -1; curTex = null;
            linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        }

        void UseTex(Texture tex)
        {
            if (curSub >= 0 && tex == curTex) return;
            curSub++;
            curTex = tex;
            if (subs.Count <= curSub) subs.Add(new List<int>());
            mats.Add(BolzenaFx.MaterialFor(tex));
        }

        // 화면식(y 아래) 꼭짓점 → 월드
        Vector3 W(float sxv, float syv) => origin + new Vector3(sxv * k, -syv * k, 0);

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0, float v0, float u1, float v1, Color col, float add, bool edge, float lx0 = -1, float lx1 = -1)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            // 웹 그림은 윗줄이 v0 — 유니티 텍스처는 아래가 0 이라 뒤집는다
            uvs.Add(new Vector2(u0, 1 - v0)); uvs.Add(new Vector2(u1, 1 - v0)); uvs.Add(new Vector2(u1, 1 - v1)); uvs.Add(new Vector2(u0, 1 - v1));
            // uv1 = (칸 안의 자리 x, y, 더하기, 걷어 낼 가장자리 폭)
            if (edge) { uv1.Add(new Vector4(0, 0, add, EDGE)); uv1.Add(new Vector4(1, 0, add, EDGE)); uv1.Add(new Vector4(1, 1, add, EDGE)); uv1.Add(new Vector4(0, 1, add, EDGE)); }
            else if (lx0 >= 0)
            {
                // 구운 칸 — 위아래(와 늘인 끝)를 넉넉히 걷는다. 칸 끝까지 찬 레이저 · 불빛이 네모로 잘려 보이지 않게
                uv1.Add(new Vector4(lx0, 0, add, SHEET_EDGE)); uv1.Add(new Vector4(lx1, 0, add, SHEET_EDGE)); uv1.Add(new Vector4(lx1, 1, add, SHEET_EDGE)); uv1.Add(new Vector4(lx0, 1, add, SHEET_EDGE));
            }
            else for (int j = 0; j < 4; j++) uv1.Add(new Vector4(EDGE_OFF, EDGE_OFF, add, EDGE));
            for (int j = 0; j < 4; j++) cols.Add(col);
            var s = subs[curSub];
            s.Add(i); s.Add(i + 1); s.Add(i + 2); s.Add(i); s.Add(i + 2); s.Add(i + 3);
        }

        void Build()
        {
            BeginBuild();
            if (sheet != null) BuildSheet(); else BuildFx();
            mesh.Clear();
            if (verts.Count == 0) { mr.enabled = false; return; }
            mr.enabled = true;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uv1);
            mesh.SetColors(cols);
            int n = curSub + 1;
            mesh.subMeshCount = n;
            for (int i = 0; i < n; i++) mesh.SetTriangles(subs[i], i, false);
            mesh.bounds = new Bounds(origin, new Vector3(1e4f, 1e4f, 10));
            if (matArr.Length != n) matArr = new Material[n];
            bool changed = false;
            for (int i = 0; i < n; i++) if (matArr[i] != mats[i]) { matArr[i] = mats[i]; changed = true; }
            if (changed || matCount != n) { mr.sharedMaterials = matArr; matCount = n; }
        }

        void BuildFx()
        {
            // 화면식 원점 이동(dx) — 웹판 runOrigin 의 sx
            float ox = flip * dx, oy = 0;
            foreach (var em in ems)
            {
                if (em.Parts.Count == 0) continue;
                var e = em.E;
                UseTex(e.Tex);
                float add = e.Add ? 1 : 0;
                bool edge = true;
                float f0 = fade;
                if (e.M3d) fade *= 0.6f;
                foreach (var p in em.Parts) Quad(e, p, ox, oy, add, edge);
                fade = f0;
            }
        }

        void Quad(FxEmitter e, P p, float ox, float oy, float add, bool edge)
        {
            float fl = flip, tt = p.Age / p.Life;
            float s = p.Size * KeyAt(e.SizeOL, tt) * e.SizeK;
            float cx = (ox + fl * p.X) * spread, cy = oy - p.Y;
            if (e.HasPivot) { cx += fl * e.Pivot.x * s; cy -= e.Pivot.y * s; }
            float ux, uy, vx, vy;
            if (e.HasQuad)
            {
                var q = e.Quad;
                float cr = Mathf.Cos(p.Rot), sr = Mathf.Sin(p.Rot);
                float ax = q.x * cr + q.z * sr, ay = q.y * cr + q.w * sr, bx = -q.x * sr + q.z * cr, by = -q.y * sr + q.w * cr;
                float sz = s / 2, sv = sz * e.Asp;
                ux = fl * ax * sz; uy = -ay * sz; vx = -fl * bx * sv; vy = by * sv;
            }
            else
            {
                float w = s, h = s * e.Sy * e.Asp;
                if (e.HasSc)
                {
                    float avg = (Mathf.Abs(e.Sc.x) + Mathf.Abs(e.Sc.y)) / 2; if (avg == 0) avg = 1;
                    w *= Mathf.Abs(e.Sc.x) / avg; h *= Mathf.Abs(e.Sc.y) / avg;
                }
                if (e.HasExt) { w *= e.Ext.x; h *= e.Ext.y; }
                if (e.Flat) h *= 0.35f;
                float ang = -p.Rot * fl;
                if (e.HasStretch)
                {
                    float vsx = fl * p.Lvx, vsy = -p.Lvy, v = Mathf.Sqrt(vsx * vsx + vsy * vsy);
                    float len = w * e.Stretch.x + v * e.Stretch.y;
                    ang = v > 1e-4f ? Mathf.Atan2(vsy, vsx) : 0;
                    h = w; w = Mathf.Max(len, w * 0.2f);
                }
                float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                ux = c * w / 2; uy = sn * w / 2; vx = -sn * h / 2; vy = c * h / 2;
                if (e.HasSc && e.Sc.x * fl < 0) { ux = -ux; uy = -uy; }
            }
            // 시트 칸
            float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
            if (e.HasSheet)
            {
                var a = e.Sheet;
                int F = a.Row != 0 ? a.Tx : a.Tx * a.Ty;
                int f;
                if (a.Fps > 0) f = Mathf.FloorToInt(p.Age * a.Fps);
                else if (a.Fk != null && a.Fk.Length > 0) f = Mathf.FloorToInt(KeyAt(a.Fk, (tt * (a.Cyc != 0 ? a.Cyc : 1)) % 1f) * F);
                else f = p.F0;
                f = ((f + p.Sf) % F + F) % F;
                int col = f % a.Tx, row = a.Row != 0 ? p.Row : f / a.Tx;
                u0 = (float)col / a.Tx; u1 = (float)(col + 1) / a.Tx; v0 = (float)row / a.Ty; v1 = (float)(row + 1) / a.Ty;
            }
            if (p.Fu) { float x = u0; u0 = u1; u1 = x; }
            var C = ColAt(e.ColOL, tt);
            float alpha = Mathf.Clamp01(p.C.a * KeyAt(e.AlphaOL, tt) * fade);
            if (alpha <= 0.003f) return;
            // 겹침(오버드로) 상한 — 한 프레임에 그리는 큰 입자의 넓이를 화면 몇 장으로 묶는다. 넘친 큰 입자는 이번 프레임에 빠진다(작은 불꽃은 늘 그린다)
            float area = Mathf.Abs(ux * vy - uy * vx) * 4 * k * k;
            if (frameStamp != Time.frameCount) { frameStamp = Time.frameCount; frameArea = 0; screenArea = ScreenArea; }
            if (OverdrawBudget > 0 && area > screenArea * 0.02f)
            {
                if (frameArea + area * alpha > OverdrawBudget * screenArea) { Culled++; return; }
                frameArea += area * alpha;
            }
            float boost = e.Add ? BolzenaFx.AddBoost : BolzenaFx.AlphaBoost;
            var col4 = Lin(p.C.r * C.x * boost, p.C.g * C.y * boost, p.C.b * C.z * boost, alpha);
            AddQuad(W(cx - ux - vx, cy - uy - vy), W(cx + ux - vx, cy + uy - vy), W(cx + ux + vx, cy + uy + vy), W(cx - ux + vx, cy - uy + vy),
                    u0, v0, u1, v1, col4, add, edge);
        }

        void BuildSheet()
        {
            var b = sheet;
            int f = Mathf.Min(b.Frames - 1, Mathf.FloorToInt(t * b.Fps)), pg = 0;
            float tt = (float)f / Mathf.Max(1, b.Frames - 1);
            float a = fade;
            if (b.FadeFrom >= 0 && tt > b.FadeFrom) a *= Mathf.Max(0, 1 - (tt - b.FadeFrom) / (1 - b.FadeFrom));
            if (a <= 0.003f || b.Pages == null || b.Pages.Length == 0) return;
            while (pg < b.Pages.Length - 1 && f >= b.PageFrames[pg]) f -= b.PageFrames[pg++];
            var tex = b.Pages[pg];
            if (tex == null) return;
            UseTex(tex);
            float fx0 = (f % b.Cols) * b.W, fy0 = (f / b.Cols) * b.H;
            // 화면식: 발밑 원점에서 시트 왼위까지. q = 원작 단위 / 시트 px
            float q = 1f / b.Ppu, fl = flip, ax = b.Anchor.x;
            float ox = fl * dx;
            float Y0 = -(b.Anchor.y + dy * b.Ppu) * q, Y1 = Y0 + b.H * q;
            float v0 = fy0 / tex.height, v1 = (fy0 + b.H) / tex.height;
            var col = new Color(1, 1, 1, a);
            if (o.SpreadWidth > 0 && !b.Reach)
            {
                float vis = b.W / b.Ppu * k * 0.6f;
                if (vis > 1e-3f) spread = Mathf.Clamp(o.SpreadWidth / vis, 1f, Mathf.Min(1.3f, o.MaxSpread));
                q *= spread;
            }
            if (sx == 1)
            {
                Span(0, b.W, ox - fl * ax * q, ox + fl * (b.W - ax) * q, 0.5f, 0.5f);
            }
            else
            {
                Span(0, ax, ox - fl * ax * q, ox, 0.5f, 0.5f);
                Span(ax, b.W, ox, ox + fl * (b.W - ax) * q * sx, 0.5f, 1f);
            }
            void Span(float x0, float x1, float X0, float X1, float l0, float l1)
            {
                float u0 = (fx0 + x0) / tex.width, u1 = (fx0 + x1) / tex.width;
                AddQuad(W(X0, Y0), W(X1, Y0), W(X1, Y1), W(X0, Y1), u0, v0, u1, v1, col, 0, false, l0, l1);
            }
        }
    }
}
