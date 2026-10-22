using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Battle;
using Bolzena.RunUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Bolzena.Demo
{
    // 모션 확인 화면(개발 · 점검용, 2026-10-13 사용자 「기존 공격 모션들 분리해서 정리 — 우선 인게임 확인 화면」).
    //   들어가는 길: 도감 → 사도 → 상세 정보 → 세로 메뉴 맨 아래 흐린 「모션 확인」(Flow.MotionLab — 판 진행 중엔 안 보인다 · 에디터나 실행 인자 -motionlab 일 때만, 2026-10-19).
    //   전투 장면을 그대로 띄운다(BattleBridge.Fight 없음 → CoreBattle.Fixture.Override: 그 사도 혼자 · 요정 셋 1웨이브). 손패 · HUD 는 치우고
    //   사도가 첫 자리에, 맞은편에 적 셋이 선다. 단추는 BattleDirector.LabPlay(BattleDirector.Lab.cs)로 실제 전투와 같은 길(Present → HeroAct …)을 탄다.
    //   왼쪽: 사도 20명(누르면 그 사도로 전투를 다시 연다) · 위: 지금 튼 것의 이름 · 길이 · 이펙트 · 타격 갈래 · 아래: 몸짓 · 대상 · 타수 · 반복 · 느리게 · 애니 이름 전부.
    //   소리는 설정 음량 그대로(시험 실행 인자면 TestMute 가 0).
    public class MotionLab : MonoBehaviour
    {
        static string pending;
        static bool loading;
        static float speedWas = -1;   // 느리게 켜기 전 배속(장면을 다시 열어도 되돌린다)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() => Flow.MotionLab = Enter;

        /// <summary>코어 사도 id 로 모션 확인 화면을 연다(판 화면 · 이 화면의 사도 목록에서).</summary>
        public static void Enter(string hero)
        {
            if (string.IsNullOrEmpty(hero) || loading || Flow.Me == null) return;
            pending = hero;
            CoreBattle.Fixture.Override = d => LabFixture(d, hero);
            loading = true;
            Flow.Me.StartCoroutine(EnterCo());
        }

        // 그 사도 혼자 · 적 셋(근거리 · 원거리 · 근거리 요정) 한 웨이브 — 판(엔진)은 첫 턴에서 멈춰 있고 단추가 지은 쪽지만 연출한다
        static CoreBattle.Fixture LabFixture(Bolzena.Core.GameData d, string hero)
        {
            var f = new CoreBattle.Fixture { Party = new List<string> { hero }, Note = "모션 확인 — " + hero };
            f.Deck = d.BuildDeck(f.Party);
            f.Waves.Add(new List<string> { "fairymobcloserange", "fairymoblongrange", "fairymobcloserange" });
            return f;
        }

        static IEnumerator EnterCo()
        {
            var stage = Flow.Me.Stage;
            bool fromBattle = BattleDirector.I != null;
            if (!fromBattle && stage.Canvas.enabled) yield return stage.Blackout(BattleBridge.BlackoutSec);
            LoadingScreen.Show("모션 확인 준비 중");
            LoadingScreen.Progress(0.05f, "전투 장면 읽는 중");
            yield return null;
            if (fromBattle) { RestoreSpeed(); Clock.Reset(); }   // 사도 바꾸기 — 지난 전투의 시계 연출을 멈추고 다시 연다
            yield return Resources.UnloadUnusedAssets();
            yield return WebBundles.WaitAll();
            SceneManager.sceneLoaded += Loaded;
            var op = SceneManager.LoadSceneAsync(BattleBridge.BattleScene);
            while (op != null && !op.isDone) { LoadingScreen.Progress(0.05f + 0.1f * op.progress); yield return null; }
            loading = false;
        }

        static void Loaded(Scene s, LoadSceneMode m)
        {
            if (s.name != BattleBridge.BattleScene) return;
            SceneManager.sceneLoaded -= Loaded;
            if (Flow.Me != null) { Flow.Me.Stage.Canvas.enabled = false; Flow.Me.Stage.ClearFade(); }
            var go = new GameObject("MotionLab");
            SceneManager.MoveGameObjectToScene(go, s);   // 전투 장면과 함께 사라지게
            go.AddComponent<MotionLab>().hero = pending;
        }

        static void RestoreSpeed()
        {
            if (speedWas > 0) { Clock.Speed = speedWas; speedWas = -1; }
        }

        // ── 화면 ──
        enum Kind { Move, Idle, Raw }
        struct Req { public Kind K; public BattleDirector.LabMove Mv; public string Anim, Label; }

        string hero;
        BattleDirector d;
        bool ready, leaving, busy, repeat, slow, all, cutin = true, infoOn = true;
        int hits = 1;
        // 추가 공격 — 쏘는 한 대의 종류(엔진 HurtOpts.Shot Label) · 여러 발(줄 세우기 확인)
        static readonly string[] ShotKinds = { "extra", "bullet", "assist", "resonance", "counter", "summon" };
        static readonly string[] ShotNames = { "추가", "탄환", "협공", "공명", "반격", "소환" };
        static readonly int[] Volleys = { 1, 4, 10 };
        int shotKind, volley;
        Btn bShotKind, bVolley;
        Req? last, queued;
        string header = "전투를 세우는 중...", shown, fired = "";
        Bolzena.Fx.FxCall firedCall;
        int firedN = -1;

        RectTransform root, infoBox, animContent;
        TextMeshProUGUI info;
        Btn bOne, bAll, bRepeat, bSlow, bCutin, bInfo;
        readonly Dictionary<int, Btn> bHits = new Dictionary<int, Btn>();
        float bh, fs;

        IEnumerator Start()
        {
            Build();
            float t = 0;
            while ((d = BattleDirector.I) == null || !d.Ready || !d.WaitingInput)
            {
                t += Time.unscaledDeltaTime;
                if (t > 90f) { header = "<color=#ff9a8a>전투가 서지 않았습니다 — 나가기를 누르세요</color>"; yield break; }
                yield return null;
            }
            // 전투 입력을 끈다(가짜 손가락을 화면 밖에) — 단추를 누른 손이 싸움터 · 손패로 새지 않게. 손패 · HUD 는 화면 밖으로
            Bolzena.UI.PointerInput.Simulated = true;
            Bolzena.UI.PointerInput.SimPos = new Vector2(0, -60);
            Bolzena.UI.PointerInput.SimHeld = false;
            if (d.UiRoot) d.UiRoot.localPosition = new Vector3(0, -200, 0);
            FillAnims();
            ready = true;
            header = Opts() + "\n단추를 누르면 실제 전투와 같은 길로 틉니다";
            Debug.Log("[MotionLab] 열림 — " + hero);
            if (shotsDir != null) StartCoroutine(Shoot());
        }

        // ── 캡처 모드(2026-10-13) ── Bolzena.exe -motionlab-shots <폴더> [-motionlab-heroes 티그_영웅,아야] [-screen-width 1600 -screen-height 900 -screen-fullscreen 0]
        //   사도(기본: 이 화면의 사도 목록 전부 · 가나다 순)마다 평타 · 강공 · 스킬 · 추가 공격(탄환 1발)을 적 1명 · 1타로 실제 전투 길로 틀고,
        //   첫 타격 순간 · +0.15초 · +0.4초(게임 시계) 세 장을 <폴더>/<번호>_<사도>_<동작>_<a|b|c>.png 로 남긴다(타격이 없으면 시작 1.2초에 한 장 _x).
        //   고학년(5고학년)은 적 전체 · 컷인 포함(LabPlay 고학년 길 = UltFlow 차례)으로 틀고, 컷인이 끝나 몸짓을 시작한 때(LabActAt)부터 0.25초마다(많아야 36장) +
        //   타격 순간마다(많아야 12장) <번호>_<사도>_5고학년_<nn>.png 와 시각표 <번호>_<사도>_5고학년.tsv(nn · 몸짓 시작에서 ms · t|hit) — Tools/motionlab_sheet.py 가 시트로 묶는다.
        //   -motionlab-ult 면 고학년만 찍는다.
        //   단추 · 글은 치우고 찍는다. 소리 0(AudioListener). 다 찍으면 _done.txt(동작마다 점검 글)를 쓰고 끈다. 지킴이: 동작 하나 8초(고학년 30초) · 전체 45분
        static string shotsDir;
        static List<string> shotsQueue;
        static readonly System.Text.StringBuilder shotsLog = new System.Text.StringBuilder();
        static float shotsStart;
        static readonly (BattleDirector.LabMove mv, string label)[] ShotMoves =
        {
            (BattleDirector.LabMove.Attack1, "1평타"), (BattleDirector.LabMove.Attack2, "2강공"),
            (BattleDirector.LabMove.Skill1, "3스킬"), (BattleDirector.LabMove.Extra, "4추가탄환"),
            (BattleDirector.LabMove.Ult, "5고학년"),
        };
        static bool shotsUltOnly;
        static bool shotsSeq;     // -motionlab-seq — 동작마다 0.1초 간격 작은 그림(seq/<이름>/<ms>.jpg) — Tools/vs_orig.py 가 원작 영상과 같은 시각으로 나란히(2026-10-15)
        static bool shotsFxDex;   // -motionlab-fxdex — 장 도감: 사도 원작 장을 하나씩 따로 틀어 찍는다(2026-10-15 「이름 ↔ 모습」 짐작 없애기)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ShotsBoot()
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, "-motionlab-shots");
            if (i < 0 || i + 1 >= a.Length || shotsDir != null) return;
            shotsDir = a[i + 1];
            shotsUltOnly = System.Array.IndexOf(a, "-motionlab-ult") >= 0;
            shotsFxDex = System.Array.IndexOf(a, "-motionlab-fxdex") >= 0;
            shotsSeq = System.Array.IndexOf(a, "-motionlab-seq") >= 0;
            int fi = System.Array.IndexOf(a, "-motionlab-seqfps"); if (fi >= 0 && fi + 1 < a.Length && float.TryParse(a[fi + 1], out var fps) && fps > 0) seqEvery = 1f / fps;   // 동영상용 — 30 이면 매 프레임
            System.IO.Directory.CreateDirectory(shotsDir);
            AudioListener.volume = 0f;   // 시험 실행은 음소거
            int j = System.Array.IndexOf(a, "-motionlab-heroes");
            if (j >= 0 && j + 1 < a.Length) shotsQueue = a[j + 1].Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            var go = new GameObject("MotionLab shots");
            DontDestroyOnLoad(go);
            go.AddComponent<ShotsKick>();
            Debug.Log("[MotionLab] 캡처 모드 — " + shotsDir);
        }

        // 판 화면(Flow)이 서면 첫 사도로 들어간다 · 전체 지킴이
        class ShotsKick : MonoBehaviour
        {
            IEnumerator Start()
            {
                shotsStart = Time.realtimeSinceStartup;
                while (Flow.Me == null || Flow.Me.Stage == null || Flow.Me.Stage.Busy) yield return null;
                yield return new WaitForSecondsRealtime(1.5f);
                if (shotsQueue == null || shotsQueue.Count == 0)
                    shotsQueue = Roster.Players.OrderBy(x => x.ko, System.StringComparer.Ordinal).Select(x => x.CoreId).ToList();
                Debug.Log("[MotionLab] 캡처 차례 — " + string.Join(",", shotsQueue));
                if (shotsQueue.Count == 0) { FinishShots(); yield break; }
                Enter(shotsQueue[0]);
                while (Time.realtimeSinceStartup - shotsStart < 2700f) yield return new WaitForSecondsRealtime(5f);
                Debug.LogWarning("[MotionLab] 캡처 45분 넘음 — 끈다");
                FinishShots();
            }
        }

        static void FinishShots()
        {
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(shotsDir, "_done.txt"), shotsLog.ToString()); }
            catch (System.Exception e) { Debug.LogWarning("[MotionLab] _done.txt 실패 " + e.Message); }
            Debug.Log("[MotionLab] 캡처 끝");
            Application.Quit();
        }

        IEnumerator Shoot()
        {
            if (root) root.gameObject.SetActive(false);   // 단추 · 글 치우기(이펙트 위를 가리지 않게)
            AudioListener.volume = 0f;
            yield return new WaitForSecondsRealtime(0.8f);
            int idx = shotsQueue != null ? shotsQueue.IndexOf(hero) : -1;
            string ko = Roster.OfCore(hero)?.ko ?? hero;
            all = false; hits = 1; volley = 0; repeat = false;
            shotKind = Mathf.Max(0, System.Array.IndexOf(ShotKinds, "bullet"));
            shotsLog.Append($"== {idx + 1:D2} {ko}({hero})\n");
            if (shotsFxDex)
            {
                yield return ShootFxDex(idx, ko);
                if (idx >= 0 && idx + 1 < shotsQueue.Count) Enter(shotsQueue[idx + 1]); else FinishShots();
                yield break;
            }
            foreach (var (mv, label) in ShotMoves)
            {
                string name = $"{idx + 1:D2}_{ko}_{label}";
                if (shotsUltOnly && mv != BattleDirector.LabMove.Ult) continue;
                if (mv == BattleDirector.LabMove.Ult) { yield return ShootUlt(name, label, ko); continue; }
                float first = -1;
                var prev = BattleDirector.Trace;
                BattleDirector.Trace = (k, s) => { prev?.Invoke(k, s); if (k == "hit" && first < 0) first = Time.time; };
                bool done = false;
                float t0 = Time.time;
                StartCoroutine(Wrap(PlayOnce(new Req { K = Kind.Move, Mv = mv, Label = label }), () => done = true));
                if (shotsSeq) StartCoroutine(SeqRec(name, t0, () => done && Bolzena.Fx.FxRun.Running == 0, 8f));   // 몸짓이 끝나도 이펙트가 다 걷힐 때까지(10-16 — 몸짓 끝 0.3초에 끊겨 대조 영상이 마지막 장면을 늘여 「남는 이펙트」로 보였다)
                while (first < 0 && !done && Time.time - t0 < 8f) yield return null;
                if (first >= 0)
                {
                    yield return Snap(name + "_a");
                    while (Time.time - first < 0.15f) yield return null;
                    yield return Snap(name + "_b");
                    while (Time.time - first < 0.4f) yield return null;
                    yield return Snap(name + "_c");
                }
                else
                {
                    while (!done && Time.time - t0 < 1.2f) yield return null;
                    yield return Snap(name + "_x");
                }
                while (!done && Time.time - t0 < 8f) yield return null;
                if (shotsSeq) while (Bolzena.Fx.FxRun.Running > 0 && Time.time - t0 < 8f) yield return null;   // 이펙트가 다 걷힐 때까지 찍고 넘어간다
                BattleDirector.Trace = prev;
                shotsLog.Append($"[{label}] {(first >= 0 ? $"첫 타격 {first - t0:F2}초" : "타격 없음")}\n{(d != null ? d.LabInfo : "")}\n");
                yield return new WaitForSecondsRealtime(0.4f);
            }
            shotsLog.Append("\n");
            if (idx >= 0 && idx + 1 < shotsQueue.Count) Enter(shotsQueue[idx + 1]);
            else FinishShots();
        }

        // 고학년 — 적 전체 · 컷인 포함. 몸짓 시작(LabActAt)부터 0.25초마다 + 타격 순간마다 한 장, 시각표(.tsv)도
        IEnumerator ShootUlt(string name, string label, string ko)
        {
            const float EVERY = 0.25f;
            const int MAX_T = 36, MAX_HIT = 12;   // (10-14) 16장(4초)에서 끊겨 긴 고학년 끝이 안 보였다 → 36장(9초)
            all = true; cutin = true;
            d.LabActAt = -1;
            int pendingHits = 0;
            var prev = BattleDirector.Trace;
            BattleDirector.Trace = (k, s) => { prev?.Invoke(k, s); if (k == "hit") pendingHits++; };
            bool done = false;
            float rt0 = Time.realtimeSinceStartup;
            StartCoroutine(Wrap(PlayOnce(new Req { K = Kind.Move, Mv = BattleDirector.LabMove.Ult, Label = label }), () => done = true));
            while (d.LabActAt < 0 && !done && Time.realtimeSinceStartup - rt0 < 15f) yield return null;   // 컷인이 끝나기까지
            float a0 = d.LabActAt >= 0 ? d.LabActAt : Time.time;
            if (shotsSeq) StartCoroutine(SeqRec(name, a0, () => done, 10f));
            var tsv = new System.Text.StringBuilder($"#hero\t{ko}\n#every\t{EVERY * 1000:0}\n");
            int n = 0, nt = 0, nh = 0;
            float next = a0;
            while (!done && Time.realtimeSinceStartup - rt0 < 30f)
            {
                bool hit = pendingHits > 0 && nh < MAX_HIT;
                bool tick = Time.time >= next && nt < MAX_T;
                if (!hit && !tick) { yield return null; continue; }
                if (hit) { pendingHits = 0; nh++; } else { nt++; next += EVERY; }
                n++;
                tsv.Append($"{n:D2}\t{(Time.time - a0) * 1000f:0}\t{(hit ? "hit" : "t")}\n");
                yield return Snap($"{name}_{n:D2}");
            }
            while (!done && Time.realtimeSinceStartup - rt0 < 30f) yield return null;
            BattleDirector.Trace = prev;
            all = false;
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(shotsDir, name + ".tsv"), tsv.ToString()); }
            catch (System.Exception e) { Debug.LogWarning("[MotionLab] 시각표 실패 " + name + " — " + e.Message); }
            shotsLog.Append($"[{label}] {n}장(0.25초 {nt} · 타격 {nh})\n{(d != null ? d.LabInfo : "")}\n");
            yield return new WaitForSecondsRealtime(0.6f);
        }

        static IEnumerator Wrap(IEnumerator e, System.Action fin) { yield return e; fin(); }

        // ── 장 도감(-motionlab-fxdex) ── 사도의 원작 장(평타 · 강공 · 스킬 · 시그니처 · 고학년 갈래)을 하나씩, 시전자 발밑(원작 장의 원점)에
        //   제 크기 · 오른쪽 보기로 틀고 0.3초 · 0.9초 두 장을 <번호>_<사도>_fxNN_<갈래>_<a|b>.png 로. _done.txt 에 번호 · 갈래 · 장 이름 · 길이 · 장 안 범위.
        //   Tools/fxdex_sheet.py 가 사도마다 이름 붙은 시트로 묶는다
        IEnumerator ShootFxDex(int idx, string ko)
        {
            var hid = d.Battle.Snapshot.Heroes[0].Id;
            var u = d.Heroes.Count > 0 ? d.Heroes[0] : null;
            if (u == null) yield break;
            int i = 0;
            foreach (var kind in new[] { "attack", "power", "skill", "sig", "ult" })
                foreach (var n in Bolzena.Fx.FxLibrary.Get(hid, kind))
                {
                    i++;
                    float dur = Bolzena.Fx.FxLibrary.Duration(n);
                    var b = Bolzena.Fx.UltFx.GeoBounds(n);
                    shotsLog.Append($"fx{i:D2}\t{kind}\t{n}\t{dur:F2}s\t{(b.HasValue ? $"x {b.Value.xMin:F0}~{b.Value.xMax:F0} · y {b.Value.yMin:F0}~{b.Value.yMax:F0}" : "-")}\n");
                    var r = Bolzena.Fx.BolzenaFx.Play(n, new Bolzena.Fx.FxPlayOptions { At = u.Fx.Feet, Flip = false, Until = Mathf.Min(dur, 2.5f), Order = 330, Parent = Bolzena.Fx.BolzenaFx.Parent });
                    float t0 = Time.time;
                    while (Time.time - t0 < 0.3f) yield return null;
                    yield return Snap($"{idx + 1:D2}_{ko}_fx{i:D2}_{kind}_a");
                    while (Time.time - t0 < 0.9f) yield return null;
                    yield return Snap($"{idx + 1:D2}_{ko}_fx{i:D2}_{kind}_b");
                    r?.Stop(0.05f);
                    float t1 = Time.time;
                    while (Time.time - t1 < 0.25f) yield return null;
                }
            shotsLog.Append("\n");
        }

        // 연속 그림 — start(게임 시계)부터 0.1초마다 640×360 jpg, 동작이 끝나고 0.3초 또는 maxSec 까지
        static RenderTexture seqRt; static Texture2D seqRead; static float seqEvery = 0.1f;
        IEnumerator SeqRec(string name, float start, System.Func<bool> done, float maxSec)
        {
            var dir = System.IO.Path.Combine(shotsDir, "seq", name);
            System.IO.Directory.CreateDirectory(dir);
            if (seqRt == null) seqRt = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            if (seqRead == null) seqRead = new Texture2D(640, 360, TextureFormat.RGB24, false);
            float next = 0, doneAt = -1;
            while (Time.time - start < maxSec)
            {
                float t = Time.time - start;
                if (done() && doneAt < 0) doneAt = t;
                if (doneAt >= 0 && t > doneAt + 0.3f) break;
                if (t >= next)
                {
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    Graphics.Blit(tex, seqRt);
                    var prevRt = RenderTexture.active; RenderTexture.active = seqRt;
                    seqRead.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); seqRead.Apply();
                    RenderTexture.active = prevRt;
                    try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"{Mathf.RoundToInt(t * 1000):D5}.jpg"), seqRead.EncodeToJPG(85)); } catch (System.Exception) { }
                    Destroy(tex);
                    next += seqEvery;
                    continue;
                }
                yield return null;
            }
        }

        static IEnumerator Snap(string name)
        {
            // 동영상 모드(-motionlab-seqfps 30)는 원본 크기 PNG 를 건너뛴다 — 한 장에 0.1초쯤 멈춰 영상이 타격 순간마다 끊겼다(2026-10-15 사용자 「렉 걸리는 거냐」 — 게임이 아니라 캡처 탓)
            if (shotsSeq && seqEvery < 0.05f) { Debug.Log("[Capture] (동영상 모드 — PNG 건너뜀) " + name); yield break; }
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(shotsDir, name + ".png"), tex.EncodeToPNG()); }
            catch (System.Exception e) { Debug.LogWarning("[MotionLab] 캡처 실패 " + name + " — " + e.Message); }
            Destroy(tex);
            Debug.Log("[Capture] " + name);
        }

        void OnDestroy()
        {
            RestoreSpeed();
            Bolzena.UI.PointerInput.Simulated = false;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Leave();
            if (info == null) return;
            // 튼 이펙트 장은 시간차로 늘어난다 — 장 수가 바뀔 때만 글을 다시 짓는다
            var call = d != null ? d.LastFxCall : null;
            int n = call != null ? call.Fired.Count : 0;
            if (call != firedCall || n != firedN) { firedCall = call; firedN = n; fired = d != null ? d.LabFired() : ""; }
            string s = header + (fired != "" ? "\n<color=#9fe0ff>튼 이펙트(이름@ms, *=대상 쪽): " + fired + "</color>" : "");
            if (s != shown) { shown = s; info.text = s; }
        }

        void Build()
        {
            bh = Theme.C(50, 64);    // 단추 높이 — 폰 가로(기준 1280×720)에서 손가락 크기
            fs = Theme.C(19, 24);
            float pad = 12, listW = Theme.C(210, 240), left = pad + listW + pad;

            var cgo = new GameObject("MotionLab UI", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var sc = cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
            sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = Theme.Compact ? new Vector2(1280, 720) : new Vector2(1600, 900);
            sc.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            sc.referencePixelsPerUnit = 100;
            cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            root = (RectTransform)cgo.transform;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
                es.transform.SetParent(transform, false);
            }

            // 왼쪽 — 나가기 · 사도 목록
            var col = Ui.Rect("heroes", root);
            col.anchorMin = new Vector2(0, 0); col.anchorMax = new Vector2(0, 1); col.pivot = new Vector2(0, 1);
            col.offsetMin = new Vector2(pad, pad); col.offsetMax = new Vector2(pad + listW, -pad);
            var back = Btn.Make(col, "나가기", BtnStyle.PillRose, Leave, fs, "lab back");
            back.GetComponent<RectTransform>().Band(1, bh);
            var listArea = Ui.Rect("list", col).Fill(0, 0, 0, bh + 8);
            var lbg = Ui.Img(listArea, Theme.White, new Color(0, 0, 0, 0.45f), "bg"); lbg.rectTransform.Fill();
            var list = Ui.Scroll(listArea, out _);
            Ui.Col(list, 6, TextAnchor.UpperCenter, new RectOffset(6, 6, 6, 6), true, false);
            foreach (var h in Roster.Players.OrderBy(x => x.ko, System.StringComparer.Ordinal))
            {
                string id = h.CoreId;
                bool on = id == hero;
                var b = Btn.Make(list, h.ko, on ? BtnStyle.PillGold : BtnStyle.PillDark, () => { if (id != hero && !leaving) Enter(id); }, fs, "lab hero " + id);
                b.Pref(-1, bh);
            }

            // 위 — 점검 글
            infoBox = Ui.Rect("info", root);
            infoBox.anchorMin = new Vector2(0, 1); infoBox.anchorMax = new Vector2(1, 1); infoBox.pivot = new Vector2(0.5f, 1);
            float infoH = Theme.C(250, 220);
            infoBox.offsetMin = new Vector2(left, -pad - infoH); infoBox.offsetMax = new Vector2(-pad, -pad);
            var ibg = Ui.Img(infoBox, Theme.White, new Color(0, 0, 0, 0.55f), "bg"); ibg.rectTransform.Fill();
            info = Ui.Text(infoBox, header, fs * 0.82f, Theme.Ink, TextAlignmentOptions.TopLeft, false, "info text");
            info.rectTransform.Fill(14, 8, 14, 8);
            info.enableAutoSizing = true; info.fontSizeMin = 9; info.fontSizeMax = info.fontSize;
            info.lineSpacing = 0;

            // 아래 — 단추 세 줄
            var bot = Ui.Rect("buttons", root);
            bot.anchorMin = new Vector2(0, 0); bot.anchorMax = new Vector2(1, 0); bot.pivot = new Vector2(0.5f, 0);
            float botH = bh * 3 + 8 * 2;
            bot.offsetMin = new Vector2(left, pad); bot.offsetMax = new Vector2(-pad, pad + botH);
            Ui.Col(bot, 8, TextAnchor.LowerLeft, null, true, false);

            var r1 = Row(bot, "moves");
            Act(r1, "평타", BattleDirector.LabMove.Attack1);
            Act(r1, "강공", BattleDirector.LabMove.Attack2);
            Act(r1, "스킬", BattleDirector.LabMove.Skill1);
            Act(r1, "고학년", BattleDirector.LabMove.Ult);
            Act(r1, "추가 공격", BattleDirector.LabMove.Extra);
            Act(r1, "피격", BattleDirector.LabMove.Hurt);
            bShotKind = Small(r1, "종류 " + ShotNames[0], () => { shotKind = (shotKind + 1) % ShotKinds.Length; bShotKind.SetLabel("종류 " + ShotNames[shotKind]); Marks(); });
            bVolley = Small(r1, "1발", () => { volley = (volley + 1) % Volleys.Length; bVolley.SetLabel(Volleys[volley] + "발"); Marks(); });
            Small(r1, "쉬기", () => Go(new Req { K = Kind.Idle, Label = "쉬는 동작" }));

            var r2 = Row(bot, "opts");
            bOne = Small(r2, "적 1명", () => { all = false; Marks(); });
            bAll = Small(r2, "적 전체", () => { all = true; Marks(); });
            foreach (var n in new[] { 1, 2, 3, 5 }) { int nn = n; bHits[n] = Small(r2, n + "타", () => { hits = nn; Marks(); }); }
            bRepeat = Small(r2, "반복", () => { repeat = !repeat; Marks(); if (repeat && !busy && last.HasValue) Go(last.Value); });
            bSlow = Small(r2, "느리게", () => SetSlow(!slow));
            bCutin = Small(r2, "컷인", () => { cutin = !cutin; Marks(); });
            bInfo = Small(r2, "글", () => { infoOn = !infoOn; infoBox.gameObject.SetActive(infoOn); Marks(); });

            // 셋째 줄 — 스파인에 있는 애니 이름 전부(가로로 넘김) · 누르면 그 애니만 그대로(1배속)
            var area = Ui.Rect("anims", bot); area.Pref(-1, bh);
            var abg = Ui.Img(area, Theme.White, new Color(0, 0, 0, 0.4f), "bg"); abg.rectTransform.Fill();
            animContent = Ui.Scroll(area, out _, true);
            Ui.Row(animContent, 6, TextAnchor.MiddleLeft, new RectOffset(6, 6, 3, 3), false, true);
            Marks();
        }

        RectTransform Row(RectTransform parent, string name)
        {
            var r = Ui.Rect(name, parent);
            Ui.Row(r, 8, TextAnchor.MiddleLeft, null, true, true);
            r.Pref(-1, bh);
            return r;
        }

        Btn Small(RectTransform row, string label, System.Action go)
        {
            var b = Btn.Make(row, label, BtnStyle.PillDark, go, fs * 0.9f, "lab " + label);
            b.Pref(60, bh, 1);
            return b;
        }

        void Act(RectTransform row, string label, BattleDirector.LabMove mv) =>
            Small(row, label, () => Go(new Req { K = Kind.Move, Mv = mv, Label = label }));

        // 켠 것은 밝은 바탕 · 금 글, 끈 것은 어둡게
        void Marks()
        {
            void M(Btn b, bool on)
            {
                if (b == null) return;
                b.SetColor(on ? Color.white : new Color(0.6f, 0.6f, 0.68f));
                if (b.Label) b.Label.color = on ? Theme.Gold : Theme.Ink;
            }
            M(bOne, !all); M(bAll, all);
            foreach (var kv in bHits) M(kv.Value, kv.Key == hits);
            M(bRepeat, repeat); M(bSlow, slow); M(bCutin, cutin); M(bInfo, infoOn);
            if (ready && !busy) header = Opts() + (d != null && d.LabInfo != "" ? "\n" + d.LabInfo : "");
        }

        void SetSlow(bool on)
        {
            slow = on;
            if (on) { if (speedWas < 0) speedWas = Clock.Speed; Clock.Speed = 0.25f; }
            else RestoreSpeed();
            Marks();
        }

        string Opts() =>
            $"<color=#ffd27a>{(Roster.OfCore(hero)?.ko ?? hero)}</color> · 대상 {(all ? "적 전체" : "적 1명")} · {hits}타 · 반복 {(repeat ? "켬" : "끔")} · 배속 {Clock.Speed:0.##} · 컷인 {(cutin ? "켬" : "끔")} · 추가 공격 {ShotNames[shotKind]}({ShotKinds[shotKind]}) {Volleys[volley]}발";

        void FillAnims()
        {
            var u = d.Heroes.Count > 0 ? d.Heroes[0] : null;
            var names = new List<string>();
            if (u != null && u.SpineArt && u.SkelData != null) foreach (var a in u.SkelData.Animations) names.Add(a.Name);
            names.Sort(System.StringComparer.Ordinal);
            if (names.Count == 0) { var t = Ui.Text(animContent, "  스파인 없음(그림 대역) — 애니 목록이 없습니다", fs * 0.8f, Theme.Dim); t.Pref(600, bh - 6); return; }
            foreach (var n in names)
            {
                string nn = n;
                var b = Btn.Make(animContent, n, BtnStyle.PillDark, () => Go(new Req { K = Kind.Raw, Anim = nn, Label = nn }), fs * 0.75f, "lab anim " + n);
                b.Pref(Mathf.Clamp(n.Length * fs * 0.5f + 34, 90, 380), bh - 6);
            }
        }

        List<int> Targets()
        {
            var r = new List<int>();
            int n = all ? d.Enemies.Count : Mathf.Min(1, d.Enemies.Count);
            for (int i = 0; i < n; i++) r.Add(i);
            return r;
        }

        // 누르면 — 틀고 있으면 끝난 뒤 이어서(하나만 기다린다)
        void Go(Req r)
        {
            if (!ready || leaving || d == null) return;
            last = r;
            if (busy) { queued = r; return; }
            StartCoroutine(PlayLoop(r));
        }

        IEnumerator PlayLoop(Req cur)
        {
            busy = true;
            while (!leaving)
            {
                yield return PlayOnce(cur);
                if (queued.HasValue) { cur = queued.Value; queued = null; continue; }
                if (!repeat) break;
                yield return new WaitForSecondsRealtime(0.4f);
                if (queued.HasValue) { cur = queued.Value; queued = null; }
                else if (!repeat) break;   // 쉬는 사이에 반복을 껐다
            }
            busy = false;
        }

        IEnumerator PlayOnce(Req r)
        {
            var u = d.Heroes.Count > 0 ? d.Heroes[0] : null;
            if (u == null) yield break;
            header = Opts() + $"\n<color=#ffd27a>트는 중 — {r.Label}</color>";
            if (r.K == Kind.Move)
            {
                d.LabInfo = "";
                bool shot = r.Mv == BattleDirector.LabMove.Extra;
                int n = shot && Volleys[volley] > 1 ? Volleys[volley] : hits;   // 여러 발을 고르면 그 수(적 전체면 번갈아), 아니면 타수
                yield return d.LabPlay(r.Mv, Targets(), n, cutin, 10, ShotKinds[shotKind]);
                while (Time.time < d.LabDoneAt && !leaving) yield return null;
                float w = 0;
                while (u && u.Feet != u.Home && w < 3f && !leaving) { w += Time.unscaledDeltaTime; yield return null; }   // 돌아오는 중이면 끝까지
                header = Opts() + "\n" + d.LabInfo;
            }
            else if (r.K == Kind.Idle)
            {
                u.Idle();
                header = Opts() + "\n<b>쉬는 동작</b> — UnitView.Idle(변신 중이면 그 꼬리판)";
                yield return new WaitForSecondsRealtime(0.3f);
            }
            else
            {
                // 애니 이름 그대로 — 이펙트 · 타격 없이 몸짓만(어떤 이펙트를 고를지는 글로)
                string hid = d.Battle.Snapshot.Heroes[0].Id;
                float dur = u.Play(r.Anim, 1f);
                var marks = u.Strikes(r.Anim);
                var plan = Bolzena.Fx.FxRules.CardPlan(hid, r.Anim, null);
                var pn = plan.Select(p => p.Name + "[" + p.At + "]").ToList();
                header = Opts() + $"\n<b>애니 그대로</b> {r.Anim} — {dur:F2}초(1배속 · 게임 시계, 몸짓만 — 이펙트 · 타격 · 소리 없음)"
                       + $"\n때리는 순간(스파인 이벤트): {string.Join(" · ", marks.Select(x => x.ToString("F2") + "초"))}"
                       + $"\n이 이름이면 이펙트 갈래 [{string.Join(" · ", Bolzena.Fx.FxRules.CardKinds(r.Anim, null))}] → {(pn.Count > 0 ? string.Join(", ", pn) : "없음")}";
                float t0 = Time.time;
                while (Time.time - t0 < dur && !leaving) yield return null;
            }
        }

        void Leave()
        {
            if (leaving || loading) return;
            leaving = true;
            RestoreSpeed();
            Bolzena.UI.PointerInput.Simulated = false;
            CoreBattle.Fixture.Override = null;
            Clock.Reset();
            string back = hero;
            void Back(Scene s, LoadSceneMode m)
            {
                SceneManager.sceneLoaded -= Back;
                var f = Flow.Me;
                if (f == null) return;
                f.Stage.Canvas.enabled = true;
                f.Dex(f.Lobby);
                f.StartCoroutine(Reopen(f, back));
            }
            SceneManager.sceneLoaded += Back;
            SceneManager.LoadScene(BattleBridge.RunScene);
        }

        // 도감이 선 뒤 그 사도의 상세를 다시 연다(들어온 자리로)
        static IEnumerator Reopen(Flow f, string coreId)
        {
            yield return null;
            while (f.Stage.Busy) yield return null;
            var h = Roster.OfCore(coreId);
            if (h == null || !h.Playable || f.Stage.Current != "dex") yield break;
            f.HeroDetail(h.key, Roster.Players.Select(x => x.key).ToList(), null);
        }
    }
}
