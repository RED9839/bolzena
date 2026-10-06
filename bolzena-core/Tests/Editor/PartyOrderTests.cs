using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>사도 열(전열 · 중열 · 후열)을 걷어 낸 뒤(2026-10-06) — 맞는 모습은 편성 순서, 옛 데이터 · 저장은 깨지지 않음, 단계 이름(stages).</summary>
    public class PartyOrderTests
    {
        [Test] public void 맞는_사도는_편성_순서_맨_앞_관통은_맨_뒤()
        {
            var us = new List<Unit> { new Unit { Idx = 2 }, new Unit { Idx = 0 }, new Unit { Idx = 1 } };
            Assert.AreEqual(0, Battle.PickInOrder(us, false).Idx, "보통 공격 — 편성 순서 맨 앞");
            Assert.AreEqual(2, Battle.PickInOrder(us, true).Idx, "관통 — 맨 뒤");
            us.RemoveAll(u => u.Idx == 0);
            Assert.AreEqual(1, Battle.PickInOrder(us, false).Idx, "맨 앞이 빠지면 그다음");
            Assert.IsNull(Battle.PickInOrder(new List<Unit>(), false));
        }

        [Test] public void 옛_row_칸은_읽고_버리며_주의로_알린다()
        {
            var d = K.Data(heroes: "[{id:'old', name:'옛', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0}, {id:'new', name:'새', role:'딜러', hp:600, atk:100, def:20, crit:0}]");
            Assert.IsTrue(d.Hero("old").HadRow);
            Assert.IsFalse(d.Hero("new").HadRow);
            var v = Validator.Check(d);
            Assert.IsTrue(v.Warnings.Any(w => w.Contains("사도 old") && w.Contains("row")), "옛 row → 주의");
            Assert.IsFalse(v.Warnings.Any(w => w.Contains("사도 new") && w.Contains("row")));
            Assert.IsFalse(v.Errors.Any(e => e.Contains("사도 old") || e.Contains("사도 new")), "row 가 있든 없든 오류는 아니다");
        }

        [Test] public void 옛_저장의_rows_를_읽어도_깨지지_않는다()
        {
            var d = K.Sample();
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 3);
            var json = run.Save();
            StringAssert.DoesNotContain("\"rows\"", json, "새 저장에는 열이 없다");
            var old = json.Insert(json.IndexOf('{') + 1, "\n  \"rows\": { \"rico\": \"front\", \"carrot\": \"mid\", \"sion\": \"back\" },");
            var back = Run.Load(d, old);
            CollectionAssert.AreEqual(run.S.Party, back.S.Party, "편성 순서는 그대로");
            StringAssert.DoesNotContain("\"rows\"", back.Save(), "다시 저장하면 옛 열은 사라진다");
        }

        const string STAGED = "[{id:'st', name:'단계', role:'서포터', hp:600, atk:100, def:20, crit:0, keyword:{name:'모습', desc:'시험', carrier:'self', cap:3, wrap:true, stages:['꿈결','심판','축복']}," +
            " passives:[{name:'교리', when:{on:'play'}, conds:[{c:'stack', id:'모습', n:2, max:2}], fx:[{k:'status', id:'사기', v:1}]}]}]";

        [Test] public void 단계_이름이_있으면_글이_숫자_대신_이름으로()
        {
            var d = K.Data(heroes: STAGED, cards: "[{id:'stc', name:'넘기기', hero:'st', cost:0, type:'스킬', fx:[{k:'stack', id:'모습', v:1}, {k:'ifStack', id:'모습', n:3, max:3}, {k:'draw', v:1}]}]");
            var tx = new CardText(d);
            StringAssert.Contains("「모습」이 축복이면", tx.Card(d.Card("stc")));
            var sheet = tx.HeroShort(d.Hero("st"));
            StringAssert.Contains("「모습」이 심판이면", sheet);
            StringAssert.Contains("꿈결 → 심판 → 축복 → 다시 꿈결", sheet);
            Assert.IsFalse(Validator.Check(d).Errors.Any(e => e.Contains("사도 st")));
        }

        [Test] public void 단계_이름_수가_cap_과_다르면_오류()
        {
            var d = K.Data(heroes: STAGED.Replace("stages:['꿈결','심판','축복']", "stages:['꿈결','심판']"));
            Assert.IsTrue(Validator.Check(d).Errors.Any(e => e.Contains("사도 st") && e.Contains("stages")));
        }

        [Test] public void 태세_조건은_그_단계에서만_돈다()
        {
            var d = K.Data(heroes: STAGED, cards: "[{id:'stc', name:'넘기기', hero:'st', cost:0, type:'스킬', fx:[{k:'stack', id:'모습', v:1}]}]");
            var b = K.Fight(d, new[] { "st", "b", "c" }, new[] { "dummy" });
            int Morale() => b.St(b.Pool, "사기");
            void Once() { K.Hand(b, "stc"); K.Play(b, "stc"); }
            Once();   // 0 → 1(꿈결) — 심판 아님
            Assert.AreEqual(0, Morale());
            Once();   // 1 → 2(심판)
            Assert.AreEqual(1, Morale());
            Once();   // 2 → 3(축복)
            Assert.AreEqual(1, Morale());
        }
    }
}
