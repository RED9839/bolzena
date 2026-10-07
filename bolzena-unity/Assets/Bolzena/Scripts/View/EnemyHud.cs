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
    //   ▭▭▭▭ 강인도 막대(칸 = 최대 강인도, 1/3 칸씩 줄어든다 — 겨누면 깎일 몫이 빛난다)
    //   (약점 성격 아이콘)  [다음 수]  [상태 칩 …]
    // 이름 · ⓘ 는 올렸을 때(또는 겨눌 때)만 막대 위에 떠오른다. 폰에서는 머리 위 묶음이 1.25배.
    public class EnemyHud : MonoBehaviour
    {
        UnitView unit;
        float barW;
        Transform hud;
        SpriteRenderer fill, lag, ghost, shieldIcon, intentIcon, intentGlow, intentGem, intentRim, info, gem, gemRim;
        TextMeshPro gemText;
        TextMeshPro hpText, shieldText, intentText, nameText, pvText;
        TextMeshPro ultText;   // 보스 고학년 이름(의도 칸 아래 — 예고 · 사용 중일 때만)
        ToughBar tough;
        readonly List<(string nat, SpriteRenderer glow, SpriteRenderer ring, Transform root)> weak = new List<(string, SpriteRenderer, SpriteRenderer, Transform)>();
        string aimNat;
        bool aimTag;
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
        public static readonly bool OldLayout = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldlayout") >= 0;
        // 머리 위 묶음의 「최대」 상자(막대 가운데 기준 · 배율 1) — 이름 · 미리보기 줄(위) ~ 마름모(왼쪽) ~ 방패(오른쪽) ~ 의도 · 상태 칩 두 줄(아래).
        //   칩이 비어 있어도 이 크기로 자리를 잡아 칩이 생겨도 묶음이 흔들리지 않는다
        public const float BoxL = -0.68f, BoxR = 0.38f, BoxB = -0.88f, BoxT = 0.64f, HeadGap = 0.1f;
        /// <summary>그림 꼭대기 바로 위(여백 HeadGap)에 묶음 아래 끝이 오는 막대 높이(유닛 기준).</summary>
        public static float BaseBarY(UnitView u, float k) => u.Visible.yMax + HeadGap - BoxB * k;
        /// <summary>이 묶음의 최대 상자(유닛 기준, 지금 막대 자리 · 배율).</summary>
        public Rect LocalBox
        {
            get
            {
                float k = hud.localScale.x;
                var p = hud.localPosition;
                return Rect.MinMaxRect(p.x + (-barW / 2 + BoxL) * k, p.y + BoxB * k, p.x + (barW / 2 + BoxR) * k, p.y + BoxT * k);
            }
        }
        public float BarW => barW;
        /// <summary>묶음 배율(화면 배율 Tone.K 에 곱한다) — 적이 많아 화면에 다 못 놓으면 배치가 조금 줄인다.</summary>
        public float HudMul { get; private set; } = 1f;
        public void SetHudMul(float m) { HudMul = m; hud.localScale = new Vector3(Tone.K * m, Tone.K * m, 1); }
        public Vector3 BarPos { get => hud.localPosition; set => hud.localPosition = value; }
        /// <summary>싸움터 좌표 · 지금 묶음의 아래 끝 y(피해 숫자를 그 밑에 띄운다).</summary>
        public float FieldBottom => unit.transform.localPosition.y + LocalBox.yMin;
        int layer;
        /// <summary>그리는 차례 — 앞줄 묶음이 뒷줄 묶음 위에(add 는 0 · -10 · -20 …).</summary>
        public void SetLayer(int add)
        {
            int d = add - layer;
            if (d == 0) return;
            layer = add;
            foreach (var r in hud.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += d;
            chips.Order += d;
        }
        const float BarH = 0.09f, IntentY = -0.44f, ToughY = -0.124f, ToughH = 0.066f, GemX = -0.36f, WeakY = -0.235f;
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
            // 머리 위 — 예전(-oldlayout): 스파인 메시 키 + 0.42, 화면 위 HUD 에 닿지 않게 3.0 에서 자른다(그래서 적이 많으면 묶음이 한 높이에 몰렸다).
            //   지금: 그 적 그림의 보이는 꼭대기(UnitView.Visible — 투명이 아닌 맨 위 픽셀) 바로 위에 묶음 전체(칩 두 줄까지)가 들어가게.
            //   적끼리 겹치면 EnemyLayout 이 뒷줄 묶음을 더 올리거나 옆으로 비킨다
            float y = OldLayout ? Mathf.Min(u.Height() + 0.42f, 3.0f - u.transform.localPosition.y - 0.6f * (k - 1)) : BaseBarY(u, k);
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
            // 이름이 길면 HP 숫자 앞에서 줄임표(…)로 끊는다
            h.hpText.text = es.MaxHp.ToString("N0");
            h.hpText.ForceMeshUpdate();
            float nameMax = Mathf.Max(0.5f, bw - h.hpText.preferredWidth - 0.38f);
            h.hpText.text = es.Hp.ToString("N0");
            if (h.nameText.preferredWidth > nameMax)
            {
                h.nameText.rectTransform.sizeDelta = new Vector2(nameMax, h.nameText.rectTransform.sizeDelta.y);
                h.nameText.textWrappingMode = TextWrappingModes.NoWrap;
                h.nameText.enableAutoSizing = true;   // 먼저 줄여 보고(75% 까지), 그래도 길면 끝을 자른다
                h.nameText.fontSizeMax = h.nameText.fontSize;
                h.nameText.fontSizeMin = h.nameText.fontSize * 0.75f;
                h.nameText.overflowMode = TextOverflowModes.Truncate;
                h.nameText.ForceMeshUpdate();
            }
            float nx = Mathf.Min(h.nameText.preferredWidth, nameMax) + 0.14f;
            h.info = Make.Box("info", h.nameRoot, Res.UI("ic_info"), new Vector3(nx, 0.01f, 0), new Vector2(0.2f, 0.2f), O + 8, new Color(0.9f, 0.92f, 1f));
            h.infoBtn = h.info.gameObject.AddComponent<UI.Button>();
            h.infoBtn.Size = new Vector2(0.4f, 0.4f) / Mathf.Max(0.01f, h.info.transform.localScale.x);
            h.infoBtn.OnClick = () => { if (Modal.Open == null && h.nameA > 0.5f) h.OnInfo?.Invoke(); };
            // 강인도 막대 — HP 막대 바로 밑, 칸 = 최대 강인도(카제나 배치 · 우리 그림)
            //   최대 강인도가 0 인 적(보스가 부르는 몹)은 격파되지 않는다 — 막대 · 약점 아이콘 없이 HP 막대만
            float tmax = es.ToughMaxV;
            if (tmax > 0.001f)
            {
                h.tough = ToughBar.Create(root, new Vector3(0, ToughY, 0), bw, ToughH, tmax, O + 1);
                h.tough.FxSpace = u.transform.parent;
                h.tough.Set(es.ToughV, false);
                h.tough.SetBroken(es.Broken, false);
                TipZone.Add(h.tough.transform, new Vector2(bw, 0.2f), () => h.State == null ? null : ToughTip(h.State, h.tough.Value), 3);
            }
            // 다음 수 — 마름모 하나(아이콘) + 값, 즉시 행동은 마름모 오른쪽 위 딱지(남은 장수)
            // 큰 마름모 — 즉시 행동까지 남은 장수(큰 숫자), 빛깔은 다음 수의 갈래
            h.gemRim = Make.Box("gemrim", root, Res.UI("diamond"), new Vector3(-bw / 2 + GemX, 0.04f, 0), new Vector2(0.6f, 0.6f), O + 5, new Color(1f, 0.85f, 0.55f));
            h.gem = Make.Box("gem", root, Res.UI("diamond"), new Vector3(-bw / 2 + GemX, 0.04f, 0), new Vector2(0.51f, 0.51f), O + 6, new Color(0.75f, 0.15f, 0.25f));
            h.gemText = Make.Text("gemt", root, "", new Vector3(-bw / 2 + GemX, 0.03f, 0), 0.3f, O + 7, Color.white);
            Make.Outline(h.gemText, 0.22f, Tone.Outline);
            TipZone.Add(h.gem, Vector2.one * 1.1f, () => Tip.Head("즉시 행동") + "\n" + InfoPanel.Rush(h.State, true) + "\n" + Tip.Dim(Terms.WordText("행동 카운트")), 4);
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
            h.chips.Rows = OldLayout ? 1 : 2;
            h.starRoot = Make.Node("stars", root, new Vector3(0, 0.45f, 0));
            h.starRoot.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) Make.Box("star" + i, h.starRoot, Res.UI("ic_star"), Vector3.zero, new Vector2(0.26f, 0.26f), O + 7, Color.white, Res.SpriteMat(false, 1.6f));
            // 미리보기 글 — 다음 수 줄의 오른쪽 끝
            h.pvText = Make.Text("pv", root, "", new Vector3(bw / 2, 0.46f, 0), 0.28f, O + 9, Color.white, TextAlignmentOptions.Right);
            h.pvText.rectTransform.pivot = new Vector2(1, 0.5f);
            Make.Outline(h.pvText, 0.3f, new Color(0.2f, 0, 0));
            // 약점 성격 — 큰 마름모 오른쪽 아래(카제나 배치), 우리 성격 아이콘
            // 약점 아이콘은 머리 위에 두지 않는다(사용자 2026-10-05 — 깔끔하게). 약점은 적 상세 · 마을 공개 · 편성에서 본다
            h.SetIntent(es);
            h.SetChips(es.Chips);
            h.SetName(0);
            return h;
        }

        static string IntentTip(EnemyState e)
        {
            if (e == null || e.Dead) return null;
            if (e.Broken) return Tip.Head("격파") + "\n1턴 동안 행동하지 못합니다 — 격파한 쪽 AP +1";
            var s = Tip.Head("다음 수") + "\n" + e.IntentText;
            if (!string.IsNullOrEmpty(e.IntentSay)) s += "\n" + Tip.Dim("「" + e.IntentSay + "」");
            var r = InfoPanel.Rush(e, true);
            if (!string.IsNullOrEmpty(r)) s += "\n\n" + r + "\n" + Tip.Dim(Terms.WordText("행동 카운트"));
            return s;
        }

        /// <summary>강인도 글 — 1 · 2/3 · 1 1/3 · 1/6(분모 2 · 3 · 4 · 5 · 6 가운데 맞는 것, 없으면 소수 한 자리).</summary>
        public static string Thirds(float v)
        {
            foreach (int d in new[] { 1, 2, 3, 4, 5, 6 })
            {
                int k = Mathf.RoundToInt(v * d);
                if (Mathf.Abs(v * d - k) > 0.02f) continue;
                int w = k / d, r = k % d;
                return r == 0 ? w.ToString() : (w > 0 ? w + " " : "") + r + "/" + d;
            }
            return v.ToString("0.0");
        }

        static string ToughTip(EnemyState e, float v)
        {
            float max = e.ToughMaxV;
            var s = Tip.Head("강인도") + (e.Broken ? "  <color=#ffd65a>격파</color>" : $"  {Thirds(v)} / {Thirds(max)}") + "\n" + Terms.WordText("강인도");
            if (e.Weak != null && e.Weak.Count > 0) s += "\n" + Tip.Dim($"약점({string.Join(" · ", e.Weak)}) 사도의 카드는 비용만큼, 아니면 그 1/3만큼 깎습니다");
            return s;
        }

        public static string WeakTip(List<string> weak) =>
            Tip.Head("약점 속성") + "  " + string.Join(" · ", weak) + "\n이 성격 사도의 카드는 강인도를 비용만큼 깎는다(아니면 1/3).\n" + Tip.Dim("「공명」 사도는 늘 약점으로 칩니다");

        void BuildWeak(List<string> list)
        {
            if (list == null) return;
            var at = new Vector3(-barW / 2 + GemX + 0.3f, WeakY, 0);
            for (int i = 0; i < list.Count; i++)
            {
                var r = Make.Node("weak_" + list[i], hud, at + new Vector3(i * 0.24f, 0, 0));
                var (glow, ring) = WeakBadge(r, list[i], 0.22f, O + 5);
                weak.Add((list[i], glow, ring, r));
            }
            if (list.Count > 0)
                TipZone.Add(weak[0].root, new Vector2(0.26f * list.Count, 0.3f), () => WeakTip(list), 4, new Vector2(0.12f * (list.Count - 1), 0));
        }

        /// <summary>약점 성격 표 하나 — 남색 동그라미 + 성격 색 테 + 성격 아이콘(판 화면 RunArt). 빛(glow)은 겨눌 때 켠다.</summary>
        public static (SpriteRenderer glow, SpriteRenderer ring) WeakBadge(Transform r, string nat, float d, int order)
        {
            var col = Bolzena.RunUI.Theme.NatureCardOf(nat);
            var glow = Make.Box("glow", r, Res.UI("soft"), Vector3.zero, Vector2.one * d * 4.2f, order, new Color(col.r, col.g, col.b, 0), Res.SpriteMat(true, 2.2f));
            var ring = Make.Box("ring", r, Res.UI("circle"), Vector3.zero, Vector2.one * d, order + 1, Color.Lerp(col, Color.white, 0.15f));
            Make.Box("disc", r, Res.UI("circle"), Vector3.zero, Vector2.one * d * 0.84f, order + 2, Tone.Well);
            var ic = Bolzena.RunUI.Theme.Icon("성격_" + nat);
            if (ic != null) Make.Box("ic", r, ic, Vector3.zero, Vector2.one * d * 0.74f, order + 3);
            else Make.Box("dot", r, Res.UI("circle"), Vector3.zero, Vector2.one * d * 0.42f, order + 3, col);
            return (glow, ring);
        }

        // 약점으로 칠 때 빛낼 아이콘 — 겨눈 사도 성격이 그 아이콘이면 그것만, 그 밖(공명 · 「약점 공격」 · 표식)은 전부
        bool WeakOn(string nat) => aimTag && (aimNat == null || aimNat == nat || State == null || !State.Weak.Contains(aimNat));

        /// <summary>겨눈 카드 · 고학년이 이 적에게 약점 공격이면(weak — core WeakFor · 「약점 공격」) 약점 아이콘이 빛난다. nature = 겨눈 사도 성격.</summary>
        public void SetAim(string nature, bool isWeak)
        {
            bool live = !hidden && State != null && !State.Dead;
            aimNat = live && isWeak ? nature : null;
            aimTag = live && isWeak;
            foreach (var w in weak)
            {
                bool me = WeakOn(w.nat);
                w.root.localScale = Vector3.one * (me ? 1.3f : 1f);
                if (!me) { var c = w.glow.color; c.a = 0; w.glow.color = c; }
                w.ring.color = me ? Color.white : Color.Lerp(Bolzena.RunUI.Theme.NatureCardOf(w.nat), Color.white, 0.15f);
            }
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

        /// <summary>방패가 깨지거나 사라질 때 — 푸른 조각이 튄다(실드가 막다 다 깨졌을 때 · unguard).</summary>
        public void ShieldBreak()
        {
            if (hidden || shieldIcon == null) return;
            var at = transform.parent.InverseTransformPoint(shieldIcon.transform.position);
            Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Fragment_01", Count = 9, Speed = new Vector2(1.5f, 3.5f), Life = new Vector2(0.3f, 0.5f),
                Size = new Vector2(0.05f, 0.11f), C0 = new Color(0.55f, 0.8f, 1f), C1 = Color.white, Gravity = 1f, Spin = true, Order = O + 9, Boost = 2f,
            });
            Vfx.Glow(at, 0.6f, new Color(0.5f, 0.75f, 1f, 0.9f), 0.25f, 3f, null, O + 8);
        }

        public void SetBlock(int b)
        {
            if (b <= 0 && shieldIcon != null && shieldIcon.enabled && isActiveAndEnabled) ShieldBreak();
            bool on = b > 0;
            shieldIcon.enabled = on;
            shieldText.enabled = on;
            shieldText.text = b.ToString();
        }

        // 강인도 — 남은 값 그대로(1/3 칸도). 깎인 자리에서 보랏빛 조각이 튀고, 칸이 통째로 비면 더 크게
        public void SetTough(float left, bool fx)
        {
            if (tough == null) return;
            float was = tough.Value;
            tough.Set(left, fx);
            // 차오름(버티기 brace · 격파에서 일어섬) — 강인도는 저절로 차지 않으니 오를 때마다 찬 칸이 빛나 눈에 띄게
            if (left > was + 0.001f && !hidden)
            {
                int lo = Mathf.Max(0, Mathf.FloorToInt(was + 0.001f)), top = Mathf.Min(tough.Cells, Mathf.CeilToInt(left - 0.001f));
                for (int i = lo; i < top; i++)
                    Vfx.Glow(transform.parent.InverseTransformPoint(tough.CellPos(i, 0.5f)), 0.55f, new Color(0.85f, 0.78f, 1f, 0.85f), 0.35f, 2.4f, null, O + 8);
                return;
            }
            if (!fx || left >= was - 0.001f) return;
            int hi = Mathf.Min(tough.Cells, Mathf.CeilToInt(was - 0.001f));
            for (int i = Mathf.Max(0, Mathf.FloorToInt(left + 0.001f)); i < hi; i++)
            {
                bool emptied = left <= i + 0.001f;
                var at = transform.parent.InverseTransformPoint(tough.CellPos(i, emptied ? 0.5f : Mathf.Clamp01(left - i)));
                Vfx.Burst(at, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Fragment_01", Count = emptied ? 7 : 3, Speed = new Vector2(1.2f, 3f), Life = new Vector2(0.25f, 0.45f),
                    Size = new Vector2(0.05f, 0.1f), C0 = ToughBar.Lilac, C1 = Color.white, Gravity = 0.8f, Spin = true, Order = O + 9, Boost = 2f,
                });
                if (emptied) Vfx.Glow(at, 0.5f, new Color(0.85f, 0.75f, 1f, 0.9f), 0.25f, 3f, null, O + 8);
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
            // 보스 클론 고학년 — 의도 칸에 그 사도 얼굴 · 고학년 이름, 붉게(맥동은 Update), 피해 숫자 크게
            if (es.UltName != null)
            {
                var face = CardView.Face(Look.Hero(es.UltHero).Art, 1f);
                if (face != null) { intentIcon.sprite = face; Make.Fit(intentIcon, new Vector2(0.27f, 0.27f)); }
                intentText.text = es.IntentValue > 0 ? "<size=125%>" + es.IntentValue + "</size>" : "";
                intentText.color = new Color(1f, 0.42f, 0.36f);
                intentRim.color = new Color(1f, 0.2f, 0.15f);
                if (ultText == null)
                {
                    ultText = Make.Text("ult", intentRoot, "", new Vector3(-0.2f, -0.3f, 0), 0.17f, O + 8, new Color(1f, 0.8f, 0.75f), TextAlignmentOptions.Left);
                    ultText.rectTransform.pivot = new Vector2(0, 0.5f);
                    Make.Outline(ultText, 0.3f, new Color(0.3f, 0, 0));
                }
                ultText.text = "「" + es.UltName + "」" + (es.UltNow ? " 이번 차례!" : " 다음 차례");
                ultText.color = new Color(1f, 0.8f, 0.75f);
                ultText.outlineColor = new Color(0.3f, 0, 0);
                ultText.gameObject.SetActive(true);
            }
            // 엘리트 · 일반 힘 모으기 — 보스 고학년보다 한 단계 약하게: 주황 · 칼 아이콘 그대로 · 피해 숫자 조금 크게 · 아래에 쏟을 수 이름 「다음 차례」(맥동은 Update, 은은하게)
            else if (es.ChargeName != null)
            {
                if (hit && es.IntentValue > 0) { s = "<size=118%>" + es.IntentValue + "</size>" + (es.IntentHits > 1 ? "<size=70%>×" + es.IntentHits + "</size>" : ""); intentText.text = s; }
                intentText.color = new Color(1f, 0.66f, 0.3f);
                intentRim.color = new Color(1f, 0.62f, 0.22f);
                if (ultText == null)
                {
                    ultText = Make.Text("ult", intentRoot, "", new Vector3(-0.2f, -0.3f, 0), 0.17f, O + 8, new Color(1f, 0.8f, 0.75f), TextAlignmentOptions.Left);
                    ultText.rectTransform.pivot = new Vector2(0, 0.5f);
                    Make.Outline(ultText, 0.3f, new Color(0.3f, 0, 0));
                }
                ultText.text = "「" + es.ChargeName + "」" + (es.ChargeNow ? " 이번 차례!" : " 다음 차례");
                ultText.color = new Color(1f, 0.82f, 0.5f);
                ultText.outlineColor = new Color(0.3f, 0.12f, 0);
                ultText.gameObject.SetActive(true);
            }
            else if (ultText != null) ultText.gameObject.SetActive(false);
            intentText.ForceMeshUpdate();
            var cp = chips.transform.localPosition;
            float cx = -barW / 2 + GemX + 0.26f + (s.Length > 0 ? intentText.preferredWidth : 0) + 0.12f;
            chips.transform.localPosition = new Vector3(cx, cp.y, cp.z);
            if (!OldLayout)
            {
                float mw = Mathf.Max(0.6f, barW / 2 + 0.3f - cx);   // 칩은 묶음(막대 오른끝 + 방패) 안에서 — 넘치면 둘째 줄
                // 의도 칸 아래 글(고학년 · 힘 모으기 이름)이 칩 둘째 줄 자리까지 뻗으면 칩은 한 줄로(남는 칩은 「+N」) — 겹치지 않게
                int rows = 2;
                if (ultText != null && ultText.gameObject.activeSelf)
                {
                    ultText.ForceMeshUpdate();
                    float labelR = -barW / 2 + GemX + (-0.2f + ultText.preferredWidth) * 1.22f;   // 맥동으로 커지는 몫까지
                    if (labelR > cx - 0.04f) rows = 1;
                }
                bool again = Mathf.Abs(mw - chips.MaxW) > 0.01f || rows != chips.Rows;
                chips.MaxW = mw; chips.Rows = rows;
                if (again && chipsLast != null) chips.Set(chipsLast);
            }
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

        List<StatusChip> chipsLast;
        static readonly bool Stress = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-hudstress") >= 0;
        public void SetChips(List<StatusChip> list)
        {
            if (Stress)   // 점검(-hudstress) — 칩이 가장 많이 붙은 모습(버프 · 디버프 6개)으로 자리를 본다
            {
                list = new List<StatusChip>(list ?? new List<StatusChip>());
                string[] ids = { "취약", "약화", "사기", "피해 감소", "그을림", "표식" };
                for (int i = list.Count; i < 6; i++) list.Add(new StatusChip { Id = ids[i], Value = (i + 2).ToString(), Turns = 2, Kind = i % 2 == 0 ? "debuff" : "buff", Text = "점검" });
            }
            chipsLast = list;
            chips.Set(list);
        }

        // 미리보기 — 깎일 몫은 막대에서 깜빡이고, 다음 수 줄 오른쪽에 「-123」 · 「처치」 · 「격파!」
        public void SetPreview(PreviewFoe p)
        {
            if (p == null)
            {
                ghost.enabled = false;
                pvText.text = "";
                tough?.Preview(0, false);
                return;
            }
            string s = p.Max ? "<size=60%>최대 </size>" : "";
            s += p.Kill ? "<color=#ff6a5a>처치</color>" : p.Hp > 0 ? "-" + p.Hp : "";
            if (p.Guard > 0) s += $" <size=65%><color=#9fd3ff>방어 -{p.Guard}</color></size>";
            float tv = p.ToughV;
            if (p.Break) s += " <size=70%><color=#ffd65a>격파!</color></size>";   // 강인도 깎일 몫은 막대 끝 금빛으로만(피해 숫자 옆 글은 겹쳐 보였다)
            pvText.text = s;
            int cut = Mathf.Min(hp, p.Hp);
            ghost.enabled = cut > 0 && !hidden;
            if (cut > 0)
            {
                float w = Mathf.Max(0.06f, barW * cut / maxHp);
                float left = -barW / 2 + barW * Mathf.Max(0, hp - cut) / maxHp;
                ghost.size = new Vector2(w, BarH);
                ghost.transform.localPosition = new Vector3(left + w / 2, 0, 0);
            }
            tough?.Preview(p.Break && tough != null ? tough.Value : tv, p.Break);
        }

        public void SetBroken(bool b)
        {
            broken = b;
            starRoot.gameObject.SetActive(b);
            if (b) intentRoot.gameObject.SetActive(false);
            if (State != null) { State.Broken = b; Gem(State); }
            if (tough != null)
            {
                tough.SetBroken(b, b);
                if (!b) tough.Set(0, false);   // 풀리면 빈 칸에서 — 뒤따르는 SetTough(남은 값)가 차오름 빛과 함께 채운다(Recover · Show)
            }
        }

        // Hide 가 끈 그림 — Show(되살아남)가 이것만 다시 켠다(원래 꺼져 있던 막대 미리보기 · 방패 아이콘은 그대로)
        readonly List<Renderer> hiddenRs = new List<Renderer>();

        public void Hide()
        {
            if (hidden) return;
            hidden = true;
            hiddenRs.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>()) if (r.gameObject != unit.Sa.gameObject && r.name != "shadow" && r.enabled) { r.enabled = false; hiddenRs.Add(r); }
            foreach (var z in GetComponentsInChildren<TipZone>()) z.enabled = false;
            foreach (var bt in GetComponentsInChildren<UI.Button>()) bt.enabled = false;
            starRoot.gameObject.SetActive(false);
            intentRoot.gameObject.SetActive(false);
            if (tough != null) tough.enabled = false;
            foreach (var w in weak) w.root.gameObject.SetActive(false);
        }

        /// <summary>되살아남(core revive — 재 속 부활 · 가사에서 일어섬) — Hide 를 되돌리고 지금 모습(체력 · 강인도 · 의도 · 칩)으로 다시 채운다.</summary>
        public void Show(EnemyState es)
        {
            hidden = false;
            foreach (var r in hiddenRs) if (r) r.enabled = true;
            hiddenRs.Clear();
            foreach (var z in GetComponentsInChildren<TipZone>(true)) z.enabled = true;
            foreach (var bt in GetComponentsInChildren<UI.Button>(true)) bt.enabled = true;
            if (tough != null) tough.enabled = true;
            if (es == null) return;
            State = es;
            SetBroken(false);
            SetHp(es.Hp, false);
            SetBlock(es.Block);
            if (tough != null) SetTough(es.ToughV, false);
            SetIntent(es);
            SetChips(es.Chips);
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
            return lp.x > -barW / 2 - 0.7f && lp.x < barW / 2 + 0.6f && lp.y > (OldLayout ? IntentY - 0.25f : BoxB) && lp.y < 0.45f;
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
            if (State != null && State.UltName != null && intentRoot.gameObject.activeSelf)
            {
                // 보스 고학년 예고 — 의도 칸이 붉게 맥동
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                intentRoot.localScale = Vector3.one * (1.12f + 0.1f * p);
                intentGlow.color = new Color(1f, 0.15f, 0.1f, 0.45f + 0.45f * p);
            }
            else if (State != null && State.ChargeName != null && intentRoot.gameObject.activeSelf)
            {
                // 힘 모으기 — 은은한 주황 맥동(고학년보다 느리고 작게)
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.6f);
                intentRoot.localScale = Vector3.one * (1.1f + 0.05f * p);
                intentGlow.color = new Color(1f, 0.5f, 0.15f, 0.3f + 0.3f * p);
            }
            if (!hidden)
            {
                float want = Hovered() ? 1 : 0;
                if (!Mathf.Approximately(want, nameA)) SetName(Mathf.MoveTowards(Mathf.Max(0, nameA), want, Time.unscaledDeltaTime * 7f));
            }
            // 창 크기가 바뀌어 폰 · PC 를 오가면 머리 위 묶음 배율도 따라간다
            float k = Tone.K * HudMul;
            if (!Mathf.Approximately(hud.localScale.x, k)) hud.localScale = new Vector3(k, k, 1);

            // 약점 아이콘 — 겨눈 카드가 약점이면 숨쉬는 빛
            if (aimTag)
                foreach (var w in weak)
                    if (WeakOn(w.nat)) { var c = w.glow.color; c.a = 0.75f + 0.25f * Mathf.Sin(Clock.Now * 9f); w.glow.color = c; }
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
            sr.enabled = !hidden && f > 0.001f;   // 숨긴 뒤(쓰러짐)에도 막대 줄어듦 트윈이 다시 켜서, 보스 마지막 일격 화면에 소환물의 붉은 막대만 남았다
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
        public float MaxW = 99f;          // 이 넓이를 넘으면 다음 줄로(Rows 까지), 마지막 줄에서 넘치면 나머지는 「+N」 하나로(적 머리 위가 이웃과 겹치지 않게)
        public int Rows = 1;
        public int Order { get => order; set => order = value; }

        /// <summary>그 이름의 칩 자리(월드) — 없으면 null. 데모가 강화 칩 위에 손가락을 올릴 때.</summary>
        public Vector3? PosOf(string id)
        {
            var t = transform.Find("chip_" + id);
            if (t == null) return null;
            var r = t.GetComponentInChildren<SpriteRenderer>();
            return r != null ? r.bounds.center : t.position;
        }

        public static ChipRow Create(Transform parent, Vector3 pos, int order, float size)
        {
            var t = Make.Node("chips", parent, pos);
            var r = t.gameObject.AddComponent<ChipRow>();
            r.size = size;
            r.order = order;
            return r;
        }

        // 원작 상태 아이콘(iconsrc/stateicons/StateIcon_N — RunArt/State, git 밖) — 그림만 보고도 뜻이 확실히 맞는 것만(runui Docs/상태칩.md).
        //   나머지는 아래 우리 글리프 그대로
        public static readonly Dictionary<string, int> Original = new Dictionary<string, int>
        {
            ["사기"] = 16, ["약화"] = 33, ["취약"] = 35, ["피해 감소"] = 27, ["불굴"] = 28, ["반격"] = 53,
            ["결정화"] = 24, ["협공"] = 155, ["고통"] = 41, ["표식"] = 143, ["다음 턴 드로우"] = 64,
            ["그을림"] = 2, ["잔불"] = 61, ["충격"] = 3, ["충격파"] = 59, ["회피"] = 18, ["초재생"] = 43,
            ["균열"] = 6, ["둔화"] = 68,
        };

        /// <summary>칩 그림 — IconOf 의 이름("state:N" = 원작 상태 아이콘, 그 밖 = Resources/UI 글리프).</summary>
        public static Sprite IconSprite(string icon) =>
            icon == null ? null : icon.StartsWith("state:") ? Bolzena.RunUI.Theme.Art("State/StateIcon_" + icon.Substring(6)) ?? Res.UI("ic_up") : Res.UI(icon);

        public static string IconOf(string id, bool good)
        {
            if (id != null && Original.TryGetValue(id, out var n) && Bolzena.RunUI.Theme.Art("State/StateIcon_" + n) != null) return "state:" + n;
            return id == "취약" ? "ic_vuln" : good ? "ic_up" : "ic_weak";
        }

        public static string IconOf(StatusChip c)
        {
            if (c.Id != null && Original.TryGetValue(c.Id, out var n) && Bolzena.RunUI.Theme.Art("State/StateIcon_" + n) != null) return "state:" + n;
            switch (c.Id)
            {
                case "취약": return "ic_vuln";
                case "약화": case "손상": return "ic_weak";
                case "방어력": case "피해 감소": case "불굴": case "실드 유지": return "ic_shield";
                case "기절": return "ic_star";
            }
            if (c.Kind == "power") return "ic_spark";   // 강화 칩 — 켜진 강화 카드
            if (c.Kind == "key") return null;
            return c.Kind == "debuff" ? "ic_weak" : "ic_up";
        }

        public static Color KindColor(string kind) => kind == "debuff" ? new Color(1f, 0.45f, 0.45f) : kind == "key" ? new Color(1f, 0.8f, 0.42f) : kind == "power" ? new Color(0.78f, 0.62f, 1f) : new Color(0.45f, 0.9f, 0.62f);

        // 강화 칩 반짝임 — 강화가 새로 켜지거나 겹이 늘면(엔진 쪽지 powerOn 과 같은 때) 그 칩이 잠깐 빛난다
        readonly Dictionary<string, string> powerSeen = new Dictionary<string, string>();
        bool powerPrimed;

        public void Set(List<StatusChip> list)
        {
            foreach (var g in made) if (g) Destroy(g);
            made.Clear();
            var flash = new HashSet<string>();
            foreach (var c in list)
            {
                if (c.Kind != "power") continue;
                if (powerPrimed && (!powerSeen.TryGetValue(c.Id, out var v0) || v0 != c.Value)) flash.Add(c.Id);
                powerSeen[c.Id] = c.Value;
            }
            powerPrimed = true;
            float x = 0, y = 0, widest = 0;
            int idx = 0, row = 0;
            foreach (var c in list)
            {
                if (x > MaxW - size * 1.4f && idx < list.Count && row < Rows - 1 && x > 0)
                {
                    widest = Mathf.Max(widest, x);
                    row++; x = 0; y -= size * 1.12f;
                }
                if (x > MaxW - size * 1.4f && idx < list.Count - 1)
                {
                    var more = Make.Node("more", transform, new Vector3(x, y, 0));
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
                var chip = Make.Node("chip_" + c.Id, transform, new Vector3(x, y, 0));
                made.Add(chip.gameObject);
                // 남색 칩 — 갈래는 왼쪽 빛깔 띠 · 아이콘으로(배경을 빨강 · 초록으로 칠하지 않는다)
                var col = new Color(0.05f, 0.075f, 0.16f, 0.94f);
                var edge = KindColor(c.Kind);
                var icon = IconOf(c);
                string label = c.Kind == "power"
                    ? $"<size=80%>{c.Id}</size>" + (string.IsNullOrEmpty(c.Value) ? "" : $" ×{c.Value}")   // 강화 칩 — 카드 이름 · 겹
                    : (icon == null ? $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Color.white, KindColor(c.Kind), 0.5f))}>{c.Id.Substring(0, 1)}</color> " : "") + c.Value + (c.Turns > 0 ? $"<size=70%><color=#9aa6c8>·{c.Turns}</color></size>" : "");
                var t = Make.Text("v", chip, label, Vector3.zero, size * 0.62f, order + 2, Color.white, TextAlignmentOptions.Left);
                Make.Outline(t, 0.3f, Color.black);
                t.ForceMeshUpdate();
                float tw = t.preferredWidth;
                float iconW = icon != null ? size : 0;
                float w = iconW + tw + size * 0.35f;
                Make.Sliced("bg", chip, Res.UI("bar_fill_9s"), new Vector3(w / 2, 0, 0), new Vector2(w, size * 1.02f), order, col);
                Make.Box("edge", chip, Res.UI("white"), new Vector3(0.02f, 0, 0), new Vector2(0.03f, size * 0.7f), order + 1, edge);
                if (icon != null) Make.Box("i", chip, IconSprite(icon), new Vector3(size * 0.55f, 0, 0), new Vector2(size * 0.9f, size * 0.9f), order + 1);
                t.rectTransform.pivot = new Vector2(0, 0.5f);
                t.transform.localPosition = new Vector3(iconW + size * 0.15f, -0.005f, 0);
                var text = Tone.StripDiff(c.Text);
                var id = c.Id;
                if (flash.Contains(c.Id))
                {
                    var glow = Make.Sliced("glow", chip, Res.UI("bar_fill_9s"), new Vector3(w / 2, 0, 0), new Vector2(w + 0.12f, size * 1.3f), order + 3, new Color(0.85f, 0.7f, 1f, 0.9f));
                    var chipT = chip; float w0 = w;
                    Clock.Run(Clock.Tween(0.7f, k =>
                    {
                        if (glow == null || chipT == null) return;
                        glow.color = new Color(0.85f, 0.7f, 1f, 0.9f * (1 - k));
                        float sc = 1 + 0.25f * Mathf.Sin(k * Mathf.PI);
                        chipT.localScale = new Vector3(sc, sc, 1);
                    }, true));
                }
                var val = c.Value;   // 머리 = 이름 · 겹, 몸 = 수치 · 지속이 다 든 한 가지 글(「자세히」 없음)
                TipZone.Add(chip, new Vector2(w, size * 1.1f), () => Tip.Head(id) + (string.IsNullOrEmpty(val) ? "" : "  " + val) + "\n" + text, 6, new Vector2(w / 2, 0));
                x += w + 0.05f;
            }
            Width = Mathf.Max(widest, x);
        }
    }
}
