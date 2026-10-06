using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 숙련 봇의 판 짜기 눈(2026-10-06) — 파티의 「축」 을 세어 카드 · 신탁 · 장비를 고를 때 시너지 점수를 더한다.
    ///   축 = 파티 사도들이 내는 것(Gives — 상태 · 고유 효과 겹 · 태그 · 일)과 듣는 것(Hears — 그 겹 · 상태 · 태그 · 일에 반응하는 규칙 · 비례).
    ///   카드 한 장의 시너지 = 그 카드가 내는 것 가운데 파티가 듣는 가짓수 + 그 카드가 듣는 것 가운데 파티가 내는 가짓수.
    /// 값의 눈금은 CardValue.Efficiency(코스트당 값어치, 보통 카드 ≈ 1) 와 같다. 난수를 쓰지 않는다.
    /// </summary>
    public sealed class DeckPlan
    {
        readonly GameData d;
        public readonly HashSet<string> Gives = new(), Hears = new();
        /// <summary>파티 사도의 고유 효과(키워드) 이름.</summary>
        public readonly HashSet<string> Keywords = new();

        /// <summary>시너지 한 가짓수의 값 · 셀 가짓수 위 끝 · 고유 카드 덤 · 강화(전투 내내 남는) 카드 덤 · 겹 쌓기 덤(겹마다).</summary>
        public const double SYN = 0.12, UNIQUE = 0.25, POWER = 0.15, STACK = 0.05;
        public const int SYN_CAP = 5;
        /// <summary>덱이 이 장수를 넘으면 새 카드(교주 카드 따위)를 받는 문턱이 올라간다 — 한 장마다 THICK_STEP.</summary>
        public const int THICK = 14;
        public const double THICK_STEP = 0.04;

        public DeckPlan(GameData d, IEnumerable<string> party)
        {
            this.d = d;
            foreach (var k in party)
            {
                var h = d.Hero(k); if (h == null) continue;
                Gives.UnionWith(HeroGives(d, h));
                Hears.UnionWith(HeroHears(d, h));
            }
        }

        // ── 사도의 축 ──────────────────────────────────────────────────
        /// <summary>사도가 내는 것 — MetaSim.Gives + 고유 효과 겹을 쌓는 카드 · 패시브(「kw:이름」).</summary>
        public static HashSet<string> HeroGives(GameData d, HeroDef h)
        {
            var o = MetaSim.Gives(d, h);
            foreach (var c in d.Cards.Values.Where(c => c.Hero == h.Id))
            {
                o.UnionWith(FxGives(c.Fx));
                foreach (var or in c.Oracles) o.UnionWith(FxGives(or.Fx));
            }
            foreach (var r in h.Passives) o.UnionWith(FxGives(r.Fx));
            if (h.Ult != null) o.UnionWith(FxGives(h.Ult.Fx));
            return o;
        }

        /// <summary>사도가 듣는 것 — MetaSim.Hears + 자기 고유 효과(겹을 쌓으면 규칙 · 최대 효과가 돈다).</summary>
        public static HashSet<string> HeroHears(GameData d, HeroDef h)
        {
            var o = MetaSim.Hears(d, h);
            foreach (var k in h.AllKeywords) if (k.Name != null) o.Add("kw:" + k.Name);
            foreach (var r in h.Passives) foreach (var c in r.Conds) if (c.C == "stack" && c.Id != null) o.Add("kw:" + c.Id);
            foreach (var c in d.Cards.Values.Where(c => c.Hero == h.Id)) o.UnionWith(FxHears(c.Fx, c.Tags));
            return o;
        }

        static IEnumerable<Fx> Walk(IEnumerable<Fx> l)
        {
            foreach (var f in l ?? Enumerable.Empty<Fx>())
            {
                yield return f;
                foreach (var x in Walk(f.Then)) yield return x;
                foreach (var x in Walk(f.Else)) yield return x;
                if (f.Rules != null) foreach (var r in f.Rules) foreach (var x in Walk(r.Fx)) yield return x;
            }
        }

        /// <summary>효과가 내는 것(상태 · 겹 · 일).</summary>
        public static HashSet<string> FxGives(IEnumerable<Fx> fx)
        {
            var o = new HashSet<string>();
            foreach (var f in Walk(fx))
            {
                switch (f.K)
                {
                    case FxK.Status: if (f.Id != null) { o.Add("st:" + f.Id); if (R.IsBadSt(f.Id)) o.Add("ev:debuff"); } break;
                    case FxK.Stack: if (f.Id != null && f.V > 0) o.Add("kw:" + f.Id); break;
                    case FxK.Feed: if (f.Id != null) o.Add("kw:" + f.Id); break;
                    case FxK.Extra: o.Add("ev:extra"); break;
                    case FxK.Make: o.Add("ev:make"); break;
                    case FxK.Discard: o.Add("ev:discard"); break;
                    case FxK.Burn: case FxK.ExileFrom: o.Add("ev:exhaust"); break;
                    case FxK.Shield: case FxK.Block: o.Add("ev:guard"); break;
                    case FxK.Heal: o.Add("ev:heal"); break;
                    case FxK.Tough: o.Add("ev:break"); break;
                    case FxK.TakenMod: if (f.V > 0) o.Add("ev:debuff"); break;
                    case FxK.DealtMod: if (f.V < 0) o.Add("ev:debuff"); break;
                }
            }
            return o;
        }

        /// <summary>효과가 듣는 것(겹 · 태그 · 상태 · 일에 비례하거나 조건이 걸린 것).</summary>
        public static HashSet<string> FxHears(IEnumerable<Fx> fx, IEnumerable<string> tags = null)
        {
            var o = new HashSet<string>();
            foreach (var f in Walk(fx))
            {
                switch (f.K)
                {
                    case FxK.PerStack: case FxK.Spend: if (f.Id != null) o.Add("kw:" + f.Id); break;
                    case FxK.IfStack: if (f.Id != null && !f.Not) o.Add("kw:" + f.Id); break;
                    case FxK.PerTag: case FxK.PerPlayed: if (f.Id != null) o.Add("tag:" + f.Id); break;
                    case FxK.IfDebuffs: case FxK.PerDebuff: o.Add("ev:debuff"); break;
                    case FxK.IfBreak: o.Add("ev:break"); break;
                    case FxK.IfFoe: if (f.Id == "broken") o.Add("ev:break"); else if (f.Id != null) o.Add("st:" + f.Id); break;
                    case FxK.IfShield: if (!f.Not) o.Add("ev:guard"); break;
                    case FxK.When: if (f.On == "passion") o.Add("tag:" + Tag.Passion); break;
                }
                if (f.Rules != null)
                    foreach (var r in f.Rules)
                    {
                        if (r.When?.Tag != null) o.Add("tag:" + r.When.Tag);
                        switch (r.When?.On)
                        {
                            case "debuff": o.Add("ev:debuff"); break;
                            case "extra": o.Add("ev:extra"); break;
                            case "make": o.Add("ev:make"); break;
                            case "discard": o.Add("ev:discard"); break;
                            case "exhaust": o.Add("ev:exhaust"); break;
                            case "break": o.Add("ev:break"); break;
                            case "guard": o.Add("ev:guard"); break;
                            case "overheal": o.Add("ev:heal"); break;
                        }
                        foreach (var c in r.Conds) if (c.C == "stack" && c.Id != null) o.Add("kw:" + c.Id); else if (c.C == "status" && c.Id != null) o.Add("st:" + c.Id);
                    }
            }
            return o;
        }

        static HashSet<string> CardGives(CardView c)
        {
            var o = FxGives(c.Fx);
            foreach (var t in c.Tags) o.Add("tag:" + Tag.Parse(t).id);
            return o;
        }

        // ── 카드 · 신탁 · 장비 점수 ────────────────────────────────────
        /// <summary>그 카드의 시너지 가짓수(SYN_CAP 까지).</summary>
        public int Hits(CardView c)
        {
            if (c == null) return 0;
            int n = CardGives(c).Count(x => Hears.Contains(x)) + FxHears(c.Fx).Count(x => Gives.Contains(x));
            return Math.Min(SYN_CAP, n);
        }

        /// <summary>
        /// 고를 때 쓰는 값 — 코스트당 값어치 + 시너지 + 고유 카드 덤 + 강화(전투 내내 남는) 덤 + 파티 고유 효과 겹을 쌓는 덤.
        /// 저주 · 상태는 아주 낮다.
        /// </summary>
        public double Score(CardView c)
        {
            if (c == null) return -9;
            if (c.IsCurse || c.IsStatus) return -2;
            double v = CardValue.Efficiency(c) + SYN * Hits(c);
            if (c.Unique) v += UNIQUE;
            if (c.IsPower) v += POWER;
            v += STACK * Math.Min(4, c.Fx.Where(f => f.K == FxK.Stack && f.V > 0 && f.Id != null && Hears.Contains("kw:" + f.Id)).Sum(f => f.V));
            return v;
        }

        /// <summary>덱 장수에 따른 「새 카드를 받을 만한가」 문턱 — 얇을수록 낮다.</summary>
        public static double TakeBar(int deck) => 1.05 + THICK_STEP * Math.Max(0, deck - THICK);

        /// <summary>장비 효과가 파티 축에 맞는 가짓수(효과의 상태 · 일 · 계기).</summary>
        public int GearHits(EquipDef e)
        {
            if (e == null) return 0;
            var rules = e.Effect.Concat(e.AffinityEffect ?? new List<PassiveRule>()).ToList();
            if (rules.Count == 0) return 0;
            var g = FxGives(rules.SelectMany(r => r.Fx));
            var h = FxHears(new[] { new Fx { K = FxK.Power, Rules = rules } });
            return Math.Min(3, g.Count(x => Hears.Contains(x)) + h.Count(x => Gives.Contains(x)));
        }
    }
}
