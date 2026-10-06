using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 카드 한 장의 대충의 값어치(웹판 tools/lib/card-value.js). 단위: 「1코 공격 한 명 120%」 ≈ 1. 코스트 c 의 기준 값어치 = 0.5 + c.
    /// 봇의 손 · 신탁 고르기 · 상점 · 검사가 쓴다. 정밀한 값이 아니다 — 크게 어긋난 카드를 찾는 체다.
    /// </summary>
    public static class CardValue
    {
        public static readonly Dictionary<string, double> STATUS_VAL = new()
        {
            ["사기"] = 0.6, ["불굴"] = 0.5, ["취약"] = 0.25, ["약화"] = 0.25, ["고통"] = 0.13, ["손상"] = 0.15, ["표식"] = 0.8, ["결의"] = 0.5, ["결정화"] = 0.32, ["반격"] = 0.4,
            ["잔불"] = 0.12, ["잔광"] = 0.4, ["피해 감소"] = 0.15, ["면역"] = 0.3, ["실드 유지"] = 0.25, ["저장"] = 0.3, ["협공"] = 0.6, ["균열"] = 0.2, ["고동"] = 0.5,
            ["그을림"] = 0.25, ["충격"] = 0.35, ["충격파"] = 0.6, ["리듬"] = 0.1,
            // 키워드 사전(2026-10-05)
            ["회피"] = 0.35, ["공명"] = 0.35, ["탄성"] = 0.2, ["칼날 벼리기"] = 0.3, ["빙벽"] = 0.6, ["구속"] = 0.1, ["형상 강화"] = 0.2, ["행동 둔화"] = 0.2,
            ["급속"] = -0.15, ["근면"] = 0.4, ["계몽"] = 0.3, ["집중"] = 0.3, ["다음 턴 드로우"] = 0.35, ["초재생"] = 0.6, ["절대 무적"] = 1.2, ["실드 보존"] = 0.3, ["끈기"] = 0.6,
            ["고통 각인"] = 0.2, ["응징"] = 0.3, ["포자증식"] = 0.12, ["죽음의 낙인"] = 0.3, ["열정 약점"] = 0.2, ["정신 붕괴"] = 0.3, ["둔화"] = 0.15, ["미끄러움"] = 0.2, ["기절"] = 0.8,
        };
        public static readonly Dictionary<string, double> TAG_VAL = new() { ["연계"] = 0.6, ["천상"] = 0.45, ["신속"] = 0.15, ["주도"] = 0.2, ["증발"] = -0.15, ["연결"] = -0.1, ["금기"] = 0, ["봉인"] = -0.5, ["개막"] = 0.2,
            ["망각"] = 0.1, ["제거"] = -0.1, ["축복"] = 0.15, ["결속"] = 0.2, ["봉인된 금기"] = 0, ["열정"] = 0.05, ["탄환"] = 0 };
        public static readonly Dictionary<string, double> HIT_TAG_VAL = new() { ["분쇄"] = 0.06, ["약점"] = 0.1, ["약점 공격"] = 0.1 };
        public const double TOUGH_VAL = 0.3;
        public static readonly Dictionary<string, double> COND_VAL = new()
        {
            [FxK.IfBroken] = 0.4, [FxK.IfChain] = 0.5, [FxK.IfTune] = 0.4, ["draw"] = 0.85, ["discard"] = 0.4, [FxK.IfLink] = 0.5, [FxK.IfPrev] = 0.45,
            ["ifNoStack"] = 0.5, [FxK.IfRhythm] = 0.4, [FxK.IfSwitched] = 0.35,
            // 운영 방식 계기 — 그 조건이 설 확률(대충)
            [FxK.IfRepeat] = 0.4, [FxK.IfHeld] = 0.4, [FxK.IfPlayedMax] = 0.5, [FxK.IfApLeft] = 0.45, [FxK.IfSpent] = 0.35, [FxK.IfBalanced] = 0.4,
            [FxK.IfHunted] = 0.6, [FxK.IfDebuffs] = 0.45, [FxK.IfHp] = 0.45,
            // 키워드 사전 · 고유 효과 틀
            [FxK.IfKill] = 0.35, [FxK.IfBreak] = 0.3, [FxK.IfWounded] = 0.3, [FxK.IfChoice] = 0.5, [FxK.IfRandom] = 0.5, [FxK.IfHand] = 0.4, [FxK.IfPile] = 0.5,
            [FxK.IfNth] = 0.35, [FxK.IfStreak] = 0.35, [FxK.IfAllHeroes] = 0.35, [FxK.IfFoe] = 0.4, [FxK.IfCardSt] = 0.5,
            ["drawAny"] = 0.9, ["burn"] = 0.4, ["passion"] = 0.4,
        };
        /// <summary>「… 1개당」 의 보통 수 — 버린 장수 · 치른 HP(단위) · 적의 디버프 가짓수.</summary>
        public const double DISC_PER = 1.5, PAID_PER = 1.5, DEBUFF_PER = 2;
        public const int RHYTHM_PER = 2;
        public const double FLIP_VAL = 0.3, HASTEN_VAL = 0.3;

        static double Area(string t) => t == "allEnemies" || t == "allAllies" || t == "party" ? 1.6 : t == "randomEnemy" ? 0.9 : 1;
        static readonly HashSet<string> ALLY_SIDE = new() { "self", "oneAlly", "allAllies", "party" };
        static string StTarget(Fx f) => f.Target ?? (R.IsBadSt(f.Id) ? "oneEnemy" : "party");
        static double StArea(Fx f) => ALLY_SIDE.Contains(StTarget(f)) && !R.HERO_ST.Contains(f.Id) ? 1.6 : Area(StTarget(f));

        /// <summary>순서 조건(잇기 · 앞이 …)이 붙은 카드인가.</summary>
        public static Fx OrderCond(List<Fx> fx) => fx?.FirstOrDefault(f => f.K == FxK.IfLink || f.K == FxK.IfPrev);
        /// <summary>리듬을 쓰는 카드인가.</summary>
        public static bool RhythmUse(List<Fx> fx) => fx != null && fx.Any(f => f.K == FxK.PerRhythm || f.K == FxK.IfRhythm);

        static bool MoraleMod(Fx f, IList<string> tags, bool power) => f.K == FxK.AtkMod && f.Run && f.V > 0 && !(power || (tags != null && tags.Contains(Tag.Power)));

        /// <summary>효과의 값어치. live — 순서 조건이 지금 서 있다 · rhythm — 지금 리듬 수.</summary>
        public static double ValueOf(List<Fx> fx, IList<string> tags = null, bool power = false, bool live = false, int? rhythm = null)
        {
            double v = 0, per = 1, cond = 1, dmgV = 0, dmgArea = 1;
            var marks = new List<Fx>();
            foreach (var f in fx ?? new List<Fx>())
            {
                int n = f.HitsOr1;
                double v0 = v;
                switch (f.K)
                {
                    case FxK.IfBroken: case FxK.IfChain: case FxK.IfTune: cond = COND_VAL[f.K]; break;
                    case FxK.IfLink: case FxK.IfPrev: cond = live ? 1 : COND_VAL[f.K]; break;
                    case FxK.IfStack: if (f.Not) cond = COND_VAL["ifNoStack"]; break;
                    case FxK.IfRhythm: cond = rhythm != null ? (rhythm >= f.N ? 1 : 0.2) : COND_VAL[FxK.IfRhythm]; break;
                    case FxK.IfRepeat: case FxK.IfHeld: case FxK.IfPlayedMax: case FxK.IfApLeft: case FxK.IfSpent: case FxK.IfBalanced: case FxK.IfHunted: case FxK.IfDebuffs: case FxK.IfHp:
                        cond = live && (f.K == FxK.IfRepeat) ? 1 : COND_VAL[f.K]; break;
                    case FxK.IfKill: case FxK.IfBreak: case FxK.IfWounded: case FxK.IfChoice: case FxK.IfRandom: case FxK.IfHand: case FxK.IfPile: case FxK.IfNth: case FxK.IfStreak: case FxK.IfAllHeroes: case FxK.IfFoe: case FxK.IfCardSt:
                        cond = f.K == FxK.IfRandom ? (f.Pct > 0 ? f.Pct : 0.5) : COND_VAL[f.K]; break;
                    case FxK.PerTag: case FxK.PerPlayed: case FxK.PerCardSt: per = 2; break;
                    case FxK.PerPile: per = 4; break;
                    case FxK.PerEvent: per = 1; break;
                    case FxK.Drain: v += f.Ratio * 1.5; break;
                    case FxK.Extra: v += f.Ratio * f.HitsOr1 * 0.83 * per * Area(f.Target ?? "oneEnemy"); per = 1; break;
                    case FxK.CardStatus: v += f.Id == "탐구심" ? 0.2 * Math.Max(1, f.V) : 0.15; break;
                    case FxK.Transform: v += 0.4; break;
                    case FxK.Form: v += FORM_VAL; break;   // 대충 — 검사는 FormValue 로 정확히
                    case FxK.FormEnd: break;
                    case FxK.Cue: break;
                    case FxK.Power: v += PowerValue(f); break;
                    case FxK.Later: case FxK.AfterCards: v += 0.8 * ValueOf(f.Then); break;
                    case FxK.Trap: v += 0.6 * ValueOf(f.Then); break;
                    case FxK.Confuse: v += 0.6; break;
                    case FxK.AutoPlay: v += 0.5 * Math.Max(1, f.N); break;
                    case FxK.CastOther: v += 0.8; break;
                    case FxK.Pull: v += 0.5 * Math.Max(1, f.N); break;
                    case FxK.ExileFrom: v += 0.1; break;
                    case FxK.Dispel: v += 0.3; break;
                    case FxK.GrowRun: v += 0.5; break;
                    case FxK.PerDiscarded: per = DISC_PER; break;
                    case FxK.PerPaid: per = PAID_PER; break;
                    case FxK.PerDebuff: per = DEBUFF_PER; break;
                    case FxK.PerApLeft: per = 1; break;
                    case FxK.Feed: v += 0.3 * f.V * (f.Target == "oneAlly" || f.Target == "self" ? 1 : 1.6); break;
                    case FxK.NextAp: v += 0.9 * f.V * (f.V < 0 ? 0.7 : 0.8); break;
                    case FxK.Burn: v += 0.05 * Math.Max(1, f.V); break;
                    case FxK.Reflect: v += f.Ratio * 1.2; break;
                    case FxK.IfSwitched: cond = COND_VAL[FxK.IfSwitched]; break;
                    case FxK.PerRhythm: per = rhythm ?? RHYTHM_PER; break;
                    case FxK.Flip: v += FLIP_VAL; break;
                    case FxK.Hasten: v += HASTEN_VAL * Math.Max(1, f.V); break;
                    case FxK.When: if (COND_VAL.TryGetValue(f.On ?? "", out var cv)) cond = cv; break;
                    case FxK.Tough: v += TOUGH_VAL * Math.Max(1, f.V) * Area(f.Target); break;
                    case FxK.PerStack: per = Math.Max(3, f.N); break;
                    case FxK.Dmg:
                        {
                            double d = f.Ratio * n * 0.83 * Area(f.Target ?? "oneEnemy") * (f.XHits ? 3 : 1) * per * (f.Fixed ? 0.9 : 1);
                            if (dmgV == 0) dmgArea = Area(f.Target ?? "oneEnemy");
                            v += d; dmgV += d * cond; per = 1; break;
                        }
                    case FxK.Block: case FxK.Shield: v += (f.Ratio / 2) * 0.8 * per; per = 1; break;
                    case FxK.Heal: v += (f.Ratio / 2.4) * 0.8 * per; per = 1; break;
                    case FxK.Draw: v += 0.4 * Math.Max(1, f.V); break;
                    case FxK.Ap: v += 0.9 * f.V; break;
                    case FxK.Gauge: v += f.V / 100; break;
                    case FxK.Status:
                        if ((f.Id == "잔불" || f.Id == "잔광") && fx.Any(x => x.K == FxK.Dmg)) { marks.Add(f); break; }
                        v += f.Id == R.STUN ? 0.8 * (f.Target == "allEnemies" ? 1.6 : 1) : (STATUS_VAL.TryGetValue(f.Id, out var sv) ? sv : 0.2) * Math.Max(1, f.V) * StArea(f);
                        break;
                    case FxK.AtkMod:
                        if (MoraleMod(f, tags, power)) { v += (Math.Abs(f.V) / 0.15) * STATUS_VAL["사기"] * Area(f.Target); break; }
                        v += Math.Abs(f.V) * 2 * Math.Min(f.Run ? 4 : f.TurnsOr1, 4) * Area(f.Target); break;
                    case FxK.DealtMod: case FxK.TakenMod: case FxK.DefMod: case FxK.CritMod:
                        v += Math.Abs(f.V) * 2 * Math.Min(f.Run ? 4 : f.TurnsOr1, 4) * Area(f.Target); break;
                    case FxK.Stack: if (f.V > 0) v += 0.3 * f.V; break;
                    case FxK.Strip: v += 0.3; break;
                    case FxK.Cleanse: v += 0.2; break;
                    case FxK.RushDown: v += 0.15 * f.V * Area(f.Target); break;
                    case FxK.NextCheaper: v += 0.8 * f.V; break;
                    case FxK.Make: v += 0.8 * Math.Max(1, f.V); break;
                }
                if (cond != 1) v = v0 + (v - v0) * cond;
            }
            foreach (var t in tags ?? new List<string>())
            {
                var id = Tag.Parse(t).id;
                if (TAG_VAL.TryGetValue(id, out var tv)) v += tv;
                if (HIT_TAG_VAL.TryGetValue(id, out var hv) && dmgV > 0) v += dmgV * hv;
                if (id == Tag.Weak && dmgV > 0) v += TOUGH_VAL * dmgArea;
            }
            foreach (var f in marks) v += f.Id == "잔불" ? dmgV * 0.1 * Math.Min(Math.Max(1, f.V), 5) : dmgV * 0.15 + TOUGH_VAL * dmgArea;
            if (tags != null && tags.Any(t => Tag.Parse(t).id == Tag.Echo)) v *= 1.8;
            return v;
        }

        /// <summary>때 붙은 마디(draw · discard · handEnd)의 값어치 — 그 마디 조각만 센다.</summary>
        public static double PartValue(List<Fx> fx, string on)
        {
            if (fx == null) return 0;
            var part = new List<Fx>(); string cur = null;
            foreach (var f in fx) { if (f.K == FxK.When) { cur = f.On; continue; } if (cur == on) part.Add(f); }
            return part.Count == 0 ? 0 : ValueOf(part);
        }

        /// <summary>그 키워드를 「쥐고 있을수록」 세지는가 — perStack 뒤에 같은 키워드를 쓰지 않거나, ifStack n(2 이상).</summary>
        public static bool GrowsWith(List<Fx> fx, string kw)
        {
            if (fx == null) return false;
            bool per = fx.Any(f => f.K == FxK.PerStack && f.Id == kw), spends = fx.Any(f => f.K == FxK.Spend && f.Id == kw);
            return (per && !spends) || fx.Any(f => f.K == FxK.IfStack && f.Id == kw && f.N >= 2 && !f.Not);
        }

        public static double ValueOf(CardView c, bool live = false, int? rhythm = null) => c == null ? 0 : ValueOf(c.Fx, c.Tags, c.IsPower, live, rhythm);

        public static double BaseValue(int cost) => 0.5 + cost;

        static readonly HashSet<string> PEN_MODS = new() { FxK.DealtMod, FxK.TakenMod, FxK.AtkMod, FxK.DefMod, FxK.CritMod };
        static bool Penalty(Fx f) => (PEN_MODS.Contains(f.K) && f.Target != null && ALLY_SIDE.Contains(f.Target) && (f.K == FxK.TakenMod ? f.V > 0 : f.V < 0))
            || (f.K == FxK.Status && f.Target != null && ALLY_SIDE.Contains(f.Target) && (f.Id == "취약" || f.Id == "약화" || f.Id == "고통" || f.Id == "손상"));

        /// <summary>카드 한 장을 통째로 — 벌칙 · HP 소모 · 버리기 · 태그(보존 · 개전 · 소멸 · 소멸 N · 회수)까지.</summary>
        public static double CardWorth(CardView c)
        {
            var tags = c.Tags; bool power = c.IsPower;
            double v = ValueOf(c.Fx, tags, power);
            foreach (var f in c.Fx)
            {
                if (Penalty(f) && f.K == FxK.Status) v -= 2 * (STATUS_VAL.TryGetValue(f.Id, out var s) ? s : 0.2) * Math.Max(1, f.V) * StArea(f);
                else if (Penalty(f)) v -= 2 * Math.Abs(f.V) * 2 * Math.Min(f.TurnsOr1, 4) * Area(f.Target);
                if (f.K == FxK.PayHp) v -= 0.0035 * f.V;
                if (f.K == FxK.PayHpPct) v -= 3 * f.V;
                if (f.K == FxK.Discard) v -= f.All ? 0.3 : 0.1 * f.V;
                if (f.K == FxK.Burn) v -= f.All ? 0.6 : 0.25 * f.V;
            }
            if (c.HasTag(Tag.Keep)) v += 0.1;
            if (c.HasTag(Tag.Opening)) v += 0.15;
            int ex = c.TagN(Tag.Exhaust);
            if (ex == 0) v *= 0.7; else if (ex > 0) v *= ex >= 3 ? 0.9 : 0.85;
            int rc = c.TagN(Tag.Recall);
            if (rc >= 0) v += 0.15 * Math.Min(3, Math.Max(1, rc));
            return v;
        }

        // ── 강화 카드 지속 규칙 ───────────────────────────────────────
        /// <summary>강화를 켠 뒤 남은 전투의 턴(대충 — 일반 싸움 3.3턴 · 보스 5.6턴, 카드는 보통 1~2턴째에 낸다) · 규칙 몫의 할인.</summary>
        public const double POWER_T = 3, POWER_W = 0.6;

        /// <summary>power 한 조각의 값어치 — 규칙마다 (한 번 발동의 값 × 남은 전투에 발동할 수 × 할인). always 의 증감은 전투 내내 증감과 같은 셈.</summary>
        public static double PowerValue(Fx f)
        {
            double v = 0;
            foreach (var r in f?.Rules ?? new List<PassiveRule>()) v += RuleWorth(r);
            return v;
        }

        /// <summary>강화 규칙 하나의 값어치.</summary>
        public static double RuleWorth(PassiveRule r)
        {
            if (r == null) return 0;
            if (r.When?.On == "always") return ValueOf(r.Fx.Select(x => { var c = x.Copy(); c.Run = true; return c; }).ToList()) * (r.Conds.Count > 0 ? 0.6 : 1);
            return ValueOf(r.Fx) * RuleFires(r) * POWER_W;
        }

        /// <summary>남은 전투(POWER_T 턴)에 그 규칙이 발동할 대충의 수 — 계기마다 턴당 빈도 × 조건 · 횟수 제한.</summary>
        public static double RuleFires(PassiveRule r)
        {
            var w = r.When ?? new When();
            double perTurn = w.On switch
            {
                "turnStart" or "turnEnd" => 1,
                "play" => (w.Who == "any" ? 3.0 : w.Who == "other" ? 2.0 : 1.4) * (w.Type != null ? 0.55 : 1) * (w.Tag != null || w.MaxCost != null || w.Marked != null ? 0.5 : 1) * (w.MinCost > 1 ? 0.5 : 1) / Math.Max(1, w.Every) * (w.Nth > 0 || w.Sig || (w.Seq != null && w.Seq.Count > 0) ? 0.35 : 1),
                "hit" => w.Who == "any" ? 2.5 : 1.0,
                "extra" => w.Who == "any" ? 1.2 : 0.6,
                "hurt" => 1.2, "blocked" => 0.6, "guard" => 1.3, "debuff" => w.Who == "any" ? 1.5 : 0.8,
                "drawn" => 1.2, "kill" => 0.35, "break" => 0.3, "crit" => 0.5, "spend" => 0.8, "make" => 0.6,
                "stackReach" => 0.4, "stackGone" => 0.4, "stackOver" => 0.3, "shieldBreak" => 0.4, "foeAct" => 1.5, "foeActBefore" => 1.5,
                "discard" => 0.6, "exhaust" => 0.4, "ult" => 0.25, "overheal" => 0.4,
                _ => 0.5,
            };
            if (r.Conds.Count > 0) perTurn *= 0.6;
            if (r.Limit != null && r.Limit.Per == "turn") perTurn = Math.Min(perTurn, r.Limit.N);
            double total = perTurn * POWER_T;
            if (r.Limit != null && r.Limit.Per == "fight") total = Math.Min(total, r.Limit.N);
            return total;
        }

        /// <summary>효과 form 한 조각의 대충의 값(데이터 없이 셀 때).</summary>
        public const double FORM_VAL = 1.0;
        /// <summary>변신 동안 카드 한 장이 손에 들 확률(턴마다, 대충).</summary>
        const double FORM_DRAWN = 0.45;

        /// <summary>
        /// 변신 하나의 값어치 — 지속 T 턴(전투 끝까지면 4) 동안: 능력치(증감과 같은 셈) + 바뀐 카드의 값 차이 × 뽑힐 확률 + 카드 덤 + 덧붙인 패시브(턴마다 반쯤) − 끈 패시브 + 풀릴 때.
        /// 정밀하지 않다 — 크게 어긋난 변신을 찾는 체.
        /// </summary>
        public static double FormValue(FormDef f, GameData d, string heroId)
        {
            if (f == null) return 0;
            double T = f.Turns > 0 ? Math.Min(f.Turns, 4) : 4, v = 0;
            if (f.Mods != null) foreach (var kv in f.Mods) v += (kv.Key == "taken" ? -kv.Value : kv.Value) * 2 * T;
            if (f.Cards != null && d != null)
                foreach (var kv in f.Cards)
                {
                    var a = d.View(kv.Key); var b = d.View(kv.Value);
                    if (a == null || b == null) continue;
                    v += (CardWorth(b) - CardWorth(a)) * T * FORM_DRAWN;
                }
            if (f.Bonus != null && d != null)
            {
                var h = d.Hero(heroId);
                var ids = (h?.Starter ?? new List<string>()).Concat(d.UniquesOf(heroId)).ToList();
                foreach (var b in f.Bonus)
                    foreach (var id in ids)
                    {
                        var c = d.View(id);
                        if (c == null || (b.Card != null && b.Card != id) || (b.Type != null && c.Type != b.Type) || (b.Unique && !c.Unique) || (b.Tag != null && !c.HasTag(b.Tag))) continue;
                        double w = c.Unique ? 0.5 : 1;
                        double dmg = b.Ratio > 0 ? ValueOf(c.Fx.Where(x => x.K == FxK.Dmg || x.K == FxK.Extra).ToList()) * (b.Ratio - 1) : 0;
                        v += (dmg + ValueOf(b.Fx ?? new List<Fx>(), b.Tags)) * w * T * FORM_DRAWN;
                    }
            }
            foreach (var r in f.Passives ?? new List<PassiveRule>()) v += ValueOf(r.Fx) * T * 0.5;
            if (f.Replace && d?.Hero(heroId) is HeroDef hd) foreach (var r in hd.Passives) v -= ValueOf(r.Fx) * T * 0.5;
            v += ValueOf(f.Off ?? new List<Fx>());
            return v;
        }

        /// <summary>코스트당 값어치 — 상점 · 신탁 · 덱 다듬기에 쓴다.</summary>
        public static double Efficiency(CardView c)
        {
            if (c == null) return 0;
            if (c.IsCurse || c.IsStatus) return -2;
            int cost = c.X ? 3 : c.Cost;
            double v = ValueOf(c);
            return v / (0.5 + cost) + (cost == 0 && v > 0 ? 0.3 : 0);
        }
    }
}
