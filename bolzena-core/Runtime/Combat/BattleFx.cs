using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>효과를 돌리는 자리 — 누가 · 누구를 · 어떤 카드로.</summary>
    public sealed class FxCtx
    {
        public Unit Owner;
        public int TargetIdx;
        public int? AllyIdx;
        public int X;
        /// <summary>축복의 배율 꼴(power · weakSpot · frost · heal · guard …).</summary>
        public string Shin;
        public HashSet<string> HitTags;
        /// <summary>사도의 카드(고학년 포함) — 강인도를 깎고 표식 · 잔광 · 충격이 돈다. 패시브는 아니다.</summary>
        public bool Card;
        public string Type;
        public bool Chain, Tune, Link, Repeat;
        /// <summary>그 카드가 손에 머문 턴 · 이 카드 앞에 이번 턴 낸 장수 · 이 카드가 버린 장수.</summary>
        public int Held, PlayedBefore, Discarded;
        /// <summary>패시브 — 그 일의 수(받은 피해 등)와 때린 적.</summary>
        public int EventV;
        public Unit Attacker;
        public bool Paid;
        public string Prev;
        /// <summary>때 붙은 마디(draw · discard · handEnd) — 그 마디만 돈다.</summary>
        public string When;
        public bool Rhythmed;
        /// <summary>계산형 — 이 카드에서 셈 조건(ifSpent · ifBalanced)이 섰다. 카드가 다 끝난 뒤 「셈이 맞으면」(tally) 계기를 낸다.</summary>
        public bool Tallied;
        public string PerStack;
        public HashSet<Unit> Toughed;
        /// <summary>패시브 — 일을 겪은 표식의 주인(적이면 효과의 「적 1명」).</summary>
        public Unit Holder;
        /// <summary>패시브 — 일을 겪은 아군.</summary>
        public Unit Ally;
        public string Passive;
        /// <summary>낸 카드의 id(손의 id) — 탐구심 · 죽음의 낙인 · cardStatus this · transform.</summary>
        public string CardId;
        /// <summary>만든 카드(~) — 형상 강화.</summary>
        public bool Made;
        /// <summary>transform(this) — 내고 나면 이 카드 대신 버린 더미로 갈 카드 id.</summary>
        public string TransformTo;
        /// <summary>강인도 피해의 AP 단위 — 카드의 적힌 비용(X 면 낸 AP). 고학년은 2.</summary>
        public int Cost = 1;
        /// <summary>강인도 단위를 따로 정할 때(고학년 — R.ToughUnitUlt). 0 이면 Cost 로.</summary>
        public double ToughUnit;
        /// <summary>두 갈래 카드에서 고른 갈래(0 = 안 고름).</summary>
        public int Choice;
        /// <summary>v2 — 피해 · 실드 · 회복 배율(다시 내기 · 대신 발동 · 저절로 내기의 비율) · 배타 무작위 갈래 · 꺼낸 카드 · 덧붙인 태그.</summary>
        public double Scale = 1;
        public int Roll;
        public string Pulled;
        public HashSet<string> ExtraTags;
        public bool Recasting, PerEach;
        /// <summary>perStack 의 최소 · 최대(n · max — 0 이면 없음). 「1개당」 이 쓰이면 지운다.</summary>
        public int PerMin, PerMax;
        public Fx CurFx;
        /// <summary>소모량을 고르는 카드(spend pick) — 고른 수(PlayOpts.Spend, null 이면 전부).</summary>
        public int? SpendPick;
        /// <summary>(시범 16) 이 카드가 이번 턴 바로 앞 카드보다 비싸다(ifPricier).</summary>
        public bool Pricier;
        /// <summary>(엘다인 ▶E32) 이번 한 번 망각 없이 사라진다(noForget).</summary>
        public bool NoForget;
        /// <summary>(엘다인 ▶E19) spend pick loop — 이어 쓸 고유 효과 · 상한(대) · 고른 뒤 남긴 몫.</summary>
        public string LoopId;
        public int LoopMax, LoopKeep;
        /// <summary>(엘다인 ▶E17) 낸 뒤에 정할 카드 값(cardStatus set · half) — 카드가 다 돈 다음 PlayCard 가 적용한다.</summary>
        public List<Fx> AfterSet;
        /// <summary>(▶E66) 이 카드는 치명 확정 · (▶E74) 고른 단계(PlayOpts.Stage).</summary>
        public bool ForceCrit;
        public int? StagePick;
        /// <summary>(엘다인 4차 ▶E77) 이 카드는 「n번째 이후」 조건이 선다(nthNext).</summary>
        public bool NthForce;
        /// <summary>(엘다인 4차 리뉴아) 이 카드를 낼 때의 비용(줄어든 값) — ifCost.</summary>
        public int CostNow;
        /// <summary>(우로스) 이번엔 짝 태세로 안 바뀐다(noToggle).</summary>
        public bool NoToggle;
        /// <summary>(엘다인 4차 요청 — 란) 이 표식이 있는 적을 치는 대는 치명 확정(critNext now foeStack).</summary>
        public string CritFoe;
        public FxCtx Copy() { var c = (FxCtx)MemberwiseClone(); c.Toughed = null; return c; }
    }

    public sealed partial class Battle
    {
        const string RHYTHM_PER = "\u0000리듬", DISC_PER = "\u0000버림", PAID_PER = "\u0000치름", DEBUFF_PER = "\u0000흠", APLEFT_PER = "\u0000남은AP", TAG_PER = "\u0000태그",
            GUARD_PER = "\u0000막음", OVERHEAL_PER = "\u0000넘침", ANYHERO_PER = "\u0000아무동료";
        /// <summary>그 아군 표시(carrier hero)가 파티에 붙은 겹 합(동료 누구에게든).</summary>
        int AnyHeroStack(string id) { int n = 0; foreach (var u in Party) n += StackOf(u.Key, id); return n; }

        /// <summary>막아 낸 양(버티형 변환) — 적의 차례 중이면 이번 판 몫, 내 턴이면 지난 판 + 이번 판(즉시 행동) 몫.</summary>
        public int GuardedVal => FoeTurn ? GuardedNow : GuardedPrev + GuardedNow;

        /// <summary>
        /// 이번 판 파티 회복의 넘친 몫 합 — pct 0 이면 최대 HP 를 넘친 몫(옛 정의), pct 가 있으면 「회복 뒤 HP 가 최대 HP × pct 를 넘은 몫」(회복량까지).
        /// 파티 HP 가 하나라 「가득일 때만 넘친다」 는 이길 때만 도는 장치가 된다 — 선을 낮춰 다시 정의한다(1단계).
        /// </summary>
        public int OverhealSum(double pct)
        {
            int sum = 0;
            for (int i = 0; i + 2 < HealLog.Count; i += 3) sum += OverPart(HealLog[i], HealLog[i + 1], HealLog[i + 2], pct);
            return sum;
        }
        static int OverPart(int raw, int v, int max, double pct) => Math.Max(0, Math.Min(v, raw - (pct > 0 ? Num.Round(max * pct) : max)));

        /// <summary>다음 카드 강화(empower) — 주인(사도 키 · 「*」 = 파티의 다음 카드)마다 비율을 더해 둔다. 판 복사 · 저장은 Counts 가 맡는다.</summary>
        void EmpowerAdd(string key, double ratio)
        {
            if (ratio <= 0) return;
            string k = "empower|" + key;
            Counts[k] = (Counts.TryGetValue(k, out var v) ? v : 0) + Num.Round(ratio * 100);
            Say($"다음 {(key == "*" ? "" : (HeroUnit(key)?.Name ?? key) + "의 ")}카드 강화 +{Num.Round(ratio * 100)}%");
            StatusCue(HeroUnit(key) ?? PartyRep(), $"강화 +{Num.Round(ratio * 100)}%", true);
        }
        /// <summary>그 사도의 카드를 낼 때 걸린 강화(사도 몫 + 파티 몫)를 쓰고 배율을 돌려준다.</summary>
        double EmpowerUse(string heroKey)
        {
            int pct = 0;
            foreach (var k in new[] { heroKey != null ? "empower|" + heroKey : null, "empower|*" })
                if (k != null && Counts.TryGetValue(k, out var v) && v > 0) { pct += v; Counts.Remove(k); }
            if (pct > 0) Say($"강화 +{pct}% — 이 카드에");
            return 1 + pct / 100.0;
        }
        /// <summary>걸려 있는 다음 카드 강화(%) — 봇 · 화면.</summary>
        public int EmpowerOf(string heroKey) => (heroKey != null && Counts.TryGetValue("empower|" + heroKey, out var a) ? a : 0) + (Counts.TryGetValue("empower|*", out var b) ? b : 0);

        /// <summary>그 적에게 걸린 해로운 상태의 가짓수 — 해로운 상태(R.BAD_ST · 표식 · 기절) + 적 표식 키워드 하나하나.</summary>
        public int DebuffKinds(Unit e)
        {
            if (e == null) return 0;
            int n = R.BAD_ST.Count(id => St(e, id) > 0) + (St(e, "표식") > 0 ? 1 : 0) + (e.Sealed && !e.Broken ? 1 : 0);
            foreach (var kw in Kw.Values) if (kw.Carrier == "enemy" && St(e, kw.Id) > 0) n++;
            return n;
        }

        List<Unit> Resolve(FxCtx ctx, string target)
        {
            var owner = ctx.Owner;
            var foes = AliveEnemies();
            var allies = AliveParty().ToList();
            var v2 = ResolveV2(ctx, target, foes, allies, ctx.CurFx);
            if (v2 != null) return v2;
            switch (target)
            {
                case "allEnemies": return foes;
                case "topEnemy": return foes.OrderByDescending(e => e.Hp).ThenBy(e => e.Idx).Take(1).ToList();
                case "lowEnemy": return foes.OrderBy(e => e.Hp).ThenBy(e => e.Idx).Take(1).ToList();
                case "bigHitEnemy": { var bh = BigHitEnemy(); return bh != null ? new List<Unit> { bh } : new List<Unit>(); }
                case "holder":   // 그 아군 표시(조각의 id)가 지금 붙은 동료 — 없으면 아무도(엘다인 4차 벨라 「존속」)
                    { var hid = ctx.CurFx?.Id; return hid == null ? new List<Unit>() : allies.Where(u => StackOf(u.Key, hid) > 0).Take(1).ToList(); }   // 공격 예고가 가장 큰 적(엘다인 3차)
                case "randomEnemy":
                    if (PreviewPick != null) return foes.Where(e => e.Idx == PreviewPick).Take(1).ToList();
                    return foes.Count > 0 ? new List<Unit> { foes[Rng.Int(foes.Count)] } : new List<Unit>();
                case "allAllies": return allies;
                case "otherAllies": return allies.Where(u => u != owner).ToList();
                case "party": return allies.Count > 0 ? new List<Unit> { owner != null && !owner.Dead && owner.Side == Side.Party ? owner : allies[0] } : new List<Unit>();
                case "oneAlly":
                    {
                        if (ctx.Ally != null) return ctx.Ally.Dead ? new List<Unit>() : new List<Unit> { ctx.Ally };
                        var pick = ctx.AllyIdx != null ? allies.FirstOrDefault(u => u.Idx == ctx.AllyIdx) : allies.FirstOrDefault(u => u.Idx == ctx.TargetIdx);
                        var t = pick ?? owner ?? allies.FirstOrDefault();
                        return t != null ? new List<Unit> { t } : new List<Unit>();
                    }
                case "self": return owner != null ? new List<Unit> { owner } : new List<Unit>();
                default:
                    {
                        var t = foes.FirstOrDefault(e => e.Idx == ctx.TargetIdx) ?? foes.FirstOrDefault();
                        return t != null ? new List<Unit> { t } : new List<Unit>();
                    }
            }
        }

        /// <summary>파티는 한 몸 — 사도 여럿을 가리켜도 파티 몫은 한 번.</summary>
        static List<Unit> Once(List<Unit> list)
        {
            bool seen = false;
            return list.Where(t => t.Side != Side.Party || (!seen && (seen = true))).ToList();
        }

        public int StackOf(string heroKey, string id) => heroKey != null && Stacks.TryGetValue(heroKey, out var b) && b.TryGetValue(id, out var v) ? v : 0;
        void AddStack(string heroKey, string id, int v)
        {
            if (!Stacks.TryGetValue(heroKey, out var bag)) Stacks[heroKey] = bag = new Dictionary<string, int>();
            int next = (bag.TryGetValue(id, out var x) ? x : 0) + v;
            if (StackCap.TryGetValue(heroKey, out var caps) && caps.TryGetValue(id, out var cap))
                next = Kw.TryGetValue(id, out var wk) && wk.Def.Wrap && cap > 0 && next > cap ? ((next - 1) % cap) + 1 : Math.Min(cap, next);
            bag[id] = Math.Max(0, next);
        }

        /// <summary>「1개당」 의 수 — perStack 에 n(최소) · max(최대)가 있으면 그 안으로(미로 「거울 속」 광선 5~10발).</summary>
        int PerCount(FxCtx ctx, string id)
        {
            int c = PerCountRaw(ctx, id);
            if (ctx.PerMax > 0) c = Math.Min(c, ctx.PerMax);
            if (ctx.PerMin > 0) c = Math.Max(c, ctx.PerMin);
            ctx.PerMin = ctx.PerMax = 0;
            return c;
        }

        int PerCountRaw(FxCtx ctx, string id)
        {
            if (id == RHYTHM_PER) return St(Pool, R.RHYTHM);
            if (id.StartsWith(ANYHERO_PER, StringComparison.Ordinal)) return AnyHeroStack(id.Substring(ANYHERO_PER.Length));
            if (id == DISC_PER) return ctx.Discarded;
            if (id == APLEFT_PER) return Ap;
            if (id.StartsWith(GUARD_PER, StringComparison.Ordinal)) return GuardedVal / Math.Max(1, int.Parse(id.Substring(GUARD_PER.Length)));
            if (id.StartsWith(OVERHEAL_PER, StringComparison.Ordinal))
            {
                var p = id.Substring(OVERHEAL_PER.Length).Split('|');
                return OverhealSum(int.Parse(p[0]) / 100.0) / Math.Max(1, int.Parse(p[1]));
            }
            { int kc = KitCount(ctx, id); if (kc >= 0) return kc; }
            { int ec = EldainCount(ctx, id); if (ec >= 0) return ec; }
            if (id.StartsWith(TAG_PER, StringComparison.Ordinal)) { var tg = id.Substring(TAG_PER.Length); return Hand.Count(h => HasTagB(h, tg)); }
            if (id.StartsWith(PAID_PER, StringComparison.Ordinal)) return PaidHp / System.Math.Max(1, int.Parse(id.Substring(PAID_PER.Length)));
            if (id == DEBUFF_PER) { var t = ctx.Holder != null && ctx.Holder.Side == Side.Enemy ? ctx.Holder : Resolve(ctx, "oneEnemy").FirstOrDefault(); return t != null ? DebuffKinds(t) : 0; }
            Kw.TryGetValue(id, out var kw);
            if (kw != null && kw.Carrier == "enemy")
            {
                var t = ctx.Holder != null && ctx.Holder.Side == Side.Enemy ? ctx.Holder : Resolve(ctx, "oneEnemy").FirstOrDefault();
                return t != null ? St(t, id) : 0;
            }
            if (kw != null && kw.Carrier == "ally") return ctx.Owner != null ? St(ctx.Owner, id) : 0;
            return ctx.Owner != null ? StackOf(ctx.Owner.Key, id) : 0;
        }

        int AtkOf(Unit u) => Math.Max(1, Num.Round(u.Atk * (1 + StatMod(u, "atk"))));
        int DefOf(Unit u) => Math.Max(0, Num.Round(u.Def * (1 + StatMod(u, "def"))));

        /// <summary>피해 한 대 — 스탯 × 배율(치명 · 축복 포함). 화면의 카드 면 숫자도 이것을 쓴다.</summary>
        public int HitAmount(Unit u, double ratio, bool shin = false, bool crit = false, string bas = null) =>
            R.FinalDamage(bas == "def" ? R.DefDmgStat(AtkOf(u), DefOf(u)) : AtkOf(u), ratio, shin: shin, crit: crit, critX: CritXOf(u));   // 파티 — 판 덤 × 치명 피해 상태(▶E83)
        /// <summary>실드 · 회복의 바탕 방어력(2026-10-10 카제나식) — 기본 방어력(사도 데이터) + 추가 방어력(아티팩트 · 판 성장 · 교주 보드, Unit.GearAdd) × R.GUARD_EXTRA_K, 그다음 전투 중 「방어 +%」 를 곱한다.</summary>
        int GuardDef(Unit u)
        {
            int extra = u.Side == Side.Party ? (u.GearAdd?.Def ?? 0) : 0;
            return Math.Max(0, Num.Round(((u.Def - extra) + extra * R.GUARD_EXTRA_K) * (1 + StatMod(u, "def"))));
        }
        /// <summary>역할별 실드 · 회복 배율 — R.RoleGuard × 같은 역할 겹침(R.ROLE_DUP). 적은 1.</summary>
        double RoleGuardOf(Unit u)
        {
            if (u.Side != Side.Party || u.Role == null) return 1;
            double k = R.RoleGuard(u.Role);
            if (R.ROLE_DUP > 0 && u.Role != "딜러") { int n = Party.Count(x => x.Role == u.Role); if (n > 1) k /= 1 + R.ROLE_DUP * (n - 1); }
            return k;
        }
        public int GuardAmount(Unit u, double ratio, string shin) => Math.Max(1, Num.Round(GuardDef(u) * ratio * RoleGuardOf(u) * (shin == "guard" ? R.SHIN : 1) * Math.Max(0.1, 1 + StatMod(u, "guard"))));
        public int HealAmount(Unit u, double ratio, string shin) => Math.Max(1, Num.Round(GuardDef(u) * ratio * RoleGuardOf(u) * (shin == "heal" ? R.SHIN : 1) * Math.Max(0, 1 + StatMod(u, "heal"))));

        /// <summary>효과 조각 목록을 돌린다(카드 · 신탁 · 축복 · 패시브 · 키워드 · 고학년 공용).</summary>
        void RunFx(List<Fx> list, FxCtx ctx)
        {
            if (list == null) return;
            var owner = ctx.Owner;
            bool gate = true;
            string part = null;
            foreach (var f in list)
            {
                if (f.K == FxK.When) { part = f.On; gate = true; continue; }
                if (ctx.When != part) continue;
                // 조건 조각은 새 문장을 연다 — 앞 조건이 서지 않았어도 다음 조건은 다시 본다(「파괴: … 조율: …」 은 따로따로)
                if (!gate && !FxK.Conditions.Contains(f.K)) continue;
                ctx.CurFx = f;
                switch (f.K)
                {
                    // ── 조건 ──
                    case FxK.IfBroken:
                        {
                            var t = Enemies.FirstOrDefault(e => e.Idx == ctx.TargetIdx);
                            bool single = list.Any(x => x.K == FxK.Dmg && (x.Target ?? "oneEnemy") == "oneEnemy");
                            gate = single ? t != null && t.Dead : KillSeq == ActSeq && ActSeq != 0;
                            break;
                        }
                    case FxK.IfTune: gate = ctx.Tune; break;
                    case FxK.IfChain: gate = ctx.Chain; break;
                    case FxK.IfLink:
                    case FxK.IfPrev:
                        gate = f.K == FxK.IfLink ? ctx.Link : ctx.Prev != null && ctx.Prev == f.Type;
                        if (gate && ctx.Card && !ctx.Rhythmed) { ctx.Rhythmed = true; RhythmAdd(1); }
                        break;
                    case FxK.IfRhythm: gate = St(Pool, R.RHYTHM) >= f.N; break;
                    case FxK.IfSwitched: gate = SwitchTurn != null && SwitchTurn == Turn; break;
                    case FxK.PerRhythm: ctx.PerStack = RHYTHM_PER; break;
                    case FxK.IfStack:
                        {
                            Kw.TryGetValue(f.Id, out var kw);
                            bool has;
                            if (kw != null && kw.Carrier == "enemy") { var t = ctx.Holder != null && ctx.Holder.Side == Side.Enemy ? ctx.Holder : Resolve(ctx, "oneEnemy").FirstOrDefault(); has = t != null && St(t, f.Id) >= Math.Max(1, f.N) && (f.Max <= 0 || St(t, f.Id) <= f.Max); }
                            else if (kw != null && kw.Carrier == "ally") has = owner != null && St(owner, f.Id) > 0;
                            else if (kw != null && kw.Carrier == "hero" && f.Who == "any") { int an = AnyHeroStack(f.Id); has = an >= Math.Max(1, f.N) && (f.Max <= 0 || an <= f.Max); }   // 동료 누구에게 붙어 있든(벨라 존속)
                            else has = owner != null && StackOf(owner.Key, f.Id) >= Math.Max(1, f.N) && (f.Max <= 0 || StackOf(owner.Key, f.Id) <= f.Max);
                            gate = f.Not ? !has : has;
                            break;
                        }
                    case FxK.PerStack: ctx.PerStack = f.Who == "any" ? ANYHERO_PER + f.Id : f.Id; ctx.PerEach = f.Each; ctx.PerMin = f.N; ctx.PerMax = f.Max; break;
                    // ── 운영 방식 계기(docs/19 §3) ──
                    case FxK.IfRepeat:   // 같은 카드 잇달아 — 박자형(서면 리듬 +1)
                        gate = ctx.Repeat;
                        if (gate && ctx.Card && !ctx.Rhythmed) { ctx.Rhythmed = true; RhythmAdd(1); }
                        break;
                    case FxK.IfHeld: gate = ctx.Held >= System.Math.Max(1, f.N); break;               // 손에 N턴 머문 카드 — 아껴 두기
                    case FxK.IfPlayedMax: gate = ctx.PlayedBefore <= f.N; break;                         // 이번 턴 이 카드 앞에 낸 장수가 N 이하 — 아껴 두기
                    case FxK.IfApLeft: gate = Ap >= System.Math.Max(1, f.N); break;                     // 이 카드를 내고도 AP 가 N 남았으면 — 아껴 두기
                    case FxK.IfSpent: gate = ApSpent == f.N; if (gate && ctx.Card) ctx.Tallied = true; break;   // 이번 턴 쓴 AP 가 꼭 N — 계산
                    case FxK.IfBalanced:                                                                 // 이번 턴 공격과 스킬을 같은 장수로 — 계산
                        { int a = PlayLog.Count(p => p.type == "공격"), sk = PlayLog.Count(p => p.type == "스킬"); gate = a > 0 && a == sk; if (gate && ctx.Card) ctx.Tallied = true; break; }
                    case FxK.IfHunted:                                                                   // 고른 적이 아군 누구에게든 찍혀 있으면 — 표적
                        { var t = Resolve(ctx, "oneEnemy").FirstOrDefault(); gate = t != null && Kw.Values.Any(k => k.Def.Hunt && St(t, k.Id) > 0); break; }
                    case FxK.IfDebuffs:                                                                  // 고른 적의 디버프가 N가지 이상 — 흠 세기
                        gate = DebuffKinds(Resolve(ctx, "oneEnemy").FirstOrDefault()) >= System.Math.Max(1, f.N); break;
                    case FxK.PerDiscarded: ctx.PerStack = DISC_PER; ctx.PerMin = f.N; ctx.PerMax = f.Max; break;   // n · max — 최소 · 최대(엘다인 3차)
                    case FxK.PerApLeft: ctx.PerStack = APLEFT_PER; break;
                    case FxK.PerGuarded: ctx.PerStack = GUARD_PER + (f.Per > 0 ? f.Per : 1); ctx.PerMax = f.Max; break;   // 막아 낸 양 per 당(1단계) · max — 최대(118명 3단계)
                    case FxK.PerOverheal: ctx.PerStack = OVERHEAL_PER + Num.Round(f.Pct * 100) + "|" + (f.Per > 0 ? f.Per : 1); ctx.PerMax = f.Max; break;   // 이번 판 넘친 회복 per 당(pct 면 그 선 넘은 몫)
                    case FxK.Empower:
                        if (f.Who == "other") { EmpowerOtherAdd(owner, f.Ratio, f.MinCost); break; }   // 다른 아군의 다음(비용 minCost 이상) 카드(엘다인 4차 요청)
                        EmpowerAdd(f.Who == "any" ? "*" : owner?.Key ?? "*", f.Ratio); break;
                    case FxK.IfHp: gate = (double)Pool.Hp / System.Math.Max(1, Pool.MaxHp) <= (f.Pct > 0 ? f.Pct : 0.5); if (f.Not) gate = !gate; break;
                    case FxK.Feed:   // 아군 키워드 +N — 가리킨 사도(기본 아군 전원)의 자기 주머니 키워드를 v 씩(모드 키워드는 빼고)
                        {
                            var ts = Resolve(ctx, f.Target ?? "allAllies");
                            foreach (var t in ts)
                            {
                                var kw = Kw.Values.FirstOrDefault(k => k.Owner == t.Key && k.Carrier == "self" && !k.Def.Mode);
                                if (kw == null) continue;
                                StackFx(new Fx { K = FxK.Stack, Id = kw.Id, V = f.V }, new FxCtx { Owner = t, TargetIdx = ctx.TargetIdx });
                                if (Over != null) return;
                            }
                            break;
                        }
                    case FxK.PerPaid: ctx.PerStack = PAID_PER + (f.Per > 0 ? f.Per : 100); break;
                    // ── 키워드 사전(2026-10-05) ──
                    case FxK.IfKill:                                                                     // 처치: 이 카드(이 일)가 적을 쓰러뜨렸으면
                        gate = KillSeq == ActSeq && ActSeq != 0;
                        // id(시범 16 쵸피) — elite: 엘리트 싸움의 적 · 보스 · boss: 보스만
                        if (gate && f.Id != null) gate = (Counts.TryGetValue("killTier", out var ktv) ? ktv : 0) >= (f.Id == "boss" ? 2 : 1);
                        if (f.Not) gate = !gate;
                        break;
                    case FxK.IfBreak: gate = BreakSeq == ActSeq && ActSeq != 0; if (f.Not) gate = !gate; break;   // 붕괴: 이 카드가 적을 격파했으면(not — 못 했으면, 시범 16 샤샤)
                    case FxK.IfWounded:                                                                 // 부상: 체력 30% 미만(기본 파티, target oneEnemy 면 고른 적)
                        {
                            var w = f.Target == "oneEnemy" ? Resolve(ctx, "oneEnemy").FirstOrDefault() : Pool;
                            gate = w != null && (double)w.Hp / Math.Max(1, w.MaxHp) < R.SV("부상");
                            break;
                        }
                    case FxK.PerTag: ctx.PerStack = TAG_PER + f.Id; break;
                    case FxK.Drain:                                                                     // 피해 기반 회복 — 이 일로 준 피해 × ratio, 최대 체력 20% 상한
                        {
                            int dealt = DealtSeq == ActSeq ? DealtAct : 0;
                            int v = Math.Min(Num.Round(Pool.MaxHp * R.SV("흡수Max")), Num.Round(dealt * f.Ratio));
                            if (v > 0 && !Pool.Dead) { int h0 = Pool.Hp; Pool.Hp = Math.Min(Pool.MaxHp, Pool.Hp + v); HealCue(PartyRep(owner), h0); TallyHeal(owner, Pool.Hp - h0); Say($"피해 기반 회복 +{Pool.Hp - h0}"); }
                            break;
                        }
                    case FxK.Extra:                                                                     // 추가 공격 — 공격력(또는 방어 기반) × ratio, 탄성 · 「추가 공격하면」
                        {
                            if (owner == null) break;
                            int hits = f.HitsOr1;
                            if (ctx.PerStack != null) { hits *= PerCount(ctx, ctx.PerStack); ctx.PerStack = null; }
                            for (int i = 0; i < hits && Over == null; i++)
                                foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy")) ExtraHit(owner, t, f.Ratio, f.Base, "추가 공격");
                            break;
                        }
                    case FxK.CardStatus: CardStatusFx(f, ctx); break;
                    case FxK.Transform:                                                                 // 카드 바꾸기(강화 · 진화) — 이 전투에서만
                        {
                            if (f.From == null) { ctx.TransformTo = f.Id; break; }
                            int cnt = 0;
                            for (int i = 0; i < Hand.Count && cnt < f.NOr1; i++)
                                if (f.From == "hand" || f.From.StartsWith("@", StringComparison.Ordinal) ? FxMatch(f.From.StartsWith("@", StringComparison.Ordinal) ? new Fx { Type = f.From.Substring(1) } : f, Hand[i], owner) && GameData.BaseId(Hand[i]) != f.Id : GameData.BaseId(Hand[i]) == f.From)
                                {
                                    var into = f.Id + GameData.PLAIN;
                                    CardCue(Hand[i], "hand", "gone", "transform"); Hand[i] = into; CardCue(into, "new", "hand", "transform"); cnt++;
                                }
                            if (cnt > 0) { Say($"「{Data.Card(f.From)?.Name ?? f.From}」 {cnt}장 → 「{Data.Card(f.Id)?.Name}」"); MergePile(Hand, "hand"); }
                            break;
                        }
                    case FxK.PerDebuff: ctx.PerStack = DEBUFF_PER; break;
                    case FxK.NextAp:                                                                     // 다음 턴 AP ±v — 대가
                        if (f.IV < 0) { ApJam += -f.IV; Pay(ctx); } else ApCarry += f.IV;
                        Say($"다음 턴 AP {(f.IV > 0 ? "+" : "")}{f.IV}");
                        break;
                    case FxK.Burn: BurnFx(f.All ? -1 : f.IV, f.Random, ctx, HasFilter(f) ? f : null); if (Over != null) return; break;
                    case FxK.Reflect:                                                                    // 받은 피해의 N% 되돌리기 — 버팀
                        {
                            if (owner == null) break;
                            int basis = ctx.Passive != null ? ctx.EventV : TakenPrev;
                            var t = ctx.Attacker != null && !ctx.Attacker.Dead && ctx.Attacker.Side == Side.Enemy ? ctx.Attacker : Resolve(ctx, f.Target ?? "oneEnemy").FirstOrDefault();
                            int v = Num.Round(basis * f.Ratio);
                            if (t != null && v > 0) { Say($"되돌린다 → {t.Name} ({v})"); Hurt(t, v, new HurtOpts { From = owner, Fixed = true }); }
                            break;
                        }

                    // ── 피해 ──
                    case FxK.Dmg:
                        {
                            if (owner == null) break;
                            if (f.OfStack != null) ctx.EventV = PerCountRaw(ctx, f.OfStack);   // 저장한 값(겹) — ofEvent 와 같이
                            if (f.OfShield) ctx.EventV = PartyShield;   // (엘다인 ▶E1) 지금 파티 실드
                            if (f.OfForm > 0) ctx.EventV = FormDealt(owner.Key);   // (엘다인 4차 ▶E76) 이번 변신 중 준 피해
                            double ofEv = f.OfForm > 0 ? f.OfForm : f.OfEvent;
                            if (ofEv > 0)
                            {   // 일의 값 × ofEvent 를 고정 피해로(넘친 치유 · 막아 낸 양 …)
                                int ev = Math.Max(1, Num.Round(ctx.EventV * ofEv * ctx.Scale));
                                foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy")) { if (ctx.EventV > 0) Hurt(t, ev, new HurtOpts { From = owner, Fixed = true }); if (Over != null) return; }
                                ctx.PerStack = null;
                                break;
                            }
                            if (ctx.PerEach && ctx.PerStack != null)
                            {   // 적마다 제 겹으로(perStack each)
                                string pid = ctx.PerStack; ctx.PerStack = null; ctx.PerEach = false;
                                foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy").ToList())
                                {
                                    if (t.Dead) continue;
                                    var c1 = ctx.Copy(); c1.TargetIdx = t.Idx; c1.Holder = t; c1.PerStack = pid; c1.Toughed = ctx.Toughed;
                                    var one = f.Copy(); one.Target = "oneEnemy";
                                    RunFx(new List<Fx> { one }, c1);
                                    if (Over != null) return;
                                }
                                break;
                            }
                            string dTarget = f.Target ?? "oneEnemy";
                            if (dTarget == "oneEnemy" && ctx.Card && ctx.Type == "공격" && Kw.Values.Any(k => k.Def.Spread && k.Owner == owner.Key && StackOf(owner.Key, k.Id) <= 0)) dTarget = "allEnemies";
                            // hitsPer — 고유 효과 겹 + hitsAdd 회(엘다인 ▶E24)
                            int hits = f.XHits ? ctx.X + (f.XStack != null ? StackOf(owner.Key, f.XStack) : 0) : f.HitsPer != null ? Math.Max(0, PerCountRaw(ctx, f.HitsPer) + f.HitsAdd) : f.HitsOr1;
                            if (ctx.PerStack != null) { hits *= PerCount(ctx, ctx.PerStack); ctx.PerStack = null; }
                            // (엘다인 ▶E19) spend pick loop — 고른 만큼 친 뒤, 치는 동안 새로 얻은 겹(남겨 둔 몫 위)을 1씩 더 써서 이어 친다(합해 LoopMax 대까지)
                            string loopId = ctx.LoopId; int loopMax = ctx.LoopMax, loopKeep = ctx.LoopKeep; ctx.LoopId = null;
                            if (loopId != null) hits = Math.Min(hits, loopMax);
                            // 카드의 덤 배율 — 탐구심(이 카드) · 칼날 벼리기(공격 카드의 방어 기반) · 형상 강화(만든 카드)
                            double ck = 1;
                            if (ctx.Card && ctx.CardId != null) { int g = CardStOf(ctx.CardId, "탐구심"); if (g > 0) ck *= 1 + R.SV("탐구심") * g; }
                            if (f.Base == "def" && ctx.Type == "공격") ck *= 1 + R.StackEff("칼날 벼리기", St(owner, "칼날 벼리기"));
                            if (ctx.Made) ck *= 1 + R.StackEff("형상 강화", St(owner, "형상 강화"));
                            string brandId = ctx.CardId != null ? GameData.BaseId(ctx.CardId) : null;
                            int killCap = f.KillMore ? (f.Max > 0 ? f.Max : KILL_MORE_MAX) : 0;   // (엘다인 4차 ▶E79) 처치하면 한 대 더
                            for (int i = 0; i < hits; i++)
                            {
                                int alive0 = killCap > 0 ? AliveEnemies().Count : 0;
                                foreach (var t in Resolve(ctx, dTarget))
                                {
                                    double critPct = owner.Crit + StatMod(owner, "crit") * 100;
                                    bool crit = !Preview && (Rng.Next() * 100 < critPct || ctx.ForceCrit || (ctx.CritFoe != null && St(t, ctx.CritFoe) > 0));   // 치명 확정(▶E66) — 난수는 그대로 쓴다
                                    bool boost = ctx.Shin == "power" || (ctx.Shin == "weakSpot" && St(t, "취약") > 0);
                                    bool first = ctx.Card && t.Side == Side.Enemy && !(ctx.Toughed != null && ctx.Toughed.Contains(t));
                                    if (first) { CardHit(owner, t, ctx); if (t.Dead) continue; }
                                    if (first && ctx.Type == "공격") GlowUse();
                                    double k2 = ctx.Scale * ck * (brandId != null && St(t, "죽음의 낙인") > 0 && t.Brand.Contains(brandId) ? 1 + R.SV("죽음의 낙인") : 1);
                                    if (f.Dot)
                                    {   // 고정 지속 피해 — 증감 · 상태 · 방어 · 실드를 모두 안 탄다
                                        Hurt(t, Math.Max(1, Num.Round(AtkOf(owner) * f.Ratio * k2)), new HurtOpts { From = owner, Pure = true, Dot = true });
                                        continue;
                                    }
                                    int v = f.Fixed ? Math.Max(1, Num.Round(AtkOf(owner) * f.Ratio * k2)) : HitAmount(owner, f.Ratio * k2 + (ctx.Card ? Morale() : 0), boost, crit, f.Base);
                                    // 처치 일격도 강인도를 깎는다 — 쓰러지기 직전에 이 카드의 강인도 피해(LethalKill)
                                    var (lf0, lt0) = (lethalFor, lethalTough);
                                    if (ctx.Card && t.Side == Side.Enemy) { var ht = t; lethalFor = t; lethalTough = () => CardTough(owner, ht, ctx, dTarget, true); }
                                    try { Hurt(t, v, new HurtOpts { From = owner, Crit = !f.Fixed && crit, Tags = ctx.HitTags, Card = ctx.Card, Fixed = f.Fixed, Attack = ctx.Type == "공격" }); }
                                    finally { (lethalFor, lethalTough) = (lf0, lt0); }
                                    if (ctx.Card && t.Side == Side.Enemy && !t.Dead)
                                    {
                                        bool once = ctx.Toughed == null || !ctx.Toughed.Contains(t);
                                        CardTough(owner, t, ctx, dTarget, false);
                                        if (once && !t.Dead) Mark(owner, t, ctx);
                                    }
                                    else if (first) (ctx.Toughed ??= new HashSet<Unit>()).Add(t);
                                    if (ctx.Shin == "frost" && !t.Dead) AddStatus(t, "취약", 1, 1, null);
                                    if (first && ctx.Card && !t.Dead && MarkedWeak(t)) UseWeakMark(t);
                                    // (엘다인 ▶E3) 맞은 적마다 — 한 대마다 그 적에게(쓰러졌으면 안 돈다)
                                    if (f.HitFx != null && f.HitFx.Count > 0 && !t.Dead && Over == null)
                                    {
                                        var hc = ctx.Copy(); hc.TargetIdx = t.Idx; hc.Holder = t; hc.PerStack = null; hc.Toughed = ctx.Toughed;
                                        RunFx(f.HitFx, hc);
                                    }
                                }
                                if (killCap > 0 && hits < killCap && Over == null && AliveEnemies().Count < alive0)
                                {   // (엘다인 4차 ▶E79) 이 대에 처치했으면 한 대 더(max 대까지)
                                    hits++;
                                    Say($"처치 — 한 대 더({hits}/{killCap})");
                                }
                                if (loopId != null && i == hits - 1 && hits < loopMax && Over == null && StackOf(owner.Key, loopId) > loopKeep)
                                {
                                    SpendFx(new Fx { K = FxK.Spend, Id = loopId, V = 1 }, new FxCtx { Owner = owner });
                                    hits++;
                                    Say($"「{loopId}」 — 이어 쏜다({hits}/{loopMax})");
                                }
                            }
                            // (아야) 아는 수를 하는 적은 knownHits 회 — 모자란 만큼 그 적에게 더 친다
                            if (f.KnownHits > 1 && Over == null)
                                foreach (var t in Resolve(ctx, dTarget).Where(x => x.Side == Side.Enemy && !x.Dead).ToList())
                                {
                                    if (!KnownFoe(owner, t, f.Once)) continue;
                                    for (int j = hits; j < hits * f.KnownHits && !t.Dead && Over == null; j++)
                                    {
                                        bool kc = !Preview && Rng.Next() * 100 < owner.Crit + StatMod(owner, "crit") * 100;
                                        Hurt(t, HitAmount(owner, f.Ratio * ctx.Scale * ck + (ctx.Card ? Morale() : 0), false, kc, f.Base), new HurtOpts { From = owner, Crit = kc, Tags = ctx.HitTags, Card = ctx.Card, Attack = ctx.Type == "공격" });
                                    }
                                }
                            break;
                        }

                    // ── 방어 · 실드 · 회복 ──
                    case FxK.Block:
                    case FxK.Shield:
                        {
                            if (owner == null) break;
                            if (f.OfStack != null) ctx.EventV = PerCountRaw(ctx, f.OfStack);
                            if (f.OfShield) ctx.EventV = PartyShield;   // (엘다인 ▶E1)
                            int k = 1;
                            if (ctx.PerStack != null) { k = PerCount(ctx, ctx.PerStack); ctx.PerStack = null; if (k == 0) break; }
                            double ratio = f.Ratio * k * ctx.Scale * (ctx.Made ? 1 + R.StackEff("형상 강화", St(owner, "형상 강화")) : 1);
                            int v0 = f.OfEvent > 0 ? Math.Max(0, Num.Round(ctx.EventV * f.OfEvent * ctx.Scale)) : f.Fixed ? Math.Max(1, Num.Round(DefOf(owner) * ratio)) : GuardAmount(owner, ratio, ctx.Shin);
                            if (v0 <= 0) break;
                            foreach (var t in Once(Resolve(ctx, f.Target ?? (f.K == FxK.Block ? "self" : "party"))))
                            {
                                int v = f.Fixed || f.OfEvent > 0 ? v0 : ShieldGain(t, v0);
                                if (t.Side == Side.Party && v > 0) ShieldBy = owner.Key;
                                if (f.K == FxK.Block) t.Block += v; else t.Shield += v;
                                if (t.Side == Side.Party) TallyShield(owner, v);   // 전투 기록 — 파티에 얹은 방어 · 실드
                                GainCue(t, f.K, v);
                                if (v > 0) Punish(t);
                                if (v > 0 && t.Side == Side.Party && !t.Dead) Emit("guard", new EmitInfo { Who = t, Kind = f.K, Seq = ActSeq });
                            }
                            break;
                        }
                    case FxK.Heal:
                        {
                            if (owner == null) break;
                            int k = 1;
                            if (ctx.PerStack != null) { k = PerCount(ctx, ctx.PerStack); ctx.PerStack = null; if (k == 0) break; }
                            foreach (var t in Once(Resolve(ctx, f.Target ?? "party")))
                            {
                                int h0 = t.Hp, v = HealAmount(owner, f.Ratio * k * ctx.Scale * (ctx.Made ? 1 + R.StackEff("형상 강화", St(owner, "형상 강화")) : 1), ctx.Shin);
                                t.Hp = Math.Min(t.MaxHp, t.Hp + v);
                                int over = Math.Max(0, h0 + v - t.MaxHp);
                                HealCue(t, h0, over);
                                if (t.Side == Side.Party && (double)h0 / Math.Max(1, t.MaxHp) < R.SV("부상") && (double)t.Hp / Math.Max(1, t.MaxHp) >= R.SV("부상")) Emit("unwound", new EmitInfo { By = Acting ?? owner.Key, Who = t });
                                if (t.Side == Side.Party) { HealLog.Add(h0 + v); HealLog.Add(v); HealLog.Add(t.MaxHp); SumHealed += Math.Min(v, Math.Max(0, t.MaxHp - h0)); TallyHeal(owner, Math.Min(v, Math.Max(0, t.MaxHp - h0))); }
                                // 회복형 계기 — 넘친 회복(V). 규칙에 pct 가 있으면 「회복 뒤 HP 그 비율 이상 — 선을 넘은 몫」 으로 다시 잰다(Before = 회복 전 HP · After = 넘치기 전 HP · N = 회복량)
                                if (t.Side == Side.Party && !t.Dead && Acting != null && v > 0) Emit("overheal", new EmitInfo { By = Acting, Who = t, V = over, Before = h0, After = h0 + v, N = v, Seq = ActSeq });
                            }
                            break;
                        }
                    case FxK.Strip:
                        foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy").ToList())
                        {
                            int lost = t.Block + t.Shield;
                            t.Block = 0; t.Shield = 0;
                            if (lost <= 0) continue;
                            LoseGuardCue(t, lost);
                            Say($"{t.Name}: 실드 파괴 (-{lost})");
                            if (t.Side == Side.Enemy && !t.Dead) { FoePassives("guardBreak", t); Emit("foeShieldBreak", new EmitInfo { Target = t, By = owner?.Key ?? Acting, V = lost }); }
                            if (Over != null) return;
                        }
                        break;

                    // ── 증감 ──
                    case FxK.DealtMod:
                    case FxK.TakenMod:
                    case FxK.AtkMod:
                    case FxK.DefMod:
                    case FxK.CritMod:
                        {
                            string stat = FxK.ModStat(f.K);
                            string tg = f.Target ?? "auto";
                            if (tg == "auto") tg = (f.K == FxK.TakenMod && f.V > 0) || (f.K == FxK.DealtMod && f.V < 0) ? "oneEnemy" : "self";
                            foreach (var t in Resolve(ctx, tg == "party" ? "allAllies" : tg)) AddModFx(t, stat, f.V * ripenScale, f.Run ? R.BOON_TURNS : f.TurnsOr1, f.Run);   // 당겨 쓴 예약(ripen)이면 남은 칸만큼 증감도 준다
                            break;
                        }

                    // ── 자원 ──
                    case FxK.Draw:
                        if (HasFilter(f))
                        {   // 거르개에 맞는 카드를 뽑을 더미 위에서부터 — 없으면 버린 카드를 섞어 넣고 한 번 더 찾는다(카제나 규칙 4 · 이번 턴에 낸 카드는 빼고)
                            for (int k = 0; k < Math.Max(1, f.IV); k++)
                            {
                                int at = Draw.FindLastIndex(id => FxMatch(f, id, owner));
                                if (at < 0 && Reshuffle()) { Emit("shuffle", new EmitInfo()); if (Over != null) return; at = Draw.FindLastIndex(id => FxMatch(f, id, owner)); }
                                if (at < 0) break;
                                var id = Draw[at]; Draw.RemoveAt(at); Draw.Add(id);
                                DrawCards(1, true); if (Over != null) return;
                            }
                        }
                        else DrawCards(f.IV, true);
                        break;
                    case FxK.Ap:
                        if (BoundBlocks(ctx)) { Say($"구속 — AP {(f.IV > 0 ? "+" : "")}{f.IV} 을 받지 않는다"); break; }
                        if (f.IV > 0)
                        {   // 멈춤 방지 — 효과로 얻는 AP 는 한 턴 AP_GAIN_CAP 까지(게임 규칙 아님 · 엘다인 3차 ▶E46)
                            string ak = "apGain|" + Turn; int had = Counts.TryGetValue(ak, out var hv) ? hv : 0, give = Math.Min(f.IV, AP_GAIN_CAP - had);
                            if (give < f.IV && capMeter && had < AP_GAIN_CAP + 1) { System.Threading.Interlocked.Increment(ref ApCapHits); }
                            if (give <= 0) { Counts[ak] = AP_GAIN_CAP + 1; Say($"AP 얻기 한 턴 상한({AP_GAIN_CAP}) — 더 얻지 않는다"); break; }
                            Counts[ak] = had + give; GainAp(give);
                        }
                        else Ap = Math.Max(0, Ap + f.IV); break;
                    case FxK.NextCheaper: NextCheaper += f.IV; break;
                    case FxK.Gauge: if (f.IV > 0 && GaugeLocked(owner?.Key)) { Say("게이지 잠금 — 오르지 않는다"); break; } Gauge = Num.Clamp(Gauge + f.IV, 0, R.GAUGE_MAX); break;
                    case FxK.Discard: ctx.Discarded += DiscardFx(f.All ? -1 : f.IV, f.Random, HasFilter(f) ? f : null, owner); if (Over != null) return; break;
                    case FxK.Make:
                        {   // (엘다인 ▶E36) 앞에 비례(perEvent …)가 있으면 그 수만큼 — 0 이면 안 만든다
                            int mn = Math.Max(1, f.IV);
                            if (ctx.PerStack != null) { mn *= PerCount(ctx, ctx.PerStack); ctx.PerStack = null; if (mn <= 0) break; }
                            Make(f.Id, mn, owner, f.To, f.Owner);
                            break;
                        }

                    // ── 상태 · 강인도 · 즉시 행동 ──
                    case FxK.Tough:
                        {   // 비례 뒤면 그 수만큼(엘다인 4차 — 아야 「만년설」 1개당 강인도 피해)
                            double tv = f.V;
                            if (ctx.PerStack != null) { tv *= PerCount(ctx, ctx.PerStack); ctx.PerStack = null; }
                            if (tv > 0) foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy")) if (t.Side == Side.Enemy) ToughHit(t, tv);
                            break;
                        }
                    case FxK.RushDown: foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy")) if (t.Side == Side.Enemy) t.RushCnt -= f.IV; break;
                    case FxK.Status:
                        {
                            // 개인 층(구속)은 기본 「자신」, 나머지는 파티 층 하나(대상 「파티」 — 파티원 전원에게 든다)
                            bool mine = R.HERO_ST.Contains(f.Id);
                            string tg = f.Target ?? (mine ? (owner != null ? "self" : "allAllies") : R.IsBadSt(f.Id) ? "oneEnemy" : "party");
                            var ts = Resolve(ctx, mine && tg == "party" ? "allAllies" : tg);
                            foreach (var t in mine ? ts : Once(ts))
                            {
                                AddStatus(t, f.Id, f.IV, f.Turns, owner);
                                if (f.Id == "죽음의 낙인" && ctx.CardId != null && t.Side == Side.Enemy) t.Brand.Add(GameData.BaseId(ctx.CardId));
                            }
                            break;
                        }
                    case FxK.Cleanse:
                        foreach (var t in Once(Resolve(ctx, f.Target ?? "party")))
                            for (int i = 0; i < Math.Max(1, f.IV); i++) { var bad = R.BAD_ST.FirstOrDefault(b => St(t, b) > 0); if (bad != null) DelSt(t, bad); }
                        break;

                    // ── 키워드 · 공용 부품 ──
                    case FxK.Stack:
                        if (f.Pick && Kw.TryGetValue(f.Id, out var pkw) && pkw.Def.Cap > 0 && owner != null)
                        {   // 단계 고르기(▶E74) — 고른 단계(없으면 다음 단계)로 정한다 · lock 이면 turns 턴 동안 고정
                            int cur0 = StackOf(owner.Key, f.Id), cap0 = pkw.Def.Cap.Value;
                            int to = ctx.StagePick is int sp && sp >= 1 && sp <= cap0 ? sp : cur0 % cap0 + 1;
                            var pf = f.Copy(); pf.Pick = false; pf.Set = to; pf.Lock = false;
                            Counts.Remove("lock|" + owner.Key + "|" + f.Id);
                            StackFx(pf, ctx);
                            if (f.Lock) Counts["lock|" + owner.Key + "|" + f.Id] = Turn + f.TurnsOr1;
                            break;
                        }
                        if (f.Lock && owner != null && f.Set != null)
                        {   // 정하고 고정(▶E74)
                            Counts.Remove("lock|" + owner.Key + "|" + f.Id);
                            var lf = f.Copy(); lf.Lock = false; StackFx(lf, ctx);
                            Counts["lock|" + owner.Key + "|" + f.Id] = Turn + f.TurnsOr1;
                            break;
                        }
                        if (ctx.PerStack != null && !f.Carry && f.Set == null)
                        {   // 비례 뒤 쌓기 — 그 수 × v(엘다인 3차: 깎은 양 20당 「불꽃」 1)
                            int pk = PerCount(ctx, ctx.PerStack); ctx.PerStack = null;
                            if (pk <= 0) break;
                            var sf = f.Copy(); sf.V = f.V * pk; StackFx(sf, ctx);
                        }
                        else StackFx(f, ctx);
                        break;
                    case FxK.Cue: CueFx(f, ctx); break;
                    case FxK.Spend: SpendFx(f, ctx); break;
                    case FxK.SpendRhythm: RhythmAdd(f.All ? (int?)null : -f.IV); break;
                    case FxK.Flip: FlipFx(ctx); break;
                    case FxK.Hasten: Hasten(f.IV); break;

                    // ── HP 치르기 ──
                    case FxK.PayHp:
                        foreach (var t in Once(Resolve(ctx, f.Target == "self" && owner == null ? "party" : f.Target ?? "party"))) { int h0 = t.Hp; Hurt(t, f.IV, new HurtOpts { Pure = true, Pay = true }); if (t.Side == Side.Party) PaidHp += System.Math.Max(0, h0 - System.Math.Max(0, t.Hp)); }
                        Pay(ctx);
                        break;
                    case FxK.PayHpPct:
                        foreach (var t in Once(Resolve(ctx, f.Target == "self" && owner == null ? "party" : f.Target ?? "party"))) { int h0 = t.Hp; Hurt(t, Num.Round(t.MaxHp * f.V), new HurtOpts { Pure = true, Pay = true }); if (t.Side == Side.Party) PaidHp += System.Math.Max(0, h0 - System.Math.Max(0, t.Hp)); }
                        Pay(ctx);
                        break;

                    default:
                        if (!FxV2(f, ctx, ref gate) && !FxKit(f, ctx, ref gate) && !FxForm(f, ctx) && !FxPower(f, ctx) && !FxEldain(f, ctx, ref gate)) throw new InvalidOperationException($"모르는 효과 조각: {f.K}");
                        if (Over != null) return;
                        break;
                }
            }
        }

        // ── 효과의 손잡이 ──────────────────────────────────────────────
        /// <summary>표식 — 공격 카드가 표식 걸린 적을 처음 칠 때: 공격력 100% 덤 타격 + 강인도 1, 표식 1 감소.</summary>
        void Mark(Unit owner, Unit t, FxCtx ctx)
        {
            if (owner == null || t.Dead || ctx.Type != "공격" || St(t, "표식") <= 0) return;
            AddSt(t, "표식", -1);
            Say($"{t.Name}: 표식 — 덤 타격"); StatusCue(t, "표식!");
            int v = R.FinalDamage(AtkOf(owner), R.SV("표식") + Morale());
            Hurt(t, v, new HurtOpts { From = owner, Tags = ctx.HitTags, Card = true });
            if (!t.Dead) ToughHit(t, R.ToughExtra(IsWeakHit(owner, t, ctx.HitTags)));
            AfterExtra(owner);
        }

        /// <summary>잔광 — 이 공격 카드(한 번의 일)에 잔광이 붙었나. 처음 물으면 1 쓴다.</summary>
        /// <summary>카드 한 장의 강인도 피해 — 적 하나에 한 번(카제나 단위 — AP 1 당 1/3칸). dying 은 처치 일격(LethalKill). 격파했으면 true.</summary>
        bool CardTough(Unit owner, Unit t, FxCtx ctx, string dTarget, bool dying)
        {
            ctx.Toughed ??= new HashSet<Unit>();
            bool once = ctx.Toughed.Add(t);
            bool glow = once && ctx.Type == "공격" && GlowUse();
            if (!once) return false;
            bool weak = IsWeakHit(owner, t, ctx.HitTags);
            if (t.ToughMax > 0 && !t.Broken) { ToughHits++; if (weak) ToughWeakHits++; }
            bool area = dTarget == "allEnemies" || dTarget != null && dTarget.StartsWith("random", StringComparison.Ordinal);   // 광역 · 논타겟 1/2
            double td = ctx.ToughUnit > 0 ? R.ToughOf(ctx.ToughUnit, weak, area) : R.ToughDmg(ctx.Cost, weak, area);
            return ToughHit(t, td + (glow ? R.TOUGH.Glow : 0), dying);
        }

        bool GlowUse()
        {
            if (ActSeq == 0) return false;
            if (GlowSeq == ActSeq) return true;
            if (St(Pool, "잔광") <= 0) return false;
            AddSt(Pool, "잔광", -1); GlowSeq = ActSeq;
            StatusCue(PartyRep(), "잔광!", true);
            return true;
        }
        bool GlowOn() => ActSeq != 0 && GlowSeq == ActSeq;

        /// <summary>카드 만들기 — 이 전투의 손에(가득 차면 버린 더미). 만든 카드는 맨 카드(신탁 · 축복 없음).</summary>
        void Make(string cardId, int n, Unit owner, string to = null, string ownerSpec = null)
        {
            var c = Data.Card(cardId);
            if (c == null) { Say($"(만들 카드가 없다: {cardId})"); return; }
            // 만든 카드의 주인(1단계) — self(기본: 카드 데이터의 주인) · other(파티 차례로 만든 사도 다음 아군) · 사도 id. 주인이 바뀐 카드는 「카드@사도~」
            string ok = ownerSpec == null || ownerSpec == "self" ? null : ownerSpec == "other" ? NextAlly(owner)?.Key
                : ownerSpec == "strongestAlly" ? AliveParty().OrderByDescending(AtkNow).ThenBy(u => u.Idx).FirstOrDefault()?.Key   // (엘다인 ▶E23) 공격력이 가장 높은 아군(자신 포함)
                : HeroUnit(ownerSpec) is Unit hu && !hu.Dead ? ownerSpec : null;
            var made = (ok != null && ok != c.Hero ? GameData.WithOwner(c.Id, ok) : c.Id) + GameData.PLAIN;
            if (ok != null && ok != c.Hero) Say($"「{c.Name}」 — 주인 {HeroUnit(ok)?.Name}");
            for (int k = 0; k < n; k++)
            {
                if (to == "draw") { Draw.Insert(Rng.Int(Draw.Count + 1), made); CardCue(made, "new", "draw", "make"); }
                else if (to == "top") { Draw.Add(made); CardCue(made, "new", "draw", "make"); }
                else if (to == "discard" || Hand.Count >= R.HAND_MAX) { Discard.Add(made); CardCue(made, "new", "discard", "make"); }
                else { Hand.Add(made); CardCue(made, "new", "hand", "make"); }
            }
            Say($"「{c.Name}」 {n}장 — {(to == "draw" ? "뽑을 더미 무작위 자리로" : to == "top" ? "뽑을 더미 맨 위로" : to == "discard" ? "버린 더미로" : "손으로")}");
            if (to == null || to == "hand") MergePile(Hand, "hand"); StatusCue(owner ?? AliveParty().FirstOrDefault(), $"「{c.Name}」 +{n}", true);
            Emit("make", new EmitInfo { By = owner?.Key ?? Acting, Id = c.Id, N = n, Seq = ActSeq });
        }

        /// <summary>파티 차례로 그 사도 다음의 산 아군(없으면 null).</summary>
        Unit NextAlly(Unit from)
        {
            var live = AliveParty().ToList();
            if (live.Count < 2) return null;
            int i = from != null ? live.IndexOf(from) : -1;
            for (int k = 1; k <= live.Count; k++) { var u = live[((i < 0 ? 0 : i) + k) % live.Count]; if (u != from) return u; }
            return null;
        }

        /// <summary>소모량을 고르는 카드(spend pick)의 후보 — 1 · 절반 · 전부(겹친 것은 하나로). 그런 카드가 아니거나 가진 것이 없으면 빈 목록.</summary>
        public List<int> SpendChoices(string cardId)
        {
            var c = CardOf(cardId);
            var f = c?.Fx.FirstOrDefault(x => x.K == FxK.Spend && x.Pick);
            if (f == null || c.Hero == null) return new List<int>();
            int have = StackOf(c.Hero, f.Id);
            if (have <= 0) return new List<int>();
            return new[] { 1, (have + 1) / 2, have }.Distinct().ToList();
        }

        /// <summary>카드로 처음 칠 때 — 충격(공격 카드의 대상이 되면 고정 피해 80%, 방어 · 실드가 있으면 +50%) · 충격파(다른 모든 적에게 고정 피해 300%).</summary>
        void CardHit(Unit owner, Unit t, FxCtx ctx)
        {
            if (t.Dead) return;
            if (ctx.Type == "공격" && St(t, "충격") > 0)
            {
                bool guarded = t.Block > 0 || t.Shield > 0;
                int v = Math.Max(1, Num.Round(DotUnit(t, "충격") * R.SV("충격") * (guarded ? 1 + R.SV("충격Shield") : 1)));
                AddSt(t, "충격", -1);
                Say($"{t.Name}: 충격 — 고정 피해 {v}"); StatusCue(t, "충격!");
                Hurt(t, v, new HurtOpts { From = owner, Fixed = true });
            }
            if (!t.Dead && St(t, "충격파") > 0)
            {
                int v = Math.Max(1, Num.Round(DotUnit(t, "충격파") * R.SV("충격파")));
                AddSt(t, "충격파", -1);
                Say($"{t.Name}: 충격파 — 다른 적 모두 고정 피해 {v}"); StatusCue(t, "충격파!");
                foreach (var x in AliveEnemies()) if (x != t) { Hurt(x, v, new HurtOpts { From = owner, Fixed = true }); if (Over != null) return; }
            }
        }

        /// <summary>상태를 건다 — 면역 · 기절(적의 다음 차례를 막는다, 보스는 두 턴에 한 번) · 리듬(파티) · 지속 피해의 바탕.</summary>
        void AddStatus(Unit t, string id, int v, int turns, Unit by)
        {
            int n = Math.Max(1, v);
            if (id == R.RHYTHM) { if (t.Side == Side.Party) RhythmAdd(n); return; }
            if (v > 0 && R.IsBadSt(id) && St(t, "면역") > 0)
            {
                AddSt(t, "면역", -1); t.Body.ImmuneHit++;
                Say($"{(t.Side == Side.Party ? "파티" : t.Name)}: 면역 — {id} 를 막았다"); StatusCue(t, "면역!", t.Side == Side.Party);
                return;
            }
            bool fresh = t.Side == Side.Enemy && (id == R.STUN ? !t.Sealed : St(t, id) <= 0);
            if (id == R.STUN)
            {
                if (t.Side != Side.Enemy) return;
                if (t.Boss && (t.Sealed || t.StunGuard > 0)) Say($"{t.Name}: 기절을 버텨 냈다");
                else { t.Sealed = true; if (t.Boss) t.StunGuard = 2; Say($"{t.Name}: 기절"); }
            }
            else
            {
                if (id == "고통" && v > 0 && St(t, "고통 각인") > 0) n += (int)R.SV("고통 각인");
                AddSt(t, id, n, SrcOf(by));
                if ((id == "협공" || id == "반격") && by != null && by.Side == Side.Party) t.Body.Giver[id] = by.Idx;
                // 지속 피해(고통 · 균열)를 건 사도 — 틱 피해의 약점은 그 사도 성격으로 본다(2026-10-10 카제나). 여럿이 쌓으면 마지막에 건 사도
                if ((id == "고통" || id == "균열") && v > 0 && by != null && by.Side == Side.Party && t.Side == Side.Enemy) t.Body.Giver[id] = by.Idx;
                if (R.IsUnitSt(id)) SetUnit(t, id, by != null && by.Side == Side.Party ? AtkNow(by) : t.Side == Side.Party && R.IsBadSt(id) ? R.FOE_DOT * EnemyDmgx : PartyAtk(null));
            }
            if (v > 0 && (id != R.STUN || t.Sealed)) StatusCue(t, id, t.Side == Side.Party);
            if (t.Side == Side.Enemy && v > 0 && !R.BUFF_ST.Contains(id))
            {
                Emit("debuff", new EmitInfo { By = Acting, Target = t, Id = id, Seq = ActSeq, Fresh = fresh });
                FoePassives("debuffed", t);
            }
        }

        static readonly Dictionary<string, string> MOD_KO = new() { ["dealt"] = "주는 피해", ["taken"] = "받는 피해", ["atk"] = "공격력", ["def"] = "방어력", ["crit"] = "치명", ["heal"] = "치유", ["guard"] = "주는 실드" };

        void AddModFx(Unit t, string stat, double v, int turns, bool run)
        {
            bool boon = run && t.Side == Side.Party;
            AddMod(t, stat, v, boon ? R.BOON_TURNS : turns, ModSrc, boon);
            int pct = Num.Round(v * 100);
            if (pct != 0) StatusCue(t, $"{MOD_KO[stat]} {(pct > 0 ? "+" : "")}{pct}%", stat == "taken" ? pct < 0 : pct > 0);
            if (t.Side == Side.Enemy && ((stat == "taken" && v > 0) || (stat == "dealt" && v < 0)))
            {
                Emit("debuff", new EmitInfo { By = Acting, Target = t, Id = stat, Seq = ActSeq });
                FoePassives("debuffed", t);
            }
        }

        /// <summary>증감을 건다. 사도의 「전투 내내 공격력 +N%」 는 사도마다 +150% 까지.</summary>
        public static void AddMod(Unit u, string stat, double v, int turns, string src, bool run)
        {
            if (run && stat == "atk" && v > 0 && u.Side == Side.Party)
            {
                double has = u.Mods.Where(m => m.Run && m.Stat == "atk" && m.V > 0).Sum(m => m.V);
                v = Math.Min(v, R.MORALE_ATK * R.SV("사기Max") - has);
                if (v <= 1e-9) return;
            }
            u.Mods.Add(new Mod { Stat = stat, V = v, Left = turns <= 0 ? 1 : turns, Src = src, Run = run });
        }

        /// <summary>버리기 — 낸 사람이 고른 카드부터, 무작위면 무작위로, 고른 것이 없으면 손 끝에서부터. n &lt; 0 이면 전부.</summary>
        /// <summary>손패 소멸시키기 — 버리기처럼 고른 카드부터(PlayOpts.Discard), 이 전투에서 사라진다. 「카드가 소멸하면」 을 깨운다.</summary>
        void BurnFx(int n, bool random, FxCtx ctx, Fx filter = null)
        {
            // 카드 거르개(엘다인 — 「손의 「마탄」 1장」) — 맞는 카드만 고르고 센다
            bool Ok(string id) => filter == null || FxMatch(filter, id, ctx.Owner);
            int fit = filter == null ? Hand.Count : Hand.Count(Ok);
            int many = n < 0 ? fit : System.Math.Min(n, fit);
            var pick = !random ? discardPick : null;
            for (int i = 0; i < many && Hand.Count > 0; i++)
            {
                int at = -1;
                while (at < 0 && pick != null && pick.Count > 0) { at = Hand.IndexOf(pick[0]); pick.RemoveAt(0); if (at >= 0 && !Ok(Hand[at])) at = -1; }
                if (at < 0) at = PickAt(random, Ok);
                if (at < 0) break;
                var id = Hand[at]; Hand.RemoveAt(at);
                Say($"「{CardOf(id).Name}」 — 태워 없앴다");
                Exile(id, "hand", "burn");
                Emit("exhaust", new EmitInfo { Hero = CardOf(id).Hero, By = Acting, Id = id });
                if (Over != null) return;
            }
        }

        /// <summary>버리기 — 버린 장수를 돌려준다.</summary>
        int DiscardFx(int n, bool random, Fx filter = null, Unit owner = null)
        {
            // 카드 거르개(엘다인 — 「손의 「마탄」 모두 버림」) — 맞는 카드만 고르고 센다
            bool Ok(string id) => filter == null || FxMatch(filter, id, owner);
            int fit = filter == null ? Hand.Count : Hand.Count(Ok);
            int many = n < 0 ? fit : Math.Min(n, fit);
            var pick = !random ? discardPick : null;
            var outs = new List<string>();
            for (int i = 0; i < many && Hand.Count > 0; i++)
            {
                int at = -1;
                while (at < 0 && pick != null && pick.Count > 0) { at = Hand.IndexOf(pick[0]); pick.RemoveAt(0); if (at >= 0 && !Ok(Hand[at])) at = -1; }
                if (at < 0) at = PickAt(random, Ok);
                if (at < 0) break;
                var id = Hand[at]; Hand.RemoveAt(at);
                Discard.Add(id); outs.Add(id); CardCue(id, "hand", "discard", "discard");
            }
            DiscardedTurn += outs.Count;
            // 안식 — 효과로 버려진 카드의 「안식: …」, 그리고 「카드가 버려지면」(버리기형 공용 계기)
            foreach (var id in outs)
            {
                if (CardOf(id) is CardView c && c.Fx.Any(f => f.K == FxK.When && f.On == "discard")) { CardWhen(id, "discard"); if (Over != null) return outs.Count; }
                BlessDrop(id);
                BulletDrop(id);
                if (Over != null) return outs.Count;
                Emit("discard", new EmitInfo { Hero = CardOf(id)?.Hero, By = Acting, Id = id });
                if (Over != null) return outs.Count;
                Resonance();
                if (Over != null) return outs.Count;
            }
            return outs.Count;
        }

        /// <summary>버리기 · 태우기의 다음 자리 — 무작위면 맞는 카드 가운데 무작위, 아니면 손 끝에서부터 맞는 카드. 없으면 -1.</summary>
        int PickAt(bool random, Func<string, bool> ok)
        {
            if (random)
            {
                var c = new List<int>();
                for (int i = 0; i < Hand.Count; i++) if (ok(Hand[i])) c.Add(i);
                return c.Count == 0 ? -1 : c[Rng.Int(c.Count)];
            }
            for (int i = Hand.Count - 1; i >= 0; i--) if (ok(Hand[i])) return i;
            return -1;
        }

        /// <summary>대가를 치렀다 — 카드(한 번의 일) 하나에 한 번 「HP를 치르면」.</summary>
        void Pay(FxCtx ctx)
        {
            if (ctx.Paid) return;
            ctx.Paid = true;
            Emit("pay", new EmitInfo { By = Acting });
        }

        // ── 키워드 ─────────────────────────────────────────────────────
        static readonly string[] FOE_T = { "oneEnemy", "allEnemies", "randomEnemy", "topEnemy", "lowEnemy", "farEnemy" };

        void StackFx(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null) return;
            if (StackLocked(owner.Key, f.Id)) { Say($"「{f.Id}」 — 고정돼 움직이지 않는다"); return; }   // (▶E74)
            if (f.Carry)
            {   // (엘다인 ▶E4) 표식 옮기기 — v 대신 일을 겪은(쓰러진) 적의 그 표식 겹
                int n = ctx.Holder != null ? St(ctx.Holder, f.Id) : 0;
                if (n <= 0) return;
                f = f.Copy(); f.Carry = false; f.V = n;
            }
            if (f.Set != null)
            {   // (엘다인 ▶E28) 그 값으로 정한다 — 지금 겹과의 차이만큼 쌓는다(최대 · 0 안으로). 자기 주머니(self) · 아군 표시(자신)
                int cur = StackOf(owner.Key, f.Id);
                int to = Math.Max(0, (int)f.Set.Value);
                if (Kw.TryGetValue(f.Id, out var sk0) && sk0.Def.CapOrMode is int cp) to = Math.Min(to, cp);
                if (to == cur) return;
                f = f.Copy(); f.Set = null; f.V = to - cur;
            }
            if (f.OfEvent > 0)
            {   // 일의 값을 저장 — 받은 피해 · 준 피해 × ofEvent 만큼(이드(재활) 꿈 · 키샤 공연)
                int n = (int)Math.Floor(ctx.EventV * f.OfEvent);
                if (n <= 0) return;
                f = f.Copy(); f.OfEvent = 0; f.V = n;
            }
            GainCount(owner.Key, f.Id, f.IV);   // 이번 전투에 쌓은 양(시범 16 — ifGained · 조건 gained). 최대에 막힌 몫도 「쌓으려 한 노력」 으로 센다
            Kw.TryGetValue(f.Id, out var kw);
            if (kw != null && kw.Carrier == "hero")
            {   // 사도에게 붙는 사도 표시(지정 아군 · 제자 …) — 가리킨 사도마다 따로
                foreach (var t in Resolve(ctx, f.Target ?? "self").Where(u => u.Side == Side.Party))
                {
                    if (kw.Def.Burn != null && t.Key == kw.Owner && f.IV > 0) { Say($"「{f.Id}」 — 주인 자신에게는 붙지 않는다"); continue; }   // 벨라 촛불은 동료에게만
                    // 한 번에 한 아군(hunt) — 새 아군에게 붙이면 다른 아군의 표시는 사라진다(옮겨 간다)
                    if (kw.Def.Hunt && f.IV > 0)
                        foreach (var o in Party)
                            if (o != t && StackOf(o.Key, f.Id) > 0) { int ob = StackOf(o.Key, f.Id); AddStack(o.Key, f.Id, -ob); Say($"「{f.Id}」 — {o.Name}에게서 {t.Name}에게로 옮긴다"); StackChanged(kw.Owner, f.Id, ob, 0, o); }
                    int before = StackOf(t.Key, f.Id);
                    AddStack(t.Key, f.Id, f.IV);
                    if (Meter != null) MeterGain(f.Id, f.IV, StackOf(t.Key, f.Id) - before, StackOf(t.Key, f.Id));
                    StackChanged(kw.Owner, f.Id, before, StackOf(t.Key, f.Id), t);
                }
                return;
            }
            if (kw != null && kw.Carrier != "self")
            {
                string tg = f.Target != null && f.Target != "auto" ? f.Target : kw.Carrier == "enemy" ? "oneEnemy" : "party";
                if (kw.Carrier == "ally" && FOE_T.Contains(tg)) tg = "self";
                if (kw.Carrier == "enemy" && !FOE_T.Contains(tg)) tg = "oneEnemy";
                foreach (var t in Once(Resolve(ctx, tg)))
                {
                    if (kw.Def.Hunt)
                        foreach (var o in Enemies) if (o != t && St(o, f.Id) > 0) { SetStRaw(o, f.Id, 0); Say($"「{f.Id}」 — {o.Name}에게서 {t.Name}에게로 옮긴다(처음부터)"); StatusCue(o, $"「{f.Id}」 옮김"); }
                    int left = f.IV, got = 0, peak = 0;
                    for (int guard = 0; guard < 20; guard++)
                    {
                        int before = St(t, f.Id);
                        int next = Math.Max(0, before + left);
                        if (kw.Def.CapOrMode != null) next = Math.Min(kw.Def.CapOrMode.Value, next);
                        SetStRaw(t, f.Id, next);
                        got += next - before; peak = Math.Max(peak, next);
                        StackChanged(owner.Key, f.Id, before, next, t);
                        left -= next - before;
                        if (left <= 0 || next == before || St(t, f.Id) >= next) break;
                    }
                    if (Meter != null) MeterGain(f.Id, f.IV, got, peak);
                    if (left > 0 && f.IV > 0 && !kw.Def.Mode && Over == null) StackOver(owner.Key, f.Id, left, t);
                }
            }
            else
            {
                int left = f.IV, got = 0, peak = 0;
                for (int guard = 0; guard < 20; guard++)
                {
                    int before = StackOf(owner.Key, f.Id);
                    AddStack(owner.Key, f.Id, left);
                    int next = StackOf(owner.Key, f.Id);
                    got += next - before; peak = Math.Max(peak, next);
                    if (f.IV > 0 && next < before && (kw?.Def.Wrap ?? false))
                    {   // (엘다인 ▶E25 · E33) 순환이 한 바퀴 돌아 앞 단계로 — 「N이 되면」 이 그 단계 진입에도 돈다(앞을 0 으로 친다)
                        StatusCue(owner, $"{f.Id} → {next}", true);
                        Emit("stackReach", new EmitInfo { Id = f.Id, Before = 0, After = next, Owner = kw.Owner, Target = owner });
                        if (Over == null && kw.Def.OnStage != null) StageFx(f.Id, next);
                    }
                    else StackChanged(owner.Key, f.Id, before, next, owner);
                    left -= next - before;
                    if (left <= 0 || next == before || StackOf(owner.Key, f.Id) >= next) break;
                }
                if (Meter != null) MeterGain(f.Id, f.IV, got, peak);
                if (left > 0 && f.IV > 0 && !(kw?.Def.Mode ?? false) && !(kw?.Def.Wrap ?? false) && Over == null) StackOver(owner.Key, f.Id, left, owner);
            }
        }

        /// <summary>
        /// 「X」가 최대에서 넘치면(stackOver) — 쌓으려던 몫이 최대에 막혀 남았다. 일의 값 = 넘친 수(perEvent 가 센다).
        /// 넘친 몫을 다른 이득으로 바꾸는 규칙에 쓴다(2026-10-05 스택형 점검 — 디아나(왕년) 「바위 던지기」 …). 적 표식이면 그 적이 「적 1명」.
        /// </summary>
        void StackOver(string ownerKey, string id, int over, Unit holder)
        {
            Say($"「{id}」 — 최대라 {over} 넘친다");
            Emit("stackOver", new EmitInfo { Id = id, Owner = Kw.TryGetValue(id, out var k) ? k.Owner : ownerKey, N = over, V = over, Target = holder });
        }

        /// <summary>
        /// 연출 쪽지 — 쪽지 K="fx", Id = cue id, Hero = 주인, V = 수(xStack 키워드의 지금 겹을 n · max 안으로, 없으면 v).
        /// 효과는 없다. 이펙트가 이 순간에 조각을 붙인다(미로 거울 숨기 · 광선 발사 …).
        /// </summary>
        void CueFx(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null || f.Id == null) return;
            int v = f.XStack != null ? StackOf(owner.Key, f.XStack) : f.IV;
            if (f.Max > 0) v = Math.Min(v, f.Max);
            if (f.N > 0) v = Math.Max(v, f.N);
            Cue("fx", owner, new Cue { Id = f.Id, Hero = owner.Key, V = v });
        }

        void SpendFx(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null) return;
            if (!Kw.ContainsKey(f.Id ?? "") && R.ALL_ST.Contains(f.Id ?? ""))
            {   // 상태 쓰기(엘다인 3차 — 비비 「반격을 모두 써서」) — 파티 상태(개인 층이면 그 사도), 쓴 수가 일의 값
                var hold = R.IsHeroSt(f.Id) ? owner : Pool;
                int have = St(hold, f.Id), take = f.All ? have : Math.Min(have, Math.Max(0, f.IV));
                if (take > 0) { if (take >= have) DelSt(hold, f.Id); else AddSt(hold, f.Id, -take); Say($"{f.Id} {take} 을 쓴다"); }
                ctx.EventV = take;
                return;
            }
            Kw.TryGetValue(f.Id, out var kw);
            if (kw != null && kw.Carrier == "hero")
            {   // 사도 표시 — 그 일을 겪은 아군(없으면 가리킨 사도 · 자신)에게서
                var hs = f.Who == "any" ? Party.Where(u => StackOf(u.Key, f.Id) > 0).ToList()   // 동료 누구에게 붙어 있든(벨라 존속)
                    : ctx.Ally != null && f.Target == null ? new List<Unit> { ctx.Ally } : Resolve(ctx, f.Target ?? "self").Where(u => u.Side == Side.Party).ToList();
                int spentAll = 0;
                foreach (var t in hs)
                {
                    int b0 = StackOf(t.Key, f.Id); if (b0 <= 0) continue;
                    spentAll += Math.Min(b0, f.All ? b0 : f.IV);
                    AddStack(t.Key, f.Id, f.All ? -b0 : -f.IV);
                    MeterSpend(f.Id, b0 - StackOf(t.Key, f.Id));
                    StackChanged(kw.Owner, f.Id, b0, StackOf(t.Key, f.Id), t);
                    Emit("spend", new EmitInfo { Owner = kw.Owner, Id = f.Id, N = b0 - StackOf(t.Key, f.Id), By = Acting, Seq = ActSeq });
                }
                ctx.EventV = spentAll;   // 쓴 양이 일의 값(뒤 perEvent)
                return;
            }
            if (kw != null && kw.Carrier != "self")
            {
                var holders = ctx.Holder != null ? new List<Unit> { ctx.Holder } : Resolve(ctx, kw.Carrier == "enemy" ? "oneEnemy" : "self");
                foreach (var t in holders)
                {
                    int before = St(t, f.Id);
                    if (before <= 0) continue;
                    int after = f.All ? 0 : Math.Max(0, before - f.IV);
                    SetStRaw(t, f.Id, after);
                    MeterSpend(f.Id, before - after);
                    StackChanged(owner.Key, f.Id, before, after, t);
                    if (before > after) Emit("spend", new EmitInfo { Owner = kw.Owner, Id = f.Id, N = before - after, By = Acting, Seq = ActSeq, Target = t.Side == Side.Enemy ? t : null });
                }
            }
            else
            {
                int before = StackOf(owner.Key, f.Id);
                // 소모량 고르기(pick) — 고른 수(1 ~ 가진 수, 안 고르면 전부). 소모한 양은 일의 값(perEvent)으로
                int take = f.Pick ? (before <= 0 ? 0 : Math.Max(1, Math.Min(before, ctx.SpendPick ?? before))) : f.All ? before : f.IV;
                AddStack(owner.Key, f.Id, -take);
                int after2 = StackOf(owner.Key, f.Id);
                if (f.Pick || f.All) ctx.EventV = before - after2;   // 쓴 양 = 일의 값(전부 써도 — 엘다인 3차)
                if (f.Pick && owner.Key == TraceHero) { TraceAdd("pick"); if (after2 > 0) TraceAdd("pickPartial"); }   // (▶E30) 고른 만큼에서 전부가 아닌 수
                if (f.Pick && f.Loop) { ctx.LoopId = f.Id; ctx.LoopMax = f.Max > 0 ? f.Max : 12; ctx.LoopKeep = after2; }   // (엘다인 ▶E19) 이어 쓰기 — 남겨 둔 몫 위로 새로 얻은 것만
                MeterSpend(f.Id, before - after2);
                StackChanged(owner.Key, f.Id, before, after2, owner);
                if (before > after2) Emit("spend", new EmitInfo { Owner = Kw.TryGetValue(f.Id, out var sk) ? sk.Owner : owner.Key, Id = f.Id, N = before - after2, By = Acting, Seq = ActSeq });
            }
        }

        /// <summary>전환 — 카드 주인의 모드 키워드를 뒤집는다(있으면 0, 없으면 1).</summary>
        void FlipFx(FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null) return;
            var kw = Kw.Values.FirstOrDefault(k => k.Def.Mode && k.Owner == owner.Key);
            if (kw == null) return;
            var hs = kw.Carrier == "self" ? new List<Unit> { owner }
                : kw.Carrier == "enemy" ? (ctx.Holder != null && ctx.Holder.Side == Side.Enemy ? new List<Unit> { ctx.Holder } : Resolve(ctx, "oneEnemy"))
                : Once(Resolve(ctx, "self"));
            foreach (var t in hs)
            {
                int before = kw.Carrier == "self" ? StackOf(owner.Key, kw.Id) : St(t, kw.Id);
                int after = before > 0 ? 0 : 1;
                if (kw.Carrier == "self") AddStack(owner.Key, kw.Id, after - before);
                else SetStRaw(t, kw.Id, after);
                StackChanged(owner.Key, kw.Id, before, after, t);
            }
        }

        void StackChanged(string ownerKey, string id, int before, int after, Unit holder)
        {
            if (after > before)
            {
                StatusCue(holder, $"{id} +{after - before}", true);
                Emit("stackReach", new EmitInfo { Id = id, Before = before, After = after, Owner = ownerKey, Target = holder });
                if (Kw.TryGetValue(id, out var kw) && kw.Def.Mode && before <= 0 && Over == null) KwSwitch(kw, holder);
                if (kw != null && kw.Def.OnMax != null && Over == null) FormOnMax(kw, before, after);
                if (Forms.Count > 0 && Over == null) FormStackMake(id, before, after);
                if (Over == null && Kw.TryGetValue(id, out var sk2) && sk2.Def.OnStage != null) StageFx(id, after);   // 순환 칸 덤(▶E73)
            }
            else if (before > 0 && after <= 0) KwGone(id, Kw.TryGetValue(id, out var k2) ? k2.Owner : ownerKey, holder, false);
        }

        static bool KwUses(PerStat p) => p.Stat == "dealt" || p.Stat == "atk" || p.Stat == "crit" || p.Stat == "taken";

        /// <summary>키워드의 「발동하면 사라진다 · N 감소」 — 1개당 덤이 카드 한 장에 쓰였으면 줄인다.</summary>
        void KwConsume(Unit owner, CardView c)
        {
            foreach (var kw in Kw.Values.ToList())
            {
                var d = kw.Def;
                if (!d.Consumes || !d.Per.Any(KwUses)) continue;
                if (d.Per.All(p => p.Stat == "crit") && CritSeq != ActSeq) continue;   // 치명만 올리는 키워드는 치명타가 터졌을 때
                int Cut(int n) => d.ConsumeAll ? 0 : Math.Max(0, n - d.Consume);
                if (kw.Carrier == "self")
                {
                    if (owner == null || owner.Key != kw.Owner || c.Type != "공격") continue;
                    int n = StackOf(kw.Owner, kw.Id);
                    if (n > 0)
                    {
                        Stacks[kw.Owner][kw.Id] = Cut(n);
                        int left = Stacks[kw.Owner][kw.Id];
                        MeterSpend(kw.Id, n - left);
                        Say($"「{kw.Id}」 — 발동해 {(left > 0 ? $"{left} 남는다" : "사라진다")}");
                        if (left == 0) KwGone(kw.Id, kw.Owner, owner, false);
                    }
                }
                else if (kw.Carrier == "enemy")
                {
                    foreach (var e in Enemies.ToList())
                        if (e.HitSeq == ActSeq && St(e, kw.Id) > 0)
                        {
                            int left = Cut(St(e, kw.Id));
                            MeterSpend(kw.Id, St(e, kw.Id) - left);
                            SetStRaw(e, kw.Id, left);
                            if (left == 0) KwGone(kw.Id, kw.Owner, e, false);
                        }
                }
                else if (owner != null && c.Type == "공격" && St(Pool, kw.Id) > 0)
                {
                    int left = Cut(St(Pool, kw.Id));
                    MeterSpend(kw.Id, St(Pool, kw.Id) - left);
                    SetStRaw(Pool, kw.Id, left);
                    if (left == 0) KwGone(kw.Id, kw.Owner, PartyRep(), false);
                }
            }
        }

        /// <summary>「다른 사도의 카드를 내면 전부 사라진다」 — 그 카드의 효과보다 먼저.</summary>
        void KwWipe(CardView c)
        {
            foreach (var kw in Kw.Values.ToList())
            {
                if (!kw.Def.Wipe || c.Hero == kw.Owner) continue;
                if (kw.Carrier == "self")
                {
                    if (StackOf(kw.Owner, kw.Id) > 0) { Stacks[kw.Owner][kw.Id] = 0; Say($"「{kw.Id}」 — 다른 카드가 끼어 사라진다"); KwGone(kw.Id, kw.Owner, HeroUnit(kw.Owner), false); }
                }
                else
                {
                    var holders = new List<Unit> { PartyRep() }; holders.AddRange(Enemies);
                    foreach (var u in holders)
                        if (u != null && St(u, kw.Id) > 0) { SetStRaw(u, kw.Id, 0); Say($"「{kw.Id}」 — 다른 카드가 끼어 사라진다"); KwGone(kw.Id, kw.Owner, u, false); }
                }
                if (Over != null) return;
            }
        }

        /// <summary>키워드 겹이 0 이 됐다 — 「사라지면」(decay 면 「다 닳으면」 도) · 모드면 전환 · 예약이 다 닳으면.</summary>
        void KwGone(string id, string ownerKey, Unit holder, bool decay)
        {
            if (Over != null || (holder != null && holder.Side == Side.Enemy && holder.Dead)) return;
            Emit("stackGone", new EmitInfo { Id = id, Owner = ownerKey, Target = holder, Decay = decay });
            Kw.TryGetValue(id, out var kw);
            if (kw != null && kw.Def.Mode) KwSwitch(kw, holder);
            if (decay && kw != null && kw.Def.Reserve && Over == null) Emit("reserveGone", new EmitInfo { Id = id, Owner = ownerKey, Target = holder });
        }

        void KwSwitch(KwRt kw, Unit holder)
        {
            if (Over != null) return;
            SwitchTurn = Turn;
            bool on = kw.Carrier == "self" ? StackOf(kw.Owner, kw.Id) > 0 : holder != null && St(holder, kw.Id) > 0;
            var who = HeroUnit(kw.Owner);
            Say($"{(who != null ? who.Name + " " : "")}「{kw.Id}」 — 전환({(on ? "켜짐" : "꺼짐")})");
            StatusCue(who ?? holder, $"전환 「{kw.Id}」", true);
            Emit("switch", new EmitInfo { Id = kw.Id, Owner = kw.Owner, Target = holder, On = on });
        }

        /// <summary>리듬 — 파티 상태 하나. n 만큼(null 이면 전부 지운다). 오르면 「리듬이 N이 되면」.</summary>
        void RhythmAdd(int? n)
        {
            int before = St(Pool, R.RHYTHM);
            if (n == null) Pool.Status.Remove(R.RHYTHM);
            else AddSt(Pool, R.RHYTHM, n.Value);
            int after = St(Pool, R.RHYTHM);
            if (after == before) return;
            Say($"리듬 {(after > before ? "+" : "")}{after - before} ({after})");
            if (after > before)
            {
                StatusCue(PartyRep(), $"리듬 +{after - before}", true);
                Emit("rhythm", new EmitInfo { Before = before, After = after });
            }
        }

        /// <summary>재촉 N — 파티가 가진 모든 예약 키워드를 N 씩 줄인다. 0 이 되면 다 닳은 것.</summary>
        void Hasten(int n)
        {
            var gone = new List<(KwRt kw, Unit holder)>();
            foreach (var kw in Kw.Values.ToList())
            {
                if (!kw.Def.Reserve) continue;
                if (kw.Carrier == "self")
                {
                    int have = StackOf(kw.Owner, kw.Id);
                    if (have > 0) { Stacks[kw.Owner][kw.Id] = Math.Max(0, have - n); if (Stacks[kw.Owner][kw.Id] == 0) gone.Add((kw, HeroUnit(kw.Owner))); }
                }
                else if (kw.Carrier == "hero")
                {   // 아군에게 심은 예약(캬롯 씨앗 — 시범 16)도 재촉이 줄인다
                    foreach (var hu in Party)
                    {
                        int have = StackOf(hu.Key, kw.Id);
                        if (have > 0) { Stacks[hu.Key][kw.Id] = Math.Max(0, have - n); if (Stacks[hu.Key][kw.Id] == 0) gone.Add((kw, hu)); }
                    }
                }
                else
                {
                    var holders = new List<Unit> { PartyRep() }; holders.AddRange(AliveEnemies());
                    foreach (var u in holders)
                        if (u != null && St(u, kw.Id) > 0)
                        {
                            int left = Math.Max(0, St(u, kw.Id) - n);
                            SetStRaw(u, kw.Id, left);
                            if (left == 0) gone.Add((kw, u));
                        }
                }
            }
            Say($"재촉 {n} — 예약이 {n} 씩 줄어든다{(gone.Count > 0 ? " · 다 닳음 " + string.Join(" ", gone.Select(g => $"「{g.kw.Id}」")) : "")}");
            foreach (var g in gone) { KwGone(g.kw.Id, g.kw.Owner, g.holder, true); if (Over != null) return; }
        }
    }
}
