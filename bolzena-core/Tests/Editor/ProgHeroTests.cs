using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 잠재력 사도 칸(카제나식 — 2026-10-20 사용자 「카제나식 잠재능력으로」): 03-1 · 05-1 · 07(kind hero — 카드 덤 · 문턱) · 05 · 06(stat of hero — 사도마다 능력치) · 05-2(shin — 축복 확률) · 갈래(after).
    /// </summary>
    public class ProgHeroTests
    {
        const string UNIQUE = "[{id:'uc', name:'다 고유', hero:'c', cost:2, type:'공격', unique:true, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]";

        static ProgTable T(string heroes = null) => GameData.FromJson<ProgTable>(@"{
 potential:[
  {id:'01', kind:'start', max:1},
  {id:'03', kind:'card', of:'neutral', v:0.02, max:10},
  {id:'03-1', kind:'hero', max:1, after:'03'},
  {id:'04', kind:'card', of:'unique', v:0.02, max:10, after:'03'},
  {id:'05', kind:'stat', of:'hero', max:5, after:'04'},
  {id:'05-1', kind:'hero', max:1, after:'05'},
  {id:'05-2', kind:'shin', v:4, max:1, after:'05'},
  {id:'07', kind:'hero', max:1, after:'05'}],
 heroes:" + (heroes ?? @"{
  c:{
   '03-1':{name:'숙련', desc:'기본 카드가 1장 더 뽑는다', cards:[{card:'hit_c', fx:[{k:'draw', v:1}]}]},
   '05':{name:'치명 · 공격 숙련', desc:'치명 +2 · 공격 +10%', stat:{crit:2, 'atk%':0.1}},
   '05-1':{name:'숙련', desc:'기본 +10%', cards:[{card:'hit_c', up:0.1}]},
   '07':{name:'고유', desc:'고유 비용 −1 · +50%', cards:[{card:'uc', cost:-1, up:0.5}]}}}") + @",
 presets:{}
}");

        static ProgState One(string hero, Action<ProgHero> f)
        {
            var h = new ProgHero();
            f?.Invoke(h);
            return new ProgState { Heroes = { [hero] = h } };
        }

        static int Dmg(GameData d, string card, Dictionary<string, ProgMods> prog)
        {
            var b = K.Fight(d, new[] { "c" }, new[] { "dummy" }, st => st.Prog = prog);
            K.Hand(b, card); b.Ap = 5;
            int h0 = K.Hp(b); K.Play(b, card);
            return h0 - K.Hp(b);
        }

        [Test] public void 사도_칸_03_1은_그_기본_카드에_조각을_덧붙이고_다른_카드_다른_사도는_그대로()
        {
            var d = K.Data(cards: UNIQUE);
            var pb = Prog.Build(d, T(), One("c", h => h.Pot["03-1"] = 1), new[] { "c" });
            Assert.AreEqual(1, pb.Mods["c"].Cards.Count);
            var b = K.Fight(d, new[] { "c" }, new[] { "dummy" }, st => st.Prog = pb.Mods);
            Assert.IsTrue(b.CardOf("hit_c").Fx.Any(f => f.K == FxK.Draw), "카드 모습(글 · 봇)에 덧붙인 조각");
            Assert.IsFalse(b.CardOf("uc").Fx.Any(f => f.K == FxK.Draw), "다른 카드는 그대로");
            Assert.IsFalse(K.Fight(d, new[] { "c" }, new[] { "dummy" }).CardOf("hit_c").Fx.Any(f => f.K == FxK.Draw), "성장 없으면 그대로");
            K.Hand(b, "hit_c"); b.Ap = 5;
            b.Draw.Add("uc");   // 뽑을 카드가 있게
            int hand0 = b.Hand.Count;
            K.Play(b, "hit_c");
            Assert.AreEqual(hand0, b.Hand.Count, "낸 1장 빠지고 1장 뽑음");
            Assert.IsNull(Prog.Build(d, T(), One("c", null), new[] { "c" }).Mods["c"].Cards, "칸을 안 열면 카드 덤 없음");
        }

        [Test] public void 사도_칸_05_1은_그_기본_카드만_키우고_능력치_문턱_덤은_없다()
        {
            var d = K.Data(cards: UNIQUE);
            var t = T();
            var low = Prog.Build(d, t, One("c", h => h.Pot["05-1"] = 1), new[] { "c" });
            Assert.AreEqual(1, low.Mods["c"].Cards.Count);
            int d0 = Dmg(d, "hit_c", null), d1 = Dmg(d, "hit_c", low.Mods);
            Assert.AreEqual(d0 * 1.1, d1, 1.0, "+10%(예전 「+5%, 문턱이면 +5% 더」 를 한 몫으로)");
            var hi = Prog.Build(d, t, One("c", h => { h.Pot["05"] = 5; h.Pot["05-1"] = 1; }), new[] { "c" });
            Assert.AreEqual(1, hi.Mods["c"].Cards.Count, "05 를 다 올려 공격이 150% 여도 덤이 더 붙지 않는다(10-22 문턱 없앰)");
        }

        [Test] public void 사도_칸_07_비용_덤과_피해_덤()
        {
            var d = K.Data(cards: UNIQUE);
            var pb = Prog.Build(d, T(), One("c", h => h.Pot["07"] = 1), new[] { "c" });
            var b = K.Fight(d, new[] { "c" }, new[] { "dummy" }, st => st.Prog = pb.Mods);
            Assert.AreEqual(1, b.CostOf("uc"), "비용 −1");
            Assert.AreEqual(2, K.Fight(d, new[] { "c" }, new[] { "dummy" }).CostOf("uc"));
            Assert.AreEqual(Dmg(d, "uc", null) * 1.5, Dmg(d, "uc", pb.Mods), 1.0, "고유 카드 +50%(문턱 없이 늘)");
        }

        [Test] public void 능력치_칸_05는_사도마다_다른_능력치를_단계만큼()
        {
            var d = K.Data(cards: UNIQUE);
            var pb = Prog.Build(d, T(), One("c", h => h.Pot["05"] = 5), new[] { "c" });
            Assert.AreEqual(10, pb.Mods["c"].Base.Crit, "치명 +2 × 5");
            Assert.AreEqual(Num.Round(d.Hero("c").Atk * 1.5) - d.Hero("c").Atk, pb.Mods["c"].Base.Atk, "공격 +10% × 5 — 공개판 기본치에 곱한다");
            var none = Prog.Build(d, T(), new ProgState { Heroes = { ["a"] = new ProgHero { Pot = { ["05"] = 5 } } } }, new[] { "a" });
            Assert.IsTrue(none.Mods["a"].Empty, "그 사도 줄이 없으면 아무것도 안 든다");
        }

        [Test] public void 축복_적응_05_2는_신탁_후보의_축복_확률을_곱하고_난수_수는_그대로()
        {
            var d = K.Sample();
            var party = new List<string> { "rico", "carrot", "sion" };
            var hero = party.First(k => d.UniquesOf(k).Any(id => d.Card(id)?.Oracles.Count > 0));
            var card = d.UniquesOf(hero).First(id => d.Card(id).Oracles.Count > 0);
            var t = T("{}");
            d.Prog = t;
            var pb = Prog.Build(d, t, One(hero, h => h.Pot["05-2"] = 1), party);
            Assert.AreEqual(4, pb.Mods[hero].Shin, 1e-9);
            var r0 = Run.New(d, party, 3); var r1 = Run.New(d, party, 3);
            r1.ApplyProg(pb);
            int with = 0, without = 0;
            for (int i = 0; i < 40; i++)
            {
                if (r1.RollOracles(card).Any(o => o.Shin != null)) with++;
                if (r0.RollOracles(card).Any(o => o.Shin != null)) without++;
            }
            Assert.AreEqual(40, with, "×(1 + 4) = 100% — 늘 축복");
            Assert.Less(without, 40);
        }

        [Test] public void 표_검사_갈래는_앞_칸만_사도_칸은_모든_사도에()
        {
            var d = K.Data(cards: UNIQUE);
            var t = T();
            var errs = Prog.Check(d, t);
            Assert.IsTrue(errs.Any(e => e.Contains("a 에 칸")), "사도 a 의 사도 칸이 빠졌다 — " + string.Join(" / ", errs));
            t.Potential.First(n => n.Id == "03-1").After = "07";
            Assert.IsTrue(Prog.Check(d, t).Any(e => e.Contains("after")));
            var bad = T("{c:{'03-1':{name:'x', desc:'x', cards:[{card:'없는카드', up:0.1}]}}}");
            Assert.IsTrue(Prog.Check(d, bad).Any(e => e.Contains("없는카드")));
            // 여는 차례 — 앞 칸(after, 없으면 표에서 바로 앞 칸)이 1 이상이어야(진급 문턱은 없앴다)
            Assert.IsEmpty(Prog.CheckState(d, T(), One("c", h => { h.Pot["01"] = 1; h.Pot["03"] = 2; h.Pot["04"] = 1; })));
            StringAssert.Contains("앞 칸 03", string.Join("\n", Prog.CheckState(d, T(), One("c", h => h.Pot["04"] = 1))));
            StringAssert.Contains("0~10", string.Join("\n", Prog.CheckState(d, T(), One("c", h => { h.Pot["01"] = 1; h.Pot["03"] = 11; }))));
        }
    }
}
