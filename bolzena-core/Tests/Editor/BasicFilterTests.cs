using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 시작(기본) 카드 거르개 — 패시브 play 계기 basic · transform from:"hand" + basic 글(2026-10-08 사도 리워크).
    /// </summary>
    public class BasicFilterTests
    {
        const string HERO = "{id:'s', name:'수련생', role:'딜러', row:'front', hp:3000, atk:100, def:50, crit:0, keyword:{name:'기본기', carrier:'self', cap:9}," +
            "passives:[{name:'기초 체력', when:{on:'play', basic:true}, fx:[{k:'stack', id:'기본기', v:1}]}]}";
        const string CARDS = "[{id:'s_b', name:'주먹', hero:'s', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]}," +
            "{id:'s_u', name:'비기', hero:'s', unique:true, cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]}," +
            "{id:'s_up', name:'단련', hero:'s', unique:true, cost:0, type:'스킬', fx:[{k:'transform', id:'s_u', from:'hand', basic:true, n:2}]}]";

        [Test] public void play_basic_은_시작_카드에만()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var b = K.Fight(d, new[] { "s" }, new[] { "dummy" }); K.Hand(b, "s_b", "s_u");
            K.Play(b, "s_u"); Assert.AreEqual(0, b.StackOf("s", "기본기"), "고유 카드는 안 셈");
            K.Play(b, "s_b"); Assert.AreEqual(1, b.StackOf("s", "기본기"), "시작 카드면 셈");
            StringAssert.Contains("시작 카드를 낼 때마다", new CardText(d).Passives(d.Hero("s").Passives));
        }

        [Test] public void transform_손_거르개_글()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            var t = new CardText(d).Fx(d.Card("s_up").Fx);
            StringAssert.Contains("손의 시작 카드 2장을 「비기」로 바꿈", t);
            StringAssert.DoesNotContain("「hand」", t);
        }
    }
}
