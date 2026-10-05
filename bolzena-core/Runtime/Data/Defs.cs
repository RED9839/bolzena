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
            Pull = "pull", ExileFrom = "exileFrom", Dispel = "dispel", MoveRow = "moveRow", GrowRun = "growRun";
        // v2 콘텐츠 요구(2026-10-05 넷째) — 배타 무작위 · 조건 · 다시 내기 · 비용 증감 · 태그 덧붙임 · 한 대 깎기 · 빚 탕감
        public const string Roll = "roll", IfRoll = "ifRoll", IfPrevSame = "ifPrevSame", IfInHand = "ifInHand", IfBond = "ifBond", IfLastMine = "ifLastMine",
            IfPulled = "ifPulled", IfShield = "ifShield", IfDebt = "ifDebt", IfTypeNew = "ifTypeNew",
            Recast = "recast", CostMod = "costMod", AddTag = "addTag", CutHit = "cutHit", ClearDebt = "clearDebt";

        public static readonly HashSet<string> Conditions = new() { IfBroken, IfTune, IfChain, IfLink, IfPrev, IfRhythm, IfSwitched, IfStack, When,
            IfRepeat, IfHeld, IfPlayedMax, IfApLeft, IfSpent, IfBalanced, IfHunted, IfDebuffs, IfHp, IfKill, IfBreak, IfWounded,
            IfChoice, IfRandom, IfHand, IfPile, IfNth, IfStreak, IfAllHeroes, IfFoe, IfCardSt,
            IfRoll, IfPrevSame, IfInHand, IfBond, IfLastMine, IfPulled, IfShield, IfDebt, IfTypeNew };
        /// <summary>「… 1개당」 — 바로 뒤 피해 · 방어 · 실드 · 회복 한 줄을 그 수만큼.</summary>
        public static readonly HashSet<string> Pers = new() { PerStack, PerRhythm, PerDiscarded, PerPaid, PerDebuff, PerApLeft, PerTag, PerPlayed, PerPile, PerCardSt, PerEvent };
        /// <summary>카드의 때 붙은 마디(when on) — 영감(능력으로 뽑힘) · 감응(뽑힘) · 안식(버려짐) · 턴 끝에 손에 · 소각(소멸할 때) · 열정(손에 있을 때 열정 카드가 나가면).</summary>
        public static readonly string[] WHEN_ON = { "draw", "drawAny", "discard", "handEnd", "burn", "passion" };
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
            Later, AfterCards, Trap, Confuse, AutoPlay, CastOther, Pull, ExileFrom, Dispel, MoveRow, GrowRun,
            Roll, IfRoll, IfPrevSame, IfInHand, IfBond, IfLastMine, IfPulled, IfShield, IfDebt, IfTypeNew, Recast, CostMod, AddTag, CutHit, ClearDebt, HealMod,
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
        /// <summary>봉인된 금기 — 보스를 처치하면 덱에서 이 카드(금기 카드)로 바뀐다.</summary>
        public string Becomes;
        /// <summary>비용을 AP 대신 이 키워드(사도 고유 효과의 겹)로 치른다 — 「시간 초월로 비용 지불」 꼴.</summary>
        public string PayWith;
        /// <summary>AP 가 모자라도 낸다 — 모자란 만큼 다음 턴 AP 가 준다(AP 빚).</summary>
        public bool Debt;
        /// <summary>payWith — AP 1 을 고유 효과 몇 개로(기본 1) · 모자라면 AP 로 섞어 치른다.</summary>
        public int PayRate;
        public bool PayMix;
        /// <summary>두 갈래로 내기 — 갈래 이름(둘). 낼 때 PlayOpts.Choice(1 · 2)로 고르고 효과는 ifChoice n 으로 가른다.</summary>
        public List<string> Choices;

        [Newtonsoft.Json.JsonIgnore] public bool Neutral => Hero == null && (Type == "공격" || Type == "스킬" || Type == "강화") && !Gift && !Token;
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
        public string Row = "mid";
        public string Role;
        /// <summary>옛 — 운영 방식(docs/19, 2026-10-05 틀 폐기). 엔진은 안 본다. 새 콘텐츠는 쓰지 않는다.</summary>
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
        /// <summary>내 턴이 끝나면 이만큼 준다 · 전부 사라진다(decay 는 적의 차례가 끝난 뒤 = 다음 내 턴 시작).</summary>
        public int EndDecay;
        public bool EndClear;
        /// <summary>순환 — 최대(cap)를 넘으면 1 부터 다시(달 위상 · 단계 순환). cap 이 있어야 한다.</summary>
        public bool Wrap;
        /// <summary>소환물 — 겹이 있으면 적의 공격 한 대를 대신 받고 1 사라진다(파티는 그 대를 안 맞는다).</summary>
        public bool Guard;
        /// <summary>적에게 거는 표식 — 걸린 적을 치는 아군 카드는 약점 공격이 된다(친 카드 한 장에 1 준다).</summary>
        public bool Weakens;
        /// <summary>guard(소환물)가 한 대를 다 막지 않고 이만큼만 깎는다(0.2 = -20%).</summary>
        public double Cut;
        /// <summary>겹이 있는 동안 그 사도의 카드(tagType 종류 — 없으면 공격)에 이 태그가 붙는다(약점 공격 · 신속 …).</summary>
        public string TagWhile, TagType;
        /// <summary>겹이 0 이면 그 사도의 공격 카드가 적 1명 대신 적 전체를 친다.</summary>
        public bool Spread;
        /// <summary>1개당 …</summary>
        public List<PerStat> Per = new();
        /// <summary>규칙 문장(「X」가 N개가 되면 · 사라지면 · 다 닳으면 …).</summary>
        public List<PassiveRule> Rules = new();

        [Newtonsoft.Json.JsonIgnore] public int? CapOrMode => Mode ? 1 : Cap;
        [Newtonsoft.Json.JsonIgnore] public bool Consumes => ConsumeAll || Consume > 0;
        [Newtonsoft.Json.JsonIgnore] public bool Decays => DecayAll || Decay > 0;
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
    }

    /// <summary>패시브 규칙 한 줄 — 언제 · 조건 · 효과 · 횟수 제한.</summary>
    public sealed class PassiveRule
    {
        public string Name;
        public When When = new When { On = "always" };
        public List<Cond> Conds = new();
        public Limit Limit;
        public List<Fx> Fx = new();
    }

    /// <summary>
    /// 언제(docs/18 §7). on: fightStart · turnStart · turnEnd · play · guard · break · kill · hurt · lowHp · rush · ult · debuff ·
    /// overheal · stackReach · stackGone · reserveGone · switch · rhythm · always.
    /// </summary>
    public sealed class When
    {
        public string On;
        /// <summary>play — 카드 종류만.</summary>
        public string Type;
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
        /// <summary>play — 바로 앞에 낸 카드와 같은 카드일 때만(같은 카드 잇달아).</summary>
        public bool Repeat;
        /// <summary>hurt — 방어 · 실드가 다 막은 공격에도(「공격을 받으면」).</summary>
        public bool Guarded;
        /// <summary>play — 이 태그가 붙은 카드만(「탄환」 · 「열정」 · 「신속」 …).</summary>
        public string Tag;
        /// <summary>play — 적힌 코스트 N 이하인 카드만.</summary>
        public int? MaxCost;
        /// <summary>play · drawn — 이 사도 표시(carrier hero 키워드)가 붙은 사도의 카드만(「지정 아군이 카드를 내면」).</summary>
        public string Marked;
        /// <summary>hit — 약점으로 들어갔을 때만 · drawn — 고유 카드만 · exhaust — 시작 카드만.</summary>
        public bool Weak, Unique, Basic;
        /// <summary>play — 이 카드 값(cardStatus 이름)이 붙은 카드를 내면.</summary>
        public string CardSt;
    }

    /// <summary>
    /// 조건. c: stack(id, n, not) · hp(pct 이하) · hpMin(pct 이상) · status(id, n — 파티 · 자신 상태) · foes(n 이상) · foesMax(n 이하) ·
    /// playedMin · playedMax(n) · ownNone · apLeft(n) · gauge(n) · guarded(kind) · rushed · hurtLast · killedLast · firstTurn · targetBroken · row(row).
    /// </summary>
    public sealed class Cond
    {
        public string C;
        public string Id;
        public int N;
        public bool Not;
        public double Pct;
        public string Kind;
        public string Row;
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
        /// <summary>"공용" 또는 땅 이름.</summary>
        public string Pool = "공용";
        public string Npc;
        public string Scene;
        /// <summary>드문 이벤트 — 굴릴 때마다 이 확률로만 후보에 든다.</summary>
        public double Rare;
        public List<EventOption> Options = new();
        public string Leave;
        public List<Outcome> LeaveOut = new();
    }

    public sealed class EventOption
    {
        public string Label;
        public List<Outcome> Out = new();
        public string Say;
        /// <summary>파티에 이 사도(들 가운데 하나)가 있어야 보인다.</summary>
        public List<string> Hero;
        public string Race;
        /// <summary>"hp30" — 파티 HP 30% 이하일 때만.</summary>
        public string When;
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

    public sealed class Gamble { public double P; public List<Outcome> Out = new(); public string Say; }
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
    /// flash(all · swap) · shin(v 확률) · noShin · shinNow · shinPick(n, kind?) · curse(id) · gift(id) · scout · shopGift(grade) · rewardFlash ·
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
