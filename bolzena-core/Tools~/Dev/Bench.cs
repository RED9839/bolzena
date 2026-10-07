using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bolzena.Core;

/// <summary>
/// 엔진 성능 벤치 — 같은 씨앗으로 판을 돌려(숙련 아닌 기본 봇) 싸움마다 시작 상태를 떠 두고,
/// 그 싸움을 「사람처럼」(봇 탐색 없이 카드 한 장씩 · 턴 넘기기) 다시 치며 카드 1장 · 적 턴 · 화면 글(CardText) 시간과 할당을 잰다.
/// 판 기록(--records)은 바이트 비교용 — 최적화 전후가 같아야 한다.
///   bench [판 20] [--seed 0] [--rep 3] [--records 파일]
/// </summary>
static class Bench
{
    public static int Run(GameData data, List<string> args)
    {
        string Opt(string name, string d)
        {
            int i = args.IndexOf("--" + name);
            if (i < 0 || i + 1 >= args.Count) return d;
            var v = args[i + 1]; args.RemoveRange(i, 2); return v;
        }
        int seed0 = int.Parse(Opt("seed", "0")), rep = int.Parse(Opt("rep", "3"));
        string recOut = Opt("records", null);
        string textOut = Opt("textdump", null);
        string simN = Opt("sim", null);
        if (simN != null)
        {   // bz sim 과 같은 MetaSim.Run(초보 봇 · 무작위 편성) — 시간과, 초를 뺀 보고(md)의 해시
            int th = int.Parse(Opt("threads", "8"));
            var sw0 = Stopwatch.StartNew();
            var res = MetaSim.Run(data, int.Parse(simN), seed0, threads: th);
            sw0.Stop();
            var md = System.Text.RegularExpressions.Regex.Replace(MetaSim.MarkdownV2(data, res, null, null, "벤치") + MetaSim.Report(data, res), @"\d+초", "");
            if (recOut != null) System.IO.File.WriteAllText(recOut, md);
            Console.WriteLine($"sim {simN}바퀴 = {res.Runs}판 · 스레드 {th} · {sw0.Elapsed.TotalSeconds:0.0}초 · 완주 {res.Clear:0.0}% · 보고 해시 {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(md))).Substring(0, 16)}");
            return 0;
        }
        int runs = args.Count > 0 && int.TryParse(args[0], out var n) ? n : 20;
        if (textOut != null)
        {   // 글 전부(카드 · 신탁 · 축복 · 사도 · 특성 · 적 · 장비) — 최적화 전후 비교용
            var t = new CardText(data); var sb = new System.Text.StringBuilder();
            foreach (var c in data.Cards.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                sb.Append(c.Id).Append('\t').Append(t.Card(c)).Append('\n');
                for (int i = 0; i < c.Oracles.Count; i++) { sb.Append(' ').Append(t.Oracle(c, c.Oracles[i])).Append('\n').Append(' ').Append(t.Card(data.View(c.Id, i + 1))).Append('\n'); }
                foreach (var bl in c.Blesses) sb.Append(' ').Append(t.Bless(bl)).Append('\n');
            }
            foreach (var h in data.Heroes.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                sb.Append(t.Hero(h)).Append('\n').Append(t.HeroShort(h)).Append('\n');
                foreach (var tr in t.Traits(h)) sb.Append(tr.Kind).Append('|').Append(tr.Name).Append('|').Append(tr.Sub).Append('|').Append(tr.Body).Append('\n');
            }
            foreach (var e in data.Enemies.Values.OrderBy(x => x.Id, StringComparer.Ordinal)) sb.Append(t.Enemy(e)).Append('\n');
            foreach (var e in data.Equips.Values.OrderBy(x => x.Id, StringComparer.Ordinal)) sb.Append(t.Equip(e)).Append('\n');
            System.IO.File.WriteAllText(textOut, sb.ToString());
            Console.WriteLine($"글 {sb.Length}자 · 해시 {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))).Substring(0, 16)}");
            return 0;
        }

        var keys = data.Heroes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        uint lcg = (uint)(seed0 * 2654435761u + 12345);
        int Next(int m) { lcg = lcg * 1664525u + 1013904223u; return (int)((lcg >> 8) % (uint)m); }

        // ── 1. 판(봇) — 한 스레드로, 판마다 시간 · 할당. 싸움 시작 상태를 떠 둔다 ──
        var fights = new List<Battle>();
        var rec = new System.Text.StringBuilder();
        double runMs = 0; long runAlloc = 0;
        var sw = new Stopwatch();
        for (int i = 0; i < runs; i++)
        {
            var party = new List<string>();
            while (party.Count < 3) { var k = keys[Next(keys.Count)]; if (!party.Contains(k)) party.Add(k); }
            long seed = 1000 + seed0 * 100000 + i;
            var rb = new RunBot(data);
            long a0 = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            var r = rb.RunFull(party, seed, new SimOpts { OnFight = (b, run) => { var c = b.Clone(); fights.Add(c); } });
            sw.Stop();
            // 떠 둔 몫(Clone)도 들어가지만 판 하나에 싸움 10여 번 — 작다
            runMs += sw.Elapsed.TotalMilliseconds; runAlloc += GC.GetAllocatedBytesForCurrentThread() - a0;
            rec.Append($"{i}\t{string.Join(",", party)}\t{seed}\t{r.Clear}\t{r.Village}\t{r.Floor}\t{r.Where}\t{r.Fights}\t{r.Turns}\t{r.Gold}\t{r.Deck}\t{r.Uniques}\t{r.Basics}\t{r.Removals}\t{r.Breaks}\t{r.ToughHits}\n");
        }
        if (recOut != null) System.IO.File.WriteAllText(recOut, rec.ToString());
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rec.ToString()))).Substring(0, 16);

        // ── 2. 사람처럼 다시 치기 ──
        var tx = new CardText(data);
        double bestCard = 1e9, bestTurn = 1e9, bestText = 1e9, bestFight = 1e9;
        long allocFight = 0, allocCards = 0, allocTurns = 0, allocText = 0; int nCards = 0, nTurns = 0, nText = 0, nFights = fights.Count;
        double maxCard = 0, maxTurn = 0; var playLog = new System.Text.StringBuilder();
        for (int k = 0; k < rep; k++)
        {
            double cardMs = 0, turnMs = 0, textMs = 0, fightMs = 0; long aF = 0, aC = 0, aT = 0, aX = 0; int nc = 0, nt = 0, nx = 0;
            playLog.Clear();
            foreach (var f0 in fights)
            {
                var b = f0.Clone(); b.Cues = new List<Cue>();
                long fa0 = GC.GetAllocatedBytesForCurrentThread(); long ft0 = Stopwatch.GetTimestamp();
                int turnGuard = 0;
                while (b.Over == null && turnGuard++ < 40)
                {
                    int g = 0;
                    while (b.Over == null && g++ < 30)
                    {
                        int tgt = 0; foreach (var e in b.Enemies) if (!e.Dead) { tgt = e.Idx; break; }
                        string ult = null; foreach (var u in b.Party) if (b.CanUlt(u.Key) == null) { ult = u.Key; break; }
                        int hi = -1;
                        if (ult == null) for (int i = 0; i < b.Hand.Count; i++) if (b.CanPlay(b.Hand[i], false, i) == null) { hi = i; break; }
                        if (ult == null && hi < 0) break;
                        long a0 = GC.GetAllocatedBytesForCurrentThread(); long t0 = Stopwatch.GetTimestamp();
                        bool ok;
                        if (ult != null) ok = b.UseUlt(ult, tgt).Ok;
                        else
                        {
                            var id = b.Hand[hi]; var cv = b.CardOf(id);
                            if (b.GlowOf(id) != null) b.ApplyEpiphany(id, 0);
                            int need = b.DiscardChoice(hi);
                            var opts = new PlayOpts { Ally = b.Party.FirstOrDefault(u => !u.Dead)?.Idx, Choice = cv.Choices != null && cv.Choices.Count == 2 ? 1 : (int?)null,
                                Discard = need > 0 ? b.Hand.Where((_, j) => j != hi).Take(need).ToList() : null };
                            ok = b.PlayCard(hi, tgt, opts).Ok;
                        }
                        double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                        cardMs += ms; aC += GC.GetAllocatedBytesForCurrentThread() - a0; nc++; if (k == rep - 1 && ms > maxCard) maxCard = ms;
                        playLog.Append(ok ? '+' : '-');
                        b.Cues.Clear();
                        // 화면 몫 — CoreBattle.Snapshot 이 행동마다 만드는 카드 글(손 · 뽑을 · 버린 · 사라진 더미 전부, 장마다 text.Card 3번) + 사도 Traits
                        long xa0 = GC.GetAllocatedBytesForCurrentThread(); long xt0 = Stopwatch.GetTimestamp();
                        foreach (var pile in new[] { b.Hand, b.Draw, b.Discard, b.Gone })
                            foreach (var cid in pile) { var v = b.CardOf(cid); if (v == null) continue; tx.Card(v); tx.Card(v); tx.Card(v); if (v.Oracle != null) tx.Card(v.Def); }
                        foreach (var u in b.Party) { var h = data.Hero(u.Key); if (h != null) tx.Traits(h); }
                        textMs += (Stopwatch.GetTimestamp() - xt0) * 1000.0 / Stopwatch.Frequency; aX += GC.GetAllocatedBytesForCurrentThread() - xa0; nx++;
                        if (!ok) break;
                    }
                    if (b.Over != null) break;
                    long ta0 = GC.GetAllocatedBytesForCurrentThread(); long tt0 = Stopwatch.GetTimestamp();
                    b.EndTurn();
                    double tms = (Stopwatch.GetTimestamp() - tt0) * 1000.0 / Stopwatch.Frequency;
                    turnMs += tms; aT += GC.GetAllocatedBytesForCurrentThread() - ta0; nt++; if (k == rep - 1 && tms > maxTurn) maxTurn = tms;
                    b.Cues.Clear();
                }
                playLog.Append(b.Over ?? "x").Append(b.Turn).Append(' ').Append(b.Pool.Hp).Append('\n');
                fightMs += (Stopwatch.GetTimestamp() - ft0) * 1000.0 / Stopwatch.Frequency; aF += GC.GetAllocatedBytesForCurrentThread() - fa0;
            }
            if (k == 0) continue;   // 데우기
            bestCard = Math.Min(bestCard, cardMs / Math.Max(1, nc)); bestTurn = Math.Min(bestTurn, turnMs / Math.Max(1, nt));
            bestText = Math.Min(bestText, textMs / Math.Max(1, nx)); bestFight = Math.Min(bestFight, fightMs / Math.Max(1, nFights));
            allocFight = aF; allocCards = aC; allocTurns = aT; allocText = aX; nCards = nc; nTurns = nt; nText = nx;
        }
        string playHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(playLog.ToString()))).Substring(0, 16);
        if (recOut != null) System.IO.File.WriteAllText(recOut + ".play", playLog.ToString());
        double KB(long b, int cnt) => b / 1024.0 / Math.Max(1, cnt);
        Console.WriteLine($"판 {runs} · 봇 한 판 {runMs / runs:0.0}ms · 할당 {runAlloc / 1024.0 / 1024 / runs:0.0}MB/판 · 판 기록 해시 {hash}");
        Console.WriteLine($"싸움 {nFights} · 카드(고학년 포함) {nCards} · 턴 넘기기 {nTurns} · 다시 치기 해시 {playHash}");
        Console.WriteLine($"카드 1장 {bestCard:0.000}ms (최대 {maxCard:0.00}) · {KB(allocCards, nCards):0.0}KB");
        Console.WriteLine($"턴 넘기기(적 턴 + 다음 턴 시작) {bestTurn:0.000}ms (최대 {maxTurn:0.00}) · {KB(allocTurns, nTurns):0.0}KB");
        Console.WriteLine($"화면 글(행동 한 번의 더미 전부) {bestText:0.000}ms · {KB(allocText, nText):0.0}KB");
        Console.WriteLine($"싸움 하나(엔진 + 화면 글) {bestFight:0.00}ms · {KB(allocFight, nFights):0.0}KB");
        return 0;
    }
}
