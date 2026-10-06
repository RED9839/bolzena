using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    public sealed partial class Battle
    {
        // ── 상태 ───────────────────────────────────────────────────────
        // 두 층: 사도 층(사기 — 사도마다) · 파티 층(그 밖 전부 — 파티 몸 하나). 적은 제 몸.
        Dictionary<string, int> BagOf(Unit u, string id) =>
            u.Side == Side.Party && u.BodyRef != null && !R.HERO_ST.Contains(id) ? u.BodyRef.Status : u.Status;

        /// <summary>u 에게 걸린 상태 id 의 겹.</summary>
        public int St(Unit u, string id) => u != null && BagOf(u, id).TryGetValue(id, out var v) ? v : 0;

        void DelSt(Unit u, string id)
        {
            SrcBag(u, id).Remove(id);
            BagOf(u, id).Remove(id);
            u.Body.DotU.Remove(id);
        }

        void SetStRaw(Unit u, string id, int v)
        {
            if (v > 0) BagOf(u, id)[id] = v; else BagOf(u, id).Remove(id);
        }

        /// <summary>겹을 더한다(빼면 음수). 면역이 해로운 것을 막으면 false. 세기 상태는 상한까지(적은 FOE_INT_MAX).</summary>
        bool AddSt(Unit u, string id, int v, string src = null)
        {
            if (v > 0 && src != null && !(id != "면역" && R.IsBadSt(id) && St(u, "면역") > 0)) AddSrc(u, id, src, v);
            if (v > 0 && id != "면역" && R.IsBadSt(id) && St(u, "면역") > 0)
            {
                SetStRaw(u, "면역", St(u, "면역") - 1);
                u.Body.ImmuneHit++;
                return false;
            }
            int n = Math.Max(0, St(u, id) + v);
            double? cap = R.SMax(id);
            if (u.Side == Side.Enemy && R.IsIntensity(id)) cap = Math.Min(cap ?? double.PositiveInfinity, R.FOE_INT_MAX);
            if (cap != null && v > 0) n = (int)Math.Min(Math.Max(cap.Value, St(u, id)), n);
            if (n > 0) BagOf(u, id)[id] = n; else DelSt(u, id);
            return true;
        }

        int DotUnit(Unit u, string id) => u.Body.DotU.TryGetValue(id, out var v) ? v : R.FOE_DOT;
        void SetUnit(Unit u, string id, double unit)
        {
            if (!R.IsUnitSt(id) || !(unit > 0)) return;
            var d = u.Body.DotU;
            d[id] = Math.Max(d.TryGetValue(id, out var x) ? x : 0, Num.Round(unit));
        }

        /// <summary>횟수 상태 — 한 번의 일(ActSeq)에 한 번만 1 줄인다. 돌았으면 true. 세기 상태는 줄지 않는다.</summary>
        bool Charge(Unit u, string id)
        {
            if (u == null) return false;
            if (R.IsIntensity(id)) return St(u, id) > 0;
            var use = R.IsHeroSt(id) && u.Side == Side.Party ? u.StUse : u.Body.StUse;
            if (ActSeq != 0 && use.TryGetValue(id, out var last) && last == ActSeq) return true;
            if (St(u, id) <= 0) return false;
            AddSt(u, id, -1);
            use[id] = ActSeq;
            return true;
        }

        /// <summary>AP 를 얻는다 — 적의 차례 · 턴을 넘기는 중이면 다음 턴으로.</summary>
        void GainAp(int n)
        {
            if (n == 0) return;
            if (FoeTurn || Ending) ApCarry += n; else Ap += n;
        }

        // ── 턴 ─────────────────────────────────────────────────────────
        void BeginTurn()
        {
            Turn++;
            MeterTurn();
            Cue("turn", PartyRep(), new Cue { V = Turn });
            int gain = Math.Max(0, R.AP_PER_TURN - ApJam) + (Turn == 1 ? StartAp : 0) + ApCarry;
            ApCarry = 0;
            if (Turn > 1 && ApLeft > 0 && St(Pool, "저장") > 0)
            {
                gain += ApLeft; AddSt(Pool, "저장", -1);
                Say($"저장 — 남긴 AP {ApLeft} 을 가져온다"); StatusCue(PartyRep(), $"저장 AP +{ApLeft}", true);
            }
            ApLeft = 0;
            if (Turn > 1) TurnStatusTick();
            if (ApJam > 0) Say($"방해로 AP -{ApJam}");
            ApJam = 0;
            Ap = Math.Max(0, gain);
            NextCheaper = 0; Ending = false; PrevNat = null;
            LastHero = null; PlayTags.Clear(); DebtNow = 0;
            PlayedPrev = new Dictionary<string, int>(PlayedBy);
            PlayLog.Clear(); PlayIds.Clear(); PlayedThisTurn = 0; PlayedBy.Clear(); RushedThisTurn = false;
            ApSpent = 0; PaidHp = 0; DiscardedTurn = 0; TakenPrev = TakenNow; TakenNow = 0;
            HurtPrev = HurtNow; HurtNow = new HashSet<string>();
            KillPrev = KillNow; KillNow = new Dictionary<string, int>();
            // 실드(· 옛 방어)는 턴이 바뀌면 사라진다(카제나 — 2026-10-05, 옛 「실드는 남는다」 를 바꿨다).
            // 실드 보존 — 다 남기고 1 쓴다 · 실드 유지 — 반을 남기고 1 쓴다
            if (Pool.Dead || Pool.Block + Pool.Shield <= 0) { }
            else if (St(Pool, "실드 보존") > 0) { AddSt(Pool, "실드 보존", -1); Say($"실드 보존 — 실드 {Pool.Block + Pool.Shield} 이 그대로 남는다"); }
            else if (St(Pool, "실드 유지") > 0)
            {
                Pool.Block = (int)Math.Floor(Pool.Block * R.SV("실드 유지")); Pool.Shield = (int)Math.Floor(Pool.Shield * R.SV("실드 유지"));
                AddSt(Pool, "실드 유지", -1);
                Say($"실드 유지 — 실드 {Pool.Block + Pool.Shield} 이 남는다");
            }
            else { Pool.Block = 0; Pool.Shield = 0; }
            // 「이번 턴」 증감 · 키워드 겹은 적의 차례까지 간다 — 다음 내 턴 시작에 줄인다
            if (Turn > 1)
            {
                TickMods();
                foreach (var g in DecayKeywords()) KwGone(g.kw.Id, g.kw.Owner, g.holder, true);
                FormTick();   // 변신 — 턴으로 재는 것은 1 줄이고 다하면 풀린다(뽑기 전에 — 새 손은 본래 모습)
            }
            if (Turn > 1) ReviveTick();
            // 격파된 적은 내 턴이 다시 오면 일어선다
            var risen = AliveEnemies().Where(e => e.Broken).ToList();
            foreach (var e in risen)
            {
                e.Broken = false; e.Tough = e.ToughMax;
                Say($"{e.Name}: 격파에서 일어선다 — 강인도 회복"); Cue("tough", e, new Cue { From = 0, To = e.Tough, Up = true });
            }
            foreach (var e in AliveEnemies()) { RollIntent(e, false); e.RushCnt = 0; e.RushedTurn = false; e.ActedTurn = false; }
            if (Turn == 1 && FirstRushDown != 0) foreach (var e in AliveEnemies()) e.RushCnt -= FirstRushDown;
            ResetFoePassives();
            foreach (var e in risen) FoePassives("recover", e);
            FoePassives("turnStart", null);
            foreach (var e in AliveEnemies()) CounterEvent(e, "turnStart");

            DrawCards(R.DRAW_PER_TURN + (Turn == 1 ? Opening : 0), false);
            AfterTurnDraw();
            if (Over == null) LaterTurn();
            if (Over == null) FoePassives("afterDraw", null);
            // 주도 — 턴 시작에 손에 든 주도 카드는 반반으로 이번 턴 코스트 -1
            FinaleLock = false;
            LeadOn.Clear();
            foreach (var id in Hand.ToList()) if (HasTagB(id, Tag.Lead) && Rng.Next() < 0.5) LeadOn.Add(id);
            Emit("turnStart", new EmitInfo());
            CheckOver();
            // 연쇄 — 지난 턴에 낸 연쇄 카드의 효과가 한 번 더(코스트 · 즉시 행동 셈 · 게이지 없이)
            var echo = Echo; Echo = new List<(string, int)>();
            foreach (var (id, target) in echo)
            {
                if (Over != null) break;
                var c = CardOf(id); if (c == null) continue;
                var owner = c.Hero != null ? Party.FirstOrDefault(u => u.Key == c.Hero && !u.Dead) : null;
                if (c.Hero != null && owner == null) continue;
                Say($"연쇄 「{c.Name}」 — 한 번 더");
                Cue("auto", owner ?? AliveParty().FirstOrDefault(), new Cue { Tag = Tag.Echo, Label = "연쇄!", Name = c.Name, CardId = id, Hero = c.Hero });
                var (prev, src0, seq0) = (Acting, ModSrc, ActSeq);
                Acting = c.Hero; ModSrc = $"「{c.Name}」 연쇄"; ActSeq = ++SeqN;
                int tgt = (Enemies.FirstOrDefault(x => x.Idx == target && !x.Dead) ?? AliveEnemies().FirstOrDefault())?.Idx ?? 0;
                try { RunFx(c.Fx, new FxCtx { Owner = owner, TargetIdx = tgt, HitTags = CardHitTags(id, c), Card = true, Type = c.Type }); }
                finally { Acting = prev; ModSrc = src0; ActSeq = seq0; }
                CheckOver();
            }
        }

        /// <summary>턴을 넘긴다 — 턴 끝 효과 · 적의 차례 · 다음 턴 시작까지.</summary>
        public Battle EndTurn()
        {
            FreeTurn.Clear();
            if (Over != null) return this;
            ApLeft = Ap;
            Ending = true;
            Emit("turnEnd", new EmitInfo());
            TickTurnEnd();
            KwEndDecay();
            CheckOver(); if (Over != null) return this;
            StatusTurnEnd();
            CheckOver(); if (Over != null) return this;
            // 「턴 끝에 손에 있으면: …」
            foreach (var id in Hand.ToList())
                if (CardOf(id) is CardView cv && cv.Fx.Any(f => f.K == FxK.When && f.On == "handEnd")) { CardWhen(id, "handEnd"); if (Over != null) return this; }
            // 증발 — 턴 끝에 손에 있으면 이 전투에서 사라진다(보존보다 앞선다) · 보존은 손에 남는다
            var gone = Hand.Where(id => HasTagB(id, Tag.Evaporate)).ToList();
            if (gone.Count > 0) Say($"증발 — {string.Join(" ", gone.Select(id => $"「{CardOf(id).Name}」"))} 사라진다");
            var keep = Hand.Where(id => !gone.Contains(id) && HasTagB(id, Tag.Keep)).ToList();
            var toss = Hand.Where(id => !keep.Contains(id) && !gone.Contains(id)).ToList();
            Hand = keep;
            foreach (var id in gone) { Exile(id, "hand", "evaporate"); Emit("exhaust", new EmitInfo { Hero = CardOf(id)?.Hero, Id = id }); if (Over != null) return this; }
            foreach (var id in toss) CardCue(id, "hand", "discard", "turnEnd");
            Discard.AddRange(toss);
            foreach (var id in toss) BlessDrop(id);
            MergePile(Discard, "discard");
            foreach (var id in Held.Keys.ToList()) if (!keep.Contains(id)) Held.Remove(id);
            foreach (var id in keep.Distinct()) Held[id] = (Held.TryGetValue(id, out var hn) ? hn : 0) + 1;
            CheckOver(); if (Over != null) return this;
            // 리듬 — 턴이 끝나면 다 사라진다
            if (St(Pool, R.RHYTHM) > 0) { Say($"리듬 {St(Pool, R.RHYTHM)} — 턴이 끝나 사라진다"); Pool.Status.Remove(R.RHYTHM); }

            FoeTurn = true;
            Cue("foeTurn", PartyRep(), new Cue { V = Turn });
            FoePassives("turnEnd", null);
            foreach (var e in AliveEnemies()) CounterEvent(e, "turnEnd");
            CheckOver(); if (Over != null) { FoeTurn = false; return this; }
            EnemyPhase();
            FoeTurn = false;
            CheckOver(); if (Over != null) return this;
            foreach (var e in Enemies) { if (e.StunGuard > 0) e.StunGuard--; e.Confused = false; }
            BeginTurn();
            return this;
        }

        /// <summary>
        /// 턴 끝의 상태 — 내 턴이 끝날 때 아군 · 적 모두. 결정화(고정 실드) · 고동(적 전체 고정 피해) · 고통(고정 지속 피해 → 절반) ·
        /// 균열(지속 피해 → 절반) · 그을림은 사라진다.
        /// </summary>
        void StatusTurnEnd()
        {
            var list = new List<Unit>();
            if (!Pool.Dead) list.Add(PartyRep());
            list.AddRange(AliveEnemies());
            foreach (var u in list)
            {
                if (u.Dead) continue;
                ActSeq = ++SeqN;
                string who = u.Side == Side.Party ? "파티" : u.Name;
                int DefOf() => u.Side == Side.Party ? PartyDef() : Math.Max(0, Num.Round(u.Def * (1 + StatMod(u, "def"))));
                if (St(u, "결정화") > 0)
                {
                    int v = Math.Max(1, Num.Round(DefOf() * R.StackEff("결정화", St(u, "결정화"))));
                    u.Shield += v; GainCue(u, "shield", v);
                    Say($"{who}: 결정화 {St(u, "결정화")} — 고정 실드 +{v}");
                }
                if (u.Side == Side.Party && St(u, "고동") > 0)
                {
                    int v = Math.Max(1, Num.Round(DotUnit(u, "고동") * R.StackEff("고동", St(u, "고동"))));
                    Say($"파티: 고동 {St(u, "고동")} — 적 전체 고정 피해 {v}");
                    foreach (var e in AliveEnemies()) { Hurt(e, v, new HurtOpts { Pure = true, Dot = true }); if (Over != null) return; }
                }
                if (St(u, "고통") > 0)
                {
                    int n = St(u, "고통"), v = Math.Max(1, Num.Round(n * R.SV("고통") * DotUnit(u, "고통")));
                    Say($"{who}: 고통 {n} — 고정 피해 {v}");
                    AddSt(u, "고통", -(n - n / 2));
                    Hurt(u, v, new HurtOpts { Pure = true, Dot = true });
                    if (Over != null) return;
                    if (u.Dead) continue;
                }
                if (St(u, "균열") > 0)
                {
                    int n = St(u, "균열"), v = Math.Max(1, Num.Round(n * R.SV("균열") * DotUnit(u, "균열")));
                    Say($"{who}: 균열 {n} — 지속 피해 {v}");
                    AddSt(u, "균열", -(n - n / 2));
                    Hurt(u, v, new HurtOpts { Dot = true });
                    if (Over != null) return;
                    if (u.Dead) continue;
                }
                if (St(u, "그을림") > 0) DelSt(u, "그을림");
            }
        }

        /// <summary>얻는 방어 · 실드 — 결의(겹마다 +20) 를 더한 뒤 손상(-50%, 한 번의 일에 1).</summary>
        int ShieldGain(Unit u, int v)
        {
            if (v > 0 && St(u, "결의") > 0) v += Num.Round(R.StackEff("결의", St(u, "결의")));
            if (v > 0 && Charge(u, "손상")) return Math.Max(0, Num.Round(v * (1 - R.SV("손상"))));
            return v;
        }

        void CheckOver()
        {
            if (!Enemies.Any(e => !e.Dead))
            {
                if (Over != "win") { var w = AliveParty().FirstOrDefault(); if (w != null) Talk(w, "win"); Cue("over", PartyRep(), new Cue { Id = "win" }); }
                Over = "win"; MeterFlush();
            }
            else if (Pool.Dead) { if (Over != "lose") Cue("over", Party.FirstOrDefault(), new Cue { Id = "lose" }); Over = "lose"; MeterFlush(); }
        }

        void Kill(Unit u)
        {
            if (u.Dead) return;
            int overkill = Math.Max(0, -u.Hp);
            u.Hp = 0; u.Dead = true;
            if (u.Side == Side.Party) { Talk(u, "down"); Say("파티 HP 0 — 더 버티지 못한다"); CheckOver(); return; }
            KillSeq = ActSeq;
            Cue("die", u);
            FoeDeath(u);
            // 근면 N — 적을 처치하면 AP 1 · 드로우 1. 카드(행동) 하나에 한 번, 한 턴에 N 번까지 — 근면 2 의 둘째는 다른 카드로 처치해야 돈다(2026-10 사용자)
            int dil = St(Pool, "근면");
            Counts.TryGetValue($"근면|{Turn}", out var dilN);
            if (Acting != null && dil > 0 && dilN < dil && !Counts.ContainsKey($"근면@{ActSeq}") && AliveEnemies().Count > 0)
            { Counts[$"근면|{Turn}"] = dilN + 1; Counts[$"근면@{ActSeq}"] = 1; GainAp(1); Say($"근면 — AP +1 · 드로우 1 ({dilN + 1}/{dil})"); StatusCue(PartyRep(HeroUnit(Acting)), "근면!", true); DrawCards(1); }
            foreach (var kw in Kw.Values.Where(k => k.Def.Hunt).ToList())
                if (St(u, kw.Id) > 0) Emit("huntDown", new EmitInfo { Id = kw.Id, Owner = kw.Owner, Target = u, N = St(u, kw.Id), By = Acting });
            Say($"{u.Name} 쓰러짐");
            if (R.KILL_AP > 0 && AliveEnemies().Count > 0) { GainAp(R.KILL_AP); Say($"처치 — AP +{R.KILL_AP}"); }
            if (Acting != null) KillNow[Acting] = (KillNow.TryGetValue(Acting, out var k) ? k : 0) + 1;
            if (Acting != null) Talk(HeroUnit(Acting), "kill");
            Emit("kill", new EmitInfo { By = Acting, Target = u, V = overkill });
            FoePassives("allyDown", u);
            CheckOver();
        }

        // ── 파티 능력치 ────────────────────────────────────────────────
        /// <summary>파티의 방어력 — 방어력이 가장 높은 사도의 것(증감 포함). 결정화가 쓴다.</summary>
        public int PartyDef()
        {
            int best = 0;
            foreach (var u in Party) best = Math.Max(best, Num.Round(u.Def * (1 + StatMod(u, "def"))));
            return best;
        }
        public int AtkNow(Unit u) => Math.Max(1, Num.Round(u.Atk * (1 + StatMod(u, "atk"))));
        public int DefNow(Unit u) => Math.Max(0, Num.Round(u.Def * (1 + StatMod(u, "def"))));
        /// <summary>파티의 막는 손 — 방어력이 가장 높은 사도(반격의 바탕).</summary>
        public Unit PartyGuard()
        {
            Unit best = null;
            foreach (var u in AliveParty()) if (best == null || DefNow(u) > DefNow(best)) best = u;
            return best;
        }
        /// <summary>협공 · 교주 카드 상태의 바탕 — but 을 뺀 사도 가운데 가장 높은 공격력.</summary>
        int PartyAtk(Unit but)
        {
            int best = 0;
            foreach (var u in AliveParty()) if (u != but) best = Math.Max(best, AtkNow(u));
            return best > 0 ? best : but != null ? AtkNow(but) : 1;
        }
    }
}
