using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// (2026-10-14 클로에 「메리 고 라운드」) 변신 덤 hits — 변신 중 그 사도 (고유) 공격 카드의 피해 조각을 n연타로 ·
    /// hit each — 카드 한 장에 한 번이 아니라 맞힌 대마다(연타면 타마다) 도는 규칙.
    /// </summary>
    public class ChloeRideTests
    {
        const string HEROES = @"[
 {id:'c', name:'클', role:'탱커', row:'front', hp:1000, atk:100, def:50, crit:0, keyword:{name:'땀', cap:9},
  passives:[{name:'꿰기', when:{on:'hit', each:true}, fx:[{k:'stack', id:'땀', v:1}]}],
  forms:[{id:'ride', name:'탑승', turns:3, bonus:[{type:'공격', unique:true, hits:3}]}]},
 {id:'o', name:'옹', role:'딜러', row:'front', hp:1000, atk:100, def:50, crit:0, keyword:{name:'옹땀', cap:9},
  passives:[{name:'한 번', when:{on:'hit'}, fx:[{k:'stack', id:'옹땀', v:1}]}]}
]";
        const string CARDS = @"[
 {id:'u_hit', name:'고유 치기', hero:'c', unique:true, cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'b_hit', name:'기본 치기', hero:'c', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'ride_on', name:'타기', hero:'c', unique:true, cost:0, type:'스킬', fx:[{k:'form', id:'ride'}]},
 {id:'u_pour', name:'쏟기', hero:'c', unique:true, cost:0, type:'공격', fx:[{k:'stack', id:'땀', v:2}, {k:'spend', id:'땀', all:true}, {k:'perEvent', n:1}, {k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'o_hit3', name:'세 번 치기', hero:'o', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.5, target:'oneEnemy', hits:3}]}
]";
        static GameData D() => K.Data(heroes: HEROES, cards: CARDS);
        static Battle F(params string[] party) => K.Fight(D(), party, new[] { "dummy" });

        static int Lost(bool ride, string card)
        {
            var b = F("c");
            if (ride) { K.Hand(b, "ride_on"); K.Play(b, "ride_on"); }
            int hp0 = K.Hp(b);
            K.Hand(b, card); K.Play(b, card);
            return hp0 - K.Hp(b);
        }

        [Test] public void 탑승_중_고유_공격은_3연타()
        {
            int one = Lost(false, "u_hit"), three = Lost(true, "u_hit");
            Assert.Greater(one, 0);
            Assert.AreEqual(one * 3, three, 3, "변신 중 고유 공격 카드 = 같은 배율 3대");
        }

        [Test] public void 탑승_중에도_기본_공격은_그대로()
        {
            Assert.AreEqual(Lost(false, "b_hit"), Lost(true, "b_hit"), 1, "unique 거르개 — 기본 카드는 덤이 없다");
        }

        [Test] public void 탑승_중에도_1개당_피해는_그대로()
        {
            Assert.AreEqual(Lost(false, "u_pour"), Lost(true, "u_pour"), 2, "per 뒤의 피해는 이미 겹만큼 여러 대 — n연타를 곱하지 않는다");
        }

        [Test] public void hit_each_는_타마다_겹이_쌓인다()
        {
            var b = F("c");
            K.Hand(b, "ride_on", "u_hit"); K.Play(b, "ride_on"); K.Play(b, "u_hit");
            Assert.AreEqual(3, b.StackOf("c", "땀"), "3연타 = 3겹");
            K.Hand(b, "b_hit"); K.Play(b, "b_hit");
            Assert.AreEqual(4, b.StackOf("c", "땀"), "한 대 = 1겹");
        }

        [Test] public void hit_each_가_없으면_카드_한_장에_한_번()
        {
            var b = F("o");
            K.Hand(b, "o_hit3"); K.Play(b, "o_hit3");
            Assert.AreEqual(1, b.StackOf("o", "옹땀"));
        }

        [Test] public void 글_n연타_타마다()
        {
            var d = D(); var tx = new CardText(d);
            StringAssert.Contains("3연타", tx.Form(d.Form("ride")));
            StringAssert.Contains("칠 때마다(타마다)", tx.Passives(d.Hero("c").Passives));
            var b = F("c"); K.Hand(b, "ride_on", "u_hit"); K.Play(b, "ride_on");
            StringAssert.Contains("3회", tx.Card(b.CardOf("u_hit")), "전투의 카드 글은 3연타 모습");
        }

        [Test] public void 검사기_hits_each()
        {
            Assert.IsFalse(Validator.Check(D()).Errors.Any(e => e.Contains("사도 c") || e.Contains("hits") || e.Contains("each")), string.Join("\n", Validator.Check(D()).Errors));
            var bad = K.Data(heroes: @"[{id:'g', name:'지', role:'딜러', row:'front', hp:1000, atk:100, def:50, keyword:{name:'지기', cap:3},
  passives:[{name:'p', when:{on:'play', each:true}, fx:[{k:'stack', id:'지기', v:1}]}],
  ult:{name:'u', cost:100, fx:[{k:'form', id:'gf'}]},
  forms:[{id:'gf', name:'지랄', turns:2, bonus:[{type:'공격', hits:7}, {type:'스킬', hits:2}]}]}]");
            var e = Validator.Check(bad).Errors;
            Assert.IsTrue(e.Any(x => x.Contains("n연타(2~5)")), "hits 7");
            Assert.IsTrue(e.Any(x => x.Contains("공격 카드 덤")), "스킬에 hits");
            Assert.IsTrue(e.Any(x => x.Contains("each(맞힌 대마다)")), "play 에 each");
        }
    }
}
