using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>엘다인 4차 태세(2026-10-11 사용자 — 카제나 린 「흑운태세」 꼴) — 들기 · 덤 · 「태세 중에만」 · 다른 아군 공격에 풀림 · 보존 · 글 · 검사기 · 봇.</summary>
    public class StanceTests
    {
        const string HEROES = @"[
 {id:'rk', name:'흑', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'흑운', cap:9},
  forms:[{id:'hk', name:'흑운태세', stance:true, until:{on:'play', who:'other', type:'공격'}}]}
]";
        const string CARDS = @"[
 {id:'draw_in', name:'발도', hero:'rk', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}, {k:'form', id:'hk'}]},
 {id:'cut', name:'멸', hero:'rk', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}, {k:'ifForm', id:'hk'}, {k:'dmg', ratio:2, target:'oneEnemy'}]},
 {id:'only', name:'발도 III', hero:'rk', cost:0, type:'공격', needForm:'hk', fx:[{k:'dmg', ratio:4.5, target:'oneEnemy'}]},
 {id:'mind', name:'심법', hero:'rk', cost:0, type:'스킬', fx:[{k:'formKeep', turns:1}]},
 {id:'sheath', name:'납도', hero:'rk', cost:0, type:'스킬', fx:[{k:'formEnd'}]}
]";
        static GameData D() => K.Data(heroes: HEROES, cards: CARDS);
        static Battle F() => K.Fight(D(), new[] { "rk", "c" }, new[] { "dummy" });

        [Test] public void 태세_들기_덤_태세_중에만()
        {
            var b = F(); b.Ap = 9;
            K.Hand(b, "only", "draw_in", "cut", "only");
            StringAssert.Contains("흑운태세", b.CanPlay("only"));
            K.Play(b, "draw_in");
            Assert.IsTrue(b.InForm("rk")); Assert.IsTrue(b.FormOf("rk").Stance);
            Assert.IsTrue(b.StatusViews(b.HeroUnit("rk")).Any(s => s.Form && s.Stance && s.Id == "흑운태세"), "태세 칩");
            int h0 = K.Hp(b); K.Play(b, "cut");
            Assert.AreEqual(300, h0 - K.Hp(b), "태세 덤 +200%");
            Assert.IsNull(b.CanPlay("only"), "태세 중에만 사용 가능");
        }

        [Test] public void 다른_아군_공격에_풀리고_보존이면_안_풀린다()
        {
            var b = F(); b.Ap = 9;
            K.Hand(b, "draw_in", "hit_c"); K.Play(b, "draw_in"); K.Play(b, "hit_c");
            Assert.IsFalse(b.InForm("rk"), "다른 아군이 공격하면 풀림");
            var x = F(); x.Ap = 9;
            K.Hand(x, "draw_in", "mind", "hit_c"); K.Play(x, "draw_in"); K.Play(x, "mind"); K.Play(x, "hit_c");
            Assert.IsTrue(x.InForm("rk"), "1턴간 보존");
            Assert.IsTrue(x.FormOf("rk").Kept);
            x.EndTurn();
            Assert.IsFalse(x.FormKept("rk"), "다음 턴엔 보존이 끝남");
            K.Hand(x, "hit_c"); K.Play(x, "hit_c");
            Assert.IsFalse(x.InForm("rk"));
            var y = F(); K.Hand(y, "draw_in", "sheath"); K.Play(y, "draw_in"); K.Play(y, "sheath");
            Assert.IsFalse(y.InForm("rk"), "스스로 해제");
        }

        [Test] public void 태세_글()
        {
            var d = D(); var t = new CardText(d);
            StringAssert.Contains("「흑운태세」 획득", t.Card(d.View("draw_in")));
            StringAssert.Contains("「흑운태세」: 그 적에게 피해 200%", t.Card(d.View("cut")).Replace("「흑운태세」: 피해 200% 더", "「흑운태세」: 그 적에게 피해 200%"));
            StringAssert.Contains("「흑운태세」 중에만 사용 가능", t.Card(d.View("only")));
            StringAssert.Contains("1턴간 「흑운태세」 보존", t.Card(d.View("mind")));
            StringAssert.Contains("「흑운태세」 해제", t.Card(d.View("sheath")));
            StringAssert.Contains("태세", t.Tips()["흑운태세"]);
            StringAssert.Contains("풀릴 때까지", t.Form(d.Form("hk")));
        }

        [Test] public void 검사기_태세는_turnMake_없어도_된다()
        {
            var st = EldainTests.Eld("디아나_왕년", "딜러", forms: ", forms:[{id:'디아나_왕년_f', name:'백수공권', stance:true, until:{on:'play', who:'other', type:'공격'}}]");
            Assert.IsFalse(EldainTests.EldErrors(st).Any(e => e.Contains("turnMake")), string.Join(" | ", EldainTests.EldErrors(st)));
            var fm = EldainTests.Eld("디아나_왕년", "딜러", forms: ", forms:[{id:'디아나_왕년_f', name:'백수공권', turns:2}]");
            EldainTests.Has(EldainTests.EldErrors(fm), "turnMake 가 없다");
            var d = K.Data(cards: "[{id:'nf', name:'엉뚱', hero:'a', cost:0, type:'스킬', needForm:'없는', fx:[{k:'draw', v:1}]}]");
            Assert.IsTrue(Validator.Check(d).Errors.Any(e => e.Contains("needForm")));
        }

        // ── 우로스 선 · 악(2026-10-11 사용자) — 자기 카드를 내면 짝 태세로 · 덤은 내는 순간의 태세 · 이번엔 유지 · 시작 태세 ──
        const string UR = @"[{id:'ur2', name:'로', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'영역', cap:99}, startForm:'gd',
  forms:[{id:'gd', name:'선', stance:true, toggle:'ev'}, {id:'ev', name:'악', stance:true, toggle:'gd'}]}]";
        const string UR_C = @"[{id:'u1', name:'순환', hero:'ur2', cost:0, type:'스킬', fx:[{k:'ifForm', id:'gd'}, {k:'stack', id:'영역', v:1}, {k:'ifForm', id:'ev'}, {k:'stack', id:'영역', v:10}]},
 {id:'stay', name:'지킴', hero:'ur2', cost:0, type:'스킬', fx:[{k:'noToggle'}]}]";
        [Test] public void 선_악_태세가_자기_카드마다_번갈아()
        {
            var d = K.Data(heroes: UR, cards: UR_C);
            var b = K.Fight(d, new[] { "ur2", "c" }, new[] { "dummy" });
            Assert.AreEqual("gd", b.Forms["ur2"].Id, "시작 태세 선");
            K.Hand(b, "u1", "u1", "stay", "hit_c"); b.Ap = 9;
            K.Play(b, "u1");
            Assert.AreEqual(1, b.StackOf("ur2", "영역"), "덤은 내는 순간의 태세(선)");
            Assert.AreEqual("ev", b.Forms["ur2"].Id, "낸 뒤 악으로");
            K.Play(b, "u1");
            Assert.AreEqual(11, b.StackOf("ur2", "영역"), "악 덤");
            Assert.AreEqual("gd", b.Forms["ur2"].Id);
            K.Play(b, "stay");
            Assert.AreEqual("gd", b.Forms["ur2"].Id, "이번엔 태세 유지");
            K.Play(b, "hit_c");
            Assert.AreEqual("gd", b.Forms["ur2"].Id, "동료 카드는 바꾸지 않는다");
            var t = new CardText(d);
            StringAssert.Contains("「선」:", t.Card(d.View("u1"))); StringAssert.Contains("「악」:", t.Card(d.View("u1")));
            StringAssert.Contains("이번엔 태세 유지", t.Card(d.View("stay")));
            // 한 바퀴(선 → 악 → 선) 계기 formLap · 한 턴 n번 상한
            var lap = K.Data(heroes: UR.Replace("startForm:'gd',", "startForm:'gd', passives:[{name:'순환', when:{on:'formLap'}, limit:{per:'turn', n:1}, fx:[{k:'gauge', v:5}]}],"), cards: UR_C);
            var lb = K.Fight(lap, new[] { "ur2", "c" }, new[] { "dummy" }); lb.Gauge = 0;
            K.Hand(lb, "u1", "u1", "u1", "u1");
            K.Play(lb, "u1"); Assert.AreEqual(0, lb.Gauge, "시작 태세에서 한 번 — 아직 한 바퀴가 아니다");
            K.Play(lb, "u1"); Assert.AreEqual(5, lb.Gauge, "선으로 돌아옴 — 한 바퀴");
            K.Play(lb, "u1"); K.Play(lb, "u1"); Assert.AreEqual(5, lb.Gauge, "한 턴 1번 상한");
            StringAssert.Contains("태세가 한 바퀴 돌 때마다", new CardText(lap).Detail(lap.Hero("ur2").Passives[0]));
            var bad = K.Data(heroes: UR.Replace("toggle:'gd'", "toggle:'zz'"), cards: UR_C);
            Assert.IsTrue(Validator.Check(bad).Errors.Any(e => e.Contains("toggle")));
        }

        // ── 벨라 「존속의 촛불」(엘다인 4차) — 동료 한 명에게 붙어 그 동료 카드마다 타며 실드 · 모아 옮기기 · 적 차례 끝 실드 남음 ──
        const string BE = @"[{id:'be2', name:'벨', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'촛불', carrier:'hero', cap:5, hunt:true, burn:{on:'play', shield:0.3}}},
 {id:'ur3', name:'로', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'영역 위', carrier:'hero', cap:1, hunt:true}}]";
        const string BE_C = @"[{id:'light', name:'첫 번째 촛불', hero:'be2', cost:0, type:'스킬', fx:[{k:'stack', id:'촛불', v:3, target:'oneAlly'}]},
 {id:'gather', name:'모으기', hero:'be2', cost:0, type:'스킬', fx:[{k:'moveMark', id:'촛불', target:'oneAlly'}]},
 {id:'stay2', name:'버팀', hero:'be2', cost:0, type:'스킬', fx:[{k:'shield', ratio:1}, {k:'when', on:'shieldLeft'}, {k:'gauge', v:10}]},
 {id:'area2', name:'영역 위', hero:'ur3', cost:0, type:'스킬', fx:[{k:'stack', id:'영역 위', v:1, target:'oneAlly'}]}]";
        [Test] public void 촛불은_붙은_동료_카드마다_타며_실드()
        {
            var d = K.Data(heroes: BE, cards: BE_C);
            var b = K.Fight(d, new[] { "be2", "c", "ur3" }, new[] { "dummy" });
            var c = b.HeroUnit("c"); var be = b.HeroUnit("be2"); b.Ap = 9;
            K.Hand(b, "light", "light", "hit_c", "area2");
            K.Play(b, "light", c.Idx, new PlayOpts { Ally = be.Idx });
            Assert.AreEqual(0, b.StackOf("be2", "촛불"), "주인 자신에게는 안 붙는다");
            K.Play(b, "light", c.Idx, new PlayOpts { Ally = c.Idx });
            Assert.AreEqual(3, b.StackOf("c", "촛불"));
            K.Play(b, "area2", c.Idx, new PlayOpts { Ally = c.Idx });
            Assert.AreEqual(1, b.StackOf("c", "영역 위"), "영역 위와 같은 동료에게 둘 다");
            Assert.IsTrue(b.StatusViews(c).Any(s => s.Id == "촛불" && s.Stacks == 3), "동료 칩에 남은 수");
            int sh0 = b.Pool.Shield; K.Play(b, "hit_c");
            Assert.AreEqual(2, b.StackOf("c", "촛불")); Assert.AreEqual(sh0 + 15, b.Pool.Shield, "벨라 방어력 50 × 30%");
            // 모아 옮기기
            b.Stacks["ur3"] = new Dictionary<string, int> { ["촛불"] = 2 };
            K.Hand(b, "gather"); K.Play(b, "gather", c.Idx, new PlayOpts { Ally = c.Idx });
            Assert.AreEqual(4, b.StackOf("c", "촛불")); Assert.AreEqual(0, b.StackOf("ur3", "촛불"));
            var t = new CardText(d);
            StringAssert.Contains("붙은 동료가 자기 카드를 낼 때마다 1 탐", t.Keyword(d.Hero("be2").Keyword));
            StringAssert.Contains("적 차례가 끝날 때 실드가 남았으면", t.Card(d.View("stay2")));
        }

        /// <summary>(2026-10-11 벨라 힐러 겸 탱커) burn.heal — 촛불이 탈 때 파티 HP 회복(주인 방어력 기준).</summary>
        [Test] public void 촛불은_타며_파티를_회복()
        {
            var d = K.Data(heroes: BE.Replace("burn:{on:'play', shield:0.3}", "burn:{on:'play', shield:0.3, heal:0.4}"), cards: BE_C);
            var b = K.Fight(d, new[] { "be2", "c" }, new[] { "dummy" });
            var c = b.HeroUnit("c"); b.Ap = 9;
            K.Hand(b, "light", "hit_c");
            K.Play(b, "light", c.Idx, new PlayOpts { Ally = c.Idx });
            b.Pool.Hp = b.Pool.MaxHp - 300; int hp0 = b.Pool.Hp;
            K.Play(b, "hit_c");
            Assert.AreEqual(2, b.StackOf("c", "촛불"));
            Assert.Greater(b.Pool.Hp, hp0, "촛불이 타며 파티 HP 회복");
            StringAssert.Contains("회복 40%(벨의 방어력)", new CardText(d).Keyword(d.Hero("be2").Keyword));
        }

        /// <summary>(2026-10-12 밤 벨라 파티 촛불) carrier self + burn — 주인이 쥔 촛불이 주인 말고 누가 카드를 내든 1 탄다. 고를 동료 없음.</summary>
        [Test] public void 파티_촛불은_주인_말고_누가_내든_탐()
        {
            var d = K.Data(heroes: BE.Replace("carrier:'hero', cap:5, hunt:true, burn:{on:'play', shield:0.3}", "carrier:'self', cap:5, burn:{on:'play', shield:0.3, heal:0.2}"),
                cards: BE_C.Replace("{k:'stack', id:'촛불', v:3, target:'oneAlly'}", "{k:'stack', id:'촛불', v:3}"));
            Assert.IsFalse(Validator.Check(d).Errors.Any(e => e.Contains("burn")), "carrier self 에도 burn");
            var b = K.Fight(d, new[] { "be2", "c", "ur3" }, new[] { "dummy" });
            var c = b.HeroUnit("c"); b.Ap = 9;
            K.Hand(b, "light", "light", "hit_c", "area2");
            K.Play(b, "light", c.Idx);
            Assert.AreEqual(3, b.StackOf("be2", "촛불"), "주인에게 쌓임");
            K.Play(b, "light", c.Idx);
            Assert.AreEqual(5, b.StackOf("be2", "촛불"), "주인 카드로는 타지 않음(최대 5)");
            int sh0 = b.Pool.Shield; K.Play(b, "hit_c");
            Assert.AreEqual(4, b.StackOf("be2", "촛불")); Assert.AreEqual(sh0 + 15, b.Pool.Shield, "벨 방어력 50 × 30%");
            K.Play(b, "area2", c.Idx, new PlayOpts { Ally = c.Idx });
            Assert.AreEqual(3, b.StackOf("be2", "촛불"), "다른 동료 카드에도 탐");
            StringAssert.Contains("벨 말고 동료가 카드를 낼 때마다 1 탐 · 실드 30% · 회복 20%(벨의 방어력)", new CardText(d).Keyword(d.Hero("be2").Keyword));
        }

        /// <summary>(2026-10-11 실비아) per from marked — 적 표식 「티파티」 는 「초대장」 이 붙은 아군이 칠 때만 받는 피해 증가.</summary>
        [Test] public void 초대장_붙은_아군이_티파티_적을_치면_피해_증가()
        {
            var d = K.Data(heroes: @"[{id:'sv', name:'실', role:'탱커', hp:800, atk:100, def:50, crit:0,
  keyword:{name:'티파', carrier:'enemy', cap:1, per:[{stat:'taken', v:0.5, from:'marked', mark:'초대'}]}, keywords:[{name:'초대', carrier:'hero', cap:1}]}]",
                cards: @"[{id:'tp', name:'장갑', hero:'sv', cost:0, type:'스킬', fx:[{k:'stack', id:'티파', v:1, target:'oneEnemy'}]},
 {id:'iv', name:'초대', hero:'sv', cost:0, type:'스킬', fx:[{k:'stack', id:'초대', v:1, target:'oneAlly'}]}]");
            var b = K.Fight(d, new[] { "sv", "c" }, new[] { "dummy" }); b.Ap = 9;
            var c = b.HeroUnit("c");
            K.Hand(b, "hit_c", "tp", "hit_c", "iv", "hit_c");
            int h0 = K.Hp(b); K.Play(b, "hit_c"); int plain = h0 - K.Hp(b);
            K.Play(b, "tp");
            h0 = K.Hp(b); K.Play(b, "hit_c"); Assert.AreEqual(plain, h0 - K.Hp(b), "초대장 없으면 그대로");
            K.Play(b, "iv", 0, new PlayOpts { Ally = c.Idx });
            h0 = K.Hp(b); K.Play(b, "hit_c"); Assert.AreEqual(plain * 1.5, h0 - K.Hp(b), 2, "초대장 + 티파티 = 받는 피해 +50%");
            StringAssert.Contains("「초대」가 붙은 아군에게 받는 피해 +50%", new CardText(d).Keyword(d.Hero("sv").Keyword));
        }

        [Test] public void 낸_뒤_적_차례가_끝날_때_실드가_남았으면()
        {
            var d = K.Data(heroes: BE, cards: BE_C);
            var b = K.Fight(d, new[] { "be2" }, new[] { "dummy" }); b.Gauge = 0;
            K.Hand(b, "stay2"); K.Play(b, "stay2");
            int g = b.Gauge; b.EndTurn();
            Assert.AreEqual(g + 10, b.Gauge, "실드가 남았다 — 뒤 효과");
            var x = K.Fight(d, new[] { "be2" }, new[] { "hitter" }); x.Gauge = 0;
            K.Hand(x, "stay2"); K.Play(x, "stay2"); int gx = x.Gauge; x.Pool.Shield = 0; x.EndTurn();
            Assert.AreEqual(gx, x.Gauge, "실드가 없으면 안 돈다");
        }

        [Test] public void 봇은_촛불을_카드를_많이_낼_동료에게()
        {
            var d = K.Data(heroes: BE, cards: BE_C);
            var b = K.Fight(d, new[] { "be2", "c", "b" }, new[] { "dummy" });
            K.Hand(b, "light", "hit_c", "hit_c"); b.Ap = 0;
            b.Draw.Clear(); b.Draw.AddRange(new[] { "hit_c", "hit_c", "hit_c", "hit_b" });
            new Bots(d).SmartPlay(b);
            Assert.AreEqual(3, b.StackOf("c", "촛불"), "카드를 많이 낼 동료(다)에게");
        }

        // ── 아야 「만년설의 앎」(엘다인 4차) — 적의 수 종류를 적음 · 아는 수면 · 아는 수 적은 2회 · 적 차례마다 한 번 ──
        const string AY = @"[{id:'ay2', name:'아', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'만년설의 앎', cap:4, known:true}}]";
        const string AY_C = @"[{id:'learn1', name:'보기', hero:'ay2', cost:0, type:'스킬', fx:[{k:'learn', target:'oneEnemy'}]},
 {id:'learnAll', name:'다 보기', hero:'ay2', cost:0, type:'스킬', fx:[{k:'learn', target:'allEnemies'}]},
 {id:'cut2', name:'발도', hero:'ay2', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}, {k:'ifFoe', id:'known', once:'foeTurn'}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'storm', name:'눈보라', hero:'ay2', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.5, target:'allEnemies', knownHits:2}]},
 {id:'gift', name:'싸락나비', hero:'c', cost:0, type:'스킬', fx:[{k:'learn', target:'oneEnemy', owner:'ay2'}]}]";
        [Test] public void 앎_적의_수를_적고_아는_수면_덤()
        {
            var d = K.Data(heroes: AY, cards: AY_C);
            var b = K.Fight(d, new[] { "ay2", "c" }, new[] { "hitter", "dummy" });
            Assert.AreEqual("attack", Battle.IntentKind(b.Enemies[0].Intent)); Assert.AreEqual("jam", Battle.IntentKind(b.Enemies[1].Intent));
            K.Hand(b, "cut2", "learn1", "cut2", "cut2");
            int h0 = K.Hp(b); K.Play(b, "cut2", 0);
            Assert.AreEqual(100, h0 - K.Hp(b), "모르는 수 — 덤 없음");
            K.Play(b, "learn1", 0);
            Assert.AreEqual(1, b.StackOf("ay2", "만년설의 앎")); CollectionAssert.AreEqual(new[] { "공격" }, b.KnownKinds("ay2"));
            h0 = K.Hp(b); K.Play(b, "cut2", 0);
            Assert.AreEqual(200, h0 - K.Hp(b), "아는 수(공격) — 덤");
            h0 = K.Hp(b); K.Play(b, "cut2", 0);
            Assert.AreEqual(100, h0 - K.Hp(b), "같은 적 · 같은 턴엔 한 번만(once foeTurn)");
            // 아는 수를 하는 적만 2회 · 동료 생성 카드가 아야의 앎에
            K.Hand(b, "storm", "gift"); h0 = K.Hp(b, 0); int d0 = K.Hp(b, 1);
            K.Play(b, "storm");
            Assert.AreEqual(100, h0 - K.Hp(b, 0), "아는 수 적은 2회"); Assert.AreEqual(50, d0 - K.Hp(b, 1), "모르는 수 적은 1회");
            K.Play(b, "gift", 1);
            Assert.AreEqual(2, b.StackOf("ay2", "만년설의 앎"), "동료 카드가 아야의 앎에(방해)");
            var t = new CardText(d);
            StringAssert.Contains("대상의 수를 「만년설의 앎」에 적음", t.Card(d.View("learn1")));
            StringAssert.Contains("아는 수면", t.Card(d.View("cut2")));
            StringAssert.Contains("아는 수를 하는 적은 2회", t.Card(d.View("storm")));
        }

        // ── 벨라 존속 — 지금 붙은 동료(holder) · 동료 누구에게 붙어 있든(who any) ──
        [Test] public void 존속_붙은_동료에게_보충_누구에게든_센다()
        {
            var d = K.Data(heroes: BE, cards: BE_C.TrimEnd(']') + @", {id:'refill', name:'나눠 주기', hero:'be2', cost:0, type:'스킬', fx:[{k:'stack', id:'촛불', v:2, target:'holder'}]},
 {id:'count', name:'세기', hero:'be2', cost:0, type:'스킬', fx:[{k:'perStack', id:'촛불', who:'any'}, {k:'shield', ratio:0.1}, {k:'ifStack', id:'촛불', who:'any', not:true}, {k:'gauge', v:10}]},
 {id:'blow', name:'끄기', hero:'be2', cost:0, type:'공격', fx:[{k:'spend', id:'촛불', all:true, who:'any'}, {k:'perEvent'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "be2", "c" }, new[] { "dummy" }); b.Gauge = 0;
            var c = b.HeroUnit("c");
            K.Hand(b, "refill", "light", "refill", "count", "blow", "count");
            K.Play(b, "refill"); Assert.AreEqual(0, b.StackOf("c", "촛불"), "붙은 동료가 없으면 아무 일 없음");
            K.Play(b, "light", c.Idx, new PlayOpts { Ally = c.Idx }); K.Play(b, "refill");
            Assert.AreEqual(5, b.StackOf("c", "촛불"), "지금 붙은 동료에게 +2");
            int sh0 = b.Pool.Shield; K.Play(b, "count");
            Assert.AreEqual(sh0 + 25, b.Pool.Shield, "동료에게 붙은 5겹 × 실드 10%(방어 50)"); Assert.AreEqual(0, b.Gauge);
            int h0 = K.Hp(b); K.Play(b, "blow");
            Assert.AreEqual(50, h0 - K.Hp(b), "모두 써서 1당 10% × 5"); Assert.AreEqual(0, b.StackOf("c", "촛불"));
            K.Play(b, "count"); Assert.AreEqual(10, b.Gauge, "없으면");
            StringAssert.Contains("「촛불」 +2", new CardText(d).Card(d.View("refill")));
        }

        [Test] public void 봇은_태세를_들고_덤을_쓴_뒤에_동료_공격()
        {
            var d = D(); var bot = new Bots(d);
            var b = K.Fight(d, new[] { "rk", "c" }, new[] { "dummy" });
            K.Hand(b, "cut", "draw_in"); b.Ap = 1;
            var log = new List<string>();
            bot.SmartPlay(b, trace: (m, v, st) => log.Add(m));
            Assert.IsTrue(log.Count >= 2 && log[0].Contains("발도") && log[1].Contains("멸"), "태세를 먼저 들고 덤 카드: " + string.Join(" | ", log));
            var x = K.Fight(d, new[] { "rk", "c" }, new[] { "dummy" });
            K.Hand(x, "draw_in"); x.Ap = 9; K.Play(x, "draw_in");
            K.Hand(x, "hit_c", "cut");
            var log2 = new List<string>();
            bot.SmartPlay(x, trace: (m, v, st) => log2.Add(m));
            int iCut = log2.FindIndex(m => m.Contains("멸")), iC = log2.FindIndex(m => m.Contains("다 때리기"));
            Assert.IsTrue(iCut >= 0 && (iC < 0 || iCut < iC), "태세를 푸는 동료 공격보다 덤 카드를 먼저: " + string.Join(" | ", log2));
        }
    }
}
