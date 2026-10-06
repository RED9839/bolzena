using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 시뮬 편성 고르기(2026-10-06) — 판마다 파티 셋을 어떻게 짜나.
    ///   random  — 옛 방식(MetaSim.Jobs): 사도 135 를 섞어 셋씩. 역할 · 시너지를 안 본다.
    ///   role    — 역할을 섞은 편성: 기준 사도(사도마다 고르게 돈다) + 그 역할이 든 섞인 틀(TSD · TDD · TSS · SDD · TTD · TTS) 의 나머지 자리를 그 역할에서 무작위로.
    ///   synergy — role 과 같은 틀 · 같은 기준 사도, 나머지 자리는 시너지 점수(키워드 맞물림 + 원작 관계)가 높은 셋째 · 넷째 가운데서.
    /// 판의 씨앗은 MetaSim.Jobs 와 같은 식(같은 seed · 같은 판 번호면 같은 판 씨앗 · 같은 마을) — 모드끼리 견줄 수 있다.
    /// 무작위는 제 LCG 만 쓴다(같은 seed 면 같은 편성).
    /// </summary>
    public static class PartyPick
    {
        public const string Random = "random", Role = "role", Synergy = "synergy";
        public static readonly string[] MODES = { Random, Role, Synergy };

        /// <summary>섞인 역할 틀과 무게 — 탱커 · 서포터 · 딜러가 고루 든 TSD 가 가장 흔하다.</summary>
        public static readonly (string comp, double w)[] MIXED = { ("TSD", 5), ("TDD", 2), ("TSS", 1), ("SDD", 1), ("TTD", 0.5), ("TTS", 0.5) };
        static readonly Dictionary<string, char> LETTER = new() { ["탱커"] = 'T', ["서포터"] = 'S', ["딜러"] = 'D' };
        /// <summary>원작 관계 한 쌍의 무게(log2(1 + 함께 나온 횟수) 에 곱한다).</summary>
        public const double REL_W = 0.5;
        /// <summary>시너지 편성에서 셋째 · 넷째를 고를 때 위에서 몇 명 가운데 무작위로 고르나(늘 같은 짝만 나오지 않게).</summary>
        public const int TOP = 3;

        public static char RoleOf(GameData d, string k) => LETTER.TryGetValue(d.Hero(k)?.Role ?? "", out var c) ? c : 'D';

        /// <summary>
        /// 원작 관계(사도 데스크 relations.json — 함께 나온 횟수) 를 사도 id 쌍으로. 관계 파일의 사도는 이름(ko)으로 잇는다(「티그(영웅)」 꼴도 그대로).
        /// 돌려줌: (id, id) → log2(1 + 횟수). 같은 쌍은 큰 쪽.
        /// </summary>
        public static Dictionary<(string, string), double> Relations(GameData d, string json)
        {
            var o = new Dictionary<(string, string), double>();
            if (string.IsNullOrEmpty(json)) return o;
            var root = Newtonsoft.Json.Linq.JObject.Parse(json);
            var byName = new Dictionary<string, List<string>>();
            foreach (var h in d.Heroes.Values)
            {
                if (!byName.TryGetValue(h.Name, out var l)) byName[h.Name] = l = new List<string>();
                l.Add(h.Id);
            }
            var keyToIds = new Dictionary<string, List<string>>();
            foreach (var p in root.Properties())
            {
                if (p.Name.StartsWith("_")) continue;
                var ko = (string)p.Value["ko"];
                if (ko != null && byName.TryGetValue(ko, out var ids)) keyToIds[p.Name] = ids;
            }
            foreach (var p in root.Properties())
            {
                if (!keyToIds.TryGetValue(p.Name, out var a) || p.Value["with"] is not Newtonsoft.Json.Linq.JObject w) continue;
                foreach (var q in w.Properties())
                {
                    if (!keyToIds.TryGetValue(q.Name, out var b)) continue;
                    double v = Math.Log(1 + (double)q.Value, 2);
                    foreach (var x in a) foreach (var y in b)
                    {
                        if (x == y) continue;
                        var k = string.CompareOrdinal(x, y) < 0 ? (x, y) : (y, x);
                        if (!o.TryGetValue(k, out var old) || old < v) o[k] = v;
                    }
                }
            }
            return o;
        }

        /// <summary>사도 둘의 시너지 — 키워드 맞물림(MetaSim.Mesh 와 같은 셈, 미리 센 Gives · Hears) + 원작 관계.</summary>
        public sealed class SynScore
        {
            readonly Dictionary<string, HashSet<string>> gives = new(), hears = new();
            readonly Dictionary<(string, string), double> rel;
            readonly Dictionary<(string, string), double> memo = new();
            public SynScore(GameData d, Dictionary<(string, string), double> relations = null)
            {
                rel = relations ?? new Dictionary<(string, string), double>();
                foreach (var h in d.Heroes.Values) { gives[h.Id] = DeckPlan.HeroGives(d, h); hears[h.Id] = DeckPlan.HeroHears(d, h); }
            }
            public double Mesh(string a, string b)
            {
                var ga = gives[a]; var gb = gives[b]; var ha = hears[a]; var hb = hears[b];
                return ga.Count(x => hb.Contains(x) && !x.StartsWith("kw:") || x.StartsWith("kw:") && hb.Contains(x) && !ha.Contains(x))
                     + gb.Count(x => ha.Contains(x) && !x.StartsWith("kw:") || x.StartsWith("kw:") && ha.Contains(x) && !hb.Contains(x))
                     + (ha.Contains("any") ? 1 : 0) + (hb.Contains("any") ? 1 : 0);
            }
            public double Rel(string a, string b) => rel.TryGetValue(string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a), out var v) ? v : 0;
            public double Pair(string a, string b)
            {
                var k = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
                lock (memo)
                {
                    if (memo.TryGetValue(k, out var v)) return v;
                    v = Mesh(a, b) + REL_W * Rel(a, b);
                    memo[k] = v;
                    return v;
                }
            }
            public double Party(IList<string> p) { double s = 0; for (int i = 0; i < p.Count; i++) for (int j = i + 1; j < p.Count; j++) s += Pair(p[i], p[j]); return s; }
        }

        /// <summary>
        /// 편성 n 판 — 바퀴(rounds) 마다 (사도 수 / 3) 판, MetaSim.Jobs 와 같은 판 수 · 같은 판 씨앗.
        /// mode 가 random 이면 MetaSim.Jobs 그대로.
        /// </summary>
        public static List<(int i, List<string> party, long seed)> Jobs(GameData d, int rounds, int seed, string mode, SynScore syn = null, List<string> heroes = null)
        {
            if (mode == null || mode == Random) return MetaSim.Jobs(d, rounds, seed, heroes);
            var all = (heroes ?? d.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList()).ToList();
            var byRole = all.GroupBy(k => RoleOf(d, k)).ToDictionary(g => g.Key, g => g.OrderBy(x => x, StringComparer.Ordinal).ToList());
            if (mode == Synergy) syn ??= new SynScore(d);
            uint s = unchecked((uint)(888 + seed * 104729 + (mode == Synergy ? 17 : 0)));
            double Rnd() { s = unchecked(s * 1103515245u + 12345u); return s / 4294967296.0; }
            int per = all.Count / 3, total = rounds * per;
            // 기준 사도 — 섞은 사도 줄을 차례로(사도마다 고르게 기준이 된다)
            var anchors = new List<string>();
            while (anchors.Count < total)
            {
                var pool = all.ToList();
                for (int i = pool.Count - 1; i > 0; i--) { int j = (int)Math.Floor(Rnd() * (i + 1)); (pool[i], pool[j]) = (pool[j], pool[i]); }
                anchors.AddRange(pool);
            }
            var jobs = new List<(int, List<string>, long)>();
            for (int n = 0; n < total; n++)
            {
                string a = anchors[n]; char ra = RoleOf(d, a);
                // 틀 — 기준 사도의 역할이 든 섞인 틀(그 역할 자리 수만큼 무게), 그 역할 사도가 모자란 틀은 뺀다
                var cand = MIXED.Where(m => m.comp.IndexOf(ra) >= 0 && m.comp.GroupBy(c => c).All(g => byRole.TryGetValue(g.Key, out var l) && l.Count >= g.Count()))
                    .Select(m => (m.comp, w: m.w * m.comp.Count(c => c == ra))).ToList();
                string comp = cand.Count > 0 ? cand[cand.Count - 1].comp : "TSD";
                double r = Rnd() * cand.Sum(x => x.w);
                foreach (var x in cand) { if (r < x.w) { comp = x.comp; break; } r -= x.w; }
                var need = comp.ToList(); need.Remove(ra);
                var party = new List<string> { a };
                foreach (var role in need)
                {
                    var pool = byRole.TryGetValue(role, out var l) ? l.Where(k => !party.Contains(k)).ToList() : new List<string>();
                    if (pool.Count == 0) pool = all.Where(k => !party.Contains(k)).ToList();
                    string pick;
                    if (mode == Synergy)
                    {
                        var top = pool.Select(k => (k, v: party.Sum(p => syn.Pair(p, k)))).OrderByDescending(x => x.v).ThenBy(x => x.k, StringComparer.Ordinal).Take(TOP).ToList();
                        pick = top[Math.Min(top.Count - 1, (int)Math.Floor(Rnd() * top.Count))].k;
                    }
                    else pick = pool[Math.Min(pool.Count - 1, (int)Math.Floor(Rnd() * pool.Count))];
                    party.Add(pick);
                }
                jobs.Add((jobs.Count, party, 20000 + seed * 7919 + jobs.Count * 37));
            }
            return jobs;
        }
    }
}
