using System;
using System.Collections;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 소모량 고르기 — 카드의 「고른 만큼 소모」(spend pick)를 낼 때 몇 개를 쓸지 정한다(PlayOpts.Spend).
    // 왼쪽에 카드, 오른쪽에 후보 줄(1 · 절반 · 전부) — 줄마다 큰 숫자 · 이름 · 결과 미리보기. 단축키 1 · 2 · 3.
    // 취소(취소 단추 · Esc · 오른쪽 클릭 · 바깥 누르기)는 카드를 손으로 되돌린다 — picked(0).
    // 가벼움: 줄은 스프라이트 몇 장 + 글 셋, 매 프레임 하는 일은 입력 확인뿐(움직임 · 파티클 없음).
    public static class SpendWindow
    {
        public static Action<string> OnStage;     // 자동 데모 캡처
        const int O = 1400;

        /// <param name="demoPick">자동 데모 — 이 번째(0~) 후보를 잠시 뒤 고른다(-1 이면 사람이 고른다)</param>
        public static IEnumerator Run(Transform parent, CardInfo info, SpendPrompt sp, Action<int> picked, int demoPick = -1)
        {
            var root = Make.Node("SpendWindow", parent);
            Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(60, 30), O, new Color(0.01f, 0.015f, 0.04f, 0.93f));
            Tooltip.Suppress = true;
            Tooltip.I?.Hide();
            CardZoom.Hide();   // 손패 확대(O=1500)가 창 위에 남지 않게
            float k = Tone.K;
            int n = sp.Options.Count;
            // 폭 — 카드 + 줄. 좁은 화면(폰 가로)에서도 가장자리에 닿지 않게 줄 폭을 줄인다
            float s = Mathf.Min(1.45f, Tone.HalfH * 2 * 0.52f / CardView.H);
            float cardW = CardView.W * s, gap = 0.5f;
            float rowW = Mathf.Min(4.9f * k, Tone.HalfW * 2 - cardW - gap - 1.0f);
            float rowH = 1.0f * k, rowGap = 0.16f * k;
            float total = cardW + gap + rowW;
            float cardX = -total / 2 + cardW / 2, rowsX = total / 2 - rowW / 2;
            float colH = n * rowH + (n - 1) * rowGap;
            float cy = 0.05f;

            var cv = CardView.Create(root, info);
            cv.ShowDesc = true; cv.ShowPin = false;
            cv.TargetPos = new Vector3(cardX, cy, 0); cv.TargetScale = s; cv.Snap();
            cv.SetOrder(O + 10);

            float topY = cy + colH / 2 + rowH / 2 + 0.2f * k;
            Tone.Text("title", root, $"<color={Tone.GoldTag}>「{sp.Kw}」</color>을 몇 개 쓸까요?", new Vector3(rowsX, topY + 0.38f * k, 0), Tone.Xl * k, O + 20, Tone.Ink, TextAlignmentOptions.Center, false, rowW + 1f);
            string sub = $"가진 {sp.Have}개" + (string.IsNullOrEmpty(sp.PerUnit) ? "" : $" · {sp.PerUnit}");
            Tone.Text("sub", root, sub, new Vector3(rowsX, topY - 0.05f * k, 0), Tone.Md * k, O + 20, Tone.Sub, TextAlignmentOptions.Center, true, rowW + 1f);

            var btns = new Button[n + 1];
            for (int i = 0; i < n; i++)
            {
                var o = sp.Options[i];
                float y = cy + colH / 2 - rowH / 2 - i * (rowH + rowGap);
                var at = new Vector3(rowsX, y, 0);
                Make.Sliced("b" + i, root, Res.UI("cell_9s"), at, new Vector2(rowW, rowH), O + 20);
                var hl = Make.Sliced("bh" + i, root, Res.UI("cell_on_9s"), at, new Vector2(rowW, rowH), O + 21, new Color(1, 1, 1, 0));
                float left = rowsX - rowW / 2;
                // 큰 숫자 알약
                float pd = rowH * 0.74f;
                Make.Box("pl" + i, root, Res.UI("circle"), new Vector3(left + 0.12f * k + pd / 2, y, 0), new Vector2(pd, pd), O + 22, Tone.Gold);
                Tone.Text("n" + i, root, o.N.ToString(), new Vector3(left + 0.12f * k + pd / 2, y - 0.01f, 0), Tone.Xl * 1.15f * k, O + 24, Tone.Brown);
                // 이름 · 단축키
                float nx = left + 0.12f * k + pd + 0.22f * k;
                Tone.Text("nm" + i, root, o.Name, new Vector3(nx, y + 0.1f * k, 0), Tone.Lg * k, O + 23, Tone.Ink, TextAlignmentOptions.Left, true, 2f);
                if (!PointerInput.Touch)
                    Tone.Text("hk" + i, root, $"<color={Tone.DimTag}>키 {i + 1}</color>", new Vector3(nx, y - 0.2f * k, 0), Tone.Cap * k, O + 23, Tone.Dim, TextAlignmentOptions.Left, true, 2f);
                // 결과 미리보기 — 오른쪽 맞춤. 강화가 걸려 있으면 「피해 360% → 피해 540%」(뒤쪽 주황)
                if (!string.IsNullOrEmpty(o.Result))
                {
                    string res = o.Boosted != null
                        ? $"<color={Tone.SubTag}><size=80%>{o.Result}</size></color> <color={Tone.DimTag}>→</color> <color={Tone.EmpTag}>{o.Boosted}</color>"
                        : $"<color={Tone.SkyTag}>{o.Result}</color>";
                    var rt = Tone.Text("rs" + i, root, res, new Vector3(rowsX + rowW / 2 - 0.2f * k, y - 0.005f, 0), Tone.Lg * k, O + 23, Tone.Ink, TextAlignmentOptions.Right, true, rowW - (nx - left) - 0.5f);
                    rt.textWrappingMode = TextWrappingModes.NoWrap;
                }
                var b = Make.Node("btn" + i, root, at).gameObject.AddComponent<Button>();
                b.Size = new Vector2(rowW, rowH);
                b.Highlight = hl;
                btns[i] = b;
            }
            // 취소 단추 — 터치에도 있다
            float cw = 2.2f * k, ch = 0.62f * k;
            var cat = new Vector3(rowsX + rowW / 2 - cw / 2, cy - colH / 2 - rowH / 2 - 0.1f * k - ch / 2, 0);
            Make.Sliced("cancel", root, Res.UI("cell_9s"), cat, new Vector2(cw, ch), O + 20, new Color(1, 1, 1, 0.8f));
            var chl = Make.Sliced("cancelh", root, Res.UI("cell_on_9s"), cat, new Vector2(cw, ch), O + 21, new Color(1, 1, 1, 0));
            Tone.Text("cancelt", root, "취소", cat + new Vector3(0, -0.01f, 0), Tone.Md * k, O + 23, Tone.Sub);
            var cb = Make.Node("btncancel", root, cat).gameObject.AddComponent<Button>();
            cb.Size = new Vector2(cw, ch); cb.Highlight = chl;
            btns[n] = cb;

            OnStage?.Invoke("spend_window");
            int result = -1;
            for (int i = 0; i < n; i++) { int v = sp.Options[i].N; btns[i].OnClick = () => result = v; }
            cb.OnClick = () => result = 0;
            float t = 0;
            var keys = new[] { UnityEngine.InputSystem.Key.Digit1, UnityEngine.InputSystem.Key.Digit2, UnityEngine.InputSystem.Key.Digit3, UnityEngine.InputSystem.Key.Digit4 };
            while (result < 0)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                if (demoPick >= 0 && t > 0.9f) result = sp.Options[Mathf.Min(demoPick, n - 1)].N;
                for (int i = 0; i < n && i < keys.Length; i++) if (PointerInput.Key(keys[i])) result = sp.Options[i].N;
                if (t > 0.2f && (PointerInput.RightDown || PointerInput.Key(UnityEngine.InputSystem.Key.Escape))) result = 0;
                if (t > 0.2f && PointerInput.Tap)
                {
                    bool onBtn = false;
                    for (int i = 0; i <= n; i++) if (btns[i].Hit(PointerInput.Pos)) onBtn = true;
                    if (!onBtn) result = 0;
                }
            }
            Sfx.Play("ui_click", 0.5f);
            UnityEngine.Object.Destroy(root.gameObject);
            Tooltip.Suppress = false;
            picked(result);
        }
    }
}
