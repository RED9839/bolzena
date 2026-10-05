using System;
using System.Collections;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 두 갈래 카드 — 낼 때 갈래를 고른다(PlayOpts.Choice 1 · 2). 카드를 가운데 크게, 그 아래 갈래 칸 둘(이름 · 단축키 1 · 2).
    // 바깥 · 오른쪽 클릭 · Esc 는 무르기(카드는 손으로 돌아간다 — picked(0)).
    public static class ChoiceWindow
    {
        public static Action<string> OnStage;     // 자동 데모 캡처
        const int O = 1400;

        public static IEnumerator Run(Transform parent, CardInfo info, Action<int> picked, int demoPick = -1)
        {
            var root = Make.Node("ChoiceWindow", parent);
            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(60, 30), O, new Color(0.01f, 0.015f, 0.04f, 0.95f));
            Tooltip.Suppress = true;
            Tooltip.I?.Hide();
            float k = Tone.K;
            float s = Mathf.Min(1.7f, Tone.HalfH * 2 * 0.5f / CardView.H);
            var cv = CardView.Create(root, info);
            cv.ShowDesc = true; cv.ShowPin = true;
            cv.TargetPos = new Vector3(0, 1.0f, 0); cv.TargetScale = s; cv.Snap();
            cv.SetOrder(O + 10);
            Tone.Text("title", root, "갈래를 고르세요", new Vector3(0, 1.0f + CardView.H * s / 2 + 0.3f * k, 0), Tone.Xl * k, O + 20, Tone.Gold);
            var btns = new Button[2];
            float bw = 3.2f * k, bh = 0.8f * k, by = 1.0f - CardView.H * s / 2 - 0.65f * k;
            for (int i = 0; i < 2; i++)
            {
                var at = new Vector3((i == 0 ? -1 : 1) * (bw / 2 + 0.15f), by, 0);
                var bg = Make.Sliced("b" + i, root, Res.UI("cell_9s"), at, new Vector2(bw, bh), O + 20);
                var hl = Make.Sliced("bh" + i, root, Res.UI("cell_on_9s"), at, new Vector2(bw, bh), O + 20, new Color(1, 1, 1, 0));
                Tone.Text("n", root, $"<color={Tone.SubTag}><size=70%>{i + 1}</size></color>  {info.Choices[i]}", at + new Vector3(0, -0.01f, 0), Tone.Lg * k, O + 22, Tone.Ink);
                var b = bg.gameObject.AddComponent<Button>();
                b.Size = new Vector2(bw, bh);
                b.Highlight = hl;
                btns[i] = b;
            }
            OnStage?.Invoke("choice_window");
            int result = -1;
            btns[0].OnClick = () => result = 1;
            btns[1].OnClick = () => result = 2;
            float t = 0;
            while (result < 0)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                if (demoPick > 0 && t > 0.9f) result = demoPick;
                if (PointerInput.Key(UnityEngine.InputSystem.Key.Digit1)) result = 1;
                if (PointerInput.Key(UnityEngine.InputSystem.Key.Digit2)) result = 2;
                if (t > 0.2f && (PointerInput.RightDown || PointerInput.Key(UnityEngine.InputSystem.Key.Escape))) result = 0;
                if (t > 0.2f && PointerInput.Tap && !btns[0].Hit(PointerInput.Pos) && !btns[1].Hit(PointerInput.Pos)) result = 0;
            }
            Sfx.Play("ui_click", 0.5f);
            UnityEngine.Object.Destroy(root.gameObject);
            Tooltip.Suppress = false;
            picked(result);
        }
    }
}
