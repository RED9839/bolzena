using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 적 난이도 통계(2026-10-10, 도구만) — role 편성 판(PartyPick.Jobs)을 돌리며 싸움마다 층 · 종류 · 적 묶음 · 턴 · 깎인 파티 HP · 쓰러짐을 모은다.
///   foestat [바퀴 200] [--seed 0] [--bot skilled] [--top 25]
///   출력: 층 · 종류별 평균 턴 · HP 손실 · 쓰러짐, 그리고 적(id)별 — 그 적이 선 싸움의 평균 턴 · HP 손실 · 쓰러짐 비율(판 수 많은 순 · 쏠림 순).
/// </summary>
static class FoeStat
{
    sealed class Rec { public int Floor; public string Kind; public string[] Foes; public int Turns; public double Lost; public bool Died; }

    public static int Run(GameData d, int rounds, int seed, bool skilled, int top)
    {
        var t0 = DateTime.Now;
        var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role);
        var all = new List<Rec>[jobs.Count];
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, j =>
        {
            var open = new List<(Battle b, int floor, string kind, int hp0)>();
            new RunBot(d).RunFull(jobs[j].party, jobs[j].seed, new SimOpts
            {
                Skilled = skilled,
                OnFight = (b, r) => open.Add((b, r.S.Floor + 1, r.S.EventFight != null ? "event" : r.IsBoss ? "boss" : r.S.Elite ? "elite" : "fight", b.Pool.Hp)),
            });
            all[j] = open.Select(o => new Rec
            {
                Floor = o.floor, Kind = o.kind, Foes = o.b.Enemies.Where(e => e.Summoner < 0).Select(e => e.Key).ToArray(), Turns = o.b.Turn,
                Lost = Math.Max(0, o.hp0 - Math.Max(0, o.b.Pool.Hp)) / (double)Math.Max(1, o.b.Pool.MaxHp), Died = o.b.Over == "lose",
            }).ToList();
        });
        var recs = all.SelectMany(x => x).ToList();
        Console.WriteLine($"foestat — role · {(skilled ? "숙련" : "초보")} 봇 · FLOOR_DMG {R.FLOOR_DMG[1]} · {R.FLOOR_DMG[2]} · {jobs.Count}판 · 싸움 {recs.Count} · {(DateTime.Now - t0).TotalSeconds:0}초");
        Console.WriteLine("\n| 층 · 종류 | 싸움 | 평균 턴 | 평균 HP 손실 | 쓰러짐(싸움당) |\n|---|---|---|---|---|");
        foreach (var g in recs.GroupBy(r => (r.Floor, r.Kind)).OrderBy(g => g.Key.Floor).ThenBy(g => g.Key.Kind))
            Console.WriteLine($"| {g.Key.Floor}층 {g.Key.Kind} | {g.Count()} | {g.Average(r => r.Turns):0.00} | {100 * g.Average(r => r.Lost):0.0}% | {100.0 * g.Count(r => r.Died) / g.Count():0.00}% |");
        // 적별 — 그 적이 선 싸움(층 · 종류별로 따로)
        var rows = recs.SelectMany(r => r.Foes.Distinct().Select(f => (f, r))).GroupBy(x => (x.r.Floor, x.r.Kind, x.f))
            .Select(g => (g.Key, n: g.Count(), turns: g.Average(x => x.r.Turns), lost: g.Average(x => x.r.Lost), died: g.Count(x => x.r.Died) / (double)g.Count()))
            .Where(x => x.n >= 50).ToList();
        foreach (var kind in new[] { "fight", "elite", "boss" })
            foreach (var fl in new[] { 1, 2 })
            {
                var rs = rows.Where(x => x.Key.Kind == kind && x.Key.Floor == fl).ToList();
                if (rs.Count == 0) continue;
                double mt = rs.Average(x => x.turns), ml = rs.Average(x => x.lost);
                Console.WriteLine($"\n### {fl}층 {kind} — 적 {rs.Count}종 · 평균 턴 {mt:0.00} · HP 손실 {100 * ml:0.0}%\n\n| 적 | 싸움 | 턴 | HP 손실 | 쓰러짐 | 판정 |\n|---|---|---|---|---|---|");
                foreach (var x in rs.OrderByDescending(x => x.lost).Take(top))
                {
                    string tag = x.lost > ml * 1.35 ? "어려움" : x.lost < ml * 0.65 ? "쉬움" : "";
                    Console.WriteLine($"| {x.Key.f} | {x.n} | {x.turns:0.00} | {100 * x.lost:0.0}% | {100 * x.died:0.0}% | {tag} |");
                }
            }
        return 0;
    }
}
