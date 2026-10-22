using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>난이도 단계(카제나 카오스 난이도 자리 — Tiers.Scale · Run.SetTier): 단계 없음 = 지금 판 · 적 체력 · 피해 배율 · 여는 조건 · 저장.</summary>
    public class TierTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };

        [Test] public void 단계가_없으면_지금_판과_같은_싸움이고_저장에_칸도_없다()
        {
            var d = K.Sample();
            var a = Run.New(d, PARTY, 7);
            var b = Run.New(d, PARTY, 7);
            Assert.IsNull(b.SetTier(0));
            StringAssert.DoesNotContain("\"tier\"", b.Save(), "단계 0 은 판 저장에 안 적힌다(옛 저장 · 옛 엔진과 같은 JSON)");
            Assert.AreEqual(a.Save(), b.Save());
            var (fa, _) = a.OpenFight(); var (fb, _) = b.OpenFight();
            Assert.AreEqual(fa.EnemyHpx, fb.EnemyHpx); Assert.AreEqual(fa.EnemyDmgx, fb.EnemyDmgx);
            Assert.AreEqual(fa.Digest(), fb.Digest(), "같은 전투");
            Assert.AreEqual(fa.Input.SetupJson.ToString(), fb.Input.SetupJson.ToString(), "셋업 JSON 그대로(교차 테이프)");
        }

        [Test] public void 단계는_층_배율_위에_적_체력과_피해를_곱한다()
        {
            var d = K.Sample();
            var a = Run.New(d, PARTY, 7);
            var b = Run.New(d, PARTY, 7);
            Assert.IsNull(b.SetTier(1));
            var sa = a.FoeScaleOf(); var sb = b.FoeScaleOf();
            Assert.AreEqual(sa.hp * Tiers.HP[1], sb.hp, 1e-12);
            Assert.AreEqual(sa.dmg * Tiers.DMG[1], sb.dmg, 1e-12);
            var (fa, _) = a.OpenFight(); var (fb, _) = b.OpenFight();
            Assert.AreEqual(fa.EnemyHpx * Tiers.HP[1], fb.EnemyHpx, 1e-12, "전투 적 체력 배율");
            Assert.AreEqual(fa.EnemyDmgx * Tiers.DMG[1], fb.EnemyDmgx, 1e-12, "전투 적 피해 배율");
            Assert.Less(fb.Enemies.Sum(e => e.MaxHp), fa.Enemies.Sum(e => e.MaxHp), "낮은 단계 — 적 체력이 준다");
            Assert.IsTrue(fb.Enemies.All(e => e.Dmgx == fb.EnemyDmgx), "적마다 피해 배율");
            var hi = Run.New(d, PARTY, 7); hi.SetTier(Tiers.MAX);
            Assert.Greater(hi.FoeScaleOf().hp, sa.hp, "높은 단계 — 더 세다");
        }

        [Test] public void 단계는_범위_안에서_첫_싸움_전에만_바꾸고_판_저장에_남는다()
        {
            var d = K.Sample();
            var run = Run.New(d, PARTY, 7);
            Assert.IsNotNull(run.SetTier(-1)); Assert.IsNotNull(run.SetTier(Tiers.MAX + 1));
            Assert.AreEqual(0, run.S.Tier, "틀린 값은 안 적는다");
            Assert.IsNull(run.SetTier(3));
            var back = Run.Load(d, run.Save());
            Assert.AreEqual(3, back.S.Tier, "판 저장에 남는다");
            Assert.AreEqual(run.FoeScaleOf(), back.FoeScaleOf());
            run.S.Hist.Add(new FightRecord());
            Assert.IsNotNull(run.SetTier(4), "싸움을 시작한 판은 못 바꾼다");
            Assert.AreEqual(3, run.S.Tier);
        }

        [Test] public void 단계_표와_여는_조건()
        {
            Assert.AreEqual(Tiers.HP.Length, Tiers.DMG.Length);
            Assert.AreEqual((1.0, 1.0), Tiers.Scale(0), "0 = 단계 없음");
            Assert.AreEqual((1.0, 1.0), Tiers.Scale(Tiers.MAX + 1));
            for (int t = 2; t <= Tiers.MAX; t++)
            {
                Assert.Greater(Tiers.HP[t], Tiers.HP[t - 1], $"단계 {t} 체력은 앞 단계보다 크다");
                Assert.Greater(Tiers.DMG[t], Tiers.DMG[t - 1]);
            }
            Assert.AreEqual(1, Tiers.Open(0), "처음은 단계 1");
            Assert.AreEqual(4, Tiers.Open(3), "단계 3 완주 → 4 열림");
            Assert.AreEqual(Tiers.MAX, Tiers.Open(Tiers.MAX));
            StringAssert.DoesNotContain("Tiers.", Fingerprint.RulesText(), "단계 표는 규칙 지문 밖 — 표를 고쳐도 공개판 테이프 지문이 안 갈린다");
        }

        [Test] public void 봇_판도_단계를_건다()
        {
            var d = K.Sample();
            int fights = 0, tier = 0; double hpx = 0;
            var o = new SimOpts { Tier = 2, OnFight = (b, r) => { if (fights++ == 0) { tier = r.S.Tier; hpx = b.EnemyHpx / r.FoeScaleOf().hp; } } };
            new RunBot(d).RunFull(PARTY, 7, o);
            Assert.Greater(fights, 0);
            Assert.AreEqual(2, tier, "봇 판에 단계가 적힌다");
            Assert.AreEqual(1.0, hpx, 1e-9, "봇 판의 전투 배율 = 단계를 곱한 판 배율");
            Assert.Throws<System.ArgumentException>(() => new RunBot(d).RunFull(PARTY, 7, new SimOpts { Tier = Tiers.MAX + 1 }));
        }
    }
}
