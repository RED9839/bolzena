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

        /// <summary>(2026-10-13 사용자 — 카제나식 탄환) 손에 남은 탄환은 턴 끝에 버려지며 쏘고, 내면(사용) 소멸한다.
        /// 10-13 저녁 사용자 — 마탄에 「소멸」 태그를 붙였다(카드에 보이게 · 동작은 탄환 규칙과 같음).</summary>
        [Test] public void 탄환_턴끝_발사_사용하면_소멸()
        {
            var d = Data();
            CollectionAssert.AreEquivalent(new[] { "탄환", "소멸" }, d.Card(B).Tags, "마탄 태그는 탄환 · 소멸");
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

        /// <summary>(2026-10-13 밤 「버려서 쏘기」) 「탄창」 — 마탄이 탄환으로 쏠 때마다 +1(전투 동안 · 턴 끝에도 안 사라짐) · 전투 시작 마탄 2장(startMake).</summary>
        [Test] public void 탄창_탄환으로_쏠_때마다_쌓이고_전투_시작_마탄_2장()
        {
            var d = Data();
            const string T = "탄창";
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = new List<string> { H + "_u3", "클로에_s1", "클로에_s1", "클로에_s1", "클로에_s1", "클로에_s1" }, Seed = 7, EnemyHp = 5000 });
            int mags = b.Hand.Count(x => GameData.BaseId(x) == B);
            Assert.GreaterOrEqual(mags, 2, "전투 시작 마탄 2장");
            Assert.AreEqual(0, b.StackOf(H, T));
            b.Ap = 9;
            Assert.IsTrue(b.PlayCard(b.Hand.FindIndex(x => GameData.BaseId(x) == H + "_u3"), 0).Ok);   // 대물 저격총 — 손의 마탄 1장 버림(탄환)
            Assert.AreEqual(1, b.StackOf(H, T), "버려져 쏘면 +1");
            double m = b.StatMod(b.HeroUnit(H), "dealt");
            int left = b.Hand.Count(x => GameData.BaseId(x) == B);
            b.EndTurn();
            Assert.AreEqual(1 + left, b.StackOf(H, T), "턴 끝에 버려져 쏜 마탄도 +1 · 턴이 끝나도 남는다");
            StringAssert.Contains("버려져 발사될 때마다", new CardText(d).Trait(d.Hero(H), d.Hero(H).AllKeywords.First(k => k.Name == T)));
        }

        /// <summary>(2026-10-13 밤) 턴 끝 탄환은 장전(이번 턴 endClear)이 사라지기 전에 쏜다 — 장전이 있으면 턴 끝 탄환 피해가 커진다.</summary>
        [Test] public void 턴끝_탄환은_장전이_사라지기_전에_쏜다()
        {
            var d = Data();
            const string K = "이 몸, 등장★";
            int Shot(int load)
            {
                var b = Battle.Start(d, new BattleSetup { Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero(H).Starter.ToList(), Seed = 7, EnemyHp = 5000 });
                b.Hand.Clear(); b.Hand.Add(B + GameData.PLAIN);
                if (load > 0) b.Stacks[H] = new Dictionary<string, int> { [K] = load };
                int d0 = b.Tally.TryGetValue(H, out var t0) ? t0.Dealt : 0;
                b.EndTurn();
                Assert.IsTrue(b.Log.Any(l => l.Contains("탄환!")), "턴 끝 탄환");
                Assert.AreEqual(0, b.StackOf(H, K), "장전은 턴 끝에 사라진다");
                return (b.Tally.TryGetValue(H, out var t1) ? t1.Dealt : 0) - d0;   // 로그의 수는 증감 전 값이라 실제로 들어간 피해(전투 기록)로 잰다
            }
            Assert.Greater(Shot(5), Shot(0), "턴 끝 탄환에 장전이 붙는다");
        }

        /// <summary>(2026-10-13 밤) 탄환 배율은 카드마다(bulletRatio) — 시온 마탄만 키우고 교주 카드 탄환(랜덤 코인)은 R.SV("탄환") 그대로.</summary>
        [Test] public void 탄환_배율은_카드마다()
        {
            var d = Data();
            Assert.Greater(d.Card(B).BulletRatio, R.SV("탄환"));
            StringAssert.Contains($"추가 공격 {Num.Round(d.Card(B).BulletRatio * 100)}%", new CardText(d).Card(d.Card(B)));
            if (d.Card("n_coin") is CardDef coin) { Assert.AreEqual(0, coin.BulletRatio); StringAssert.Contains($"추가 공격 {Num.Round(R.SV("탄환") * 100)}%", new CardText(d).Card(coin)); }
        }

        /// <summary>(2026-10-13 밤 사용자) 마탄을 쏠 때마다 「이 몸, 등장★」 +1 — 마탄 fx 에서 고유 효과 tally(tallyTag)로 옮겼다. 효과로 버려짐(탄환 발사) · 소각은 +1, 직접 내기는 옛 fx 그대로 +2(내기 +1 · 제 소멸 +1).</summary>
        [Test] public void 마탄_쏠_때마다_장전_1()
        {
            var d = Data();
            const string K = "이 몸, 등장★";
            Battle Fresh(string card, int flash)
            {
                var b = Start(d, card, flash);
                b.Hand.RemoveAll(x => GameData.BaseId(x) == B); b.Discard.Clear();
                b.Ap = 9;
                return b;
            }
            // 직접 내기 — 마탄은 소멸(exhaust)
            var b1 = Fresh(H + "_u1", 0);
            b1.Hand.Insert(0, B + GameData.PLAIN);
            int k0 = b1.StackOf(H, K);
            Assert.IsTrue(b1.PlayCard(0, 0).Ok);
            Assert.AreEqual(k0 + 2, b1.StackOf(H, K), "마탄을 내면 +2(내기 · 제 소멸)");
            // 효과로 버려짐 — 대물 저격총(손의 마탄 1장 버림 · 탄환 발사)
            var b2 = Fresh(H + "_u3", 0);
            b2.Hand.Add(B + GameData.PLAIN);
            int k1 = b2.StackOf(H, K), shots = b2.Log.Count(l => l.Contains("탄환!"));
            Assert.IsTrue(b2.PlayCard(b2.Hand.FindIndex(x => GameData.BaseId(x) == H + "_u3"), 0).Ok);
            Assert.Greater(b2.Log.Count(l => l.Contains("탄환!")), shots, "버린 마탄이 쏜다");
            Assert.AreEqual(k1 + 1, b2.StackOf(H, K), "버려지면 +1");
            // 소각 — 대물 저격총 신탁 3(버린 더미의 마탄 1장 소멸)
            var b3 = Fresh(H + "_u3", 3);
            b3.Discard.Add(B + GameData.PLAIN);
            int k2 = b3.StackOf(H, K);
            Assert.IsTrue(b3.PlayCard(b3.Hand.FindIndex(x => GameData.BaseId(x) == H + "_u3"), 0).Ok);
            Assert.AreEqual(k2 + 1, b3.StackOf(H, K), "소각되면 +1");
            // 마탄 글 — 장전 줄이 빠지고 55자 안
            var t = new CardText(d).Card(d.Card(B));
            StringAssert.DoesNotContain(K, t);
            Assert.LessOrEqual(t.Count(ch => !char.IsWhiteSpace(ch)), 55, t);
        }

        /// <summary>(2026-10-13 사용자) 탄환 카드는 본문에 발사 줄이 보인다 — 칩이 없는 사도 상세 · 카드 도감에서도.</summary>
        [Test] public void 탄환_카드_본문에_발사_줄()
        {
            var d = Data();
            StringAssert.Contains("버린 더미로 가면 무작위 적에게 추가 공격", new CardText(d).Card(d.Card(B)));
        }
    }
}
