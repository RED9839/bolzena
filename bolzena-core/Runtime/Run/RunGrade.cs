using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 학점제 학년(트릭컬 고유 — 2026-10-08 사용자, 설계 bolzena-content-v2/_measure/학년_시스템.md).
    /// 파티 전체가 함께 오른다(사도마다 따로가 아니다). 이긴 싸움마다 학점을 받고, 누적 학점이 진급표를 넘으면 학년이 오른다(1 → 6, 6 = 졸업).
    /// 진급(2~5학년)마다 보상 셋 가운데 하나를 고른다(2026-10-09 사용자 — 「능력치 +% 는 체감이 안 된다」). 학년이 오를수록 센 것이 나온다.
    /// 졸업(6학년)은 고르지 않고 졸업 선물(회복 · 은총 · 전투 시작 게이지). 숫자는 전부 여기 — 바꾸려면 이 표만 고친다.
    /// </summary>
    public static class Grades
    {
        public const int FIRST = 1, MAX = 6;
        /// <summary>이긴 싸움 종류 → 학점(Run.FightKind — fight · elite · boss · event · eventElite). 진 싸움은 0.</summary>
        public static readonly Dictionary<string, int> CREDIT = new() { ["fight"] = 1, ["elite"] = 3, ["boss"] = 5, ["event"] = 1, ["eventElite"] = 3 };
        /// <summary>진급 누적 학점 — NEED[g] 학점이면 g학년(0 · 1 칸은 비운다).</summary>
        public static readonly int[] NEED = { 0, 0, 3, 6, 9, 12, 16 };
        /// <summary>진급 한 번마다 파티 능력치 — 사도 기본 공격 · 방어 · 최대 HP 의 이 비율(누적). 고르는 보상이 주인공이라 아주 작게.</summary>
        public const double STAT_PCT = 0.01;
        /// <summary>진급 보상 후보 수.</summary>
        public const int OFFER_N = 3;
        /// <summary>졸업(6학년) 선물 — 파티 HP 를 최대의 GRAD_HEAL 만큼 채우고, 은총(고유 카드 1장), 그 뒤 싸움을 열 때 게이지 +GRAD_GAUGE%.</summary>
        public const int GRAD = 6, GRAD_GAUGE = 30;
        public const double GRAD_HEAL = 0.3;

        /// <summary>
        /// 진급 보상 종류 — gold 골드 · heal 파티 HP 비율 회복 · gauge 고학년 게이지 · flash 신탁 1회(무작위 카드 하나의 신탁 셋 중 하나)
        /// · grace 은총(파티 사도의 아직 없는 고유 카드 1장) · remove 카드 1장 빼기 · equip 장비 1개 · neutral 교주 카드 1장 · prep 예습 노트(다음 전투 PREP_FIGHTS 번 첫 손패 +V) · mock 전술 교본(다음 엘리트 · 보스 전투 MOCK_FIGHTS 번 시작 사기 +V).
        /// (옛 crayon 「크레파스 상자」 는 판 밖 재화라 이번 판에 도움이 안 돼 2026-10-09 사용자 결정으로 prep · mock 로 바꿨다.)
        /// 무게 · 값 · 등급은 학년 g 에 따라(WeightOf · ValueOf · GradeOf). 무게 0 이면 그 학년에 안 나온다.
        /// </summary>
        public static readonly string[] KINDS = { "gold", "heal", "gauge", "flash", "grace", "remove", "equip", "neutral", "prep", "mock" };
        /// <summary>예습 노트 · 전술 교본이 걸리는 전투 수.</summary>
        public const int PREP_FIGHTS = 3, MOCK_FIGHTS = 1;
        public static double WeightOf(string kind, int g) => kind switch
        {
            "gold" => 10, "heal" => 10, "gauge" => 7, "flash" => 8 + 2 * g, "grace" => 4 + 3 * g, "remove" => 8, "equip" => 3 + 2 * g, "neutral" => 6, "prep" => 6, "mock" => 4 + g, _ => 0,
        };
        public static double ValueOf(string kind, int g) => kind switch
        {
            "gold" => 25 + 12 * g, "heal" => 0.12 + 0.03 * g, "gauge" => 30 + 10 * g, "prep" => g >= 4 ? 2 : 1, "mock" => g >= 4 ? 2 : 1, _ => 1,
        };
        public static string GradeOf(string kind, int g) => kind == "equip" ? (g <= 3 ? "고급" : "희귀") : null;

        /// <summary>
        /// 보상 글 — 이름 · 설명. 이름은 원작 트릭컬 리바이브 용어(2026-10-09 사용자 — 근거는 학년_시스템.md §0 「이름의 근거」):
        /// 골드(원작 재화) · 딸기맛 캡슐 · 소다맛 캡슐 · 전술 교본(원작 스펠 — 효과의 뜻이 같다) · 아티팩트 · 스펠. 원작 대응이 없는 것은 우리 이름 그대로.
        /// </summary>
        public static (string name, string desc) Describe(GradeChoice c) => c.Kind switch
        {
            "gold" => ("골드", $"골드 +{c.V:0}"),
            "heal" => ("딸기맛 캡슐", $"스펠 효과 1회 — 모든 사도의 HP {c.V * 100:0}% 회복"),
            "gauge" => ("소다맛 캡슐", $"스펠 효과 1회 — 고학년 게이지 +{c.V:0}%"),
            "flash" => ("특별 수업", "카드 하나에 신탁 — 신탁 셋 가운데 하나를 고릅니다"),
            "grace" => ("사도의 선물", "은총 — 파티 사도의 고유 카드 1장"),
            "remove" => ("정리 정돈", "덱에서 카드 1장을 뺍니다"),
            "equip" => ("아티팩트", $"{c.Grade} 아티팩트 1개"),
            "neutral" => ("스펠", "스펠(교주 카드) 1장 — 무작위"),
            "prep" => ("예습 노트", $"다음 전투 {PREP_FIGHTS}번 — 첫 손패 +{c.V:0}장"),
            "mock" => ("전술 교본", $"스펠 효과 1회 — 다음 엘리트 · 보스전 시작 시 사기 {c.V:0}"),
            _ => (c.Kind, ""),
        };

        public static int CreditOf(string kind) => kind != null && CREDIT.TryGetValue(kind, out var c) ? c : 0;
        /// <summary>누적 학점 → 학년.</summary>
        public static int Of(int credits) { int g = FIRST; while (g < MAX && credits >= NEED[g + 1]) g++; return g; }
        /// <summary>g학년이 되는 학점(g ≤ 1 이면 0, MAX 보다 크면 MAX 의 것).</summary>
        public static int Need(int g) => g <= FIRST ? 0 : NEED[Math.Min(MAX, g)];
        /// <summary>학년 이름 — 「3학년」 · 6학년은 「졸업」.</summary>
        public static string Name(int g) => g >= GRAD ? "졸업" : $"{g}학년";

        /// <summary>그 학년이 되며 받는 것(학년 표 · 툴팁) — 한 줄씩.</summary>
        public static List<string> PerksAt(int g)
        {
            var o = new List<string>();
            if (g <= FIRST) return o;
            if (g < GRAD) o.Add("진급 보상 셋 가운데 하나를 고릅니다" + (g >= 4 ? " — 은총 · 신탁 · 아티팩트가 더 자주" : ""));
            else { o.Add($"졸업 선물 — 파티 HP {GRAD_HEAL * 100:0}% 회복 · 은총 1장"); o.Add($"그 뒤 전투 시작 고학년 게이지 +{GRAD_GAUGE}%"); }
            o.Add($"파티 공격 · 방어 · 최대 HP +{STAT_PCT * 100:0}% (누적 +{STAT_PCT * 100 * (g - FIRST):0}%)");
            return o;
        }
    }

    /// <summary>진급 보상 후보 하나.</summary>
    public sealed class GradeChoice
    {
        public string Kind;
        public double V;
        public string Grade;
    }

    /// <summary>한 번의 진급에 뜬 후보 셋(그 학년).</summary>
    public sealed class GradeOffer
    {
        public int Grade;
        public List<GradeChoice> Choices = new();
    }

    /// <summary>보상을 고른 뒤 — 화면 · 봇이 이어서 할 것(받은 카드 보이기 · 신탁 고르기 · 뺄 카드 고르기 · 장비 정하기).</summary>
    public sealed class GradeTake
    {
        public GradeChoice Choice;
        /// <summary>grace · neutral — 받은 카드 id(교주 카드는 주인 고르기 줄에 섰다).</summary>
        public string Card;
        /// <summary>flash — 신탁 고르기(Run.TakeFlash 로 받는다).</summary>
        public FlashOffer Flash;
        /// <summary>remove — 뺄 카드를 고른다(Run.GradeRemove).</summary>
        public bool Remove;
        /// <summary>equip — 받은 장비(Bag 에 섰다 — 끼기 · 팔기).</summary>
        public string Equip;
        /// <summary>대신 받은 것(후보를 못 받게 됐을 때 — 골드).</summary>
        public string Note;
    }

    public sealed partial class Run
    {
        /// <summary>지금 학년(1~6, 옛 저장은 1).</summary>
        public int Grade => Math.Max(Grades.FIRST, Math.Min(Grades.MAX, S.Grade));
        /// <summary>이 학년 막대 — (이 학년 시작 학점, 다음 학년 학점). 졸업이면 (졸업 학점, 졸업 학점).</summary>
        public (int from, int to) GradeSpan => (Grades.Need(Grade), Grades.Need(Math.Min(Grades.MAX, Grade + 1)));
        /// <summary>신탁 후보 수(R.ORACLE_PICKS + 크레파스 보드).</summary>
        public int OraclePicks => R.ORACLE_PICKS + (int)Perk("oraclePick");

        /// <summary>진급 소식 — 화면이 아직 안 보인 새 학년들(차례대로). 꺼내면 비운다.</summary>
        public List<int> PopGradeNews()
        {
            var o = S.GradeNews.ToList();
            S.GradeNews.Clear();
            return o;
        }

        /// <summary>아직 안 고른 진급 보상(차례대로 — 첫 것부터). 없으면 null.</summary>
        public GradeOffer GradeOfferNow => S.GradeOffers.FirstOrDefault();

        /// <summary>싸움을 열 때 사도 능력치 — 판 성장(RunState.Growth) + 학년 몫 + 교주 보드(사도 기본 공격 · 방어 × 비율). RunState 는 건드리지 않는다.</summary>
        Dictionary<string, Stats> GradeGrowth()
        {
            double k = Grades.STAT_PCT * (Grade - Grades.FIRST);
            double ka = k + Perk("atk"), kd = k + Perk("def");   // 교주 보드(크레파스 — RunCrayon.cs)
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

        /// <summary>싸움을 열 때 고학년 게이지 — 남은 게이지 + 졸업 몫.</summary>
        int GradeGauge() => Math.Min(R.GAUGE_MAX, S.Gauge + (Grade >= Grades.GRAD ? Grades.GRAD_GAUGE : 0));

        /// <summary>이긴 싸움의 학점을 더하고, 진급하면 그 학년의 몫을 준다(AfterFight 끝에서).</summary>
        void GainCredits(Battle b)
        {
            S.LastCredit = 0;
            if (!R.GRADE_ON || b.Over != "win") return;   // 학년 꺼짐(2026-10-09) — 학점 · 진급 없음
            string kind = FightKind == "event" && S.EventFight.Elite ? "eventElite" : FightKind;
            int c = Grades.CreditOf(kind);
            if (c <= 0) return;
            S.LastCredit = c;
            S.Credits += c;
            int from = Grade, to = Grades.Of(S.Credits);
            for (int g = from + 1; g <= to; g++) Promote(g);
        }

        /// <summary>g학년이 된다 — 최대 HP(사도 기본 HP 합 × 비율), 2~5학년은 보상 후보 셋, 졸업은 졸업 선물.</summary>
        void Promote(int g)
        {
            S.Grade = g;
            S.GradeNews.Add(g);
            int baseHp = S.Party.Sum(k => Data.Hero(k)?.Hp ?? 0);
            ShiftHp(Num.Round(baseHp * Grades.STAT_PCT));
            if (g < Grades.GRAD) { S.GradeOffers.Add(RollGradeOffer(g)); return; }
            // 졸업 선물 — 회복 · 은총(남은 고유 카드가 있는 사도에게서 1장, 없으면 골드)
            S.PartyHp = Math.Min(S.PartyMaxHp, S.PartyHp + Num.Round(S.PartyMaxHp * Grades.GRAD_HEAL));
            S.GradeGift = GraceOne();
            if (S.GradeGift == null) S.Gold += GRACE_GOLD;
        }

        /// <summary>파티 사도 하나의 아직 없는 고유 카드 1장을 덱에(은총). 못 하면 null.</summary>
        string GraceOne()
        {
            if (MindBroken) return null;
            var hs = S.Party.Where(k => UniquesLeft(k).Count > 0).ToList();
            if (hs.Count == 0) return null;
            var k = hs[RndInt(hs.Count)];
            var left = UniquesLeft(k);
            var id = left[RndInt(left.Count)];
            GainCard(id);
            return id;
        }

        /// <summary>그 학년의 보상 후보 셋 — 무게(학년마다)로 서로 다른 종류를 뽑는다. 못 받을 종류(남은 고유 카드 없음 · 신탁 붙일 카드 없음 · 덱이 얇음)는 뺀다.</summary>
        GradeOffer RollGradeOffer(int g)
        {
            var kinds = Grades.KINDS.Where(k => Grades.WeightOf(k, g) > 0).ToList();
            if (!S.Party.Any(k => UniquesLeft(k).Count > 0)) kinds.Remove("grace");
            if (FlashTargets().Count == 0) kinds.Remove("flash");
            if (S.Deck.Count <= 6) kinds.Remove("remove");
            var o = new GradeOffer { Grade = g };
            while (o.Choices.Count < Grades.OFFER_N && kinds.Count > 0)
            {
                double tot = kinds.Sum(k => Grades.WeightOf(k, g)), x = Rnd() * tot;
                int i = 0;
                while (i < kinds.Count - 1 && (x -= Grades.WeightOf(kinds[i], g)) >= 0) i++;
                var kind = kinds[i]; kinds.RemoveAt(i);
                o.Choices.Add(new GradeChoice { Kind = kind, V = Grades.ValueOf(kind, g), Grade = Grades.GradeOf(kind, g) });
            }
            return o;
        }

        /// <summary>
        /// 진급 보상을 고른다(GradeOfferNow 의 i 번째) — 바로 되는 것은 여기서, 고를 것(신탁 · 뺄 카드 · 장비 정하기)은 돌려준 GradeTake 로 화면 · 봇이 잇는다.
        /// 고를 것이 없으면 null.
        /// </summary>
        public GradeTake TakeGrade(int i)
        {
            var off = GradeOfferNow;
            if (off == null || i < 0 || i >= off.Choices.Count) return null;
            S.GradeOffers.RemoveAt(0);
            var c = off.Choices[i];
            var t = new GradeTake { Choice = c };
            var rec = Pick("grade", null, off.Choices.Select(x => x.Kind), c.Kind);
            switch (c.Kind)
            {
                case "gold": S.Gold += (int)c.V; break;
                case "heal": S.PartyHp = Math.Min(S.PartyMaxHp, S.PartyHp + Num.Round(S.PartyMaxHp * c.V)); break;
                case "gauge": S.Gauge = Math.Min(R.GAUGE_MAX, S.Gauge + (int)c.V); break;
                case "flash":
                    t.Flash = MindBroken ? null : OfferFlash();
                    if (t.Flash == null) { S.Gold += GRACE_GOLD; t.Note = $"신탁을 붙일 카드가 없습니다 — 대신 골드 +{GRACE_GOLD}"; }
                    break;
                case "grace":
                    t.Card = GraceOne();
                    if (t.Card == null) { S.Gold += GRACE_GOLD; t.Note = $"받을 고유 카드가 없습니다 — 대신 골드 +{GRACE_GOLD}"; }
                    break;
                case "remove":
                    t.Remove = !MindBroken && S.Deck.Count > 1;
                    if (!t.Remove) { S.Gold += GRACE_GOLD; t.Note = $"뺄 수 없습니다 — 대신 골드 +{GRACE_GOLD}"; }
                    break;
                case "equip":
                    {
                        var ids = OfferEquip(new Dictionary<string, int> { [c.Grade ?? "고급"] = 1 }, 1);
                        if (ids.Count > 0) { GainEquip(ids[0]); t.Equip = ids[0]; }
                        else { S.Gold += GRACE_GOLD; t.Note = $"아티팩트가 다 나왔습니다 — 대신 골드 +{GRACE_GOLD}"; }
                        break;
                    }
                case "neutral":
                    {
                        var pool = MindBroken ? new List<string>() : NeutralOffer(c.Grade, 1);
                        if (pool.Count > 0) { GainCard(pool[0]); t.Card = pool[0]; }
                        else { S.Gold += GRACE_GOLD; t.Note = $"받을 교주 카드가 없습니다 — 대신 골드 +{GRACE_GOLD}"; }
                        break;
                    }
                case "prep": S.PrepLeft = Grades.PREP_FIGHTS; S.PrepHand = Math.Max(S.PrepHand, (int)c.V); break;
                case "mock": S.MockLeft = Grades.MOCK_FIGHTS; S.MockMorale = Math.Max(S.MockMorale, (int)c.V); break;
            }
            rec.Card = t.Card ?? t.Equip;
            if (t.Flash != null) Pick("oracle", GameData.NoInst(t.Flash.CardId), t.Flash.Picks.Select(x => x.ToString()), null);   // 고르면 TakeFlash 가 채운다
            return t;
        }

        /// <summary>
        /// 싸움을 열 때 진급 보상 몫을 이번 싸움의 「다음 전투」 에 더하고 횟수를 하나 쓴다 — 예습 노트(첫 손패) · 전술 교본(엘리트 · 보스만, 사기).
        /// 이벤트 「다음 전투」 와 합친다(NextFight.Merge).
        /// </summary>
        NextFight GradeNext(NextFight next)
        {
            if (S.PrepLeft > 0 && S.PrepHand > 0)
            {
                next = (next ?? new NextFight()).Merge(new NextFight { Hand = S.PrepHand });
                if (--S.PrepLeft <= 0) S.PrepHand = 0;
            }
            bool big = S.EventFight == null && (IsBoss || S.Elite) || (S.EventFight?.Elite ?? false);
            if (big && S.MockLeft > 0 && S.MockMorale > 0)
            {
                next = (next ?? new NextFight()).Merge(new NextFight { Buff = new Dictionary<string, int> { ["사기"] = S.MockMorale } });
                if (--S.MockLeft <= 0) S.MockMorale = 0;
            }
            return next;
        }

        /// <summary>진급 보상 「카드 1장 빼기」 — 상점 값 없이 덱에서 뺀다. 못 빼면 까닭.</summary>
        public string GradeRemove(string cardId)
        {
            if (MindBroken) return MIND;
            int i = cardId != null ? S.Deck.IndexOf(cardId) : -1;
            if (i < 0) return "덱에 없는 카드입니다";
            S.Deck.RemoveAt(i);
            if (!S.Deck.Contains(cardId)) ForgetCard(cardId);
            Pick("gradeRemove", null, null, cardId);
            return null;
        }
    }
}
