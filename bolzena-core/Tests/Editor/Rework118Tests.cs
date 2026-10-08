using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 18갈래 3단계(2026-10-08) — 나머지 118명을 다시 쓰며 더한 작은 부품.
    /// perGuarded · perOverheal 의 max(최대 몇 번) · 계기 exhaust · discard 의 who other 검사기 허용(엔진은 이미 됨).
    /// </summary>
    public class Rework118Tests
    {
        const string HEROES = @"[
 {id:'p', name:'주인', role:'딜러', hp:600, atk:100, def:20, keyword:{name:'주머니', cap:9}, passives:[PASSIVE]},
 {id:'q', name:'짝', role:'탱커', hp:900, atk:200, def:60, keyword:{name:'짝주머니', cap:9}}]";
        const string CARDS = @"[
 {id:'ovmax', name:'넘친 만큼(최대 둘)', hero:'p', cost:0, type:'스킬', fx:[{k:'heal', ratio:10}, {k:'perOverheal', pct:0.75, per:25, max:2}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'ov', name:'넘친 만큼', hero:'p', cost:0, type:'스킬', fx:[{k:'heal', ratio:10}, {k:'perOverheal', pct:0.75, per:25}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'cp', name:'대신 내기(주인)', hero:'p', cost:0, type:'스킬', fx:[{k:'castOther'}]},
 {id:'cq', name:'대신 내기(짝)', hero:'q', cost:0, type:'스킬', fx:[{k:'castOther'}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'gmax', name:'막은 만큼', hero:'p', cost:0, type:'공격', fx:[{k:'perGuarded', per:10, max:3}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}]";

        static GameData Data(string passive = "{name:'없음', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}")
            => K.Data(heroes: HEROES.Replace("PASSIVE", passive), cards: CARDS);
        static Battle Two(GameData d, string foe = "dummy") => K.Fight(d, new[] { "p", "q" }, new[] { foe });

        [Test] public void 넘친_회복_비례에_최대가_선다()
        {
            // 파티 HP 1000 → 1200, 75% 선(1125)을 넘은 몫 75 · per 25 = 3회 — max 2 면 2회
            var a = Two(Data()); a.Pool.Hp = 1000; K.Hand(a, "ov");
            int one = a.HitAmount(a.HeroUnit("p"), 1.0);
            K.Play(a, "ov");
            Assert.AreEqual(1000 - 3 * one, K.Hp(a), 2, "최대 없음 — 3회");
            var b = Two(Data()); b.Pool.Hp = 1000; K.Hand(b, "ovmax");
            K.Play(b, "ovmax");
            Assert.AreEqual(1000 - 2 * one, K.Hp(b), 2, "max 2 — 2회");
        }

        [Test] public void 막아_낸_양_비례에_최대가_선다()
        {
            var b = Two(Data(), "hitter");
            b.Pool.Shield = 5000;
            b.EndTurn();
            Assert.Greater(b.GuardedPrev, 30, "막아 낸 양이 per 10 × max 3 보다 크다");
            K.Hand(b, "gmax");
            int one = b.HitAmount(b.HeroUnit("p"), 0.1), hp0 = K.Hp(b);
            K.Play(b, "gmax");
            Assert.AreEqual(3 * one, hp0 - K.Hp(b), 2, "max 3 — 3회까지만");
        }

        [Test] public void 최대는_글에_보인다()
        {
            var d = Data(); var t = new CardText(d);
            StringAssert.Contains("(최대 2)", t.Fx(d.Card("ovmax").Fx));
            StringAssert.Contains("(최대 3)", t.Fx(d.Card("gmax").Fx));
        }

        [Test] public void 대신_발동_카드끼리_서로_부르지_않는다()
        {
            var b = Two(Data()); K.Hand(b, "cp", "cq");
            int hp0 = K.Hp(b);
            K.Play(b, "cp");
            Assert.AreEqual(hp0, K.Hp(b), "손의 짝 카드는 대신 발동 카드라 고르지 않는다(끝없는 연쇄 없음)");
        }

        [Test] public void 검사기_소멸_버림_계기에_who_other_를_받는다()
        {
            var ok = Validator.Check(Data(passive: "{name:'남의 소멸', when:{on:'exhaust', who:'other'}, fx:[{k:'stack', id:'주머니', v:1}]}"));
            Assert.IsFalse(ok.Errors.Any(x => x.Contains("who other")), string.Join("\n", ok.Errors));
            var ok2 = Validator.Check(Data(passive: "{name:'남의 버림', when:{on:'discard', who:'other'}, fx:[{k:'stack', id:'주머니', v:1}]}"));
            Assert.IsFalse(ok2.Errors.Any(x => x.Contains("who other")), string.Join("\n", ok2.Errors));
            var bad = Validator.Check(Data(passive: "{name:'틀림', when:{on:'turnStart', who:'other'}, fx:[{k:'stack', id:'주머니', v:1}]}"));
            Assert.IsTrue(bad.Errors.Any(x => x.Contains("who other")));
            var bad2 = Validator.Check(K.Data(heroes: HEROES.Replace("PASSIVE", "{name:'없음', when:{on:'ult'}, fx:[{k:'gauge', v:1}]}"), cards: "[{id:'x', name:'틀림', hero:'p', cost:0, type:'스킬', fx:[{k:'perGuarded', per:10, max:-1}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]}]"));
            Assert.IsTrue(bad2.Errors.Any(x => x.Contains("max 는 0 이상")));
        }
    }
}
