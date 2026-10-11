using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 엘다인 3차 원작 검토(2026-10-11, Docs/데이터.md §24 · 02 E63~E75) — 도발 · 은방울 · 다음 동료 카드 치명 · 분산 · 바로 앞 동료 카드 다시 ·
    /// 그 적을 친 횟수 · 처형 · 순환 칸 덤 · 위상 고르기 · 고정 · 단계 카드 덤.
    /// </summary>
    public sealed partial class Battle
    {
        /// <summary>최근 낸 카드의 턴(RecentPlays 와 짝).</summary>
        public List<int> RecentTurns = new();

        bool FxEldain4(Fx f, FxCtx ctx, ref bool gate)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                case FxK.Taunt:   // 이번 적 차례의 적 공격이 이 사도를 노린다 — 그 대에 파티 실드가 (1+v) 배로 막는다(▶E63)
                    if (owner == null) return true;
                    Counts["taunt|" + owner.Key] = Turn; Counts["tauntV|" + owner.Key] = Num.Round(f.V * 100);
                    Say($"{owner.Name} — 도발(이번 적 차례의 공격이 {owner.Name}에게)"); StatusCue(owner, "도발!", true);
                    return true;
                case FxK.CritNext:   // 다음 카드 v 장(기본 1) 치명 확정 — who other 면 다른 아군의 다음 카드(▶E66)
                    {
                        if (f.Now) { if (f.FoeStack != null) ctx.CritFoe = f.FoeStack; else ctx.ForceCrit = true; return true; }   // now(엘다인 4차 요청) — 이 카드(이 조각 뒤의 피해)에 바로
                        if (owner == null) return true;
                        string ck = (f.Who == "other" ? "critNextO|" : "critNextS|") + owner.Key;
                        Counts[ck] = (Counts.TryGetValue(ck, out var cn) ? cn : 0) + System.Math.Max(1, f.IV);
                        Say($"{(f.Who == "other" ? "다른 아군의" : owner.Name + "의")} 다음 카드 {System.Math.Max(1, f.IV)}장 — 치명 확정");
                        return true;
                    }
                case FxK.StackLock:   // 그 고유 효과를 turns 턴(다음 턴 시작 포함) 고정(▶E74)
                    if (owner != null && f.Id != null) { Counts["lock|" + owner.Key + "|" + f.Id] = Turn + f.TurnsOr1; Say($"「{f.Id}」 — {f.TurnsOr1}턴 고정"); }
                    return true;
                case FxK.Execute: ExecuteFx(f, ctx); return true;
            }
            return FxEldain5(f, ctx, ref gate);
        }

        // ── 도발 · 분산 · 은방울(▶E63 · E69 · E65) ─────────────────────────
        /// <summary>이번 적 차례에 도발 중인 사도(없으면 null).</summary>
        Unit Taunter() { if (Counts.Count == 0) return null; foreach (var u in AliveParty()) if (Counts.TryGetValue("taunt|" + u.Key, out var t) && t == Turn) return u; return null; }
        /// <summary>그 사도가 도발 중이면 실드 배율 덤(0.5 = +50%), 아니면 0.</summary>
        double TauntK(Unit u) => u != null && Counts.Count > 0 && Counts.TryGetValue("taunt|" + u.Key, out var t) && t == Turn && Counts.TryGetValue("tauntV|" + u.Key, out var v) ? v / 100.0 : 0;

        /// <summary>분산 — 아군 표시(share)가 붙은 아군을 노린 적의 한 대를 -share 깎는다. 깎인 양(나중에 파티 실드로)을 돌려준다.</summary>
        int ShareCut(Unit u, ref int d)
        {
            if (d <= 0) return 0;
            int cut = 0;
            foreach (var kw in Kw.Values)
                if (kw.Def.Share > 0 && kw.Carrier == "hero" && StackOf(u.Key, kw.Id) > 0)
                {
                    int c = Num.Round(d * kw.Def.Share);
                    if (c <= 0) continue;
                    d -= c; cut += c;
                    TallyShield(HeroUnit(kw.Owner), c);   // 전투 기록 — 분산이 실드로 돌린 몫은 키워드 주인 몫
                    Say($"「{kw.Id}」 — {u.Name}을 노린 피해 -{c}(실드로)");
                }
            return cut;
        }

        /// <summary>은방울 — 파티 실드가 깨지거나 턴 끝에 남으면 그 겹만큼 피해 감소로 바뀐다(한 번).</summary>
        void SilverBell(string why)
        {
            int n = St(Pool, "은방울");
            if (n <= 0 || Pool.Dead) return;
            DelSt(Pool, "은방울");
            Say($"은방울 — {why}: 피해 감소 {n}");
            AddStatus(Pool, "피해 감소", n, 0, null);
        }

        // ── 다음 카드 치명(▶E66) ───────────────────────────────────────
        /// <summary>이 카드가 치명 확정을 쓰나 — 다른 아군이 건 것(critNextO)은 그 사도가 아닌 카드, 자기 것(critNextS)은 그 사도 카드. 쓰면 지운다.</summary>
        bool UseCritNext(string hero)
        {
            if (hero == null || Counts.Count == 0) return false;
            foreach (var u in Party)
                foreach (var k in new[] { u.Key != hero ? "critNextO|" + u.Key : null, u.Key == hero ? "critNextS|" + u.Key : null })
                    if (k != null && Counts.TryGetValue(k, out var n) && n > 0) { if (n > 1) Counts[k] = n - 1; else Counts.Remove(k); return true; }
            return false;
        }

        // ── 그 적을 친 횟수 · 처형(▶E71 · E72) ─────────────────────────────
        /// <summary>이번 전투에 그 사도가 그 적을 친 횟수(카드의 피해 한 대마다).</summary>
        public int HitsBy(Unit e, string heroKey) => e != null && heroKey != null && Counts.TryGetValue("hitsBy|" + e.Idx + "|" + heroKey, out var v) ? v : 0;
        void CountHit(Unit e, Unit from) { string k = "hitsBy|" + e.Idx + "|" + from.Key; Counts[k] = (Counts.TryGetValue(k, out var v) ? v : 0) + 1; }

        void ExecuteFx(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null) return;
            foreach (var t in Resolve(ctx, f.Target ?? "oneEnemy").Where(x => x.Side == Side.Enemy && !x.Dead).ToList())
            {
                bool big = t.Boss || (Data.Enemy(t.Key)?.Clone != null);
                if (big)
                {
                    if (f.BossRatio > 0) { Say($"처형 — {t.Name}(보스) 대신 덤 피해"); Hurt(t, HitAmount(owner, f.BossRatio * ctx.Scale), new HurtOpts { From = owner, Card = ctx.Card, Attack = ctx.Type == "공격" }); }
                    continue;
                }
                if ((double)t.Hp / Math.Max(1, t.MaxHp) > (f.Pct > 0 ? f.Pct : 0.1)) continue;
                Say($"처형 — {t.Name}"); StatusCue(t, "처형!");
                Hurt(t, t.Hp + t.Block + t.Shield, new HurtOpts { From = owner, Pure = true });
                if (Over != null) return;
            }
        }

        // ── 위상 고르기 · 고정 · 순환 칸 덤 · 단계 카드 덤(▶E74 · E73 · E75) ─────────
        /// <summary>단계를 고르는 쌓기(stack pick) 카드의 후보 단계(1 ~ 최대) — 화면 · 봇. 그런 카드가 아니면 빈 목록. 고른 수는 PlayOpts.Stage.</summary>
        public List<int> StageChoices(string cardId)
        {
            var c = CardOf(cardId);
            var f = c?.Fx.FirstOrDefault(x => x.K == FxK.Stack && x.Pick);
            if (f == null || !Kw.TryGetValue(f.Id, out var kw) || !(kw.Def.Cap > 0)) return new List<int>();
            return Enumerable.Range(1, kw.Def.Cap.Value).ToList();
        }
        /// <summary>그 사도의 그 고유 효과가 고정돼 있나(stack lock — 그 턴들 동안 쌓기 · 감소가 안 든다).</summary>
        bool StackLocked(string heroKey, string id) => Counts.Count > 0 && Counts.TryGetValue("lock|" + heroKey + "|" + id, out var until) && until >= Turn;

        int stageDepth;
        /// <summary>순환 칸에 닿았다 — 그 칸의 효과(onStage).</summary>
        void StageFx(string id, int after)
        {
            if (stageDepth > 2 || !Kw.TryGetValue(id, out var kw) || kw.Def.OnStage == null || after < 1 || after > kw.Def.OnStage.Count) return;
            var fx = kw.Def.OnStage[after - 1];
            var o = HeroUnit(kw.Owner);
            if (fx == null || fx.Count == 0 || o == null || o.Dead || Over != null) return;
            stageDepth++;
            try { RunPassive(o, fx, new FxCtx { Owner = o, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0 }, $"{o.Name} · 「{id}」 {StageLabel(kw.Def, after)}"); }
            finally { stageDepth--; }
        }
        static string StageLabel(KeywordDef d, int n) => d.Stages != null && n >= 1 && n <= d.Stages.Count ? d.Stages[n - 1] : n + "칸";

        readonly Dictionary<(string, int, int, string), CardView> stageViews = new();
        bool? stageOn;
        /// <summary>단계 카드 덤 — 순환 고유 효과의 지금 단계에 맞는 덤(stageBonus)을 그 사도 카드 모습에 얹는다.</summary>
        CardView StageCard(string id, CardView v)
        {
            stageOn ??= Kw.Values.Any(k => k.Def.StageBonus != null && k.Def.StageBonus.Count > 0);
            if (stageOn == false || v?.Hero == null) return v;
            foreach (var kw in Kw.Values)
            {
                if (kw.Owner != v.Hero || kw.Def.StageBonus == null || kw.Def.StageBonus.Count == 0) continue;
                int st = StackOf(kw.Owner, kw.Id);
                var key = (id + "|" + v.Type + "|" + v.Def.Id, v.FlashN, st, kw.Id);
                if (stageViews.TryGetValue(key, out var hit)) { v = hit; continue; }
                var o = v;
                for (int bi = 0; bi < kw.Def.StageBonus.Count; bi++)
                {
                    var b = kw.Def.StageBonus[bi];
                    if ((b.Stage > 0 ? b.Stage : bi + 1) != st) continue;   // stage 가 없으면 목록 차례가 단계
                    if (b.Card != null && GameData.BaseId(id) != b.Card) continue;
                    if (b.Type != null && o.Type != b.Type) continue;
                    if (b.Unique && !v.Unique) continue;
                    if (b.Tag != null && !o.HasTag(b.Tag)) continue;
                    var fx = o.Fx.Select(f => b.Ratio > 0 && (f.K == FxK.Dmg || f.K == FxK.Extra) && f.OfEvent <= 0 ? Scaled(f, b.Ratio) : f).ToList();
                    int at = fx.FindIndex(f => FxK.Conditions.Contains(f.K));
                    fx.InsertRange(at < 0 ? fx.Count : at, b.Fx ?? new List<Fx>());
                    var tags = o.Tags.ToList();
                    foreach (var t in b.Tags ?? new List<string>()) if (!tags.Contains(t)) tags.Add(t);
                    o = new CardView(o, tags, fx); o.Target = Data.TargetOf(o.Fx);
                }
                stageViews[key] = o;
                v = o;
            }
            return v;
        }

        // ── 카드 값 키워드(grow handEnd · reset play) ──────────────────────────
        /// <summary>턴 끝 — 그 사도 고유 카드가 손에 있으면 그 카드 값 +1(grow handEnd).</summary>
        void CardValueTurnEnd()
        {
            foreach (var kw in Kw.Values)
            {
                if (kw.Def.CardValue?.Grow != "handEnd") continue;
                foreach (var id in Hand.Distinct().ToList())
                {
                    var c = CardOf(id);
                    if (c?.Hero != kw.Owner || !c.Unique) continue;
                    int before = CardStOf(id, kw.Id);
                    AddCardSt(id, kw.Id, 1);
                    if (CardStOf(id, kw.Id) > before && Over == null) Emit("grow", new EmitInfo { Id = id, Hero = c.Hero, By = c.Hero, Kind = kw.Id, V = 1 });
                }
            }
        }
        /// <summary>(엘다인 4차 리뉴아) 아군이 카드를 냈다 — grow allyPlay 키워드면 손의 그 사도 고유 카드(강화 빼고) 값 +1(최대 max). 이 전투에서만(판으로 안 넘어감).</summary>
        void CardValueAllyPlay(string playedHero)
        {
            foreach (var kw in Kw.Values)
            {
                var cv = kw.Def.CardValue;
                if (cv?.Grow != "allyPlay" || playedHero == null) continue;
                if (cv.Who == "other" && playedHero == kw.Owner) continue;
                BattleVals.Add(kw.Id);
                foreach (var id in Hand.Distinct().ToList())
                {
                    var c = CardOf(id);
                    if (c?.Hero != kw.Owner || !c.Unique || c.Type == "강화") continue;
                    AddCardSt(id, kw.Id, 1, cv.Max);
                }
            }
        }
        /// <summary>카드 값 1당 비용 증감(cardValue cost) — CostOf 가 더한다.</summary>
        int CardValueCost(string id)
        {
            int adj = 0;
            if (cvCostOn == false) return 0;
            cvCostOn ??= Kw.Values.Any(k => k.Def.CardValue != null && k.Def.CardValue.Cost != 0);
            if (cvCostOn == false) return 0;
            var c = CardOf(id);
            foreach (var kw in Kw.Values)
                if (kw.Def.CardValue != null && kw.Def.CardValue.Cost != 0 && c?.Hero == kw.Owner) adj += CardStOf(id, kw.Id) * kw.Def.CardValue.Cost;
            return adj;
        }
        bool? cvCostOn;

        /// <summary>낸 뒤 — 그 사도 카드의 카드 값을 0 으로(reset play).</summary>
        void CardValueAfterPlay(string cardId, string hero)
        {
            if (hero == null) return;
            foreach (var kw in Kw.Values) if (kw.Owner == hero && kw.Def.CardValue?.Reset == "play") DelCardSt(cardId, kw.Id);
        }

        // ── 바로 앞 동료 카드 다시(▶E70) ───────────────────────────────────
        /// <summary>이번 턴 마지막에 낸 다른 아군 카드(생성 · 상태 · 저주 · 대신 발동 · 낭독 카드 빼고) — 없으면 null.</summary>
        string LastOtherThisTurn(Unit owner)
        {
            for (int i = RecentPlays.Count - 1; i >= 0; i--)
            {
                if (i < RecentTurns.Count && RecentTurns[i] != Turn) break;
                var c = CardOf(RecentPlays[i]);
                if (c?.Hero == null || (owner != null && c.Hero == owner.Key)) continue;
                if (!Recordable(c) || c.Fx.Any(x => x.K == FxK.CastOther)) continue;
                return RecentPlays[i];
            }
            return null;
        }
    }
}
