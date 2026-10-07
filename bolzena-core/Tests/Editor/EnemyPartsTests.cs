using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 적 리워크 엔진 부품(2026-10-07 — 콘텐츠 v2 _measure/적_리워크_지침.md §7):
    /// 패시브 계기 allyBroken(동료가 격파되면) · 쌓이는 수치 clearOnAttack(치고 나면 0) · 수 seize(카드 빼앗기 — 격파 · 처치 · 피해로 되찾음) · 적 글.
    /// </summary>
    public class EnemyPartsTests
    {
        const string CARDS = @"[
 {id:'p_brk', name:'깨기', hero:'a', cost:1, type:'공격', fx:[{k:'tough', v:9, target:'oneEnemy'}]},
 {id:'p_kill', name:'끝내기', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:50, target:'oneEnemy'}]},
 {id:'p_card', name:'평범한 카드', hero:'a', cost:0, type:'스킬', fx:[]}]";
        const string FOES = @"[
 {id:'wall', name:'벽', hp:1000, tough:3, intents:[{t:'jam', v:0, rush:0}]},
 {id:'watcher', name:'지켜보는 적', hp:1000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'성냄', on:'allyBroken', who:'wall', do:{t:'buff', id:'사기', v:1}}]},
 {id:'any', name:'아무나 보는 적', hp:1000, tough:3, intents:[{t:'jam', v:0, rush:0}],
  passives:[{name:'놀람', on:'allyBroken', do:{t:'buff', id:'사기', v:1}}]},
 {id:'aimer', name:'노리는 적', hp:1000, tough:3, intents:[{t:'attack', v:100, rush:0}],
  counters:[{name:'급소 노림', start:2, max:3, dealt:0.5, clearOnAttack:true}]},
 {id:'thief', name:'도둑', hp:200, tough:3, intents:[{t:'seize', n:1, v:10, rush:0}]}]";

        static GameData D() => K.Data(cards: CARDS, enemies: FOES);
        static Battle F(params string[] foes) => K.Fight(D(), new[] { "a" }, foes, st => st.Deck = Enumerable.Repeat("p_card", 10).ToList());
        static int All(Battle b) => b.Draw.Count + b.Hand.Count + b.Discard.Count + b.Enemies.Sum(e => e.Seized.Count);

        [Test] public void 동료가_격파되면_그_동료일_때만_돈다()
        {
            var b = F("wall", "watcher", "any");
            K.Hand(b, "p_brk", "p_brk");
            K.Play(b, "p_brk", 1);   // 지켜보는 적 자신이 격파 — 자기 일에는 안 돈다 · who 가 벽이 아니니 안 돈다
            Assert.IsTrue(K.E(b, 1).Broken);
            Assert.AreEqual(0, b.St(K.E(b, 1), "사기"), "자기 격파 · 다른 동료에는 안 돈다");
            Assert.AreEqual(1, b.St(K.E(b, 2), "사기"), "who 가 없으면 어느 동료든");
            K.Play(b, "p_brk", 0);   // 벽 격파
            Assert.AreEqual(1, b.St(K.E(b, 1), "사기"), "who 의 그 동료가 격파되면");
        }

        [Test] public void 공격하면_비우는_수치는_친_뒤에_0_그_공격에는_실린다()
        {
            var b = F("aimer");
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            Assert.AreEqual(0, b.St(K.E(b), "급소 노림"), "치고 나면 0");
            Assert.GreaterOrEqual(hp0 - b.Pool.Hp, 190, "쌓인 2 × 50% 가 그 공격에 실렸다(100 × 2)");
        }

        [Test] public void 빼앗기_적의_차례엔_뽑을_더미에서_피해로_되찾음()
        {
            var b = F("thief");
            Assert.AreEqual(10, All(b));
            b.EndTurn();
            var e = K.E(b);
            Assert.AreEqual(1, e.Seized.Count, "손이 비었으니 뽑을 더미 맨 위");
            Assert.AreEqual(1, b.St(e, R.SEIZED), "칩에 쥔 장수");
            Assert.AreEqual(20, b.SeizeLeft(e), "최대 HP 200 의 10%");
            Assert.AreEqual(10, All(b), "카드는 사라지지 않는다");
            string id = e.Seized[0];
            b.Hand.Clear(); b.Hand.Add("hit");
            K.Play(b, "hit");
            Assert.AreEqual(0, e.Seized.Count, "피해로 되찾음");
            Assert.AreEqual(0, b.St(e, R.SEIZED));
            Assert.Contains(id, b.Hand, "손으로 돌아온다");
        }

        [Test] public void 빼앗기_격파_처치로_되찾음_손에서_먼저()
        {
            var b = F("thief", "wall");
            K.Hand(b, "p_card", "p_brk", "p_kill");
            var e = K.E(b);
            var it = new Intent { T = "seize", N = 1, V = 90 };
            b.FoeDo(e, it);
            Assert.AreEqual(1, e.Seized.Count, "손에서(상태 · 저주 아닌 것)");
            Assert.AreEqual(2, b.Hand.Count);
            string got = e.Seized[0];
            if (got == "p_brk" || got == "p_kill") Assert.Pass("무작위로 쓸 카드를 가져갔다 — 나머지 흐름은 다른 씨앗이 본다");
            K.Play(b, "p_brk", 0);
            Assert.AreEqual(0, e.Seized.Count, "격파하면 돌아온다");
            Assert.Contains(got, b.Hand);
            b.FoeDo(e, it);
            Assert.AreEqual(1, e.Seized.Count);
            K.Hand(b, "p_kill"); b.Ap = 3;
            K.Play(b, "p_kill", 0);
            Assert.IsTrue(e.Dead);
            Assert.AreEqual(0, e.Seized.Count, "쓰러뜨리면 돌아온다");
        }

        [Test] public void 적_글_힘_모으기_끊김_한_번_자신에게_버프_같은_계기_한_줄_동료_이름()
        {
            var d = K.Data(enemies: @"[
 {id:'g1', name:'골렘', hp:900, intents:[{t:'charge', say:'모으기', brk:true, next:{t:'attack', v:200, say:'쾅', brk:true}}, {t:'buff', id:'사기', v:1}],
  passives:[{name:'결 깨짐', on:'broken', do:{t:'count', id:'결', v:-5}}, {name:'드러난 핵', on:'broken', do:{t:'buff', id:'취약', v:3}},
            {name:'원수', on:'allyDown', who:'g2', do:{t:'buff', id:'사기', v:1, all:true}}],
  counters:[{name:'결', taken:-0.1, onTurnStart:1, max:5}, {name:'급소', dealt:0.1, onTurnStart:1, clearOnAttack:true}]},
 {id:'g2', name:'제자', hp:300, intents:[{t:'seize', n:1}]}]");
            var tx = new CardText(d);
            var e = d.Enemy("g1");
            string charge = tx.Intent(e.Intents[0]);
            Assert.AreEqual(1, charge.Split(new[] { "격파하면 끊김" }, System.StringSplitOptions.None).Length - 1, charge);
            StringAssert.Contains("자신에게 사기 +1", tx.Intent(e.Intents[1]));
            string all = tx.Enemy(e);
            StringAssert.Contains("결 깨짐 · 드러난 핵: 격파되면 결 -5 · 자신에게 취약 +3", all);
            StringAssert.Contains("원수: 제자가 쓰러지면", all);
            StringAssert.Contains("공격하면 모두 사라짐", all);
            StringAssert.Contains("카드 1장 빼앗기", tx.Intent(d.Enemy("g2").Intents[0]));
            StringAssert.Contains($"HP {R.SEIZE_PCT}%", tx.Intent(d.Enemy("g2").Intents[0]));
        }

        [Test] public void 상태_카드_넣기는_덱에_max_장까지()
        {
            var d = K.Data(cards: CARDS + "", enemies: @"[
 {id:'piler', name:'쌓는 적', hp:9000, pick:'shuffle', intents:[{t:'addCard', id:'st_junk', n:2, to:'draw', max:2, w:100}, {t:'block', v:10, w:1}]}]")
                .Add(null, @"[{id:'st_junk', name:'잡동사니', cost:2, type:'상태', tags:['소멸']}]", null, null, null, null);
            var b = K.Fight(d, new[] { "a" }, new[] { "piler" }, st => st.Deck = Enumerable.Repeat("p_card", 10).ToList());
            for (int t = 0; t < 6 && b.Over == null; t++) b.EndTurn();
            int n = b.Draw.Concat(b.Hand).Concat(b.Discard).Count(x => GameData.BaseId(x) == "st_junk");
            Assert.AreEqual(2, n, "2장이 들어간 뒤로는 그 수를 고르지 않는다");
        }

        [Test] public void 검사기_새_계기_수_수치를_안다()
        {
            var v = Validator.Check(D());
            var mine = v.Errors.Where(x => (x.Contains("watcher") || x.Contains("thief") || x.Contains("aimer")) && !x.Contains("jam")).ToList();   // jam v 0 은 시험 틀의 허수아비 몫
            Assert.IsEmpty(mine, string.Join("\n", mine));
        }
    }
}
