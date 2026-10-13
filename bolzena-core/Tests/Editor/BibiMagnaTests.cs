using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// (2026-10-13) 비비 원작 1:1 — 반격 전체화 n턴(counterAll turns) · 반격 피해 전투 내내(counterMod run) · 파티 상태 겹 비례(perStack 반격) ·
    /// 살아 있는 적 1명당(perFoe) · 자신의 카드로 실드를 얻으면(guard mine).
    /// </summary>
    public class BibiMagnaTests
    {
        const string HEROES = @"[
 {id:'bb', name:'비', role:'탱커', hp:800, atk:100, def:50, crit:0},
 {id:'cl', name:'클', role:'탱커', hp:800, atk:100, def:50, crit:0}
]";
        const string CARDS = @"[
 {id:'ctr', name:'반격 걸기', hero:'bb', cost:0, type:'스킬', fx:[{k:'status', id:'반격', v:1}]},
 {id:'all1', name:'1턴 전체', hero:'bb', cost:0, type:'스킬', fx:[{k:'counterAll', turns:1}]},
 {id:'boostRun', name:'전투 내내 반격', hero:'bb', cost:0, type:'강화', fx:[{k:'counterMod', v:1, run:true}]},
 {id:'per1', name:'반격 1 실드', hero:'bb', cost:0, type:'스킬', fx:[{k:'status', id:'반격', v:1}, {k:'perStack', id:'반격'}, {k:'shield', ratio:0.5}]},
 {id:'per3', name:'반격 3 실드', hero:'bb', cost:0, type:'스킬', fx:[{k:'status', id:'반격', v:3}, {k:'perStack', id:'반격'}, {k:'shield', ratio:0.5}]},
 {id:'foeShield', name:'적 수 실드', hero:'bb', cost:0, type:'스킬', fx:[{k:'perFoe'}, {k:'shield', ratio:0.5}]},
 {id:'storm', name:'폭풍', hero:'bb', cost:0, type:'강화', fx:[{k:'power', rules:[{when:{on:'guard', kind:'shield', mine:true}, fx:[{k:'status', id:'반격', v:1}]}]}]},
 {id:'bbShield', name:'비 실드', hero:'bb', cost:0, type:'스킬', fx:[{k:'shield', ratio:1}]},
 {id:'clShield', name:'클 실드', hero:'cl', cost:0, type:'스킬', fx:[{k:'shield', ratio:1}]}
]";
        const string FOES = @"[{id:'poke', name:'콕', hp:1000, intents:[{t:'attack', v:100, rush:0}]}]";
        static Battle F(string[] party, params string[] foes) => K.Fight(K.Data(heroes: HEROES, cards: CARDS, enemies: FOES), party, foes);

        [Test] public void 반격_전체화_1턴이면_다음_턴엔_때린_적만()
        {
            var b = F(new[] { "bb" }, "poke", "dummy");
            K.Hand(b, "ctr", "ctr", "all1"); K.Play(b, "ctr"); K.Play(b, "ctr"); K.Play(b, "all1");
            b.EndTurn();
            int d1 = K.Hp(b, 1);
            Assert.Less(d1, 1000, "이번 턴(적의 차례까지)은 때리지 않은 적도 맞는다");
            int p1 = K.Hp(b, 0);
            K.Hand(b, "ctr"); K.Play(b, "ctr");
            b.EndTurn();
            Assert.Less(K.Hp(b, 0), p1, "다음 턴에도 반격은 남아 때린 적을 친다");
            Assert.AreEqual(d1, K.Hp(b, 1), "1턴이 지나면 다시 때린 적만");
        }

        [Test] public void 반격_피해_증감_run_이면_다음_턴에도()
        {
            int Lost(bool boost)
            {
                var b = F(new[] { "bb" }, "poke");
                K.Hand(b, "ctr", "boostRun"); if (boost) K.Play(b, "boostRun");
                b.EndTurn();   // 1턴은 반격 없이 넘긴다 — 증감이 한 턴 뒤에도 남는지 본다
                K.Hand(b, "ctr"); K.Play(b, "ctr");
                int hp0 = K.Hp(b);
                b.EndTurn();
                return hp0 - K.Hp(b);
            }
            int a = Lost(false), x = Lost(true);
            Assert.Greater(a, 0);
            Assert.AreEqual(a * 2, x, 2, "전투 내내 반격 피해 ×2");
        }

        [Test] public void perStack_반격_파티_상태_겹만큼()
        {
            int Sh(string id) { var b = F(new[] { "bb" }, "dummy"); K.Hand(b, id); K.Play(b, id); return b.PartyShield; }
            int one = Sh("per1"), three = Sh("per3");
            Assert.Greater(one, 0);
            Assert.AreEqual(one * 3, three, 3, "반격 3 이면 실드 3배");
        }

        [Test] public void perFoe_살아_있는_적_1명당()
        {
            int Sh(params string[] foes) { var b = F(new[] { "bb" }, foes); K.Hand(b, "foeShield"); K.Play(b, "foeShield"); return b.PartyShield; }
            int one = Sh("dummy"), three = Sh("dummy", "dummy", "dummy");
            Assert.Greater(one, 0);
            Assert.AreEqual(one * 3, three, 3, "적 3명이면 실드 3배");
        }

        [Test] public void guard_mine_자신의_카드로_얻은_실드만()
        {
            var b = F(new[] { "bb", "cl" }, "dummy");
            K.Hand(b, "storm", "clShield", "bbShield");
            K.Play(b, "storm");
            K.Play(b, "clShield");
            Assert.AreEqual(0, b.St(b.Pool, "반격"), "동료 카드의 실드에는 안 돈다");
            K.Play(b, "bbShield");
            Assert.AreEqual(1, b.St(b.Pool, "반격"), "자신의 카드로 실드를 얻으면 반격 1");
        }
    }
}
