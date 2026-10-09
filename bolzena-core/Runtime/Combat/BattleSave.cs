using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 전투 저장 — 내 턴에 카드를 기다리는 자리(쉬는 때)에서만 쓴다. 규칙(패시브 · 키워드)은 데이터에서 다시 건다.
    /// 난수는 그 자리의 다음 수부터 이어진다.
    /// </summary>
    public sealed class BattleSave
    {
        public int V = 1;
        public uint Rng;
        public List<Unit> Party = new();
        public Unit Pool;
        public List<Unit> Enemies = new();
        public int Turn, Ap, ApJam, ApCarry, ApLeft, StartAp, Opening, Gauge, NextCheaper, SeqN, KillSeq, CritSeq, GlowSeq, ShockSeq, FirstRushDown, FoeQuiet, Drawn, PlayedThisTurn;
        public int? SwitchTurn, PreviewPick;
        public string LastUlt, Over, PrevNat;
        public bool NoNature, RushedThisTurn, FinaleLock;
        public List<string> Draw, Hand, Discard, Gone, GainedCards;
        public List<GainedFlashRec> GainedFlash = new();
        public Dictionary<string, int> Flash, FreeTurn, Fired, Counts, PlayedBy, KillPrev, KillNow, UseCount, Recalled;
        public Dictionary<string, string> Shin;
        public Dictionary<string, Glow> Glow;
        public HashSet<string> FreeOnce, HurtPrev, HurtNow, LeadOn, Unsealed;
        public Dictionary<string, Dictionary<string, int>> Stacks;
        public List<PlayRec> PlayLog = new();
        public List<EchoRec> Echo = new();
        public Dictionary<string, List<PassiveRule>> GearRules;
        public List<string> PlayIds;
        public Dictionary<string, int> Held;
        public int ApSpent, PaidHp, DiscardedTurn, TakenNow, TakenPrev;
        public double EnemyHpx = 1, EnemyDmgx = 1, CritDmgX;
        public bool EliteFight;
        public string EnemyNature;
        public int Floor;
        // 2026-10-05 키워드 사전
        public Dictionary<string, Dictionary<string, int>> CardSt;
        public Dictionary<string, int> Frozen, Bond;
        public List<string> Removed;
        public string LastHero;
        public int BreakSeq, DealtSeq, DealtAct;
        public List<List<string>> PlayTags;
        public List<LaterRec> Later;
        public int PlaysTotal, UltsUsed;
        public BattleTape Tape;
        public List<CostModRec> CostMods;
        public Dictionary<string, int> PlayedPrev;
        public HashSet<string> BattleVals;
        public int DebtNow;
        public string ShieldBy;
        public double CutNext;
        public Dictionary<string, Stats> GrowthGain;
        // 2026-10-05 변신
        public Dictionary<string, FormRt> Forms;
        // 2026-10-05 강화 카드 지속 규칙
        public List<PowerRt> Powers;
        // 2026-10-08 18갈래 공용 계기
        public int GoneN;
        public bool FightEndDone;
        // 2026-10-08 1단계 — 막아 낸 양 · 회복 기록
        public int GuardedNow, GuardedPrev;
        public List<int> HealLog;
    }
    public sealed class GainedFlashRec { public string CardId; public int N; public string Shin; }
    public sealed class PlayRec { public string Hero, Type; }
    public sealed class EchoRec { public string Id; public int Target; }

    public sealed partial class Battle
    {
        Dictionary<string, List<PassiveRule>> gearRules;

        public string Save() => GameData.ToJson(Snapshot());

        public BattleSave Snapshot() => new BattleSave
        {
            Rng = Rng.State, Party = Party, Pool = Pool, Enemies = Enemies,
            Turn = Turn, Ap = Ap, ApJam = ApJam, ApCarry = ApCarry, ApLeft = ApLeft, StartAp = StartAp, Opening = Opening, Gauge = Gauge, NextCheaper = NextCheaper,
            SeqN = SeqN, KillSeq = KillSeq, CritSeq = CritSeq, GlowSeq = GlowSeq, ShockSeq = ShockSeq, FirstRushDown = FirstRushDown, FoeQuiet = FoeQuiet, Drawn = Drawn,
            PlayedThisTurn = PlayedThisTurn, SwitchTurn = SwitchTurn, PreviewPick = PreviewPick, LastUlt = LastUlt, Over = Over, PrevNat = PrevNat,
            NoNature = NoNature, RushedThisTurn = RushedThisTurn, FinaleLock = FinaleLock,
            Draw = Draw, Hand = Hand, Discard = Discard, Gone = Gone, GainedCards = GainedCards,
            GainedFlash = GainedFlash.Select(g => new GainedFlashRec { CardId = g.cardId, N = g.n, Shin = g.shin }).ToList(),
            Flash = Flash, FreeTurn = FreeTurn, Fired = Fired, Counts = Counts, PlayedBy = PlayedBy, KillPrev = KillPrev, KillNow = KillNow, UseCount = UseCount, Recalled = Recalled,
            Shin = Shin, Glow = Glow, FreeOnce = FreeOnce, HurtPrev = HurtPrev, HurtNow = HurtNow, LeadOn = LeadOn, Unsealed = Unsealed, Stacks = Stacks,
            PlayLog = PlayLog.Select(p => new PlayRec { Hero = p.hero, Type = p.type }).ToList(),
            Echo = Echo.Select(e => new EchoRec { Id = e.id, Target = e.target }).ToList(),
            GearRules = gearRules,
            EnemyHpx = EnemyHpx, EnemyDmgx = EnemyDmgx, CritDmgX = CritDmgX, EliteFight = EliteFight, EnemyNature = EnemyNature, Floor = Floor,
            CardSt = CardSt, Frozen = Frozen, Bond = Bond, Removed = Removed, LastHero = LastHero, BreakSeq = BreakSeq, DealtSeq = DealtSeq, DealtAct = DealtAct, PlayTags = PlayTags, Later = Later, GrowthGain = GrowthGain, PlaysTotal = PlaysTotal, UltsUsed = UltsUsed, Tape = Tape, CostMods = CostMods, PlayedPrev = PlayedPrev, BattleVals = BattleVals, DebtNow = DebtNow, ShieldBy = ShieldBy, CutNext = CutNext, Forms = Forms.Count > 0 ? Forms : null, Powers = Powers.Count > 0 ? Powers : null,
            PlayIds = PlayIds, Held = Held, ApSpent = ApSpent, PaidHp = PaidHp, DiscardedTurn = DiscardedTurn, TakenNow = TakenNow, TakenPrev = TakenPrev,
            GoneN = GoneN, FightEndDone = FightEndDone,
            GuardedNow = GuardedNow, GuardedPrev = GuardedPrev, HealLog = HealLog.Count > 0 ? HealLog : null,
        };

        /// <summary>저장한 전투를 되살린다.</summary>
        public static Battle Load(GameData data, string json, List<Cue> cues = null)
        {
            var x = GameData.FromJson<BattleSave>(json);
            var s = new Battle(data) { Rng = new Rng(1) { State = x.Rng }, Cues = cues };
            s.Pool = x.Pool; s.Party = x.Party; s.Enemies = x.Enemies;
            foreach (var u in s.Party) u.BodyRef = s.Pool;
            s.Turn = x.Turn; s.Ap = x.Ap; s.ApJam = x.ApJam; s.ApCarry = x.ApCarry; s.ApLeft = x.ApLeft; s.StartAp = x.StartAp; s.Opening = x.Opening; s.Gauge = x.Gauge;
            s.NextCheaper = x.NextCheaper; s.SeqN = x.SeqN; s.KillSeq = x.KillSeq; s.CritSeq = x.CritSeq; s.GlowSeq = x.GlowSeq; s.ShockSeq = x.ShockSeq;
            s.FirstRushDown = x.FirstRushDown; s.FoeQuiet = x.FoeQuiet; s.Drawn = x.Drawn; s.PlayedThisTurn = x.PlayedThisTurn; s.SwitchTurn = x.SwitchTurn; s.PreviewPick = x.PreviewPick;
            s.LastUlt = x.LastUlt; s.Over = x.Over; s.PrevNat = x.PrevNat; s.NoNature = x.NoNature; s.RushedThisTurn = x.RushedThisTurn; s.FinaleLock = x.FinaleLock;
            s.Draw = x.Draw ?? new(); s.Hand = x.Hand ?? new(); s.Discard = x.Discard ?? new(); s.Gone = x.Gone ?? new(); s.GainedCards = x.GainedCards ?? new();
            s.GainedFlash = x.GainedFlash.Select(g => (g.CardId, g.N, g.Shin)).ToList();
            s.Flash = x.Flash ?? new(); s.FreeTurn = x.FreeTurn ?? new(); s.PlayedBy = x.PlayedBy ?? new(); s.KillPrev = x.KillPrev ?? new(); s.KillNow = x.KillNow ?? new();
            s.UseCount = x.UseCount ?? new(); s.Recalled = x.Recalled ?? new(); s.Shin = x.Shin ?? new(); s.Glow = x.Glow ?? new();
            s.FreeOnce = x.FreeOnce ?? new(); s.HurtPrev = x.HurtPrev ?? new(); s.HurtNow = x.HurtNow ?? new(); s.LeadOn = x.LeadOn ?? new(); s.Unsealed = x.Unsealed ?? new();
            s.PlayLog = x.PlayLog.Select(p => (p.Hero, p.Type)).ToList();
            s.Echo = x.Echo.Select(e => (e.Id, e.Target)).ToList();
            s.PlayIds = x.PlayIds ?? new(); s.Held = x.Held ?? new();
            s.EnemyHpx = x.EnemyHpx; s.EnemyDmgx = x.EnemyDmgx; s.CritDmgX = x.CritDmgX; s.EliteFight = x.EliteFight; s.EnemyNature = x.EnemyNature; s.Floor = x.Floor;
            s.ApSpent = x.ApSpent; s.PaidHp = x.PaidHp; s.DiscardedTurn = x.DiscardedTurn; s.TakenNow = x.TakenNow; s.TakenPrev = x.TakenPrev;
            s.CardSt = x.CardSt ?? new(); s.Frozen = x.Frozen ?? new(); s.Bond = x.Bond ?? new(); s.Removed = x.Removed ?? new(); s.LastHero = x.LastHero;
            s.BreakSeq = x.BreakSeq; s.DealtSeq = x.DealtSeq; s.DealtAct = x.DealtAct; s.PlayTags = x.PlayTags ?? new(); s.Later = x.Later ?? new(); s.PlaysTotal = x.PlaysTotal; s.UltsUsed = x.UltsUsed; s.Tape = x.Tape; s.CostMods = x.CostMods ?? new(); s.PlayedPrev = x.PlayedPrev ?? new(); s.BattleVals = x.BattleVals ?? new(); s.DebtNow = x.DebtNow; s.ShieldBy = x.ShieldBy; s.CutNext = x.CutNext; s.GrowthGain = x.GrowthGain ?? new();
            s.Forms = x.Forms ?? new();
            s.Powers = x.Powers ?? new();
            s.GoneN = x.GoneN; s.FightEndDone = x.FightEndDone;
            s.GuardedNow = x.GuardedNow; s.GuardedPrev = x.GuardedPrev; s.HealLog = x.HealLog ?? new();
            s.gearRules = x.GearRules;
            s.SetupPassives(x.GearRules);   // 규칙 · 키워드 · 항상 — 데이터에서 다시
            s.Stacks = x.Stacks ?? new(); s.Fired = x.Fired ?? new(); s.Counts = x.Counts ?? new();
            return s;
        }
    }
}
