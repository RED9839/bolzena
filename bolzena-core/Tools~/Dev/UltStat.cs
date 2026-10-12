using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bolzena.Core;

/// <summary>
/// 고학년 영향 측정(2026-10-12, 도구만 — 엔진은 안 건드린다). 숙련 봇 판을 돌리며 싸움마다 연출 쪽지(OnCue)를 엿들어
///   ① 고학년 한 번이 바로 낸 것(적 HP 깎임 · 파티 실드 늘어남 · 회복) — 고학년 쪽지부터 다음 행동 쪽지까지
///   ② 고학년을 쓴 턴(내 턴 + 이어지는 적의 차례)의 파티 받은 피해 · 막은 양 · 회복 · 적 HP 깎임 — 같은 사도가 낀 싸움의 「고학년 없는 턴」 평균과 나란히
///   ultstat [바퀴 400] [--seed 0] [--party role|random] [--out 파일.md]
/// </summary>
static class UltStat
{
    sealed class Agg
    {
        public long Ults, Fights;
        // 바로 낸 것
        public double ImmDmg, ImmShield, ImmHeal;
        // 그 턴(내 턴 + 적의 차례)
        public long UltTurns; public double UIn, UGuard, UHeal, UDmg;
        // 같은 사도가 낀 싸움의 고학년 없는 턴
        public long BaseTurns; public double BIn, BGuard, BHeal, BDmg;
        public void Add(Agg o)
        {
            Ults += o.Ults; Fights += o.Fights; ImmDmg += o.ImmDmg; ImmShield += o.ImmShield; ImmHeal += o.ImmHeal;
            UltTurns += o.UltTurns; UIn += o.UIn; UGuard += o.UGuard; UHeal += o.UHeal; UDmg += o.UDmg;
            BaseTurns += o.BaseTurns; BIn += o.BIn; BGuard += o.BGuard; BHeal += o.BHeal; BDmg += o.BDmg;
        }
    }

    sealed class Snap
    {
        public Dictionary<Unit, int> Foe = new();
        public int Inc, Guard, Healed, Shield;
        public static Snap Of(Battle b)
        {
            var s = new Snap { Inc = b.SumIncoming, Guard = b.SumGuarded, Healed = b.SumHealed, Shield = b.Pool?.Shield ?? 0 };
            foreach (var e in b.Enemies) s.Foe[e] = Math.Max(0, e.Hp);
            return s;
        }
        public double FoeDrop(Battle b) { double d = 0; foreach (var kv in Foe) d += Math.Max(0, kv.Value - Math.Max(0, kv.Key.Dead ? 0 : kv.Key.Hp)); return d; }
    }

    /// <summary>싸움 하나를 엿듣는다 — 턴 창 · 고학년 창.</summary>
    sealed class Watch
    {
        readonly Battle b; readonly Dictionary<string, Agg> agg; readonly List<string> party;
        Snap turn; readonly HashSet<string> ultIn = new();
        Snap imm; string immHero;
        public Watch(Battle b, Dictionary<string, Agg> agg, List<string> party)
        {
            this.b = b; this.agg = agg; this.party = party;
            foreach (var k in party) A(k).Fights++;
            turn = Snap.Of(b);
            b.OnCue += OnCue;
        }
        Agg A(string k) { if (!agg.TryGetValue(k, out var a)) agg[k] = a = new Agg(); return a; }
        void OnCue(Cue c)
        {
            if (c.K == "act" || c.K == "turn") CloseImm();
            if (c.K == "turn") { CloseTurn(); turn = Snap.Of(b); }
            if (c.K == "act" && c.Anim == "ult" && c.Side == Side.Party)
            {
                var u = b.Party.FirstOrDefault(x => x.Idx == c.Idx);
                if (u != null) { immHero = u.Key; imm = Snap.Of(b); ultIn.Add(u.Key); A(u.Key).Ults++; }
            }
        }
        void CloseImm()
        {
            if (imm == null) return;
            var a = A(immHero);
            a.ImmDmg += imm.FoeDrop(b);
            a.ImmShield += Math.Max(0, (b.Pool?.Shield ?? 0) - imm.Shield);
            a.ImmHeal += b.SumHealed - imm.Healed;
            imm = null; immHero = null;
        }
        void CloseTurn()
        {
            if (turn == null) return;
            double inc = b.SumIncoming - turn.Inc, gd = b.SumGuarded - turn.Guard, he = b.SumHealed - turn.Healed, dm = turn.FoeDrop(b);
            if (ultIn.Count > 0)
                foreach (var k in ultIn) { var a = A(k); a.UltTurns++; a.UIn += inc; a.UGuard += gd; a.UHeal += he; a.UDmg += dm; }
            else
                foreach (var k in party) { var a = A(k); a.BaseTurns++; a.BIn += inc; a.BGuard += gd; a.BHeal += he; a.BDmg += dm; }
            ultIn.Clear();
        }
        public void Close() { CloseImm(); CloseTurn(); turn = null; b.OnCue -= OnCue; }
    }

    public static string Run(GameData d, int rounds, int seed, string partyMode, int gauge = 0)
    {
        var t0 = DateTime.Now;
        var jobs = PartyPick.Jobs(d, rounds, seed, partyMode);
        var all = new Dictionary<string, Agg>(); var gate = new object();
        var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };
        Parallel.ForEach(jobs, po, j =>
        {
            var mine = new Dictionary<string, Agg>(); Watch w = null;
            var P = new SimOpts { Skilled = true };
            P.OnFight = (b, run) => { w?.Close(); if (gauge > 0) b.Gauge = Math.Max(b.Gauge, gauge); w = new Watch(b, mine, j.party); };   // --gauge: 싸움마다 게이지를 채워 고학년 한 번의 값을 잰다(판 결과는 보지 않는다)
            P.OnEnd = run => { w?.Close(); w = null; };
            new RunBot(d).RunFull(j.party, j.seed, P);
            w?.Close();
            lock (gate) foreach (var kv in mine) { if (!all.TryGetValue(kv.Key, out var a)) all[kv.Key] = a = new Agg(); a.Add(kv.Value); }
        });
        var sb = new StringBuilder();
        sb.AppendLine($"# 고학년 영향 — 숙련 봇 · {partyMode} 편성 · {jobs.Count}판 · 씨앗 {seed}{(gauge > 0 ? $" · 싸움 시작 게이지 {gauge}%(고학년 한 번 값 재기용 — 판 결과 아님)" : "")} · {(DateTime.Now - t0).TotalSeconds:0}초");
        sb.AppendLine();
        sb.AppendLine("- **바로**: 고학년 한 번이 곧바로 낸 것(다음 행동 전까지) — 적 HP 깎임 · 파티 실드 늘어남 · 채운 회복.");
        sb.AppendLine("- **그 턴**: 고학년을 쓴 턴(내 턴 + 이어지는 적의 차례) 평균 — 받음(적 공격이 파티에 든 양, 막힌 몫 포함) · 막음(방어 · 실드) · 회복 · 적 HP 깎임. 「평소」 는 같은 사도가 낀 싸움의 고학년 없는 턴.");
        sb.AppendLine("- **HP 잃음** = 받음 − 막음 − 회복(그 턴 파티가 실제로 깎인 몫, 음수면 더 채움).");
        sb.AppendLine();
        sb.AppendLine("| 사도 | 역할 | 비용 | 싸움당 | 바로 적 피해 | 바로 실드 | 바로 회복 | 그 턴 받음 | 그 턴 막음 | 그 턴 회복 | 그 턴 HP 잃음 | 평소 HP 잃음 | 그 턴 적 피해 | 평소 적 피해 |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var kv in all.OrderBy(kv => "탱커서포터딜러".IndexOf(d.Hero(kv.Key).Role ?? "딜러")).ThenBy(kv => d.Hero(kv.Key).Name, StringComparer.Ordinal))
        {
            var h = d.Hero(kv.Key); var a = kv.Value;
            double U(double v) => a.Ults > 0 ? v / a.Ults : 0;
            double T(double v) => a.UltTurns > 0 ? v / a.UltTurns : 0;
            double B(double v) => a.BaseTurns > 0 ? v / a.BaseTurns : 0;
            sb.AppendLine($"| {h.Name} | {h.Role} | {h.Ult?.Cost} | {(a.Fights > 0 ? (double)a.Ults / a.Fights : 0):0.00} | {U(a.ImmDmg):0} | {U(a.ImmShield):0} | {U(a.ImmHeal):0} | {T(a.UIn):0} | {T(a.UGuard):0} | {T(a.UHeal):0} | {T(a.UIn - a.UGuard - a.UHeal):0} | {B(a.BIn - a.BGuard - a.BHeal):0} | {T(a.UDmg):0} | {B(a.BDmg):0} |");
        }
        sb.AppendLine();
        sb.AppendLine("## 고학년 값어치(데이터 — CardValue.ValueOf, 변신은 FormValue · 검사기와 같은 셈)");
        sb.AppendLine();
        sb.AppendLine("| 사도 | 역할 | 비용 | 값어치 | 게이지 100% 당 | 효과 수 |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var h in d.Heroes.Values.Where(x => x.Ult != null).OrderBy(x => "탱커서포터딜러".IndexOf(x.Role ?? "딜러")).ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            double v = CardValue.ValueOf(h.Ult.Fx.Where(f => f.K != FxK.Form).ToList()) + h.Ult.Fx.Where(f => f.K == FxK.Form).Sum(f => CardValue.FormValue(d.Form(f.Id), d, h.Id));
            sb.AppendLine($"| {h.Name} | {h.Role} | {h.Ult.Cost} | {v:0.00} | {v * 100 / Math.Max(1, h.Ult.Cost):0.00} | {h.Ult.Fx.Count} |");
        }
        return sb.ToString();
    }
}
