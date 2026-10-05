using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>샘플 콘텐츠(사도 셋 · 적 · 마을 하나 · 이벤트 · 장비)로 판이 끝까지 도는가 — 봇 · 시뮬 포함.</summary>
    public class IntegrationTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };

        [Test] public void 똑똑한_봇은_판을_끝까지_돈다()
        {
            var d = K.Sample();
            for (long seed = 1; seed <= 6; seed++)
            {
                var r = new RunBot(d).RunFull(PARTY, seed, new SimOpts());
                Assert.Greater(r.Fights, 0, $"씨앗 {seed}");
                Assert.IsTrue(r.Clear || r.Where != null, $"씨앗 {seed} — 이기거나 어딘가에서 쓰러졌다");
                if (r.Clear) Assert.IsTrue(r.Kinds.ContainsKey("2:boss"), "완주는 2층 보스를 넘은 것");
            }
        }

        [Test] public void 생각_없는_봇도_판을_끝까지_돈다()
        {
            var d = K.Sample();
            var r = new RunBot(d).RunFull(PARTY, 3, new SimOpts { SmartFight = false, SmartOut = false });
            Assert.Greater(r.Fights, 0);
        }

        [Test] public void 같은_씨앗이면_같은_판()
        {
            var d = K.Sample();
            string One(long seed) { var r = new RunBot(d).RunFull(PARTY, seed, new SimOpts()); return $"{r.Clear}|{r.Floor}|{r.Where}|{r.Fights}|{r.Turns}|{r.Gold}|{r.Deck}"; }
            Assert.AreEqual(One(21), One(21));
        }

        [Test] public void 세게_하면_진다()
        {
            var d = K.Sample();
            var r = new RunBot(d).RunFull(PARTY, 5, new SimOpts { Hpx = 3, Dmgx = 3 });
            Assert.IsFalse(r.Clear);
        }

        [Test] public void 판_하나를_손으로_끝까지()
        {
            // 화면이 부르는 차례 그대로 — 지도 칸 → 싸움 · 이벤트 · 캠프 → 보상 · 장비 → 층 → 완주 또는 패배. 봇은 싸움의 카드만 고른다
            var d = K.Sample();
            var run = Run.New(d, PARTY, 8);
            var bots = new Bots(d);
            int guard = 0;
            while (run.S.Done == null && guard++ < 100)
            {
                var node = run.EnterNode(run.Reachable()[0]);
                Assert.IsNotNull(node);
                if (node.Type == "fight" || node.Type == "elite" || node.Type == "boss")
                {
                    var cues = new List<Cue>();
                    var (b, loot) = run.OpenFight(cues: cues);
                    while (b.Over == null && b.Turn < 40) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
                    Assert.IsTrue(cues.Any(c => c.K == "act"), "연출 쪽지가 쌓인다");
                    run.AfterFight(b);
                    if (b.Over != "win") { Assert.IsTrue(run.PartyWiped || b.Turn >= 40); return; }
                    run.TakeGold();
                    if (loot?.Equip != null) { run.TakeEquip(loot.Equip[0]); var e = d.Equip(loot.Equip[0]); run.Equip(PARTY.First(k => !run.GearOf(k).ContainsKey(e.Slot)) ?? PARTY[0], e.Id, replace: true); }
                    if (run.IsBoss) { var off = run.BossCopyOffer(); if (off.Count > 0) run.BossCopy(off[0]); run.Advance(); }
                }
                else if (node.Type == "event")
                {
                    run.EnterEvent();
                    if (run.S.Event.Id == null) run.PickEvent(run.S.Event.Choices[0]);
                    var ev = d.Event(run.S.Event.Id);
                    var opts = run.OptionsOf(ev);
                    int pick = opts.FindIndex(o => run.LockOf(o) == null && o.Fight == null);
                    run.Choose(pick);
                    while (run.S.Event.Pending.Count > 0)
                    {
                        var p = run.S.Event.Pending[0];
                        object v = p.K switch { "card" => p.Cards[0], "flash" => p.Offer.Picks[0], "gambleChoice" => 0, "remove" => run.S.Deck[0], _ => null };
                        if (run.ResolvePending(v) != null) run.S.Event.Pending.RemoveAt(0);
                    }
                    run.LeaveEvent();
                }
                else if (node.Type == "camp" || node.Type == "campshop")
                {
                    run.EnterCamp(node.Type);
                    if (node.Type == "campshop") run.RollShop();
                    run.CampRest();
                }
                foreach (var id in run.S.Bag.ToList()) if (run.SellEquip(id) != null) run.Equip(PARTY[0], id, replace: true);
                // 저장 · 이어하기가 판 내내 된다
                var again = Run.Load(d, run.Save());
                Assert.AreEqual(run.S.Floor, again.S.Floor);
            }
            Assert.AreEqual("clear", run.S.Done);
        }

        [Test] public void 메타_시뮬()
        {
            var d = K.Sample();
            var r = MetaSim.Run(d, rounds: 6, seed: 0, threads: 2);
            Assert.AreEqual(6, r.Runs);
            Assert.AreEqual(3, r.Comps.Count, "샘플 사도 넷(에르핀 더함) — 셋씩 섞은 편성");
            Assert.AreEqual(4, r.Heroes.Count);
            var r2 = MetaSim.Run(d, rounds: 6, seed: 0, threads: 1);
            Assert.AreEqual(r.Clear, r2.Clear, "작업 수와 상관없이 같다");
            StringAssert.Contains("메타 통계", MetaSim.Report(d, r));
        }
    }
}
