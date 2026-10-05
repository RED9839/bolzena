using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>나머지 규칙 — 고동 · X 코스트 · 방어 파괴 · HP 치르기 · 전투 내내 증감 · 다음 카드 코스트 · 한 번의 일.</summary>
    public class MoreRuleTests
    {
        [Test] public void 고동은_턴_끝에_겹마다_적_전체_고정_70퍼센트()
        {
            var d = K.Data(cards: "[{id:'beat', name:'고동', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'고동', v:2}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "beat");
            K.Play(b, "beat");
            b.Enemies[0].Status["취약"] = 1;
            b.EndTurn();
            Assert.AreEqual(1000 - 140, K.Hp(b, 0), "고정 피해 — 취약을 안 탄다");
            Assert.AreEqual(1000 - 140, K.Hp(b, 1));
            Assert.AreEqual(2, b.St(b.Pool, "고동"), "세기 — 줄지 않는다");
        }

        [Test] public void X_코스트는_남은_AP_를_전부_쓰고_그_수만큼()
        {
            var d = K.Data(cards: "[{id:'xx', name:'몰아치기', hero:'a', cost:0, x:true, type:'공격', fx:[{k:'dmg', ratio:0.5, xHits:true, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "xx");
            K.Play(b, "xx");
            Assert.AreEqual(1000 - 150, K.Hp(b));
            Assert.AreEqual(0, b.Ap);
            Assert.AreEqual(30, b.Gauge);
        }

        [Test] public void 방어_실드_파괴_뒤_피해()
        {
            var d = K.Data(cards: "[{id:'brk', name:'부수고', hero:'a', cost:0, type:'공격', fx:[{k:'strip', target:'oneEnemy'}, {k:'dmg', ratio:2.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "brk");
            K.E(b).Block = 300; K.E(b).Shield = 300;
            K.Play(b, "brk");
            Assert.AreEqual(800, K.Hp(b));
        }

        [Test] public void HP_치르기는_방어로_못_막는다()
        {
            var d = K.Data(cards: "[{id:'pay', name:'대가', hero:'a', cost:0, type:'스킬', fx:[{k:'payHpPct', v:0.1}, {k:'payHp', v:50}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "pay");
            b.Pool.Block = 999;
            K.Play(b, "pay");
            Assert.AreEqual(1000 - 100 - 50, b.Pool.Hp);
            Assert.AreEqual(999, b.Pool.Block);
        }

        [Test] public void 이번_턴_증감은_적의_차례까지_가고_다음_내_턴에_풀린다()
        {
            var d = K.Data(cards: "[{id:'wall', name:'버티기', hero:'a', cost:0, type:'스킬', fx:[{k:'takenMod', v:-0.5, target:'self'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "hitter" }); K.Hand(b, "wall");
            K.Play(b, "wall");
            b.EndTurn();
            Assert.AreEqual(950, b.Pool.Hp);
            b.EndTurn();
            Assert.AreEqual(850, b.Pool.Hp);
        }

        [Test] public void 받는_피해_증감은_파티에서_좋은_것_하나_나쁜_것_하나()
        {
            var d = K.Data(cards: "[{id:'wall', name:'다 함께', hero:'a', cost:0, type:'스킬', fx:[{k:'takenMod', v:-0.2, target:'allAllies'}]}]");
            var b = K.Fight(d, new[] { "a", "b", "c" }, new[] { "hitter" }); K.Hand(b, "wall");
            K.Play(b, "wall");
            b.EndTurn();
            Assert.AreEqual(2400 - 80, b.Pool.Hp, "-20% 가 세 번 겹치지 않는다");
        }

        [Test] public void 적에게_거는_받는_피해_증감()
        {
            var d = K.Data(cards: "[{id:'expose', name:'드러내기', hero:'a', cost:0, type:'스킬', fx:[{k:'takenMod', v:0.25, turns:2}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "expose", "hit");
            K.Play(b, "expose");
            K.Play(b, "hit");
            Assert.AreEqual(875, K.Hp(b), "대상 말이 없으면 받는 피해 + 는 고른 적에게");
            b.EndTurn(); K.Hand(b, "hit"); K.Play(b, "hit");
            Assert.AreEqual(875 - 125, K.Hp(b), "2턴간");
            b.EndTurn(); K.Hand(b, "hit"); K.Play(b, "hit");
            Assert.AreEqual(750 - 100, K.Hp(b));
        }

        [Test] public void 다음_카드_코스트()
        {
            var d = K.Data(cards: "[{id:'prep', name:'준비', hero:'a', cost:0, type:'스킬', fx:[{k:'nextCheaper', v:1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "prep", "big", "big");
            K.Play(b, "prep");
            Assert.AreEqual(1, b.CostOf("big"));
            K.Play(b, "big");
            Assert.AreEqual(2, b.CostOf("big"), "다음 한 장에만");
        }

        [Test] public void 무작위_적_타격은_살아_있는_적에게만()
        {
            var d = K.Data(cards: "[{id:'rain', name:'비', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.5, hits:6, target:'randomEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "rain");
            b.Enemies[0].Hp = 40;
            K.Play(b, "rain");
            int lost1 = 1000 - K.Hp(b, 1);
            Assert.AreEqual(0, lost1 % 50, "한 대 50");
            if (!b.Enemies[0].Dead) Assert.AreEqual(300, lost1, "첫 적이 안 맞았으면 여섯 대가 다 둘째에게");
            else Assert.LessOrEqual(lost1, 250, "쓰러진 적에게 간 대 수만큼 덜");
        }

        [Test] public void 기절한_적은_다음_차례에_못_움직이고_모은_힘이_흩어진다()
        {
            var d = K.Data(enemies: "[{id:'ch', name:'모으기', hp:1000, intents:[{t:'charge', say:'모은다', next:{t:'attack', v:300, say:'쾅'}}, {t:'attack', v:10, rush:0}]}]",
                cards: "[{id:'stun', name:'기절', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'기절', v:1, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "ch" });
            b.EndTurn();
            Assert.AreEqual(300, K.E(b).Intent.V);
            K.Hand(b, "stun"); K.Play(b, "stun");
            b.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp);
        }
    }
}
