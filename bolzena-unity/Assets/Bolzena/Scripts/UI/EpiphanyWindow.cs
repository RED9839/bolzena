using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 신탁 — 빛나는 카드를 내면: 화면이 어두워지며 「신탁!」, 카드가 가운데로 와서 세 갈래로 갈라져 펼쳐진다(내면 고르기).
    // 하나를 고르면 나머지는 흩어지고, 고른 카드가 원래 카드와 겹치며 하얗게 타올라 바뀐 모습으로 다시 태어난다.
    public static class EpiphanyWindow
    {
        const int O = 800;
        // 연출 세기(2026-10-07 사용자 「신탁 연출이 너무 화려」) — 빛 · 섬광 세기 · 입자 수 · 지속을 예전의 대략 절반(K 0.5).
        // 저사양 모드 · 움직임 줄이기면 더 약하게(K 0.3 — 고리 하나 · 빛살 안 돈다).
        static bool Calm => Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;
        static float K => Calm ? 0.3f : 0.5f;
        static int N(int n) => Mathf.Max(1, Mathf.RoundToInt(n * K));
        public static System.Action<string> OnStage;
        /// <summary>지금 열린 창의 고를 카드들(닫혀 있으면 null) — 입력 재현(InputProbe)이 짚는다.</summary>
        public static List<CardView> Options { get; private set; }

        // card — 손에서 뽑아 낸 그 카드(이 창이 끝나면 바뀐 모습으로 돌려준다). demoPick 이 0 이상이면 그것을 고른다(자동 데모)
        public static IEnumerator Run(Transform parent, CardView card, IReadOnlyList<CardInfo> options, System.Action<int> picked, int demoPick = -1, System.Action openDeck = null)
        {
            var root = Make.Node("Epiphany", parent);
            // 창이 열린 동안은 시간이 흐르게 — 일시정지 · 맞는 순간 멈칫이 남아 있으면 창 안의 연출(화면 시계)이 멈춘다
            Clock.Paused = false;
            Clock.ClearStops();
            Sfx.Play("epiphany_open", 0.6f);
            TextMeshPro bandTxt = null;
            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(44, 16), O, new Color(0f, 0f, 0f, 0));
            var raysMat = Res.NewMat("Bolzena/Rays");
            raysMat.SetColor("_Color", new Color(1f, 0.82f, 0.4f, 1f));
            raysMat.SetFloat("_Count", 14);
            raysMat.SetFloat("_Spin", Calm ? 0f : 0.025f);
            raysMat.SetFloat("_Boost", 1.6f * K);
            raysMat.SetFloat("_Inner", 0.08f);
            raysMat.SetFloat("_Outer", 0.55f);
            var rays = Make.Quad("rays", root, new Vector3(0, 0.2f, 0), new Vector2(18, 18), raysMat, O + 1);
            Make.Own(rays.gameObject, raysMat);
            rays.gameObject.SetActive(false);   // 단순한 창(2026-10-08 시안) — 빛살 없음
            var title = Make.Text("title", root, "<color=#ffd76a>신탁 효과</color>를 결정하세요.", new Vector3(0, 3.45f, 0), 0.5f, O + 40, Color.white);
            Make.Outline(title, 0.22f, Color.black);
            // 아래 안내(회색 작은 글) — 길게 누르면 카드 상세. 폰엔 오른쪽 단추가 없어 「(우클릭)」을 뺀다
            var sub = Make.Text("sub", root, Application.isMobilePlatform ? "정보를 확인하려면 카드를 길게 누르세요." : "정보를 확인하려면 카드를 길게 누르세요.(우클릭)", new Vector3(0, -4.05f, 0), 0.2f, O + 40, new Color(0.82f, 0.84f, 0.9f));
            Make.Outline(sub, 0.3f, Color.black);
            // 제목 아래 가는 금빛 줄 + 가운데 별 하나
            var subBg = Make.Box("titleline", root, Res.UI("band_line"), new Vector3(0, 3.0f, 0), new Vector2(5.2f, 0.03f), O + 40, new Color(1f, 0.84f, 0.45f, 0));
            var titleStar = Make.Box("titlestar", root, Bolzena.RunUI.Theme.S("ic_spark"), new Vector3(0, 3.0f, 0), new Vector2(0.24f, 0.24f), O + 41, new Color(1f, 0.88f, 0.5f, 0));
            title.alpha = 0; sub.alpha = 0;
            var fadeSr = new List<(SpriteRenderer sr, float a)>();
            // 축복이 얹힌 선택지가 있으면 겨우살이(축복을 주는 존재) 일러스트를 크게 깔고 아래 띠 「겨우살이 축복」
            bool anyBless = false;
            if (!Bolzena.RunUI.BlessFx.Old) foreach (var o in options) if (!string.IsNullOrEmpty(o.BlessName)) anyBless = true;
            SkeletonAnimation noone = null;
            if (anyBless)
            {
                var data = Res.Spine("st_noone");
                if (data != null)
                {
                    noone = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                    noone.transform.SetParent(root, false);
                    var an = SpineAnim(noone.Skeleton.Data);   // 웃는 표정
                    var ca = System.Environment.GetCommandLineArgs(); int ni = System.Array.IndexOf(ca, "-noone"); if (ni >= 0 && ni + 1 < ca.Length && noone.Skeleton.Data.FindAnimation(ca[ni + 1]) != null) an = ca[ni + 1];
                    if (an != null) noone.AnimationState.SetAnimation(0, an, true);
                    noone.Update(0f); noone.LateUpdate();
                    var mr = noone.GetComponent<MeshRenderer>();
                    mr.sortingOrder = O + 2;
                    var bb0 = mr.bounds;
                    float k0 = bb0.size.y > 0.01f ? 8.6f / bb0.size.y : 1f;
                    noone.transform.localScale = Vector3.one * k0;
                    noone.transform.localPosition = new Vector3(-6.2f - bb0.center.x * k0, -5.0f - (bb0.min.y - noone.transform.position.y) * k0, 0);
                    noone.Skeleton.SetColor(new Color(1f, 1f, 1f, 0f));
                }
                bandTxt = Make.Text("orabandtx", root, "겨우살이 축복", new Vector3(0, -3.5f, 0), 0.36f, O + 42, Color.white);
                Make.Outline(bandTxt, 0.3f, Color.black);
                bandTxt.alpha = 0;
            }
            void Fade(float f) { foreach (var x in fadeSr) if (x.sr) Make.Alpha(x.sr, x.a * f); if (bandTxt) bandTxt.alpha = f; if (noone != null) noone.Skeleton.SetColor(new Color(1f, 1f, 1f, 0.92f * f)); }
            // 덱 보기(2026-10-06 사용자: 「신탁 때 자기 덱 눌러서 덱 풀 볼 수 있게」) — 오른쪽 위. 더미 보기 창이 이 창 위에 열리고, 닫으면 고르기가 그대로 이어진다
            SpriteRenderer deckBg = null; TextMeshPro deckTx = null;
            if (openDeck != null)
            {
                float halfW = Camera.main != null ? Camera.main.orthographicSize * Camera.main.aspect : 8f;
                var at = new Vector3(Mathf.Max(5.5f, halfW - 1.6f), 3.55f, 0);
                deckBg = Make.Sliced("deckbtn", root, Res.UI("panel_9s"), at, new Vector2(2.4f, 0.62f), O + 41);
                deckTx = Make.Text("decktx", root, "덱 보기", at, 0.26f, O + 42, new Color(1f, 0.92f, 0.8f));
            }

            // 카드를 가운데로
            // 원래 카드는 곧바로 숨는다(가운데 금빛 · 그림자 잔상이 남지 않게 — 끝에 고른 모습으로 되살린다)
            card.SetAlpha(0);
            card.gameObject.SetActive(false);
            yield return Clock.Tween(0.2f, t =>
            {
                Make.Alpha(dim, 0.99f * t);
                title.alpha = t; sub.alpha = t; Make.Alpha(subBg, 0.85f * t); Fade(t);
            }, true);
            Sfx.Play("epiphany", 0.5f, 1.2f);

            // 세 갈래로 — 가운데 카드에서 겹쳐 나와 펼쳐진다
            var opts = new List<CardView>();
            var sparkles = new List<Glint>();
            var live = new bool[] { true };
            for (int i = 0; i < options.Count; i++)
            {
                var c = CardView.Create(root, options[i]);
                                c.ShowPin = false;   // 신탁 창은 오른쪽 위 주인 얼굴 없이(2026-10-06 사용자)
                c.transform.localPosition = new Vector3(0, 0.1f, 0);
                c.transform.localScale = Vector3.one * 1.3f;
                c.Follow = 9f;
                c.TargetPos = new Vector3((i - (options.Count - 1) / 2f) * 3.7f, 0f, 0);
                c.TargetRot = 0;
                c.TargetScale = 1.55f;
                c.SetAlpha(0);
                Clock.Run(Clock.Tween(0.25f, t => { if (c) c.SetAlpha(t); }, true));
                c.SetOrder(O + 10 + i * 10);
                opts.Add(c);
                // 축복이 얹힌 선택지 — 금빛 · 연두 테두리 빛 · 반짝이 · 비용 아래 문장 · 본문 끝 축복 효과 줄(카제나 「신성 번뜩임」 꼴)
                if (!string.IsNullOrEmpty(options[i].BlessName) && !Bolzena.RunUI.BlessFx.Old) AddBless(c, options[i], O + 10 + i * 10, sparkles);
            }
            if (sparkles.Count > 0) Clock.Run(Sparkle(sparkles, live));
            yield return Clock.WaitU(0.3f);
            OnStage?.Invoke("epiphany_window");
            Options = opts;

            // 고르기 — 손가락(진짜 · 데모)으로 올려 누른다
            int choice = -1;
            int hover = -1, keyHover = -1, press = -1;
            float pressT = 0; bool zoomed = false;
            float waited = 0;
            while (choice < 0)
            {
                if (Modal.Open != null) { waited = 0; yield return null; continue; }   // 덱 보기가 열린 동안은 고르기 멈춤(건너뛰는 길 없음)
                var p = PointerInput.Pos;
                if (deckBg != null && PointerInput.Down)
                {
                    var dl = deckBg.transform.InverseTransformPoint(p);
                    if (Mathf.Abs(dl.x) < 1.2f && Mathf.Abs(dl.y) < 0.31f) { openDeck(); yield return null; continue; }
                }
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
                        opts[i].TargetScale = i == h ? 1.65f : 1.55f;
                        opts[i].TargetPos = new Vector3((i - (opts.Count - 1) / 2f) * 3.7f, i == h ? 0.1f : 0f, 0);
                    }
                    if (h >= 0) Sfx.Play("card_hover", 0.4f);
                }
                // 누르면 고르기 — 단 길게(0.3초) 누르면 카드 상세(CardZoom)를 열고 놓아도 고르지 않는다. PC 는 우클릭으로도 상세
                if (PointerInput.Down) { press = hover; pressT = 0; zoomed = false; }
                if (PointerInput.Held && press >= 0 && !PointerInput.Moved) { pressT += Time.unscaledDeltaTime; if (pressT >= 0.3f) zoomed = true; }
                if (PointerInput.RightDown && hover >= 0) { press = hover; zoomed = !zoomed; }
                if (zoomed && press >= 0 && press < opts.Count) CardZoom.Show(root, options[press], new Vector3(0, 0.25f, 0), 1.7f);
                else CardZoom.Hide();
                if (PointerInput.Up)
                {
                    if (!zoomed && press >= 0 && press == hover) choice = hover;
                    if (!PointerInput.RightDown) { zoomed = false; }
                    press = -1;
                }
                if (hover >= 0 && (PointerInput.Key(UnityEngine.InputSystem.Key.Enter) || PointerInput.Key(UnityEngine.InputSystem.Key.NumpadEnter) || PointerInput.Key(UnityEngine.InputSystem.Key.Space))) choice = hover;
                waited += Time.unscaledDeltaTime;
                if (demoPick >= 0 && waited > 3f) choice = demoPick;   // 데모 손가락이 못 누른 때의 안전판
                yield return null;
            }
            Options = null; live[0] = false;
            CardZoom.Hide();
            if (deckBg != null) { deckBg.enabled = false; deckTx.alpha = 0; }
            picked?.Invoke(choice);
            OnStage?.Invoke("epiphany_pick");

            // 고른 뒤 — 연출 없음(2026-10-08 사용자). 나머지 둘은 빠르게 흐려지고, 고른 카드는 그 자리에서 곧바로 이어서 나간다(손패로)
            var chosen = opts[choice];
            foreach (var oc in opts) foreach (Transform ch in oc.transform) if (ch.name.StartsWith("bless") || ch.name == "glint") ch.gameObject.SetActive(false);
            for (int i = 0; i < opts.Count; i++)
            {
                if (i == choice) continue;
                var c = opts[i];
                Clock.Run(Clock.Tween(0.15f, t => { if (c) c.SetAlpha(1 - t); }, true));
            }
            card.Info = options[choice];
            card.gameObject.SetActive(true);
            card.Refresh();
            card.SetAlpha(1);
            card.Follow = 14f;
            card.transform.localPosition = chosen.transform.localPosition;
            card.transform.localScale = chosen.transform.localScale;
            card.TargetPos = chosen.TargetPos;
            card.TargetScale = chosen.TargetScale;
            card.SetOrder(O + 50);
            chosen.SetAlpha(0);
            OnStage?.Invoke("epiphany_burn");
            yield return Clock.Tween(0.2f, t =>
            {
                Make.Alpha(dim, 0.99f * (1 - t));
                title.alpha = 1 - t; sub.alpha = 1 - t; Make.Alpha(subBg, 0.85f * (1 - t)); Fade(1 - t);
            }, true);
            OnStage?.Invoke("epiphany_reborn");
            foreach (var c in opts) Object.Destroy(c.gameObject);
            Object.Destroy(root.gameObject);
        }

        // ── 축복 선택지 꾸밈 ──
        sealed class Glint { public SpriteRenderer Sr; public float Phase, Speed, X, Y, Size; }
        static readonly Color Lav = new Color(0.72f, 0.58f, 1f), LavLight = new Color(0.88f, 0.8f, 1f);
        static string SpineAnim(Spine.SkeletonData d)
        {
            foreach (var n in new[] { "Happy_3", "Happy_1", "Happy_2", "Happy", "Idle_1", "Idle", "Normal" }) if (d.FindAnimation(n) != null) return n;
            return d.Animations.Count > 0 ? d.Animations.Items[0].Name : null;
        }

        static void AddBless(CardView c, CardInfo info, int o, List<Glint> sparkles)
        {
            var t = c.transform;
            float W = CardView.W, H = CardView.H;
            // 테두리 빛 — 바깥 넓은 금빛 + 안쪽 연두빛
            Make.Box("blessglow", t, Res.UI("card_glow"), Vector3.zero, new Vector2(2.9f, 3.7f), o - 2, new Color(Lav.r, Lav.g, Lav.b, Calm ? 0.55f : 0.9f), Res.SpriteMat(true, Calm ? 1.6f : 2.6f));
            Make.Box("blesslime", t, Res.UI("card_glow"), Vector3.zero, new Vector2(2.4f, 3.2f), o - 1, new Color(0.9f, 0.85f, 1f, Calm ? 0.3f : 0.5f), Res.SpriteMat(true, Calm ? 1.4f : 2.0f));
            // 반짝이
            int n = Calm ? 3 : 9;
            for (int k = 0; k < n; k++)
            {
                float sz = Random.Range(0.09f, 0.2f);
                var sr = Make.Box("glint", t, Bolzena.RunUI.Theme.S("ic_spark"), Vector3.zero, new Vector2(sz, sz), o + 15, new Color(0.88f, 0.8f, 1f, 0), Res.SpriteMat(true, 2.2f));
                sparkles.Add(new Glint { Sr = sr, Phase = Random.value, Speed = Random.Range(0.6f, 1.2f), Size = sz,
                    X = 0, Y = 0 });
                var gg = sparkles[sparkles.Count - 1];
                if (Random.value < 0.5f) { gg.X = Mathf.Sign(Random.value - 0.5f) * (W / 2 + Random.Range(0.02f, 0.16f)); gg.Y = Random.Range(-H / 2, H / 2); }
                else { gg.Y = Mathf.Sign(Random.value - 0.5f) * (H / 2 + Random.Range(0.02f, 0.16f)); gg.X = Random.Range(-W / 2, W / 2); }
            }
        }

        static IEnumerator Sparkle(List<Glint> gl, bool[] live)
        {
            float t = 0;
            while (live[0])
            {
                t += Clock.UDt;
                foreach (var g in gl)
                {
                    if (!g.Sr) continue;
                    float u = Mathf.Repeat(t * g.Speed * (Calm ? 0.3f : 1f) + g.Phase, 1f);
                    g.Sr.transform.localPosition = new Vector3(g.X, g.Y + u * 0.15f, 0);
                    g.Sr.transform.localEulerAngles = new Vector3(0, 0, u * 90f);
                    float s = Mathf.Sin(u * Mathf.PI);
                    g.Sr.transform.localScale = Vector3.one * (g.Size / Mathf.Max(0.01f, g.Sr.sprite.bounds.size.x)) * (0.5f + s * 0.8f);
                    Make.Alpha(g.Sr, s * 0.95f);
                }
                yield return null;
            }
        }
    }
}
