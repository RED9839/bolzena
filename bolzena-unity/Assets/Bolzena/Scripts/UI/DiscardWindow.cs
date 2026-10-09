using System;
using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 손패 버리기 / 소멸 고르기 — 고르는 버리기 · 소멸 카드를 낼 때 손패(낼 카드 제외)에서 n장을 눌러 고르고 확인(2026-10-09).
    // 소모량 창(SpendWindow)과 같은 결: 어두운 막 · 위 제목 · 취소(단추 · Esc · 오른쪽 클릭) = 카드가 손으로 돌아간다 → picked(null).
    public static class DiscardWindow
    {
        public static Action<string> OnStage;
        const int O = 1400;

        /// <param name="shown">낼 카드를 뺀 손패(화면 순서)</param>
        /// <param name="demoPick">자동 데모 — 앞에서 n장을 고른다</param>
        public static IEnumerator Run(Transform parent, CardInfo played, IList<CardInfo> shown, int need, string kind, Action<List<int>> picked, bool demoPick = false)
        {
            var root = Make.Node("DiscardWindow", parent);
            Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(60, 30), O, new Color(0.01f, 0.015f, 0.04f, 0.93f));
            Tooltip.Suppress = true; Tooltip.I?.Hide(); CardZoom.Hide();
            float k = Tone.K;
            int n = shown.Count;
            float availW = Tone.HalfW * 2 - 1.0f;
            float cw = Mathf.Min(CardView.W * 1.15f, (availW - 0.2f * (n - 1)) / Mathf.Max(1, n));
            float s = cw / CardView.W;
            float gap = n > 1 ? Mathf.Min(0.25f, (availW - cw * n) / (n - 1)) : 0;
            float total = n * cw + (n - 1) * gap;
            var views = new List<CardView>(); var marks = new List<SpriteRenderer>();
            var chosen = new List<int>();
            for (int i = 0; i < n; i++)
            {
                var cv = CardView.Create(root, shown[i]);
                cv.ShowDesc = true; cv.ShowPin = false;
                var at = new Vector3(-total / 2 + cw / 2 + i * (cw + gap), 0.1f, 0);
                cv.TargetPos = at; cv.TargetScale = s; cv.Snap(); cv.SetOrder(O + 10 + i * 10);
                views.Add(cv);
                var m = Make.Box("sel" + i, root, Res.UI("card_glow"), at, new Vector2(CardView.W * s * 1.25f, CardView.H * s * 1.2f), O + 9 + i * 10, new Color(1f, 0.4f, 0.35f, 0), Res.SpriteMat(true, 2f));
                marks.Add(m);
            }
            float tk = Mathf.Min(1f, Tone.HalfH / 4.5f);
            var title = Tone.Text("title", root, "", new Vector3(0, 3.35f * tk, 0), Tone.Xl * k, O + 30, Tone.Ink);
            Tone.Text("sub", root, $"{played.Name} — 손패에서 {need}장을 눌러 고르세요", new Vector3(0, 2.8f * tk, 0), Tone.Md * k, O + 30, Tone.Sub, TextAlignmentOptions.Center, true, 12f);
            float by = -Tone.HalfH + 0.85f;
            var okBg = Make.Sliced("ok", root, Res.UI("pill_gold_9s"), new Vector3(1.6f, by, 0), new Vector2(2.6f, 0.78f), O + 30);
            var okT = Make.Text("okt", root, "확인", new Vector3(1.6f, by, 0), Tone.Lg * k, O + 32, Tone.Brown);
            var cnBg = Make.Sliced("cn", root, Res.UI("pill_dark_9s"), new Vector3(-1.6f, by, 0), new Vector2(2.6f, 0.78f), O + 30);
            Make.Text("cnt", root, "취소", new Vector3(-1.6f, by, 0), Tone.Lg * k, O + 32, Tone.Ink);
            void Refresh()
            {
                title.text = $"{kind} 카드를 고르세요 <color={Tone.GoldTag}>({chosen.Count}/{need})</color>";
                bool ok = chosen.Count == need;
                okBg.color = new Color(1, 1, 1, ok ? 1f : 0.4f); okT.alpha = ok ? 1f : 0.5f;
                for (int i = 0; i < n; i++)
                {
                    bool on = chosen.Contains(i);
                    Make.Alpha(marks[i], on ? 0.85f : 0);
                    views[i].TargetPos = new Vector3(views[i].TargetPos.x, on ? 0.35f : 0.1f, 0);
                }
            }
            Refresh();
            OnStage?.Invoke("discard_window");
            List<int> result = null; bool done = false; float t0 = Time.unscaledTime;
            while (!done)
            {
                var p = PointerInput.Pos;
                if (Time.unscaledTime - t0 > 0.25f)
                {
                    if (PointerInput.Down)
                    {
                        var bo = okBg.bounds; var bc = cnBg.bounds;
                        if (bo.Contains(new Vector3(p.x, p.y, bo.center.z))) { if (chosen.Count == need) { result = new List<int>(chosen); done = true; } }
                        else if (bc.Contains(new Vector3(p.x, p.y, bc.center.z))) done = true;
                        else
                            for (int i = 0; i < n; i++)
                            {
                                var l = views[i].transform.InverseTransformPoint(p);
                                if (Mathf.Abs(l.x) < CardView.W / 2 && Mathf.Abs(l.y) < CardView.H / 2)
                                {
                                    if (chosen.Contains(i)) chosen.Remove(i); else if (chosen.Count < need) chosen.Add(i);
                                    Sfx.Play("card_hover", 0.4f); Refresh(); break;
                                }
                            }
                    }
                    if (PointerInput.RightDown || PointerInput.Key(UnityEngine.InputSystem.Key.Escape)) done = true;
                    if (PointerInput.Key(UnityEngine.InputSystem.Key.Enter) && chosen.Count == need) { result = new List<int>(chosen); done = true; }
                }
                if (demoPick && Time.unscaledTime - t0 > 1.0f)
                {
                    if (chosen.Count < need) { chosen.Add(chosen.Count); Refresh(); t0 = Time.unscaledTime - 0.6f; }
                    else { result = new List<int>(chosen); done = true; }
                }
                yield return null;
            }
            Tooltip.Suppress = false;
            UnityEngine.Object.Destroy(root.gameObject);
            picked?.Invoke(result);
        }
    }
}
