using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>학점제 학년(RunGrade.cs) — 학점 · 진급 · 보상 · 저장.</summary>
    public class GradeTests
    {
        // 학년(학점제)은 2026-10-09 꺼 두었다(R.GRADE_ON) — 학년 · 학점을 보는 이 시험은 켜고 돈다
        bool gradeOn0;
        [SetUp] public void GradeOn() { gradeOn0 = R.GRADE_ON; R.GRADE_ON = true; }
        [TearDown] public void GradeBack() => R.GRADE_ON = gradeOn0;

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

        [Test] public void 졸업만_전투_시작_게이지()
        {
            var run = New();
            run.S.Gauge = 10;
            run.S.Grade = 5; Assert.AreEqual(10, run.OpenFight().battle.Gauge);
            run.S.Grade = Grades.GRAD; Assert.AreEqual(10 + Grades.GRAD_GAUGE, run.OpenFight().battle.Gauge);
        }

        [Test] public void 진급마다_서로_다른_보상_셋을_고른다()
        {
            for (long seed = 1; seed <= 30; seed++)
            {
                var run = New(seed);
                run.S.Credits = Grades.NEED[2] - 1;
                Win(run);
                var off = run.GradeOfferNow;
                Assert.IsNotNull(off); Assert.AreEqual(2, off.Grade);
                Assert.AreEqual(Grades.OFFER_N, off.Choices.Count);
                Assert.AreEqual(Grades.OFFER_N, off.Choices.Select(c => c.Kind).Distinct().Count());
                Assert.IsTrue(off.Choices.All(c => Grades.KINDS.Contains(c.Kind) && Grades.Describe(c).desc.Length > 0));
            }
        }

        [Test] public void 보상을_고르면_바로_받고_다음_보상으로()
        {
            var run = New();
            run.S.Credits = Grades.NEED[4] - Grades.CreditOf("boss");
            run.S.Grade = Grades.Of(run.S.Credits);
            Win(run, "boss");   // 두 학년을 넘는다 — 보상 둘
            Assert.AreEqual(2, run.S.GradeOffers.Count);
            run.S.GradeOffers[0].Choices[0] = new GradeChoice { Kind = "gold", V = 77 };
            int g0 = run.S.Gold;
            var t = run.TakeGrade(0);
            Assert.AreEqual("gold", t.Choice.Kind);
            Assert.AreEqual(g0 + 77, run.S.Gold);
            Assert.AreEqual(1, run.S.GradeOffers.Count, "다음 보상이 남는다");
            run.S.GradeOffers[0].Choices[1] = new GradeChoice { Kind = "remove", V = 1 };
            var t2 = run.TakeGrade(1);
            Assert.IsTrue(t2.Remove);
            int n = run.S.Deck.Count; var id = run.S.Deck[0];
            Assert.IsNull(run.GradeRemove(id));
            Assert.AreEqual(n - 1, run.S.Deck.Count);
            Assert.IsNull(run.GradeOfferNow);
            Assert.IsNull(run.TakeGrade(0), "고를 것이 없다");
        }

        [Test] public void 보상_종류마다_받는다()
        {
            foreach (var kind in Grades.KINDS)
            {
                var run = New(5);
                run.S.GradeOffers.Add(new GradeOffer { Grade = 3, Choices = new List<GradeChoice> { new GradeChoice { Kind = kind, V = Grades.ValueOf(kind, 3), Grade = Grades.GradeOf(kind, 3) } } });
                int deck = run.S.Deck.Count, hp0 = run.S.PartyHp = run.S.PartyMaxHp / 2, bag = run.S.Bag.Count, gauge = run.S.Gauge, gold = run.S.Gold;
                var t = run.TakeGrade(0);
                Assert.IsNotNull(t, kind);
                switch (kind)
                {
                    case "gold": Assert.Greater(run.S.Gold, gold); break;
                    case "heal": Assert.Greater(run.S.PartyHp, hp0); break;
                    case "gauge": Assert.Greater(run.S.Gauge, gauge); break;
                    case "flash": Assert.IsTrue(t.Flash != null || t.Note != null, kind); if (t.Flash != null) Assert.IsTrue(run.TakeFlash(t.Flash.CardId, t.Flash.Picks[0])); break;
                    case "grace": Assert.IsTrue(t.Card != null ? run.S.Deck.Contains(t.Card) : t.Note != null, kind); break;
                    case "remove": Assert.IsTrue(t.Remove); break;
                    case "equip": Assert.IsTrue(t.Equip != null ? run.S.Bag.Count == bag + 1 : t.Note != null, kind); break;
                    case "neutral": Assert.IsTrue(t.Card != null || t.Note != null, kind); break;
                    case "prep": Assert.AreEqual(Grades.PREP_FIGHTS, run.S.PrepLeft); Assert.Greater(run.S.PrepHand, 0); break;
                    case "mock": Assert.AreEqual(Grades.MOCK_FIGHTS, run.S.MockLeft); Assert.Greater(run.S.MockMorale, 0); break;
                }
            }
        }

        [Test] public void 예습_노트는_다음_전투_셋_첫_손패_전술_교본은_엘리트_보스만()
        {
            var run = New(9);
            int hand0 = run.OpenFight().battle.Hand.Count;
            run.S.GradeOffers.Add(new GradeOffer { Grade = 2, Choices = new List<GradeChoice> { new GradeChoice { Kind = "prep", V = 1 }, new GradeChoice { Kind = "mock", V = 1 } } });
            run.TakeGrade(0);
            for (int i = 0; i < Grades.PREP_FIGHTS; i++) Assert.AreEqual(hand0 + 1, run.OpenFight().battle.Hand.Count, $"{i + 1}번째 전투");
            Assert.AreEqual(hand0, run.OpenFight().battle.Hand.Count, "횟수가 다 되면 끝");
            run.S.GradeOffers.Add(new GradeOffer { Grade = 2, Choices = new List<GradeChoice> { new GradeChoice { Kind = "mock", V = 1 } } });
            run.TakeGrade(0);
            Assert.AreEqual(0, run.OpenFight().battle.St(run.OpenFight().battle.Pool, "사기"), "일반 전투에는 안 걸린다");
            Assert.AreEqual(1, run.S.MockLeft);
            run.S.Elite = true;
            var b = run.OpenFight().battle;
            Assert.AreEqual(1, b.St(b.Pool, "사기"));
            Assert.AreEqual(0, run.S.MockLeft);
        }

        [Test] public void 졸업은_선물_고르지_않는다()
        {
            var run = New();
            run.S.Grade = Grades.GRAD - 1; run.S.Credits = Grades.NEED[Grades.GRAD] - 1;
            run.S.PartyHp = 1;
            Win(run);
            Assert.AreEqual(Grades.GRAD, run.Grade);
            Assert.IsNull(run.GradeOfferNow, "졸업은 고르는 보상이 아니다");
            Assert.GreaterOrEqual(run.S.PartyHp, Num.Round(run.S.PartyMaxHp * Grades.GRAD_HEAL));
            Assert.IsTrue(run.S.GradeGift == null || run.S.Deck.Contains(run.S.GradeGift));
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
