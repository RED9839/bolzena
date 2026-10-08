using System;
using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using UnityEngine.InputSystem;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 전투 화면의 감독 — 규칙(IBattle)이 내준 이벤트를 받아 차례대로 연출한다.
    // 장면은 전부 코드로 만든다: 싸움터(Field — 흔들림 · 줌을 탄다) · 화면 덮개(Screen) · UI(손패 · 파티 막대 · 툴팁 · 창).
    public partial class BattleDirector : MonoBehaviour
    {
        public static BattleDirector I;
        public IBattle Battle;
        public Camera Cam;
        public Transform FieldRoot, ScreenRoot, UiRoot;
        public readonly List<UnitView> Heroes = new List<UnitView>();
        public readonly List<UnitView> Enemies = new List<UnitView>();
        public readonly List<EnemyHud> EnemyHuds = new List<EnemyHud>();
        public HandView Hand;
        public PartyHud Hud;
        public bool WaitingInput { get; private set; }
        /// <summary>턴 종료를 누른 뒤 적 차례가 끝날 때까지(턴 종료 단추가 「적 차례」 로 잠긴다).</summary>
        public bool EnemyTurn { get; private set; }
        public bool Over { get; private set; }
        public event Action<string> Moment;                 // 「hit」 · 「crit」 · 「break」 … 연출의 고비(자동 데모가 캡처)
        public int DemoEpiphanyPick = -1;
        public int DemoBranchPick = -1;
        public int DemoSpendPick = -1;                      // 소모량 고르기 창 — 자동 데모가 이 번째(0~) 후보를 고른다
        public bool Auto;                                   // 자동 전투(위 오른쪽 토글)
        public int UltSel { get; private set; } = -1;       // 고른 고학년(사도 번호)
        public int UltAim { get; private set; } = -1;

        enum ReqKind { Card, Ult, End }
        (ReqKind kind, int a, int b)? request;
        string skipPlayed;                                  // 손에서 직접 낸 카드 — 뒤따르는 CardPlayed 는 이미 그렸다

        // 사도 · 적 자리(싸움터 좌표 — 발)
        static readonly Vector3[] HeroPos = { new Vector3(-1.55f, -0.72f, 0), new Vector3(-3.2f, -0.32f, 0), new Vector3(-4.85f, -0.78f, 0) };
        // 사도 셋 간격 고르기(2026-10-09) — 그려진 몸 너비(ArtBounds)로 셋 사이 빈 간격을 같게 · y 는 좌우 대칭 엇갈림. 앞(첫째)은 제자리, 뒤는 왼쪽으로
        IEnumerator HeroSpread()
        {
            for (int k = 0; k < 4; k++) yield return null;
            if (Heroes.Count < 2) yield break;
            var ws = Heroes.ConvertAll(h => h != null ? Mathf.Clamp(h.ArtBounds.size.x, 0.8f, 3.2f) : 1.2f);
            float left = -7.0f, gap = 0.3f;
            float x0 = HeroPos[0].x;
            for (int pass = 0; pass < 2; pass++)
            {
                float edge = x0 + ws[0] / 2;
                float x = x0;
                for (int i = 1; i < ws.Count; i++) x -= ws[i - 1] / 2 + gap + ws[i] / 2;
                float lastLeft = x - ws[ws.Count - 1] / 2;
                if (lastLeft < left) gap = Mathf.Max(0.02f, gap - (left - lastLeft) / (ws.Count - 1)); else break;
            }
            float cx = x0;
            for (int i = 0; i < Heroes.Count; i++)
            {
                if (i > 0) cx -= ws[i - 1] / 2 + gap + ws[i] / 2;
                float y = (i == 1 ? -0.5f : -0.74f);
                var pos = new Vector3(cx, y, 0);
                var u = Heroes[i]; if (u == null) continue;
                u.Home = u.Slot = pos;
                u.transform.localPosition = pos;
            }
            ResetHeroOrder();
        }

        static readonly Vector3[][] EnemyPosN =
        {
            new[] { new Vector3(3.2f, -0.75f, 0) },
            new[] { new Vector3(2.35f, -0.78f, 0), new Vector3(4.75f, -0.4f, 0) },
            new[] { new Vector3(2.1f, -0.8f, 0), new Vector3(4.1f, -0.38f, 0), new Vector3(6.1f, -0.85f, 0) },
        };
        static readonly Vector3 BossPos = new Vector3(3.9f, -0.95f, 0);

        // 적 자리 — 셋까지는 표, 넷 이상은 앞뒤로 엇갈려 고르게 편다(전에는 넷째부터 셋째 자리에 겹쳐 섰다 — 고학년 점검의 도마뱀 둘)
        static Vector3 EnemySlot(int n, int i)
        {
            if (n <= 3) return EnemyPosN[Mathf.Max(1, n) - 1][i];
            float x = 1.9f + i * (5.0f / (n - 1));
            return new Vector3(x, i % 2 == 0 ? -0.85f : -0.3f, 0);
        }
        const float UnitScale = 0.3f;
        /// <summary>적 크기 — 사도보다 살짝 작게(2026-10-09 사용자 「적군 크기 살짝 줄여줘」).</summary>
        const float EnemyShrink = 0.9f;

        void Awake()
        {
            I = this;
            // 한글 줄바꿈을 낱말(띄어쓰기) 단위로 — 카드 설명이 「피 / 해」처럼 글자 중간에서 끊기지 않게. 전투 동안만(판 화면은 제 설정 그대로)
            hangulWas = TMPro.TMP_Settings.useModernHangulLineBreakingRules;
            TMPro.TMP_Settings.useModernHangulLineBreakingRules = true;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo") >= 0) Demo.DemoGuard.Install();
            Clock.Ensure();
            Clock.Asleep = false;
            // 프레임 제한 · 수직동기는 화면 설정(DisplayOptions — 부팅 때 RunBoot 가 적용)을 따른다
            int pc = Array.IndexOf(QualitySettings.names, "PC");
            if (pc >= 0) QualitySettings.SetQualityLevel(pc, true);
        }

        void Start()
        {
            if (Demo.UltAudit.On) Demo.UltAudit.Prepare();      // 점검 차림을 전투를 만들기 전에
            StartCoroutine(Boot());
        }

        /// <summary>전투 준비가 끝났다(로딩 화면을 걷고 등장 연출로 넘어갔다).</summary>
        public bool Ready { get; private set; }

        // 전투 열기(2026-10-07 「처음 모험에 진입하면 응답 없음 · 검은 화면」) — 무거운 일을 로딩 화면 아래에서 여러 프레임에 나눈다.
        //   예전에는 Start 한 프레임에 장면 세우기 + 미리 불러 두기 + Shader.WarmupAllShaders 를 다 했다 — 웹(헤드리스 크롬 · RTX 4080)에서
        //   그 한 프레임이 9.2초(그 가운데 셰이더 전부 데우기 8.8초 · 66 셰이더 406 조합)였다.
        //   ① 스파인(사도 SD · 컷인 스탠딩 · 적)을 하나씩 읽어 파싱  ② 장면 세우기(이제 가볍다)  ③ 이펙트 · 소리 · 고학년 데우기를 사도마다
        //   ④ 쓰는 셰이더 변형만 프레임 예산 안에서 나눠 데우기(ShaderWarm)  ⑤ 로딩 화면을 걷고 Main(등장 연출)
        IEnumerator Boot()
        {
            if (!Bolzena.RunUI.LoadingScreen.Visible) Bolzena.RunUI.LoadingScreen.Show("전투 준비 중");   // 다리(BattleBridge.Enter)가 이미 띄웠으면 그대로
            const float S0 = 0.15f, A = 0.4f, B = 0.65f, C = 0.95f;   // 진행 몫 — (다리: 정리 · 장면 읽기 ~S0) 스파인 ~A · 데우기 ~B · 셰이더 ~C · 마지막 그리기 ~1
            Bolzena.RunUI.LoadingScreen.Progress(S0, "사도 · 적 그림 읽는 중");
            var fight = BattleBridge.Fight;
            if (fight != null && fight.Battle != null)
            {
                var keys = new List<string>();
                foreach (var id in fight.Battle.Fx.Party) { var art = Look.Hero(id).Art; keys.Add(art); keys.Add("st_" + art); }
                keys.AddRange(fight.Battle.SpineKeys());
                keys = new List<string>(new HashSet<string>(keys));
                for (int i = 0; i < keys.Count; i++)
                {
                    try { using (Bolzena.RunUI.Hitch.Span("스파인 읽기 · 파싱")) { var d = Res.Spine(keys[i]); if (d != null) d.GetSkeletonData(true); } }
                    catch (Exception ex) { Debug.LogWarning("[Preload] 스파인 미리 읽기 실패 " + keys[i] + " — " + ex.Message); }   // 미리 읽기는 못 해도 전투는 연다
                    Bolzena.RunUI.LoadingScreen.Progress(S0 + (A - S0) * (i + 1) / keys.Count);
                    yield return null;
                }
            }
            using (Bolzena.RunUI.Hitch.Span("전투 Build")) Build();
            if (Battle == null || Hand == null) yield break;   // -artaudit 등 Build 가 일찍 끝냄
            // 사람 실행 — 가짜 손가락은 늘 끈 채로 시작한다(데모 · 봇이 켠 채 남으면 진짜 마우스를 읽지 않는다)
            PointerInput.Simulated = false;
            PointerInput.SimHeld = PointerInput.SimRight = false;
            if (Demo.UltAudit.On) Demo.UltAudit.Attach(this);                 // 고학년 점검(-ultaudit) — 사도마다 고학년 한 번
            else if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo") >= 0)
            {
                if (BattleBridge.Fight != null)                                 // 한 판 데모 — 판에서 넘어온 싸움
                {
                    // -humanfight: 판 화면은 데모가 넘기고 싸움은 사람(또는 OS 포인터를 흉내 내는 시험 스크립트)이 한다
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "-humanfight") < 0) Demo.RunFightBot.Attach(this);
                }
                else Demo.DemoRunner.Attach(this);                              // 전투 시범(-battle)
            }
            Bolzena.RunUI.LoadingScreen.Progress(A + 0.02f, "전투 화면 세우는 중");
            yield return null;                                              // 세우기와 데우기를 다른 프레임에
            var s = Battle.Snapshot;
            var steps = PreloadSteps(s);
            for (int i = 0; i < steps.Count; i++)
            {
                try { using (Bolzena.RunUI.Hitch.Span(steps[i].name)) steps[i].run(); }
                catch (Exception ex) { Debug.LogWarning("[Preload] " + steps[i].name + " 실패 — " + ex.Message); }
                Bolzena.RunUI.LoadingScreen.Progress(A + (B - A) * (i + 1) / steps.Count, steps[i].what);
                yield return null;
            }
            // 쓰는 셰이더 변형만 — 한 프레임 예산(웹은 그리기 · 컴파일이 같은 스레드라 짧게)
            Bolzena.RunUI.LoadingScreen.Progress(B, "셰이더 준비 중");
            var mats = ShaderWarm.Pending();
            yield return ShaderWarm.Run(mats, Application.platform == RuntimePlatform.WebGLPlayer ? 60 : 40, p => Bolzena.RunUI.LoadingScreen.Progress(B + (C - B) * p));
            // 장면을 몇 프레임 그려 둔다(로딩 화면 아래) — 화면 효과 · 글꼴 · 막 세운 재질의 첫 그리기를 여기서
            for (int i = 0; i < 3; i++) { Bolzena.RunUI.LoadingScreen.Progress(C + (1 - C) * (i + 1) / 3f, "거의 다 됐습니다"); yield return null; }
            Debug.Log($"[Battle] 준비 끝 — 셰이더 변형 {mats.Count}개 데움(누적 {ShaderWarm.Warmed})");
            Bolzena.RunUI.Hitch.Mark("전투 준비 끝(등장 연출)");
            Bolzena.RunUI.LoadingScreen.Hide(0.2f);
            Ready = true;
            StartCoroutine(Main());
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-layoutshot") >= 0) StartCoroutine(LayoutShot());
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cardartsheet") >= 0) Demo.CardArtSheet.Attach(this);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cardpicsheet") >= 0) Demo.CardPicSheet.Attach(this);   // 원작 그림 카드 전후 시트(그림 자리 — CardArt.Place)
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cardtextshot") >= 0) Demo.CardTextShot.Attach(this);   // 카드 글 가독성 점검(대표 카드 확대 + 낱말 판)   // 카드 그림 시트(135명 대표 카드 — 손패 · 확대 모습)
        }

        // -layoutshot <이름> — 첫 입력 대기에서 화면을 한 장 찍고(<-captures>/<이름>_<가로>x<세로>.png) 적 자리 겹침을 적은 뒤 끝낸다(배치 점검)
        IEnumerator LayoutShot()
        {
            var a = Environment.GetCommandLineArgs();
            int ni = Array.IndexOf(a, "-layoutshot");
            string nm = ni + 1 < a.Length ? a[ni + 1] : "layout";
            int ci = Array.IndexOf(a, "-captures");
            string dir = ci >= 0 && ci + 1 < a.Length ? a[ci + 1] : System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "Captures"));
            System.IO.Directory.CreateDirectory(dir);
            PointerInput.Simulated = true;
            PointerInput.SimPos = new Vector2(0, -6);
            float t = 0;
            while (!WaitingInput && t < 40f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.6f);
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            var path = System.IO.Path.Combine(dir, $"{nm}_{Screen.width}x{Screen.height}.png");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            var items = LayoutItems();
            foreach (var it in items) { it.Pos = it.U.Home; it.Bar = it.Hud.BarPos; }
            Debug.Log($"[LayoutShot] {System.IO.Path.GetFileName(path)} — {EnemyLayout.Report(items, AliveItem)}");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(0);
        }

        // 창 크기가 바뀌면(설정 창의 해상도 · 전체화면) 카메라 높이 · 배경을 다시 맞춘다 — 21:9 는 옆이 넓게, 16:10 은 위아래가 넓게
        SpriteRenderer bgSprite;
        float lastAspect;
        void FitBg()
        {
            lastAspect = Cam.aspect;
            Cam.orthographicSize = Tone.CamSize(Cam.aspect);
            float bw = Mathf.Max(18.4f, Cam.orthographicSize * Cam.aspect * 2 + 1.2f, (Cam.orthographicSize * 2 + 1.6f) * 18.4f / 10.35f);
            Make.Fit(bgSprite, new Vector2(bw, bw * 10.35f / 18.4f));
        }

        void LateUpdate()
        {
            if (Cam != null && bgSprite != null && !Mathf.Approximately(Cam.aspect, lastAspect)) { FitBg(); layoutDirty = true; }
            // 화면 비율이 바뀌면(창 크기 · 시작 직후 창이 자리를 잡을 때) 적 자리 · 묶음을 다시 — 입력을 기다릴 때만(움직이는 중엔 미룬다)
            if (layoutDirty && WaitingInput && !EnemyHud.OldLayout && Enemies.Count > 0)
            {
                layoutDirty = false;
                LayoutEnemies();
                foreach (var u in Enemies) if (u != null) u.transform.localPosition = u.Home;
            }
        }
        bool layoutDirty;

        bool hangulWas;
        static Func<Vector3> fxCenter0;
        static Func<float?> fxTop0;
        void OnDestroy()
        {
            if (TMPro.TMP_Settings.instance != null) TMPro.TMP_Settings.useModernHangulLineBreakingRules = hangulWas;
            // 정적 자리에 남은 이 전투의 손잡이를 놓는다 — 다음 장면을 여는 정리(UnloadUnusedAssets)가 지난 싸움의 감독 · 유닛 · 스파인을
            //   「아직 쓰는 것」 으로 보고 붙들지 않게(람다가 this · FieldRoot 를 쥐고 있었다)
            if (I == this) I = null;
            if (!Ready) Bolzena.RunUI.LoadingScreen.Hide(0);   // 준비 도중 장면이 바뀌면 로딩 화면이 남지 않게
            CardView.HeroOf = null;
            BattleBridge.OnOverlay = null;
            Vfx.Field = Vfx.Screen = null;
            ScreenFx.I = null;
            PostFx.I = null;
            Tooltip.I = null;
            Bolzena.Fx.BolzenaFx.Parent = null;
            Bolzena.Fx.FxRun.ClearPool();
            if (fxCenter0 != null) Bolzena.Fx.BolzenaFx.ScreenCenter = fxCenter0;
            if (fxTop0 != null) Bolzena.Fx.BolzenaFx.TopY = fxTop0;
            CardZoom.Hide();
        }

        void Build()
        {
            Cam = Camera.main;
            Cam.orthographic = true;
            Cam.orthographicSize = Tone.CamSize(Cam.aspect);
            Cam.transform.position = new Vector3(0, 0, -10);
            Cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            PostFx.Create();

            FieldRoot = Make.Node("Field", null);
            FieldRoot.gameObject.AddComponent<FieldRig>();
            ScreenRoot = Make.Node("Screen", null);
            UiRoot = Make.Node("UI", null);
            Vfx.Field = FieldRoot;
            // 원작 이펙트(com.bolzena.fx) — 싸움터 아래에 · 싸움터 좌표로. 소리는 이 전투의 Sfx 가 낸다(두 번 나지 않게 패키지 소리는 끈다)
            Bolzena.Fx.BolzenaFx.Parent = FieldRoot;
            Bolzena.Fx.BolzenaFx.Sound = false;
            Bolzena.Fx.BolzenaFx.UltDrop = UltDropPart;
            mirror = null; mirrorLeft = 0; sleeping.Clear();   // 원작 방식 고학년 — 발동 때 틀 장은 시전 때 빼기
            Bolzena.Fx.BolzenaFx.Calm = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-nofx") >= 0;   // 원작 이펙트 끄고 재 보기(점검)
            Bolzena.Fx.BolzenaFx.UltOrder = 325; Bolzena.Fx.BolzenaFx.CardOrder = 150; Bolzena.Fx.BolzenaFx.CommonOrderAdd = -150;
            fxCenter0 ??= Bolzena.Fx.BolzenaFx.ScreenCenter;
            fxTop0 ??= Bolzena.Fx.BolzenaFx.TopY;
            Bolzena.Fx.BolzenaFx.ScreenCenter = () => FieldRoot ? FieldRoot.InverseTransformPoint(Camera.main ? Camera.main.transform.position : Vector3.zero) : Vector3.zero;
            Bolzena.Fx.BolzenaFx.TopY = () => { var c = Camera.main; if (!FieldRoot || !c || !c.orthographic) return null; return FieldRoot.InverseTransformPoint(c.transform.position + new Vector3(0, c.orthographicSize, 0)).y - Bolzena.Fx.FxRules.TOP_PAD; };
            Vfx.Screen = ScreenRoot;

            // 배경 — 흔들림 · 줌에 모자라지 않게 화면보다 크게
            var run = BattleBridge.Fight;
            var bg = Make.Sprite("bg", FieldRoot, BattleBridge.BgSprite(run?.Ticket.Bg) ?? Res.Sprite("Bg/stage3_2"), new Vector3(0, 0.4f, 0), 0);
            bgSprite = bg;
            FitBg();
            // 바닥 쪽을 살짝 어둡게(유닛이 떠 보이게) · 위쪽 빛
            Make.Box("floorShade", FieldRoot, Res.UI("soft"), new Vector3(0, -3.6f, 0), new Vector2(30, 6f), 2, new Color(0, 0, 0, 0.55f));
            Make.Box("sky", FieldRoot, Res.UI("soft"), new Vector3(-2, 4.2f, 0), new Vector2(22, 5f), 2, new Color(1f, 0.9f, 0.7f, 0.18f), Res.SpriteMat(true, 1.1f));
            StartCoroutine(Motes());

            ScreenFx.Create(ScreenRoot);
            Tooltip.Create(UiRoot);
            if (run != null) Battle = run.Battle;                  // 판에서 연 싸움(Run.OpenFight)
            else
            {
                var data = CoreBattle.LoadData();
                Bolzena.Demo.EngineDemo.Install();   // -enginedemo: 소모량 고르기 · 다음 카드 강화 시험 카드(메모리에서만)
                var cb = new CoreBattle(data, CoreBattle.Fixture.Override?.Invoke(data) ?? CoreBattle.Fixture.FromArgs(data) ?? CoreBattle.Fixture.Pilot(data));
                Battle = cb;
                if (cb.Fx.Note != null) Debug.Log("[Pilot] " + cb.Fx.Note);
                var fbg = BattleBridge.BgSprite(cb.Fx.Bg);
                if (fbg != null) { bg.sprite = fbg; FitBg(); }
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-artaudit") >= 0) { Look.Audit(); Application.Quit(0); return; }   // 그림 감사만 하고 끝
            var evs0 = Battle.Begin();
            pendingBegin = evs0;
            var s = Battle.Snapshot;
            for (int i = 0; i < s.Heroes.Count; i++)
            {
                var h = s.Heroes[i];
                UnitView u;
                using (Bolzena.RunUI.Hitch.Span("사도 유닛 세우기")) u = UnitView.Create(FieldRoot, "hero_" + h.Key, h.Key, "Normal", true, UnitScale, HeroPos[i], 40 + i * 2);
                u.Ref = UnitRef.Party(i);
                u.FxKey = h.Id;
                Heroes.Add(u);
            }
            // 앞줄이 위에 오게(발이 낮을수록 앞)
            ResetHeroOrder();
            StartCoroutine(HeroSpread());

            CardView.HeroOf = i => { var hs = Battle.Snapshot.Heroes; return i >= 0 && i < hs.Count ? hs[i] : null; };
            using (Bolzena.RunUI.Hitch.Span("손패 세우기")) Hand = HandView.Create(UiRoot);
            Hand.EnemyAt = EnemyAt;
            Hand.EnemyNear = NearestEnemy;
            Hand.EnemyAim = i => FieldRoot.TransformPoint(Enemies[i].Center);
            Hand.NextEnemy = NextEnemy;
            Hand.EnemyCount = () => Enemies.Count;
            Hand.AllyCount = () => Heroes.Count;
            Hand.EnemyFoot = i => i >= 0 && i < Enemies.Count && Enemies[i] != null && !Dead(i) ? (FieldRoot.TransformPoint(Enemies[i].Feet), Enemies[i].ArtBounds.size.x) : ((Vector3, float)?)null;
            Hand.AllyFoot = i => i >= 0 && i < Heroes.Count && Heroes[i] != null ? (FieldRoot.TransformPoint(Heroes[i].Feet), Heroes[i].ArtBounds.size.x * 0.8f) : ((Vector3, float)?)null;
            Hand.AllyAt = HeroAt;
            Hand.CanPlay = c =>
            {
                int idx = Hand.Cards.FindIndex(v => v.Info == c);
                return idx >= 0 && Battle.CanPlay(idx, out _);
            };
            Hand.WhyNot = i => Battle.CanPlay(i, out var why) ? null : why;
            using (Bolzena.RunUI.Hitch.Span("HUD 세우기")) Hud = PartyHud.Create(UiRoot, s);
            Hud.SetAp(s.Ap, s.MaxAp);
            Hud.OnPile = OpenPile;
            Hud.OnSpeed = ToggleSpeed;
            Hud.OnPause = OpenPause;
            Hud.OnHero = i => OpenHeroInfo(i, Hud.PortraitRect(i));
            Hud.OnParty = () => OpenParty(1);
            Hud.OnAuto = () => { Auto = !Auto; Hud.SetAuto(Auto); Sfx.Play("ui_click", 0.5f); };
            Hud.SetSpeed(Clock.Speed);
        }

        // 미리 불러 두기 — 고학년 · 보스 등장에서 처음 쓰는 것(이펙트 · 컷인 스탠딩 스파인 · 목소리 · 보스 스파인)을 시작할 때.
        //   한 단계 = 한 프레임(Boot 가 사이사이 yield) — 사도 · 적 하나씩. 셰이더는 Boot 가 ShaderWarm 으로 따로(예전 WarmupAllShaders 대신)
        List<(string name, string what, Action run)> PreloadSteps(BattleSnapshot s)
        {
            var o = new List<(string, string, Action)>();
            // 원작 이펙트 — 공용(맞음 · 회복 …)은 첫 사도와 함께, 그다음 사도마다 고학년 · 카드
            foreach (var h in s.Heroes) { var id = h.Id; o.Add(("이펙트 데우기", "이펙트 준비 중", () => Bolzena.Fx.BolzenaFx.Prewarm(new[] { id }, sounds: false))); }
            o.Add(("몸짓 · 소리 목록", "사도 준비 중", () => { foreach (var hv in Heroes) { hv.PrewarmTravel(); HeroClips(hv.name.Replace("hero_", "")); } }));   // 고학년 순간에 재지 않게(몸짓 이동 · 소리 목록)
            o.Add(("고학년 데우기", "고학년 준비 중", () => PrewarmUlt(s)));
            foreach (var h in s.Heroes) { var key = h.Key; o.Add(("소리 미리 읽기", "목소리 준비 중", () => { Sfx.Preload(key); foreach (Motion m in Enum.GetValues(typeof(Motion))) { HeroSfx(key, m, true); HeroSfx(key, m, false); } })); }
            var keys = new List<string>();
            foreach (var h in s.Heroes) keys.Add("st_" + h.Key);
            if (Battle is CoreBattle cb) keys.AddRange(cb.SpineKeys());
            foreach (var k in new HashSet<string>(keys))
                o.Add(("스파인 미리 세우기", "적 준비 중", () =>
                {
                    var data = Res.Spine(k);
                    if (data == null) return;
                    // 한 번 세워 그려 둔다 — 아틀라스 텍스처 · 재질이 GPU 에 올라가게(ShaderWarm 이 이 재질도 데운다)
                    var sa = Spine.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                    sa.transform.position = new Vector3(0, -40, 0);
                    sa.Update(0);
                    sa.LateUpdate();
                    Destroy(sa.gameObject, 0.5f);
                }));
            o.Add(("Vfx.Preload", "효과 준비 중", () => Vfx.Preload()));
            return o;
        }

        // 고학년 첫 순간의 몫을 싸움 시작으로 — 2026-10-05 점검(-ultaudit -ultprobe): 프로세스 첫 고학년의 SD 시작 프레임이 40.7ms,
        //   그 가운데 PlanUlt 23ms(ult_motion.json 670KB 를 처음 읽고 푸는 일 · 9MB 할당) · OrigFx 7.5ms(고학년 이펙트 경로 첫 실행 JIT).
        //   갈래 0 으로 계획만 세우고(난수를 쓰지 않는다), 이펙트는 화면 밖에서 한 번 틀자마자 걷는다
        void PrewarmUlt(BattleSnapshot s)
        {
            bool fxWarm = false;
            for (int i = 0; i < Heroes.Count && i < s.Heroes.Count; i++)
            {
                var u = Heroes[i];
                if (u == null || !u.SpineArt || u.SkelData == null) continue;
                try
                {
                    var plan = Bolzena.Fx.SpineMotion.PlanUlt(u.SkelData, s.Heroes[i].Id, 0);
                    if (fxWarm || plan == null || plan.Anim == null || !Bolzena.Fx.FxLibrary.HasUlt(s.Heroes[i].Id)) continue;
                    fxWarm = true;
                    var far = new Bolzena.Fx.FxActor { FeetAt = () => new Vector3(1e5f, 1e5f, 0), Party = true, Key = u.Fx.Key };
                    var foe = new Bolzena.Fx.FxActor { FeetAt = () => new Vector3(1e5f + 3, 1e5f, 0) };
                    var call = Bolzena.Fx.BolzenaFx.Ult(s.Heroes[i].Id, far, new List<Bolzena.Fx.FxActor> { foe }, null, Bolzena.Fx.SpineFx.Sync(plan), false, null);
                    call?.Stop(0);
                }
                catch (Exception ex) { Debug.LogWarning("[Preload] 고학년 데우기 실패 " + s.Heroes[i].Id + " — " + ex.Message); }
            }
        }

        IReadOnlyList<BattleEvent> pendingBegin;

        // 적 그리는 차례 = 화면 앞뒤(발 y) — 아래(앞줄)에 선 적이 위(뒷줄)에 선 적보다 늘 앞에. 넷 이상 엇갈려 서면(2 4 / 1 3)
        //   번호 순으로 그리던 때는 뒷줄 2 가 앞줄 1 · 3 위에 겹쳤다. 보스 · 소환 · 쓰러진 자리도 같은 규칙(y 가 같으면 번호 순).
        //   적 몸은 22~38(사도 40~46 아래) — HP 막대 · 강인도 · 칩 · 의도(EnemyHud 430~)는 그대로 모든 몸 위
        const int EnemyOrderBase = 22, EnemyOrderMax = 38;
        void ResetEnemyOrder()
        {
            var idx = new List<int>();
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null) idx.Add(i);
            idx.Sort((a, b) => { int c = Enemies[b].Home.y.CompareTo(Enemies[a].Home.y); return c != 0 ? c : a.CompareTo(b); });
            for (int r = 0; r < idx.Count; r++)
            {
                var u = Enemies[idx[r]];
                u.BaseOrder = Mathf.Min(EnemyOrderMax, EnemyOrderBase + r * 2);
                if (!Dead(idx[r])) u.SetOrder(u.BaseOrder);
            }
        }

        void ResetHeroOrder()
        {
            int[] o = { 46, 42, 44 };
            for (int i = 0; i < Heroes.Count; i++) Heroes[i].SetOrder(o[Mathf.Min(i, 2)]);
        }

        // 떠다니는 빛 티끌 — 숲의 공기
        IEnumerator Motes()
        {
            while (true)
            {
                Vfx.Burst(new Vector3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-2.5f, 3f), 0), new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Glow", Count = 1, Speed = new Vector2(0.1f, 0.35f), Angle = 80, Spread = 60, Life = new Vector2(3f, 5f),
                    Size = new Vector2(0.05f, 0.14f), C0 = new Color(1f, 0.95f, 0.7f, 0.7f), C1 = new Color(0.8f, 1f, 0.8f, 0.5f), Order = 5, Boost = 1.6f, ShrinkTo = 0.6f,
                });
                yield return Clock.Wait(0.25f);
            }
        }

        // 적 판정 상자(월드) — 그림(스파인 메시 경계)을 넉넉히 덮는다. 예전 상자(폭 45% · 발 아래 0.4)는 그림보다 작아
        // 적 가장자리 · 머리 위에 놓으면 「클릭이 안 먹는」 것처럼 보였다
        public static bool OldEnemyBox = Array.IndexOf(Environment.GetCommandLineArgs(), "-oldbox") >= 0;
        public Rect EnemyBox(int i)
        {
            var e = Enemies[i];
            var p = FieldRoot.TransformPoint(e.Feet);
            if (OldEnemyBox) { float ow = e.Width() * 0.45f + 0.3f; return new Rect(p.x - ow, p.y - 0.4f, ow * 2, e.Height() + 0.4f); }   // 고치기 전 상자(-oldbox — 비교용)
            // 그림의 실제 경계(스파인 메시) + 여유 0.3, 발 둘레 최소 상자와 합친다(그림이 아주 작거나 아직 안 그려졌을 때)
            var b = e.ArtBounds;
            float minW = 0.9f;
            float x0 = Mathf.Min(b.min.x - 0.3f, p.x - minW), x1 = Mathf.Max(b.max.x + 0.3f, p.x + minW);
            float y0 = Mathf.Min(b.min.y - 0.3f, p.y - 0.6f), y1 = Mathf.Max(b.max.y + 0.3f, p.y + 1.6f);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        int EnemyAt(Vector2 world)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < Enemies.Count; i++)
            {
                var e = Enemies[i];
                if (e == null || Dead(i)) continue;
                var r = EnemyBox(i);
                if (!r.Contains(world)) continue;
                float d = Mathf.Abs(world.x - r.center.x);          // 상자가 겹치면 가운데가 가까운 적
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        // 적 상자 밖에 놓았을 때 — 싸움터 위쪽(손 위)이면 가장 가까운 산 적으로 붙는다(대상 카드를 놓았는데 아무 일도 없는 일이 없게)
        int NearestEnemy(Vector2 world)
        {
            int best = -1;
            float bestD = 3.2f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i] == null || Dead(i)) continue;
                float d = Vector2.Distance(world, EnemyBox(i).center);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        int HeroAt(Vector2 world)
        {
            for (int i = 0; i < Heroes.Count; i++)
            {
                var u = Heroes[i];
                var p = FieldRoot.TransformPoint(u.Feet);
                float w = Mathf.Min(1.1f, u.Width() * 0.35f), h = Mathf.Min(2.4f, u.Height());
                if (world.x > p.x - w && world.x < p.x + w && world.y > p.y - 0.2f && world.y < p.y + h) return i;
            }
            return -1;
        }

        bool Dead(int i)
        {
            var s = Battle.Snapshot;
            return i >= s.Enemies.Count || s.Enemies[i].Dead;
        }

        public int FirstAliveEnemy()
        {
            var s = Battle.Snapshot;
            for (int i = 0; i < s.Enemies.Count; i++) if (!s.Enemies[i].Dead) return i;
            return 0;
        }

        int NextEnemy(int cur, int dir)
        {
            var s = Battle.Snapshot;
            int n = s.Enemies.Count;
            if (n == 0) return -1;
            int i = cur < 0 ? (dir > 0 ? -1 : n) : cur;
            for (int k = 0; k < n; k++)
            {
                i = ((i + dir) % n + n) % n;
                if (!s.Enemies[i].Dead) return i;
            }
            return -1;
        }

        void Emit(string m) => Moment?.Invoke(m);

        // ── 흐름 ──
        IEnumerator Main()
        {
            var evs = pendingBegin;
            yield return Intro();
            yield return Present(evs);
            while (!Over)
            {
                request = null;
                Hand.Request = null;
                Hand.Interactive = true;
                WaitingInput = true;
                Bolzena.RunUI.Hitch.FirstInput();
                Hud.SetEndReady(!AnyPlayable());
                float autoT = 0;
                while (request == null && !Over)
                {
                    if (Auto && Modal.Open == null && !Hand.HasSelection && (autoT += Time.unscaledDeltaTime) > 0.45f / Mathf.Max(1, Clock.Speed)) { request = AutoPick(); autoT = 0; if (request != null) break; }
                    if (Hand.Request != null) { request = (ReqKind.Card, Hand.Request.Value.hand, Hand.Request.Value.target); Hand.Request = null; }
                    else PollInput();
                    PaintPreview();
                    yield return null;
                }
                WaitingInput = false;
                Hand.Interactive = false;
                ClearPreview();
                SetUltSel(-1);
                Hud.SetEndReady(false);
                if (request == null) break;
                var r = request.Value;
                Debug.Log($"[Battle] 요청 {r.kind} {r.a} → {r.b}");
                if (r.kind == ReqKind.Card) yield return PlayCardFlow(r.a, r.b);
                else if (r.kind == ReqKind.Ult) yield return UltFlow(r.a, r.b);
                else { EnemyTurn = true; yield return EndTurnFlow(); EnemyTurn = false; }
            }
            // 판에서 넘어온 싸움 — 끝을 보여 준 뒤 판 화면으로 돌아간다
            if (BattleBridge.Fight != null)
            {
                yield return Clock.WaitU(Battle.Snapshot.Won ? 0.8f : 1.6f);
                // 끝 자세 위에 판 화면의 보상을 띄우는 손이 있으면 그것이 「떠나기」 까지 붙든다(runui 가 아직 안 냈으면 바로 돌아간다)
                if (BattleBridge.EndHold != null)
                {
                    BattleBridge.OnOverlay = () =>
                    {
                        Hud.gameObject.SetActive(false);   // 판 화면의 머리 띠가 파티 HP 를 맡는다
                        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-rewardoverlay") >= 0) StartCoroutine(OverlayShot());
                    };
                    yield return BattleBridge.EndHold(Battle.Snapshot.Won);
                }
                BattleBridge.Return();
            }
        }

        // 자동 전투 — 고학년(쓸 수 있으면) → 공격 카드(가장 체력 낮은 적) → 그 밖 카드 → 턴 종료
        (ReqKind kind, int a, int b)? AutoPick()
        {
            var s = Battle.Snapshot;
            int foe = -1;
            for (int i = 0; i < s.Enemies.Count; i++) if (!s.Enemies[i].Dead && (foe < 0 || s.Enemies[i].Hp < s.Enemies[foe].Hp)) foe = i;
            if (foe < 0) return null;
            for (int h = 0; h < s.Heroes.Count; h++) if (Battle.CanUlt(h, out _)) return (ReqKind.Ult, h, foe);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < Hand.Cards.Count; i++)
                {
                    var c = Hand.Cards[i].Info;
                    if (!Battle.CanPlay(i, out _) || c.Epiphany || c.Choices != null) continue;
                    if (pass == 0 && c.Type != CardType.Attack) continue;
                    return (ReqKind.Card, i, foe);
                }
            return (ReqKind.End, 0, 0);
        }

        bool AnyPlayable()
        {
            for (int i = 0; i < Hand.Cards.Count; i++) if (Battle.CanPlay(i, out _)) return true;
            return false;
        }

        // ── 입력(카드 밖) — 턴 종료 · 고학년 · 더미 · 정보 · 단축키 ──
        void PollInput()
        {
            if (Modal.Open != null) return;
            var p = PointerInput.Pos;
            Hud.EndHover = Hud.OverEnd(p) && !PointerInput.Touch;

            // 단축키
            if (PointerInput.Key(Key.Tab)) ToggleSpeed();
            if (PointerInput.Key(Key.Q)) Hud.OnAuto?.Invoke();
            if (PointerInput.Key(Key.Escape)) { if (Hand.HasSelection) Hand.Cancel(); else if (UltSel >= 0) SetUltSel(-1); else OpenPause(); return; }
            if (PointerInput.Key(Key.E)) { RequestEnd(); return; }
            if (PointerInput.Key(Key.A)) { OpenPile(0); return; }
            if (PointerInput.Key(Key.S)) { OpenPile(1); return; }
            Key[] ultKeys = { Key.Z, Key.X, Key.C };
            for (int k = 0; k < ultKeys.Length && k < Hud.Ults.Count; k++) if (PointerInput.Key(ultKeys[k])) { TapUlt(k); return; }
            if (UltSel >= 0)
            {
                if (PointerInput.Key(Key.RightArrow)) UltAim = NextEnemy(UltAim, 1);
                if (PointerInput.Key(Key.LeftArrow)) UltAim = NextEnemy(UltAim, -1);
                if (PointerInput.Key(Key.Space) || PointerInput.Key(Key.Enter)) { ConfirmUlt(); return; }
            }

            // 고른 고학년 — 마우스는 올린 적을 겨눈다
            if (UltSel >= 0 && !PointerInput.Touch) { int e = EnemyAt(p); if (e >= 0) UltAim = e; }

            // 오른쪽 클릭 · 길게 누르기 — 정보(고른 것이 있으면 내려놓기)
            if (PointerInput.RightDown || PointerInput.LongPress)
            {
                if (UltSel >= 0 && PointerInput.RightDown) { SetUltSel(-1); return; }
                if (Hand.HasSelection) return;
                int e = EnemyAt(p);
                if (e >= 0) { OpenEnemyInfo(e); return; }
                int h = HeroAt(p);
                if (h >= 0) { OpenHeroInfo(h); return; }
                for (int k = 0; k < Hud.Ults.Count; k++) if (Hud.Ults[k].Over(p)) { OpenHeroInfo(k, RectOf(Hud.Ults[k].transform, UltButton.D)); return; }
                return;
            }

            if (!PointerInput.Tap || Hand.HasSelection || Hand.Hover >= 0) return;
            if (Hud.OverEnd(p)) { Sfx.Play("ui_click", 0.6f); RequestEnd(); return; }
            for (int k = 0; k < Hud.Ults.Count; k++) if (Hud.Ults[k].Over(p)) { TapUlt(k); return; }
            int te = EnemyAt(p);
            if (UltSel >= 0)
            {
                if (te >= 0)
                {
                    if (PointerInput.Touch && UltAim != te) { UltAim = te; return; }   // 첫 탭 — 겨누기
                    UltAim = te;
                    ConfirmUlt();
                    return;
                }
                if (p.y > -1.6f && !Battle.UltNeedsTarget(UltSel)) { ConfirmUlt(); return; }
                SetUltSel(-1);
                return;
            }
            // 아무것도 안 들었을 때 적 · 사도를 누르면 정보
            if (te >= 0) { OpenEnemyInfo(te); return; }
            int th = HeroAt(p);
            if (th >= 0) OpenHeroInfo(th);
        }

        void TapUlt(int hero)
        {
            if (UltSel == hero) { ConfirmUlt(); return; }
            if (!Battle.CanUlt(hero, out var why))
            {
                Sfx.Play("card_cant", 0.5f);
                Vfx.Word(Hud.Ults[hero].transform.position + new Vector3(0, 0.9f, 0), why, 0.22f, new Color(1f, 0.75f, 0.7f), new Color(0.2f, 0, 0), 1.1f, 1f, UiRoot, 700, 0.3f);
                return;
            }
            Hand.Cancel();
            SetUltSel(hero);
            Sfx.Play("ult_ready", 0.35f, 1.3f);
        }

        void SetUltSel(int hero)
        {
            UltSel = hero;
            UltAim = hero >= 0 ? FirstAliveEnemy() : -1;
            for (int k = 0; k < Hud.Ults.Count; k++) Hud.Ults[k].Selected = k == hero;
        }

        void ConfirmUlt()
        {
            if (UltSel < 0) return;
            int t = UltAim >= 0 ? UltAim : FirstAliveEnemy();
            request = (ReqKind.Ult, UltSel, t);
        }

        void ToggleSpeed()
        {
            Clock.Speed = Clock.Speed > 1 ? 1f : 2f;
            Hud.SetSpeed(Clock.Speed);
            Sfx.Play("ui_click", 0.5f);
        }

        public void OpenPile(int which) { if (Modal.Open == null) PileView.Show(UiRoot, Battle.Snapshot, which); }
        // 정보 창 — 누른 대상(싸움터의 사도 · 적, 초상 원 · 고학년 원) 옆에 붙는다
        public void OpenHeroInfo(int i, Rect? at = null) { if (Modal.Open == null && i >= 0 && i < Battle.Snapshot.Heroes.Count) PartyView.Show(UiRoot, Battle.Snapshot, 0, i); }
        public void OpenParty(int tab = 1) { if (Modal.Open == null) PartyView.Show(UiRoot, Battle.Snapshot, tab, -1); }
        public void OpenEnemyInfo(int i) { if (Modal.Open == null && i >= 0 && i < Enemies.Count) InfoPanel.Enemy(UiRoot, Battle.Snapshot, i, EnemyBox(i)); }

        public Rect HeroRect(int i)
        {
            var u = Heroes[i];
            var p = FieldRoot.TransformPoint(u.Feet);
            float w = Mathf.Min(1.1f, u.Width() * 0.35f), h = Mathf.Min(2.4f, u.Height());
            return new Rect(p.x - w, p.y - 0.2f, w * 2, h + 0.2f);
        }

        static Rect RectOf(Transform t, float d) { var s = d * Mathf.Abs(t.lossyScale.x); return new Rect(t.position.x - s / 2, t.position.y - s / 2, s, s); }
        public void OpenPause() { if (Modal.Open == null) InfoPanel.Pause(UiRoot, ToggleSpeed); }

        // ── 미리보기 — 든 카드(고른 고학년)를 그 대상에게 내면 ──
        (int card, int aim, bool lifted, int ult, int ultAim) pvKey = (-9, -9, false, -9, -9);

        void PaintPreview()
        {
            var key = (Hand.Held, Hand.Aim, Hand.Lifted, UltSel, UltAim);
            if (key.Equals(pvKey)) return;
            pvKey = key;
            IReadOnlyList<PreviewFoe> foes = null;
            PreviewParty party = null;
            if (UltSel >= 0)
            {
                bool needs = Battle.UltNeedsTarget(UltSel);
                if (!needs || UltAim >= 0) foes = Battle.PreviewUlt(UltSel, needs ? UltAim : FirstAliveEnemy());
            }
            else if (Hand.Held >= 0 && Hand.Held < Hand.Cards.Count)
            {
                var c = Hand.Cards[Hand.Held].Info;
                if (c.Target == TargetKind.Enemy) { if (Hand.Aim >= 0) foes = Battle.PreviewCard(Hand.Held, Hand.Aim); }
                else if (Hand.Lifted) foes = Battle.PreviewCard(Hand.Held, FirstAliveEnemy());
                if (Hand.Lifted || Hand.Aim >= 0) party = Battle.PreviewPartyOf(Hand.Held);
            }
            // 겨눈 카드 · 고학년 주인의 성격 — 적 머리 위 약점 아이콘이 빛난다(공명은 늘 · 「약점 공격」 카드도)
            string aimNat = null;
            int aimHero = -1;
            bool weakTag = false;
            if (UltSel >= 0) { aimHero = UltSel; aimNat = UltSel < Battle.Snapshot.Heroes.Count ? Battle.Snapshot.Heroes[UltSel].Nature : null; }
            else if (Hand.Held >= 0 && Hand.Held < Hand.Cards.Count)
            {
                var ci = Hand.Cards[Hand.Held].Info;
                aimHero = ci.Owner >= 0 ? ci.Owner : ci.Hero;
                aimNat = ci.Nature;
                weakTag = ci.Tags != null && (ci.Tags.Contains("약점") || ci.Tags.Contains("약점 공격"));
            }
            for (int i = 0; i < EnemyHuds.Count; i++)
            {
                if (EnemyHuds[i] == null) continue;
                var pf = foes != null && i < foes.Count ? foes[i] : null;
                EnemyHuds[i].SetPreview(pf);
                // 약점인가는 core 가 정한다(WeakFor — 성격 · 공명은 늘 · 적 표식) + 카드의 「약점 공격」
                bool weak = pf != null && (weakTag || Battle.WeakFor(aimHero, i));
                EnemyHuds[i].SetAim(weak ? aimNat : null, weak);
            }
            Hud.SetPreview(party);
            if (foes != null || party != null) Emit("preview");
        }

        void ClearPreview()
        {
            pvKey = (-9, -9, false, -9, -9);
            foreach (var h in EnemyHuds) if (h != null) { h.SetPreview(null); h.SetAim(null, false); }
            Hud.SetPreview(null);
        }

        IEnumerator Intro()
        {
            // 사도가 왼쪽에서 달려 들어온다
            foreach (var h in Heroes)
            {
                h.transform.localPosition = h.Home + new Vector3(-6f, 0, 0);
                h.Loop("Move");
            }
            ScreenFx.I.Fade(0, 1.6f);
            Sfx.Play("battle_start", 0.6f);
            for (int i = 0; i < Heroes.Count; i++) StartCoroutine(RunIn(Heroes[i], Heroes[i].Home, 0.8f + i * 0.08f));
            yield return Clock.Wait(0.5f);
            Sfx.Voice(Heroes[0].name.Replace("hero_", ""), "spawn");
            yield return Clock.Wait(0.5f);
        }

        IEnumerator RunIn(UnitView u, Vector3 home, float dur)
        {
            var from = u.transform.localPosition;
            yield return Clock.Tween(dur, t => u.transform.localPosition = Vector3.Lerp(from, home, Ease.OutCubic(t)));
            u.Idle();
        }

        // ── 이벤트 연출 ──
        static bool Boundary(EventKind k) => k == EventKind.Act || k == EventKind.WaveStart || k == EventKind.TurnStart || k == EventKind.Victory
                                             || k == EventKind.Defeat || k == EventKind.Discard || k == EventKind.Exhaust || k == EventKind.CardPlayed || k == EventKind.EpiphanyApplied;

        // 소환 — 규칙이 적을 b.Enemies 끝에 붙였는데 화면에 아직 없으면 세운다(빈 자리에 연기와 함께 솟아난다)
        static readonly Vector3[] SummonPos = { new Vector3(6.1f, -1.35f, 0), new Vector3(1.4f, -1.45f, 0), new Vector3(5.0f, -0.2f, 0), new Vector3(7.0f, -0.4f, 0), new Vector3(2.8f, -1.7f, 0) };
        int summoned;
        static bool HasWave(IReadOnlyList<BattleEvent> evs) { foreach (var e in evs) if (e.Kind == EventKind.WaveStart) return true; return false; }
        void SpawnSummons()
        {
            var s = Battle.Snapshot;
            while (Enemies.Count < s.Enemies.Count)
            {
                int i = Enemies.Count;
                var es = s.Enemies[i];
                // 빈 자리 — 소환 자리 중 지금 선 적들과 가장 먼 곳(차례로 고르면 이미 선 적과 겹칠 수 있었다)
                var pos = SummonPos[summoned++ % SummonPos.Length];
                float bestD = -1;
                foreach (var cand in SummonPos)
                {
                    float d = float.MaxValue;
                    foreach (var en in Enemies) if (en != null && en.gameObject.activeSelf) d = Mathf.Min(d, Vector2.Distance(cand, en.Home));
                    if (d > bestD) { bestD = d; pos = cand; }
                }
                var u = UnitView.Create(FieldRoot, "enemy_" + es.Key, es.Key, es.Skin, false, UnitScale * EnemyShrink, pos, 34 - i * 2, Look.EnemyIconAs(es.Id, es.Nature), es.Name, Look.IsStandIn(es.Id));
                u.Ref = UnitRef.Enemy(i);
                u.Mood = (es.Skin ?? "").Replace("Skin_", "").Replace("Joly", "Jolly");   // 판 성격 애니(Attack1_1_Cool …) — 오리카 오타 스킨도
                Enemies.Add(u);
                var hud = EnemyHud.Attach(u, es, i);
                int ii = i;
                hud.OnInfo = () => OpenEnemyInfo(ii);
                EnemyHuds.Add(hud);
                PlaceSummon(u, hud, pos);
                pos = u.Home;
                u.Play(u.Resolve("Spawn") ?? "Idle", 1.4f);
                Vfx.Burst(pos + new Vector3(0, 0.3f, 0), new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Smoke_01", Count = 10, Speed = new Vector2(1.5f, 3.5f), Angle = 90, Spread = 160, Life = new Vector2(0.5f, 0.8f),
                    Size = new Vector2(0.6f, 1.2f), C0 = new Color(0.85f, 0.8f, 0.9f, 0.5f), C1 = new Color(0.6f, 0.55f, 0.7f, 0.3f), Additive = false, Boost = 1f,
                    Drag = 3f, Order = 60, ShrinkTo = 1.3f,
                });
                Vfx.Word(pos + new Vector3(0, 2.2f, 0), "소환!", 0.36f, new Color(0.9f, 0.8f, 1f), new Color(0.15f, 0.05f, 0.25f));
                Debug.Log($"[Battle] 소환 — {es.Name} ({i})");
            }
            ResetEnemyOrder();
        }

        IEnumerator Present(IReadOnlyList<BattleEvent> evs)
        {
            int i = 0;
            if (Battle.Snapshot.Enemies.Count > Enemies.Count && Enemies.Count > 0 && !HasWave(evs)) SpawnSummons();

            var dealing = new List<CardInfo>();
            while (i < evs.Count)
            {
                var e = evs[i];
                if (e.Kind == EventKind.Act)
                {
                    int j = i + 1;
                    var group = new List<BattleEvent>();
                    while (j < evs.Count && !Boundary(evs[j].Kind)) group.Add(evs[j++]);
                    // 처치해서 고학년을 한 번 더(란 · 키디언 cue) — cue 앞은 첫 고학년, 뒤는 두 번째 고학년으로 따로 튼다
                    int again = e.Actor.Side == Side.Party ? group.FindIndex(x => x.Kind == EventKind.FxCue && (x.Text == "ran_again" || x.Text == "kidian_again")) : -1;
                    if (again >= 0)
                    {
                        yield return HeroAct(e, group.GetRange(0, again));
                        yield return FxCueFx(group[again]);
                        yield return HeroAct(e, group.GetRange(again + 1, group.Count - again - 1));
                    }
                    else if (e.Actor.Side == Side.Party) yield return HeroAct(e, group);
                    else yield return EnemyAct(e, group);
                    i = j;
                    continue;
                }
                if (e.Kind == EventKind.FxCue && IsCueStrike(e.Text))
                {
                    // 턴 시작 발동 — 뒤따르는 피해를 묶어 그 박자에(미로 광선 V 발 · 오로라 기둥 V 대 · 키샤 · 이드(재활)는 적 전체 한 번)
                    int j = i + 1, dmg = 0, cap = CueCap(e);
                    var group = new List<BattleEvent>();
                    while (j < evs.Count && dmg < cap && !Boundary(evs[j].Kind) && evs[j].Kind != EventKind.FxCue) { if (evs[j].Kind == EventKind.Damage) dmg++; group.Add(evs[j++]); }
                    // 이드(재활) — 꿈이 무너지며 파티 실드(바로 뒤 방어 · 실드)
                    if (e.Text == "ide_wake") while (j < evs.Count && (evs[j].Kind == EventKind.Block || evs[j].Kind == EventKind.Status) && evs[j].Target.Side == Side.Party) group.Add(evs[j++]);
                    yield return e.Text == "miro_beam" ? MiroBeam(e, group) : CUE2.TryGetValue(e.Text, out var c2) ? Cue2Strike(e, group, c2) : CueStrike(e, group);
                    i = j;
                    continue;
                }
                if (e.Kind == EventKind.Draw && e.Text != null && e.Text.StartsWith("grace"))
                {   // 은총으로 얻은 카드 — 크게 보인 뒤 손패로(손이 가득하면 버림 더미로)
                    yield return GraceIn(e);
                    i++;
                    continue;
                }
                if (e.Kind == EventKind.Draw)
                {
                    // 연달아 뽑는 것은 한꺼번에 — 부채가 한 번에 펼쳐지게
                    while (i < evs.Count && evs[i].Kind == EventKind.Draw) dealing.Add(evs[i++].Card);
                    yield return Deal(dealing);
                    dealing.Clear();
                    continue;
                }
                yield return Simple(e);
                i++;
            }
            RefreshHud();
        }

        public void RefreshAll() => RefreshHud();   // 점검이 판을 바꾼 뒤
        void RefreshHud()
        {
            var s = Battle.Snapshot;
            Hud.SetAp(s.Ap, s.MaxAp);
            Hud.SetPiles(s.DrawCount, s.DiscardCount, s.GoneCount);
            for (int i = 0; i < s.Heroes.Count && i < Hud.Ults.Count; i++) Hud.Ults[i].Set(s.Heroes[i].Ult, s.Heroes[i].UltMax);
            Hud.SetSnapshot(s);
            Hud.SetBlock(s.PartyBlock);
            for (int i = 0; i < EnemyHuds.Count && i < s.Enemies.Count; i++)
            {
                if (EnemyHuds[i] == null || s.Enemies[i].Dead) continue;
                EnemyHuds[i].SetIntent(s.Enemies[i]);
                EnemyHuds[i].SetChips(s.Enemies[i].Chips);
            }
            Hand.Sync(s.Hand);
            Hand.Layout();
        }

        IEnumerator Deal(List<CardInfo> cards)
        {
            foreach (var c in cards)
            {
                Hand.Add(c);
                Sfx.Play("card_draw", 0.3f);
                yield return Clock.WaitU(0.07f);
            }
            RefreshHud();
            yield return Clock.WaitU(0.25f);
        }

        IEnumerator Simple(BattleEvent e)
        {
            var s = Battle.Snapshot;
            switch (e.Kind)
            {
                case EventKind.WaveStart:
                    yield return WaveIn(e);
                    break;
                case EventKind.TurnStart:
                    Hud.SetTurn(e.Value, s.Wave, s.WaveCount);
                    Sfx.Play("turn_start", 0.5f);
                    yield return Banners.Turn(UiRoot, "PLAYER TURN", new Color(1f, 0.85f, 0.5f));
                    break;
                case EventKind.ApChanged:
                    Hud.SetAp(e.Value, s.MaxAp);
                    break;
                case EventKind.Block:
                    ApplyBlock(e);
                    break;
                case EventKind.CardChanged:
                {
                    // 손 안에서 바뀜(진화 · 변신 · 결속) — 번쩍이고 새 모습으로
                    var cv = Hand.Find(e.Card.Id);
                    if (cv != null)
                    {
                        cv.Info = e.Card;
                        cv.Refresh();
                        Clock.Run(cv.FlashCo(0.4f, 0.9f));
                        CueWord(cv.transform.position, e.Text);
                        Sfx.Play("card_skill", 0.4f, 1.2f);
                        yield return Clock.WaitU(0.25f);
                    }
                    break;
                }
                case EventKind.Discard:
                case EventKind.Exhaust:
                {
                    var cv = Hand.Find(e.Card.Id);
                    if (cv != null && e.Text != null && LabelKo.ContainsKey(e.Text)) CueWord(cv.transform.position, e.Text);
                    if (cv != null)
                    {
                        Hand.Remove(cv);
                        bool gone = e.Kind == EventKind.Exhaust;
                        cv.TargetPos = gone ? cv.TargetPos + new Vector3(0, 1.2f, 0) : Hand.DiscardPos;
                        cv.TargetScale = gone ? 0.9f : 0.25f;
                        cv.TargetRot = gone ? 0 : -30;
                        if (gone) { Clock.Run(Clock.Tween(0.4f, t => { if (cv) cv.SetAlpha(1 - t); }, true)); Clock.Run(cv.FlashCo(0.3f, 0.8f)); }
                        Destroy(cv.gameObject, 0.5f);
                        yield return Clock.WaitU(0.04f);
                    }
                    break;
                }
                case EventKind.CardPlayed:
                {
                    // 저절로 나간 카드(연계 · 천상 …) — 손에서 빠져 사도에게 날아간다
                    if (skipPlayed == e.Card.Id) { skipPlayed = null; break; }
                    var cv = Hand.Find(e.Card.Id);
                    if (cv == null) break;
                    Hand.Remove(cv);
                    StartCoroutine(CardUse(cv, Heroes[Mathf.Clamp(e.Card.Hero >= 0 ? e.Card.Hero : e.Card.Owner, 0, Heroes.Count - 1)]));
                    Sfx.Play("card_play", 0.4f);
                    yield return Clock.WaitU(0.12f);
                    break;
                }
                case EventKind.UltGauge:
                    Hud.SetGauge(e.Value);
                    if (e.Actor.Index < Hud.Ults.Count) Hud.Ults[e.Actor.Index].Set(e.Value, s.Heroes[e.Actor.Index].UltMax);
                    break;
                case EventKind.UltReady:
                    Sfx.Play("ult_ready", 0.6f);
                    Vfx.Glow(Heroes[e.Actor.Index].Center, 3f, new Color(1f, 0.85f, 0.5f, 0.8f), 0.6f, 2.5f);
                    break;
                case EventKind.Intent:
                {
                    int i = e.Target.Index;
                    if (i < EnemyHuds.Count && EnemyHuds[i] != null && i < s.Enemies.Count) EnemyHuds[i].SetIntent(s.Enemies[i]);
                    Hud.SetSnapshot(s);
                    break;
                }
                case EventKind.FxCue:
                    yield return FxCueFx(e);
                    break;
                case EventKind.FoeUlt:
                    yield return FoeUltFx(e);
                    break;
                case EventKind.FoeCharge:
                    yield return FoeChargeFx(e);
                    break;
                case EventKind.Form:
                    yield return FormFx(e);
                    break;
                case EventKind.Recover:
                {
                    int i = e.Target.Index;
                    if (i >= EnemyHuds.Count || EnemyHuds[i] == null) break;
                    EnemyHuds[i].SetBroken(false);
                    EnemyHuds[i].SetTough(e.FAfter, false);
                    Enemies[i].Idle();
                    Vfx.Word(Enemies[i].Top + new Vector3(0, 0.3f, 0), "회복", 0.4f, new Color(0.85f, 0.95f, 1f), new Color(0, 0.1f, 0.25f));
                    yield return Clock.Wait(0.35f);
                    break;
                }
                case EventKind.Victory:
                    yield return VictoryFlow();
                    break;
                case EventKind.Defeat:
                    Over = true;
                    yield return Banners.Defeat(UiRoot);
                    break;
                default:
                    yield return ApplyConsequence(e, null);
                    break;
            }
        }

        // 카드 이동 쪽지 Label → 낱말(최소 연출 — 카드 위에 떠오른다)
        static readonly Dictionary<string, string> LabelKo = new Dictionary<string, string>
        {
            ["form"] = "변신", ["forget"] = "망각", ["remove"] = "제거", ["bond"] = "결속", ["evolve"] = "진화", ["transform"] = "변신", ["pull"] = "끌어옴",
            ["burn"] = "소멸", ["evaporate"] = "증발", ["recall"] = "회수", ["make"] = "생성", ["connect"] = "연결",
            ["seize"] = "빼앗김",   // 적의 손패 흡수(core 수 seize) — 손 카드가 적에게 끌려간다 · 되찾으면 뽑기처럼 손으로
        };

        void CueWord(Vector3 at, string label)
        {
            if (label == null || !LabelKo.TryGetValue(label, out var w)) return;
            Vfx.Word(at + new Vector3(0, 1.4f, 0), w, 0.32f, new Color(1f, 0.92f, 0.7f), new Color(0.15f, 0.08f, 0), 0.9f, 1f, UiRoot, 700, 0.25f);
        }

        void ApplyBlock(BattleEvent e)
        {
            if (e.Target.Side == Side.Party)
            {
                Hud.SetBlock(e.BlockAfter);
                if (e.Value > 0)
                {
                    Hud.PopBlock();
                    Sfx.Play("block_gain", 0.6f);
                    foreach (var h in Heroes)
                    {
                        if (Bolzena.Fx.BolzenaFx.Common("shield", h.Fx) == null)   // 원작 실드(이드) — 없으면 자체 고리
                            Vfx.Glow(h.Center, 2.6f, new Color(0.45f, 0.75f, 1f, 0.8f), 0.45f, 2.4f, "FX_IN_Ring_ShockWave_03", 120);
                        h.Flash(new Color(0.6f, 0.85f, 1f), 0.3f, 0.6f);
                    }
                    var at = Heroes[Mathf.Clamp(e.Target.Index, 0, Heroes.Count - 1)].Top + new Vector3(0, 0.2f, 0);
                    Vfx.Word(at, (e.Text == "shield" ? "실드 +" : "방어 +") + e.Value, 0.36f, new Color(0.75f, 0.9f, 1f), new Color(0, 0.08f, 0.2f));
                }
            }
            else
            {
                int i = e.Target.Index;
                if (i >= EnemyHuds.Count || EnemyHuds[i] == null) return;
                EnemyHuds[i].SetBlock(e.BlockAfter);
                if (e.Value < 0)
                {
                    // 방어 · 실드가 사라짐 — 방패 조각이 흩어지고 「방어 사라짐」
                    EnemyHuds[i].ShieldBreak();
                    Sfx.Play("block_hit", 0.45f, 0.8f);
                    Vfx.Word(Enemies[i].Top + new Vector3(0, 0.2f, 0), "방어 -" + (-e.Value), 0.34f, new Color(0.7f, 0.8f, 0.95f), new Color(0, 0.05f, 0.15f));
                }
                if (e.Value > 0)
                {
                    Sfx.Play("block_gain", 0.5f);
                    Vfx.Glow(Enemies[i].Center, 2.6f, new Color(0.45f, 0.75f, 1f, 0.8f), 0.45f, 2.4f, "FX_IN_Ring_ShockWave_03", 120);
                    Enemies[i].Flash(new Color(0.6f, 0.85f, 1f), 0.3f, 0.6f);
                    Vfx.Word(Enemies[i].Top + new Vector3(0, 0.2f, 0), "방어 +" + e.Value, 0.4f, new Color(0.75f, 0.9f, 1f), new Color(0, 0.08f, 0.2f));
                }
            }
        }

        // ── 적 자리 · 머리 위 묶음 자리(EnemyLayout) ──
        List<EnemyLayout.Item> LayoutItems()
        {
            var l = new List<EnemyLayout.Item>();
            for (int i = 0; i < Enemies.Count && i < EnemyHuds.Count; i++)
                if (Enemies[i] != null && EnemyHuds[i] != null) l.Add(new EnemyLayout.Item { U = Enemies[i], Hud = EnemyHuds[i], Want = Enemies[i].Slot, Pos = Enemies[i].Home, Bar = EnemyHuds[i].BarPos });
            return l;
        }

        bool AliveItem(EnemyLayout.Item it) { int i = Enemies.IndexOf(it.U); return i >= 0 && !Dead(i); }

        void LayoutEnemies()
        {
            var items = LayoutItems();
            var rep = EnemyLayout.Place(items);
            LayoutCheck(items, "웨이브", rep);
        }

        // 소환 — 빈자리 후보(촘촘한 칸) 가운데 산 적들의 몸 · 묶음과 겹침이 가장 적은 곳(같으면 원래 소환 자리에 가까운 곳). 그 뒤 묶음을 다시 쌓는다
        void PlaceSummon(UnitView u, EnemyHud hud, Vector3 want)
        {
            if (EnemyHud.OldLayout) return;
            var items = LayoutItems();
            var me = items.Find(x => x.U == u);
            if (me == null) return;
            foreach (var o in items) if (o != me && AliveItem(o)) { u.Rescale(o.U.ScaleMul); hud.SetHudMul(o.Hud.HudMul); break; }   // 이웃과 같은 배율로
            Vector3 best = want; float bestS = float.MaxValue;
            for (float y = -1.5f; y <= 0.31f; y += 0.45f)
                for (float x = EnemyLayout.XMin + 0.6f; x <= EnemyLayout.XMax - 0.6f; x += 0.3f)
                {
                    me.Pos = new Vector3(x, y, 0);
                    bool ok = EnemyLayout.Huds(items, AliveItem);
                    float sc = EnemyLayout.Overlap(items, null, AliveItem) * 100 + Vector2.Distance(me.Pos, want) * 0.05f + (ok ? 0 : 10);
                    if (sc < bestS) { bestS = sc; best = me.Pos; }
                }
            me.Pos = best;
            u.Home = u.Slot = best;
            u.transform.localPosition = best;
            RestackHuds("소환");
        }

        /// <summary>머리 위 묶음만 다시 쌓는다(소환 · 쓰러짐 뒤) — 몸 자리는 그대로.</summary>
        void RestackHuds(string why)
        {
            if (EnemyHud.OldLayout) return;
            var items = LayoutItems();
            EnemyLayout.Huds(items, AliveItem);
            foreach (var it in items) if (AliveItem(it)) it.Hud.BarPos = it.Bar;
            EnemyLayout.Layer(items);
            LayoutCheck(items, why, EnemyLayout.Report(items, AliveItem));
        }

        // 점검 단언 — 서로 다른 적끼리 몸 · 묶음 겹침이 0 이어야 한다(데모 · 점검이면 오류로 적는다)
        public float LastOverlap { get; private set; }
        void LayoutCheck(List<EnemyLayout.Item> items, string why, string rep)
        {
            LastOverlap = EnemyLayout.Overlap(items, null, AliveItem);
            bool test = Array.IndexOf(Environment.GetCommandLineArgs(), "-demo") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-layoutshot") >= 0;
            string line = $"[Layout] {why} — {rep}";
            if (LastOverlap > 1e-3f && test) Debug.LogError(line + " — 겹침 단언 실패");
            else Debug.Log(line);
        }

        // 웨이브 — 적이 들어온다(보스면 등장 띠부터)
        IEnumerator WaveIn(BattleEvent e)
        {
            foreach (var old in Enemies) if (old != null) Destroy(old.gameObject);
            Enemies.Clear();
            EnemyHuds.Clear();
            summoned = 0;
            var s = Battle.Snapshot;
            var cloneE = s.Enemies.Find(x => Look.Data?.Enemy(x.Id)?.Clone != null);   // 사도 클론은 늘 보스(점검 판처럼 Boss 표시가 없어도)
            if (e.Boss || cloneE != null)
            {
                var b = s.Enemies.Find(x => x.Boss) ?? cloneE ?? s.Enemies[0];
                yield return Clock.Wait(0.3f);
                Hud.SetTurn(s.Turn, s.Wave, s.WaveCount);
                Emit("boss_banner_start");
                string bsub = b.ToughMaxV > 0 ? "구역 보스  ·  강인도 " + EnemyHud.Thirds(b.ToughMaxV) : "구역 보스";
                var cdef = Look.Data?.Enemy(b.Id);
                if (cdef?.Clone != null)
                {
                    // 사도 클론 보스 — SD 띠 대신 그 사도의 스탠딩 일러로 등장 컷(붉은 톤 · 오른쪽에서 · 「보스 클론」). 짧게, 누르면 건너뜀, 「컷인 건너뛰기」면 생략
                    if (!Bolzena.RunUI.Settings.SkipCutin)
                    {
                        Sfx.Play("boss_entry", 0.8f);
                        string hname = Look.Data.Hero(cdef.Clone)?.Name ?? b.Name;
                        yield return UltCutin.Play(ScreenRoot, Look.Hero(cdef.Clone).Art, "", hname, new Color(0.85f, 0.12f, 0.12f), true, "클론", 1.0f, true, "spawn");
                        Emit("clone_entry");
                    }
                }
                else yield return Banners.Boss(ScreenRoot, b.Key, b.Skin, b.Name, bsub);
            }
            int n = s.Enemies.Count;
            int bossI = s.Enemies.FindIndex(x => x.Boss);
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                var es = s.Enemies[i];
                Vector3 pos;
                if (es.Boss) pos = n == 1 ? BossPos : new Vector3(3.3f, -1.45f, 0);
                else if (bossI >= 0) { pos = k == 0 ? new Vector3(6.3f, -0.95f, 0) : k == 1 ? new Vector3(6.7f, -1.6f, 0) : new Vector3(1.5f + (k - 2) * 0.9f, -1.6f + (k % 2) * 0.5f, 0); k++; }
                else pos = EnemySlot(n, i);
                float sc = (es.Boss ? UnitScale * 1.0f : UnitScale * 1.1f) * EnemyShrink;
                using var _h = Bolzena.RunUI.Hitch.Span("적 유닛 세우기");
                var u = UnitView.Create(FieldRoot, "enemy_" + es.Key, es.Key, es.Skin, false, sc, pos, es.Boss ? 30 : 36 - i * 2, Look.EnemyIconAs(es.Id, es.Nature), es.Name, Look.IsStandIn(es.Id));
                u.Ref = UnitRef.Enemy(i);
                u.Mood = (es.Skin ?? "").Replace("Skin_", "").Replace("Joly", "Jolly");   // 판 성격 애니(Attack1_1_Cool …) — 오리카 오타 스킨도
                Enemies.Add(u);
                var hud = EnemyHud.Attach(u, es, i);
                int ii = i;
                hud.OnInfo = () => OpenEnemyInfo(ii);
                EnemyHuds.Add(hud);
            }
            // 자리 — 몸 · 머리 위 묶음이 서로 겹치지 않게(EnemyLayout). 예전 자리(-oldlayout)면 표 그대로
            if (!EnemyHud.OldLayout) LayoutEnemies();
            for (int i = 0; i < n; i++)
            {
                var es = s.Enemies[i];
                var u = Enemies[i];
                var pos = u.Home;
                u.transform.localPosition = pos;
                if (es.Boss)
                {
                    u.Play(u.Resolve("Spawn"), 1.6f);
                    FieldRig.Shake(0.35f);
                    Vfx.Burst(pos + new Vector3(0, 0.2f, 0), new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Smoke_01", Count = 16, Speed = new Vector2(2f, 5f), Angle = 90, Spread = 160, Life = new Vector2(0.6f, 1f),
                        Size = new Vector2(0.8f, 1.6f), C0 = new Color(0.8f, 0.75f, 0.7f, 0.5f), C1 = new Color(0.6f, 0.55f, 0.5f, 0.4f), Additive = false, Boost = 1f,
                        Drag = 3f, Order = 60, ShrinkTo = 1.4f,
                    });
                }
                else
                {
                    u.transform.localPosition = pos + new Vector3(7f, 0, 0);
                    u.Loop("Move");
                    StartCoroutine(RunIn(u, pos, 0.75f + i * 0.1f));
                }
            }
            ResetEnemyOrder();
            yield return Clock.Wait(e.Boss ? 1.4f : 0.9f);
            if (e.Boss) Emit("boss_in");
        }

        // ── 카드 ──
        IEnumerator PlayCardFlow(int handIndex, int target)
        {
            if (handIndex < 0 || handIndex >= Hand.Cards.Count || !Battle.CanPlay(handIndex, out _))
            {
                var core = Battle.Snapshot.Hand;
                Debug.LogWarning($"[Battle] 카드를 못 냄 — 손 {handIndex}/{Hand.Cards.Count}(규칙 손 {core.Count}) " +
                                 $"화면 {(handIndex >= 0 && handIndex < Hand.Cards.Count ? Hand.Cards[handIndex].Info.Id : "-")} · 규칙 {(handIndex >= 0 && handIndex < core.Count ? core[handIndex].Id : "-")}");
                yield break;
            }
            var cv = Hand.Cards[handIndex];
            var info = cv.Info;
            int choice = -1;
            Hand.Remove(cv);
            if (info.Epiphany && info.Grace)
            {   // 은총 — 고르기 없음(선택지 하나). 신탁 창을 열지 않고, 얻은 카드는 PlayCard 의 Draw(grace) 연출(GraceIn)이 크게 보인 뒤 손패로
                choice = 0;
                graceFrom = info.Name;
            }
            else if (info.Epiphany)
            {
                var opts = Battle.EpiphanyOptions(handIndex);
                if (opts.Count > 0) yield return EpiphanyWindow.Run(ScreenRoot, cv, opts, c => choice = c, DemoEpiphanyPick, () => OpenPile(0));   // 덱 보기 — 뽑을 더미 창(사도별 묶음)
            }
            int branch = 0;
            if (info.Choices != null && info.Choices.Count == 2)
            {
                yield return ChoiceWindow.Run(UiRoot, info, b => branch = b, DemoBranchPick);
                if (branch <= 0) { Hand.Cards.Insert(Mathf.Min(handIndex, Hand.Cards.Count), cv); Hand.Layout(); yield break; }   // 물렀다
            }
            int spend = 0;
            var spendPrompt = Battle.SpendPromptOf(handIndex);   // 소모량을 고르는 카드(spend pick) — 자동 전투는 창 없이 가장 많이(전부), 사람은 창에서
            if (spendPrompt != null && spendPrompt.Options.Count > 0)
            {
                if (Auto) spend = spendPrompt.Options[spendPrompt.Options.Count - 1].N;
                else
                {
                    bool handOn = Hand.gameObject.activeSelf;
                    Hand.gameObject.SetActive(false);   // 창 뒤로 손패 글이 비치지 않게(폰은 올려 둔 카드의 글이 밝게 비쳤다)
                    yield return SpendWindow.Run(UiRoot, info, spendPrompt, v => spend = v, DemoSpendPick);
                    Hand.gameObject.SetActive(handOn);
                    if (spend <= 0) { Hand.Cards.Insert(Mathf.Min(handIndex, Hand.Cards.Count), cv); Hand.Layout(); yield break; }   // 물렀다
                }
            }
            skipPlayed = info.Id;
            var evs = Battle.PlayCard(handIndex, target, choice, branch, spend);
            if (choice >= 0) Bolzena.Fx.BolzenaFx.Common("oracle", Heroes[Mathf.Clamp(info.Hero >= 0 ? info.Hero : info.Owner, 0, Heroes.Count - 1)].Fx);   // 신탁을 골랐다
            StartCoroutine(CardUse(cv, Heroes[Mathf.Clamp(info.Hero >= 0 ? info.Hero : info.Owner, 0, Heroes.Count - 1)]));
            Sfx.Play("card_play", 0.5f);
            Sfx.Play(info.Type == CardType.Attack ? "card_swing" : "card_skill", 0.4f);
            yield return Clock.WaitU(0.12f);
            yield return Present(evs);
        }

        // 낸 카드 — 위로 떠올라 빛으로 부서지며 사도에게 날아간다
        IEnumerator CardUse(CardView cv, UnitView hero)
        {
            cv.Follow = 18f;
            cv.SetOrder(660);
            cv.TargetPos = new Vector3(cv.transform.localPosition.x * 0.4f, -1.0f, 0);
            cv.TargetRot = 0;
            cv.TargetScale = 0.95f;
            yield return Clock.WaitU(0.12f);
            Clock.Run(cv.FlashCo(0.25f));
            var heroWorld = FieldRoot.TransformPoint(hero.Center);
            Vfx.Burst(cv.transform.position, new Vfx.BurstOpt
            {
                Tex = "FX_UI_star_02", Count = 14, Speed = new Vector2(2f, 5f), Life = new Vector2(0.3f, 0.55f), Size = new Vector2(0.08f, 0.2f),
                C0 = new Color(1f, 0.9f, 0.6f), C1 = Color.white, Drag = 3f, Order = 670, Boost = 3f, Parent = UiRoot, ShrinkTo = 0,
            });
            cv.Follow = 9f;
            cv.TargetPos = heroWorld;
            cv.TargetScale = 0.15f;
            yield return Clock.Tween(0.16f, t => { if (cv) cv.SetAlpha(1 - Ease.OutCubic(t)); }, true);
            Vfx.Glow(hero.Center, 1.6f, new Color(1f, 0.9f, 0.6f, 0.8f), 0.3f, 2.5f);
            if (cv) Destroy(cv.gameObject);
        }

        IEnumerator EndTurnFlow()
        {
            Sfx.Play("turn_end", 0.5f);
            var evs = Battle.EndTurn();
            // 손패를 버리고 적 차례 띠
            int k = 0;
            while (k < evs.Count && (evs[k].Kind == EventKind.Discard || evs[k].Kind == EventKind.Exhaust)) { yield return Simple(evs[k]); k++; }
            yield return Banners.Turn(UiRoot, "ENEMY TURN", new Color(1f, 0.4f, 0.35f));
            var rest = new List<BattleEvent>();
            for (; k < evs.Count; k++) rest.Add(evs[k]);
            yield return Present(rest);
        }

        // -rewardoverlay: 보상 오버레이가 뜬 모습을 찍고 끝낸다(판 자동 데모는 오버레이의 「떠나기」 를 누르지 못한다)
        IEnumerator OverlayShot()
        {
            yield return Clock.WaitU(2.0f);
            yield return new WaitForEndOfFrame();
            var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "Captures", "overlay"));
            System.IO.Directory.CreateDirectory(dir);
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "reward_overlay.png"), tex.EncodeToPNG());
            Debug.Log("[Capture] reward_overlay");
            yield return Clock.WaitU(0.5f);
            Application.Quit();
        }

        static IEnumerator FadeOut(Transform t, float dur)
        {
            var srs = t.GetComponentsInChildren<SpriteRenderer>();
            var txs = t.GetComponentsInChildren<TMPro.TextMeshPro>();
            var mrs = t.GetComponentsInChildren<MeshRenderer>();
            yield return Clock.Tween(dur, k =>
            {
                foreach (var r in srs) if (r) Make.Alpha(r, r.color.a * (1 - k * 0.5f));
                foreach (var x in txs) if (x) x.alpha = 1 - k;
                foreach (var m in mrs) if (m && m.sharedMaterial != null && m.sharedMaterial.HasProperty("_Alpha")) m.sharedMaterial.SetFloat("_Alpha", 1 - k);
            }, true);
            if (t) Destroy(t.gameObject);
        }

        IEnumerator VictoryFlow()
        {
            Over = true;
            Hand.Hidden = true;
            yield return Clock.WaitU(0.6f);
            foreach (var h in Heroes) h.Loop(h.Resolve("Victory") ?? "Idle");
            Sfx.Voice(Heroes[0].name.Replace("hero_", ""), "victory");
            yield return Banners.Victory(UiRoot);
            Emit("victory");
            yield return Clock.WaitU(1.4f);
            // 끝 자세 — 싸움터를 그대로 두고(사도는 대기 동작) 손패 · 전투 HUD 를 걷는다. 위 파티 HP 띠와 「BATTLE END」 칩만
            foreach (var h in Heroes) h.Idle();
            Hud.EndPose(true);
            // 승리 띠를 걷는다 — 끝 자세(싸움터만) 위에 보상 줄이 올라오게
            var vic = UiRoot.Find("Victory");
            if (vic != null) StartCoroutine(FadeOut(vic, 0.5f));
            Emit("battle_end");
            yield return Clock.WaitU(1.2f);
            if (BattleBridge.EndHold == null) ScreenFx.I.Fade(1f, 1.2f);   // 보상 오버레이가 없으면 예전처럼 검게 닫고 판으로
        }
    }
}
