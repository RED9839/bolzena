using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>플레이 기록 v2(RunRecord · BattleTape · RecState.Picks) — 형식 · 크기 상한 · 선택 기록 · 저장 이어가기.</summary>
    public class PlayRecordTests
    {
        // 학년(학점제)은 2026-10-09 꺼 두었다(R.GRADE_ON) — 학년 · 학점을 보는 이 시험은 켜고 돈다
        bool gradeOn0;
        [SetUp] public void GradeOn() { gradeOn0 = R.GRADE_ON; R.GRADE_ON = true; }
        [TearDown] public void GradeBack() => R.GRADE_ON = gradeOn0;

        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        const string ANON = "0123456789abcdef0123456789abcdef";

        static (Run run, PlayRecord rec) BotRun(long seed, int max = RunRecord.MAX_BYTES)
        {
            Run end = null;
            new RunBot(K.Sample()).RunFull(PARTY, seed, new SimOpts { Skilled = true, OnEnd = r => end = r });
            Assert.IsNotNull(end, "봇 판이 끝나지 않았다");
            var rec = RunRecord.Of(end, new RecordMeta { Anon = ANON, Ver = "0.1.0", Os = "web", Result = end.S.Done == "clear" ? "win" : "lose", Board = 3 }, max);
            return (end, rec);
        }

        [Test] public void 판_기록은_받는_쪽_검사를_통과하고_싸움마다_턴_기록이_있다()
        {
            var (run, rec) = BotRun(11);
            var json = RunRecord.Json(rec);
            Assert.IsNull(RunRecord.Check(json), RunRecord.Check(json));
            Assert.AreEqual("bolzena-record", rec.Kind); Assert.AreEqual(2, rec.V);
            Assert.AreEqual(run.S.Hist.Count, rec.Fights.Count);
            Assert.AreEqual(PARTY, rec.Party.Select(p => p.Id).ToList());
            Assert.AreEqual(3, rec.Board);
            foreach (var f in rec.Fights)
            {
                Assert.IsNotNull(f.Log, "턴 기록");
                Assert.AreEqual(f.Turns, f.Log.Count, "턴마다 한 줄");
                Assert.AreEqual(f.Plays, f.Log.Sum(t => t.C.Count), "낸 카드 수 = 턴별 카드 합");
                Assert.AreEqual(f.Ults, f.Log.Sum(t => t.U));
                Assert.IsNotNull(f.Deck);
            }
            Assert.IsTrue(rec.Fights.SelectMany(f => f.Log).Any(t => t.F != null && t.F.Any(a => a.D > 0)), "적의 수와 피해");
            if (rec.Result == "lose") { Assert.IsNotNull(rec.Death); Assert.AreEqual(rec.Fights.Last().Foes, rec.Death.Foes); }
            Assert.Less(RunRecord.Bytes(rec), RunRecord.MAX_BYTES);
            Assert.AreEqual(0, rec.Trim);
        }

        [Test] public void 선택_기록_제시된_것과_고른_것()
        {
            var (_, rec) = BotRun(5);
            Assert.IsTrue(rec.Picks.Any(p => p.K == "equip"), "전리품 아티팩트");
            foreach (var p in rec.Picks.Where(p => p.Offer != null && p.Pick != null && (p.K == "equip" || p.K == "grade" || p.K == "copy" || p.K == "train")))
                Assert.Contains(p.Pick, p.Offer, $"{p.K} 고른 것은 제시된 것 가운데");
            Assert.IsTrue(rec.Picks.Any(p => p.K == "grade" && p.Offer.Count >= 2 && p.Pick != null), "진급 보상");
            Assert.IsTrue(rec.Picks.All(p => p.F >= 1 && p.F <= 2));
        }

        [Test] public void 상한을_넘으면_일반_전투_턴부터_줄이고_큰_싸움은_남긴다()
        {
            var (_, full) = BotRun(11, int.MaxValue);
            int size = RunRecord.Bytes(full);
            var (_, small) = BotRun(11, size - 200);
            Assert.AreEqual(1, small.Trim);
            Assert.LessOrEqual(RunRecord.Bytes(small), size - 200);
            Assert.IsTrue(small.Fights.Where(f => f.Kind == "elite" || f.Kind == "boss" || f.Result != "win").All(f => f.Log != null), "엘리트 · 보스 · 진 싸움 턴은 남는다");
            Assert.IsTrue(small.Fights.Any(f => f.Kind == "fight" && f.Log == null));
            var (_, tiny) = BotRun(11, 3000);
            Assert.GreaterOrEqual(tiny.Trim, 4, "선택 기록까지 줄였다");
            Assert.IsNull(tiny.Picks);
        }

        [Test] public void 판_저장을_이어도_기록이_이어지고_복사한_전투에는_녹화가_없다()
        {
            var d = K.Sample();
            var run = Run.New(d, PARTY, 3);
            run.S.Rec.Began = "2026-10-09T10:00Z";
            run.MapOf();
            var id = run.Reachable().First();
            run.EnterNode(id);
            var (b, _) = run.OpenFight();
            Assert.IsNotNull(b.Tape);
            Assert.IsNull(b.Clone().Tape, "봇이 읽는 복사본은 녹화하지 않는다");
            new Bots(d).SmartPlay(b);
            var b2 = Battle.Load(d, b.Save());
            Assert.AreEqual(b.Tape.Turns.Sum(t => t.C.Count), b2.Tape.Turns.Sum(t => t.C.Count), "전투 저장에 녹화가 남는다");
            while (b2.Over == null && b2.Turn < 40) { new Bots(d).SmartPlay(b2); if (b2.Over == null) b2.EndTurn(); }
            run.AfterFight(b2);
            var r2 = Run.Load(d, run.Save());
            Assert.AreEqual("2026-10-09T10:00Z", r2.S.Rec.Began);
            Assert.AreEqual(run.S.Rec.Picks.Count, r2.S.Rec.Picks.Count);
            Assert.AreEqual(b2.PlaysTotal, r2.S.Hist[0].Plays);
            var rec = RunRecord.Of(r2, new RecordMeta { Anon = ANON, Result = "quit", Ended = new System.DateTime(2026, 10, 9, 10, 42, 30, System.DateTimeKind.Utc) });
            Assert.AreEqual(42, rec.Min);
            Assert.AreEqual("2026-10-09T10:42Z", rec.Ended);
        }

        [Test] public void 받는_쪽_검사는_익명_id_와_결과를_본다()
        {
            var (_, rec) = BotRun(2);
            rec.Anon = "me@example.com"; Assert.AreEqual("anon", RunRecord.Check(RunRecord.Json(rec)));
            rec.Anon = ANON; rec.Result = "cheat"; Assert.AreEqual("result", RunRecord.Check(RunRecord.Json(rec)));
            rec.Result = "abandon"; Assert.IsNull(RunRecord.Check(RunRecord.Json(rec)));
        }
    }
}
