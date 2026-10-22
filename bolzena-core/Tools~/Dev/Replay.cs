using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 입력 테이프 다시 돌리기(프론티어 1단계 — bolzena-unity/Docs/프론티어_설계.md §7).
///   replay [판 54] [--seed 0] [--threads 8] [--bot basic|skilled] [--party random|role|synergy] [--preview] [--fine] [--out 폴더]
///     봇 판을 돌리며 전투마다 테이프를 남기고(Run.OpenFight 가 단다), 테이프를 JSON 으로 썼다 읽어 처음부터 다시 돌려 턴 지문 · 끝 지문이 모두 같은지 본다.
///     --preview: 다시 돌릴 때 수마다 화면 · 봇 미리보기(BattleInput.Poke)를 끼운다. --out: 테이프 파일을 남긴다(유니티 교차 시험 · bz replay 파일).
///     --fine: 수마다 그 수 앞 지문도 적어(InputOp.Pre) 어긋난 수를 한 수 단위로 찾는다.
///   replay 파일.json|폴더 — 남긴 테이프를 다시 돌려 지문을 견준다(규칙 · 콘텐츠 지문이 다르면 알린다).
/// </summary>
static class Replay
{
    public static int Run(GameData data, List<string> args, Func<string, string, string> opt, Func<string, bool> flag, Func<(SimOpts bot, string party, string err)> botOpts)
    {
        bool preview = flag("preview");
        BattleInput.Fine = flag("fine");   // 수마다 앞 지문(어긋난 수 찾기)
        if (args.Count > 0 && (File.Exists(args[0]) || Directory.Exists(args[0]))) return Files(data, args[0], preview);
        int seed = int.Parse(opt("seed", "0")), threads = int.Parse(opt("threads", "8"));
        string outDir = opt("out", null);
        var (bot, party, err) = botOpts(); if (err != null) { Console.WriteLine(err); return 1; }
        int n = args.Count > 0 && int.TryParse(args[0], out var x) ? x : 54;
        int per = Math.Max(1, data.Heroes.Count / 3);
        var jobs = PartyPick.Jobs(data, (n + per - 1) / per, seed, party).Take(n).ToList();
        if (outDir != null) Directory.CreateDirectory(outDir);
        Console.WriteLine($"replay — 판 {jobs.Count} · 봇 {(bot.Skilled ? "숙련" : "초보")} · 편성 {party} · 성장 {bot.Prog ?? "없음"} · 미리보기 {(preview ? "끼움" : "안 끼움")} · 규칙 {Fingerprint.Rules()} · 콘텐츠 {data.ContentHash ?? "-"}");
        int fights = 0, same = 0, ops = 0, turns = 0, cut = 0, runs = 0;
        long bytes = 0, recTicks = 0;
        var bad = new ConcurrentBag<string>();
        var sw = Stopwatch.StartNew();
        Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = threads }, j =>
        {
            var got = new List<Battle>();
            var rb = new RunBot(data);
            var P = new SimOpts { Skilled = bot.Skilled, UniqueOnly = bot.UniqueOnly, Prog = bot.Prog, OnFight = (b, run) => got.Add(b) };
            rb.RunFull(j.party, j.seed, P);
            Interlocked.Increment(ref runs);
            var bots = preview ? new Bots(data) : null;
            for (int k = 0; k < got.Count; k++)
            {
                var b = got[k];
                var tape = b.Input;
                if (tape == null) { bad.Add($"판 {j.i} 전투 {k} — 테이프 없음"); continue; }
                long t0 = Stopwatch.GetTimestamp();
                var json = tape.Json();
                Interlocked.Add(ref recTicks, Stopwatch.GetTimestamp() - t0);
                Interlocked.Add(ref bytes, json.Length);
                var back = BattleInput.Parse(json);
                var r = BattleInput.Replay(data, back, preview ? (Action<Battle>)(s => BattleInput.Poke(s, bots)) : null);
                Interlocked.Increment(ref fights); Interlocked.Add(ref ops, tape.Ops.Count); Interlocked.Add(ref turns, tape.Digests.Count - 1);
                // 봇이 40턴에서 끊은 싸움 — 엔진 밖에서 lose 로 정한다. 지문은 같아야 하고 결과만 비어 있다
                bool cutOff = tape.Over == "lose" && r.Battle.Over == null;
                if (cutOff) Interlocked.Increment(ref cut);
                if (r.Same && (cutOff || r.Battle.Over == tape.Over)) Interlocked.Increment(ref same);
                else bad.Add($"판 {j.i}({string.Join(",", j.party)} · 씨앗 {j.seed}) 전투 {k} — {(r.Same ? $"결과 {tape.Over} → {r.Battle.Over}" : r.Why)}");
                if (outDir != null) File.WriteAllText(Path.Combine(outDir, $"tape_{(bot.Prog != null ? bot.Prog + "_" : "")}{j.i:000}_{k:00}.json"), json);   // 성장 묶음 테이프는 이름에 묶음(같은 폴더에 섞어 둔다)
            }
        });
        sw.Stop();
        Console.WriteLine($"판 {runs} · 전투 {fights} · 같음 {same} · 다름 {fights - same} · 수 {ops} · 턴 {turns} · 봇 40턴 끊김 {cut} · 테이프 평균 {(fights > 0 ? bytes / fights / 1024.0 : 0):0.0}KB · JSON 쓰기 합 {recTicks * 1000.0 / Stopwatch.Frequency:0}ms · {sw.Elapsed.TotalSeconds:0.0}초");
        foreach (var s in bad.Take(30)) Console.WriteLine("  다름 " + s);
        if (outDir != null) Console.WriteLine($"→ {Path.GetFullPath(outDir)}");
        return fights > 0 && same == fights ? 0 : 1;
    }

    static int Files(GameData data, string path, bool preview)
    {
        var files = File.Exists(path) ? new List<string> { path } : Directory.GetFiles(path, "*.json").OrderBy(x => x, StringComparer.Ordinal).ToList();
        string rules = Fingerprint.Rules();
        int same = 0, rulesOff = 0, contentOff = 0;
        var bots = preview ? new Bots(data) : null;
        foreach (var f in files)
        {
            var tape = BattleInput.Parse(File.ReadAllText(f));
            if (tape.Rules != rules) rulesOff++;
            if (tape.Content != null && tape.Content != data.ContentHash) contentOff++;
            var r = BattleInput.Replay(data, tape, preview ? (Action<Battle>)(s => BattleInput.Poke(s, bots)) : null);
            bool cutOff = tape.Over == "lose" && r.Battle.Over == null;
            if (r.Same && (cutOff || r.Battle.Over == tape.Over)) same++;
            else Console.WriteLine($"  다름 {Path.GetFileName(f)} — {(r.Same ? $"결과 {tape.Over} → {r.Battle.Over}" : $"{r.Why} · 턴 지문 {r.Turn}")}");
            if (files.Count == 1) Console.WriteLine($"{Path.GetFileName(f)} — 수 {tape.Ops.Count} · 턴 {tape.Digests.Count - 1} · 결과 {tape.Over} · 끝 지문 {tape.End} → {r.Battle.Input.End}");
        }
        Console.WriteLine($"테이프 {files.Count} · 같음 {same} · 다름 {files.Count - same}" + (rulesOff > 0 ? $" · 규칙 지문 다름 {rulesOff}(지금 {rules})" : "") + (contentOff > 0 ? $" · 콘텐츠 지문 다름 {contentOff}(지금 {data.ContentHash})" : ""));
        return same == files.Count ? 0 : 1;
    }
}
