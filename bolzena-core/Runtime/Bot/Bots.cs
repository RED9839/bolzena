using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 모의전의 손(웹판 tools/lib/bot.js).
    ///   Simple — 아무 생각 없는 손: 글자 규칙(효과 종류)으로 줄 세워 낸다.
    ///   Smart  — 사람만큼 하는 손: 낼 수 있는 수(카드 × 대상 · 고학년)를 판을 복사해 실제로 둬 보고, 둔 뒤의 판을 점수(HP 단위)로 매긴다.
    /// 봇은 난수를 쓰지 않는다 — 복사한 판에는 고정 씨앗(7)을 준다.
    /// </summary>
    public sealed class Bots
    {
        readonly GameData data;
        const int K = R.SCALE;
        public Bots(GameData data) { this.data = data; }
        /// <summary>신탁 고르기를 바꿔 끼운다(숙련 봇 — 파티 축 시너지를 더해 고른다). null 이면 EpiChoice.</summary>
        public Func<Battle, string, int> EpiPick;

        static readonly string[] ATTACKS = { "attack", "back", "multi", "attackAll" };

        // ── 아무 생각 없는 손 ──────────────────────────────────────────
        public void SimplePlay(Battle s, Func<double> r)
        {
            int g = 0;
            while (s.Over == null && g++ < 80)
            {
                foreach (var u in s.Party) if (s.CanUlt(u.Key) == null) { s.UseUlt(u.Key, Target(s)); if (s.Over != null) return; }
                var ok = Enumerable.Range(0, s.Hand.Count).Where(i => s.CanPlay(s.Hand[i], false, i) == null).ToList();
                if (ok.Count == 0) break;
                bool hurt = s.Pool.Hp < s.Pool.MaxHp * 0.5;
                double Score(int i)
                {
                    var c = s.CardOf(s.Hand[i]); int cost = s.CostOf(s.Hand[i], i);
                    if (hurt && c.Fx.Any(f => f.K == FxK.Heal || f.K == FxK.Block || f.K == FxK.Shield)) return 120;
                    if (c.Fx.Any(f => f.K == FxK.Ap && f.V > 0)) return 100;
                    if (c.Fx.Any(f => f.K == FxK.Draw)) return 80;
                    if (c.Fx.Any(f => f.K == FxK.Stack)) return 60 + cost;
                    return 30 + cost * 5 + r();
                }
                bool near = s.AliveEnemies().Any(e => !e.Sealed && e.Intent != null && s.RushOf(e) > 0 && e.RushCnt + 1 >= s.RushOf(e) && ATTACKS.Contains(e.Intent.T));
                bool Calms(int i) => s.CardOf(s.Hand[i]).Fx.Any(f => f.K == FxK.RushDown || (f.K == FxK.Status && f.Id == R.STUN));
                ok = ok.OrderByDescending(i => near && Calms(i) ? 1 : 0).ThenByDescending(Score).ToList();
                if (near && !Calms(ok[0])) break;
                var gid = s.Hand[ok[0]];
                if (s.GlowOf(gid) != null) s.ApplyEpiphany(gid, 0);
                if (!s.PlayCard(ok[0], Target(s)).Ok) break;
            }
        }

        static int Target(Battle s)
        {
            int t = 0, best = int.MaxValue;
            foreach (var e in s.Enemies) if (!e.Dead && e.Hp < best) { best = e.Hp; t = e.Idx; }
            return t;
        }

        // ── 사람만큼 하는 손 ───────────────────────────────────────────
        readonly Dictionary<string, double> cvCache = new();

        public double CardVal(Battle s, string id)
        {
            int fl = s.Flash.TryGetValue(id, out var n) ? n : 0;
            string key = id + ":" + fl + (s.Forms.Count > 0 ? ":" + s.FormKeyOf(id) : "");   // 변신 중이면 그 사도 카드는 변신판 모습
            if (cvCache.TryGetValue(key, out var v)) return v;
            var c = s.CardOf(id);
            v = c == null ? 0 : c.IsCurse || c.IsStatus ? -1 : CardValue.ValueOf(c);
            cvCache[key] = v;
            return v;
        }

        double LiveVal(Battle s, string id, CardView c)
        {
            int fi = FormCondAt(c.Fx);
            if (fi >= 0)
            {   // 「「태세」: …」 · 「변신 중이면」 덤(엘다인 4차 태세) — 그 사도가 그 변신 중이면 덤을 다, 아니면 뺀 값
                var (b0, full) = HoldTiming.Split(c.Fx, new HoldTiming.T("form", 0, 1, fi));
                return FormOn(s, c.Hero, c.Fx[fi].Id) ? CardValue.ValueOf(full, c.Tags) : CardValue.ValueOf(b0, c.Tags);
            }
            if (c.Fx.Any(f => f.K == FxK.IfNth && f.After))
            {   // 「n번째 이후」(엘다인 4차 ▶E77 · E87) — 지금 그 자리면 덤을 다, 이번 턴에 닿을 수 있으면 그만큼(뒤로 미뤄 낼 값)
                var t = HoldTiming.Of(s, id);
                if (t.Why == "nth")
                {
                    var (b, full) = HoldTiming.Split(c.Fx, t);
                    double bv = CardValue.ValueOf(b, c.Tags), fv = CardValue.ValueOf(full, c.Tags);
                    return bv + t.Now * Math.Max(0, fv - bv);
                }
            }
            var oc = CardValue.OrderCond(c.Fx); bool rh = CardValue.RhythmUse(c.Fx);
            if (oc == null && c.Fx.Any(f => f.K == FxK.IfRepeat))
            {
                bool rep = s.PlayIds.Count > 0 && s.PlayIds[s.PlayIds.Count - 1] == GameData.BaseId(id);
                return rep ? CardValue.ValueOf(c, true) : CardVal(s, id);
            }
            if (oc == null && !rh && c.Fx.Any(f => f.K == FxK.IfPricier))
            {   // 직전보다 비싼가(2단계) — 이번 턴 바로 앞 카드보다 비싸면 그 덤이 선다(아니면 옛 값 — 더 싼 카드를 먼저 내면 설 수 있다)
                bool pr = s.Counts.TryGetValue("lastCostT", out var lt) && lt == s.Turn && s.Counts.TryGetValue("lastCost", out var lc) && s.CostOf(id) > lc;
                return pr ? CardValue.ValueOf(c, true) : CardVal(s, id);
            }
            if (oc == null && !rh) return CardVal(s, id);
            var last = s.PlayLog.Count > 0 ? s.PlayLog[s.PlayLog.Count - 1] : ((string hero, string type)?)null;
            bool on = oc != null && last != null && (oc.K == FxK.IfLink ? c.Hero != null && last.Value.hero == c.Hero : last.Value.type == oc.Type);
            if (!on && !rh) return CardVal(s, id);
            int? n = rh ? s.St(s.Pool, R.RHYTHM) : (int?)null;
            string key = id + ":" + (s.Flash.TryGetValue(id, out var f) ? f : 0) + ":live" + (on ? 1 : 0) + (rh ? ":r" + n : "") + (s.Forms.Count > 0 ? ":" + s.FormKeyOf(id) : "");
            if (cvCache.TryGetValue(key, out var v)) return v;
            v = CardValue.ValueOf(c, on, n);
            cvCache[key] = v;
            return v;
        }

        double SeqBonus(Battle s)
        {
            double v = 0;
            foreach (var u in s.AliveParty())
            {
                if (!s.Passives.TryGetValue(u.Key, out var rules)) continue;
                for (int i = 0; i < rules.Count; i++)
                {
                    var w = rules[i].R.When;
                    if (w.Seq == null || w.Seq.Count == 0) continue;
                    int k = s.SeqStep(w, u.Key, s.Counts.TryGetValue($"{u.Key}|{i}|seq|{s.Turn}", out var c) ? c : 0);
                    if (k <= 0 || k >= w.Seq.Count) continue;
                    string want = w.Seq[k];
                    bool can = false;
                    foreach (var id in s.Hand) { var cv = s.CardOf(id); if (cv != null && cv.Type == want && (w.Who == "any" || cv.Hero == u.Key) && s.CanPlay(id) == null) { can = true; break; } }
                    if (can) v += 0.6 * 1.2 * u.Atk * Math.Max(0, CardValue.ValueOf(rules[i].R.Fx)) * ((double)k / w.Seq.Count);
                }
            }
            return v;
        }

        /// <summary>
        /// 박자형 손(1단계) — 셋째 · 넷째 카드에 덤이 붙는 카드 · 패시브가 있나(n장째 3 이상 · 리듬 2 이상 · 같은 사도 3장 연속 · 이번 턴 낸 장수 비례 ·
        /// 한 턴 N장째(3 이상) · 한 턴 N장마다(3 이상) · 이번 턴 N장 이상 냈으면). 있으면 세 수 앞까지 본다.
        /// </summary>
        public static bool Rhythmic(Battle s) =>
            s.Hand.Any(id => { var fx = s.CardOf(id)?.Fx; return fx != null && fx.Any(f => (f.K == FxK.IfNth && f.N >= 3) || (f.K == FxK.IfRhythm && f.N >= 2) || (f.K == FxK.IfStreak && f.N >= 3) || f.K == FxK.PerPlayed || f.K == FxK.IfPricier); })
            || s.AliveParty().Any(u => s.Passives.TryGetValue(u.Key, out var rs) && rs.Any(r => (r.R.When.On == "play" && (r.R.When.Nth >= 3 || (r.R.When.Every >= 3 && r.R.When.PerTurn)))
                || (r.R.When.On == "rhythm" && r.R.When.N >= 3) || r.R.Conds.Any(c => c.C == "playedMin" && c.N >= 2)));

        bool Orderly(Battle s) => s.Hand.Any(id => { var fx = s.CardOf(id)?.Fx; return CardValue.OrderCond(fx) != null || CardValue.RhythmUse(fx) || (fx != null && fx.Any(f => f.K == FxK.IfRepeat || f.K == FxK.IfSpent || f.K == FxK.IfBalanced || Setup(f))); })
            || HasEmpower(s) || ForgetClog(s) >= 3
            || s.Forms.Values.Any(fr => s.Data.Form(fr.Id)?.Toggle != null)   // (우로스 선 · 악) 태세가 번갈아 바뀌면 내는 차례가 덤을 가른다 — 두 수 앞까지
            || s.Hand.Any(id => s.CardOf(id)?.Fx.Any(f => f.K == FxK.Record) ?? false)   // (엘다인 3차) 받아 적기 — 동료 카드를 먼저 내고 받아 적는 두 수   // (엘다인 E9 · 아야) 망각 카드가 손 · 뽑을 더미 위를 막으면 자세를 바꾸고 내는 두 수를 본다
            || s.AliveParty().Any(u => s.Passives.TryGetValue(u.Key, out var rs) && rs.Any(r => r.R.When.Seq != null && r.R.When.Seq.Count > 0));

        /// <summary>뒤 카드를 세게 하는 판 꾸미기(봇 손질 2026-10-08) — 다음 카드 강화 · 아군 주는 피해 · 공격력 증가. 1수만 보면 큰 카드를 먼저 내고 꾸미기를 버린다.</summary>
        static bool Setup(Fx f) => f.K == FxK.Empower || ((f.K == FxK.DealtMod || f.K == FxK.AtkMod) && f.V > 0 && f.Target != "oneEnemy" && f.Target != "allEnemies");
        static bool HasEmpower(Battle s) => s.Counts.Any(kv => kv.Value > 0 && kv.Key.StartsWith("empower|", StringComparison.Ordinal));
        /// <summary>그 사도가 이번 턴 아직 칠 카드가 손에 있나(남은 AP 로 낼 수 있는 피해 카드).</summary>
        static bool CanStillHit(Battle s, Unit u) => s.Hand.Any(id => { var c = s.CardOf(id); return c != null && c.Hero == u.Key && s.CostOf(id) <= s.Ap && c.Fx.Any(f => f.K == FxK.Dmg || f.K == FxK.Extra); });

        readonly Dictionary<string, double> threatCache = new();
        static double HitOf(Intent it) => it == null ? 0 : it.T == "attack" || it.T == "back" ? it.V : it.T == "multi" ? it.V * Math.Max(1, it.N) : it.T == "attackAll" ? it.V * R.FOE_ALL_X
            : it.T == "ult" && it.Then != null ? it.Then.Sum(HitOf) : 0;
        double ThreatOf(string key)
        {
            if (threatCache.TryGetValue(key, out var t)) return t;
            var d = data.Enemy(key);
            var list = d.Intents.Concat(d.Phase?.Intents ?? new List<Intent>()).ToList();
            double sum = 0; int n = 0;
            foreach (var it in list) { sum += HitOf(it.T == "charge" && it.Next != null ? it.Next : it) / (it.T == "charge" ? 2 : 1); n++; }
            t = n > 0 ? sum / n : 5 * K;
            threatCache[key] = t;
            return t;
        }

        double TakenModOf(Battle s)
        {
            double up = 0, down = 0;
            foreach (var h in s.Party) { double v = s.StatMod(h, "taken"); if (v > up) up = v; if (v < down) down = v; }
            return up + down;
        }

        int HitTo(Battle s, Unit e, Unit u, int v, int vulLeft)
        {
            int fort = s.St(s.Pool, "불굴");
            int d = Num.Round(v * (vulLeft > 0 ? 1 + R.SV("취약") : 1) * (1 - R.StackEff("불굴", fort)));
            if (!s.NoNature && u != null)
            {
                int ed = R.NatureEdge(e.Nature, u.Nature);
                if (ed < 0) d = Num.Round(d * (1 - R.NATURE_DEF));   // 적이 이기는 상성 +는 2026-10-10 없앴다(BattleDamage.NatureMod)
            }
            double m = (1 + s.StatMod(e, "dealt")) * (1 + TakenModOf(s));
            return Math.Max(0, Num.Round(d * Math.Max(0.1, m)));
        }

        static Unit PickT(List<Unit> live, bool fromBack) => Battle.PickInOrder(live, fromBack);

        /// <summary>이번 적의 차례에 파티가 잃을 HP + 다음 턴에 쏟을 힘 · 방해.</summary>
        public (int hp, int shield, double later, double misc) Incoming(Battle s)
        {
            var live = s.AliveParty();   // 읽기만 한다(옛 ToList 와 같다)
            int hp = s.Pool.Hp, block = s.Pool.Block, shield = s.Pool.Shield;
            int vul = s.St(s.Pool, "취약");
            double tk = s.Counts.Count > 0 ? s.TauntNow : 0;   // 도발 중이면 그 대에 실드가 (1+k) 배로 막는다(엘다인 4차 ▶E87)
            bool used = false;
            void Done() { if (used) vul = Math.Max(0, vul - 1); used = false; }
            void Hit(Unit e, Unit u, int v, bool pierce)
            {
                used = true;
                int d = HitTo(s, e, u, v, vul);
                if (!pierce) { int a = Math.Min(block, d); block -= a; d -= a; }
                if (tk > 0) { int a = Math.Min(d, (int)Math.Floor(shield * (1 + tk))); shield -= Math.Min(shield, (int)Math.Ceiling(a / (1 + tk))); d -= a; }
                else { int b = Math.Min(shield, d); shield -= b; d -= b; }
                hp -= d;
            }
            double later = 0, misc = 0;
            foreach (var e in s.Enemies)
            {
                if (e.Dead || e.Intent == null || e.Sealed || e.RushedTurn) continue;
                // 보스 클론의 고학년(ult)은 안의 수들을 차례로
                foreach (var it in e.Intent.T == "ult" && e.Intent.Then != null ? e.Intent.Then : new List<Intent> { e.Intent })
                {
                    if (it.T == "attack" || it.T == "back") Hit(e, PickT(live, it.T == "back"), s.Dealt(e, it.V), it.T == "back");
                    else if (it.T == "multi") { int d = s.Dealt(e, it.V); for (int k = 0; k < Math.Max(1, it.N); k++) Hit(e, PickT(live, false), d, false); }
                    else if (it.T == "attackAll") Hit(e, PickT(live, false), s.Dealt(e, Num.Round(it.V * R.FOE_ALL_X)), false);
                    else if (it.T == "charge") later += HitOf(it.Next) * e.Dmgx;
                    else if (it.T == "jam") misc += 5 * K * Math.Max(1, it.V);
                    else if (it.T == "buff") misc += 2 * K * Math.Max(1, it.V);
                    else if (it.T == "debuff") misc += 3 * K * Math.Max(1, it.V);
                    else if (it.T == "heal") misc += it.V * 0.8;
                    else if (it.T == "addCard") misc += 3 * K * Math.Max(1, it.N);
                }
                Done();
            }
            hp += LowHpRescue(s, hp);
            return (hp, shield, later, misc);
        }

        /// <summary>
        /// 「HP가 n% 이하가 되면」(lowHp) — 이번 적의 차례에 선을 넘어 내려가면 돌 회복 · 실드(아직 안 쓴 것만). 봇 손질(2026-10-08):
        /// 옛 셈은 이것을 몰라 위기 패시브가 있는 사도도 HP 가 낮으면 막기만 골랐다.
        /// </summary>
        int LowHpRescue(Battle s, int hp)
        {
            if (hp <= 0 || hp >= s.Pool.Hp) return 0;
            double before = (double)s.Pool.Hp / Math.Max(1, s.Pool.MaxHp), after = (double)hp / Math.Max(1, s.Pool.MaxHp);
            int add = 0;
            foreach (var u in s.AliveParty())
            {
                if (!s.Passives.TryGetValue(u.Key, out var rules)) continue;
                for (int i = 0; i < rules.Count; i++)
                {
                    var r = rules[i].R;
                    if (r.When.On != "lowHp" || !(before > r.When.Pct && after <= r.When.Pct)) continue;
                    string key = $"{u.Key}|{i}|f";
                    int lim = r.Limit != null && r.Limit.Per == "fight" ? r.Limit.N : int.MaxValue;   // 전투당 제한 없음(2026-10-10)
                    if ((s.Fired.TryGetValue(key, out var fv) ? fv : 0) >= lim) continue;
                    foreach (var f in r.Fx)
                    {
                        if (f.K == FxK.Heal && f.Ratio > 0) add += s.HealAmount(u, f.Ratio, null);
                        else if ((f.K == FxK.Shield || f.K == FxK.Block) && f.Ratio > 0 && !f.Fixed && f.OfEvent <= 0) add += s.GuardAmount(u, f.Ratio, null) / 2;   // 실드는 이미 지난 공격 뒤라 반만
                    }
                }
            }
            return Math.Min(add, s.Pool.MaxHp - hp);
        }

        /// <summary>카드의 실드 · 회복 계수 합(조건 · 고정 · 일의 값 실드는 빼고 — 대충).</summary>
        readonly Dictionary<string, (double sh, double he)> guardCache = new();
        (double sh, double he) GuardRatios(Battle s, string id, CardView c)
        {
            string key = id + ":" + (s.Flash.TryGetValue(id, out var n) ? n : 0) + (s.Forms.Count > 0 ? ":" + s.FormKeyOf(id) : "");
            if (guardCache.TryGetValue(key, out var g)) return g;
            double sh = 0, he = 0;
            foreach (var f in c.Fx)
            {
                if ((f.K == FxK.Shield || f.K == FxK.Block) && !f.Fixed && f.OfEvent <= 0 && f.OfStack == null) sh += f.Ratio;
                else if (f.K == FxK.Heal) he += f.Ratio;
            }
            return guardCache[key] = (sh, he);
        }

        double Potential(Battle s, (int hp, int shield, double later, double misc)? known = null)
        {
            if (s.FinaleLock) return 0;   // 종극 — 이번 턴은 끝났다
            int ap = s.Ap;
            var cards = new List<(int cost, double v)>();
            // 실드 · 회복 카드의 값은 지금 판으로(2026-10-08) — 지금 실드로 못 막는 예고 피해 · 잃은 HP
            var inc = known ?? Incoming(s);
            int need = Math.Max(0, s.Pool.Hp - inc.hp), missing = Math.Max(0, s.Pool.MaxHp - s.Pool.Hp);
            bool conv = false;   // 넘친 회복을 바꾸는 패시브가 있나 — LINQ 없이(2026-10-09)
            foreach (var u in s.AliveParty())
            {
                if (!s.Passives.TryGetValue(u.Key, out var rs)) continue;
                foreach (var r in rs) if (r.R.When.On == "overheal") { conv = true; break; }
                if (conv) break;
            }
            // 봇 손질(2026-10-10 빌드 피드백): 켜진 강화 카드의 규칙이 넘친 회복을 바꿔도 같다(힐데 · 서포터 메인딜)
            if (!conv) foreach (var p in s.Powers) { if (p.Rules != null && p.Rules.Any(r => r.When?.On == "overheal" || (r.Fx?.Any(f => f.K == FxK.PerOverheal) ?? false))) { conv = true; break; } }
            foreach (var id in s.Hand)
            {
                var c = s.CardOf(id); if (c == null) continue;
                if (c.Hero != null && s.HeroUnit(c.Hero) == null) continue;
                if (s.IsFrozen(id) || s.CanPlay(id) != null && !(c.HasTag(Tag.Link) || c.HasTag(Tag.Heaven))) continue;
                bool auto = c.HasTag(Tag.Link) || c.HasTag(Tag.Heaven);
                int cost = auto ? 0 : c.X ? Math.Max(1, ap) : s.CostOf(id);
                var o = c.Hero != null ? s.HeroUnit(c.Hero) : null;
                double v = LiveVal(s, id, c);
                var gr = GuardRatios(s, id, c);
                if (gr.sh > 0 || gr.he > 0)
                {
                    var gu = o ?? s.AliveParty().FirstOrDefault();
                    if (gu != null) v += CardValue.LiveGuard(gr.sh, gr.he, s.GuardAmount(gu, 1, null), s.HealAmount(gu, 1, null), need, missing, o != null ? o.Atk : 12 * K);
                    // 봇 손질(2026-10-08): 넘친 회복을 바꾸는 사도(패시브 overheal · 카드 perOverheal)면 HP 가 차도 넘칠 몫에 값이 있다(반값 — 무엇으로 바뀌는지는 판마다)
                    if (gu != null && gr.he > 0 && (conv || c.Fx.Any(f => f.K == FxK.PerOverheal || (f.K == FxK.Power && (f.Rules?.Any(r => r.When?.On == "overheal") ?? false)))))
                        v += 0.5 * Math.Max(0, gr.he * s.HealAmount(gu, 1, null) - missing) * CardValue.HP_W / (1.2 * (o != null ? o.Atk : 12 * K));
                }
                v *= auto ? 0.7 : 1;
                if (v <= 0) continue;
                cards.Add((cost, v * 1.2 * (o != null ? o.Atk : 12 * K)));
            }
            double sum = 0;
            foreach (var x in cards.OrderByDescending(x => x.v / (x.cost + 0.5))) if (x.cost <= ap) { ap -= x.cost; sum += x.v; }
            return sum;
        }

        static readonly Dictionary<string, double> MOD_W = new() { ["atk"] = 12 * K, ["dealt"] = 12 * K, ["crit"] = 4 * K, ["def"] = 8 * K, ["taken"] = -14 * K };
        static readonly Dictionary<string, double> FOE_ST = new()
        {
            ["취약"] = 5 * K, ["약화"] = 2 * K, ["고통"] = 5 * K, ["균열"] = 4 * K, ["손상"] = 0.5 * K, ["표식"] = 9 * K, ["잔불"] = 3 * K, ["그을림"] = 6 * K, ["충격"] = 8 * K, ["충격파"] = 20 * K,
            ["사기"] = -7 * K, ["불굴"] = -8 * K, ["결의"] = -4 * K, ["결정화"] = -3 * K, ["반격"] = -4 * K, ["피해 감소"] = -2 * K, ["면역"] = -3 * K, ["실드 유지"] = -2 * K,
            ["응징"] = 4 * K, ["포자증식"] = 2 * K, ["죽음의 낙인"] = 4 * K, ["열정 약점"] = 2 * K, ["순수 약점"] = 2 * K, ["광기 약점"] = 2 * K, ["냉정 약점"] = 2 * K, ["활발 약점"] = 2 * K, ["우울 약점"] = 2 * K, ["공명 약점"] = 2 * K, ["약점 노출"] = 4 * K, ["둔화"] = 3 * K, ["급속"] = -3 * K, ["기절"] = 0, ["고통 각인"] = 2 * K,
        };
        static readonly Dictionary<string, double> ALLY_ST = new()
        {
            ["사기"] = 7 * K, ["불굴"] = 8 * K, ["결의"] = 5 * K, ["결정화"] = 4 * K, ["반격"] = 6 * K, ["취약"] = -3 * K, ["약화"] = -3 * K, ["고통"] = -2 * K, ["균열"] = -2 * K, ["손상"] = -1 * K,
            ["그을림"] = -1 * K, ["충격"] = -2 * K, ["충격파"] = -2 * K, ["잔광"] = 5 * K, ["피해 감소"] = 3 * K, ["면역"] = 3 * K, ["실드 유지"] = 2 * K, ["저장"] = 4 * K, ["협공"] = 9 * K, ["고동"] = 9 * K,
            ["회피"] = 6 * K, ["공명"] = 5 * K, ["탄성"] = 3 * K, ["칼날 벼리기"] = 4 * K, ["빙벽"] = 8 * K, ["형상 강화"] = 2 * K, ["행동 둔화"] = 3 * K, ["근면"] = 4 * K, ["계몽"] = 3 * K,
            ["집중"] = 3 * K, ["다음 턴 드로우"] = 4 * K, ["초재생"] = 6 * K, ["절대 무적"] = 20 * K, ["실드 보존"] = 3 * K, ["끈기"] = 8 * K,
            ["응징"] = -3 * K, ["포자증식"] = -1 * K, ["정신 붕괴"] = -4 * K, ["미끄러움"] = -3 * K, ["고통 각인"] = -2 * K,
        };
        // 판 점수의 상태 표를 배열로(2026-10-09) — 차례는 표 그대로(더하는 차례가 같아야 실수 합이 같다). 사도 층(HERO_ST) 여부도 미리 갈라 둔다.
        static readonly KeyValuePair<string, double>[] FOE_ARR = FOE_ST.ToArray();
        static readonly KeyValuePair<string, double>[] ALLY_POOL = ALLY_ST.Where(kv => !R.HERO_ST.Contains(kv.Key)).ToArray();
        static readonly KeyValuePair<string, double>[] ALLY_HERO = ALLY_ST.Where(kv => R.HERO_ST.Contains(kv.Key)).ToArray();
        static double Useful(string k, int n) => !R.IsIntensity(k) ? n : R.SV(k) > 0 ? R.StackEff(k, n) / R.SV(k) : n;
        const double TOUGH_W = 2 * K, BROKEN_W = 4 * K;

        /// <summary>판의 점수 — HP 단위, 높을수록 좋다.</summary>
        public double Score(Battle s, bool withPot)
        {
            if (s.Over == "lose") return -1e6;
            double v = 0;
            if (s.Over == "win") v += 5000;
            foreach (var e in s.Enemies)
            {
                if (e.Dead) { v += 12 * K + 3 * ThreatOf(e.Key); continue; }
                v -= e.Hp;
                // 겹이 0 이면 Useful 이 0 이라 더할 몫이 없다(±0 더하기) — 그 상태는 건너뛴다. 적은 제 몸의 상태(Battle.St 와 같다)
                if (e.Status.Count > 0)
                    foreach (var kv in FOE_ARR)
                    {
                        int n = Math.Max(0, (e.Status.TryGetValue(kv.Key, out var sn) ? sn : 0) - (kv.Key == "약화" ? 1 : 0));
                        if (n != 0) v += kv.Value * Useful(kv.Key, n);
                    }
                foreach (var kv in e.Status) if (!FOE_ST.ContainsKey(kv.Key) && !R.ALL_ST.Contains(kv.Key) && s.CounterOf(e, kv.Key) == null)
                    v += s.Kw.TryGetValue(kv.Key, out var ek) && ReservePayoff(s, ek) is double rp && rp > 0 && kv.Value > 0 ? rp + 0.3 * K * kv.Value : 1.2 * K * kv.Value;   // 키워드 표식(적의 쌓이는 수치는 빼고) · 예약 표식은 터질 몫(2단계)
                if (e.ToughMax > 0) v += TOUGH_W * (e.ToughMax - e.Tough) + (e.Broken ? BROKEN_W : 0);
                foreach (var m in e.Mods) { double w = m.Stat == "taken" ? 10 * K : m.Stat == "dealt" || m.Stat == "atk" ? -10 * K : 0; v += w * m.V * Math.Min(m.Left, 3); }
            }
            var P = s.Pool;
            (int hp, int shield, double later, double misc)? incS = null;   // 아래 Potential 이 같은 판의 Incoming 을 다시 셈하지 않게(판을 바꾸지 않는 셈이다 — 2026-10-09)
            if (s.Over != "win")
            {
                var inc = Incoming(s); incS = inc;
                if (inc.hp <= 0) v -= 2000 + P.MaxHp * 0.5;
                else
                {
                    v += 1.3 * inc.hp;
                    if (inc.hp < P.MaxHp * 0.25) v -= (P.MaxHp * 0.25 - inc.hp) * 0.8;
                    v += 0.4 * Math.Max(0, inc.shield);
                }
                v -= 0.4 * inc.later + inc.misc;
            }
            else v += 1.3 * P.Hp;
            // 파티 층 — 파티 몸(Pool)의 상태. 사도 층(HERO_ST)은 그 사도의 상태(Battle.St 와 같다). 겹 0 은 건너뛴다(위와 같은 까닭).
            if (!P.Dead && P.Status.Count > 0)
                foreach (var kv in ALLY_POOL) { int n = P.Status.TryGetValue(kv.Key, out var sn) ? sn : 0; if (n != 0) v += kv.Value * Useful(kv.Key, n); }
            foreach (var u in s.AliveParty())
            {
                if (u.Status.Count > 0)
                    foreach (var kv in ALLY_HERO) { int n = u.Status.TryGetValue(kv.Key, out var sn) ? sn : 0; if (n != 0) v += kv.Value * Useful(kv.Key, n) * (u.Role == "딜러" ? 1.4 : 0.8); }
                double rw = u.Role == "딜러" ? 1.4 : 0.8;
                foreach (var m in u.Mods)
                {
                    double eff = Math.Min(m.Left, 3);
                    // 봇 손질(2026-10-08): 주는 피해 · 공격력 증가의 이번 턴 몫은 그 사도가 아직 칠 카드가 있을 때만(턴 끝에 남은 1턴짜리 버프를 한 턴 값으로 치던 것)
                    if ((m.Stat == "dealt" || m.Stat == "atk") && m.V > 0 && !m.Run && !CanStillHit(s, u)) eff -= 1;
                    v += (MOD_W.TryGetValue(m.Stat, out var w) ? w : 0) * rw * m.V * eff;
                }
            }
            foreach (var kv in s.Stacks)
                foreach (var st in kv.Value)
                    if (st.Value > 0) v += StackWorth(s, st.Key, st.Value, kv.Key);
            v += 0.06 * K * s.Gauge;
            // 켜진 강화 — 남은 전투에 돌 몫(카드 한 장 값과 같은 눈금 · 겹마다)
            foreach (var pw in s.Powers)
            {
                var o = s.HeroUnit(pw.Hero);
                if (o == null) continue;
                double pv = 0; foreach (var r in pw.Rules) pv += CardValue.RuleWorth(r);
                v += 0.8 * 1.2 * o.Atk * pv * Math.Max(1, pw.N);
            }
            v += ReserveWorth(s) + RunWorth(s) + EmpowerWorth(s) + HoldWorth(s) - ClogCost(s) + RecordWorth(s) + KeepWorth(s) + StanceWorth(s);
            foreach (var id in s.Hand)
            {
                var c = s.CardOf(id);
                if (c == null || !c.Fx.Any(f => f.K == FxK.When && f.On == "handEnd")) continue;
                if (c.IsStatus || c.IsCurse) { v -= 6 * K; continue; }
                var o = c.Hero != null ? s.HeroUnit(c.Hero) : null;
                double kv = KeepShieldWorth(c.Fx, incS);   // 쥐고 넘기면 실드 유지 · 보존 — 적 차례 뒤 남을 실드로(엘다인 4차 ▶E87 · 이드)
                v += kv >= 0 ? kv : 0.8 * 1.2 * (o != null ? o.Atk : 12 * K) * Math.Max(0, CardValue.PartValue(c.Fx, "handEnd"));
            }
            // ② 턴을 넘기면 돌 패시브 — 조건이 지금 판에 걸린 것(AP가 남았으면 · 이번 턴 카드를 안 냈으면 · 손에 묵힌 카드 …)은 그 몫을 센다.
            //    그래서 「AP 를 남기고 끝내기」 · 「보존 카드를 쥐고 넘기기」 가 후보가 된다
            if (s.Over == null) v += TurnEndBonus(s);
            v -= 5 * K * s.ApJam;
            if (withPot && s.Over != "win") v += 0.6 * Potential(s, incS) + SeqBonus(s);
            return v;
        }

        // ── 1단계(2026-10-08) — 걸어 둔 예약 · 판 단위 값(성장) · 다음 카드 강화 ─────────
        /// <summary>예약 할인 — 턴으로 거는 예약(다음 턴 이후) · 카드 장수로 거는 예약 · 함정(적이 쳐야 돈다).</summary>
        public const double LATER_W = 0.8, AFTER_W = 0.9, TRAP_W = 0.6;
        /// <summary>판 단위 값 — 남은 싸움 수(대충). 판에 남는 카드 값 1 · 판 성장 공격력 1 의 무게.</summary>
        public const double RUN_LEFT = 3, CARDST_RUN_W = 1.0 * K, CARDST_W = 1.5 * K, GROW_ATK_W = 5 * K, GROW_DEF_W = 3 * K, GROW_CRIT_W = 2 * K;

        /// <summary>걸어 둔 예약(later · afterCards)과 적에게 깐 함정 — 터질 효과의 값(카드 한 장 눈금)을 할인해 판 점수에. 예약 카드를 내는 수 · 일찍 캐기 판단이 이것을 본다.</summary>
        public double ReserveWorth(Battle s)
        {
            double v = 0;
            foreach (var r in s.Later)
            {
                var o = s.HeroUnit(r.Owner);
                if (r.Owner != null && (o == null || o.Dead)) continue;
                v += (r.Cards > 0 ? AFTER_W : LATER_W) * 1.2 * (o != null ? o.Atk : 12 * K) * Math.Max(0, CardValue.ValueOf(r.Fx));
            }
            foreach (var e in s.Enemies)
            {
                if (e.Dead) continue;
                foreach (var r in e.Traps) { var o = s.HeroUnit(r.Owner); v += TRAP_W * 1.2 * (o != null ? o.Atk : 12 * K) * Math.Max(0, CardValue.ValueOf(r.Fx)); }
            }
            return v;
        }

        /// <summary>판 단위 값(성장형 근사) — 카드에 붙은 데이터 값(비용 · 카드 상태 빼고, 이 전투만이면 덜) · 이 전투에서 생긴 판 성장(공격력 · 방어력 · 치명).</summary>
        public double RunWorth(Battle s)
        {
            double v = 0;
            foreach (var kv in s.CardSt)
                foreach (var st in kv.Value)
                {
                    if (st.Value <= 0 || st.Key == "비용" || R.IsCardSt(st.Key)) continue;
                    v += st.Value * (s.BattleVals.Contains(st.Key) ? CARDST_W : CARDST_W + CARDST_RUN_W * RUN_LEFT);
                }
            foreach (var g in s.GrowthGain.Values) v += g.Atk * GROW_ATK_W + g.Def * GROW_DEF_W + g.Crit * GROW_CRIT_W;
            return v;
        }

        // ── 2단계(2026-10-08 시범 16) — 고유 효과 겹을 실제 쓰임대로 ─────────
        /// <summary>
        /// 고유 효과 겹 n 의 판 점수. 옛 셈(겹마다 1.5K, 쥐면 세지는 카드가 손에 있으면 3.5K)에 더해:
        /// 예약(reserve + 「다 닳으면」 규칙)은 터질 몫(LATER_W 할인) — 일찍 캐기(ripen) · 재촉 판단이 이것과 견준다 ·
        /// 턴 끝에 사라지는 것(endClear · endDecay)은 이번 턴에 쓸 데가 없으면 0.3배 · 1개당 주는 피해/공격력(손맛)은 이번 턴 낼 수 있는 그 사도 공격 카드만큼 ·
        /// 소환물(summon 이 부르는 키워드)은 남은 따라 치기 몫.
        /// </summary>
        public double StackWorth(Battle s, string id, int n, string holder = null)
        {
            if (!s.Kw.TryGetValue(id, out var kw)) return 1.5 * K * n;
            bool partyBurn = kw.Def.Burn != null && kw.Carrier == "self";   // (2026-10-12 벨라 파티 촛불) 주인 말고 누가 내든 탐
            if (kw.Def.Burn != null && (holder != null || partyBurn))
            {   // (엘다인 4차 벨라 촛불) 붙은 동료가 낼 카드 수만큼 탈 실드 — 손의 그 동료 카드 + 다음 턴 몫(카드를 많이 내는 동료에게 켤수록 값)
                var giver = s.HeroUnit(kw.Owner); if (giver == null || giver.Dead) return 0;
                int mine = 0, deck = 0, all = 0;
                bool Burns(string hid) => partyBurn ? hid != null && hid != kw.Owner : hid == holder;
                foreach (var h in s.Hand) if (Burns(s.CardOf(h)?.Hero)) mine++;
                foreach (var h in s.Draw.Concat(s.Discard)) { all++; if (Burns(s.CardOf(h)?.Hero)) deck++; }
                double plays = mine + (all > 0 ? 5.0 * deck / all : 1.5);
                return Math.Min(n, plays) * (s.GuardAmount(giver, kw.Def.Burn.Shield, null) + (kw.Def.Burn.Heal > 0 ? s.HealAmount(giver, kw.Def.Burn.Heal, null) : 0)) * 0.5;
            }
            // 순환(단계 · 자세 · 위상)은 자원이 아니라 상태 — 겹 수로 값을 매기지 않는다(엘다인 E9: 「칼」(2) 이 「양산」(1) 의 두 배로 세여 자세를 못 바꾸던 것). 단계마다의 값은 그 단계에서 낸 카드가 판에 남긴다
            if (kw.Def.Wrap) return 0;
            bool grows = false;
            foreach (var h in s.Hand) if (CardValue.GrowsWith(s.CardOf(h)?.Fx, id)) { grows = true; break; }
            double v = (grows ? 3.5 : 1.5) * K * n;
            var d = kw.Def; var o = s.HeroUnit(kw.Owner); double atk = o != null ? o.Atk : 12 * K;
            double rp = ReservePayoff(s, kw);
            if (rp > 0) return rp + 0.3 * K * n;
            bool once = d.EndClear || d.EndDecay > 0;
            if (once && !grows) v *= 0.3;
            double perV = 0;
            foreach (var p in d.Per) if (p.Stat == "dealt" || p.Stat == "atk") perV += p.V;
            if (perV > 0 && o != null)
            {
                int atkCards = s.Hand.Count(h => { var c = s.CardOf(h); return c != null && c.Hero == o.Key && c.Type == "공격" && s.CanPlay(h) == null; });
                v += n * perV * atk * 0.83 * Math.Min(atkCards, 2) * (once ? 1 : 1.5);
            }
            double sr = SummonRatio(s, kw);
            if (sr > 0) v += Math.Min(n, Battle.SUMMON_CAP) * sr * atk * 0.83 * SUMMON_LEFT;
            return v;
        }
        /// <summary>소환물 한 겹이 남은 싸움에 따라 칠 대 수(대충 — 공격 카드 1.5장).</summary>
        public const double SUMMON_LEFT = 1.5;

        /// <summary>예약 키워드가 다 닳으면 터질 몫(LATER_W 할인 · 카드 한 장 눈금 × 공격력) — 예약이 아니면 0.</summary>
        double ReservePayoff(Battle s, KwRt kw)
        {
            if (!kw.Def.Reserve) return 0;
            var o = s.HeroUnit(kw.Owner); double atk = o != null ? o.Atk : 12 * K;
            double pv = 0;
            foreach (var r in kw.Def.Rules) if (r.When?.On == "stackGone" && r.When.Decay) pv += Math.Max(0, CardValue.ValueOf(r.Fx));
            return LATER_W * 1.2 * atk * pv;
        }

        /// <summary>그 키워드를 부르는 소환물 행동(summon)의 가장 큰 비율 — 주인 사도의 패시브 · 손패 카드에서.</summary>
        double SummonRatio(Battle s, KwRt kw)
        {
            double r = 0;
            if (s.Passives.TryGetValue(kw.Owner, out var rules))
                foreach (var rt in rules) foreach (var f in rt.R.Fx) if (f.K == FxK.Summon && f.Id == kw.Id) r = Math.Max(r, f.Ratio);
            foreach (var h in s.Hand) { var c = s.CardOf(h); if (c != null) foreach (var f in c.Fx) if (f.K == FxK.Summon && f.Id == kw.Id) r = Math.Max(r, f.Ratio * 0.5); }
            return r;
        }

        /// <summary>
        /// (엘다인 ▶E9) 쥐기 — 손에 쥔 무리(카드 id · 태그)가 손의 다른 카드를 세게 하면, 쥔 장수만큼 그 카드의 덤을 반쯤 센다.
        /// 「대물 저격총」(손의 「마탄」 1장당 +40%)이 손에 있으면 마탄을 버리거나 먼저 내기보다 쥐고 있다가 저격총에 싣는 쪽이 판 점수에 보인다.
        /// 내 턴에 그 카드를 낼 AP 가 있을 때만(턴 끝엔 손이 비어 몫이 사라진다).
        /// </summary>
        public double HoldWorth(Battle s)
        {
            if (s.Over != null || s.Ending || s.Hand.Count < 2) return 0;
            double v = 0;
            foreach (var h in s.Hand)
            {
                var c = s.CardOf(h);
                if (c?.Fx == null || c.Hero == null || s.CanPlay(h) != null) continue;
                for (int i = 0; i + 1 < c.Fx.Count; i++)
                {
                    var f = c.Fx[i];
                    if (f.K != FxK.PerTag || f.Id == null) continue;
                    var nx = c.Fx[i + 1];
                    if (nx.K != FxK.Dmg && nx.K != FxK.Extra) continue;
                    int held = s.Hand.Count(x => x != h && s.HasTagB(x, f.Id));
                    var o = s.HeroUnit(c.Hero); if (o == null) continue;
                    v += 0.5 * held * nx.Ratio * nx.HitsOr1 * o.Atk * 0.83;
                }
            }
            // (엘다인 3차 ▶E48) 손에서 큰 한 대를 막는 카드 — 적의 예고가 그 선을 넘으면 쥐고 있을 몫(막을 HP)
            int big = -1;
            foreach (var h in s.Hand)
            {
                var w = s.CardOf(h)?.Fx.FirstOrDefault(x => x.K == FxK.When && x.On == "handHurtBig");
                if (w == null) continue;
                if (big < 0) big = s.AliveEnemies().Select(e => s.IntentHit(e) ?? 0).DefaultIfEmpty(0).Max();
                int cap = (int)Math.Round(s.Pool.MaxHp * (w.Pct > 0 ? w.Pct : 0.15));
                if (big > cap) { v += 1.3 * (big - cap) * 0.8; break; }
            }
            return v;
        }

        // ── 엘다인 4차 태세 — 들기 · 유지 · 풀리지 않게 ─────────────────────────
        /// <summary>그 카드의 「변신 · 태세 중이면」 조건 자리(not 아닌 것 · 없으면 -1).</summary>
        static int FormCondAt(List<Fx> fx) { if (fx == null) return -1; for (int i = 0; i < fx.Count; i++) if (fx[i].K == FxK.IfForm && !fx[i].Not) return i; return -1; }
        static bool FormOn(Battle s, string hero, string id) => hero != null && s.Forms.TryGetValue(hero, out var fr) && (id == null || fr.Id == id);
        /// <summary>태세가 남아 있을 몫 — 뽑을 · 버린 더미에 있는 그 사도의 태세 덤 카드(곧 손에 올 것)의 덤 몫 × STANCE_LATER. 그래서 들기 · 유지가 값이 되고, 태세를 푸는 수(다른 아군 공격 …)는 이 몫을 잃는다.</summary>
        public const double STANCE_LATER = 0.25;
        public double StanceWorth(Battle s)
        {
            if (s.Forms.Count == 0 || s.Over != null) return 0;
            double v = 0;
            foreach (var kv in s.Forms)
            {
                var o = s.HeroUnit(kv.Key); if (o == null || o.Dead) continue;
                foreach (var id in s.Draw.Concat(s.Discard))
                {
                    var c = s.CardOf(id);
                    if (c == null || c.Hero != kv.Key) continue;
                    int fi = FormCondAt(c.Fx);
                    if (fi < 0 || (c.Fx[fi].Id != null && c.Fx[fi].Id != kv.Value.Id)) continue;
                    var (b0, full) = HoldTiming.Split(c.Fx, new HoldTiming.T("form", 0, 1, fi));
                    v += STANCE_LATER * 1.2 * o.Atk * Math.Max(0, CardValue.ValueOf(full, c.Tags) - CardValue.ValueOf(b0, c.Tags));
                }
            }
            return v;
        }

        // ── 엘다인 4차 ▶E87 — 때 보고 쥐기 ────────────────────────────────
        /// <summary>쥐고 넘긴 보존 카드의 다음 턴 값 할인(때가 맞을 확률은 HoldTiming.Next 가 따로 곱한다).</summary>
        public const double HOLD_W = 0.95;
        /// <summary>보스 · 엘리트전(싸움이 길다)에서 쥐기 값에 곱하는 덤 — 다음 턴이 올 확률이 높고 큰 한 방의 몫이 크다(사용자 방향 2026-10-11).</summary>
        public const double HOLD_LONG = 1.25;
        /// <summary>
        /// 때를 보는 보존 카드 — 지금이 그 카드의 때가 아니고(HoldTiming.Now) 다음 턴이 더 나으면(Next), 「다음 턴에 낼 때 값」 =
        /// HOLD_W × (덤 뺀 값 + Next × 덤 몫) × 1.2 × 공격력 (+ Next × 처형할 적 HP) 를 판 점수에. 그 카드를 지금 내면 이 몫이 사라지고 둬 본 판의 값이 남는다 —
        /// 그래서 「지금 내기(이번 턴 값) ↔ 쥐기(다음 턴 값)」 를 견준다. 때 조각이 없는 보존 카드 · 지금이 때인 카드는 0(옛 셈 그대로 낸다).
        /// </summary>
        public double KeepWorth(Battle s)
        {
            if (s.Over != null || s.Ending || s.FinaleLock) return 0;
            double v = 0, lw = s.LongFight ? HOLD_LONG : 1;
            foreach (var h in s.Hand.Distinct())
            {
                if (!s.HasTagB(h, Tag.Keep) || s.HasTagB(h, Tag.Evaporate)) continue;
                var t = HoldTiming.Of(s, h);
                if (!t.Hold) continue;
                var c = s.CardOf(h); var o = s.HeroUnit(c.Hero);
                if (o == null || o.Dead) continue;
                var (bas, full) = HoldTiming.Split(c.Fx, t);
                double b = Math.Max(0, CardValue.ValueOf(bas, c.Tags)), f = Math.Max(b, CardValue.ValueOf(full, c.Tags));
                v += Math.Max(0, lw * HOLD_W * ((b + t.Next * (f - b)) * 1.2 * o.Atk + t.Next * t.Gain) * (t.Next - t.Now) / Math.Max(0.01, t.Next));   // Gain 이 음수(남길 AP)면 덜 쥔다
            }
            return v;
        }
        /// <summary>쥐고 넘기면 실드 유지 · 실드 보존(handEnd status) — 적 차례 뒤 남을 실드(inc.shield)의 그 몫(HP 눈금 0.8). 그런 마디가 없으면 -1.</summary>
        static double KeepShieldWorth(List<Fx> fx, (int hp, int shield, double later, double misc)? inc)
        {
            if (fx == null || inc == null) return -1;
            bool on = false; double k = 0;
            foreach (var f in fx)
            {
                if (f.K == FxK.When) { on = f.On == "handEnd"; continue; }
                if (!on || f.K != FxK.Status) continue;
                if (f.Id == "실드 유지") k = Math.Max(k, R.SV("실드 유지"));
                else if (f.Id == "실드 보존") k = 1;
            }
            return k > 0 ? 0.8 * k * Math.Max(0, inc.Value.shield) : -1;
        }

        /// <summary>(엘다인 3차) 받아 적은 쪽 — 낭독할 몫(카드 한 장 값 × 반쯤 · 그 주인 공격력 눈금).</summary>
        public double RecordWorth(Battle s)
        {
            double v = 0;
            foreach (var kv in s.Records)
                foreach (var id in kv.Value)
                {
                    var c = s.CardOf(id); var o = c?.Hero != null ? s.HeroUnit(c.Hero) : null;
                    if (c == null || o == null || o.Dead) continue;
                    v += 0.5 * 1.2 * o.Atk * Math.Max(0, CardValue.ValueOf(c.Fx));
                }
            return v;
        }

        /// <summary>(엘다인 E9 · 아야) 망각 카드 한 장이 다음 드로우 칸을 막는 값 — 카드 반 장쯤(HP 단위).</summary>
        public const double CLOG_W = 6 * K;
        /// <summary>뽑을 더미 맨 위(다음 턴 드로우 칸)의 망각 카드 수.</summary>
        static int ClogTop(Battle s)
        {
            int n = 0;
            for (int i = s.Draw.Count - 1, k = 0; i >= 0 && k < R.DRAW_PER_TURN; i--, k++) if (s.HasTagB(s.Draw[i], Tag.Forget)) n++;
            return n;
        }
        /// <summary>망각 카드가 손 · 뽑을 더미 위에 몇 장인가(두 수 앞 보기의 문턱).</summary>
        static int ForgetClog(Battle s) => ClogTop(s) + s.Hand.Count(h => s.HasTagB(h, Tag.Forget));
        /// <summary>
        /// 막힘 — 망각 카드(소멸하면 뽑을 더미 맨 위로)가 다음 드로우 칸을 채우면 다른 사도 카드가 안 들어온다. 첫 장은 쓸 만하다고 보고 둘째부터 한 장마다 CLOG_W.
        /// 「망각 없이 사라짐」(noForget — 아야 칼 자세)으로 치우거나 나비를 덜 만드는 쪽이 판 점수에 보인다.
        /// </summary>
        public double ClogCost(Battle s) => s.Over != null ? 0 : CLOG_W * Math.Max(0, ClogTop(s) - 1);

        /// <summary>걸려 있는 다음 카드 강화 — 카드 한 장(값 1) × 강화 비율.</summary>
        public double EmpowerWorth(Battle s)
        {
            double v = 0;
            foreach (var kv in s.Counts)
            {
                if (kv.Key.StartsWith("empowerO|", StringComparison.Ordinal) && kv.Value > 0)
                {   // 다른 아군의 다음 카드 강화(엘다인 4차 요청) — 다른 아군 가운데 가장 센 공격력 눈금
                    var giver = kv.Key.Substring(9);
                    double oa = s.AliveParty().Where(u => u.Key != giver).Select(u => (double)u.Atk).DefaultIfEmpty(12 * K).Max();
                    v += 0.8 * 1.2 * oa * kv.Value / 100.0;
                    continue;
                }
                if (!kv.Key.StartsWith("empower|", StringComparison.Ordinal) || kv.Value <= 0) continue;
                var o = s.HeroUnit(kv.Key.Substring(8));
                double atk = o != null ? o.Atk : s.AliveParty().Select(u => (double)u.Atk).DefaultIfEmpty(12 * K).Max();
                v += 0.8 * 1.2 * atk * kv.Value / 100.0;
            }
            return v;
        }

        /// <summary>턴 끝 패시브 가운데 조건이 붙은 것(조건이 지금 서 있으면 그 효과의 값). 조건 없는 것은 어느 수든 같아 셀 필요가 없다.</summary>
        double TurnEndBonus(Battle s)
        {
            double v = 0;
            foreach (var u in s.AliveParty())
            {
                if (!s.Passives.TryGetValue(u.Key, out var rules)) continue;
                int kept = -1;
                foreach (var rt in rules)
                {
                    // 아껴 두기 계기(keepAp) — AP 를 남겼거나 보존 카드를 쥐고 있으면 턴을 넘길 때 돈다(1단계: 보존도)
                    if (rt.R.When.On == "keepAp")
                    {
                        if (kept < 0) kept = s.Hand.Count(id => s.HasTagB(id, Tag.Keep) && !s.HasTagB(id, Tag.Evaporate));
                        var w = rt.R.When; int n = Math.Max(1, w.N);
                        bool on = w.Kind == "keep" ? kept >= n : w.Kind == "ap" ? s.Ap >= n : s.Ap >= n || kept > 0;
                        // 3단계(118명): 쌓기에 ofEvent 가 있으면 일의 값(남긴 AP + 쥔 보존 장수)만큼 센다 — 옛 셈은 v 만 봐 「아껴 둔 몫」 을 작게 쳤다
                        int ev = (w.Kind == "keep" ? 0 : Math.Max(0, s.Ap)) + (w.Kind == "ap" ? 0 : kept);
                        if (on && s.CondsHold(u, rt)) v += 0.8 * 1.2 * u.Atk * Math.Max(0, CardValue.ValueOf(rt.R.Fx)) + 1.5 * K * rt.R.Fx.Where(f => f.K == FxK.Stack).Sum(f => f.OfEvent > 0 ? Math.Floor(ev * f.OfEvent) : f.V);
                        continue;
                    }
                    if (rt.R.When.On != "turnEnd" || rt.R.Conds.Count == 0) continue;
                    if (!s.CondsHold(u, rt)) continue;
                    v += 0.8 * 1.2 * u.Atk * Math.Max(0, CardValue.ValueOf(rt.R.Fx)) + 1.5 * K * rt.R.Fx.Where(f => f.K == FxK.Stack).Sum(f => f.V);
                }
            }
            return v;
        }

        public Battle CloneFor(Battle s) { var c = s.Clone(new Rng(7), bot: true); c.Preview = true; return c; }

        /// <summary>신탁 고르기 — 고른 뒤 그 카드의 코스트당 값어치가 가장 큰 것.</summary>
        public int EpiChoice(Battle s, string id)
        {
            var g = s.GlowOf(id);
            if (g == null || g.Kind != "card") return 0;
            int best = 0; double bv = -1e9;
            for (int i = 0; i < g.Picks.Count; i++)
            {
                var c = data.View(id, g.Picks[i].N);
                int cost = c.X ? 3 : c.Cost;
                double v = CardValue.ValueOf(c) / (0.5 + cost) + (g.Picks[i].Shin != null ? 0.25 : 0) + (cost == 0 ? 0.2 : 0);
                if (v > bv) { bv = v; best = i; }
            }
            return best;
        }

        /// <summary>둘 수 있는 수 하나 — 카드(손 번호 · 대상 · 아군 · 버릴 카드) 또는 고학년.</summary>
        public sealed class Move
        {
            public bool Ult;
            public int I; public string Id; public int T; public int? Ally; public List<string> Discard; public string Hero;
            /// <summary>두 갈래 카드 — 고른 갈래.</summary>
            public int? Choice;
            /// <summary>소모량을 고르는 카드 — 고른 수(1 · 절반 · 전부를 다 둬 본다).</summary>
            public int? Spend;
            /// <summary>(엘다인 3차 ▶E74) 단계를 고르는 쌓기 — 고른 단계(단계마다 둬 본다).</summary>
            public int? Stage;
        }

        public List<Move> Moves(Battle s)
        {
            var outs = new List<Move>();
            var foes = s.AliveEnemies().Select(e => e.Idx).ToList();
            var allies = s.AliveParty().Select(u => u.Idx).ToList();   // 아군 1명 — 사도마다 둬 본다(증감 · 사도 층 상태가 사도마다라)
            var seen = new HashSet<string>();
            for (int i = 0; i < s.Hand.Count; i++)
            {
                var id = s.Hand[i];
                if (seen.Contains(id) || s.CanPlay(id, false, i) != null) continue;
                seen.Add(id);
                var c = s.CardOf(id);
                if (s.Pool.Hp < s.Pool.MaxHp * 0.4 && c.Fx.Any(f => f.K == FxK.PayHp || f.K == FxK.PayHpPct)) continue;   // 대가형 — 낮은 HP 에서는 치르지 않는다
                bool single = c.Fx.Any(f => (f.Target ?? (f.K == FxK.Dmg ? "oneEnemy" : null)) == "oneEnemy");
                var ts = c.Target == "적" ? (single ? foes : new List<int> { foes.FirstOrDefault() }) : c.Target == "아군" ? allies : new List<int> { 0 };
                bool allyToo = c.Target == "적" && c.Fx.Any(f => f.Target == "oneAlly") && allies.Count > 1;
                // 흠 세기 — 디버프 가짓수로 세지는 카드는 가짓수가 가장 많은 적(들)에게만 겨눈다
                if (c.Target == "적" && c.Fx.Any(f => f.K == FxK.PerDebuff || f.K == FxK.IfDebuffs) && ts.Count > 1)
                {
                    int top = ts.Max(t => s.DebuffKinds(s.Enemies[t]));
                    ts = ts.Where(t => s.DebuffKinds(s.Enemies[t]) == top).ToList();
                }
                List<string> discard = null;
                int need = s.DiscardChoice(i);
                if (need > 0)
                {
                    bool burning = c.Fx.Any(f => f.K == FxK.Burn) && !c.Fx.Any(f => f.K == FxK.Discard);
                    discard = s.Hand.Where((x, j) => j != i && s.DiscardFits(i, x)).OrderBy(x => DiscardWorth(s, x, burning)).Take(need).ToList();   // 거르개(「손의 「마탄」」)에 맞는 카드만
                }
                var picks = c.Choices != null && c.Choices.Count == 2 ? new int?[] { 1, 2 } : new int?[] { null };   // 두 갈래 — 둘 다 둬 본다
                var sp = s.SpendChoices(id);   // 소모량 고르기 — 1 · 절반 · 전부를 다 둬 본다(1단계)
                if (sp.Count == 0) sp = s.ShieldChoices(id);   // 파티 실드 고른 만큼(엘다인 ▶E1 · E9) — 절반 · 전부. 헐지 않는 쪽은 그 카드를 안 내는 수(멈춤)와 견준다
                var spends = sp.Count > 1 ? sp.Select(x => (int?)x).ToArray() : new int?[] { null };
                var stg = s.StageChoices(id);
                var stages = stg.Count > 1 ? stg.Select(x => (int?)x).ToArray() : new int?[] { null };   // 단계 고르기 — 단계마다 둬 본다
                foreach (var ch in picks)
                    foreach (var spn in spends)
                        foreach (var sg in stages)
                            foreach (var t in ts)
                            {
                                if (allyToo) foreach (var a in allies) outs.Add(new Move { I = i, Id = id, T = t, Ally = a, Discard = discard, Choice = ch, Spend = spn, Stage = sg });
                                else outs.Add(new Move { I = i, Id = id, T = t, Discard = discard, Choice = ch, Spend = spn, Stage = sg });
                            }
            }
            foreach (var u in s.Party)
            {
                if (s.CanUlt(u.Key) != null) continue;
                var fx = s.UltOf(u.Key).Fx;
                var ts = fx.Any(f => f.Target == "oneEnemy" || (f.K == FxK.Dmg && f.Target == null)) ? foes : new List<int> { foes.FirstOrDefault() };
                foreach (var t in ts) outs.Add(new Move { Ult = true, Hero = u.Key, T = t });
            }
            return outs;
        }

        /// <summary>버릴(태울) 후보의 값 — 낮을수록 먼저 버린다. 상태 카드 · 저주가 먼저, 안식(버려지면 도는) 카드는 버리기가 이득.</summary>
        /// <summary>고르는 버리기 · 소멸 카드를 낼 때 봇이 고르는 손패(낼 카드 제외) — 화면의 자동 전투도 이것을 쓴다.</summary>
        public List<string> PickDiscard(Battle s, int handIdx)
        {
            int need = s.DiscardChoice(handIdx);
            if (need <= 0) return null;
            var c = s.CardOf(s.Hand[handIdx]);
            bool burning = c.Fx.Any(f => f.K == FxK.Burn) && !c.Fx.Any(f => f.K == FxK.Discard);
            return s.Hand.Where((x, j) => j != handIdx && s.DiscardFits(handIdx, x)).OrderBy(x => DiscardWorth(s, x, burning)).Take(need).ToList();
        }

        double DiscardWorth(Battle s, string id, bool burning)
        {
            var c = s.CardOf(id);
            if (c == null) return 0;
            if (c.IsStatus || c.IsCurse) return -10;
            bool bullet = c.Tags.Any(t => Tag.Parse(t).id == Tag.Bullet);
            return CardVal(s, id) - (burning ? (bullet ? -2 : 0) : 2 * CardValue.PartValue(c.Fx, "discard") + (bullet ? 1 : 0));   // 탄환 — 버리면 쏘지만 턴 끝에도 쏜다(2026-10-13) · 태우면 그 한 발을 잃는다
        }

        public bool Apply(Battle s, Move m)
        {
            if (m.Ult) return s.UseUlt(m.Hero, m.T).Ok;
            var id = s.Hand[m.I];
            var g = s.GlowOf(id);
            if (g != null) s.ApplyEpiphany(id, g.Kind == "card" ? (EpiPick ?? EpiChoice)(s, id) : 0);
            return s.PlayCard(m.I, m.T, new PlayOpts { Ally = m.Ally, Discard = m.Discard?.ToList(), Choice = m.Choice, Spend = m.Spend, Stage = m.Stage }).Ok;
        }

        /// <summary>
        /// 소모량 고르기(spend pick) — 봇이 수를 고를 때와 같은 셈으로 소모량을 하나 돌려준다(시범 16 · 화면의 자동 전투용).
        /// 후보는 b.SpendChoices(카드) 의 1 · 절반 · 전부. 판을 복사해 하나씩 내 보고 판 점수(Score)가 가장 높은 수를 고른다.
        /// 고를 것이 없으면(소모량 고르기 카드가 아니거나 겹이 0 · 1) 그 하나(없으면 0)를 돌려준다. 손에 그 카드가 없으면 전부(가진 겹).
        /// </summary>
        public int PickSpend(Battle b, string cardInstanceId, int target)
        {
            var sp = b.SpendChoices(cardInstanceId);
            if (sp.Count == 0) sp = b.ShieldChoices(cardInstanceId);   // 파티 실드 고른 만큼(엘다인)
            if (sp.Count == 0) return 0;
            if (sp.Count == 1) return sp[0];
            int i = b.Hand.IndexOf(cardInstanceId);
            if (i < 0) return sp[sp.Count - 1];
            int best = sp[sp.Count - 1]; double bv = double.MinValue;
            foreach (var x in sp)
            {
                var sh = TryMove(b, new Move { I = i, Id = cardInstanceId, T = target, Spend = x });
                if (sh == null) continue;
                double v = Score(sh, true);
                if (v > bv) { bv = v; best = x; }
            }
            return best;
        }

        Battle TryMove(Battle s, Move m)
        {
            var sh = CloneFor(s);
            bool ok;
            try { ok = Apply(sh, m); } catch (Exception) { ok = false; }
            return ok ? sh : null;
        }

        /// <summary>이번 턴에 둘 수를 하나씩 고른다 — 턴 넘기기보다 나은 수가 없으면 멈춘다. 차례가 값을 바꾸는 손이면 두 수 앞까지(폭 2).</summary>
        public void SmartPlay(Battle s, int depth0 = 1, int width0 = 3, Action<string, double, double> trace = null)
        {
            int g = 0;
            while (s.Over == null && g++ < 60)
            {
                // 차례가 값을 바꾸는 손이면 두 수, 셋째 · 넷째 카드 덤(박자형)이 있으면 세 수 앞까지(폭 2) — 1단계
                bool deeper = depth0 < 3 && Rhythmic(s);
                bool deep = depth0 < 2 && (deeper || Orderly(s));
                int depth = deeper ? 3 : deep ? 2 : depth0, width = deep ? 2 : width0;
                double stop = Score(s, false);
                var ms = Moves(s);
                if (ms.Count == 0) break;
                var tried = new List<(Move m, Battle sh, double v, double? v2)>();
                foreach (var m in ms) { var sh = TryMove(s, m); if (sh != null) tried.Add((m, sh, Score(sh, true), null)); }
                if (tried.Count == 0) break;
                tried = tried.OrderByDescending(x => x.v).ToList();
                if (depth > 1)
                {
                    for (int i = 0; i < Math.Min(width, tried.Count); i++)
                    {
                        var x = tried[i];
                        if (x.sh.Over != null) continue;
                        tried[i] = (x.m, x.sh, x.v, Math.Max(x.v, Look(x.sh, depth - 1, width)));
                    }
                    tried = tried.OrderByDescending(x => x.v2 ?? x.v).ToList();
                }
                var top = tried[0];
                if ((top.v2 ?? top.v) <= stop + 0.01) break;
                trace?.Invoke(Describe(s, top.m), top.v2 ?? top.v, stop);
                if (!Apply(s, top.m)) break;
            }
        }

        /// <summary>그 판에서 d 수 안에 닿을 가장 좋은 점수(멈추기 포함). 다음 단계는 점수가 높은 width 개만 더 판다.</summary>
        double Look(Battle s, int d, int width)
        {
            double best = Score(s, false);
            var kids = new List<(Battle sh, double v)>();
            foreach (var m in Moves(s)) { var sh = TryMove(s, m); if (sh == null) continue; double v = Score(sh, true); kids.Add((sh, v)); if (v > best) best = v; }
            if (d > 1)
                foreach (var k in kids.Where(k => k.sh.Over == null).OrderByDescending(k => k.v).Take(width))
                    best = Math.Max(best, Look(k.sh, d - 1, width));
            return best;
        }

        public string Describe(Battle s, Move m)
        {
            string Foe(int i) => s.Enemies.FirstOrDefault(x => x.Idx == i)?.Name ?? "-";
            if (m.Ult) return $"고학년 {s.HeroUnit(m.Hero).Name} 「{s.UltOf(m.Hero).Name}」 → {Foe(m.T)}";
            var c = s.CardOf(m.Id);
            return $"「{c.Name}」({s.CostOf(m.Id)}AP · {data.Hero(c.Hero)?.Name ?? "교주"}){(c.Target == "적" ? " → " + Foe(m.T) : "")}{(m.Discard != null ? $" · 버림 {m.Discard.Count}" : "")}{(m.Spend != null ? $" · 소모 {m.Spend}" : "")}";
        }
    }
}
