using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 키워드 사전(Docs/키워드.md, 2026-10-05)의 부품 — 카드에 붙는 상태 · 행동 카운트 · 망각 · 소각 · 축복 · 결속/진화 합치기 ·
    /// 추가 공격 · 공명 · 탄성 · 응징 · 턴 시작 상태(1턴 · 턴 수 · 다음 턴 드로우 · 집중 · 초재생).
    /// </summary>
    public sealed partial class Battle
    {
        // ── 읽기(화면) ────────────────────────────────────────────────
        /// <summary>행동 카운트 — 이 적이 행동하기까지 남은 카드 수(둔화 · 급속 반영). 당겨지지 않는 수 · 이미 행동했으면 null.</summary>
        public int? ActionCount(Unit e)
        {
            if (e == null || e.Dead || e.Intent == null || e.Sealed || e.RushedTurn) return null;
            int n = RushNeed(e);
            return n > 0 ? Math.Max(0, n - e.RushCnt) : (int?)null;
        }

        /// <summary>파티 층 상태(파티 HP 밑에 보일 것) — 사기 · 불굴 · 취약 … 파티원 전원에게 든다.</summary>
        public Dictionary<string, int> PartyStatus() => new Dictionary<string, int>(Pool.Status);

        /// <summary>개인(사도) 층 상태(초상 밑에 보일 것) — 구속 + 그 사도 고유 효과(키워드 carrier self)의 겹.</summary>
        public Dictionary<string, int> HeroStatus(Unit hero)
        {
            var o = new Dictionary<string, int>();
            if (hero == null) return o;
            foreach (var kv in hero.Status) if (kv.Value != 0) o[kv.Key] = kv.Value;
            if (Stacks.TryGetValue(hero.Key, out var bag)) foreach (var kv in bag) if (kv.Value != 0) o[kv.Key] = kv.Value;
            return o;
        }

        /// <summary>카드에 붙은 상태의 값(독 · 봉쇄 · 침체 · 빙결 · 탐구심 · 비용).</summary>
        public int CardStOf(string cardId, string st) => cardId != null && CardSt.TryGetValue(cardId, out var d) && d.TryGetValue(st, out var v) ? v : 0;
        /// <summary>결속으로 겹친 수(1 ~ 5).</summary>
        public int BondOf(string cardId) => cardId != null && Bond.TryGetValue(cardId, out var n) ? n : 1;
        /// <summary>빙결 — 이번 턴 낼 수 없다.</summary>
        public bool IsFrozen(string cardId) => cardId != null && Frozen.TryGetValue(cardId, out var t) && t == Turn;

        // ── 카드에 붙는 상태 ──────────────────────────────────────────
        void AddCardSt(string cardId, string st, int v, int max = 0)
        {
            if (cardId == null || v == 0) return;
            if (max > 0 && v > 0) { v = Math.Min(v, max - CardStOf(cardId, st)); if (v <= 0) return; }
            if (st == "빙결" && Hand.Contains(cardId)) { Frozen[cardId] = Turn; Say($"「{CardOf(cardId)?.Name}」 — 빙결(이번 턴 낼 수 없다)"); return; }
            if (!CardSt.TryGetValue(cardId, out var d)) CardSt[cardId] = d = new Dictionary<string, int>();
            int n = (d.TryGetValue(st, out var x) ? x : 0) + v;
            if (n == 0 && st == "비용") d.Remove(st); else if (n <= 0 && st != "비용") d.Remove(st); else d[st] = n;
            if (d.Count == 0) CardSt.Remove(cardId);
            Say($"「{CardOf(cardId)?.Name}」 — {st} {(v > 0 ? "+" : "")}{v}");
        }
        void DelCardSt(string cardId, string st)
        {
            if (cardId != null && CardSt.TryGetValue(cardId, out var d)) { d.Remove(st); if (d.Count == 0) CardSt.Remove(cardId); }
        }

        /// <summary>카드 고르기 — 손(hand) · 뽑을 더미(draw)에서 무작위 n 장(0 이면 전부). 상태 카드 · 저주는 빼고.</summary>
        List<string> PickCards(string to, int n, Fx filter = null, Unit owner = null)
        {
            bool typed = filter != null && (filter.Type == "상태" || filter.Type == "저주");
            var src = (to == "draw" ? Draw : Hand).Where(id => { var c = CardOf(id); return c != null && (typed || (!c.IsStatus && !c.IsCurse)) && (filter == null || FxMatch(filter, id, owner)); }).Distinct().ToList();
            if (n <= 0 || n >= src.Count) return src;
            var o = new List<string>();
            while (o.Count < n && src.Count > 0) { int i = Rng.Int(src.Count); o.Add(src[i]); src.RemoveAt(i); }
            return o;
        }

        /// <summary>효과 조각 cardStatus — 이 카드(this) · 손 · 뽑을 더미의 카드에 상태.</summary>
        void CardStatusFx(Fx f, FxCtx ctx)
        {
            var ids = f.To == "hand" || f.To == "draw" ? PickCards(f.To, f.N, f, ctx.Owner) : f.To == "pulled" ? new List<string> { ctx.Pulled } : new List<string> { ctx.CardId };
            if (f.Battle) BattleVals.Add(f.Id);
            foreach (var id in ids)
            {
                if (id == null) continue;
                int before = CardStOf(id, f.Id);
                AddCardSt(id, f.Id, f.IV == 0 ? 1 : f.IV, f.Max);
                // 성장형 공용 계기 — 카드의 값이 실제로 올랐으면 「카드가 자라면」(비용 · 빙결 · 침체는 자람이 아니다)
                if (CardStOf(id, f.Id) > before && !NOT_GROWTH.Contains(f.Id) && Over == null)
                    Emit("grow", new EmitInfo { Id = id, Hero = CardOf(id)?.Hero, By = ctx.Owner?.Key, Kind = f.Id, V = CardStOf(id, f.Id) - before });
            }
        }
        static readonly HashSet<string> NOT_GROWTH = new() { "비용", "빙결", "침체", "독" };

        // ── 행동 카운트 ───────────────────────────────────────────────
        /// <summary>행동까지 필요한 카드 수 — 수의 즉시 행동 장수 + 둔화 - 급속(최소 1). 0 이면 당겨지지 않는다.</summary>
        int RushNeed(Unit e)
        {
            int n = RushOf(e);
            if (n == 0) return 0;
            return Math.Max(1, n + St(e, "둔화") - St(e, "급속"));
        }

        // ── 소멸 · 망각 · 소각 · 축복 ────────────────────────────────
        /// <summary>카드가 소멸한다 — 망각이면 뽑을 더미 맨 위로. 「소각:」 마디가 있으면 돈다.</summary>
        void Exile(string id, string from, string why = null)
        {
            var c = CardOf(id);
            if (HasTagB(id, Tag.Forget))
            {
                Draw.Add(id); CardCue(id, from, "draw", "forget");
                Say($"「{c?.Name}」 — 망각: 뽑을 더미 맨 위로");
            }
            else { Gone.Add(id); CardCue(id, from, "gone", why); }
            if (c != null && c.Fx.Any(f => f.K == FxK.When && f.On == "burn") && Over == null) CardWhen(id, "burn");
        }

        /// <summary>축복 — 실드가 최대 체력 30% 미만일 때 손에서 버린 더미로 가면 고정 실드 60%(카드 주인 방어력, 교주 카드면 파티 방어력).</summary>
        void BlessDrop(string id)
        {
            if (Pool.Dead || !HasTagB(id, Tag.Blessing)) return;
            if (Pool.Shield >= Pool.MaxHp * R.SV("축복At")) return;
            var c = CardOf(id);
            var owner = c?.Hero != null ? HeroUnit(c.Hero) : null;
            int v = Math.Max(1, Num.Round((owner != null ? DefNow(owner) : PartyDef()) * R.SV("축복")));
            Pool.Shield += v; GainCue(PartyRep(owner), "shield", v);
            Say($"축복 「{c?.Name}」 — 고정 실드 +{v}");
        }

        // ── 결속 · 진화 ───────────────────────────────────────────────
        /// <summary>
        /// 한 더미 안에서 합친다 — 결속(같은 주인의 결속 카드가 한 장으로, 겹친 수 최대 5) · 진화(같은 카드 n 장 → into 한 장).
        /// 합쳐져 사라진 카드는 이 전투에서만 빠진다(덱은 판이 쥐고 있다).
        /// </summary>
        void MergePile(List<string> pile, string name)
        {
            if (pile == null || pile.Count < 2) return;
            // 결속
            var seen = new Dictionary<string, int>();   // 주인 → 남는 카드의 자리
            for (int i = 0; i < pile.Count; i++)
            {
                var d = Data.Card(pile[i]);
                if (d == null || d.Hero == null || !d.Tags.Contains(Tag.Bond)) continue;
                if (!seen.TryGetValue(d.Hero, out var at)) { seen[d.Hero] = i; continue; }
                var keep = pile[at]; var drop = pile[i];
                int sum = Math.Min(5, BondOf(keep) + (drop == keep ? 1 : BondOf(drop)));
                Bond[keep] = sum;
                pile.RemoveAt(i); i--;
                CardCue(drop, name, "gone", "bond");
                Say($"결속 — 「{CardOf(keep)?.Name}」 에 겹친다({sum})");
            }
            // 진화
            for (int guard = 0; guard < 10; guard++)
            {
                string hit = null; int need = 0;
                foreach (var id in pile)
                {
                    var d = Data.Card(id); if (d?.Evolve == null || d.Evolve.Into == null) continue;
                    int n = Math.Max(2, d.Evolve.N);
                    if (pile.Count(x => GameData.BaseId(x) == d.Id) >= n) { hit = d.Id; need = n; break; }
                }
                if (hit == null) break;
                int first = pile.FindIndex(x => GameData.BaseId(x) == hit);
                for (int k = 0; k < need; k++) { int j = pile.FindIndex(x => GameData.BaseId(x) == hit); CardCue(pile[j], name, "gone", "evolve"); pile.RemoveAt(j); }
                var into = Data.Card(hit).Evolve.Into + GameData.PLAIN;
                pile.Insert(Math.Min(first, pile.Count), into);
                CardCue(into, "new", name, "evolve");
                Say($"진화 — 「{Data.Card(hit).Name}」 {need}장 → 「{Data.Card(into)?.Name}」");
            }
        }

        /// <summary>사기 몫 — 파티 사기 겹마다 카드 계수 +20%p(합연산).</summary>
        double Morale() => R.StackEff("사기", St(Pool, "사기"));

        /// <summary>협공 · 반격을 건 사도(없거나 쓰러졌으면 null).</summary>
        Unit GiverOf(string id) => Pool.Giver.TryGetValue(id, out var i) ? Party.FirstOrDefault(u => u.Idx == i && !u.Dead) : null;

        // ── 추가 공격 · 공명 · 탄성 ───────────────────────────────────
        /// <summary>추가 공격 한 번 — 공격력(또는 방어 기반) × ratio. 끝나면 탄성 · 「추가 공격하면」.</summary>
        void ExtraHit(Unit hero, Unit t, double ratio, string bas, string label)
        {
            if (hero == null || t == null || t.Dead || Over != null) return;
            int v = HitAmount(hero, ratio + Morale(), bas: bas);
            Say($"{hero.Name}: {label} → {t.Name} ({v})"); StatusCue(hero, label, true);
            Hurt(t, v, new HurtOpts { From = hero, Card = true });
            AfterExtra(hero);
        }

        /// <summary>아군 추가 공격 뒤 — 탄성(중첩마다 치유 50%, 발동하면 사라진다) · 패시브 「추가 공격하면」.</summary>
        void AfterExtra(Unit hero)
        {
            if (Over != null) return;
            int n = St(Pool, "탄성");
            if (n > 0 && !Pool.Dead)
            {
                int h0 = Pool.Hp, v = Math.Max(1, Num.Round(PartyDef() * R.StackEff("탄성", n)));
                Pool.Hp = Math.Min(Pool.MaxHp, Pool.Hp + v); HealCue(PartyRep(hero), h0);
                DelSt(Pool, "탄성");
                Say($"탄성 {n} — 파티 HP +{Pool.Hp - h0}");
            }
            Emit("extra", new EmitInfo { By = hero?.Key, Seq = ActSeq });
        }

        /// <summary>공명 — 카드를 버리면 추가 공격 80%(버린 카드 한 장에 1). 치는 사도는 버린 사도, 없으면 공격력이 가장 높은 사도.</summary>
        void Resonance()
        {
            if (Over != null || St(Pool, "공명") <= 0) return;
            var hero = HeroUnit(Acting) is Unit h && !h.Dead ? h : AliveParty().OrderByDescending(AtkNow).FirstOrDefault();
            var t = AliveEnemies().FirstOrDefault();
            if (hero == null || t == null) return;
            AddSt(Pool, "공명", -1);
            ExtraHit(hero, t, R.SV("공명"), null, "공명!");
        }

        // ── 응징 ──────────────────────────────────────────────────────
        /// <summary>응징 — 걸린 쪽이 방어 · 실드를 얻으면 그쪽 모두에게 고정 피해 200%(건 사람 공격력), 1 준다.</summary>
        void Punish(Unit holder)
        {
            if (holder == null || holder.Dead || St(holder, "응징") <= 0 || Over != null) return;
            int v = Math.Max(1, Num.Round(DotUnit(holder, "응징") * R.SV("응징")));
            AddSt(holder, "응징", -1);
            Say($"{(holder.Side == Side.Party ? "파티" : holder.Name)}: 응징 — 고정 피해 {v}"); StatusCue(holder, "응징!");
            var ts = holder.Side == Side.Party ? new List<Unit> { PartyRep() } : AliveEnemies();
            foreach (var x in ts) { Hurt(x, v, new HurtOpts { Fixed = true }); if (Over != null) return; }
        }

        // ── 턴 시작의 상태 ────────────────────────────────────────────
        /// <summary>내 턴이 시작될 때(2턴부터) — 1턴 상태 지우기 · 초재생 · 턴 수 상태 1 줄이기.</summary>
        void TurnStatusTick()
        {
            var bodies = new List<Unit> { Pool }; bodies.AddRange(Party); bodies.AddRange(Enemies.Where(e => !e.Dead));
            foreach (var u in bodies)
                foreach (var id in R.TURN_ST) if (u.Status.ContainsKey(id)) u.Status.Remove(id);
            if (!Pool.Dead && St(Pool, "초재생") > 0)
            {
                int h0 = Pool.Hp, v = Math.Max(1, Num.Round(PartyDef() * R.SV("초재생")));
                Pool.Hp = Math.Min(Pool.MaxHp, Pool.Hp + v); HealCue(PartyRep(), h0);
                Say($"초재생 — 파티 HP +{Pool.Hp - h0}");
            }
            foreach (var u in new[] { Pool }.Concat(Party))
                foreach (var id in R.TICK_ST)
                    if (u.Status.TryGetValue(id, out var n)) { if (n <= 1) u.Status.Remove(id); else u.Status[id] = n - 1; }
        }

        /// <summary>턴 시작 손패를 뽑은 뒤 — 다음 턴 드로우 · 집중.</summary>
        void AfterTurnDraw()
        {
            int nd = St(Pool, "다음 턴 드로우");
            if (nd > 0) { DelSt(Pool, "다음 턴 드로우"); Say($"다음 턴 드로우 {nd}"); DrawCards(nd, true); }
            if (St(Pool, "집중") > 0 && !Pool.Dead && Pool.Hp < Pool.MaxHp * R.SV("집중")) { GainAp(1); Say("집중 — AP +1"); StatusCue(PartyRep(), "집중 AP +1", true); }
        }

        /// <summary>구속 — 사도에게 걸리면 AP 를 얻고 잃는 효과를 받지 않는다(그 사도 자신의 카드 · 턴 시작 · 격파 몫은 예외).</summary>
        bool BoundBlocks(FxCtx ctx)
        {
            var holders = AliveParty().Where(u => St(u, "구속") > 0).ToList();
            if (holders.Count == 0) return false;
            return !(ctx.Card && ctx.Owner != null && holders.Contains(ctx.Owner));
        }

        /// <summary>열정 — 열정 카드가 나가면 손에 있는 카드의 「열정:」 마디가 돈다.</summary>
        void PassionWake()
        {
            if (Over != null || senseDepth >= 3) return;
            senseDepth++;
            try
            {
                foreach (var id in Hand.ToList())
                    if (Hand.Contains(id) && CardOf(id) is CardView c && c.Fx.Any(f => f.K == FxK.When && f.On == "passion")) { CardWhen(id, "passion"); if (Over != null) return; }
            }
            finally { senseDepth--; }
        }
    }
}
