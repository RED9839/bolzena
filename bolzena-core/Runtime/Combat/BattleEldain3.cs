using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 엘다인 3차(2026-10-11 카제나 재정렬, Docs/데이터.md §23) — 반격 모양 · 반격 계기 · 반격 피해 증감(비비) · 고른 만큼 나눠 입히기 · 붙은 동안 보존(클로에) ·
    /// 가장 비싼 카드 · AP 안전 상한(우이) · 손에서 반응하는 카드(우이(기억)) · 게이지 잠금(실비아) · 적마다 한 대 깎기 · 가장 큰 예고(벨라) · 카드 기록 · 재현(죠안).
    /// </summary>
    public sealed partial class Battle
    {
        /// <summary>사도 효과로 얻는 AP 의 한 턴 상한(멈춤 방지 — 게임 규칙 아님 · 글에 안 나옴, ▶E46).</summary>
        public const int AP_GAIN_CAP = 3;
        /// <summary>AP 상한에 걸린 턴 수(실제 판만 — bz sim 끝 줄).</summary>
        public static int ApCapHits;
        /// <summary>죠안 「주교의 경전」 같은 카드 기록 — 사도 키 → 받아 적은 카드(손의 id, 앞이 첫 쪽). 최대 RECORD_MAX 쪽.</summary>
        public Dictionary<string, List<string>> Records = new();
        public const int RECORD_MAX = 3;
        /// <summary>이번 전투에 낸 카드(손의 id · 최근 것이 뒤) — 받아 적기가 「바로 앞 다른 아군 카드」 를 찾는다. 6장까지.</summary>
        public List<string> RecentPlays = new();
        /// <summary>고른 만큼 나눠 입힐 카드(PlayOpts.Spread — 낼 때만).</summary>
        List<string> spreadPick;

        /// <summary>그 사도가 받아 적은 카드(첫 쪽부터) — 화면 미리보기.</summary>
        public IReadOnlyList<string> RecordsOf(string heroKey) => heroKey != null && Records.TryGetValue(heroKey, out var l) ? l : (IReadOnlyList<string>)Array.Empty<string>();

        bool FxEldain3(Fx f, FxCtx ctx, ref bool gate)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                case FxK.CounterHits:   // 반격 한 번을 v 대로 나눈다(ratio — 한 대 비율, 없으면 바탕 반격 비율 / v)
                    Counts["ctr|hits"] = Math.Max(1, f.IV);
                    if (f.Ratio > 0) Counts["ctr|ratio"] = Num.Round(f.Ratio * 100); else Counts.Remove("ctr|ratio");
                    Say($"반격 — {Math.Max(1, f.IV)}대로 나눠 친다");
                    return true;
                case FxK.CounterAll:
                    {   // (2026-10-13 비비 원작 「빙벽」) turns 가 있으면 n턴간(내 턴 + 그 적의 차례) — 「ctr|all」 은 그 턴 다음 값(이 전투 내내는 int.MaxValue). 긴 쪽을 남긴다
                        int until = f.Turns > 0 ? Turn + f.Turns : int.MaxValue;
                        if (!Counts.TryGetValue("ctr|all", out var had) || had < until) Counts["ctr|all"] = until;
                        Say(f.Turns > 0 ? $"반격 — {f.Turns}턴간 적 전체로" : "반격 — 적 전체로");
                        return true;
                    }
                case FxK.CounterMod:   // 반격 피해 ×(1+v) — n턴(파티 사도 모두에게 걸어 반격을 건 사도가 누구든 든다) · run 이면 이 전투 내내(2026-10-13 비비)
                    foreach (var u in Party) AddMod(u, "counter", f.V, f.Run ? R.BOON_TURNS : f.TurnsOr1, ModSrc, f.Run);
                    Say($"반격 피해 {(f.V >= 0 ? "+" : "")}{Num.Round(f.V * 100)}%");
                    return true;
                case FxK.GaugeLock:
                    if (owner != null) { Counts["gaugeLock|" + owner.Key] = 1; Say($"{owner.Name} — 이 전투 동안 고학년 게이지가 오르지 않는다"); }
                    return true;
                case FxK.Record: RecordFx(f, owner); return true;
                case FxK.Replay: ReplayFx(f, ctx); return true;
            }
            return FxEldain4(f, ctx, ref gate);
        }

        /// <summary>키워드 tally — play(그 사도가 카드를 내면 +1) · discardBy(그 사도 효과로 카드가 버려지면 +1). 규칙이 아니라 엔진이 센다.
        /// tallyTag 가 있으면 그 카드만 — play · discard · exhaust · bullet(탄환으로 쏠 때, BulletDrop 이 부른다).</summary>
        void TallyAdd(string ev, EmitInfo info)
        {
            foreach (var kw in Kw.Values)
            {
                var t = kw.Def.Tally;
                if (t == null || t.Count == 0 || kw.Carrier != "self") continue;
                bool hit = kw.Def.TallyTag != null ? t.Contains(ev) && info.Id != null && HasTagB(info.Id, kw.Def.TallyTag)   // (2026-10-13 시온) 그 카드 · 태그만 — 누구의 효과든
                    : ev == "play" ? t.Contains("play") && info.Hero == kw.Owner : ev == "discard" && t.Contains("discardBy") && info.By == kw.Owner;
                if (!hit) continue;
                var o = HeroUnit(kw.Owner);
                if (o != null && !o.Dead) StackFx(new Fx { K = FxK.Stack, Id = kw.Id, V = 1 }, new FxCtx { Owner = o });
                if (Over != null) return;
            }
        }

        /// <summary>그 사도의 카드로 게이지가 잠겼나(▶E51 — 실비아 「궁극의 유희」).</summary>
        bool GaugeLocked(string heroKey) => heroKey != null && Counts.Count > 0 && Counts.ContainsKey("gaugeLock|" + heroKey);

        // ── 반격(▶E38 · E39 · E40) ─────────────────────────────────────
        /// <summary>반격 한 번 — 모양(나눠 침 · 적 전체)과 반격 피해 증감을 본다. 모양이 없으면 옛 반격 그대로(난수 한 번).</summary>
        void CounterStrike(Unit u, Unit from, bool full)
        {
            var g = GiverOf("반격") ?? PartyGuard();
            int hits = Counts.TryGetValue("ctr|hits", out var hv) && hv > 1 ? hv : 1;
            bool all = Counts.TryGetValue("ctr|all", out var allUntil) && allUntil > Turn;
            double baseR = full ? R.SV("반격Full") : R.SV("반격");
            double ratio = Counts.TryGetValue("ctr|ratio", out var rv) && rv > 0 ? rv / 100.0 * (full ? R.SV("반격Full") / R.SV("반격") : 1) : baseR / hits;
            double mult = 1 + (g != null ? StatMod(g, "counter") : 0);
            int bas = g != null ? R.DefDmgStat(AtkNow(g), DefNow(g)) : PartyDef();
            var targets = all ? AliveEnemies() : new List<Unit> { from };
            foreach (var t in targets)
                for (int i = 0; i < hits && !t.Dead && Over == null; i++)
                {
                    double critPct = g != null ? g.Crit + StatMod(g, "crit") * 100 : 0;
                    bool isCrit = !Preview && Rng.Next() * 100 < critPct;
                    int cv = R.FinalDamage(bas, ratio * Math.Max(0.1, mult), crit: isCrit, critX: CritXOf(g ?? u));
                    Say($"파티: 반격{(full ? "(다 막음)" : "")} → {t.Name} ({cv}{(isCrit ? " 치명" : "")})");
                    StatusCue(u, full ? "반격!!" : "반격!", true);
                    Hurt(t, cv, new HurtOpts { From = g ?? u, Counter = true, Crit = isCrit, Shot = "counter" });
                    if (Over == null) Emit("counter", new EmitInfo { By = g?.Key, Who = u, Target = t, V = cv, Seq = ++SeqN });
                }
        }

        // ── 손에서 반응하는 카드(▶E48 — 우이(기억) 세잎클로버) ─────────────────
        /// <summary>
        /// 적의 한 대가 파티 최대 HP 의 pct 를 넘게 깎으려 하면 — 손의 「손에 있으면: 큰 피해」 카드(when on handHurtBig, pct) 한 장이 그 대를 pct 로 깎고 저절로 소멸한다.
        /// 일의 값 = 막은 양. 한 대에 한 장.
        /// </summary>
        void HandHurtBig(Unit u, ref int d)
        {
            if (Hand.Count == 0) return;
            for (int i = 0; i < Hand.Count; i++)
            {
                var id = Hand[i]; var c = CardOf(id);
                var w = c?.Fx.FirstOrDefault(x => x.K == FxK.When && x.On == "handHurtBig");
                if (w == null) continue;
                int cap = Math.Max(1, Num.Round(u.MaxHp * (w.Pct > 0 ? w.Pct : 0.15)));
                if (d <= cap) return;
                int saved = d - cap; d = cap;
                var owner = c.Hero != null ? HeroUnit(c.Hero) : null;
                Hand.RemoveAt(i);
                Say($"「{c.Name}」 — 손에서 큰 피해를 {saved} 막는다"); StatusCue(owner ?? PartyRep(), $"「{c.Name}」!", true);
                var (prev, src0, seq0) = (Acting, ModSrc, ActSeq);
                Acting = c.Hero; ModSrc = $"「{c.Name}」"; ActSeq = ++SeqN;
                try { RunFx(c.Fx, new FxCtx { Owner = owner, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0, Card = true, Type = c.Type, When = "handHurtBig", CardId = id, EventV = saved, Cost = c.Cost }); }
                finally { Acting = prev; ModSrc = src0; ActSeq = seq0; }
                Exile(id, "hand", "burn");
                Emit("exhaust", new EmitInfo { Hero = c.Hero, By = c.Hero, Id = id });
                return;
            }
        }

        // ── 적마다 한 대 깎기(▶E54 · E55 · E56) ─────────────────────────────
        /// <summary>적 하나의 다음 한 대를 깎아 둔 비율(%) — 그 적이 칠 때 쓴다.</summary>
        int CutOf(Unit e) => e != null && Counts.Count > 0 && Counts.TryGetValue("cut|" + e.Idx, out var v) ? v : 0;
        /// <summary>cutHit 에 대상이 있으면 그 적들의 다음 한 대 -v · 일의 값 = 그 적들의 공격 예고 피해 × v(공격이 아니면 0). 대상이 없으면 옛 꼴(파티가 받을 다음 한 대).</summary>
        void CutHitFx(Fx f, FxCtx ctx)
        {
            int ev = 0;
            if (f.Target == null)
            {
                CutNext = Math.Max(CutNext, f.V);
                var big = AliveEnemies().Select(e => IntentHit(e) ?? 0).DefaultIfEmpty(0).Max();
                ev = Num.Round(big * f.V);
            }
            else
                foreach (var t in Resolve(ctx, f.Target).Where(x => x.Side == Side.Enemy))
                {
                    int pct = Num.Round(f.V * 100);
                    Counts["cut|" + t.Idx] = Math.Max(CutOf(t), pct);
                    ev += Num.Round((IntentHit(t) ?? 0) * f.V);
                }
            ctx.EventV = ev;
            if (Over == null) Emit("cutHit", new EmitInfo { By = ctx.Owner?.Key ?? Acting, V = ev, Seq = ActSeq });   // 계기 「적 예고를 깎으면」(엘다인 3차)
            Say($"{(f.Target == "allEnemies" ? "적 전체의 " : f.Target != null ? "" : "")}다음 한 대 -{Num.Round(f.V * 100)}%{(ev > 0 ? $"(깎을 몫 {ev})" : "")}");
        }

        /// <summary>공격 예고가 가장 큰 적(▶E56 bigHitEnemy) — 같으면 앞의 적.</summary>
        Unit BigHitEnemy() => AliveEnemies().OrderByDescending(e => IntentHit(e) ?? 0).ThenBy(e => e.Idx).FirstOrDefault();

        // ── 카드 기록 · 재현(▶E57 — 죠안 「주교의 경전」) ──────────────────────
        static bool Recordable(CardView c) => c != null && !c.IsStatus && !c.IsCurse && !c.Def.Token && !c.Fx.Any(x => x.K == FxK.Record || x.K == FxK.Replay);

        /// <summary>받아 적기 — 바로 앞에 낸 다른 아군 카드 n 장(기본 1)을 그 사도 쪽 목록 끝에(최대 RECORD_MAX — 넘치면 첫 쪽이 밀려난다).</summary>
        void RecordFx(Fx f, Unit owner)
        {
            if (owner == null) return;
            int n = Math.Max(1, f.IV), got = 0;
            if (!Records.TryGetValue(owner.Key, out var pages)) Records[owner.Key] = pages = new List<string>();
            for (int i = RecentPlays.Count - 1; i >= 0 && i >= RecentPlays.Count - n; i--)   // 「바로 앞」 n 장만 — 자기 카드가 끼면 그 자리는 건너뛰지 않고 빈다
            {
                var id = RecentPlays[i]; var c = CardOf(id);
                if (c?.Hero == null || c.Hero == owner.Key || !Recordable(c)) continue;
                pages.Add(id); got++;
                Say($"{owner.Name} — 「{c.Name}」 를 받아 적는다");
            }
            while (pages.Count > RECORD_MAX) pages.RemoveAt(0);
            if (got > 0) StatusCue(owner, $"받아 적기 {pages.Count}쪽", true);
        }

        /// <summary>낭독 — 쪽 n 장(all 이면 모두)을 첫 쪽부터 그 카드 주인의 능력치로 ratio 만큼 다시 발동. keep 이 아니면 그 쪽을 지운다. 재현은 받아 적기를 다시 깨우지 않는다.</summary>
        void ReplayFx(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            if (owner == null || !Records.TryGetValue(owner.Key, out var pages) || pages.Count == 0) return;
            int n = f.All ? pages.Count : Math.Min(pages.Count, Math.Max(1, f.IV));
            var take = pages.Take(n).ToList();
            if (!f.Keep) pages.RemoveRange(0, n);
            foreach (var id in take)
            {
                if (Over != null) return;
                var cv = CardOf(id); var who = cv?.Hero != null ? HeroUnit(cv.Hero) : null;
                if (cv == null || who == null || who.Dead) continue;
                Say($"낭독 — 「{cv.Name}」({Num.Round((f.Ratio > 0 ? f.Ratio : 1) * 100)}%)");
                StatusCue(owner, $"낭독 「{cv.Name}」", true);
                var (prev, src0) = (Acting, ModSrc);
                Acting = who.Key; ModSrc = $"낭독 「{cv.Name}」";
                try { RunFx(cv.Fx, new FxCtx { Owner = who, TargetIdx = ctx.TargetIdx, HitTags = CardHitTags(id, cv), Card = true, Type = cv.Type, CardId = id, Cost = cv.Cost, Scale = f.Ratio > 0 ? f.Ratio : 1 }); }
                finally { Acting = prev; ModSrc = src0; }
            }
        }

        /// <summary>낸 카드를 최근 목록에 — PlayCard 끝에서.</summary>
        void NotePlayed(string cardId)
        {
            RecentPlays.Add(cardId); RecentTurns.Add(Turn);
            while (RecentPlays.Count > 6) RecentPlays.RemoveAt(0);
            while (RecentTurns.Count > RecentPlays.Count) RecentTurns.RemoveAt(0);
        }

        // ── 고른 만큼 나눠 입히기 · 가장 비싼 카드 · 붙은 동안 보존(▶E42 · E43 · E45) ───────
        /// <summary>손의 그 카드들 가운데 가장 비싼(지금 비용) 것 — 같으면 앞의 것.</summary>
        string Priciest(IEnumerable<string> ids) => ids.Select((id, i) => (id, i, c: CostOf(id))).OrderByDescending(x => x.c).ThenBy(x => x.i).Select(x => x.id).FirstOrDefault();

        /// <summary>나눠 입힐 카드 차례 — 고른 것(PlayOpts.Spread · 맞는 것만) 먼저, 모자라면 비싼 카드부터 돌아가며.</summary>
        List<string> SpreadTargets(List<string> cands, int n)
        {
            var o = new List<string>();
            if (spreadPick != null) foreach (var x in spreadPick) { if (o.Count >= n) break; if (cands.Contains(x)) o.Add(x); }
            var order = cands.Select((id, i) => (id, i, c: CostOf(id))).OrderByDescending(x => x.c).ThenBy(x => x.i).Select(x => x.id).Distinct().ToList();
            for (int k = 0; o.Count < n && order.Count > 0; k++) o.Add(order[k % order.Count]);
            return o;
        }

        /// <summary>그 카드에 「붙은 동안 보존」 카드 값이 있나(▶E43).</summary>
        bool KeepByVal(string id)
        {
            if (Counts.Count == 0 || !CardSt.TryGetValue(id, out var st)) return false;
            foreach (var k in st.Keys) if (Counts.ContainsKey("keepSt|" + k)) return true;
            return false;
        }
        /// <summary>낸 뒤 — 붙은 동안 보존인 카드 값을 벗는다.</summary>
        void DropKeepVals(string id)
        {
            if (Counts.Count == 0 || !CardSt.TryGetValue(id, out var st)) return;
            foreach (var k in st.Keys.ToList()) if (Counts.ContainsKey("keepSt|" + k)) DelCardSt(id, k);
        }

        /// <summary>고른 만큼 나눠 입히는 카드(cardStatus pick)의 후보(화면 · 봇) — 손에서 그 거르개에 맞는 카드. 그런 카드가 아니면 빈 목록.</summary>
        public List<string> SpreadCands(int handIdx)
        {
            if (handIdx < 0 || handIdx >= Hand.Count) return new List<string>();
            var c = CardOf(Hand[handIdx]);
            var f = c?.Fx.FirstOrDefault(x => x.K == FxK.CardStatus && x.Pick);
            if (f == null) return new List<string>();
            var owner = c.Hero != null ? HeroUnit(c.Hero) : null;
            return Hand.Where((id, i) => i != handIdx && FxMatch(f, id, owner)).Distinct().ToList();
        }
    }
}
