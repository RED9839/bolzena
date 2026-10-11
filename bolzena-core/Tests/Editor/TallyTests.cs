using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>전투 기록(2026-10-11, 데미지 지표) — 사도별 피해 · 실드 · 회복을 낸 사도 몫으로 센다.</summary>
    public class TallyTests
    {
        const string CARDS = @"[
 {id:'nuke_c', name:'다 큰 한 방', hero:'c', cost:0, type:'공격', fx:[{k:'dmg', ratio:30, target:'oneEnemy'}]},
 {id:'hit_c0', name:'다 톡', hero:'c', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'wall_b', name:'나 실드', hero:'b', cost:0, type:'스킬', fx:[{k:'shield', ratio:2.0}]},
 {id:'mend_b', name:'나 회복', hero:'b', cost:0, type:'스킬', fx:[{k:'heal', ratio:3.0}]},
 {id:'rift_c', name:'다 균열', hero:'c', cost:0, type:'스킬', fx:[{k:'status', id:'균열', v:3, target:'oneEnemy'}]}
]";

        static Battle Fight(GameData d) => K.Fight(d, new[] { "a", "b", "c" }, new[] { "dummy" });
        static HeroTally T(Battle b, string k) => b.Tally.TryGetValue(k, out var t) ? t : new HeroTally();

        [Test] public void 카드_피해는_카드_주인_몫이고_넘친_피해는_세지_않는다()
        {
            var b = Fight(K.Data(cards: CARDS));
            K.Hand(b, "hit_c0");
            int hp0 = K.Hp(b);
            K.Play(b, "hit_c0");
            Assert.AreEqual(hp0 - K.Hp(b), T(b, "c").Dealt, "준 피해 = 깎인 HP");
            Assert.AreEqual(0, T(b, "a").Dealt);

            int left = K.Hp(b), before = T(b, "c").Dealt;
            K.Hand(b, "nuke_c");
            K.Play(b, "nuke_c");
            Assert.IsTrue(K.E(b).Dead);
            Assert.AreEqual(left, T(b, "c").Dealt - before, "쓰러뜨린 일격은 남은 HP 까지만");
        }

        [Test] public void 적의_실드를_벗긴_양도_피해에_든다()
        {
            var b = Fight(K.Data(cards: CARDS));
            K.E(b).Shield = 40;
            int hp0 = K.Hp(b);
            K.Hand(b, "hit_c0");
            K.Play(b, "hit_c0");
            Assert.AreEqual(0, K.E(b).Shield);
            Assert.AreEqual(hp0 - K.Hp(b) + 40, T(b, "c").Dealt);
        }

        [Test] public void 실드와_회복은_준_사도_몫이고_넘친_회복은_뺀다()
        {
            var b = Fight(K.Data(cards: CARDS));
            int sh0 = b.Pool.Shield;
            K.Hand(b, "wall_b");
            K.Play(b, "wall_b");
            Assert.Greater(b.Pool.Shield - sh0, 0);
            Assert.AreEqual(b.Pool.Shield - sh0, T(b, "b").Shield);

            b.Pool.Hp = b.Pool.MaxHp - 30;   // 회복량(방어 40 × 3 = 120 남짓)보다 덜 비었다 — 30 만 센다
            K.Hand(b, "mend_b");
            K.Play(b, "mend_b");
            Assert.AreEqual(b.Pool.MaxHp, b.Pool.Hp);
            Assert.AreEqual(30, T(b, "b").Heal);
            Assert.AreEqual(0, T(b, "a").Heal + T(b, "c").Heal);
        }

        [Test] public void 지속_피해는_상태를_건_사도_몫()
        {
            var b = Fight(K.Data(cards: CARDS));
            K.Hand(b, "rift_c");
            K.Play(b, "rift_c");
            Assert.AreEqual(0, T(b, "c").Dealt);
            int hp0 = K.Hp(b);
            b.EndTurn();
            Assert.Greater(hp0 - K.Hp(b), 0, "균열이 돌았다");
            Assert.AreEqual(hp0 - K.Hp(b), T(b, "c").Dealt);
        }

        [Test] public void 저장과_복사에_기록이_따라가고_복사본은_원본을_건드리지_않는다()
        {
            var d = K.Data(cards: CARDS);
            var b = Fight(d);
            K.Hand(b, "hit_c0", "wall_b");
            K.Play(b, "hit_c0");
            K.Play(b, "wall_b");
            var re = Battle.Load(d, b.Save());
            Assert.AreEqual(T(b, "c").Dealt, T(re, "c").Dealt);
            Assert.AreEqual(T(b, "b").Shield, T(re, "b").Shield);

            int dealt = T(b, "c").Dealt;
            var cl = b.Clone();
            K.Hand(cl, "hit_c0");
            K.Play(cl, "hit_c0");
            Assert.Greater(T(cl, "c").Dealt, dealt);
            Assert.AreEqual(dealt, T(b, "c").Dealt, "복사본이 친 몫은 원본에 안 든다");
            Assert.IsTrue(b.Tally.Keys.All(k => k == "b" || k == "c"));
        }
    }
}
