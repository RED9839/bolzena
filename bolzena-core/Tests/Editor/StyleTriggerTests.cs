using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>18갈래 공용 계기(2026-10-08) — tally · keepAp · reserveFire · grow · fightEnd · exhaust 거르기 · goneMin · 검사기 style.</summary>
    public class StyleTriggerTests
    {
        const string TWO = @"[
 {id:'p', name:'주인', role:'딜러', hp:600, atk:100, def:20, keyword:{name:'주머니', cap:9}, passives:[PASSIVE]},
 {id:'q', name:'짝', role:'탱커', hp:900, atk:200, def:60, keyword:{name:'짝주머니', cap:9}}]";
        const string CARDS = @"[
 {id:'pz', name:'주인 숨', hero:'p', cost:0, type:'스킬', fx:[]},
 {id:'qz', name:'짝 숨', hero:'q', cost:0, type:'공격', fx:[]},
 {id:'calc1', name:'셈 하나', hero:'p', cost:1, type:'스킬', fx:[{k:'ifSpent', n:1}, {k:'stack', id:'주머니', v:1}]},
 {id:'calc2', name:'셈 둘', hero:'p', cost:1, type:'스킬', fx:[{k:'ifSpent', n:2}, {k:'stack', id:'주머니', v:1}]},
 {id:'lt', name:'나중에', hero:'p', cost:0, type:'스킬', fx:[{k:'later', n:1, then:[{k:'gauge', v:1}]}]},
 {id:'grow', name:'갈고닦기', hero:'p', cost:0, type:'스킬', fx:[{k:'cardStatus', id:'숙련', v:1}]},
 {id:'cheap', name:'싸게', hero:'p', cost:0, type:'스킬', fx:[{k:'cardStatus', id:'비용', v:-1}]},
 {id:'big', name:'큰 한 방', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:99, target:'oneEnemy'}]},
 {id:'qgone', name:'짝 태우기', hero:'q', cost:0, type:'스킬', tags:['소멸'], fx:[]},
 {id:'pgone', name:'주인 태우기', hero:'p', cost:0, type:'스킬', tags:['소멸'], fx:[]}]";
        static Battle Two(string passive)
        {
            var d = K.Data(heroes: TWO.Replace("PASSIVE", passive), cards: CARDS);
            return K.Fight(d, new[] { "p", "q" }, new[] { "dummy" });
        }

        [Test] public void 셈이_맞으면_카드가_끝난_뒤()
        {
            // 카드를 내면 비용만큼 게이지가 차므로, 계기 결과는 키워드 수로 본다(카드 +1 · 계기 +5)
            var b = Two("{name:'맞았다', when:{on:'tally'}, fx:[{k:'stack', id:'주머니', v:5}]}"); K.Hand(b, "calc1", "calc2");
            K.Play(b, "calc1");
            Assert.AreEqual(6, b.StackOf("p", "주머니"), "이번 턴 쓴 AP 가 꼭 1");
            K.Play(b, "calc2");
            Assert.AreEqual(9, b.StackOf("p", "주머니"), "쓴 AP 가 2 — 셈 둘도 맞는다(cap 9)");
        }

        [Test] public void 셈이_안_맞으면_안_돈다()
        {
            var b = Two("{name:'맞았다', when:{on:'tally'}, fx:[{k:'stack', id:'주머니', v:5}]}"); K.Hand(b, "calc2");
            K.Play(b, "calc2");
            Assert.AreEqual(0, b.StackOf("p", "주머니"));
        }

        [Test] public void AP를_남기고_턴을_마치면()
        {
            var b = Two("{name:'아껴 둠', when:{on:'keepAp', n:1}, fx:[{k:'stack', id:'주머니', v:2}]}");
            Assert.Greater(b.Ap, 0);
            b.EndTurn();
            Assert.AreEqual(2, b.StackOf("p", "주머니"));
        }

        [Test] public void AP가_모자라면_안_돈다()
        {
            var b = Two("{name:'아껴 둠', when:{on:'keepAp', n:9}, fx:[{k:'stack', id:'주머니', v:2}]}");
            b.EndTurn();
            Assert.AreEqual(0, b.StackOf("p", "주머니"));
        }

        [Test] public void 예약이_터지면()
        {
            var b = Two("{name:'터졌다', when:{on:'reserveFire'}, fx:[{k:'stack', id:'주머니', v:3}]}"); K.Hand(b, "lt");
            K.Play(b, "lt");
            Assert.AreEqual(0, b.StackOf("p", "주머니"), "아직 안 터졌다");
            b.EndTurn();
            Assert.AreEqual(3, b.StackOf("p", "주머니"), "다음 턴 예약이 터진 뒤");
        }

        [Test] public void 카드가_자라면_비용은_자람이_아니다()
        {
            var b = Two("{name:'자랐다', when:{on:'grow'}, fx:[{k:'gauge', v:2}]}"); K.Hand(b, "grow", "cheap");
            K.Play(b, "grow");
            Assert.AreEqual(2, b.Gauge);
            K.Play(b, "cheap");
            Assert.AreEqual(2, b.Gauge, "비용 깎기는 grow 가 아니다");
        }

        [Test] public void 카드값_이름으로_거르기()
        {
            var b = Two("{name:'다른 값', when:{on:'grow', cardSt:'다른것'}, fx:[{k:'gauge', v:2}]}"); K.Hand(b, "grow");
            K.Play(b, "grow");
            Assert.AreEqual(0, b.Gauge);
        }

        [Test] public void 전투에서_이기면_한_번()
        {
            var b = Two("{name:'승리', when:{on:'fightEnd'}, fx:[{k:'gauge', v:7}]}"); K.Hand(b, "big");
            K.Play(b, "big");
            Assert.AreEqual("win", b.Over);
            Assert.AreEqual(7, b.Gauge);
        }

        [Test] public void 다른_아군의_카드가_소멸하면()
        {
            var b = Two("{name:'불쏘시개', when:{on:'exhaust', who:'other'}, fx:[{k:'gauge', v:2}]}"); K.Hand(b, "pgone", "qgone");
            K.Play(b, "pgone");
            Assert.AreEqual(0, b.Gauge, "제 카드는 아니다");
            K.Play(b, "qgone");
            Assert.AreEqual(2, b.Gauge);
        }

        [Test] public void 이번_전투_소멸_장수()
        {
            var b = Two("{name:'잿더미', when:{on:'play'}, conds:[{c:'goneMin', n:1}], fx:[{k:'stack', id:'주머니', v:1}]}"); K.Hand(b, "pz", "qgone");
            K.Play(b, "pz");
            Assert.AreEqual(0, b.StackOf("p", "주머니"));
            K.Play(b, "qgone");
            Assert.AreEqual(1, b.GoneN);
            K.Hand(b, "pz");
            K.Play(b, "pz");
            Assert.AreEqual(1, b.StackOf("p", "주머니"));
        }

        [Test] public void 저장해도_소멸_장수가_남는다()
        {
            var b = Two("{name:'없음', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}"); K.Hand(b, "qgone");
            K.Play(b, "qgone");
            var b2 = Battle.Load(b.Data, b.Save());
            Assert.AreEqual(1, b2.GoneN);
        }

        [Test] public void 검사기_갈래_이름()
        {
            var d = K.Data(heroes: "[{id:'s', name:'갈래', role:'딜러', hp:600, atk:100, def:20, style:'모르는갈래', keyword:{name:'갈래주머니', cap:3}, passives:[{name:'채움', when:{on:'turnStart'}, fx:[{k:'stack', id:'갈래주머니', v:1}]}]}]");
            Assert.IsTrue(Validator.Check(d).Errors.Any(x => x.Contains("모르는 갈래")));
        }

        [Test] public void 검사기_저절로_터짐은_쌓고_고르기도_주의()
        {
            // 1단계(2026-10-08 사용자) — 쌓고 고르기도 센다. 다 차면 「카드 만들기 · 다음 카드 강화」 로 바꾼 것만 터짐이 아니다(StageOneTests)
            const string H = "[{id:'s', name:'갈래', role:'딜러', hp:600, atk:100, def:20, style:'STYLE', keyword:{name:'갈래주머니', cap:3}, passives:[{name:'채움', when:{on:'turnStart'}, fx:[{k:'stack', id:'갈래주머니', v:1}]}, {name:'터짐', when:{on:'stackReach', id:'갈래주머니', n:3}, fx:[{k:'spend', id:'갈래주머니', v:3}, {k:'gauge', v:1}]}]}]";
            Assert.IsTrue(Validator.Check(K.Data(heroes: H.Replace("STYLE", "계산형"))).Warnings.Any(x => x.Contains("저절로 터진다")));
            Assert.IsTrue(Validator.Check(K.Data(heroes: H.Replace("STYLE", "쌓고 고르기"))).Warnings.Any(x => x.Contains("저절로 터진다")));
        }

        [Test] public void 카드_글()
        {
            var t = new CardText(K.Data());
            Assert.AreEqual("셈이 맞으면", t.WhenText(new When { On = "tally" }));
            Assert.AreEqual("아군의 예약이 터지면", t.WhenText(new When { On = "reserveFire", Who = "any" }));
            Assert.AreEqual("AP를 2 이상 남기거나 보존 카드를 쥐고 턴을 마치면", t.WhenText(new When { On = "keepAp", N = 2 }));
            Assert.AreEqual("AP를 2 이상 남기고 턴을 마치면", t.WhenText(new When { On = "keepAp", N = 2, Kind = "ap" }));
            Assert.AreEqual("전투에서 이기면", t.WhenText(new When { On = "fightEnd" }));
            Assert.AreEqual("이번 전투에 소멸한 카드가 3장 이상이면", t.CondText(new Cond { C = "goneMin", N = 3 }));
        }
    }
}
