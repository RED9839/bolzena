using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 18갈래 1단계(2026-10-08) — 시범 16 전에 엔진 · 봇 손질.
    /// 다 차면 만들기 · 강화(onMax make · empower) · 만든 카드 주인(make owner) · 소모량 고르기(spend pick) · keepAp 보존 ·
    /// 버티 · 회복 변환(guardSum · perGuarded · overheal pct · perOverheal) · 연쇄 상한 셋 · 검사기 · 봇(세 수 · 예약 · 판 단위 값).
    /// </summary>
    public class StageOneTests
    {
        const string HEROES = @"[
 {id:'p', name:'주인', role:'딜러', hp:600, atk:100, def:20, keyword:KW, passives:[PASSIVE]},
 {id:'q', name:'짝', role:'탱커', hp:900, atk:200, def:60, keyword:{name:'짝주머니', cap:9}, passives:[QPASSIVE]}]";
        const string CARDS = @"[
 {id:'tok', name:'만든 칼', hero:'p', cost:0, type:'공격', token:true, fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'fill', name:'채우기', hero:'p', cost:0, type:'스킬', fx:[{k:'stack', id:'주머니', v:1}]},
 {id:'phit', name:'주인 치기', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'qhit', name:'짝 치기', hero:'q', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'mk', name:'건네기', hero:'p', cost:0, type:'스킬', fx:[{k:'make', id:'tok', v:1, owner:'other'}]},
 {id:'emp', name:'북돋기', hero:'p', cost:0, type:'스킬', fx:[{k:'empower', ratio:1, who:'any'}]},
 {id:'sp', name:'털기', hero:'p', cost:0, type:'공격', fx:[{k:'spend', id:'주머니', pick:true}, {k:'perEvent'}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'keepc', name:'쥘 카드', hero:'p', cost:9, type:'스킬', tags:['보존'], fx:[{k:'draw', v:1}]},
 {id:'plainc', name:'맨 카드', hero:'p', cost:9, type:'스킬', fx:[{k:'draw', v:1}]},
 {id:'bigheal', name:'큰 회복', hero:'p', cost:0, type:'스킬', fx:[{k:'heal', ratio:10}]},
 {id:'ovp', name:'넘친 만큼', hero:'p', cost:0, type:'스킬', fx:[{k:'heal', ratio:10}, {k:'perOverheal', pct:0.75, per:25}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'rush', name:'재촉', hero:'p', cost:0, type:'스킬', fx:[{k:'hasten', v:1}]},
 {id:'revive', name:'되살리기', hero:'p', cost:0, type:'스킬', fx:[{k:'pull', from:'gone', n:1}]}]";

        static GameData Data(string kw = "{name:'주머니', cap:9}", string passive = "{name:'없음', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}", string qpassive = "{name:'없음2', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}")
            => K.Data(heroes: HEROES.Replace("KW", kw).Replace("QPASSIVE", qpassive).Replace("PASSIVE", passive), cards: CARDS);
        static Battle Two(GameData d, string foe = "dummy") => K.Fight(d, new[] { "p", "q" }, new[] { foe });
        static void SetStack(Battle b, string hero, string id, int n) { if (!b.Stacks.TryGetValue(hero, out var bag)) b.Stacks[hero] = bag = new Dictionary<string, int>(); bag[id] = n; }

        // ── 다 차면 만들기 · 강화 ────────────────────────────────────
        [Test] public void 다_차면_시그니처_카드를_손에_만든다()
        {
            var b = Two(Data(kw: "{name:'주머니', cap:3, onMax:{make:'tok', consume:true}}")); K.Hand(b, "fill", "fill", "fill");
            K.Play(b, "fill"); K.Play(b, "fill");
            Assert.IsFalse(b.Hand.Any(id => GameData.BaseId(id) == "tok"));
            K.Play(b, "fill");
            Assert.AreEqual(1, b.Hand.Count(id => GameData.BaseId(id) == "tok"), "최대가 되면 한 장");
            Assert.AreEqual(0, b.StackOf("p", "주머니"), "consume — 다 쓴다");
            Assert.AreEqual(1000, K.Hp(b), "저절로 터지지 않는다(피해 없음)");
        }

        [Test] public void 다_차면_다음_카드_강화()
        {
            var b = Two(Data(kw: "{name:'주머니', cap:2, onMax:{empower:'next', ratio:1, consume:true}}")); K.Hand(b, "phit", "fill", "fill", "phit");
            K.Play(b, "phit"); int one = 1000 - K.Hp(b);
            K.Play(b, "fill"); K.Play(b, "fill");
            Assert.AreEqual(100, b.EmpowerOf("p"));
            int hp0 = K.Hp(b); K.Play(b, "phit");
            Assert.AreEqual(one * 2, hp0 - K.Hp(b), 1, "강화 +100% — 두 배");
            Assert.AreEqual(0, b.EmpowerOf("p"), "한 장에 쓰고 사라진다");
        }

        [Test] public void 사도_몫_강화는_다른_사도_카드에_안_쓴다()
        {
            var b = Two(Data(kw: "{name:'주머니', cap:1, onMax:{empower:'next', ratio:0.5}}")); K.Hand(b, "fill", "qhit");
            K.Play(b, "fill");
            K.Play(b, "qhit");
            Assert.AreEqual(50, b.EmpowerOf("p"), "짝의 카드는 주인 몫을 안 쓴다");
        }

        [Test] public void 효과_empower_any_는_파티의_다음_카드()
        {
            var b = Two(Data()); K.Hand(b, "emp", "qhit");
            K.Play(b, "emp");
            Assert.AreEqual(100, b.EmpowerOf("q"));
            K.Play(b, "qhit");
            Assert.AreEqual(0, b.EmpowerOf("q"));
            var b2 = Battle.Load(b.Data, b.Save());
            Assert.AreEqual(0, b2.EmpowerOf("q"));
        }

        // ── 만든 카드의 주인 ─────────────────────────────────────────
        [Test] public void 만든_카드의_주인을_다음_아군으로()
        {
            var b = Two(Data()); K.Hand(b, "mk");
            K.Play(b, "mk");
            var made = b.Hand.Single(id => GameData.BaseId(id) == "tok");
            Assert.AreEqual("q", b.CardOf(made).Hero, "손패는 하나 — 「다른 아군 손에」 대신 주인을 바꾼다");
            Assert.IsTrue(GameData.IsPlain(made));
            K.Play(b, made);
            Assert.AreEqual(1, b.PlayedBy["q"]);
        }

        // ── 소모량 고르기 ────────────────────────────────────────────
        [Test] public void 소모량을_골라_그만큼()
        {
            var b = Two(Data()); SetStack(b, "p", "주머니", 5); K.Hand(b, "sp", "sp");
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, b.SpendChoices("sp"));
            int one = b.HitAmount(b.HeroUnit("p"), 1.0);
            K.Play(b, "sp", 0, new PlayOpts { Spend = 2 });
            Assert.AreEqual(3, b.StackOf("p", "주머니"));
            Assert.AreEqual(1000 - 2 * one, K.Hp(b), 2, "소모한 2 가 일의 값 — 2회");
            K.Play(b, "sp");
            Assert.AreEqual(0, b.StackOf("p", "주머니"), "안 고르면 전부");
        }

        [Test] public void 봇은_소모량_셋을_다_둬_본다()
        {
            var d = Data(); var b = Two(d); SetStack(b, "p", "주머니", 4); K.Hand(b, "sp");
            var ms = new Bots(d).Moves(b).Where(m => m.Id == "sp").Select(m => m.Spend).OrderBy(x => x).ToList();
            CollectionAssert.AreEqual(new int?[] { 1, 2, 4 }, ms);
        }

        // ── keepAp 넓히기 ────────────────────────────────────────────
        [Test] public void 보존_카드를_쥐고_턴을_마쳐도_아껴_두기()
        {
            var b = Two(Data(passive: "{name:'아껴 둠', when:{on:'keepAp'}, fx:[{k:'stack', id:'주머니', v:2}]}")); K.Hand(b, "keepc"); b.Ap = 0;
            b.EndTurn();
            Assert.AreEqual(2, b.StackOf("p", "주머니"), "AP 0 이어도 보존 카드를 쥐었으면");
        }

        [Test] public void keepAp_kind_ap_는_보존을_안_센다()
        {
            var b = Two(Data(passive: "{name:'아껴 둠', when:{on:'keepAp', kind:'ap'}, fx:[{k:'stack', id:'주머니', v:2}]}")); K.Hand(b, "keepc"); b.Ap = 0;
            b.EndTurn();
            Assert.AreEqual(0, b.StackOf("p", "주머니"));
            var c = Two(Data(passive: "{name:'아껴 둠', when:{on:'keepAp', kind:'keep'}, fx:[{k:'stack', id:'주머니', v:2}]}")); K.Hand(c, "plainc");
            c.EndTurn();
            Assert.AreEqual(0, c.StackOf("p", "주머니"), "kind keep — AP 만 남긴 것은 안 센다");
        }

        // ── 버티 · 회복 변환 ─────────────────────────────────────────
        [Test] public void 막아_낸_양을_다음_턴_시작에_피해로()
        {
            var b = Two(Data(passive: "{name:'되갚기', when:{on:'guardSum'}, fx:[{k:'dmg', ratio:1, ofEvent:1, target:'oneEnemy'}]}"), "hitter");
            b.Pool.Shield = 5000;
            b.EndTurn();
            Assert.Greater(b.GuardedPrev, 0, "때리는 놈의 공격을 실드가 막았다");
            Assert.AreEqual(1000 - b.GuardedPrev, K.Hp(b), "막아 낸 양 그대로 고정 피해");
            Assert.AreEqual(b.GuardedPrev, Battle.Load(b.Data, b.Save()).GuardedPrev);
        }

        [Test] public void 넘침을_회복_뒤_HP_비율로_다시_정의()
        {
            // 파티 최대 HP 1500 · HP 1000 에서 회복 200(방어력 20 × 10) → 1200 = 80%. 75% 선(1125)을 넘은 몫 75
            var b = Two(Data(passive: "{name:'넘침', when:{on:'overheal', pct:0.75}, fx:[{k:'stack', id:'주머니', v:1, ofEvent:0.1}]}")); b.Pool.Hp = 1000; K.Hand(b, "bigheal");
            K.Play(b, "bigheal");
            Assert.AreEqual(1200, b.Pool.Hp);
            Assert.AreEqual(7, b.StackOf("p", "주머니"), "선을 넘은 몫 75 × 0.1");
            var c = Two(Data(passive: "{name:'넘침', when:{on:'overheal'}, fx:[{k:'stack', id:'주머니', v:1, ofEvent:0.1}]}")); c.Pool.Hp = 1000; K.Hand(c, "bigheal");
            K.Play(c, "bigheal");
            Assert.AreEqual(0, c.StackOf("p", "주머니"), "pct 가 없으면 옛 정의 — 최대 HP 를 안 넘쳤다");
        }

        [Test] public void 이번_판_넘친_회복_비례()
        {
            var b = Two(Data()); b.Pool.Hp = 1000; K.Hand(b, "ovp");
            int one = b.HitAmount(b.HeroUnit("p"), 1.0);
            K.Play(b, "ovp");
            Assert.AreEqual(75, b.OverhealSum(0.75));
            Assert.AreEqual(1000 - 3 * one, K.Hp(b), 2, "75 / 25 = 3회");
        }

        // ── 연쇄 상한 ────────────────────────────────────────────────
        [Test] public void 예약_계기_안의_예약_계기는_돌지_않는다()
        {
            const string KW = "{name:'시계', cap:9, reserve:true, decay:1}";
            var d = K.Data(heroes: @"[
 {id:'p', name:'주인', role:'딜러', hp:600, atk:100, def:20, keyword:" + KW + @", passives:[{name:'또 재촉', when:{on:'reserveGone'}, fx:[{k:'gauge', v:1}, {k:'stack', id:'시계', v:1}, {k:'hasten', v:1}]}]},
 {id:'q', name:'짝', role:'탱커', hp:900, atk:200, def:60, keyword:{name:'짝시계', cap:9, reserve:true, decay:1}, passives:[{name:'또 재촉2', when:{on:'reserveGone'}, fx:[{k:'gauge', v:1}, {k:'stack', id:'짝시계', v:1}, {k:'hasten', v:1}]}]}]", cards: CARDS);
            var b = Two(d); SetStack(b, "p", "시계", 1); SetStack(b, "q", "짝시계", 1); K.Hand(b, "rush");
            K.Play(b, "rush");
            // 재촉 한 번 → 두 예약이 다 닳음(바깥 계기 둘) → 패시브 둘씩 = 4. 그 안의 재촉이 만든 예약 계기는 돌지 않는다
            Assert.AreEqual(4, b.Gauge);
            Assert.IsTrue(b.Log.Any(x => x.Contains("예약 연쇄")));
        }

        [Test] public void 소멸_더미_되살리기는_사도마다_턴에_한_번()
        {
            var b = Two(Data()); b.Gone.AddRange(new[] { "phit", "qhit", "fill" }); K.Hand(b, "revive", "revive");
            K.Play(b, "revive");
            Assert.AreEqual(2, b.Hand.Count, "되살리기 한 장 + 꺼낸 한 장");
            K.Play(b, "revive");
            Assert.AreEqual(1, b.Hand.Count, "둘째는 아무것도 안 꺼낸다");
            Assert.AreEqual(2, b.Gone.Count);
        }

        [Test] public void 같은_갈래_who_any_는_한_사도만()
        {
            const string H = @"[
 {id:'s1', name:'하나', role:'딜러', hp:600, atk:100, def:20, style:'거드는 형', keyword:{name:'하나주머니', cap:9}, passives:[{name:'올라타기', when:{on:'play', who:'any'}, fx:[{k:'gauge', v:2}]}, {name:'또 올라타기', when:{on:'play', who:'any'}, fx:[{k:'gauge', v:1}]}]},
 {id:'s2', name:'둘', role:'딜러', hp:600, atk:100, def:20, style:'STYLE2', keyword:{name:'둘주머니', cap:9}, passives:[{name:'올라타기2', when:{on:'play', who:'any'}, fx:[{k:'gauge', v:2}]}]},
 {id:'r', name:'셋', role:'탱커', hp:900, atk:100, def:60, keyword:{name:'셋주머니', cap:9}}]";
            const string C = "[{id:'rz', name:'셋 숨', hero:'r', cost:0, type:'스킬', fx:[]}]";
            var same = K.Fight(K.Data(heroes: H.Replace("STYLE2", "거드는 형"), cards: C), new[] { "s1", "s2", "r" }, new[] { "dummy" }); K.Hand(same, "rz");
            K.Play(same, "rz");
            Assert.AreEqual(3, same.Gauge, "같은 갈래 — 하나만(그 사도의 두 규칙은 다 돈다)");
            var diff = K.Fight(K.Data(heroes: H.Replace("STYLE2", "표적형"), cards: C), new[] { "s1", "s2", "r" }, new[] { "dummy" }); K.Hand(diff, "rz");
            K.Play(diff, "rz");
            Assert.AreEqual(5, diff.Gauge, "다른 갈래는 따로 돈다");
        }

        // ── 검사기 ───────────────────────────────────────────────────
        [Test] public void 검사기_다_차면_만들기_강화는_터짐이_아니다()
        {
            const string H = "[{id:'s', name:'갈래', role:'딜러', hp:600, atk:100, def:20, style:'박자형', keyword:KW, passives:[{name:'채움', when:{on:'turnStart'}, fx:[{k:'stack', id:'갈래주머니', v:1}]} RULE]}]";
            const string C = "[{id:'sig', name:'시그니처', hero:'s', cost:1, type:'공격', fx:[{k:'dmg', ratio:2, target:'oneEnemy'}]}]";
            bool Burst(string kw, string rule) => Validator.Check(K.Data(heroes: H.Replace("KW", kw).Replace("RULE", rule), cards: C)).Warnings.Any(x => x.Contains("저절로 터진다"));
            Assert.IsFalse(Burst("{name:'갈래주머니', cap:3, onMax:{make:'sig', consume:true}}", ""));
            Assert.IsFalse(Burst("{name:'갈래주머니', cap:3, onMax:{empower:'next', ratio:0.5}}", ""));
            Assert.IsFalse(Burst("{name:'갈래주머니', cap:3}", ", {name:'만들기', when:{on:'stackReach', id:'갈래주머니', n:3}, fx:[{k:'spend', id:'갈래주머니', all:true}, {k:'make', id:'sig'}]}"));
            Assert.IsTrue(Burst("{name:'갈래주머니', cap:3}", ", {name:'터짐', when:{on:'stackReach', id:'갈래주머니', n:3}, fx:[{k:'dmg', ratio:2, target:'allEnemies'}]}"));
            var bad = Validator.Check(K.Data(heroes: H.Replace("KW", "{name:'갈래주머니', cap:3, onMax:{make:'sig', empower:'next', ratio:0.5}}").Replace("RULE", ""), cards: C));
            Assert.IsTrue(bad.Errors.Any(x => x.Contains("하나만")));
        }

        [Test] public void 검사기_0코_태우기_뽑기는_소멸을_단다()
        {
            const string C = "[{id:'fuel', name:'땔감', hero:'a', cost:0, type:'스킬', fx:[{k:'burn', v:1}, {k:'draw', v:1}]}, {id:'fuel2', name:'땔감2', hero:'a', cost:0, type:'스킬', tags:['소멸'], fx:[{k:'burn', v:1}, {k:'draw', v:1}]}]";
            var w = Validator.Check(K.Data(cards: C)).Warnings;
            Assert.IsTrue(w.Any(x => x.Contains("fuel:") && x.Contains("0코 태우기")), string.Join("\n", w));
            Assert.IsFalse(w.Any(x => x.Contains("fuel2") && x.Contains("0코 태우기")));
        }

        [Test] public void 검사기_새_문법()
        {
            var d = Data(kw: "{name:'주머니', cap:3, onMax:{make:'tok', consume:true}}", passive: "{name:'넘침', when:{on:'overheal', pct:0.8}, fx:[{k:'perOverheal', pct:0.8, per:10}, {k:'shield', ratio:0.1}]}",
                qpassive: "{name:'막음', when:{on:'guardSum'}, fx:[{k:'perGuarded', per:20}, {k:'dmg', ratio:0.2, target:'oneEnemy'}]}");
            var v = Validator.Check(d);
            Assert.IsFalse(v.Errors.Any(x => x.StartsWith("사도 p") || x.StartsWith("사도 q") || x.StartsWith("카드 mk") || x.StartsWith("카드 sp") || x.StartsWith("카드 emp")), string.Join("\n", v.Errors));
            var bad = Validator.Check(K.Data(cards: "[{id:'x1', name:'틀림', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:1, owner:'other'}]}]"));
            Assert.IsTrue(bad.Errors.Any(x => x.Contains("owner 는 make 에만")));
        }

        // ── 글 ───────────────────────────────────────────────────────
        [Test] public void 카드_글()
        {
            var t = new CardText(Data());
            Assert.AreEqual("보존 카드를 쥐고 턴을 마치면", t.WhenText(new When { On = "keepAp", Kind = "keep" }));
            Assert.AreEqual("회복 뒤 HP가 80% 이상이면", t.WhenText(new When { On = "overheal", Pct = 0.8 }));
            StringAssert.Contains("고른 만큼 소모", t.Fx(new List<Fx> { new Fx { K = FxK.Spend, Id = "주머니", Pick = true } }));
            StringAssert.Contains("다음 카드", t.Fx(new List<Fx> { new Fx { K = FxK.Empower, Ratio = 0.5 } }));
            StringAssert.Contains("주인: 다음 아군", t.Fx(new List<Fx> { new Fx { K = FxK.Make, Id = "tok", V = 1, Owner = "other" } }));
        }

        // ── 봇 ───────────────────────────────────────────────────────
        const string RCARDS = @"[
 {id:'r1', name:'첫 박', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.3, target:'oneEnemy'}]},
 {id:'r2', name:'둘째 박', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.3, target:'oneEnemy'}]},
 {id:'r3', name:'셋째 박', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'ifNth', n:3}, {k:'dmg', ratio:8, target:'oneEnemy'}]},
 {id:'r4', name:'한 방', hero:'a', cost:3, type:'공격', fx:[{k:'dmg', ratio:2.5, target:'oneEnemy'}]}]";

        [Test] public void 봇은_셋째_카드_덤을_본다()
        {
            var d = K.Data(cards: RCARDS);
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.EnemyHp = 5);
            K.Hand(b, "r4", "r1", "r2", "r3"); b.Ap = 3;
            Assert.IsTrue(Bots.Rhythmic(b));
            new Bots(d).SmartPlay(b);
            Assert.IsTrue(b.Hand.Contains("r4"), "한 방(3AP)보다 셋째 덤이 크다");
            Assert.AreEqual(0, b.Hand.Count(id => id.StartsWith("r") && id != "r4"), "세 장을 차례로");
        }

        [Test] public void 걸어_둔_예약은_판_점수에_든다()
        {
            var d = K.Data(cards: "[{id:'lt', name:'나중에', hero:'a', cost:0, type:'스킬', fx:[{k:'later', n:1, then:[{k:'dmg', ratio:3, target:'oneEnemy'}]}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "lt");
            var bot = new Bots(d);
            double before = bot.Score(b, false);
            Assert.AreEqual(0, bot.ReserveWorth(b));
            K.Play(b, "lt");
            Assert.Greater(bot.ReserveWorth(b), 0);
            Assert.Greater(bot.Score(b, false), before, "예약을 걸면 판이 좋아진다");
        }

        [Test] public void 보존_카드를_쥐면_아껴_두기_몫을_센다()
        {
            var d = Data(passive: "{name:'아껴 둠', when:{on:'keepAp', kind:'keep'}, fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]}");
            var bot = new Bots(d);
            var a = Two(d); K.Hand(a, "keepc"); a.Ap = 0;
            var c = Two(d); K.Hand(c, "plainc"); c.Ap = 0;
            Assert.Greater(bot.Score(a, false), bot.Score(c, false));
        }

        [Test] public void 판_단위_값_근사()
        {
            var grow = new List<Fx> { new Fx { K = FxK.CardStatus, Id = "숙련", V = 1 } };
            var once = new List<Fx> { new Fx { K = FxK.CardStatus, Id = "숙련", V = 1, Battle = true } };
            Assert.Greater(CardValue.ValueOf(grow), CardValue.ValueOf(once), "판에 남는 카드 값은 더 센다");
            Assert.Greater(CardValue.ValueOf(new List<Fx> { new Fx { K = FxK.GrowRun, Id = "atk", V = 3 } }), CardValue.ValueOf(new List<Fx> { new Fx { K = FxK.GrowRun, Id = "atk", V = 1 } }));
            var d = Data(); var b = Two(d); var bot = new Bots(d);
            double v0 = bot.RunWorth(b);
            b.CardSt["phit"] = new Dictionary<string, int> { ["숙련"] = 2 };
            Assert.Greater(bot.RunWorth(b), v0);
        }
    }
}
