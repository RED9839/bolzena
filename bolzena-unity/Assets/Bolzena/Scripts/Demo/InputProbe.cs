using System;
using System.Collections;
using System.IO;
using Bolzena.Battle;
using Bolzena.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bolzena.Demo
{
    // 사람 입력 재현(-inputprobe) — 가짜 손가락(PointerInput.Simulated)이 아니라 Input System 에 마우스 이벤트를 넣어
    // 사람이 끄는 것과 같은 길(Mouse.current → PointerInput → HandView → BattleDirector → 코어)로 카드를 낸다.
    // 데스크톱의 진짜 커서는 건드리지 않는다(가상 마우스 장치). 싸움마다: 낼 수 있는 카드를 적에게 끌어 놓고, 없으면 턴 종료.
    // 결과는 로그([Probe]) · Captures/probe/*.png. 싸움 하나가 끝나면(또는 180초) 나간다.
    //   Bolzena.exe -battle -inputprobe [-party a,b,c -foes x,y]      전투만
    //   Bolzena.exe -demo -humanfight -inputprobe                      판 화면은 데모가 넘기고 첫 싸움을 이 손으로
    public class InputProbe : MonoBehaviour
    {
        static InputProbe me;
        Mouse mouse;
        string dir;
        int shotNo, played, refused, ends, drops, aimOk, aimMiss, skillOk, skillMiss, epiphanies, epiOk, epiStuck;
        Keyboard keyboard;

        IEnumerator Press(Key k)
        {
            if (keyboard == null) keyboard = InputSystem.AddDevice<Keyboard>("ProbeKeyboard");
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(k)); yield return Frames(3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return Frames(3);
        }
        readonly System.Collections.Generic.List<LineRenderer> lines = new System.Collections.Generic.List<LineRenderer>();

        static string Fmt(Vector3 v) => $"({v.x:F1},{v.y:F1})";

        static Bounds ArtBounds(Bolzena.View.UnitView u)
        {
            var mr = u.GetComponentInChildren<MeshRenderer>();
            return mr != null ? mr.bounds : new Bounds(u.transform.position, Vector3.one);
        }

        // 판정 상자(노랑) · 그림 경계(하늘) 를 화면에 그린다 — 캡처로 겹침을 본다
        void LateUpdate()
        {
            var d = BattleDirector.I;
            int n = 0;
            if (d != null && d.Enemies.Count > 0)
                for (int i = 0; i < d.Enemies.Count; i++)
                {
                    if (d.Enemies[i] == null || d.Battle.Snapshot.Enemies.Count <= i || d.Battle.Snapshot.Enemies[i].Dead) continue;
                    var r = d.EnemyBox(i);
                    Box(n++, r.min, r.max, new Color(1f, 0.9f, 0.1f));
                    var b = ArtBounds(d.Enemies[i]);
                    Box(n++, b.min, b.max, new Color(0.3f, 0.85f, 1f));
                }
            for (int k = n; k < lines.Count; k++) if (lines[k]) lines[k].enabled = false;
        }

        void Box(int k, Vector2 a, Vector2 b, Color c)
        {
            while (lines.Count <= k)
            {
                var go = new GameObject("probe box");
                DontDestroyOnLoad(go);
                var l = go.AddComponent<LineRenderer>();
                l.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Bolzena/Sprite"));
                l.loop = true; l.positionCount = 4; l.widthMultiplier = 0.04f; l.sortingOrder = 2000; l.useWorldSpace = true;
                lines.Add(l);
            }
            var lr = lines[k];
            if (!lr) return;
            lr.enabled = true;
            lr.startColor = lr.endColor = c;
            lr.SetPositions(new[] { new Vector3(a.x, a.y, 0), new Vector3(b.x, a.y, 0), new Vector3(b.x, b.y, 0), new Vector3(a.x, b.y, 0) });
        }

        public static void Install()
        {
            if (me != null || Array.IndexOf(Environment.GetCommandLineArgs(), "-inputprobe") < 0) return;
            var go = new GameObject("InputProbe");
            DontDestroyOnLoad(go);
            me = go.AddComponent<InputProbe>();
        }

        void Start()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // 창이 뒤에 있어도
            mouse = InputSystem.AddDevice<Mouse>("ProbeMouse");
            mouse.MakeCurrent();
            // 진짜 마우스 · 터치는 끈다 — 사람이 그 사이 마우스를 움직이면 Mouse.current 가 진짜 장치로 넘어가 가상 입력이 먹히지 않았다
            foreach (var dev in InputSystem.devices.ToArray())
                if (dev is Pointer && dev != mouse) InputSystem.DisableDevice(dev);
            dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures", "probe"));
            Directory.CreateDirectory(dir);
            Debug.Log("[Probe] 가상 마우스로 사람 입력을 흉내 냅니다 → " + dir);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-sound") < 0) AudioListener.volume = 0f;   // 점검은 음소거
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-holdprobe") >= 0) StartCoroutine(HoldRun());
            else StartCoroutine(Run());
            StartCoroutine(Limit());
        }

        IEnumerator Limit()
        {
            yield return new WaitForSecondsRealtime(240f);
            Debug.LogError("[Probe] 240초 — 끝냅니다");
            Application.Quit(5);
        }

        void Set(Vector2 screen, bool left)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = (ushort)(left ? 1 : 0) });
        }

        Vector2 Scr(Vector3 world) => Camera.main.WorldToScreenPoint(world);

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Drag(Vector3 fromWorld, Vector3 toWorld)
        {
            Vector2 a = Scr(fromWorld), b = Scr(toWorld);
            Set(a, false); yield return Frames(12);          // 올려 두기
            Set(a, true); yield return Frames(3);
            for (int i = 1; i <= 24; i++) { Set(Vector2.Lerp(a, b, i / 24f), true); yield return null; }
            yield return Frames(12);
            Set(b, false); yield return Frames(4);
        }

        IEnumerator Click(Vector3 world)
        {
            var p = Scr(world);
            Set(p, false); yield return Frames(6);
            Set(p, true); yield return Frames(3);
            Set(p, false); yield return Frames(4);
        }

        void Shot(string name)
        {
            StartCoroutine(ShotCo(name));
        }
        IEnumerator ShotCo(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, $"{++shotNo:D2}_{name}.png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        // 연속 촬영(-probeburst) — 보스가 45% 아래이고 곁에 다른 적(소환물)이 있는 동안부터 싸움이 끝나고 3초 뒤까지 4프레임마다 한 장(jpg).
        //   보스와 함께 쓰러지는 소환물의 화면(배치 · 체력바 · 사라짐)을 보려고 — 보스가 지속 피해 · 반격 등 어느 때 쓰러져도 담긴다
        IEnumerator Watch(BattleDirector d)
        {
            while (!d.Over)
            {
                var es = d.Battle.Snapshot.Enemies;
                if (es.Exists(e => e.Boss && !e.Dead && e.Hp <= e.MaxHp * 0.45f) && es.Exists(e => !e.Boss && !e.Dead)) break;
                if (!es.Exists(e => e.Boss) && es.Exists(e => e.Dead) && es.Exists(e => !e.Dead)) break;   // 보스 없는 싸움 — 하나가 쓰러진 뒤(되살아남 확인)
                yield return null;
            }
            var sub = Path.Combine(dir, "burst");
            Directory.CreateDirectory(sub);
            float after = 0; int n = 0, f = 0;
            while (after < 3f)
            {
                yield return new WaitForEndOfFrame();
                if (d.Over) after += Time.unscaledDeltaTime;
                if (f++ % 4 != 0) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(sub, $"{++n:D4}.jpg"), tex.EncodeToJPG(75));
                Destroy(tex);
            }
        }

        // ── -holdprobe: 손패 길게 누르기(카제나식) — 가상 마우스 · 가상 터치로 같은 장면을 연속 촬영(3프레임마다 jpg) ──
        //   ① 꾹 누르기 → 확대 → 떼기(닫힘 · 안 냄)  ② 꾹 누르기 → 확대 → 끌어서 적 타격  ③ 짧게 누르기 = 고르기(다시 눌러 내려놓기)
        //   결과: [Hold] 로그 · Captures/probe/hold_<입력>_<장면>/NNNN.jpg
        Touchscreen touch;
        bool touchMode;
        int holdFail;

        void Ptr(Vector2 screen, bool held, bool began = false)
        {
            if (!touchMode) { Set(screen, held); return; }
            var ph = began ? UnityEngine.InputSystem.TouchPhase.Began : held ? UnityEngine.InputSystem.TouchPhase.Moved : UnityEngine.InputSystem.TouchPhase.Ended;
            touch.MakeCurrent();
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = ph, position = screen, pressure = held ? 1 : 0 });
            // Windows 는 터치로 마우스 자리도 옮긴다 — 가상 마우스도 같은 자리에(단추는 안 누름)
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 0 });
        }

        IEnumerator Burst(string tag, Func<bool> until)
        {
            var sub = Path.Combine(dir, tag);
            if (Directory.Exists(sub)) foreach (var f in Directory.GetFiles(sub)) File.Delete(f);
            Directory.CreateDirectory(sub);
            int n = 0, fr = 0;
            while (!until())
            {
                yield return new WaitForEndOfFrame();
                if (fr++ % 3 != 0) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(sub, $"{++n:D4}.jpg"), tex.EncodeToJPG(80));
                Destroy(tex);
            }
        }

        void Check(bool ok, string what)
        {
            if (!ok) holdFail++;
            Debug.Log($"[Hold] {(ok ? "맞음" : "틀림(!)")} — {what}");
        }

        string ZoomInfo(BattleDirector d)
        {
            var cam = Camera.main;
            var c = CardZoom.At;
            float hw = cam.orthographicSize * cam.aspect;
            return $"확대 {(CardZoom.Shown ? "켜짐" : "꺼짐")} · 가운데 x {c.x - cam.transform.position.x:F2}(화면 반폭 {hw:F2}) · y {c.y:F2}(위 끝 {c.y + CardView.H * CardZoom.Scale / 2:F2} · 아래 끝 {c.y - CardView.H * CardZoom.Scale / 2:F2} · 화면 위 {cam.orthographicSize:F2} · 손패 위 {HandView.Bottom + CardView.H * d.Hand.HandScale:F2}) · 배율 {CardZoom.Scale:F2}(손패 {d.Hand.HandScale:F2} → {CardZoom.Scale / Mathf.Max(0.01f, d.Hand.HandScale):F2}배) · 판 {CardZoom.PanelCount}개 {(CardZoom.PanelsRight ? "오른쪽" : "왼쪽")}";
        }

        IEnumerator HoldRun()
        {
            float t = 0;
            while ((BattleDirector.I == null || !BattleDirector.I.WaitingInput) && t < 150f) { t += Time.unscaledDeltaTime; yield return null; }
            var d = BattleDirector.I;
            if (d == null) { Debug.LogError("[Probe] 전투가 열리지 않았습니다"); Application.Quit(6); yield break; }
            touch = InputSystem.AddDevice<Touchscreen>("ProbeTouch");
            Debug.Log($"[Hold] 화면 {Screen.width}×{Screen.height} · 폰 {Tone.Compact}");
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (var mode in new[] { "mouse", "touch" })
            {
                touchMode = mode == "touch";
                // ① 꾹 누르기 → 떼기
                yield return WaitReady(d);
                int i = PickCard(d);
                if (i < 0) { Debug.LogError("[Hold] 낼 카드 없음"); break; }
                var cv = d.Hand.Cards[i];
                int hand0 = d.Battle.Snapshot.Hand.Count, ap0 = d.Battle.Snapshot.Ap;
                Vector2 at = Scr(cv.transform.position + new Vector3(0, -0.3f, 0));
                bool done = false;
                StartCoroutine(Burst($"hold_{mode}_release", () => done));
                Ptr(at, false); yield return Frames(4);
                Ptr(at, true, true); yield return Frames(2);
                float held = 0; while (held < 0.15f) { Ptr(at, true); held += Time.unscaledDeltaTime; yield return null; }
                Check(!d.Hand.HoldZooming && !CardZoom.Shown, $"{mode} 0.15초 — 아직 확대 안 함");
                while (held < 0.7f) { Ptr(at, true); held += Time.unscaledDeltaTime; yield return null; }
                Debug.Log($"[Hold] {mode} 꾹 누름 「{cv.Info.Name}」 — {ZoomInfo(d)}");
                var cam = Camera.main;
                Check(d.Hand.HoldZooming && CardZoom.Shown && Mathf.Abs(CardZoom.At.x - cam.transform.position.x) < 0.05f && CardZoom.At.y > 0 && CardZoom.PanelsRight, $"{mode} 꾹 누름 — 가운데 위 확대 · 판 오른쪽");
                Shot($"hold_{mode}_zoom");
                yield return Frames(3);
                Ptr(at, false); yield return Frames(4);
                yield return new WaitForSecondsRealtime(0.4f);
                Check(!CardZoom.Shown && !d.Hand.HasSelection && d.Battle.Snapshot.Hand.Count == hand0 && d.Battle.Snapshot.Ap == ap0 && d.WaitingInput, $"{mode} 그 자리에서 뗌 — 닫힘 · 안 냄 · 고르지 않음");
                done = true; yield return Frames(2);

                // ② 꾹 누르기 → 끌어서 적 타격
                yield return WaitReady(d);
                i = PickCard(d);
                if (i < 0) { Debug.LogError("[Hold] 낼 카드 없음"); break; }
                cv = d.Hand.Cards[i];
                string id = cv.Info.Id;
                hand0 = d.Battle.Snapshot.Hand.Count; ap0 = d.Battle.Snapshot.Ap;
                int target = d.FirstAliveEnemy();
                bool aims = cv.Info.Target == TargetKind.Enemy;
                Vector3 to = aims ? ArtBounds(d.Enemies[target]).center : cv.Info.Target == TargetKind.Ally ? d.Hand.AllyDrop(cv.Info) : new Vector3(0.3f, 0.6f, 0);
                at = Scr(cv.transform.position + new Vector3(0, -0.3f, 0));
                Vector2 b = Scr(to);
                done = false;
                StartCoroutine(Burst($"hold_{mode}_drag", () => done));
                Ptr(at, false); yield return Frames(4);
                Ptr(at, true, true); yield return Frames(2);
                held = 0; while (held < 0.6f) { Ptr(at, true); held += Time.unscaledDeltaTime; yield return null; }
                Check(d.Hand.HoldZooming && CardZoom.Shown, $"{mode} 끌기 전 확대 — {ZoomInfo(d)}");
                for (int k = 1; k <= 30; k++)
                {
                    Ptr(Vector2.Lerp(at, b, Ease.InOutCubic(k / 30f)), true); yield return null;
                    if (k == 8) Check(!CardZoom.Shown && d.Hand.Held == i, $"{mode} 움직이기 시작 — 확대 닫고 그 카드를 끔(끄는 카드 {d.Hand.Held})");
                }
                for (int k = 0; k < 12; k++) { Ptr(b, true); yield return null; }
                Check(d.Hand.Aim >= 0 || !aims, $"{mode} 적 위 — 겨눔 {d.Hand.Aim}");
                Check(!aims || d.Hand.RingCount == 1, $"{mode} 끄는 동안 — 발밑 고리 {d.Hand.RingCount}개");
                if (aims) Shot($"drag_{mode}_aim");
                Ptr(b, false); yield return Frames(3);
                float w = 0;
                while (d.WaitingInput && w < 1.5f) { w += Time.unscaledDeltaTime; yield return null; }
                w = 0;
                while (!d.WaitingInput && !d.Over && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
                var s = d.Battle.Snapshot;
                Check(s.Over || s.Ap != ap0 || s.Hand.Count != hand0 || !s.Hand.Exists(c => c.Id == id), $"{mode} 꾹 누른 뒤 끌어 놓기 — 카드 {id}{(aims ? "(적 대상)" : "")} 나감 · AP {ap0}→{s.Ap}");
                done = true; yield return Frames(2);
                if (d.Over) break;
                if (!touchMode) { yield return KindsRun(d); if (d.Over) break; }

                // ③ 짧게 누르기 = 고르기, 다시 누르면 내려놓기(적 카드) — 지금 동작 그대로
                yield return WaitReady(d);
                i = PickCard(d);
                if (i < 0) continue;
                cv = d.Hand.Cards[i];
                at = Scr(cv.transform.position + new Vector3(0, -0.3f, 0));
                Ptr(at, false); yield return Frames(4);
                Ptr(at, true, true); yield return Frames(4);
                Ptr(at, false); yield return Frames(12);
                Check(d.Hand.HasSelection && !CardZoom.Shown, $"{mode} 짧게 누름 — 고름 · 확대 없음");
                Shot($"tap_{mode}_select");
                bool enemyCard = d.Hand.Cards[i].Info.Target == TargetKind.Enemy;
                at = Scr(d.Hand.Cards[i].transform.position);
                if (enemyCard)
                {
                    Ptr(at, false); yield return Frames(4);
                    Ptr(at, true, true); yield return Frames(4);
                    Ptr(at, false); yield return Frames(12);
                    Check(!d.Hand.HasSelection, $"{mode} 다시 누름 — 내려놓기({d.Hand.LastTap} → 고른 {d.Hand.Held})");
                }
                if (d.Hand.HasSelection) d.Hand.Cancel();
                // 손에서 비켜 둔다(올려 두기 확대가 다음 장면에 남지 않게)
                Ptr(Scr(new Vector3(0, 2.5f, 0)), false); yield return Frames(30);
            }
            Debug.Log($"[Hold] 끝 — 틀림 {holdFail}");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(holdFail > 0 ? 7 : 0);
        }

        // 끌다가 빈 곳에 놓기(취소 — 손패로 돌아감) · 적 전체 카드(모든 적 발밑 고리) · 아군 카드(사도 위 초록 고리)
        IEnumerator KindsRun(BattleDirector d)
        {
            // 취소
            yield return WaitReady(d);
            int i = PickCard(d);
            if (i >= 0)
            {
                var cv = d.Hand.Cards[i];
                int hand0 = d.Battle.Snapshot.Hand.Count, ap0 = d.Battle.Snapshot.Ap;
                Vector2 at = Scr(cv.transform.position + new Vector3(0, -0.3f, 0)), b = Scr(new Vector3(-0.5f, 3.6f, 0));
                bool done = false;
                StartCoroutine(Burst("drag_cancel", () => done));
                Ptr(at, false); yield return Frames(4);
                Ptr(at, true, true); yield return Frames(2);
                for (int k = 1; k <= 24; k++) { Ptr(Vector2.Lerp(at, b, k / 24f), true); yield return null; }
                yield return Frames(10);
                Check(d.Hand.Aim < 0 && d.Hand.RingCount == 0, $"빈 곳 위 — 겨눔 없음 · 고리 {d.Hand.RingCount}");
                Ptr(b, false); yield return Frames(30);
                Check(d.Battle.Snapshot.Hand.Count == hand0 && d.Battle.Snapshot.Ap == ap0 && d.Hand.Held < 0, "빈 곳에 놓음 — 취소 · 손패로 돌아감");
                done = true; yield return Frames(2);
            }
            foreach (var kind in new[] { TargetKind.AllEnemies, TargetKind.Ally })
            {
                yield return WaitReady(d);
                if (d.Over) yield break;
                int j = -1;
                for (int k = 0; k < d.Hand.Cards.Count; k++) if (d.Battle.CanPlay(k, out _) && d.Hand.Cards[k].Info.Target == kind) { j = k; break; }
                if (j < 0) { Debug.Log($"[Hold] {kind} 카드가 손에 없음 — 건너뜀"); continue; }
                var cv = d.Hand.Cards[j];
                string id = cv.Info.Id;
                int hand0 = d.Battle.Snapshot.Hand.Count, ap0 = d.Battle.Snapshot.Ap;
                Vector3 to = kind == TargetKind.Ally ? d.Hand.AllyDrop(cv.Info) : new Vector3(0.3f, 1.2f, 0);
                Vector2 at = Scr(cv.transform.position + new Vector3(0, -0.3f, 0)), b = Scr(to);
                bool done = false;
                string tag = kind == TargetKind.Ally ? "drag_ally" : "drag_all";
                StartCoroutine(Burst(tag, () => done));
                Ptr(at, false); yield return Frames(4);
                Ptr(at, true, true); yield return Frames(2);
                for (int k = 1; k <= 26; k++) { Ptr(Vector2.Lerp(at, b, Ease.InOutCubic(k / 26f)), true); yield return null; }
                yield return Frames(12);
                int alive = 0; foreach (var e in d.Battle.Snapshot.Enemies) if (!e.Dead) alive++;
                Check(kind == TargetKind.Ally ? d.Hand.AllyAim >= 0 && d.Hand.RingCount == 1 : d.Hand.RingCount == alive, $"{kind} 위로 — 고리 {d.Hand.RingCount}개(산 적 {alive}) · 아군 겨눔 {d.Hand.AllyAim}");
                Shot(tag);
                Ptr(b, false); yield return Frames(3);
                float w = 0;
                while (d.WaitingInput && w < 1.5f) { w += Time.unscaledDeltaTime; yield return null; }
                w = 0;
                while (!d.WaitingInput && !d.Over && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
                var s = d.Battle.Snapshot;
                Check(s.Over || s.Ap != ap0 || s.Hand.Count != hand0 || !s.Hand.Exists(c => c.Id == id), $"{kind} 카드 {id} 놓기 — 나감 · AP {ap0}→{s.Ap}");
                done = true; yield return Frames(2);
            }
        }

        IEnumerator WaitReady(BattleDirector d)
        {
            float w = 0;
            while (!d.WaitingInput && !d.Over && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.4f);
        }

        // 낼 수 있는 카드 — 적을 고르는 카드 먼저
        int PickCard(BattleDirector d)
        {
            int any = -1;
            for (int i = 0; i < d.Hand.Cards.Count; i++)
            {
                if (!d.Battle.CanPlay(i, out _)) continue;
                if (d.Hand.Cards[i].Info.Target == TargetKind.Enemy) return i;
                if (any < 0) any = i;
            }
            return any;
        }

        IEnumerator Run()
        {
            // 전투 장면 · 입력 대기까지
            float t = 0;
            while ((BattleDirector.I == null || !BattleDirector.I.WaitingInput) && t < 150f) { t += Time.unscaledDeltaTime; yield return null; }
            var d = BattleDirector.I;
            if (d == null) { Debug.LogError("[Probe] 전투가 열리지 않았습니다"); Application.Quit(6); yield break; }
            Debug.Log($"[Probe] 전투 입력 대기 — 가짜 손가락 {(PointerInput.Simulated ? "켜짐(!)" : "꺼짐")} · 판에서 넘어옴 {BattleBridge.Fight != null} · timeScale {Time.timeScale}");
            yield return new WaitForSecondsRealtime(0.5f);
            Shot("start");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-probeburst") >= 0) StartCoroutine(Watch(d));
            int turn = 1;
            for (int step = 0; step < 60 && !d.Over; step++)
            {
                float w = 0;
                while (!d.WaitingInput && !d.Over && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
                if (d.Over) break;
                yield return new WaitForSecondsRealtime(0.3f);
                int pick = -1;
                for (int i = 0; i < d.Hand.Cards.Count; i++) if (d.Battle.CanPlay(i, out _)) { pick = i; break; }
                if (pick >= 0)
                {
                    var cv = d.Hand.Cards[pick];
                    string id = cv.Info.Id;
                    int before = d.Battle.Snapshot.Hand.Count, ap = d.Battle.Snapshot.Ap;
                    int target = d.FirstAliveEnemy();
                    // 놓는 자리 — 적 그림(스파인 메시 경계) 안의 여러 곳을 돌아가며: 가운데 · 머리 쪽 · 오른쪽 가장자리 · 왼쪽 가장자리
                    string spot = cv.Info.Target == TargetKind.Ally ? "아군" : "스킬";
                    Vector3 to = cv.Info.Target == TargetKind.Ally ? d.Hand.AllyDrop(cv.Info) : new Vector3(0.3f, 0.6f, 0);
                    bool aims = cv.Info.Target == TargetKind.Enemy;
                    if (aims)
                    {
                        var art = ArtBounds(d.Enemies[target]);
                        int k = drops++ % 4;
                        spot = k == 0 ? "가운데" : k == 1 ? "머리 쪽" : k == 2 ? "오른쪽 끝" : "왼쪽 끝";
                        to = k == 0 ? art.center : k == 1 ? new Vector3(art.center.x, art.max.y - art.size.y * 0.12f, 0)
                           : k == 2 ? new Vector3(art.max.x - art.size.x * 0.1f, art.center.y, 0) : new Vector3(art.min.x + art.size.x * 0.1f, art.center.y, 0);
                        var box = d.EnemyBox(target);
                        Debug.Log($"[Probe] 적 {target} 그림 {Fmt(art.min)}~{Fmt(art.max)} · 판정 상자 {Fmt(box.min)}~{Fmt(box.max)} · 놓는 곳({spot}) {Fmt(to)} 상자 안 {box.Contains((Vector2)to)}");
                    }
                    yield return Drag(cv.transform.position + new Vector3(0, -0.3f, 0), to);
                    // 빛나는 카드였으면 신탁 창 — 첫 번째는 마우스로, 그다음은 키보드(숫자)로 고른다
                    w = 0;
                    while (EpiphanyWindow.Options == null && !d.WaitingInput && w < 2.5f) { w += Time.unscaledDeltaTime; yield return null; }
                    if (EpiphanyWindow.Options != null)
                    {
                        yield return new WaitForSecondsRealtime(0.4f);
                        Shot("epiphany_open");
                        var opts = EpiphanyWindow.Options;
                        bool byKey = epiphanies++ % 2 == 1;
                        if (byKey) yield return Press(Key.Digit1);
                        else
                        {
                            var o = opts[0].transform.position;
                            Set(Scr(o), false); yield return Frames(20);
                            yield return Click(o);
                        }
                        w = 0;
                        while (EpiphanyWindow.Options != null && w < 5f) { w += Time.unscaledDeltaTime; yield return null; }
                        bool closed = EpiphanyWindow.Options == null;
                        Debug.Log($"[Probe] 신탁 창 ({opts.Count}갈래) — {(byKey ? "키보드 1" : "마우스로 첫 카드 누름")} → {(closed ? "골라짐" : "그대로(!)")}");
                        if (closed) epiOk++; else { epiStuck++; Shot("epiphany_stuck"); }
                    }
                    // 낸 것이 규칙에 닿았나 — 연출이 끝나고 다시 입력을 기다릴 때 손 · AP 를 본다
                    w = 0;
                    while (d.WaitingInput && w < 1.5f) { w += Time.unscaledDeltaTime; yield return null; }
                    w = 0;
                    while (!d.WaitingInput && !d.Over && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
                    var s = d.Battle.Snapshot;
                    bool went = s.Over || s.Ap != ap || s.Hand.Count != before || !s.Hand.Exists(c => c.Id == id);
                    if (went) played++; else refused++;
                    Debug.Log($"[Probe] {turn}턴 카드 {id}({spot}) 끌어 놓기 → {(went ? "나감" : "안 나감(!)")} · AP {ap}→{s.Ap} · 손 {before}→{s.Hand.Count}");
                    if (aims) { if (went) aimOk++; else aimMiss++; } else { if (went) skillOk++; else skillMiss++; }
                    if (played == 1 && went) Shot("played_first");
                    if (!went) { Shot("refused_" + refused); if (refused >= 8) break; }
                    continue;
                }
                Shot($"turn{turn}_end");
                yield return Click(d.Hud.EndPos);
                ends++;
                Debug.Log($"[Probe] {turn}턴 종료 누름");
                turn++;
                w = 0;
                while (d.WaitingInput && w < 1.5f) { w += Time.unscaledDeltaTime; yield return null; }
            }
            yield return new WaitForSecondsRealtime(Array.IndexOf(Environment.GetCommandLineArgs(), "-probeburst") >= 0 ? 3.2f : 1f);   // 연속 촬영이 끝 3초를 담게
            Shot("end");
            Debug.Log($"[Probe] 끝 — 상자 {(BattleDirector.OldEnemyBox ? "옛것(-oldbox)" : "새것")} · 대상 카드 나감 {aimOk} / 안 나감 {aimMiss} · 대상 없는 카드 나감 {skillOk} / 안 나감 {skillMiss} · 신탁 창 골라짐 {epiOk} / 멈춤 {epiStuck} · 턴 종료 {ends} · 싸움 {(d.Over ? (d.Battle.Snapshot.Won ? "승리" : "패배") : "안 끝남")}");
            yield return new WaitForSecondsRealtime(0.8f);
            Application.Quit(refused > 0 ? 7 : 0);
        }
    }
}
