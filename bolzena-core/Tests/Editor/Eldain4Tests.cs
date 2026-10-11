using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>엘다인 3차 원작 검토(2026-10-11, Docs/데이터.md §24 · E63~E75) — 도발 · 손님 계기 · 은방울 · 다음 카드 치명 · 분산 · 바로 앞 동료 카드 다시 · 친 횟수 · 처형 · 순환 칸 덤 · 단계 고르기 · 고정 · 단계 카드 덤 · 시작 값 · 들 때.</summary>
    public class Eldain4Tests
    {
        const string HEROES = @"[
 {id:'tt', name:'도', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'도발 수', cap:9}},
 {id:'sv', name:'실', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'초청객', carrier:'hero', cap:9},
  keywords:[{name:'결투', carrier:'enemy', cap:9}, {name:'방울', cap:99}],
  passives:[{name:'손님 덤', when:{on:'hit', marked:'초청객', foeStack:'결투'}, fx:[{k:'stack', id:'방울', v:1}]}]},
 {id:'jj', name:'죠', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'교리', carrier:'hero', cap:9, share:0.3}},
 {id:'hg', name:'티', role:'딜러', hp:600, atk:100, def:20, crit:0,
  keyword:{name:'영웅의 길', cap:3, wrap:true, stages:['하나','둘','셋'], start:1, onStage:[[{k:'stack', id:'영웅심', v:1}], [{k:'stack', id:'영웅심', v:10}], [{k:'stack', id:'영웅심', v:100}]]},
  keywords:[{name:'영웅심', cap:999}]},
 {id:'ym', name:'요', role:'딜러', hp:600, atk:100, def:20, crit:0,
  keyword:{name:'위상', cap:4, wrap:true, stages:['초승','반달','상현','보름'], start:2, stageBonus:[{stage:4, type:'공격', ratio:2}]},
  passives:[{name:'차오름', when:{on:'turnStart'}, fx:[{k:'stack', id:'위상', v:1}]}]},
 {id:'dn2', name:'디', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'기혈', cap:9}, keywords:[{name:'연격', cap:9}],
  forms:[{id:'bg', name:'맨주먹', turns:2, turnMake:[{id:'tt_fist', n:1}], onEnter:[{k:'spend', id:'기혈', all:true}, {k:'stack', id:'연격', v:1, ofEvent:1}]}]}
]";
        const string CARDS = @"[
 {id:'taunt', name:'경계선', hero:'tt', cost:0, type:'스킬', fx:[{k:'taunt', v:0.5}]},
 {id:'invite', name:'초대', hero:'sv', cost:0, type:'스킬', fx:[{k:'stack', id:'초청객', v:1, target:'oneAlly'}]},
 {id:'duel', name:'결투', hero:'sv', cost:0, type:'스킬', fx:[{k:'stack', id:'결투', v:1, target:'oneEnemy'}]},
 {id:'tip', name:'작은 뜻', hero:'a', cost:0, type:'스킬', fx:[{k:'critNext', who:'other'}]},
 {id:'doctrine', name:'교리', hero:'jj', cost:0, type:'스킬', fx:[{k:'stack', id:'교리', v:1, target:'oneAlly'}]},
 {id:'again', name:'경전 낭독', hero:'jj', cost:0, type:'공격', fx:[{k:'castOther', from:'last', ratio:0.6}]},
 {id:'strike', name:'두 번', hero:'hg', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, hits:2, target:'oneEnemy'}]},
 {id:'finish', name:'결착', hero:'hg', cost:0, type:'공격', fx:[{k:'ifFoe', hitsBy:'self', n:2}, {k:'execute', pct:0.5, bossRatio:1}]},
 {id:'step', name:'한 걸음', hero:'hg', cost:0, type:'스킬', fx:[{k:'stack', id:'영웅의 길', v:1}]},
 {id:'pray', name:'기도', hero:'ym', cost:0, type:'스킬', fx:[{k:'stack', id:'위상', pick:true, lock:true, turns:1}]},
 {id:'moonhit', name:'달빛', hero:'ym', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'bgin', name:'도끼 부수기', hero:'dn2', cost:0, type:'스킬', fx:[{k:'form', id:'bg'}]},
 {id:'tt_fist', name:'주먹', hero:'dn2', token:true, cost:0, type:'공격', tags:['증발', '소멸'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]}
]";
        const string FOES = @"[{id:'poke', name:'콕', hp:1000, intents:[{t:'attack', v:100, rush:0}]}]";
        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(string[] party, params string[] foes) => K.Fight(D(), party, foes.Length > 0 ? foes : new[] { "dummy" });

        [Test] public void E63_도발은_그_대에_실드를_더_쓴다()
        {
            int Lost(bool taunt)
            {
                var b = F(new[] { "a", "tt" }, "poke");
                if (taunt) { K.Hand(b, "taunt"); K.Play(b, "taunt"); }
                b.Pool.Shield = 80; int hp0 = b.Pool.Hp;
                b.EndTurn();
                return hp0 - b.Pool.Hp;
            }
            Assert.AreEqual(20, Lost(false));
            Assert.AreEqual(0, Lost(true), "실드 80 × 1.5 = 120 이 100 을 다 막는다");
        }

        [Test] public void E64_손님_카드가_결투_적을_치면()
        {
            var b = F(new[] { "a", "sv" });
            K.Hand(b, "invite", "duel", "hit"); b.Ap = 3;
            K.Play(b, "invite", 0, new PlayOpts { Ally = b.HeroUnit("a").Idx });
            K.Play(b, "hit");
            Assert.AreEqual(0, b.StackOf("sv", "방울"), "결투가 없으면 안 돈다");
            K.Play(b, "duel");
            K.Hand(b, "hit"); K.Play(b, "hit");
            Assert.AreEqual(1, b.StackOf("sv", "방울"));
        }

        [Test] public void E65_은방울()
        {
            var b = F(new[] { "a" }, "poke");
            b.Pool.Status["은방울"] = 2; b.Pool.Shield = 50;
            b.EndTurn();
            Assert.AreEqual(1, b.St(b.Pool, "피해 감소"), "턴 끝에 실드가 남아 피해 감소 2 → 적의 한 대에 1 씀");
            Assert.AreEqual(0, b.St(b.Pool, "은방울"), "한 번만");
            var c = F(new[] { "a" });
            c.Pool.Status["은방울"] = 3; c.Pool.Shield = 50;
            c.EndTurn();
            Assert.AreEqual(3, c.St(c.Pool, "피해 감소"), "턴 끝에 실드가 남으면");
            Assert.AreEqual(0, c.St(c.Pool, "은방울"));
        }

        [Test] public void E66_다른_아군의_다음_카드_치명()
        {
            var b = F(new[] { "a", "c" });
            K.Hand(b, "tip", "hit_c"); b.Ap = 3;
            K.Play(b, "tip"); K.Play(b, "hit_c");
            Assert.Greater(1000 - K.Hp(b), 150, "치명 확정");
        }

        [Test] public void E69_분산_E70_바로_앞_동료_카드()
        {
            var b = F(new[] { "a", "jj" }, "poke");
            K.Hand(b, "doctrine"); K.Play(b, "doctrine", 0, new PlayOpts { Ally = b.HeroUnit("a").Idx });
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            Assert.AreEqual(70, hp0 - b.Pool.Hp, "표시된 아군(맨 앞 가)을 노린 100 이 70 으로");
            var c = F(new[] { "c", "jj" });
            K.Hand(c, "hit_c", "again"); c.Ap = 3;
            K.Play(c, "hit_c"); K.Play(c, "again");
            Assert.AreEqual(1000 - 150 - 90, K.Hp(c), "바로 앞 다 때리기(150) × 60%");
            StringAssert.Contains("이번 턴 바로 앞 다른 아군 카드를 다시(60%)", new CardText(c.Data).Card(c.CardOf("again")));
        }

        [Test] public void E71_E72_친_횟수와_처형()
        {
            var b = F(new[] { "hg" });
            K.E(b).Hp = 400;
            K.Hand(b, "finish", "strike", "finish"); b.Ap = 3;
            K.Play(b, "finish");
            Assert.IsFalse(K.E(b).Dead, "두 번 치기 전에는 안 선다");
            K.Play(b, "strike");
            Assert.AreEqual(2, b.HitsBy(K.E(b), "hg"));
            K.Play(b, "finish");
            Assert.IsTrue(K.E(b).Dead, "HP 50% 이하 — 처형");
        }

        [Test] public void E73_순환_칸_덤과_시작_값()
        {
            var b = F(new[] { "hg" });
            Assert.AreEqual(1, b.StackOf("hg", "영웅의 길"), "start 1");
            K.Hand(b, "step", "step", "step"); b.Ap = 3;
            K.Play(b, "step"); K.Play(b, "step"); K.Play(b, "step");
            Assert.AreEqual(10 + 100 + 1, b.StackOf("hg", "영웅심"), "둘 · 셋 · 한 바퀴 돌아 하나");
            StringAssert.Contains("둘에 닿으면", new CardText(b.Data).Keyword(b.Data.Hero("hg").Keyword));
        }

        [Test] public void E74_E75_단계_고르기_고정과_단계_덤()
        {
            var b = F(new[] { "ym" });
            Assert.AreEqual(3, b.StackOf("ym", "위상"), "start 2 + 첫 턴 시작 차오름");
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, b.StageChoices("pray"));
            K.Hand(b, "moonhit", "pray", "moonhit"); b.Ap = 3;
            K.Play(b, "moonhit");
            Assert.AreEqual(1000 - 10, K.Hp(b));
            K.Play(b, "pray", 0, new PlayOpts { Stage = 4 });
            Assert.AreEqual(4, b.StackOf("ym", "위상"));
            K.Play(b, "moonhit");
            Assert.AreEqual(990 - 20, K.Hp(b), "보름 — 공격 카드 피해 ×2");
            b.EndTurn();
            Assert.AreEqual(4, b.StackOf("ym", "위상"), "고정 — 다음 턴 시작에도 그대로");
            b.EndTurn();
            Assert.AreEqual(1, b.StackOf("ym", "위상"), "풀린 뒤 차오름");
        }

        [Test] public void 변신_들_때()
        {
            var b = F(new[] { "dn2" });
            b.Stacks["dn2"] = new Dictionary<string, int> { ["기혈"] = 4 };
            K.Hand(b, "bgin"); K.Play(b, "bgin");
            Assert.AreEqual(0, b.StackOf("dn2", "기혈"));
            Assert.AreEqual(4, b.StackOf("dn2", "연격"), "기혈을 모두 연격으로");
            StringAssert.Contains("들 때:", new CardText(b.Data).Form(b.Data.Form("bg")));
        }
    }
}
