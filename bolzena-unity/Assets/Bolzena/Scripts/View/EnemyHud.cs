using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using TMPro;
using UnityEngine;

namespace Bolzena.View
{
    // 적 머리 위(카제나식 배치 · 우리 그림) — 붐비지 않게 셋째 줄까지:
    //   [다음 수 마름모(아이콘) + 값  ·  즉시 행동 딱지]                 [미리보기 -123]
    //   ━━━━━━━━ HP 막대(숫자를 얹는다) ━━━━━━━━ [방어]
    //   ◆◆◆◆ 강인도  [상태 칩 …]
    // 이름 · ⓘ 는 올렸을 때(또는 겨눌 때)만 막대 위에 떠오른다. 폰에서는 머리 위 묶음이 1.25배.
    public class EnemyHud : MonoBehaviour
    {
        UnitView unit;
        float barW;
        Transform hud;
        SpriteRenderer fill, lag, ghost, shieldIcon, intentIcon, intentGlow, intentGem, intentRim, info, gem, gemRim;
        TextMeshPro gemText;
        TextMeshPro hpText, shieldText, intentText, nameText, pvText;
        readonly List<SpriteRenderer> pips = new List<SpriteRenderer>();
        Transform intentRoot, starRoot, nameRoot;
        UI.Button infoBtn;
        ChipRow chips;
        int hp, maxHp;
        float lagFrac = 1, fillFrac = 1, lagHold, nameA = -1;
        bool broken, hidden;
        public int Index;
        public EnemyState State;            // 툴팁이 읽는 지금 모습(감독이 갈아 넣는다)
        public System.Action OnInfo;
        public Vector3 IntentPos => intentRoot.position;
        public Vector3 ChipPos => chips.transform.position + new Vector3(0.15f, 0, 0) * Tone.K;
        public bool HasChips => chips.Width > 0;
        const int O = 430;
        const float BarH = 0.09f, IntentY = -0.44f, PipY = -0.15f, GemX = -0.36f;
        float pipW;
        static readonly Color HpRed = new Color(0.9f, 0.27f, 0.34f);

        public static EnemyHud Attach(UnitView u, EnemyState es, int index)
        {
            var h = u.gameObject.AddComponent<EnemyHud>();
            h.unit = u;
            h.Index = index;
            h.State = es;
            h.maxHp = h.hp = es.MaxHp;
            h.barW = es.Boss ? 2.4f : 1.6f;
            float k = Tone.K;
            // 머리 위 — 화면 위 HUD(오른쪽 위 단추 · 가운데 턴)에 닿지 않게
            float y = Mathf.Min(u.Height() + 0.42f, 3.0f - u.transform.localPosition.y - 0.6f * (k - 1));
            h.hud = Make.Node("hud", u.transform, new Vector3(0, y, 0));
            h.hud.localScale = new Vector3(k, k, 1);
            var root = h.hud;
            float bw = h.barW;
            // 막대 — 숫자를 얹을 만큼만 굵게, 남색 바탕
            Make.Sliced("barbg", root, Res.UI("bar_bg_9s"), Vector3.zero, new Vector2(bw + 0.06f, BarH + 0.06f), O);
            h.lag = Make.Sliced("lag", root, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(bw, BarH), O + 1, new Color(1f, 0.88f, 0.76f));
            h.fill = Make.Sliced("fill", root, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(bw, BarH), O + 2, HpRed);
            h.ghost = Make.Sliced("ghost", root, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(0.1f, BarH), O + 3, new Color(1f, 0.95f, 0.6f, 0.9f));
            h.ghost.enabled = false;
            h.hpText = Make.Text("hp", root, es.Hp.ToString("N0"), new Vector3(bw / 2, 0.17f, 0), 0.19f, O + 4, new Color(0.62f, 1f, 0.82f), TextAlignmentOptions.Right);
            h.hpText.rectTransform.pivot = new Vector2(1, 0.5f);
            Make.Outline(h.hpText, 0.32f, Tone.Outline);
            TipZone.Add(h.hpText, new Vector2(bw, 0.4f), () => h.State == null ? null : Tip.Head(h.State.Name) + $"  {h.State.Hp:N0} / {h.State.MaxHp:N0}" +
                (h.State.Block > 0 ? Tip.Dim($"  ·  방어 {h.State.Block}") : "") + "\n" + Tip.Dim("누르거나 오른쪽 클릭 · 길게 누르기 — 적 정보"), 3);
            // 방어 — 막대 오른쪽 끝 방패
            h.shieldIcon = Make.Box("shield", root, Res.UI("ic_shield"), new Vector3(bw / 2 + 0.18f, 0.01f, 0), new Vector2(0.36f, 0.36f), O + 5);
            h.shieldText = Make.Text("shieldv", root, "", new Vector3(bw / 2 + 0.18f, 0, 0), 0.14f, O + 6, Color.white);
            Make.Outline(h.shieldText, 0.35f, new Color(0, 0.1f, 0.3f));
            h.SetBlock(es.Block);
            // 이름 · ⓘ — 올렸을 때만(막대 위)
            h.nameRoot = Make.Node("name", root, new Vector3(-bw / 2, 0.19f, 0));
            h.nameText = Make.Text("t", h.nameRoot, es.Name, Vector3.zero, Tone.Sm, O + 8, Tone.Ink, TextAlignmentOptions.Left);
            h.nameText.rectTransform.pivot = new Vector2(0, 0.5f);
            Make.Outline(h.nameText, 0.3f, Tone.Outline);
            h.nameText.ForceMeshUpdate();
            float nx = h.nameText.preferredWidth + 0.14f;
            h.info = Make.Box("info", h.nameRoot, Res.UI("ic_info"), new Vector3(nx, 0.01f, 0), new Vector2(0.2f, 0.2f), O + 8, new Color(0.9f, 0.92f, 1f));
            h.infoBtn = h.info.gameObject.AddComponent<UI.Button>();
            h.infoBtn.Size = new Vector2(0.4f, 0.4f) / Mathf.Max(0.01f, h.info.transform.localScale.x);
            h.infoBtn.OnClick = () => { if (Modal.Open == null && h.nameA > 0.5f) h.OnInfo?.Invoke(); };
            // 강인도 칸 — 막대 밑 작은 마름모
            h.pipW = 0.15f;
            var pipRoot = Make.Node("pips", root, new Vector3(-bw / 2 + 0.07f, PipY, 0));
            for (int i = 0; i < es.MaxTough; i++)
                h.pips.Add(Make.Box("pip" + i, pipRoot, Res.UI("pip_on"), new Vector3(i * h.pipW, 0, 0), new Vector2(0.15f, 0.15f), O + 3));
            TipZone.Add(pipRoot, new Vector2(Mathf.Max(0.4f, es.MaxTough * h.pipW), 0.24f), () =>
                Tip.Head("강인도") + $"  {h.State.Tough} / {h.State.MaxTough}\n" + Terms.Words["강인도"], 3, new Vector2((es.MaxTough - 1) * h.pipW / 2, 0));
            // 다음 수 — 마름모 하나(아이콘) + 값, 즉시 행동은 마름모 오른쪽 위 딱지(남은 장수)
            // 큰 마름모 — 즉시 행동까지 남은 장수(큰 숫자), 빛깔은 다음 수의 갈래
            h.gemRim = Make.Box("gemrim", root, Res.UI("diamond"), new Vector3(-bw / 2 + GemX, 0.04f, 0), new Vector2(0.6f, 0.6f), O + 5, new Color(1f, 0.85f, 0.55f));
            h.gem = Make.Box("gem", root, Res.UI("diamond"), new Vector3(-bw / 2 + GemX, 0.04f, 0), new Vector2(0.51f, 0.51f), O + 6, new Color(0.75f, 0.15f, 0.25f));
            h.gemText = Make.Text("gemt", root, "", new Vector3(-bw / 2 + GemX, 0.03f, 0), 0.3f, O + 7, Color.white);
            Make.Outline(h.gemText, 0.22f, Tone.Outline);
            TipZone.Add(h.gem, Vector2.one * 1.1f, () => Tip.Head("즉시 행동") + "\n" + InfoPanel.Rush(h.State, true) + "\n" + Tip.Dim(Terms.Words["즉시 행동"]), 4);
            h.intentRoot = Make.Node("intent", root, new Vector3(-bw / 2 + GemX, IntentY, 0));
            h.intentGlow = Make.Box("glow", h.intentRoot, Res.UI("soft"), Vector3.zero, new Vector2(1.1f, 1.1f), O + 5, new Color(1, 0.3f, 0.2f, 0.0f), Res.SpriteMat(true, 1.5f));
            h.intentRim = Make.Box("rim", h.intentRoot, Res.UI("diamond"), Vector3.zero, new Vector2(0.36f, 0.36f), O + 6, new Color(0.95f, 0.45f, 0.45f));
            h.intentGem = Make.Box("gem", h.intentRoot, Res.UI("diamond"), Vector3.zero, new Vector2(0.29f, 0.29f), O + 7, new Color(0.06f, 0.09f, 0.2f));
            h.intentIcon = Make.Box("icon", h.intentRoot, Res.UI("ic_sword"), Vector3.zero, new Vector2(0.19f, 0.19f), O + 8);
            h.intentText = Make.Text("v", h.intentRoot, "", new Vector3(0.24f, -0.01f, 0), 0.22f, O + 8, Color.white, TextAlignmentOptions.Left);
            h.intentText.rectTransform.pivot = new Vector2(0, 0.5f);
            Make.Outline(h.intentText, 0.3f, Tone.Outline);
            TipZone.Add(h.intentRoot, new Vector2(0.9f, 0.4f), () => IntentTip(h.State), 4, new Vector2(0.25f, 0));
            // 상태 칩 — 강인도 오른쪽(같은 줄)
            h.chips = ChipRow.Create(root, new Vector3(-bw / 2 + 0.5f, IntentY, 0), O + 5, 0.24f);
            h.chips.MaxW = bw - 0.2f;
            h.starRoot = Make.Node("stars", root, new Vector3(0, 0.45f, 0));
            h.starRoot.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) Make.Box("star" + i, h.starRoot, Res.UI("ic_star"), Vector3.zero, new Vector2(0.26f, 0.26f), O + 7, Color.white, Res.SpriteMat(false, 1.6f));
            // 미리보기 글 — 다음 수 줄의 오른쪽 끝
            h.pvText = Make.Text("pv", root, "", new Vector3(bw / 2, 0.46f, 0), 0.28f, O + 9, Color.white, TextAlignmentOptions.Right);
            h.pvText.rectTransform.pivot = new Vector2(1, 0.5f);
            Make.Outline(h.pvText, 0.3f, new Color(0.2f, 0, 0));
            h.SetIntent(es);
            h.SetChips(es.Chips);
            h.SetName(0);
            return h;
        }

        static string IntentTip(EnemyState e)
        {
            if (e == null || e.Dead) return null;
            if (e.Broken) return Tip.Head("격파") + "\n이번 차례는 쉰다 — 받는 피해가 늘어난다";
            var s = Tip.Head("다음 수") + "\n" + e.IntentText;
            if (!string.IsNullOrEmpty(e.IntentSay)) s += "\n" + Tip.Dim("「" + e.IntentSay + "」");
            var r = InfoPanel.Rush(e, true);
            if (!string.IsNullOrEmpty(r)) s += "\n\n" + r + "\n" + Tip.Dim(Terms.Words["즉시 행동"]);
            return s;
        }

        public void SetHp(int v, bool hit = true)
        {
            hp = Mathf.Max(0, v);
            fillFrac = maxHp > 0 ? (float)hp / maxHp : 0;
            hpText.text = hp.ToString("N0");
            lagHold = 0.35f;
            if (hit && isActiveAndEnabled) StartCoroutine(BarFlash());
        }

        IEnumerator BarFlash()
        {
            fill.color = Color.white;
            yield return Clock.Wait(0.05f);
            fill.color = HpRed;
        }

        public void SetBlock(int b)
        {
            bool on = b > 0;
            shieldIcon.enabled = on;
            shieldText.enabled = on;
            shieldText.text = b.ToString();
        }

        // 강인도 — 남은 칸 수. 깨지는 칸에서 조각이 튄다
        public void SetTough(int left, bool fx)
        {
            for (int i = 0; i < pips.Count; i++)
            {
                bool on = i < left;
                var p = pips[i];
                bool was = p.sprite == Res.UI("pip_on");
                p.sprite = Res.UI(on ? "pip_on" : "pip_off");
                if (was && !on && fx)
                {
                    var at = transform.parent.InverseTransformPoint(p.transform.position);
                    Vfx.Burst(at, new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Fragment_01", Count = 7, Speed = new Vector2(1.5f, 3.5f), Life = new Vector2(0.25f, 0.45f),
                        Size = new Vector2(0.06f, 0.12f), C0 = new Color(1f, 0.85f, 0.3f), C1 = Color.white, Gravity = 0.8f, Spin = true, Order = O + 9, Boost = 2f,
                    });
                    Vfx.Glow(at, 0.6f, new Color(1f, 0.8f, 0.3f, 0.9f), 0.25f, 3f, null, O + 8);
                }
            }
        }

        public void SetIntent(EnemyState es)
        {
            State = es;
            Gem(es);
            var k = es.Intent;
            bool show = k != IntentKind.None && !broken && !es.Dead;
            intentRoot.gameObject.SetActive(show);
            if (!show) return;
            string icon = k == IntentKind.Defend ? "ic_shield" : k == IntentKind.Buff ? "ic_up" : k == IntentKind.Debuff ? "ic_weak" : "ic_sword";
            intentIcon.sprite = Res.UI(icon);
            Make.Fit(intentIcon, new Vector2(0.24f, 0.24f));
            bool hit = k == IntentKind.Attack || k == IntentKind.Heavy;
            string s = hit || k == IntentKind.Defend ? es.IntentValue.ToString() : "";
            if (hit && es.IntentHits > 1) s += "<size=70%>×" + es.IntentHits + "</size>";
            intentText.text = s;
            intentText.color = k == IntentKind.Defend ? new Color(0.7f, 0.88f, 1f) : k == IntentKind.Heavy ? new Color(1f, 0.62f, 0.42f) : Color.white;
            intentRim.color = k == IntentKind.Heavy ? new Color(1f, 0.5f, 0.25f) : hit ? new Color(0.95f, 0.42f, 0.45f)
                : k == IntentKind.Defend ? new Color(0.45f, 0.7f, 1f) : new Color(0.72f, 0.55f, 1f);
            intentGlow.color = k == IntentKind.Heavy ? new Color(1f, 0.25f, 0.15f, 0.7f) : new Color(1, 0.3f, 0.2f, 0f);
            intentRoot.localScale = Vector3.one * (k == IntentKind.Heavy ? 1.12f : 1f);
            intentText.ForceMeshUpdate();
            var cp = chips.transform.localPosition;
            chips.transform.localPosition = new Vector3(-barW / 2 + GemX + 0.26f + (s.Length > 0 ? intentText.preferredWidth : 0) + 0.12f, cp.y, cp.z);
        }

        // 큰 마름모 — 즉시 행동까지 남은 장수 · 다음 수의 빛깔(공격 붉게 · 큰 수 주황 · 막기 푸르게 · 그 밖 보라), 1 이면 테가 붉게
        void Gem(EnemyState es)
        {
            int left = es.RushNeed - es.RushCnt;
            gemText.text = es.Dead ? "" : es.Broken ? "<size=55%>격파</size>" : es.Sealed ? "-" : es.RushNeed <= 0 ? "<size=80%>∞</size>" : es.RushedTurn ? "-" : left.ToString();
            var k = es.Intent;
            gem.color = es.Broken ? new Color(0.45f, 0.42f, 0.5f) : k == IntentKind.Heavy ? new Color(0.85f, 0.38f, 0.12f) : k == IntentKind.Attack ? new Color(0.7f, 0.14f, 0.28f)
                : k == IntentKind.Defend ? new Color(0.2f, 0.4f, 0.78f) : new Color(0.46f, 0.25f, 0.7f);
            gemRim.color = left == 1 && !es.RushedTurn && es.RushNeed > 0 ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.86f, 0.62f);
        }

        public void SetChips(List<StatusChip> list) => chips.Set(list);

        // 미리보기 — 깎일 몫은 막대에서 깜빡이고, 다음 수 줄 오른쪽에 「-123」 · 「처치」 · 「격파!」
        public void SetPreview(PreviewFoe p)
        {
            if (p == null)
            {
                ghost.enabled = false;
                pvText.text = "";
                foreach (var pip in pips) pip.color = Color.white;
                return;
            }
            string s = p.Max ? "<size=60%>최대 </size>" : "";
            s += p.Kill ? "<color=#ff6a5a>처치</color>" : p.Hp > 0 ? "-" + p.Hp : "";
            if (p.Guard > 0) s += $" <size=65%><color=#9fd3ff>방어 -{p.Guard}</color></size>";
            if (p.Break) s += " <size=70%><color=#ffd65a>격파!</color></size>";
            else if (p.Tough > 0) s += $" <size=65%><color=#ffd65a>◆-{p.Tough}</color></size>";
            pvText.text = s;
            int cut = Mathf.Min(hp, p.Hp);
            ghost.enabled = cut > 0;
            if (cut > 0)
            {
                float w = Mathf.Max(0.06f, barW * cut / maxHp);
                float left = -barW / 2 + barW * Mathf.Max(0, hp - cut) / maxHp;
                ghost.size = new Vector2(w, BarH);
                ghost.transform.localPosition = new Vector3(left + w / 2, 0, 0);
            }
            int t0 = Mathf.Max(0, State != null ? State.Tough - p.Tough : 0);
            for (int i = 0; i < pips.Count; i++)
                pips[i].color = State != null && p.Tough > 0 && i >= t0 && i < State.Tough ? new Color(1, 1, 1, 0.5f + 0.5f * Mathf.Sin(Clock.Now * 12f)) : Color.white;
        }

        public void SetBroken(bool b)
        {
            broken = b;
            starRoot.gameObject.SetActive(b);
            if (b) intentRoot.gameObject.SetActive(false);
            if (State != null) { State.Broken = b; Gem(State); }
            if (!b) SetTough(pips.Count, false);
        }

        public void Hide()
        {
            hidden = true;
            foreach (var r in GetComponentsInChildren<Renderer>()) if (r.gameObject != unit.Sa.gameObject && r.name != "shadow") r.enabled = false;
            foreach (var z in GetComponentsInChildren<TipZone>()) z.enabled = false;
            foreach (var bt in GetComponentsInChildren<UI.Button>()) bt.enabled = false;
            starRoot.gameObject.SetActive(false);
            intentRoot.gameObject.SetActive(false);
        }

        // 이름 줄 — 올림 · 겨눔에 따라 스르르
        void SetName(float a)
        {
            nameA = a;
            nameText.alpha = a;
            info.color = new Color(0.9f, 0.92f, 1f, a);
            nameRoot.localPosition = new Vector3(-barW / 2, 0.17f + 0.04f * a, 0);
            infoBtn.enabled = a > 0.5f && !hidden;
        }

        bool Hovered()
        {
            var d = BattleDirector.I;
            if (d == null || hidden || Modal.Open != null || (State != null && State.Dead)) return false;
            if (d.Hand != null && d.Hand.Held >= 0 && d.Hand.Aim == Index) return true;
            if (d.UltSel >= 0 && d.UltAim == Index) return true;
            if (PointerInput.Touch) return false;
            var p = PointerInput.Pos;
            if (Index < d.Enemies.Count && d.EnemyBox(Index).Contains(p)) return true;
            // 머리 위 묶음 둘레(이름 줄의 ⓘ 까지)
            var lp = hud.InverseTransformPoint(p);
            return lp.x > -barW / 2 - 0.7f && lp.x < barW / 2 + 0.6f && lp.y > IntentY - 0.25f && lp.y < 0.45f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (lagHold > 0) lagHold -= dt;
            else lagFrac = Mathf.MoveTowards(lagFrac, fillFrac, dt * 1.2f);
            if (lagFrac < fillFrac) lagFrac = fillFrac;
            Bar(fill, fillFrac);
            Bar(lag, lagFrac);
            if (ghost.enabled) { var c = ghost.color; c.a = 0.55f + 0.4f * Mathf.Sin(Clock.Now * 10f); ghost.color = c; }
            if (!hidden)
            {
                float want = Hovered() ? 1 : 0;
                if (!Mathf.Approximately(want, nameA)) SetName(Mathf.MoveTowards(Mathf.Max(0, nameA), want, Time.unscaledDeltaTime * 7f));
            }
            // 창 크기가 바뀌어 폰 · PC 를 오가면 머리 위 묶음 배율도 따라간다
            float k = Tone.K;
            if (!Mathf.Approximately(hud.localScale.x, k)) hud.localScale = new Vector3(k, k, 1);

            if (broken)
            {
                for (int i = 0; i < starRoot.childCount; i++)
                {
                    float a = Clock.Now * 4f + i * Mathf.PI * 2 / 3;
                    starRoot.GetChild(i).localPosition = new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a) * 0.12f, 0);
                    starRoot.GetChild(i).localRotation = Quaternion.Euler(0, 0, Clock.Now * 200f);
                }
            }
        }

        void Bar(SpriteRenderer sr, float f)
        {
            float w = Mathf.Max(0.0001f, barW * f);
            sr.enabled = f > 0.001f;
            sr.size = new Vector2(Mathf.Max(w, 0.16f), sr.size.y);
            var p = sr.transform.localPosition;
            sr.transform.localPosition = new Vector3(-barW / 2 + Mathf.Max(w, 0.16f) / 2, p.y, p.z);
        }
    }

    // 상태 칩 한 줄 — 아이콘(없으면 글자) · 값 · 남은 턴. 칩마다 툴팁
    public class ChipRow : MonoBehaviour
    {
        float size;
        int order;
        readonly List<GameObject> made = new List<GameObject>();
        public float Width { get; private set; }
        public float MaxW = 99f;          // 이 넓이를 넘으면 나머지는 「+N」 하나로(적 머리 위가 이웃과 겹치지 않게)

        public static ChipRow Create(Transform parent, Vector3 pos, int order, float size)
        {
            var t = Make.Node("chips", parent, pos);
            var r = t.gameObject.AddComponent<ChipRow>();
            r.size = size;
            r.order = order;
            return r;
        }

        public static string IconOf(StatusChip c)
        {
            switch (c.Id)
            {
                case "취약": return "ic_vuln";
                case "약화": case "손상": return "ic_weak";
                case "방어력": case "피해 감소": case "불굴": case "실드 유지": return "ic_shield";
                case "기절": return "ic_star";
            }
            if (c.Kind == "key") return null;
            return c.Kind == "debuff" ? "ic_weak" : "ic_up";
        }

        public static Color KindColor(string kind) => kind == "debuff" ? new Color(1f, 0.45f, 0.45f) : kind == "key" ? new Color(1f, 0.8f, 0.42f) : new Color(0.45f, 0.9f, 0.62f);

        public void Set(List<StatusChip> list)
        {
            foreach (var g in made) if (g) Destroy(g);
            made.Clear();
            float x = 0;
            int idx = 0;
            foreach (var c in list)
            {
                if (x > MaxW - size * 1.4f && idx < list.Count - 1)
                {
                    var more = Make.Node("more", transform, new Vector3(x, 0, 0));
                    made.Add(more.gameObject);
                    int rest = list.Count - idx;
                    var mt = Make.Text("m", more, "+" + rest, new Vector3(0.02f, -0.005f, 0), size * 0.62f, order + 2, new Color(0.85f, 0.88f, 0.95f), TMPro.TextAlignmentOptions.Left);
                    mt.rectTransform.pivot = new Vector2(0, 0.5f);
                    Make.Outline(mt, 0.3f, Color.black);
                    var all = new List<StatusChip>(list.GetRange(idx, rest));
                    TipZone.Add(mt, new Vector2(size * 1.6f, size * 1.1f), () => { var sb = new System.Text.StringBuilder(); foreach (var cc in all) { if (sb.Length > 0) sb.Append("\n"); sb.Append(Tip.Head(cc.Id)).Append(" " + cc.Value); } return sb.ToString(); }, 6, new Vector2(size * 0.6f, 0));
                    x += size * 1.6f;
                    break;
                }
                idx++;
                var chip = Make.Node("chip_" + c.Id, transform, new Vector3(x, 0, 0));
                made.Add(chip.gameObject);
                // 남색 칩 — 갈래는 왼쪽 빛깔 띠 · 아이콘으로(배경을 빨강 · 초록으로 칠하지 않는다)
                var col = new Color(0.05f, 0.075f, 0.16f, 0.94f);
                var edge = c.Kind == "debuff" ? new Color(1f, 0.45f, 0.45f) : c.Kind == "key" ? new Color(1f, 0.8f, 0.42f) : new Color(0.45f, 0.9f, 0.62f);
                var icon = IconOf(c);
                string label = (icon == null ? $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Color.white, KindColor(c.Kind), 0.5f))}>{c.Id.Substring(0, 1)}</color> " : "") + c.Value + (c.Turns > 0 ? $"<size=70%><color=#9aa6c8>·{c.Turns}</color></size>" : "");
                var t = Make.Text("v", chip, label, Vector3.zero, size * 0.62f, order + 2, Color.white, TextAlignmentOptions.Left);
                Make.Outline(t, 0.3f, Color.black);
                t.ForceMeshUpdate();
                float tw = t.preferredWidth;
                float iconW = icon != null ? size : 0;
                float w = iconW + tw + size * 0.35f;
                Make.Sliced("bg", chip, Res.UI("bar_fill_9s"), new Vector3(w / 2, 0, 0), new Vector2(w, size * 1.02f), order, col);
                Make.Box("edge", chip, Res.UI("white"), new Vector3(0.02f, 0, 0), new Vector2(0.03f, size * 0.7f), order + 1, edge);
                if (icon != null) Make.Box("i", chip, Res.UI(icon), new Vector3(size * 0.55f, 0, 0), new Vector2(size * 0.9f, size * 0.9f), order + 1);
                t.rectTransform.pivot = new Vector2(0, 0.5f);
                t.transform.localPosition = new Vector3(iconW + size * 0.15f, -0.005f, 0);
                var text = c.Text;
                var id = c.Id;
                TipZone.Add(chip, new Vector2(w, size * 1.1f), () => Tip.Head(id) + "\n" + text, 6, new Vector2(w / 2, 0));
                x += w + 0.05f;
            }
            Width = x;
        }
    }
}
