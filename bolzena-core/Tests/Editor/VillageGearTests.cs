using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 마을 장비(2026-10-21 카제나식 — bolzena-unity/Docs/마을_장비_설계.md) — 장비 pool 이 마을 id 면 그 마을 판에서만, 없거나 「공용」 이면 모든 판.
    /// 그 칸 · 등급이 마을에 없으면 모든 장비에서. pool 을 안 적은 콘텐츠는 지금과 같은 뽑기(난수까지).
    /// </summary>
    public class VillageGearTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        const string EQ = @"[
          { ""id"": ""vg_er"", ""name"": ""요정 검"", ""grade"": ""희귀"", ""slot"": ""무기"", ""pool"": ""erpien"", ""stats"": { ""atk"": 1 } },
          { ""id"": ""vg_ot"", ""name"": ""남의 검"", ""grade"": ""희귀"", ""slot"": ""무기"", ""pool"": ""other"", ""stats"": { ""atk"": 1 } },
          { ""id"": ""vg_ot_leg"", ""name"": ""남의 목걸이"", ""grade"": ""전설"", ""slot"": ""장신구"", ""pool"": ""other"", ""stats"": { ""hp"": 1 } },
          { ""id"": ""vg_ot_leg2"", ""name"": ""남의 갑옷"", ""grade"": ""전설"", ""slot"": ""방어구"", ""pool"": ""other"", ""stats"": { ""hp"": 1 } },
          { ""id"": ""vg_ot_leg3"", ""name"": ""남의 투구"", ""grade"": ""전설"", ""slot"": ""방어구"", ""pool"": ""other"", ""stats"": { ""hp"": 1 } }
        ]";

        /// <summary>샘플 + 마을 하나 더(other — erpien 을 베낀 것) + 마을 장비.</summary>
        static GameData Data()
        {
            var d = K.Sample(); d.Add(null, null, null, null, null, EQ);
            var v2 = GameData.FromJson<VillageDef>(GameData.ToJson(d.Villages["erpien"])); v2.Id = "other"; v2.Name = "남의 마을";
            d.Villages["other"] = v2;
            return d;
        }

        [Test] public void 마을_장비는_그_마을_판에서만()
        {
            var d = Data();
            foreach (var (vil, mine, theirs) in new[] { ("erpien", "vg_er", "vg_ot"), ("other", "vg_ot", "vg_er") })
            {
                var got = new HashSet<string>();
                for (long seed = 1; seed <= 60; seed++)
                {
                    var run = Run.New(d, PARTY, seed, vil);
                    for (int i = 0; i < 5; i++) got.UnionWith(run.OfferEquip(new Dictionary<string, int> { ["희귀"] = 1, ["고급"] = 1, ["전설"] = 1 }, 2));
                }
                Assert.IsTrue(got.Contains(mine), $"{vil}: 제 마을 장비가 나온다");
                Assert.IsFalse(got.Contains(theirs), $"{vil}: 다른 마을 장비는 안 나온다");
                Assert.IsTrue(got.Any(id => GameData.IsCommonEquip(d.Equip(id))), $"{vil}: 공용 장비도 나온다");
            }
        }

        [Test] public void 칸_등급이_마을에_없으면_모든_장비에서()
        {
            var d = Data();
            var run = Run.New(d, PARTY, 3, "erpien");
            // 전설 장신구는 other 에만 — erpien 판의 이벤트가 「전설 장신구」 를 주면 빈손 대신 그것
            CollectionAssert.AreEqual(new[] { "vg_ot_leg" }, run.OfferEquipSlot("전설", "장신구"));
            // 전설 방어구는 샘플(공용)에도 있다 — 그러면 마을 장비(공용)에서만
            for (int i = 0; i < 20; i++) Assert.IsFalse(run.OfferEquipSlot("전설", "방어구").Any(id => id.StartsWith("vg_ot")), "마을에 있으면 다른 마을 것은 안 나온다");
        }

        [Test] public void pool_을_안_적은_콘텐츠는_뽑기가_그대로()
        {
            var a = K.Sample();
            var b = K.Sample(); foreach (var e in b.Equips.Values) e.Pool = "공용";
            var w = new Dictionary<string, int> { ["일반"] = 3, ["고급"] = 2, ["희귀"] = 2, ["전설"] = 1 };
            for (long seed = 1; seed <= 30; seed++)
            {
                var ra = Run.New(a, PARTY, seed); var rb = Run.New(b, PARTY, seed);
                for (int i = 0; i < 6; i++)
                {
                    CollectionAssert.AreEqual(ra.OfferEquip(w, 3), rb.OfferEquip(w, 3));
                    CollectionAssert.AreEqual(ra.OfferEquipSlot("일반", "무기"), rb.OfferEquipSlot("일반", "무기"));
                }
                Assert.AreEqual(ra.S.RngState, rb.S.RngState, "난수도 같은 만큼 썼다");
            }
        }

        [Test] public void 마을_장비_조회와_대표_장비()
        {
            var d = Data();
            CollectionAssert.AreEqual(new[] { "vg_ot", "vg_ot_leg", "vg_ot_leg2", "vg_ot_leg3" }, d.VillageGear("other"));
            CollectionAssert.AreEqual(new[] { "vg_ot_leg", "vg_ot_leg2", "vg_ot_leg3" }, d.VillageGear("other", "전설"));
            Assert.IsTrue(d.VillageGear("erpien", common: true).Contains("vg_er") && d.VillageGear("erpien", common: true).Any(id => GameData.IsCommonEquip(d.Equip(id))), "common = 공용 포함");
            // 대표 장비 — showEquips 가 없으면 전설 앞 2개, 있으면 그것
            CollectionAssert.AreEqual(new[] { "vg_ot_leg", "vg_ot_leg2" }, d.VillageShowcase("other"));
            d.Villages["other"].ShowEquips = new List<string> { "vg_ot_leg3", "vg_ot" };
            CollectionAssert.AreEqual(new[] { "vg_ot_leg3", "vg_ot" }, d.VillageShowcase("other"));
            Assert.AreEqual(0, d.VillageShowcase("없는마을").Count);
            // 나오는 마을 — 공용은 모든 마을
            CollectionAssert.AreEqual(new[] { "other" }, d.EquipVillages("vg_ot"));
            var common = d.Equips.Values.First(GameData.IsCommonEquip).Id;
            CollectionAssert.AreEqual(new[] { "erpien", "other" }, d.EquipVillages(common));
        }

        [Test] public void 검사기는_모르는_pool_과_남의_대표_장비를_오류로()
        {
            var d = Data();
            Assert.IsFalse(Validator.Check(d).Errors.Any(e => e.Contains("pool") || e.Contains("showEquips")), string.Join(" / ", Validator.Check(d).Errors));
            d.Equip("vg_er").Pool = "없는마을";
            Assert.IsTrue(Validator.Check(d).Errors.Any(e => e.Contains("vg_er") && e.Contains("pool")));
            d.Equip("vg_er").Pool = "erpien";
            d.Villages["erpien"].ShowEquips = new List<string> { "vg_ot_leg" };
            Assert.IsTrue(Validator.Check(d).Errors.Any(e => e.Contains("showEquips") && e.Contains("vg_ot_leg")));
        }

        /// <summary>콘텐츠(BZ_CONTENT — 마을 장비 패치를 넣은 사본)에서 마을마다 전설 2개 이상 · 대표 장비 2개가 그 마을 전설.</summary>
        [Test] public void 콘텐츠_마을마다_전설과_대표_장비()
        {
            string dir = Environment.GetEnvironmentVariable("BZ_CONTENT") is string bc && bc.Length > 0 ? bc : "C:/projects/bolzena-content-v2";
            if (!Directory.Exists(dir)) Assert.Ignore("콘텐츠가 없다");
            var d = GameData.FromFolder(dir);
            if (d.Equips.Values.All(GameData.IsCommonEquip)) Assert.Ignore("마을 장비 패치를 안 넣은 콘텐츠(BZ_CONTENT 에 사본)");
            foreach (var v in d.Villages.Keys)
            {
                Assert.That(d.VillageGear(v, "전설").Count, Is.GreaterThanOrEqualTo(2), v);
                var show = d.VillageShowcase(v);
                Assert.AreEqual(2, show.Count, v);
                Assert.IsTrue(show.All(id => d.Equip(id).Grade == "전설" && d.Equip(id).Pool == v), $"{v}: 대표 장비는 그 마을 전설");
            }
            Assert.IsTrue(Validator.Check(d).Ok, string.Join("\n", Validator.Check(d).Errors.Take(10)));
        }
    }
}
