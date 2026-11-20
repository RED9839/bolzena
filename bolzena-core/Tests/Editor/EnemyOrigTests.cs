using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>2026-10-31 원작 대조 — 광폭화(R.RageX) · 편성 배율(R.PackX).</summary>
    public class EnemyOrigTests
    {
        [Test] public void 광폭화는_정한_턴이_지나면_적_피해를_겹쳐_올린다()
        {
            Assert.AreEqual(1, R.RageX(5, 5), "5턴까지는 그대로");
            Assert.AreEqual(1.1, R.RageX(6, 5), 1e-12, "지난 첫 턴 ×1.1");
            Assert.AreEqual(1.1 * 1.25 * 1.25, R.RageX(8, 5), 1e-12, "그 뒤 턴마다 ×1.25");
            Assert.AreEqual(1, R.RageX(99, 0), "0 이면 꺼짐");
            Assert.AreEqual(R.RAGE_MAX, R.RageX(500, 5), "상한");
            Assert.AreEqual((10, 7, 5), (R.RageTurnOf(true, false), R.RageTurnOf(false, true), R.RageTurnOf(false, false)));

            // 때리는 놈(공격 100) — 광폭화 2턴짜리 싸움: 2턴까지 100, 3턴 110, 4턴 137
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "hitter" }, st => { st.Rage = 2; st.EnemyHp = 1000; });
            var e = K.E(b);
            var seen = new List<int>();
            for (int t = 0; t < 4; t++) { seen.Add(b.IntentHit(e) ?? -1); b.EndTurn(); }
            CollectionAssert.AreEqual(new[] { 100, 100, 110, Num.Round(100 * 1.1 * 1.25) }, seen, "머리 위 예고 피해에 든다");
            Assert.AreEqual(2, Battle.Load(K.Data(), b.Save()).RageTurn, "저장 왕복");

            var off = K.Fight(K.Data(), new[] { "a" }, new[] { "hitter" }, st => st.EnemyHp = 1000);
            for (int t = 0; t < 4; t++) off.EndTurn();
            Assert.AreEqual(100, off.IntentHit(K.E(off)), "판 밖 싸움(Rage 없음)은 광폭화 없음 — 프론티어 · 시험");
        }

        [Test] public void 편성_배율은_처음_선_적의_체력과_피해에만()
        {
            Assert.IsNull(R.PackX(1, true), "정예 혼자는 1 — 칸을 적지 않는다");
            Assert.AreEqual(0.8, R.PackX(2, true).Value, 1e-12);
            Assert.AreEqual(R.PACK_FIGHT, R.PackX(1, false).Value, 1e-12);
            Assert.AreEqual(0.43 * R.PACK_FIGHT, R.PackX(5, false).Value, 1e-12);
            Assert.IsNull(R.PackX(6, false), "다섯 넘게는 없다");

            var d = K.Data(enemies: "[{id:'caller', name:'부르는 놈', hp:1000, intents:[{t:'summon', id:'hitter', n:1, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "caller", "hitter" }, st => st.FoePack = 0.5);
            Assert.AreEqual(500, K.E(b, 1).MaxHp, "체력 × 배율");
            Assert.AreEqual(50, b.IntentHit(K.E(b, 1)), "피해 × 배율");
            b.EndTurn();
            var called = b.Enemies.Last();
            Assert.AreEqual(3, b.Enemies.Count, "소환");
            Assert.AreEqual(1000, called.MaxHp, "소환물은 배율 밖");
            Assert.AreEqual(500, Battle.Load(d, b.Save()).Enemies[1].MaxHp, "저장 왕복");
        }

        [Test] public void 셋업_JSON_은_새_칸이_비면_옛_꼴_그대로()
        {
            // 프론티어 검증은 셋업 JSON 을 통째로 견준다 — Rage · FoePack 이 null 이면 칸이 없어야 옛 기록과 맞는다
            var j = BattleInput.Of(new BattleSetup { Party = new List<string> { "a" }, Enemies = new List<string> { "dummy" } }).SetupJson.ToString();
            StringAssert.DoesNotContain("\"rage\"", j); StringAssert.DoesNotContain("\"foePack\"", j);
        }
    }
}
