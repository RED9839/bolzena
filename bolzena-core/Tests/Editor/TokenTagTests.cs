using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 생성물 카드 id 를 갈래 태그로 세기(perTag · 거르개 tag) · exileFrom all(맞는 카드 전부) — 2026-10-07 사도 리워크(에르핀 「케이크」).
    /// </summary>
    public class TokenTagTests
    {
        const string HERO = "{id:'s', name:'먹보', role:'딜러', row:'front', hp:3000, atk:100, def:50, crit:0, keyword:{name:'배', carrier:'self', cap:5}}";
        const string CARDS = "[{id:'s_cake', name:'케이크', hero:'s', token:true, cost:0, type:'스킬', tags:['소멸'], fx:[{k:'stack', id:'배', v:1}]}," +
            "{id:'s_shot', name:'폭주', hero:'s', cost:1, type:'공격', fx:[{k:'perTag', id:'s_cake'}, {k:'dmg', ratio:1, target:'oneEnemy'}, {k:'exileFrom', from:'hand', all:true, tag:'s_cake'}]}," +
            "{id:'s_other', name:'딴것', hero:'s', cost:1, type:'스킬', fx:[{k:'stack', id:'배', v:1}]}]";

        [Test] public void 손의_생성물_수만큼_치고_전부_소멸()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var b = K.Fight(d, new[] { "s" }, new[] { "dummy" }); K.Hand(b, "s_shot", "s_cake", "s_cake", "s_cake", "s_other");
            int hp0 = K.Hp(b);
            K.Play(b, "s_shot");
            Assert.AreEqual(hp0 - 300, K.Hp(b), "케이크 셋 × 100%");
            Assert.AreEqual(0, b.Hand.Count(id => GameData.BaseId(id) == "s_cake"), "케이크 전부 소멸");
            Assert.IsTrue(b.Hand.Contains("s_other"), "다른 카드는 남는다");
        }

        [Test] public void 글은_생성물_이름으로()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var t = new CardText(d).Fx(d.Card("s_shot").Fx);
            StringAssert.Contains("손의 「케이크」 카드 1장당", t);
            StringAssert.Contains("손패의 「케이크」 카드 모두 소멸", t);
        }
    }
}
