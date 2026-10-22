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
        public float Cut = float.PositiveInfinity;     // 이 초에 바로 걷는다(흐리지 않고 — 구운 낱장의 뒷칸을 안 보일 때)
        public bool Center;                // 뿌리에서 멀리 짜인 이펙트를 At 에 가운데 맞춘다
        public float? Top;                 // 이펙트가 넘지 않을 위끝(월드 y)
        public bool FitTop;                // 그린 입자 · 낱장이 Top 위로 나가면 그 판을 자리(At) 둘레로 줄이고(많아야 FIT_MIN 배), 그래도 넘으면 내린다(많아야 FIT_DOWN) — 카드 장(2026-10-13 아야 · 이드 · 요미 · 벨라 화면 위 잘림)
        public Func<Vector3?> Track;       // 매 프레임 그 자리에 붙는다(총구 본)
        public bool Bare;                  // 구운 판의 TUNE dx · dy 를 안 쓴다(본에 붙일 때)
        public Vector3? MoveTo;            // 투사체 — At 에서 MoveTo 로 MoveDur 동안 날아간다
        public float MoveDur = 0.35f;
        public bool CarryParticles;        // 이미 나온 입자도 뿌리와 같이 움직인다(시뮬레이션 공간 Local) — 통째로 미는 장(요미 고학년 달빛)
        public float MoveDelay;            // 이 초 동안 At 에 머문 뒤 움직이기 시작(요미 고학년 달빛 — 생기고 0.9초 뒤 적 쪽으로 쓸기, 10-17)
        public float MoveBack;             // > 0 이면 MoveTo 에 닿은 뒤 이 초 쉬었다가 같은 길이로 At 까지 돌아온다(아야 스킬 얼음 나비 왕복, 10-17)
        public float MoveBackDur;          // > 0 이면 돌아오는 데 이 초(없으면 MoveDur) — (10-18 2차) 아야 나비: 나갈 때 0.31초 거리 · 돌아올 때 0.6초 거리 속도가 다르다
        public bool MoveLinear;            // 고른 속도로(부드럽게 서고 출발하지 않는다) — 화면 밖까지 나갔다 오는 나비
        public float MoveArc;              // > 0 이면 날아가는 길을 이 높이(월드)의 포물선으로(비비(신성) 스킬 씨앗, fix_Y 8)
        public bool Aim;                  // 투사체 — 판을 At → MoveTo 쪽으로 돌린다(원작 판은 가로로 날아가게 짜였다. 많아야 AIM_MAX)
        public Vector3? AimAt;             // 날지 않는 판을 이 자리 쪽으로 돌린다(Aim 과 같이 — 카드 총구 레이저가 무기 끝에서 허공으로 뻗던 것, 2026-10-13 리뉴아)
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
        public static float EDGE = 0.1f, SHEET_EDGE = 0.22f, EDGE_MIN = 0.1f;   // 표 값 하한 0.1(10-15 「타격 임팩트 각진 것」 — Codex 는 0 을 권했지만 칸 끝까지 찬 그림이 네모로 잘렸다. 0.14 는 그림을 줄여서 0.1)
        // 그림마다 가장자리 폭(2026-10-13) — Tools/fx_edge.py 가 원본 그림의 칸 바깥 세기를 재 Resources/<Root>/fx_edge.txt 에 적는다.
        //   열쇠 = 그림 · 시트 칸 수 · 더하기. 폭이 음수면 타원으로 걷는다(둥근 불빛이 네모로 잘린 그림). lum = 알파 없는 그림을 알파 섞기로 — 밝기를 알파로.
        //   표에 없으면 EDGE 그대로
        static Dictionary<string, Vector2> edgeTab;
        static void EdgeOf(FxEmitter e, out float w, out bool lum)
        {
            if (edgeTab == null)
            {
                edgeTab = new Dictionary<string, Vector2>();
                var ta = Resources.Load<TextAsset>(FxLibrary.Root + "/fx_edge");
                if (ta != null)
                    foreach (var line in ta.text.Split('\n'))
                    {
                        if (line.Length == 0 || line[0] == '#') continue;
                        var c = line.TrimEnd('\r').Split('\t');
                        if (c.Length < 6) continue;
                        float.TryParse(c[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ew);
                        edgeTab[c[0] + "|" + c[1] + "|" + c[2] + "|" + c[3]] = new Vector2(ew, c[5] == "1" ? 1 : 0);
                    }
            }
            w = EDGE; lum = false;
            if (string.IsNullOrEmpty(e.TexPath)) return;
            // _lum 은 원본 이름일 수도, 추출기가 알파를 구운 결과일 수도 있다(Codex) — 화면에서는 이름 판정이 맞았다(비비 물보라 · 실비아 레이스가 검게 나옴). 알파 섞기 판만
            bool nameLum = !e.Add && e.TexPath.IndexOf("_lum", StringComparison.OrdinalIgnoreCase) >= 0;
            lum = nameLum;
            int tx = e.HasSheet ? Mathf.Max(1, e.Sheet.Tx) : 1, ty = e.HasSheet ? Mathf.Max(1, e.Sheet.Ty) : 1;
            if (!edgeTab.TryGetValue(e.TexPath + "|" + tx + "|" + ty + "|" + (e.Add ? 1 : 0), out var v)) return;
            if (v.x != 0) w = v.x;
            lum = !e.Add && (v.y != 0 || nameLum);   // 이름 판정(_lum)도 — 비비 평타 물보라가 검게 나왔다(화면 대조 10-15, Codex 는 표만 쓰자고 했다)
            if (w > 0 && w < EDGE_MIN) w = EDGE_MIN;
        }

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
        float fitK = 1, fitDown;            // FitTop — 지금까지 줄인 배율 · 내린 몫(줄이기만 한다 — 한 번 줄면 그 판이 끝날 때까지)
        const float FIT_MIN = 0.6f, FIT_DOWN = 1.2f;
        float rc = 1, rs = 0;              // 투사체 돌림(cos · sin) — FxPlayOptions.Aim
        const float AIM_MAX = 60f * Mathf.Deg2Rad;
        Vector3 origin, from, to;
        bool moving;
        Em[] ems;
        Mesh mesh;
        MeshRenderer mr;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Vector4> uv1 = new List<Vector4>();
        readonly List<Vector4> uv2 = new List<Vector4>();   // 가리개 그림 자리(10-15) · 셰이더 그래프 재현이면 Custom2
        readonly List<Color> cols = new List<Color>();
        readonly List<List<int>> subs = new List<List<int>>();
        readonly List<Material> mats = new List<Material>();
        Material[] matArr = new Material[0];
        int matCount = -1;

        class Burst { public float T, N, Iv; public int Left; }
        class P
        {
            public float X, Y, Vx, Vy, Lvx, Lvy, Age, Life, Size, Rot, Spin;
            public float OX, OY;   // 방출 순간 원점(월드) — 월드 시뮬 이미터는 여기에 남는다
            public Color C;
            public int Row, F0, Sf;
            public bool Fu;
        }
        class Em
        {
            public FxEmitter E;
            public float T0, Acc;
            public float Edge = EDGE;   // 가장자리 폭(EdgeOf — 음수 = 타원)
            public bool Lum;            // 밝기를 알파로(알파 없는 그림 · 알파 섞기)
            public bool FlipV;          // 판의 세로 축을 뒤집는다(FLIP_V_BY_ART)
            public bool RotLocal;       // 늘인 판을 늘인 뒤에 돌린다(ROT_LOCAL_BY_ART)
            public bool Arc;            // 쐐기 그림을 호로 휘어 그린다(ARC_BY_ART)
            public bool Mesh;           // 원작 메시 몸을 그린다(MeshOk)
            public Material Sg;         // 셰이더 그래프 재현 재질(SG_ON 이고 이미터에 있으면)
            public float ArcScale = 3f; // 호 바깥 반지름 = 판 반 폭 × 이것(ARC_SCALE_BY)
            public Burst[] Bursts;
            public readonly List<P> Parts = new List<P>();
        }

        static readonly Stack<P> pool = new Stack<P>();       // 입자 되쓰기(가비지 없이)
        public static int Live;                       // 모든 무대의 입자 수
        public static int Cap = 1500;
        // 입자 밀도(0~1) — 저사양 모드가 낮춘다(쓰는 쪽이 건다). 방출량 · 터뜨림 수에 곱한다(작은 수는 확률로 — 한 알짜리가 사라지지 않게 최소 1)
        public static float Density = 1f;
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
        public static int Running;   // 지금 도는 장 수(촬영 도구가 이펙트가 다 걷힐 때까지 찍으려고 — 10-16)

        internal static FxRun Create(string name, FxEffect fx, FxSheet sheet, FxPlayOptions o)
        {
            Running++;
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
            r.rc = 1; r.rs = 0;
            var aimTo = o.MoveTo ?? o.AimAt;
            if (aimTo.HasValue && o.Aim)
            {
                // 판은 앞(뒤집으면 -x)으로 날게 짜였다 — 그 앞이 날아가는 쪽을 보게(뒤집힌 판은 부호째 돌린다)
                var d = aimTo.Value - o.At;
                if (d.sqrMagnitude > 1e-6f)
                {
                    float ang = Mathf.Clamp(Mathf.Atan2(d.y * r.flip, d.x * r.flip), -AIM_MAX, AIM_MAX);
                    if (AIM_BASE.TryGetValue(name, out var ab)) ang -= r.flip * ab * Mathf.Deg2Rad;   // 비스듬히 짜인 판 — 짜인 각만큼 덜 돌린다
                    r.rc = Mathf.Cos(ang); r.rs = Mathf.Sin(ang);
                }
            }
            var psPrefab = sheet == null && fx != null && BolzenaFx.UsePs ? (fx.Ps != null ? fx.Ps : PsPrefab(name)) : null;
            if (psPrefab != null)   // (10-16) 원작 파티클 그대로
            {
                // 장면 장 당기기(우리 단위, 사도가 보는 쪽이 +x) — 원작 싸움터는 사도 · 적 간격이 좁아 장면이 둘을 함께 덮지만 우리는 넓어 적 쪽에 깔렸다(10-17 실비아 다과회)
                r.psShift = PS_SHIFT.TryGetValue(name, out var sh) ? new Vector3(r.flip * sh.x * BolzenaFx.UnitWorld / 0.3f, sh.y * BolzenaFx.UnitWorld / 0.3f, 0) : Vector3.zero;
                r.origin += r.psShift;
                r.InitPs(psPrefab); return r;
            }
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
        // 사도별 — 셰이더 그래프([SG]) 이미터만 빼는 텍스처(2026-10-13 깨진 판). 극좌표로 휘어 호(弧)가 되는 쐐기 그림이라 그대로 그리면 세모 판이 된다:
        //   네르(빡침) 평타 파란 세모 = FX_NerRage_Slssh_1(쐐기) · FX_UI_particle_02(마름모 잡음), 실비아 평타 주황 세모 = FX_Silvia_slash1~3(쐐기).
        //   같은 텍스처를 쓰는 다른 사도(로니 등)는 손대지 않는다 — 그 사도 판정 뒤에
        static readonly Dictionary<string, string[]> SG_ONLY_BY_ART = new Dictionary<string, string[]>
        {
            //   (2026-10-14) 네르(빡침) 평타 맞음 Custom_hit(FX_NerRage_Slssh_line_1 — 원작은 셰이더가 일부만 흘려 보이는 긴 선) — 통째로 그리면 바닥 아래까지 닿는 긴 흰 가시(평타 · 추가탄환 b)
            ["nerrage"] = new[] { "FX_NerRage_Slssh_1.png", "FX_UI_particle_02", "FX_NerRage_Slssh_line_1.png" },
            //   (2026-10-14) 클로에 스킬 오라 뒤판(FX_TEX_Tint_Aura_01 — 색이 거의 검정, 원작은 셰이더 틴트) — 시전자 몸 위 검은 연기(스킬 a)
            ["chloe"] = new[] { "FX_TEX_Tint_Aura_01_lum" },
            ["silvia"] = new[] { "FX_Silvia_slash", "FX_UI_particle_02" },
            // 티그(영웅) 고학년 slash_1 의 [SG]_Slash_1~3(FX_TigHero_Slash_4) — 원작은 휜 푸른 초승달, 우리 판은 오른쪽 끝 곧은 모서리 판(2026-10-13 고학년 캡처 15~16칸 · 이 그림은 이 장만 쓴다)
            ["tighero"] = new[] { "FX_TigHero_Slash_4" },
        };
        static bool SgOnly(FxEffect f, FxEmitter e) => e.N != null && e.N.IndexOf("[SG]", StringComparison.Ordinal) >= 0 && ByArt(SG_ONLY_BY_ART, f, e) && !ByArt(ARC_BY_ART, f, e);
        // 호로 휘어 그리는 쐐기 그림(2026-10-15 원작 대조) — 원작은 셰이더 그래프가 극좌표로 휘어 초승달 베기를 만든다. 판 한 장이면 세모라 SG_ONLY 로 빼 왔고,
        //   그래서 네르(빡침) 평타의 큰 흰 초승달 · 실비아 평타 초승달 · 티그(영웅) 고학년 푸른 초승달이 아예 안 보였다. 그림 가로 = 각도, 세로 = 반지름으로 조각 12개 띠
        static readonly Dictionary<string, string[]> ARC_BY_ART = new Dictionary<string, string[]>
        {
            ["nerrage"] = new[] { "FX_NerRage_Slssh_1.png" },
            ["silvia"] = new[] { "FX_Silvia_slash" },
            ["tighero"] = new[] { "FX_TigHero_Slash_4" },
        };
        static readonly Dictionary<string, float> ARC_SCALE_BY = new Dictionary<string, float> { ["nerrage"] = 3f, ["silvia"] = 1.2f, ["tighero"] = 2f };   // 쐐기 판 크기가 그림마다 달라서(실비아 판은 커서 3배면 화면을 덮었다)
        public static float ARC_DEG = 160f, ARC_INNER = 0.3f, ARC_SCALE = 3f, ARC_LIFT = 0.35f, ARC_WHITE = 0.55f;   // 호 각도 · 안쪽 반지름 비 · 바깥 반지름 = 판 반 폭 × ARC_SCALE
        static bool ByArt(Dictionary<string, string[]> tab, FxEffect f, FxEmitter e)
        {
            if (f == null || e.TexPath == null || f.Hero == null || !tab.TryGetValue(f.Hero, out var l)) return false;
            foreach (var s in l) if (e.TexPath.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        // 굴절(일그러뜨림) 재질 이미터(2026-10-13) — 원작은 화면을 grab 해 휘게만 하는 판(재질 FX_RimChaos_Distort)인데, 우리 판은 주 그림(디졸브 원)을 그대로 그려
        //   티그(영웅) 스킬 · 고학년의 매끈한 검은 · 흰 타원이 됐다. 같은 그림을 쓰는 다른 사도(63장)는 굴절 재질이 아니라 사도별로. 고학년 장에도 건다
        static readonly Dictionary<string, string[]> DISTORT_BY_ART = new Dictionary<string, string[]>
        {
            ["tighero"] = new[] { "FX_TEX_Dissolve_Circle_01_lum" },
        };
        static bool Distort(FxEffect f, FxEmitter e) => e.N != null && e.N.IndexOf("[SG]", StringComparison.Ordinal) >= 0 && ByArt(DISTORT_BY_ART, f, e);
        // 카드 장에서 빼는 이미터(이름표와 상관없이 · 2026-10-13 엘다인 캡처 검토) — 사도별 그림:
        //   란 평타 FX_Ran_Slash_5~7 — 원작은 휜 메시(그리는 법 4)에 얹는 세로 칼빛인데 판 한 장(55)으로 그려 바닥 아래까지 닿는 보라 세로판 · 아래 끝 네모
        //   비비 강공 FX_Vivi_Stab_01 — 원작은 내리꽂는 셰이더 판, 우리 판은 머리 위 9.5 에 멈춘 단검 4개(화면 위 허공)
        //   우로스 강공 FX_Uros_Mountain_1 — 금빛 용 끝(+34)의 산 그림, 셋째 적 옆 허공 흰 기둥 · FX_Renewa_Glow_1 — 마스크 셰이더(Custom/FX_TEX_Tint_Mask) 판이라 통째 빛기둥
        static readonly Dictionary<string, string[]> CARD_DROP_BY_ART = new Dictionary<string, string[]>
        {
            ["ran"] = new[] { "FX_Ran_Slash_5", "FX_Ran_Slash_6", "FX_Ran_Slash_7" },
            ["vivi"] = new[] { "FX_Vivi_Stab_01" },
            ["uros"] = new[] { "FX_Uros_Mountain_1", "FX_Renewa_Glow_1" },
            ["tighero"] = new[] { "FX_TigHero_Line_1" },   // 티그(영웅) 스킬 dash_2 의 진한 남색 줄(반투명 남색 · 크기 42) — 판 한 장으로 그려져 화면에 남색 세로 막대로 남았다(2026-10-14 원작 대조)
            // 2026-10-14 캡처 검토
            //   클로에 가시(Slash_seq_mesh — 휜 메시에 얹는 끝이 평평한 쐐기 그림 · FX_IN_Piller 기둥) — 뒤집어도 판 한 장으론 앞 잎사귀까지 내려오는 분홍 판(평타 · 강공 · 스킬 b)
            ["chloe"] = new[] { "FX_Chloe_Slash_", "FX_Chloe_Attackslash_", "FX_IN_Piller.png" },
            //   네르(빡침) 강공 box_glow · 스킬 back_glow(FX_IN_Gradient_Edge — 가로로 고른 세로 띠, asp 4 · 위로 pivot) — 화면 위끝까지 닿는 직사각 빛기둥(타원 걷기를 해도 몸통이 곧다)
            ["nerrage"] = new[] { "FX_IN_Gradient_Edge" },
            //   비비 강공 · 평타 LightSlash · Slash_Highlight(FX_Slash_Seq_07 — 세로 18 단위 칼빛) — 적 발밑 한참 아래 흰 조각
            ["vivi"] = new[] { "FX_Vivi_Stab_01", "FX_Slash_Seq_07" },
            //   에르핀(왕도) 스킬 시전 — 반쪽 빛(Half_Glow_Depth, 아래가 곧게 잘린 그림)이 시전자 밑 분홍 네모 · 램프 빛(Lamp_Light)이 수직 금빛 기둥
            ["erpinroyale"] = new[] { "FX_ErpinRoyale_Half_Glow_Depth_1", "FX_IN_Lamp_Light" },
        };
        // 카드 장에서 빼는 이미터(이미터 이름 앞머리) — 같은 그림을 다른 칸 수로 나눠 쓰는 사도라 그림으로는 못 가른다(2026-10-14):
        //   아야 강공 [Seq]_Wind_Up(FX_Aya_Tex_1 10×4 둘째 줄 — 오른변이 곧은 칼날 그림, 원작은 휜 메시) — 베기 오른쪽 변 수직 잘림. 눈 · 얼음 칸은 남긴다
        static readonly Dictionary<string, string> CARD_DROP_BY_NAME = new Dictionary<string, string> { ["aya"] = "[Seq]_Wind_Up" };
        // 카드 · 고학년 장 모두에서 빼는 이미터(2026-10-14 「란 임팩트 직각으로 잘림」) — 원작은 휜 메시(그리는 법 4)에 얹는 세로 띠 그림인데
        //   우리는 판 한 장으로 그려 끝이 곧은 보라 네모 · 사다리꼴 판이 된다. 원작 번들에서 이 그림을 쓰는 이미터는 모두 메시(판으로 쓰는 곳 없음):
        //   란 FX_Ran_Slash_8 · 9(5×2 세로 띠 — 고학년 cast_5 · slash_1 · slash_2, 스킬 slash_4) · FX_Ran_Slash_3(1×5 — 강공이 빌린 스킬 slash_3 · 스킬 hit_2).
        //   고학년의 초승달(FX_Ran_Uslash_6~8 — 원작도 판)은 남는다
        static readonly Dictionary<string, string[]> MESH_DROP_BY_ART = new Dictionary<string, string[]>
        {
            ["ran"] = new[] { "FX_Ran_Slash_3", "FX_Ran_Slash_8", "FX_Ran_Slash_9" },
        };
        // 어둡게 까는 판(2026-10-13) — 알파 섞기 · 색이 어두운(최대 채널 < 0.5) 둥근 빛 그림. 원작은 밝은 판 뒤를 살짝 눌러 주는 「Glow_bg · Back_Glow · _Black · Core_b」인데
        //   우리 판은 밝은 몫이 빠지거나(입체 메시 · 셰이더 그래프) 블룸이 없어 검은 타원 · 원판만 남았다(티그(영웅) 스킬 · 이드 강공 구체 · 에르핀 · 클로에 · 디아나 · 우로스 …).
        //   그림이 빛 · 고리 꼴일 때만(비비의 검은 물 · 칼빛처럼 일부러 어두운 그림은 둔다). 고학년 장에도 건다
        static readonly System.Text.RegularExpressions.Regex DARK_TEX = new System.Text.RegularExpressions.Regex("(glow|grow|flash|ring|circle|flare)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        static bool DarkBack(FxEmitter e)
        {
            // 셰이더 그래프 판([SG] — 색 · 세기를 셰이더가 따로 민다)은 두고, 아틀라스 · 일반 판만
            if (e.Add || e.TexPath == null || (e.N != null && e.N.IndexOf("[SG]", StringComparison.Ordinal) >= 0) || !DARK_TEX.IsMatch(System.IO.Path.GetFileName(e.TexPath))) return false;
            float c = 0;
            if (e.Col == null || e.Col.Length == 0) c = 1;
            else foreach (var k in e.Col) c = Mathf.Max(c, Mathf.Max(k.r * e.Tint.r, Mathf.Max(k.g * e.Tint.g, k.b * e.Tint.b)));
            float ol = 0;
            if (e.ColOL == null || e.ColOL.Length == 0) ol = 1;
            else foreach (var k in e.ColOL) ol = Mathf.Max(ol, Mathf.Max(k.y, Mathf.Max(k.z, k.w)));
            return c * ol < 0.5f;
        }
        // 위아래가 뒤집혀 짜인 휜 메시 칼빛(판의 세로 축이 아래로). 사도별 · 이미터 이름 앞머리
        //   (2026-10-14) 클로에 가시는 뒤집어도 끝이 평평한 판이 앞 잎사귀까지 내려와 카드 장에서 뺐다(CARD_DROP_BY_ART) — 지금은 비어 있다
        static readonly Dictionary<string, string> FLIP_V_BY_ART = new Dictionary<string, string>();
        // 날아가는 쪽이 비스듬히 짜인 판(도, 앞 = 0 · 위 = +) — Aim 으로 적 쪽으로 돌릴 때 이만큼 덜 돌린다(2026-10-14):
        //   리뉴아 강공 레이저(lazer_1 — 끝 고리가 (12, 13.8), 47.5°)가 적이 아니라 오른쪽 위 하늘로 · 이드 강공 레이저(attack_2 — 공 앞 원판에서 바닥 빛(4, −1.7)으로, −22°)
        static readonly Dictionary<string, float> AIM_BASE = new Dictionary<string, float> { ["fx_renewa_attack_lazer_1"] = 47.5f, ["fx_ed_attack_2"] = -22f };
        // 입자 제 크기를 가로 · 세로 따로 준 판(원작 size3D — 우리 quad 에 구워짐)을 무작위로 돌리는 이미터(2026-10-14 「티그 영웅 타격 임팩트 이상함」) —
        //   원작은 늘인 판을 입자마다 돌려 맞는 자리에 비스듬한 긴 별빛이 엇갈리는데, 우리는 돌린 뒤에 세로로 늘여 모두 곧은 세로 빛기둥(몸 위에서 발밑 아래까지)이 됐다.
        //   티그(영웅) 강공 hit_3 · 스킬 hit_1 · 고학년 hit_1 의 [Atlas_add]_Star(FX_IN_Particle_Star_01, 1×3 · 1×3.33). 사도별 · 그림 이름
        // 원작 메시 몸을 그릴 이미터(10-15) — 셰이더 그래프를 안 쓰는 단순 셰이더(모바일 입자 · 아틀라스)는 다, 셰이더 그래프 메시는 화면으로 확인한 사도의 납작한 것만.
        //   셰이더 그래프 메시를 다 그렸더니(20명 대조) 디아나(왕년) 고학년 회색 기둥(FX_TEX_Tint — 밝기 = 투명도) · 비비(신성) 고학년 화면 반을 덮는 초록 띠 ·
        //   네르(빡침) · 실비아 평타가 발밑에 눕는 파란 · 검붉은 고리(FX_Master Custom2 UV 흐름)가 나왔다. 셰이더를 옮기기 전까지는 판(옛 방식)으로
        static readonly HashSet<string> MESH_ART = new HashSet<string> { "tighero", "dianayester" };
        // 장 하나만 입체 메시를 그린다 — 벨라 고학년 소용돌이 기둥(hit_e, M3D). 사도 전체(MESH_ART)를 열면 강공 attack2 메시까지 벽이 될 수 있어서(fix_Y 5-2)
        static readonly HashSet<string> MESH_FX = new HashSet<string> { "fx_vela_ultimate1_hit_e" };
        // 메시보다 옛 방식이 원작에 가까운 사도 — 실비아 평타 칼빛 메시가 적 발밑 주황 고리로 눕는다(호 흉내 ARC_BY_ART 가 흰 초승달로 더 비슷, 10-15)
        static readonly HashSet<string> MESH_NO_ART = new HashSet<string> { "silvia" };
        static bool MeshOk(FxEffect fx, FxEmitter e)
        {
            // 입체 메시(원통 · 돔 · 고리 벽)는 셰이더가 단순해도 원작은 UV 흐름 · 가리개로 일부만 보인다 — 통째로 그리면 화면을 덮는 벽이 됐다(20명 대조 B: 죠안 강공 원통 벽 · 고학년 흰/붉은 화면, 이드 머리 위 띠). 납작한 메시만
            // 가리개가 있어도 입체 메시는 뺀다 — 비비(신성) 고학년 초록 띠가 다시 화면을 덮었다(10-15 재시험). 원작은 UV 흐름 · 꼭짓점 색까지 함께 쓴다
            if (!e.HasMesh) return false;
            if (SG_ON && e.Sg != null) return true;   // 셰이더 그래프 재현 — 원작처럼 UV 흐름 · 가리개 · 디졸브로 그린다(입체 메시도)
            if (MESH_FX.Contains(fx.name)) return true;
            if (e.M3d) return false;
            if (fx.Hero != null && MESH_NO_ART.Contains(fx.Hero)) return false;
            if (string.IsNullOrEmpty(e.Shn) || e.Shn.StartsWith("Mobile/", StringComparison.Ordinal)) return true;
            if (e.HasC1 && e.Shn.EndsWith("FX_Master", StringComparison.Ordinal)) return true;   // 사용자 데이터를 옮긴 FX_Master 칼빛(UV 밀기 · 밝기) — 10-15
            return fx.Hero != null && MESH_ART.Contains(fx.Hero);
        }
        bool curMesh;
        static readonly Dictionary<string, string[]> ROT_LOCAL_BY_ART = new Dictionary<string, string[]>
        {
            ["tighero"] = new[] { "FX_IN_Particle_Star_01" },
        };

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
                bool meshOk = MeshOk(fx, e);
                if ((e.M3d && !meshOk && !only3d) || e.Tex == null) continue;   // 그릴 수 있는 메시 몸이면 그대로(10-15)
                bool ult = fx.name.IndexOf("ultimate", StringComparison.Ordinal) >= 0;
                // 셰이더 그래프로만 뜻이 있는 그림 · 굴절 판 · 어둡게 까는 판은 고학년 장에서도 뺀다(2026-10-13 고학년 캡처: 네르(빡침) 파란 그라디언트 판 · 이드 공 안 회색 원판 · 티그 굴절 타원)
                //   사도별 셰이더 그래프 쐐기(SG_ONLY_BY_ART — 네르(빡침) 고학년 파란 세모 · 네모 판도 같은 그림)도 고학년까지
                bool sgOn = SG_ON && e.Sg != null;   // 셰이더 그래프 재현이면 「셰이더로만 뜻이 있는 그림」 거르기를 안 한다(굴절은 그대로 뺀다)
                if ((!sgOn && (ShaderOnly(e) || SgOnly(fx, e) || DarkBack(e))) || Distort(fx, e)) continue;
                if (!ult && ByArt(CARD_DROP_BY_ART, fx, e)) continue;   // 사도별 메시 · 셰이더 판(란 · 비비 · 우로스)은 카드 장만
                if (!ult && fx.Hero != null && e.N != null && CARD_DROP_BY_NAME.TryGetValue(fx.Hero, out var dn) && e.N.StartsWith(dn, StringComparison.Ordinal)) continue;
                if (ByArt(MESH_DROP_BY_ART, fx, e)) continue;   // 사도별 휜 메시 칼빛(란) — 카드 · 고학년 모두
                if (o.Only != null && Array.IndexOf(o.Only, i) < 0) continue;
                var em = new Em { E = e, T0 = Rand(e.Delay) };
                em.FlipV = e.HasQuad && e.Quad.w < 0 && fx.Hero != null && e.N != null && FLIP_V_BY_ART.TryGetValue(fx.Hero, out var fv) && e.N.StartsWith(fv, StringComparison.Ordinal);
                em.RotLocal = e.HasQuad && ByArt(ROT_LOCAL_BY_ART, fx, e);
                em.Arc = !BolzenaFx.OriginalOnly && ByArt(ARC_BY_ART, fx, e);
                em.Mesh = meshOk;
                em.Sg = SG_ON ? e.Sg : null;
                if (em.Arc && fx.Hero != null && ARC_SCALE_BY.TryGetValue(fx.Hero, out var asc)) em.ArcScale = asc;
                EdgeOf(e, out em.Edge, out em.Lum);
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

        // ── 원작 ParticleSystem 그대로(10-16) ── 프리팹을 띄우고 자리 · 방향 · 크기 · 끝만 우리가 맡는다
        GameObject psInst;
        ParticleSystem[] psAll, psRoots;
        bool psStopped;
        static readonly bool psLog = System.Environment.GetEnvironmentVariable("BOLZENA_PS_LOG") == "1";
        int psLogN = -1;
        static ParticleSystem.Particle[] psBuf = new ParticleSystem.Particle[64];
        static readonly Dictionary<string, GameObject> psCache = new Dictionary<string, GameObject>();
        Vector3 psShift;
        public static int BEHIND_ORDER = 14;   // 원작 바닥 레이어 판 — 사도 · 적(22~46) 뒤 · 떠다니는 티끌(5) 위
        // 장면 장 당기기 — 장 이름 → (x, y) 원작 단위 × 0.3 = 우리 단위(UnitWorld 0.3 기준). 실비아 다과회는 장면 가운데가 원점 +3.9 라 그만큼 시전자 쪽으로
        static readonly Dictionary<string, Vector2> PS_SHIFT = new Dictionary<string, Vector2>
        {
            ["fx_silvia_skill1_teatime_1_default"] = new Vector2(-3.9f, 0),   // 탁자(teatable)는 원점 근처에 짜여 그대로
            ["fx_dianayester_skill_cast_1"] = new Vector2(-0.525f, 1.0f),   // (10-18 2차 사용자 「도끼에서 번쩍」) 크기 0.5(CARD_PART_SCALE)로 줄며 구 · 별(원점 −3.5, 6.7 = 치켜든 도끼)이 몸통으로 끌려왔다 — 줄어든 몫만큼 되민다
            ["fx_tighero_skill_dash_1"] = new Vector2(-1.15f, 0),   // (10-18 4차 「초승달이 대상보다 너무 멀리」) 크기 0.5 로 줄인 초승달 범위(대상 −3.6 ~ +1.5)를 대상 둘레로 — TARGET_FACING 으로 r.flip 이 뒤집혀 음수 = 적 쪽(오른쪽)
        };
        // 장 안에서 끌 이미터(원작 그리기와 어긋나거나 사용자가 빼라 한 것, 10-17 3차)
        // 원작이 스프라이트 모드(UV 모듈 mode 1)로 그리던 판 — 우리 프리팹은 스프라이트 모드를 버려 그림 축이 90° 누웠다(10-17 네르 평타 맞음: 원작은 반달 그림의 밑변이 긴 축인 가는 렌즈 「/」,
        //   우리는 밑변이 짧은 축인 굵은 창 「\」). 크기 X·Y 를 맞바꾸고 90° 돌린다
        static readonly Dictionary<string, string[]> SPRITE_ROT90 = new Dictionary<string, string[]>
        {
            ["fx_nerrage_attack_1_hit"] = new[] { "[SG]Custom_hit_1", "[SG]Custom_hit_2" },
            ["fx_nerrage_attack_1_hit_2"] = new[] { "[SG]Custom_hit_1", "[SG]Custom_hit_2" },
        };
        const float ROT90_THIN = 0.3f;
        static void Rot90(ParticleSystem ps)
        {
            var m = ps.main;
            if (m.startSize3D) { var x = m.startSizeX; m.startSizeX = m.startSizeY; x.constant *= ROT90_THIN; x.constantMin *= ROT90_THIN; x.constantMax *= ROT90_THIN; m.startSizeY = x; }   // 두께 — 원작 영상 렌즈 두께(길이의 약 0.08)에 맞춤
            var r = m.startRotation; r.constant += 90f * Mathf.Deg2Rad; r.constantMin += 90f * Mathf.Deg2Rad; r.constantMax += 90f * Mathf.Deg2Rad; m.startRotation = r;
            var so = ps.sizeOverLifetime;
            if (so.enabled && so.separateAxes) { var x = so.x; so.x = so.y; so.y = x; }   // 곡선 배율(curveMultiplier)도 곡선과 함께 바뀐다
        }
        static readonly HashSet<string> Z_FWD = new HashSet<string> { "fx_ed_attack_1_1", "fx_ed_attack_1_2", "fx_ed_attack_1_3", "fx_ed_attack_1_4", "fx_aya_skill1_proj_1", "fx_epica_attack_proj_2", "fx_renewa_attack_proj_1", "fx_yomi_attack2_proj_1" };   // (10-18 2차) 리뉴아 평타 화살촉 · 요미 강공 혜성도 판 깊이(z)가 길이   // (10-18) 이드 레이저 갈래 · 아야 나비 — 판 깊이(z)가 화면 가로로 짜였다(나비가 세로줄로 보였다)
          // (10-18 검증_1) 에피카 강공 불 에피콘도 — 이미터가 z −1.04(에피콘) ~ −2.6(연기)에 놓이고 꼬리 빔 · 불꽃 판(정렬 Local)이 깊이 쪽으로 서 있어 돌리지 않으면 적 앞 세로 노란 줄만 보였다
          // (10-17 3차 조사) 이드 평타 레이저 — 위로 짧게 서던 것
        static readonly Dictionary<string, string[]> PS_MUTE = new Dictionary<string, string[]>
        {
            ["fx_uieru_skill_ground_1"] = new[] { "Root_LRain" },   // 하늘에서 떨어지는 잎 · 비 알갱이(「잎 날리는 것 없애」)
            ["fx_joanne_attack2_1_buff_1"] = new[] { "[Mesh]_Top_Cylinder02", "[Mesh]_Top_Cylinder02_1" },   // 원통 벽 — 원작은 UV 흐름 · 가리개로 테두리만, 우리는 통째로 세로 타원
            ["fx_joanne_attack2_1_buff_2"] = new[] { "[Mesh]_Top_Cylinder02", "[Mesh]_Top_Cylinder02_1" },
            ["fx_vivi_skill_barrier_1"] = new[] { "[SG]_Water_01" },   // (10-18) 구 메시를 눌러 만든 원판 — 원작 재질(내장 셰이더)을 못 살려 머리에 불투명 흰 접시로 얹혔다. 돌리면 몸을 통째로 가렸다
            ["fx_nerrage_attack_1_hit"] = new[] { "[SG]Custom_hit_1", "[SG]Custom_hit_2" },     // (10-18 4차 사용자 결정 「흰 레이저 없애」) 원작엔 있으나 뺀다 — 사선 판만, 맞는 자리 가루 · 마름모는 남김
            ["fx_nerrage_attack_1_hit_2"] = new[] { "[SG]Custom_hit_1", "[SG]Custom_hit_2" },
            ["fx_ed_ultimate_ground"] = new[] { "Ground_Glow_1 (1)" },   // (10-18 4차 사용자 결정 「원작대로」) 이드 고학년 밝은 연보라 구 껍질(반원 빛 알파 1.0) — 원작(14.png)엔 안 보인다. 고리 궤도 · 그늘(Ground_Glow_1 검정)은 남김
        };
        // 프리팹 조립(FxPsBuild.ApplyXf)이 돌림을 버린 이미터 — 원작 세계 행렬의 y 열이 0(구 메시를 y 로 눌러 원판으로 쓴다)이면 돌림을 안 넣어
        //   원판이 바닥에 눕는다. (10-18 3차 비비 스킬 「구 위 불투명 흰 타원」) [SG]_Water_01 — xf M 열 x (0.8, 0, 0) · y 0 · z (0, −0.752, −0.274):
        //   원작은 원판이 화면을 보고(z 축이 −20° 기운 아래쪽) 몸 전체를 우윳빛으로 덮는다(저학년.mp4 0.9~1.7초). 우리는 회전 없이 (0.8, 0.0001, 0.8) 이라 머리~가슴에 납작한 접시
        static readonly Dictionary<string, (string child, Quaternion rot)> PS_ROT_FIX = new Dictionary<string, (string, Quaternion)>
        {
            // (10-18 4차 되돌림) 비비 [SG]_Water_01 을 화면 쪽으로 돌리자 불투명 흰 원판이 비비를 통째로 가렸다(cap_fix4 04_비비_3스킬 0.8~1.9초).
            //   원작 재질은 FX_empty_ab(빈 재질 + 스프라이트 FX_IN_SimpleRect, 색 0.623 · 알파 1, 수명 무한이라 수명 알파도 거의 1) — 우리가 가진 값으로는 반투명이 나올 근거가 없어 뺐다.
            //   원작 화면의 우윳빛은 같은 이름의 다른 이미터(재질 FX_Naia_Wave_05 — 물결 셰이더 그래프, 가리개 · 흐름)일 수 있으나 그 이미터는 추출되지 않았다
            //["fx_vivi_skill_barrier_1"] = ("[SG]_Water_01", Quaternion.LookRotation(new Vector3(0, -0.94f, -0.342f), new Vector3(0, -0.342f, 0.94f))),
        };
        // (10-18 3차 시온 스킬 「X 조준이 +자로」) 추출기가 원작 직렬화 값을 유효숫자 3자리로 줄인다(fx_extract_core.py r3 · _fin). 그림 칸 고정값(UV 모듈 frameOverTime 상수)이
        //   0.3125(32칸 중 10번 = 가는 「+」, 45° 판이라 X) → 0.312 가 되어 0.312 × 32 = 9.984 → 9번 빈 칸을 그렸다(X 가 안 보이고 팔 · 링만 남음).
        //   요미 강공 혜성 Proj_1 도 4/9 = 0.444… → 0.444 × 9 = 3.996 → 3번 칸. 칸 수를 곱해 정수 바로 아래(0.95 넘게)면 그 정수 칸으로 되돌린다.
        //   같은 꼴이 다른 장(75 이미터 — 아라그니아 · 란 · 리뉴아 고학년 · 우로스 · 우이 …)에도 있으나 화면 확인 전이라 이름으로 좁혔다
        //   (10-18 4차) 시온 crosshair_3 은 뺐다 — 10번 칸으로 되돌리자 크기 50 판이 화면 전체를 청록으로 덮었다(cap_fix4 01813 · 01913). 칸은 맞으나 판 크기 · 밝기(Mul 100)가 원작과 다른 듯 — 원인 미확정
        //   (10-18 4차 사용자 「디아나 스킬 끝 방패 · 사슴뿔 계속」) 디아나 스킬 대지 Ground_2(FX_Diana_Particle_1 1×3, 0.333)가 0번 줄(바위 · 흰 터짐 · 불꽃 뿔)을 16초 그렸다 — 1번 줄 = 가로 용암 띠.
        //   같은 그림 3×3 의 시전 impactglow (1)(0.111 → 0번 바위 · 1번 흰 터짐), 고학년 대지 Glow_Ground_1(FX_DianaYester_Ground_1 1×3, 0.333 → 0번 불빛 · 1번 빛나는 균열 — 짝 Glow_Ground_2 · 3 이 2번 · 0번)도.
        //   그림 칸 내용은 텍스처로 확인(10-18). 바뀌는 것은 칸뿐 — 판 크기 · 밝기는 그대로이고 지금도 같은 판에 그림(0번 칸)이 그려지고 있어 덮는 넓이는 늘지 않는다(시온 crosshair_3 은 빈 칸 → 그림 칸이라 새로 덮였다)
        static readonly HashSet<string> SHEET_SNAP = new HashSet<string> { "fx_yomi_attack2_proj_1", "fx_dianayester_skill_ground_1", "fx_dianayester_skill_cast_1", "fx_dianayester_ultimate_ground_1" };
        static void SnapSheet(ParticleSystem ps)
        {
            var ts = ps.textureSheetAnimation;
            if (!ts.enabled || ts.mode != ParticleSystemAnimationMode.Grid || ts.frameOverTime.mode != ParticleSystemCurveMode.Constant) return;
            float n = (ts.animation == ParticleSystemAnimationType.SingleRow ? ts.numTilesX : ts.numTilesX * ts.numTilesY) * Mathf.Max(1f, ts.cycleCount);
            float x = ts.frameOverTime.constant * n, k = Mathf.Round(x);
            if (k > x && k - x < 0.05f && k < n) ts.frameOverTime = new ParticleSystem.MinMaxCurve((k + 0.001f) / n);
        }
        // (10-18 3차 비비(신성) 고학년 「성격에 따라 레이저 색」) 굵은 레이저 4줄기(full_laser_1~4)는 원작 번들에 성격 갈래가 없다. 이미터 이름 · 셰이더 그래프가
        //   laser_one_<성격>_default 와 같고 색만 다르며, laser_1 의 색은 resonance 판과 같다(Custom_Laser_fr 0.57, 1, 0.49 …) — 원작은 갈래 장의 자리를 옮겨 쓴 것으로 본다.
        //   원작 영상 고학년_Jolly(노랑) · 고학년_Naive(초록) · 고학년(하늘색 = cool)에서 굵은 줄기 색이 성격마다 바뀐다 → 같은 이름 이미터의 시작 색 · 수명 색을 그 성격 판에서 옮긴다
        static readonly System.Text.RegularExpressions.Regex VIVI_LASER_N = new System.Text.RegularExpressions.Regex(@"^fx_vividivine_ultimate_full_laser_\d$");
        static readonly Dictionary<string, string> MOOD_ALIAS = new Dictionary<string, string> { ["[SG]Custom (1)"] = "[SG]Custom (2)", ["[SG]Custom_Noise2"] = "[SG]Custom_Noise" };   // laser_1~4 에만 있는 이름 — 같은 그림 · 색의 갈래 판 이미터
        void MoodColors()
        {
            if (Name == null || UltFx.Mood == null || UltFx.Mood == "resonance" || !VIVI_LASER_N.IsMatch(Name)) return;
            var src = PsPrefab("fx_vividivine_ultimate_full_laser_one_" + UltFx.Mood + "_default");
            if (src == null) return;
            var by = new Dictionary<string, ParticleSystem>();
            foreach (var s in src.GetComponentsInChildren<ParticleSystem>(true)) if (!by.ContainsKey(s.name)) by[s.name] = s;
            foreach (var ps in psAll)
            {
                if (!by.TryGetValue(ps.name, out var s) && !(MOOD_ALIAS.TryGetValue(ps.name, out var al) && by.TryGetValue(al, out s))) continue;
                var m = ps.main; m.startColor = s.main.startColor;
                var col = ps.colorOverLifetime; var sc = s.colorOverLifetime;
                col.enabled = sc.enabled; if (sc.enabled) col.color = sc.color;
            }
        }
        // 프리팹은 Resources/<Root>/Ps/<이름> — 가져오기 일괄 편집 중에 저장해 FxEffect.Ps 참조가 비는 일이 있어 이름으로도 찾는다
        static GameObject PsPrefab(string name)
        {
            if (psCache.TryGetValue(name, out var g)) return g;
            g = Resources.Load<GameObject>(FxLibrary.Root + "/Ps/" + name);
            psCache[name] = g;
            return g;
        }
        void InitPs(GameObject prefab)
        {
            psInst = Instantiate(prefab, transform, false);
            psInst.name = "ps";
            psAll = psInst.GetComponentsInChildren<ParticleSystem>(true);
            if (PS_MUTE.TryGetValue(Name, out var mute))
            {
                foreach (var ps in psAll) if (Array.IndexOf(mute, ps.name) >= 0) ps.gameObject.SetActive(false);
                psAll = Array.FindAll(psAll, x => x.gameObject.activeSelf);
            }
            MoodColors();
            if (SHEET_SNAP.Contains(Name)) foreach (var ps in psAll) SnapSheet(ps);
            if (PS_ROT_FIX.TryGetValue(Name, out var rfx)) foreach (var ps in psAll) if (ps.name == rfx.child) ps.transform.localRotation = rfx.rot;
            if (SPRITE_ROT90.TryGetValue(Name, out var r90)) foreach (var ps in psAll) if (Array.IndexOf(r90, ps.name) >= 0) Rot90(ps);
            // (10-18 사용자 4차 「에피카 뒤로 차가 소환 — 적 쪽으로」) 미니카 · 미니 배는 원작에서 에피카 뒤(x −15)에서 적 쪽(+x)으로 달린다 — 우리 무대에선 속도가 뒤집혀 뒤로 빠졌다(음수 이미터 배율 × Local 속도, 까닭은 짐작)
            if (Name != null && (Name.StartsWith("fx_epica_ultimate_desert_minicar") || Name.StartsWith("fx_epica_ultimate_sea_miniship")))
                foreach (var ps in psAll) { var v = ps.velocityOverLifetime; if (v.enabled) v.xMultiplier = -v.xMultiplier; }
            // (10-18 사용자 4차 「요미 달빛이 두 개」) 일렁이는 달 [SG]_Moon_Wave 만 — 데이터 그림(노이즈 · 달 알파)이 sRGB 로 읽혀 일그러뜨림이 한쪽으로 쏠렸다. 셰이더(FxShock Kind 3)가 이 판에서만 감마로 되돌린다
            if (Name == "fx_yomi_ultimate_3")
                foreach (var ps in psAll)
                    if (ps.name == "[SG]_Moon_Wave" && ps.TryGetComponent<ParticleSystemRenderer>(out var wr)) { var mpb = new MaterialPropertyBlock(); wr.GetPropertyBlock(mpb); mpb.SetFloat("_Data_Gamma", 1f); wr.SetPropertyBlock(mpb); }
            var subs = new HashSet<ParticleSystem>();
            foreach (var ps in psAll) { var sm = ps.subEmitters; if (!sm.enabled) continue; for (int i = 0; i < sm.subEmittersCount; i++) { var c = sm.GetSubEmitterSystem(i); if (c) subs.Add(c); } }
            var roots = new List<ParticleSystem>();
            foreach (var ps in psAll)
            {
                var rd = ps.GetComponent<ParticleSystemRenderer>();
                if (rd)
                {
                    // 원작 바닥 레이어(FxPsBuild 가 −1000 으로 표시) — 사도 · 적(22~46) 뒤, 배경 위(BEHIND_ORDER). 나머지는 원작 순서 값만큼 장 순서 위
                    if (rd.sortingOrder <= -500) rd.sortingOrder = BEHIND_ORDER + Mathf.Clamp(rd.sortingOrder + 1000, 0, 4);
                    else rd.sortingOrder = o.Order + Mathf.Clamp(rd.sortingOrder, -40, 40);
                    if (Name != null && Name.StartsWith("fx_nerrage_ultimate") && ps.name.Contains("BACK_")) rd.sortingOrder -= 5;   // (재검토) 어두운 구 — 같은 순서 값이라 크리스털에 덮여 안 보였다(원작은 원근 거리로 뒤)
                    if (!string.IsNullOrEmpty(o.SortingLayer)) rd.sortingLayerName = o.SortingLayer; else rd.sortingLayerID = 0;
                }
                // 월드 공간 입자는 싸움터(부모) 공간에서 — 카메라 연출이 싸움터를 확대 · 이동하면 이미 나온 입자도 같이(10-17 사용자 「실비아 임팩트 계속 움직임」 · 「카메라 풀리며 따라 움직임」)
                if (o.CarryParticles) { var ml = ps.main; ml.simulationSpace = ParticleSystemSimulationSpace.Local; }
                else if (transform.parent != null) { var mn = ps.main; if (mn.simulationSpace == ParticleSystemSimulationSpace.World) { mn.simulationSpace = ParticleSystemSimulationSpace.Custom; mn.customSimulationSpace = transform.parent; } }
                if (!subs.Contains(ps)) roots.Add(ps);
            }
            psRoots = roots.ToArray();
            psStopped = false; psFadeK = 1f;
            k = BolzenaFx.UnitWorld * o.Scale;
            PsPlace();
            foreach (var ps in psRoots) { ps.Clear(false); ps.Play(false); }
        }
        void PsPlace()
        {
            UpdateOrigin();
            // origin 은 싸움터(부모) 좌표다 — FxActor 자리(UnitView.Fx)와 UltFx 의 셈이 모두 싸움터 좌표이고, 옛 판 그리기(Build)도 메시 꼭짓점을 이 무대(부모 아래 단위 변환) 안에 둔다.
            //   (10-18 사용자 2차 「이드 구가 엉뚱한 자리」 · 시온 불꽃 · 아야 씨앗) 전엔 이 값을 월드 자리(position)로 넣어, 고학년 카메라(FieldRig 가 싸움터를 확대 · 이동)가
            //   크기(localScale)만 키우고 자리는 안 옮겼다 — 유닛은 월드 z·(P − c), 장은 P 에 놓여 (z−1)·P − z·c 만큼 벌어졌다. 무대(FxRun)는 부모 바로 아래 단위 변환이라
            //   local 로 넣으면 부모가 확대 · 이동 · 흔들림을 그대로 준다(따라가지 않는 장은 origin 이 그대로라 싸움터와 같이 움직인다 — 10-17 「카메라 풀리며 따라 움직임」 고침도 그대로)
            var tr = psInst.transform;
            tr.localPosition = origin;
            float ang = Mathf.Atan2(rs, rc) * Mathf.Rad2Deg;
            bool back = o.MoveBack > 0 && t > o.MoveDelay + (o.MoveDur > 0 ? o.MoveDur : 0.35f) + o.MoveBack * 0.5f;   // 왕복 — 돌아서는 때부터 반대쪽을 본다(아야 나비, 10-18)
            float fy = back ? -flip : flip;
            var yaw = Z_FWD.Contains(Name) ? Quaternion.Euler(0, -90f * fy, 0) : Quaternion.identity;   // 길이가 깊이(−z)로 짜인 판 — 원작은 Y −90° 돌려 적 쪽(+x)으로 쓴다
            tr.localRotation = Quaternion.Euler(0, 0, ang) * Quaternion.Euler(-BolzenaFx.CamTilt, 0, 0) * yaw;
            tr.localScale = new Vector3(flip * k, k, k);
        }
        void PsTick(float dt)
        {
            t += dt;
            foreach (var ps in psAll) { var m = ps.main; m.simulationSpeed = BolzenaFx.Rate; }
            PsPlace();
            if (psLog && (int)(t * 5) != psLogN)   // (점검) BOLZENA_PS_LOG=1 — 0.2초마다 이미터별 화면 크기 · 입자 수
            {
                psLogN = (int)(t * 5);
                var sb = new System.Text.StringBuilder("[PsLog] " + name + " t=" + t.ToString("0.0") + " until=" + o.Until.ToString("0.0") + " dur=" + Duration.ToString("0.0") + " rate=" + BolzenaFx.Rate.ToString("0.0") + " o=" + origin.x.ToString("0.0") + "," + origin.y.ToString("0.0") + (moving ? " mv" : ""));
                foreach (var ps in psAll)
                {
                    if (ps.particleCount == 0) continue;
                    if (psBuf.Length < ps.particleCount) psBuf = new ParticleSystem.Particle[ps.particleCount];
                    ps.GetParticles(psBuf, 1);
                    var q = psBuf[0]; var sz = q.GetCurrentSize3D(ps); var ls = ps.transform.lossyScale;
                    var wp = ps.main.simulationSpace == ParticleSystemSimulationSpace.World ? q.position : ps.transform.TransformPoint(q.position);
                    sb.Append(" | ").Append(ps.name).Append(" n").Append(ps.particleCount).Append(" sz").Append((sz.x * Mathf.Abs(ls.x)).ToString("0.0")).Append("x").Append((sz.y * Mathf.Abs(ls.y)).ToString("0.0"))
                      .Append(" @").Append((wp.x - origin.x).ToString("0.0")).Append(",").Append((wp.y - origin.y).ToString("0.0")).Append(" life").Append(q.remainingLifetime.ToString("0.00")).Append(" a").Append((q.GetCurrentColor(ps).a / 255f).ToString("0.00")).Append(" r").Append(q.rotation3D.ToString("0")).Append(" rt").Append(ps.transform.eulerAngles.ToString("0"));
                }
                Debug.Log(sb.ToString());
            }
            if (!psStopped && t >= o.Until) { psStopped = true; foreach (var ps in psRoots) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
            bool alive = false;
            foreach (var ps in psRoots) if (ps.IsAlive(true)) { alive = true; break; }
            // 수명이 사실상 끝없는 입자(원작 방어막 고리 · 빛 — 수명 99999초, 원작은 게임이 장을 거둔다) — 뿜기를 멈춘 뒤엔 그것만 남으면 끝
            if (alive && psStopped && t > o.Until + 0.1f)
            {
                alive = false;
                foreach (var ps in psAll)
                {
                    int n = ps.particleCount; if (n == 0) continue;
                    if (psBuf.Length < n) psBuf = new ParticleSystem.Particle[n];
                    n = ps.GetParticles(psBuf, n);
                    for (int i = 0; i < n; i++) if (psBuf[i].remainingLifetime < 100f) { alive = true; break; }
                    if (alive) break;
                }
            }
            // 끝 — 옛 판 그리기와 같게: 정한 끝(Until)부터 FADE 동안 입자 알파를 내려 걷고, 장 길이 + 1.5초가 상한(10-16 전수조사: 뿜기만 멈추면 이미 나온 입자가 제 수명대로 몇 초씩 남았다 — 리뉴아 X자 빛 · 실비아 풀 · 바닥판)
            if (psStopped && t > o.Until) PsFade(Mathf.Clamp01((o.Until + FADE - t) / FADE));
            if (t >= o.Cut || t >= o.Until + FADE || t >= Duration + 1.5f || t > 10f || (!alive && t > 0.05f)) Finish();
        }
        float psFadeK = 1f;
        // 걷힘 — 지난 배율 대비 줄어든 만큼 입자 시작 색 알파에 곱한다(색 곡선이 그 위에 곱해지므로 전체가 같은 비율로 옅어진다)
        void PsFade(float k)
        {
            if (k >= psFadeK) return;
            float r = psFadeK > 1e-4f ? k / psFadeK : 0f;
            psFadeK = k;
            foreach (var ps in psAll)
            {
                int n = ps.particleCount; if (n == 0) continue;
                if (psBuf.Length < n) psBuf = new ParticleSystem.Particle[n];
                n = ps.GetParticles(psBuf, n);
                bool spin = ps.rotationOverLifetime.enabled || ps.rotationBySpeed.enabled;
                // 다시 써 넣으면(SetParticles) 회전 모듈 없는 입자에 각속도가 붙어 걷히는 동안 판이 다 같이 돌았다(10-17 사용자 「디아나 대지 끝나고 회전」 · PsLog r 0 → −11 → −33)
                for (int i = 0; i < n; i++) { var c = psBuf[i].startColor; c.a = (byte)Mathf.RoundToInt(c.a * r); psBuf[i].startColor = c; if (!spin) psBuf[i].angularVelocity3D = Vector3.zero; }
                ps.SetParticles(psBuf, n);
            }
        }

        void Tick()
        {
            float dt = Mathf.Min(MAX_DT, Mathf.Max(0, BolzenaFx.DeltaTime())) * BolzenaFx.Rate;
            if (psInst != null) { PsTick(dt); return; }
            fade = Mathf.Clamp01((o.Until - t) / FADE);
            bool busy = Step(dt);
            if (!(busy && t < Duration + 1.5f && t < o.Until && t < o.Cut))
            {
                Finish();
                return;
            }
            UpdateOrigin();
            Build();
        }

        void Finish()
        {
            if (!IsDone) Running = Mathf.Max(0, Running - 1);
            IsDone = true;
            if (psInst != null) { Destroy(psInst); psInst = null; psAll = psRoots = null; }
            if (ems != null) foreach (var em in ems) { Live -= em.Parts.Count; foreach (var q in em.Parts) pool.Push(q); em.Parts.Clear(); }
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
            fitK = 1; fitDown = 0;
            moving = false; ems = null; matCount = -1;
            if (psInst != null) { Destroy(psInst); psInst = null; psAll = psRoots = null; }
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
            // 원작 파티클(psInst)은 따라가지 않는다 — 터진 자리에 고정(10-17 사용자 「전체적으로 임팩트를 고정해야 되는데 사도를 중심으로 움직인다」 · 디아나 대지 · 티그 고학년).
            //   첫 자리만 Track 으로 잡는다(InitPs 의 첫 PsPlace 때 t == 0)
            //   단 뼈에 붙인 장(Bare — 손 · 총구 · 몸 가운데)은 계속 따라간다(10-17 사용자 「네르 고학년 손의 보석이 허공에 고정」)
            if (o.Track != null && (psInst == null || t <= 0f || o.Bare)) { var p = o.Track(); if (p.HasValue) origin = p.Value + psShift; }
            else if (moving)
            {
                float md = o.MoveDur > 0 ? o.MoveDur : 0.35f, mb = o.MoveBackDur > 0 ? o.MoveBackDur : md;
                float f = Mathf.Clamp01((t - o.MoveDelay) / md);
                // 왕복 — 닿고 MoveBack 초 쉰 뒤 돌아온다(MoveBackDur 초, 없으면 같은 길이)
                if (o.MoveBack > 0 && t > o.MoveDelay + md + o.MoveBack) f = Mathf.Max(0, 1 - (t - o.MoveDelay - md - o.MoveBack) / mb);
                float e = o.MoveLinear ? f : f * f * (3 - 2 * f);
                origin = Vector3.LerpUnclamped(from, to, e) + new Vector3(0, o.MoveArc * 4f * f * (1 - f), 0);
            }
        }

        // ── 셈 ──
        static float Rand(Vector2 r) => r.x + UnityEngine.Random.value * (r.y - r.x);

        static float C1Z(FxEmitter e, float t) => e.C1z != null && e.C1z.Length > 0 ? KeyAt(e.C1z, t) : 0f;
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

        // 추출기의 view 와 같은 20도 카메라 투영. 월드 모듈에는 이미터 행렬을 곱하지 않는다.
        const float VIEW_COS = 0.9396926f, VIEW_SIN = 0.3420201f;
        static Vector2 WorldView(Vector3 v) => new Vector2(v.x, VIEW_COS * v.y + VIEW_SIN * v.z);
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
                p.OX = origin.x; p.OY = origin.y;
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
                    if (a.HasSf) p.Sf = Mathf.FloorToInt(Rand(a.Sf) * F);
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
                    em.Acc += e.Rate * Density * dt;
                    int n = Mathf.FloorToInt(em.Acc);
                    em.Acc -= n;
                    if (n > 0) Spawn(em, n);
                }
                foreach (var b in em.Bursts)
                {
                    while (b.Left > 0 && te >= b.T)
                    {
                        float bn = b.N >= 1 ? Mathf.Max(1f, b.N * Density) : b.N;
                        int n = Mathf.FloorToInt(bn) + (UnityEngine.Random.value < bn % 1 ? 1 : 0);
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

        // 솟고 멈추는 수명 속도(2026-10-14 사용자 「실비아는 은분수 계속 올라가는 거」) — 원작 수명 속도는 곡선(처음에만 솟음)인데 추출이 평균 하나(상수)만 남겨
        //   수명 내내 올라갔다. 실비아 고학년 석상(FX_Silvia_Statue_1 — Pos y −6.43 · 속도 y 27.8 · 수명 2.88초)은 0.23초에 바닥(0)까지 솟고 그 자리에 선다
        //   (원작 영상 0.6~0.7초 솟음 → 1.4초 물줄기 → 2.5초까지 제자리에서 사라짐). 그림 경로 낱말 → 속도를 끄는 나이(초)
        static readonly (string tex, float sec)[] VEL_STOP = { ("FX_Silvia_Statue", 0.23f) };
        static readonly Dictionary<FxEmitter, float> velStop = new Dictionary<FxEmitter, float>();
        static float VelStop(FxEmitter e)
        {
            if (velStop.TryGetValue(e, out var s)) return s;
            s = float.MaxValue;
            if (e.TexPath != null) foreach (var v in VEL_STOP) if (e.TexPath.IndexOf(v.tex, StringComparison.OrdinalIgnoreCase) >= 0) { s = v.sec; break; }
            return velStop[e] = s;
        }

        static void Move(FxEmitter e, P p, float dt)
        {
            if (e.Grav != 0) p.Vy -= e.Grav * VIEW_COS * dt;
            if (e.HasForce)
            {
                var F = e.ForceW ? WorldView(e.Force) : Proj(e.HasMt ? e.Mt : e.M, e.Force);
                p.Vx += F.x * dt; p.Vy += F.y * dt;
            }
            if (e.Drag != 0) { float d = Mathf.Exp(-e.Drag * dt); p.Vx *= d; p.Vy *= d; }
            // 수명 속도는 매 프레임 더해지는 몫이라 쌓지 않는다. 속도 제한은 둘을 합친 것에 건다
            var V = e.HasVel && !(p.Age > VelStop(e)) ? (e.VelW ? WorldView(e.Vel) : Proj(e.HasMt ? e.Mt : e.M, e.Vel)) : Vector2.zero;
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
        float curEdge = EDGE;   // 지금 쌓는 이미터의 가장자리 폭(BuildFx 가 Em.Edge 로 바꿔 끼운다)
        bool curFlipV;          // 지금 쌓는 이미터의 세로 뒤집기(Em.FlipV)
        bool curRotLocal;       // 지금 쌓는 이미터 — 늘인 뒤에 돌리기(Em.RotLocal)
        bool curArc;            // 지금 쌓는 이미터 — 호로 휘기(Em.Arc)
        float curArcScale = 3f;
        // 길쭉한 판(그린 가로 · 세로 비가 이만큼 넘는 것)은 타원으로 걷는다 — 칸 끝까지 찬 빛기둥 · 세로판의 끝이 직선으로 잘려 보이던 것(2026-10-13 네르(빡침) 흰 빛기둥 · 반달 얼룩)
        const float LONG_QUAD = 3f, LONG_EDGE = 0.15f;

        void BeginBuild()
        {
            verts.Clear(); uvs.Clear(); uv1.Clear(); uv2.Clear(); cols.Clear();
            foreach (var s in subs) s.Clear();
            mats.Clear();
            curSub = -1; curTex = null;
            linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        }

        void UseTex(Texture tex, Texture mask = null, Material sg = null)
        {
            if (curSub >= 0 && tex == curTex && mask == curMask && sg == curSg) return;
            curSub++;
            curTex = tex; curMask = mask; curSg = sg;
            if (subs.Count <= curSub) subs.Add(new List<int>());
            mats.Add(sg != null ? sg : BolzenaFx.MaterialFor(tex, mask));
        }
        Texture curMask;
        Material curSg;                 // 셰이더 그래프 재현 재질(10-15) — 있으면 uv1 = Custom1, uv2 = Custom2 를 싣는다
        Vector4 curC1, curC2;
        // 셰이더 그래프 재현을 쓸지 — 끄면 옛 판(EdgeOf · 구운 그림)
        public static bool SG_ON = true;

        // 화면식(y 아래) 꼭짓점 → 월드
        Vector3 W(float sxv, float syv)
        {
            float x = sxv * k, y = -syv * k;
            if (rs == 0) return origin + new Vector3(x, y, 0);
            return origin + new Vector3(x * rc - y * rs, x * rs + y * rc, 0);   // 투사체 돌림(Aim)
        }

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0, float v0, float u1, float v1, Color col, float add, bool edge, float lx0 = -1, float lx1 = -1)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            // 웹 그림은 윗줄이 v0 — 유니티 텍스처는 아래가 0 이라 뒤집는다
            uvs.Add(new Vector2(u0, 1 - v0)); uvs.Add(new Vector2(u1, 1 - v0)); uvs.Add(new Vector2(u1, 1 - v1)); uvs.Add(new Vector2(u0, 1 - v1));
            if (curSg != null)   // 셰이더 그래프 재현 — uv0 = 칸 uv 그대로, uv1 = Custom1, uv2 = Custom2(원작 꼭짓점 스트림)
            {
                for (int j = 0; j < 4; j++) { uv1.Add(curC1); uv2.Add(curC2); cols.Add(col); }
                var sgs = subs[curSub];
                sgs.Add(i); sgs.Add(i + 1); sgs.Add(i + 2); sgs.Add(i); sgs.Add(i + 2); sgs.Add(i + 3);
                return;
            }
            uv2.Add(new Vector2(0, 1)); uv2.Add(new Vector2(1, 1)); uv2.Add(new Vector2(1, 0)); uv2.Add(new Vector2(0, 0));   // 가리개 — 판 한 장이 가리개 한 장
            // uv1 = (칸 안의 자리 x, y, 더하기(1) · 밝기를 알파로(-1), 걷어 낼 가장자리 폭(음수 = 타원))
            if (edge) { uv1.Add(new Vector4(0, 0, add, curEdge)); uv1.Add(new Vector4(1, 0, add, curEdge)); uv1.Add(new Vector4(1, 1, add, curEdge)); uv1.Add(new Vector4(0, 1, add, curEdge)); }
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
            if (o.FitTop && o.Top.HasValue && !BolzenaFx.OriginalOnly) FitVerts(o.Top.Value);
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uv1);
            mesh.SetUVs(2, uv2);
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

        // 화면 위끝 맞추기(FitTop) — 이번 프레임 꼭짓점의 위끝이 top 을 넘으면 자리(origin) 둘레로 줄이고(FIT_MIN 까지), 그래도 넘는 몫은 내린다(FIT_DOWN 까지).
        //   배율 · 내림은 줄어들기만 해서(그 판이 끝날 때까지) 커졌다 작아졌다 하지 않는다
        void FitVerts(float top)
        {
            float hi = float.MinValue;
            for (int i = 0; i < verts.Count; i++) if (verts[i].y > hi) hi = verts[i].y;
            float span = hi - origin.y, room = top - origin.y;
            if (hi > top) fitK = Mathf.Min(fitK, span > 1e-3f && room > 0 ? Mathf.Max(FIT_MIN, room / span) : FIT_MIN);
            float over = origin.y + span * fitK - top;
            if (over > 0) fitDown = Mathf.Min(FIT_DOWN, Mathf.Max(fitDown, over));
            if (fitK >= 0.999f && fitDown <= 0) return;
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                verts[i] = new Vector3(origin.x + (v.x - origin.x) * fitK, origin.y + (v.y - origin.y) * fitK - fitDown, v.z);
            }
        }

        void BuildFx()
        {
            // 화면식 원점 이동(dx) — 웹판 runOrigin 의 sx
            float ox = flip * dx, oy = 0;
            foreach (var em in ems)
            {
                if (em.Parts.Count == 0) continue;
                var e = em.E;
                UseTex(e.Tex, e.Mask, em.Sg);
                float add = e.Add ? 1 : em.Lum ? -1 : 0;
                curEdge = em.Edge;
                curFlipV = em.FlipV;
                curRotLocal = em.RotLocal;
                curArc = em.Arc;
                curArcScale = em.ArcScale;
                curMesh = em.Mesh;
                bool edge = true;
                float f0 = fade;
                if (e.M3d && !em.Mesh) fade *= 0.6f;
                foreach (var p in em.Parts) Quad(e, p, ox, oy, add, edge);
                fade = f0;
            }
        }

        void Quad(FxEmitter e, P p, float ox, float oy, float add, bool edge)
        {
            float fl = flip, tt = p.Age / p.Life;
            float s = p.Size * KeyAt(e.SizeOL, tt) * e.SizeK;
            float cx = (ox + fl * p.X) * spread, cy = oy - p.Y;
            if (e.SimW && k > 1e-6f) { cx += (p.OX - origin.x) / k; cy -= (p.OY - origin.y) / k; }   // 월드 시뮬 — 따라가는 원점에서 떼어 방출 자리에
            // 추출기의 pivot 은 R 로 변환 · 투영된 오프셋이고, 세로 크기 비율(sy)도 추출 때 곱해 둔다(10-15 추출 보강 — fx_extract_core.py). 크기만 한 번 곱한다
            if (e.HasPivot) { cx += fl * e.Pivot.x * s; cy -= e.Pivot.y * s; }
            float ux, uy, vx, vy;
            if (e.HasQuad)
            {
                var q = e.Quad;
                float cr = Mathf.Cos(p.Rot), sr = Mathf.Sin(p.Rot);
                float ax = q.x * cr + q.z * sr, ay = q.y * cr + q.w * sr, bx = -q.x * sr + q.z * cr, by = -q.y * sr + q.w * cr;
                if (curRotLocal) { ax = cr * q.x - sr * q.y; ay = sr * q.x + cr * q.y; bx = cr * q.z - sr * q.w; by = sr * q.z + cr * q.w; }   // 늘인 판을 돌린다(ROT_LOCAL_BY_ART)
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
                // 수평 판은 XZ 평면에서 회전한 뒤 투영한다. 화면에서 납작한 판을 돌리면 바닥에서 일어선다.
                float ang = -p.Rot * fl;
                if (e.HasStretch)
                {
                    float vsx = fl * p.Lvx, vsy = -p.Lvy, v = Mathf.Sqrt(vsx * vsx + vsy * vsy);
                    float len = w * e.Stretch.x + v * e.Stretch.y;
                    ang = v > 1e-4f ? Mathf.Atan2(vsy, vsx) : 0;
                    h = w; w = len;   // 0 길이는 사라지고 음수 길이는 축이 뒤집힌다. 임의의 최소 길이를 넣지 않는다.
                }
                float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                ux = c * w / 2; uy = sn * w / 2; vx = -sn * h / 2; vy = c * h / 2;
                if (e.Flat) { uy *= VIEW_SIN; vy *= VIEW_SIN; }
                if (e.HasSc && e.Sc.x * fl < 0) { ux = -ux; uy = -uy; }
            }
            if (curFlipV) { vx = -vx; vy = -vy; }
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
            if (curSg != null)
            {
                curC1 = e.HasC1 ? new Vector4(KeyAt(e.C1x, tt), KeyAt(e.C1y, tt), C1Z(e, tt), KeyAt(e.C1w, tt)) : new Vector4(0, 0, 0, 1);
                curC2 = e.C2x != null && e.C2x.Length > 0 ? new Vector4(KeyAt(e.C2x, tt), KeyAt(e.C2y, tt), KeyAt(e.C2z, tt), KeyAt(e.C2w, tt)) : Vector4.zero;   // Custom2 가 없으면 0(원작 꼭짓점 스트림 빈 칸) — (1,1,0,1) 이면 툰 폭발이 노랗게 칠해졌다(10-16 시온 스킬)
            }
            float boost = BolzenaFx.OriginalOnly ? 1f : e.Add ? BolzenaFx.AddBoost : BolzenaFx.AlphaBoost;
            var col4 = Lin(p.C.r * C.x * boost, p.C.g * C.y * boost, p.C.b * C.z * boost, alpha);
            float lu = ux * ux + uy * uy, lv = vx * vx + vy * vy, ce = curEdge;
            if (curEdge >= 0 && Mathf.Min(lu, lv) > 0 && Mathf.Max(lu, lv) > LONG_QUAD * LONG_QUAD * Mathf.Min(lu, lv)) curEdge = -Mathf.Max(curEdge, LONG_EDGE);
            if (curMesh)   // (10-15) 휜 호 흉내(ARC_BY_ART)보다 먼저 — 원작 메시 몸 — 판 대신 삼각형 그대로(휘는 칼빛 · 고리). 판과 같은 크기 · 돌림 · 뒤집기
            {
                float mcr = Mathf.Cos(p.Rot), msr = Mathf.Sin(p.Rot), cu = u1 - u0, cv = v1 - v0;
                // 사용자 데이터 Custom1 — 주 그림 UV 밀기(칼빛이 그어지듯 드러남) · 밝기 배율(원작 HDR — 파란 색 × 15 가 흰빛). 셰이더 그래프 FX_Master
                float offU = 0, offV = 0;
                if (e.HasC1 && curSg == null)
                {
                    offU = KeyAt(e.C1x, tt); offV = KeyAt(e.C1y, tt);
                    float inten = Mathf.Max(0, KeyAt(e.C1w, tt));
                    col4 = new Color(col4.r * inten, col4.g * inten, col4.b * inten, col4.a);
                }
                int b0 = verts.Count;
                bool hasA = e.MeshA != null && e.MeshA.Length == e.MeshV.Length;   // 유니티는 null 배열을 길이 0 으로 저장한다
                for (int i = 0; i < e.MeshV.Length; i++)
                {
                    var mv = e.MeshV[i];
                    var mlv = new Vector3(mv.x * mcr - mv.y * msr, mv.x * msr + mv.y * mcr, mv.z);   // 메시 평면(z 축)에서 돌린 뒤
                    float rx = Vector3.Dot(e.MeshAx, mlv), ry = Vector3.Dot(e.MeshAy, mlv);              // 화면에 떨군다(view ∘ R)
                    verts.Add(W(cx + fl * rx * s, cy - ry * s));
                    var mu = e.MeshUV[i];
                    float uu = p.Fu ? 1 - mu.x : mu.x;
                    uvs.Add(new Vector2(u0 + (uu + offU) * cu, (1 - v1) + (mu.y + offV) * cv));
                    if (curSg != null) { uv1.Add(curC1); uv2.Add(curC2); }   // 셰이더 그래프 재현 — UV 밀기 · 밝기는 셰이더가
                    else
                    {
                        uv2.Add(new Vector2(uu, mu.y));   // 가리개는 밀지 않은 메시 UV(원작 _Mask_Tex 는 제자리)
                        uv1.Add(new Vector4(EDGE_OFF, EDGE_OFF, add, EDGE));
                    }
                    var mc = col4; if (hasA) mc.a *= e.MeshA[i];
                    cols.Add(mc);
                }
                var ms = subs[curSub];
                for (int i = 0; i < e.MeshT.Length; i++) ms.Add(b0 + e.MeshT[i]);
                curEdge = ce;
                return;
            }
            if (curArc)
            {
                // 호 — 쐐기 그림은 세로(v)가 호를 따라가는 방향(위 = 넓은 머리, 아래 = 가는 꼬리), 가로(u)가 두께(왼쪽 곧은 변 = 바깥 호, 오른쪽 휜 변 = 안쪽).
                //   판 가운데를 중심으로, 판의 세로 축 쪽이 호 가운데. 반지름은 판 반 폭 × ARC_SCALE(원작 셰이더가 판보다 크게 펼친다)
                float lenU = Mathf.Sqrt(lu), lenV = Mathf.Sqrt(lv);
                if (lenU > 1e-5f && lenV > 1e-5f)
                {
                    float r1 = Mathf.Max(lenU, lenV) * curArcScale, r0 = r1 * ARC_INNER;
                    float acx = cx, acy = cy - r1 * ARC_LIFT;   // (화면식 y 는 아래로 커진다 — 빼야 위)   // 원작은 몸통 높이에서 몸을 감싼다 — 발밑 원점이면 호가 땅 아래로 처졌다
                    var acol = new Color(Mathf.Lerp(col4.r, 1f, ARC_WHITE), Mathf.Lerp(col4.g, 1f, ARC_WHITE), Mathf.Lerp(col4.b, 1f, ARC_WHITE), col4.a);
                    // 호 가운데는 늘 사도가 보는 쪽(앞) — 판이 돌아가 있으면 판 축이 뒤를 가리켜 호가 몸 뒤로 갔다(실비아)
                    float Ux = fl, Uy = 0, Vx = 0, Vy = -1;
                    const int N = 14; float A = ARC_DEG * Mathf.Deg2Rad;
                    for (int i = 0; i < N; i++)
                    {
                        float f0 = (float)i / N, f1 = (float)(i + 1) / N;
                        float a0 = -A / 2 + A * f0, a1 = -A / 2 + A * f1;
                        // 호 가운데는 판의 가로 축(앞 — 적 쪽): 사도 앞에서 「)」 로 휘두르는 베기
                        float d0x = Mathf.Cos(a0) * Ux + Mathf.Sin(a0) * Vx, d0y = Mathf.Cos(a0) * Uy + Mathf.Sin(a0) * Vy;
                        float d1x = Mathf.Cos(a1) * Ux + Mathf.Sin(a1) * Vx, d1y = Mathf.Cos(a1) * Uy + Mathf.Sin(a1) * Vy;
                        float va = Mathf.Lerp(v0, v1, f0), vb = Mathf.Lerp(v0, v1, f1);
                        // AddQuad 모서리 차례 = (u0,va) (u1,va) (u1,vb) (u0,vb) — u0 바깥(r1) · u1 안쪽(r0)
                        AddQuad(W(acx + d0x * r1, acy + d0y * r1), W(acx + d0x * r0, acy + d0y * r0), W(acx + d1x * r0, acy + d1y * r0), W(acx + d1x * r1, acy + d1y * r1),
                                u0, va, u1, vb, acol, 1f, false);   // 빛 더하기(원작 셰이더 그래프 베기는 빛나는 흰빛)
                    }
                }
                curEdge = ce;
                return;
            }
            AddQuad(W(cx - ux - vx, cy - uy - vy), W(cx + ux - vx, cy + uy - vy), W(cx + ux + vx, cy + uy + vy), W(cx - ux + vx, cy - uy + vy),
                    u0, v0, u1, v1, col4, add, edge);
            curEdge = ce;
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
