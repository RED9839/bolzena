using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 고유 효과(키워드) 계측 — 시뮬 전용(2026-10-05, 스택형 점검). <see cref="On"/> 이 켜져 있을 때 새로 연 전투만 잰다
    /// (봇이 수를 읽으려고 복사한 판은 재지 않는다 — <see cref="Battle.Clone"/> 이 끈다).
    /// 사도 · 키워드마다: 쌓으려 한 양 · 실제로 쌓인 양(넘친 몫 = 버려짐) · 쓴 양 · 처음 최대에 닿은 턴 · 최대로 시작한 턴의 몫.
    /// </summary>
    public static class KwMeter
    {
        public static volatile bool On;

        public sealed class Row
        {
            public string Hero, Id, Carrier;
            public int Cap;
            /// <summary>그 사도가 낀 싸움 · 그 싸움들의 턴 · 턴 시작에 최대였던 턴.</summary>
            public long Fights, Turns, TurnsAtCap;
            /// <summary>최대에 한 번이라도 닿은 싸움 · 그 싸움들에서 처음 닿은 턴의 합.</summary>
            public long CapFights, CapTurnSum;
            /// <summary>쌓으려 한 양 · 실제로 쌓인 양 · 쓴 양(spend · 비용 · 발동) · 쓴 횟수 · 한 번이라도 쌓인 싸움.</summary>
            public long Asked, Got, Spent, SpendN, GainFights;
            public double Waste => Asked > 0 ? (double)(Asked - Got) / Asked : 0;
            public double CapTurn => CapFights > 0 ? (double)CapTurnSum / CapFights : 0;
            public double CapShare => Turns > 0 ? (double)TurnsAtCap / Turns : 0;
        }

        static readonly ConcurrentDictionary<string, Row> rows = new();
        static readonly object gate = new();

        public static void Reset() => rows.Clear();
        public static List<Row> Rows() => rows.Values.OrderBy(r => r.Hero, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();

        internal static void Merge(IEnumerable<Battle.MeterRow> fight)
        {
            lock (gate)
                foreach (var m in fight)
                {
                    var r = rows.GetOrAdd(m.Hero + "|" + m.Id, _ => new Row { Hero = m.Hero, Id = m.Id, Carrier = m.Carrier, Cap = m.Cap });
                    r.Fights++; r.Turns += m.Turns; r.TurnsAtCap += m.TurnsAtCap;
                    if (m.FirstCap > 0) { r.CapFights++; r.CapTurnSum += m.FirstCap; }
                    r.Asked += m.Asked; r.Got += m.Got; r.Spent += m.Spent; r.SpendN += m.SpendN;
                    if (m.Got > 0) r.GainFights++;
                }
        }
    }

    public sealed partial class Battle
    {
        /// <summary>계측 한 줄(이 전투) — <see cref="KwMeter"/>.</summary>
        public sealed class MeterRow
        {
            public string Hero, Id, Carrier;
            public int Cap, Turns, TurnsAtCap, FirstCap;
            public long Asked, Got, Spent, SpendN;
        }

        /// <summary>이 전투의 계측(null = 재지 않는다 — 봇의 복사본 · 화면 · 테스트).</summary>
        internal Dictionary<string, MeterRow> Meter;

        void MeterInit()
        {
            if (!KwMeter.On) return;
            Meter = new Dictionary<string, MeterRow>();
            foreach (var kw in Kw.Values)
            {
                if (kw.Owner == null || !Party.Any(u => u.Key == kw.Owner)) continue;
                var d = kw.Def;
                int cap = d.Mode || d.Wrap ? 0 : d.Cap ?? 0;
                Meter[kw.Id] = new MeterRow { Hero = kw.Owner, Id = kw.Id, Carrier = kw.Carrier, Cap = cap };
            }
        }

        /// <summary>지금 겹 — self 는 주인 주머니, enemy 는 가장 많이 든 적, hero 는 가장 많이 든 사도, ally 는 파티.</summary>
        int MeterNow(KwRt kw)
        {
            switch (kw.Carrier)
            {
                case "enemy": return Enemies.Where(e => !e.Dead).Select(e => St(e, kw.Id)).DefaultIfEmpty(0).Max();
                case "hero": return Party.Select(u => StackOf(u.Key, kw.Id)).DefaultIfEmpty(0).Max();
                case "ally": return St(Pool, kw.Id);
                default: return StackOf(kw.Owner, kw.Id);
            }
        }

        void MeterGain(string id, int asked, int got, int now)
        {
            if (Meter == null || !Meter.TryGetValue(id, out var m)) return;
            if (asked > 0) { m.Asked += asked; m.Got += Math.Max(0, Math.Min(asked, got)); }
            if (m.Cap > 0 && now >= m.Cap && m.FirstCap == 0) m.FirstCap = Math.Max(1, Turn);
        }

        void MeterSpend(string id, int n)
        {
            if (Meter == null || n <= 0 || !Meter.TryGetValue(id, out var m)) return;
            m.Spent += n; m.SpendN++;
        }

        /// <summary>내 턴 시작(손패를 뽑기 전) — 턴 수 · 최대로 시작한 턴.</summary>
        void MeterTurn()
        {
            if (Meter == null) return;
            foreach (var kw in Kw.Values)
            {
                if (!Meter.TryGetValue(kw.Id, out var m)) continue;
                m.Turns++;
                if (m.Cap > 0 && MeterNow(kw) >= m.Cap) m.TurnsAtCap++;
            }
        }

        /// <summary>싸움이 끝났다 — 모은 것을 넘기고 끈다(한 번만).</summary>
        void MeterFlush()
        {
            if (Meter == null) return;
            var m = Meter; Meter = null;
            KwMeter.Merge(m.Values);
        }
    }
}
