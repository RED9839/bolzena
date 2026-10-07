using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 적 효과 틀(Docs/키워드.md §5) — 쌓이는 수치(counters) · 희귀종(rare) · 죽을 때(death · 재 속 · 가사) · 소환 연결 · 영혼 공유 ·
    /// 카드에 상태 · 손 코스트 · 섞기 · 저절로 내기 · 예고 바꾸기.
    /// </summary>
    public sealed partial class Battle
    {
        // ── 쌓이는 수치 ───────────────────────────────────────────────
        public CounterDef CounterOf(Unit e, string name) => e == null || name == null ? null : Data.Enemy(e.Key)?.Counters?.FirstOrDefault(c => c.Name == name);
        List<CounterDef> CountersOf(Unit e) => e != null && e.Side == Side.Enemy ? Data.Enemy(e.Key)?.Counters ?? new List<CounterDef>() : new List<CounterDef>();

        void InitCounters(Unit e)
        {
            foreach (var c in CountersOf(e)) if (c.Start > 0) e.Status[c.Name] = Math.Min(c.Max, c.Start);
        }

        /// <summary>쌓이는 수치의 1개당 주는 · 받는 피해 합(stat: dealt · taken).</summary>
        double CounterMod(Unit e, string stat)
        {
            double v = 0;
            foreach (var c in CountersOf(e))
            {
                int n = St(e, c.Name); if (n <= 0) continue;
                v += n * (stat == "dealt" ? c.Dealt : c.Taken);
            }
            return v;
        }

        /// <summary>과보호 — 겹이 있으면 받는 피해가 이 값. 없으면 -1.</summary>
        int CounterFlat(Unit e)
        {
            int f = -1;
            foreach (var c in CountersOf(e)) if (c.Flat > 0 && St(e, c.Name) > 0) f = f < 0 ? c.Flat : Math.Min(f, c.Flat);
            return f;
        }

        /// <summary>쌓이는 수치에 더한다(빼면 음수). 문턱을 넘으면 행동, 0 이 되면 기절(비행).</summary>
        void CounterAdd(Unit e, CounterDef c, int delta)
        {
            if (e == null || c == null || e.Dead || delta == 0 || Over != null) return;
            int before = St(e, c.Name);
            int after = Num.Clamp(before + delta, 0, c.Max);
            if (after == before) return;
            SetStRaw(e, c.Name, after);
            StatusCue(e, $"{c.Name} {(after > before ? "+" : "")}{after - before}", after > before);
            if (c.StunAtZero && before > 0 && after == 0)
            {
                Say($"{e.Name}: {c.Name} 0 — 기절"); AddStatus(e, R.STUN, 1, 1, null);
            }
            if (c.At > 0 && c.Act != null && before < c.At && after >= c.At)
            {
                if (!c.Keep) SetStRaw(e, c.Name, Math.Min(c.Max, c.Start));
                string mode = c.Mode ?? "now";
                // 격파 · 기절로 쉬는 적은 지금 움직이지 못한다 — 「즉시」 수는 다음 차례로 미룬다(사용자 버그 2026-10-07: 격파된 불효자손이 심통으로 내리찍었다)
                if (mode == "now" && e.Sealed) mode = "next";
                Say($"{e.Name}: {c.Name} {after} — {(mode == "next" ? "다음 차례에" : mode == "replace" ? "수가 바뀐다" : "즉시")} 「{c.Act.Say ?? c.Name}」");
                if (mode == "next") e.ForceNext = c.Act;
                else if (mode == "replace") { e.Intent = c.Act; e.IntentFromCharge = false; StatusCue(e, "수 바뀜"); }
                else { Rushing = true; try { ActEnemy(e, c.Act, true, c.Act.Say ?? c.Name); } finally { Rushing = false; } }
            }
        }

        /// <summary>쌓이는 수치의 때 — hit(맞음) · card(파티가 카드를 냄, type) · turnStart · turnEnd.</summary>
        void CounterEvent(Unit e, string ev, string type = null)
        {
            if (e == null || e.Dead) return;
            foreach (var c in CountersOf(e).ToList())
            {
                if (Over != null || e.Dead) return;
                switch (ev)
                {
                    case "hit": if (c.OnHit != 0) CounterAdd(e, c, c.OnHit); break;
                    case "card":
                        if (c.OnCard == 0 || (c.AfterAct && !e.ActedTurn)) break;
                        if (c.CardType != null && !TypeOk(c.CardType, type)) break;
                        CounterAdd(e, c, c.OnCard); break;
                    case "turnStart":
                        if (c.ResetTurnStart) { int b = St(e, c.Name); SetStRaw(e, c.Name, Math.Min(c.Max, c.Start)); if (b != St(e, c.Name)) StatusCue(e, $"{c.Name} {St(e, c.Name)}", true); }
                        if (c.OnTurnStart != 0) CounterAdd(e, c, c.OnTurnStart);
                        break;
                    case "turnEnd":
                        if (c.ClearTurnEnd && St(e, c.Name) > 0) { SetStRaw(e, c.Name, 0); Say($"{e.Name}: {c.Name} — 사라진다"); }
                        if (c.OnTurnEnd != 0) CounterAdd(e, c, c.OnTurnEnd);
                        break;
                    case "attack":   // 자기가 치는 수를 한 뒤 — 모은 것을 쏟았다
                        if (c.ClearOnAttack && St(e, c.Name) > 0) { SetStRaw(e, c.Name, 0); Say($"{e.Name}: {c.Name} — 쏟아 사라진다"); StatusCue(e, $"{c.Name} 0", true); }
                        break;
                }
            }
        }

        /// <summary>카드 종류 조건 — 「공격」 · 「!공격」(공격이 아닌 것).</summary>
        static bool TypeOk(string want, string type) => want == null || (want.StartsWith("!", StringComparison.Ordinal) ? type != want.Substring(1) : type == want);

        // ── 희귀종 ────────────────────────────────────────────────────
        bool HasRare(Unit e, string id, out RareDef r)
        {
            r = Data.Enemy(e?.Key)?.Rare?.FirstOrDefault(x => x.Id == id);
            return r != null;
        }

        static readonly Dictionary<EnemyDef, List<EnemyPassive>> passCache = new();

        /// <summary>적 패시브 + 희귀종 덧붙임이 만든 패시브.</summary>
        public static List<EnemyPassive> FoePassList(EnemyDef d)
        {
            if (d == null) return new List<EnemyPassive>();
            if (d.Rare == null || d.Rare.Count == 0) return d.Passives ?? new List<EnemyPassive>();
            lock (passCache)
            {
                if (passCache.TryGetValue(d, out var hit)) return hit;
                var o = (d.Passives ?? new List<EnemyPassive>()).ToList();
                foreach (var r in d.Rare)
                {
                    EnemyPassive P(string on, Intent i) => new EnemyPassive { Name = "희귀종 · " + (R.RARES.TryGetValue(r.Id, out var k) ? k : r.Id), On = on, Do = i };
                    switch (r.Id)
                    {
                        case "poisonHand": o.Add(P("afterDraw", new Intent { T = "cardDebuff", Id = "독", N = 2, To = "hand" })); break;
                        case "reshuffle": o.Add(P("act", new Intent { T = "reshuffle", Id = r.Card, N = 1 })); break;
                        case "autoCard": o.Add(P("afterDraw", new Intent { T = "autoPlay", N = 1 })); break;
                        case "costUp": o.Add(P("afterDraw", new Intent { T = "handCost", V = 1, N = 2 })); break;
                        case "crystal": o.Add(P("fightStart", new Intent { T = "buff", Id = "결정화", V = 3 })); break;
                        case "actDebuff": o.Add(P("act", new Intent { T = "debuff", Id = r.St ?? "취약", V = 2 })); break;
                    }
                }
                passCache[d] = o;
                return o;
            }
        }

        /// <summary>희귀종 anxietyHits — 그 상태 카드가 뽑을 더미 · 손 · 버린 더미에 몇 장.</summary>
        int RareHits(Unit e)
        {
            if (!HasRare(e, "anxietyHits", out var r) || r.Card == null) return 0;
            return Draw.Concat(Hand).Concat(Discard).Count(x => GameData.BaseId(x) == r.Card);
        }

        // ── 죽을 때 · 되살아남 · 소환 연결 ────────────────────────────
        /// <summary>적이 쓰러졌다 — 죽을 때 패시브(재 속 · 가사 · 그 밖의 수) · 그 적이 세운 Tied 적도 쓰러진다.</summary>
        void FoeDeath(Unit u)
        {
            SeizeBack(u, "처치");   // 빼앗긴 카드는 쓰러뜨리면 돌아온다
            var d = Data.Enemy(u.Key);
            if (d != null && !(FoeQuiet > 0 && Turn <= FoeQuiet))
                foreach (var p in FoePassList(d).Where(p => p.On == "death").ToList())
                {
                    if (Over != null && Over != "win") return;
                    Say($"{u.Name} · {p.Name}");
                    if (p.Do.T == "revive") { u.ReviveIn = Math.Max(1, p.Do.N); u.ReviveHp = p.Do.V > 0 ? p.Do.V / 100.0 : 0.5; Say($"{u.Name}: 재 속 — {u.ReviveIn}턴 뒤 되살아난다"); StatusCue(u, "재 속"); }
                    else if (p.Do.T == "feign") { u.Feign = true; Say($"{u.Name}: 쓰러진 척한다 — 회복받으면 일어선다"); StatusCue(u, "가사"); }
                    else ActEnemy(u, p.Do, true, p.Do.Say ?? p.Name);
                }
            // 보스가 세운 강인도 없는 소환물(수 summon 의 noTough)은 보스가 쓰러지면 같이 쓰러진다 — 보스 전투는 보스 하나를 쓰러뜨리면 끝(사용자 2026-10-06)
            foreach (var x in Enemies.Where(x => !x.Dead && x.Summoner == u.Idx && ((Data.Enemy(x.Key)?.Tied ?? false) || (u.Boss && x.ToughMax <= 0))).ToList())
            {
                Say($"{x.Name}: 세운 이가 쓰러져 함께 쓰러진다");
                Kill(x);
            }
        }

        void Revive(Unit e, int hp)
        {
            e.Dead = false; e.Feign = false; e.ReviveIn = 0;
            e.Hp = Math.Max(1, Math.Min(e.MaxHp, hp));
            e.Status.Clear(); e.Mods.Clear(); e.Broken = false; e.Tough = e.ToughMax; e.Sealed = false;
            InitCounters(e);
            RollIntent(e, true);
            Cue("revive", e, new Cue { V = e.Hp, Name = e.Name });
            Say($"{e.Name}: 되살아난다 (HP {e.Hp})");
        }

        /// <summary>내 턴 시작 — 재 속 적이 다 되면 되살아난다(남은 적이 있을 때만 — 다 쓰러지면 이미 이겼다).</summary>
        void ReviveTick()
        {
            if (Over != null) return;
            foreach (var e in Enemies.Where(x => x.Dead && x.ReviveIn > 0).ToList())
                if (--e.ReviveIn <= 0) Revive(e, Num.Round(e.MaxHp * e.ReviveHp));
        }

        // ── 적의 수(새것) ──────────────────────────────────────────────
        /// <summary>새 수 — 처리했으면 true.</summary>
        bool FoeActKit(Unit e, Intent it, string say)
        {
            switch (it.T)
            {
                case "seize": SeizeCards(e, it, say); return true;   // 손패 흡수(BattleSeize.cs)
                case "count":
                    CounterAdd(e, CounterOf(e, it.Id), it.V);
                    Say($"{e.Name}: {say} ({it.Id} {(it.V >= 0 ? "+" : "")}{it.V})");
                    return true;
                case "cardDebuff":
                    {
                        var ids = PickCards(it.To ?? "hand", Math.Max(0, it.N));
                        foreach (var id in ids)
                        {
                            if (it.Id == "독")
                            {
                                int bas = it.V > 0 ? it.V : R.FOE_DOT;
                                AddCardSt(id, "독", Math.Max(1, Num.Round(bas * (e.Dmgx > 0 ? e.Dmgx : 1) * R.SV("독"))));
                            }
                            else AddCardSt(id, it.Id, 1);
                        }
                        StatusCue(e, $"{it.Id} → 카드 {ids.Count}장");
                        Say($"{e.Name}: {say} (카드 {ids.Count}장에 {it.Id})");
                        return true;
                    }
                case "handCost":
                    {
                        var ids = PickCards("hand", Math.Max(0, it.N));
                        foreach (var id in ids) AddCardSt(id, "비용", it.V);
                        Say($"{e.Name}: {say} (손 {ids.Count}장 비용 {(it.V >= 0 ? "+" : "")}{it.V})");
                        return true;
                    }
                case "reshuffle":
                    {
                        foreach (var id in Hand) CardCue(id, "hand", "discard", "foe");
                        Discard.AddRange(Hand); Hand.Clear();
                        Draw = Shuffle(Rng, Draw.Concat(Discard).ToList()); Discard.Clear(); CardCue(null, "discard", "draw", "shuffle");
                        if (it.Id != null && Data.Card(it.Id) != null)
                            for (int k = 0; k < Math.Max(1, it.N); k++) { Draw.Insert(Rng.Int(Draw.Count + 1), it.Id); CardCue(it.Id, "new", "draw", "foe"); }
                        Say($"{e.Name}: {say} (손을 모두 버리고 섞는다{(it.Id != null ? $" · 「{Data.Card(it.Id)?.Name}」" : "")})");
                        Emit("shuffle", new EmitInfo());
                        return true;
                    }
                case "autoPlay":
                    {
                        for (int k = 0; k < Math.Max(1, it.N) && Over == null; k++)
                        {
                            var ok = Hand.Select((id, i) => (id, i)).Where(x => { var c = CardOf(x.id); return c != null && !c.IsStatus && !c.IsCurse && CanPlay(x.id, true) == null; }).ToList();
                            if (ok.Count == 0) break;
                            var pick = ok[Rng.Int(ok.Count)];
                            var foes = AliveEnemies(); if (foes.Count == 0) break;
                            Say($"{e.Name}: {say} — 「{CardOf(pick.id).Name}」 이(가) 저절로 나간다");
                            FreeOnce.Add(pick.id);
                            if (!PlayCard(pick.i, foes[Rng.Int(foes.Count)].Idx, new PlayOpts { Auto = true }).Ok) FreeOnce.Remove(pick.id);
                        }
                        return true;
                    }
                case "shift":
                    if (it.Next != null) { e.Intent = it.Next; e.IntentFromCharge = false; StatusCue(e, "수 바뀜"); Say($"{e.Name}: {say} — 수가 바뀐다"); }
                    return true;
                case "revive":
                case "feign":
                    return true;   // 죽을 때 패시브에서만 뜻이 있다
            }
            return false;
        }
    }
}
