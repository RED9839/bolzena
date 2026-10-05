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
            string key = id + ":" + fl;
            if (cvCache.TryGetValue(key, out var v)) return v;
            var c = s.CardOf(id);
            v = c == null ? 0 : c.IsCurse || c.IsStatus ? -1 : CardValue.ValueOf(c);
            cvCache[key] = v;
            return v;
        }

        double LiveVal(Battle s, string id, CardView c)
        {
            var oc = CardValue.OrderCond(c.Fx); bool rh = CardValue.RhythmUse(c.Fx);
            if (oc == null && c.Fx.Any(f => f.K == FxK.IfRepeat))
            {
                bool rep = s.PlayIds.Count > 0 && s.PlayIds[s.PlayIds.Count - 1] == GameData.BaseId(id);
                return rep ? CardValue.ValueOf(c, true) : CardVal(s, id);
            }
            if (oc == null && !rh) return CardVal(s, id);
            var last = s.PlayLog.Count > 0 ? s.PlayLog[s.PlayLog.Count - 1] : ((string hero, string type)?)null;
            bool on = oc != null && last != null && (oc.K == FxK.IfLink ? c.Hero != null && last.Value.hero == c.Hero : last.Value.type == oc.Type);
            if (!on && !rh) return CardVal(s, id);
            int? n = rh ? s.St(s.Pool, R.RHYTHM) : (int?)null;
            string key = id + ":" + (s.Flash.TryGetValue(id, out var f) ? f : 0) + ":live" + (on ? 1 : 0) + (rh ? ":r" + n : "");
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
                    bool can = s.Hand.Any(id => { var cv = s.CardOf(id); return cv != null && cv.Type == want && (w.Who == "any" || cv.Hero == u.Key) && s.CanPlay(id) == null; });
                    if (can) v += 0.6 * 1.2 * u.Atk * Math.Max(0, CardValue.ValueOf(rules[i].R.Fx)) * ((double)k / w.Seq.Count);
                }
            }
            return v;
        }

        bool Orderly(Battle s) => s.Hand.Any(id => { var fx = s.CardOf(id)?.Fx; return CardValue.OrderCond(fx) != null || CardValue.RhythmUse(fx) || (fx != null && fx.Any(f => f.K == FxK.IfRepeat || f.K == FxK.IfSpent || f.K == FxK.IfBalanced)); })
            || s.AliveParty().Any(u => s.Passives.TryGetValue(u.Key, out var rs) && rs.Any(r => r.R.When.Seq != null && r.R.When.Seq.Count > 0));

        readonly Dictionary<string, double> threatCache = new();
        static double HitOf(Intent it) => it == null ? 0 : it.T == "attack" || it.T == "back" ? it.V : it.T == "multi" ? it.V * Math.Max(1, it.N) : it.T == "attackAll" ? it.V * R.FOE_ALL_X : 0;
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
                if (ed > 0) d = Num.Round(d * (1 + R.NATURE_DMG)); else if (ed < 0) d = Num.Round(d * (1 - R.NATURE_DEF));
            }
            double m = (1 + s.StatMod(e, "dealt")) * (1 + TakenModOf(s));
            return Math.Max(0, Num.Round(d * Math.Max(0.1, m)));
        }

        static Unit PickT(List<Unit> live, bool fromBack)
        {
            if (live.Count == 0) return null;
            foreach (var r in fromBack ? R.ROWS.Reverse() : R.ROWS)
            {
                var inRow = live.Where(u => u.Row == r).ToList();
                if (inRow.Count > 0) return inRow.Aggregate((a, b) => fromBack ? (b.Idx < a.Idx ? b : a) : (b.Idx > a.Idx ? b : a));
            }
            return live[0];
        }

        /// <summary>이번 적의 차례에 파티가 잃을 HP + 다음 턴에 쏟을 힘 · 방해.</summary>
        public (int hp, int shield, double later, double misc) Incoming(Battle s)
        {
            var live = s.AliveParty().ToList();
            int hp = s.Pool.Hp, block = s.Pool.Block, shield = s.Pool.Shield;
            int vul = s.St(s.Pool, "취약");
            bool used = false;
            void Done() { if (used) vul = Math.Max(0, vul - 1); used = false; }
            void Hit(Unit e, Unit u, int v, bool pierce)
            {
                used = true;
                int d = HitTo(s, e, u, v, vul);
                if (!pierce) { int a = Math.Min(block, d); block -= a; d -= a; }
                int b = Math.Min(shield, d); shield -= b; d -= b;
                hp -= d;
            }
            double later = 0, misc = 0;
            foreach (var e in s.Enemies)
            {
                if (e.Dead || e.Intent == null || e.Sealed || e.RushedTurn) continue;
                var it = e.Intent;
                if (it.T == "attack" || it.T == "back") Hit(e, PickT(live, it.T == "back"), s.Dealt(e, it.V), it.T == "back");
                else if (it.T == "multi") { int d = s.Dealt(e, it.V); for (int k = 0; k < Math.Max(1, it.N); k++) Hit(e, PickT(live, false), d, false); }
                else if (it.T == "attackAll") Hit(e, PickT(live, false), s.Dealt(e, Num.Round(it.V * R.FOE_ALL_X)), false);
                else if (it.T == "charge") later += HitOf(it.Next) * e.Dmgx;
                else if (it.T == "jam") misc += 5 * K * Math.Max(1, it.V);
                else if (it.T == "buff") misc += 2 * K * Math.Max(1, it.V);
                else if (it.T == "debuff") misc += 3 * K * Math.Max(1, it.V);
                else if (it.T == "heal") misc += it.V * 0.8;
                else if (it.T == "addCard") misc += 3 * K * Math.Max(1, it.N);
                Done();
            }
            return (hp, shield, later, misc);
        }

        double Potential(Battle s)
        {
            if (s.FinaleLock) return 0;   // 종극 — 이번 턴은 끝났다
            int ap = s.Ap;
            var cards = new List<(int cost, double v)>();
            foreach (var id in s.Hand)
            {
                var c = s.CardOf(id); if (c == null) continue;
                if (c.Hero != null && s.HeroUnit(c.Hero) == null) continue;
                if (s.IsFrozen(id) || s.CanPlay(id) != null && !(c.HasTag(Tag.Link) || c.HasTag(Tag.Heaven))) continue;
                bool auto = c.HasTag(Tag.Link) || c.HasTag(Tag.Heaven);
                int cost = auto ? 0 : c.X ? Math.Max(1, ap) : s.CostOf(id);
                double v = LiveVal(s, id, c) * (auto ? 0.7 : 1);
                if (v <= 0) continue;
                var o = c.Hero != null ? s.HeroUnit(c.Hero) : null;
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
            ["응징"] = 4 * K, ["포자증식"] = 2 * K, ["죽음의 낙인"] = 4 * K, ["열정 약점"] = 2 * K, ["둔화"] = 3 * K, ["급속"] = -3 * K, ["기절"] = 0, ["고통 각인"] = 2 * K,
        };
        static readonly Dictionary<string, double> ALLY_ST = new()
        {
            ["사기"] = 7 * K, ["불굴"] = 8 * K, ["결의"] = 5 * K, ["결정화"] = 4 * K, ["반격"] = 6 * K, ["취약"] = -3 * K, ["약화"] = -3 * K, ["고통"] = -2 * K, ["균열"] = -2 * K, ["손상"] = -1 * K,
            ["그을림"] = -1 * K, ["충격"] = -2 * K, ["충격파"] = -2 * K, ["잔광"] = 5 * K, ["피해 감소"] = 3 * K, ["면역"] = 3 * K, ["실드 유지"] = 2 * K, ["저장"] = 4 * K, ["협공"] = 9 * K, ["고동"] = 9 * K,
            ["회피"] = 6 * K, ["공명"] = 5 * K, ["탄성"] = 3 * K, ["칼날 벼리기"] = 4 * K, ["빙벽"] = 8 * K, ["형상 강화"] = 2 * K, ["행동 둔화"] = 3 * K, ["근면"] = 4 * K, ["계몽"] = 3 * K,
            ["집중"] = 3 * K, ["다음 턴 드로우"] = 4 * K, ["초재생"] = 6 * K, ["절대 무적"] = 20 * K, ["실드 보존"] = 3 * K, ["끈기"] = 8 * K,
            ["응징"] = -3 * K, ["포자증식"] = -1 * K, ["정신 붕괴"] = -4 * K, ["미끄러움"] = -3 * K, ["고통 각인"] = -2 * K,
        };
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
                foreach (var kv in FOE_ST) v += kv.Value * Useful(kv.Key, Math.Max(0, s.St(e, kv.Key) - (kv.Key == "약화" ? 1 : 0)));
                foreach (var kv in e.Status) if (!FOE_ST.ContainsKey(kv.Key) && !R.ALL_ST.Contains(kv.Key) && s.CounterOf(e, kv.Key) == null) v += 1.2 * K * kv.Value;   // 키워드 표식(적의 쌓이는 수치는 빼고)
                if (e.ToughMax > 0) v += TOUGH_W * (e.ToughMax - e.Tough) + (e.Broken ? BROKEN_W : 0);
                foreach (var m in e.Mods) { double w = m.Stat == "taken" ? 10 * K : m.Stat == "dealt" || m.Stat == "atk" ? -10 * K : 0; v += w * m.V * Math.Min(m.Left, 3); }
            }
            var P = s.Pool;
            if (s.Over != "win")
            {
                var inc = Incoming(s);
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
            if (!P.Dead) foreach (var kv in ALLY_ST) if (!R.HERO_ST.Contains(kv.Key)) v += kv.Value * Useful(kv.Key, s.St(P, kv.Key));
            foreach (var u in s.AliveParty())
            {
                foreach (var kv in ALLY_ST) if (R.HERO_ST.Contains(kv.Key)) v += kv.Value * Useful(kv.Key, s.St(u, kv.Key)) * (u.Role == "딜러" ? 1.4 : 0.8);
                double rw = u.Role == "딜러" ? 1.4 : 0.8;
                foreach (var m in u.Mods) v += (MOD_W.TryGetValue(m.Stat, out var w) ? w : 0) * rw * m.V * Math.Min(m.Left, 3);
            }
            foreach (var kv in s.Stacks)
                foreach (var st in kv.Value)
                {
                    bool grows = s.Hand.Any(id => CardValue.GrowsWith(s.CardOf(id)?.Fx, st.Key));
                    v += (grows ? 3.5 : 1.5) * K * st.Value;
                }
            v += 0.06 * K * s.Gauge;
            foreach (var id in s.Hand)
            {
                var c = s.CardOf(id);
                if (c == null || !c.Fx.Any(f => f.K == FxK.When && f.On == "handEnd")) continue;
                if (c.IsStatus || c.IsCurse) { v -= 6 * K; continue; }
                var o = c.Hero != null ? s.HeroUnit(c.Hero) : null;
                v += 0.8 * 1.2 * (o != null ? o.Atk : 12 * K) * Math.Max(0, CardValue.PartValue(c.Fx, "handEnd"));
            }
            // ② 턴을 넘기면 돌 패시브 — 조건이 지금 판에 걸린 것(AP가 남았으면 · 이번 턴 카드를 안 냈으면 · 손에 묵힌 카드 …)은 그 몫을 센다.
            //    그래서 「AP 를 남기고 끝내기」 · 「보존 카드를 쥐고 넘기기」 가 후보가 된다
            if (s.Over == null) v += TurnEndBonus(s);
            v -= 5 * K * s.ApJam;
            if (withPot && s.Over != "win") v += 0.6 * Potential(s) + SeqBonus(s);
            return v;
        }

        /// <summary>턴 끝 패시브 가운데 조건이 붙은 것(조건이 지금 서 있으면 그 효과의 값). 조건 없는 것은 어느 수든 같아 셀 필요가 없다.</summary>
        double TurnEndBonus(Battle s)
        {
            double v = 0;
            foreach (var u in s.AliveParty())
            {
                if (!s.Passives.TryGetValue(u.Key, out var rules)) continue;
                foreach (var rt in rules)
                {
                    if (rt.R.When.On != "turnEnd" || rt.R.Conds.Count == 0) continue;
                    if (!s.CondsHold(u, rt)) continue;
                    v += 0.8 * 1.2 * u.Atk * Math.Max(0, CardValue.ValueOf(rt.R.Fx)) + 1.5 * K * rt.R.Fx.Where(f => f.K == FxK.Stack).Sum(f => f.V);
                }
            }
            return v;
        }

        public Battle CloneFor(Battle s) { var c = s.Clone(new Rng(7)); c.Preview = true; return c; }

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
                    discard = s.Hand.Where((_, j) => j != i).OrderBy(x => DiscardWorth(s, x, burning)).Take(need).ToList();
                }
                var picks = c.Choices != null && c.Choices.Count == 2 ? new int?[] { 1, 2 } : new int?[] { null };   // 두 갈래 — 둘 다 둬 본다
                foreach (var ch in picks)
                    foreach (var t in ts)
                    {
                        if (allyToo) foreach (var a in allies) outs.Add(new Move { I = i, Id = id, T = t, Ally = a, Discard = discard, Choice = ch });
                        else outs.Add(new Move { I = i, Id = id, T = t, Discard = discard, Choice = ch });
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
        double DiscardWorth(Battle s, string id, bool burning)
        {
            var c = s.CardOf(id);
            if (c == null) return 0;
            if (c.IsStatus || c.IsCurse) return -10;
            return CardVal(s, id) - (burning ? 0 : 2 * CardValue.PartValue(c.Fx, "discard"));
        }

        public bool Apply(Battle s, Move m)
        {
            if (m.Ult) return s.UseUlt(m.Hero, m.T).Ok;
            var id = s.Hand[m.I];
            var g = s.GlowOf(id);
            if (g != null) s.ApplyEpiphany(id, g.Kind == "card" ? EpiChoice(s, id) : 0);
            return s.PlayCard(m.I, m.T, new PlayOpts { Ally = m.Ally, Discard = m.Discard?.ToList(), Choice = m.Choice }).Ok;
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
                bool deep = depth0 < 2 && Orderly(s);
                int depth = deep ? 2 : depth0, width = deep ? 2 : width0;
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
                        double best = Score(x.sh, false);
                        foreach (var m2 in Moves(x.sh)) { var sh2 = TryMove(x.sh, m2); if (sh2 != null) best = Math.Max(best, Score(sh2, true)); }
                        tried[i] = (x.m, x.sh, x.v, Math.Max(x.v, best));
                    }
                    tried = tried.OrderByDescending(x => x.v2 ?? x.v).ToList();
                }
                var top = tried[0];
                if ((top.v2 ?? top.v) <= stop + 0.01) break;
                trace?.Invoke(Describe(s, top.m), top.v2 ?? top.v, stop);
                if (!Apply(s, top.m)) break;
            }
        }

        public string Describe(Battle s, Move m)
        {
            string Foe(int i) => s.Enemies.FirstOrDefault(x => x.Idx == i)?.Name ?? "-";
            if (m.Ult) return $"고학년 {s.HeroUnit(m.Hero).Name} 「{s.UltOf(m.Hero).Name}」 → {Foe(m.T)}";
            var c = s.CardOf(m.Id);
            return $"「{c.Name}」({s.CostOf(m.Id)}AP · {data.Hero(c.Hero)?.Name ?? "교주"}){(c.Target == "적" ? " → " + Foe(m.T) : "")}{(m.Discard != null ? $" · 버림 {m.Discard.Count}" : "")}";
        }
    }
}
