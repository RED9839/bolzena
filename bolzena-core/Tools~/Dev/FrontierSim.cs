using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 프론티어 개발 도구(2단계 — bolzena-unity/Docs/프론티어_설계.md §7).
///   frontier [판 200] [--slotbot basic|skilled|both] [--fights 300] [--fightbot smart|simple|both] [--seed 0] [--threads 12]
///            [--boss id] [--turns N] [--hp 1] [--dmg 1] [--refined 1|0] [--save 폴더] [--out 파일.md]
///     --refined 1(기본): 슬롯을 사람의 「깎은 덱」 쪽으로 다듬는다(Refine — 사도 고유 카드만 · 장비 희귀 이상). 0 이면 판 끝 그대로.
///     봇 판을 돌려 완주한 판마다 사도 셋을 슬롯으로 뜨고(HeroSlot.Of → 규칙 검사 → JSON 왕복), 슬롯 셋(같은 사도 둘 없이)으로 프론티어를 봇이 싸워 점수를 낸 뒤
///     그 테이프를 JSON 으로 썼다 읽어 슬롯 · 시즌 · 씨앗으로 다시 지은 셋업과 견주고 처음부터 다시 돌려(Frontier.Verify) 점수 · 지문 · 끝이 같은지 본다.
///     편성(같은 판 셋 · 섞음 · 역할 조합) · 슬롯 봇 · 전투 봇별 점수 분포(평균 · 가운데 · 최고 …)와 끝(시간 · 쓰러짐 · 보스 처치) 비율.
///   frontier --slots a.json,b.json,c.json [--seed 1] [--fightbot smart] [--tape 파일.json] — 슬롯 파일로 한 번 싸워 점수 · 테이프
///   frontier --verify 테이프.json [--slots a.json,b.json,c.json] — 남긴 테이프를 다시 돌려 확인
///   frontier --slots a,b,c --seed N [--prog-state 성장.json] --tape 파일 — 슬롯 셋으로 한 판(성장 = 서버 시작 응답의 prog)
/// </summary>
static class FrontierSim
{
    sealed class SlotRec { public HeroSlot Slot; public string Bot; public int Run; public string Role; }
    sealed class FightRec { public string Bot, SlotBot, Comp, Roles, Heroes; public int Score, Played, MaxTurn; public string Over; public bool Same; }

    public static int Run(GameData data, List<string> args, Func<string, string, string> opt, Func<string, bool> flag)
    {
        var season = new FrontierSeason
        {
            Id = Frontier.PRESEASON.Id, Boss = opt("boss", Frontier.PRESEASON.Boss), Turns = int.Parse(opt("turns", Frontier.PRESEASON.Turns.ToString())),
            Hp = double.Parse(opt("hp", "1"), System.Globalization.CultureInfo.InvariantCulture), Dmg = double.Parse(opt("dmg", "1"), System.Globalization.CultureInfo.InvariantCulture),
        };
        if (data.Enemy(season.Boss) == null) { Console.WriteLine($"시즌 보스 「{season.Boss}」 가 데이터에 없다 — --data 에 bolzena-content-v2 를 주었나?"); return 1; }
        string verify = opt("verify", null), slotFiles = opt("slots", null);
        if (verify != null) return VerifyFile(data, season, verify, slotFiles);
        if (slotFiles != null) return OneFight(data, season, slotFiles, long.Parse(opt("seed", "1")), opt("fightbot", "smart"), opt("tape", null), opt("prog-state", null));

        int seed = int.Parse(opt("seed", "0")), threads = int.Parse(opt("threads", "12")), fights = int.Parse(opt("fights", "300"));
        string slotBot = opt("slotbot", "both"), fightBot = opt("fightbot", "both"), save = opt("save", null), outMd = opt("out", null);
        bool refined = opt("refined", "1") != "0";
        string progName = opt("prog", null);   // 사도 영구 성장 측정 묶음 — 프론티어 싸움에만(슬롯을 뜨는 봇 판은 성장 없이)
        if (progName != null && (data.Prog == null || !data.Prog.Presets.ContainsKey(progName))) { Console.WriteLine($"--prog 묶음 「{progName}」 이 성장 표에 없다"); return 1; }
        int n = args.Count > 0 && int.TryParse(args[0], out var x) ? x : 200;
        var slotBots = slotBot == "both" ? new[] { "basic", "skilled" } : new[] { slotBot };
        var fightBots = fightBot == "both" ? new[] { "smart", "simple" } : new[] { fightBot };
        if (slotBots.Any(b => b != "basic" && b != "skilled") || fightBots.Any(b => b != "smart" && b != "simple")) { Console.WriteLine("--slotbot basic|skilled|both · --fightbot smart|simple|both"); return 1; }
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"frontier — 보스 {season.Boss}({data.Enemy(season.Boss).Name}) · {season.Turns}턴 · HP×{season.Hp} · 피해×{season.Dmg} · 판 {n}×{slotBots.Length} · 프론티어 {fights}×{fightBots.Length} · 슬롯 {(refined ? "다듬음(고유 카드만 · 장비 희귀 이상)" : "그대로")} · 성장 {progName ?? "없음"} · 규칙 {Fingerprint.Rules()} · 콘텐츠 {data.ContentHash ?? "-"}");
        int cardsBefore = 0, cardsAfter = 0, gearSwap = 0, gearFill = 0;

        // ── 1. 봇 판 → 슬롯 ──
        var slots = new ConcurrentBag<SlotRec>();
        var bad = new ConcurrentBag<string>();
        int runs = 0, clears = 0;
        foreach (var sb in slotBots)
        {
            int per = Math.Max(1, data.Heroes.Count / 3);
            var jobs = PartyPick.Jobs(data, (n + per - 1) / per, seed + (sb == "skilled" ? 1000 : 0), PartyPick.Random).Take(n).ToList();
            Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = threads }, j =>
            {
                Run end = null;
                new RunBot(data).RunFull(j.party, j.seed, new SimOpts { Skilled = sb == "skilled", OnEnd = r => end = r });
                Interlocked.Increment(ref runs);
                if (end?.S.Done != "clear") return;
                Interlocked.Increment(ref clears);
                foreach (var h in end.S.Party)
                {
                    var s = HeroSlot.Of(end, h);
                    if (refined)
                    {
                        Interlocked.Add(ref cardsBefore, s.Cards.Count);
                        var (swap, fill) = Refine(data, s);
                        Interlocked.Add(ref cardsAfter, s.Cards.Count); Interlocked.Add(ref gearSwap, swap); Interlocked.Add(ref gearFill, fill);
                    }
                    var c = HeroSlot.Check(data, s);
                    if (!c.Ok) { bad.Add($"슬롯 검사 실패 {h}(판 {sb} {j.i}) — {c}"); continue; }
                    var back = HeroSlot.Parse(s.Json());
                    if (back.Json() != s.Json()) { bad.Add($"슬롯 왕복이 다르다 {h}(판 {sb} {j.i})"); continue; }
                    slots.Add(new SlotRec { Slot = back, Bot = sb, Run = j.i + (sb == "skilled" ? 100000 : 0), Role = data.Hero(h)?.Role ?? "?" });
                }
            });
        }
        var pool = slots.OrderBy(s => s.Run).ThenBy(s => s.Slot.Hero, StringComparer.Ordinal).ToList();
        Console.WriteLine($"판 {runs} · 완주 {clears} · 슬롯 {pool.Count} · 슬롯 문제 {bad.Count} · {sw.Elapsed.TotalSeconds:0}초");
        if (refined && pool.Count > 0)
            Console.WriteLine($"다듬기 — 카드 슬롯당 {(double)cardsBefore / pool.Count:0.0} → {(double)cardsAfter / pool.Count:0.0}장 · 고유 카드 장수 분포 {string.Join(" ", pool.GroupBy(s => s.Slot.Cards.Count).OrderBy(g => g.Key).Select(g => $"{g.Key}장:{g.Count()}"))} · 장비 바꿈 {gearSwap} · 빈 칸 채움 {gearFill}");
        if (save != null)
        {
            Directory.CreateDirectory(Path.Combine(save, "slots"));
            foreach (var s in pool) File.WriteAllText(Path.Combine(save, "slots", $"{s.Bot}_{s.Run:000000}_{s.Slot.Hero}.json"), s.Slot.Json(true));
        }
        if (pool.Select(s => s.Slot.Hero).Distinct().Count() < 3) { Console.WriteLine("서로 다른 사도 슬롯이 셋이 안 된다"); foreach (var b in bad.Take(20)) Console.WriteLine("  " + b); return 1; }

        // ── 2. 편성 → 프론티어 → 다시 돌리기 ──
        var byRun = pool.GroupBy(s => s.Run).Where(g => g.Count() == 3).Select(g => g.ToList()).ToList();
        var plan = new List<(int i, string fb, List<SlotRec> party, long seed, string comp)>();
        uint rs = unchecked((uint)(4242 + seed * 7919));
        int Rnd(int m) { rs = unchecked(rs * 1103515245u + 12345u); return (int)((rs >> 8) % (uint)m); }
        for (int i = 0; i < fights; i++)
        {
            List<SlotRec> party; string comp;
            if (i % 3 == 0 && byRun.Count > 0) { party = byRun[Rnd(byRun.Count)]; comp = "같은 판"; }
            else
            {
                party = new List<SlotRec>();
                for (int g = 0; party.Count < 3 && g < 200; g++) { var s = pool[Rnd(pool.Count)]; if (party.All(p => p.Slot.Hero != s.Slot.Hero)) party.Add(s); }
                comp = "섞음";
            }
            long fs = 900000 + (long)seed * 1000003 + i * 7919;
            foreach (var fb in fightBots) plan.Add((plan.Count, fb, party, fs, comp));
        }
        var recs = new ConcurrentBag<FightRec>();
        long tapeBytes = 0;
        Parallel.ForEach(plan, new ParallelOptions { MaxDegreeOfParallelism = threads }, p =>
        {
            var sl = p.party.Select(s => s.Slot).ToList();
            var prog = progName != null ? Prog.Preset(data, data.Prog, progName, sl.Select(s => s.Hero)) : null;
            var b = Frontier.Start(data, sl, season, p.seed, prog: prog);
            int maxTurn = Play(data, b, season, p.fb, p.seed);
            var tape = Frontier.Tape(b, season, sl);
            var json = tape.Json();
            Interlocked.Add(ref tapeBytes, json.Length);
            var chk = Frontier.Verify(data, FrontierTape.Parse(json), season, sl.Select(s => HeroSlot.Parse(s.Json())).ToList(), prog);
            bool same = chk.Ok && chk.Score == tape.Score && chk.Over == tape.Over;
            if (!same) bad.Add($"프론티어 다시 돌리기 다름 #{p.i}({string.Join(",", sl.Select(s => s.Hero))} · 씨앗 {p.seed} · {p.fb}) — {chk.Why ?? $"점수 {tape.Score} → {chk.Score}"}");
            if (save != null && p.i < 60) { Directory.CreateDirectory(Path.Combine(save, "tapes")); File.WriteAllText(Path.Combine(save, "tapes", $"frontier_{p.i:0000}.json"), json); }
            recs.Add(new FightRec
            {
                Bot = p.fb, SlotBot = string.Join("+", p.party.Select(s => s.Bot).Distinct().OrderBy(x => x)), Comp = p.comp,
                Roles = string.Join("·", p.party.Select(s => s.Role).OrderBy(r => r, StringComparer.Ordinal)),
                Heroes = string.Join(",", sl.Select(s => s.Hero)), Score = tape.Score, Played = tape.Played, MaxTurn = maxTurn, Over = tape.Over, Same = same,
            });
        });
        var all = recs.ToList();
        int diff = all.Count(r => !r.Same);
        Console.WriteLine($"프론티어 {all.Count} · 다시 돌리기 같음 {all.Count - diff} · 다름 {diff} · 테이프 평균 {(all.Count > 0 ? tapeBytes / all.Count / 1024.0 : 0):0.0}KB · {sw.Elapsed.TotalSeconds:0}초");
        foreach (var s in bad.Take(30)) Console.WriteLine("  " + s);
        var md = Report(data, season, all, pool, runs, clears, bad.Count);
        Console.WriteLine(md);
        if (outMd != null) { File.WriteAllText(outMd, md); Console.WriteLine($"→ {Path.GetFullPath(outMd)}"); }
        if (save != null) Console.WriteLine($"→ {Path.GetFullPath(save)}");
        return diff == 0 && bad.Count == 0 ? 0 : 1;
    }

    static readonly Dictionary<string, int> GRADE_RANK = new() { ["일반"] = 0, ["고급"] = 1, ["희귀"] = 2, ["전설"] = 3 };
    const int REFINE_GRADE = 2;   // 희귀 이상

    /// <summary>
    /// 봇 슬롯을 사람의 「깎은 덱」 쪽으로 다듬는다(2026-10-18 사용자 지시 — 프론티어 시험 기준).
    /// 카드: 사도 고유 카드(CardDef.Hero · Unique — 복제본 ^ 포함, 신탁 · 축복 · 카드 값 그대로)만 남긴다. 사도 기본 카드(Run.IsBasic — 고유 아닌 사도 카드),
    /// 교주 · 선물 카드(「@사도」 — 사도 고유가 아니다), 짐(주인 없는 상태 · 저주)은 뺀다.
    /// 장비: 칸마다 희귀 미만이거나 비었으면 같은 칸의 희귀 이상 장비 중 하나로(판 씨앗 · 사도 · 칸으로 정한 고정 무작위 — 다시 돌려도 같다).
    /// 반환: (바꾼 장비 수, 채운 빈 칸 수).
    /// </summary>
    static (int swap, int fill) Refine(GameData d, HeroSlot s)
    {
        s.Cards = s.Cards.Where(c => { var def = d.Card(c.Id); return def != null && def.Hero != null && def.Unique; }).ToList();
        int swap = 0, fill = 0;
        foreach (var slot in R.SLOTS)
        {
            bool has = s.Gear.TryGetValue(slot, out var cur);
            var e = has ? d.Equip(cur) : null;
            if (e != null && GRADE_RANK.TryGetValue(e.Grade ?? "", out var g) && g >= REFINE_GRADE) continue;
            var pool = d.Equips.Values.Where(x => x.Slot == slot && GRADE_RANK.TryGetValue(x.Grade ?? "", out var xg) && xg >= REFINE_GRADE)
                .Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (pool.Count == 0) continue;
            ulong h = 14695981039346656037UL;   // FNV-1a — 씨앗 · 사도 · 칸
            foreach (var ch in $"{s.From.Seed}:{s.Hero}:{slot}") { h ^= ch; h *= 1099511628211UL; }
            s.Gear[slot] = pool[(int)(h % (ulong)pool.Count)];
            if (has) swap++; else fill++;
        }
        var order = HeroSlot.SlotOrder(s.Gear.Keys);
        s.Gear = order.ToDictionary(k => k, k => s.Gear[k]);
        return (swap, fill);
    }

    /// <summary>
    /// 봇이 싸운다 —끝(Frontier.Over)까지 턴마다 손을 다 쓰고 넘긴다. smart = SmartPlay(판 봇과 같은 손) · simple = SimplePlay(생각 없는 손).
    /// 반환: 한 턴에 보스가 잃은 HP 의 최대(첫 턴은 연 직후 몫 포함 · 턴 끝의 지속 피해 · 반사 포함) — 서버 제출 상한(Docs/프론티어_서버.md §제출 상한)의 근거.
    /// </summary>
    static int Play(GameData data, Battle b, FrontierSeason season, string bot, long seed)
    {
        var bots = new Bots(data);
        uint a = unchecked((uint)seed * 2654435761u + 1u);
        double R() { a ^= a << 13; a ^= a >> 17; a ^= a << 5; return a / 4294967296.0; }
        int guard = 0, last = 0, max = 0;
        while (Frontier.Over(b, season) == null && guard++ < season.Turns + 5)
        {
            if (bot == "simple") bots.SimplePlay(b, R); else bots.SmartPlay(b);
            if (b.Over == null) b.EndTurn();
            int now = Frontier.Score(b);
            max = Math.Max(max, now - last); last = now;
        }
        return max;
    }

    static string Report(GameData data, FrontierSeason season, List<FightRec> all, List<SlotRec> pool, int runs, int clears, int bad)
    {
        var sb = new StringBuilder();
        var boss = data.Enemy(season.Boss);
        int bossHp = (int)Math.Round(boss.Hp * season.Hp);
        sb.AppendLine($"# 프론티어 점수 분포 — {boss.Name} · {season.Turns}턴 · 보스 HP {bossHp:N0}").AppendLine();
        sb.AppendLine($"봇 판 {runs} · 완주 {clears}({(runs > 0 ? 100.0 * clears / runs : 0):0.0}%) · 슬롯 {pool.Count}(사도 {pool.Select(s => s.Slot.Hero).Distinct().Count()}명) · 문제 {bad} · 프론티어 {all.Count} · 다시 돌리기 어긋남 {all.Count(r => !r.Same)}").AppendLine();
        sb.AppendLine("| 묶음 | 판 | 평균 | 가운데 | 하위 10% | 상위 10% | 최고 | 보스 HP 대비 평균 | 시간 끝 | 쓰러짐 | 보스 처치 | 평균 낸 턴 |").AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        void Row(string name, IEnumerable<FightRec> rows)
        {
            var l = rows.ToList(); if (l.Count == 0) return;
            var sc = l.Select(r => (double)r.Score).OrderBy(v => v).ToList();
            double Q(double q) => sc[Math.Min(sc.Count - 1, (int)Math.Floor(q * sc.Count))];
            double P(string o) => 100.0 * l.Count(r => r.Over == o) / l.Count;
            sb.AppendLine($"| {name} | {l.Count} | {sc.Average():N0} | {Q(0.5):N0} | {Q(0.1):N0} | {Q(0.9):N0} | {sc.Max():N0} | {100 * sc.Average() / Math.Max(1, bossHp):0.0}% | {P(Frontier.TIME):0}% | {P("lose"):0}% | {P("win"):0}% | {l.Average(r => r.Played):0.0} |");
        }
        Row("전체", all);
        foreach (var g in all.GroupBy(r => r.Bot).OrderBy(g => g.Key)) Row($"전투 봇 {g.Key}", g);
        foreach (var g in all.GroupBy(r => r.SlotBot).OrderBy(g => g.Key)) Row($"슬롯 봇 {g.Key}", g);
        foreach (var g in all.GroupBy(r => r.Comp).OrderBy(g => g.Key)) Row($"편성 {g.Key}", g);
        foreach (var g in all.GroupBy(r => (r.Bot, r.Comp)).OrderBy(g => g.Key)) Row($"{g.Key.Bot} · {g.Key.Comp}", g);
        foreach (var g in all.GroupBy(r => r.Roles).OrderByDescending(g => g.Count()).Take(8)) Row($"역할 {g.Key}", g);
        // 한 턴 최대 피해(보스가 한 턴에 잃은 HP) — 서버 제출 상한(seasons.turn_max)을 정하는 근거
        if (all.Count > 0)
        {
            var mt = all.Select(r => r.MaxTurn).OrderBy(v => v).ToList();
            int MQ(double q) => mt[Math.Min(mt.Count - 1, (int)Math.Floor(q * mt.Count))];
            var top = all.OrderByDescending(r => r.MaxTurn).First();
            sb.AppendLine().AppendLine($"한 턴 최대 피해 — 가운데 {MQ(0.5):N0} · 상위 10% {MQ(0.9):N0} · 상위 1% {MQ(0.99):N0} · 최고 {mt[^1]:N0}({top.Bot} · {top.Over} · {top.Played}턴 · {string.Join(", ", top.Heroes.Split(',').Select(k => data.Hero(k)?.Name ?? k))})");
        }
        sb.AppendLine().AppendLine("## 사도별(그 사도가 든 프론티어 · smart 봇)").AppendLine().AppendLine("| 사도 | 판 | 평균 | 최고 |").AppendLine("|---|---|---|---|");
        var smart = all.Where(r => r.Bot == "smart").ToList();
        foreach (var h in data.Heroes.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var l = smart.Where(r => r.Heroes.Split(',').Contains(h)).ToList();
            if (l.Count > 0) sb.AppendLine($"| {data.Hero(h).Name} | {l.Count} | {l.Average(r => r.Score):N0} | {l.Max(r => r.Score):N0} |");
        }
        sb.AppendLine().AppendLine("## 최고 점수 다섯(smart)").AppendLine();
        foreach (var r in smart.OrderByDescending(r => r.Score).Take(5)) sb.AppendLine($"- {r.Score:N0} · {r.Over} · {r.Played}턴 · {string.Join(", ", r.Heroes.Split(',').Select(k => data.Hero(k)?.Name ?? k))} · {r.Comp}");
        return sb.ToString();
    }

    static List<HeroSlot> LoadSlots(string files) => files.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(f => HeroSlot.Parse(File.ReadAllText(f))).ToList();

    static int OneFight(GameData data, FrontierSeason season, string files, long seed, string bot, string tapeOut, string progFile = null)
    {
        var sl = LoadSlots(files);
        // --prog-state 파일 — 서버가 시도를 열 때 떠 둔 성장(시작 응답의 prog · ProgState JSON). 그 성장으로 셋업을 짓는다(검증기와 같게)
        var prog = progFile != null ? GameData.FromJson<ProgState>(File.ReadAllText(progFile)) : null;
        if (prog != null) Console.WriteLine($"잠재력 — {string.Join(" · ", prog.Heroes.Select(kv => $"{kv.Key} {kv.Value.Pot.Values.Sum()}단계"))}{(data.Prog == null ? "(콘텐츠에 성장 표가 없어 셋업에 안 들어감)" : "")}");
        foreach (var s in sl) Console.WriteLine($"{s.Hero} — {HeroSlot.Check(data, s)} · 카드 {s.Cards.Count} · 장비 {string.Join(",", s.Gear.Values)}");
        var why = Frontier.WhyNot(data, sl, season);
        if (why != null) { Console.WriteLine("열 수 없다 — " + why); return 1; }
        var b = Frontier.Start(data, sl, season, seed, prog: prog);
        int maxTurn = Play(data, b, season, bot, seed);
        var tape = Frontier.Tape(b, season, sl);
        var chk = Frontier.Verify(data, FrontierTape.Parse(tape.Json()), season, sl, prog);
        Console.WriteLine($"점수 {tape.Score:N0} · 끝 {tape.Over} · 낸 턴 {tape.Played} · 한 턴 최대 {maxTurn:N0} · 파티 HP {Math.Max(0, b.Pool.Hp)}/{b.Pool.MaxHp} · 다시 돌리기 {(chk.Ok ? "같음" : "다름 — " + chk.Why)}");
        if (tapeOut != null) { File.WriteAllText(tapeOut, tape.Json()); Console.WriteLine($"→ {Path.GetFullPath(tapeOut)}"); }
        return chk.Ok ? 0 : 1;
    }

    static int VerifyFile(GameData data, FrontierSeason season, string path, string files)
    {
        var tape = FrontierTape.Parse(File.ReadAllText(path));
        var chk = Frontier.Verify(data, tape, season, files != null ? LoadSlots(files) : null);
        Console.WriteLine($"{Path.GetFileName(path)} — {(chk.Ok ? "같음" : "다름 — " + chk.Why)} · 점수 {tape.Score:N0} → {chk.Score:N0} · 끝 {tape.Over} → {chk.Over}{(chk.Turn >= 0 ? $" · 어긋난 턴 지문 {chk.Turn}" : "")}");
        return chk.Ok ? 0 : 1;
    }
}
