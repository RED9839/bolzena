using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 카드를 얻으면 화면 가운데에 크게(2026-10-06 사용자: 「카드를 얻었을 때는 화면 중앙에 얻은 카드 띄워 줘」)
    //   빛과 함께 살짝 커졌다 자리 잡고 → 누르거나 잠시 뒤 오른쪽 위 덱 아이콘으로 날아가며 사라지고 덱 수가 오른다 → 주인 없는 카드(교주 · 선물 · 골칫거리)는 사도 고르기(PickOwners).
    //   모든 「덱에 카드가 들어오는 곳」이 Flow.GainCards 하나를 부른다. 얻은 것은 CardSnap(덱 + 주인 기다리는 줄) 전후 차이로 센다.
    public partial class Flow
    {
        /// <summary>머리 띠(왼쪽 위) 파티 HP 를 바꾼다(숫자 · 막대) — StatusBar 가 단다. 휴식 쉬기의 회복 연출이 쓴다.</summary>
        public Action<int, int> HudHp;

        /// <summary>덱과 주인을 기다리는 줄(NeutralWait)을 찍어 둔다 — 카드를 얻는 일 바로 앞에서.</summary>
        public List<string> CardSnap() => P.S.Deck.Concat(P.S.NeutralWait).ToList();

        /// <summary>snap 뒤로 새로 들어온 카드(덱 · 주인 기다리는 줄) — 같은 카드가 여럿이면 그 수만큼.</summary>
        public List<string> NewCards(List<string> snap)
        {
            var left = snap.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            var o = new List<string>();
            foreach (var id in CardSnap()) { if (left.TryGetValue(id, out var n) && n > 0) left[id] = n - 1; else o.Add(id); }
            return o;
        }

        /// <summary>얻은 카드를 가운데에 보이고 → 덱으로 날려 보내고 → 주인 없는 카드가 있으면 사도 고르기 → then. 카드가 없으면 사도 고르기만(기다리는 것이 있으면).
        /// title · hint 를 주면 그 글로(은총: 「은총!」 · 「다야의 고유 카드 — 덱에」), hold 가 0 이상이면 그만큼만 보여 주고 덱으로(누르면 바로).</summary>
        public void GainCards(IList<string> ids, Action then, string title = null, string hint = null, float hold = -1)
        {
            var list = (ids ?? new List<string>()).Where(x => x != null && P.Data.Card(x) != null).ToList();
            if (list.Count == 0) { PickOwners(then); return; }
            CardGain.Show(this, list, () => PickOwners(then), title, hint, hold);
        }
    }

    public static class CardGain
    {
        /// <summary>null(닫힘) · enter · show · fly. 자동 데모가 본다.</summary>
        public static string Phase { get; private set; }
        public static bool IsOpen => Phase != null;
        /// <summary>마지막으로 보인 카드 수(데모 단언).</summary>
        public static int LastCount { get; private set; }

        public static void Show(Flow f, List<string> ids, Action done, string title = null, string hint = null, float hold = -1) => f.StartCoroutine(Run(f, ids, done, title, hint, hold));

        static IEnumerator Run(Flow f, List<string> ids, Action done, string titleText, string hintText, float hold)
        {
            while (OracleReveal.IsOpen) yield return null;   // 신탁 연출이 끝난 뒤에 잇는다(겹치지 않게)
            var st = f.Stage;
            Phase = "enter"; LastCount = ids.Count;
            bool compact = Theme.Compact;
            var size = st.Size;
            int n = ids.Count;
            int perRow = Mathf.Min(n, compact ? 5 : 6);
            float cw = Mathf.Min(Theme.C(230, 150), (size.x - 120) / (perRow * 1.12f)), ch = cw * 1.4f, gap = cw * 1.12f;
            int rows = Mathf.CeilToInt(n / (float)perRow);
            if (rows > 1) { float k = Mathf.Min(1, (size.y - 220) / (rows * ch * 1.1f)); cw *= k; ch *= k; gap *= k; }

            var root = Ui.Rect("cardgain", st.ModalLayer).Fill();
            var back = root.gameObject.AddComponent<BackClose>();
            var dim = Ui.Img(root, Theme.White, new Color(0.02f, 0.02f, 0.05f, 0), "dim", true); dim.rectTransform.Fill();
            var halo = Ui.Img(root, Theme.S("soft"), Theme.Gold.A(0), "halo"); halo.rectTransform.At(0.5f, 0.5f, 0, 10, Mathf.Min(size.x, gap * perRow + cw * 2), ch * rows * 1.9f);
            var title = Ui.Title(root, titleText ?? (n > 1 ? $"카드 {n}장을 얻었습니다" : "카드를 얻었습니다"), compact ? 34 : 44, Theme.Gold, TextAlignmentOptions.Center, "title");
            title.rectTransform.At(0.5f, 0.5f, 0, rows * ch * 1.1f / 2 + (compact ? 44 : 62), 900, 60); title.Outline(0.25f, new Color(0.3f, 0.15f, 0)); title.alpha = 0;
            var hint = Ui.Text(root, hintText ?? "누르면 덱으로", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center, false, "hint");
            hint.rectTransform.At(0.5f, 0.5f, 0, -rows * ch * 1.1f / 2 - (compact ? 28 : 40), 1100, 30); hint.alpha = 0;

            var holders = new List<RectTransform>();
            for (int i = 0; i < n; i++)
            {
                int r = i / perRow, c = i % perRow, inRow = Mathf.Min(perRow, n - r * perRow);
                float x = (c - (inRow - 1) / 2f) * gap, y = ((rows - 1) / 2f - r) * ch * 1.1f;
                var h = Ui.Rect("got" + i, root).At(0.5f, 0.5f, x, y, cw, ch);
                var glow = Ui.Img(h, Theme.S("soft"), Theme.Gold.A(0.55f), "glow"); glow.rectTransform.At(0.5f, 0.5f, 0, 0, cw * 1.7f, ch * 1.5f);
                var face = W.Card(h, f, ids[i], cw);
                face.At(0.5f, 0.5f, 0, 0, cw, ch);
                var flash = Ui.Img(face, Theme.White, new Color(1, 1, 1, 0.9f), "flash"); flash.rectTransform.Fill();
                h.localScale = Vector3.one * 0.4f;
                var hg = h.Group(); hg.alpha = 0;
                float d = 0.12f + i * 0.08f;
                Tw.Run(h, 0.45f, t => { if (!h) return; hg.alpha = Mathf.Clamp01(t * 3); h.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, Tw.OutBack(t)); if (flash) flash.color = flash.color.A(0.9f * (1 - t)); }, Tw.Linear, d);
                Tw.Pulse(glow, 0.3f, 0.6f, 1.8f, i * 0.2f);
                holders.Add(h);
            }
            Sfx.Play("reveal", 0.5f);
            var skip = root.gameObject.AddComponent<Btn>(); skip.Bg = null;
            bool go = false;
            skip.OnClick = () => go = true;
            st.Hot["gain.ok"] = skip;
            back.Close = () => go = true;
            float t0 = 0;
            while (t0 < 0.35f) { if (!root) { Phase = null; yield break; } t0 += Time.unscaledDeltaTime; dim.color = dim.color.A(0.72f * t0 / 0.35f); halo.color = halo.color.A(0.35f * t0 / 0.35f); title.alpha = t0 / 0.35f; yield return null; }
            yield return new WaitForSecondsRealtime(0.12f + n * 0.08f + 0.3f);
            Phase = "show";
            hint.alpha = 1;
            float wait = 0, auto = hold >= 0 ? (Settings.ReduceMotion ? Mathf.Min(hold, 0.6f) : hold) : Settings.ReduceMotion ? 0.9f : 1.8f + 0.15f * n;
            while (!go && wait < auto && root) { wait += Time.unscaledDeltaTime; yield return null; }
            if (!root) { Phase = null; yield break; }

            // 덱 아이콘으로 — 덱 수가 한 장씩 오른다
            Phase = "fly";
            var deckBtn = st.Hot.TryGetValue("deck", out var db) && db != null ? db : null;
            var deckTxt = deckBtn != null ? deckBtn.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => int.TryParse(x.text, out _)) : null;
            Vector3 to = deckBtn != null ? deckBtn.transform.position : new Vector3(Screen.width - 160, Screen.height - 40, 0);
            int shown = deckTxt != null && int.TryParse(deckTxt.text, out var dn) ? dn : P(f).S.Deck.Count;
            int target = P(f).S.Deck.Count + P(f).S.NeutralWait.Count;
            int step = Mathf.Max(0, target - shown);
            for (int i = 0; i < holders.Count; i++)
            {
                var h = holders[i];
                if (!h) continue;
                Vector3 from = h.position; int k = i;
                Tw.Run(h, 0.45f, t =>
                {
                    if (!h) return;
                    var mid = (from + to) / 2 + new Vector3(0, 120 * st.Root.lossyScale.y, 0);
                    h.position = Vector3.Lerp(Vector3.Lerp(from, mid, t), Vector3.Lerp(mid, to, t), t);
                    h.localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, t);
                    h.localEulerAngles = new Vector3(0, 0, -12 * t);
                }, Tw.InOut, i * 0.07f, () =>
                {
                    if (h) UnityEngine.Object.Destroy(h.gameObject);
                    if (deckTxt && step > 0) { int v = shown + Mathf.Min(step, Mathf.CeilToInt(step * (k + 1f) / holders.Count)); deckTxt.text = v.ToString(); var tr = deckBtn.transform; Tw.Run(tr, 0.2f, q => { if (tr) tr.localScale = Vector3.one * (1 + 0.18f * Mathf.Sin(q * Mathf.PI)); }); }
                    Sfx.Play("step", 0.5f);
                });
            }
            var g = root.Group();
            float fl = 0.45f + holders.Count * 0.07f + 0.1f;
            float tt = 0;
            while (tt < fl && root) { tt += Time.unscaledDeltaTime; dim.color = dim.color.A(0.72f * (1 - tt / fl)); halo.color = halo.color.A(0.35f * (1 - tt / fl)); title.alpha = hint.alpha = 1 - tt / fl; yield return null; }
            if (root) UnityEngine.Object.Destroy(root.gameObject);
            st.Hot.Remove("gain.ok");
            Phase = null;
            done?.Invoke();
        }

        static RunPort P(Flow f) => f.P;
    }
}
