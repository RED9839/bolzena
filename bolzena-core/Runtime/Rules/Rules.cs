using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 전투 · 판 규칙의 수치 한 곳(웹판 js/rules.js · docs/18). 엔진 · 봇 · 글 생성기가 모두 여기서 읽는다.
    /// </summary>
    public static class R
    {
        // ── AP · 손 ─────────────────────────────────────────────────
        public const int AP_PER_TURN = 3;
        public const int DRAW_PER_TURN = 5;
        public const int HAND_MAX = 10;

        // ── 눈금 · 적 세기 ───────────────────────────────────────────
        public const double ENEMY_HP = 0.85;
        public const int SCALE = 10;
        public static readonly Dictionary<int, double> FLOOR_HP = new() { [1] = 2.0, [2] = 3.25 };
        public static readonly Dictionary<int, double> BOSS_HP = new() { [1] = 0.7, [2] = 0.6 };
        public static readonly Dictionary<int, double> FLOOR_DMG = new() { [1] = 2.4, [2] = 3.8 };
        public const double ELITE_HP = 1.5;

        /// <summary>층(0부터) · 보스 · 엘리트 → 적 체력 배율 · 피해 배율.</summary>
        public static (double hp, double dmg) FoeScale(int floor, bool boss = false, bool elite = false)
        {
            int k = floor + 1;
            double hp = ENEMY_HP * (FLOOR_HP.TryGetValue(k, out var a) ? a : 1)
                * (boss ? (BOSS_HP.TryGetValue(k, out var b) ? b : 1) : 1) * (elite ? ELITE_HP : 1);
            return (hp, FLOOR_DMG.TryGetValue(k, out var d) ? d : 1);
        }

        // ── 즉시 행동 ────────────────────────────────────────────────
        public const int ENEMY_RUSH_SMALL = 3;
        public const int ENEMY_RUSH_MIN = 3;

        // ── 신탁(전투 중) · 축복 ────────────────────────────────────
        public static readonly Dictionary<string, double> EPI_HERO = new() { ["fight"] = 0.4, ["elite"] = 0.7, ["boss"] = 0.8, ["event"] = 0.3 };
        public static readonly Dictionary<string, double> EPI_CARD = new() { ["fight"] = 0.3, ["elite"] = 0.5, ["boss"] = 0.4, ["event"] = 0.15 };
        public static readonly string[] EPI_SURE_HERO = { "fight", "elite", "boss" };
        public static readonly string[] EPI_SURE_CARD = { "elite" };
        /// <summary>
        /// 신탁 규칙(2026-10-05 사용자 통일) — 어디서 신탁이 나든(전투 중 번뜩임 · 이벤트 · 캠프 수련 · 보상) 그 카드의 신탁 ①~⑤ 가운데 무작위 셋을 보여 주고 하나를 고른다.
        /// 셋 가운데 하나에 ORACLE_BLESS 확률로 축복(그 카드의 축복 셋 가운데 하나, 없으면 공용 풀)이 얹혀 보인다.
        /// 15% 근거: 전투당 신탁이 0.3~0.5번 꼴이라 판(싸움 열 남짓)에 축복 0.5~1장 — 옛 20% 보다 조금 드물게, 「발생할 수도 있음」 의 감.
        /// </summary>
        public const int ORACLE_PICKS = 3;
        public const double ORACLE_BLESS = 0.15;
        public const double DIVINE = ORACLE_BLESS;
        /// <summary>공용 축복 풀 — 카드 종류마다. (웹판의 thorn 「중독」 은 중독을 뺐으므로 없앴다)</summary>
        public static readonly Dictionary<string, string[]> DIVINE_KINDS = new()
        {
            ["공격"] = new[] { "power", "cost", "weakSpot", "frost" },
            ["스킬"] = new[] { "ap", "draw", "cost", "heal", "guard" },
            ["강화"] = new[] { "atkUp", "defUp", "cost" },
        };
        public static readonly Dictionary<string, string> DIVINE_KO = new()
        {
            ["power"] = "불타는 웅변 — 피해 ×1.3", ["cost"] = "가벼운 발걸음 — 코스트 -1", ["weakSpot"] = "약점 공략 — 취약인 적에게 피해 ×1.3",
            ["frost"] = "눈보라 예보 — 맞은 적 취약 1", ["ap"] = "발맞추기 — 내면 AP +1", ["draw"] = "끝없는 이야기 — 내면 드로우 1",
            ["heal"] = "괜찮아 — 회복 ×1.3", ["guard"] = "양보하는 마음 — 방어 · 실드 ×1.3",
            ["atkUp"] = "한 땀 한 땀 — 내면 이번 전투 이 사도 공격력 +10%", ["defUp"] = "꺾이지 않는 실 — 내면 이번 전투 이 사도 방어력 +10%",
        };
        public const string DIVINE_NAME = "겨우살이의 축복";
        public const double SHIN = 1.3;

        // ── 골드 · 상점 · 장비 ───────────────────────────────────────
        public const int GOLD_START = 99;
        public static readonly int[] GOLD_FIGHT = { 20, 30 };
        public const int GOLD_FLOOR = 10;
        public const int GOLD_BOSS = 95;
        public const double ELITE_GOLD = 1.5;
        public const int SHOP_NEUTRAL = 3;
        public const int SHOP_EQUIP_N = 3;
        public const int SHOP_REROLL = 25, SHOP_REROLL_STEP = 25;
        public const int PRICE_REMOVE = 80, PRICE_REMOVE_STEP = 20;
        public static readonly Dictionary<string, int> SHOP_GRADE_WEIGHT = new() { ["일반"] = 5, ["고급"] = 4, ["희귀"] = 2, ["전설"] = 1 };
        public static readonly string[] SLOTS = { "무기", "방어구", "장신구" };
        public static readonly string[] GRADES = { "일반", "고급", "희귀", "전설" };
        public static readonly Dictionary<string, int> EQUIP_PRICE = new() { ["일반"] = 90, ["고급"] = 130, ["희귀"] = 180, ["전설"] = 250 };
        public const double EQUIP_SELL = 0.4;
        public static readonly Dictionary<string, int> SHOP_EQUIP = new() { ["일반"] = 3, ["고급"] = 3, ["희귀"] = 2, ["전설"] = 1 };
        public static readonly Dictionary<string, int>[] ELITE_EQUIP = { new() { ["고급"] = 1 }, new() { ["희귀"] = 3, ["전설"] = 1 } };
        public static readonly Dictionary<string, int>[] BOSS_EQUIP = { new() { ["전설"] = 1 }, new() { ["전설"] = 1 } };
        public static readonly Dictionary<string, int>[] FIGHT_EQUIP = { new() { ["일반"] = 2, ["고급"] = 1 }, new() { ["고급"] = 2, ["희귀"] = 3 } };
        public const double DROP_FIGHT = 0.5;   // 일반 싸움 장비 확률(엘리트 · 보스는 늘)

        // ── 캠프 · 층 사이 ───────────────────────────────────────────
        public const double CAMP_HEAL = 0.3;
        public const int FLOOR_REST = 10;     // 사도 한 명 몫 — 파티에 × 사도 수

        // ── 이벤트 ───────────────────────────────────────────────────
        public const double EVENT_FLOOR_SHARE = 0.7;
        public const int EVENT_REMOVE_WEIGHT = 5;

        // ── 파티 · 적의 수 ───────────────────────────────────────────
        public const double FOE_ALL_X = 2;
        /// <summary>
        /// 개인(사도) 층 상태 — 뜻이 「이 전투원」 을 짚는 것만. 나머지 이로운 · 해로운 효과는 전부 파티 층 하나에 걸리고 파티원 전원에게 든다(카제나 그대로).
        /// 2026-10-05 사용자 정정: 사기도 파티 층(사기 1 → 셋 모두 피해 +20%). 사도 고유 효과(키워드 carrier self)는 따로 사도마다.
        /// </summary>
        public static readonly HashSet<string> HERO_ST = new() { "구속" };
        /// <summary>그 상태의 층 — "hero"(사도마다) · "party"(파티 몸 하나) · "card"(카드에 붙는다). 적은 늘 제 몸.</summary>
        public static string LayerOf(string id) => HERO_ST.Contains(id) ? "hero" : Array.IndexOf(CARD_ST, id) >= 0 ? "card" : "party";
        public static bool IsHeroSt(string id) => HERO_ST.Contains(id);

        // ── 고학년 게이지 ────────────────────────────────────────────
        public const int GAUGE_MAX = 300;
        public const int GAUGE_PER_AP = 10;

        public const double CRIT_MULT = 1.5;

        // ── 상태(docs/18 §5) ─────────────────────────────────────────
        public static readonly Dictionary<string, double> STATUS_V = new()
        {
            ["취약"] = 0.5, ["약화"] = 0.25, ["손상"] = 0.5, ["피해 감소"] = 0.15,
            ["반격"] = 1.5, ["반격Full"] = 3.0, ["반격Max"] = 10, ["표식"] = 1.0,
            ["사기"] = 0.2, ["불굴"] = 0.2, ["불굴Cap"] = 0.8, ["결의"] = 20, ["결정화"] = 0.2,
            ["고동"] = 0.7,
            ["사기Max"] = 10, ["불굴Max"] = 10, ["결의Max"] = 10, ["결정화Max"] = 10, ["고동Max"] = 10,
            ["고통"] = 0.5, ["고통Max"] = 20, ["균열"] = 0.4, ["균열Max"] = 30,
            ["잔불"] = 0.3, ["잔불Max"] = 5, ["잔광"] = 0.5,
            ["실드 유지"] = 0.5, ["협공"] = 1.0, ["충격"] = 0.8, ["충격Shield"] = 0.5, ["충격파"] = 3.0, ["그을림"] = 0.8, ["그을림Max"] = 10,
            ["분쇄"] = 0.2,
            ["리듬Max"] = 10,
            // 2026-10-05 키워드 사전(Docs/키워드.md) — 카제나 효과 사전 값
            ["공명"] = 0.8, ["탄성"] = 0.5, ["탄성Max"] = 5, ["칼날 벼리기"] = 0.2, ["칼날 벼리기Max"] = 3, ["형상 강화"] = 1.0, ["형상 강화Max"] = 3,
            ["급속Max"] = 9, ["둔화Max"] = 9, ["초재생"] = 3.0, ["집중"] = 0.5, ["탐구심"] = 0.2, ["고통 각인"] = 2, ["응징"] = 2.0, ["포자증식"] = 0.1,
            ["죽음의 낙인"] = 0.8, ["행동 둔화"] = 0.2, ["독"] = 1.0, ["부상"] = 0.3, ["흡수Max"] = 0.2, ["축복"] = 0.6, ["축복At"] = 0.3,
        };
        public static double SV(string id) => STATUS_V.TryGetValue(id, out var v) ? v : 0;
        public static double? SMax(string id) => STATUS_V.TryGetValue(id + "Max", out var v) ? v : (double?)null;

        public const string RHYTHM = "리듬";
        public const string STUN = "기절";

        /// <summary>횟수형 — 한 번의 일에 1 준다(회피는 한 대에 1, 공명은 버린 카드 한 장에 1).</summary>
        public static readonly string[] CHARGE_ST = { "취약", "약화", "손상", "피해 감소", "반격", "표식", "잔광", "면역", "실드 유지", "저장", "협공", "충격", "충격파", "그을림",
            "회피", "공명", "실드 보존", "행동 둔화", "응징", "미끄러움" };
        /// <summary>세기형 — 겹마다 세지고 줄지 않는다(탄성 · 포자증식은 발동하면 모두 사라진다).</summary>
        public static readonly string[] INTENSITY_ST = { "사기", "불굴", "결의", "결정화", "고동", "칼날 벼리기", "형상 강화", "탄성", "포자증식", "고통 각인", "근면", "계몽", "집중" };
        public static readonly string[] DOT_ST = { "고통", "균열" };
        /// <summary>1턴 — 다음 내 턴이 시작되면 사라진다(적의 차례까지 간다).</summary>
        public static readonly string[] TURN_ST = { "칼날 벼리기", "빙벽", "절대 무적", "열정 약점", "형상 강화" };
        /// <summary>턴 수 — 내 턴이 시작될 때마다 1 준다.</summary>
        public static readonly string[] TICK_ST = { "구속", "초재생", "정신 붕괴" };
        /// <summary>그 밖 — 다음 턴 드로우(다음 턴 시작에 겹만큼 뽑고 사라진다) · 죽음의 낙인(적) · 급속 · 둔화(적의 행동 카운트, 그 적이 행동하면 사라진다).</summary>
        public static readonly string[] OTHER_ST = { "다음 턴 드로우", "죽음의 낙인", "급속", "둔화", "끈기" };
        /// <summary>카드에 붙는 상태 — 상태 이름이 아니라 카드 id 에 붙는다(cardStatus 조각 · 적의 수 cardDebuff).</summary>
        public static readonly string[] CARD_ST = { "독", "봉쇄", "침체", "빙결", "탐구심" };
        public static readonly string[] UNIT_ST = { "고통", "균열", "고동", "그을림", "충격", "충격파", "응징" };
        /// <summary>해로운 상태 — 「디버프 해제」 의 차례 · 면역이 막는 것.</summary>
        public static readonly string[] BAD_ST = { "취약", "약화", "손상", "고통", "균열", "그을림", "충격", "충격파", "잔불",
            "고통 각인", "응징", "포자증식", "죽음의 낙인", "열정 약점", "정신 붕괴", "둔화", "미끄러움" };
        /// <summary>적에게 걸려도 「디버프를 걸면」 이 아닌 좋은 상태.</summary>
        public static readonly HashSet<string> BUFF_ST = new() { "사기", "불굴", "결의", "반격", "결정화", "잔광", "피해 감소", "면역", "실드 유지", "저장", "협공", "고동",
            "회피", "공명", "탄성", "칼날 벼리기", "빙벽", "구속", "형상 강화", "행동 둔화", "급속", "근면", "계몽", "집중", "다음 턴 드로우", "초재생", "절대 무적", "실드 보존", "끈기" };
        /// <summary>엔진이 아는 상태 전부(검사기가 본다) — 카드에 붙는 상태(CARD_ST)는 빼고.</summary>
        public static readonly HashSet<string> ALL_ST = new(CHARGE_ST.Concat(INTENSITY_ST).Concat(DOT_ST).Concat(TURN_ST).Concat(TICK_ST).Concat(OTHER_ST).Concat(new[] { "잔불", RHYTHM, STUN }));
        public static bool IsTurnSt(string id) => Array.IndexOf(TURN_ST, id) >= 0;
        public static bool IsCardSt(string id) => Array.IndexOf(CARD_ST, id) >= 0;

        static readonly HashSet<string> INT_SET = new(INTENSITY_ST);
        public static bool IsIntensity(string id) => INT_SET.Contains(id);
        public static bool IsBadSt(string id) => Array.IndexOf(BAD_ST, id) >= 0 || id == "표식" || id == STUN;
        public static bool IsUnitSt(string id) => Array.IndexOf(UNIT_ST, id) >= 0;

        /// <summary>세기 상태 n 겹이 하는 일 — 사기 3 → 0.6. 최대 겹 · 불굴Cap 을 지킨다.</summary>
        public static double StackEff(string id, int n)
        {
            double max = SMax(id) ?? double.PositiveInfinity;
            double k = Math.Max(0, Math.Min(n, max));
            double v = k * SV(id);
            return id == "불굴" ? Math.Min(v, STATUS_V["불굴Cap"]) : v;
        }

        public static readonly (double def, double atk) DEF_DMG = (2.1, 0.3);
        public static int DefDmgStat(int atk, int def) => Math.Max(1, Num.Round(def * DEF_DMG.def + atk * DEF_DMG.atk));
        public const int FOE_DOT = 20;
        public const int FOE_INT_MAX = 3;
        public const int KILL_AP = 0;

        // ── 강인도 · 격파 ────────────────────────────────────────────
        public static class TOUGH
        {
            /// <summary>
            /// 적 데이터에 tough 가 없을 때의 칸(2026-10-05 다시 — Docs/데이터.md §15 강인도 기준): 일반 4 · 엘리트 6 · 보스 10.
            /// 단위 = 약점 공격 AP 1. 엘리트 싸움에 같이 선 여린 적(칸이 Elite 보다 작은)은 EliteMinion 만큼 더.
            /// 잔광은 강인도 피해 +Glow.
            /// </summary>
            public const double Fight = 4, Elite = 6, Boss = 10, EliteMinion = 1, Glow = 1;
            /// <summary>모든 적의 최대 강인도 최소치(사용자 규칙 2026-10-05 — 소환물 · 잔챙이 포함). 데이터가 이보다 작으면 검사 오류, 엔진도 이 아래로 세우지 않는다.</summary>
            public const double Min = 3;
            public const int Ap = 1;
            /// <summary>소수 찌꺼기를 지우는 눈금(1/60 — 1/2 · 1/3 · 1/4 · 1/5 · 1/6 · ×0.8 이 다 맞아떨어진다).</summary>
            public const double Grid = 60;
        }

        /// <summary>
        /// 강인도 피해(카드가 적 하나를 처음 칠 때) — 사용자 확정 단위(Docs/키워드.md §1): 약점 공격은 카드 비용 1 당 1 · 약점이 아니면 그 1/3.
        /// 비용 0 은 비용 1/2 로 친다(약점 1/2 · 아니면 1/6) · 광역(allEnemies)은 대상마다 절반 · X 는 낸 AP · 고학년은 비용 2.
        /// 2026-10-05 — 옛 「카드마다 0.5 + 약점 0.5」 를 바꿨다.
        /// </summary>
        public static double ToughDmg(int cost, bool weak, bool area)
        {
            double unit = cost > 0 ? cost : 0.5;
            double v = weak ? unit : unit / 3;
            return area ? v / 2 : v;
        }

        /// <summary>약점 속성으로 치면 피해 +25%(카제나 추정값, Docs/키워드.md §0). 약점이 아닌 상성 우위는 NATURE_DMG.</summary>
        public const double WEAK_DMG = 0.25;

        // ── 성격 상성 ────────────────────────────────────────────────
        public static readonly Dictionary<string, string> BEATS = new() { ["광기"] = "순수", ["순수"] = "냉정", ["냉정"] = "광기", ["활발"] = "우울", ["우울"] = "활발" };
        public static readonly string[] NATURES = { "순수", "광기", "냉정", "활발", "우울", "공명" };
        public const double NATURE_DMG = 0.10;
        public const double NATURE_DEF = 0.05;
        /// <summary>판의 적 속성으로 뽑는 성격 — 공명은 뺀다(공명 적은 약점이 없다).</summary>
        public static readonly string[] FOE_NATURES = { "순수", "광기", "냉정", "활발", "우울" };
        public static List<string> WeakTo(string nature) => BEATS.Where(kv => kv.Value == nature).Select(kv => kv.Key).ToList();
        public static int NatureEdge(string attacker, string defender)
        {
            if (string.IsNullOrEmpty(attacker) || string.IsNullOrEmpty(defender)) return 0;
            if (attacker == "공명" && defender != "공명") return 1;
            if (defender == "공명" && attacker != "공명") return -1;
            if (BEATS.TryGetValue(attacker, out var a) && a == defender) return 1;
            if (BEATS.TryGetValue(defender, out var d) && d == attacker) return -1;
            return 0;
        }

        // ── 최종 피해 ────────────────────────────────────────────────
        /// <summary>최종 = 스탯 × (배율 + 신탁) × 축복(×1.3) × (1 + 전역) × 치명(×1.5). 증가가 있으면 최소 1.</summary>
        public static int FinalDamage(double stat, double ratio, double flash = 0, bool shin = false, double global = 0, bool crit = false)
        {
            double v = stat * (ratio + flash);
            if (shin) v *= SHIN;
            v *= 1 + global;
            if (crit) v *= CRIT_MULT;
            int o = Num.Round(v);
            if (o == 0 && stat * ratio > 0) return 1;
            return o;
        }

        /// <summary>강화 카드의 「전투 내내」 — 화면은 이 값 이상이면 「전투 내내」 라고 적는다.</summary>
        public const int BOON_TURNS = 9999;
        /// <summary>「전투 내내 공격력 +N%」 의 사도마다 상한 — 15% × 사기 겹 상한(10).</summary>
        public const double MORALE_ATK = 0.15;

        /// <summary>희귀종 덧붙임(엘리트) — id → 뜻. EnemyDef.Rare 에 쓴다.</summary>
        public static readonly Dictionary<string, string> RARES = new()
        {
            ["poisonHand"] = "턴 시작에 손 2장에 독", ["reshuffle"] = "행동할 때마다 손을 모두 버리고 섞은 뒤 상태 카드 1장(card)",
            ["anxietyHits"] = "덱에 든 상태 카드(card) 수만큼 타격 +1", ["autoCard"] = "턴 시작에 손의 무작위 카드 1장이 저절로 나간다",
            ["costUp"] = "턴 시작에 손 2장 비용 +1", ["crystal"] = "전투 시작에 결정화 3", ["actDebuff"] = "행동할 때 파티에 취약 2(st 로 약화도)",
            ["toughGuard"] = "받는 강인도 피해 -20%",
        };

        public static readonly string[] CARD_TYPES = { "공격", "스킬", "강화", "상태", "저주" };
        public static readonly string[] ROLES = { "탱커", "서포터", "딜러" };
        public static readonly string[] ROWS = { "front", "mid", "back" };
    }
}
