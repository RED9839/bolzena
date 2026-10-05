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
        Vector2 dragOffset;
        TargetArrow arrow;
        SpriteRenderer reticle;
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
            h.reticle = Make.Box("reticle", root, Res.UI("reticle"), Vector3.zero, new Vector2(1.3f, 1.3f), 690, new Color(1f, 0.35f, 0.3f, 0.9f), Res.SpriteMat(false, 1.6f));
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
                bool changed = Cards[i].Info.Cost != hand[i].Cost || Cards[i].Info.Epiphany != hand[i].Epiphany || Cards[i].Info.Text != hand[i].Text;
                Cards[i].Info = hand[i];
                if (changed) Cards[i].Refresh();
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
            float spacing = n > 1 ? Mathf.Min(cw * 0.88f, (span - cw) / (n - 1)) : 0;
            int up = drag >= 0 ? (Lifted && NeedsEnemy(drag) ? drag : -1) : sel >= 0 ? sel : hover;
            float upS = Mathf.Max(s * 1.34f, 1.18f);
            float upPush = up >= 0 ? Mathf.Max(0, (CardView.W * upS + cw) / 2 + Gap - spacing) : 0;   // 올린 카드와 이웃이 안 겹칠 만큼
            for (int i = 0; i < n; i++)
            {
                var c = Cards[i];
                float off = i - (n - 1) / 2f;
                c.SlotX = off * spacing;
                if (i == drag) continue;
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
                if (c.Info.Epiphany && i != up) y += 0.14f + 0.05f * Mathf.Sin(Clock.Now * 3f);   // 신탁 카드는 살짝 떠서 숨 쉰다
                if (Hidden) y -= 4.5f;
                c.TargetPos = new Vector3(x, y, 0);
                c.TargetRot = rot;
                c.TargetScale = sc;
                c.Hovered = i == up;
                c.ShowDesc = i == up;
                if (c.Order != order) c.SetOrder(order);
                c.Playable = (CanPlay == null || CanPlay(c.Info)) && !c.Info.Unplayable;
            }
            if (drag >= 0 && drag < n) Cards[drag].ShowDesc = true;
            Pins(up);
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
                int hero = c.Info.Hero;
                if (hero < 0) continue;
                int prev = i - 1;
                if (prev == drag) prev--;
                if (prev >= 0 && Cards[prev].Info.Hero == hero && i != up && prev != up) continue;
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
            Sfx.Play("card_hover", 0.45f, 1.15f);
            Layout();
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
            Tooltip.I?.Unpin();
            Request = (i, target);
        }

        void Update()
        {
            Layout();
            if (!Interactive || Modal.Open != null)
            {
                if (!Interactive) { if (drag >= 0) CancelDrag(); sel = -1; press = -1; }
                if (hover >= 0) { hover = -1; Layout(); }
                arrow.Hide(); reticle.enabled = false; Lifted = false;
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
                if (PointerInput.Touch && !PointerInput.Held) h = -1;   // 터치에는 올려 두기가 없다
                if (h != hover)
                {
                    hover = h;
                    hoverT = 0;
                    if (h >= 0 && !PointerInput.Touch) Sfx.Play("card_hover", 0.35f);
                    Layout();
                }
                hoverT += Time.unscaledDeltaTime;
                if (PointerInput.Down) press = CardAt(p);
                // 누른 채 움직이면 끌기
                if (PointerInput.Held && press >= 0 && PointerInput.Moved && press < Cards.Count)
                {
                    var c = Cards[press];
                    if (!c.Playable) { Cant(press); press = -1; }
                    else
                    {
                        drag = press;
                        sel = -1;
                        dragOffset = (Vector2)c.transform.localPosition - p;
                        c.SetOrder(650);
                    }
                }
                if (drag < 0 && PointerInput.Tap) OnTap(p);
                if (PointerInput.Up) press = -1;
            }

            if (drag >= 0) DragUpdate(p);
            else if (sel >= 0) SelUpdate(p);
            else { arrow.Hide(); reticle.enabled = false; Lifted = false; }

            // 카드 확대 — 올린 카드는 잠깐 뒤 그 자리 위로 크게(키워드 판은 옆), 고른 카드는 왼쪽(사도 쪽)에 크게 — 오른쪽 적 · 미리보기를 가리지 않게
            int tipCard = drag >= 0 ? -1 : sel >= 0 ? sel : hover >= 0 && hoverT > 0.4f ? hover : -1;
            if (tipCard >= 0 && tipCard < Cards.Count)
            {
                var c = Cards[tipCard];
                string why = !c.Playable ? WhyNot?.Invoke(tipCard) : null;
                float s = Mathf.Min(2.0f, (Tone.HalfH * 2 * 0.58f) / CardView.H);
                if (tipCard == sel)
                    CardZoom.Show(transform.parent, c.Info, new Vector3(-Tone.HalfW + CardView.W * s / 2 + 0.3f, 0.6f, 0), s, false, why);
                else
                    CardZoom.Show(transform.parent, c.Info, new Vector3(c.TargetPos.x, Bottom + CardView.H * s / 2 + 0.05f, 0), s, false, why);
                Tooltip.I?.Unpin();
            }
            else CardZoom.Hide();
        }

        void OnTap(Vector2 p)
        {
            int at = CardAt(p);
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

        void Reticle(Vector3 end, bool on)
        {
            reticle.enabled = on;
            if (!on) return;
            reticle.transform.position = end;
            reticle.transform.localRotation = Quaternion.Euler(0, 0, Clock.Now * 90f);
            float rs = 1.3f * (1 + 0.06f * Mathf.Sin(Clock.Now * 10f));
            Make.Fit(reticle, new Vector2(rs, rs));
        }

        void DragUpdate(Vector2 p)
        {
            var card = Cards[drag];
            bool aims = card.Info.Target == TargetKind.Enemy;
            bool lifted = p.y > LiftY;
            Lifted = lifted;
            aimTarget = -1;
            if (aims && lifted)
            {
                // 대상 카드 — 카드는 손 위에 띄워 두고 화살표로 짚는다
                card.TargetScale = Mathf.Max(HandScale * 1.2f, 1.08f);
                card.TargetPos = new Vector3(Mathf.Clamp(card.SlotX, -HalfSpan + 1.1f, HalfSpan - 1.1f), Bottom + 0.25f + CardView.H * card.TargetScale / 2, 0);
                card.TargetRot = 0;
                aimTarget = AimAt(p);
                Vector3 end = aimTarget >= 0 && EnemyAim != null ? EnemyAim(aimTarget) : (Vector3)p;
                arrow.Show(card.transform.position + new Vector3(0, CardView.H * 0.5f, 0), end, aimTarget >= 0);
                Reticle(end, aimTarget >= 0);
            }
            else
            {
                arrow.Hide();
                reticle.enabled = false;
                card.TargetPos = new Vector3(p.x + dragOffset.x, p.y + dragOffset.y, 0);
                card.TargetRot = 0;
                card.TargetScale = lifted ? 1.0f : 1.1f;
            }

            if (PointerInput.Up)
            {
                int idx = drag;
                bool ok = lifted && (!aims || aimTarget >= 0);
                int target = aimTarget;
                CancelDrag();
                if (ok) Confirm(idx, target);
                else if (lifted && aims) Miss(p);
            }
        }

        void CancelDrag()
        {
            drag = -1;
            press = -1;
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
