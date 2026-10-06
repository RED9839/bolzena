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
                if (e.Kind == EventKind.Damage || e.Kind == EventKind.PartyHurt)
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
            ult |= act.Text == "ult";

            // 대상들
            var targets = new List<int>();
            foreach (var e in group) if (e.Kind == EventKind.Damage && !targets.Contains(e.Target.Index)) targets.Add(e.Target.Index);
            if (targets.Count == 0 && act.Motion != Motion.None && act.Target.Side == Side.Enemy && act.Target.Index < Enemies.Count) targets.Add(act.Target.Index);
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
                yield return Clock.Wait(0.25f);
                yield break;
            }

            string anim = u.AnimFor(act.Motion);
            float speed = ult ? (key == "ed" ? 1.35f : 1.05f) : act.Motion == Motion.Attack1 && key == "leets" ? 1.7f : 1.3f;
            var marks = u.Strikes(u.F(anim));   // 변신 중이면 실제로 트는 꼬리판(Attack1_1_Change)의 때리는 순간
            // 카드 — 이어지는 조각까지(BattleDirector.Cards.cs CARD_CHAIN) · 때리는 순간은 조각 전체의 타격 이벤트(0ms 시전 표시 빼고)
            string hidC = Battle.Snapshot.Heroes[act.Actor.Index].Id;
            List<string> cardChain = null; float cardTotal = 0;
            if (!ult && !OldSync && u.SpineArt && u.SkelData != null && anim != null)
            {
                cardChain = CardChain(u, hidC, act.Motion, anim);
                anim = CardFxAnim(hidC, act.Motion, cardChain[0]);   // 이 뒤 anim 은 이펙트 갈래 · 시전 본 고르기에만(몸짓은 cardChain)
                marks = CardHitsFix(hidC, act.Motion, CardStrikes(u, cardChain, out cardTotal));
            }
            // 고학년 — 원작 조각을 웹판처럼 잇는다(SpineMotion.PlanUlt: Ultimate1_1 → 1_2(_Loop) … · 갈래가 여럿이면 하나 · 달려가는 사도는 고리 횟수까지)
            //   때리는 순간도 그 계획(스파인 SFX · Event)으로. 전에는 Ultimate1_1 한 조각만 틀어 몸짓이 중간에 끊겼다
            UltPlan = null;
            if (ult && u.SpineArt && u.SkelData != null)
            {
                try { UltPlan = Bolzena.Fx.SpineMotion.PlanUlt(u.SkelData, Battle.Snapshot.Heroes[act.Actor.Index].Id); }
                catch (System.Exception ex) { Debug.LogWarning("[Ult] 계획 실패 " + key + " — " + ex.Message); }
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
            // 원작 방식 고학년 — 제자리에서 시전 조각만(발동 조각은 cue 때). 타격도 시전 조각 안의 것만
            var split = ult && UltPlan != null && UltPlan.Anim != null && ULT_SPLIT.TryGetValue(Battle.Snapshot.Heroes[act.Actor.Index].Id, out var sp0) ? sp0 : null;
            if (split != null)
            {
                UltPlan.Chain.Clear(); UltPlan.Table = null;
                if (split.CastEnd > 0)
                {
                    marks = marks.FindAll(m => m <= split.CastEnd - 0.1f);
                    if (marks.Count == 0) marks.Add(Mathf.Min(0.5f, split.CastEnd * 0.4f));
                }
            }
            // 긴 고학년 줄이기(2026-10-06 사용자 원칙) — 원작 몸짓이 7초(ULT_MAX, 화면 시계) 안이면 그대로. 넘으면 꼬리 · 앞 준비만 모자란 몫만큼, 그래도 길면 1.2배속까지.
            //   몸짓 · 이펙트 · 소리 · 이동 · 타격이 모두 speed 로 시각을 나누니 같은 시계로 따라온다. 그래도 넘는 사도는 점검 표에 남는다
            UltSpeed = speed; UltNaturalMs = 0; UltShortShown = false;
            cutSkips.Clear(); cutEnd = float.MaxValue; UltCutFrontMs = 0; UltCutBackMs = 0;
            var fit = ult && split == null ? UltFitOf(Battle.Snapshot.Heroes[act.Actor.Index].Id) : null;   // 원작 영상 맞춤(UltVideoFit.cs)
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
                    if (fit.End > 0) cutEnd = fit.End / 1000f;
                    UltCutFrontMs = 0; for (int i = 0; fit.Skip != null && i + 1 < fit.Skip.Length; i += 2) UltCutFrontMs += fit.Skip[i + 1] - fit.Skip[i];
                    UltCutBackMs = fit.End > 0 ? Mathf.Max(0, UltNaturalMs - fit.End) : 0;
                    nat = Rm(nat);
                }
                if (fit != null && fit.Speed > 0) { speed = fit.Speed; keepLong = true; }
                if (split != null && split.CastEnd > 0) { cutEnd = split.CastEnd; UltCutBackMs = Mathf.RoundToInt((nat - cutEnd) * 1000); UltCutFrontMs = 0; nat = Rm(nat); }   // 원작 방식 — 시전 조각에서 끊는다
                else if (UltShortNow(Battle.Snapshot.Heroes[act.Actor.Index].Id) && !keepLong && !fitLen && nat / speed > ULT_SHORT)   // 짧게 보기(설정 · 판에서 두 번째부터) — 앞 준비 · 꼬리를 더 덜고 조금 빠르게
                { PlanCut(u, hidUlt: Battle.Snapshot.Heroes[act.Actor.Index].Id, need: nat - ULT_SHORT * speed); nat = Rm(nat); speed *= 1.15f; UltShortShown = true; }
                else if (!keepLong && !fitLen && nat / speed > ULT_MAX) { PlanCut(u, hidUlt: Battle.Snapshot.Heroes[act.Actor.Index].Id, need: nat - ULT_MAX * speed); nat = Rm(nat); }   // 7초 넘는 몫만 — 꼬리 · 앞 준비부터(가운데 타격은 그대로)
                if (!keepLong && nat / speed > ULT_MAX) speed = Mathf.Clamp(nat / ULT_MAX, speed, Mathf.Max(speed, ULT_SPEED_MAX));   // 그래도 길면 조금 빠르게 — 많아야 1.2배속(사용자 원칙 2026-10-06)
                UltSpeed = speed;
            }
            // 카드 — 애니 끝까지(전엔 마지막 타격 + 0.32초에 끊었다). 상한(CardCap)을 넘으면 꼬리 · 앞 준비 · 빈 구간부터 줄이고, 그래도 0.4초 넘게 남으면 1.5배속까지
            bool cardSelf = cardChain != null && CardSelf(hidC, act.Motion);
            if (cardChain != null)
            {
                CardNaturalMs = Mathf.RoundToInt(cardTotal * 1000);
                float over = CardCut(u, cardChain, marks, cardTotal, CardCap(act.Motion), speed);
                float shown = Rm(cardTotal) / speed;
                if (over > 0.05f && shown > CardCap(act.Motion) + 0.4f) speed = Mathf.Min(CARD_SPEED_MAX, speed * shown / (CardCap(act.Motion) + 0.4f));
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
                var uh = split != null ? null : Bolzena.Fx.UltMotion.Hits(hidFx);   // 원작 방식 — 기준표 타수는 발동까지 센 것이라 시전 조각엔 안 맞는다
                // 무작위 대상 여러 번(셰이디 6타가 적 셋에 흩어짐) — 엔진은 적마다 몇 번째인지로 묶어 2 묶음이 된다. 피해 수가 원작 타수와 같으면 피해 하나를 한 박자로
                int dmgN = 0; foreach (var e in group) if (e.Kind == EventKind.Damage) dmgN++;
                if (uh != null && uh.N == dmgN && dmgN > byHit.Count) byHit = SeqHits(group);
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
            if (cardChain != null && !CardLagKeep(hidC)) lagMs = 0;   // 카드 — 원작은 스파인 타격 이벤트 때 맞는다(투사체는 그만큼 일찍 쏜다 — UltFx.RunPlan shot = 타격 − 비행). 전엔 비행 0.33초를 더해 0.35~0.45초 늦었다
            if (cutSkips.Count > 0 || cutEnd < float.MaxValue) for (int i = 0; i < marks.Count; i++) marks[i] = Rm(marks[i]);   // 줄인 몸짓의 시계로   // 기준표 시각은 이펙트가 닿는 때 그대로
            if (ult && hidFx == "미로" && UltPlan != null) { UltPlan.Chain.Clear(); UltPlan.Table = null; }   // 미로 — 고학년은 「거울에 숨기」(1_1)만, 광선(1_3 · 1_4)은 다음 턴 miro_beam 에
            var times = HitTimes(marks, Mathf.Max(1, fxOn ? byHit.Count : hits), speed);
            if (lagMs > 0) for (int i = 0; i < times.Count; i++) times[i] += lagMs / 1000f;
            Demo.UltLap.Lap("PlanUlt");

            // 다른 유닛은 살짝 물러서게(어둡게)
            foreach (var h in Heroes) if (h != u) h.Tint(new Color(0.62f, 0.62f, 0.68f));
            for (int i = 0; i < Enemies.Count; i++) if (!targets.Contains(i)) Enemies[i].Tint(new Color(0.62f, 0.62f, 0.68f));

            // 내딛기 — 근접은 대상 앞까지 달려가 잔상을 남긴다, 마법은 반 걸음
            Vector3 dest = u.Home;
            // 고학년은 원작대로 — 근접(앞줄 또는 원작 평타가 투사체 없는 사도)이고 몸짓이 제 몸을 멀리 옮기지 않으면 적 앞까지 가서 한다(UltMover)
            bool dash = ult ? split == null && UltMover(u, key, Battle.Snapshot.Heroes[act.Actor.Index].Id, anim, targets)
                : cardChain != null ? CardDashes(u, key, hidC, act.Motion, cardChain, targets.Count) : Look.MeleeArt(key) && targets.Count > 0;
            if (targets.Count > 0)
            {
                var tp = targets.Count == 1 ? Enemies[targets[0]].Feet : Mid(targets);
                float reach = targets.Count == 1 ? Enemies[targets[0]].Width() * 0.35f + 1.25f : 2.4f;
                dest = dash ? new Vector3(tp.x - reach, tp.y - 0.02f, 0) : cardSelf ? u.Home : u.Home + new Vector3(0.5f, 0, 0);   // 자기형은 반 걸음도 안 나간다
            }
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
                }
                UltMoveWhy += " · 영상 맞춤 " + fit.Move;
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
                if (act.Motion == Motion.Skill1) CardOrb(u);   // 저학년 시작 흰 구슬 섬광(원작 공용 연출)
            }
            float tStart = Time.time;                                  // 몸짓 · 이펙트 · 타격이 같이 보는 시계(게임 시간 — 멈칫 · 배속을 같이 탄다)
            fxClock = fxOn ? new Bolzena.Fx.UltClock() : null;
            if (ult) { UltAnimLog = UltPlan != null && UltPlan.Anim != null ? UltPlan.Anim + (UltPlan.Chain.Count > 0 ? " → " + string.Join(" → ", UltPlan.Chain) : "") : anim; UltTargets = targets.Count; }
            Demo.UltLap.Lap("PlayChain");
            if (ult) Bolzena.Fx.BolzenaFx.UltTweak = FitTweak(hidFx, fit, speed);   // 원작 영상 맞춤 — 장 자리 · 시각(이 고학년만)
            bool auth = ult && AUTH_THUNDER.Contains(hidFx) && times.Count > 0;   // 1성 「교주의 천벌」 — 원작은 사도 장 없이 하늘에서 권능 낙뢰만(스킬 발사체 · 총구 장은 안 튼다)
            bool orig = !auth && OrigFx(act, u, anim, marks, speed, ult, targets, dash ? dest : (Vector3?)null, fxOn ? times : null, cardSelf);
            if (auth) { StartCoroutine(AuthThunder(targets, times[0])); orig = true; }
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
                if (dash) StartCoroutine(Ghosts(u, dashTime, new Color(0.75f, 0.9f, 1f, 0.22f)));
            }
            if (ult && !orig) StartCoroutine(UltCharge(u, times[0]));   // 원작 고학년 이펙트가 없을 때만(우이) 자체 모으기

            // 고학년 카메라 — 시전자에게 당겼다가 첫 타격에 적 쪽으로
            if (ult) StartCoroutine(CamTo(u.Center + new Vector3(1.2f, 0, 0), 1.18f, 0.35f));
            if (ult && KEEP_ON_SCREEN.Contains(Battle.Snapshot.Heroes[act.Actor.Index].Id)) StartCoroutine(KeepOnScreen(u, duration + 0.3f));   // 몸짓이 몸을 화면 왼쪽 밖으로 끌고 가면(버터 · 우로스 · 셰이디(역전) 루트 이동) 안으로 당긴다
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
                foreach (var e in kv.Value)
                {
                    if (e.Kind == EventKind.Damage) { Impact(e, u, ult, last, hitSfx, kv.Value.Count > 3, !aoeFxDone); seqTotal += e.Value; if (aoeAt.HasValue) { aoeFxDone = true; aoeHitTotal += e.Value; } }
                    else yield return ApplyConsequence(e, u);
                }
                if (cardChain != null && !kv.Value.Exists(ev0 => ev0.Kind == EventKind.Damage)) CardGhostHit(targets, act.HitKind);   // 나누어 보인 박자에 피해 조각이 없으면(작은 피해) 맞는 반응만
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
            // 남은 몸짓 — 카드는 마지막 타격 뒤 조금 더 보고 돌아온다. 고학년은 몸짓을 끝까지(조각 · 고리 다) 하고 돌아온다
            //   (웹판: 달려간 고학년은 total + back 까지. 전에는 타격 + 0.6초에 Idle 로 끊어 끝 조각이 잘렸다)
            float tail = ult ? Mathf.Max(duration, table != null ? Rm(table.TotalMs / 1000f) / speed : 0) : cardChain != null ? duration : Mathf.Min(duration, elapsed + 0.32f);   // 카드도 애니 끝까지(줄인 길이)
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

        // ── 보스 클론 고학년(엔진 cue foeUlt* · 보스_고학년.md) ── 예고: 의도 칸이 얼굴 · 이름으로 붉게 맥동(EnemyHud) + 머리 위 경고 ·
        //   사용: 붉은 테 컷인(오른쪽에서) → 그 사도의 고학년 몸짓 → 치는 수마다 붉은 번쩍 · 흔들림 → 끝: 테를 걷음 · 끊김: 「끊김!」 · 깨지는 조각
        readonly List<SpriteRenderer> foeFrame = new List<SpriteRenderer>();
        IEnumerator FoeUltFx(BattleEvent e)
        {
            int ei = e.Actor.Index;
            var u = ei >= 0 && ei < Enemies.Count ? Enemies[ei] : null;
            var es = ei >= 0 && ei < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[ei] : null;
            Debug.Log($"[FoeUlt] {e.Text} {(u ? u.name : "-")} 「{e.Say}」 v={e.Value} {e.Anim}");
            Emit("foeult_" + e.Text);
            switch (e.Text)
            {
                case "warn":
                    RefreshHud();
                    if (u)
                    {
                        u.Flash(new Color(1f, 0.3f, 0.25f), 0.4f, 0.8f);
                        Vfx.Glow(u.Center, 3.2f, new Color(1f, 0.2f, 0.15f, 0.8f), 0.5f, 2.5f, "FX_IN_Ring_ShockWave_03", 186);
                        Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 1.0f, 3.3f), 0), "고학년 예고 「" + e.Say + "」", 0.36f, new Color(1f, 0.55f, 0.45f), new Color(0.3f, 0, 0), 1.6f, 1.3f, null, 476, 0.4f);
                        Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 0.55f, 2.85f), 0), $"다음 턴 {e.Value}", 0.3f, new Color(1f, 0.85f, 0.8f), new Color(0.3f, 0, 0), 1.6f, 1.2f, null, 476, 0.4f);
                    }
                    Sfx.Play("turn_start", 0.6f, 0.75f);
                    yield return Clock.Wait(0.6f);
                    break;
                case "start":
                {
                    FoeFrame(true);
                    var look = e.Anim != null ? Look.Hero(e.Anim) : null;
                    if (look != null && !Bolzena.RunUI.Settings.SkipCutin)
                        yield return UltCutin.Play(ScreenRoot, look.Art, es != null ? es.Name : "", e.Say ?? "", new Color(0.85f, 0.12f, 0.12f), true);
                    break;
                }
                case "hit":
                    break;   // 치는 때는 EnemyAct 가 맞는 순간에(FoeUltBeat)
                case "end":
                    FoeFrame(false);
                    break;
                case "cut":
                {
                    FoeFrame(false);
                    RefreshHud();
                    yield return CutFx(u, $"{e.Anim ?? "격파"} — 「{e.Say}」");
                    break;
                }
            }
        }

        // 「끊김!」 — 예고한 큰 수를 격파로 끊었다(보스 고학년 foeUltCut · 엘리트 · 일반 힘 모으기). 깨지는 조각 · 흰 번쩍 · 약 1초
        //   underBreak — 바로 앞 「격파!」 글(머리 위 Top+0.45)과 겹치지 않게 글을 몸 쪽으로 내린다(엘리트 · 일반 — 키가 작아 Center+1.2 가 머리 위와 겹쳤다)
        IEnumerator CutFx(UnitView u, string sub, bool underBreak = false)
        {
            var at = u ? u.Center : new Vector3(4f, 0, 0);
            float wy = 1.2f;
            if (underBreak && u) wy = Mathf.Min(wy, u.Top.y - 0.5f - at.y);
            Clock.HitStop(0.12f);
            FieldRig.Shake(0.5f, 2f);
            Vfx.Flash(new Color(1f, 1f, 1f, 0.35f), 0.2f);
            Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Spark", Count = 26, Speed = new Vector2(4f, 11f), Life = new Vector2(0.35f, 0.7f), Size = new Vector2(0.08f, 0.22f),
                C0 = new Color(1f, 0.6f, 0.5f), C1 = new Color(0.6f, 0.1f, 0.1f), Gravity = 6f, Spin = true, Angle = 90, Spread = 360, Order = 476, Boost = 2.5f,
            });
            Vfx.Glow(at, 3.5f, new Color(1f, 0.85f, 0.7f, 0.9f), 0.35f, 3f, "FX_IN_Ring_ShockWave_03", 475);
            Vfx.Word(at + new Vector3(0, wy, 0), "끊김!", 0.8f, new Color(1f, 0.95f, 0.85f), new Color(0.45f, 0.05f, 0.05f), 1.0f, 1.6f, null, 480, 0.3f);
            Vfx.Word(at + new Vector3(0, wy - 0.65f, 0), sub, 0.3f, new Color(1f, 0.8f, 0.7f), new Color(0.2f, 0, 0), 1.0f, 1.2f, null, 478, 0.35f);
            Sfx.Play("block_hit", 0.8f, 0.7f);
            Sfx.Play("ult_impact", 0.5f, 0.8f);
            yield return Clock.Wait(0.7f);
        }

        // ── 엘리트 · 일반 적 힘 모으기(엔진 cue foeChargeWarn · API v2.8) ── 보스 고학년보다 한 단계 약하게: 붉은 대신 주황, 얼굴 · 컷인 없음.
        //   모으기: 주황 번쩍 · 고리 하나 + 머리 위 짧은 「힘 모으는 중」 띠(의도 칸은 EnemyHud 가 「다음 차례」 · 은은한 맥동) ·
        //   쏟기: EnemyAct(Anim "charge") 가 그 수 이름 띠 · 조금 센 타격 · 끊김: 「끊김!」(CutFx)
        IEnumerator FoeChargeFx(BattleEvent e)
        {
            int ei = e.Actor.Index;
            var u = ei >= 0 && ei < Enemies.Count ? Enemies[ei] : null;
            Debug.Log($"[FoeCharge] {e.Text} {(u ? u.name : "-")} 「{e.Say}」 v={e.Value} {e.Anim}");
            Emit("foecharge_" + e.Text);
            // 그 적의 머리 위만 새로(RefreshHud 는 손까지 맞춰서, 적 차례 중간에 부르면 아직 안 뽑은 손이 먼저 깔린다)
            var snapE = ei >= 0 && ei < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[ei] : null;
            if (snapE != null && ei < EnemyHuds.Count && EnemyHuds[ei] != null && !snapE.Dead) { EnemyHuds[ei].SetIntent(snapE); EnemyHuds[ei].SetChips(snapE.Chips); }
            switch (e.Text)
            {
                case "warn":
                    if (!u) break;
                    u.Flash(new Color(1f, 0.62f, 0.3f), 0.3f, 0.6f);
                    Vfx.Glow(u.Center, 2.4f, new Color(1f, 0.55f, 0.2f, 0.6f), 0.45f, 2.2f, "FX_IN_Ring_ShockWave_03", 186);
                    ChargeBand(u, "힘 모으는 중", 0.28f, 1.3f);
                    Sfx.Play("turn_start", 0.4f, 0.9f);
                    yield return Clock.Wait(0.4f);
                    break;
                case "cut":
                    yield return CutFx(u, $"격파 — 「{e.Say}」", true);
                    break;
            }
        }

        // 머리 위 짧은 띠 — 머리 위 묶음(EnemyHud) 위 끝 바로 위에, 화면 위 HUD 에 닿지 않게
        void ChargeBand(UnitView u, string text, float size, float life)
        {
            float y = u.Top.y + 0.75f;
            int ei = Enemies.IndexOf(u);
            if (ei >= 0 && ei < EnemyHuds.Count && EnemyHuds[ei] != null)
                y = u.transform.localPosition.y + EnemyHuds[ei].LocalBox.yMax + 0.22f;
            Vfx.Word(new Vector3(u.Top.x, Mathf.Min(y, 3.35f), 0), text, size, new Color(1f, 0.78f, 0.45f), new Color(0.32f, 0.12f, 0), life, 1.15f, null, 474, 0.3f);
        }

        // 보스 고학년 한 수 — 붉은 번쩍 · 흔들림 · 붉은 충격 고리(마지막 수는 더 크게)
        void FoeUltBeat(UnitView u, bool last)
        {
            Vfx.Flash(new Color(1f, 0.15f, 0.1f, last ? 0.35f : 0.22f), last ? 0.3f : 0.18f);
            FieldRig.Shake(last ? 1.0f : 0.6f, last ? 3.5f : 2.5f);
            PostFx.Kick(chroma: last ? 1f : 0.6f, lens: -0.15f);
            var mid = Vector3.zero; int n = 0;
            foreach (var h in Heroes) if (h) { mid += h.Center; n++; }
            if (n > 0) Vfx.Glow(mid / n, last ? 6f : 4.5f, new Color(1f, 0.25f, 0.2f, 0.85f), 0.35f, 2.5f, "FX_IN_Ring_ShockWave_03", 188);
        }

        // 붉은 테 — 보스 고학년 동안 화면 가장자리를 붉게(맥동은 ScreenFit 이 따라 늘인다)
        void FoeFrame(bool on)
        {
            foreach (var f in foeFrame) if (f) Destroy(f.gameObject);
            foeFrame.Clear();
            if (!on || ScreenRoot == null) return;
            float hw = Tone.HalfW, hh = Tone.HalfH, t = 0.22f;
            var c = new Color(0.9f, 0.08f, 0.06f, 0.75f);
            foeFrame.Add(Make.Box("foeTop", ScreenRoot, Res.UI("white"), new Vector3(0, hh - t / 2, 0), new Vector2(hw * 2 + 1, t), 690, c));
            foeFrame.Add(Make.Box("foeBot", ScreenRoot, Res.UI("white"), new Vector3(0, -hh + t / 2, 0), new Vector2(hw * 2 + 1, t), 690, c));
            foeFrame.Add(Make.Box("foeL", ScreenRoot, Res.UI("white"), new Vector3(-hw + t / 2, 0, 0), new Vector2(t, hh * 2), 690, c));
            foeFrame.Add(Make.Box("foeR", ScreenRoot, Res.UI("white"), new Vector3(hw - t / 2, 0, 0), new Vector2(t, hh * 2), 690, c));
            Clock.Run(FoeFramePulse());
        }

        IEnumerator FoeFramePulse()
        {
            float t = 0;
            while (foeFrame.Count > 0)
            {
                t += Time.unscaledDeltaTime;
                float a = 0.45f + 0.35f * Mathf.Sin(t * 7f);
                foreach (var f in foeFrame) if (f) Make.Alpha(f, a);
                yield return null;
            }
        }

        // 화면 밖 막기 — 고학년 몸짓 동안 몸(메시 경계)의 왼끝이 화면 왼끝 + 여백보다 밖이면 그만큼 몸(Art)을 오른쪽으로 당긴다(부드럽게).
        //   끝나면 0.25초에 제자리로. 몸짓 자체 · 이동(UltTravelCo — 몸 전체 transform)은 그대로 두고 Art 만 민다
        static readonly HashSet<string> KEEP_ON_SCREEN = new HashSet<string> { "버터", "우로스", "셰이디_역전" };   // 원작 영상 비교: 몸짓 루트 이동이 화면 왼쪽 밖으로
        IEnumerator KeepOnScreen(UnitView u, float dur)
        {
            var cam = Camera.main;
            if (u == null || u.Art == null || cam == null) yield break;
            float push = 0, t = 0;
            while (t < dur && u)
            {
                float sx = Mathf.Max(0.001f, u.Art.parent.lossyScale.x);
                float left = cam.transform.position.x - cam.orthographicSize * cam.aspect + 0.25f;
                float out0 = left - (u.MeshBounds.min.x - push * sx);   // 밀지 않았다면 왼끝이 화면 밖으로 나간 몫(월드)
                push = Mathf.MoveTowards(push, Mathf.Max(0, out0) / sx, Time.deltaTime * 12f);
                u.Nudge = new Vector3(push, 0, 0);
                yield return null; t += Time.deltaTime;
            }
            if (!u) yield break;
            float from = push;
            yield return Clock.Tween(0.25f, k => { if (u) u.Nudge = new Vector3(from * (1 - k), 0, 0); });
            if (u) u.Nudge = Vector3.zero;
        }

        // ── 1성 고학년 「교주의 천벌」 ── 원작은 사도 몸짓(Skill1_1) 위에 교주 권능 낙뢰(fx_authority_thunder)가 대상에게 떨어진다(공용 장 — 사도 고학년 장이 따로 없다)
        static readonly HashSet<string> AUTH_THUNDER = new HashSet<string> { "레이지", "파트라", "메죵", "베루", "쵸피", "타이다", "유미미", "사리", "밍스" };
        IEnumerator AuthThunder(List<int> targets, float hitAt)
        {
            const string FX = "fx_authority_thunder";
            float imp = Bolzena.Fx.FxLibrary.ImpactSec(FX);
            float t = 0, fireAt = Mathf.Max(0, hitAt - imp);
            while (t < fireAt) { yield return null; t += Time.deltaTime; }
            var list = targets.Count > 0 ? targets : new List<int> { FirstAliveEnemy() };
            foreach (var i in list)
                if (i >= 0 && i < Enemies.Count && Enemies[i] != null)
                    Bolzena.Fx.BolzenaFx.Play(FX, new Bolzena.Fx.FxPlayOptions { At = Enemies[i].Feet, Until = 1.4f, Order = 325, Parent = FieldRoot });
            Sfx.Play("ult_impact", 0.5f, 1.25f);
        }

        // ── 원작 방식 고학년 둘째 묶음(15명) cue ── 한 줄 = 원작 장 · 자리 · 띠 글 · 빛깔.
        //   At: caster(시전자) · aim(지금 몸짓의 대상 — 마무리 · 기절 직전) · mid(적 무리 가운데) · each(맞는 적마다 — 턴 발동의 피해 박자)
        //   Strike: 턴 시작 · 끝처럼 몸짓 밖에서 오면 뒤따르는 피해를 묶어 이 장과 함께 친다(Cap: V 대 · 1 · 살아 있는 적 전부)
        //   Finish: 마무리 일격(즉사 대신 「HP 낮은 적에게 큰 덤」 — 티그(영웅) · 로니 · 아르코 · 기절 직전) — 멈칫 · 확대 · 큰 띠
        class CueLook { public string Fx; public string At = "caster"; public string Word; public Color Tint = Color.white; public bool Strike, Finish; public int Cap; public float Until = 1.4f; }
        static readonly Dictionary<string, CueLook> CUE2 = new Dictionary<string, CueLook>
        {
            ["tig_finish"] = new CueLook { Fx = "fx_tighero_ultimate_cros_1", At = "aim", Word = "결착!", Tint = new Color(1f, 0.75f, 0.35f), Finish = true },
            ["roni_finish"] = new CueLook { Fx = "fx_ronnie_outlaw_2", At = "aim", Word = "수사 종결!", Tint = new Color(1f, 0.55f, 0.45f), Finish = true },
            ["arco_crit"] = new CueLook { Fx = "fx_arco_ultimate_ground_4", At = "aim", Word = "확정 치명!", Tint = new Color(1f, 0.85f, 0.4f), Finish = true },
            ["benny_honey_stun"] = new CueLook { Fx = "fx_benibeni_skill_explosion_2", At = "aim", Word = "꿀단지 강타!", Tint = new Color(1f, 0.82f, 0.3f), Finish = true },
            ["amelia_shock_stun"] = new CueLook { Fx = "fx_amelia_ultimate_laser_2", At = "aim", Word = "감전 기절!", Tint = new Color(0.6f, 0.85f, 1f), Finish = true },
            ["r41_shock_stun"] = new CueLook { Fx = "fx_ameliar41_ultimate_explosion_1", At = "aim", Word = "감전 기절!", Tint = new Color(0.6f, 0.85f, 1f), Finish = true },
            ["yomi_moon_summon"] = new CueLook { Fx = "fx_yomi_ultimate_1", At = "mid", Word = "구름 걷는 달빛", Tint = new Color(0.8f, 0.85f, 1f) },
            ["yomi_moonlight"] = new CueLook { Fx = "fx_yomi_ultimate_3", At = "each", Word = "달빛", Tint = new Color(0.8f, 0.85f, 1f), Strike = true },
            ["sherum_zone"] = new CueLook { Fx = "fx_sherum_ultimate_area_1", At = "mid", Word = "회한의 영역", Tint = new Color(0.75f, 0.6f, 1f), Until = 1.8f },
            ["sherum_regret"] = new CueLook { Fx = "fx_sherum_ultimate_ink_1", At = "each", Word = "흑역사!", Tint = new Color(0.75f, 0.6f, 1f), Strike = true, Cap = 1 },
            ["levi_papers"] = new CueLook { Fx = "fx_levigraduate_ultimate_1", At = "mid", Word = "서류 폭풍", Tint = new Color(0.95f, 0.95f, 0.85f), Strike = true, Until = 1.8f },
            ["ui_pond"] = new CueLook { Fx = "fx_uimemory_attack2_pond_1_default", At = "mid", Word = "기억의 연못", Tint = new Color(0.6f, 0.9f, 1f), Until = 1.8f },
            ["ui_pond_wave"] = new CueLook { Fx = "fx_uimemory_attack2_pond_1_default", At = "mid", Word = "일렁임", Tint = new Color(0.6f, 0.9f, 1f), Strike = true },
            ["epica_concert"] = new CueLook { Fx = "fx_epica_ultimate_buff_start", At = "caster", Word = "연주 시작!", Tint = new Color(1f, 0.8f, 0.5f) },
            ["epica_play"] = new CueLook { Fx = "fx_epica_ultimate_battlefield_epicon", At = "each", Word = "에피콘 합주", Tint = new Color(1f, 0.8f, 0.5f), Strike = true, Cap = -1 },
            ["epica_finale"] = new CueLook { Fx = "fx_epica_ultimate_buff_top", At = "caster", Word = "만족스러운 연주", Tint = new Color(1f, 0.85f, 0.55f) },
            ["prickle_vine"] = new CueLook { Fx = "fx_fricle_ultimate_vine_1", At = "mid", Word = "가시 덩굴", Tint = new Color(0.6f, 1f, 0.6f) },
            ["prickle_tentacle"] = new CueLook { Fx = "fx_fricle_ultimate_ground_01_start", At = "mid", Word = "가시 촉수!", Tint = new Color(0.6f, 1f, 0.6f) },
            ["heidi_next_scoop"] = new CueLook { Fx = "fx_heidi_ultimate_news_1", At = "mid", Word = "다음 특종!", Tint = new Color(1f, 0.9f, 0.6f) },
            ["skia_knot"] = new CueLook { Fx = "fx_skea_ultimate_orb_1", At = "aim", Word = "언약의 매듭", Tint = new Color(0.85f, 0.7f, 1f) },
            ["skia_knot_burst"] = new CueLook { Fx = "fx_skea_ultimate_explosion_bottom_1", At = "mid", Word = "매듭 폭발!", Tint = new Color(0.85f, 0.7f, 1f), Finish = true },
            ["alice_sun"] = new CueLook { Fx = "fx_alice_cardlight_1", At = "caster", Word = "태양", Tint = new Color(1f, 0.75f, 0.4f) },
            ["alice_tower"] = new CueLook { Fx = "fx_alice_cardlight_1", At = "caster", Word = "탑", Tint = new Color(0.75f, 0.8f, 1f) },
            ["alice_star"] = new CueLook { Fx = "fx_alice_cardlight_2", At = "caster", Word = "별", Tint = new Color(1f, 0.95f, 0.6f) },
        };

        Vector3 EnemyMid()
        {
            var all = new List<int>(); for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) all.Add(i);
            return all.Count > 0 ? MidBody(all) : new Vector3(4f, -0.6f, 0);
        }

        // 몸짓 안에서 온 cue(띠 · 장만) — 마무리면 멈칫 · 번쩍 · 흔들림
        IEnumerator Cue2Fx(BattleEvent e, UnitView h, CueLook c)
        {
            Vector3 at = c.At == "aim" ? actAim ?? EnemyMid() : c.At == "caster" ? h.Center : EnemyMid();
            if (c.Fx != null) Bolzena.Fx.BolzenaFx.Play(c.Fx, new Bolzena.Fx.FxPlayOptions { At = c.At == "mid" ? new Vector3(at.x, at.y - 0.6f, 0) : at, Until = c.Until, Order = 195, Parent = FieldRoot });
            if (c.Word != null) Vfx.Word((c.At == "caster" ? h.Top : at + new Vector3(0, 1.0f, 0)) + new Vector3(0, 0.4f, 0), c.Word, c.Finish ? 0.62f : 0.42f, c.Tint, new Color(0.25f, 0.08f, 0.02f), 1.2f, c.Finish ? 1.5f : 1.2f, null, 478, 0.35f);
            if (c.Finish)
            {
                Clock.HitStop(0.14f);
                Vfx.Flash(new Color(c.Tint.r, c.Tint.g, c.Tint.b, 0.3f), 0.2f);
                FieldRig.Shake(0.7f, 3f);
                FieldRig.Punch(at, 0.05f);
                Sfx.Play("ult_impact", 0.6f, 1.15f);
                yield return Clock.Wait(0.25f);
            }
            else Sfx.Play("buff", 0.45f, 1.1f);
        }

        // 몸짓 밖(턴 시작 · 끝)에서 온 발동 — 그 장과 함께 뒤따르는 피해를 친다
        IEnumerator Cue2Strike(BattleEvent cue, List<BattleEvent> group, CueLook c)
        {
            int i = Mathf.Clamp(cue.Actor.Index, 0, Heroes.Count - 1);
            var u = Heroes[i];
            var dmg = group.FindAll(x => x.Kind == EventKind.Damage);
            Debug.Log($"[FxCue] {cue.Text} {(u ? u.name : "-")} v={cue.Value} 피해 {dmg.Count}");
            Emit(cue.Text);
            if (u) u.Flash(c.Tint, 0.3f, 0.6f);
            var tgts = new List<int>(); foreach (var d in dmg) if (!tgts.Contains(d.Target.Index) && d.Target.Index < Enemies.Count) tgts.Add(d.Target.Index);
            Vector3 mid = tgts.Count > 0 ? MidBody(tgts) : EnemyMid();
            if (c.Word != null) Vfx.Word(mid + new Vector3(0, 1.5f, 0), c.Word + (cue.Value > 1 && c.Cap < 0 ? $" ×{cue.Value}" : ""), 0.42f, c.Tint, new Color(0.2f, 0.06f, 0.02f), 1.2f, 1.2f, null, 478, 0.35f);
            float t = 0, imp = c.Fx != null ? Bolzena.Fx.FxLibrary.ImpactSec(c.Fx) : 0;
            if (c.At != "each" && c.Fx != null) Bolzena.Fx.BolzenaFx.Play(c.Fx, new Bolzena.Fx.FxPlayOptions { At = new Vector3(mid.x, mid.y - 0.6f, 0), Until = c.Until, Order = 190, Parent = FieldRoot });
            float first = c.At == "each" ? Mathf.Max(0.3f, imp) : Mathf.Max(0.35f, imp);
            float gap = c.At == "each" ? 0.22f : 0.06f;
            int n = 0;
            foreach (var e in group)
            {
                if (e.Kind != EventKind.Damage) { yield return ApplyConsequence(e, u); continue; }
                float at = first + gap * n;
                var tgt = e.Target.Index < Enemies.Count ? Enemies[e.Target.Index] : null;
                if (c.At == "each" && c.Fx != null && tgt != null)
                {
                    while (t < at - imp) { yield return null; t += Time.deltaTime; }
                    Bolzena.Fx.BolzenaFx.Play(c.Fx, new Bolzena.Fx.FxPlayOptions { At = tgt.Feet, Until = 0.9f, Order = 190, Parent = FieldRoot });
                }
                while (t < at) { yield return null; t += Time.deltaTime; }
                Impact(e, u, true, n == dmg.Count - 1, null, false);
                n++;
            }
            while (t < first + gap * n + 0.3f) { yield return null; t += Time.deltaTime; }
        }

        // ── 원작 방식 고학년(엔진 cue · 2026-10-06 고학년_원작방식.md) ── 시전 땐 시전 조각만, 발동 조각은 cue 때.
        //   CastEnd: 시전 조각 끝(초, 1배속 · 0 = 몸짓 전부) · Drop: 시전 때 안 트는 원작 장(발동 때 따로) · Trig*: 발동 조각(어디서부터 · 터지는 때 · 이펙트)
        //   Sleep*: 시전 뒤 다음 차례까지 되풀이하는 구간(이드(재활) 꿈속). 시각은 원작 영상 시트(ult_videos/<사도>/고학년_sheet.png)와 스파인 이벤트로 잡았다
        class UltSplit { public float CastEnd; public string Drop; public float TrigFrom = -1, TrigHit; public string TrigFx; public float SleepFrom, SleepTo; }
        static readonly Dictionary<string, UltSplit> ULT_SPLIT = new Dictionary<string, UltSplit>
        {
            // 키샤 — 무대 · 공연(0~5초)만 시전 때, 무대로 뛰어올라 하트 폭발(7.9초~ · 터짐 8.7초)은 다음 턴 kisha_finale
            ["키샤"] = new UltSplit { CastEnd = 5.0f, Drop = "ultimate_end", TrigFrom = 7.9f, TrigHit = 8.7f, TrigFx = "fx_kishya_ultimate_end_1" },
            // 이드(재활) — 꿈 구슬로 들어가 잠들기(0~2초) → 잠(2~5.2초 되풀이) → 다음 턴 ide_wake 에 구슬이 깨짐(5.3초~ · 터짐 5.8초)
            ["이드_재활"] = new UltSplit { CastEnd = 2.0f, Drop = "ultimate_explosion", TrigFrom = 5.3f, TrigHit = 5.8f, TrigFx = "fx_edrehab_ultimate_explosion_1", SleepFrom = 2.0f, SleepTo = 5.2f },
            // 오로라 — 시전은 오로라를 부르는 몸짓 전부, 플라즈마 기둥(thunder)은 턴 시작마다 aurora_rain
            ["오로라"] = new UltSplit { CastEnd = 0, Drop = "ultimate_thunder" },
        };
        public static bool UltDropPart(string hero, string part) => hero != null && part != null && ULT_SPLIT.TryGetValue(hero, out var s) && s.Drop != null && part.Contains(s.Drop);

        // 뒤따르는 피해를 묶는 cue(턴 시작 발동) — miro_beam · aurora_rain 은 V 대, kisha_finale · ide_wake 는 살아 있는 적 수만큼
        static readonly HashSet<string> CUE_STRIKE = new HashSet<string> { "miro_beam", "aurora_rain", "kisha_finale", "ide_wake" };
        static bool IsCueStrike(string id) => id != null && (CUE_STRIKE.Contains(id) || (CUE2.TryGetValue(id, out var c) && c.Strike));
        int CueCap(BattleEvent e)
        {
            if (e.Text == "miro_beam" || e.Text == "aurora_rain") return Mathf.Max(1, e.Value);
            if (CUE2.TryGetValue(e.Text, out var c2) && c2.Cap != 0) return c2.Cap < 0 ? Mathf.Max(1, e.Value) : c2.Cap;
            int n = 0; for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) n++;
            return Mathf.Max(1, n);
        }

        // ── 미로 고학년(엔진 cue) ── miro_hide: 숨기 조각(1_1)이 끝나면 거울 속 고리(1_2)로 · miro_flash: 거울 속에서 맞을 때 반짝 ·
        // miro_beam(V): 광선 조각(1_3 → 1_4)을 틀며 V 발을 하나씩 — 시전 자리에서 쏘고 0.33초 뒤 맞는다
        IEnumerator FxCueFx(BattleEvent e)
        {
            int i = Mathf.Clamp(e.Actor.Index, 0, Heroes.Count - 1);
            var h = Heroes[i];
            if (h == null) yield break;
            Debug.Log($"[FxCue] {e.Text} {h.name} v={e.Value}");
            Emit("cue_" + e.Text);
            switch (e.Text)
            {
                case "miro_hide":
                    Clock.Run(MiroHide(h));
                    break;
                case "miro_flash":
                    h.Flash(new Color(0.8f, 0.95f, 1f), 0.25f, 0.7f);
                    Bolzena.Fx.BolzenaFx.Play("fx_miro_ultimate_aura_1", new Bolzena.Fx.FxPlayOptions { At = h.Center, Until = 0.6f, Order = 200, Parent = FieldRoot });
                    Sfx.Play("buff", 0.4f, 1.3f);
                    break;
                case "ide_sleep":
                    Clock.Run(IdeSleep(h));
                    break;
                case "ide_store":   // 잠든 동안 맞음 — 아픔이 꿈으로 스민다(보라 빛이 몸으로 빨려 든다)
                    h.Flash(new Color(0.7f, 0.6f, 1f), 0.3f, 0.6f);
                    Vfx.Glow(h.Center, 1.6f, new Color(0.6f, 0.5f, 1f, 0.7f), 0.35f, 0.6f, "FX_IN_Ring_ShockWave_03", 186);
                    break;
                case "eshur_mirror_summon":
                    Clock.Run(MirrorSummon(h));
                    break;
                case "eshur_mirror_shot":
                    yield return MirrorShot(h);
                    break;
                case "ran_again":
                case "kidian_again":   // 처치해서 한 번 더 — 「한 번 더!」 띠 · 번쩍, 이어 두 번째 고학년(Present 가 나눠 튼다)
                    h.Flash(new Color(1f, 0.85f, 0.6f), 0.35f, 0.8f);
                    Vfx.Word(h.Top + new Vector3(0, 0.55f, 0), "한 번 더!", 0.5f, new Color(1f, 0.85f, 0.5f), new Color(0.35f, 0.1f, 0), 1.2f, 1.2f, null, 476, 0.35f);
                    Sfx.Play("buff", 0.6f, 1.2f);
                    yield return Clock.Wait(0.45f);
                    break;
                case "aurora_summon":
                case "kisha_stage":
                    break;   // 시전 몸짓 · 원작 시전 장이 그대로 보여 준다
                default:
                    if (CUE2.TryGetValue(e.Text, out var c2)) yield return Cue2Fx(e, h, c2);
                    break;
            }
        }

        // 이드(재활) — 시전 조각(꿈 구슬로 들어감) 끝에서 잠 구간을 되풀이, ide_wake 까지
        readonly HashSet<UnitView> sleeping = new HashSet<UnitView>();
        IEnumerator IdeSleep(UnitView h)
        {
            var sp = ULT_SPLIT["이드_재활"];
            sleeping.Add(h);
            yield return Clock.Wait(Mathf.Max(0.05f, (sp.CastEnd - 0.06f) / Mathf.Max(0.5f, UltSpeed)));
            if (h && sleeping.Contains(h)) h.LoopPart(h.AnimFor(Motion.Ultimate), sp.SleepFrom, sp.SleepTo);
        }

        // 에슈르(마도) 거울 형상 — 같은 스파인을 보랏빛 반투명으로 뒤에 세워 원작 MirrorImage* 동작을 튼다(3번 쏘면 사라짐)
        UnitView mirror; int mirrorLeft;
        IEnumerator MirrorSummon(UnitView h)
        {
            mirrorLeft += 3;
            if (mirror == null)
            {
                var hk = h.name.Replace("hero_", "");
                mirror = UnitView.Create(FieldRoot, "mirror_" + hk, hk, "Normal", true, UnitScale, h.Home + new Vector3(-0.85f, 0.45f, 0), h.BaseOrder - 1);
                mirror.Tint(new Color(0.62f, 0.45f, 1f), 100f);
                mirror.SetAlpha(0.7f);
            }
            Bolzena.Fx.BolzenaFx.Play("fx_ashurmagi_shadow_spawn", new Bolzena.Fx.FxPlayOptions { At = mirror.Feet, Until = 1.2f, Order = 120, Parent = FieldRoot });
            if (mirror.Has("MirrorImageSpawn")) mirror.Play("MirrorImageSpawn", 1.1f);
            yield break;
        }

        Vector3? actAim;   // 지금 몸짓의 첫 대상 몸 가운데(거울 형상이 함께 쏠 곳)
        IEnumerator MirrorShot(UnitView h)
        {
            if (mirror == null) yield return MirrorSummon(h);
            var m = mirror;
            if (m == null) yield break;
            if (m.Has("MirrorImageAttack1_1")) m.Play("MirrorImageAttack1_1", 1.6f);
            var from = m.Center + new Vector3(0.3f, 0.1f, 0);
            var to = actAim ?? (from + new Vector3(4f, 0, 0));
            Bolzena.Fx.BolzenaFx.Play("fx_ashurmagi_shadow_attack2_start", new Bolzena.Fx.FxPlayOptions { At = from, Until = 0.6f, Order = 190, Parent = FieldRoot });
            Bolzena.Fx.BolzenaFx.Play("fx_ashurmagi_attack_2_proj", new Bolzena.Fx.FxPlayOptions { At = from, MoveTo = to, MoveDur = 0.25f, Until = 0.35f, Order = 191, Parent = FieldRoot });
            yield return Clock.Wait(0.25f);
            Bolzena.Fx.BolzenaFx.Play("fx_ashurmagi_attack_2_hit", new Bolzena.Fx.FxPlayOptions { At = to, Until = 0.7f, Order = 192, Parent = FieldRoot });
            if (--mirrorLeft <= 0) { mirrorLeft = 0; Clock.Run(MirrorGone(m)); mirror = null; }
        }

        IEnumerator MirrorGone(UnitView m)
        {
            yield return Clock.Wait(0.5f);
            if (m == null) yield break;
            Bolzena.Fx.BolzenaFx.Play("fx_ashurmagi_shadow_spawn", new Bolzena.Fx.FxPlayOptions { At = m.Feet, Until = 0.8f, Order = 120, Parent = FieldRoot });
            yield return Clock.Tween(0.4f, t => { if (m) m.SetAlpha(0.7f * (1 - t)); });
            if (m) Destroy(m.gameObject);
        }

        // 턴 시작 발동 — 오로라 플라즈마 기둥 · 키샤 열기 폭발 · 이드(재활) 꿈의 붕괴. group: 그 cue 뒤의 피해(와 이드 실드)
        IEnumerator CueStrike(BattleEvent cue, List<BattleEvent> group)
        {
            int i = Mathf.Clamp(cue.Actor.Index, 0, Heroes.Count - 1);
            var u = Heroes[i];
            var dmg = group.FindAll(x => x.Kind == EventKind.Damage);
            Debug.Log($"[FxCue] {cue.Text} {(u ? u.name : "-")} v={cue.Value} 피해 {dmg.Count}");
            Emit(cue.Text);
            string hid = Battle.Snapshot.Heroes[i].Id;
            ULT_SPLIT.TryGetValue(hid, out var sp);
            var tgts = new List<int>(); foreach (var d in dmg) if (!tgts.Contains(d.Target.Index) && d.Target.Index < Enemies.Count) tgts.Add(d.Target.Index);
            Vector3 mid = tgts.Count > 0 ? MidBody(tgts) : new Vector3(4f, -0.6f, 0);
            float t = 0;
            if (cue.Text == "aurora_rain")
            {
                // 하늘의 오로라가 번쩍이고 플라즈마 기둥이 한 대씩(원작 20타 → 4타씩 3번)
                if (u) u.Flash(new Color(0.75f, 0.9f, 1f), 0.3f, 0.6f);
                Bolzena.Fx.BolzenaFx.Play("fx_aurora_ultimate_aurora_1", new Bolzena.Fx.FxPlayOptions { At = new Vector3(mid.x, -1.2f, 0), Until = 1.6f, Order = 180, Parent = FieldRoot });
                float imp = Bolzena.Fx.FxLibrary.ImpactSec("fx_aurora_ultimate_thunder_1");
                int n = 0;
                foreach (var e in group)
                {
                    if (e.Kind != EventKind.Damage) { yield return ApplyConsequence(e, u); continue; }
                    float at = 0.35f + 0.28f * n;
                    var tgt = e.Target.Index < Enemies.Count ? Enemies[e.Target.Index] : null;
                    float fireAt = Mathf.Max(0, at - imp);
                    while (t < fireAt) { yield return null; t += Time.deltaTime; }
                    if (tgt != null) Bolzena.Fx.BolzenaFx.Play("fx_aurora_ultimate_thunder_1", new Bolzena.Fx.FxPlayOptions { At = tgt.Feet, Until = 0.9f, Order = 190, Parent = FieldRoot });
                    while (t < at) { yield return null; t += Time.deltaTime; }
                    Impact(e, u, true, n == dmg.Count - 1, null, false);
                    n++;
                }
                while (t < 0.35f + 0.28f * n + 0.3f) { yield return null; t += Time.deltaTime; }
                yield break;
            }
            // 키샤 · 이드(재활) — 발동 조각을 그 자리부터 틀고, 터지는 때 전체를 한꺼번에
            float speed = 1.05f;
            float dur = 0, hitAt = 0.4f;
            if (u != null && sp != null && sp.TrigFrom >= 0)
            {
                sleeping.Remove(u);
                dur = u.PlayPart(u.AnimFor(Motion.Ultimate), sp.TrigFrom, speed);
                hitAt = (sp.TrigHit - sp.TrigFrom) / speed;
            }
            if (u) Sfx.Voice(u.name.Replace("hero_", ""), "ultimate", "shout");
            if (cue.Text == "kisha_finale" && u) Vfx.Word(u.Top + new Vector3(0, 0.55f, 0), $"환호 ×{cue.Value}", 0.42f, new Color(1f, 0.75f, 0.9f), new Color(0.35f, 0.05f, 0.2f), 1.1f, 1.2f, null, 476, 0.35f);
            string fx = sp?.TrigFx;
            Vector3 fxAt = cue.Text == "ide_wake" && u ? u.Center : mid;
            float fimp = fx != null ? Bolzena.Fx.FxLibrary.ImpactSec(fx) : 0;
            while (t < hitAt - fimp) { yield return null; t += Time.deltaTime; }
            if (fx != null) Bolzena.Fx.BolzenaFx.Play(fx, new Bolzena.Fx.FxPlayOptions { At = fxAt, Until = 1.6f, Order = 190, Parent = FieldRoot });
            while (t < hitAt) { yield return null; t += Time.deltaTime; }
            var hitSfx = u ? HeroSfx(u.name.Replace("hero_", ""), Motion.Ultimate, true) : null;
            aoeAt = tgts.Count > 1 ? mid : (Vector3?)null;
            int total = 0, k = 0;
            bool fxDone = false;
            foreach (var e in group)
            {
                if (e.Kind == EventKind.Damage) { Impact(e, u, true, k == dmg.Count - 1, hitSfx, dmg.Count > 3, !fxDone); fxDone = aoeAt.HasValue; total += e.Value; k++; }
                else yield return ApplyConsequence(e, u);
            }
            if (aoeAt.HasValue && total > 0) Vfx.Word(aoeAt.Value + new Vector3(0, 1.3f, 0), "합 " + total.ToString("N0"), 0.5f, new Color(1f, 0.92f, 0.7f), new Color(0.3f, 0.1f, 0), 1.0f, 1f, null, 480, 0.5f);
            aoeAt = null;
            if (dmg.Count > 0 && u) UltFinish(mid, u, true);
            while (t < Mathf.Max(hitAt + 0.5f, dur)) { yield return null; t += Time.deltaTime; }
        }

        IEnumerator MiroHide(Bolzena.View.UnitView h)
        {
            float d = h.Duration("Ultimate1_1");
            yield return Clock.Wait(Mathf.Max(0.1f, d / 1.05f - 0.05f));
            if (h && h.Has("Ultimate1_2")) h.Loop("Ultimate1_2");      // 거울 속 — 광선 차례까지 이 자세로
        }

        IEnumerator MiroBeam(BattleEvent cue, List<BattleEvent> group)
        {
            int i = Mathf.Clamp(cue.Actor.Index, 0, Heroes.Count - 1);
            var u = Heroes[i];
            var dmg = group.FindAll(x => x.Kind == EventKind.Damage);
            Debug.Log($"[FxCue] miro_beam {(u ? u.name : "-")} v={cue.Value} 피해 {dmg.Count}");
            Emit("miro_beam");
            int n = Mathf.Max(1, dmg.Count);
            float dur = u != null && u.Has("Ultimate1_3") ? u.PlayChain("Ultimate1_3", new List<string> { "Ultimate1_4" }, 1.05f) : 0;
            Sfx.Voice(u ? u.name.Replace("hero_", "") : "", "ultimate", "shout");
            float start = 0.6f, span = Mathf.Max(0.8f, Mathf.Min(3.2f, (u != null ? u.Duration("Ultimate1_3") / 1.05f : 3f) - 0.6f));
            float t = 0; int k = 0, di = 0;
            foreach (var e in group)
            {
                if (e.Kind != EventKind.Damage) { yield return ApplyConsequence(e, u); continue; }
                float at = start + (n > 1 ? span * di / (n - 1) : 0);
                while (t < at - 0.33f) { yield return null; t += Time.deltaTime; }
                var tgt = e.Target.Index < Enemies.Count ? Enemies[e.Target.Index] : null;
                if (u != null && tgt != null)
                {
                    var from = u.Fx.At(Bolzena.Fx.FxAnchor.Cast, "Ult1");
                    Bolzena.Fx.BolzenaFx.Play("fx_miro_ultimate_shoot_start_1", new Bolzena.Fx.FxPlayOptions { At = from, Until = 0.5f, Order = 190, Parent = FieldRoot });
                    Bolzena.Fx.BolzenaFx.Play("fx_miro_ultimate_proj_1", new Bolzena.Fx.FxPlayOptions { At = from, MoveTo = tgt.Center, MoveDur = 0.33f, Until = 0.45f, Order = 191, Parent = FieldRoot });
                }
                while (t < at) { yield return null; t += Time.deltaTime; }
                if (tgt != null) Bolzena.Fx.BolzenaFx.Play("fx_miro_ultimate_hit_1", new Bolzena.Fx.FxPlayOptions { At = tgt.Center, Until = 0.8f, Order = 192, Parent = FieldRoot });
                Impact(e, u, true, di == n - 1, null, false);
                di++; k++;
            }
            while (t < dur) { yield return null; t += Time.deltaTime; }
            if (u) u.Idle();
        }

        // ── 변신 ── 들 때: 빛(원작 신탁 빛기둥) · 「꿈결 형상!」 띠 · 그 모습의 쉬는 동작(고학년 끝 자세에서 이어서), 풀릴 때: 본 Idle 로
        IEnumerator FormFx(BattleEvent e)
        {
            int i = Mathf.Clamp(e.Actor.Index, 0, Heroes.Count - 1);
            var h = Heroes[i];
            if (h == null) yield break;
            if (e.Text == "on")
            {
                string tail = e.Anim != null && e.Anim.StartsWith("Idle_") ? e.Anim.Substring(5) : e.Anim;
                h.SetForm(tail);
                if (h.Has("AS1_Ultimate1_2")) h.FormAttack = "AS1_Ultimate1_2";   // 림(혼돈) — 변신 그림은 없고 평타가 원작 강화 공격(고학년 뒤 「차원 너머의 낫」)
                h.Flash(new Color(1f, 0.92f, 0.7f), 0.4f, 0.6f);
                Bolzena.Fx.BolzenaFx.Common("oracle", h.Fx);
                Sfx.Play("buff", 0.6f, 0.9f);
                Vfx.Word(h.Top + new Vector3(0, 0.55f, 0), (e.Say ?? "변신") + "!", 0.5f, new Color(1f, 0.9f, 0.55f), new Color(0.3f, 0.12f, 0), 1.3f, 1.4f, null, 476, 0.35f);
                if (e.Value > 0) Vfx.Word(h.Top + new Vector3(0, 0.15f, 0), $"{e.Value}턴", 0.28f, new Color(1f, 0.95f, 0.85f), new Color(0.2f, 0.1f, 0), 1.0f, 1.2f);
                Emit("form_on");
            }
            else
            {
                h.SetForm(null);
                h.FormAttack = null;
                h.Flash(Color.white, 0.25f, 0.4f);
                Vfx.Word(h.Top + new Vector3(0, 0.4f, 0), (e.Say ?? "변신") + " 풀림", 0.32f, new Color(0.85f, 0.9f, 1f), new Color(0.05f, 0.08f, 0.2f), 1.0f, 1.2f);
                Emit("form_off");
            }
            Hud.SetSnapshot(Battle.Snapshot);
            yield return Clock.Wait(0.2f);
        }

        // ── 긴 고학년 줄이기 ── 이어 튼 조각에서 아무 일(스파인 이벤트 · 원작 타격)도 없는 2.5초 넘는 구간은 가운데를 건너뛰고(앞뒤 0.5초 남김),
        // 마지막 일 뒤로 1.5초 넘게 남으면 1초 뒤에 쉬는 동작으로. 시각은 모두 Rm 으로 옮긴다(타격 · 이펙트 · 소리 · 이동)
        readonly List<(float from, float to)> cutSkips = new List<(float, float)>();
        // 손으로 정한 건너뛰기(초, 1배속) — 시전 되풀이가 이벤트로 이어져 빈 구간으로 안 잡히는 사도. 리뉴아: 1.2~11.2초 시전 고리(첫 타격 11.6초 → 1.6초)
        static readonly Dictionary<string, List<(float from, float to)>> CUT_FIX = new Dictionary<string, List<(float, float)>>
        {
            // (리뉴아 1.2~11.2초 건너뛰기는 원작 가운데 타격까지 지워서 뺐다 — 사용자 원칙: 앞 준비 · 뒤 꼬리만)
        };
        float cutEnd = float.MaxValue;
        public int UltCutSkipMs, UltCutFrontMs, UltCutBackMs;   // 줄인 몫 — 앞(첫 타격 전) · 뒤(마지막 타격 뒤)                        // 점검 — 줄인 몫(ms, 1배속)
        // ── 고학년 보기 길이(설정) ── 기본: 판에서 처음 쓰는 고학년만 길게(원작 길이 · 7초 원칙), 두 번째부터 짧게(ULT_SHORT 초 남짓 · 1.15배).
        //   설정 「고학년 짧게 보기」 를 켜면 늘 짧게. 판 밖(전투 시범 · 점검)은 늘 길게(-ultshort 면 짧게 — 점검)
        const float ULT_SHORT = 3.5f;
        public bool UltShortShown;                       // 점검 — 이번 고학년을 짧게 보였나
        static long ultSeenRun = long.MinValue;
        static readonly HashSet<string> ultSeen = new HashSet<string>();
        static bool UltShortNow(string hid)
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultshort") >= 0) return true;
            if (Bolzena.RunUI.Settings.UltShort) return true;
            var seed = BattleBridge.Fight?.Port?.S?.Seed;
            if (seed == null) return false;
            if (seed.Value != ultSeenRun) { ultSeenRun = seed.Value; ultSeen.Clear(); }
            return !ultSeen.Add(hid);
        }

        void PlanCut(UnitView u, string hidUlt, float need = float.MaxValue)
        {
            var names = new List<string> { UltPlan.Anim }; names.AddRange(UltPlan.Chain);
            var (evs, offs, total) = u.ChainEvents(names);
            var uh = Bolzena.Fx.UltMotion.Hits(hidUlt);
            var hitsS = new List<float>();
            if (UltFitNow != null && UltFitNow.Hits != null && UltFitNow.Hits.Length > 0) foreach (var t in UltFitNow.Hits) hitsS.Add(t / 1000f);   // 원작 영상 맞춤 타격
            else if (uh != null && uh.Times.Count > 0) foreach (var t in uh.Times) hitsS.Add(t / 1000f);
            else if (UltPlan.Table != null) foreach (var t in UltPlan.Table.Strikes()) hitsS.Add(t / 1000f);
            if (need < float.MaxValue) { PlanCutNeed(evs, offs, total, hitsS, need); return; }
            evs.AddRange(hitsS);
            if (UltPlan.Table != null && UltPlan.Table.Moves) { evs.Add(UltPlan.Table.GoMs / 1000f); evs.Add(UltPlan.Table.LandMs / 1000f); }
            evs.Sort();
            if (evs.Count == 0) return;
            int PieceOf(float t) { int p = 0; for (int i = 0; i < offs.Count; i++) if (t >= offs[i]) p = i; return p; }
            float skipped = 0;
            if (CUT_FIX.TryGetValue(hidUlt, out var fix)) foreach (var k in fix) { cutSkips.Add(k); skipped += k.to - k.from; }
            // 앞 — 첫 타격 전 준비(기 모으기 · 대기)만. 때리는 가운데(첫 타격 ~ 마지막 타격)는 건드리지 않는다(사용자 원칙 2026-10-06).
            // 첫 타격이 2초 넘게 늦으면 그 앞 0.6초 ~ 첫 타격 0.8초 전을 건너뛴다(같은 조각 안일 때만 — 조각 경계를 넘으면 앞 조각 끝까지)
            UltCutFrontMs = 0; UltCutBackMs = 0;
            float firstHit = hitsS.Count > 0 ? hitsS[0] : (evs.Count > 0 ? evs[0] : 0);
            if (firstHit > 2f)
            {
                float a = 0.6f, b = firstHit - 0.8f;
                int pa = PieceOf(a), pb = PieceOf(b);
                if (pa != pb) b = offs[pa + 1] - 0.05f;
                if (b - a > 0.3f) { cutSkips.Add((a, b)); skipped += b - a; UltCutFrontMs = Mathf.RoundToInt((b - a) * 1000); }
            }
            // 꼬리 — 마지막 타격 뒤 2초 넘게 남으면 1.2초 뒤에 쉬는 동작으로(림(혼돈) 공중에서 2초 서 있던 것). 타격을 모르면 마지막 이벤트 뒤 1.5초
            float lastHit = hitsS.Count > 0 ? hitsS[hitsS.Count - 1] : evs[evs.Count - 1];
            float keep = hitsS.Count > 0 ? 1.2f : 1f;
            if (total - lastHit > 2f) { cutEnd = lastHit + keep; skipped += total - cutEnd; UltCutBackMs = Mathf.RoundToInt((total - cutEnd) * 1000); }
            UltCutSkipMs = Mathf.RoundToInt(skipped * 1000);
        }
        float Rm(float sec)
        {
            float t = sec;
            foreach (var k in cutSkips) { if (sec >= k.to) t -= k.to - k.from; else if (sec > k.from) t -= sec - k.from; }
            if (cutEnd < float.MaxValue) t = Mathf.Min(t, Rm0(cutEnd));
            return t;
        }
        float Rm0(float sec) { float t = sec; foreach (var k in cutSkips) { if (sec >= k.to) t -= k.to - k.from; else if (sec > k.from) t -= sec - k.from; } return t; }

        // ── 고학년 점검 몫 ──
        public string UltMoveWhy = "";                   // 이동형 판정 근거
        public bool UltMoved;
        public float UltTravel;                          // 몸짓이 스스로 앞으로 간 거리(싸움터 단위)
        public int UltMotionMs, UltCutMs, UltSndTailMs;  // 몸짓 길이 · 끝까지 못 하고 잘린 ms · 몸짓 끝에 줄여 끊은 소리 꼬리 ms
        public string UltSndMode = "";                   // events(스파인 SFX 칸) · fallback(시전 소리 + 맞는 소리)
        public int UltSndSpanMs;                         // 칸 소리 중 가장 늦게 끝나는 때(ms, 몸짓 시작에서)
        readonly List<AudioSource> ultHeld = new List<AudioSource>();
        const float SELF_TRAVEL = 2.5f;                  // 몸짓이 이만큼 넘게 제 몸을 옮기면(순간이동 · 뛰어들기) 따로 달려가지 않는다

        bool UltMover(UnitView u, string key, string hid, string anim, List<int> targets)
        {
            var names = new List<string>();
            if (UltPlan != null && UltPlan.Anim != null) { names.Add(UltPlan.Anim); names.AddRange(UltPlan.Chain); } else if (anim != null) names.Add(anim);
            if (UltPlan != null && UltPlan.Table != null)
            {
                var tw = UltPlan.Table;
                UltTravel = 0;
                UltMoved = tw.Moves && targets.Count > 0;
                UltMoveWhy = "표 " + tw.MoveType + (tw.Moves ? $" → {tw.Dest} 가기 {tw.GoMs} · 닿기 {tw.LandMs} · 돌아오기 {tw.ReturnMs}+{tw.BackMs}" : "");
                return UltMoved;
            }
            UltTravel = u.Travel(names);
            bool front = Look.MeleeArt(key);
            var byFx = Bolzena.Fx.FxRules.MeleeByFx(hid);
            bool shot = u.SpineArt && u.Sa.Skeleton.FindBone("Point_Attack1_Shot") != null;
            bool melee = front || (byFx == true && !shot);
            UltMoved = melee && targets.Count > 0 && UltTravel < SELF_TRAVEL;
            UltMoveWhy = (front ? "앞줄" : byFx == true && !shot ? "원작 평타 근접" : byFx == false || shot ? "원작 평타 원거리" : "모름") + $" · 몸짓 이동 {UltTravel:F1}" + (melee && UltTravel >= SELF_TRAVEL ? " (스스로 감)" : "");
            return UltMoved;
        }

        // 고학년 소리 — 스파인 SFX(n) 이벤트 칸을 그 사도의 고학년 소리 파일에 차례로 맞춰(웹판 sfx.js action · SfxMap.SlotMap) 그 시각에 튼다.
        // 맞는 소리 칸은 건너뛴다(맞는 순간 Impact 가 낸다). 칸 수가 파일 수와 안 맞으면 false — 예전처럼 시전 소리 + 맞는 소리
        bool UltSounds(string key, float speed)
        {
            UltSndMode = "fallback"; UltSndSpanMs = 0;
            if (UltPlan != null && UltPlan.Table != null && UltPlan.Table.Plays.Count > 0)
            {
                var (tkeys, tlen) = HeroClips(key);
                foreach (var pl in UltPlan.Table.Plays)
                {
                    var path = "hero/" + key + "/" + pl.Value;
                    // 표의 파일 이름이 원작 오타(마리: maire_ultimate02)면 그 사도 파일 가운데 꼬리가 같은 것(marie_ultimate02)으로.
                    //   없는 이름을 고학년 순간에 Resources.Load 하지 않는다(실패 · 경고가 그 프레임에 든다)
                    if (!tlen.ContainsKey(path))
                    {
                        int us = pl.Value.IndexOf('_');
                        string tail = us >= 0 ? pl.Value.Substring(us) : null;
                        string alt = tail == null ? null : tkeys.Find(k => k.EndsWith(tail, System.StringComparison.Ordinal));
                        if (alt == null) continue;
                        path = alt;
                    }
                    float at = Rm(pl.Key / 1000f) / Mathf.Max(0.01f, speed);
                    StartCoroutine(SndAt(path, at));
                    UltSndSpanMs = Mathf.Max(UltSndSpanMs, Mathf.RoundToInt((at + (tlen.TryGetValue(path, out var l) ? l : 0)) * 1000));
                }
                UltSndMode = "표 " + UltPlan.Table.SoundMode;
                return true;
            }
            if (UltPlan == null || UltPlan.Snd == null || UltPlan.Snd.Count == 0) return false;
            var (keys, len) = HeroClips(key);
            var slots = Bolzena.Fx.SfxMap.SlotsIn(keys, key, "ult");
            var ns = new List<int>();
            foreach (var e in UltPlan.Snd) ns.Add(e.N);
            var m = Bolzena.Fx.SfxMap.SlotMap(slots, ns);
            if (m == null) return false;
            foreach (var e in UltPlan.Snd)
                if (m.TryGetValue(e.N, out var path) && path != "hit")
                {
                    float at = Rm(e.T / 1000f) / Mathf.Max(0.01f, speed);   // 배속이면 몸짓이 빨라진 만큼 당긴다(높낮이는 그대로)
                    StartCoroutine(SndAt(path, at));
                    UltSndSpanMs = Mathf.Max(UltSndSpanMs, Mathf.RoundToInt((at + (len.TryGetValue(path, out var l) ? l : 0)) * 1000));
                }
            UltSndMode = "events";
            return true;
        }

        // 사도 소리 파일 목록 · 길이 — 싸움을 열 때 한 번(고학년 순간에 Resources.LoadAll 을 하지 않게)
        static readonly Dictionary<string, (List<string>, Dictionary<string, float>)> heroClips = new Dictionary<string, (List<string>, Dictionary<string, float>)>();
        static (List<string>, Dictionary<string, float>) HeroClips(string key)
        {
            if (heroClips.TryGetValue(key, out var got)) return got;
            var keys = new List<string>();
            var len = new Dictionary<string, float>();
            foreach (var c in Resources.LoadAll<AudioClip>("Sfx/hero/" + key)) { var k = "hero/" + key + "/" + c.name; keys.Add(k); len[k] = c.length; }
            return heroClips[key] = (keys, len);
        }

        // 표대로 오가기 — dash: goMs 에 출발해 landMs 에 적 앞(잔상) · teleport/leap: goMs~landMs(몸이 안 보이는 때) 가운데에 옮김.
        // returnMs(leap 은 homeMs)에 제자리로 — backMs 가 있으면 돌아서 Move 로 뛰어오고, 없으면 그 자리에서 옮긴다. 시각은 ÷ 배속
        bool travelling;
        IEnumerator UltTravelCo(UnitView u, Bolzena.Fx.UltWay w, Vector3 dest, float speed, float lastHit = 0)
        {
            travelling = true;
            float k = 1f / 1000f / Mathf.Max(0.01f, speed);
            float t0 = Time.time;
            float El() => Time.time - t0;
            float sp = 1f / Mathf.Max(0.01f, speed);
            float go = Rm(w.GoMs / 1000f) * sp, land = Mathf.Max(go, Rm(w.LandMs / 1000f) * sp);
            float home = Rm((w.HomeMs ?? w.ReturnMs) / 1000f) * sp, back = w.BackMs * k;
            home = Mathf.Max(home, lastHit + 0.25f);   // 마지막 타격 뒤에 돌아온다(루포 · 캐시 — 돌아온 뒤 제자리에서 때리던 것)
            var homePos = u.Home;
            while (El() < go) yield return null;
            Trace?.Invoke("move", "가기(" + w.MoveType + ")");
            if (w.MoveType == "dash")
            {
                float d = Mathf.Max(0.06f, land - go);
                StartCoroutine(Ghosts(u, d, new Color(0.75f, 0.9f, 1f, 0.22f)));
                yield return u.MoveTo(dest, d, Ease.InCubic);
            }
            else
            {
                while (El() < (go + land) / 2) yield return null;
                yield return u.MoveTo(dest, 0.01f);
            }
            Trace?.Invoke("move", "도착");
            while (El() < home) yield return null;
            Trace?.Invoke("move", "복귀");
            if (back > 0.02f)
            {
                u.Loop("Move");
                u.Sa.Skeleton.ScaleX = -u.Sa.Skeleton.ScaleX;          // 돌아서 뛴다
                yield return u.MoveTo(homePos, back, Ease.InOutCubic);
                u.Sa.Skeleton.ScaleX = -u.Sa.Skeleton.ScaleX;
                u.Idle();
            }
            else yield return u.MoveTo(homePos, 0.01f);
            travelling = false;
        }

        IEnumerator SndAt(string path, float at)
        {
            if (at > 0) yield return Clock.Wait(at);
            var a = Sfx.PlayHeld(path, 0.75f);
            if (a != null) ultHeld.Add(a);
        }

        // ── 원작 이펙트(com.bolzena.fx) ── 몸짓 시작에 건다. 고학년은 사도의 원작 고학년 한 벌(시각은 스파인 계획 · 이 몸짓의 배속),
        // 카드는 그 동작(Attack1 · Attack2 · Skill1)의 원작 이펙트. 원작 것이 없으면 false — 자체 Vfx 가 대신한다
        public bool LastFxOrig;                          // 점검 — 마지막 몸짓에 원작 이펙트를 틀었나
        public int LastFxParts;
        bool OrigFx(BattleEvent act, UnitView u, string anim, List<float> marks, float speed, bool ult, List<int> targets, Vector3? dashTo, List<float> hitTimes = null, bool selfFx = false)
        {
            List<float> hitMs = null;
            if (hitTimes != null) { hitMs = new List<float>(); foreach (var t in hitTimes) hitMs.Add(t * 1000f); }
            LastFxCall = null;
            LastFxOrig = false; LastFxParts = 0;
            var hs = Battle.Snapshot.Heroes[act.Actor.Index];
            string hid = hs.Id;
            var caster = u.Fx;
            if (dashTo.HasValue)   // 달려가 때리는 사도 — 이펙트는 닿는 자리에서(몸짓 시작 0.16초 안에 거기 간다)
            {
                var d = dashTo.Value; var b = caster;
                caster = new Bolzena.Fx.FxActor { FeetAt = () => d, Height = b.Height, Party = true, Key = b.Key, Point = b.Point, Muzzle = b.Muzzle };
            }
            var tfx = new List<Bolzena.Fx.FxActor>();
            foreach (var ti in targets) if (ti < Enemies.Count && Enemies[ti] != null) { var f = Enemies[ti].Fx; f.Alive = !Dead(ti); tfx.Add(f); }
            bool aoe = targets.Count > 1;
            Bolzena.Fx.FxCall c;
            if (ult)
            {
                if (!Bolzena.Fx.FxLibrary.HasUlt(hid)) return false;
                var sync = UltPlan != null && UltPlan.Anim != null ? Bolzena.Fx.SpineFx.Sync(UltPlan) : null;
                if (sync != null) sync.Speed = speed;
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
                c = Bolzena.Fx.BolzenaFx.Card(hid, caster, tfx, sync, null, aoe, hitMs, fxClock);
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
            foreach (var e in group) if (e.Kind == EventKind.Damage) n++;
            foreach (var e in group)
            {
                if (e.Kind == EventKind.Damage) { cur++; res[cur] = new List<BattleEvent>(); e.Hit = cur; e.Hits = n; }
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
                    if (e.Kind != EventKind.Damage || e.Value < m) { res[slot + m - 1].Add(e); continue; }
                    int done = 0;
                    for (int j = 0; j < m; j++)
                    {
                        int v = j == m - 1 ? e.Value - done : Mathf.RoundToInt(e.Value * (j + 1f) / m) - done;
                        done += v;
                        res[slot + j].Add(new BattleEvent
                        {
                            Kind = e.Kind, Actor = e.Actor, Target = e.Target, Value = v, Blocked = j == m - 1 ? e.Blocked : 0, Crit = e.Crit && j == m - 1,
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

        // 맞음 이펙트 — 원작 공용 타격(구운 낱장, 웹판 sparkFx). 갈래: 둔기 · 마법 · 그 밖은 베기
        static string FxHitKind(HitKind k) => k == HitKind.Blunt ? "blunt" : k == HitKind.Magic ? "magic" : "slash";
        static bool HitSheets => Bolzena.Fx.FxLibrary.Has("fx_common_hit_3_m");

        // 무리의 발 자리 — 살아 있는 대상들의 경계 상자 가운데(한 마리면 그 적). 전체 공격의 목표 지점
        Vector3 Mid(List<int> targets)
        {
            var alive = targets.FindAll(t => t < Enemies.Count && Enemies[t] != null && !Dead(t));
            if (alive.Count == 0) alive = targets.FindAll(t => t < Enemies.Count && Enemies[t] != null);
            if (alive.Count == 0) return new Vector3(3.5f, -1f, 0);
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var t in alive)
            {
                var f = Enemies[t].Feet;
                float hw = Enemies[t].Width() * 0.4f;
                x0 = Mathf.Min(x0, f.x - hw); x1 = Mathf.Max(x1, f.x + hw);
                y0 = Mathf.Min(y0, f.y); y1 = Mathf.Max(y1, f.y);
            }
            return new Vector3((x0 + x1) / 2, (y0 + y1) / 2, 0);
        }

        // 무리의 몸 가운데 — 경계 상자 가운데에 몸 높이의 중간(이펙트 · 충격파 · 카메라가 본다)
        Vector3 MidBody(List<int> targets)
        {
            var m = Mid(targets);
            float h = 0; int n = 0;
            foreach (var t in targets) if (t < Enemies.Count && Enemies[t] != null && !Dead(t)) { h += Enemies[t].Center.y - Enemies[t].Feet.y; n++; }
            return m + new Vector3(0, n > 0 ? h / n : 1f, 0);
        }

        Vector3? aoeAt;          // 전체 공격 중이면 무리 가운데 — 타격 이펙트를 거기 한 번에

        IEnumerator Ghosts(UnitView u, float dur, Color c)
        {
            float t = 0;
            while (t < dur + 0.04f)
            {
                u.Ghost(c, 0.22f);
                yield return Clock.Wait(0.045f);
                t += 0.045f;
            }
        }

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

        // 고학년 모으기 — 시전자 둘레로 빛이 모이고 바닥이 빛난다
        IEnumerator UltCharge(UnitView u, float until)
        {
            var heroColor = Battle.Snapshot.Heroes[u.Ref.Index].Tint;
            Vfx.Glow(u.Feet + new Vector3(0, 0.1f, 0), 4f, new Color(heroColor.r, heroColor.g, heroColor.b, 0.8f), until + 0.3f, 2f, "FX_IN_Crack_Round_Glow", 310, null, 1.2f);
            float t = 0;
            while (t < until)
            {
                Vfx.Burst(u.Center, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Spark", Count = 5, Speed = new Vector2(-9f, -5f), Life = new Vector2(0.15f, 0.25f), Size = new Vector2(0.06f, 0.14f), Radius = 1.6f,
                    C0 = heroColor, C1 = Color.white, Stretch = true, StretchK = 0.05f, Order = 335, Boost = 3.5f,
                });
                yield return Clock.Wait(0.06f);
                t += 0.06f;
            }
        }

        // 고학년 끝 충격파
        void UltFinish(Vector3 at, UnitView caster, bool orig = false)
        {
            var tint = Battle.Snapshot.Heroes[caster.Ref.Index].Tint;
            Clock.HitStop(0.24f);
            FieldRig.Shake(0.9f, 4f);
            FieldRig.Punch(at, 0.1f);
            // 세기는 크기 · 흔들림 · 고리 · 집중선으로 내고, 빛(블룸 · 흰 덮개)은 눌러 둔다 — 대상(320)의 실루엣과 숫자가 읽혀야 한다.
            // 흰 폭발 구름은 대상 뒤(315)에 깐다: 몸 둘레로 터져 나오고 몸을 덮지 않는다
            PostFx.Kick(chroma: 1f, lens: -0.4f, bloom: 0.2f);
            Vfx.Flash(new Color(tint.r, tint.g, tint.b, 0.08f), 0.2f, 1f, 305);
            ScreenFx.I.Lines(0.75f, Color.white, FieldRoot.TransformPoint(at), 302, 0.26f);
            Clock.Run(LinesOff(0.5f));
            Sfx.Play("ult_impact", 1f);
            Emit("ult_impact");
            Emit("ult_impact_" + caster.name.Replace("hero_", ""));
            if (orig) return;   // 원작 고학년 이펙트가 터지는 중 — 자체 고리 · 구름 · 불꽃은 원작이 없을 때만
            Vfx.Ring(at, 0.6f, 11f, 0.6f, new Color(1f, 0.95f, 0.85f, 0.75f), 1.6f, "FX_IN_Ring_Impact_wave_01", 340);
            Vfx.Ring(at, 0.3f, 7f, 0.5f, new Color(tint.r, tint.g, tint.b, 0.85f), 1.8f, "FX_IN_Ring_Impact_wave_01", 341);
            Vfx.Ring(at + new Vector3(0, -1f, 0), 0.5f, 12f, 0.7f, new Color(1f, 0.9f, 0.7f, 0.7f), 1.6f, "FX_IN_Ring_ShockWave_02", 339, null, 0.28f);
            Vfx.Sheet("fx_common_hit_explosion_1_m", at + new Vector3(0, -0.3f, 0), 2.0f, 315);
            Vfx.Sheet("fx_common_hit_22", at, 1.7f, 343, 0, false, 1f, true, 1.15f, new Color(tint.r, tint.g, tint.b, 0.8f));
            Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Spark", Count = 50, Speed = new Vector2(10f, 24f), Life = new Vector2(0.3f, 0.6f), Size = new Vector2(0.08f, 0.22f),
                C0 = new Color(1f, 0.97f, 0.88f), C1 = tint, Stretch = true, StretchK = 0.05f, Drag = 3f, Order = 344, Boost = 2.2f,
            });
            Vfx.Burst(at + new Vector3(0, -0.8f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_IN_Fragment_02", Count = 22, Speed = new Vector2(5f, 11f), Angle = 90, Spread = 140, Life = new Vector2(0.6f, 1f),
                Size = new Vector2(0.12f, 0.3f), C0 = new Color(0.75f, 0.65f, 0.55f), C1 = new Color(0.45f, 0.4f, 0.35f), Gravity = 2.2f, Spin = true, Additive = false, Boost = 1f, Order = 345,
            });
        }

        IEnumerator LinesOff(float after)
        {
            yield return Clock.WaitU(after);
            ScreenFx.I.Lines(0, Color.white, Vector2.zero);
        }

        // ── 맞는 순간 ──
        // fxHere — 전체 공격이면 첫 대상만 무리 가운데에 큰 이펙트를 내고, 나머지는 몸 반응 · 숫자만
        void Impact(BattleEvent e, UnitView by, bool ult, bool last, string hitSfx, bool crowd, bool fxHere = true)
        {
            int ti = e.Target.Index;
            if (ti >= Enemies.Count) return;
            var t = Enemies[ti];
            Trace?.Invoke("hit", $"피해 {e.Value}{(e.Crit ? "!" : "")} → {t.name.Replace("enemy_", "")}");
            var hud = EnemyHuds[ti];
            var kind = e.HitKind;
            var at = t.Center + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.2f, 0.25f), 0);
            bool crit = e.Crit;
            bool multi = e.Hits > 1;
            float flip = Random.value < 0.5f ? 1 : -1;

            // 멈칫 · 흔들림 · 줌 펀치
            float stop = crit ? 0.14f : ult ? 0.075f : multi ? 0.05f : 0.085f;
            if (!crowd || e.Target.Index == 0) Clock.HitStop(stop);
            FieldRig.Shake(crit ? 0.5f : ult ? 0.38f : multi ? 0.2f : 0.3f, crit ? 2.5f : 0.6f);
            FieldRig.Punch(aoeAt ?? at, crit ? 0.075f : ult ? 0.04f : 0.035f);

            // 몸 — 흰 번쩍임(반쯤만 — 온통 하얗게 채우면 실루엣이 날아간다) · 밀려남 · 붉게
            // 고학년은 여러 대가 겹치니 한 대의 빛을 더 줄인다(fade)
            float fade = ult ? 0.6f : 1f;
            bool bigFoe = ti < Battle.Snapshot.Enemies.Count && Battle.Snapshot.Enemies[ti].Boss;   // 큰 몸은 흰 채우기가 더 넓게 번진다
            t.Flash(new Color(1f, 0.97f, 0.9f), crit ? 0.18f : 0.11f, (crit ? 0.42f : 0.32f) * (ult ? 0.7f : 1f) * (bigFoe ? 0.6f : 1f));
            t.Knock(1f, crit ? 0.5f : multi ? 0.18f : 0.3f, crit ? 0.36f : 0.26f);
            if (!e.Crit) StartCoroutine(RedTint(t));

            // 전체 공격 — 이펙트는 무리 가운데에 한 번(조금 크게), 대상마다는 몸 반응 · 숫자만
            var fxAt = aoeAt ?? at;
            if ((fxHere || !aoeAt.HasValue) && HitSheets)
            {
                // 원작 공용 타격(웹판 sparkFx) — 자리는 원작 본 Point_Middle(전체 공격이면 무리 가운데)
                var tf = t.Fx;
                if (aoeAt.HasValue) { var p0 = fxAt; tf = new Bolzena.Fx.FxActor { FeetAt = () => t.Feet, Height = t.Height(), Party = false, Point = n => n == "Middle" ? p0 : (Vector3?)null }; }
                Bolzena.Fx.BolzenaFx.HitOrder = 182 + fxBoost;
                Bolzena.Fx.BolzenaFx.Hit(tf, FxHitKind(kind), !ult && !multi && last && by != null, crit, ult, e.Hit == 0);
                if (e.Blocked > 0) Bolzena.Fx.BolzenaFx.Common("shieldHit", t.Fx);
                if (crit)
                {
                    Vfx.Flash(new Color(1f, 0.9f, 0.7f, 0.06f), 0.12f);
                    PostFx.Kick(chroma: 0.6f, lens: -0.15f, bloom: 0.12f);
                    ScreenFx.I.Lines(0.45f, new Color(1f, 0.97f, 0.9f), FieldRoot.TransformPoint(fxAt), 302, 0.3f);
                    Clock.Run(LinesOff(0.28f));
                }
                else if (ult) PostFx.Kick(chroma: 0.35f, bloom: 0.08f);
            }
            else if (fxHere || !aoeAt.HasValue)
            {
                // 원작 타격 낱장 + 빛 + 불꽃 + 파편
                Color sparkC;
                switch (kind)
                {
                    case HitKind.Blunt:
                        Vfx.Sheet("fx_common_hit_1_m", fxAt, crit ? 1.6f : 1.15f, 182 + fxBoost, Random.Range(-20f, 20f));
                        Vfx.Sheet("fx_common_hit_2_m", fxAt, 1.1f, 183 + fxBoost);
                        sparkC = new Color(1f, 0.85f, 0.4f);
                        break;
                    case HitKind.Magic:
                        Vfx.Sheet("fx_mago_hit_1", fxAt, crit ? 1.15f : 0.9f, 182 + fxBoost, 0, false, 1.2f, true, 1.05f, new Color(0.6f, 0.68f, 1f, 0.85f * fade));
                        Vfx.Sheet("fx_common_hit_4_m", fxAt, 0.95f, 183 + fxBoost, Random.Range(-30f, 30f), flip < 0);
                        sparkC = new Color(0.6f, 0.8f, 1f);
                        break;
                    default:
                        Vfx.Sheet("fx_common_hit_slash_3", fxAt, crit ? 1.5f : 1.15f, 182 + fxBoost, Random.Range(-35f, 25f), flip < 0, 1.2f);
                        Vfx.Sheet("fx_common_hit_3_m", fxAt, 1.0f, 183 + fxBoost, Random.Range(-15f, 15f));
                        sparkC = new Color(1f, 0.9f, 0.55f);
                        break;
                }
                // 가운데 빛은 작고 옅게 — 대상의 몸을 덮지 않을 만큼(블룸 문턱도 넘지 않게 세기 1.2 안팎)
                Vfx.Glow(fxAt, crit ? 1.5f : 1.05f, new Color(1f, 0.92f, 0.75f, (crit ? 0.5f : 0.4f) * fade), 0.14f, 1.2f, null, 181 + fxBoost, null, 1.25f);
                Vfx.Ring(fxAt, 0.3f, crit ? 3.0f : 2.0f, crit ? 0.3f : 0.22f, new Color(1f, 0.95f, 0.85f, 0.6f * fade), 1.5f, "FX_IN_RIng_Hit_wave_01", 184 + fxBoost);
                Vfx.Burst(fxAt, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Spark", Count = crit ? 22 : multi ? 8 : 12, Speed = new Vector2(7f, crit ? 20f : 15f), Life = new Vector2(0.12f, 0.3f),
                    Size = new Vector2(0.05f, 0.13f), C0 = new Color(1f, 0.97f, 0.85f), C1 = sparkC, Stretch = true, StretchK = 0.045f, Drag = 4f, Angle = 10, Spread = 150, Order = 186 + fxBoost, Boost = 2.2f,
                });
                Vfx.Burst(fxAt, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Fragment_02", Count = crit ? 10 : 5, Speed = new Vector2(2.5f, 6.5f), Life = new Vector2(0.4f, 0.7f), Size = new Vector2(0.08f, 0.18f),
                    C0 = sparkC, C1 = Color.white, Gravity = 1.8f, Spin = true, Angle = 60, Spread = 120, Order = 185 + fxBoost, Boost = 1.6f,
                });
                if (crit)
                {
                    // 치명 — 빛 대신 모양으로(번개 갈래 · 충격파 · 집중선 · 색수차). 화면 번쩍임은 아주 옅게
                    Vfx.Sheet("fx_common_hit_22", fxAt, 1.25f, 187 + fxBoost, Random.Range(-10f, 10f), false, 1f, true, 1.1f, new Color(1f, 0.85f, 0.5f, 0.85f));
                    Vfx.Sheet("fx_common_hit_shockwave_1", fxAt, 1.3f, 188 + fxBoost);
                    Vfx.Flash(new Color(1f, 0.9f, 0.7f, 0.06f), 0.12f);
                    PostFx.Kick(chroma: 0.6f, lens: -0.15f, bloom: 0.12f);
                    ScreenFx.I.Lines(0.45f, new Color(1f, 0.97f, 0.9f), FieldRoot.TransformPoint(fxAt), 302, 0.3f);
                    Clock.Run(LinesOff(0.28f));
                }
                else if (ult) PostFx.Kick(chroma: 0.35f, bloom: 0.08f);
            }

            // 소리
            Sfx.Play(crit ? "hit_crit" : kind == HitKind.Blunt ? "hit_blunt" : kind == HitKind.Magic ? "hit_magic" : "hit_slash", crit ? 0.95f : 0.8f, 1f, true);
            if (hitSfx != null) Sfx.Play(hitSfx, 0.6f, 1f, true);
            if (e.Blocked > 0) Sfx.Play("block_hit", 0.6f);

            // 숫자 · 막대
            // 숫자는 몸 위쪽에 — 가운데에 띄우면 큰 적(보스)의 몸을 가린다
            var numAt = Vector3.Lerp(t.Center, t.Top, 0.55f);
            float numY = Mathf.Max(at.y + 0.55f, numAt.y);
            if (EnemyHud.OldLayout) Vfx.Number(new Vector3(at.x + 0.2f, numY, 0), e.Value, crit, e.Hit, null, false, e.Blocked);
            else
            {
                // 숫자는 머리 위 묶음 밑에서 떠올라 막대에 닿기 전에 사라지게(떠오르는 높이 · 연타 한 칸씩 · 글 높이만큼 아래),
                //   그래도 닿으면 묶음 밑으로 그린다(355 — 묶음 360~ 아래, 고학년 이펙트 325 · 사도 330 위)
                float room = hud.FieldBottom - 1.15f - (crit ? 0.2f : 0) - e.Hit * 0.32f;
                numY = Mathf.Max(t.Center.y, Mathf.Min(numY, room));
                Vfx.Number(new Vector3(at.x + 0.2f, numY, 0), e.Value, crit, e.Hit, null, false, e.Blocked, 355);
            }
            hud.SetHp(e.HpAfter);
            hud.SetBlock(e.BlockAfter);

            if (!firstHitShot && !crit && !ult) { firstHitShot = true; Emit("hit"); }
            if (!firstCritShot && crit) { firstCritShot = true; Emit("crit"); }
            if (ult) Emit(last ? "ult_last" : "ult_hit");
        }

        IEnumerator RedTint(UnitView u)
        {
            yield return Clock.Wait(0.06f);
            u.Tint(new Color(1f, 0.55f, 0.55f), 40f);
            yield return Clock.Wait(0.12f);
            u.Tint(Color.white, 8f);
        }

        // 맞는 순간에 함께 걸리는 것 — 상태 · 강인도 · 격파 · 쓰러짐 · 게이지 …
        IEnumerator ApplyConsequence(BattleEvent e, UnitView by)
        {
            var s = Battle.Snapshot;
            switch (e.Kind)
            {
                case EventKind.Block:
                    ApplyBlock(e);
                    break;
                case EventKind.Status:
                    yield return StatusFx(e, s);
                    break;
                case EventKind.Heal:
                    HealFx(e);
                    break;
                case EventKind.Damage:
                    // 몸짓 밖의 피해(지속 피해 · 반격 · 패시브) — 작게
                    if (e.Target.Index < Enemies.Count && Enemies[e.Target.Index] != null) Impact(e, null, false, false, null, true);
                    break;
                case EventKind.PartyHurt:
                    Hurt(e, e.Actor.Side == Side.Enemy && e.Actor.Index < Enemies.Count ? Enemies[e.Actor.Index].name.Replace("enemy_", "") : "fairymobcloserange", false);
                    break;
                case EventKind.Toughness:
                    if (e.Target.Index < EnemyHuds.Count && EnemyHuds[e.Target.Index] != null) EnemyHuds[e.Target.Index].SetTough(e.FAfter, !e.Up);
                    break;
                case EventKind.Break:
                    yield return BreakFx(e.Target.Index);
                    break;
                case EventKind.Death:
                    StartCoroutine(DeathFx(e.Target.Index, e.Boss));
                    // 보스가 쓰러지면 보스가 세운 소환물도 같이 쓰러진다(core FoeDeath) — 그 쪽지는 보스 연출(1.4초) 뒤에 와서,
                    //   보스 마지막 일격 화면에 소환물 체력바가 밝게 남고 보스가 사라진 뒤 따로 쓰러졌다. 같은 수에 쓰러진 다른 적을 지금 함께 쓰러뜨린다
                    if (e.Boss)
                    {
                        var ss = Battle.Snapshot.Enemies;
                        for (int j = 0; j < Enemies.Count && j < ss.Count; j++)
                            if (j != e.Target.Index && ss[j].Dead && Enemies[j] != null && Enemies[j].gameObject.activeSelf && !dyingFoes.Contains(Enemies[j]))
                                StartCoroutine(DeathFx(j, false));
                        yield return Clock.WaitU(1.4f);
                    }
                    break;
                case EventKind.Revive:
                    yield return ReviveFx(e.Target.Index, s);
                    break;
                case EventKind.UltGauge:
                    Hud.SetGauge(e.Value);
                    Hud.Ults[e.Actor.Index].Set(e.Value, s.Heroes[e.Actor.Index].UltMax);
                    break;
                case EventKind.UltReady:
                case EventKind.Discard:
                case EventKind.Exhaust:
                case EventKind.CardPlayed:
                    yield return Simple(e);
                    break;
                case EventKind.ApChanged:
                    Hud.SetAp(e.Value, s.MaxAp);
                    break;
                case EventKind.Draw:
                    yield return Deal(new List<CardInfo> { e.Card });
                    break;
                case EventKind.Intent:
                case EventKind.Recover:
                    yield return Simple(e);
                    break;
                case EventKind.FxCue:
                    yield return FxCueFx(e);
                    break;
                case EventKind.FoeUlt:
                    yield return FoeUltFx(e);
                    break;
                case EventKind.FoeCharge:
                    yield return FoeChargeFx(e);
                    break;
                case EventKind.Form:
                    yield return FormFx(e);
                    break;
            }
        }

        // 상태 — 적에게 거는 해로운 것은 색 번쩍 · 고리 · 떠오르는 글, 파티 · 사도에게 좋은 것은 초록 빛. 칩은 지금 모습으로
        IEnumerator StatusFx(BattleEvent e, BattleSnapshot s)
        {
            int i = e.Target.Index;
            string id = e.Text ?? "";
            bool good = e.Up || Bolzena.Core.R.BUFF_ST.Contains(id);
            var col = good ? new Color(0.55f, 1f, 0.6f) : id == "취약" ? new Color(1f, 0.5f, 0.4f) : new Color(0.75f, 0.55f, 1f);
            if (e.Target.Side == Side.Enemy)
            {
                if (i >= Enemies.Count || Enemies[i] == null) yield break;
                if (i < s.Enemies.Count) EnemyHuds[i].SetChips(s.Enemies[i].Chips);
                Sfx.Play(good ? "buff" : "debuff", 0.55f);
                Enemies[i].Flash(col, 0.4f, 0.55f);
                if (Bolzena.Fx.BolzenaFx.Common(good ? "buff" : "debuff", Enemies[i].Fx) == null)
                    Vfx.Glow(Enemies[i].Center, 2.4f, new Color(col.r, col.g, col.b, 0.7f), 0.5f, 1.8f, "FX_IN_Ring_ShockWave_03", 150);
                Vfx.Word(Enemies[i].Top + new Vector3(0, 0.25f, 0), id, 0.4f, col, new Color(0.15f, 0, 0.1f));
                string ic = ChipRow.IconOf(id, good);
                var icon = Make.Box("st", FieldRoot, ChipRow.IconSprite(ic), Enemies[i].Top + new Vector3(0.6f, 0, 0), new Vector2(0.6f, 0.6f), 470);
                Clock.Run(Clock.Tween(0.6f, t => { if (icon) { icon.transform.localPosition += new Vector3(0, Time.deltaTime * (good ? 0.3f : -0.3f), 0); Make.Alpha(icon, 1 - t); if (t >= 1) Destroy(icon.gameObject); } }));
            }
            else
            {
                var h = Heroes[Mathf.Clamp(i, 0, Heroes.Count - 1)];
                Hud.SetSnapshot(s);
                Sfx.Play(good ? "buff" : "debuff", 0.5f);
                h.Flash(col, 0.35f, 0.45f);
                if (Bolzena.Fx.BolzenaFx.Common(good ? "buff" : "debuff", h.Fx) == null)
                    Vfx.Glow(h.Center, 2.2f, new Color(col.r, col.g, col.b, 0.6f), 0.45f, 1.6f, "FX_IN_Ring_ShockWave_03", 150);
                Vfx.Word(h.Top + new Vector3(0, 0.3f, 0), id, 0.36f, col, new Color(0.05f, 0.12f, 0.05f));
            }
            yield return Clock.Wait(0.18f);
        }

        void HealFx(BattleEvent e)
        {
            if (e.Target.Side == Side.Party)
            {
                Hud.SetHp(e.HpAfter, false);
                Sfx.Play("buff", 0.5f, 1.1f);
                foreach (var h in Heroes)
                {
                    h.Flash(new Color(0.6f, 1f, 0.6f), 0.35f, 0.45f);
                    if (Bolzena.Fx.BolzenaFx.Common("heal", h.Fx) == null)
                    Vfx.Burst(h.Center, new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Glow", Count = 6, Speed = new Vector2(0.4f, 1.4f), Angle = 90, Spread = 60, Life = new Vector2(0.5f, 0.9f), Size = new Vector2(0.08f, 0.18f),
                        C0 = new Color(0.6f, 1f, 0.6f), C1 = Color.white, Order = 150, Boost = 1.6f, ShrinkTo = 0, Radius = 0.4f,
                    });
                }
                var at = Heroes[Mathf.Clamp(e.Target.Index, 0, Heroes.Count - 1)].Top;
                Vfx.Number(at + new Vector3(0, 0.3f, 0), e.Value, false, 0, new Color(0.6f, 1f, 0.6f));
            }
            else if (e.Target.Index < EnemyHuds.Count && EnemyHuds[e.Target.Index] != null)
            {
                EnemyHuds[e.Target.Index].SetHp(e.HpAfter, false);
                if (e.Target.Index < Enemies.Count && Enemies[e.Target.Index] != null) Bolzena.Fx.BolzenaFx.Common("heal", Enemies[e.Target.Index].Fx);
                Vfx.Number(Enemies[e.Target.Index].Top, e.Value, false, 0, new Color(0.6f, 1f, 0.6f));
            }
        }

        // 격파 — 칸이 다 깨졌다: 느려지고, 흑백으로 번쩍, 「격파!」, 조각이 흩어지고 적이 휘청(그로기)
        IEnumerator BreakFx(int i)
        {
            var t = Enemies[i];
            var at = t.Center;
            Clock.HitStop(0.12f);
            Clock.SlowMo(0.3f, 0.55f);
            Sfx.Play("break", 0.9f);
            FieldRig.Shake(0.65f, 3f);
            FieldRig.Punch(at, 0.09f);
            PostFx.Kick(chroma: 0.9f, lens: -0.28f, sat: -45f, bloom: 0.12f);
            Vfx.Flash(new Color(1f, 0.95f, 0.85f, 0.08f), 0.2f);
            t.Flash(new Color(1f, 0.9f, 0.5f), 0.45f, 0.6f);
            Bolzena.Fx.BolzenaFx.CommonOrderAdd = -150 + fxBoost;
            bool brk = Bolzena.Fx.BolzenaFx.Common("break", t.Fx) != null;   // 원작 기절 별(머리 위)
            Bolzena.Fx.BolzenaFx.CommonOrderAdd = -150;
            if (!brk) Vfx.Ring(at, 0.4f, 5f, 0.45f, new Color(1f, 0.85f, 0.4f, 0.85f), 1.8f, "FX_IN_Ring_ShockWave_01", 190 + fxBoost);
            if (!brk) Vfx.Glow(at, 3.0f, new Color(1f, 0.8f, 0.35f, 0.7f), 0.5f, 1.6f, "FX_IN_Crack_Round_Glow", 189 + fxBoost, null, 1.4f, Random.Range(0f, 360f));
            if (!brk) Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Sliced_Piece_Particle_Gray", Count = 22, Speed = new Vector2(4f, 11f), Life = new Vector2(0.5f, 0.9f), Size = new Vector2(0.12f, 0.32f),
                C0 = new Color(1f, 0.92f, 0.6f), C1 = new Color(0.8f, 0.9f, 1f), Gravity = 1.4f, Spin = true, Drag = 1.5f, Order = 191 + fxBoost, Boost = 2.4f,
            });
            var word = Vfx.Word(t.Top + new Vector3(0, 0.45f, 0), "격파!", 0.95f, new Color(1f, 0.9f, 0.45f), new Color(0.35f, 0.1f, 0), 1.4f, 1.6f, null, 475, 0.4f);
            word.colorGradient = new TMPro.VertexGradient(Color.white, Color.white, new Color(1f, 0.7f, 0.2f), new Color(1f, 0.7f, 0.2f));
            EnemyHuds[i].SetBroken(true);
            t.Loop(t.Resolve("Groggy"));
            Emit(Battle.Snapshot.Enemies[i].Boss ? "boss_break" : "break");
            yield return Clock.Wait(0.35f);
        }

        // 쓰러지는 연출을 이미 건 적(보스와 함께 쓰러진 소환물이 뒤에 오는 제 쪽지로 두 번 쓰러지지 않게) — 웨이브마다 새 UnitView 라 비우지 않아도 된다
        readonly HashSet<UnitView> dyingFoes = new HashSet<UnitView>();

        IEnumerator DeathFx(int i, bool boss)
        {
            if (i < 0 || i >= Enemies.Count || Enemies[i] == null) yield break;
            var t = Enemies[i];
            if (!dyingFoes.Add(t)) yield break;
            var at = t.Center;
            EnemyHuds[i].Hide();
            Sfx.Play("death_enemy", 0.7f);
            if (boss)
            {
                // 마지막 일격 — 크게 느려지고, 폭발 · 하얀 화면
                Clock.SlowMo(0.18f, 1.4f);
                Clock.HitStop(0.3f);
                FieldRig.Shake(1.1f, 5f);
                FieldRig.Punch(at, 0.12f);
                // 마지막 일격 — 세상이 어두워지고 보스만 하얗게 남는다
                // 블룸 · 흰 구름은 보스 뒤(316)로 — 앞을 덮으면 무엇이 쓰러지는지 안 보인다
                PostFx.Kick(chroma: 1f, lens: -0.5f, bloom: 0.15f);
                t.SetOrder(320);
                ScreenFx.I.Dim(0.82f, 300, 30f);
                ScreenFx.I.Lines(0.7f, Color.white, FieldRoot.TransformPoint(at), 302, 0.3f);
                Clock.Run(BossKillLift(t));
                Vfx.Sheet("fx_common_hit_explosion_1_m", at, 2.3f, 316, 0, false, 1f, false, 1f, new Color(1f, 0.8f, 0.6f, 0.6f));
                Vfx.Glow(at, 5f, new Color(1f, 0.7f, 0.35f, 0.35f), 0.7f, 1.2f, null, 312, null, 1.3f);
                Vfx.Ring(at, 1f, 16f, 0.9f, new Color(1f, 0.9f, 0.7f, 0.8f), 1.6f, "FX_IN_Ring_Impact_wave_01", 342, null, 1f, true);
                Vfx.Burst(at, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Spark", Count = 70, Speed = new Vector2(8f, 26f), Life = new Vector2(0.4f, 0.9f), Size = new Vector2(0.08f, 0.24f),
                    C0 = new Color(1f, 0.95f, 0.8f), C1 = new Color(1f, 0.7f, 0.3f), Stretch = true, StretchK = 0.05f, Drag = 2f, Order = 343, Boost = 2.2f,
                });
                Sfx.Play("ult_impact", 1f, 0.8f);
                Emit("boss_kill");
            }
            t.Flash(new Color(1f, 0.97f, 0.9f), 0.5f, boss ? 0.2f : 0.35f);
            float d = t.Play(t.Resolve("Die"), boss ? 1.4f : 1.6f, false);
            yield return Clock.Wait(Mathf.Min(d, 0.7f));
            if (!t || !dyingFoes.Contains(t)) yield break;   // 그사이 다음 웨이브가 들어와 지워졌다 · 되살아났다(ReviveFx)
            // 흰 실루엣으로 녹아 위로 흩어진다 — 원작 쓰러짐 연기, 없으면 자체 빛망울
            bool smoke = Bolzena.Fx.BolzenaFx.Common("kill", t.Fx) != null;
            for (int k = 0; k < (smoke ? 0 : 6); k++)
                Vfx.Burst(t.Feet + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, t.Height() * 0.8f), 0), new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Glow", Count = 4, Speed = new Vector2(0.5f, 2f), Angle = 90, Spread = 50, Life = new Vector2(0.5f, 1f), Size = new Vector2(0.1f, 0.25f),
                    C0 = new Color(1f, 0.95f, 0.85f), Order = 70, Boost = 2.5f, ShrinkTo = 0,
                });
            yield return Clock.Tween(0.5f, k => { if (t && dyingFoes.Contains(t)) { t.SetAlpha(1 - k); t.Flash(Color.white, 0.05f); } });
            if (t && dyingFoes.Contains(t)) t.gameObject.SetActive(false);
        }

        // 되살아남(core revive — 호바깅 · 불쌍한의 「재 속」 부활, 산사모 가사에서 일어섬) — 쓰러짐 표(dyingFoes)에서 빼고 몸 · 머리 위 묶음을 다시 켠다.
        //   연출: 발밑 불씨가 솟고 몸이 아래에서 떠오르며 흰빛으로 번쩍 → 「부활!」 · 체력 · 강인도 · 의도 칸이 지금 값으로
        IEnumerator ReviveFx(int i, BattleSnapshot s)
        {
            if (i < 0 || i >= Enemies.Count || Enemies[i] == null) yield break;
            var t = Enemies[i];
            dyingFoes.Remove(t);
            Debug.Log($"[Battle] 되살아남 적 {i} {t.name} · 쓰러짐 연출 중이었나 {!t.gameObject.activeSelf}");
            t.gameObject.SetActive(true);
            t.SetAlpha(0);
            t.Idle();
            var es = i < s.Enemies.Count ? s.Enemies[i] : null;
            if (i < EnemyHuds.Count && EnemyHuds[i] != null) EnemyHuds[i].Show(es);
            Sfx.Play("buff", 0.6f, 0.8f);
            Vfx.Burst(t.Feet + new Vector3(0, 0.1f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_IN_Glow", Count = 18, Speed = new Vector2(1f, 3.5f), Angle = 90, Spread = 40, Life = new Vector2(0.5f, 1.0f), Size = new Vector2(0.1f, 0.26f),
                C0 = new Color(1f, 0.75f, 0.35f), C1 = new Color(1f, 0.95f, 0.8f), Order = 150, Boost = 2.2f, ShrinkTo = 0, Radius = 0.5f,
            });
            Vfx.Ring(t.Feet + new Vector3(0, 0.15f, 0), 0.3f, 3.2f, 0.5f, new Color(1f, 0.85f, 0.5f, 0.8f), 1.6f, "FX_IN_Ring_ShockWave_01", 149);
            var home = t.transform.localPosition;
            yield return Clock.Tween(0.45f, k =>
            {
                if (!t) return;
                t.SetAlpha(k);
                t.transform.localPosition = home + new Vector3(0, -0.35f * (1 - k), 0);
                t.Flash(Color.white, 0.6f * (1 - k));
            });
            if (!t) yield break;
            t.transform.localPosition = home;
            t.SetAlpha(1);
            t.Flash(new Color(1f, 0.95f, 0.8f), 0.4f, 0.4f);
            Vfx.Word(t.Top + new Vector3(0, 0.35f, 0), "부활!", 0.55f, new Color(1f, 0.85f, 0.45f), new Color(0.3f, 0.12f, 0), 1.1f, 1.3f);
            yield return Clock.Wait(0.35f);
        }

        IEnumerator BossKillLift(UnitView t)
        {
            yield return Clock.WaitU(1.1f);
            ScreenFx.I.Dim(0, 300, 2.5f);
            ScreenFx.I.Lines(0, Color.white, Vector2.zero);
        }

        // ── 적의 몸짓 ──
        IEnumerator EnemyAct(BattleEvent act, List<BattleEvent> group)
        {
            int ei = act.Actor.Index;
            if (ei >= Enemies.Count) SpawnSummons();
            if (ei < 0 || ei >= Enemies.Count) yield break;
            var u = Enemies[ei];
            Split(group, out var pre, out var byHit, out int hits);
            bool foeUlt = act.Text == "foeult";
            bool spend = act.Anim == "charge";   // 모은 힘을 쏟는 턴(엘리트 · 일반 힘 모으기)
            bool heavy = act.Text == "heavy" || foeUlt;
            string anim = u.AnimFor(act.Motion);
            float speed = foeUlt ? 1.05f : heavy ? 1.1f : 1.25f;
            var marks = u.Strikes(anim);
            var times = HitTimes(marks, Mathf.Max(1, hits), speed);
            string key = u.name.Replace("enemy_", "");

            u.SetOrder(60);
            foreach (var en in Enemies) if (en != u) en.Tint(new Color(0.62f, 0.62f, 0.68f));
            if (act.Up)
            {
                Vfx.Word(u.Top + new Vector3(0, 1.0f, 0), "즉시 행동!", 0.42f, new Color(1f, 0.85f, 0.3f), new Color(0.3f, 0.1f, 0), 1.0f, 1.2f);
                Sfx.Play("turn_start", 0.5f, 1.3f);
                yield return Clock.Wait(0.35f);
            }
            if (spend)
            {
                // 모은 힘을 쏟는다 — 그 수 이름 띠(주황, 보통 수 이름보다 크게) · 주황 고리 · 집중선
                Emit("foecharge_spend");
                ChargeBand(u, "「" + (act.Say ?? "") + "」", 0.42f, 1.5f);
                u.Flash(new Color(1f, 0.6f, 0.25f), 0.35f, 0.5f);
                Vfx.Glow(u.Center, 3f, new Color(1f, 0.5f, 0.15f, 0.75f), 0.4f, 2.6f, "FX_IN_Ring_ShockWave_03", 186);
                if (heavy) ScreenFx.I.Lines(0.6f, new Color(1f, 0.55f, 0.25f), FieldRoot.TransformPoint(u.Center), 302, 0.3f);
                Sfx.Play("turn_start", 0.45f, 0.8f);
                yield return Clock.Wait(0.4f);
            }
            else if (!string.IsNullOrEmpty(act.Say))
                Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 1.2f, 3.45f), 0), "「" + act.Say + "」", 0.24f, new Color(1f, 0.95f, 0.88f), new Color(0.1f, 0.05f, 0.1f), 1.3f, 1f, null, 472, 0.2f);
            if (heavy && !foeUlt && !spend)
            {
                // 큰 수 — 붉은 경고, 집중선
                Vfx.Word(u.Top + new Vector3(0, 0.6f, 0), "강타!", 0.7f, new Color(1f, 0.5f, 0.35f), new Color(0.3f, 0, 0), 1.0f, 1.5f);
                ScreenFx.I.Lines(0.55f, new Color(1f, 0.4f, 0.3f), FieldRoot.TransformPoint(u.Center), 302, 0.3f);
                yield return Clock.Wait(0.3f);
            }
            float dur = u.Play(anim, speed);
            bool attack = hits > 0;
            Sfx.Play("monster/" + key + "/" + key + (attack ? (heavy ? "_skillcast" : "_basicattack") : "_skillcast"), 0.7f);
            bool lunge = attack && key == "fairymobcloserange";
            if (lunge) StartCoroutine(u.MoveTo(u.Home + new Vector3(-2.4f, 0, 0), 0.18f, Ease.InCubic));
            else if (attack) StartCoroutine(u.MoveTo(u.Home + new Vector3(-0.4f, 0, 0), 0.15f));

            foreach (var e in pre) yield return ApplyConsequence(e, u);
            float elapsed = 0;
            int k = 0;
            foreach (var kv in byHit)
            {
                float at = k < times.Count ? times[k] : times[times.Count - 1] + 0.12f;
                while (elapsed < at) { yield return null; elapsed += Time.deltaTime; }
                if (foeUlt) FoeUltBeat(u, k == byHit.Count - 1);
                else if (spend && k == 0) { Vfx.Flash(new Color(1f, 0.55f, 0.2f, 0.2f), 0.15f); FieldRig.Shake(0.45f, 2f); }   // 쏟는 첫 수 — 조금 센 타격감(보스 고학년 붉은 번쩍보다 약하게)
                foreach (var e in kv.Value)
                {
                    if (e.Kind == EventKind.PartyHurt) Hurt(e, key, heavy);
                    else yield return ApplyConsequence(e, u);
                }
                k++;
            }
            if (heavy) ScreenFx.I.Lines(0, Color.white, Vector2.zero);
            float tail = foeUlt ? dur : Mathf.Min(dur, elapsed + 0.4f);   // 보스 고학년은 몸짓을 끝까지
            while (elapsed < tail) { yield return null; elapsed += Time.deltaTime; }
            if (foeUlt) u.Idle();
            if (u.Feet != u.Home)
            {
                yield return u.Return(0.2f);
            }
            u.SetOrder(u.BaseOrder);
            foreach (var en in Enemies) if (en) en.Tint(Color.white);
            yield return Clock.Wait(0.15f);
        }

        void Hurt(BattleEvent e, string enemyKey, bool heavy)
        {
            var h = Heroes[e.Target.Index];
            var at = h.Center + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.2f), 0);
            Clock.HitStop(heavy ? 0.13f : 0.07f);
            FieldRig.Shake(heavy ? 0.6f : 0.32f, heavy ? 3f : 1f);
            FieldRig.Punch(at, heavy ? 0.06f : 0.03f);
            h.Flash(e.Value > 0 ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 0.85f, 1f), 0.16f, 0.5f);
            h.Knock(-1f, heavy ? 0.45f : 0.25f);
            if (e.Value > 0)
            {
                ScreenFx.I.Hurt(heavy ? 0.85f : 0.55f);
                if (HitSheets) { Bolzena.Fx.BolzenaFx.HitOrder = 182; Bolzena.Fx.BolzenaFx.Hit(h.Fx, Bolzena.Fx.MotionTables.HitKindEnemy(enemyKey), heavy); }   // 원작 공용 타격(사도가 맞으면 뒤집힌다)
                else
                {
                    Vfx.Sheet("fx_common_hit_1_m", at, 1.0f, 182, Random.Range(-20f, 20f), true, 1f, false, 1f, new Color(1f, 0.6f, 0.5f));
                    Vfx.Burst(at, new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Spark", Count = 10, Speed = new Vector2(6f, 12f), Life = new Vector2(0.12f, 0.3f), Size = new Vector2(0.06f, 0.14f),
                        C0 = Color.white, C1 = new Color(1f, 0.4f, 0.3f), Stretch = true, StretchK = 0.045f, Drag = 4f, Angle = 170, Spread = 140, Order = 186, Boost = 3.5f,
                    });
                }
                if (Random.value < 0.4f) Sfx.Voice(h.name.Replace("hero_", ""), "hit");
            }
            if (e.Blocked > 0)
            {
                if (Bolzena.Fx.BolzenaFx.Common("shieldHit", h.Fx) == null) Vfx.Glow(at, 2f, new Color(0.5f, 0.8f, 1f, 0.9f), 0.25f, 3f, "FX_IN_Ring_ShockWave_03", 187);
                Sfx.Play("block_hit", 0.7f);
            }
            Sfx.Play("monster/" + enemyKey + "/" + enemyKey + "_basicattack_hit", 0.7f, 1f, true);
            Sfx.Play("hurt", 0.5f, 1f, true);
            Vfx.Number(at + new Vector3(0, 0.5f, 0), e.Value, false, e.Hit, null, true, e.Blocked);
            Hud.SetHp(e.HpAfter);
            Hud.SetBlock(e.BlockAfter);
            if (heavy)
            {
                Vfx.Flash(new Color(1f, 0.3f, 0.2f, 0.25f), 0.25f);
                PostFx.Kick(chroma: 0.8f, lens: -0.2f);
            }
            if (!firstHurtShot) { firstHurtShot = true; Emit("enemy_attack"); }
        }

        // ── 고학년 ──
        public Bolzena.Fx.ActPlan UltPlan;               // 마지막 고학년 몸짓 계획(점검 로그)
        public string UltAnimLog;                        // 마지막 고학년에 튼 조각들
        public int UltTargets;                           // 마지막 고학년이 맞힌 적 수
        IEnumerator UltFlow(int hero, int target)
        {
            var s = Battle.Snapshot;
            var hs = s.Heroes[hero];
            InUlt = true;
            Emit("ult_start");
            if (!Bolzena.RunUI.Settings.SkipCutin) yield return UltCutin.Play(ScreenRoot, hs.Key, hs.Name, hs.UltName, hs.Tint);
            else { Sfx.Voice(hs.Key, "ultimate", "shout"); Vfx.Flash(new Color(1, 1, 1, 0.25f), 0.15f); }   // 컷인 건너뛰기(설정) — 목소리 · 번쩍만
            Demo.UltLap.Begin("SD 시작 " + hs.Key);
            var evs = Battle.UseUlt(hero, target);
            Demo.UltLap.Lap("UseUlt");
            // Act 를 고학년으로 연출하고, 나머지는 보통으로
            yield return Present(evs);
            InUlt = false;
        }

        /// <summary>점검(-cardsheet) — 카드 없이 그 사도의 몸짓 하나를 적 하나에게(피해 1) 그대로 연출한다. 판은 바뀌지 않는다.</summary>
        public IEnumerator DemoMotion(int hero, Motion m, int target)
        {
            var hs = Battle.Snapshot.Heroes[hero];
            var es = target < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[target] : null;
            var act = new BattleEvent { Kind = EventKind.Act, Actor = UnitRef.Party(hero), Target = UnitRef.Enemy(target), Motion = m, HitKind = Look.Hero(hs.Id).Hit };
            var dmg = new BattleEvent { Kind = EventKind.Damage, Actor = UnitRef.Party(hero), Target = UnitRef.Enemy(target), Value = 1, Hit = 0, Hits = 1, HpAfter = es != null ? es.Hp : 1 };
            yield return HeroAct(act, new List<BattleEvent> { dmg });
        }

        public void RequestUlt(int hero) => request = (ReqKind.Ult, hero, UltAim >= 0 ? UltAim : FirstAliveEnemy());
        public void DemoSelectUlt(int hero) => TapUlt(hero);
        public bool InUlt { get; private set; }            // 고학년 연출 중(프레임 재기)
        public void RequestEnd() => request = (ReqKind.End, 0, 0);
    }
}
