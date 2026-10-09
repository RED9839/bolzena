using System;
using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bolzena.UI
{
    // 손패 — 부채꼴로 펼치고, 올리면 들어 올리고(풀이 툴팁), 내는 길은 셋:
    //   ① 끌어서 놓기 — 적을 고르는 카드는 화살표로 대상을 짚고 놓는다
    //   ② 눌러서 고르고 → 대상(적)을 누르거나, 대상 없는 카드는 싸움터를 누르거나 다시 그 카드를 누른다
    //   ③ 터치 — 첫 탭은 고르기(+ 미리보기), 적을 처음 누르면 그 적을 겨눠 미리보기, 같은 적을 한 번 더 누르면 낸다
    //   오른쪽 클릭 · Esc — 내려놓기. 단축키 1~9 고르기 · ←→ 대상 · Space/Enter 내기
    //   길게 누르기(0.3초 · 마우스 · 터치 같게) — 그 카드를 화면 가운데 위에 손패의 약 2.5배로 + 오른쪽 키워드 판(넘치면 왼쪽).
    //     손패의 그 자리는 흐리게. 그 자리에서 떼면 닫기(내지 않음), 누른 채 끌면 확대를 닫고 바로 끌어 내기로 이어진다
    //   카드 확대(상세 보기)는 언제나 화면 가운데 위 — 고른 카드는 확대하지 않는다(손패에서 들어 올린 카드 글로 본다 · 적을 가리지 않게)
    public class HandView : MonoBehaviour
    {
        public readonly List<CardView> Cards = new List<CardView>();
        public bool Interactive;
        public bool Hidden;                           // 전투가 끝나면 손패를 아래로 내린다
        public Func<Vector2, int> EnemyAt;            // 월드 좌표 → 적 번호(-1)
        public Func<Vector2, int> EnemyNear;          // 상자 밖이면 가까운 적(-1 = 너무 멂)
        public Func<int, Vector3> EnemyAim;           // 적 번호 → 화살표가 가리킬 월드 좌표
        public Func<int, int, int> NextEnemy;         // (지금, 방향) → 다음 산 적
        public Func<CardInfo, bool> CanPlay;
        public Func<int, string> WhyNot;              // 못 내는 까닭
        public (int hand, int target)? Request;       // 낸 카드 — 연출 쪽이 가져간다
        public Vector3 DeckPos = new Vector3(-7.35f, -0.3f, 0), DiscardPos = new Vector3(7.35f, -0.3f, 0);
        int hover = -1, drag = -1, sel = -1, aimTarget = -1, press = -1;
        float hoverT;
        // 길게 누르기 확대(카제나식) — 손패 카드를 누른 채 HoldZoomT 초 움직이지 않으면 그 카드를 화면 가운데 위에 크게(키워드 판은 오른쪽).
        //   떼면 접힌다(이 누름은 탭으로 치지 않는다). 누른 채 PointerInput 끌기 거리(0.18) 넘게 움직이면 접고 지금처럼 끌어 낸다.
        public const float HoldZoomT = 0.3f;
        float pressT;
        bool holdZoom;
        bool hoverMute;                                // 길게 눌러 본 뒤 뗐다 — 마우스가 그 카드 위에 그대로 있어도 올려 두기 확대를 다시 열지 않는다(떼면 닫힘)
        /// <summary>점검 — 마지막 탭이 무엇을 했나.</summary>
        public string LastTap { get; private set; }
        /// <summary>지금 길게 누르기로 카드를 크게 띄우고 있나(자동 데모 · 점검).</summary>
        public bool HoldZooming => holdZoom && press >= 0 && PointerInput.Held;
        Vector2 dragOffset;
        TargetArrow arrow;
        SpriteRenderer reticle;
        // 끌기 꾸밈(카제나식) — 대상 발밑 타원 고리(적 붉게 · 아군 초록 파랑)
        public Func<int, (Vector3 at, float w)?> EnemyFoot, AllyFoot;   // 번호 → 발 자리(월드) · 몸 폭. 없거나 쓰러졌으면 null
        public Func<Vector2, int> AllyAt;                               // 월드 좌표 → 사도 번호(-1)
        public Func<int> EnemyCount, AllyCount;
        readonly List<SpriteRenderer> rings = new List<SpriteRenderer>();
        int ringsUsed;
        int allyTarget = -1;
        /// <summary>점검 — 지금 끄는 카드가 겨눈 사도(-1) · 띄운 고리 수 · 뽑기 연출이 도는 중인가.</summary>
        public int AllyAim => allyTarget;
        public int RingCount => ringsUsed;
        public bool Pulling => pullT >= 0;
        // 뽑기 연출 — 고르거나 끌기 시작한 순간 0.22초: 튀어 오름(작은 넘침) · 살짝 기울었다 섬 · 테두리 빛(카드 성격 빛) · 뒤 빛 번짐.
        //   그동안과 그 뒤로도 고른 · 끄는 카드 밖의 손패는 살짝 어둡게. 2× 배속 · 「움직임 줄이기」 는 약하게
        CardView pulled;
        float pullT = -1, pullAmp = 1;
        SpriteRenderer pullGlow;
        const float PullDur = 0.22f;
        const string PullSfx = "card_pull";            // 효과음 자리 — Resources/Sfx/card_pull 이 생기면 그것, 없으면 card_hover 를 높게
        const float LiftY = -1.3f;                    // 이 위로 끌어 올리면 「낸다」(손 위 싸움터)

        // 미리보기 · 툴팁이 읽는 지금 손의 모습
        public int Held => drag >= 0 ? drag : sel;
        public int Aim => aimTarget;
        public bool Lifted { get; private set; }
        public int Hover => hover;

        public static HandView Create(Transform parent)
        {
            var root = Make.Node("Hand", parent);
            var h = root.gameObject.AddComponent<HandView>();
            h.arrow = TargetArrow.Create(root);
            h.reticle = Make.Box("reticle", root, ScopeSprite(), Vector3.zero, new Vector2(1.3f, 1.3f), 690, new Color(1f, 0.25f, 0.2f, 0.95f), Res.SpriteMat(false, 1.6f));
            h.reticle.enabled = false;
            return h;
        }

        public CardView Add(CardInfo info, bool fromDeck = true)
        {
            var c = CardView.Create(transform, info);
            c.ShowPin = false; c.ShowDesc = false;
            if (fromDeck)
            {
                c.transform.localPosition = DeckPos;
                c.transform.localScale = Vector3.one * 0.3f;
                c.transform.localRotation = Quaternion.Euler(0, 0, 30);
            }
            Cards.Add(c);
            Layout();
            return c;
        }

        public void Remove(CardView c)
        {
            int i = Cards.IndexOf(c);
            Cards.Remove(c);
            if (i >= 0)
            {
                if (sel == i) sel = -1; else if (sel > i) sel--;
                if (drag == i) drag = -1; else if (drag > i) drag--;
                if (press == i) { press = -1; holdZoom = false; } else if (press > i) press--;
            }
            if (hover >= Cards.Count) hover = -1;
            Layout();
        }

        public CardView Find(string id) => Cards.Find(v => v.Info.Id == id);

        public int IndexOf(CardView c) => Cards.IndexOf(c);

        // 손의 카드 모습을 새로(코스트 · 신탁이 바뀌었을 때) — 차례는 규칙의 손과 같다
        public void Sync(List<CardInfo> hand)
        {
            // 화면의 손과 규칙의 손이 어긋났으면(쪽지로 그리지 않은 움직임 — 손 → 뽑을 더미 · 예약 · 카드가 바뀜 …) 규칙 쪽에 맞춘다.
            // 어긋난 채 두면 화면 번호로 낸 요청이 규칙의 다른 칸을 가리켜 「카드가 안 나간다」(CanPlay 실패 → 조용히 돌아옴)
            bool same = Cards.Count == hand.Count;
            for (int i = 0; same && i < hand.Count; i++) same = Cards[i].Info.Id == hand[i].Id;
            if (!same && drag < 0)
            {
                Debug.LogWarning($"[Hand] 규칙의 손과 어긋남 — 맞춥니다. 화면 [{string.Join(", ", Cards.ConvertAll(c => c.Info.Id))}] · 규칙 [{string.Join(", ", hand.ConvertAll(c => c.Id))}]");
                var pool = new List<CardView>(Cards);
                var next = new List<CardView>();
                foreach (var info in hand)
                {
                    var v = pool.Find(c => c.Info.Id == info.Id);
                    if (v != null) pool.Remove(v);
                    else { v = CardView.Create(transform, info); v.ShowPin = false; v.ShowDesc = false; }
                    next.Add(v);
                }
                foreach (var gone in pool) if (gone) Destroy(gone.gameObject);
                Cards.Clear();
                Cards.AddRange(next);
                sel = -1; press = -1; hover = -1;
                Layout();
            }
            for (int i = 0; i < Cards.Count && i < hand.Count; i++)
            {
                if (Cards[i].Info.Id != hand[i].Id) continue;
                bool changed = Cards[i].Info.Cost != hand[i].Cost || Cards[i].Info.Epiphany != hand[i].Epiphany || Cards[i].Info.Text != hand[i].Text || Cards[i].Info.EmpowerMul != hand[i].EmpowerMul;
                int oldCost = Cards[i].Info.Cost;
                Cards[i].Info = hand[i];
                if (changed) Cards[i].Refresh();
                if (hand[i].CostUpWhy != null && hand[i].Cost > oldCost && hand[i].Cost > hand[i].BaseCost) Cards[i].CostUpPop(hand[i].Cost - oldCost);
            }
        }

        // 부채꼴 자리 — 카드끼리 겹치지 않게(이름 · 설명이 다 읽히게) 왼쪽 AP 와 오른쪽 턴 종료 사이에 나란히.
        // 장수가 많아 자리가 모자라면 먼저 조금 줄이고(MinScale), 그래도 모자라면 그때만 살짝 겹친다.
        // 올린 카드는 곧게 크게(아래 끝은 그대로 — 위로 자란다), 이웃은 그만큼 비켜선다.
        public float HalfSpan = 4.55f;                // 손이 쓸 가로 반폭(화면이 넓으면 감독 · HUD 가 넓힌다)
        public static float Bottom => -Tone.HalfH + 0.5f * Tone.K;   // 카드 아래 끝 — 그 밑 가운데에 AP 숫자 · 손패 수
        const float Gap = 0.07f, MinScale = 0.8f;
        public float HandScale { get; private set; } = 0.9f;

        public void Layout()
        {
            int n = Cards.Count;
            float wide = Mathf.Clamp01((HalfSpan - 4.55f) / 1.7f);
            float maxS = Mathf.Lerp(0.92f, 1.04f, wide) * (Tone.Compact ? 1.1f : 1f);   // 폰 — 카드 글이 읽히게 조금 더 크게
            float span = HalfSpan * 2;
            float s = n > 0 ? Mathf.Clamp((span - Gap * (n - 1)) / (n * CardView.W), MinScale, maxS) : maxS;
            HandScale = s;
            float cw = CardView.W * s;
            // 카드는 살짝 겹친 부채꼴(카제나처럼 — 손패에서는 그림 · 이름만, 효과 글은 올리면)
            // 끄는 카드는 손패에서 뽑혀 나왔다 — 남은 카드끼리 빈자리를 메워 좁혀 앉는다(크기는 그대로). 취소하면 다시 벌어져 제자리로
            int m = drag >= 0 && drag < n ? n - 1 : n;
            float spacing = m > 1 ? Mathf.Min(cw * 0.88f, (span - cw) / (m - 1)) : 0;
            int up = drag >= 0 ? -1 : sel >= 0 ? sel : hover;
            int ghost = HoldZooming ? press : -1;     // 길게 눌러 크게 본 카드 — 손패 자리는 흐리게 두고 들어 올리지 않는다
            if (ghost >= 0 && up == ghost) up = -1;
            DimSlot(ghost);
            float upS = Mathf.Max(s * 1.34f, 1.18f);
            float upPush = up >= 0 ? Mathf.Max(0, (CardView.W * upS + cw) / 2 + Gap - spacing) : 0;   // 올린 카드와 이웃이 안 겹칠 만큼
            for (int i = 0; i < n; i++)
            {
                var c = Cards[i];
                if (i == drag) continue;                  // 뽑혀 나온 카드 — 자리(SlotX)는 뽑기 전 것을 둔다
                float off = (drag >= 0 && i > drag ? i - 1 : i) - (m - 1) / 2f;
                c.SlotX = off * spacing;
                float push = 0;
                if (up >= 0 && i != up) push = (i < up ? -1 : 1) * upPush;
                float x = off * spacing + push;
                float sc = s;
                float y = Bottom + CardView.H * sc / 2 - off * off * 0.018f;
                float rot = -off * 1.1f;
                int order = 500 + i * 10 + (c.Info.Epiphany ? 60 : 0);   // 신탁 카드는 빛이 이웃 위로 번지게
                if (i == up)
                {
                    sc = i == sel ? upS * 1.04f : upS;
                    y = Bottom + 0.08f + CardView.H * sc / 2 + (i == sel ? 0.12f : 0);
                    rot = 0;
                    order = 620;
                    // 화면 밖으로 안 나가게
                    float half = CardView.W * sc / 2;
                    x = Mathf.Clamp(off * spacing, -HalfSpan - 0.1f + half, HalfSpan + 0.6f - half);
                }
                if (c.Info.Epiphany && i != up) y += 0.08f + (Bolzena.RunUI.Settings.ReduceMotion ? 0f : 0.02f * Mathf.Sin(Clock.Now * 2f));   // 신탁 카드는 살짝 떠서 숨 쉰다(2026-10-07 — 은은하게, 움직임 줄이기면 멈춤)
                if (Hidden) y -= 4.5f;
                c.TargetPos = new Vector3(x, y, 0);
                c.TargetRot = rot;
                c.TargetScale = sc;
                c.Hovered = i == up;
                c.Shade = (drag >= 0 || sel >= 0) && i != sel ? 0.68f : 1f;   // 고른 · 끄는 카드 밖의 손패는 살짝 어둡게
                c.ShowDesc = i == up;
                if (c.Order != order) c.SetOrder(order);
                c.Playable = (CanPlay == null || CanPlay(c.Info)) && !c.Info.Unplayable;
            }
            if (drag >= 0 && drag < n) Cards[drag].ShowDesc = true;
            Pins(up);
        }

        // 길게 누르기 확대 동안 그 카드의 손패 자리를 흐리게(카제나 — 카드가 위로 「떠나간」 자리). 바뀔 때만 만진다(다른 연출의 SetAlpha 를 덮지 않게)
        CardView dimmed;
        void DimSlot(int i)
        {
            var c = i >= 0 && i < Cards.Count ? Cards[i] : null;
            if (c == dimmed) return;
            if (dimmed) dimmed.SetAlpha(1f);
            dimmed = c;
            if (c) c.SetAlpha(0.3f);
        }

        // 사도 핀 — 같은 사도 카드가 이어지면 그 묶음 첫 카드 위에 핀 하나(핀 빛 = 사도 빛)
        readonly List<Transform> pins = new List<Transform>();
        readonly List<string> pinKeys = new List<string>();
        void Pins(int up)
        {
            int used = 0;
            for (int i = 0; i < Cards.Count; i++)
            {
                var c = Cards[i];
                if (i == drag) continue;
                int hero = c.Info.Hero >= 0 ? c.Info.Hero : c.Info.Owner;   // 교주 카드는 넣은 사도의 핀
                if (hero < 0) continue;
                int prev = i - 1;
                if (prev == drag) prev--;
                if (prev >= 0 && (Cards[prev].Info.Hero >= 0 ? Cards[prev].Info.Hero : Cards[prev].Info.Owner) == hero && i != up && prev != up) continue;
                var hs = CardView.HeroOf != null ? CardView.HeroOf(hero) : null;
                if (hs == null) continue;
                if (used >= pins.Count) { pins.Add(null); pinKeys.Add(null); }
                if (pins[used] == null || pinKeys[used] != hs.Key)
                {
                    if (pins[used] != null) Destroy(pins[used].gameObject);
                    pins[used] = MakePin(hs, used);
                    pinKeys[used] = hs.Key;
                }
                var p = pins[used];
                float sc = c.TargetScale;
                var at = c.TargetPos + Quaternion.Euler(0, 0, c.TargetRot) * new Vector3(-CardView.W * sc * 0.3f, CardView.H * sc / 2 + 0.26f, 0);
                p.gameObject.SetActive(!Hidden);
                p.localPosition = Vector3.Lerp(p.localPosition == Vector3.zero ? at : p.localPosition, at, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
                used++;
            }
            for (int i = used; i < pins.Count; i++) if (pins[i] != null) pins[i].gameObject.SetActive(false);
        }

        Transform MakePin(HeroState h, int slot)
        {
            var t = Make.Node("pin_" + h.Key, transform);
            int o = 640 + slot * 4;
            Make.Box("tail", t, Res.UI("diamond"), new Vector3(0, -0.2f, 0), new Vector2(0.18f, 0.22f), o, Color.Lerp(h.Tint, Color.white, 0.25f));
            Make.Box("rim", t, Res.UI("circle"), Vector3.zero, new Vector2(0.44f, 0.44f), o, Color.Lerp(h.Tint, Color.white, 0.25f));
            Make.Box("well", t, Res.UI("circle"), Vector3.zero, new Vector2(0.38f, 0.38f), o + 1, Tone.Well);
            Tone.RoundFace("face", t, h.Key, Vector3.zero, 0.37f, o + 2);
            return t;
        }

        bool Inside(CardView c, Vector2 p)
        {
            var local = c.transform.InverseTransformPoint(p);
            return Mathf.Abs(local.x) < CardView.W / 2 && Mathf.Abs(local.y) < CardView.H / 2;
        }

        int CardAt(Vector2 p)
        {
            int best = -1;
            for (int i = 0; i < Cards.Count; i++)
                if (Inside(Cards[i], p) && (best < 0 || Cards[i].Order > Cards[best].Order)) best = i;
            return best;
        }

        bool NeedsEnemy(int i) => i >= 0 && i < Cards.Count && Cards[i].Info.Target == TargetKind.Enemy;

        public void Cancel()
        {
            if (sel >= 0 || drag >= 0) Sfx.Play("card_hover", 0.25f, 0.8f);
            sel = -1;
            press = -1;
            holdZoom = false;
            aimTarget = -1;
            CancelDrag();
        }

        public bool HasSelection => sel >= 0 || drag >= 0;

        // 고르기(단축키 · 탭) — 못 내는 카드면 까닭을 띄운다
        public void Select(int i)
        {
            if (i < 0 || i >= Cards.Count) return;
            if (sel == i) { Cancel(); return; }
            var c = Cards[i];
            if (!c.Playable) { Cant(i); return; }
            sel = i;
            aimTarget = NeedsEnemy(i) && NextEnemy != null ? NextEnemy(-1, 1) : -1;
            Layout();
            PullFx(c);
        }

        void Cant(int i)
        {
            Sfx.Play("card_cant", 0.5f);
            var why = WhyNot?.Invoke(i);
            if (!string.IsNullOrEmpty(why))
                Vfx.Word(Cards[i].transform.position + new Vector3(0, 1.7f, 0), why, 0.26f, new Color(1f, 0.75f, 0.7f), new Color(0.2f, 0, 0), 1.1f, 1f, transform.parent, 700, 0.3f);
        }

        // 대상 카드를 적 밖에 놓았다 — 조용히 돌아가지 않고 알린다
        void Miss(Vector3 at)
        {
            Sfx.Play("card_cant", 0.5f);
            Vfx.Word(at + new Vector3(0, 0.6f, 0), "적에게 놓으세요", 0.26f, new Color(1f, 0.85f, 0.6f), new Color(0.2f, 0.05f, 0), 1.0f, 1f, transform.parent, 700, 0.3f);
        }

        // 겨눈 적 — 상자 안이면 그 적, 싸움터 위(손 위)에서 상자 밖이면 가장 가까운 적
        int AimAt(Vector2 p)
        {
            int e = EnemyAt != null ? EnemyAt(p) : -1;
            if (e < 0 && p.y > LiftY && EnemyNear != null && !BattleDirector.OldEnemyBox) e = EnemyNear(p);
            return e;
        }

        void Confirm(int i, int target)
        {
            if (NeedsEnemy(i) && target < 0) return;
            sel = -1; press = -1;
            aimTarget = -1;
            arrow.Hide();
            reticle.enabled = false;
            Lifted = false;
            allyTarget = -1;
            Tooltip.I?.Unpin();
            Request = (i, target);
        }

        void Update()
        {
            Layout();
            if (!Interactive || Modal.Open != null)
            {
                if (!Interactive) { if (drag >= 0) CancelDrag(); sel = -1; press = -1; holdZoom = false; }
                if (hover >= 0) { hover = -1; Layout(); }
                arrow.Hide(); reticle.enabled = false; Lifted = false;
                RingsBegin(); RingsEnd();
                Tooltip.I?.Unpin();
                if (!PileUi.Open) CardZoom.Hide();
                return;
            }
            var p = PointerInput.Pos;
            if (PointerInput.RightDown && HasSelection) { Cancel(); return; }
            Keys();

            if (drag < 0)
            {
                int h = CardAt(p);
                // 올린 카드는 원래 자리 둘레도 올린 채로 둔다(떨리지 않게)
                if (h < 0 && hover >= 0 && hover < Cards.Count && p.y < LiftY && Mathf.Abs(p.x - Cards[hover].TargetPos.x) < 0.8f) h = hover;
                // 터치에는 올려 두기가 없다(뗀 그 프레임까지는 둔다 — 카드 위 뗌이 싸움터 탭으로 새지 않게)
                if (PointerInput.Touch && !PointerInput.Held && !PointerInput.Up) h = -1;
                if (h != hover)
                {
                    hover = h;
                    hoverT = 0;
                    hoverMute = false;
                    if (h >= 0 && !PointerInput.Touch) Sfx.Play("card_hover", 0.35f);
                    Layout();
                }
                hoverT += Time.unscaledDeltaTime;
                if (PointerInput.Down) { press = CardAt(p); pressT = 0; holdZoom = false; }
                // 누른 채 가만히 — 길게 누르기 확대
                if (PointerInput.Held && press >= 0 && !PointerInput.Moved)
                {
                    pressT += Time.unscaledDeltaTime;
                    if (!holdZoom && pressT >= HoldZoomT) { holdZoom = true; Sfx.Play("card_hover", 0.4f, 1.1f); }
                }
                // 누른 채 움직이면 끌기(확대는 접는다)
                if (PointerInput.Held && press >= 0 && PointerInput.Moved && press < Cards.Count)
                {
                    var c = Cards[press];
                    holdZoom = false;
                    if (!c.Playable) { Cant(press); press = -1; }
                    else
                    {
                        drag = press;
                        sel = -1;
                        dragOffset = (Vector2)c.transform.localPosition - p;
                        c.SetOrder(650);
                        PullFx(c);
                    }
                }
                // 길게 눌러 크게 본 뒤 뗌은 탭이 아니다(고르기 · 내기 없이 원래대로)
                if (drag < 0 && PointerInput.Tap && !holdZoom) OnTap(p);
                if (PointerInput.Up) { if (holdZoom) { hoverMute = true; hoverT = 0; } press = -1; holdZoom = false; }
            }

            RingsBegin();
            if (drag >= 0) DragUpdate(p);
            else if (sel >= 0) SelUpdate(p);
            else { arrow.Hide(); reticle.enabled = false; Lifted = false; }
            RingsEnd();
            PullUpdate();

            // 카드 확대(모두 CardZoom 한 판 · 언제나 화면 가운데 위 · 키워드 판은 카드 오른쪽, 넘치면 왼쪽) —
            //   길게 누른 카드(마우스 · 터치 같게), 마우스를 잠깐 올려 둔 카드(PC). 고른 카드 · 끄는 카드는 확대하지 않는다(적 · 화살표를 가리지 않게)
            int holdCard = HoldZooming && press < Cards.Count ? press : -1;
            int tipCard = drag >= 0 ? -1 : holdCard >= 0 ? holdCard : -1;   // 올려 두기만으로는 확대 · 낱말 판을 띄우지 않는다(2026-10-09 사용자 — 길게 누를 때만)
            if (tipCard >= 0 && tipCard < Cards.Count)
            {
                var c = Cards[tipCard];
                string why = !c.Playable ? WhyNot?.Invoke(tipCard) : null;
                CardZoom.Show(transform.parent, c.Info, ZoomAt(out float s), s, false, why);
                Tooltip.I?.Unpin();
            }
            else CardZoom.Hide();
        }

        /// <summary>손패 확대 자리 — 화면 가운데(x = 0 · 카메라 가운데) 위쪽, 크기는 손패 카드의 약 2.5배(화면 높이에 맞춰 줄인다).</summary>
        public Vector3 ZoomAt(out float s)
        {
            // 카제나처럼 손패 바로 위에 — 카드 아래 끝은 손패 카드 위쪽 절반께(손패 위에 살짝 겹친다), 위 끝은 위 HUD(파티 체력 줄) 아래.
            //   둘 사이에 들도록 크기를 줄인다(그래서 손패의 약 2.2~2.5배)
            float bottom = Bottom + CardView.H * HandScale * 0.45f;
            float top = Tone.HalfH - 1.0f * Tone.K;
            s = Mathf.Min(HandScale * 2.5f, (top - bottom) / CardView.H);
            var cam = Camera.main;
            float cx = cam != null && transform.parent != null ? transform.parent.InverseTransformPoint(cam.transform.position).x : 0;
            return new Vector3(cx, bottom + CardView.H * s / 2, 0);
        }

        void OnTap(Vector2 p)
        {
            int at = CardAt(p);
            LastTap = $"탭 카드 {at} · 고른 {sel} · y {p.y:F2}";
            if (sel >= 0)
            {
                if (at == sel)
                {
                    // 다시 누름 — 대상 없는 카드는 낸다. 적 카드는 터치로 겨눈 적이 있으면 그 적에게, 아니면 내려놓기
                    if (!NeedsEnemy(sel)) Confirm(sel, -1);
                    else if (aimTarget >= 0 && PointerInput.Touch) Confirm(sel, aimTarget);
                    else Cancel();
                    return;
                }
                if (at >= 0) { Select(at); return; }
                if (NeedsEnemy(sel))
                {
                    int e = AimAt(p);
                    if (e < 0) { if (p.y < LiftY) Cancel(); else Miss(p); return; }
                    if (PointerInput.Touch && aimTarget != e) { aimTarget = e; Sfx.Play("card_hover", 0.4f, 1.3f); return; }   // 첫 탭 — 겨누기(미리보기)
                    Confirm(sel, e);
                    return;
                }
                if (p.y > LiftY) Confirm(sel, -1);
                else Cancel();
                return;
            }
            if (at >= 0) Select(at);
        }

        void Keys()
        {
            for (int k = 0; k < 9; k++)
                if (PointerInput.Key(Key.Digit1 + k) || PointerInput.Key(Key.Numpad1 + k)) { if (k < Cards.Count) Select(k); return; }
            if (sel >= 0 && NeedsEnemy(sel) && NextEnemy != null)
            {
                if (PointerInput.Key(Key.RightArrow)) aimTarget = NextEnemy(aimTarget, 1);
                if (PointerInput.Key(Key.LeftArrow)) aimTarget = NextEnemy(aimTarget, -1);
            }
            if (sel >= 0 && (PointerInput.Key(Key.Space) || PointerInput.Key(Key.Enter)))
            {
                int t = NeedsEnemy(sel) ? (aimTarget >= 0 ? aimTarget : NextEnemy != null ? NextEnemy(-1, 1) : 0) : -1;
                Confirm(sel, t);
            }
        }

        /// <summary>자동 데모 · 점검 — 아군 카드를 놓을 자리(카드 주인 사도, 없으면 첫 사도의 몸 가운데께).</summary>
        public Vector3 AllyDrop(CardInfo info)
        {
            int h = info.Hero >= 0 ? info.Hero : info.Owner >= 0 ? info.Owner : 0;
            var f = AllyFoot?.Invoke(h) ?? AllyFoot?.Invoke(0);
            return f.HasValue ? f.Value.at + new Vector3(0, 0.9f, 0) : new Vector3(-3f, 0.2f, 0);
        }

        // 자동 데모 — 손가락 없이 고른 채로 겨누기
        public void DemoAim(int e) => aimTarget = e;

        void SelUpdate(Vector2 p)
        {
            var card = Cards[sel];
            Lifted = true;
            if (!NeedsEnemy(sel)) { arrow.Hide(); reticle.enabled = false; return; }
            // 마우스는 올린 적을 겨눈다(벗어나면 마지막 것 그대로) — 터치 · 단축키는 탭 · 화살표로
            if (!PointerInput.Touch && EnemyAt != null)
            {
                int e = EnemyAt(p);
                if (e >= 0) aimTarget = e;
            }
            Vector3 end = aimTarget >= 0 && EnemyAim != null ? EnemyAim(aimTarget) : (Vector3)p;
            arrow.Show(card.transform.position + new Vector3(0, CardView.H * 0.5f * card.transform.localScale.x, 0), end, aimTarget >= 0);
            Reticle(end, aimTarget >= 0);
        }

        void Reticle(Vector3 end, bool on, float bodyW = 1.6f)
        {
            reticle.enabled = on;
            if (!on) return;
            reticle.transform.position = end;
            reticle.transform.localRotation = Quaternion.identity;
            float rs = Mathf.Clamp(bodyW * 0.7f, 1.1f, 2.2f) * (1 + 0.05f * Mathf.Sin(Clock.Now * 10f));
            Make.Fit(reticle, new Vector2(rs, rs));
        }

        // 끌기(카제나식) — 카드는 손패에서 뽑혀 나온다.
        //   적 1명 · 아군 1명 카드: 카드는 손패 가운데 위에 조금 크게 서 있고(손가락을 따라가지 않는다) 카드 위쪽에서 손가락까지 위로 볼록한 호.
        //     적 위면 몸통에 조준경 + 발밑 붉은 타원 고리, 아군 위면 발밑 초록 파랑 고리. 대상 위에서 떼면 낸다, 아니면 손패로 돌아간다
        //   그 밖(적 전체 · 대상 없음): 카드가 손가락을 따라간다. 위(싸움터)로 올리면 낸다 — 적 전체 카드는 그때 모든 적 발밑에 붉은 고리
        //   어느 쪽이든 뽑는 순간 짧은 뽑기 연출(PullFx), 나머지 손패는 살짝 어둡게
        static readonly Color FoeRed = new Color(1f, 0.22f, 0.18f), AllyTeal = new Color(0.3f, 0.95f, 0.75f);
        public static float PullScale(float handScale) => Mathf.Max(handScale * 1.22f, 1.12f);

        void DragUpdate(Vector2 p)
        {
            var card = Cards[drag];
            var kind = card.Info.Target;
            bool pointer = kind == TargetKind.Enemy || kind == TargetKind.Ally;
            bool lifted = p.y > LiftY;
            aimTarget = -1;
            allyTarget = -1;
            if (pointer)
            {
                float ps = PullScale(HandScale);
                card.TargetScale = ps;
                card.TargetRot = 0;
                card.Hovered = true;                       // 테두리 빛(카드 성격 빛)
                // 손패 줄 위로 확실히 — 카드 아래 끝이 손패 카드 높이의 4할께(더 올리면 폰에서 가까운 적을 가린다)
                card.TargetPos = new Vector3(0, Bottom + CardView.H * HandScale * 0.4f + CardView.H * ps / 2, 0);
                if (kind == TargetKind.Enemy) aimTarget = EnemyAt != null ? EnemyAt(p) : -1;
                else allyTarget = AllyAt != null ? AllyAt(p) : -1;
                bool on = aimTarget >= 0 || allyTarget >= 0;
                Lifted = on || lifted;
                Vector3 top = card.transform.position + new Vector3(0, CardView.H * 0.5f * card.transform.localScale.y, 0);
                arrow.Show(top, p, on, kind == TargetKind.Enemy ? FoeRed : AllyTeal);
                if (aimTarget >= 0 && EnemyAim != null)
                {
                    var f = EnemyFoot?.Invoke(aimTarget);
                    Reticle(EnemyAim(aimTarget), true, f.HasValue ? f.Value.w : 1.6f);
                    if (f.HasValue) Ring(f.Value.at, f.Value.w, FoeRed);
                }
                else Reticle(Vector3.zero, false);
                if (allyTarget >= 0) { var f = AllyFoot?.Invoke(allyTarget); if (f.HasValue) Ring(f.Value.at, f.Value.w, AllyTeal); }
            }
            else
            {
                Lifted = lifted;
                arrow.Hide();
                reticle.enabled = false;
                card.TargetPos = new Vector3(p.x + dragOffset.x, p.y + dragOffset.y, 0);
                card.TargetRot = 0;
                card.Hovered = true;
                card.TargetScale = PullScale(HandScale) * (lifted ? 0.95f : 1f);
                if (lifted && kind == TargetKind.AllEnemies && EnemyFoot != null && EnemyCount != null)
                    for (int e = 0; e < EnemyCount(); e++) { var f = EnemyFoot(e); if (f.HasValue) Ring(f.Value.at, f.Value.w, FoeRed); }
            }

            if (PointerInput.Up)
            {
                int idx = drag;
                bool ok = pointer ? (aimTarget >= 0 || allyTarget >= 0) : lifted;
                int target = kind == TargetKind.Enemy ? aimTarget : kind == TargetKind.Ally ? allyTarget : -1;
                CancelDrag();
                if (ok) Confirm(idx, target);
                else Sfx.Play("card_hover", 0.3f, 0.8f);   // 대상이 아닌 곳 · 손패 쪽 — 취소, 카드는 제자리로
            }
        }

        // ── 발밑 타원 고리 — 프레임마다 쓴 만큼만 켠다(RingsBegin 으로 셈을 비우고, RingsEnd 로 남은 것을 끈다) ──
        void RingsBegin() => ringsUsed = 0;
        void RingsEnd()
        {
            for (int i = ringsUsed; i < rings.Count; i++) if (rings[i].enabled) rings[i].enabled = false;
        }

        void Ring(Vector3 at, float w, Color col)
        {
            if (ringsUsed >= rings.Count)
                rings.Add(Make.Box("ring" + rings.Count, transform, RingSprite(), Vector3.zero, Vector2.one, 21, col, Res.SpriteMat(false, 1.5f)));
            var r = rings[ringsUsed++];
            r.enabled = true;
            float rw = Mathf.Clamp(w * 0.95f, 1.3f, 4.2f) * (1 + 0.04f * Mathf.Sin(Clock.Now * 8f));
            r.transform.position = new Vector3(at.x, at.y, 0);
            Make.Fit(r, new Vector2(rw, rw * 0.3f));
            col.a = 0.95f;
            r.color = col;
        }

        // ── 뽑기 연출 ──
        void PullFx(CardView c)
        {
            bool calm = Bolzena.RunUI.Settings.ReduceMotion || Clock.Speed > 1.5f;
            pullAmp = calm ? 0.35f : 1f;
            if (pulled != null && pulled != c) pulled.Follow = 14f;
            pulled = c;
            pullT = 0;
            c.Follow = 30f;                                            // 이 동안은 빨리 따라간다(튀어 오름이 뭉개지지 않게)
            if (Res.Clip("Sfx/" + PullSfx) != null) Sfx.Play(PullSfx, 0.5f); else Sfx.Play("card_hover", 0.5f, 1.35f);
            Clock.Run(c.FlashCo(0.16f, 0.25f * pullAmp));
            var tint = CardView.RimColor(c.Info);
            if (pullGlow == null) pullGlow = Make.Box("pullglow", transform, Res.UI("soft"), Vector3.zero, Vector2.one, 640, tint, Res.SpriteMat(true, 2f));
            pullGlow.color = tint;
            pullGlow.transform.position = c.transform.position;        // 뽑힌 자리(손패)에 잠깐 남는 빛
            pullGlow.enabled = true;
        }

        void PullUpdate()
        {
            if (pullT < 0) { if (pullGlow != null) pullGlow.enabled = false; return; }
            pullT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(pullT / PullDur);
            int i = pulled != null ? Cards.IndexOf(pulled) : -1;
            if (i >= 0 && (i == drag || i == sel))
            {
                // 튀어 오름 — 0 → 넘침 → 제자리(sin 반 바퀴), 기울기 — 처음에 6° 기울었다 줄어든다
                pulled.TargetScale *= 1 + 0.12f * pullAmp * Mathf.Sin(Mathf.PI * k);
                pulled.TargetRot += 6f * pullAmp * (1 - k) * (1 - k) * (pulled.SlotX >= 0 ? -1 : 1);
                pulled.TargetPos += new Vector3(0, 0.18f * pullAmp * Mathf.Sin(Mathf.PI * k), 0);
            }
            if (pullGlow != null)
            {
                var c = pullGlow.color; c.a = 0.55f * pullAmp * (1 - k) * (1 - k); pullGlow.color = c;
                float g = CardView.W * HandScale * (1.3f + 0.5f * k);
                Make.Fit(pullGlow, new Vector2(g, g * 1.35f));
            }
            if (k >= 1)
            {
                pullT = -1;
                if (pulled != null) pulled.Follow = 14f;
                pulled = null;
                if (pullGlow != null) pullGlow.enabled = false;
            }
        }

        // ── 그림 — 조준경(원 두 겹 + 가운데가 빈 십자선) · 타원 고리(가장자리가 부드러운 띠). 처음 한 번 만든다 ──
        static Sprite scopeSpr, ringSpr;
        static Sprite ScopeSprite()
        {
            if (scopeSpr != null) return scopeSpr;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            float aa = 3f / N;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1, d = Mathf.Sqrt(u * u + v * v);
                    float a = 0;
                    a = Mathf.Max(a, Band(d, 0.84f, 0.93f, aa));                  // 바깥 원
                    a = Mathf.Max(a, Band(d, 0.40f, 0.46f, aa));                  // 안 원
                    float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
                    if (ay > 0.14f && ay < 0.99f) a = Mathf.Max(a, Band(ax, -1f, 0.022f, aa));   // 세로 십자선(가운데는 비운다)
                    if (ax > 0.14f && ax < 0.99f) a = Mathf.Max(a, Band(ay, -1f, 0.022f, aa));   // 가로 십자선
                    a = Mathf.Max(a, Band(d, -1f, 0.045f, aa));                   // 가운데 점
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            scopeSpr = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            scopeSpr.name = "scope";
            return scopeSpr;
        }

        static Sprite RingSprite()
        {
            if (ringSpr != null) return ringSpr;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1, d = Mathf.Sqrt(u * u + v * v);
                    float core = Band(d, 0.82f, 0.94f, 0.03f);                    // 고리
                    float glow = Mathf.Clamp01(1 - Mathf.Abs(d - 0.88f) / 0.12f) * 0.45f;   // 둘레 번짐
                    float fill = d < 0.88f ? 0.12f * d : 0;                       // 안쪽은 아주 옅게
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(Mathf.Max(core, Mathf.Max(glow, fill))) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            ringSpr = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            ringSpr.name = "foot ring";
            return ringSpr;
        }

        // d 가 [lo, hi] 안이면 1, 밖으로 aa 만큼 부드럽게 0
        static float Band(float d, float lo, float hi, float aa) => Mathf.Clamp01(Mathf.Min(d - lo, hi - d) / aa + 0.5f);

        void CancelDrag()
        {
            drag = -1;
            press = -1;
            allyTarget = -1;
            arrow.Hide();
            reticle.enabled = false;
            Lifted = false;
            Layout();
        }

        // 자동 데모 — 가짜 손가락으로 카드를 올리고 끌어 놓는다(진짜 입력과 같은 길)
        public IEnumerator DemoDrag(int handIndex, Vector3 to, float hoverTime = 0.35f, float dragTime = 0.5f, float holdTime = 0.25f, Action onAiming = null)
        {
            if (handIndex < 0 || handIndex >= Cards.Count) yield break;
            PointerInput.Simulated = true;
            var c = Cards[handIndex];
            Vector2 start = c.transform.position + new Vector3(0, -0.2f, 0);
            Vector2 from = PointerInput.SimPos == Vector2.zero ? new Vector2(start.x, -4.4f) : PointerInput.SimPos;
            PointerInput.SimHeld = false;
            yield return Clock.Tween(0.2f, t => PointerInput.SimPos = Vector2.Lerp(from, start, Ease.OutCubic(t)), true);
            yield return Clock.WaitU(hoverTime);
            PointerInput.SimHeld = true;
            yield return null;
            Vector2 a = PointerInput.SimPos;
            yield return Clock.Tween(dragTime, t => PointerInput.SimPos = Vector2.Lerp(a, to, Ease.InOutCubic(t)), true);
            onAiming?.Invoke();
            yield return Clock.WaitU(holdTime);
            PointerInput.SimHeld = false;
            yield return null;
            yield return null;
        }
    }
}
