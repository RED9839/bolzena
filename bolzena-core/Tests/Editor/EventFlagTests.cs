using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Bolzena.Core.Tests
{
    /// <summary>연속 이벤트 깃발 · 잠긴 칸(2026-10-07) — flag 결과가 깃발을 세우고, flag · noFlag 선택지와 needFlag 이벤트가 읽는다. 사도 · HP 조건은 잠금으로 보인다.</summary>
    public class EventFlagTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        const string EVS = @"[
          { ""id"": ""A1"", ""name"": ""앞"", ""pool"": ""erpien"", ""floor"": 1, ""options"": [
              { ""label"": ""돕기"", ""out"": [ { ""k"": ""flag"", ""id"": ""t_friend"" }, { ""k"": ""gold"", ""v"": 1 } ] } ] },
          { ""id"": ""B1"", ""name"": ""뒤"", ""pool"": ""erpien"", ""floor"": 2, ""options"": [
              { ""label"": ""겨루기"", ""noFlag"": ""t_friend"", ""out"": [] },
              { ""label"": ""아는 사이"", ""flag"": ""t_friend"", ""out"": [] },
              { ""label"": ""보이는 깃발"", ""flag"": ""t_friend"", ""showLocked"": true, ""lockText"": ""숲에서 도왔다면"", ""out"": [] },
              { ""label"": ""지친 파티"", ""when"": ""hp30"", ""out"": [] },
              { ""label"": ""숨긴 사도"", ""hero"": [ ""nobody"" ], ""showLocked"": false, ""out"": [] } ] },
          { ""id"": ""B2"", ""name"": ""2층 다른 이벤트"", ""pool"": ""erpien"", ""floor"": 2, ""options"": [ { ""label"": ""a"", ""out"": [] } ] },
          { ""id"": ""B3"", ""name"": ""2층 또 다른 이벤트"", ""pool"": ""erpien"", ""floor"": 2, ""options"": [ { ""label"": ""a"", ""out"": [] } ] },
          { ""id"": ""C1"", ""name"": ""줄기 뒤편"", ""pool"": ""erpien"", ""needFlag"": ""t_friend"", ""options"": [ { ""label"": ""a"", ""out"": [] } ] }
        ]";

        static Run New(long seed = 3)
        {
            var d = K.Sample(); d.Add(null, null, null, null, EVS, null);
            return Run.New(d, PARTY, seed);
        }

        static string[] Labels(Run run, string id) => run.OptionsOf(run.Data.Event(id)).Select(o => o.Label).ToArray();

        [Test] public void 깃발이_없으면_noFlag_만_보이고_깃발_조건은_숨는다()
        {
            var run = New();
            run.S.PartyHp = run.S.PartyMaxHp;
            var labels = Labels(run, "B1");
            CollectionAssert.AreEqual(new[] { "겨루기", "보이는 깃발", "지친 파티", "떠나기" }, labels);
            var opts = run.OptionsOf(run.Data.Event("B1"));
            Assert.AreEqual("숲에서 도왔다면", run.LockOf(opts[1]));
            StringAssert.Contains("HP 30%", run.LockOf(opts[2]));
        }

        [Test] public void flag_결과가_깃발을_세우고_뒤_이벤트_선택지가_바뀐다()
        {
            var run = New();
            run.S.Event = new EventState { Key = "t", Choices = new List<string> { "A1" }, Id = "A1" };
            var (fight, why) = run.Choose(0);
            Assert.IsFalse(fight); Assert.IsNull(why);
            Assert.IsTrue(run.HasFlag("t_friend"));
            Assert.IsFalse(run.S.Event.Log.Any(l => l.Contains("flag")), "깃발은 결과 글에 안 보인다");
            var labels = Labels(run, "B1");
            CollectionAssert.AreEqual(new[] { "아는 사이", "보이는 깃발", "지친 파티", "떠나기" }, labels);
            Assert.IsNull(run.LockOf(run.OptionsOf(run.Data.Event("B1"))[0]));
        }

        [Test] public void needFlag_이벤트는_깃발이_서야_뽑힌다()
        {
            var seen = new HashSet<string>();
            for (long seed = 1; seed <= 30; seed++)
            {
                var run = New(seed);
                for (int i = 0; i < 20; i++) { run.S.EventsSeen.Clear(); foreach (var e in run.RollEvents()) seen.Add(e.Id); }
            }
            Assert.IsFalse(seen.Contains("C1"), "깃발 없이 줄기 뒤편이 뽑혔다");
            var got = false;
            for (long seed = 1; seed <= 30 && !got; seed++)
            {
                var run = New(seed); run.S.Flags.Add("t_friend");
                for (int i = 0; i < 20 && !got; i++) { run.S.EventsSeen.Clear(); got = run.RollEvents().Any(e => e.Id == "C1"); }
            }
            Assert.IsTrue(got, "깃발이 서면 줄기 뒤편이 뽑힌다");
        }

        [Test] public void 깃발이_서면_줄기_뒤_이벤트가_더_잘_뽑힌다()
        {
            // 2층 마을 풀: B1(깃발을 읽는 줄기 뒤) · B2 · B3 · C1(needFlag) — 깃발이 서면 B1 · C1 무게 ×3(R.EVENT_FLAG_WEIGHT)
            // B1 이 B2(깃발과 무관한 같은 층 이벤트)보다 몇 배 뽑히나 — 깃발이 없으면 약 1배, 서면 약 3배
            double Ratio(bool flag)
            {
                int hit = 0, all = 0;
                for (long seed = 1; seed <= 60; seed++)
                {
                    var run = New(seed); run.S.Floor = 1;
                    if (flag) run.S.Flags.Add("t_friend");
                    for (int i = 0; i < 30; i++)
                    {
                        run.S.EventsSeen.Clear();
                        var e = run.RollEvents().FirstOrDefault();
                        if (e == null || e.Pool == "공용") continue;
                        if (e.Id == "B1") hit++; else if (e.Id == "B2") all++;
                    }
                }
                return all == 0 ? 0 : (double)hit / all;
            }
            double off = Ratio(false), on = Ratio(true);
            Assert.That(off, Is.InRange(0.6, 1.6), $"깃발 없음 B1/B2 {off:0.00}");
            Assert.That(on, Is.InRange(2.0, 4.5), $"깃발 있음 B1/B2 {on:0.00}(무게 ×{R.EVENT_FLAG_WEIGHT})");
        }

        [Test] public void 깃발은_판_저장에_남는다()
        {
            var run = New();
            run.S.Flags.Add("t_friend");
            var back = JsonConvert.DeserializeObject<RunState>(JsonConvert.SerializeObject(run.S));
            Assert.IsTrue(back.Flags.Contains("t_friend"));
        }

        [Test] public void 잠긴_칸은_고를_수_없다()
        {
            var run = New();
            run.S.PartyHp = run.S.PartyMaxHp;
            run.S.Event = new EventState { Key = "t", Choices = new List<string> { "B1" }, Id = "B1" };
            int idx = run.OptionsOf(run.Data.Event("B1")).FindIndex(o => o.Label == "지친 파티");
            var (_, why) = run.Choose(idx);
            Assert.IsNotNull(why);
            Assert.AreEqual("choose", run.S.Event.Phase);
        }

        [Test] public void 검사기는_짝없는_깃발을_주의로()
        {
            var d = K.Sample(); d.Add(null, null, null, null, @"[{ ""id"": ""Z1"", ""name"": ""z"", ""options"": [ { ""label"": ""a"", ""flag"": ""t_none"", ""out"": [] } ] }]", null);
            var rep = Validator.Check(d);
            Assert.IsTrue(rep.Warnings.Any(w => w.Contains("t_none")), string.Join("\n", rep.Warnings));
        }
    }
}
