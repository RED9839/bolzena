using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 격파된 적도 제 차례에 그대로 행동한다(사용자 2026-10-24 — 10-08 「격파된 적은 행동 · 반응을 모두 멈춘다」 를 뒤집음, 카제나처럼).
    /// 격파가 하는 일은 AP +1 · 「격파하면 끊김」(brk) 수 끊기 · 「격파되면」 규칙뿐. 강인도가 차는 때는 그대로 —
    /// 내 턴에 격파되면 다음 내 턴 시작, 적 차례에 제 자리를 지난 뒤 격파되면 다음 적 차례를 지난 뒤의 내 턴 시작.
    /// ① 다단 도중 격파돼도 남은 타격 · 「행동하면」 패시브가 돈다. ② 적 차례에 격파된 적은 다음 적 차례에도 행동하고 그다음 내 턴에 다 찬다.
    /// ③ 모으는 수는 brk 일 때만 끊긴다. ④ 턴 끝 · 맞으면 · 카드 · 동료 격파 반응이 그대로 돈다. ⑤ 즉시 행동도 그대로. ⑥ 격파 반응은 한 번.
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
 {id:'chargerB', name:'끊기는 적', hp:50000, tough:3, intents:[{t:'charge', say:'모으기', brk:true, rush:0, next:{t:'attack', v:500, say:'내리찍기', rush:0}}]},
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

        [Test] public void 적_차례에_다단_도중_격파돼도_남은_타격과_행동_패시브가_돈다()
        {
            var cues = new List<Cue>();
            var b = F(new[] { "g" }, new[] { "flurry" }, cues);
            cues.Clear();
            b.EndTurn();
            Assert.IsTrue(K.E(b).Broken, "첫 타격에 되받아 깨기로 격파");
            Assert.AreEqual(4, PartyHits(cues), "격파돼도 남은 세 타격을 다 친다");
            Assert.AreEqual(1, b.St(K.E(b), "사기"), "격파된 적의 「행동하면」 패시브도 돈다");
        }

        [Test] public void 적_차례에_격파된_적은_다음_적_차례에도_행동하고_그다음_내_턴에_다_찬다()
        {
            var cues = new List<Cue>();
            var b = F(new[] { "g" }, new[] { "flurry" }, cues);
            b.EndTurn();
            Assert.IsTrue(K.E(b).Broken, "내 턴 동안은 격파 상태 그대로");
            Assert.AreEqual(0, K.E(b).Tough, 1e-9);
            cues.Clear();
            b.EndTurn();
            Assert.AreEqual(4, PartyHits(cues), "다음 적 차례에도 그대로 친다");
            Assert.IsFalse(K.E(b).Broken, "그다음 내 턴 시작에 일어선다");
            Assert.AreEqual(3, K.E(b).Tough, 1e-9);
        }

        [Test] public void 모으는_중_격파돼도_끊김_수가_아니면_모은_수가_나가고_끊김_수는_끊긴다()
        {
            var b = F(new[] { "c" }, new[] { "charger" }); K.Hand(b, "h_brk");
            Assert.AreEqual("charge", K.E(b).Intent.T);
            K.Play(b, "h_brk");
            Assert.IsTrue(K.E(b).Broken);
            Assert.AreEqual("charge", K.E(b).Intent?.T, "끊김 수가 아니면 그대로");
            int hp = b.Pool.Hp;
            b.EndTurn(); b.EndTurn();
            Assert.Less(b.Pool.Hp, hp, "모은 수가 나간다");

            var c = F(new[] { "c" }, new[] { "chargerB" }); K.Hand(c, "h_brk");
            K.Play(c, "h_brk");
            Assert.IsNull(K.E(c).Intent, "격파하면 끊김 — 모으던 수가 사라진다");
            int hp2 = c.Pool.Hp;
            c.EndTurn(); c.EndTurn();
            Assert.AreEqual(hp2, c.Pool.Hp, "다시 모으기부터 — 끊긴 수가 나가지 않는다");
        }

        [Test] public void 내_턴에_격파된_적도_턴_끝_패시브를_하고_다음_내_턴에_일어선다()
        {
            var b = F(new[] { "c" }, new[] { "tender" }); K.Hand(b, "h_brk");
            K.Play(b, "h_brk");
            b.EndTurn();
            Assert.AreEqual(1, b.St(K.E(b), "사기"), "턴 끝 패시브를 그대로 한다");
            Assert.IsFalse(K.E(b).Broken, "다음 내 턴에 일어선다");
        }

        [Test] public void 즉시_행동_한_장_전에_카드로_격파해도_즉시_행동한다()
        {
            var b = F(new[] { "c" }, new[] { "rusher", "rusher" }); K.Hand(b, "h_brk", "h_hit");
            int need = b.RushOf(K.E(b));
            Assert.Greater(need, 0);
            K.E(b, 0).RushCnt = need - 1; K.E(b, 1).RushCnt = need - 1;
            int hp = b.Pool.Hp;
            K.Play(b, "h_brk", 0);
            Assert.IsTrue(K.E(b, 0).Broken);
            Assert.IsTrue(K.E(b, 0).RushedTurn, "격파된 적도 즉시 행동한다");
            Assert.IsTrue(K.E(b, 1).RushedTurn, "옆 적도 그대로 즉시 행동");
            Assert.Less(b.Pool.Hp, hp);
            // 처치 일격이 곧 격파면 — 죽은 적은 행동하지 않는다
            var b2 = F(new[] { "c" }, new[] { "rusher" }); K.Hand(b2, "h_hit");
            K.E(b2).RushCnt = need - 1; K.E(b2).Hp = 1; K.E(b2).Tough = 0.1;
            K.Play(b2, "h_hit");
            Assert.IsTrue(K.E(b2).Dead && K.E(b2).Broken);
            Assert.AreEqual(b2.Pool.MaxHp, b2.Pool.Hp, "죽은 적은 행동하지 않는다");
        }

        [Test] public void 격파_중에도_반응_패시브가_돈다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS, enemies: FOES.TrimEnd(']') + @",
 {id:'touchy', name:'예민', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'움찔', on:'hurt', limit:9, do:{t:'block', v:10}}, {name:'눈치', on:'card', limit:9, do:{t:'buff', id:'사기', v:1}},
   {name:'놀람', on:'allyBroken', do:{t:'buff', id:'불굴', v:1}}]}]");
            var b = K.Fight(d, new[] { "c" }, new[] { "touchy", "proud" }, st => st.Deck = Enumerable.Repeat("h_draw", 20).ToList());
            K.Hand(b, "h_brk", "h_hit", "h_draw");
            K.Play(b, "h_brk", 0);
            Assert.IsTrue(K.E(b).Broken);
            int sagi = b.St(K.E(b), "사기");
            K.Play(b, "h_hit", 0);
            Assert.AreEqual(10, K.E(b).Block, "맞으면 — 돈다");
            K.Play(b, "h_draw");
            Assert.Greater(b.St(K.E(b), "사기"), sagi, "카드를 내면 — 돈다");
            K.Hand(b, "h_brk"); b.Ap = 3;
            K.Play(b, "h_brk", 1);
            Assert.AreEqual(1, b.St(K.E(b), "불굴"), "동료가 격파되면 — 돈다");
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
