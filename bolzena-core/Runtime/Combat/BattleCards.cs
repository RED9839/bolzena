using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    public sealed class AutoInfo
    {
        public Unit Target;
        public string Hero;
        public int Cost;
    }

    /// <summary>미리보기 — 적 하나에 깎일 체력 · 방어/실드 · 처치 · 강인도 · 격파.</summary>
    public sealed class PreviewFoe { public int Hp, Guard; public bool Kill, Max, Brk; public double Tough; }
    /// <summary>미리보기 — 파티(한 몸).</summary>
    public sealed class PreviewParty { public int Heal, Block, Shield, Lose, Over; }

    public sealed partial class Battle
    {
        // ── 카드의 지금 모습 ───────────────────────────────────────────
        /// <summary>이 전투에서 그 카드가 실제로 무엇인가 — 신탁을 골랐으면 바뀐 쪽.</summary>
        public CardView CardOf(string id)
        {
            var v = Data.View(id, Flash.TryGetValue(id, out var n) ? n : 0, Party.Count > 0 ? Party[0].Key : null);   // 주인 없는 교주 카드(옛 저장)는 첫 사도의 카드
            // 결속 — 겹친 수가 3 이상이면 사도마다 정한 「강해진 카드」 의 모습
            if (v != null && Bond.Count > 0 && v.Def.BondCard != null && Bond.TryGetValue(id, out var b) && b >= 3) v = Data.View(v.Def.BondCard) ?? v;
            // 축복 — 피해를 더하는 축복을 받은 스킬은 공격 카드(변신 덤의 종류 거르개도 이것을 보게 변신 앞에, 카드 바꾸기 뒤에 한 번 더)
            v = Blessed(id, v);
            // 변신 — 그 사도가 변신 중이면 변신판 모습(카드 바꾸기 · 덤)
            if (Forms.Count > 0) v = Blessed(id, FormCard(id, v));
            if (stageOn != false && Kw.Count > 0) v = StageCard(id, v);   // 단계 카드 덤(▶E75)
            if (ProgOf != null && v?.Hero != null) v = ProgCard(id, v);   // 영구 성장 — 잠재력 사도 칸의 카드 덤(없으면 그대로)
            return v;
        }

        // 잠재력 사도 칸 카드 덤 — 모습 캐시(바뀌지 않는 덤이라 같은 바탕 모습이면 같은 결과 · 복사본과 나눠 쓴다)
        readonly Dictionary<CardView, CardView> progViews = new();
        CardView ProgCard(string id, CardView v)
        {
            if (!ProgOf.TryGetValue(v.Hero, out var pm) || pm.Cards == null || pm.Cards.Count == 0) return v;
            if (progViews.TryGetValue(v, out var hit)) return hit;
            var o = Prog.CardWith(Data, v, GameData.BaseId(id), pm.Cards);
            progViews[v] = o;
            return o;
        }

        /// <summary>그 카드(id)에 얹힌 그 카드만의 축복까지 본 모습 — 카드 종류는 늘 이것(CardOf)으로 읽는다.</summary>
        CardView Blessed(string id, CardView v) =>
            v == null || Shin.Count == 0 || !Shin.TryGetValue(id, out var sh) ? v : GameData.WithBless(v, BlessOf(Data.Card(id), sh));

        /// <summary>그 카드에 얹힌 그 카드만의 축복(shin = own · own1 · own2).</summary>
        public static BlessDef BlessOf(CardDef c, string shin)
        {
            int i = shin == "own" ? 0 : shin == "own1" ? 1 : shin == "own2" ? 2 : -1;
            return i >= 0 && c != null && i < c.Blesses.Count ? c.Blesses[i] : null;
        }
        /// <summary>축복의 배율 꼴(power · cost · …) — 공용 축복이면 그 이름, 그 카드만의 축복이면 그 축복의 kind.</summary>
        public static string ShinKind(CardDef c, string shin) =>
            shin == null ? null : shin.StartsWith("own", StringComparison.Ordinal) ? BlessOf(c, shin)?.Kind : shin;
        public string ShinOf(string id) => Shin.TryGetValue(id, out var s) ? s : null;

        bool BlessTag(string cardId, string tag)
        {
            var b = BlessOf(Data.Card(cardId), ShinOf(cardId));
            return b != null && b.Tags.Any(t => Tag.Parse(t).id == tag);
        }
        /// <summary>그 카드(손의 id)에 태그가 붙었나 — 카드 글의 태그 또는 고른 축복의 태그.</summary>
        public bool HasTagB(string id, string tag) => (CardOf(id)?.HasTag(tag) ?? false) || BlessTag(id, tag) || KwTag(id, tag) || GameData.BaseId(id) == tag || (tag == Tag.Keep && KeepByVal(id));   // 붙은 동안 보존(엘다인 3차)   // 생성물 카드 id 도 갈래 태그로(perTag · 거르개 tag — 「케이크」 세기, 2026-10-07)

        HashSet<string> CardHitTags(string id, CardView c)
        {
            var o = new HashSet<string>();
            foreach (var t in new[] { Tag.Crush, Tag.Weak, Tag.Passion }) if (c.HasTag(t) || BlessTag(id, t)) o.Add(t);
            if (c.HasTag(Tag.WeakHit) || BlessTag(id, Tag.WeakHit) || KwTag(id, Tag.WeakHit)) o.Add(Tag.Weak);   // 「약점 공격」 = 옛 「약점」
            return o;
        }

        static readonly Dictionary<string, string> WHEN_KO = new() { ["draw"] = "영감", ["discard"] = "안식", ["handEnd"] = "턴 끝", ["drawAny"] = "감응", ["burn"] = "소각", ["passion"] = "열정" };

        /// <summary>카드의 때 붙은 효과(영감 · 안식 · 턴 끝에 손에 있으면) — 카드는 제자리에.</summary>
        void CardWhen(string id, string on)
        {
            var c = CardOf(id);
            var owner = c.Hero != null ? Party.FirstOrDefault(u => u.Key == c.Hero && !u.Dead) : null;
            if (c.Hero != null && owner == null) return;
            var (prev, src0, seq0) = (Acting, ModSrc, ActSeq);
            Acting = c.Hero; ModSrc = $"「{c.Name}」 {WHEN_KO[on]}";
            ActSeq = ++SeqN;
            Say($"「{c.Name}」 — {(on == "handEnd" ? "손에 남아" : WHEN_KO[on])}");
            if (on != "handEnd") StatusCue(owner ?? AliveParty().FirstOrDefault(), $"{WHEN_KO[on]} 「{c.Name}」", !c.IsStatus && !c.IsCurse);
            int tgt = AliveEnemies().FirstOrDefault()?.Idx ?? 0;
            try { RunFx(c.Fx, new FxCtx { Owner = owner, TargetIdx = tgt, HitTags = CardHitTags(id, c), Card = true, Type = c.Type, When = on, CardId = id, Made = GameData.IsPlain(id), Cost = c.Cost }); }
            finally { Acting = prev; ModSrc = src0; ActSeq = seq0; }
            CheckOver();
        }

        /// <summary>뽑는다. ability — 카드 · 패시브의 효과로 뽑기(영감이 깨어난다). 손은 열 장까지 — 넘치면 사라진다.</summary>
        public void DrawCards(int n, bool ability = true)
        {
            int burned = 0;
            for (int i = 0; i < n; i++)
            {
                if (Draw.Count == 0)
                {   // 섞기 — 이번 턴에 낸 카드는 돌아가지 않는다(버린 카드가 없으면 그만)
                    if (!Reshuffle()) break;
                    Emit("shuffle", new EmitInfo()); if (Over != null) break;
                    if (Draw.Count == 0) break;   // 「섞으면」 계기(우로스 「순환」 드로우 등)가 섞인 카드를 먼저 가져갔다 — 뽑을 게 없으면 못 뽑는다
                }
                var id = Draw[Draw.Count - 1]; Draw.RemoveAt(Draw.Count - 1);
                Drawn++;
                if (Hand.Count >= R.HAND_MAX) { Gone.Add(id); burned++; CardCue(id, "draw", "gone", "burn"); continue; }
                Hand.Add(id); CardCue(id, "draw", "hand");
                if (!AfterDraw(id, ability)) break;
            }
            if (burned > 0) Say($"손이 가득 차 {burned}장이 사라졌다 (최대 {R.HAND_MAX}장)");
            MergePile(Hand, "hand");
        }

        /// <summary>드로우로 손에 막 들어온 카드 — 빙결 · 감응 · 「카드가 뽑히면」 · 영감. 뽑을 더미 → 손만 드로우다(생성 · 버린 더미에서 꺼내기는 아니다 — Docs/카드이동.md). 전투가 끝나면 false.</summary>
        bool AfterDraw(string id, bool ability)
        {
            var c = CardOf(id);
            // 빙결 — 뽑은 턴에는 낼 수 없다
            if (CardStOf(id, "빙결") > 0) { DelCardSt(id, "빙결"); Frozen[id] = Turn; Say($"「{c?.Name}」 — 빙결(이번 턴 낼 수 없다)"); }
            // 감응 — 드로우하면(턴 시작 드로우 · 효과 드로우 · 뽑을 더미에서 손으로 꺼내기)
            if (c != null && c.Fx.Any(f => f.K == FxK.When && f.On == "drawAny") && senseDepth < 3)
            {
                senseDepth++;
                try { CardWhen(id, "drawAny"); } finally { senseDepth--; }
                if (Over != null) return false;
            }
            Emit("drawn", new EmitInfo { Hero = c?.Hero, Id = id, Type = c?.Type, Tags = c?.Tags });
            if (Over != null) return false;
            // 적이 끼워 넣은 상태 카드 · 저주는 턴 시작에 뽑혀도 돈다
            if (c != null && (ability || c.IsStatus || c.IsCurse) && c.Fx.Any(f => f.K == FxK.When && f.On == "draw") && senseDepth < 3)
            {
                senseDepth++;
                try { CardWhen(id, "draw"); } finally { senseDepth--; }
                if (Over != null) return false;
            }
            return true;
        }

        /// <summary>버린 더미에서 이번 턴에 낸 카드(Used)의 자리 — 같은 이름은 뒤(나중에 들어온 것)부터 짝짓는다.</summary>
        HashSet<int> UsedAt()
        {
            var at = new HashSet<int>();
            if (Used.Count == 0) return at;
            var left = new Dictionary<string, int>();
            foreach (var u in Used) left[u] = (left.TryGetValue(u, out var n) ? n : 0) + 1;
            for (int i = Discard.Count - 1; i >= 0; i--)
                if (left.TryGetValue(Discard[i], out var n) && n > 0) { left[Discard[i]] = n - 1; at.Add(i); }
            return at;
        }

        /// <summary>섞기 — 버린 카드를 뽑을 더미로 되돌려(남은 뽑을 더미와 함께) 섞는다. 이번 턴에 낸 카드는 버린 더미에 남는다(카제나 — 무한 순환 막기). 돌아온 카드가 없으면 false.</summary>
        bool Reshuffle()
        {
            var used = UsedAt();
            if (Discard.Count == 0 || used.Count >= Discard.Count) return false;
            var free = new List<string>(Discard.Count - used.Count);
            var stay = new List<string>(used.Count);
            for (int i = 0; i < Discard.Count; i++) (used.Contains(i) ? stay : free).Add(Discard[i]);
            Discard.Clear(); Discard.AddRange(stay);
            Draw = Shuffle(Rng, Draw.Count == 0 ? free : Draw.Concat(free).ToList());
            CardCue(null, "discard", "draw", "shuffle");
            return true;
        }

        // ── 고학년 스킬 ────────────────────────────────────────────────
        public UltDef UltOf(string heroKey) => Data.Hero(heroKey)?.Ult;

        /// <summary>쓸 수 없으면 까닭, 쓸 수 있으면 null.</summary>
        public string CanUlt(string heroKey)
        {
            var u = HeroUnit(heroKey);
            if (u == null || u.Dead) return "나설 수 없다";
            var ult = UltOf(heroKey);
            if (ult == null) return "고학년 스킬이 없다";
            int uc = UltCostOf(heroKey);   // 요구 게이지 증감(엘다인 4차 ▶E78)
            if (Gauge < uc) return $"게이지가 모자라다 ({Gauge}% / {uc}%)";
            if (St(Pool, "정신 붕괴") > 0) return "정신 붕괴 — 고학년 스킬을 쓸 수 없다";
            return null;
        }

        /// <summary>stage — 단계를 고르는 쌓기(stack pick)가 든 고학년의 고른 단계(PlayOpts.Stage 와 같음 · 없으면 다음 단계, 2026-10-13).</summary>
        public PlayResult UseUlt(string heroKey, int targetIdx = 0, int? stage = null)
        {
            Input?.Ult(this, heroKey, targetIdx, stage);   // 입력 테이프(BattleInput.cs) — 못 써도 적는다(다시 돌리면 같은 까닭으로 못 쓴다)
            var why = CanUlt(heroKey);
            if (why != null) return PlayResult.Fail(why);
            var ult = UltOf(heroKey);
            int cost = UltCostOf(heroKey);
            Gauge -= cost;
            LastUlt = heroKey;
            UltsUsed++; Tape?.Ult(this);
            var owner = HeroUnit(heroKey);
            Say($"{owner.Name} 고학년 스킬 — {ult.Name} (게이지 {cost}%)");
            Talk(owner, "ego");
            Cue("act", owner, new Cue { Anim = "ult", Name = ult.Name });
            var (prev, src0) = (Acting, ModSrc);
            Acting = heroKey; ModSrc = $"{owner.Name} 「{ult.Name}」";
            ActSeq = ++SeqN;
            var tags = new HashSet<string>();
            RunFx(ult.Fx, new FxCtx { Owner = owner, TargetIdx = targetIdx, HitTags = tags, Card = true, Cost = 2, ToughUnit = R.ToughUnitUlt(ult.Cost), StagePick = stage });
            Acting = prev; ModSrc = src0;
            if (St(Pool, "계몽") > 0 && Over == null) { Say("계몽 — 드로우 1"); DrawCards(1); }
            Emit("ult", new EmitInfo { Hero = heroKey });
            CheckOver();
            return new PlayResult { Ok = true };
        }

        // ── 코스트 ─────────────────────────────────────────────────────
        /// <summary>은총으로 얻은 카드의 「그 턴 코스트 0」 — 그 장수만큼, 손의 같은 카드 가운데 앞에서부터.</summary>
        public bool GraceFree(string cardId, int? handIdx)
        {
            int n = FreeTurn.TryGetValue(cardId, out var x) ? x : 0;
            if (n <= 0) return false;
            if (handIdx == null || handIdx < 0 || handIdx >= Hand.Count || Hand[handIdx.Value] != cardId) return true;
            int before = 0;
            for (int i = 0; i < handIdx; i++) if (Hand[i] == cardId) before++;
            return before < n;
        }

        /// <summary>비용이 올라간 까닭(화면 풀이용) — 침체 · 카드에 붙은 비용 · 적이 바꾼 비용. 안 올랐으면 null.</summary>
        public string CostUpWhy(string cardId)
        {
            if (CardStOf(cardId, "침체") > 0) return "침체";
            if (CardStOf(cardId, "비용") > 0) return "비용 증가 효과";
            if (CostMods.Count > 0 && CostModOf(cardId) > 0) return "적이 바꾼 비용";
            if (CostIfAdd(CardOf(cardId), cardId, null) > 0) return $"손의 「{Data.Card(CardOf(cardId).CostIf.InHand)?.Name}」";   // (2026-10-13 이드) costIf
            return null;
        }
        /// <summary>(2026-10-13 이드) costIf — 손에 그 카드(이 카드 자신은 빼고)가 있으면 비용 +add. 아니면 0.</summary>
        int CostIfAdd(CardView c, string cardId, int? handIdx)
        {
            var ci = c?.CostIf;
            if (ci?.InHand == null) return 0;
            for (int i = 0; i < Hand.Count; i++)
                if (i != handIdx && Hand[i] != cardId && GameData.BaseId(Hand[i]) == ci.InHand) return ci.Add;
            return 0;
        }
        /// <summary>영구 성장 비용 덤(잠재력 사도 칸) — 그 카드 주인의 카드 덤(ProgMods.Cards) 가운데 그 카드에 맞는 비용 덤 합.</summary>
        int ProgCostOf(string cardId)
        {
            var def = Data.Card(cardId);
            if (def?.Hero == null || !ProgOf.TryGetValue(def.Hero, out var pm) || pm.Cards == null) return 0;
            return Prog.CostOfMods(CardOf(cardId), GameData.BaseId(cardId), pm.Cards);
        }

        /// <summary>영구 성장 카드군 덤 — 그 사도가 낸 카드가 고유(복제 · 생성 카드 포함 — 사도 카드이고 고유이거나 생성) · 기본(사도 카드, 고유 아님) · 교주(주인을 붙인 중립 카드)인지 보고 그 몫.</summary>
        double ProgCardOf(string ownerKey, string cardId)
        {
            if (!ProgOf.TryGetValue(ownerKey, out var pm)) return 0;
            var def = Data.Card(cardId);
            if (def == null) return 0;
            if (def.Hero != null) return def.Unique || def.Token ? pm.Unique : pm.Basic;
            return GameData.OwnerOf(cardId) != null ? pm.Neutral : 0;
        }

        public int CostOf(string cardId, int? handIdx = null)
        {
            var c = CardOf(cardId);
            if (FreeOnce.Contains(cardId) || GraceFree(cardId, handIdx)) return 0;
            int divine = ShinKind(Data.Card(cardId), ShinOf(cardId)) == "cost" ? 1 : 0;
            int lead = LeadOn.Contains(cardId) && PlayedThisTurn <= 0 ? 1 : 0;
            int adj = (CardStOf(cardId, "침체") > 0 ? 1 : 0) + CardStOf(cardId, "비용") + (CostMods.Count > 0 ? CostModOf(cardId) : 0) + CardValueCost(cardId) + CostIfAdd(c, cardId, handIdx) + (ProgOf != null ? ProgCostOf(cardId) : 0);   // 영구 성장 — 잠재력 사도 칸 비용 덤   // costIf — 손에 그 카드가 있으면 +N(2026-10-13 이드)   // 카드 값 1당 비용(엘다인 4차 리뉴아 「시간」)   // 침체 +1 · 적이 바꾼 비용(착란 …)
            return Math.Max(0, c.Cost - divine - lead - NextCheaper + adj);
        }

        // ── 신탁 ───────────────────────────────────────────────────────
        /// <summary>빛나는 카드의 신탁 — 정신 붕괴 중에는 없다(그 카드는 맨 카드로 나간다 · 빛은 남는다).</summary>
        public Glow GlowOf(string cardId) => St(Pool, "정신 붕괴") > 0 ? null : Glow.TryGetValue(cardId, out var g) ? g : null;

        /// <summary>빛나는 카드의 신탁을 고른다 — choice 는 선택지 번호. 카드 신탁은 그 카드가 바로 바뀌고 이번에는 코스트 0, 은총은 고유 카드가 손에(그 턴 코스트 0).</summary>
        public string ApplyEpiphany(string cardId, int choice)
        {
            Input?.Epi(this, cardId, choice);   // 입력 테이프(BattleInput.cs)
            var g = GlowOf(cardId);
            if (g == null || choice < 0 || choice >= g.Count) return null;
            Glow.Remove(cardId);
            Tape?.Epi(this, cardId, g, choice);
            Cue("epiphany", HeroUnit(CardOf(cardId)?.Hero) ?? PartyRep(),new Cue { CardId = cardId, Id = g.Kind, V = choice, Hero = g.Hero });
            if (g.Kind == "card")
            {
                var o = g.Picks[choice];
                Flash[cardId] = o.N;
                if (o.Shin != null) Shin[cardId] = o.Shin;
                FreeOnce.Add(cardId);
                GainedFlash.Add((cardId, o.N, o.Shin));
                var od = Data.Card(cardId).Oracles[o.N - 1];
                Say($"신탁! 「{Data.Card(cardId).Name}」 → 신탁{(o.Shin != null ? " · 축복" : "")}");
            }
            else
            {
                var opt = g.Options[choice];
                if (Hand.Count < R.HAND_MAX) { Hand.Add(opt); CardCue(opt, "new", "hand", "grace"); } else { Discard.Add(opt); CardCue(opt, "new", "discard", "grace"); }
                FreeTurn[opt] = (FreeTurn.TryGetValue(opt, out var n) ? n : 0) + 1;
                GainedCards.Add(opt);
                Say($"은총! {Data.Hero(g.Hero)?.Name} — 「{Data.Card(opt).Name}」");
            }
            return g.Kind;
        }

        // ── 내기 ───────────────────────────────────────────────────────
        /// <summary>낼 수 없으면 까닭(화면이 그대로 보인다), 낼 수 있으면 null.</summary>
        public string CanPlay(string cardId, bool free = false, int? handIdx = null)
        {
            var c = CardOf(cardId);
            if (c == null) return "그런 카드가 없다";
            if (c.HasTag(Tag.Unplayable)) return "낼 수 없는 카드다";
            if (FinaleLock) return "종극 — 이번 턴은 끝났다";
            if (IsFrozen(cardId)) return "빙결 — 뽑은 턴에는 낼 수 없다";
            var def = Data.Card(cardId);
            if (!free)
            {
                int cost = CostOf(cardId, handIdx);
                if (def?.PayWith != null)
                {
                    int rate = Math.Max(1, def.PayRate), have = StackOf(c.Hero, def.PayWith) / rate;
                    if (def.PayMix ? have + Ap < cost : have < cost) return $"{Ko.J("「" + def.PayWith + "」", "이가")} 모자라다";
                }
                else if (cost > Ap && !(def?.Debt ?? false)) return "AP가 모자라다";
            }
            var owner = c.Hero != null ? HeroUnit(c.Hero) : null;
            if (c.Hero != null && (owner == null || owner.Dead)) return $"{Data.Hero(c.Hero)?.Name ?? c.Hero} — 나설 수 없다";
            if (NeedFormWhy(c) is string nf) return $"「{nf}」 중에만 낼 수 있다";   // 태세일 때만 사용 가능(엘다인 4차)
            return null;
        }

        /// <summary>연속(같은 성격) — 공명 성격 카드는 어느 성격과도 잇는다(엘다인 4차 D14 · ▶E84).</summary>
        public static bool ChainNat(string prev, string nat) => nat != null && prev != null && (prev == nat || nat == "공명" || prev == "공명");

        /// <summary>손의 handIdx 번째 카드를 낸다. targetIdx — 적의 idx(아군을 고르는 카드면 사도 idx).</summary>
        public PlayResult PlayCard(int handIdx, int targetIdx, PlayOpts opts = null)
        {
            opts ??= new PlayOpts();
            if (Input != null && !opts.Auto) Input.Play(this, handIdx, targetIdx, opts);   // 입력 테이프(BattleInput.cs) — 저절로 내는 카드(Auto)는 결과라 적지 않는다
            if (Over != null) return PlayResult.Fail("전투가 끝났다");
            if (handIdx < 0 || handIdx >= Hand.Count) return PlayResult.Fail("그런 카드가 없다");
            var cardId = Hand[handIdx];
            var why = CanPlay(cardId, false, handIdx);
            if (why != null) return PlayResult.Fail(why);

            var c = CardOf(cardId);
            TraceKeepPlay(cardId, c.Hero);   // 쥐고 넘긴 보존 카드를 냄 — 때가 맞았나(▶E87 측정)
            ActSeq = ++SeqN;
            PlaysTotal++;
            Tape?.Card(this, cardId);
            var owner = c.Hero != null ? HeroUnit(c.Hero) : null;
            bool grace = !FreeOnce.Contains(cardId) && GraceFree(cardId, handIdx);
            var def0 = Data.Card(cardId);
            int cost0 = c.X ? Ap : CostOf(cardId, handIdx);
            int costUp = c.X ? 0 : CostIfAdd(c, cardId, handIdx);   // (2026-10-13 이드) costIf 로 오른 비용 — 천상 · 「비용 N 이상」 계기도 오른 비용으로 센다
            int paid = cost0;
            bool payKw = def0?.PayWith != null && !c.X;
            if (payKw)
            {   // 고유 효과로 치르기 — rate 개가 AP 1, 섞어 치르기면 모자란 만큼 AP
                int rate = Math.Max(1, def0.PayRate), kwPay = Math.Min(cost0, StackOf(c.Hero, def0.PayWith) / rate);
                if (kwPay > 0 && owner != null) SpendFx(new Fx { K = FxK.Spend, Id = def0.PayWith, V = kwPay * rate }, new FxCtx { Owner = owner });
                paid = def0.PayMix ? cost0 - kwPay : 0;
            }
            else if (cost0 > Ap) { ApJam += cost0 - Ap; DebtNow += cost0 - Ap; Say($"AP 빚 {cost0 - Ap} — 다음 턴 AP 가 그만큼 줄어든다"); paid = Ap; }
            if (CostMods.Count > 0) UseCostMods(cardId);
            bool tune = !c.X && !payKw && paid == Ap;
            Ap -= paid;
            ApSpent += paid;
            if (c.X && owner?.Key == TraceHero && TraceHero != null)
            {   // (10-22 에르핀) X 카드에 쓴 AP · 그때 쌓인 X 겹(xStack) — bz sim --trace
                TraceAdd("xPlay"); TraceAdd("xAp", paid); TraceAdd("xAp:" + paid);
                foreach (var xs in c.Fx.Where(f => f.XStack != null).Select(f => f.XStack).Distinct()) TraceAdd("xStack", StackOf(owner.Key, xs));
            }
            // 직전보다 비싼가(시범 16 리코타) — 이번 턴 바로 앞 카드의 비용과 견준다(턴 첫 카드는 아니다)
            int prevCost = Counts.TryGetValue("lastCostT", out var lct) && lct == Turn && Counts.TryGetValue("lastCost", out var lcv) ? lcv : -1;
            bool pricier = prevCost >= 0 && cost0 > prevCost;
            Counts["lastCost"] = cost0; Counts["lastCostT"] = Turn; Counts["pricier"] = pricier ? 1 : 0;
            int held = Held.TryGetValue(cardId, out var hv) ? hv : 0;
            int playedBefore = PlayedThisTurn;
            string baseId = GameData.BaseId(cardId);
            bool repeat = PlayIds.Count > 0 && PlayIds[PlayIds.Count - 1] == baseId;
            if (paid > 0 && !GaugeLocked(c.Hero)) Gauge = Math.Min(R.GAUGE_MAX, Gauge + paid * R.GAUGE_PER_AP);   // 게이지 잠금(엘다인 3차 ▶E51)
            NextCheaper = 0;
            FreeOnce.Remove(cardId);
            if (grace) { int left = (FreeTurn.TryGetValue(cardId, out var ft) ? ft : 1) - 1; if (left > 0) FreeTurn[cardId] = left; else FreeTurn.Remove(cardId); }
            Hand.RemoveAt(handIdx);
            CardCue(cardId, "hand", "play");
            discardPick = opts.Discard?.ToList();
            spreadPick = opts.Spread?.ToList();

            var sh = ShinOf(cardId);
            var nat = owner != null ? NatureOf(owner) : null;
            var last = PlayLog.Count > 0 ? PlayLog[PlayLog.Count - 1] : ((string, string)?)null;
            var ctx = new FxCtx
            {
                Owner = owner, TargetIdx = targetIdx, AllyIdx = opts.Ally, X = c.X ? paid : 0, Shin = ShinKind(Data.Card(cardId), sh),
                HitTags = CardHitTags(cardId, c), Card = true, Type = c.Type, Chain = ChainNat(PrevNat, nat), Tune = tune,
                Link = c.Hero != null && last != null && last.Value.Item1 == c.Hero, Prev = last?.Item2,
                Repeat = repeat, Held = held, PlayedBefore = playedBefore,
                CardId = cardId, Made = GameData.IsPlain(cardId), Cost = c.X ? paid : cost0 /* 강인도 — 낼 때의 비용(2026-10-12 사용자 「카드를 낼 때의 비용으로」 · 옛: 적힌 비용) */, Choice = opts.Choice ?? 0, Scale = opts.Scale ?? 1, SpendPick = opts.Spend, Pricier = pricier,
                ForceCrit = !c.IsStatus && !c.IsCurse && UseCritNext(c.Hero), StagePick = opts.Stage,
                NthForce = !c.IsStatus && !c.IsCurse && UseNthNext(c.Hero), CostNow = cost0,
                CritFoe = c.Fx.FirstOrDefault(x => x.K == FxK.CritNext && x.Now && x.FoeStack != null)?.FoeStack,   // 그 표식 적이면 치명(카드 어디에 둬도 처음부터 — 엘다인 4차 요청)
            };
            // 다음 카드 강화(empower · onMax empower) — 그 사도 몫 · 파티 몫을 이 카드가 쓴다(상태 카드 · 저주는 안 쓴다)
            if (!c.IsStatus && !c.IsCurse && Counts.Count > 0 && EmpowerOf(c.Hero) > 0) ctx.Scale *= EmpowerUse(c.Hero);
            // 다른 아군이 걸어 둔 다음 카드 강화(empower who other · minCost — 엘다인 4차 요청)
            if (!c.IsStatus && !c.IsCurse && Counts.Count > 0) { int eo = EmpowerOther(c.Hero, c.X ? paid : Math.Max(c.Cost + costUp, c.CountAs), true); if (eo > 0) { ctx.Scale *= 1 + eo / 100.0; Say($"동료 강화 +{eo}% — 이 카드에"); } }
            if (ProgOf != null && owner != null && !c.IsStatus && !c.IsCurse) ctx.Scale *= 1 + ProgCardOf(owner.Key, cardId);   // 영구 성장 — 잠재력의 카드군 피해 · 실드 · 치유 +%
            bool sealedNow = HasTagB(cardId, Tag.Seal) && !Unsealed.Contains(cardId);
            bool blockedNow = CardStOf(cardId, "봉쇄") > 0;
            if (blockedNow) DelCardSt(cardId, "봉쇄");
            bool mute = sealedNow || blockedNow;
            bool sameHero = c.Hero != null && LastHero == c.Hero;
            LastHero = c.Hero;
            // 연결 — 직접 내면 손의 다른 연결 카드를 모두 버린다
            if (!opts.Auto && HasTagB(cardId, Tag.Connect))
            {
                var drop = Hand.Where(id => id != cardId && HasTagB(id, Tag.Connect)).ToList();
                if (drop.Count > 0) { foreach (var id in drop) { Hand.Remove(id); CardCue(id, "hand", "discard", "connect"); } Discard.AddRange(drop); Say($"연결 — {string.Join(" ", drop.Select(id => $"「{CardOf(id).Name}」"))} 버린다"); foreach (var id in drop) { BulletDrop(id); if (Over != null) return new PlayResult { Ok = true }; } }
            }
            Say($"▶ {(owner != null ? owner.Name : "교주")} 「{c.Name}」{(paid > 0 ? $" (AP {paid})" : "")}{(opts.Auto ? " — 저절로" : "")}");
            PrevNat = nat;
            PlayLog.Add((c.Hero, c.Type));
            PlayIds.Add(baseId);
            Acting = c.Hero;
            ModSrc = $"{(owner != null ? owner.Name + " " : "")}「{c.Name}」";
            Cue("act", owner, new Cue { Anim = c.Type == "공격" ? "attack" : "skill", CardId = cardId, Name = c.Name });
            KwWipe(c);
            string formBefore = owner != null && Forms.TryGetValue(owner.Key, out var frb) ? frb.Id : null;   // 덤은 내는 순간의 태세(우로스 선 · 악)
            try
            {
                // 독 — 이 카드를 내면 파티가 피해(실드 무시)
                int psn = CardStOf(cardId, "독");
                if (psn > 0) { DelCardSt(cardId, "독"); Say($"「{c.Name}」 — 독! 파티 피해 {psn}"); StatusCue(PartyRep(owner), "독!"); Hurt(PartyRep(owner), psn, new HurtOpts { Dot = true }); CheckOver(); }
                if (Over != null) { }
                else if (blockedNow) { Say($"「{c.Name}」 — 봉쇄(효과 없음, 봉쇄가 풀린다)"); StatusCue(owner ?? AliveParty().FirstOrDefault(), "봉쇄 해제", true); }
                else if (sealedNow)
                {
                    Unsealed.Add(cardId);
                    Say($"「{c.Name}」 — 봉인이 풀린다(효과 없음)"); StatusCue(owner ?? AliveParty().FirstOrDefault(), "봉인 해제", true);
                }
                else RunFx(c.Fx, ctx);
                // 그 카드만의 축복 — 덤 효과(배율은 ctx.Shin 이 실었다)
                var bl = mute ? null : BlessOf(Data.Card(cardId), sh);
                if (bl != null && bl.Fx.Count > 0) { Say(R.DIVINE_NAME); var c2 = ctx.Copy(); c2.Shin = null; RunFx(bl.Fx, c2); }
                // 협공 — 사도가 공격 카드를 내면 다른 아군의 공격력 100% 로 같은 적을, 1 쓴다
                // 협공 — 협공을 건 사도의 공격력 100% 로 추가 공격(건 사도가 없으면 낸 사도를 뺀 가장 센 사도), 1 쓴다
                if (!mute && owner != null && c.Type == "공격" && St(Pool, "협공") > 0 && Over == null)
                {
                    var t = Enemies.FirstOrDefault(e => e.Idx == targetIdx && !e.Dead) ?? AliveEnemies().FirstOrDefault();
                    if (t != null)
                    {
                        AddSt(Pool, "협공", -1);
                        var g = GiverOf("협공") ?? AliveParty().Where(u => u != owner).OrderByDescending(AtkNow).FirstOrDefault() ?? owner;
                        ExtraHit(g, t, R.SV("협공"), null, "협공!");
                    }
                }
            }
            finally { discardPick = null; spreadPick = null; }
            if (formBefore != null && !mute) FormToggle(owner, ctx, formBefore);   // 자기 카드를 내면 짝 태세로(toggle)

            // 강화 카드 · 소멸 · 소멸 N · 회수
            bool exhausted = false;
            int useN = c.TagN(Tag.Exhaust);   // 「소멸 2」 → 2 · 「소멸」 → 0 · 없음 → -1
            if (useN < 0 && c.HasTag(Tag.Bullet)) useN = 0;   // 탄환 — 사용하면 소멸(키워드 규칙, 2026-10-13 사용자)
            bool exhaustN = useN > 0;
            bool spent = c.Type == "강화" && !exhaustN;   // 강화는 주인 없는 교주 카드 · 신탁으로 강화가 된 카드도 — 다시 손에 오면 겹이 끝없이 쌓였다(자기 계발 0코 신탁, 2026-10-12)
            if (spent) Say($"강화 카드 「{c.Name}」 — 이 전투에서 사라진다");
            if (!mute) KwConsume(owner, c);
            if (exhaustN) UseCount[cardId] = (UseCount.TryGetValue(cardId, out var uc) ? uc : 0) + 1;
            bool goneN = exhaustN && UseCount[cardId] >= useN;
            int recallN = c.TagN(Tag.Recall);
            if (recallN == 0) recallN = 1;
            bool recallOk = c.HasTag(Tag.Recall) && (Recalled.TryGetValue(cardId, out var rc) ? rc : 0) < recallN && Hand.Count < R.HAND_MAX;
            if (HasTagB(cardId, Tag.Remove) && !(owner != null && owner.Dead))
            {   // 제거 — 이 전투에서 사라지고 판의 덱에서도 빠진다(소멸이 아니다)
                Gone.Add(cardId); Removed.Add(cardId); CardCue(cardId, "play", "gone", "remove");
                Say($"「{c.Name}」 — 제거: 덱에서 완전히 빠진다");
            }
            else if (ctx.TransformTo != null && Data.Card(ctx.TransformTo) != null)
            {   // 카드 바꾸기(이 카드) — 이 카드 대신 바뀐 카드가 버린 더미로(이 전투에서만)
                var into = ctx.TransformTo + GameData.PLAIN;
                CardCue(cardId, "play", "gone", "transform"); Discard.Add(into); Used.Add(into); CardCue(into, "new", "discard", "transform");
                Say($"「{c.Name}」 → 「{Data.Card(into).Name}」");
            }
            else if (spent || goneN || useN == 0 || BlessTag(cardId, Tag.Exhaust) || (owner != null && owner.Dead))
            {
                bool ex = !spent && !(owner != null && owner.Dead);
                if (ex) Exile(cardId, "play", null, ctx.NoForget); else { Gone.Add(cardId); CardCue(cardId, "play", "gone"); }
                if (goneN) Say($"「{c.Name}」 — {useN}번째, 소멸");
                if (ex) exhausted = true;
            }
            else if (recallOk) { Recalled[cardId] = (Recalled.TryGetValue(cardId, out var r0) ? r0 : 0) + 1; Hand.Add(cardId); CardCue(cardId, "play", "hand", "recall"); Say($"회수 — 「{c.Name}」 손으로 돌아온다"); }
            else { Discard.Add(cardId); Used.Add(cardId); CardCue(cardId, "play", "discard"); }   // 이번 턴에 낸 카드 — 이 턴의 섞기로는 돌아가지 않는다
            if (exhausted) Emit("exhaust", new EmitInfo { Hero = c.Hero, By = owner?.Key, Id = cardId });
            if (!mute && HasTagB(cardId, Tag.Echo)) Echo.Add((cardId, targetIdx));
            Bond.Remove(cardId); DelCardSt(cardId, "침체"); DelCardSt(cardId, "비용");
            DropKeepVals(cardId);   // 붙은 동안 보존 — 낸 뒤 벗는다(엘다인 3차)
            CardValueAfterPlay(cardId, c.Hero);   // 카드 값 키워드 reset play
            NotePlayed(cardId);   // 받아 적기가 찾는 최근 카드(엘다인 3차)
            if (ctx.AfterSet != null) foreach (var af in ctx.AfterSet) CardStatusSet(af, cardId);   // (엘다인 ▶E17) 낸 뒤 카드 값 정하기
            // 공용 축복 — 낼 때 붙는 것
            var sk = ctx.Shin;
            if (sk == "draw") DrawCards(1);
            if (sk == "ap") Ap += 1;
            if ((sk == "atkUp" || sk == "defUp") && owner != null) AddMod(owner, sk == "atkUp" ? "atk" : "def", 0.10, 999, $"「{c.Name}」 {R.DIVINE_NAME}", false);
            PlayedThisTurn++;
            if (c.Hero != null && owner != null) PlayedBy[owner.Key] = (PlayedBy.TryGetValue(owner.Key, out var pb) ? pb : 0) + 1;
            var tgt = Enemies.FirstOrDefault(e => e.Idx == targetIdx && !e.Dead);
            PlayTags.Add(c.Tags.Select(t => Tag.Parse(t).id).ToList());
            if (Over == null && c.Hero != null && !c.IsStatus && !c.IsCurse) CardValueAllyPlay(c.Hero);
            if (Over == null && c.Hero != null && !c.IsStatus && !c.IsCurse && !mute) MarkBurn(c.Hero);   // 붙은 표시가 탐(벨라 존속의 촛불)
            if (Over == null && !mute) ShieldLeftArm(c, owner, targetIdx);   // 낸 뒤 적 차례가 끝날 때 실드가 남았으면   // 아군이 카드를 냄 — 손의 그 사도 카드 값 +1(엘다인 4차)
            Emit("play", new EmitInfo { Id = cardId, Hero = c.Hero, Actor = owner?.Key, Type = c.Type, Nth = PlayedThisTurn, Target = tgt, Cost = c.X ? paid : c.Cost + costUp, CostNow = c.X ? paid : cost0, Sig = c.Signature, Repeat = repeat, Who = owner, Tags = c.Tags, Chain = ctx.Chain, SameHero = ctx.Link });
            // 계산형 공용 계기 — 이 카드의 셈 조건이 섰으면 「셈이 맞으면」(카드가 다 끝난 뒤)
            if (ctx.Tallied && Over == null) Emit("tally", new EmitInfo { Id = cardId, Hero = c.Hero, By = owner?.Key, Target = tgt });
            Acting = null; ModSrc = null;
            CheckOver();
            if (Over == null) { FoePassives("card", null, type: c.Type, nth: PlayedThisTurn, same: sameHero); CheckOver(); }
            if (Over == null) foreach (var e in AliveEnemies()) { CounterEvent(e, "card", c.Type); if (Over != null) break; }
            // 행동 둔화 — 신속 카드를 내면 1턴간 모든 적 사기 -1(주는 피해 -20%), 1 쓴다
            if (Over == null && HasTagB(cardId, Tag.Swift) && St(Pool, "행동 둔화") > 0)
            {
                AddSt(Pool, "행동 둔화", -1);
                foreach (var e in AliveEnemies()) AddMod(e, "dealt", -R.SV("행동 둔화"), 1, "행동 둔화", false);
                Say("행동 둔화 — 1턴간 적 전체 사기 -1");
            }
            // 열정 — 열정 카드가 나가면 손의 「열정:」 마디
            if (Over == null && HasTagB(cardId, Tag.Passion)) PassionWake();
            if (Over == null && !opts.Auto && Later.Count > 0) LaterCard();
            // 미끄러움 — 카드를 내면 무작위 1장 버림, 1 준다
            if (Over == null && St(Pool, "미끄러움") > 0 && Hand.Count > 0 && !opts.Auto)
            {
                AddSt(Pool, "미끄러움", -1); Say("미끄러움 — 손에서 무작위 1장이 떨어진다");
                DiscardFx(1, true); CheckOver();
            }
            // 신속 — 이 카드는 적의 즉시 행동 셈을 늘리지 않는다
            bool swift = HasTagB(cardId, Tag.Swift) || (ctx.ExtraTags?.Contains(Tag.Swift) ?? false) || SwiftTurnOn;   // 이번 턴 남은 카드 신속(엘다인 4차 ▶E80)
            if (Over == null && !swift)
            {
                RushEnemies(); CheckOver();
                // 그을림(파티) — 파티가 낸 카드가 적의 행동 카운트를 줄일 때마다 80% 지속 피해(건 적 바탕), 1 준다
                if (Over == null && St(Pool, "그을림") > 0 && !Pool.Dead)
                {
                    int sv = Math.Max(1, Num.Round(DotUnit(Pool, "그을림") * R.SV("그을림")));
                    AddSt(Pool, "그을림", -1);
                    Say($"파티: 그을림 — 지속 피해 {sv}"); StatusCue(PartyRep(owner), "그을림!");
                    Hurt(PartyRep(owner), sv, new HurtOpts { Dot = true }); CheckOver();
                }
            }
            // 천상 셈 — 저절로 나간 카드는 0(천상을 다시 깨우지 않는다), countAs 가 있으면 그 비용으로(엘다인 ▶E2)
            if (Over == null) HandAuto("play", new AutoInfo { Target = tgt, Hero = c.Hero, Cost = opts.Auto ? 0 : c.X ? paid : Math.Max(c.Cost + costUp, c.CountAs) });
            if (Over == null && HasTagB(cardId, Tag.Finale)) { FinaleLock = true; Say($"「{c.Name}」 — 종극: 턴이 끝난다"); return new PlayResult { Ok = true, Finale = true }; }
            return new PlayResult { Ok = true };
        }

        /// <summary>이 카드를 내면 버릴 카드를 골라야 하나 — 고를 장수(무작위 · 전부 · 남은 손패가 모자라면 0).</summary>
        /// <summary>버릴 카드로 고를 수 있나(거르개 — 화면 · 봇). 거르개가 없으면 늘 된다.</summary>
        public bool DiscardFits(int handIdx, string candidate)
        {
            if (handIdx < 0 || handIdx >= Hand.Count) return false;
            var c = CardOf(Hand[handIdx]);
            var f = c?.Fx.FirstOrDefault(x => (x.K == FxK.Discard || x.K == FxK.Burn) && !x.Random && !x.All);
            return f == null || !HasFilter(f) || FxMatch(f, candidate, c.Hero != null ? HeroUnit(c.Hero) : null);
        }

        public int DiscardChoice(int handIdx)
        {
            if (handIdx < 0 || handIdx >= Hand.Count) return 0;
            var c = CardOf(Hand[handIdx]);
            var f = c?.Fx.FirstOrDefault(x => (x.K == FxK.Discard || x.K == FxK.Burn) && !x.Random && !x.All);
            if (f == null) return 0;
            // 카드 거르개가 있으면 맞는 카드만 센다(엘다인 — 「손의 「마탄」 1장」)
            int rest = HasFilter(f) ? Hand.Where((id, i) => i != handIdx).Count(id => FxMatch(f, id, c.Hero != null ? HeroUnit(c.Hero) : null)) : Hand.Count - 1;
            return rest > f.IV ? f.IV : 0;
        }

        // ── 개막 · 연계 · 천상 ─────────────────────────────────────────
        /// <summary>개막 — 전투가 시작되면 덱의 개막 카드가 AP 를 써서 저절로 나간다. 모자라면 안 나간다.</summary>
        void OpeningPlays()
        {
            var seen = new HashSet<string>();
            var ids = Hand.Concat(Enumerable.Reverse(Draw)).Where(id => seen.Add(id) && HasTagB(id, Tag.Curtain)).ToList();
            foreach (var id in ids)
            {
                if (Over != null) return;
                var c = CardOf(id);
                if (CostOf(id) > Ap || CanPlay(id) != null) { Say($"개막 「{c.Name}」 — AP 가 모자라 나가지 않는다"); continue; }
                if (!Hand.Contains(id) && !Draw.Contains(id)) continue;   // 앞의 카드가 나가는 사이 자리를 떠났다
                if (!Hand.Contains(id)) { Draw.RemoveAt(Draw.LastIndexOf(id)); Hand.Add(id); CardCue(id, "draw", "hand", "curtain"); }   // 개막은 덱에서 바로 「사용」 — 드로우가 아니다(감응 없음)
                Say($"개막! 「{c.Name}」 — 저절로");
                Cue("auto", (c.Hero != null ? HeroUnit(c.Hero) : null) ?? AliveParty().FirstOrDefault(), new Cue { Tag = Tag.Curtain, Label = "개막!", Name = c.Name, CardId = id, Hero = c.Hero });
                PlayCard(Hand.LastIndexOf(id), AliveEnemies().FirstOrDefault()?.Idx ?? 0, new PlayOpts { Auto = true, OpeningPlay = true });
            }
        }

        public const int AUTO_DEPTH = 3;

        /// <summary>손에서 저절로 나가는 카드 — 연계(다른 사도의 카드를 내면) · 천상(코스트 2 이상 카드를 내면). 한 사슬은 세 겹까지.</summary>
        void HandAuto(string ev, AutoInfo info)
        {
            if (Over != null || ev != "play") return;
            int d0 = autoDepth;
            if (d0 >= AUTO_DEPTH) return;
            autoDepth = d0 + 1;
            var tried = new HashSet<string>();
            try
            {
                for (int i = 0; i < Hand.Count && Over == null;)
                {
                    var id = Hand[i]; var c = CardOf(id);
                    string tag = null;
                    if (!tried.Contains(id))
                    {
                        if (HasTagB(id, Tag.Link) && info.Hero != null && (c.Hero == null || c.Hero != info.Hero)) tag = Tag.Link;
                        else if (HasTagB(id, Tag.Heaven) && info.Cost >= 2) tag = Tag.Heaven;
                    }
                    if (tag == null) { i++; continue; }
                    tried.Add(id);
                    if (CanPlay(id, free: true) != null) { i++; continue; }
                    FreeOnce.Add(id);
                    string label = tag == Tag.Link ? "연계!" : "천상!";
                    Say($"{label} 「{c.Name}」 — 손에서 저절로");
                    Cue("auto", (c.Hero != null ? HeroUnit(c.Hero) : null) ?? AliveParty().FirstOrDefault(),
                        new Cue { Tag = tag, Label = label, Name = c.Name, CardId = id, Hero = c.Hero, Target = info.Target != null && !info.Target.Dead ? info.Target.Idx : (int?)null });
                    int t = info.Target != null && !info.Target.Dead ? info.Target.Idx : AliveEnemies().FirstOrDefault()?.Idx ?? 0;
                    if (!PlayCard(i, t, new PlayOpts { Auto = true }).Ok) { FreeOnce.Remove(id); i++; }
                    else if (tag == Tag.Link) Emit("link", new EmitInfo { Hero = c.Hero, Id = id });
                }
            }
            finally { autoDepth = d0; }
        }

        // ── 미리보기 — 판을 복사해 실제로 내 본다(치명 없이, 무작위 대상은 그 적에게 몰렸을 때) ──
        public List<PreviewFoe> PreviewCard(int handIdx, int targetIdx) =>
            handIdx < 0 || handIdx >= Hand.Count || Over != null || CanPlay(Hand[handIdx], false, handIdx) != null ? null
            : PreviewFoes(CardOf(Hand[handIdx]).Fx, sh => sh.PlayCard(handIdx, targetIdx));

        public List<PreviewFoe> PreviewUlt(string heroKey, int targetIdx) =>
            Over != null || CanUlt(heroKey) != null ? null : PreviewFoes(UltOf(heroKey).Fx, sh => sh.UseUlt(heroKey, targetIdx));

        List<PreviewFoe> PreviewFoes(List<Fx> fx, Action<Battle> act)
        {
            bool random = fx.Any(f => f.K == FxK.Dmg && f.Target == "randomEnemy");
            Battle Once(int? pick) { var sh = Clone(new Rng(1)); sh.Preview = true; sh.PreviewPick = pick; act(sh); return sh; }
            List<(int? idx, Battle b)> runs;
            try { runs = random ? AliveEnemies().Select(e => ((int?)e.Idx, Once(e.Idx))).ToList() : new List<(int?, Battle)> { (null, Once(null)) }; }
            catch (Exception) { return null; }
            return Enemies.Select(e =>
            {
                if (e.Dead) return null;
                var after = (random ? runs.First(r => r.idx == e.Idx).b : runs[0].b).Enemies[e.Idx];
                int hp = e.Hp - Math.Max(0, after.Hp);
                int guard = Math.Max(0, e.Block - after.Block + e.Shield - after.Shield);
                // 처치 일격도 강인도를 깎는다(LethalKill) — 쓰러뜨리며 격파하면 Kill · Brk 둘 다
                double tough = e.Broken ? 0 : Math.Max(0, e.Tough - after.Tough);
                bool brk = !e.Broken && after.Broken;
                if (hp <= 0 && guard <= 0 && !after.Dead && tough <= 0) return null;
                return new PreviewFoe { Hp = hp, Guard = guard, Kill = after.Dead, Max = random, Tough = tough, Brk = brk };
            }).ToList();
        }

        public PreviewParty PreviewPartyOf(int handIdx, int targetIdx)
        {
            if (handIdx < 0 || handIdx >= Hand.Count || Over != null || CanPlay(Hand[handIdx], false, handIdx) != null) return null;
            Battle sh;
            var cues = new List<Cue>();
            try { sh = Clone(new Rng(1)); sh.Preview = true; sh.Cues = cues; sh.PlayCard(handIdx, targetIdx); }
            catch (Exception) { return null; }
            int over = cues.Where(f => f.K == "heal" && f.Side == Side.Party).Sum(f => Math.Max(0, f.Over));
            var a = sh.Pool; var u = Pool;
            var p = new PreviewParty { Heal = Math.Max(0, a.Hp - u.Hp), Lose = Math.Max(0, u.Hp - a.Hp), Block = Math.Max(0, a.Block - u.Block), Shield = Math.Max(0, a.Shield - u.Shield), Over = over };
            return p.Heal == 0 && p.Lose == 0 && p.Block == 0 && p.Shield == 0 ? null : p;
        }
    }
}
