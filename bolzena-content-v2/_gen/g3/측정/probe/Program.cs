using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks; using System.Collections.Concurrent;
using Bolzena.Core;
// 장비 · 교주 카드 하나를 「판 처음부터 쥐여 주고」 완주율을 잰다(2026-10-10 장비 · 스펠 리뉴얼).
//   Probe <데이터> <바퀴> <씨앗> <eq|card|both|base> [id,id… | all] [병렬 12]
//   같은 씨앗 · 같은 편성(숙련 봇 · 역할 편성)으로 바탕(아무것도 안 줌)과 그 장비 · 카드를 준 판을 나란히 돌린다.
//   장비: 첫 싸움 뒤 파티 첫 사도의 그 칸에 낀다(봇이 나중에 바꿔 낄 수는 있다). 카드: 첫 싸움 뒤 파티 첫 사도를 주인으로 덱에 한 장.
//   [병렬] 뒤에 party=사도,사도,사도 를 주면 편성을 그 셋으로 고정한다(빌드 파츠 측정 — 씨앗 · 마을은 그대로).
//   출력(탭): 종류 · id · 이름 · 등급 · 판 · 완주% · 바탕과의 차(%p) · 끝까지 남은 비율(장비)
static class P {
  static int Main(string[] a) {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var d = GameData.FromFolder(a[0]); int rounds = int.Parse(a[1]); int seed = int.Parse(a[2]); string mode = a[3];
    string pick = a.Length > 4 ? a[4] : "all"; int par = a.Length > 5 && !a[5].StartsWith("party=") ? int.Parse(a[5]) : 12;
    var jobs = PartyPick.Jobs(d, rounds, seed, PartyPick.Role);
    var fixedParty = a.FirstOrDefault(x => x.StartsWith("party="));
    if (fixedParty != null) { var fp = fixedParty.Substring(6).Split(',').ToList(); jobs = jobs.Select(j => (j.i, fp, j.seed)).ToList(); }
    var items = new List<(string kind, string id)>();
    bool all = pick == "all";
    var want = all ? null : new HashSet<string>(pick.Split(','));
    if (mode == "eq" || mode == "both") foreach (var e in d.Equips.Values.Where(e => e.Affinity == null).OrderBy(e => Array.IndexOf(R.GRADES, e.Grade)).ThenBy(e => e.Slot).ThenBy(e => e.Id)) if (all || want.Contains(e.Id)) items.Add(("eq", e.Id));
    if (mode == "card" || mode == "both") foreach (var c in d.Cards.Values.Where(c => c.Neutral && c.Price > 0).OrderBy(c => Array.IndexOf(R.GRADES, c.Grade)).ThenBy(c => c.Id)) if (all || want.Contains(c.Id)) items.Add(("card", c.Id));
    double baseRate = RunSet(d, jobs, null, null, par, out _);
    Console.WriteLine($"바탕\t-\t-\t-\t{jobs.Count}\t{baseRate:0.00}\t0\t-");
    foreach (var (kind, id) in items) {
      double rate = RunSet(d, jobs, kind, id, par, out double kept);
      string name = kind == "eq" ? d.Equip(id).Name : d.Card(id).Name; string grade = kind == "eq" ? d.Equip(id).Grade : d.Card(id).Grade;
      Console.WriteLine($"{kind}\t{id}\t{name}\t{grade}\t{jobs.Count}\t{rate:0.00}\t{rate - baseRate:+0.00;-0.00}\t{(kind == "eq" ? kept.ToString("0.00") : "-")}");
    }
    return 0;
  }
  static double RunSet(GameData d, List<(int i, List<string> party, long seed)> jobs, string kind, string id, int par, out double kept) {
    int c = 0, keep = 0;
    Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = par }, j => {
      bool given = false; Run last = null;
      var r = new RunBot(d).RunFull(j.party, j.seed, new SimOpts { Skilled = true, OnFight = (b, run) => {
        last = run;
        if (given || kind == null) return; given = true;
        var k = run.S.Party[0];
        if (kind == "eq") { run.GainEquip(id); run.Equip(k, id, replace: true); }
        else run.GainCard(id, k);
      } });
      if (r.Clear) System.Threading.Interlocked.Increment(ref c);
      if (kind == "eq" && last != null && last.S.Party.Any(k => last.GearOf(k).ContainsValue(id))) System.Threading.Interlocked.Increment(ref keep);
    });
    kept = jobs.Count > 0 ? (double)keep / jobs.Count : 0;
    return 100.0 * c / jobs.Count;
  }
}
