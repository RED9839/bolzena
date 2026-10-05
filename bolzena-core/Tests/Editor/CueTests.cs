using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>연출 쪽지(Docs/API.md §1) — 화면이 기대는 차례와 칸.</summary>
    public class CueTests
    {
        [Test] public void 카드를_내면_손에서_나가고_움직이고_맞고_버린_더미로()
        {
            var cues = new List<Cue>();
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "dummy" }, st => st.Deck = Enumerable.Repeat("hit", 8).ToList(), cues);
            Assert.AreEqual("turn", cues.First(c => c.K == "turn").K);
            Assert.AreEqual(5, cues.Count(c => c.K == "card" && c.Pile == "draw" && c.ToPile == "hand"));
            cues.Clear();
            b.PlayCard(0, 0);
            var ks = cues.Select(c => c.K).ToList();
            CollectionAssert.AreEqual(new[] { "card", "act", "hurt", "tough", "card" }, ks);
            Assert.AreEqual("play", cues[0].ToPile);
            Assert.AreEqual("discard", cues[4].ToPile);
            Assert.AreEqual(100, cues[2].V);
        }

        [Test] public void 격파_즉시_행동_적의_차례_끝()
        {
            var cues = new List<Cue>();
            var d = K.Data(enemies: "[{id:'q', name:'재촉', hp:100, intents:[{t:'attack', v:10, rush:3}]}]",
                cards: "[{id:'brk', name:'깨기', hero:'a', cost:0, type:'공격', fx:[{k:'tough', v:9, target:'oneEnemy'}]}, {id:'kill', name:'끝', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:5.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "q", "q" }, cues: cues); K.Hand(b, "zero", "zero", "zero", "brk", "kill");
            K.Play(b, "zero"); K.Play(b, "zero"); K.Play(b, "zero");
            Assert.IsTrue(cues.Any(c => c.K == "act" && c.Rush && c.Side == Side.Enemy));
            K.Play(b, "brk", 0);
            Assert.IsTrue(cues.Any(c => c.K == "break" && c.V == 1));
            b.EndTurn();
            Assert.IsTrue(cues.Any(c => c.K == "foeTurn"));
            K.Hand(b, "kill", "kill");
            K.Play(b, "kill", 0); K.Play(b, "kill", 1);
            Assert.AreEqual(2, cues.Count(c => c.K == "die"));
            Assert.AreEqual("win", cues.Last(c => c.K == "over").Id);
        }

        [Test] public void 쪽지를_구독으로도_받는다()
        {
            var got = new List<string>();
            var b = Battle.Start(K.Data(), new BattleSetup { Party = new List<string> { "a" }, Enemies = new List<string> { "hitter" }, Deck = new List<string>(), EnemyHp = 1, Seed = 1 }, null, c => got.Add(c.K));
            b.EndTurn();
            CollectionAssert.Contains(got, "hurt");
            CollectionAssert.Contains(got, "act");
        }
    }
}
