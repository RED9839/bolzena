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
            // 대물 저격총 신탁 ③(보존 · 턴 끝에 손에 있으면 마탄 1)
            b = Start(d, H + "_u3", 3);
            m0 = Mag(b);
            b.EndTurn();
            notes.Add($"대물 ③ 손에 쥐고 턴 끝: +{Mag(b) - m0} (알바 1 + 보존 1 = 2)");
            // 같은 것 — AP 를 다 쓰고(알바 없음) 턴 끝
            b = Start(d, H + "_u3", 3);
            for (int k = 0; k < 6 && b.Ap > 0; k++) { int j = b.Hand.FindIndex(x => GameData.BaseId(x) != H + "_u3" && b.CanPlay(x) == null); if (j < 0) break; b.PlayCard(j, 0); }
            m0 = Mag(b);
            bool held = b.Hand.Any(x => GameData.BaseId(x) == H + "_u3");
            b.EndTurn();
            notes.Add($"대물 ③ AP 0 · 손에 {(held ? "있음" : "없음")} 턴 끝: +{Mag(b) - m0} (보존 1)");
            // 턴 끝에 만든 마탄은 버려지지 않고 다음 턴 손에 남는다(2026-10-12 — 전에는 손패와 함께 버려졌다)
            Assert.GreaterOrEqual(b.Hand.Count(x => GameData.BaseId(x) == B), 1, "턴 끝에 만든 마탄이 다음 턴 손에");
            Assert.IsTrue(b.Hand.Any(x => GameData.BaseId(x) == H + "_u3"), "보존 카드도 손에");
            // 패시브 「어.둠.의.구.원.자★」 — 다른 아군이 버리게 하면 1(한 턴 한 번)
            b = Start(d, "클로에_u2", 0, new List<string> { H + "_s3" });
            hi = b.Hand.FindIndex(x => GameData.BaseId(x) == "클로에_u2");
            m0 = Mag(b);
            var r = b.PlayCard(hi, 0, new PlayOpts { Discard = b.Hand.Where(x => GameData.BaseId(x) != "클로에_u2").Take(1).ToList() });
            notes.Add($"클로에가 버리게 함: {(r.Ok ? "ok" : r.Why)} +{Mag(b) - m0} (1)");
            UnityEngine.Debug.Log("[SION] " + string.Join(" · ", notes));
            Assert.Pass(string.Join(" · ", notes));
        }
    }
}
