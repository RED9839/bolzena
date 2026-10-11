using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bolzena.Core;
using Newtonsoft.Json.Linq;

/// <summary>
/// 사도 빌드 검증(2026-10-10) — bz build --spec 빌드.json [--n 200] [--seed 0] [--full] [--out 파일.md] [--tsv 파일]
///   빌드마다 데이터를 새로 읽고(카드 모습 캐시가 섞이지 않게) 그 사도의 고유 카드를 「빌드 모양」 으로 고정한 뒤 숙련 봇으로 판 전체를 돈다.
///   · core 에 든 고유 카드: 신탁 번호(1~5)면 그 신탁 모습을 카드 자체로 굳힌다(신탁 고르기 없음), 0 이면 기본형 그대로 · 신탁 없음.
///   · core 에 없는 고유 카드(시동 카드 제외): 은총 · 보상에 안 나온다(빌드 밖).
///   · --full: core 카드를 처음부터 시작 덱에 넣는다(완성 덱 측정). 없으면 은총으로 모으는 보통 판.
///   · 편성: hero + party 칸(사도 id · "*" 아무나 · "role:탱커|서포터|딜러" 그 역할 무작위). 칸이 없으면 "*","*".
/// 빌드.json = { "builds": [ { "hero": "에르핀_왕도", "name": "왕도 빌드", "core": { "에르핀_왕도_u1": 2, "에르핀_왕도_u2": 1 }, "party": ["role:서포터", "*"] } ] }
///   core 를 빼면(null) 데이터 그대로(바탕 측정).
/// </summary>
static class BuildSim
{
    sealed class Spec { public string Hero, Name, Group; public Dictionary<string, int> Core; public List<string> Party; }
    sealed class Row { public Spec S; public int N, Win, F1, F2, BossN, BossT, BossW, EliteN, EliteW, FightN, FightT, Uniq, Basic, Deck; public string Err; }

    public static int Run(List<string> paths, string specFile, int n, int seed, bool full, string outMd, string tsv, int threads)
    {
        var root = JObject.Parse(File.ReadAllText(specFile));
        var specs = root["builds"].Select(b => new Spec
        {
            Hero = (string)b["hero"], Name = (string)b["name"] ?? "?", Group = (string)b["group"],
            Core = b["core"] is JObject o ? o.Properties().ToDictionary(p => p.Name, p => (int)p.Value) : null,
            Party = b["party"]?.Select(x => (string)x).ToList() ?? new List<string> { "*", "*" },
        }).ToList();
        var rows = new List<Row>();
        var t0 = DateTime.Now;
        foreach (var s in specs)
        {
            var r = One(paths, s, n, seed, full, threads);
            rows.Add(r);
            Console.WriteLine(r.Err != null ? $"{s.Hero} · {s.Name} — 오류: {r.Err}"
                : $"{s.Hero} · {s.Name} — 완주 {Pct(r.Win, r.N):0.0}% ±{Half(r.Win, r.N):0.0} · 1층 {Pct(r.F1, r.N):0.0}% · 2층 {Pct(r.F2, r.N):0.0}% · 보스 {Avg(r.BossT, r.BossN):0.0}턴 · 일반 {Avg(r.FightT, r.FightN):0.0}턴 · 보스전 승 {Pct(r.BossW, r.BossN):0.0}% · 엘리트전 승 {Pct(r.EliteW, r.EliteN):0.0}% · 덱 {(double)r.Deck / Math.Max(1, r.N):0.0}(고유 {(double)r.Uniq / Math.Max(1, r.N):0.0})");
        }
        if (tsv != null)
        {
            var sb = new StringBuilder("hero\tgroup\tbuild\tparty\tn\twin\terr\tfell1\tfell2\tbossTurns\tfightTurns\tdeck\tuniques\tbasics\terror\n");
            foreach (var r in rows)
                sb.Append(string.Join("\t", r.S.Hero, r.S.Group ?? "", r.S.Name, string.Join(",", r.S.Party), r.N, Pct(r.Win, r.N).ToString("0.0"), Half(r.Win, r.N).ToString("0.0"),
                    Pct(r.F1, r.N).ToString("0.0"), Pct(r.F2, r.N).ToString("0.0"), Avg(r.BossT, r.BossN).ToString("0.0"), Avg(r.FightT, r.FightN).ToString("0.0"),
                    ((double)r.Deck / Math.Max(1, r.N)).ToString("0.0"), ((double)r.Uniq / Math.Max(1, r.N)).ToString("0.0"), ((double)r.Basic / Math.Max(1, r.N)).ToString("0.0"), r.Err ?? "")).Append('\n');
            File.WriteAllText(tsv, sb.ToString());
            Console.WriteLine($"→ {Path.GetFullPath(tsv)}");
        }
        if (outMd != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 사도 빌드 검증").AppendLine();
            sb.AppendLine($"숙련 봇 · 판 전체 · 빌드마다 {n}판 · 씨앗 {seed}{(full ? " · 완성 덱(core 를 시작 덱에)" : " · core 만 은총으로 모음")} · {(DateTime.Now - t0).TotalSeconds:0}초").AppendLine();
            sb.AppendLine("| 사도 | 빌드 | 편성 | 판 | 완주 | 오차 | 1층 쓰러짐 | 2층 쓰러짐 | 보스 턴 | 일반 턴 | 끝 덱(고유) |").AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var r in rows)
                sb.AppendLine(r.Err != null ? $"| {r.S.Hero} | {r.S.Name} | | | 오류 {r.Err} | | | | | | |"
                    : $"| {r.S.Hero} | {r.S.Name} | {string.Join(",", r.S.Party)} | {r.N} | {Pct(r.Win, r.N):0.0}% | ±{Half(r.Win, r.N):0.0} | {Pct(r.F1, r.N):0.0}% | {Pct(r.F2, r.N):0.0}% | {Avg(r.BossT, r.BossN):0.0} | {Avg(r.FightT, r.FightN):0.0} | {(double)r.Deck / Math.Max(1, r.N):0.0}({(double)r.Uniq / Math.Max(1, r.N):0.0}) |");
            File.WriteAllText(outMd, sb.ToString());
            Console.WriteLine($"→ {Path.GetFullPath(outMd)}");
        }
        Console.WriteLine($"빌드 {rows.Count} · {(DateTime.Now - t0).TotalSeconds:0}초");
        return 0;
    }

    static double Pct(int w, int n) => n > 0 ? 100.0 * w / n : 0;
    static double Avg(int t, int n) => n > 0 ? (double)t / n : 0;
    static double Half(int w, int n) { if (n == 0) return 100; double p = (double)w / n; return 1.96 * Math.Sqrt(Math.Max(0.0025, p * (1 - p)) / n) * 100; }

    /// <summary>그 사도의 고유 카드를 빌드 모양으로 굳힌다. 틀린 카드 id · 신탁 번호면 까닭.</summary>
    static string Force(GameData d, Spec s, bool full)
    {
        var h = d.Hero(s.Hero); if (h == null) return "사도 없음";
        if (s.Core == null) return null;
        var mine = d.Cards.Values.Where(c => c.Hero == h.Id && c.Unique).ToList();
        foreach (var kv in s.Core)
        {
            var c = mine.FirstOrDefault(x => x.Id == kv.Key);
            if (c == null) return $"고유 카드 아님: {kv.Key}";
            if (kv.Value < 0 || kv.Value > c.Oracles.Count) return $"{kv.Key} 신탁 {kv.Value} 없음";
            if (kv.Value > 0)
            {
                var o = c.Oracles[kv.Value - 1];
                c.Cost = o.Cost ?? c.Cost;
                if (o.Power) c.Type = "강화";
                var tags = o.Tags.ToList();
                if (c.Tags.Contains(Tag.Unique) && !tags.Contains(Tag.Unique)) tags.Insert(0, Tag.Unique);
                c.Tags = tags; c.Fx = o.Fx;
            }
            c.Oracles = new List<OracleDef>();
        }
        foreach (var c in mine.Where(c => !s.Core.ContainsKey(c.Id) && !h.Starter.Contains(c.Id))) c.Unique = false;   // 빌드 밖 — 은총에 안 나온다
        if (full) foreach (var id in s.Core.Keys) if (!h.Starter.Contains(id)) h.Starter.Add(id);
        return null;
    }

    static Row One(List<string> paths, Spec s, int n, int seed, bool full, int threads)
    {
        var row = new Row { S = s };
        GameData d;
        try { d = GameData.FromFolders(paths); } catch (Exception e) { row.Err = e.Message; return row; }
        row.Err = Force(d, s, full); if (row.Err != null) return row;
        var all = d.Heroes.Keys.Where(k => k != s.Hero).OrderBy(x => x, StringComparer.Ordinal).ToList();
        uint st = unchecked((uint)(7771 + seed * 104729));
        int Rnd(int m) { st = unchecked(st * 1103515245u + 12345u); return (int)(st / 4294967296.0 * m); }
        var jobs = new List<(int i, List<string> party, long seed)>();
        for (int i = 0; i < n; i++)
        {
            var party = new List<string> { s.Hero };
            foreach (var slot in s.Party)
            {
                if (slot != "*" && !slot.StartsWith("role:")) { if (!party.Contains(slot) && d.Hero(slot) != null) party.Add(slot); continue; }
                var pool = all.Where(k => !party.Contains(k) && !s.Party.Contains(k) && (slot == "*" || d.Hero(k).Role == slot.Substring(5))).ToList();
                if (pool.Count == 0) pool = all.Where(k => !party.Contains(k)).ToList();
                party.Add(pool[Rnd(pool.Count)]);
            }
            if (party.Count != 3) { row.Err = "편성이 셋이 아니다: " + string.Join(",", party); return row; }
            jobs.Add((i, party, 50000 + seed * 7919 + i * 37));
        }
        var res = new SimResult[jobs.Count];
        var po = new ParallelOptions { MaxDegreeOfParallelism = threads > 0 ? threads : Math.Max(1, Environment.ProcessorCount - 1) };
        Parallel.ForEach(jobs, po, j => { res[j.i] = new RunBot(d).RunFull(j.party, j.seed, new SimOpts { Skilled = true }); });
        foreach (var r in res)
        {
            row.N++;
            if (r.Clear) row.Win++; else if (r.Floor == 0) row.F1++; else row.F2++;
            row.Deck += r.Deck; row.Uniq += r.Uniques; row.Basic += r.Basics;
            foreach (var kv in r.Kinds)
            {
                if (kv.Key.EndsWith(":boss")) { row.BossT += kv.Value.turns; row.BossN += kv.Value.n; row.BossW += kv.Value.win; }
                else if (kv.Key.EndsWith(":elite")) { row.EliteN += kv.Value.n; row.EliteW += kv.Value.win; }
                else if (kv.Key.EndsWith(":fight")) { row.FightT += kv.Value.turns; row.FightN += kv.Value.n; }
            }
        }
        return row;
    }
}
