using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 18갈래 2단계 — 시범 16 에 넣은 엔진 부품(2026-10-08).
    /// 예약 당겨 쓰기 · 남은 칸 비례(ripen) · 아군에게 심은 예약의 재촉 · ifStreak 종류 거르기 · 직전보다 비싼가(ifPricier · 조건 pricier) ·
    /// 이번 전투 소멸한 자신의 카드(perGone who self) · 처치한 적 등급(ifKill elite · boss) · 이번 전투에 쌓은 양(ifGained · 조건 gained) ·
    /// 소환물 행동(summon · summonAct) · 한 카드 상한 · 소환물 uses · perCardSt 최소.
    /// </summary>
    public class TrialSixteenTests
    {
        const string HEROES = @"[
 {id:'p', name:'주인', role:'딜러', hp:600, atk:100, def:20, keyword:KW, keywords:[KWS], passives:[PASSIVE]},
 {id:'q', name:'짝', role:'탱커', hp:900, atk:100, def:60, keyword:{name:'짝주머니', cap:9}}]";
        const string CARDS = @"[
 {id:'c0', name:'영코', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'c1', name:'일코', hero:'p', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'c2', name:'이코', hero:'p', cost:2, type:'스킬', fx:[{k:'ifPricier'}, {k:'stack', id:'주머니', v:5}]},
 {id:'pk', name:'주인 스킬', hero:'p', cost:0, type:'스킬', fx:[{k:'draw', v:0}]},
 {id:'st3', name:'셋째 칼', hero:'p', cost:0, type:'공격', fx:[{k:'ifStreak', n:3, type:'공격'}, {k:'stack', id:'주머니', v:1}]},
 {id:'qhit', name:'짝 치기', hero:'q', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'ex', name:'태울 것', hero:'p', cost:0, type:'스킬', tags:['소멸'], fx:[]},
 {id:'qex', name:'짝 태울 것', hero:'q', cost:0, type:'스킬', tags:['소멸'], fx:[]},
 {id:'gone', name:'소멸 비례', hero:'p', cost:0, type:'공격', fx:[{k:'perGone', who:'self'}, {k:'dmg', ratio:1, target:'oneEnemy', fixed:true}]},
 {id:'kill', name:'막타', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:99, target:'oneEnemy'}, {k:'ifKill', id:'elite'}, {k:'stack', id:'주머니', v:2}, {k:'ifKill'}, {k:'stack', id:'주머니', v:1}]},
 {id:'gain', name:'쌓기', hero:'p', cost:0, type:'스킬', fx:[{k:'stack', id:'주머니', v:3}]},
 {id:'spend', name:'털기', hero:'p', cost:0, type:'스킬', fx:[{k:'spend', id:'주머니', all:true}]},
 {id:'paper', name:'논문', hero:'p', cost:0, type:'스킬', fx:[{k:'ifGained', id:'주머니', n:5}, {k:'gauge', v:3}]},
 {id:'clock1', name:'시계 1', hero:'p', cost:0, type:'스킬', fx:[{k:'stack', id:'시계', v:1, target:'oneEnemy'}]},
 {id:'clock3', name:'시계 3', hero:'p', cost:0, type:'스킬', fx:[{k:'stack', id:'시계', v:3, target:'oneEnemy'}]},
 {id:'ripe', name:'당기기', hero:'p', cost:0, type:'스킬', fx:[{k:'ripen', id:'시계', v:0.25}]},
 {id:'ripeA', name:'아군 당기기', hero:'p', cost:0, type:'스킬', fx:[{k:'ripen', id:'씨앗', v:0.3, target:'oneAlly'}]},
 {id:'seed', name:'심기', hero:'p', cost:0, type:'스킬', fx:[{k:'stack', id:'씨앗', v:2, target:'oneAlly'}]},
 {id:'rush', name:'재촉', hero:'p', cost:0, type:'스킬', fx:[{k:'hasten', v:1}]},
 {id:'swarm', name:'벌떼', hero:'p', cost:0, type:'공격', fx:[{k:'summon', id:'벌', ratio:1, target:'oneEnemy', fixed:true}]},
 {id:'learn', name:'배움', hero:'p', cost:0, type:'공격', fx:[{k:'perCardSt', id:'배움', n:3}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}]";

        const string KW0 = "{name:'주머니', cap:9}";
        const string KWS0 = "{name:'시계', carrier:'enemy', cap:3, decay:1, reserve:true, rules:[{name:'결말', when:{on:'stackGone', id:'시계', decay:true}, fx:[{k:'dmg', ratio:2, target:'oneEnemy'}]}]}, {name:'씨앗', carrier:'hero', cap:3, decay:1, reserve:true, rules:[{name:'수확', when:{on:'stackGone', id:'씨앗', decay:true}, fx:[{k:'dealtMod', v:0.3, target:'oneAlly', turns:2}]}]}, {name:'벌', cap:9}";
        const string NOP = "{name:'없음', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}";

        static GameData Data(string kw = KW0, string passive = NOP, string kws = KWS0)
            => K.Data(heroes: HEROES.Replace("KWS", kws).Replace("KW", kw).Replace("PASSIVE", passive), cards: CARDS);
        static Battle Two(GameData d, string foe = "dummy", System.Action<BattleSetup> tweak = null) => K.Fight(d, new[] { "p", "q" }, new[] { foe }, tweak);
        static void SetStack(Battle b, string hero, string id, int n) { if (!b.Stacks.TryGetValue(hero, out var bag)) b.Stacks[hero] = bag = new Dictionary<string, int>(); bag[id] = n; }

        // ── 직전보다 비싼가 ───────────────────────────────────────────
        [Test] public void 바로_앞_카드보다_비싸면()
        {
            var b = Two(Data()); K.Hand(b, "c2", "c1", "c2"); b.Ap = 9;
            K.Play(b, "c2");
            Assert.AreEqual(0, b.StackOf("p", "주머니"), "턴 첫 카드는 비싸다고 치지 않는다");
            K.Play(b, "c1"); K.Play(b, "c2");
            Assert.AreEqual(5, b.StackOf("p", "주머니"), "1코 다음 2코");
        }

        [Test] public void 패시브_조건_pricier()
        {
            var b = Two(Data(passive: "{name:'코스', when:{on:'play', who:'any'}, conds:[{c:'pricier'}], fx:[{k:'stack', id:'주머니', v:1}]}")); K.Hand(b, "c0", "c1", "c1", "c2"); b.Ap = 9;
            K.Play(b, "c0"); K.Play(b, "c1"); K.Play(b, "c1"); K.Play(b, "c2");
            // c0(첫) 0 · c1(0→1) +1 · c1(같다) 0 · c2(1→2) +1 (+ c2 자체 효과 5)
            Assert.AreEqual(7, b.StackOf("p", "주머니"));
            b.EndTurn();
            K.Hand(b, "c1"); K.Play(b, "c1");
            Assert.AreEqual(7, b.StackOf("p", "주머니"), "새 턴의 첫 카드 — 지난 턴 카드와 견주지 않는다");
        }

        // ── ifStreak 종류 ─────────────────────────────────────────────
        [Test] public void 제_공격만_잇달아_셋째()
        {
            var b = Two(Data()); K.Hand(b, "pk", "c0", "c0", "st3");
            K.Play(b, "pk"); K.Play(b, "c0"); K.Play(b, "c0"); K.Play(b, "st3");
            Assert.AreEqual(1, b.StackOf("p", "주머니"), "스킬 뒤로 제 공격 셋(c0 · c0 · st3)");
        }

        [Test] public void 스킬이_끼면_끊긴다()
        {
            var b = Two(Data()); K.Hand(b, "c0", "c0", "pk", "st3");
            K.Play(b, "c0"); K.Play(b, "c0"); K.Play(b, "pk"); K.Play(b, "st3");
            Assert.AreEqual(0, b.StackOf("p", "주머니"));
            var c = Two(Data()); K.Hand(c, "c0", "qhit", "c0", "st3");
            K.Play(c, "c0"); K.Play(c, "qhit"); K.Play(c, "c0"); K.Play(c, "st3");
            Assert.AreEqual(0, c.StackOf("p", "주머니"), "동료 카드가 끼어도 끊긴다");
            var d = Two(Data()); K.Hand(d, "c0", "c0", "st3");
            K.Play(d, "c0"); K.Play(d, "c0"); K.Play(d, "st3");
            Assert.AreEqual(1, d.StackOf("p", "주머니"));
        }

        // ── 이번 전투 소멸한 자신의 카드 ─────────────────────────────
        [Test] public void 자신의_카드만_센다()
        {
            var a = Two(Data()); K.Hand(a, "ex", "gone");
            K.Play(a, "ex"); K.Play(a, "gone");
            int one = 1000 - K.Hp(a);
            Assert.Greater(one, 0);
            var b = Two(Data()); K.Hand(b, "ex", "ex", "qex", "gone");
            K.Play(b, "ex"); K.Play(b, "ex"); K.Play(b, "qex");
            Assert.AreEqual(3, b.GoneN);
            Assert.AreEqual(2, b.GoneOf("p"));
            K.Play(b, "gone");
            Assert.AreEqual(2 * one, 1000 - K.Hp(b), 2, "짝의 소멸은 안 센다 — 2타");
            Assert.AreEqual(2, Battle.Load(b.Data, b.Save()).GoneOf("p"), "저장 · 불러오기에 남는다(Counts)");
        }

        // ── 처치한 적 등급 ────────────────────────────────────────────
        [Test] public void 엘리트_처치_덤()
        {
            var b = Two(Data()); K.Hand(b, "kill");
            K.Play(b, "kill");
            Assert.AreEqual(1, b.StackOf("p", "주머니"), "보통 적 — 처치 덤만");
            var e = Two(Data(), "dummy", st => st.Elite = true); K.Hand(e, "kill");
            K.Play(e, "kill");
            Assert.AreEqual(3, e.StackOf("p", "주머니"), "엘리트 싸움 — 엘리트 덤 + 처치 덤");
        }

        // ── 이번 전투에 쌓은 양 ───────────────────────────────────────
        [Test] public void 소모해도_쌓은_양은_남는다()
        {
            var b = Two(Data()); K.Hand(b, "gain", "spend", "paper");
            K.Play(b, "gain"); K.Play(b, "spend");
            Assert.AreEqual(0, b.StackOf("p", "주머니"));
            Assert.AreEqual(3, b.GainedOf("p", "주머니"));
            K.Play(b, "paper");
            Assert.AreEqual(0, b.Gauge, "3 < 5");
            K.Hand(b, "gain", "paper"); K.Play(b, "gain"); K.Play(b, "paper");
            Assert.AreEqual(3, b.Gauge, "6 ≥ 5");
        }

        [Test] public void 전투_승리_조건_gained()
        {
            var b = Two(Data(passive: "{name:'결산', when:{on:'fightEnd'}, conds:[{c:'gained', id:'주머니', n:3}], fx:[{k:'gauge', v:7}]}")); K.Hand(b, "gain", "kill");
            K.Play(b, "gain"); K.Play(b, "kill");
            Assert.AreEqual("win", b.Over);
            Assert.AreEqual(7, b.Gauge);
        }

        // ── 예약 당겨 쓰기 ───────────────────────────────────────────
        [Test] public void 적의_예약을_당기면_남은_칸만큼_약하다()
        {
            var full = Two(Data()); K.Hand(full, "clock1", "ripe"); K.Play(full, "clock1");
            int one = full.HitAmount(full.HeroUnit("p"), 2.0);
            K.Play(full, "ripe");
            Assert.AreEqual(1000 - Num.Round(one * 0.75), K.Hp(full), 3, "1칸 남음 — 75%");
            Assert.AreEqual(0, full.St(full.Enemies[0], "시계"));
            var three = Two(Data()); K.Hand(three, "clock3", "ripe"); K.Play(three, "clock3");
            K.Play(three, "ripe");
            Assert.AreEqual(1000 - Num.Round(one * 0.25), K.Hp(three), 3, "3칸 남음 — 25%");
        }

        [Test] public void 아군의_씨앗을_당기면_그_아군이_세진다()
        {
            var b = Two(Data()); K.Hand(b, "seed", "ripeA");
            K.Play(b, "seed", 1, new PlayOpts { Ally = b.HeroUnit("q").Idx });
            Assert.AreEqual(2, b.StackOf("q", "씨앗"));
            K.Play(b, "ripeA", 0, new PlayOpts { Ally = b.HeroUnit("q").Idx });
            Assert.AreEqual(0, b.StackOf("q", "씨앗"));
            Assert.Greater(b.StatMod(b.HeroUnit("q"), "dealt"), 0.05, "그 아군 주는 피해 + (2칸 남음 → 40%)");
            Assert.AreEqual(0, b.StatMod(b.HeroUnit("p"), "dealt"), 1e-9);
            Assert.IsTrue(b.Log.Any(x => x.Contains("당겨 쓴다")));
        }

        [Test] public void 재촉은_아군의_씨앗도_줄인다()
        {
            var b = Two(Data()); SetStack(b, "q", "씨앗", 1); K.Hand(b, "rush");
            K.Play(b, "rush");
            Assert.AreEqual(0, b.StackOf("q", "씨앗"));
            Assert.Greater(b.StatMod(b.HeroUnit("q"), "dealt"), 0.29, "다 닳아 수확(효과 100%)");
        }

        // ── 소환물 ───────────────────────────────────────────────────
        [Test] public void 소환물이_겹_수만큼_따라_친다_한_카드에_상한()
        {
            var b = Two(Data(passive: "{name:'행동', when:{on:'summonAct', id:'벌', kind:'atk'}, fx:[{k:'gauge', v:1}]}")); SetStack(b, "p", "벌", 3); K.Hand(b, "swarm");
            K.Play(b, "swarm");
            Assert.AreEqual(1000 - 3 * 100, K.Hp(b), "고정 피해 100 × 3대");
            Assert.AreEqual(1, b.Gauge, "summonAct 는 한 번");
            var c = Two(Data()); SetStack(c, "p", "벌", 9); K.Hand(c, "swarm");
            K.Play(c, "swarm");
            Assert.AreEqual(1000 - Battle.SUMMON_CAP * 100, K.Hp(c), $"한 카드에 {Battle.SUMMON_CAP}대까지");
        }

        [Test] public void 따라_치기_패시브와_카드가_상한을_나눠_쓴다()
        {
            var b = Two(Data(passive: "{name:'따라', when:{on:'play', type:'공격'}, fx:[{k:'summon', id:'벌', ratio:1, target:'oneEnemy', fixed:true}]}")); SetStack(b, "p", "벌", 4); K.Hand(b, "swarm");
            K.Play(b, "swarm");
            Assert.AreEqual(1000 - Battle.SUMMON_CAP * 100, K.Hp(b), "카드 4대 + 패시브는 남은 1대만");
        }

        [Test] public void 분신은_uses_대를_받고_사라진다()
        {
            var d = Data(kw: "{name:'분신', cap:3, guard:true, uses:2}", passive: "{name:'자폭', when:{on:'summonAct', id:'분신', kind:'lost'}, fx:[{k:'gauge', v:5}]}");
            var b = Two(d, "hitter"); SetStack(b, "p", "분신", 1);
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            Assert.AreEqual(hp0, b.Pool.Hp, "첫 대 — 분신이 받고 남는다");
            Assert.AreEqual(1, b.StackOf("p", "분신"));
            Assert.AreEqual(0, b.Gauge);
            b.EndTurn();
            Assert.AreEqual(0, b.StackOf("p", "분신"), "둘째 대에 사라진다");
            Assert.AreEqual(5, b.Gauge);
        }

        // ── perCardSt 최소 ───────────────────────────────────────────
        [Test] public void 카드_값_비례의_최소()
        {
            var b = Two(Data()); K.Hand(b, "learn");
            int one = b.HitAmount(b.HeroUnit("p"), 0.1);
            K.Play(b, "learn");
            Assert.AreEqual(1000 - 3 * one, K.Hp(b), 2, "배움 0 이어도 최소 3타");
        }

        // ── 봇 소모량 고르기 공개 API(화면 자동 전투) ─────────────────
        [Test] public void 봇_PickSpend_는_후보_가운데_하나()
        {
            var d = K.Data(heroes: HEROES.Replace("KWS", KWS0).Replace("KW", KW0).Replace("PASSIVE", NOP),
                cards: "[{id:'sp', name:'털기', hero:'p', cost:0, type:'공격', fx:[{k:'spend', id:'주머니', pick:true}, {k:'perEvent'}, {k:'dmg', ratio:1, target:'oneEnemy'}]}, {id:'nop', name:'맨', hero:'p', cost:0, type:'스킬', fx:[{k:'draw', v:1}]}]");
            var b = Two(d); SetStack(b, "p", "주머니", 5); K.Hand(b, "sp", "nop");
            var bot = new Bots(d);
            int x = bot.PickSpend(b, "sp", 0);
            CollectionAssert.Contains(b.SpendChoices("sp"), x);
            Assert.AreEqual(5, x, "다른 쓸 데가 없으면 전부가 가장 세다");
            Assert.AreEqual(5, b.StackOf("p", "주머니"), "판은 그대로(복사해서 셈)");
            Assert.AreEqual(0, bot.PickSpend(b, "nop", 0), "소모량 고르기 카드가 아니면 0");
        }

        // ── 봇(2단계 손질) ───────────────────────────────────────────
        [Test] public void 봇은_예약_표식을_터질_몫으로_센다()
        {
            var d = Data(); var bot = new Bots(d);
            var a = Two(d); K.Hand(a, "clock1"); double s0 = bot.Score(a, false);
            K.Play(a, "clock1");
            double gain = bot.Score(a, false) - s0;
            Assert.Greater(gain, 0.5 * a.HitAmount(a.HeroUnit("p"), 2.0), "다 닳으면 터질 피해(공격력 200%)를 할인해 센다 — 옛 셈은 겹 1 = 작은 값");
        }

        [Test] public void 봇_소환물_겹은_따라_칠_몫()
        {
            var d = Data(passive: "{name:'따라', when:{on:'play', type:'공격'}, fx:[{k:'summon', id:'벌', ratio:1, target:'oneEnemy'}]}");
            var b = Two(d); var bot = new Bots(d);
            Assert.Greater(bot.StackWorth(b, "벌", 3), bot.StackWorth(b, "주머니", 3), "따라 치는 소환물은 맨 겹보다 무겁다");
            var e = Two(Data(kw: "{name:'주머니', cap:9, endClear:true}"));
            Assert.Less(new Bots(e.Data).StackWorth(e, "주머니", 3), bot.StackWorth(b, "주머니", 3), "턴 끝에 사라지는 겹은 덜");
        }

        // ── 검사기 · 글 ──────────────────────────────────────────────
        [Test] public void 검사기_새_부품()
        {
            var v = Validator.Check(Data());
            Assert.IsFalse(v.Errors.Any(x => x.StartsWith("카드 ") && x.Contains("(") == false && (x.Contains("ripe") || x.Contains("swarm") || x.Contains("paper") || x.Contains("kill"))), string.Join("\n", v.Errors));
            var bad = Validator.Check(K.Data(heroes: HEROES.Replace("KWS", KWS0).Replace("KW", KW0).Replace("PASSIVE", NOP),
                cards: "[{id:'b1', name:'틀림', hero:'p', cost:0, type:'스킬', fx:[{k:'ripen', id:'주머니'}]}, {id:'b2', name:'틀림2', hero:'p', cost:0, type:'공격', fx:[{k:'ifKill', id:'왕'}, {k:'draw', v:1}]}, {id:'b3', name:'틀림3', hero:'p', cost:0, type:'공격', fx:[{k:'summon', id:'벌', ratio:0.3, max:9}]}]"));
            Assert.IsTrue(bad.Errors.Any(x => x.Contains("예약 키워드")), string.Join("\n", bad.Errors));
            Assert.IsTrue(bad.Errors.Any(x => x.Contains("elite")));
            Assert.IsTrue(bad.Errors.Any(x => x.Contains("max 는")));
            var uses = Validator.Check(Data(kw: "{name:'주머니', cap:3, uses:2}"));
            Assert.IsTrue(uses.Errors.Any(x => x.Contains("uses 는 guard")));
        }

        [Test] public void 카드_글()
        {
            var t = new CardText(Data());
            StringAssert.Contains("바로 앞 카드보다 비싸면", t.Fx(new List<Fx> { new Fx { K = FxK.IfPricier }, new Fx { K = FxK.Draw, V = 1 } }));
            StringAssert.Contains("자신의 공격 카드를 3장째 잇달아", t.Fx(new List<Fx> { new Fx { K = FxK.IfStreak, N = 3, Type = "공격" }, new Fx { K = FxK.Draw, V = 1 } }));
            StringAssert.Contains("소멸한 자신의 카드 1장당", t.Fx(new List<Fx> { new Fx { K = FxK.PerGone, Who = "self" }, new Fx { K = FxK.Dmg, Ratio = 0.3, Target = "oneEnemy" } }));
            StringAssert.Contains("엘리트 · 보스를 처치하면", t.Fx(new List<Fx> { new Fx { K = FxK.IfKill, Id = "elite" }, new Fx { K = FxK.Draw, V = 1 } }));
            StringAssert.Contains("이번 전투에 쌓은", t.Fx(new List<Fx> { new Fx { K = FxK.IfGained, Id = "주머니", N = 5 }, new Fx { K = FxK.Draw, V = 1 } }));
            StringAssert.Contains("지금 터뜨림", t.Fx(new List<Fx> { new Fx { K = FxK.Ripen, Id = "시계", V = 0.25 } }));
            StringAssert.Contains("따라 침", t.Fx(new List<Fx> { new Fx { K = FxK.Summon, Id = "벌", Ratio = 0.3 } }));
            Assert.AreEqual("「벌」이 하나 사라지면", t.WhenText(new When { On = "summonAct", Id = "벌", Kind = "lost" }));
        }
    }
}
