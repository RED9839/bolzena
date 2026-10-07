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
            // 변신 — 그 사도가 변신 중이면 변신판 모습(카드 바꾸기 · 덤)
            if (Forms.Count > 0) v = FormCard(id, v);
            return v;
        }

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
        public bool HasTagB(string id, string tag) => (CardOf(id)?.HasTag(tag) ?? false) || BlessTag(id, tag) || KwTag(id, tag) || GameData.BaseId(id) == tag;   // 생성물 카드 id 도 갈래 태그로(perTag · 거르개 tag — 「케이크」 세기, 2026-10-07)

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
                {
                    if (Discard.Count == 0) break;
                    Draw = Shuffle(Rng, Discard.ToList()); Discard.Clear(); CardCue(null, "discard", "draw", "shuffle");
                    Emit("shuffle", new EmitInfo()); if (Over != null) break;
                }
                var id = Draw[Draw.Count - 1]; Draw.RemoveAt(Draw.Count - 1);
                Drawn++;
                if (Hand.Count >= R.HAND_MAX) { Gone.Add(id); burned++; CardCue(id, "draw", "gone", "burn"); continue; }
                Hand.Add(id); CardCue(id, "draw", "hand");
                var c = CardOf(id);
                // 빙결 — 뽑은 턴에는 낼 수 없다
                if (CardStOf(id, "빙결") > 0) { DelCardSt(id, "빙결"); Frozen[id] = Turn; Say($"「{c?.Name}」 — 빙결(이번 턴 낼 수 없다)"); }
                // 감응 — 뽑히면(턴 시작 뽑기 포함)
                if (c != null && c.Fx.Any(f => f.K == FxK.When && f.On == "drawAny") && senseDepth < 3)
                {
                    senseDepth++;
                    try { CardWhen(id, "drawAny"); } finally { senseDepth--; }
                    if (Over != null) break;
                }
                Emit("drawn", new EmitInfo { Hero = c?.Hero, Id = id, Type = c?.Type, Tags = c?.Tags });
                if (Over != null) break;
                // 적이 끼워 넣은 상태 카드 · 저주는 턴 시작에 뽑혀도 돈다
                if (c != null && (ability || c.IsStatus || c.IsCurse) && c.Fx.Any(f => f.K == FxK.When && f.On == "draw") && senseDepth < 3)
                {
                    senseDepth++;
                    try { CardWhen(id, "draw"); } finally { senseDepth--; }
                    if (Over != null) break;
                }
            }
            if (burned > 0) Say($"손이 가득 차 {burned}장이 사라졌다 (최대 {R.HAND_MAX}장)");
            MergePile(Hand, "hand");
        }

        // ── 고학년 스킬 ────────────────────────────────────────────────
        public UltDef UltOf(string heroKey) => Data.Hero(heroKey)?.Ult;

        /// <summary>쓸 수 없으면 까닭, 쓸 수 있으면 null.</summary>
        public string CanUlt(string heroKey)
        {
            var u = HeroUnit(heroKey);
            if (u == null || u.Dead) return "나설 수 없습니다";
            var ult = UltOf(heroKey);
            if (ult == null) return "고학년 스킬이 없습니다";
            if (Gauge < ult.Cost) return $"게이지가 모자랍니다 ({Gauge}% / {ult.Cost}%)";
            if (St(Pool, "정신 붕괴") > 0) return "정신 붕괴 — 고학년 스킬을 쓸 수 없습니다";
            return null;
        }

        public PlayResult UseUlt(string heroKey, int targetIdx = 0)
        {
            var why = CanUlt(heroKey);
            if (why != null) return PlayResult.Fail(why);
            var ult = UltOf(heroKey);
            Gauge -= ult.Cost;
            LastUlt = heroKey;
            var owner = HeroUnit(heroKey);
            Say($"{owner.Name} 고학년 스킬 — {ult.Name} (게이지 {ult.Cost}%)");
            Talk(owner, "ego");
            Cue("act", owner, new Cue { Anim = "ult", Name = ult.Name });
            var (prev, src0) = (Acting, ModSrc);
            Acting = heroKey; ModSrc = $"{owner.Name} 「{ult.Name}」";
            ActSeq = ++SeqN;
            var tags = new HashSet<string>();
            RunFx(ult.Fx, new FxCtx { Owner = owner, TargetIdx = targetIdx, HitTags = tags, Card = true, Cost = 2 });
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

        public int CostOf(string cardId, int? handIdx = null)
        {
            var c = CardOf(cardId);
            if (FreeOnce.Contains(cardId) || GraceFree(cardId, handIdx)) return 0;
            int divine = ShinKind(Data.Card(cardId), ShinOf(cardId)) == "cost" ? 1 : 0;
            int lead = LeadOn.Contains(cardId) && PlayedThisTurn <= 0 ? 1 : 0;
            int adj = (CardStOf(cardId, "침체") > 0 ? 1 : 0) + CardStOf(cardId, "비용") + (CostMods.Count > 0 ? CostModOf(cardId) : 0);   // 침체 +1 · 적이 바꾼 비용(착란 …)
            return Math.Max(0, c.Cost - divine - lead - NextCheaper + adj);
        }

        // ── 신탁 ───────────────────────────────────────────────────────
        /// <summary>빛나는 카드의 신탁 — 정신 붕괴 중에는 없다(그 카드는 맨 카드로 나간다 · 빛은 남는다).</summary>
        public Glow GlowOf(string cardId) => St(Pool, "정신 붕괴") > 0 ? null : Glow.TryGetValue(cardId, out var g) ? g : null;

        /// <summary>빛나는 카드의 신탁을 고른다 — choice 는 선택지 번호. 카드 신탁은 그 카드가 바로 바뀌고 이번에는 코스트 0, 은총은 고유 카드가 손에(그 턴 코스트 0).</summary>
        public string ApplyEpiphany(string cardId, int choice)
        {
            var g = GlowOf(cardId);
            if (g == null || choice < 0 || choice >= g.Count) return null;
            Glow.Remove(cardId);
            Cue("epiphany", HeroUnit(CardOf(cardId)?.Hero) ?? PartyRep(),new Cue { CardId = cardId, Id = g.Kind, V = choice, Hero = g.Hero });
            if (g.Kind == "card")
            {
                var o = g.Picks[choice];
                Flash[cardId] = o.N;
                if (o.Shin != null) Shin[cardId] = o.Shin;
                FreeOnce.Add(cardId);
                GainedFlash.Add((cardId, o.N, o.Shin));
                var od = Data.Card(cardId).Oracles[o.N - 1];
                Say($"신탁! 「{Data.Card(cardId).Name}」 → 신탁 {o.N}{(o.Shin != null ? " · 축복" : "")}");
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
            if (c == null) return "그런 카드가 없습니다";
            if (c.HasTag(Tag.Unplayable)) return "낼 수 없는 카드입니다";
            if (FinaleLock) return "종극 — 이번 턴은 끝났습니다";
            if (IsFrozen(cardId)) return "빙결 — 뽑은 턴에는 낼 수 없습니다";
            var def = Data.Card(cardId);
            if (!free)
            {
                int cost = CostOf(cardId, handIdx);
                if (def?.PayWith != null)
                {
                    int rate = Math.Max(1, def.PayRate), have = StackOf(c.Hero, def.PayWith) / rate;
                    if (def.PayMix ? have + Ap < cost : have < cost) return $"{Ko.J("「" + def.PayWith + "」", "이가")} 모자랍니다";
                }
                else if (cost > Ap && !(def?.Debt ?? false)) return "AP가 모자랍니다";
            }
            var owner = c.Hero != null ? HeroUnit(c.Hero) : null;
            if (c.Hero != null && (owner == null || owner.Dead)) return $"{Data.Hero(c.Hero)?.Name ?? c.Hero} — 나설 수 없습니다";
            return null;
        }

        /// <summary>손의 handIdx 번째 카드를 낸다. targetIdx — 적의 idx(아군을 고르는 카드면 사도 idx).</summary>
        public PlayResult PlayCard(int handIdx, int targetIdx, PlayOpts opts = null)
        {
            opts ??= new PlayOpts();
            if (Over != null) return PlayResult.Fail("전투가 끝났습니다");
            if (handIdx < 0 || handIdx >= Hand.Count) return PlayResult.Fail("그런 카드가 없습니다");
            var cardId = Hand[handIdx];
            var why = CanPlay(cardId, false, handIdx);
            if (why != null) return PlayResult.Fail(why);

            var c = CardOf(cardId);
            ActSeq = ++SeqN;
            PlaysTotal++;
            var owner = c.Hero != null ? HeroUnit(c.Hero) : null;
            bool grace = !FreeOnce.Contains(cardId) && GraceFree(cardId, handIdx);
            var def0 = Data.Card(cardId);
            int cost0 = c.X ? Ap : CostOf(cardId, handIdx);
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
            int held = Held.TryGetValue(cardId, out var hv) ? hv : 0;
            int playedBefore = PlayedThisTurn;
            string baseId = GameData.BaseId(cardId);
            bool repeat = PlayIds.Count > 0 && PlayIds[PlayIds.Count - 1] == baseId;
            if (paid > 0) Gauge = Math.Min(R.GAUGE_MAX, Gauge + paid * R.GAUGE_PER_AP);
            NextCheaper = 0;
            FreeOnce.Remove(cardId);
            if (grace) { int left = (FreeTurn.TryGetValue(cardId, out var ft) ? ft : 1) - 1; if (left > 0) FreeTurn[cardId] = left; else FreeTurn.Remove(cardId); }
            Hand.RemoveAt(handIdx);
            CardCue(cardId, "hand", "play");
            discardPick = opts.Discard?.ToList();

            var sh = ShinOf(cardId);
            var nat = owner != null ? NatureOf(owner) : null;
            var last = PlayLog.Count > 0 ? PlayLog[PlayLog.Count - 1] : ((string, string)?)null;
            var ctx = new FxCtx
            {
                Owner = owner, TargetIdx = targetIdx, AllyIdx = opts.Ally, X = c.X ? paid : 0, Shin = ShinKind(Data.Card(cardId), sh),
                HitTags = CardHitTags(cardId, c), Card = true, Type = c.Type, Chain = nat != null && PrevNat == nat, Tune = tune,
                Link = c.Hero != null && last != null && last.Value.Item1 == c.Hero, Prev = last?.Item2,
                Repeat = repeat, Held = held, PlayedBefore = playedBefore,
                CardId = cardId, Made = GameData.IsPlain(cardId), Cost = c.X ? paid : c.Cost, Choice = opts.Choice ?? 0, Scale = opts.Scale ?? 1,
            };
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
                if (drop.Count > 0) { foreach (var id in drop) { Hand.Remove(id); CardCue(id, "hand", "discard", "connect"); } Discard.AddRange(drop); Say($"연결 — {string.Join(" ", drop.Select(id => $"「{CardOf(id).Name}」"))} 버린다"); }
            }
            Say($"▶ {(owner != null ? owner.Name : "교주")} 「{c.Name}」{(paid > 0 ? $" (AP {paid})" : "")}{(opts.Auto ? " — 저절로" : "")}");
            PrevNat = nat;
            PlayLog.Add((c.Hero, c.Type));
            PlayIds.Add(baseId);
            Acting = c.Hero;
            ModSrc = $"{(owner != null ? owner.Name + " " : "")}「{c.Name}」";
            Cue("act", owner, new Cue { Anim = c.Type == "공격" ? "attack" : "skill", CardId = cardId, Name = c.Name });
            KwWipe(c);
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
                if (bl != null && bl.Fx.Count > 0) { Say($"{R.DIVINE_NAME} 「{bl.Name}」"); var c2 = ctx.Copy(); c2.Shin = null; RunFx(bl.Fx, c2); }
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
            finally { discardPick = null; }

            // 강화 카드 · 소멸 · 소멸 N · 회수
            bool exhausted = false;
            int useN = c.TagN(Tag.Exhaust);   // 「소멸 2」 → 2 · 「소멸」 → 0 · 없음 → -1
            bool exhaustN = useN > 0;
            bool spent = (c.IsPower || (c.Type == "강화" && Data.Card(cardId).Tags.Contains(Tag.Exhaust))) && !exhaustN;
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
                CardCue(cardId, "play", "gone", "transform"); Discard.Add(into); CardCue(into, "new", "discard", "transform");
                Say($"「{c.Name}」 → 「{Data.Card(into).Name}」");
            }
            else if (spent || goneN || useN == 0 || BlessTag(cardId, Tag.Exhaust) || (owner != null && owner.Dead))
            {
                bool ex = !spent && !(owner != null && owner.Dead);
                if (ex) Exile(cardId, "play"); else { Gone.Add(cardId); CardCue(cardId, "play", "gone"); }
                if (goneN) Say($"「{c.Name}」 — {useN}번째, 소멸");
                if (ex) exhausted = true;
            }
            else if (recallOk) { Recalled[cardId] = (Recalled.TryGetValue(cardId, out var r0) ? r0 : 0) + 1; Hand.Add(cardId); CardCue(cardId, "play", "hand", "recall"); Say($"회수 — 「{c.Name}」 손으로 돌아온다"); }
            else { Discard.Add(cardId); CardCue(cardId, "play", "discard"); }
            if (exhausted) Emit("exhaust", new EmitInfo { Hero = c.Hero, By = owner?.Key, Id = cardId });
            if (!mute && HasTagB(cardId, Tag.Echo)) Echo.Add((cardId, targetIdx));
            Bond.Remove(cardId); DelCardSt(cardId, "침체"); DelCardSt(cardId, "비용");
            // 공용 축복 — 낼 때 붙는 것
            var sk = ctx.Shin;
            if (sk == "draw") DrawCards(1);
            if (sk == "ap") Ap += 1;
            if ((sk == "atkUp" || sk == "defUp") && owner != null) AddMod(owner, sk == "atkUp" ? "atk" : "def", 0.10, 999, $"「{c.Name}」 {R.DIVINE_NAME}", false);
            PlayedThisTurn++;
            if (c.Hero != null && owner != null) PlayedBy[owner.Key] = (PlayedBy.TryGetValue(owner.Key, out var pb) ? pb : 0) + 1;
            var tgt = Enemies.FirstOrDefault(e => e.Idx == targetIdx && !e.Dead);
            PlayTags.Add(c.Tags.Select(t => Tag.Parse(t).id).ToList());
            Emit("play", new EmitInfo { Id = cardId, Hero = c.Hero, Actor = owner?.Key, Type = c.Type, Nth = PlayedThisTurn, Target = tgt, Cost = c.X ? paid : c.Cost, Sig = c.Signature, Repeat = repeat, Who = owner, Tags = c.Tags });
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
            bool swift = HasTagB(cardId, Tag.Swift) || (ctx.ExtraTags?.Contains(Tag.Swift) ?? false);
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
            if (Over == null) HandAuto("play", new AutoInfo { Target = tgt, Hero = c.Hero, Cost = opts.Auto ? 0 : c.X ? paid : c.Cost });
            if (Over == null && HasTagB(cardId, Tag.Finale)) { FinaleLock = true; Say($"「{c.Name}」 — 종극: 턴이 끝난다"); return new PlayResult { Ok = true, Finale = true }; }
            return new PlayResult { Ok = true };
        }

        /// <summary>이 카드를 내면 버릴 카드를 골라야 하나 — 고를 장수(무작위 · 전부 · 남은 손패가 모자라면 0).</summary>
        public int DiscardChoice(int handIdx)
        {
            if (handIdx < 0 || handIdx >= Hand.Count) return 0;
            var c = CardOf(Hand[handIdx]);
            var f = c?.Fx.FirstOrDefault(x => (x.K == FxK.Discard || x.K == FxK.Burn) && !x.Random && !x.All);
            if (f == null) return 0;
            int rest = Hand.Count - 1;
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
                if (!Hand.Contains(id)) { Draw.RemoveAt(Draw.LastIndexOf(id)); Hand.Add(id); CardCue(id, "draw", "hand", "curtain"); }
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
