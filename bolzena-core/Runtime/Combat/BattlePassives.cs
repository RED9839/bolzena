using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>패시브를 깨우는 일의 내용.</summary>
    public sealed class EmitInfo
    {
        public string Hero, Actor, Type, By, Id, Owner, Kind;
        public int Nth, Cost, Seq, N, V;
        /// <summary>keepAp — 쥐고 넘긴 보존 카드 수.</summary>
        public int Kept;
        public bool Sig, Decay, On, Repeat, Fresh, Weak;
        public Unit Target, Who, From;
        public double Before, After;
        /// <summary>play — 낸 카드의 태그.</summary>
        public List<string> Tags;
    }

    public sealed partial class Battle
    {
        static readonly Dictionary<string, string> MOD_STAT = new() { [FxK.DealtMod] = "dealt", [FxK.TakenMod] = "taken", [FxK.AtkMod] = "atk", [FxK.DefMod] = "def", [FxK.CritMod] = "crit" };

        /// <summary>사도마다 규칙(패시브 + 키워드 규칙 + 장비 효과)을 건다.</summary>
        void SetupPassives(Dictionary<string, List<PassiveRule>> gearRules)
        {
            foreach (var u in Party)
            {
                var h = Data.Hero(u.Key);
                var rules = new List<RuleRt>();
                rules.AddRange(h.Passives.Select(r => new RuleRt { R = r, Own = true }));
                foreach (var kd in h.AllKeywords)
                {
                    Kw[kd.Name] = new KwRt { Def = kd, Owner = u.Key };
                    rules.AddRange(kd.Rules.Select(r => new RuleRt { R = r, KwOf = kd.Carrier ?? "self" }));
                    var car = kd.Carrier ?? "self";
                    if ((car == "self" || car == "hero") && kd.CapOrMode != null)
                        foreach (var holder in car == "hero" ? Party : new List<Unit> { u })
                        {
                            if (!StackCap.TryGetValue(holder.Key, out var caps)) StackCap[holder.Key] = caps = new();
                            caps[kd.Name] = kd.CapOrMode.Value;
                        }
                }
                if (gearRules != null && gearRules.TryGetValue(u.Key, out var gr)) rules.AddRange(gr.Select(r => new RuleRt { R = r, Gear = true }));
                Passives[u.Key] = rules;
            }
            // 「항상」 규칙의 증감 — 늘 걸려 있다
            foreach (var kv in Passives)
                foreach (var rt in kv.Value)
                {
                    if (rt.R.When.On != "always") continue;
                    foreach (var f in rt.R.Fx)
                    {
                        if (!MOD_STAT.TryGetValue(f.K, out var stat)) continue;
                        var who = f.Target == "allAllies" || f.Target == "party" ? Party.Select(p => p.Key).ToList() : new List<string> { kv.Key };
                        foreach (var k in who)
                        {
                            if (!Always.TryGetValue(k, out var l)) Always[k] = l = new List<AlwaysMod>();
                            l.Add(new AlwaysMod { Stat = stat, V = f.V, Cond = rt.R.Conds, Owner = kv.Key, Name = rt.R.Name });
                        }
                    }
                }
        }

        /// <summary>능력치 증감 합 — 걸린 증감(Mods) + 「항상」 패시브 + 키워드 1개당.</summary>
        public double StatMod(Unit u, string stat)
        {
            if (u == null) return 0;
            double v = FormMod(u, stat) + PowerMod(u, stat);
            foreach (var m in u.Mods) if (m.Stat == stat) v += m.V;
            if (u.Side == Side.Party && u.BodyRef != null && Always.TryGetValue(u.Key, out var al))
                foreach (var m in al)
                {
                    if (m.Stat != stat) continue;
                    if (m.Cond != null && m.Cond.Count > 0) { var owner = HeroUnit(m.Owner); if (owner == null || !CondOk(owner, m.Cond, null, new EmitInfo())) continue; }
                    v += m.V;
                }
            foreach (var kw in Kw.Values)
                foreach (var p in kw.Def.Per)
                {
                    if (p.Stat != stat) continue;
                    if (kw.Carrier == "hero") { if (u.Side == Side.Party) v += StackOf(u.Key, kw.Id) * p.V; continue; }
                    if (kw.Carrier == "self")
                    {
                        var owner = HeroUnit(kw.Owner);
                        int n = StackOf(kw.Owner, kw.Id);
                        if (n == 0 || owner == null) continue;
                        if (p.Who == "allies" ? u.Side == Side.Party : u == owner) v += n * p.V;
                    }
                    else
                    {
                        if (p.From == "owner") continue;   // 건 사도가 칠 때만 — Hurt 의 OwnerMarkMod
                        int n = St(u, kw.Id);
                        if (n != 0) v += n * p.V;
                    }
                }
            return v;
        }

        public int StackOn(Unit holder, string id, Unit owner)
        {
            if (Kw.TryGetValue(id, out var kw) && kw.Carrier != "self" && holder != null) return St(holder, id);
            return StackOf(owner?.Key ?? holder?.Key, id);
        }

        /// <summary>그 규칙의 조건이 지금 서 있나(봇이 「턴을 넘기면 돌 패시브」 를 미리 볼 때). 횟수 제한은 안 본다.</summary>
        public bool CondsHold(Unit owner, RuleRt rt) => CondOk(owner, rt.R.Conds, rt.KwOf, new EmitInfo());

        bool CondOk(Unit owner, List<Cond> conds, string kwOf, EmitInfo info)
        {
            foreach (var c in conds)
            {
                switch (c.C)
                {
                    case "stack":
                        {
                            Kw.TryGetValue(c.Id, out var kw);
                            string kc = kw?.Carrier;
                            Unit holder = kc == "enemy" ? info.Target
                                : kc == "ally" && kwOf == "ally" && info.Actor != null ? (HeroUnit(info.Actor) ?? owner)
                                : owner;
                            int n = StackOn(holder, c.Id, owner);
                            if (c.Not ? n > 0 : n < Math.Max(1, c.N)) return false;
                            if (!c.Not && c.Max > 0 && n > c.Max) return false;
                            break;
                        }
                    case "hp": if ((double)owner.Hp / owner.MaxHp > c.Pct) return false; break;
                    case "hpMin": if ((double)owner.Hp / owner.MaxHp < c.Pct) return false; break;
                    case "status": if (St(c.Id == R.RHYTHM ? Pool : owner, c.Id) < c.N) return false; break;
                    case "foes": if (AliveEnemies().Count < c.N) return false; break;
                    case "foesMax": if (AliveEnemies().Count > c.N) return false; break;
                    case "playedMax": if (PlayedThisTurn > c.N) return false; break;
                    case "playedMin": if (PlayedThisTurn < c.N) return false; break;
                    case "ownNone":
                        if (c.Type != null) { if (PlayLog.Any(p => p.hero == owner.Key && p.type == c.Type)) return false; }
                        else if (PlayedBy.TryGetValue(owner.Key, out var pb) && pb > 0) return false;
                        break;
                    case "apLeft": if (Ap < Math.Max(1, c.N)) return false; break;
                    case "gauge": if (Gauge < c.N) return false; break;
                    case "guarded":
                        {
                            bool g = c.Kind == "block" ? owner.Block > 0 : c.Kind == "shield" ? owner.Shield > 0 : owner.Block > 0 || owner.Shield > 0;
                            if (g == c.Not) return false;   // not — 실드가 없으면
                            break;
                        }
                    case "onlyMe": if (!(PlayLog.Count > 0 && PlayLog.All(p => p.hero == owner.Key))) return false; break;
                    case "ally": if (!Party.Any(u => u.Key == c.Id && !u.Dead)) return false; break;
                    case "inDebt": if (ApJam <= 0) return false; break;
                    case "heldCards": if (Hand.Count(id => CardOf(id)?.Hero == (c.Id ?? owner.Key)) < Math.Max(1, c.N)) return false; break;
                    case "idleLast": if (Turn <= 1 || !AliveParty().Any(u => u != owner && !(PlayedPrev.TryGetValue(u.Key, out var pp) && pp > 0))) return false; break;
                    case "typeNew": if (info.Type == null || PlayLog.Count(p => p.type == info.Type) != 1) return false; break;
                    case "rushed": if (!RushedThisTurn) return false; break;
                    case "hurtLast": if (!HurtPrev.Contains(owner.Key)) return false; break;
                    case "killedLast": if (!KillPrev.ContainsKey(owner.Key)) return false; break;
                    case "firstTurn": if (Turn != 1) return false; break;
                    case "repeat": if (!(PlayIds.Count >= 2 && PlayIds[PlayIds.Count - 1] == PlayIds[PlayIds.Count - 2])) return false; break;
                    case "held": if (!Hand.Any(id => (Held.TryGetValue(id, out var hn) ? hn : 0) >= System.Math.Max(1, c.N))) return false; break;
                    case "spent": if (ApSpent != c.N) return false; break;
                    case "balanced": { int a = PlayLog.Count(p => p.type == "공격"), sk = PlayLog.Count(p => p.type == "스킬"); if (!(a > 0 && a == sk)) return false; break; }
                    case "debuffs": if (!AliveEnemies().Any(e => DebuffKinds(e) >= System.Math.Max(1, c.N))) return false; break;
                    case "paid": if (PaidHp < System.Math.Max(1, c.N)) return false; break;
                    case "targetBroken": if (!(info.Target != null && info.Target.Side == Side.Enemy && info.Target.Broken)) return false; break;
                    case "wounded": if (!((double)Pool.Hp / Math.Max(1, Pool.MaxHp) < R.SV("부상"))) return false; break;
                    case "goneMin": if (GoneN < Math.Max(1, c.N)) return false; break;   // 이번 전투에 소멸한 카드가 N장 이상 — 소멸형
                // 시범 16(2026-10-08 2단계)
                case "pricier": if (!(Counts.TryGetValue("pricier", out var prv) && prv > 0)) return false; break;   // 방금 낸 카드가 이번 턴 바로 앞 카드보다 비쌌으면
                case "gained": if ((GainedOf(owner.Key, c.Id) >= Math.Max(1, c.N)) == c.Not) return false; break;  // 이번 전투에 그 고유 효과를 n 이상 쌓았으면(not — 못 쌓았으면)
                    default: throw new InvalidOperationException($"모르는 조건: {c.C}");
                }
            }
            return true;
        }

        /// <summary>overheal 의 일의 값 — pct 가 없으면 최대 HP 를 넘친 몫(info.V), 있으면 그 선을 넘은 몫(회복량까지).</summary>
        static int OverOf(When w, EmitInfo info) => w.Pct > 0 && info.Who != null ? OverPart((int)info.After, info.N, info.Who.MaxHp, w.Pct) : info.V;

        /// <summary>공용 계기의 「갈래마다 한 사도」 — who any · other 로 아군의 일에 반응하는 규칙과 keepAp(파티의 일). 갈래가 없는 사도 · 장비 · 강화 규칙은 빼고.</summary>
        bool SharedTaken(Unit owner, RuleRt rt, string ev)
        {
            if (rt.Gear || rt.Power != null) return false;
            if (!(rt.R.When.Who == "any" || rt.R.When.Who == "other" || ev == "keepAp")) return false;
            var style = Data.Hero(owner.Key)?.Style;
            if (style == null) return false;
            sharedOnce ??= new Dictionary<string, string>();
            string k = style + "|" + ev;
            if (sharedOnce.TryGetValue(k, out var first)) return first != owner.Key;   // 같은 사도의 다른 규칙은 돈다
            sharedOnce[k] = owner.Key;
            return false;
        }

        static bool Whose(When w, Unit owner, string who) => w.Who == "any" || (w.Who == "other" ? who != null && who != owner.Key : who == owner.Key);

        /// <summary>한 번의 일(카드 한 장 · 패시브 한 번)에 한 번만 도는 일.</summary>
        static readonly HashSet<string> ONCE_PER_ACT = new() { "debuff", "spend", "crit", "make", "hit" };

        /// <summary>규칙의 계기가 이 일인가 — Matches 의 첫 줄과 같다(Emit 이 미리 거른다).</summary>
        bool HasPowerOf(string hero) { foreach (var p in Powers) if (p.Hero == hero) return true; return false; }
        static bool OnEvent(When w, string ev) => w.On == ev || (w.On == "hurt" && w.Guarded && ev == "blocked");
        bool Matches(Unit owner, When w, string ev, EmitInfo info, string kwOf)
        {
            if (w.On != ev && !(w.On == "hurt" && w.Guarded && ev == "blocked")) return false;
            switch (ev)
            {
                case "play":
                    if (w.Marked != null) { if (info.Hero == null || StackOf(info.Hero, w.Marked) <= 0) return false; }
                    else if (w.Who == "other") { if (info.Hero == null || info.Hero == owner.Key) return false; }
                    else if (w.Who != "any" && kwOf != "ally" && info.Hero != owner.Key) return false;
                    if (w.Type != null && info.Type != w.Type) return false;
                    if (w.Nth > 0 && info.Nth != w.Nth) return false;
                    int minCost = w.MinCost ?? (w.Every > 0 ? 1 : 0);
                    if (minCost > 0 && info.Cost < minCost) return false;
                    if (w.Sig && !info.Sig) return false;
                    if (w.Repeat && !info.Repeat) return false;
                    if (w.Tag != null && !(info.Tags != null && info.Tags.Any(t => Tag.Parse(t).id == w.Tag))) return false;
                    if (w.MaxCost != null && info.Cost > w.MaxCost.Value) return false;
                    if (w.CardSt != null && CardStOf(info.Id, w.CardSt) <= 0) return false;
                    if (w.Basic) { var pc = Data.Card(GameData.BaseId(info.Id)); if (pc == null || pc.Hero == null || pc.Unique || pc.Token || GameData.IsCopy(info.Id) || GameData.IsPlain(info.Id)) return false; }   // 시작 카드만(2026-10-08)
                    return true;
                case "guard": return info.Who != null && info.Who.Side == Side.Party && (w.Kind == null || info.Kind == w.Kind);
                case "kill":
                case "break": return !w.Mine || info.By == owner.Key;
                case "hurt": return info.Who != null && info.Who.Side == Side.Party && (w.Pct <= 0 || info.N >= info.Who.MaxHp * w.Pct);
                case "lowHp": return info.Who != null && info.Who.Side == Side.Party && info.Before > w.Pct && info.After <= w.Pct;
                case "ult": return w.Who == "any" || info.Hero == owner.Key;
                case "debuff": return (w.Who == "any" || info.By == owner.Key) && (!w.Fresh || info.Fresh);
                // 넘친 회복 — pct 가 있으면 「회복 뒤 HP 가 최대 HP × pct 이상, 선을 넘은 몫이 있으면」(파티 HP 하나 — 1단계 다시 정의)
                case "overheal": return (w.Who == "any" || info.By == owner.Key) && OverOf(w, info) > 0;
                case "guardSum": return info.V >= Math.Max(1, w.N);
                // 짝을 잇는 공용 계기 — 기본은 그 사도, who any 면 아군 누구든 · other 면 다른 아군만
                case "spend": return Whose(w, owner, info.Owner) && (w.Id == null || w.Id == info.Id) && info.N >= Math.Max(1, w.N);
                case "link": return Whose(w, owner, info.Hero);
                case "crit":
                case "make":
                case "extra": return Whose(w, owner, info.By);
                // 운영 방식 공용 계기 — 「카드가 버려지면」 · 「HP를 치르면」 · 「카드가 소멸하면」: 기본은 그 사도(제 카드 · 제 효과), who any 면 파티 누구든
                case "discard":
                case "exhaust":
                    if (w.Basic) { var bc = Data.Card(info.Id); if (bc == null || bc.Hero == null || bc.Unique || bc.Token || GameData.IsCopy(info.Id) || GameData.IsPlain(info.Id)) return false; }
                    // type — 그 카드의 종류만(상태 · 저주 · 공격 · 스킬 …) · who other — 다른 아군의 카드 · 다른 아군이 지운 것만
                    if (w.Type != null && CardOf(info.Id)?.Type != w.Type) return false;
                    if (w.Who == "other") return (info.Hero ?? info.By) != null && info.Hero != owner.Key && info.By != owner.Key;
                    return w.Who == "any" || info.Hero == owner.Key || info.By == owner.Key;
                // 18갈래 공용 계기(2026-10-08) — 셈이 맞으면 · AP 를 남기고 턴 끝 · 예약이 터지면 · 카드가 자라면 · 전투 끝
                case "tally": return Whose(w, owner, info.By);
                // 아껴 두기 — 기본: AP 를 n(기본 1) 이상 남겼거나 보존 카드를 쥐고 넘김 · kind ap: AP 만 · kind keep: 보존 카드 n 장 이상만
                case "keepAp": return w.Kind == "keep" ? info.Kept >= Math.Max(1, w.N) : w.Kind == "ap" ? info.N >= Math.Max(1, w.N) : info.N >= Math.Max(1, w.N) || info.Kept > 0;
                case "reserveFire": return Whose(w, owner, info.Owner);
                case "grow": return Whose(w, owner, info.Hero) && (w.CardSt == null || w.CardSt == info.Kind);
                case "fightEnd": return true;
                case "pay": return w.Who == "any" || info.By == owner.Key;
                // 「찍은 적이 쓰러지면」 — 그 사도의 찍기 키워드, who any 면 아군 누구의 찍기든
                case "huntDown": return (w.Who == "any" || info.Owner == owner.Key) && (w.Id == null || w.Id == info.Id);
                case "blocked": return info.Who != null && info.Who.Side == Side.Party;
                // 사도 고유 효과 틀 — 뽑힘 · 적의 행동(직전/직후, type 공격이면 치는 수만) · 아군이 적을 침 · 실드 깨짐 · 적 실드
                case "drawn":
                    if (w.Unique && !(Data.Card(info.Id)?.Unique ?? false)) return false;
                    if (w.Marked != null) return info.Hero != null && StackOf(info.Hero, w.Marked) > 0;
                    if (w.Type != null && info.Type != w.Type) return false;
                    if (w.Tag != null && !(info.Tags != null && info.Tags.Any(t => Tag.Parse(t).id == w.Tag))) return false;
                    return Whose(w, owner, info.Hero);
                case "foeAct":
                case "foeActBefore": return w.Type == null || info.Type == w.Type;
                case "hit": return Whose(w, owner, info.By) && (!w.Weak || info.Weak);
                case "shieldBreak": return !w.Mine || info.By == owner.Key;
                case "endure": return true;
                case "unwound": return Whose(w, owner, info.By);
                case "foeShieldBreak": return !w.Mine || info.By == owner.Key;
                case "stackReach": return w.Id == info.Id && info.Before < w.N && info.After >= w.N && (info.Owner == null || info.Owner == owner.Key);
                case "stackGone": return w.Id == info.Id && (!w.Decay || info.Decay) && (info.Owner == null || info.Owner == owner.Key);
                case "stackOver": return w.Id == info.Id && (info.Owner == null || info.Owner == owner.Key);
                // 소환물이 행동하면(시범 16) — kind atk(따라 침) · guard(대신 맞음) · lost(하나가 사라짐), 기본은 그 사도의 소환물
                case "summonAct": return Whose(w, owner, info.Owner) && (w.Id == null || w.Id == info.Id) && (w.Kind == null || w.Kind == info.Kind);
                case "switch": return w.Who == "any" || info.Owner == owner.Key;
                case "rhythm": return info.Before < w.N && info.After >= w.N;
                default: return true;
            }
        }

        /// <summary>일이 났다 — 맞는 규칙을 모두 돌린다. 패시브가 패시브를 부르는 고리는 다섯 겹에서 끊는다.</summary>
        void Emit(string ev, EmitInfo info)
        {
            if (ev == "exhaust")
            {
                GoneN++;   // 이번 전투 소멸 장수 — 패시브가 없어도 센다(조건 goneMin)
                if (info.Hero != null) Counts["gone|" + info.Hero] = GoneOf(info.Hero) + 1;   // 그 사도의 카드 — perGone who self(시범 16 니콜)
            }
            if (Passives.Count == 0 || Over != null) return;
            if (depth > 4) return;
            // 연쇄 상한(1단계) — 예약 계기 안에서 난 예약 계기는 패시브를 깨우지 않는다(재촉 ↔ reserveFire 순환 · 깊이 1)
            bool rsv = ev == "reserveFire" || ev == "reserveGone";
            if (rsv && reserveIn > 0) { Say("(예약 연쇄 — 예약 계기 안의 예약 계기는 돌지 않는다)"); return; }
            if (rsv) reserveIn++;
            var shared0 = sharedOnce; sharedOnce = null;   // 「갈래마다 한 사도」 는 이 일 한 번 몫
            depth++;
            try
            {
                foreach (var owner in Party)
                {
                    if (owner.Dead || Over != null) continue;
                    if (!Passives.TryGetValue(owner.Key, out var rules)) continue;
                    // 변신 — 그 변신의 패시브를 뒤에 더하고(replace 면 사도 자신의 패시브를 끈다)
                    var fd = Forms.Count > 0 ? FormDefOf(owner.Key) : null;
                    var frules = fd != null ? FormRules(fd) : null;
                    int total = rules.Count + (frules?.Count ?? 0);
                    for (int i = 0; i < total; i++)
                    {
                        bool fr = i >= rules.Count;
                        var rt = fr ? frules[i - rules.Count] : rules[i];
                        if (!fr && rt.Own && fd != null && fd.Replace) continue;
                        if (!OnEvent(rt.R.When, ev)) continue;   // Matches 의 첫 줄 — 안 맞는 규칙은 id 글을 짓기 전에 거른다
                        string id = fr ? $"{owner.Key}|form:{fd.Id}|{i - rules.Count}" : $"{owner.Key}|{i}";
                        FireRule(owner, rt, id, ev, info, 1);
                    }
                    // 강화 카드 지속 규칙 — 켜진 강화(그 사도가 낸 것)마다, 겹 수만큼 효과가 돈다
                    if (HasPowerOf(owner.Key))   // 이 사도의 강화가 없으면 목록을 베끼지 않는다
                        foreach (var pw in Powers.ToList())
                        {
                            if (pw.Hero != owner.Key || Over != null) continue;
                            var prs = PowerRules(pw);
                            for (int i = 0; i < prs.Count; i++) if (OnEvent(prs[i].R.When, ev)) FireRule(owner, prs[i], $"{owner.Key}|pw:{pw.Id}|{i}", ev, info, Math.Max(1, pw.N));
                        }
                }
                if (Forms.Count > 0 && Over == null) FormUntil(ev, info);
            }
            finally { depth--; sharedOnce = shared0; if (rsv) reserveIn--; }
        }

        /// <summary>규칙 하나를 그 일에 맞춰 돌린다(맞지 않으면 그냥 돌아간다). reps — 효과를 되풀이할 수(강화 겹). 횟수 제한은 발동 수로 센다.</summary>
        void FireRule(Unit owner, RuleRt rt, string id, string ev, EmitInfo info, int reps)
        {
            var r = rt.R;
            if (!Matches(owner, r.When, ev, info, rt.KwOf)) return;
            if (firing.Contains(id)) return;
            // 「적에게 디버프를 걸면」 — 한 번의 일에 한 번
            if (ONCE_PER_ACT.Contains(ev) && info.Seq != 0) { string dk = id + "|" + ev; if (Counts.TryGetValue(dk, out var dv) && dv == info.Seq) return; Counts[dk] = info.Seq; }
            // 「N장 낼 때마다」 — 장수는 조건과 상관없이 세고, N장째에 조건을 본다
            if (r.When.Every > 0)
            {
                string ck = r.When.PerTurn ? $"{id}|{Turn}" : id;
                Counts[ck] = (Counts.TryGetValue(ck, out var cv) ? cv : 0) + 1;
                if (Counts[ck] % r.When.Every != 0) return;
            }
            // 「… 카드를 차례로 내면」
            if (r.When.Seq != null && r.When.Seq.Count > 0)
            {
                string sk = $"{id}|seq|{Turn}";
                if (SeqStep(r.When, owner.Key, Counts.TryGetValue(sk, out var sv) ? sv : 0) < r.When.Seq.Count) return;
                Counts[sk] = PlayLog.Count;
            }
            if (!CondOk(owner, r.Conds, rt.KwOf, info)) return;
            // 인원 세기 시너지 없음(1단계) — 같은 갈래 사도 여럿이 한 일에 who any · other(· keepAp)로 겹쳐 돌지 않게, 갈래마다 처음 한 사도만
            if (SharedTaken(owner, rt, ev)) return;
            if (ev == "rhythm") { string rk = $"{id}|rhythm|{Turn}"; if (Counts.ContainsKey(rk)) return; Counts[rk] = 1; }
            if (r.Limit != null)
            {
                string key = $"{id}|{(r.Limit.Per == "fight" ? "f" : Turn.ToString())}";
                if ((Fired.TryGetValue(key, out var fv) ? fv : 0) >= r.Limit.N) return;
                Fired[key] = (Fired.TryGetValue(key, out var fv2) ? fv2 : 0) + 1;
            }
            else if (ev == "lowHp")
            {
                string key = $"{id}|f";
                if (Fired.ContainsKey(key)) return;
                Fired[key] = 1;
            }
            if (r.Fx.Count == 0) return;
            // 일을 당한 적 — 대상 적 · 없으면 때린 적(공격받음 · 막음 · 실드 깨짐)
            var target = info.Target != null && info.Target.Side == Side.Enemy ? info.Target : info.From != null && info.From.Side == Side.Enemy && !info.From.Dead ? info.From : null;
            Unit holder = ((ev == "stackReach" || ev == "stackGone" || ev == "stackOver") && info.Target != null && info.Target != owner)
                || ((ev == "reserveGone" || ev == "reserveFire" || ev == "switch") && info.Target != null && info.Target.Side == Side.Enemy) ? info.Target : null;
            var ally = info.Who != null && info.Who.Side == Side.Party && !info.Who.Dead ? info.Who
                // 아군에게 붙은 표시(carrier hero)가 차거나 사라지면 「아군 1명」 = 그 아군(캬롯 씨앗이 여문 아군 — 시범 16)
                : (ev == "stackReach" || ev == "stackGone" || ev == "reserveGone" || ev == "reserveFire") && info.Target != null && info.Target.Side == Side.Party && !info.Target.Dead ? info.Target
                : owner;
            firing.Add(id);
            var gs0 = gearSrc; gearSrc = rt.Gear ? "gear:" + owner.Key : null;
            if (ev == "huntDown" || ev == "kill" || ev == "break") holder = info.Target;
            string label = rt.Power != null ? $"{owner.Name} · 강화 「{rt.Power}」" : $"{owner.Name} · {r.Name}";
            try
            {
                for (int k = 0; k < reps && Over == null; k++)
                {
                    var t = target != null && !target.Dead ? target : null;
                    RunPassive(owner, r.Fx, new FxCtx { Owner = owner, TargetIdx = t != null ? t.Idx : (AliveEnemies().FirstOrDefault()?.Idx ?? 0), Passive = r.Name ?? rt.Power, Holder = holder, Ally = ally, EventV = ev == "overheal" ? OverOf(r.When, info) : info.V, Attacker = info.From, Scale = ripenScale }, label);
                }
            }
            finally { firing.Remove(id); gearSrc = gs0; }
        }

        void RunPassive(Unit owner, List<Fx> fx, FxCtx ctx, string label)
        {
            Say(label);
            var (prev, src0, seq0) = (Acting, ModSrc, ActSeq);
            int ap0 = Ap;
            Acting = owner.Key; ModSrc = label;
            ActSeq = ++SeqN;
            bool top = !gainIn; gainIn = true;
            try { RunFx(fx, ctx); } finally { if (top) gainIn = false; }
            Acting = prev; ModSrc = src0; ActSeq = seq0;
            if (top && Ap > ap0) { Say($"{owner.Name}: AP +{Ap - ap0}"); StatusCue(owner, $"AP +{Ap - ap0}", true); }
            CheckOver();
        }

        /// <summary>턴 끝 — 키워드의 1개당 턴 끝 피해 · 회복.</summary>
        void TickTurnEnd()
        {
            foreach (var kw in Kw.Values.ToList())
            {
                var owner = HeroUnit(kw.Owner);
                if (owner == null) continue;
                foreach (var p in kw.Def.Per)
                {
                    if (p.Stat == "dot" && kw.Carrier == "self")
                    {
                        int n = StackOf(kw.Owner, kw.Id);
                        var foes = AliveEnemies();
                        if (n > 0 && foes.Count > 0)
                        {
                            var e = foes[Rng.Int(foes.Count)];
                            int v = Math.Max(1, Num.Round(owner.Atk * p.Ratio * n));
                            Hurt(e, v, new HurtOpts { From = owner });
                            Say($"{owner.Name} · {kw.Id} {n} — {e.Name}에게 {v} 피해");
                        }
                    }
                    else if (p.Stat == "dot")
                    {
                        var holders = kw.Carrier == "enemy" ? Enemies.ToList() : Party.Take(1).ToList();
                        foreach (var e in holders)
                        {
                            int n = St(e, kw.Id);
                            if (n == 0 || e.Dead) continue;
                            int v = Math.Max(1, Num.Round(owner.Atk * p.Ratio * n));
                            Hurt(e, v, new HurtOpts { From = owner });
                            Say($"{e.Name}: {kw.Id} {n} — {v} 피해");
                        }
                    }
                    if (p.Stat == "hot")
                    {
                        var holders = kw.Carrier == "self" ? new List<Unit> { owner } : Party.Take(1).ToList();
                        foreach (var u in holders)
                        {
                            int n = kw.Carrier == "self" ? StackOf(kw.Owner, kw.Id) : St(u, kw.Id);
                            if (n == 0 || u.Dead) continue;
                            int v = Math.Max(1, Num.Round(owner.Def * (1 + StatMod(owner, "def")) * p.Ratio * n));
                            int h0 = u.Hp; u.Hp = Math.Min(u.MaxHp, u.Hp + v); HealCue(u, h0);
                        }
                    }
                    if (Over != null) return;
                }
            }
        }

        /// <summary>사도 고유 효과의 「내 턴이 끝나면 N 감소 · 전부 사라진다」.</summary>
        void KwEndDecay()
        {
            foreach (var kw in Kw.Values.ToList())
            {
                var d = kw.Def;
                if (!d.EndClear && d.EndDecay <= 0) continue;
                int Cut(int n) => d.EndClear ? 0 : Math.Max(0, n - d.EndDecay);
                if (kw.Carrier == "hero")
                {
                    foreach (var hu in Party) { int n = StackOf(hu.Key, kw.Id); if (n > 0) { Stacks[hu.Key][kw.Id] = Cut(n); if (Stacks[hu.Key][kw.Id] == 0) KwGone(kw.Id, kw.Owner, hu, true); } }
                }
                else if (kw.Carrier == "self")
                {
                    int n = StackOf(kw.Owner, kw.Id);
                    if (n > 0) { Stacks[kw.Owner][kw.Id] = Cut(n); if (Stacks[kw.Owner][kw.Id] == 0) KwGone(kw.Id, kw.Owner, HeroUnit(kw.Owner), true); }
                }
                else
                    foreach (var u in Party.Take(1).Concat(Enemies).ToList())
                        if (St(u, kw.Id) > 0) { int left = Cut(St(u, kw.Id)); SetStRaw(u, kw.Id, left); if (left == 0) KwGone(kw.Id, kw.Owner, u, true); }
                if (Over != null) return;
            }
        }

        /// <summary>키워드 겹 줄이기(적의 차례가 끝나면 N 감소) — 다음 내 턴 시작에. 다 닳은 것을 돌려준다.</summary>
        List<(KwRt kw, Unit holder)> DecayKeywords()
        {
            var gone = new List<(KwRt, Unit)>();
            foreach (var kw in Kw.Values)
            {
                var d = kw.Def;
                if (!d.Decays) continue;
                int Cut(int n) => d.DecayAll ? 0 : Math.Max(0, n - d.Decay);
                if (kw.Carrier == "hero")
                {
                    foreach (var hu in Party) { int n = StackOf(hu.Key, kw.Id); if (n > 0) { Stacks[hu.Key][kw.Id] = Cut(n); if (Stacks[hu.Key][kw.Id] == 0) gone.Add((kw, hu)); } }
                }
                else if (kw.Carrier == "self")
                {
                    int n = StackOf(kw.Owner, kw.Id);
                    if (n > 0) { Stacks[kw.Owner][kw.Id] = Cut(n); if (Stacks[kw.Owner][kw.Id] == 0) gone.Add((kw, HeroUnit(kw.Owner))); }
                }
                else
                {
                    var holders = Party.Take(1).Concat(Enemies).ToList();
                    foreach (var u in holders)
                        if (St(u, kw.Id) > 0)
                        {
                            int left = Cut(St(u, kw.Id));
                            SetStRaw(u, kw.Id, left);
                            if (left == 0) gone.Add((kw, u));
                        }
                }
            }
            return gone;
        }

        /// <summary>「공격 · 스킬 · 강화 카드를 차례로 내면」 이 어디까지 왔나(0 ~ 차례 길이). from — 이미 쓴 자리.</summary>
        public int SeqStep(When w, string ownerKey, int from = 0)
        {
            var seq = w.Seq ?? new List<string>();
            for (int k = Math.Min(seq.Count, PlayLog.Count - from); k > 0; k--)
            {
                bool ok = true;
                for (int i = 0; i < k; i++)
                {
                    var p = PlayLog[PlayLog.Count - k + i];
                    if (p.type != seq[i] || (w.Who != "any" && p.hero != ownerKey)) { ok = false; break; }
                }
                if (ok) return k;
            }
            return 0;
        }

        void TickMods()
        {
            foreach (var u in Party.Concat(Enemies))
            {
                if (u.Mods.Count == 0) continue;
                foreach (var m in u.Mods) m.Left -= 1;
                u.Mods.RemoveAll(m => m.Left <= 0);
            }
        }
    }
}
