using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>축복 · 신탁으로 피해가 생긴 스킬은 공격 카드 — 종류 · 「공격 카드를 내면」 계기 · 고르기 창 미리보기. 강화 신탁 · 피해 없는 축복은 그대로.</summary>
    public class BlessTypeTests
    {
        const string CARDS = @"[
 {id:'sk_hit', name:'때리는 축복', hero:'a', cost:1, type:'스킬', fx:[{k:'block', ratio:1.0}],
  blesses:[{name:'한 대', fx:[{k:'dmg', ratio:0.5, target:'oneEnemy'}]}, {name:'게이지', fx:[{k:'gauge', v:30}]}],
  oracles:[{name:'찌르기', fx:[{k:'block', ratio:1.0}, {k:'dmg', ratio:0.5, target:'oneEnemy'}]},
           {name:'다짐', power:true, fx:[{k:'dmg', ratio:0.5, target:'oneEnemy'}]},
           {name:'더 막기', fx:[{k:'block', ratio:2.0}]}]},
 {id:'pw_p', name:'연타 습관', hero:'a', unique:true, cost:0, type:'강화',
  fx:[{k:'power', rules:[{when:{on:'play', type:'공격'}, fx:[{k:'gauge', v:5}]}]}]}
]";
        static GameData D() => K.Data(cards: CARDS);

        static Battle F(string shin)
        {
            var b = K.Fight(D(), new[] { "a" }, new[] { "dummy" }, st => { if (shin != null) st.Shin = new Dictionary<string, string> { ["sk_hit"] = shin }; });
            K.Hand(b, "pw_p", "sk_hit");
            K.Play(b, "pw_p");
            return b;
        }

        [Test] public void 스킬에_피해_축복_공격_카드_공격_계기()
        {
            var plain = F(null);
            int g0 = plain.Gauge; K.Play(plain, "sk_hit");
            Assert.AreEqual("스킬", plain.PlayLog.Last().type);
            var b = F("own");
            Assert.AreEqual("공격", b.CardOf("sk_hit").Type, "피해를 더하는 축복 — 공격 카드");
            int g1 = b.Gauge; K.Play(b, "sk_hit");
            Assert.AreEqual("공격", b.PlayLog.Last().type);
            Assert.AreEqual((plain.Gauge - g0) + 5, b.Gauge - g1, "「공격 카드를 내면」 이 돈다(게이지 +5)");
        }

        [Test] public void 스킬에_피해_없는_축복_스킬_그대로()
        {
            Assert.AreEqual("스킬", F("own1").CardOf("sk_hit").Type, "덤이 게이지뿐");
            Assert.AreEqual("스킬", F("power").CardOf("sk_hit").Type, "배율형 공용 축복은 피해를 직접 더하지 않는다");
            Assert.AreEqual("스킬", F(null).CardOf("sk_hit").Type);
        }

        [Test] public void 신탁_피해_공격_강화_신탁은_강화()
        {
            var d = D();
            Assert.AreEqual("공격", d.View("sk_hit", 1).Type, "기본형에 없던 피해가 신탁으로");
            Assert.AreEqual("강화", d.View("sk_hit", 2).Type, "강화 신탁이 먼저");
            Assert.AreEqual("스킬", d.View("sk_hit", 3).Type);
            Assert.AreEqual("강화", d.View("pw_p").Type);
        }

        [Test] public void 고르기_창_미리보기_축복으로_공격()
        {
            var d = D();
            var o = OracleOption.Of(d, "sk_hit", new[] { new GlowPick { N = 3, Shin = "own" }, new GlowPick { N = 3, Shin = "own1" }, new GlowPick { N = 1, Shin = "own" } });
            Assert.IsTrue(o[0].ToAttack); Assert.AreEqual("공격", o[0].View.Type);
            Assert.IsFalse(o[1].ToAttack); Assert.AreEqual("스킬", o[1].View.Type);
            Assert.IsFalse(o[2].ToAttack, "신탁이 이미 공격으로 만들었으면 축복이 바꾸는 것이 아니다"); Assert.AreEqual("공격", o[2].View.Type);
        }
    }
}
