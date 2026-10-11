using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>보스 클론의 고학년(BossUlt) — 예고 → 사용 · 격파 · 기절로 끊기 · 효과 변환.</summary>
    public class BossUltTests
    {
        // z: 1성 사도 — 고학년 「큰 일격」(적 전체 180% · 적 전체 약화 2 · 사기 1 · AP +1)
        // s: 3성 지원 사도 — 고학년 「든든한 벽」(실드 · 반격 2 · 적 1명 기절) — 피해 없음
        const string HEROES = @"[
 {id:'z', name:'제트', role:'딜러', row:'back', hp:600, atk:150, def:20, crit:0, star:1,
  ult:{name:'큰 일격', cost:100, fx:[{k:'dmg', ratio:1.8, target:'allEnemies'}, {k:'status', id:'약화', v:2, target:'allEnemies'}, {k:'status', id:'사기', v:1}, {k:'ap', v:1}]}},
 {id:'s', name:'에스', role:'서포터', row:'mid', hp:800, atk:80, def:40, crit:0, star:3,
  ult:{name:'든든한 벽', cost:100, fx:[{k:'shield', ratio:3.0}, {k:'status', id:'반격', v:2}, {k:'status', id:'기절', v:1, target:'oneEnemy'}]}}
]";
        const string ENEMIES = @"[
 {id:'cz', name:'제트 (클론)', hp:5000, boss:true, clone:'z', tough:3, intents:[{t:'attack', v:10, rush:0}]},
 {id:'cs', name:'에스 (클론)', hp:5000, boss:true, clone:'s', tough:3, intents:[{t:'attack', v:10, rush:0}]},
 {id:'nz', name:'보스 아닌 클론', hp:5000, clone:'z', tough:3, intents:[{t:'attack', v:10, rush:0}]}
]";
        const string CARDS = "[{id:'brk', name:'깨기', hero:'a', cost:0, type:'공격', fx:[{k:'tough', v:9, target:'oneEnemy'}]}, {id:'stun', name:'기절시키기', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'기절', v:1, target:'oneEnemy'}]}]";

        /// <summary>z 의 1층 고학년 피해(전체 공격 V × 2).</summary>
        static int U => Num.Round(Num.Round(BossUlt.RAW[1] * BossUlt.NOW_X) / R.FOE_ALL_X) * 2;

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: ENEMIES);

        [Test] public void 효과를_적_쪽으로_뒤집는다()
        {
            var p = BossUlt.Plan(D(), "z", 1);
            Assert.AreEqual("큰 일격", p.Name);
            Assert.IsNull(p.Warn, "예고 없음(2026-10-09)"); Assert.AreEqual("ult", p.Use.T); Assert.IsFalse(p.Use.Brk, "격파로 끊기지 않는다");
            var t = p.Use.Then;
            var hit = t.Single(x => x.T == "attackAll");
            Assert.AreEqual(Num.Round(Num.Round(BossUlt.RAW[1] * BossUlt.NOW_X) / R.FOE_ALL_X), hit.V, "1층 기준 × 비율 1(1.8/1.8) × NOW_X ÷ 전체 공격 2");
            Assert.AreEqual(U, p.Raw);
            Assert.IsTrue(t.Any(x => x.T == "debuff" && x.Id == "약화" && x.V == 2 + BossUlt.DEBUFF_PLUS), "적 디버프 → 파티 디버프(원작 값 + DEBUFF_PLUS)");
            Assert.IsTrue(t.Any(x => x.T == "buff" && x.Id == "사기" && x.V == 1 && x.All), "아군 버프 → 적 전체 버프");
            CollectionAssert.Contains(p.Dropped, "AP", "사도 자원은 뺀다");
            Assert.AreEqual(Num.Round(Num.Round(BossUlt.RAW[2] * BossUlt.NOW_X) / R.FOE_ALL_X), BossUlt.Plan(D(), "z", 2).Use.Then.Single(x => x.T == "attackAll").V, "2층 기준");

            var s = BossUlt.Plan(D(), "s", 2);
            Assert.AreEqual(Num.Round(Num.Round(BossUlt.RAW[2] * BossUlt.SUPPORT_HIT * BossUlt.NOW_X) / R.FOE_ALL_X), s.Use.Then.Single(x => x.T == "attackAll").V, "피해 없는 고학년도 전체 공격을 붙인다");
            Assert.IsTrue(s.Use.Then.Any(x => x.T == "block" && x.V == BossUlt.BLOCK[2]), "실드 → 보스 방어");
            Assert.IsTrue(s.Use.Then.Any(x => x.T == "buff" && x.Id == "피해 감소"), "적에게 없는 버프(반격) → 피해 감소");
            Assert.IsTrue(s.Use.Then.Any(x => x.T == "jam" && x.V == 1), "기절 → 파티 다음 턴 AP -1");
        }

        [Test] public void 층은_마을_보스_줄에서_없으면_성급으로()
        {
            var d = D();
            Assert.AreEqual(1, BossUlt.Tier(d, "cz"), "1성 → 1층");
            Assert.AreEqual(2, BossUlt.Tier(d, "cs"), "3성 → 2층");
            var v = K.Data(heroes: HEROES, cards: CARDS, enemies: ENEMIES, villages: @"[{id:'tv', name:'시험 마을', race:'요정', floors:[
 {name:'1층', land:'시험', pools:[[['nz']]], elites:[['nz']], boss:['cs']},
 {name:'2층', land:'시험', pools:[[['nz']]], elites:[['nz']], boss:['cz']}]}]");
            Assert.AreEqual(1, BossUlt.Tier(v, "cs"), "마을 1층 보스");
            Assert.AreEqual(2, BossUlt.Tier(v, "cz"), "마을 2층 보스");
            Assert.AreEqual(2, BossUlt.Tier(v, GameData.CloneId("s", "cz")), "빌린 몸은 몸이 선 층");
        }

        [Test] public void 전투가_층을_알면_그_층_값을_쓴다()
        {
            // 매 판 보스를 새로 뽑고 1층에 1~2성이 없으면 3성이 1층에 선다 — 그때도 1층 값(사용자 2026-10-06)
            var d = D();
            Assert.AreEqual(1, BossUlt.PlanFor(d, d.Enemy("cs"), 1).Tier, "3성이어도 1층에 서면 1층");
            Assert.AreEqual(2, BossUlt.PlanFor(d, d.Enemy("cz"), 2).Tier, "1성이어도 2층에 서면 2층");
            Assert.AreEqual(2, BossUlt.PlanFor(d, d.Enemy("cs")).Tier, "층을 모르면 예전처럼(성급)");
            var b = K.Fight(d, new[] { "a" }, new[] { "cs" }, st => st.Floor = 1);
            b.EndTurn();
            var e = K.E(b);
            Assert.IsTrue(BossUlt.IsUlt(e.Intent));
            Assert.AreEqual(BossUlt.Plan(d, "s", 1).Raw, b.UltHit(e), "3성 클론이 1층 전투면 1층 기준 피해");
            Assert.AreEqual(1, Battle.Load(d, b.Save()).Floor, "저장 왕복 — 층");
        }

        [Test] public void 둘째_턴에_예고_없이_바로_쓴다()
        {
            // 2026-10-09 사용자: 끊기 없음 — 예고 턴 없이 그 차례의 수로 바로 쓴다
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" }, cues: cues);
            var e = K.E(b);
            Assert.AreEqual("attack", e.Intent.T, "첫 턴은 평소 수");
            b.EndTurn();
            Assert.AreEqual(2, b.Turn);
            Assert.AreEqual("ult", e.Intent.T, "둘째 턴 — 고학년을 바로");
            Assert.AreEqual(0, b.RushOf(e), "고학년은 즉시 행동으로 당겨지지 않는다");
            Assert.AreEqual(U, b.IntentHit(e));
            Assert.AreEqual(U, b.UltHit(e));
            Assert.IsFalse(cues.Any(c => c.K == "foeUltWarn"), "예고 쪽지 없음");
            StringAssert.DoesNotContain("끊김", new CardText(b.Data).Intent(e.Intent));
            cues.Clear();
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            var ks = cues.Select(c => c.K).ToList();
            int iu = ks.IndexOf("foeUlt"), ia = ks.FindIndex(iu, k => k == "act"), ih = ks.IndexOf("foeUltHit"), ihurt = ks.FindIndex(ih, k => k == "hurt"), ie = ks.IndexOf("foeUltEnd");
            Assert.IsTrue(iu >= 0 && iu < ia && ia < ih && ih < ihurt && ihurt < ie, "foeUlt → act → foeUltHit → hurt → foeUltEnd: " + string.Join(",", ks));
            Assert.AreEqual("ult", cues[ia].T); Assert.AreEqual(Side.Enemy, cues[iu].Side);
            Assert.Less(b.Pool.Hp, hp0, "파티가 맞았다");
            Assert.AreEqual(2 + BossUlt.DEBUFF_PLUS, b.St(b.Pool, "약화"), "파티 약화 2 + DEBUFF_PLUS");
            Assert.AreEqual(1, b.St(e, "사기"), "보스 사기 1");
            Assert.AreEqual("attack", e.Intent.T, "셋째 턴 — 평소 수로 돌아온다");
            b.EndTurn(); b.EndTurn(); b.EndTurn();
            Assert.AreEqual(6, b.Turn);
            Assert.IsTrue(BossUlt.IsUlt(e.Intent), "그 뒤 EVERY(4)턴마다 다시");
        }

        [Test] public void 격파해도_기절해도_끊기지_않고_그_턴만_쉰다()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" }, cues: cues);
            var e = K.E(b);
            b.EndTurn();
            Assert.AreEqual("ult", e.Intent.T);
            K.Hand(b, "brk"); K.Play(b, "brk", 0);
            Assert.IsTrue(e.Broken);
            Assert.IsFalse(cues.Any(c => c.K == "foeUltCut"), "끊김 없음");
            Assert.AreEqual(0, b.St(e, "취약"), "끊는 보상 없음");
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            Assert.IsFalse(cues.Any(c => c.K == "foeUlt")); Assert.AreEqual(hp0, b.Pool.Hp, "격파된 보스는 쉰다");

            cues.Clear();
            var b2 = K.Fight(D(), new[] { "a" }, new[] { "cz" }, cues: cues);
            var e2 = K.E(b2);
            b2.EndTurn();
            K.Hand(b2, "stun"); K.Play(b2, "stun", 0);
            Assert.IsTrue(e2.Sealed && !e2.Broken);
            b2.EndTurn();
            Assert.IsFalse(cues.Any(c => c.K == "foeUltCut"), "기절도 끊김 쪽지 없음");
        }

        [Test] public void 보스가_아니거나_끄면_없다()
        {
            var b = K.Fight(D(), new[] { "a" }, new[] { "nz" });
            b.EndTurn();
            Assert.IsFalse(BossUlt.IsUlt(K.E(b).Intent), "보스가 아닌 클론은 고학년을 안 쓴다");
            BossUlt.On = false;
            try
            {
                var b2 = K.Fight(D(), new[] { "a" }, new[] { "cz" });
                b2.EndTurn();
                Assert.IsFalse(BossUlt.IsUlt(K.E(b2).Intent));
            }
            finally { BossUlt.On = true; }
        }

        [Test] public void 클론_둘이면_둘째는_한_턴_늦게()
        {
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz", "cs" });
            b.EndTurn();
            Assert.IsTrue(BossUlt.IsUlt(K.E(b, 0).Intent)); Assert.IsFalse(BossUlt.IsUlt(K.E(b, 1).Intent));
            b.EndTurn();
            Assert.IsTrue(BossUlt.IsUlt(K.E(b, 1).Intent) && K.E(b, 1).Intent.T == "ult", "둘째 클론은 셋째 턴에 바로");
        }

        [Test] public void 저장하고_불러도_이어진다()
        {
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" });
            b.EndTurn();
            Assert.AreEqual("ult", K.E(b).Intent.T, "둘째 턴 — 고학년");
            var back = Battle.Load(b.Data, b.Save());
            Assert.AreEqual("ult", K.E(back).Intent.T, "불러도 이번 차례 고학년");
            Assert.AreEqual(2 + BossUlt.EVERY, K.E(back).UltAt, "다음 고학년 턴도 이어진다");
            back.EndTurn();
            Assert.AreEqual("attack", K.E(back).Intent.T, "그다음 턴은 평소 수");
        }

        [Test] public void 클론_전용_사도는_목록_밖에서_보스만_선다()
        {
            // 2026-10-11 엘다인 20명 전환 — 플레이어 목록에서 뺀 사도는 cloneHeroes 로 이름 · 종족 · 성격 · 성급 · 고학년만 남아 보스 클론이 된다
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bz_clonehero_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "all.json"), @"{
 cloneHeroes:[{id:'z', name:'제트', race:'요정', nature:'순수', role:'딜러', star:1, ult:{name:'큰 일격', cost:100, fx:[{k:'dmg', ratio:1.8, target:'allEnemies'}]}}],
 enemies:[{id:'cz', name:'제트 (클론)', hp:5000, boss:true, clone:'z', nature:'순수', tough:5, intents:[{t:'attack', v:10}]},
          {id:'mob', name:'졸개', hp:100, tough:3, intents:[{t:'attack', v:10}]}],
 villages:[{id:'tv', name:'시험 마을', race:'요정', floors:[{name:'1층', land:'시험', pools:[[['mob']]], elites:[['mob']], boss:['cz']}]}]}");
                var d = GameData.FromFolder(dir);
                Assert.IsFalse(d.Heroes.ContainsKey("z"), "플레이어 사도 목록에 없다");
                Assert.IsNull(d.Hero("z"));
                Assert.AreEqual("제트", d.CloneHero("z").Name, "보스 클론은 클론 전용 사도를 찾는다");
                CollectionAssert.Contains(Run.CloneCandidates(d, "tv", "순수", 0), "z", "보스 후보");
                Assert.IsTrue(BossUlt.Has(d, d.Enemy("cz")), "클론 전용 사도의 고학년을 보스가 쓴다");
                Assert.AreEqual("큰 일격", BossUlt.PlanFor(d, d.Enemy("cz"), 1).Name);
                Assert.IsFalse(Validator.Check(d).Errors.Any(e => e.Contains("클론")), string.Join(" / ", Validator.Check(d).Errors));
                // 되살린 사도를 cloneHeroes 에 남겨 두면 검사기가 잡는다
                d.Heroes["z"] = d.CloneHeroes["z"];
                Assert.IsTrue(Validator.Check(d).Errors.Any(e => e.Contains("플레이어 사도(heroes)에도 있다")));
            }
            finally { System.IO.Directory.Delete(dir, true); }
        }
    }
}
