using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 이드 칼리페식 손질(2026-10-13 사용자) — 「나타로 들이받기」 X 코스트(perX) · 「꿈속의 나타」 손에 경계가 있으면 비용 +1(costIf) ·
    /// 패시브 「꿈에서 깨지 않았다」(꿈의 막 3겹이면 모두 써서 적 전체 방어 기반 300%)가 실제로 터지는지.
    /// </summary>
    public class IdeKalipeTests
    {
        const string H = "이드", K = "이드 더 이터널 불릿", RAM = H + "_u4", NATA = H + "_u5", BORDER = H + "_u1";

        static GameData Data()
        {
            string dir = new[] { "../bolzena-content-v2", "../../bolzena-content-v2", "C:/projects/bolzena-content-v2" }.FirstOrDefault(Directory.Exists);
            if (dir == null) Assert.Ignore("bolzena-content-v2 가 없다");
            return GameData.FromFolder(dir);
        }

        static Battle Start(GameData d, params string[] deck) => Battle.Start(d, new BattleSetup
        {
            Party = new List<string> { H, "클로에" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = deck.ToList(), Seed = 7, EnemyHp = 50,
        });

        static int Idx(Battle b, string id) => b.Hand.FindIndex(x => GameData.BaseId(x) == id);

        [Test] public void 들이받기_X_만큼_실드()
        {
            var d = Data();
            Assert.IsTrue(d.Card(RAM).X, "들이받기는 X 코스트");
            int Gain(int ap)
            {
                var b = Start(d, RAM, "클로에_s1", "클로에_s1");
                b.Ap = ap;
                int g0 = b.Pool.Shield + b.Pool.Block, hi = Idx(b, RAM);
                Assert.GreaterOrEqual(hi, 0, "손에 들이받기가 없다");
                var r = b.PlayCard(hi, 0);
                Assert.IsTrue(r.Ok, r.Why);
                Assert.AreEqual(0, b.Ap, "X 는 AP 를 모두 쓴다");
                return b.Pool.Shield + b.Pool.Block - g0;
            }
            int g1 = Gain(1), g3 = Gain(3);
            Assert.Greater(g1, 0);
            Assert.AreEqual(g1 * 3, g3, 3, $"X=1 {g1} · X=3 {g3}");
            StringAssert.Contains("X당", new CardText(d).Card(d.View(RAM)));
        }

        [Test] public void 꿈속의_나타_손에_경계면_비용_올라_천상을_깨운다()
        {
            var d = Data();
            var b = Start(d, NATA, BORDER, "클로에_s1");
            int i = Idx(b, NATA);
            Assert.AreEqual(2, b.CostOf(b.Hand[i], i), "손에 경계가 있으면 비용 2");
            Assert.IsNotNull(b.CostUpWhy(b.Hand[i]));
            b.Ap = 1;
            Assert.IsNotNull(b.CanPlay(b.Hand[i], false, i), "AP 1 로는 못 낸다");
            b.Ap = 3;
            var r = b.PlayCard(i, 0);
            Assert.IsTrue(r.Ok, r.Why);
            Assert.AreEqual(1, b.Ap, "오른 비용 2 를 치른다");
            Assert.Less(Idx(b, BORDER), 0, "오른 비용 2 로 손의 「흐릿한 경계」(천상)가 저절로 나갔다");

            var b2 = Start(d, NATA, "클로에_s1", "클로에_s1");
            int j = Idx(b2, NATA);
            Assert.AreEqual(1, b2.CostOf(b2.Hand[j], j), "경계가 없으면 비용 1");
            Assert.IsNull(b2.CostUpWhy(b2.Hand[j]));
            StringAssert.Contains("비용 +1", new CardText(d).Card(d.View(NATA)));
        }

        [Test] public void 꿈의_막_3겹이면_패시브가_터진다()
        {
            var d = Data();
            int Lost(int pre)
            {
                var b = Start(d, BORDER, "클로에_s1", "클로에_s1");
                Assert.AreEqual(1, b.StackOf(H, K), "영원살이 막내 — 전투 시작 +1");
                b.Stacks[H][K] = pre;
                var e = b.Enemies[0];
                int hp0 = e.Hp;
                b.Ap = 3;
                var r = b.PlayCard(Idx(b, BORDER), 0);
                Assert.IsTrue(r.Ok, r.Why);
                if (pre == 2)
                {
                    Assert.AreEqual(0, b.StackOf(H, K), "3겹이 되면 모두 쓴다");
                    Assert.IsTrue(b.Log.Any(l => l.Contains("꿈에서 깨지 않았다")), string.Join("\n", b.Log));
                }
                else Assert.AreEqual(pre + 1, b.StackOf(H, K));
                return hp0 - e.Hp;
            }
            int plain = Lost(0), burst = Lost(2);
            Assert.Greater(burst, plain * 2, $"경계만 {plain} · 패시브까지 {burst}");
        }
    }
}
