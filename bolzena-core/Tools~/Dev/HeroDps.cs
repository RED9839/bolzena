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
/// 사도별 개별 딜(2026-10-19 사용자 지시 — 「M.E.O.W 에서 기본 카드 다 빼고 신탁 받은 고유 카드로 사도마다 딜 체크」). 측정 전용.
///   herodps [--fights 400] [--oracle best|1..5|all] [--fixed a,b[;c,d]] [--fightbot smart|simple] [--seed 0] [--threads 8] [--verify 50] [--out 파일.md] [--tsv 파일]
///   슬롯: 그 사도의 고유 카드(CardDef.Hero · Unique, 토큰 · 복제본 빼고) 한 장씩 전부 · 기본 카드 없음 · 교주 카드 없음 · 축복 없음 · 성장 없음.
///         신탁 — best: 숙련 봇과 같은 값(DeckPlan(그 사도 혼자).Score)으로 카드마다 가장 좋은 신탁 · n: 모든 카드에 신탁 n(완성형 카드는 신탁 없음) · all: best + 1..5.
///         장비 — 칸마다 희귀 이상 장비 중 하나(싸움 씨앗 · 사도 · 칸 FNV — FrontierSim --refined 와 같은 방식).
///   편성: (a) 사도마다 무작위 동료 둘(동료도 같은 규칙의 슬롯 · best 신탁) --fights 판. (b) --fixed 고정 동료 둘(그 사도가 동료 중에 있으면 다음 짝 「;」).
///   개별 딜 = Battle.HpMeter(측정 전용 — 보스가 잃은 HP 를 From/Credit 사도 몫으로) · 드론 딜 = 보스 아닌 적이 잃은 HP. 주인 모를 몫은 「?」.
///   --cards 파일.md [--cards-tsv 파일]: 시험 사도의 보스 딜을 출처별로(Battle.HpSrc — 한 대 종류 · ModSrc) — 고유 카드마다 · 고학년 · 지속 피해 · 반격 · 패시브 · 키워드 · 토큰 카드 · 기타.
///   --verify K: K 판마다 테이프를 다시 돌려 점수 · 끝 · 지문이 같은지(측정 코드가 판을 안 바꿨는지).
/// </summary>
static class HeroDps
{
    sealed class Rec { public string Hero, Mode, Partners; public int Oracle; public long Boss, Drone, Unknown; public int Score, Played, Output; public string Over; public Dictionary<string, long> Mates; public Dictionary<string, long> Src; public Dictionary<string, int> Plays; }

    static readonly Dictionary<string, int> GRADE_RANK = new() { ["일반"] = 0, ["고급"] = 1, ["희귀"] = 2, ["전설"] = 3 };

    public static List<string> Uniques(GameData d, string hero) => d.Cards.Values
        .Where(c => c.Hero == hero && c.Unique && !c.Token && !c.IsCurse && !c.IsStatusCard && !GameData.IsCopy(c.Id) && !GameData.IsPlain(c.Id) && GameData.NoInst(c.Id) == c.Id && GameData.OwnerOf(c.Id) == null)
        .Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

    /// <summary>oracle 0 = best(봇 값), n = 모든 카드 신탁 n.</summary>
    public static HeroSlot Build(GameData d, string hero, int oracle, long gearSeed)
    {
        var s = new HeroSlot { Hero = hero, Rules = Fingerprint.Rules(), Content = d.ContentHash, From = new SlotFrom { Seed = gearSeed, Result = "clear", Floor = 2, Party = new List<string> { hero } } };
        var plan = new DeckPlan(d, new[] { hero });
        foreach (var id in Uniques(d, hero))
        {
            var def = d.Card(id);
            int o = 0;
            if (!def.Complete && def.Oracles.Count > 0)
            {
                if (oracle > 0) o = Math.Min(oracle, def.Oracles.Count);
                else { double bv = double.MinValue; for (int n = 1; n <= def.Oracles.Count; n++) { double v = plan.Score(d.View(id, n)); if (v > bv) { bv = v; o = n; } } }
            }
            s.Cards.Add(new SlotCard { Id = id, O = o });
        }
        foreach (var slot in R.SLOTS)
        {
            var pool = d.Equips.Values.Where(x => x.Slot == slot && GRADE_RANK.TryGetValue(x.Grade ?? "", out var g) && g >= 2).Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (pool.Count == 0) continue;
            ulong h = 14695981039346656037UL;
            foreach (var ch in $"{gearSeed}:{hero}:{slot}") { h ^= ch; h *= 1099511628211UL; }
            s.Gear[slot] = pool[(int)(h % (ulong)pool.Count)];
        }
        return s;
    }

    static int Play(GameData data, Battle b, FrontierSeason season, string bot, long seed)
    {
        var bots = new Bots(data);
        uint a = unchecked((uint)seed * 2654435761u + 1u);
        double Rr() { a ^= a << 13; a ^= a >> 17; a ^= a << 5; return a / 4294967296.0; }
        int guard = 0;
        while (Frontier.Over(b, season) == null && guard++ < season.Turns + 5)
        {
            if (bot == "simple") bots.SimplePlay(b, Rr); else bots.SmartPlay(b);
            if (b.Over == null) b.EndTurn();
        }
        return guard;
    }

    public static int Run(GameData data, List<string> args, Func<string, string, string> opt, Func<string, bool> flag)
    {
        var season = Frontier.PRESEASON;
        if (data.Enemy(season.Boss) == null) { Console.WriteLine($"시즌 보스 「{season.Boss}」 가 없다 — --data C:\\projects\\bolzena-content-v2"); return 1; }
        int fights = int.Parse(opt("fights", "400")), seed = int.Parse(opt("seed", "0")), threads = int.Parse(opt("threads", "8")), verifyEvery = int.Parse(opt("verify", "50"));
        string cardsMd = opt("cards", null), cardsTsv = opt("cards-tsv", null);
        string oracleOpt = opt("oracle", "best"), fixedOpt = opt("fixed", null), bot = opt("fightbot", "smart"), outMd = opt("out", null), tsv = opt("tsv", null);
        bool list = flag("list");
        string progName = opt("prog", null);   // 사도 영구 성장 측정 묶음(none · half · max …) — 편성 셋 모두 같은 묶음
        if (progName != null && (data.Prog == null || !data.Prog.Presets.ContainsKey(progName))) { Console.WriteLine($"--prog 묶음 「{progName}」 이 성장 표에 없다"); return 1; }
        var heroes = data.Heroes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (list)
        {
            foreach (var h in heroes)
            {
                var hd = data.Hero(h); var s = Build(data, h, 0, 1);
                Console.WriteLine($"{h}\t{hd.Name}\t{hd.Role}\t{hd.Nature}\t{string.Join(" ", s.Cards.Select(c => $"{c.Id}:o{c.O}/{data.Card(c.Id).Oracles.Count}{(data.Card(c.Id).Complete ? "완" : "")}"))}\t{HeroSlot.Check(data, s)}");
            }
            return 0;
        }
        var oracles = oracleOpt == "all" ? new[] { 0, 1, 2, 3, 4, 5 } : new[] { oracleOpt == "best" ? 0 : int.Parse(oracleOpt) };
        var fixedSets = fixedOpt?.Split(';').Select(x => x.Split(',').Select(y => y.Trim()).ToArray()).ToList();
        foreach (var fs in fixedSets ?? new()) foreach (var k in fs) if (data.Hero(k) == null) { Console.WriteLine($"없는 사도 {k}"); return 1; }
        KwMeter.On = false; Battle.HpMeterOn = true;
        var sw = Stopwatch.StartNew();

        var plan = new List<(string hero, int oracle, string mode, string[] mates, long seed)>();
        uint rs = unchecked((uint)(4242 + seed * 7919));
        int Rnd(int m) { rs = unchecked(rs * 1103515245u + 12345u); return (int)((rs >> 8) % (uint)m); }
        foreach (var o in oracles)
            foreach (var h in heroes)
                for (int i = 0; i < fights; i++)
                {
                    long fseed = 700000 + (long)seed * 1000003 + heroes.IndexOf(h) * 100003L + i * 7919L + o * 13L;
                    string[] mates;
                    string mode;
                    if (fixedSets != null)
                    {
                        mates = fixedSets.FirstOrDefault(fs => !fs.Contains(h)) ?? fixedSets[0];
                        mode = "고정";
                    }
                    else
                    {
                        var others = heroes.Where(x => x != h).ToList();
                        int a = Rnd(others.Count), b; do b = Rnd(others.Count); while (b == a);
                        mates = new[] { others[a], others[b] };
                        mode = "무작위";
                    }
                    plan.Add((h, o, mode, mates, fseed));
                }

        var recs = new ConcurrentBag<Rec>();
        var bad = new ConcurrentBag<string>();
        int done = 0, verified = 0;
        Parallel.ForEach(Enumerable.Range(0, plan.Count), new ParallelOptions { MaxDegreeOfParallelism = threads }, idx =>
        {
            var p = plan[idx];
            // 편성 차례 — 시험 사도를 셋 중 씨앗으로 정한 자리에(앞 · 가운데 · 뒤가 고르게)
            var order = new List<string>(p.mates); order.Insert((int)(p.seed % 3), p.hero);
            var sl = order.Select(k => Build(data, k, k == p.hero ? p.oracle : 0, p.seed)).ToList();
            var why = Frontier.WhyNot(data, sl, season);
            if (why != null) { bad.Add($"{p.hero} — {why}"); return; }
            var prog = progName != null ? Prog.Preset(data, data.Prog, progName, order) : null;
            var b = Frontier.Start(data, sl, season, p.seed, prog: prog);
            b.Tape = new BattleTape();   // 낸 카드 세기 — 기록만(판 · 점수 · 지문 무관, 봇 복사본엔 안 따라감)
            Play(data, b, season, bot, p.seed);
            var tape = Frontier.Tape(b, season, sl);
            var m = b.HpMeter ?? new Dictionary<string, long[]>();
            long Get(string k, int i) => m.TryGetValue(k, out var a) ? a[i] : 0;
            long sum = m.Values.Sum(a => a[0]);
            if (sum != tape.Score) bad.Add($"집계 합 {sum} ≠ 점수 {tape.Score}({p.hero} 씨앗 {p.seed})");
            if (verifyEvery > 0 && idx % verifyEvery == 0)
            {
                var chk = Frontier.Verify(data, FrontierTape.Parse(tape.Json()), season, sl.Select(s => HeroSlot.Parse(s.Json())).ToList(), prog);
                if (!(chk.Ok && chk.Score == tape.Score && chk.Over == tape.Over)) bad.Add($"다시 돌리기 다름 {p.hero} 씨앗 {p.seed} — {chk.Why}");
                Interlocked.Increment(ref verified);
            }
            recs.Add(new Rec
            {
                Hero = p.hero, Oracle = p.oracle, Mode = p.mode, Partners = string.Join(",", p.mates), Boss = Get(p.hero, 0), Drone = Get(p.hero, 1), Unknown = Get("", 0),
                Score = tape.Score, Played = tape.Played, Over = tape.Over, Output = b.Enemies.Where(e => e.Key == season.Boss).Select(e => b.St(e, "출력")).DefaultIfEmpty(0).Max(),   // 측정 전용 — 끝난 때 보스 「출력」(맞을 때마다 +1, 2026-10-19 타수 줄이기 비교)
                Mates = p.mates.ToDictionary(k => k, k => Get(k, 0)),
                Src = (b.HpSrc ?? new()).Where(kv => kv.Key.StartsWith(p.hero + "\t", StringComparison.Ordinal) && kv.Value[0] > 0).ToDictionary(kv => kv.Key.Substring(p.hero.Length + 1), kv => kv.Value[0]),
                Plays = b.Tape.Turns.SelectMany(tr => tr.C).Select(c => GameData.BaseId(GameData.NoInst(c.TrimStart('+').Split('/')[0]))).Where(c => data.Card(c)?.Hero == p.hero).GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count()),
            });
            int dn = Interlocked.Increment(ref done);
            if (dn % 2000 == 0) Console.WriteLine($"  {dn}/{plan.Count} · {sw.Elapsed.TotalSeconds:0}초");
        });
        Battle.HpMeterOn = false;
        var all = recs.ToList();
        Console.WriteLine($"herodps — {season.Boss} · {season.Turns}턴 · 성장 {progName ?? "없음"} · 싸움 {all.Count} · 다시 돌리기 확인 {verified} · 문제 {bad.Count} · 규칙 {Fingerprint.Rules()} · 콘텐츠 {data.ContentHash ?? "-"} · {sw.Elapsed.TotalSeconds:0}초");
        foreach (var s in bad.Take(20)) Console.WriteLine("  " + s);

        var sb = new StringBuilder();
        var t = new StringBuilder("hero\tname\trole\tnature\toracle\tmode\tn\tavgBoss\tperTurn\tp50\tp90\tsharePct\tlosePct\tavgScore\tavgPlayed\tavgDrone\tuniques\n");
        foreach (var o in oracles)
        {
            sb.AppendLine($"## 신탁 {(o == 0 ? "best(봇 값)" : o.ToString())} · {(fixedSets != null ? "고정 동료 " + fixedOpt : "무작위 동료 둘")} · {bot}").AppendLine();
            sb.AppendLine("| 사도 | 역할 | 성격 | 판 | 평균 개별 딜 | 턴당 | 가운데 | 상위 10% | 팀 점수 중 | 쓰러짐 | 팀 평균 점수 | 낸 턴 | 드론 딜 | 고유 카드 | 끝 출력 |").AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var h in heroes)
            {
                var l = all.Where(r => r.Hero == h && r.Oracle == o).ToList(); if (l.Count == 0) continue;
                var hd = data.Hero(h);
                var v = l.Select(r => (double)r.Boss).OrderBy(x => x).ToList();
                double Q(double q) => v[Math.Min(v.Count - 1, (int)Math.Floor(q * v.Count))];
                double avg = v.Average(), perTurn = l.Sum(r => (double)r.Boss) / Math.Max(1, l.Sum(r => r.Played));
                double share = 100.0 * l.Sum(r => (double)r.Boss) / Math.Max(1, l.Sum(r => (double)r.Score));
                double lose = 100.0 * l.Count(r => r.Over == "lose") / l.Count;
                int nu = Uniques(data, h).Count;
                sb.AppendLine($"| {hd.Name} | {hd.Role} | {hd.Nature} | {l.Count} | {avg:N0} | {perTurn:N0} | {Q(0.5):N0} | {Q(0.9):N0} | {share:0.0}% | {lose:0}% | {l.Average(r => r.Score):N0} | {l.Average(r => r.Played):0.0} | {l.Average(r => r.Drone):N0} | {nu} | {l.Average(r => r.Output):0.0} |");
                t.Append(string.Join("\t", h, hd.Name, hd.Role, hd.Nature, o, l[0].Mode, l.Count, avg.ToString("0"), perTurn.ToString("0"), Q(0.5).ToString("0"), Q(0.9).ToString("0"), share.ToString("0.0"), lose.ToString("0.0"),
                    l.Average(r => r.Score).ToString("0"), l.Average(r => r.Played).ToString("0.00"), l.Average(r => r.Drone).ToString("0"), nu)).Append('\n');
            }
            var unk = all.Where(r => r.Oracle == o).ToList();
            sb.AppendLine().AppendLine($"주인 모를 몫(교주 · 장비 · 사건 · 반사 등): 점수의 {100.0 * unk.Sum(r => (double)r.Unknown) / Math.Max(1, unk.Sum(r => (double)r.Score)):0.0}%").AppendLine();
            // 동료로 들어갔을 때(무작위) — 그 사도가 동료 자리에서 낸 딜(best 신탁) — 시험 사도 자리와 견줄 표본 늘리기
            if (o == 0 && fixedSets == null)
            {
                sb.AppendLine("| 사도 | 동료 자리 판 | 동료 자리 평균 개별 딜 |").AppendLine("|---|---|---|");
                foreach (var h in heroes)
                {
                    var l = unk.Where(r => r.Mates.ContainsKey(h)).Select(r => (double)r.Mates[h]).ToList();
                    if (l.Count > 0) sb.AppendLine($"| {data.Hero(h).Name} | {l.Count} | {l.Average():N0} |");
                }
                sb.AppendLine();
            }
        }
        Console.WriteLine(sb.ToString());
        if (outMd != null) { File.WriteAllText(outMd, sb.ToString()); Console.WriteLine($"→ {Path.GetFullPath(outMd)}"); }
        if (tsv != null) { File.WriteAllText(tsv, t.ToString()); Console.WriteLine($"→ {Path.GetFullPath(tsv)}"); }
        if (cardsMd != null) Cards(data, all.Where(r => r.Oracle == oracles[0]).ToList(), heroes, cardsMd, cardsTsv);
        return bad.Count == 0 ? 0 : 1;
    }

    static readonly Dictionary<string, string> SHOT_KO = new() { ["bullet"] = "탄환", ["assist"] = "협공", ["resonance"] = "공명", ["extra"] = "추가 공격", ["summon"] = "소환물", ["pure"] = "고정 지속" };

    /// <summary>출처 열쇠(한 대 종류 \t ModSrc) → (묶음 열쇠, 보일 이름). card:id · ult · dot · counter · token:이름 · kw:이름 · passive:이름 · mate:이름 · other:글.</summary>
    static (string key, string label) Classify(GameData d, string hero, Dictionary<string, string> uniqByName, string srcKey)
    {
        var parts = srcKey.Split('\t');
        string kind = parts[0], ms = parts.Length > 1 ? parts[1] : "";
        var hd = d.Hero(hero);
        if (kind == "dot") return ("dot", "지속 피해");
        if (kind == "counter") return ("counter", "반격");
        string shot = kind == "hit" ? null : SHOT_KO.TryGetValue(kind, out var sk) ? sk : kind;
        if (ms.StartsWith("@턴끝:")) return ("kwend:" + ms.Substring(4), $"키워드 턴 끝 피해 「{ms.Substring(4)}」");
        if (ms.StartsWith("@탄환:")) return ("bullet:" + ms.Substring(4), $"「{ms.Substring(4)}」 탄환(버려질 때)" + (uniqByName.ContainsKey(ms.Substring(4)) ? "" : " — 토큰"));
        if (ms.StartsWith("낭독 「")) return ("replay:" + ms, $"{ms}(남이 다시 발동)");
        string power = null, passive = null, named = null;
        int pw = ms.IndexOf(" · 강화 「", StringComparison.Ordinal);
        if (pw >= 0) power = ms.Substring(pw + " · 강화 「".Length).TrimEnd('」');
        else if (ms.Contains(" · ")) passive = ms.Substring(ms.LastIndexOf(" · ", StringComparison.Ordinal) + 3);
        else if (ms.Contains("「"))
        {
            int a = ms.IndexOf('「'), z = ms.IndexOf('」', a + 1);
            if (z > a) named = ms.Substring(a + 1, z - a - 1);
        }
        string nm = power ?? named;
        if (nm != null && uniqByName.TryGetValue(nm, out var cid)) return ("card:" + cid, null);
        if (named != null && hd.Ult != null && named == hd.Ult.Name) return ("ult", "고학년");
        if (nm != null && d.Cards.Values.Any(c => c.Hero == hero && c.Name == nm)) return ("token:" + nm, $"토큰 카드 「{nm}」");
        if (nm != null) return ("mate:" + nm + "|" + kind, $"남의 카드 중 「{nm}」" + (shot != null ? $"({shot})" : ""));
        if (passive != null)
            return hd.AllKeywords.Any(k => k.Name == passive) ? ("kw:" + passive, $"키워드 「{passive}」")
                : hd.Passives.Any(r => r.Name == passive) ? ("passive:" + passive, $"패시브 「{passive}」")
                : hd.AllKeywords.FirstOrDefault(k => (k.Rules ?? new()).Any(r => r.Name == passive)) is KeywordDef kk ? ("kw:" + passive, $"키워드 「{kk.Name}」 규칙 「{passive}」")
                : (hd.Forms ?? new()).FirstOrDefault(f => (f.Passives ?? new()).Any(r => r.Name == passive)) is FormDef ff ? ("form:" + passive, $"변신 「{ff.Name}」 패시브 「{passive}」")
                : d.Equips.Values.Any(e => e.Effect.Concat(e.AffinityEffect ?? new()).Any(r => r.Name == passive)) ? ("gear:" + passive, $"장비 「{passive}」")
                : ("passive:" + passive, $"패시브(엔진 이름) 「{passive}」");
        if (ms == "") return ("other:" + kind, shot != null ? $"기타(출처 없는 {shot})" : "기타(출처 없음)");
        return ("other:" + ms, $"기타({ms})");
    }

    static string Coef(GameData d, string id, int o)
    {
        var v = d.View(id, o);
        try { return new CardText(d).Card(v).Replace("\r", "").Replace("\n", " / ").Replace("|", "/"); } catch (Exception e) { return "(글 못 지음: " + e.Message + ")"; }
    }

    static void Cards(GameData d, List<Rec> all, List<string> heroes, string md, string tsvPath)
    {
        var sb = new StringBuilder();
        var tsv = new StringBuilder("hero\tname\tsrc\tlabel\toracle\tcost\tplaysPerFight\tdmgPerFight\tdmgPerPlay\tdmgPerCost\tsharePct\ttext\n");
        var detail = new StringBuilder();
        sb.AppendLine("# 사도별 · 카드별 보스 딜(측정 전용 HpSrc)").AppendLine();
        sb.AppendLine("| 사도 | 역할 | 판 | 평균 개별 딜 | 턴당 | 팀 점수 중 | 쓰러짐 | 팀 평균 점수 | 고학년 판당 | 1위 출처 | 2위 출처 |").AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var h in heroes)
        {
            var l = all.Where(r => r.Hero == h).ToList(); if (l.Count == 0) continue;
            var hd = d.Hero(h); int n = l.Count;
            var slot = Build(d, h, l[0].Oracle, 1);
            var uniqByName = new Dictionary<string, string>();
            foreach (var c in slot.Cards) uniqByName[d.Card(c.Id).Name] = c.Id;
            var agg = new Dictionary<string, (string label, long v)>();
            foreach (var r in l) foreach (var kv in r.Src)
            {
                var (k, lab) = Classify(d, h, uniqByName, kv.Key);
                agg[k] = agg.TryGetValue(k, out var e) ? (e.label ?? lab, e.v + kv.Value) : (lab, kv.Value);
            }
            long tot = agg.Values.Sum(x => x.v), boss = l.Sum(r => r.Boss);
            string Lab(string k) => k.StartsWith("card:") ? $"{k.Substring(k.LastIndexOf('_') + 1)} 「{d.Card(k.Substring(5)).Name}」" : agg[k].label;
            var top = agg.OrderByDescending(kv => kv.Value.v).Take(2).Select(kv => $"{Lab(kv.Key)} {100.0 * kv.Value.v / Math.Max(1, tot):0}%").ToList();
            while (top.Count < 2) top.Add("-");
            sb.AppendLine($"| {hd.Name} | {hd.Role} | {n} | {(double)boss / n:N0} | {boss / Math.Max(1.0, l.Sum(r => r.Played)):N0} | {100.0 * boss / Math.Max(1.0, l.Sum(r => (double)r.Score)):0.0}% | {100.0 * l.Count(r => r.Over == "lose") / n:0}% | {l.Average(r => r.Score):N0} | {(agg.TryGetValue("ult", out var ug) ? ug.v : 0) / (double)n:N0} | {top[0]} | {top[1]} |");
            if (tot != boss) detail.AppendLine($"> 경고: {hd.Name} 출처 합 {tot} ≠ 개별 딜 합 {boss}").AppendLine();

            detail.AppendLine($"## {hd.Name} — 판당 {(double)boss / n:N0}").AppendLine();
            detail.AppendLine("| 출처 | 신탁 | 코스트 | 판당 낸 횟수 | 판당 피해 | 한 번 평균 | 코스트 1당 | 딜 중 |").AppendLine("|---|---|---|---|---|---|---|---|");
            var texts = new List<string>();
            foreach (var c in slot.Cards)
            {
                var v = d.View(c.Id, c.O);
                long dmg = agg.TryGetValue("card:" + c.Id, out var e) ? e.v : 0;
                double plays = l.Sum(r => r.Plays.TryGetValue(c.Id, out var pc) ? pc : 0) / (double)n;
                double perFight = dmg / (double)n, perPlay = plays > 0 ? perFight / plays : 0;
                string per1 = v.Cost > 0 ? (perPlay / v.Cost).ToString("N0") : "(0코)";
                string on = c.O > 0 ? d.Card(c.Id).Oracles[c.O - 1].Name : d.Card(c.Id).Complete ? "완성형" : "-";
                string tag = c.Id.Substring(c.Id.LastIndexOf('_') + 1);
                detail.AppendLine($"| {tag} 「{d.Card(c.Id).Name}」({v.Type}) | {on} | {v.Cost} | {plays:0.00} | {perFight:N0} | {perPlay:N0} | {per1} | {100.0 * dmg / Math.Max(1, tot):0.0}% |");
                string txt = Coef(d, c.Id, c.O);
                texts.Add($"- {tag} 「{d.Card(c.Id).Name}」({on}): {txt}");
                tsv.Append(string.Join("\t", h, hd.Name, c.Id, d.Card(c.Id).Name, c.O, v.Cost, plays.ToString("0.00"), perFight.ToString("0"), perPlay.ToString("0"), v.Cost > 0 ? (perPlay / v.Cost).ToString("0") : "", (100.0 * dmg / Math.Max(1, tot)).ToString("0.0"), txt)).Append('\n');
            }
            foreach (var kv in agg.Where(kv => !kv.Key.StartsWith("card:")).OrderByDescending(kv => kv.Value.v))
            {
                double perFight = kv.Value.v / (double)n;
                if (kv.Key.StartsWith("token:"))
                {   // 토큰 카드 — 이름으로 낸 횟수 · 글
                    string tn = kv.Key.Substring(6);
                    var tdef = d.Cards.Values.FirstOrDefault(c => c.Hero == h && c.Name == tn);
                    double tp = l.Sum(r => r.Plays.Where(pc => d.Card(pc.Key)?.Name == tn).Sum(pc => pc.Value)) / (double)n;
                    detail.AppendLine($"| {kv.Value.label} | | {tdef?.Cost} | {tp:0.00} | {perFight:N0} | {(tp > 0 ? perFight / tp : 0):N0} | {(tdef != null && tdef.Cost > 0 && tp > 0 ? (perFight / tp / tdef.Cost).ToString("N0") : "(0코)")} | {100.0 * kv.Value.v / Math.Max(1, tot):0.0}% |");
                    if (tdef != null) texts.Add($"- 토큰 「{tn}」: {Coef(d, tdef.Id, 0)}");
                }
                else detail.AppendLine($"| {kv.Value.label} | | | | {perFight:N0} | | | {100.0 * kv.Value.v / Math.Max(1, tot):0.0}% |");
                tsv.Append(string.Join("\t", h, hd.Name, kv.Key, kv.Value.label, "", "", "", perFight.ToString("0"), "", "", (100.0 * kv.Value.v / Math.Max(1, tot)).ToString("0.0"), "")).Append('\n');
            }
            if (hd.Ult != null) texts.Add($"- 고학년 「{hd.Ult.Name}」: {new CardText(d).Ult(hd.Ult).Replace("\r", "").Replace("\n", " / ").Replace("|", "/")}");
            detail.AppendLine().AppendLine("카드 글(신탁 적용 뒤):").AppendLine();
            foreach (var x in texts) detail.AppendLine(x);
            detail.AppendLine();
        }
        sb.AppendLine().Append(detail);
        File.WriteAllText(md, sb.ToString()); Console.WriteLine($"→ {Path.GetFullPath(md)}");
        if (tsvPath != null) { File.WriteAllText(tsvPath, tsv.ToString()); Console.WriteLine($"→ {Path.GetFullPath(tsvPath)}"); }
    }
}
