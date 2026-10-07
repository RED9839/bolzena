using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bolzena.Core
{
    /// <summary>
    /// 메타 통계(웹판 tools/meta-sim.js) — 사도별 · 역할 편성별 · 마을별 완주율(smart 봇, 판 전체).
    /// 한 바퀴마다 사도를 섞어 셋씩 묶는다(사도마다 한 번). 결과는 작업 수와 상관없이 같다(씨앗이 정해져 있다).
    /// </summary>
    public static class MetaSim
    {
        public sealed class HeroRow { public string Key, Name, Role; public int N; public double Win; }
        public sealed class Result
        {
            public int Runs, Rounds;
            public double Clear, Hpx, Dmgx;
            public double Sec;
            public List<HeroRow> Heroes = new();
            public Dictionary<string, (int n, double win)> Comps = new();
            public Dictionary<string, (int n, double win)> Villages = new();
            public double[] Fell = new double[2];
            public double AvgTurnsFight, AvgTurnsBoss;
            /// <summary>격파 — 판당 · 싸움 종류(fight · elite · boss)마다 싸움당. 강인도를 깎은 카드 가운데 약점 공격 비율(%).</summary>
            public double BreaksPerRun, WeakPct;
            public Dictionary<string, double> BreaksPerFight = new();
            /// <summary>봇 · 편성 이름(보고 머리).</summary>
            public string Label = "초보 봇 · 무작위 편성";
            /// <summary>판이 끝날 때 덱 — 평균 장수 · 고유 카드 · 기본 카드(복제본 포함 · 교주 카드는 장수에만).</summary>
            public double AvgDeck, AvgUniques, AvgBasics;
            /// <summary>판당 상점 빼기 횟수.</summary>
            public double AvgRemovals;
            /// <summary>연속 이벤트 깃발 — 판당 선 수 · 깃발마다 선 판 비율(%).</summary>
            public double FlagsPerRun, FlagReadsPerRun;
            public Dictionary<string, double> FlagPct = new();
        }

        static readonly Dictionary<string, string> LETTER = new() { ["탱커"] = "T", ["서포터"] = "S", ["딜러"] = "D" };

        public static List<(int i, List<string> party, long seed)> Jobs(GameData d, int rounds, int seed, List<string> heroes = null)
        {
            var all = (heroes ?? d.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList()).ToList();
            uint s = unchecked((uint)(777 + seed * 104729));
            double Rnd() { s = unchecked(s * 1103515245u + 12345u); return s / 4294967296.0; }
            var jobs = new List<(int, List<string>, long)>();
            for (int r = 0; r < rounds; r++)
            {
                var pool = all.ToList();
                for (int i = pool.Count - 1; i > 0; i--) { int j = (int)Math.Floor(Rnd() * (i + 1)); (pool[i], pool[j]) = (pool[j], pool[i]); }
                for (int i = 0; i + 3 <= pool.Count; i += 3) jobs.Add((jobs.Count, pool.GetRange(i, 3), 20000 + seed * 7919 + jobs.Count * 37));
            }
            return jobs;
        }

        /// <summary>한 사도를 늘 넣고 나머지 둘을 무작위로(콘텐츠 작가가 제 사도를 잴 때). n 판.</summary>
        public static List<(int i, List<string> party, long seed)> JobsWith(GameData d, string hero, int n, int seed)
        {
            var others = d.Heroes.Keys.Where(k => k != hero).OrderBy(x => x, StringComparer.Ordinal).ToList();
            uint s = unchecked((uint)(991 + seed * 104729));
            int Rnd(int m) { s = unchecked(s * 1103515245u + 12345u); return (int)(s / 4294967296.0 * m); }
            var jobs = new List<(int, List<string>, long)>();
            for (int i = 0; i < n; i++)
            {
                var party = new List<string> { hero };
                var pool = others.ToList();
                while (party.Count < 3 && pool.Count > 0) { int j = Rnd(pool.Count); party.Add(pool[j]); pool.RemoveAt(j); }
                jobs.Add((i, party, 30000 + seed * 7919 + i * 37));
            }
            return jobs;
        }

        /// <summary>메타 시뮬. with 를 주면 그 사도를 늘 넣은 rounds 판(JobsWith).</summary>
        /// <param name="bot">봇 손잡이(Skilled · UniqueOnly) — null 이면 초보 봇.</param>
        /// <param name="party">편성(PartyPick.MODES — random · role · synergy). null 이면 random.</param>
        public static Result Run(GameData d, int rounds = 30, int seed = 0, double hpx = 1, double dmgx = 1, int threads = 0, List<string> heroes = null, Action<int, int> progress = null, string with = null,
            SimOpts bot = null, string party = null, PartyPick.SynScore syn = null)
        {
            var t0 = DateTime.Now;
            var jobs = with != null ? JobsWith(d, with, rounds, seed) : PartyPick.Jobs(d, rounds, seed, party, syn, heroes);
            bool skilled = bot?.Skilled == true, uonly = bot?.UniqueOnly == true;
            var res = new SimResult[jobs.Count];
            int done = 0;
            var po = new ParallelOptions { MaxDegreeOfParallelism = threads > 0 ? threads : Math.Max(1, Environment.ProcessorCount - 1) };
            // 봇은 카드 값어치를 담아 두니 작업마다 따로 만든다(스레드끼리 나누지 않는다)
            Parallel.ForEach(jobs, po, j =>
            {
                var rb = new RunBot(d);
                res[j.i] = rb.RunFull(j.party, j.seed, new SimOpts { Hpx = hpx, Dmgx = dmgx, Skilled = skilled, UniqueOnly = uonly });
                progress?.Invoke(System.Threading.Interlocked.Increment(ref done), jobs.Count);
            });
            double Pct(int w, int n) => n > 0 ? 100.0 * w / n : 0;
            var hero = new Dictionary<string, int[]>(); var comp = new Dictionary<string, int[]>(); var vil = new Dictionary<string, int[]>();
            void Add(Dictionary<string, int[]> m, string k, bool win) { if (!m.TryGetValue(k, out var x)) m[k] = x = new int[2]; x[0]++; if (win) x[1]++; }
            int wins = 0; var fell = new int[2];
            int fT = 0, fN = 0, bT = 0, bN = 0;
            int brk = 0, th = 0, tw = 0; var bk = new Dictionary<string, int[]>();
            foreach (var j in jobs)
            {
                var r = res[j.i];
                if (r.Clear) wins++; else fell[Math.Min(1, r.Floor)]++;
                Add(vil, r.Village, r.Clear);
                foreach (var k in j.party) Add(hero, k, r.Clear);
                Add(comp, string.Concat(j.party.Select(k => LETTER.TryGetValue(d.Hero(k).Role, out var l) ? l : "?").OrderBy(x => "TSD".IndexOf(x))), r.Clear);
                brk += r.Breaks; th += r.ToughHits; tw += r.ToughWeakHits;
                foreach (var kv in r.Kinds)
                {
                    string g = kv.Key.Substring(kv.Key.IndexOf(':') + 1);
                    if (!bk.TryGetValue(g, out var a)) bk[g] = a = new int[2];
                    a[0] += kv.Value.n; a[1] += r.BreaksBy.TryGetValue(kv.Key, out var b) ? b : 0;
                }
                foreach (var kv in r.Kinds) { if (kv.Key.EndsWith(":boss")) { bT += kv.Value.turns; bN += kv.Value.n; } else if (kv.Key.EndsWith(":fight")) { fT += kv.Value.turns; fN += kv.Value.n; } }
            }
            return new Result
            {
                Runs = jobs.Count, Rounds = rounds, Hpx = hpx, Dmgx = dmgx, Clear = Pct(wins, jobs.Count), Sec = (DateTime.Now - t0).TotalSeconds,
                Heroes = hero.Select(kv => new HeroRow { Key = kv.Key, Name = d.Hero(kv.Key).Name, Role = d.Hero(kv.Key).Role, N = kv.Value[0], Win = Pct(kv.Value[1], kv.Value[0]) }).OrderByDescending(x => x.Win).ToList(),
                Comps = comp.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => (kv.Value[0], Pct(kv.Value[1], kv.Value[0]))),
                Villages = vil.ToDictionary(kv => kv.Key, kv => (kv.Value[0], Pct(kv.Value[1], kv.Value[0]))),
                Fell = fell.Select(n => Pct(n, jobs.Count)).ToArray(),
                AvgTurnsFight = fN > 0 ? (double)fT / fN : 0, AvgTurnsBoss = bN > 0 ? (double)bT / bN : 0,
                BreaksPerRun = jobs.Count > 0 ? (double)brk / jobs.Count : 0, WeakPct = Pct(tw, th),
                BreaksPerFight = bk.ToDictionary(kv => kv.Key, kv => kv.Value[0] > 0 ? (double)kv.Value[1] / kv.Value[0] : 0),
                Label = $"{(skilled ? "숙련" : "초보")} 봇 · {PartyName(party)} 편성{(uonly ? " · 고유 카드만" : "")}",
                AvgDeck = res.Length > 0 ? res.Average(x => x.Deck) : 0, AvgUniques = res.Length > 0 ? res.Average(x => x.Uniques) : 0, AvgBasics = res.Length > 0 ? res.Average(x => x.Basics) : 0, AvgRemovals = res.Length > 0 ? res.Average(x => x.Removals) : 0,
                FlagsPerRun = res.Length > 0 ? res.Average(x => x.Flags.Count) : 0,
                FlagReadsPerRun = res.Length > 0 ? res.Average(x => x.FlagReads) : 0,
                FlagPct = res.SelectMany(x => x.Flags).GroupBy(x => x).ToDictionary(g => g.Key, g => Pct(g.Count(), res.Length)),
            };
        }

        public static string PartyName(string mode) => mode == PartyPick.Role ? "역할" : mode == PartyPick.Synergy ? "시너지" : "무작위";

        /// <summary>판 묶음을 병렬로 — 완주했나만(짝 재기 따위).</summary>
        public static bool[] RunJobs(GameData d, List<(int i, List<string> party, long seed)> jobs, int threads = 0, double hpx = 1, double dmgx = 1)
        {
            var res = new bool[jobs.Count];
            var po = new ParallelOptions { MaxDegreeOfParallelism = threads > 0 ? threads : Math.Max(1, Environment.ProcessorCount - 1) };
            Parallel.ForEach(jobs, po, j => { res[j.i] = new RunBot(d).RunFull(j.party, j.seed, new SimOpts { Hpx = hpx, Dmgx = dmgx }).Clear; });
            return res;
        }

        /// <summary>짝 한 칸 — 같은 방식 · 같은 성격 짝.</summary>
        public sealed class PairRow { public string Style, Nature; public List<string> Heroes; public int N; public double Pair, Base; public double Lift => Pair - Base; }

        /// <summary>
        /// 짝 시너지 — 방식 × 성격 칸마다(사도 둘 이상): 그 칸의 둘 + 다른 방식의 사도 하나(짝) 와, 그 둘 가운데 하나 + 다른 방식의 둘(바탕)을 n 판씩.
        /// 같은 씨앗 · 같은 셋째 자리를 맞춰 비교한다. Lift = 짝 − 바탕(%p).
        /// </summary>
        public static List<PairRow> Pairs(GameData d, int n = 60, int seed = 0, int threads = 0)
        {
            var heroes = d.Heroes.Values.Where(h => h.Style != null && h.Nature != null).OrderBy(h => h.Id, StringComparer.Ordinal).ToList();
            var cells = heroes.GroupBy(h => (h.Style, h.Nature)).Where(g => g.Count() >= 2).OrderBy(g => g.Key.Style, StringComparer.Ordinal).ThenBy(g => g.Key.Nature, StringComparer.Ordinal).ToList();
            uint s = unchecked((uint)(4242 + seed * 104729));
            int Rnd(int m) { s = unchecked(s * 1103515245u + 12345u); return (int)(s / 4294967296.0 * m); }
            var jobs = new List<(int, List<string>, long)>();
            var meta = new List<(int cell, bool pair)>();
            foreach (var (g, ci) in cells.Select((g, i) => (g, i)))
            {
                var inCell = g.Select(h => h.Id).ToList();
                var outside = heroes.Where(h => h.Style != g.Key.Style).Select(h => h.Id).ToList();
                for (int k = 0; k < n; k++)
                {
                    int a = Rnd(inCell.Count), b = Rnd(inCell.Count - 1); if (b >= a) b++;
                    string x = outside[Rnd(outside.Count)], y; do y = outside[Rnd(outside.Count)]; while (y == x);
                    long sd = 50000 + seed * 7919 + jobs.Count * 37;
                    jobs.Add((jobs.Count, new List<string> { inCell[a], inCell[b], x }, sd)); meta.Add((ci, true));
                    jobs.Add((jobs.Count, new List<string> { inCell[a], y, x }, sd)); meta.Add((ci, false));
                }
            }
            var res = RunJobs(d, jobs, threads);
            return cells.Select((g, ci) =>
            {
                int pn = 0, pw = 0, bn = 0, bw = 0;
                for (int i = 0; i < meta.Count; i++) if (meta[i].cell == ci) { if (meta[i].pair) { pn++; if (res[i]) pw++; } else { bn++; if (res[i]) bw++; } }
                return new PairRow { Style = g.Key.Style, Nature = g.Key.Nature, Heroes = g.Select(h => h.Name).ToList(), N = pn, Pair = pn > 0 ? 100.0 * pw / pn : 0, Base = bn > 0 ? 100.0 * bw / bn : 0 };
            }).ToList();
        }

        // ── 새 기준(2026-10-05 — 운영 방식 폐기) : 같은 종족 짝 · 키워드 맞물림 짝 · 역할 조합 전부 · 사도 혼자 완주율 ──

        /// <summary>그 사도의 효과 조각 전부(카드 · 생성 카드 · 패시브 · 고유 효과 규칙 · 고학년, then · else 안까지).</summary>
        static IEnumerable<Fx> HeroFx(GameData d, HeroDef h)
        {
            IEnumerable<Fx> Walk(IEnumerable<Fx> l) { foreach (var f in l ?? Enumerable.Empty<Fx>()) { yield return f; foreach (var x in Walk(f.Then)) yield return x; foreach (var x in Walk(f.Else)) yield return x; } }
            var all = d.Cards.Values.Where(c => c.Hero == h.Id).SelectMany(c => c.Fx.Concat(c.Oracles.SelectMany(o => o.Fx)))
                .Concat(h.Passives.SelectMany(p => p.Fx)).Concat(h.AllKeywords.SelectMany(k => k.Rules.SelectMany(r => r.Fx))).Concat(h.Ult?.Fx ?? new List<Fx>());
            return Walk(all).ToList();
        }

        /// <summary>내는 것(상태 · 태그 · 일) — 맞물림 짝을 고를 때.</summary>
        public static HashSet<string> Gives(GameData d, HeroDef h)
        {
            var o = new HashSet<string>();
            foreach (var f in HeroFx(d, h))
            {
                if (f.K == FxK.Status && f.Id != null) { o.Add("st:" + f.Id); if (R.IsBadSt(f.Id)) o.Add("ev:debuff"); }
                if (f.K == FxK.Extra) o.Add("ev:extra");
                if (f.K == FxK.Make) o.Add("ev:make");
                if (f.K == FxK.Discard) o.Add("ev:discard");
                if (f.K == FxK.Burn || f.K == FxK.ExileFrom) o.Add("ev:exhaust");
                if (f.K == FxK.Shield || f.K == FxK.Block) o.Add("ev:guard");
                if (f.K == FxK.Heal) o.Add("ev:heal");
                if (f.K == FxK.Tough) o.Add("ev:break");
                if (f.K == FxK.TakenMod && f.V > 0 || f.K == FxK.DealtMod && f.V < 0) o.Add("ev:debuff");
            }
            foreach (var c in d.Cards.Values.Where(c => c.Hero == h.Id))
                foreach (var t in c.Tags) { var id = Tag.Parse(t).id; o.Add("tag:" + id); if (id == Tag.Exhaust) o.Add("ev:exhaust"); }
            return o;
        }

        /// <summary>듣는 것 — 다른 아군의 일에 반응하는 계기 · 조건 · 비례.</summary>
        public static HashSet<string> Hears(GameData d, HeroDef h)
        {
            var o = new HashSet<string>();
            foreach (var r in h.Passives.Concat(h.AllKeywords.SelectMany(k => k.Rules)))
            {
                var w = r.When; bool party = w.Who == "any" || w.Who == "other";
                if (w.Tag != null) o.Add("tag:" + w.Tag);
                if (party || w.On == "debuff" || w.On == "break" || w.On == "kill")
                    switch (w.On)
                    {
                        case "debuff": o.Add("ev:debuff"); break;
                        case "extra": o.Add("ev:extra"); break;
                        case "make": o.Add("ev:make"); break;
                        case "discard": o.Add("ev:discard"); break;
                        case "exhaust": o.Add("ev:exhaust"); break;
                        case "overheal": case "unwound": o.Add("ev:heal"); break;
                        case "break": o.Add("ev:break"); break;
                    }
                if (w.On == "guard") o.Add("ev:guard");
                foreach (var c in r.Conds) if (c.C == "status" && c.Id != null) o.Add("st:" + c.Id); else if (c.C == "debuffs") o.Add("ev:debuff");
            }
            foreach (var f in HeroFx(d, h))
            {
                if (f.K == FxK.PerTag || f.K == FxK.PerPlayed) { if (f.Id != null) o.Add("tag:" + f.Id); }
                if (f.K == FxK.When && f.On == "passion") o.Add("tag:" + Tag.Passion);
                if (f.K == FxK.IfDebuffs || f.K == FxK.PerDebuff) o.Add("ev:debuff");
                if (f.K == FxK.IfBreak || (f.K == FxK.IfFoe && f.Id == "broken")) o.Add("ev:break");
                if (f.K == FxK.IfShield && !f.Not) o.Add("ev:guard");
            }
            foreach (var c in d.Cards.Values.Where(c => c.Hero == h.Id)) if (c.Tags.Contains(Tag.Link)) o.Add("any");
            return o;
        }

        /// <summary>맞물림 점수 — A 가 내는 것을 B 가 듣는 가짓수 + 거꾸로.</summary>
        public static int Mesh(GameData d, HeroDef a, HeroDef b)
        {
            var ga = Gives(d, a); var gb = Gives(d, b); var ha = Hears(d, a); var hb = Hears(d, b);
            return ga.Count(x => hb.Contains(x)) + gb.Count(x => ha.Contains(x)) + (ha.Contains("any") ? 1 : 0) + (hb.Contains("any") ? 1 : 0);
        }

        /// <summary>짝 한 칸(새 기준) — Kind: 종족 · 맞물림. Label: 종족 이름 · 「A × B」.</summary>
        public sealed class PairRowV2 { public string Kind, Label; public List<string> Heroes; public int N, Score; public double Pair, Base; public double Lift => Pair - Base; }

        /// <summary>
        /// 짝 시너지(새 기준) — ① 같은 종족 칸(사도 둘 이상): 그 종족 둘 + 다른 종족 하나(짝) 대 그 둘 가운데 하나 + 다른 종족 둘(바탕) ·
        /// ② 키워드 맞물림 짝(점수가 높은 top 쌍): 그 둘 + 셋째(짝) 대 하나 + 맞물림 없는 사도 + 같은 셋째(바탕). 같은 씨앗 · 같은 셋째로 견준다.
        /// </summary>
        public static List<PairRowV2> PairsV2(GameData d, int n = 60, int seed = 0, int threads = 0, int top = 30)
        {
            var heroes = d.Heroes.Values.OrderBy(h => h.Id, StringComparer.Ordinal).ToList();
            uint s = unchecked((uint)(5151 + seed * 104729));
            int Rnd(int m) { s = unchecked(s * 1103515245u + 12345u); return (int)(s / 4294967296.0 * m); }
            var cells = new List<(string kind, string label, List<string> ids, List<string> outside, int score)>();
            foreach (var g in heroes.Where(h => h.Race != null).GroupBy(h => h.Race).Where(g => g.Count() >= 2).OrderBy(g => g.Key, StringComparer.Ordinal))
                cells.Add(("종족", g.Key, g.Select(h => h.Id).ToList(), heroes.Where(h => h.Race != g.Key).Select(h => h.Id).ToList(), 0));
            var mesh = new List<(HeroDef a, HeroDef b, int sc)>();
            for (int i = 0; i < heroes.Count; i++) for (int j = i + 1; j < heroes.Count; j++) { int sc = Mesh(d, heroes[i], heroes[j]); if (sc > 0) mesh.Add((heroes[i], heroes[j], sc)); }
            var used = new Dictionary<string, int>();   // 한 사도가 짝 표를 차지하지 않게 — 사도마다 둘까지
            var picked = new List<(HeroDef a, HeroDef b, int sc)>();
            foreach (var m in mesh.OrderByDescending(x => x.sc).ThenBy(x => x.a.Id, StringComparer.Ordinal).ThenBy(x => x.b.Id, StringComparer.Ordinal))
            {
                if (picked.Count >= top) break;
                if ((used.TryGetValue(m.a.Id, out var ua) ? ua : 0) >= 2 || (used.TryGetValue(m.b.Id, out var ub) ? ub : 0) >= 2) continue;
                picked.Add(m); used[m.a.Id] = ua + 1; used[m.b.Id] = ub + 1;
            }
            foreach (var m in picked)
                cells.Add(("맞물림", $"{m.a.Name} × {m.b.Name}", new List<string> { m.a.Id, m.b.Id },
                    heroes.Where(h => h != m.a && h != m.b && Mesh(d, m.a, h) == 0 && Mesh(d, m.b, h) == 0).Select(h => h.Id).ToList(), m.sc));
            cells.RemoveAll(c => c.outside.Count < 2);
            var jobs = new List<(int, List<string>, long)>(); var meta = new List<(int cell, bool pair)>();
            for (int ci = 0; ci < cells.Count; ci++)
            {
                var (_, _, inCell, outside, _) = cells[ci];
                for (int k = 0; k < n; k++)
                {
                    int a = Rnd(inCell.Count), b = Rnd(inCell.Count - 1); if (b >= a) b++;
                    string x = outside[Rnd(outside.Count)], y; do y = outside[Rnd(outside.Count)]; while (y == x);
                    long sd = 60000 + seed * 7919 + jobs.Count * 37;
                    jobs.Add((jobs.Count, new List<string> { inCell[a], inCell[b], x }, sd)); meta.Add((ci, true));
                    jobs.Add((jobs.Count, new List<string> { inCell[a], y, x }, sd)); meta.Add((ci, false));
                }
            }
            var res = RunJobs(d, jobs, threads);
            return cells.Select((c, ci) =>
            {
                int pn = 0, pw = 0, bn = 0, bw = 0;
                for (int i = 0; i < meta.Count; i++) if (meta[i].cell == ci) { if (meta[i].pair) { pn++; if (res[i]) pw++; } else { bn++; if (res[i]) bw++; } }
                return new PairRowV2 { Kind = c.kind, Label = c.label, Heroes = c.ids.Select(id => d.Hero(id).Name).ToList(), N = pn, Score = c.score, Pair = pn > 0 ? 100.0 * pw / pn : 0, Base = bn > 0 ? 100.0 * bw / bn : 0 };
            }).ToList();
        }

        public static readonly string[] COMPS = { "TTT", "TTS", "TTD", "TSS", "TSD", "TDD", "SSS", "SSD", "SDD", "DDD" };

        /// <summary>역할 조합 열 가지(TTT~DDD) 마다 n 판 — 그 역할의 사도를 무작위로 골라 채운다(그 역할 사도가 모자라면 빈칸).</summary>
        public static Dictionary<string, (int n, double win)> CompTable(GameData d, int n = 100, int seed = 0, int threads = 0)
        {
            var by = new Dictionary<char, List<string>>
            {
                ['T'] = d.Heroes.Values.Where(h => h.Role == "탱커").Select(h => h.Id).OrderBy(x => x, StringComparer.Ordinal).ToList(),
                ['S'] = d.Heroes.Values.Where(h => h.Role == "서포터").Select(h => h.Id).OrderBy(x => x, StringComparer.Ordinal).ToList(),
                ['D'] = d.Heroes.Values.Where(h => h.Role == "딜러").Select(h => h.Id).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            };
            uint s = unchecked((uint)(6161 + seed * 104729));
            int Rnd(int m) { s = unchecked(s * 1103515245u + 12345u); return (int)(s / 4294967296.0 * m); }
            var jobs = new List<(int, List<string>, long)>(); var tag = new List<string>();
            foreach (var comp in COMPS)
            {
                if (comp.GroupBy(ch => ch).Any(g => by[g.Key].Count < g.Count())) continue;
                for (int k = 0; k < n; k++)
                {
                    var party = new List<string>();
                    foreach (var ch in comp) { string id; do id = by[ch][Rnd(by[ch].Count)]; while (party.Contains(id)); party.Add(id); }
                    jobs.Add((jobs.Count, party, 70000 + seed * 7919 + jobs.Count * 37)); tag.Add(comp);
                }
            }
            var res = RunJobs(d, jobs, threads);
            return COMPS.ToDictionary(c => c, c =>
            {
                var idx = Enumerable.Range(0, tag.Count).Where(i => tag[i] == c).ToList();
                return (idx.Count, idx.Count > 0 ? 100.0 * idx.Count(i => res[i]) / idx.Count : 0);
            });
        }

        /// <summary>사도 혼자 완주율 한 줄 — 그 사도 + 무작위 둘, N 판 · 95% 오차.</summary>
        public sealed class SoloRow { public string Key, Name, Role, Race; public int N; public double Win, Err; }

        /// <summary>
        /// 사도마다 「혼자」 완주율 — 그 사도를 늘 넣고 나머지 둘을 무작위로. 먼저 min 판씩 다 돌린 뒤, 95% 오차가 err(%p) 를 넘는 사도만
        /// 필요한 판 수(1.96²·p(1-p)/err²)까지 더 돌린다(max 판까지). 전부 병렬 — 사도 135 × 판을 한 번에 깐다.
        /// </summary>
        public static List<SoloRow> Solo(GameData d, double err = 2, int min = 300, int max = 3000, int seed = 0, int threads = 0, List<string> heroes = null, Action<string> log = null)
        {
            var ids = heroes ?? d.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
            var n = ids.ToDictionary(k => k, k => 0); var w = ids.ToDictionary(k => k, k => 0);
            void Round(Dictionary<string, int> want)
            {
                var jobs = new List<(int, List<string>, long)>(); var who = new List<string>();
                foreach (var kv in want.Where(kv => kv.Value > 0))
                    foreach (var j in JobsWith(d, kv.Key, n[kv.Key] + kv.Value, seed).Skip(n[kv.Key])) { jobs.Add((jobs.Count, j.party, j.seed)); who.Add(kv.Key); }
                if (jobs.Count == 0) return;
                var t0 = DateTime.Now;
                var res = RunJobs(d, jobs, threads);
                for (int i = 0; i < jobs.Count; i++) { n[who[i]]++; if (res[i]) w[who[i]]++; }
                log?.Invoke($"  {jobs.Count}판 · {(DateTime.Now - t0).TotalSeconds:0}초");
            }
            double Half(string k) => n[k] > 0 ? 1.96 * Math.Sqrt(Math.Max(0.0025, (double)w[k] / n[k] * (1 - (double)w[k] / n[k])) / n[k]) * 100 : 100;
            Round(ids.ToDictionary(k => k, k => min));
            for (int pass = 0; pass < 3; pass++)
            {
                var more = new Dictionary<string, int>();
                foreach (var k in ids.Where(k => Half(k) > err))
                {
                    double p = Math.Max(0.05, (double)w[k] / n[k]);
                    int need = Math.Min(max, (int)Math.Ceiling(1.96 * 1.96 * p * (1 - p) / Math.Pow(err / 100, 2) * 1.05));
                    if (need > n[k]) more[k] = need - n[k];
                }
                if (more.Count == 0) break;
                Round(more);
            }
            return ids.Select(k => { var h = d.Hero(k); return new SoloRow { Key = k, Name = h.Name, Role = h.Role, Race = h.Race, N = n[k], Win = n[k] > 0 ? 100.0 * w[k] / n[k] : 0, Err = Half(k) }; })
                .OrderByDescending(x => x.Win).ToList();
        }

        public static string SoloMarkdown(GameData d, List<SoloRow> rows, string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {title}").AppendLine();
            sb.AppendLine($"사도마다 그 사도 + 무작위 둘 · smart 봇 · 판 전체. 판 {rows.Sum(r => r.N)} · 평균 {rows.Average(r => r.Win):0.0}% · 오차(95%) 최대 ±{rows.Max(r => r.Err):0.0}%p").AppendLine();
            sb.AppendLine("## 종족 · 역할 평균").AppendLine().AppendLine("| 갈래 | 사도 | 평균 | 낮음 ~ 높음 |").AppendLine("|---|---|---|---|");
            foreach (var g in rows.GroupBy(r => "종족 " + (r.Race ?? "-")).Concat(rows.GroupBy(r => "역할 " + r.Role)).OrderBy(g => g.Key))
                sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Average(x => x.Win):0.0}% | {g.Min(x => x.Win):0} ~ {g.Max(x => x.Win):0} |");
            sb.AppendLine().AppendLine("## 사도").AppendLine().AppendLine("| 사도 | 역할 | 종족 | 판 | 완주 | 오차 |").AppendLine("|---|---|---|---|---|---|");
            foreach (var r in rows) sb.AppendLine($"| {r.Name} | {r.Role} | {r.Race} | {r.N} | {r.Win:0.0}% | ±{r.Err:0.0} |");
            return sb.ToString();
        }

        /// <summary>새 기준 문서 표 — 전체 · 마을 · 역할 조합 전부 · 종족 평균 · 짝(종족 · 맞물림) · 사도.</summary>
        public static string MarkdownV2(GameData d, Result r, Dictionary<string, (int n, double win)> comps, List<PairRowV2> pairs, string title)
        {
            var sb = new StringBuilder();
            double Half(double p, int n) => n > 0 ? 1.96 * Math.Sqrt(p / 100 * (1 - p / 100) / n) * 100 : 0;
            sb.AppendLine($"# {title}").AppendLine();
            sb.AppendLine($"{r.Label} · 판 전체 · {r.Runs}판 · 전체 완주 **{r.Clear:0.0}%** · 쓰러진 층 1층 {r.Fell[0]:0.0}% · 2층 {r.Fell[1]:0.0}% · 평균 턴 일반 {r.AvgTurnsFight:0.0} · 보스 {r.AvgTurnsBoss:0.0} · {r.Sec:0}초").AppendLine();
            sb.AppendLine("## 마을").AppendLine().AppendLine("| 마을 | 판 | 완주 |").AppendLine("|---|---|---|");
            foreach (var kv in r.Villages.OrderByDescending(x => x.Value.win)) sb.AppendLine($"| {d.Villages[kv.Key].Name} | {kv.Value.n} | {kv.Value.win:0.0}% |");
            var ct = comps ?? r.Comps;
            sb.AppendLine().AppendLine("## 역할 조합(TTT ~ DDD)").AppendLine().AppendLine("| 편성 | 판 | 완주 | 오차 |").AppendLine("|---|---|---|---|");
            foreach (var c in COMPS) { var v = ct.TryGetValue(c, out var x) ? x : (0, 0.0); sb.AppendLine($"| {c} | {v.Item1} | {(v.Item1 > 0 ? $"{v.Item2:0.0}%" : "-")} | {(v.Item1 > 0 ? $"±{Half(v.Item2, v.Item1):0}" : "")} |"); }
            sb.AppendLine().AppendLine("## 종족 평균").AppendLine().AppendLine("| 종족 | 사도 | 평균 완주 | 낮음 ~ 높음 |").AppendLine("|---|---|---|---|");
            foreach (var g in r.Heroes.GroupBy(h => d.Hero(h.Key).Race ?? "-").OrderByDescending(g => g.Average(x => x.Win)))
                sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Average(x => x.Win):0.0}% | {g.Min(x => x.Win):0} ~ {g.Max(x => x.Win):0} |");
            if (pairs != null && pairs.Count > 0)
                foreach (var kind in new[] { "종족", "맞물림" })
                {
                    var ps = pairs.Where(p => p.Kind == kind).ToList(); if (ps.Count == 0) continue;
                    sb.AppendLine().AppendLine($"## 짝 — {(kind == "종족" ? "같은 종족" : "키워드 맞물림(한쪽이 내는 상태 · 태그 · 일을 다른 쪽이 듣는다)")}").AppendLine();
                    sb.AppendLine("짝 = 그 둘 + 셋째, 바탕 = 그 가운데 하나 + 칸 밖 사도 + 같은 셋째(같은 씨앗).").AppendLine();
                    sb.AppendLine($"| {(kind == "종족" ? "종족" : "짝")} | 사도 | 점수 | 판 | 짝 | 바탕 | 차이 |").AppendLine("|---|---|---|---|---|---|---|");
                    foreach (var p in ps.OrderByDescending(x => x.Lift)) sb.AppendLine($"| {p.Label} | {(p.Heroes.Count > 4 ? p.Heroes.Count + "명" : string.Join(" · ", p.Heroes))} | {(kind == "종족" ? "" : p.Score.ToString())} | {p.N} | {p.Pair:0.0}% | {p.Base:0.0}% | {(p.Lift >= 0 ? "+" : "")}{p.Lift:0.0} |");
                    sb.AppendLine().AppendLine($"평균 차이 {ps.Average(x => x.Lift):+0.0;-0.0}%p · +10%p 이상 {ps.Count(x => x.Lift >= 10)} / {ps.Count}.");
                }
            sb.AppendLine().AppendLine("## 사도").AppendLine().AppendLine("| 사도 | 역할 | 종족 | 판 | 완주 |").AppendLine("|---|---|---|---|---|");
            foreach (var h in r.Heroes) { var hd = d.Hero(h.Key); sb.AppendLine($"| {h.Name} | {h.Role} | {hd.Race} | {h.N} | {h.Win:0.0}% |"); }
            return sb.ToString();
        }

        /// <summary>문서용 표(Markdown) — 전체 · 마을 · 편성 · 방식 · 사도 · 짝.</summary>
        public static string Markdown(GameData d, Result r, List<PairRow> pairs, string title)
        {
            var sb = new StringBuilder();
            double Half(double p, int n) => n > 0 ? 1.96 * Math.Sqrt(p / 100 * (1 - p / 100) / n) * 100 : 0;
            sb.AppendLine($"# {title}").AppendLine();
            sb.AppendLine($"smart 봇 · 판 전체 · {r.Runs}판(사도마다 약 {r.Runs * 3 / Math.Max(1, d.Heroes.Count)}판) · 전체 완주 **{r.Clear:0.0}%** · 쓰러진 층 1층 {r.Fell[0]:0.0}% · 2층 {r.Fell[1]:0.0}% · 평균 턴 일반 {r.AvgTurnsFight:0.0} · 보스 {r.AvgTurnsBoss:0.0} · {r.Sec:0}초");
            sb.AppendLine($"사도 한 명 값의 오차는 ±{Half(r.Clear, r.Runs * 3 / Math.Max(1, d.Heroes.Count)):0}%p 쯤(95%) — 가운데 순위는 믿지 말고 양 끝 · 방식 평균을 본다.").AppendLine();
            sb.AppendLine("## 마을").AppendLine().AppendLine("| 마을 | 판 | 완주 |").AppendLine("|---|---|---|");
            foreach (var kv in r.Villages.OrderByDescending(x => x.Value.win)) sb.AppendLine($"| {d.Villages[kv.Key].Name} | {kv.Value.n} | {kv.Value.win:0.0}% |");
            sb.AppendLine().AppendLine("## 편성(역할 셋)").AppendLine().AppendLine("| 편성 | 판 | 완주 |").AppendLine("|---|---|---|");
            foreach (var kv in r.Comps.OrderByDescending(x => x.Value.win)) sb.AppendLine($"| {kv.Key} | {kv.Value.n} | {kv.Value.win:0.0}% |");
            sb.AppendLine().AppendLine("## 운영 방식 평균").AppendLine().AppendLine("| 방식 | 사도 | 평균 완주 | 가장 낮음 · 높음 |").AppendLine("|---|---|---|---|");
            foreach (var g in r.Heroes.GroupBy(h => d.Hero(h.Key).Style ?? "-").OrderByDescending(g => g.Average(x => x.Win)))
                sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Average(x => x.Win):0.0}% | {g.Min(x => x.Win):0} ~ {g.Max(x => x.Win):0} |");
            if (pairs != null && pairs.Count > 0)
            {
                sb.AppendLine().AppendLine("## 짝 시너지 — 같은 방식 · 같은 성격").AppendLine();
                sb.AppendLine("그 칸의 사도 둘 + 다른 방식 하나(짝) 와 그 둘 가운데 하나 + 다른 방식 둘(바탕)을 같은 씨앗 · 같은 셋째 자리로 견줬다. 목표(docs/19 §8)는 +10%p 이상.").AppendLine();
                sb.AppendLine("| 방식 | 성격 | 사도 | 판 | 짝 | 바탕 | 차이 |").AppendLine("|---|---|---|---|---|---|---|");
                foreach (var p in pairs.OrderByDescending(x => x.Lift)) sb.AppendLine($"| {p.Style} | {p.Nature} | {string.Join(" · ", p.Heroes)} | {p.N} | {p.Pair:0.0}% | {p.Base:0.0}% | {(p.Lift >= 0 ? "+" : "")}{p.Lift:0.0} |");
                sb.AppendLine().AppendLine($"평균 차이 {pairs.Average(x => x.Lift):+0.0;-0.0}%p · +10%p 이상 {pairs.Count(x => x.Lift >= 10)} / {pairs.Count} 칸.");
            }
            sb.AppendLine().AppendLine("## 사도").AppendLine().AppendLine("| 사도 | 역할 | 성격 | 방식 | 판 | 완주 |").AppendLine("|---|---|---|---|---|---|");
            foreach (var h in r.Heroes) { var hd = d.Hero(h.Key); sb.AppendLine($"| {h.Name} | {h.Role} | {hd.Nature} | {hd.Style} | {h.N} | {h.Win:0.0}% |"); }
            return sb.ToString();
        }

        public static string Report(GameData d, Result r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"메타 통계 — {r.Label} {r.Runs}판(바퀴 {r.Rounds}) · 전체 완주 {r.Clear:0.0}% · {r.Sec:0}초{(r.Hpx != 1 || r.Dmgx != 1 ? $" · 적 체력 ×{r.Hpx} 피해 ×{r.Dmgx}" : "")}");
            sb.AppendLine($"  끝난 덱 — 평균 {r.AvgDeck:0.0}장 · 고유 {r.AvgUniques:0.0} · 기본 {r.AvgBasics:0.0} · 상점 빼기 판당 {r.AvgRemovals:0.00}");
            sb.AppendLine($"  마을  {string.Join(" · ", r.Villages.Select(kv => $"{d.Villages[kv.Key].Name} {kv.Value.win:0.0}%({kv.Value.n}판)"))} · 쓰러진 층 1층 {r.Fell[0]:0.0}% · 2층 {r.Fell[1]:0.0}%");
            sb.AppendLine($"  평균 턴 — 일반 싸움 {r.AvgTurnsFight:0.0} · 보스 {r.AvgTurnsBoss:0.0}");
            if (r.FlagPct.Count > 0) sb.AppendLine($"  깃발(연속 이벤트) — 판당 선 깃발 {r.FlagsPerRun:0.00} · 줄기 뒤 이벤트를 만남 {r.FlagReadsPerRun:0.000} · {string.Join(" · ", r.FlagPct.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:0.0}%"))}");
            sb.AppendLine($"  격파 — 판당 {r.BreaksPerRun:0.0} · 싸움당 {string.Join(" · ", r.BreaksPerFight.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value:0.00}"))} · 강인도 깎은 카드 가운데 약점 공격 {r.WeakPct:0}%");
            sb.AppendLine("편성(역할 셋)");
            foreach (var kv in r.Comps.OrderByDescending(x => x.Value.win)) sb.AppendLine($"  {kv.Key}  {kv.Value.win,5:0}%  ({kv.Value.n}판)");
            sb.AppendLine("사도");
            foreach (var h in r.Heroes) sb.AppendLine($"  {h.Name}({h.Role}) {h.Win:0.0}% ({h.N}판)");
            return sb.ToString();
        }
    }
}
