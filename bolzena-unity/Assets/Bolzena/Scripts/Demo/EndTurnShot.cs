using System;
using System.Collections;
using System.IO;
using System.Linq;
using Bolzena.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bolzena.Demo
{
    /// <summary>
    /// 턴 종료 단추 점검(-endturnshot &lt;이름&gt; -captures &lt;폴더&gt;, -battle 과 함께) — 음소거.
    ///   1) 상태별 모양을 얼려 두고 단추 둘레만 잘라 찍는다(보통 · 올림 · 누름 · 끝낼 때 · 끝낼 때(잔잔) · 잠김 · 적 차례 · 자동 전투)
    ///      + 전체 화면 두 장(보통 · 끝낼 때). 끝낼 때 맥동 폭(빛 고리 알파 최소~최대)을 보통 · 잔잔으로 재어 적는다.
    ///   2) 가상 마우스로 단추를 실제로 눌러 턴이 넘어가는지(올림 · 누름 상태도 실제 입력으로 찍는다)
    ///   3) 가상 터치로 한 번 더(폰 — 터치에선 올림이 켜지지 않아야 한다)
    /// 결과는 로그 [EndTurnShot] 줄 — 「통과」/「실패」.
    /// </summary>
    public class EndTurnShot : MonoBehaviour
    {
        string dir, nm;
        Mouse mouse;
        Touchscreen touch;
        int fails;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-endturnshot") < 0) return;
            var go = new GameObject("EndTurnShot");
            DontDestroyOnLoad(go);
            go.AddComponent<EndTurnShot>();
        }

        void Start()
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "-endturnshot");
            nm = i + 1 < a.Length && !a[i + 1].StartsWith("-") ? a[i + 1] : "endturn";
            int ci = Array.IndexOf(a, "-captures");
            dir = ci >= 0 && ci + 1 < a.Length ? a[ci + 1] : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures", "endturn"));
            Directory.CreateDirectory(dir);
            AudioListener.volume = 0f;   // 점검은 음소거
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // 창이 최소화 · 뒤에 있어도
            mouse = InputSystem.AddDevice<Mouse>("EndTurnMouse");
            touch = InputSystem.AddDevice<Touchscreen>("EndTurnTouch");
            foreach (var dev in InputSystem.devices.ToArray())
                if (dev is Pointer && dev != mouse && dev != touch) InputSystem.DisableDevice(dev);
            mouse.MakeCurrent();
            StartCoroutine(Run());
            StartCoroutine(Limit());
        }

        IEnumerator Limit()
        {
            yield return new WaitForSecondsRealtime(200f);
            Debug.LogError("[EndTurnShot] 200초 — 끝냅니다");
            Application.Quit(5);
        }

        static IEnumerator Real(float s) { yield return new WaitForSecondsRealtime(s); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Scr(Vector3 w) => Camera.main.WorldToScreenPoint(w);

        void MouseAt(Vector2 p, bool held)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = (ushort)(held ? 1 : 0) });
        }

        void TouchAt(Vector2 p, UnityEngine.InputSystem.TouchPhase ph)
        {
            touch.MakeCurrent();
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = ph, position = p, pressure = ph == UnityEngine.InputSystem.TouchPhase.Ended ? 0 : 1 });
        }

        void Check(bool ok, string what)
        {
            if (!ok) fails++;
            Debug.Log($"[EndTurnShot] {(ok ? "통과" : "실패")} — {what}");
        }

        // 단추 둘레 — 월드 상자 → 화면 픽셀(슈퍼사이즈 반영)
        IEnumerator Crop(EndTurnButton b, string state)
        {
            yield return new WaitForEndOfFrame();
            int ss = Capture.SuperSize;
            var tex = ScreenCapture.CaptureScreenshotAsTexture(ss);
            float k = Tone.K;
            var c = b.transform.position;
            float hw = Mathf.Max(1.0f, b.Half.x + 0.3f) * k, up = 0.95f * k, down = 1.05f * k;
            Vector2 lo = Scr(c + new Vector3(-hw, -down, 0)) * ss, hi = Scr(c + new Vector3(hw, up, 0)) * ss;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(lo.x), 0, tex.width - 1), y0 = Mathf.Clamp(Mathf.FloorToInt(lo.y), 0, tex.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(hi.x), x0 + 1, tex.width), y1 = Mathf.Clamp(Mathf.CeilToInt(hi.y), y0 + 1, tex.height);
            var crop = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGBA32, false);
            crop.SetPixels(tex.GetPixels(x0, y0, x1 - x0, y1 - y0));
            crop.Apply();
            File.WriteAllBytes(Path.Combine(dir, $"{nm}_{state}.png"), crop.EncodeToPNG());
            Destroy(crop);
            Destroy(tex);
        }

        IEnumerator Full(string state)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture(Capture.SuperSize);
            File.WriteAllBytes(Path.Combine(dir, $"{nm}_full_{state}.png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        // 끝낼 때 맥동 폭 — 빛 고리 알파를 2.5초 재어 최소 · 최대
        IEnumerator PulseRange(EndTurnButton b, string label)
        {
            float lo = 9, hi = -9, sLo = 9, sHi = -9;
            for (float t = 0; t < 2.5f; t += Time.unscaledDeltaTime)
            {
                float a = b.HaloA, s = b.FaceScale;
                lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a);
                sLo = Mathf.Min(sLo, s); sHi = Mathf.Max(sHi, s);
                yield return null;
            }
            Debug.Log($"[EndTurnShot] 맥동 {label} — 빛 고리 알파 {lo:F2}~{hi:F2}(폭 {hi - lo:F2}) · 크기 {sLo:F3}~{sHi:F3}");
            lastPulse = hi - lo;
        }
        float lastPulse;

        void SetState(EndTurnButton b, bool hover = false, bool pressed = false, bool ready = false, bool locked = false, bool enemy = false, bool auto = false)
        {
            b.Frozen = true;
            b.Hover = hover; b.Pressed = pressed; b.Ready = ready; b.Locked = locked; b.EnemyTurn = enemy; b.Auto = auto;
        }

        IEnumerator Run()
        {
            float t = 0;
            while ((BattleDirector.I == null || !BattleDirector.I.WaitingInput) && t < 150f) { t += Time.unscaledDeltaTime; yield return null; }
            var d = BattleDirector.I;
            if (d == null) { Debug.LogError("[EndTurnShot] 전투가 열리지 않았습니다"); Application.Quit(6); yield break; }
            var b = d.Hud.EndButton;
            MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.95f), false);   // 마우스는 화면 위쪽 빈 곳에
            yield return Real(1.8f);
            Debug.Log($"[EndTurnShot] 시작 — 모양 {b.Kind} · 화면 {Screen.width}x{Screen.height} · 폰 배율 {Tone.K} · 슈퍼사이즈 {Capture.SuperSize}");

            // ── 1) 상태별 모양(얼려 두고) ──
            SetState(b); yield return Real(0.8f); yield return Crop(b, "1_normal"); yield return Full("normal");
            SetState(b, hover: true); yield return Real(0.6f); yield return Crop(b, "2_hover");
            SetState(b, hover: true, pressed: true); yield return Real(0.4f); yield return Crop(b, "3_pressed");
            SetState(b, ready: true); yield return Real(1.2f);
            yield return PulseRange(b, "보통");
            float full = lastPulse;
            yield return Crop(b, "4_ready"); yield return Full("ready");
            EndTurnButton.ForceCalm = true; yield return Real(0.6f);
            yield return PulseRange(b, "잔잔(저사양 · 움직임 줄이기)");
            Check(lastPulse < full * 0.6f, $"잔잔 모드 맥동이 약하다({lastPulse:F2} < {full:F2}×0.6)");
            yield return Crop(b, "5_ready_calm");
            EndTurnButton.ForceCalm = false;
            SetState(b, locked: true); yield return Real(1.0f); yield return Crop(b, "6_locked");
            SetState(b, locked: true, enemy: true); yield return Real(0.8f); yield return Crop(b, "7_enemy");
            SetState(b, auto: true); yield return Real(1.2f); yield return Crop(b, "8_auto");
            b.Frozen = false;
            yield return Real(0.6f);

            // ── 2) 가상 마우스로 실제 누르기 ──
            int turn0 = d.Battle.Snapshot.Turn;
            var p = Scr(d.Hud.EndPos);
            MouseAt(p, false); yield return Frames(10);
            Check(b.Hover && d.Hud.EndHover, "마우스를 올리면 올림 상태");
            yield return Real(0.3f); yield return Crop(b, "r1_hover");
            MouseAt(p, true); yield return Frames(4);
            Check(b.Pressed, "누르는 동안 누름 상태");
            yield return Crop(b, "r2_press");
            MouseAt(p, false); yield return Frames(3);
            MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.95f), false);
            float w = 0;
            while (!d.EnemyTurn && w < 3f) { w += Time.unscaledDeltaTime; yield return null; }
            Check(d.EnemyTurn, "마우스 클릭 → 적 차례로 넘어감");
            yield return Real(0.5f); yield return Crop(b, "r3_enemy_turn"); yield return Full("enemy_turn");
            w = 0;
            while ((!d.WaitingInput || d.EnemyTurn) && !d.Over && w < 40f) { w += Time.unscaledDeltaTime; yield return null; }
            int turn1 = d.Battle.Snapshot.Turn;
            Check(turn1 == turn0 + 1 || d.Over, $"마우스로 턴이 넘어감 {turn0} → {turn1}");

            // ── 3) 가상 터치로 한 번 더 ──
            if (!d.Over)
            {
                yield return Real(0.8f);
                p = Scr(d.Hud.EndPos);
                TouchAt(p, UnityEngine.InputSystem.TouchPhase.Began); yield return Frames(4);
                Check(!d.Hud.EndHover, "터치 중엔 올림이 켜지지 않음");
                Check(b.Pressed, "터치로 누르는 동안 누름 상태");
                yield return Crop(b, "t1_touch_press");
                TouchAt(p, UnityEngine.InputSystem.TouchPhase.Ended); yield return Frames(3);
                w = 0;
                while (!d.EnemyTurn && w < 3f) { w += Time.unscaledDeltaTime; yield return null; }
                Check(d.EnemyTurn, "터치 → 적 차례로 넘어감");
                w = 0;
                while ((!d.WaitingInput || d.EnemyTurn) && !d.Over && w < 40f) { w += Time.unscaledDeltaTime; yield return null; }
                int turn2 = d.Battle.Snapshot.Turn;
                Check(turn2 == turn1 + 1 || d.Over, $"터치로 턴이 넘어감 {turn1} → {turn2}");
            }
            Debug.Log($"[EndTurnShot] 끝 — 실패 {fails}");
            yield return Real(0.3f);
            Application.Quit(fails == 0 ? 0 : 3);
        }
    }
}
