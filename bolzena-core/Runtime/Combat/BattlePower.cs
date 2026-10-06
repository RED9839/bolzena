using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>켜진 강화 하나(저장된다) — 누가(Hero) · 무엇(Id · Name · Card) · 겹(N) · 규칙(Rules — 처음 켤 때의 것).</summary>
    public sealed class PowerRt
    {
        public string Hero, Id, Name, Card;
        public int N;
        public List<PassiveRule> Rules = new();
        public PowerRt Copy() => (PowerRt)MemberwiseClone();
    }

    /// <summary>화면이 읽는 강화 칩 — 주인 사도 · 이름(카드 이름) · 카드 id · 겹 · 규칙 글(겹을 뺀 한 겹의 글).</summary>
    public sealed class PowerView
    {
        public string Hero, Id, Name, Card;
        public int Stacks;
        public string Text;
    }

    /// <summary>
    /// 강화 카드 지속 규칙(2026-10-05 일곱째) — 효과 조각 <c>{ "k": "power", "id"?, "rules": [ 패시브 규칙 … ] }</c>.
    /// 카드를 내면 그 규칙들이 이 전투 끝까지 카드 주인의 패시브처럼 켜져 있다(when · conds · limit · fx 모두 패시브와 같다).
    /// 겹치기: 같은 사도가 같은 id(없으면 카드 이름)를 또 켜면 겹 +1 — 규칙은 처음 것 그대로, 발동할 때 효과가 겹 수만큼 돈다.
    /// 횟수 제한(limit)은 발동 수로 센다(겹이 2 여도 「턴당 1회」 는 한 번 발동 = 효과 두 번).
    /// 「항상」(always) 규칙의 증감은 겹 수만큼 곱해 늘 걸린다.
    /// 쪽지: powerOn(Hero · Id · Name · CardId · V = 겹).
    /// </summary>
    public sealed partial class Battle
    {
        /// <summary>켜진 강화 — 켠 차례대로.</summary>
        public List<PowerRt> Powers = new();
        Dictionary<string, List<RuleRt>> powerRules = new();

        // ── 화면 API ──────────────────────────────────────────────────
        /// <summary>켜진 강화 칩 목록(heroKey 를 주면 그 사도 것만) — 이름 · 겹 · 규칙 글.</summary>
        public List<PowerView> PowersOf(string heroKey = null)
        {
            var tx = new CardText(Data);
            return Powers.Where(p => heroKey == null || p.Hero == heroKey)
                .Select(p => new PowerView { Hero = p.Hero, Id = p.Id, Name = p.Name, Card = p.Card, Stacks = p.N, Text = tx.PowerRules(p.Rules) }).ToList();
        }

        /// <summary>그 사도의 그 강화 겹(없으면 0).</summary>
        public int PowerStacks(string heroKey, string id) => Powers.FirstOrDefault(p => p.Hero == heroKey && p.Id == id)?.N ?? 0;

        // ── 켜기 ──────────────────────────────────────────────────────
        bool FxPower(Fx f, FxCtx ctx)
        {
            if (f.K != FxK.Power) return false;
            var owner = ctx.Owner;
            if (owner != null && owner.Side == Side.Party && owner.BodyRef != null) AddPower(owner, f, ctx.CardId);
            return true;
        }

        /// <summary>강화를 켠다 — 같은 사도 · 같은 id 면 겹 +1.</summary>
        public void AddPower(Unit owner, Fx f, string cardId)
        {
            string baseId = cardId != null ? GameData.BaseId(cardId) : null;
            var card = baseId != null ? Data.Card(baseId) : null;
            string name = card?.Name ?? f.Id ?? "강화";
            string id = f.Id ?? name;
            var p = Powers.FirstOrDefault(x => x.Hero == owner.Key && x.Id == id);
            if (p == null) { p = new PowerRt { Hero = owner.Key, Id = id, Name = name, Card = baseId, N = 1, Rules = f.Rules ?? new List<PassiveRule>() }; Powers.Add(p); }
            else p.N++;
            Say($"{owner.Name} — 강화 「{name}」{(p.N > 1 ? $" {p.N}겹" : "")} (이 전투 동안)");
            Cue("powerOn", owner, new Cue { Hero = owner.Key, Id = id, Name = name, CardId = baseId, V = p.N });
        }

        /// <summary>그 강화의 규칙(RuleRt) — 강화마다 한 번 만들어 둔다.</summary>
        List<RuleRt> PowerRules(PowerRt p)
        {
            string key = p.Hero + "|" + p.Id;
            if (!powerRules.TryGetValue(key, out var l))
                powerRules[key] = l = (p.Rules ?? new List<PassiveRule>()).Where(r => r.When?.On != "always").Select(r => new RuleRt { R = r, Power = p.Name }).ToList();
            return l;
        }

        /// <summary>켜진 강화의 「항상」 증감(StatMod 가 더한다) — 대상 allAllies · party 면 파티 전원, otherAllies 면 주인 빼고, 아니면 주인만. 겹 수만큼.</summary>
        double PowerMod(Unit u, string stat)
        {
            if (Powers.Count == 0 || u == null || u.Side != Side.Party || u.BodyRef == null) return 0;
            double v = 0;
            foreach (var p in Powers)
                foreach (var r in p.Rules ?? new List<PassiveRule>())
                {
                    if (r.When?.On != "always") continue;
                    var owner = HeroUnit(p.Hero);
                    if (owner == null) continue;
                    if (r.Conds != null && r.Conds.Count > 0 && !CondOk(owner, r.Conds, null, new EmitInfo())) continue;
                    foreach (var f in r.Fx)
                    {
                        if (!MOD_STAT.TryGetValue(f.K, out var st) || st != stat) continue;
                        string tg = f.Target ?? "self";
                        bool hit = tg == "allAllies" || tg == "party" ? true : tg == "otherAllies" ? u.Key != p.Hero : u.Key == p.Hero;
                        if (hit) v += f.V * Math.Max(1, p.N);
                    }
                }
            return v;
        }
    }
}
