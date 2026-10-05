using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>비용 증감 하나(costMod) — 거르개에 맞는 카드의 비용 ±V. 턴 Until 까지 · Uses 장(0 = 그 동안 전부).</summary>
    public sealed class CostModRec
    {
        public int V, Until, Uses;
        public string Owner, Who, Type, Tag;
        public bool Unique, Basic;
        public CostModRec Copy() => (CostModRec)MemberwiseClone();
    }

    /// <summary>
    /// v2 콘텐츠 요구(bolzena-content-v2/NEEDS.md, 2026-10-05 넷째) — 카드 거르개 · 비용 증감 · 배타 무작위 · 다시 내기 · 태그 덧붙임 ·
    /// 한 대 깎기 · AP 빚 탕감 · 새 대상(otherEnemy · nextEnemy · slowestEnemy · markedEnemy · strongestAlly · hero:키).
    /// </summary>
    public sealed partial class Battle
    {
        public List<CostModRec> CostMods = new();
        /// <summary>이번 턴 debt 로 진 AP 빚(clearDebt 가 탕감).</summary>
        public int DebtNow;
        /// <summary>파티 실드를 마지막으로 준 사도(shieldBreak 의 By).</summary>
        public string ShieldBy;
        /// <summary>cutHit — 적의 다음 한 대를 이 비율만큼 깎는다(그 한 대에 쓰인다).</summary>
        public double CutNext;
        /// <summary>이 전투에서만 쓰는 카드 값 이름(cardStatus battle) — 판에 안 넘긴다.</summary>
        public HashSet<string> BattleVals = new();
        /// <summary>지난 턴 사도마다 낸 장수(조건 idleLast).</summary>
        public Dictionary<string, int> PlayedPrev = new();

        // ── 카드 거르개 ──────────────────────────────────────────────
        /// <summary>효과 조각의 카드 거르개(who · type · unique · basic · tag)에 그 카드가 맞나. 거르개가 없으면 늘 맞다.</summary>
        public bool CardMatch(string id, string who, string type, bool unique, bool basic, string tag, Unit owner)
        {
            var c = CardOf(id);
            if (c == null) return false;
            if (type != null && c.Type != type) return false;
            if (who == "self" && (owner == null || c.Hero != owner.Key)) return false;
            if (who == "other" && (c.Hero == null || (owner != null && c.Hero == owner.Key))) return false;
            if (who != null && who != "self" && who != "other" && c.Hero != who) return false;
            if (unique && !c.Unique) return false;
            if (basic && !(c.Def.Hero != null && !c.Unique && !GameData.IsCopy(id) && !GameData.IsPlain(id) && !c.Def.Token)) return false;
            if (tag != null && !HasTagB(id, tag)) return false;
            return true;
        }
        bool FxMatch(Fx f, string id, Unit owner) => CardMatch(id, f.Who, f.K == FxK.IfPrev ? null : f.Type, f.Unique, f.Basic, f.Tag, owner);
        static bool HasFilter(Fx f) => f.Who != null || f.Type != null || f.Unique || f.Basic || f.Tag != null;

        // ── 비용 증감 ─────────────────────────────────────────────────
        int CostModOf(string id)
        {
            int v = 0;
            foreach (var m in CostMods)
                if (m.Until >= Turn && CardMatch(id, m.Who, m.Type, m.Unique, m.Basic, m.Tag, HeroUnit(m.Owner))) v += m.V;
            return v;
        }
        void UseCostMods(string id)
        {
            foreach (var m in CostMods.ToList())
                if (m.Uses > 0 && m.Until >= Turn && CardMatch(id, m.Who, m.Type, m.Unique, m.Basic, m.Tag, HeroUnit(m.Owner)) && --m.Uses <= 0) CostMods.Remove(m);
            CostMods.RemoveAll(m => m.Until < Turn);
        }

        // ── 대상(새것) ────────────────────────────────────────────────
        List<Unit> ResolveV2(FxCtx ctx, string target, List<Unit> foes, List<Unit> allies, Fx f)
        {
            switch (target)
            {
                case "otherEnemy":
                    {   // 방금 그 적(일을 당한 적 · 고른 적)이 아닌 무작위 적
                        var not = ctx.Holder != null && ctx.Holder.Side == Side.Enemy ? ctx.Holder : foes.FirstOrDefault(e => e.Idx == ctx.TargetIdx);
                        var rest = foes.Where(e => e != not).ToList();
                        return rest.Count > 0 ? new List<Unit> { rest[PreviewPick != null ? 0 : Rng.Int(rest.Count)] } : new List<Unit>();
                    }
                case "nextEnemy":
                case "slowestEnemy":
                    {   // 행동 카운트가 가장 작은 · 큰 적(당겨지지 않는 적은 뒤로)
                        var withC = foes.Select(e => (e, n: ActionCount(e) ?? 99)).ToList();
                        var pick = target == "nextEnemy" ? withC.OrderBy(x => x.n).ThenBy(x => x.e.Idx).FirstOrDefault() : withC.OrderByDescending(x => x.n == 99 ? -1 : x.n).ThenBy(x => x.e.Idx).FirstOrDefault();
                        return pick.e != null ? new List<Unit> { pick.e } : new List<Unit>();
                    }
                case "markedEnemy":
                    {   // 그 표식(id)을 가장 많이 든 적 — 없으면 고른 적
                        var t = f?.Id != null ? foes.Where(e => St(e, f.Id) > 0).OrderByDescending(e => St(e, f.Id)).ThenBy(e => e.Idx).FirstOrDefault() : null;
                        t ??= foes.FirstOrDefault(e => e.Idx == ctx.TargetIdx) ?? foes.FirstOrDefault();
                        return t != null ? new List<Unit> { t } : new List<Unit>();
                    }
                case "strongestAlly":
                    {
                        var t = allies.Where(u => u != ctx.Owner).OrderByDescending(AtkNow).FirstOrDefault() ?? ctx.Owner;
                        return t != null ? new List<Unit> { t } : new List<Unit>();
                    }
            }
            if (target != null && target.StartsWith("hero:"))
            {   // 그 사도(있으면) — 없으면 공격력이 가장 높은 다른 아군
                var t = allies.FirstOrDefault(u => u.Key == target.Substring(5)) ?? allies.Where(u => u != ctx.Owner).OrderByDescending(AtkNow).FirstOrDefault() ?? ctx.Owner;
                return t != null ? new List<Unit> { t } : new List<Unit>();
            }
            return null;
        }

        /// <summary>v2 효과 조각 — 처리했으면 true.</summary>
        bool FxV2(Fx f, FxCtx ctx, ref bool gate)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                case FxK.Roll: ctx.Roll = 1 + Rng.Int(Math.Max(2, f.N)); return true;
                case FxK.IfRoll: gate = ctx.Roll == f.N; return true;
                case FxK.IfPrevSame:
                    {   // 바로 앞 카드(누구 것이든)와 종류가 같으면 — not 이면 다르면
                        int at = PlayLog.Count - (ctx.Card || ctx.Passive != null ? 2 : 1);   // 이 카드(· 계기가 된 카드)는 이미 PlayLog 끝에 있다
                        var prev = at >= 0 && at < PlayLog.Count ? PlayLog[at].type : null;
                        gate = prev != null && prev == ctx.Type; if (f.Not) gate = prev != null && !gate;
                        return true;
                    }
                case FxK.IfInHand: gate = Hand.Any(h => GameData.BaseId(h) == f.Id); if (f.Not) gate = !gate; return true;   // 낸 카드는 이미 손을 떠났다
                case FxK.IfBond: gate = ctx.CardId != null && BondOf(ctx.CardId) >= Math.Max(1, f.N); return true;
                case FxK.IfLastMine: gate = owner != null && PlayLog.Count > 0 && PlayLog[PlayLog.Count - 1].hero == owner.Key; if (f.Not) gate = !gate; return true;
                case FxK.IfPulled: gate = ctx.Pulled != null && FxMatch(f, ctx.Pulled, owner); return true;
                case FxK.IfShield: gate = Pool.Block + Pool.Shield > 0; if (f.Not) gate = !gate; return true;
                case FxK.IfDebt: gate = DebtNow > 0 || ApJam > 0; return true;
                case FxK.IfTypeNew:
                    {   // 이 카드의 종류가 이번 턴 파티의 첫 장이면
                        int n = PlayLog.Count(p => p.type == ctx.Type);
                        gate = ctx.Type != null && n <= (ctx.Card && ctx.Passive == null && PlayLog.Count > 0 && PlayLog[PlayLog.Count - 1].type == ctx.Type ? 1 : 0);
                        return true;
                    }
                case FxK.Recast:
                    {   // 방금 낸 이 카드의 효과를 ratio 배로 한 번 더(다시 내기 안에서는 안 돈다)
                        if (ctx.Recasting || ctx.CardId == null) return true;
                        var c = CardOf(ctx.CardId); if (c == null) return true;
                        var c2 = ctx.Copy(); c2.Recasting = true; c2.Scale = ctx.Scale * (f.Ratio > 0 ? f.Ratio : 0.5); c2.Toughed = null;
                        Say($"「{c.Name}」 — 한 번 더({Num.Round(c2.Scale * 100)}%)");
                        RunFx(c.Fx.Where(x => x.K != FxK.Recast).ToList(), c2);
                        return true;
                    }
                case FxK.CostMod:
                    CostMods.Add(new CostModRec { V = f.IV, Until = Turn + f.TurnsOr1 - 1, Uses = Math.Max(0, f.N), Owner = owner?.Key ?? Acting, Who = f.Who, Type = f.Type, Tag = f.Tag, Unique = f.Unique, Basic = f.Basic });
                    Say($"비용 {(f.IV > 0 ? "+" : "")}{f.IV}{(f.N > 0 ? $" ({f.N}장)" : "")}");
                    return true;
                case FxK.AddTag:
                    (ctx.ExtraTags ??= new HashSet<string>()).Add(f.Id);
                    if (f.Id == Tag.WeakHit || f.Id == Tag.Weak) ctx.HitTags?.Add(Tag.Weak);
                    if (f.Id == Tag.Crush) ctx.HitTags?.Add(Tag.Crush);
                    return true;
                case FxK.CutHit: CutNext = Math.Max(CutNext, f.V); Say($"다음 한 대 -{Num.Round(f.V * 100)}%"); return true;
                case FxK.ClearDebt:
                    if (ApJam > 0) { Say($"빚 탕감 — 다음 턴 AP -{ApJam} 이 사라진다"); ApJam = 0; DebtNow = 0; StatusCue(owner ?? PartyRep(), "빚 탕감", true); }
                    return true;
                case FxK.HealMod:
                    foreach (var t in Resolve(ctx, f.Target ?? "self")) AddModFx(t, "heal", f.V, f.Run ? R.BOON_TURNS : f.TurnsOr1, f.Run);
                    return true;
            }
            return false;
        }

        /// <summary>그 사도의 태그 덧붙임(키워드 tagWhile) — 겹이 있는 동안 그 사도 카드에 태그.</summary>
        bool KwTag(string id, string tag)
        {
            if (Kw.Count == 0) return false;
            var c = Data.View(id, Flash.TryGetValue(id, out var n) ? n : 0);
            if (c?.Hero == null) return false;
            foreach (var kw in Kw.Values)
                if (kw.Def.TagWhile == tag && kw.Owner == c.Hero && (c.Type == (kw.Def.TagType ?? "공격")) && StackOf(kw.Owner, kw.Id) > 0) return true;
            return false;
        }
    }
}

namespace Bolzena.Core
{
    /// <summary>
    /// 신탁 고르기 창의 후보 하나 — 전투 번뜩임(b.EpiphanyOptions) · 이벤트/캠프(run.FlashOptions)가 같은 꼴로 준다.
    /// N(신탁 번호 1~5) · Name · Text(그 신탁의 카드 글) · Shin(얹힌 축복 꼴 — null 이면 없음) · BlessName · BlessText.
    /// </summary>
    public sealed class OracleOption
    {
        public int N;
        public string Name, Text, Shin, BlessName, BlessText;
        public bool Blessed => Shin != null;

        public static System.Collections.Generic.List<OracleOption> Of(GameData d, string cardId, System.Collections.Generic.IEnumerable<GlowPick> picks)
        {
            var o = new System.Collections.Generic.List<OracleOption>();
            var c = d.Card(cardId); if (c == null) return o;
            var tx = new CardText(d);
            foreach (var p in picks)
            {
                if (p.N < 1 || p.N > c.Oracles.Count) continue;
                var or = c.Oracles[p.N - 1];
                var b = Battle.BlessOf(c, p.Shin);
                o.Add(new OracleOption
                {
                    N = p.N, Name = or.Name, Text = tx.Oracle(c, or), Shin = p.Shin,
                    BlessName = p.Shin == null ? null : b?.Name ?? R.DIVINE_NAME,
                    BlessText = p.Shin == null ? null : b != null ? tx.Bless(b) : (R.DIVINE_KO.TryGetValue(p.Shin, out var k) ? k : p.Shin),
                });
            }
            return o;
        }
    }

    public sealed partial class Battle
    {
        /// <summary>빛나는 카드의 신탁 후보 셋(축복 포함) — 카드 신탁일 때. 은총(hero)이거나 빛이 없으면 빈 목록. 고른 차례 번호를 ApplyEpiphany 에.</summary>
        public System.Collections.Generic.List<OracleOption> EpiphanyOptions(string cardId)
        {
            var g = GlowOf(cardId);
            return g == null || g.Kind != "card" ? new System.Collections.Generic.List<OracleOption>() : OracleOption.Of(Data, cardId, g.Picks);
        }
    }
}
