using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 엘다인 문법(2026-10-11, Docs/데이터.md §22) — 엘다인 20명 개인 엔진에 넣은 부품.
    /// 파티 실드 소모(spendShield) · 실드 비례(perShield) · 변신 조건(ifForm) · 변신 연장(formExtend) · 이번엔 망각 없이(noForget) ·
    /// 변신 카드 생성(FormDef.TurnMake) · 손의 카드 값 합 · 이번 턴 사라진 카드 수.
    /// </summary>
    public sealed partial class Battle
    {
        const string SHIELD_PER = "\u0000실드", GONET_PER = "\u0000턴소멸", CSHAND_PER = "\u0000손카드값";

        // ── 결정 지점 측정(▶E30 — bz sim --trace 사도) ─────────────────────
        /// <summary>재는 사도(null 이면 안 잰다). 실제 판만(봇 미리보기 복사본은 안 센다).</summary>
        public static string TraceHero;
        /// <summary>셈 — turns(그 사도가 있는 턴) · played(그 사도 카드를 낸 턴) · held(그 사도 카드를 쥐고 턴을 마침) · heldPlayable(낼 AP 가 있는데 쥠) ·
        /// tokenHeld(그 사도 생성 카드를 쥐고 마침) · shieldKept(실드 소모 카드를 쥐고 마침) · shieldSpend · shieldPartial · pick · pickPartial(고른 만큼에서 전부가 아닌 수) ·
        /// formIn · formInTurn:n · formOff:time|card|until|switch · formTurns(변신 턴 합).</summary>
        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> Trace = new();
        void TraceAdd(string k, long v = 1)
        {
            if (!capMeter || TraceHero == null) return;
            Trace.AddOrUpdate(k, v, (_, x) => x + v);
            Trace.AddOrUpdate(FightKind + "|" + k, v, (_, x) => x + v);   // 싸움 종류별(엘다인 4차 — 결정 지점 목표는 보스 · 엘리트전 기준)
        }
        /// <summary>싸움 종류 — boss(보스 · 클론이 있음) · elite(엘리트 싸움) · normal.</summary>
        public string FightKind => Enemies.Any(e => e.Boss || Data.Enemy(e.Key)?.Clone != null) ? "boss" : EliteFight ? "elite" : "normal";
        /// <summary>긴 싸움인가(보스 · 엘리트) — 봇이 쥐기를 더 적극적으로 본다(Bots.KeepWorth).</summary>
        public bool LongFight => FightKind != "normal";

        /// <summary>턴을 마치기 직전 — 그 사도 카드를 쥐고 넘기나.</summary>
        void TraceTurnEnd()
        {
            if (TraceHero == null || !capMeter) return;
            var u = HeroUnit(TraceHero);
            if (u == null || u.Dead) return;
            TraceAdd("turns");
            if (PlayedBy.TryGetValue(TraceHero, out var pb) && pb > 0) TraceAdd("played");
            var mine = Hand.Where(h => { var c = CardOf(h); return c != null && c.Hero == TraceHero && !c.IsStatus && !c.IsCurse; }).ToList();
            if (mine.Count == 0) return;
            TraceAdd("held");
            if (mine.Any(h => CanPlay(h) == null)) TraceAdd("heldPlayable");
            if (mine.Any(h => Data.Card(h)?.Token ?? false)) TraceAdd("tokenHeld");
            if (mine.Any(h => CardOf(h).Fx.Any(f => f.K == FxK.SpendShield))) TraceAdd("shieldKept");
            // (엘다인 4차 ▶E87) 낼 수 있는 보존 카드를 쥐고 넘김 — 까닭(때 조각)별로 · 다음에 낼 때 셈하려고 표시
            var keepers = mine.Where(h => HasTagB(h, Tag.Keep) && !HasTagB(h, Tag.Evaporate) && CanPlay(h) == null).Distinct().ToList();
            if (keepers.Count > 0) TraceAdd("keepHeld");
            foreach (var h in keepers)
            {
                var t = HoldTiming.Of(this, h);
                TraceAdd("keepWhy:" + (t.Why ?? "none") + (t.Why != null && !t.Hold ? "(때인데 쥠)" : ""));
                Counts["keepHeld|" + h] = Turn;
            }
        }

        /// <summary>(▶E87) 쥐고 넘긴 보존 카드를 냈다 — 그때 때가 맞았나(keepPaid · keepUnpaid).</summary>
        void TraceKeepPlay(string cardId, string hero)
        {
            if (TraceHero == null || !capMeter || hero != TraceHero || Counts.Count == 0 || !Counts.Remove("keepHeld|" + cardId)) return;
            var t = HoldTiming.Of(this, cardId);
            TraceAdd(t.Why == null ? "keepPlayed" : t.Now >= 1 ? "keepPaid" : "keepUnpaid");
        }

        /// <summary>지금 파티 실드(방어 포함) — 실드 소모 · 실드 비례가 본다.</summary>
        public int PartyShield => Pool == null ? 0 : Pool.Block + Pool.Shield;

        /// <summary>엘다인 비례의 수 — 해당 없으면 -1.</summary>
        int EldainCount(FxCtx ctx, string id)
        {
            if (id.StartsWith(SHIELD_PER, StringComparison.Ordinal)) return PartyShield / Math.Max(1, int.Parse(id.Substring(SHIELD_PER.Length)));
            if (id.StartsWith(GONET_PER, StringComparison.Ordinal))
            {   // 이번 턴 사라진 카드 수(id 가 있으면 그 카드만)
                var cid = id.Substring(GONET_PER.Length);
                return Counts.TryGetValue(GoneTurnKey(cid.Length == 0 ? "*" : cid), out var v) ? v : 0;
            }
            if (id.StartsWith(CSHAND_PER, StringComparison.Ordinal))
            {   // 손의 카드 값 합 — 「사도 키|값 이름」(사도 키가 비면 누구 카드든)
                var p = id.Substring(CSHAND_PER.Length).Split(new[] { '|' }, 2);
                int sum = 0;
                foreach (var h in Hand) if (p[0].Length == 0 || CardOf(h)?.Hero == p[0]) sum += Math.Max(0, CardStOf(h, p[1]));
                return sum;
            }
            return -1;
        }

        string GoneTurnKey(string cardBase) => "goneT|" + Turn + "|" + cardBase;
        /// <summary>소멸 셈(이번 턴) — Emit("exhaust") 가 부른다.</summary>
        void CountGoneTurn(string id)
        {
            foreach (var k in new[] { GoneTurnKey("*"), id != null ? GoneTurnKey(GameData.BaseId(id)) : null })
                if (k != null) Counts[k] = (Counts.TryGetValue(k, out var v) ? v : 0) + 1;
        }

        bool FxEldain(Fx f, FxCtx ctx, ref bool gate)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                case FxK.SpendShield: ctx.EventV = SpendShieldFx(f, ctx); return true;
                case FxK.PerShield: ctx.PerStack = SHIELD_PER + (f.Per > 0 ? f.Per : 1); ctx.PerMax = f.Max; return true;
                case FxK.IfForm:
                    {
                        bool on = owner != null && Forms.TryGetValue(owner.Key, out var fr) && (f.Id == null || fr.Id == f.Id);
                        gate = f.Not ? !on : on;
                        return true;
                    }
                case FxK.FormExtend: FormExtendFx(f, owner); return true;
                case FxK.NoForget: ctx.NoForget = true; return true;
                case FxK.IfEvent:   // (3차) 앞 조각의 일의 값(쓴 양 · 깎은 양 …, 없으면 이 카드가 버린 장수)이 n 이상이면
                    {
                        int ev = ctx.EventV > 0 ? ctx.EventV : ctx.Discarded;
                        gate = ev >= Math.Max(1, f.N); if (f.Not) gate = !gate;
                        return true;
                    }
            }
            return FxEldain3(f, ctx, ref gate);
        }

        // ── 실드 소모(▶E1) ─────────────────────────────────────────────
        /// <summary>파티 실드를 쓴다 — half 절반(내림) · all 전부 · pick 고른 만큼(안 고르면 절반) · v 그만큼. 방어(옛 block)부터 깎는다. 쓴 양을 돌려준다(일의 값).</summary>
        int SpendShieldFx(Fx f, FxCtx ctx)
        {
            int have = PartyShield;
            if (have <= 0 || Pool.Dead) { Say("파티 실드가 없다 — 쓸 것이 없다"); return 0; }
            int take = f.All ? have : f.Pick ? Math.Max(1, Math.Min(have, ctx.SpendPick ?? have / 2)) : f.Half ? have / 2 : Math.Min(have, Math.Max(0, f.IV));
            if (take <= 0) return 0;
            int fromBlock = Math.Min(Pool.Block, take);
            Pool.Block -= fromBlock; Pool.Shield -= take - fromBlock;
            LoseGuardCue(PartyRep(ctx.Owner), take);
            Say($"파티 실드 {take} 을 쓴다({have} → {PartyShield})");
            if (ctx.Owner?.Key == TraceHero) { TraceAdd("shieldSpend"); if (take < have) TraceAdd("shieldPartial"); }
            return take;
        }

        /// <summary>파티 실드를 고른 만큼 쓰는 카드(spendShield pick)의 후보 — 절반 · 전부(겹친 것은 하나로). 그런 카드가 아니거나 실드가 없으면 빈 목록. 고른 수는 PlayOpts.Spend 로.</summary>
        public List<int> ShieldChoices(string cardId)
        {
            var c = CardOf(cardId);
            if (c == null || !c.Fx.Any(x => x.K == FxK.SpendShield && x.Pick)) return new List<int>();
            int have = PartyShield;
            if (have <= 0) return new List<int>();
            return new[] { Math.Max(1, have / 2), have }.Distinct().ToList();
        }

        // ── 변신 연장(▶E26) ────────────────────────────────────────────
        void FormExtendFx(Fx f, Unit owner)
        {
            if (owner == null || !Forms.TryGetValue(owner.Key, out var fr) || (f.Id != null && fr.Id != f.Id)) return;
            var fd = FormDefOf(owner.Key);
            if (fd == null || fd.Turns <= 0) return;
            int v = Math.Max(1, f.IV);
            int room = f.Max > 0 ? f.Max - (fd.Turns + fr.Ext) : v;
            int add = Math.Min(v, room);
            if (add <= 0) { Say($"{owner.Name} — 「{fd.Name}」 이미 최대 {f.Max}턴"); return; }
            fr.Left += add; fr.Ext += add;
            Say($"{owner.Name} — 「{fd.Name}」 +{add}턴(남은 {fr.Left}턴)");
            StatusCue(owner, $"「{fd.Name}」 +{add}턴", true);
            Cue("formOn", owner, new Cue { Hero = owner.Key, Id = fd.Id, Name = fd.Name, Label = fd.Skin, T = fd.Anim, V = fr.Left });
        }

        // ── 변신 카드 생성(▶E6) ───────────────────────────────────────
        /// <summary>그 사도의 변신 카드(조건 없는 줄) — 화면 미리보기 · 글. 변신 중이 아니면 빈 목록.</summary>
        public List<FormMake> FormTurnMake(string heroKey) => FormDefOf(heroKey)?.TurnMake?.Where(m => m.When == null).ToList() ?? new List<FormMake>();

        /// <summary>변신 카드를 손에 — 변신에 드는 순간 · 내 턴 시작(조건 없는 줄만).</summary>
        void FormMakeCards(Unit owner, FormDef fd)
        {
            if (owner == null || owner.Dead || fd?.TurnMake == null || fd.TurnMake.Count == 0 || Over != null) return;
            var (prev, seq0) = (Acting, ActSeq);
            Acting = owner.Key; ActSeq = ++SeqN;
            try
            {
                foreach (var m in fd.TurnMake)
                {
                    if (m.When != null || m.Id == null) continue;
                    Make(m.Id, m.NOr1, owner);
                    if (Over != null) return;
                }
            }
            finally { Acting = prev; ActSeq = seq0; }
        }

        /// <summary>내 턴 시작 — 변신 중인 사도마다 변신 카드.</summary>
        void FormTurnStart()
        {
            if (Forms.Count == 0) return;
            foreach (var key in Forms.Keys.ToList())
            {
                if (Over != null) return;
                var u = HeroUnit(key);
                if (u == null || u.Dead) continue;
                FormMakeCards(u, FormDefOf(key));
            }
        }

        /// <summary>조건부 변신 카드 — 그 고유 효과가 n 이상이 되는 순간 그 턴 한 번(엔진이 막는 상한 — 「기혈 폭발」).</summary>
        void FormStackMake(string id, int before, int after)
        {
            if (!Kw.TryGetValue(id, out var kw) || kw.Carrier != "self") return;
            var owner = HeroUnit(kw.Owner);
            if (owner == null || owner.Dead || !Forms.ContainsKey(owner.Key)) return;
            var fd = FormDefOf(owner.Key);
            if (fd?.TurnMake == null) return;
            for (int i = 0; i < fd.TurnMake.Count; i++)
            {
                var m = fd.TurnMake[i];
                if (m.When == null || m.When.Stack != id || m.Id == null) continue;
                int n = Math.Max(1, m.When.N);
                if (!(before < n && after >= n)) continue;
                string k = $"formMake|{owner.Key}|{fd.Id}|{i}";
                if (Counts.TryGetValue(k, out var t) && t == Turn) continue;
                Counts[k] = Turn;
                var (prev, seq0) = (Acting, ActSeq);
                Acting = owner.Key; ActSeq = ++SeqN;
                try { Make(m.Id, m.NOr1, owner); } finally { Acting = prev; ActSeq = seq0; }
                if (Over != null) return;
            }
        }
    }
}
