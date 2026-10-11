using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 3성 깊이 시범(2026-10-09, content-v2 _measure/깊이_기준) — 엔진 요청 셋.
    /// 대상 farEnemy(가장 먼 적 — 뒤 줄 우선, 같은 줄이면 가장 뒤 자리) · ifStack not + n ≥ 2 글 「n개 미만이면」 · 규칙 속 ifHand 글(카드를 낼 때 규칙만 「그 뒤」).
    /// </summary>
    public class DeepTrialTests
    {
        const string ENEMIES = @"[
 {id:'near', name:'앞 줄', hp:1000, row:'front', intents:[{t:'jam', v:0, rush:0}]},
 {id:'far', name:'뒤 줄', hp:1000, row:'back', intents:[{t:'jam', v:0, rush:0}]}]";
        const string CARDS = @"[
 {id:'snipe', name:'저격', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'farEnemy'}]},
 {id:'under', name:'넷 미만', hero:'a', cost:0, type:'스킬', fx:[{k:'ifStack', id:'주머니', n:4, not:true}, {k:'stack', id:'주머니', v:1}]},
 {id:'none', name:'없으면', hero:'a', cost:0, type:'스킬', fx:[{k:'ifStack', id:'주머니', not:true}, {k:'stack', id:'주머니', v:1}]}]";
        const string HEROES = @"[
 {id:'p', name:'주인', role:'딜러', hp:600, atk:100, def:20, keyword:{name:'주머니', cap:9},
  passives:[{name:'턴 끝 빈손', when:{on:'turnEnd'}, fx:[{k:'ifHand', n:1}, {k:'stack', id:'주머니', v:1}]},
            {name:'낼 때 빈손', when:{on:'play', type:'공격'}, fx:[{k:'ifHand', n:1}, {k:'stack', id:'주머니', v:1}]}]}]";

        static GameData Data() => K.Data(heroes: HEROES, cards: CARDS, enemies: ENEMIES);

        [Test] public void 가장_먼_적은_뒤_줄_적이다()
        {
            var d = Data();
            var b = K.Fight(d, new[] { "a" }, new[] { "far", "near" });   // 뒤 줄이 앞 자리여도
            K.Hand(b, "snipe"); K.Play(b, "snipe", 1);
            Assert.Less(K.Hp(b, 0), 1000, "뒤 줄 적이 맞는다");
            Assert.AreEqual(1000, K.Hp(b, 1), "앞 줄 적은 안 맞는다");
            var c = K.Fight(d, new[] { "a" }, new[] { "near", "near" });  // 같은 줄이면 가장 뒤 자리
            K.Hand(c, "snipe"); K.Play(c, "snipe", 0);
            Assert.AreEqual(1000, K.Hp(c, 0)); Assert.Less(K.Hp(c, 1), 1000);
            StringAssert.Contains("가장 먼 적에게", new CardText(d).Card(d.Card("snipe")));
            Assert.IsEmpty(Validator.Check(d).Errors.FindAll(e => e.Contains("snipe")), "검사기가 대상을 안다");
        }

        [Test] public void 겹_조건_not_은_n개_미만으로_읽힌다()
        {
            var d = Data(); var t = new CardText(d);
            StringAssert.Contains("4 미만이면", t.Card(d.Card("under")));   // 2026-10-11 글 길이 — 「n개」 → 「n」
            StringAssert.Contains("없으면", t.Card(d.Card("none")));
        }

        [Test] public void 규칙_속_손패_조건은_카드를_낼_때만_그_뒤()
        {
            var d = Data(); var t = new CardText(d); var h = d.Hero("p");
            string end = t.Rule(h.Passives[0]), play = t.Rule(h.Passives[1]);
            StringAssert.DoesNotContain("그 뒤", end, end);
            StringAssert.Contains("손패가 1장 이하면", end);
            StringAssert.Contains("그 뒤 손패가 1장 이하면", play);
        }
    }
}
