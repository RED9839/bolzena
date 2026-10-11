using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// (엘다인 4차 ▶E87) 봇 「때 보고 쥐기」 — 보존 카드를 지금 낼 때인지, 쥐고 다음 턴에 낼 때가 나은지.
    /// 카드의 때 조각을 읽는다: 도발(taunt — 이번 적 차례 공격 합 ≥ 파티 실드 절반이면 지금) · 공격 예고 조건(ifFoe attack · anyAttack) ·
    /// 「n번째 이후」(ifNth after — 이번 턴에 그 자리에 닿나) · 처형(execute — 처형 선 안의 적이 있나 · 다음 턴 안에 들어올 적이 있나).
    /// Now 는 지금 때가 맞으면 1, Next 는 다음 턴에 맞을 확률(대충). 봇(Bots.KeepWorth)이 「다음 턴에 낼 때 값」 을 판 점수에 넣고
    /// 이번 턴 값(실제로 둬 본 판)과 견준다. 쥔 턴 · 쥔 까닭은 bz sim --trace 가 센다(Battle.TraceTurnEnd).
    /// </summary>
    public static class HoldTiming
    {
        /// <summary>다음 턴에 때가 맞을 확률(대충) — 공격 예고 · 정각 자리 · 처형 선 · 큰 공격(도발).</summary>
        public const double NEXT_ATTACK = 0.7, NEXT_NTH = 0.8, NEXT_EXEC = 0.6, NEXT_TAUNT = 0.6;
        /// <summary>처형 선 위로 이만큼(최대 HP 비율) 안이면 다음 턴 안에 선에 들어온다고 본다.</summary>
        public const double EXEC_NEAR = 0.3;
        /// <summary>쥐고 넘기면 실드가 남는 실드 소모 카드(이드 들이받기) — 다음 턴에 큰 공격이 올 확률 대신 「다음 턴에 낼 때」 확률.</summary>
        public const double NEXT_SHIELD = 0.6;

        public readonly struct T
        {
            /// <summary>쥘 까닭 — taunt · attack · nth · execute(때 조각이 없으면 null).</summary>
            public readonly string Why;
            public readonly double Now, Next;
            /// <summary>때 조각의 자리(효과 목록 번호) — 값 셈에서 덤 몫을 가른다.</summary>
            public readonly int At;
            /// <summary>때가 맞으면 더 얻는 HP 눈금 몫(처형 — 그 적 HP).</summary>
            public readonly double Gain;
            public T(string why, double now, double next, int at, double gain = 0) { Why = why; Now = now; Next = next; At = at; Gain = gain; }
            /// <summary>쥐는 쪽이 나은 때인가 — 다음 턴에 맞을 확률이 지금보다 크다.</summary>
            public bool Hold => Why != null && Next > Now;
        }

        /// <summary>그 카드(손의 id)의 때 — 때 조각이 없으면 Why null.</summary>
        public static T Of(Battle s, string id)
        {
            var c = s.CardOf(id);
            if (c?.Fx == null || c.Hero == null) return default;
            var fx = c.Fx;
            // 실드 소모 + 쥐고 넘기면 실드 유지 · 보존(이드 「나타로 들이받기」) — 이번 적 차례 공격이 헐고 남을 실드를 넘으면 쥘 때.
            // 덤(Gain) = 쥐어서 남긴 실드를 다음 턴에 쓸 몫(쓰는 비율 × 남기는 비율 × 지금 실드 × 그 피해 비율)
            int sp = fx.FindIndex(f => f.K == FxK.SpendShield);
            if (sp >= 0)
            {
                double keep = KeepShieldK(fx);
                if (keep > 0)
                {
                    var spf = fx[sp];
                    double frac = spf.All ? 1 : spf.Half || spf.Pick ? 0.5 : Math.Min(1, spf.V / Math.Max(1.0, s.PartyShield));
                    double ofEv = fx.Skip(sp + 1).Where(f => f.K == FxK.Dmg && f.OfEvent > 0).Select(f => f.OfEvent * (f.Target == "allEnemies" ? 1.6 : 1)).DefaultIfEmpty(0).Max();
                    int sh = s.PartyShield;
                    bool big = sh > 0 && s.FoeHitSum() > sh * (1 - frac);
                    return new T("shield", big ? 0 : 1, NEXT_SHIELD, sp, ofEv * frac * keep * sh);
                }
            }
            for (int i = 0; i < fx.Count; i++)
            {
                var f = fx[i];
                switch (f.K)
                {
                    case FxK.Taunt:
                        {
                            int atk = s.FoeHitSum(), sh = s.PartyShield;
                            return new T("taunt", atk > 0 && atk * 2 >= sh ? 1 : 0, NEXT_TAUNT, i);
                        }
                    case FxK.IfFoe when f.Id == "attack" || f.Id == "anyAttack":
                        {
                            bool on = f.Id == "anyAttack" ? s.AnyFoeAttack() : s.AliveEnemies().Any(e => !e.Sealed && (s.IntentHit(e) ?? 0) > 0);
                            if (f.Not) return new T("attack", on ? 0 : 1, 1 - NEXT_ATTACK, i);
                            return new T("attack", on ? 1 : 0, NEXT_ATTACK, i);
                        }
                    case FxK.IfFoe when !f.Not && (f.Id == "hp" || f.Id == "hpMob" || (f.Id == null && f.HpBelow > 0)):
                        {   // HP 선 아래 적이면 덤(티그 「영웅의 검」) — 선 아래 적이 있으면 지금 · 선 +30% 안이면 다음 턴
                            double pct = f.Id == null ? f.HpBelow : f.Pct > 0 ? f.Pct : 0.3;
                            double now = 0, next = 0;
                            foreach (var e in s.AliveEnemies())
                            {
                                if (e.MaxHp <= 0 || (f.Id == "hpMob" && e.Boss)) continue;
                                double r = (double)e.Hp / e.MaxHp;
                                if (r <= pct) now = 1; else if (r <= pct + EXEC_NEAR) next = NEXT_EXEC;
                            }
                            return new T("hp", now, Math.Max(now, next), i);
                        }
                    case FxK.IfCost when !f.Not:
                        {   // 비용이 줄어 n 이하가 되면 덤(리뉴아 「시간」 — 정각) — 지금 그 비용이면 지금, 다음 턴까지 아군이 낼 장수만큼 줄면 다음 턴
                            int now = s.CostOf(id), drop = CostDropNext(s, c);
                            // 덤(음수) = 쥐면 이번 턴 못 쓰고 남을 AP — 지금 남은 AP 가운데 이 카드가 쓸 몫(다른 카드에 다 쓴 판이면 0). 그래서 남는 AP 는 다른 카드에 먼저 쓰고,
                            // 남은 AP 를 버리느니 지금 비싸게 내는 쪽과 견준다(데이터 담당 측정 — 쥐기가 너무 세던 것)
                            double waste = Math.Min(s.Ap, now) * AP_WASTE * 1.2 * (s.HeroUnit(c.Hero)?.Atk ?? 12 * R.SCALE);
                            return new T("cheap", now <= f.Max ? 1 : 0, now - drop <= f.Max ? NEXT_NTH : 0, i, -waste / NEXT_NTH);
                        }
                    case FxK.IfNth when f.After && !f.Not:
                        {
                            bool on = s.NthNow >= f.N || s.NthNextOn(c.Hero);
                            return new T("nth", on ? 1 : Reach(s, id, f.N) ? NEXT_NTH : 0, NEXT_NTH, i);
                        }
                    case FxK.Execute:
                        {
                            double pct = f.Pct > 0 ? f.Pct : 0.1;
                            int need = 0;
                            for (int j = i - 1; j >= 0; j--) if (fx[j].K == FxK.IfFoe && fx[j].HitsBy != null) { need = Math.Max(1, fx[j].N); break; }
                            double now = 0, next = 0, gain = 0;
                            foreach (var e in s.AliveEnemies())
                            {
                                if (e.Boss || e.MaxHp <= 0) continue;
                                double r = (double)e.Hp / e.MaxHp;
                                int hb = s.HitsBy(e, c.Hero);
                                // 덤 = 그 적을 쓰러뜨린 몫(HP + 처치 덤 — 판 점수의 「쓰러진 적」 눈금)
                                if (r <= pct && hb >= need) { now = 1; gain = Math.Max(gain, e.Hp + 12 * R.SCALE); }
                                else if (r <= pct + EXEC_NEAR && hb + 2 >= need) { next = NEXT_EXEC; gain = Math.Max(gain, e.Hp + 12 * R.SCALE); }
                            }
                            return new T("execute", now, Math.Max(now, next), i, gain);
                        }
                }
            }
            return default;
        }

        /// <summary>다음 턴에 낼 때까지 줄 비용(어림) — 그 사도의 카드 값 키워드(grow allyPlay · cost) × 그때까지 아군이 낼 장수(이번 턴 남은 손 + 다음 턴 PLAYS_NEXT), 최대 값까지.</summary>
        public const int PLAYS_NEXT = 4;
        /// <summary>쥐어서 이번 턴 남길 AP 1 의 값(카드 한 장 눈금) — 쥐기 값에서 뺀다.</summary>
        public const double AP_WASTE = 0.6;
        public static int CostDropNext(Battle s, CardView c)
        {
            int drop = 0;
            foreach (var kw in s.Kw.Values)
            {
                var cv = kw.Def.CardValue;
                if (cv?.Grow != "allyPlay" || cv.Cost >= 0 || kw.Owner != c.Hero) continue;
                int plays = Math.Max(0, s.Hand.Count - 1) / 2 + PLAYS_NEXT;
                int room = cv.Max > 0 ? Math.Max(0, cv.Max - s.CardStOf(c.Id, kw.Id)) : plays;
                drop += Math.Min(plays, room) * -cv.Cost;
            }
            return drop;
        }

        /// <summary>쥐고 넘기면(handEnd) 남는 실드 비율 — 실드 유지 0.5 · 실드 보존 1 · 없으면 0.</summary>
        public static double KeepShieldK(List<Fx> fx)
        {
            bool on = false; double k = 0;
            foreach (var f in fx)
            {
                if (f.K == FxK.When) { on = f.On == "handEnd"; continue; }
                if (!on || f.K != FxK.Status) continue;
                if (f.Id == "실드 유지") k = Math.Max(k, R.SV("실드 유지"));
                else if (f.Id == "실드 보존") k = 1;
            }
            return k;
        }

        /// <summary>이번 턴에 그 자리(n번째)에 닿나 — 지금 낸 장수 + 그 카드 말고 지금 AP 로 낼 수 있는 카드 수(싼 차례로).</summary>
        static bool Reach(Battle s, string id, int n)
        {
            int ap = s.Ap, k = s.PlayedThisTurn;
            bool skipped = false;
            var costs = new List<int>();
            foreach (var h in s.Hand)
            {
                if (!skipped && h == id) { skipped = true; continue; }
                if (s.CanPlay(h) == null) costs.Add(s.CostOf(h));
            }
            costs.Sort();
            foreach (var cost in costs)
            {
                if (k + 1 >= n) return true;
                if (cost > ap) break;
                ap -= cost; k++;
            }
            return k + 1 >= n;
        }

        /// <summary>때 조각에 걸린 덤 몫을 뺀 효과(때가 아니면 나가는 모습) · 덤을 다 센 효과(때가 맞으면) — 값 셈용.</summary>
        public static (List<Fx> bas, List<Fx> full) Split(List<Fx> fx, T t)
        {
            if (fx == null || t.Why == null) return (fx, fx);
            var full = fx.Where((f, i) => i != t.At).ToList();
            if (t.Why == "taunt" || t.Why == "execute") return (full, fx);
            if (t.Why == "shield") return (fx, fx);   // 덤은 Gain(남긴 실드를 다음 턴에 쓸 몫)
            // 조건이면 그 조건부터 다음 조건 앞까지가 덤
            int end = t.At + 1;
            while (end < fx.Count && !FxK.Conditions.Contains(fx[end].K)) end++;
            var bas = fx.Where((f, i) => i < t.At || i >= end).ToList();
            return (bas, full);
        }
    }
}
