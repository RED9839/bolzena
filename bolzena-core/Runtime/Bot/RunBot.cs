using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>한 판 시뮬의 손잡이.</summary>
    public sealed class SimOpts
    {
        public bool SmartFight = true, SmartOut = true;
        /// <summary>숙련 봇(RunBotSkilled.cs) — 의도한 플레이: 기본 카드를 빼고 고유 카드 · 파티 축으로 덱을 짠다. 끄면 옛 봇(초보).</summary>
        public bool Skilled;
        /// <summary>기본 카드 없이 파티 사도의 고유 카드만으로 판을 시작한다(극단 측정).</summary>
        public bool UniqueOnly;
        /// <summary>교주 능력치(크레파스 — Crayon.Perks) — 판을 열 때 건다. null = 없음(난이도 기준).</summary>
        public Dictionary<string, double> Perks;
        public int Depth = 1, Width = 3;
        public double Hpx = 1, Dmgx = 1;
        public string Village;
        /// <summary>싸움마다 판을 넘겨 본다(시험 · 기록).</summary>
        public Action<Battle, Run> OnFight;
    }

    /// <summary>한 판의 결과.</summary>
    public sealed class SimResult
    {
        public bool Clear;
        public string Village;
        /// <summary>쓰러진 층(0 · 1).</summary>
        public int Floor;
        public string Where;
        public int Fights, Turns, Gold, Deck;
        /// <summary>판이 끝날 때 덱의 고유 카드(복제본 포함) · 기본 카드 장수.</summary>
        public int Uniques, Basics;
        /// <summary>상점에서 뺀 횟수.</summary>
        public int Removals;
        /// <summary>판이 끝날 때 서 있던 연속 이벤트 깃발.</summary>
        public List<string> Flags = new();
        /// <summary>깃발이 선 채로 줄기 뒤 이벤트를 만난 횟수.</summary>
        public int FlagReads;
        public Dictionary<string, (int n, int turns, int win)> Kinds = new();
        /// <summary>강인도 통계 — 카드가 강인도를 깎은 횟수 · 그 가운데 약점 · 격파 수. 싸움 종류(fight · elite · boss …)마다 격파 수.</summary>
        public int ToughHits, ToughWeakHits, Breaks;
        public Dictionary<string, int> BreaksBy = new();
        /// <summary>학점제 학년 — 판이 끝날 때 학년 · 누적 학점, 마지막(2층) 보스 싸움을 열 때 학년(못 닿았으면 0).</summary>
        public int Grade, Credits, GradeLastBoss, CreditsLastBoss;
    }

    /// <summary>
    /// 한 판을 처음부터 끝까지 화면 없이(웹판 tools/lib/run-bot.js) — 지도 · 싸움 · 이벤트 · 캠프 · 상점 · 장비.
    ///   simple — 지도는 첫 갈래, 캠프는 늘 쉬기, 상점은 지나감, 이벤트는 첫 선택지, 장비는 빈 칸에만
    ///   smart  — 지도는 HP · 골드로 길 전체를 따지고, 캠프는 HP 가 낮으면 쉬고 아니면 수련, 상점은 기본 카드 빼기 · 쓸 만한 장비 · 교주 카드,
    ///            이벤트는 결과의 값어치로, 장비는 나아질 때 바꿔 낀다
    /// 난수는 판과 전투의 것만 쓴다 — 같은 씨앗이면 같은 판.
    /// </summary>
    public sealed partial class RunBot
    {
        readonly GameData data;
        readonly Bots bots;
        const int K = R.SCALE;
        /// <summary>장비 효과 한 싸움 값어치 → 장비 점수(옛 장비 효과 평균 값어치 0.93 이 옛 고정값 4 와 같게) · 한 효과의 위 끝.</summary>
        public const double GEAR_EFF = 4.3, GEAR_EFF_CAP = 12;
        public RunBot(GameData data) { this.data = data; bots = new Bots(data); }
        /// <summary>숙련 봇의 판 짜기 눈(판마다 새로) — 초보 봇이면 null.</summary>
        DeckPlan plan;

        double HpRatio(Run run) => (double)run.S.PartyHp / Math.Max(1, run.S.PartyMaxHp);
        double DeckEff(Run run, string id) => CardValue.Efficiency(run.ViewOf(id));
        double CardEff(string id, int n = 0) => CardValue.Efficiency(data.View(id, n));

        // ── 교주 카드 주인 ──
        /// <summary>
        /// 교주 카드를 누구 덱에 넣을까 — 피해 카드는 공격력이 가장 높은 사도, 실드 · 방어 · 회복 카드는 방어력이 가장 높은 사도,
        /// 그 밖(버프 · 드로우 · AP)은 서포터, 없으면 덱에 카드가 가장 적은 사도(장비 · 성장 포함 능력치로 잰다).
        /// </summary>
        public string PickOwner(Run run, string cardId)
        {
            var c = data.View(cardId);
            var party = run.S.Party;
            if (c == null || party.Count == 0) return party.FirstOrDefault();
            var gear = run.GearStats();
            Stats Of(string k) { var h = data.Hero(k); var s = new Stats { Atk = h?.Atk ?? 0, Def = h?.Def ?? 0 }; if (gear.TryGetValue(k, out var g)) s = s + g; if (run.S.Growth.TryGetValue(k, out var gr)) s = s + gr; return s; }
            bool Has(params string[] ks) => c.Fx.Any(f => ks.Contains(f.K));
            if (Has(FxK.Dmg, FxK.Extra)) return party.OrderByDescending(k => Of(k).Atk).First();
            if (Has(FxK.Shield, FxK.Block, FxK.Heal)) return party.OrderByDescending(k => Of(k).Def).First();
            var sup = party.FirstOrDefault(k => data.Hero(k)?.Role == "서포터");
            if (sup != null) return sup;
            return party.OrderBy(k => run.S.Deck.Count(id => run.ViewOf(id).Hero == k)).First();
        }

        /// <summary>주인을 기다리는 교주 카드를 모두 넣는다.</summary>
        void SettleNeutrals(Run run)
        {
            int guard = 0;
            while (run.PendingNeutral != null && guard++ < 20) run.AssignNeutral(PickOwner(run, run.PendingNeutral));
        }

        // ── 장비 ──
        public double GearScore(Run run, string id, string k)
        {
            if (id == null) return 0;
            var e = data.Equip(id); var s = run.StatsOf(id, k); var role = data.Hero(k)?.Role;
            double atkW = role == "딜러" ? 3 : role == "서포터" ? 1.8 : 1.4;
            double v = (s.Hp * 0.3 + s.Atk * atkW + s.Def * (role == "탱커" ? 2.2 : role == "서포터" ? 2 : 1)) / K + s.Crit * 0.25;
            // 효과 몫 — 한 싸움 값어치(CardValue.GearWorth, 1코 카드 ≈ 1.5) × GEAR_EFF. 옛 「효과가 있으면 +4 · 애착 +5」 를 효과 크기로(2026-10-07)
            if (e.Effect.Count > 0) v += Math.Min(GEAR_EFF_CAP, Math.Max(1, GEAR_EFF * CardValue.GearWorth(e.Effect)));
            if (e.Affinity == k && e.AffinityEffect.Count > 0) v += Math.Min(GEAR_EFF_CAP, Math.Max(2, GEAR_EFF * CardValue.GearWorth(e.AffinityEffect)));
            if (plan != null) v += 1.5 * plan.GearHits(e);   // 숙련 — 파티 축에 맞는 효과
            return v;
        }

        void ManageGear(Run run, bool smart)
        {
            foreach (var id in run.S.Bag.ToList())
            {
                var e = data.Equip(id); if (e == null) continue;
                string best = null; double gain = smart ? 0.5 : -1e9;
                foreach (var k in run.S.Party)
                {
                    run.GearOf(k).TryGetValue(e.Slot, out var old);
                    if (!smart) { if (old == null) { best = k; break; } continue; }
                    double g = GearScore(run, id, k) - GearScore(run, old, k) - (old != null ? 0 : -1);
                    if (g > gain) { gain = g; best = k; }
                }
                if (best == null && run.IsBought(id)) best = run.S.Party[0];
                if (best != null) run.Equip(best, id, replace: true); else run.SellEquip(id);
            }
        }

        // ── 싸움 ──
        string KindOf(Run run) => run.S.EventFight != null ? (run.S.EventFight.Elite ? "eventElite" : "event") : run.IsBoss ? "boss" : run.S.Elite ? "elite" : "fight";

        bool Fight(Run run, SimOpts P, SimResult outr)
        {
            string kind = $"{run.S.Floor + 1}:{KindOf(run)}";
            if (run.IsBoss && run.IsLastFloor && run.S.EventFight == null) { outr.GradeLastBoss = run.Grade; outr.CreditsLastBoss = run.S.Credits; }
            var (st, loot) = run.OpenFight(P.Hpx, P.Dmgx);
            var r = RngOf((uint)(run.S.Seed * 31 + run.S.Step));
            P.OnFight?.Invoke(st, run);
            int t = 0;
            while (st.Over == null && t++ < 40)
            {
                if (P.SmartFight) bots.SmartPlay(st, P.Depth, P.Width);
                else bots.SimplePlay(st, r);
                if (st.Over == null) st.EndTurn();
            }
            if (st.Over == null) st.Over = "lose";   // 40턴을 넘기면 진 것으로 친다
            outr.Fights++; outr.Turns += st.Turn;
            outr.Kinds.TryGetValue(kind, out var kk);
            outr.Kinds[kind] = (kk.n + 1, kk.turns + st.Turn, kk.win + (st.Over == "win" ? 1 : 0));
            outr.ToughHits += st.ToughHits; outr.ToughWeakHits += st.ToughWeakHits; outr.Breaks += st.Breaks;
            outr.BreaksBy.TryGetValue(kind, out var kb); outr.BreaksBy[kind] = kb + st.Breaks;
            run.AfterFight(st);
            SettleNeutrals(run);
            if (st.Over != "win") return false;
            if (plan != null) ClaimLeftover(run, st);   // 숙련 — 빛났지만 안 낸 카드도 보상에서 받는다(화면의 보상 줄과 같다)
            if (loot != null)
            {
                if (loot.Equip != null && loot.Equip.Count > 0 && loot.EquipTaken == null) run.TakeEquip(loot.Equip[0]);
                run.TakeGold();
            }
            ManageGear(run, P.SmartOut);
            return true;
        }

        static Func<double> RngOf(uint seed)
        {
            uint a = seed;
            return () =>
            {
                a = unchecked(a + 0x6d2b79f5u); uint t = a;
                t = unchecked((t ^ (t >> 15)) * (t | 1u));
                t ^= unchecked(t + (t ^ (t >> 7)) * (t | 61u));
                return (t ^ (t >> 14)) / 4294967296.0;
            };
        }

        // ── 지도 ──
        string PickNode(Run run, bool smart)
        {
            var ids = run.Reachable();
            if (!smart || ids.Count == 1) return ids[0];
            var map = run.MapOf(); double h = HpRatio(run); int g = run.S.Gold;
            double pull = plan != null ? ShopPull(run) : 0;   // 숙련 — 기본 카드를 뺄 상점
            double Val(MapNode n) => n.Type switch
            {
                "fight" => h < 0.35 ? -2 : 3,
                "elite" => h >= 0.8 ? 8 : h >= 0.6 ? 2 : -10,
                "event" => 4,
                "camp" => h < 0.6 ? 9 : 4,
                "campshop" => (h < 0.6 ? 9 : 4) + (g >= 100 ? 5 : 1) + pull,
                _ => 0,
            };
            var memo = new Dictionary<string, double>();
            double Best(string id)
            {
                if (memo.TryGetValue(id, out var v)) return v;
                var n = Run.NodeById(map, id);
                v = Val(n) + (n.Next.Count > 0 ? Math.Max(0, n.Next.Max(Best)) : 0);
                memo[id] = v;
                return v;
            }
            string pick = ids[0]; double bv = -1e9;
            foreach (var id in ids) { double v = Best(id); if (v > bv) { bv = v; pick = id; } }
            return pick;
        }

        // ── 캠프 · 상점 ──
        void Camp(Run run, string kind, SimOpts P)
        {
            run.EnterCamp(kind);
            if (plan != null) { SkilledCamp(run, kind); return; }
            if (P.SmartOut)
            {
                if (kind == "campshop") Shop(run);
                double h = HpRatio(run);
                bool bossNext = run.Reachable().Any(id => Run.NodeById(run.MapOf(), id)?.Type == "boss");
                var t = run.S.Camp?.Train;
                int n = t != null ? t.Picks.OrderByDescending(x => CardEff(t.CardId, x)).First() : 0;
                if (t == null || h < (bossNext ? 0.75 : 0.55)) run.CampRest(); else run.CampTrain(n);
                ManageGear(run, true);
            }
            else { run.CampRest(); ManageGear(run, false); }
        }

        bool IsBasic(string id) { var c = data.Card(id); return c != null && c.Hero != null && !c.Unique && !GameData.IsCopy(id); }

        string WorstCard(Run run)
        {
            string w = null; double wv = 1e9;
            foreach (var id in run.S.Deck)
            {
                var c = data.Card(id);
                double v = DeckEff(run, id) + (c != null && c.IsCurse ? -10 : IsBasic(id) ? 0 : 5);
                if (v < wv) { wv = v; w = id; }
            }
            return w;
        }

        void Shop(Run run)
        {
            if (plan != null) { SkilledShop(run); return; }
            if (run.S.Shop == null || run.S.Shop.Floor != run.S.Floor || run.S.Shop.At != run.S.Map?.At) run.RollShop();
            var items = run.S.Shop.Items;
            double EqGain(string id) => run.S.Party.Max(k => { run.GearOf(k).TryGetValue(data.Equip(id).Slot, out var old); return GearScore(run, id, k) - GearScore(run, old, k); });
            var order = items.Select((it, i) => (it, i, v: it.Kind == "equip" ? EqGain(it.Id) / Math.Max(30, it.Price) * 10 : (CardEff(it.Id) - 0.9) * 2 / Math.Max(30, it.Price) * 100))
                .Where(x => !x.it.Sold).OrderByDescending(x => x.v).ToList();
            foreach (var x in order) if (x.it.Kind == "equip" && x.v > 0.5 && run.S.Gold >= x.it.Price) { run.Buy(x.i); ManageGear(run, true); }
            var w = WorstCard(run);
            if (w != null && run.S.Gold >= run.RemovePrice && (IsBasic(w) || data.Card(w).IsCurse ? run.S.Deck.Count > 6 : run.S.Deck.Count > 8 && DeckEff(run, w) < 1.0)) run.RemoveCard(w);
            foreach (var x in order) if (x.it.Kind == "neutral" && !x.it.Sold && CardEff(x.it.Id) >= 1.1 && run.S.Gold >= x.it.Price) run.Buy(x.i, PickOwner(run, x.it.Id));
        }

        // ── 이벤트 ──
        static readonly Dictionary<string, double> GRADE_V = new() { ["일반"] = 10, ["고급"] = 15, ["희귀"] = 22, ["전설"] = 30 };
        double OpsValue(Run run, List<Outcome> ops)
        {
            double v = 0;
            foreach (var o in ops ?? new List<Outcome>())
            {
                switch (o.K)
                {
                    case "gold": v += 0.2 * Math.Max(o.V, -run.S.Gold); break;
                    case "hp":
                        {
                            int d = Num.Round(run.S.PartyMaxHp * o.V);
                            if (d > 0) v += (double)Math.Min(d, run.S.PartyMaxHp - run.S.PartyHp) / K;
                            else v += d * (run.S.PartyHp + d < run.S.PartyMaxHp * 0.3 ? 2 : 1.1) / K;
                            break;
                        }
                    case "maxHp": v += o.V * 1.2 / K; break;
                    case "remove": v += plan != null ? RemoveWorth(run, o.Basic) * Math.Max(1, o.N) : run.S.Deck.Any(id => data.Card(id)?.IsCurse == true) ? 30 : run.S.Deck.Any(IsBasic) ? 22 : 8; break;
                    case "dupe": v += plan != null ? DupeWorth(run) : run.S.Deck.Any(id => !IsBasic(id) && data.Card(id)?.IsCurse == false) ? 16 : 4; break;
                    case "unique": v += plan != null ? (run.GraceHeroes().Count > 0 ? 26 * Math.Max(1, o.N) : 15) : 20; break;
                    case "neutral": v += (o.Grade == "전설" ? 20 : o.Grade == "희귀" ? 15 : 10) * (plan != null && run.S.Deck.Count > DeckPlan.THICK ? 0.5 : 1); break;
                    case "equip": v += GRADE_V.TryGetValue(o.Grade ?? "", out var gv) ? gv : 10; break;
                    case "flash": v += 14; break;
                    case "shin": v += 15 * o.V; break;
                    case "shinPick": v += 12 * Math.Max(1, o.N); break;
                    case "shinNow": v += 12; break;
                    case "noShin": v -= 2; break;
                    case "curse": v -= plan != null ? 30 : 25; break;
                    case "gift": v += 8; break;
                    case "scout": v += 2; break;
                    case "shopGift": v += (GRADE_V.TryGetValue(o.Grade ?? "", out var sg) ? sg : 10) * 0.8; break;
                    case "rewardFlash": v += 8; break;
                    case "next":
                        {
                            var n = o.Next ?? new NextFight();
                            v += 8 * n.Ap + 0.1 * n.Gauge + 4 * n.Hand - 6 * n.Weak + 3 * n.Rush + 4 * n.FoeVuln + 3 * n.Quiet + 4 * (n.Buff?.Values.Sum() ?? 0);
                            if (n.HpCut > 0) v -= n.HpCut * run.S.PartyMaxHp / K;
                            break;
                        }
                }
            }
            return v;
        }

        double OptValue(Run run, EventOption opt)
        {
            if (run.LockOf(opt) != null) return -1e9;
            if (opt.Fight != null)
            {
                double hpx = R.FoeScale(run.S.Floor, elite: opt.Fight.Elite).hp;
                var foes = opt.Fight.ByLand != null && opt.Fight.ByLand.TryGetValue(run.Land, out var bl) ? bl : opt.Fight.Enemies;
                double foeHp = foes.Sum(id => (data.Enemy(id)?.Hp ?? 400) * hpx);
                return OpsValue(run, opt.Fight.Win) + 10 - foeHp * 0.25 / K - (HpRatio(run) < 0.6 ? 60 : 0);
            }
            if (opt.Gamble != null)
            {
                var vs = opt.Gamble.Select(g => OpsValue(run, g.Out)).ToList();
                return opt.Choose ? vs.Max() : opt.Gamble.Select((g, i) => g.P * vs[i]).Sum();
            }
            if (opt.Judge != null) { var j = run.JudgeOf(opt); return OpsValue(run, j.Pass ? opt.Judge.Pass : opt.Judge.Fail); }
            return OpsValue(run, run.OutOf(opt));
        }

        void ResolvePending(Run run, bool smart)
        {
            int guard = 0;
            while (run.S.Event != null && run.S.Event.Pending.Count > 0 && guard++ < 30)
            {
                var p = run.S.Event.Pending[0];
                object val = null;
                if (plan != null) val = SkilledPending(run, p);
                else switch (p.K)
                {
                    case "remove": val = smart ? WorstCard(run) : run.S.Deck[0]; break;
                    case "dupe":
                        {
                            var ok = run.S.Deck.Distinct().Where(run.DupeOk).ToList();
                            val = smart ? ok.OrderByDescending(id => DeckEff(run, id)).FirstOrDefault() : ok.FirstOrDefault();
                            break;
                        }
                    case "card": val = smart ? p.Cards.OrderByDescending(id => CardEff(id)).First() : p.Cards[0]; break;
                    case "grace":
                        {
                            var hs = run.GraceHeroes();
                            val = smart ? hs.OrderByDescending(k => run.UniquesLeft(k).Max(id => (double?)CardEff(id)) ?? 0).FirstOrDefault() : hs.FirstOrDefault();
                            break;
                        }
                    case "flash": val = smart ? p.Offer.Picks.OrderByDescending(n => CardEff(p.Offer.CardId, n)).First() : p.Offer.Picks[0]; break;
                    case "gambleChoice":
                        {
                            int best = 0;
                            if (smart) best = Enumerable.Range(0, p.Options.Count).OrderByDescending(i => OpsValue(run, p.Options[i])).First();
                            val = best; break;
                        }
                    case "shinPick":
                        {
                            var able = run.ShinAble(p.Kind);
                            val = smart ? able.OrderByDescending(id => DeckEff(run, id)).FirstOrDefault() : able.FirstOrDefault();
                            break;
                        }
                }
                var why = run.ResolvePending(val, p.K == "card" && val is string cid ? PickOwner(run, cid) : null);
                if (why != null && run.S.Event.Pending.Count > 0) run.S.Event.Pending.RemoveAt(0);
            }
        }

        bool Event(Run run, SimOpts P, SimResult outr)
        {
            if (!run.EventLeft()) return true;
            run.EnterEvent();
            var E = run.S.Event;
            if (E.Id == null && E.Choices.Count > 0)
                run.PickEvent(P.SmartOut ? E.Choices.OrderByDescending(id => run.OptionsOf(data.Event(id)).Max(o => OptValue(run, o))).First() : E.Choices[0]);
            var ev = data.Event(run.S.Event.Id);
            if (ev != null && run.FlagHit(ev)) outr.FlagReads++;
            if (ev == null) { run.LeaveEvent(); return true; }
            var opts = run.OptionsOf(ev);
            int idx = 0;
            if (P.SmartOut) { double bv = -1e9; for (int i = 0; i < opts.Count; i++) { double v = OptValue(run, opts[i]); if (v > bv) { bv = v; idx = i; } } }
            else idx = Math.Max(0, opts.FindIndex(o => run.LockOf(o) == null));
            var (fight, _) = run.Choose(idx);
            if (fight)
            {
                bool won = Fight(run, P, outr);
                if (!won) { run.S.EventFight = null; return false; }
                run.AfterEventFight(true);
            }
            ResolvePending(run, P.SmartOut);
            SettleNeutrals(run);
            run.LeaveEvent();
            ManageGear(run, P.SmartOut);
            return true;
        }

        // ── 한 판 ──
        public SimResult RunFull(List<string> party, long seed, SimOpts P)
        {
            var run = Run.New(data, party, seed, P.Village);
            run.ApplyPerks(P.Perks);
            plan = P.Skilled ? new DeckPlan(data, party) : null;
            bots.EpiPick = plan != null ? SkilledEpi : null;
            if (P.UniqueOnly) { run.S.Deck.Clear(); run.S.Deck.AddRange(party.SelectMany(k => data.UniquesOf(k))); }   // 고유만 — 기본 카드 없이 시작
            var outr = new SimResult { Village = run.S.Village };
            int guard = 0;
            while (run.S.Done == null && guard++ < 200)
            {
                run.MapOf();
                var id = PickNode(run, P.SmartOut);
                var node = run.EnterNode(id);
                if (node == null) break;
                outr.Floor = run.S.Floor; outr.Where = node.Type;
                if (node.Type == "fight" || node.Type == "elite" || node.Type == "boss")
                {
                    if (!Fight(run, P, outr)) return Finish(run, outr);
                    run.S.Elite = false;
                    if (!run.IsBoss) continue;
                    var off = run.BossCopyOffer();
                    if (off.Count > 0) run.BossCopy(plan != null ? off.OrderByDescending(x => plan.Score(run.ViewOf(x))).First() : P.SmartOut ? off.OrderByDescending(x => DeckEff(run, x)).First() : off[0]);
                    run.Advance();
                    if (run.S.Done == "clear") break;
                }
                else if (node.Type == "event") { if (!Event(run, P, outr)) return Finish(run, outr); }
                else if (node.Type == "camp" || node.Type == "campshop") Camp(run, node.Type, P);
            }
            outr.Clear = run.S.Done == "clear";
            return Finish(run, outr);
        }

        static SimResult Finish(Run run, SimResult o)
        {
            o.Gold = run.S.Gold; o.Deck = run.S.Deck.Count;
            o.Uniques = run.S.Deck.Count(id => run.Data.Card(id)?.Unique == true);
            o.Basics = run.S.Deck.Count(run.IsBasic);
            o.Removals = run.S.Removals;
            o.Flags = run.S.Flags.ToList();
            o.Grade = run.Grade; o.Credits = run.S.Credits;
            return o;
        }
    }
}
