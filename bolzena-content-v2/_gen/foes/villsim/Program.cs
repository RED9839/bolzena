// 적 리뉴얼 마을 고정 측정(2026-10-10). 엔진은 안 고친다 — 코어 소스를 그대로 컴파일해 부른다.
//   villsim sim <데이터> <바퀴> [--seed 0] [--village id] [--weak role|0|dealer|3] [--out 줄.tsv]
//       숙련 봇 · 역할 편성(PartyPick.Jobs role). --village 면 모든 판이 그 마을(속성은 씨앗으로 그 마을에서 고를 수 있는 것).
//       --weak 0 = 약점 사도 0명 · dealer = 딜러만 약점(탱커 · 서포터는 약점 아님) · 3 = 셋 다 — 같은 역할 안에서 바꿔 끼운다(WeakSim 과 같은 방식).
//   villsim dex <데이터> <적 id …>   — 정보창 글(CardText.Intent · CardText.Enemy)
using System; using System.Collections.Generic; using System.Collections.Concurrent; using System.Linq; using System.IO; using System.Threading.Tasks;
using Bolzena.Core;
static class P
{
    static List<string> rest;
    static string Opt(string n, string d = null) { int i = rest.IndexOf("--" + n); if (i < 0) return d; var v = rest[i + 1]; rest.RemoveRange(i, 2); return v; }
    static bool IsWeak(GameData d, string h, string nat) { var n = d.Hero(h)?.Nature; return n == "공명" || (n != null && R.WeakTo(nat).Contains(n)); }
    static int Main(string[] a)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        rest = a.ToList(); string cmd = rest[0];
        var d = GameData.FromFolders(rest[1].Split(';').ToList());
        if (cmd == "check")
        {
            var v = Validator.Check(d);
            Console.WriteLine($"오류 {v.Errors.Count} · 주의 {v.Warnings.Count}");
            foreach (var e in v.Errors) Console.WriteLine("오류 " + e);
            foreach (var w in v.Warnings) if (rest.Count < 3 || rest.Skip(2).Any(k => w.Contains(k))) Console.WriteLine("주의 " + w);
            return v.Errors.Count == 0 ? 0 : 1;
        }
        if (cmd == "dex")
        {
            var tx = new CardText(d);
            foreach (var id in rest.Skip(2))
            {
                var e = d.Enemy(id); if (e == null) { Console.WriteLine("없음 " + id); continue; }
                Console.WriteLine($"■ {e.Name} ({id}) · HP {e.Hp} · 강인도 {e.Tough} · {e.Row}");
                Console.WriteLine("  " + e.Blurb);
                foreach (var it in e.Intents) Console.WriteLine($"  수(w{it.W}) {it.Say}: {tx.Intent(it)}");
                if (e.Phase != null) foreach (var it in e.Phase.Intents) Console.WriteLine($"  [HP {e.Phase.At * 100}% 아래] 수(w{it.W}) {it.Say}: {tx.Intent(it)}");
                foreach (var l in tx.Enemy(e).Split('\n')) if (l.Length > 0) Console.WriteLine("  · " + l);
                foreach (var p in e.Passives) Console.WriteLine($"  [정보창 패시브] {p.Name}: {tx.FoePassive(p)}");
                Console.WriteLine();
            }
            return 0;
        }
        int rounds = int.Parse(rest[2]); rest.RemoveRange(0, 3);
        int seed = int.Parse(Opt("seed", "0")); string vil = Opt("village"); string weak = Opt("weak", "role"); string outp = Opt("out");
        var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role);
        var heroes = d.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var list = new List<(List<string> party, long seed)>();
        foreach (var (i, p0, s) in jobs)
        {
            var v = vil ?? Run.VillageBySeed(d, s); var nat = Run.NatureBySeed(d, v, s);
            var party = p0.ToList();
            if (weak != "role")
            {
                var rng = new Rng(s * 31 + 7);
                bool[] want = new bool[3];
                if (weak == "dealer") for (int j = 0; j < 3; j++) want[j] = PartyPick.RoleOf(d, party[j]) == 'D';
                else { int n = int.Parse(weak); var isW = party.Select(k => IsWeak(d, k, nat)).ToList(); var order = Enumerable.Range(0, 3).OrderByDescending(j => isW[j]).ToList(); for (int k = 0; k < n; k++) want[order[k]] = true; }
                for (int j = 0; j < 3; j++)
                {
                    if (IsWeak(d, party[j], nat) == want[j]) continue;
                    char role = PartyPick.RoleOf(d, party[j]);
                    var pool = heroes.Where(k => !party.Contains(k) && PartyPick.RoleOf(d, k) == role && IsWeak(d, k, nat) == want[j]).ToList();
                    if (pool.Count == 0) continue;
                    party[j] = pool[rng.Int(pool.Count)];
                }
            }
            list.Add((party, s));
        }
        var agg = new ConcurrentDictionary<string, double[]>(); // n, lose, hpLost, turns, breaks
        var res = new SimResult[list.Count]; var crashes = new List<string>();
        var t0 = DateTime.Now;
        Parallel.For(0, list.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, j =>
        {
            var fights = new List<(Battle b, string key, int hp0, int max)>();
            try {
            res[j] = new RunBot(d).RunFull(list[j].party, list[j].seed, new SimOpts
            {
                Skilled = true, Village = vil, OnFight = (b, run) =>
                {
                    string kind = run.S.EventFight != null ? "event" : run.IsBoss ? "boss" : run.S.Elite ? "elite" : "fight";
                    string line = kind == "boss" ? "boss" : string.Join("+", b.Enemies.Select(e => e.Key));
                    fights.Add((b, $"{run.S.Village}\t{run.S.Floor + 1}\t{kind}\t{line}", run.S.PartyHp, Math.Max(1, run.S.PartyMaxHp)));
                }
            });
            } catch (Exception ex) {   // 엔진 예외 — 그 판은 빼고 센다(어느 판인지 남김)
                lock (crashes) crashes.Add($"{string.Join(",", list[j].party)} 씨앗 {list[j].seed} · {ex.GetType().Name}: {ex.Message} @ {ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}");
                return;
            }
            foreach (var f in fights)
            {
                bool lose = f.b.Over != "win"; double lost = Math.Max(0, f.hp0 - Math.Max(0, f.b.Pool.Hp)) * 100.0 / f.max;
                agg.AddOrUpdate(f.key, _ => new double[] { 1, lose ? 1 : 0, lost, f.b.Turn, f.b.Breaks }, (_, x) => { lock (x) { x[0]++; x[1] += lose ? 1 : 0; x[2] += lost; x[3] += f.b.Turn; x[4] += f.b.Breaks; } return x; });
            }
        });
        res = res.Where(r => r != null).ToArray();
        if (crashes.Count > 0) { Console.WriteLine($"엔진 예외 {crashes.Count}판(뺌) — 첫 판: {crashes[0]}"); }
        int N = res.Length; double Pc(int c) => 100.0 * c / N;
        int win = res.Count(r => r.Clear); int f1 = res.Count(r => !r.Clear && r.Floor == 0), f2 = res.Count(r => !r.Clear && r.Floor >= 1);
        string W(int fl, string w) => Pc(res.Count(r => !r.Clear && Math.Min(1, r.Floor) == fl && r.Where == w)).ToString("0.0");
        double half = 1.96 * Math.Sqrt(Pc(win) / 100 * (1 - Pc(win) / 100) / N) * 100;
        (double t, double br, int n) K(string k)
        {
            int n = 0, t = 0, b = 0;
            foreach (var r in res) foreach (var kv in r.Kinds) if (kv.Key.EndsWith(":" + k)) { n += kv.Value.n; t += kv.Value.turns; b += r.BreaksBy.TryGetValue(kv.Key, out var x) ? x : 0; }
            return (n > 0 ? (double)t / n : 0, n > 0 ? (double)b / n : 0, n);
        }
        var kf = K("fight"); var ke = K("elite"); var kb = K("boss");
        Console.WriteLine($"villsim — 마을 {vil ?? "씨앗대로"} · 약점 {weak} · {N}판 · {(DateTime.Now - t0).TotalSeconds:0}초 · FLOOR_DMG {R.FLOOR_DMG[1]}/{R.FLOOR_DMG[2]} · TOUGH Min {R.TOUGH.Min}/{R.TOUGH.MinElite}/{R.TOUGH.MinBoss} Area {R.TOUGH.Area} · WEAK_DMG {R.WEAK_DMG}");
        Console.WriteLine($"완주 {Pc(win):0.0}% ±{half:0.0} · 1층 쓰러짐 {Pc(f1):0.0} (일반 {W(0, "fight")} · 엘리트 {W(0, "elite")} · 보스 {W(0, "boss")}) · 2층 {Pc(f2):0.0} (일반 {W(1, "fight")} · 엘리트 {W(1, "elite")} · 보스 {W(1, "boss")})");
        Console.WriteLine($"평균 턴 — 일반 {kf.t:0.00} · 엘리트 {ke.t:0.00} · 보스 {kb.t:0.00} | 싸움당 격파 — 일반 {kf.br:0.00} · 엘리트 {ke.br:0.00} · 보스 {kb.br:0.00}");
        if (vil == null) foreach (var g in res.GroupBy(r => r.Village).OrderBy(g => g.Key)) Console.WriteLine($"  {g.Key} {100.0 * g.Count(r => r.Clear) / g.Count():0.0}% ({g.Count()})");
        if (outp != null) using (var w = new StreamWriter(outp))
            {
                w.WriteLine("village\tfloor\tkind\tline\tn\tlosePct\thpLostPct\tturns\tbreaks");
                foreach (var kv in agg.OrderBy(k => k.Key, StringComparer.Ordinal)) { var x = kv.Value; w.WriteLine($"{kv.Key}\t{x[0]}\t{100 * x[1] / x[0]:0.0}\t{x[2] / x[0]:0.0}\t{x[3] / x[0]:0.00}\t{x[4] / x[0]:0.00}"); }
            }
        return 0;
    }
}
