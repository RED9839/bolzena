using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 몸짓과 타격 — 내딛기 · 잔상 · 때리는 순간(스파인 이벤트) · 흰 번쩍임 · 히트스톱 · 흔들림/줌 펀치 · 파편 · 숫자 · 치명 · 격파 · 쓰러짐
    public partial class BattleDirector
    {
        bool firstHitShot, firstCritShot, firstBreakShot, firstHurtShot;
        int fxBoost;                     // 고학년 동안 이펙트를 어둠막(300) 위로

        // 맞는 차례 k 를 동작의 타격 시각 중 어디에 둘까
        static List<float> HitTimes(List<float> marks, int hits, float speed)
        {
            var res = new List<float>();
            int m = marks.Count;
            for (int k = 0; k < hits; k++)
            {
                float t;
                if (hits == 1) t = marks[0];
                else if (hits <= m) t = marks[Mathf.RoundToInt(k * (m - 1) / (float)(hits - 1))];
                else
                {
                    float span = m > 1 ? marks[m - 1] - marks[0] : 0.12f * (hits - 1);
                    t = marks[0] + span * k / (hits - 1);
                }
                res.Add(t / speed);
            }
            return res;
        }

        // 결과를 맞는 차례별로 — 첫 타격 전의 것(pre)과 차례마다의 묶음
        static void Split(List<BattleEvent> group, out List<BattleEvent> pre, out SortedDictionary<int, List<BattleEvent>> byHit, out int hits)
        {
            pre = new List<BattleEvent>();
            byHit = new SortedDictionary<int, List<BattleEvent>>();
            hits = 0;
            int cur = -1;
            foreach (var e in group)
            {
                if ((e.Kind == EventKind.Damage && !IsShot(e)) || e.Kind == EventKind.PartyHurt)   // 쏘는 한 대는 박자를 세우지 않고 지금 박자에 붙는다
                {
                    cur = e.Hit;
                    hits = Mathf.Max(hits, e.Hits);
                }
                if (cur < 0) { pre.Add(e); continue; }
                if (!byHit.TryGetValue(cur, out var l)) byHit[cur] = l = new List<BattleEvent>();
                l.Add(e);
            }
        }

        static readonly Dictionary<string, string> heroSfx = new Dictionary<string, string>();

        static string HeroSfx(string key, Motion m, bool hit)
        {
            string ck = key + "|" + m + "|" + hit;
            if (heroSfx.TryGetValue(ck, out var got)) return got;
            return heroSfx[ck] = HeroSfxFind(key, m, hit);
        }

        static string HeroSfxFind(string key, Motion m, bool hit)
        {
            string pre = m == Motion.Attack1 ? "basicattack" : m == Motion.Attack2 ? "powerattack" : m == Motion.Skill1 ? "spskill" : "ultimate";
            var all = Resources.LoadAll<AudioClip>("Sfx/hero/" + key);
            string best = null;
            foreach (var c in all)
            {
                var n = c.name.Substring(key.Length + 1);
                if (!n.StartsWith(pre)) continue;
                if (n.Contains("hit") != hit) continue;
                if (best == null || string.CompareOrdinal(c.name, best) < 0) best = c.name;
            }
            if (best == null && m == Motion.Ultimate) return HeroSfxFind(key, Motion.Skill1, hit);   // 고학년 효과음이 없는 사도(이프리트) — 스킬 소리로
            return best == null ? null : "hero/" + key + "/" + best;
        }

        // ── 사도의 몸짓 ──
        IEnumerator HeroAct(BattleEvent act, List<BattleEvent> group, bool ult = false)
        {
            var u = Heroes[act.Actor.Index];
            string key = u.name.Replace("hero_", "");
            Split(group, out var pre, out var byHit, out int hits);
            // 몸짓의 피해가 모두 쏘는 한 대(추가 공격만 하는 카드)면 — 몸짓의 때리는 순간에 쏜다(시작하자마자 쏘지 않게)
            if (byHit.Count == 0)
            {
                int s0 = pre.FindIndex(x => x.Kind == EventKind.Shot);
                if (s0 >= 0) { byHit[0] = pre.GetRange(s0, pre.Count - s0); pre.RemoveRange(s0, pre.Count - s0); hits = 1; }
            }
            ult |= act.Text == "ult";
            int seq = BeginAct(u);   // 이 사도가 앞 수의 뒷정리(복귀) 중이면 그 자리에서 이어받는다

            // 대상들
            var targets = new List<int>();
            foreach (var e in group) if (e.Kind == EventKind.Damage && !IsShot(e) && !targets.Contains(e.Target.Index)) targets.Add(e.Target.Index);   // 쏘는 한 대(다른 적에게 간 탄환 · 협공)는 몸짓 대상이 아니다
            if (targets.Count == 0 && act.Motion != Motion.None && act.Target.Side == Side.Enemy && act.Target.Index < Enemies.Count) targets.Add(act.Target.Index);
            TigNear = targets.Count > 0 && FrontEnemy() == targets[0];   // (10-18 4차) 티그(영웅) 강공 거리 갈래 — 살아 있는 적 가운데 우리 쪽에 가장 가까운(맨 앞) 적이면 가까움
            Demo.UltLap.Lap("대상");
            actAim = targets.Count > 0 && targets[0] < Enemies.Count && Enemies[targets[0]] != null ? Enemies[targets[0]].Center : (Vector3?)null;

            if (act.Motion == Motion.None)
            {
                // 몸짓 없는 스킬 — 빛이 감돌고 곧바로
                u.Flash(new Color(1f, 0.95f, 0.75f), 0.35f);
                Vfx.Glow(u.Center, 2.4f, new Color(1f, 0.9f, 0.6f, 0.8f), 0.4f, 2.4f);
                Vfx.Burst(u.Center, new Vfx.BurstOpt { Tex = "FX_UI_star_02", Count = 10, Speed = new Vector2(1f, 3f), Angle = 90, Spread = 120, Life = new Vector2(0.4f, 0.7f), Size = new Vector2(0.08f, 0.18f), C0 = new Color(1f, 0.9f, 0.6f), Order = 150, Boost = 3f, ShrinkTo = 0 });
                foreach (var e in pre) yield return ApplyConsequence(e, u);
                foreach (var kv in byHit) foreach (var e in kv.Value) yield return ApplyConsequence(e, u);
                yield return Clock.Wait(OldTempo ? 0.25f : 0.12f);
                yield break;
            }

            string anim = u.AnimFor(act.Motion);
            float speed = ult ? (key == "ed" ? 1.35f : 1.05f) : act.Motion == Motion.Attack1 && key == "leets" ? 1.7f : 1.3f;
            bool origTempo = Bolzena.Fx.BolzenaFx.OriginalOnly;   // (10-17 사용자 「모션 줄이지 말고 원작과 같은 속도로」) — 자르기 없음
            if (origTempo) speed = OrigSpeed(Battle.Snapshot.Heroes[act.Actor.Index].Id, ult ? Motion.Ultimate : act.Motion);   // 원작 영상 배속(Cards ORIG_SPEED — 없으면 1배속). 표 시각(몸짓 ms)은 모두 이 speed 로 나눠 화면 시각이 된다
            var marks = u.Strikes(u.F(anim));   // 변신 중이면 실제로 트는 꼬리판(Attack1_1_Change)의 때리는 순간
            // 카드 — 이어지는 조각까지(BattleDirector.Cards.cs CARD_CHAIN) · 때리는 순간은 조각 전체의 타격 이벤트(0ms 시전 표시 빼고)
            string hidC = Battle.Snapshot.Heroes[act.Actor.Index].Id;
            List<string> cardChain = null; float cardTotal = 0;
            if (!ult && !OldSync && u.SpineArt && u.SkelData != null && anim != null)
            {
                cardChain = CardChain(u, hidC, act.Motion, anim, byHit.Count);   // 탄 수만큼 고리(CARD_LOOP — 시온 스킬)
                anim = CardFxAnim(hidC, act.Motion, cardChain[0]);   // 이 뒤 anim 은 이펙트 갈래 · 시전 본 고르기에만(몸짓은 cardChain)
                if (anim != null && u.FormTail != null) anim = u.F(anim);   // 변신 중이면 꼬리까지(_DreamForm) — 장 표가 변신 판을 고른다(10-17 죠안 꿈결 붉은 장)
                marks = LoopShots(u, hidC, act.Motion, cardChain, CardHitsFix(hidC, act.Motion, CardStrikes(u, cardChain, out cardTotal), u.FormTail));   // (10-18 2차) 고리 몸짓은 고리마다 한 발
                if (NearUpperCut(hidC, act.Motion)) marks = new List<float> { 1.55f };   // (10-18 4차) 가까운 적 — cast_1 올려 베기(시전 100 + 장 안 지연 1.45초) 때. 먼 적은 표 1900(검기가 닿는 때) 그대로
            }
            // 고학년 — 원작 조각을 웹판처럼 잇는다(SpineMotion.PlanUlt: Ultimate1_1 → 1_2(_Loop) … · 갈래가 여럿이면 하나 · 달려가는 사도는 고리 횟수까지)
            //   때리는 순간도 그 계획(스파인 SFX · Event)으로. 전에는 Ultimate1_1 한 조각만 틀어 몸짓이 중간에 끊겼다
            UltPlan = null; ultPickFit = null;
            if (ult && u.SpineArt && u.SkelData != null)
            {
                //   란은 늘 Ultimate1_1(달 · 몰이 4타) — 1_2 는 원작이 처치 뒤 다시 쓸 때만 트는 짧은 조각이라 첫 고학년이 1_2 면 달 없이 시작했다(2026-10-14)
                try { UltPlan = Bolzena.Fx.SpineMotion.PlanUlt(u.SkelData, Battle.Snapshot.Heroes[act.Actor.Index].Id, UltFirstBranch(Battle.Snapshot.Heroes[act.Actor.Index].Id)); }
                catch (System.Exception ex) { Debug.LogWarning("[Ult] 계획 실패 " + key + " — " + ex.Message); }
                var fc0 = UltPlan != null ? UltFitOf(Battle.Snapshot.Heroes[act.Actor.Index].Id) : null;
                ultPickFit = null;
                if (fc0?.Pick != null)   // 갈래 하나 무작위(에피카) — 그 갈래 조각만 틀고, 맞춤도 그 갈래 값으로
                {
                    var ok = System.Array.FindAll(fc0.Pick, v => v.Anim != null && u.Has(v.Anim));
                    if (ok.Length > 0)
                    {
                        ultPickFit = ok[PickRng.Index("ult:" + key, ok.Length)];
                        UltPlan.Anim = ultPickFit.Anim; UltPlan.Chain.Clear(); UltPlan.Table = null;
                        Debug.Log($"[Ult] {key} 갈래 {ultPickFit.Anim} — {ultPickFit.Note}");
                    }
                }
                if (fc0 != null && fc0.Pick == null && fc0.Chain == null && fc0.Anim != null && u.Has(fc0.Anim)) { UltPlan.Anim = fc0.Anim; UltPlan.Chain.Clear(); UltPlan.Table = null; }   // 갈래 없이 몸짓만 바꾸는 사도(클로에 Skill2_1 — fix_Y 4)
                // (란) 1_2 는 처치 뒤 다시 쓸 때만 — 엔진(ifKill)이 다음 적에게 피해를 줬을 때 그 수만큼. 처치가 없으면 1_1 한 조각(맞음 3번)
                //   남는 조각 밖의 타격 · 장 시각(More)은 거르고, 첫 시각부터 밖인 장(1_2 의 slash_2)은 뺀다
                if (fc0?.Chain != null && UltReuseOnKill(Battle.Snapshot.Heroes[act.Actor.Index].Id))
                {
                    int reuse = Mathf.Clamp(targets.Count - 1, 0, fc0.Chain.Length - 1);
                    float endMs = 3300 + 2167 * reuse;   // 1_1 3300 + 1_2 2167 × reuse
                    var ch = new string[1 + reuse]; System.Array.Copy(fc0.Chain, ch, ch.Length);
                    var fx = new Dictionary<string, FxFix>(); var drop = new List<string>();
                    if (fc0.Fx != null)
                        foreach (var kv in fc0.Fx)
                        {
                            if (kv.Value.Ms >= endMs) { drop.Add(kv.Key); continue; }
                            fx[kv.Key] = new FxFix(kv.Value.At, kv.Value.Ms, kv.Value.Scale) { More = kv.Value.More == null ? null : System.Array.FindAll(kv.Value.More, m => m < endMs) };
                        }
                    ultPickFit = new UltFit { Move = fc0.Move, Chain = ch, Speed = fc0.Speed, Add = fc0.Add, Cap = fc0.Cap, Rep = fc0.Rep, Fx = fx, Drop = drop.Count > 0 ? drop.ToArray() : null,
                                              Hits = fc0.Hits == null ? null : System.Array.FindAll(fc0.Hits, h => h < endMs), Note = fc0.Note + $" · 이번 {ch.Length}조각" };
                    fc0 = ultPickFit;
                }
                if (fc0?.Chain != null && System.Array.TrueForAll(fc0.Chain, n => u.Has(n)))   // 원작이 잇는 조각(에피카 자동차 → 해적선 → 유령 말)
                {
                    UltPlan.Anim = fc0.Chain[0]; UltPlan.Chain.Clear();
                    for (int ci = 1; ci < fc0.Chain.Length; ci++) UltPlan.Chain.Add(fc0.Chain[ci]);
                    UltPlan.Table = null;
                    // (10-18 사용자 4차 「란 소리와 임팩트 시간차 · 끊김」) 조각을 바꿨으면 소리 칸(스파인 SFX)도 그 조각들로 — 전엔 1_1 칸만 남아 1_2 마다 칼 소리가 없었다
                    UltPlan.Snd = Bolzena.Fx.SpineMotion.PlanNames(u.SkelData, null, fc0.Chain, "ult", null, true).Snd;   // heroKey null — 돌진 표(DASH Loop)를 타지 않게
                }
                if (UltPlan != null && UltPlan.Anim != null)
                {
                    anim = UltPlan.Anim;
                    if (UltPlan.S != null)
                    {
                        marks = new List<float> { UltPlan.S.At / 1000f };
                        foreach (var m in UltPlan.S.Marks) if (m > UltPlan.S.At) marks.Add(m / 1000f);
                    }
                }
            }
            // 긴 고학년 줄이기(2026-10-06 사용자 원칙) — 원작 몸짓이 7초(ULT_MAX, 화면 시계) 안이면 그대로. 넘으면 꼬리 · 앞 준비만 모자란 몫만큼, 그래도 길면 1.2배속까지.
            //   몸짓 · 이펙트 · 소리 · 이동 · 타격이 모두 speed 로 시각을 나누니 같은 시계로 따라온다. 그래도 넘는 사도는 점검 표에 남는다
            UltSpeed = speed; UltNaturalMs = 0; UltShortShown = false;
            cutSkips.Clear(); cutEnd = float.MaxValue; UltCutFrontMs = 0; UltCutBackMs = 0;
            var fit = ult ? (ultPickFit ?? UltFitOf(Battle.Snapshot.Heroes[act.Actor.Index].Id)) : null;   // 원작 영상 맞춤(UltVideoFit.cs)
            UltFitNow = fit;
            if (ult && !OldSync && UltPlan != null && UltPlan.Anim != null)
            {
                float nat = u.Duration(UltPlan.Anim);
                foreach (var cn in UltPlan.Chain) nat += u.Duration(cn);
                if (UltPlan.Table != null) nat = Mathf.Max(nat, UltPlan.Table.TotalMs / 1000f);
                UltNaturalMs = Mathf.RoundToInt(nat * 1000);
                bool keepLong = ULT_KEEP_LONG.Contains(Battle.Snapshot.Heroes[act.Actor.Index].Id);
                bool fitLen = false;
                if (fit != null && (fit.Skip != null || fit.End > 0))   // 원작 영상 맞춤 — 길이를 손으로 정한 사도(고리 횟수 · 끝). 앞 · 뒤 셈 줄이기는 안 하고 배속만
                {
                    fitLen = true;
                    if (fit.Skip != null) for (int i = 0; i + 1 < fit.Skip.Length; i += 2) cutSkips.Add((fit.Skip[i] / 1000f, fit.Skip[i + 1] / 1000f));
                    if (fit.End > 0 && !origTempo) cutEnd = fit.End / 1000f;
                    UltCutFrontMs = 0; for (int i = 0; fit.Skip != null && i + 1 < fit.Skip.Length; i += 2) UltCutFrontMs += fit.Skip[i + 1] - fit.Skip[i];
                    UltCutBackMs = fit.End > 0 && !origTempo ? Mathf.Max(0, UltNaturalMs - fit.End) : 0;
                    nat = Rm(nat);
                }
                if (fit != null && fit.Speed > 0 && !origTempo) { speed = fit.Speed; keepLong = true; }
                if (origTempo) keepLong = true;
                if (UltShortNow(Battle.Snapshot.Heroes[act.Actor.Index].Id) && !keepLong && !fitLen && nat / speed > ULT_SHORT)   // 짧게 보기(설정 · 판에서 두 번째부터) — 앞 준비 · 꼬리를 더 덜고 조금 빠르게
                { PlanCut(u, hidUlt: Battle.Snapshot.Heroes[act.Actor.Index].Id, need: nat - ULT_SHORT * speed); nat = Rm(nat); speed *= 1.15f; UltShortShown = true; }
                else if (!keepLong && !fitLen && nat / speed > ULT_MAX) { PlanCut(u, hidUlt: Battle.Snapshot.Heroes[act.Actor.Index].Id, need: nat - ULT_MAX * speed); nat = Rm(nat); }   // 7초 넘는 몫만 — 꼬리 · 앞 준비부터(가운데 타격은 그대로)
                if (!keepLong && nat / speed > ULT_MAX) speed = Mathf.Clamp(nat / ULT_MAX, speed, Mathf.Max(speed, ULT_SPEED_MAX));   // 그래도 길면 조금 빠르게 — 많아야 1.2배속(사용자 원칙 2026-10-06)
                UltSpeed = speed;
                if (ultPickFit?.PetSkin != null)   // 갈래 소환수 — 배속 · 줄인 몫(Rm)이 정해진 뒤에(던지기 박자를 화면 시각으로)
                    StartCoroutine(UltPetCo(u, "epicon_ult", ultPickFit.PetSkin, ultPickFit.PetAnim, ultPickFit.PetHits ?? (ultPickFit.PetMs >= 0 ? new[] { ultPickFit.PetMs } : null), speed));
            }
            // 카드 — 애니 끝까지(전엔 마지막 타격 + 0.32초에 끊었다). 상한(CardCap)을 넘으면 꼬리 · 앞 준비 · 빈 구간부터 줄이고, 그래도 0.4초 넘게 남으면 1.5배속까지
            bool cardSelf = cardChain != null && CardSelf(hidC, act.Motion);
            if (cardChain != null)
            {
                CardNaturalMs = Mathf.RoundToInt(cardTotal * 1000);
                // 타수만큼만 휘두른다(2026-10-09 「1타인데 2타 모션이 나가 길어진다」) — 엔진이 N 번 때리는데 애니가 그보다 많이 휘두르면
                //   앞 N 번의 타격 뒤(+ 회수 몫 CARD_TAIL_KEEP)에서 몸짓을 끊는다. 자기형(강화 표시 이벤트)은 그대로
                float trimEnd = float.MaxValue;
                Debug.Log($"[Tempo] 카드 몸짓 {key} {act.Motion} {string.Join(" → ", cardChain)} — 엔진 타수 {byHit.Count} · 애니 타격 {marks.Count} · 애니 {cardTotal:F2}s");
                if (!OldTempo && !origTempo && !cardSelf && byHit.Count >= 1 && marks.Count > byHit.Count && !CARD_SPLIT.Contains(MKey(hidC, act.Motion)))   // 한 번 휘두르는 연타(CARD_SPLIT)는 끊지 않고 아래에서 나눈다
                {
                    marks = marks.GetRange(0, byHit.Count);
                    trimEnd = Mathf.Min(cardTotal, marks[marks.Count - 1] + CARD_TAIL_KEEP);
                    cardTotal = trimEnd;
                }
                float over = origTempo ? 0f : CardCut(u, cardChain, marks, cardTotal, CardCap(act.Motion, hidC), speed);
                if (trimEnd < cutEnd) cutEnd = trimEnd;
                float shown = Rm(cardTotal) / speed;
                if (over > 0.05f && shown > CardCap(act.Motion, hidC) + 0.4f) speed = Mathf.Min(CARD_SPEED_MAX, speed * shown / (CardCap(act.Motion, hidC) + 0.4f));
                CardSpeed = speed;
                CardAnimLog = string.Join(" → ", cardChain);
                // 원작 타수대로 보이기만 나눈다(엔진 피해 합 그대로) — 자기형(강화 표시 이벤트)은 빼고
                if (!cardSelf && byHit.Count == 1 && marks.Count > 1) { byHit = SplitHits(byHit, Mathf.Min(marks.Count, CARD_HITS_MAX)); hits = byHit.Count; }
                CardHitsShown = byHit.Count;
            }
            // 타격 맞추기(2026-10-06 「임팩트랑 타격이 따로 논다」) — 원작 이펙트가 있으면
            //   · 엔진이 한 번에 준 고학년 피해를 원작 박자(스파인 타격 이벤트 marks)대로 나눠 보인다(합계는 그대로)
            //   · 투사체가 있으면 탄이 닿는 때가 타격(쏘는 순간 + 날아가는 몫)
            //   · 그 맞는 시각들을 이펙트에 넘겨 대상 쪽 이펙트가 그 시각에 터지게(이펙트 안의 터지는 때만큼 일찍 튼다)
            string hidFx = Battle.Snapshot.Heroes[act.Actor.Index].Id;
            bool fxOn = !OldSync && (ult ? Bolzena.Fx.FxLibrary.HasUlt(hidFx) : Bolzena.Fx.FxRules.CardPlan(hidFx, anim, null).Count > 0);
            // 원작 타격 순간(맞는 소리 칸 · 때리는 창 표시를 250ms 로 묶은 것) — 엔진이 한 번에 준 피해를 그 수만큼 나눠 보인다(림(혼돈) 두 번)
            UltEngineHits = byHit.Count; UltOrigHits = 0; UltShownBy = "";
            if (!OldSync && ult && byHit.Count > 0)
            {
                // 1) 원작 타수 기준표(ult_hits.json — 설명 글의 「N회」 · 되풀이 이펙트 · 맞는 소리). 원작이 더 많이 때리면 보이기만 나눈다(엔진 합계 그대로).
                //    원작 < 우리 · 불명은 그대로. 횟수가 같아도 원작 시각이 또렷하면(되풀이 이펙트 · 맞는 소리) 그 시각에 맞춘다(셰이디)
                var uh = Bolzena.Fx.UltMotion.Hits(hidFx);
                // 무작위 대상 여러 번(셰이디 6타가 적 셋에 흩어짐) — 엔진은 적마다 몇 번째인지로 묶어 2 묶음이 된다. 피해 수가 원작 타수와 같으면 피해 하나를 한 박자로
                int dmgN = 0; foreach (var e in group) if (e.Kind == EventKind.Damage && !IsShot(e)) dmgN++;
                if (uh != null && uh.N == dmgN && dmgN > byHit.Count) byHit = SeqHits(group);
                // 란 몰이 사냥 — 처치하면 다음 적에게 4타를 더 준다(ifKill). 엔진 타격 차례가 적마다 0~3 이라 두 적의 피해가 같은 박자 넷에 둘씩 겹쳐 떴다 → 피해 하나를 한 박자로(2026-10-14)
                else if (UltReuseOnKill(hidFx) && byHit.Count > 1 && dmgN > byHit.Count) byHit = SeqHits(group);
                int strikeAt = UltPlan != null && UltPlan.S != null ? UltPlan.S.At : 0;
                // 원작이 지금 보이는 수보다 많이 때리면(판정이 「일치」여도 엔진이 타격을 한 묶음으로 준 경우 — 셰이디 6 인데 2 묶음) 나눈다. 원작 < 우리 · 불명은 손대지 않는다
                bool more = uh != null && uh.N > byHit.Count && uh.Verdict != null && !uh.Verdict.StartsWith("원작 <") && !uh.Verdict.StartsWith("불명");
                bool same = uh != null && uh.N == byHit.Count && uh.N > 1 && (uh.Why == "되풀이 이펙트" || uh.Why == "맞는 소리" || uh.Why.StartsWith("원작 애니"));
                if (uh != null && uh.Times.Count > 0 && (more || same))
                {
                    var ts = OrigTimes(uh.Times, uh.N, strikeAt, UltPlan?.S != null ? UltPlan.S.End : strikeAt, uh.Why, UltPlan?.Table);
                    UltOrigHits = uh.N; UltShownBy = "표 " + uh.Why;
                    marks = new List<float>(); foreach (var x in ts) marks.Add(x / 1000f);
                    if (byHit.Count < uh.N) byHit = SplitHits(byHit, uh.N);
                }
                // 2) 표가 없으면 몸짓 표(ult_motion)의 타격 순간 — 한 번에 온 피해만 나눈다
                else if (UltPlan != null && UltPlan.Table != null)
                {
                    var st = UltPlan.Table.Strikes();
                    UltOrigHits = uh != null ? uh.N : st.Count; UltShownBy = "몸짓 표";
                    if (st.Count > 0) { marks = new List<float>(); foreach (var x in st) marks.Add(x / 1000f); }
                    if (byHit.Count == 1 && marks.Count > 1 && (uh == null || uh.N > 1)) byHit = SplitHits(byHit, Mathf.Min(marks.Count, uh != null ? uh.N : 8));
                }
            }
            // 3) 원작 영상 맞춤 — 영상에서 읽은 타격 시각 · 수(보이기만, 엔진 합 그대로)
            if (fit != null && fit.Hits != null && fit.Hits.Length > 0)
            {
                marks = new List<float>(); foreach (var x in fit.Hits) marks.Add(x / 1000f);
                if (byHit.Count > 0) byHit = ReshapeHits(byHit, fit.Hits.Length);
                UltOrigHits = fit.Hits.Length; UltShownBy = "영상";
            }
            int lagMs = !fxOn ? 0 : ult ? Bolzena.Fx.BolzenaFx.UltLagMs(hidFx, UltPlan?.Pick) : Bolzena.Fx.BolzenaFx.CardLagMs(hidFx, anim, null);
            if (UltShownBy.StartsWith("표") || UltShownBy == "영상") lagMs = 0;
            if (cardChain != null && !CardLagKeep(hidC, act.Motion)) lagMs = 0;   // 카드 — 원작은 스파인 타격 이벤트 때 맞는다(투사체는 그만큼 일찍 쏜다 — UltFx.RunPlan shot = 타격 − 비행). 전엔 비행 0.33초를 더해 0.35~0.45초 늦었다
            if (cutSkips.Count > 0 || cutEnd < float.MaxValue) for (int i = 0; i < marks.Count; i++) marks[i] = Rm(marks[i]);   // 줄인 몸짓의 시계로   // 기준표 시각은 이펙트가 닿는 때 그대로
            var times = HitTimes(marks, Mathf.Max(1, fxOn ? byHit.Count : hits), speed);
            if (lagMs > 0) for (int i = 0; i < times.Count; i++) times[i] += lagMs / 1000f;
            Demo.UltLap.Lap("PlanUlt");

            // 다른 유닛은 살짝 물러서게(어둡게)
            foreach (var h in Heroes) if (h != u) h.Tint(new Color(0.62f, 0.62f, 0.68f));
            for (int i = 0; i < Enemies.Count; i++) if (!targets.Contains(i)) Enemies[i].Tint(new Color(0.62f, 0.62f, 0.68f));

            // 내딛기 — 근접은 대상 앞까지 달려가 잔상을 남긴다, 마법은 반 걸음
            Vector3 dest = u.Home;
            // 고학년은 원작대로 — 근접(앞줄 또는 원작 평타가 투사체 없는 사도)이고 몸짓이 제 몸을 멀리 옮기지 않으면 적 앞까지 가서 한다(UltMover)
            bool dash = ult ? UltMover(u, key, Battle.Snapshot.Heroes[act.Actor.Index].Id, anim, targets)
                : cardChain != null ? CardDashes(u, key, hidC, act.Motion, cardChain, targets.Count) : Look.MeleeArt(key) && targets.Count > 0;
            if (targets.Count > 0)
            {
                var tp = targets.Count == 1 ? Enemies[targets[0]].Feet : Mid(targets);
                float reach = targets.Count == 1 ? Enemies[targets[0]].Width() * 0.35f + 1.25f : 2.4f;
                // 자기형 · 제자리형(CARD_STAY) · 중열 · 후열 카드는 반 걸음도 안 나간다(fix_Y 9 — 우이(기억) 평타: 몸짓 이동 + 반 걸음이 겹쳐 다가가 보였다)
                bool stayHome = cardSelf || (cardChain != null && (CARD_STAY.Contains(MKey(hidC, act.Motion)) || HeroRow(hidC) == "중열" || HeroRow(hidC) == "후열") && !CARD_DASH.Contains(MKey(hidC, act.Motion)));
                dest = dash ? new Vector3(tp.x - reach, tp.y - 0.02f, 0) : stayHome ? u.Home : u.Home + new Vector3(0.5f, 0, 0);
            }
            // 적 뒤로 순간이동하는 카드(CARD_BEHIND — 티그(영웅) 저학년) — 달려가지 않고 제자리에서 시작해, 몸이 숨는 때에 적 뒤로 옮긴다(CardBehindCo)
            Vector2? behind = cardChain != null && targets.Count > 0 && CARD_BEHIND.TryGetValue(MKey(hidC, act.Motion), out var bh0) ? bh0 : (Vector2?)null;
            if (behind.HasValue) { dash = false; dest = u.Home; CardMoveLog = "원작 적 뒤 순간이동"; }
            var table = ult && UltPlan != null ? UltPlan.Table : null;   // 고학년 몸짓 표(ult_motion.json)
            if (fit != null && fit.Move != null)   // 원작 영상 맞춤 — 제자리 · 가는 때를 영상대로
            {
                if (fit.Move == "none") { dash = false; dest = u.Home; UltMoved = false; }
                else if (targets.Count > 0)
                {
                    dash = true; UltMoved = true;
                    table = FitWay(table, fit, UltNaturalMs);
                    var tp = targets.Count == 1 ? Enemies[targets[0]].Feet : Mid(targets);
                    float reach = targets.Count == 1 ? Enemies[targets[0]].Width() * 0.35f + 1.25f : 2.4f;
                    dest = new Vector3(tp.x - reach, tp.y - 0.02f, 0);
                    if (fit.Dest == "on") dest = new Vector3(tp.x - 0.3f, tp.y - 0.02f, 0);   // 대상 자리에 내리찍기(에르핀(왕도) — 주먹 = 폭발 자리)
                }
                UltMoveWhy += " · 영상 맞춤 " + fit.Move;
            }
            else if (ult && (HeroRow(hidC) == "중열" || HeroRow(hidC) == "후열"))   // 맞춤 표에 이동이 없으면 원작 열대로 — 중열 · 후열 고학년은 제자리(10-17 사용자)
            {
                dash = false; dest = u.Home; UltMoved = false;
                UltMoveWhy += " · " + HeroRow(hidC) + " 제자리";
            }
            if (table != null && dash && table.Dest == "behind")              // 무리 뒤로(모모 — 적 너머로 넘어가 친다)
            {
                float x1 = float.MinValue, y = 0; int n = 0;
                for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) { x1 = Mathf.Max(x1, Enemies[i].Feet.x + Enemies[i].Width() * 0.4f); y += Enemies[i].Feet.y; n++; }
                if (n > 0) dest = new Vector3(Mathf.Min(x1 + 0.5f, 7.2f), y / n - 0.02f, 0);
            }
            if (table != null && !dash) dest = u.Home;                       // 표에서 제자리형이면 반 걸음도 안 나간다
            if (table != null && dash && table.Dest == "middle")              // 무리 가운데로(로니 · 니콜) — 살아 있는 적들 발의 가운데, 몸 반쯤 앞에 선다
            {
                var all = new List<int>(); for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) all.Add(i);
                if (all.Count > 0) { var mid = Mid(all); dest = new Vector3(mid.x - 0.5f, mid.y - 0.02f, 0); }
            }
            Demo.UltLap.Lap("UltMover");
            int baseOrder = 46;
            u.SetOrder(ult ? 330 : 60);
            if (ult)
            {
                fxBoost = 160;
                // 맞는 적은 어둠(300 · 302) · 흰 구름(315) 위, 고학년 이펙트(325) · 사도(330) 아래 — 맞는 적끼리도 앞뒤(발 y) 차례 그대로(316~320)
                var tOrd = new List<int>(targets);
                tOrd.Sort((a, b) => Enemies[a].BaseOrder.CompareTo(Enemies[b].BaseOrder));
                for (int r = 0; r < tOrd.Count; r++) Enemies[tOrd[r]].SetOrder(Mathf.Min(320, 316 + r));
                ScreenFx.I.Dim(0.7f, 300, 5f);
                FieldRig.I.Zoom = 1f;
            }

            Demo.UltLap.Lap("Dim");
            float dashTime = dash ? Mathf.Min(0.16f, times[0] * 0.8f + 0.06f) : 0.12f;
            float t0 = Clock.Now;
            float duration = ult && UltPlan != null && UltPlan.Anim != null ? u.PlayChain(UltPlan.Anim, UltPlan.Chain, speed) : cardChain != null ? 0 : u.Play(anim, speed);
            if (ult && (cutSkips.Count > 0 || cutEnd < float.MaxValue) && UltPlan != null && UltPlan.Anim != null)
            {
                u.CutChain(UltPlan.Anim, UltPlan.Chain, new List<(float, float)>(cutSkips), cutEnd);
                duration = Rm(duration * speed) / speed;
            }
            if (cardChain != null)   // 카드 — 조각을 잇고(CARD_CHAIN) 줄인 몫을 건너뛴다
            {
                var rest = cardChain.GetRange(1, cardChain.Count - 1);
                duration = u.PlayChain(cardChain[0], rest, speed);
                if (cutSkips.Count > 0 || cutEnd < float.MaxValue) { u.CutChain(cardChain[0], rest, new List<(float, float)>(cutSkips), cutEnd); duration = Rm(duration * speed) / speed; }
                CardShownMs = Mathf.RoundToInt(duration * 1000);
                if (act.Motion == Motion.Skill1 && !Bolzena.Fx.BolzenaFx.OriginalOnly) CardOrb(u);   // 저학년 시작 흰 구슬 섬광(원작 공용 연출)
                PetAct(u, act.Motion, speed, times.Count > 0 ? times[0] : 0); DropAct(u, act.Motion, speed);   // 곁의 소환수도 같이(에피칸 — 강공에 불 뿜기 · 저학년에 춤)
                if (behind.HasValue) StartCoroutine(CardBehindCo(u, seq, targets, behind.Value, speed));
                StartCoroutine(CardKeepY(u, seq, duration));   // 뛰어오르는 몸짓이 화면 위로 나가면 몸을 내린다
            }
            float tStart = Time.time;                                  // 몸짓 · 이펙트 · 타격이 같이 보는 시계(게임 시간 — 멈칫 · 배속을 같이 탄다)
            fxClock = fxOn ? new Bolzena.Fx.UltClock() : null;
            if (ult) ultOwnerView = u;   // 이 사도의 곁 소환수는 고학년 동안 숨는다
            if (ult) { UltAnimLog = UltPlan != null && UltPlan.Anim != null ? UltPlan.Anim + (UltPlan.Chain.Count > 0 ? " → " + string.Join(" → ", UltPlan.Chain) : "") : anim; UltTargets = targets.Count; }
            Demo.UltLap.Lap("PlayChain");
            if (ult) Bolzena.Fx.BolzenaFx.UltTweak = FitTweak(hidFx, fit, speed);   // 원작 영상 맞춤 — 장 자리 · 시각(이 고학년만)
            if (cardChain != null) Bolzena.Fx.BolzenaFx.CardClock = ms => Rm(ms / 1000f) / Mathf.Max(0.01f, speed) * 1000f;   // 카드 장 표 시각(몸짓 ms) → 화면 ms(이 몸짓만)
            bool orig = OrigFx(act, u, anim, marks, speed, ult, targets, dash ? dest : (Vector3?)null, fxOn ? times : null, cardSelf, cardChain);
            Bolzena.Fx.BolzenaFx.CardClock = null;
            ownHitFx = orig && LastFxCall != null && LastFxCall.TargetParts > 0 && !cardSelf;   // 사도 고유 맞음 장이 적에게 터지면 공용 타격 낱장은 안 겹친다(2026-10-14)
            if (ult) Bolzena.Fx.BolzenaFx.UltTweak = null;
            var hitAtMs = new List<float>();
            Demo.UltLap.Lap("OrigFx");
            var cast = HeroSfx(key, act.Motion, false);
            bool evSnd = ult && UltSounds(key, speed);                 // 고학년 소리 — 스파인 SFX 칸 시각대로(되면 시전 소리를 따로 안 낸다)
            if (cast != null && !evSnd) { var hs0 = Sfx.PlayHeld(cast, 0.7f); if (ult && hs0 != null) ultHeld.Add(hs0); }
            if (act.Motion == Motion.Attack2 || act.Motion == Motion.Skill1) Sfx.Voice(key, act.Motion == Motion.Skill1 ? "spskill" : "powerattack", "shout");
            Demo.UltLap.Lap("소리");
            bool tableMove = table != null && dash;
            if (tableMove) StartCoroutine(UltTravelCo(u, table, dest, speed, times.Count > 0 ? times[times.Count - 1] : 0));   // 원작 시각대로 가고(달리기 · 순간이동 · 뛰어들기) 돌아온다
            else
            {
                StartCoroutine(u.MoveTo(dest, dashTime, Ease.InCubic));
                if (dash && DASH_GHOSTS) StartCoroutine(Ghosts(u, dashTime, new Color(0.75f, 0.9f, 1f, 0.22f)));
            }
            if (ult && !orig) StartCoroutine(UltCharge(u, times[0]));   // 원작 고학년 이펙트가 없을 때만(우이) 자체 모으기

            // 고학년 카메라 — 시전자에게 당겼다가 첫 타격에 적 쪽으로
            if (ult) StartCoroutine(CamTo(u.Center + new Vector3(1.2f, 0, 0), 1.18f, 0.35f));
            if (ult) ultPunchOn = HasUltPunch(hidFx);   // 표가 있는 고학년은 보통 타격 흔들림을 더 낮춰 큰 순간과 차이를 낸다
            if (ult && HasUltPunch(hidFx)) StartCoroutine(UltPunchCo(hidFx, speed, tStart, u, targets));   // 원작 영상의 번쩍 · 큰 흔들림 · 어두워짐 · 다가감(UltVideoFit ULT_PUNCH)
            if (ult && ULT_LOOP.TryGetValue(Battle.Snapshot.Heroes[act.Actor.Index].Id, out var loop)) StartCoroutine(UltLoopCo(u, loop, duration + 0.3f));   // 적 무리를 한 바퀴(우로스) — 화면 밖 막기도 같이
            else if (ult && KEEP_ON_SCREEN.Contains(Battle.Snapshot.Heroes[act.Actor.Index].Id)) StartCoroutine(KeepOnScreen(u, duration + 0.3f));   // 몸짓이 몸을 화면 왼쪽 밖으로 끌고 가면(버터 · 우로스 · 셰이디(역전) 루트 이동) 안으로 당긴다
            if (ult && ULT_KEEP_TOP.Contains(Battle.Snapshot.Heroes[act.Actor.Index].Id)) StartCoroutine(CardKeepY(u, seq, duration));   // 뛰어오른 고학년 몸이 화면 위로 나가면 내린다(카드와 같은 것)
            if (ult && ULT_STANDING.TryGetValue(hidFx, out var ultSt)) StartCoroutine(UltStandingCo(ultSt, tStart, speed, targets));   // 몸이 화면 밖에 나간 사이 원작 고학년 클로즈업 스파인(비비(신성) vividivineult)
            Demo.UltLap.Lap("이동 · 카메라");

            foreach (var e in pre) yield return ApplyConsequence(e, u);
            Demo.UltLap.End();

            // 타격들 — 시각은 몸짓을 시작한 때부터(앞의 일(pre)을 기다린 몫도 센다. 전에는 그 뒤부터 세어 이펙트보다 늦었다)
            float elapsed = OldSync ? 0 : Time.time - tStart;
            int k = 0, seqTotal = 0;
            foreach (var kv in byHit)
            {
                float at = k < times.Count ? times[k] : times[times.Count - 1] + 0.1f * (k - times.Count + 1);
                while (elapsed < at) { yield return null; elapsed = OldSync ? elapsed + Time.deltaTime : Time.time - tStart; if (fxClock != null) fxClock.Ms = elapsed * 1000f; }
                hitAtMs.Add((Time.time - tStart) * 1000f);
                if (ult && k == 0) StartCoroutine(CamTo(MidBody(targets) + new Vector3(-0.8f, 0.2f, 0), 1.08f, 0.25f));
                aoeAt = targets.Count > 1 ? MidBody(targets) : (Vector3?)null;
                int aoeHitTotal = 0;
                bool aoeFxDone = false;
                bool last = k == byHit.Count - 1;
                var hitSfx = HeroSfx(key, act.Motion, true);
                int dn = 0;
                foreach (var e in kv.Value)
                {
                    if (e.Kind == EventKind.Damage && !IsShot(e))
                    {
                        // 전체 공격 — 적마다 조금씩 늦게 물결처럼(큰 이펙트는 첫 적에서 무리 가운데에 한 번). 몰리면 간격을 줄이고 합이 AOE_LAG_MAX 를 넘지 않게
                        if (aoeAt.HasValue && dn > 0) { float lag = AoeLag(targets.Count); if (lag * dn <= AOE_LAG_MAX) yield return Clock.WaitU(lag); }
                        Impact(e, u, ult, last, hitSfx, kv.Value.Count > 3 || aoeAt.HasValue, !aoeFxDone); seqTotal += e.Value; dn++;   // 무리면 멈칫은 한 번(물결로 늦춘 적마다 다시 멈추지 않게)
                        if (aoeAt.HasValue) { aoeFxDone = true; aoeHitTotal += e.Value; }
                    }
                    else yield return ApplyConsequence(e, u);   // 쏘는 한 대(Shot · Damage "shot")도 여기서 — 카드 타격 뒤에 차례로
                }
                if (cardChain != null && !kv.Value.Exists(ev0 => ev0.Kind == EventKind.Damage || ev0.Kind == EventKind.Shot)) CardGhostHit(targets, act.HitKind);   // 나누어 보인 박자에 피해 조각이 없으면(작은 피해) 맞는 반응만
                // 전체 공격 — 한 대의 합을 무리 가운데에 크게(적마다 숫자는 그 적 위에 그대로)
                // 한 적 여러 대 — 마지막 대에 합을 크게(2026-10-06 리빌딩 「다단 합계 숫자 크게」)
                if (last && !aoeAt.HasValue && byHit.Count > 1 && seqTotal > 0 && targets.Count == 1 && targets[0] < Enemies.Count && Enemies[targets[0]] != null)
                    Vfx.Word(Enemies[targets[0]].Top + new Vector3(0, 0.9f, 0), "합 " + seqTotal.ToString("N0"), ult ? 0.62f : 0.42f, new Color(1f, 0.92f, 0.7f), new Color(0.3f, 0.1f, 0), 1.1f, 1.3f, null, 480, 0.5f);
                if (aoeAt.HasValue && aoeHitTotal > 0 && (ult || last))
                    Vfx.Word(aoeAt.Value + new Vector3(0, 1.3f + k * 0.3f, 0), "합 " + aoeHitTotal.ToString("N0"), ult ? 0.5f : 0.36f, new Color(1f, 0.92f, 0.7f), new Color(0.3f, 0.1f, 0), 1.0f, 1f, null, 480, 0.5f);
                if (ult && last) UltFinish(targets.Count > 1 ? MidBody(targets) : Enemies[targets.Count > 0 ? targets[0] : 0].Center, Heroes[act.Actor.Index], orig);
                aoeAt = null;
                k++;
            }
            ownHitFx = null;
            // 남은 몸짓 — 카드는 마지막 타격 뒤 조금 더 보고 돌아온다. 고학년은 몸짓을 끝까지(조각 · 고리 다) 하고 돌아온다
            //   (웹판: 달려간 고학년은 total + back 까지. 전에는 타격 + 0.6초에 Idle 로 끊어 끝 조각이 잘렸다)
            float tail = ult ? Mathf.Max(duration, table != null ? Rm(table.TotalMs / 1000f) / speed : 0) : cardChain != null ? duration : Mathf.Min(duration, elapsed + 0.32f);   // 카드도 애니 끝까지(줄인 길이)
            // 카드 — 마지막 타격 뒤 잠깐(HOLD_AFTER_HIT)만 붙들고 다음 입력을 받는다. 남은 몸짓 · 복귀 · 색 되돌리기는 뒤에서(Finish)
            if (!ult && !OldTempo && !OldSync)
            {
                float hold = Mathf.Min(tail, elapsed + HOLD_AFTER_HIT);
                while (elapsed < hold) { yield return null; elapsed = Time.time - tStart; if (fxClock != null) fxClock.Ms = elapsed * 1000f; }
                StartCoroutine(Finish(u, seq, tStart, tail, fxClock, dash, dash ? 0.22f : 0.15f, baseOrder - act.Actor.Index * 2, true));
                yield break;
            }
            while (elapsed < tail) { yield return null; elapsed = OldSync ? elapsed + Time.deltaTime : Time.time - tStart; if (fxClock != null) fxClock.Ms = elapsed * 1000f; }
            if (ult) SyncReport(hitAtMs, tStart);
            if (ult)
            {
                UltMotionMs = Mathf.RoundToInt(duration * 1000);
                UltCutMs = Mathf.Max(0, Mathf.RoundToInt((duration - elapsed) * 1000));
                // 소리 꼬리 — 몸짓이 끝났는데 남은 고학년 소리는 0.3초에 줄여 끊는다(짧은 소리는 그대로 — 늘이지 않는다)
                int faded = 0;
                foreach (var a in ultHeld) if (a != null && a.isPlaying) { faded = Mathf.Max(faded, Mathf.RoundToInt((a.clip.length - a.time) * 1000)); StartCoroutine(Sfx.FadeOut(a, 0.3f)); }
                UltSndTailMs = faded;
                ultHeld.Clear();
            }
            if (tableMove) { while (travelling) { yield return null; } }   // 표대로 돌아오는 중이면 끝까지
            else if (u.Feet != u.Home)
            {
                if (dash) u.Loop("Move");
                yield return u.Return(dash ? 0.24f : 0.15f);
                if (dash) u.Idle();   // 반 걸음만 나갔으면 하던 몸짓(뒤에 Idle 이 이어져 있다)을 끊지 않는다
            }
            u.SetOrder(baseOrder - act.Actor.Index * 2);
            foreach (var h in Heroes) h.Tint(Color.white);
            foreach (var en in Enemies) if (en) en.Tint(Color.white);
            if (ult)
            {
                ScreenFx.I.Dim(0, 300, 3f);
                fxBoost = 0;
                yield return CamTo(Vector3.zero, 1f, 0.4f);
                for (int i = 0; i < Enemies.Count; i++) if (!Dead(i)) Enemies[i].SetOrder(Enemies[i].BaseOrder);   // 쓰러지는 중인 적은 그대로(마지막 일격 구름이 앞으로 나오지 않게)
            }
            ResetHeroOrder();
        }

        // 사도 소리 파일 목록 · 길이 — 싸움을 열 때 한 번(고학년 순간에 Resources.LoadAll 을 하지 않게)
        static readonly Dictionary<string, (List<string>, Dictionary<string, float>)> heroClips = new Dictionary<string, (List<string>, Dictionary<string, float>)>();

        // ── 원작 이펙트(com.bolzena.fx) ── 몸짓 시작에 건다. 고학년은 사도의 원작 고학년 한 벌(시각은 스파인 계획 · 이 몸짓의 배속),
        // 카드는 그 동작(Attack1 · Attack2 · Skill1)의 원작 이펙트. 원작 것이 없으면 false — 자체 Vfx 가 대신한다
        public bool LastFxOrig;                          // 점검 — 마지막 몸짓에 원작 이펙트를 틀었나
        public int LastFxParts;
        bool OrigFx(BattleEvent act, UnitView u, string anim, List<float> marks, float speed, bool ult, List<int> targets, Vector3? dashTo, List<float> hitTimes = null, bool selfFx = false, List<string> chain = null)
        {
            List<float> hitMs = null;
            if (hitTimes != null) { hitMs = new List<float>(); foreach (var t in hitTimes) hitMs.Add(t * 1000f); }
            LastFxCall = null;
            LastFxOrig = false; LastFxParts = 0;
            var hs = Battle.Snapshot.Heroes[act.Actor.Index];
            string hid = hs.Id;
            // (10-18 사용자 2차) 원작 성격 갈래 장(비비(신성) 고학년 빛기둥 · 끝 · 레이저 — _jolly_ 자리)을 시전 사도 성격으로. 그 성격 판이 없으면 jolly 그대로(UltFx.MoodName)
            Bolzena.Fx.UltFx.Mood = hs.Nature != null && Bolzena.Fx.UltFx.MOOD.TryGetValue(hs.Nature, out var md) ? md : null;
            var caster = u.Fx;
            if (dashTo.HasValue)   // 달려가 때리는 사도 — 이펙트는 닿는 자리에서(몸짓 시작 0.16초 안에 거기 간다)
            {
                var d = dashTo.Value; var b = caster;
                caster = new Bolzena.Fx.FxActor { FeetAt = () => d, Height = b.Height, Party = true, Key = b.Key, Point = b.Point, Muzzle = b.Muzzle, Weapon = b.Weapon };
            }
            var mouth = ult ? null : PetMouth(u, act.Motion);   // 곁 소환수가 쏘는 몸짓(에피칸 강공 불덩이) — 투사체 · 총구 장은 소환수 입에서
            if (mouth != null) { var b = caster; caster = new Bolzena.Fx.FxActor { FeetAt = b.FeetAt, Height = b.Height, Party = true, Key = b.Key, Point = b.Point, Muzzle = mouth, Weapon = mouth }; }
            var tfx = new List<Bolzena.Fx.FxActor>();
            foreach (var ti in targets) if (ti < Enemies.Count && Enemies[ti] != null) { var f = Enemies[ti].Fx; f.Alive = !Dead(ti); tfx.Add(f); }
            bool aoe = targets.Count > 1;
            Bolzena.Fx.FxCall c;
            if (ult)
            {
                if (!Bolzena.Fx.FxLibrary.HasUlt(hid)) return false;
                var sync = UltPlan != null && UltPlan.Anim != null ? Bolzena.Fx.SpineFx.Sync(UltPlan) : null;
                if (sync != null) sync.Speed = speed;
                if (sync != null && UltFitNow?.Chain != null) sync.Pick = null;   // 조각을 잇는 사도 — 장도 갈래 하나로 거르지 않는다
                var allies = new List<Bolzena.Fx.FxActor>();
                foreach (var h in Heroes) if (h != u && h) allies.Add(h.Fx);
                c = Bolzena.Fx.BolzenaFx.Ult(hid, caster, tfx, null, sync, aoe, allies, hitMs, fxClock);
            }
            else
            {
                if (tfx.Count == 0 && act.Target.Side == Side.Party && act.Target.Index < Heroes.Count) tfx.Add(Heroes[act.Target.Index].Fx);   // 아군에게 거는 카드
                if (selfFx) { tfx.Clear(); tfx.Add(act.Target.Side == Side.Party && act.Target.Index < Heroes.Count ? Heroes[act.Target.Index].Fx : u.Fx); aoe = false; }   // 원작이 자기 · 아군에게 거는 몸짓(BattleDirector.Cards CARD_SELF) — 몸짓 이펙트는 시전자(아군) 쪽, 맞음 · 숫자만 적에게
                var ms = new List<int>();
                foreach (var m in marks) ms.Add(Mathf.RoundToInt(m * 1000));
                var sync = new Bolzena.Fx.FxSync { Anim = anim, Impact = ms.Count > 0 ? ms[0] : (int?)null, Marks = ms, End = ms.Count > 0 ? ms[ms.Count - 1] : (int?)null, Speed = speed };
                if (Bolzena.Fx.FxRules.HasEventFx(hid))   // 원작 이벤트 ID 표가 있는 사도 — 그 열의 이벤트 칸(전열 Front · 중열 Middle · 후열 Back)
                {
                    var row = HeroRow(hid);
                    var pieces = chain ?? new List<string> { anim };
                    sync.Events = u.RowEvents(pieces, row == "중열" ? "Event_Middle" : row == "후열" ? "Event_Back" : "Event_Front");
                    if (sync.Events.Count == 0) sync.Events = u.RowEvents(pieces, "Event");   // 열 칸이 없는 사도(리뉴아 · 벨라 · 요미 …)는 한 칸 Event
                }
                var alliesC = new List<Bolzena.Fx.FxActor>();   // (10-18 2차) 아군에게 가는 카드 장(실비아 초대장 — UltFx.ALLIES_ONLY 만)
                foreach (var h in Heroes) if (h != u && h) alliesC.Add(h.Fx);
                c = Bolzena.Fx.BolzenaFx.Card(hid, caster, tfx, sync, null, aoe, hitMs, fxClock, alliesC,
                    dropShot: NearUpperCut(hid, act.Motion));   // (10-18 4차) 가까운 적 — 검기(proj) 없이 올려 베기 맞음
            }
            LastFxCall = c;
            LastFxParts = c != null ? c.Parts : 0;
            LastFxOrig = LastFxParts > 0;
            return LastFxOrig;
        }

        // ── 타격 맞추기 ──
        public static bool OldSync => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldsync") >= 0;   // 고치기 전 방식(점검 전후 비교)
        Bolzena.Fx.UltClock fxClock;
        public Bolzena.Fx.FxCall LastFxCall;
        // 점검(-ultsheet) — 그때 일어난 일(갈래 · 글). 피해 · 이동 · 고학년 몸짓 시작
        public static System.Action<string, string> Trace;
        public const float ULT_MAX = 7.0f;              // 고학년 몸짓 최대(초, 화면 시계) — 원작이 7초 안이면 그대로(2026-10-06 사용자 원칙 · 컷인은 따로)
        public const float ULT_SPEED_MAX = 1.2f;        // 7초 넘는 고학년도 많아야 이 배속까지
        public float UltSpeed = 1f; public int UltNaturalMs;   // 점검 — 실제 배속 · 원래 길이(1배속 ms)
        public int UltEngineHits, UltOrigHits;          // 점검 — 엔진이 준 타격 수 · 원작 타격 수
        public string UltShownBy = "";                  // 보이는 타격 시각을 어디서 가져왔나

        // 원작 타격 시각(ms, 1배속) n 개 — 몸짓의 때리는 순간보다 한참 앞선 것(모으기 · 예고 이펙트)은 빼고, 이동형은 닿은 뒤부터. 모자라면 고르게 채운다
        static List<int> OrigTimes(List<int> src, int n, int strikeAt, int strikeEnd, string why, Bolzena.Fx.UltWay w)
        {
            // 몸짓이 실제로 때리는 순간(strikeAt — 둘째 효과음 · 닿는 때) 앞의 표 시각은 쓰지 않는다. 디아나: 기 모으는 이펙트(1002405 ×5, 233~767)를
            // 타격으로 잡아 정권(1433)보다 먼저 터지던 것(2026-10-06 사용자). 시각이 짐작(고르게)이면 몸짓의 때리는 창(at~end)에 고르게
            int from = w != null && w.Moves ? Mathf.Max(w.HitMs, strikeAt - 100) : strikeAt - 100;
            bool exact = why == "되풀이 이펙트" || why == "맞는 소리" || (why != null && why.StartsWith("원작 애니"));
            var ts = exact ? src.FindAll(t => t >= from) : new List<int>();
            if (!exact)
            {
                int a0 = Mathf.Max(from + 100, strikeAt), b0 = Mathf.Max(strikeEnd, a0 + 100 * (n - 1));
                var r = new List<int>();
                for (int i = 0; i < n; i++) r.Add(n > 1 ? Mathf.RoundToInt(a0 + (b0 - a0) * i / (n - 1f)) : a0);
                return r;
            }
            if (ts.Count == 0) ts = new List<int> { Mathf.Max(strikeAt, from) };
            if (ts.Count == n) return ts;
            var res = new List<int>();
            if (ts.Count > n) { for (int i = 0; i < n; i++) res.Add(ts[n > 1 ? Mathf.RoundToInt(i * (ts.Count - 1f) / (n - 1)) : 0]); return res; }
            int a = ts[0], b = Mathf.Max(ts[ts.Count - 1], a + 90 * (n - 1));
            for (int i = 0; i < n; i++) res.Add(n > 1 ? Mathf.RoundToInt(a + (b - a) * i / (n - 1f)) : a);
            return res;
        }
        public int UltHitCount, UltFxImpacts, UltSyncFirstMs, UltSyncMaxMs;   // 점검 — 타격 수 · 대상 쪽 이펙트 충격 수 · 첫 타격 어긋남 · 가장 큰 어긋남(ms)
        public string UltSyncLog = "";

        // 한 번에 온 피해를 n 번으로 나눠 보인다 — 값은 나누어 합이 그대로, 체력은 그만큼씩 줄고, 치명 · 다른 일(상태 · 격파 · 쓰러짐)은 마지막 타격에
        // 피해 하나 = 한 박자(나오는 차례대로). 첫 피해 앞의 일은 pre 라 빼고, 피해 뒤의 일은 그 박자에
        static SortedDictionary<int, List<BattleEvent>> SeqHits(List<BattleEvent> group)
        {
            var res = new SortedDictionary<int, List<BattleEvent>>();
            int cur = -1, n = 0;
            foreach (var e in group) if (e.Kind == EventKind.Damage && !IsShot(e)) n++;
            foreach (var e in group)
            {
                if (e.Kind == EventKind.Damage && !IsShot(e)) { cur++; res[cur] = new List<BattleEvent>(); e.Hit = cur; e.Hits = n; }
                if (cur >= 0) res[cur].Add(e);
            }
            return res;
        }

        static SortedDictionary<int, List<BattleEvent>> SplitHits(SortedDictionary<int, List<BattleEvent>> byHit, int n)
        {
            // 엔진 타격 k 번을 n 번으로 — 타격마다 몫(n/k, 나머지는 앞쪽에)으로 나누고, 값은 나누어 합이 그대로 · 체력은 그만큼씩 · 치명과 다른 일은 그 타격의 마지막 조각에
            var groups = new List<List<BattleEvent>>(byHit.Values);
            int k = Mathf.Max(1, groups.Count);
            var res = new SortedDictionary<int, List<BattleEvent>>();
            int slot = 0;
            for (int g = 0; g < groups.Count; g++)
            {
                int m = n / k + (g < n % k ? 1 : 0);
                if (m < 1) m = 1;
                for (int j = 0; j < m; j++) res[slot + j] = new List<BattleEvent>();
                foreach (var e in groups[g])
                {
                    if (e.Kind != EventKind.Damage || e.Value < m || IsShot(e)) { res[slot + m - 1].Add(e); continue; }   // 쏘는 한 대는 나누지 않는다
                    int done = 0;
                    for (int j = 0; j < m; j++)
                    {
                        int v = j == m - 1 ? e.Value - done : Mathf.RoundToInt(e.Value * (j + 1f) / m) - done;
                        done += v;
                        res[slot + j].Add(new BattleEvent
                        {
                            Kind = e.Kind, Actor = e.Actor, Target = e.Target, Value = v, Blocked = j == m - 1 ? e.Blocked : 0, Crit = e.Crit && j == m - 1, Weak = e.Weak, By = e.By,
                            HpAfter = e.HpAfter + (e.Value - done), BlockAfter = e.BlockAfter, Hit = slot + j, Hits = n, HitKind = e.HitKind, Motion = e.Motion, Text = e.Text,
                        });
                    }
                }
                slot += m;
            }
            return res;
        }

        // 원작 이펙트의 충격(대상 쪽 장이 틀린 때 + 그 이펙트 안의 터지는 때)마다 가장 가까운 타격과의 차이(ms, + 면 이펙트가 늦다).
        // 충격이 타격 위에 떨어지는지를 본다(타격이 더 많은 다단은 남는 타격에 공용 맞음 불꽃만 — 그건 타격 그 순간이라 따로 안 잰다)
        void SyncReport(List<float> hitAtMs, float tStart)
        {
            UltHitCount = hitAtMs.Count; UltFxImpacts = 0; UltSyncFirstMs = 0; UltSyncMaxMs = 0; UltSyncLog = "";
            var imp = new List<float>();
            if (LastFxCall != null) foreach (var f in LastFxCall.Fired) if (f.Target) imp.Add(f.Ms - tStart * 1000f + f.ImpactMs);
            imp.Sort();
            UltFxImpacts = imp.Count;
            if (imp.Count == 0 || hitAtMs.Count == 0) { UltSyncLog = "-"; return; }
            var parts = new List<string>();
            float first = float.MaxValue;
            foreach (var x in imp)
            {
                float best = float.MaxValue;
                foreach (var h in hitAtMs) if (Mathf.Abs(x - h) < Mathf.Abs(best)) best = x - h;
                int d = Mathf.RoundToInt(best);
                UltSyncMaxMs = Mathf.Max(UltSyncMaxMs, Mathf.Abs(d));
                parts.Add(d.ToString());
                if (Mathf.Abs(x - hitAtMs[0] - best) < 1 && Mathf.Abs(best) < Mathf.Abs(first)) first = best;
            }
            UltSyncFirstMs = first == float.MaxValue ? 0 : Mathf.RoundToInt(first);
            UltSyncLog = string.Join(",", parts);
        }

        // 달려갈 때 잔상 — 원작에는 없다. 시전 직후 사도 뒤에 회청색 연기 덩어리처럼 보여(2026-10-15 20명 대조: 클로에 · 티그 · 란 · 평타 · 강공 대부분) 끈다
        const bool DASH_GHOSTS = false;
        IEnumerator Ghosts(UnitView u, float dur, Color c)
        {
            float t = 0;
            while (t < dur + 0.04f)
            {
                u.Ghost(c, 0.22f);
                float step = LowSpecFx.GhostStep;   // 저사양 모드면 잔상 반
                yield return Clock.Wait(step);
                t += step;
            }
        }

        bool ultPunchOn;                                 // 지금 고학년이 타격감 표(ULT_PUNCH)를 쓰나

        IEnumerator CamTo(Vector3 center, float zoom, float dur)
        {
            var rig = FieldRig.I;
            Vector2 c0 = rig.Center;
            float z0 = rig.Zoom;
            Vector2 c1 = new Vector2(center.x, center.y) * (zoom > 1.001f ? 0.55f : 0f);
            yield return Clock.Tween(dur, t =>
            {
                float k = Ease.InOutCubic(t);
                rig.Center = Vector2.Lerp(c0, c1, k);
                rig.Zoom = Mathf.Lerp(z0, zoom, k);
            });
        }

        // ── 템포(2026-10-09 「모션이 너무 길다 — 2배속이 아니면 루즈」) ──
        public static readonly bool OldTempo = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldtempo") >= 0;   // 고치기 전 흐름(전후 비교)
        const float HOLD_AFTER_HIT = 0.12f;      // 마지막 타격 뒤 붙드는 몫(게임 초) — 그 뒤는 다음 입력 · 다음 적
        // -tempolog — 연출 한 칸이 붙든 시간(화면 초, 0.1초 넘는 것만). 어디서 시간이 드는지 재는 점검용
        static readonly bool TempoLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-tempolog") >= 0;
        IEnumerator Timed(string what, IEnumerator co)
        {
            if (!TempoLog) { yield return co; yield break; }
            float t0 = Clock.RealNow;
            yield return co;
            float d = Clock.RealNow - t0;
            if (d > 0.1f) Debug.Log($"[TempoE] {what} {d:F2}s");
        }
        int actSeq;                              // 몸짓을 시작할 때마다 하나씩
        readonly Dictionary<UnitView, int> unitSeq = new Dictionary<UnitView, int>();
        int BeginAct(UnitView u) { unitSeq[u] = ++actSeq; return actSeq; }
        bool Stale(UnitView u, int seq) => !u || !unitSeq.TryGetValue(u, out var s) || s != seq;

        /// <summary>점검(-cardsheet) — 카드 없이 그 사도의 몸짓 하나를 적 하나에게(피해 1) 그대로 연출한다. 판은 바뀌지 않는다.</summary>
        public IEnumerator DemoMotion(int hero, Motion m, int target)
        {
            var hs = Battle.Snapshot.Heroes[hero];
            var es = target < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[target] : null;
            var act = new BattleEvent { Kind = EventKind.Act, Actor = UnitRef.Party(hero), Target = UnitRef.Enemy(target), Motion = m, HitKind = Look.Hero(hs.Id).Hit };
            var dmg = new BattleEvent { Kind = EventKind.Damage, Actor = UnitRef.Party(hero), Target = UnitRef.Enemy(target), Value = 1, Hit = 0, Hits = 1, HpAfter = es != null ? es.Hp : 1 };
            yield return HeroAct(act, new List<BattleEvent> { dmg });
        }
        public void RequestEnd() => request = (ReqKind.End, 0, 0);
    }
}
