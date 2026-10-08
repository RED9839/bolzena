using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 시동 카드(고유 카드이면서 시작 덱에 든 것) — 은총 후보에서 빠진다(2026-10-08 카제나식 시작 덱 = 기본 3 + 시동 1).
    /// </summary>
    public class OpenerTests
    {
        const string HERO = "{id:'s', name:'시동', role:'딜러', row:'front', hp:3000, atk:100, def:50, crit:0, keyword:{name:'시동 장치', carrier:'self', cap:3}, starter:['s_b','s_b','s_b','s_u1']}";
        const string CARDS = "[{id:'s_b', name:'주먹', hero:'s', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]}," +
            "{id:'s_u1', name:'열기', hero:'s', unique:true, cost:0, type:'스킬', fx:[{k:'stack', id:'시동 장치', v:1}]}," +
            "{id:'s_u2', name:'둘', hero:'s', unique:true, cost:1, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]}," +
            "{id:'s_u5', name:'다섯', hero:'s', unique:true, cost:1, type:'스킬', fx:[{k:'draw', v:1}]}]";

        [Test] public void 시동_카드는_은총_후보에서_빠진다()
        {
            var d = K.Data(heroes: "[" + HERO + "]", cards: CARDS);
            Assert.IsTrue(d.IsOpener("s_u1"));
            Assert.IsFalse(d.IsOpener("s_u2"));
            Assert.IsFalse(d.IsOpener("s_b"), "기본 카드는 시동이 아니다");
            CollectionAssert.AreEqual(new[] { "s_u2", "s_u5" }, d.GraceUniquesOf("s"));
        }
    }
}
