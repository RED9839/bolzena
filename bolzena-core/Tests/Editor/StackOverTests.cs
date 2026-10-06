using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 「X」가 최대라 넘치면(stackOver) — 최대에 막혀 버려지던 몫을 다른 이득으로(2026-10-05 스택형 점검) · 고유 효과 계측(KwMeter).
    /// </summary>
    public class StackOverTests
    {
        static GameData Hero(string heroJson, string cards) => K.Data(heroes: "[" + heroJson + "]", cards: cards);

        const string ROCK = "{id:'k', name:'바위꾼', role:'딜러', row:'front', hp:500, atk:100, def:20, crit:0, keyword:{name:'주먹', carrier:'self', cap:3, " +
            "rules:[{name:'바위 던지기', when:{on:'stackOver', id:'주먹'}, fx:[{k:'perEvent'}, {k:'dmg', ratio:0.5, target:'allEnemies'}]}]}}";
        const string CARDS = "[{id:'load', name:'모으기', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'주먹', v:5}]}," +
            " {id:'two', name:'둘', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'주먹', v:2}]}," +
            " {id:'nuke', name:'끝', hero:'k', cost:0, type:'공격', fx:[{k:'dmg', ratio:50, target:'allEnemies'}]}]";

        [Test] public void 최대에서_넘친_몫만큼_규칙이_돈다()
        {
            var b = K.Fight(Hero(ROCK, CARDS), new[] { "k" }, new[] { "dummy", "dummy" }); K.Hand(b, "load");
            K.Play(b, "load");
            Assert.AreEqual(3, b.StackOf("k", "주먹"), "최대 3");
            Assert.AreEqual(1000 - 100, K.Hp(b, 0), "넘친 2 × 공격력 50%");
            Assert.AreEqual(1000 - 100, K.Hp(b, 1), "적 전체");
        }

        [Test] public void 최대에_닿기만_하면_돌지_않는다()
        {
            var b = K.Fight(Hero(ROCK, CARDS), new[] { "k" }, new[] { "dummy" }); K.Hand(b, "two", "two");
            K.Play(b, "two");
            Assert.AreEqual(1000, K.Hp(b));
            K.Play(b, "two");
            Assert.AreEqual(3, b.StackOf("k", "주먹"));
            Assert.AreEqual(1000 - 50, K.Hp(b), "2 + 2 → 3, 넘친 1");
        }

        [Test] public void 아군_표시_hunt_는_한_번에_한_명()
        {
            var d = K.Data(heroes: "[{id:'k', name:'구경꾼', role:'서포터', row:'back', hp:500, atk:50, def:20, crit:0, keyword:{name:'챔프', carrier:'hero', cap:1, hunt:true}," +
                " passives:[{name:'관중석', when:{on:'fightStart'}, fx:[{k:'stack', id:'챔프', v:1, target:'strongestAlly'}]}]}," +
                " {id:'p', name:'힘센', role:'딜러', row:'front', hp:500, atk:200, def:20, crit:0}, {id:'q', name:'약한', role:'딜러', row:'front', hp:500, atk:100, def:20, crit:0}]",
                cards: "[{id:'pick', name:'지목', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'챔프', v:1, target:'oneAlly'}]}]");
            var b = K.Fight(d, new[] { "k", "p", "q" }, new[] { "dummy" });
            Assert.AreEqual(1, b.StackOf("p", "챔프"), "전투 시작 — 공격력이 가장 높은 다른 아군");
            Assert.AreEqual(0, b.StackOf("q", "챔프"));
            K.Hand(b, "pick");
            K.Play(b, "pick", 0, new PlayOpts { Ally = 2 });
            Assert.AreEqual(1, b.StackOf("q", "챔프"));
            Assert.AreEqual(0, b.StackOf("p", "챔프"), "옮겨 간다");
        }

        [Test] public void 계측은_넘친_몫과_최대_도달_턴을_센다()
        {
            KwMeter.Reset(); KwMeter.On = true;
            try
            {
                var b = K.Fight(Hero(ROCK, CARDS), new[] { "k" }, new[] { "dummy" }); K.Hand(b, "load", "nuke");
                var copy = b.Clone();
                K.Play(copy, "load");   // 봇이 읽으려 복사한 판은 재지 않는다
                K.Play(b, "load");
                K.Play(b, "nuke");
                Assert.AreEqual("win", b.Over);
            }
            finally { KwMeter.On = false; }
            var r = KwMeter.Rows().Single(x => x.Id == "주먹");
            Assert.AreEqual(1, r.Fights);
            Assert.AreEqual(5, r.Asked);
            Assert.AreEqual(3, r.Got);
            Assert.AreEqual(0.4, r.Waste, 1e-9);
            Assert.AreEqual(1, r.CapFights);
            Assert.AreEqual(1.0, r.CapTurn, 1e-9);
            KwMeter.Reset();
        }
    }
}
