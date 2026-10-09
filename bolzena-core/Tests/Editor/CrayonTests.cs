using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>크레파스 보드(RunCrayon.cs) — 얻기 · 칠하기(단계) · 판에 걸기 · 진행 코드.</summary>
    public class CrayonTests
    {
        // 학년(학점제)은 2026-10-09 꺼 두었다(R.GRADE_ON) — 학년 · 학점을 보는 이 시험은 켜고 돈다
        bool gradeOn0;
        [SetUp] public void GradeOn() { gradeOn0 = R.GRADE_ON; R.GRADE_ON = true; }
        [TearDown] public void GradeBack() => R.GRADE_ON = gradeOn0;

        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        const string TABLE = @"{earn:{low:2, lowPerFloor:2, lowPerGrade:1, lowWin:3, midFloor2:2, midPerElite:1, highPerBoss:1, highClear:1, topGrad:1, topClear:1}, cols:3, cells:[
 {id:'atk', kind:'atk', v:0.02, costs:[{tier:0, n:3}, {tier:0, n:4}, {tier:1, n:1}]}, {id:'hp', kind:'hp', v:0.02, costs:[{tier:0, n:1}, {tier:0, n:1}]}, {id:'blank', kind:'blank'},
 {id:'gold', kind:'gold', v:50, costs:[{tier:2, n:1}]}, {id:'credits', kind:'credits', v:2, costs:[{tier:3, n:1}]}, {id:'rm', kind:'removeCost', v:25, costs:[{tier:2, n:1}]}]}";
        static CrayonTable T() => Crayon.Parse(TABLE);

        [Test] public void 표를_읽고_검사한다()
        {
            var t = T();
            Assert.AreEqual(6, t.Cells.Count);
            Assert.IsEmpty(Crayon.Check(t));
            t.Cells[0].Costs[1].Tier = 3;   // 2단계가 3단계(중급)보다 높은 등급
            Assert.IsNotEmpty(Crayon.Check(t));
            t = T();
            for (int i = 0; i < 7; i++) t.Cells[0].Costs.Add(new CrayonCost { Tier = 3, N = 1 });   // 공격 +20% — 상한(+15%)을 넘는다
            Assert.IsNotEmpty(Crayon.Check(t));
            CollectionAssert.AreEqual(new[] { 9, 1, 2, 1 }, Crayon.TotalCost(T()));
        }

        [Test] public void 판이_끝나면_등급별로_받는다()
        {
            var t = T();
            var s = new RunState { Floor = 1, Grade = 5 };
            s.Hist.Add(new FightRecord { Kind = "elite", Result = "win" });
            s.Hist.Add(new FightRecord { Kind = "boss", Result = "win" });
            s.Hist.Add(new FightRecord { Kind = "boss", Result = "lose" });
            CollectionAssert.AreEqual(new[] { 2 + 2 + 5, 2 + 1, 1, 0 }, Crayon.EarnOf(t, s, false));
            s.Grade = 6;
            CollectionAssert.AreEqual(new[] { 2 + 2 + 6, 2 + 1, 1, 1 }, Crayon.EarnOf(t, s, false), "졸업하면 최상급 하나");
            CollectionAssert.AreEqual(new[] { 2 + 2 + 6 + 3, 3, 2, 2 }, Crayon.EarnOf(t, s, true));
            s.Grade = 5;
            CollectionAssert.AreEqual(new[] { 2 + 1, 0, 0, 0 }, Crayon.EarnOf(t, new RunState { Floor = 0, Grade = 1 }, false), "1층에서 지면 하급만 조금");
            var save = new CrayonSave();
            Crayon.Earn(t, save, s, true);
            Assert.AreEqual(12, save.Have[0]); Assert.AreEqual(1, save.Runs);
        }

        [Test] public void 능력치_칸은_단계마다_값이_오르고_높은_단계는_높은_등급_빈칸은_못_칠한다()
        {
            var t = T(); var s = new CrayonSave();
            s.Have[0] = 7;
            Assert.IsNull(Crayon.Paint(t, s, "atk"));   // 하급 3개
            Assert.IsNull(Crayon.Paint(t, s, "atk"));   // 하급 4개
            Assert.AreEqual(0, s.Have[0]); Assert.AreEqual(2, s.LevelOf("atk"));
            Assert.AreEqual(1, Crayon.NextCost(t.Cell("atk"), s).Tier, "3단계부터 중급");
            StringAssert.Contains("중급", Crayon.Paint(t, s, "atk"));
            s.Have[1] = 1;
            Assert.IsNull(Crayon.Paint(t, s, "atk"));
            Assert.IsNull(Crayon.NextCost(t.Cell("atk"), s));
            StringAssert.Contains("빈칸", Crayon.Paint(t, s, "blank"));
            s.Have[2] = 5;
            Assert.IsNull(Crayon.Paint(t, s, "gold"));
            StringAssert.Contains("다 칠한", Crayon.Paint(t, s, "gold"));
            Assert.AreEqual(0.06, Crayon.Perks(t, s)["atk"], 1e-9);
            var all = Crayon.Perks(t, null, all: true);
            Assert.AreEqual(0.06, all["atk"], 1e-9); Assert.AreEqual(50, all["gold"]);
            Assert.IsFalse(all.ContainsKey("blank"));
            Assert.IsFalse(Crayon.Done(t, s));
        }

        [Test] public void 보드_효과가_판에_든다()
        {
            var d = K.Sample();
            var run = Run.New(d, PARTY, 1);
            int max = run.S.PartyMaxHp, gold = run.S.Gold, rp = run.RemovePrice;
            var b0 = run.OpenFight().battle;
            run.ApplyPerks(new Dictionary<string, double> { ["atk"] = 0.1, ["hp"] = 0.05, ["crit"] = 3, ["gold"] = 50, ["credits"] = 2, ["removeCost"] = 25, ["oraclePick"] = 1, ["critDmg"] = 0.2 });
            Assert.AreEqual(max + Num.Round(PARTY.Sum(k => d.Hero(k).Hp) * 0.05), run.S.PartyMaxHp);
            Assert.AreEqual(gold + 50, run.S.Gold);
            Assert.AreEqual(2, run.S.Credits);
            Assert.AreEqual(rp - 25, run.RemovePrice);
            Assert.AreEqual(R.ORACLE_PICKS + 1, run.OraclePicks);
            var b = run.OpenFight().battle;
            for (int i = 0; i < PARTY.Count; i++)
            {
                Assert.AreEqual(b0.Party[i].Atk + Num.Round(d.Hero(PARTY[i]).Atk * 0.1), b.Party[i].Atk);
                Assert.AreEqual(b0.Party[i].Crit + 3, b.Party[i].Crit);
            }
            Assert.AreEqual(0.2, b.CritDmgX, 1e-9);
            Assert.AreEqual(0.2, Battle.Load(d, b.Save()).CritDmgX, 1e-9);
            Assert.AreEqual(0.1, Run.Load(d, run.Save()).Perk("atk"), 1e-9);
            Assert.AreEqual(R.FinalDamage(100, 1, crit: true) + 20, R.FinalDamage(100, 1, crit: true, critX: 0.2));
        }

        [Test] public void 업그레이드_없으면_판이_그대로()
        {
            var a = Run.New(K.Sample(), PARTY, 5); var b = Run.New(K.Sample(), PARTY, 5);
            b.ApplyPerks(null); b.ApplyPerks(new Dictionary<string, double>());
            Assert.AreEqual(a.Save(), b.Save());
        }

        [Test] public void 진행_코드는_되돌아오고_틀린_코드는_거부한다()
        {
            var t = T();
            var s = new CrayonSave { Have = new[] { 5, 4, 3, 2 }, Earned = new[] { 50, 9, 8, 7 }, Runs = 5, Level = new Dictionary<string, int> { ["atk"] = 2 } };
            var code = Crayon.Export(s);
            StringAssert.StartsWith("BZP1-", code);
            var (back, why) = Crayon.Import(code, t);
            Assert.IsNull(why);
            CollectionAssert.AreEqual(s.Have, back.Have); Assert.AreEqual(2, back.LevelOf("atk"));
            var parts = code.Split('-');
            var bad = parts[0] + "-" + (parts[1][0] == 'e' ? "f" : "e") + parts[1].Substring(1) + "-" + parts[2];
            Assert.IsNotNull(Crayon.Import(bad, t).why, "체크섬");
            Assert.IsNotNull(Crayon.Import("아무거나", t).why);
            Assert.IsNotNull(Crayon.Import("", t).why);
            Assert.IsNotNull(Crayon.Import(Crayon.Export(new CrayonSave { Level = new Dictionary<string, int> { ["atk"] = 99 } }), t).why, "단계를 넘음");
            Assert.IsNotNull(Crayon.Import(Crayon.Export(new CrayonSave { Level = new Dictionary<string, int> { ["blank"] = 1 } }), t).why, "빈칸");
            Assert.IsNotNull(Crayon.Import(Crayon.Export(new CrayonSave { Level = new Dictionary<string, int> { ["zz"] = 1 } }), t).why, "모르는 칸");
        }

        [Test] public void 진행_코드_왕복_빈_일부_꽉_참_그리고_옛_코드()
        {
            var t = T();
            var empty = (new CrayonSave(), new Dictionary<string, string>());
            var some = (new CrayonSave { Have = new[] { 3, 1, 0, 0 }, Earned = new[] { 9, 2, 1, 0 }, Runs = 2, Level = new Dictionary<string, int> { ["atk"] = 1 } },
                new Dictionary<string, string> { ["recent"] = "rico|carrot|", ["preset1"] = "rico|carrot|sion" });
            var full = (new CrayonSave { Have = new[] { 99, 50, 20, 9 }, Earned = new[] { 300, 90, 40, 15 }, Runs = 40, Level = t.Cells.Where(c => !c.Blank).ToDictionary(c => c.Id, c => c.Levels) },
                new Dictionary<string, string> { ["recent"] = "a|b|c", ["preset1"] = "a|b|c", ["preset5"] = "c||a", ["tag1"] = "광기", ["clears"] = "a=3;b=1" });
            foreach (var (s, x) in new[] { empty, some, full })
            {
                var code = Crayon.Export(s, x);
                Assert.IsNotEmpty(code);
                var (back, bx, why) = Crayon.ImportAll(code, t);
                Assert.IsNull(why, why);
                CollectionAssert.AreEqual(s.Have, back.Have); CollectionAssert.AreEqual(s.Earned, back.Earned); Assert.AreEqual(s.Runs, back.Runs);
                CollectionAssert.AreEquivalent(s.Level, back.Level);
                CollectionAssert.AreEquivalent(x, bx);
                Assert.AreEqual(code, Crayon.Export(back, bx), "다시 내보내도 같은 코드");
            }
            // 옛 1판 코드(보드만 · v 없음)도 읽는다 — 덧붙임은 null
            var oldBody = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"h\":[1,2,0,0],\"e\":[1,2,0,0],\"l\":{\"atk\":1},\"r\":1}"));
            var oldCode = $"BZP1-{oldBody}-{Crayon.Checksum(oldBody):x8}";
            var (o, ox, ow) = Crayon.ImportAll(oldCode, t);
            Assert.IsNull(ow); Assert.IsNull(ox); Assert.AreEqual(1, o.LevelOf("atk"));
            // 모르는 새 판은 거부
            var newBody = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"v\":99,\"h\":[0,0,0,0],\"e\":[0,0,0,0],\"l\":{},\"r\":0}"));
            Assert.IsNotNull(Crayon.ImportAll($"BZP1-{newBody}-{Crayon.Checksum(newBody):x8}", t).why);
        }

        [Test] public void 봇이_보드_효과를_건_판을_돈다()
        {
            var r = new RunBot(K.Sample()).RunFull(PARTY, 3, new SimOpts { Perks = Crayon.Perks(T(), null, all: true) });
            Assert.GreaterOrEqual(r.Credits, 2);
        }
    }
}
