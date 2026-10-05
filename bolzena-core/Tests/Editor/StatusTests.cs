using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>상태 — docs/18 §5 표 하나하나(값은 R.STATUS_V).</summary>
    public class StatusTests
    {
        [Test] public void 난수는_웹판과_같다()
        {
            var r = new Rng(42);
            double[] web = { 0.002643892541527748, 0.660311977379024, 0.11095708678476512, 0.8493769019842148, 0.8754393914714456 };
            foreach (var w in web) Assert.AreEqual(w, r.Next(), 1e-15);
            Assert.AreEqual(3759983556u, r.State);
            var z = new Rng(0);   // 씨앗 0 은 1 로
            Assert.AreEqual(0.00006295018829405308, z.Next(), 1e-18);
        }

        [Test] public void 피해는_공격력_곱하기_배율()
        {
            var b = K.Fight(); K.Hand(b, "hit");
            K.Play(b, "hit");
            Assert.AreEqual(900, K.Hp(b));
            Assert.AreEqual(2, b.Ap);
            Assert.AreEqual(10, b.Gauge, "AP 1 당 게이지 10%");
        }

        [Test] public void 취약은_카드_한_장에_한_번_쓰고_그_카드의_모든_타격에_붙는다()
        {
            var d = K.Data(cards: "[{id:'two', name:'두 번', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.5, hits:2, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "two", "two");
            K.E(b).Status["취약"] = 2;
            K.Play(b, "two");
            Assert.AreEqual(1000 - 75 - 75, K.Hp(b));
            Assert.AreEqual(1, b.St(K.E(b), "취약"));
            K.Play(b, "two");
            Assert.AreEqual(850 - 150, K.Hp(b));
            Assert.AreEqual(0, b.St(K.E(b), "취약"));
        }

        [Test] public void 약화는_파티의_카드_피해를_25퍼센트_깎고_1_준다()
        {
            var b = K.Fight(); K.Hand(b, "hit", "hit");
            b.Pool.Status["약화"] = 1;
            K.Play(b, "hit");
            Assert.AreEqual(925, K.Hp(b));
            Assert.AreEqual(0, b.St(b.Pool, "약화"));
            K.Play(b, "hit");
            Assert.AreEqual(825, K.Hp(b));
        }

        [Test] public void 적의_약화는_치는_수를_깎는다()
        {
            var b = K.Fight("hitter");
            K.E(b).Status["약화"] = 1;
            Assert.AreEqual(75, b.IntentHit(K.E(b)));
            b.EndTurn();
            Assert.AreEqual(925, b.Pool.Hp);
            Assert.AreEqual(0, b.St(K.E(b), "약화"));
        }

        [Test] public void 불굴은_겹마다_받는_피해_20퍼센트_최대_80퍼센트()
        {
            var b = K.Fight("hitter");
            b.Pool.Status["불굴"] = 2;
            b.EndTurn();
            Assert.AreEqual(940, b.Pool.Hp);
            Assert.AreEqual(2, b.St(b.Pool, "불굴"), "세기 상태는 줄지 않는다");
            b.Pool.Status["불굴"] = 9;
            b.EndTurn();
            Assert.AreEqual(940 - 20, b.Pool.Hp);
        }

        [Test] public void 사기는_파티_층_겹마다_카드_계수_20퍼센트p_합연산_셋_모두()
        {
            var d = K.Data(cards: "[{id:'big3', name:'삼연타', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:3.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "hit", "hit_b", "big3");
            b.Pool.Status["사기"] = 2;
            K.Play(b, "hit");
            Assert.AreEqual(1000 - 140, K.Hp(b), "100% + 40%p");
            K.Play(b, "hit_b");
            Assert.AreEqual(860 - 112, K.Hp(b), "다른 사도도 — 80 × 140%");
            K.Play(b, "big3");
            Assert.AreEqual(748 - 340, K.Hp(b), "300% 카드 → 340%(곱이 아니라 합)");
            Assert.IsFalse(b.Party[0].Status.ContainsKey("사기"));
        }

        [Test] public void 카드의_사기_1은_셋_모두에게()
        {
            var d = K.Data(cards: "[{id:'cheer', name:'응원', cost:1, type:'스킬', grade:'일반', price:70, fx:[{k:'status', id:'사기', v:1}]}, {id:'cheer_a', name:'가 응원', hero:'a', cost:1, type:'스킬', fx:[{k:'status', id:'사기', v:1}]}]");
            var b = K.Fight(d, new[] { "a", "b", "c" }, new[] { "dummy" }); K.Hand(b, "cheer_a");
            K.Play(b, "cheer_a");
            Assert.IsTrue(b.Party.All(u => b.St(u, "사기") == 1));
        }

        [Test] public void 결의는_얻는_실드에_겹마다_20_손상은_반()
        {
            var b = K.Fight(); K.Hand(b, "guard", "guard", "guard");
            b.Pool.Status["결의"] = 2;
            K.Play(b, "guard");
            Assert.AreEqual(140, b.Pool.Block);
            b.Pool.Status["손상"] = 1;
            K.Play(b, "guard");
            Assert.AreEqual(140 + 70, b.Pool.Block);
            Assert.AreEqual(0, b.St(b.Pool, "손상"));
        }

        [Test] public void 실드는_턴이_바뀌면_사라진다_실드_유지는_반_실드_보존은_전부()
        {
            var b = K.Fight();
            b.Pool.Block = 100; b.Pool.Shield = 100;
            b.EndTurn();
            Assert.AreEqual(0, b.Pool.Block);
            Assert.AreEqual(0, b.Pool.Shield, "카제나 — 실드도 턴 끝에 사라진다");
            b.Pool.Block = 100; b.Pool.Shield = 60; b.Pool.Status["실드 유지"] = 1;
            b.EndTurn();
            Assert.AreEqual(50, b.Pool.Block); Assert.AreEqual(30, b.Pool.Shield);
            Assert.AreEqual(0, b.St(b.Pool, "실드 유지"));
            b.Pool.Shield = 80; b.Pool.Block = 0; b.Pool.Status["실드 보존"] = 1;
            b.EndTurn();
            Assert.AreEqual(80, b.Pool.Shield + b.Pool.Block, "실드 보존 — 그대로");
        }

        [Test] public void 결정화는_턴_끝에_방어력_20퍼센트_고정_실드()
        {
            var b = K.Fight("hitter");
            b.Pool.Status["결정화"] = 2;
            b.Pool.Status["손상"] = 1;
            b.EndTurn();
            Assert.AreEqual(1000 - 80, b.Pool.Hp, "50 × 0.4 = 20 고정 실드(손상 안 탄다)가 적의 공격 100 을 받는다");
        }

        [Test] public void 고통은_겹의_50퍼센트_고정_지속_피해_뒤에_절반()
        {
            var d = K.Data(cards: "[{id:'pain', name:'고통 주기', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'고통', v:3, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "pain");
            K.Play(b, "pain");
            Assert.AreEqual(100, K.E(b).DotU["고통"], "바탕은 건 사도의 공격력");
            K.E(b).Block = 500;
            b.EndTurn();
            Assert.AreEqual(1000 - 150, K.Hp(b), "3 × 50% × 100, 방어를 뚫는다");
            Assert.AreEqual(1, b.St(K.E(b), "고통"));
        }

        [Test] public void 균열은_겹마다_40퍼센트_지속_피해_취약을_탄다()
        {
            var d = K.Data(cards: "[{id:'crack', name:'금', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'균열', v:4, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "crack");
            K.Play(b, "crack");
            K.E(b).Status["취약"] = 1;
            b.EndTurn();
            Assert.AreEqual(1000 - 240, K.Hp(b), "4 × 40% × 100 × 1.5");
            Assert.AreEqual(2, b.St(K.E(b), "균열"));
        }

        [Test] public void 반격은_맞으면_방어_기반_150퍼센트_다_막으면_300퍼센트()
        {
            var b = K.Fight("hitter");
            b.Pool.Status["반격"] = 2;
            b.EndTurn();
            Assert.AreEqual(900, b.Pool.Hp);
            Assert.AreEqual(1000 - 203, K.Hp(b), "(50×2.1 + 100×0.3) × 1.5 = 202.5");
            Assert.AreEqual(1, b.St(b.Pool, "반격"));
            b.Pool.Block = 500;
            b.EndTurn();
            Assert.AreEqual(797 - 405, K.Hp(b), "다 막았다 — 135 × 3");
        }

        [Test] public void 표식은_공격_카드에_덤_타격과_강인도_1()
        {
            var b = K.Fight(); K.Hand(b, "hit");
            K.E(b).Status["표식"] = 2;
            K.Play(b, "hit");
            Assert.AreEqual(800, K.Hp(b));
            Assert.AreEqual(4 - 1.0 / 3 - 1.0 / 6, K.E(b).Tough, 1e-9, "카드 1/3칸 + 표식 추가 공격(0 비용) 1/6칸");
            Assert.AreEqual(1, b.St(K.E(b), "표식"));
        }

        [Test] public void 잔광은_공격_카드에_강인도_1칸_더_격파된_적이면_50퍼센트()
        {
            var b = K.Fight(); K.Hand(b, "hit", "hit");
            b.Pool.Status["잔광"] = 2;
            K.Play(b, "hit");
            Assert.AreEqual(4 - 1.0 / 3 - 1, K.E(b).Tough, 1e-9);
            K.E(b).Broken = true;
            K.Play(b, "hit");
            Assert.AreEqual(900 - 150, K.Hp(b));
            Assert.AreEqual(0, b.St(b.Pool, "잔광"));
        }

        [Test] public void 협공은_다른_아군의_공격력_100퍼센트()
        {
            var b = K.Fight(K.Data(), new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "hit");
            b.Pool.Status["협공"] = 1;
            K.Play(b, "hit");
            Assert.AreEqual(1000 - 100 - 80, K.Hp(b));
            Assert.AreEqual(0, b.St(b.Pool, "협공"));
        }

        [Test] public void 충격은_공격_카드의_대상이_되면_고정_80퍼센트_방어가_있으면_50퍼센트_더()
        {
            var d = K.Data(cards: "[{id:'shock', name:'충격 걸기', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'충격', v:2, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "shock", "hit", "hit");
            K.Play(b, "shock");
            K.Play(b, "hit");
            Assert.AreEqual(1000 - 80 - 100, K.Hp(b));
            K.E(b).Block = 500;
            K.Play(b, "hit");
            Assert.AreEqual(820, K.Hp(b), "고정 피해도 방어에는 막힌다");
            Assert.AreEqual(500 - 120 - 100, K.E(b).Block, "방어 위라 80 × 1.5 = 120");
        }

        [Test] public void 충격파는_카드에_맞으면_다른_적_모두_고정_300퍼센트()
        {
            var d = K.Data(cards: "[{id:'wave', name:'충격파 걸기', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'충격파', v:1, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "wave", "hit");
            K.Play(b, "wave", 0);
            K.Play(b, "hit", 0);
            Assert.AreEqual(900, K.Hp(b, 0));
            Assert.AreEqual(700, K.Hp(b, 1));
        }

        [Test] public void 그을림은_즉시_행동_셈이_오를_때마다_80퍼센트()
        {
            var d = K.Data(enemies: "[{id:'slow', name:'느림보', hp:1000, intents:[{t:'jam', v:0, rush:5}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "slow" }); K.Hand(b, "zero", "zero");
            K.E(b).Status["그을림"] = 2; K.E(b).DotU["그을림"] = 100;
            K.Play(b, "zero");
            Assert.AreEqual(920, K.Hp(b));
            K.Play(b, "zero");
            Assert.AreEqual(840, K.Hp(b));
            Assert.AreEqual(0, b.St(K.E(b), "그을림"));
        }

        [Test] public void 잔불은_격파된_적을_치면_겹마다_30퍼센트_그리고_사라진다()
        {
            var b = K.Fight(); K.Hand(b, "hit", "hit");
            K.E(b).Status["잔불"] = 2;
            K.Play(b, "hit");
            Assert.AreEqual(900, K.Hp(b), "격파 전엔 안 붙는다");
            K.E(b).Broken = true;
            K.Play(b, "hit");
            Assert.AreEqual(900 - 160, K.Hp(b));
            Assert.AreEqual(0, b.St(K.E(b), "잔불"));
        }

        [Test] public void 면역은_해로운_것_하나를_막는다()
        {
            var d = K.Data(enemies: "[{id:'curser', name:'저주꾼', hp:1000, intents:[{t:'debuff', id:'취약', v:2, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "curser" });
            b.Pool.Status["면역"] = 1;
            b.EndTurn();
            Assert.AreEqual(0, b.St(b.Pool, "취약"));
            Assert.AreEqual(0, b.St(b.Pool, "면역"));
            b.EndTurn();
            Assert.AreEqual(2, b.St(b.Pool, "취약"));
        }

        [Test] public void 저장은_남긴_AP_를_다음_턴으로()
        {
            var b = K.Fight(); K.Hand(b, "hit");
            b.Pool.Status["저장"] = 1;
            K.Play(b, "hit");
            b.EndTurn();
            Assert.AreEqual(5, b.Ap);
            Assert.AreEqual(0, b.St(b.Pool, "저장"));
        }

        [Test] public void 피해_감소는_받는_피해_15퍼센트_수_하나에_1()
        {
            var b = K.Fight("hitter");
            b.Pool.Status["피해 감소"] = 1;
            b.EndTurn();
            Assert.AreEqual(915, b.Pool.Hp);
            Assert.AreEqual(0, b.St(b.Pool, "피해 감소"));
        }

        [Test] public void 적에게_거는_세기_상태는_3겹까지()
        {
            var d = K.Data(enemies: "[{id:'rager', name:'성난 놈', hp:1000, intents:[{t:'buff', id:'사기', v:2, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "rager" });
            b.EndTurn(); b.EndTurn(); b.EndTurn();
            Assert.AreEqual(3, b.St(K.E(b), "사기"));
        }

        [Test] public void 디버프_해제는_차례대로()
        {
            var d = K.Data(cards: "[{id:'clean', name:'씻기', hero:'a', cost:0, type:'스킬', fx:[{k:'cleanse', v:1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "clean");
            b.Pool.Status["약화"] = 1; b.Pool.Status["취약"] = 1;
            K.Play(b, "clean");
            Assert.AreEqual(0, b.St(b.Pool, "취약"), "취약이 먼저");
            Assert.AreEqual(1, b.St(b.Pool, "약화"));
        }
    }
}
