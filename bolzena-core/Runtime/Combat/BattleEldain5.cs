using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 엘다인 4차(2026-10-11, Docs/데이터.md §25 · 02 E76~E87) — 이번 변신 중 준 피해 · n번째 이후 카드(정각) · 고학년 요구 게이지 증감 ·
    /// 처치하면 한 대 더 · 이번 턴 남은 카드 신속 · 표시된 아군에게만 증감 · 치명 피해 상태 · 이번 턴 맞은 대 수.
    /// 데이터 담당 요청 — 이 카드 치명 확정(critNext now) · 적이 하나뿐이면(ifFoe alone) · 다른 아군의 다음 비싼 카드 강화(empower who other minCost) · 거르개 minCost.
    /// </summary>
    public sealed partial class Battle
    {
        /// <summary>X 다타 처치 한 대 더(▶E79)의 기본 상한.</summary>
        public const int KILL_MORE_MAX = 12;
        /// <summary>고학년 요구 게이지 하한(▶E78) — 데이터 값의 50%.</summary>
        public const double ULT_COST_FLOOR = 0.5;
        const string HIT_TAKEN = "n|hitTaken", SWIFT_TURN = "swiftTurn", CRIT_DMG = "critDmg", CRIT_DMG_END = "critDmgEnd", CRIT_DMG_OFF = "\u0000치명피해끝";

        bool FxEldain5(Fx f, FxCtx ctx, ref bool gate)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                case FxK.NthNext:   // 이번 턴 자신의 다음 카드 v장은 「n번째 이후」 조건이 선다(▶E77 — 리뉴아 시계 ③)
                    if (owner != null) { string k = "nthNext|" + Turn + "|" + owner.Key; Counts[k] = (Counts.TryGetValue(k, out var n0) ? n0 : 0) + Math.Max(1, f.IV); Say($"{owner.Name}의 다음 카드 — 정각으로 셈"); }
                    return true;
                case FxK.UltCostMod:   // 이 전투 동안 그 사도 고학년 요구 게이지 ×(1+v), 하한 50%(▶E78)
                    if (owner != null && f.V != 0)
                    {
                        string k = "ultCost|" + owner.Key;
                        Counts[k] = (Counts.TryGetValue(k, out var u0) ? u0 : 0) + Num.Round(f.V * 100);
                        Say($"{owner.Name} 고학년 게이지 {(f.V > 0 ? "+" : "")}{Num.Round(f.V * 100)}% — 지금 {UltCostOf(owner.Key)}%");
                        StatusCue(owner, $"고학년 게이지 {(f.V > 0 ? "+" : "")}{Num.Round(f.V * 100)}%", f.V < 0);
                    }
                    return true;
                case FxK.SwiftTurn:   // 이번 턴 남은 카드 모두 신속(▶E80)
                    Counts[SWIFT_TURN] = Turn; Say("이번 턴 남은 카드 — 신속"); StatusCue(PartyRep(owner), "신속!", true);
                    return true;
                case FxK.CritDmg: CritDmgFx(f, ctx); return true;
                case FxK.MoveMark: MoveMarkFx(f, ctx); return true;
                case FxK.Learn: LearnFx(f, ctx); return true;
                case FxK.IfHitTaken: gate = HitsTaken >= Math.Max(1, f.N); if (f.Not) gate = !gate; return true;   // 이번 턴 맞은 대 수(▶E85)
                case FxK.IfCost: gate = ctx.Card && ctx.CostNow <= f.Max; if (f.Not) gate = !gate; return true;   // 이 카드를 비용 max 이하로 냈으면(리뉴아 정각)
            }
            return false;
        }

        // ── 이번 변신 중 준 피해(▶E76) ─────────────────────────────────
        /// <summary>그 사도가 지금 든 변신 동안 준 피해(변신 중이 아니면 0).</summary>
        public int FormDealt(string heroKey) => heroKey != null && Forms.TryGetValue(heroKey, out var fr) ? fr.Dealt : 0;
        void FormDealtAdd(Unit from, int v)
        {
            if (v <= 0 || from == null || Forms.Count == 0 || !Forms.TryGetValue(from.Key, out var fr)) return;
            fr.Dealt += v;
        }

        // ── n번째 이후(▶E77) ───────────────────────────────────────────
        /// <summary>지금 손에서 내면 이번 턴 몇 번째 카드인가 — 화면이 손 카드에 「정각」 을 보일 때.</summary>
        public int NthNow => PlayedThisTurn + 1;
        /// <summary>이 사도의 카드가 「다음 카드는 정각」(nthNext)을 쓰나 — 쓰면 하나 줄인다.</summary>
        bool UseNthNext(string hero)
        {
            if (hero == null || Counts.Count == 0) return false;
            string k = "nthNext|" + Turn + "|" + hero;
            if (!Counts.TryGetValue(k, out var n) || n <= 0) return false;
            if (n > 1) Counts[k] = n - 1; else Counts.Remove(k);
            return true;
        }
        /// <summary>그 사도의 다음 카드가 「정각으로 셈」 인가(봇 · 화면).</summary>
        public bool NthNextOn(string hero) => hero != null && Counts.TryGetValue("nthNext|" + Turn + "|" + hero, out var n) && n > 0;

        // ── 고학년 요구 게이지(▶E78) ──────────────────────────────────
        /// <summary>그 사도 고학년의 지금 요구 게이지 — 데이터 값 ×(1 + 키워드 1개당 ultCost + ultCostMod), 데이터 값의 50% 아래로는 안 내려간다.</summary>
        public int UltCostOf(string heroKey)
        {
            var ult = UltOf(heroKey);
            if (ult == null) return 0;
            double m = UltCostMod(heroKey);
            if (m == 0) return ult.Cost;
            return Math.Max((int)Math.Ceiling(ult.Cost * ULT_COST_FLOOR), Math.Min(R.GAUGE_MAX, Num.Round(ult.Cost * (1 + m))));
        }
        double UltCostMod(string heroKey)
        {
            double m = Counts.Count > 0 && Counts.TryGetValue("ultCost|" + heroKey, out var c) ? c / 100.0 : 0;
            foreach (var kw in Kw.Values)
                foreach (var p in kw.Def.Per)
                {
                    if (p.Stat != "ultCost") continue;
                    int n = kw.Carrier == "self" ? (kw.Owner == heroKey ? StackOf(kw.Owner, kw.Id) : 0)
                        : kw.Carrier == "hero" ? StackOf(heroKey, kw.Id)
                        : kw.Carrier == "ally" ? St(Pool, kw.Id) : 0;
                    m += n * p.V;
                }
            return m;
        }

        // ── 이번 턴 신속(▶E80) ────────────────────────────────────────
        /// <summary>이번 턴 남은 카드가 모두 신속인가(swiftTurn).</summary>
        public bool SwiftTurnOn => Counts.Count > 0 && Counts.TryGetValue(SWIFT_TURN, out var t) && t == Turn;

        // ── 치명 피해 상태(▶E83) ──────────────────────────────────────
        void CritDmgFx(Fx f, FxCtx ctx)
        {
            int turns = f.TurnsOr1;
            Counts[CRIT_DMG] = Num.Round(f.V * 100);
            Counts[CRIT_DMG_END] = Turn + turns;
            // 끝날 때 효과 — 그 턴 시작에(다시 쓰면 지속을 처음으로 — 앞의 끝 효과는 지운다)
            Later.RemoveAll(r => r.Name == CRIT_DMG_OFF);
            if (f.Off != null && f.Off.Count > 0) Later.Add(new LaterRec { Turn = Turn + turns, Owner = ctx.Owner?.Key, Name = CRIT_DMG_OFF, Target = ctx.TargetIdx, Fx = f.Off });
            string nm = f.Id != null ? $"「{f.Id}」 " : "";
            Say($"{nm}{turns}턴 — 아군 치명 피해 +{Num.Round(f.V * 100)}%"); StatusCue(PartyRep(ctx.Owner), $"{f.Id ?? "치명 피해"} +{Num.Round(f.V * 100)}%", true);
        }
        /// <summary>지금 걸린 파티 치명 피해 증가(0.4 = +40%) · 남은 턴(0 이면 없음) — 화면.</summary>
        public (double v, int left) CritDmgState => Counts.Count > 0 && Counts.TryGetValue(CRIT_DMG_END, out var e) && e > Turn && Counts.TryGetValue(CRIT_DMG, out var p) ? (p / 100.0, e - Turn) : (0, 0);
        /// <summary>치명 배율 덤(R.FinalDamage critX) — 파티는 판 덤(CritDmgX)에 치명 피해 상태를 곱한다.</summary>
        double CritXOf(Unit u)
        {
            if (u == null || u.Side != Side.Party) return 0;
            double b = CritDmgState.v;
            double cx = CritDmgX + (ProgOf != null && ProgOf.TryGetValue(u.Key, out var pm) ? pm.StatOf("critDmg") : 0);   // 영구 성장 치명 피해(사도마다)
            return b == 0 ? cx : (R.CRIT_MULT + cx) * (1 + b) - R.CRIT_MULT;
        }

        // ── 이번 턴 맞은 대 수(▶E85) ──────────────────────────────────
        /// <summary>바로 앞 적 차례 + 이번 내 턴에 파티가 적에게 맞은 대 수(다 막은 대 포함 · 지속 피해 빼고). 적 차례가 시작될 때 0.</summary>
        public int HitsTaken => Counts.TryGetValue(HIT_TAKEN, out var v) ? v : 0;
        void HitTakenAdd() => Counts[HIT_TAKEN] = HitsTaken + 1;
        void HitTakenReset() => Counts.Remove(HIT_TAKEN);

        // ── 벨라 「존속의 촛불」(엘다인 4차) ───────────────────────────────
        /// <summary>그 동료가 자기 카드를 냈다 — burn 표시가 1 타며 키워드 주인 방어력 기준 실드 · 회복.
        /// carrier hero = 그 동료에게 붙은 표시 · carrier self = 주인이 쥔 파티 촛불(2026-10-12 벨라 — 주인 말고 누가 내든 탐).</summary>
        void MarkBurn(string hero)
        {
            if (hero == null || Kw.Count == 0) return;
            foreach (var kw in Kw.Values)
            {
                var bn = kw.Def.Burn;
                if (bn == null || bn.On != "play") continue;
                string at = kw.Carrier == "hero" ? hero : kw.Carrier == "self" && hero != kw.Owner ? kw.Owner : null;
                if (at == null || StackOf(at, kw.Id) <= 0) continue;
                var who = HeroUnit(hero); var giver = HeroUnit(kw.Owner);
                if (who == null || giver == null || giver.Dead || Pool.Dead) continue;
                int before = StackOf(at, kw.Id);
                AddStack(at, kw.Id, -1);
                StackChanged(kw.Owner, kw.Id, before, StackOf(at, kw.Id), at == hero ? who : giver);
                int v = bn.Shield > 0 ? GuardAmount(giver, bn.Shield, null) : 0;
                if (v > 0) { Pool.Shield += v; GainCue(PartyRep(who), "shield", v); TallyShield(giver, v); }
                Say($"「{kw.Id}」 — {who.Name}의 카드에 1 탐{(v > 0 ? $"(실드 {v})" : "")} · 남은 {StackOf(at, kw.Id)}");
                if (bn.Heal > 0)   // (2026-10-11) 촛불이 타며 파티 회복 — 주인(벨라) 방어력 기준, 보통 회복과 같은 길(넘친 회복 · 부상 계기 포함)
                    RunFx(new List<Fx> { new Fx { K = FxK.Heal, Ratio = bn.Heal, Target = "party" } }, new FxCtx { Owner = giver, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0 });
                if (Over != null) return;
            }
        }
        /// <summary>moveMark — 다른 동료에게 붙은 그 표시를 지금 대상(아군 1명)에게 모두 옮긴다(최대 cap).</summary>
        void MoveMarkFx(Fx f, FxCtx ctx)
        {
            if (f.Id == null || !Kw.TryGetValue(f.Id, out var kw) || kw.Carrier != "hero") return;
            var to = Resolve(ctx, f.Target ?? "oneAlly").FirstOrDefault(u => u.Side == Side.Party);
            if (to == null) return;
            int sum = 0;
            foreach (var o in Party)
                if (o != to && StackOf(o.Key, f.Id) > 0) { int n = StackOf(o.Key, f.Id); sum += n; AddStack(o.Key, f.Id, -n); StackChanged(kw.Owner, f.Id, n, 0, o); }
            if (sum <= 0) return;
            int before = StackOf(to.Key, f.Id);
            AddStack(to.Key, f.Id, sum);
            StackChanged(kw.Owner, f.Id, before, StackOf(to.Key, f.Id), to);
            Say($"「{f.Id}」 {sum} — {to.Name}에게로 모아 옮김");
        }

        // ── 낸 뒤 이번 적 차례가 끝날 때 실드가 남았으면(when shieldLeft — 벨라) ──
        const string SHIELD_LEFT = "\u0000실드남음";
        /// <summary>낸 카드의 「적 차례가 끝날 때 실드가 남았으면」 마디를 걸어 둔다.</summary>
        void ShieldLeftArm(CardView c, Unit owner, int target)
        {
            if (c?.Fx == null || !c.Fx.Any(x => x.K == FxK.When && x.On == "shieldLeft")) return;
            var part = new List<Fx>(); bool on = false;
            foreach (var x in c.Fx) { if (x.K == FxK.When) { on = x.On == "shieldLeft"; continue; } if (on) part.Add(x); }
            if (part.Count > 0) Later.Add(new LaterRec { Name = SHIELD_LEFT, Owner = owner?.Key, Target = target, Fx = part });
        }
        /// <summary>적 차례가 끝났다 — 걸어 둔 「실드가 남았으면」 마디(실드가 남았을 때만 · 아니면 그냥 사라짐).</summary>
        void ShieldLeftFire()
        {
            if (Later.Count == 0) return;
            var list = Later.Where(r => r.Name == SHIELD_LEFT).ToList();
            if (list.Count == 0) return;
            bool left = PartyShield > 0;
            foreach (var r in list)
            {
                Later.Remove(r);
                if (!left || Over != null) continue;
                var owner = HeroUnit(r.Owner);
                if (r.Owner != null && (owner == null || owner.Dead)) continue;
                var (prev, seq0) = (Acting, ActSeq);
                Acting = r.Owner; ActSeq = ++SeqN;
                Say($"실드가 남았다 — {owner?.Name ?? ""}");
                try { RunFx(r.Fx, new FxCtx { Owner = owner, TargetIdx = (Enemies.FirstOrDefault(x => x.Idx == r.Target && !x.Dead) ?? AliveEnemies().FirstOrDefault())?.Idx ?? 0 }); }
                finally { Acting = prev; ActSeq = seq0; }
                CheckOver();
            }
        }

        // ── 아야 「만년설의 앎」(엘다인 4차) ────────────────────────────────
        public static readonly string[] INTENT_KINDS = { "attack", "defend", "buff", "jam" };
        public static readonly Dictionary<string, string> INTENT_KIND_KO = new() { ["attack"] = "공격", ["defend"] = "방어", ["buff"] = "강화", ["jam"] = "방해" };
        /// <summary>적의 수 종류 — attack(치는 수 · 고학년 · 모으기) · defend(방어 · 보호 · 회복) · buff(강화 · 소환 · 셈) · jam(방해 · 약화 · 카드 넣기 …). 수가 없으면 null.</summary>
        public static string IntentKind(Intent it)
        {
            if (it == null) return null;
            switch (it.T)
            {
                case "attack": case "back": case "multi": case "attackAll": case "thorns": case "charge": case "ult": return "attack";
                case "block": case "guard": case "brace": case "heal": case "selfHeal": case "revive": case "feign": return "defend";
                case "debuff": case "jam": case "addCard": case "cardDebuff": case "handCost": case "seize": case "reshuffle": return "jam";
                default: return "buff";
            }
        }
        /// <summary>그 사도의 앎 키워드(known) — 없으면 null.</summary>
        KwRt KnownKw(string hero) => hero == null ? null : Kw.Values.FirstOrDefault(k => k.Def.Known && k.Owner == hero);
        /// <summary>그 사도(앎이 없으면 파티의 아무 앎)가 그 종류를 아나.</summary>
        public bool Knows(string hero, string kind)
        {
            if (kind == null || Counts.Count == 0) return false;
            var kw = KnownKw(hero);
            if (kw != null) return Counts.ContainsKey("known|" + kw.Owner + "|" + kind);
            foreach (var k in Kw.Values) if (k.Def.Known && Counts.ContainsKey("known|" + k.Owner + "|" + kind)) return true;
            return false;
        }
        /// <summary>그 적의 지금 수를 아나 — once foeTurn 이면 이 내 턴에 그 적에게 한 번만(쓰면 표시).</summary>
        bool KnownFoe(Unit owner, Unit e, string once)
        {
            if (e == null || e.Dead) return false;
            var kind = IntentKind(e.Intent);
            if (!Knows(owner?.Key, kind)) return false;
            if (once == "foeTurn")
            {
                string k = "knownOnce|" + e.Idx + "|" + Turn;
                if (Counts.ContainsKey(k)) return false;
                Counts[k] = 1;
            }
            return true;
        }
        /// <summary>learn — 적의 지금 수 종류를 앎에 적는다. 일의 값 = 지금 아는 종류 수. owner(사도 id)면 그 사도의 앎에.</summary>
        void LearnFx(Fx f, FxCtx ctx)
        {
            var kw = KnownKw(f.Owner ?? ctx.Owner?.Key) ?? (f.Owner == null ? Kw.Values.FirstOrDefault(k => k.Def.Known) : null);
            if (kw == null) return;
            foreach (var e in Resolve(ctx, f.Target ?? "oneEnemy").Where(x => x.Side == Side.Enemy && !x.Dead))
            {
                var kind = IntentKind(e.Intent);
                if (kind == null || !Counts.TryAdd("known|" + kw.Owner + "|" + kind, 1)) continue;
                int before = StackOf(kw.Owner, kw.Id);
                AddStack(kw.Owner, kw.Id, 1);
                StackChanged(kw.Owner, kw.Id, before, StackOf(kw.Owner, kw.Id), HeroUnit(kw.Owner));
                Say($"「{kw.Id}」 — {e.Name}의 수({INTENT_KIND_KO[kind]})를 적음");
            }
            ctx.EventV = INTENT_KINDS.Count(k => Counts.ContainsKey("known|" + kw.Owner + "|" + k));
        }
        /// <summary>그 사도가 아는 수 종류(화면).</summary>
        public List<string> KnownKinds(string hero) { var kw = KnownKw(hero); return kw == null ? new List<string>() : INTENT_KINDS.Where(k => Counts.ContainsKey("known|" + kw.Owner + "|" + k)).Select(k => INTENT_KIND_KO[k]).ToList(); }

        // ── 봇 · 화면이 보는 이번 적 차례(▶E87) ─────────────────────────
        /// <summary>이번 적 차례에 칠 예고 피해 합(머리 위 수치 × 대 수 · 기절로 쉬는 적 · 즉시 행동을 한 적 빼고).</summary>
        public int FoeHitSum()
        {
            int sum = 0;
            foreach (var e in AliveEnemies())
            {
                if (e.Sealed || e.RushedTurn || e.Intent == null) continue;
                int? h = IntentHit(e);
                if (h is int v && v > 0) sum += v * (e.Intent.T == "multi" ? Math.Max(1, e.Intent.N) : 1);
            }
            return sum;
        }
        /// <summary>공격하려는 적이 하나라도 있나(ifFoe anyAttack).</summary>
        public bool AnyFoeAttack() => AliveEnemies().Any(e => !e.Sealed && HitLike(e.Intent));
        /// <summary>이번 적 차례 도발 중인 사도의 실드 배율 덤(없으면 0) — 봇의 예고 피해 셈.</summary>
        public double TauntNow => TauntK(Taunter());

        // ── 다른 아군의 다음 비싼 카드 강화(요청 — 이드 착한 마음씨) ─────────────
        /// <summary>empower who other — 걸어 둔 사도(owner)가 아닌 아군의 다음 카드(minCost 이상)에. 걸린 몫은 Counts 「empowerO|사도」 · 「empowerOMin|사도」.</summary>
        void EmpowerOtherAdd(Unit owner, double ratio, int minCost)
        {
            if (owner == null || ratio <= 0) return;
            string k = "empowerO|" + owner.Key;
            Counts[k] = (Counts.TryGetValue(k, out var v) ? v : 0) + Num.Round(ratio * 100);
            if (minCost > 0) Counts["empowerOMin|" + owner.Key] = minCost; else Counts.Remove("empowerOMin|" + owner.Key);
            Say($"다른 아군의 다음{(minCost > 0 ? $" 비용 {minCost} 이상" : "")} 카드 강화 +{Num.Round(ratio * 100)}%");
            StatusCue(owner, $"동료 강화 +{Num.Round(ratio * 100)}%", true);
        }
        /// <summary>그 카드(주인 hero · 비용 cost)가 쓸 다른 아군 강화(%) — use 면 쓴다.</summary>
        int EmpowerOther(string hero, int cost, bool use)
        {
            if (hero == null || Counts.Count == 0) return 0;
            int pct = 0;
            foreach (var u in Party)
            {
                if (u.Key == hero) continue;
                string k = "empowerO|" + u.Key;
                if (!Counts.TryGetValue(k, out var v) || v <= 0) continue;
                int min = Counts.TryGetValue("empowerOMin|" + u.Key, out var m) ? m : 0;
                if (cost < min) continue;
                pct += v;
                if (use) { Counts.Remove(k); Counts.Remove("empowerOMin|" + u.Key); }
            }
            return pct;
        }
    }
}
