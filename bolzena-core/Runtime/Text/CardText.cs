using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 데이터 → 한국어 글. 해석기가 아니라 생성기다 — 카드 면 · 신탁 · 축복 · 패시브 · 키워드 · 고학년 · 적의 수 · 이벤트 결과 · 장비.
    /// 문법은 웹판 docs/18 §9 「베껴 쓰는 줄」 과 같은 꼴(「적 1명에게 공격력 120% 피해」 · 「연계.」 · 「파괴: …」).
    /// data 를 주면 카드 이름 · 키워드의 붙는 곳을 찾아 쓴다(없어도 된다).
    /// </summary>
    public sealed class CardText
    {
        readonly GameData data;
        public CardText(GameData data = null) { this.data = data; }

        static string P(double ratio) => Num.Round(ratio * 100).ToString(CultureInfo.InvariantCulture) + "%";
        static string N(double v) => v == Math.Floor(v) ? ((long)v).ToString(CultureInfo.InvariantCulture) : v.ToString("0.##", CultureInfo.InvariantCulture);
        static string Q(string id) => $"「{id}」";

        string CardName(string id) => data?.Card(id)?.Name ?? id;

        string Carrier(string kwId)
        {
            if (data == null || kwId == null) return null;
            foreach (var h in data.Heroes.Values) if (h.Keyword != null && h.Keyword.Name == kwId) return h.Keyword.Carrier ?? "self";
            return null;
        }

        // ── 대상 말 ───────────────────────────────────────────────────
        static string Who(string target, string fallback = "적 1명") => target switch
        {
            "oneEnemy" => "적 1명", "allEnemies" => "적 전체", "randomEnemy" => "무작위 적", "self" => "자신", "oneAlly" => "아군 1명",
            "otherEnemy" => "다른 적 1명", "nextEnemy" => "행동 카운트가 가장 작은 적", "slowestEnemy" => "행동 카운트가 가장 큰 적", "markedEnemy" => "표식이 가장 많은 적", "strongestAlly" => "공격력이 가장 높은 아군",
            "allAllies" => "아군 전원", "party" => "파티", "topEnemy" => "체력이 가장 높은 적", "lowEnemy" => "체력이 가장 낮은 적", null => fallback, _ => fallback,
        };
        static string WhoTo(string target) => target switch
        {
            "allEnemies" => "적 전체에", "randomEnemy" => "무작위 적에게", "otherEnemy" => "다른 적 1명에게", "nextEnemy" => "행동 카운트가 가장 작은 적에게", "slowestEnemy" => "행동 카운트가 가장 큰 적에게", "markedEnemy" => "표식이 가장 많은 적에게", "strongestAlly" => "공격력이 가장 높은 아군에게", "topEnemy" => "체력이 가장 높은 적에게", "lowEnemy" => "체력이 가장 낮은 적에게", "self" => "자신에게", "oneAlly" => "아군 1명에게", "allAllies" => "아군 전원에게", "party" => "파티에", _ => "적 1명에게",
        };

        // ── 카드 ───────────────────────────────────────────────────────
        /// <summary>카드 면의 글 — 태그 + 효과(신탁을 얹은 모습이면 신탁의 글).</summary>
        public string Card(CardView c) { choices = c.Def.Choices; try { return Head(c.Def) + Compose(c.Tags, c.Fx); } finally { choices = null; } }
        public string Card(CardDef c) { choices = c.Choices; try { return Head(c) + Compose(c.Tags, c.Fx); } finally { choices = null; } }
        List<string> choices;
        /// <summary>카드 머리의 덤 — 고유 효과로 치르는 비용 · AP 빚 · 두 갈래.</summary>
        static string Head(CardDef c)
        {
            var s = new List<string>();
            if (c.PayWith != null) s.Add($"비용은 {Q(c.PayWith)}로.");
            if (c.Debt) s.Add("AP 가 모자라도 낸다(모자란 만큼 다음 턴 AP -).");
            if (c.Choices != null && c.Choices.Count == 2) s.Add($"갈래 — {c.Choices[0]} / {c.Choices[1]}.");
            return s.Count == 0 ? "" : string.Join(" ", s) + " ";
        }
        public string Oracle(CardDef baseCard, OracleDef o)
        {
            var head = new List<string>();
            if (o.Power) head.Add("강화 카드.");
            if (o.Cost != null && o.Cost != baseCard.Cost) head.Add($"코스트 {o.Cost}.");
            choices = baseCard.Choices;
            string body;
            try { body = Compose(o.Tags, o.Fx); } finally { choices = null; }
            return string.Join(" ", head.Concat(new[] { body }).Where(s => s.Length > 0));
        }
        public string Bless(BlessDef b)
        {
            var parts = new List<string>();
            if (b.Kind != null && R.DIVINE_KO.TryGetValue(b.Kind, out var k)) parts.Add(k.Split('—').Last().Trim() + ".");
            var body = Compose(b.Tags, b.Fx);
            if (body.Length > 0) parts.Add(body);
            return string.Join(" ", parts);
        }
        public string Ult(UltDef u) => $"「{u.Name}」 (게이지 {u.Cost}%) {Fx(u.Fx)}";

        /// <summary>태그 + 효과.</summary>
        public string Compose(IEnumerable<string> tags, List<Fx> fx)
        {
            var head = string.Join(" ", (tags ?? Enumerable.Empty<string>()).Select(t => t + "."));
            var body = Fx(fx);
            return head.Length == 0 ? body : body.Length == 0 ? head : head + " " + body;
        }

        /// <summary>효과 조각 목록 → 한 줄(조건은 「파괴: …」 처럼 새 문장).</summary>
        public string Fx(List<Fx> fx)
        {
            if (fx == null || fx.Count == 0) return "";
            var sentences = new List<string>();
            var cur = new List<string>();
            string pending = null;    // 「「X」 1개당」 · 「리듬 1개당」 — 바로 뒤 한 줄의 머리
            string head = null;       // 문장의 머리(조건)
            string lastLead = null;   // 바로 앞 조각의 대상 머리(「적 1명 」 · 「이번 턴 자신의 」) — 같으면 「· 」 로 잇는다
            Fx prev = null;
            void Flush()
            {
                if (cur.Count == 0 && head == null) return;
                sentences.Add((head != null ? head + ": " : "") + string.Join(", ", cur));
                cur.Clear(); head = null; lastLead = null;
            }
            foreach (var f in fx)
            {
                string cond = CondHead(f);
                if (cond != null) { Flush(); head = cond; prev = f; continue; }
                if (f.K == FxK.PerStack) { pending = f.Each ? $"적마다 제 {Q(f.Id)} 1개당 " : $"{Q(f.Id)} 1개당 "; prev = f; continue; }
                if (f.K == FxK.PerRhythm) { pending = "리듬 1개당 "; prev = f; continue; }
                if (f.K == FxK.PerDiscarded) { pending = "이 카드로 버린 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPaid) { pending = $"이번 턴 치른 HP {(f.Per > 0 ? f.Per : 100)}당 "; prev = f; continue; }
                if (f.K == FxK.PerDebuff) { pending = "적 1명의 디버프 1가지당 "; prev = f; continue; }
                if (f.K == FxK.PerApLeft) { pending = "남은 AP 1당 "; prev = f; continue; }
                if (f.K == FxK.PerTag) { pending = $"손의 {Q(f.Id)} 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPlayed) { pending = f.Id != null ? $"이번 턴 낸 {Q(f.Id)} 카드 1장당 " : "이번 턴 낸 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPile) { pending = $"{PileKo(f.From)} 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerCardSt) { pending = $"이 카드의 {Q(f.Id)} 1당 "; prev = f; continue; }
                if (f.K == FxK.PerEvent) { pending = f.Per > 1 ? $"그 값 {f.Per}당 " : "그 값 1당 "; prev = f; continue; }
                var t = One(f, prev);
                if (t == null) { prev = f; continue; }
                string lead = Lead(f);
                if (lead != null && !t.StartsWith(lead)) lead = null;
                if (pending != null) { t = pending + t; pending = null; lead = null; }
                // 같은 대상에 잇단 상태 · 증감은 대상을 한 번만 — 「적 1명 고통 3 · 손상 1」
                if (lead != null && lead == lastLead && cur.Count > 0) cur[cur.Count - 1] += " · " + t.Substring(lead.Length);
                else cur.Add(t);
                lastLead = lead;
                prev = f;
            }
            Flush();
            return string.Join(". ", sentences);
        }

        /// <summary>대상 머리 — 적에게 거는 상태(「적 1명 」)와 능력치 증감(「이번 턴 자신의 」). 그 밖엔 null.</summary>
        static string Lead(Fx f)
        {
            switch (f.K)
            {
                case FxK.Status:
                    if (f.Id == R.STUN) return null;
                    bool foe = f.Target == "oneEnemy" || f.Target == "allEnemies" || f.Target == "randomEnemy" || (f.Target == null && R.IsBadSt(f.Id));
                    return foe ? Who(f.Target) + " " : null;
                case FxK.DealtMod:
                case FxK.TakenMod:
                case FxK.AtkMod:
                case FxK.DefMod:
                case FxK.CritMod:
                    {
                        string tg = f.Target ?? ((f.K == FxK.TakenMod && f.V > 0) || (f.K == FxK.DealtMod && f.V < 0) ? "oneEnemy" : "self");
                        return Dur(f) + Whose(tg);
                    }
                default: return null;
            }
        }

        string CondHead(Fx f) => f.K switch
        {
            FxK.IfBroken => "파괴",
            FxK.IfTune => "조율",
            FxK.IfChain => "연속",
            FxK.IfLink => "잇기",
            FxK.IfPrev => $"앞이 {f.Type}",
            FxK.IfRhythm => $"리듬이 {f.N} 이상이면",
            FxK.IfSwitched => "전환",
            FxK.IfStack => f.Not ? $"{Ko.J(Q(f.Id), "이가")} 없으면" : f.Max > 0 && f.Max == Math.Max(1, f.N) ? $"{Ko.J(Q(f.Id), "이가")} {f.Max}{(Ko.HasFinal(f.Max.ToString()) ? "이면" : "면")}" : f.Max > 0 ? $"{Ko.J(Q(f.Id), "이가")} {Math.Max(1, f.N)}~{f.Max}{(Ko.HasFinal(f.Max.ToString()) ? "이면" : "면")}" : f.N > 1 ? $"{Ko.J(Q(f.Id), "이가")} {f.N}개 이상이면" : $"{Ko.J(Q(f.Id), "이가")} 있으면",
            FxK.When => f.On switch { "draw" => "영감", "drawAny" => "감응", "discard" => "안식", "handEnd" => "턴 끝에 손에 있으면", "burn" => "소각", "passion" => "열정", _ => f.On },
            FxK.IfRepeat => "되풀이",
            FxK.IfHeld => $"손에서 {Math.Max(1, f.N)}턴 묵혔으면",
            FxK.IfPlayedMax => f.N == 0 ? "이번 턴 첫 장이면" : $"이번 턴 앞서 낸 카드가 {f.N}장 이하면",
            FxK.IfApLeft => $"AP가 {Math.Max(1, f.N)} 이상 남으면",
            FxK.IfSpent => $"이번 턴 쓴 AP가 꼭 {f.N}이면",
            FxK.IfBalanced => "공격과 스킬이 같은 장수면",
            FxK.IfHunted => "찍은 적이면",
            FxK.IfDebuffs => $"디버프가 {Math.Max(1, f.N)}가지 이상이면",
            FxK.IfHp => f.Not ? $"파티 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}%보다 많으면" : $"파티 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 이하이면",
            FxK.IfKill => "처치",
            FxK.IfBreak => "붕괴",
            FxK.IfWounded => f.Target == "oneEnemy" ? "적이 부상이면" : "부상",
            FxK.IfChoice => choices != null && f.N >= 1 && f.N <= choices.Count ? choices[f.N - 1] : $"갈래 {f.N}",
            FxK.IfRandom => $"{Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 확률로",
            FxK.IfHand => f.N <= 0 ? "손패가 없으면" : $"손패가 {f.N}장 이하면",
            FxK.IfPile => $"{PileKo(f.From)}가 {Math.Max(1, f.N)}장 이상이면",
            FxK.IfNth => $"이번 턴 {f.N}장째면",
            FxK.IfStreak => $"같은 사도의 카드를 {Math.Max(2, f.N)}장째 잇달아 내면",
            FxK.IfAllHeroes => "이번 턴 사도 모두가 카드를 냈으면",
            FxK.IfFoe => (f.Id switch { "broken" => "적이 격파 상태면", "tough" => $"적 강인도가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 이하면", "guarded" => "적에게 실드가 있으면", "attack" => "적이 공격하려 하면", _ => f.Id }) + (f.Not ? "(아니면)" : ""),
            FxK.IfRoll => $"{f.N}",
            FxK.IfPrevSame => f.Not ? "바로 앞 카드와 종류가 다르면" : "바로 앞 카드와 종류가 같으면",
            FxK.IfInHand => $"손에 다른 {Ko.J(Q(CardName(f.Id)), "이가")} 있으면",
            FxK.IfBond => $"결속 {f.N} 이상이면",
            FxK.IfLastMine => f.Not ? "마지막 카드가 다른 아군의 것이면" : "마지막 카드가 자신의 것이면",
            FxK.IfPulled => $"꺼낸 카드가 {Filt(f)}카드면",
            FxK.IfShield => f.Not ? "파티에 실드가 없으면" : "파티에 실드가 있으면",
            FxK.IfDebt => "AP 빚이 있으면",
            FxK.IfTypeNew => "그 종류가 이번 턴 처음이면",
            FxK.IfCardSt => f.Not ? $"이 카드의 {Ko.J(Q(f.Id), "이가")} 없으면" : $"이 카드의 {Ko.J(Q(f.Id), "이가")} {Math.Max(1, f.N)} 이상이면",
            _ => null,
        };

        /// <summary>카드 거르개 글 — 「자신의 고유 공격 」.</summary>
        string Filt(Fx f)
        {
            var s = new List<string>();
            if (f.Who == "self") s.Add("자신의"); else if (f.Who == "other") s.Add("다른 아군의"); else if (f.Who != null) s.Add((data?.Hero(f.Who)?.Name ?? f.Who) + "의");
            if (f.Basic) s.Add("시작"); if (f.Unique) s.Add("고유");
            if (f.Tag != null) s.Add(Q(f.Tag));
            if (f.Type != null && f.K != FxK.IfPrev) s.Add(f.Type);
            return s.Count == 0 ? "" : string.Join(" ", s) + " ";
        }
        static bool HasF(Fx f) => f.Who != null || f.Basic || f.Unique || f.Tag != null || f.Type != null;

        static string PileKo(string p) => p switch { "draw" => "뽑을 더미", "gone" => "소멸 더미", "hand" => "손패", _ => "버린 더미" };

        static string Dur(Fx f)
        {
            if (f.Run) return "전투 내내 ";
            int t = f.TurnsOr1;
            if (t >= 999) return "이번 전투 동안 ";
            return t > 1 ? $"{t}턴간 " : "이번 턴 ";
        }

        static readonly Dictionary<string, string> STAT_KO = new() { ["dealt"] = "주는 피해", ["taken"] = "받는 피해", ["atk"] = "공격력", ["def"] = "방어력", ["crit"] = "치명 확률", ["guard"] = "주는 실드" };

        /// <summary>대상 표기(Docs/키워드.md §0) — 표기 없음 = 파티 전원 · 「자신의」 = 카드 주인 · 「아군의」 = 자신을 뺀 사도.</summary>
        static string Whose(string tg) => tg switch
        {
            "self" => "자신의 ", "otherAllies" => "아군의 ", "allAllies" => "", "party" => "", "oneAlly" => "아군 1명의 ",
            "allEnemies" => "적 전체의 ", "randomEnemy" => "무작위 적의 ", "topEnemy" => "체력이 가장 높은 적의 ", "lowEnemy" => "체력이 가장 낮은 적의 ", _ => "적 1명의 ",
        };

        string One(Fx f, Fx prev)
        {
            switch (f.K)
            {
                case FxK.Dmg:
                    {
                        if (f.OfEvent > 0) return $"{WhoTo(f.Target)} 그 값의 {P(f.OfEvent)} 고정 피해";
                        string hits = f.XHits ? $"X회{(f.XStack != null ? $"(+{Q(f.XStack)} 수)" : "")} × " : f.HitsOr1 > 1 ? $"{f.HitsOr1}회 × " : "";
                        string what = f.Base == "def" ? $"방어 기반 피해 {P(f.Ratio)}" : f.Dot ? $"공격력 {P(f.Ratio)} 고정 지속 피해" : $"공격력 {P(f.Ratio)} {(f.Fixed ? "고정 " : "")}피해";
                        string to = f.Target == "randomEnemy" && hits.Length > 0 ? "무작위 적" : WhoTo(f.Target);
                        return $"{to} {hits}{what}";
                    }
                case FxK.Block:
                case FxK.Shield:
                    {
                        if (f.OfEvent > 0) return $"그 값의 {P(f.OfEvent)} 고정 실드";
                        // 표기 없음 = 파티(Docs/키워드.md §0) — 실드는 늘 파티 공용이라 대상을 적지 않는다
                        return $"방어력 {P(f.Ratio)} {(f.Fixed ? "고정 " : "")}{(f.K == FxK.Block ? "방어" : "실드")}";
                    }
                case FxK.Heal: return $"HP 회복(방어력 {P(f.Ratio)})";
                case FxK.Strip: return $"{Who(f.Target)}의 방어·실드 전부 파괴";
                case FxK.DealtMod:
                case FxK.TakenMod:
                case FxK.AtkMod:
                case FxK.DefMod:
                case FxK.CritMod:
                    {
                        string stat = FxK.ModStat(f.K);
                        string tg = f.Target ?? ((f.K == FxK.TakenMod && f.V > 0) || (f.K == FxK.DealtMod && f.V < 0) ? "oneEnemy" : "self");
                        int pct = Num.Round(f.V * 100);
                        return $"{Dur(f)}{Whose(tg)}{STAT_KO[stat]} {(pct >= 0 ? "+" : "")}{pct}%";
                    }
                case FxK.Draw: return HasF(f) ? $"{Filt(f)}카드 {f.IV}장 드로우" : $"드로우 {f.IV}";
                case FxK.HealMod: return $"{Dur(f)}{Whose(f.Target ?? "self")}치유 {(f.V >= 0 ? "+" : "")}{Num.Round(f.V * 100)}%";
                case FxK.Roll: return $"무작위로 {Math.Max(2, f.N)}갈래 가운데 하나";
                case FxK.Recast: return $"이 카드 효과를 {P(f.Ratio > 0 ? f.Ratio : 0.5)}로 한 번 더";
                case FxK.CostMod: return $"{(f.TurnsOr1 > 1 ? $"{f.TurnsOr1}턴간 " : "이번 턴 ")}{Filt(f)}카드{(f.N > 0 ? $" {f.N}장" : "")} 비용 {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.AddTag: return $"이번엔 {Q(f.Id)}";
                case FxK.CutHit: return $"적의 다음 공격 한 대 피해 -{P(f.V)}";
                case FxK.ClearDebt: return "AP 빚 탕감";
                case FxK.Ap: return $"AP {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.NextCheaper: return $"다음 카드 코스트 -{f.IV}";
                case FxK.Gauge: return $"고학년 게이지 {(f.IV >= 0 ? "+" : "")}{f.IV}%";
                case FxK.Discard: return f.All ? "손패 전부 버리기" : $"{(f.Random ? "무작위 " : "")}손패 {f.IV}장 버리기";
                case FxK.Make: return $"{Q(CardName(f.Id))} {Math.Max(1, f.IV)}장 생성{(f.To == "draw" ? "(뽑을 더미 무작위 자리)" : f.To == "top" ? "(뽑을 더미 맨 위)" : f.To == "discard" ? "(버린 더미)" : "")}";
                case FxK.Drain: return $"준 피해의 {P(f.Ratio)}만큼 HP 회복(최대 체력 20%까지)";
                case FxK.Extra: return $"{WhoTo(f.Target)} {(f.HitsOr1 > 1 ? $"{f.HitsOr1}회 × " : "")}추가 공격 {(f.Base == "def" ? "방어 기반 " : "")}{P(f.Ratio)}";
                case FxK.CardStatus:
                    {
                        string where = f.To == "hand" ? (f.N > 0 ? $"손의 {Filt(f)}카드 {f.N}장에 " : $"손의 {Filt(f)}카드 전부에 ") : f.To == "draw" ? (f.N > 0 ? $"뽑을 더미 {Filt(f)}{f.N}장에 " : "뽑을 더미 전부에 ") : f.To == "pulled" ? "그 카드에 " : "이 카드에 ";
                        return f.Id == "비용" ? $"{where}비용 {(f.IV >= 0 ? "+" : "")}{f.IV}" : R.IsCardSt(f.Id) ? $"{where}{f.Id} {f.IV}" : $"{where}{Q(f.Id)} {(f.IV >= 0 ? "+" : "")}{f.IV}";
                    }
                case FxK.Transform: return f.From == null ? $"이 카드를 {Q(CardName(f.Id))}로 바꾼다(이 전투)" : $"손의 {Q(CardName(f.From))}{(f.N > 1 ? $" {f.N}장" : "")}을 {Q(CardName(f.Id))}로 바꾼다(이 전투)";
                case FxK.Form: { var fd = data?.Form(f.Id); return fd == null ? $"변신 {Q(f.Id)}" : $"{Q(fd.Name)}{Ro(fd.Name)} 변신({FormDur(fd)})"; }
                case FxK.FormEnd: return "변신이 풀린다";
                case FxK.Later: return $"{Math.Max(1, f.N)}턴 뒤 턴 시작에: {Fx(f.Then)}";
                case FxK.AfterCards: return $"카드를 {Math.Max(1, f.N)}장 더 내면: {Fx(f.Then)}";
                case FxK.Trap: return $"{WhoTo(f.Target)} 함정 — 다음에 공격하면: {Fx(f.Then)}";
                case FxK.Confuse: return $"{Who(f.Target)} 혼란(다음 공격이 다른 적에게)";
                case FxK.AutoPlay: return $"손의 무작위 {Filt(f)}{(f.Id != null ? Q(f.Id) + " " : "")}카드 {f.NOr1}장이 저절로 나간다{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}";
                case FxK.CastOther: return $"손의 다른 사도 {(f.Id != null ? f.Id + " " : "")}카드 하나를 대신 발동{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}(카드는 손에 남는다)";
                case FxK.Pull: return $"{PileKo(f.From)} {(f.At == "bottom" ? "맨 아래" : f.At == "random" ? "무작위" : "맨 위")} {Filt(f)}카드 {f.NOr1}장을 {(f.To == "top" ? "뽑을 더미 맨 위로" : "손으로")}";
                case FxK.ExileFrom: return f.From == "pulled" ? "그 카드 소멸" : $"{PileKo(f.From)}에서 무작위 {Filt(f)}{f.NOr1}장 소멸";
                case FxK.Dispel: return $"{Who(f.Target)}의 이로운 효과 {f.NOr1Of(f.IV)} 지우기";
                case FxK.MoveRow: return $"{(f.Id == "front" ? "전열" : f.Id == "mid" ? "중열" : "후열")}로 옮긴다";
                case FxK.GrowRun: return $"{Whose(f.Target ?? "self")}{(f.Id == "def" ? "방어력" : f.Id == "crit" ? "치명" : "공격력")} +{f.IV} (판이 끝날 때까지)";
                case FxK.Tough:
                    {
                        bool after = prev != null && prev.K == FxK.Dmg && (prev.Target ?? "oneEnemy") == (f.Target ?? "oneEnemy");
                        return after ? $"강인도 피해 {N(f.V)}" : $"{Who(f.Target)} 강인도 피해 {N(f.V)}";
                    }
                case FxK.RushDown: return $"{Who(f.Target)} 즉시 행동 {f.IV}장 늦춤";
                case FxK.Status:
                    {
                        if (f.Id == R.STUN) return $"{Who(f.Target)} 기절";
                        bool foe = f.Target == "oneEnemy" || f.Target == "allEnemies" || f.Target == "randomEnemy" || (f.Target == null && R.IsBadSt(f.Id));
                        if (foe) return $"{Who(f.Target)} {f.Id} {f.IV}";
                        if (R.IsBadSt(f.Id)) return $"파티 {f.Id} {f.IV}";
                        // 파티 층(표기 없음 = 파티 전원) · 개인 층은 대상을 적는다
                        return f.Target != null && f.Target != "party" && f.Target != "auto" ? $"{WhoTo(f.Target)} {f.Id} {f.IV}" : R.IsHeroSt(f.Id) && f.Target == null ? $"자신에게 {f.Id} {f.IV}" : $"{f.Id} {f.IV}";
                    }
                case FxK.Cleanse: return $"파티 디버프 {Math.Max(1, f.IV)}개 해제";
                case FxK.Stack:
                    {
                        string c = Carrier(f.Id);
                        bool foe = c == "enemy" || (c == null && (f.Target == "oneEnemy" || f.Target == "allEnemies" || f.Target == "randomEnemy"));
                        return foe ? $"{WhoTo(f.Target ?? "oneEnemy")} {Q(f.Id)} +{f.IV}" : $"{Q(f.Id)} +{f.IV}";
                    }
                case FxK.Spend: return f.All ? $"{Q(f.Id)} 전부 소모" : $"{Q(f.Id)} {f.IV} 소모";
                case FxK.SpendRhythm: return f.All ? "리듬 전부 소모" : $"리듬 {f.IV} 소모";
                case FxK.Flip: return "전환";
                case FxK.Hasten: return $"재촉 {f.IV}";
                case FxK.PayHp: return $"파티 HP {f.IV} 소모";
                case FxK.Feed: return f.Target == "oneAlly" ? $"아군 1명의 키워드 +{f.IV}" : f.Target == "self" ? $"자신의 키워드 +{f.IV}" : $"아군 전원의 키워드 +{f.IV}";
                case FxK.NextAp: return $"다음 턴 AP {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.Burn: return f.All ? "손패 전부 소멸" : $"{(f.Random ? "무작위 " : "")}손패 {f.IV}장 소멸";
                case FxK.Reflect: return inRule ? $"그 피해의 {P(f.Ratio)}를 때린 적에게 고정 피해로" : $"지난 적의 차례에 받은 피해의 {P(f.Ratio)}를 {Who(f.Target)}에게 고정 피해로";
                case FxK.PayHpPct: return $"파티 최대 HP의 {P(f.V)} 만큼 HP 소모";
                default: return $"({f.K})";
            }
        }

        // ── 패시브 · 키워드 ───────────────────────────────────────────
        public string WhenText(When w)
        {
            switch (w.On)
            {
                case "fightStart": return "전투 시작 시";
                case "turnStart": return "턴 시작 시";
                case "turnEnd": return "턴 종료 시";
                case "play":
                    {
                        bool any = w.Who == "any";
                        if (w.Seq != null && w.Seq.Count > 0) return $"{(any ? "이번 턴 " : "자신의 ")}{string.Join(" · ", w.Seq)} 카드를 차례로 내면";
                        if (w.Nth > 0) return $"파티가 이번 턴 카드를 {w.Nth}장째 낼 때";
                        if (w.Sig) return "시그니처 카드를 내면";
                        if (w.Repeat) return $"{(any ? "" : "자신의 ")}같은 카드를 잇달아 내면";
                        // 「0코 이하」 → 「0코」, 「1코 이상 1코 이하」 → 「1코」
                        bool exact = w.MaxCost != null && (w.MaxCost == 0 || w.MinCost == w.MaxCost);
                        string type = (w.Tag != null ? Q(w.Tag) + " " : "") + (w.Type != null ? w.Type + " " : "") + (w.MaxCost != null ? $"{w.MaxCost}코{(exact ? "" : " 이하")} " : "");
                        int minCost = exact ? 0 : w.MinCost ?? 0;
                        if (w.Marked != null) return $"{Ko.J(Q(w.Marked), "이가")} 붙은 아군이 {type}카드를 낼 때마다";
                        if (w.Every > 0)
                            return $"{(any ? "파티가 " : "자신의 ")}{(w.PerTurn ? "한 턴에 " : "")}{(minCost > 1 ? $"{minCost}코 이상 " : "")}{type}카드를 {w.Every}장 낼 때마다";
                        return $"{(w.Who == "other" ? "다른 아군이 " : any ? "아군이 " : "")}{(minCost > 0 ? $"{minCost}코 이상 " : "")}{type}카드를 낼 때마다";
                    }
                case "guard": return w.Kind == "block" ? "방어를 얻으면" : w.Kind == "shield" ? "실드를 얻으면" : "방어나 실드를 얻으면";
                case "break": return w.Mine ? "적을 격파하면" : "적이 격파되면";
                case "kill": return w.Mine ? "적을 처치하면" : "적이 쓰러지면";
                case "hurt": return w.Pct > 0 ? $"한 번에 최대 HP {Num.Round(w.Pct * 100)}% 이상 잃으면" : w.Guarded ? "공격을 받으면(다 막아도)" : "피해를 받으면";
                case "endure": return "끈기로 버티면";
                case "unwound": return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}치유로 부상에서 벗어나면";
                case "lowHp": return $"HP가 {Num.Round(w.Pct * 100)}% 이하가 되면";
                case "rush": return "적이 즉시 행동하면";
                case "ult": return w.Who == "any" ? "아군이 고학년 스킬을 쓰면" : "고학년 스킬을 쓰면";
                case "debuff": return $"{(w.Who == "any" ? "아군이 " : "")}적에게 {(w.Fresh ? "새 디버프" : "디버프")}를 걸면";
                case "overheal": return w.Who == "any" ? "아군의 회복량이 최대 HP를 초과하면" : "회복량이 최대 HP를 초과하면";
                case "stackReach": return $"{Ko.J(Q(w.Id), "이가")} {w.N}개가 되면";
                case "stackGone": return w.Decay ? $"{Ko.J(Q(w.Id), "이가")} 다 닳으면" : $"{Ko.J(Q(w.Id), "이가")} 사라지면";
                case "reserveGone": return "아군의 예약이 다 닳으면";
                case "switch": return w.Who == "any" ? "아군이 전환하면" : "전환하면";
                case "rhythm": return $"리듬이 {w.N}{(Ko.HasFinal(w.N.ToString()) ? "이" : "가")} 되면";
                case "always": return "항상";
                case "discard": return w.Who == "any" ? "아군의 카드가 버려지면" : "자신의 카드가 버려지면";
                case "pay": return w.Who == "any" ? "아군이 HP를 치르면" : "HP를 치르면";
                case "exhaust": return $"{(w.Who == "any" ? "아군의" : "자신의")} {(w.Basic ? "시작 " : "")}카드가 소멸하면";
                case "huntDown": return w.Who == "any" ? "아군이 찍은 적이 쓰러지면" : "찍은 적이 쓰러지면";
                case "blocked": return "공격을 방어 · 실드로 다 막으면";
                case "spend": return $"{WhoSub(w, "아군이", "다른 아군이", "")}{Ko.J((w.Id != null ? Q(w.Id) : "키워드") + (w.N > 1 ? $" {w.N}개 이상" : ""), "을를")} 소모하면";
                case "link": return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}연계 카드가 저절로 나가면";
                case "crit": return $"{WhoSub(w, "아군이", "다른 아군이", "")}치명타를 내면";
                case "make": return $"{WhoSub(w, "아군이", "다른 아군이", "")}카드를 생성하면";
                case "drawn": if (w.Unique) return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}고유 카드가 뽑히면"; return w.Marked != null ? $"{Ko.J(Q(w.Marked), "이가")} 붙은 아군의 카드가 뽑히면" : $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}{(w.Tag != null ? Q(w.Tag) + " " : "")}{(w.Type != null ? w.Type + " " : "")}카드가 뽑히면";
                case "shuffle": return "뽑을 더미를 섞으면";
                case "extra": return $"{WhoSub(w, "아군이", "다른 아군이", "")}추가 공격하면";
                case "hit": return $"{WhoSub(w, "아군의 카드가", "다른 아군의 카드가", "자신의 카드가 ")}적을 {(w.Weak ? "약점으로 " : "")}치면".Replace("  ", " ");
                case "shieldBreak": return w.Mine ? "자신이 준 실드가 적의 공격에 깨지면" : "파티 실드가 적의 공격에 깨지면";
                case "foeShieldBreak": return w.Mine ? "적의 실드를 깨면" : "적의 실드가 깨지면";
                case "foeGuard": return "적이 실드를 얻으면";
                case "foeAct": return w.Type == "공격" ? "적이 공격하면(그 뒤)" : "적이 행동하면(그 뒤)";
                case "foeActBefore": return w.Type == "공격" ? "적이 공격하기 직전" : "적이 행동하기 직전";
                default: return w.On;
            }
        }

        static string WhoSub(When w, string any, string other, string self) => w.Who == "any" ? any + " " : w.Who == "other" ? other + " " : self;

        public string CondText(Cond c)
        {
            switch (c.C)
            {
                case "stack": return c.Not ? $"{Ko.J(Q(c.Id), "이가")} 없으면" : c.N > 1 ? $"{Ko.J(Q(c.Id), "이가")} {c.N}개 이상이면" : $"{Ko.J(Q(c.Id), "이가")} 있으면";
                case "hp": return $"HP가 {Num.Round(c.Pct * 100)}% 이하이면";
                case "hpMin": return $"HP가 {Num.Round(c.Pct * 100)}% 이상이면";
                case "status": return c.Id == R.RHYTHM ? $"리듬이 {c.N} 이상이면" : $"{(R.HERO_ST.Contains(c.Id) ? "자신" : "파티")} {Ko.J(c.Id, "이가")} {c.N} 이상이면";
                case "foes": return $"적이 {c.N}명 이상이면";
                case "foesMax": return $"적이 {c.N}명뿐이면";
                case "row": return $"{(c.Row == "front" ? "전열" : c.Row == "mid" ? "중열" : "후열")}에 서 있으면";
                case "playedMin": return $"파티가 이번 턴 카드를 {c.N}장 이상 냈으면";
                case "playedMax": return $"파티가 이번 턴 카드를 {c.N}장 이하로 냈으면";
                case "ownNone": return c.Type != null ? $"이번 턴 자신의 {c.Type} 카드를 내지 않았으면" : "이번 턴 자신의 카드를 내지 않았으면";
                case "guarded" when c.Not: return "실드가 없으면";
                case "apLeft": return c.N > 1 ? $"AP가 {c.N} 이상 남았으면" : "AP가 남았으면";
                case "gauge": return $"고학년 게이지가 {c.N}% 이상이면";
                case "guarded" when !c.Not: return c.Kind == "block" ? "방어가 있으면" : c.Kind == "shield" ? "실드가 있으면" : "방어나 실드가 있으면";
                case "rushed": return "적이 즉시 행동했으면";
                case "hurtLast": return "지난 턴에 피해를 받았으면";
                case "killedLast": return "지난 턴 적을 처치했으면";
                case "firstTurn": return "첫 턴이면";
                case "targetBroken": return "대상이 격파 상태이면";
                case "repeat": return "바로 앞에 낸 카드와 같은 카드면";
                case "held": return $"손에서 {Math.Max(1, c.N)}턴 묵힌 카드가 있으면";
                case "spent": return $"이번 턴 쓴 AP가 꼭 {c.N}이면";
                case "balanced": return "이번 턴 공격과 스킬을 같은 장수로 냈으면";
                case "debuffs": return $"디버프가 {Math.Max(1, c.N)}가지 이상인 적이 있으면";
                case "paid": return $"이번 턴 HP를 {Math.Max(1, c.N)} 이상 치렀으면";
                case "wounded": return "부상이면";
                case "onlyMe": return "이번 턴 낸 카드가 모두 자신의 것이면";
                case "ally": return $"파티에 {data?.Hero(c.Id)?.Name ?? c.Id}{(Ko.HasFinal(data?.Hero(c.Id)?.Name ?? c.Id) ? "이" : "가")} 있으면";
                case "inDebt": return "AP 빚이 있으면";
                case "heldCards": return $"손에 {(c.Id == null ? "자신의" : (data?.Hero(c.Id)?.Name ?? c.Id) + "의")} 카드가 {Math.Max(1, c.N)}장 이상이면";
                case "idleLast": return "지난 턴 카드를 못 낸 아군이 있으면";
                case "typeNew": return "그 종류가 이번 턴 처음이면";
                default: return c.C;
            }
        }

        /// <summary>규칙 한 줄(이름 빼고) — 「피해를 받으면 반격 1 (턴당 1회)」.</summary>
        public string Rule(PassiveRule r)
        {
            inRule = true;
            try { return RuleBody(r); } finally { inRule = false; }
        }
        bool inRule;

        string RuleBody(PassiveRule r) => RuleJoin(RuleHead(r), Fx(r.Fx), RuleLimit(r));

        /// <summary>규칙의 머리(계기 + 조건) · 꼬리(횟수 제한) — 머리 · 꼬리가 같은 잇단 규칙은 효과를 한 줄로 묶는다.</summary>
        string RuleHead(PassiveRule r)
        {
            var parts = new List<string>();
            if (r.When.On != "always" || r.Conds.Count == 0) parts.Add(WhenText(r.When));
            parts.AddRange(r.Conds.Select(CondText));
            return string.Join(" ", parts);
        }
        static string RuleLimit(PassiveRule r) => r.Limit != null ? $" ({(r.Limit.Per == "fight" ? "전투당" : "턴당")} {r.Limit.N}회)" : "";
        /// <summary>조건 머리 · 여러 문장이 없는 효과 글(「, 」 로 이어 붙여도 되는 것).</summary>
        static bool Plain(string fx) => fx.Length > 0 && !fx.Contains(": ") && !fx.Contains(". ");
        static string RuleJoin(string head, string fx, string limit) => (fx.Length > 0 ? (head.Length > 0 ? head + " " + fx : fx) : head) + limit;

        /// <summary>패시브 — 「이름: 규칙」, 같은 이름이 잇달면 한 이름 아래 문장으로(계기 · 조건 · 횟수까지 같으면 효과만 잇는다).</summary>
        public string Passives(List<PassiveRule> rules)
        {
            var outs = new List<string>();
            string last = null;
            inRule = true;
            try
            {
                foreach (var g in Group(rules, true, false))
                {
                    string body = RuleJoin(g.Head, g.Fx, g.Limit);
                    if (g.Name != null && g.Name == last) { outs[outs.Count - 1] += ". " + body; continue; }
                    outs.Add($"{g.Name ?? "패시브"}: {body}");
                    last = g.Name;
                }
            }
            finally { inRule = false; }
            return string.Join(" · ", outs);
        }

        /// <summary>잇단 규칙 묶음 — 이름 · 계기들 · 조건 · 효과 · 횟수.</summary>
        sealed class RuleGroup
        {
            public string Name, Conds, Fx, Limit;
            public List<string> Whens = new();
            public string Head => string.Join(" ", new[] { WhenJoin(Whens), Conds }.Where(x => !string.IsNullOrEmpty(x)));
            // 「적을 처치하면」 + 「적을 격파하면」 → 「적을 처치하거나 적을 격파하면」
            static string WhenJoin(List<string> ws) => ws.Count == 0 || ws[0] == null ? "" : string.Join("", ws.Take(ws.Count - 1).Select(w => w.Substring(0, w.Length - 1) + "거나 ")) + ws[ws.Count - 1];
        }

        /// <summary>
        /// 잇단 규칙을 묶는다(같은 이름끼리 — byName) — ① 계기 · 조건 · 횟수가 같으면 효과를 「, 」 로 잇고,
        /// ② 효과 · 조건이 같고 횟수 제한이 없으며 계기가 「…면」 이면 계기를 「…거나 …면」 으로 잇는다.
        /// </summary>
        List<RuleGroup> Group(IEnumerable<PassiveRule> rules, bool byName, bool alwaysWhen)
        {
            var o = new List<RuleGroup>();
            foreach (var r in rules)
            {
                string when = alwaysWhen || r.When.On != "always" || r.Conds.Count == 0 ? WhenText(r.When) : null;
                string conds = string.Join(" ", r.Conds.Select(CondText)), fx = Fx(r.Fx), limit = RuleLimit(r);
                var last = o.Count > 0 ? o[o.Count - 1] : null;
                if (last != null && (!byName || (r.Name != null && r.Name == last.Name)) && last.Conds == conds && last.Limit == limit)
                {
                    if (last.Whens.Count == 1 && last.Whens[0] == when && Plain(fx) && Plain(last.Fx)) { last.Fx += ", " + fx; continue; }
                    if (fx == last.Fx && limit.Length == 0 && fx.Length > 0 && when != null && when.EndsWith("면") && last.Whens.All(w => w != null && w.EndsWith("면"))) { last.Whens.Add(when); continue; }
                }
                var g = new RuleGroup { Name = r.Name, Conds = conds, Fx = fx, Limit = limit };
                g.Whens.Add(when);
                o.Add(g);
            }
            return o;
        }

        /// <summary>
        /// 키워드 자신의 규칙 한 줄 — 제 이름은 덜어 낸다:
        /// 「「원고」가 3개가 되면: 「원고」 전부 소모, 「연재 회차」 +1」 → 「3개가 되면 모두 써서: 「연재 회차」 +1」.
        /// </summary>
        string SelfRule(string name, RuleGroup g)
        {
            string head = g.Head, fx = g.Fx;
            if (name != null)
            {
                string q = Q(name), subj = Ko.J(q, "이가") + " ";
                if (head.StartsWith(subj) && head.Length > subj.Length && char.IsDigit(head[subj.Length]))
                {
                    head = head.Substring(subj.Length);
                    string all = q + " 전부 소모";
                    var m = System.Text.RegularExpressions.Regex.Match(fx, System.Text.RegularExpressions.Regex.Escape(q) + @" (\d+) 소모");
                    if (fx.Contains(all)) { head += " 모두 써서"; fx = Cut(fx, all); }
                    else if (m.Success) { head += $" {m.Groups[1].Value}개 써서"; fx = Cut(fx, m.Value); }
                }
                fx = fx.Replace(q + " 1개당 ", "1개당 ");
            }
            return $"{head}: {(fx.Length > 0 ? fx : "끝")}{g.Limit}";
        }
        static string Cut(string fx, string part)
        {
            if (fx.Contains(part + ", ")) return fx.Replace(part + ", ", "");
            if (fx.Contains(", " + part)) return fx.Replace(", " + part, "");
            return fx.Replace(part, "");
        }

        // ── 짧은 글 · 자세한 글 ───────────────────────────────────────
        // UI 는 기본으로 Short(한 줄 요약)을 보이고, 툴팁 · 상세 창을 펼치면 Detail(수치까지 정확한 규칙 글)을 보인다.

        /// <summary>고유 효과 한 줄 — 손으로 쓴 설명(desc), 없으면 자동 요약.</summary>
        public string Short(KeywordDef k)
        {
            if (!string.IsNullOrEmpty(k.Desc)) return k.Desc.TrimEnd('.');
            var s = new List<string>();
            foreach (var p in k.Per.Take(1))
                s.Add(p.Stat == "dot" ? $"1개당 턴 종료 시 공격력 {P(p.Ratio)} 피해" : p.Stat == "hot" ? $"1개당 턴 종료 시 HP 회복(방어력 {P(p.Ratio)})" : $"1개당 {STAT_KO[p.Stat]} {(p.V >= 0 ? "+" : "")}{Num.Round(p.V * 100)}%");
            if (s.Count == 0 && k.Rules.Count > 0) s.Add(Short(k.Rules[0]));
            if (k.Cap != null && !k.Mode) s.Add($"최대 {k.Cap}");
            return string.Join(" · ", s);
        }
        /// <summary>고유 효과 자세히 — 설명 + 최대치 · 1개당 · 규칙(= Keyword).</summary>
        public string Detail(KeywordDef k) => Keyword(k);

        /// <summary>패시브 규칙 한 줄 요약 — 계기 · 조건 + 핵심 효과(소모 · 횟수 제한 · 조건부 덤은 Detail 로).</summary>
        public string Short(PassiveRule r)
        {
            inRule = true;
            try { return RuleJoin(RuleHead(r), ShortFx(r.Fx, 22), ""); } finally { inRule = false; }
        }
        /// <summary>패시브 규칙 자세히(= Rule).</summary>
        public string Detail(PassiveRule r) => Rule(r);

        /// <summary>고학년 스킬 한 줄 요약 — 핵심 효과(이름 · 게이지는 화면이 따로 보인다).</summary>
        public string Short(UltDef u)
        {
            var s = ShortFx(u.Fx, 34);
            // 변신은 잘리지 않게 — 「… 등 · 「성전 모드」로 변신」
            foreach (var f in u.Fx.Where(x => x.K == FxK.Form))
            {
                var t = One(f, null);
                if (!s.Contains(t)) s += " · " + t;
            }
            return s;
        }
        /// <summary>고학년 스킬 자세히(= Ult).</summary>
        public string Detail(UltDef u) => Ult(u);

        // ── 변신 ───────────────────────────────────────────────────────
        static string Ro(string w)
        {
            if (!Ko.HasFinal(w)) return "로";
            var t = w.TrimEnd('」', ')', ' ');
            char c = t[t.Length - 1];
            return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 == 8 ? "로" : "으로";
        }
        static string FormDur(FormDef f) => f.Turns > 0 ? $"{f.Turns}턴" : "전투 끝까지";

        /// <summary>변신 자세히 — 설명 · 지속 · 풀리는 계기 · 그동안(능력치 · 카드 · 덤 · 패시브) · 풀릴 때.</summary>
        public string Form(FormDef f)
        {
            var s = new List<string>();
            if (!string.IsNullOrEmpty(f.Desc)) s.Add(f.Desc.TrimEnd('.'));
            s.Add(f.Turns > 0 ? $"{f.Turns}턴 지속" : "전투 끝까지");
            if (f.Until != null) s.Add($"{WhenText(f.Until).Replace("낼 때마다", "내면")} 풀린다");
            var during = FormDuring(f, true);
            if (during.Count > 0) s.Add("그동안 " + string.Join(" · ", during));
            if (f.Off != null && f.Off.Count > 0) s.Add($"풀릴 때: {Fx(f.Off)}");
            bool byMax = data != null && data.Heroes.Values.Any(h => h.AllKeywords.Any(k => k.OnMax?.Form == f.Id));
            s.Add(byMax ? "변신 중에 다시 최대가 되어도 그대로" : "다시 쓰면 지속이 처음으로");
            return string.Join(". ", s) + ".";
        }

        /// <summary>변신 한 줄 요약 — 설명(desc), 없으면 지속 + 바뀌는 것 앞부분.</summary>
        public string Short(FormDef f)
        {
            if (!string.IsNullOrEmpty(f.Desc)) return $"{f.Desc.TrimEnd('.')} ({FormDur(f)})";
            var d = FormDuring(f, false);
            return $"{FormDur(f)}{(d.Count > 0 ? " · " + string.Join(" · ", d.Take(2)) : "")}";
        }
        public string Detail(FormDef f) => Form(f);

        List<string> FormDuring(FormDef f, bool full)
        {
            var s = new List<string>();
            if (f.Mods != null)
                foreach (var kv in f.Mods)
                {
                    int pct = Num.Round(kv.Value * 100);
                    s.Add($"자신의 {(STAT_KO.TryGetValue(kv.Key, out var k) ? k : kv.Key == "heal" ? "치유" : kv.Key)} {(pct >= 0 ? "+" : "")}{pct}%");
                }
            if (f.Cards != null)
                foreach (var kv in f.Cards)
                {
                    var to = data?.Card(kv.Value);
                    s.Add(full && to != null ? $"{Q(CardName(kv.Key))} → {Q(to.Name)}({Card(to)})" : $"{Q(CardName(kv.Key))} → {Q(CardName(kv.Value))}");
                }
            foreach (var b in f.Bonus ?? new List<FormBonus>())
            {
                string who = b.Card != null ? Q(CardName(b.Card)) : $"자신의 {(b.Unique ? "고유 " : "")}{(b.Tag != null ? Q(b.Tag) + " " : "")}{(b.Type != null ? b.Type + " " : "")}카드";
                var add = new List<string>();
                if (b.Ratio > 0 && b.Ratio != 1) add.Add($"피해 ×{N(b.Ratio)}");
                foreach (var t in b.Tags ?? new List<string>()) add.Add($"{t}.");
                if (b.Fx != null && b.Fx.Count > 0) add.Add(Fx(b.Fx));
                if (add.Count > 0) s.Add($"{who}: {string.Join(" ", add)}");
            }
            if (f.Replace) s.Add("원래 패시브는 쉰다");
            if (f.Passives != null && f.Passives.Count > 0) s.Add(full ? Passives(f.Passives) : string.Join(" · ", f.Passives.Select(r => $"{r.Name ?? "패시브"}: {Short(r)}")));
            return s;
        }

        /// <summary>패시브 여럿 요약 — 「이름: 요약 · 이름: 요약」.</summary>
        public string ShortPassives(List<PassiveRule> rules)
        {
            var outs = new List<string>();
            string last = null;
            foreach (var r in rules)
            {
                if (r.Name != null && r.Name == last) { if (!outs[outs.Count - 1].EndsWith(" 등")) outs[outs.Count - 1] += " 등"; continue; }   // 같은 이름 둘째 규칙부터는 Detail 로
                outs.Add($"{r.Name ?? "패시브"}: {Short(r)}");
                last = r.Name;
            }
            return string.Join(" · ", outs);
        }

        static bool IsPer(string k) => k == FxK.PerStack || k == FxK.PerRhythm || k == FxK.PerDiscarded || k == FxK.PerPaid || k == FxK.PerDebuff || k == FxK.PerApLeft || k == FxK.PerTag || k == FxK.PerPlayed || k == FxK.PerPile || k == FxK.PerCardSt || k == FxK.PerEvent;

        /// <summary>효과 요약 — 조건 머리 앞까지, 소모는 빼고, 효과를 budget 글자까지 담고 남으면 「 등」.</summary>
        string ShortFx(List<Fx> fx, int budget)
        {
            if (fx == null || fx.Count == 0) return "";
            if (CondHead(fx[0]) != null) { var all = Fx(fx); int dot = all.IndexOf(". "); return dot > 0 ? all.Substring(0, dot) + " 등" : all; }
            var units = new List<List<Fx>>();
            var pend = new List<Fx>();
            foreach (var f in fx)
            {
                if (CondHead(f) != null) break;
                if (f.K == FxK.Spend || f.K == FxK.SpendRhythm || f.K == FxK.PayHp || f.K == FxK.PayHpPct) continue;   // 소모 · 치르는 값은 Detail 로
                pend.Add(f);
                if (IsPer(f.K)) continue;
                units.Add(pend); pend = new List<Fx>();
            }
            if (units.Count == 0) { var all = Fx(fx); int dot = all.IndexOf(". "); return dot > 0 ? all.Substring(0, dot) : all; }
            // 피해 · 실드 · 회복 같은 몸통 효과를 앞에(차례는 그대로 두고 첫 몸통만 끌어올린다)
            int main = units.FindIndex(u => u.Any(f => f.K == FxK.Dmg || f.K == FxK.Shield || f.K == FxK.Heal || f.K == FxK.Extra));
            if (main > 0) { var m = units[main]; units.RemoveAt(main); units.Insert(0, m); }
            var take = new List<Fx>(units[0]);
            int used = 1;
            for (; used < units.Count; used++)
            {
                var next = take.Concat(units[used]).ToList();
                if (Fx(next).Length > budget) break;
                take = next;
            }
            bool more = used < units.Count || fx.Any(f => CondHead(f) != null);
            return Fx(take) + (more ? " 등" : "");
        }

        public string Keyword(KeywordDef k)
        {
            var s = new List<string>();
            if (!string.IsNullOrEmpty(k.Desc)) s.Add(k.Desc.TrimEnd('.'));
            if (k.Carrier == "enemy") s.Add("적에게 건다");
            else if (k.Carrier == "ally") s.Add("아군에게 건다");
            else if (k.Carrier == "hero") s.Add("사도마다 따로 붙는다");
            if (k.Wrap && k.Cap != null) s.Add($"{k.Cap}{(Ko.HasFinal(k.Cap.ToString()) ? "을" : "를")} 넘으면 1 부터 다시");
            if (k.Guard) s.Add("적의 공격 한 대를 대신 받고 1 사라진다");
            if (k.Weakens) s.Add("걸린 적은 아군 카드가 약점 공격으로 친다(친 카드 한 장에 1 감소)");
            if (k.Mode) s.Add("전환하는 모드다");
            else if (k.Cap != null) s.Add($"최대 {k.Cap}");
            if (k.Reserve) s.Add("예약이다");
            if (k.Hunt) s.Add("한 번에 한 적에게만 — 옮기면 처음부터");
            foreach (var p in k.Per)
            {
                if (p.Stat == "dot") s.Add($"1개당 턴 종료 시 공격력 {P(p.Ratio)} 피해");
                else if (p.Stat == "hot") s.Add($"1개당 턴 종료 시 HP 회복(방어력 {P(p.Ratio)})");
                else
                {
                    int pct = Num.Round(p.V * 100);
                    s.Add($"1개당 {(p.Who == "allies" ? "아군 전원 " : "")}{STAT_KO[p.Stat]} {(pct >= 0 ? "+" : "")}{pct}%");
                }
            }
            if (k.DecayAll) s.Add("적의 차례가 끝나면 전부 사라진다");
            else if (k.Decay > 0) s.Add($"적의 차례마다 {k.Decay} 감소");
            if (k.ConsumeAll) s.Add("발동하면 사라진다");
            else if (k.Consume > 0) s.Add($"발동하면 {k.Consume} 감소");
            if (k.Wipe && k.EndClear) s.Add("다른 사도의 카드를 내거나 턴이 끝나면 전부 사라진다");
            else if (k.Wipe) s.Add("다른 사도의 카드를 내면 전부 사라진다");
            else if (k.EndClear) s.Add("턴이 끝나면 전부 사라진다");
            if (!k.EndClear && k.EndDecay > 0) s.Add($"턴이 끝나면 {k.EndDecay} 감소");
            if (k.OnMax != null) { var fd = data?.Form(k.OnMax.Form); string nm = fd?.Name ?? k.OnMax.Form; s.Add($"최대가 되면{(k.OnMax.Consume ? " 모두 써서" : "")} {Q(nm)}{Ro(nm)} 변신{(fd != null ? $"({FormDur(fd)})" : "")}"); }
            // 계기 · 조건 · 횟수가 같은 잇단 규칙은 효과를, 효과가 같은 잇단 규칙은 계기를 한 줄로 묶는다
            foreach (var g in Group(k.Rules, false, true)) s.Add(SelfRule(k.Name, g));
            return string.Join(". ", s) + ".";
        }

        // ── 적의 수 ───────────────────────────────────────────────────
        /// <summary>적의 수 — 머리 위 · 정보 창. shown 은 엔진이 셈한 실제 피해(Battle.IntentHit)가 있으면 그것.</summary>
        public string Intent(Intent it, int? shown = null)
        {
            if (it == null) return "";
            int v = shown ?? (it.T == "attackAll" ? Num.Round(it.V * R.FOE_ALL_X) : it.V);
            string s = it.T switch
            {
                "attack" => $"공격 {v}",
                "back" => $"관통 {v}(방어 무시)",
                "attackAll" => $"전체 공격 {v}",
                "multi" => $"{v} × {Math.Max(1, it.N)}회",
                "charge" => $"힘을 모은다 — 다음 턴 {Intent(it.Next)}",
                "block" => $"방어 +{it.V}",
                "guard" => $"적 전체 방어 +{it.V}",
                "heal" => $"적 회복 {it.V}",
                "buff" => $"{(it.All ? "적 전체 " : "")}{it.Id} +{it.V}",
                "debuff" => $"파티 {it.Id} +{it.V}",
                "jam" => $"다음 턴 AP -{it.V}",
                "addCard" => $"{Q(CardName(it.Id))} {Math.Max(1, it.N)}장 → {(it.To == "hand" ? "손" : it.To == "draw" ? "뽑을 더미" : "버린 더미")}",
                "summon" => $"{data?.Enemy(it.Id)?.Name ?? it.Id} {Math.Max(1, it.N)} 부르기{(it.Max > 0 ? $"(최대 {it.Max})" : "")}",
                "count" => $"{it.Id} {(it.V >= 0 ? "+" : "")}{it.V}",
                "cardDebuff" => $"{(it.To == "draw" ? "뽑을 더미" : "손")} {(it.N > 0 ? it.N + "장" : "전부")}에 {it.Id}",
                "handCost" => $"손 {(it.N > 0 ? it.N + "장" : "전부")} 비용 {(it.V >= 0 ? "+" : "")}{it.V}",
                "reshuffle" => $"손을 모두 버리고 섞는다{(it.Id != null ? $" · {Q(CardName(it.Id))} {Math.Max(1, it.N)}장" : "")}",
                "autoPlay" => $"손의 카드 {Math.Max(1, it.N)}장이 저절로 나간다",
                "shift" => $"수를 바꾼다 — {Intent(it.Next)}",
                "revive" => $"재 속 — {Math.Max(1, it.N)}턴 뒤 체력 {(it.V > 0 ? it.V : 50)}%로 되살아난다",
                "feign" => "쓰러진 척 — 적이 회복하면 일어선다",
                "thorns" => $"가시 {it.V}",
                "selfHeal" => $"스스로 회복 {it.V}",
                _ => it.T,
            };
            if (it.Id != null && (it.T == "attack" || it.T == "back" || it.T == "attackAll")) s += $" · 파티 {it.Id} {Math.Max(1, it.N)}";
            if (it.Id != null && it.T == "multi") s += $" · 한 대마다 {it.Id} {Math.Max(1, it.Per)}";
            if (it.Tough > 0) s += $" · 강인도 +{N(it.Tough)}";
            if (it.Brk) s += " · 격파하면 끊긴다";
            if (it.If?.Allies != null) s += $" (적이 {it.If.Allies}명 이상일 때)";
            if (it.If?.PartyBlock != null) s += it.If.PartyBlock.Value ? " (파티에 방어 · 실드가 있을 때)" : " (파티에 방어 · 실드가 없을 때)";
            if (it.If?.SelfBlock != null) s += it.If.SelfBlock.Value ? " (자기에게 실드가 있을 때)" : " (자기에게 실드가 없을 때)";
            if (it.If?.Counter != null) s += $" ({it.If.Counter} {Math.Max(1, it.If.N)} 이상일 때)";
            return s;
        }

        // ── 이벤트 결과 ───────────────────────────────────────────────
        public string Outcomes(List<Outcome> outs) => outs == null || outs.Count == 0 ? "없음" : string.Join(" · ", outs.Select(Outcome));

        public string Outcome(Outcome o)
        {
            switch (o.K)
            {
                case "none": return "없음";
                case "gold": return $"골드 {(o.V >= 0 ? "+" : "")}{N(o.V)}";
                case "hp": return $"HP {(o.V >= 0 ? "+" : "")}{Num.Round(o.V * 100)}%";
                case "maxHp": return $"최대 HP {(o.V >= 0 ? "+" : "")}{N(o.V)}";
                case "remove": return $"카드 제거 {Math.Max(1, o.N)}";
                case "dupe": return $"카드 복제 {Math.Max(1, o.N)}";
                case "unique": return "고유 카드 선택";
                case "neutral": return o.Grade != null ? $"교주 카드 ({o.Grade})" : "교주 카드";
                case "equip": return $"{o.Slot ?? "장비"} ({o.Grade})";
                case "flash": return o.Swap ? "신탁 바꾸기 1" : "신탁 1";
                case "shin": return $"축복 {Num.Round(o.V * 100)}%";
                case "noShin": return "축복 막힘";
                case "shinNow": return "축복 1";
                case "shinPick": return $"축복 카드 {Math.Max(1, o.N)}{(o.Kind == "cost" ? " (코스트)" : o.Kind == "power" ? " (위력)" : "")}";
                case "curse": return $"골칫거리 {Q(CardName(o.Id))}";
                case "gift": return $"카드 {Q(CardName(o.Id))}";
                case "scout": return "지도 공개";
                case "shopGift": return $"다음 상점: 장비 ({o.Grade})";
                case "rewardFlash": return "다음 보상: 신탁 1";
                case "next": return "다음 전투: " + Next(o.Next);
                case "mindBreak": return $"정신 붕괴 {Math.Max(1, o.N)}";
                default: return $"({o.K})";
            }
        }

        public static string Next(NextFight n)
        {
            if (n == null) return "";
            var s = new List<string>();
            if (n.Ap != 0) s.Add($"첫 턴 AP {(n.Ap > 0 ? "+" : "")}{n.Ap}");
            if (n.Gauge != 0) s.Add($"고학년 게이지 +{n.Gauge}%");
            if (n.Hand != 0) s.Add($"첫 손패 +{n.Hand}");
            if (n.Weak != 0) s.Add($"파티 약화 {n.Weak}");
            if (n.HpCut > 0) s.Add($"HP -{Num.Round(n.HpCut * 100)}%");
            if (n.Rush != 0) s.Add($"첫 턴 적 전체 즉시 행동 {n.Rush}장 늦춤");
            if (n.FoeVuln != 0) s.Add($"적 전체 취약 {n.FoeVuln}");
            if (n.Quiet != 0) s.Add($"적 패시브 꺼짐 {n.Quiet}턴");
            if (n.Buff != null) foreach (var kv in n.Buff) s.Add($"{kv.Key} {kv.Value}");
            return string.Join(" · ", s);
        }

        // ── 사도 도감 ─────────────────────────────────────────────────
        /// <summary>사도 한 장 — 도감 · 파티 고르기 화면 · 검토용(여러 줄).</summary>
        /// <summary>사도 한 장 — 짧은 글만(고유 효과 · 패시브 · 고학년 요약). 자세한 글은 Hero.</summary>
        public string HeroShort(HeroDef h)
        {
            var lines = new List<string> { $"{h.Name} — {h.Role} · {h.Nature ?? "성격 없음"} · {(h.Row == "front" ? "전열" : h.Row == "mid" ? "중열" : "후열")}" };
            foreach (var k in h.AllKeywords) lines.Add($"고유 「{k.Name}」 {Short(k)}");
            string last = null;
            foreach (var r in h.Passives)
            {
                if (r.Name != null && r.Name == last) { if (!lines[lines.Count - 1].EndsWith(" 등")) lines[lines.Count - 1] += " 등"; continue; }
                lines.Add($"패시브 「{r.Name ?? "패시브"}」 {Short(r)}"); last = r.Name;
            }
            if (h.Ult != null) lines.Add($"고학년 「{h.Ult.Name}」 {Short(h.Ult)}");
            foreach (var f in h.Forms ?? new List<FormDef>()) lines.Add($"변신 「{f.Name}」 {Short(f)}");
            return string.Join("\n", lines);
        }

        public string Hero(HeroDef h)
        {
            var lines = new List<string>
            {
                $"{h.Name} — {h.Role} · {h.Nature ?? "성격 없음"} · {(h.Row == "front" ? "전열" : h.Row == "mid" ? "중열" : "후열")}{(h.Style != null ? " · " + h.Style : "")}",
                $"HP {h.Hp} · 공격력 {h.Atk} · 방어력 {h.Def} · 치명 {h.Crit}%",
            };
            foreach (var k in h.AllKeywords) lines.Add($"키워드 「{k.Name}」 {Keyword(k)}");
            if (h.Passives.Count > 0) lines.Add($"패시브 {Passives(h.Passives)}");
            if (h.Ult != null) lines.Add($"고학년 {Ult(h.Ult)}");
            foreach (var f in h.Forms ?? new List<FormDef>()) lines.Add($"변신 「{f.Name}」 {Form(f)}");
            if (data != null)
            {
                foreach (var id in h.Starter.Distinct()) { var c = data.Card(id); if (c != null) lines.Add($"  시작 「{c.Name}」 [{(c.X ? "X" : c.Cost.ToString())}] {Card(c)}{(h.Starter.Count(x => x == id) > 1 ? $" ×{h.Starter.Count(x => x == id)}" : "")}"); }
                foreach (var id in data.UniquesOf(h.Id)) { var c = data.Card(id); lines.Add($"  고유 「{c.Name}」 [{(c.X ? "X" : c.Cost.ToString())}] {c.Type} · {Card(c)}"); }
                foreach (var f in h.Forms ?? new List<FormDef>())
                    foreach (var kv in f.Cards ?? new Dictionary<string, string>()) { var c = data.Card(kv.Value); if (c != null) lines.Add($"  변신판 「{c.Name}」 [{(c.X ? "X" : c.Cost.ToString())}] {c.Type} · {Card(c)} (← 「{CardName(kv.Key)}」, {f.Name})"); }
            }
            return string.Join("\n", lines);
        }

        // ── 키워드 칩(툴팁) ───────────────────────────────────────────
        /// <summary>엔진 키워드의 짧은 설명(Docs/키워드.md 뜻을 줄인 우리 말) — 칩 · 툴팁용. 수치는 R 의 값과 같다.</summary>
        public static readonly IReadOnlyDictionary<string, string> TIPS = new Dictionary<string, string>
        {
            // 전투 규칙
            ["행동 카운트"] = "적이 행동하기까지 남은 카드 수. 카드를 낼 때마다 1 줄고, 0 이 되면 그 적이 바로 행동한다.",
            ["강인도"] = "적의 버티는 힘. 카드로 치면 깎인다 — 약점 공격은 비용 1당 1, 아니면 1/3(공명 사도는 늘 약점). 다 깎이면 격파.",
            ["격파"] = "강인도를 다 깎았다 — AP +1, 그 적은 다음 차례를 쉬고 내 턴이 다시 오면 강인도가 찬다.",
            ["붕괴"] = "이 카드로 적을 격파하면 발동.", ["처치"] = "이 카드로 적을 쓰러뜨리면 발동.", ["파괴"] = "대상이 쓰러졌으면 발동.",
            ["약점 공격"] = "적의 약점으로 친다 — 피해 +25%, 강인도를 비용 1당 1 깎는다(아니면 1/3).",
            ["방어 기반 피해"] = "방어력 210% + 공격력 30% 로 계산하는 피해.", ["고정 피해"] = "증감 · 상태를 받지 않는 피해.",
            ["고정 지속 피해"] = "증감 · 상태 · 실드를 모두 무시하는 피해.", ["고정 실드"] = "증감을 받지 않는 실드.",
            ["피해 기반 회복"] = "준 피해의 일정 비율만큼 HP 회복(최대 체력 20%까지).", ["부상"] = "체력 30% 미만.",
            ["추가 공격"] = "카드와 별개로 들어가는 공격(협공 · 표식 · 공명 …).",
            // 카드 태그
            ["보존"] = "턴이 끝나도 버려지지 않는다.", ["소멸"] = "내면 이 전투에서 사라진다(소멸 N — N 번 내면).", ["증발"] = "턴이 끝날 때 손에 있으면 소멸.",
            ["회수"] = "내면 버린 더미 대신 손으로 돌아온다(회수 N — N 번까지).", ["망각"] = "소멸하는 대신 뽑을 더미 맨 위로.", ["제거"] = "내면 덱에서 완전히 빠진다.",
            ["개전"] = "전투가 시작되면 첫 손패에 든다.", ["개막"] = "전투가 시작되면 AP 를 치르고 저절로 나간다(모자라면 안 나간다).",
            ["종극"] = "내면 턴이 끝난다.", ["신속"] = "적의 행동 카운트를 줄이지 않는다.", ["주도"] = "턴 시작에 반반으로 비용 -1 — 다른 카드를 먼저 내면 사라진다.",
            ["연결"] = "직접 내면 손의 다른 연결 카드를 모두 버린다.", ["봉인"] = "처음 내면 효과 없이 봉인만 풀린다.", ["사용 불가"] = "낼 수 없다.",
            ["연계"] = "다른 사도의 카드가 나가면 손에서 저절로(공짜로) 나간다.", ["천상"] = "비용 2 이상인 카드가 나가면 손에서 저절로 나간다.",
            ["연속"] = "바로 앞 카드와 성격이 같으면 발동.", ["조율"] = "이 카드의 비용이 남은 AP 와 같으면 발동.", ["안식"] = "효과로 버려지면 발동.",
            ["감응"] = "뽑히면 발동.", ["영감"] = "카드 효과로 뽑히면 발동.", ["열정"] = "열정 카드가 나가면 손에 있는 이 카드의 「열정:」 이 발동.",
            ["소각"] = "소멸할 때 발동.", ["연쇄"] = "다음 턴 시작에 같은 효과가 한 번 더.", ["축복"] = "실드가 최대 체력 30% 미만일 때 손에서 버려지면 고정 실드 60%.",
            ["유일"] = "덱에 한 장만.", ["금기"] = "신탁 · 복제 · 제거가 안 된다.", ["봉인된 금기"] = "금기 + 보스를 처치하면 금기 카드로 바뀐다.",
            ["분쇄"] = "실드가 있는 적을 치면 피해 +20%.", ["탄환"] = "탄환 카드 — 다른 카드 · 장비가 센다.",
            ["결속"] = "같은 사도의 결속 카드가 한 장으로 겹친다(최대 5). 3 이상이면 강해진 모습으로, 내면 처음으로.",
            // 이로운 효과
            ["사기"] = "파티 — 겹마다 카드 피해 계수 +20%p.", ["불굴"] = "파티 — 겹마다 받는 피해 -20%p.", ["결의"] = "파티 — 겹마다 얻는 실드 +20.",
            ["결정화"] = "턴이 끝날 때 겹마다 고정 실드(방어력 20%).", ["고동"] = "턴이 끝날 때 겹마다 적 전체에 고정 피해 70%.",
            ["협공"] = "아군이 공격 카드를 내면 협공을 건 사도가 공격력 100% 로 추가 공격(1 감소).",
            ["반격"] = "적에게 맞으면 반격을 건 사도의 방어 기반 피해 150%(다 막으면 300%)로 되친다(1 감소, 최대 10).",
            ["피해 감소"] = "받는 피해 -15%(1 감소).", ["면역"] = "해로운 효과 하나를 막는다(1 감소).", ["실드 유지"] = "턴이 바뀔 때 실드 절반이 남는다(1 감소).",
            ["실드 보존"] = "턴이 바뀔 때 실드가 그대로 남는다(1 감소).", ["저장"] = "남긴 AP 를 다음 턴으로(1 감소).",
            ["잔광"] = "공격 카드의 강인도 피해 +1, 격파된 적이면 피해 +50%(1 감소).", ["공명"] = "카드를 버리면 추가 공격 80%(1 감소).",
            ["탄성"] = "아군이 추가 공격하면 겹마다 치유 50% — 그리고 사라진다(최대 5).", ["칼날 벼리기"] = "1턴 — 공격 카드의 방어 기반 피해 겹마다 +20%(최대 3).",
            ["빙벽"] = "1턴 — 맞을 때마다 반격(겹을 쓰지 않는다).", ["구속"] = "그 사도 — 자기 카드 말고는 AP 를 얻거나 잃지 않는다.",
            ["형상 강화"] = "1턴 — 만든 카드의 효과 겹마다 +100%(최대 3).", ["행동 둔화"] = "신속 카드를 내면 1턴간 적 전체 사기 -1(1 감소).",
            ["급속"] = "행동 카운트 -N(최대 9). 그 적이 행동하면 사라진다.", ["근면"] = "적을 처치하면 AP 1 · 드로우 1(턴당 1회).", ["계몽"] = "고학년 스킬을 쓰면 드로우 1.",
            ["집중"] = "턴 시작에 체력 50% 미만이면 AP +1.", ["회피"] = "적의 공격 한 대에 체력을 잃지 않는다(1 감소).", ["다음 턴 드로우"] = "다음 턴 시작에 겹만큼 더 뽑는다.",
            ["초재생"] = "턴 시작에 치유 300%(턴마다 1 감소).", ["탐구심"] = "이 카드 — 겹마다 피해 +20%.", ["절대 무적"] = "1턴 — 체력을 잃지 않는다.",
            ["끈기"] = "쓰러질 피해를 받아도 HP 1 로 버틴다(1 감소).",
            // 해로운 효과
            ["취약"] = "받는 피해 +50%(1 감소).", ["약화"] = "주는 피해 -25%(1 감소). 파티에 걸리면 모든 사도가.", ["손상"] = "얻는 실드 -50%(1 감소).",
            ["고통"] = "턴이 끝날 때 겹 × 50% 고정 지속 피해, 그 뒤 절반(최대 20).", ["고통 각인"] = "고통을 얻을 때 2 더.",
            ["균열"] = "턴이 끝날 때 겹 × 40% 지속 피해, 그 뒤 절반(최대 30).", ["그을림"] = "행동 카운트가 줄 때마다 80% 지속 피해(1 감소), 턴이 끝나면 사라진다.",
            ["표식"] = "공격 카드에 맞으면 추가 공격 100% + 강인도 피해(1 감소).", ["잔불"] = "격파된 대상을 치면 겹마다 피해 +30% — 그리고 사라진다(최대 5).",
            ["충격"] = "공격 카드의 대상이 되면 고정 피해 80%, 실드가 있으면 +50%(1 감소).", ["충격파"] = "카드에 맞으면 다른 모두에게 고정 피해 300%(1 감소).",
            ["응징"] = "실드를 얻으면 그쪽 모두에게 고정 피해 200%(1 감소).", ["포자증식"] = "받는 피해 겹마다 +10% — 한 번 들면 모두 사라진다.",
            ["죽음의 낙인"] = "낙인을 건 그 카드로 치면 피해 +80%.", ["열정 약점"] = "1턴 — 열정 카드가 약점 공격으로 든다.",
            ["정신 붕괴"] = "고학년 스킬 · 신탁을 쓸 수 없고, 판에서 카드 얻기 · 신탁 · 제거가 막힌다.", ["둔화"] = "행동 카운트 +N(최대 9). 그 적이 행동하면 사라진다.",
            ["미끄러움"] = "카드를 내면 손에서 무작위 1장이 버려진다(1 감소).", ["봉쇄"] = "이 카드 — 내도 효과가 없고 봉쇄만 풀린다.",
            ["침체"] = "이 카드 — 비용 +1, 내면 풀린다.", ["빙결"] = "이 카드 — 뽑은 턴에는 낼 수 없다.", ["독"] = "이 카드 — 내면 파티가 피해를 입는다(실드 무시).",
            ["기절"] = "적의 다음 차례를 막는다.",
        };

        /// <summary>엔진 키워드 하나의 짧은 설명(없으면 null).</summary>
        public static string Tip(string keyword) => keyword != null && TIPS.TryGetValue(Tag.Parse(keyword).id, out var t) ? t : null;

        /// <summary>칩 · 툴팁 표 한 벌 — 엔진 키워드(TIPS) + 데이터의 사도 고유 효과(이름 → 설명) + 적의 쌓이는 수치. UI 는 이것 하나로 칩을 그린다.</summary>
        public Dictionary<string, string> Tips()
        {
            var o = TIPS.ToDictionary(kv => kv.Key, kv => kv.Value);
            if (data == null) return o;
            foreach (var h in data.Heroes.Values) foreach (var k in h.AllKeywords) if (k.Name != null) o[k.Name] = $"{h.Name}의 고유 효과 — {Keyword(k)}";
            foreach (var h in data.Heroes.Values) foreach (var f in h.Forms ?? new List<FormDef>()) if (f.Name != null && !o.ContainsKey(f.Name)) o[f.Name] = $"{h.Name}의 변신 — {Form(f)}";
            foreach (var e in data.Enemies.Values) foreach (var c in e.Counters) if (c.Name != null && !o.ContainsKey(c.Name)) o[c.Name] = Counter(c);
            return o;
        }

        /// <summary>카드에 나오는 키워드 이름들(칩 차례) — 태그 · 조건 머리 · 상태 · 사도 고유 효과.</summary>
        public List<string> Chips(CardView c)
        {
            var o = new List<string>();
            void Add(string k) { if (k != null && !o.Contains(k)) o.Add(k); }
            foreach (var t in c.Tags) { var id = Tag.Parse(t).id; Add(id == Tag.Weak ? Tag.WeakHit : id); }
            void Walk(List<Fx> fx)
            {
                foreach (var f in fx ?? new List<Fx>())
                {
                    switch (f.K)
                    {
                        case FxK.When: Add(f.On == "draw" ? "영감" : f.On == "drawAny" ? "감응" : f.On == "discard" ? "안식" : f.On == "burn" ? "소각" : f.On == "passion" ? "열정" : null); break;
                        case FxK.IfKill: Add("처치"); break;
                        case FxK.IfBreak: Add("붕괴"); break;
                        case FxK.IfBroken: Add("파괴"); break;
                        case FxK.IfTune: Add("조율"); break;
                        case FxK.IfChain: Add("연속"); break;
                        case FxK.IfWounded: Add("부상"); break;
                        case FxK.Drain: Add("피해 기반 회복"); break;
                        case FxK.Extra: Add("추가 공격"); break;
                        case FxK.Status: case FxK.CardStatus: Add(f.Id); break;
                        case FxK.Dmg: if (f.Base == "def") Add("방어 기반 피해"); if (f.Dot) Add("고정 지속 피해"); else if (f.Fixed) Add("고정 피해"); break;
                        case FxK.Shield: if (f.Fixed) Add("고정 실드"); break;
                        case FxK.Stack: case FxK.Spend: case FxK.IfStack: case FxK.PerStack: Add(f.Id); break;
                        case FxK.Form: Add(data?.Form(f.Id)?.Name); break;
                    }
                    Walk(f.Then);
                }
            }
            Walk(c.Fx);
            if (c.Def.PayWith != null) Add(c.Def.PayWith);
            return o;
        }

        // ── 적 · 쌓이는 수치 ───────────────────────────────────────────
        /// <summary>쌓이는 수치 한 줄 — 「분노: 1개당 주는 피해 +10%. 공격이 아닌 카드를 낼 때마다 +1. 4가 되면 즉시: 공격 300.」</summary>
        public string Counter(CounterDef c)
        {
            var s = new List<string>();
            if (!string.IsNullOrEmpty(c.Desc)) s.Add(c.Desc.TrimEnd('.'));
            if (c.Dealt != 0) s.Add($"1개당 주는 피해 {(c.Dealt > 0 ? "+" : "")}{P(c.Dealt)}");
            if (c.Taken != 0) s.Add($"1개당 받는 피해 {(c.Taken > 0 ? "+" : "")}{P(c.Taken)}");
            if (c.Flat > 0) s.Add($"있으면 받는 피해가 {c.Flat}");
            if (c.OnHit != 0) s.Add($"맞을 때마다 {(c.OnHit > 0 ? "+" : "")}{c.OnHit}");
            if (c.OnCard != 0)
            {
                string ty = c.CardType == null ? "" : c.CardType.StartsWith("!") ? c.CardType.Substring(1) + "이 아닌 " : c.CardType + " ";
                s.Add($"{(c.AfterAct ? "행동한 뒤 " : "")}{ty}카드를 낼 때마다 {(c.OnCard > 0 ? "+" : "")}{c.OnCard}");
            }
            if (c.ResetTurnStart) s.Add($"턴 시작에 {c.Start}로");
            if (c.OnTurnStart != 0) s.Add($"턴 시작에 {(c.OnTurnStart > 0 ? "+" : "")}{c.OnTurnStart}");
            if (c.ClearTurnEnd) s.Add("턴이 끝나면 사라진다");
            if (c.OnTurnEnd != 0) s.Add($"턴이 끝나면 {(c.OnTurnEnd > 0 ? "+" : "")}{c.OnTurnEnd}");
            if (c.StunAtZero) s.Add("0 이 되면 기절");
            if (c.At > 0 && c.Act != null) s.Add($"{c.At}{(Ko.HasFinal(c.At.ToString()) ? "이" : "가")} 되면 {(c.Mode == "next" ? "다음 차례에" : c.Mode == "replace" ? "수를 바꿔" : "즉시")}: {Intent(c.Act)}");
            if (c.Max < 99) s.Add($"최대 {c.Max}");
            return $"{c.Name}: {string.Join(". ", s)}.";
        }

        /// <summary>적 정보 창의 덧붙임 — 쌓이는 수치 · 희귀종 · 영혼 공유 · 소환 연결 · 패시브(여러 줄).</summary>
        public string Enemy(EnemyDef e)
        {
            var l = new List<string>();
            foreach (var c in e.Counters) l.Add(Counter(c));
            foreach (var r in e.Rare) l.Add($"희귀종 — {(R.RARES.TryGetValue(r.Id ?? "", out var k) ? k : r.Id)}{(r.Card != null ? $" ({Q(CardName(r.Card))})" : "")}");
            if (e.Soul) l.Add("영혼 공유 — 맨 앞이 아니면 피해를 받지 않는다");
            if (e.Tied) l.Add("세운 이가 쓰러지면 같이 쓰러진다");
            if (e.ToughTaken > 0 && e.ToughTaken != 1) l.Add($"받는 강인도 피해 ×{N(e.ToughTaken)}");
            foreach (var p in e.Passives) l.Add($"{p.Name}: {FoeOn(p)} {Intent(p.Do)}");
            return string.Join("\n", l);
        }

        static string FoeOn(EnemyPassive p)
        {
            string ty = p.Type == null ? "" : p.Type.StartsWith("!") ? p.Type.Substring(1) + "이 아닌 " : p.Type + " ";
            switch (p.On)
            {
                case "fightStart": return "전투 시작 시";
                case "turnStart": return "턴 시작 시";
                case "turnEnd": return "턴이 끝나면";
                case "hurt": return "맞으면";
                case "lowHp": return $"체력이 {Num.Round(p.At * 100)}% 이하가 되면";
                case "allyDown": return "동료가 쓰러지면";
                case "card": return $"{(p.Same ? "같은 사도의 카드를 잇달아 " : "")}{ty}카드를 {(p.Every > 1 ? p.Every + "장 " : "")}내면";
                case "rushed": return "즉시 행동한 뒤";
                case "debuffed": return "해로운 효과를 받으면";
                case "broken": return "격파되면";
                case "recover": return "격파에서 일어서면";
                case "death": return "쓰러질 때";
                case "guardBreak": return "실드가 깨지면";
                case "act": return "행동한 뒤";
                case "afterDraw": return "파티가 손패를 뽑으면";
                default: return p.On;
            }
        }

        // ── 장비 ───────────────────────────────────────────────────────
        public static string StatsText(Stats st)
        {
            if (st == null) return "";
            var s = new List<string>();
            if (st.Hp != 0) s.Add($"HP +{st.Hp}");
            if (st.Atk != 0) s.Add($"공격력 +{st.Atk}");
            if (st.Def != 0) s.Add($"방어력 +{st.Def}");
            if (st.Crit != 0) s.Add($"치명 +{st.Crit}%");
            return string.Join(" · ", s);
        }

        public string Equip(EquipDef e)
        {
            var s = new List<string> { StatsText(e.Stats) };
            if (e.Effect.Count > 0) s.Add(Passives(e.Effect));
            if (e.Affinity != null)
            {
                var who = data?.Hero(e.Affinity)?.Name ?? e.Affinity;
                var aff = new List<string>();
                if (e.AffinityEffect.Count > 0) aff.Add(Passives(e.AffinityEffect));
                if (e.AffinityStats != null) aff.Add(StatsText(e.AffinityStats));
                if (aff.Count > 0) s.Add($"애착({who}): {string.Join(" · ", aff)}");
            }
            return string.Join(" · ", s.Where(x => x.Length > 0));
        }
    }
}
