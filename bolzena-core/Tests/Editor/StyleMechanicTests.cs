using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>운영 방식 계기(docs/19 §3 「새로」) — 박자 · 아껴 두기 · 버리기 · 계산 · 대가 · 표적 · 흠 세기 · 버팀.</summary>
    public class StyleMechanicTests
    {
        static GameData D(string cards = null, string heroes = null, string enemies = null) => K.Data(heroes: heroes, cards: cards, enemies: enemies);

        // ── 박자형 ──
        [Test] public void 같은_카드_잇달아는_되풀이_조건과_리듬_1()
        {
            var d = D("[{id:'rep', name:'반복', hero:'a', cost:0, type:'공격', fx:[{k:'ifRepeat'}, {k:'gauge', v:20}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "rep", "rep", "zero", "rep");
            K.Play(b, "rep"); Assert.AreEqual(0, b.Gauge);
            K.Play(b, "rep"); Assert.AreEqual(20, b.Gauge);
            Assert.AreEqual(1, b.St(b.Pool, R.RHYTHM), "되풀이도 순서 조건 — 리듬 +1");
            K.Play(b, "zero"); K.Play(b, "rep"); Assert.AreEqual(20, b.Gauge, "사이에 다른 카드가 끼면 끊긴다");
        }

        [Test] public void 같은_카드를_잇달아_내면_패시브()
        {
            var d = D(heroes: "[{id:'p', name:'반복', role:'탱커', row:'front', hp:900, atk:80, def:60, passives:[{name:'구령', when:{on:'play', repeat:true}, fx:[{k:'gauge', v:7}]}]}]",
                cards: "[{id:'p0', name:'하나', hero:'p', cost:0, type:'공격', fx:[]}, {id:'p1', name:'둘', hero:'p', cost:0, type:'공격', fx:[]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "p0", "p1", "p1", "p1");
            K.Play(b, "p0"); K.Play(b, "p1"); Assert.AreEqual(0, b.Gauge);
            K.Play(b, "p1"); Assert.AreEqual(7, b.Gauge);
            K.Play(b, "p1"); Assert.AreEqual(14, b.Gauge);
        }

        // ── 아껴 두기형 ──
        [Test] public void 손에_N턴_묵힌_카드()
        {
            var d = D("[{id:'aged', name:'묵은지', hero:'a', cost:0, type:'스킬', tags:['보존'], fx:[{k:'ifHeld', n:2}, {k:'gauge', v:50}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "aged");
            b.EndTurn(); Assert.AreEqual(1, b.Held["aged"]);
            var c = b.Clone(); K.Play(c, "aged"); Assert.AreEqual(0, c.Gauge, "한 턴만 묵혔다");
            b.EndTurn(); Assert.AreEqual(2, b.Held["aged"]);
            K.Play(b, "aged"); Assert.AreEqual(50, b.Gauge);
        }

        [Test] public void 이번_턴_첫_장이면_AP가_남으면()
        {
            var d = D("[{id:'first', name:'첫 수', hero:'a', cost:1, type:'스킬', fx:[{k:'ifPlayedMax', n:0}, {k:'gauge', v:10}]}, {id:'spare', name:'여유', hero:'a', cost:1, type:'스킬', fx:[{k:'ifApLeft', n:2}, {k:'gauge', v:100}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "first", "first", "spare");
            K.Play(b, "first"); Assert.AreEqual(10 + 10, b.Gauge);
            K.Play(b, "first"); Assert.AreEqual(30, b.Gauge, "둘째 장은 아니다");
            K.Play(b, "spare"); Assert.AreEqual(40, b.Gauge, "AP 0 남았다");
            b.EndTurn(); K.Hand(b, "spare"); K.Play(b, "spare"); Assert.AreEqual(150, b.Gauge, "AP 2 남았다");
        }

        // ── 버리기형 ──
        [Test] public void 카드가_버려지면_그리고_버린_장수만큼()
        {
            var d = D(heroes: "[{id:'p', name:'버림', role:'딜러', row:'back', hp:600, atk:100, def:20, passives:[{name:'털기', when:{on:'discard'}, fx:[{k:'gauge', v:3}]}]}, {id:'q', name:'남', role:'탱커', row:'front', hp:900, atk:80, def:60, passives:[{name:'줍기', when:{on:'discard', who:'any'}, fx:[{k:'gauge', v:1}]}]}]",
                cards: "[{id:'toss', name:'던지기', hero:'p', cost:0, type:'공격', fx:[{k:'discard', v:2}, {k:'perDiscarded'}, {k:'dmg', ratio:0.5, target:'oneEnemy'}]}, {id:'z', name:'잡동사니', hero:'q', cost:0, type:'스킬', fx:[]}]");
            var b = K.Fight(d, new[] { "p", "q" }, new[] { "dummy" }); K.Hand(b, "toss", "z", "z");
            K.Play(b, "toss");
            Assert.AreEqual(1000 - 100, K.Hp(b), "버린 두 장 × 50");
            Assert.AreEqual(2 * 3 + 2 * 1, b.Gauge, "제 효과로 버림 3씩 · 누구든 1씩");
            Assert.AreEqual(2, b.DiscardedTurn);
        }

        // ── 계산형 ──
        [Test] public void 이번_턴_쓴_AP가_꼭_N_그리고_공격_스킬_같은_장수()
        {
            var d = D("[{id:'calc', name:'셈', hero:'a', cost:1, type:'스킬', fx:[{k:'ifSpent', n:3}, {k:'gauge', v:30}]}, {id:'eq', name:'맞춤', hero:'a', cost:0, type:'스킬', fx:[{k:'ifBalanced'}, {k:'gauge', v:5}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "big", "calc", "eq", "eq");
            K.Play(b, "eq"); Assert.AreEqual(0, b.Gauge, "공격 0 · 스킬 1");
            K.Play(b, "big");
            K.Play(b, "calc"); Assert.AreEqual(20 + 10 + 30, b.Gauge, "2 + 1 = 3");
            Assert.AreEqual(3, b.ApSpent);
            K.Play(b, "eq"); Assert.AreEqual(60, b.Gauge, "공격 1 · 스킬 3");
        }

        // ── 대가형 ──
        [Test] public void 다음_턴_AP_그리고_HP를_치르면_한_장에_한_번()
        {
            var d = D(heroes: "[{id:'p', name:'대가', role:'딜러', row:'back', hp:1000, atk:100, def:20, passives:[{name:'과충전', when:{on:'pay', who:'any'}, fx:[{k:'gauge', v:4}]}]}]",
                cards: "[{id:'blood', name:'피값', hero:'p', cost:0, type:'공격', fx:[{k:'payHp', v:50}, {k:'payHp', v:50}, {k:'nextAp', v:-1}, {k:'perPaid'}, {k:'dmg', ratio:0.3, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "blood");
            K.Play(b, "blood");
            Assert.AreEqual(900, b.Pool.Hp);
            Assert.AreEqual(4, b.Gauge, "한 장에 한 번");
            Assert.AreEqual(100, b.PaidHp);
            Assert.AreEqual(1000 - 30, K.Hp(b), "치른 HP 100 당 30%");
            b.EndTurn();
            Assert.AreEqual(2, b.Ap, "다음 턴 AP -1");
        }

        [Test] public void 치른_HP로도_HP가_N퍼센트_이하가_되면()
        {
            var d = D(heroes: "[{id:'p', name:'대가', role:'딜러', row:'back', hp:1000, atk:100, def:20, passives:[{name:'고비', when:{on:'lowHp', pct:0.5}, fx:[{k:'gauge', v:9}]}]}]",
                cards: "[{id:'blood', name:'피값', hero:'p', cost:0, type:'스킬', fx:[{k:'payHpPct', v:0.6}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "blood");
            K.Play(b, "blood");
            Assert.AreEqual(9, b.Gauge);
        }

        [Test] public void 손패_소멸시키기와_카드가_소멸하면()
        {
            var d = D(heroes: "[{id:'p', name:'장작', role:'딜러', row:'back', hp:600, atk:100, def:20, passives:[{name:'장작불', when:{on:'exhaust', who:'any'}, fx:[{k:'gauge', v:6}]}]}]",
                cards: "[{id:'burn', name:'태우기', hero:'p', cost:0, type:'스킬', fx:[{k:'burn', v:1}]}, {id:'ex', name:'한 번', hero:'p', cost:0, type:'스킬', tags:['소멸'], fx:[]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "burn", "zero", "ex");
            Assert.AreEqual(1, b.DiscardChoice(0), "태울 카드를 고른다");
            K.Play(b, "burn", 0, new PlayOpts { Discard = new List<string> { "zero" } });
            CollectionAssert.Contains(b.Gone, "zero");
            Assert.AreEqual(6, b.Gauge);
            K.Play(b, "ex");
            Assert.AreEqual(12, b.Gauge, "소멸 태그로 사라져도");
        }

        // ── 표적형 ──
        [Test] public void 찍기는_한_번에_한_적_옮기면_처음부터_찍은_적이_쓰러지면()
        {
            var d = D(heroes: "[{id:'h', name:'사냥꾼', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'현상수배', carrier:'enemy', hunt:true, cap:5}, passives:[{name:'다음 사냥감', when:{on:'huntDown'}, fx:[{k:'perStack', id:'현상수배'}, {k:'dmg', ratio:0.1, target:'topEnemy'}, {k:'stack', id:'현상수배', v:2, target:'topEnemy'}]}]}]",
                cards: "[{id:'mark', name:'찍기', hero:'h', cost:0, type:'스킬', fx:[{k:'stack', id:'현상수배', v:3}]}, {id:'kill', name:'처형', hero:'h', cost:0, type:'공격', fx:[{k:'ifHunted'}, {k:'dmg', ratio:20, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "h" }, new[] { "dummy", "dummy", "dummy" }); K.Hand(b, "mark", "mark", "kill", "kill");
            b.Enemies[2].Hp = 500;
            K.Play(b, "mark", 0);
            K.Play(b, "mark", 1);
            Assert.AreEqual(0, b.St(b.Enemies[0], "현상수배"), "옮기면 옛 적의 것은 사라진다");
            Assert.AreEqual(3, b.St(b.Enemies[1], "현상수배"));
            K.Play(b, "kill", 0); Assert.AreEqual(1000, K.Hp(b, 0), "찍힌 적이 아니면 안 돈다");
            K.Play(b, "kill", 1); Assert.IsTrue(b.Enemies[1].Dead);
            Assert.AreEqual(1000 - 30, K.Hp(b, 0), "쓰러진 적의 겹 3 × 10% — 체력이 가장 높은 적에게");
            Assert.AreEqual(2, b.St(b.Enemies[0], "현상수배"), "그 적을 새로 찍는다");
        }

        // ── 흠 세기형 ──
        [Test] public void 디버프_가짓수_1개당과_N가지_이상이면과_새_디버프()
        {
            var d = D(heroes: "[{id:'f', name:'흠', role:'딜러', row:'back', hp:600, atk:100, def:20, passives:[{name:'탐험 깃발', when:{on:'debuff', who:'any', fresh:true}, fx:[{k:'gauge', v:1}]}]}]",
                cards: @"[{id:'weak2', name:'두 흠', hero:'f', cost:0, type:'스킬', fx:[{k:'status', id:'취약', v:1, target:'oneEnemy'}, {k:'status', id:'약화', v:1, target:'oneEnemy'}]},
 {id:'count', name:'흠 세기', hero:'f', cost:0, type:'공격', fx:[{k:'perDebuff'}, {k:'dmg', ratio:0.2, target:'oneEnemy'}, {k:'ifDebuffs', n:3}, {k:'ap', v:5}]}]");
            var b = K.Fight(d, new[] { "f" }, new[] { "dummy" }); K.Hand(b, "weak2", "weak2", "count");
            K.Play(b, "weak2");
            Assert.AreEqual(1, b.Gauge, "새 가짓수 둘이지만 「디버프를 걸면」 은 카드 한 장에 한 번");
            K.Play(b, "weak2");
            Assert.AreEqual(1, b.Gauge, "이미 있던 것은 새 디버프가 아니다");
            Assert.AreEqual(2, b.DebuffKinds(K.E(b)));
            K.Play(b, "count");
            Assert.AreEqual(1000 - 60, K.Hp(b), "2가지 × 20% × 1.5(취약)");
            Assert.AreEqual(3, b.Ap, "3가지가 아니다");
        }

        // ── 버팀형 ──
        [Test] public void 받은_피해를_되돌린다_패시브와_카드()
        {
            var d = D(heroes: "[{id:'t', name:'버팀', role:'탱커', row:'front', hp:1000, atk:80, def:60, passives:[{name:'가시 갑옷', when:{on:'blocked'}, fx:[{k:'reflect', ratio:0.5}]}]}]",
                cards: "[{id:'back', name:'되갚기', hero:'t', cost:0, type:'공격', fx:[{k:'reflect', ratio:1.0}]}]",
                enemies: "[{id:'h2', name:'때림', hp:1000, intents:[{t:'attack', v:100, rush:0}]}]");
            var b = K.Fight(d, new[] { "t" }, new[] { "h2" });
            b.Pool.Block = 500;
            b.EndTurn();
            Assert.AreEqual(1000 - 50, K.Hp(b), "다 막은 100 의 50%");
            K.Hand(b, "back"); K.Play(b, "back");
            Assert.AreEqual(950 - 100, K.Hp(b), "지난 적의 차례에 받은 100 의 100%");
        }

        [Test] public void 글과_검사()
        {
            var t = new CardText(K.Data());
            Assert.AreEqual("되풀이: 드로우 1", t.Fx(new List<Fx> { new Fx { K = "ifRepeat" }, new Fx { K = "draw", V = 1 } }));
            Assert.AreEqual("이번 턴 쓴 AP가 꼭 3이면 AP +1", t.Fx(new List<Fx> { new Fx { K = "ifSpent", N = 3 }, new Fx { K = "ap", V = 1 } }));
            Assert.AreEqual("적 1명의 디버프 1가지당 적 1명에게 피해 20%", t.Fx(new List<Fx> { new Fx { K = "perDebuff" }, new Fx { K = "dmg", Ratio = 0.2, Target = "oneEnemy" } }));
            Assert.AreEqual("다음 턴 AP -1, 손패 1장 소멸", t.Fx(new List<Fx> { new Fx { K = "nextAp", V = -1 }, new Fx { K = "burn", V = 1 } }));
            Assert.AreEqual("가시: 공격을 방어 · 실드로 다 막으면 그 피해의 50%를 때린 적에게 고정 피해로",
                t.Passives(new List<PassiveRule> { new PassiveRule { Name = "가시", When = new When { On = "blocked" }, Fx = new List<Fx> { new Fx { K = "reflect", Ratio = 0.5 } } } }));
            Assert.AreEqual("아군이 적에게 새 디버프를 걸면", t.WhenText(new When { On = "debuff", Who = "any", Fresh = true }));
            var kw = new KeywordDef { Name = "현상수배", Desc = "찍어 둔 사냥감", Carrier = "enemy", Hunt = true, Cap = 5 };
            StringAssert.Contains("한 번에 적 1명에게만(다른 적에게 쌓으면 옮겨 가고 처음부터)", t.Keyword(kw));
            var v = Validator.Check(K.Data(heroes: "[{id:'x', name:'x', role:'딜러', hp:1, atk:1, def:1, keyword:{name:'k', hunt:true}}]",
                cards: "[{id:'bad', name:'잘못', hero:'x', cost:0, type:'스킬', fx:[{k:'ifSpent'}, {k:'reflect'}, {k:'perDebuff'}, {k:'draw', v:1}]}]"));
            string all = string.Join("\n", v.Errors);
            StringAssert.Contains("찍기(hunt)는", all);
            StringAssert.Contains("ifSpent: n 이 없다", all);
            StringAssert.Contains("reflect: ratio", all);
            StringAssert.Contains("1개당」 바로 뒤", all);
        }
    }
}
