using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 카드 이동 · 드로우 — 카제나 규칙(Docs/카드이동.md, 2026-10-10). 감응 카드(sense)는 드로우될 때마다 게이지 +40 — 게이지로 발동 횟수를 센다.
    /// </summary>
    public class CardMoveTests
    {
        const string CARDS = @"[
 {id:'sense', name:'감응', hero:'a', cost:1, type:'스킬', fx:[{k:'when', on:'drawAny'}, {k:'gauge', v:40}]},
 {id:'mk', name:'만들기', hero:'a', cost:0, type:'스킬', fx:[{k:'make', id:'sense', v:1}]},
 {id:'dr', name:'한 장', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:1}]},
 {id:'dr2', name:'또 한 장', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:1}]},
 {id:'drAtk', name:'공격 찾기', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:1, type:'공격'}]},
 {id:'pickAtk', name:'골라 드로우', hero:'a', cost:0, type:'스킬', fx:[{k:'pull', from:'draw', type:'공격'}]},
 {id:'pickAny', name:'뽑을 더미에서', hero:'a', cost:0, type:'스킬', fx:[{k:'pull', from:'draw'}]},
 {id:'grave', name:'무덤에서', hero:'a', cost:0, type:'스킬', fx:[{k:'pull', from:'discard'}]},
 {id:'scrap', name:'버린 카드에서', hero:'a', cost:0, type:'스킬', fx:[{k:'pull', from:'discarded'}]}
]";
        static Battle B(params string[] hand)
        {
            var b = K.Fight(K.Data(cards: CARDS), new[] { "a" }, new[] { "dummy" });
            b.Draw.Clear(); b.Discard.Clear(); b.Used.Clear(); b.Gauge = 0; b.Ap = 9;
            K.Hand(b, hand);
            return b;
        }

        // 규칙 1 — 생성은 드로우가 아니다
        [Test] public void 생성은_드로우가_아니라_감응이_없다()
        {
            var b = B("mk"); int drawn = b.Drawn;
            K.Play(b, "mk");
            Assert.IsTrue(b.Hand.Any(id => GameData.BaseId(id) == "sense")); Assert.AreEqual(0, b.Gauge, "생성 — 감응 없음"); Assert.AreEqual(drawn, b.Drawn);
        }

        // 규칙 2 · 3 — 뽑을 더미 → 손은 드로우(효과 드로우 · 꺼내기 모두 감응)
        [Test] public void 효과_드로우는_감응_발동()
        {
            var b = B("dr"); b.Draw.Add("sense");
            K.Play(b, "dr");
            CollectionAssert.Contains(b.Hand, "sense"); Assert.AreEqual(40, b.Gauge);
        }

        [Test] public void 턴_시작_드로우도_감응_발동()
        {
            var b = B(); b.Draw.AddRange(Enumerable.Repeat("sense", 5));
            b.EndTurn();
            Assert.AreEqual(5 * 40, b.Gauge);
        }

        [Test] public void 뽑을_더미에서_손으로_꺼내기는_드로우_감응_발동()
        {
            var b = B("pickAny"); b.Draw.Add("sense"); int drawn = b.Drawn;
            K.Play(b, "pickAny");
            CollectionAssert.Contains(b.Hand, "sense"); Assert.AreEqual(40, b.Gauge); Assert.AreEqual(drawn + 1, b.Drawn);
        }

        [Test] public void 버린_더미에서_손으로는_드로우가_아니라_감응이_없다()
        {
            var b = B("grave"); b.Discard.Add("sense");
            K.Play(b, "grave");
            CollectionAssert.Contains(b.Hand, "sense"); Assert.AreEqual(0, b.Gauge);
        }

        // 규칙 4 — 섞기는 이번 턴에 낸 카드를 빼고
        [Test] public void 섞기에_이번_턴에_낸_카드는_돌아가지_않는다()
        {
            var b = B("zero", "dr"); b.Discard.Add("guard");
            K.Play(b, "zero");
            CollectionAssert.Contains(b.Used, "zero");
            K.Play(b, "dr");
            CollectionAssert.Contains(b.Hand, "guard", "버린 카드는 섞여 드로우");
            CollectionAssert.Contains(b.Discard, "zero", "이번 턴에 낸 카드는 버린 더미에 남는다");
            CollectionAssert.DoesNotContain(b.Draw, "zero");
        }

        [Test] public void 드로우_카드만_도는_덱도_한_턴에_끝없이_돌지_않는다()
        {
            var b = B("dr"); b.Discard.Add("dr2");
            int plays = 0;
            while (plays < 50)
            {
                int i = b.Hand.FindIndex(id => id == "dr" || id == "dr2");
                if (i < 0) break;
                Assert.IsTrue(b.PlayCard(i, 0).Ok); plays++;
            }
            Assert.AreEqual(2, plays, "dr → dr2 섞여 드로우 → 낸 카드뿐이라 더 섞지 않는다");
            Assert.AreEqual(0, b.Hand.Count); Assert.AreEqual(0, b.Draw.Count);
        }

        [Test] public void 턴이_끝나면_이번_턴에_낸_카드도_다시_섞인다()
        {
            var b = B("zero");
            K.Play(b, "zero");
            b.EndTurn();
            Assert.AreEqual(0, b.Used.Count);
            CollectionAssert.Contains(b.Hand, "zero", "다음 턴 시작 드로우에 섞여 돌아온다");
        }

        [Test] public void 조건_드로우는_맞는_카드가_없으면_버린_카드를_섞어_찾는다()
        {
            var b = B("drAtk"); b.Draw.Add("guard"); b.Discard.Add("hit");
            K.Play(b, "drAtk");
            CollectionAssert.Contains(b.Hand, "hit");
        }

        // 규칙 5 — 무덤(버린 더미 전부)은 이번 턴에 낸 카드도 · 버린 카드에서는 못 가져온다
        [Test] public void 버린_더미에서_손으로는_이번_턴에_낸_카드도_가져온다()
        {
            var b = B("zero", "grave");
            K.Play(b, "zero");
            K.Play(b, "grave");
            CollectionAssert.Contains(b.Hand, "zero"); CollectionAssert.DoesNotContain(b.Used, "zero");
        }

        [Test] public void 버린_카드에서는_이번_턴에_낸_카드를_못_가져온다()
        {
            var b = B("zero", "scrap"); b.Discard.Add("guard");
            K.Play(b, "zero");
            K.Play(b, "scrap");
            CollectionAssert.Contains(b.Hand, "guard"); CollectionAssert.DoesNotContain(b.Hand, "zero");

            var c = B("zero", "scrap");
            K.Play(c, "zero");
            K.Play(c, "scrap");
            Assert.AreEqual(0, c.Hand.Count, "버린 카드가 없다 — 불발"); CollectionAssert.Contains(c.Discard, "zero");
        }

        // 규칙 6 — 고른 뒤 드로우는 뽑을 더미에 없으면 섞지 않고 불발
        [Test] public void 뽑을_더미에서_고르는_드로우는_없으면_섞지_않고_불발()
        {
            var b = B("pickAtk"); b.Draw.Add("guard"); b.Discard.Add("hit");
            K.Play(b, "pickAtk");
            CollectionAssert.DoesNotContain(b.Hand, "hit");
            CollectionAssert.AreEqual(new[] { "guard" }, b.Draw, "섞지 않았다");
            CollectionAssert.Contains(b.Discard, "hit"); CollectionAssert.Contains(b.Discard, "pickAtk", "카드는 사용됨");
        }

        [Test] public void 이번_턴에_낸_카드는_저장해도_남는다()
        {
            var b = B("zero");
            K.Play(b, "zero");
            var l = Battle.Load(K.Data(cards: CARDS), b.Save());
            CollectionAssert.AreEqual(b.Used, l.Used);
        }

        // 버그(2026-10-10 적 리뉴얼 측정 · 우로스 「순환」) — 섞자마자 「뽑을 더미를 섞으면 드로우 1」 이 섞인 한 장을 먼저 가져가 뽑을 더미가 비면 범위 밖 예외
        [Test] public void 섞은_뒤_계기가_뽑을_더미를_비워도_예외_없이_멈춘다()
        {
            var d = K.Data(heroes: "[{id:'u', name:'순환이', role:'탱커', row:'front', hp:1000, atk:100, def:50, crit:0, passives:[{name:'순환', when:{on:'shuffle'}, fx:[{k:'draw', v:1}]}]}]", cards: CARDS);
            var b = K.Fight(d, new[] { "a", "u" }, new[] { "dummy" });
            b.Draw.Clear(); b.Discard.Clear(); b.Used.Clear(); b.Ap = 9;
            K.Hand(b, "zero", "dr"); b.Discard.Add("guard");
            K.Play(b, "zero");   // 버린 더미: guard(버린 카드) · zero(이번 턴에 낸 카드) — 섞으면 한 장뿐
            Assert.DoesNotThrow(() => K.Play(b, "dr"));
            CollectionAssert.Contains(b.Hand, "guard"); Assert.AreEqual(0, b.Draw.Count);
        }

        [Test] public void 출처마다_카드_글()
        {
            var d = K.Data(cards: CARDS); var t = new CardText(d);
            StringAssert.Contains("버린 더미 맨 위 카드 1장을 손으로", t.Card(d.Card("grave")));
            StringAssert.Contains("이번 턴에 낸 카드 빼고", t.Card(d.Card("scrap")));
            StringAssert.Contains("드로우(없으면 섞지 않음)", t.Card(d.Card("pickAtk")));
            StringAssert.Contains("생성", t.Card(d.Card("mk")));
        }
    }
}
