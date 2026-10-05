using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>전투를 여는 데 필요한 것 — 판(Run.OpenFight)이 채운다. 시험 · 봇은 손으로 채워도 된다.</summary>
    public sealed class BattleSetup
    {
        public List<string> Party = new();
        public Dictionary<string, string> Rows;
        public List<string> Deck = new();
        public List<string> Enemies = new();
        /// <summary>파티 HP · 최대 HP — 없으면 세 사도 HP(장비 포함) 합으로 가득.</summary>
        public int? PartyHp, PartyMaxHp;
        public long Seed = 1;
        public bool NoNature;
        /// <summary>판의 적 속성 — 주면 이 싸움의 모든 적(소환 포함)의 성격 · 약점을 이것으로 맞춘다(RunState.EnemyNature). 없으면 적 데이터 그대로.</summary>
        public string EnemyNature;
        /// <summary>사도마다 장비 능력치 줄(HP 는 파티 최대 HP 에 이미 들어 있다 — 여기선 공격 · 방어 · 치명만).</summary>
        public Dictionary<string, Stats> Gear;
        /// <summary>사도마다 장비 효과(패시브로 돈다).</summary>
        public Dictionary<string, List<PassiveRule>> GearRules;
        public Dictionary<string, int> Flash;
        public Dictionary<string, string> Shin;
        public Dictionary<string, Glow> Glow;
        /// <summary>적 체력 배율(없으면 R.ENEMY_HP) · 피해 배율(없으면 1).</summary>
        public double? EnemyHp, EnemyDmg;
        public NextFight Next;
        public int Gauge;
        public bool Elite;
        /// <summary>판 단위 사도 성장(공격 · 방어 · 치명) — 장비처럼 더한다.</summary>
        public Dictionary<string, Stats> Growth;
        /// <summary>카드 값(판에 남는 카드 인스턴스 카운터).</summary>
        public Dictionary<string, Dictionary<string, int>> CardVals;
    }

    /// <summary>
    /// 신탁 — 이 전투에서 빛나는 카드. 내는 순간 고른다(Battle.ApplyEpiphany).
    /// Kind "hero"(은총 — Options 는 고유 카드 id) · "card"(카드 신탁 — Picks 는 신탁 번호와 축복).
    /// </summary>
    public sealed class Glow
    {
        public string Kind;
        public string Hero;
        public List<string> Options = new();
        public List<GlowPick> Picks = new();
        public Glow Copy() => new Glow { Kind = Kind, Hero = Hero, Options = Options.ToList(), Picks = Picks.Select(p => new GlowPick { N = p.N, Shin = p.Shin }).ToList() };
        [Newtonsoft.Json.JsonIgnore] public int Count => Kind == "card" ? Picks.Count : Options.Count;
    }
    public sealed class GlowPick { public int N; public string Shin; }

    /// <summary>카드를 낼 때 고르는 것 — 버릴 카드 · 아군.</summary>
    public sealed class PlayOpts
    {
        public List<string> Discard;
        public int? Ally;
        /// <summary>두 갈래 카드(CardDef.Choices) — 고른 갈래 번호(1 · 2). 효과의 ifChoice n 이 이것을 본다.</summary>
        public int? Choice;
        internal double? Scale;
        internal bool Auto, OpeningPlay;
    }

    public struct PlayResult
    {
        public bool Ok;
        public string Why;
        /// <summary>종극 — 이 카드로 턴이 끝난다(화면이 턴을 넘긴다).</summary>
        public bool Finale;
        public static PlayResult Fail(string why) => new PlayResult { Ok = false, Why = why };
    }

    /// <summary>
    /// 전투 하나. 화면을 모른다 — 상태를 바꾸고 기록(Log)과 연출 쪽지(Cues · OnCue)를 남긴다.
    /// 부르는 곳: Start → (PlayCard · UseUlt · ApplyEpiphany)* → EndTurn … Over 가 "win" · "lose" 가 될 때까지.
    /// </summary>
    public sealed partial class Battle
    {
        public readonly GameData Data;
        public Rng Rng;

        public List<Unit> Party = new();
        public Unit Pool;
        public List<Unit> Enemies = new();

        public int Turn, Ap, ApJam, ApCarry, ApLeft, StartAp, Opening;
        public int Gauge;
        public string LastUlt;
        public List<string> Draw = new(), Hand = new(), Discard = new(), Gone = new();
        public int NextCheaper;
        public string Over;
        public bool NoNature, Preview;
        /// <summary>판의 적 속성(BattleSetup.EnemyNature) — null 이면 적마다 데이터 성격.</summary>
        public string EnemyNature;
        public int? PreviewPick;

        public Dictionary<string, int> Flash = new();
        public Dictionary<string, string> Shin = new();
        public Dictionary<string, Glow> Glow = new();
        public List<string> GainedCards = new();
        public List<(string cardId, int n, string shin)> GainedFlash = new();
        public HashSet<string> FreeOnce = new();
        public Dictionary<string, int> FreeTurn = new();

        // 키워드 · 패시브
        public Dictionary<string, KwRt> Kw = new();
        public Dictionary<string, List<RuleRt>> Passives = new();
        public Dictionary<string, Dictionary<string, int>> Stacks = new();
        public Dictionary<string, Dictionary<string, int>> StackCap = new();
        public Dictionary<string, int> Fired = new(), Counts = new();
        public Dictionary<string, List<AlwaysMod>> Always = new();
        int depth;
        HashSet<string> firing = new();

        // 한 번의 일 · 누가
        public int ActSeq, SeqN;
        public string Acting, ModSrc;
        bool gainIn;

        // 이번 턴
        public string PrevNat;
        public List<(string hero, string type)> PlayLog = new();
        /// <summary>이번 턴 낸 카드 id(맨 id — 신탁 · 복제 꼬리 없이) — 「같은 카드 잇달아」.</summary>
        public List<string> PlayIds = new();
        /// <summary>이번 턴 카드에 쓴 AP · 치른 HP · 버린 장수. 받은 피해(방어 · 실드가 받은 몫 포함) — 이번 판(내 턴 + 적의 차례) · 지난 판.</summary>
        public int ApSpent, PaidHp, DiscardedTurn, TakenNow, TakenPrev;
        /// <summary>카드 id → 손에 남아 넘긴 턴 수(보존으로 이어서) — 「손에 N턴 머문 카드」.</summary>
        public Dictionary<string, int> Held = new();
        public int PlayedThisTurn;
        public Dictionary<string, int> PlayedBy = new();
        public bool RushedThisTurn;
        public HashSet<string> HurtPrev = new(), HurtNow = new();
        public Dictionary<string, int> KillPrev = new(), KillNow = new();
        public bool Ending, FoeTurn, Rushing, FinaleLock;
        public HashSet<string> LeadOn = new();
        public List<(string id, int target)> Echo = new();
        public HashSet<string> Unsealed = new();
        public Dictionary<string, int> UseCount = new(), Recalled = new();
        public int? SwitchTurn;
        public int KillSeq, CritSeq, GlowSeq, ShockSeq;
        int autoDepth, senseDepth;
        List<string> discardPick;
        public int FirstRushDown, FoeQuiet;
        /// <summary>이 싸움의 적 체력 · 피해 배율 · 엘리트(소환한 적도 같은 눈금).</summary>
        public double EnemyHpx = 1, EnemyDmgx = 1;
        public bool EliteFight;
        public int Drawn;

        // ── 키워드 사전(2026-10-05) ──
        /// <summary>카드에 붙은 상태 — 카드 id → { 독 · 봉쇄 · 침체 · 빙결 · 탐구심 · 비용(±) → 값 }. 같은 id 의 카드는 함께 쓴다.</summary>
        public Dictionary<string, Dictionary<string, int>> CardSt = new();
        /// <summary>빙결 — 카드 id → 낼 수 없는 턴.</summary>
        public Dictionary<string, int> Frozen = new();
        /// <summary>결속 — 카드 id → 겹친 수(없으면 1).</summary>
        public Dictionary<string, int> Bond = new();
        /// <summary>「제거」 태그로 덱에서 완전히 뺄 카드(판이 AfterFight 에서 뺀다).</summary>
        public List<string> Removed = new();
        /// <summary>바로 앞에 낸 카드의 주인(적 패시브 「같은 사도 카드를 연달아」).</summary>
        public string LastHero;
        public int BreakSeq, DealtSeq, DealtAct;
        /// <summary>통계(저장 안 함 — 시뮬 · 시험) — 카드가 강인도를 깎은 횟수 · 그 가운데 약점 공격 · 깎은 강인도 합 · 격파 수.</summary>
        public int ToughHits, ToughWeakHits, Breaks;
        public double ToughDealt;
        /// <summary>이번 턴 낸 카드마다 태그(맨 이름) — 「이번 턴 낸 X 카드 수」.</summary>
        public List<List<string>> PlayTags = new();
        /// <summary>예약 효과(later · afterCards) — 때가 되면 돈다.</summary>
        public List<LaterRec> Later = new();
        /// <summary>이 전투에서 낸 카드 수(저절로 포함) — 예약 효과가 「걸린 카드」 를 가린다.</summary>
        public int PlaysTotal;
        /// <summary>이 전투에서 생긴 판 단위 성장(판이 AfterFight 에서 RunState.Growth 에 더한다).</summary>
        public Dictionary<string, Stats> GrowthGain = new();

        // ── 기록 · 연출 ──
        public List<string> Log = new();
        /// <summary>연출 쪽지 — null 이면 모으지 않는다(봇 · 시뮬). 화면은 new List 를 달거나 OnCue 를 건다.</summary>
        public List<Cue> Cues;
        public event Action<Cue> OnCue;
        /// <summary>기록 한 줄이 생길 때마다.</summary>
        public event Action<string> OnLog;

        Battle(GameData data) { Data = data; }

        // ── 열기 ──────────────────────────────────────────────────────
        public static Battle Start(GameData data, BattleSetup st, List<Cue> cues = null, Action<Cue> onCue = null)
        {
            var s = new Battle(data) { Rng = new Rng(st.Seed), Cues = cues };
            if (onCue != null) s.OnCue += onCue;
            s.Setup(st);
            return s;
        }

        void Setup(BattleSetup st)
        {
            var gear = st.Gear ?? new Dictionary<string, Stats>();
            for (int i = 0; i < st.Party.Count; i++)
            {
                var key = st.Party[i];
                var h = Data.Hero(key) ?? throw new ArgumentException($"없는 사도: {key}");
                var g = gear.TryGetValue(key, out var gg) ? gg : new Stats();
                if (st.Growth != null && st.Growth.TryGetValue(key, out var gr)) g = g + new Stats { Atk = gr.Atk, Def = gr.Def, Crit = gr.Crit };
                Party.Add(new Unit
                {
                    Side = Side.Party, Idx = i, Key = key, Name = h.Name, Role = h.Role, Nature = h.Nature,
                    Share = h.Hp + g.Hp, Atk = h.Atk + g.Atk, Def = h.Def + g.Def, Crit = h.Crit + g.Crit,
                    GearAdd = new Stats { Atk = g.Atk, Def = g.Def, Crit = g.Crit },
                    Row = st.Rows != null && st.Rows.TryGetValue(key, out var row) ? row : h.Row ?? "mid",
                });
            }
            int sumMax = Party.Sum(u => u.Share);
            int pMax = st.PartyMaxHp ?? Math.Max(1, sumMax);
            int pHp = st.PartyHp ?? pMax;
            Pool = new Unit { Side = Side.Party, Key = "party", Name = "파티", Idx = -1 };
            Pool.MaxHp = pMax; Pool.Hp = Math.Min(pMax, Math.Max(0, pHp)); Pool.Dead = Pool.Hp <= 0;
            foreach (var u in Party) u.BodyRef = Pool;

            double hpx = st.EnemyHp ?? R.ENEMY_HP, dmgx = st.EnemyDmg ?? 1;
            EnemyHpx = hpx; EnemyDmgx = dmgx; EliteFight = st.Elite;
            EnemyNature = string.IsNullOrEmpty(st.EnemyNature) ? null : st.EnemyNature;
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                var e = Data.Enemy(st.Enemies[i]) ?? throw new ArgumentException($"없는 적: {st.Enemies[i]}");
                int ehp = Num.Round(e.Hp * hpx);
                double tm = ToughOf(e, st.Elite);
                var u = new Unit { Side = Side.Enemy, Key = e.Id, Name = e.Name, Idx = i, Row = e.Row ?? "front", Nature = FoeNature(e), Boss = e.Boss, Dmgx = dmgx, Tough = tm, ToughMax = tm };
                u.MaxHp = ehp; u.Hp = ehp;
                Enemies.Add(u);
                InitCounters(u);
            }

            Gauge = Num.Clamp(st.Gauge, 0, R.GAUGE_MAX);
            NoNature = st.NoNature;
            Draw = Shuffle(Rng, st.Deck.ToList());
            if (st.CardVals != null) foreach (var kv in st.CardVals) CardSt[kv.Key] = new Dictionary<string, int>(kv.Value);
            Flash = st.Flash != null ? new Dictionary<string, int>(st.Flash) : new();
            Shin = st.Shin != null ? new Dictionary<string, string>(st.Shin) : new();

            gearRules = st.GearRules;
            SetupPassives(st.GearRules);

            // 개전 — 첫 손패에 든다(뽑을 더미는 끝에서부터 뽑으니 끝으로)
            var opening = Draw.Where(id => HasTagB(id, Tag.Opening)).ToList();
            if (opening.Count > 0) Draw = Draw.Where(id => !opening.Contains(id)).Concat(opening).ToList();
            MergePile(Draw, "draw");
            Glow = st.Glow != null ? st.Glow.ToDictionary(kv => kv.Key, kv => kv.Value.Copy()) : new();

            var nx = st.Next;
            if (nx != null)
            {
                if (nx.Ap != 0) { StartAp += nx.Ap; Say($"이벤트 — 첫 턴 AP {(nx.Ap > 0 ? "+" : "")}{nx.Ap}"); }
                if (nx.Gauge != 0) { Gauge = Math.Min(R.GAUGE_MAX, Gauge + nx.Gauge); Say($"이벤트 — 고학년 게이지 +{nx.Gauge}%"); }
                if (nx.Hand != 0) { Opening += nx.Hand; Say($"이벤트 — 첫 손패 +{nx.Hand}"); }
                if (nx.Weak != 0) { AddSt(Pool, "약화", nx.Weak, "event"); Say($"이벤트 — 파티 약화 {nx.Weak}"); }
                if (nx.Buff != null) foreach (var kv in nx.Buff) { if (R.HERO_ST.Contains(kv.Key)) foreach (var u in Party) AddSt(u, kv.Key, kv.Value, "event"); else AddSt(Pool, kv.Key, kv.Value, "event"); Say($"이벤트 — 파티 {kv.Key} {kv.Value}"); }
                if (nx.Rush != 0) { FirstRushDown = nx.Rush; Say($"이벤트 — 첫 턴 적 전체 즉시 행동 {nx.Rush}장 늦춤"); }
                if (nx.FoeVuln != 0) { foreach (var e in AliveEnemies()) AddSt(e, "취약", nx.FoeVuln, "event"); Say($"이벤트 — 적 전체 취약 {nx.FoeVuln}"); }
                if (nx.Quiet != 0) { FoeQuiet = nx.Quiet; Say($"이벤트 — 적 패시브가 {nx.Quiet}턴 동안 잠잠하다"); }
                if (nx.HpCut > 0) { Pool.Hp = Math.Max(1, Pool.Hp - Num.Round(Pool.MaxHp * nx.HpCut)); Say($"이벤트 — 파티 HP -{Num.Round(nx.HpCut * 100)}%"); }
            }
            var opener = AliveParty().FirstOrDefault();
            if (opener != null) Talk(opener, "start");
            Emit("fightStart", new EmitInfo());
            FoePassives("fightStart", null);
            BeginTurn();
            OpeningPlays();
        }

        /// <summary>적의 성격 — 사도 클론은 그 사도의 성격 그대로, 나머지는 판의 적 속성(있으면) · 없으면 데이터.</summary>
        public string FoeNature(EnemyDef e) => e.Clone != null ? (Data.Hero(e.Clone)?.Nature ?? e.Nature) : EnemyNature ?? e.Nature;

        /// <summary>적의 강인도 칸 — 데이터 tough(없으면 일반 · 엘리트 · 보스 기본). 엘리트 싸움에 선 여린 적(엘리트 몸보다 작은 칸)은 +EliteMinion. 최소 R.TOUGH.Min(3).</summary>
        public double ToughOf(EnemyDef e, bool elite)
        {
            double t = e.Tough > 0 ? e.Tough : e.Boss ? R.TOUGH.Boss : elite ? R.TOUGH.Elite : R.TOUGH.Fight;
            if (elite && !e.Boss && t < R.TOUGH.Elite) t += R.TOUGH.EliteMinion;
            return Math.Max(R.TOUGH.Min, t);
        }

        // ── 복사(봇 · 미리보기) ──────────────────────────────────────
        /// <summary>판을 통째로 복사한다 — 기록 · 연출 쪽지 · 구독은 빼고. 난수는 rng(없으면 지금 것을 베낀다).</summary>
        public Battle Clone(Rng rng = null)
        {
            var s = (Battle)MemberwiseClone();
            s.OnCue = null; s.OnLog = null; s.Cues = null;
            s.Log = new List<string>();
            s.Rng = rng ?? Rng.Clone();
            s.Pool = Pool.Clone();
            s.Party = Party.ConvertAll(u => { var c = u.Clone(); c.BodyRef = s.Pool; return c; });
            s.Enemies = Enemies.ConvertAll(u => u.Clone());
            s.Draw = Draw.ToList(); s.Hand = Hand.ToList(); s.Discard = Discard.ToList(); s.Gone = Gone.ToList();
            s.Flash = new(Flash); s.Shin = new(Shin);
            s.Glow = Glow.ToDictionary(kv => kv.Key, kv => kv.Value.Copy());
            s.GainedCards = GainedCards.ToList(); s.GainedFlash = GainedFlash.ToList();
            s.FreeOnce = new(FreeOnce); s.FreeTurn = new(FreeTurn);
            s.Kw = new(Kw); s.Passives = Passives;   // 규칙은 전투 내내 그대로 — 나눠 쓴다
            s.Stacks = Stacks.ToDictionary(kv => kv.Key, kv => new Dictionary<string, int>(kv.Value));
            s.StackCap = StackCap.ToDictionary(kv => kv.Key, kv => new Dictionary<string, int>(kv.Value));
            s.Fired = new(Fired); s.Counts = new(Counts);
            s.Always = Always;
            s.firing = new HashSet<string>();
            s.PlayLog = PlayLog.ToList(); s.PlayedBy = new(PlayedBy); s.PlayIds = PlayIds.ToList(); s.Held = new(Held);
            s.HurtPrev = new(HurtPrev); s.HurtNow = new(HurtNow); s.KillPrev = new(KillPrev); s.KillNow = new(KillNow);
            s.LeadOn = new(LeadOn); s.Echo = Echo.ToList(); s.Unsealed = new(Unsealed);
            s.UseCount = new(UseCount); s.Recalled = new(Recalled);
            s.discardPick = discardPick?.ToList();
            s.CardSt = CardSt.ToDictionary(kv => kv.Key, kv => new Dictionary<string, int>(kv.Value));
            s.Frozen = new(Frozen); s.Bond = new(Bond); s.Removed = Removed.ToList();
            s.PlayTags = PlayTags.Select(x => x.ToList()).ToList();
            s.CostMods = CostMods.Select(x => x.Copy()).ToList(); s.PlayedPrev = new(PlayedPrev); s.BattleVals = new(BattleVals);
            s.Later = Later.Select(x => x.Copy()).ToList();
            s.GrowthGain = GrowthGain.ToDictionary(kv => kv.Key, kv => kv.Value + new Stats());
            s.Forms = Forms.ToDictionary(kv => kv.Key, kv => kv.Value.Copy());   // 모습 캐시(formViews · formRules)는 나눠 쓴다 — 바뀌지 않는다
            return s;
        }

        // ── 작은 손잡이 ───────────────────────────────────────────────
        public IEnumerable<Unit> AliveParty() => Pool.Dead ? Enumerable.Empty<Unit>() : Party;
        public List<Unit> AliveEnemies() => Enemies.Where(e => !e.Dead).ToList();
        public Unit HeroUnit(string key) => key == null ? null : Party.FirstOrDefault(u => u.Key == key);
        /// <summary>파티의 대표 — 파티에 거는 것(방어 · 실드 · 회복 · 상태)의 연출 자리.</summary>
        public Unit PartyRep(Unit prefer = null) => prefer != null && prefer.Side == Side.Party && !prefer.Dead ? prefer : AliveParty().FirstOrDefault() ?? Party.FirstOrDefault();

        static List<string> Shuffle(Rng rng, List<string> a)
        {
            for (int i = a.Count - 1; i > 0; i--) { int j = (int)Math.Floor(rng.Next() * (i + 1)); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        void Say(string t) { Log.Add(t); OnLog?.Invoke(t); }

        void Cue(string k, Unit u, Cue more = null)
        {
            if (u == null || (Cues == null && OnCue == null)) return;
            var c = more ?? new Cue();
            c.K = k; c.Side = u.Side; c.Idx = u.Idx;
            Cues?.Add(c);
            OnCue?.Invoke(c);
        }
        /// <summary>카드 이동 쪽지 — 사람 자리 없이(Side · Idx 는 쓰지 않는다).</summary>
        void CardCue(string id, string from, string to, string why = null)
        {
            if (Cues == null && OnCue == null) return;
            var c = new Cue { K = "card", CardId = id, Pile = from, ToPile = to, Label = why, Idx = -1 };
            Cues?.Add(c); OnCue?.Invoke(c);
        }
        void HealCue(Unit u, int h0, int over = 0) { if (u != null && (u.Hp > h0 || over > 0)) Cue("heal", u, new Cue { V = u.Hp - h0, From = h0, To = u.Hp, Over = over }); }
        void GainCue(Unit u, string k, int v) { if (v > 0) Cue(k, u, new Cue { V = v }); }
        void StatusCue(Unit u, string id, bool up = false) => Cue("status", u, new Cue { Id = id, Up = up });
        /// <summary>사도 대사 때 — 화면이 그 순간의 줄을 고른다(엔진은 난수를 쓰지 않는다).</summary>
        void Talk(Unit hero, string moment) { if (hero != null) Cue("talk", hero, new Cue { Hero = hero.Key, Moment = moment }); }

        static string Sign(int v) => v > 0 ? "+" + v : v.ToString();
    }

    /// <summary>예약 효과 하나 — Turn(그 턴 시작에) 또는 Cards(파티가 카드를 그만큼 더 내면).</summary>
    public sealed class LaterRec
    {
        public int Turn, Cards;
        /// <summary>afterCards — 걸린 카드(그 카드 자신은 세지 않는다).</summary>
        public int Born;
        public string Owner, Name;
        public int Target;
        public List<Fx> Fx = new();
        /// <summary>함정 — 그 적이 공격이 아닌 수를 하면 대신 도는 효과.</summary>
        public List<Fx> Else;
        public LaterRec Copy() => (LaterRec)MemberwiseClone();
    }

    /// <summary>키워드 하나 — 정의와 주인.</summary>
    public sealed class KwRt
    {
        public KeywordDef Def;
        public string Owner;
        public string Id => Def.Name;
        public string Carrier => Def.Carrier ?? "self";
    }

    /// <summary>패시브 규칙 하나 — 키워드 규칙이면 그 키워드가 어디 붙는지(KwOf).</summary>
    public sealed class RuleRt
    {
        public PassiveRule R;
        public string KwOf;
        public bool Gear;
        /// <summary>사도 자신의 패시브(HeroDef.Passives) — 변신 replace 가 끄는 것.</summary>
        public bool Own;
        /// <summary>변신이 덧붙인 규칙이면 그 변신 id.</summary>
        public string Form;
    }

    public sealed class AlwaysMod
    {
        public string Stat;
        public double V;
        public List<Cond> Cond;
        public string Owner;
        public string Name;
    }
}
