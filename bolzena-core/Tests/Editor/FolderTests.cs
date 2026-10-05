using System;
using System.IO;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>폴더 통째 읽기 — 묶음 파일 · 배열 파일 · 겹친 id · 건너뛰기.</summary>
    public class FolderTests
    {
        string dir;
        [SetUp] public void Make() { dir = Path.Combine(Path.GetTempPath(), "bz_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); }
        [TearDown] public void Clean() { try { Directory.Delete(dir, true); } catch (Exception) { } }
        void W(string rel, string json) { var p = Path.Combine(dir, rel); Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p, json.Replace('\'', '"')); }

        [Test] public void 묶음과_배열을_재귀로_합친다()
        {
            W("heroes/pace/h1.json", "{'heroes':[{'id':'h1','name':'하나','role':'탱커','hp':900,'atk':80,'def':60,'starter':['c1']}], 'cards':[{'id':'c1','name':'카드','hero':'h1','cost':1,'type':'스킬','fx':[{'k':'draw','v':1}]}]}");
            W("common/enemies.json", "[{'id':'e1','name':'적','hp':100,'intents':[{'t':'attack','v':10}]}]");
            W("common/misc.json", "{'$schema':'x', 'equips':[{'id':'q1','name':'장비','grade':'일반','slot':'무기'}]}");
            W("_draft/skip.json", "{'heroes':[{'id':'nope'}]}");
            W("x.schema.json", "{'whatever':1}");
            var d = GameData.FromFolder(dir);
            Assert.AreEqual(1, d.Heroes.Count); Assert.AreEqual(1, d.Cards.Count); Assert.AreEqual(1, d.Enemies.Count); Assert.AreEqual(1, d.Equips.Count);
            Assert.AreEqual("heroes/pace/h1.json", d.SourceOf("card", "c1"));
            StringAssert.Contains("(heroes/pace/h1.json)", d.At("hero", "h1"));
        }

        [Test] public void 같은_id_가_두_번이면_오류_두_파일을_적는다()
        {
            W("a.json", "{'cards':[{'id':'c1','name':'가','cost':0,'type':'스킬'}]}");
            W("b/c.json", "{'cards':[{'id':'c1','name':'나','cost':0,'type':'스킬'}]}");
            var e = Assert.Throws<FormatException>(() => GameData.FromFolder(dir));
            StringAssert.Contains("「c1」", e.Message);
            StringAssert.Contains("a.json", e.Message);
            StringAssert.Contains("b/c.json", e.Message);
        }

        [Test] public void 모르는_최상위_키_배열_파일_이름_JSON_꼴을_잡는다()
        {
            W("a.json", "{'card':[]}");
            W("b.json", "[]");
            W("c.json", "{ not json");
            var e = Assert.Throws<FormatException>(() => GameData.FromFolder(dir));
            StringAssert.Contains("모르는 키 「card」", e.Message);
            StringAssert.Contains("b.json: 배열 파일은", e.Message);
            StringAssert.Contains("c.json: JSON 꼴이 아니다", e.Message);
        }

        [Test] public void 모르는_칸은_파일_이름과_함께()
        {
            W("h.json", "{'cards':[{'id':'c1','name':'가','cost':0,'type':'스킬','ratoi':1}]}");
            var e = Assert.Throws<FormatException>(() => GameData.FromFolder(dir));
            StringAssert.Contains("h.json", e.Message);
            StringAssert.Contains("ratoi", e.Message);
        }

        [Test] public void 여러_경로를_한_벌로()
        {
            W("one/a.json", "{'cards':[{'id':'c1','name':'가','cost':0,'type':'스킬'}]}");
            W("two/b.json", "{'cards':[{'id':'c2','name':'나','cost':0,'type':'스킬'}]}");
            var d = GameData.FromFolders(new[] { Path.Combine(dir, "one"), Path.Combine(dir, "two") });
            Assert.AreEqual(2, d.Cards.Count);
        }
    }
}
