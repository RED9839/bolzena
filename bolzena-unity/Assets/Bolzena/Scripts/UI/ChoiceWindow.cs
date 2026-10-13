using System;
using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 두 갈래 카드 — 낼 때 갈래를 고른다(PlayOpts.Choice 1 · 2). 카드를 가운데 크게, 그 아래 갈래 칸 둘(이름 · 단축키 1 · 2).
    // 단계 고르기(stack pick — 요미 「달님께 비는 기도」 · 고학년, 2026-10-13)도 같은 창 — 단계 이름 칸 N개(PlayOpts.Stage 1~N). 카드가 없으면(고학년) 칸만.
    // 바깥 · 오른쪽 클릭 · Esc 는 무르기(카드는 손으로 돌아간다 — picked(0)).
    public static class ChoiceWindow
    {
        public static Action<string> OnStage;     // 자동 데모 캡처
        const int O = 1400;

        public static IEnumerator Run(Transform parent, CardInfo info, Action<int> picked, int demoPick = -1) =>
            Run(parent, info, info.Choices, "갈래를 고르세요", picked, demoPick);

        /// <summary>labels 의 칸 가운데 하나를 고른다 — picked(1 ~ labels.Count), 무르면 0. info 가 null 이면 카드 없이 칸만.</summary>
        public static IEnumerator Run(Transform parent, CardInfo info, IReadOnlyList<string> labels, string title, Action<int> picked, int demoPick = -1)
        {
            var root = Make.Node("ChoiceWindow", parent);
            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(60, 30), O, new Color(0.01f, 0.015f, 0.04f, 0.95f));
            Tooltip.Suppress = true;
            Tooltip.I?.Hide();
            float k = Tone.K;
            float s = Mathf.Min(1.7f, Tone.HalfH * 2 * 0.5f / CardView.H);
            float top, by;
            if (info != null)
            {
                var cv = CardView.Create(root, info);
                cv.ShowDesc = true; cv.ShowPin = true;
                cv.TargetPos = new Vector3(0, 1.0f, 0); cv.TargetScale = s; cv.Snap();
                cv.SetOrder(O + 10);
                top = 1.0f + CardView.H * s / 2 + 0.3f * k;
                by = 1.0f - CardView.H * s / 2 - 0.65f * k;
            }
            else { top = 0.9f * k; by = -0.3f * k; }
            Tone.Text("title", root, title, new Vector3(0, top, 0), Tone.Xl * k, O + 20, Tone.Gold);
            int n = labels.Count;
            var btns = new Button[n];
            float gap = 0.3f;
            float bw = n <= 2 ? 3.2f * k : Mathf.Min(3.2f * k, (Tone.HalfW * 2 * 0.92f - gap * (n - 1)) / n), bh = 0.8f * k;   // 칸이 많으면 화면 폭에 맞춰 좁힌다
            for (int i = 0; i < n; i++)
            {
                var at = new Vector3((i - (n - 1) / 2f) * (bw + gap), by, 0);
                var bg = Make.Sliced("b" + i, root, Res.UI("cell_9s"), at, new Vector2(bw, bh), O + 20);
                var hl = Make.Sliced("bh" + i, root, Res.UI("cell_on_9s"), at, new Vector2(bw, bh), O + 20, new Color(1, 1, 1, 0));
                Tone.Text("n", root, $"<color={Tone.SubTag}><size=70%>{i + 1}</size></color>  {labels[i]}", at + new Vector3(0, -0.01f, 0), Tone.Lg * k, O + 22, Tone.Ink);
                var b = bg.gameObject.AddComponent<Button>();
                b.Size = new Vector2(bw, bh);
                b.Highlight = hl;
                btns[i] = b;
            }
            OnStage?.Invoke("choice_window");
            int result = -1;
            for (int i = 0; i < n; i++) { int v = i + 1; btns[i].OnClick = () => result = v; }
            var keys = new[] { UnityEngine.InputSystem.Key.Digit1, UnityEngine.InputSystem.Key.Digit2, UnityEngine.InputSystem.Key.Digit3, UnityEngine.InputSystem.Key.Digit4,
                               UnityEngine.InputSystem.Key.Digit5, UnityEngine.InputSystem.Key.Digit6, UnityEngine.InputSystem.Key.Digit7, UnityEngine.InputSystem.Key.Digit8, UnityEngine.InputSystem.Key.Digit9 };
            float t = 0;
            while (result < 0)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                if (demoPick > 0 && t > 0.9f) result = Mathf.Min(demoPick, n);
                for (int i = 0; i < n && i < keys.Length; i++) if (PointerInput.Key(keys[i])) { result = i + 1; break; }
                if (result >= 0) break;
                if (t > 0.2f && (PointerInput.RightDown || PointerInput.Key(UnityEngine.InputSystem.Key.Escape))) result = 0;
                if (t > 0.2f && PointerInput.Tap && !Array.Exists(btns, x => x.Hit(PointerInput.Pos))) result = 0;
            }
            Sfx.Play("ui_click", 0.5f);
            UnityEngine.Object.Destroy(root.gameObject);
            Tooltip.Suppress = false;
            picked(result);
        }
    }
}
