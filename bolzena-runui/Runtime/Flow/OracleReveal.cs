using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    /// <summary>고르기 연출의 후보 한 장 — 카드 id 와 그 모습(신탁을 얹은 것), 카드 위 이름표, 얹힌 축복.</summary>
    public sealed class RevealPick
    {
        public string CardId;
        public Core.CardView View;
        public string Label;
        public string BlessName, BlessText;

        /// <summary>신탁 후보(Core.OracleOption — 전투 · 보상 · 이벤트 · 캠프가 같은 꼴)를 연출 후보로.</summary>
        public static List<RevealPick> Of(Core.GameData d, string cardId, IList<Core.OracleOption> opts)
        {
            var o = new List<RevealPick>();
            foreach (var x in opts)
                o.Add(new RevealPick { CardId = cardId, View = d.View(cardId, x.N), Label = x.Name, BlessName = x.Blessed ? x.BlessName : null, BlessText = x.Blessed ? x.BlessText : null });
            return o;
        }
    }

    /// <summary>고르기 연출의 설정. Try 가 null 이 아닌 글을 돌려주면 못 고른 것(알림만 띄우고 창은 그대로).</summary>
    public sealed class RevealOpts
    {
        public string Title = "신탁!";
        public string Sub;
        /// <summary>가운데에서 갈라지는 원래 카드(없으면 후보가 곧장 펼쳐진다).</summary>
        public string BaseCardId;
        public Core.CardView BaseView;
        public List<RevealPick> Picks = new List<RevealPick>();
        public Func<int, string> Try;
        /// <summary>다시 태어나는 연출이 끝난 뒤.</summary>
        public Action<int> Done;
        /// <summary>닫기(×) · 뒤로 키를 허락하나. false 면 반드시 하나를 고른다.</summary>
        public bool Cancel;
        public Action Cancelled;
        /// <summary>후보 단추 이름 — Stage.Hot[Hot + i](자동 데모가 누른다).</summary>
        public string Hot = "reveal.opt";
        /// <summary>고른 뒤 카드 아래 큰 글의 머리 — 「신탁 · 이름」.</summary>
        public string Awake = "신탁";
    }

    /// <summary>
    /// 신탁 · 은총 고르기 연출(2026-10-06 사용자: 「휴식 수련도 신탁과 같은 임팩트로」) — 본 게임 EpiphanyWindow 의 uGUI 판.
    /// 어두운 막 · 빛살 → 「신탁!」 → 원래 카드가 가운데서 후보로 갈라져 펼쳐짐 → 고르면 나머지는 흩어지고 고른 카드가 흰빛으로 타올라 다시 태어남.
    /// 번호는 붙이지 않는다(이름만). 캠프 수련 · 보상의 빛났던 카드(GlowPick)가 쓰고, 이벤트 신탁 창도 같은 Show 를 쓰면 된다.
    /// 효과음 자리: reveal_open · reveal · reveal_hover · reveal_reborn(Resources/RunArt/Sfx — 파일이 없으면 소리 없이 지나간다).
    /// </summary>
    public static class OracleReveal
    {
        /// <summary>지금 단계 — null(닫힘) · enter · choose · burn · reborn · leave. 자동 데모가 찍는 때를 본다.</summary>
        public static string Phase { get; private set; }
        public static bool IsOpen => Phase != null;
        /// <summary>신탁 창에서 덱 보기를 연 횟수(자동 데모가 본다).</summary>
        public static int DeckOpens { get; private set; }

        public static void Show(Flow f, RevealOpts o) => f.StartCoroutine(Run(f, o));

        static IEnumerator Run(Flow f, RevealOpts o)
        {
            var st = f.Stage;
            int n = o.Picks.Count;
            if (n == 0) yield break;
            Phase = "enter";
            bool compact = Theme.Compact;
            var size = st.Size;
            float cw = Mathf.Min(Theme.C(236, 168), (size.x - 80) / (n * 1.18f)), ch = cw * 1.4f, gap = cw * 1.22f;
            float cy = compact ? -18 : -6;

            var root = Ui.Rect("reveal", st.ModalLayer).Fill();
            var back = root.gameObject.AddComponent<BackClose>();
            back.Locked = true;
            var dim = Ui.Img(root, Theme.White, new Color(0.03f, 0.02f, 0.06f, 0), "dim", true);   // 밑 화면 누르기를 막는다
            dim.rectTransform.Fill();
            var rays = new GameObject("rays", typeof(RectTransform)).AddComponent<RawImage>();
            rays.transform.SetParent(root, false);
            rays.texture = RaysTex(); rays.raycastTarget = false;
            rays.color = new Color(1f, 0.82f, 0.4f, 0);
            float rs = Mathf.Max(size.x, size.y) * 1.5f;
            rays.rectTransform.At(0.5f, 0.5f, 0, cy, rs, rs);
            rays.gameObject.AddComponent<Spin>().Speed = 6;
            var core = Ui.Img(root, Theme.S("soft"), new Color(1f, 0.85f, 0.45f, 0), "core");
            core.rectTransform.At(0.5f, 0.5f, 0, cy, cw * 3.2f, cw * 3.2f);

            var title = Ui.Title(root, o.Title, compact ? 64 : 84, new Color(1f, 0.95f, 0.75f), TextAlignmentOptions.Center);
            title.rectTransform.At(0.5f, 1, 0, compact ? -42 : -60, 900, compact ? 80 : 100);
            title.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.78f, 0.3f), new Color(1f, 0.78f, 0.3f));
            title.Outline(0.22f, new Color(0.35f, 0.15f, 0));
            title.alpha = 0;
            var subBg = Ui.Img(root, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0), "subbg");
            subBg.rectTransform.At(0.5f, 1, 0, compact ? -116 : -158, Mathf.Min(size.x - 40, compact ? 900 : 1100), compact ? 40 : 46);
            var sub = Ui.Text(subBg.transform, o.Sub ?? "", Theme.FsMd, new Color(1f, 0.92f, 0.8f), TextAlignmentOptions.Center);
            sub.rectTransform.Fill(20, 0, 20, 0); sub.alpha = 0;
            sub.enableAutoSizing = true; sub.fontSizeMin = 12; sub.fontSizeMax = Theme.FsMd;

            Btn close = null;
            if (o.Cancel)
            {
                Action cancel = () =>
                {
                    if (Phase != "choose") return;
                    Phase = "leave";
                    var g = root.Group();
                    Tw.Run(root, 0.2f, t => { if (g) g.alpha = 1 - t; }, Tw.Linear, 0, () => { if (root) UnityEngine.Object.Destroy(root.gameObject); Phase = null; o.Cancelled?.Invoke(); });
                };
                close = Btn.Icon(root, Theme.S("ic_x"), cancel, 52, "close");
                close.GetComponent<RectTransform>().At(1, 1, -Theme.Gutter, -Theme.Gutter, 52, 52);
                close.Interactable = false;
                st.Hot["modal.x"] = close;
                st.Hot[o.Hot + ".close"] = close;
                back.Close = cancel;
            }

            // 덱 보기(2026-10-06 사용자: 「신탁 때 자기 덱 눌러서 덱 풀 볼 수 있게」) — 오른쪽 위 단추 + 머리 띠 덱 아이콘 자리(어두운 막 위에 같은 자리로 덮는다).
            //   덱 보기는 이 창 위에 열리고, 닫으면 이 창이 그대로(고르기 · 연출은 그대로 흐른다). 덱 보기 안에는 고르기를 건너뛰는 길이 없다.
            System.Action openDeck = () => { if (Phase == "leave" || Phase == "reborn") return; DeckOpens++; f.DeckView(); };
            var deckB = Btn.Make(root, null, BtnStyle.PillDark, openDeck, 0, "deckbtn");
            deckB.GetComponent<RectTransform>().At(1, 1, -Theme.Gutter - (o.Cancel ? 66 : 0), compact ? -78 : -96, compact ? 150 : 176, compact ? 46 : 54);
            var dic = Ui.Img(deckB.transform, Theme.S("ic_deck"), Theme.Gold, "ic"); dic.rectTransform.At(0, 0.5f, 18, 0, 26, 26); dic.preserveAspect = true;
            var dl = Ui.Title(deckB.transform, $"덱 보기 <size=80%><color={Theme.SubTag}>{f.P.S.Deck.Count}</color></size>", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft); dl.rectTransform.Fill(52, 0, 10, 0);
            st.Hot[o.Hot + ".deck"] = deckB;
            if (st.Hot.TryGetValue("deck", out var hudDeck) && hudDeck != null)
            {
                var hb = Ui.Rect("huddeck", root); var hr = (RectTransform)hudDeck.transform;
                hb.anchorMin = hb.anchorMax = new Vector2(0.5f, 0.5f); hb.pivot = hr.pivot; hb.position = hr.position; hb.sizeDelta = hr.rect.size * (hr.lossyScale.x / Mathf.Max(0.0001f, root.lossyScale.x));
                var hbi = Ui.Img(hb, Theme.White, new Color(0, 0, 0, 0), "hit", true); hbi.rectTransform.Fill();
                var hbb = hb.gameObject.AddComponent<Btn>(); hbb.Bg = null; hbb.OnClick = openDeck;
            }

            // 원래 카드를 가운데로
            RectTransform baseCard = null;
            if (o.BaseCardId != null)
            {
                var bh = Ui.Rect("base", root).At(0.5f, 0.5f, 0, cy, cw, ch);
                baseCard = W.Card(bh, f, o.BaseCardId, cw, "card", o.BaseView, noFace: true);
                baseCard.At(0.5f, 0.5f, 0, 0, cw, ch);
                bh.localScale = Vector3.one * 0.8f;
            }
            Sfx.Play("reveal_open", 0.6f);
            yield return Lerp(root, 0.35f, t =>
            {
                dim.color = dim.color.A(0.8f * t);
                rays.color = rays.color.A(0.5f * t);
                core.color = core.color.A(0.55f * t);
                title.alpha = t; sub.alpha = t; subBg.color = subBg.color.A(t);
                title.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(2.2f, 1f, Tw.OutBack(t));
                if (baseCard) baseCard.parent.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.15f, Tw.OutCubic(t));
            });
            if (!root) { Phase = null; yield break; }
            Tw.Pulse(core, 0.35f, 0.6f, 2.2f);
            Sfx.Play("reveal", 0.5f);

            // 후보 — 가운데에서 겹쳐 나와 펼쳐진다
            var holders = new List<RectTransform>();
            var homes = new List<Vector2>();
            var labels = new List<CanvasGroup>();
            int chosen = -1;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var p = o.Picks[i];
                var h = Ui.Rect("pick" + i, root).At(0.5f, 0.5f, 0, cy, cw, ch);
                var face = W.Card(h, f, p.CardId, cw, "card", p.View, noFace: true);   // 신탁 창은 주인 얼굴 핀 없이
                face.At(0.5f, 0.5f, 0, 0, cw, ch);
                // 이름표(번호 없이)
                if (!string.IsNullOrEmpty(p.Label))
                {
                    var lb = Ui.Img(h, Theme.S("pill_dark", 46), Color.white, "label");
                    lb.rectTransform.At(0.5f, 1, 0, compact ? 34 : 42, cw * 1.08f, compact ? 32 : 38);
                    var lt = Ui.Title(lb.transform, p.Label, compact ? 15 : 19, new Color(0.953f, 0.831f, 0.549f), TextAlignmentOptions.Center);
                    lt.rectTransform.Fill(10, 0, 10, 0);
                    lt.enableAutoSizing = true; lt.fontSizeMin = 10; lt.fontSizeMax = compact ? 15 : 19;
                    labels.Add(lb.Group());
                }
                else labels.Add(null);
                // 축복이 얹힌 후보 — 카드 뒤 금빛 · 아래 「축복!」 띠와 축복 글
                if (!string.IsNullOrEmpty(p.BlessName))
                {
                    var gl = Ui.Img(h, Theme.S("soft"), new Color(1f, 0.8f, 0.3f, 0.75f), "blessglow");
                    gl.rectTransform.At(0.5f, 0.5f, 0, 0, cw * 1.6f, ch * 1.45f); gl.transform.SetAsFirstSibling();
                    Tw.Pulse(gl, 0.45f, 0.85f, 1.6f);
                    var band = Ui.Img(h, Theme.S("pill_gold", 46), Color.white, "blessband");
                    band.rectTransform.At(0.5f, 0, 0, compact ? -30 : -36, cw * 1.05f, compact ? 26 : 30);
                    var bt = Ui.Title(band.transform, "축복! · " + p.BlessName, compact ? 13 : 16, Theme.Brown, TextAlignmentOptions.Center);
                    bt.rectTransform.Fill(8, 0, 8, 0); bt.enableAutoSizing = true; bt.fontSizeMin = 9; bt.fontSizeMax = compact ? 13 : 16;
                    if (!string.IsNullOrEmpty(p.BlessText))
                    {
                        var tx = Ui.Text(h, p.BlessText, compact ? 11 : 13, new Color(1f, 0.92f, 0.7f), TextAlignmentOptions.Top);
                        tx.rectTransform.At(0.5f, 0, 0, compact ? -70 : -84, cw * 1.15f, compact ? 40 : 46);
                        tx.enableAutoSizing = true; tx.fontSizeMin = 9; tx.fontSizeMax = compact ? 11 : 13;
                    }
                }
                var b = face.gameObject.AddComponent<Btn>();
                b.OnClick = () =>
                {
                    if (Phase != "choose" || chosen >= 0) return;
                    var why = o.Try?.Invoke(idx);
                    if (why != null) { Toast.Show(why); return; }
                    chosen = idx;
                };
                st.Hot[o.Hot + i] = b;
                holders.Add(h);
                homes.Add(new Vector2((i - (n - 1) / 2f) * gap, cy));
                h.localScale = Vector3.one * 1.1f;
            }
            if (baseCard) baseCard.parent.SetAsLastSibling();
            title.transform.SetAsLastSibling(); subBg.transform.SetAsLastSibling();
            if (close) close.transform.SetAsLastSibling();
            deckB.transform.SetAsLastSibling();
            var baseG = baseCard ? baseCard.parent.Group() : null;
            Burst(root, new Vector2(0, cy), 26, 120, 420, new Color(1f, 0.85f, 0.4f), 0.5f, 0.9f);
            yield return Lerp(root, 0.45f, t =>
            {
                float k = Tw.OutBack(t);
                for (int i = 0; i < holders.Count; i++)
                    if (holders[i]) { holders[i].anchoredPosition = Vector2.LerpUnclamped(new Vector2(0, cy), homes[i], k); holders[i].localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, t); }
                if (baseG) baseG.alpha = 1 - t;
            });
            if (!root) { Phase = null; yield break; }
            if (baseCard) UnityEngine.Object.Destroy(baseCard.parent.gameObject);
            Phase = "choose";
            if (close) close.Interactable = true;
            back.Locked = !o.Cancel;

            // 고르기 — 올린 카드는 조금 떠오른다
            int hover = -1;
            while (chosen < 0)
            {
                if (!root || Phase != "choose") { if (Phase == "leave" || !root) yield break; }
                int hv = -1;
                for (int i = 0; i < holders.Count; i++) { var hb = holders[i] ? holders[i].GetComponentInChildren<Btn>() : null; if (hb != null && hb.IsHovered) hv = i; }
                if (hv != hover) { hover = hv; if (hv >= 0) Sfx.Play("reveal_hover", 0.4f); }
                for (int i = 0; i < holders.Count; i++)
                {
                    if (!holders[i]) continue;
                    var want = homes[i] + new Vector2(0, i == hover ? 16 : 0);
                    holders[i].anchoredPosition = Vector2.Lerp(holders[i].anchoredPosition, want, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
                    float sc = Mathf.Lerp(holders[i].localScale.x, i == hover ? 1.08f : 1f, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
                    holders[i].localScale = Vector3.one * sc;
                }
                yield return null;
            }
            back.Locked = true;
            if (close) UnityEngine.Object.Destroy(close.gameObject);
            if (deckB) UnityEngine.Object.Destroy(deckB.gameObject);

            // 고른 것 — 나머지는 흩어지고, 고른 카드가 가운데로
            Phase = "burn";
            var ch0 = holders[chosen];
            ch0.SetAsLastSibling();
            if (labels[chosen]) labels[chosen].alpha = 0;
            var from = holders.ConvertAll(h => h ? h.anchoredPosition : Vector2.zero);
            var flash = Ui.Img(ch0, Theme.White, new Color(1, 1, 1, 0), "flash");
            flash.rectTransform.Fill();
            yield return Lerp(root, 0.32f, t =>
            {
                for (int i = 0; i < holders.Count; i++)
                {
                    if (!holders[i]) continue;
                    if (i == chosen) { holders[i].anchoredPosition = Vector2.Lerp(from[i], new Vector2(0, cy), Tw.OutCubic(t)); holders[i].localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, t); continue; }
                    holders[i].anchoredPosition = from[i] + new Vector2(0, -420 * t * t);
                    holders[i].localEulerAngles = new Vector3(0, 0, (i < chosen ? 25 : -25) * t);
                    holders[i].Group().alpha = 1 - t;
                }
            });
            if (!root) { Phase = null; yield break; }
            yield return Lerp(root, 0.22f, t => { if (flash) flash.color = flash.color.A(t * t * t); });

            // 다시 태어남 — 흰빛이 걷히며 바뀐 모습으로 · 빛 고리 · 불티
            Phase = "reborn";
            for (int i = 0; i < holders.Count; i++) if (i != chosen && holders[i]) UnityEngine.Object.Destroy(holders[i].gameObject);
            Sfx.Play("reveal_reborn", 0.9f);
            var sflash = Ui.Img(root, Theme.White, new Color(1f, 0.95f, 0.8f, 0.35f), "sflash"); sflash.rectTransform.Fill();
            Tw.Run(sflash, 0.35f, t => { if (sflash) sflash.color = sflash.color.A(0.35f * (1 - t)); });
            Ring(root, new Vector2(0, cy), cw * 0.6f, cw * 3.4f, 0.55f, new Color(1f, 0.85f, 0.5f, 1f));
            Ring(root, new Vector2(0, cy), cw * 0.3f, cw * 2.4f, 0.45f, Color.white);
            Burst(root, new Vector2(0, cy), 36, 300, 900, new Color(1f, 0.8f, 0.35f), 0.3f, 0.6f);
            Burst(root, new Vector2(0, cy), 20, 80, 320, new Color(1f, 0.9f, 0.5f), 0.6f, 1.1f);
            var pk = o.Picks[chosen];
            var awake = Ui.Title(root, string.IsNullOrEmpty(pk.Label) ? o.Awake : $"{o.Awake} · {pk.Label}", compact ? 34 : 44, new Color(1f, 0.9f, 0.55f), TextAlignmentOptions.Center);
            awake.rectTransform.At(0.5f, 0.5f, 0, cy - ch * 1.3f / 2 - (compact ? 34 : 48), 1200, 60);
            awake.Outline(0.25f, new Color(0.3f, 0.12f, 0));
            yield return Lerp(root, 0.45f, t =>
            {
                if (flash) flash.color = flash.color.A(1 - Tw.OutCubic(t));
                if (ch0) ch0.localScale = Vector3.one * Mathf.Lerp(1.6f, 1.35f, Tw.OutCubic(t));
                awake.alpha = t;
            });
            yield return Wait(0.6f);
            Phase = "leave";
            if (root)
            {
                var g = root.Group();
                yield return Lerp(root, 0.25f, t => { if (g) g.alpha = 1 - t; });
                if (root) UnityEngine.Object.Destroy(root.gameObject);
            }
            Phase = null;
            o.Done?.Invoke(chosen);
        }

        static IEnumerator Lerp(UnityEngine.Object owner, float dur, Action<float> step)
        {
            if (Settings.ReduceMotion) dur *= 0.4f;
            float t = 0;
            while (t < dur) { if (!owner) yield break; step(t / dur); t += Time.unscaledDeltaTime; yield return null; }
            if (owner) step(1);
        }

        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(Settings.ReduceMotion ? s * 0.4f : s); }

        static void Ring(RectTransform root, Vector2 at, float r0, float r1, float dur, Color c)
        {
            var im = Ui.Img(root, Theme.S("ring"), c, "ringfx");
            var rt = im.rectTransform; rt.At(0.5f, 0.5f, at.x, at.y, r0, r0);
            Tw.Run(rt, dur, t => { if (!rt) return; float r = Mathf.Lerp(r0, r1, Tw.OutCubic(t)); rt.sizeDelta = new Vector2(r, r); im.color = c.A(c.a * (1 - t)); }, Tw.Linear, 0, () => { if (rt) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        static void Burst(RectTransform root, Vector2 at, int count, float v0, float v1, Color c, float life0, float life1)
        {
            if (Settings.ReduceMotion) count /= 3;
            for (int i = 0; i < count; i++)
            {
                var im = Ui.Img(root, Theme.S(i % 3 == 0 ? "ic_spark" : "soft"), c, "spark");
                var rt = im.rectTransform;
                float s = UnityEngine.Random.Range(8f, 22f);
                rt.At(0.5f, 0.5f, at.x, at.y, s, s);
                float a = UnityEngine.Random.Range(0, Mathf.PI * 2), v = UnityEngine.Random.Range(v0, v1);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Tw.Run(rt, UnityEngine.Random.Range(life0, life1), t =>
                {
                    if (!rt) return;
                    rt.anchoredPosition = at + dir * v * (1 - (1 - t) * (1 - t)) * 0.6f;
                    im.color = Color.Lerp(c, Color.white, t).A(1 - t);
                    rt.localScale = Vector3.one * (1 - t * 0.7f);
                }, Tw.Linear, 0, () => { if (rt) UnityEngine.Object.Destroy(rt.gameObject); });
            }
        }

        // 빛살 — 바퀴살 22개가 가운데서 뻗고 바깥으로 옅어진다(한 번 굽는다)
        static Texture2D rays;
        static Texture2D RaysTex()
        {
            if (rays) return rays;
            const int N = 512;
            rays = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "reveal_rays" };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1;
                    float r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                    float spoke = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 11)), 8) * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(ang * 3 + 1)));
                    float fall = Mathf.Clamp01(1 - r) * Mathf.Clamp01(1 - r) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((r - 0.04f) / 0.12f));
                    float a = Mathf.Clamp01(spoke * fall * 1.6f + 0.35f * Mathf.Pow(Mathf.Clamp01(1 - r * 1.6f), 3));
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            rays.SetPixels32(px); rays.Apply(false, true);
            return rays;
        }

        sealed class Spin : MonoBehaviour
        {
            public float Speed;
            void Update() { if (!Settings.ReduceMotion) transform.Rotate(0, 0, Speed * Time.unscaledDeltaTime); }
        }
    }
}
