using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bolzena.Core
{
    /// <summary>
    /// 한 판의 상태 — 전투 바깥의 것 전부(편성 · 마을 · 층 · 덱 · 파티 HP · 골드 · 장비 · 지도 · 이벤트).
    /// 그대로 JSON 으로 저장한다(Run.Save · Run.Load). 난수는 RngState 로 이어진다 — 이어해도 같은 굴림을 다시 굴릴 수 없다.
    /// </summary>
    public sealed class RunState
    {
        public long Seed;
        public uint RngState;
        public string Village;
        /// <summary>판의 적 속성 — 새 판에 마을과 함께 무작위로 하나(공명 빼고). 이 판의 모든 적(일반 · 엘리트 · 보스 · 소환)의 성격 · 약점이 이것. null = 옛 저장(적 데이터 성격 그대로).</summary>
        public string EnemyNature;
        /// <summary>층마다 보스 줄(적 id) — 새 판에 적 속성에 맞춰 사도 클론을 고른다(Run.PickBosses). null = 옛 저장(마을 데이터 그대로).</summary>
        public List<List<string>> Bosses;
        public List<string> Party = new();
        public Dictionary<string, string> Rows = new();
        public int PartyHp, PartyMaxHp;
        public List<string> Deck = new();
        /// <summary>카드 id → 신탁 번호(1~5).</summary>
        public Dictionary<string, int> Flash = new();
        /// <summary>카드 id → 축복(공용 꼴 이름 또는 own · own1 · own2).</summary>
        public Dictionary<string, string> Shin = new();
        public int Gauge;
        public int Gold;
        /// <summary>사도 → 칸 → 장비 id.</summary>
        public Dictionary<string, Dictionary<string, string>> Gear = new();
        /// <summary>받았지만 끼기 · 팔기를 아직 정하지 않은 장비(가방이 아니다 — 화면이 곧장 묻는다).</summary>
        public List<string> Bag = new();
        /// <summary>그 가운데 상점에서 산 것 — 팔 수 없고 껴야 한다.</summary>
        public List<string> BagBought = new();
        /// <summary>0 · 1 — 마을의 1층(바깥) · 2층(안쪽).</summary>
        public int Floor;
        /// <summary>싸움 세기 0..2, 3 = 보스.</summary>
        public int Node;
        public bool Elite;
        public int Step;
        public string Done;
        public MapState Map;
        public RewardState Reward;
        public ShopState Shop;
        public HashSet<int> ShopSeen = new();
        public int Removals;
        public Dictionary<string, string> Stops = new();
        public CampState Camp;
        /// <summary>한 번 빼 버린 고유 카드 — 은총으로 다시 안 나온다.</summary>
        public List<string> Dropped = new();
        public EventFightState EventFight;
        public EventState Event;
        public List<string> EventsSeen = new();
        public HashSet<string> EventDone = new();
        public NextFight NextFight;
        public bool Scout, RewardFlash, NoShin;
        public string ShopGift;
        public CopyOffer CopyOffer;
        public List<FightRecord> Hist = new();
        /// <summary>시험 도구가 정해 넣는 빛(없으면 굴린다).</summary>
        public Dictionary<string, Glow> ForceGlow;
        public bool DevDrop;
        /// <summary>정신 붕괴 — 남은 싸움 수. 0 보다 크면 카드 얻기 · 신탁 · 제거가 막힌다(싸움이 끝날 때마다 1 준다).</summary>
        public int MindBreak;
        /// <summary>판 단위 사도 성장(growRun) — 사도 → 능력치(공격 · 방어 · 치명). 싸움마다 장비처럼 더한다.</summary>
        public Dictionary<string, Stats> Growth = new();
        /// <summary>카드 값(카드 인스턴스 카운터 — 소장 가치 · 충전 …) — 카드 id → 이름 → 값. 싸움을 넘어 남는다.</summary>
        public Dictionary<string, Dictionary<string, int>> CardVals = new();
        /// <summary>
        /// 주인 사도를 기다리는 교주 카드(카드 id, 들어온 차례) — 얻었지만 어느 사도 덱에 넣을지 아직 안 골랐다.
        /// 화면은 Run.PendingNeutral 이 있으면 사도를 고르게 하고 Run.AssignNeutral 로 넣는다. 덱의 교주 카드는 「id@사도」.
        /// </summary>
        public List<string> NeutralWait = new();
    }

    public sealed class MapNode
    {
        public string Id;
        public int Row, Col;
        public int? Lane;
        public double X;
        /// <summary>start · fight · elite · camp · campshop · event · boss.</summary>
        public string Type;
        public List<string> Next = new();
        /// <summary>싸움 세기(0 약 · 1 중 · 2 강).</summary>
        public int Fight;
        public List<string> Foes;
    }

    public sealed class MapState
    {
        public int Floor;
        public string Village;
        public List<List<MapNode>> Rows = new();
        public string At;
        public List<string> Seen = new();
    }

    public sealed class RewardState
    {
        public List<string> Equip;
        public string EquipTaken;
        public int Gold;
        public bool GoldTaken;
    }

    public sealed class ShopItem
    {
        public string Id;
        /// <summary>neutral · equip.</summary>
        public string Kind;
        public int Price;
        public bool Sold;
        public bool Delivery;
    }

    public sealed class ShopState
    {
        public int Floor;
        public string At;
        public List<ShopItem> Items = new();
        public int Rerolls;
        public bool RemoveUsed;
    }

    public sealed class FlashOffer
    {
        public string CardId;
        public List<int> Picks = new();
        /// <summary>후보마다 얹힌 축복(Picks 와 같은 차례, null = 없음).</summary>
        public List<string> Shins;
        public bool Swap;
    }

    public sealed class CampState
    {
        public string Key;
        public FlashOffer Train;
    }

    public sealed class CopyOffer
    {
        public string At;
        public List<string> Ids = new();
    }

    public sealed class EventFightState
    {
        public string Name;
        public List<string> Enemies = new();
        public List<Outcome> Win = new();
        public bool Elite;
    }

    /// <summary>이벤트에서 아직 고를 것 — remove · dupe · card(Cards) · flash(Offer) · shinPick(Kind) · gambleChoice(Options).</summary>
    public sealed class Pending
    {
        public string K;
        public List<string> Cards;
        public string Label;
        public FlashOffer Offer;
        public string Kind;
        public List<List<Outcome>> Options;
        /// <summary>remove — 시작 카드만.</summary>
        public bool Basic;
    }

    public sealed class EventState
    {
        public string Key;
        public List<string> Choices = new();
        public string Id;
        /// <summary>choose · fight · result.</summary>
        public string Phase = "choose";
        public List<string> Log = new();
        public List<Pending> Pending = new();
        public string Judged;
        public string Label;
        public string Say;
        public double ShinChance;
    }

    public sealed class FightRecord
    {
        public int Floor, Node;
        public string Kind;
        public List<string> Foes = new();
        public string Result;
        public int Turns;
        public int HpBefore, HpAfter, HpMax;
    }
}
