using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks; using System.Collections.Concurrent;
using Bolzena.Core;
static class P {
  static int Main(string[] a) {
    // Tracker <data> <rounds> <seeds 0,1,2> [full]
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var d = GameData.FromFolder(a[0]); int rounds = int.Parse(a[1]); var seeds = a[2].Split(',').Select(int.Parse).ToList(); bool full = a.Length > 3 && a[3] == "full"; double hpx = a.Length > 4 ? double.Parse(a[4], System.Globalization.CultureInfo.InvariantCulture) : 1, dmgx = a.Length > 5 ? double.Parse(a[5], System.Globalization.CultureInfo.InvariantCulture) : 1;
    var worn = new ConcurrentDictionary<string,int>(); var deckN = new ConcurrentDictionary<string,int>();
    var wornWin = new ConcurrentDictionary<string,int>(); var deckWin = new ConcurrentDictionary<string,int>();
    int total = 0, clears = 0; long neutralSum = 0, deckSum = 0;
    foreach (var seed in seeds) {
      var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role); int c = 0;
      Parallel.ForEach(jobs, new ParallelOptions{ MaxDegreeOfParallelism = 20 }, j => {
        Run last = null; var seen = new HashSet<string>();
        var r = new RunBot(d).RunFull(j.party, j.seed, new SimOpts { Skilled = true, Hpx = hpx, Dmgx = dmgx, OnFight = (b, run) => { last = run; foreach (var k in run.S.Party) foreach (var kv in run.GearOf(k)) seen.Add(kv.Value); } });
        if (r.Clear) System.Threading.Interlocked.Increment(ref c);
        foreach (var g in seen) { worn.AddOrUpdate(g, 1, (_, v) => v + 1); if (r.Clear) wornWin.AddOrUpdate(g,1,(_,v)=>v+1); }
        if (last != null) {
          var ns = last.S.Deck.Select(x => GameData.NoInst(x).Split('@')[0].TrimEnd('^')).Where(x => d.Card(x)?.Neutral == true).ToList();
          System.Threading.Interlocked.Add(ref neutralSum, ns.Count); System.Threading.Interlocked.Add(ref deckSum, last.S.Deck.Count);
          foreach (var id in ns.Distinct()) { deckN.AddOrUpdate(id, 1, (_, v) => v + 1); if (r.Clear) deckWin.AddOrUpdate(id,1,(_,v)=>v+1);} }
      });
      Console.WriteLine($"씨앗 {seed}: {jobs.Count}판 완주 {100.0*c/jobs.Count:0.00}%"); total += jobs.Count; clears += c;
    }
    Console.WriteLine($"합 {total}판 완주 {100.0*clears/total:0.00}% · 끝 덱 교주 카드 평균 {(double)neutralSum/total:0.00}장 · 덱 {(double)deckSum/total:0.0}장");
    if (!full) return 0;
    Console.WriteLine("== 장비: 낀 판 · 낀 판 완주%");
    foreach (var e in d.Equips.Values.Where(e => e.Affinity == null).OrderBy(e => e.Grade).ThenByDescending(e => worn.GetValueOrDefault(e.Id)))
      Console.WriteLine($"  {e.Grade}\t{e.Slot}\t{e.Id}\t{e.Name}\t{worn.GetValueOrDefault(e.Id)}\t{(worn.GetValueOrDefault(e.Id)>0?100.0*wornWin.GetValueOrDefault(e.Id)/worn[e.Id]:0):0}%");
    Console.WriteLine("== 교주 카드: 끝 덱에 든 판 · 든 판 완주%");
    foreach (var c in d.Cards.Values.Where(c => c.Neutral && c.Price > 0).OrderBy(c => c.Grade).ThenByDescending(c => deckN.GetValueOrDefault(c.Id)))
      Console.WriteLine($"  {c.Grade}\t{c.Id}\t{c.Name}\t{deckN.GetValueOrDefault(c.Id)}\t{(deckN.GetValueOrDefault(c.Id)>0?100.0*deckWin.GetValueOrDefault(c.Id)/deckN[c.Id]:0):0}%");
    return 0;
  }
}
