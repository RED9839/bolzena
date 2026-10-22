using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// (2026-10-19 사용자 「조건부 추가 타격도 합쳐 최대 2타」) dmg plus · plusIf — 조건이 서면 친 대마다 비율 +plus(대 수는 그대로).
    /// ifKill orBoss — 처치했거나 고른 적이 살아 있는 보스면(보스전에서도 도는 처치 효과).
    /// </summary>
    public class PlusHitTests
    {
        const string HEROES = @"[
 {id:'p', name:'플', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, keyword:{name:'표', cap:9},
  forms:[{id:'pf', name:'덤 모드', turns:3}]}]";
        const string CARDS = @"[
 {id:'plus2', name:'두 대', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy', hits:2, plus:0.5, plusIf:{k:'ifForm', id:'pf'}}]},
 {id:'enter', name:'들기', hero:'p', cost:0, type:'스킬', fx:[{k:'form', id:'pf'}]},
 {id:'kb', name:'보스 처치', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'ifKill', id:'orBoss'}, {k:'stack', id:'표', v:1}]},
 {id:'bp', name:'보스 덤', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy', plus:0.5, plusIf:{k:'ifFoe', id:'boss'}}]},
 {id:'bpa', name:'보스 덤 전체', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'allEnemies', plus:0.5, plusIf:{k:'ifFoe', id:'boss'}}]},
 {id:'bad', name:'잘못', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy', plus:0.5, plusIf:{k:'draw', v:1}}]}]";
        const string FOES = @"[
 {id:'mob', name:'졸개', hp:5000, intents:[{t:'jam', v:0, rush:0}]},
 {id:'boss', name:'두목', hp:5000, boss:true, intents:[{t:'jam', v:0, rush:0}]}]";

        static Battle F(params string[] foes) => K.Fight(K.Data(heroes: HEROES, cards: CARDS, enemies: FOES), new[] { "p" }, foes);

        [Test] public void 조건_몫은_대를_늘리지_않고_친_대마다_더한다()
        {
            var b = F("mob");
            K.Hand(b, "plus2", "enter", "plus2");
            int h0 = K.Hp(b), n0 = b.HitsBy(K.E(b), "p");
            K.Play(b, "plus2");
            int off = h0 - K.Hp(b);
            Assert.AreEqual(2, b.HitsBy(K.E(b), "p") - n0, "조건이 안 서도 2대");
            K.Play(b, "enter");
            int h1 = K.Hp(b), n1 = b.HitsBy(K.E(b), "p");
            K.Play(b, "plus2");
            int on = h1 - K.Hp(b);
            Assert.AreEqual(2, b.HitsBy(K.E(b), "p") - n1, "조건이 서도 2대 그대로(3대가 아니다)");
            Assert.AreEqual(off * 1.5, on, 2, "대마다 100% → 150%");
            StringAssert.Contains("150%)", new CardText(b.Data).Card(b.CardOf("plus2")), "글: 「피해 100%(… 150%)」");
        }

        [Test] public void 처치하거나_보스면()
        {
            var b = F("mob");
            K.Hand(b, "kb"); K.Play(b, "kb");
            Assert.AreEqual(0, b.StackOf("p", "표"), "보스가 아닌 적을 처치 못 하면 안 돈다");
            K.E(b).Hp = 1; K.Hand(b, "kb"); K.Play(b, "kb");
            Assert.IsTrue(K.E(b).Dead);
            Assert.AreEqual(1, b.StackOf("p", "표"), "처치하면 돈다");

            var c = F("boss");
            K.Hand(c, "kb"); K.Play(c, "kb");
            Assert.IsFalse(K.E(c).Dead);
            Assert.AreEqual(1, c.StackOf("p", "표"), "보스면 처치 못 해도 돈다");
            StringAssert.Contains("처치하거나 보스면", new CardText(c.Data).Card(c.CardOf("kb")));
        }

        /// <summary>(2026-10-19 아야 빙설화도) plusIf ifFoe boss — 대상이 보스면 친 대에 더한다 · 적 전체면 보스 하나에게만.</summary>
        [Test] public void 대상이_보스면_덤()
        {
            var b = F("mob");
            K.Hand(b, "bp"); int h0 = K.Hp(b); K.Play(b, "bp");
            int mob = h0 - K.Hp(b);
            var c = F("boss");
            K.Hand(c, "bp"); int h1 = K.Hp(c); K.Play(c, "bp");
            Assert.AreEqual(mob * 1.5, h1 - K.Hp(c), 2, "보스면 100% → 150%");
            StringAssert.Contains("피해 100%(보스면 150%)", new CardText(c.Data).Card(c.CardOf("bp")));

            var d = F("mob", "boss");
            K.Hand(d, "bpa"); int m0 = K.Hp(d, 0), b0 = K.Hp(d, 1); K.Play(d, "bpa");
            int toMob = m0 - K.Hp(d, 0), toBoss = b0 - K.Hp(d, 1);
            Assert.AreEqual(mob, toMob, 2, "적 전체여도 보스가 아닌 적은 그대로");
            Assert.AreEqual(toMob * 1.5, toBoss, 2, "보스 하나에게만 150%");
            StringAssert.Contains("(보스에게 150%)", new CardText(d.Data).Card(d.CardOf("bpa")));
        }

        [Test] public void 검사기_plusIf_는_조건_조각만()
        {
            var v = Validator.Check(K.Data(heroes: HEROES, cards: CARDS, enemies: FOES));
            Assert.IsTrue(v.Errors.Any(e => e.Contains("plusIf 는 조건 조각")), string.Join("\n", v.Errors));
            Assert.IsFalse(v.Errors.Any(e => e.Contains("plus2") || e.Contains("kb") || e.Contains("bp")), string.Join("\n", v.Errors.Where(e => e.Contains("plus2") || e.Contains("kb") || e.Contains("bp"))));
        }
    }
}
