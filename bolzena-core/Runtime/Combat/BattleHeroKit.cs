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
    /// 더미 조작(pull · exileFrom) · 적 버프 지우기(dispel) · 판 단위 성장(growRun).
    /// </summary>
    public sealed partial class Battle
    {
        const string PLAYED_PER = "\u0000낸", PILE_PER = "\u0000더미", CARDST_PER = "\u0000카드값", EVENT_PER = "\u0000일값";

        List<string> PileOf(string name) => name switch { "draw" => Draw, "discard" => Discard, "gone" => Gone, "hand" => Hand, _ => Discard };

        int KitCount(FxCtx ctx, string id)
        {
            if (id.StartsWith(PLAYED_PER, StringComparison.Ordinal)) { var tg = id.Substring(PLAYED_PER.Length); return tg.Length == 0 ? PlayTags.Count : PlayTags.Count(t => t.Contains(tg)); }
            if (id.StartsWith(PILE_PER, StringComparison.Ordinal)) return PileOf(id.Substring(PILE_PER.Length)).Count;
            if (id.StartsWith(CARDST_PER, StringComparison.Ordinal)) return CardStOf(ctx.CardId, id.Substring(CARDST_PER.Length));
            if (id.StartsWith(EVENT_PER, StringComparison.Ordinal)) return ctx.EventV / Math.Max(1, int.Parse(id.Substring(EVENT_PER.Length)));
            if (id.StartsWith(GONE_PER, StringComparison.Ordinal)) { var hk = id.Substring(GONE_PER.Length); return hk.Length == 0 ? GoneN : GoneOf(hk); }
            return -1;
        }

        // ── 시범 16(2026-10-08 2단계) ────────────────────────────────
        const string GONE_PER = "\u0000소멸";
        /// <summary>소환물이 한 카드(한 번의 일)에 따라 치는 대 수의 상한 — 다타 × 소환 × 추가 공격이 곱해지지 않게.</summary>
        public const int SUMMON_CAP = 5;

        /// <summary>이번 전투에 소멸한 그 사도의 카드 수(니콜 「내 카드」) — 소멸 계기마다 센다.</summary>
        public int GoneOf(string heroKey) => heroKey != null && Counts.TryGetValue("gone|" + heroKey, out var v) ? v : 0;
        /// <summary>이번 전투에 그 사도가 그 고유 효과를 쌓은 양(소모해도 줄지 않는다).</summary>
        public int GainedOf(string heroKey, string id) => heroKey != null && Counts.TryGetValue("gain|" + heroKey + "|" + id, out var v) ? v : 0;
        void GainCount(string heroKey, string id, int v) { if (heroKey == null || v <= 0) return; string k = "gain|" + heroKey + "|" + id; Counts[k] = (Counts.TryGetValue(k, out var x) ? x : 0) + v; }

        /// <summary>예약 당겨 쓰기의 효과 배율 — 그 동안만 규칙 효과에 곱한다(FireRule).</summary>
        double ripenScale = 1;

        /// <summary>
        /// 예약 당겨 쓰기(캬롯 일찍 캐기 · 마카샤 적혀 있던 결말) — 예약 키워드를 지금 다 닳게 한다. 그 키워드의 「다 닳으면」 규칙이 돌되
        /// 남은 칸 1당 효과가 v(기본 0.25)씩 준다(최소 25%). 그 뒤 예약이 터진 것으로(reserveFire).
        /// </summary>
        void RipenFx(Fx f, FxCtx ctx)
        {
            if (!Kw.TryGetValue(f.Id ?? "", out var kw)) return;
            double per = f.V > 0 ? f.V : 0.25;
            var holders = new List<Unit>();
            if (kw.Carrier == "enemy") holders.AddRange(Resolve(ctx, f.Target ?? "oneEnemy").Where(u => u.Side == Side.Enemy && St(u, kw.Id) > 0));
            else if (kw.Carrier == "hero") holders.AddRange(Resolve(ctx, f.Target ?? "allAllies").Where(u => u.Side == Side.Party && StackOf(u.Key, kw.Id) > 0));
            else if (kw.Carrier == "self") { var o = HeroUnit(kw.Owner); if (o != null && StackOf(o.Key, kw.Id) > 0) holders.Add(o); }
            else if (St(Pool, kw.Id) > 0) holders.Add(PartyRep());
            foreach (var u in holders)
            {
                if (Over != null) return;
                int left = kw.Carrier == "enemy" || kw.Carrier == "ally" ? St(u, kw.Id) : StackOf(u.Key, kw.Id);
                double sc = Math.Max(0.25, 1 - left * per);
                if (kw.Carrier == "enemy" || kw.Carrier == "ally") SetStRaw(u, kw.Id, 0); else AddStack(u.Key, kw.Id, -left);
                MeterSpend(kw.Id, left);
                Say($"「{kw.Id}」 — 당겨 쓴다(남은 {left}칸, 효과 {Num.Round(sc * 100)}%)"); StatusCue(u, $"{kw.Id} 당김", true);
                double s0 = ripenScale; ripenScale = s0 * sc;
                try { KwGone(kw.Id, kw.Owner, u, true); }
                finally { ripenScale = s0; }
                if (Over == null) Emit("reserveFire", new EmitInfo { Owner = kw.Owner, Id = kw.Id, Target = u.Side == Side.Enemy && !u.Dead ? u : null });
            }
        }

        /// <summary>
        /// 소환물 행동(쥬비 벌 · 모모 분신) — 그 소환물 겹 수만큼(max · 한 카드에 SUMMON_CAP 대까지) 추가 공격.
        /// 추가 공격 계기(extra)는 한 번만 깨우고, 「소환물이 행동하면」(summonAct, kind atk · 일의 값 = 친 대 수)을 낸다.
        /// </summary>
        void SummonFx(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null || !Kw.TryGetValue(f.Id ?? "", out var kw)) return;
            var who = HeroUnit(kw.Owner) ?? owner;
            int have = kw.Carrier == "self" ? StackOf(kw.Owner, kw.Id) : kw.Carrier == "hero" ? StackOf(owner.Key, kw.Id) : St(Pool, kw.Id);
            // 한 카드 몫 — 그 카드가 깨운 패시브(따라 치기)까지 한 묶음으로 센다(패시브는 ActSeq 가 새로 서므로 낸 카드 수로 묶는다)
            string ck = $"summon@{Turn}@{PlaysTotal}";
            int used = Counts.TryGetValue(ck, out var u0) ? u0 : 0;
            int hits = Math.Min(have, Math.Min(f.Max > 0 ? f.Max : SUMMON_CAP, SUMMON_CAP - used));
            if (hits <= 0) { if (have > 0) Say($"「{kw.Id}」 — 이 카드에는 더 따라 치지 않는다(한 카드에 {SUMMON_CAP}대)"); return; }
            Counts[ck] = used + hits;
            for (int i = 0; i < hits && Over == null; i++)
                foreach (var t in Resolve(ctx, f.Target ?? "randomEnemy"))
                {
                    if (t.Dead || Over != null) continue;
                    int v = HitAmount(who, f.Ratio + Morale(), bas: f.Base);
                    Hurt(t, v, new HurtOpts { From = who, Card = true });
                }
            Say($"「{kw.Id}」 {hits}대 — 따라 친다"); StatusCue(who, $"{kw.Id} ×{hits}", true);
            if (Over != null) return;
            AfterExtra(who);
            if (Over == null) Emit("summonAct", new EmitInfo { Owner = kw.Owner, Id = kw.Id, Kind = "atk", V = hits, N = hits, By = who.Key, Seq = ActSeq });
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
                        // type(시범 16 — 티그) — 그 종류의 제 카드만 잇달아 센다(제 스킬 · 남의 카드가 끼면 끊긴다)
                        for (int i = PlayLog.Count - 1; i >= 0 && owner != null && PlayLog[i].hero == owner.Key && (f.Type == null || PlayLog[i].type == f.Type); i--) k++;
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
                            "attack" => t.Intent != null && HitLike(t.Intent),
                            // 2026-10-06 — 마무리(즉사의 턴제판): HP 비율 이하 · 보스가 아닌 적만
                            "hp" => t.MaxHp > 0 && (double)t.Hp / t.MaxHp <= (f.Pct > 0 ? f.Pct : 0.3),
                            "hpMob" => !t.Boss && t.MaxHp > 0 && (double)t.Hp / t.MaxHp <= (f.Pct > 0 ? f.Pct : 0.3),
                            // 그 상태가 걸린 적(감전 → 충격 …)
                            var st when R.ALL_ST.Contains(st) => St(t, st) > 0,
                            _ => false,
                        });
                        if (f.Not) gate = !gate;
                        return true;
                    }
                case FxK.IfCardSt: gate = CardStOf(ctx.CardId, f.Id) >= Math.Max(1, f.N); if (f.Not) gate = !gate; return true;
                // ── 시범 16(2026-10-08 2단계) ──
                // 직전보다 비싼가(리코타 코스 순서) — 이번 턴 바로 앞 카드보다 이 카드의 비용이 크면(턴 첫 카드는 아니다)
                case FxK.IfPricier: gate = ctx.Card ? ctx.Pricier : Counts.TryGetValue("pricier", out var pv) && pv > 0; if (f.Not) gate = !gate; return true;
                // 이번 전투에 그 고유 효과를 n 이상 쌓았으면(레비(졸업) 노력 결산 — 소모해도 줄지 않는 누적)
                case FxK.IfGained: gate = owner != null && GainedOf(owner.Key, f.Id) >= Math.Max(1, f.N); if (f.Not) gate = !gate; return true;
                case FxK.PerGone: ctx.PerStack = GONE_PER + (f.Who == "self" && owner != null ? owner.Key : ""); return true;
                case FxK.Ripen: RipenFx(f, ctx); return true;
                case FxK.Summon: SummonFx(f, ctx); return true;
                // ── 비례 ──
                case FxK.PerPlayed: ctx.PerStack = PLAYED_PER + (f.Id ?? ""); return true;
                case FxK.PerPile: ctx.PerStack = PILE_PER + (f.From ?? "discard"); return true;
                case FxK.PerCardSt: ctx.PerStack = CARDST_PER + f.Id; ctx.PerMin = f.N; ctx.PerMax = f.Max; return true;   // n · max — 최소 · 최대(시범 16 쵸피 「배움」 최소 3)
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
                        var ok = Hand.Where(id => { var c = CardOf(id); return c != null && c.Hero != null && (owner == null || c.Hero != owner.Key) && HeroUnit(c.Hero) is Unit h && !h.Dead && (f.Id == null || c.Type == f.Id) && !c.Fx.Any(x => x.K == FxK.CastOther); }).ToList();   // 연쇄 상한(3단계) — 대신 발동 카드는 대신 발동하지 않는다(두 사도가 서로를 끝없이 부르던 것)
                        if (ok.Count == 0) return true;
                        var id = ok[Rng.Int(ok.Count)]; var cv = CardOf(id); var who = HeroUnit(cv.Hero);
                        Say($"「{cv.Name}」 — 대신 발동");
                        StatusCue(owner ?? who, $"대신 「{cv.Name}」", true);
                        RunFx(cv.Fx, new FxCtx { Owner = who, TargetIdx = ctx.TargetIdx, HitTags = CardHitTags(id, cv), Card = true, Type = cv.Type, CardId = id, Cost = cv.Cost, Scale = f.Ratio > 0 ? f.Ratio : 1 });
                        return true;
                    }
                case FxK.Pull:
                    {   // 더미에서 n 장 → 손(기본) · 뽑을 더미 맨 위(to top)
                        // 연쇄 상한(1단계) — 소멸 더미 되살리기는 사도마다 턴에 한 번(0코 태우기 · 뽑기와 도는 순환을 끊는다)
                        string gk = null;
                        if (f.From == "gone")
                        {
                            gk = $"gonePull|{owner?.Key ?? Acting}|{Turn}";
                            if (Counts.ContainsKey(gk)) { Say("소멸 더미 되살리기 — 이번 턴은 이미 했다(사도마다 턴에 한 번)"); return true; }
                        }
                        var src = PileOf(f.From ?? "discard");
                        if (gk != null && src.Count > 0) Counts[gk] = 1;
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
                        for (int k = 0; k < (f.All ? int.MaxValue : f.NOr1) && src.Count > 0; k++)   // all — 맞는 카드 전부(2026-10-07)
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
            // 예약형 공용 계기 — 예약(later · afterCards · 덫)이 터지면. 고른 적은 그 예약의 표적.
            if (Over == null) Emit("reserveFire", new EmitInfo { Owner = r.Owner, Id = r.Name, Target = Enemies.FirstOrDefault(x => x.Idx == t && !x.Dead) });
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
                d = Num.Round(d * (1 - kwCut.Def.Cut));
                bool lost = MinionUse(kwCut, hk);
                Say($"「{kwCut.Id}」 — 한 대를 -{Num.Round(kwCut.Def.Cut * 100)}% 깎았다{(lost ? " · 하나가 사라졌다" : "")}");
                return false;
            }
            return MinionBlocks();
        }

        /// <summary>
        /// 소환물 하나가 한 대를 받았다 — uses(기본 1) 대를 받으면 하나가 사라진다(시범 16 모모 분신). 「소환물이 행동하면」 kind guard, 사라지면 kind lost.
        /// 사라졌으면 true.
        /// </summary>
        bool MinionUse(KwRt kw, Unit hu)
        {
            string uk = "uses|" + hu.Key + "|" + kw.Id;
            int took = (Counts.TryGetValue(uk, out var u0) ? u0 : 0) + 1;
            bool lost = took >= Math.Max(1, kw.Def.Uses);
            int n = StackOf(hu.Key, kw.Id);
            if (lost) { Counts.Remove(uk); AddStack(hu.Key, kw.Id, -1); }
            else Counts[uk] = took;
            Emit("summonAct", new EmitInfo { Owner = kw.Owner, Id = kw.Id, Kind = "guard", V = 1, N = 1, By = hu.Key });
            if (lost && Over == null)
            {
                Emit("summonAct", new EmitInfo { Owner = kw.Owner, Id = kw.Id, Kind = "lost", V = 1, N = 1, By = hu.Key });
                StackChanged(kw.Owner, kw.Id, n, n - 1, hu);
            }
            return lost;
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
                    bool lost = MinionUse(kw, hu);
                    Say($"「{kw.Id}」 — 공격을 대신 받았다{(lost ? " · 하나가 사라졌다" : "")}"); if (lost) StatusCue(hu, $"{kw.Id} -1");
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
