using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// M.E.O.W 10-24 개정 — ③ 격파로 「출력」 다스리기(쌓이는 수치 onBreak · breakCut · breakHold) · ② 방어 점수(Frontier.Ward — 막은 보스 피해 × R.FRONTIER_WARD% + ward 수를 다 막은 몫 × R.FRONTIER_WARD_FULL%).
    /// </summary>
    public class FrontierScoreTests
    {
        const string HEROES = @"[
 {id:'cool', name:'냉정이', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, nature:'냉정'}]";
        const string CARDS = @"[
 {id:'h_cool', name:'냉정 치기', hero:'cool', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_brk', name:'깨기', hero:'cool', cost:1, type:'공격', fx:[{k:'tough', v:6, target:'oneEnemy'}]},
 {id:'h_draw', name:'뽑을 것', hero:'cool', cost:0, type:'스킬', fx:[]}]";
        const string FOES = @"[
 {id:'meow', name:'기계', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  counters:[{name:'출력', onHit:1, dealt:0.05, max:99, onBreak:-10, breakHold:true}]},
 {id:'meowhalf', name:'기계 반', hp:50000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  counters:[{name:'출력', onHit:1, dealt:0.05, max:99, breakCut:0.5}]},
 {id:'presser', name:'누르개', hp:50000, tough:3, intents:[{t:'attack', v:100, ward:true, rush:0}]},
 {id:'puncher', name:'주먹', hp:50000, tough:3, intents:[{t:'attack', v:100, rush:0}]}]";

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(params string[] foes) => K.Fight(D(), new[] { "cool" }, foes, st => st.Deck = Enumerable.Repeat("h_draw", 12).ToList());

        [Test] public void 격파하면_출력이_줄고_일어설_때까지_오르지_않는다()
        {
            var b = F("meow"); var e = K.E(b);
            K.Hand(b, "h_cool"); K.Play(b, "h_cool");
            Assert.AreEqual(1, b.St(e, "출력"), "맞으면 +1");
            e.Status["출력"] = 15;
            K.Hand(b, "h_brk"); K.Play(b, "h_brk");
            Assert.IsTrue(e.Broken);
            Assert.AreEqual(5, b.St(e, "출력"), "격파 -10");
            K.Hand(b, "h_cool"); K.Play(b, "h_cool");
            Assert.AreEqual(5, b.St(e, "출력"), "격파된 동안 맞아도 오르지 않는다");
            b.EndTurn();
            Assert.IsFalse(e.Broken, "다음 내 턴에 일어선다");
            K.Hand(b, "h_cool"); K.Play(b, "h_cool");
            Assert.AreEqual(6, b.St(e, "출력"), "일어선 뒤엔 다시 오른다");
        }

        [Test] public void breakCut_은_지금_겹의_비율을_올림으로_덜고_0_이면_그대로()
        {
            var b = F("meowhalf"); var e = K.E(b);
            K.Hand(b, "h_brk"); K.Play(b, "h_brk");
            Assert.IsTrue(e.Broken);
            Assert.AreEqual(0, b.St(e, "출력"), "0 에서 격파 — 그대로");
            b.EndTurn();
            e.Status["출력"] = 15;
            K.Hand(b, "h_brk"); K.Play(b, "h_brk");
            Assert.IsTrue(e.Broken);
            Assert.AreEqual(7, b.St(e, "출력"), "15 의 절반 올림 8 을 덜어 7");
            K.Hand(b, "h_cool"); K.Play(b, "h_cool");
            Assert.AreEqual(8, b.St(e, "출력"), "breakHold 가 없으면 격파 중에도 오른다");
        }

        [Test] public void 방어_점수는_막은_보스_피해의_비율이고_ward_수를_다_막으면_덤()
        {
            var b = F("presser"); var e = K.E(b);
            b.Pool.Block = 99999;
            b.EndTurn();
            Assert.Greater(e.Warded, 0, "막은 피해를 센다");
            Assert.AreEqual(e.Warded, e.WardFull, "ward 수를 HP 하나 안 잃고 다 막았다");
            Assert.AreEqual((e.Warded * R.FRONTIER_WARD + e.WardFull * R.FRONTIER_WARD_FULL) / 100, Frontier.Ward(b));
            Assert.AreEqual(Frontier.Dealt(b) + Frontier.Ward(b), Frontier.Score(b));
            Assert.AreEqual(e.Lost, Frontier.Dealt(b));
            int w0 = e.Warded, f0 = e.WardFull, hp0 = b.Pool.Hp;
            b.Pool.Block = 1;
            b.EndTurn();
            Assert.Less(b.Pool.Hp, hp0, "다 못 막았다");
            Assert.AreEqual(w0 + 1, e.Warded, "막은 1 만 더 센다");
            Assert.AreEqual(f0, e.WardFull, "다 못 막으면 덤 없음");
        }

        [Test] public void ward_가_없는_수는_다_막아도_덤이_없다()
        {
            var b = F("puncher"); var e = K.E(b);
            b.Pool.Block = 99999;
            b.EndTurn();
            Assert.Greater(e.Warded, 0);
            Assert.AreEqual(0, e.WardFull);
            Assert.AreEqual(e.Warded * R.FRONTIER_WARD / 100, Frontier.Ward(b));
        }

        [Test] public void 카운터_풀이에_격파_줄이_나온다()
        {
            var d = D(); var t = new CardText(d);
            StringAssert.Contains("격파되면 -10", t.Counter(d.Enemy("meow").Counters[0]));
            StringAssert.Contains("격파된 동안 오르지 않음", t.Counter(d.Enemy("meow").Counters[0]));
            StringAssert.Contains("격파되면 절반으로", t.Counter(d.Enemy("meowhalf").Counters[0]));
        }
    }
}
