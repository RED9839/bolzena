using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 빌드 피드백 엔진 요청(2026-10-10) — 자원 공유 소비자용 셈 갈래(perPile · ifPile 의 from: token · broken · drawn · discards · morale)와
    /// 「생성 카드를 낼 때」 계기 거르개(when.token).
    /// </summary>
    public class BuildFeedbackTests
    {
        const string CARDS = "[{id:'tk', name:'생성물', hero:'a', cost:0, type:'스킬', token:true, fx:[{k:'draw', v:1}]}," +
            "{id:'pt', name:'생성 세기', hero:'a', cost:0, type:'공격', fx:[{k:'perPile', from:'token'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}," +
            "{id:'pm', name:'사기 세기', hero:'a', cost:0, type:'공격', fx:[{k:'perPile', from:'morale'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}]";

        [Test] public void 손의_생성_카드_장수를_센다()
        {
            var d = K.Data(cards: CARDS);
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.EnemyHp = 100000);
            K.Hand(b, "pt", "tk", "tk");
            Assert.AreEqual(2, b.PileCount("token"));
            int hp0 = K.Hp(b);
            K.Play(b, "pt");
            Assert.AreEqual(hp0 - 20, K.Hp(b), "생성 카드 2장 × 공격력 100 의 10%");
        }

        [Test] public void 생성_카드를_낼_때만_도는_계기()
        {
            var d = K.Data(heroes: "[{id:'g', name:'소비자', role:'딜러', row:'back', hp:500, atk:100, def:50, crit:0, passives:[{name:'받아 쓰기', when:{on:'play', who:'any', token:true}, fx:[{k:'gauge', v:10}]}]}]", cards: CARDS);
            var b = K.Fight(d, new[] { "a", "g" }, new[] { "dummy" }, st => st.EnemyHp = 100000);
            int g0 = b.Gauge;
            K.Hand(b, "pt"); K.Play(b, "pt");
            Assert.AreEqual(g0, b.Gauge, "생성 카드가 아니면 안 돈다");
            K.Hand(b, "tk"); K.Play(b, "tk");
            Assert.Greater(b.Gauge, g0, "생성 카드를 내면 돈다");
        }

        [Test] public void 셈_갈래의_글과_검사()
        {
            var d = K.Data(cards: CARDS);
            var t = new CardText(d);
            StringAssert.Contains("손의 생성 카드 1장당", t.Card(d.Card("pt")));
            StringAssert.Contains("파티 사기 1당", t.Card(d.Card("pm")));
            Assert.IsFalse(Validator.Check(d).Errors.Any(e => e.Contains("from 은")), "새 갈래도 검사를 지난다");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" });
            Assert.AreEqual(0, b.PileCount("broken"));
            Assert.AreEqual(0, b.PileCount("discards"));
        }
    }
}
