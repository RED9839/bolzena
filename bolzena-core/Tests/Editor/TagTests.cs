using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>카드 태그 · 조건(docs/18 §4).</summary>
    public class TagTests
    {
        static GameData D(string cards) => K.Data(cards: cards);

        [Test] public void 개전은_첫_손패에_든다()
        {
            var d = D("[{id:'first', name:'먼저', hero:'a', cost:1, type:'스킬', tags:['개전'], fx:[{k:'draw', v:1}]}]");
            var deck = Enumerable.Repeat("zero", 20).Concat(new[] { "first" }).ToList();
            for (int seed = 1; seed < 6; seed++)
            {
                var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => { st.Deck = deck; st.Seed = seed; });
                Assert.Contains("first", b.Hand);
            }
        }

        [Test] public void 보존은_손에_남고_증발은_사라진다()
        {
            var d = D("[{id:'keep', name:'남김', hero:'a', cost:1, type:'스킬', tags:['보존'], fx:[]}, {id:'gone', name:'김', hero:'a', cost:1, type:'스킬', tags:['증발', '보존'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "keep", "gone", "zero");
            b.EndTurn();
            Assert.AreEqual(1, b.Hand.Count(x => x == "keep"));
            CollectionAssert.DoesNotContain(b.Hand, "gone");
            CollectionAssert.Contains(b.Gone, "gone");
            CollectionAssert.DoesNotContain(b.Gone, "zero", "버려진 카드는 버린 더미로(다시 섞여 뽑힌다)");
        }

        [Test] public void 소멸과_소멸_N()
        {
            var d = D("[{id:'ex', name:'한 번', hero:'a', cost:0, type:'스킬', tags:['소멸'], fx:[]}, {id:'ex2', name:'두 번', hero:'a', cost:0, type:'스킬', tags:['소멸 2'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "ex", "ex2");
            K.Play(b, "ex"); CollectionAssert.Contains(b.Gone, "ex");
            K.Play(b, "ex2"); CollectionAssert.Contains(b.Discard, "ex2");
            b.Hand.Add("ex2"); b.Discard.Remove("ex2");
            K.Play(b, "ex2"); CollectionAssert.Contains(b.Gone, "ex2");
        }

        [Test] public void 강화_카드는_내면_이_전투에서_사라지고_전투_내내_버프()
        {
            var d = D("[{id:'pw', name:'강화', hero:'a', cost:1, type:'강화', unique:true, fx:[{k:'atkMod', v:0.1, run:true, target:'self'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "pw", "hit");
            K.Play(b, "pw");
            CollectionAssert.Contains(b.Gone, "pw");
            K.Play(b, "hit");
            Assert.AreEqual(890, K.Hp(b));
            for (int i = 0; i < 5; i++) b.EndTurn();
            Assert.AreEqual(0.1, b.StatMod(b.Party[0], "atk"), 1e-9, "전투 내내");
        }

        [Test] public void 전투_내내_공격력은_사도마다_150퍼센트까지()
        {
            var d = D("[{id:'up', name:'북돋기', hero:'a', cost:0, type:'스킬', fx:[{k:'atkMod', v:0.6, run:true, target:'self'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" });
            for (int i = 0; i < 4; i++) { K.Hand(b, "up"); K.Play(b, "up"); }
            Assert.AreEqual(1.5, b.StatMod(b.Party[0], "atk"), 1e-9);
        }

        [Test] public void 종극은_턴을_끝낸다()
        {
            var d = D("[{id:'fin', name:'끝', hero:'a', cost:0, type:'스킬', tags:['종극'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "fin", "zero");
            var r = K.Play(b, "fin");
            Assert.IsTrue(r.Finale);
            Assert.IsNotNull(b.CanPlay("zero"));
            b.EndTurn();
            Assert.IsNull(b.CanPlay("zero"));
        }

        [Test] public void 주도는_턴_첫_카드면_코스트_1_덜()
        {
            var d = D("[{id:'lead', name:'앞장', hero:'a', cost:2, type:'스킬', tags:['주도'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "lead", "zero");
            b.LeadOn.Add("lead");
            Assert.AreEqual(1, b.CostOf("lead"));
            K.Play(b, "zero");
            Assert.AreEqual(2, b.CostOf("lead"), "다른 카드를 먼저 내면 풀린다");
        }

        [Test] public void 연계는_다른_사도의_카드를_내면_공짜로_저절로()
        {
            var d = D("[{id:'link', name:'끼어들기', hero:'b', cost:1, type:'공격', tags:['연계'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "link", "hit");
            K.Play(b, "hit");
            Assert.AreEqual(1000 - 100 - 80, K.Hp(b));
            Assert.AreEqual(2, b.Ap, "연계는 공짜");
            CollectionAssert.Contains(b.Discard, "link");
        }

        [Test] public void 같은_사도의_카드는_연계를_안_깨운다()
        {
            var d = D("[{id:'link_a', name:'가 끼어들기', hero:'a', cost:1, type:'공격', tags:['연계'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "link_a", "hit");
            K.Play(b, "hit");
            CollectionAssert.Contains(b.Hand, "link_a");
        }

        [Test] public void 천상은_코스트_2_이상을_내면()
        {
            var d = D("[{id:'heaven', name:'하늘', hero:'b', cost:1, type:'스킬', tags:['천상'], fx:[{k:'draw', v:1}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "heaven", "hit", "big");
            K.Play(b, "hit");
            CollectionAssert.Contains(b.Hand, "heaven");
            K.Play(b, "big");
            CollectionAssert.DoesNotContain(b.Hand, "heaven");
        }

        [Test] public void 연결은_직접_내면_다른_연결_카드를_버린다()
        {
            var d = D("[{id:'c1', name:'연결1', hero:'a', cost:0, type:'스킬', tags:['연결'], fx:[]}, {id:'c2', name:'연결2', hero:'a', cost:0, type:'스킬', tags:['연결'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "c1", "c2", "zero");
            K.Play(b, "c1");
            CollectionAssert.AreEqual(new[] { "zero" }, b.Hand);
            CollectionAssert.Contains(b.Discard, "c2");
        }

        [Test] public void 개막은_전투가_시작되면_AP_를_써서_저절로()
        {
            var d = D("[{id:'curtain', name:'막', hero:'a', cost:2, type:'스킬', tags:['개막'], fx:[{k:'status', id:'불굴', v:1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = new List<string> { "zero", "zero", "zero", "zero", "zero", "zero", "curtain" });
            Assert.AreEqual(1, b.St(b.Pool, "불굴"));
            Assert.AreEqual(1, b.Ap);
            CollectionAssert.DoesNotContain(b.Draw, "curtain");
        }

        [Test] public void 봉인은_처음엔_효과_없이_풀리기만()
        {
            var d = D("[{id:'seal', name:'봉인된 것', hero:'a', cost:0, type:'공격', tags:['봉인'], fx:[{k:'dmg', ratio:2.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "seal");
            K.Play(b, "seal");
            Assert.AreEqual(1000, K.Hp(b));
            b.Hand.Add("seal"); b.Discard.Remove("seal");
            K.Play(b, "seal");
            Assert.AreEqual(800, K.Hp(b));
        }

        [Test] public void 회수는_버린_더미_대신_손으로_N번()
        {
            var d = D("[{id:'rc', name:'돌아옴', hero:'a', cost:0, type:'스킬', tags:['회수 2'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "rc");
            K.Play(b, "rc"); CollectionAssert.Contains(b.Hand, "rc");
            K.Play(b, "rc"); CollectionAssert.Contains(b.Hand, "rc");
            K.Play(b, "rc"); CollectionAssert.DoesNotContain(b.Hand, "rc");
            CollectionAssert.Contains(b.Discard, "rc");
        }

        [Test] public void 연쇄는_다음_턴_시작에_한_번_더()
        {
            var d = D("[{id:'echo', name:'메아리', hero:'a', cost:1, type:'공격', tags:['연쇄'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "echo");
            K.Play(b, "echo");
            Assert.AreEqual(900, K.Hp(b));
            b.EndTurn();
            Assert.AreEqual(800, K.Hp(b));
            Assert.AreEqual(3, b.Ap);
        }

        [Test] public void 사용_불가는_못_낸다()
        {
            var b = K.Fight(K.Data(cards: "[{id:'block_hand', name:'막힘', cost:0, type:'상태', tags:['사용 불가', '증발']}]"), new[] { "a" }, new[] { "dummy" });
            K.Hand(b, "block_hand");
            Assert.IsNotNull(b.CanPlay("block_hand"));
            Assert.IsFalse(b.PlayCard(0, 0).Ok);
        }

        [Test] public void 분쇄는_방어가_남은_적에게_20퍼센트()
        {
            var d = D("[{id:'crush', name:'부수기', hero:'a', cost:0, type:'공격', tags:['분쇄'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "crush");
            K.E(b).Block = 50;
            K.Play(b, "crush");
            Assert.AreEqual(1000 - 70, K.Hp(b), "120 - 방어 50");
        }

        [Test] public void 약점_태그는_성격과_상관없이_약점_공격()
        {
            var d = D("[{id:'weak', name:'급소', hero:'a', cost:0, type:'공격', tags:['약점'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "weak");
            K.Play(b, "weak");
            Assert.AreEqual(875, K.Hp(b), "약점 피해 +25%");
            Assert.AreEqual(3, K.E(b).Tough, 1e-9, "약점 공격 — 0코도 1코처럼 1칸");
        }

        // ── 조건 ──
        [Test] public void 파괴는_대상이_처치됐을_때()
        {
            var d = D("[{id:'fin', name:'마무리', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'ifBroken'}, {k:'ap', v:2}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "dummy" }); K.Hand(b, "fin", "fin");
            K.Play(b, "fin", 0);
            Assert.AreEqual(2, b.Ap);
            b.Enemies[1].Hp = 50;
            K.Play(b, "fin", 1);
            Assert.AreEqual(1 + 2, b.Ap);
        }

        [Test] public void 조율은_코스트가_남은_AP와_같을_때()
        {
            var d = D("[{id:'tune', name:'맞춤', hero:'a', cost:1, type:'스킬', fx:[{k:'ifTune'}, {k:'draw', v:2}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = Enumerable.Repeat("zero", 10).ToList());
            K.Hand(b, "tune", "tune");
            K.Play(b, "tune");
            Assert.AreEqual(1, b.Hand.Count);
            b.Ap = 1;
            K.Play(b, "tune");
            Assert.AreEqual(2, b.Hand.Count);
        }

        [Test] public void 연속은_바로_앞_카드가_같은_성격일_때()
        {
            var d = K.Data(heroes: "[{id:'m1', name:'광1', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'광기'}, {id:'m2', name:'광2', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'광기'}]",
                cards: "[{id:'m1z', name:'광1 숨', hero:'m1', cost:0, type:'스킬', fx:[]}, {id:'m2c', name:'이어서', hero:'m2', cost:0, type:'스킬', fx:[{k:'ifChain'}, {k:'gauge', v:50}]}]");
            var b = K.Fight(d, new[] { "m1", "m2" }, new[] { "dummy" }); K.Hand(b, "m2c", "m1z", "m2c");
            K.Play(b, "m2c");
            Assert.AreEqual(0, b.Gauge);
            K.Play(b, "m1z");
            K.Play(b, "m2c");
            Assert.AreEqual(50, b.Gauge);
        }

        [Test] public void 잇기는_같은_사도의_카드가_앞일_때_리듬_1이_저절로()
        {
            var d = D("[{id:'tie', name:'잇기', hero:'a', cost:0, type:'스킬', fx:[{k:'ifLink'}, {k:'gauge', v:30}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "zero", "tie", "zero_b", "tie");
            K.Play(b, "zero"); K.Play(b, "tie");
            Assert.AreEqual(30, b.Gauge);
            Assert.AreEqual(1, b.St(b.Pool, R.RHYTHM));
            K.Play(b, "zero_b"); K.Play(b, "tie");
            Assert.AreEqual(30, b.Gauge, "교주 · 다른 사도 카드가 끼면 끊긴다");
            b.EndTurn();
            Assert.AreEqual(0, b.St(b.Pool, R.RHYTHM), "리듬은 턴이 끝나면 사라진다");
        }

        [Test] public void 앞이_공격()
        {
            var d = D("[{id:'after', name:'뒤따라', hero:'b', cost:0, type:'스킬', fx:[{k:'ifPrev', type:'공격'}, {k:'gauge', v:20}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" }); K.Hand(b, "after", "hit", "after");
            K.Play(b, "after");
            Assert.AreEqual(0, b.Gauge);
            K.Play(b, "hit");
            K.Play(b, "after");
            Assert.AreEqual(10 + 20, b.Gauge);
        }

        [Test] public void 영감은_능력으로_뽑힐_때만()
        {
            var d = D("[{id:'insp', name:'영감', hero:'a', cost:1, type:'스킬', fx:[{k:'when', on:'draw'}, {k:'gauge', v:40}]}, {id:'drawer', name:'뽑기', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = new List<string> { "insp", "insp", "insp", "insp", "insp", "insp" });
            Assert.AreEqual(0, b.Gauge, "턴 시작의 뽑기는 아니다");
            b.Hand.Add("drawer");
            K.Play(b, "drawer");
            Assert.AreEqual(40, b.Gauge);
            int i = b.Hand.IndexOf("insp");
            b.PlayCard(i, 0);
            Assert.AreEqual(40 + 10, b.Gauge, "내면 영감 마디는 안 돈다");
        }

        [Test] public void 적이_끼운_상태_카드의_영감은_턴_시작에도()
        {
            var d = D("[{id:'dizzy', name:'어지럼', cost:0, type:'상태', tags:['소멸'], fx:[{k:'when', on:'draw'}, {k:'ap', v:-1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = new List<string> { "dizzy" });
            Assert.AreEqual(2, b.Ap);
        }

        [Test] public void 안식은_효과로_버려질_때()
        {
            var d = D("[{id:'rest', name:'안식', hero:'a', cost:1, type:'스킬', fx:[{k:'when', on:'discard'}, {k:'gauge', v:25}]}, {id:'toss', name:'버리기', hero:'a', cost:0, type:'스킬', fx:[{k:'discard', v:1}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "toss", "zero", "rest");
            Assert.AreEqual(1, b.DiscardChoice(0));
            K.Play(b, "toss", 0, new PlayOpts { Discard = new List<string> { "rest" } });
            Assert.AreEqual(25, b.Gauge);
            CollectionAssert.AreEqual(new[] { "zero" }, b.Hand);
        }

        [Test] public void 턴_끝에_손에_있으면()
        {
            var d = D("[{id:'note', name:'쪽지', cost:1, type:'상태', tags:['소멸'], fx:[{k:'when', on:'handEnd'}, {k:'payHp', v:90}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }); K.Hand(b, "note");
            b.EndTurn();
            Assert.AreEqual(910, b.Pool.Hp);
        }

        [Test] public void 축복_불타는_웅변_가벼운_발걸음_그_카드만의_축복()
        {
            var d = D("[{id:'bl', name:'축복 받을 카드', hero:'a', cost:2, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}], blesses:[{name:'덤', fx:[{k:'gauge', v:30}], tags:['보존']}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Shin = new Dictionary<string, string> { ["hit"] = "power", ["big"] = "cost", ["bl"] = "own" });
            K.Hand(b, "hit", "big", "bl");
            Assert.AreEqual(1, b.CostOf("big"), "가벼운 발걸음 — 코스트 -1");
            K.Play(b, "hit");
            Assert.AreEqual(870, K.Hp(b), "불타는 웅변 — 피해 ×1.3");
            K.Play(b, "big");
            Assert.AreEqual(770, K.Hp(b));
            b.EndTurn();
            CollectionAssert.Contains(b.Hand, "bl", "그 카드만의 축복의 태그(보존)");
            K.Play(b, "bl");
            Assert.AreEqual(10 + 10 + 20 + 30, b.Gauge, "축복의 덤 효과(게이지 +30)");
        }
    }
}
