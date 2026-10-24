using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 피해 한 대의 성질. Pure — 고정 지속(상태 · 상성 · 증감 · 방어 · 실드 모두 안 탄다) · Fixed — 고정 피해(상태 · 상성 · 증감을 안 탄다, 방어 · 실드엔 막힌다) ·
    /// Dot — 지속 피해(방어 · 실드를 뚫는다 · 반격 · 「피해를 받으면」 을 안 깨운다) · Pierce — 관통(방어를 건너뛴다) ·
    /// Card — 사도의 카드(고학년 포함)가 친 것 · Attack — 공격 카드가 친 것.
    /// </summary>
    public sealed class HurtOpts
    {
        public Unit From;
        public bool Pure, Crit, Card, Counter, Pierce, Fixed, Dot, Attack, All;
        /// <summary>HP 를 값으로 치른 것(payHp) — 「HP가 N% 이하가 되면」 을 깨운다.</summary>
        public bool Pay;
        public HashSet<string> Tags;
        /// <summary>전투 기록에서 이 피해를 받을 사도(From 이 없을 때만 — 지속 피해 · 응징처럼 상태를 건 사도). 피해 계산에는 쓰지 않는다.</summary>
        public Unit Credit;
        /// <summary>
        /// 쏘는 한 대(2026-10-13) — 카드 몸짓 밖에서 사도가 따로 치는 것: extra(추가 공격 · 패시브 피해) · bullet(탄환) · counter(반격) ·
        /// assist(협공) · resonance(공명) · summon(소환물). hurt 쪽지에 Label(이것) · Hero(쏜 사도 키)로 실려 화면이 쏘는 사도를 보인다. 판에는 영향 없다.
        /// </summary>
        public string Shot;
    }

    public sealed partial class Battle
    {
        /// <summary>성격 — 사도는 사도 데이터, 적은 적 데이터.</summary>
        public string NatureOf(Unit u) => u?.Nature;

        /// <summary>적의 약점 성격 — 판의 적 속성이 있으면(사도 클론 빼고) 그것을 이기는 성격, 없으면 적힌 weak, 그것도 없으면 상성에서(그 성격을 이기는 성격).</summary>
        public List<string> WeakOf(Unit e)
        {
            var d = Data.Enemy(e.Key);
            if (EnemyNature != null && e?.Nature != null && d?.Clone == null) return R.WeakTo(e.Nature);
            if (d?.Weak != null && d.Weak.Count > 0) return d.Weak;
            return d?.Nature != null ? R.WeakTo(d.Nature) : new List<string>();
        }

        /// <summary>WeakOf(e).Contains(n) 를 목록을 만들지 않고 — R.WeakTo(x) 에 n 이 든다 ⇔ R.BEATS[n] == x(2026-10-09).</summary>
        bool WeakHas(Unit e, string n)
        {
            var d = Data.Enemy(e.Key);
            if (EnemyNature != null && e?.Nature != null && d?.Clone == null) return R.BEATS.TryGetValue(n, out var b1) && b1 == e.Nature;
            if (d?.Weak != null && d.Weak.Count > 0) return d.Weak.Contains(n);
            return d?.Nature != null && R.BEATS.TryGetValue(n, out var b2) && b2 == d.Nature;
        }

        /// <summary>약점으로 쳤나 — 사도 성격이 그 적의 약점이거나(공명은 늘), 카드에 「약점」 이 붙었다.</summary>
        public bool IsWeakHit(Unit from, Unit to, HashSet<string> tags)
        {
            if (from == null || to == null || from.Side != Side.Party || to.Side != Side.Enemy) return false;
            if (tags != null && tags.Contains(Tag.Weak)) return true;
            if (tags != null && tags.Contains(Tag.Passion) && St(to, "열정 약점") > 0) return true;
            if (to.Status.Count > 0)   // 「약점 노출」 · 「<성격> 약점」(2026-10-10)
            {
                if (St(to, R.EXPOSED) > 0) return true;
                var nw = R.NatureWeakSt(NatureOf(from)); if (nw != null && St(to, nw) > 0) return true;
            }
            if (MarkedWeak(to)) return true;
            foreach (var k in Kw.Values) if (k.Def.WeakOwner && k.Carrier == "enemy" && k.Owner == from.Key && St(to, k.Id) > 0) return true;   // (아야 리워크) weakOwner — 키워드 주인만 약점
            var n = NatureOf(from);
            return !NoNature && n != null && (n == "공명" || WeakHas(to, n));
        }

        double NatureMod(Unit from, Unit to)
        {
            if (NoNature || from == null || to == null) return 1;
            int e = R.NatureEdge(NatureOf(from), NatureOf(to));
            // 2026-10-10 카제나식 — 상성 불리 페널티 없음: 적이 사도를 이기는 성격이어도 더 세게 치지 않고, 사도가 지는 상성이어도 덜 치지 않는다
            if (from.Side == Side.Party) return e > 0 ? 1 + R.NATURE_DMG : 1;
            return e < 0 ? 1 - R.NATURE_DEF : 1;
        }

        /// <summary>맞는 쪽 — 피해 감소 -15%(횟수) · 불굴 겹마다 -20%(세기, Cap 까지) · 포자증식은 합연산, 취약 ×1.5(횟수)는 따로 곱한다.</summary>
        int Taken(Unit to, int v)
        {
            // 받는 피해 증감은 합연산(카제나 — 불굴 -20%p · 피해 감소 -15%p · 포자증식 +10%p). 취약은 2026-10-10 카제나 공식대로 묶음 밖에서 ×(1 + 50%) 따로 곱한다
            double m = 1;
            bool vul = Charge(to, "취약");
            if (Charge(to, "피해 감소")) m -= R.SV("피해 감소");
            m -= R.StackEff("불굴", St(to, "불굴"));
            // 포자증식 — 겹마다 받는 피해 +10%, 발동하면 모두 사라진다(그 한 번의 일 동안은 붙는다)
            int spore = St(to, "포자증식");
            if (spore > 0) { to.Body.StUse["포자증식"] = ActSeq; to.Body.DotU["포자증식K"] = spore; DelSt(to, "포자증식"); }
            if (ActSeq != 0 && to.Body.StUse.TryGetValue("포자증식", out var sq) && sq == ActSeq && to.Body.DotU.TryGetValue("포자증식K", out var sk)) m += R.SV("포자증식") * sk;
            if (to.Side == Side.Enemy) m += CounterMod(to, "taken");
            return Math.Max(0, Num.Round(v * Math.Max(0, m) * (vul ? 1 + R.SV("취약") : 1)));
        }

        /// <summary>받는 피해 증감 — 파티는 사도마다 걸린 것 가운데 가장 큰 감소 하나와 가장 큰 증가 하나.</summary>
        double TakenMod(Unit u)
        {
            if (u.Side != Side.Party) return StatMod(u, "taken");
            double up = 0, down = 0;
            foreach (var h in Party) { double v = StatMod(h, "taken"); if (v > up) up = v; if (v < down) down = v; }
            return up + down;
        }

        /// <summary>피해 한 대. 사도를 치면 파티 몸을 친다(사도는 연출 자리).</summary>
        public void Hurt(Unit u, int v, HurtOpts o = null)
        {
            o ??= new HurtOpts();
            var from = o.From;
            // 영혼 공유 — 영혼 공유 적 가운데 맨 왼쪽이 아니면 피해 0
            if (u.Side == Side.Enemy && !u.Dead && (Data.Enemy(u.Key)?.Soul ?? false) && Enemies.Any(x => x != u && !x.Dead && x.Idx < u.Idx && (Data.Enemy(x.Key)?.Soul ?? false)))
            { Say($"{u.Name}: 영혼 공유 — 맨 앞이 아니라 피해가 들지 않는다"); StatusCue(u, "영혼 공유"); return; }
            int guard0 = u.Block + u.Shield;
            bool plain = o.Pure || o.Fixed;
            if (o.Card && u.Side == Side.Enemy) u.HitSeq = ActSeq;
            if (o.Card && o.Crit && from != null && from.Side == Side.Party && u.Side == Side.Enemy && CritSeq != ActSeq) { CritSeq = ActSeq; Emit("crit", new EmitInfo { By = from.Key, Target = u, Seq = ActSeq }); }
            int d = plain ? v : Taken(u, v);
            // 때리는 사도의 상태(카드의 피해에만) — 사기 · 약화
            if (!plain && o.Card && from != null && from.Side == Side.Party)
            {
                // 사기는 합연산이라 카드 계수에 더했다(BattleFx · Morale) — 여기선 약화만
                double m = 1;
                if (Charge(from, "약화")) m *= 1 - R.SV("약화");
                if (m != 1) d = Num.Round(d * m);
            }
            // 성격 상성 — 사도 → 적은 약점이 곧 유리. 키워드 1개당 「약점 공격 피해」(stat weak — 2026-10-19 아야 「만년설」)는 약점으로 칠 때만 그 사도에게 더한다
            if (!plain && from != null)
                d = Num.Round(d * (from.Side == Side.Party && u.Side == Side.Enemy && IsWeakHit(from, u, o.Tags) ? 1 + R.WEAK_DMG + StatMod(from, "weak") : NatureMod(from, u)));
            // 분쇄 · 잔불 · 잔광
            if (!plain && u.Side == Side.Enemy && from != null && from.Side == Side.Party && o.Card)
            {
                double k = 1;
                if (o.Tags != null && o.Tags.Contains(Tag.Crush) && (u.Block > 0 || u.Shield > 0)) k *= 1 + R.SV("분쇄");
                if (u.EmberSeq == ActSeq && ActSeq != 0) k *= u.EmberK;
                else if (St(u, "잔불") > 0 && (u.Broken || Num.Round(d * k) >= u.Hp + u.Block + u.Shield))
                {
                    int n = Math.Min(St(u, "잔불"), (int)R.SV("잔불Max"));
                    u.EmberK = 1 + n * R.SV("잔불"); u.EmberSeq = ActSeq; k *= u.EmberK;
                    DelSt(u, "잔불");
                    Say($"{u.Name}: 잔불 {n} — 피해 +{Num.Round(n * R.SV("잔불") * 100)}%"); StatusCue(u, "잔불!");
                }
                if (o.Attack && u.Broken && GlowOn()) k *= 1 + R.SV("잔광");
                if (k != 1) d = Num.Round(d * k);
            }
            // 증감 — 주는 피해(때리는 쪽) × 받는 피해(맞는 쪽). 아무리 깎여도 10%
            if (!plain)
            {
                double m = (1 + (from != null ? StatMod(from, "dealt") : 0)) * (1 + TakenMod(u) + OwnerMarkMod(from, u));
                d = Math.Max(0, Num.Round(d * Math.Max(0.1, m)));
            }
            // 과보호(쌓이는 수치 flat) — 받는 피해가 그 값
            if (u.Side == Side.Enemy && d > 0 && !o.Pure && !o.Dot) { int fl = CounterFlat(u); if (fl > 0) d = Math.Min(d, fl); }
            // 한 대 깎기(cutHit) · 소환물(키워드 guard) — 적의 공격 한 대를 대신 받거나(cut 이면 깎기만)
            if (u.Side == Side.Party && from != null && from.Side == Side.Enemy && !o.Pure && !o.Dot && d > 0)
            {
                if (CutNext > 0) { d = Num.Round(d * (1 - CutNext)); Say($"한 대 깎기 -{Num.Round(CutNext * 100)}%"); CutNext = 0; }
                else if (CutOf(from) is int cp && cp > 0) { d = Num.Round(d * (1 - cp / 100.0)); Counts.Remove("cut|" + from.Idx); Say($"{from.Name}: 한 대 깎기 -{cp}%"); }   // 적마다 깎기(엘다인 3차)
                if (MinionGuard(ref d)) return;
            }
            int guard = 0;
            bool foeHit = u.Side == Side.Party && from != null && from.Side == Side.Enemy && !o.Pure && !o.Dot;
            int shareCut = foeHit && Kw.Count > 0 ? ShareCut(u, ref d) : 0;   // 분산(▶E69)
            double tk = foeHit ? TauntK(u) : 0;   // 도발 — 그 대에 실드 (1+k) 배(▶E63)
            if (!o.Pure && !o.Dot && !o.Pierce && u.Block > 0) { int a = Math.Min(u.Block, d); u.Block -= a; d -= a; guard += a; }
            if (!o.Pure && !o.Dot && u.Shield > 0)
            {
                if (tk > 0) { int a = Math.Min(d, (int)Math.Floor(u.Shield * (1 + tk))); int used = Math.Min(u.Shield, (int)Math.Ceiling(a / (1 + tk))); u.Shield -= used; d -= a; guard += a; }
                else { int a = Math.Min(u.Shield, d); u.Shield -= a; d -= a; guard += a; }
            }
            if (shareCut > 0 && !Pool.Dead) { Pool.Shield += shareCut; GainCue(PartyRep(u), "shield", shareCut); }
            if (u.Side == Side.Party && d > 0 && from != null && from.Side == Side.Enemy && !o.Pure && !o.Dot && !o.Pay) HandHurtBig(u, ref d);   // 손에서 반응하는 카드(엘다인 3차 ▶E48)
            // 절대 무적(1턴) · 회피(한 대에 1) — 파티가 체력을 잃지 않는다(값으로 치른 HP 는 예외)
            if (u.Side == Side.Party && d > 0 && !o.Pay)
            {
                if (St(u, "절대 무적") > 0) { Say("파티: 절대 무적 — 체력을 잃지 않는다"); StatusCue(u, "절대 무적!", true); d = 0; }
                else if (!o.Dot && from != null && from.Side == Side.Enemy && St(u, "회피") > 0) { AddSt(u, "회피", -1); Say("파티: 회피!"); StatusCue(u, "회피!", true); d = 0; }
            }
            if (from != null && from.Side == Side.Party && u.Side == Side.Enemy && ActSeq != 0)
            {
                if (DealtSeq != ActSeq) { DealtSeq = ActSeq; DealtAct = 0; }
                DealtAct += d + guard;
            }
            if (from != null && from.Side == Side.Party && u.Side == Side.Enemy) FormDealtAdd(from, d + guard);   // 이번 변신 중 준 피해(▶E76)
            // 끈기 — 파티가 쓰러질 피해에서 HP 1 로 버틴다(1 준다)
            if (u.Side == Side.Party && d >= u.Hp && d > 0 && St(u, "끈기") > 0) { AddSt(u, "끈기", -1); d = Math.Max(0, u.Hp - 1); Say("파티: 끈기 — HP 1 로 버틴다"); StatusCue(u, "끈기!", true); Emit("endure", new EmitInfo { Who = u, From = from }); }   // 「적 1명」 = 그 대를 친 적(엘다인 3차 ▶E68)
            double before = (double)u.Hp / u.MaxHp; int hp0 = u.Hp;
            u.Hp -= d;
            if (u.Side == Side.Enemy && d > 0 && hp0 > 0) u.Lost += Math.Min(d, hp0);   // 적이 잃은 HP 합(프론티어 점수) — 주인 없는 피해도
            if (HpMeter != null && u.Side == Side.Enemy && d > 0 && hp0 > 0) HpMeterAdd(from ?? o.Credit, u, Math.Min(d, hp0), o);   // 측정 전용(사도별 HP 피해)
            // 전투 기록 — 적에게 실제로 깎인 HP(넘친 피해 빼고) + 벗긴 방어 · 실드. 반격 · 추가 공격 · 소환물 · 지속 피해도 From(없으면 Credit) 사도 몫
            if (u.Side == Side.Enemy) TallyDealt(from ?? o.Credit, Math.Max(0, Math.Min(d, hp0)) + guard);
            if (d > 0 || guard > 0) Cue("hurt", u, new Cue { V = d, Guard = guard, Crit = o.Crit, From = Math.Max(0, hp0), To = Math.Max(0, u.Hp),
                Label = o.Shot != null && from != null && from.Side == Side.Party ? o.Shot : null, Hero = o.Shot != null && from != null && from.Side == Side.Party ? from.Key : null });
            if (u.Side == Side.Party && d > 0) Talk(u, "hit");
            if (u.Hp <= 0) { LethalKill(u); return; }
            if (u.Side == Side.Enemy && from != null && from.Side == Side.Party && !o.Dot && (d > 0 || guard > 0)) CounterEvent(u, "hit");
            if (u.Side == Side.Enemy && d > 0 && u.Seized.Count > 0) SeizeHurt(u, d);   // 빼앗은 적에게 준 피해(지속 피해 포함)
            if (u.Side == Side.Enemy && guard > 0) Say($"{u.Name}: 실드가 {guard} 막음 (남은 실드 {u.Block + u.Shield})");
            if (u.Side == Side.Enemy && guard > 0 && guard0 > 0 && u.Block + u.Shield == 0 && !u.Dead) { FoePassives("guardBreak", u); Emit("foeShieldBreak", new EmitInfo { Target = u, By = from?.Side == Side.Party ? from.Key : Acting, V = guard }); }
            if (u.Side == Side.Party && guard > 0 && guard0 > 0 && u.Block + u.Shield == 0 && from != null && from.Side == Side.Enemy && St(Pool, "은방울") > 0) SilverBell("실드가 깨짐");   // (▶E65)
            if (u.Side == Side.Party && guard > 0 && guard0 > 0 && u.Block + u.Shield == 0 && from != null && from.Side == Side.Enemy) Emit("shieldBreak", new EmitInfo { Who = u, From = from, Target = from, V = guard, By = ShieldBy, Seq = ActSeq });
            if (u.Side == Side.Enemy && o.Card && from != null && from.Side == Side.Party && (d > 0 || guard > 0)) CountHit(u, from);   // 그 적을 친 횟수(▶E71)
            if (u.Side == Side.Enemy && o.Card && from != null && from.Side == Side.Party && (d > 0 || guard > 0) && !u.Dead) Emit("hit", new EmitInfo { By = from.Key, Target = u, V = d + guard, Seq = ActSeq, Weak = IsWeakHit(from, u, o.Tags) });
            if (foeHit && guard > 0) from.Warded += guard;   // 그 적의 피해를 막은 합(프론티어 방어 점수 — Frontier.Ward)
            if (u.Side == Side.Party && !o.Pure && !o.Dot && from != null && from.Side == Side.Enemy) { HitTakenAdd(); TakenNow += d + guard; GuardedNow += guard; SumIncoming += d + guard; SumGuarded += guard; }
            if (u.Side == Side.Party && (d > 0 || (guard > 0 && from != null && from.Side == Side.Enemy)) && !o.Pure && !o.Dot)
            {
                if (d > 0) foreach (var h in Party) HurtNow.Add(h.Key);
                var tgt = from != null && from.Side == Side.Enemy ? from : null;
                if (d > 0) Emit("hurt", new EmitInfo { Who = u, From = from, V = d + guard, N = d, Target = tgt });
                else Emit("blocked", new EmitInfo { Who = u, From = from, V = guard, Target = tgt, Seq = ActSeq });
                if (d > 0) Emit("lowHp", new EmitInfo { Who = u, Before = before, After = (double)u.Hp / u.MaxHp });
            }
            // HP 를 치렀다(대가) — 「HP가 N% 이하가 되면」 은 치른 HP 로도 돈다
            if (u.Side == Side.Party && o.Pay && d > 0) Emit("lowHp", new EmitInfo { Who = u, Before = before, After = (double)u.Hp / u.MaxHp });
            // 반격 — 적에게 맞으면 그 적에게 방어 기반 피해 150%(다 막았으면 300%), 치명 적용. 수 하나에 한 번 · 1 준다
            if (!o.Pure && !o.Dot && !o.Counter && u.Side == Side.Party && !u.Dead && from != null && from.Side == Side.Enemy && !from.Dead && (St(u, "반격") > 0 || St(u, "빙벽") > 0))
            {
                var body = u.Body;
                // 빙벽(1턴) — 반격 겹을 쓰지 않고 반격한다
                if (body.CtrSeq != ActSeq && (St(u, "빙벽") > 0 || Charge(u, "반격")))
                {
                    body.CtrSeq = ActSeq;
                    CounterStrike(u, from, d <= 0 && guard > 0);   // 반격 모양 · 증감(엘다인 3차 ▶E38~E40) — 모양이 없으면 옛 반격 그대로
                }
            }
            // 충격(파티 — 적이 건 것) — 적의 치는 수에 맞으면 고정 피해 80%(방어 · 실드가 받아 냈으면 +50%), 수 하나에 한 번
            if (!o.Pure && !o.Dot && !o.Counter && u.Side == Side.Party && !u.Dead && from != null && from.Side == Side.Enemy && St(u, "충격") > 0 && ShockSeq != ActSeq)
            {
                ShockSeq = ActSeq;
                int sv = Math.Max(1, Num.Round(DotUnit(u, "충격") * R.SV("충격") * (guard > 0 ? 1 + R.SV("충격Shield") : 1)));
                AddSt(u, "충격", -1);
                Say($"파티: 충격{(guard > 0 ? "(방어 위)" : "")} — 고정 피해 {sv}"); StatusCue(u, "충격!");
                Hurt(u, sv, new HurtOpts { Pure = true, Dot = true });
            }
            if (u.Side == Side.Enemy && d > 0 && !o.Pure && !o.Dot)
            {
                FoePassives("hurt", u, from);
                if (!u.Dead) FoePassives("lowHp", u, null, before, (double)u.Hp / u.MaxHp);
            }
        }

        // ── 강인도 · 격파 ──────────────────────────────────────────────
        /// <summary>
        /// 강인도를 n 깎는다. 0 이 되면 격파 — AP +1 · brk 수는 끊긴다 · 「격파되면」 규칙이 돈다. 격파했으면 true.
        /// 격파된 적도 제 차례에 그대로 행동하고 패시브 · 반응도 그대로 돈다(2026-10-24 — 10-08 「행동 불가」 를 없앴다, 카제나처럼).
        /// dying — 이 일격에 쓰러지는 적(처치 일격, LethalKill). 강인도 · 격파 쪽지와 AP 는 같고, 끊김 · 적의 「격파되면」 은 없다.
        /// 「격파」 일(Emit · HandAuto)은 쓰러뜨린 뒤 LethalKill 이 낸다.
        /// </summary>
        bool ToughHit(Unit e, double n, bool dying = false)
        {
            if (e == null || e.Side != Side.Enemy || e.Dead || e.Broken || !(n > 0) || e.ToughMax <= 0) return false;
            foreach (var kw in Kw.Values) if (kw.Carrier == "enemy") foreach (var p in kw.Def.Per) if (p.Stat == "tough") n += St(e, kw.Id) * p.V;   // 적 표식 1개당 받는 강인도 피해 +v
            double tt = Data.Enemy(e.Key)?.ToughTaken ?? 0;
            if (tt > 0) n *= tt;
            if (HasRare(e, "toughGuard", out _)) n *= 0.8;
            double from = e.Tough;
            double left = e.Tough - n;
            e.Tough = left < 1e-6 ? 0 : Math.Round(left * R.TOUGH.Grid) / R.TOUGH.Grid;   // 1/3 · 1/6 을 여러 번 빼면 남는 소수 찌꺼기(1e-15)가 격파를 막았다
            ToughDealt += from - e.Tough;
            HitCue(e, "tough", new Cue { From = from, To = e.Tough });
            Say($"{e.Name}: 강인도 {from:0.##} → {e.Tough:0.##} / {e.ToughMax:0.##}");
            if (e.Tough > 0) return false;
            e.Broken = true;
            Breaks++;
            BreakSeq = ActSeq;
            GainAp(R.TOUGH.Ap);
            if (dying)
            {
                Say($"{e.Name}: 격파하며 처치! (AP +{R.TOUGH.Ap})");
                HitCue(e, "break", new Cue { V = R.TOUGH.Ap });
                return true;
            }
            e.BreakWait = true;
            Say($"{e.Name}: 격파! (AP +{R.TOUGH.Ap})");
            HitCue(e, "break", new Cue { V = R.TOUGH.Ap });
            Emit("break", new EmitInfo { Target = e, By = Acting });
            if (!e.Dead && Over == null) CounterEvent(e, "break");   // 쌓이는 수치 onBreak · breakCut(M.E.O.W 「출력」 — 2026-10-24)
            if (e.Intent != null && e.Intent.Brk && !e.Dead) { UltCut(e, "격파"); Say($"{e.Name}: 격파 — 「{e.Intent.Say}」 를 놓쳤다"); StatusCue(e, "끊김!"); e.Intent = null; }
            if (!e.Dead) FoePassives("broken", e);
            if (Over == null) FoePassives("allyBroken", e);   // 동료가 격파되면(2026-10-07 — 조합 반응)
            SeizeBack(e, "격파");   // 빼앗긴 카드는 격파하면 돌아온다
            HandAuto("break", new AutoInfo { Target = e });
            return true;
        }

        // ── 처치 일격의 강인도 ─────────────────────────────────────────
        /// <summary>카드의 한 대가 적을 쓰러뜨리면, 쓰러뜨리기 전에 그 카드의 강인도 피해를 먼저 넣는다(BattleFx 가 걸고 Hurt 가 부른다).</summary>
        Unit lethalFor; Func<bool> lethalTough;

        /// <summary>
        /// 쓰러뜨린다 — 피해 → 강인도 → 격파 판정 → 죽음(사용자 2026-10-07). 처치 일격에 강인도가 0 이 되면 「격파하며 처치」:
        /// 격파와 같은 AP +1(적마다 — 보통 격파와 같은 몫) · 「격파」 일은 쓰러뜨린 뒤에(그 적은 이미 없어 「그 적」 효과는 다른 적에게). 근면과는 따로 — 둘 다 받는다.
        /// </summary>
        void LethalKill(Unit u)
        {
            bool brk = false;
            if (u.Side == Side.Enemy && lethalTough != null && lethalFor == u)
            {
                var f = lethalTough; lethalTough = null; lethalFor = null;
                brk = f();
            }
            Kill(u);
            if (!brk) return;
            Emit("break", new EmitInfo { Target = u, By = Acting });
            HandAuto("break", new AutoInfo { Target = u });
        }

        /// <summary>강인도 쪽지는 그 적이 맞은 쪽지 바로 뒤에(맞자마자 움직인 적 패시브 몸짓보다 앞).</summary>
        void HitCue(Unit e, string k, Cue more)
        {
            if (Cues == null) { Cue(k, e, more); return; }
            int at = -1;
            for (int i = Cues.Count - 1; i >= 0; i--) { var f = Cues[i]; if (f.K == "hurt" && f.Side == Side.Enemy && f.Idx == e.Idx) { at = i; break; } }
            bool actAfter = false;
            for (int i = at + 1; at >= 0 && i < Cues.Count; i++) if (Cues[i].K == "act") { actAfter = true; break; }
            if (at < 0 || !actAfter) { Cue(k, e, more); return; }
            int j = at + 1;
            while (j < Cues.Count && Cues[j].Side == Side.Enemy && Cues[j].Idx == e.Idx && (Cues[j].K == "tough" || Cues[j].K == "break")) j++;
            more.K = k; more.Side = e.Side; more.Idx = e.Idx;
            Cues.Insert(j, more);
            OnCue?.Invoke(more);
        }
    }
}
