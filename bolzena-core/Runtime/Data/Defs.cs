using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    // 콘텐츠 데이터의 틀 — 사람이(또는 AI 가) JSON 으로 손으로 쓴다. 글(카드 면 · 패시브 줄)은 데이터에서 만든다(CardText).
    // JSON 키는 camelCase(속성 이름의 첫 글자를 내린 것). 자세한 설명은 Docs/데이터.md.

    /// <summary>
    /// 효과 조각 하나. k 가 종류, 나머지는 종류마다 쓰는 칸만 쓴다.
    /// 카드 · 신탁 · 축복 · 패시브 · 키워드 규칙 · 고학년 스킬이 모두 이 조각의 목록을 쓴다.
    /// 조건 조각(ifBroken · ifChain · when …)은 그 뒤의 조각을 그 조건에 건다.
    /// </summary>
    public sealed class Fx
    {
        public string K;
        /// <summary>수(드로우 · AP · 상태 겹 · 증감 비율 · 강인도 …).</summary>
        public double V;
        /// <summary>「전부」(spend · spendRhythm · discard).</summary>
        public bool All;
        /// <summary>배율(1.2 = 120%) — dmg · block · shield · heal.</summary>
        public double Ratio;
        /// <summary>대상: oneEnemy · allEnemies · randomEnemy · self · oneAlly · allAllies · party (없으면 종류마다 기본).</summary>
        public string Target;
        /// <summary>상태 이름 · 키워드 이름 · 만들 카드 id.</summary>
        public string Id;
        /// <summary>여러 번 치기(dmg) — 0 이면 1.</summary>
        public int Hits;
        /// <summary>조건의 수(ifRhythm n · ifStack n).</summary>
        public int N;
        /// <summary>증감의 길이(턴). 0 이면 1.</summary>
        public int Turns;
        /// <summary>「전투 내내」 증감.</summary>
        public bool Run;
        /// <summary>dmg 의 바탕 — "def" 면 방어 기반 피해.</summary>
        public string Base;
        /// <summary>고정 피해 · 고정 실드.</summary>
        public bool Fixed;
        /// <summary>X 코스트 — 낸 AP 만큼 친다(XStack 이 있으면 그 키워드 겹을 더한다).</summary>
        public bool XHits;
        public string XStack;
        /// <summary>ifStack 의 반대(「「X」가 없으면」).</summary>
        public bool Not;
        /// <summary>ifPrev 의 카드 종류(공격 · 스킬 · 강화).</summary>
        public string Type;
        /// <summary>when 의 때: draw(영감) · discard(안식) · handEnd(턴 끝에 손에 있으면).</summary>
        public string On;
        /// <summary>discard — 무작위로 버린다.</summary>
        public bool Random;
        /// <summary>perPaid — 몇 HP 마다 1(기본 100).</summary>
        public int Per;
        /// <summary>ifHp — 파티 HP 비율(0.5 = 50% 이하).</summary>
        public double Pct;
        /// <summary>dmg — 고정 지속 피해(증감 · 상태 · 방어 · 실드를 모두 안 탄다).</summary>
        public bool Dot;
        /// <summary>cardStatus — 어느 카드에: this(이 카드 · 기본) · hand(손의 무작위 n장, n 0 이면 전부) · draw(뽑을 더미 무작위 n장).</summary>
        public string To;
        /// <summary>transform — 손에서 바꿀 카드 id(없으면 이 카드 자신). pull · exileFrom · perPile · ifPile — 더미(draw · discard · gone · hand).</summary>
        public string From;
        /// <summary>pull — 더미의 어디서(top · bottom · random). make — 넣는 자리는 to(hand · draw · top · discard).</summary>
        public string At;
        /// <summary>ifStack 의 위 끝(n 이상 max 이하 — 태세 · 순환 단계를 가른다). 0 이면 없음.</summary>
        public int Max;
        /// <summary>later · afterCards · trap 의 안에 든 효과.</summary>
        public List<Fx> Then;
        /// <summary>trap — 그 적이 공격이 아닌 수를 하면 대신 도는 효과.</summary>
        public List<Fx> Else;
        // ── v2 콘텐츠 요구(2026-10-05 넷째) — 카드 거르개 · 그 밖 ──
        /// <summary>카드 거르개 — 주인: self(카드 주인 · 패시브 주인) · other(다른 사도) · 사도 id. (type 은 카드 종류 거르개로도 쓴다)</summary>
        public string Who;
        /// <summary>카드 거르개 — 고유 카드만 · 시작(기본) 카드만.</summary>
        public bool Unique, Basic;
        /// <summary>카드 거르개 — 이 태그가 붙은 카드만.</summary>
        public string Tag;
        /// <summary>perStack(적 표식) — 대상 적마다 제 겹을 센다(allEnemies 와).</summary>
        public bool Each;
        /// <summary>cardStatus — 이 전투에서만(판을 넘기지 않는 카드 값).</summary>
        public bool Battle;
        /// <summary>dmg · shield — 일의 값(EventV) × 이것을 고정값으로(넘친 치유 100% → 고정 피해).</summary>
        public double OfEvent;
        /// <summary>dmg · shield — 이 고유 효과의 지금 겹을 일의 값으로 삼는다(ofEvent 와 같이: 겹 × ofEvent 고정값). stack — 쓰지 않음.</summary>
        public string OfStack;
        /// <summary>power — 강화 카드의 지속 규칙(패시브 규칙과 같은 꼴). 카드를 내면 이 전투 끝까지 켜져 있다.</summary>
        public List<PassiveRule> Rules;
        /// <summary>make — 만든 카드의 주인(2026-10-08 1단계): self(만든 사도 · 기본) · other(파티 차례로 다음 아군) · 사도 id. 손패는 파티에 하나라 「다른 아군 손에」 대신 주인을 바꾼다.</summary>
        public string Owner;
        /// <summary>spend — 낼 때 소모량을 고른다(PlayOpts.Spend · 안 고르면 전부). 소모한 양이 일의 값(perEvent)이 된다.</summary>
        public bool Pick;
        // ── 엘다인 문법(2026-10-11, Docs/데이터.md §22) ──
        /// <summary>spendShield — 파티 실드 절반 · cardStatus — 카드 값을 절반으로.</summary>
        public bool Half;
        /// <summary>dmg · shield — 일의 값 = 지금 파티 실드(ofEvent 와 같이).</summary>
        public bool OfShield;
        /// <summary>카드 거르개 — 생성 카드(token)만.</summary>
        public bool Token;
        /// <summary>dmg — 한 대마다 맞은 그 적에게 도는 효과.</summary>
        public List<Fx> HitFx;
        /// <summary>stack — v 대신 일을 겪은(쓰러진) 적의 그 표식 겹(표식 옮기기).</summary>
        public bool Carry;
        /// <summary>stack · cardStatus — 더하기 대신 그 값으로 정한다.</summary>
        public double? Set;
        /// <summary>dmg — 친 횟수 = 이 고유 효과의 지금 겹 + hitsAdd.</summary>
        public string HitsPer;
        public int HitsAdd;
        /// <summary>spend pick — 치는 동안 새로 얻은 겹을 이어 쓴다(max 대까지, 기본 12).</summary>
        public bool Loop;
        /// <summary>ifFoe — 고른 적 HP 비율이 이 값 이하면(id hp 와 같다).</summary>
        public double HpBelow;
        /// <summary>perGone — 이번 턴에 사라진 것만.</summary>
        public bool Turn;
        // ── 엘다인 3차(2026-10-11 카제나 재정렬) ──
        /// <summary>costMod · cardStatus — 손에서 고를 카드: priciest(가장 비싼 카드 한 장 — 같으면 앞의 것).</summary>
        public string Choose;
        /// <summary>cardStatus — 이 값이 붙은 동안 그 카드는 보존(낸 뒤 벗는다).</summary>
        public bool Keep;
        /// <summary>ifFoe — 이번 전투에 이 사도(self)가 그 적을 친 횟수(타마다) n 이상이면(▶E71).</summary>
        public string HitsBy;
        /// <summary>execute — 보스 · 클론에게는 쓰러뜨리는 대신 이 비율의 피해(▶E72).</summary>
        public double BossRatio;
        /// <summary>stack — 정한 뒤 turns 턴 동안 이 고유 효과가 움직이지 않는다(▶E74 위상 고정).</summary>
        public bool Lock;
        // ── 엘다인 4차(2026-10-11, 02 E76~E87) ──
        /// <summary>dmg — 일의 값 = 이번 변신 중 그 사도가 준 피해(새로 들 때 0), × ofForm 고정 피해(▶E76).</summary>
        public double OfForm;
        /// <summary>ifNth — 꼭 n번째가 아니라 n번째 이후면(▶E77). id 가 있으면 글 이름(「정각이면」).</summary>
        public bool After;
        /// <summary>dmg — 치는 동안 처치하면 한 대 더(max 대까지, 기본 12 — ▶E79).</summary>
        public bool KillMore;
        /// <summary>critDmg — 끝날 때 도는 효과(▶E83).</summary>
        public List<Fx> Off;
        /// <summary>critNext — 다음 카드가 아니라 이 카드(이 조각 뒤의 피해)에 치명 확정.</summary>
        public bool Now;
        /// <summary>critNext now — 이 카드가 친 적에게 이 표식(적 표식 키워드 · 상태)이 있으면 그 대만 치명 확정(조건 조각이 아니다 · 카드의 어디에 둬도 처음부터).</summary>
        public string FoeStack;
        /// <summary>카드 거르개 · empower — 비용이 이 값 이상인 카드만(0 이면 없음).</summary>
        public int MinCost;
        /// <summary>(아야) dmg — 아는 수(만년설의 앎)를 하는 적에게는 이 횟수로 친다.</summary>
        public int KnownHits;
        /// <summary>(아야) ifFoe known · knownHits — foeTurn 이면 같은 적에게 그 적 차례마다(내 턴마다) 한 번만 덤.</summary>
        public string Once;

        [Newtonsoft.Json.JsonIgnore] public int NOr1 => N <= 0 ? 1 : N;

        [Newtonsoft.Json.JsonIgnore] public int IV => (int)V;
        [Newtonsoft.Json.JsonIgnore] public int HitsOr1 => Hits <= 0 ? 1 : Hits;
        [Newtonsoft.Json.JsonIgnore] public int TurnsOr1 => Turns <= 0 ? 1 : Turns;

        public Fx Copy() => (Fx)MemberwiseClone();
    }

    /// <summary>효과 조각 k 의 이름들 — 엔진(Battle.RunFx)의 갈래와 1:1.</summary>
    public static class FxK
    {
        // 조건 · 대상 고르기
        public const string IfBroken = "ifBroken", IfTune = "ifTune", IfChain = "ifChain", IfLink = "ifLink", IfPrev = "ifPrev",
            IfRhythm = "ifRhythm", IfSwitched = "ifSwitched", IfStack = "ifStack", PerStack = "perStack", PerRhythm = "perRhythm", When = "when";
        // 운영 방식 계기(docs/19 §3 「새로」) — 박자 · 아껴 두기 · 계산 · 대가 · 표적 · 흠 세기 · 버리기 · 버팀
        public const string IfRepeat = "ifRepeat", IfHeld = "ifHeld", IfPlayedMax = "ifPlayedMax", IfApLeft = "ifApLeft", IfSpent = "ifSpent", IfBalanced = "ifBalanced",
            IfHunted = "ifHunted", IfDebuffs = "ifDebuffs", PerDiscarded = "perDiscarded", PerPaid = "perPaid", PerDebuff = "perDebuff",
            NextAp = "nextAp", Burn = "burn", Reflect = "reflect",
            // 짝을 잇는 것(2026-10-04 둘째) — 파티 HP 조건 · 남긴 AP 비례 · 아군 키워드 채우기
            IfHp = "ifHp", PerApLeft = "perApLeft", Feed = "feed";
        // 피해 · 방어 · 회복
        public const string Dmg = "dmg", Block = "block", Shield = "shield", Heal = "heal", Strip = "strip";
        // 증감
        public const string DealtMod = "dealtMod", TakenMod = "takenMod", AtkMod = "atkMod", DefMod = "defMod", CritMod = "critMod", HealMod = "healMod";
        // 자원
        public const string Draw = "draw", Ap = "ap", NextCheaper = "nextCheaper", Gauge = "gauge", Discard = "discard", Make = "make";
        // 상태 · 강인도 · 즉시 행동
        public const string Status = "status", Cleanse = "cleanse", Tough = "tough", RushDown = "rushDown";
        // 키워드 · 공용 부품
        public const string Stack = "stack", Spend = "spend", SpendRhythm = "spendRhythm", Flip = "flip", Hasten = "hasten";
        // HP 치르기
        public const string PayHp = "payHp", PayHpPct = "payHpPct";
        // 키워드 사전(2026-10-05) — 처치 · 붕괴 · 부상 조건, 손의 태그 비례, 피해 기반 회복, 추가 공격, 카드에 붙는 상태, 카드 바꾸기
        public const string IfKill = "ifKill", IfBreak = "ifBreak", IfWounded = "ifWounded", PerTag = "perTag",
            Drain = "drain", Extra = "extra", CardStatus = "cardStatus", Transform = "transform";
        // 사도 고유 효과 틀(2026-10-05 셋째) — 갈래 · 무작위 · 손/더미 수 · 카드 차례 · 적 상태 조건, 예약 · 함정 · 혼란 · 저절로 내기 · 다른 사도 카드 대신 발동 ·
        // 더미 조작 · 적 버프 지우기 · 열 옮기기 · 판 단위 성장, 카드 값(인스턴스 카운터)
        public const string IfChoice = "ifChoice", IfRandom = "ifRandom", IfHand = "ifHand", IfPile = "ifPile", IfNth = "ifNth", IfStreak = "ifStreak", IfAllHeroes = "ifAllHeroes",
            IfFoe = "ifFoe", IfCardSt = "ifCardSt", PerPlayed = "perPlayed", PerPile = "perPile", PerCardSt = "perCardSt", PerEvent = "perEvent",
            Later = "later", AfterCards = "afterCards", Trap = "trap", Confuse = "confuse", AutoPlay = "autoPlay", CastOther = "castOther",
            Pull = "pull", ExileFrom = "exileFrom", Dispel = "dispel", GrowRun = "growRun";
        // v2 콘텐츠 요구(2026-10-05 넷째) — 배타 무작위 · 조건 · 다시 내기 · 비용 증감 · 태그 덧붙임 · 한 대 깎기 · 빚 탕감
        public const string Roll = "roll", IfRoll = "ifRoll", IfPrevSame = "ifPrevSame", IfInHand = "ifInHand", IfBond = "ifBond", IfLastMine = "ifLastMine",
            IfPulled = "ifPulled", IfShield = "ifShield", IfDebt = "ifDebt", IfTypeNew = "ifTypeNew",
            Recast = "recast", CostMod = "costMod", AddTag = "addTag", CutHit = "cutHit", ClearDebt = "clearDebt";
        // 변신(2026-10-05 여섯째) — 그 사도가 변신 상태가 된다(HeroDef.Forms 의 id) · 지금 변신을 푼다
        public const string Form = "form", FormEnd = "formEnd";
        // 강화 카드 지속 규칙(2026-10-05 일곱째) — 카드를 내면 rules 가 이 전투 끝까지 켜진다(같은 id 를 또 내면 겹 +1)
        public const string Power = "power";
        // 연출 쪽지(2026-10-06) — 효과는 없고 화면 · 이펙트에 그 순간을 알린다(cue id · 수). 글에 안 나온다
        public const string Cue = "cue";
        // 18갈래 1단계(2026-10-08) — 다음 카드 강화 · 막아 낸 양 비례 · 넘친 회복 비례(이번 판)
        public const string Empower = "empower", PerGuarded = "perGuarded", PerOverheal = "perOverheal";
        // 시범 16(2026-10-08 2단계) — 직전보다 비싼가 · 이번 전투 소멸 장수 비례 · 이번 전투 쌓은 고유 효과 · 예약 당겨 쓰기(남은 칸 비례) · 소환물 행동
        public const string IfPricier = "ifPricier", PerGone = "perGone", IfGained = "ifGained", Ripen = "ripen", Summon = "summon";
        // 엘다인 문법(2026-10-11) — 파티 실드 소모 · 실드 비례 · 변신 조건 · 변신 연장 · 이번엔 망각 없이
        public const string SpendShield = "spendShield", PerShield = "perShield", IfForm = "ifForm", FormExtend = "formExtend", NoForget = "noForget";
        // 엘다인 3차(2026-10-11) — 반격 모양 · 반격 피해 · 게이지 잠금 · 카드 기록 · 재현
        public const string IfEvent = "ifEvent";
        // 엘다인 3차 원작 검토(E63~E75) — 도발 · 다음 동료 카드 치명 · 처형
        public const string Taunt = "taunt", CritNext = "critNext", Execute = "execute", StackLock = "stackLock";
        public const string CounterHits = "counterHits", CounterAll = "counterAll", CounterMod = "counterMod", GaugeLock = "gaugeLock", Record = "record", Replay = "replay";
        // 엘다인 4차(E76~E87) — 정각 당겨 쓰기 · 고학년 요구 게이지 · 이번 턴 신속 · 치명 피해 상태 · 맞은 대 수 조건
        public const string NthNext = "nthNext", UltCostMod = "ultCostMod", SwiftTurn = "swiftTurn", CritDmg = "critDmg", IfHitTaken = "ifHitTaken";
        // 엘다인 4차 태세 — n턴간 태세(변신) 보존(풀리는 계기 · 시간에 안 풀림)
        public const string FormKeep = "formKeep";
        // 엘다인 4차 리뉴아 「시간」 — 이 카드를 비용 n 이하로 내면(정각)
        public const string IfCost = "ifCost";
        // 우로스 선 · 악 — 이번엔 태세를 바꾸지 않음
        public const string NoToggle = "noToggle";
        // 벨라 「존속의 촛불」 — 다른 동료에게 붙은 그 표시를 지금 대상에게 모두 옮김
        public const string MoveMark = "moveMark";
        // 아야 「만년설의 앎」 — 적의 지금 수 종류를 적음
        public const string Learn = "learn";
        // (2026-10-13 이드) X 코스트 카드 — 이 카드에 쓴 AP(X) 1당
        public const string PerX = "perX";
        // (2026-10-13 비비 원작 「냉기 충전 I」) 살아 있는 적 1당
        public const string PerFoe = "perFoe";

        public static readonly HashSet<string> Conditions = new() { IfBroken, IfTune, IfChain, IfLink, IfPrev, IfRhythm, IfSwitched, IfStack, When,
            IfRepeat, IfHeld, IfPlayedMax, IfApLeft, IfSpent, IfBalanced, IfHunted, IfDebuffs, IfHp, IfKill, IfBreak, IfWounded,
            IfChoice, IfRandom, IfHand, IfPile, IfNth, IfStreak, IfAllHeroes, IfFoe, IfCardSt,
            IfRoll, IfPrevSame, IfInHand, IfBond, IfLastMine, IfPulled, IfShield, IfDebt, IfTypeNew, IfPricier, IfGained, IfForm, IfEvent, IfHitTaken, IfCost };
        /// <summary>「… 1개당」 — 바로 뒤 피해 · 방어 · 실드 · 회복 한 줄을 그 수만큼.</summary>
        public static readonly HashSet<string> Pers = new() { PerStack, PerRhythm, PerDiscarded, PerPaid, PerDebuff, PerApLeft, PerTag, PerPlayed, PerPile, PerCardSt, PerEvent, PerGuarded, PerOverheal, PerGone, PerShield, PerX, PerFoe };
        /// <summary>카드의 때 붙은 마디(when on) — 영감(능력으로 뽑힘) · 감응(뽑힘) · 안식(버려짐) · 턴 끝에 손에 · 소각(소멸할 때) · 열정(손에 있을 때 열정 카드가 나가면).</summary>
        public static readonly string[] WHEN_ON = { "draw", "drawAny", "discard", "handEnd", "burn", "passion", "handHurtBig", "shieldLeft" };   // shieldLeft(엘다인 4차 벨라) — 낸 뒤 이번 적 차례가 끝날 때 파티 실드가 남았으면   // handHurtBig(엘다인 3차) — 손에 있는 동안 아군이 한 대에 최대 HP pct 넘게 잃으려 하면
        /// <summary>옛 운영 방식 부품(리듬 · 전환 · 재촉/예약 · 잇기 · 앞이 … · 되풀이 …) — 지우지 않는다. 새 콘텐츠는 키워드 사전(Docs/키워드.md)의 것을 쓴다.</summary>
        public static readonly HashSet<string> Old = new() { IfLink, IfPrev, IfRhythm, IfSwitched, PerRhythm, SpendRhythm, Flip, Hasten, IfRepeat, IfHeld, IfPlayedMax, IfSpent, IfBalanced, IfHunted };
        public static readonly HashSet<string> Mods = new() { DealtMod, TakenMod, AtkMod, DefMod, CritMod, HealMod };
        public static readonly HashSet<string> All = new()
        {
            IfBroken, IfTune, IfChain, IfLink, IfPrev, IfRhythm, IfSwitched, IfStack, PerStack, PerRhythm, When,
            Dmg, Block, Shield, Heal, Strip, DealtMod, TakenMod, AtkMod, DefMod, CritMod,
            Draw, Ap, NextCheaper, Gauge, Discard, Make, Status, Cleanse, Tough, RushDown,
            Stack, Spend, SpendRhythm, Flip, Hasten, PayHp, PayHpPct,
            IfRepeat, IfHeld, IfPlayedMax, IfApLeft, IfSpent, IfBalanced, IfHunted, IfDebuffs, PerDiscarded, PerPaid, PerDebuff, NextAp, Burn, Reflect,
            IfHp, PerApLeft, Feed,
            IfKill, IfBreak, IfWounded, PerTag, Drain, Extra, CardStatus, Transform,
            IfChoice, IfRandom, IfHand, IfPile, IfNth, IfStreak, IfAllHeroes, IfFoe, IfCardSt, PerPlayed, PerPile, PerCardSt, PerEvent,
            Later, AfterCards, Trap, Confuse, AutoPlay, CastOther, Pull, ExileFrom, Dispel, GrowRun,
            Roll, IfRoll, IfPrevSame, IfInHand, IfBond, IfLastMine, IfPulled, IfShield, IfDebt, IfTypeNew, Recast, CostMod, AddTag, CutHit, ClearDebt, HealMod,
            Form, FormEnd, Power, Cue, Empower, PerGuarded, PerOverheal,
            IfPricier, PerGone, IfGained, Ripen, Summon,
            SpendShield, PerShield, IfForm, FormExtend, NoForget,
            CounterHits, CounterAll, CounterMod, GaugeLock, Record, Replay, IfEvent,
            Taunt, CritNext, Execute, StackLock,
            NthNext, UltCostMod, SwiftTurn, CritDmg, IfHitTaken, FormKeep, IfCost, NoToggle, MoveMark, Learn, PerX, PerFoe,
        };
        public static string ModStat(string k) => k switch
        {
            DealtMod => "dealt", TakenMod => "taken", AtkMod => "atk", DefMod => "def", CritMod => "crit", HealMod => "heal", _ => null,
        };
    }

    /// <summary>카드 태그 — 글 맨 앞 낱말(docs/18 §4). 「소멸 2」 · 「회수 2」 처럼 수가 붙을 수 있다.</summary>
    public static class Tag
    {
        public const string Opening = "개전", Keep = "보존", Exhaust = "소멸", Evaporate = "증발", Finale = "종극", Lead = "주도", Unique = "유일",
            Link = "연계", Heaven = "천상", Swift = "신속", Crush = "분쇄", Weak = "약점", Connect = "연결", Curtain = "개막", Taboo = "금기",
            Seal = "봉인", Recall = "회수", Echo = "연쇄", Unplayable = "사용 불가", Power = "강화",
            // 키워드 사전(2026-10-05). 「약점」 은 옛 이름 — 새 콘텐츠는 「약점 공격」
            Forget = "망각", Remove = "제거", SealedTaboo = "봉인된 금기", WeakHit = "약점 공격", Bullet = "탄환", Bond = "결속", Blessing = "축복", Passion = "열정";
        public static readonly string[] All = { Opening, Keep, Exhaust, Evaporate, Finale, Lead, Unique, Link, Heaven, Swift, Crush, Weak, Connect, Curtain, Taboo, Seal, Recall, Echo, Unplayable,
            Forget, Remove, SealedTaboo, WeakHit, Bullet, Bond, Blessing, Passion };
        /// <summary>수가 붙는 태그와 사전의 수 — 소멸 N(1 · 2 · 3 · 5) · 회수 N(3 · 4). 다른 수는 검사기 주의.</summary>
        public static readonly Dictionary<string, int[]> Counted = new() { [Exhaust] = new[] { 1, 2, 3, 5 }, [Recall] = new[] { 3, 4 } };

        /// <summary>「소멸 2」 → ("소멸", 2) · 「보존」 → ("보존", 0).</summary>
        public static (string id, int n) Parse(string t)
        {
            t = (t ?? "").Trim();
            int sp = t.LastIndexOf(' ');
            if (sp > 0 && int.TryParse(t.Substring(sp + 1), out int n)) return (t.Substring(0, sp).Trim(), n);
            return (t, 0);
        }
    }

    /// <summary>카드 한 장.</summary>
    public sealed class CardDef
    {
        public string Id;
        public string Name;
        /// <summary>주인 사도 id — 없으면 교주 카드 · 상태 카드 · 저주.</summary>
        public string Hero;
        public int Cost;
        /// <summary>X 코스트(남은 AP 를 전부 쓴다).</summary>
        public bool X;
        /// <summary>공격 · 스킬 · 강화 · 상태 · 저주.</summary>
        public string Type;
        public List<string> Tags = new();
        public List<Fx> Fx = new();
        /// <summary>사도의 고유 카드(은총으로 얻는다). 아니면 시작 카드.</summary>
        public bool Unique;
        public bool Signature;
        /// <summary>신탁 ①~⑤ — 고르면 그 카드가 바뀐다.</summary>
        public List<OracleDef> Oracles = new();
        /// <summary>그 카드만의 겨우살이의 축복(셋까지). 없으면 공용 축복 풀(R.DIVINE_KINDS).</summary>
        public List<BlessDef> Blesses = new();
        /// <summary>교주 카드 — 상점 진열 등급 · 값.</summary>
        public string Grade;
        public int Price;
        /// <summary>이벤트의 선물 카드(덱에 한 장).</summary>
        public bool Gift;
        /// <summary>사람이 읽는 한 줄(이야기).</summary>
        public string Blurb;
        /// <summary>사도 전용 생성 카드(토큰) — 덱에 안 들어간다. 효과(make)로만 손에 생긴다.</summary>
        public bool Token;
        /// <summary>같은 카드가 손에 n 장 모이면 into 한 장으로 합쳐진다(★ → ★★ 진화).</summary>
        public EvolveDef Evolve;
        /// <summary>결속 — 겹친 수가 3 이상이면 이 카드(강해진 카드) 로 낸다. 사도마다 카드 데이터에서 정한다.</summary>
        public string BondCard;
        /// <summary>완성형(2026-10-10 카제나식) — 신탁(번뜩임) 없이 처음부터 완성된 고유 카드. 신탁 후보에 안 뜨고, 축복은 신탁 없이 혼자 붙는다.</summary>
        public bool Complete;
        /// <summary>봉인된 금기 — 보스를 처치하면 덱에서 이 카드(금기 카드)로 바뀐다.</summary>
        public string Becomes;
        /// <summary>비용을 AP 대신 이 키워드(사도 고유 효과의 겹)로 치른다 — 「시간 초월로 비용 지불」 꼴.</summary>
        public string PayWith;
        /// <summary>AP 가 모자라도 낸다 — 모자란 만큼 다음 턴 AP 가 준다(AP 빚).</summary>
        public bool Debt;
        /// <summary>payWith — AP 1 을 고유 효과 몇 개로(기본 1) · 모자라면 AP 로 섞어 치른다.</summary>
        public int PayRate;
        public bool PayMix;
        /// <summary>(2026-10-13 시온 마탄) 손에 쥘 수 있는 이 카드의 최대 장수 — 0 이면 제한 없음. 넘게 만들면 손에 안 들고 탄환이면 바로 쏜다(Battle.Make).</summary>
        public int HandMax;
        /// <summary>(2026-10-13 시온 — 버려서 쏘기) 이 카드의 탄환 추가 공격 배율(공격력 ×) — 0 이면 R.SV("탄환"). 카드마다 따로 키운다(교주 카드는 그대로).</summary>
        public double BulletRatio;
        /// <summary>두 갈래로 내기 — 갈래 이름(둘). 낼 때 PlayOpts.Choice(1 · 2)로 고르고 효과는 ifChoice n 으로 가른다.</summary>
        public List<string> Choices;
        /// <summary>(엘다인 ▶E2) 천상 계기에서만 이 카드를 비용 N 으로 센다(직접 낼 때). 0 이면 적힌 비용.</summary>
        public int CountAs;
        /// <summary>(엘다인 4차 태세) 그 변신 · 태세 중에만 낼 수 있다(변신 id · 「*」 = 아무 변신).</summary>
        public string NeedForm;
        /// <summary>(2026-10-13 이드) 손에 그 카드가 있으면 이 카드 비용 +add — Battle.CostOf 가 센다.</summary>
        public CostIfDef CostIf;

        [Newtonsoft.Json.JsonIgnore] public bool Neutral => Hero == null && (Type == "공격" || Type == "스킬" || Type == "강화") && !Gift && !Token;
        /// <summary>얻을 때 주인 사도를 고르는 카드 — 교주 카드 · 선물 카드 · 골칫거리(저주) · 상태 카드처럼 사도가 없는 것(만든 카드 제외).
        /// 교주 카드가 아닌 것의 주인(「id@사도」)은 덱 묶음 표시만 — 전투에서 누가 내는지 · 효과는 바뀌지 않는다(View 의 Hero 는 교주 카드만).</summary>
        [Newtonsoft.Json.JsonIgnore] public bool Ownable => Hero == null && !Token && (Neutral || Gift);   // 주인을 고르는 것은 교주 · 선물 카드만 — 상태 · 저주는 주인 없이 덱의 「상태」 · 「저주」 묶음으로(2026-10-06 사용자)
        [Newtonsoft.Json.JsonIgnore] public bool IsStatusCard => Type == "상태";
        [Newtonsoft.Json.JsonIgnore] public bool IsCurse => Type == "저주";
    }

    /// <summary>진화 — 같은 카드 n 장이 손에 모이면 into 한 장(만든 카드)으로.</summary>
    public sealed class EvolveDef
    {
        public int N = 2;
        public string Into;
    }

    /// <summary>신탁 하나 — 고르면 카드의 글 · 효과 · 태그를 통째로 갈아 끼운다(코스트는 적으면 바뀐다).</summary>
    public sealed class OracleDef
    {
        public string Name;
        /// <summary>바뀐 코스트(없으면 그대로).</summary>
        public int? Cost;
        public List<string> Tags = new();
        public List<Fx> Fx = new();
        /// <summary>「강화 카드.」 신탁 — 고르면 이 카드가 강화 카드가 된다.</summary>
        public bool Power;
        /// <summary>(엘다인) 「스킬로」 · 「공격으로」 신탁 — 고르면 카드 종류가 이것으로. 없으면 그대로(power 가 앞선다).</summary>
        public string Type;
        /// <summary>(엘다인 ▶E2) 천상 계기에서 이 신탁의 카드를 비용 N 으로 센다. 없으면 카드의 countAs.</summary>
        public int? CountAs;
        /// <summary>(엘다인 4차 태세) 이 신탁을 얹으면 그 변신 · 태세 중에만 낼 수 있다.</summary>
        public string NeedForm;
        /// <summary>(2026-10-13 이드) 이 신탁의 「손에 그 카드가 있으면 비용 +add」 — 신탁은 제 것만 본다(카드의 costIf 를 물려받지 않음).</summary>
        public CostIfDef CostIf;
    }

    /// <summary>(2026-10-13 이드) 「손에 inHand 카드가 있으면 비용 +add」(이 카드 자신은 빼고 센다).</summary>
    public sealed class CostIfDef
    {
        public string InHand;
        public int Add = 1;
    }

    /// <summary>그 카드만의 축복 — 배율 하나(kind) · 덤 효과 · 덤 태그.</summary>
    public sealed class BlessDef
    {
        public string Name;
        /// <summary>power · cost · weakSpot · frost · ap · draw · heal · guard · atkUp · defUp 가운데 하나(없어도 된다).</summary>
        public string Kind;
        public List<Fx> Fx = new();
        public List<string> Tags = new();
    }

    /// <summary>능력치 줄(장비).</summary>
    public sealed class Stats
    {
        public int Hp, Atk, Def, Crit;
        public static Stats operator +(Stats a, Stats b) => new Stats { Hp = a.Hp + b.Hp, Atk = a.Atk + b.Atk, Def = a.Def + b.Def, Crit = a.Crit + b.Crit };
    }

    /// <summary>사도.</summary>
    public sealed class HeroDef
    {
        public string Id;
        public string Name;
        public string Nature;
        public string Race;
        public string Role;
        /// <summary>옛 칸 「row」(전열 · 중열 · 후열 — 2026-10-06 걷어냄). 옛 데이터를 읽어도 깨지지 않게 받아서 버리고, 검사기가 주의로 알린다.</summary>
        [Newtonsoft.Json.JsonProperty("row")] string RowOld { set => HadRow = value != null; }
        [Newtonsoft.Json.JsonIgnore] public bool HadRow { get; private set; }
        /// <summary>갈래(18갈래 v2, 2026-10-08 되살림) — Validator.STYLES 중 하나. 전투 엔진은 아직 안 보고, 검사기(저절로 터짐 주의) · 통계가 본다.</summary>
        public string Style;
        public int Star = 3;
        public int Hp, Atk, Def, Crit;
        public KeywordDef Keyword;
        /// <summary>고유 효과를 더(자원 + 적 표식 + 태세 처럼 여럿이 필요한 사도). 이름은 135명 사이에서 겹치면 안 된다.</summary>
        public List<KeywordDef> Keywords = new();
        public List<PassiveRule> Passives = new();
        /// <summary>이 사도의 고유 효과 전부(keyword + keywords).</summary>
        [Newtonsoft.Json.JsonIgnore] public IEnumerable<KeywordDef> AllKeywords => (Keyword != null ? new[] { Keyword } : new KeywordDef[0]).Concat(Keywords ?? new List<KeywordDef>());
        public UltDef Ult;
        /// <summary>시작 덱(카드 id) — 보통 넉 장.</summary>
        public List<string> Starter = new();
        public string Blurb;
        /// <summary>변신(고학년 등의 효과 form 으로 들어간다) — 모습 · 능력이 바뀌는 상태. id 는 모든 사도 사이에서 겹치면 안 된다.</summary>
        public List<FormDef> Forms = new();
        /// <summary>(엘다인 4차) 전투 시작 때 든 변신 · 태세 id.</summary>
        public string StartForm;
    }

    /// <summary>
    /// 변신 하나 — 효과 { k: "form", id } 로 그 사도가 이 상태가 된다(개인 층 — 그 사도에게만). 지속(turns 턴 · 0 이면 전투 끝까지) ·
    /// 풀리는 계기(until) · 그동안 바뀌는 것(mods 능력치 · cards 카드 바꾸기 · bonus 카드 덤 · passives 덧붙임/교체) · 풀릴 때(off).
    /// 같은 변신을 변신 중에 다시 쓰면 지속을 처음으로 되돌린다(겹치지 않는다). 다른 변신이면 앞의 것을 풀고(off 가 돈다) 새로.
    /// </summary>
    public sealed class FormDef
    {
        public string Id;
        public string Name;
        /// <summary>한 줄 설명(사람이 읽는 것).</summary>
        public string Desc;
        /// <summary>지속 — 그 사도의 내 턴 N 번(든 턴 포함, N+1 번째 내 턴 시작에 풀린다). 0 이면 전투 끝까지(until · formEnd 로만 풀린다).</summary>
        public int Turns;
        /// <summary>이 일이 나면 풀린다(패시브의 when 과 같은 꼴 — stackGone · play · hurt …). 없으면 지속으로만.</summary>
        public When Until;
        /// <summary>그동안 그 사도의 능력치 증감 — dealt · taken · atk · def · crit · guard · heal → 비율(0.2 = +20%).</summary>
        public Dictionary<string, double> Mods;
        /// <summary>카드 바꾸기 — 그 사도의 카드 id → 변신판 카드 id(token). 손 · 더미의 카드 id 는 그대로, 전투가 그 카드를 변신판으로 본다(풀리면 돌아온다).</summary>
        public Dictionary<string, string> Cards;
        /// <summary>카드 덤 — 거르개에 맞는 그 사도의 카드에 피해 배율 · 효과 · 태그를 더한다.</summary>
        public List<FormBonus> Bonus = new();
        /// <summary>그동안 더 도는 패시브(when always 는 안 된다 — 능력치는 mods).</summary>
        public List<PassiveRule> Passives = new();
        /// <summary>그동안 그 사도의 원래 패시브(passives)를 끈다(고유 효과 규칙 · 장비 효과는 그대로).</summary>
        public bool Replace;
        /// <summary>풀릴 때 도는 효과(그 사도가 한다). 전투가 끝나 풀리는 것은 돌지 않는다.</summary>
        public List<Fx> Off = new();
        /// <summary>모습 키 — 스파인 스킨 이름 · 애니 접두어(화면이 읽는다, 엔진은 안 본다). 모르면 "?".</summary>
        public string Skin, Anim;
        /// <summary>(엘다인 ▶E6) 변신에 드는 순간 + 변신 중 내 턴 시작마다 손에 만드는 변신 카드(token · 증발 · 소멸). when 이 있는 줄은 그 고유 효과가 n 이상이 되는 순간 그 턴 한 장.</summary>
        public List<FormMake> TurnMake = new();
        /// <summary>(엘다인 ▶E22) 지속이 다 돼 풀릴 때만(반동) · formEnd 카드로 풀 때만(보상) — 둘 다 off 뒤에.</summary>
        public List<Fx> OffExpire = new();
        /// <summary>(엘다인 3차) 새로 드는 순간 효과(그 사도가 한다) — 규칙이 아니다.</summary>
        public List<Fx> OnEnter = new();
        public List<Fx> OffEnd = new();
        /// <summary>(엘다인 4차 — 카제나 린 「흑운태세」) 태세 — 변신과 같은 틀(들기 form · 풀기 formEnd · until · turns)이되 글은 「획득 · 해제」, 매 턴 토큰(turnMake) · 겉모습이 없어도 된다.</summary>
        public bool Stance;
        /// <summary>(엘다인 4차 — 우로스 선 · 악) 그 사도가 자기 카드를 내면(카드 효과가 다 돈 뒤) 이 짝 태세로 바뀐다. 덤은 내는 순간의 태세로 본다. 자동 규칙이 아니다.</summary>
        public string Toggle;
    }

    /// <summary>변신 카드 한 줄 — id 카드 n 장(기본 1). when(stack · n)이 있으면 그 고유 효과가 n 이상이 되는 순간 그 턴 한 장.</summary>
    public sealed class FormMake
    {
        public string Id;
        public int N;
        public FormMakeWhen When;
        [Newtonsoft.Json.JsonIgnore] public int NOr1 => N <= 0 ? 1 : N;
    }
    public sealed class FormMakeWhen { public string Stack; public int N; }

    /// <summary>변신 중 카드 덤 — card(그 카드 id) · type · unique · tag 거르개(모두 맞아야), ratio(피해 배율 ×) · fx(뒤에 덧붙임) · tags(덧붙임).</summary>
    public sealed class FormBonus
    {
        public string Card, Type, Tag;
        public bool Unique;
        public double Ratio;
        public List<Fx> Fx = new();
        public List<string> Tags = new();
    }

    /// <summary>고학년 스킬 — 게이지를 써서 AP 없이.</summary>
    public sealed class UltDef
    {
        public string Name;
        public int Cost;
        public List<Fx> Fx = new();
    }

    /// <summary>사도 전용 키워드(고유 효과, docs/18 §6).</summary>
    public sealed class KeywordDef
    {
        public string Name;
        /// <summary>첫 문장 — 사람이 읽는 설명.</summary>
        public string Desc;
        /// <summary>self(자기 주머니) · enemy(적에게 거는 표식) · ally(아군에게 거는 표식 — 파티에 하나).</summary>
        public string Carrier = "self";
        public int? Cap;
        /// <summary>적의 차례가 끝나면 이만큼 준다(다음 내 턴 시작에).</summary>
        public int Decay;
        /// <summary>적의 차례가 끝나면 전부 사라진다.</summary>
        public bool DecayAll;
        /// <summary>발동하면 이만큼 준다.</summary>
        public int Consume;
        /// <summary>발동하면 사라진다.</summary>
        public bool ConsumeAll;
        /// <summary>다른 사도의 카드를 내면 전부 사라진다.</summary>
        public bool Wipe;
        /// <summary>전환하는 모드다(최대 1).</summary>
        public bool Mode;
        /// <summary>예약이다(재촉이 줄인다).</summary>
        public bool Reserve;
        /// <summary>찍기(표적형) — 적에게 거는 표식이 한 번에 한 적에게만. 다른 적에게 쌓으면 옛 적의 것은 사라진다(옮기면 처음부터).</summary>
        public bool Hunt;
        /// <summary>(엘다인 4차 — 란 사냥감) hunt 표식이 붙은 적이 쓰러지면 그 겹 그대로 다른 적에게 옮긴다 — lowEnemy(HP 비율이 가장 낮은 산 적). 규칙이 아니다.</summary>
        public string HuntCarry;
        /// <summary>(엘다인 4차 — 벨라 「존속의 촛불」) 아군 표시(carrier hero)가 타는 꼴 — 붙은 동료가 자기 카드를 낼 때마다 1 이 타며 키워드 주인 방어력 기준 실드. 규칙이 아니다.</summary>
        public KwBurn Burn;
        /// <summary>(엘다인 4차 — 아야 「만년설의 앎」) 적의 수 종류(공격 · 방어 · 강화 · 방해)를 적어 두는 칸 — 겹 = 아는 종류 수(최대 4 · 전투 끝까지 · 쓰지 않음). learn 이 적는다.</summary>
        public bool Known;
        /// <summary>내 턴이 끝나면 이만큼 준다 · 전부 사라진다(decay 는 적의 차례가 끝난 뒤 = 다음 내 턴 시작).</summary>
        public int EndDecay;
        public bool EndClear;
        /// <summary>순환 — 최대(cap)를 넘으면 1 부터 다시(달 위상 · 단계 순환). cap 이 있어야 한다.</summary>
        public bool Wrap;
        /// <summary>단계 이름(태세 · 형상) — 값 1 부터 차례로. 있으면 글이 「「형상」이 2면」 대신 「「형상」이 꿈결이면」 으로 쓴다. 개수는 cap 과 같아야 한다.</summary>
        public List<string> Stages;
        /// <summary>소환물 — 겹이 있으면 적의 공격 한 대를 대신 받고 1 사라진다(파티는 그 대를 안 맞는다).</summary>
        public bool Guard;
        /// <summary>적에게 거는 표식 — 걸린 적을 치는 아군 카드는 약점 공격이 된다(친 카드 한 장에 1 준다).</summary>
        public bool Weakens;
        /// <summary>guard(소환물)가 한 대를 다 막지 않고 이만큼만 깎는다(0.2 = -20%).</summary>
        public double Cut;
        /// <summary>(시범 16) guard(소환물) 하나가 사라지기 전에 받는 대 수(기본 1) — 모모 분신은 셋을 맞아야 깨진다.</summary>
        public int Uses;
        /// <summary>겹이 있는 동안 그 사도의 카드(tagType 종류 — 없으면 공격)에 이 태그가 붙는다(약점 공격 · 신속 …).</summary>
        public string TagWhile, TagType;
        /// <summary>겹이 0 이면 그 사도의 공격 카드가 적 1명 대신 적 전체를 친다.</summary>
        public bool Spread;
        /// <summary>최대(cap)에 닿으면 그 사도가 변신한다(carrier self) — 고학년이 아닌 계기의 변신.</summary>
        public KwOnMax OnMax;
        /// <summary>1개당 …</summary>
        public List<PerStat> Per = new();
        /// <summary>(엘다인 3차) 엔진이 세는 수 — 규칙이 아니다(자동 규칙 수에 안 든다): play(그 사도가 카드를 내면 +1) · discardBy(그 사도 효과로 카드가 버려지면 장마다 +1).</summary>
        public List<string> Tally;
        /// <summary>(2026-10-13 시온 「이 몸, 등장★」) tally 를 이 카드 id · 태그 카드에만 — 그때 tally 는 play · discard · exhaust(누구의 효과든 그 카드를 내거나 · 버려지거나 · 소멸하면 +1).</summary>
        public string TallyTag;
        /// <summary>(2026-10-13 시온 「탄창」) 손 따라가기 — 손에 쥔 이 카드 id · 태그의 장수가 곧 겹(cap 까지, 없으면 손 한도). 엔진이 맞춘다 — 규칙이 아니다 · carrier self 에만.</summary>
        public string Hand;
        /// <summary>(▶E69 분산) 아군 표시(carrier hero) — 그 표시가 붙은 아군을 노린 적의 공격 피해 -share, 깎인 양만큼 파티 실드.</summary>
        public double Share;
        /// <summary>(▶E73) 순환 칸에 닿을 때 그 칸의 효과(1번째 칸부터) — 규칙 하나로 센다.</summary>
        public List<List<Fx>> OnStage;
        /// <summary>(▶E75) 지금 단계마다 그 사도 카드 덤(변신 bonus 꼴 + stage).</summary>
        public List<StageBonus> StageBonus;
        /// <summary>(엘다인 3차) 전투 시작 값 — 규칙이 아니다(E62 규칙 수에 안 든다). 순환이면 단계(1부터).</summary>
        public int? Start;
        /// <summary>(2026-10-13 시온 — 초반 보강) 전투 시작에 이 카드를 startMakeN 장(기본 1) 손에 만든다(주인은 그 사도). 규칙이 아니다 · carrier self.</summary>
        public string StartMake;
        public int StartMakeN;
        /// <summary>(엘다인 3차) 이 이름의 카드 값 — grow handEnd(그 사도 고유 카드가 턴 끝 손에 있으면 +1) · reset play(그 사도 카드를 낸 뒤 0). 규칙이 아니다.</summary>
        public CardValueDef CardValue;
        /// <summary>규칙 문장(「X」가 N개가 되면 · 사라지면 · 다 닳으면 …).</summary>
        public List<PassiveRule> Rules = new();

        [Newtonsoft.Json.JsonIgnore] public int? CapOrMode => Mode ? 1 : Cap;
        [Newtonsoft.Json.JsonIgnore] public bool Consumes => ConsumeAll || Consume > 0;
        [Newtonsoft.Json.JsonIgnore] public bool Decays => DecayAll || Decay > 0;
    }

    /// <summary>카드 값 키워드 — grow: handEnd · reset: play.</summary>
    /// <summary>
    /// 카드 값 키워드 — grow(handEnd: 턴 끝에 손에 있으면 +1 · allyPlay: 아군이 카드를 낼 때마다 손의 그 사도 고유 카드 +1) · reset(play: 낸 뒤 0).
    /// (엘다인 4차 리뉴아 「시간」) cost — 값 1당 그 카드 비용 증감(-1) · max — 값의 최대 · who — allyPlay 의 「아군」(any 자신 포함 · other 자신 빼고, 기본 any).
    /// </summary>
    /// <summary>(엘다인 4차) 표시가 탈 때 — on: play(붙은 동료가 자기 카드를 내면) · shield: 키워드 주인 방어력 × 이 비율 실드.</summary>
    public sealed class KwBurn { public string On = "play"; public double Shield; /// <summary>(2026-10-11 벨라 힐러 겸 탱커) 1 탈 때 파티 회복 — 주인 방어력 기준 비율.</summary>
        public double Heal; }

    public sealed class CardValueDef { public string Grow, Reset, Who; public int Cost, Max; }

    /// <summary>(▶E75) 단계 카드 덤 — stage(1부터)일 때 거르개(card · type · unique · tag)에 맞는 그 사도 카드에 ratio(피해 배율) · fx(덧붙임) · tags.</summary>
    public sealed class StageBonus
    {
        public int Stage;
        public string Card, Type, Tag;
        public bool Unique;
        public double Ratio;
        public List<Fx> Fx = new();
        public List<string> Tags = new();
    }

    /// <summary>
    /// 「최대에 닿으면 변신」 — form(그 사도의 변신 id) · consume(닿은 겹을 다 쓴다 — 기본 false: 겹은 남고 변신의 off 가 치운다).
    /// 이미 그 변신 중이면 아무 일 없다(고학년과 달리 갱신하지 않는다 — 꽉 찬 자원이 다시 차오를 때마다 갱신되면 변신이 끝나지 않는다).
    /// </summary>
    public sealed class KwOnMax
    {
        public string Form;
        public bool Consume;
        /// <summary>
        /// (2026-10-08 1단계) 다 차면 저절로 터지는 대신 — make: 그 카드(시그니처 · 생성물 id)를 n 장(기본 1) 손에 만든다(주인은 그 사도) ·
        /// empower: 「next」(그 사도의 다음 카드) · 「any」(파티의 다음 카드) 의 피해 · 실드 · 회복을 ratio 만큼(+50% = 0.5). form · make · empower 가운데 하나.
        /// </summary>
        public string Make, Empower;
        public int N;
        public double Ratio;
    }

    /// <summary>「1개당 …」 — stat: dealt · taken · atk · def · crit(v 는 비율) · dot(턴 끝 공격력 ratio 피해) · hot(턴 끝 방어력 ratio 회복).</summary>
    public sealed class PerStat
    {
        public string Stat;
        public double V;
        public double Ratio;
        /// <summary>holder(든 사람) · allies(아군 전원).</summary>
        public string Who = "holder";
        /// <summary>(적 표식) owner — 그 표식을 건 사도가 칠 때만 든다.</summary>
        public string From;
        /// <summary>(from owner) 그 사도가 표식이 없는 적을 칠 때의 덤(음수면 깎임).</summary>
        public double Else;
        /// <summary>who marked(엘다인 4차 ▶E81) — 이 아군 표시(carrier hero)가 붙은 아군에게만.</summary>
        public string Mark;
    }

    /// <summary>패시브 규칙 한 줄 — 언제 · 조건 · 효과 · 횟수 제한.</summary>
    public sealed class PassiveRule
    {
        public string Name;
        public When When = new When { On = "always" };
        public List<Cond> Conds = new();
        public Limit Limit;
        public List<Fx> Fx = new();
        /// <summary>고유 능력(keyword) 규칙이어도 패시브처럼 그 사도 자신의 계기로 돈다 — 패시브를 고유 능력으로 합칠 때(2026-10-10), 아군에 붙는(ally) 고유 효과 아래로 옮긴 규칙이 「아군 누구든」 으로 바뀌지 않게.</summary>
        public bool Self;
    }

    /// <summary>
    /// 언제(docs/18 §7). on: fightStart · turnStart · turnEnd · play · guard · break · kill · hurt · lowHp · rush · ult · debuff ·
    /// overheal · stackReach · stackGone · reserveGone · switch · rhythm · always.
    /// 18갈래 공용 계기(2026-10-08): tally(셈이 맞으면) · keepAp(AP 를 남기고 턴 끝, n = 남긴 AP 이상) · reserveFire(예약이 터지면) ·
    /// grow(카드가 자라면, cardSt 로 값 이름 거르기) · fightEnd(전투에서 이기기 직전). exhaust 는 type(카드 종류) · who other 도 거른다.
    /// 1단계(2026-10-08): keepAp 은 보존 카드를 쥐고 턴을 마쳐도 돈다(kind ap · keep 으로 한쪽만) · overheal pct(회복 뒤 HP 그 비율 이상이면, 선을 넘은 몫) ·
    /// guardSum(내 턴이 시작될 때 지난 판에 막아 낸 양).
    /// </summary>
    public sealed class When
    {
        public string On;
        /// <summary>play — 카드 종류만.</summary>
        public string Type;
        /// <summary>play — true: 생성 카드(token)를 낼 때만(2026-10-10 빌드 피드백 — 생성 소비자) · false: 생성 카드를 빼고(엘다인 ▶E25).</summary>
        public bool? Token;
        /// <summary>(엘다인 ▶E7) discard · exhaust — 버리게 한(지운) 사도: other(다른 아군의 효과) · self(자신의 효과). 카드 주인은 상관없다.</summary>
        public string By;
        /// <summary>(엘다인 ▶E10) 한 번의 일(카드 한 장 · 패시브 한 번)에 한 번만.</summary>
        public bool Once;
        /// <summary>(▶E64) hit — 맞은 적에게 이 고유 효과(적 표식)가 있을 때만.</summary>
        public string FoeStack;
        /// <summary>play — N장마다.</summary>
        public int Every;
        /// <summary>play every — 장수를 턴마다 다시 센다.</summary>
        public bool PerTurn;
        /// <summary>play — 적힌 코스트 N 이상인 카드만(every 는 기본 1).</summary>
        public int? MinCost;
        /// <summary>play — 이번 턴 N장째.</summary>
        public int Nth;
        /// <summary>play — 누구의 카드든(any). 없으면 그 사도의 카드만.</summary>
        public string Who;
        /// <summary>play — 시그니처 카드만.</summary>
        public bool Sig;
        /// <summary>play — 「이번 턴 공격 · 스킬 · 강화 카드를 차례로 내면」.</summary>
        public List<string> Seq;
        /// <summary>guard — block · shield (없으면 둘 다).</summary>
        public string Kind;
        /// <summary>break · kill — 이 사도가 한 것만.</summary>
        public bool Mine;
        /// <summary>lowHp — 비율(0.3).</summary>
        public double Pct;
        /// <summary>stackReach · stackGone — 키워드 이름.</summary>
        public string Id;
        /// <summary>stackReach N · rhythm N.</summary>
        public int N;
        /// <summary>stackGone — 다 닳았을 때만(적의 차례 · 재촉).</summary>
        public bool Decay;
        /// <summary>debuff — 그 적에게 없던 가짓수가 새로 붙을 때만.</summary>
        public bool Fresh;
        /// <summary>play — 바로 앞 카드와 성격이 같을 때만(카제나 「연속」, 2026-10-10 계기 넓히기).</summary>
        public bool Chain;
        /// <summary>play — 바로 앞 카드와 같은 사도의 카드일 때만(같은 사도 잇달아).</summary>
        public bool SameHero;
        /// <summary>play — 바로 앞에 낸 카드와 같은 카드일 때만(같은 카드 잇달아).</summary>
        public bool Repeat;
        /// <summary>hurt — 방어 · 실드가 다 막은 공격에도(「공격을 받으면」).</summary>
        public bool Guarded;
        /// <summary>play — 이 태그가 붙은 카드만(「탄환」 · 「열정」 · 「신속」 …).</summary>
        public string Tag;
        /// <summary>play — 적힌 코스트 N 이하인 카드만.</summary>
        public int? MaxCost;
        /// <summary>(엘다인 4차 리뉴아) play — 낼 때의 비용(줄어든 값 · 할인 반영)이 이 값 이하인 카드만. maxCost 는 카드에 적힌 비용.</summary>
        public int? PaidMax;
        /// <summary>play · drawn — 이 사도 표시(carrier hero 키워드)가 붙은 사도의 카드만(「지정 아군이 카드를 내면」).</summary>
        public string Marked;
        /// <summary>hit — 약점으로 들어갔을 때만 · drawn — 고유 카드만 · exhaust — 시작 카드만.</summary>
        public bool Weak, Unique, Basic;
        /// <summary>play — 이 카드 값(cardStatus 이름)이 붙은 카드를 내면.</summary>
        public string CardSt;
    }

    /// <summary>
    /// 조건. c: stack(id, n, not) · hp(pct 이하) · hpMin(pct 이상) · status(id, n — 파티 · 자신 상태) · foes(n 이상) · foesMax(n 이하) ·
    /// playedMin · playedMax(n) · ownNone · apLeft(n) · gauge(n) · guarded(kind) · rushed · hurtLast · killedLast · firstTurn · targetBroken.
    /// </summary>
    public sealed class Cond
    {
        public string C;
        public string Id;
        public int N;
        public bool Not;
        public double Pct;
        public string Kind;
        /// <summary>stack — 위 끝(n 이상 max 이하). ownNone — 카드 종류(type).</summary>
        public int Max;
        public string Type;
    }

    /// <summary>횟수 제한 — per: turn · fight.</summary>
    public sealed class Limit
    {
        public string Per = "turn";
        public int N = 1;
    }

    /// <summary>적.</summary>
    public sealed class EnemyDef
    {
        public string Id;
        public string Name;
        public int Hp;
        public string Row = "front";
        public string Nature;
        /// <summary>사도 클론 — 그 사도 키. 클론은 판의 적 속성에 맞추지 않고 그 사도의 성격 그대로(사용자 2026-10-05).</summary>
        public string Clone;
        /// <summary>약점 성격 — 없으면 상성에서.</summary>
        public List<string> Weak;
        /// <summary>강인도 칸 — 0 이면 보통 · 엘리트 · 보스 기본(R.TOUGH).</summary>
        public double Tough;
        public bool Boss;
        /// <summary>cycle(적힌 순서) · shuffle(무작위, 같은 종류 세 번 잇지 않는다 — w 무게).</summary>
        public string Pick = "cycle";
        /// <summary>첫 턴의 수.</summary>
        public Intent Open;
        public List<Intent> Intents = new();
        public Phase Phase;
        public Phase Phase2;
        /// <summary>즉시 행동 장수(수에 없으면 이것, 이것도 없으면 값어치로).</summary>
        public int? Rush;
        public List<EnemyPassive> Passives = new();
        /// <summary>그림 — 엔진은 안 본다(화면 몫).</summary>
        public EnemyArt Art;
        public string Blurb;
        /// <summary>쌓이는 수치(분노 · 허기 · 비행 …) — 이름은 적 데이터가 자유롭게 짓는다(§5 틀).</summary>
        public List<CounterDef> Counters = new();
        /// <summary>희귀종 덧붙임(엘리트) — RareDef.Id 목록은 R.RARES.</summary>
        public List<RareDef> Rare = new();
        /// <summary>자기를 세운 적(소환자)이 쓰러지면 같이 쓰러진다.</summary>
        public bool Tied;
        /// <summary>영혼 공유 — 영혼 공유 적 가운데 맨 왼쪽(Idx 가 가장 작은 산 것)이 아니면 피해 0.</summary>
        public bool Soul;
        /// <summary>받는 강인도 피해 배율(0 이면 1) — 0.8 = 20% 덜.</summary>
        public double ToughTaken;
    }

    /// <summary>
    /// 쌓이는 수치 하나(적 몸) — 1개당 주는/받는 피해, 맞을 때 · 카드 낼 때 · 턴 시작 · 턴 끝에 변화, 문턱(at)에서 행동.
    /// 겹은 적의 Status[name] 에 있다(화면이 그대로 보인다).
    /// </summary>
    public sealed class CounterDef
    {
        public string Name;
        /// <summary>처음 겹(전투 시작 · 턴 시작 초기화의 값).</summary>
        public int Start;
        public int Max = 99;
        /// <summary>1개당 주는 피해 · 받는 피해(비율, 0.1 = +10%).</summary>
        public double Dealt, Taken;
        /// <summary>겹이 있으면 받는 피해가 이 값으로(과보호 — 1).</summary>
        public int Flat;
        /// <summary>맞을 때마다 · 파티가 카드를 낼 때마다 · 턴 시작에 · 턴 끝에 더하는 수(음수면 준다).</summary>
        public int OnHit, OnCard, OnTurnStart, OnTurnEnd;
        /// <summary>onCard 의 카드 종류 — 「공격」 · 「!공격」(공격이 아닌 카드).</summary>
        public string CardType;
        /// <summary>onCard — 이 적이 이번 턴 이미 행동했을 때만(「행동 후 카드를 내면 쌓인다」).</summary>
        public bool AfterAct;
        /// <summary>턴 시작에 start 로 되돌린다 · 턴 끝에 모두 지운다.</summary>
        public bool ResetTurnStart, ClearTurnEnd;
        /// <summary>문턱 — 겹이 at 이상이 되면 act 를 한다.</summary>
        public int At;
        public Intent Act;
        /// <summary>now(즉시 행동 — 기본) · next(다음 차례의 수가 된다 — 「1턴 뒤 강공」) · replace(지금 예고한 수를 바꾼다).</summary>
        public string Mode;
        /// <summary>문턱을 넘겨도 겹을 그대로 둔다(기본은 start 로 되돌린다).</summary>
        public bool Keep;
        /// <summary>0 이 되면 기절(비행).</summary>
        public bool StunAtZero;
        /// <summary>자기가 치는 수(공격 · 관통 · 연타 · 전체)를 한 뒤 0 으로 — 모았다가 한 방에 쏟는 「급소 노림」(2026-10-07).</summary>
        public bool ClearOnAttack;
        public string Desc;
    }

    /// <summary>희귀종 덧붙임 — id: poisonHand · reshuffle(card) · anxietyHits(card) · autoCard · costUp · crystal · actDebuff(st) · toughGuard.</summary>
    public sealed class RareDef
    {
        public string Id;
        /// <summary>reshuffle · anxietyHits 의 상태 카드 id.</summary>
        public string Card;
        /// <summary>actDebuff 의 상태(취약 · 약화).</summary>
        public string St;
    }

    /// <summary>적 그림 — 스파인 · 스킨 · 아이콘 · 배율(화면이 읽는다).</summary>
    public sealed class EnemyArt
    {
        public string Spine, Skin, Icon;
        public double Scale;
    }

    /// <summary>조건부 수 — 서지 않으면 그 수는 건너뛴다(cycle 은 다음 수, shuffle 은 후보에서 뺀다).</summary>
    public sealed class IntentIf
    {
        /// <summary>살아 있는 적(자신 포함)이 n 이상이면.</summary>
        public int? Allies;
        /// <summary>파티에 방어 · 실드가 있으면(true) · 없으면(false).</summary>
        public bool? PartyBlock;
        /// <summary>자기에게 방어 · 실드가 있으면(true) · 없으면(false).</summary>
        public bool? SelfBlock;
        /// <summary>자기 쌓이는 수치 counter 가 n 이상이면.</summary>
        public string Counter;
        public int N;
    }

    public sealed class Phase
    {
        public double At;
        public string Say;
        public List<Intent> Intents = new();
    }

    /// <summary>
    /// 적의 수. t: attack · back · attackAll · multi · charge · block · guard · heal · buff · debuff · jam · addCard (패시브만: thorns · selfHeal).
    /// </summary>
    public sealed class Intent
    {
        public string T;
        public int V;
        /// <summary>multi 횟수 · 상태 겹(attack · attackAll) · addCard 장수.</summary>
        public int N;
        /// <summary>multi — 한 대마다 거는 상태 겹(기본 1).</summary>
        public int Per;
        /// <summary>건다(attack · multi · attackAll · buff · debuff) · 끼워 넣을 카드 id(addCard).</summary>
        public string Id;
        public string Say;
        /// <summary>shuffle 무게(기본 1).</summary>
        public int W;
        public int? Rush;
        /// <summary>charge — 다음 턴에 반드시 할 수.</summary>
        public Intent Next;
        /// <summary>격파되면 이 수가 끊긴다.</summary>
        public bool Brk;
        /// <summary>그 수와 함께 강인도를 되찾는다.</summary>
        public double Tough;
        /// <summary>buff — 적 전체.</summary>
        public bool All;
        /// <summary>addCard — draw · discard · hand.</summary>
        public string To;
        /// <summary>조건부 수.</summary>
        public IntentIf If;
        /// <summary>summon — 같은 적이 이만큼 살아 있으면 더 세우지 않는다(0 = 제한 없음).</summary>
        public int Max;
        /// <summary>summon — 세운 소환물은 강인도가 없다(최대 0 — 격파되지 않고 화면은 강인도 막대를 숨긴다). 강인도 최소치 3 의 유일한 예외(사용자 2026-10-05).</summary>
        public bool NoTough;
        /// <summary>ult(보스 클론의 고학년) — 차례로 하는 수들. 엔진이 사도 고학년에서 만든다(BossUlt) — 데이터에 쓰지 않는다.</summary>
        public List<Intent> Then;

        [Newtonsoft.Json.JsonIgnore] public int WOr1 => W <= 0 ? 1 : W;
    }

    /// <summary>
    /// 적 패시브 — on: fightStart · turnStart · turnEnd · hurt · lowHp(at) · allyDown · card(type · every) · rushed · debuffed · broken · recover.
    /// </summary>
    public sealed class EnemyPassive
    {
        public string Name;
        public string On;
        public double At;
        public string Type;
        public int Every;
        /// <summary>턴당 몇 번(기본 1, 0 은 제한 없음). fightStart · lowHp 는 한 번.</summary>
        public int? Limit;
        /// <summary>그 판에서만(0 · 1 · 2).</summary>
        public List<int> Phase;
        /// <summary>allyDown — 그 적(id)이 쓰러질 때만.</summary>
        public string Who;
        public Intent Do;
        /// <summary>card — 바로 앞 카드와 같은 사도의 카드를 잇달아 냈을 때만.</summary>
        public bool Same;
    }

    /// <summary>마을 — 두 층(바깥 · 안쪽). 2층 보스가 판의 끝.</summary>
    public sealed class VillageDef
    {
        public string Id;
        public string Name;
        public string Race;
        public string Line;
        public List<FloorDef> Floors = new();
    }

    public sealed class FloorDef
    {
        public string Name;
        public string Sub;
        /// <summary>땅 — 이벤트 풀이 이것으로 묶인다.</summary>
        public string Land;
        public Dictionary<string, string> Bg;
        /// <summary>세기(약 · 중 · 강)마다 싸움 짝 여럿.</summary>
        public List<List<List<string>>> Pools = new();
        public List<List<string>> Elites = new();
        public List<string> Boss = new();
        /// <summary>보스 데이터가 없는 층 — 보스 칸이 엘리트 몸(체력 ×1.5 · 강인도 +1)으로 선다.</summary>
        public bool BossElite;
    }

    /// <summary>이벤트.</summary>
    public sealed class EventDef
    {
        public string Id;
        public string Name;
        /// <summary>"공용" · 마을 id(그 마을 판에서만 — 2026-10-06 마을 이벤트) · 땅 이름(옛 꼴 — 그 층의 land).</summary>
        public string Pool = "공용";
        /// <summary>나오는 층(1 · 2). 0 = 두 층 모두. 마을 이벤트가 층마다 보상 · 위험을 달리할 때.</summary>
        public int Floor;
        public string Npc;
        public string Scene;
        /// <summary>드문 이벤트 — 굴릴 때마다 이 확률로만 후보에 든다.</summary>
        public double Rare;
        /// <summary>연속 이벤트 — 이 깃발(앞 이벤트 결과 `flag`)이 서 있어야 뽑힌다.</summary>
        public string NeedFlag;
        /// <summary>줄기 뒤 이벤트 — 이 이벤트가 읽는 깃발(needFlag · 선택지 flag)이 서 있으면 뽑힐 무게 × 이 값. 0 이하 = 기본(R.EVENT_FLAG_WEIGHT = 3).</summary>
        public double FlagWeight;
        public List<EventOption> Options = new();
        public string Leave;
        public List<Outcome> LeaveOut = new();
    }

    public sealed class EventOption
    {
        public string Label;
        public List<Outcome> Out = new();
        public string Say;
        /// <summary>
        /// 결과 글(say · gamble say · judge passSay)의 따옴표 대사마다 말하는 이 — 순서대로. null · 빈 칸 = 장면 npc(기본),
        /// "hero" = 이 선택지를 연 파티 사도(hero · price.hero 가운데 파티에 있는 사도), 그 밖 = 그 이름(사도 이름 · 「경비대」 따위).
        /// </summary>
        public List<string> By;
        /// <summary>파티에 이 사도(들 가운데 하나)가 있어야 보인다.</summary>
        public List<string> Hero;
        public string Race;
        /// <summary>"hp30" — 파티 HP 30% 이하일 때만.</summary>
        public string When;
        /// <summary>연속 이벤트 — 이 깃발이 서 있어야 보인다(앞 이벤트 결과 `{ "k": "flag", "id" }`).</summary>
        public string Flag;
        /// <summary>이 깃발이 서 있으면 안 보인다.</summary>
        public string NoFlag;
        /// <summary>
        /// 조건(hero · race · when · flag · noFlag)이 안 맞을 때 — null(기본): hero · race · when 은 잠긴 칸으로 보이고(조건 글과 함께), flag · noFlag 는 숨긴다.
        /// true = 깃발 조건도 잠금으로 보인다 · false = 사도 조건도 숨긴다.
        /// </summary>
        public bool? ShowLocked;
        /// <summary>잠긴 칸에 보일 조건 글(없으면 엔진이 짓는다 — 「루포 · 티그 가운데 한 명이 있어야 합니다」).</summary>
        public string LockText;
        public List<Gamble> Gamble;
        /// <summary>gamble 의 결과를 골라서 받는다.</summary>
        public bool Choose;
        public Judge Judge;
        public EventFight Fight;
        /// <summary>그 사도가 있으면 결과가 바뀐다.</summary>
        public PriceAlt Price;
        public int NeedGold;
        /// <summary>gamble 의 확률을 화면에 안 보인다(엔진은 같다).</summary>
        public bool Hidden;
        /// <summary>엔진이 맨 끝에 붙이는 「떠난다」 — 데이터에 쓰지 않는다.</summary>
        [Newtonsoft.Json.JsonIgnore] public bool Leave;
    }

    public sealed class Gamble { public double P; public List<Outcome> Out = new(); public string Say; /// <summary>이 결과 글의 대사별 말하는 이(없으면 선택지의 by).</summary>
        public List<string> By; }
    public sealed class Judge { public string By; public int At; public List<Outcome> Pass = new(); public List<Outcome> Fail = new(); public string PassSay; }
    public sealed class EventFight
    {
        public string Name;
        public List<string> Enemies = new();
        /// <summary>땅마다 다른 적(공용 이벤트) — 있으면 Enemies 보다 앞선다.</summary>
        public Dictionary<string, List<string>> ByLand;
        public List<Outcome> Win = new();
        public bool Elite;
    }
    public sealed class PriceAlt { public string Hero; public List<Outcome> Out = new(); }

    /// <summary>
    /// 이벤트 결과 한 조각. k: none · gold(v) · hp(v 비율) · maxHp(v) · remove(n) · dupe(n) · unique · neutral(grade?) · equip(grade, slot?) ·
    /// flash(all · swap) · shin(v 확률) · noShin · shinNow · shinPick(n, kind?, random?) · curse(id) · gift(id) · flag(id — 연속 이벤트 깃발) · scout · shopGift(grade) · rewardFlash ·
    /// next(ap · gauge · hand · weak · hpCut · rush · foeVuln · quiet · buff).
    /// </summary>
    public sealed class Outcome
    {
        public string K;
        public double V;
        public int N;
        public string Grade;
        public string Slot;
        public string Id;
        public string Kind;
        public bool All;
        public bool Swap;
        public NextFight Next;
        /// <summary>remove — 시작(기본) 카드만 고를 수 있다.</summary>
        public bool Basic;
        /// <summary>shinPick — 축복 종류 · 신탁 번호를 무작위로(이벤트 글에 「무작위」 라고 적힌 것만 켠다). 끄면(기본) 카드를 고른 뒤 축복 → 신탁을 차례로 고른다(2026-10-13 사용자).</summary>
        public bool Random;
    }

    /// <summary>「다음 전투:」 효과 — 그 전투 하나에 한 번.</summary>
    public sealed class NextFight
    {
        public int Ap, Gauge, Hand, Weak, Rush, FoeVuln, Quiet;
        public double HpCut;
        public Dictionary<string, int> Buff;

        public NextFight Merge(NextFight o)
        {
            if (o == null) return this;
            Ap += o.Ap; Gauge += o.Gauge; Hand += o.Hand; Weak += o.Weak; Rush += o.Rush; FoeVuln += o.FoeVuln; Quiet += o.Quiet; HpCut += o.HpCut;
            if (o.Buff != null) { Buff ??= new(); foreach (var kv in o.Buff) Buff[kv.Key] = (Buff.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value; }
            return this;
        }
    }

    /// <summary>장비 — 사도당 무기 · 방어구 · 장신구 한 칸씩.</summary>
    public sealed class EquipDef
    {
        public string Id;
        public string Name;
        public string Grade;
        public string Slot;
        public Stats Stats = new();
        /// <summary>효과 — 낀 사도의 패시브로 돈다.</summary>
        public List<PassiveRule> Effect = new();
        /// <summary>애착 사도 id — 그 사도가 끼면 AffinityStats · AffinityEffect 가 더 붙는다.</summary>
        public string Affinity;
        public Stats AffinityStats;
        public List<PassiveRule> AffinityEffect = new();
        public string Blurb;
    }
}
