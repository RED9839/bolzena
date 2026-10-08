using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 처치 일격의 강인도 · 격파하며 처치(사용자 2026-10-07) — 피해 → 강인도 → 격파 판정 → 죽음.
    /// 처치 일격도 그 카드의 강인도 피해를 넣고, 그때 강인도가 0 이 되면 격파와 같은 AP +1(적마다). 근면과는 따로 받는다.
    /// 격파 · 기절로 쉬는 적은 쌓이는 수치의 「즉시」 수 · 치는 패시브로도 움직이지 못한다(배포판 버그 — 격파된 불효자손이 심통으로 내리찍었다).
    /// </summary>
    public class BreakKillTests
    {
        const string HEROES = @"[
 {id:'cool', name:'냉정이', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, nature:'냉정'},
 {id:'pure', name:'순수이', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, nature:'순수'}]";
        const string CARDS = @"[
 {id:'h_cool', name:'냉정 치기', hero:'cool', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_coolA', name:'냉정 쓸기', hero:'cool', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'allEnemies'}]},
 {id:'h_pure', name:'순수 치기', hero:'pure', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_brk', name:'깨기', hero:'cool', cost:1, type:'공격', fx:[{k:'tough', v:6, target:'oneEnemy'}]},
 {id:'h_skill', name:'숨 고르기', hero:'cool', cost:0, type:'스킬', fx:[]},
 {id:'h_draw', name:'뽑을 것', hero:'cool', cost:0, type:'스킬', fx:[]}]";
        const string FOES = @"[
 {id:'mad', name:'광기 적', hp:5000, nature:'광기', tough:3, intents:[{t:'jam', v:0, rush:0}]},
 {id:'grump', name:'심통 괭이', hp:5000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  counters:[{name:'심통', onCard:1, cardType:'!공격', max:9, at:2, act:{t:'attack', v:300, say:'쾅', rush:0}, mode:'now'}]},
 {id:'avenger', name:'원수 갚는 적', hp:5000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'앙갚음', on:'card', do:{t:'attack', v:300, say:'앙갚음'}}]}]";

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(string[] foes, List<Cue> cues = null) =>
            K.Fight(D(), new[] { "cool", "pure" }, foes, st => st.Deck = Enumerable.Repeat("h_draw", 12).ToList(), cues);

        [Test] public void 처치_일격도_강인도를_깎고_쪽지는_강인도_격파_죽음_차례()
        {
            var cues = new List<Cue>();
            var b = F(new[] { "mad", "mad" }, cues); K.Hand(b, "h_cool");
            K.E(b).Tough = 1; K.E(b).Hp = 1;
            K.Play(b, "h_cool");
            Assert.IsTrue(K.E(b).Dead);
            Assert.AreEqual(0, K.E(b).Tough, 1e-9, "약점 1코 = 강인도 1 — 처치 일격도 깎는다");
            Assert.IsTrue(K.E(b).Broken, "그 일격으로 0 이 되면 격파");
            Assert.AreEqual(1, b.Breaks);
            int tough = cues.FindIndex(c => c.K == "tough" && c.Side == Side.Enemy && c.Idx == 0);
            int brk = cues.FindIndex(c => c.K == "break" && c.Side == Side.Enemy && c.Idx == 0);
            int die = cues.FindIndex(c => c.K == "die" && c.Side == Side.Enemy && c.Idx == 0);
            Assert.GreaterOrEqual(tough, 0, "강인도 쪽지"); Assert.Greater(brk, tough, "격파는 강인도 뒤"); Assert.Greater(die, brk, "죽음은 격파 뒤");
        }

        [Test] public void 격파하며_처치하면_AP_1()
        {
            var b = F(new[] { "mad", "mad" }); K.Hand(b, "h_cool");
            K.E(b).Tough = 1; K.E(b).Hp = 1;
            K.Play(b, "h_cool");
            Assert.AreEqual(3 - 1 + R.TOUGH.Ap, b.Ap, "AP 1 을 쓰고 격파 처치로 1");
            Assert.IsFalse(K.E(b, 1).Broken);
        }

        [Test] public void 격파_없는_처치는_AP_없음_강인도는_깎인다()
        {
            var b = F(new[] { "mad", "mad" }); K.Hand(b, "h_pure");
            K.E(b).Hp = 1;
            K.Play(b, "h_pure");
            Assert.IsTrue(K.E(b).Dead);
            Assert.AreEqual(3 - 1.0 / 3, K.E(b).Tough, 1e-9, "비약점 1/3 — 처치 일격에도 깎인다");
            Assert.IsFalse(K.E(b).Broken);
            Assert.AreEqual(2, b.Ap, "AP 1 만 썼다");
            Assert.AreEqual(0, b.Breaks);
        }

        [Test] public void 광역으로_둘을_격파하며_처치하면_적마다_AP_1()
        {
            var b = F(new[] { "mad", "mad", "mad" }); K.Hand(b, "h_coolA");
            for (int i = 0; i < 2; i++) { K.E(b, i).Tough = 0.5; K.E(b, i).Hp = 1; }
            K.Play(b, "h_coolA");
            Assert.IsTrue(K.E(b, 0).Dead && K.E(b, 1).Dead);
            Assert.AreEqual(3 - 1 + 2 * R.TOUGH.Ap, b.Ap, "보통 격파처럼 적마다");
            Assert.AreEqual(2, K.E(b, 2).Tough, 1e-9, "살아 있는 적은 그대로 1칸(광역도 적마다 단일과 같다)");
        }

        [Test] public void 근면과_겹치면_둘_다_받는다()
        {
            var b = F(new[] { "mad", "mad" }); K.Hand(b, "h_cool");
            b.Pool.Status["근면"] = 1;
            K.E(b).Tough = 1; K.E(b).Hp = 1;
            K.Play(b, "h_cool");
            Assert.AreEqual(3 - 1 + R.TOUGH.Ap + 1, b.Ap, "격파 처치 AP 1 + 근면 AP 1");
            Assert.AreEqual(1, b.Hand.Count, "근면 드로우 1");
        }

        [Test] public void 미리보기도_처치와_격파를_함께_보인다()
        {
            var b = F(new[] { "mad", "mad" }); K.Hand(b, "h_cool");
            K.E(b).Tough = 1; K.E(b).Hp = 1;
            var p = b.PreviewCard(0, 0);
            Assert.IsTrue(p[0].Kill); Assert.IsTrue(p[0].Brk); Assert.AreEqual(1, p[0].Tough, 1e-9);
        }

        [Test] public void 격파된_적은_쌓이는_수치의_즉시_수를_다음_차례로_미루고_이번_차례는_쉰다()
        {
            var b = F(new[] { "grump" }); K.Hand(b, "h_brk", "h_skill", "h_skill");
            int hp = b.Pool.Hp;
            K.Play(b, "h_brk");
            Assert.IsTrue(K.E(b).Broken); Assert.IsTrue(K.E(b).Sealed);
            K.Play(b, "h_skill"); K.Play(b, "h_skill");
            Assert.AreEqual(hp, b.Pool.Hp, "격파 중엔 심통 2 가 차도 지금 내리찍지 못한다");
            b.EndTurn();
            Assert.AreEqual(hp, b.Pool.Hp, "격파된 적은 그 차례를 쉰다");
            Assert.AreEqual("쾅", K.E(b).Intent?.Say, "미룬 수가 다음 예고로");
            b.EndTurn();
            Assert.Less(b.Pool.Hp, hp, "그다음 차례에 친다");
        }

        [Test] public void 격파된_적은_카드에_치는_패시브도_못_한다()
        {
            var b = F(new[] { "avenger" }); K.Hand(b, "h_brk", "h_skill");
            K.Play(b, "h_brk");
            int hp = b.Pool.Hp;
            K.Play(b, "h_skill");
            Assert.AreEqual(hp, b.Pool.Hp, "격파 중엔 앙갚음이 돌지 않는다");
        }
    }
}
