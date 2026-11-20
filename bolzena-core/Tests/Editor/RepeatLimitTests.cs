using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>2026-11-01 — 같은 적 편성 반복 줄이기(지도) · 장비 효과의 원작식 횟수 제한.</summary>
    public class RepeatLimitTests
    {
        [Test] public void 지도의_싸움은_두_줄_위까지_올_수_있는_칸의_편성과_겹치지_않는다()
        {
            var d = K.Sample();
            // 편성 열 가지 — 두 줄 위까지 앞 칸은 많아야 일곱(바로 위 셋 + 그 위 넷)이라 늘 겹치지 않는 것이 남는다
            var groups = Enumerable.Range(0, 10).Select(i => new List<string> { "f" + i }).ToList();
            foreach (var f in d.Villages["erpien"].Floors)
            {
                f.Pools = Enumerable.Range(0, 3).Select(_ => groups.Select(g => g.ToList()).ToList()).ToList();
                f.Elites = groups.Select(g => g.ToList()).ToList();
            }
            int checkedN = 0;
            for (long seed = 1; seed <= 40; seed++)
                for (int floor = 0; floor < 2; floor++)
                {
                    var m = Run.GenMap(d, seed, floor, "erpien");
                    for (int r = 1; r < m.Rows.Count; r++)
                        foreach (var n in m.Rows[r].Where(x => x.Foes != null))
                        {
                            var up1 = m.Rows[r - 1].Where(p => p.Next.Contains(n.Id)).ToList();
                            var up2 = r > 1 ? m.Rows[r - 2].Where(p => up1.Any(q => p.Next.Contains(q.Id))).ToList() : new List<MapNode>();
                            foreach (var p in up1.Concat(up2).Where(p => p.Foes != null))
                            {
                                Assert.IsFalse(p.Foes.SequenceEqual(n.Foes), $"씨앗 {seed} · {floor + 1}층 {n.Id} 이 앞 칸 {p.Id} 와 같은 편성 {string.Join(",", n.Foes)}");
                                checkedN++;
                            }
                        }
                }
            Assert.Greater(checkedN, 100, "두 줄 위 칸까지 견준 수");
        }

        [Test] public void 장비_효과의_턴당_전투당_횟수_제한과_글()
        {
            var d = K.Data(equips: @"[{id:'eq', name:'시험 펜던트', grade:'희귀', slot:'장신구', effect:[
 {name:'턴마다 한 번', when:{on:'play', type:'공격'}, limit:{per:'turn', n:1}, fx:[{k:'gauge', v:1}]},
 {name:'판에 한 번', when:{on:'play', type:'공격'}, limit:{per:'fight', n:1}, fx:[{k:'gauge', v:10}]}]}]");
            // 카드 자체로 차는 게이지는 장비 없는 싸움과의 차이로 지운다
            List<int> Gauges(bool gear)
            {
                var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => { if (gear) st.GearRules = new Dictionary<string, List<PassiveRule>> { ["a"] = d.Equips["eq"].Effect.ToList() }; });
                var g = new List<int>();
                for (int t = 0; t < 2; t++) { K.Hand(b, "hit", "hit"); K.Play(b, "hit", 0); K.Play(b, "hit", 0); g.Add(b.Gauge); b.EndTurn(); }
                return g;
            }
            var on = Gauges(true); var off = Gauges(false);
            Assert.AreEqual(1 + 10, on[0] - off[0], "첫 턴 — 둘 다 한 번씩만");
            Assert.AreEqual(1 + 10 + 1, on[1] - off[1], "다음 턴 — 턴당은 다시, 전투당은 끝");

            var s = new CardText(d).Equip(d.Equips["eq"]);
            StringAssert.Contains("(턴당 1회)", s);
            StringAssert.Contains("(전투당 1회)", s);
        }
    }
}
