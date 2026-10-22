using System.Collections.Generic;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>진행 코드(ProgressCode.cs) — 편성 기억 왕복 · 틀린 코드 거부 · 옛 교주 보드 코드(판 1~3)에서 덧붙임만.</summary>
    public class ProgressCodeTests
    {
        static string Code(string json)
        {
            var body = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            return $"BZP1-{body}-{ProgressCode.Checksum(body):x8}";
        }

        [Test] public void 진행_코드_왕복_빈_것과_꽉_찬_것()
        {
            foreach (var x in new[] { new Dictionary<string, string>(), new Dictionary<string, string> { ["recent"] = "a|b|c", ["preset1"] = "a|b|c", ["preset5"] = "c||a", ["tag1"] = "광기", ["clears"] = "a=3;b=1" } })
            {
                var code = ProgressCode.Export(x);
                StringAssert.StartsWith("BZP1-", code);
                var (back, why) = ProgressCode.Import(code);
                Assert.IsNull(why, why);
                CollectionAssert.AreEquivalent(x, back);
                Assert.AreEqual(code, ProgressCode.Export(back), "다시 내보내도 같은 코드");
            }
        }

        [Test] public void 틀린_코드는_거부한다()
        {
            var code = ProgressCode.Export(new Dictionary<string, string> { ["recent"] = "rico|carrot|" });
            var parts = code.Split('-');
            var bad = parts[0] + "-" + (parts[1][0] == 'e' ? "f" : "e") + parts[1].Substring(1) + "-" + parts[2];
            Assert.IsNotNull(ProgressCode.Import(bad).why, "체크섬");
            Assert.IsNotNull(ProgressCode.Import("아무거나").why);
            Assert.IsNotNull(ProgressCode.Import("").why);
            Assert.IsNotNull(ProgressCode.Import(Code("{\"v\":99,\"x\":{}}")).why, "모르는 새 판");
        }

        [Test] public void 옛_교주_보드_코드는_덧붙임만_받고_보드만_담긴_코드는_받지_않는다()
        {
            // 판 3(보드 + 덧붙임) — 보드 몫(h · e · l · r)은 버린다
            var (x, why) = ProgressCode.Import(Code("{\"v\":3,\"h\":[3,1,0,0],\"e\":[9,2,1,0],\"l\":{\"atk\":1},\"r\":2,\"x\":{\"recent\":\"rico|carrot|\",\"preset1\":\"rico|carrot|sion\"}}"));
            Assert.IsNull(why, why);
            CollectionAssert.AreEquivalent(new Dictionary<string, string> { ["recent"] = "rico|carrot|", ["preset1"] = "rico|carrot|sion" }, x);
            // 판 1(보드만 · v 없음) — 받을 것이 없다
            var (o, ow) = ProgressCode.Import(Code("{\"h\":[1,2,0,0],\"e\":[1,2,0,0],\"l\":{\"atk\":1},\"r\":1}"));
            Assert.IsNull(o); Assert.IsNotNull(ow);
        }
    }
}
