using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>교주 카드 주인 사도 — 덱에 넣을 때 고른 사도의 카드로 낸다(「id@사도」). 피해 · 실드는 주인 능력치.</summary>
    public class NeutralOwnerTests
    {
        const string NCARDS = @"[
 {id:'nx', name:'교주 한 방', cost:1, type:'공격', grade:'일반', price:50, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'ns', name:'교주 막기', cost:1, type:'스킬', grade:'일반', price:50, fx:[{k:'shield', ratio:1.0}]}
]";

        static int Hit(string party0, string party1, string cardId)
        {
            var b = K.Fight(K.Data(cards: NCARDS), new[] { party0, party1 }, new[] { "dummy" });
            K.Hand(b, cardId);
            int hp = K.Hp(b);
            K.Play(b, cardId);
            return hp - K.Hp(b);
        }

        [Test] public void 같은_교주_카드도_주인_공격력에_따라_피해가_다르다()
        {
            int byA = Hit("a", "c", "nx@a");   // a 공격력 100
            int byC = Hit("a", "c", "nx@c");   // c 공격력 150
            Assert.Greater(byA, 0);
            Assert.Greater(byC, byA, "공격력이 높은 사도에게 넣은 교주 카드가 더 아프다");
            Assert.AreEqual(byA * 3 / 2, byC, 1);
        }

        [Test] public void 실드는_주인_방어력()
        {
            int Shield(string id)
            {
                var b = K.Fight(K.Data(cards: NCARDS), new[] { "a", "c" }, new[] { "dummy" });
                K.Hand(b, id); K.Play(b, id);
                return b.Pool.Shield;
            }
            Assert.Greater(Shield("ns@a"), Shield("ns@c"), "방어력 50(a) > 20(c)");
        }

        [Test] public void 카드_모습에_주인이_실린다_주인_없으면_첫_사도()
        {
            var b = K.Fight(K.Data(cards: NCARDS), new[] { "c", "a" }, new[] { "dummy" });
            Assert.AreEqual("a", b.CardOf("nx@a").Owner);
            Assert.AreEqual("a", b.CardOf("nx@a").Hero);
            Assert.IsTrue(b.CardOf("nx@a").Neutral);
            Assert.AreEqual("c", b.CardOf("nx").Hero, "주인 없는 옛 카드는 첫 사도");
            Assert.IsNull(b.CardOf("hit").Owner, "사도 카드는 Owner 가 없다");
            Assert.AreEqual("nx", GameData.BaseId("nx@a^"));
            Assert.AreEqual("a", GameData.OwnerOf("nx@a^"));
            Assert.AreEqual("nx@c^", GameData.WithOwner("nx@a^", "c"));
            Assert.AreEqual("nx", GameData.NoOwner("nx@a"));
            Assert.AreEqual("교주 한 방", b.Data.Card("nx@a").Name);
        }

        static Run NewRun()
        {
            var d = K.Sample().Add(null, NCARDS, null, null, null, null);
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 1);
            run.S.Gold = 1000;
            run.S.Shop = new ShopState { Items = new List<ShopItem> { new ShopItem { Id = "nx", Kind = "neutral", Price = 10 }, new ShopItem { Id = "nx", Kind = "neutral", Price = 10 } } };
            return run;
        }

        [Test] public void 상점에서_사면_주인을_고르고_저장해도_남는다()
        {
            var run = NewRun();
            Assert.IsNull(run.Buy(0));
            Assert.AreEqual("nx", run.PendingNeutral, "주인을 기다린다");
            Assert.IsFalse(run.S.Deck.Any(x => GameData.BaseId(x) == "nx"));
            Assert.IsNotNull(run.AssignNeutral("nobody"));
            Assert.IsNull(run.AssignNeutral("sion"));
            Assert.IsNull(run.PendingNeutral);
            CollectionAssert.Contains(run.S.Deck, "nx@sion");
            // 바로 주인을 주는 길
            Assert.IsNull(run.Buy(1, "rico"));
            CollectionAssert.Contains(run.S.Deck, "nx@rico");
            // 저장 왕복
            var back = Run.Load(run.Data, run.Save());
            CollectionAssert.Contains(back.S.Deck, "nx@sion");
            CollectionAssert.Contains(back.S.Deck, "nx@rico");
            Assert.AreEqual("sion", back.ViewOf("nx@sion").Owner);
            // 기다리는 줄도 저장된다
            var w = NewRun(); w.Buy(0);
            Assert.AreEqual("nx", Run.Load(w.Data, w.Save()).PendingNeutral);
        }

        [Test] public void 판에서_주인에_따라_피해가_다르고_저장해도_같다()
        {
            var run = NewRun();
            run.Buy(0, "rico"); run.Buy(1, "sion");
            var back = Run.Load(run.Data, run.Save());
            var (b, _) = back.OpenFight();
            int Dmg(string id)
            {
                var c = b.Clone();
                c.Hand.Clear(); c.Hand.Add(id); c.Ap = 9;
                var e = c.Enemies.First(x => !x.Dead);
                int hp = e.Hp + e.Block + e.Shield;
                Assert.IsTrue(c.PlayCard(0, e.Idx).Ok);
                return hp - (e.Hp + e.Block + e.Shield);
            }
            Assert.Greater(Dmg("nx@sion"), Dmg("nx@rico"), "시온(공격 140) > 리코(공격 90)");
        }

        [Test] public void 옛_저장의_주인_없는_교주_카드는_첫_사도에게()
        {
            var run = NewRun();
            run.S.Deck.Add("nx"); run.S.Shin["nx"] = "power";
            var back = Run.Load(run.Data, run.Save());
            CollectionAssert.Contains(back.S.Deck, "nx@rico");
            CollectionAssert.DoesNotContain(back.S.Deck, "nx");
            Assert.AreEqual("power", back.S.Shin["nx@rico"]);
        }

        [Test] public void 이벤트_카드_받기_복제_유일()
        {
            var run = NewRun();
            run.S.Event = new EventState { Phase = "result", Pending = new List<Pending> { new Pending { K = "card", Cards = new List<string> { "nx" }, Label = "교주 카드" } } };
            Assert.IsNull(run.ResolvePending("nx", "carrot"));
            CollectionAssert.Contains(run.S.Deck, "nx@carrot");
            Assert.AreEqual("nx@carrot^", run.AddCopy("nx@carrot"), "복제본도 주인 그대로");
            // 유일 — 주인이 달라도 덱에 한 장
            run.S.Deck.Add("n_bond@sion");
            Assert.IsNotNull(run.PowerWhy("n_bond"));
        }

        const string OCARDS = @"[
 {id:'cz', name:'골칫덩이', cost:1, type:'저주', grade:'일반', price:0, fx:[]},
 {id:'gz', name:'작은 선물', cost:0, type:'스킬', grade:'일반', price:0, gift:true, fx:[{k:'draw', n:1}]}
]";

        [Test] public void 선물_카드는_주인을_고르고_저주는_주인_없이_덱에_전투_모습은_그대로()
        {
            var d = K.Sample().Add(null, NCARDS, null, null, null, null).Add(null, OCARDS, null, null, null, null);
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 1);
            Assert.IsTrue(d.Card("gz").Ownable && d.Card("nx").Ownable);
            Assert.IsFalse(d.Card("cz").Ownable, "저주는 사도 덱에 넣지 않는다(2026-10-06)");
            Assert.IsFalse(d.Card("hit")?.Ownable ?? false, "사도 카드는 주인을 고르지 않는다");
            Assert.AreEqual("cz", run.GainCard("cz"), "저주 — 주인 없이 바로 덱에");
            Assert.IsNull(run.PendingNeutral);
            CollectionAssert.Contains(run.S.Deck, "cz");
            // 옛 저장의 주인 붙은 저주(cz@carrot)도 그대로 읽힌다
            run.S.Deck.Add("cz@carrot");
            Assert.AreEqual("gz@sion", run.GainCard("gz", "sion"), "선물 — 바로 주인을 주는 길");
            // 주인은 덱 묶음 표시만 — 전투 모습에는 사도가 실리지 않는다(주인이 쓰러져도 저주 · 선물은 그대로)
            Assert.IsNull(run.ViewOf("cz@carrot").Hero);
            Assert.IsNull(run.ViewOf("gz@sion").Hero);
            Assert.IsTrue(run.ViewOf("cz@carrot").IsCurse);
            var back = Run.Load(run.Data, run.Save());
            CollectionAssert.Contains(back.S.Deck, "cz@carrot");
            CollectionAssert.Contains(back.S.Deck, "gz@sion");
        }
    }
}
