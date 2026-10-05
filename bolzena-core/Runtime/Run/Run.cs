using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 한 판 — 마을 하나의 두 층(1-1 ~ 2-10), 2층 보스를 이기면 완주. 지도 · 싸움 열기/닫기 · 보상 · 신탁 · 캠프 · 상점 · 장비 · 저장.
    /// 이벤트는 RunEvents.cs, 지도는 RunMap.cs.
    /// </summary>
    public sealed partial class Run
    {
        public readonly GameData Data;
        public readonly RunState S;
        public readonly Rng Rng;

        Run(GameData data, RunState s)
        {
            Data = data; S = s;
            Rng = new Rng(1) { State = s.RngState == 0 ? 1u : s.RngState };
        }

        /// <summary>판의 난수를 상태에 적어 둔다(저장 전에 · 매번 굴린 뒤에).</summary>
        double Rnd() { double v = Rng.Next(); S.RngState = Rng.State; return v; }
        int RndInt(int n) => n <= 0 ? 0 : (int)Math.Floor(Rnd() * n);

        // ── 새 판 · 저장 ───────────────────────────────────────────────
        public List<string> VillageIds => Data.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>마을을 안 준 판은 씨앗으로 정한다 — 같은 씨앗이면 같은 마을(판의 난수는 건드리지 않는다).</summary>
        public static string VillageBySeed(GameData d, long seed)
        {
            var ids = d.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (ids.Count == 0) return null;
            uint h = unchecked(((uint)seed ^ 0x9e3779b9u) * 2654435761u);
            return ids[(int)(h % (uint)ids.Count)];
        }

        /// <summary>모험을 시작하면 마을 하나를 무작위로 — 파티를 고르기 전에 보인다. r01 은 [0, 1) 난수.</summary>
        public static string RollVillage(GameData d, double r01)
        {
            var ids = d.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
            return ids.Count == 0 ? null : ids[Math.Min(ids.Count - 1, (int)Math.Floor(r01 * ids.Count))];
        }

        public static Run New(GameData data, List<string> party, long seed, string village = null, Dictionary<string, string> rows = null)
        {
            int max = party.Sum(k => data.Hero(k)?.Hp ?? 0);
            var s = new RunState
            {
                Seed = seed, RngState = new Rng(seed).State,
                Village = village != null && data.Villages.ContainsKey(village) ? village : VillageBySeed(data, seed),
                Party = party.ToList(),
                Rows = rows != null ? new Dictionary<string, string>(rows) : party.ToDictionary(k => k, k => data.Hero(k)?.Row ?? "mid"),
                PartyHp = max, PartyMaxHp = max, Gold = R.GOLD_START,
                Deck = data.BuildDeck(party),
            };
            return new Run(data, s);
        }

        public string Save() => GameData.ToJson(S);
        public static Run Load(GameData data, string json) => new Run(data, GameData.FromJson<RunState>(json));
        public static Run Of(GameData data, RunState s) => new Run(data, s);

        // ── 어디인가 ───────────────────────────────────────────────────
        public VillageDef VillageDef => Data.Villages[S.Village];
        public FloorDef CurrentFloor => VillageDef.Floors[Math.Min(S.Floor, VillageDef.Floors.Count - 1)];
        public bool IsLastFloor => S.Floor >= VillageDef.Floors.Count - 1;
        public bool IsBoss => S.Node >= 3;
        bool BossAsElite => S.EventFight == null && IsBoss && CurrentFloor.BossElite;
        public string Land => CurrentFloor.Land;

        public List<string> CurrentEnemies()
        {
            if (S.EventFight != null) return S.EventFight.Enemies;
            var f = CurrentFloor;
            if (IsBoss) return f.Boss;
            var at = S.Map?.At != null ? NodeById(S.Map, S.Map.At) : null;
            return at?.Foes ?? f.Pools[Math.Min(S.Node, f.Pools.Count - 1)][0];
        }

        string FightKind => S.EventFight != null ? "event" : IsBoss ? "boss" : S.Elite ? "elite" : "fight";

        public (double hp, double dmg) FoeScaleOf()
        {
            if (BossAsElite) return R.FoeScale(S.Floor, elite: true);
            return R.FoeScale(S.Floor, boss: IsBoss && S.EventFight == null, elite: S.EventFight != null ? S.EventFight.Elite : S.Elite);
        }

        // ── 싸움 ───────────────────────────────────────────────────────
        /// <summary>
        /// 싸움을 연다 — 전투 상태와 전리품을 이 자리에서 굴린다(차례: 신탁 → 전리품). 전투는 제 씨앗으로 따로 굴린다.
        /// hpx · dmgx 는 재는 도구가 층 배율 위에 더 곱하는 것.
        /// </summary>
        public (Battle battle, RewardState loot) OpenFight(double hpx = 1, double dmgx = 1, List<Cue> cues = null, Action<Cue> onCue = null)
        {
            var next = S.NextFight; S.NextFight = null;
            var glow = S.ForceGlow ?? RollEpiphany();
            var sc = FoeScaleOf();
            var setup = new BattleSetup
            {
                Party = S.Party.ToList(), Rows = S.Rows, Deck = S.Deck.ToList(), Enemies = CurrentEnemies().ToList(),
                PartyHp = S.PartyHp, PartyMaxHp = S.PartyMaxHp, Gear = GearStats(), GearRules = GearRules(), Flash = S.Flash,
                EnemyHp = sc.hp * hpx, EnemyDmg = sc.dmg * dmgx, Next = next, Shin = S.Shin, Gauge = S.Gauge,
                Elite = S.EventFight != null ? S.EventFight.Elite : S.Elite || BossAsElite,
                Glow = glow, Growth = S.Growth, CardVals = S.CardVals,
                Seed = (uint)(S.Seed + S.Floor * 101 + S.Node * 7 + S.Step * 13 + (S.EventFight != null ? 555 : 0)),
            };
            var b = Battle.Start(Data, setup, cues, onCue);
            var loot = S.EventFight != null ? null : RollReward();
            return (b, loot);
        }

        /// <summary>전투가 끝난 뒤 — 얻은 카드 · 신탁, 파티 HP, 남은 게이지, 싸움 기록.</summary>
        public void AfterFight(Battle b)
        {
            bool mind = MindBroken;
            if (!mind)
            {
                foreach (var id in b.GainedCards) if (!S.Deck.Contains(id) && PowerWhy(id) == null) S.Deck.Add(id);
                foreach (var (cardId, n, shin) in b.GainedFlash) { S.Flash[cardId] = n; if (shin != null) S.Shin[cardId] = shin; }
            }
            // 제거 태그 — 덱에서 완전히 뺀다
            foreach (var id in b.Removed) { int i = S.Deck.IndexOf(id); if (i >= 0) { S.Deck.RemoveAt(i); if (!S.Deck.Contains(id)) ForgetCard(id); } }
            // 판 단위 성장 · 카드 값(카드 인스턴스 카운터 — 엔진 상태 말고 데이터가 지은 이름)
            foreach (var kv in b.GrowthGain) S.Growth[kv.Key] = (S.Growth.TryGetValue(kv.Key, out var g0) ? g0 : new Stats()) + kv.Value;
            foreach (var kv in b.CardSt)
            {
                var keep = kv.Value.Where(x => !R.IsCardSt(x.Key) && x.Key != "비용" && !b.BattleVals.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                if (keep.Count > 0 && S.Deck.Contains(kv.Key)) S.CardVals[kv.Key] = keep; else S.CardVals.Remove(kv.Key);
            }
            // 봉인된 금기 — 보스를 처치하면 금기 카드로 바뀐다
            if (b.Over == "win" && IsBoss && S.EventFight == null)
                for (int i = 0; i < S.Deck.Count; i++)
                {
                    var d = Data.Card(S.Deck[i]);
                    if (d != null && d.Becomes != null && Data.Card(d.Becomes) != null && Data.View(S.Deck[i]).HasTag(Tag.SealedTaboo)) { S.Flash.Remove(S.Deck[i]); S.Deck[i] = d.Becomes; }
                }
            // 정신 붕괴 — 이 싸움이 끝나면 하나 줄고, 파티에 정신 붕괴가 남은 채 끝났으면 다음 싸움까지
            if (S.MindBreak > 0) S.MindBreak--;
            if (b.St(b.Pool, "정신 붕괴") > 0) S.MindBreak = Math.Max(S.MindBreak, 1);
            S.Hist.Add(new FightRecord
            {
                Floor = S.Floor + 1, Node = S.Node, Kind = FightKind, Foes = CurrentEnemies().ToList(), Result = b.Over, Turns = b.Turn,
                HpBefore = S.PartyHp, HpAfter = Math.Max(0, b.Pool.Hp), HpMax = S.PartyMaxHp,
            });
            S.PartyHp = Math.Max(0, b.Pool.Hp); S.PartyMaxHp = b.Pool.MaxHp;
            S.Gauge = Num.Clamp(b.Gauge, 0, R.GAUGE_MAX);
        }

        public bool PartyWiped => S.PartyHp <= 0;

        // ── 신탁(전투 중) ──────────────────────────────────────────────
        /// <summary>
        /// 싸움을 열 때 어느 카드가 빛날지 — 은총(사도마다: 아직 얻을 고유 카드가 남은 사도의 기본 카드) · 카드 신탁(사도마다 + 교주 카드 몫).
        /// 일반 · 엘리트 · 보스 칸은 은총 하나는 반드시, 엘리트는 카드 신탁 하나도 반드시.
        /// </summary>
        public Dictionary<string, Glow> RollEpiphany()
        {
            string kind = FightKind;
            T Pick<T>(IList<T> a) => a[RndInt(a.Count)];
            var glow = new Dictionary<string, Glow>();
            var heroes = S.Party.Where(k => UniquesLeft(k).Count > 0 && S.Deck.Any(id => Data.Card(id) is CardDef c && c.Hero == k && !c.Unique)).ToList();
            void Grace(string k)
            {
                var bases = S.Deck.Where(id => Data.Card(id) is CardDef c && c.Hero == k && !c.Unique && !GameData.IsCopy(id)).ToList();
                if (bases.Count == 0) return;
                var at = Pick(bases);
                glow[at] = new Glow { Kind = "hero", Hero = k, Options = new List<string> { Pick(UniquesLeft(k)) } };
            }
            foreach (var k in heroes) if (Rnd() < (R.EPI_HERO.TryGetValue(kind, out var p) ? p : 0)) Grace(k);
            if (heroes.Count > 0 && R.EPI_SURE_HERO.Contains(kind) && glow.Count == 0) Grace(Pick(heroes));
            var able = FlashTargets().Where(id => !glow.ContainsKey(id)).ToList();
            var owners = new List<(string owner, List<string> ids)>();
            foreach (var id in able)
            {
                var o = Data.Card(id).Hero ?? "neutral";
                var g = owners.FirstOrDefault(x => x.owner == o);
                if (g.ids == null) owners.Add((o, new List<string> { id })); else g.ids.Add(id);
            }
            var lit = new List<string>();
            foreach (var (_, ids) in owners) if (Rnd() < (R.EPI_CARD.TryGetValue(kind, out var q) ? q : 0)) lit.Add(Pick(ids));
            if (able.Count > 0 && lit.Count == 0 && (S.RewardFlash || R.EPI_SURE_CARD.Contains(kind))) lit.Add(Pick(able));
            if (lit.Count > 0) S.RewardFlash = false;
            foreach (var cardId in lit)
            {
                var opts = RollOracles(cardId);
                if (opts.Count > 0) glow[cardId] = new Glow { Kind = "card", Picks = opts };
            }
            return glow;
        }

        /// <summary>
        /// 신탁 후보 셋(R.ORACLE_PICKS) — 그 카드의 신탁 ①~⑤ 가운데 붙일 수 있는 것에서 무작위로(not 은 빼고 — 신탁 바꾸기),
        /// R.ORACLE_BLESS 확률로 그 가운데 하나에 축복. 전투 번뜩임 · 이벤트 · 캠프가 모두 이것을 쓴다.
        /// </summary>
        public List<GlowPick> RollOracles(string cardId, int not = 0)
        {
            var c = Data.Card(cardId);
            if (c == null) return new List<GlowPick>();
            var pool = Enumerable.Range(1, c.Oracles.Count).Where(n => n != not && FlashOk(cardId, n)).ToList();
            var picks = new List<int>();
            while (picks.Count < R.ORACLE_PICKS && pool.Count > 0) { int i = RndInt(pool.Count); picks.Add(pool[i]); pool.RemoveAt(i); }
            picks.Sort();
            var opts = picks.Select(n => new GlowPick { N = n }).ToList();
            if (opts.Count > 0 && Rnd() < R.ORACLE_BLESS)
            {
                var o = opts[RndInt(opts.Count)];
                var kinds = DivineKindsFor(Data.View(cardId, o.N));
                o.Shin = kinds.Count > 0 ? kinds[RndInt(kinds.Count)] : "draw";
            }
            return opts;
        }

        /// <summary>신탁 고르기 창의 후보(이벤트 · 캠프 — FlashOffer). 전투의 번뜩임은 b.EpiphanyOptions.</summary>
        public List<OracleOption> FlashOptions(FlashOffer offer) => offer == null ? new List<OracleOption>() :
            OracleOption.Of(Data, offer.CardId, offer.Picks.Select((n, i) => new GlowPick { N = n, Shin = offer.Shins != null && i < offer.Shins.Count ? offer.Shins[i] : null }));

        /// <summary>쓰지 못한 신탁 — 빛났지만 그 카드를 안 내고 끝났다. 끝난 뒤 하나를 골라 받는다. 못 받으면 까닭.</summary>
        /// <summary>정신 붕괴 — 카드 얻기 · 신탁 · 제거가 막혔다.</summary>
        public bool MindBroken => S.MindBreak > 0;
        const string MIND = "정신 붕괴 — 카드를 얻거나 신탁 · 제거를 할 수 없습니다";

        public string ClaimGlow(string cardId, Glow g, int choice)
        {
            if (MindBroken) return MIND;
            if (g == null || choice < 0 || choice >= g.Count) return "고를 수 없습니다";
            if (g.Kind == "hero")
            {
                var o = g.Options[choice];
                var why = PowerWhy(o); if (why != null) return why;
                S.Deck.Add(o);
                return null;
            }
            var p = g.Picks[choice];
            if (!FlashOk(cardId, p.N)) return "강화 카드가 되는 신탁은 덱에 한 장일 때만 붙습니다";
            S.Flash[cardId] = p.N;
            if (p.Shin != null) S.Shin[cardId] = p.Shin;
            return null;
        }

        /// <summary>아직 얻을 수 있는 고유 카드 — 덱에 있는 것 · 빼 버린 것은 빠진다.</summary>
        public List<string> UniquesLeft(string heroKey) => Data.UniquesOf(heroKey).Where(id => !S.Deck.Contains(id) && !S.Dropped.Contains(id)).ToList();

        void ForgetCard(string cardId)
        {
            if (Data.Card(cardId)?.Unique == true) S.Dropped.Add(cardId);
            S.Flash.Remove(cardId);
        }

        /// <summary>보상의 고유 카드 셋(이벤트 「고유 카드 선택」).</summary>
        public List<string> RewardCards()
        {
            var pool = S.Party.SelectMany(UniquesLeft).ToList();
            var outs = new List<string>();
            while (outs.Count < 3 && pool.Count > 0) { int i = RndInt(pool.Count); outs.Add(pool[i]); pool.RemoveAt(i); }
            return outs;
        }

        public CardView ViewOf(string id) => Data.View(id, S.Flash.TryGetValue(id, out var n) ? n : 0);
        public bool OnlyCard(string id) => Data.Card(id) != null && ViewOf(id).IsOnly;
        public string PowerWhy(string cardId) => OnlyCard(cardId) && S.Deck.Contains(cardId) ? "유일 — 덱에 한 장만 넣을 수 있습니다" : null;

        /// <summary>신탁 n 을 붙여도 되나 — 「강화 카드.」 신탁은 덱에 그 카드가 한 장일 때만.</summary>
        public bool FlashOk(string cardId, int n)
        {
            var c = Data.Card(cardId);
            if (c == null || n < 1 || n > c.Oracles.Count) return false;
            if (!Data.View(cardId, n).IsPower || Data.View(cardId).IsPower) return true;
            return S.Deck.Count(x => x == cardId) <= 1;
        }

        /// <summary>신탁을 붙일 수 있는 카드 — 가진 고유 · 교주 카드 가운데 신탁 다섯이 있고 아직 안 붙은 것(복제본 · 금기 빼고).</summary>
        public List<string> FlashTargets() => S.Deck.Distinct().Where(id =>
        {
            var c = Data.Card(id);
            return c != null && !GameData.IsCopy(id) && (c.Unique || c.Neutral) && c.Oracles.Count == 5 && !S.Flash.ContainsKey(id) && !Data.View(id).IsTaboo;
        }).ToList();

        /// <summary>이 카드에 쓸모 있는 축복 — 그 카드만의 축복이 있으면 그것들, 없으면 공용 풀에서 쓸모 있는 것.</summary>
        public static List<string> DivineKindsFor(CardView c)
        {
            if (c == null) return new List<string>();
            if (c.Def.Blesses.Count > 0) return Enumerable.Range(0, c.Def.Blesses.Count).Select(i => i == 0 ? "own" : "own" + i).ToList();
            bool Has(string k) => c.Fx.Any(f => f.K == k);
            var ok = new Dictionary<string, bool>
            {
                ["power"] = Has(FxK.Dmg), ["weakSpot"] = Has(FxK.Dmg), ["frost"] = Has(FxK.Dmg), ["heal"] = Has(FxK.Heal),
                ["guard"] = Has(FxK.Block) || Has(FxK.Shield), ["cost"] = c.Cost >= 1, ["ap"] = c.Cost >= 1,
            };
            var kinds = R.DIVINE_KINDS.TryGetValue(c.Type, out var ks) ? ks : new[] { "draw", "cost" };
            return kinds.Where(k => !ok.TryGetValue(k, out var v) || v).ToList();
        }

        public FlashOffer OfferFlash()
        {
            var able = FlashTargets();
            if (able.Count == 0) return null;
            var cardId = able[RndInt(able.Count)];
            return OfferOf(cardId);
        }

        /// <summary>그 카드의 신탁 고르기(무작위 셋 + 축복 확률). not — 빼는 신탁(바꾸기).</summary>
        public FlashOffer OfferOf(string cardId, int not = 0, bool swap = false)
        {
            var picks = RollOracles(cardId, not);
            return new FlashOffer { CardId = cardId, Picks = picks.Select(p => p.N).ToList(), Shins = picks.Select(p => p.Shin).ToList(), Swap = swap };
        }

        public bool TakeFlash(string cardId, int n)
        {
            if (cardId == null || n <= 0 || !FlashOk(cardId, n)) return false;
            S.Flash[cardId] = n;
            return true;
        }

        /// <summary>신탁 고르기 결과를 판에 — 고른 후보에 축복이 얹혔으면 축복도.</summary>
        bool TakeOffer(FlashOffer o, int n)
        {
            if (o == null || !o.Picks.Contains(n) || !TakeFlash(o.CardId, n)) return false;
            int i = o.Picks.IndexOf(n);
            var sh = o.Shins != null && i < o.Shins.Count ? o.Shins[i] : null;
            if (sh != null && !S.NoShin) S.Shin[o.CardId] = sh;
            return true;
        }

        // ── 보상 ───────────────────────────────────────────────────────
        /// <summary>전리품 — 싸움을 열 때 한 번 굴린다. 골드는 늘, 장비는 확률로 하나(마지막 보스는 안 떨군다).</summary>
        public RewardState RollReward()
        {
            int baseGold = IsBoss ? R.GOLD_BOSS : R.GOLD_FIGHT[0] + RndInt(R.GOLD_FIGHT[1] - R.GOLD_FIGHT[0] + 1) + S.Floor * R.GOLD_FLOOR;
            int gold = S.Elite ? Num.Round(baseGold * R.ELITE_GOLD) : baseGold;
            bool lastBoss = IsBoss && IsLastFloor;
            var table = IsBoss ? R.BOSS_EQUIP : S.Elite ? R.ELITE_EQUIP : R.FIGHT_EQUIP;
            double chance = IsBoss || S.Elite ? 1 : R.DROP_FIGHT;
            var w = table[Math.Min(S.Floor, table.Length - 1)];
            var eq = !lastBoss && (S.DevDrop || Rnd() < chance) ? OfferEquip(w, 1) : new List<string>();
            S.Reward = new RewardState { Equip = eq.Count > 0 ? eq : null, Gold = gold };
            return S.Reward;
        }

        public void TakeGold()
        {
            if (S.Reward != null && !S.Reward.GoldTaken) { S.Gold += S.Reward.Gold; S.Reward.GoldTaken = true; }
        }

        public string TakeEquip(string equipId)
        {
            var rw = S.Reward;
            if (rw?.Equip == null || !rw.Equip.Contains(equipId) || rw.EquipTaken != null) return "고를 수 없습니다";
            GainEquip(equipId);
            rw.EquipTaken = equipId;
            return null;
        }

        // ── 캠프 ───────────────────────────────────────────────────────
        /// <summary>캠프에 든다 — 쉬기(파티 HP 30%) 또는 수련(가진 고유 카드 하나에 신탁) 하나.</summary>
        public CampState EnterCamp(string kind)
        {
            string key = $"{S.Floor}:{kind}{(S.Map?.At != null ? ":" + S.Map.At : "")}";
            if (!S.Stops.ContainsKey(key)) { S.Stops[key] = ""; S.Camp = new CampState { Key = key, Train = OfferFlash() }; }
            return S.Camp;
        }
        bool CampUsed => S.Camp == null || !S.Stops.ContainsKey(S.Camp.Key) || S.Stops[S.Camp.Key] != "";
        public int CampHealOf() => Math.Min(S.PartyMaxHp - S.PartyHp, Num.Round(S.PartyMaxHp * R.CAMP_HEAL));
        public string CampRest()
        {
            if (CampUsed) return "이번 캠프에서는 이미 골랐습니다";
            S.PartyHp = Math.Min(S.PartyMaxHp, S.PartyHp + Math.Max(0, CampHealOf()));
            S.Stops[S.Camp.Key] = "rest";
            return null;
        }
        public string CampTrain(int n)
        {
            if (CampUsed) return "이번 캠프에서는 이미 골랐습니다";
            if (MindBroken) return MIND;
            var t = S.Camp.Train;
            if (t == null || !TakeOffer(t, n)) return "수련할 카드가 없습니다";
            S.Stops[S.Camp.Key] = "train";
            return null;
        }

        // ── 상점 ───────────────────────────────────────────────────────
        List<ShopItem> Shelf()
        {
            var has = new HashSet<string>(S.Deck);
            var pool = Data.NeutralIds().Where(id => !(OnlyCard(id) && has.Contains(id))).ToList();
            var neutral = new List<string>();
            while (neutral.Count < R.SHOP_NEUTRAL && pool.Count > 0)
            {
                var w = pool.Select(id => R.SHOP_GRADE_WEIGHT.TryGetValue(Data.Card(id).Grade ?? "", out var x) ? x : 1).ToList();
                double r = Rnd() * w.Sum(); int i = 0;
                while (r >= w[i]) r -= w[i++];
                neutral.Add(pool[i]); pool.RemoveAt(i);
            }
            return neutral.Select(id => new ShopItem { Id = id, Kind = "neutral", Price = Data.Card(id).Price })
                .Concat(OfferEquip(R.SHOP_EQUIP, R.SHOP_EQUIP_N).Select(id => new ShopItem { Id = id, Kind = "equip", Price = R.EQUIP_PRICE[Data.Equip(id).Grade] })).ToList();
        }

        public ShopState RollShop()
        {
            S.Shop = new ShopState { Floor = S.Floor, At = S.Map?.At, Items = Shelf() };
            if (S.ShopGift != null)
            {
                var ids = OfferEquip(new Dictionary<string, int> { [S.ShopGift] = 1 }, 1);
                if (ids.Count > 0) S.Shop.Items.Add(new ShopItem { Id = ids[0], Kind = "equip", Price = 0, Delivery = true });
                S.ShopGift = null;
            }
            S.ShopSeen.Add(S.Floor);
            return S.Shop;
        }

        public int RerollPrice => R.SHOP_REROLL + R.SHOP_REROLL_STEP * (S.Shop?.Rerolls ?? 0);
        public string RerollShop()
        {
            if (S.Shop == null) return "상점이 열려 있지 않습니다";
            int price = RerollPrice;
            if (S.Gold < price) return "골드가 모자랍니다";
            S.Gold -= price;
            var keep = S.Shop.Items.Where(it => it.Delivery && !it.Sold).ToList();
            S.Shop.Items = Shelf().Concat(keep).ToList();
            S.Shop.Rerolls++;
            return null;
        }

        public string Buy(int idx)
        {
            var it = S.Shop != null && idx >= 0 && idx < S.Shop.Items.Count ? S.Shop.Items[idx] : null;
            if (it == null || it.Sold) return "이미 팔린 물건입니다";
            if (S.Gold < it.Price) return "골드가 모자랍니다";
            if (it.Kind == "neutral" && PowerWhy(it.Id) != null) return PowerWhy(it.Id);
            if (it.Kind == "neutral" && MindBroken) return MIND;
            S.Gold -= it.Price;
            it.Sold = true;
            if (it.Kind == "equip") GainEquip(it.Id, bought: true); else S.Deck.Add(it.Id);
            return null;
        }

        public int RemovePrice => R.PRICE_REMOVE + R.PRICE_REMOVE_STEP * S.Removals;
        public string RemoveCard(string cardId)
        {
            if (S.Shop == null || S.Shop.RemoveUsed) return "이번에는 더 뺄 수 없습니다";
            if (MindBroken) return MIND;
            int price = RemovePrice;
            if (S.Gold < price) return "골드가 모자랍니다";
            int i = S.Deck.IndexOf(cardId);
            if (i < 0) return "덱에 없는 카드입니다";
            if (ViewOf(cardId).IsTaboo) return "금기 카드는 뺄 수 없습니다";
            S.Gold -= price;
            S.Deck.RemoveAt(i);
            S.Removals++;
            S.Shop.RemoveUsed = true;
            ForgetCard(cardId);
            return null;
        }

        // ── 장비 ───────────────────────────────────────────────────────
        public Stats StatsOf(string equipId, string heroKey)
        {
            var e = Data.Equip(equipId);
            if (e == null) return new Stats();
            var s = e.Stats ?? new Stats();
            if (e.Affinity != null && e.Affinity == heroKey && e.AffinityStats != null) s = s + e.AffinityStats;
            return s;
        }
        public Dictionary<string, string> GearOf(string heroKey) => S.Gear.TryGetValue(heroKey, out var g) ? g : new Dictionary<string, string>();

        public Dictionary<string, Stats> GearStats() => S.Party.ToDictionary(k => k, k => GearOf(k).Values.Aggregate(new Stats(), (a, id) => a + StatsOf(id, k)));

        /// <summary>낀 장비의 효과(+ 애착 사도가 꼈으면 애착 효과) — 그 사도의 패시브로 돈다.</summary>
        public Dictionary<string, List<PassiveRule>> GearRules()
        {
            var o = new Dictionary<string, List<PassiveRule>>();
            foreach (var k in S.Party)
            {
                var rules = new List<PassiveRule>();
                foreach (var id in GearOf(k).Values)
                {
                    var e = Data.Equip(id); if (e == null) continue;
                    rules.AddRange(e.Effect);
                    if (e.Affinity == k) rules.AddRange(e.AffinityEffect);
                }
                if (rules.Count > 0) o[k] = rules;
            }
            return o;
        }

        /// <summary>파티 최대 HP 가 바뀌면 지금 HP 도 — 늘면 그만큼 차고, 줄면 넘치는 만큼만 깎인다.</summary>
        void ShiftHp(int d)
        {
            if (d == 0) return;
            S.PartyMaxHp = Math.Max(1, S.PartyMaxHp + d);
            S.PartyHp = Math.Max(1, Math.Min(S.PartyMaxHp, S.PartyHp + Math.Max(0, d)));
        }

        /// <summary>장비를 얻는다 — 「정할 차례」 줄(Bag)에 선다. 화면은 곧장 끼기 or 팔기를 묻는다.</summary>
        public string GainEquip(string equipId, bool bought = false)
        {
            if (Data.Equip(equipId) == null) return "그런 장비가 없습니다";
            S.Bag.Add(equipId);
            if (bought) S.BagBought.Add(equipId);
            return null;
        }
        public bool IsBought(string equipId) => S.BagBought.Contains(equipId);

        /// <summary>낀다 — replace 면 그 칸에 낀 것을 판다(바꿔 끼기 = 옛 장비 팔기). 한 번 끼면 빼지 못한다.</summary>
        public string Equip(string heroKey, string equipId, bool replace = false)
        {
            var e = Data.Equip(equipId);
            if (e == null) return "그런 장비가 없습니다";
            if (!S.Party.Contains(heroKey)) return "파티에 없는 사도입니다";
            int i = S.Bag.IndexOf(equipId);
            if (i < 0) return "받은 장비가 아닙니다";
            if (!S.Gear.TryGetValue(heroKey, out var g)) S.Gear[heroKey] = g = new Dictionary<string, string>();
            g.TryGetValue(e.Slot, out var old);
            if (old != null && !replace) return $"{e.Slot} 칸이 차 있습니다 — 바꿔 끼면 낀 것은 팔립니다";
            S.Bag.RemoveAt(i);
            if (old != null) { ShiftHp(-StatsOf(old, heroKey).Hp); S.Gold += SellPrice(old); }
            g[e.Slot] = equipId;
            ShiftHp(StatsOf(equipId, heroKey).Hp);
            S.BagBought.Remove(equipId);
            return null;
        }

        public int SellPrice(string equipId) { var e = Data.Equip(equipId); return e != null ? Num.Round(R.EQUIP_PRICE[e.Grade] * R.EQUIP_SELL) : 0; }
        public string SellEquip(string equipId)
        {
            int i = S.Bag.IndexOf(equipId);
            if (i < 0) return "받은 장비가 아닙니다 — 낀 장비는 바꿔 낄 때 팔립니다";
            if (IsBought(equipId)) return "상점에서 산 장비는 팔 수 없습니다 — 사도에게 낍니다";
            S.Bag.RemoveAt(i);
            S.Gold += SellPrice(equipId);
            return null;
        }

        HashSet<string> Owned() => new HashSet<string>(S.Bag.Concat(S.Gear.Values.SelectMany(g => g.Values)));

        /// <summary>무작위로 n개 — 등급 무게. 이미 가진 장비도 나온다(같은 장비를 둘이 낄 수 있다). 한 번에 뽑는 것끼리는 겹치지 않는다.</summary>
        public List<string> OfferEquip(Dictionary<string, int> weights, int n, bool dupes = true)
        {
            var have = dupes ? new HashSet<string>() : Owned();
            var pool = Data.Equips.Keys.OrderBy(x => x, StringComparer.Ordinal).Where(id => !have.Contains(id) && Data.Equip(id).Grade != null && weights.TryGetValue(Data.Equip(id).Grade, out var gw) && gw > 0 && R.EQUIP_PRICE.ContainsKey(Data.Equip(id).Grade)).ToList();
            var outs = new List<string>();
            while (outs.Count < n && pool.Count > 0)
            {
                var w = pool.Select(id => weights[Data.Equip(id).Grade]).ToList();
                double r = Rnd() * w.Sum(); int i = 0;
                while (r >= w[i]) r -= w[i++];
                outs.Add(pool[i]); pool.RemoveAt(i);
            }
            return outs;
        }

        /// <summary>칸을 정해 하나 — 이미 가진 것은 빼고.</summary>
        public List<string> OfferEquipSlot(string grade, string slot)
        {
            var have = Owned();
            var pool = Data.Equips.Values.Where(e => !have.Contains(e.Id) && e.Grade == grade && e.Slot == slot).Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            return pool.Count > 0 ? new List<string> { pool[RndInt(pool.Count)] } : new List<string>();
        }

        /// <summary>판 기록 — 읽기 좋은 꼴(이름을 같이 적는다). 모아서 밸런스를 잰다(웹판 recordOf). stage — 「2층 보스 전」 · 「승리」 · 「패배」 따위.</summary>
        public Dictionary<string, object> Record(string stage) => new()
        {
            ["kind"] = "bolzena-record", ["v"] = 3, ["stage"] = stage, ["at"] = DateTime.UtcNow.ToString("o"), ["seed"] = S.Seed, ["village"] = S.Village,
            ["party"] = S.Party.Select(k => new Dictionary<string, object> { ["key"] = k, ["name"] = Data.Hero(k)?.Name, ["row"] = S.Rows.TryGetValue(k, out var r) ? r : null,
                ["gear"] = GearOf(k).ToDictionary(kv => kv.Key, kv => Data.Equip(kv.Value)?.Name ?? kv.Value) }).ToList(),
            ["partyHp"] = S.PartyHp, ["partyMaxHp"] = S.PartyMaxHp, ["gold"] = S.Gold, ["gauge"] = S.Gauge, ["removals"] = S.Removals,
            ["deck"] = S.Deck.GroupBy(x => x).Select(g => new Dictionary<string, object> { ["id"] = g.Key, ["name"] = Data.Card(g.Key)?.Name, ["n"] = g.Count(),
                ["flash"] = S.Flash.TryGetValue(g.Key, out var f) ? f : (int?)null, ["shin"] = S.Shin.TryGetValue(g.Key, out var sh) ? sh : null }).ToList(),
            ["fights"] = S.Hist,
        };

        // ── 층 넘기기 · 보스 복제 ──────────────────────────────────────
        /// <summary>다음 칸으로. 보스를 넘으면 층이 바뀐다 — 2층 보스를 넘으면 판을 이긴 것(Done = "clear").</summary>
        public void Advance()
        {
            if (!IsBoss) { S.Node++; return; }
            if (IsLastFloor) { S.Done = "clear"; return; }
            S.Floor++; S.Node = 0;
            S.PartyHp = Math.Min(S.PartyMaxHp, S.PartyHp + R.FLOOR_REST * S.Party.Count);
        }

        List<string> Copyable() => S.Deck.Distinct().Where(id =>
        {
            var c = Data.Card(id);
            return c != null && c.Unique && c.Hero != null && !GameData.IsCopy(id) && !ViewOf(id).IsOnly && !ViewOf(id).IsTaboo;
        }).ToList();

        /// <summary>층 보스 보상 — 덱에 가진 고유 카드 가운데 셋. 하나를 고르면 복제본이 한 장 더(BossCopy).</summary>
        public List<string> BossCopyOffer()
        {
            string at = S.Floor.ToString();
            if (S.CopyOffer != null && S.CopyOffer.At == at) return S.CopyOffer.Ids.ToList();
            var pool = Copyable(); var ids = new List<string>();
            while (ids.Count < 3 && pool.Count > 0) { int i = RndInt(pool.Count); ids.Add(pool[i]); pool.RemoveAt(i); }
            S.CopyOffer = new CopyOffer { At = at, Ids = ids };
            return ids.ToList();
        }

        public string BossCopy(string id)
        {
            var pool = Copyable();
            S.CopyOffer = null;
            if (MindBroken) return null;
            if (id == null || !pool.Contains(id)) return null;
            return AddCopy(id);
        }

        /// <summary>복제본을 덱에 — 원본의 신탁 · 축복을 옮겨 받은 따로 된 카드(id + ^).</summary>
        public string AddCopy(string id)
        {
            string cid = GameData.IsCopy(id) ? id : GameData.BaseId(id) + GameData.COPY;
            if (!S.Deck.Contains(cid))
            {
                if (S.Flash.TryGetValue(id, out var f)) S.Flash[cid] = f; else S.Flash.Remove(cid);
                if (S.Shin.TryGetValue(id, out var sh)) S.Shin[cid] = sh; else S.Shin.Remove(cid);
            }
            S.Deck.Add(cid);
            return cid;
        }
    }
}
