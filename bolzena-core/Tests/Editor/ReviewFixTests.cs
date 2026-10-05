using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>검토에서 나온 것의 회귀 시험.</summary>
    public class ReviewFixTests
    {
        [Test] public void 불러온_전투를_다시_저장해도_장비_효과가_남는다()
        {
            var rule = new PassiveRule { Name = "행운", When = new When { On = "turnStart" }, Fx = new List<Fx> { new Fx { K = "gauge", V = 11 } } };
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "dummy" }, st => st.GearRules = new Dictionary<string, List<PassiveRule>> { ["a"] = new List<PassiveRule> { rule } });
            var c = Battle.Load(b.Data, Battle.Load(b.Data, b.Save()).Save());
            c.EndTurn();
            Assert.AreEqual(22, c.Gauge);
        }

        [Test] public void 개막_카드가_먼저_자리를_떠나도_멈추지_않는다()
        {
            var d = K.Data(cards: "[{id:'c1', name:'막1', hero:'a', cost:0, type:'스킬', tags:['개막'], fx:[{k:'discard', v:9}]}, {id:'c2', name:'막2', hero:'a', cost:0, type:'스킬', tags:['개막'], fx:[{k:'gauge', v:5}]}]");
            Assert.DoesNotThrow(() => K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = new List<string> { "zero", "zero", "zero", "c2", "c1" }));
        }

        [Test] public void 다음_전투_사기는_사도_셋에게()
        {
            var b = K.Fight(K.Data(), new[] { "a", "b" }, new[] { "dummy" }, st => st.Next = new NextFight { Buff = new Dictionary<string, int> { ["사기"] = 1, ["불굴"] = 1 } });
            Assert.IsTrue(b.Party.All(u => b.St(u, "사기") == 1));
            Assert.AreEqual(1, b.St(b.Pool, "불굴"));
        }

        [Test] public void 턴_종료_시_AP는_다음_턴으로()
        {
            var d = K.Data(heroes: "[{id:'p', name:'여유', role:'서포터', row:'mid', hp:600, atk:80, def:50, crit:0, passives:[{name:'남김', when:{on:'turnEnd'}, fx:[{k:'ap', v:1}]}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" });
            b.EndTurn();
            Assert.AreEqual(4, b.Ap);
        }

        [Test] public void 금기_고유_카드는_보스_복제에_안_나온다()
        {
            var d = K.Sample();
            d.Add(null, "[{id:'taboo_u', name:'금기', hero:'rico', cost:1, type:'스킬', unique:true, tags:['금기'], fx:[{k:'draw', v:1}]}]", null, null, null, null);
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 1);
            run.S.Deck.Add("taboo_u");
            CollectionAssert.DoesNotContain(run.BossCopyOffer(), "taboo_u");
        }
    }
}
