using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 엘다인 4차(2026-10-11, Docs/데이터.md §25 · 02 E76~E87) — 이번 변신 중 준 피해 · n번째 이후(정각) · 고학년 요구 게이지 · 처치하면 한 대 더 ·
    /// 이번 턴 신속 · 표시된 아군 증감 · 넘친 회복 한 번 · 치명 피해 상태 · 확인 묶음(감응 · 연쇄 · 축복 · 공명 연속) · 맞은 대 수 ·
    /// 데이터 담당 요청(이 카드 치명 · 적이 하나뿐이면 · 다른 아군 다음 비싼 카드 · 거르개 minCost) · 소유 키워드 검사 · 봇 때 보고 쥐기 · 글.
    /// </summary>
    public class Eldain5Tests
    {
        const string HEROES = @"[
 {id:'nr', name:'네', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'신앙', cap:9},
  forms:[{id:'holy', name:'성전', turns:3}, {id:'rest', name:'허기', turns:3}]},
 {id:'rn', name:'리', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'박', cap:9}},
 {id:'uk', name:'우', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'예열', cap:4, per:[{stat:'ultCost', v:-0.25}]},
  ult:{name:'행복', cost:100, fx:[{k:'draw', v:1}]}},
 {id:'ep', name:'에', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'근', cap:9}},
 {id:'ur', name:'로', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'영역', cap:5, per:[{stat:'atk', v:0.1, who:'marked', mark:'영역 위'}]},
  keywords:[{name:'영역 위', carrier:'hero', cap:1, hunt:true}]},
 {id:'ow', name:'저', role:'서포터', hp:600, atk:100, def:40, crit:0, keyword:{name:'저금', cap:99, rules:[{name:'저금', when:{on:'overheal', who:'any', once:true}, fx:[{k:'stack', id:'저금', v:1}]}]}},
 {id:'bs', name:'신', role:'딜러', hp:600, atk:100, def:20, crit:100, keyword:{name:'새싹', cap:9}},
 {id:'ln', name:'란', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'늑대', carrier:'enemy', cap:9}},
 {id:'ay', name:'아', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'나비', cap:99}},
 {id:'bb', name:'비', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'상자', cap:9}},
 {id:'sp', name:'공', role:'서포터', nature:'공명', hp:600, atk:100, def:40, crit:0, keyword:{name:'공', cap:9}},
 {id:'pu', name:'순', role:'딜러', nature:'순수', hp:600, atk:100, def:20, crit:0, keyword:{name:'흐름', cap:9, rules:[{name:'흐름', when:{on:'play', chain:true}, fx:[{k:'stack', id:'흐름', v:1}]}]}}
]";
        const string CARDS = @"[
 {id:'enter', name:'선포', hero:'nr', cost:0, type:'스킬', fx:[{k:'form', id:'holy'}]},
 {id:'enter2', name:'허기', hero:'nr', cost:0, type:'스킬', fx:[{k:'form', id:'rest'}]},
 {id:'nhit', name:'벼락', hero:'nr', cost:0, type:'공격', fx:[{k:'dmg', ratio:2, target:'oneEnemy'}]},
 {id:'axe', name:'도끼창', hero:'nr', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}, {k:'dmg', ratio:1, target:'oneEnemy', ofForm:0.1}]},
 {id:'rz', name:'싼 박자', hero:'rn', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'clock', name:'시계', hero:'rn', cost:0, type:'공격', fx:[{k:'ifNth', n:3, after:true, id:'정각'}, {k:'stack', id:'박', v:1}]},
 {id:'pre', name:'당김', hero:'rn', cost:0, type:'스킬', fx:[{k:'nthNext', v:1, id:'정각'}]},
 {id:'stop', name:'정지', hero:'rn', cost:0, type:'스킬', fx:[{k:'swiftTurn'}]},
 {id:'six', name:'6초', hero:'rn', cost:1, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:0.2, target:'oneEnemy'}, {k:'ifNth', n:4, after:true, id:'정각'}, {k:'dmg', ratio:3, target:'oneEnemy'}, {k:'stack', id:'박', v:1}]},
 {id:'heat', name:'예열', hero:'uk', cost:0, type:'스킬', fx:[{k:'stack', id:'예열', v:2}]},
 {id:'cheap', name:'싸게', hero:'uk', cost:0, type:'스킬', fx:[{k:'ultCostMod', v:-0.2}]},
 {id:'barrage', name:'난타', hero:'ep', cost:0, x:true, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy', xHits:true, killMore:true, max:4}]},
 {id:'area', name:'영역 펼치기', hero:'ur', cost:0, type:'스킬', fx:[{k:'stack', id:'영역', v:3}]},
 {id:'markc', name:'영역 위로', hero:'ur', cost:0, type:'스킬', fx:[{k:'stack', id:'영역 위', v:1, target:'oneAlly'}]},
 {id:'twoheal', name:'두 번 회복', hero:'ow', cost:0, type:'스킬', fx:[{k:'heal', ratio:1}, {k:'heal', ratio:1}]},
 {id:'holy', name:'신성', hero:'bs', cost:0, type:'스킬', fx:[{k:'critDmg', v:0.4, turns:2, id:'신성', off:[{k:'status', id:'취약', v:1, target:'allEnemies'}]}]},
 {id:'bhit', name:'신 치기', hero:'bs', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'bear', name:'맞으면', hero:'bb', cost:0, type:'스킬', fx:[{k:'ifHitTaken', n:4}, {k:'stack', id:'상자', v:1}]},
 {id:'sure', name:'확정', hero:'ln', cost:0, type:'공격', fx:[{k:'critNext', now:true}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'claw', name:'발톱', hero:'ln', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}, {k:'critNext', now:true, foeStack:'늑대'}]},
 {id:'wolf', name:'늑대 걸기', hero:'ln', cost:0, type:'스킬', fx:[{k:'stack', id:'늑대', v:1, target:'oneEnemy'}]},
 {id:'solo', name:'홀로', hero:'ay', cost:0, type:'공격', fx:[{k:'ifFoe', id:'alone'}, {k:'stack', id:'나비', v:1}]},
 {id:'kind', name:'착한 마음씨', hero:'bb', cost:0, type:'스킬', fx:[{k:'empower', who:'other', minCost:2, ratio:0.5}]},
 {id:'dr2', name:'꿈', hero:'bb', cost:0, type:'스킬', fx:[{k:'draw', v:1, minCost:2}]},
 {id:'big_c', name:'다 큰 한 방', hero:'c', cost:2, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'butter', name:'나비 한 장', hero:'ay', cost:0, type:'공격', tags:['망각', '소멸'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'when', on:'drawAny'}, {k:'stack', id:'나비', v:1}]},
 {id:'frog', name:'개굴비', hero:'ay', cost:0, type:'스킬', tags:['연쇄'], fx:[{k:'stack', id:'나비', v:1}]},
 {id:'bread', name:'빵', hero:'bb', token:true, cost:0, type:'스킬', tags:['축복'], fx:[]},
 {id:'bake', name:'빵 굽기', hero:'bb', cost:0, type:'스킬', fx:[{k:'make', id:'bread'}]},
 {id:'sp_c', name:'공명 카드', hero:'sp', cost:0, type:'스킬', fx:[]},
 {id:'pu_c', name:'순수 카드', hero:'pu', cost:0, type:'스킬', fx:[]},
 {id:'tease', name:'오시려구요', hero:'bb', cost:0, type:'스킬', tags:['보존'], fx:[{k:'taunt', v:0.5}]},
 {id:'fount', name:'은분수', hero:'bb', cost:1, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:0.3, target:'allEnemies'}, {k:'ifFoe', id:'anyAttack'}, {k:'dmg', ratio:2, target:'allEnemies'}]},
 {id:'sword', name:'영웅의 검', hero:'ln', cost:1, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:0.3, target:'oneEnemy'}, {k:'execute', pct:0.1}]}
]";
        const string FOES = @"[
 {id:'weak', name:'약골', hp:50, intents:[{t:'jam', v:0, rush:0}]},
 {id:'multi4', name:'네 번', hp:1000, intents:[{t:'multi', v:10, n:4, rush:0}]},
 {id:'rusher', name:'급한 놈', hp:1000, intents:[{t:'attack', v:10, rush:5}]},
 {id:'smash', name:'박살', hp:1000, intents:[{t:'attack', v:400, rush:0}]}
]";
        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(string[] party, params string[] foes) => K.Fight(D(), party, foes.Length > 0 ? foes : new[] { "dummy" });

        // ── E76 이번 변신 중 준 피해 ──
        [Test] public void E76_이번_변신_중_준_피해의_비율_고정_피해()
        {
            var b = F(new[] { "nr" });
            Assert.AreEqual(0, b.FormDealt("nr"), "변신 밖에선 0");
            K.Hand(b, "enter", "nhit", "axe"); K.Play(b, "enter"); K.Play(b, "nhit");
            Assert.AreEqual(200, b.FormDealt("nr"), "변신 중 준 피해를 모은다");
            int before = K.Hp(b);
            K.Play(b, "axe");
            Assert.AreEqual(100 + 30, before - K.Hp(b), "도끼창 100 뒤 이번 변신 피해 300 의 10% 고정");
            K.Hand(b, "enter2"); K.Play(b, "enter2");
            Assert.AreEqual(0, b.FormDealt("nr"), "새 변신에 들면 0 부터");
        }

        // ── E77 n번째 이후 · nthNext ──
        [Test] public void E77_n번째_이후면_정각_다음_카드_당겨_셈()
        {
            var b = F(new[] { "rn" });
            K.Hand(b, "clock", "rz", "rz", "clock");
            Assert.AreEqual(1, b.NthNow);
            K.Play(b, "clock");
            Assert.AreEqual(0, b.StackOf("rn", "박"), "첫 카드는 정각이 아니다");
            K.Play(b, "rz"); K.Play(b, "rz");
            K.Play(b, "clock");
            Assert.AreEqual(1, b.StackOf("rn", "박"), "4번째(3번째 이후)면 정각");
            var c = F(new[] { "rn" });
            K.Hand(c, "pre", "clock"); K.Play(c, "pre");
            Assert.IsTrue(c.NthNextOn("rn"));
            K.Play(c, "clock");
            Assert.AreEqual(1, c.StackOf("rn", "박"), "당겨 셈 — 다음 자신의 카드는 정각");
            Assert.IsFalse(c.NthNextOn("rn"));
        }

        // ── E78 고학년 요구 게이지 ──
        [Test] public void E78_고학년_요구_게이지_증감과_하한()
        {
            var b = K.Fight(D(), new[] { "uk" }, new[] { "dummy" }, st => st.Gauge = 60);
            Assert.AreEqual(100, b.UltCostOf("uk"));
            Assert.IsNotNull(b.CanUlt("uk"));
            K.Hand(b, "heat"); K.Play(b, "heat");
            Assert.AreEqual(50, b.UltCostOf("uk"), "1개당 -25% × 2");
            Assert.IsNull(b.CanUlt("uk"), "게이지 60 ≥ 50");
            K.Hand(b, "heat", "cheap"); K.Play(b, "heat"); K.Play(b, "cheap");
            Assert.AreEqual(50, b.UltCostOf("uk"), "하한 50%");
            Assert.IsTrue(b.UseUlt("uk").Ok);
            Assert.AreEqual(10, b.Gauge, "깎인 요구만큼 쓴다");
        }

        // ── E79 처치하면 한 대 더 ──
        [Test] public void E79_X_다타_중_처치하면_한_대_더()
        {
            var b = F(new[] { "ep" }, "weak", "weak", "weak", "dummy");
            b.Ap = 1;
            K.Hand(b, "barrage"); K.Play(b, "barrage");
            Assert.IsTrue(b.Enemies.Take(3).All(e => e.Dead), "1대 → 처치마다 한 대 더");
            Assert.AreEqual(1000 - 100, K.Hp(b, 3), "넷째 대는 다음 적에게 · max 4");
        }

        // ── E80 이번 턴 남은 카드 신속 ──
        [Test] public void E80_이번_턴_남은_카드_신속()
        {
            var b = F(new[] { "rn" }, "rusher");
            K.Hand(b, "rz", "stop", "rz", "rz");
            K.Play(b, "rz");
            int r1 = K.E(b).RushCnt;
            Assert.Greater(r1, 0, "신속이 아닌 카드는 행동 카운트를 줄인다");
            K.Play(b, "stop");
            Assert.IsTrue(b.SwiftTurnOn);
            int r2 = K.E(b).RushCnt;
            K.Play(b, "rz"); K.Play(b, "rz");
            Assert.AreEqual(r2, K.E(b).RushCnt, "남은 카드는 신속");
        }

        // ── E81 표시된 아군에게만 ──
        [Test] public void E81_표시된_아군에게만_공격력()
        {
            var b = F(new[] { "ur", "c" });
            var c = b.HeroUnit("c"); var ur = b.HeroUnit("ur");
            K.Hand(b, "area"); K.Play(b, "area");
            Assert.AreEqual(150, b.AtkNow(c), "표시가 없으면 아무에게도");
            K.Hand(b, "markc"); K.Play(b, "markc", c.Idx, new PlayOpts { Ally = c.Idx });
            Assert.AreEqual(195, b.AtkNow(c), "영역 3 × 10% — 표시된 아군만");
            Assert.AreEqual(100, b.AtkNow(ur), "주인도 표시가 없으면 아니다");
            K.Hand(b, "markc"); K.Play(b, "markc", ur.Idx, new PlayOpts { Ally = ur.Idx });
            Assert.AreEqual(150, b.AtkNow(c), "표시를 옮기면(hunt) 떨어진다");
            Assert.AreEqual(130, b.AtkNow(ur));
        }

        // ── E82 넘친 회복은 카드 한 장에 한 번 ──
        [Test] public void E82_누구의_회복이든_넘치면_카드_한_장에_한_번()
        {
            var b = F(new[] { "ow" });
            K.Hand(b, "twoheal"); K.Play(b, "twoheal");
            Assert.AreEqual(1, b.StackOf("ow", "저금"), "두 번 넘쳐도 한 번(once)");
        }

        // ── E83 치명 피해 상태 ──
        [Test] public void E83_치명_피해_증가_n턴_끝나면_효과()
        {
            int Dealt(bool holy)
            {
                var b = F(new[] { "bs" });
                if (holy) { K.Hand(b, "holy"); K.Play(b, "holy"); }
                K.Hand(b, "bhit"); int h0 = K.Hp(b); K.Play(b, "bhit");
                return h0 - K.Hp(b);
            }
            Assert.AreEqual(150, Dealt(false), "치명 ×1.5");
            Assert.AreEqual(210, Dealt(true), "치명 피해 +40% — ×1.5 × 1.4");
            var x = F(new[] { "bs" });
            K.Hand(x, "holy"); K.Play(x, "holy");
            Assert.AreEqual((0.4, 2), x.CritDmgState);
            x.EndTurn();
            Assert.AreEqual(0, x.St(K.E(x), "취약"));
            x.EndTurn();
            Assert.AreEqual(0.0, x.CritDmgState.v, "2턴 뒤 끝");
            Assert.AreEqual(1, x.St(K.E(x), "취약"), "끝날 때 효과 — 적 전체 취약");
        }

        // ── E84 확인 묶음 ──
        [Test] public void E84_망각으로_돌아온_카드가_뽑히면_감응()
        {
            var b = F(new[] { "ay" });
            K.Hand(b, "butter"); b.Draw.Clear(); b.Discard.Clear();
            K.Play(b, "butter");
            Assert.AreEqual("butter", b.Draw.LastOrDefault(), "망각 — 뽑을 더미 맨 위로");
            b.EndTurn();
            Assert.Contains("butter", b.Hand);
            Assert.AreEqual(1, b.StackOf("ay", "나비"), "뽑힐 때 감응");
        }

        [Test] public void E84_연쇄로_다시_돈_효과는_연쇄를_다시_걸지_않는다()
        {
            var b = F(new[] { "ay" });
            K.Hand(b, "frog"); K.Play(b, "frog");
            Assert.AreEqual(1, b.StackOf("ay", "나비"));
            b.EndTurn();
            Assert.AreEqual(2, b.StackOf("ay", "나비"), "다음 턴 한 번 더");
            b.EndTurn();
            Assert.AreEqual(2, b.StackOf("ay", "나비"), "다시 돈 것은 연쇄를 걸지 않는다");
        }

        [Test] public void E84_생성한_축복_카드가_손에서_버려지면_실드()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "bb" }, new[] { "dummy" }, null, cues);
            K.Hand(b, "bake"); K.Play(b, "bake");
            Assert.IsTrue(b.Hand.Any(h => GameData.BaseId(h) == "bread"));
            b.EndTurn();
            Assert.IsTrue(cues.Any(c => c.K == "shield" && c.V > 0), "턴 끝 손에서 버려진 생성 축복 카드 — 고정 실드");
        }

        [Test] public void E84_공명_카드는_어느_성격과도_연속()
        {
            Assert.IsTrue(Battle.ChainNat("순수", "공명")); Assert.IsTrue(Battle.ChainNat("공명", "광기")); Assert.IsFalse(Battle.ChainNat("순수", "광기"));
            var b = F(new[] { "sp", "pu" });
            K.Hand(b, "sp_c", "pu_c"); K.Play(b, "sp_c"); K.Play(b, "pu_c");
            Assert.AreEqual(1, b.StackOf("pu", "흐름"), "공명 카드 뒤 순수 카드 — 연속");
        }

        // ── E85 맞은 대 수 ──
        [Test] public void E85_이번_턴_맞은_대_수()
        {
            var b = F(new[] { "bb" }, "multi4");
            K.Hand(b, "bear"); K.Play(b, "bear");
            Assert.AreEqual(0, b.StackOf("bb", "상자"), "아직 안 맞았다");
            b.EndTurn();
            Assert.AreEqual(4, b.HitsTaken, "앞 적 차례 4대");
            K.Hand(b, "bear"); K.Play(b, "bear");
            Assert.AreEqual(1, b.StackOf("bb", "상자"));
        }

        // ── 데이터 담당 요청 ──
        [Test] public void 요청_이_카드_치명_표식_적이면_치명()
        {
            var b = F(new[] { "ln" });
            K.Hand(b, "sure"); int h0 = K.Hp(b); K.Play(b, "sure");
            Assert.AreEqual(150, h0 - K.Hp(b), "이 카드 치명");
            K.Hand(b, "claw"); h0 = K.Hp(b); K.Play(b, "claw");
            Assert.AreEqual(100, h0 - K.Hp(b), "표식이 없으면 치명 아님");
            K.Hand(b, "wolf", "claw"); K.Play(b, "wolf"); h0 = K.Hp(b); K.Play(b, "claw");
            Assert.AreEqual(150, h0 - K.Hp(b), "표식 적이면 치명(카드 뒤에 둬도 처음부터)");
        }

        [Test] public void 요청_적이_하나뿐이면()
        {
            var b = F(new[] { "ay" }, "dummy", "dummy");
            K.Hand(b, "solo"); K.Play(b, "solo");
            Assert.AreEqual(0, b.StackOf("ay", "나비"));
            var c = F(new[] { "ay" }, "dummy");
            K.Hand(c, "solo"); K.Play(c, "solo");
            Assert.AreEqual(1, c.StackOf("ay", "나비"));
        }

        [Test] public void 요청_다른_아군의_다음_비싼_카드_강화_거르개_비용()
        {
            var b = F(new[] { "bb", "c" });
            b.Ap = 9;
            K.Hand(b, "kind", "hit_c", "big_c"); K.Play(b, "kind");
            int h0 = K.Hp(b); K.Play(b, "hit_c");
            Assert.AreEqual(150, h0 - K.Hp(b), "비용 1 카드에는 안 쓴다");
            h0 = K.Hp(b); K.Play(b, "big_c");
            Assert.AreEqual(225, h0 - K.Hp(b), "비용 2 이상 다른 아군 카드 +50%");
            var x = F(new[] { "bb", "c" });
            K.Hand(x, "dr2"); x.Draw.Clear(); x.Draw.AddRange(new[] { "big_c", "hit_c", "hit_c" });
            K.Play(x, "dr2");
            Assert.Contains("big_c", x.Hand, "비용 2 이상 카드 1장 드로우");
        }

        // ── E86 소유 키워드 검사 ──
        static List<string> EldWarns(params (string hero, string cards)[] hs)
        {
            var d = K.Data(heroes: "[" + string.Join(",", hs.Select(h => h.hero)) + "]");
            foreach (var h in hs) d.Add(null, h.cards, null, null, null, null);
            return Validator.Check(d).Warnings.Where(e => e.StartsWith("엘다인", System.StringComparison.Ordinal)).ToList();
        }
        [Test] public void E86_소유_키워드_정산은_주인만_주의()
        {
            var w = EldWarns(EldainTests.Eld("벨라", uniqueFx: "{k:'taunt', v:0.5}"));
            Assert.IsTrue(w.Any(x => x.Contains("「도발」은") && x.Contains("소유 키워드")), string.Join("\n", w));
            Assert.IsFalse(EldWarns(EldainTests.Eld("비비", uniqueFx: "{k:'taunt', v:0.5}")).Any(x => x.Contains("소유 키워드")), "주인은 괜찮다");
            Assert.IsTrue(EldWarns(EldainTests.Eld("이드", uniqueFx: "{k:'dmg', ratio:1, target:'oneEnemy'}, {k:'ifFoe', id:'취약'}, {k:'dmg', ratio:1, target:'oneEnemy'}")).Any(x => x.Contains("「취약」")), "취약 정산은 비비만");
            Assert.IsFalse(EldWarns(EldainTests.Eld("이드", uniqueFx: "{k:'status', id:'취약', v:1}")).Any(x => x.Contains("소유 키워드")), "걸어 주기는 괜찮다");
        }

        [Test] public void E29_4차_2코_기본은_1장까지()
        {
            var two = EldainTests.Eld("이드");
            two.cards = two.cards.Replace("{id:'이드_s1', name:'기본1', hero:'이드', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, base:'def'", "{id:'이드_s1', name:'기본1', hero:'이드', cost:2, type:'공격', fx:[{k:'dmg', ratio:2.2, base:'def'");
            EldainTests.Has(EldainTests.EldErrors(two), "기본 덱은 가벼운");
            Assert.AreEqual(0, EldainTests.EldErrors(EldainTests.Eld("이드")).Count, "무거운 덱(1 · 2 · 1)은 된다");
        }

        /// <summary>가벼운 덱 — 1코 기본 공격 한 종을 시작 덱에 두 장(같은 카드 · 사용자 2026-10-11).</summary>
        static (string hero, string cards) Light(string id)
        {
            var e = EldainTests.Eld(id);
            e.cards = e.cards.Replace($"{{id:'{id}_s2', name:'기본2', hero:'{id}', cost:2, type:'공격', fx:[{{k:'dmg', ratio:2.2, base:'def', target:'oneEnemy'}}]}},", "");
            e.hero = e.hero.Replace($"starter:['{id}_s1','{id}_s2','{id}_s3'", $"starter:['{id}_s1','{id}_s1','{id}_s3'");
            return e;
        }

        [Test] public void E29_가벼운_덱은_같은_기본_공격_두_장()
        {
            var light = Light("이드");
            StringAssert.Contains("'이드_s1','이드_s1'", light.hero);
            Assert.AreEqual(0, EldainTests.EldErrors(light).Count, string.Join(" | ", EldainTests.EldErrors(light)));
            Assert.AreEqual(0, EldainTests.EldErrors(EldainTests.Eld("이드")).Count, "무거운 덱(1 · 2 · 1 각 1장)");
            // 같은 카드 2장은 1코 기본 공격만 · 서로 다른 1코 공격 두 종도 가벼운 덱이 아니다
            var bad = EldainTests.Eld("이드"); bad.hero = bad.hero.Replace("starter:['이드_s1','이드_s2','이드_s3'", "starter:['이드_s1','이드_s3','이드_s3'");
            EldainTests.Has(EldainTests.EldErrors(bad), "같은 카드 2장은 가벼운 덱의 1코 기본 공격만");
            var two = EldainTests.Eld("이드");
            two.cards = two.cards.Replace("{id:'이드_s2', name:'기본2', hero:'이드', cost:2, type:'공격', fx:[{k:'dmg', ratio:2.2, base:'def'", "{id:'이드_s2', name:'기본2', hero:'이드', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, base:'def'");
            EldainTests.Has(EldainTests.EldErrors(two), "기본 덱은 가벼운");
        }

        [Test] public void 같은_기본_카드_두_장_덱_전투_빛_제거()
        {
            var light = Light("이드");
            var d = K.Data(heroes: "[" + light.hero + "]");
            d.Add(null, light.cards, null, null, null, null);
            Assert.AreEqual(2, d.BuildDeck(new[] { "이드" }).Count(x => x == "이드_s1"), "시작 덱에 두 장");
            // 전투 — 두 장 다 손 · 더미에 있고, 빛(은총)은 한 장에만 붙는다
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { "이드" }, Enemies = new List<string> { "dummy" }, Deck = d.BuildDeck(new[] { "이드" }), EnemyHp = 1, Seed = 1, Glow = new Dictionary<string, Glow> { ["이드_s1"] = new Glow { Kind = "grace", Hero = "이드", Options = new List<string> { "이드_u2" } } } }, null);
            var all = b.Hand.Concat(b.Draw).Concat(b.Discard).ToList();
            Assert.AreEqual(2, all.Count(x => GameData.BaseId(GameData.NoInst(x)) == "이드_s1"));
            Assert.AreEqual(1, all.Count(x => b.GlowOf(x) != null), "빛은 한 장에만");
            // 판 — 상점에서 한 장 빼면 한 장 남는다
            var run = Run.New(d, new List<string> { "이드" }, 1);
            Assert.AreEqual(2, run.S.Deck.Count(x => x == "이드_s1"));
            run.S.Gold = 9999; run.RollShop();
            Assert.IsNull(run.RemoveCard("이드_s1"));
            Assert.AreEqual(1, run.S.Deck.Count(x => x == "이드_s1"));
            var save = GameData.ToJson(run.S);
            Assert.AreEqual(1, Run.Load(d, save).S.Deck.Count(x => x == "이드_s1"), "저장 · 이어하기");
        }

        [Test] public void E29_스킬인데_적을_치면_오류_공격인데_안_치면_주의()
        {
            var sk = EldainTests.Eld("이드");
            sk.cards = sk.cards.Replace("{id:'이드_u3', name:'고유3', hero:'이드', unique:true, cost:1, type:'공격'", "{id:'이드_u3', name:'고유3', hero:'이드', unique:true, cost:1, type:'스킬'");
            EldainTests.Has(EldainTests.EldErrors(sk), "스킬인데 적에게 피해");
            Assert.AreEqual(0, EldainTests.EldErrors(EldainTests.Eld("이드")).Count(e => e.Contains("스킬인데")));
            var atk = EldainTests.Eld("이드", uniqueFx: "{k:'shield', ratio:1.2}, {k:'stack', id:'이드의 마음', v:1}");
            EldainTests.Has(EldainTests.EldErrors(atk), "공격인데 적 피해가 없다");   // 오류(사용자 2026-10-11)
            var sum = EldainTests.Eld("이드", uniqueFx: "{k:'summon', id:'이드의 마음', ratio:0.4}");
            Assert.IsFalse(EldainTests.EldErrors(sum).Any(x => x.Contains("이드_u2: 공격인데")), "소환물 따라 치기도 적 피해");
            // 신탁 type — 스킬로 바꾼 신탁이 적을 치면 오류
            var orc = EldainTests.Eld("이드");
            orc.cards = orc.cards.Replace("{name:'이드1o1', fx:[", "{name:'이드1o1', type:'스킬', fx:[");
            EldainTests.Has(EldainTests.EldErrors(orc), "이드_u1 신탁1: 스킬인데");
        }

        [Test] public void E78_E81_검사기_칸()
        {
            var d = K.Data(heroes: @"[{id:'zz', name:'즈', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'예', cap:4, per:[{stat:'ultCost', v:-0.25}, {stat:'atk', v:0.1, who:'marked', mark:'없는 표시'}]}}]",
                cards: @"[{id:'zz1', name:'즈1', hero:'zz', cost:0, type:'공격', fx:[{k:'stack', id:'예', v:1}, {k:'ifNth', n:2, id:'정각'}, {k:'dmg', ratio:1, target:'oneEnemy', killMore:true}]}]");
            var errs = Validator.Check(d).Errors;
            Assert.IsFalse(errs.Any(e => e.Contains("모르는 stat")), string.Join("\n", errs));
            Assert.IsTrue(errs.Any(e => e.Contains("who marked 의 mark")), string.Join("\n", errs));
            Assert.IsTrue(errs.Any(e => e.Contains("after 와 같이")), string.Join("\n", errs));
        }

        // ── E87 봇 때 보고 쥐기 ──
        [Test] public void E87_도발은_큰_공격이_올_때만()
        {
            var d = D(); var bot = new Bots(d);
            var b = K.Fight(d, new[] { "bb" }, new[] { "dummy" });
            K.Hand(b, "tease"); b.Pool.Shield = 300;
            Assert.AreEqual("taunt", HoldTiming.Of(b, "tease").Why);
            Assert.IsTrue(HoldTiming.Of(b, "tease").Hold, "공격이 없으면 쥘 때");
            bot.SmartPlay(b);
            Assert.Contains("tease", b.Hand, "칠 적이 없으면 쥔다");
            var x = K.Fight(d, new[] { "bb" }, new[] { "smash" });
            K.Hand(x, "tease"); x.Pool.Shield = 300;
            Assert.IsFalse(HoldTiming.Of(x, "tease").Hold);
            bot.SmartPlay(x);
            Assert.IsFalse(x.Hand.Contains("tease"), "큰 공격(400 ≥ 실드 절반) — 도발");
        }

        [Test] public void E87_공격_예고가_없으면_은분수를_쥔다()
        {
            var d = D(); var bot = new Bots(d);
            var b = K.Fight(d, new[] { "bb" }, new[] { "dummy" });
            K.Hand(b, "fount"); b.Ap = 3;
            bot.SmartPlay(b);
            Assert.Contains("fount", b.Hand, "예고가 없으면 쥔다");
            var x = K.Fight(d, new[] { "bb" }, new[] { "smash" });
            K.Hand(x, "fount"); x.Ap = 3;
            bot.SmartPlay(x);
            Assert.IsFalse(x.Hand.Contains("fount"), "공격 예고가 있으면 낸다");
        }

        [Test] public void E87_처형_선이_가까우면_검을_쥐고_선_안이면_낸다()
        {
            var d = D(); var bot = new Bots(d);
            var b = K.Fight(d, new[] { "ln" }, new[] { "dummy" });
            K.Hand(b, "sword"); b.Ap = 3; K.E(b).Hp = 250;
            Assert.IsTrue(HoldTiming.Of(b, "sword").Hold, "HP 25% — 다음 턴 안");
            bot.SmartPlay(b);
            Assert.Contains("sword", b.Hand, "처형 선까지 쥔다");
            var x = K.Fight(d, new[] { "ln" }, new[] { "dummy" });
            K.Hand(x, "sword"); x.Ap = 3; K.E(x).Hp = 80;
            bot.SmartPlay(x);
            Assert.IsTrue(K.E(x).Dead, "선 안이면 처형");
            var y = K.Fight(d, new[] { "ln" }, new[] { "dummy" });
            K.Hand(y, "sword"); y.Ap = 3;
            Assert.IsFalse(HoldTiming.Of(y, "sword").Hold, "선에서 멀면 쥘 까닭이 없다");
        }

        [Test] public void E87_정각에_닿지_못하면_쥐고_닿으면_싼_카드_먼저()
        {
            var d = D(); var bot = new Bots(d);
            var b = K.Fight(d, new[] { "rn" }, new[] { "dummy" });
            K.Hand(b, "six"); b.Ap = 3;
            Assert.IsTrue(HoldTiming.Of(b, "six").Hold, "4번째에 못 닿는다");
            bot.SmartPlay(b);
            Assert.Contains("six", b.Hand, "정각이 안 되는 턴은 쥔다");
            var x = K.Fight(d, new[] { "rn" }, new[] { "dummy" });
            K.Hand(x, "six", "rz", "rz", "rz"); x.Ap = 3;
            bot.SmartPlay(x);
            Assert.IsFalse(x.Hand.Contains("six"));
            Assert.AreEqual(1, x.StackOf("rn", "박"), "싼 카드 셋 뒤 정각에 낸다");
        }

        [Test] public void E87_실드를_헐_카드는_큰_공격이_오면_쥐고_넘긴다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS.TrimEnd(']') + @", {id:'ram', name:'들이받기', hero:'bb', cost:1, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:0.8, base:'def', target:'oneEnemy'}, {k:'spendShield', half:true}, {k:'dmg', ratio:1, ofEvent:2, target:'oneEnemy'}, {k:'when', on:'handEnd'}, {k:'status', id:'실드 유지', v:1}]}]", enemies: FOES);
            var bot = new Bots(d);
            var b = K.Fight(d, new[] { "bb" }, new[] { "smash" });
            K.Hand(b, "ram"); b.Pool.Shield = 500; b.Ap = 3;
            var t = HoldTiming.Of(b, "ram");
            Assert.AreEqual("shield", t.Why); Assert.IsTrue(t.Hold, "400 이 헐고 남을 250 을 넘는다");
            bot.SmartPlay(b);
            Assert.Contains("ram", b.Hand, "큰 공격 앞에서는 헐지 않고 쥐고 넘긴다");
            var x = K.Fight(d, new[] { "bb" }, new[] { "dummy" });
            K.Hand(x, "ram"); x.Pool.Shield = 500; x.Ap = 3;
            Assert.IsFalse(HoldTiming.Of(x, "ram").Hold);
            bot.SmartPlay(x);
            Assert.IsFalse(x.Hand.Contains("ram"), "칠 적이 없으면 헐어 친다");
        }

        [Test] public void E87_HP_선_덤_카드는_선이_가까우면_쥔다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS.TrimEnd(']') + @", {id:'hsw', name:'영웅의 검2', hero:'ln', cost:1, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:0.3, target:'oneEnemy'}, {k:'ifFoe', id:'hp', pct:0.3}, {k:'dmg', ratio:3, target:'oneEnemy'}]}]", enemies: FOES);
            var bot = new Bots(d);
            var b = K.Fight(d, new[] { "ln" }, new[] { "dummy" });
            K.Hand(b, "hsw"); b.Ap = 3; K.E(b).Hp = 450;
            var t = HoldTiming.Of(b, "hsw");
            Assert.AreEqual("hp", t.Why); Assert.IsTrue(t.Hold, "HP 45% — 선(30%) +30% 안");
            bot.SmartPlay(b);
            Assert.Contains("hsw", b.Hand, "선 아래로 올 때까지 쥔다");
            var x = K.Fight(d, new[] { "ln" }, new[] { "dummy" });
            K.Hand(x, "hsw"); x.Ap = 3; K.E(x).Hp = 250;
            Assert.IsFalse(HoldTiming.Of(x, "hsw").Hold);
            bot.SmartPlay(x);
            Assert.IsFalse(x.Hand.Contains("hsw"), "선 아래면 낸다");
        }

        // ── 리뉴아 「시간」(2026-10-11 사용자) — 아군이 낼 때마다 손의 카드 비용 -1 · 0 이면 정각 ──
        const string TIME_H = @"[{id:'tm', name:'시', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'시간', cap:9, cardValue:{grow:'allyPlay', reset:'play', cost:-1, max:6}}}]";
        const string TIME_C = @"[{id:'clockbig', name:'큰 시계', hero:'tm', unique:true, cost:4, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:1, target:'oneEnemy'}, {k:'ifCost', max:0, id:'정각'}, {k:'gauge', v:10}]},
 {id:'grow1', name:'자라기', hero:'tm', unique:true, cost:0, type:'공격', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}, {k:'ifFoe', id:'hp', pct:0.4}, {k:'dmg', ratio:1, target:'oneEnemy'}, {k:'cardStatus', id:'성장', v:1}]},
 {id:'zc', name:'다 숨', hero:'c', cost:0, type:'스킬', fx:[]},
 {id:'clockbig2', name:'작은 시계', hero:'tm', unique:true, cost:4, type:'스킬', fx:[{k:'draw', v:0}]}]";
        [Test] public void 시간_아군이_낼_때마다_비용이_줄고_0이면_정각()
        {
            var d = K.Data(heroes: TIME_H, cards: TIME_C);
            var b = K.Fight(d, new[] { "tm", "c" }, new[] { "dummy" });
            b.Ap = 9; b.Gauge = 0;
            K.Hand(b, "clockbig", "zc", "zc", "zc", "zc");
            Assert.AreEqual(4, b.CostOf("clockbig"));
            for (int i = 0; i < 4; i++) K.Play(b, "zc");
            Assert.AreEqual(0, b.CostOf("clockbig"), "아군 카드 넷 — 비용 4 → 0");
            K.Play(b, "clockbig");
            Assert.AreEqual(10, b.Gauge, "비용 0 으로 냄 — 정각 덤(게이지 +10)");
            K.Hand(b, "clockbig");
            Assert.AreEqual(4, b.CostOf("clockbig"), "낸 카드는 원래 비용으로");
            var t = new CardText(d);
            StringAssert.Contains("정각이면", t.Card(d.View("clockbig")));
            StringAssert.Contains("1당 비용 -1", t.Keyword(d.Hero("tm").Keyword));
            StringAssert.Contains("비용이 0", t.Tips()["정각"]);
            var s = t.Card(d.View("grow1"));
            StringAssert.Contains("HP 40% 이하면", s); StringAssert.DoesNotContain("그 적의 HP", s);
            StringAssert.Contains("낸 뒤 「성장」 +1", s);
        }

        /// <summary>(2026-10-11 사용자 「한 장 쓰면 다른 카드도 초기화?」) 낸 카드만 0 — 손에 남은 다른 카드의 시간은 그대로.</summary>
        [Test] public void 시간_한_장을_내도_다른_카드는_그대로()
        {
            var d = K.Data(heroes: TIME_H.Replace("max:6}", "max:6, who:'other'}"), cards: TIME_C);   // 실제 리뉴아처럼 다른 아군 카드만 센다
            var b = K.Fight(d, new[] { "tm", "c" }, new[] { "dummy" });
            b.Ap = 9;
            K.Hand(b, "clockbig", "clockbig2", "zc", "zc", "zc");
            for (int i = 0; i < 3; i++) K.Play(b, "zc");
            Assert.AreEqual(1, b.CostOf("clockbig")); Assert.AreEqual(1, b.CostOf("clockbig2"));
            K.Play(b, "clockbig");
            Assert.AreEqual(1, b.CostOf("clockbig2"), "다른 카드는 그대로 1");
        }

        [Test] public void 시간_낼_때의_비용으로_계기_paidMax()
        {
            var d = K.Data(heroes: TIME_H.Replace("cardValue:{grow:'allyPlay', reset:'play', cost:-1, max:6}}", "cardValue:{grow:'allyPlay', reset:'play', cost:-1, max:6}}, passives:[{name:'평화', when:{on:'play', paidMax:0}, fx:[{k:'gauge', v:7}]}]"), cards: TIME_C);
            var b = K.Fight(d, new[] { "tm", "c" }, new[] { "dummy" }); b.Ap = 9; b.Gauge = 0;
            K.Hand(b, "clockbig", "clockbig2", "zc", "zc", "zc", "zc");
            K.Play(b, "clockbig2");
            Assert.AreEqual(4 * R.GAUGE_PER_AP, b.Gauge, "4코로 냄 — 계기 안 돎(낸 AP 게이지만)");
            for (int i = 0; i < 4; i++) K.Play(b, "zc");
            int g1 = b.Gauge;
            K.Play(b, "clockbig");
            Assert.AreEqual(g1 + 7 + 10, b.Gauge, "비용 0 으로 냄 — 계기(게이지 +7) · 정각(+10)");
            StringAssert.Contains("비용 0으로 낸", new CardText(d).Detail(d.Hero("tm").Passives[0]));
        }

        [Test] public void 시간_봇은_다음_턴에_싸질_카드를_쥔다()
        {
            var d = K.Data(heroes: TIME_H, cards: TIME_C);
            var bot = new Bots(d);
            var b = K.Fight(d, new[] { "tm", "c" }, new[] { "dummy" });
            b.Ap = 4; b.Gauge = 0; K.Hand(b, "clockbig", "hit_c", "hit_c", "hit_c", "hit_c");
            var t = HoldTiming.Of(b, "clockbig");
            Assert.AreEqual("cheap", t.Why); Assert.IsTrue(t.Hold, "지금 4코 — 아군 카드로 0 이 된다");
            bot.SmartPlay(b);
            Assert.IsFalse(b.Hand.Contains("clockbig"));
            Assert.GreaterOrEqual(b.Gauge, 10, "남는 AP 는 동료 카드에 먼저 쓰고, 비용 0 이 된 뒤 정각으로 낸다");
            var x = K.Fight(d, new[] { "tm", "c" }, new[] { "dummy" });
            x.Ap = 4; K.Hand(x, "clockbig");
            bot.SmartPlay(x);
            Assert.IsFalse(x.Hand.Contains("clockbig"), "쥐면 AP 4 를 버린다 — 지금 비싸게라도 낸다");
        }

        [Test] public void 사냥감은_쓰러지면_가장_약한_적에게_겹째로()
        {
            var d = K.Data(heroes: @"[{id:'wf', name:'늑', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'발톱', carrier:'enemy', cap:3, hunt:true, huntCarry:'lowEnemy', per:[{stat:'taken', v:0.15, from:'owner'}]}}]",
                cards: @"[{id:'mark3', name:'표식', hero:'wf', cost:0, type:'스킬', fx:[{k:'stack', id:'발톱', v:3, target:'oneEnemy'}]}, {id:'kill', name:'처치', hero:'wf', cost:0, type:'공격', fx:[{k:'dmg', ratio:9, target:'oneEnemy'}]}]",
                enemies: @"[{id:'w50', name:'약', hp:50, intents:[{t:'jam', v:0, rush:0}]}]");
            var b = K.Fight(d, new[] { "wf" }, new[] { "w50", "dummy", "dummy" });
            K.Enemies(b)[2].Hp = 300;   // 셋째가 가장 약하다(30%)
            K.Hand(b, "mark3", "kill"); K.Play(b, "mark3", 0); K.Play(b, "kill", 0);
            Assert.IsTrue(b.Enemies[0].Dead);
            Assert.AreEqual(3, b.St(b.Enemies[2], "발톱"), "겹 그대로 가장 약한 적에게");
            Assert.AreEqual(0, b.St(b.Enemies[1], "발톱"));
            StringAssert.Contains("쓰러지면 가장 약한 적에게 겹째로 옮김", new CardText(d).Keyword(d.Hero("wf").Keyword));
        }

        [Test] public void 비례_뒤_강인도_피해도_그_수만큼()
        {
            var d = K.Data(heroes: @"[{id:'sn', name:'눈', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'만년설', cap:9}}]",
                cards: @"[{id:'snowT', name:'눈 강인도', hero:'sn', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'perStack', id:'만년설'}, {k:'tough', v:0.5}]}]",
                enemies: @"[{id:'tf', name:'단단', hp:1000, tough:6, intents:[{t:'jam', v:0, rush:0}]}]");
            var b = K.Fight(d, new[] { "sn" }, new[] { "tf" });
            b.Stacks["sn"] = new Dictionary<string, int> { ["만년설"] = 4 };
            double t0 = K.E(b).Tough; K.Hand(b, "snowT"); K.Play(b, "snowT");
            double lost = t0 - K.E(b).Tough;
            Assert.GreaterOrEqual(lost, 2.0, "만년설 4 × 0.5"); Assert.LessOrEqual(lost, 2.0 + R.TOUGH.Zero + 1e-9, "그 위는 카드 자체의 강인도 몫뿐");
            StringAssert.Contains("「만년설」 1개당 강인도 피해 0.5", new CardText(d).Card(d.View("snowT")));
            Assert.IsFalse(Validator.Check(d).Errors.Any(e => e.Contains("1개당")), string.Join(" | ", Validator.Check(d).Errors));
        }

        // ── 글 ──
        [Test] public void 글_4차()
        {
            var b = F(new[] { "rn", "nr", "bs", "ep", "ur", "bb", "ln", "ay" });
            var t = new CardText(b.Data);
            StringAssert.Contains("정각이면", t.Card(b.CardOf("clock")));
            StringAssert.Contains("이번 턴 자신의 다음 카드는 정각", t.Card(b.CardOf("pre")));
            StringAssert.Contains("이번 턴 남은 카드 신속", t.Card(b.CardOf("stop")));
            StringAssert.Contains("처치하면 1회 더(최대 4)", t.Card(b.CardOf("barrage")));
            StringAssert.Contains("「신성」 2턴: 치명 피해 +40%, 끝나면 적 전체 취약 1", t.Card(b.CardOf("holy")));
            StringAssert.Contains("이번 턴 4대 이상 맞았으면", t.Card(b.CardOf("bear")));
            StringAssert.Contains("이 카드 치명", t.Card(b.CardOf("sure")));
            StringAssert.Contains("「늑대」 적이면 치명", t.Card(b.CardOf("claw")));
            StringAssert.Contains("적이 하나뿐이면", t.Card(b.CardOf("solo")));
            StringAssert.Contains("다른 아군의 다음 비용 2 이상 카드 +50%", t.Card(b.CardOf("kind")));
            StringAssert.Contains("비용 2 이상 카드 1장 드로우", t.Card(b.CardOf("dr2")));
            StringAssert.Contains("공격하려는 적이 있으면", t.Card(b.CardOf("fount")));
            StringAssert.Contains("준 피해의 10% 고정 피해", t.Card(b.CardOf("axe")));
            StringAssert.Contains("「영역 위」 아군의 공격력 +10%", t.Keyword(b.Data.Hero("ur").Keyword));
            StringAssert.Contains("고학년 게이지 -25%", t.Keyword(b.Data.Hero("uk").Keyword));
            var tips = t.Tips();
            Assert.IsTrue(tips.ContainsKey("쥐고 넘기면"));
            StringAssert.Contains("3번째 카드부터", tips["정각"]);
            StringAssert.Contains("치명 피해 +40%", tips["신성"]);
        }

        [Test] public void 글_쥐고_넘기면_실드_쓴_양()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS.TrimEnd(']') + @", {id:'ram', name:'들이받기', hero:'bb', cost:1, type:'공격', tags:['보존'], fx:[{k:'dmg', ratio:0.8, base:'def', target:'oneEnemy'}, {k:'spendShield', half:true}, {k:'dmg', ratio:1, ofEvent:1.5, target:'oneEnemy'}, {k:'when', on:'handEnd'}, {k:'status', id:'실드 유지', v:1}]}]", enemies: FOES);
            var s = new CardText(d).Card(d.View("ram", 0));
            StringAssert.Contains("파티 실드 절반을 써 그 150% 고정 피해", s);
            StringAssert.Contains("쥐고 넘기면", s);
            StringAssert.DoesNotContain("턴 끝에 손에 있으면", s);
        }
    }
}
