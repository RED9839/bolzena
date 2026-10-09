using System.Collections;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 카드 한 장 — 손패 · 신탁 창 · 더미 보기가 같은 정보 차례(카제나 카드의 차례를 따르고, 그림 · 장식 · 글꼴은 우리 것):
    //   위: 큰 코스트 · 이름 · 종류 아이콘 + 종류(「공격」 · 「스킬」 · 「강화」)      가운데: 그림이 카드 전체(둥근 모서리로 오린다 — 판 화면 W.Card 와 같은 꼴)
    //   그림(임시 규칙 — runui Docs/카드그림.md · CardArt): 시작 카드 = 주인 사도 스탠딩 상반신, 고유 카드 = 카드 그림 표의 스킬 아이콘
    //   (흐린 확대 바탕 + 가운데 선명한 아이콘), 교주 · 상태 카드 = 그림 없음
    //   아래: 작은 장식 선 · 키워드 태그 줄(금 「[ 회수 / 소멸 ]」) · 효과 글(수치 하늘색) — 손패에서는 감추고 올리면 보인다(ShowDesc)
    //   왼쪽 가장자리 띠 · 테 = 주인 사도의 성격 색(교주 카드는 넣은 사도의 성격 — 주인이 없으면 금 · 상태 잿빛 보라 · 신탁 금)
    // 신탁 카드면 뒤에 도는 빛줄기 · 금빛 테 · 떠오르는 반짝이. 자리(Target*)를 주면 매 프레임 부드럽게 따라간다(화면 시간).
    public class CardView : MonoBehaviour
    {
        public CardInfo Info;
        public Vector3 TargetPos;
        public float TargetRot, TargetScale = 1f;
        public float Follow = 14f;
        public float Shade = 1f;                                   // 손패가 다른 카드를 고르는 동안 살짝 어둡게(1 = 그대로)
        public bool Playable = true;
        public bool Hovered;
        public bool ShowDesc = true;          // 효과 글(손패는 올렸을 때만)
        public bool NoOracleMark;             // 신탁 고르기 창 — 신탁 별 마크 없음(축복 마크만)
        SpriteRenderer markBg, blessLineIc; TextMeshPro blessLineTx;
        public Color? CostTint;               // 비용 숫자 빛깔 덮기(신탁 축복 — 비용이 내려가면 초록)
        public bool ShowPin = false;           // 오른쪽 위 사도 얼굴(손패는 손 위의 핀이 맡는다)
        public float SlotX;                   // 손 안의 제자리 x(끌어 겨눌 때 그 자리에 띄운다)
        SpriteRenderer rim, body, art, iconPlate, icon, descBg, glow, flash, epiGlow, band, shadeT, shadeB, typeIcon, deco, decoDot, pinRim, pin;
        SpriteMask artMask, pinMask;
        SpriteRenderer typeBg;
        MeshRenderer rays;
        Material raysMat;
        TextMeshPro costText, nameText, typeText, tagText, descText, epiText;
        // 표식(2026-10-06 — 판 화면 W.Card 와 같은 자리): 신탁 = 장식 선 위 금빛 띠 + 테 바깥 금빛 · 축복 = 그 위 초록 띠(후광) · 복제 = 오른쪽 위 셋째 줄
        SpriteRenderer oraRim, epiBg, epiIcon, blessBg, blessIcon, copyBg, copyIcon;
        TextMeshPro blessText, copyText;
        // 신탁 받은 카드 — 장식 선의 별 줄(가운데 큰 별 + 양옆 작은 별 둘씩 — 카제나 번뜩임 별, 2026-10-07 사용자 「이름 띠 말고 별 표시」).
        //   이름 띠(epiBg · epiText)는 쓰지 않는다. 별은 선 가운데 ±0.2 W 안 — 선 양 끝(DecoHalf)은 다른 표식(축복 날개 따위)이 쓴다.
        SpriteRenderer[] oraStars;
        bool oracleMark;
        /// <summary>장식 선 높이(카드 가운데 기준) · 반 너비 — 장식 선에 붙는 표식(신탁 별 · 축복 …)이 같이 쓴다.</summary>
        public float DecoY { get; private set; }
        public float DecoHalf => deco != null && deco.sprite != null ? deco.transform.localScale.x * deco.sprite.bounds.size.x / 2 : W * 0.275f;
        // 다음 카드 강화 표시(2026-10-08) — 주황 가는 테 + 옅은 빛 + 윗변에 걸친 작은 「강화 ×1.5」 배지. 신탁(금 별) · 축복(연보라 마름모) · 연두 · 청록 책과 겹치지 않는 색
        SpriteRenderer empRim, empGlow, empBadgeBg, empBadgeIc;
        TextMeshPro empBadgeTx;
        float empT;
        SpriteRenderer blessWingL, blessWingR;   // 축복 표식 — 장식 선 양 끝 금빛 날개(2026-10-07 · 이름 띠 대신)
        int order;
        float epiT, sparkT;
        float dim = 1f, descA = 1f;
        float alphaMul = 1f;
        public const float W = 1.9f, H = 2.7f;
        // 그림 창 — 카드 전체(테 안쪽)
        public const float Bd = 0.03f;   // 테두리 두께(양쪽 합) — 2026-10-08 사용자 「너무 두껍다」 0.07 → 0.04
        const float ArtW = W - Bd, ArtH = H - Bd;
        const float Px = W / 200f, TypeY = 44 * Px;   // 판 화면 W.Card(폭 200) 한 px · 종류 알약 가운데(위에서)
        const float ArtY = 0f;
        const float IconS = 1.06f, IconY = 0.27f;   // 고유 카드 아이콘(가운데 · 살짝 위)
        /// <summary>카드 주인 사도(초상 핀 · 빛깔) — 감독이 단다.</summary>
        public static System.Func<int, HeroState> HeroOf;

        public static CardView Create(Transform parent, CardInfo info)
        {
            var root = Make.Node("card_" + info.Id, parent);
            var c = root.gameObject.AddComponent<CardView>();
            c.Build(info);
            return c;
        }

        void Build(CardInfo info)
        {
            Info = info;
            var t = transform;
            raysMat = Res.NewMat("Bolzena/Rays");
            raysMat.SetColor("_Color", new Color(1f, 0.8f, 0.35f, 1f));
            raysMat.SetFloat("_Count", 16);
            raysMat.SetFloat("_Spin", 0.12f);
            raysMat.SetFloat("_Boost", 3f);
            raysMat.SetFloat("_Inner", 0.2f);
            raysMat.SetFloat("_Outer", 0.62f);
            rays = Make.Quad("rays", t, new Vector3(0, 0.1f, 0), new Vector2(5.4f, 5.4f), raysMat, 0);
            Make.Own(rays.gameObject, raysMat);       // 카드마다 새로 만든 재질 — 카드와 함께 지운다
            epiGlow = Make.Box("epiglow", t, Res.UI("card_glow"), Vector3.zero, new Vector2(2.55f, 3.3f), 0, new Color(1f, 0.82f, 0.35f, 1f), Res.SpriteMat(true, 1.8f));
            glow = Make.Box("glow", t, Res.UI("card_glow"), Vector3.zero, new Vector2(2.5f, 3.3f), 0, new Color(0.6f, 0.9f, 1f, 0f), Res.SpriteMat(true, 1.8f));
            rim = Make.Box("rim", t, Res.UI("card_mask"), Vector3.zero, new Vector2(W, H), 0);
            body = Make.Box("body", t, Res.UI("card_mask"), Vector3.zero, new Vector2(W - Bd, H - Bd), 0, new Color(0.06f, 0.08f, 0.16f));
            // 그림 — 카드 대부분, 둥근 모서리로 오린다(마스크 범위는 SetOrder 가 이 카드 차례에 맞춘다)
            var mn = Make.Node("artmask", t);
            artMask = mn.gameObject.AddComponent<SpriteMask>();
            artMask.sprite = Res.UI("card_mask");
            artMask.isCustomRangeActive = true;
            { var mb = artMask.sprite.bounds.size; mn.localScale = new Vector3((W - Bd) / mb.x, (H - Bd) / mb.y, 1); }
            art = Make.Sprite("art", t, null, Vector3.zero, 0);
            art.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            iconPlate = Make.Box("iconplate", t, Res.UI("card_mask"), new Vector3(0, IconY, 0), new Vector2(IconS + 0.07f, IconS + 0.07f), 0);
            iconPlate.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            icon = Make.Sprite("icon", t, null, new Vector3(0, IconY, 0), 0);
            icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            band = Make.Box("band", t, Res.UI("white"), new Vector3(-W / 2 + 0.07f, 0, 0), new Vector2(0.07f, H - 0.1f), 0);
            band.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            shadeT = Make.Box("shadeT", t, Res.UI("grad_v"), new Vector3(0, H / 2 - 0.5f, 0), new Vector2(W - Bd, 1.0f), 0, new Color(0.02f, 0.03f, 0.08f, 0.92f));
            shadeT.flipY = true;          // grad_v 는 아래가 짙다 — 위 그늘은 뒤집어 위가 짙게
            shadeT.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            shadeB = Make.Box("shadeB", t, Res.UI("grad_v"), new Vector3(0, -H / 2 + 0.85f, 0), new Vector2(W - Bd, 1.7f), 0, new Color(0.01f, 0.02f, 0.06f, 1f));   // 그림이 카드 전체라 글 밑을 더 짙게 · 높게
            shadeB.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            // 효과 글 뒤 어둠 판 — 그림이 카드 전체라 흰 옷 · 밝은 그림 위에서도 글이 읽히게(올렸을 때 짙게)
            descBg = Make.Box("descbg", t, Res.UI("card_mask"), Vector3.zero, new Vector2(W - 0.14f, 0.6f), 0, new Color(0.01f, 0.02f, 0.06f, 0.78f));
            descBg.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            // 위 — 큰 코스트 · 이름 · 종류(2026-10-09 판 화면 W.Card 와 같은 자리 · 크기 — 카드 폭 200 px = W, Px 한 칸)
            float x0 = -W / 2, y0 = H / 2;
            costText = Make.Text("cost", t, "", new Vector3(x0 + 27 * Px, y0 - 27 * Px, 0), 44 * Px, 0, Color.white);
            Make.Outline(costText, 0.22f, Tone.Outline);
            nameText = Make.Text("name", t, "", new Vector3(x0 + 47 * Px, y0 - 20.5f * Px, 0), 19 * Px, 0, Color.white, TextAlignmentOptions.Left, W - 0.73f);
            nameText.rectTransform.pivot = new Vector2(0, 0.5f);
            nameText.enableAutoSizing = true; nameText.fontSizeMax = 190 * Px; nameText.fontSizeMin = 110 * Px;
            nameText.rectTransform.sizeDelta = new Vector2(W - 0.73f, 25 * Px);
            Make.Outline(nameText, 0.25f, Tone.Outline);
            typeBg = Make.Sliced("typebg", t, Res.UI("bar_fill_9s"), new Vector3(x0 + 45 * Px + 0.4f, y0 - TypeY, 0), new Vector2(0.8f, 20 * Px), 0, new Color(0.02f, 0.03f, 0.08f, 0.7f));
            typeIcon = Make.Box("typei", t, Res.UI("ic_sword"), new Vector3(x0 + 58.5f * Px, y0 - TypeY, 0), new Vector2(13 * Px, 13 * Px), 0);
            typeText = Make.Text("type", t, "", new Vector3(x0 + 67 * Px, y0 - TypeY, 0), 13 * Px, 0, Color.white, TextAlignmentOptions.Left, 1.2f);
            typeText.rectTransform.pivot = new Vector2(0, 0.5f);
            Make.Outline(typeText, 0.28f, Tone.Outline);
            // 사도 얼굴 핀 — 오른쪽 위 작은 원(손패는 손 위 핀이 맡아 감춘다)
            var fp = new Vector3(W / 2 - 0.26f, H / 2 - 0.27f, 0);
            pinRim = Make.Box("pinring", t, Res.UI("circle"), fp, new Vector2(0.38f, 0.38f), 0);
            var pm = Make.Node("pinmask", t, fp);
            pinMask = pm.gameObject.AddComponent<SpriteMask>();
            pinMask.sprite = Res.UI("circle");
            pinMask.isCustomRangeActive = true;
            { var mb = pinMask.sprite.bounds.size; pm.localScale = new Vector3(0.33f / mb.x, 0.33f / mb.y, 1); }
            pin = Make.Sprite("pin", t, null, fp, 0);
            pin.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            // 아래 — 장식 선 · 태그 줄 · 효과 글
            deco = Make.Box("deco", t, Res.UI("band_line"), Vector3.zero, new Vector2(W * 0.55f, 0.02f), 0, new Color(1f, 0.88f, 0.6f, 0.75f));
            decoDot = Make.Box("decod", t, Res.UI("diamond"), Vector3.zero, new Vector2(0.09f, 0.09f), 0, new Color(1f, 0.9f, 0.65f));
            oraStars = new SpriteRenderer[5];
            for (int i = 0; i < 5; i++)
                oraStars[i] = Make.Box("orastar" + i, t, Bolzena.RunUI.Theme.S("ic_spark"), Vector3.zero, i == 0 ? new Vector2(0.19f, 0.19f) : new Vector2(0.1f, 0.1f), 0, new Color(1f, 0.85f, 0.45f));
            tagText = Tone.Text("tags", t, "", new Vector3(0, -H / 2 + 0.88f, 0), 0.14f, 0, Tone.Gold, TextAlignmentOptions.Center, true, W - 0.2f);
            Make.Outline(tagText, 0.2f, Tone.Outline);
            descText = Tone.Text("desc", t, "", Vector3.zero, 0.155f, 0, Tone.Ink, TextAlignmentOptions.Center, true, W - 0.24f);
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.enableAutoSizing = true; descText.fontSizeMax = 1.55f; descText.fontSizeMin = 1.12f;
            Bolzena.RunUI.CardTerms.Prepare(descText);   // 글 속 생성 카드 아이콘(<sprite>) — 안 걸면 태그가 글자로 보인다
            descText.overflowMode = TextOverflowModes.Ellipsis;
            descText.lineSpacing = -4;
            Make.Outline(descText, 0.2f, Tone.Outline);
            // 신탁 이름표 — 장식 선 바로 위(Refresh 가 자리를 잡는다)
            epiText = Make.Text("epi", t, "", new Vector3(0, -0.05f, 0), 0.14f, 0, new Color(1f, 0.88f, 0.55f));
            Make.Outline(epiText, 0.3f, Tone.Outline);
            // 표식 — 신탁 띠 · 축복 띠 · 복제 표(Refresh 가 켜고 자리를 잡는다)
            oraRim = Make.Box("orarim", t, Res.UI("card_mask"), Vector3.zero, new Vector2(W + 0.05f, H + 0.05f), 0, new Color(1f, 0.82f, 0.4f, 0.95f));
            empGlow = Make.Box("empglow", t, Res.UI("card_glow"), Vector3.zero, new Vector2(2.3f, 3.1f), 0, new Color(Tone.Emp.r, Tone.Emp.g, Tone.Emp.b, 0f), Res.SpriteMat(true, 1.8f));
            empRim = Make.Box("emprim", t, Res.UI("card_mask"), Vector3.zero, new Vector2(W + 0.045f, H + 0.045f), 0, Tone.Emp);
            empBadgeBg = Make.Sliced("empbg", t, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(0.9f, 0.2f), 0, new Color(0.26f, 0.1f, 0.01f, 0.96f));
            empBadgeIc = Make.Box("empic", t, Res.UI("ic_up"), Vector3.zero, new Vector2(0.13f, 0.13f), 0, Tone.Emp);
            empBadgeTx = Make.Text("emptx", t, "", Vector3.zero, 0.105f, 0, new Color(1f, 0.82f, 0.58f));
            Make.Outline(empBadgeTx, 0.3f, Tone.Outline);
            epiBg = Make.Sliced("epibg", t, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(1f, 0.22f), 0, new Color(0.35f, 0.25f, 0.05f, 0.95f));
            epiIcon = Make.Box("epiic", t, Bolzena.RunUI.Theme.S("ic_spark"), Vector3.zero, new Vector2(0.14f, 0.14f), 0, new Color(1f, 0.82f, 0.48f));
            blessBg = Make.Sliced("blessbg", t, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(1f, 0.22f), 0, new Color(0.06f, 0.27f, 0.2f, 0.95f));
            blessIcon = Make.Box("blessic", t, Bolzena.RunUI.Theme.S("ic_bless"), Vector3.zero, new Vector2(0.15f, 0.15f), 0, new Color(0.75f, 1f, 0.82f));
            blessText = Make.Text("bless", t, "", Vector3.zero, 0.13f, 0, new Color(0.85f, 1f, 0.9f));
            Make.Outline(blessText, 0.3f, Tone.Outline);
            blessWingR = Make.Sprite("blesswingr", t, Bolzena.RunUI.BlessFx.Wing, Vector3.zero, 0);
            blessWingL = Make.Sprite("blesswingl", t, Bolzena.RunUI.BlessFx.Wing, Vector3.zero, 0);
            blessWingL.flipX = true;
            copyBg = Make.Sliced("copybg", t, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(1f, 0.2f), 0, new Color(0.04f, 0.05f, 0.1f, 0.88f));
            copyIcon = Make.Box("copyic", t, Bolzena.RunUI.Theme.S("ic_copy"), Vector3.zero, new Vector2(0.14f, 0.14f), 0, new Color(0.78f, 0.86f, 1f));
            copyText = Make.Text("copy", t, "", Vector3.zero, 0.105f, 0, new Color(0.85f, 0.9f, 1f));
            Make.Outline(copyText, 0.3f, Tone.Outline);
            markBg = Make.Box("markbg", t, Res.UI("circle"), Vector3.zero, new Vector2(0.4f, 0.4f), 0, new Color(1f, 0.82f, 0.38f));
            blessLineIc = Make.Box("blesslineic", t, Bolzena.RunUI.Theme.S("ic_bless"), Vector3.zero, new Vector2(0.15f, 0.15f), 0, new Color(0.85f, 0.76f, 1f));
            blessLineTx = Tone.Text("blessline", t, "", Vector3.zero, 0.125f, 0, new Color(0.88f, 0.8f, 1f), TextAlignmentOptions.Center, true, W - 0.5f);
            Make.Outline(blessLineTx, 0.25f, Tone.Outline);
            flash = Make.Box("flash", t, Res.UI("card_mask"), Vector3.zero, new Vector2(W, H), 0, new Color(1, 1, 1, 0), Res.SpriteMat(true, 2f));
            Refresh();
            SetOrder(500);
        }

        // 틀 빛깔 = 주인 사도의 성격(톤.md 성격 색) — 종류(공격 · 스킬 · 강화)는 아이콘 · 글로 가른다.
        // 교주 카드 = 금빛 중립, 상태 · 저주 = 어두운 잿빛 보라, 신탁(빛나는) = 금
        public static Color RimColor(CardInfo c)
        {
            // 판 화면과 같은 표(Theme.NatureCard — 톤.md §1: 순수 #4CB83A · 광기 #E04848 · 냉정 #18C2E6 · 우울 #8A5CE6 · 활발 #E6C21A)
            if (c.Epiphany) return new Color(1f, 0.82f, 0.4f);
            if (c.Type == CardType.Status || (c.Hero < 0 && c.Unplayable)) return Bolzena.RunUI.Theme.StatusCard;
            if (c.Hero < 0 && !string.IsNullOrEmpty(c.Grade)) return Bolzena.RunUI.Theme.GradeOf(c.Grade);   // 교주 카드 = 등급 색(일반 회색 · 고급 연두 · 희귀 하늘 · 전설 보라)
            if ((c.Hero < 0 && c.Owner < 0) || string.IsNullOrEmpty(c.Nature)) return Bolzena.RunUI.Theme.LeaderCard;   // 교주 카드는 넣은 사도(Owner)의 성격 — 주인이 없을 때만 금빛 중립
            return Bolzena.RunUI.Theme.NatureCardOf(c.Nature);
        }

        public void Refresh()
        {
            var info = Info;
            var rc = RimColor(info);
            rim.color = rc;
            int who = info.Hero >= 0 ? info.Hero : info.Owner;   // 교주 카드는 넣은 사도의 얼굴 핀
            band.color = rc;
            glow.color = new Color(Mathf.Lerp(rc.r, 1f, 0.3f), Mathf.Lerp(rc.g, 1f, 0.3f), Mathf.Lerp(rc.b, 1f, 0.3f), glow.color.a);   // 올림 빛도 성격 빛
            SetArt(info, rc);
            costText.text = info.Unplayable && info.Cost <= 0 ? "-" : info.Cost.ToString() + (info.CostUpWhy != null && info.Cost > info.BaseCost ? "<size=40%><voffset=0.5em>▲</voffset></size>" : "");
            nameText.text = info.Name;
            typeText.text = info.TypeName ?? (info.Type == CardType.Attack ? "공격" : info.Type == CardType.Power ? "강화" : "스킬");   // 「공격」 · 「스킬」 · 「강화」 그대로(머리말 없이 — 판 화면 W.TypeLabel 과 같은 글)
            typeText.color = TypeColor(info);
            typeText.ForceMeshUpdate();
            typeBg.size = new Vector2(typeText.preferredWidth + 31 * Px, 20 * Px);
            typeBg.transform.localPosition = new Vector3(-W / 2 + 45 * Px + typeBg.size.x / 2, H / 2 - TypeY, 0);
            // 판 화면(W.TypeIcon)과 같은 그림 — 칼 둘(ic_swords)은 runui 스프라이트에만 있다(UI/ 에서 찾으면 비어 공격 카드에 아이콘이 없었다)
            typeIcon.sprite = info.Type == CardType.Attack ? Bolzena.RunUI.Theme.S("ic_swords") : Res.UI(info.Type == CardType.Power ? "ic_up" : info.Type == CardType.Status ? "ic_skull" : "ic_skill");
            Make.Fit(typeIcon, new Vector2(13 * Px, 13 * Px));
            var h = who >= 0 && HeroOf != null ? HeroOf(who) : null;
            pin.sprite = h != null ? Face(h.Key) : null;
            if (pin.sprite != null) Make.Fit(pin, new Vector2(0.36f, 0.36f));
            if (h != null) pinRim.color = info.Hero >= 0 ? Color.Lerp(h.Tint, Color.white, 0.4f) : Color.Lerp(rc, Color.white, 0.35f);
            string tags = Tone.TagLine(info.Tags);
            tagText.text = tags;
            // 효과 글 칸 높이 — 기본(태그 있으면 0.56 · 없으면 0.68)에서 글이 많으면 0.16까지 위로 늘린다(2026-10-07 카드 글을 조건마다 줄 나눔 —
            // 줄 수가 늘어도 글자가 1.3 아래로 줄지 않게). 늘린 만큼 태그 줄 · 장식 선 · 어둠 판도 올린다(얼굴은 위 35% 칸이라 닿지 않음)
            // 글 = 어절 단위 · 덩이 묶음 · 화살표 규칙(조건 → 결과가 칸에 안 들어가면 「→ 결과」 를 다음 줄 맨 앞으로 — CardTerms.Fit)
            string rawDesc = Tone.CardText(info.Text);
            float descW = W - 0.24f, baseH = tags.Length > 0 ? 0.56f : 0.68f, needH = 0;
            descText.text = Bolzena.RunUI.CardTerms.FitFor(descText, rawDesc, descW, 1.3f);
            if (!string.IsNullOrEmpty(descText.text))
            {
                bool auto = descText.enableAutoSizing; float fs = descText.fontSize;
                descText.enableAutoSizing = false; descText.fontSize = 1.3f;
                needH = descText.GetPreferredValues(descText.text, descW, 0).y;
                descText.enableAutoSizing = auto; descText.fontSize = fs;
            }
            float descH = Mathf.Clamp(needH + 0.04f, 0.22f, baseH + 0.16f), extra = descH - baseH;   // 글 칸은 글이 필요한 만큼만(도감 카드와 같게 — 아래가 비지 않고 그림 칸이 넓어진다)
            bool hasBL = !string.IsNullOrEmpty(info.MarkBless) && !string.IsNullOrEmpty(info.MarkBlessText) && !Bolzena.RunUI.BlessFx.Old;
            float blessH = hasBL ? 0.2f : 0;   // 축복 효과 전용 줄(본문 맨 아래) — 기본 효과 글은 그대로 두고 그 아래에 덧붙인다
            tagText.transform.localPosition = new Vector3(0, -H / 2 + 0.88f + extra + blessH, 0);
            float decoY = -H / 2 + (tags.Length > 0 ? 1.06f : 0.9f) + extra + blessH;
            PlaceArt(decoY);   // 그림 자리 — 장식 선에 붙인다
            deco.transform.localPosition = new Vector3(0, decoY, 0);
            decoDot.transform.localPosition = new Vector3(0, decoY, 0);
            DecoY = decoY;
            oracleMark = !info.Epiphany && !string.IsNullOrEmpty(info.EpiphanyLabel);   // 받은 신탁 — 이름 대신 별 줄
            PlaceOracleStars();
            epiText.transform.localPosition = new Vector3(0, decoY + 0.14f, 0);   // 신탁 이름표 — 장식 선 바로 위(그림 · 아이콘 가운데를 가리지 않게)
            descText.transform.localPosition = new Vector3(0, -H / 2 + (tags.Length > 0 ? 0.38f : 0.44f) + extra / 2 + blessH, 0);
            descText.rectTransform.sizeDelta = new Vector2(descW, descH);
            // 화살표 규칙은 그릴 크기로 다시 — 자동 크기가 고른 크기에서 한 줄에 드는지 재고, 바뀌면 다시 고른다(두세 번이면 멈춘다)
            if (!string.IsNullOrEmpty(rawDesc))
            {
                float fsNow = descText.fontSizeMax;
                for (int it = 0; it < 3; it++)
                {
                    var fitted = Bolzena.RunUI.CardTerms.FitFor(descText, rawDesc, descW, fsNow);
                    if (fitted == descText.text && it > 0) break;
                    descText.text = fitted;
                    descText.ForceMeshUpdate();
                    if (Mathf.Abs(descText.fontSize - fsNow) < 0.001f) break;
                    fsNow = descText.fontSize;
                }
            }
            // 어둠 판 — 글 줄 수만큼(태그 줄까지 덮는다)
            descText.ForceMeshUpdate();
            float th = string.IsNullOrEmpty(descText.text) ? 0 : Mathf.Min(descText.rectTransform.sizeDelta.y, descText.GetRenderedValues(true).y);
            float bgTop = tags.Length > 0 ? -H / 2 + 0.98f + extra + blessH : descText.transform.localPosition.y + th / 2 + 0.06f;
            float bgBot = descText.transform.localPosition.y - th / 2 - 0.08f - blessH;
            descBg.enabled = th > 0 || hasBL;
            blessLineTx.enabled = blessLineIc.enabled = hasBL;
            if (hasBL)
            {
                blessLineTx.text = Tone.StripDiff(info.MarkBlessText);
                blessLineTx.rectTransform.sizeDelta = new Vector2(W - 0.52f, blessH);
                float ly = bgBot + 0.04f + blessH / 2 + 0.02f;
                blessLineTx.transform.localPosition = new Vector3(0.1f, ly, 0);
                blessLineIc.transform.localPosition = new Vector3(-W / 2 + 0.2f, ly, 0);
                Make.Fit(blessLineIc, new Vector2(0.15f, 0.15f));
            }
            CostTint = info.Cost > info.BaseCost && info.BaseCost >= 0 && info.CostUpWhy != null ? new Color(1f, 0.35f, 0.3f) : info.CostDown ? new Color(0.6f, 1f, 0.45f) : (Color?)null;
            descBg.transform.localPosition = new Vector3(0.025f, (bgTop + bgBot) / 2, 0);   // 왼쪽은 색 띠, 오른쪽은 테두리 바로 안까지
            Make.Fit(descBg, new Vector2(W - 0.09f, Mathf.Max(0.2f, bgTop - bgBot)));
            // 빛나는 카드(신탁 · 은총 대기)는 띠 글 없이 빛만(2026-10-07 사용자) — 받은 신탁의 이름 띠는 그대로
            epiText.text = "";   // 받은 신탁의 이름 띠도 뺀다(2026-10-07 사용자 — 장식 선의 별 줄 · 금 테로)
            PlaceMarks(info, decoY + 0.14f);
            PlaceCostMark(info);
            PlaceEmpower(info);
            bool epi = info.Epiphany;
            bool bl0 = !string.IsNullOrEmpty(info.MarkBless);
            rays.enabled = false;   // 손패의 신탁 카드는 은은한 테두리 빛(epiGlow) · 금 테만(띠 글은 뺌)(2026-10-07 사용자 「신탁 연출이 너무 화려」 — 빛줄기 뺌)
            bool blessGlow = !epi && bl0 && !Bolzena.RunUI.BlessFx.Old;   // 축복 받은 카드 — 연보라 은은한 빛(신탁만 받은 카드의 금빛과 구분)
            epiGlow.enabled = epi || blessGlow;
            epiGlow.color = blessGlow ? new Color(0.7f, 0.56f, 1f, 1f) : new Color(1f, 0.82f, 0.35f, 1f);
            ApplyVisibility();
        }

        /// <summary>신탁 별 줄 — 장식 선 가운데 큰 별(마름모 자리), 양옆 ±0.09 · ±0.17 W 에 작은 별. 받은 신탁이 없으면 끄고 마름모를 켠다.</summary>
        // 비용 바로 아래 마크(카제나 번뜩임) — 신탁 받은 카드 = 금빛 별 · 축복까지 받은 카드 = 신탁 마크 대신 연보라 날개 문장 · 없으면 없음
        void PlaceCostMark(CardInfo info)
        {
            bool bless = !string.IsNullOrEmpty(info.MarkBless) && !Bolzena.RunUI.BlessFx.Old;
            bool ora = oracleMark && !bless && !NoOracleMark;
            var at = new Vector3(-W / 2 + 0.29f, H / 2 - 0.1f - 0.27f - 0.43f, 0);
            decoDot.enabled = true;
            for (int i = 0; i < oraStars.Length; i++) oraStars[i].enabled = i == 0 && ora;
            oraStars[0].transform.localPosition = at; Make.Fit(oraStars[0], new Vector2(0.26f, 0.26f)); oraStars[0].color = new Color(0.35f, 0.2f, 0.02f);
            markBg.enabled = ora || bless;
            markBg.transform.localPosition = at;
            markBg.transform.localScale = Vector3.one; Make.Fit(markBg, new Vector2(0.4f, 0.4f));
            markBg.color = bless ? new Color(0.76f, 0.64f, 1f) : new Color(1f, 0.82f, 0.38f);
            blessWingL.enabled = false; blessWingR.enabled = bless;
            markBg.sprite = bless ? Res.UI("diamond") : Res.UI("circle");   // 축복 = 연보라 마름모 · 신탁 = 금빛 원
            Make.Fit(markBg, bless ? new Vector2(0.5f, 0.5f) : new Vector2(0.4f, 0.4f));
            if (bless)
            {   // 마름모 안의 흰 네 갈래 별(✦)
                blessWingR.sprite = Bolzena.RunUI.Theme.S("ic_spark"); blessWingR.flipX = false;
                blessWingR.transform.localEulerAngles = Vector3.zero; blessWingR.transform.localScale = Vector3.one;
                Make.Fit(blessWingR, new Vector2(0.24f, 0.24f));
                blessWingR.transform.localPosition = at;
                blessWingR.color = new Color(1f, 1f, 1f, blessWingR.color.a);
            }
        }

        // 다음 카드 강화 — 윗변에 걸친 배지(오른쪽 맞춤. 이름 · 종류 줄은 안 가린다)
        void PlaceEmpower(CardInfo info)
        {
            bool on = info.EmpowerMul > 0;
            empRim.enabled = empGlow.enabled = empBadgeBg.enabled = empBadgeIc.enabled = empBadgeTx.enabled = on;
            if (!on) return;
            empBadgeTx.text = "강화 ×" + (Mathf.Round(info.EmpowerMul * 100f) / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            empBadgeTx.ForceMeshUpdate();
            float tw = empBadgeTx.preferredWidth, bw = tw + 0.3f, cy = H / 2 - 0.01f, right = W / 2 - 0.1f;
            empBadgeBg.size = new Vector2(bw, 0.2f);
            empBadgeBg.transform.localPosition = new Vector3(right - bw / 2, cy, 0);
            empBadgeIc.transform.localPosition = new Vector3(right - bw + 0.12f, cy, 0);
            Make.Fit(empBadgeIc, new Vector2(0.13f, 0.13f));
            empBadgeTx.transform.localPosition = new Vector3(right - bw / 2 + 0.1f, cy - 0.005f, 0);
        }

        void PlaceOracleStars()
        {
            decoDot.enabled = !oracleMark;
            float[] xs = { 0, -0.09f * W, 0.09f * W, -0.17f * W, 0.17f * W };
            for (int i = 0; i < oraStars.Length; i++)
            {
                oraStars[i].enabled = oracleMark;
                oraStars[i].transform.localPosition = new Vector3(xs[i], DecoY, 0);
                Make.Fit(oraStars[i], i == 0 ? new Vector2(0.19f, 0.19f) : new Vector2(0.1f, 0.1f));
            }
        }

        // 표식 자리 — 신탁 띠(장식 선 바로 위, 아이콘 + 이름) · 그 위 축복 띠 · 오른쪽 위 셋째 줄 복제 표. 셋이 겹쳐도 읽히게 따로 선다.
        void PlaceMarks(CardInfo info, float y)
        {
            bool ora = !string.IsNullOrEmpty(epiText.text);
            oraRim.enabled = oracleMark;   // 받은 신탁 — 테 바깥 금빛(빛나는 카드는 제 빛 epiGlow 가 있다)
            epiBg.enabled = epiIcon.enabled = ora;
            if (ora)
            {
                epiText.ForceMeshUpdate();
                float tw = Mathf.Min(W - 0.5f, epiText.preferredWidth);
                epiText.transform.localPosition = new Vector3(0.09f, y, 0);
                epiBg.size = new Vector2(Mathf.Max(1.0f, tw + 0.42f), 0.22f);
                epiBg.transform.localPosition = new Vector3(0, y, 0);
                epiIcon.transform.localPosition = new Vector3(-epiBg.size.x / 2 + 0.13f, y, 0);
                Make.Fit(epiIcon, new Vector2(0.14f, 0.14f));
                y += 0.25f;
            }
            bool bl = !string.IsNullOrEmpty(info.MarkBless);
            // 축복 — 이름 띠 대신 장식 선 양 끝 금빛 날개(이름 · 효과는 키워드 상자에서). 띠는 -oldbless 점검에서만
            bool oldBand = bl && Bolzena.RunUI.BlessFx.Old;
            blessWingL.enabled = blessWingR.enabled = bl && !oldBand;
            if (bl && !oldBand)
            {
                float dy = y - 0.14f;   // 장식 선 높이(PlaceMarks 는 그 0.14 위에서 받는다)
                float half = deco.sprite != null ? deco.transform.localScale.x * deco.sprite.bounds.size.x / 2 : W * 0.275f;
                float ws = 0.3f / Bolzena.RunUI.BlessFx.Wing.bounds.size.x;   // 날개 너비 0.3
                blessWingR.transform.localScale = new Vector3(ws, ws, 1);
                blessWingL.transform.localScale = new Vector3(ws, ws, 1);
                blessWingR.transform.localPosition = new Vector3(half + 0.01f, dy - 0.02f, 0);
                blessWingL.transform.localPosition = new Vector3(-half - 0.01f, dy - 0.02f, 0);
            }
            blessBg.enabled = blessIcon.enabled = blessText.enabled = oldBand;
            if (oldBand)
            {
                blessText.text = info.MarkBless;
                blessText.ForceMeshUpdate();
                float tw = Mathf.Min(W - 0.5f, blessText.preferredWidth);
                blessText.transform.localPosition = new Vector3(0.09f, y, 0);
                blessBg.size = new Vector2(Mathf.Max(1.0f, tw + 0.42f), 0.22f);
                blessBg.transform.localPosition = new Vector3(0, y, 0);
                blessIcon.transform.localPosition = new Vector3(-blessBg.size.x / 2 + 0.13f, y, 0);
                Make.Fit(blessIcon, new Vector2(0.15f, 0.15f));
            }
            copyBg.enabled = copyIcon.enabled = info.Copy; copyText.enabled = false;   // 복제는 작은 아이콘 하나 — 문구는 풀이 상자로(2026-10-09)
            if (info.Copy)
            {
                copyBg.size = new Vector2(0.3f, 0.3f);
                copyBg.transform.localPosition = new Vector3(W / 2 - 0.26f, H / 2 - 0.62f, 0);
                copyIcon.transform.localPosition = copyBg.transform.localPosition;
                Make.Fit(copyIcon, new Vector2(0.2f, 0.2f));
            }
            if (false)
            {
                copyText.text = Bolzena.Core.CardMark.COPY_LINE;
                copyText.ForceMeshUpdate();
                float tw = Mathf.Min(W - 0.4f, copyText.preferredWidth);
                float cy = H / 2 - 0.82f, bw = tw + 0.3f;
                copyBg.size = new Vector2(bw, 0.2f);
                copyBg.transform.localPosition = new Vector3(W / 2 - 0.08f - bw / 2, cy, 0);
                copyIcon.transform.localPosition = new Vector3(W / 2 - 0.08f - bw + 0.12f, cy, 0);
                Make.Fit(copyIcon, new Vector2(0.14f, 0.14f));
                copyText.transform.localPosition = new Vector3(W / 2 - 0.08f - bw / 2 + 0.1f, cy, 0);
            }
        }

        // 그림 — Info.Art: "st:<그림 키>" 스탠딩 상반신 · "pc:<그림>" 고유 · 생성 카드 원작 그림(창 비율로 미리 자른 것) · "ic:<아이콘>" 고유 카드 아이콘 · 그 밖은 Resources/Art 의 그림 이름(옛 꼴)
        bool iconKind;
        // 그림 자리(2026-10-07 「카드 이미지가 너무 붕 뜬 것」) — 0 = 창을 덮는 한 장 · 1 = 사물 · SD(CardObj) · 2 = 스킬 아이콘 판
        int artMode;
        string artKey;
        Bolzena.RunUI.CardArt.ObjPic objPic;
        const float ArtTop = 56f / 276f;   // 위 글 · 칩 끝(그림 창 몫 — 판 화면 W.Card 와 같게)

        /// <summary>사물 · 아이콘 판 자리 — 위 글 · 칩 아래 ~ 장식 선(decoY) 위 칸을 채우고 아래를 장식 선에 붙인다(CardArt.Place — 판 W.Card 와 같은 규칙).</summary>
        /// <summary>점검용 — 장식 선 자리(그림 창 몫, 위에서) · 그림 내용 자리(창 몫 · 사물 · 아이콘 판만, 없으면 0 크기) · 늘리는 상한에 걸렸나.</summary>
        public float ArtBottom { get; private set; }
        public Rect ArtRect { get; private set; }
        public bool ArtCapped { get; private set; }

        void PlaceArt(float decoY)
        {
            float bottom = (ArtY + ArtH / 2 - decoY) / ArtH;
            ArtBottom = bottom; ArtRect = default; ArtCapped = false;
            if (artMode == 0) return;
            if (artMode == 1)
            {
                var cr = Bolzena.RunUI.CardArt.Place(objPic.Content.width, objPic.Content.height, objPic.Src.y, ArtW / ArtH, ArtTop, bottom, artKey, out bool cap);
                ArtRect = cr; ArtCapped = cap;
                var r = Bolzena.RunUI.CardArt.Full(objPic, cr);
                icon.transform.localPosition = new Vector3((r.center.x - 0.5f) * ArtW, ArtY + (0.5f - r.center.y) * ArtH, 0);
                Make.Fit(icon, new Vector2(r.width * ArtW, r.height * ArtH));
            }
            else
            {
                float srcH = icon.sprite != null ? icon.sprite.rect.height : 0;
                var r = Bolzena.RunUI.CardArt.Place(1, 1, srcH, ArtW / ArtH, ArtTop, bottom, artKey, out bool cap);
                ArtRect = r; ArtCapped = cap;
                float sz = r.height * ArtH;
                var c = new Vector3((r.center.x - 0.5f) * ArtW, ArtY + (0.5f - r.center.y) * ArtH, 0);
                iconPlate.transform.localPosition = icon.transform.localPosition = c;
                Make.Fit(iconPlate, new Vector2(sz, sz));
                Make.Fit(icon, new Vector2(sz - 0.07f, sz - 0.07f));
            }
        }
        void SetArt(CardInfo info, Color rc)
        {
            string a = info.Art;
            Sprite pic = null, ic = null, obj = null;
            artMode = 0; artKey = null;
            if (a != null && a.StartsWith("st:"))
            {
                var key = a.Substring(3);
                pic = Bolzena.RunUI.CardArt.Card(key) ?? Crop(Res.Sprite("Art/" + key), ArtW / ArtH, 0.58f);   // 판 화면 W.Card 와 같은 한 장(얼굴이 위 글 · 칩과 아래 효과 판 사이)
            }
            else if (a != null && a.StartsWith("pc:"))
            {
                // 장면(story · cg) = 창을 덮는 한 장 · 사물 · SD(CardObj) = 성격 바탕 + 알파 경계로 자른 그림(자리는 PlaceArt — 판 W.Card 와 같은 CardArt.Place)
                artKey = a.Substring(3);
                if (Bolzena.RunUI.CardArt.Obj(artKey, out objPic)) { artMode = 1; pic = Bolzena.RunUI.CardArt.Back(objPic.Nature); obj = objPic.Sprite; }
                else pic = Bolzena.RunUI.CardArt.Pic(artKey);
            }
            else if (a != null && a.StartsWith("ic:"))
            {
                var key = a.Substring(3);
                ic = Bolzena.RunUI.CardArt.Icon(key);
                pic = Bolzena.RunUI.CardArt.Blur(key);
                if (ic == null) { pic = Crop(Res.Sprite("Art/" + key), ArtW / ArtH, 0.5f); }
            }
            else if (!string.IsNullOrEmpty(a)) pic = Crop(Res.Sprite("Art/" + a), ArtW / ArtH, 0.58f);
            iconKind = ic != null;
            art.sprite = pic;
            art.transform.localPosition = new Vector3(0, ArtY, 0);
            if (pic != null) Make.Fit(art, new Vector2(ArtW, ArtH));
            icon.sprite = ic;
            icon.enabled = iconPlate.enabled = iconKind;
            if (iconKind) { Make.Fit(icon, new Vector2(IconS, IconS)); iconPlate.color = Color.Lerp(rc, Color.white, 0.25f); }
            if (iconKind && !Bolzena.RunUI.CardArt.OldFit) { artMode = 2; artKey = a.Substring(3); }
            if (obj != null) { icon.sprite = obj; icon.enabled = true; icon.transform.localPosition = new Vector3(0, IconY, 0); Make.Fit(icon, new Vector2(IconS, IconS)); }   // 교주 카드(원작 스펠 카드 그림)도 같은 꼴
        }

        void ApplyVisibility()
        {
            bool pinOn = ShowPin && pin.sprite != null;
            if (pin.enabled != pinOn) pin.enabled = pinRim.enabled = pinOn;
            nameText.rectTransform.sizeDelta = new Vector2(pinOn ? W - 0.73f : W - 47 * Px - 8 * Px, 25 * Px);   // 얼굴 배지가 없으면 이름을 오른쪽 끝까지
        }

        public static Color TypeColor(CardInfo info) =>
            info.Type == CardType.Attack ? new Color(1f, 0.55f, 0.58f) : info.Type == CardType.Power ? new Color(0.8f, 0.68f, 1f)
            : info.Type == CardType.Status ? new Color(0.7f, 0.7f, 0.78f) : new Color(0.38f, 0.9f, 0.82f);

        // 정사각 그림을 창 비율로 — 넘치는 쪽을 자르고, 세로는 centerY 에 치우쳐 자른다
        static readonly System.Collections.Generic.Dictionary<string, Sprite> crops = new System.Collections.Generic.Dictionary<string, Sprite>();
        // 자른 그림은 넘치면 비운다 — 화면에 있는 것은 렌더러가 쥐어 살고, 나머지는 다음 장면 정리 때 원본 그림과 함께 풀린다
        static void KeepCrops() { if (crops.Count >= 256) crops.Clear(); }
        public static Sprite Crop(Sprite icon, float ratio, float centerY)
        {
            if (icon == null) return null;
            string key = icon.name + "|" + ratio.ToString("F3") + "|" + centerY.ToString("F2");
            if (crops.TryGetValue(key, out var sp) && sp != null) return sp;
            KeepCrops();
            var tex = icon.texture;
            float w = tex.width, h = tex.width / ratio;
            if (h > tex.height) { h = tex.height; w = h * ratio; }
            float cy = Mathf.Clamp(tex.height * centerY, h / 2, tex.height - h / 2);
            sp = Sprite.Create(tex, new Rect((tex.width - w) / 2, cy - h / 2, w, h), new Vector2(0.5f, 0.5f), 100);
            crops[key] = sp;
            return sp;
        }

        // 사도 얼굴(SD 머리께) — 핀 · 초상 띠 · 고학년 띠가 같이 쓴다. ratio = 가로 / 세로
        public static Sprite Face(string key, float ratio = 1f)
        {
            var icon = Res.Sprite("Art/" + key);
            if (icon == null) return null;
            string ck = "face|" + key + "|" + ratio.ToString("F2");
            if (crops.TryGetValue(ck, out var sp) && sp != null) return sp;
            KeepCrops();
            var tex = icon.texture;
            float w = tex.width * (ratio >= 1.4f ? 0.84f : 0.6f), h = w / ratio;
            if (h > tex.height * 0.7f) { h = tex.height * 0.7f; w = h * ratio; }
            float cy = Mathf.Clamp(tex.height * 0.62f, h / 2, tex.height - h / 2);
            sp = Sprite.Create(tex, new Rect((tex.width - w) / 2, cy - h / 2, w, h), new Vector2(0.5f, 0.5f), 100);
            crops[ck] = sp;
            return sp;
        }

        public void SetOrder(int o)
        {
            order = o;
            rays.sortingOrder = o - 3;
            epiGlow.sortingOrder = o - 2;
            glow.sortingOrder = o - 1;
            rim.sortingOrder = o;
            body.sortingOrder = o + 1;
            art.sortingOrder = o + 2;
            iconPlate.sortingOrder = o + 3;
            icon.sortingOrder = o + 4;
            band.sortingOrder = o + 5;
            shadeT.sortingOrder = o + 5;
            shadeB.sortingOrder = o + 5;
            descBg.sortingOrder = o + 6;
            artMask.backSortingOrder = o + 1;
            artMask.frontSortingOrder = o + 6;   // 효과 글 어둠 판(o+6)까지 둥글게 오린다
            costText.sortingOrder = o + 7;
            nameText.sortingOrder = o + 7;
            typeIcon.sortingOrder = o + 7;
            typeBg.sortingOrder = o + 6;
            typeText.sortingOrder = o + 7;
            pinRim.sortingOrder = o + 7;
            pin.sortingOrder = o + 8;
            pinMask.backSortingOrder = o + 7;
            pinMask.frontSortingOrder = o + 8;
            deco.sortingOrder = o + 7;
            decoDot.sortingOrder = o + 7;
            foreach (var st in oraStars) st.sortingOrder = o + 9;
            tagText.sortingOrder = o + 7;
            descText.sortingOrder = o + 7;
            epiText.sortingOrder = o + 7;
            oraRim.sortingOrder = o - 1;
            empGlow.sortingOrder = o - 2; empRim.sortingOrder = o - 1;
            empBadgeBg.sortingOrder = o + 9; empBadgeIc.sortingOrder = o + 10; empBadgeTx.sortingOrder = o + 10;
            epiBg.sortingOrder = blessBg.sortingOrder = copyBg.sortingOrder = o + 6;
            epiIcon.sortingOrder = blessIcon.sortingOrder = copyIcon.sortingOrder = o + 7;
            blessText.sortingOrder = copyText.sortingOrder = o + 7;
            blessWingL.sortingOrder = blessWingR.sortingOrder = o + 9;
            markBg.sortingOrder = o + 8; blessLineIc.sortingOrder = o + 8; blessLineTx.sortingOrder = o + 8;
            flash.sortingOrder = o + 9;
        }

        public int Order => order;

        public void Snap()
        {
            transform.localPosition = TargetPos;
            transform.localRotation = Quaternion.Euler(0, 0, TargetRot);
            transform.localScale = Vector3.one * TargetScale;
            descA = ShowDesc ? 1 : 0;
        }

        // 하얗게 번쩍 — 신탁 변신 · 낼 때
        public IEnumerator FlashCo(float dur = 0.35f, float peak = 1f)
        {
            yield return Clock.Tween(dur, t => { if (flash) Make.Alpha(flash, peak * (1 - Ease.OutCubic(t))); }, true);
        }

        /// <summary>비용이 올랐을 때 — 짧은 빨간 깜빡임 + 「비용 +n」 띄움 글.</summary>
        public void CostUpPop(int by)
        {
            Clock.Run(CostUpCo(by));
        }
        IEnumerator CostUpCo(int by)
        {
            var old = flash.color;
            flash.color = new Color(1f, 0.2f, 0.15f, 0);
            var tx = Make.Text("costup", transform, "비용 +" + by, new Vector3(0, H / 2 - 0.1f, 0), 0.3f, order + 20, new Color(1f, 0.4f, 0.35f));
            Make.Outline(tx, 0.3f, Color.black);
            yield return Clock.Tween(0.7f, t =>
            {
                if (flash) Make.Alpha(flash, 0.55f * (1 - t) * (0.5f + 0.5f * Mathf.Sin(t * 14)));
                if (tx) { tx.transform.localPosition = new Vector3(0, H / 2 - 0.1f + 0.5f * Ease.OutCubic(t), 0); tx.alpha = 1 - Mathf.Clamp01((t - 0.6f) * 2.5f); }
            }, true);
            if (tx) Destroy(tx.gameObject);
            if (flash) { flash.color = old; Make.Alpha(flash, 0); }
        }

        public void SetFlash(float a) => Make.Alpha(flash, a);

        public void SetAlpha(float a)
        {
            alphaMul = a;
            raysMat.SetFloat("_Alpha", a);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1 - Mathf.Exp(-dt * Follow);
            transform.localPosition = Vector3.Lerp(transform.localPosition, TargetPos, k);
            float rot = Mathf.LerpAngle(transform.localEulerAngles.z, TargetRot, k);
            transform.localRotation = Quaternion.Euler(0, 0, rot);
            float s = Mathf.Lerp(transform.localScale.x, TargetScale, k);
            transform.localScale = new Vector3(s, s, 1);
            ApplyVisibility();

            // 낼 수 없으면 어둡게 · 효과 글은 보일 때만 스르르
            dim = Mathf.MoveTowards(dim, (Playable ? 1f : 0.45f) * Shade, dt * 4f);
            descA = Mathf.MoveTowards(descA, ShowDesc ? 1f : 0f, dt * 8f);
            float a = alphaMul;
            var rc = RimColor(Info);
            rim.color = new Color(rc.r * dim, rc.g * dim, rc.b * dim, a);
            Make.Alpha(body, a);
            var dc = new Color(dim, dim, dim, a);
            float bgk = iconKind ? 0.62f * dim : dim;   // 고유 카드의 흐린 바탕은 눌러 아이콘이 서게
            art.color = new Color(bgk, bgk, bgk, a);
            icon.color = dc;
            var pc = iconPlate.color; pc.a = 0.9f * a; iconPlate.color = pc;
            pin.color = dc;
            var tc = Info != null ? TypeColor(Info) : Color.white;   // 유형 글과 같은 색(공격 빨강 · 스킬 청록) — 판 화면 W.TypeColor 처럼
            typeIcon.color = new Color(tc.r * dim, tc.g * dim, tc.b * dim, a);
            var bc = band.color; bc.a = a; band.color = bc;
            Make.Alpha(shadeT, 0.9f * a);
            Make.Alpha(shadeB, (0.35f + 0.63f * descA) * a);
            Make.Alpha(pinRim, a);
            Make.Alpha(typeBg, 0.7f * a);
            costText.alpha = nameText.alpha = typeText.alpha = epiText.alpha = a;
            blessText.alpha = copyText.alpha = a;
            Make.Alpha(oraRim, 0.95f * a); Make.Alpha(epiBg, 0.95f * a); Make.Alpha(blessBg, 0.95f * a); Make.Alpha(copyBg, 0.88f * a);
            Make.Alpha(epiIcon, a); Make.Alpha(blessIcon, a); Make.Alpha(copyIcon, a);
            if (empRim.enabled)
            {
                Make.Alpha(empRim, 0.95f * a); Make.Alpha(empBadgeBg, 0.96f * a); Make.Alpha(empBadgeIc, a); empBadgeTx.alpha = a;
                bool calmE = Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;
                empT += dt;
                var egc = empGlow.color; egc.a = (calmE ? 0.38f : 0.34f + 0.1f * Mathf.Sin(empT * 2.6f)) * a; empGlow.color = egc;
            }
            Make.Alpha(blessWingL, a); Make.Alpha(blessWingR, a); Make.Alpha(markBg, a); Make.Alpha(blessLineIc, descA * a); blessLineTx.alpha = descA * a;
            costText.color = CostTint.HasValue ? new Color(CostTint.Value.r, CostTint.Value.g, CostTint.Value.b, a) : Playable ? new Color(1, 1, 1, a) : new Color(1f, 0.6f, 0.6f, a);
            tagText.alpha = descText.alpha = descA * a;
            Make.Alpha(deco, 0.75f * descA * a);
            Make.Alpha(decoDot, descA * a);
            foreach (var st in oraStars) Make.Alpha(st, Mathf.Max(descA, 0.85f) * a);   // 별은 효과 글을 접은 손패에서도 보이게
            Make.Alpha(descBg, 0.78f * descA * a);

            var gc = glow.color;
            gc.a = Mathf.MoveTowards(gc.a, Hovered && Playable ? 0.9f : 0f, dt * 6f);
            glow.color = gc;

            if (!Info.Epiphany && epiGlow.enabled)
            {
                epiT += dt;
                bool calm0 = Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;
                var bc0 = epiGlow.color; bc0.a = (calm0 ? 0.4f : 0.45f + 0.1f * Mathf.Sin(epiT * 2f)) * alphaMul; epiGlow.color = bc0;
            }
            if (Info.Epiphany)
            {
                // 은은한 테두리 빛 — 천천히 옅게 숨 쉰다. 저사양 · 움직임 줄이기면 숨도 불티도 없이 더 옅게
                bool calm = Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;
                epiT += dt;
                float pulse = calm ? 0.45f : 0.5f + 0.12f * Mathf.Sin(epiT * 2.2f);
                var ec = epiGlow.color; ec.a = pulse * alphaMul; epiGlow.color = ec;
                sparkT -= dt;
                if (!calm && sparkT <= 0)
                {
                    sparkT = 0.45f;
                    var edge = new Vector3(Random.Range(-W / 2, W / 2), Random.Range(-H / 2, H / 2), 0);
                    if (Random.value < 0.5f) edge.x = Mathf.Sign(edge.x) * W / 2; else edge.y = Mathf.Sign(edge.y) * H / 2;
                    Vfx.Burst(edge, new Vfx.BurstOpt
                    {
                        Tex = "FX_UI_star_02", Count = 1, Speed = new Vector2(0.3f, 0.9f), Angle = 90, Spread = 50, Life = new Vector2(0.6f, 1.0f),
                        Size = new Vector2(0.1f, 0.2f), C0 = new Color(1f, 0.9f, 0.5f), C1 = Color.white, Order = order + 9, Boost = 1.8f, Parent = transform,
                        ShrinkTo = 0f,
                    });
                }
            }
        }
    }
}
