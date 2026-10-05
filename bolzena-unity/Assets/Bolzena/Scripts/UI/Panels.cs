using System;
using System.Collections.Generic;
using System.Text;
using Bolzena.Battle;
using Bolzena.View;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 창 — 뒤를 어둡게 하고 판(톤.md 의 큰 판: 남색 + 얇은 금 테)을 둔다. 바깥을 누르거나 오른쪽 클릭 · Esc 로 닫힌다. 한 번에 하나.
    // 크기는 열고 나서 내용에 맞춰 Fit 으로 정할 수 있다(정보 창 — 높이 자동).
    public class Modal : MonoBehaviour
    {
        public static Modal Open;
        public static Action<string> OnOpened;          // 자동 데모 캡처
        public Transform Content;
        public SpriteRenderer Panel, Dim;
        public Action OnClose;
        public bool PauseTime;
        Vector2 size;                                   // 월드
        float openedAt;
        public const int O = 850;

        public static Modal Create(Transform parent, string name, Vector2 size, Vector3 center, bool pause = false, float dim = 0.74f)
        {
            Open?.Close();
            var root = Make.Node("Modal_" + name, parent);
            var m = root.gameObject.AddComponent<Modal>();
            m.Dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(40, 12), O, new Color(0.01f, 0.015f, 0.04f, dim));
            m.Panel = Make.Sliced("panel", root, Res.UI("panel_9s"), center, size, O + 2);
            m.Content = Make.Node("content", root, center);
            m.size = size;
            m.openedAt = Time.unscaledTime;
            m.PauseTime = pause;
            if (pause) Clock.Paused = true;
            Open = m;
            Tooltip.Suppress = true;
            Sfx.Play("ui_click", 0.5f);
            OnOpened?.Invoke(name);
            return m;
        }

        /// <summary>판을 내용 크기(world)로 맞추고 자리를 옮긴다 — Content 는 판의 왼쪽 위에 둔다(k 배).</summary>
        public void Fit(Vector2 worldSize, Vector3 center, float k)
        {
            size = worldSize;
            Panel.size = worldSize;
            Panel.transform.localPosition = center;
            Content.localPosition = center + new Vector3(-worldSize.x / 2, worldSize.y / 2, 0);
            Content.localScale = new Vector3(k, k, 1);
        }

        public bool Inside(Vector2 p)
        {
            var c = (Vector2)Panel.transform.position;
            return Mathf.Abs(p.x - c.x) < size.x / 2 && Mathf.Abs(p.y - c.y) < size.y / 2;
        }

        public void Close()
        {
            if (Open == this) Open = null;
            if (PauseTime) Clock.Paused = false;
            Tooltip.Suppress = false;
            Tooltip.Ceiling = Tooltip.DefaultCeiling;
            OnClose?.Invoke();
            Destroy(gameObject);
        }

        void Update()
        {
            if (Time.unscaledTime - openedAt < 0.15f) return;
            if (PointerInput.RightDown || PointerInput.Key(UnityEngine.InputSystem.Key.Escape)) { Close(); return; }
            if (PointerInput.Tap && !Inside(PointerInput.Pos)) Close();
        }

        // 창 안 글 상자(왼쪽 위 기준)
        public TextMeshPro Text(string text, Vector2 at, float size, float width, Color? color = null)
        {
            var t = Make.Text("t", Content, text, new Vector3(at.x, at.y, 0), size, O + 5, color ?? Tone.Ink, TextAlignmentOptions.TopLeft, width);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.pivot = new Vector2(0, 1);
            t.rectTransform.sizeDelta = new Vector2(width, 20);
            t.lineSpacing = -4;
            return t;
        }
    }

    // 누르는 사각형 하나 — Size · Offset 은 이 물체의 자리 단위(부모 배율 · 폰 1.25배를 따라간다).
    // 창이 열려 있으면 창 안의 단추만 듣는다. Highlight 를 주면 올렸을 때 켠다(톤.md — 올림은 금 테).
    public class Button : MonoBehaviour
    {
        public Vector2 Size = Vector2.one;
        public Vector2 Offset;
        public Action OnClick;
        public SpriteRenderer Highlight;
        public bool Hot { get; private set; }

        public bool Hit(Vector2 p)
        {
            var ls = transform.lossyScale;
            var c = (Vector2)transform.position + Offset * new Vector2(ls.x, ls.y);
            return Mathf.Abs(p.x - c.x) < Size.x * Mathf.Abs(ls.x) / 2 && Mathf.Abs(p.y - c.y) < Size.y * Mathf.Abs(ls.y) / 2;
        }

        void Update()
        {
            bool blocked = Modal.Open != null && !transform.IsChildOf(Modal.Open.transform);
            var p = PointerInput.Pos;
            Hot = !blocked && !PointerInput.Touch && Hit(p);
            if (Highlight != null)
            {
                var c = Highlight.color;
                c.a = Mathf.MoveTowards(c.a, Hot ? 0.9f : 0f, Time.unscaledDeltaTime * 8f);
                Highlight.color = c;
            }
            if (blocked || !PointerInput.Tap) return;
            if (Hit(p)) { Sfx.Play("ui_click", 0.5f); OnClick?.Invoke(); }
        }
    }

    // 일시정지 · 설정 창(정보 창은 InfoPanel.cs)
    public static partial class InfoPanel
    {
        // 일시정지 · 설정 — 계속하기(금 알약) · 배속(남색 알약) · 화면 묶음(판 화면과 같은 DisplayOptions — 창 모드 · 해상도 · 프레임 · 수직동기).
        // 창 모드 · 해상도는 바로 시험 적용(Try)하고 「10초 안에 유지」를 묻는다 — 답이 없거나 창을 닫으면 앞 화면으로(Revert).
        public static Modal Pause(Transform parent, Action onSpeed)
        {
            var m = Modal.Create(parent, "pause", new Vector2(7.6f, 7f), new Vector3(0, 0, 0), true);
            var ui = m.gameObject.AddComponent<PauseUi>();
            ui.Init(m, onSpeed);
            return m;
        }
    }

    // 일시정지 창 속 — 화면이 바뀌면(크기 · 폰/PC) 다시 그린다
    public class PauseUi : MonoBehaviour
    {
        Modal m;
        Action onSpeed;
        float trialUntil = -1;
        TextMeshPro confirmText;
        const float Wd = 7.4f, Pad = 0.34f;
        const int OC = Modal.O + 5;

        public void Init(Modal modal, Action speed)
        {
            m = modal;
            onSpeed = speed;
            Bolzena.RunUI.DisplayOptions.Changed += OnChanged;
            m.OnClose += () =>
            {
                Bolzena.RunUI.DisplayOptions.Changed -= OnChanged;
                if (Bolzena.RunUI.DisplayOptions.Pending) Bolzena.RunUI.DisplayOptions.Revert();   // 확인 없이 닫음 — 앞 화면으로
            };
            Draw();
        }

        bool dirty;
        void OnChanged() => dirty = true;

        void Update()
        {
            // 창 크기는 바뀐 다음 프레임에 잡힌다 — 그때 다시 그린다
            if (dirty && Time.frameCount % 2 == 0) { dirty = false; Draw(); }
            if (trialUntil > 0)
            {
                float left = trialUntil - Time.unscaledTime;
                if (confirmText != null) confirmText.text = $"이 화면을 유지할까요?  <color={Tone.GoldTag}>{Mathf.CeilToInt(Mathf.Max(0, left))}</color>";
                if (left <= 0) { trialUntil = -1; Bolzena.RunUI.DisplayOptions.Revert(); }
            }
        }

        void Draw()
        {
            for (int i = m.Content.childCount - 1; i >= 0; i--) Destroy(m.Content.GetChild(i).gameObject);
            m.Content.localScale = Vector3.one;
            var T = m.Content;
            float y = -Pad;
            // 제목 · 주 동작
            Make.Text("title", T, "일시정지", new Vector3(Wd / 2, y - 0.24f, 0), Tone.Xl, OC, Tone.Gold);
            y -= 0.66f;
            Pill(T, "resume", new Vector3(Wd / 2 - 1.42f, y - 0.33f, 0), 2.6f, "계속하기", true, m.Close);
            TextMeshPro st = null;
            st = Pill(T, "speed", new Vector3(Wd / 2 + 1.5f, y - 0.33f, 0), 2.3f, "배속 " + Clock.Speed + "×", false, () => { onSpeed?.Invoke(); st.text = "배속 " + Clock.Speed + "×"; });
            y -= 0.66f + 0.2f;
            Make.Box("rule", T, Res.UI("white"), new Vector3(Wd / 2, y, 0), new Vector2(Wd - Pad * 2, 0.012f), OC, Tone.Line);
            y -= 0.18f;

            // 화면
            Tone.Text("dh", T, $"화면  <size=70%><color={Tone.SubTag}>{Bolzena.RunUI.DisplayOptions.Describe()}</color></size>", new Vector3(Pad, y, 0), Tone.Lg, OC, Tone.Ink, TextAlignmentOptions.TopLeft);
            y -= 0.42f;
            int curMode = Bolzena.RunUI.DisplayOptions.ModeIndex, curPreset = Bolzena.RunUI.DisplayOptions.PresetIndex;

            // 판 화면 설정 창과 같은 모양 — 칸은 줄 폭을 똑같이 나눈다, 화면 모드 밑에 그 모드 풀이 한 줄, 수직동기는 칸 줄 + 스위치
            float rowW = Wd - Pad * 2, gap = 0.1f;
            if (Bolzena.RunUI.DisplayOptions.Web)
            {
                // 웹 — 창 모드 · 해상도는 브라우저가 정한다. 브라우저 창 · 전체 화면 두 칸만
                y = Label(T, y, "화면 모드", "Esc 로 전체 화면에서 나옵니다");
                bool full = Screen.fullScreen;
                float cw = (rowW - gap) / 2;
                Chip(T, Pad, y, "브라우저 창", null, !full, true, null, () => Bolzena.RunUI.DisplayOptions.SetWebFullScreen(false), cw);
                Chip(T, Pad + cw + gap, y, "전체 화면", null, full, true, null, () => Bolzena.RunUI.DisplayOptions.SetWebFullScreen(true), cw);
                y -= 0.52f + 0.12f;
            }
            else
            {
            y = Label(T, y, "화면 모드", null);
            int nm = Bolzena.RunUI.DisplayOptions.Modes.Count;
            for (int i = 0; i < nm; i++)
            {
                int k = i;
                bool ok = Bolzena.RunUI.DisplayOptions.ModeAvailable(i);
                float cw = (rowW - gap * (nm - 1)) / nm;
                Chip(T, Pad + i * (cw + gap), y, Bolzena.RunUI.DisplayOptions.Modes[i].Name, null, i == curMode, ok, ok ? null : "이 기기에서는 쓸 수 없습니다", () => Pick(k, -1, curMode, curPreset), cw);
            }
            y -= 0.52f + 0.06f;
            Tone.Text("mh", T, "· " + Bolzena.RunUI.DisplayOptions.ModeHint(curMode), new Vector3(Pad, y - 0.12f, 0), Tone.Cap, OC, Tone.Sub, TextAlignmentOptions.Left, true);
            y -= 0.3f;

            var mon = Bolzena.RunUI.DisplayOptions.Monitor;
            y = Label(T, y, "해상도", $"이 모니터 {mon.x}×{mon.y} · 더 큰 것은 잠깁니다");
            int np = Bolzena.RunUI.DisplayOptions.Presets.Count;
            for (int i = 0; i < np; i++)
            {
                int k = i;
                var sz = Bolzena.RunUI.DisplayOptions.SizeOf(i);
                bool ok = Bolzena.RunUI.DisplayOptions.Fits(i);
                float cw = (rowW - gap * (np - 1)) / np;
                Chip(T, Pad + i * (cw + gap), y, Bolzena.RunUI.DisplayOptions.Presets[i].Name, $"{sz.x}×{sz.y}", i == curPreset, ok, ok ? null : $"이 모니터({mon.x}×{mon.y})보다 큽니다", () => Pick(-1, k, curMode, curPreset), cw);
            }
            y -= 0.66f + 0.12f;
            }

            bool vs = Bolzena.RunUI.DisplayOptions.VSync;
            y = Label(T, y, "프레임 제한", "초당 그리는 횟수 — 낮추면 전기 · 열이 줄어듭니다");
            int nf = Bolzena.RunUI.DisplayOptions.FrameCaps.Count;
            for (int i = 0; i < nf; i++)
            {
                int k = i;
                float cw = (rowW - gap * (nf - 1)) / nf;
                Chip(T, Pad + i * (cw + gap), y, Bolzena.RunUI.DisplayOptions.FrameCapName(i), null, !vs && i == Bolzena.RunUI.DisplayOptions.FpsIndex, !vs, vs ? "수직동기를 끄면 고를 수 있습니다" : null,
                    () => Bolzena.RunUI.DisplayOptions.SetFrameCap(k), cw);
            }
            y -= 0.52f + 0.12f;
            // 수직동기 — 칸 줄 하나에 이름 · 풀이 · 스위치(누르면 켬/끔)
            {
                float rh = 0.54f;
                var rc = new Vector3(Wd / 2, y - rh / 2, 0);
                var row = Make.Sliced("vrow", T, Res.UI("cell_9s"), rc, new Vector2(rowW, rh), OC);
                var hl = Make.Sliced("vrowhl", T, Res.UI("cell_on_9s"), rc, new Vector2(rowW, rh), OC, new Color(1, 1, 1, 0));
                var lt = Tone.Text("vl", T, "수직동기", new Vector3(Pad + 0.2f, rc.y, 0), Tone.Body, OC + 1, Tone.Ink, TextAlignmentOptions.Left, false);
                lt.ForceMeshUpdate();
                Tone.Text("vs", T, "주사율에 맞춰 찢김 없이 · 켜면 프레임 제한은 쉰다", new Vector3(Pad + 0.32f + lt.preferredWidth, rc.y - 0.01f, 0), Tone.Cap, OC + 1, Tone.Sub, TextAlignmentOptions.Left, true);
                var track = new Vector3(Wd - Pad - 0.5f, rc.y, 0);
                Make.Sliced("vtrack", T, Res.UI("bar_fill_9s"), track, new Vector2(0.64f, 0.3f), OC + 1, vs ? Tone.Gold : new Color(0.1f, 0.13f, 0.24f));
                Make.Box("vknob", T, Res.UI("circle"), track + new Vector3(vs ? 0.17f : -0.17f, 0, 0), new Vector2(0.26f, 0.26f), OC + 2, Color.white);
                var b = row.gameObject.AddComponent<Button>();
                b.Size = new Vector2(rowW, rh);
                b.Highlight = hl;
                b.OnClick = () => Bolzena.RunUI.DisplayOptions.SetVSync(!vs);
                y -= rh;
            }

            // 시험 적용 확인
            if (Bolzena.RunUI.DisplayOptions.Pending)
            {
                y -= 0.14f;
                Make.Sliced("cbar", T, Res.UI("cell_on_9s"), new Vector3(Wd / 2, y - 0.32f, 0), new Vector2(Wd - Pad * 2, 0.64f), OC - 1);
                confirmText = Tone.Text("ct", T, "", new Vector3(Pad + 0.2f, y - 0.32f, 0), Tone.Body, OC, Tone.Ink, TextAlignmentOptions.Left, true);
                Pill(T, "keep", new Vector3(Wd - Pad - 2.3f, y - 0.32f, 0), 1.3f, "유지", true, () => { trialUntil = -1; Bolzena.RunUI.DisplayOptions.Keep(); }, 0.46f);
                Pill(T, "revert", new Vector3(Wd - Pad - 0.75f, y - 0.32f, 0), 1.4f, "되돌리기", false, () => { trialUntil = -1; Bolzena.RunUI.DisplayOptions.Revert(); }, 0.46f);
                if (trialUntil < 0) trialUntil = Time.unscaledTime + 10f;
                y -= 0.64f;
            }
            else { confirmText = null; trialUntil = -1; }

            y -= 0.16f;
            Tone.Text("hint", T, "Esc · 바깥을 누르면 계속합니다", new Vector3(Wd / 2, y - 0.12f, 0), Tone.Cap, OC, Tone.Dim, TextAlignmentOptions.Center, true, Wd);
            y -= 0.24f + Pad;

            // 크기 — 폰은 1.25 배, 다만 화면 높이를 넘지 않게
            float h = -y;
            float k2 = Mathf.Min(Tone.K, (Tone.HalfH * 2 - 0.3f) / h, (Tone.HalfW * 2 - 0.3f) / Wd);
            m.Fit(new Vector2(Wd, h) * k2, Vector3.zero, k2);
        }

        void Pick(int mode, int preset, int curMode, int curPreset)
        {
            int mm = mode >= 0 ? mode : curMode;
            int p = preset >= 0 ? preset : curPreset >= 0 ? curPreset : (mm == 0 ? (Bolzena.RunUI.DisplayOptions.Fits(2) ? 2 : 1) : 0);
            if (mm == curMode && p == curPreset) return;
            if (!Bolzena.RunUI.DisplayOptions.Try(mm, p)) return;
            trialUntil = Time.unscaledTime + 10f;
        }

        float Label(Transform T, float y, string name, string sub)
        {
            var l = Tone.Text("l", T, name, new Vector3(Pad, y - 0.12f, 0), Tone.Body, OC, Tone.Ink, TextAlignmentOptions.Left, false);
            if (sub != null) { l.ForceMeshUpdate(); Tone.Text("ls", T, sub, new Vector3(Pad + l.preferredWidth + 0.12f, y - 0.13f, 0), Tone.Cap, OC, Tone.Sub, TextAlignmentOptions.Left, true); }
            return y - 0.3f;
        }

        // 고르는 칸 — 고르면 금 테(cell_on), 잠기면 흐리게 + 누르면 까닭
        float Chip(Transform T, float x, float y, string label, string sub, bool on, bool ok, string why, Action click, float minW = 0)
        {
            float h = sub != null ? 0.66f : 0.52f;
            var t = Tone.Text("c", T, label, Vector3.zero, Tone.Body, OC + 1, on ? Tone.Gold : ok ? Tone.Ink : Tone.Dim, TextAlignmentOptions.Center, true);
            t.ForceMeshUpdate();
            float w = minW > 0 ? minW : Mathf.Max(t.preferredWidth + 0.4f, sub != null ? 1.18f : 0.8f);
            var c = new Vector3(x + w / 2, y - h / 2, 0);
            var bg = Make.Sliced("cb", T, Res.UI(on ? "cell_on_9s" : "cell_9s"), c, new Vector2(w, h), OC, new Color(1, 1, 1, ok ? 1f : 0.45f));
            t.transform.localPosition = c + new Vector3(0, sub != null ? 0.1f : -0.01f, 0);
            if (sub != null) Tone.Text("cs", T, sub, c + new Vector3(0, -0.15f, 0), 0.12f, OC + 1, ok ? Tone.Sub : Tone.Dim, TextAlignmentOptions.Center, true);
            var hl = on || !ok ? null : Make.Sliced("chl", T, Res.UI("cell_on_9s"), c, new Vector2(w, h), OC, new Color(1, 1, 1, 0));
            var b = bg.gameObject.AddComponent<Button>();
            b.Size = new Vector2(w, h);
            b.Highlight = hl;
            b.OnClick = () =>
            {
                if (!ok) { if (why != null) Vfx.Word(bg.transform.position + new Vector3(0, 0.5f, 0), why, 0.2f, new Color(1f, 0.8f, 0.7f), new Color(0.2f, 0, 0), 1f, 1f, m.transform, Modal.O + 30, 0.3f); return; }
                click?.Invoke();
            };
            if (why != null) TipZone.Add(bg, new Vector2(w, h), () => why, 120);
            return w;
        }

        static TextMeshPro Pill(Transform T, string name, Vector3 at, float w, string label, bool gold, Action click, float h = 0.62f)
        {
            var size = new Vector2(w, h);
            var bg = Make.Sliced(name, T, Res.UI(gold ? "pill_gold_9s" : "pill_dark_9s"), at, size, OC);
            var t = Make.Text(name + "t", bg.transform, label, new Vector3(0, -0.01f, 0), h < 0.55f ? Tone.Md : Tone.Lg, OC + 2, gold ? Tone.Brown : Tone.Ink);
            var b = bg.gameObject.AddComponent<Button>();
            b.Size = size;
            b.OnClick = click;
            return t;
        }
    }
}
