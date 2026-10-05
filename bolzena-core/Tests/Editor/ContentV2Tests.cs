using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>v2 콘텐츠 요구(bolzena-content-v2/NEEDS.md) — 범용 부품. 사도 a(공 100 방 50) · b(공 80 방 40) · c(공 150 방 20).</summary>
    public class ContentV2Tests
    {
        static GameData D(string cards = null, string enemies = null, string heroes = null) => K.Data(heroes: heroes, cards: cards, enemies: enemies);

        [Test] public void 카드_거르개_cardStatus_비용_음수_사도_종류()
        {
            var b = K.Fight(D("[{id:'cs', name:'깎기', hero:'a', cost:0, type:'스킬', fx:[{k:'cardStatus', id:'비용', v:-1, to:'hand', n:1, who:'self', type:'공격'}]}]"), new[] { "a", "b" }, new[] { "dummy" });
            K.Hand(b, "cs", "hit_b", "big", "guard");
            K.Play(b, "cs");
            Assert.AreEqual(1, b.CostOf("big"), "a 의 공격 카드만 -1");
            Assert.AreEqual(1, b.CostOf("hit_b")); Assert.AreEqual(1, b.CostOf("guard"));
        }

        [Test] public void 카드_값_이_전투만_과_상한()
        {
            var d = K.Sample().Add(null, "[{id:'chg', name:'충전', hero:'rico', cost:0, type:'스킬', fx:[{k:'cardStatus', id:'충전', v:2, max:3, battle:true}]}]", null, null, null, null);
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 3); run.S.Deck.Add("chg");
            var (b, _) = run.OpenFight();
            K.Hand(b, "chg"); K.Play(b, "chg"); b.Hand.Add("chg"); b.Discard.Remove("chg"); K.Play(b, "chg");
            Assert.AreEqual(3, b.CardStOf("chg", "충전"), "상한 3");
            b.Over = "win"; run.AfterFight(b);
            Assert.IsFalse(run.S.CardVals.ContainsKey("chg"), "전투에서만");
        }

        [Test] public void 배타_무작위_셋_중_하나()
        {
            var d = D("[{id:'r3', name:'셋 중 하나', hero:'a', cost:0, type:'스킬', fx:[{k:'roll', n:3}, {k:'ifRoll', n:1}, {k:'gauge', v:1}, {k:'ifRoll', n:2}, {k:'gauge', v:10}, {k:'ifRoll', n:3}, {k:'gauge', v:100}]}]");
            for (int s = 1; s < 12; s++)
            {
                var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Seed = s); K.Hand(b, "r3"); K.Play(b, "r3");
                CollectionAssert.Contains(new[] { 1, 10, 100 }, b.Gauge, "정확히 하나만");
            }
        }

        [Test] public void 다시_내기_50퍼센트와_대신_발동_비율()
        {
            var b = K.Fight(D("[{id:'rc', name:'한 번 더', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'recast', ratio:0.5}]}, {id:'co', name:'흉내', hero:'a', cost:0, type:'스킬', fx:[{k:'castOther', ratio:0.5}]}]"), new[] { "a", "b" }, new[] { "dummy" });
            K.Hand(b, "rc", "co", "hit_b");
            K.Play(b, "rc"); Assert.AreEqual(850, K.Hp(b));
            K.Play(b, "co"); Assert.AreEqual(810, K.Hp(b), "b 의 80 × 50%");
        }

        [Test] public void 비용_증감_이번_턴_자기_카드와_한_장()
        {
            var b = K.Fight(D("[{id:'cm', name:'영웅', hero:'a', cost:0, type:'스킬', fx:[{k:'costMod', v:-1, who:'self'}]}, {id:'nx', name:'다음 공격', hero:'a', cost:0, type:'스킬', fx:[{k:'costMod', v:-2, n:1, type:'공격'}]}]"), new[] { "a", "b" }, new[] { "dummy" });
            K.Hand(b, "cm", "big", "hit_b", "nx");
            K.Play(b, "cm");
            Assert.AreEqual(1, b.CostOf("big")); Assert.AreEqual(1, b.CostOf("hit_b"));
            K.Play(b, "nx"); Assert.AreEqual(0, b.CostOf("big")); Assert.AreEqual(0, b.CostOf("hit_b"));
            K.Play(b, "hit_b"); Assert.AreEqual(1, b.CostOf("big"), "한 장 쓰면 끝 — 자기 카드 -1 만 남음");
            b.EndTurn(); Assert.AreEqual(2, b.CostOf("big"), "이번 턴만");
        }

        [Test] public void 태그_덧붙임_신속과_약점_공격_조건부()
        {
            var b = K.Fight(D("[{id:'sw', name:'틈', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'ifFoe', id:'broken'}, {k:'addTag', id:'신속'}]}]", "[{id:'slow', name:'느림', hp:1000, intents:[{t:'jam', v:0, rush:5}]}]"), new[] { "a" }, new[] { "slow" });
            K.Hand(b, "sw", "sw");
            K.Play(b, "sw"); Assert.AreEqual(4, b.ActionCount(K.E(b)), "격파 상태가 아니라 신속이 안 붙었다");
            K.E(b).Broken = true;
            K.Play(b, "sw"); Assert.AreEqual(4, b.ActionCount(K.E(b)), "격파 상태 — 이번엔 신속");
        }

        [Test] public void 한_대_깎기와_소환물_cut()
        {
            var b = K.Fight(D("[{id:'ct', name:'기도', hero:'a', cost:0, type:'스킬', fx:[{k:'cutHit', v:0.3}]}]"), new[] { "a" }, new[] { "hitter" }); K.Hand(b, "ct");
            K.Play(b, "ct"); b.EndTurn();
            Assert.AreEqual(930, b.Pool.Hp); b.EndTurn(); Assert.AreEqual(830, b.Pool.Hp, "한 대에만");
            var d = K.Data(heroes: "[{id:'ep', name:'에피', role:'탱커', row:'front', hp:1000, atk:100, def:50, keyword:{name:'에피콘', cap:3, guard:true, cut:0.2}}]",
                cards: "[{id:'ec', name:'부르기', hero:'ep', cost:0, type:'스킬', fx:[{k:'stack', id:'에피콘', v:1}]}]");
            var b2 = K.Fight(d, new[] { "ep" }, new[] { "hitter" }); K.Hand(b2, "ec"); K.Play(b2, "ec"); b2.EndTurn();
            Assert.AreEqual(920, b2.Pool.Hp); Assert.AreEqual(0, b2.StackOf("ep", "에피콘"));
        }

        [Test] public void AP_빚과_탕감()
        {
            var b = K.Fight(D("[{id:'db', name:'외상', hero:'a', cost:5, type:'공격', debt:true, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'ifKill'}, {k:'clearDebt'}]}]"), new[] { "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "db");
            K.E(b).Hp = 50; K.Play(b, "db", 0);
            Assert.AreEqual(0, b.ApJam, "처치 — 빚 탕감");
        }

        [Test] public void 고유_효과_섞어_치르기_비율()
        {
            var d = K.Data(heroes: "[{id:'g', name:'금화', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'금화', cap:9}}]",
                cards: "[{id:'buy', name:'사기', hero:'g', cost:2, type:'스킬', payWith:'금화', payRate:2, payMix:true, fx:[{k:'gauge', v:5}]}, {id:'coin', name:'벌기', hero:'g', cost:0, type:'스킬', fx:[{k:'stack', id:'금화', v:3}]}]");
            var b = K.Fight(d, new[] { "g" }, new[] { "dummy" }); K.Hand(b, "coin", "buy");
            K.Play(b, "coin"); K.Play(b, "buy");
            Assert.AreEqual(1, b.StackOf("g", "금화"), "금화 2 = AP 1"); Assert.AreEqual(2, b.Ap, "나머지 1 은 AP");
        }

        [Test] public void 새_대상_행동_카운트와_다른_적_표식_많은_적()
        {
            var d = D("[{id:'nx', name:'먼저', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'nextEnemy'}]}, {id:'ot', name:'옆', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'otherEnemy'}]}]",
                "[{id:'r3', name:'빠름', hp:1000, intents:[{t:'jam', v:0, rush:3}]}, {id:'r6', name:'느림', hp:1000, intents:[{t:'jam', v:0, rush:6}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "r6", "r3" }); K.Hand(b, "nx", "ot");
            K.Play(b, "nx"); Assert.AreEqual(900, K.Hp(b, 1));
            K.Play(b, "ot", 1); Assert.AreEqual(900, K.Hp(b, 0), "고른 적 말고");
        }

        [Test] public void 계기_다른_아군이_약점으로_치면_hurt_비율_끈기_치유로_부상_탈출()
        {
            var d = K.Data(heroes: @"[{id:'w', name:'구경', role:'서포터', row:'mid', hp:1000, atk:80, def:40, passives:[
  {name:'박수', when:{on:'hit', who:'other', weak:true}, fx:[{k:'gauge', v:7}]},
  {name:'크게 아픔', when:{on:'hurt', pct:0.05}, fx:[{k:'gauge', v:11}]},
  {name:'버팀', when:{on:'endure'}, fx:[{k:'gauge', v:13}]},
  {name:'회복', when:{on:'unwound', who:'any'}, fx:[{k:'gauge', v:17}]}]}]",
                cards: "[{id:'wk', name:'급소', hero:'a', cost:0, type:'공격', tags:['약점 공격'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]}, {id:'hl', name:'치유', hero:'a', cost:0, type:'스킬', fx:[{k:'heal', ratio:3.0}]}]",
                enemies: "[{id:'big', name:'큰 놈', hp:1000, intents:[{t:'attack', v:120, rush:0}]}]");
            var b = K.Fight(d, new[] { "w", "a" }, new[] { "big" }); K.Hand(b, "wk", "hit");
            K.Play(b, "hit"); Assert.AreEqual(10, b.Gauge);
            K.Play(b, "wk"); Assert.AreEqual(17, b.Gauge, "약점으로 쳤을 때만");
            b.Gauge = 0; b.EndTurn(); Assert.AreEqual(11, b.Gauge, "120 ≥ 최대 HP 2000 × 5%");
            b.Gauge = 0; b.Pool.Hp = 500; K.Hand(b, "hl"); K.Play(b, "hl"); Assert.AreEqual(17, b.Gauge, "500 → 650 (30% 넘음)");
            b.Gauge = 0; b.Pool.Hp = 50; b.Pool.Status["끈기"] = 1; b.EndTurn(); Assert.AreEqual(1, b.Pool.Hp); Assert.AreEqual(13, b.Gauge, "끈기로 버팀(잃은 49 는 5% 아래)");
        }

        [Test] public void 조건_실드가_없으면_자기_공격_안냄_혼자만_파티에_있으면()
        {
            var d = K.Data(heroes: @"[{id:'s', name:'조건', role:'탱커', row:'front', hp:1000, atk:80, def:40, passives:[
  {name:'맨몸', when:{on:'turnEnd'}, conds:[{c:'guarded', not:true}], fx:[{k:'gauge', v:3}]},
  {name:'명상', when:{on:'turnEnd'}, conds:[{c:'ownNone', type:'공격'}], fx:[{k:'gauge', v:5}]},
  {name:'독무대', when:{on:'turnEnd'}, conds:[{c:'onlyMe'}], fx:[{k:'gauge', v:7}]},
  {name:'친구', when:{on:'turnEnd'}, conds:[{c:'ally', id:'a'}], fx:[{k:'gauge', v:11}]}]}]",
                cards: "[{id:'sz', name:'숨', hero:'s', cost:0, type:'스킬', fx:[]}]");
            var b = K.Fight(d, new[] { "s", "a" }, new[] { "dummy" }); K.Hand(b, "sz");
            K.Play(b, "sz"); b.EndTurn();
            Assert.AreEqual(3 + 5 + 7 + 11, b.Gauge);
        }

        [Test] public void 적_표식_건_사도만과_강인도_덤_적마다_제_겹()
        {
            var d = K.Data(heroes: "[{id:'m', name:'왕도', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'왕도', carrier:'enemy', cap:5, per:[{stat:'taken', v:0.2, from:'owner', else:-0.2}, {stat:'tough', v:1}]}}]",
                cards: "[{id:'mk', name:'찍기', hero:'m', cost:0, type:'스킬', fx:[{k:'stack', id:'왕도', v:1, target:'allEnemies'}]}, {id:'mh', name:'왕도 치기', hero:'m', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}, {id:'boom', name:'폭발', hero:'m', cost:0, type:'스킬', fx:[{k:'perStack', id:'왕도', each:true}, {k:'dmg', ratio:0.1, target:'allEnemies'}]}]");
            var b = K.Fight(d, new[] { "m", "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "mh", "mk", "mh", "hit", "boom");
            K.Play(b, "mh", 0); Assert.AreEqual(1000 - 80, K.Hp(b, 0), "표식 없는 적 — 그 사도 -20%");
            K.Play(b, "mk"); b.Ap = 3;
            K.Play(b, "mh", 0); Assert.AreEqual(920 - 120, K.Hp(b, 0));
            Assert.AreEqual(4 - 1.0 / 3 - (1.0 / 3 + 1), K.E(b, 0).Tough, 1e-9, "표식 1 → 강인도 +1");
            K.Play(b, "hit", 0); Assert.AreEqual(800 - 100, K.Hp(b, 0), "다른 사도에겐 안 든다");
            K.Hand(b, "boom"); K.E(b, 1).Status["왕도"] = 2;
            K.Play(b, "boom");
            Assert.AreEqual(700 - 12, K.Hp(b, 0), "적 0 — 겹 1 × 10% × (1 + 20%)"); Assert.AreEqual(1000 - 28, K.Hp(b, 1), "적 1 — 겹 2 × 10% × (1 + 40%)");
        }

        [Test] public void 태그_덧붙임_키워드와_공격이_퍼짐()
        {
            var d = K.Data(heroes: "[{id:'r', name:'로네', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'첩보원', cap:1, tagWhile:'약점 공격'}, keywords:[{name:'봉인 단계', cap:3, spread:true}]}]",
                cards: "[{id:'rh', name:'로네 치기', hero:'r', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}, {id:'spy', name:'변장', hero:'r', cost:0, type:'스킬', fx:[{k:'stack', id:'첩보원', v:1}, {k:'stack', id:'봉인 단계', v:1}]}]");
            var b = K.Fight(d, new[] { "r" }, new[] { "dummy", "dummy" }); K.Hand(b, "rh", "spy", "rh");
            K.Play(b, "rh", 0); Assert.AreEqual(900, K.Hp(b, 0)); Assert.AreEqual(900, K.Hp(b, 1), "봉인 0 — 적 전체로");
            K.Play(b, "spy"); K.Play(b, "rh", 0);
            Assert.AreEqual(900 - 125, K.Hp(b, 0), "첩보원 — 약점 공격"); Assert.AreEqual(900, K.Hp(b, 1));
        }

        [Test] public void 거르개_뽑기_꺼내기_꺼낸_카드_조건과_소멸_변환_종류()
        {
            var d = D(@"[{id:'dr', name:'내 카드', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:1, who:'self', type:'공격'}]},
 {id:'dig', name:'발굴', hero:'a', cost:0, type:'스킬', fx:[{k:'pull', from:'discard', basic:true}, {k:'ifPulled', basic:true}, {k:'exileFrom', from:'pulled'}, {k:'shield', ratio:1.0}]},
 {id:'frog', name:'개굴비', hero:'a', cost:0, type:'스킬', fx:[]}, {id:'cv', name:'덮기', hero:'a', cost:0, type:'스킬', fx:[{k:'transform', from:'@상태', id:'frog'}]},
 {id:'junk', name:'쓰레기', cost:0, type:'상태', tags:['사용 불가'], fx:[]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "dr", "dig", "cv", "junk");
            b.Draw.Clear(); b.Draw.AddRange(new[] { "hit", "hit_b", "guard" });
            K.Play(b, "dr"); CollectionAssert.Contains(b.Hand, "hit");
            b.Discard.Clear(); b.Discard.AddRange(new[] { "guard", "hit~" });
            K.Play(b, "dig"); CollectionAssert.Contains(b.Gone, "guard"); Assert.AreEqual(50, b.Pool.Shield);
            K.Play(b, "cv"); CollectionAssert.Contains(b.Hand, "frog~"); CollectionAssert.DoesNotContain(b.Hand, "junk");
        }

        [Test] public void 사도_표시_spend_와_저절로_내기_거르개_비율()
        {
            var d = K.Data(heroes: "[{id:'n', name:'나이아', role:'서포터', row:'mid', hp:600, atk:80, def:40, keywords:[{name:'물총', carrier:'hero', cap:1}], passives:[{name:'물벼락', when:{on:'play', marked:'물총'}, fx:[{k:'gauge', v:9}, {k:'spend', id:'물총', v:1}]}]}]",
                cards: "[{id:'wet', name:'쏘기', hero:'n', cost:0, type:'스킬', fx:[{k:'stack', id:'물총', v:1, target:'oneAlly'}]}, {id:'ap', name:'멋대로', hero:'n', cost:0, type:'스킬', fx:[{k:'autoPlay', who:'other', type:'공격', ratio:0.7}]}]");
            var b = K.Fight(d, new[] { "n", "a" }, new[] { "dummy" }); K.Hand(b, "wet", "zero", "zero", "ap", "guard", "hit");
            K.Play(b, "wet", 0, new PlayOpts { Ally = 1 });
            K.Play(b, "zero"); Assert.AreEqual(9, b.Gauge); Assert.AreEqual(0, b.StackOf("a", "물총"), "사도 표시도 spend");
            K.Play(b, "zero"); Assert.AreEqual(9, b.Gauge);
            K.Play(b, "ap"); Assert.AreEqual(930, K.Hp(b), "a 의 공격 카드만 · 70%"); CollectionAssert.Contains(b.Hand, "guard");
        }

        [Test] public void 그을림은_파티에도_파티가_카드를_낼_때마다()
        {
            var d = D("[{id:'st_ember', name:'불씨', cost:0, type:'상태', fx:[{k:'status', id:'그을림', v:2, target:'party'}]}, {id:'sw', name:'재빨리', hero:'a', cost:0, type:'스킬', tags:['신속'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.EnemyDmg = 2); K.Hand(b, "st_ember", "zero", "sw", "zero");
            K.Play(b, "st_ember");
            Assert.AreEqual(1000 - 32, b.Pool.Hp, "상태 카드도 카드 — 20 × 피해 배율 2 × 80%");
            K.Play(b, "sw"); Assert.AreEqual(968, b.Pool.Hp, "신속은 행동 카운트를 안 줄인다");
            K.Play(b, "zero"); Assert.AreEqual(936, b.Pool.Hp); Assert.AreEqual(0, b.St(b.Pool, "그을림"));
            b.Pool.Status["그을림"] = 3; b.EndTurn(); Assert.AreEqual(0, b.St(b.Pool, "그을림"), "턴 끝에 사라진다");
        }

        [Test] public void 함정이_빗나가면_else_와_혼란_확률_지운_수_일의_값()
        {
            var d = D(@"[{id:'tp', name:'덫', hero:'a', cost:0, type:'스킬', fx:[{k:'trap', target:'oneEnemy', then:[{k:'gauge', v:50}], else:[{k:'gauge', v:20}]}]},
 {id:'ds', name:'지우기', hero:'a', cost:0, type:'스킬', fx:[{k:'dispel', n:3, target:'oneEnemy'}, {k:'perEvent'}, {k:'shield', ratio:0.2}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "tp", "ds");
            K.Play(b, "tp"); b.EndTurn(); Assert.AreEqual(20, b.Gauge, "공격이 아니라 else");
            K.E(b).Status["사기"] = 2; K.Hand(b, "ds"); K.Play(b, "ds");
            Assert.AreEqual(20, b.Pool.Shield, "지운 2 × 방어력 20%");
        }

        [Test] public void 조건_꺼낸_종류_결속_마지막_카드_손에_다른_카드_종류_처음()
        {
            var d = D(@"[{id:'tn', name:'처음', hero:'a', cost:0, type:'스킬', fx:[{k:'ifTypeNew'}, {k:'gauge', v:3}]},
 {id:'ih', name:'짝', hero:'a', cost:0, type:'스킬', fx:[{k:'ifInHand', id:'ih'}, {k:'gauge', v:5}]},
 {id:'ps', name:'같은 종류', hero:'a', cost:0, type:'스킬', fx:[{k:'ifPrevSame'}, {k:'gauge', v:7}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "tn", "ih", "ih", "ps", "tn");
            K.Play(b, "tn"); Assert.AreEqual(3, b.Gauge);
            K.Play(b, "ih"); Assert.AreEqual(8, b.Gauge, "손에 다른 짝");
            K.Play(b, "ih"); Assert.AreEqual(8, b.Gauge);
            K.Play(b, "ps"); Assert.AreEqual(15, b.Gauge);
            K.Play(b, "tn"); Assert.AreEqual(15, b.Gauge, "스킬은 이미 나왔다");
        }

        [Test] public void 새_시뮬_혼자_완주율_역할_조합_짝()
        {
            var d = K.Sample();
            var solo = MetaSim.Solo(d, err: 50, min: 2, max: 2, threads: 2, heroes: new List<string> { "rico", "erpin" });
            Assert.AreEqual(2, solo.Count); Assert.IsTrue(solo.All(r => r.N == 2));
            var ct = MetaSim.CompTable(d, 1, threads: 2);
            Assert.AreEqual(10, ct.Count); Assert.AreEqual(1, ct["TSD"].n);
            Assert.Greater(MetaSim.Mesh(d, d.Hero("erpin"), d.Hero("rico")) + MetaSim.Mesh(d, d.Hero("erpin"), d.Hero("sion")), -1);
            StringAssert.Contains("## 역할 조합", MetaSim.MarkdownV2(d, MetaSim.Run(d, 1, threads: 2), ct, null, "시험"));
        }

[Test] public void 신탁은_어디서든_무작위_셋_가운데_하나_축복_확률()
        {
            var d = K.Sample();
            int blessed = 0, offers = 0;
            for (int s = 1; s <= 400; s++)
            {
                var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, s);
                run.S.Deck.Add("rico_u1");
                var o = run.OfferOf("rico_u1");
                Assert.AreEqual(R.ORACLE_PICKS, o.Picks.Count); Assert.AreEqual(o.Picks.Count, o.Picks.Distinct().Count());
                offers++; if (o.Shins.Any(x => x != null)) { blessed++; Assert.AreEqual(1, o.Shins.Count(x => x != null), "축복은 셋 가운데 하나에만"); }
                var opts = run.FlashOptions(o);
                Assert.AreEqual(3, opts.Count); Assert.IsTrue(opts.All(x => x.Text.Length > 0));
                Assert.AreEqual(o.Shins.Count(x => x != null), opts.Count(x => x.Blessed));
            }
            Assert.AreEqual(0.15, (double)blessed / offers, 0.05, "축복 15% 쯤");
            var sw = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 9); sw.S.Deck.Add("rico_u1"); sw.S.Flash["rico_u1"] = 2;
            CollectionAssert.DoesNotContain(sw.OfferOf("rico_u1", 2, true).Picks, 2, "바꾸기는 지금 신탁을 빼고 셋");
        }

        [Test] public void 고른_후보의_축복이_판에_붙고_전투_후보도_같은_꼴()
        {
            var d = K.Sample();
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 1); run.S.Deck.Add("rico_u1");
            var offer = new FlashOffer { CardId = "rico_u1", Picks = new List<int> { 1, 3, 5 }, Shins = new List<string> { null, "own", null } };
            run.S.Camp = new CampState { Key = "k", Train = offer }; run.S.Stops["k"] = "";
            Assert.IsNull(run.CampTrain(3));
            Assert.AreEqual(3, run.S.Flash["rico_u1"]); Assert.AreEqual("own", run.S.Shin["rico_u1"]);
            var (b, _) = run.OpenFight();
            b.Glow["rico_u1"] = new Glow { Kind = "card", Picks = new List<GlowPick> { new GlowPick { N = 2 }, new GlowPick { N = 4, Shin = "power" }, new GlowPick { N = 5 } } };
            var eo = b.EpiphanyOptions("rico_u1");
            Assert.AreEqual(new[] { 2, 4, 5 }, eo.Select(x => x.N).ToArray()); Assert.IsTrue(eo[1].Blessed); StringAssert.Contains("1.3", eo[1].BlessText);
            var bad = Validator.Check(K.Data(events: "[{id:'e', name:'이벤트', options:[{label:'고르기', out:[{k:'flash', all:true}]}]}]"));
            StringAssert.Contains("all(다섯 중 고르기)은 없앴다", string.Join("\n", bad.Errors));
        }

        [Test] public void 검사기와_글()
        {
            var d = D("[{id:'bad', name:'잘못', hero:'a', cost:0, type:'스킬', fx:[{k:'ifRoll', n:1}, {k:'gauge', v:1}, {k:'cardStatus', id:'독', v:-1}]}, {id:'ok', name:'좋음', hero:'a', cost:0, type:'스킬', fx:[{k:'costMod', v:-1, who:'self', type:'공격', turns:2}, {k:'cutHit', v:0.3}]}]");
            var all = string.Join("\n", Validator.Check(d).Errors);
            StringAssert.Contains("앞에 roll", all); StringAssert.Contains("음수로 걸 수 없다", all);
            Assert.AreEqual("2턴간 자신의 공격 카드 비용 -1, 적의 다음 공격 한 대 피해 -30%", new CardText(d).Card(d.Card("ok")));
        }
    }
}
