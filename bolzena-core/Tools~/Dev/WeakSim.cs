using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 약점 속성 의존도 시험(2026-10-09, 도구만) — role 편성 판(PartyPick.Jobs)을 그대로 쓰되, 판의 적 속성(씨앗으로 정해짐)에 대해
/// 약점 사도 수를 강제한다(같은 역할 안에서 바꿔 끼움). 「약점」 = 그 적 속성을 이기는 성격(R.WeakTo) 또는 공명(늘 약점으로 친다).
///   weaksim [바퀴 400] [--seed 0] [--bot skilled] [--weak role|0|1|2|3] [--floordmg a,b]
///   --weak role 이면 편성을 바꾸지 않고, 약점 사도 수별로 나눠 보인다.
/// </summary>
static class WeakSim
{
    public static bool IsWeak(GameData d, string hero, string nat)
    {
        var n = d.Hero(hero)?.Nature;
        return n == "공명" || (n != null && R.WeakTo(nat).Contains(n));
    }

    public static int Run(GameData d, int rounds, int seed, bool skilled, string weak)
    {
        var t0 = DateTime.Now;
        var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role);
        var heroes = d.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var list = new List<(List<string> party, long seed, string nat, int w)>();
        int moved = 0, short_ = 0;
        foreach (var (i, party0, s) in jobs)
        {
            var nat = Bolzena.Core.Run.NatureBySeed(d, Bolzena.Core.Run.VillageBySeed(d, s), s);
            var party = party0.ToList();
            if (weak.StartsWith("pair:") || weak.StartsWith("solo:"))
            {
                // 키워드 빌드 짝 측정 — pair:소비,짝1,짝2 = 그 셋 그대로 · solo:소비,짝1,짝2 = 소비 + 짝과 같은 역할의 무작위 사도(짝 · 소비 빼고)
                var ids = weak.Substring(5).Split(',').ToList(); var rng = new Rng(s * 31 + 9);
                if (weak.StartsWith("pair:")) party = ids.Take(3).ToList();
                else
                {
                    party = new List<string> { ids[0] };
                    foreach (var p in ids.Skip(1).Take(2))
                    {
                        char r = PartyPick.RoleOf(d, p);
                        var pool = heroes.Where(k => !ids.Contains(k) && !party.Contains(k) && PartyPick.RoleOf(d, k) == r).ToList();
                        party.Add(pool[rng.Int(pool.Count)]);
                    }
                }
            }
            else if (weak.StartsWith("comp:"))
            {
                // 편성 강제(약점과 상관없이) — 「comp:DDT」 이면 딜러 둘 · 탱커 하나. 자리마다 그 역할이 아니면 그 역할 사도로 바꿔 끼운다
                var want = weak.Substring(5).ToCharArray(); var rng = new Rng(s * 31 + 5);
                var need = want.ToList();
                var keep = new List<string>();
                foreach (var k in party) { char r = PartyPick.RoleOf(d, k); if (need.Contains(r)) { need.Remove(r); keep.Add(k); } }
                foreach (var r in need)
                {
                    var pool = heroes.Where(k => !keep.Contains(k) && PartyPick.RoleOf(d, k) == r).ToList();
                    keep.Add(pool[rng.Int(pool.Count)]); moved++;
                }
                party = keep;
            }
            else if (weak == "D" || weak == "S" || weak == "T")
            {
                // 그 역할 한 명만 약점 · 나머지 둘은 약점 아님 — 그 역할 자리가 없으면 탱커 아닌(탱커면 아무) 자리 하나를 그 역할로 바꾼다
                char want = weak[0]; var rng = new Rng(s * 31 + 11);
                int slot = party.FindIndex(k => PartyPick.RoleOf(d, k) == want);
                if (slot < 0) slot = party.FindIndex(k => PartyPick.RoleOf(d, k) != 'T'); if (slot < 0) slot = 0;
                for (int j = 0; j < 3; j++)
                {
                    bool wantW = j == slot; char role = j == slot ? want : PartyPick.RoleOf(d, party[j]);
                    if (IsWeak(d, party[j], nat) == wantW && PartyPick.RoleOf(d, party[j]) == role) continue;
                    var pool = heroes.Where(k => !party.Contains(k) && PartyPick.RoleOf(d, k) == role && IsWeak(d, k, nat) == wantW).ToList();
                    if (pool.Count == 0) pool = heroes.Where(k => !party.Contains(k) && IsWeak(d, k, nat) == wantW).ToList();
                    if (pool.Count == 0) { short_++; continue; }
                    party[j] = pool[rng.Int(pool.Count)]; moved++;
                }
            }
            else if (weak != "role")
            {
                int want = int.Parse(weak);
                var rng = new Rng(s * 31 + 7);
                // 자리마다 약점 여부를 정한다 — 앞에서부터 want 자리는 약점, 나머지는 약점 아님. 이미 맞으면 그대로 둔다.
                var isW = party.Select(k => IsWeak(d, k, nat)).ToList();
                // 이미 약점인 자리를 먼저 약점 자리로 쓰고, 모자라거나 넘치면 같은 역할에서 바꿔 끼운다
                var order = Enumerable.Range(0, 3).OrderByDescending(j => isW[j]).ToList();
                var wantW = new bool[3]; for (int k = 0; k < want; k++) wantW[order[k]] = true;
                for (int j = 0; j < 3; j++)
                {
                    if (isW[j] == wantW[j]) continue;
                    char role = PartyPick.RoleOf(d, party[j]);
                    var pool = heroes.Where(k => !party.Contains(k) && PartyPick.RoleOf(d, k) == role && IsWeak(d, k, nat) == wantW[j]).ToList();
                    if (pool.Count == 0) pool = heroes.Where(k => !party.Contains(k) && IsWeak(d, k, nat) == wantW[j]).ToList();
                    if (pool.Count == 0) { short_++; continue; }
                    party[j] = pool[rng.Int(pool.Count)]; moved++;
                }
            }
            list.Add((party, s, nat, party.Count(k => IsWeak(d, k, nat))));
        }
        var res = new SimResult[list.Count];
        long inc = 0, grd = 0, hl = 0; var turns = new Dictionary<string, (long n, long t)>();
        Parallel.For(0, list.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, j =>
        {
            var fights = new List<Battle>(); var kinds = new List<string>();
            res[j] = new RunBot(d).RunFull(list[j].party, list[j].seed, new SimOpts { Skilled = skilled, OnFight = (b, r) => { fights.Add(b); kinds.Add($"{r.S.Floor + 1}|{(r.S.EventFight != null ? "event" : r.IsBoss ? "boss" : r.S.Elite ? "elite" : "fight")}"); } });
            lock (turns) for (int q = 0; q < fights.Count; q++) { turns.TryGetValue(kinds[q], out var tv); turns[kinds[q]] = (tv.n + 1, tv.t + fights[q].Turn); }   // 층 · 종류별 평균 턴(엘다인 4차 난이도 — 편성별 싸움 길이)
            System.Threading.Interlocked.Add(ref inc, fights.Sum(b => (long)b.SumIncoming));
            System.Threading.Interlocked.Add(ref grd, fights.Sum(b => (long)b.SumGuarded));
            System.Threading.Interlocked.Add(ref hl, fights.Sum(b => (long)b.SumHealed));
        });
        string Row(IEnumerable<int> idx, string label)
        {
            var ix = idx.ToList(); int n = ix.Count; if (n == 0) return $"| {label} | 0 | - | - | - | - |";
            double P(int c) => 100.0 * c / n;
            int win = ix.Count(j => res[j].Clear);
            int f1 = ix.Count(j => !res[j].Clear && res[j].Floor == 0), f2 = ix.Count(j => !res[j].Clear && res[j].Floor >= 1);
            string W(int fl, string where) => P(ix.Count(j => !res[j].Clear && Math.Min(1, res[j].Floor) == fl && res[j].Where == where)).ToString("0.0");
            double half = 1.96 * Math.Sqrt(P(win) / 100 * (1 - P(win) / 100) / n) * 100;
            return $"| {label} | {n} | **{P(win):0.0}%** ±{half:0.0} | {P(f1):0.0} ({W(0, "fight")} · {W(0, "elite")} · {W(0, "boss")}) | {P(f2):0.0} ({W(1, "fight")} · {W(1, "elite")} · {W(1, "boss")}) |";
        }
        Console.WriteLine($"weaksim — 편성 {(weak == "role" ? "role 그대로" : weak == "D" ? "딜러 1명만 약점(" : weak == "S" ? "서포터 1명만 약점(" : weak == "T" ? "탱커 1명만 약점(" : $"약점 {weak}명 강제(")}{(weak == "role" ? "" : $"바꿔 끼운 자리 {moved} · 못 맞춘 자리 {short_})")} · {(skilled ? "숙련" : "초보")} 봇 · FLOOR_DMG {R.FLOOR_DMG[1]} · {R.FLOOR_DMG[2]} · {list.Count}판 · {(DateTime.Now - t0).TotalSeconds:0}초");
        Console.WriteLine($"생존 몫 — 적 공격이 든 양 가운데 방어 · 실드가 막은 {(inc > 0 ? 100.0 * grd / inc : 0):0.0}% · 회복이 채운 {(inc > 0 ? 100.0 * hl / inc : 0):0.0}%(판당 든 피해 {(double)inc / Math.Max(1, list.Count):0})");
        string T(string k) => turns.TryGetValue(k, out var tv) && tv.n > 0 ? $"{(double)tv.t / tv.n:0.00}" : "-";
        Console.WriteLine($"평균 턴 — 1층 일반 {T("1|fight")} · 엘리트 {T("1|elite")} · 보스 {T("1|boss")} | 2층 일반 {T("2|fight")} · 엘리트 {T("2|elite")} · 보스 {T("2|boss")}");
        Console.WriteLine("| 편성 | 판 | 완주 | 1층 쓰러짐 (일반 · 엘리트 · 보스) | 2층 쓰러짐 (일반 · 엘리트 · 보스) |");
        Console.WriteLine("|---|---|---|---|---|");
        Console.WriteLine(Row(Enumerable.Range(0, list.Count), "전체"));
        for (int w = 0; w <= 3; w++) Console.WriteLine(Row(Enumerable.Range(0, list.Count).Where(j => list[j].w == w), $"약점 {w}명"));
        foreach (var nat in R.FOE_NATURES) Console.WriteLine(Row(Enumerable.Range(0, list.Count).Where(j => list[j].nat == nat), $"적 속성 {nat}"));
        return 0;
    }
}
