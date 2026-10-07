using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Bolzena.Core
{
    public enum Side { Party, Enemy }

    /// <summary>능력치 증감 — 주는/받는 피해 · 공격력 · 방어력 · 치명. Left 턴 남는다(다음 내 턴 시작에 1 준다).</summary>
    public sealed class Mod
    {
        public string Stat;
        public double V;
        public int Left;
        public string Src;
        /// <summary>강화 카드의 「전투 내내」.</summary>
        public bool Run;
        public Mod Copy() => (Mod)MemberwiseClone();
    }

    /// <summary>
    /// 싸우는 몸 하나 — 사도 · 적 · 파티(한 몸).
    /// 파티는 한 몸이다(docs/16 §8): 사도의 HP · 방어 · 실드 · 사망은 파티(Pool)의 것을 가리킨다(BodyRef).
    /// 사도에게 남는 것: 공격력 · 방어력 · 치명 · 줄 · 증감(Mods) · 사도 층 상태(사기).
    /// </summary>
    public sealed class Unit
    {
        public Side Side;
        public int Idx;
        public string Key;
        public string Name;

        /// <summary>사도면 파티 몸. 그 밖은 null(자기가 몸).</summary>
        [JsonIgnore] public Unit BodyRef;

        [JsonProperty] int hp, maxHp, block, shield;
        [JsonProperty] bool dead;
        [JsonIgnore] public int Hp { get => BodyRef != null ? BodyRef.Hp : hp; set { if (BodyRef != null) BodyRef.Hp = value; else hp = value; } }
        [JsonIgnore] public int MaxHp { get => BodyRef != null ? BodyRef.MaxHp : maxHp; set { if (BodyRef != null) BodyRef.MaxHp = value; else maxHp = value; } }
        [JsonIgnore] public int Block { get => BodyRef != null ? BodyRef.Block : block; set { if (BodyRef != null) BodyRef.Block = value; else block = value; } }
        [JsonIgnore] public int Shield { get => BodyRef != null ? BodyRef.Shield : shield; set { if (BodyRef != null) BodyRef.Shield = value; else shield = value; } }
        [JsonIgnore] public bool Dead { get => BodyRef != null ? BodyRef.Dead : dead; set { if (BodyRef != null) BodyRef.Dead = value; else dead = value; } }
        [JsonIgnore] public Unit Body => BodyRef ?? this;

        /// <summary>이 몸의 상태 — 사도는 사도 층(사기)만 여기, 나머지는 파티 몸의 것(Battle.St 가 가른다).</summary>
        public Dictionary<string, int> Status = new();
        public List<Mod> Mods = new();
        /// <summary>지속 피해 · 고정 피해 상태의 바탕(건 사람의 공격력).</summary>
        public Dictionary<string, int> DotU = new();
        /// <summary>횟수 상태마다 마지막으로 쓴 일 번호.</summary>
        public Dictionary<string, int> StUse = new();
        public int CtrSeq;
        public int ImmuneHit;

        // ── 사도 ──
        public int Atk, Def, Crit;
        /// <summary>적의 줄(front · back — 배치 2 4 / 1 3). 사도는 열이 없다 — 맞는 모습은 파티 순서(PickTarget).</summary>
        public string Row;
        public string Role;
        public string Nature;
        public int Share;
        public Stats GearAdd;

        // ── 적 ──
        public bool Boss;
        public int Step;
        public Intent Intent;
        /// <summary>지금 수가 모으기(charge)에서 넘어온 수 — 즉시 행동으로 당겨지지 않는다.</summary>
        public bool IntentFromCharge;
        public List<string> Hist = new();
        public double Dmgx = 1;
        public double Tough, ToughMax;
        public bool Broken, Sealed, Phased, Phased2;
        public int StunGuard;
        public int RushCnt;
        public bool RushedTurn;
        public Dictionary<string, int> PUsed = new();
        public int HitSeq, EmberSeq;
        public double EmberK = 1;
        // 적 효과 틀(2026-10-05)
        /// <summary>다음 차례에 반드시 할 수(쌓이는 수치 문턱 mode next).</summary>
        public Intent ForceNext;
        /// <summary>재 속 — 다음 내 턴 시작까지 남은 턴(0 이면 없음) · 되살아날 체력 비율.</summary>
        public int ReviveIn;
        /// <summary>손패 흡수(수 seize) — 이 적이 빼앗아 쥔 카드 · 빼앗은 뒤 받은 피해 · 되찾는 피해(Battle.Seize).</summary>
        public List<string> Seized = new();
        public int SeizeDmg, SeizeNeed;
        public double ReviveHp;
        /// <summary>가사 — 쓰러졌지만 적이 회복하면 되살아난다.</summary>
        public bool Feign;
        /// <summary>자기를 세운 적의 Idx(-1 = 처음부터 있던 적).</summary>
        public int Summoner = -1;
        /// <summary>이번 턴(내 턴 · 적의 차례) 이미 행동했다.</summary>
        public bool ActedTurn;
        /// <summary>죽음의 낙인 — 낙인을 건 카드(맨 id).</summary>
        public HashSet<string> Brand = new();
        /// <summary>협공 · 반격을 건 사도의 Idx(파티 몸) — 반격은 그 사도 방어력, 협공은 그 사도 공격력.</summary>
        public Dictionary<string, int> Giver = new();
        /// <summary>적 — 걸린 함정(다음 행동이 공격이면 돈다).</summary>
        public List<LaterRec> Traps = new();
        /// <summary>적 — 혼란(다음 치는 수가 다른 적을 친다).</summary>
        public bool Confused;
        /// <summary>보스 클론 — 다음에 고학년을 예고할 턴(0 = 아직 안 정함). BossUlt.</summary>
        public int UltAt;
        /// <summary>상태 겹마다 건 쪽 — 상태 id → 건 쪽(hero:키 · enemy:Idx · gear:키 · neutral · event) → 겹. 화면은 Battle.StatusViews 로 읽는다.</summary>
        public Dictionary<string, Dictionary<string, int>> Src = new();

        public Unit Clone()
        {
            var u = (Unit)MemberwiseClone();
            u.Brand = new HashSet<string>(Brand);
            u.Giver = new Dictionary<string, int>(Giver);
            u.Traps = Traps.ConvertAll(x => x.Copy());
            u.Src = Src.ToDictionary(kv => kv.Key, kv => new Dictionary<string, int>(kv.Value));
            u.BodyRef = null;
            u.Status = new Dictionary<string, int>(Status);
            u.Mods = Mods.ConvertAll(m => m.Copy());
            u.DotU = new Dictionary<string, int>(DotU);
            u.StUse = new Dictionary<string, int>(StUse);
            u.Hist = new List<string>(Hist);
            u.PUsed = new Dictionary<string, int>(PUsed);
            return u;
        }

        public override string ToString() => $"{Name}({Side} {Idx})";
    }

    /// <summary>
    /// 연출 쪽지 — 화면이 이것을 보고 그린다. 판에는 아무 영향이 없다.
    /// K: act(움직임 — Anim · Say · T) · hurt(V · Guard · Crit · From → To) · heal · block · shield · status(Id · Up) · tough(From → To) ·
    /// break · die · auto(손에서 저절로 — Tag · Label · Name) · talk(사도 대사 때 — Hero · Moment).
    /// </summary>
    public sealed class Cue
    {
        public string K;
        public Side Side;
        public int Idx;
        public int V;
        public double From, To;
        public int Guard;
        public bool Crit;
        public string Id;
        public bool Up;
        public string Anim;
        public string Say;
        public string T;
        public bool Rush;
        public string Tag;
        public string Label;
        public string Name;
        public string Hero;
        public string CardId;
        public string Moment;
        public int Over;
        /// <summary>card 쪽지 — 어디서(draw · hand · discard · gone · new) 어디로(draw · hand · discard · gone · play).</summary>
        public string Pile, ToPile;
        public int? Target;

        public override string ToString() => $"{K}:{Side}{Idx} {Id}{V}";
    }
}
