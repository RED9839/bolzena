using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 플레이 기록(v2) 받기 · 모으기 — bolzena-unity/Docs/플레이 기록.md.
///   bz records pull [--out C:\projects\bolzena-records]     KV(bolzena-records)의 v2/ 기록을 받아 온다(wrangler 로그인 필요 · 읽기만)
///   bz records [폴더] [--sim 200] [--bot skilled|basic] [--seed 0] [--out 파일.md]   모아 보기 + 같은 편성 봇 판과 나란히
/// </summary>
static class Records
{
    public const string NS = "c4ddda9070294a9784c366daa02300b7";   // bolzena-unity/Web/wrangler.toml 의 RECORDS
    public const string DIR = @"C:\projects\bolzena-records";

    // ── 받기 ──────────────────────────────────────────────────────
    static string Wrangler(string args)
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c npx wrangler " + args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, StandardOutputEncoding = Encoding.UTF8 };
        using var p = Process.Start(psi);
        var o = p.StandardOutput.ReadToEnd(); var e = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new Exception("wrangler " + args + " 실패: " + e.Trim());
        return o;
    }

    /// <summary>KV → 폴더. 이미 받은 키는 건너뛴다. extra — 「--local --persist-to 폴더」 따위(로컬 시험).</summary>
    public static int Pull(string outDir, string extra = "--remote")
    {
        Directory.CreateDirectory(outDir);
        var keys = Newtonsoft.Json.Linq.JArray.Parse(Wrangler($"kv key list --namespace-id {NS} --prefix v2/ {extra}")).Select(k => (string)k["name"]).ToList();
        string FileOf(string k) => Path.Combine(outDir, k.Replace('/', '_') + ".json");
        var need = keys.Where(k => !File.Exists(FileOf(k))).ToList();
        int got = 0;
        for (int i = 0; i < need.Count; i += 100)
        {
            var part = need.Skip(i).Take(100).ToList();
            var tmp = Path.Combine(Path.GetTempPath(), $"bz-keys-{Guid.NewGuid():N}.json");
            File.WriteAllText(tmp, Newtonsoft.Json.JsonConvert.SerializeObject(part));
            try
            {
                var o = Wrangler($"kv bulk get \"{tmp}\" --namespace-id {NS} {extra}");
                int at = o.IndexOf('{');
                var map = Newtonsoft.Json.Linq.JObject.Parse(o.Substring(Math.Max(0, at)));
                foreach (var kv in map)
                {
                    var v = kv.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object && kv.Value["value"] != null ? kv.Value["value"] : kv.Value;
                    var text = v.Type == Newtonsoft.Json.Linq.JTokenType.String ? (string)v : v.ToString(Newtonsoft.Json.Formatting.None);
                    if (string.IsNullOrEmpty(text)) continue;
                    File.WriteAllText(FileOf(kv.Key), text); got++;
                }
            }
            finally { File.Delete(tmp); }
        }
        Console.WriteLine($"기록 {keys.Count}개 가운데 새로 {got}개를 받았습니다 → {outDir}");
        return got;
    }

    // ── 읽기 ──────────────────────────────────────────────────────
    /// <summary>폴더의 기록 — 한 판(익명 id · 씨앗)에 여러 장이면(메인으로 → 이어서 끝) 끝난 쪽 · 늦은 쪽 하나.</summary>
    public static List<PlayRecord> Load(string dir)
    {
        var all = new List<PlayRecord>();
        if (!Directory.Exists(dir)) return all;
        foreach (var f in Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            try { var r = RunRecord.Parse(File.ReadAllText(f)); if (r?.Kind == RunRecord.KIND && r.V == RunRecord.VERSION) all.Add(r); } catch { }
        }
        int Rank(PlayRecord r) => r.Result == "quit" ? 0 : 1;
        return all.GroupBy(r => (r.Anon, r.Seed)).Select(g => g.OrderByDescending(Rank).ThenByDescending(r => r.Ended ?? "", StringComparer.Ordinal).ThenByDescending(r => r.Step).First()).ToList();
    }

    /// <summary>봇 판 n 개의 기록 — 편성은 사람 기록에서 돌려 쓰고(없으면 무작위), 씨앗은 따로.</summary>
    public static List<PlayRecord> Bots(GameData d, int n, int seed, bool skilled, List<PlayRecord> human)
    {
        var parties = human.Where(r => r.Party != null && r.Party.Count == 3 && r.Party.All(p => d.Hero(p.Id) != null)).Select(r => (r.Party.Select(p => p.Id).ToList(), r.Village)).ToList();
        var jobs = PartyPick.Jobs(d, Math.Max(1, (n + d.Heroes.Count - 1) / Math.Max(1, d.Heroes.Count)), seed, PartyPick.Random, null, null).Take(n).ToList();
        var outs = new ConcurrentBag<PlayRecord>();
        Parallel.For(0, Math.Min(n, jobs.Count), new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var party = parties.Count > 0 ? parties[i % parties.Count].Item1 : jobs[i].party;
            string vil = parties.Count > 0 ? parties[i % parties.Count].Village : null;
            new RunBot(d).RunFull(party, 90000 + seed * 7919 + i, new SimOpts
            {
                Skilled = skilled, Village = vil,
                OnEnd = run => outs.Add(RunRecord.Of(run, new RecordMeta { Anon = "b0b0b0b0b0b0b0b0" + i.ToString("x16"), Os = "bot", Result = run.S.Done == "clear" ? "win" : "lose" }, int.MaxValue)),
            });
        });
        return outs.ToList();
    }

    // ── 모으기 ────────────────────────────────────────────────────
    sealed class Stat
    {
        public int Runs, Win, Lose, Abandon, Quit;
        public List<int> Mins = new();
        public Dictionary<string, int[]> Hero = new();           // 사도 → [판, 완주]
        public Dictionary<string, int> DiedAt = new();           // 「n층 종류」
        public Dictionary<string, int> KilledBy = new();         // 쓰러뜨린 적
        public Dictionary<string, int[]> Offer = new();          // 갈래|대상 → [제시, 고름]
        public Dictionary<string, int[]> CardUse = new();        // 카드 → [덱에 있던 싸움, 낸 수]
        public Dictionary<string, (int n, int dmg)> Crash = new();   // 무너진 턴의 적 수 「적|수」 → (횟수, 피해 합)
        public int CrashTurns, Turns;
        public Dictionary<string, (int n, int turns, int lost, int max)> Fights = new();   // 종류 → 싸움 · 턴 · 잃은 HP · 최대 HP
        public double FinishPct => Win + Lose + Abandon > 0 ? 100.0 * Win / (Win + Lose + Abandon) : 0;
    }

    static void Inc(Dictionary<string, int> m, string k, int v = 1) => m[k] = (m.TryGetValue(k, out var x) ? x : 0) + v;
    static void Inc2(Dictionary<string, int[]> m, string k, int a, int b) { if (!m.TryGetValue(k, out var x)) m[k] = x = new int[2]; x[0] += a; x[1] += b; }
    static string CardKey(string c)
    {
        if (c == null) return null;
        if (c.StartsWith("+")) c = c.Substring(1);
        int o = c.IndexOf("/o", StringComparison.Ordinal); if (o > 0) c = c.Substring(0, o);
        return GameData.BaseId(c);
    }

    static Stat Gather(List<PlayRecord> rs)
    {
        var s = new Stat();
        foreach (var r in rs)
        {
            s.Runs++;
            switch (r.Result) { case "win": s.Win++; break; case "lose": s.Lose++; break; case "abandon": s.Abandon++; break; default: s.Quit++; break; }
            if (r.Result != "quit" && r.Min > 0) s.Mins.Add(r.Min);
            bool done = r.Result != "quit";
            if (done) foreach (var p in r.Party ?? new()) Inc2(s.Hero, p.Id, 1, r.Result == "win" ? 1 : 0);
            if (r.Death != null) { Inc(s.DiedAt, $"{r.Death.F}층 {Ko(r.Death.K)}"); foreach (var e in r.Death.Foes.Distinct()) Inc(s.KilledBy, e); }
            foreach (var p in r.Picks ?? new())
            {
                if (p.K == "shop" || p.K == "event" || p.K == "rest") continue;
                if (p.K == "oracle" || p.K == "train" || p.K == "ev.flash")
                {   // 신탁 — 번호마다
                    foreach (var o in (p.Offer ?? new()).Distinct()) Inc2(s.Offer, "신탁|" + o, 1, p.Pick == o ? 1 : 0);
                    Inc2(s.Offer, "신탁|(넘김)", 1, p.Pick == null ? 1 : 0);
                    continue;
                }
                string g = p.K switch { "grade" => "진급", "grace" => "은총", "equip" => "아티팩트", "ev.card" => "카드", "copy" => "복제", "buy" => "구매", "remove" or "gradeRemove" or "ev.remove" => "빼기", _ => null };
                if (g == null) continue;
                if (p.Offer != null && p.Offer.Count > 0) { foreach (var o in p.Offer.Distinct()) Inc2(s.Offer, g + "|" + o, 1, p.Pick == o ? 1 : 0); Inc2(s.Offer, g + "|(넘김)", 1, p.Pick == null ? 1 : 0); }
                else if (p.Pick != null) Inc2(s.Offer, g + "|" + p.Pick, 0, 1);
            }
            foreach (var f in r.Fights ?? new())
            {
                var k = Ko(f.Kind);
                s.Fights.TryGetValue(k, out var fx);
                s.Fights[k] = (fx.n + 1, fx.turns + f.Turns, fx.lost + Math.Max(0, f.HpBefore - f.HpAfter), fx.max + f.HpMax);
                if (f.Log == null) continue;
                var played = new Dictionary<string, int>();
                foreach (var t in f.Log)
                {
                    s.Turns++;
                    foreach (var c in t.C) { var ck = CardKey(c); if (ck != null) played[ck] = (played.TryGetValue(ck, out var x) ? x : 0) + 1; }
                    if (f.HpMax > 0 && t.D >= f.HpMax * 0.25)
                    {
                        s.CrashTurns++;
                        foreach (var a in t.F ?? new()) { var key = a.E + "|" + a.A; s.Crash.TryGetValue(key, out var cv); s.Crash[key] = (cv.n + 1, cv.dmg + a.D); }
                    }
                }
                foreach (var c in f.Deck ?? played.Keys.ToList()) Inc2(s.CardUse, c, 1, played.TryGetValue(c, out var pn) ? pn : 0);
            }
        }
        return s;
    }

    static string Ko(string kind) => kind switch { "boss" => "보스", "elite" => "엘리트", "event" => "이벤트", "fight" => "일반", _ => kind ?? "?" };

    public static string Report(GameData d, List<PlayRecord> human, List<PlayRecord> bot, string botLabel)
    {
        var H = Gather(human); var B = Gather(bot);
        var sb = new StringBuilder();
        string Pct(double a, double b) => b > 0 ? $"{100 * a / b:0.0}%" : "-";
        string Name(string id) => d.Hero(id)?.Name ?? d.Card(id)?.Name ?? d.Enemy(id)?.Name ?? d.Equip(id)?.Name ?? id;
        sb.AppendLine($"# 플레이 기록 — 사람 {H.Runs}판 · {botLabel} {B.Runs}판").AppendLine();
        sb.AppendLine("## 판").AppendLine().AppendLine("| | 사람 | 봇 |").AppendLine("|---|---|---|");
        sb.AppendLine($"| 판 수(끝난 · 메인으로) | {H.Runs - H.Quit} · {H.Quit} | {B.Runs - B.Quit} · {B.Quit} |");
        sb.AppendLine($"| 완주율(승리 / 승리+패배+포기) | **{H.FinishPct:0.0}%** | **{B.FinishPct:0.0}%** |");
        sb.AppendLine($"| 승리 · 패배 · 포기 | {H.Win} · {H.Lose} · {H.Abandon} | {B.Win} · {B.Lose} · {B.Abandon} |");
        sb.AppendLine($"| 평균 판 시간 | {(H.Mins.Count > 0 ? $"{H.Mins.Average():0}분 (중앙 {H.Mins.OrderBy(x => x).ElementAt(H.Mins.Count / 2)}분)" : "-")} | - |");
        foreach (var k in new[] { "일반", "엘리트", "보스", "이벤트" })
        {
            H.Fights.TryGetValue(k, out var h); B.Fights.TryGetValue(k, out var b);
            string Cell((int n, int turns, int lost, int max) x) => x.n > 0 ? $"{x.n}번 · {(double)x.turns / x.n:0.0}턴 · 잃은 HP {Pct(x.lost, x.max)}" : "-";
            sb.AppendLine($"| {k} 싸움 | {Cell(h)} | {Cell(b)} |");
        }
        sb.AppendLine().AppendLine("## 층별 쓰러짐(끝난 판 가운데)").AppendLine().AppendLine("| 곳 | 사람 | 봇 |").AppendLine("|---|---|---|");
        foreach (var k in H.DiedAt.Keys.Union(B.DiedAt.Keys).OrderBy(x => x, StringComparer.Ordinal))
            sb.AppendLine($"| {k} | {Pct(H.DiedAt.GetValueOrDefault(k), H.Runs - H.Quit)} ({H.DiedAt.GetValueOrDefault(k)}) | {Pct(B.DiedAt.GetValueOrDefault(k), B.Runs - B.Quit)} ({B.DiedAt.GetValueOrDefault(k)}) |");
        sb.AppendLine().AppendLine("## 파티를 쓰러뜨린 적").AppendLine().AppendLine("| 적 | 사람 | 봇 |").AppendLine("|---|---|---|");
        foreach (var k in H.KilledBy.Keys.Union(B.KilledBy.Keys).OrderByDescending(k => H.KilledBy.GetValueOrDefault(k) * 1000 + B.KilledBy.GetValueOrDefault(k)).Take(25))
            sb.AppendLine($"| {Name(k)} | {H.KilledBy.GetValueOrDefault(k)} | {B.KilledBy.GetValueOrDefault(k)} |");
        sb.AppendLine().AppendLine($"## 무너지는 턴 — 받은 피해 ≥ 최대 HP 25% 인 턴의 적 수(사람 {H.CrashTurns}/{H.Turns}턴 · 봇 {B.CrashTurns}/{B.Turns}턴)").AppendLine();
        sb.AppendLine("| 적 · 수 | 사람 횟수 · 평균 피해 | 봇 횟수 · 평균 피해 |").AppendLine("|---|---|---|");
        foreach (var k in H.Crash.Keys.Union(B.Crash.Keys).OrderByDescending(k => (H.Crash.TryGetValue(k, out var x) ? x.dmg : 0) * 10 + (B.Crash.TryGetValue(k, out var y) ? y.dmg : 0)).Take(25))
        {
            var p = k.Split('|');
            string C(Dictionary<string, (int n, int dmg)> m) => m.TryGetValue(k, out var v) ? $"{v.n} · {(double)v.dmg / v.n:0}" : "-";
            sb.AppendLine($"| {Name(p[0])} · {p[1]} | {C(H.Crash)} | {C(B.Crash)} |");
        }
        sb.AppendLine().AppendLine("## 사도 — 편성률 · 완주율").AppendLine().AppendLine("| 사도 | 사람 편성 | 사람 완주 | 봇 편성 | 봇 완주 |").AppendLine("|---|---|---|---|---|");
        int hn = H.Runs - H.Quit, bn = B.Runs - B.Quit;
        foreach (var k in H.Hero.Keys.Union(B.Hero.Keys).OrderByDescending(k => H.Hero.TryGetValue(k, out var x) ? x[0] : 0).ThenBy(k => k, StringComparer.Ordinal))
        {
            var h = H.Hero.GetValueOrDefault(k) ?? new int[2]; var b = B.Hero.GetValueOrDefault(k) ?? new int[2];
            sb.AppendLine($"| {Name(k)} | {Pct(h[0], hn)} ({h[0]}) | {Pct(h[1], h[0])} | {Pct(b[0], bn)} ({b[0]}) | {Pct(b[1], b[0])} |");
        }
        void Offers(string title, string group, int top)
        {
            sb.AppendLine().AppendLine($"## {title} — 제시 · 고른 비율").AppendLine().AppendLine("| 것 | 사람 제시 · 고름 | 봇 제시 · 고름 |").AppendLine("|---|---|---|");
            var keys = H.Offer.Keys.Union(B.Offer.Keys).Where(k => k.StartsWith(group + "|", StringComparison.Ordinal))
                .OrderByDescending(k => H.Offer.TryGetValue(k, out var x) ? x[0] + x[1] : 0).ThenByDescending(k => B.Offer.TryGetValue(k, out var y) ? y[0] : 0).Take(top);
            foreach (var k in keys)
            {
                string C(Dictionary<string, int[]> m) => m.TryGetValue(k, out var v) ? (v[0] > 0 ? $"{v[0]} · {Pct(v[1], v[0])}" : $"고름 {v[1]}") : "-";
                var id = k.Substring(group.Length + 1);
                string nm = group == "진급" ? Grades.Describe(new GradeChoice { Kind = id }).name ?? id : group == "신탁" ? (id == "(넘김)" ? id : $"신탁 {id}") : Name(id);
                sb.AppendLine($"| {nm} | {C(H.Offer)} | {C(B.Offer)} |");
            }
        }
        Offers("진급 보상", "진급", 20); Offers("신탁(번호)", "신탁", 10); Offers("은총(고유 카드)", "은총", 30); Offers("이벤트 카드", "카드", 20);
        Offers("전리품 아티팩트", "아티팩트", 20); Offers("상점 구매", "구매", 20); Offers("빼기", "빼기", 20); Offers("보스 복제", "복제", 15);
        sb.AppendLine().AppendLine("## 카드 실제 사용률 — 낸 수 / 덱에 있던 싸움(사람 기준 많은 순)").AppendLine().AppendLine("| 카드 | 사람 싸움 · 싸움당 | 봇 싸움 · 싸움당 |").AppendLine("|---|---|---|");
        foreach (var k in H.CardUse.Keys.Union(B.CardUse.Keys).OrderByDescending(k => H.CardUse.TryGetValue(k, out var x) ? x[0] : 0).ThenByDescending(k => B.CardUse.TryGetValue(k, out var y) ? y[0] : 0).Take(60))
        {
            string C(Dictionary<string, int[]> m) => m.TryGetValue(k, out var v) && v[0] > 0 ? $"{v[0]} · {(double)v[1] / v[0]:0.00}" : "-";
            sb.AppendLine($"| {Name(k)} | {C(H.CardUse)} | {C(B.CardUse)} |");
        }
        return sb.ToString();
    }
}
