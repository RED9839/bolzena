using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bolzena.Core;

/// <summary>
/// 콘텐츠 작가용 콘솔 — 유니티 없이 검사 · 글 · 전투 · 판 · 시뮬. 사용법은 Docs/데이터.md §10.
/// </summary>
static class Program
{
    static List<string> rest;
    static string Opt(string name, string d = null)
    {
        int i = rest.IndexOf("--" + name);
        if (i < 0) return d;
        var v = i + 1 < rest.Count ? rest[i + 1] : "";
        rest.RemoveRange(i, Math.Min(2, rest.Count - i));
        return v;
    }
    static bool Flag(string name) { int i = rest.IndexOf("--" + name); if (i < 0) return false; rest.RemoveAt(i); return true; }
    static List<string> Many(string name) { var o = new List<string>(); string v; while ((v = Opt(name)) != null) o.AddRange(v.Split(';', StringSplitOptions.RemoveEmptyEntries)); return o; }
    static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        rest = args.ToList();
        var paths = Many("data");
        if (paths.Count == 0) paths.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Data/Sample")));
        GameData data;
        try { data = GameData.FromFolders(paths); }
        catch (Exception e) { Console.WriteLine("읽기 실패"); Console.WriteLine(e.Message); return 2; }
        string cmd = rest.Count > 0 ? rest[0] : "check";
        if (rest.Count > 0) rest.RemoveAt(0);
        var tx = new CardText(data);
        switch (cmd)
        {
            case "check":
                {
                    string hero = Opt("hero"); bool quiet = Flag("quiet");
                    var v = Validator.Check(data);
                    bool Mine(string e) => hero == null || e.Contains(hero);
                    var errs = v.Errors.Where(Mine).ToList();
                    var warns = v.Warnings.Where(Mine).ToList();
                    Console.WriteLine($"{(errs.Count == 0 ? "검사 통과" : "검사 실패")} — 오류 {errs.Count} · 주의 {warns.Count} · 사도 {data.Heroes.Count} · 카드 {data.Cards.Count} · 적 {data.Enemies.Count} · 마을 {data.Villages.Count} · 이벤트 {data.Events.Count} · 장비 {data.Equips.Count}");
                    foreach (var e in errs) Console.WriteLine("오류 " + e);
                    foreach (var w in warns) Console.WriteLine("주의 " + w);
                    if (!quiet && hero != null && data.Hero(hero) != null) { Console.WriteLine(); Console.WriteLine(Sheet(data, tx, hero)); }
                    return errs.Count == 0 ? 0 : 1;
                }
            case "hero":
                {
                    bool brief = Flag("short");   // --short: 짧은 글(CardText.Short)만
                    foreach (var h in rest.Count > 0 ? rest.ToList() : data.Heroes.Keys.ToList()) { Console.WriteLine(brief ? (data.Hero(h) != null ? tx.HeroShort(data.Hero(h)) : $"(사도 없음: {h})") : Sheet(data, tx, h)); Console.WriteLine(); }
                }
                return 0;
            case "cards":
                {
                    string hero = Opt("hero");
                    foreach (var c in data.Cards.Values.Where(c => hero == null || c.Hero == hero))
                        Console.WriteLine($"{c.Id} [{(c.X ? "X" : c.Cost.ToString())}] {c.Type} 「{c.Name}」 {tx.Card(c)}  (코스트당 값 {CardValue.Efficiency(data.View(c.Id)):0.00})");
                    return 0;
                }
            case "fight":
                {
                    // fight 사도,사도,사도 적,적 [씨앗] [--floor 1|2] [--nature 성격(판의 적 속성)] — 똑똑한 봇이 싸운다(기록 전부). 적 배율은 그 층의 일반 싸움
                    int floor = int.Parse(Opt("floor", "1")) - 1; string nat = Opt("nature");
                    if (rest.Count < 2) { Console.WriteLine("fight 사도,사도,사도 적,적 [씨앗] [--floor 1]"); return 1; }
                    var party = rest[0].Split(',').ToList(); var foes = rest[1].Split(',').ToList();
                    long seed = rest.Count > 2 ? long.Parse(rest[2]) : 1;
                    var sc = R.FoeScale(floor);
                    var b = Battle.Start(data, new BattleSetup { Party = party, Deck = data.BuildDeck(party), Enemies = foes, Seed = seed, EnemyHp = sc.hp, EnemyDmg = sc.dmg, EnemyNature = nat });
                    var bots = new Bots(data);
                    while (b.Over == null && b.Turn < 40) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
                    foreach (var l in b.Log) Console.WriteLine(l);
                    Console.WriteLine($"결과 {b.Over ?? "40턴 넘음"} · {b.Turn}턴 · 파티 HP {Math.Max(0, b.Pool.Hp)}/{b.Pool.MaxHp}");
                    Console.WriteLine($"강인도 — 깎은 카드 {b.ToughHits}(약점 {b.ToughWeakHits}) · 깎은 양 {b.ToughDealt:0.##} · 격파 {b.Breaks}");
                    return 0;
                }
            case "run":
                {
                    // run 사도,사도,사도 [씨앗] [--village 마을]
                    string village = Opt("village");
                    if (rest.Count < 1) { Console.WriteLine("run 사도,사도,사도 [씨앗] [--village 마을]"); return 1; }
                    var party = rest[0].Split(',').ToList();
                    long seed = rest.Count > 1 ? long.Parse(rest[1]) : 1;
                    var r = new RunBot(data).RunFull(party, seed, new SimOpts
                    {
                        Village = village,
                        OnFight = (b, run) => Console.WriteLine($"  {run.S.Floor + 1}층 {(run.IsBoss ? "보스" : run.S.Elite ? "엘리트" : "싸움")} {string.Join(",", b.Enemies.Select(e => e.Name))} · 파티 HP {run.S.PartyHp}/{run.S.PartyMaxHp}"),
                    });
                    Console.WriteLine($"{(r.Clear ? "완주" : $"{r.Floor + 1}층 {r.Where} 에서 쓰러짐")} · 싸움 {r.Fights} · 턴 {r.Turns} · 골드 {r.Gold} · 덱 {r.Deck}");
                    return 0;
                }
            case "sim":
                {
                    // sim [판(바퀴)] [--with 사도] [--hp 1] [--dmg 1] [--seed 0]
                    string with = Opt("with");
                    int seed = int.Parse(Opt("seed", "0")); double hp = D(Opt("hp", "1")), dmg = D(Opt("dmg", "1"));
                    int n = rest.Count > 0 && int.TryParse(rest[0], out var x) ? x : 30;
                    var res = MetaSim.Run(data, n, seed, hp, dmg, with: with);
                    Console.WriteLine(MetaSim.Report(data, res));
                    return 0;
                }
            case "report":
                {
                    // report [바퀴] [--pairs 판] [--comps 판] [--top 쌍] [--old] [--out 파일.md] — 메타 + 역할 조합 + 짝(같은 종족 · 키워드 맞물림)
                    string outp = Opt("out", "meta.md"); int pairs = int.Parse(Opt("pairs", "0")); int seed = int.Parse(Opt("seed", "0"));
                    int comps = int.Parse(Opt("comps", "0")); int top = int.Parse(Opt("top", "30")); bool old = Flag("old"); string title = Opt("title", "사도 완주율");
                    int n = rest.Count > 0 && int.TryParse(rest[0], out var x2) ? x2 : 100;
                    var res = MetaSim.Run(data, n, seed);
                    if (old) { var pr = pairs > 0 ? MetaSim.Pairs(data, pairs, seed) : null; File.WriteAllText(outp, MetaSim.Markdown(data, res, pr, title)); }
                    else
                    {
                        var ct = comps > 0 ? MetaSim.CompTable(data, comps, seed) : null;
                        var pr = pairs > 0 ? MetaSim.PairsV2(data, pairs, seed, top: top) : null;
                        File.WriteAllText(outp, MetaSim.MarkdownV2(data, res, ct, pr, title));
                    }
                    Console.WriteLine($"→ {Path.GetFullPath(outp)} · 전체 완주 {res.Clear:0.0}% · {res.Sec:0}초");
                    return 0;
                }
            case "solo":
                {
                    // solo [--err 2] [--min 300] [--max 3000] [--hero 키;키] [--out 파일.md] [--seed 0] — 사도마다 「혼자」 완주율(그 사도 + 무작위 둘), 병렬
                    double err = D(Opt("err", "2")); int min = int.Parse(Opt("min", "300")), max = int.Parse(Opt("max", "3000")), seed = int.Parse(Opt("seed", "0"));
                    var only = Many("hero"); string outp = Opt("out", "solo.md");
                    var t0 = DateTime.Now;
                    var rows = MetaSim.Solo(data, err, min, max, seed, heroes: only.Count > 0 ? only : null, log: Console.WriteLine);
                    File.WriteAllText(outp, MetaSim.SoloMarkdown(data, rows, Opt("title", "사도 혼자 완주율")));
                    foreach (var r in rows) Console.WriteLine($"  {r.Name}({r.Role}) {r.Win:0.0}% ±{r.Err:0.0} ({r.N}판)");
                    Console.WriteLine($"→ {Path.GetFullPath(outp)} · {rows.Sum(r => r.N)}판 · {(DateTime.Now - t0).TotalSeconds:0}초");
                    return 0;
                }
            case "natures":
                {
                    // natures [씨앗] — 마을 × 적 속성: 고를 수 있나 · 그 속성의 보스 클론(빌린 몸이면 「몸」)
                    long seed = rest.Count > 0 ? long.Parse(rest[0]) : 1;
                    foreach (var v in data.Villages.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
                    {
                        var ok = Run.NaturesFor(data, v.Id);
                        Console.WriteLine($"{v.Id}({v.Race}) — 클론 자리 {Run.CloneSlots(data, v.Id).Count} · 고를 수 있는 속성 {string.Join(" · ", ok)}");
                        foreach (var n in R.FOE_NATURES)
                        {
                            var c1 = Run.CloneCandidates(data, v.Id, n, 0); var c2 = Run.CloneCandidates(data, v.Id, n, 1);
                            string bs = ok.Contains(n) ? string.Join(" / ", Run.PickBosses(data, v.Id, n, seed).Select(l => string.Join("+", l.Select(id => { var e = data.Enemy(id); return e?.Clone == null ? id : e.Clone + $"{data.Hero(e.Clone)?.Star}성" + (Run.StarFits(Run.PickBosses(data, v.Id, n, seed).FindIndex(l => l.Contains(id)), data.Hero(e.Clone)?.Star ?? 3) ? "" : "*예외") + (id.StartsWith(GameData.CLONE_MARK) ? "(빌린 몸 " + id.Split('~')[2] + ")" : ""); })))) : "못 고름";
                            Console.WriteLine($"  {n} 1층 후보(1~2성) {c1.Count}({string.Join(",", c1)}) · 2층 후보(3성) {c2.Count}({string.Join(",", c2)}) → 씨앗 {seed}: {bs}");
                            if (ok.Contains(n))
                            {
                                var kinds = Enumerable.Range(1, 200).Select(s2 => string.Join(" / ", Run.PickBosses(data, v.Id, n, s2).Select(l => string.Join("+", l.Select(id => data.Enemy(id)?.Clone).Where(x => x != null))))).Distinct().ToList();
                                Console.WriteLine($"      씨앗 1~200 에서 나온 보스 조합 {kinds.Count}가지: {string.Join(" · ", kinds.Take(8))}{(kinds.Count > 8 ? " …" : "")}");
                            }
                        }
                    }
                    return 0;
                }
            default:
                Console.WriteLine("명령: check [--hero id] [--quiet] · hero [id …] · cards [--hero id] · fight 사도,… 적,… [씨앗] [--floor 1] [--nature 광기] · run 사도,… [씨앗] [--village id] · sim [판] [--with 사도] [--hp] [--dmg] [--seed] · report [바퀴] [--pairs 판] [--comps 판] [--top 30] [--out] · solo [--err 2] [--min 300] [--max 3000] [--hero 키] [--out]");
                Console.WriteLine("데이터: --data 경로(폴더 · 파일, 거듭 쓰거나 ; 로 여럿). 없으면 Data/Sample");
                return 1;
        }
    }

    static string Sheet(GameData d, CardText tx, string id)
    {
        var h = d.Hero(id);
        if (h == null) return $"(사도 없음: {id})";
        var lines = new List<string> { tx.Hero(h) };
        foreach (var cid in d.UniquesOf(id))
        {
            var c = d.Card(cid);
            lines.Add($"  ─ 「{c.Name}」 신탁 · 축복");
            for (int i = 0; i < c.Oracles.Count; i++) lines.Add($"    신탁{i + 1} 「{c.Oracles[i].Name}」 {tx.Oracle(c, c.Oracles[i])}");
            foreach (var b in c.Blesses) lines.Add($"    축복 「{b.Name}」 {tx.Bless(b)}");
        }
        return string.Join("\n", lines);
    }
}
