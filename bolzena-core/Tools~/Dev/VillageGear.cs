using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 마을 장비 측정(2026-10-21 — bolzena-unity/Docs/마을_장비_설계.md §5).
///   vilgear [셋 판 300] [--solo 판 60] [--seed 0] [--threads 8] [--out 파일.md]
///     마을을 정해(SimOpts.Village) 숙련 봇 · 역할 편성 판을 마을마다 돌려 완주율을 견주고, 사도마다 「혼자」(그 사도 + 무작위 둘) 판을 마을마다 돌려
///     사도 × 마을 표(사도 평균과의 차이)를 낸다. 판이 끝날 때 낀 장비 가운데 그 마을 판에 안 나오는 것(이벤트 빈칸 대체)도 센다.
/// </summary>
static class VillageGear
{
    public static int Run(GameData data, List<string> rest, Func<string, string, string> opt)
    {
        int n = rest.Count > 0 && int.TryParse(rest[0], out var x) ? x : 300;
        int solo = int.Parse(opt("solo", "60")), seed = int.Parse(opt("seed", "0")), threads = int.Parse(opt("threads", "8"));
        string outp = opt("out", null);
        var vils = data.Villages.Keys.OrderBy(v => v, StringComparer.Ordinal).ToList();
        var po = new ParallelOptions { MaxDegreeOfParallelism = threads };
        int foreign = 0, gearN = 0;
        var worn = new ConcurrentDictionary<string, int[]>();   // 장비 → [판 끝에 낀 판, 그 가운데 완주]
        var t0 = DateTime.Now;
        // 판 하나 — 마을을 정해 숙련 봇으로. 끝날 때 낀 장비 가운데 다른 마을 것
        bool One(List<string> party, long s, string v)
        {
            int f = 0, g = 0; var ids = new HashSet<string>();
            var r = new RunBot(data).RunFull(party, s, new SimOpts
            {
                Skilled = true, Village = v,
                OnEnd = run => { foreach (var id in run.S.Gear.Values.SelectMany(gg => gg.Values)) { g++; ids.Add(id); if (!GameData.EquipIn(data.Equip(id), v)) f++; } },
            });
            System.Threading.Interlocked.Add(ref foreign, f); System.Threading.Interlocked.Add(ref gearN, g);
            foreach (var id in ids) { var a = worn.GetOrAdd(id, _ => new int[2]); System.Threading.Interlocked.Increment(ref a[0]); if (r.Clear) System.Threading.Interlocked.Increment(ref a[1]); }
            return r.Clear;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"# 마을 장비 측정 — 콘텐츠 {data.ContentHash ?? "-"} · 규칙 {Fingerprint.Rules()}").AppendLine();
        // ① 숙련 봇 · 역할 편성 셋 — 마을마다 같은 편성 · 씨앗(마을만 다르다)
        var jobs = PartyPick.Jobs(data, (n + 5) / 6, seed, PartyPick.Role).Take(n).ToList();
        var team = new Dictionary<string, double>();
        foreach (var v in vils)
        {
            var win = new bool[jobs.Count];
            Parallel.ForEach(jobs, po, j => win[j.i] = One(j.party, j.seed, v));
            team[v] = 100.0 * win.Count(b => b) / Math.Max(1, win.Length);
            Console.WriteLine($"  셋 {data.Villages[v].Name} {team[v]:0.0}% ({win.Length}판)");
        }
        double tAvg = team.Values.Average();
        sb.AppendLine($"## 숙련 봇 · 역할 편성 셋 — 마을마다 {jobs.Count}판(같은 편성 · 씨앗)").AppendLine();
        sb.AppendLine("| 마을 | 완주 | 평균과 차이 |").AppendLine("|---|---|---|");
        foreach (var v in vils) sb.AppendLine($"| {data.Villages[v].Name} | {team[v]:0.0}% | {team[v] - tAvg:+0.0;-0.0}%p |");
        sb.AppendLine($"| 평균 | {tAvg:0.0}% | 폭 {team.Values.Max() - team.Values.Min():0.0}%p |").AppendLine();
        // ② 혼자 — 사도마다 그 사도 + 무작위 둘, 마을마다 같은 편성 · 씨앗
        if (solo > 0)
        {
            var heroes = data.Heroes.Keys.OrderBy(h => h, StringComparer.Ordinal).ToList();
            var cell = new ConcurrentDictionary<(string h, string v), double>();
            var cells = heroes.SelectMany(h => vils.Select(v => (h, v))).ToList();
            Parallel.ForEach(cells, po, c =>
            {
                var js = MetaSim.JobsWith(data, c.h, solo, seed);
                int w = js.Count(j => One(j.party, j.seed, c.v));
                cell[c] = 100.0 * w / Math.Max(1, js.Count);
            });
            sb.AppendLine($"## 혼자 — 사도 + 무작위 둘, 사도 · 마을마다 {solo}판(±{100 * 1.96 * Math.Sqrt(0.25 / solo):0}%p 오차)").AppendLine();
            sb.AppendLine("| 사도 | 평균 | " + string.Join(" | ", vils.Select(v => data.Villages[v].Name)) + " | 유리한 마을 |").AppendLine("|---|---|" + string.Concat(vils.Select(_ => "---|")) + "---|");
            foreach (var h in heroes)
            {
                double avg = vils.Average(v => cell[(h, v)]);
                var best = vils.OrderByDescending(v => cell[(h, v)]).First();
                string mark = cell[(h, best)] - avg >= 10 ? $"{data.Villages[best].Name} {cell[(h, best)] - avg:+0}" : "-";
                sb.AppendLine($"| {data.Hero(h).Name} | {avg:0.0}% | " + string.Join(" | ", vils.Select(v => $"{cell[(h, v)] - avg:+0;-0;0}")) + $" | {mark} |");
            }
            sb.AppendLine().AppendLine("| 마을 | 혼자 평균 |").AppendLine("|---|---|");
            foreach (var v in vils) sb.AppendLine($"| {data.Villages[v].Name} | {heroes.Average(h => cell[(h, v)]):0.0}% |");
            sb.AppendLine();
        }
        // 장비마다 판 끝에 낀 판의 완주율(편성 · 마을이 섞인 대략 — 센 장비 찾기용)
        sb.AppendLine("## 장비 — 판 끝에 낀 판의 완주율(대략, 센 장비 찾기)").AppendLine().AppendLine("| 장비 | 등급 · 칸 | 마을 | 낀 판 | 완주 |").AppendLine("|---|---|---|---|---|");
        foreach (var kv in worn.Where(kv => kv.Value[0] >= 30).OrderByDescending(kv => (double)kv.Value[1] / kv.Value[0]))
        {
            var e = data.Equip(kv.Key);
            sb.AppendLine($"| {e.Name} | {e.Grade} {e.Slot} | {(GameData.IsCommonEquip(e) ? "공용" : data.Villages.TryGetValue(e.Pool, out var pv) ? pv.Name : e.Pool)} | {kv.Value[0]} | {100.0 * kv.Value[1] / kv.Value[0]:0.0}% |");
        }
        sb.AppendLine();
        sb.AppendLine($"판 끝에 낀 장비 {gearN}개 가운데 그 마을 판에 안 나오는 것 {foreign}개(이벤트가 칸 · 등급을 정해 마을에 없을 때 모든 장비에서) · {(DateTime.Now - t0).TotalSeconds:0}초");
        var md = sb.ToString();
        if (outp != null) { File.WriteAllText(outp, md); Console.WriteLine($"→ {Path.GetFullPath(outp)}"); }
        Console.WriteLine(md);
        return 0;
    }
}
