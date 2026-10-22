using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 사도 영구 성장(카제나판 — Prog.cs, 2026-10-21 밤 「잠재력 하나만」): 성장 없음 = 지금 판 · 잠재력 카드군 · 능력치 · 문턱형 · 판 · 프론티어 · 저장 ·
    /// 없앤 축(레벨 · 진급 · 교주 레벨 · 추억 조각 · 발현 · 어사이드)은 표 · 상태가 받지 않는다.
    /// </summary>
    public class ProgTests
    {
        static readonly string CONTENT = Environment.GetEnvironmentVariable("BZ_CONTENT") is string bc && bc.Length > 0 ? bc : "C:/projects/bolzena-content-v2";   // 교차 시험처럼 성장 표를 넣은 사본으로 돌릴 수 있게
        const string UNIQUE = "[{id:'uc', name:'다 고유', hero:'c', cost:2, type:'공격', unique:true, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}," +
                              " {id:'uc2', name:'다 고유 작은', hero:'c', cost:1, type:'공격', unique:true, fx:[{k:'dmg', ratio:0.5, target:'oneEnemy'}]}]";

        /// <summary>시험 표 — 숫자를 손으로 셀 수 있게.</summary>
        static ProgTable T() => GameData.FromJson<ProgTable>(@"{
 potential:[
  {id:'01', kind:'start', max:1},
  {id:'02', kind:'card', of:'basic', v:0.02, max:10},
  {id:'04', kind:'card', of:'unique', v:0.02, max:10, tier:3},
  {id:'05', kind:'stat', of:'hp', v:0.05, max:5, tier:3},
  {id:'06', kind:'stat', of:'role', v:0.1, max:5, tier:3},
  {id:'07', kind:'skill', to:['unique'], v:0.05, max:1, tier:7}],
 presets:{}
}");

        static ProgState One(string hero, Action<ProgHero> f)
        {
            var h = new ProgHero();
            f?.Invoke(h);
            return new ProgState { Heroes = { [hero] = h } };
        }

        [Test] public void 성장이_없으면_셋업_JSON에_prog_칸이_없고_잠재력_0은_같은_전투다()
        {
            var d = K.Data();
            var st0 = new BattleSetup { Party = new List<string> { "c" }, Enemies = new List<string> { "hitter" }, Deck = new List<string> { "hit_c", "hit_c", "hit_c", "hit_c", "hit_c" }, EnemyHp = 1, Seed = 5 };
            Assert.IsNull(BattleInput.Of(st0).SetupJson["prog"], "성장 없음 — 칸 자체가 없다(성장 전 테이프의 셋업 비교가 맞는다)");
            var pb = Prog.Build(d, T(), One("c", null), new[] { "c" });
            Assert.IsTrue(pb.Mods["c"].Empty, "잠재력 0 은 바뀌는 것이 없다(능력치 = 공개판 기본치)");
            var st1 = BattleInput.Of(st0).Setup(); st1.Prog = pb.Mods;
            Assert.IsNotNull(BattleInput.Of(st1).SetupJson["prog"]);
            var a = Battle.Start(d, st0); var b = Battle.Start(d, st1);
            for (int t = 0; t < 3; t++)
            {
                foreach (var x in new[] { a, b })
                {
                    for (int g = 0; g < 10 && x.Over == null && x.Hand.Count > 0 && x.CostOf(x.Hand[0]) <= x.Ap; g++) if (!x.PlayCard(0, 0).Ok) break;
                    if (x.Over == null) x.EndTurn();
                }
                Assert.AreEqual(a.Digest(), b.Digest(), $"{t}턴 — 같은 전투");
            }
        }

        [Test] public void 없앤_축은_표와_상태가_받지_않고_잠재력은_받는다()
        {
            // 2026-10-21 밤 사용자 결정(잠재력 하나만) — 없앤 축의 키가 표 · 상태에 남아 있으면 모르는 키로 읽기부터 실패한다(조용히 무시하지 않는다).
            foreach (var key in new[] { "level:{max:60}", "pieces:{}", "ego:[]", "aside:{}" })
                Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgTable>("{" + key + "}"), key);
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgTable>("{potential:[{id:'02', kind:'card', of:'basic', v:0.02, max:10, promo:1}]}"), "잠재력 칸의 진급 문턱(promo)");
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgTable>("{presets:{max:{lv:60, pot:{}}}}"), "측정 묶음의 레벨");
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgTable>("{potential:[{id:'07', kind:'skill', to:['unique'], v:0.05, need:'role', needX:1.3, v2:0.08}]}"), "skill 의 능력치 문턱(10-22 없앰)");
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgTable>("{heroes:{c:{'07':{name:'x', desc:'x', need:'atk', needV:1, up:{}}}}}"), "사도 칸의 능력치 문턱 덤(10-22 없앰)");
            // 칸 여는 도전 사다리 단계(tier — 10-22) — 범위 · 앞 칸보다 낮으면 안 됨
            var tt = T();
            Assert.IsEmpty(Prog.Check(K.Data(), tt));
            tt.Node("07").Tier = Tiers.MAX + 1;
            StringAssert.Contains("tier 는 0", string.Join("\n", Prog.Check(K.Data(), tt)));
            tt.Node("07").Tier = 7; tt.Node("05").Tier = 1;
            StringAssert.Contains("앞 칸 04", string.Join("\n", Prog.Check(K.Data(), tt)));
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgState>("{account:1, heroes:{}}"), "교주 레벨");
            foreach (var key in new[] { "lv:1", "promo:2", "pieces:[]", "ego:2", "aside:{id:'blade'}" })
                Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => GameData.FromJson<ProgState>("{heroes:{c:{" + key + "}}}"), "성장 상태의 " + key);
            Assert.AreEqual(1, GameData.FromJson<ProgTable>("{potential:[{id:'01', kind:'start', max:1}], heroes:{}}").Potential.Count, "잠재력 표는 받는다");
            Assert.AreEqual(1, GameData.FromJson<ProgState>("{heroes:{c:{pot:{'01':1}}}}").Heroes["c"].Pot["01"], "성장 상태의 잠재력 칸은 받는다");
        }

        [Test] public void 잠재력_고유_카드군은_그_사도의_고유_카드만_키운다()
        {
            var d = K.Data(cards: UNIQUE);
            var pb = Prog.Build(d, T(), One("c", h => h.Pot["04"] = 10), new[] { "c" });
            Assert.AreEqual(0.2, pb.Mods["c"].Unique, 1e-9);
            int Dmg(string card, Dictionary<string, ProgMods> prog)
            {
                var b = K.Fight(d, new[] { "c" }, new[] { "dummy" }, st => st.Prog = prog);
                K.Hand(b, card); b.Ap = 5;
                int h0 = K.Hp(b); K.Play(b, card);
                return h0 - K.Hp(b);
            }
            Assert.AreEqual(Dmg("uc", null) * 1.2, Dmg("uc", pb.Mods), 1.0, "고유 카드 +20%");
            Assert.AreEqual(Dmg("hit_c", null), Dmg("hit_c", pb.Mods), "기본 카드는 그대로");
            Assert.AreEqual(10, pb.Sum["c"].Pot["04"]);
            var over = Prog.Build(d, T(), One("c", h => h.Pot["04"] = 99), new[] { "c" });
            Assert.AreEqual(0.2, over.Mods["c"].Unique, 1e-9, "단계는 칸 max 로 자른다");
        }

        [Test] public void 능력치_칸은_공개판_기본치에_곱하고_문턱_덤은_없다()
        {
            var d = K.Data(cards: UNIQUE);
            var c = d.Hero("c");
            var pb = Prog.Build(d, T(), One("c", h => { h.Pot["05"] = 2; h.Pot["06"] = 4; h.Pot["07"] = 1; }), new[] { "c" });
            var m = pb.Mods["c"];
            string rs = Prog.RoleStat(c.Role);
            int baseRole = rs == "atk" ? c.Atk : c.Def;
            Assert.AreEqual(Num.Round(c.Hp * 1.1) - c.Hp, m.Base.Hp, "HP +5% × 2");
            Assert.AreEqual(Num.Round(baseRole * 1.4) - baseRole, rs == "atk" ? m.Base.Atk : m.Base.Def, "역할 능력치 +10% × 4");
            Assert.AreEqual(0.05, m.Unique, 1e-9, "07 — 능력치가 높아도 문턱 덤 없음(10-22 없앰)");
            var b = K.Fight(d, new[] { "c" }, new[] { "dummy" }, s => s.Prog = pb.Mods);
            Assert.AreEqual(0, b.Party[0].GearAdd.Atk, "성장은 장비 몫(GearAdd)이 아니라 기본치");
            Assert.AreEqual(Num.Round(c.Hp * 1.1), b.Pool.MaxHp, "파티 HP 도 성장 몫으로");
            var low = Prog.Build(d, T(), One("c", h => { h.Pot["06"] = 2; h.Pot["07"] = 1; }), new[] { "c" });
            Assert.AreEqual(0.05, low.Mods["c"].Unique, 1e-9, "능력치가 낮아도 같다");
        }

        [Test] public void 판은_시작에_HP를_옮기고_첫_선물_고유_카드를_더하고_싸움마다_성장을_건다()
        {
            var d = K.Sample();
            d.Prog = T();
            var party = new List<string> { "rico", "carrot", "sion" };
            var run = Run.New(d, party, 7);
            int max0 = run.S.PartyMaxHp, deck0 = run.S.Deck.Count;
            var st = new ProgState();
            foreach (var k in party) st.Heroes[k] = new ProgHero { Pot = { ["01"] = 1, ["05"] = 2 } };
            run.ApplyProg(Prog.Build(d, d.Prog, st, party));
            Assert.Greater(run.S.PartyMaxHp, max0, "잠재력 HP 만큼 최대 HP 가 는다");
            Assert.AreEqual(deck0 + 3, run.S.Deck.Count, "첫 선물 — 사도마다 고유 카드 1장");
            var (b, _) = run.OpenFight();
            Assert.IsNotNull(b.ProgOf);
            Assert.AreEqual(d.Hero(b.Party[0].Key).Atk, b.Party[0].Atk - b.Party[0].GearAdd.Atk, "공격은 공개판 기본치 그대로(레벨 없음)");
            var back = Run.Load(d, run.Save());
            Assert.AreEqual(run.S.Prog.Count, back.S.Prog.Count, "판 저장에 남는다");
            var sv = Battle.Load(d, b.Save());
            Assert.IsNotNull(sv.ProgOf, "전투 저장에도");
            var none = Run.New(d, party, 7); none.ApplyProg(null); none.ApplyProg(new ProgBuild());
            Assert.IsNull(none.S.Prog); Assert.AreEqual(max0, none.S.PartyMaxHp);
        }

        [Test] public void 프론티어는_떠_둔_성장으로_셋업을_다시_짓고_다르면_거절한다()
        {
            var d = K.Sample();
            d.Prog = T();
            var run = Run.New(d, new List<string> { "rico", "carrot", "sion" }, 7);
            run.S.Done = "clear";
            var slots = run.S.Party.Select(h => HeroSlot.Of(run, h)).ToList();
            var season = new FrontierSeason { Id = "test", Boss = "witch", Turns = 3, Hp = 30, Dmg = 1 };
            var prog = new ProgState();
            foreach (var k in run.S.Party) prog.Heroes[k] = new ProgHero { Pot = { ["01"] = 1, ["02"] = 3, ["04"] = 5 } };
            var b = Frontier.Start(d, slots, season, 11, prog: prog);
            Assert.IsNotNull(b.ProgOf);
            var bots = new Bots(d);
            for (int g = 0; Frontier.Over(b, season) == null && g < season.Turns + 5; g++) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
            var json = Frontier.Tape(b, season, slots).Json();
            var ok = Frontier.Verify(d, FrontierTape.Parse(json), season, slots, prog);
            Assert.IsTrue(ok.Ok, ok.Why);
            StringAssert.Contains("셋업", Frontier.Verify(d, FrontierTape.Parse(json), season, slots).Why, "성장을 빼고 지으면 셋업이 다르다");
            prog.Heroes["rico"].Pot["04"] = 10;
            Assert.IsFalse(Frontier.Verify(d, FrontierTape.Parse(json), season, slots, prog).Ok, "떠 둔 값과 다른 성장");
            Assert.IsTrue(Frontier.Verify(d, FrontierTape.Parse(json), season).Ok, "테이프만으로 다시 돌려도 같다");
            Assert.IsNull(BattleInput.Of(Frontier.Setup(d, slots, season, 11)).SetupJson["prog"], "성장 없으면 예전 셋업 그대로");
        }

        [Test] public void 콘텐츠_성장_표는_검사를_지나고_none_묶음은_바뀌는_것이_없다()
        {
            if (!Directory.Exists(CONTENT)) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(CONTENT);
            if (d.Prog == null) Assert.Ignore("콘텐츠에 성장 표가 없다");
            CollectionAssert.IsEmpty(Prog.Check(d, d.Prog));
            CollectionAssert.IsEmpty(Validator.Check(d).Errors.Where(e => e.Contains("성장 표")).ToList());
            foreach (var name in d.Prog.Presets.Keys)
                CollectionAssert.IsEmpty(Prog.CheckState(d, d.Prog, Prog.Preset(d, d.Prog, name, d.Heroes.Keys)), name);
            if (d.Prog.Presets.ContainsKey("none"))
                Assert.IsTrue(Prog.Build(d, d.Prog, Prog.Preset(d, d.Prog, "none", d.Heroes.Keys), d.Heroes.Keys).Mods.Values.All(m => m.Empty), "none = 지금 판");
            if (d.Prog.Presets.ContainsKey("max"))
                Assert.IsTrue(Prog.Build(d, d.Prog, Prog.Preset(d, d.Prog, "max", d.Heroes.Keys), d.Heroes.Keys).Mods.Values.All(m => !m.Empty), "max 는 모두 무언가 바뀐다");
        }
    }
}
