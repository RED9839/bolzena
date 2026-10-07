using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 숙련 봇(2026-10-06) — 의도한 플레이 「기본 카드를 거의 다 빼고 고유 카드로만 덱을 짜서 보스전」(SimOpts.Skilled).
    ///   · 빼기 기회(상점 · 이벤트)가 오면 저주 → 기본 카드 차례로 뺀다. 은총을 받을 수 있게 고유 카드가 남은 사도의 기본 카드는 한 장 남긴다.
    ///   · 카드 · 은총 · 신탁 · 축복 · 복제는 DeckPlan.Score(코스트당 값 + 파티 축 시너지 + 고유 · 강화 덤)로 고른다.
    ///   · 덱이 두꺼우면(DeckPlan.THICK 넘음) 교주 카드를 받는 문턱이 오르고, 문턱 아래면 받지 않는다(건너뛰기).
    ///   · 싸움을 이기면 빛났지만 안 낸 카드(은총 · 신탁)도 보상에서 받는다 — 화면의 보상 줄과 같다.
    ///   · 장비는 GearScore 에 파티 축에 맞는 효과 덤(RunBot.GearScore).
    /// 전투의 손(Bots.SmartPlay)은 초보와 같다 — 신탁 고르기만 SkilledEpi 로 바꿔 끼운다. 난수를 쓰지 않는다.
    /// </summary>
    public sealed partial class RunBot
    {
        /// <summary>덱이 이 장수 이하면 기본 카드도 더 빼지 않는다(저주 · 상태는 뺀다).</summary>
        public const int MIN_DECK = 8;
        /// <summary>신탁 후보에 축복이 얹혔으면 더하는 값.</summary>
        const double SHIN_BONUS = 0.25;

        double OracleScore(string cardId, int n, string shin) => plan.Score(data.View(cardId, n)) + (shin != null ? SHIN_BONUS : 0);

        /// <summary>전투 중 신탁 고르기(Bots.EpiPick).</summary>
        int SkilledEpi(Battle s, string id)
        {
            var g = s.GlowOf(id);
            if (g == null || g.Kind != "card" || g.Picks.Count == 0) return 0;
            int best = 0; double bv = double.MinValue;
            for (int i = 0; i < g.Picks.Count; i++) { double v = OracleScore(id, g.Picks[i].N, g.Picks[i].Shin); if (v > bv) { bv = v; best = i; } }
            return best;
        }

        /// <summary>이긴 싸움의 남은 빛(안 낸 은총 · 신탁) — 하나씩 가장 좋은 것을 받는다.</summary>
        void ClaimLeftover(Run run, Battle st)
        {
            foreach (var kv in st.Glow.OrderBy(x => x.Key, StringComparer.Ordinal).ToList())
            {
                var g = kv.Value;
                if (g == null || g.Count == 0) continue;
                int best = 0; double bv = double.MinValue;
                if (g.Kind == "card")
                {
                    var nk = GameData.NoInst(kv.Key);
                    if (!run.S.Deck.Any(x => GameData.NoInst(x) == nk)) continue;
                    for (int i = 0; i < g.Picks.Count; i++) { double v = OracleScore(kv.Key, g.Picks[i].N, g.Picks[i].Shin); if (v > bv) { bv = v; best = i; } }
                }
                else for (int i = 0; i < g.Options.Count; i++) { double v = plan.Score(data.View(g.Options[i])); if (v > bv) { bv = v; best = i; } }
                run.ClaimGlow(kv.Key, g, best);
            }
            SettleNeutrals(run);
        }

        // ── 빼기 ──────────────────────────────────────────────────────
        /// <summary>
        /// 뺄 카드 — 저주 · 상태 → 기본 카드(코스트당 값이 낮은 것부터, 고유 카드가 남은 사도는 은총 자리로 기본 카드 한 장을 남긴다) → (basicOnly 가 아니면) 축에 안 맞는 교주 카드.
        /// 덱이 MIN_DECK 이하면 저주 · 상태만. 뺄 것이 없으면 null.
        /// </summary>
        string RemovePick(Run run, bool basicOnly)
        {
            var deck = run.S.Deck;
            if (!basicOnly)
            {
                var bad = deck.Where(id => data.Card(id) is CardDef c && (c.IsCurse || c.IsStatusCard) && !run.ViewOf(id).IsTaboo).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                if (bad != null) return bad;
            }
            if (deck.Count <= MIN_DECK) return null;
            // 은총 자리 — 남은 고유 카드가 있는 사도마다 가장 좋은 기본 카드 한 장
            var keep = new List<string>();
            foreach (var k in run.S.Party)
            {
                if (run.UniquesLeft(k).Count == 0) continue;
                var b = deck.Where(id => run.IsBasic(id) && data.Card(id).Hero == k).OrderByDescending(id => DeckEff(run, id)).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                if (b != null) keep.Add(b);
            }
            var cands = deck.Where(run.IsBasic).ToList();
            foreach (var k in keep) cands.Remove(k);   // 한 장만 남긴다(같은 카드가 둘이면 하나는 뺄 수 있다)
            var w = cands.OrderBy(id => DeckEff(run, id)).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            if (w != null || basicOnly) return w;
            return deck.Where(id => data.Card(id)?.Neutral == true && !run.ViewOf(id).IsTaboo && plan.Score(run.ViewOf(id)) < 0.9)
                .OrderBy(id => plan.Score(run.ViewOf(id))).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
        }

        /// <summary>이벤트 「카드 빼기」 결과의 값.</summary>
        double RemoveWorth(Run run, bool basicOnly)
        {
            var w = RemovePick(run, basicOnly);
            if (w == null) return 0;
            var c = data.Card(w);
            return c.IsCurse || c.IsStatusCard ? 32 : run.IsBasic(w) ? 26 : 10;
        }

        /// <summary>이벤트 「복제」 결과의 값 — 복제할 가장 좋은 고유 카드의 점수로.</summary>
        double DupeWorth(Run run)
        {
            var ok = run.S.Deck.Distinct().Where(run.DupeOk).ToList();
            if (ok.Count == 0) return 0;
            double best = ok.Max(id => plan.Score(run.ViewOf(id)));
            return Math.Max(6, Math.Min(24, 14 + 10 * (best - 1.1)));
        }

        // ── 이벤트 고르기 ─────────────────────────────────────────────
        object SkilledPending(Run run, Pending p)
        {
            switch (p.K)
            {
                case "remove": return RemovePick(run, p.Basic);
                case "dupe":
                    return run.S.Deck.Distinct().Where(run.DupeOk).OrderByDescending(id => plan.Score(run.ViewOf(id))).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                case "card":
                    {
                        var best = p.Cards.Where(id => run.PowerWhy(id) == null).OrderByDescending(id => plan.Score(data.View(id))).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                        return best != null && plan.Score(data.View(best)) >= DeckPlan.TakeBar(run.S.Deck.Count) ? best : null;   // 덱에 안 맞으면 건너뛴다
                    }
                case "grace":
                    {
                        // 남은 고유 카드에서 무작위로 받는다 — 그 사도 남은 고유 카드 점수의 평균이 높은 사도
                        double Avg(string k) { var l = run.UniquesLeft(k).Where(x => run.PowerWhy(x) == null).ToList(); return l.Count == 0 ? double.MinValue : l.Average(x => plan.Score(data.View(x))); }
                        return run.GraceHeroes().OrderByDescending(Avg).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                    }
                case "flash":
                    {
                        var o = p.Offer; if (o == null || o.Picks.Count == 0) return null;
                        int bi = 0; double bv = double.MinValue;
                        for (int i = 0; i < o.Picks.Count; i++) { double v = OracleScore(o.CardId, o.Picks[i], o.Shins != null && i < o.Shins.Count ? o.Shins[i] : null); if (v > bv) { bv = v; bi = i; } }
                        return o.Picks[bi];
                    }
                case "gambleChoice":
                    return Enumerable.Range(0, p.Options.Count).OrderByDescending(i => OpsValue(run, p.Options[i])).First();
                case "shinPick":
                    // 축복은 자주 내는 카드에 — 덱에 든 장수 × 점수
                    return run.ShinAble(p.Kind).OrderByDescending(id => plan.Score(run.ViewOf(id)) * (1 + 0.3 * (run.S.Deck.Count(x => x == id) - 1))).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            }
            return null;
        }

        // ── 캠프 · 상점 ──────────────────────────────────────────────
        /// <summary>지도에서 상점 칸을 더 반기는 몫 — 빼기 값을 치를 골드가 있고 뺄 기본 카드 · 저주가 남았으면.</summary>
        double ShopPull(Run run) => run.S.Gold >= run.RemovePrice && RemovePick(run, false) != null ? 6 : 0;

        void SkilledCamp(Run run, string kind)
        {
            if (kind == "campshop") SkilledShop(run);
            double h = HpRatio(run);
            bool bossNext = run.Reachable().Any(id => Run.NodeById(run.MapOf(), id)?.Type == "boss");
            var t = run.S.Camp?.Train;
            int n = 0;
            if (t != null && t.Picks.Count > 0)
            {
                double bv = double.MinValue;
                for (int i = 0; i < t.Picks.Count; i++) { double v = OracleScore(t.CardId, t.Picks[i], t.Shins != null && i < t.Shins.Count ? t.Shins[i] : null); if (v > bv) { bv = v; n = t.Picks[i]; } }
            }
            if (t == null || h < (bossNext ? 0.75 : 0.55)) run.CampRest(); else run.CampTrain(n);
            ManageGear(run, true);
        }

        /// <summary>상점 — 빼기(기본 카드 · 저주)를 먼저, 그다음 쓸 만한 장비, 남은 골드로 축에 맞는 교주 카드(덱이 얇을 때만).</summary>
        void SkilledShop(Run run)
        {
            if (run.S.Shop == null || run.S.Shop.Floor != run.S.Floor || run.S.Shop.At != run.S.Map?.At) run.RollShop();
            var w = RemovePick(run, false);
            if (w != null && run.S.Gold >= run.RemovePrice) run.RemoveCard(w);
            var items = run.S.Shop.Items;
            double EqGain(string id) => run.S.Party.Max(k => { run.GearOf(k).TryGetValue(data.Equip(id).Slot, out var old); return GearScore(run, id, k) - GearScore(run, old, k); });
            var eq = items.Select((it, i) => (it, i)).Where(x => x.it.Kind == "equip" && !x.it.Sold).Select(x => (x.it, x.i, v: EqGain(x.it.Id) / Math.Max(30, x.it.Price) * 10)).OrderByDescending(x => x.v).ToList();
            foreach (var x in eq) if (x.v > 0.5 && !x.it.Sold && run.S.Gold >= x.it.Price) { run.Buy(x.i); ManageGear(run, true); }
            var cards = items.Select((it, i) => (it, i)).Where(x => x.it.Kind == "neutral" && !x.it.Sold).Select(x => (x.it, x.i, v: plan.Score(data.View(x.it.Id)))).OrderByDescending(x => x.v).ToList();
            foreach (var x in cards)
                if (!x.it.Sold && x.v >= DeckPlan.TakeBar(run.S.Deck.Count) + 0.15 && run.S.Gold >= x.it.Price) run.Buy(x.i, PickOwner(run, x.it.Id));
        }
    }
}
