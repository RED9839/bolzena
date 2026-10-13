using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>시온 「마탄」 생성 점검(2026-10-12 사용자 「원 플러스 원의 마탄 생성이 안 되는 것도 있는 듯」) — 카드 · 신탁마다 내 보고 생긴 장수를 센다.</summary>
    public class SionMakeTests
    {
        const string H = "시온더다크불릿", B = H + "_t1";

        static GameData Data()
        {
            string dir = new[] { "../bolzena-content-v2", "../../bolzena-content-v2", "C:/projects/bolzena-content-v2" }.FirstOrDefault(Directory.Exists);
            if (dir == null) Assert.Ignore("bolzena-content-v2 가 없다");
            return GameData.FromFolder(dir);
        }

        static int Mag(Battle b) => b.Hand.Count(x => GameData.BaseId(x) == B) + b.Discard.Count(x => GameData.BaseId(x) == B) + b.Draw.Count(x => GameData.BaseId(x) == B);

        static Battle Start(GameData d, string card, int flash, List<string> extra = null)
        {
            var deck = new List<string> { card, "클로에_s1", "클로에_s1", "시온더다크불릿_s1" };
            if (extra != null) deck.AddRange(extra);
            return Battle.Start(d, new BattleSetup
            {
                Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = deck,
                Flash = flash > 0 ? new Dictionary<string, int> { [card] = flash } : null, Seed = 7, EnemyHp = 50,
            });
        }

        [Test] public void 시온_카드마다_마탄_장수()
        {
            var d = Data();
            var lines = new List<string>();
            var bad = new List<string>();
            foreach (var id in new[] { "_u1", "_u2", "_u3", "_u4", "_u5" }.Select(x => H + x))
            {
                var def = d.Card(id);
                for (int n = 0; n <= def.Oracles.Count; n++)
                {
                    var b = Start(d, id, n);
                    int hi = b.Hand.FindIndex(x => GameData.BaseId(x) == id);
                    if (hi < 0) { lines.Add($"{def.Name} #{n}: 손에 없음"); continue; }
                    var v = b.CardOf(b.Hand[hi]);
                    int expect = v.Fx.TakeWhile(f => f.K != FxK.When && f.K != FxK.IfNth && f.K != FxK.Power).Where(f => f.K == FxK.Make && f.Id == B).Sum(f => System.Math.Max(1, (int)f.V));
                    int before = Mag(b);
                    var r = b.PlayCard(hi, 0);
                    int got = Mag(b) - before;
                    string line = $"{def.Name} #{n}: 낸다 {(r.Ok ? "ok" : "실패 " + r.Why)} · 마탄 +{got} (카드 글 {expect})";
                    lines.Add(line);
                    if (r.Ok && got < expect) bad.Add(line);
                }
            }
            UnityEngine.Debug.Log("[SION]\n" + string.Join("\n", lines));
            Assert.IsEmpty(bad, string.Join("\n", bad) + "\n---\n" + string.Join("\n", lines));
        }

        [Test] public void 시온_패시브_고학년_마탄()
        {
            var d = Data();
            var notes = new List<string>();
            // 고학년 — 마탄 3
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero(H).Starter.ToList(), Gauge = 300, Seed = 7, EnemyHp = 50 });
            int m0 = Mag(b);
            Assert.IsTrue(b.UseUlt(H).Ok);
            notes.Add($"고학년: +{Mag(b) - m0} (3)");
            // 편의점 야간 알바 — AP 를 남기고 턴을 마치면 1
            b = Start(d, H + "_s1", 0);
            m0 = Mag(b);
            b.EndTurn();
            notes.Add($"AP 남기고 턴 끝: +{Mag(b) - m0} (1)");
            // 원 플러스 원 신탁 ③(강화 — 매 턴 시작 마탄 1): 낸 뒤 턴을 넘기면 알바(AP 남김) 1 + 강화 1
            b = Start(d, H + "_u1", 3);
            int hi = b.Hand.FindIndex(x => GameData.BaseId(x) == H + "_u1");
            b.PlayCard(hi, 0);
            m0 = Mag(b);
            b.EndTurn();
            notes.Add($"원플원 ③ 강화 다음 턴: +{Mag(b) - m0} (알바 1 + 강화 1 = 2)");
            // 턴 끝에 만든 마탄은 버려지지 않고 다음 턴 손에 남는다(2026-10-12 — 전에는 손패와 함께 버려졌다)
            // 2026-10-13 리뉴얼: 대물 ③(턴 끝 마탄)은 무덤 정리 신탁으로 바뀌어 — 알바(AP 를 남기고 턴 끝 마탄 1)로 잰다 · 보존은 사수
            b = Start(d, H + "_u4", 0);
            m0 = Mag(b);
            b.EndTurn();
            notes.Add($"사수 쥐고 AP 남기고 턴 끝: +{Mag(b) - m0} (알바 1)");
            Assert.GreaterOrEqual(b.Hand.Count(x => GameData.BaseId(x) == B), 1, "턴 끝에 만든 마탄이 다음 턴 손에");
            Assert.IsTrue(b.Hand.Any(x => GameData.BaseId(x) == H + "_u4"), "보존 카드(마.탄.의.사.수)도 손에");
            // 패시브 「어.둠.의.구.원.자★」 — 다른 아군이 버리게 하면 1(한 턴 한 번)
            b = Start(d, "클로에_u2", 0, new List<string> { H + "_s3" });
            hi = b.Hand.FindIndex(x => GameData.BaseId(x) == "클로에_u2");
            m0 = Mag(b);
            var r = b.PlayCard(hi, 0, new PlayOpts { Discard = b.Hand.Where(x => GameData.BaseId(x) != "클로에_u2").Take(1).ToList() });
            notes.Add($"클로에가 버리게 함: {(r.Ok ? "ok" : r.Why)} +{Mag(b) - m0} (1)");
            UnityEngine.Debug.Log("[SION] " + string.Join(" · ", notes));
            Assert.Pass(string.Join(" · ", notes));
        }

        /// <summary>(2026-10-13 사용자) 마탄 「손에 9장까지 · 넘치면 바로 쏜다」 를 없앴다 — 손 한도는 다른 카드와 같고, 넘쳐도 쏘지 않는다.
        /// 고학년(레노아 「검은 가시」)은 뽑을 · 버린 더미의 마탄을 모두 손으로.</summary>
        [Test] public void 시온_마탄_손_한도_없음_고학년_모두_손으로()
        {
            var d = Data();
            Assert.AreEqual(0, d.Card(B).HandMax, "마탄 handMax 없음");
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero(H).Starter.ToList(), Gauge = 300, Seed = 7, EnemyHp = 50 });
            b.Hand.Clear(); b.Draw.Clear(); b.Discard.Clear();
            for (int k = 0; k < 4; k++) b.Draw.Add(B + GameData.PLAIN);
            for (int k = 0; k < 5; k++) b.Discard.Add(B + GameData.PLAIN);
            Assert.IsTrue(b.UseUlt(H).Ok);
            Assert.AreEqual(9, b.Hand.Count(x => GameData.BaseId(x) == B), "두 더미의 마탄 9장 모두 손으로");
            Assert.IsFalse(b.Log.Any(l => l.Contains("바로 쏜다")), "넘쳐도 바로 쏘지 않는다");
        }

        /// <summary>(2026-10-13 사용자 — 카제나식 탄환) 손에 남은 탄환은 턴 끝에 버려지며 쏘고, 내면(사용) 소멸한다(「소멸」 태그 없이).</summary>
        [Test] public void 탄환_턴끝_발사_사용하면_소멸()
        {
            var d = Data();
            Assert.IsFalse(d.Card(B).Tags.Contains("소멸"), "마탄 태그는 탄환만");
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero(H).Starter.ToList(), Seed = 7, EnemyHp = 50 });
            b.Hand.Clear();
            b.Hand.Add(B + GameData.PLAIN); b.Hand.Add(B + GameData.PLAIN);
            int n0 = b.Log.Count(l => l.Contains("탄환!"));
            var r = b.PlayCard(0, 0);
            Assert.IsTrue(r.Ok, r.Why);
            Assert.IsTrue(b.Gone.Any(x => GameData.BaseId(x) == B) || b.Log.Any(l => l.Contains("마탄") && l.Contains("소멸")), "낸 마탄은 소멸");
            Assert.IsFalse(b.Discard.Any(x => GameData.BaseId(x) == B), "낸 마탄은 버린 더미로 가지 않는다");
            b.EndTurn();
            Assert.Greater(b.Log.Count(l => l.Contains("탄환!")), n0, "턴 끝에 손에 남은 마탄이 버려지며 쏜다");
        }
    }
}
