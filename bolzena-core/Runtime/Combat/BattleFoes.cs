using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    public sealed partial class Battle
    {
        public static readonly string[] FOE_HITS = { "attack", "back", "multi", "attackAll", "thorns" };
        static bool IsHit(string t) => t == "attack" || t == "back" || t == "multi" || t == "attackAll";

        // ── 적의 수 고르기 ─────────────────────────────────────────────
        /// <summary>적의 다음 수. fresh — 수를 흐트러뜨릴 때(모은 힘도 흩어진다).</summary>
        void RollIntent(Unit e, bool fresh)
        {
            var d = Data.Enemy(e.Key);
            if (!fresh && e.Intent != null && e.Intent.Next != null) { e.Intent = e.Intent.Next; e.IntentFromCharge = true; return; }
            e.IntentFromCharge = false;
            if (d.Phase != null && !e.Phased && e.Hp <= e.MaxHp * d.Phase.At)
            {
                e.Phased = true; e.Step = 0;
                Say($"{e.Name}: {d.Phase.Say}");   // 판이 바뀌어도 강인도는 차지 않는다(사용자 2026-10-06 — 격파 뒤 다음 턴 · 회복 스킬만)
            }
            if (d.Phase2 != null && e.Phased && !e.Phased2 && e.Hp <= e.MaxHp * d.Phase2.At)
            {
                e.Phased2 = true; e.Step = 0;
                Say($"{e.Name}: {d.Phase2.Say}");
            }
            if (e.ForceNext != null) { e.Intent = e.ForceNext; e.ForceNext = null; e.Hist.Add(e.Intent.T); e.Step++; return; }
            if (UltDue(e, d)) return;
            var list = e.Phased2 ? d.Phase2.Intents : e.Phased ? d.Phase.Intents : d.Intents;
            if (list == null || list.Count == 0) list = d.Intents;
            if (list == null || list.Count == 0) { e.Intent = null; return; }
            var ok = list.Where(x => IntentOk(e, x)).ToList();
            Intent it;
            if (!e.Phased && e.Step == 0 && d.Open != null) it = d.Open;
            else if (d.Pick == "shuffle")
            {
                // 같은 종류를 세 번 잇지 않는다 · 힘 모으기는 CHARGE_GAP 동안 다시 안 고른다 · 꽉 찬 소환은 안 고른다
                string a = e.Hist.Count >= 2 ? e.Hist[e.Hist.Count - 2] : null, b = e.Hist.Count >= 1 ? e.Hist[e.Hist.Count - 1] : null;
                var src = ok.Count > 0 ? ok : list;
                var pool = src.Where(x => !(a != null && a == b && x.T == a) && ShuffleOk(e, x)).ToList();
                if (pool.Count == 0) pool = src.Where(x => !(a != null && a == b && x.T == a)).ToList();
                if (pool.Count == 0) pool = src;
                double total = pool.Sum(x => ShuffleW(e, x));
                double r = Rng.Next() * total;
                it = pool.FirstOrDefault(x => (r -= ShuffleW(e, x)) < 0) ?? pool[pool.Count - 1];
            }
            else
            {
                int i = e.Step - (d.Open != null && !e.Phased ? 1 : 0);
                it = list[((i % list.Count) + list.Count) % list.Count];
                // 조건부 수 — 서지 않으면 다음 수로(한 바퀴까지)
                for (int k = 1; k < list.Count && !IntentOk(e, it); k++) { e.Step++; it = list[(((i + k) % list.Count) + list.Count) % list.Count]; }
            }
            e.Intent = it;
            e.Hist.Add(it.T);
            e.Step++;
            if (it.T == "charge" && it.Next != null && it.Next.T != "ult")
                Cue("foeChargeWarn", e, new Cue { Name = it.Next.Say, Say = it.Say, V = ChargeHit(e) ?? 0, T = it.Next.T });
        }

        // ── 무작위(shuffle) 고르기 ─────────────────────────────────────
        /// <summary>힘 모으기(charge)를 고른 뒤 이만큼의 고르기 동안은 다시 안 고른다(모은 수를 쏟는 턴은 고르기가 아니다 — 힘 모으기 → 쏟기 → 둘 → 다시 모으기 가 가장 빠르다).</summary>
        public const int CHARGE_GAP = 2;
        /// <summary>회복 수 — 적 모두가 이 비율 위면 무게를 HEAL_IDLE 배로(다친 동료가 없을 때 회복만 하다 턴을 버리지 않게).</summary>
        public const double HEAL_NEED = 0.75, HEAL_IDLE = 0.25;

        /// <summary>무작위로 고를 수 있나 — 힘 모으기 간격 · 소환 상한(같은 적 max · 자리 MAX_FOES).</summary>
        bool ShuffleOk(Unit e, Intent x)
        {
            if (x.T == "charge")
            {
                for (int k = 1; k <= CHARGE_GAP && k <= e.Hist.Count; k++) if (e.Hist[e.Hist.Count - k] == "charge") return false;
                return true;
            }
            if (x.T == "summon")
            {
                var alive = AliveEnemies();
                if (alive.Count >= MAX_FOES) return false;
                if (x.Max > 0 && alive.Count(u => u.Key == x.Id) >= x.Max) return false;
            }
            return true;
        }

        /// <summary>무작위 무게 — 데이터 w(기본 1)에 형편을 곱한다. 회복은 다친 동료가 있을 때, 강인도 회복 수는 강인도가 반 아래일 때 더 자주.</summary>
        double ShuffleW(Unit e, Intent x)
        {
            double w = x.WOr1;
            if (x.T == "heal" && !Enemies.Any(p => p.Dead && p.Feign) && AliveEnemies().All(p => p.Hp >= p.MaxHp * HEAL_NEED)) w *= HEAL_IDLE;
            if ((x.Tough > 0 || x.T == "brace") && e.ToughMax > 0)
            {
                if (e.Tough < e.ToughMax * 0.5) w *= 2;
                else if (e.Tough >= e.ToughMax) w *= 0.5;
            }
            return w;
        }

        /// <summary>힘 모으기(charge) 중인 적의 다음 턴 피해 — 머리 위 「공격 예고 중」 칸에 크게 보일 값(층 배율 · 약화 · 사기, multi 는 합). 고학년 예고도 같다. 예고가 아니거나 치지 않는 수면 null.</summary>
        public int? ChargeHit(Unit e)
        {
            var n = e?.Intent?.T == "charge" ? e.Intent.Next : null;
            if (n == null) return null;
            if (n.T == "ult") return UltHitOf(e, n);
            return IsHit(n.T) ? Dealt(e, AllX(n)) * (n.T == "multi" ? Math.Max(1, n.N) : 1) : (int?)null;
        }

        // ── 보스 클론의 고학년(BossUlt) ────────────────────────────────
        /// <summary>
        /// 보스 클론이 고학년을 예고할 턴이면 예고 수(charge → 다음 턴 ult)를 세우고 true. 평소 수의 차례(Step)는 그대로 둔다.
        /// 처음은 BossUlt.FIRST 턴 — 같은 싸움의 두 번째 클론부터는 한 턴씩 늦게. 그 뒤 EVERY 턴마다.
        /// </summary>
        bool UltDue(Unit e, EnemyDef d)
        {
            if (Turn < 1 || !BossUlt.Has(Data, d)) return false;
            if (e.UltAt == 0)
            {
                int order = Enemies.Count(x => x != e && x.Idx < e.Idx && x.Boss && Data.Enemy(x.Key)?.Clone != null);
                e.UltAt = Math.Max(Turn, BossUlt.FIRST) + order;
            }
            if (Turn < e.UltAt) return false;
            var p = BossUlt.PlanFor(Data, d, Floor);
            if (p == null) return false;
            e.UltAt = Turn + BossUlt.EVERY;
            e.Intent = p.Warn; e.IntentFromCharge = false;
            e.Hist.Add(p.Warn.T);
            int shown = UltHitOf(e, p.Use);
            Say($"{e.Name}: 고학년 예고 — 「{p.Name}」(다음 턴 피해 {shown} · 격파하면 끊김)");
            Cue("foeUltWarn", e, new Cue { Hero = p.Hero, Name = p.Name, V = shown, T = p.Use.Then.FirstOrDefault(x => IsHit(x.T))?.T });
            return true;
        }

        /// <summary>고학년 한 번의 피해 합(머리 위 숫자와 같은 셈 — 층 배율 · 약화 · 사기).</summary>
        int UltHitOf(Unit e, Intent use)
        {
            int s = 0;
            if (use?.Then != null) foreach (var x in use.Then) if (IsHit(x.T)) s += Dealt(e, AllX(x)) * (x.T == "multi" ? Math.Max(1, x.N) : 1);
            return s;
        }

        /// <summary>그 적이 예고 · 사용 중인 고학년의 피해(예고 턴에도 — 다음 턴 피해). 고학년이 아니면 null.</summary>
        public int? UltHit(Unit e) => e?.Intent == null ? null : e.Intent.T == "ult" ? UltHitOf(e, e.Intent) : BossUlt.IsUlt(e.Intent) ? UltHitOf(e, e.Intent.Next) : (int?)null;

        /// <summary>치는 수인가(고학년이면 그 안에 치는 수가 있나).</summary>
        static bool HitLike(Intent it) => it != null && (IsHit(it.T) || (it.T == "ult" && it.Then != null && it.Then.Any(x => IsHit(x.T))));

        /// <summary>고학년이 끊겼다(격파 · 기절) — 쪽지 foeUltCut.</summary>
        void UltCut(Unit e, string why)
        {
            if (!BossUlt.IsUlt(e?.Intent)) return;
            var use = e.Intent.T == "ult" ? e.Intent : e.Intent.Next;
            Say($"{e.Name}: {why} — 고학년 「{use.Say}」 이(가) 끊겼다");
            Cue("foeUltCut", e, new Cue { Hero = use.Id, Name = use.Say, Label = why, T = e.Intent.T });
            // 끊는 보상 — 모은 힘이 되튀어 무방비(취약). 면역이면 막힌다
            if (BossUlt.CUT_VULN > 0 && AddSt(e, "취약", BossUlt.CUT_VULN)) { StatusCue(e, "취약"); Say($"{e.Name}: 고학년이 끊겨 무방비 — 취약 +{BossUlt.CUT_VULN}"); }
        }

        bool IntentOk(Unit e, Intent it)
        {
            if (it?.If == null) return true;
            if (it.If.Allies != null && AliveEnemies().Count < it.If.Allies.Value) return false;
            if (it.If.PartyBlock != null && (Pool.Block + Pool.Shield > 0) != it.If.PartyBlock.Value) return false;
            if (it.If.SelfBlock != null && (e.Block + e.Shield > 0) != it.If.SelfBlock.Value) return false;
            if (it.If.Counter != null && St(e, it.If.Counter) < Math.Max(1, it.If.N)) return false;
            return true;
        }

        /// <summary>적을 세운다(소환) — 그 싸움의 체력 · 피해 배율 그대로. 살아 있는 적이 MAX_FOES 면 안 선다.</summary>
        public const int MAX_FOES = 5;
        Unit Summon(string id, Unit by = null, bool noTough = false)
        {
            var d = Data.Enemy(id);
            if (d == null || AliveEnemies().Count >= MAX_FOES) return null;
            int ehp = Num.Round(d.Hp * EnemyHpx);
            double tm = noTough ? 0 : ToughOf(d, EliteFight);   // 강인도 없는 소환물(수 summon 의 noTough)
            var u = new Unit { Side = Side.Enemy, Key = d.Id, Name = d.Name, Idx = Enemies.Count, Row = d.Row ?? "front", Nature = FoeNature(d), Boss = d.Boss, Dmgx = EnemyDmgx, Tough = tm, ToughMax = tm };
            u.MaxHp = ehp; u.Hp = ehp;
            u.Summoner = by?.Idx ?? -1;
            Enemies.Add(u);
            InitCounters(u);
            RollIntent(u, false);
            Cue("summon", u, new Cue { Name = d.Name });
            FoePassives("fightStart", u);
            return u;
        }

        /// <summary>전체 공격은 파티를 한 번 — 값 × FOE_ALL_X.</summary>
        static int AllX(Intent it) => it.T == "attackAll" ? Num.Round(it.V * R.FOE_ALL_X) : it.V;

        /// <summary>머리 위에 보여 줄 수치 — 층 배율 · 약화 · 사기가 들어간 값. 치는 수가 아니면 null.</summary>
        public int? IntentHit(Unit e) => e.Intent != null && IsHit(e.Intent.T) ? Dealt(e, AllX(e.Intent)) : e.Intent?.T == "ult" ? UltHitOf(e, e.Intent) : (int?)null;

        /// <summary>적의 치는 수의 값 — 층 배율만(상태는 빼고). 적 정보 창 · 가시가 쓴다.</summary>
        public static int FoeV(Unit e, Intent it)
        {
            if (it == null || Array.IndexOf(FOE_HITS, it.T) < 0) return it?.V ?? 0;
            double m = e?.Dmgx ?? 1; int v = AllX(it);
            return m != 1 && v > 0 ? Math.Max(1, Num.Round(v * m)) : v;
        }

        /// <summary>
        /// 즉시 행동 장수 — 지금 예고한 수마다. 수의 rush → 적의 rush → 값어치(센 수일수록 많이). 0 이면 당겨지지 않는다.
        /// 모으는 수(charge)와 모아서 쏟는 수는 0.
        /// </summary>
        public static int IntentRush(Intent it, EnemyDef d, bool fromCharge)
        {
            if (it == null) return 0;
            int Floor(int n) => n != 0 ? Math.Max(R.ENEMY_RUSH_MIN, n) : 0;
            if (it.Rush != null) return Floor(it.Rush.Value);
            if (d?.Rush != null) return Floor(d.Rush.Value);
            if (it.T == "charge" || fromCharge) return 0;
            double threat = it.T == "attack" || it.T == "back" ? it.V : it.T == "multi" ? it.V * Math.Max(1, it.N) : it.T == "attackAll" ? it.V * 2.5 : 0;
            if (threat <= 0) return R.ENEMY_RUSH_SMALL;
            int K = R.SCALE;
            return threat <= 6 * K ? 3 : threat <= 12 * K ? 4 : threat <= 20 * K ? 5 : 6;
        }
        public int RushOf(Unit e) => IntentRush(e.Intent, Data.Enemy(e.Key), e.IntentFromCharge);

        /// <summary>즉시 행동 — 예고된 뒤 파티가 카드를 그 장수만큼 내면 수를 당겨서 한다. 한 적은 내 턴에 한 번만.</summary>
        void RushEnemies()
        {
            foreach (var e in AliveEnemies())
            {
                int n = RushNeed(e);
                if (n == 0 || e.Sealed || e.Intent == null || e.RushedTurn) continue;
                e.RushCnt++;
                // 그을림 — 즉시 행동 셈이 1 오를 때마다 지속 피해 80%, 1 쓴다
                if (St(e, "그을림") > 0)
                {
                    int v = Math.Max(1, Num.Round(DotUnit(e, "그을림") * R.SV("그을림")));
                    AddSt(e, "그을림", -1);
                    Say($"{e.Name}: 그을림 — 지속 피해 {v}");
                    Hurt(e, v, new HurtOpts { Dot = true });
                    if (Over != null) return;
                    if (e.Dead) continue;
                }
                if (e.RushCnt < n) continue;
                e.RushCnt = 0;
                e.RushedTurn = true;
                Say($"{e.Name}: 카드 {n}장 — 즉시 행동!");
                Rushing = true;
                try { ActEnemy(e, e.Intent, false); } finally { Rushing = false; }
                RushedThisTurn = true;
                Emit("rush", new EmitInfo { Target = e });
                if (Over != null) return;
                // 즉시 행동한 적은 이번 판에 할 일을 다 했다 — 새 수는 다음 내 턴에, 적의 차례에는 쉰다
                if (!e.Dead) { FoePassives("rushed", e); e.Intent = null; }
                if (Over != null) return;
            }
        }

        /// <summary>
        /// 적의 방어는 적의 차례가 시작될 때 한꺼번에 사라진다(내 턴 동안은 남는다) — 가호(실드 보존)면 그대로 · 실드 유지면 반.
        /// 결정화의 고정 실드(Shield)는 남는다. 사라진 몫은 쪽지 unguard(V = 잃은 양 · To = 남은 방어+실드).
        /// 2026-10-05 — 예전엔 적마다 제가 움직이기 직전에 지워, 앞 적이 「적 전체 방어」 로 준 방어를 뒤 적이 곧바로 잃었다.
        /// </summary>
        void FoeGuardFade()
        {
            foreach (var e in AliveEnemies())
            {
                if (e.Block <= 0) continue;
                int was = e.Block;
                if (St(e, "실드 보존") > 0) { AddSt(e, "실드 보존", -1); continue; }
                if (St(e, "실드 유지") > 0) { e.Block = (int)Math.Floor(e.Block * R.SV("실드 유지")); AddSt(e, "실드 유지", -1); }
                else e.Block = 0;
                if (was > e.Block) LoseGuardCue(e, was - e.Block);
            }
        }

        /// <summary>방어 · 실드를 잃었다(사라짐 · 파괴) — 쪽지 unguard.</summary>
        void LoseGuardCue(Unit u, int lost) => Cue("unguard", u, new Cue { V = lost, To = u.Block + u.Shield });

        void EnemyPhase()
        {
            FoeGuardFade();
            foreach (var e in AliveEnemies())
            {
                if (e.Dead) continue;
                if (e.Sealed)
                {
                    if (!e.Broken) UltCut(e, "기절");   // 격파는 격파하는 자리에서 끊는다(ToughHit)
                    e.Sealed = false;
                    Say($"{e.Name}: 움직이지 못한다{(e.Intent?.Next != null ? " — 모은 힘이 흩어졌다" : "")}");
                    if (e.Intent?.Next != null) e.Intent = null;
                    continue;
                }
                if (e.RushedTurn) { Say($"{e.Name}: 즉시 행동을 했다 — 이번에는 쉰다"); continue; }
                ActEnemy(e, e.Intent, false);
                if (Over != null) return;
            }
        }

        // ── 적 패시브 ──────────────────────────────────────────────────
        static bool FoeOnce(string on) => on == "fightStart" || on == "lowHp";

        void FoePassives(string ev, Unit target, Unit from = null, double before = 0, double after = 0, string type = null, int nth = 0, bool same = false)
        {
            if (FoeQuiet > 0 && Turn <= FoeQuiet) return;
            foreach (var e in AliveEnemies())
            {
                var ps = FoePassList(Data.Enemy(e.Key)); if (ps.Count == 0) continue;
                foreach (var p in ps)
                {
                    if (p.On != ev) continue;
                    if ((ev == "hurt" || ev == "lowHp" || ev == "rushed" || ev == "debuffed" || ev == "broken" || ev == "recover" || ev == "guardBreak" || ev == "act") && target != e) continue;
                    if (ev == "fightStart" && target != null && target != e) continue;
                    if (ev == "allyDown" && target == e) continue;
                    if (ev == "allyDown" && p.Who != null && target?.Key != p.Who) continue;
                    if (ev == "lowHp" && !(before > p.At && after <= p.At)) continue;
                    if (ev == "card" && ((p.Type != null && !TypeOk(p.Type, type)) || (p.Every > 0 && nth % p.Every != 0) || (p.Same && !same))) continue;
                    if (p.Do != null && !IntentOk(e, p.Do)) continue;
                    if (p.Phase != null && !p.Phase.Contains(e.Phased2 ? 2 : e.Phased ? 1 : 0)) continue;
                    int lim = FoeOnce(ev) ? 1 : (p.Limit ?? 1);
                    e.PUsed.TryGetValue(p.Name, out int used);
                    if (lim > 0 && used >= lim) continue;
                    e.PUsed[p.Name] = used + 1;
                    Say($"{e.Name} · {p.Name}");
                    if (p.Do.T == "thorns") { if (from != null && !from.Dead && from.Side == Side.Party) Hurt(from, FoeV(e, p.Do)); }
                    else if (p.Do.T == "selfHeal") { int v = Math.Min(p.Do.V, e.MaxHp - e.Hp), h0 = e.Hp; e.Hp += v; HealCue(e, h0); }
                    else ActEnemy(e, p.Do, true, p.Do.Say ?? p.Name);
                    if (Over != null) return;
                }
            }
        }

        void ResetFoePassives()
        {
            foreach (var e in Enemies)
                foreach (var k in e.PUsed.Keys.ToList())
                {
                    var p = FoePassList(Data.Enemy(e.Key)).FirstOrDefault(x => x.Name == k);
                    if (p == null || !FoeOnce(p.On)) e.PUsed.Remove(k);
                }
        }

        // ── 적이 수를 한다 ─────────────────────────────────────────────
        /// <summary>적의 수 하나 = 한 번의 일(취약 · 반격 · 약화가 이 수에 한 번만 돈다).</summary>
        void ActEnemy(Unit e, Intent it, bool passive, string sayName = null)
        {
            if (it == null) return;
            if (!passive)
            {   // 적이 행동하기 직전 — 함정 · 「적이 행동하면(직전)」
                SpringTraps(e, it.T == "ult" ? it.Then?.FirstOrDefault(x => IsHit(x.T)) ?? it : it);
                if (Over != null || e.Dead) return;
                Emit("foeActBefore", new EmitInfo { Target = e, Type = HitLike(it) ? "공격" : "기타" });
                if (Over != null || e.Dead || e.Sealed) return;
            }
            int seq0 = ActSeq; ActSeq = ++SeqN;
            try { FoeAct(e, it, sayName ?? it.Say); } finally { ActSeq = seq0; }
            // 수를 한 뒤(패시브 말고 제 차례 · 즉시 행동) — 행동 카운트 상태(둔화 · 급속)는 사라지고 「행동하면」 이 돈다
            if (!passive && !e.Dead && Over == null)
            {
                e.ActedTurn = true;
                DelSt(e, "둔화"); DelSt(e, "급속");
                FoePassives("act", e);
                if (Over == null) Emit("foeAct", new EmitInfo { Target = e, Type = HitLike(it) ? "공격" : "기타" });
            }
        }

        int FoeStatus(Unit e, Unit t, string id, int n)
        {
            if (t == null || t.Dead || string.IsNullOrEmpty(id)) return 0;
            if (id == "고통" && St(t, "고통 각인") > 0) n += (int)R.SV("고통 각인");
            if (!AddSt(t, id, n, "enemy:" + e.Idx)) { Say($"파티: 면역 — {id} 를 막았다"); StatusCue(t, "면역!", true); return 0; }
            SetUnit(t, id, R.FOE_DOT * (e.Dmgx > 0 ? e.Dmgx : 1));
            StatusCue(t, id);
            return n;
        }

        int FoeBlock(Unit x, int v)
        {
            v = ShieldGain(x, v);
            x.Block += v; GainCue(x, "block", v);
            if (v > 0) { Punish(x); Emit("foeGuard", new EmitInfo { Target = x, V = v }); }
            return v;
        }

        /// <summary>강인도 회복 스킬 — 수 brace(버티기) · 수에 붙은 tough. 격파 중이면 안 찬다(격파는 다음 내 턴 시작에 다 찬다).</summary>
        void RegainTough(Unit x, double n)
        {
            if (x.ToughMax <= 0 || x.Broken || x.Dead || x.Tough >= x.ToughMax) return;
            double from = x.Tough;
            x.Tough = Math.Min(x.ToughMax, x.Tough + n);
            Cue("tough", x, new Cue { From = from, To = x.Tough, Up = true });
            Say($"{x.Name}: 강인도 +{x.Tough - from:0.##}");
        }

        void FoeAct(Unit e, Intent it, string say, bool quiet = false)
        {
            if (it.T == "ult") { FoeUlt(e, it); return; }
            if (!quiet) Cue("act", e, new Cue { Anim = IsHit(it.T) ? "attack" : "skill", Say = say, T = it.T, Rush = Rushing });
            if (FoeActKit(e, it, say)) { if (it.Tough > 0) RegainTough(e, it.Tough); return; }
            // 혼란 — 치는 수가 다른 적(무작위)을 친다
            if (e.Confused && IsHit(it.T))
            {
                e.Confused = false;
                var others = AliveEnemies().Where(x => x != e).ToList();
                if (others.Count > 0)
                {
                    var x = others[Rng.Int(others.Count)];
                    int hits = it.T == "multi" ? Math.Max(1, it.N) : 1, d = Dealt(e, AllX(it));
                    Say($"{e.Name}: 혼란 — {x.Name}을(를) 친다 ({d}×{hits})");
                    for (int k = 0; k < hits && !x.Dead; k++) Hurt(x, d, new HurtOpts { From = e });
                    return;
                }
            }
            int extraHits = IsHit(it.T) ? RareHits(e) : 0;
            switch (it.T)
            {
                case "attack":
                case "back":
                    {
                        var t = PickTarget(it.T == "back");
                        if (t == null) break;
                        int d = Dealt(e, it.V);
                        for (int k = 0; k <= extraHits && !t.Dead; k++) Hurt(t, d, new HurtOpts { From = e, Pierce = it.T == "back" });
                        Say($"{e.Name}: {say} → 파티 ({d}{(it.T == "back" ? " · 방어 관통" : "")})");
                        if (it.Id != null && !t.Dead) { int v = FoeStatus(e, t, it.Id, Math.Max(1, it.N)); Say($"파티: {it.Id} +{v}"); }
                        break;
                    }
                case "multi":
                    {
                        int d = Dealt(e, it.V);
                        for (int k = 0; k < Math.Max(1, it.N) + extraHits; k++)
                        {
                            var t = PickTarget(false); if (t == null) break;
                            Hurt(t, d, new HurtOpts { From = e });
                            if (it.Id != null) FoeStatus(e, t, it.Id, Math.Max(1, it.Per));
                        }
                        Say($"{e.Name}: {say} ({d}×{Math.Max(1, it.N)}){(it.Id != null ? " · " + it.Id : "")}");
                        break;
                    }
                case "charge": Say($"{e.Name}: {say} — 다음 턴 {it.Next?.Say}"); break;
                case "guard":
                    foreach (var x in AliveEnemies()) FoeBlock(x, it.V);
                    Say($"{e.Name}: {say} (적 전체 방어 +{it.V})");
                    break;
                case "heal":
                    {
                        var fe = Enemies.FirstOrDefault(p => p.Dead && p.Feign);
                        if (fe != null) { Revive(fe, it.V); Say($"{e.Name}: {say} — {fe.Name} 이(가) 일어선다"); break; }
                        var x = AliveEnemies().OrderBy(p => (double)p.Hp / p.MaxHp).FirstOrDefault();
                        if (x != null) { int v = Math.Min(it.V, x.MaxHp - x.Hp), h0 = x.Hp; x.Hp += v; HealCue(x, h0); Say($"{e.Name}: {say} ({x.Name} +{v})"); }
                        break;
                    }
                case "attackAll":
                    {
                        var t = PickTarget(false);
                        if (t == null) break;
                        int d = Dealt(e, AllX(it)); Hurt(t, d, new HurtOpts { From = e, All = true });
                        if (it.Id != null && !t.Dead) FoeStatus(e, t, it.Id, Math.Max(1, it.N));
                        Say($"{e.Name}: {say} → 파티 ({d}){(it.Id != null ? $" · {it.Id} {Math.Max(1, it.N)}" : "")}");
                        break;
                    }
                case "brace": Say($"{e.Name}: {say}"); RegainTough(e, it.V); break;   // 버티기 — 강인도 v 회복(격파 중이면 안 찬다)
                case "block": { int bv = FoeBlock(e, it.V); Say($"{e.Name}: {say} (방어 +{bv} → {e.Block + e.Shield})"); break; }
                case "buff":
                    foreach (var x in it.All ? AliveEnemies() : new List<Unit> { e }) { AddSt(x, it.Id, it.V, "enemy:" + e.Idx); StatusCue(x, it.Id, true); }
                    Say($"{e.Name}: {say} ({(it.All ? "적 전체 " : "")}{it.Id} +{it.V})");
                    break;
                case "jam":
                    if (AliveParty().Any(u => St(u, "구속") > 0)) { Say($"{e.Name}: {say} — 구속으로 AP 를 잃지 않는다"); break; }
                    ApJam += it.V;
                    Say($"{e.Name}: {say} (다음 턴 AP -{it.V})");
                    break;
                case "debuff":
                    {
                        int v = 0;
                        if (AliveParty().Any()) { if (R.IsHeroSt(it.Id)) foreach (var h in AliveParty().ToList()) v = FoeStatus(e, h, it.Id, it.V); else v = FoeStatus(e, PartyRep(), it.Id, it.V); }
                        Say($"{e.Name}: {say} (파티 {it.Id} +{v})");
                        break;
                    }
                case "summon":
                    {
                        int n = Math.Max(1, it.N), made = 0;
                        for (int k = 0; k < n; k++)
                        {
                            if (it.Max > 0 && AliveEnemies().Count(x => x.Key == it.Id) >= it.Max) break;
                            if (Summon(it.Id, e, it.NoTough) != null) made++;
                        }
                        Say($"{e.Name}: {say} ({Data.Enemy(it.Id)?.Name ?? it.Id} {made}{(made < n ? $" — 자리가 없어 {n - made} 못 세움" : "")})");
                        break;
                    }
                case "addCard":
                    {
                        var c = Data.Card(it.Id);
                        if (c == null) { Say($"(알 수 없는 상태 카드: {it.Id})"); return; }
                        int n = Math.Max(1, it.N); string to = it.To ?? "discard";
                        for (int k = 0; k < n; k++)
                        {
                            if (to == "hand" && Hand.Count < R.HAND_MAX) { Hand.Add(c.Id); CardCue(c.Id, "new", "hand", "foe"); }
                            else if (to == "draw") { Draw.Insert(Rng.Int(Draw.Count + 1), c.Id); CardCue(c.Id, "new", "draw", "foe"); }
                            else { Discard.Add(c.Id); CardCue(c.Id, "new", "discard", "foe"); }
                        }
                        StatusCue(e, $"「{c.Name}」 +{n}");
                        Say($"{e.Name}: {say} (「{c.Name}」 {n}장 → {(to == "hand" ? "손" : to == "draw" ? "뽑을 더미" : "버린 더미")})");
                        break;
                    }
            }
            // 강인도 회복은 그 적 자신만(all 이면 적 전체). 옛 「guard 면 적 전체」 는 없앴다(사용자 2026-10-06 — 회복 스킬이 있는 적만)
            if (it.Tough > 0) foreach (var x in it.All ? AliveEnemies() : new List<Unit> { e }) RegainTough(x, it.Tough);
            // 치는 수는 적의 약화를 한 번 쓴다(Dealt 가 이미 넣었다)
            if (IsHit(it.T) && St(e, "약화") > 0) Charge(e, "약화");
        }

        /// <summary>
        /// 보스 클론의 고학년 — 쪽지 foeUlt(시작) → act(T "ult" · Anim 은 치는 수가 있으면 attack) → 치는 수마다 foeUltHit → 그 수의 hurt · status … → foeUltEnd.
        /// 안의 수들은 한 번의 일(ActSeq 하나)로 돈다 — 약화 · 반격은 한 번만.
        /// </summary>
        void FoeUlt(Unit e, Intent it)
        {
            var subs = it.Then ?? new List<Intent>();
            int shown = UltHitOf(e, it);
            Say($"{e.Name}: 고학년 — 「{it.Say}」");
            Cue("foeUlt", e, new Cue { Hero = it.Id, Name = it.Say, V = shown });
            Cue("act", e, new Cue { Anim = HitLike(it) ? "attack" : "skill", Say = it.Say, T = "ult", Rush = Rushing, Hero = it.Id, Name = it.Say });
            int k = 0;
            foreach (var x in subs)
            {
                if (Over != null || e.Dead) break;
                if (IsHit(x.T)) Cue("foeUltHit", e, new Cue { Hero = it.Id, Name = it.Say, V = k++, T = x.T });
                FoeAct(e, x, x.Say ?? it.Say, true);
            }
            if (!e.Dead) Cue("foeUltEnd", e, new Cue { Hero = it.Id, Name = it.Say });
        }

        /// <summary>맞는 모습을 보일 사도(연출 · 성격 상성) — 피해는 늘 파티 몸으로 간다. 사도에게 열은 없다: 편성 순서 맨 앞 사도, 관통은 맨 뒤 사도.</summary>
        Unit PickTarget(bool fromBack) => PickInOrder(AliveParty().ToList(), fromBack);

        /// <summary>편성 순서로 맞을 사도 — 앞(Idx 가 작은)부터, 관통은 뒤부터. 봇(Bots.PickT)도 같은 셈.</summary>
        public static Unit PickInOrder(List<Unit> live, bool fromBack)
        {
            if (live.Count == 0) return null;
            return live.Aggregate((a, b) => fromBack ? (b.Idx > a.Idx ? b : a) : (b.Idx < a.Idx ? b : a));
        }

        /// <summary>적이 치는 값 — 층 배율 · 약화(-25%) · 사기(겹마다 +20%).</summary>
        public int Dealt(Unit from, int v)
        {
            double x = v;
            if (from.Dmgx != 1 && v > 0) x = Math.Max(1, Num.Round(v * from.Dmgx));
            double m = 1;
            if (St(from, "약화") > 0) m *= 1 - R.SV("약화");
            double up = 1 + R.StackEff("사기", St(from, "사기"));
            if (from.Side == Side.Enemy) up *= Math.Max(0.1, 1 + CounterMod(from, "dealt"));
            return Math.Max(0, Num.Round(x * m * up));
        }
    }
}
