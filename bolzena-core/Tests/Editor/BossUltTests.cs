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
        static int U => Num.Round(BossUlt.RAW[1] / R.FOE_ALL_X) * 2;

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: ENEMIES);

        [Test] public void 효과를_적_쪽으로_뒤집는다()
        {
            var p = BossUlt.Plan(D(), "z", 1);
            Assert.AreEqual("큰 일격", p.Name);
            Assert.AreEqual("charge", p.Warn.T); Assert.AreSame(p.Use, p.Warn.Next); Assert.IsTrue(p.Warn.Brk && p.Use.Brk, "예고 · 사용 모두 격파로 끊긴다");
            var t = p.Use.Then;
            var hit = t.Single(x => x.T == "attackAll");
            Assert.AreEqual(Num.Round(BossUlt.RAW[1] / R.FOE_ALL_X), hit.V, "1층 기준 × 비율 1(1.8/1.8) ÷ 전체 공격 2");
            Assert.AreEqual(Num.Round(BossUlt.RAW[1] / R.FOE_ALL_X) * 2, p.Raw);
            Assert.IsTrue(t.Any(x => x.T == "debuff" && x.Id == "약화" && x.V == 2 + BossUlt.DEBUFF_PLUS), "적 디버프 → 파티 디버프(원작 값 + DEBUFF_PLUS)");
            Assert.IsTrue(t.Any(x => x.T == "buff" && x.Id == "사기" && x.V == 1 && x.All), "아군 버프 → 적 전체 버프");
            CollectionAssert.Contains(p.Dropped, "AP", "사도 자원은 뺀다");
            Assert.AreEqual(Num.Round(BossUlt.RAW[2] / R.FOE_ALL_X), BossUlt.Plan(D(), "z", 2).Use.Then.Single(x => x.T == "attackAll").V, "2층 기준");

            var s = BossUlt.Plan(D(), "s", 2);
            Assert.AreEqual(Num.Round(Num.Round(BossUlt.RAW[2] * BossUlt.SUPPORT_HIT) / R.FOE_ALL_X), s.Use.Then.Single(x => x.T == "attackAll").V, "피해 없는 고학년도 전체 공격을 붙인다");
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

        [Test] public void 둘째_턴에_예고하고_셋째_턴에_쓴다()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" }, cues: cues);
            var e = K.E(b);
            Assert.AreEqual("attack", e.Intent.T, "첫 턴은 평소 수");
            b.EndTurn();
            Assert.AreEqual(2, b.Turn);
            Assert.IsTrue(BossUlt.IsUlt(e.Intent) && e.Intent.T == "charge", "둘째 턴 — 고학년 예고");
            Assert.AreEqual(0, b.RushOf(e), "예고는 즉시 행동으로 당겨지지 않는다");
            Assert.IsNull(b.IntentHit(e), "예고 턴에는 치지 않는다");
            Assert.AreEqual(U, b.UltHit(e), "예고 피해 = 다음 턴 피해");
            var warn = cues.Single(c => c.K == "foeUltWarn");
            Assert.AreEqual("z", warn.Hero); Assert.AreEqual("큰 일격", warn.Name); Assert.AreEqual(U, warn.V);
            StringAssert.Contains("고학년 예고", new CardText(b.Data).Intent(e.Intent));

            b.EndTurn();
            Assert.AreEqual("ult", e.Intent.T, "셋째 턴 — 고학년");
            Assert.AreEqual(0, b.RushOf(e));
            Assert.AreEqual(U, b.IntentHit(e));
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
            Assert.AreEqual("attack", e.Intent.T, "넷째 턴 — 평소 수로 돌아온다");
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual(6, b.Turn);
            Assert.IsTrue(BossUlt.IsUlt(e.Intent), "그 뒤 EVERY(4)턴마다 다시 예고");
        }

        [Test] public void 예고_중에_격파하면_끊긴다()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" }, cues: cues);
            var e = K.E(b);
            b.EndTurn();
            Assert.IsTrue(BossUlt.IsUlt(e.Intent));
            K.Hand(b, "brk"); K.Play(b, "brk", 0);
            Assert.IsTrue(e.Broken);
            var cut = cues.Single(c => c.K == "foeUltCut");
            Assert.AreEqual("격파", cut.Label); Assert.AreEqual("큰 일격", cut.Name);
            Assert.IsNull(e.Intent, "예고가 지워졌다");
            Assert.AreEqual(BossUlt.CUT_VULN, b.St(e, "취약"), "끊는 보상 — 보스 취약");
            b.EndTurn();
            Assert.IsFalse(BossUlt.IsUlt(e.Intent), "다음 턴에 고학년을 쓰지 않는다");
            b.EndTurn();
            Assert.IsFalse(cues.Any(c => c.K == "foeUlt"), "고학년은 끝내 나가지 않았다");
        }

        [Test] public void 사용_턴에_격파해도_기절시켜도_끊긴다()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" }, cues: cues);
            var e = K.E(b);
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual("ult", e.Intent.T);
            K.Hand(b, "brk"); K.Play(b, "brk", 0);
            Assert.AreEqual(1, cues.Count(c => c.K == "foeUltCut"));
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
            Assert.AreEqual("기절", cues.Single(c => c.K == "foeUltCut").Label, "기절도 예고를 끊는다");
            Assert.IsFalse(BossUlt.IsUlt(e2.Intent));
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
            Assert.IsTrue(BossUlt.IsUlt(K.E(b, 1).Intent) && K.E(b, 1).Intent.T == "charge");
        }

        [Test] public void 저장하고_불러도_이어진다()
        {
            var b = K.Fight(D(), new[] { "a" }, new[] { "cz" });
            b.EndTurn();
            var back = Battle.Load(b.Data, b.Save());
            back.EndTurn();
            Assert.AreEqual("ult", K.E(back).Intent.T);
            Assert.AreEqual(2 + BossUlt.EVERY, K.E(back).UltAt, "다음 예고 턴도 이어진다");
        }
    }
}
