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
            dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures", "probe"));
            Directory.CreateDirectory(dir);
            Debug.Log("[Probe] 가상 마우스로 사람 입력을 흉내 냅니다 → " + dir);
            StartCoroutine(Run());
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
                    string spot = "스킬";
                    Vector3 to = new Vector3(0.3f, 0.6f, 0);
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
            yield return new WaitForSecondsRealtime(1f);
            Shot("end");
            Debug.Log($"[Probe] 끝 — 상자 {(BattleDirector.OldEnemyBox ? "옛것(-oldbox)" : "새것")} · 대상 카드 나감 {aimOk} / 안 나감 {aimMiss} · 대상 없는 카드 나감 {skillOk} / 안 나감 {skillMiss} · 신탁 창 골라짐 {epiOk} / 멈춤 {epiStuck} · 턴 종료 {ends} · 싸움 {(d.Over ? (d.Battle.Snapshot.Won ? "승리" : "패배") : "안 끝남")}");
            yield return new WaitForSecondsRealtime(0.8f);
            Application.Quit(refused > 0 ? 7 : 0);
        }
    }
}
