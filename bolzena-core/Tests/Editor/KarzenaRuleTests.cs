using System.Collections.Generic;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>2026-10-10 카제나 맞추기 — 최소 강인도 · 약점 노출 · 성격 약점 · 지속 피해 약점(건 사도 성격) · 실드 바탕 방어력 · 추가 공격 강인도.</summary>
    public class KarzenaRuleTests
    {
        const string HEROES = "[{id:'mad', name:'광기', role:'딜러', row:'back', hp:1000, atk:100, def:50, crit:0, nature:'광기'}, {id:'live', name:'활발', role:'딜러', row:'back', hp:1000, atk:100, def:50, crit:0, nature:'활발'}]";
        const string CARDS = "[{id:'m_hit', name:'광 때리기', hero:'mad', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}, {id:'l_hit', name:'활 때리기', hero:'live', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},"
            + "{id:'m_pain', name:'광 고통', hero:'mad', cost:0, type:'스킬', fx:[{k:'status', id:'고통', v:4, target:'oneEnemy'}]}, {id:'l_pain', name:'활 고통', hero:'live', cost:0, type:'스킬', fx:[{k:'status', id:'고통', v:4, target:'oneEnemy'}]},"
            + "{id:'shd', name:'막기', hero:'live', cost:1, type:'스킬', fx:[{k:'shield', ratio:1.0}]}]";
        const string FOES = "[{id:'pure', name:'순수 적', hp:1000, nature:'순수', tough:2, intents:[{t:'jam', v:0, rush:0}]}, {id:'big_elite', name:'엘리트', hp:1000, tough:4, intents:[{t:'jam', v:0, rush:0}]}]";
        static GameData D() => K.Data(HEROES, CARDS, FOES);

        [Test] public void 최소_강인도_일반_3_엘리트_5()
        {
            var d = D();
            Assert.AreEqual(3, Battle.ToughOf(d.Enemy("pure"), false), 1e-9, "데이터 2 → 일반 최소 3");
            Assert.AreEqual(5, Battle.ToughOf(d.Enemy("big_elite"), true), 1e-9, "엘리트 데이터 4(+1 여린 적 몫) → 최소 5");
            Assert.AreEqual(3, Battle.ToughOf(d.Enemy("pure"), true, summon: true), 1e-9, "소환물은 일반 기준");
        }

        [Test] public void 약점_노출이면_어느_성격이든_약점()
        {
            var d = D();
            var a = K.Fight(d, new[] { "live" }, new[] { "pure" }); K.Hand(a, "l_hit"); K.Play(a, "l_hit");
            var b = K.Fight(d, new[] { "live" }, new[] { "pure" }); K.E(b).Status[R.EXPOSED] = 1; K.Hand(b, "l_hit"); K.Play(b, "l_hit");
            Assert.AreEqual(1000 - 100, K.Hp(a), "활발은 순수의 약점이 아니다");
            Assert.AreEqual(1000 - 125, K.Hp(b), "약점 노출 — ×1.25");
            var c = K.Fight(d, new[] { "live" }, new[] { "pure" }); K.E(c).Status["활발 약점"] = 1; K.Hand(c, "l_hit"); K.Play(c, "l_hit");
            Assert.AreEqual(1000 - 125, K.Hp(c), "활발 약점 — 활발 사도의 카드가 약점");
            var e = K.Fight(d, new[] { "live" }, new[] { "pure" }); K.E(e).Status["우울 약점"] = 1; K.Hand(e, "l_hit"); K.Play(e, "l_hit");
            Assert.AreEqual(1000 - 100, K.Hp(e), "다른 성격 약점은 들지 않는다");
        }

        [Test] public void 고통_피해의_약점은_건_사도_성격()
        {
            var d = D();
            var w = K.Fight(d, new[] { "mad", "live" }, new[] { "pure" }); K.Hand(w, "m_pain"); K.Play(w, "m_pain"); w.EndTurn();
            var n = K.Fight(d, new[] { "mad", "live" }, new[] { "pure" }); K.Hand(n, "l_pain"); K.Play(n, "l_pain"); n.EndTurn();
            int lw = 1000 - K.Hp(w), ln = 1000 - K.Hp(n);
            Assert.Greater(ln, 0);
            Assert.AreEqual(Num.Round(ln * 1.25), lw, 1, "광기(순수의 약점)가 건 고통은 ×1.25");
        }

        [Test] public void 실드_바탕은_기본_방어력_더하기_추가의_절반()
        {
            var d = D();
            var b = K.Fight(d, new[] { "live" }, new[] { "pure" }, st => st.Gear = new Dictionary<string, Stats> { ["live"] = new Stats { Def = 100 } });
            Assert.AreEqual(Num.Round(50 + 100 * R.GUARD_EXTRA_K), b.GuardAmount(b.Party[0], 1.0, null), "기본 50 + 추가 100 × 0.5");
        }

        [Test] public void 역할별_실드_회복_배율()
        {
            var d = K.Data("[{id:'tk', name:'탱', role:'탱커', row:'front', hp:1000, atk:100, def:100, crit:0, nature:'광기'}]", null, FOES);
            var b = K.Fight(d, new[] { "tk" }, new[] { "pure" });
            double was = R.ROLE_GUARD["탱커"]; R.ROLE_GUARD["탱커"] = 1.5;
            try { Assert.AreEqual(150, b.GuardAmount(b.Party[0], 1.0, null), "탱커 실드 ×1.5"); Assert.AreEqual(150, b.HealAmount(b.Party[0], 1.0, null), "회복도 ×1.5"); }
            finally { R.ROLE_GUARD["탱커"] = was; }
        }

        [Test] public void 같은_역할이_둘이면_실드_배율이_준다()
        {
            var d = K.Data("[{id:'tk', name:'탱', role:'탱커', row:'front', hp:1000, atk:100, def:100, crit:0, nature:'광기'}, {id:'tk2', name:'탱2', role:'탱커', row:'front', hp:1000, atk:100, def:100, crit:0, nature:'광기'}]", null, FOES);
            var one = K.Fight(d, new[] { "tk" }, new[] { "pure" }); var two = K.Fight(d, new[] { "tk", "tk2" }, new[] { "pure" });
            double was = R.ROLE_DUP; R.ROLE_DUP = 0.5;
            try { Assert.AreEqual(100, one.GuardAmount(one.Party[0], 1.0, null)); Assert.AreEqual(67, two.GuardAmount(two.Party[0], 1.0, null), "탱커 둘 — 1 / (1 + 0.5)"); }
            finally { R.ROLE_DUP = was; }
        }

        [Test] public void 첫_수_약점_노출은_첫_두_턴_모두_약점()
        {
            int was = R.OPEN_EXPOSE; R.OPEN_EXPOSE = 2;
            try
            {
                var d = D();
                var b = K.Fight(d, new[] { "live" }, new[] { "pure" }); K.Hand(b, "l_hit"); K.Play(b, "l_hit");
                Assert.AreEqual(1000 - 125, K.Hp(b), "1턴 — 활발도 약점");
                b.EndTurn(); Assert.AreEqual(1, b.St(K.E(b), R.EXPOSED), "2턴 시작에 1 남음");
                b.EndTurn(); Assert.AreEqual(0, b.St(K.E(b), R.EXPOSED), "3턴에는 풀림");
            }
            finally { R.OPEN_EXPOSE = was; }
        }

        [Test] public void 협공_추가_공격도_강인도를_깎는다()
        {
            var d = D();
            var b = K.Fight(d, new[] { "mad", "live" }, new[] { "pure" }); b.Pool.Status["협공"] = 1; K.Hand(b, "l_hit");
            double t0 = K.E(b).Tough; K.Play(b, "l_hit");
            Assert.Greater(t0 - K.E(b).Tough, R.ToughDmg(1, false, false) + 1e-9, "카드 1/3칸 + 협공 몫");
        }
    }
}
