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

        /// <summary>지금 글을 짓는 사도(Traits · Trait · Keyword) — 고유 효과 이름을 그 사도의 것으로 먼저 찾는다.</summary>
        HeroDef ctxHero;
        HeroDef OwnerOf(KeywordDef k) => data?.Heroes.Values.FirstOrDefault(h => h.AllKeywords.Contains(k));

        /// <summary>단계 이름이 있는 고유 효과(KeywordDef.Stages)의 n 단계 이름 — 없으면 null.</summary>
        string StageName(string kwId, int n)
        {
            if (kwId == null || n < 1) return null;
            KeywordDef hit = null;
            if (ctxHero != null) hit = ctxHero.AllKeywords.FirstOrDefault(k => k.Name == kwId);
            if (hit == null && data != null) hit = data.Heroes.Values.SelectMany(h => h.AllKeywords).FirstOrDefault(k => k.Name == kwId);
            return hit?.Stages != null && n <= hit.Stages.Count ? hit.Stages[n - 1] : null;
        }
        /// <summary>「「형상」이 꿈결이면」 — 단계 이름이 있고 한 단계만 가리킬 때. 아니면 null.</summary>
        string StageIf(string kwId, int n, int max)
        {
            if (max <= 0 || max != Math.Max(1, n)) return null;
            var st = StageName(kwId, max);
            return st == null ? null : $"{Ko.J(Q(kwId), "이가")} {st}{(Ko.HasFinal(st) ? "이면" : "면")}";
        }

        string Carrier(string kwId)
        {
            if (kwId == null) return null;
            // 글을 짓는 사도의 것이 먼저(디아나 「제자」 ↔ 밍스 「제자」 처럼 이름이 겹친다)
            if (ctxHero != null) foreach (var k in ctxHero.AllKeywords) if (k.Name == kwId) return k.Carrier ?? "self";
            if (data == null) return null;
            foreach (var h in data.Heroes.Values) foreach (var k in h.AllKeywords) if (k.Name == kwId) return k.Carrier ?? "self";
            return null;
        }

        // ── 대상 말 ───────────────────────────────────────────────────
        static string Who(string target, string fallback = "적 1명") => target switch
        {
            "oneEnemy" => "적 1명", "allEnemies" => "적 전체", "randomEnemy" => "무작위 적", "self" => "자신", "oneAlly" => "아군 1명",
            "otherEnemy" => "다른 적 1명", "nextEnemy" => "행동 카운트가 가장 작은 적", "slowestEnemy" => "행동 카운트가 가장 큰 적", "markedEnemy" => "표식이 가장 많은 적", "strongestAlly" => "공격력이 가장 높은 아군",
            "allAllies" => "파티", "otherAllies" => "아군", "party" => "파티", "topEnemy" => "HP가 가장 높은 적", "lowEnemy" => "HP가 가장 낮은 적", null => fallback, _ when target.StartsWith("hero:") => $"{target.Substring(5)}(없으면 공격력이 가장 높은 아군)", _ => fallback,
        };
        static string WhoTo(string target) => target switch
        {
            "allEnemies" => "적 전체에", "randomEnemy" => "무작위 적에게", "otherEnemy" => "다른 적 1명에게", "nextEnemy" => "행동 카운트가 가장 작은 적에게", "slowestEnemy" => "행동 카운트가 가장 큰 적에게", "markedEnemy" => "표식이 가장 많은 적에게", "strongestAlly" => "공격력이 가장 높은 아군에게", "topEnemy" => "HP가 가장 높은 적에게", "lowEnemy" => "HP가 가장 낮은 적에게", "self" => "자신에게", "oneAlly" => "아군 1명에게", "allAllies" => "사도마다", "otherAllies" => "아군마다", "party" => "파티에", _ when target != null && target.StartsWith("hero:") => $"{target.Substring(5)}(없으면 공격력이 가장 높은 아군)에게", _ => "적 1명에게",
        };

        // ── 카드 ───────────────────────────────────────────────────────
        /// <summary>카드 면의 글 — 태그 + 효과(신탁을 얹은 모습이면 신탁의 글).</summary>
        public string Card(CardView c) { choices = c.Def.Choices; powerCard = c.Type == "강화"; bool l0 = lines; lines = true; try { return CardLines(c.Def, c.Tags, c.Fx); } finally { choices = null; powerCard = false; lines = l0; } }
        public string Card(CardDef c) { choices = c.Choices; powerCard = c.Type == "강화"; bool l0 = lines; lines = true; try { return CardLines(c, c.Tags, c.Fx); } finally { choices = null; powerCard = false; lines = l0; } }
        /// <summary>카드 면 — 태그 줄(「소멸. 신속.」) · 머리 줄(AP 빚 · 갈래) · 효과 줄들.</summary>
        string CardLines(CardDef d, IEnumerable<string> tags, List<Fx> fx)
        {
            var tagLine = string.Join(" ", (tags ?? Enumerable.Empty<string>()).Select(t => t + "."));
            var parts = new List<string> { tagLine, Head(d).TrimEnd('\n'), Fx(fx) };
            return string.Join("\n", parts.Where(x => x.Length > 0));
        }
        /// <summary>
        /// 카드 · 신탁 · 축복 글을 짓는 중 — 문장(기본 효과 · 조건 하나 · 강화 규칙 하나)마다 줄을 바꾼다(2026-10-07 가독성, Docs/설명글.md §7).
        /// 안쪽 글(예약 · 함정의 뒤 효과, 강화 규칙의 효과)은 한 줄 그대로.
        /// </summary>
        bool lines;
        int fxDepth;
        /// <summary>문장 이음 — 카드 글의 맨 바깥은 줄바꿈, 그 밖은 「. 」.</summary>
        string SentJoin => !lines ? ". " : fxDepth <= 1 ? "\n" : "; ";
        /// <summary>강화 카드의 글을 짓는 중 — 세기형 버프(사기 · 결의 …)에 「이 전투 동안」 을 붙인다.</summary>
        bool powerCard;
        /// <summary>전투 끝까지 남는 세기형 상태(겹마다, 줄지 않는다) — 강화 카드 글에서 「이 전투 동안」 으로 읽힌다.</summary>
        static readonly HashSet<string> LASTING_ST = new() { "사기", "불굴", "결의", "결정화", "고동", "근면", "계몽", "집중" };
        bool Lasting(Fx f) => powerCard && f.K == FxK.Status && f.Target == null && f.Id != null && LASTING_ST.Contains(f.Id);
        List<string> choices;
        /// <summary>카드 머리의 덤 — 고유 효과로 치르는 비용 · AP 빚 · 두 갈래(카드 글에서는 한 줄씩).</summary>
        string Head(CardDef c)
        {
            var s = new List<string>();
            if (c.PayWith != null) s.Add($"비용은 {Q(c.PayWith)}로.");
            if (c.Debt) s.Add("AP 빚 가능 — 모자란 AP만큼 다음 턴 AP -.");
            if (c.Choices != null && c.Choices.Count == 2) s.Add($"갈래 — {c.Choices[0]} / {c.Choices[1]}.");
            if (s.Count == 0) return "";
            return lines ? string.Join("\n", s.Select(x => x.TrimEnd('.'))) + "\n" : string.Join(" ", s) + " ";
        }
        public string Oracle(CardDef baseCard, OracleDef o)
        {
            var head = new List<string>();
            if (o.Power) head.Add("강화 카드.");
            if (o.Cost != null && o.Cost != baseCard.Cost) head.Add($"비용 {o.Cost}.");
            choices = baseCard.Choices; powerCard = baseCard.Type == "강화" || o.Power;
            bool l0 = lines; lines = true;
            string body;
            try { body = Compose(o.Tags, o.Fx); } finally { choices = null; powerCard = false; lines = l0; }
            return string.Join(" ", head) + (head.Count > 0 && body.Length > 0 ? "\n" : "") + body;
        }
        public string Bless(BlessDef b)
        {
            var parts = new List<string>();
            if (b.Kind != null && R.DIVINE_KO.TryGetValue(b.Kind, out var k)) parts.Add(k.Split('—').Last().Trim() + ".");
            bool l0 = lines; lines = true;
            string body;
            try { body = Compose(b.Tags, b.Fx); } finally { lines = l0; }
            if (body.Length > 0) parts.Add(body);
            return string.Join("\n", parts);
        }
        public string Ult(UltDef u) => $"「{u.Name}」 (게이지 {u.Cost}%) {Fx(u.Fx)}";

        /// <summary>태그 + 효과(카드 글이면 태그 줄 다음 줄에 효과).</summary>
        public string Compose(IEnumerable<string> tags, List<Fx> fx)
        {
            var head = string.Join(" ", (tags ?? Enumerable.Empty<string>()).Select(t => t + "."));
            var body = Fx(fx);
            return head.Length == 0 ? body : body.Length == 0 ? head : head + (lines ? "\n" : " ") + body;
        }

        /// <summary>조건 머리가 키워드 꼴(「처치:」 · 「연속:」 · 갈래 이름)인가 — 아니면 말로 쓴 조건(「낸 뒤 손패가 없으면 →」).</summary>
        static bool KwHead(Fx f) => f.K switch
        {
            FxK.IfBroken or FxK.IfTune or FxK.IfChain or FxK.IfLink or FxK.IfPrev or FxK.IfSwitched or FxK.IfRepeat or FxK.IfKill or FxK.IfBreak or FxK.IfChoice or FxK.IfRoll => true,
            FxK.IfWounded => f.Target != "oneEnemy",
            FxK.When => f.On != "handEnd",
            _ => false,
        };
        /// <summary>조건 문장에서 기본 효과와 같은 갈래의 효과에 「더」 를 붙인다(조건 효과는 기본 효과에 얹힌다 — 바꾸지 않는다).</summary>
        static string AddKind(string k) => k == FxK.Dmg ? "dmg" : k == FxK.Shield || k == FxK.Block ? "guard" : k == FxK.Heal ? "heal" : null;

        /// <summary>앞에서 「적 1명」 을 짚었나(같은 글 안 — 뒤의 「적 1명」 은 「그 적」 으로).</summary>
        bool sawOne;

        /// <summary>효과 조각 목록 → 글(조건은 새 문장 — 카드 글이면 새 줄).</summary>
        public string Fx(List<Fx> fx)
        {
            if (fx == null || fx.Count == 0) return "";
            fxDepth++;
            bool one0 = sawOne; sawOne = false;
            try { return FxBody(fx); }
            finally { fxDepth--; sawOne = one0; }
        }

        string FxBody(List<Fx> fx)
        {
            var sentences = new List<string>();
            var cur = new List<string>();
            string pending = null;    // 「「X」 1개당」 · 「리듬 1개당」 — 바로 뒤 한 줄의 머리
            bool pendEach = false;    // 적마다 그 적의 「X」 1개당 — 뒤 효과의 「적 전체에」 를 뺀다
            string head = null;       // 문장의 머리(조건)
            bool kwHead = false, choiceHead = false;
            string lastLead = null;   // 바로 앞 조각의 대상 머리(「적 1명 」 · 「이번 턴 자신의 」) — 같으면 「· 」 로 잇는다
            var baseKinds = new HashSet<string>();
            bool sawBase = false;     // 기본 문장(조건 없음)에서 「적 1명」 을 짚었나
            Fx prev = null;
            void Flush()
            {
                if (cur.Count == 0 && head == null) return;
                sentences.Add((head != null ? head + (kwHead || inRule ? ": " : " → ") : "") + string.Join(", ", cur));
                cur.Clear(); head = null; lastLead = null;
            }
            foreach (var f in fx)
            {
                string cond = CondHead(f);
                if (cond != null) { Flush(); if (f.K == FxK.IfChoice || f.K == FxK.When) sawOne = sawBase; head = cond; kwHead = KwHead(f); choiceHead = f.K == FxK.IfChoice || f.K == FxK.When; prev = f; continue; }
                if (f.K == FxK.PerStack) { string lim = f.N > 0 && f.Max > 0 ? $"(최소 {f.N} · 최대 {f.Max})" : f.N > 0 ? $"(최소 {f.N})" : f.Max > 0 ? $"(최대 {f.Max})" : ""; pending = f.Each ? $"적마다 그 적의 {Q(f.Id)} 1개당{lim} " : $"{Q(f.Id)} 1개당{lim} "; pendEach = f.Each; prev = f; continue; }
                if (f.K == FxK.PerRhythm) { pending = "리듬 1개당 "; prev = f; continue; }
                if (f.K == FxK.PerDiscarded) { pending = "이 카드로 버린 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPaid) { pending = $"이번 턴 치른 HP {(f.Per > 0 ? f.Per : 100)}당 "; prev = f; continue; }
                if (f.K == FxK.PerDebuff) { pending = $"{(sawOne ? "그 적" : "적 1명")}의 디버프 1가지당 "; prev = f; continue; }
                if (f.K == FxK.PerApLeft) { pending = "남은 AP 1당 "; prev = f; continue; }
                if (f.K == FxK.PerTag) { pending = $"손의 {Q(f.Id)} 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPlayed) { pending = f.Id != null ? $"이번 턴 낸 {Q(f.Id)} 카드 1장당 " : "이번 턴 낸 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPile) { pending = $"{PileKo(f.From)} 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerCardSt) { pending = $"이 카드의 {Q(f.Id)} 1당 "; prev = f; continue; }
                if (f.K == FxK.PerEvent) { pending = f.Per > 1 ? $"그 값 {f.Per}당 " : "그 값 1당 "; prev = f; continue; }
                // 강화 카드 지속 규칙 — 제 문장(앞의 조건에 걸리지 않게 보이도록 따로)
                if (f.K == FxK.Power) { string ph = head; bool pk = kwHead; Flush(); string pt = PowerText(f); sentences.Add(ph != null ? ph + (pk ? ": " : " → ") + pt : pt); prev = f; continue; }
                if ((f.K == FxK.Later || f.K == FxK.AfterCards) && cur.Count > 0 && head == null) Flush();
                var t = One(f, prev);
                if (t == null) { prev = f; continue; }
                if (Lasting(f)) t = "이 전투 동안 " + t;
                string lead = Lasting(f) ? "이 전투 동안 " : Lead(f);
                if (lead != null && !t.StartsWith(lead)) lead = null;
                // 같은 글에서 두 번째부터의 「적 1명」 = 고른 그 적(엔진 Resolve oneEnemy = 카드의 대상)
                if (t.StartsWith("적 1명"))
                {
                    if (sawOne) { t = "그 적" + t.Substring(4); if (lead != null && lead.StartsWith("적 1명")) lead = "그 적" + lead.Substring(4); }
                    else sawOne = true;
                }
                else if (t.Contains("적 1명")) sawOne = true;
                if (head == null) sawBase = sawOne;
                if (pending != null)
                {
                    if (pendEach && t.StartsWith("적 전체에 ")) t = t.Substring("적 전체에 ".Length);
                    t = pending + t; pending = null; pendEach = false; lead = null;
                }
                // 조건 효과는 기본 효과에 얹힌다 — 같은 갈래(피해 · 실드 · 회복)가 기본에 있으면 「더」
                string ak = AddKind(f.K);
                if (ak != null)
                {
                    if (head == null) baseKinds.Add(ak);
                    else if (!choiceHead && baseKinds.Contains(ak) && !t.StartsWith("다른 적")) t += " 더";
                }
                // 같은 대상에 잇단 상태 · 증감은 대상을 한 번만 — 「적 1명 고통 3 · 손상 1」
                const string RUN = "이 전투 동안 ";
                static string NormLead(string l) => l != null && l.StartsWith("그 적") ? "적 1명" + l.Substring(3) : l;
                if (lead != null && NormLead(lead) == NormLead(lastLead) && cur.Count > 0) cur[cur.Count - 1] += " · " + t.Substring(lead.Length);
                else if (t.StartsWith(RUN) && cur.Count > 0 && cur[cur.Count - 1].StartsWith(RUN)) cur.Add(t.Substring(RUN.Length));
                else cur.Add(t);
                lastLead = lead;
                prev = f;
                // 예약 뒤의 효과는 지금 일어난다 — 예약 문장과 떼어 새 문장으로(조건 안이면 그 조건에 그대로)
                if ((f.K == FxK.Later || f.K == FxK.AfterCards) && head == null) Flush();
            }
            Flush();
            return string.Join(SentJoin, sentences);
        }

        /// <summary>대상 머리 — 적에게 거는 상태(「적 1명 」)와 능력치 증감(「이번 턴 자신의 」). 그 밖엔 null.</summary>
        static string Lead(Fx f)
        {
            switch (f.K)
            {
                case FxK.Status:
                    if (f.Id == R.STUN) return null;
                    bool foe = IsFoeT(f.Target) || (f.Target == null && R.IsBadSt(f.Id));
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

        /// <summary>조건이 짚는 적 — 앞에서 「적 1명」 을 짚었으면 「그 적」, 아니면 「고른 적」(엔진: 카드의 대상).</summary>
        string Foe() => sawOne || inRule ? "그 적" : "고른 적";
        /// <summary>적에게 쌓는 고유 효과의 조건이면 「그 적에게 」(엔진 IfStack — carrier enemy 는 대상 적의 것을 본다).</summary>
        string FoeStack(string id) => !inRule && Carrier(id) == "enemy" ? Foe() + "에게 " : "";

        string CondHead(Fx f) => f.K switch
        {
            FxK.IfBroken => "파괴",
            FxK.IfTune => "조율",
            FxK.IfChain => "연속",
            FxK.IfLink => "잇기",
            FxK.IfPrev => $"앞이 {f.Type}",
            FxK.IfRhythm => $"리듬이 {f.N} 이상이면",
            FxK.IfSwitched => "전환",
            FxK.IfStack => FoeStack(f.Id) + (f.Not ? $"{Ko.J(Q(f.Id), "이가")} 없으면" : StageIf(f.Id, f.N, f.Max) is string sti ? sti : f.Max > 0 && f.Max == Math.Max(1, f.N) ? $"{Ko.J(Q(f.Id), "이가")} {f.Max}{(Ko.HasFinal(f.Max.ToString()) ? "이면" : "면")}" : f.Max > 0 ? $"{Ko.J(Q(f.Id), "이가")} {Math.Max(1, f.N)}~{f.Max}{(Ko.HasFinal(f.Max.ToString()) ? "이면" : "면")}" : f.N > 1 ? $"{Ko.J(Q(f.Id), "이가")} {f.N}개 이상이면" : $"{Ko.J(Q(f.Id), "이가")} 있으면"),
            FxK.When => f.On switch { "draw" => "영감", "drawAny" => "감응", "discard" => "안식", "handEnd" => "턴 끝에 손에 있으면", "burn" => "소각", "passion" => "열정", _ => f.On },
            FxK.IfRepeat => "되풀이",
            FxK.IfHeld => $"손에서 {Math.Max(1, f.N)}턴 묵혔으면",
            FxK.IfPlayedMax => f.N == 0 ? "이번 턴 첫 카드면" : $"이번 턴 앞서 낸 카드가 {f.N}장 이하면",
            FxK.IfApLeft => $"낸 뒤 AP가 {Math.Max(1, f.N)} 이상 남으면",
            FxK.IfSpent => $"이번 턴 쓴 AP가 꼭 {f.N}이면",
            FxK.IfBalanced => "공격과 스킬이 같은 장수면",
            FxK.IfHunted => $"{Foe()}이 찍혀 있으면",
            FxK.IfDebuffs => $"{Foe()}의 디버프가 {Math.Max(1, f.N)}가지 이상이면",
            FxK.IfHp => f.Not ? $"파티 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}%보다 많으면" : $"파티 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 이하이면",
            FxK.IfKill => "처치",
            FxK.IfBreak => "붕괴",
            FxK.IfWounded => f.Target == "oneEnemy" ? $"{Foe()}이 부상이면" : "부상",
            FxK.IfChoice => choices != null && f.N >= 1 && f.N <= choices.Count ? choices[f.N - 1] : $"갈래 {f.N}",
            FxK.IfRandom => $"{Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 확률로",
            FxK.IfHand => (inRule ? "그 뒤 " : "낸 뒤 ") + (f.N <= 0 ? "손패가 없으면" : $"손패가 {f.N}장 이하면"),
            FxK.IfPile => $"{PileKo(f.From)}가 {Math.Max(1, f.N)}장 이상이면",
            FxK.IfNth => f.N <= 1 ? "이번 턴 첫 카드면" : $"이번 턴 {f.N}번째 카드면",
            FxK.IfStreak => $"같은 사도의 카드를 {Math.Max(2, f.N)}장째 잇달아 내면",
            FxK.IfAllHeroes => "이번 턴 사도 모두가 카드를 냈으면(이 카드 포함)",
            FxK.IfFoe => (f.Id switch { "broken" => $"{Foe()}이 격파 상태면", "tough" => $"{Foe()}의 강인도가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 이하면", "guarded" => $"{Foe()}에게 실드가 있으면", "attack" => $"{Foe()}이 공격하려 하면", "hp" => $"{Foe()}의 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.3) * 100)}% 이하면", "hpMob" => $"{Foe()}이 보스가 아니고 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.3) * 100)}% 이하면", var st when R.ALL_ST.Contains(st) => $"{Foe()}에게 {Ko.J(st, "이가")} {(f.Not ? "없으면" : "있으면")}", _ => f.Id }) + (f.Not && !R.ALL_ST.Contains(f.Id ?? "") ? "(아니면)" : ""),
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
            if (f.Run) return "이 전투 동안 ";
            int t = f.TurnsOr1;
            if (t >= 999) return "이 전투 동안 ";
            return t > 1 ? $"{t}턴간 " : "이번 턴 ";
        }

        static readonly Dictionary<string, string> STAT_KO = new() { ["dealt"] = "주는 피해", ["taken"] = "받는 피해", ["atk"] = "공격력", ["def"] = "방어력", ["crit"] = "치명 확률", ["guard"] = "주는 실드" };

        /// <summary>대상 표기(Docs/키워드.md §0) — 표기 없음 = 파티 전원 · 「자신의」 = 카드 주인 · 「아군의」 = 자신을 뺀 사도.</summary>
        /// <summary>적을 가리키는 대상인가(글에서 「적 …」 으로 적을 것) — 2026-10-06 nextEnemy · topEnemy 등이 「파티」 로 읽히던 것 고침.</summary>
        static bool IsFoeT(string t) => t == "oneEnemy" || t == "allEnemies" || t == "randomEnemy" || t == "topEnemy" || t == "lowEnemy" || t == "otherEnemy" || t == "nextEnemy" || t == "slowestEnemy" || t == "markedEnemy";
        static string Whose(string tg) => tg switch
        {
            "self" => "자신의 ", "otherAllies" => "아군의 ", "allAllies" => "", "party" => "", "oneAlly" => "아군 1명의 ",
            "strongestAlly" => "공격력이 가장 높은 아군의 ", "otherEnemy" => "다른 적 1명의 ", "nextEnemy" => "행동 카운트가 가장 작은 적의 ", "slowestEnemy" => "행동 카운트가 가장 큰 적의 ", "markedEnemy" => "표식이 가장 많은 적의 ", _ when tg != null && tg.StartsWith("hero:") => $"{tg.Substring(5)}(없으면 공격력이 가장 높은 아군)의 ", "allEnemies" => "적 전체의 ", "randomEnemy" => "무작위 적의 ", "topEnemy" => "HP가 가장 높은 적의 ", "lowEnemy" => "HP가 가장 낮은 적의 ", _ => "적 1명의 ",
        };

        string One(Fx f, Fx prev)
        {
            switch (f.K)
            {
                case FxK.Dmg:
                    {
                        if (f.OfEvent > 0) return f.OfStack != null ? $"{WhoTo(f.Target)} {Q(f.OfStack)} 1당 {Num.Round(f.OfEvent)} 고정 피해" : $"{WhoTo(f.Target)} 그 값의 {P(f.OfEvent)} 고정 피해";
                        string hits = f.XHits ? $"X회{(f.XStack != null ? $"(+{Q(f.XStack)} 수)" : "")} × " : f.HitsOr1 > 1 ? $"{f.HitsOr1}회 × " : "";
                        string what = f.Base == "def" ? $"방어 기반 피해 {P(f.Ratio)}" : f.Dot ? $"공격력 {P(f.Ratio)} 고정 지속 피해" : $"공격력 {P(f.Ratio)} {(f.Fixed ? "고정 " : "")}피해";
                        string to = f.Target == "randomEnemy" && hits.Length > 0 ? "무작위 적" : WhoTo(f.Target);
                        return $"{to} {hits}{what}";
                    }
                case FxK.Block:
                case FxK.Shield:
                    {
                        if (f.OfEvent > 0) return f.OfStack != null ? $"{Q(f.OfStack)} 1당 {Num.Round(f.OfEvent)} 고정 실드" : $"그 값의 {P(f.OfEvent)} 고정 실드";
                        // 표기 없음 = 파티(Docs/키워드.md §0) — 실드는 늘 파티 공용이라 대상을 적지 않는다
                        return $"방어력 {P(f.Ratio)} {(f.Fixed ? "고정 " : "")}{(f.K == FxK.Block ? "방어" : "실드")}";
                    }
                case FxK.Heal: return $"HP 회복(방어력 {P(f.Ratio)})";
                case FxK.Strip: return $"{Who(f.Target)}의 방어 · 실드 전부 파괴";
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
                case FxK.HealMod: return $"{Dur(f)}{Whose(f.Target ?? "self")}회복량 {(f.V >= 0 ? "+" : "")}{Num.Round(f.V * 100)}%";
                case FxK.Roll: return $"무작위로 {Math.Max(2, f.N)}갈래 가운데 하나";
                case FxK.Recast: return $"이 카드 효과를 {P(f.Ratio > 0 ? f.Ratio : 0.5)}로 한 번 더";
                case FxK.CostMod: return $"{(f.TurnsOr1 > 1 ? $"{f.TurnsOr1}턴간 " : "이번 턴 ")}{Filt(f)}카드{(f.N > 0 ? $" {f.N}장" : "")} 비용 {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.AddTag: return $"이번엔 {Q(f.Id)}";
                case FxK.CutHit: return $"적의 다음 공격 한 대 피해 -{P(f.V)}";
                case FxK.ClearDebt: return "AP 빚 탕감";
                case FxK.Ap: return $"AP {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.NextCheaper: return $"다음 카드 비용 -{f.IV}";
                case FxK.Gauge: return $"고학년 게이지 {(f.IV >= 0 ? "+" : "")}{f.IV}%";
                case FxK.Discard: return f.All ? "손패 전부 버리기" : $"{(f.Random ? "무작위 " : "")}손패 {f.IV}장 버리기";
                case FxK.Make: return $"{Q(CardName(f.Id))} {Math.Max(1, f.IV)}장 생성{(f.To == "draw" ? "(뽑을 더미 무작위 자리)" : f.To == "top" ? "(뽑을 더미 맨 위)" : f.To == "discard" ? "(버린 더미)" : "")}";
                case FxK.Drain: return $"준 피해의 {P(f.Ratio)}만큼 HP 회복(최대 HP 20%까지)";
                case FxK.Extra: return $"{WhoTo(f.Target)} {(f.HitsOr1 > 1 ? $"{f.HitsOr1}회 × " : "")}추가 공격 {(f.Base == "def" ? "방어 기반 " : "")}{P(f.Ratio)}";
                case FxK.CardStatus:
                    {
                        string where = f.To == "hand" ? (f.N > 0 ? $"손의 {Filt(f)}카드 {f.N}장에 " : $"손의 {Filt(f)}카드 전부에 ") : f.To == "draw" ? (f.N > 0 ? $"뽑을 더미 {Filt(f)}{f.N}장에 " : "뽑을 더미 전부에 ") : f.To == "pulled" ? "그 카드에 " : "이 카드에 ";
                        return f.Id == "비용" ? $"{where}비용 {(f.IV >= 0 ? "+" : "")}{f.IV}" : R.IsCardSt(f.Id) ? $"{where}{f.Id} {f.IV}" : $"{where}{Q(f.Id)} {(f.IV >= 0 ? "+" : "")}{f.IV}";
                    }
                case FxK.Transform: return f.From == null ? $"이 카드를 {Q(CardName(f.Id))}로 바꿈(이번 전투)" : $"손의 {Q(CardName(f.From))}{(f.N > 1 ? $" {f.N}장" : "")}을 {Q(CardName(f.Id))}로 바꿈(이번 전투)";
                case FxK.Form: { var fd = data?.Form(f.Id); return fd == null ? $"변신 {Q(f.Id)}" : $"{Q(fd.Name)}{Ro(fd.Name)} 변신({FormDur(fd)})"; }
                case FxK.FormEnd: return "변신 해제";
                case FxK.Cue: return null;
                case FxK.Later: return $"{Math.Max(1, f.N)}턴 뒤 턴 시작에 → {Fx(f.Then)}";
                case FxK.AfterCards: return $"이 뒤로 카드를 {Math.Max(1, f.N)}장 더 내면 → {Fx(f.Then)}";
                case FxK.Trap: return $"{WhoTo(f.Target)} 함정 — 다음에 공격하면: {Fx(f.Then)}";
                case FxK.Confuse: return $"{Who(f.Target)} 혼란(다음 공격이 다른 적에게)";
                case FxK.AutoPlay: return $"손의 무작위 {Filt(f)}{(f.Id != null ? Q(f.Id) + " " : "")}카드 {f.NOr1}장 저절로 나감{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}";
                case FxK.CastOther: return $"손의 다른 사도 {(f.Id != null ? f.Id + " " : "")}카드 하나를 대신 발동{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}(카드는 손에 남음)";
                case FxK.Pull: return $"{PileKo(f.From)} {(f.At == "bottom" ? "맨 아래" : f.At == "random" ? "무작위" : "맨 위")} {Filt(f)}카드 {f.NOr1}장을 {(f.To == "top" ? "뽑을 더미 맨 위로" : "손으로")}";
                case FxK.ExileFrom: return f.From == "pulled" ? "그 카드 소멸" : $"{PileKo(f.From)}에서 무작위 {Filt(f)}{f.NOr1}장 소멸";
                case FxK.Dispel: return $"{Who(f.Target)}의 버프 {f.NOr1Of(f.IV)}개 제거";
                case FxK.GrowRun: return $"{Whose(f.Target ?? "self")}{(f.Id == "def" ? "방어력" : f.Id == "crit" ? "치명" : "공격력")} +{f.IV} (모험이 끝날 때까지)";
                case FxK.Tough:
                    {
                        bool after = prev != null && prev.K == FxK.Dmg && (prev.Target ?? "oneEnemy") == (f.Target ?? "oneEnemy");
                        return after ? $"강인도 피해 {N(f.V)}" : $"{Who(f.Target)} 강인도 피해 {N(f.V)}";
                    }
                case FxK.RushDown: return $"{Who(f.Target)} 즉시 행동 {f.IV}장 늦춤";
                case FxK.Status:
                    {
                        if (f.Id == R.STUN) return $"{Who(f.Target)} 기절";
                        bool foe = IsFoeT(f.Target) || (f.Target == null && R.IsBadSt(f.Id));
                        if (foe) return $"{Who(f.Target)} {f.Id} {f.IV}";
                        if (R.IsBadSt(f.Id)) return $"파티 {f.Id} {f.IV}";
                        // 파티 층(표기 없음 = 파티 전원) · 개인 층은 대상을 적는다
                        return f.Target != null && f.Target != "party" && f.Target != "auto" ? $"{WhoTo(f.Target)} {f.Id} {f.IV}" : R.IsHeroSt(f.Id) && f.Target == null ? $"자신에게 {f.Id} {f.IV}" : $"{f.Id} {f.IV}";
                    }
                case FxK.Cleanse: return $"파티 디버프 {Math.Max(1, f.IV)}개 해제";
                case FxK.Stack:
                    {
                        if (f.OfEvent > 0) return $"그 값 {Num.Round(1 / f.OfEvent)}당 {Q(f.Id)} +1";
                        string c = Carrier(f.Id);
                        bool foe = c == "enemy" || (c == null && (f.Target == "oneEnemy" || f.Target == "allEnemies" || f.Target == "randomEnemy"));
                        if (foe) return $"{WhoTo(f.Target ?? "oneEnemy")} {Q(f.Id)} +{f.IV}";
                        // 사도 표시(hero) — 누구에게 붙이는지 적는다
                        return c == "hero" && f.Target != null && f.Target != "self" ? $"{WhoTo(f.Target)} {Q(f.Id)} +{f.IV}" : $"{Q(f.Id)} +{f.IV}";
                    }
                case FxK.Spend: return f.All ? $"{Q(f.Id)} 전부 소모" : $"{Q(f.Id)} {f.IV} 소모";
                case FxK.SpendRhythm: return f.All ? "리듬 전부 소모" : $"리듬 {f.IV} 소모";
                case FxK.Flip: return "전환";
                case FxK.Hasten: return $"재촉 {f.IV}";
                case FxK.PayHp: return $"파티 HP {f.IV} 소모";
                case FxK.Feed: return f.Target == "oneAlly" ? $"아군 1명의 고유 효과 +{f.IV}" : f.Target == "self" ? $"자신의 고유 효과 +{f.IV}" : $"사도마다 고유 효과 +{f.IV}";
                case FxK.NextAp: return $"다음 턴 AP {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.Burn: return f.All ? "손패 전부 소멸" : $"{(f.Random ? "무작위 " : "")}손패 {f.IV}장 소멸";
                case FxK.Reflect: return inRule ? $"그 피해의 {P(f.Ratio)}를 때린 적에게 고정 피해로" : $"지난 적의 차례에 받은 피해의 {P(f.Ratio)}를 {Who(f.Target)}에게 고정 피해로";
                case FxK.PayHpPct: return $"파티 최대 HP의 {P(f.V)}만큼 HP 소모";
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
                        if (w.Nth > 0) return w.Nth == 1 ? "파티가 한 턴에 첫 카드를 낼 때" : $"파티가 한 턴에 카드를 {w.Nth}장째 낼 때";
                        if (w.Sig) return $"{SelfOf(w)}시그니처 카드를 내면";
                        if (w.Repeat) return $"{(any ? "" : "자신의 ")}같은 카드를 잇달아 내면";
                        // 「0코 이하」 → 「0코」, 「1코 이상 1코 이하」 → 「1코」
                        bool exact = w.MaxCost != null && (w.MaxCost == 0 || w.MinCost == w.MaxCost);
                        string type = (w.MaxCost != null ? $"비용 {w.MaxCost}{(exact ? "" : " 이하")} " : "") + (w.Tag != null ? Q(w.Tag) + " " : "") + (w.Type != null ? w.Type + " " : "");
                        int minCost = exact ? 0 : w.MinCost ?? 0;
                        if (w.Marked != null) return $"{Ko.J(Q(w.Marked), "이가")} 붙은 아군이 {type}카드를 낼 때마다";
                        if (w.Every > 0)
                            return $"{(any ? "파티가 " : "자신의 ")}{(w.PerTurn ? "한 턴에 " : "")}{(minCost > 1 ? $"비용 {minCost} 이상 " : "")}{type}카드를 {w.Every}장 낼 때마다";
                        return $"{(w.Who == "other" ? "다른 아군이 " : any ? "아군이 " : SelfOf(w))}{(minCost > 0 ? $"비용 {minCost} 이상 " : "")}{type}카드를 낼 때마다";
                    }
                case "guard": return PartyOf() + (w.Kind == "block" ? "방어를 얻으면" : w.Kind == "shield" ? "실드를 얻으면" : "방어나 실드를 얻으면");
                case "break": return w.Mine ? "적을 격파하면" : "적이 격파되면";
                case "kill": return w.Mine ? "적을 처치하면" : "적이 쓰러지면";
                case "hurt": return PartyOf() + (w.Pct > 0 ? $"한 번에 최대 HP {Num.Round(w.Pct * 100)}% 이상 잃으면" : w.Guarded ? "공격을 받으면(다 막아도)" : "피해를 받으면");
                case "endure": return "끈기로 버티면";
                case "unwound": return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}회복으로 부상에서 벗어나면";
                case "lowHp": return $"{(mine ? "파티 " : "")}HP가 {Num.Round(w.Pct * 100)}% 이하가 되면";
                case "rush": return "적이 즉시 행동하면";
                case "ult": return w.Who == "any" ? "아군이 고학년을 쓰면" : "고학년을 쓰면";
                case "debuff": return $"{(w.Who == "any" ? "아군이 " : "")}적에게 {(w.Fresh ? "새 디버프" : "디버프")}를 걸면";
                case "overheal": return w.Who == "any" ? "아군의 회복량이 최대 HP를 초과하면" : "회복량이 최대 HP를 초과하면";
                case "stackReach": return $"{Ko.J(Q(w.Id), "이가")} {w.N}개가 되면";
                case "stackGone": return w.Decay ? $"{Ko.J(Q(w.Id), "이가")} 다 닳으면" : $"{Ko.J(Q(w.Id), "이가")} 사라지면";
                case "stackOver": return $"{Ko.J(Q(w.Id), "이가")} 최대라 넘치면";
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
                case "stack":
                    {
                        // 적에게 쌓는 고유 효과는 「대상 적」 의 것을 본다(그 카드의 대상 · 쓰러진 적 · 때린 적)
                        string on = mine && Carrier(c.Id) == "enemy" ? "그 적에게 " : "";
                        if (!c.Not && StageIf(c.Id, c.N, c.Max) is string sti) return on + sti;
                        if (!c.Not && c.Max > 0) return c.Max == Math.Max(1, c.N) ? $"{on}{Ko.J(Q(c.Id), "이가")} {c.Max}{(Ko.HasFinal(c.Max.ToString()) ? "이면" : "면")}" : $"{on}{Ko.J(Q(c.Id), "이가")} {Math.Max(1, c.N)}~{c.Max}{(Ko.HasFinal(c.Max.ToString()) ? "이면" : "면")}";
                        return c.Not ? $"{on}{Ko.J(Q(c.Id), "이가")} 없으면" : c.N > 1 ? $"{on}{Ko.J(Q(c.Id), "이가")} {c.N}개 이상이면" : $"{on}{Ko.J(Q(c.Id), "이가")} 있으면";
                    }
                case "hp": return $"{(mine ? "파티 " : "")}HP가 {Num.Round(c.Pct * 100)}% 이하이면";
                case "hpMin": return $"{(mine ? "파티 " : "")}HP가 {Num.Round(c.Pct * 100)}% 이상이면";
                case "status": return c.Id == R.RHYTHM ? $"리듬이 {c.N} 이상이면" : $"{(R.HERO_ST.Contains(c.Id) ? "자신" : "파티")} {Ko.J(c.Id, "이가")} {c.N} 이상이면";
                case "foes": return $"적이 {c.N}명 이상이면";
                case "foesMax": return $"적이 {c.N}명뿐이면";
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

        /// <summary>
        /// 강화 카드 지속 규칙(power)의 글 — 「이 전투 동안 매 턴 시작 시 아군 전원에게 「물총」 +1 · 공격 카드를 낼 때마다 … (턴당 2회)」.
        /// 턴 시작 · 턴 종료는 「매 턴 …」, 항상(always)의 증감은 「이번 턴」 없이.
        /// </summary>
        public string PowerText(Fx f) => string.Join("\n", PowerRules(f.Rules).Split('\n').Select(x => "이 전투 동안 " + x));

        /// <summary>강화 규칙들 — 「 · 」 로 잇는다(「이 전투 동안」 머리는 빼고).</summary>
        public string PowerRules(List<PassiveRule> rules)
        {
            if (rules == null || rules.Count == 0) return "(규칙 없음)";
            bool pc = powerCard, m0 = mine; powerCard = false; mine = true;
            inRule = true;
            try
            {
                var outs = new List<string>();
                foreach (var r in rules)
                {
                    string head;
                    if (r.When?.On == "always") head = string.Join(" ", r.Conds.Select(CondText));
                    else
                    {
                        var parts = new List<string> { r.When?.On == "turnStart" ? "매 턴 시작 시" : r.When?.On == "turnEnd" ? "매 턴 종료 시" : WhenText(r.When) };
                        parts.AddRange(r.Conds.Select(CondText));
                        head = string.Join(" ", parts);
                    }
                    var fx = r.When?.On == "always" ? r.Fx.Select(x => FxK.Mods.Contains(x.K) && !x.Run ? AsRun(x) : x).ToList() : r.Fx;
                    string body = EvPer(r.When, Fx(fx));
                    if (r.When?.On == "always") body = body.Replace("이 전투 동안 ", "");
                    outs.Add(RuleJoin(head, body, RuleLimit(r)));
                }
                return string.Join(lines && fxDepth <= 1 ? "\n" : " · ", outs);
            }
            finally { inRule = false; powerCard = pc; mine = m0; }
        }
        static Fx AsRun(Fx f) { var c = f.Copy(); c.Run = true; return c; }

        /// <summary>규칙 한 줄(이름 빼고) — 「피해를 받으면 반격 1 (턴당 1회)」.</summary>
        public string Rule(PassiveRule r)
        {
            bool m0 = mine; inRule = true; mine = true;
            try { return RuleBody(r); } finally { inRule = false; mine = m0; }
        }
        bool inRule;
        /// <summary>
        /// 사도 패시브 · 고유 효과 글을 짓는 중(Docs/설명글.md) — 계기에 대상 낱말을 붙인다(「자신의 공격 카드를 낼 때마다」 · 「파티가 피해를 받으면」 ·
        /// 「대상 적에게 「X」가 있으면」). 강화 카드 지속 규칙(PowerRules) 글은 이 표시 없이 옛 꼴 그대로.
        /// </summary>
        bool mine;
        /// <summary>파티 층 고유 효과(carrier ally)의 규칙 — who 가 없으면 누구의 카드든(엔진 Matches 의 kwOf ally).</summary>
        bool kwAlly;
        string SelfOf(When w) => mine && w.Who == null && !kwAlly ? "자신의 " : "";
        string PartyOf() => mine ? "파티가 " : "";

        string RuleBody(PassiveRule r) => RuleLine(RuleHead(r), Aim(r.When, Fx(r.Fx)), RuleLimit(r));

        /// <summary>규칙 한 줄 — 「계기 → 결과 (턴당 N회)」(Docs/설명글.md §1). 계기가 없으면 결과만.</summary>
        /// <summary>
        /// 계기가 적 하나를 짚으면 규칙 속 「적 1명」 을 그 적으로 — 엔진은 oneEnemy 를 그 일의 대상(카드의 대상 · 친 적 · 때린 적)으로 푼다(Battle.Resolve).
        /// </summary>
        string Aim(When w, string fx)
        {
            fx = EvPer(w, fx);
            if (!mine || w == null || fx.Length == 0) return fx;
            string who = w.On switch
            {
                "play" when w.Type == "공격" => "그 적",
                "hit" => "친 적",
                "hurt" or "blocked" => "때린 적",
                "foeAct" or "foeActBefore" or "foeGuard" or "foeShieldBreak" or "break" => "그 적",
                _ => null,
            };
            return who == null ? fx : fx.Replace("적 1명에게 ", who + "에게 ").Replace("적 1명 ", who + " ");
        }
        /// <summary>계기가 넘친 값(stackOver)이면 「그 값 1당」 → 「넘친 1개당」.</summary>
        static string EvPer(When w, string fx) => w?.On == "stackOver" ? System.Text.RegularExpressions.Regex.Replace(fx, @"그 값 (\d+)당 ", "넘친 $1개당 ") : fx;
        /// <summary>계기 → 결과. 결과가 말로 쓴 조건 하나로 시작하면(「그 뒤 손패가 없으면 → …」) 계기에 붙여 화살표를 하나만.</summary>
        static string RuleLine(string head, string fx, string limit)
        {
            if (fx.Length == 0) return head + limit;
            if (head.Length == 0) return fx + limit;
            // 규칙 속 조건은 「X면: Y」 로 온다 — 문장이 하나면 「계기 X면 → Y」 로 화살표 하나
            int colon = fx.IndexOf(": ", StringComparison.Ordinal);
            if (colon > 0 && fx.IndexOf(". ", StringComparison.Ordinal) < 0 && fx.IndexOf(": ", colon + 2, StringComparison.Ordinal) < 0)
            {
                string cond = fx.Substring(0, colon);
                if ((cond.EndsWith("면") || cond.EndsWith("확률로")) && !cond.Contains(", ") && !cond.Contains(" → ")) return head + " " + cond + " → " + fx.Substring(colon + 2) + limit;
            }
            return head + " → " + fx + limit;
        }

        /// <summary>
        /// 규칙의 계기 · 조건 글. 적에게 쌓는 고유 효과(carrier enemy)를 보는 조건이 처치 · 격파 계기와 붙으면 한 마디로 —
        /// 「적이 쓰러지면 「깃발」이 있으면」 → 「「깃발」이 있는 적이 쓰러지면」(엔진: 조건의 대상 = 쓰러진 적).
        /// </summary>
        (string when, string conds) HeadParts(PassiveRule r, bool alwaysWhen)
        {
            var conds = r.Conds.ToList();
            string when = alwaysWhen || r.When.On != "always" || conds.Count == 0 ? WhenText(r.When) : null;
            if (mine && (r.When.On == "kill" || r.When.On == "break"))
            {
                var c = conds.FirstOrDefault(x => x.C == "stack" && x.N <= 1 && Carrier(x.Id) == "enemy");
                if (c != null)
                {
                    conds.Remove(c);
                    string has = $"{Ko.J(Q(c.Id), "이가")} {(c.Not ? "없는" : "있는")} 적";
                    when = r.When.On == "kill" ? (r.When.Mine ? $"{has}을 처치하면" : $"{has}이 쓰러지면") : (r.When.Mine ? $"{has}을 격파하면" : $"{has}이 격파되면");
                }
            }
            return (when, string.Join(" ", conds.Select(CondText)));
        }

        /// <summary>규칙의 머리(계기 + 조건) · 꼬리(횟수 제한) — 머리 · 꼬리가 같은 잇단 규칙은 효과를 한 줄로 묶는다.</summary>
        string RuleHead(PassiveRule r)
        {
            var (when, conds) = HeadParts(r, false);
            return string.Join(" ", new[] { when, conds }.Where(x => !string.IsNullOrEmpty(x)));
        }
        static string RuleLimit(PassiveRule r) => r.Limit != null ? $" ({(r.Limit.Per == "fight" ? "전투당" : "턴당")} {r.Limit.N}회)" : "";
        /// <summary>조건 머리 · 여러 문장이 없는 효과 글(「, 」 로 이어 붙여도 되는 것).</summary>
        static bool Plain(string fx) => fx.Length > 0 && !fx.Contains(": ") && !fx.Contains(". ") && !fx.Contains(" → ") && !fx.Contains("\n");
        static string RuleJoin(string head, string fx, string limit) => RuleLine(head, fx, limit);

        /// <summary>패시브 — 「이름: 규칙」, 같은 이름이 잇달면 한 이름 아래 문장으로(계기 · 조건 · 횟수까지 같으면 효과만 잇는다).</summary>
        public string Passives(List<PassiveRule> rules)
        {
            var outs = new List<string>();
            string last = null;
            bool m0 = mine; inRule = true; mine = true;
            try
            {
                foreach (var g in Group(rules, true, false))
                {
                    string body = RuleLine(g.Head, g.Fx, g.Limit);
                    if (g.Name != null && g.Name == last) { outs[outs.Count - 1] += ". " + body; continue; }
                    outs.Add($"{g.Name ?? "패시브"}: {body}");
                    last = g.Name;
                }
            }
            finally { inRule = false; mine = m0; }
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
                var (when, conds) = HeadParts(r, alwaysWhen);
                string fx = Aim(r.When, Fx(r.Fx)), limit = RuleLimit(r);
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
                    string all = q + " 전부 소모";
                    var m = System.Text.RegularExpressions.Regex.Match(fx, System.Text.RegularExpressions.Regex.Escape(q) + @" (\d+) 소모");
                    if (fx.Contains(all)) { head += " 모두 써서"; fx = Cut(fx, all); }
                    else if (m.Success) { head += $" {m.Groups[1].Value}개 써서"; fx = Cut(fx, m.Value); }
                }
            }
            return $"{head} → {(fx.Length > 0 ? fx : "끝")}{g.Limit}";
        }
        static string Cut(string fx, string part)
        {
            if (fx.Contains(part + ", ")) return fx.Replace(part + ", ", "");
            if (fx.Contains(", " + part)) return fx.Replace(", " + part, "");
            if (fx.Contains(". " + part)) return fx.Replace(". " + part, "");
            if (fx.Contains(part + ". ")) return fx.Replace(part + ". ", "");
            return fx.Replace(part, "");
        }

        // ── 한 가지 글(2026-10-05 사용자: 「자세히 보기를 없애고 수치가 다 보이게」 — Docs/설명글.md) ──────
        // Short · Detail 은 옛 이름이다 — 둘 다 수치 · 조건 · 제한이 다 든 같은 글을 돌려준다(화면이 「자세히」를 따로 둘 까닭이 없다).
        // 사도 상세처럼 고유 효과 · 패시브 · 고학년을 함께 보이는 곳은 Traits(사도) — 고학년 → 고유 효과 → 패시브 세 칸을 늘 따로(2026-10-06 사용자 「패시브 칸 따로」).

        /// <summary>고유 효과 글(= Keyword) — 부제(desc) + 규칙에서 만든 줄.</summary>
        public string Short(KeywordDef k) => Keyword(k);
        /// <summary>고유 효과 글(= Keyword).</summary>
        public string Detail(KeywordDef k) => Keyword(k);
        /// <summary>패시브 규칙 한 줄(= Rule) — 「계기 → 결과 (턴당 N회)」.</summary>
        public string Short(PassiveRule r) => Rule(r);
        /// <summary>패시브 규칙 한 줄(= Rule).</summary>
        public string Detail(PassiveRule r) => Rule(r);
        /// <summary>고학년 효과 전부(이름 · 게이지는 화면이 따로 보인다).</summary>
        public string Short(UltDef u) => Fx(u.Fx);
        /// <summary>고학년 — 「이름」 (게이지 N%) 효과 전부(= Ult).</summary>
        public string Detail(UltDef u) => Ult(u);

        /// <summary>줄 목록 → 글. 둘 이상이면 줄마다 「· 」 머리 · 줄바꿈(Docs/설명글.md §1).</summary>
        public static string Bullets(IList<string> lines)
        {
            var l = lines.Where(x => !string.IsNullOrEmpty(x)).ToList();
            return l.Count == 0 ? "" : l.Count == 1 ? l[0] : string.Join("\n", l.Select(x => "· " + x));
        }

        /// <summary>사도 상세 한 칸 — 종류(고학년 · 고유 효과 · 패시브) · 이름 · 부제(작은 글) · 본문(줄바꿈된 수치 글).</summary>
        public sealed class TraitText
        {
            public string Kind, Name, Sub, Body;
            /// <summary>고유 효과 칸이면 그 키워드.</summary>
            public KeywordDef Keyword;
        }

        /// <summary>규칙이 키워드 이름을 가리키나(계기 · 조건 · 효과 — 안쪽 갈래 · 강화 규칙까지).</summary>
        public static bool Refers(PassiveRule r, string name)
        {
            if (r == null || name == null) return false;
            if (r.When != null && (r.When.Id == name || r.When.Marked == name || r.When.CardSt == name)) return true;
            if (r.Conds != null && r.Conds.Any(c => c.Id == name)) return true;
            return FxRefers(r.Fx, name);
        }
        static bool FxRefers(List<Fx> fx, string name) => fx != null && fx.Any(f => f != null && (f.Id == name || FxRefers(f.Then, name) || (f.Rules != null && f.Rules.Any(r => Refers(r, name)))));

        /// <summary>그 사도의 패시브 가운데 이 고유 효과 아래로 모일 것(그 키워드를 가리키는 규칙 — 여러 키워드를 가리키면 앞의 키워드로).</summary>
        public List<PassiveRule> RulesOf(HeroDef h, KeywordDef k)
        {
            if (h == null || k == null) return new List<PassiveRule>();
            var kws = h.AllKeywords.ToList();
            return h.Passives.Where(r => kws.FirstOrDefault(x => Refers(r, x.Name)) == k).ToList();
        }

        /// <summary>규칙 여럿 → 줄 목록(같은 이름 · 계기 · 조건 · 횟수면 한 줄로 묶는다). 「계기 → 결과 (턴당 N회)」.</summary>
        public List<string> RuleLines(IEnumerable<PassiveRule> rules)
        {
            bool m0 = mine, r0 = inRule; mine = true; inRule = true;
            try { return Group(rules, true, false).Select(g => RuleLine(g.Head, g.Fx, g.Limit)).ToList(); }
            finally { mine = m0; inRule = r0; }
        }

        /// <summary>
        /// 사도 고유 효과 본문 — 그 자원 · 표식 자체의 규칙만(쌓이는 곳 · 최대 · 1당 · 사라짐 · 다 차면 · keywords 의 rules, 부제 desc 는 빼고).
        /// 그 키워드를 쌓는 패시브는 패시브 칸에(2026-10-06 사용자 「패시브 칸 따로」).
        /// </summary>
        public string Trait(HeroDef h, KeywordDef k)
        {
            var c0 = ctxHero; ctxHero = h ?? ctxHero;
            try { return Bullets(KeywordLines(k)); } finally { ctxHero = c0; }
        }

        /// <summary>사도 칸(Traits)의 변신 갈래 이름.</summary>
        public const string FormKind = "변신";

        /// <summary>패시브가 없는 사도의 패시브 칸 이름.</summary>
        public const string NoPassive = "없음";

        /// <summary>
        /// 사도 상세 · 편성 · 정보 창에 보일 칸들 — 고학년 → 고유 효과(자원 · 표식 자체의 규칙) → 패시브(passives 전부, 같은 이름끼리 한 칸 · 없으면 「없음」 한 칸).
        /// 「자세히」 없이 이것 하나를 다 보인다(넘치면 칸 안에서 스크롤).
        /// </summary>
        public List<TraitText> Traits(HeroDef h)
        {
            var o = new List<TraitText>();
            if (h == null) return o;
            var c0 = ctxHero; ctxHero = h;
            try {
            if (h.Ult != null) o.Add(new TraitText { Kind = "고학년", Name = h.Ult.Name, Sub = $"게이지 {h.Ult.Cost}%", Body = Fx(h.Ult.Fx) });
            var kws = h.AllKeywords.ToList();
            foreach (var k in kws) o.Add(new TraitText { Kind = "고유 효과", Name = k.Name, Sub = string.IsNullOrEmpty(k.Desc) ? null : k.Desc.TrimEnd('.'), Body = Trait(h, k), Keyword = k });
            // 변신(2026-10-07 사용자 「네르의 성전 모드 · 디아나(왕년)의 맨주먹 전성기가 뭔지 안 나온다」) — 부제 = desc, 본문 = 규칙에서(FormBody)
            foreach (var fm in h.Forms ?? new List<FormDef>()) o.Add(new TraitText { Kind = FormKind, Name = fm.Name, Sub = string.IsNullOrEmpty(fm.Desc) ? null : fm.Desc.TrimEnd('.'), Body = FormBody(fm) });
            var rest = h.Passives.ToList();
            if (rest.Count == 0) o.Add(new TraitText { Kind = "패시브", Name = NoPassive, Body = "" });
            for (int i = 0; i < rest.Count;)
            {
                string name = rest[i].Name; int j = i + 1;
                while (j < rest.Count && name != null && rest[j].Name == name) j++;
                o.Add(new TraitText { Kind = "패시브", Name = name ?? "패시브", Body = Bullets(RuleLines(rest.GetRange(i, j - i))) });
                i = j;
            }
            } finally { ctxHero = c0; }
            return o;
        }

        // ── 변신 ───────────────────────────────────────────────────────
        static string Ro(string w)
        {
            if (!Ko.HasFinal(w)) return "로";
            var t = w.TrimEnd('」', ')', ' ');
            char c = t[t.Length - 1];
            return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 == 8 ? "로" : "으로";
        }
        static string FormDur(FormDef f) => f.Turns > 0 ? $"{f.Turns}턴" : "전투 끝까지";

        /// <summary>
        /// 변신 본문 — 데이터 desc(부제) 없이 규칙에서 만든 줄(「· 」 머리, 한 줄 = 한 가지): 지속 · 풀리는 계기 · 능력치 · 바뀌는 카드 · 덤 · 패시브 · 풀릴 때 · 다시 쓰면.
        /// 낱말 판(runui CardTerms) · 사도 칸(Traits 「변신」) · 전투 변신 칩이 같이 쓴다. desc 는 화면이 제목 아래 작은 글로 따로 보인다.
        /// </summary>
        public string FormBody(FormDef f)
        {
            var s = new List<string>();
            string dur = f.Turns > 0 ? $"{f.Turns}턴 지속" : "전투 끝까지";
            if (f.Until != null) dur += $" · {WhenText(f.Until).Replace("낼 때마다", "내면")} 풀림";
            s.Add(dur);
            s.AddRange(FormDuring(f, true));
            if (f.Off != null && f.Off.Count > 0) s.Add($"풀릴 때 → {Fx(f.Off)}");
            bool byMax = data != null && data.Heroes.Values.Any(h => h.AllKeywords.Any(k => k.OnMax?.Form == f.Id));
            s.Add(byMax ? "변신 중에 다시 최대가 되어도 그대로" : "다시 쓰면 지속이 처음으로");
            return Bullets(s);
        }

        /// <summary>변신 자세히 — 설명 · 지속 · 풀리는 계기 · 그동안(능력치 · 카드 · 덤 · 패시브) · 풀릴 때.</summary>
        public string Form(FormDef f)
        {
            var s = new List<string>();
            if (!string.IsNullOrEmpty(f.Desc)) s.Add(f.Desc.TrimEnd('.'));
            s.Add(f.Turns > 0 ? $"{f.Turns}턴 지속" : "전투 끝까지");
            if (f.Until != null) s.Add($"{WhenText(f.Until).Replace("낼 때마다", "내면")} 풀림");
            var during = FormDuring(f, true);
            if (during.Count > 0) s.Add("그동안 " + string.Join(" · ", during));
            if (f.Off != null && f.Off.Count > 0) s.Add($"풀릴 때: {Fx(f.Off)}");
            bool byMax = data != null && data.Heroes.Values.Any(h => h.AllKeywords.Any(k => k.OnMax?.Form == f.Id));
            s.Add(byMax ? "변신 중에 다시 최대가 되어도 그대로" : "다시 쓰면 지속이 처음으로");
            return string.Join(". ", s) + ".";
        }

        /// <summary>변신 글(= Form) — 짧은 글 따로 없음.</summary>
        public string Short(FormDef f) => Form(f);
        public string Detail(FormDef f) => Form(f);

        List<string> FormDuring(FormDef f, bool full)
        {
            var s = new List<string>();
            if (f.Mods != null)
                foreach (var kv in f.Mods)
                {
                    int pct = Num.Round(kv.Value * 100);
                    s.Add($"자신의 {(STAT_KO.TryGetValue(kv.Key, out var k) ? k : kv.Key == "heal" ? "회복량" : kv.Key)} {(pct >= 0 ? "+" : "")}{pct}%");
                }
            if (f.Cards != null)
                foreach (var kv in f.Cards)
                {
                    var to = data?.Card(kv.Value);
                    s.Add(full && to != null ? $"{Q(CardName(kv.Key))} → {Q(to.Name)}({Card(to).Replace("\n", " / ")})" : $"{Q(CardName(kv.Key))} → {Q(CardName(kv.Value))}");
                }
            foreach (var b in f.Bonus ?? new List<FormBonus>())
            {
                string who = b.Card != null ? Q(CardName(b.Card)) : $"자신의 {(b.Unique ? "고유 " : "")}{(b.Tag != null ? Q(b.Tag) + " " : "")}{(b.Type != null ? b.Type + " " : "")}카드";
                var add = new List<string>();
                if (b.Ratio > 0 && b.Ratio != 1) add.Add($"피해 ×{N(b.Ratio)}");
                foreach (var t in b.Tags ?? new List<string>()) add.Add($"{t}.");
                if (b.Fx != null && b.Fx.Count > 0) add.Add(Fx(b.Fx));
                if (add.Count > 0) s.Add($"{Ko.J(who.TrimEnd(), "을를")} 내면 → {string.Join(" ", add)}");
            }
            if (f.Replace) s.Add("원래 패시브는 쉼");
            if (f.Passives != null && f.Passives.Count > 0) s.Add(full ? Passives(f.Passives) : string.Join(" · ", f.Passives.Select(r => $"{r.Name ?? "패시브"}: {Short(r)}")));
            return s;
        }

        /// <summary>패시브 여럿(= Passives) — 짧은 글 따로 없음.</summary>
        public string ShortPassives(List<PassiveRule> rules) => Passives(rules);

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

        /// <summary>고유 효과 글 — 부제(desc) 한 줄 + 규칙에서 만든 줄(KeywordLines). 그 키워드를 쓰는 패시브까지 보려면 Trait.</summary>
        public string Keyword(KeywordDef k)
        {
            var body = Bullets(KeywordLines(k));
            return string.IsNullOrEmpty(k.Desc) ? body : k.Desc.TrimEnd('.') + "\n" + body;
        }

        string OwnerName(KeywordDef k) => (ctxHero != null && ctxHero.AllKeywords.Contains(k) ? ctxHero : OwnerOf(k))?.Name;

        /// <summary>
        /// 고유 효과 자신의 줄(Docs/설명글.md §4) — 쌓는 곳 · 최대 → 1당 → 성질(약점 · 대신 맞음 · 태그 · 퍼짐) → 사라짐 → 변신 → 자기 규칙.
        /// 엔진이 실제로 하는 것만 적는다(예: 「발동하면 사라진다」 는 1당 증감이 카드에 실릴 때만 엔진이 줄이므로 그때만).
        /// </summary>
        public List<string> KeywordLines(KeywordDef k)
        {
            var c0 = ctxHero; ctxHero ??= OwnerOf(k);
            try { return KeywordLinesIn(k); } finally { ctxHero = c0; }
        }
        List<string> KeywordLinesIn(KeywordDef k)
        {
            var s = new List<string>();
            string q = Q(k.Name), car = k.Carrier ?? "self";
            string Has(bool not = false) => $"{Ko.J(q, "이가")} {(not ? "없" : "있")}";
            // 쌓는 곳 · 최대
            var head = new List<string>();
            if (car == "enemy") head.Add("적에게 쌓임");
            else if (car == "ally") head.Add("파티에 쌓임");
            else if (car == "hero") head.Add(k.Cap == 1 ? "아군에게 붙이는 표시" : "아군에게 붙임(사도마다 따로)");
            else head.Add(k.Mode ? "자신의 상태" : "자신에게 쌓임");
            if (k.Mode) head.Add("켜짐 · 꺼짐 둘 중 하나(전환)");
            else if (k.Cap != null)
            {
                if (k.Wrap && k.Stages != null && k.Stages.Count == k.Cap) head.Add($"{string.Join(" → ", k.Stages)} → 다시 {k.Stages[0]}");
                else if (k.Wrap) head.Add($"1 → {k.Cap} 다음은 다시 1");
                else if (car == "enemy") head.Add($"적마다 최대 {k.Cap}");
                else if (car == "hero") head.Add(k.Cap == 1 ? "사도마다 하나" : $"사도마다 최대 {k.Cap}");
                else head.Add($"최대 {k.Cap}");
            }
            if (k.Hunt) head.Add(car == "hero" ? "한 번에 아군 1명에게만(다른 아군에게 붙이면 옮겨 감)" : "한 번에 적 1명에게만(다른 적에게 쌓으면 옮겨 가고 처음부터)");
            if (k.Reserve) head.Add("예약 — 재촉으로 감소");
            s.Add(string.Join(" · ", head));
            // 1당
            string owner = OwnerName(k);
            foreach (var p in k.Per)
            {
                if (p.Stat == "dot")
                    s.Add(car == "self" ? $"내 턴이 끝날 때 → 무작위 적 1명에게 {q} 1당 공격력 {P(p.Ratio)} 피해"
                        : car == "enemy" ? $"내 턴이 끝날 때 → 그 적에게 {q} 1당 공격력 {P(p.Ratio)} 피해"
                        : $"내 턴이 끝날 때 → 파티에 {q} 1당 공격력 {P(p.Ratio)} 피해");
                else if (p.Stat == "hot") s.Add($"내 턴이 끝날 때 → {q} 1당 HP 회복(자신의 방어력 {P(p.Ratio)})");
                else
                {
                    string stat = STAT_KO.TryGetValue(p.Stat, out var sk) ? sk : p.Stat;
                    int pct = Num.Round(p.V * 100);
                    string v = $"{(pct >= 0 ? "+" : "")}{pct}%";
                    if (car == "enemy")
                    {
                        if (p.From == "owner")
                        {
                            s.Add($"{q} 1당 그 적이 {owner ?? "건 사도"}에게 받는 피해 {v}");
                            if (p.Else != 0) { int e = Num.Round(p.Else * 100); s.Add($"{Has(true)}는 적은 {owner ?? "건 사도"}에게 받는 피해 {(e >= 0 ? "+" : "")}{e}%"); }
                        }
                        else s.Add($"{q} 1당 그 적의 {stat} {v}");
                    }
                    else if (car == "hero") s.Add($"{q} 1당 붙은 아군의 {stat} {v}");
                    else if (car == "ally" || p.Who == "allies") s.Add($"{q} 1당 {stat} {v}");   // 표시 없음 = 파티
                    else s.Add($"{q} 1당 자신의 {stat} {v}");
                }
            }
            // 성질
            if (k.Weakens) s.Add($"{Has()}는 적은 아군 카드에 약점으로 맞음 — 카드 1장이 칠 때마다 −1");
            if (k.Guard) s.Add(k.Cut > 0 ? $"적의 공격 한 대마다 → 그 피해 −{P(k.Cut)}, {q} −1" : $"적의 공격 한 대를 대신 받음(파티는 안 맞음) → {q} −1");
            if (k.TagWhile != null) s.Add($"{Has()}으면 자신의 {k.TagType ?? "공격"} 카드에 「{k.TagWhile}」");
            if (k.Spread) s.Add($"{Ko.J(q, "이가")} 0이면 자신의 공격 카드(적 1명)가 적 전체를 침");
            // 사라짐
            if (k.DecayAll) s.Add("적의 차례가 끝나면 → 모두 사라짐");
            else if (k.Decay > 0) s.Add($"적의 차례가 끝나면 → −{k.Decay}");
            bool uses = car != "hero" && k.Per.Any(p => p.Stat == "dealt" || p.Stat == "atk" || p.Stat == "crit" || p.Stat == "taken");
            if (k.Consumes && uses)
            {
                bool critOnly = k.Per.All(p => p.Stat == "crit");
                string by = car == "self" ? (critOnly ? "자신의 공격 카드가 치명타를 내면" : "자신의 공격 카드를 내면(1당 덤이 실린 뒤)")
                    : car == "enemy" ? (critOnly ? "그 적이 치명타를 맞으면" : "그 적이 아군 카드에 맞으면")
                    : critOnly ? "공격 카드가 치명타를 내면" : "공격 카드를 내면";
                s.Add($"{by} → {(k.ConsumeAll ? "모두 사라짐" : $"−{k.Consume}")}");
            }
            if (k.Wipe) s.Add(k.EndClear ? "다른 사도의 카드를 내거나 내 턴이 끝나면 → 모두 사라짐" : "다른 사도의 카드를 내면 → 모두 사라짐");
            else if (k.EndClear) s.Add("내 턴이 끝나면 → 모두 사라짐");
            if (!k.EndClear && k.EndDecay > 0) s.Add($"내 턴이 끝나면 → −{k.EndDecay}");
            if (k.OnMax != null) { var fd = data?.Form(k.OnMax.Form); string nm = fd?.Name ?? k.OnMax.Form; s.Add($"최대가 되면{(k.OnMax.Consume ? " 모두 써서" : "")} → {Q(nm)}{Ro(nm)} 변신{(fd != null ? $"({FormDur(fd)})" : "")}"); }
            // 자기 규칙 — 계기 · 조건 · 횟수가 같은 잇단 규칙은 효과를, 효과가 같은 잇단 규칙은 계기를 한 줄로 묶는다
            bool a0 = kwAlly, m0 = mine, r0 = inRule; kwAlly = car == "ally"; mine = true; inRule = true;
            try { foreach (var g in Group(k.Rules, false, true)) s.Add(SelfRule(k.Name, g)); }
            finally { kwAlly = a0; mine = m0; inRule = r0; }
            return s;
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
                "charge" when it.Next?.T == "ult" => $"고학년 예고 — 다음 턴 {Intent(it.Next)}",
                "charge" => $"힘 모으기 — 다음 턴 {Intent(it.Next)}",
                "ult" => $"고학년 「{it.Say}」: {string.Join(" · ", (it.Then ?? new List<Intent>()).Select(x => Intent(x)))}",
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
                "reshuffle" => $"손패를 모두 버리고 섞기{(it.Id != null ? $" · {Q(CardName(it.Id))} {Math.Max(1, it.N)}장" : "")}",
                "autoPlay" => $"손패 {Math.Max(1, it.N)}장 저절로 나감",
                "shift" => $"수 바꾸기 — {Intent(it.Next)}",
                "revive" => $"재 속 — {Math.Max(1, it.N)}턴 뒤 HP {(it.V > 0 ? it.V : 50)}%로 부활",
                "feign" => "쓰러진 척 — 적이 회복하면 일어섬",
                "thorns" => $"가시 {it.V}",
                "selfHeal" => $"스스로 회복 {it.V}",
                "brace" => $"강인도 회복 {it.V}",
                _ => it.T,
            };
            if (it.Id != null && (it.T == "attack" || it.T == "back" || it.T == "attackAll")) s += $" · 파티 {it.Id} {Math.Max(1, it.N)}";
            if (it.Id != null && it.T == "multi") s += $" · 한 대마다 {it.Id} {Math.Max(1, it.Per)}";
            if (it.Tough > 0) s += $" · {(it.All ? "적 전체 " : "")}강인도 회복 {N(it.Tough)}";
            if (it.Brk && !(it.T == "charge" && it.Next?.T == "ult")) s += " · 격파하면 끊김";   // 고학년 예고는 안의 고학년 글이 적는다
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
                case "unique": return $"은총: 고른 사도의 고유 카드 {Math.Max(1, o.N)}";
                case "neutral": return o.Grade != null ? $"교주 카드 ({o.Grade})" : "교주 카드";
                case "equip": return $"{o.Slot ?? "장비"} ({o.Grade})";
                case "flash": return o.Swap ? "신탁 바꾸기 1" : "신탁 1";
                case "shin": return $"축복 {Num.Round(o.V * 100)}%";
                case "noShin": return "축복 막힘";
                case "shinNow": return "축복 1";
                case "shinPick": return $"축복 카드 {Math.Max(1, o.N)}{(o.Kind == "cost" ? " (비용)" : o.Kind == "power" ? " (위력)" : "")}";
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
            var lines = new List<string> { $"{h.Name} — {h.Role} · {h.Nature ?? "성격 없음"}" };
            lines.AddRange(TraitSheet(h));
            foreach (var f in h.Forms ?? new List<FormDef>()) lines.Add($"변신 「{f.Name}」 {Form(f)}");
            return string.Join("\n", lines);
        }

        /// <summary>Traits 를 글 줄로 — 「고유 효과 「깃발」 — 부제」 + 들여 쓴 본문.</summary>
        List<string> TraitSheet(HeroDef h) => Traits(h).Select(t => $"{t.Kind} 「{t.Name}」{(string.IsNullOrEmpty(t.Sub) ? "" : " — " + t.Sub)}\n" + string.Join("\n", t.Body.Split('\n').Select(x => "    " + x))).ToList();

        public string Hero(HeroDef h)
        {
            var lines = new List<string>
            {
                $"{h.Name} — {h.Role} · {h.Nature ?? "성격 없음"}{(h.Style != null ? " · " + h.Style : "")}",
                $"HP {h.Hp} · 공격력 {h.Atk} · 방어력 {h.Def} · 치명 {h.Crit}%",
            };
            lines.AddRange(TraitSheet(h));
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
        // 수치는 Rules.cs 에서 바로 읽는다(글과 엔진 값이 어긋나지 않게) — Docs/설명글.md §5 「누구에게 · 무엇이 얼마나 · 지속」.
        static string Pc(string id) => Num.Round(R.SV(id) * 100).ToString(CultureInfo.InvariantCulture);
        static string Mx(string id) => ((int)(R.SMax(id) ?? 0)).ToString(CultureInfo.InvariantCulture);

        /// <summary>엔진 키워드 설명(Docs/키워드.md 뜻을 줄인 우리 말) — 칩 · 툴팁 · 밑줄 낱말 판. 「자세히」 없이 이 글 하나에 수치 · 지속이 다 든다.</summary>
        public static readonly IReadOnlyDictionary<string, string> TIPS = new Dictionary<string, string>
        {
            // 전투 규칙
            ["행동 카운트"] = "적이 행동하기까지 남은 카드 수 · 카드 1장마다 −1(신속 카드는 그대로) · 0이 되면 그 적이 바로 행동",
            ["강인도"] = "적의 버티는 힘 · 카드 1장이 적 1명을 처음 칠 때 감소 — 약점 공격은 비용 1당 1, 아니면 1/3 · 비용 0 카드는 1/2, 적 전체 공격은 대상마다 절반 · 0이 되면 격파 · 저절로 차지 않음(회복 스킬이 있는 적만 되찾음)",
            ["격파"] = $"강인도가 0 → AP +{R.TOUGH.Ap} · 그 적은 다음 차례를 쉬고, 다음 내 턴 시작에 강인도가 가득 참",
            ["붕괴"] = "이 카드로 적을 격파하면 → 뒤의 효과", ["처치"] = "이 카드로 적을 쓰러뜨리면 → 뒤의 효과", ["파괴"] = "이 카드의 대상이 쓰러져 있으면 → 뒤의 효과",
            ["약점 공격"] = $"약점을 치는 공격 · 피해 +{Num.Round(R.WEAK_DMG * 100)}% · 강인도 피해 비용 1당 1(약점이 아니면 1/3)",
            ["방어 기반 피해"] = $"카드 주인의 방어력 {Num.Round(R.DEF_DMG.def * 100)}% + 공격력 {Num.Round(R.DEF_DMG.atk * 100)}% 를 바탕으로 한 피해",
            ["고정 피해"] = "증감 · 상태 효과를 받지 않는 피해",
            ["고정 지속 피해"] = "증감 · 상태 · 실드를 모두 무시하고 HP에 바로 드는 피해", ["고정 실드"] = "증감을 받지 않는 실드",
            ["피해 기반 회복"] = $"이 카드가 준 피해의 적힌 %만큼 파티 HP 회복 · 한 번에 최대 HP {Pc("흡수Max")}%까지",
            ["부상"] = $"파티 HP가 최대의 {Pc("부상")}% 미만인 상태",
            ["추가 공격"] = "카드와 따로 들어가는 공격(협공 · 표식 · 공명 …) · 사기 적용",
            // 카드 태그
            ["보존"] = "턴이 끝나도 손에 남음", ["소멸"] = "내면 이번 전투에서 빠짐 · 소멸 N = N번 내면 빠짐", ["증발"] = "턴이 끝날 때 손에 있으면 소멸",
            ["회수"] = "내면 버린 더미 대신 손으로 돌아옴 · 회수 N = N번까지", ["망각"] = "소멸하는 대신 뽑을 더미 맨 위로", ["제거"] = "내면 이번 전투와 덱에서 완전히 빠짐",
            ["개전"] = "전투 시작 시 첫 손패에 포함", ["개막"] = "전투 시작 시 AP를 치르고 저절로 나감(AP가 모자라면 안 나감)",
            ["종극"] = "내면 바로 턴 종료", ["신속"] = "내도 적의 행동 카운트가 줄지 않음", ["주도"] = "턴 시작에 50% 확률로 비용 −1 · 다른 카드를 먼저 내면 사라짐",
            ["연결"] = "직접 내면 손의 다른 연결 카드를 모두 버림", ["봉인"] = "처음 내면 효과 없이 봉인만 풀림", ["사용 불가"] = "낼 수 없음",
            ["연계"] = "다른 사도의 카드가 나가면 손에서 저절로(AP 없이) 나감", ["천상"] = "비용 2 이상인 카드가 나가면 손에서 저절로 나감",
            ["연속"] = "바로 앞 카드와 성격이 같으면 → 뒤의 효과", ["조율"] = "이 카드의 비용 = 남은 AP 이면 → 뒤의 효과", ["안식"] = "효과로 버려지면 → 뒤의 효과",
            ["감응"] = "뽑히면(턴 시작 뽑기 포함) → 뒤의 효과", ["영감"] = "카드 효과로 뽑히면 → 뒤의 효과", ["열정"] = "열정 카드가 나가면 → 손에 있는 이 카드의 「열정:」 효과",
            ["소각"] = "소멸할 때 → 뒤의 효과", ["연쇄"] = "다음 턴 시작에 같은 효과가 한 번 더(덤 없이)",
            ["축복"] = $"실드가 최대 HP {Pc("축복At")}% 미만일 때 손에서 버려지면 → 고정 실드(카드 주인 방어력 {Pc("축복")}%)",
            ["유일"] = "덱에 한 장만(복제 안 됨)", ["금기"] = "신탁 · 복제 · 제거 불가", ["봉인된 금기"] = "금기 · 보스를 처치하면 금기 카드로 바뀜",
            // 복제본(보스 뒤 복제 · 이벤트 복제 — Run.AddCopy, 2026-10-06 사용자)
            ["복제본"] = "복제한 순간의 신탁 · 축복 그대로 · 그 뒤 신탁 · 축복 불가(원본이 나중에 받는 것은 따라오지 않음)",
            // 빛나는 카드(전투마다 run.RollEpiphany) — 한 전투에 사도마다 신탁 · 은총 가운데 하나만(2026-10-06 사용자)
            ["신탁"] = "전투 중 빛나는 고유 · 교주 카드를 내면 신탁 셋 가운데 하나를 얹음(이번에는 비용 0) · 내지 않으면 전투가 끝난 뒤 고름 · 한 전투에 사도마다 신탁 · 은총 가운데 하나만(교주 카드는 주인 사도 몫)",
            ["은총"] = "전투 중 빛나는 시작 카드를 내면 그 사도의 고유 카드를 손에 얻음(그 턴 비용 0) · 내지 않으면 전투가 끝난 뒤 받음 · 한 전투에 사도마다 신탁 · 은총 가운데 하나만",
            ["분쇄"] = $"실드가 있는 적을 치면 피해 +{Pc("분쇄")}%", ["탄환"] = "다른 카드 · 장비 · 고유 효과가 세는 카드",
            ["결속"] = "같은 사도의 결속 카드가 한 장으로 겹침(최대 5) · 3 이상이면 강해진 카드로 · 내면 처음으로",
            // 이로운 효과 — 「파티」 = 파티 하나에 걸려 셋 모두에게 든다
            ["사기"] = $"파티 · 사기 1당 카드 피해 계수 +{Pc("사기")}%p(합연산, 최대 {Mx("사기")}) · 전투 끝까지",
            ["불굴"] = $"파티 · 불굴 1당 받는 피해 −{Pc("불굴")}%p(합쳐 최대 −{Pc("불굴Cap")}%) · 전투 끝까지",
            ["결의"] = $"파티 · 결의 1당 얻는 실드 +{R.SV("결의")}(고정값, 최대 {Mx("결의")}) · 전투 끝까지",
            ["결정화"] = $"파티 · 내 턴이 끝날 때 결정화 1당 고정 실드(방어력 {Pc("결정화")}%, 최대 {Mx("결정화")}) · 줄지 않음",
            ["고동"] = $"파티 · 내 턴이 끝날 때 고동 1당 적 전체에 고정 피해 {Pc("고동")}%(최대 {Mx("고동")}) · 줄지 않음",
            ["협공"] = $"파티 · 아군이 공격 카드를 내면 → 협공을 건 사도가 그 적에게 추가 공격(공격력 {Pc("협공")}%) · 쓸 때마다 −1",
            ["반격"] = $"파티 · 적에게 맞으면 → 반격을 건 사도가 그 적에게 방어 기반 피해 {Pc("반격")}%(실드로 다 막으면 {Pc("반격Full")}%, 치명 적용) · 쓸 때마다 −1 · 최대 {Mx("반격")}",
            ["피해 감소"] = $"파티 · 받는 피해 −{Pc("피해 감소")}% · 맞을 때마다 −1", ["면역"] = "파티 · 디버프 1개를 막음 · 막을 때마다 −1",
            ["실드 유지"] = $"파티 · 턴이 바뀔 때 실드의 {Pc("실드 유지")}%가 남음 · 쓸 때마다 −1",
            ["실드 보존"] = "파티 · 턴이 바뀔 때 실드가 그대로 남음 · 쓸 때마다 −1", ["저장"] = "파티 · 남은 AP를 다음 턴으로 넘김 · 쓸 때마다 −1",
            ["잔광"] = $"파티 · 공격 카드의 강인도 피해 +{R.TOUGH.Glow} · 격파 상태인 적이면 피해 +{Pc("잔광")}% · 공격 카드 1장마다 −1",
            ["공명"] = $"파티 · 카드를 버리면 → 버린 사도가 추가 공격(공격력 {Pc("공명")}%) · 버린 카드 1장마다 −1",
            ["탄성"] = $"파티 · 아군이 추가 공격하면 → 탄성 1당 HP 회복(방어력 {Pc("탄성")}%, 최대 {Mx("탄성")}) · 한 번 들면 모두 사라짐",
            ["칼날 벼리기"] = $"파티 · 1턴 · 칼날 벼리기 1당 공격 카드의 방어 기반 피해 +{Pc("칼날 벼리기")}%(최대 {Mx("칼날 벼리기")})",
            ["빙벽"] = "파티 · 1턴 · 맞을 때마다 반격(반격 수를 쓰지 않음)", ["구속"] = "그 사도 · 자기 카드 말고는 AP를 얻거나 잃지 않음 · 턴마다 −1",
            ["형상 강화"] = $"파티 · 1턴 · 형상 강화 1당 만든 카드의 피해 · 실드 · 회복 +{Pc("형상 강화")}%(최대 {Mx("형상 강화")})",
            ["행동 둔화"] = $"파티 · 신속 카드를 내면 → 1턴간 적 전체 주는 피해 −{Pc("행동 둔화")}% · 쓸 때마다 −1",
            ["급속"] = $"적 · 행동 카운트 −N(최대 {Mx("급속")}) · 그 적이 행동하면 사라짐",
            ["근면"] = "파티 · 적을 처치하면 → AP +1 · 드로우 1 — 카드 하나에 한 번, 한 턴에 근면 수만큼 · 줄지 않음",
            ["계몽"] = "파티 · 고학년을 쓰면 → 드로우 1 · 줄지 않음",
            ["집중"] = $"파티 · 턴 시작에 파티 HP가 {Pc("집중")}% 미만이면 → AP +1 · 줄지 않음", ["회피"] = "파티 · 적의 공격 한 대에 HP를 잃지 않음 · 한 대마다 −1",
            ["다음 턴 드로우"] = "파티 · 다음 턴 시작에 수만큼 더 드로우 후 사라짐",
            ["초재생"] = $"파티 · 턴 시작에 HP 회복(방어력 {Pc("초재생")}%) · 턴마다 −1", ["탐구심"] = $"이 카드 · 탐구심 1당 피해 +{Pc("탐구심")}%",
            ["절대 무적"] = "파티 · 1턴 · HP를 잃지 않음(치른 HP는 예외)", ["끈기"] = "파티 · 쓰러질 피해를 받으면 HP 1로 버팀 · 버틸 때마다 −1",
            // 해로운 효과 — 걸린 쪽(파티 · 적)
            ["취약"] = $"받는 피해 +{Pc("취약")}% · 맞을 때마다 −1", ["약화"] = $"주는 피해 −{Pc("약화")}% · 칠 때마다 −1 · 파티에 걸리면 모든 사도가",
            ["손상"] = $"얻는 실드 −{Pc("손상")}% · 얻을 때마다 −1",
            ["고통"] = $"턴이 끝날 때 고통 1당 고정 지속 피해 {Pc("고통")}%(건 쪽 공격력, 최대 {Mx("고통")}) · 그 뒤 절반으로", ["고통 각인"] = $"고통을 얻을 때마다 +{R.SV("고통 각인")} 더",
            ["균열"] = $"턴이 끝날 때 균열 1당 지속 피해 {Pc("균열")}%(최대 {Mx("균열")}) · 그 뒤 절반으로",
            ["그을림"] = $"행동 카운트가 1 줄 때마다 지속 피해 {Pc("그을림")}%(최대 {Mx("그을림")}) · 들 때마다 −1 · 턴이 끝나면 사라짐",
            ["표식"] = $"공격 카드에 처음 맞으면 → 추가 공격(공격력 {Pc("표식")}%) + 강인도 피해 · 맞을 때마다 −1",
            ["잔불"] = $"격파 상태에서 맞거나 쓰러질 공격을 맞으면 잔불 1당 그 피해 +{Pc("잔불")}%(최대 {Mx("잔불")}) · 한 번 들면 사라짐",
            ["충격"] = $"공격 카드의 대상이 되면 고정 피해 {Pc("충격")}%(실드가 있으면 +{Pc("충격Shield")}%) · 들 때마다 −1",
            ["충격파"] = $"카드로 공격받으면 → 다른 모두에게 고정 피해 {Pc("충격파")}% · 들 때마다 −1",
            ["응징"] = $"실드를 얻으면 → 그쪽 모두에게 고정 피해 {Pc("응징")}%(건 쪽 공격력) · 들 때마다 −1",
            ["포자증식"] = $"포자증식 1당 받는 피해 +{Pc("포자증식")}% · 한 번 들면 모두 사라짐",
            ["죽음의 낙인"] = $"낙인을 건 그 카드로 치면 피해 +{Pc("죽음의 낙인")}%", ["열정 약점"] = "1턴 · 열정 카드가 약점 공격으로 들어감",
            ["정신 붕괴"] = "고학년 · 신탁 사용 불가 · 턴마다 −1 · 남은 채 전투가 끝나면 다음 전투까지 카드 얻기 · 신탁 · 제거 불가",
            ["둔화"] = $"적 · 행동 카운트 +N(최대 {Mx("둔화")}) · 그 적이 행동하면 사라짐",
            ["미끄러움"] = "카드를 내면 손에서 무작위 1장이 버려짐 · 낼 때마다 −1", ["봉쇄"] = "이 카드 · 내도 효과가 없고 봉쇄만 풀림",
            ["침체"] = "이 카드 · 비용 +1 · 내면 풀림", ["빙결"] = "이 카드 · 뽑은 턴에는 낼 수 없음", ["독"] = "이 카드 · 내면 파티가 적힌 만큼 피해(실드 무시) · 한 번 터지면 사라짐",
            ["기절"] = "적 · 다음 차례를 쉼",
        };

        /// <summary>엔진 키워드 하나의 짧은 설명(없으면 null).</summary>
        public static string Tip(string keyword) => keyword != null && TIPS.TryGetValue(Tag.Parse(keyword).id, out var t) ? t : null;

        /// <summary>칩 · 툴팁 표 한 벌 — 엔진 키워드(TIPS) + 데이터의 사도 고유 효과(이름 → 설명) + 적의 쌓이는 수치. UI 는 이것 하나로 칩을 그린다.</summary>
        public Dictionary<string, string> Tips()
        {
            var o = TIPS.ToDictionary(kv => kv.Key, kv => kv.Value);
            if (data == null) return o;
            foreach (var h in data.Heroes.Values) foreach (var k in h.AllKeywords) if (k.Name != null) o[k.Name] = $"{h.Name}의 고유 효과{(string.IsNullOrEmpty(k.Desc) ? "" : " — " + k.Desc.TrimEnd('.'))}\n{Trait(h, k)}";
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
                        case FxK.Power: foreach (var r in f.Rules ?? new List<PassiveRule>()) Walk(r.Fx); break;
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
            if (c.ResetTurnStart) s.Add($"턴 시작에 {c.Start}{(Ko.HasFinal(c.Start.ToString()) && c.Start % 10 != 1 && c.Start % 10 != 7 && c.Start % 10 != 8 ? "으로" : "로")}");
            if (c.OnTurnStart != 0) s.Add($"턴 시작에 {(c.OnTurnStart > 0 ? "+" : "")}{c.OnTurnStart}");
            if (c.ClearTurnEnd) s.Add("턴이 끝나면 사라짐");
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
            foreach (var r in e.Rare) l.Add($"희귀종 — {(R.RARES.TryGetValue(r.Id ?? "", out var k) ? k : r.Id)}{(r.Card != null ? $" ({Q(CardName(r.Card))})" : "")}{(r.Id == "actDebuff" ? $" ({r.St ?? "취약"})" : "")}");
            if (e.Soul) l.Add("영혼 공유 — 맨 앞이 아니면 피해를 받지 않음");
            if (e.Tied) l.Add("세운 이가 쓰러지면 같이 쓰러짐");
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
                case "lowHp": return $"HP가 {Num.Round(p.At * 100)}% 이하가 되면";
                case "allyDown": return "동료가 쓰러지면";
                case "card": return p.Same ? $"같은 사도의 {ty}카드를 잇달아 {(p.Every > 1 ? p.Every + "장 " : "")}내면" : $"{ty}카드를 {(p.Every > 1 ? p.Every + "장 " : "")}내면";
                case "rushed": return "즉시 행동한 뒤";
                case "debuffed": return "디버프를 받으면";
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
