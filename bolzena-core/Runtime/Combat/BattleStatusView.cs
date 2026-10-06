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
        /// <summary>변신 칩(사도 개인 층) — Id 는 변신 이름, Stacks 는 남은 턴(0 = 전투 끝까지). 자세한 것은 b.FormOf(사도 키).</summary>
        public bool Form;
        /// <summary>강화 칩(파티 층) — 켜진 강화 카드 지속 규칙. Id 는 카드 이름, Stacks 는 겹 수. 자세한 것(규칙 글 · 주인)은 b.PowersOf().</summary>
        public bool Power;
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

    /// <summary>
    /// 적 강인도 화면 값(b.ToughViewOf). 단위 = 약점 공격 AP 1 — 1코 약점 공격 한 장이 1, 약점이 아니면 1/3 깎는다.
    /// Left · Max 는 소수(1/3 · 1/6 눈금)라 칸(Pips = Max 올림)을 그릴 땐 칸마다 채운 비율(Fill)로 그린다 — 정수로 반올림하면 1/3 이 안 보인다.
    /// </summary>
    public sealed class ToughView
    {
        public double Left, Max;
        /// <summary>칸 수(Max 올림).</summary>
        public int Pips;
        /// <summary>격파 상태(이번 턴 · 적의 다음 차례를 쉬고 다음 내 턴에 강인도가 다시 찬다) · 다음 차례를 못 움직임.</summary>
        public bool Broken, Resting;
        /// <summary>적의 성격(판의 적 속성이면 그것) · 약점 성격.</summary>
        public string Nature;
        public List<string> Weak = new();
        /// <summary>강인도 없음(Max 0 — 강인도 없는 소환물). 격파되지 않는다 — 막대를 숨긴다.</summary>
        public bool None => Max <= 0;
        /// <summary>i 번째 칸(0부터)이 찬 비율 0~1.</summary>
        public double Fill(int i) => Broken ? 0 : Math.Max(0, Math.Min(1, Left - i));
    }

    public sealed partial class Battle
    {
        /// <summary>적 강인도 화면 값 — 남은 · 최대 · 칸 · 격파 상태 · 성격 · 약점(ToughView).</summary>
        public ToughView ToughViewOf(Unit e) => e == null ? null : new ToughView
        {
            Left = e.Broken ? 0 : e.Tough, Max = e.ToughMax, Pips = (int)Math.Ceiling(e.ToughMax - 1e-6), Broken = e.Broken, Resting = e.Sealed,
            Nature = e.Nature, Weak = e.Side == Side.Enemy ? WeakOf(e).ToList() : new List<string>(),
        };

        /// <summary>그 사도가 그 적을 치면 약점 공격인가(태그 없이 — 성격 · 공명 · 적 표식). 카드 태그까지 보려면 IsWeakHit.</summary>
        public bool WeakFor(Unit hero, Unit e) => IsWeakHit(hero, e, null);

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
                foreach (var pw in Powers)
                    o.Add(new StatusView { Id = pw.Name, Stacks = pw.N, Layer = "party", Power = true, Sources = new List<StatusSource> { new StatusSource { Kind = "hero", Hero = pw.Hero, Stacks = pw.N } } });
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
            var fv = FormOf(u.Key);
            if (fv != null) o.Add(new StatusView { Id = fv.Name, Stacks = fv.Left, Layer = "hero", Form = true, Sources = new List<StatusSource> { new StatusSource { Kind = "hero", Hero = u.Key, Stacks = fv.Left } } });
            return o;
        }
    }
}
