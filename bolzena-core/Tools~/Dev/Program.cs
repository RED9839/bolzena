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
    /// <summary>고유 효과 계측 표(탭 구분) — 사도 · 키워드마다 싸움당 쌓은 양 · 넘쳐 버린 몫 · 쓴 양 · 최대 도달 턴(--kw 파일).</summary>
    static string KwTable(GameData d)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("hero\tname\trole\tkw\tcarrier\tcap\tfights\tgainFightPct\taskedPerFight\tgotPerFight\twastePct\tspentPerFight\tspendNPerFight\tcapFightPct\tcapTurn\tcapSharePct\tturnsPerFight").Append('\n');
        foreach (var r in KwMeter.Rows())
        {
            var h = d.Hero(r.Hero); double f = Math.Max(1, r.Fights);
            sb.Append(string.Join("\t", r.Hero, h?.Name, h?.Role, r.Id, r.Carrier, r.Cap, r.Fights, (100.0 * r.GainFights / f).ToString("0.0"), (r.Asked / f).ToString("0.00"), (r.Got / f).ToString("0.00"),
                (100 * r.Waste).ToString("0.0"), (r.Spent / f).ToString("0.00"), (r.SpendN / f).ToString("0.00"), (100.0 * r.CapFights / f).ToString("0.0"), r.CapTurn.ToString("0.00"), (100 * r.CapShare).ToString("0.0"), (r.Turns / f).ToString("0.00"))).Append('\n');
        }
        return sb.ToString();
    }
    static bool Flag(string name) { int i = rest.IndexOf("--" + name); if (i < 0) return false; rest.RemoveAt(i); return true; }

    /// <summary>--bot basic|skilled · --party random|role|synergy · --uniqueonly → 봇 손잡이 · 편성. 틀리면 err.</summary>
    static (SimOpts bot, string party, string err) BotOpts()
    {
        string b = Opt("bot", "basic"), p = Opt("party", PartyPick.Random); bool u = Flag("uniqueonly");
        if (b != "basic" && b != "skilled") return (null, null, "--bot 은 basic(초보) · skilled(숙련)");
        if (!PartyPick.MODES.Contains(p)) return (null, null, "--party 는 random · role · synergy");
        // --crayon all | 표.json — 교주 능력치(크레파스) 전부 올린 상태(기본 표: bolzena-runui Resources/RunUI/crayon.json)
        string cr = Opt("crayon");
        Dictionary<string, double> perks = null;
        if (cr != null)
        {
            string f = cr == "all" ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../bolzena-runui/Runtime/Resources/RunUI/crayon.json")) : cr;
            if (!File.Exists(f)) return (null, null, "크레파스 표가 없다: " + f);
            perks = Crayon.Perks(Crayon.Parse(File.ReadAllText(f)), null, all: true);
            Console.WriteLine("교주 능력치 전부 — " + string.Join(" · ", perks.Select(kv => $"{kv.Key} {kv.Value}")));
        }
        return (new SimOpts { Skilled = b == "skilled", UniqueOnly = u, Perks = perks }, p, null);
    }

    /// <summary>원작 관계(시너지 편성) — --relations 파일, 없으면 데이터 폴더의 _ref/relations.json, 그다음 사도 데스크의 relations.json. 못 찾으면 빈 표(키워드 맞물림만).</summary>
    static Dictionary<(string, string), double> Relations(GameData d, List<string> dataPaths)
    {
        var cands = new List<string>();
        string o = Opt("relations"); if (o != null) cands.Add(o);
        foreach (var p in dataPaths) if (Directory.Exists(p)) cands.Add(Path.Combine(p, "_ref", "relations.json"));
        cands.Add(@"C:\projects\사도 데스크\prototype\data\relations.json");
        var f = cands.FirstOrDefault(File.Exists);
        if (f == null) { Console.WriteLine("관계 파일 없음 — 시너지 편성은 키워드 맞물림만 본다"); return new Dictionary<(string, string), double>(); }
        var rel = PartyPick.Relations(d, File.ReadAllText(f));
        Console.WriteLine($"관계 {rel.Count}쌍 ← {f}");
        return rel;
    }

    /// <summary>botcmp 표 — 모드마다 전체 · 마을 · 역할 조합 · 사도, 그리고 두 모드 사이 사도 완주율 차이(위 · 아래).</summary>
    static string BotCmpMarkdown(GameData d, List<MetaSim.Result> rs, int a, int b, int top, string title)
    {
        var sb = new System.Text.StringBuilder();
        double Half(double p, int n) => n > 0 ? 1.96 * Math.Sqrt(p / 100 * (1 - p / 100) / n) * 100 : 0;
        sb.AppendLine($"# {title}").AppendLine();
        sb.AppendLine("## 모드별 완주율").AppendLine().AppendLine("| 모드 | 판 | 완주 | 오차(95%) | 1층 쓰러짐 | 2층 쓰러짐 | 평균 턴 일반 · 보스 | 끝난 덱(장 · 고유 · 기본) | 상점 빼기/판 |").AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var r in rs) sb.AppendLine($"| {r.Label} | {r.Runs} | **{r.Clear:0.0}%** | ±{Half(r.Clear, r.Runs):0.0} | {r.Fell[0]:0.0}% | {r.Fell[1]:0.0}% | {r.AvgTurnsFight:0.0} · {r.AvgTurnsBoss:0.0} | {r.AvgDeck:0.0} · {r.AvgUniques:0.0} · {r.AvgBasics:0.0} | {r.AvgRemovals:0.00} |");
        sb.AppendLine().AppendLine("## 마을별").AppendLine().AppendLine("| 마을 | " + string.Join(" | ", rs.Select(r => r.Label)) + " |").AppendLine("|---|" + string.Concat(rs.Select(_ => "---|")));
        foreach (var v in d.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal))
            sb.AppendLine($"| {d.Villages[v].Name} | " + string.Join(" | ", rs.Select(r => r.Villages.TryGetValue(v, out var x) ? $"{x.win:0.0}% ({x.n})" : "-")) + " |");
        sb.AppendLine().AppendLine("## 역할 조합별").AppendLine().AppendLine("| 편성 | " + string.Join(" | ", rs.Select(r => r.Label)) + " |").AppendLine("|---|" + string.Concat(rs.Select(_ => "---|")));
        foreach (var c in MetaSim.COMPS)
            sb.AppendLine($"| {c} | " + string.Join(" | ", rs.Select(r => r.Comps.TryGetValue(c, out var x) ? $"{x.win:0.0}% ({x.n})" : "-")) + " |");
        sb.AppendLine().AppendLine("## 역할별 사도 평균").AppendLine().AppendLine("| 역할 | " + string.Join(" | ", rs.Select(r => r.Label)) + " |").AppendLine("|---|" + string.Concat(rs.Select(_ => "---|")));
        foreach (var role in new[] { "탱커", "서포터", "딜러" })
            sb.AppendLine($"| {role} | " + string.Join(" | ", rs.Select(r => { var h = r.Heroes.Where(x => x.Role == role && x.N > 0).ToList(); return h.Count > 0 ? $"{h.Average(x => x.Win):0.0}%" : "-"; })) + " |");
        if (a >= 0 && b >= 0 && a < rs.Count && b < rs.Count && a != b)
        {
            var A = rs[a].Heroes.ToDictionary(h => h.Key); var B = rs[b].Heroes.ToDictionary(h => h.Key);
            var diff = A.Keys.Where(B.ContainsKey).Select(k => (k, A[k], B[k], dv: B[k].Win - A[k].Win)).OrderByDescending(x => x.dv).ToList();
            sb.AppendLine().AppendLine($"## 사도 완주율 변화 — {rs[a].Label} → {rs[b].Label}").AppendLine();
            sb.AppendLine($"사도 {diff.Count}명 · 평균 변화 {diff.Average(x => x.dv):+0.0;-0.0}%p · 사도 한 명 값의 오차 ±{Half(rs[a].Clear, (int)diff.Average(x => x.Item2.N)):0}%p 쯤(95%) — 양 끝만 본다.").AppendLine();
            void Rows(IEnumerable<(string k, MetaSim.HeroRow x, MetaSim.HeroRow y, double dv)> l)
            {
                sb.AppendLine("| 사도 | 역할 | 전 | 뒤 | 변화 |").AppendLine("|---|---|---|---|---|");
                foreach (var t in l) sb.AppendLine($"| {t.x.Name} | {t.x.Role} | {t.x.Win:0.0}% ({t.x.N}) | {t.y.Win:0.0}% ({t.y.N}) | {(t.dv >= 0 ? "+" : "")}{t.dv:0.0} |");
            }
            sb.AppendLine("### 가장 오른 사도").AppendLine(); Rows(diff.Take(top));
            sb.AppendLine().AppendLine("### 가장 덜 오른(내린) 사도").AppendLine(); Rows(diff.AsEnumerable().Reverse().Take(top));
        }
        sb.AppendLine().AppendLine("## 사도(모드별 완주율)").AppendLine().AppendLine("| 사도 | 역할 | " + string.Join(" | ", rs.Select(r => r.Label)) + " |").AppendLine("|---|---|" + string.Concat(rs.Select(_ => "---|")));
        foreach (var h in d.Heroes.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
            sb.AppendLine($"| {h.Name} | {h.Role} | " + string.Join(" | ", rs.Select(r => { var x = r.Heroes.FirstOrDefault(y => y.Key == h.Id); return x != null ? $"{x.Win:0.0}% ({x.N})" : "-"; })) + " |");
        return sb.ToString();
    }
    static List<string> Many(string name) { var o = new List<string>(); string v; while ((v = Opt(name)) != null) o.AddRange(v.Split(';', StringSplitOptions.RemoveEmptyEntries)); return o; }
    static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        rest = args.ToList();
        if (Opt("bossult") == "0") BossUlt.On = false;   // 보스 클론 고학년 끄기(전후 비교)
        string bux = Opt("bossultx"); if (bux != null) BossUlt.Scale = D(bux);   // 보스 고학년 피해 배율(계수 찾기)
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
                    // sim [판(바퀴)] [--with 사도] [--hp 1] [--dmg 1] [--seed 0] [--bot basic|skilled] [--party random|role|synergy] [--uniqueonly] [--relations 관계.json] [--out 파일.md]
                    string with = Opt("with");
                    int seed = int.Parse(Opt("seed", "0")); double hp = D(Opt("hp", "1")), dmg = D(Opt("dmg", "1"));
                    var (bot, party, err) = BotOpts(); if (err != null) { Console.WriteLine(err); return 1; }
                    var syn = party == PartyPick.Synergy ? new PartyPick.SynScore(data, Relations(data, paths)) : null;
                    string outMd = Opt("out");
                    int n = rest.Count > 0 && int.TryParse(rest[0], out var x) ? x : 30;
                    string kwOut = Opt("kw"); if (kwOut != null) { KwMeter.Reset(); KwMeter.On = true; }
                    var res = MetaSim.Run(data, n, seed, hp, dmg, with: with, bot: bot, party: party, syn: syn);
                    if (outMd != null) { File.WriteAllText(outMd, MetaSim.MarkdownV2(data, res, null, null, res.Label)); Console.WriteLine($"→ {Path.GetFullPath(outMd)}"); }
                    if (kwOut != null) { KwMeter.On = false; File.WriteAllText(kwOut, KwTable(data)); }
                    Console.WriteLine(MetaSim.Report(data, res));
                    return 0;
                }
            case "botcmp":
                {
                    // botcmp [바퀴] [--seed 0] [--hp 1] [--dmg 1] [--modes "basic:random;skilled:role;skilled:synergy;skilled:role:unique"] [--vs 0,1] [--top 12] [--relations 파일] [--out 파일.md] [--title 제목]
                    //   같은 씨앗으로 봇 · 편성 모드를 나란히 — 모드 = 봇(basic · skilled):편성(random · role · synergy)[:unique]. --vs a,b 는 사도 변화 표를 낼 두 모드(차례 번호).
                    int seed = int.Parse(Opt("seed", "0")); double hp = D(Opt("hp", "1")), dmg = D(Opt("dmg", "1"));
                    var modes = (Opt("modes") ?? "basic:random;skilled:role;skilled:synergy;skilled:role:unique").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                    var vs = (Opt("vs") ?? "0,1").Split(',').Select(int.Parse).ToArray(); int top = int.Parse(Opt("top", "12"));
                    string outp = Opt("out", "botcmp.md"), title = Opt("title", "봇 · 편성 모드별 완주율");
                    int n = rest.Count > 0 && int.TryParse(rest[0], out var x3) ? x3 : 100;
                    PartyPick.SynScore syn = null;
                    var rs = new List<MetaSim.Result>();
                    foreach (var m in modes)
                    {
                        var p = m.Split(':');
                        if (p.Length < 2 || (p[0] != "basic" && p[0] != "skilled") || !PartyPick.MODES.Contains(p[1])) { Console.WriteLine($"모드 「{m}」 — 봇:편성[:unique] (basic|skilled : random|role|synergy)"); return 1; }
                        if (p[1] == PartyPick.Synergy && syn == null) syn = new PartyPick.SynScore(data, Relations(data, paths));
                        var r = MetaSim.Run(data, n, seed, hp, dmg, bot: new SimOpts { Skilled = p[0] == "skilled", UniqueOnly = p.Length > 2 && p[2] == "unique" }, party: p[1], syn: syn);
                        rs.Add(r);
                        Console.WriteLine($"{r.Label} — 완주 {r.Clear:0.0}% · {r.Runs}판 · 1층 {r.Fell[0]:0.0}% · 2층 {r.Fell[1]:0.0}% · 덱 {r.AvgDeck:0.0}(고유 {r.AvgUniques:0.0} · 기본 {r.AvgBasics:0.0}) · 상점 빼기 {r.AvgRemovals:0.00} · {r.Sec:0}초");
                    }
                    File.WriteAllText(outp, BotCmpMarkdown(data, rs, vs.Length > 0 ? vs[0] : -1, vs.Length > 1 ? vs[1] : -1, top, title));
                    Console.WriteLine($"→ {Path.GetFullPath(outp)}");
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
                    var only = Many("hero"); string outp = Opt("out", "solo.md"); string kwOut = Opt("kw");
                    if (kwOut != null) { KwMeter.Reset(); KwMeter.On = true; }
                    var t0 = DateTime.Now;
                    var rows = MetaSim.Solo(data, err, min, max, seed, heroes: only.Count > 0 ? only : null, log: Console.WriteLine);
                    File.WriteAllText(outp, MetaSim.SoloMarkdown(data, rows, Opt("title", "사도 혼자 완주율")));
                    if (kwOut != null) { KwMeter.On = false; File.WriteAllText(kwOut, KwTable(data)); Console.WriteLine($"→ 고유 효과 계측 {Path.GetFullPath(kwOut)}"); }
                    foreach (var r in rows) Console.WriteLine($"  {r.Name}({r.Role}) {r.Win:0.0}% ±{r.Err:0.0} ({r.N}판)");
                    Console.WriteLine($"→ {Path.GetFullPath(outp)} · {rows.Sum(r => r.N)}판 · {(DateTime.Now - t0).TotalSeconds:0}초");
                    return 0;
                }
            case "bench":
                // bench [판 20] [--seed 0] [--rep 3] [--records 파일] · bench --sim 바퀴 [--threads 8] · bench --textdump 파일 — 엔진 성능(Bench.cs)
                return Bench.Run(data, rest);
            case "bossult":
                {
                    // bossult [--out 파일.md] — 보스 클론 고학년 변환표(사도마다 원래 효과 · 보스 효과 · 예고 피해)
                    string outp = Opt("out");
                    string md = BossUlt.Table(data);
                    if (outp != null) { File.WriteAllText(outp, md); Console.WriteLine($"→ {Path.GetFullPath(outp)}"); } else Console.WriteLine(md);
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
                            var c1 = Run.FloorCandidates(data, v.Id, n, 0); var c2 = Run.FloorCandidates(data, v.Id, n, 1); bool alt = Run.CloneCandidates(data, v.Id, n, 0).Count == 0;
                            string bs = ok.Contains(n) ? string.Join(" / ", Run.PickBosses(data, v.Id, n, seed).Select(l => string.Join("+", l.Select(id => { var e = data.Enemy(id); return e?.Clone == null ? id : e.Clone + $"{data.Hero(e.Clone)?.Star}성" + (Run.StarFits(Run.PickBosses(data, v.Id, n, seed).FindIndex(l => l.Contains(id)), data.Hero(e.Clone)?.Star ?? 3) ? "" : "*1~2성 없어 3성") + (data.Hero(e.Clone)?.Race == v.Race ? "" : "·" + data.Hero(e.Clone)?.Race) + (id.StartsWith(GameData.CLONE_MARK) ? "(빌린 몸 " + id.Split('~')[2] + ")" : ""); })))) : "못 고름";
                            Console.WriteLine($"  {n} {(ok.Contains(n) ? "열림" : "닫힘")} · 1층 후보({(alt ? "1~2성 없어 3성" : "1~2성")}) {c1.Count}({string.Join(",", c1)}) · 2층 후보(3성) {c2.Count}({string.Join(",", c2)}) → 씨앗 {seed}: {bs}");
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
                Console.WriteLine("명령: check [--hero id] [--quiet] · hero [id …] · cards [--hero id] · fight 사도,… 적,… [씨앗] [--floor 1] [--nature 광기] · run 사도,… [씨앗] [--village id] · sim [판] [--with 사도] [--hp] [--dmg] [--seed] [--bot basic|skilled] [--party random|role|synergy] [--uniqueonly] [--out] · botcmp [판] [--modes 봇:편성;…] [--vs a,b] [--out] · report [바퀴] [--pairs 판] [--comps 판] [--top 30] [--out] · solo [--err 2] [--min 300] [--max 3000] [--hero 키] [--out] · bossult [--out] (아무 명령에 --bossult 0 = 보스 클론 고학년 끄기)");
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
