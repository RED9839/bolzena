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

        // ── 한 전투에 사도마다 신탁 · 은총 가운데 하나만(2026-10-06 사용자) ──
        /// <summary>빛나는 카드를 주인 사도별로 센다 — 은총은 Glow.Hero, 카드 신탁은 그 카드의 주인(교주 카드는 주인 사도).</summary>
        static List<string> GlowOwners(Run run, Dictionary<string, Glow> glow) =>
            glow.Select(kv => kv.Value.Kind == "hero" ? kv.Value.Hero : run.ViewOf(kv.Key).Hero ?? "neutral").ToList();

        [Test] public void 한_전투에_사도마다_신탁_은총_가운데_하나만()
        {
            int both = 0, cards = 0, graces = 0;
            foreach (var kind in new[] { "fight", "elite", "boss" })
                for (long seed = 1; seed <= 300; seed++)
                {
                    var run = New(seed);
                    run.S.Deck.AddRange(new[] { "rico_u1", "carrot_u1", "sion_u1" });   // 사도마다 신탁을 받을 고유 카드 · 아직 얻을 고유 카드도 남았다
                    run.S.Elite = kind == "elite"; run.S.Node = kind == "boss" ? 3 : 1;
                    var glow = run.RollEpiphany();
                    var owners = GlowOwners(run, glow);
                    Assert.AreEqual(owners.Count, owners.Distinct().Count(), $"{kind} 씨앗 {seed}: 한 사도에 빛이 둘 — {string.Join(", ", owners)}");
                    int h = glow.Values.Count(g => g.Kind == "hero"), c = glow.Values.Count(g => g.Kind == "card");
                    Assert.GreaterOrEqual(h, 1, $"{kind}: 은총 하나는 반드시");
                    if (kind == "elite") Assert.GreaterOrEqual(c, 1, "엘리트: 빈 사도가 있으면 카드 신탁도 반드시");
                    graces += h; cards += c; if (h > 0 && c > 0) both++;
                }
            Assert.Greater(both, 0, "다른 사도끼리는 은총 · 신탁이 한 전투에 같이 선다");
            Assert.Greater(cards, 0); Assert.Greater(graces, 0);
        }

        [Test] public void 교주_카드는_주인_사도_몫으로_센다()
        {
            int lit = 0;
            for (long seed = 1; seed <= 300; seed++)
            {
                var run = New(seed);
                run.Data.Cards["nz"] = new CardDef
                {
                    Id = "nz", Name = "시험 교주 카드", Type = "공격", Cost = 1,
                    Oracles = Enumerable.Range(1, 5).Select(i => new OracleDef { Name = "시험 신탁 " + i }).ToList(),
                };
                run.S.Deck.Add(GameData.WithOwner("nz", "rico"));   // 신탁을 받을 카드는 리코타 몫의 교주 카드 하나뿐
                run.S.Elite = true;
                var glow = run.RollEpiphany();
                bool rGrace = glow.Values.Any(g => g.Kind == "hero" && g.Hero == "rico");
                bool nCard = glow.TryGetValue(GameData.WithOwner("nz", "rico"), out var ng) && ng.Kind == "card";
                Assert.IsFalse(rGrace && nCard, $"씨앗 {seed}: 리코타 은총과 리코타 몫 교주 카드 신탁이 같이 섰다");
                if (nCard) lit++;
            }
            Assert.Greater(lit, 0, "리코타에게 은총이 없으면 교주 카드 신탁은 선다");
        }

        [Test] public void 다음_전투_신탁_약속은_은총을_바꿔서라도_지킨다()
        {
            for (long seed = 1; seed <= 200; seed++)
            {
                var run = New(seed);
                run.S.Deck.Add("rico_u1");          // 신탁을 받을 카드는 리코타 것 하나뿐
                run.S.RewardFlash = true;           // 이벤트 「다음 전투에서 신탁이 꼭 뜹니다」
                var glow = run.RollEpiphany();
                var owners = GlowOwners(run, glow);
                Assert.AreEqual(owners.Count, owners.Distinct().Count(), $"씨앗 {seed}: 한 사도에 빛이 둘");
                Assert.IsTrue(glow.TryGetValue("rico_u1", out var g) && g.Kind == "card", $"씨앗 {seed}: 약속한 신탁이 없다");
                Assert.IsFalse(run.S.RewardFlash, "약속은 한 번 쓰면 끝");
            }
        }

        // ── 보상 골드는 화면이 열리면 저절로(2026-10-06 사용자) — 두 번 받거나 못 받는 일이 없게 ──
        [Test] public void 보상_골드는_한_번만_저장_불러오기에도()
        {
            var run = New(5);
            run.EnterNode(run.Reachable()[0]);
            var saved = run.Save();                         // 싸움 직전 저장(판 화면의 「fight」)
            var (b, loot) = run.OpenFight();
            var bots = new Bots(run.Data);
            while (b.Over == null) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
            run.AfterFight(b);
            int g0 = run.S.Gold;
            run.TakeGold();                                 // 보상 화면이 열리며 저절로
            Assert.AreEqual(g0 + loot.Gold, run.S.Gold);
            run.TakeGold();                                 // 다시 그려도 · 떠날 때 한 번 더 불러도
            Assert.AreEqual(g0 + loot.Gold, run.S.Gold, "골드는 한 번만");
            // 받은 뒤 저장(장비 고르기 「gear」)하고 불러오면 — 받은 채 · 다시 불러도 그대로
            var back = Run.Load(run.Data, run.Save());
            Assert.IsTrue(back.S.Reward.GoldTaken);
            back.TakeGold();
            Assert.AreEqual(g0 + loot.Gold, back.S.Gold, "불러온 뒤에도 두 번 받지 않는다");
            // 받기 전 저장(싸움 직전)으로 돌아가면 — 골드는 아직 없고, 싸움을 다시 열면 새 보상을 한 번 받는다
            var before = Run.Load(run.Data, saved);
            int s0 = before.S.Gold;
            var (b2, loot2) = before.OpenFight();
            while (b2.Over == null) { bots.SmartPlay(b2); if (b2.Over == null) b2.EndTurn(); }
            before.AfterFight(b2);
            before.TakeGold(); before.TakeGold();
            Assert.AreEqual(s0 + loot2.Gold, before.S.Gold, "끄고 다시 켜도 못 받거나 두 번 받지 않는다");
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

        [Test] public void 보스_복제_후보는_덱의_파티_고유_카드_무작위_셋_같은_카드는_한_번()
        {
            var seen = new HashSet<string>();
            for (long seed = 1; seed <= 60; seed++)
            {
                var run = New(seed);
                run.S.Deck.AddRange(new[] { "rico_u1", "rico_u1", "rico_u2", "sion_u2", "carrot_u2", "rico_u3", "n_bond@rico" });
                run.AddCopy("rico_u2");
                var off = run.BossCopyOffer();
                Assert.AreEqual(3, off.Count, $"씨앗 {seed}");
                Assert.AreEqual(3, off.Distinct().Count(), "같은 카드는 한 번");
                foreach (var id in off)
                {
                    var c = run.Data.Card(id);
                    Assert.IsTrue(c.Unique && c.Hero != null && run.S.Party.Contains(c.Hero), $"{id}: 파티 사도 고유 카드만");
                    Assert.IsFalse(GameData.IsCopy(id), "복제본은 후보가 아니다");
                    Assert.AreNotEqual("rico_u3", id, "유일은 복제하지 않는다");
                    Assert.IsTrue(run.S.Deck.Contains(id), "덱에 든 것만");
                    seen.Add(id);
                }
            }
            CollectionAssert.AreEquivalent(new[] { "rico_u1", "rico_u2", "sion_u2", "carrot_u2" }, seen, "넷 가운데 무작위 셋");

            var one = New();
            one.S.Deck.Add("sion_u2");
            CollectionAssert.AreEqual(new[] { "sion_u2" }, one.BossCopyOffer(), "셋보다 적으면 있는 만큼");
            var none = New();
            none.AddCopy("rico_u1");
            Assert.AreEqual(0, none.BossCopyOffer().Count, "고유 카드가 없으면(복제본뿐) 빈 목록 — 화면이 건너뛴다");
        }

        [Test] public void 복제본은_복제한_순간_모습에_묶이고_원본이_신탁을_받아도_그대로()
        {
            var run = New();
            run.S.Deck.Add("rico_u1");
            run.S.Flash["rico_u1"] = 2; run.S.Shin["rico_u1"] = "own";
            var a = run.AddCopy("rico_u1");
            Assert.AreEqual("rico_u1^", a);
            Assert.AreEqual(2, run.S.Flash[a]); Assert.AreEqual("own", run.S.Shin[a]);
            var ma = run.MarkOf(a); var mo = run.MarkOf("rico_u1");
            Assert.IsTrue(ma.Copy); Assert.IsFalse(mo.Copy);
            Assert.AreEqual(mo.Oracle, ma.Oracle); Assert.AreEqual(mo.Bless, ma.Bless); Assert.IsNotNull(ma.Bless);
            StringAssert.Contains(CardMark.COPY_LINE, ma.Lines());

            // 신탁 없는 원본을 복제 → 원본이 신탁을 받는다 → 복제본은 그대로, 다시 복제하면 새 모습의 복제본(꼬리 하나 더)
            run.S.Deck.Add("rico_u2");
            var b = run.AddCopy("rico_u2");
            Assert.AreEqual("rico_u2^", b);
            var g = new Glow { Kind = "card", Picks = new List<GlowPick> { new GlowPick { N = 3, Shin = "draw" } } };
            Assert.IsNull(run.ClaimGlow("rico_u2", g, 0));
            Assert.AreEqual(3, run.S.Flash["rico_u2"]);
            Assert.IsFalse(run.S.Flash.ContainsKey(b), "원본이 신탁을 받아도 복제본은 그대로");
            Assert.IsFalse(run.S.Shin.ContainsKey(b));
            var b2 = run.AddCopy("rico_u2");
            Assert.AreEqual("rico_u2^^", b2, "모습이 다르면 따로 된 복제본");
            Assert.AreEqual(3, run.S.Flash[b2]); Assert.AreEqual("draw", run.S.Shin[b2]);
            Assert.AreEqual("rico_u2", GameData.BaseId(b2)); Assert.AreEqual(2, GameData.CopyNo(b2));
            Assert.AreEqual("rico_u2^^", run.AddCopy("rico_u2"), "같은 모습이면 같은 id 로 한 장 더");
            Assert.AreEqual(2, run.S.Deck.Count(x => x == b2));

            // 저장 · 불러오기에서도 그대로
            var r2 = Run.Load(run.Data, run.Save());
            Assert.IsFalse(r2.MarkOf(b).Oracle != null);
            Assert.AreEqual(run.MarkOf(b2).Oracle, r2.MarkOf(b2).Oracle);
            Assert.IsTrue(r2.MarkOf(b2).Copy && r2.MarkOf(b2).Blessed);
        }

        [Test] public void 복제본은_빛_수련_이벤트_신탁_축복_후보에_들지_않는다()
        {
            for (long seed = 1; seed <= 80; seed++)
            {
                var run = New(seed);
                run.S.Deck.Add("rico_u1");
                var cid = run.AddCopy("rico_u1");
                run.S.Deck.Remove("rico_u1");   // 원본을 빼도 복제본은 대상이 아니다
                run.S.Deck.AddRange(new[] { "carrot_u1", "sion_u1" });
                CollectionAssert.DoesNotContain(run.FlashTargets(), cid);
                run.S.RewardFlash = true; run.S.Elite = seed % 2 == 0; run.S.Node = 1;
                var glow = run.RollEpiphany();
                Assert.IsFalse(glow.Keys.Any(GameData.IsCopy), $"씨앗 {seed}: 복제본이 빛났다");
                var camp = run.EnterCamp("camp");
                Assert.IsFalse(GameData.IsCopy(camp.Train?.CardId), "수련 후보");
                Assert.IsFalse(GameData.IsCopy(run.OfferFlash()?.CardId), "이벤트 신탁 후보");
                CollectionAssert.DoesNotContain(run.ShinAble(null), cid, "축복 얹기");
                CollectionAssert.DoesNotContain(run.ShinAble("power"), cid);
                Assert.IsFalse(run.FlashOk(cid, 1));
                var g = new Glow { Kind = "card", Picks = new List<GlowPick> { new GlowPick { N = 1 } } };
                Assert.IsNotNull(run.ClaimGlow(cid, g, 0), "보상 신탁도 막힌다");
                Assert.IsFalse(run.S.Flash.ContainsKey(cid));
            }
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

        [Test] public void 이벤트_은총은_사도를_골라_그_사도의_남은_고유_카드를_얻는다()
        {
            var run = New();
            run.S.Event = new EventState { Key = "t", Id = "X", Phase = "result" };
            run.ApplyOutcomes(new List<Outcome> { new Outcome { K = "unique", N = 2 } });
            var p = run.S.Event.Pending[0];
            Assert.AreEqual("grace", p.K); Assert.AreEqual(2, p.N);
            // 저장 · 불러오기 중간에도 그대로
            run = Run.Load(run.Data, run.Save());
            Assert.AreEqual("grace", run.S.Event.Pending[0].K);
            Assert.IsNotNull(run.ResolvePending("nobody"), "파티에 없는 사도");
            var left = run.UniquesLeft("carrot");
            Assert.Greater(left.Count, 0);
            int deck = run.S.Deck.Count;
            Assert.IsNull(run.ResolvePending("carrot"));
            var got = run.S.Deck.Skip(deck).ToList();
            Assert.AreEqual(System.Math.Min(2, left.Count), got.Count, "고른 사도의 고유 카드 n 장");
            Assert.IsTrue(got.All(id => left.Contains(id) && run.Data.Card(id).Hero == "carrot"));
            Assert.AreEqual(0, run.S.Event.Pending.Count);
            // 남은 고유 카드가 없는 사도는 고를 수 없다
            foreach (var id in run.UniquesLeft("carrot")) run.S.Deck.Add(id);
            CollectionAssert.DoesNotContain(run.GraceHeroes(), "carrot");
            run.S.Event.Pending.Add(new Pending { K = "grace", N = 1 });
            Assert.IsNotNull(run.ResolvePending("carrot"));
            Assert.IsNull(run.ResolvePending(null), "받지 않기");
            // 파티 셋 다 없으면 대신 골드
            foreach (var k in run.S.Party) foreach (var id in run.UniquesLeft(k)) run.S.Deck.Add(id);
            int g = run.S.Gold;
            run.ApplyOutcomes(new List<Outcome> { new Outcome { K = "unique" } });
            Assert.AreEqual(0, run.S.Event.Pending.Count);
            Assert.AreEqual(g + Run.GRACE_GOLD, run.S.Gold);
        }

        [Test] public void 이벤트_사도_조건_골드_잠금_판정_고르는_것()
        {
            var run = New();
            var c1 = run.Data.Event("C1");
            Assert.AreEqual(3, run.OptionsOf(c1).Count, "선택지 둘 + 떠나기");
            run.Data.Add(null, null, null, null, "[{\"id\":\"H1\",\"name\":\"사도 선택지\",\"options\":[{\"label\":\"리코타만\",\"hero\":[\"rico\"],\"out\":[{\"k\":\"gold\",\"v\":1}]},{\"label\":\"없는 사도만\",\"hero\":[\"nobody\"],\"out\":[]}]}]", null);
            // 조건이 안 맞는 사도 선택지는 잠긴 칸으로 보인다(2026-10-07) — 조건 글이 잠금 까닭
            var h1 = run.OptionsOf(run.Data.Event("H1"));
            CollectionAssert.AreEqual(new[] { "리코타만", "없는 사도만", "떠나기" }, h1.Select(o => o.Label));
            Assert.IsNull(run.LockOf(h1[0]));
            StringAssert.Contains("파티에 있어야", run.LockOf(h1[1]));
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
    
        // ── 빛은 카드 한 장에만(2026-10-07 사용자 제보 — 같은 기본 카드 여러 장이 모두 빛났다) ──
        static (Run run, string basic, string nz) ThreeOfEach(long seed)
        {
            var run = New(seed);
            run.Data.Cards["nz"] = new CardDef
            {
                Id = "nz", Name = "시험 교주 카드", Type = "공격", Cost = 1,
                Oracles = Enumerable.Range(1, 5).Select(i => new OracleDef { Name = "시험 신탁 " + i }).ToList(),
            };
            var basic = run.S.Deck.First(id => run.Data.Card(id) is CardDef c && c.Hero == "rico" && !c.Unique);
            var nz = GameData.WithOwner("nz", "rico");
            run.S.Deck.RemoveAll(x => x == basic);
            for (int i = 0; i < 3; i++) { run.S.Deck.Add(basic); run.S.Deck.Add(nz); }
            run.S.Shin[basic] = "draw";   // 갈라 낸 한 장도 축복을 이어 받는다
            run.S.ForceGlow = new Dictionary<string, Glow>
            {
                [basic] = new Glow { Kind = "hero", Hero = "rico", Options = new List<string> { "rico_u1" } },
                [nz] = new Glow { Kind = "card", Picks = new List<GlowPick> { new GlowPick { N = 2 }, new GlowPick { N = 4, Shin = "power" } } },
            };
            run.EnterNode(run.Reachable()[0]);
            return (run, basic, nz);
        }

        static List<string> All(Battle b) => b.Draw.Concat(b.Hand).Concat(b.Discard).ToList();

        [Test] public void 같은_카드_세_장이면_빛은_한_장에만_서고_보이고_고른_뒤에도_한_장만_바뀐다()
        {
            var (run, basic, nz) = ThreeOfEach(7);
            var (b, _) = run.OpenFight();
            var all = All(b);
            Assert.AreEqual(3, all.Count(x => GameData.NoInst(x) == basic));
            Assert.AreEqual(3, all.Count(x => GameData.NoInst(x) == nz));
            Assert.AreEqual(1, all.Count(x => GameData.NoInst(x) == basic && b.GlowOf(x) != null), "은총 빛은 기본 카드 한 장에만");
            Assert.AreEqual(1, all.Count(x => GameData.NoInst(x) == nz && b.GlowOf(x) != null), "신탁 빛은 교주 카드 한 장에만");
            var litB = all.Single(x => GameData.NoInst(x) == basic && b.GlowOf(x) != null);
            var litN = all.Single(x => GameData.NoInst(x) == nz && b.GlowOf(x) != null);
            Assert.AreNotEqual(basic, litB); Assert.AreEqual(basic, GameData.BaseId(litB));
            Assert.AreEqual("rico", GameData.OwnerOf(litN)); Assert.AreEqual("nz", GameData.BaseId(litN));
            Assert.AreEqual("draw", b.ShinOf(litB), "갈라 낸 한 장도 축복을 이어 받는다");
            Assert.AreEqual(run.Data.Card(basic).Name, b.CardOf(litB).Name);

            // 손에 여섯 장 — 빛은 둘, 표식(신탁)은 고른 한 장에만
            K.Hand(b, all.Where(x => GameData.NoInst(x) == basic || GameData.NoInst(x) == nz).ToArray());
            Assert.AreEqual(6, b.Hand.Count);
            Assert.AreEqual(2, b.Hand.Count(x => b.GlowOf(x) != null), "손에서도 빛나는 카드는 둘");
            Assert.AreEqual("card", b.ApplyEpiphany(litN, 1));
            Assert.IsNotNull(b.MarkOf(litN).Oracle);
            Assert.AreEqual(1, b.Hand.Count(x => b.MarkOf(x).Oracle != null), "신탁은 고른 한 장만 바뀐다");
            Assert.AreEqual(0, b.Hand.Count(x => GameData.NoInst(x) == nz && b.GlowOf(x) != null));
            Assert.AreEqual(1, b.Hand.Count(x => b.GlowOf(x) != null), "기본 카드 빛은 그대로 한 장");

            // 전투 저장 · 불러오기에도 그대로
            var b2 = Battle.Load(run.Data, b.Save());
            Assert.AreEqual(1, b2.Hand.Count(x => b2.GlowOf(x) != null));
            Assert.AreEqual(1, b2.Hand.Count(x => b2.MarkOf(x).Oracle != null));

            // 판에 — 신탁은 덱의 한 장에만, 은총 빛(안 냄)은 덱을 건드리지 않는다
            run.AfterFight(b);
            Assert.AreEqual(3, run.S.Deck.Count(x => x == basic), "안 쓴 은총 빛 — 기본 카드는 그대로 세 장");
            Assert.AreEqual(3, run.S.Deck.Count(x => GameData.NoInst(x) == nz));
            Assert.AreEqual(1, run.S.Deck.Count(x => GameData.NoInst(x) == nz && run.MarkOf(x).Oracle != null), "판에서도 한 장만 신탁");
            Assert.AreEqual(4, run.S.Flash[litN]); Assert.AreEqual("power", run.S.Shin[litN]);
            Assert.IsFalse(run.S.Flash.ContainsKey(nz), "나머지 두 장은 맨 카드");
            Assert.AreEqual(2, run.S.Deck.Count(x => x == nz));

            // 남은 은총(안 낸 빛)을 끝난 뒤 받는다
            Assert.IsNull(run.ClaimGlow(litB, b.Glow[litB], 0));
            CollectionAssert.Contains(run.S.Deck, "rico_u1");
            Assert.AreEqual(3, run.S.Deck.Count(x => x == basic));

            // 판 저장 · 불러오기, 신탁 받은 한 장의 복제(복제본은 그 모습 그대로)
            var r2 = Run.Load(run.Data, run.Save());
            Assert.AreEqual(1, r2.S.Deck.Count(x => GameData.NoInst(x) == nz && r2.MarkOf(x).Oracle != null));
            var cp = run.AddCopy(litN);
            Assert.AreEqual(nz + "^", cp);
            Assert.AreEqual(4, run.S.Flash[cp]);
            Assert.IsFalse(run.FlashOk(cp, 1), "복제본은 신탁 불가");
            CollectionAssert.DoesNotContain(run.FlashTargets(), litN, "신탁 받은 한 장은 다시 후보가 아니다");
            CollectionAssert.Contains(run.FlashTargets(), nz, "맨 두 장은 아직 후보");
        }

        [Test] public void 안_낸_신탁_빛을_끝난_뒤_받아도_한_장만_바뀐다()
        {
            var (run, basic, nz) = ThreeOfEach(11);
            var (b, _) = run.OpenFight();
            var litN = All(b).Single(x => GameData.NoInst(x) == nz && b.GlowOf(x) != null);
            run.AfterFight(b);
            Assert.AreEqual(3, run.S.Deck.Count(x => x == nz), "전투가 끝나도 덱은 그대로");
            Assert.IsNull(run.ClaimGlow(litN, b.Glow[litN], 0));
            Assert.AreEqual(1, run.S.Deck.Count(x => GameData.NoInst(x) == nz && run.S.Flash.ContainsKey(x)));
            Assert.AreEqual(2, run.S.Deck.Count(x => x == nz));
            Assert.AreEqual(2, run.S.Flash[run.S.Deck.Single(x => GameData.IsInst(x))]);
        }

        [Test] public void 한_장_번호_id_는_정의_주인_꼬리를_지킨다()
        {
            Assert.AreEqual("a#2", GameData.WithInst("a", 2));
            Assert.AreEqual("n_x@rico#1", GameData.WithInst("n_x@rico", 1));
            Assert.AreEqual("n_x", GameData.BaseId("n_x@rico#1"));
            Assert.AreEqual("rico", GameData.OwnerOf("n_x@rico#1"));
            Assert.AreEqual("n_x@rico", GameData.NoInst("n_x@rico#1"));
            Assert.AreEqual("n_x@sion#1", GameData.WithOwner("n_x@rico#1", "sion"));
            Assert.AreEqual("a", GameData.BaseId("a#3~"));
            Assert.AreEqual("a~", GameData.NoInst("a#3~"));
            Assert.IsFalse(GameData.IsInst("a")); Assert.IsTrue(GameData.IsInst("a#1"));
        }
    }
}
