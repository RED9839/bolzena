using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 격파된 적은 행동하지 않는다(사용자 점검 2026-10-08).
    /// ① 적 차례 도중(우리 반응으로) 격파되면 남은 타격 · 그 차례의 남은 수(행동 패시브)를 멈추고, 다음 적 차례를 쉰 뒤 그다음 내 턴 시작에 강인도가 다 찬다.
    /// ② 힘 모으는 중 격파되면 모은 수가 나가지 않는다. ③ 내 턴에 격파된 적은 턴 시작 · 턴 끝 · 행동하면 패시브를 하지 않는다(맞으면 · 카드 · 동료 격파 같은 반응은 치는 수만 막는다).
    /// ④ 격파하며 처치 · 격파 반응이 두 번 돌지 않는다. ⑤ 즉시 행동 직전에 카드로 격파하면 행동하지 않는다.
    /// </summary>
    public class BreakActTests
    {
        const string HEROES = @"[
 {id:'g', name:'가시이', role:'탱커', row:'front', hp:5000, atk:100, def:0, crit:0, nature:'냉정',
  passives:[{name:'되받아 깨기', when:{on:'hurt'}, limit:{per:'turn', n:1}, fx:[{k:'tough', v:3, target:'oneEnemy'}]}]},
 {id:'c', name:'치기', role:'딜러', row:'back', hp:5000, atk:100, def:0, crit:0, nature:'순수'}]";
        const string CARDS = @"[
 {id:'h_brk', name:'깨기', hero:'c', cost:1, type:'스킬', fx:[{k:'tough', v:9, target:'oneEnemy'}]},
 {id:'h_hit', name:'치기', hero:'c', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_draw', name:'뽑을 것', hero:'c', cost:0, type:'스킬', fx:[]}]";
        const string FOES = @"[
 {id:'flurry', name:'난타', hp:50000, tough:3, intents:[{t:'multi', v:50, n:4, rush:0}],
  passives:[{name:'기세', on:'act', do:{t:'buff', id:'사기', v:1}}]},
 {id:'charger', name:'모으는 적', hp:50000, tough:3, intents:[{t:'charge', say:'모으기', rush:0, next:{t:'attack', v:500, say:'내리찍기', rush:0}}]},
 {id:'tender', name:'다짐하는 적', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'다짐', on:'turnEnd', do:{t:'buff', id:'사기', v:1}}]},
 {id:'rusher', name:'급한 적', hp:50000, tough:3, intents:[{t:'attack', v:300}]},
 {id:'proud', name:'자존심', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'분함', on:'broken', do:{t:'buff', id:'사기', v:1}}]},
 {id:'friend', name:'동료', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'놀람', on:'allyBroken', do:{t:'buff', id:'사기', v:1}}]}]";

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(string[] party, string[] foes, List<Cue> cues = null) =>
            K.Fight(D(), party, foes, st => st.Deck = Enumerable.Repeat("h_draw", 20).ToList(), cues);
        static int PartyHits(List<Cue> cues) => cues.Count(c => c.K == "hurt" && c.Side == Side.Party);

        [Test] public void 적_차례에_다단_도중_격파되면_남은_타격과_행동_패시브를_멈춘다()
        {
            var cues = new List<Cue>();
            var b = F(new[] { "g" }, new[] { "flurry" }, cues);
            cues.Clear();
            b.EndTurn();
            Assert.AreEqual(1, PartyHits(cues), "첫 타격에 되받아 깨기로 격파 — 남은 세 타격은 없다");
            Assert.IsTrue(K.E(b).Broken);
            Assert.AreEqual(0, b.St(K.E(b), "사기"), "격파된 적의 「행동하면」 패시브는 돌지 않는다");
        }

        [Test] public void 적_차례에_격파되면_다음_적_차례를_쉬고_그다음_내_턴에_다_찬다()
        {
            var cues = new List<Cue>();
            var b = F(new[] { "g" }, new[] { "flurry" }, cues);
            b.EndTurn();
            Assert.IsTrue(K.E(b).Broken, "내 턴 동안은 격파 상태 그대로");
            Assert.AreEqual(0, K.E(b).Tough, 1e-9);
            cues.Clear();
            b.EndTurn();
            Assert.AreEqual(0, PartyHits(cues), "다음 적 차례는 쉰다");
            Assert.IsFalse(K.E(b).Broken, "그다음 내 턴 시작에 일어선다");
            Assert.AreEqual(3, K.E(b).Tough, 1e-9);
        }

        [Test] public void 힘_모으는_중_격파되면_모은_수가_나가지_않는다()
        {
            var b = F(new[] { "c" }, new[] { "charger" }); K.Hand(b, "h_brk");
            Assert.AreEqual("charge", K.E(b).Intent.T);
            K.Play(b, "h_brk");
            Assert.IsTrue(K.E(b).Broken);
            int hp = b.Pool.Hp;
            b.EndTurn();
            Assert.AreEqual(hp, b.Pool.Hp, "쉬는 차례 — 모은 수 없음");
            b.EndTurn();
            Assert.AreEqual(hp, b.Pool.Hp, "다시 모으기부터 — 끊긴 수가 그대로 나가지 않는다");
        }

        [Test] public void 내_턴에_격파된_적은_턴_끝_패시브를_하지_않는다()
        {
            var b = F(new[] { "c" }, new[] { "tender" }); K.Hand(b, "h_brk");
            K.Play(b, "h_brk");
            b.EndTurn();
            Assert.AreEqual(0, b.St(K.E(b), "사기"), "턴 끝 패시브도 하지 않는다");
            Assert.IsFalse(K.E(b).Broken, "다음 내 턴에 일어선다");
            b.EndTurn();
            Assert.AreEqual(1, b.St(K.E(b), "사기"), "일어선 뒤에는 다시 한다");
        }

        [Test] public void 즉시_행동_한_장_전에_카드로_격파하면_행동하지_않는다()
        {
            var b = F(new[] { "c" }, new[] { "rusher", "rusher" }); K.Hand(b, "h_brk", "h_hit");
            int need = b.RushOf(K.E(b));
            Assert.Greater(need, 0);
            K.E(b, 0).RushCnt = need - 1; K.E(b, 1).RushCnt = need - 1;
            int hp = b.Pool.Hp;
            K.Play(b, "h_brk", 0);
            Assert.IsTrue(K.E(b, 0).Broken);
            Assert.IsFalse(K.E(b, 0).RushedTurn, "격파된 적은 즉시 행동하지 않는다");
            Assert.IsTrue(K.E(b, 1).RushedTurn, "옆 적은 그대로 즉시 행동");
            Assert.Less(b.Pool.Hp, hp);
            int hp2 = b.Pool.Hp;
            // 처치 일격이 곧 격파일 때도 같다
            var b2 = F(new[] { "c" }, new[] { "rusher" }); K.Hand(b2, "h_hit");
            K.E(b2).RushCnt = need - 1; K.E(b2).Hp = 1; K.E(b2).Tough = 0.1;
            K.Play(b2, "h_hit");
            Assert.IsTrue(K.E(b2).Dead && K.E(b2).Broken);
            Assert.AreEqual(b2.Pool.MaxHp, b2.Pool.Hp, "죽은 적은 행동하지 않는다");
        }

        [Test] public void 격파_중이면_반응_패시브가_없고_일어서면_다시_돈다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS, enemies: FOES.TrimEnd(']') + @",
 {id:'touchy', name:'예민', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'움찔', on:'hurt', limit:9, do:{t:'block', v:10}}, {name:'눈치', on:'card', limit:9, do:{t:'buff', id:'사기', v:1}},
   {name:'놀람', on:'allyBroken', do:{t:'buff', id:'불굴', v:1}}]}]");
            var b = K.Fight(d, new[] { "c" }, new[] { "touchy", "proud" }, st => st.Deck = Enumerable.Repeat("h_draw", 20).ToList());
            K.Hand(b, "h_brk", "h_hit", "h_draw");
            K.Play(b, "h_brk", 0);
            int sagi = b.St(K.E(b), "사기");
            K.Play(b, "h_hit", 0);
            Assert.AreEqual(0, K.E(b).Block, "맞으면 — 없음");
            K.Play(b, "h_draw");
            Assert.AreEqual(sagi, b.St(K.E(b), "사기"), "카드를 내면 — 없음");
            K.Hand(b, "h_brk"); b.Ap = 3;
            K.Play(b, "h_brk", 1);
            Assert.AreEqual(0, b.St(K.E(b), "불굴"), "동료가 격파되면 — 없음");
            b.EndTurn();
            Assert.IsFalse(K.E(b).Broken);
            K.Hand(b, "h_hit");
            K.Play(b, "h_hit", 0);
            Assert.AreEqual(10, K.E(b).Block, "일어선 뒤에는 다시 돈다");
        }

        [Test] public void 격파_반응은_한_번_격파하며_처치는_두_번_돌지_않는다()
        {
            var b = F(new[] { "c" }, new[] { "proud", "friend" }); K.Hand(b, "h_brk");
            K.Play(b, "h_brk", 0);
            Assert.AreEqual(1, b.St(K.E(b, 0), "사기"), "제 격파 반응은 한 번");
            Assert.AreEqual(1, b.St(K.E(b, 1), "사기"), "동료 격파 계기 한 번");

            var b3 = F(new[] { "c" }, new[] { "proud", "friend" }); K.Hand(b3, "h_hit");
            K.E(b3, 0).Hp = 1; K.E(b3, 0).Tough = 0.1;
            K.Play(b3, "h_hit", 0);
            Assert.IsTrue(K.E(b3, 0).Dead && K.E(b3, 0).Broken);
            Assert.LessOrEqual(b3.St(K.E(b3, 1), "사기"), 1, "격파하며 처치 — 반응이 두 번 돌지 않는다");
            Assert.AreEqual(b3.Pool.MaxHp, b3.Pool.Hp, "죽은 적은 행동하지 않는다");
        }
    }
}
