using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 일의 값을 고유 효과에 저장(stack ofEvent)하고 다음 턴에 그 값으로 고정 피해 · 실드(ofStack) — 2026-10-06 이드(재활) 「잠든 꿈」 · 키샤 「공연」.
    /// </summary>
    public class UltStoreTests
    {
        const string HERO = "{id:'s', name:'잠꾸러기', role:'탱커', row:'front', hp:3000, atk:100, def:50, crit:0, keywords:[" +
            "{name:'잠', carrier:'self', cap:1, rules:[" +
            "  {name:'저장', when:{on:'hurt', guarded:true}, conds:[{c:'stack', id:'잠', n:1}], fx:[{k:'stack', id:'아픔', v:1, ofEvent:0.1}]}," +
            "  {name:'깸', when:{on:'turnStart'}, conds:[{c:'stack', id:'잠', n:1}], fx:[{k:'shield', ratio:1, ofStack:'아픔', ofEvent:5}, {k:'dmg', ratio:1, ofStack:'아픔', ofEvent:2, target:'allEnemies'}]}," +
            "  {name:'깸', when:{on:'turnStart'}, conds:[{c:'stack', id:'잠', n:1}], fx:[{k:'spend', id:'아픔', all:true}, {k:'spend', id:'잠', all:true}]}]}," +
            "{name:'아픔', carrier:'self', cap:60}]}";
        const string CARDS = "[{id:'sleep', name:'잠들기', hero:'s', cost:0, type:'스킬', fx:[{k:'stack', id:'잠', v:1}]}]";

        [Test] public void 받은_피해를_저장했다가_다음_턴에_실드와_고정_피해로()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var b = K.Fight(d, new[] { "s" }, new[] { "hitter", "hitter" }); K.Hand(b, "sleep");
            K.Play(b, "sleep");
            b.EndTurn();   // 때리는 놈 둘 × 100 — 그 값 10당 1 → 아픔 20, 다음 턴 시작에 실드 20×5 · 적마다 20×2 고정 피해
            Assert.AreEqual(100, b.Pool.Shield, "아픔 20 × 5");
            Assert.AreEqual(1000 - 40, K.Hp(b, 0));
            Assert.AreEqual(1000 - 40, K.Hp(b, 1));
            Assert.AreEqual(0, b.StackOf("s", "아픔"));
            Assert.AreEqual(0, b.StackOf("s", "잠"));
        }

        [Test] public void 잠들지_않았으면_저장하지_않는다()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var b = K.Fight(d, new[] { "s" }, new[] { "hitter" });
            b.EndTurn();
            Assert.AreEqual(0, b.StackOf("s", "아픔"));
            Assert.AreEqual(1000, K.Hp(b));
        }

        [Test] public void 글_그_값_10당과_1당_고정()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var tx = new CardText(d);
            var rules = d.Hero("s").AllKeywords.First(k => k.Name == "잠").Rules;
            StringAssert.Contains("그 값 10당 「아픔」 +1", tx.Fx(rules[0].Fx));
            StringAssert.Contains("「아픔」 1당 5 고정 실드", tx.Fx(rules[1].Fx));
            StringAssert.Contains("「아픔」 1당 2 고정 피해", tx.Fx(rules[1].Fx));
        }
    }
}
