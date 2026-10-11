using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 엘다인 3차(2026-10-11 카제나 재정렬, Docs/데이터.md §23) — 반격 모양 · 반격 계기 · 반격 피해 증감 · 나눠 입히기 · 붙은 동안 보존 · 가장 비싼 카드 · AP 안전 상한 ·
    /// 손에서 반응하는 카드 · 게이지 잠금 · 적마다 깎기 · 가장 큰 예고 · 카드 기록 · 재현 · ifEvent · tally · 검사기(▶E62).
    /// </summary>
    public class Eldain3Tests
    {
        const string HEROES = @"[
 {id:'bb', name:'비', role:'탱커', hp:800, atk:100, def:50, crit:0,
  keyword:{name:'상자', cap:99, rules:[{name:'상자', when:{on:'counter'}, fx:[{k:'stack', id:'상자', v:1}]}]}},
 {id:'cl', name:'클', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'몸집', cap:9}},
 {id:'jo', name:'조', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'경전', cap:9}},
 {id:'ep', name:'에', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'열기', cap:9, tally:['play', 'discardBy'], endClear:true}},
 {id:'be', name:'벨', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'불꽃', cap:99}}
]";
        const string CARDS = @"[
 {id:'ctr', name:'반격 걸기', hero:'bb', cost:0, type:'스킬', fx:[{k:'status', id:'반격', v:1}]},
 {id:'spear', name:'창 네 자루', hero:'bb', cost:0, type:'스킬', fx:[{k:'counterHits', v:2, ratio:0.8}]},
 {id:'spearAll', name:'적 전체 창', hero:'bb', cost:0, type:'스킬', fx:[{k:'counterAll'}]},
 {id:'box', name:'상자 열기', hero:'bb', cost:0, type:'스킬', fx:[{k:'counterMod', v:1, turns:1}]},
 {id:'runway', name:'런웨이', hero:'cl', cost:0, type:'스킬', fx:[{k:'spend', id:'몸집', pick:true}, {k:'cardStatus', id:'탐구심', v:1, to:'hand', who:'other', pick:true}]},
 {id:'suit', name:'맞춤 의상', hero:'cl', cost:0, type:'스킬', fx:[{k:'cardStatus', id:'의상', v:1, to:'hand', who:'other', choose:'priciest', keep:true}]},
 {id:'tattle', name:'이른다', hero:'cl', cost:0, type:'스킬', fx:[{k:'costMod', v:-1, choose:'priciest', who:'other'}]},
 {id:'apx', name:'AP 잔뜩', hero:'cl', cost:0, type:'스킬', fx:[{k:'ap', v:5}]},
 {id:'clover', name:'세잎클로버', hero:'cl', cost:1, type:'스킬', tags:['보존'], fx:[{k:'shield', ratio:1}, {k:'when', on:'handHurtBig', pct:0.15}, {k:'gauge', v:20}]},
 {id:'lock', name:'유희', hero:'cl', cost:1, type:'스킬', fx:[{k:'gaugeLock'}]},
 {id:'lockhit', name:'유희 뒤', hero:'cl', cost:2, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'flame', name:'빼앗기', hero:'be', cost:0, type:'스킬', fx:[{k:'cutHit', v:0.5, target:'allEnemies'}, {k:'perEvent', per:20}, {k:'stack', id:'불꽃', v:1}]},
 {id:'bighit', name:'큰 놈 치기', hero:'be', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'bigHitEnemy'}]},
 {id:'note', name:'받아 적기', hero:'jo', cost:0, type:'스킬', fx:[{k:'record'}]},
 {id:'read', name:'낭독', hero:'jo', cost:0, type:'공격', fx:[{k:'replay', v:1, ratio:0.6}]},
 {id:'read0', name:'빈 경전', hero:'jo', cost:0, type:'공격', fx:[{k:'ifPile', from:'record', n:1, not:true}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'mark', name:'명검 벼리기', hero:'jo', cost:0, type:'스킬', fx:[{k:'cardStatus', id:'명검', v:1, to:'hand', tag:'big'}]},
 {id:'sweep', name:'탄 모두', hero:'ep', cost:0, type:'공격', fx:[{k:'discard', all:true, tag:'zero'}, {k:'ifEvent', n:2}, {k:'stack', id:'열기', v:5}]},
 {id:'ep_hit', name:'에 치기', hero:'ep', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]}
]";
        const string FOES = @"[{id:'smash', name:'박살', hp:1000, intents:[{t:'attack', v:400, rush:0}]}, {id:'poke', name:'콕', hp:1000, intents:[{t:'attack', v:100, rush:0}]}]";
        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(string[] party, params string[] foes) => K.Fight(D(), party, foes.Length > 0 ? foes : new[] { "dummy" });

        // ── E38 · E39 · E40 반격 ──
        [Test] public void E38_E39_반격을_나눠_치면_한_대마다_계기()
        {
            var b = F(new[] { "bb" }, "poke");
            K.Hand(b, "ctr", "spear"); K.Play(b, "ctr"); K.Play(b, "spear");
            int hp0 = K.Hp(b);
            b.EndTurn();
            Assert.AreEqual(2, b.StackOf("bb", "상자"), "반격 한 번이 두 대 — 계기도 두 번");
            Assert.Less(K.Hp(b), hp0);
        }

        [Test] public void E38_반격이_적_전체로()
        {
            var b = F(new[] { "bb" }, "poke", "dummy");
            K.Hand(b, "ctr", "spearAll"); K.Play(b, "ctr"); K.Play(b, "spearAll");
            b.EndTurn();
            Assert.Less(K.Hp(b, 1), 1000, "때리지 않은 적도 반격에 맞는다");
            Assert.AreEqual(2, b.StackOf("bb", "상자"));
        }

        [Test] public void E40_반격_피해_증감()
        {
            int Lost(bool boost)
            {
                var b = F(new[] { "bb" }, "poke");
                K.Hand(b, "ctr", "box"); K.Play(b, "ctr"); if (boost) K.Play(b, "box");
                b.EndTurn();
                return 1000 - K.Hp(b);
            }
            int a = Lost(false), x = Lost(true);
            Assert.Greater(a, 0);
            Assert.AreEqual(a * 2, x, 2, "이번 턴 반격 피해 ×2");
        }

        // ── E42 · E43 · E45 · E46 ──
        [Test] public void E42_고른_만큼_나눠_입힌다()
        {
            var b = F(new[] { "cl", "a" });
            b.Stacks["cl"] = new Dictionary<string, int> { ["몸집"] = 3 };
            K.Hand(b, "runway", "big", "hit");
            K.Play(b, "runway", 0, new PlayOpts { Spend = 3 });
            Assert.AreEqual(2, b.CardStOf("big", "탐구심"), "비싼 카드부터 돌아가며 — 2");
            Assert.AreEqual(1, b.CardStOf("hit", "탐구심"));
            var b2 = F(new[] { "cl", "a" });
            b2.Stacks["cl"] = new Dictionary<string, int> { ["몸집"] = 3 };
            K.Hand(b2, "runway", "big", "hit");
            CollectionAssert.AreEquivalent(new[] { "big", "hit" }, b2.SpreadCands(0));
            K.Play(b2, "runway", 0, new PlayOpts { Spend = 3, Spread = new List<string> { "hit", "hit", "hit" } });
            Assert.AreEqual(3, b2.CardStOf("hit", "탐구심"), "고른 대로 한 카드에 몰기");
        }

        [Test] public void E43_E45_붙은_동안_보존과_가장_비싼_카드()
        {
            var b = F(new[] { "cl", "a" });
            K.Hand(b, "suit", "tattle", "big", "hit"); b.Ap = 5;
            K.Play(b, "suit");
            Assert.AreEqual(1, b.CardStOf("big", "의상"), "가장 비싼 아군 카드에");
            Assert.IsTrue(b.HasTagB("big", Tag.Keep), "붙은 동안 보존");
            K.Play(b, "tattle");
            Assert.AreEqual(1, b.CostOf("big"), "가장 비싼 카드 비용 -1");
            b.EndTurn();
            Assert.Contains("big", b.Hand, "보존 — 손에 남는다");
            b.Ap = 5; K.Play(b, "big");
            Assert.AreEqual(0, b.CardStOf("big", "의상"), "낸 뒤 벗는다");
        }

        [Test] public void E46_효과로_얻는_AP_는_한_턴_3()
        {
            var b = F(new[] { "cl" });
            K.Hand(b, "apx", "apx"); int ap0 = b.Ap;
            K.Play(b, "apx");
            Assert.AreEqual(ap0 + Battle.AP_GAIN_CAP, b.Ap);
            K.Play(b, "apx");
            Assert.AreEqual(ap0 + Battle.AP_GAIN_CAP, b.Ap, "한 턴 상한");
        }

        // ── E48 · E51 ──
        [Test] public void E48_손에서_큰_한_대를_막고_소멸()
        {
            var b = F(new[] { "cl" }, "smash");
            K.Hand(b, "clover"); int hp0 = b.Pool.Hp, g0 = b.Gauge;
            b.EndTurn();
            int cap = (int)System.Math.Round(b.Pool.MaxHp * 0.15);
            Assert.AreEqual(hp0 - cap, b.Pool.Hp, "그 대를 15% 로 깎는다");
            Assert.Contains("clover", b.Gone, "저절로 소멸");
            Assert.GreaterOrEqual(b.Gauge, g0 + 20, "뒤 효과(게이지 +20)");
            StringAssert.Contains("손에 있을 때 한 대에 HP 15% 넘게 잃으려 하면 15%로 막고 소멸", new CardText(b.Data).Card(b.CardOf("clover")));
        }

        [Test] public void E51_게이지_잠금()
        {
            var b = F(new[] { "cl" });
            K.Hand(b, "lock", "lockhit"); b.Ap = 5;
            K.Play(b, "lock"); int g0 = b.Gauge;   // 잠금 카드 자신의 AP 몫은 잠기기 전에 든다
            K.Play(b, "lockhit");
            Assert.AreEqual(g0, b.Gauge, "자신의 카드로 게이지가 오르지 않는다");
        }

        // ── E54 · E55 · E56 ──
        [Test] public void E54_E55_적마다_깎고_깎은_양이_일의_값()
        {
            var b = F(new[] { "be" }, "smash", "smash");
            int ev = (b.IntentHit(K.E(b, 0)) ?? 0) + (b.IntentHit(K.E(b, 1)) ?? 0);
            K.Hand(b, "flame"); K.Play(b, "flame");
            Assert.AreEqual((int)System.Math.Round(ev * 0.5) / 20, b.StackOf("be", "불꽃"), "깎은 양 20당 1");
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            Assert.AreEqual(hp0 - (int)System.Math.Round(ev * 0.5), b.Pool.Hp, 2, "두 적 모두 한 대 -50%");
        }

        [Test] public void E56_공격_예고가_가장_큰_적()
        {
            var b = F(new[] { "be" }, "poke", "smash");
            K.Hand(b, "bighit"); K.Play(b, "bighit", 0);
            Assert.AreEqual(1000, K.Hp(b, 0)); Assert.Less(K.Hp(b, 1), 1000);
        }

        // ── E57 기록 · 재현 ──
        [Test] public void E57_받아_적고_낭독한다()
        {
            var b = F(new[] { "c", "jo" });
            K.Hand(b, "read0"); K.Play(b, "read0");
            Assert.AreEqual(1000 - 10, K.Hp(b), "쪽이 없으면 — 10%");
            K.Hand(b, "hit_c", "note"); b.Ap = 3;
            K.Play(b, "hit_c"); int hp1 = K.Hp(b);
            K.Play(b, "note");
            CollectionAssert.AreEqual(new[] { "hit_c" }, b.RecordsOf("jo"));
            K.Hand(b, "read"); K.Play(b, "read");
            Assert.AreEqual(hp1 - 90, K.Hp(b), "다 때리기(150) × 60% — 그 주인 공격력으로");
            Assert.AreEqual(0, b.RecordsOf("jo").Count, "쪽을 지운다");
            var l = Battle.Load(b.Data, b.Save());
            Assert.AreEqual(b.RecentPlays.Count, l.RecentPlays.Count, "저장 왕복");
            StringAssert.Contains("받아 적은 첫 쪽을 낭독(60%)", new CardText(b.Data).Card(b.CardOf("read")));
        }

        [Test] public void E57_낭독은_받아_적기를_다시_깨우지_않는다()
        {
            var b = F(new[] { "c", "jo" });
            K.Hand(b, "note", "hit_c", "note"); b.Ap = 3;
            K.Play(b, "hit_c"); K.Play(b, "note"); K.Play(b, "note");
            Assert.AreEqual(1, b.RecordsOf("jo").Count, "받아 적기 카드 자신 · 같은 사도 카드는 적지 않는다");
        }

        // ── E60 · ifEvent · tally ──
        [Test] public void E60_손에_없으면_더미의_그_카드에()
        {
            var b = F(new[] { "a", "jo" });
            b.Draw.Add("big");
            K.Hand(b, "mark"); K.Play(b, "mark");
            Assert.AreEqual(1, b.CardStOf("big", "명검"));
        }

        [Test] public void ifEvent_와_tally()
        {
            var b = F(new[] { "a", "ep" });
            K.Hand(b, "sweep", "zero", "zero", "hit");
            K.Play(b, "sweep");
            // tally — 에가 카드를 냄 +1 · 에의 효과로 버려짐 +2, ifEvent(버린 2장 ≥ 2) +5
            Assert.AreEqual(1 + 2 + 5, b.StackOf("ep", "열기"));
            CollectionAssert.AreEqual(new[] { "hit" }, b.Hand);
            b.EndTurn();
            Assert.AreEqual(0, b.StackOf("ep", "열기"), "턴 끝 0");
            StringAssert.Contains("자신이 카드를 내거나 자신의 효과로 카드가 버려질 때마다", new CardText(b.Data).Keyword(b.Data.Hero("ep").Keyword));
        }

        // ── E62 검사기 ──
        [Test] public void E62_규칙_하나_글_45자_AP_신탁()
        {
            var two = "{name:'하나', when:{on:'play'}, fx:[{k:'stack', id:'이드의 마음', v:1}]}, {name:'둘', when:{on:'turnStart'}, fx:[{k:'stack', id:'이드의 마음', v:1}]}";
            Assert.IsFalse(EldainTests.EldErrors(EldainTests.Eld("이드", rules: two)).Any(e => e.Contains("규칙은 사도당")), "둘까지는 된다(사용자 2026-10-11)");
            var three = two + ", {name:'셋', when:{on:'turnEnd'}, fx:[{k:'stack', id:'이드의 마음', v:1}]}";
            EldainTests.Has(EldainTests.EldErrors(EldainTests.Eld("이드", rules: three)), "고유 능력 규칙은 사도당 2개 이하");
            var tally = EldainTests.Eld("이드"); tally.hero = tally.hero.Replace("cap:5, rules:[]", "cap:5, tally:['play'], rules:[]");
            Assert.IsFalse(EldainTests.EldErrors(tally).Any(e => e.Contains("규칙은 사도당")), "tally 는 규칙이 아니다");
            string longFx = string.Join(", ", Enumerable.Range(0, 6).Select(i => "{k:'dmg', ratio:1.1, target:'allEnemies'}, {k:'status', id:'취약', v:1, target:'allEnemies'}"));
            EldainTests.Has(EldainTests.EldErrors(EldainTests.Eld("이드", uniqueFx: longFx)), "공백 뺀 45자 이하");
            // AP 신탁 셋 · 0코 AP 신탁에 소멸 없음
            var ap = EldainTests.Eld("이드");
            for (int u = 1; u <= 3; u++) ap.cards = ap.cards.Replace($"{{name:'이드{u}o1', fx:[", $"{{name:'이드{u}o1', cost:0, fx:[{{k:'ap', v:1}}, ");
            var errs = EldainTests.EldErrors(ap);
            EldainTests.Has(errs, "AP 를 주는 신탁은 사도당 2개 이하");
            EldainTests.Has(errs, "0코 + AP 신탁은 「소멸」");
        }
    }
}
