using System.Collections.Generic;

// 전투 규칙과 화면 사이에 오가는 자료. 규칙 쪽(CoreBattle — Bolzena.Core 어댑터)은 이것만 내보내고,
// 화면 쪽(BattleDirector)은 이것만 읽는다 — 둘을 갈아 끼워도 서로 모르게.
namespace Bolzena.Battle
{
    public enum Side { Party, Enemy }

    public enum CardType { Attack, Skill, Power, Status }

    public enum TargetKind { None, Enemy, AllEnemies, Self, Ally }

    // 몸짓 — 사도 SD 의 어느 동작을 쓰는가
    public enum Motion { None, Attack1, Attack2, Skill1, Ultimate }

    // 맞는 결 — 이펙트 · 소리를 고른다
    public enum HitKind { Slash, Blunt, Magic }

    public enum IntentKind { None, Attack, Defend, Heavy, Buff, Debuff }

    public struct UnitRef
    {
        public Side Side;
        public int Index;
        public UnitRef(Side side, int index) { Side = side; Index = index; }
        public static UnitRef Party(int i) => new UnitRef(Side.Party, i);
        public static UnitRef Enemy(int i) => new UnitRef(Side.Enemy, i);
        public override string ToString() => $"{Side}:{Index}";
    }

    // 낱말 풀이 한 줄 — 카드 툴팁 · 정보 창(kind: kw 키워드 · tag 태그 · status 상태 · flash 신탁 · no 못 냄)
    public class Term
    {
        public string Word, Text, Kind;   // Kind: kw · tag · status · flash · no · card(생성 카드 — Card 를 작은 카드로)
        public CardInfo Card;
        public string Detail;             // 사도 고유 효과의 자세히(CardText.Detail) — 판 아래 흐리게
        public Term(string word, string text, string kind = "kw") { Word = word; Text = text; Kind = kind; }
    }

    // 상태 칩 — 칩 하나 = 이름 · 값(겹 또는 %) · 남은 턴. Kind: buff · debuff · key(사도 키워드)
    public class StatusChip
    {
        public string Id;
        public string Value;            // 「2」 · 「+20%」
        public int Turns = -1;          // 남은 턴(-1 이면 없음 · 전투 내내)
        public string Kind = "buff";
        public string Text;             // 풀이(툴팁) — 짧은 글(고유 효과는 CardText.Short)
        public string Detail;           // 자세히(고유 효과 CardText.Detail — 툴팁 아래 · 펼침). 없으면 null
        /// <summary>이 상태를 건 사도(화면 키 — 초상). 엔진이 아직 안 내면 null — 정보 창은 초상 없이 그린다.</summary>
        public List<string> From;
    }

    // 카드 한 장의 모습 — 화면이 그리는 데 필요한 것
    public class CardInfo
    {
        public string Id;
        public string Name;
        public int Hero = -1;            // 파티 몇 번째 사도의 카드(-1 이면 교주 · 상태 카드)
        public string HeroName;
        public int Cost;
        public CardType Type;
        public string TypeName;          // 공격 · 스킬 · 강화 · 상태 · 저주
        public TargetKind Target;
        public string Text;              // 설명(효과에서 만든다 — TMP 서식)
        public string Art;               // Resources/Art 의 그림 이름
        public Motion Motion;
        public HitKind Hit;
        public bool Epiphany;            // 신탁이 걸린 빛나는 카드
        public string EpiphanyLabel;     // 신탁으로 바뀐 카드면 그 이름표
        public List<string> Tags = new List<string>();
        public List<Term> Terms = new List<Term>();   // 카드 글에 나오는 낱말 풀이
        public bool Unplayable;
        public bool Unique;              // 고유 카드(더미 보기 차례: 사도별 기본 → 고유)
        public int Owner = -1;           // 교주 카드를 넣은 사도(파티 몇 번째) — 틀 빛깔 · 핀이 그 사도. 주인이 없으면 -1(금빛 중립)
        public string Nature;            // 주인 사도의 성격(카드 틀 빛깔) — 교주 카드는 넣은 사도의 성격, 주인 없는 교주 · 상태 카드는 null
        public string BlessName, BlessText;   // 신탁 선택지에 축복이 얹혔으면(15%) 그 이름 · 글
        public List<string> Choices;     // 두 갈래 카드 — 갈래 이름 둘(낼 때 고른다). 없으면 null
    }

    // 낀 장비 한 칸 — 정보 창(전투원 탭)
    public class GearSlot
    {
        public string Slot;              // 무기 · 방어구 · 장신구
        public string Id, Name, Grade, Text;
    }

    public class HeroState
    {
        public string Key;               // 스파인 · 소리 폴더 이름(ricota · kyarot …) — 화면용
        public string Id;                // 규칙 쪽 id(rico …)
        public string Name;
        public string UltName, UltText;  // UltText = 자세히(효과 전부)
        public string UltShort;          // 고학년 한 줄 요약(CardText.Short) — 기본으로 보이는 글
        public UnityEngine.Color Tint;
        public int Atk, Def, Crit;       // 바탕(장비 포함)
        public int AtkNow, DefNow, CritNow;
        public string Role, Nature, Row, Blurb;
        public int Ult, UltMax;          // 게이지(파티 공용) · 이 사도 고학년 값
        public bool Dead;
        public List<string> Passives = new List<string>();   // 패시브 한 줄씩 — 자세히(CardText.Detail)
        public List<string> PassivesShort = new List<string>();   // 패시브 한 줄 요약 「이름 — 요약」(CardText.Short) — 기본
        public string KeywordName, KeywordText;   // KeywordText = 자세히(CardText.Detail)
        public string KeywordShort;               // 고유 효과 한 줄(CardText.Short) — 기본
        public int KeywordStacks;
        public List<StatusChip> Chips = new List<StatusChip>();
        public string Race;
        public List<GearSlot> Gear = new List<GearSlot>();
    }

    public class EnemyState
    {
        public string Key;               // 스파인 폴더 이름
        public string Id;
        public string Skin;
        public string Name;
        public bool Boss;
        public int Hp, MaxHp, Block;
        public float ToughV, ToughMaxV;  // 강인도 그대로(1/3 · 1/6 칸도 — core ToughView.Left · Max). ToughMaxV 0 = 강인도 없음(격파 안 됨)
        public bool Resting;             // 다음 차례를 쉰다(core ToughView.Resting)
        public bool Broken, Sealed;
        public bool Dead;
        public IntentKind Intent;
        public int IntentValue, IntentHits;
        public string IntentText, IntentSay;
        public int RushNeed, RushCnt;    // 즉시 행동 — 카드 RushNeed 장이면 당겨서 한다(0 이면 안 당겨짐)
        public bool RushedTurn;
        public string Nature, Blurb;
        public List<string> Weak = new List<string>();
        public List<string> Passives = new List<string>();
        public List<StatusChip> Chips = new List<StatusChip>();
    }

    public class BattleSnapshot
    {
        public int Turn, Wave, WaveCount;
        public int PartyHp, PartyMaxHp, PartyBlock;
        public int Ap, MaxAp;
        public int DrawCount, DiscardCount, GoneCount;
        public int Gauge, GaugeMax;
        public List<HeroState> Heroes = new List<HeroState>();
        public List<EnemyState> Enemies = new List<EnemyState>();
        public List<CardInfo> Hand = new List<CardInfo>();
        public List<CardInfo> DrawPile = new List<CardInfo>(), DiscardPile = new List<CardInfo>(), GonePile = new List<CardInfo>();
        public List<StatusChip> PartyChips = new List<StatusChip>();
        public bool Over, Won;
    }

    // 미리보기 — 카드(고학년)를 그 대상에게 내면
    public class PreviewFoe { public int Hp, Guard; public bool Kill, Max, Break; public float ToughV; }
    public class PreviewParty { public int Heal, Block, Lose, Over; }

    public enum EventKind
    {
        WaveStart,      // Value = 웨이브 번호, Boss = 보스 웨이브인가
        TurnStart,      // Value = 턴
        Draw,           // Card
        Discard,        // Card(손 → 버린 더미)
        Exhaust,        // Card(손 → 사라짐) · Text = 쪽지 Label(forget · remove · bond …)
        CardChanged,    // Card(손 안에서 바뀜 — evolve · transform · bond) · Text = Label
        ApChanged,      // Value = 지금 AP
        CardPlayed,     // Card · Actor(사도)
        EpiphanyApplied,// Card(바뀐 카드) · Text(원래 카드 id)
        Act,            // Actor 가 Motion 으로 움직인다(그 뒤의 일들이 이 몸짓의 결과). Text: ult · attack · heavy · defend, Say: 적의 말
        Damage,         // Actor → Target, Value 피해, Crit, Hit(몇 번째), Hits(모두 몇 번), HpAfter, Blocked
        Block,          // Target 방어 · 실드 얻음(Value), BlockAfter
        Heal,           // Target(파티) 회복 Value, HpAfter
        Status,         // Target 에 Text(취약 · 약화 · 「반격!」 …) — Up 이면 좋은 일
        Toughness,      // Target 강인도 Value 깎임(남은 것 HpAfter 자리에)
        Break,          // Target 격파
        Recover,        // Target 격파에서 일어남
        Death,          // Target 쓰러짐
        UltGauge,       // Actor(사도) 게이지 Value
        UltReady,       // Actor(사도)
        Intent,         // Target(적) 예고가 바뀜
        PartyHurt,      // 파티가 맞음 — Target(맞는 사도) Value 피해 · Blocked
        Form,           // 변신 — Actor(사도) · Text on/off · Say 변신 이름 · Anim 변신 쉬는 동작(Idle_DreamForm …) · Value 턴(0 = 전투 끝까지) · off 면 Anim 에 까닭
        Talk,           // Actor(사도) 대사 때 — Text(start · hit · kill · win · down · ego)
        Victory,
        Defeat,
    }

    public class BattleEvent
    {
        public EventKind Kind;
        public UnitRef Actor, Target;
        public int Value, HpAfter, BlockAfter, Blocked;
        public float FAfter;             // Toughness · Recover — 남은 강인도 그대로(1/3 칸도)
        public int Hit, Hits = 1;
        public bool Crit, Boss, Up;
        public Motion Motion;
        public HitKind HitKind;
        public CardInfo Card;
        public string Text, Say;
        public string Anim;              // Form — 변신 쉬는 동작 이름
        public override string ToString() => $"{Kind} {Actor}->{Target} v={Value} hit={Hit}/{Hits} crit={Crit} {Text}";
    }
}
