using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 신탁 — 빛나는 카드를 내면: 화면이 어두워지며 「신탁!」, 카드가 가운데로 와서 세 갈래로 갈라져 펼쳐진다(내면 고르기).
    // 하나를 고르면 나머지는 흩어지고, 고른 카드가 원래 카드와 겹치며 하얗게 타올라 바뀐 모습으로 다시 태어난다.
    public static class EpiphanyWindow
    {
        const int O = 800;
        public static System.Action<string> OnStage;
        /// <summary>지금 열린 창의 고를 카드들(닫혀 있으면 null) — 입력 재현(InputProbe)이 짚는다.</summary>
        public static List<CardView> Options { get; private set; }

        // card — 손에서 뽑아 낸 그 카드(이 창이 끝나면 바뀐 모습으로 돌려준다). demoPick 이 0 이상이면 그것을 고른다(자동 데모)
        public static IEnumerator Run(Transform parent, CardView card, IReadOnlyList<CardInfo> options, System.Action<int> picked, int demoPick = -1)
        {
            var root = Make.Node("Epiphany", parent);
            // 창이 열린 동안은 시간이 흐르게 — 일시정지 · 맞는 순간 멈칫이 남아 있으면 창 안의 연출(화면 시계)이 멈춘다
            Clock.Paused = false;
            Clock.ClearStops();
            Sfx.Play("epiphany_open", 0.6f);
            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(44, 16), O, new Color(0.03f, 0.02f, 0.06f, 0));
            var raysMat = Res.NewMat("Bolzena/Rays");
            raysMat.SetColor("_Color", new Color(1f, 0.82f, 0.4f, 1f));
            raysMat.SetFloat("_Count", 22);
            raysMat.SetFloat("_Spin", 0.05f);
            raysMat.SetFloat("_Boost", 1.6f);
            raysMat.SetFloat("_Inner", 0.08f);
            raysMat.SetFloat("_Outer", 0.55f);
            var rays = Make.Quad("rays", root, new Vector3(0, 0.2f, 0), new Vector2(18, 18), raysMat, O + 1);
            var title = Make.Text("title", root, "신탁!", new Vector3(0, 3.55f, 0), 0.95f, O + 40, new Color(1f, 0.95f, 0.75f));
            title.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.78f, 0.3f), new Color(1f, 0.78f, 0.3f));
            Make.Outline(title, 0.2f, new Color(0.35f, 0.15f, 0));
            Make.Glow(title, Color.white, 1.5f);
            var sub = Make.Text("sub", root, "<color=#ffd76a>" + card.Info.Name + "</color>에 신탁 — 카드를 눌러 하나를 고르세요(1~" + options.Count + " · ←→ 엔터). 이번에는 코스트 0",
                new Vector3(0, 2.85f, 0), 0.24f, O + 40, new Color(1f, 0.92f, 0.8f));
            Make.Outline(sub, 0.25f, Color.black);
            var subBg = Make.Box("subbg", root, Res.UI("band"), new Vector3(0, 2.85f, 0), new Vector2(13f, 0.56f), O + 39, new Color(1, 1, 1, 0));
            title.alpha = 0; sub.alpha = 0;

            // 카드를 가운데로
            card.Follow = 10f;
            card.SetOrder(O + 20);
            card.TargetPos = new Vector3(0, 0.1f, 0);
            card.TargetRot = 0;
            card.TargetScale = 1.25f;
            yield return Clock.Tween(0.35f, t =>
            {
                Make.Alpha(dim, 0.78f * t);
                raysMat.SetFloat("_Alpha", 0.55f * t);
                title.alpha = t; sub.alpha = t; Make.Alpha(subBg, t);
                title.transform.localScale = Vector3.one * Mathf.LerpUnclamped(2.2f, 1f, Ease.OutBack(t, 2f));
            }, true);
            Vfx.Glow(new Vector3(0, 0.1f, 0), 4.5f, new Color(1f, 0.85f, 0.45f, 0.9f), 0.5f, 3f, null, O + 30, root);
            Sfx.Play("epiphany", 0.5f, 1.2f);

            // 세 갈래로 — 가운데 카드에서 겹쳐 나와 펼쳐진다
            var opts = new List<CardView>();
            var labels = new List<TextMeshPro>();
            for (int i = 0; i < options.Count; i++)
            {
                var c = CardView.Create(root, options[i]);
                c.transform.localPosition = new Vector3(0, 0.1f, 0);
                c.transform.localScale = Vector3.one * 1.2f;
                c.Follow = 9f;
                c.TargetPos = new Vector3((i - (options.Count - 1) / 2f) * 3.5f, 0.05f, 0);
                c.TargetRot = 0;
                c.TargetScale = 1.12f;
                c.SetOrder(O + 10 + i * 10);
                opts.Add(c);
                var lbg = Make.Sliced("labelbg" + i, c.transform, Res.UI("panel_9s"), new Vector3(0, 1.64f, 0), new Vector2(1.8f, 0.44f), O + 17 + i * 10);
                var lb = Make.Text("label" + i, c.transform, $"<size=70%><color=#9aa6c8>{i + 1}</color></size>  " + (options[i].EpiphanyLabel ?? ""), new Vector3(0, 1.635f, 0), 0.24f, O + 18 + i * 10, new Color(0.953f, 0.831f, 0.549f));
                Make.Outline(lb, 0.25f, new Color(0.02f, 0.03f, 0.08f));
                lb.ForceMeshUpdate();
                lbg.size = new Vector2(Mathf.Max(1.6f, lb.preferredWidth + 0.4f), 0.44f);
                labels.Add(lb);
                // 축복이 얹힌 선택지 — 카드 뒤 금빛 · 아래 「축복!」 띠와 축복 글
                if (!string.IsNullOrEmpty(options[i].BlessName))
                {
                    Make.Box("blessglow", c.transform, Res.UI("card_glow"), Vector3.zero, new Vector2(2.9f, 3.7f), O + 8 + i * 10, new Color(1f, 0.8f, 0.3f, 0.9f), Res.SpriteMat(true, 2.6f));
                    var band = Make.Sliced("blessband", c.transform, Res.UI("pill_gold_9s"), new Vector3(0, -1.58f, 0), new Vector2(1.9f, 0.34f), O + 17 + i * 10);
                    var bt = Make.Text("blesst", c.transform, "축복! · " + options[i].BlessName, new Vector3(0, -1.585f, 0), 0.17f, O + 18 + i * 10, Tone.Brown);
                    bt.ForceMeshUpdate();
                    band.size = new Vector2(Mathf.Max(1.4f, bt.preferredWidth + 0.4f), 0.34f);
                    var tx = Tone.Text("blesstx", c.transform, options[i].BlessText ?? "", new Vector3(0, -1.82f, 0), 0.135f, O + 18 + i * 10, new Color(1f, 0.92f, 0.7f), TextAlignmentOptions.Top, true, 2.0f);
                    tx.textWrappingMode = TextWrappingModes.Normal;
                    tx.rectTransform.pivot = new Vector2(0.5f, 1);
                    tx.rectTransform.sizeDelta = new Vector2(2.0f, 1);
                    Make.Outline(tx, 0.25f, Tone.Outline);
                }
            }
            // 원래 카드는 빛으로 흩어진다
            Clock.Run(Clock.Tween(0.3f, t => card.SetAlpha(1 - t), true));
            Vfx.Burst(new Vector3(0, 0.1f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_UI_star_02", Count = 26, Speed = new Vector2(2f, 6f), Life = new Vector2(0.4f, 0.8f), Size = new Vector2(0.1f, 0.3f),
                C0 = new Color(1f, 0.85f, 0.4f), C1 = Color.white, Drag = 2f, Order = O + 45, Boost = 3f, Parent = root, ShrinkTo = 0,
            });
            yield return Clock.WaitU(0.55f);
            OnStage?.Invoke("epiphany_window");
            Options = opts;

            // 고르기 — 손가락(진짜 · 데모)으로 올려 누른다
            int choice = -1;
            int hover = -1, keyHover = -1;
            float waited = 0;
            while (choice < 0)
            {
                var p = PointerInput.Pos;
                // 키보드 — 숫자로 바로, ←→ 로 옮기고 엔터 · 스페이스로 고른다(마우스가 카드 위에 없을 때 키보드 자리가 산다)
                for (int k = 0; k < opts.Count && k < 9; k++)
                    if (PointerInput.Key(UnityEngine.InputSystem.Key.Digit1 + k) || PointerInput.Key(UnityEngine.InputSystem.Key.Numpad1 + k)) choice = k;
                if (PointerInput.Key(UnityEngine.InputSystem.Key.RightArrow)) keyHover = hover < 0 ? 0 : Mathf.Min(opts.Count - 1, hover + 1);
                if (PointerInput.Key(UnityEngine.InputSystem.Key.LeftArrow)) keyHover = hover < 0 ? opts.Count - 1 : Mathf.Max(0, hover - 1);
                int h = -1;
                for (int i = 0; i < opts.Count; i++)
                {
                    var local = opts[i].transform.InverseTransformPoint(p);
                    if (Mathf.Abs(local.x) < CardView.W / 2 && Mathf.Abs(local.y) < CardView.H / 2) h = i;
                }
                if (h >= 0) keyHover = -1;
                else h = keyHover;
                if (h != hover)
                {
                    hover = h;
                    for (int i = 0; i < opts.Count; i++)
                    {
                        opts[i].Hovered = i == h;
                        opts[i].TargetScale = i == h ? 1.25f : 1.08f;
                        opts[i].TargetPos = new Vector3((i - (opts.Count - 1) / 2f) * 3.5f, i == h ? 0.25f : 0.05f, 0);
                    }
                    if (h >= 0) Sfx.Play("card_hover", 0.4f);
                }
                if (PointerInput.Down && hover >= 0) choice = hover;
                if (hover >= 0 && (PointerInput.Key(UnityEngine.InputSystem.Key.Enter) || PointerInput.Key(UnityEngine.InputSystem.Key.NumpadEnter) || PointerInput.Key(UnityEngine.InputSystem.Key.Space))) choice = hover;
                waited += Time.unscaledDeltaTime;
                if (demoPick >= 0 && waited > 3f) choice = demoPick;   // 데모 손가락이 못 누른 때의 안전판
                yield return null;
            }
            Options = null;
            picked?.Invoke(choice);

            // 고른 것 — 나머지는 흩어지고, 고른 카드가 가운데로
            for (int i = 0; i < opts.Count; i++)
            {
                if (i == choice) continue;
                var c = opts[i];
                c.TargetPos = c.TargetPos + new Vector3(0, -6f, 0);
                c.TargetRot = (i < choice ? 25 : -25);
                Clock.Run(Clock.Tween(0.3f, t => { if (c) c.SetAlpha(1 - t); }, true));
            }
            var chosen = opts[choice];
            labels[choice].alpha = 0;
            chosen.TargetPos = new Vector3(0, 0.1f, 0);
            chosen.TargetScale = 1.3f;
            yield return Clock.WaitU(0.3f);

            // 변신 — 흰빛으로 타오르다(겹친 원래 카드가 다시 보인다) 바뀐 모습으로
            yield return Clock.Tween(0.22f, t => chosen.SetFlash(Ease.InCubic(t)), true);
            OnStage?.Invoke("epiphany_burn");
            card.Info = options[choice];
            card.Refresh();
            card.SetAlpha(1);
            card.SetFlash(1);
            card.transform.localPosition = new Vector3(0, 0.1f, 0);
            card.TargetPos = new Vector3(0, 0.1f, 0);
            card.transform.localScale = Vector3.one * 1.6f;
            card.TargetScale = 1.35f;
            card.SetOrder(O + 50);
            foreach (var c in opts) Object.Destroy(c.gameObject);
            Sfx.Play("epiphany", 0.9f);
            Vfx.Flash(new Color(1f, 0.95f, 0.8f, 0.3f), 0.3f, 1f, O + 60);
            Vfx.Ring(new Vector3(0, 0.1f, 0), 1f, 9f, 0.55f, new Color(1f, 0.85f, 0.5f, 1f), 3f, "FX_IN_Ring_ShockWave_01", O + 55, root, 1f, true);
            Vfx.Ring(new Vector3(0, 0.1f, 0), 0.5f, 6f, 0.45f, new Color(1f, 1f, 1f, 1f), 2.5f, "FX_IN_RIng_Hit_wave_01", O + 55, root, 1f, true);
            Vfx.Burst(new Vector3(0, 0.1f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_IN_Spark", Count = 40, Speed = new Vector2(6f, 15f), Life = new Vector2(0.3f, 0.6f), Size = new Vector2(0.08f, 0.2f),
                C0 = new Color(1f, 0.8f, 0.35f), C1 = Color.white, Stretch = true, StretchK = 0.04f, Drag = 3f, Order = O + 56, Boost = 4f, Parent = root,
            });
            Vfx.Burst(new Vector3(0, 0.1f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_UI_star_02", Count = 24, Speed = new Vector2(1.5f, 5f), Life = new Vector2(0.6f, 1.1f), Size = new Vector2(0.12f, 0.32f),
                C0 = new Color(1f, 0.9f, 0.5f), C1 = Color.white, Drag = 1.5f, Order = O + 56, Boost = 3f, Parent = root, ShrinkTo = 0, Spin = true,
            });
            var awake = Make.Text("awake", root, "신탁 · " + (options[choice].EpiphanyLabel ?? ""), new Vector3(0, -2.25f, 0), 0.5f, O + 61, new Color(1f, 0.9f, 0.55f));
            Make.Outline(awake, 0.25f, new Color(0.3f, 0.12f, 0));
            Make.Glow(awake, new Color(1f, 0.9f, 0.6f), 1.4f);
            yield return Clock.Tween(0.45f, t => card.SetFlash(1 - Ease.OutCubic(t)), true);
            OnStage?.Invoke("epiphany_reborn");
            yield return Clock.WaitU(0.45f);

            // 닫기 — 카드는 그대로 남아 이어서 나간다
            yield return Clock.Tween(0.25f, t =>
            {
                Make.Alpha(dim, 0.78f * (1 - t));
                raysMat.SetFloat("_Alpha", 0.55f * (1 - t));
                title.alpha = 1 - t; sub.alpha = 1 - t; awake.alpha = 1 - t; Make.Alpha(subBg, 1 - t);
            }, true);
            Object.Destroy(root.gameObject);
        }
    }
}
