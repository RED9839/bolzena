using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>짝을 잇는 계기 · 적 · 이벤트(NEEDS.md C1 · C3 · C7 · W1 · W2) — 그리고 봇이 운영 방식을 쓰는가.</summary>
    public class PairTriggerTests
    {
        const string TWO = @"[
 {id:'p', name:'주인', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'주머니', cap:9}, passives:[PASSIVE]},
 {id:'q', name:'짝', role:'탱커', row:'front', hp:900, atk:200, def:60, keyword:{name:'짝주머니', cap:9}}]";
        const string CARDS = @"[
 {id:'pz', name:'주인 숨', hero:'p', cost:0, type:'스킬', fx:[]},
 {id:'qz', name:'짝 숨', hero:'q', cost:0, type:'공격', fx:[]},
 {id:'qspend', name:'짝 털기', hero:'q', cost:0, type:'스킬', fx:[{k:'stack', id:'짝주머니', v:3}, {k:'spend', id:'짝주머니', v:2}]},
 {id:'feed', name:'나눠 주기', hero:'p', cost:0, type:'스킬', fx:[{k:'feed', v:2}]}]";
        static Battle Two(string passive, string extraCards = null)
        {
            var d = K.Data(heroes: TWO.Replace("PASSIVE", passive), cards: CARDS).Add(null, extraCards, null, null, null, null);
            return K.Fight(d, new[] { "p", "q" }, new[] { "dummy" });
        }

        [Test] public void 다른_아군이_낼_때만()
        {
            var b = Two("{name:'거들기', when:{on:'play', who:'other'}, fx:[{k:'gauge', v:5}]}"); K.Hand(b, "pz", "qz");
            K.Play(b, "pz"); Assert.AreEqual(0, b.Gauge);
            K.Play(b, "qz"); Assert.AreEqual(5, b.Gauge);
        }

        [Test] public void 카드를_낸_아군이_패시브의_아군_1명()
        {
            var b = Two("{name:'따라붙기', when:{on:'play', who:'other'}, fx:[{k:'atkMod', v:0.5, target:'oneAlly'}]}"); K.Hand(b, "qz");
            K.Play(b, "qz");
            Assert.AreEqual(0.5, b.StatMod(b.HeroUnit("q"), "atk"), 1e-9);
            Assert.AreEqual(0, b.StatMod(b.HeroUnit("p"), "atk"), 1e-9);
        }

        [Test] public void 아군이_키워드를_소모하면_N개_이상()
        {
            var b = Two("{name:'같이 놀자', when:{on:'spend', who:'any', n:2}, fx:[{k:'stack', id:'주머니', v:1}]}"); K.Hand(b, "qspend");
            K.Play(b, "qspend");
            Assert.AreEqual(1, b.StackOf("p", "주머니"));
            Assert.AreEqual(1, b.StackOf("q", "짝주머니"));
        }

        [Test] public void 아군_키워드_채우기()
        {
            var b = Two("{name:'없음', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}"); K.Hand(b, "feed");
            K.Play(b, "feed");
            Assert.AreEqual(2, b.StackOf("p", "주머니"));
            Assert.AreEqual(2, b.StackOf("q", "짝주머니"));
        }

        [Test] public void 연계가_저절로_나가면()
        {
            var b = Two("{name:'끼어들기 사슬', when:{on:'link', who:'any'}, fx:[{k:'gauge', v:9}]}", "[{id:'qlink', name:'짝 연계', hero:'q', cost:1, type:'스킬', tags:['연계'], fx:[]}]");
            K.Hand(b, "qlink", "pz");
            K.Play(b, "pz");
            Assert.AreEqual(9, b.Gauge);
        }

        [Test] public void 치명타가_터지면()
        {
            var b = Two("{name:'은총', when:{on:'crit', who:'any'}, fx:[{k:'gauge', v:4}]}", "[{id:'crit', name:'급소', hero:'q', cost:0, type:'공격', fx:[{k:'critMod', v:1.0, target:'self'}, {k:'dmg', ratio:0.1, hits:3, target:'oneEnemy'}]}]");
            K.Hand(b, "crit");
            K.Play(b, "crit");
            Assert.AreEqual(4, b.Gauge, "카드 한 장에 한 번");
        }

        [Test] public void 공격을_받으면_다_막아도()
        {
            var d = K.Data(heroes: "[{id:'t', name:'방패', role:'탱커', row:'front', hp:1000, atk:80, def:60, passives:[{name:'맞받기', when:{on:'hurt', guarded:true}, fx:[{k:'gauge', v:3}]}]}]");
            var b = K.Fight(d, new[] { "t" }, new[] { "hitter" });
            b.Pool.Block = 999;
            b.EndTurn();
            Assert.AreEqual(3, b.Gauge);
        }

        [Test] public void HP_조건과_남은_AP_비례()
        {
            var d = K.Data(cards: "[{id:'low', name:'버팀', hero:'a', cost:0, type:'스킬', fx:[{k:'ifHp', pct:0.5}, {k:'gauge', v:20}]}, {id:'lazy', name:'낮잠', hero:'a', cost:1, type:'스킬', fx:[{k:'perApLeft'}, {k:'shield', ratio:0.5}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "low", "low", "lazy");
            K.Play(b, "low"); Assert.AreEqual(0, b.Gauge);
            b.Pool.Hp = 400;
            K.Play(b, "low"); Assert.AreEqual(20, b.Gauge);
            K.Play(b, "lazy");
            Assert.AreEqual(50, b.Pool.Shield, "남은 AP 2 × 방어력 50 × 50%");
        }

        // ── 적 ──
        [Test] public void 소환_조건부_수_특정_동료가_쓰러지면()
        {
            var d = K.Data(enemies: @"[
 {id:'caller', name:'부르는 놈', hp:1000, intents:[{t:'summon', id:'dummy', n:1, if:{allies:3}, rush:0}, {t:'summon', id:'minion', n:2, rush:0}],
   passives:[{name:'짝 잃음', on:'allyDown', who:'minion', do:{t:'buff', id:'사기', v:1}}]},
 {id:'minion', name:'졸개', hp:100, intents:[{t:'jam', v:0, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "caller" });
            Assert.AreEqual("minion", K.E(b).Intent.Id, "적이 셋이 안 되어 첫 수를 건너뛴다");
            b.EndTurn();
            Assert.AreEqual(3, b.AliveEnemies().Count);
            Assert.AreEqual(100, b.Enemies[1].MaxHp, "체력 배율 1 그대로");
            Assert.AreEqual("dummy", K.E(b).Intent.Id, "이제 셋 — 첫 수");
            K.Hand(b, "hit"); b.Enemies[1].Hp = 1;
            K.Play(b, "hit", 1);
            Assert.AreEqual(1, b.St(K.E(b), "사기"), "졸개가 쓰러지면");
        }

        // ── 이벤트 ──
        [Test] public void 시작_카드만_빼기()
        {
            var run = Run.New(K.Sample(), new List<string> { "rico", "carrot", "sion" }, 1);
            run.S.Deck.Add("rico_u1");
            run.S.Event = new EventState { Key = "t" };
            run.ApplyOutcomes(new List<Outcome> { new Outcome { K = "remove", N = 1, Basic = true } });
            Assert.IsNotNull(run.ResolvePending("rico_u1"));
            Assert.IsNull(run.ResolvePending("rico_s1"));
        }

        // ── 봇 ──
        [Test] public void 봇은_AP_가_남았으면_패시브를_보고_AP_를_남긴다()
        {
            var d = K.Data(heroes: "[{id:'lazy', name:'게으름', role:'서포터', row:'mid', hp:800, atk:80, def:50, keyword:{name:'낮잠', cap:9}, passives:[{name:'여유', when:{on:'turnEnd'}, conds:[{c:'apLeft', n:3}], fx:[{k:'stack', id:'낮잠', v:3}, {k:'shield', ratio:4.0}]}]}]",
                cards: "[{id:'meh', name:'그저 그런', hero:'lazy', cost:1, type:'스킬', fx:[{k:'block', ratio:0.3}]}]");
            var b = K.Fight(d, new[] { "lazy" }, new[] { "dummy" }); K.Hand(b, "meh", "meh");
            new Bots(d).SmartPlay(b);
            Assert.AreEqual(3, b.Ap, "약한 카드를 내느니 AP 3 을 남긴다");
        }

        [Test] public void 봇은_버릴_카드로_상태_카드와_안식_카드를_고른다()
        {
            var d = K.Data(cards: "[{id:'rest', name:'안식', hero:'a', cost:2, type:'스킬', fx:[{k:'when', on:'discard'}, {k:'gauge', v:60}]}, {id:'toss', name:'버리기', hero:'a', cost:0, type:'스킬', fx:[{k:'discard', v:1}, {k:'draw', v:1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "toss", "hit", "rest");
            var m = new Bots(d).Moves(b).First(x => x.Id == "toss");
            CollectionAssert.AreEqual(new[] { "rest" }, m.Discard);
        }

        [Test] public void 봇은_사도_카드의_턴_끝_마디를_벌점으로_보지_않는다()
        {
            var d = K.Data(cards: "[{id:'aged', name:'묵히기', hero:'a', cost:1, type:'스킬', tags:['보존'], fx:[{k:'block', ratio:0.2}, {k:'when', on:'handEnd'}, {k:'gauge', v:30}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "aged");
            var bots = new Bots(d);
            var hold = bots.Score(b, false);
            K.Play(b, "aged");
            Assert.Greater(hold, bots.Score(b, false), "쥐고 넘기는 쪽이 낫다");
        }

        [Test] public void 봇은_아군_1명을_사도마다_둬_본다()
        {
            var d = K.Data(cards: "[{id:'boost', name:'북돋기', hero:'a', cost:1, type:'스킬', fx:[{k:'atkMod', v:0.3, run:true, target:'oneAlly'}]}]");
            var b = K.Fight(d, new[] { "a", "b", "c" }, new[] { "dummy" }); K.Hand(b, "boost");
            var ts = new Bots(d).Moves(b).Where(x => x.Id == "boost").Select(x => x.T).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, ts);
        }

        [Test] public void 봇은_디버프_세기_카드를_가짓수가_가장_많은_적에게()
        {
            var d = K.Data(cards: "[{id:'flaw', name:'흠', hero:'a', cost:1, type:'공격', fx:[{k:'perDebuff'}, {k:'dmg', ratio:0.5, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "dummy", "dummy" }); K.Hand(b, "flaw");
            b.Enemies[1].Status["취약"] = 1; b.Enemies[1].Status["약화"] = 1; b.Enemies[2].Status["취약"] = 1;
            CollectionAssert.AreEqual(new[] { 1 }, new Bots(d).Moves(b).Where(x => x.Id == "flaw").Select(x => x.T));
        }

        [Test] public void 봇은_HP_40퍼센트_밑에서_HP를_치르지_않는다()
        {
            var d = K.Data(cards: "[{id:'blood', name:'피값', hero:'a', cost:0, type:'공격', fx:[{k:'payHp', v:50}, {k:'dmg', ratio:3.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "blood");
            Assert.IsTrue(new Bots(d).Moves(b).Any(x => x.Id == "blood"));
            b.Pool.Hp = 300;
            Assert.IsFalse(new Bots(d).Moves(b).Any(x => x.Id == "blood"));
        }

        [Test] public void 글()
        {
            var t = new CardText(K.Data());
            Assert.AreEqual("다른 아군이 카드를 낼 때마다", t.WhenText(new When { On = "play", Who = "other" }));
            Assert.AreEqual("아군이 키워드 2개 이상을 소모하면", t.WhenText(new When { On = "spend", Who = "any", N = 2 }));
            Assert.AreEqual("아군의 연계 카드가 저절로 나가면", t.WhenText(new When { On = "link", Who = "any" }));
            Assert.AreEqual("사도마다 고유 효과 +1", t.Fx(new List<Fx> { new Fx { K = "feed", V = 1 } }));
            Assert.AreEqual("파티 HP가 50% 이하이면 → 드로우 1", t.Fx(new List<Fx> { new Fx { K = "ifHp", Pct = 0.5 }, new Fx { K = "draw", V = 1 } }));
            Assert.AreEqual("졸개 2 부르기 (적이 3명 이상일 때)", new CardText(K.Data(enemies: "[{id:'m', name:'졸개', hp:1, intents:[{t:'jam', v:1}]}]")).Intent(new Intent { T = "summon", Id = "m", N = 2, If = new IntentIf { Allies = 3 } }));
        }
    }
}
