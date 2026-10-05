using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>Docs/schema/bolzena.schema.json(편집기 자동 완성 · 즉시 검사용)이 엔진의 낱말 목록과 어긋나지 않는가.</summary>
    public class SchemaTests
    {
        static JObject Schema()
        {
            foreach (var p in new[] { "Packages/com.bolzena.core/Docs/schema/bolzena.schema.json", "../bolzena-core/Docs/schema/bolzena.schema.json" })
                if (File.Exists(p)) return JObject.Parse(File.ReadAllText(p));
            Assert.Fail("스키마를 못 찾았다");
            return null;
        }

        static string[] Enum(JObject s, string path) => ((JArray)s.SelectToken(path)).Select(x => (string)x).ToArray();

        [Test] public void 효과_조각_종류가_같다() =>
            CollectionAssert.AreEquivalent(FxK.All, Enum(Schema(), "$defs.fx.properties.k.enum"));

        [Test] public void 상태_이름이_같다() =>
            CollectionAssert.AreEquivalent(R.ALL_ST, Enum(Schema(), "$defs.fx.allOf[0].then.properties.id.enum"));

        [Test] public void 태그가_같다()
        {
            var pat = (string)Schema().SelectToken("$defs.tags.items.pattern");
            foreach (var t in Tag.All) StringAssert.Contains(t, pat);
            Assert.AreEqual(Tag.All.Length, pat.Split('|').Length);
        }

        [Test] public void 성격이_같다() =>
            CollectionAssert.AreEquivalent(R.NATURES, Enum(Schema(), "$defs.hero.properties.nature.enum"));
    }
}
