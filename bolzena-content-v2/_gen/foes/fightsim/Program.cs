// 사용(볼제나 콘텐츠 v2 적 리워크 측정): dotnet build -c Release -o <임시 폴더> 뒤
//   dotnet <임시>/fightsim.dll <데이터 사본> <바퀴> <씨앗> <out.tsv> [마을]   — 싸움 줄별 패배 · 잃는 HP · 턴(숙련 봇 · 역할 편성)
//   dotnet <임시>/fightsim.dll <데이터 사본> dex <적 id …>                  — 도감 글(CardText.Intent · Enemy)
// 싸움 줄별 패배율 — 숙련 봇 · 역할 편성(bz sim --bot skilled --party role 과 같은 판 묶음). 엔진은 안 고친다.
// fightsim <data> <rounds> <seed> <out.tsv> [village]
using System; using System.Collections.Generic; using System.Collections.Concurrent; using System.Linq; using System.IO; using System.Threading.Tasks;
using Bolzena.Core;
static class P {
  static int Main(string[] a) {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var d = GameData.FromFolders(new List<string>{a[0]});
    if (a[1] == "dex") { var tx = new CardText(d); foreach (var id in a.Skip(2)) { var e = d.Enemy(id); Console.WriteLine($"■ {e.Name} ({id}) · HP {e.Hp} · 강인도 {e.Tough} · {e.Row}"); Console.WriteLine("  " + e.Blurb); foreach (var it in e.Intents) Console.WriteLine($"  수(w{it.W}) {it.Say}: {tx.Intent(it)}"); foreach (var l in tx.Enemy(e).Split((char)10)) Console.WriteLine("  · " + l); Console.WriteLine(); } return 0; }
    int rounds = int.Parse(a[1]); int seed = int.Parse(a[2]); string outp = a[3]; string vil = a.Length > 4 ? a[4] : null;
    var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role);
    var agg = new ConcurrentDictionary<string, double[]>(); // n, lose, hpLostPct, turns
    var vagg = new ConcurrentDictionary<string, int[]>(); int wins = 0, n = 0;
    var t0 = DateTime.Now;
    Parallel.ForEach(jobs, new ParallelOptions{ MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)}, j => {
      var fights = new List<(Battle b, string key, int hp0, int max)>();
      var r = new RunBot(d).RunFull(j.party, j.seed, new SimOpts { Skilled = true, Village = vil, OnFight = (b, run) => {
        string kind = run.S.EventFight != null ? "event" : run.IsBoss ? "boss" : run.S.Elite ? "elite" : "fight";
        string line = kind == "boss" ? "boss" : string.Join("+", b.Enemies.Select(e => e.Key));
        fights.Add((b, $"{run.S.Village}\t{run.S.Floor + 1}\t{kind}\t{line}", run.S.PartyHp, Math.Max(1, run.S.PartyMaxHp)));
      }});
      foreach (var f in fights) {
        bool lose = f.b.Over != "win"; double lost = Math.Max(0, f.hp0 - Math.Max(0, f.b.Pool.Hp)) * 100.0 / f.max;
        agg.AddOrUpdate(f.key, _ => new double[]{1, lose?1:0, lost, f.b.Turn}, (_, x) => { lock (x) { x[0]++; x[1] += lose?1:0; x[2] += lost; x[3] += f.b.Turn; } return x; });
      }
      vagg.AddOrUpdate(r.Village, _ => new[]{1, r.Clear?1:0}, (_, x) => { lock (x) { x[0]++; x[1] += r.Clear?1:0; } return x; });
      System.Threading.Interlocked.Increment(ref n); if (r.Clear) System.Threading.Interlocked.Increment(ref wins);
    });
    using (var w = new StreamWriter(outp)) {
      w.WriteLine("village\tfloor\tkind\tline\tn\tlosePct\thpLostPct\tturns");
      foreach (var kv in agg.OrderBy(k => k.Key, StringComparer.Ordinal)) { var x = kv.Value; w.WriteLine($"{kv.Key}\t{x[0]}\t{100*x[1]/x[0]:0.0}\t{x[2]/x[0]:0.0}\t{x[3]/x[0]:0.00}"); }
    }
    Console.WriteLine($"완주 {100.0*wins/n:0.0}% · {n}판 · {(DateTime.Now-t0).TotalSeconds:0}초");
    foreach (var kv in vagg.OrderBy(k => k.Key)) Console.WriteLine($"  {kv.Key} {100.0*kv.Value[1]/kv.Value[0]:0.0}% ({kv.Value[0]})");
    return 0;
  }
}
