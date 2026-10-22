using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 신탁 A/B(2026-10-13 신탁 전수 점검, 도구만) — 숙련 봇 · 역할 편성 판을 돌려 싸움 시작 모습(덱 · HP · 장비 · 적)을 모은 뒤,
/// 고유 카드마다 같은 싸움을 「그 카드 없음 · 기본형 · 신탁 1~5」 일곱 가지로 다시 싸운다(같은 씨앗 · 싸움 중 신탁 빛 끔).
///   oracleab [바퀴 120] [--seed 0] [--per 1200] [--hero 키;키] [--hpx 1] [--dmgx 1] [--hard] [--reps 1] [--out 파일.tsv]
///   --hpx · --dmgx: 다시 싸울 때 적 체력 · 피해에 더 곱한다(숙련 봇에게 싸움이 너무 쉬우면 카드 몫이 안 보여서). --hard: 2층 싸움 · 1층 엘리트 · 보스만.
/// 싸움 점수 U: 이기면 0.5 + 0.5 × 남은 파티 HP 비율, 지면 0.5 × 깎은 적 HP 비율(0~1).
/// 값어치 배율 = 1 + (U신탁 − U기본) / (U기본 − U없음) — 「기본형이 덱에 보탠 몫」 을 1 로 본 신탁의 몫. 덱에 그 카드가 없던 싸움은 한 장 넣어 잰다.
/// 함께: 판당 낸 횟수(봇이 그 카드를 쓰는지) · 손에 들어왔는데 한 번도 안 낸 싸움 비율 · 이긴 비율 · 평균 턴 · 검사기 배율(Validator 와 같은 셈).
/// </summary>
static class OracleAB
{
    /// <summary>OpenFight 와 같은 싸움 설정(신탁 빛 · 다음 싸움 덤은 뺌).</summary>
    static BattleSetup Capture(Battle b, Run run)
    {
        var S = run.S; var sc = run.FoeScaleOf();
        return new BattleSetup
        {
            Party = S.Party.ToList(), Deck = S.Deck.ToList(), Enemies = run.CurrentEnemies().ToList(),
            PartyHp = S.PartyHp, PartyMaxHp = S.PartyMaxHp, Gear = run.GearStats(), GearRules = run.GearRules(),
            Flash = new Dictionary<string, int>(S.Flash), Shin = new Dictionary<string, string>(S.Shin),
            EnemyHp = sc.hp, EnemyDmg = sc.dmg, Mind = S.MindBreak > 0, Gauge = Math.Min(R.GAUGE_MAX, S.Gauge),
            Elite = b.EliteFight, EnemyNature = S.EnemyNature, Floor = S.Floor + 1, Glow = null,
            Growth = S.Growth,
            CardVals = S.CardVals?.ToDictionary(kv => kv.Key, kv => new Dictionary<string, int>(kv.Value)),
            Seed = (uint)(S.Seed + S.Floor * 101 + S.Node * 7 + S.Step * 13 + (S.EventFight != null ? 555 : 0)),
        };
    }

    struct Out { public double U; public bool Win; public int Turns, Plays; public bool HeldNever; }

    /// <summary>card 를 mode(-1 없음 · 0 기본 · 1~5 신탁)로 둔 덱으로 한 번 싸운다.</summary>
    static Out Fight(GameData d, Bots bots, BattleSetup s0, string card, int mode)
    {
        var deck = s0.Deck.ToList(); var flash = new Dictionary<string, int>(s0.Flash);
        int at = deck.FindIndex(x => !GameData.IsCopy(x) && d.Card(x)?.Id == card);
        string inst;
        if (at < 0) { inst = card; if (mode >= 0) deck.Add(card); }
        else { inst = deck[at]; if (mode < 0) deck.RemoveAt(at); }
        flash.Remove(inst);
        if (mode > 0) flash[inst] = mode;
        var st = new BattleSetup
        {
            Party = s0.Party.ToList(), Deck = deck, Enemies = s0.Enemies.ToList(), PartyHp = s0.PartyHp, PartyMaxHp = s0.PartyMaxHp,
            Gear = s0.Gear, GearRules = s0.GearRules, Flash = flash, Shin = new Dictionary<string, string>(s0.Shin),
            EnemyHp = s0.EnemyHp, EnemyDmg = s0.EnemyDmg, Mind = s0.Mind, Gauge = s0.Gauge, Elite = s0.Elite, EnemyNature = s0.EnemyNature,
            Floor = s0.Floor, Glow = null, Growth = s0.Growth, CardVals = s0.CardVals?.ToDictionary(kv => kv.Key, kv => new Dictionary<string, int>(kv.Value)),
            CritDmg = s0.CritDmg, Seed = s0.Seed,
        };
        int plays = 0; bool drawn = false;
        var b = Battle.Start(d, st, null, c =>
        {
            if (c.K != "card" || mode < 0 || c.CardId == null || GameData.IsCopy(c.CardId) || d.Card(c.CardId)?.Id != card) return;
            if (c.Pile == "play") plays++;
            if (c.ToPile == "hand") drawn = true;
        });
        int t = 0;
        while (b.Over == null && t++ < 40) { bots.SmartPlay(b, 1, 3); if (b.Over == null) b.EndTurn(); }
        bool win = b.Over == "win";
        double u = win ? 0.5 + 0.5 * Math.Max(0, b.Pool.Hp) / Math.Max(1.0, b.Pool.MaxHp)
                       : 0.5 * (1 - b.Enemies.Where(e => !e.Dead).Sum(e => (double)Math.Max(0, e.Hp)) / Math.Max(1.0, b.Enemies.Sum(e => (double)e.MaxHp)));
        return new Out { U = u, Win = win, Turns = b.Turn, Plays = plays, HeldNever = drawn && plays == 0 };
    }

    static double CheckerRatio(GameData d, string card, int n)
    {
        var b = d.View(card); var o = d.View(card, n);
        double vb = Math.Max(0.05, CardValue.CardWorth(b));
        return (CardValue.CardWorth(o) / CardValue.BaseValue(o.X ? 3 : o.Cost)) / (vb / CardValue.BaseValue(b.X ? 3 : b.Cost));
    }

    public static int Run(GameData d, int rounds, int seed, int per, List<string> onlyHeroes, string outp, double hpx = 1, double dmgx = 1, bool hard = false, int reps = 1)
    {
        var t0 = DateTime.Now;
        var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role);
        var snaps = new List<(int job, int k, BattleSetup s)>[jobs.Count];
        var runWin = new bool[jobs.Count];
        var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) };
        Parallel.For(0, jobs.Count, po, j =>
        {
            var l = new List<(int, int, BattleSetup)>();
            var r = new RunBot(d).RunFull(jobs[j].party, jobs[j].seed, new SimOpts { Skilled = true, OnFight = (b, run) =>
            {
                if (hard && run.S.Floor == 0 && !run.IsBoss && !b.EliteFight) return;
                var s = Capture(b, run); s.EnemyHp *= hpx; s.EnemyDmg *= dmgx; l.Add((j, l.Count, s));
            } });
            snaps[j] = l; runWin[j] = r.Clear;
        });
        var all = snaps.SelectMany(x => x).ToList();
        Console.WriteLine($"모은 싸움 {all.Count} · 판 {jobs.Count} · 완주 {100.0 * runWin.Count(x => x) / jobs.Count:0.0}% · {(DateTime.Now - t0).TotalSeconds:0}초");

        var heroes = d.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).Where(h => onlyHeroes == null || onlyHeroes.Contains(h)).ToList();
        var sb = new System.Text.StringBuilder();
        sb.Append("hero\tcard\tn\tcost\tfights\tUnone\tUbase\tUor\tvalue\tseDelta\tdeltaU\twinBase\twinOr\tturnBase\tturnOr\tplayBase\tplayOr\theldNeverBase\theldNeverOr\tchecker\n");
        foreach (var h in heroes)
        {
            var cards = d.Cards.Values.Where(c => c.Hero == h && c.Unique && c.Oracles.Count == 5).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            // 그 사도가 든 싸움 — 고르게 per 개(씨앗 고정 차례)
            var mine = all.Where(x => x.s.Party.Contains(h)).ToList();
            int step = Math.Max(1, mine.Count / Math.Max(1, per));
            var pick0 = mine.Where((x, i) => i % step == 0).Take(per).ToList();
            // 같은 싸움을 씨앗만 바꿔 reps 번(표본 늘리기)
            var pick = Enumerable.Range(0, reps).SelectMany(r => pick0.Select(x => { if (r == 0) return x; var s2 = x.s; return (x.job, x.k, s: new BattleSetup
            {
                Party = s2.Party, Deck = s2.Deck, Enemies = s2.Enemies, PartyHp = s2.PartyHp, PartyMaxHp = s2.PartyMaxHp, Gear = s2.Gear, GearRules = s2.GearRules,
                Flash = s2.Flash, Shin = s2.Shin, EnemyHp = s2.EnemyHp, EnemyDmg = s2.EnemyDmg, Mind = s2.Mind, Gauge = s2.Gauge, Elite = s2.Elite,
                EnemyNature = s2.EnemyNature, Floor = s2.Floor, Glow = null, Growth = s2.Growth, CardVals = s2.CardVals, CritDmg = s2.CritDmg, Seed = s2.Seed + (uint)(r * 7919),
            }); })).ToList();
            int N = pick.Count, M = cards.Count * 7;
            var res = new Out[N, M];
            var tl = new System.Threading.ThreadLocal<Bots>(() => new Bots(d));
            Parallel.For(0, N * M, po, q =>
            {
                int i = q / M, m = q % M;
                res[i, m] = Fight(d, tl.Value, pick[i].s, cards[m / 7], m % 7 - 1);
            });
            for (int ci = 0; ci < cards.Count; ci++)
            {
                string c = cards[ci];
                double Mean(int mode, Func<Out, double> f) { double s = 0; for (int i = 0; i < N; i++) s += f(res[i, ci * 7 + mode + 1]); return s / Math.Max(1, N); }
                double uNone = Mean(-1, o => o.U), uBase = Mean(0, o => o.U);
                double card = uBase - uNone;
                for (int n = 1; n <= 5; n++)
                {
                    double uOr = Mean(n, o => o.U);
                    // 짝 차이(신탁 − 기본)의 표준 오차 → 배율 오차
                    double md = uOr - uBase, v = 0;
                    for (int i = 0; i < N; i++) { double x = res[i, ci * 7 + n + 1].U - res[i, ci * 7 + 1].U - md; v += x * x; }
                    double se = Math.Sqrt(v / Math.Max(1, N - 1) / Math.Max(1, N));
                    double value = card > 1e-4 ? 1 + md / card : double.NaN;
                    var cv = d.View(c, n);
                    sb.Append(string.Join("\t", h, c, n, cv.X ? "X" : cv.Cost.ToString(), N, uNone.ToString("0.0000"), uBase.ToString("0.0000"), uOr.ToString("0.0000"),
                        double.IsNaN(value) ? "nan" : value.ToString("0.000"), se.ToString("0.00000"), md.ToString("0.0000"),
                        Mean(0, o => o.Win ? 1 : 0).ToString("0.000"), Mean(n, o => o.Win ? 1 : 0).ToString("0.000"),
                        Mean(0, o => o.Turns).ToString("0.00"), Mean(n, o => o.Turns).ToString("0.00"),
                        Mean(0, o => o.Plays).ToString("0.00"), Mean(n, o => o.Plays).ToString("0.00"),
                        Mean(0, o => o.HeldNever ? 1 : 0).ToString("0.000"), Mean(n, o => o.HeldNever ? 1 : 0).ToString("0.000"),
                        CheckerRatio(d, c, n).ToString("0.00"))).Append('\n');
                }
            }
            Console.WriteLine($"  {h} — 싸움 {N} × 카드 {cards.Count} · {(DateTime.Now - t0).TotalSeconds:0}초");
            File.WriteAllText(outp, sb.ToString());
        }
        Console.WriteLine($"→ {Path.GetFullPath(outp)} · {(DateTime.Now - t0).TotalSeconds:0}초");
        return 0;
    }
}
