using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 생성 카드 불어남 점검(2026-10-13 시온 마탄 · 도구만) — 그 사도를 넣은 숙련 봇 판을 돌려, 싸움이 끝날 때 덱(뽑을 · 손 · 버린 더미)에 남은
/// 그 생성 카드 장수와 비율, 소멸 더미로 간 장수를 모은다(카제나 레노아식 탄환 — 버려도 무덤에 남으니 덱이 불어나는지).
///   magstat [판 600] --hero 시온더다크불릿 [--card 시온더다크불릿_t1] [--seed 0]
/// </summary>
static class MagStat
{
    public static int Run(GameData d, int n, int seed, string hero, string card)
    {
        var jobs = MetaSim.JobsWith(d, hero, n, seed);
        var rows = new List<(int floor, int turns, int deck, int mags, int gone)>[jobs.Count];
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, j =>
        {
            var fights = new List<(Battle b, int floor)>();
            new RunBot(d).RunFull(jobs[j].party, jobs[j].seed, new SimOpts { Skilled = true, OnFight = (b, r) => fights.Add((b, r.S.Floor + 1)) });
            bool Is(string x) => GameData.BaseId(x) == card;
            rows[j] = fights.Select(f => (f.floor, f.b.Turn, f.b.Draw.Count + f.b.Hand.Count + f.b.Discard.Count,
                f.b.Draw.Count(Is) + f.b.Hand.Count(Is) + f.b.Discard.Count(Is), f.b.Gone.Count(Is))).ToList();
        });
        var all = rows.SelectMany(x => x).ToList();
        void Line(string label, IEnumerable<(int floor, int turns, int deck, int mags, int gone)> src)
        {
            var l = src.ToList(); if (l.Count == 0) return;
            double share = l.Average(x => x.deck > 0 ? (double)x.mags / x.deck : 0);
            Console.WriteLine($"{label}: 싸움 {l.Count} · 평균 턴 {l.Average(x => x.turns):0.0} · 끝난 덱 {l.Average(x => x.deck):0.0}장 · 그 가운데 마탄 {l.Average(x => x.mags):0.0}장({100 * share:0.0}%) · 비율 최대 {100 * l.Max(x => x.deck > 0 ? (double)x.mags / x.deck : 0):0}% · 소멸 더미로 간 마탄 {l.Average(x => x.gone):0.0}장");
        }
        Line("전체", all);
        Line("1층", all.Where(x => x.floor == 1));
        Line("2층", all.Where(x => x.floor >= 2));
        Line("6턴 이상 싸움", all.Where(x => x.turns >= 6));
        return 0;
    }
}
