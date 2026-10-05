using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 사도 고유 효과 틀(2026-10-05 셋째, bolzena-content/고유효과.md §5) — 데이터만으로 사도마다 자기 규칙을 짓는 범용 부품.
    /// 조건: 갈래(ifChoice) · 무작위(ifRandom) · 손 수(ifHand) · 더미 수(ifPile) · N장째(ifNth) · 같은 사도 연속(ifStreak) · 셋 다 냄(ifAllHeroes) · 적 상태(ifFoe) · 카드 값(ifCardSt).
    /// 비례: 이번 턴 낸 카드(perPlayed) · 더미 장수(perPile) · 카드 값(perCardSt) · 일의 값(perEvent — 초과 피해 · 넘친 치유 · 막아 낸 양).
    /// 효과: 예약(later · afterCards) · 함정(trap) · 혼란(confuse) · 저절로 내기(autoPlay) · 다른 사도 카드 대신 발동(castOther) ·
    /// 더미 조작(pull · exileFrom) · 적 버프 지우기(dispel) · 열 옮기기(moveRow) · 판 단위 성장(growRun).
    /// </summary>
    public sealed partial class Battle
    {
        const string PLAYED_PER = "\u0000낸", PILE_PER = "\u0000더미", CARDST_PER = "\u0000카드값", EVENT_PER = "\u0000일값";

        List<string> PileOf(string name) => name switch { "draw" => Draw, "discard" => Discard, "gone" => Gone, "hand" => Hand, _ => Discard };

        int KitCount(FxCtx ctx, string id)
        {
            if (id.StartsWith(PLAYED_PER)) { var tg = id.Substring(PLAYED_PER.Length); return tg.Length == 0 ? PlayTags.Count : PlayTags.Count(t => t.Contains(tg)); }
            if (id.StartsWith(PILE_PER)) return PileOf(id.Substring(PILE_PER.Length)).Count;
            if (id.StartsWith(CARDST_PER)) return CardStOf(ctx.CardId, id.Substring(CARDST_PER.Length));
            if (id.StartsWith(EVENT_PER)) return ctx.EventV / Math.Max(1, int.Parse(id.Substring(EVENT_PER.Length)));
            return -1;
        }

        /// <summary>효과 조각 하나(사도 고유 효과 틀) — 처리했으면 true.</summary>
        bool FxKit(Fx f, FxCtx ctx, ref bool gate)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                // ── 조건 ──
                case FxK.IfChoice: gate = ctx.Choice == f.N; return true;
                case FxK.IfRandom: gate = Rng.Next() < (f.Pct > 0 ? f.Pct : 0.5); return true;
                case FxK.IfHand: gate = Hand.Count <= f.N; return true;
                case FxK.IfPile: gate = PileOf(f.From ?? "discard").Count >= Math.Max(1, f.N); return true;
                case FxK.IfNth: gate = ctx.PlayedBefore + 1 == f.N; return true;
                case FxK.IfStreak:
                    {
                        int k = 0;
                        for (int i = PlayLog.Count - 1; i >= 0 && owner != null && PlayLog[i].hero == owner.Key; i--) k++;
                        gate = k >= Math.Max(2, f.N); return true;
                    }
                case FxK.IfAllHeroes: gate = AliveParty().All(u => PlayLog.Any(p => p.hero == u.Key) || (ctx.Card && owner == u)); return true;
                case FxK.IfFoe:
                    {
                        var t = Resolve(ctx, "oneEnemy").FirstOrDefault();
                        gate = t != null && (f.Id switch
                        {
                            "broken" => t.Broken,
                            "tough" => t.ToughMax > 0 && t.Tough / t.ToughMax <= (f.Pct > 0 ? f.Pct : 0.5),
                            "guarded" => t.Block + t.Shield > 0,
                            "attack" => t.Intent != null && IsHit(t.Intent.T),
                            _ => false,
                        });
                        if (f.Not) gate = !gate;
                        return true;
                    }
                case FxK.IfCardSt: gate = CardStOf(ctx.CardId, f.Id) >= Math.Max(1, f.N); if (f.Not) gate = !gate; return true;
                // ── 비례 ──
                case FxK.PerPlayed: ctx.PerStack = PLAYED_PER + (f.Id ?? ""); return true;
                case FxK.PerPile: ctx.PerStack = PILE_PER + (f.From ?? "discard"); return true;
                case FxK.PerCardSt: ctx.PerStack = CARDST_PER + f.Id; return true;
                case FxK.PerEvent: ctx.PerStack = EVENT_PER + (f.Per > 0 ? f.Per : 1); return true;
                // ── 예약 · 함정 ──
                case FxK.Later:
                    Later.Add(new LaterRec { Turn = Turn + Math.Max(1, f.N), Owner = owner?.Key, Name = ModSrc, Target = ctx.TargetIdx, Fx = f.Then ?? new List<Fx>() });
                    Say($"예약 — {Math.Max(1, f.N)}턴 뒤"); return true;
                case FxK.AfterCards:
                    Later.Add(new LaterRec { Cards = Math.Max(1, f.N), Born = PlaysTotal, Owner = owner?.Key, Name = ModSrc, Target = ctx.TargetIdx, Fx = f.Then ?? new List<Fx>() });
                    Say($"예약 — 카드 {Math.Max(1, f.N)}장 뒤"); return true;
                case FxK.Trap:
                    foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy").Where(x => x.Side == Side.Enemy))
                    { t.Traps.Add(new LaterRec { Owner = owner?.Key, Name = ModSrc, Target = t.Idx, Fx = f.Then ?? new List<Fx>(), Else = f.Else }); StatusCue(t, "함정"); Say($"{t.Name}: 함정을 깔았다"); }
                    return true;
                case FxK.Confuse:
                    if (f.Pct > 0 && Rng.Next() >= f.Pct) { Say("혼란 — 빗나갔다"); return true; }
                    foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy").Where(x => x.Side == Side.Enemy)) { t.Confused = true; StatusCue(t, "혼란"); Say($"{t.Name}: 혼란 — 다음 공격이 다른 적에게"); }
                    return true;
                // ── 카드 ──
                case FxK.AutoPlay:
                    for (int k = 0; k < f.NOr1 && Over == null; k++)
                    {
                        var ok = Hand.Select((id, i) => (id, i)).Where(x => { var c = CardOf(x.id); return c != null && !c.IsStatus && !c.IsCurse && CanPlay(x.id, true) == null && (f.Id == null || c.HasTag(f.Id)) && FxMatch(f, x.id, owner); }).ToList();
                        var foes = AliveEnemies();
                        if (ok.Count == 0 || foes.Count == 0) break;
                        var pick = ok[Rng.Int(ok.Count)];
                        Say($"「{CardOf(pick.id).Name}」 — 저절로 나간다");
                        FreeOnce.Add(pick.id);
                        if (!PlayCard(pick.i, foes[Rng.Int(foes.Count)].Idx, new PlayOpts { Auto = true, Scale = f.Ratio > 0 ? f.Ratio : 1 }).Ok) FreeOnce.Remove(pick.id);
                    }
                    return true;
                case FxK.CastOther:
                    {   // 손의 다른 사도 카드 하나의 효과를 대신 돌린다(카드는 손에 남는다 · 그 카드 주인의 능력치로)
                        var ok = Hand.Where(id => { var c = CardOf(id); return c != null && c.Hero != null && (owner == null || c.Hero != owner.Key) && HeroUnit(c.Hero) is Unit h && !h.Dead && (f.Id == null || c.Type == f.Id); }).ToList();
                        if (ok.Count == 0) return true;
                        var id = ok[Rng.Int(ok.Count)]; var cv = CardOf(id); var who = HeroUnit(cv.Hero);
                        Say($"「{cv.Name}」 — 대신 발동");
                        StatusCue(owner ?? who, $"대신 「{cv.Name}」", true);
                        RunFx(cv.Fx, new FxCtx { Owner = who, TargetIdx = ctx.TargetIdx, HitTags = CardHitTags(id, cv), Card = true, Type = cv.Type, CardId = id, Cost = cv.Cost, Scale = f.Ratio > 0 ? f.Ratio : 1 });
                        return true;
                    }
                case FxK.Pull:
                    {   // 더미에서 n 장 → 손(기본) · 뽑을 더미 맨 위(to top)
                        var src = PileOf(f.From ?? "discard");
                        for (int k = 0; k < f.NOr1 && src.Count > 0; k++)
                        {
                            var okIdx = Enumerable.Range(0, src.Count).Where(i => FxMatch(f, src[i], owner)).ToList();
                            if (okIdx.Count == 0) break;
                            int at = f.At == "bottom" ? okIdx[0] : f.At == "random" ? okIdx[Rng.Int(okIdx.Count)] : okIdx[okIdx.Count - 1];
                            ctx.Pulled = src[at];
                            var id = src[at]; src.RemoveAt(at);
                            if (f.To == "top") { Draw.Add(id); CardCue(id, f.From ?? "discard", "draw", "pull"); }
                            else if (Hand.Count < R.HAND_MAX) { Hand.Add(id); CardCue(id, f.From ?? "discard", "hand", "pull"); }
                            else { Discard.Add(id); CardCue(id, f.From ?? "discard", "discard", "pull"); }
                            Say($"「{CardOf(id)?.Name}」 — {(f.To == "top" ? "뽑을 더미 맨 위로" : "손으로")}");
                        }
                        MergePile(Hand, "hand");
                        return true;
                    }
                case FxK.ExileFrom:
                    {
                        if (f.From == "pulled")
                        {   // 방금 꺼낸 카드를 소멸
                            var pid = ctx.Pulled; if (pid == null) return true;
                            foreach (var pile in new[] { Hand, Draw, Discard }) { int pi = pile.LastIndexOf(pid); if (pi >= 0) { pile.RemoveAt(pi); Exile(pid, pile == Hand ? "hand" : pile == Draw ? "draw" : "discard", "burn"); ctx.EventV = 1; break; } }
                            return true;
                        }
                        var src = PileOf(f.From ?? "discard");
                        int burned = 0;
                        for (int k = 0; k < f.NOr1 && src.Count > 0; k++)
                        {
                            var okIdx = Enumerable.Range(0, src.Count).Where(i => FxMatch(f, src[i], owner)).ToList();
                            if (okIdx.Count == 0) break;
                            int at = okIdx[Rng.Int(okIdx.Count)]; var id = src[at]; src.RemoveAt(at); burned++; ctx.EventV = burned;
                            Exile(id, f.From ?? "discard", "burn");
                            Emit("exhaust", new EmitInfo { Hero = CardOf(id)?.Hero, By = Acting, Id = id });
                            if (Over != null) return true;
                        }
                        return true;
                    }
                // ── 적 · 사도 ──
                case FxK.Dispel:
                    {
                        int gone = 0;
                        foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy").Where(x => x.Side == Side.Enemy))
                            for (int k = 0; k < f.NOr1Of(f.IV); k++)
                            {
                                var id = t.Status.Keys.FirstOrDefault(s => R.BUFF_ST.Contains(s) || CounterOf(t, s) != null);
                                if (id == null) break;
                                SetStRaw(t, id, St(t, id) - 1); StatusCue(t, $"{id} -1"); Say($"{t.Name}: {id} 1 지움"); gone++;
                            }
                        ctx.EventV = gone;   // 지운 수 — 뒤의 perEvent 가 센다
                        return true;
                    }
                case FxK.MoveRow:
                    if (owner != null && Array.IndexOf(R.ROWS, f.Id) >= 0) { owner.Row = f.Id; Say($"{owner.Name}: {(f.Id == "front" ? "전열" : f.Id == "mid" ? "중열" : "후열")}로"); StatusCue(owner, "자리 옮김", true); }
                    return true;
                case FxK.GrowRun:
                    {   // 판 단위 성장 — 지금 바로 오르고, 판이 기억한다(다음 싸움부터 장비처럼)
                        var who = Resolve(ctx, f.Target ?? "self").Where(u => u.Side == Side.Party).ToList();
                        foreach (var u in who)
                        {
                            if (!GrowthGain.TryGetValue(u.Key, out var g)) GrowthGain[u.Key] = g = new Stats();
                            int v = f.IV;
                            if (f.Id == "def") { g.Def += v; u.Def += v; } else if (f.Id == "crit") { g.Crit += v; u.Crit += v; } else { g.Atk += v; u.Atk += v; }
                            StatusCue(u, $"성장 {(f.Id == "def" ? "방어력" : f.Id == "crit" ? "치명" : "공격력")} +{v}", true);
                        }
                        return true;
                    }
            }
            return false;
        }

        /// <summary>예약 효과 돌리기 — 턴(Turn) 이 됐거나 카드 장수(Cards) 가 다 찼을 때.</summary>
        void RunLater(LaterRec r)
        {
            var owner = HeroUnit(r.Owner);
            if (r.Owner != null && (owner == null || owner.Dead)) return;
            var (prev, src0, seq0) = (Acting, ModSrc, ActSeq);
            Acting = r.Owner; ModSrc = r.Name; ActSeq = ++SeqN;
            Say($"예약 — {r.Name ?? "효과"}");
            int t = (Enemies.FirstOrDefault(x => x.Idx == r.Target && !x.Dead) ?? AliveEnemies().FirstOrDefault())?.Idx ?? 0;
            try { RunFx(r.Fx, new FxCtx { Owner = owner, TargetIdx = t, Passive = r.Name ?? "예약" }); }
            finally { Acting = prev; ModSrc = src0; ActSeq = seq0; }
            CheckOver();
        }

        void LaterTurn()
        {
            foreach (var r in Later.Where(x => x.Cards == 0 && x.Turn > 0 && x.Turn <= Turn).ToList()) { Later.Remove(r); RunLater(r); if (Over != null) return; }
        }

        void LaterCard()
        {
            foreach (var r in Later.Where(x => x.Cards > 0 && x.Born != PlaysTotal).ToList())
                if (--r.Cards <= 0) { Later.Remove(r); RunLater(r); if (Over != null) return; }
        }

        /// <summary>함정 — 그 적이 제 차례(즉시 행동 포함)에 치는 수를 하면 깔아 둔 효과가 돈다(대상 = 그 적). 치지 않는 수면 사라진다.</summary>
        void SpringTraps(Unit e, Intent it)
        {
            if (e.Traps.Count == 0) return;
            var traps = e.Traps.ToList(); e.Traps.Clear();
            if (it == null || !IsHit(it.T))
            {
                Say($"{e.Name}: 함정 — 공격이 아니었다");
                foreach (var r in traps.Where(x => x.Else != null && x.Else.Count > 0)) { RunLater(new LaterRec { Owner = r.Owner, Name = r.Name, Target = e.Idx, Fx = r.Else }); if (Over != null) return; }
                return;
            }
            foreach (var r in traps) { r.Target = e.Idx; RunLater(r); if (Over != null || e.Dead) return; }
        }

        /// <summary>소환물 — 한 대를 대신 받았으면 true. 키워드 cut 이 있으면 다 막지 않고 그만큼 깎는다(d 를 줄이고 false).</summary>
        bool MinionGuard(ref int d)
        {
            var kwCut = Kw.Values.FirstOrDefault(k => k.Def.Guard && k.Def.Cut > 0 && Party.Any(u => StackOf(u.Key, k.Id) > 0));
            if (kwCut != null)
            {
                var hk = Party.First(u => StackOf(u.Key, kwCut.Id) > 0);
                int n = StackOf(hk.Key, kwCut.Id);
                AddStack(hk.Key, kwCut.Id, -1); StackChanged(kwCut.Owner, kwCut.Id, n, n - 1, hk);
                d = Num.Round(d * (1 - kwCut.Def.Cut));
                Say($"「{kwCut.Id}」 — 한 대를 -{Num.Round(kwCut.Def.Cut * 100)}% 깎고 하나가 사라졌다");
                return false;
            }
            return MinionBlocks();
        }

        /// <summary>적 표식의 「건 사도가 칠 때만」(per from owner) — 표식이 있으면 겹 × v, 없으면 else.</summary>
        double OwnerMarkMod(Unit from, Unit to)
        {
            if (from == null || to == null || from.Side != Side.Party || to.Side != Side.Enemy || Kw.Count == 0) return 0;
            double v = 0;
            foreach (var kw in Kw.Values)
            {
                if (kw.Carrier != "enemy" || kw.Owner != from.Key) continue;
                foreach (var p in kw.Def.Per)
                    if (p.Stat == "taken" && p.From == "owner") { int n = St(to, kw.Id); v += n > 0 ? n * p.V : p.Else; }
            }
            return v;
        }

        /// <summary>소환물(키워드 guard) — 적의 공격 한 대를 대신 받고 1 사라진다. 받았으면 true.</summary>
        bool MinionBlocks()
        {
            foreach (var kw in Kw.Values)
            {
                if (!kw.Def.Guard || kw.Def.Cut > 0) continue;
                var holders = kw.Carrier == "hero" ? Party.Select(u => u.Key).ToList() : new List<string> { kw.Owner };
                foreach (var hk in holders)
                {
                    int n = StackOf(hk, kw.Id);
                    if (n <= 0) continue;
                    var hu = HeroUnit(hk); if (hu == null || hu.Dead) continue;
                    AddStack(hk, kw.Id, -1);
                    Say($"「{kw.Id}」 — 공격을 대신 받고 하나가 사라졌다"); StatusCue(hu, $"{kw.Id} -1");
                    StackChanged(kw.Owner, kw.Id, n, n - 1, hu);
                    return true;
                }
            }
            return false;
        }

        /// <summary>적 표식(weakens) — 걸린 적은 아군 카드가 약점 공격으로 친다.</summary>
        bool MarkedWeak(Unit to) => to != null && to.Side == Side.Enemy && Kw.Values.Any(k => k.Def.Weakens && k.Carrier == "enemy" && St(to, k.Id) > 0);

        void UseWeakMark(Unit t)
        {
            foreach (var kw in Kw.Values.Where(k => k.Def.Weakens && k.Carrier == "enemy").ToList())
            {
                int n = St(t, kw.Id); if (n <= 0) continue;
                SetStRaw(t, kw.Id, n - 1); StackChanged(kw.Owner, kw.Id, n, n - 1, t);
            }
        }
    }

    static class FxExt
    {
        public static int NOr1Of(this Fx f, int v) => f.N > 0 ? f.N : v > 0 ? v : 1;
    }
}
