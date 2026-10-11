using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 계기 넓히기 · 카제나 이름(2026-10-10) — play 의 chain(연속) · sameHero, debuff 의 id, pulled(손으로 끌어옴),
    /// 탄환(버려지면 추가 공격), 규칙 머리의 카제나 이름(감응 · 연계 …), 완성형(complete) 검사.
    /// </summary>
    public class TriggerKazenaTests
    {
        const string HEROES = "[{id:'n1', name:'순1', role:'딜러', nature:'순수', row:'back', hp:500, atk:100, def:50, crit:0}," +
            "{id:'n2', name:'순2', role:'딜러', nature:'순수', row:'back', hp:500, atk:100, def:50, crit:0}," +
            "{id:'w', name:'보는 이', role:'서포터', nature:'광기', row:'mid', hp:500, atk:100, def:50, crit:0, passives:[" +
            "{name:'연속 보기', when:{on:'play', who:'any', chain:true}, fx:[{k:'gauge', v:10}]}]}]";
        const string CARDS = "[{id:'p1', name:'순1 카드', hero:'n1', cost:0, type:'스킬', fx:[]}," +
            "{id:'p2', name:'순2 카드', hero:'n2', cost:0, type:'스킬', fx:[]}," +
            "{id:'pw', name:'광기 카드', hero:'w', cost:0, type:'스킬', fx:[]}," +
            "{id:'weak', name:'약화 걸기', hero:'n1', cost:0, type:'스킬', fx:[{k:'status', id:'약화', v:1, target:'oneEnemy'}]}," +
            "{id:'vul', name:'취약 걸기', hero:'n1', cost:0, type:'스킬', fx:[{k:'status', id:'취약', v:1, target:'oneEnemy'}]}," +
            "{id:'bul', name:'탄알', hero:'n1', cost:0, type:'공격', tags:['탄환'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]}," +
            "{id:'toss', name:'던지기', hero:'n1', cost:0, type:'스킬', fx:[{k:'discard', v:1}]}]";

        [Test] public void 연속_앞_카드와_성격이_같을_때만()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS);
            var b = K.Fight(d, new[] { "n1", "n2", "w" }, new[] { "dummy" }, st => st.EnemyHp = 100000);
            int g0 = b.Gauge;
            K.Hand(b, "p1", "pw", "p2");
            K.Play(b, "p1"); Assert.AreEqual(g0, b.Gauge, "첫 카드는 앞 카드가 없다");
            K.Play(b, "pw"); Assert.AreEqual(g0, b.Gauge, "순수 → 광기는 연속이 아니다");
            K.Hand(b, "p1", "p2");
            K.Play(b, "p1"); int g1 = b.Gauge;
            K.Play(b, "p2"); Assert.Greater(b.Gauge, g1, "순수 → 순수(다른 사도라도)는 연속");
        }

        [Test] public void 같은_사도_잇달아()
        {
            var hs = HEROES.Replace("chain:true", "sameHero:true");
            var d = K.Data(heroes: hs, cards: CARDS);
            var b = K.Fight(d, new[] { "n1", "n2", "w" }, new[] { "dummy" }, st => st.EnemyHp = 100000);
            K.Hand(b, "p1", "p2", "weak");
            K.Play(b, "p1"); int g0 = b.Gauge;
            K.Play(b, "p2"); Assert.AreEqual(g0, b.Gauge, "성격이 같아도 다른 사도면 아니다");
            K.Hand(b, "p1", "weak");
            K.Play(b, "p1"); int g1 = b.Gauge;
            K.Play(b, "weak"); Assert.Greater(b.Gauge, g1, "같은 사도의 카드가 잇달면 돈다");
        }

        [Test] public void 디버프_종류를_가린다()
        {
            var hs = HEROES.Replace("{on:'play', who:'any', chain:true}", "{on:'debuff', who:'any', id:'취약'}");
            var d = K.Data(heroes: hs, cards: CARDS);
            var b = K.Fight(d, new[] { "n1", "w" }, new[] { "dummy" }, st => st.EnemyHp = 100000);
            int g0 = b.Gauge;
            K.Hand(b, "weak"); K.Play(b, "weak");
            Assert.AreEqual(g0, b.Gauge, "약화는 아니다");
            K.Hand(b, "vul"); K.Play(b, "vul");
            Assert.Greater(b.Gauge, g0, "취약이면 돈다");
            StringAssert.Contains("「취약」", new CardText(d).WhenText(d.Hero("w").Passives[0].When));
        }

        [Test] public void 탄환은_버려지면_쏜다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS);
            var b = K.Fight(d, new[] { "n1" }, new[] { "dummy" }, st => st.EnemyHp = 100000);
            int hp0 = K.Hp(b);
            K.Hand(b, "toss", "bul");
            K.Play(b, "toss", o: new PlayOpts { Discard = new System.Collections.Generic.List<string> { "bul" } });
            Assert.Less(K.Hp(b), hp0, "탄환이 버려지면 추가 공격");
            Assert.IsTrue(b.Discard.Contains("bul"));
        }

        [Test] public void 계기_카제나_이름()
        {
            var hs = "[{id:'k', name:'카', role:'딜러', row:'back', hp:500, atk:100, def:50, crit:0, keywords:[{name:'별', carrier:'self', rules:[" +
                "{name:'뽑힘', when:{on:'drawn', who:'any'}, fx:[{k:'stack', id:'별', v:1}]}," +
                "{name:'남의 카드', when:{on:'play', who:'other', type:'공격'}, fx:[{k:'stack', id:'별', v:1}]}," +
                "{name:'처치', when:{on:'kill', mine:true}, fx:[{k:'stack', id:'별', v:1}]}]}]}]";
            var d = K.Data(heroes: hs);
            string t = new CardText(d).Trait(d.Hero("k"), d.Hero("k").AllKeywords.First());
            StringAssert.Contains("감응:", t);
            StringAssert.Contains("연계(공격):", t);
            StringAssert.Contains("파괴(자신):", t);
        }

        [Test] public void 완성형은_신탁이_없어도_된다()
        {
            var ok = K.Data(cards: "[{id:'cu', name:'완성', hero:'a', cost:1, type:'공격', unique:true, complete:true, fx:[{k:'dmg', ratio:1.3, target:'oneEnemy'}]}]");
            var r = Validator.Check(ok);
            Assert.IsFalse(r.Warnings.Any(w => w.Contains("cu") && w.Contains("신탁이 없다")), "완성형은 신탁 없음 주의가 없다");
            var plain = K.Data(cards: "[{id:'cu', name:'완성', hero:'a', cost:1, type:'공격', unique:true, fx:[{k:'dmg', ratio:1.3, target:'oneEnemy'}]}]");
            Assert.IsTrue(Validator.Check(plain).Warnings.Any(w => w.Contains("cu") && w.Contains("신탁이 없다")), "완성형이 아니면 주의");
        }
    }
}
