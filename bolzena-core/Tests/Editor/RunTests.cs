using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>판 — 지도 · 싸움 열기/닫기 · 보상 · 신탁 · 캠프 · 상점 · 장비 · 이벤트 · 저장.</summary>
    public class RunTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        static Run New(long seed = 1) => Run.New(K.Sample(), PARTY, seed);

        [Test] public void 새_판()
        {
            var run = New();
            Assert.AreEqual("erpien", run.S.Village);
            Assert.AreEqual(900 + 600 + 550, run.S.PartyMaxHp);
            Assert.AreEqual(12, run.S.Deck.Count);
            Assert.AreEqual(R.GOLD_START, run.S.Gold);
        }

        [Test] public void 지도는_열_칸_보스_앞은_휴식_상점_같은_씨앗이면_같은_길()
        {
            var d = K.Sample();
            for (long seed = 1; seed <= 20; seed++)
                for (int floor = 0; floor < 2; floor++)
                {
                    var m = Run.GenMap(d, seed, floor, "erpien");
                    Assert.AreEqual(Run.MAP_ROWS + 1, m.Rows.Count);
                    Assert.AreEqual("start", m.Rows[0][0].Type);
                    Assert.AreEqual("boss", m.Rows[10][0].Type);
                    Assert.AreEqual("campshop", m.Rows[9][0].Type);
                    Assert.IsTrue(m.Rows[1].All(n => n.Type == "fight"));
                    Assert.That(m.Rows[1].Count, Is.InRange(2, 4));
                    for (int r = 2; r <= 8; r++) Assert.IsTrue(m.Rows[r].Any(n => n.Type == "fight" || n.Type == "elite"));
                    Assert.IsFalse(m.Rows[2].Concat(m.Rows[3]).Any(n => n.Type == "elite" || n.Type == "camp"), "엘리트 · 휴식은 1-4 부터");
                    // 모든 칸이 보스에 닿는다
                    var reach = new HashSet<string> { "r10c0" };
                    for (int r = 9; r >= 0; r--) foreach (var n in m.Rows[r]) if (n.Next.Any(reach.Contains)) reach.Add(n.Id);
                    Assert.IsTrue(m.Rows.SelectMany(x => x).All(n => reach.Contains(n.Id)));
                    foreach (var n in m.Rows.SelectMany(x => x).Where(n => n.Type == "fight" || n.Type == "elite")) Assert.IsNotNull(n.Foes);
                    Assert.AreEqual(GameData.ToJson(m), GameData.ToJson(Run.GenMap(d, seed, floor, "erpien")));
                }
        }

        [Test] public void 칸에_들면_싸움을_열고_닫는다()
        {
            var run = New(3);
            var id = run.Reachable()[0];
            var node = run.EnterNode(id);
            Assert.AreEqual("fight", node.Type);
            var (b, loot) = run.OpenFight();
            CollectionAssert.AreEqual(node.Foes, b.Enemies.Select(e => e.Key).ToList());
            Assert.AreEqual(Num.Round(run.Data.Enemy(b.Enemies[0].Key).Hp * R.FoeScale(0).hp), b.Enemies[0].MaxHp);
            Assert.AreEqual(R.FoeScale(0).dmg, b.Enemies[0].Dmgx);
            Assert.IsNotNull(loot);
            Assert.That(loot.Gold, Is.InRange(20, 30));
            Assert.IsTrue(b.Glow.Values.Any(g => g.Kind == "hero"), "일반 싸움도 은총 하나는 반드시");
            var bots = new Bots(run.Data);
            while (b.Over == null) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
            run.AfterFight(b);
            Assert.AreEqual(b.Pool.Hp, run.S.PartyHp);
            Assert.AreEqual(1, run.S.Hist.Count);
            int g0 = run.S.Gold; run.TakeGold(); run.TakeGold();
            Assert.AreEqual(g0 + loot.Gold, run.S.Gold, "골드는 한 번만");
        }

        [Test] public void 은총으로_얻은_카드는_판의_덱에_남는다()
        {
            var run = New(4);
            run.EnterNode(run.Reachable()[0]);
            var (b, _) = run.OpenFight();
            var (cardId, g) = b.Glow.First(kv => kv.Value.Kind == "hero");
            b.Hand.Add(cardId);
            b.ApplyEpiphany(cardId, 0);
            run.AfterFight(b);
            CollectionAssert.Contains(run.S.Deck, g.Options[0]);
        }

        [Test] public void 캠프는_쉬기_또는_수련_하나()
        {
            var run = New();
            run.S.PartyHp = 1000;
            run.EnterCamp("camp");
            Assert.IsNull(run.CampRest());
            Assert.AreEqual(1000 + Num.Round(2050 * R.CAMP_HEAL), run.S.PartyHp);
            Assert.IsNotNull(run.CampRest());
            run.S.Deck.Add("sion_u2");
            run.S.Map = null; run.S.Floor = 1;
            var c = run.EnterCamp("camp");
            Assert.IsNotNull(c.Train);
            Assert.IsNull(run.CampTrain(c.Train.Picks[0]));
            Assert.AreEqual(c.Train.Picks[0], run.S.Flash["sion_u2"]);
        }

        [Test] public void 상점_사기_새로고침_제거()
        {
            var run = New();
            run.S.Gold = 1000;
            var shop = run.RollShop();
            Assert.AreEqual(R.SHOP_NEUTRAL + R.SHOP_EQUIP_N, shop.Items.Count);
            int i = shop.Items.FindIndex(x => x.Kind == "equip");
            Assert.IsNull(run.Buy(i));
            Assert.IsTrue(run.IsBought(shop.Items[i].Id));
            Assert.IsNotNull(run.SellEquip(shop.Items[i].Id), "산 장비는 팔 수 없다");
            Assert.IsNotNull(run.Buy(i), "이미 팔렸다");
            Assert.IsNull(run.RerollShop());
            Assert.AreEqual(R.SHOP_REROLL + R.SHOP_REROLL_STEP, run.RerollPrice);
            int gold = run.S.Gold;
            Assert.IsNull(run.RemoveCard("rico_s1"));
            Assert.AreEqual(gold - R.PRICE_REMOVE, run.S.Gold);
            Assert.AreEqual(11, run.S.Deck.Count);
            Assert.IsNotNull(run.RemoveCard("rico_s1"), "한 번 들를 때 한 번");
            Assert.AreEqual(R.PRICE_REMOVE + R.PRICE_REMOVE_STEP, run.RemovePrice);
        }

        [Test] public void 장비는_끼면_능력치_바꿔_끼면_판다()
        {
            var run = New();
            run.GainEquip("eq_apron");
            Assert.IsNull(run.Equip("rico", "eq_apron"));
            Assert.AreEqual(2050 + 60, run.S.PartyMaxHp);
            run.GainEquip("eq_shell");
            Assert.IsNotNull(run.Equip("rico", "eq_shell"), "찬 칸은 replace 로만");
            int gold = run.S.Gold;
            Assert.IsNull(run.Equip("rico", "eq_shell", replace: true));
            Assert.AreEqual(gold + run.SellPrice("eq_apron"), run.S.Gold);
            Assert.AreEqual(2050 + 160, run.S.PartyMaxHp);
            Assert.AreEqual(15, run.GearStats()["rico"].Def);
            Assert.AreEqual(1, run.GearRules()["rico"].Count);
            run.GainEquip("eq_rifle"); run.Equip("sion", "eq_rifle");
            Assert.AreEqual(28 + 10, run.GearStats()["sion"].Atk, "애착 사도가 끼면 애착 능력치");
            Assert.AreEqual(2, run.GearRules()["sion"].Count, "효과 + 애착 효과");
            var (b, _) = run.OpenFight();
            Assert.AreEqual(140 + 38, b.HeroUnit("sion").Atk);
            Assert.AreEqual(2, b.StackOf("sion", "마탄"), "애착 효과 「장전 완료」 — 전투 시작 시");
        }

        [Test] public void 보스를_넘으면_층이_바뀌고_2층_보스면_완주()
        {
            var run = New();
            run.S.Node = 3;
            run.S.PartyHp = 100;
            run.Advance();
            Assert.AreEqual(1, run.S.Floor); Assert.AreEqual(0, run.S.Node);
            Assert.AreEqual(100 + R.FLOOR_REST * 3, run.S.PartyHp);
            run.S.Node = 3;
            run.Advance();
            Assert.AreEqual("clear", run.S.Done);
        }

        [Test] public void 보스_복제는_가진_고유_카드에서_셋_복제본은_신탁을_옮겨_받는다()
        {
            var run = New();
            run.S.Deck.AddRange(new[] { "rico_u1", "rico_u2", "sion_u2", "rico_u3" });
            run.S.Flash["rico_u1"] = 2;
            var off = run.BossCopyOffer();
            Assert.AreEqual(3, off.Count);
            CollectionAssert.DoesNotContain(off, "rico_u3", "강화 카드(유일)는 복제하지 않는다");
            CollectionAssert.AreEqual(off, run.BossCopyOffer(), "다시 물어도 같은 셋");
            var cid = run.AddCopy("rico_u1");
            Assert.AreEqual("rico_u1^", cid);
            Assert.AreEqual(2, run.S.Flash[cid]);
            Assert.AreEqual(3, run.ViewOf(cid).Cost, "신탁 ②(코스트 3)");
        }

        [Test] public void 유일과_강화_카드는_덱에_한_장()
        {
            var run = New();
            run.S.Deck.Add("rico_u3");
            Assert.IsNotNull(run.PowerWhy("rico_u3"));
            Assert.IsNull(run.PowerWhy("rico_u2"));
            run.S.Deck.Add("n_bond");
            Assert.IsNotNull(run.PowerWhy("n_bond"));
        }

        [Test] public void 이벤트_고르기와_결과()
        {
            var run = New();
            run.S.Event = new EventState { Key = "t", Choices = new List<string> { "C3" }, Id = "C3" };
            var ev = run.Data.Event("C3");
            var opts = run.OptionsOf(ev);
            Assert.AreEqual(4, opts.Count, "선택지 셋 + 떠나기");
            run.S.PartyHp = 1000;
            var (fight, why) = run.Choose(1);
            Assert.IsFalse(fight); Assert.IsNull(why);
            Assert.AreEqual(1000 + Num.Round(2050 * 0.3), run.S.PartyHp);
            Assert.AreEqual(-1, run.S.NextFight.Ap);
            run.LeaveEvent();
            run.EnterNode(run.Reachable()[0]);
            var (b, _) = run.OpenFight();
            Assert.AreEqual(2, b.Ap, "다음 전투: 첫 턴 AP -1");
            Assert.IsNull(run.S.NextFight, "한 번만");
        }

        [Test] public void 이벤트_사도_조건_골드_잠금_판정_고르는_것()
        {
            var run = New();
            var c1 = run.Data.Event("C1");
            Assert.AreEqual(3, run.OptionsOf(c1).Count, "선택지 둘 + 떠나기");
            run.Data.Add(null, null, null, null, "[{\"id\":\"H1\",\"name\":\"사도 선택지\",\"options\":[{\"label\":\"리코타만\",\"hero\":[\"rico\"],\"out\":[{\"k\":\"gold\",\"v\":1}]},{\"label\":\"없는 사도만\",\"hero\":[\"nobody\"],\"out\":[]}]}]", null);
            CollectionAssert.AreEqual(new[] { "리코타만", "떠납니다" }, run.OptionsOf(run.Data.Event("H1")).Select(o => o.Label));
            run.S.Gold = 10;
            Assert.IsNotNull(run.LockOf(c1.Options[0]));
            run.S.Gold = 500;
            run.S.Event = new EventState { Key = "t", Choices = new List<string> { "C1" }, Id = "C1" };
            run.Choose(1);
            Assert.AreEqual("remove", run.S.Event.Pending[0].K);
            Assert.IsNull(run.ResolvePending("carrot_s3"));
            Assert.AreEqual(11, run.S.Deck.Count);
            var c4 = run.Data.Event("C4");
            var j = run.JudgeOf(c4.Options[1]);
            Assert.IsTrue(j.Pass, "시온 공격력 140 ≥ 130");
            Assert.AreEqual("sion", j.Who);
        }

        [Test] public void 이벤트_싸움()
        {
            var run = New(9);
            var E1 = run.Data.Event("E1");
            run.S.Event = new EventState { Key = "t", Choices = new List<string> { "E1" }, Id = "E1" };
            var (fight, _) = run.Choose(0);
            Assert.IsTrue(fight);
            var (b, loot) = run.OpenFight();
            Assert.IsNull(loot, "이벤트 싸움은 적힌 보상만");
            CollectionAssert.AreEqual(new[] { "fairy_close", "fairy_long" }, b.Enemies.Select(e => e.Key));
            var bots = new Bots(run.Data);
            while (b.Over == null) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
            run.AfterFight(b);
            int g = run.S.Gold;
            run.AfterEventFight(b.Over == "win");
            if (b.Over == "win") { Assert.AreEqual(g + 50, run.S.Gold); Assert.AreEqual(1, run.S.Bag.Count); }
        }

        [Test] public void 저장하고_이어하면_같은_판()
        {
            var a = New(11);
            a.EnterNode(a.Reachable()[0]);
            a.S.Gold = 321;
            var json = a.Save();
            var b = Run.Load(a.Data, json);
            Assert.AreEqual(json, b.Save());
            Assert.AreEqual(a.Rng.State, b.Rng.State, "난수도 그 자리에서 이어진다");
            Assert.AreEqual(321, b.S.Gold);
            CollectionAssert.AreEqual(a.Reachable(), b.Reachable());
            CollectionAssert.AreEqual(a.RollReward().Equip ?? new List<string>(), b.RollReward().Equip ?? new List<string>());
        }

        [Test] public void 전투를_저장하고_이어하면_끊김_없이_같다()
        {
            var d = K.Sample();
            BattleSetup Setup() => new BattleSetup { Party = PARTY.ToList(), Deck = d.BuildDeck(PARTY), Enemies = new List<string> { "magicfork", "fairy_long" }, Seed = 77, EnemyHp = 2 };
            var bots = new Bots(d);
            var a = Battle.Start(d, Setup());
            bots.SmartPlay(a); a.EndTurn(); bots.SmartPlay(a);
            var json = a.Save();
            var b = Battle.Load(d, json);
            Assert.AreEqual(json, b.Save());
            for (int t = 0; t < 6; t++)
            {
                if (a.Over != null) break;
                a.EndTurn(); b.EndTurn();
                bots.SmartPlay(a); bots.SmartPlay(b);
                Assert.AreEqual(Canon(a), Canon(b), $"{t} 턴 뒤");
            }
        }

        /// <summary>판의 뜻이 같은지 — 사전 · 집합은 차례가 달라도 같게(지우고 다시 넣으면 차례가 바뀐다).</summary>
        static string Canon(Battle s)
        {
            string D(Dictionary<string, int> d) => string.Join(",", d.OrderBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value));
            string U(Unit u) => $"{u.Key}:{u.Hp}/{u.MaxHp}/{u.Block}/{u.Shield}/{u.Tough}/{u.Broken}/{u.Intent?.T}{u.Intent?.V}/{D(u.Status)}/{u.Mods.Count}";
            return $"{s.Turn}|{s.Ap}|{s.Gauge}|{s.Over}|{U(s.Pool)}|{string.Join(";", s.Party.Select(U))}|{string.Join(";", s.Enemies.Select(U))}|{string.Join(",", s.Hand)}|{string.Join(",", s.Draw)}|{string.Join(",", s.Discard)}|{string.Join(";", s.Stacks.OrderBy(k => k.Key).Select(k => k.Key + ":" + D(k.Value)))}|{s.Rng.State}";
        }
    }
}
