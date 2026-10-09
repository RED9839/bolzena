using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>크레파스 보드 판 2(2026-10-09) — 3 · 4줄 칸도 여러 단계 · 옛 저장(한 번 칸) 옮기기 · 정수 효과의 확률 몫.</summary>
    public class CrayonStepTests
    {
        // 학년(학점제)은 2026-10-09 꺼 두었다(R.GRADE_ON) — 학년 · 학점을 보는 이 시험은 켜고 돈다
        bool gradeOn0;
        [SetUp] public void GradeOn() { gradeOn0 = R.GRADE_ON; R.GRADE_ON = true; }
        [TearDown] public void GradeBack() => R.GRADE_ON = gradeOn0;

        const string TABLE = @"{cols:3, cells:[
 {id:'atk', kind:'atk', v:0.02, costs:[{tier:0, n:3}, {tier:0, n:4}, {tier:1, n:1}]},
 {id:'gold', kind:'gold', v:10, oldLevel:3, costs:[{tier:0, n:3}, {tier:0, n:4}, {tier:1, n:1}, {tier:2, n:2}]},
 {id:'credits', kind:'credits', v:0.25, oldLevel:4, costs:[{tier:0, n:4}, {tier:1, n:1}, {tier:1, n:2}, {tier:3, n:1}]}]}";
        static CrayonTable T() => Crayon.Parse(TABLE);

        [Test] public void 옛_코드의_한_번_칸은_같은_효과의_단계로_옮긴다()
        {
            var t = T();
            Assert.IsEmpty(Crayon.Check(t));
            // 판 2 코드(보드 판 1) — gold · credits 를 한 번 칠했었다
            var body = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"v\":2,\"h\":[0,0,0,0],\"e\":[0,0,0,0],\"l\":{\"atk\":2,\"gold\":1,\"credits\":1},\"r\":3,\"x\":{}}"));
            var (s, _, why) = Crayon.ImportAll($"BZP1-{body}-{Crayon.Checksum(body):x8}", t);
            Assert.IsNull(why, why);
            Assert.AreEqual(2, s.LevelOf("atk"), "능력치 칸은 그대로");
            Assert.AreEqual(3, s.LevelOf("gold"), "골드 30 = 10 × 3단계");
            Assert.AreEqual(4, s.LevelOf("credits"), "학점 +1 = 다 칠함");
            Assert.AreEqual(Crayon.BOARD_VER, s.Ver);
            // 새 코드는 옮기지 않는다(새 보드의 1단계는 1단계)
            var fresh = new CrayonSave { Level = new Dictionary<string, int> { ["gold"] = 1 } };
            var (back, _, w2) = Crayon.ImportAll(Crayon.Export(fresh), t);
            Assert.IsNull(w2); Assert.AreEqual(1, back.LevelOf("gold"));
        }

        [Test] public void 로컬_저장도_판이_없으면_한_번만_옮긴다()
        {
            var t = T();
            var old = new CrayonSave { Ver = 0, Level = new Dictionary<string, int> { ["gold"] = 1 } };
            Assert.IsTrue(Crayon.Migrate(t, old)); Assert.AreEqual(3, old.LevelOf("gold"));
            Assert.IsFalse(Crayon.Migrate(t, old), "두 번 옮기지 않는다");
            var now = new CrayonSave { Level = new Dictionary<string, int> { ["gold"] = 1 } };
            Assert.IsFalse(Crayon.Migrate(t, now)); Assert.AreEqual(1, now.LevelOf("gold"));
        }

        [Test] public void 학점_후보는_덜_칠하면_확률_다_칠하면_그대로()
        {
            Assert.AreEqual("모험 시작 학점 +1 (25% 확률)", Crayon.EffectOf("credits", 0.25));
            Assert.AreEqual("모험 시작 학점 +1", Crayon.EffectOf("credits", 1.0));
            Assert.AreEqual("신탁 후보 +1 (75% 확률)", Crayon.EffectOf("oraclePick", 0.75));
            var full = Crayon.Perks(T(), null, all: true);
            Assert.AreEqual(1.0, full["credits"], 1e-9); Assert.AreEqual(40, full["gold"], 1e-9);
            // 다 칠함 → 언제나 +1, 절반 → 판마다 0 또는 1(여러 씨앗에서 둘 다 나온다 · 작은 씨앗은 xorshift 첫 수가 작아 씨앗을 넓게 흩는다)
            var d = K.Sample(); var party = new List<string> { "rico", "carrot", "sion" };
            var r1 = Run.New(d, party, 1); r1.ApplyPerks(new Dictionary<string, double> { ["credits"] = 1.0 }); Assert.AreEqual(1, r1.S.Credits);
            var got = Enumerable.Range(1, 40).Select(seed => { var r = Run.New(d, party, seed * 2654435761L % 4294967291L); r.ApplyPerks(new Dictionary<string, double> { ["credits"] = 0.5 }); return r.S.Credits; }).ToList();
            CollectionAssert.IsSubsetOf(got.Distinct(), new[] { 0, 1 }); Assert.Contains(0, got); Assert.Contains(1, got);
        }

        [Test] public void 골디_할인권은_상점_전_품목을_깎는다()
        {
            Assert.AreEqual("상점 가격 -5%", Crayon.EffectOf("shopDiscount", 0.05));
            var d = K.Sample(); var party = new List<string> { "rico", "carrot", "sion" };
            var a = Run.New(d, party, 7); var b = Run.New(d, party, 7);
            b.ApplyPerks(new Dictionary<string, double> { ["shopDiscount"] = 0.2 });
            var sa = a.RollShop(); var sb = b.RollShop();
            Assert.AreEqual(sa.Items.Count, sb.Items.Count);
            for (int i = 0; i < sa.Items.Count; i++)
            {
                Assert.AreEqual(0, sa.Items[i].Base, "할인이 없으면 Base 0");
                Assert.AreEqual(sa.Items[i].Id, sb.Items[i].Id, "진열은 같다(난수를 안 쓴다)");
                Assert.AreEqual((int)System.Math.Floor(sa.Items[i].Price * 0.8 + 0.5), sb.Items[i].Price);
                Assert.AreEqual(sa.Items[i].Price, sb.Items[i].Base);
            }
            Assert.AreEqual(64, b.RemovePrice, "빼기 80 → 64"); Assert.AreEqual(80, b.RemoveBase);
            Assert.AreEqual((int)System.Math.Floor(a.RerollPrice * 0.8 + 0.5), b.RerollPrice);
        }
    }
}

namespace Bolzena.Core.Tests
{
    /// <summary>학년 꺼짐(R.GRADE_ON false, 2026-10-09) — 학점 · 진급 · 보상이 판에서 사라지고, 예습 노트는 시작 신탁이 된다.</summary>
    public class GradeOffTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        bool on0;
        [SetUp] public void Off() { on0 = R.GRADE_ON; R.GRADE_ON = false; }
        [TearDown] public void Back() => R.GRADE_ON = on0;

        [Test] public void 학년이_꺼지면_이겨도_학점_진급_보상이_없다()
        {
            var run = Run.New(K.Sample(), PARTY, 1);
            int max = run.S.PartyMaxHp;
            for (int i = 0; i < 4; i++)
            {
                run.S.Elite = true; run.S.Node = 0;
                var (b, _) = run.OpenFight();
                b.Over = "win"; run.AfterFight(b);
            }
            Assert.AreEqual(0, run.S.Credits); Assert.AreEqual(1, run.Grade); Assert.AreEqual(0, run.S.LastCredit);
            Assert.IsNull(run.GradeOfferNow); Assert.IsEmpty(run.PopGradeNews());
            Assert.AreEqual(max, run.S.PartyMaxHp, "진급 HP 없음");
            run.ApplyPerks(new Dictionary<string, double> { ["credits"] = 2 });
            Assert.AreEqual(0, run.S.Credits, "옛 보드의 시작 학점도 안 든다");
        }

        [Test] public void 예습_노트는_모험_시작_때_카드_한_장에_신탁()
        {
            Assert.AreEqual("모험 시작 때 25% 확률로 무작위 카드 1장에 신탁", Crayon.EffectOf("startOracle", 0.25));
            Assert.AreEqual("모험 시작 때 무작위 카드 1장에 신탁", Crayon.EffectOf("startOracle", 1.0));
            var d = K.Sample();
            var run = Run.New(d, PARTY, 3);
            var five = d.Cards.Values.FirstOrDefault(c => (c.Unique || c.Neutral) && c.Oracles.Count == 5);
            Assert.IsNotNull(five, "샘플에 신탁 다섯 카드");
            if (run.FlashTargets().Count == 0) run.S.Deck.Add(five.Id);
            Assert.AreEqual(0, run.S.Flash.Count);
            run.ApplyPerks(new Dictionary<string, double> { ["startOracle"] = 1.0 });
            Assert.AreEqual(1, run.S.Flash.Count);
            var a = Run.New(K.Sample(), PARTY, 3); a.ApplyPerks(new Dictionary<string, double> { ["gold"] = 10 });
            Assert.AreEqual(0, a.S.Flash.Count, "몫이 없으면 신탁 없음");
        }
    }
}
