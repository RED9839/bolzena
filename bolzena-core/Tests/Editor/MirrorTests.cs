using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 「맞은 만큼 다음 턴에 쏜다」(2026-10-06 미로 「수많은 시선」) — perStack 의 최소 · 최대(n · max), 연출 쪽지 cue.
    /// </summary>
    public class MirrorTests
    {
        const string HERO = "{id:'m', name:'거울', role:'탱커', row:'front', hp:3000, atk:100, def:50, crit:0, keywords:[" +
            "{name:'숨음', carrier:'self', cap:1, rules:[" +
            "  {name:'밖으로', when:{on:'turnStart'}, conds:[{c:'stack', id:'숨음', n:1}], fx:[{k:'cue', id:'beam', xStack:'맺힘', n:5, max:10}, {k:'perStack', id:'맺힘', n:5, max:10}, {k:'dmg', ratio:1.0, target:'randomEnemy'}]}," +
            "  {name:'밖으로', when:{on:'turnStart'}, conds:[{c:'stack', id:'숨음', n:1}], fx:[{k:'spend', id:'맺힘', all:true}, {k:'spend', id:'숨음', all:true}]}]}," +
            "{name:'맺힘', carrier:'self', cap:10}]," +
            " passives:[{name:'맺히는 공격', when:{on:'hurt', guarded:true}, conds:[{c:'stack', id:'숨음', n:1}], fx:[{k:'cue', id:'flash'}, {k:'stack', id:'맺힘', v:1}]}]}";
        const string CARDS = "[{id:'hide', name:'숨기', hero:'m', cost:0, type:'스킬', fx:[{k:'cue', id:'hide'}, {k:'stack', id:'숨음', v:1}]}," +
            " {id:'lots', name:'잔뜩', hero:'m', cost:0, type:'스킬', fx:[{k:'stack', id:'맺힘', v:12}]}]";

        static GameData D() => K.Data(heroes: "[" + HERO + "]", cards: CARDS);
        static int Lost(Battle b) => b.Enemies.Sum(e => e.MaxHp - e.Hp);

        [Test] public void 맞은_횟수가_5_미만이면_다음_턴에_5발()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "m" }, new[] { "hitter", "hitter", "hitter" }, null, cues); K.Hand(b, "hide");
            K.Play(b, "hide");
            Assert.AreEqual(1, b.StackOf("m", "숨음"));
            b.EndTurn();   // 적 셋이 한 대씩 — 맺힘 3, 다음 턴 시작에 최소 5발
            Assert.AreEqual(500, Lost(b), "5발 × 공격력 100%");
            Assert.AreEqual(0, b.StackOf("m", "맺힘"));
            Assert.AreEqual(0, b.StackOf("m", "숨음"), "턴 시작에 거울에서 나온다");
            Assert.AreEqual(3, cues.Count(c => c.K == "fx" && c.Id == "flash"), "맞을 때마다 반짝");
            var beam = cues.Single(c => c.K == "fx" && c.Id == "beam");
            Assert.AreEqual(5, beam.V, "발 수");
            Assert.AreEqual(1, cues.Count(c => c.K == "fx" && c.Id == "hide"));
        }

        [Test] public void 맞은_횟수만큼_쏘되_10발까지()
        {
            var b = K.Fight(D(), new[] { "m" }, new[] { "dummy" }); K.Hand(b, "hide", "lots");
            K.Play(b, "hide");
            K.Play(b, "lots");
            Assert.AreEqual(10, b.StackOf("m", "맺힘"), "최대 10");
            b.EndTurn();
            Assert.AreEqual(1000, Lost(b), "10발 × 공격력 100%");
        }

        [Test] public void 거울에_숨지_않았으면_세지_않는다()
        {
            var b = K.Fight(D(), new[] { "m" }, new[] { "hitter" });
            b.EndTurn();
            Assert.AreEqual(0, b.StackOf("m", "맺힘"));
            Assert.AreEqual(0, Lost(b));
        }

        [Test] public void 글에_최소_최대가_나오고_cue_는_안_나온다()
        {
            var d = D();
            var tx = new CardText(d);
            string t = tx.Fx(d.Card("hide").Fx);
            Assert.IsFalse(t.Contains("hide"));
            StringAssert.Contains("1개당(최소 5 · 최대 10)", tx.Fx(d.Hero("m").AllKeywords.First(k => k.Name == "숨음").Rules[0].Fx));
        }
    }
}
