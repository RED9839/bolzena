using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>정신 붕괴(판, 2026-10-12 다시 만듦) — 다음 전투의 매 턴 시작에 손으로 「망령의 속삭임」. 카드 얻기 · 신탁 · 제거는 막지 않는다.</summary>
    public class MindBreakTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        const string CARDS = @"[{ id:'st_mindwhisper', name:'망령의 속삭임', cost:1, type:'상태', tags:['소멸', '증발'],
            fx:[{k:'when', on:'handEnd'}, {k:'status', id:'약화', v:2, target:'party'}] }]";
        const string EVS = @"[{ id:'M1', name:'늪', pool:'공용', options:[
            { label:'꿈 엿보기', out:[{k:'flash'}, {k:'mindBreak', n:1}] },
            { label:'짐 버리기', out:[{k:'remove', n:1}] } ] }]";

        static Run New()
        {
            var d = K.Sample(); d.Add(null, CARDS, null, null, EVS, null);
            return Run.New(d, PARTY, 5);
        }

        [Test] public void 이벤트_정신_붕괴는_다음_전투_매_턴_망령의_속삭임()
        {
            var run = New();
            run.S.Event = new EventState { Key = "t", Choices = new List<string> { "M1" }, Id = "M1" };
            run.Choose(0);
            Assert.IsTrue(run.MindBroken);
            Assert.IsTrue(run.S.Event.Log.Any(l => l.Contains("망령의 속삭임")), "결과 글에 효과가 보인다");
            // 옛 효과(카드 얻기 · 제거 막힘)는 없다
            Assert.IsNull(run.LockOf(run.OptionsOf(run.Data.Event("M1"))[1]));

            var (b, _) = run.OpenFight();
            Assert.AreEqual(1, b.Hand.Count(id => id == R.MIND_CARD), "첫 턴 손에 한 장");
            Assert.IsFalse(run.S.Deck.Contains(R.MIND_CARD), "덱에는 들지 않는다");
            int weak0 = b.St(b.Pool, "약화");
            b.EndTurn();
            if (b.Over == null)
            {
                Assert.GreaterOrEqual(b.St(b.Pool, "약화"), weak0 + 2, "떨치지 않고 넘기면 턴 끝에 약화 2");
                Assert.AreEqual(1, b.Hand.Count(id => id == R.MIND_CARD), "다음 턴에도 한 장(지난 것은 흩어짐)");
            }
            b.Over = "win";
            run.AfterFight(b);
            Assert.IsFalse(run.MindBroken, "한 싸움이 지나면 풀린다");
            var (b2, _) = run.OpenFight();
            Assert.IsFalse(b2.Hand.Contains(R.MIND_CARD));
        }

        [Test] public void 정신_붕괴_칩_글에_효과가_보인다()
        {
            var run = New();
            var t = new CardText(run.Data);
            Assert.AreEqual("유령의 장난 1: 다음 전투 1번, 매 턴 「망령의 속삭임」", t.Outcome(new Outcome { K = "mindBreak", N = 1 }));
        }
    }
}
