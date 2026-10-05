using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 키워드 사전(Docs/키워드.md, 2026-10-05) — 키워드마다 카제나 값. 사도 a(공 100 방 50) · b(공 80 방 40) · c(공 150 방 20), 적 dummy · hitter(공격 100).
    /// </summary>
    public class KeywordDictTests
    {
        static GameData D(string cards = null, string enemies = null, string heroes = null) => K.Data(heroes: heroes, cards: cards, enemies: enemies);
        const string TOSS = "{id:'toss', name:'버리기', hero:'a', cost:0, type:'스킬', fx:[{k:'discard', v:1}]}";

        // ── 카드 태그 ──────────────────────────────────────────────────
        [Test] public void 망각은_소멸_대신_뽑을_더미_맨_위로()
        {
            var b = K.Fight(D("[{id:'fg', name:'잊음', hero:'a', cost:0, type:'스킬', tags:['소멸', '망각'], fx:[]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "fg");
            K.Play(b, "fg");
            Assert.AreEqual("fg", b.Draw.Last()); CollectionAssert.DoesNotContain(b.Gone, "fg");
        }

        [Test] public void 제거는_덱에서_완전히_빠진다()
        {
            var b = K.Fight(D("[{id:'rm', name:'한 번뿐', hero:'a', cost:0, type:'스킬', tags:['제거'], fx:[]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "rm");
            K.Play(b, "rm");
            CollectionAssert.Contains(b.Gone, "rm"); CollectionAssert.Contains(b.Removed, "rm");
        }

        [Test] public void 감응은_턴_시작에_뽑혀도_발동()
        {
            var d = D("[{id:'sense', name:'감응', hero:'a', cost:1, type:'스킬', fx:[{k:'when', on:'drawAny'}, {k:'gauge', v:40}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = Enumerable.Repeat("sense", 6).ToList());
            Assert.AreEqual(200, b.Gauge, "5장 × 40");
        }

        [Test] public void 소각은_소멸할_때_발동()
        {
            var b = K.Fight(D("[{id:'ash', name:'재', hero:'a', cost:0, type:'스킬', tags:['소멸'], fx:[{k:'when', on:'burn'}, {k:'gauge', v:30}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "ash");
            K.Play(b, "ash");
            Assert.AreEqual(30, b.Gauge);
        }

        [Test] public void 열정_카드가_나가면_손의_열정_마디()
        {
            var d = D("[{id:'pa', name:'불꽃', hero:'a', cost:0, type:'공격', tags:['열정'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}, {id:'pb', name:'응답', hero:'b', cost:1, type:'스킬', fx:[{k:'when', on:'passion'}, {k:'gauge', v:25}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "pa", "pb");
            K.Play(b, "pa");
            Assert.AreEqual(25, b.Gauge); CollectionAssert.Contains(b.Hand, "pb");
        }

        [Test] public void 축복은_실드가_30퍼센트_미만일_때_버려지면_고정_실드_60퍼센트()
        {
            var b = K.Fight(D("[" + TOSS + ", {id:'bl', name:'축복', hero:'a', cost:1, type:'스킬', tags:['축복'], fx:[]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "toss", "zero", "bl");
            K.Play(b, "toss", 0, new PlayOpts { Discard = new List<string> { "bl" } });
            Assert.AreEqual(30, b.Pool.Shield, "방어력 50 × 60%");
        }

        [Test] public void 약점_공격은_피해_25퍼센트_강인도_AP1당_1칸()
        {
            var b = K.Fight(D("[{id:'wk', name:'급소', hero:'a', cost:1, type:'공격', tags:['약점 공격'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "wk");
            K.Play(b, "wk");
            Assert.AreEqual(875, K.Hp(b)); Assert.AreEqual(3, K.E(b).Tough, 1e-9);
        }

        [Test] public void 결속은_겹치고_3이상이면_강해진_카드로_내면_처음으로()
        {
            var d = D(@"[{id:'bd1', name:'끈1', hero:'a', cost:1, type:'공격', tags:['결속'], bondCard:'bdx', fx:[{k:'dmg', ratio:0.5, target:'oneEnemy'}]},
 {id:'bd2', name:'끈2', hero:'a', cost:1, type:'공격', tags:['결속'], bondCard:'bdx', fx:[{k:'dmg', ratio:0.5, target:'oneEnemy'}]},
 {id:'bdx', name:'굳은 끈', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:2.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = new List<string> { "bd1", "bd2", "bd1" });
            Assert.AreEqual(1, b.Hand.Count, "세 장이 한 장으로");
            var id = b.Hand[0];
            Assert.AreEqual(3, b.BondOf(id));
            Assert.AreEqual("굳은 끈", b.CardOf(id).Name);
            K.Play(b, id);
            Assert.AreEqual(800, K.Hp(b));
            Assert.AreEqual(1, b.BondOf(id), "내면 처음으로");
        }

        [Test] public void 생성_카드_진화_두_장이면_한_장으로()
        {
            var d = D(@"[{id:'tok', name:'씨앗', hero:'a', token:true, cost:0, type:'스킬', evolve:{n:2, into:'tok2'}, fx:[]},
 {id:'tok2', name:'새싹', hero:'a', token:true, cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'mk', name:'심기', hero:'a', cost:0, type:'스킬', fx:[{k:'make', id:'tok', v:2}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "mk");
            K.Play(b, "mk");
            CollectionAssert.AreEqual(new[] { "tok2~" }, b.Hand);
        }

        // ── 조건 · 피해 종류 ──────────────────────────────────────────
        [Test] public void 처치와_붕괴()
        {
            var d = D(@"[{id:'fin', name:'끝', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'ifKill'}, {k:'ap', v:2}]},
 {id:'brk', name:'깨', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'ifBreak'}, {k:'gauge', v:50}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "fin", "brk");
            K.E(b, 0).Hp = 50;
            K.Play(b, "fin", 0);
            Assert.AreEqual(4, b.Ap, "처치: AP +2");
            K.E(b, 1).Tough = 0.3;
            K.Play(b, "brk", 1);
            Assert.AreEqual(10 + 10 + 50, b.Gauge, "붕괴: 게이지 +50");
        }

        [Test] public void 부상은_파티_체력_30퍼센트_미만()
        {
            var b = K.Fight(D("[{id:'wd', name:'버팀', hero:'a', cost:0, type:'스킬', fx:[{k:'ifWounded'}, {k:'gauge', v:30}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "wd", "wd");
            b.Pool.Hp = 300; K.Play(b, "wd"); Assert.AreEqual(0, b.Gauge, "30% 는 부상이 아니다");
            b.Pool.Hp = 299; K.Play(b, "wd"); Assert.AreEqual(30, b.Gauge);
        }

        [Test] public void 고정_지속_피해는_실드도_취약도_무시()
        {
            var b = K.Fight(D("[{id:'dt', name:'스밈', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, dot:true, target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "dt");
            K.E(b).Block = 500; K.E(b).Status["취약"] = 1;
            K.Play(b, "dt");
            Assert.AreEqual(900, K.Hp(b)); Assert.AreEqual(500, K.E(b).Block); Assert.AreEqual(1, b.St(K.E(b), "취약"));
        }

        [Test] public void 피해_기반_회복은_준_피해의_비율_최대_체력_20퍼센트까지()
        {
            var b = K.Fight(D("[{id:'dr', name:'흡', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'drain', ratio:0.5}]}, {id:'dr9', name:'큰 흡', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:9.0, target:'oneEnemy'}, {k:'drain', ratio:1.0}]}]"), new[] { "a" }, new[] { "dummy" });
            K.Hand(b, "dr", "dr9"); b.Pool.Hp = 300;
            K.Play(b, "dr"); Assert.AreEqual(350, b.Pool.Hp);
            K.Play(b, "dr9"); Assert.AreEqual(550, b.Pool.Hp, "900 이 아니라 최대 체력 20% = 200");
        }

        [Test] public void 추가_공격과_탄성()
        {
            var b = K.Fight(D("[{id:'ex', name:'덤', hero:'a', cost:0, type:'스킬', fx:[{k:'extra', ratio:0.8, target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "ex");
            b.Pool.Status["탄성"] = 2; b.Pool.Hp = 500;
            K.Play(b, "ex");
            Assert.AreEqual(920, K.Hp(b)); Assert.AreEqual(550, b.Pool.Hp, "방어력 50 × 50% × 2"); Assert.AreEqual(0, b.St(b.Pool, "탄성"));
        }

        // ── 이로운 효과 ────────────────────────────────────────────────
        [Test] public void 불굴은_취약과_합연산()
        {
            var b = K.Fight("hitter"); b.Pool.Status["불굴"] = 1; b.Pool.Status["취약"] = 1;
            b.EndTurn();
            Assert.AreEqual(870, b.Pool.Hp, "100 × (1 + 0.5 - 0.2)");
        }

        [Test] public void 협공은_협공을_건_사도의_공격력_100퍼센트()
        {
            var d = D("[{id:'gk', name:'함께', hero:'b', cost:0, type:'스킬', fx:[{k:'status', id:'협공', v:1}]}]");
            var b = K.Fight(d, new[] { "a", "b", "c" }, new[] { "dummy" }); K.Hand(b, "gk", "hit");
            K.Play(b, "gk"); K.Play(b, "hit");
            Assert.AreEqual(1000 - 100 - 80, K.Hp(b), "c(150)가 아니라 건 b(80)");
        }

        [Test] public void 반격은_반격을_건_사도의_방어력()
        {
            var d = D("[{id:'ck', name:'되받기', hero:'b', cost:0, type:'스킬', fx:[{k:'status', id:'반격', v:1}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "hitter" }); K.Hand(b, "ck");
            K.Play(b, "ck"); b.EndTurn();
            Assert.AreEqual(1000 - 162, K.Hp(b), "(40×2.1 + 80×0.3) × 1.5");
        }

        [Test] public void 공명은_카드를_버리면_추가_공격_80퍼센트()
        {
            var b = K.Fight(D("[" + TOSS + "]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "toss", "zero");
            b.Pool.Status["공명"] = 1;
            K.Play(b, "toss", 0, new PlayOpts { Discard = new List<string> { "zero" } });
            Assert.AreEqual(920, K.Hp(b)); Assert.AreEqual(0, b.St(b.Pool, "공명"));
        }

        [Test] public void 칼날_벼리기는_공격_카드의_방어_기반_피해_겹마다_20퍼센트()
        {
            var b = K.Fight(D("[{id:'bd', name:'방패치기', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, base:'def', target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "bd");
            b.Pool.Status["칼날 벼리기"] = 2;
            K.Play(b, "bd");
            Assert.AreEqual(1000 - 189, K.Hp(b), "135 × 1.4");
            b.EndTurn();
            Assert.AreEqual(0, b.St(b.Pool, "칼날 벼리기"), "1턴");
        }

        [Test] public void 빙벽은_반격_겹을_안_쓰고_반격_1턴()
        {
            var b = K.Fight("hitter"); b.Pool.Status["빙벽"] = 1;
            b.EndTurn();
            Assert.AreEqual(1000 - 203, K.Hp(b)); Assert.AreEqual(0, b.St(b.Pool, "빙벽"), "다음 내 턴에 사라진다");
        }

        [Test] public void 구속은_그_사도의_카드_말고는_AP를_받지_않는다()
        {
            var d = D("[{id:'apn', name:'교주 AP', cost:0, type:'스킬', grade:'일반', price:50, fx:[{k:'ap', v:1}]}, {id:'apa', name:'가 AP', hero:'a', cost:0, type:'스킬', fx:[{k:'ap', v:1}]}]");
            // 교주 카드는 주인 사도의 카드다 — 다른 사도(b)에게 넣은 교주 카드는 구속을 못 뚫는다
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "apn@b", "apa");
            b.Party[0].Status["구속"] = 1;
            K.Play(b, "apn@b"); Assert.AreEqual(3, b.Ap);
            K.Play(b, "apa"); Assert.AreEqual(4, b.Ap);
            Assert.AreEqual(0, b.St(b.Pool, "구속"), "구속은 개인 층");
        }

        [Test] public void 형상_강화는_만든_카드_효과_100퍼센트()
        {
            var b = K.Fight(); K.Hand(b, "hit~");
            b.Pool.Status["형상 강화"] = 1;
            K.Play(b, "hit~");
            Assert.AreEqual(800, K.Hp(b));
        }

        [Test] public void 행동_둔화는_신속_카드에_적_전체_사기_마이너스1()
        {
            var b = K.Fight(D("[{id:'sw', name:'재빨리', hero:'a', cost:0, type:'스킬', tags:['신속'], fx:[]}]"), new[] { "a" }, new[] { "hitter" }); K.Hand(b, "sw");
            b.Pool.Status["행동 둔화"] = 1;
            K.Play(b, "sw"); b.EndTurn();
            Assert.AreEqual(920, b.Pool.Hp);
        }

        [Test] public void 급속과_둔화는_행동_카운트()
        {
            var b = K.Fight(D(enemies: "[{id:'slow', name:'느림', hp:1000, intents:[{t:'jam', v:0, rush:5}]}]"), new[] { "a" }, new[] { "slow" });
            Assert.AreEqual(5, b.ActionCount(K.E(b)));
            K.E(b).Status["급속"] = 2; Assert.AreEqual(3, b.ActionCount(K.E(b)));
            K.E(b).Status["둔화"] = 3; Assert.AreEqual(6, b.ActionCount(K.E(b)));
        }

        [Test] public void 근면은_처치하면_AP1_드로우1_턴당_한_번()
        {
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "dummy", "dummy" }, st => st.Deck = Enumerable.Repeat("zero", 12).ToList()); K.Hand(b, "hit");
            b.Pool.Status["근면"] = 1; K.E(b).Hp = 50;
            K.Play(b, "hit", 0);
            Assert.AreEqual(3, b.Ap); Assert.AreEqual(1, b.Hand.Count);
        }

        [Test] public void 집중_다음_턴_드로우_초재생()
        {
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "dummy" }, st => st.Deck = Enumerable.Repeat("zero", 30).ToList());
            b.Pool.Status["집중"] = 1; b.Pool.Status["다음 턴 드로우"] = 2; b.Pool.Status["초재생"] = 1; b.Pool.Hp = 400;
            b.EndTurn();
            Assert.AreEqual(400 + 150, b.Pool.Hp, "초재생 — 방어력 300%");
            Assert.AreEqual(3, b.Ap, "550 은 50% 넘음 — 집중은 안 돈다");
            Assert.AreEqual(7, b.Hand.Count, "5 + 다음 턴 드로우 2");
            b.Pool.Hp = 300; b.EndTurn();
            Assert.AreEqual(4, b.Ap, "집중 — 50% 미만이면 AP +1");
        }

        [Test] public void 회피_절대_무적_끈기()
        {
            var b = K.Fight("hitter");
            b.Pool.Status["회피"] = 1; b.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp); Assert.AreEqual(0, b.St(b.Pool, "회피"));
            b.Pool.Status["절대 무적"] = 1; b.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp); Assert.AreEqual(0, b.St(b.Pool, "절대 무적"), "1턴");
            b.Pool.Hp = 50; b.Pool.Status["끈기"] = 1; b.EndTurn();
            Assert.AreEqual(1, b.Pool.Hp); Assert.IsNull(b.Over);
        }

        [Test] public void 계몽은_고학년을_쓰면_드로우1()
        {
            var d = K.Data(heroes: "[{id:'u', name:'우', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, ult:{name:'한 방', cost:150, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}}]");
            var b = K.Fight(d, new[] { "u" }, new[] { "dummy" }, st => { st.Gauge = 150; st.Deck = Enumerable.Repeat("zero", 12).ToList(); });
            b.Pool.Status["계몽"] = 1;
            b.UseUlt("u");
            Assert.AreEqual(6, b.Hand.Count);
        }

        [Test] public void 탐구심은_이_카드_피해_겹마다_20퍼센트()
        {
            var b = K.Fight(D("[{id:'inq', name:'탐구', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'cardStatus', id:'탐구심', v:1}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "inq");
            K.Play(b, "inq"); b.Discard.Remove("inq"); b.Hand.Add("inq"); K.Play(b, "inq");
            Assert.AreEqual(1000 - 100 - 120, K.Hp(b));
        }

        // ── 해로운 효과 ────────────────────────────────────────────────
        [Test] public void 고통_각인은_고통을_2_더()
        {
            var b = K.Fight(D("[{id:'pn', name:'고통', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'고통', v:1, target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "pn");
            K.E(b).Status["고통 각인"] = 1;
            K.Play(b, "pn");
            Assert.AreEqual(3, b.St(K.E(b), "고통"));
        }

        [Test] public void 응징은_실드를_얻으면_그쪽에_고정_피해_200퍼센트()
        {
            var b = K.Fight(); K.Hand(b, "guard");
            b.Pool.Status["응징"] = 1; b.Pool.DotU["응징"] = 30;
            K.Play(b, "guard");
            Assert.AreEqual(100 - 60, b.Pool.Block, "고정 피해 60 이 얻은 실드에 막힌다"); Assert.AreEqual(0, b.St(b.Pool, "응징"));
        }

        [Test] public void 포자증식은_겹마다_10퍼센트_발동하면_사라짐()
        {
            var b = K.Fight(); K.Hand(b, "hit", "hit"); K.E(b).Status["포자증식"] = 2;
            K.Play(b, "hit"); Assert.AreEqual(880, K.Hp(b)); Assert.AreEqual(0, b.St(K.E(b), "포자증식"));
            K.Play(b, "hit"); Assert.AreEqual(780, K.Hp(b));
        }

        [Test] public void 죽음의_낙인은_건_카드로만_80퍼센트()
        {
            var b = K.Fight(D("[{id:'brand', name:'낙인', hero:'a', cost:0, type:'공격', fx:[{k:'status', id:'죽음의 낙인', v:1, target:'oneEnemy'}, {k:'dmg', ratio:1.0, target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "brand", "hit");
            K.Play(b, "brand"); Assert.AreEqual(820, K.Hp(b));
            K.Play(b, "hit"); Assert.AreEqual(720, K.Hp(b), "다른 카드는 그대로");
        }

        [Test] public void 열정_약점은_열정_카드를_약점으로()
        {
            var b = K.Fight(D("[{id:'pas', name:'열', hero:'a', cost:1, type:'공격', tags:['열정'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "pas", "hit");
            K.E(b).Status["열정 약점"] = 1;
            K.Play(b, "pas"); Assert.AreEqual(875, K.Hp(b));
            K.Play(b, "hit"); Assert.AreEqual(775, K.Hp(b), "열정이 아니면 그대로");
        }

        [Test] public void 정신_붕괴는_고학년_신탁을_막는다()
        {
            var d = K.Data(heroes: "[{id:'u', name:'우', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, ult:{name:'한 방', cost:150, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}}]",
                cards: "[{id:'u1', name:'우 때리기', hero:'u', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "u" }, new[] { "dummy" }, st => { st.Gauge = 150; st.Glow = new Dictionary<string, Glow> { ["u1"] = new Glow { Kind = "hero", Hero = "u", Options = new List<string> { "u1" } } }; });
            b.Pool.Status["정신 붕괴"] = 1;
            Assert.IsNotNull(b.CanUlt("u")); Assert.IsNull(b.GlowOf("u1"));
        }

        [Test] public void 미끄러움은_카드를_내면_무작위_1장_버림()
        {
            var b = K.Fight(); K.Hand(b, "zero", "hit", "guard"); b.Pool.Status["미끄러움"] = 1;
            K.Play(b, "zero");
            Assert.AreEqual(1, b.Hand.Count); Assert.AreEqual(0, b.St(b.Pool, "미끄러움"));
        }

        [Test] public void 봉쇄_침체_빙결_독은_카드에_붙는다()
        {
            var b = K.Fight(); K.Hand(b, "hit", "big");
            b.CardSt["hit"] = new Dictionary<string, int> { ["봉쇄"] = 1 };
            K.Play(b, "hit"); Assert.AreEqual(1000, K.Hp(b), "봉쇄 — 효과 없음");
            b.Discard.Remove("hit"); b.Hand.Add("hit"); K.Play(b, "hit"); Assert.AreEqual(900, K.Hp(b), "봉쇄가 풀렸다");
            b.CardSt["big"] = new Dictionary<string, int> { ["침체"] = 1 };
            Assert.AreEqual(3, b.CostOf("big"));
            b.Ap = 3; K.Play(b, "big"); Assert.AreEqual(0, b.CardStOf("big", "침체"), "내면 풀린다");
            b.Draw.Add("guard"); b.CardSt["guard"] = new Dictionary<string, int> { ["빙결"] = 1 };
            b.DrawCards(1);
            StringAssert.Contains("빙결", b.CanPlay("guard"));
            b.CardSt["zero"] = new Dictionary<string, int> { ["독"] = 40 }; b.Hand.Add("zero");
            K.Play(b, "zero"); Assert.AreEqual(960, b.Pool.Hp, "독 — 실드 무시 피해");
        }

        [Test] public void 적의_독_손_희귀종()
        {
            var d = D(enemies: "[{id:'rp', name:'독쟁이', hp:1000, intents:[{t:'jam', v:0, rush:0}], rare:[{id:'poisonHand'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "rp" }, st => st.Deck = new List<string> { "hit", "hit", "hit", "zero", "zero", "zero" });
            Assert.AreEqual(2, b.CardSt.Count(kv => kv.Value.ContainsKey("독")), "손 2장에 독");
            Assert.AreEqual(20, b.CardStOf("hit", "독"), "바탕 20 × 층 배율");
        }

        // ── 적 효과 틀 ────────────────────────────────────────────────
        [Test] public void 분노는_공격_외_카드마다_쌓이다_문턱에서_즉시_강공()
        {
            var d = D(enemies: "[{id:'rage', name:'성난 놈', hp:1000, intents:[{t:'jam', v:0, rush:0}], counters:[{name:'분노', onCard:1, cardType:'!공격', at:2, act:{t:'attack', v:300, say:'폭발'}, dealt:0.1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "rage" }); K.Hand(b, "zero", "hit", "zero");
            K.Play(b, "zero"); Assert.AreEqual(1, b.St(K.E(b), "분노"));
            K.Play(b, "hit"); Assert.AreEqual(1, b.St(K.E(b), "분노"), "공격 카드는 안 센다");
            K.Play(b, "zero");
            Assert.AreEqual(700, b.Pool.Hp); Assert.AreEqual(0, b.St(K.E(b), "분노"), "터지면 처음으로");
        }

        [Test] public void 허기_과보호_비행()
        {
            var d = D(enemies: @"[{id:'hunger', name:'굶주림', hp:1000, intents:[{t:'attack', v:100, rush:0}], counters:[{name:'허기', start:2, dealt:0.2, onHit:-1, resetTurnStart:true}]},
 {id:'guardy', name:'과보호', hp:1000, intents:[{t:'jam', v:0, rush:0}], counters:[{name:'보호막', start:3, flat:1, onHit:-1}]},
 {id:'fly', name:'날개', hp:1000, intents:[{t:'attack', v:100, rush:0}], counters:[{name:'날갯짓', start:1, onHit:-1, stunAtZero:true, resetTurnStart:true}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "hunger", "guardy", "fly" }); K.Hand(b, "hit", "hit", "hit");
            Assert.AreEqual(140, b.IntentHit(K.E(b, 0)));
            K.Play(b, "hit", 0); Assert.AreEqual(120, b.IntentHit(K.E(b, 0)));
            K.Play(b, "hit", 1); Assert.AreEqual(999, K.Hp(b, 1)); Assert.AreEqual(2, b.St(K.E(b, 1), "보호막"));
            K.Play(b, "hit", 2); Assert.IsTrue(K.E(b, 2).Sealed, "0 이면 기절");
            b.EndTurn();
            Assert.AreEqual(1000 - 120, b.Pool.Hp, "날개는 못 쳤다");
            Assert.AreEqual(2, b.St(K.E(b, 0), "허기"), "턴 시작에 처음 값");
        }

        [Test] public void 쌓여서_1턴_뒤_강공()
        {
            var d = D(enemies: "[{id:'ch', name:'기세', hp:1000, intents:[{t:'jam', v:0, rush:0}], counters:[{name:'기운', onTurnStart:1, at:2, act:{t:'attack', v:200, rush:0}, mode:'next'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "ch" });
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual(200, K.E(b).Intent.V);
        }

        [Test] public void 죽을_때_재_속과_가사()
        {
            var d = D(enemies: @"[{id:'ph', name:'불새', hp:100, intents:[{t:'jam', v:0, rush:0}], passives:[{name:'재', on:'death', do:{t:'revive', v:50, n:1}}]},
 {id:'fake', name:'죽은 척', hp:100, intents:[{t:'jam', v:0, rush:0}], passives:[{name:'가사', on:'death', do:{t:'feign'}}]},
 {id:'healer', name:'약사', hp:1000, intents:[{t:'heal', v:30, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "ph", "fake", "healer" }); K.Hand(b, "hit", "big");
            K.Play(b, "hit", 0); K.Play(b, "big", 1);
            Assert.IsTrue(K.E(b, 0).Dead && K.E(b, 1).Dead);
            b.EndTurn();
            Assert.IsFalse(K.E(b, 1).Dead, "약사가 회복하자 일어섰다"); Assert.AreEqual(30, K.Hp(b, 1));
            Assert.IsFalse(K.E(b, 0).Dead, "재 속에서 되살아났다"); Assert.AreEqual(50, K.Hp(b, 0));
        }

        [Test] public void 소환자가_쓰러지면_같이_영혼_공유는_맨_앞만()
        {
            var d = D(enemies: @"[{id:'caller', name:'부르는 이', hp:1000, intents:[{t:'summon', id:'imp', rush:0, max:1}]},
 {id:'imp', name:'졸개', hp:100, tied:true, intents:[{t:'jam', v:0, rush:0}]},
 {id:'twin', name:'쌍둥이', hp:1000, soul:true, intents:[{t:'jam', v:0, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "caller", "twin", "twin" });
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual(4, b.Enemies.Count, "max 1 — 두 번째는 안 세운다");
            K.Hand(b, "hit", "hit");
            K.Play(b, "hit", 2); Assert.AreEqual(1000, K.Hp(b, 2), "맨 앞 쌍둥이가 아니다");
            K.E(b, 0).Hp = 1; K.Play(b, "hit", 0);
            Assert.IsTrue(b.Enemies[3].Dead, "졸개도 같이");
        }

        [Test] public void 실드가_깨지면_수가_바뀐다_같은_사도_연달아면_즉시()
        {
            var d = D(enemies: @"[{id:'shell', name:'껍질', hp:1000, intents:[{t:'attack', v:100, rush:0}], passives:[{name:'깨짐', on:'guardBreak', do:{t:'shift', next:{t:'block', v:50, say:'움츠림'}}}, {name:'연타 벌', on:'card', same:true, limit:0, do:{t:'attack', v:50}}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "shell" }); K.Hand(b, "hit", "zero");
            K.E(b).Block = 50;
            K.Play(b, "hit"); Assert.AreEqual("block", K.E(b).Intent.T);
            K.Play(b, "zero"); Assert.AreEqual(950, b.Pool.Hp, "같은 사도 카드를 잇달아 — 즉시 50");
        }

        [Test] public void 받는_강인도_피해_20퍼센트_덜_희귀종()
        {
            var b = K.Fight(D(enemies: "[{id:'tg', name:'단단', hp:1000, intents:[{t:'jam', v:0, rush:0}], rare:[{id:'toughGuard'}]}]"), new[] { "a" }, new[] { "tg" }); K.Hand(b, "hit");
            K.Play(b, "hit");
            Assert.AreEqual(4 - 0.8 / 3, K.E(b).Tough, 1e-9);
        }

        // ── 사도 고유 효과 틀 ──────────────────────────────────────────
        [Test] public void 두_갈래로_내기()
        {
            var b = K.Fight(D("[{id:'ch', name:'갈래', hero:'a', cost:0, type:'스킬', choices:['불', '물'], fx:[{k:'ifChoice', n:1}, {k:'gauge', v:10}, {k:'ifChoice', n:2}, {k:'gauge', v:20}]}]"), new[] { "a" }, new[] { "dummy" }); K.Hand(b, "ch");
            K.Play(b, "ch", 0, new PlayOpts { Choice = 2 });
            Assert.AreEqual(20, b.Gauge);
            Assert.AreEqual("갈래 — 불 / 물. 불: 고학년 게이지 +10%. 물: 고학년 게이지 +20%", new CardText(b.Data).Card(b.Data.Card("ch")));
        }

        [Test] public void 예약_N턴_뒤와_N장_뒤()
        {
            var d = D("[{id:'lt', name:'나중', hero:'a', cost:0, type:'스킬', fx:[{k:'later', n:1, then:[{k:'gauge', v:40}]}]}, {id:'ac', name:'모이면', hero:'a', cost:0, type:'스킬', fx:[{k:'afterCards', n:2, then:[{k:'gauge', v:30}]}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "lt", "ac", "zero", "zero");
            K.Play(b, "lt"); K.Play(b, "ac"); K.Play(b, "zero");
            Assert.AreEqual(0, b.Gauge);
            K.Play(b, "zero"); Assert.AreEqual(30, b.Gauge, "카드 2장 뒤");
            b.EndTurn(); Assert.AreEqual(70, b.Gauge, "1턴 뒤");
        }

        [Test] public void 함정과_혼란()
        {
            var d = D("[{id:'tp', name:'덫', hero:'a', cost:0, type:'스킬', fx:[{k:'trap', target:'oneEnemy', then:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]}, {id:'cf', name:'헷갈림', hero:'a', cost:0, type:'스킬', fx:[{k:'confuse', target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "hitter", "dummy" }); K.Hand(b, "tp", "cf");
            K.Play(b, "tp", 0); K.Play(b, "cf", 0);
            b.EndTurn();
            Assert.AreEqual(900, K.Hp(b, 0), "함정 — 공격하려는 순간");
            Assert.AreEqual(900, K.Hp(b, 1), "혼란 — 다른 적을 쳤다");
            Assert.AreEqual(1000, b.Pool.Hp);
        }

        [Test] public void 다른_사도_카드_대신_발동과_더미에서_꺼내기()
        {
            var d = D("[{id:'co', name:'흉내', hero:'a', cost:0, type:'스킬', fx:[{k:'castOther'}]}, {id:'pl', name:'줍기', hero:'a', cost:0, type:'스킬', fx:[{k:'pull', from:'discard'}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "co", "hit_b", "pl");
            K.Play(b, "co");
            Assert.AreEqual(920, K.Hp(b)); CollectionAssert.Contains(b.Hand, "hit_b", "카드는 손에 남는다");
            b.Discard.Add("guard");
            K.Play(b, "pl"); CollectionAssert.Contains(b.Hand, "guard");
        }

        [Test] public void 고유_효과로_비용_치르기와_AP_빚()
        {
            var d = K.Data(heroes: "[{id:'p', name:'시계', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, keyword:{name:'초침', cap:5}}]",
                cards: "[{id:'pw', name:'멈춘 시간', hero:'p', cost:2, type:'공격', payWith:'초침', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}, {id:'tick', name:'째깍', hero:'p', cost:0, type:'스킬', fx:[{k:'stack', id:'초침', v:3}]}, {id:'debt', name:'외상', hero:'p', cost:5, type:'공격', debt:true, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "pw", "tick", "debt");
            StringAssert.Contains("초침", b.CanPlay("pw"));
            K.Play(b, "tick"); K.Play(b, "pw");
            Assert.AreEqual(3, b.Ap, "AP 대신 초침"); Assert.AreEqual(1, b.StackOf("p", "초침"));
            K.Play(b, "debt");
            Assert.AreEqual(0, b.Ap); Assert.AreEqual(2, b.ApJam);
            b.EndTurn(); Assert.AreEqual(1, b.Ap, "빚 2 를 갚았다");
        }

        [Test] public void 사도에게_붙이는_표시_지정_아군이_카드를_내면()
        {
            var d = K.Data(heroes: "[{id:'m', name:'선생', role:'서포터', row:'mid', hp:600, atk:80, def:40, crit:0, keywords:[{name:'제자', carrier:'hero', cap:1}], passives:[{name:'가르침', when:{on:'play', marked:'제자'}, fx:[{k:'gauge', v:15}]}]}]",
                cards: "[{id:'mark', name:'지목', hero:'m', cost:0, type:'스킬', fx:[{k:'stack', id:'제자', v:1, target:'oneAlly'}]}, {id:'mz', name:'선생 숨', hero:'m', cost:0, type:'스킬', fx:[]}]");
            var b = K.Fight(d, new[] { "m", "a" }, new[] { "dummy" }); K.Hand(b, "mark", "zero", "mz");
            K.Play(b, "mark", 0, new PlayOpts { Ally = 1 });
            Assert.AreEqual(1, b.StackOf("a", "제자"));
            K.Play(b, "zero"); Assert.AreEqual(15, b.Gauge);
            K.Play(b, "mz"); Assert.AreEqual(15, b.Gauge, "표시가 없는 사도는 아니다");
            Assert.AreEqual(1, b.HeroStatus(b.Party[1])["제자"]);
        }

        [Test] public void 소환물은_공격을_대신_받고_표식은_약점으로_순환()
        {
            var d = K.Data(heroes: @"[{id:'g', name:'벌치기', role:'탱커', row:'front', hp:600, atk:100, def:40, crit:0, keyword:{name:'벌떼', cap:3, guard:true}, keywords:[{name:'과녁', carrier:'enemy', weakens:true}, {name:'달', cap:4, wrap:true}]}]",
                cards: "[{id:'bee', name:'벌', hero:'g', cost:0, type:'스킬', fx:[{k:'stack', id:'벌떼', v:2}, {k:'stack', id:'과녁', v:1, target:'oneEnemy'}, {k:'stack', id:'달', v:5}]}, {id:'gh', name:'침', hero:'g', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "g" }, new[] { "hitter" }); K.Hand(b, "bee", "gh");
            K.Play(b, "bee");
            Assert.AreEqual(1, b.StackOf("g", "달"), "4 를 넘으면 1 부터");
            K.Play(b, "gh"); Assert.AreEqual(875, K.Hp(b), "과녁 — 약점 공격"); Assert.AreEqual(0, b.St(K.E(b), "과녁"));
            b.EndTurn();
            Assert.AreEqual(b.Pool.MaxHp, b.Pool.Hp, "벌떼가 대신 맞았다"); Assert.AreEqual(1, b.StackOf("g", "벌떼"));
        }

        [Test] public void 조건_연속_N장째_손패_낸_카드_비례_판_단위_성장()
        {
            var d = D(@"[{id:'st', name:'잇기', hero:'a', cost:0, type:'스킬', fx:[{k:'ifStreak', n:2}, {k:'gauge', v:30}]},
 {id:'nth', name:'셋째', hero:'a', cost:0, type:'스킬', fx:[{k:'ifNth', n:3}, {k:'gauge', v:20}]},
 {id:'pp', name:'몰아치기', hero:'a', cost:0, type:'공격', fx:[{k:'perPlayed'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'gr', name:'단련', hero:'a', cost:0, type:'스킬', fx:[{k:'growRun', id:'atk', v:10}, {k:'ifHand', n:0}, {k:'gauge', v:5}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "zero", "st", "nth", "pp", "gr");
            K.Play(b, "zero"); K.Play(b, "st"); Assert.AreEqual(30, b.Gauge);
            K.Play(b, "nth"); Assert.AreEqual(50, b.Gauge);
            K.Play(b, "pp"); Assert.AreEqual(1000 - 30, K.Hp(b), "앞서 낸 3장 × 10");
            K.Play(b, "gr"); Assert.AreEqual(110, b.Party[0].Atk); Assert.AreEqual(10, b.GrowthGain["a"].Atk); Assert.AreEqual(55, b.Gauge, "손패 0장");
        }

        [Test] public void 처치_계기는_초과_피해를_값으로_넘긴다()
        {
            var d = K.Data(heroes: "[{id:'k', name:'넘침', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, passives:[{name:'여파', when:{on:'kill'}, fx:[{k:'perEvent', per:10}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}]}]",
                cards: "[{id:'kh', name:'넘침 때리기', hero:'k', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "k" }, new[] { "dummy", "dummy" }); K.Hand(b, "kh");
            K.E(b, 0).Hp = 50;
            K.Play(b, "kh", 0);
            Assert.AreEqual(1000 - 50, K.Hp(b, 1), "초과 50 → 10당 1 × 10");
        }

        // ── 층 · 검사기 · 글 · 저장 ────────────────────────────────────
        [Test] public void 파티_층_상태를_아군_1명에게_걸면_검사기_오류()
        {
            var d = D("[{id:'bad', name:'잘못', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'사기', v:1, target:'oneAlly'}]}, {id:'bad2', name:'잘못2', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'구속', v:1, target:'party'}]}, {id:'bad3', name:'잘못3', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'독', v:1}]}]");
            var all = string.Join("\n", Validator.Check(d).Errors);
            StringAssert.Contains("사기 는 파티 층 상태", all); StringAssert.Contains("구속 는 개인 층 상태", all); StringAssert.Contains("cardStatus 로", all);
        }

        [Test] public void 검사기는_고유_효과_이름_겹침과_안_쓰임을_잡는다()
        {
            var d = K.Data(heroes: "[{id:'x1', name:'하나', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'같은 이름'}}, {id:'x2', name:'둘', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'같은 이름'}}, {id:'x3', name:'셋', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'사기'}}]");
            var v = Validator.Check(d);
            StringAssert.Contains("겹치면 안 된다", string.Join("\n", v.Errors));
            StringAssert.Contains("엔진 키워드", string.Join("\n", v.Errors));
            StringAssert.Contains("어디에도 안 쓰인다", string.Join("\n", v.Warnings));
            StringAssert.Contains("고유 효과(keyword)가 없다", string.Join("\n", v.Warnings));
        }

        [Test] public void 툴팁과_칩()
        {
            var d = K.Data(heroes: "[{id:'p', name:'시계', role:'딜러', row:'back', hp:600, atk:100, def:20, keyword:{name:'초침', desc:'째깍째깍 쌓이는 시간', cap:5}}]",
                cards: "[{id:'cc', name:'칩', hero:'p', cost:1, type:'공격', tags:['보존', '소멸 2'], fx:[{k:'status', id:'사기', v:1}, {k:'stack', id:'초침', v:1}, {k:'ifKill'}, {k:'ap', v:1}]}]");
            var t = new CardText(d);
            var tips = t.Tips();
            StringAssert.Contains("째깍째깍", tips["초침"]); StringAssert.Contains("20%p", tips["사기"]);
            CollectionAssert.AreEqual(new[] { "보존", "소멸", "사기", "초침", "처치" }, t.Chips(d.View("cc")));
            Assert.AreEqual("보존. 소멸 2. 사기 1, 「초침」 +1. 처치: AP +1", t.Card(d.Card("cc")));
            StringAssert.Contains("뽑을 더미 맨 위", CardText.Tip("망각"));
        }

        // ── 판 쪽 ──────────────────────────────────────────────────────
        [Test] public void 판_제거_태그_정신_붕괴_봉인된_금기_판_단위_성장()
        {
            var d = K.Sample().Add(null, @"[{id:'st1', name:'잠든 금기', hero:'rico', cost:1, type:'스킬', tags:['봉인된 금기'], becomes:'st2', fx:[{k:'draw', v:1}]},
 {id:'st2', name:'깨어난 금기', hero:'rico', cost:1, type:'스킬', tags:['금기'], fx:[{k:'draw', v:2}]},
 {id:'rm1', name:'한 번뿐', hero:'rico', cost:0, type:'스킬', tags:['제거'], fx:[]}]", null, null, null, null);
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 7);
            run.S.Deck.Add("st1"); run.S.Deck.Add("rm1");
            run.S.Node = 3;   // 보스 칸
            var (b, _) = run.OpenFight();
            b.Removed.Add("rm1");
            b.GrowthGain["rico"] = new Stats { Atk = 5 };
            b.Pool.Status["정신 붕괴"] = 1;
            b.Over = "win";
            run.AfterFight(b);
            CollectionAssert.DoesNotContain(run.S.Deck, "rm1", "제거 — 덱에서 빠졌다");
            CollectionAssert.Contains(run.S.Deck, "st2", "봉인된 금기 — 보스를 처치하자 금기 카드로");
            Assert.AreEqual(5, run.S.Growth["rico"].Atk);
            Assert.IsTrue(run.MindBroken);
            StringAssert.Contains("정신 붕괴", run.ClaimGlow("st2", new Glow { Kind = "hero", Options = new List<string> { "st2" } }, 0));
            var (b2, _) = run.OpenFight();
            Assert.AreEqual(d.Hero("rico").Atk + 5, b2.Party[0].Atk, "판 단위 성장은 다음 싸움에도");
            run.AfterFight(b2);
            Assert.IsFalse(run.MindBroken, "한 싸움이 지나면 풀린다");
        }

        [Test] public void 적_정보_글()
        {
            var d = K.Sample(); var t = new CardText(d);
            var s = t.Enemy(d.Enemy("fairy_knight"));
            StringAssert.Contains("벌침: 공격이 아닌 카드에 약이 오른다. 1개당 주는 피해 +10%. 공격이 아닌 카드를 낼 때마다 +1. 3이 되면 즉시: 공격 150", s);
            StringAssert.Contains("마지막 침: 쓰러질 때 파티 취약 +2", s);
        }

        [Test] public void 상태마다_건_쪽을_보인다()
        {
            var d = D("[{id:'v_a', name:'가 취약', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'취약', v:2, target:'oneEnemy'}, {k:'status', id:'사기', v:1}]}, {id:'v_b', name:'나 취약', hero:'b', cost:0, type:'스킬', fx:[{k:'status', id:'취약', v:1, target:'oneEnemy'}]}]",
                "[{id:'curser', name:'저주꾼', hp:1000, intents:[{t:'debuff', id:'약화', v:2, rush:0}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "curser" }); K.Hand(b, "v_a", "v_b");
            K.Play(b, "v_a"); K.Play(b, "v_b");
            var vul = b.StatusViews(K.E(b)).Single(v => v.Id == "취약");
            Assert.AreEqual(3, vul.Stacks);
            Assert.AreEqual("a", vul.Sources[0].Hero); Assert.AreEqual(2, vul.Sources[0].Stacks);
            Assert.AreEqual("b", vul.Sources[1].Hero); Assert.AreEqual(1, vul.Sources[1].Stacks);
            b.EndTurn();
            var weak = b.StatusViews(b.Pool).Single(v => v.Id == "약화");
            Assert.AreEqual("enemy", weak.Sources[0].Kind); Assert.AreEqual(0, weak.Sources[0].Enemy);
            Assert.AreEqual("party", b.StatusViews(b.Party[1]).Count == 0 ? "party" : "hero", "사기 · 약화는 파티 층 — 사도 칩에는 없다");
            K.Hand(b, "hit"); K.Play(b, "hit");
            Assert.AreEqual(2, b.StatusViews(K.E(b)).Single(v => v.Id == "취약").Stacks, "한 겹 쓰면 먼저 건 쪽부터 줄인다");
            Assert.AreEqual(1, b.StatusViews(K.E(b)).Single(v => v.Id == "취약").Sources[0].Stacks);
        }

        [Test] public void 저장하고_되살려도_카드_상태와_결속이_남는다()
        {
            var b = K.Fight(); K.Hand(b, "hit");
            b.CardSt["hit"] = new Dictionary<string, int> { ["탐구심"] = 2 }; b.Bond["hit"] = 3;
            var c = Battle.Load(b.Data, b.Save());
            Assert.AreEqual(2, c.CardStOf("hit", "탐구심")); Assert.AreEqual(3, c.BondOf("hit"));
        }
    }
}
