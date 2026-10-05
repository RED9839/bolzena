using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>상태 하나의 모습 — 칩 · 적 상세 창. Sources 는 겹마다 건 쪽(합이 Stacks 를 넘지 않게 맞춘다).</summary>
    public sealed class StatusView
    {
        public string Id;
        public int Stacks;
        /// <summary>party(파티 층) · hero(개인 층 · 사도 고유 효과) · enemy(적 몸) · card.</summary>
        public string Layer;
        /// <summary>사도 고유 효과(키워드)의 겹인가 · 적의 쌓이는 수치인가.</summary>
        public bool Keyword, Counter;
        public List<StatusSource> Sources = new();
    }

    /// <summary>
    /// 건 쪽 하나 — Kind: hero(사도 카드 · 패시브 · 고학년) · gear(장비 효과 — Hero 가 낀 사도) · enemy(적 — Enemy 가 그 적의 Idx) · neutral(교주 카드) · event(이벤트 「다음 전투」).
    /// </summary>
    public sealed class StatusSource
    {
        public string Kind;
        public string Hero;
        public int? Enemy;
        public int Stacks;
    }

    public sealed partial class Battle
    {
        /// <summary>지금 도는 장비 효과의 출처(장비 패시브가 거는 상태).</summary>
        string gearSrc;

        Dictionary<string, Dictionary<string, int>> SrcBag(Unit u, string id) =>
            u.Side == Side.Party && u.BodyRef != null && !R.HERO_ST.Contains(id) ? u.BodyRef.Src : u.Src;

        void AddSrc(Unit u, string id, string src, int v)
        {
            var bag = SrcBag(u, id);
            if (!bag.TryGetValue(id, out var d)) bag[id] = d = new Dictionary<string, int>();
            d[src] = (d.TryGetValue(src, out var x) ? x : 0) + v;
        }

        /// <summary>효과를 건 쪽의 이름 — 사도면 hero:키, 적이면 enemy:Idx, 장비 패시브면 gear:키, 주인 없는 카드면 neutral.</summary>
        string SrcOf(Unit by)
        {
            if (gearSrc != null) return gearSrc;
            if (by != null && by.Side == Side.Party && by.BodyRef != null) return "hero:" + by.Key;
            if (by != null && by.Side == Side.Enemy) return "enemy:" + by.Idx;
            if (Acting != null) return "hero:" + Acting;
            return "neutral";
        }

        static StatusSource ParseSrc(string s, int n)
        {
            int c = s.IndexOf(':');
            string kind = c < 0 ? s : s.Substring(0, c), rest = c < 0 ? null : s.Substring(c + 1);
            var o = new StatusSource { Kind = kind, Stacks = n };
            if (kind == "enemy" && int.TryParse(rest, out var i)) o.Enemy = i; else o.Hero = rest;
            return o;
        }

        /// <summary>건 쪽 목록 — 기록이 지금 겹보다 많으면(겹이 줄었으면) 먼저 건 쪽부터 덜어 합을 맞춘다. 기록이 모자라면 모르는 몫은 neutral 로.</summary>
        List<StatusSource> SourcesOf(Unit u, string id, int stacks)
        {
            var o = new List<StatusSource>();
            if (stacks <= 0) return o;
            if (SrcBag(u, id).TryGetValue(id, out var d))
                foreach (var kv in d) if (kv.Value > 0) o.Add(ParseSrc(kv.Key, kv.Value));
            int sum = o.Sum(s => s.Stacks);
            for (int i = 0; sum > stacks && i < o.Count; i++) { int cut = Math.Min(o[i].Stacks, sum - stacks); o[i].Stacks -= cut; sum -= cut; }
            o.RemoveAll(s => s.Stacks <= 0);
            if (sum < stacks && !R.INTENSITY_ST.Contains(id) && o.Count == 0) o.Add(new StatusSource { Kind = "neutral", Stacks = stacks - sum });
            return o;
        }

        /// <summary>
        /// 상태 칩 목록(건 쪽까지) — 적이면 그 적의 상태 · 쌓이는 수치 · 적에게 건 사도 표식, 파티 몸(b.Pool)이면 파티 층,
        /// 사도면 개인 층(구속) + 그 사도 고유 효과(자기 주머니 · 사도 표시). 세기 상태는 Stacks 가 겹, 횟수 상태는 남은 횟수.
        /// </summary>
        public List<StatusView> StatusViews(Unit u)
        {
            var o = new List<StatusView>();
            if (u == null) return o;
            if (u.Side == Side.Enemy)
            {
                foreach (var kv in u.Status.Where(kv => kv.Value != 0))
                {
                    bool counter = CounterOf(u, kv.Key) != null, kw = Kw.ContainsKey(kv.Key);
                    var v = new StatusView { Id = kv.Key, Stacks = kv.Value, Layer = "enemy", Counter = counter, Keyword = kw };
                    if (kw && Kw[kv.Key].Owner != null) v.Sources.Add(new StatusSource { Kind = "hero", Hero = Kw[kv.Key].Owner, Stacks = kv.Value });
                    else if (!counter) v.Sources = SourcesOf(u, kv.Key, kv.Value);
                    o.Add(v);
                }
                if (u.Sealed && !u.Broken) o.Add(new StatusView { Id = R.STUN, Stacks = 1, Layer = "enemy" });
                return o;
            }
            if (u == Pool || u.BodyRef == null)
            {
                foreach (var kv in Pool.Status.Where(kv => kv.Value != 0))
                {
                    bool kw = Kw.ContainsKey(kv.Key);
                    var v = new StatusView { Id = kv.Key, Stacks = kv.Value, Layer = "party", Keyword = kw };
                    if (kw) v.Sources.Add(new StatusSource { Kind = "hero", Hero = Kw[kv.Key].Owner, Stacks = kv.Value });
                    else v.Sources = SourcesOf(Pool, kv.Key, kv.Value);
                    o.Add(v);
                }
                return o;
            }
            foreach (var kv in u.Status.Where(kv => kv.Value != 0))
                o.Add(new StatusView { Id = kv.Key, Stacks = kv.Value, Layer = "hero", Sources = SourcesOf(u, kv.Key, kv.Value) });
            if (Stacks.TryGetValue(u.Key, out var bag))
                foreach (var kv in bag.Where(kv => kv.Value != 0))
                {
                    var owner = Kw.TryGetValue(kv.Key, out var k) ? k.Owner : u.Key;
                    o.Add(new StatusView { Id = kv.Key, Stacks = kv.Value, Layer = "hero", Keyword = true, Sources = new List<StatusSource> { new StatusSource { Kind = "hero", Hero = owner, Stacks = kv.Value } } });
                }
            return o;
        }
    }
}
