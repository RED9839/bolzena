using System.Collections.Generic;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>봇의 장비 · 교주 카드 눈(2026-10-07) — 장비 효과 값어치(CardValue.GearWorth) · 증폭 짝(DeckPlan.AMP) · 판에 맞춘 실드 · 회복 값(CardValue.LiveGuard, 10-08).</summary>
    public class GearBotTests
    {
        static PassiveRule Rule(string on, List<Fx> fx, Limit limit = null, int every = 0, string type = null, int? maxCost = null)
            => new PassiveRule { When = new When { On = on, Every = every, Type = type, MaxCost = maxCost }, Fx = fx, Limit = limit };

        [Test] public void 효과가_크면_장비_값어치가_크다()
        {
            var small = new List<PassiveRule> { Rule("play", new List<Fx> { new Fx { K = FxK.Extra, Ratio = 0.5, Target = "oneEnemy" } }, every: 5, type: "공격") };
            var big = new List<PassiveRule> { Rule("play", new List<Fx> { new Fx { K = FxK.Extra, Ratio = 1.5, Target = "randomEnemy" } }, every: 3, type: "공격") };
            Assert.Greater(CardValue.GearWorth(big), CardValue.GearWorth(small));
            Assert.AreEqual(0, CardValue.GearWorth(new List<PassiveRule>()));
        }

        [Test] public void 전투_시작_효과는_한_번만_센다()
        {
            var fx = new List<Fx> { new Fx { K = FxK.Shield, Ratio = 1.5 } };
            Assert.AreEqual(CardValue.ValueOf(fx), CardValue.GearWorth(new List<PassiveRule> { Rule("fightStart", fx) }), 1e-9);
            Assert.Greater(CardValue.GearWorth(new List<PassiveRule> { Rule("turnStart", fx) }), CardValue.GearWorth(new List<PassiveRule> { Rule("fightStart", fx) }));
        }

        [Test] public void 치유_증감도_값이_있다()
        {
            var r = Rule("fightStart", new List<Fx> { new Fx { K = FxK.HealMod, V = 0.4, Run = true } });
            Assert.Greater(CardValue.GearWorth(new List<PassiveRule> { r }), 0);
        }

        [Test] public void 비용_0_계기는_덜_돈다()
        {
            var fx = new List<Fx> { new Fx { K = FxK.Extra, Ratio = 0.6, Target = "oneEnemy" } };
            var any = Rule("play", fx, new Limit { Per = "turn", N = 2 });
            var zero = Rule("play", fx, new Limit { Per = "turn", N = 2 }, maxCost: 0);
            Assert.Less(CardValue.GearWorth(new List<PassiveRule> { zero }), CardValue.GearWorth(new List<PassiveRule> { any }));
        }

        [Test] public void 실드는_막을_피해만큼_회복은_잃은_HP_만큼()
        {
            // 실드 계수 1.4 · 계수 1 의 실드 50 → 실드 70. 공격력 100(카드 값 1 = 피해 120)
            double none = CardValue.LiveGuard(1.4, 0, 50, 50, need: 0, missing: 0, atk: 100);
            double some = CardValue.LiveGuard(1.4, 0, 50, 50, need: 30, missing: 0, atk: 100);
            double full = CardValue.LiveGuard(1.4, 0, 50, 50, need: 500, missing: 0, atk: 100);
            Assert.AreEqual(-(1.4 / 2 * 0.8), none, 1e-9, "막을 피해가 없으면 정적 실드 값만큼 깎인다");
            Assert.Less(none, some); Assert.Less(some, full);
            Assert.AreEqual(full, CardValue.LiveGuard(1.4, 0, 50, 50, need: 70, missing: 0, atk: 100), 1e-9, "실드량을 넘는 예고 피해는 더 쳐 주지 않는다");
            // 회복 — 잃은 HP 가 없으면 값이 없고, 잃은 만큼까지만
            Assert.Less(CardValue.LiveGuard(0, 1.0, 50, 50, 0, missing: 0, atk: 100), CardValue.LiveGuard(0, 1.0, 50, 50, 0, missing: 20, atk: 100));
            Assert.AreEqual(CardValue.LiveGuard(0, 1.0, 50, 50, 0, missing: 50, atk: 100), CardValue.LiveGuard(0, 1.0, 50, 50, 0, missing: 999, atk: 100), 1e-9);
            Assert.AreEqual(0, CardValue.LiveGuard(0, 0, 50, 50, 100, 100, 100));
        }

        [Test] public void 증폭_짝_고통이면_고통_각인을_듣는다()
        {
            Assert.That(DeckPlan.AMP, Has.Member(("st:고통 각인", "st:고통")));
            Assert.That(DeckPlan.AMP, Has.Member(("st:결의", "ev:guard")));
        }
    }
}
