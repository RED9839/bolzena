using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 화면 UI — 카제나 전투 화면의 배치 · 밀도를 따른다(그림 · 아이콘 · 글은 우리 것, 색 · 글자 단계는 판 화면 톤.md).
    // 판 · 상자를 거의 없애고 화면 대부분을 싸움터에 쓴다. 무리마다 화면 모서리 · 가장자리에 붙는다(21:9 · 16:10 에서도), 폰은 1.25배.
    //   위 왼쪽: 가는 파티 HP 막대(숫자 얹기 · 예고 피해 ▼) → 사도 초상 띠 셋(비스듬한 조각 · 옆에 키워드 수) → 파티 상태 칩(작게)
    //   위 오른쪽: 자동 전투(알약) · 배속(1× · 2× 알약) · 메뉴(≡ — 일시정지 · 설정 · 화면), 얇은 구분선
    //   왼쪽 가운데: 뽑을 더미(아이콘 + 수)       오른쪽 가운데: 무덤(버린 더미 — 아이콘 + 수)
    //   (턴 칩 · 웨이브 · 소멸 더미 표시 · 게이지 머리 아이콘 · 「/ 최대」 는 2026-10-07 사용자 요청으로 걷음 — 턴 · 웨이브는 로그 · 스냅샷에만, 소멸 더미는 무덤 창의 탭으로 본다)
    //   왼쪽 아래: 고학년 게이지(큰 % + 세로 칸) 옆에 고학년 띠 셋(비스듬한 초상 · 값 · 쓸 수 있으면 반짝이)
    //   아래 가운데: 손패 밑 큰 AP 숫자(빛나는 마름모) · 손패 수 「05/10」        오른쪽 아래: 턴 종료(금 테 메달 · 다음 턴 화살 · 이름표 — EndTurnButton)
    //   싸움터: 사도 머리 위 고유 효과(키워드) 작은 표시
    public class PartyHud : MonoBehaviour
    {
        SpriteRenderer hpFill, hpLag, hpGain, hpDue, dueTick, shieldIcon, speedBg, menuBg, apGem, apGlow;
        TextMeshPro hpText, shieldText, apText, handText, deckText, discText, speedText, pvText, gaugeText;
        EndTurnButton endTurn;
        /// <summary>오른쪽 아래 턴 종료 단추(EndTurnButton — 상태별 모양 · 점검).</summary>
        public EndTurnButton EndButton => endTurn;
        ChipRow chips;
        /// <summary>파티 버프 줄(강화 칩 포함).</summary>
        public ChipRow Chips => chips;
        Group TL, TR, ML, MR, BL, BR, BC;
        public System.Action<int> OnPile;           // 0 뽑을 · 1 버린 · 2 사라진
        public System.Action OnSpeed, OnPause, OnParty, OnAuto;
        SpriteRenderer autoKnob, autoTrack, autoIcon, speedIcon;
        public System.Action<int> OnHero;           // 왼쪽 위 초상을 누름 — 사도 정보
        public BattleSnapshot Snap;                 // 툴팁이 읽는 지금 모습
        SpriteRenderer gaugeFill, gaugeLead, gaugeBg;
        readonly List<(int need, SpriteRenderer line, TextMeshPro num)> ticks = new List<(int, SpriteRenderer, TextMeshPro)>();
        readonly List<Portrait> portraits = new List<Portrait>();
        public readonly List<UltButton> Ults = new List<UltButton>();
        int hp, maxHp, gauge, gaugeMax = 300, block, due, ap;
        float fillF = 1, lagF = 1, lagHold, gaugeShown;
        float apPop;
        // AP 를 얻은 순간(격파 · 격파하며 처치) — AP 숫자 오른쪽 위에 「+N」 이 떠올랐다 사라진다
        TextMeshPro apPlus; float apPlusT;

        // 자리(16:9 기준 좌표 — 무리마다 모서리를 따라 옮겨진다)
        const float BarX0 = -7.72f, BarW = 4.9f, BarY = 4.2f, BarH = 0.13f;
        const float StripY = 3.78f, StripW = 1.5f, StripDx = 1.62f;
        static readonly Vector3 DeckAt = new Vector3(-7.45f, -0.5f, 0);
        static readonly Vector3 DiscAt = new Vector3(7.45f, -0.5f, 0);
        // 고학년 — 왼쪽 열: 큰 %(실제 값) → 세로 막대(부드러운 채움 + 사도마다 필요치 눈금). 오른쪽: 사도 띠 셋
        //   (머리 아이콘 · 「/ 최대」 를 걷은 뒤 % 를 막대 바로 위로 내렸다 — 옛 자리 -1.86)
        static readonly Vector3 GaugeAt = new Vector3(-7.42f, -2.04f, 0);
        const float GBarX = -7.3f, GBarTop = -2.32f, GBarBot = -4.16f, GBarW = 0.2f;
        const float UltX = -5.86f, UltY0 = -2.62f, UltDy = 0.74f;
        static readonly Vector3 ApAt = new Vector3(0, -3.92f, 0), HandAt = new Vector3(0, -4.34f, 0);
        static readonly Vector3 EndAt = new Vector3(7.02f, -3.58f, 0);
        // 위 오른쪽 줄 — 자동 전투 · 배속 · 메뉴(2026-10-07 사용자: 배속을 옛 자동 전투 자리로, 자동 전투는 그 왼쪽)
        static readonly Vector3 AutoAt = new Vector3(5.74f, 4.05f, 0), MenuAt = new Vector3(7.6f, 4.05f, 0);
        static readonly Vector3 SpeedAt = new Vector3(6.72f, 4.05f, 0);      // 자동 전투와 같은 알약(시계 + 1× · 2×)
        const float PillW = 0.86f, PillH = 0.4f;

        public Vector3 EndPos => endTurn.transform.position;
        public Vector3 SpeedPos => speedBg.transform.position;
        public Vector3 PartyPos => TL.At(new Vector3(BarX0 + BarW / 2, BarY, 0));
        public Vector3 PausePos => menuBg.transform.position;
        public bool EndHover;
        const int O = 560, OAp = 692;

        public static PartyHud Create(Transform parent, BattleSnapshot s)
        {
            var root = Make.Node("PartyHud", parent);
            var h = root.gameObject.AddComponent<PartyHud>();
            h.Build(s);
            return h;
        }

        // 모서리 무리 — 안쪽 마디를 16:9 기준 모서리만큼 되돌려 두어, 자식은 16:9 좌표를 그대로 쓴다
        class Group
        {
            public Transform Node, Inner;
            public Vector2 Sign;
            public Group(string name, Transform parent, Vector2 sign)
            {
                Sign = sign;
                Node = Make.Node(name, parent);
                Inner = Make.Node("in", Node, new Vector3(-sign.x * 8f, -sign.y * 4.5f, 0));
            }
            public void Place(float hw, float hh, float k)
            {
                Node.localPosition = new Vector3(Sign.x * hw, Sign.y * hh, 0);
                Node.localScale = new Vector3(k, k, 1);
            }
            public Vector3 At(Vector3 p) => Inner.TransformPoint(p);
        }

        static TextMeshPro Txt(string name, Transform p, string text, Vector3 at, float size, int order, Color c, TextAlignmentOptions align = TextAlignmentOptions.Center, float outline = 0.24f)
        {
            var t = Make.Text(name, p, text, at, size, order, c, align);
            if (align == TextAlignmentOptions.Left) t.rectTransform.pivot = new Vector2(0, 0.5f);
            else if (align == TextAlignmentOptions.Right) t.rectTransform.pivot = new Vector2(1, 0.5f);
            if (outline > 0) Make.Outline(t, outline, Tone.Outline);
            return t;
        }

        void Build(BattleSnapshot s)
        {
            var t = transform;
            TL = new Group("TL", t, new Vector2(-1, 1));
            TR = new Group("TR", t, new Vector2(1, 1));
            ML = new Group("ML", t, new Vector2(-1, 0));
            MR = new Group("MR", t, new Vector2(1, 0));
            BL = new Group("BL", t, new Vector2(-1, -1));
            BR = new Group("BR", t, new Vector2(1, -1));
            BC = new Group("BC", t, new Vector2(0, -1));
            var tl = TL.Inner; var tr = TR.Inner; var ml = ML.Inner; var mr = MR.Inner; var bl = BL.Inner; var br = BR.Inner; var bc = BC.Inner;
            maxHp = hp = s.PartyMaxHp;
            gaugeMax = Mathf.Max(1, s.GaugeMax);

            // ── 위 왼쪽 — 가는 HP 막대(숫자 얹기) · 초상 띠 · 상태 칩 ──
            Make.Box("fadeTop", tl, Res.UI("hud_fade"), new Vector3(-5.4f, 3.9f, 0), new Vector2(6.0f, 1.6f), O - 6, new Color(1, 1, 1, 0.6f));
            var bcn = new Vector3(BarX0 + BarW / 2, BarY, 0);
            Make.Sliced("bg", tl, Res.UI("bar_bg_9s"), bcn, new Vector2(BarW + 0.05f, BarH + 0.05f), O);
            hpLag = Make.Sliced("lag", tl, Res.UI("bar_fill_9s"), bcn, new Vector2(BarW, BarH), O + 1, new Color(1f, 0.82f, 0.62f));
            hpFill = Make.Sliced("fill", tl, Res.UI("bar_fill_9s"), bcn, new Vector2(BarW, BarH), O + 2, new Color(0.3f, 0.78f, 0.5f));
            hpGain = Make.Sliced("gain", tl, Res.UI("bar_fill_9s"), bcn, new Vector2(0.1f, BarH), O + 3, new Color(0.78f, 1f, 0.86f, 0.85f));
            hpGain.enabled = false;
            hpDue = Make.Sliced("due", tl, Res.UI("bar_fill_9s"), bcn, new Vector2(0.1f, BarH), O + 3, new Color(1f, 0.32f, 0.36f, 0.8f));
            hpDue.enabled = false;
            dueTick = Make.Box("duetick", tl, Res.UI("diamond"), bcn, new Vector2(0.12f, 0.14f), O + 4, Tone.Bad);
            hpText = Txt("hp", tl, "", bcn + new Vector3(0, 0.005f, 0), 0.17f, O + 5, Color.white, TextAlignmentOptions.Center, 0.34f);
            shieldIcon = Make.Box("shield", tl, Res.UI("ic_shield"), new Vector3(BarX0 + BarW + 0.25f, BarY, 0), new Vector2(0.4f, 0.4f), O + 5);
            shieldText = Txt("shieldv", tl, "", shieldIcon.transform.localPosition + new Vector3(0, -0.01f, 0), 0.16f, O + 6, Color.white, TextAlignmentOptions.Center, 0.4f);
            pvText = Txt("pv", tl, "", new Vector3(BarX0 + BarW + 0.52f, BarY, 0), Tone.Sm, O + 6, Color.white, TextAlignmentOptions.Left);
            var hpZone = Make.Node("hpzone", tl, bcn);
            { var hb = hpZone.gameObject.AddComponent<Button>(); hb.Size = new Vector2(BarW + 0.4f, 0.4f); hb.OnClick = () => OnParty?.Invoke(); }
            TipZone.Add(hpZone, new Vector2(BarW + 0.4f, 0.4f), () => Snap == null ? null :
                Tip.Head("파티") + $"  {Snap.PartyHp} / {Snap.PartyMaxHp}" + (Snap.PartyBlock > 0 ? $"  ·  방어 {Snap.PartyBlock}" : "") +
                (due > 0 ? $"\n<color={Tone.BadTag}>예고 피해 {due}</color>" + Tip.Dim(" — 이번 적의 차례에 예고된 피해의 합. ▼ 는 맞은 뒤 남을 HP") : "") +
                "\n파티는 한 몸입니다 — 사도 셋이 HP · 방어 · 실드를 함께 씁니다. 방어 · 실드가 피해를 먼저 받습니다." + Tip.Dim("\n누르면 파티 효과 · 전투원 · 덱"), 2);
            SetHp(s.PartyHp, false);
            SetBlock(0);
            for (int i = 0; i < s.Heroes.Count; i++)
            {
                int ii = i;
                var p = Portrait.Create(tl, s.Heroes[i], new Vector3(BarX0 + StripW / 2 + i * StripDx, StripY, 0), O + 2);
                p.OnTap = () => OnHero?.Invoke(ii);
                portraits.Add(p);
            }
            chips = ChipRow.Create(tl, new Vector3(BarX0, 3.4f, 0), O + 3, 0.24f);

            // ── 위 오른쪽 — 자동 전투 · 배속 · 메뉴(≡), 배속과 메뉴 사이에 얇은 선 ──
            // 배속 — 자동 전투와 같은 알약 트랙: 왼쪽 시계 아이콘 + 오른쪽 지금 배속(1× · 2×). 누를 때마다 1× ↔ 2×, 2× 면 금빛(자동 켬과 같은 빛깔)
            speedBg = Make.Sliced("speed", tr, Res.UI("bar_fill_9s"), SpeedAt, new Vector2(PillW, PillH), O + 1, new Color(0.08f, 0.1f, 0.2f, 0.85f));
            speedIcon = Make.Box("speedi", tr, Res.UI("ic_clock"), SpeedAt + new Vector3(-0.2f, 0, 0), new Vector2(0.26f, 0.26f), O + 2, new Color(0.95f, 0.93f, 0.88f));
            speedText = Txt("speedt", tr, "1×", SpeedAt + new Vector3(0.14f, -0.01f, 0), Tone.Sm, O + 2, Tone.Ink, TextAlignmentOptions.Center, 0.24f);
            { var sb = speedBg.gameObject.AddComponent<Button>(); sb.Size = new Vector2(PillW + 0.04f, PillH + 0.06f); sb.OnClick = () => OnSpeed?.Invoke(); TipZone.Add(speedBg, sb.Size, () => "전투 배속 1× · 2× — 단축키 Tab", 2); }
            // 자동 전투 토글 — 알약 트랙 + 손잡이(켜면 금빛, 손잡이 위 자동 아이콘)
            autoTrack = Make.Sliced("autotrack", tr, Res.UI("bar_fill_9s"), AutoAt, new Vector2(PillW, PillH), O + 1, new Color(0.08f, 0.1f, 0.2f, 0.85f));
            autoKnob = Make.Box("autoknob", tr, Res.UI("circle"), AutoAt + new Vector3(-0.22f, 0, 0), new Vector2(0.36f, 0.36f), O + 2, new Color(0.85f, 0.87f, 0.95f));
            autoIcon = Make.Box("autoi", tr, Res.UI("ic_auto"), AutoAt + new Vector3(-0.22f, 0, 0), new Vector2(0.24f, 0.24f), O + 3, new Color(0.1f, 0.12f, 0.2f));
            { var ab = autoTrack.gameObject.AddComponent<Button>(); ab.Size = new Vector2(0.9f, 0.46f); ab.OnClick = () => OnAuto?.Invoke(); TipZone.Add(autoTrack, ab.Size, () => "자동 전투 — 카드 · 고학년을 저절로 냅니다 · 단축키 Q", 2); }
            Make.Box("sep", tr, Res.UI("white"), (SpeedAt + MenuAt) / 2 + new Vector3(0.1f, 0, 0), new Vector2(0.015f, 0.36f), O + 1, new Color(1, 1, 1, 0.3f));
            menuBg = IconBtn("menu", tr, MenuAt, "ic_menu", () => OnPause?.Invoke(), "메뉴 — 일시정지 · 설정 · 화면 · Esc");

            // ── 왼쪽 가운데 — 뽑을 더미 ──
            deckText = PileButton(ml, "deck", DeckAt, 0, "ic_pile_up", false, "뽑을 더미 — 다음에 뽑을 카드들(차례는 감춤). 누르면 봅니다 · 단축키 A");

            // ── 오른쪽 가운데 — 무덤(버린 더미) ──
            discText = PileButton(mr, "disc", DiscAt, 1, "ic_pile_down", true, "무덤 — 낸 카드 · 버린 카드. 덱이 바닥나면 섞여 돌아옵니다. 누르면 봅니다 · 단축키 S");

            // ── 왼쪽 아래 — 고학년 게이지(큰 % + 세로 칸) · 고학년 띠 셋 ──
            Make.Box("fadeBL", bl, Res.UI("hud_fade"), new Vector3(-6.4f, -3.0f, 0), new Vector2(3.6f, 3.4f), O - 6, new Color(1, 1, 1, 0.55f));
            gaugeText = Txt("gval", bl, "", GaugeAt, 0.4f, O + 3, Tone.Gold, TextAlignmentOptions.Center, 0.22f);
            // 세로 막대 — 바탕 · 부드러운 채움(아래부터) · 채움 끝 빛
            float gH = GBarTop - GBarBot, gMid = (GBarTop + GBarBot) / 2;
            gaugeBg = Make.Sliced("gbg", bl, Res.UI("bar_bg_9s"), new Vector3(GBarX, gMid, 0), new Vector2(GBarW + 0.06f, gH + 0.06f), O + 1, new Color(0.06f, 0.08f, 0.16f, 0.92f));
            gaugeFill = Make.Sliced("gfill", bl, Res.UI("bar_fill_9s"), new Vector3(GBarX, GBarBot, 0), new Vector2(GBarW, 0.05f), O + 2, new Color(0.45f, 0.8f, 1f));
            gaugeLead = Make.Box("glead", bl, Res.UI("soft"), new Vector3(GBarX, GBarBot, 0), new Vector2(0.62f, 0.2f), O + 3, new Color(0.8f, 0.95f, 1f, 0.8f), Res.SpriteMat(true, 1.6f));
            // 사도마다 필요치 눈금(같은 값은 하나로) — 막대를 가로지르는 선 + 왼쪽에 값
            var needs = new SortedSet<int>();
            foreach (var hh in s.Heroes) if (hh.UltMax > 0 && hh.UltMax <= gaugeMax) needs.Add(hh.UltMax);
            foreach (var n in needs)
            {
                float y = GBarBot + gH * n / gaugeMax;
                var ln = Make.Box("tick" + n, bl, Res.UI("white"), new Vector3(GBarX, y, 0), new Vector2(GBarW + 0.16f, 0.025f), O + 4, new Color(1, 1, 1, 0.55f));
                var nt = Txt("tickv" + n, bl, n.ToString(), new Vector3(GBarX - 0.2f, y, 0), 0.115f, O + 4, Tone.Sub, TextAlignmentOptions.Right, 0.3f);
                ticks.Add((n, ln, nt));
            }
            var gz = Make.Node("gzone", bl, new Vector3(GaugeAt.x, (GaugeAt.y + 0.25f + GBarBot) / 2, 0));
            TipZone.Add(gz, new Vector2(0.9f, GaugeAt.y + 0.3f - GBarBot), () => Tip.Head("고학년 게이지") +
                $"  {gauge}% / {gaugeMax}%\n파티가 함께 쓰는 게이지 — 카드에 쓴 AP 1 마다 +10%. 막대의 눈금은 사도마다 고학년에 드는 값, 그 선을 넘으면 그 사도 띠가 반짝입니다.", 2);
            for (int i = 0; i < s.Heroes.Count; i++)
                Ults.Add(UltButton.Create(bl, s.Heroes[i], i, new Vector3(UltX, UltY0 - i * UltDy, 0)));

            // ── 아래 가운데 — 큰 AP 숫자 · 손패 수 ──
            apGlow = Make.Box("apglow", bc, Res.UI("soft"), ApAt, new Vector2(1.8f, 1.1f), OAp, new Color(1f, 0.78f, 0.4f, 0.4f), Res.SpriteMat(true, 1.3f));
            Make.Box("apl", bc, Res.UI("band_line"), ApAt + new Vector3(0, -0.02f, 0), new Vector2(1.9f, 0.03f), OAp + 1, new Color(1f, 0.86f, 0.55f, 0.9f), Res.SpriteMat(false, 1.4f));
            Make.Box("apdl", bc, Res.UI("diamond"), ApAt + new Vector3(-0.42f, 0, 0), new Vector2(0.12f, 0.12f), OAp + 2, Tone.Gold);
            Make.Box("apdr", bc, Res.UI("diamond"), ApAt + new Vector3(0.42f, 0, 0), new Vector2(0.12f, 0.12f), OAp + 2, Tone.Gold);
            apGem = Make.Box("apgem", bc, Res.UI("diamond"), ApAt, new Vector2(0.62f, 0.62f), OAp + 2, new Color(0.07f, 0.1f, 0.22f, 0.92f));
            apText = Txt("ap", bc, "", ApAt + new Vector3(0, 0.01f, 0), 0.52f, OAp + 4, new Color(1f, 0.96f, 0.84f), TextAlignmentOptions.Center, 0.2f);
            Make.Glow(apText, new Color(1f, 0.92f, 0.7f), 1.25f);
            apPlus = Txt("ap_plus", bc, "", ApAt + new Vector3(0.62f, 0.2f, 0), 0.4f, OAp + 5, new Color(1f, 0.86f, 0.4f, 0), TextAlignmentOptions.Left, 0.25f);
            Make.Box("handi", bc, Res.UI("ic_cards"), HandAt + new Vector3(-0.42f, 0, 0), new Vector2(0.2f, 0.2f), OAp + 3, new Color(0.85f, 0.88f, 0.95f));
            handText = Txt("hand", bc, "", HandAt + new Vector3(0.08f, -0.005f, 0), Tone.Cap, OAp + 3, Tone.Sub, TextAlignmentOptions.Center, 0.3f);
            TipZone.Add(apGem, new Vector2(1.2f, 1.0f) / apGem.transform.localScale.x, () => Tip.Head("AP") + "\n카드를 내는 값 — 매 턴 3, 남으면 사라집니다.\n격파하면 AP 를 얻습니다." + Tip.Dim("\n아래 줄은 손패 수 / 최대"), 2);

            // ── 오른쪽 아래 — 턴 종료(금 테 메달 + 다음 턴 화살 + 「턴 종료」 이름표 — EndTurnButton, 점검 인자 -endturnstyle) ──
            endTurn = EndTurnButton.Create(br, EndAt, O, s.Heroes.Count > 0 ? s.Heroes[0].Nature : null, EndTurnButton.Chosen);
            TipZone.Add(endTurn.Body, new Vector2(endTurn.Half.x * 2 / endTurn.Body.transform.localScale.x, endTurn.Half.y * 2 / endTurn.Body.transform.localScale.y), () => "턴을 넘깁니다 — 손패를 버리고 적의 차례 · 단축키 E", 2);

            Anchor();
        }

        // 아이콘 단추 — 판 없이, 뒤에 옅은 그늘 원만. 올리면 금 테
        SpriteRenderer IconBtn(string name, Transform p, Vector3 at, string icon, System.Action click, string tip)
        {
            float d = 0.56f;
            var bg = Make.Box(name, p, Res.UI("soft"), at, new Vector2(d * 1.4f, d * 1.4f), O, new Color(0.02f, 0.03f, 0.08f, 0.55f));
            var hl = Make.Box(name + "_hl", p, Res.UI("btn_round"), at, new Vector2(d, d), O + 1, new Color(1, 1, 1, 0));
            if (icon != null) Make.Box(name + "_i", p, Res.UI(icon), at, new Vector2(0.34f, 0.34f), O + 2, new Color(0.95f, 0.93f, 0.88f));
            var b = bg.gameObject.AddComponent<Button>();
            b.Size = Vector2.one * (d + 0.06f) / bg.transform.localScale.x;
            b.OnClick = click;
            b.Highlight = hl;
            TipZone.Add(bg, b.Size, () => tip, 2);
            return bg;
        }

        // 더미 — 큰 카드 더미 아이콘 + 숫자(판 없이). right — 숫자를 아이콘 왼쪽에
        TextMeshPro PileButton(Transform t, string name, Vector3 at, int which, string icon, bool right, string tip, float size = 0.52f)
        {
            Make.Box(name + "_i", t, Res.UI(icon), at, new Vector2(size, size), O + 1, which == 2 ? new Color(0.78f, 0.75f, 0.88f) : new Color(0.95f, 0.93f, 0.88f));
            var txt = Txt(name, t, "", at + new Vector3((right ? -1 : 1) * (size / 2 + 0.06f), -size * 0.12f, 0), size > 0.4f ? Tone.Lg : Tone.Sm, O + 3, Tone.Ink,
                right ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, 0.26f);
            var b = txt.gameObject.AddComponent<Button>();
            b.Size = new Vector2(size + 0.5f, size + 0.1f);
            b.Offset = new Vector2((right ? 1 : -1) * (size / 2 + 0.06f - 0.05f), size * 0.12f);
            b.OnClick = () => OnPile?.Invoke(which);
            TipZone.Add(txt, b.Size, () => tip, 2, b.Offset);
            return txt;
        }

        public Rect PortraitRect(int i)
        {
            var t = portraits[Mathf.Clamp(i, 0, portraits.Count - 1)].transform;
            float k = Mathf.Abs(t.lossyScale.x);
            return new Rect(t.position.x - StripW * k / 2, t.position.y - 0.25f * k, StripW * k, 0.5f * k);
        }

        public Vector3 PilePos(int which) => which == 0 ? ML.At(DeckAt) : MR.At(DiscAt);   // 소멸(2) 표시는 걷었다 — 무덤 자리

        // 화면 가장자리에 붙이기 — 무리마다 모서리로, 폰은 1.25배. 손패는 왼쪽 고학년 띠 · 오른쪽 턴 종료 사이
        void Anchor()
        {
            float hw = Tone.HalfW, hh = Tone.HalfH, k = Tone.K;
            TL.Place(hw, hh, k); TR.Place(hw, hh, k); ML.Place(hw, hh, k); MR.Place(hw, hh, k); BL.Place(hw, hh, k); BR.Place(hw, hh, k); BC.Place(hw, hh, k);
            var hand = BattleDirector.I != null ? BattleDirector.I.Hand : null;
            if (hand != null)
            {
                float left = -hw + k * (UltX + UltButton.SW / 2 + 0.1f + 8f);
                float right = hw - k * (8f - (EndAt.x - endTurn.Half.x));
                hand.HalfSpan = Mathf.Min(-left, right) - 0.1f;
                hand.DeckPos = PilePos(0);
                hand.DiscardPos = PilePos(1);
            }
        }

        public void SetHp(int v, bool hit = true)
        {
            hp = Mathf.Max(0, v);
            fillF = maxHp > 0 ? (float)hp / maxHp : 0;
            hpText.text = $"{hp:N0}<size=75%><color=#cfd6ea> / {maxHp:N0}</color></size>";
            lagHold = 0.4f;
            if (hit) StartCoroutine(Shake());
        }

        IEnumerator Shake()
        {
            var p0 = TL.Inner.localPosition;
            yield return Clock.Tween(0.3f, t => TL.Inner.localPosition = p0 + new Vector3(Mathf.Sin(t * 60) * 0.05f * (1 - t), 0, 0));
            TL.Inner.localPosition = p0;
        }

        public void SetBlock(int b)
        {
            block = Mathf.Max(0, b);
            shieldIcon.enabled = b > 0;
            shieldText.enabled = b > 0;
            shieldText.text = b.ToString();
        }

        public void PopBlock()
        {
            StartCoroutine(Pop(shieldIcon.transform, 0.4f));
            Vfx.Glow(shieldIcon.transform.position, 1.2f, new Color(0.5f, 0.8f, 1f, 0.9f), 0.35f, 2.5f, null, O + 7, transform.parent);
        }

        IEnumerator Pop(Transform tr, float size)
        {
            var sr = tr.GetComponent<SpriteRenderer>();
            yield return Clock.Tween(0.3f, t => Make.Fit(sr, Vector2.one * size * (1 + 0.5f * (1 - Ease.OutBack(t)))));
        }

        /// <summary>AP 를 얻었다(격파 · 격파하며 처치) — 「+N」 이 떠오르고 숫자가 톡. 숫자 값은 수가 끝난 뒤 ApChanged 가 맞춘다.</summary>
        public void ApGain(int n)
        {
            if (n <= 0 || apPlus == null) return;
            apPlus.text = "+" + n;
            apPlusT = 1;
            apPop = 1;
        }

        public void SetAp(int v, int max)
        {
            if (apText.text != v.ToString()) apPop = 1;
            ap = v;
            apText.text = v.ToString();
            apText.color = v > 0 ? new Color(1f, 0.96f, 0.84f) : Tone.Dim;
        }

        public void SetPiles(int draw, int disc, int gone = 0)
        {
            deckText.text = draw.ToString();
            discText.text = disc.ToString();
        }

        public void SetSpeed(float v)
        {
            bool fast = v > 1;
            speedText.text = fast ? "2×" : "1×";
            speedText.color = fast ? new Color(0.1f, 0.12f, 0.2f) : Tone.Ink;   // 금빛 트랙 위에서는 짙게(자동 켬 손잡이 아이콘과 같은 짝)
            speedText.outlineWidth = fast ? 0f : 0.24f;
            speedIcon.color = fast ? new Color(0.1f, 0.12f, 0.2f) : new Color(0.95f, 0.93f, 0.88f);
            speedBg.color = fast ? new Color(0.85f, 0.66f, 0.28f, 0.95f) : new Color(0.08f, 0.1f, 0.2f, 0.85f);
        }

        public void SetAuto(bool on)
        {
            autoTrack.color = on ? new Color(0.85f, 0.66f, 0.28f, 0.95f) : new Color(0.08f, 0.1f, 0.2f, 0.85f);
            var x = AutoAt + new Vector3(on ? 0.22f : -0.22f, 0, 0);
            autoKnob.transform.localPosition = x;
            autoIcon.transform.localPosition = x;
            autoKnob.color = on ? Color.white : new Color(0.85f, 0.87f, 0.95f);
        }

        // 칩 · 초상 띠 · 고학년 띠를 지금 모습으로
        public void SetSnapshot(BattleSnapshot s)
        {
            Snap = s;
            due = 0;
            if (!s.Over)
                foreach (var e in s.Enemies)
                    if (!e.Dead && !e.Broken && !e.Sealed && !e.IntentLater && (e.Intent == IntentKind.Attack || e.Intent == IntentKind.Heavy)) due += e.IntentValue * Mathf.Max(1, e.IntentHits);
            chips.Set(s.PartyChips);
            gauge = s.Gauge;
            gaugeMax = Mathf.Max(1, s.GaugeMax);
            for (int i = 0; i < Ults.Count && i < s.Heroes.Count; i++) Ults[i].State = s.Heroes[i];
            for (int i = 0; i < portraits.Count && i < s.Heroes.Count; i++) portraits[i].Set(s.Heroes[i]);
        }

        public void SetGauge(int v) => gauge = v;

        public void SetPreview(PreviewParty p)
        {
            if (p == null) { pvText.text = ""; hpGain.enabled = false; return; }
            string s = "";
            if (p.Heal > 0) s += $"<color=#9fe3b4>+{p.Heal}</color> ";
            if (p.Over > 0) s += $"<size=75%><color={Tone.SubTag}>넘침 {p.Over}</color></size> ";
            if (p.Block > 0) s += $"<color=#9fd3ff>방어 +{p.Block}</color> ";
            if (p.Lose > 0) s += $"<color=#ff8a7a>HP -{p.Lose}</color>";
            pvText.text = s;
            hpGain.enabled = p.Heal > 0 && maxHp > 0;
            if (hpGain.enabled)
            {
                float w = Mathf.Max(0.06f, BarW * Mathf.Min(p.Heal, maxHp - hp) / maxHp);
                hpGain.size = new Vector2(w, BarH);
                hpGain.transform.localPosition = new Vector3(BarX0 + BarW * hp / maxHp + w / 2, BarY, 0);
            }
        }

        public void SetTurn(int turn, int wave, int waves) { }   // 턴 · 웨이브 표시는 걷었다(2026-10-07) — 부르는 곳은 그대로 둔다

        public bool OverEnd(Vector2 p) => endTurn.Over(p);

        // 끝 자세 — 전투가 끝나면 위 파티 HP 띠만 남기고 걷고, 왼쪽에 작은 「BATTLE END」 칩(싸움터는 그대로)
        public void EndPose(bool won)
        {
            foreach (var g in new[] { TR, ML, MR, BL, BR, BC }) g.Node.gameObject.SetActive(false);
            endPose = true;
            var chip = Make.Node("battleEnd", ML.Node.parent, Vector3.zero);
            endChip = chip;
            Make.Sliced("bg", chip, Res.UI("cell_9s"), Vector3.zero, new Vector2(1.9f, 0.5f), O + 1, new Color(1, 1, 1, 0.9f));
            Make.Box("bar", chip, Res.UI("white"), new Vector3(-0.9f, 0, 0), new Vector2(0.05f, 0.36f), O + 2, won ? Tone.Gold : Tone.Bad);
            Txt("t", chip, won ? "BATTLE END" : "DEFEAT", new Vector3(0.05f, -0.01f, 0), Tone.Md, O + 2, won ? Tone.Gold : Tone.Bad, TextAlignmentOptions.Center, 0.2f);
        }
        bool endPose;
        Transform endChip;

        public void SetEndReady(bool ready) => endReady = ready;
        bool endReady;

        void Update()
        {
            Anchor();
            if (endChip != null) { float k = Tone.K; endChip.localScale = new Vector3(k, k, 1); endChip.localPosition = new Vector3(-Tone.HalfW + (0.15f + 0.95f) * k, 0.6f * k, 0); }
            float dt = Time.deltaTime;
            if (lagHold > 0) lagHold -= dt; else lagF = Mathf.MoveTowards(lagF, fillF, dt * 0.9f);
            if (lagF < fillF) lagF = fillF;
            Bar(hpFill, fillF);
            Bar(hpLag, lagF);
            // 예고 피해 — 방어를 넘는 몫만큼 막대 끝이 붉게 깜빡, 맞은 뒤 남을 자리에 ▼
            int lose = Mathf.Min(hp, Mathf.Max(0, due - block));
            hpDue.enabled = dueTick.enabled = lose > 0 && maxHp > 0;
            if (hpDue.enabled)
            {
                float w = Mathf.Max(0.05f, BarW * lose / maxHp);
                hpDue.size = new Vector2(w, BarH);
                hpDue.transform.localPosition = new Vector3(BarX0 + BarW * hp / maxHp - w / 2, BarY, 0);
                var dc = hpDue.color; dc.a = 0.45f + 0.35f * Mathf.Sin(Clock.Now * 5f); hpDue.color = dc;
                dueTick.transform.localPosition = new Vector3(BarX0 + BarW * (hp - lose) / maxHp, BarY + BarH / 2 + 0.09f, 0);
            }
            // 턴 종료 단추 — 지금 상태를 넘긴다(모양은 EndTurnButton 이). 점검이 얼려 두면(Frozen) 그대로
            if (!endTurn.Frozen)
            {
                var dir = BattleDirector.I;
                endTurn.Hover = EndHover;
                endTurn.Ready = endReady;
                endTurn.Locked = dir != null && !dir.WaitingInput;
                endTurn.EnemyTurn = dir != null && dir.EnemyTurn;
                endTurn.Auto = dir != null && dir.Auto;
                endTurn.Pressed = PointerInput.Held && !PointerInput.Moved && endTurn.Over(PointerInput.Pos);
            }
            // AP — 바뀌면 톡
            apPop = Mathf.MoveTowards(apPop, 0, Time.unscaledDeltaTime * 4f);
            apText.transform.localScale = Vector3.one * (1 + 0.25f * Ease.OutCubic(apPop));
            var agc = apGlow.color; agc.a = (ap > 0 ? 0.3f : 0.1f) + 0.1f * Mathf.Sin(Clock.Now * 2.5f) + 0.4f * apPop; apGlow.color = agc;
            if (apPlus != null && apPlusT > 0)
            {   // 「+N」 — 1.4초 동안 떠오르며 사라진다(앞 0.25초는 또렷이)
                apPlusT = Mathf.MoveTowards(apPlusT, 0, Time.unscaledDeltaTime / 1.4f);
                float k = 1 - apPlusT;
                apPlus.transform.localPosition = ApAt + new Vector3(0.62f, 0.2f + 0.45f * Ease.OutCubic(k), 0);
                var pc = apPlus.color; pc.a = k < 0.25f ? 1 : apPlusT / 0.75f; apPlus.color = pc;
            }
            var hand = BattleDirector.I != null ? BattleDirector.I.Hand : null;
            int hc = hand != null ? hand.Cards.Count : 0;
            handText.text = $"{hc:00}<color={Tone.DimTag}>/{Bolzena.Core.R.HAND_MAX:00}</color>";
            // 고학년 게이지 — 큰 %(실제 값) · 부드러운 세로 채움 · 필요치 눈금
            gaugeShown = Mathf.MoveTowards(gaugeShown, gauge, Time.unscaledDeltaTime * Mathf.Max(120f, Mathf.Abs(gauge - gaugeShown) * 4f));
            gaugeText.text = Mathf.RoundToInt(gaugeShown) + "<size=45%>%</size>";
            {
                float gH = GBarTop - GBarBot, gf = Mathf.Clamp01(gaugeShown / gaugeMax), fh = gH * gf;
                bool any = false;
                foreach (var u in Ults) if (u.State != null && !u.State.Dead && gaugeShown >= u.State.UltMax) any = true;
                gaugeFill.enabled = fh > 0.02f;
                if (gaugeFill.enabled) { gaugeFill.size = new Vector2(GBarW, Mathf.Max(0.05f, fh)); gaugeFill.transform.localPosition = new Vector3(GBarX, GBarBot + fh / 2, 0); }
                gaugeFill.color = any ? Color.Lerp(new Color(0.45f, 0.8f, 1f), new Color(1f, 0.82f, 0.45f), 0.55f + 0.15f * Mathf.Sin(Clock.Now * 3f)) : new Color(0.45f, 0.8f, 1f);
                gaugeLead.enabled = gaugeFill.enabled && gf < 0.999f;
                gaugeLead.transform.localPosition = new Vector3(GBarX, GBarBot + fh, 0);
                var lc = gaugeLead.color; lc.a = 0.55f + 0.25f * Mathf.Sin(Clock.Now * 4f); gaugeLead.color = lc;
                foreach (var (need, line, num) in ticks)
                {
                    bool on = gaugeShown >= need;
                    line.color = on ? new Color(1f, 0.86f, 0.5f) : new Color(1, 1, 1, 0.55f);
                    num.color = on ? Tone.Gold : Tone.Sub;
                }
            }
            for (int i = 0; i < Ults.Count; i++) Ults[i].Gauge = gaugeShown;
        }

        void Bar(SpriteRenderer sr, float f)
        {
            float w = Mathf.Max(0.12f, BarW * f);
            sr.enabled = f > 0.001f;
            sr.size = new Vector2(w, sr.size.y);
            sr.transform.localPosition = new Vector3(BarX0 + w / 2, BarY, 0);
        }
    }

    // 비스듬한 띠 조각 하나 — 평행사변형으로 오린 얼굴(왼쪽 위 초상 · 고학년 띠가 같이 쓴다)
    public static class Slant
    {
        public static SpriteRenderer Face(string name, Transform p, string heroKey, Vector3 at, Vector2 size, int order, float centerX = 0)
            => Face(name, p, CardView.Face(heroKey, size.x / size.y), at, size, order, centerX);

        // 그림을 바로 받는 판 — 고학년 띠의 원작 볼따구(icon_graduateskill_<키>)
        public static SpriteRenderer Face(string name, Transform p, Sprite sp, Vector3 at, Vector2 size, int order, float centerX = 0)
        {
            var node = Make.Node(name, p, at);
            var mask = node.gameObject.AddComponent<SpriteMask>();
            mask.sprite = Res.UI("slant");
            mask.isCustomRangeActive = true;
            mask.frontSortingOrder = order + 1;
            mask.backSortingOrder = order - 1;
            var b = mask.sprite.bounds.size;
            node.localScale = new Vector3(size.x / b.x, size.y / b.y, 1);
            if (sp == null) return null;
            var f = Make.Sprite("face", p, sp, at + new Vector3(centerX, 0, 0), order);
            // 가로세로 같은 배율로 칸을 덮는다(넘친 곳은 마스크가 오림) — 네모 볼따구를 띠 비율로 늘리면 찌그러진다
            var sb = sp.bounds.size;
            float k = Mathf.Max(size.x / sb.x, size.y / sb.y);
            f.transform.localScale = new Vector3(k, k, 1);
            f.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return f;
        }
    }

    // 위 왼쪽 초상 띠 하나 — 비스듬한 얼굴 조각 + 성격 빛 밑줄, 오른쪽 끝에 아주 작은 키워드 아이콘 · 수. 누르면 사도 정보
    public class Portrait : MonoBehaviour
    {
        HeroState state;
        SpriteRenderer face, kwIcon;
        TextMeshPro kwText;
        public System.Action OnTap;
        int lastKw = -1;
        float pop;
        const float W = 1.5f, H = 0.4f, FaceW = 0.98f;

        public static Portrait Create(Transform parent, HeroState h, Vector3 at, int order)
        {
            var t = Make.Node("portrait_" + h.Key, parent, at);
            var p = t.gameObject.AddComponent<Portrait>();
            p.state = h;
            var tint = Color.Lerp(Tone.Nature(h.Nature), h.Tint, 0.4f);
            Make.Box("bg", t, Res.UI("slant"), Vector3.zero, new Vector2(W, H), order, new Color(0.04f, 0.06f, 0.13f, 0.78f));
            p.face = Slant.Face("face", t, h.Key, new Vector3(-W / 2 + FaceW / 2, 0, 0), new Vector2(FaceW, H), order + 2);
            Make.Box("line", t, Res.UI("white"), new Vector3(-0.03f, -H / 2 + 0.012f, 0), new Vector2(W - 0.1f, 0.025f), order + 3, tint);
            p.kwIcon = Make.Box("kwi", t, Res.UI("diamond"), new Vector3(W / 2 - 0.4f, 0, 0), new Vector2(0.14f, 0.14f), order + 3, Tone.Gold);
            p.kwText = Make.Text("kw", t, "", new Vector3(W / 2 - 0.3f, -0.005f, 0), 0.16f, order + 3, Color.white, TextAlignmentOptions.Left);
            p.kwText.rectTransform.pivot = new Vector2(0, 0.5f);
            Make.Outline(p.kwText, 0.25f, Tone.Outline);
            var b = t.gameObject.AddComponent<Button>();
            b.Size = new Vector2(W, H + 0.06f);
            b.OnClick = () => p.OnTap?.Invoke();
            TipZone.Add(p, new Vector2(W, H + 0.06f), () => p.TipText(), 3);
            p.Set(h);
            return p;
        }

        string TipText()
        {
            var h = state;
            if (h == null) return null;
            var s = Tip.Head(h.Name) + "  " + Tip.Dim($"{h.Role} · {h.Nature}");
            if (!string.IsNullOrEmpty(h.KeywordName)) s += "\n" + $"<color={Tone.GoldTag}>{h.KeywordName}</color> {h.KeywordStacks}" + (string.IsNullOrEmpty(h.KeywordText) ? "" : "\n" + h.KeywordText);   // 수치가 다 든 한 가지 글(CardText.Trait)
            s += "\n" + Tip.Dim("누르면 사도 정보");
            return s;
        }

        public void Set(HeroState h)
        {
            state = h;
            bool has = !string.IsNullOrEmpty(h.KeywordName);
            kwIcon.enabled = has;
            kwText.enabled = has;
            if (has)
            {
                if (lastKw >= 0 && h.KeywordStacks != lastKw) pop = 1;
                lastKw = h.KeywordStacks;
                kwText.text = h.KeywordStacks.ToString();
                kwText.color = h.KeywordStacks > 0 ? Tone.Gold : Tone.Sub;
            }
            if (face) face.color = h.Dead ? new Color(0.35f, 0.35f, 0.4f) : Color.white;
        }

        void Update()
        {
            if (pop <= 0) return;
            pop = Mathf.MoveTowards(pop, 0, Time.unscaledDeltaTime * 3f);
            kwText.transform.localScale = Vector3.one * (1 + 0.45f * Ease.OutCubic(pop));
        }
    }

    // 고학년 띠 하나 — 비스듬한 초상 조각 + 왼쪽 값 상자 + (쓸 수 있으면) 오른쪽 위 반짝이. 게이지가 차면 띠가 밝아지고 숨 쉰다.
    // 이름 · 고학년 이름 · 효과는 툴팁(오른쪽 클릭 · 길게 누르기는 사도 정보). 고르면 금 테 + 「대상 선택」
    public class UltButton : MonoBehaviour
    {
        public int Hero;
        public HeroState State;
        public bool Selected;
        public float Gauge;
        SpriteRenderer face, glow, rim, spark, fillBar;
        TextMeshPro label, costText, nameText, stateText;
        public bool Ready;
        const int O = 570;
        public const float SW = 2.4f, SH = 0.64f;
        const float FaceW = 0.92f;          // 오른쪽 초상 너비 — 왼쪽은 값 상자 · 고학년 이름 · 남은 양
        public const float D = 0.9f;                 // 정보 창 자리 잡기용 대략 크기

        public static UltButton Create(Transform parent, HeroState h, int idx, Vector3 pos)
        {
            var t = Make.Node("ult_" + h.Key, parent, pos);
            var b = t.gameObject.AddComponent<UltButton>();
            b.Hero = idx;
            b.glow = Make.Box("glow", t, Res.UI("soft"), Vector3.zero, new Vector2(SW * 1.5f, SH * 2.4f), O - 2, new Color(1f, 0.8f, 0.4f, 0), Res.SpriteMat(true, 1.6f));
            b.rim = Make.Box("rim", t, Res.UI("slant"), Vector3.zero, new Vector2(SW + 0.06f, SH + 0.06f), O - 1, new Color(1, 1, 1, 0.25f));
            Make.Box("bg", t, Res.UI("slant"), Vector3.zero, new Vector2(SW, SH), O, new Color(0.04f, 0.06f, 0.13f, 0.92f));
            // 얼굴 — 원작 고학년 단추의 볼따구(Art/icon_graduateskill_<키>), 없으면 초상 얼굴
            var gs = Res.Sprite("Art/icon_graduateskill_" + h.Key);
            b.face = gs != null ? Slant.Face("face", t, gs, new Vector3(SW / 2 - FaceW / 2 - 0.04f, 0, 0), new Vector2(FaceW, SH), O + 1)
                                : Slant.Face("face", t, h.Key, new Vector3(SW / 2 - FaceW / 2 - 0.04f, 0, 0), new Vector2(FaceW, SH), O + 1);
            // 차오름 — 띠 아래 가는 줄
            b.fillBar = Make.Box("fill", t, Res.UI("white"), new Vector3(0, -SH / 2 + 0.025f, 0), new Vector2(SW - 0.1f, 0.04f), O + 3, Tone.Gold);
            // 값 상자 — 왼쪽 위(이 사도 고학년에 드는 게이지, 실제 %)
            var cb = new Vector3(-SW / 2 + 0.38f, 0.13f, 0);
            Make.Sliced("costbg", t, Res.UI("cell_9s"), cb, new Vector2(0.62f, 0.28f), O + 3);
            b.costText = Make.Text("cost", t, h.UltMax + "<size=60%>%</size>", cb + new Vector3(0, -0.005f, 0), Tone.Body, O + 4, Color.white);
            Make.Outline(b.costText, 0.25f, Tone.Outline);
            // 남은 양(또는 「사용 가능」) — 값 상자 아래
            b.stateText = Make.Text("ustate", t, "", new Vector3(-SW / 2 + 0.74f, 0.13f, 0), Tone.Cap, O + 4, Tone.Sub, TextAlignmentOptions.Left);
            b.stateText.rectTransform.pivot = new Vector2(0, 0.5f);
            b.stateText.rectTransform.sizeDelta = new Vector2(SW - FaceW - 0.76f, 0.2f);

            b.stateText.textWrappingMode = TextWrappingModes.NoWrap;
            Make.Outline(b.stateText, 0.3f, Tone.Outline);
            // 고학년 이름 — 띠 안 아랫줄(초상 왼쪽까지)
            b.nameText = Make.Text("uname", t, h.UltName ?? "", new Vector3(-SW / 2 + 0.1f, -0.14f, 0), Tone.Cap, O + 4, Tone.Sub, TextAlignmentOptions.Left);
            b.nameText.rectTransform.pivot = new Vector2(0, 0.5f);
            b.nameText.rectTransform.sizeDelta = new Vector2(SW - FaceW - 0.12f, 0.2f);
            b.nameText.textWrappingMode = TextWrappingModes.NoWrap; b.nameText.overflowMode = TextOverflowModes.Ellipsis;
            Make.Outline(b.nameText, 0.3f, Tone.Outline);
            b.spark = Make.Box("spark", t, Res.UI("ic_spark"), new Vector3(SW / 2 - 0.16f, SH / 2 - 0.08f, 0), new Vector2(0.3f, 0.3f), O + 4, Color.white, Res.SpriteMat(false, 1.5f));
            b.label = Make.Text("label", t, "대상 선택", new Vector3(SW / 2 + 0.1f, 0, 0), Tone.Cap, O + 4, Tone.Gold, TextAlignmentOptions.Left);
            b.label.rectTransform.pivot = new Vector2(0, 0.5f);
            Make.Outline(b.label, 0.3f, Tone.Outline);
            b.label.enabled = false;
            b.State = h;
            TipZone.Add(b, new Vector2(SW, SH), () => b.TipText(), 3);
            return b;
        }

        public void Set(int v, int max)
        {
            bool r = v >= max;
            if (r && !Ready) StartCoroutine(ReadyPop());
            Ready = r;
        }

        IEnumerator ReadyPop()
        {
            Vfx.Ring(transform.position, 0.4f, 1.6f, 0.5f, new Color(1f, 0.85f, 0.5f, 1f), 2f, "FX_IN_Ring_ShockWave_01", O + 4, transform.parent, 1f, true);
            yield return Clock.Tween(0.35f, t => transform.localScale = Vector3.one * (1 + 0.12f * (1 - Ease.OutBack(t))), true);
        }

        public bool Over(Vector2 p)
        {
            var l = transform.InverseTransformPoint(p);
            return Mathf.Abs(l.x) < SW / 2 + 0.04f && Mathf.Abs(l.y) < SH / 2 + 0.04f;
        }

        string TipText()
        {
            var h = State;
            if (h == null) return null;
            string body = h.UltText;   // 효과 전부(CardText.Fx) — 「자세히」 없음
            var s = Tip.Head($"{h.Name} · 「{h.UltName}」") + "\n" + body + "\n" + Tip.Dim($"게이지 {Mathf.RoundToInt(Gauge)}% / 필요 {h.UltMax}%" + " (파티 공용 — 카드에 쓴 AP 1 마다 +10%)");
            s += "\n\n" + Tip.Dim(Ready ? "눌러서 고르고, 적(또는 다시 이 띠)을 눌러 씁니다 · 단축키 Z X C" : "게이지가 모자랍니다") + "\n" + Tip.Dim("오른쪽 클릭 · 길게 누르기 — 사도 정보");
            return s;
        }

        void Update()
        {
            label.enabled = Selected;
            float f = State != null && State.UltMax > 0 ? Mathf.Clamp01(Gauge / State.UltMax) : 0;
            fillBar.transform.localScale = new Vector3(Mathf.Max(0.001f, f) * fillBar.transform.localScale.x / Mathf.Max(0.001f, fillBar.transform.localScale.x), fillBar.transform.localScale.y, 1);
            Make.Fit(fillBar, new Vector2(Mathf.Max(0.001f, (SW - 0.1f) * f), 0.035f));
            fillBar.transform.localPosition = new Vector3(-(SW - 0.1f) / 2 + (SW - 0.1f) * f / 2, -SH / 2 + 0.02f, 0);
            fillBar.color = Ready ? new Color(1f, 0.86f, 0.5f) : new Color(0.62f, 0.88f, 1f, 0.8f);
            spark.enabled = Ready;
            if (Ready) { spark.transform.localRotation = Quaternion.Euler(0, 0, Clock.Now * 60f); Make.Alpha(spark, 0.75f + 0.25f * Mathf.Sin(Clock.Now * 5f)); }
            rim.color = Selected ? Tone.Gold : Ready ? new Color(1f, 0.86f, 0.5f, 0.7f) : new Color(1, 1, 1, 0.18f);
            var c = glow.color;
            c.a = Selected ? 0.6f : Ready ? 0.16f + 0.1f * Mathf.Sin(Clock.Now * 4f) : 0f;
            glow.color = c;
            if (face) face.color = Ready || Selected ? Color.white : new Color(0.45f, 0.47f, 0.55f);
            costText.color = Ready ? Tone.Gold : Tone.Ink;
            stateText.text = State != null && State.Dead ? "쓰러짐" : Ready ? "사용 가능" : "";   // 남은 양은 적지 않는다(사용자 2026-10-05)
            stateText.color = Ready ? new Color(1f, 0.86f, 0.5f, 0.8f + 0.2f * Mathf.Sin(Clock.Now * 5f)) : new Color(0.62f, 0.85f, 1f);
            nameText.color = Ready ? new Color(1f, 0.92f, 0.75f) : Tone.Sub;
            float sx = Mathf.MoveTowards(transform.localScale.x, Selected ? 1.06f : 1f, Time.unscaledDeltaTime * 2f);
            transform.localScale = new Vector3(sx, sx, 1);
        }
    }
}
