using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 카드 목록의 순서 — 판 화면 · 전투 화면이 카드를 늘어놓는 곳은 모두 이것 하나를 쓴다(사용자 규칙).
    ///   사도 순서(편성 1 · 2 · 3, 파티 밖 사도는 그 뒤 데이터 순서) → 그 사도 안에서 기본 카드(시작 카드) 먼저 → 고유 카드
    ///   → 상태 · 저주 → 맨 마지막 교주 카드.
    ///   같은 카드(같은 id — 복제 · 신탁 얹은 것도 id 가 같으면)는 나란히, 그 안은 카드 정의 순서(기본 카드는 사도의 Starter 순서).
    /// 상태 · 저주를 사도 묶음 뒤 · 교주 카드 앞에 두는 까닭: 사도가 만든 카드가 아니라 적 · 이벤트가 넣은 것이라
    /// 어느 사도 묶음에 넣어도 그 사도의 카드로 읽힌다. 교주 카드는 사용자 규칙대로 맨 끝.
    /// </summary>
    public static class CardOrder
    {
        /// <summary>묶음 종류 — 사도 · 상태/저주 · 교주.</summary>
        public enum Kind { Hero, Status, Leader }

        public sealed class Group
        {
            public Kind Kind;
            /// <summary>사도 id(Kind.Hero 일 때) — 아니면 null.</summary>
            public string Hero;
            /// <summary>이 묶음의 카드 id(정렬된 채, 같은 카드는 그 수만큼).</summary>
            public List<string> Ids = new List<string>();
            public string Title => Kind == Kind.Leader ? "교주 카드" : Kind == Kind.Status ? "상태 · 저주" : Hero;
        }

        /// <summary>정렬한 카드 id 목록. party 는 편성 순서(없으면 데이터 순서).</summary>
        public static List<string> Sort(IEnumerable<string> ids, GameData data, IList<string> party = null)
            => Groups(ids, data, party).SelectMany(g => g.Ids).ToList();

        /// <summary>사도별 묶음 — 화면이 묶음마다 머리표(초상 + 이름)를 붙일 때.</summary>
        public static List<Group> Groups(IEnumerable<string> ids, GameData data, IList<string> party = null)
        {
            var list = (ids ?? Enumerable.Empty<string>()).Where(x => x != null).ToList();
            var defIndex = DefIndex(data);
            var heroIndex = new Dictionary<string, int>();
            if (party != null) for (int i = 0; i < party.Count; i++) heroIndex[party[i]] = i;
            int hi = 0;
            foreach (var h in data.Heroes.Keys) if (!heroIndex.ContainsKey(h)) heroIndex[h] = 1000 + hi++;

            var groups = new List<Group>();
            Group G(Kind k, string hero)
            {
                var g = groups.FirstOrDefault(x => x.Kind == k && x.Hero == hero);
                if (g == null) { g = new Group { Kind = k, Hero = hero }; groups.Add(g); }
                return g;
            }
            foreach (var id in list)
            {
                var c = data.Card(id);
                if (c != null && c.Hero != null) G(Kind.Hero, c.Hero).Ids.Add(id);
                else if (c != null && (c.IsStatusCard || c.IsCurse)) G(Kind.Status, null).Ids.Add(id);
                else G(Kind.Leader, null).Ids.Add(id);
            }
            foreach (var g in groups)
            {
                HeroDef hd = g.Hero != null && data.Heroes.TryGetValue(g.Hero, out var h0) ? h0 : null;
                g.Ids = g.Ids
                    .OrderBy(id => data.Card(id)?.Unique == true ? 1 : 0)                                   // 기본 → 고유
                    .ThenBy(id => StarterIndex(hd, id))                                                     // 기본 카드는 Starter 순서
                    .ThenBy(id => defIndex.TryGetValue(GameData.BaseId(id), out var di) ? di : int.MaxValue) // 카드 정의 순서
                    .ThenBy(id => GameData.OwnerOf(id) is string o && heroIndex.TryGetValue(o, out var oi) ? oi : -1)   // 교주 카드는 주인 편성 순서(주인 없으면 앞)
                    .ThenBy(id => id, StringComparer.Ordinal)                                              // 같은 카드는 나란히
                    .ToList();
            }
            return groups
                .OrderBy(g => g.Kind == Kind.Hero ? 0 : g.Kind == Kind.Status ? 1 : 2)
                .ThenBy(g => g.Hero != null && heroIndex.TryGetValue(g.Hero, out var x) ? x : int.MaxValue)
                .ToList();
        }

        static int StarterIndex(HeroDef h, string id)
        {
            if (h?.Starter == null) return int.MaxValue;
            int i = h.Starter.IndexOf(GameData.BaseId(id));
            return i < 0 ? int.MaxValue : i;
        }

        static GameData lastData;
        static Dictionary<string, int> lastIndex;
        static Dictionary<string, int> DefIndex(GameData d)
        {
            if (ReferenceEquals(d, lastData) && lastIndex != null) return lastIndex;
            var m = new Dictionary<string, int>();
            int i = 0;
            foreach (var k in d.Cards.Keys) m[k] = i++;
            lastData = d; lastIndex = m;
            return m;
        }
    }
}
