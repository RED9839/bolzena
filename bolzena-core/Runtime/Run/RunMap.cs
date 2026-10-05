using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 지도 — 층마다 열 칸 길(1-1 ~ 1-10), 줄마다 칸 2~4개. 1-1 은 일반 전투만, 1-2 ~ 1-8 은 일반 · 엘리트 · 휴식 · 휴식(상점) · 이벤트,
    /// 1-9 는 휴식(상점) 하나, 1-10 은 보스. 선은 같은 자리 · 바로 옆 자리로만(엇갈리지 않는다). 씨앗 · 층 · 마을로 정해진다.
    /// </summary>
    public sealed partial class Run
    {
        public const int MAP_ROWS = 10;
        public const int MAX_LANES = 4;
        public static readonly Dictionary<string, string> KIND_KO = new() { ["start"] = "출발", ["fight"] = "일반", ["elite"] = "엘리트", ["camp"] = "휴식", ["campshop"] = "휴식+상점", ["event"] = "이벤트", ["boss"] = "보스" };

        static List<(string kind, int w)> KindWeights(int r) => new()
        {
            ("fight", 46), ("event", 22), ("elite", r >= 3 ? 17 : 0), ("camp", r >= 3 ? 8 : 0), ("campshop", r >= 3 && r <= MAP_ROWS - 4 ? 20 : 0),
        };
        static int TierOf(int r) => r < 3 ? 0 : r < 5 ? 1 : 2;

        static List<string> PickFoes(GameData d, Rng rng, string village, int floor, MapNode node, List<List<string>> prev)
        {
            var fl = d.Villages[village].Floors; var F = floor < fl.Count ? fl[floor] : fl[0];
            var pool = node.Type == "elite" ? (F.Elites.Count > 0 ? F.Elites : new List<List<string>> { F.Pools[2][0] }) : F.Pools[Math.Min(node.Fight, F.Pools.Count - 1)];
            var fresh = pool.Where(p => !prev.Any(q => q.SequenceEqual(p))).ToList();
            var from = fresh.Count > 0 ? fresh : pool;
            return from[(int)Math.Floor(rng.Next() * from.Count)].ToList();
        }

        public static MapState GenMap(GameData d, long seed, int floor, string village)
        {
            var rng = new Rng(unchecked((uint)seed ^ ((uint)(floor + 1) * 2654435761u)));
            string Weighted(List<(string kind, int w)> ws)
            {
                double tot = ws.Sum(x => x.w);
                double x = rng.Next() * tot;
                foreach (var (k, w) in ws) if ((x -= w) < 0) return k;
                return ws[0].kind;
            }
            List<int> Shuffle(List<int> arr) { for (int i = arr.Count - 1; i > 0; i--) { int j = (int)Math.Floor(rng.Next() * (i + 1)); (arr[i], arr[j]) = (arr[j], arr[i]); } return arr; }

            // ① 뼈대 — 한 줄에 자리 넷. 선은 같은 자리 · 바로 옆으로, 엇갈리지 않게
            var lanesOf = new List<List<int>>();
            var links = new List<List<(int a, int b)>>();
            lanesOf.Add(Shuffle(new List<int> { 0, 1, 2, 3 }).Take(2 + (int)Math.Floor(rng.Next() * (MAX_LANES - 1))).OrderBy(x => x).ToList());
            for (int r = 0; r < MAP_ROWS - 3; r++)
            {
                var from = lanesOf[r]; var L = new List<(int a, int b)>();
                bool Crosses(int i, int j) => L.Any(p => (p.a < i && p.b > j) || (p.a > i && p.b < j));
                foreach (var i in from)
                {
                    var opts = Shuffle(new List<int> { i - 1, i, i + 1 }.Where(j => j >= 0 && j < MAX_LANES).ToList());
                    int made = 0, want = rng.Next() < 0.38 ? 2 : 1;
                    foreach (var j in opts)
                    {
                        if (made >= want) break;
                        if (Crosses(i, j)) continue;
                        L.Add((i, j)); made++;
                    }
                    if (made == 0) L.Add((i, i));
                }
                var to = L.Select(p => p.b).Distinct().ToList();
                if (to.Count < 2)
                {
                    foreach (var i in Shuffle(from.ToList()))
                    {
                        int? j = new[] { i - 1, i + 1 }.Where(x => x >= 0 && x < MAX_LANES && !to.Contains(x) && !Crosses(i, x)).Select(x => (int?)x).FirstOrDefault();
                        if (j != null) { L.Add((i, j.Value)); to.Add(j.Value); break; }
                    }
                }
                lanesOf.Add(to.OrderBy(x => x).ToList());
                links.Add(L);
            }

            // ② 칸 종류
            var rows = new List<List<MapNode>>();
            for (int r = 0; r < MAP_ROWS; r++)
            {
                bool last = r == MAP_ROWS - 1, beforeBoss = r == MAP_ROWS - 2;
                var lanes = last || beforeBoss ? new List<int?> { null } : lanesOf[r].Select(x => (int?)x).ToList();
                var kinds = lanes.Select(_ => last ? "boss" : beforeBoss ? "campshop" : r == 0 ? "fight" : Weighted(KindWeights(r))).ToList();
                if (!last && !beforeBoss && !kinds.Any(k => k == "fight" || k == "elite")) kinds[(int)Math.Floor(rng.Next() * kinds.Count)] = "fight";
                var row = new List<MapNode>();
                for (int c = 0; c < lanes.Count; c++)
                {
                    var n = new MapNode { Id = $"r{r}c{c}", Row = r, Col = c, Lane = lanes[c], X = lanes[c] == null ? 0.5 : lanes[c].Value / (double)(MAX_LANES - 1), Type = kinds[c] };
                    if (n.Type == "fight") n.Fight = TierOf(r);
                    if (n.Type == "elite") n.Fight = Math.Min(2, TierOf(r) + 1);
                    row.Add(n);
                }
                rows.Add(row);
            }
            // ③ 잇기
            for (int r = 0; r < MAP_ROWS - 1; r++)
            {
                var a = rows[r]; var b = rows[r + 1];
                if (b.Count == 1) { foreach (var n in a) n.Next = new List<string> { b[0].Id }; continue; }
                foreach (var (i, j) in links[r])
                {
                    var n = a.FirstOrDefault(z => z.Lane == i); var t = b.FirstOrDefault(z => z.Lane == j);
                    if (n != null && t != null && !n.Next.Contains(t.Id)) n.Next.Add(t.Id);
                }
                foreach (var n in a) n.Next = n.Next.OrderBy(id => b.First(z => z.Id == id).Col).ToList();
            }
            // ③½ 적 — 칸마다 미리. 들어오는 칸들의 짝과 겹치지 않게
            for (int r = 0; r < rows.Count; r++)
                foreach (var n in rows[r])
                {
                    if (n.Type != "fight" && n.Type != "elite") continue;
                    var prev = r > 0 ? rows[r - 1].Where(p => p.Next.Contains(n.Id) && p.Foes != null).Select(p => p.Foes).ToList() : new List<List<string>>();
                    n.Foes = PickFoes(d, rng, village, floor, n, prev);
                }
            // ④ 출발 칸
            var rename = new Dictionary<string, string>();
            foreach (var row in rows) foreach (var n in row) { n.Row += 1; var id = $"r{n.Row}c{n.Col}"; rename[n.Id] = id; n.Id = id; }
            foreach (var row in rows) foreach (var n in row) n.Next = n.Next.Select(x => rename[x]).ToList();
            var start = new MapNode { Id = "r0c0", Row = 0, Col = 0, X = 0.5, Type = "start", Next = rows[0].Select(n => n.Id).ToList() };
            rows.Insert(0, new List<MapNode> { start });
            return new MapState { Floor = floor, Village = village, Rows = rows, At = start.Id, Seen = new List<string> { start.Id } };
        }

        /// <summary>이 판의 지금 층 지도 — 없거나 층이 바뀌었으면 새로 그린다.</summary>
        public MapState MapOf()
        {
            if (S.Map == null || S.Map.Floor != S.Floor || S.Map.Village != S.Village) S.Map = GenMap(Data, S.Seed, S.Floor, S.Village);
            return S.Map;
        }

        public static MapNode NodeById(MapState m, string id) => m.Rows.SelectMany(r => r).FirstOrDefault(n => n.Id == id);
        public MapNode CurrentNode => S.Map?.At != null ? NodeById(S.Map, S.Map.At) : null;
        public string StageName(MapNode n) => $"{S.Floor + 1}-{n.Row}";

        /// <summary>지금 고를 수 있는 칸.</summary>
        public List<string> Reachable()
        {
            var m = MapOf();
            if (m.At == null) return m.Rows[1].Select(n => n.Id).ToList();
            return NodeById(m, m.At)?.Next.ToList() ?? new List<string>();
        }

        /// <summary>앞으로 닿을 수 있는 칸 전부(화면이 그 밖을 흐리게).</summary>
        public HashSet<string> AheadOf()
        {
            var m = MapOf();
            var outs = new HashSet<string>(Reachable());
            foreach (var row in m.Rows) foreach (var n in row) if (outs.Contains(n.Id)) foreach (var x in n.Next) outs.Add(x);
            return outs;
        }

        /// <summary>칸에 들어간다 — 싸움 칸이면 세기(Node) · 엘리트를 정한다. 보스는 Node 3.</summary>
        public MapNode EnterNode(string id)
        {
            var m = MapOf();
            if (!Reachable().Contains(id)) return null;
            var node = NodeById(m, id);
            m.At = id; m.Seen.Add(id);
            S.Step++;
            S.Elite = node.Type == "elite";
            if (node.Type == "fight" || node.Type == "elite") S.Node = node.Fight;
            if (node.Type == "boss") S.Node = 3;
            return node;
        }

        public List<string> EnemiesAt(MapNode node)
        {
            var f = CurrentFloor;
            if (node.Type == "boss") return f.Boss;
            if (node.Type == "fight" || node.Type == "elite") return node.Foes ?? f.Pools[node.Fight][0];
            return new List<string>();
        }
    }
}
