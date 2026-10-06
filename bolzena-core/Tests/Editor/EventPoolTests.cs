using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>마을 이벤트(2026-10-06) — pool 이 마을 id 면 그 마을 판에서만, floor 가 있으면 그 층에서만, 한 판에 한 번만.</summary>
    public class EventPoolTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        const string EVS = @"[
          { ""id"": ""V1"", ""name"": ""1층 마을"", ""pool"": ""erpien"", ""floor"": 1, ""options"": [ { ""label"": ""a"", ""out"": [] } ] },
          { ""id"": ""V2"", ""name"": ""2층 마을"", ""pool"": ""erpien"", ""floor"": 2, ""options"": [ { ""label"": ""a"", ""out"": [] } ] },
          { ""id"": ""V0"", ""name"": ""두 층 마을"", ""pool"": ""erpien"", ""options"": [ { ""label"": ""a"", ""out"": [] } ] },
          { ""id"": ""X1"", ""name"": ""다른 마을"", ""pool"": ""monatium"", ""floor"": 1, ""options"": [ { ""label"": ""a"", ""out"": [] } ] }
        ]";

        static Run New(long seed)
        {
            var d = K.Sample(); d.Add(null, null, null, null, EVS, null);
            return Run.New(d, PARTY, seed);
        }

        [Test] public void 마을_이벤트는_그_마을_그_층에서만()
        {
            var got = new[] { new HashSet<string>(), new HashSet<string>() };
            for (long seed = 1; seed <= 40; seed++)
                for (int floor = 0; floor < 2; floor++)
                {
                    var run = New(seed); run.S.Floor = floor;
                    for (int i = 0; i < 30; i++) { run.S.EventsSeen.Clear(); foreach (var e in run.RollEvents()) got[floor].Add(e.Id); }
                }
            foreach (var ids in got) Assert.IsFalse(ids.Contains("X1"), "다른 마을 이벤트");
            Assert.IsFalse(got[0].Contains("V2"), "1층에 2층 이벤트");
            Assert.IsFalse(got[1].Contains("V1"), "2층에 1층 이벤트");
            Assert.IsTrue(got[0].Contains("V1") && got[0].Contains("V0"), "1층: 1층 마을 이벤트 · 두 층 이벤트가 뽑힌다");
            Assert.IsTrue(got[1].Contains("V2") && got[1].Contains("V0"), "2층: 2층 마을 이벤트 · 두 층 이벤트가 뽑힌다");
        }

        [Test] public void 한_판에_같은_이벤트는_한_번만_뜨는_순간_나온_것으로()
        {
            var run = New(7);
            var seen = new List<string>();
            for (int i = 0; i < 40; i++)
            {
                run.S.Event = null; run.S.Node = i;   // 칸마다 새로 굴린다
                var E = run.EnterEvent();
                if (E.Id == null) break;
                Assert.IsFalse(seen.Contains(E.Id), "겹친 이벤트 " + E.Id);
                Assert.IsTrue(run.S.EventsSeen.Contains(E.Id), "고르기 전에 이미 나온 것으로");
                seen.Add(E.Id);
            }
            Assert.That(seen.Count, Is.GreaterThan(3));
            Assert.IsFalse(run.EventLeft() && run.RollEvents().Any(e => seen.Contains(e.Id)));
        }
    }
}
