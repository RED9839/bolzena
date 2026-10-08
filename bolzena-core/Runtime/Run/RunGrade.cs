using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 학점제 학년(트릭컬 고유 — 2026-10-08 사용자, 설계 bolzena-content-v2/_measure/학년_시스템.md).
    /// 파티 전체가 함께 오른다(사도마다 따로가 아니다). 이긴 싸움마다 학점을 받고, 누적 학점이 진급표를 넘으면 학년이 오른다(1 → 6, 6 = 졸업).
    /// 숫자는 전부 여기 — 바꾸려면 이 표만 고친다.
    /// </summary>
    public static class Grades
    {
        public const int FIRST = 1, MAX = 6;
        /// <summary>이긴 싸움 종류 → 학점(Run.FightKind — fight · elite · boss · event · eventElite). 진 싸움은 0.</summary>
        public static readonly Dictionary<string, int> CREDIT = new() { ["fight"] = 1, ["elite"] = 3, ["boss"] = 5, ["event"] = 1, ["eventElite"] = 3 };
        /// <summary>진급 누적 학점 — NEED[g] 학점이면 g학년(0 · 1 칸은 비운다).</summary>
        public static readonly int[] NEED = { 0, 0, 3, 6, 9, 12, 16 };
        /// <summary>진급 한 번마다 파티 능력치 — 사도 기본 공격 · 방어 · 최대 HP 의 이 비율(누적, 6학년이면 ×5).</summary>
        public const double STAT_PCT = 0.02;
        /// <summary>이 학년부터 신탁 후보 +1(무작위 셋 → 넷).</summary>
        public const int ORACLE_AT = 3, ORACLE_PLUS = 1;
        /// <summary>이 학년부터 싸움을 열 때 고학년 게이지 +GAUGE_START%.</summary>
        public const int GAUGE_AT = 4, GAUGE_START = 10;
        /// <summary>이 학년이 되는 순간 다음 싸움에 카드 신탁 하나 확정(RunState.RewardFlash).</summary>
        public const int FLASH_AT = 5;
        /// <summary>졸업(6학년) — 진급 때 파티 HP 를 최대의 GRAD_HEAL 만큼 더 채우고, 싸움을 열 때 게이지 GRAD_GAUGE%(GAUGE_START 대신).</summary>
        public const int GRAD = 6, GRAD_GAUGE = 30;
        public const double GRAD_HEAL = 0.2;

        public static int CreditOf(string kind) => kind != null && CREDIT.TryGetValue(kind, out var c) ? c : 0;
        /// <summary>누적 학점 → 학년.</summary>
        public static int Of(int credits) { int g = FIRST; while (g < MAX && credits >= NEED[g + 1]) g++; return g; }
        /// <summary>g학년이 되는 학점(g ≤ 1 이면 0, MAX 보다 크면 MAX 의 것).</summary>
        public static int Need(int g) => g <= FIRST ? 0 : NEED[Math.Min(MAX, g)];
        /// <summary>학년 이름 — 「3학년」 · 6학년은 「졸업」.</summary>
        public static string Name(int g) => g >= GRAD ? "졸업" : $"{g}학년";

        /// <summary>그 학년이 되며 받는 것(진급 안내 · 화면 툴팁) — 한 줄씩.</summary>
        public static List<string> PerksAt(int g)
        {
            var o = new List<string>();
            if (g <= FIRST) return o;
            o.Add($"파티 공격 · 방어 · 최대 HP +{Math.Round(STAT_PCT * 100)}% (누적 +{Math.Round(STAT_PCT * 100 * (g - FIRST))}%)");
            if (g == ORACLE_AT) o.Add($"신탁 후보 +{ORACLE_PLUS} (셋 → 넷)");
            if (g == GAUGE_AT) o.Add($"전투 시작 고학년 게이지 +{GAUGE_START}%");
            if (g == FLASH_AT) o.Add("다음 전투에서 카드 신탁 하나 확정");
            if (g == GRAD) { o.Add($"졸업 — 파티 HP {Math.Round(GRAD_HEAL * 100)}% 회복"); o.Add($"전투 시작 고학년 게이지 +{GRAD_GAUGE}%"); }
            return o;
        }
    }

    public sealed partial class Run
    {
        /// <summary>지금 학년(1~6, 옛 저장은 1).</summary>
        public int Grade => Math.Max(Grades.FIRST, Math.Min(Grades.MAX, S.Grade));
        /// <summary>이 학년 막대 — (이 학년 시작 학점, 다음 학년 학점). 졸업이면 (졸업 학점, 졸업 학점).</summary>
        public (int from, int to) GradeSpan => (Grades.Need(Grade), Grades.Need(Math.Min(Grades.MAX, Grade + 1)));
        /// <summary>신탁 후보 수(R.ORACLE_PICKS, 학년 보상으로 +1).</summary>
        public int OraclePicks => R.ORACLE_PICKS + (Grade >= Grades.ORACLE_AT ? Grades.ORACLE_PLUS : 0) + (int)Perk("oraclePick");   // + 크레파스 보드

        /// <summary>진급 소식 — 화면이 아직 안 보인 새 학년들(차례대로). 꺼내면 비운다.</summary>
        public List<int> PopGradeNews()
        {
            var o = S.GradeNews.ToList();
            S.GradeNews.Clear();
            return o;
        }

        /// <summary>싸움을 열 때 사도 능력치 — 판 성장(RunState.Growth) + 학년 몫 + 교주 능력치(사도 기본 공격 · 방어 × 비율). RunState 는 건드리지 않는다.</summary>
        Dictionary<string, Stats> GradeGrowth()
        {
            double k = Grades.STAT_PCT * (Grade - Grades.FIRST);
            double ka = k + Perk("atk"), kd = k + Perk("def");   // 교주 능력치(크레파스 — RunCrayon.cs)
            int kc = (int)Math.Round(Perk("crit"));
            if (ka <= 0 && kd <= 0 && kc <= 0) return S.Growth;
            var o = new Dictionary<string, Stats>();
            foreach (var key in S.Party)
            {
                var h = Data.Hero(key);
                var g = S.Growth.TryGetValue(key, out var g0) ? g0 + new Stats() : new Stats();
                if (h != null) g = g + new Stats { Atk = Num.Round(h.Atk * ka), Def = Num.Round(h.Def * kd), Crit = kc };
                o[key] = g;
            }
            foreach (var kv in S.Growth) if (!o.ContainsKey(kv.Key)) o[kv.Key] = kv.Value;
            return o;
        }

        /// <summary>싸움을 열 때 고학년 게이지 — 남은 게이지 + 학년 몫(4학년부터, 졸업은 더).</summary>
        int GradeGauge()
        {
            int add = Grade >= Grades.GRAD ? Grades.GRAD_GAUGE : Grade >= Grades.GAUGE_AT ? Grades.GAUGE_START : 0;
            return Math.Min(R.GAUGE_MAX, S.Gauge + add);
        }

        /// <summary>이긴 싸움의 학점을 더하고, 진급하면 그 학년의 몫을 바로 준다(AfterFight 끝에서).</summary>
        void GainCredits(Battle b)
        {
            S.LastCredit = 0;
            if (b.Over != "win") return;
            string kind = FightKind == "event" && S.EventFight.Elite ? "eventElite" : FightKind;
            int c = Grades.CreditOf(kind);
            if (c <= 0) return;
            S.LastCredit = c;
            S.Credits += c;
            int from = Grade, to = Grades.Of(S.Credits);
            for (int g = from + 1; g <= to; g++) Promote(g);
        }

        /// <summary>g학년이 된다 — 최대 HP(사도 기본 HP 합 × 비율) · 신탁 확정 · 졸업 회복.</summary>
        void Promote(int g)
        {
            S.Grade = g;
            S.GradeNews.Add(g);
            int baseHp = S.Party.Sum(k => Data.Hero(k)?.Hp ?? 0);
            ShiftHp(Num.Round(baseHp * Grades.STAT_PCT));
            if (g == Grades.FLASH_AT) S.RewardFlash = true;
            if (g == Grades.GRAD) S.PartyHp = Math.Min(S.PartyMaxHp, S.PartyHp + Num.Round(S.PartyMaxHp * Grades.GRAD_HEAL));
        }
    }
}
