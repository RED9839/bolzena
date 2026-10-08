using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>학점제 학년(RunGrade.cs) — 학점 · 진급 · 보상 · 저장.</summary>
    public class GradeTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        static Run New(long seed = 1) => Run.New(K.Sample(), PARTY, seed);

        /// <summary>지금 칸에서 싸움을 열어 이긴 것으로 닫는다(elite · boss 는 판 상태를 맞춘다).</summary>
        static Battle Win(Run run, string kind = "fight")
        {
            run.S.Elite = kind == "elite";
            run.S.Node = kind == "boss" ? 3 : 0;
            var (b, _) = run.OpenFight();
            b.Over = "win";
            run.AfterFight(b);
            return b;
        }

        [Test] public void 새_판은_1학년_학점_0()
        {
            var run = New();
            Assert.AreEqual(1, run.Grade);
            Assert.AreEqual(0, run.S.Credits);
            Assert.AreEqual((0, Grades.NEED[2]), run.GradeSpan);
        }

        [Test] public void 학점표와_진급표()
        {
            Assert.AreEqual(1, Grades.CreditOf("fight"));
            Assert.AreEqual(3, Grades.CreditOf("elite"));
            Assert.AreEqual(5, Grades.CreditOf("boss"));
            Assert.AreEqual(0, Grades.CreditOf("nope"));
            Assert.AreEqual(1, Grades.Of(Grades.NEED[2] - 1));
            for (int g = 2; g <= Grades.MAX; g++) { Assert.AreEqual(g, Grades.Of(Grades.NEED[g])); Assert.Greater(Grades.NEED[g], Grades.NEED[g - 1]); }
            Assert.AreEqual(Grades.MAX, Grades.Of(999));
            Assert.AreEqual("졸업", Grades.Name(6));
        }

        [Test] public void 이기면_종류만큼_학점_지면_없다()
        {
            var run = New();
            Win(run, "fight"); Assert.AreEqual(1, run.S.Credits); Assert.AreEqual(1, run.S.LastCredit);
            Win(run, "elite"); Assert.AreEqual(4, run.S.Credits);
            var (b, _) = run.OpenFight(); b.Over = "lose"; run.AfterFight(b);
            Assert.AreEqual(4, run.S.Credits); Assert.AreEqual(0, run.S.LastCredit);
        }

        [Test] public void 진급하면_최대_HP_가_오르고_소식이_남는다()
        {
            var run = New();
            run.S.Credits = Grades.NEED[2] - 1;
            int max0 = run.S.PartyMaxHp;
            Win(run);
            Assert.AreEqual(2, run.Grade);
            int baseHp = PARTY.Sum(k => run.Data.Hero(k).Hp);
            Assert.AreEqual(max0 + Num.Round(baseHp * Grades.STAT_PCT), run.S.PartyMaxHp);
            CollectionAssert.AreEqual(new[] { 2 }, run.PopGradeNews());
            Assert.IsEmpty(run.PopGradeNews(), "꺼내면 비운다");
        }

        [Test] public void 한_번에_두_학년을_넘으면_둘_다_소식()
        {
            var run = New();
            run.S.Credits = Grades.NEED[4] - Grades.CreditOf("boss");
            run.S.Grade = Grades.Of(run.S.Credits);
            int g0 = run.Grade;
            Win(run, "boss");
            Assert.AreEqual(4, run.Grade);
            Assert.Greater(run.Grade - g0, 1, "보스 5학점이 두 학년을 넘는다");
            CollectionAssert.AreEqual(Enumerable.Range(g0 + 1, run.Grade - g0).ToList(), run.PopGradeNews());
        }

        [Test] public void 학년_능력치가_전투에_든다()
        {
            var run = New();
            var (b1, _) = run.OpenFight();
            var atk1 = b1.Party.Select(u => u.Atk).ToList();
            run.S.Grade = 4;
            var (b4, _) = run.OpenFight();
            for (int i = 0; i < PARTY.Count; i++)
            {
                var h = run.Data.Hero(PARTY[i]);
                Assert.AreEqual(atk1[i] + Num.Round(h.Atk * Grades.STAT_PCT * 3), b4.Party[i].Atk);
            }
            Assert.IsFalse(run.S.Growth.Values.Any(g => g.Atk > 0), "판 성장(RunState.Growth)은 건드리지 않는다");
        }

        [Test] public void 사학년부터_게이지_졸업은_더()
        {
            var run = New();
            run.S.Gauge = 10;
            run.S.Grade = 3; Assert.AreEqual(10, run.OpenFight().battle.Gauge);
            run.S.Grade = Grades.GAUGE_AT; Assert.AreEqual(10 + Grades.GAUGE_START, run.OpenFight().battle.Gauge);
            run.S.Grade = Grades.GRAD; Assert.AreEqual(10 + Grades.GRAD_GAUGE, run.OpenFight().battle.Gauge);
        }

        [Test] public void 삼학년부터_신탁_후보_넷()
        {
            var d = K.Sample().Add(null, @"[{id:'or5', name:'다섯 갈래', hero:'rico', cost:1, type:'스킬', fx:[{k:'draw', v:1}],
 oracles:[{name:'하나', fx:[]}, {name:'둘', fx:[]}, {name:'셋', fx:[]}, {name:'넷', fx:[]}, {name:'다섯', fx:[]}]}]", null, null, null, null);
            var run = Run.New(d, PARTY, 1);
            run.S.Deck.Add("or5");
            Assert.AreEqual(R.ORACLE_PICKS, run.OraclePicks);
            Assert.AreEqual(R.ORACLE_PICKS, run.RollOracles("or5").Count);
            run.S.Grade = Grades.ORACLE_AT;
            Assert.AreEqual(R.ORACLE_PICKS + Grades.ORACLE_PLUS, run.OraclePicks);
            Assert.AreEqual(R.ORACLE_PICKS + Grades.ORACLE_PLUS, run.RollOracles("or5").Count);
        }

        [Test] public void 오학년_진급은_다음_전투_신탁_확정_졸업은_회복()
        {
            var run = New();
            run.S.Grade = Grades.FLASH_AT - 1; run.S.Credits = Grades.NEED[Grades.FLASH_AT] - 1;
            Win(run);
            Assert.IsTrue(run.S.RewardFlash);
            run.S.PartyHp = 1;
            run.S.Credits = Grades.NEED[Grades.GRAD] - 1;
            Win(run);
            Assert.AreEqual(Grades.GRAD, run.Grade);
            Assert.Greater(run.S.PartyHp, Num.Round(run.S.PartyMaxHp * Grades.GRAD_HEAL) - 1);
        }

        [Test] public void 저장하고_불러도_학년이_남고_옛_저장은_1학년()
        {
            var run = New();
            run.S.Credits = Grades.NEED[2] - 1;
            Win(run);
            var back = Run.Load(run.Data, run.Save());
            Assert.AreEqual(run.S.Credits, back.S.Credits);
            Assert.AreEqual(2, back.Grade);
            CollectionAssert.AreEqual(new[] { 2 }, back.S.GradeNews);
            var jo = Newtonsoft.Json.Linq.JObject.Parse(run.Save());
            StringAssert.Contains("grade", jo.ToString());
            foreach (var k in new[] { "grade", "credits", "lastCredit", "gradeNews" }) jo.Remove(k);
            var o = Run.Load(run.Data, jo.ToString());
            Assert.AreEqual(1, o.Grade);
            Assert.AreEqual(0, o.S.Credits);
        }

        [Test] public void 봇_한_판이_학년을_남긴다()
        {
            var r = new RunBot(K.Sample()).RunFull(PARTY, 7, new SimOpts());
            Assert.GreaterOrEqual(r.Grade, 1);
            Assert.AreEqual(Grades.Of(r.Credits), r.Grade);
        }
    }
}
