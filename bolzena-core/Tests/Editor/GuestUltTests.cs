using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 2026-11-01 실비아 「초청객」(안 A 「손님 고르기」 — content-v2 _measure/실비아_벨라_우로스_설계안_20261101.md §2-1) 엔진 부품.
    /// 계기 ult + marked(그 표시가 붙은 동료가 고학년을 쓰면) · 고학년 규칙의 「아군 1명」 = 쓴 사도 · 키워드 noOwner(주인 자신에게는 안 붙음) · 글.
    /// </summary>
    public class GuestUltTests
    {
        const string HEROES = @"[
 {id:'sv', name:'실', role:'서포터', hp:600, atk:100, def:40, crit:0,
  keyword:{name:'초청객', carrier:'hero', cap:2, noOwner:true, per:[{stat:'ultCost', v:-0.2}]},
  passives:[{name:'진은의 대공', when:{on:'ult', marked:'초청객'}, fx:[{k:'status', id:'은방울', v:2}, {k:'stack', id:'초청객', v:-2, target:'oneAlly'}]}],
  ult:{name:'실 한 방', cost:100, fx:[{k:'dmg', ratio:0.1, target:'allEnemies'}]}},
 {id:'ga', name:'가가', role:'딜러', hp:600, atk:100, def:20, crit:0, ult:{name:'가 한 방', cost:100, fx:[{k:'dmg', ratio:0.1, target:'allEnemies'}]}}
]";
        const string CARDS = @"[
 {id:'invite', name:'초대', hero:'sv', cost:0, type:'스킬', fx:[{k:'stack', id:'초청객', v:1, target:'oneAlly'}]},
 {id:'seat', name:'옮겨 앉히기', hero:'sv', cost:0, type:'스킬', fx:[{k:'moveMark', id:'초청객', target:'oneAlly'}]}
]";
        static Battle F() => K.Fight(K.Data(heroes: HEROES, cards: CARDS), new[] { "sv", "ga" }, new[] { "dummy" }, st => st.Gauge = 200);

        [Test] public void 초청객은_주인에게_안_붙고_동료_고학년을_앞당긴다()
        {
            var b = F();
            int sv = b.HeroUnit("sv").Idx, ga = b.HeroUnit("ga").Idx;
            K.Hand(b, "invite"); K.Play(b, "invite", 0, new PlayOpts { Ally = sv });
            Assert.AreEqual(0, b.StackOf("sv", "초청객"), "noOwner — 주인 자신에게는 안 붙는다");
            K.Hand(b, "invite", "invite", "invite");
            for (int i = 0; i < 3; i++) K.Play(b, "invite", 0, new PlayOpts { Ally = ga });
            Assert.AreEqual(2, b.StackOf("ga", "초청객"), "최대 2");
            Assert.AreEqual(60, b.UltCostOf("ga"), "1당 -20% × 2");
            Assert.AreEqual(100, b.UltCostOf("sv"), "주인 고학년은 그대로");
            K.Hand(b, "seat"); K.Play(b, "seat", 0, new PlayOpts { Ally = sv });
            Assert.AreEqual(2, b.StackOf("ga", "초청객"), "moveMark 도 주인에게는 옮기지 않는다");
            Assert.AreEqual(0, b.StackOf("sv", "초청객"));
        }

        [Test] public void 표시_붙은_동료가_고학년을_쓰면_돌고_그_동료의_표시를_지운다()
        {
            var b = F();
            int ga = b.HeroUnit("ga").Idx;
            Assert.IsTrue(b.UseUlt("sv").Ok);
            Assert.AreEqual(0, b.St(b.Pool, "은방울"), "표시 없는 사도(주인)의 고학년에는 안 돈다");
            K.Hand(b, "invite", "invite");
            K.Play(b, "invite", 0, new PlayOpts { Ally = ga }); K.Play(b, "invite", 0, new PlayOpts { Ally = ga });
            Assert.IsTrue(b.UseUlt("ga").Ok);
            Assert.AreEqual(40, b.Gauge, "200 - 100(주인) - 60(깎인 요구)");
            Assert.AreEqual(2, b.St(b.Pool, "은방울"), "초청객 붙은 동료의 고학년 → 은방울 2");
            Assert.AreEqual(0, b.StackOf("ga", "초청객"), "아군 1명 = 고학년을 쓴 사도 — 그 동료의 표시가 사라진다");
            Assert.AreEqual(100, b.UltCostOf("ga"));
        }

        [Test] public void 글_고학년_표시_계기와_요구_게이지()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS);
            var t = new CardText(d);
            var h = d.Hero("sv");
            StringAssert.Contains("「초청객」이 붙은 아군이 고학년을 쓰면", t.Rule(h.Passives[0]));
            StringAssert.Contains("그 아군의 「초청객」 -2", t.Rule(h.Passives[0]));
            StringAssert.Contains("「초청객」 1당 붙은 아군의 고학년 요구 게이지 -20%", t.Keyword(h.Keyword));
        }
    }
}
