using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>강화 카드 지속 규칙(power) — 켜짐 · 매 턴 · 계기 · 횟수 · 겹 · 항상 증감 · 칩 · 쪽지 · 저장 · 복사 · 글 · 검사 · 값어치.</summary>
    public class PowerTests
    {
        const string CARDS = @"[
 {id:'pw_t', name:'매일 다짐', hero:'a', unique:true, cost:1, type:'강화',
  fx:[{k:'draw', v:1}, {k:'power', rules:[{when:{on:'turnStart'}, fx:[{k:'status', id:'결의', v:1}]}]}]},
 {id:'pw_p', name:'연타 습관', hero:'a', unique:true, cost:0, type:'강화',
  fx:[{k:'power', rules:[{when:{on:'play', type:'공격'}, limit:{per:'turn', n:1}, fx:[{k:'gauge', v:5}]}]}]},
 {id:'pw_a', name:'단련', hero:'a', unique:true, cost:0, type:'강화',
  fx:[{k:'power', id:'단련', rules:[{when:{on:'always'}, fx:[{k:'atkMod', v:0.1, target:'allAllies'}]}]}]},
 {id:'bad_sk', name:'잘못된 스킬', hero:'a', cost:1, type:'스킬', fx:[{k:'power', rules:[{when:{on:'turnStart'}, fx:[{k:'draw', v:1}]}]}]},
 {id:'bad_fs', name:'잘못된 강화', hero:'a', unique:true, cost:1, type:'강화', fx:[{k:'power', rules:[{when:{on:'fightStart'}, fx:[{k:'draw', v:1}]}]}]},
 {id:'bad_empty', name:'빈 강화', hero:'a', unique:true, cost:1, type:'강화', fx:[{k:'power', rules:[]}]},
 {id:'once_pw', name:'한 번짜리', hero:'a', unique:true, cost:1, type:'강화', fx:[{k:'draw', v:2}]}
]";
        static GameData D() => K.Data(cards: CARDS);
        static Battle F(List<Cue> cues = null) => K.Fight(D(), new[] { "a", "b" }, new[] { "dummy" }, null, cues);

        [Test] public void 내면_켜지고_매_턴_시작에_돈다()
        {
            var cues = new List<Cue>();
            var b = F(cues);
            K.Hand(b, "pw_t");
            K.Play(b, "pw_t");
            Assert.AreEqual(1, b.PowerStacks("a", "매일 다짐"), "id 가 없으면 카드 이름");
            Assert.AreEqual(0, b.St(b.Pool, "결의"), "낸 턴에는 아직");
            Assert.IsFalse(b.Hand.Contains("pw_t"));
            b.EndTurn();
            Assert.AreEqual(1, b.St(b.Pool, "결의"), "다음 턴 시작에 결의 1");
            b.EndTurn();
            Assert.AreEqual(2, b.St(b.Pool, "결의"), "전투 끝까지 매 턴");
            var on = cues.Last(c => c.K == "powerOn");
            Assert.AreEqual("a", on.Hero); Assert.AreEqual("매일 다짐", on.Name); Assert.AreEqual("pw_t", on.CardId); Assert.AreEqual(1, on.V);
        }

        [Test] public void 같은_강화를_또_켜면_겹이_늘고_효과가_겹만큼()
        {
            var b = F();
            K.Hand(b, "pw_t", "pw_t");
            K.Play(b, "pw_t"); K.Play(b, "pw_t");
            Assert.AreEqual(1, b.Powers.Count, "칩은 하나");
            Assert.AreEqual(2, b.PowerStacks("a", "매일 다짐"));
            b.EndTurn();
            Assert.AreEqual(2, b.St(b.Pool, "결의"), "겹 2 — 한 번 발동에 효과 두 번");
        }

        [Test] public void 계기와_횟수_제한()
        {
            var b = F();
            b.Ap = 10;
            K.Hand(b, "pw_p", "hit", "hit", "hit_b");
            K.Play(b, "pw_p");
            int g0 = b.Gauge;
            K.Play(b, "hit");
            int g1 = b.Gauge;
            K.Play(b, "hit");
            int g2 = b.Gauge;
            K.Play(b, "hit_b");
            int g3 = b.Gauge;
            Assert.AreEqual(5, (g1 - g0) - (g2 - g1), "턴당 1회 — 두 번째 공격에는 안 돈다");
            Assert.AreEqual(g3 - g2, g2 - g1, "다른 사도의 카드에는 안 돈다(who 없음 = 주인)");
        }

        [Test] public void 항상_증감은_겹만큼_늘_걸린다()
        {
            var b = F();
            var a = b.HeroUnit("a"); var bb = b.HeroUnit("b");
            K.Hand(b, "pw_a", "pw_a");
            K.Play(b, "pw_a");
            Assert.AreEqual(110, b.AtkNow(a)); Assert.AreEqual(88, b.AtkNow(bb), "allAllies — 파티 전원");
            K.Play(b, "pw_a");
            Assert.AreEqual(120, b.AtkNow(a), "겹 2");
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual(120, b.AtkNow(a), "턴이 지나도 남는다");
        }

        [Test] public void 강화_칩_상태_보기_화면_API()
        {
            var b = F();
            K.Hand(b, "pw_t");
            K.Play(b, "pw_t");
            var chip = b.StatusViews(b.Pool).Single(s => s.Power);
            Assert.AreEqual("매일 다짐", chip.Id); Assert.AreEqual(1, chip.Stacks); Assert.AreEqual("party", chip.Layer); Assert.AreEqual("a", chip.Sources[0].Hero);
            var pv = b.PowersOf().Single();
            Assert.AreEqual("a", pv.Hero); Assert.AreEqual("pw_t", pv.Card); Assert.AreEqual(1, pv.Stacks);
            StringAssert.Contains("매 턴 시작 시 결의 1", pv.Text);
            Assert.AreEqual(0, b.PowersOf("b").Count);
        }

        [Test] public void 저장_왕복과_복사()
        {
            var d = D();
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy" });
            K.Hand(b, "pw_t", "pw_a");
            var pre = b.Clone();
            K.Hand(pre, "pw_t", "pw_a");
            pre.PlayCard(0, 0);
            Assert.AreEqual(0, b.Powers.Count, "복사판에서 켠 강화는 원본에 없다");
            K.Play(b, "pw_t"); K.Play(b, "pw_a");
            var s = Battle.Load(d, b.Save());
            Assert.AreEqual(2, s.Powers.Count);
            Assert.AreEqual(110, s.AtkNow(s.HeroUnit("a")), "항상 증감도 되살아난다");
            s.EndTurn();
            Assert.AreEqual(1, s.St(s.Pool, "결의"), "되살린 판에서도 매 턴");
        }

        [Test] public void 글_수치가_바로_보인다()
        {
            var d = D();
            var tx = new CardText(d);
            Assert.AreEqual("드로우 1. 이 전투 동안 매 턴 시작 시 결의 1", tx.Card(d.Card("pw_t")));
            Assert.AreEqual("이 전투 동안 공격 카드를 낼 때마다 고학년 게이지 +5% (턴당 1회)", tx.Card(d.Card("pw_p")));
            Assert.AreEqual("이 전투 동안 공격력 +10%", tx.Card(d.Card("pw_a")));
            Assert.IsTrue(new CardText(d).Chips(d.View("pw_t")).Contains("결의"), "칩에 규칙 속 상태도");
        }

        [Test] public void 강화_카드의_세기형_버프는_이_전투_동안으로_읽힌다()
        {
            var d = K.Data(cards: @"[{id:'pw_s', name:'다짐', hero:'a', unique:true, cost:1, type:'강화', fx:[{k:'status', id:'결의', v:1}, {k:'status', id:'사기', v:1}, {k:'draw', v:1}]},
                                     {id:'sk_s', name:'다짐 스킬', hero:'a', cost:1, type:'스킬', fx:[{k:'status', id:'결의', v:1}]}]");
            var tx = new CardText(d);
            Assert.AreEqual("이 전투 동안 결의 1 · 사기 1, 드로우 1", tx.Card(d.Card("pw_s")));
            Assert.AreEqual("결의 1", tx.Card(d.Card("sk_s")), "강화가 아니면 그대로");
        }

        [Test] public void 검사기()
        {
            var v = Validator.Check(D());
            Assert.IsTrue(v.Errors.Any(e => e.Contains("bad_sk") && e.Contains("power")), "강화가 아닌 카드에 power");
            Assert.IsTrue(v.Errors.Any(e => e.Contains("bad_fs") && e.Contains("fightStart")), "fightStart 는 돌지 않는다");
            Assert.IsTrue(v.Errors.Any(e => e.Contains("bad_empty") && e.Contains("rules")), "빈 규칙");
            Assert.IsTrue(v.Warnings.Any(w => w.Contains("once_pw") && w.Contains("남는 효과")), "남는 효과 없는 강화 카드는 주의");
            Assert.IsFalse(v.Errors.Any(e => e.Contains("pw_t") || e.Contains("pw_p") || e.Contains("pw_a")));
            Assert.IsFalse(v.Warnings.Any(w => w.Contains("pw_t") && w.Contains("남는 효과")));
        }

        [Test] public void 값어치_규칙이_값을_가진다()
        {
            var d = D();
            Assert.Greater(CardValue.ValueOf(d.View("pw_t")), 0.4 + 0.5, "드로우 1 보다 크다 — 매 턴 결의");
            Assert.Greater(CardValue.ValueOf(d.View("pw_a")), 0.5);
        }

        [Test] public void 봇이_강화_카드를_낸다()
        {
            var b = F();
            K.Hand(b, "pw_t");
            new Bots(D()).SmartPlay(b);
            Assert.AreEqual(1, b.Powers.Count, "봇이 강화를 켠다");
        }
    }
}
