using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>판정 — 결과를 미리 보여 주고 고르게 한다.</summary>
    public sealed class JudgeResult { public bool Pass; public string Who; public int Value, Need; public bool Hp; }

    /// <summary>
    /// 이벤트 — 뽑기 · 선택지 조건 · 결과 적용 · 고르는 것(제거 · 복제 · 카드 · 신탁 · 축복)을 하나씩.
    /// 화면: EnterEvent → (PickEvent) → OptionsOf · LockOf · JudgeOf 로 보여 주고 → Choose → 싸움이면 OpenFight … AfterEventFight →
    /// Pending[0] 을 보여 주고 ResolvePending … → LeaveEvent.
    /// </summary>
    public sealed partial class Run
    {
        /// <summary>이 판 이 층의 이벤트인가 — 공용 · 이 판 마을(id) · 이 층의 땅(옛 꼴). floor 가 있으면 그 층에서만.</summary>
        bool Local(EventDef ev) => ev.Pool == S.Village || ev.Pool == Land;
        bool Eligible(EventDef ev) => !S.EventsSeen.Contains(ev.Id) && (ev.Floor <= 0 || ev.Floor == S.Floor + 1) && (ev.Pool == "공용" || Local(ev))
                                      && (string.IsNullOrEmpty(ev.NeedFlag) || S.Flags.Contains(ev.NeedFlag));

        /// <summary>연속 이벤트 깃발이 서 있는가.</summary>
        public bool HasFlag(string id) => !string.IsNullOrEmpty(id) && S.Flags.Contains(id);

        public bool EventLeft() => Data.Events.Any(Eligible);

        static bool HasRemove(EventDef e) => e.Options.Any(o => o.Out.Any(x => x.K == "remove") || (o.Gamble?.Any(g => g.Out.Any(x => x.K == "remove")) ?? false));

        /// <summary>줄기 뒤 이벤트인데 그 깃발이 서 있다 — needFlag 가 충족됐거나, 선택지의 flag 깃발이 하나라도 서 있다.</summary>
        public bool FlagHit(EventDef e) => (!string.IsNullOrEmpty(e.NeedFlag) && HasFlag(e.NeedFlag)) || e.Options.Any(o => o.Flag != null && HasFlag(o.Flag));

        /// <summary>마을 풀 70% · 공용 30%(한쪽이 비면 다른 쪽). 카드 제거가 있는 이벤트는 무게 ×5, 깃발이 선 줄기 뒤 이벤트는 ×flagWeight(기본 3). 한 번 나온 이벤트는 그 판에서 다시 안 나온다.</summary>
        public List<EventDef> RollEvents(int n = 1)
        {
            var outs = new List<EventDef>();
            for (int i = 0; i < n; i++)
            {
                var left = Data.Events.Where(e => Eligible(e) && !outs.Contains(e) && (e.Rare <= 0 || Rnd() < e.Rare)).ToList();
                var floorPool = left.Where(Local).ToList();
                var common = left.Where(e => e.Pool == "공용").ToList();
                var from = floorPool.Count == 0 ? common : common.Count == 0 ? floorPool : Rnd() < R.EVENT_FLOOR_SHARE ? floorPool : common;
                if (from.Count == 0) break;
                var w = from.Select(e => (HasRemove(e) ? R.EVENT_REMOVE_WEIGHT : 1.0) * (FlagHit(e) ? (e.FlagWeight > 0 ? e.FlagWeight : R.EVENT_FLAG_WEIGHT) : 1.0)).ToList();
                double x = Rnd() * w.Sum(); int k = 0;
                while (k < from.Count - 1 && (x -= w[k]) >= 0) k++;
                outs.Add(from[k]);
            }
            return outs;
        }

        /// <summary>이벤트 칸에 든다 — 들어올 때 한 번만 굴린다. 「지도 공개」 가 있으면 둘 중 고른다.</summary>
        public EventState EnterEvent()
        {
            string key = $"{S.Floor}:{(S.Map?.At ?? S.Node.ToString())}";
            if (S.Event == null || S.Event.Key != key)
            {
                var evs = RollEvents(S.Scout ? 2 : 1);
                S.Event = new EventState { Key = key, Choices = evs.Select(e => e.Id).ToList(), Id = evs.Count == 1 ? evs[0].Id : null };
                if (S.Scout && evs.Count > 1) S.Scout = false;
                // 뜬 이벤트는 바로 「나왔다」 — 고르기 전에 판을 저장 · 다시 열어도 같은 판에 또 나오지 않게
                if (S.Event.Id != null && !S.EventsSeen.Contains(S.Event.Id)) S.EventsSeen.Add(S.Event.Id);
            }
            return S.Event;
        }

        public string PickEvent(string id)
        {
            if (S.Event == null || !S.Event.Choices.Contains(id)) return "고를 수 없습니다";
            S.Event.Id = id;
            if (!S.EventsSeen.Contains(id)) S.EventsSeen.Add(id);
            return null;
        }

        bool HasHero(string id) => S.Party.Contains(id);
        double HpRatio => (double)S.PartyHp / Math.Max(1, S.PartyMaxHp);

        /// <summary>
        /// 이 선택지의 조건(hero · race · when · flag · noFlag)이 안 맞는 까닭 — 맞으면 null.
        /// 화면은 잠긴 칸에 이 글을 보인다(LockOf 가 맨 먼저 돌려준다).
        /// </summary>
        public string CondWhy(EventOption o)
        {
            if (o == null || o.Leave) return null;
            if (o.Flag != null && !HasFlag(o.Flag)) return o.LockText ?? "앞선 일이 있어야 합니다";
            if (o.NoFlag != null && HasFlag(o.NoFlag)) return o.LockText ?? "이미 지나간 일입니다";
            if (o.Hero != null && o.Hero.Count > 0 && !o.Hero.Any(HasHero))
            {
                var names = o.Hero.Select(k => Data.Hero(k)?.Name ?? k).Distinct().ToList();
                return o.LockText ?? (names.Count == 1 ? $"{Ko.J(names[0], "이가")} 파티에 있어야 합니다" : $"{string.Join(" · ", names)} 가운데 한 명이 파티에 있어야 합니다");
            }
            if (o.Race != null && !S.Party.Any(k => Data.Hero(k)?.Race == o.Race)) return o.LockText ?? $"{o.Race} 사도가 파티에 있어야 합니다";
            if (o.When == "hp30" && HpRatio > 0.3) return o.LockText ?? "파티 HP 30% 이하일 때만 고를 수 있습니다";
            return null;
        }

        /// <summary>조건이 안 맞을 때 잠긴 칸으로 보이는가 — 기본: 사도 · 종족 · HP 조건은 보이고, 깃발 조건은 숨긴다.</summary>
        static bool ShowsLocked(EventOption o) => o.ShowLocked ?? (o.Flag == null && o.NoFlag == null);

        /// <summary>보이는 선택지 — 조건이 맞는 것 + 잠긴 칸으로 보이는 것(LockOf 가 조건 글을 돌려준다) + 맨 끝 「떠난다」.</summary>
        public List<EventOption> OptionsOf(EventDef ev)
        {
            var opts = ev.Options.Where(o => CondWhy(o) == null || ShowsLocked(o)).ToList();
            opts.Add(new EventOption { Label = ev.Leave ?? "떠나기", Out = ev.LeaveOut ?? new List<Outcome>(), Leave = true });
            return opts;
        }

        /// <summary>이 선택지가 실제로 무엇을 하는가(그 사도가 있으면 바뀌는 값).</summary>
        public List<Outcome> OutOf(EventOption o) => o.Price != null && HasHero(o.Price.Hero) ? o.Price.Out : o.Out;

        public JudgeResult JudgeOf(EventOption o)
        {
            var j = o.Judge;
            if (j == null) return null;
            if (j.By == "atk-max")
            {
                var top = S.Party.Select(k => (k, v: Data.Hero(k)?.Atk ?? 0)).OrderByDescending(x => x.v).FirstOrDefault();
                if (top.k == null) return new JudgeResult { Pass = false, Need = j.At };
                return new JudgeResult { Pass = top.v >= j.At, Who = top.k, Value = top.v, Need = j.At };
            }
            int pct = Num.Round(HpRatio * 100);
            return new JudgeResult { Pass = pct >= j.At, Value = pct, Need = j.At, Hp = true };
        }

        /// <summary>못 고르는 까닭 — 골드 · 뺄 카드 · 복제할 카드.</summary>
        public string LockOf(EventOption o)
        {
            if (o.Leave) return null;
            var cond = CondWhy(o);
            if (cond != null) return cond;
            var ops = OutOf(o);
            int cost = -(int)ops.Where(x => x.K == "gold" && x.V < 0).Sum(x => x.V);
            int need = Math.Max(cost, o.NeedGold);
            if (need > 0 && S.Gold < need) return $"골드가 모자랍니다 ({need} 필요)";
            var rm = ops.FirstOrDefault(x => x.K == "remove");
            if (rm != null && S.Deck.Count <= Math.Max(1, rm.N)) return "뺄 카드가 모자랍니다";
            if (rm != null && rm.Basic && !S.Deck.Any(IsBasic)) return "뺄 시작 카드가 없습니다";
            if (ops.Any(x => x.K == "dupe") && !S.Deck.Any(DupeOk)) return "복제할 고유 카드가 없습니다";
            if (ops.Any(x => x.K == "gift" && OnlyCard(x.Id) && HasCard(x.Id))) return "유일 — 이미 덱에 있는 카드입니다";
            if (MindBroken && ops.Any(x => x.K == "remove" || x.K == "dupe" || x.K == "unique" || x.K == "neutral" || x.K == "flash" || x.K == "gift")) return MIND;
            return null;
        }

        /// <summary>고른다. 돌려주는 것: 싸움이면 true(OpenFight 로 싸우고 AfterEventFight), 아니면 false(결과가 Event.Log · Pending 에).</summary>
        public (bool fight, string why) Choose(int idx)
        {
            var ev = Data.Event(S.Event?.Id);
            if (ev == null || S.Event.Phase != "choose") return (false, "고를 수 없습니다");
            var opts = OptionsOf(ev);
            if (idx < 0 || idx >= opts.Count) return (false, "없는 선택지입니다");
            var opt = opts[idx];
            var why = LockOf(opt);
            if (why != null) return (false, why);
            if (!S.EventsSeen.Contains(ev.Id)) S.EventsSeen.Add(ev.Id);
            var E = S.Event;
            E.Label = opt.Label;
            if (opt.Fight != null)
            {
                E.Phase = "fight";
                var foes = opt.Fight.ByLand != null && opt.Fight.ByLand.TryGetValue(Land, out var bl) ? bl : opt.Fight.ByLand != null && opt.Fight.Enemies.Count == 0 ? opt.Fight.ByLand.Values.First() : opt.Fight.Enemies;
                S.EventFight = new EventFightState { Name = opt.Fight.Name, Enemies = foes.ToList(), Win = opt.Fight.Win, Elite = opt.Fight.Elite };
                return (true, null);
            }
            var outs = OutOf(opt);
            string say = opt.Say;
            if (opt.Gamble != null && !opt.Choose)
            {
                double r = Rnd(); var g = opt.Gamble[opt.Gamble.Count - 1];
                foreach (var x in opt.Gamble) if ((r -= x.P) < 0) { g = x; break; }
                outs = g.Out; say = g.Say ?? say;
            }
            if (opt.Gamble != null && opt.Choose)
            {
                E.Phase = "result"; E.Say = say;
                E.Pending = new List<Pending> { new Pending { K = "gambleChoice", Options = opt.Gamble.Select(g => g.Out).ToList() } };
                return (false, null);
            }
            if (opt.Judge != null)
            {
                var j = JudgeOf(opt);
                E.Judged = j.Who;
                outs = j.Pass ? opt.Judge.Pass : opt.Judge.Fail;
                E.Log.Add(j.Hp ? $"파티 HP {j.Value}% ({j.Need}% 이상이면 성공) · {(j.Pass ? "성공" : "실패")}" : $"{Data.Hero(j.Who)?.Name} — 공격력 {j.Value} ({j.Need} 이상이면 성공) · {(j.Pass ? "성공" : "실패")}");
                if (j.Pass && opt.Judge.PassSay != null) say = opt.Judge.PassSay;
            }
            E.Phase = "result"; E.Say = say;
            ApplyOutcomes(outs);
            return (false, null);
        }

        /// <summary>결과를 적용한다. 바로 되는 것은 여기서, 고르는 것은 Pending 에 쌓는다.</summary>
        public void ApplyOutcomes(List<Outcome> ops)
        {
            var E = S.Event ??= new EventState { Phase = "result" };
            var tx = new CardText(Data);
            foreach (var o in ops ?? new List<Outcome>())
            {
                switch (o.K)
                {
                    case "none": break;
                    case "gold": S.Gold = Math.Max(0, S.Gold + (int)o.V); E.Log.Add(tx.Outcome(o)); break;
                    case "hp":
                        {
                            int max = Math.Max(1, S.PartyMaxHp);
                            S.PartyHp = Math.Max(1, Math.Min(max, S.PartyHp + Num.Round(max * o.V)));
                            E.Log.Add($"파티 {tx.Outcome(o)}");
                            break;
                        }
                    case "maxHp":
                        if (o.V < 0) { S.PartyMaxHp = Math.Max(1, S.PartyMaxHp + (int)o.V); S.PartyHp = Math.Min(S.PartyHp, S.PartyMaxHp); }
                        else { S.PartyMaxHp += (int)o.V; S.PartyHp += (int)o.V; }
                        E.Log.Add($"파티 {tx.Outcome(o)}");
                        break;
                    case "remove": for (int i = 0; i < Math.Max(1, o.N); i++) E.Pending.Add(new Pending { K = "remove", Basic = o.Basic }); break;
                    case "dupe": for (int i = 0; i < Math.Max(1, o.N); i++) E.Pending.Add(new Pending { K = "dupe" }); break;
                    case "unique":
                        {
                            // 이벤트 은총(2026-10-06 사용자) — 파티 셋 가운데 사도를 고르고(grace), 그 사도의 아직 없는 고유 카드에서 무작위 n 장
                            // 남은 고유 카드가 있는 사도가 아무도 없으면 대신 골드(GRACE_GOLD)
                            if (S.Party.Any(k => UniquesLeft(k).Count > 0)) E.Pending.Add(new Pending { K = "grace", N = Math.Max(1, o.N), Label = "은총" });
                            else { S.Gold += GRACE_GOLD; E.Log.Add($"파티 사도의 고유 카드는 이미 다 가졌습니다 — 대신 골드 +{GRACE_GOLD}"); }
                            break;
                        }
                    case "neutral":
                        {
                            var cards = NeutralOffer(o.Grade, 3);
                            if (cards.Count > 0) E.Pending.Add(new Pending { K = "card", Cards = cards, Label = tx.Outcome(o) });
                            break;
                        }
                    case "equip":
                        {
                            var ids = o.Slot != null ? OfferEquipSlot(o.Grade, o.Slot) : OfferEquip(new Dictionary<string, int> { [o.Grade] = 1 }, 1);
                            if (ids.Count > 0) { GainEquip(ids[0]); E.Log.Add($"장비 「{Data.Equip(ids[0]).Name}」({o.Grade}) — 끼거나 팝니다"); }
                            else E.Log.Add($"{o.Grade} {o.Slot ?? "장비"} — 이미 다 가졌습니다");
                            break;
                        }
                    case "flash":
                        {
                            var offer = OfferFlash();
                            if (o.Swap)
                            {
                                var had = S.Flash.Keys.Where(id => Data.Card(id)?.Oracles.Count == 5 && !GameData.IsCopy(id)).OrderBy(x => x, StringComparer.Ordinal).ToList();
                                if (had.Count > 0) { var id = had[RndInt(had.Count)]; offer = OfferOf(id, S.Flash[id], swap: true); }
                            }
                            // all(다섯 중 고르기)은 없앴다(2026-10-05 신탁 통일) — 늘 무작위 셋
                            if (offer != null) E.Pending.Add(new Pending { K = "flash", Offer = offer });
                            else E.Log.Add("신탁을 붙일 고유 카드가 없습니다 — 고유 카드를 먼저 얻으세요");
                            break;
                        }
                    case "shin": E.ShinChance += o.V; break;
                    case "noShin": S.NoShin = true; break;
                    case "shinPick":
                        {
                            if (ShinAble(o.Kind).Count == 0) { E.Log.Add("축복을 얹을 카드가 없습니다"); break; }
                            for (int i = 0; i < Math.Max(1, o.N); i++) E.Pending.Add(new Pending { K = "shinPick", Kind = o.Kind });
                            break;
                        }
                    case "shinNow":
                        {
                            var ids = S.Flash.Keys.Where(id => Data.Card(id) != null && !GameData.IsCopy(id) && S.Deck.Contains(id) && !S.Shin.ContainsKey(id)).OrderBy(x => x, StringComparer.Ordinal).ToList();
                            if (ids.Count > 0) { var id = ids[RndInt(ids.Count)]; S.Shin[id] = OwnRandom(Data.Card(id)) ?? "power"; E.Log.Add($"{R.DIVINE_NAME}! 「{Data.Card(id).Name}」"); }
                            else { E.ShinChance = 1; E.Log.Add("축복을 얹을 신탁이 아직 없습니다 — 이번에 고르는 신탁에 얹힙니다"); }
                            break;
                        }
                    case "curse": GainCard(o.Id); E.Log.Add($"{tx.Outcome(o)} — 덱에"); break;
                    case "gift":
                        if (OnlyCard(o.Id) && HasCard(o.Id)) { E.Log.Add($"「{Data.Card(o.Id).Name}」 — 유일, 이미 덱에 있습니다"); break; }
                        GainCard(o.Id); E.Log.Add($"「{Data.Card(o.Id).Name}」 — 덱에"); break;
                    case "mindBreak": S.MindBreak = Math.Max(S.MindBreak, Math.Max(1, o.N)); E.Log.Add($"정신 붕괴 — 다음 전투 {Math.Max(1, o.N)}번이 끝날 때까지 카드 얻기 · 신탁 · 제거를 할 수 없습니다"); break;
                    case "flag": if (!string.IsNullOrEmpty(o.Id)) S.Flags.Add(o.Id); break;
                    case "scout": S.Scout = true; E.Log.Add("지도 공개 — 다음 이벤트 칸에서 둘 중 하나를 고릅니다"); break;
                    case "shopGift": S.ShopGift = o.Grade; E.Log.Add($"다음 상점에서 {o.Grade} 장비 하나를 공짜로 받습니다"); break;
                    case "rewardFlash": S.RewardFlash = true; E.Log.Add("다음 전투에서 신탁이 꼭 뜹니다"); break;
                    case "next":
                        S.NextFight = (S.NextFight ?? new NextFight()).Merge(o.Next);
                        E.Log.Add(tx.Outcome(o));
                        break;
                    default: E.Log.Add($"(읽지 못한 결과: {o.K})"); break;
                }
            }
        }

        /// <summary>이벤트 은총을 받을 고유 카드가 하나도 남지 않았을 때 대신 주는 골드.</summary>
        public const int GRACE_GOLD = 75;

        /// <summary>이벤트 은총 — 고를 수 있는 사도(남은 고유 카드가 있는 파티 사도).</summary>
        public List<string> GraceHeroes() => S.Party.Where(k => UniquesLeft(k).Count > 0).ToList();

        List<string> NeutralOffer(string grade, int n)
        {
            var pool = Data.NeutralIds().Where(id => (grade == null || Data.Card(id).Grade == grade) && !(OnlyCard(id) && HasCard(id))).ToList();
            var outs = new List<string>();
            while (outs.Count < n && pool.Count > 0) { int i = RndInt(pool.Count); outs.Add(pool[i]); pool.RemoveAt(i); }
            return outs;
        }

        /// <summary>
        /// 고르는 것을 하나씩 푼다 — Pending[0] 에 대한 값(카드 id · 신탁 번호(int) · gambleChoice 번호(int)). value 가 null 이면 건너뛴다(받지 않는다).
        /// 못 고르면 까닭. heroKey — card 로 교주 카드를 받을 때 넣을 사도(없으면 주인 고르기 줄 → PendingNeutral · AssignNeutral).
        /// </summary>
        public string ResolvePending(object value, string heroKey = null)
        {
            var E = S.Event;
            var p = E?.Pending.FirstOrDefault();
            if (p == null) return "고를 것이 없습니다";
            string id = value as string;
            switch (p.K)
            {
                case "remove":
                    {
                        int i = S.Deck.IndexOf(id);
                        if (i < 0) return "덱에 없는 카드입니다";
                        if (p.Basic && !IsBasic(id)) return "시작 카드만 뺄 수 있습니다";
                        if (ViewOf(id).IsTaboo) return "금기 카드는 뺄 수 없습니다";
                        S.Deck.RemoveAt(i); ForgetCard(id);
                        E.Log.Add($"「{Data.Card(id).Name}」 — 덱에서 뺐습니다");
                        break;
                    }
                case "shinPick":
                    {
                        if (id == null) { E.Log.Add($"{R.DIVINE_NAME} — 받지 않았습니다"); break; }
                        if (!ShinAble(p.Kind).Contains(id)) return "축복을 얹을 수 없는 카드입니다";
                        var c = Data.Card(id);
                        var own = Enumerable.Range(0, c.Blesses.Count).Select(i => i == 0 ? "own" : "own" + i).ToList();
                        var pool = own.Count > 0 ? own : p.Kind != null ? new List<string> { p.Kind } : DivineKindsFor(ViewOf(id));
                        if (pool.Count == 0) return "축복을 얹을 수 없는 카드입니다";
                        S.Shin[id] = pool[RndInt(pool.Count)];
                        E.Log.Add($"{R.DIVINE_NAME}! 「{c.Name}」");
                        break;
                    }
                case "grace":
                    {
                        // value = 사도 키 — 그 사도의 남은 고유 카드에서 무작위 N 장(남은 만큼). null 이면 받지 않는다
                        if (id == null) { E.Log.Add($"{p.Label ?? "은총"} — 받지 않았습니다"); break; }
                        if (!S.Party.Contains(id)) return "파티에 없는 사도입니다";
                        var left = UniquesLeft(id).Where(x => PowerWhy(x) == null).OrderBy(x => x, StringComparer.Ordinal).ToList();
                        if (left.Count == 0) return "남은 고유 카드가 없는 사도입니다";
                        for (int i = 0; i < Math.Max(1, p.N) && left.Count > 0; i++)
                        {
                            int k = RndInt(left.Count); var cid = left[k]; left.RemoveAt(k);
                            GainCard(cid);
                            E.Log.Add($"은총 — {Data.Hero(id)?.Name ?? id} 「{Data.Card(cid).Name}」 — 덱에");
                        }
                        break;
                    }
                case "dupe":
                    {
                        if (!S.Deck.Contains(id)) return "덱에 없는 카드입니다";
                        if (!DupeOk(id)) return "복제는 사도 고유 카드만 됩니다(유일 · 금기 제외)";
                        AddCopy(id);
                        E.Log.Add($"「{Data.Card(id).Name}」 — 복제본 한 장 더");
                        break;
                    }
                case "card":
                    {
                        if (id == null) { E.Log.Add($"{p.Label} — 받지 않았습니다"); break; }
                        if (!p.Cards.Contains(id)) return "고를 수 없는 카드입니다";
                        var why = PowerWhy(id); if (why != null) return why;
                        GainCard(id, heroKey);
                        E.Log.Add($"「{Data.Card(id).Name}」 — 덱에");
                        break;
                    }
                case "flash":
                    {
                        if (value == null) { E.Log.Add("신탁 — 받지 않았습니다"); break; }
                        int n = Convert.ToInt32(value);
                        if (!p.Offer.Picks.Contains(n)) return "고를 수 없는 신탁입니다";
                        if (GameData.IsCopy(p.Offer.CardId)) return "복제본은 신탁 · 축복을 받을 수 없습니다";
                        if (!TakeOffer(p.Offer, n)) S.Flash[p.Offer.CardId] = n;
                        var c = Data.Card(p.Offer.CardId);
                        E.Log.Add($"「{c.Name}」 — 신탁 {n}");
                        if (E.ShinChance > 0 && !S.NoShin && Rnd() < E.ShinChance)
                        {
                            S.Shin[p.Offer.CardId] = OwnRandom(c) ?? "power";
                            E.Log.Add($"{R.DIVINE_NAME}! 신탁 위에 한 줄이 더");
                        }
                        break;
                    }
                case "gambleChoice":
                    {
                        int n = value == null ? -1 : Convert.ToInt32(value);
                        if (n < 0 || n >= p.Options.Count) return "고를 수 없습니다";
                        E.Pending.RemoveAt(0);
                        ApplyOutcomes(p.Options[n]);
                        return null;
                    }
            }
            E.Pending.RemoveAt(0);
            return null;
        }

        /// <summary>이벤트 싸움이 끝났다 — 이기면 적힌 보상.</summary>
        public void AfterEventFight(bool won)
        {
            var f = S.EventFight;
            var E = S.Event;
            S.EventFight = null;
            if (E == null) return;
            E.Phase = "result";
            if (!won || f == null) return;
            E.Log.Add($"{Ko.J(f.Name ?? "적", "을를")} 물리쳤습니다");
            ApplyOutcomes(f.Win);
        }

        /// <summary>축복을 얹을 수 있는 카드 — 덱의 카드 종류 가운데 축복이 아직 없는 것(저주 · 복제본 빼고).</summary>
        public List<string> ShinAble(string kind)
        {
            var ids = S.Deck.Distinct().Where(id => Data.Card(id) is CardDef c && !c.IsCurse && !c.IsStatusCard && !GameData.IsCopy(id) && !S.Shin.ContainsKey(id)).ToList();
            if (kind == "cost") return ids.Where(id => ViewOf(id).Cost >= 1).ToList();
            return kind != null ? ids : ids.Where(id => DivineKindsFor(ViewOf(id)).Count > 0).ToList();
        }

        string OwnRandom(CardDef c)
        {
            if (c.Blesses.Count == 0) return null;
            int i = RndInt(c.Blesses.Count);
            return i == 0 ? "own" : "own" + i;
        }

        /// <summary>복제 — 사도 고유 카드만. 유일 · 금기 · 복제본은 안 된다.</summary>
        public bool DupeOk(string id) { var c = Data.Card(id); return c != null && !GameData.IsCopy(id) && c.Hero != null && c.Unique && !ViewOf(id).IsOnly && !ViewOf(id).IsTaboo; }

        /// <summary>시작(기본) 카드 — 사도 카드이면서 고유 카드가 아니고 복제본도 아닌 것.</summary>
        public bool IsBasic(string id) { var c = Data.Card(id); return c != null && c.Hero != null && !c.Unique && !GameData.IsCopy(id) && !GameData.IsPlain(id); }

        public void LeaveEvent()
        {
            if (S.Event != null) S.EventDone.Add(S.Event.Key);
            S.Event = null;
        }
    }
}
