using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 마무리(즉사의 턴제판, 2026-10-06 티그(영웅) · 로니) — ifFoe hp · hpMob(보스 제외), 상태가 걸린 적(아멜리아 감전 → 충격).
    /// </summary>
    public class FinishTests
    {
        const string CARDS = "[{id:'fin', name:'마무리', hero:'a', cost:0, type:'공격', fx:[" +
            "{k:'ifFoe', id:'hp', pct:0.3}, {k:'dmg', ratio:1.0, target:'oneEnemy'}," +
            "{k:'ifFoe', id:'hpMob', pct:0.3}, {k:'dmg', ratio:3.0, target:'oneEnemy'}]}," +
            " {id:'zap', name:'지지기', hero:'a', cost:0, type:'스킬', fx:[{k:'ifFoe', id:'충격'}, {k:'status', id:'약화', v:1, target:'oneEnemy'}]}]";
        const string FOES = "[{id:'mob', name:'잡몹', hp:1000, intents:[{t:'jam', v:0, rush:0}]}, {id:'boss1', name:'두목', hp:1000, boss:true, intents:[{t:'jam', v:0, rush:0}]}]";

        static Battle F(string foe)
        {
            var b = K.Fight(K.Data(cards: CARDS, enemies: FOES), new[] { "a" }, new[] { foe });
            b.Enemies[0].Hp = 300;   // 30%
            K.Hand(b, "fin");
            return b;
        }

        [Test] public void HP_30퍼센트_이하_일반_적은_큰_마무리()
        {
            var b = F("mob"); K.Play(b, "fin");
            Assert.IsTrue(b.Enemies[0].Dead, "100% + 300% 로 쓰러짐");
        }

        [Test] public void 보스는_작은_덤만()
        {
            var b = F("boss1"); K.Play(b, "fin");
            Assert.AreEqual(300 - 100, b.Enemies[0].Hp, "hp 갈래만 — hpMob 은 보스에 안 돈다");
        }

        [Test] public void HP가_넉넉하면_덤_없음()
        {
            var b = K.Fight(K.Data(cards: CARDS, enemies: FOES), new[] { "a" }, new[] { "mob" }); K.Hand(b, "fin");
            K.Play(b, "fin");
            Assert.AreEqual(1000, b.Enemies[0].Hp);
        }

        [Test] public void 상태가_걸린_적이면()
        {
            var b = K.Fight(K.Data(cards: CARDS, enemies: FOES), new[] { "a" }, new[] { "mob" }); K.Hand(b, "zap", "zap");
            K.Play(b, "zap");
            Assert.AreEqual(0, b.St(b.Enemies[0], "약화"), "충격이 없으면 안 돈다");
            b.Enemies[0].Status["충격"] = 1;
            K.Play(b, "zap");
            Assert.AreEqual(1, b.St(b.Enemies[0], "약화"));
        }
    }
}
