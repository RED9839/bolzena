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
            return st == null ? null : $"{st}{(Ko.HasFinal(st) ? "이면" : "면")}";   // 「양산이면」 — 단계 이름이 고유 효과를 말한다(엘다인 글 길이 2026-10-11)
        }

        /// <summary>두 단계 순환(자세 — 양산 ↔ 칼)인가 — v 1 쌓기는 「바꿈」 으로 읽힌다(엘다인 ▶E33).</summary>
        bool Swap2(string kwId)
        {
            KeywordDef hit = ctxHero?.AllKeywords.FirstOrDefault(k => k.Name == kwId) ?? data?.Heroes.Values.SelectMany(h => h.AllKeywords).FirstOrDefault(k => k.Name == kwId);
            return hit != null && hit.Wrap && hit.Cap == 2 && hit.Stages != null && hit.Stages.Count == 2;
        }
        /// <summary>맞은 적마다 붙는 글 — 앞의 「적 1명에게 」 · 「그 적에게 」 를 덜어 낸다.</summary>
        static string StripFoe(string t)
        {
            foreach (var p in new[] { "적 1명에게 ", "그 적에게 ", "적 1명 ", "그 적 " }) if (t.StartsWith(p, StringComparison.Ordinal)) return t.Substring(p.Length);
            return t;
        }

        string Carrier(string kwId)
        {
            if (kwId == null) return null;
            // 글을 짓는 사도의 것이 먼저(디아나 「제자」 ↔ 밍스 「제자」 처럼 이름이 겹친다)
            if (ctxHero != null) foreach (var k in ctxHero.AllKeywords) if (k.Name == kwId) return k.Carrier ?? "self";
            if (data == null) return null;
            // 데이터 전체에서 찾는 몫은 글을 지을 때마다 같다 — 처음 찾은 것을 담아 둔다(사도 135 × 키워드를 매번 돌지 않게)
            if (carrierOf.TryGetValue(kwId, out var hit)) return hit;
            string found = null;
            foreach (var h in data.Heroes.Values) { foreach (var k in h.AllKeywords) if (k.Name == kwId) { found = k.Carrier ?? "self"; break; } if (found != null) break; }
            carrierOf[kwId] = found;
            return found;
        }
        readonly Dictionary<string, string> carrierOf = new();

        // ── 대상 말 ───────────────────────────────────────────────────
        static string Who(string target, string fallback = "적 1명") => target switch
        {
            "oneEnemy" => "적 1명", "allEnemies" => "적 전체", "randomEnemy" => "무작위 적", "self" => "자신", "oneAlly" => "아군 1명",
            "otherEnemy" => "다른 적 1명", "nextEnemy" => "행동 카운트가 가장 작은 적", "slowestEnemy" => "행동 카운트가 가장 큰 적", "farEnemy" => "가장 먼 적", "markedEnemy" => "표식이 가장 많은 적", "strongestAlly" => "공격력이 가장 높은 아군", "bigHitEnemy" => "공격 예고가 가장 큰 적",
            "allAllies" => "파티", "otherAllies" => "아군", "party" => "파티", "topEnemy" => "HP가 가장 높은 적", "lowEnemy" => "HP가 가장 낮은 적", null => fallback, _ when target.StartsWith("hero:", StringComparison.Ordinal) => $"{target.Substring(5)}(없으면 공격력이 가장 높은 아군)", _ => fallback,
        };
        static string WhoTo(string target) => target switch
        {
            "allEnemies" => "적 전체에", "randomEnemy" => "무작위 적에게", "otherEnemy" => "다른 적 1명에게", "nextEnemy" => "행동 카운트가 가장 작은 적에게", "slowestEnemy" => "행동 카운트가 가장 큰 적에게", "farEnemy" => "가장 먼 적에게", "markedEnemy" => "표식이 가장 많은 적에게", "strongestAlly" => "공격력이 가장 높은 아군에게", "bigHitEnemy" => "공격 예고가 가장 큰 적에게", "topEnemy" => "HP가 가장 높은 적에게", "lowEnemy" => "HP가 가장 낮은 적에게", "self" => "자신에게", "oneAlly" => "아군 1명에게", "allAllies" => "사도마다", "otherAllies" => "아군마다", "party" => "파티에", _ when target != null && target.StartsWith("hero:", StringComparison.Ordinal) => $"{target.Substring(5)}(없으면 공격력이 가장 높은 아군)에게", _ => "적 1명에게",
        };

        // ── 카드 ───────────────────────────────────────────────────────
        /// <summary>카드 면의 글 — 태그 + 효과(신탁을 얹은 모습이면 신탁의 글).</summary>
        public string Card(CardView c) { choices = c.Def.Choices; powerCard = c.Type == "강화"; countAs = c.CountAs; needForm = c.NeedForm; costIf = c.CostIf; bool l0 = lines; lines = true; try { return CardLines(c.Def, c.Tags, c.Fx); } finally { choices = null; powerCard = false; lines = l0; countAs = 0; needForm = null; costIf = null; } }
        public string Card(CardDef c) { choices = c.Choices; powerCard = c.Type == "강화"; countAs = c.CountAs; needForm = c.NeedForm; costIf = c.CostIf; bool l0 = lines; lines = true; try { return CardLines(c, c.Type == "강화" && !c.Tags.Contains(Tag.Unique) ? new[] { Tag.Unique }.Concat(c.Tags) : c.Tags, c.Fx); } finally { choices = null; powerCard = false; lines = l0; countAs = 0; needForm = null; costIf = null; } }
        /// <summary>(2026-10-13 이드) 「손에 그 카드가 있으면 비용 +N」 — 카드 글 머리 줄.</summary>
        CostIfDef costIf;
        string CostIfLine(CostIfDef ci) => ci?.InHand == null ? "" : $"손에 {Ko.J(Q(CardName(ci.InHand)), "이가")} 있으면 비용 +{ci.Add}";
        /// <summary>효과에 같은 카드의 「손에 … 있으면」(ifInHand) 이 있으면 따로 줄을 두지 않고 그 조건에 「비용 +N,」 을 붙인다(45자).</summary>
        static bool CostIfMerged(List<Fx> fx, CostIfDef ci) => ci?.InHand != null && fx != null && fx.Any(f => f.K == FxK.IfInHand && !f.Not && f.Id == ci.InHand);
        /// <summary>(엘다인 ▶E2) 천상에서 셀 비용 — 카드 글 끝 줄.</summary>
        int countAs;
        /// <summary>(엘다인 4차 태세) 그 변신 · 태세 중에만 낼 수 있는 카드 — 카드 글 첫 줄.</summary>
        string needForm;
        string NeedFormLine(CardDef d)
        {
            if (needForm == null) return "";
            if (needForm == "*") return "변신 중에만 사용 가능";
            var fd = data?.Form(needForm) ?? data?.Hero(d.Hero)?.Forms?.FirstOrDefault(x => x.Id == needForm);
            return $"{Q(fd?.Name ?? needForm)} 중에만 사용 가능";
        }
        /// <summary>카드 글을 짓는 사도(변신 해제 글에 변신 이름을 넣으려고).</summary>
        string formHero;
        /// <summary>그 사도의 변신 이름 — 변신이 하나뿐일 때만(여럿이면 null).</summary>
        string FormNameOf(string hero)
        {
            var fs = hero == null ? null : data?.Hero(hero)?.Forms;
            return fs != null && fs.Count == 1 ? fs[0].Name : null;
        }
        /// <summary>변신 중에 내면 변신이 풀리는 카드(변신의 덤 효과 formEnd) — 변신 밖의 카드 글에도 미리 알린다. 효과에 이미 formEnd 가 있으면(변신 중 모습) 따로 안 붙인다.</summary>
        string FormEndNote(CardDef d, List<Fx> fx)
        {
            if (d.Hero == null || (fx != null && fx.Any(f => f.K == FxK.FormEnd))) return "";
            string id = GameData.BaseId(d.Id);
            foreach (var fm in data?.Hero(d.Hero)?.Forms ?? new List<FormDef>())
                foreach (var b in fm.Bonus ?? new List<FormBonus>())
                    if (b.Card == id && b.Fx != null && b.Fx.Any(f => f.K == FxK.FormEnd)) return $"{Q(fm.Name)} 중에 내면 변신 해제";
            return "";
        }
        /// <summary>(2026-10-13 사용자) 탄환 카드의 발사 줄 — 풀이가 키워드 칩(툴팁)에만 있으면 칩이 없는 사도 상세 · 카드 도감에서 안 보여서 카드 본문에 적는다.</summary>
        static string BulletLine(IEnumerable<string> tags, CardDef d = null) => tags != null && tags.Contains(Tag.Bullet) ? $"{Reg("버린 더미로 가면")} 무작위 적에게 추가 공격 {(d != null && d.BulletRatio > 0 ? Num.Round(d.BulletRatio * 100).ToString(CultureInfo.InvariantCulture) : Pc("탄환"))}%" : "";   // 카드마다 배율(bulletRatio)
        /// <summary>카드 면 — 태그 줄(「소멸. 신속.」) · 머리 줄(AP 빚 · 갈래) · 효과 줄들.</summary>
        string CardLines(CardDef d, IEnumerable<string> tags, List<Fx> fx)
        {
            var tagLine = string.Join(" ", (tags ?? Enumerable.Empty<string>()).Select(t => t + ".").Concat(d.HandMax > 0 ? new[] { $"손에 {d.HandMax}장까지." } : new string[0]));   // (2026-10-13 시온 마탄) 손 최대 장수
            formHero = d.Hero;
            string body; try { body = Fx(fx); } finally { formHero = null; }
            var parts = new List<string> { tagLine, NeedFormLine(d), CostIfMerged(fx, costIf) ? "" : CostIfLine(costIf), Head(d).TrimEnd('\n'), body, BulletLine(tags, d), FormEndNote(d, fx), countAs > 0 ? $"천상에서 비용 {countAs}로 센다" : "" };
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
        /// <summary>
        /// 강화 카드의 글을 짓는 중. 2026-10-07 — 세기형 버프(사기 · 결의 …)에 붙이던 「이 전투 동안」 은 뺐다:
        /// 그 상태가 원래 전투 끝까지 남아(키워드 풀이) 강화 카드가 아닌 카드의 「사기 1」 과 뜻이 같다.
        /// </summary>
        bool powerCard;
        List<string> choices;
        /// <summary>카드 머리의 덤 — 고유 효과로 치르는 비용 · AP 빚 · 두 갈래(카드 글에서는 한 줄씩).</summary>
        string Head(CardDef c)
        {
            var s = new List<string>();
            if (c.PayWith != null) s.Add($"비용은 {Q(c.PayWith)}로.");
            if (c.Debt) s.Add("AP 빚 가능 — 모자란 AP만큼 다음 턴 AP -.");
            // 효과 줄이 「불: …」 「물: …」 로 두 갈래를 이미 다 말하면 머리 「갈래 — 불 / 물」 은 같은 말이라 뺀다(2026-10-13 벨라 45자)
            if (c.Choices != null && c.Choices.Count == 2 && !(c.Fx.Any(f => f.K == FxK.IfChoice && f.N == 1) && c.Fx.Any(f => f.K == FxK.IfChoice && f.N == 2))) s.Add($"갈래 — {c.Choices[0]} / {c.Choices[1]}.");
            if (s.Count == 0) return "";
            return lines ? string.Join("\n", s.Select(x => x.TrimEnd('.'))) + "\n" : string.Join(" ", s) + " ";
        }
        public string Oracle(CardDef baseCard, OracleDef o)
        {
            var head = new List<string>();
            if (o.Power) head.Add("강화 카드.");
            if (o.Cost != null && o.Cost != baseCard.Cost) head.Add($"비용 {o.Cost}.");
            if (o.CostIf?.InHand != null && !CostIfMerged(o.Fx, o.CostIf)) head.Add(CostIfLine(o.CostIf) + ".");   // (2026-10-13 이드)
            choices = baseCard.Choices; powerCard = baseCard.Type == "강화" || o.Power; costIf = o.CostIf;
            bool l0 = lines; lines = true;
            string body;
            try { body = Compose(o.Tags, o.Fx); } finally { choices = null; powerCard = false; lines = l0; costIf = null; }
            return string.Join(" ", head) + (head.Count > 0 && body.Length > 0 ? "\n" : "") + body;
        }
        public string Bless(BlessDef b)
        {
            var parts = new List<string>();
            if (b.Kind != null && R.DIVINE_KO.TryGetValue(b.Kind, out var k)) parts.Add(k.Split('—').Last().Trim());   // 끝 마침표 없음 — 효과 글과 같은 꼴(2026-10-12)
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
            var bullet = BulletLine(tags);   // 탄환 발사 줄 — 신탁 · 축복 글도 카드 본문과 같게(2026-10-13)
            if (bullet.Length > 0) body = body.Length == 0 ? bullet : body + (lines ? "\n" : ". ") + bullet;
            return head.Length == 0 ? body : body.Length == 0 ? head : head + (lines ? "\n" : " ") + body;
        }

        // ── 조건 머리 등록부(2026-10-07 화살표 없앰) ──────────────────────
        // 말로 쓴 조건 · 계기는 「X면 Y」 처럼 띄어쓰기 하나로 결과와 잇는다(옛 「X면 → Y」). 화면(runui CardTerms.Fit)은 긴 줄을
        // 「X면,」 / 「Y」 로 나누는데, 글만 보고는 조건이 어디서 끝나는지 모르므로 엔진이 지은 머리를 여기 적어 두고 HeadEnd 로 찾는다.
        static readonly HashSet<string> condHeads = new();
        static readonly object condLock = new();
        /// <summary>말로 쓴 조건 · 계기 머리를 등록하고 그대로 돌려준다.</summary>
        static string Reg(string h)
        {
            if (!string.IsNullOrEmpty(h)) lock (condLock) condHeads.Add(h);
            return h;
        }
        /// <summary>
        /// 글 한 줄(서식 태그 없는 글)에서 말로 쓴 조건 머리가 끝나는 자리 — 머리 바로 뒤 띄어쓰기의 위치. 없으면 -1.
        /// 줄 머리 「· 」 · 강화 규칙의 「이 전투 동안 」 은 건너뛰고 본다. 가장 긴 머리를 고른다(「낼 때마다 그 뒤 손패가 없으면」).
        /// </summary>
        public static int HeadEnd(string line)
        {
            if (string.IsNullOrEmpty(line)) return -1;
            int start = 0;
            if (line.StartsWith("· ", StringComparison.Ordinal)) start = 2;
            const string RUN = "이 전투 동안 ";
            int best = -1;
            lock (condLock)
            {
                if (condHeads.Count == 0) return -1;
                foreach (int s0 in new[] { start, line.IndexOf(RUN, start, StringComparison.Ordinal) == start ? start + RUN.Length : -1 })
                {
                    if (s0 < 0) continue;
                    for (int i = line.IndexOf(' ', s0); i > s0; i = line.IndexOf(' ', i + 1))
                        if (i < line.Length - 1 && condHeads.Contains(line.Substring(s0, i - s0))) best = Math.Max(best, i);
                }
            }
            return best;
        }

        /// <summary>조건 머리가 키워드 꼴(「처치:」 · 「연속:」 · 갈래 이름)인가 — 아니면 말로 쓴 조건(「낸 뒤 손패가 없으면 …」).</summary>
        static bool KwHead(Fx f) => f.K switch
        {
            FxK.IfBroken or FxK.IfTune or FxK.IfChain or FxK.IfLink or FxK.IfPrev or FxK.IfSwitched or FxK.IfRepeat or FxK.IfBreak or FxK.IfChoice or FxK.IfRoll => true,
            FxK.IfKill => f.Id != "orBoss",   // (2026-10-19) 「처치하거나 대상이 보스면」 은 말로 쓴 조건
            FxK.IfWounded => f.Target != "oneEnemy",
            FxK.When => f.On != "handEnd" && f.On != "shieldLeft",   // 말로 쓴 조건(「… 실드가 남았으면」)은 쌍점 없이
            FxK.IfForm => !f.Not && stanceHead,
            _ => false,
        };
        /// <summary>조건 문장에서 기본 효과와 같은 갈래의 효과에 「더」 를 붙인다(조건 효과는 기본 효과에 얹힌다 — 바꾸지 않는다).</summary>
        static string AddKind(string k) => k == FxK.Dmg ? "dmg" : k == FxK.Shield || k == FxK.Block ? "guard" : k == FxK.Heal ? "heal" : null;

        /// <summary>(엘다인 4차) 지금 짓는 조건 머리가 태세 이름(「「흑운태세」: …」)인가 — KwHead 가 본다.</summary>
        [ThreadStatic] static bool stanceHead;
        /// <summary>태세 변신(Stance) — id 가 있으면 그 변신, 없으면 글을 짓는 사도의 유일한 태세.</summary>
        FormDef StanceOf(string id)
        {
            var hero = formHero != null ? data?.Hero(formHero) : ctxHero;
            var fd = id != null ? (data?.Form(id) ?? hero?.Forms?.FirstOrDefault(x => x.Id == id)) : hero?.Forms?.Count == 1 ? hero.Forms[0] : null;
            return fd != null && fd.Stance ? fd : null;
        }

        /// <summary>앞에서 「적 1명」 을 짚었나(같은 글 안 — 뒤의 「적 1명」 은 「그 적」 으로).</summary>
        bool sawOne;
        /// <summary>(엘다인) 앞에 「「X 모드」면:」 이 있었다 — 뒤의 formEnd 는 「변신을 끝냄」 으로 짧게.</summary>
        bool formCond;

        /// <summary>그 사도의 효과 글 — 고유 효과 · 변신 이름을 그 사도의 것으로 먼저 찾는다(보스 클론 고학년의 원래 효과 — 클론 전용 사도는 Heroes 밖이라).</summary>
        public string FxOf(HeroDef h, List<Fx> fx)
        {
            var c0 = ctxHero; ctxHero = h ?? ctxHero;
            try { return Fx(fx); } finally { ctxHero = c0; }
        }

        /// <summary>효과 조각 목록 → 글(조건은 새 문장 — 카드 글이면 새 줄).</summary>
        public string Fx(List<Fx> fx)
        {
            if (fx == null || fx.Count == 0) return "";
            fxDepth++;
            bool one0 = sawOne, fc0 = formCond; sawOne = false; formCond = false;
            try { return FxBody(fx); }
            finally { fxDepth--; sawOne = one0; formCond = fc0; }
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
            bool baseOneDmg = false;  // 기본 문장에 적 1명(카드의 대상)을 치는 피해가 있나 — 조건 쪽 「그 적에게 … 더」 의 「그 적에게」 를 덜어 낼 수 있다
            Fx prev = null;
            Fx skipFx = null;
            string tailNote = null;   // 비례 뒤 효과 끝에 붙일 말(이어 쓰기)
            var dmgTargets = new HashSet<string>();   // 앞서 친 피해의 대상 — 같은 대상의 비례 · 조건 피해는 대상 말 없이 「더」
            string lastFoe = null;    // 바로 앞 조각이 짚은 적 묶음(one · all) — 같은 적에게 잇단 효과는 대상을 한 번만 쓰고 「 · 」 로 잇는다(2026-10-12 카드 글 간소화)
            void Flush()
            {
                if (cur.Count == 0 && head == null) return;
                sentences.Add((head != null ? (kwHead || inRule ? head + ": " : Reg(head) + " ") : "") + string.Join(", ", cur));
                cur.Clear(); head = null; lastLead = null; lastFoe = null;
            }
            foreach (var f in fx)
            {
                string cond = CondHead(f);
                if (f.K == FxK.IfForm && !f.Not) formCond = true;
                if (f.K == FxK.IfStack && !f.Not && f.Max <= 0 && Carrier(f.Id) != "enemy")
                {   // 「「X」 2 이상이면 「X」 2 소모,」 → 「「X」 2를 써서:」(엘다인 글 길이)
                    int fi = fx.IndexOf(f);
                    var nx = fi >= 0 && fi + 1 < fx.Count ? fx[fi + 1] : null;
                    if (nx != null && nx.K == FxK.Spend && nx.Id == f.Id && !nx.All && !nx.Pick && nx.IV == Math.Max(1, f.N))
                    {
                        Flush(); head = $"{Q(f.Id)} {nx.IV}{(Ko.HasFinal(nx.IV.ToString()) ? "을" : "를")} 써서"; kwHead = true; choiceHead = false; prev = nx; skipFx = nx; continue;
                    }
                }
                if (skipFx != null && ReferenceEquals(f, skipFx)) { skipFx = null; continue; }
                if (cond != null) { Flush(); if (f.K == FxK.IfChoice || f.K == FxK.When) sawOne = sawBase; if (cond.Contains("고른 적")) sawOne = true; head = cond; stanceHead = f.K == FxK.IfForm && StanceOf(f.Id) != null; kwHead = KwHead(f); choiceHead = f.K == FxK.IfChoice || f.K == FxK.When; prev = f; continue; }
                if (f.K == FxK.PerStack && f.Each && f.Max == 1 && f.N <= 0) { pending = $"{Ko.J(Q(f.Id), "이가")} 있는 적마다 "; pendEach = true; prev = f; continue; }   // 「적마다 그 적의 「X」 1개당(최대 1)」 → 「「X」가 있는 적마다」
                if (f.K == FxK.PerStack) { string lim = f.N > 0 && f.Max > 0 ? $"(최소 {f.N} · 최대 {f.Max})" : f.N > 0 ? $"(최소 {f.N})" : f.Max > 0 ? $"(최대 {f.Max})" : ""; pending = f.Each ? $"적마다 그 적의 {Q(f.Id)} 1당{lim} " : R.BUFF_ST.Contains(f.Id ?? "") ? $"{f.Id} 1당{lim} " : $"{Q(f.Id)} 1당{lim} "; pendEach = f.Each; prev = f; continue; }   // 파티 상태(반격 — 2026-10-13 비비)는 상태 글처럼 낫표 없이   // 「1개당」 → 「1당」(Docs/설명글.md §2 「「이름」 1당」 — 카드 값 「「성장」 1당」 과 한 꼴, 2026-10-12)
                if (f.K == FxK.PerRhythm) { pending = "리듬 1당 "; prev = f; continue; }
                if (f.K == FxK.PerDiscarded)
                {   // 같은 글에 버리기 · 태우기가 있으면 「버린 1장당」(엘다인 3차 — 글 길이), n · max 는 최소 · 최대
                    bool dsc = fx.Any(x => x.K == FxK.Discard || x.K == FxK.Burn);
                    string lim = f.N > 0 && f.Max > 0 ? $"(최소 {f.N} · 최대 {f.Max})" : f.N > 0 ? $"(최소 {f.N})" : f.Max > 0 ? $"(최대 {f.Max})" : "";
                    pending = (dsc ? "버린 1장당" : "이 카드로 버린 카드 1장당") + lim + " "; prev = f; continue;
                }
                if (f.K == FxK.PerPaid) { pending = $"이번 턴 치른 HP {(f.Per > 0 ? f.Per : 100)}당 "; prev = f; continue; }
                if (f.K == FxK.PerDebuff) { pending = $"{(sawOne ? "그 적" : "적 1명")}의 디버프 1가지당 "; prev = f; continue; }
                if (f.K == FxK.PerApLeft) { pending = "남은 AP 1당 "; prev = f; continue; }
                if (f.K == FxK.PerX) { pending = "X당 "; prev = f; continue; }   // (2026-10-13 이드) X 코스트 — 쓴 AP 1당
                if (f.K == FxK.PerFoe) { pending = $"적 1명당{(f.Max > 0 ? $"(최대 {f.Max})" : "")} "; prev = f; continue; }   // (2026-10-13 비비) 살아 있는 적 수
                if (f.K == FxK.PerTag) { pending = data?.Card(f.Id) != null ? $"손의 {Q(TagKo(f.Id))} 1장당 " : $"손의 {Q(TagKo(f.Id))} 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPlayed) { pending = f.Id != null ? $"이번 턴 낸 {Q(f.Id)} 카드 1장당 " : "이번 턴 낸 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerPile) { pending = f.Tag != null ? $"{PileKo(f.From)}의 {Q(TagKo(f.Tag))} 1장당 " : $"{PileKo(f.From)} {PileUnit(f.From)} "; prev = f; continue; }
                if (f.K == FxK.PerShield) { pending = $"파티 실드 {(f.Per > 1 ? f.Per : 1)}당{(f.Max > 0 ? $"(최대 {f.Max})" : "")} "; prev = f; continue; }
                if (f.K == FxK.PerCardSt && f.From == "hand") { pending = $"손의 {(f.Who == "self" ? "자신의 " : "")}카드 {Q(f.Id)} 합 1당{(f.Max > 0 ? $"(최대 {f.Max})" : "")} "; prev = f; continue; }
                if (f.K == FxK.PerCardSt) { string lim = f.N > 0 && f.Max > 0 ? $"(최소 {f.N} · 최대 {f.Max})" : f.N > 0 ? $"(최소 {f.N})" : f.Max > 0 ? $"(최대 {f.Max})" : ""; pending = $"{Q(f.Id)} 1당{lim} "; prev = f; continue; }
                // 고른 만큼 · 전부 쓴 바로 뒤면 「… 써서 1개당」 으로 잇는다(엘다인 — 글 길이)
                if (f.K == FxK.PerEvent && prev != null && prev.K == FxK.Spend && (prev.Pick || prev.All) && cur.Count > 0 && f.Per <= 1 && cur[cur.Count - 1].EndsWith(" 소모", StringComparison.Ordinal))
                { var last = cur[cur.Count - 1]; cur.RemoveAt(cur.Count - 1); pending = last.Substring(0, last.Length - 3) + " 써서 1개당 "; if (prev.Loop) tailNote = $"(얻으면 이어 씀 · 최대 {(prev.Max > 0 ? prev.Max : 12)})"; lastLead = null; prev = f; continue; }
                if (f.K == FxK.PerEvent) { pending = (f.Per > 1 ? $"그 값 {f.Per}당" : "그 값 1당") + (f.Max > 0 ? $"(최대 {f.Max}) " : " "); prev = f; continue; }
                if (f.K == FxK.PerGuarded) { pending = $"막아 낸 피해 {(f.Per > 1 ? f.Per : 1)}당{(f.Max > 0 ? $"(최대 {f.Max})" : "")} "; prev = f; continue; }
                if (f.K == FxK.PerGone && f.Turn) { pending = f.Id != null ? $"이번 턴 소멸한 {Q(CardName(f.Id))} 1장당 " : "이번 턴 소멸한 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerGone) { pending = f.Who == "self" ? "이번 전투에 소멸한 자신의 카드 1장당 " : "이번 전투에 소멸한 카드 1장당 "; prev = f; continue; }
                if (f.K == FxK.PerOverheal) { string lim = f.Max > 0 ? $"(최대 {f.Max})" : ""; pending = f.Pct > 0 ? $"이번 전투 HP {Num.Round(f.Pct * 100)}%를 넘긴 회복 {(f.Per > 1 ? f.Per : 1)}당{lim} " : $"이번 전투 넘친 회복 {(f.Per > 1 ? f.Per : 1)}당{lim} "; prev = f; continue; }
                // 강화 카드 지속 규칙 — 제 문장(앞의 조건에 걸리지 않게 보이도록 따로)
                if (f.K == FxK.Power) { string ph = head; bool pk = kwHead; Flush(); string pt = PowerText(f); sentences.Add(ph != null ? (pk ? ph + ": " : Reg(ph) + " ") + pt : pt); prev = f; continue; }
                if ((f.K == FxK.Later || f.K == FxK.AfterCards) && cur.Count > 0 && head == null) Flush();
                var t = One(f, prev);
                if (t != null && f.Target == "markedEnemy" && f.Id != null) t = t.Replace("표식이 가장 많은 적", $"{Ko.J(Q(f.Id), "이가")} 가장 많은 적");   // 막연한 「표식」 대신 그 이름(2026-10-11 사용자)
                if (t == null) { prev = f; continue; }
                string lead = Lead(f);
                if (lead != null && !t.StartsWith(lead, StringComparison.Ordinal)) lead = null;
                // 같은 글에서 두 번째부터의 「적 1명」 = 고른 그 적(엔진 Resolve oneEnemy = 카드의 대상)
                if (t.StartsWith("적 1명", StringComparison.Ordinal))
                {
                    if (sawOne) { t = "그 적" + t.Substring(4); if (lead != null && lead.StartsWith("적 1명", StringComparison.Ordinal)) lead = "그 적" + lead.Substring(4); }
                    else sawOne = true;
                }
                else if (t.Contains("적 1명")) sawOne = true;
                if (head == null) sawBase = sawOne;
                // 실드를 쓴 바로 뒤 「쓴 양의 n% 고정 피해」 — 앞 피해와 같은 대상이면 대상 말 없이(엘다인 4차 — 이드 글 길이)
                if (f.K == FxK.Dmg && f.OfEvent > 0 && prev != null && prev.K == FxK.SpendShield && dmgTargets.Contains(f.Target ?? "oneEnemy"))
                {
                    foreach (var pf in new[] { "그 적에게 ", WhoTo(f.Target) + " " }) if (t.StartsWith(pf, StringComparison.Ordinal)) { t = t.Substring(pf.Length); break; }
                    // 「파티 실드 절반을 씀, 쓴 양의 200% …」 → 「파티 실드 절반을 써 그 200% …」(한 마디로)
                    if (head == null && pending == null && cur.Count > 0 && cur[cur.Count - 1].EndsWith(" 씀", StringComparison.Ordinal) && t.StartsWith("쓴 양의 ", StringComparison.Ordinal))
                    {
                        var sp = cur[cur.Count - 1]; cur.RemoveAt(cur.Count - 1);
                        t = sp.Substring(0, sp.Length - 1) + "써 그 " + t.Substring("쓴 양의 ".Length);
                        lastLead = null;
                    }
                }
                bool more = false, perHead = pending != null;   // 비례 머리(「X」 1당 …)가 붙은 조각은 묶지 않는다
                if (pending != null)
                {
                    if (pendEach && t.StartsWith("적 전체에 ", StringComparison.Ordinal)) t = t.Substring("적 전체에 ".Length);
                    else if (f.K == FxK.Dmg && f.OfEvent <= 0 && dmgTargets.Contains(f.Target ?? "oneEnemy"))
                    {   // 앞 피해와 같은 대상 — 「… 1개당 피해 30% 더」(엘다인 글 길이)
                        foreach (var pf in new[] { "그 적에게 ", WhoTo(f.Target) + " " }) if (t.StartsWith(pf, StringComparison.Ordinal)) { t = t.Substring(pf.Length); break; }
                        t += " 더"; more = true;
                    }
                    else if ((pending.StartsWith("그 적의 ", StringComparison.Ordinal) || pending.StartsWith("적 1명의 ", StringComparison.Ordinal)) && (f.Target ?? "oneEnemy") == "oneEnemy")
                    {   // 「그 적의 디버프 1가지당 그 적에게 피해」 → 「그 적의 디버프 1가지당 피해」 — 같은 적을 두 번 짚지 않는다
                        foreach (var pf in new[] { "그 적에게 ", "적 1명에게 " }) if (t.StartsWith(pf, StringComparison.Ordinal)) { t = t.Substring(pf.Length); break; }
                    }
                    t = pending + t + (tailNote ?? ""); pending = null; pendEach = false; lead = null; tailNote = null;
                }
                // 조건 효과는 기본 효과에 얹힌다 — 같은 갈래(피해 · 실드 · 회복)가 기본에 있으면 「더」
                string ak = AddKind(f.K);
                if (ak != null)
                {
                    if (head == null) { baseKinds.Add(ak); if (ak == "dmg" && (f.Target ?? "oneEnemy") == "oneEnemy") baseOneDmg = true; }
                    else if (!more && !choiceHead && baseKinds.Contains(ak) && !t.StartsWith("다른 적", StringComparison.Ordinal) && (ak != "dmg" || dmgTargets.Contains(f.Target ?? "oneEnemy")))
                    {
                        t += " 더";
                        // 「더」 = 기본 효과와 같은 대상에 얹힘 — 「그 적에게」 는 덜어 낸다(2026-10-07 「낸 뒤 손패가 없으면 피해 140% 더」)
                        if (ak == "dmg" && baseOneDmg && (f.Target ?? "oneEnemy") == "oneEnemy" && t.StartsWith("그 적에게 ", StringComparison.Ordinal)) t = t.Substring("그 적에게 ".Length);
                        // 기본 피해가 한 대상(적 전체 따위)뿐이면 그 대상 말도 덜어 낸다 — 「정각이면 적 전체에 피해 80% 더」 → 「정각이면 피해 80% 더」(2026-10-12)
                        else if (ak == "dmg" && dmgTargets.Count == 1 && f.Target != null && f.Target != "oneEnemy" && f.Target != "randomEnemy" && dmgTargets.Contains(f.Target) && t.StartsWith(WhoTo(f.Target) + " ", StringComparison.Ordinal)) t = t.Substring(WhoTo(f.Target).Length + 1);
                    }
                }
                if (f.K == FxK.Dmg) dmgTargets.Add(f.Target ?? "oneEnemy");
                // 같은 대상에 잇단 상태 · 증감은 대상을 한 번만 — 「적 1명 고통 3 · 손상 1」
                const string RUN = "이 전투 동안 ";
                static string NormLead(string l) => l != null && l.StartsWith("그 적", StringComparison.Ordinal) ? "적 1명" + l.Substring(3) : l;
                // 조건이 이미 그 적을 짚었으면(「그 적의 디버프가 1가지 이상이면」) 첫 효과의 「그 적」 은 덜어 낸다(2026-10-12 카드 글 간소화)
                bool foeCut = false;
                if (head != null && cur.Count == 0 && (head.Contains("그 적") || head.Contains("대상")) && !head.Contains("다른 적"))
                    foreach (var pf in new[] { "그 적에게 ", "그 적 " }) if (t.StartsWith(pf, StringComparison.Ordinal)) { t = t.Substring(pf.Length); foeCut = true; lead = null; break; }
                var (fk, fl) = foeCut ? ("one", 0) : perHead ? (null, 0) : FoeGroup(t);
                // 같은 적 묶음이 잇달면 대상은 한 번만 — 「적 전체에 피해 100% · 「행복한 아기 늑대」 1」 · 「적 1명에게 피해 130% · 강인도 피해 1」
                bool toughJoin = fk == null && f.K == FxK.Tough && t.StartsWith("강인도 피해 ", StringComparison.Ordinal);
                bool joined = !foeCut && cur.Count > 0 && lastFoe != null && (fk == lastFoe || toughJoin);
                if (joined) cur[cur.Count - 1] += " · " + OneDrop(t.Substring(toughJoin ? 0 : fl));
                else if (fk == null && lead != null && NormLead(lead) == NormLead(lastLead) && cur.Count > 0) cur[cur.Count - 1] += " · " + OneDrop(t.Substring(lead.Length));
                else if (t.StartsWith(RUN, StringComparison.Ordinal) && cur.Count > 0 && cur[cur.Count - 1].StartsWith(RUN, StringComparison.Ordinal)) cur.Add(OneDrop(t.Substring(RUN.Length)));
                else
                {   // 한 명 대상 말을 덜되, 같은 문장의 적 전체 효과와 같은 이름이 또 나오면(「적 전체에 … 「X」 1, 「X」 2」) 「대상에게」 로 가른다
                    string d = OneDrop(t);
                    if (d != t && aimWho == null && cur.Count > 0)
                    {
                        string all = string.Join(", ", cur);
                        string nm = System.Text.RegularExpressions.Regex.Replace(d, @" [+−-]?\d+%?$", "");
                        if (all.Contains("적 전체") && nm.Length > 0 && all.Contains(nm)) d = "대상에게 " + d;
                    }
                    cur.Add(d);
                }
                // 조각 안에 쉼표가 있으면(「맞은 적마다 …」 · 「처치하면 1회 더」) 뒤를 묶지 않는다 — 그 덤에 붙은 것으로 읽힌다
                lastFoe = t.Contains(", ") ? null : toughJoin ? lastFoe : !joined && fk != null && t.Substring(0, fl).EndsWith("의 ", StringComparison.Ordinal) ? null : fk;
                lastLead = lead;
                prev = f;
                // 예약 뒤의 효과는 지금 일어난다 — 예약 문장과 떼어 새 문장으로(조건 안이면 그 조건에 그대로)
                if ((f.K == FxK.Later || f.K == FxK.AfterCards) && head == null) Flush();
            }
            Flush();
            return string.Join(SentJoin, sentences);
        }

        /// <summary>규칙 글에서 한 명 대상 말을 바꿀 말(「때린 적」) — 없으면 한 명 대상은 말 없이.</summary>
        string aimWho;
        static readonly System.Text.RegularExpressions.Regex ONE_TO = new(@"(?<=^|[\s(])(?<!무작위 )(?<!다른 )(?<!마다 )(적 1명에게|그 적에게|적 1명|그 적)(?= )", System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex ONE_OF = new(@"(?<=^|[\s(])(?<!무작위 )(?<!다른 )(?<!마다 )(적 1명의|그 적의)(?= )", System.Text.RegularExpressions.RegexOptions.Compiled);
        /// <summary>
        /// (2026-10-12 밤 사용자) 대상이 한 명이면 대상 말을 쓰지 않는다 — 「적 1명에게 피해 120%」 → 「피해 120%」, 「적 1명 취약 1」 → 「취약 1」.
        /// 효과 조각에만(조건 머리 「그 적에게 「X」가 있으면」 은 그대로). 「적 1명의 · 그 적의」 는 「대상의」 로(「2턴간 대상의 받는 피해 +20%」 — 빼면 자신의 것으로 읽힌다).
        /// 무작위 · 다른 적 · 「적마다 그 적의」 는 한 명 대상이 아니라 그대로.
        /// </summary>
        string OneDrop(string t)
        {
            if (string.IsNullOrEmpty(t) || !(t.Contains("적 1명") || t.Contains("그 적"))) return t;
            t = ONE_OF.Replace(t, "대상의");
            return ONE_TO.Replace(t, m => aimWho == null ? "\u0001" : m.Value.EndsWith("에게", StringComparison.Ordinal) ? aimWho + "에게" : aimWho).Replace("\u0001 ", "").Replace("\u0001", "");
        }

        /// <summary>조각이 적 묶음 대상 말로 시작하면 (묶음, 대상 말 길이) — one = 고른 적(「적 1명」 · 「그 적」), all = 적 전체. 그 밖(무작위 · HP가 가장 높은 적 …)은 매번 다시 고르므로 묶지 않는다.</summary>
        static (string, int) FoeGroup(string t)
        {
            foreach (var (p, k) in FOE_HEADS) if (t.StartsWith(p, StringComparison.Ordinal)) return (k, p.Length);
            return (null, 0);
        }
        // 「그 적의 」 · 「적 전체의 」 는 묶음을 잇기만 한다(「피해 30% · 버프 1개 제거」) — 묶음 첫머리가 되면 뒤 효과까지 「의」 에 걸려 읽힌다
        static readonly (string, string)[] FOE_HEADS = { ("적 1명에게 ", "one"), ("그 적에게 ", "one"), ("적 1명 ", "one"), ("그 적 ", "one"), ("그 적의 ", "one"), ("적 전체에 ", "all"), ("적 전체 ", "all"), ("적 전체의 ", "all") };

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
        string Foe() => inRule ? "그 적" : sawOne ? "대상" : "고른 적";   // 2026-10-12 밤: 카드 글은 앞의 「적 1명」 을 쓰지 않으니(OneDrop) 이어 짚는 조건은 「대상」
        bool FoeNamed => Foe() != "고른 적";
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
            FxK.IfStack => FoeStack(f.Id) + (f.Not ? (f.N > 1 ? $"{Q(f.Id)} {f.N} 미만이면" : $"{Ko.J(Q(f.Id), "이가")} 없으면") : StageIf(f.Id, f.N, f.Max) is string sti ? sti : f.Max > 0 && f.Max == Math.Max(1, f.N) ? $"{Ko.J(Q(f.Id), "이가")} {f.Max}{(Ko.HasFinal(f.Max.ToString()) ? "이면" : "면")}" : f.Max > 0 ? $"{Ko.J(Q(f.Id), "이가")} {Math.Max(1, f.N)}~{f.Max}{(Ko.HasFinal(f.Max.ToString()) ? "이면" : "면")}" : f.N > 1 ? $"{Q(f.Id)} {f.N} 이상이면" : $"{Ko.J(Q(f.Id), "이가")} 있으면"),
            FxK.When => f.On switch { "handHurtBig" => $"손에 있을 때 한 대에 HP {Num.Round((f.Pct > 0 ? f.Pct : 0.15) * 100)}% 넘게 잃으려 하면 {Num.Round((f.Pct > 0 ? f.Pct : 0.15) * 100)}%로 막고 소멸", "draw" => "영감", "drawAny" => "감응", "discard" => "안식", "handEnd" => formHero != null ? "쥐고 넘기면" : "턴 끝에 손에 있으면", "shieldLeft" => "적 차례가 끝날 때 실드가 남았으면", "burn" => "소각", "passion" => "열정", _ => f.On },
            FxK.IfRepeat => "되풀이",
            FxK.IfHeld => $"손에서 {Math.Max(1, f.N)}턴 묵혔으면",
            FxK.IfPlayedMax => f.N == 0 ? "이번 턴 첫 카드면" : $"이번 턴 앞서 낸 카드가 {f.N}장 이하면",
            FxK.IfApLeft => $"낸 뒤 AP가 {Math.Max(1, f.N)} 이상 남으면",
            FxK.IfSpent => $"이번 턴 쓴 AP가 꼭 {f.N}이면",
            FxK.IfBalanced => "공격과 스킬이 같은 장수면",
            FxK.IfHunted => $"{Foe()}이 찍혀 있으면",
            FxK.IfDebuffs => $"{Foe()}의 디버프가 {Math.Max(1, f.N)}가지 이상이면",
            FxK.IfHp => f.Not ? $"파티 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}%보다 많으면" : $"파티 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 이하면",
            FxK.IfKill => f.Id == "orBoss" ? "처치하거나 보스면" : f.Id == "boss" ? (f.Not ? "보스를 처치하지 못했으면" : "보스를 처치하면") : f.Id == "elite" ? (f.Not ? "엘리트 · 보스를 처치하지 못했으면" : "엘리트 · 보스를 처치하면") : f.Not ? "처치하지 못했으면" : "처치",
            FxK.IfBreak => f.Not ? "격파하지 못했으면" : "붕괴",
            FxK.IfPricier => f.Not ? "바로 앞 카드의 비용 이하면" : "바로 앞 카드의 비용을 넘으면",
            FxK.IfGained => f.Not ? $"이번 전투에 쌓은 {Ko.J(Q(f.Id), "이가")} {Math.Max(1, f.N)} 미만이면" : $"이번 전투에 쌓은 {Ko.J(Q(f.Id), "이가")} {Math.Max(1, f.N)} 이상이면",
            FxK.IfWounded => f.Target == "oneEnemy" ? $"{Foe()}이 부상이면" : "부상",
            FxK.IfChoice => choices != null && f.N >= 1 && f.N <= choices.Count ? choices[f.N - 1] : $"갈래 {f.N}",
            FxK.IfRandom => $"{Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 확률로",
            FxK.IfHand => (inRule ? (ruleOn == null || ruleOn == "play" ? "그 뒤 " : "") : "낸 뒤 ") + (f.N <= 0 ? "손패가 없으면" : $"손패가 {f.N}장 이하면"),
            FxK.IfPile when f.From == "record" => f.Not ? "받아 적은 쪽이 없으면" : f.N > 1 ? $"받아 적은 쪽이 {f.N}장 이상이면" : "받아 적은 쪽이 있으면",
            FxK.IfPile => $"{PileKo(f.From)}가 {Math.Max(1, f.N)}장 이상이면",
            FxK.IfNth when f.After => f.Id != null ? $"{f.Id}{(Ko.HasFinal(f.Id) ? "이면" : "면")}" : $"이번 턴 {f.N}번째 이후면",   // 엘다인 4차 ▶E77 — 「정각이면」(풀이는 툴팁)
            FxK.IfNth => f.N <= 1 ? "이번 턴 첫 카드면" : $"이번 턴 {f.N}번째 카드면",
            FxK.IfCost => f.Id != null ? (f.Not ? $"{Ko.J(f.Id, "이가")} 아니면" : $"{f.Id}{(Ko.HasFinal(f.Id) ? "이면" : "면")}") : f.Not ? $"비용 {f.Max}보다 비싸게 내면" : $"비용 {f.Max}{(f.Max == 0 ? "으로" : " 이하로")} 내면",   // 리뉴아 「시간」 — 「정각이면」(툴팁 「비용이 0 이 되면」)
            FxK.IfHitTaken => f.Not ? $"이번 턴 맞은 대가 {Math.Max(1, f.N)} 미만이면" : $"이번 턴 {Math.Max(1, f.N)}대 이상 맞았으면",   // ▶E85
            FxK.IfStreak => f.Type != null ? $"자신의 {f.Type} 카드를 {Math.Max(2, f.N)}장째 잇달아 내면" : $"같은 사도의 카드를 {Math.Max(2, f.N)}장째 잇달아 내면",
            FxK.IfAllHeroes => "이번 턴 사도 모두가 카드를 냈으면(이 카드 포함)",
            FxK.IfFoe when f.HitsBy != null => f.Not ? $"{KoNum(f.N)} 번 친 적이 아니면" : $"{KoNum(f.N)} 번 친 적이면",   // 글 길이(엘다인 3차)
            FxK.IfFoe when f.Id == null && f.HpBelow > 0 => FoeNamed ? $"HP {Num.Round(f.HpBelow * 100)}% 이하면" : $"{Foe()}의 HP가 {Num.Round(f.HpBelow * 100)}% 이하면",   // 앞 피해와 같은 적이면 짧게(엘다인 4차 글 길이)
            FxK.IfFoe => (f.Id switch { "broken" => FoeNamed ? "격파 상태면" : $"{Foe()}이 격파 상태면", "tough" => $"{Foe()}의 강인도가 {Num.Round((f.Pct > 0 ? f.Pct : 0.5) * 100)}% 이하면", "guarded" => $"{Foe()}에게 실드가 있으면", "attack" => $"{Foe()}이 공격하려 하면", "hp" => FoeNamed ? $"HP {Num.Round((f.Pct > 0 ? f.Pct : 0.3) * 100)}% 이하면" : $"{Foe()}의 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.3) * 100)}% 이하면", "hpMob" => $"{Foe()}이 보스가 아니고 HP가 {Num.Round((f.Pct > 0 ? f.Pct : 0.3) * 100)}% 이하면", "alone" => f.Not ? "적이 둘 이상이면" : "적이 하나뿐이면", "boss" => $"{Foe()}이 보스면", "known" => f.Not ? "모르는 수면" : "아는 수면", "anyAttack" => f.Not ? "공격하려는 적이 없으면" : "공격하려는 적이 있으면", var st when R.ALL_ST.Contains(st) => $"{Foe()}에게 {Ko.J(st, "이가")} {(f.Not ? "없으면" : "있으면")}", _ => f.Id }) + (f.Not && !R.ALL_ST.Contains(f.Id ?? "") ? "(아니면)" : ""),
            FxK.IfRoll => $"{f.N}",
            FxK.IfPrevSame => f.Not ? "바로 앞 카드와 종류가 다르면" : "바로 앞 카드와 종류가 같으면",
            FxK.IfInHand => costIf != null && !f.Not && f.Id == costIf.InHand ? $"손에 {Ko.J(Q(CardName(f.Id)), "이가")} 있으면 비용 +{costIf.Add}," : $"손에 다른 {Ko.J(Q(CardName(f.Id)), "이가")} 있으면",   // costIf 와 한 조건(2026-10-13 이드)
            FxK.IfBond => $"결속 {f.N} 이상이면",
            FxK.IfLastMine => f.Not ? "마지막 카드가 다른 아군의 것이면" : "마지막 카드가 자신의 것이면",
            FxK.IfPulled => $"꺼낸 카드가 {Filt(f)}카드면",
            FxK.IfShield => f.Not ? "파티에 실드가 없으면" : "파티에 실드가 있으면",
            FxK.IfDebt => "AP 빚이 있으면",
            FxK.IfTypeNew => "그 종류가 이번 턴 처음이면",
            FxK.IfForm => FormIf(f),
            FxK.IfEvent => f.Not ? $"{Math.Max(1, f.N)} 미만이면" : $"{Math.Max(1, f.N)} 이상이면",
            FxK.IfCardSt => f.Not ? $"{Ko.J(Q(f.Id), "이가")} 없으면" : $"{Q(f.Id)} {Math.Max(1, f.N)} 이상이면",   // 「이 카드의」 는 뺐다(엘다인 3차 — 글 길이)
            _ => null,
        };

        /// <summary>「두 번」 · 「세 번」 — 1~5 는 말로, 그 밖은 숫자.</summary>
        static string KoNum(int n) => n switch { 1 => "한", 2 => "두", 3 => "세", 4 => "네", 5 => "다섯", _ => n.ToString() };

        /// <summary>「「백수공권 모드」면」 · 「… 가 아니면」 — 변신 이름으로(id 없으면 「변신 중이면」).</summary>
        /// <summary>(2026-10-19 타수 합치기) dmg plusIf — 「피해 100%(「본성」이면 140%)」 꼴의 괄호 덧말.</summary>
        string PlusTxt(Fx f)
        {
            if (f.PlusIf == null || !(f.Plus > 0)) return "";
            var c = f.PlusIf;
            string cond;
            if (c.K == FxK.IfForm && !c.Not && StanceOf(c.Id) is { } sd) cond = $"{Q(sd.Name)}{(Ko.HasFinal(sd.Name) ? "이면" : "면")}";
            else if (c.K == FxK.IfFoe && c.Id == "boss" && !c.Not) cond = (f.Target ?? "oneEnemy") == "oneEnemy" ? "보스면" : "보스에게";   // (2026-10-19) 「피해 220%(보스면 320%)」 · 적 전체는 「(보스에게 …)」 — 맞는 적마다 본다
            else
            {   // 한 적을 치는 대면 조건의 적도 그 적 — 「대상의 」 를 떼어 짧게(「피해 90%(격파 상태면 240%)」)
                bool one = (f.Target ?? "oneEnemy") == "oneEnemy", s0 = sawOne;
                if (one) sawOne = true;
                cond = CondHead(c) ?? "";
                sawOne = s0;
                if (one) foreach (var pre in new[] { "대상의 ", "대상이 ", "대상에게 " }) if (cond.StartsWith(pre, StringComparison.Ordinal)) { cond = cond.Substring(pre.Length); break; }
                if (!cond.EndsWith("면", StringComparison.Ordinal)) cond += (cond.Length > 0 && Ko.HasFinal(cond) ? "이면" : "면");
            }
            return $"({cond} {P(f.Ratio + f.Plus)})";
        }

        string FormIf(Fx f)
        {
            var sd = StanceOf(f.Id);
            if (sd != null) return f.Not ? $"{Ko.J(Q(sd.Name), "이가")} 아니면" : Q(sd.Name);   // 태세(엘다인 4차 — 린 「흑운태세 : …」 꼴)
            var owner = f.Id != null ? (data?.HeroOfForm(f.Id) ?? ctxHero) : null;
            var nm = f.Id != null && (owner?.Forms?.Count ?? 0) > 1 ? (data?.Form(f.Id)?.Name ?? ctxHero?.Forms?.FirstOrDefault(x => x.Id == f.Id)?.Name ?? f.Id) : null;
            if (nm == null) return f.Not ? "변신 중이 아니면" : "변신 중이면";   // 사도의 변신이 하나면 이름 없이(엘다인 글 길이)
            return f.Not ? $"{Ko.J(Q(nm), "이가")} 아니면" : $"{Q(nm)}{(Ko.HasFinal(nm) ? "이면" : "면")}";
        }

        /// <summary>카드 거르개 글 — 「자신의 고유 공격 」.</summary>
        string Filt(Fx f)
        {
            var s = new List<string>();
            if (f.Who == "self") s.Add("자신의"); else if (f.Who == "other") s.Add("다른 아군의"); else if (f.Who != null) s.Add((data?.Hero(f.Who)?.Name ?? f.Who) + "의");
            if (f.Basic) s.Add("시작"); if (f.Unique) s.Add("고유"); if (f.Token) s.Add("생성");
            if (f.MinCost > 0 && f.K != FxK.Empower) s.Add($"비용 {f.MinCost} 이상");   // 엘다인 4차 요청(이드)
            if (f.Tag != null) s.Add(Q(TagKo(f.Tag)));
            if (f.Type != null && f.K != FxK.IfPrev) s.Add(f.Type);
            return s.Count == 0 ? "" : string.Join(" ", s) + " ";
        }
        static bool HasF(Fx f) => f.Who != null || f.Basic || f.Unique || f.Tag != null || f.Type != null || f.Token || f.MinCost > 0;
        /// <summary>손의 카드 거르개 글 — 거르개가 카드 id 하나뿐이면 「손의 「마탄」 」, 아니면 「손의 … 카드 」.</summary>
        string HandFilt(Fx f) => f.Tag != null && data?.Card(f.Tag) != null && f.Who == null && !f.Basic && !f.Unique && f.Type == null && !f.Token ? $"손의 {Q(TagKo(f.Tag))} " : $"손의 {Filt(f)}카드 ";

        /// <summary>갈래 태그 글 — 카드 id 면 그 카드 이름(생성물 「케이크」), 아니면 태그 그대로.</summary>
        string TagKo(string t) => data?.Card(t)?.Name ?? t;
        static string PileKo(string p) => p switch { "draw" => "뽑을 더미", "gone" => "소멸 더미", "hand" => "손패", "discarded" => "버린 더미(이번 턴에 낸 카드 빼고)",
            "token" => "손의 생성 카드", "broken" => "이번 전투 격파", "drawn" => "이번 턴 뽑은 카드", "discards" => "이번 전투 버린 카드", "morale" => "파티 사기", _ => "버린 더미" };
        static string PileUnit(string p) => p switch { "broken" => "1번당", "morale" => "1당", _ => "1장당" };   // discard = 버린 더미 전부(이번 턴에 낸 카드 포함) — Docs/카드이동.md

        static string Dur(Fx f)
        {
            if (f.Run) return "이 전투 동안 ";
            int t = f.TurnsOr1;
            if (t >= 999) return "이 전투 동안 ";
            return t > 1 ? $"{t}턴간 " : "이번 턴 ";
        }

        /// <summary>
        /// 피해 % 표기(2026-10-10 카제나): 「피해량 +x%」 = 합연산(카드 계수에 더함 — 사기), 「피해 x% 증가 · 감소」 = 곱연산(주는 피해 · 약점 · 분쇄 · 잔광 · 탐구심 따위).
        /// 주는 피해(dealt)는 곱연산이라 「x% 증가」 로 쓴다. 그 밖의 능력(공격력 · 방어력 · 받는 피해 …)은 「+x%」 그대로.
        /// </summary>
        static string StatPct(string stat, int pct) => stat == "dealt" ? (pct >= 0 ? $"{pct}% 증가" : $"{-pct}% 감소") : $"{(pct >= 0 ? "+" : "")}{pct}%";
        static readonly Dictionary<string, string> STAT_KO = new() { ["dealt"] = "주는 피해", ["taken"] = "받는 피해", ["atk"] = "공격력", ["def"] = "방어력", ["crit"] = "치명 확률", ["tough"] = "받는 강인도 피해", ["guard"] = "주는 실드", ["counter"] = "반격 피해", ["ultCost"] = "고학년 게이지", ["weak"] = "약점 공격 피해" };

        /// <summary>대상 표기(Docs/키워드.md §0) — 표기 없음 = 파티 전원 · 「자신의」 = 카드 주인 · 「아군의」 = 자신을 뺀 사도.</summary>
        /// <summary>적을 가리키는 대상인가(글에서 「적 …」 으로 적을 것) — 2026-10-06 nextEnemy · topEnemy 등이 「파티」 로 읽히던 것 고침.</summary>
        static bool IsFoeT(string t) => t == "bigHitEnemy" || t == "oneEnemy" || t == "allEnemies" || t == "randomEnemy" || t == "topEnemy" || t == "lowEnemy" || t == "otherEnemy" || t == "nextEnemy" || t == "slowestEnemy" || t == "farEnemy" || t == "markedEnemy";
        static string Whose(string tg) => tg switch
        {
            "self" => "자신의 ", "otherAllies" => "아군의 ", "allAllies" => "", "party" => "", "oneAlly" => "아군 1명의 ",
            "strongestAlly" => "공격력이 가장 높은 아군의 ", "bigHitEnemy" => "공격 예고가 가장 큰 적의 ", "otherEnemy" => "다른 적 1명의 ", "nextEnemy" => "행동 카운트가 가장 작은 적의 ", "slowestEnemy" => "행동 카운트가 가장 큰 적의 ", "farEnemy" => "가장 먼 적의 ", "markedEnemy" => "표식이 가장 많은 적의 ", _ when tg != null && tg.StartsWith("hero:", StringComparison.Ordinal) => $"{tg.Substring(5)}(없으면 공격력이 가장 높은 아군)의 ", "allEnemies" => "적 전체의 ", "randomEnemy" => "무작위 적의 ", "topEnemy" => "HP가 가장 높은 적의 ", "lowEnemy" => "HP가 가장 낮은 적의 ", _ => "적 1명의 ",
        };

        string One(Fx f, Fx prev)
        {
            switch (f.K)
            {
                case FxK.Dmg:
                    {
                        if (f.OfForm > 0) return $"{WhoTo(f.Target)} 이번 {Q(FormNameOf(formHero) ?? ctxHero?.Forms?.FirstOrDefault()?.Name ?? "변신")} 준 피해의 {P(f.OfForm)} 고정 피해";   // ▶E76
                        if (f.OfEvent > 0) return f.OfStack != null ? $"{WhoTo(f.Target)} {Q(f.OfStack)} 1당 {Num.Round(f.OfEvent)} 고정 피해" : f.OfShield ? $"{WhoTo(f.Target)} 파티 실드의 {P(f.OfEvent)} 고정 피해" : prev != null && prev.K == FxK.SpendShield ? $"{WhoTo(f.Target)} 쓴 양의 {P(f.OfEvent)} 고정 피해" : $"{WhoTo(f.Target)} 그 값의 {P(f.OfEvent)} 고정 피해";
                        string hitFx = f.HitFx != null && f.HitFx.Count > 0 ? ", 맞은 적마다 " + StripFoe(Fx(f.HitFx)) : "";
                        if (f.HitsPer != null && R.PER_HIT_MERGE)   // (2026-10-19) 겹 수 타수는 한 대 — 「「왕마력」 1당 피해 30%」 · 「(「용기」+2)당 피해 50%」
                            return $"{WhoTo(f.Target)} {(f.HitsAdd != 0 ? $"({Q(f.HitsPer)}{(f.HitsAdd > 0 ? "+" : "")}{f.HitsAdd})당" : $"{Q(f.HitsPer)} 1당")} {(f.Base == "def" ? "방어 기반 피해" : "피해")} {P(f.Ratio)}{PlusTxt(f)}{hitFx}";
                        if (f.HitsPer != null) return $"{(f.Target == "randomEnemy" ? "무작위 적" : WhoTo(f.Target))} ({Q(f.HitsPer)}{(f.HitsAdd != 0 ? $"{(f.HitsAdd > 0 ? "+" : "")}{f.HitsAdd}" : "")})회 × {(f.Base == "def" ? "방어 기반 피해" : "피해")} {P(f.Ratio)}{hitFx}";
                        string hits = f.XHits ? (R.PER_HIT_MERGE ? $"(X{(f.XStack != null ? $"+{Q(f.XStack)}" : "")})당 " : $"X회{(f.XStack != null ? $"(+{Q(f.XStack)} 수)" : "")} × ")   // (2026-10-19) X 타수는 한 대 — 「(X+「왕마력」)당 피해 N%」
                            : f.HitsOr1 > 1 ? $"{f.HitsOr1}회 × " : "";
                        // 2026-10-07 카제나 꼴 — 피해 % 의 바탕은 공격력(Docs/키워드.md 「피해량」), 「공격력 N% 피해」 → 「피해 N%」
                        string what = f.Base == "def" ? $"방어 기반 피해 {P(f.Ratio)}" : f.Dot ? $"고정 지속 피해 {P(f.Ratio)}" : $"{(f.Fixed ? "고정 " : "")}피해 {P(f.Ratio)}";
                        string to = f.Target == "randomEnemy" && hits.Length > 0 && !(f.XHits && R.PER_HIT_MERGE) ? "무작위 적" : WhoTo(f.Target);
                        string kn = f.KnownHits > 1 ? $", 아는 수를 하는 적은 {f.KnownHits}회" : "";
                        string more = f.KillMore ? $", 처치하면 1회 더(최대 {(f.Max > 0 ? f.Max : Battle.KILL_MORE_MAX)})" : "";   // ▶E79
                        return $"{to} {hits}{what}{PlusTxt(f)}{hitFx}{more}{kn}";
                    }
                case FxK.Block:
                case FxK.Shield:
                    {
                        if (f.OfEvent > 0) return f.OfStack != null ? $"{Q(f.OfStack)} 1당 {Num.Round(f.OfEvent)} 고정 실드" : f.OfShield ? $"파티 실드의 {P(f.OfEvent)} 고정 실드" : $"그 값의 {P(f.OfEvent)} 고정 실드";
                        // 표기 없음 = 파티(Docs/키워드.md §0) — 실드는 늘 파티 공용이라 대상을 적지 않는다
                        // 2026-10-07 카제나 꼴 — 실드 % 의 바탕은 방어력, 「방어력 N% 실드」 → 「실드 N%」
                        return $"{(f.Fixed ? "고정 " : "")}{(f.K == FxK.Block ? "방어" : "실드")} {P(f.Ratio)}";
                    }
                case FxK.Heal: return $"회복 {P(f.Ratio)}";   // 카제나 꼴(2026-10-12 사용자) — 방어력 기준은 낱말 판 「회복」 에
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
                        return $"{Dur(f)}{Whose(tg)}{STAT_KO[stat]} {StatPct(stat, pct)}";
                    }
                case FxK.Draw: return !HasF(f) ? $"드로우 {f.IV}" : f.Tag != null && f.Who == null && !f.Basic && !f.Unique && f.Type == null && !f.Token && f.MinCost <= 0 && data?.Card(f.Tag) == null ? $"뽑을 더미에서 {Ko.J(Q(TagKo(f.Tag)), "이가")} 붙은 카드 {f.IV}장 드로우" : $"{Filt(f)}카드 {f.IV}장 드로우";   // 2026-10-12 사용자 — 「「천상」 카드 1장 드로우」 가 천상 발동으로 읽혔다
                case FxK.HealMod: return $"{Dur(f)}{Whose(f.Target ?? "self")}회복량 {(f.V >= 0 ? "+" : "")}{Num.Round(f.V * 100)}%";
                case FxK.Roll: return $"무작위로 {Math.Max(2, f.N)}갈래 가운데 하나";
                case FxK.Recast: return $"이 카드 효과를 {P(f.Ratio > 0 ? f.Ratio : 0.5)}로 한 번 더";
                case FxK.CostMod when f.Choose == "priciest": return $"손의 가장 비싼 {Filt(f)}카드 비용 {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.CostMod: return $"{(f.TurnsOr1 > 1 ? $"{f.TurnsOr1}턴간 " : "이번 턴 ")}{Filt(f)}카드{(f.N > 0 ? $" {f.N}장" : "")} 비용 {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.AddTag: return $"이번엔 {Q(f.Id)}";
                case FxK.CutHit: return f.Target != null ? $"{Whose(f.Target)}다음 공격 한 대 피해 -{P(f.V)}" : $"적의 다음 공격 한 대 피해 -{P(f.V)}";
                case FxK.ClearDebt: return "AP 빚 탕감";
                case FxK.Ap: return $"AP {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.NextCheaper: return $"다음 카드 비용 -{f.IV}";
                case FxK.Gauge: return $"고학년 게이지 {(f.IV >= 0 ? "+" : "")}{f.IV}%";
                case FxK.Discard:
                    if (HasF(f)) return f.All ? $"{HandFilt(f)}모두 버림" : $"{HandFilt(f)}{(f.Random ? "무작위 " : "")}{f.IV}장 버림";   // 거르개(엘다인 — 「손의 「마탄」 1장 버림」)
                    return f.All ? "손패 전부 버리기" : (f.Random ? $"무작위 손패 {f.IV}장 버리기" : $"손패에서 {f.IV}장 골라 버리기");
                case FxK.Make: return $"{Q(CardName(f.Id))} {Math.Max(1, f.IV)}장 생성{(f.To == "draw" ? "(뽑을 더미 무작위 자리)" : f.To == "top" ? "(뽑을 더미 맨 위)" : f.To == "discard" ? "(버린 더미)" : "")}{""/* 「(주인: 다음 아군)」 괄호는 뺐다(2026-10-13 사용자 — 누구 카드인지는 손패 얼굴 핀이 보여 준다) */}";
                case FxK.Empower when f.Who == "other": return $"다른 아군의 다음{(f.MinCost > 0 ? $" 비용 {f.MinCost} 이상" : "")} 카드 +{P(f.Ratio)}";   // 엘다인 4차 요청(이드)
                case FxK.Empower: return $"{(f.Who == "any" ? "파티의" : "자신의")} 다음 카드 효과 +{P(f.Ratio)}";
                case FxK.Ripen:
                    {
                        string c0 = Carrier(f.Id);
                        string whose = c0 == "enemy" ? (f.Target == "allEnemies" ? "적 전체의 " : "적 1명의 ") : c0 == "hero" ? (f.Target == "oneAlly" ? "아군 1명의 " : "아군 모두의 ") : "";
                        // 사도의 예약 고유 효과가 하나면 이름 없이 「지금 정각」(엘다인 3차 — 글 길이)
                        var rh = (formHero != null ? data?.Hero(formHero) : ctxHero) ?? data?.Heroes.Values.FirstOrDefault(h => h.AllKeywords.Any(k => k.Name == f.Id));   // 카드 밖 글(bz hero · 신탁 줄)도 고유 효과 주인으로
                        if (rh != null && rh.AllKeywords.Count(k => k.Reserve) == 1 && rh.AllKeywords.Any(k => k.Name == f.Id)) return $"{whose}지금 터뜨림(1칸당 -{P(f.V > 0 ? f.V : 0.25)})";
                        return $"{whose}{Ko.J(Q(f.Id), "을를")} 지금 터뜨림(남은 1칸당 효과 -{P(f.V > 0 ? f.V : 0.25)})";
                    }
                case FxK.Summon: return $"{Q(f.Id)} 1개당 {WhoTo(f.Target ?? "randomEnemy")} 추가 공격 {(f.Base == "def" ? "방어 기반 " : "")}{P(f.Ratio)}{(f.Max > 0 && f.Max < Battle.SUMMON_CAP ? $"(최대 {f.Max}대)" : "")}";   // 상한(5대)은 툴팁(엘다인 글 길이)
                case FxK.Drain: return $"피해 기반 회복 {P(f.Ratio)}";   // 카제나 키워드 — 풀이(준 피해의 N% · 최대 HP 20%까지)는 TIPS
                case FxK.Extra: return $"{WhoTo(f.Target)} {(f.HitsOr1 > 1 ? $"{f.HitsOr1}회 × " : "")}추가 공격 {(f.Base == "def" ? "방어 기반 " : "")}{P(f.Ratio)}";
                case FxK.CardStatus:
                    {
                        string where = f.To == "hand" ? (f.N > 0 ? $"손의 {Filt(f)}카드 {f.N}장에 " : $"손의 {Filt(f)}카드 전부에 ") : f.To == "draw" ? (f.N > 0 ? $"뽑을 더미 {Filt(f)}{f.N}장에 " : "뽑을 더미 전부에 ") : f.To == "pulled" ? "그 카드에 " : "이 카드에 ";
                        // 엘다인 3차 — 동료 손패 · 보존(글 길이)
                        string hand = f.Who == "other" && f.Type == null && f.Tag == null && !f.Unique && !f.Basic && !f.Token ? "동료 손패" : $"손의 {Filt(f)}카드";
                        string stn = R.IsCardSt(f.Id) ? f.Id : Q(f.Id);
                        string keepN = f.Keep ? "(보존)" : "";
                        if (f.Choose == "priciest") where = f.Who == "other" && hand == "동료 손패" ? "가장 비싼 동료 손패 1장에 " : $"손의 가장 비싼 {Filt(f)}카드에 ";
                        if (f.Pick && f.To == "hand") return $"고른 만큼 {hand}에 {stn}{keepN}";   // (엘다인 3차 ▶E42)
                        if (f.Keep && f.To == "hand" && f.Choose == null) return $"{hand} {(f.N > 0 ? f.N : 1)}장에 {stn} {f.IV}{keepN}";
                        if (f.Keep) return $"{where}{stn} {f.IV}{keepN}";
                        if (f.Set != null || f.Half)
                        {   // (엘다인 ▶E17) 이 카드면 「낸 뒤 「X」 0」 · 「낸 뒤 「X」 절반」
                            string w2 = f.To == null || f.To == "this" ? "낸 뒤 " : where.EndsWith("에 ", StringComparison.Ordinal) ? where.Substring(0, where.Length - 2) + "의 " : where;
                            return f.Half ? $"{w2}{Q(f.Id)} 절반" : $"{w2}{Q(f.Id)} {(int)f.Set.Value}";
                        }
                        if ((f.To == null || f.To == "this") && f.IV > 0 && f.Id != "비용" && !R.IsCardSt(f.Id)) return $"낸 뒤 {Q(f.Id)} +{f.IV}";   // 이 카드에 쌓는 값 — 낸 뒤 붙는다(엘다인 4차 글 길이)
                        return f.Id == "비용" ? $"{where}비용 {(f.IV >= 0 ? "+" : "")}{f.IV}" : R.IsCardSt(f.Id) ? $"{where}{f.Id} {f.IV}" : $"{where}{Q(f.Id)} {f.IV}";
                    }
                case FxK.Transform: return f.From == null ? $"이 카드를 {Q(CardName(f.Id))}로 바꿈(이번 전투)" : f.From == "hand" || f.From.StartsWith("@", StringComparison.Ordinal) ? $"손의 {Filt(f.From.StartsWith("@", StringComparison.Ordinal) ? new Fx { K = f.K, Type = f.From.Substring(1) } : f)}카드 {f.NOr1}장을 {Q(CardName(f.Id))}로 바꿈(이번 전투)" : $"손의 {Q(CardName(f.From))}{(f.N > 1 ? $" {f.N}장" : "")}을 {Q(CardName(f.Id))}로 바꿈(이번 전투)";   // from hand · @종류 = 거르개(2026-10-08)
                case FxK.Form: { var fd = data?.Form(f.Id) ?? ctxHero?.Forms?.FirstOrDefault(x => x.Id == f.Id); return fd == null ? $"변신 {Q(f.Id)}" : fd.Stance ? $"{Q(fd.Name)} 획득{(fd.Turns > 0 ? $"({fd.Turns}턴)" : "")}" : $"{Q(fd.Name)}{Ro(fd.Name)} 변신({FormDur(fd)})"; }
                case FxK.FormKeep: { var sk = StanceOf(null); return $"{f.TurnsOr1}턴간 {(sk != null ? Q(sk.Name) : "태세")} 보존"; }
                case FxK.FormEnd: { var st0 = StanceOf(null); if (st0 != null) return $"{Q(st0.Name)} 해제"; if (formCond) return "변신을 끝냄"; var fn = FormNameOf(formHero); return fn != null ? $"{Q(fn)} 변신 해제" : "변신 해제"; }
                case FxK.SpendShield: return f.All ? "파티 실드를 모두 씀" : f.Pick ? "파티 실드를 고른 만큼 씀" : f.Half ? "파티 실드 절반을 씀" : $"파티 실드 {f.IV}{(Ko.HasFinal(f.IV.ToString()) ? "을" : "를")} 씀";
                case FxK.FormExtend: { var fd = f.Id != null ? data?.Form(f.Id) : null; string nm = fd?.Name ?? FormNameOf(formHero); return $"{(nm != null ? Q(nm) + " " : "변신 ")}+{Math.Max(1, f.IV)}턴{(f.Max > 0 ? $"(최대 {f.Max}턴)" : "")}"; }
                case FxK.NoForget: return "이번엔 망각 없이 사라짐";
                // 엘다인 3차
                case FxK.CounterHits: return $"이 전투 동안 반격이 {Math.Max(2, f.IV)}회{(f.Ratio > 0 ? $" × {P(f.Ratio)}" : "로 나뉨")}";
                case FxK.CounterAll: return f.Turns > 0 ? $"{Dur(f)}반격이 적 전체로" : "이 전투 동안 반격이 적 전체로";   // turns — n턴간(2026-10-13 비비 원작 「빙벽」)
                case FxK.CounterMod: return $"{Dur(f)}반격 피해 {(f.V >= 0 ? "+" : "")}{P(f.V)}";
                case FxK.GaugeLock: return "자신의 게이지 정지";   // 풀이는 툴팁 「게이지 정지」(엘다인 4차 글 길이)
                case FxK.NoToggle: return "이번엔 태세 유지";
                case FxK.Learn:
                    {
                        var kh = (f.Owner != null ? data?.Hero(f.Owner) : null) ?? (formHero != null ? data?.Hero(formHero) : ctxHero);
                        var kn = kh?.AllKeywords.FirstOrDefault(k => k.Known)?.Name ?? data?.Heroes.Values.SelectMany(h => h.AllKeywords).FirstOrDefault(k => k.Known)?.Name ?? "앎";
                        return $"{Who(f.Target ?? "oneEnemy")}의 수를 {Q(kn)}에 적음";
                    }
                case FxK.MoveMark: return $"동료들의 {Ko.J(Q(f.Id), "을를")} 모두 {(f.Target == "self" ? "자신" : "아군 1명")}에게 옮김";
                case FxK.Taunt: return f.V > 0 ? $"도발(막는 실드 +{P(f.V)})" : "도발";
                case FxK.CritNext when f.Now: return f.FoeStack != null ? $"{Q(f.FoeStack)} 적이면 치명" : "이 카드 치명";   // 엘다인 4차 요청(란)
                // 엘다인 4차(E77 · E78 · E80 · E83)
                case FxK.NthNext: return $"이번 턴 자신의 다음 카드{(f.IV > 1 ? $" {f.IV}장" : "")}{(f.Id != null ? $"{(f.IV > 1 ? "은" : "는")} {f.Id}" : "는 n번째 이후로 셈")}";
                case FxK.UltCostMod: return $"이 전투 동안 자신의 고학년 게이지 {(f.V >= 0 ? "+" : "")}{P(f.V)}";
                case FxK.SwiftTurn: return "이번 턴 남은 카드 신속";
                case FxK.CritDmg: return $"{(f.Id != null ? Q(f.Id) + " " : "")}{f.TurnsOr1}턴: 치명 피해 +{P(f.V)}{(f.Off != null && f.Off.Count > 0 ? $", 끝나면 {Fx(f.Off)}" : "")}";
                case FxK.CritNext: return $"{(f.Who == "other" ? "다른 아군의" : "자신의")} 다음 카드{(f.IV > 1 ? $" {f.IV}장" : "")} 치명";
                case FxK.StackLock: return $"{Ko.J(Q(f.Id), "을를")} {f.TurnsOr1}턴 고정";
                case FxK.Execute: return $"{(f.Target == "allEnemies" ? $"HP {P(f.Pct > 0 ? f.Pct : 0.1)} 이하인 적 처형" : $"HP {P(f.Pct > 0 ? f.Pct : 0.1)} 이하면 처형")}{(f.BossRatio > 0 ? $"(보스는 피해 {P(f.BossRatio)})" : "")}";
                case FxK.Record: return f.IV > 1 ? $"바로 앞 다른 아군 카드 {f.IV}장을 받아 적음" : "바로 앞 다른 아군 카드를 받아 적음";
                case FxK.Replay: return $"{(f.All ? "받아 적은 쪽을 모두" : f.IV > 1 ? $"받아 적은 쪽 {f.IV}장을" : "받아 적은 첫 쪽을")} 낭독({P(f.Ratio > 0 ? f.Ratio : 1)}){(f.Keep ? "(쪽은 남김)" : "")}";
                case FxK.Cue: return null;
                case FxK.Later: return $"{Reg($"{Math.Max(1, f.N)}턴 뒤 턴 시작에")} {Fx(f.Then)}";
                case FxK.AfterCards: return $"{Reg($"이 뒤로 카드를 {Math.Max(1, f.N)}장 더 내면")} {Fx(f.Then)}";
                case FxK.Trap: return $"{WhoTo(f.Target)} 함정(공격하면 {Fx(f.Then)})";
                case FxK.Confuse: return $"{Who(f.Target)} 혼란";
                case FxK.AutoPlay:
                    if (f.All) return $"{HandFilt(f)}모두 저절로 냄{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}";
                    return $"손의 무작위 {Filt(f)}{(f.Id != null ? Q(f.Id) + " " : "")}카드 {f.NOr1}장 저절로 나감{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}";
                case FxK.CastOther when f.From == "last": return $"이번 턴 바로 앞 다른 아군 카드를 다시({P(f.Ratio > 0 ? f.Ratio : 1)})";
                case FxK.CastOther: return $"손의 다른 사도 {(f.Id != null ? f.Id + " " : "")}카드 하나를 대신 발동{(f.Ratio > 0 && f.Ratio != 1 ? $"(효과 {P(f.Ratio)})" : "")}(카드는 손에 남음)";
                case FxK.Pull:
                    if (f.All) return $"{PileKo(f.From)}의 {Filt(f)}카드 모두 {(f.To == "top" ? "뽑을 더미 맨 위로" : "손으로")}";   // (2026-10-13 시온 고학년) 맞는 카드 모두
                    return f.From == "draw" && f.To != "top"   // 뽑을 더미 → 손 = 드로우(없으면 섞지 않고 불발)
                        ? $"뽑을 더미 {(f.At == "bottom" ? "맨 아래" : f.At == "random" ? "무작위" : "맨 위")} {Filt(f)}카드 {f.NOr1}장 드로우(없으면 섞지 않음)"
                        : $"{PileKo(f.From)} {(f.At == "bottom" ? "맨 아래" : f.At == "random" ? "무작위" : "맨 위")} {Filt(f)}카드 {f.NOr1}장을 {(f.To == "top" ? "뽑을 더미 맨 위로" : "손으로")}";
                case FxK.ExileFrom: return f.From == "pulled" ? "그 카드 소멸" : f.All ? $"{PileKo(f.From)}의 {Filt(f)}카드 모두 소멸" : $"{PileKo(f.From)}에서 무작위 {Filt(f)}{f.NOr1}장 소멸";
                case FxK.Dispel: return $"{Who(f.Target)}의 버프 {f.NOr1Of(f.IV)}개 제거";
                case FxK.GrowRun: return $"{Whose(f.Target ?? "self")}{(f.Id == "def" ? "방어력" : f.Id == "crit" ? "치명" : "공격력")} +{f.IV} (모험이 끝날 때까지)";
                case FxK.Tough:
                    {
                        bool after = prev != null && (prev.K == FxK.Dmg && (prev.Target ?? "oneEnemy") == (f.Target ?? "oneEnemy") || FxK.Pers.Contains(prev.K));   // 비례 뒤면 「「X」 1개당 강인도 피해 0.5」
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
                        if (f.OfEvent > 0) return Math.Abs(f.OfEvent - 1) < 1e-9 ? $"그만큼 {Q(f.Id)}" : $"그 값 {Num.Round(1 / f.OfEvent)}당 {Q(f.Id)} 1";
                        string c = Carrier(f.Id);
                        // (엘다인) 표식 옮기기 · 값 정하기 · 자세 바꾸기
                        if (f.Carry) return $"쓰러진 적의 {Q(f.Id)}만큼 {WhoTo(f.Target ?? "allEnemies")} {Q(f.Id)}";
                        string lockN = f.Lock ? $"({f.TurnsOr1}턴 고정)" : "";
                        if (f.Pick) return $"{Ko.J(Q(f.Id), "을를")} 고른 단계로{lockN}";   // (▶E74)
                        if (f.Set != null) { int sv = (int)f.Set.Value; string stn = StageName(f.Id, sv) ?? sv.ToString(); return $"{Ko.J(Q(f.Id), "을를")} {stn}{Ro(stn)}{lockN}"; }
                        if (f.IV == 1 && Swap2(f.Id)) return "자세를 바꿈";   // 2단계 순환(양산 ↔ 칼) — 글 길이(엘다인)
                        bool foe = c == "enemy" || (c == null && (f.Target == "oneEnemy" || f.Target == "allEnemies" || f.Target == "randomEnemy"));
                        if (foe) return $"{WhoTo(f.Target ?? "oneEnemy")} {Q(f.Id)} {f.IV}";
                        // 사도 표시(hero) — 누구에게 붙이는지 적는다
                        if (c == "hero" && f.Target == "holder") return $"{Q(f.Id)} +{f.IV}";   // 지금 붙은 동료에게 보충(벨라) — 「「존속」 동료에게 +2」 가 헷갈려 「「존속」 +2」(2026-10-12)
                        return c == "hero" && f.Target != null && f.Target != "self" ? $"{WhoTo(f.Target)} {Q(f.Id)} {f.IV}" : $"{Q(f.Id)} {f.IV}";
                    }
                case FxK.Spend: return f.Pick ? $"{Ko.J(Q(f.Id), "을를")} 고른 만큼 소모" : f.All ? $"{Q(f.Id)} 전부 소모" : $"{Q(f.Id)} {f.IV} 소모";
                case FxK.SpendRhythm: return f.All ? "리듬 전부 소모" : $"리듬 {f.IV} 소모";
                case FxK.Flip: return "전환";
                case FxK.Hasten: return $"재촉 {f.IV}";
                case FxK.PayHp: return $"파티 HP {f.IV} 소모";
                case FxK.Feed: return f.Target == "oneAlly" ? $"아군 1명의 고유 효과 +{f.IV}" : f.Target == "self" ? $"자신의 고유 효과 +{f.IV}" : $"사도마다 고유 효과 +{f.IV}";
                case FxK.NextAp: return $"다음 턴 AP {(f.IV >= 0 ? "+" : "")}{f.IV}";
                case FxK.Burn:
                    if (HasF(f)) return f.All ? $"{HandFilt(f)}모두 소멸" : $"{HandFilt(f)}{(f.Random ? "무작위 " : "")}{f.IV}장 소멸";
                    return f.All ? "손패 전부 소멸" : (f.Random ? $"무작위 손패 {f.IV}장 소멸" : $"손패에서 {f.IV}장 골라 소멸");
                case FxK.Reflect: return inRule ? $"그 피해의 {P(f.Ratio)}를 때린 적에게 고정 피해로" : $"지난 차례 받은 피해 {P(f.Ratio)} 되갚기";
                case FxK.PayHpPct: return $"HP {P(f.V)} 소모";
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
                        if (w.Chain) return $"{(any ? "" : "자신의 ")}바로 앞 카드와 성격이 같은 {(w.Type != null ? w.Type + " " : "")}카드를 내면";
                        if (w.SameHero) return $"{(any ? "" : "자신의 ")}바로 앞 카드와 같은 사도의 {(w.Type != null ? w.Type + " " : "")}카드를 내면";
                        // 「0코 이하」 → 「0코」, 「1코 이상 1코 이하」 → 「1코」
                        bool exact = w.MaxCost != null && (w.MaxCost == 0 || w.MinCost == w.MaxCost);
                        string type = (w.MaxCost != null ? $"비용 {w.MaxCost}{(exact ? "" : " 이하")} " : "") + (w.PaidMax != null ? $"비용 {w.PaidMax}{(w.PaidMax == 0 ? "으로" : " 이하로")} 낸 " : "") + (w.Basic ? "시작 " : "") + (w.Token == true ? "생성 " : w.Token == false ? "생성 카드가 아닌 " : "") + (w.Tag != null ? Q(TagKo(w.Tag)) + " " : "") + (w.Type != null ? w.Type + " " : "");
                        int minCost = exact ? 0 : w.MinCost ?? 0;
                        if (w.Marked != null) return $"{Ko.J(Q(w.Marked), "이가")} 붙은 아군이 {type}카드를 낼 때마다";
                        if (w.Every > 0)
                            return $"{(w.Who == "other" ? "다른 아군이 " : any ? "파티가 " : "자신의 ")}{(w.PerTurn ? "한 턴에 " : "")}{(minCost > 1 ? $"비용 {minCost} 이상 " : "")}{type}카드를 {w.Every}장 낼 때마다";   // who other(2026-10-12 밤 벨라 신탁)
                        return $"{(w.Who == "other" ? "다른 아군이 " : any ? "아군이 " : SelfOf(w))}{(minCost > 0 ? $"비용 {minCost} 이상 " : "")}{type}카드를 낼 때마다";
                    }
                case "guard": return (w.Mine ? "자신의 카드로 " : PartyOf()) + (w.Kind == "block" ? "방어를 얻으면" : w.Kind == "shield" ? "실드를 얻으면" : "방어나 실드를 얻으면");   // mine — 자신의 카드로 얻었을 때만(2026-10-13 비비)
                case "break": return w.Who == "other" ? "다른 아군이 적을 격파하면" : w.Mine ? "적을 격파하면" : "적이 격파되면";
                case "kill": return w.Who == "other" ? "다른 아군이 적을 처치하면" : w.Mine ? "적을 처치하면" : "적이 쓰러지면";
                case "formLap": return "태세가 한 바퀴 돌 때마다";
                case "formOn": { var fd = w.Id != null ? data?.Form(w.Id) : null; return w.Who == "any" ? "아군이 변신하면" : fd != null ? $"{Q(fd.Name)}에 들 때마다" : "변신할 때마다"; }
                case "hurt": return PartyOf() + (w.Pct > 0 ? $"한 번에 최대 HP {Num.Round(w.Pct * 100)}% 이상 잃으면" : w.Guarded ? "공격을 받으면(다 막아도)" : "피해를 받으면");
                case "endure": return "끈기로 버티면";
                case "unwound": return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}회복으로 부상에서 벗어나면";
                case "lowHp": return $"{(mine ? "파티 " : "")}HP가 {Num.Round(w.Pct * 100)}% 이하가 되면";
                case "rush": return "적이 즉시 행동하면";
                case "ult": return w.Who == "any" ? "아군이 고학년을 쓰면" : "고학년을 쓰면";
                case "debuff": return w.Id != null ? $"{(w.Who == "any" ? "아군이 " : w.Who == "other" ? "다른 아군이 " : "")}적에게 {Ko.J(Q(w.Id), "을를")} 걸면" : $"{(w.Who == "any" ? "아군이 " : w.Who == "other" ? "다른 아군이 " : "")}적에게 {(w.Fresh ? "새 디버프" : "디버프")}를 걸면";
                case "pulled": return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}{(w.Type != null ? w.Type + " " : "")}카드를 손으로 끌어오면";
                case "overheal": return w.Pct > 0 ? $"{(w.Who == "any" ? "아군의 " : "")}회복 뒤 HP가 {Num.Round(w.Pct * 100)}% 이상이면" : w.Who == "any" ? "아군의 회복량이 최대 HP를 초과하면" : "회복량이 최대 HP를 초과하면";
                case "guardSum": return w.N > 1 ? $"턴 시작 시 지난 적의 차례에 {w.N} 이상 막아 냈으면" : "턴 시작 시 지난 적의 차례에 막아 낸 것이 있으면";
                case "stackReach": return StageName(w.Id, w.N) is string stn ? $"{Ko.J(stn, "이가")} 되면" : $"{Ko.J(Q(w.Id), "이가")} {w.N}개가 되면";   // 단계 이름이 있으면 「칼이 되면」
                case "stackGone": return w.Decay ? $"{Ko.J(Q(w.Id), "이가")} 다 닳으면" : $"{Ko.J(Q(w.Id), "이가")} 사라지면";
                case "stackOver": return $"{Ko.J(Q(w.Id), "이가")} 최대라 넘치면";
                case "reserveGone": return "아군의 예약이 다 닳으면";
                case "switch": return w.Who == "any" ? "아군이 전환하면" : "전환하면";
                case "rhythm": return $"리듬이 {w.N}{(Ko.HasFinal(w.N.ToString()) ? "이" : "가")} 되면";
                case "always": return "항상";
                case "discard":
                    {
                        string what = w.Tag != null ? Ko.J(Q(TagKo(w.Tag)), "이가") : "카드가";
                        if (w.By != null) return $"{(w.By == "other" ? "다른 아군" : "자신")}의 효과로 {what} 버려지면";   // (엘다인 ▶E7)
                        return w.Who == "any" ? $"아군의 {what} 버려지면" : w.Who == "other" ? $"다른 아군의 {what} 버려지면" : $"자신의 {what} 버려지면";
                    }
                case "pay": return w.Who == "any" ? "아군이 HP를 치르면" : "HP를 치르면";
                case "exhaust":
                    if (w.By != null || w.Tag != null) return $"{(w.By == "other" ? "다른 아군의 효과로 " : w.By == "self" ? "자신의 효과로 " : w.Who == "any" ? "아군의 " : w.Who == "other" ? "다른 아군의 " : "자신의 ")}{(w.Tag != null ? Ko.J(Q(TagKo(w.Tag)), "이가") : "카드가")} 소멸하면";
                    return $"{(w.Who == "any" ? "아군의" : w.Who == "other" ? "다른 아군의" : "자신의")} {(w.Basic ? "시작 " : "")}{(w.Type != null ? w.Type + " " : "")}카드가 소멸하면";
                // 18갈래 공용 계기(2026-10-08)
                case "tally": return $"{WhoSub(w, "아군의", "다른 아군의", "")}셈이 맞으면";
                case "keepAp":
                    if (w.Kind == "keep") return w.N > 1 ? $"보존 카드를 {w.N}장 이상 쥐고 턴을 마치면" : "보존 카드를 쥐고 턴을 마치면";
                    if (w.Kind == "ap") return w.N > 1 ? $"AP를 {w.N} 이상 남기고 턴을 마치면" : "AP를 남기고 턴을 마치면";
                    return w.N > 1 ? $"AP를 {w.N} 이상 남기거나 보존 카드를 쥐고 턴을 마치면" : "AP를 남기거나 보존 카드를 쥐고 턴을 마치면";
                case "reserveFire": return $"{WhoSub(w, "아군의", "다른 아군의", "")}예약이 터지면";
                case "grow": return w.CardSt != null ? $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}카드의 {Ko.J(Q(w.CardSt), "이가")} 자라면" : $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}카드가 자라면";
                case "fightEnd": return "전투에서 이기면";
                case "counter": return w.Who == "any" ? "아군의 반격이 터지면" : "반격이 터지면";
                case "cutHit": return w.Who == "any" ? "아군이 적의 공격을 깎으면" : "적의 공격을 깎으면";
                case "summonAct":
                    {
                        string sid = w.Id != null ? Q(w.Id) : "소환물";
                        string who = w.Who == "any" ? "아군의 " : w.Who == "other" ? "다른 아군의 " : "";
                        return w.Kind == "atk" ? $"{who}{Ko.J(sid, "이가")} 따라 치면" : w.Kind == "guard" ? $"{who}{Ko.J(sid, "이가")} 공격을 대신 받으면" : w.Kind == "lost" ? $"{who}{Ko.J(sid, "이가")} 하나 사라지면" : $"{who}{Ko.J(sid, "이가")} 행동하면";
                    }
                case "huntDown": return w.Who == "any" ? "아군이 찍은 적이 쓰러지면" : "찍은 적이 쓰러지면";
                case "blocked": return "공격을 방어 · 실드로 다 막으면";
                case "spend": return $"{WhoSub(w, "아군이", "다른 아군이", "")}{Ko.J((w.Id != null ? Q(w.Id) : "키워드") + (w.N > 1 ? $" {w.N}개 이상" : ""), "을를")} 소모하면";
                case "link": return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}연계 카드가 저절로 나가면";
                case "crit": return $"{WhoSub(w, "아군이", "다른 아군이", "")}치명타를 내면";
                case "make": return $"{WhoSub(w, "아군이", "다른 아군이", "")}카드를 생성하면";
                case "drawn": if (w.Unique) return $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}고유 카드가 뽑히면"; return w.Marked != null ? $"{Ko.J(Q(w.Marked), "이가")} 붙은 아군의 카드가 뽑히면" : $"{WhoSub(w, "아군의", "다른 아군의", "자신의 ")}{(w.Tag != null ? Q(w.Tag) + " " : "")}{(w.Type != null ? w.Type + " " : "")}카드가 뽑히면";
                case "shuffle": return "뽑을 더미를 섞으면";
                case "extra": return $"{WhoSub(w, "아군이", "다른 아군이", "")}추가 공격하면";
                case "hit":
                    if (w.Marked != null) return $"{Ko.J(Q(w.Marked), "이가")} 붙은 아군의 카드가 {(w.FoeStack != null ? Ko.J(Q(w.FoeStack), "이가") + " 걸린 " : "")}적을 치면";   // (▶E64)
                    return $"{WhoSub(w, "아군의 카드가", "다른 아군의 카드가", "자신의 카드가 ")}{(w.FoeStack != null ? Ko.J(Q(w.FoeStack), "이가") + " 걸린 " : "")}적을 {(w.Weak ? "약점으로 " : "")}{(w.Each ? "칠 때마다(타마다)" : "치면")}".Replace("  ", " ");
                case "shieldBreak": return w.Mine ? "자신이 준 실드가 적의 공격에 깨지면" : "파티 실드가 적의 공격에 깨지면";
                case "foeShieldBreak": return w.Mine ? "적의 실드를 깨면" : "적의 실드가 깨지면";
                case "foeGuard": return "적이 실드를 얻으면";
                case "foeAct": return w.Type == "공격" ? "적이 공격하면(그 뒤)" : "적이 행동하면(그 뒤)";
                case "foeActBefore": return w.Type == "공격" ? "적이 공격하기 직전에" : "적이 행동하기 직전에";   // 「직전에 실드 30%」 — 화살표 없이 결과와 이어도 읽히게
                default: return w.On;
            }
        }

        /// <summary>
        /// 자주 쓰는 계기의 카제나 이름(2026-10-10 사용자 결정) — 규칙 머리를 「감응:」 · 「연계(공격):」 처럼 짧게, 풀이는 툴팁(TIPS).
        /// 감응(뽑힘) · 안식(버려짐) · 소각(소멸) · 연계(다른 아군이 냄) · 천상(비용 2 이상) · 연속(앞 카드와 성격 같음) · 붕괴(격파) · 파괴(처치).
        /// 우리 편 글만 · 꾸밈(장수 · 차례 · 태그 · 표식 …)이 붙은 계기와 드문 계기는 null — 문장 그대로.
        /// 괄호는 기본(아군 누구든)에서 벗어난 것만: 자신 · 다른 아군 · 카드 종류.
        /// </summary>
        public string TrigName(When w)
        {
            if (!mine || w == null) return null;
            string who = w.Who == "any" ? null : w.Who == "other" ? "다른 아군" : kwAlly ? null : "자신";
            static string N(string name, params string[] q) { var qs = q.Where(x => !string.IsNullOrEmpty(x)).ToList(); return name + (qs.Count > 0 ? $"({string.Join(" · ", qs)})" : "") + ":"; }
            switch (w.On)
            {
                case "drawn": return w.Unique || w.Marked != null || w.Tag != null ? null : N("감응", who, w.Type);
                case "discard": return w.Basic || w.By != null || w.Tag != null ? null : N("안식", w.Who == "other" ? "다른 아군" : w.Who == "any" ? null : "자신", w.Type);
                case "exhaust": return w.By != null || w.Tag != null ? null : N("소각", w.Who == "other" ? "다른 아군" : w.Who == "any" ? null : "자신", w.Basic ? "시작 카드" : null, w.Type);
                case "break": return N("붕괴", w.Who == "other" ? "다른 아군" : w.Mine ? "자신" : null);
                case "kill": return N("파괴", w.Who == "other" ? "다른 아군" : w.Mine ? "자신" : null);
                case "play":
                    if ((w.Seq != null && w.Seq.Count > 0) || w.Nth > 0 || w.Sig || w.Repeat || w.Marked != null || w.Every > 0 || w.Tag != null || w.MaxCost != null || w.PaidMax != null || w.Basic || w.Token != null || w.CardSt != null || w.SameHero) return null;
                    if (w.Chain) return (w.MinCost ?? 0) > 0 ? null : N("연속", who, w.Type);
                    // 「천상」 은 카드 태그(이드)로만 — 장비 · 패시브의 「비용 2 이상 카드를 내면」 은 문장 그대로(2026-10-12 사용자 「천상은 무기에 있으면 안 된다」)
                    if (w.Who == "other" && (w.MinCost ?? 0) <= 0) return N("연계", w.Type);
                    return null;
            }
            return null;
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
                case "hp": return $"{(mine ? "파티 " : "")}HP가 {Num.Round(c.Pct * 100)}% 이하면";
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
                case "goneMin": return $"이번 전투에 소멸한 카드가 {Math.Max(1, c.N)}장 이상이면";
                case "pricier": return "그 카드가 바로 앞 카드의 비용을 넘으면";
                case "gained": return c.Not ? $"이번 전투에 쌓은 {Ko.J(Q(c.Id), "이가")} {Math.Max(1, c.N)} 미만이면" : $"이번 전투에 쌓은 {Ko.J(Q(c.Id), "이가")} {Math.Max(1, c.N)} 이상이면";
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
        public string PowerText(Fx f) => string.Join("\n", PowerRules(f.Rules).Split('\n').Select(x =>
        {
            const string GS = "턴 시작 시 지난 적의 차례에 ";   // guardSum — 「이 전투 동안 매 턴: 100 이상 막아 냈으면 …」(엘다인 4차 글 길이)
            if (x.StartsWith(GS, StringComparison.Ordinal)) x = "매 턴: " + x.Substring(GS.Length);
            return x.StartsWith("매 턴 ", StringComparison.Ordinal) ? x : "이 전투 동안 " + x;
        }));

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
                    ruleOn = r.When?.On;
                    string head;
                    if (r.When?.On == "always") head = string.Join(" ", r.Conds.Select(CondText));
                    else
                    {
                        var parts = new List<string> { r.When?.On == "turnStart" ? "매 턴 시작 시" : r.When?.On == "turnEnd" ? "매 턴 종료 시" : TrigName(r.When) ?? WhenText(r.When) };
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
            finally { inRule = false; powerCard = pc; mine = m0; ruleOn = null; }
        }
        static Fx AsRun(Fx f) { var c = f.Copy(); c.Run = true; return c; }

        /// <summary>규칙 한 줄(이름 빼고) — 「피해를 받으면 반격 1 (턴당 1회)」.</summary>
        public string Rule(PassiveRule r)
        {
            bool m0 = mine; inRule = true; mine = true;
            try { return RuleBody(r); } finally { inRule = false; mine = m0; }
        }
        bool inRule;
        /// <summary>글을 짓는 규칙의 계기(when.on) — 「그 뒤 손패가」 는 카드를 낼 때 규칙에만(2026-10-09 깊이 시범).</summary>
        string ruleOn;
        /// <summary>
        /// 사도 패시브 · 고유 효과 글을 짓는 중(Docs/설명글.md) — 계기에 대상 낱말을 붙인다(「자신의 공격 카드를 낼 때마다」 · 「파티가 피해를 받으면」 ·
        /// 「대상 적에게 「X」가 있으면」). 강화 카드 지속 규칙(PowerRules) 글은 이 표시 없이 옛 꼴 그대로.
        /// </summary>
        bool mine;
        /// <summary>파티 층 고유 효과(carrier ally)의 규칙 — who 가 없으면 누구의 카드든(엔진 Matches 의 kwOf ally).</summary>
        bool kwAlly;
        string SelfOf(When w) => mine && w.Who == null && !kwAlly ? "자신의 " : "";
        string PartyOf() => mine ? "파티가 " : "";

        string RuleBody(PassiveRule r) { var o0 = ruleOn; ruleOn = r.When?.On; try { return RuleLine(RuleHead(r), Aim(r.When, AlwaysFx(r)), RuleLimit(r)); } finally { ruleOn = o0; } }
        /// <summary>늘 켜진 규칙(always)의 효과 — 증감은 켜져 있는 동안 내내라 「이번 턴」 · 「이 전투 동안」 없이(강화 규칙 PowerRules 와 같은 꼴, 2026-10-12 옛 「항상 이번 턴 자신의 주는 피해 6% 증가」).</summary>
        string AlwaysFx(PassiveRule r)
        {
            // 계기가 때린 적을 짚으면(hurt · blocked) 한 명 대상 말을 「때린 적」 으로 남긴다 — 그 밖의 한 명 대상은 말 없이(OneDrop)
            var a0 = aimWho; aimWho = mine && (r.When?.On == "hurt" || r.When?.On == "blocked") ? "때린 적" : null;
            try { return r.When?.On != "always" ? Fx(r.Fx) : Fx(r.Fx.Select(x => FxK.Mods.Contains(x.K) && !x.Run ? AsRun(x) : x).ToList()).Replace("이 전투 동안 ", ""); }
            finally { aimWho = a0; }
        }

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
        /// <summary>
        /// 계기 + 결과 — 「계기 결과 (턴당 N회)」(2026-10-07 화살표 없앰 — 띄어쓰기 하나로 잇고, 머리는 Reg 로 적어 둔다).
        /// 결과가 말로 쓴 조건 하나로 시작하면(「그 뒤 손패가 없으면 …」) 계기에 붙여 한 머리로.
        /// </summary>
        static string RuleLine(string head, string fx, string limit)
        {
            if (fx.Length == 0) return head + limit;
            if (head.Length == 0) return fx + limit;
            // 규칙 속 조건은 「X면: Y」 로 온다 — 문장이 하나면 「계기 X면 Y」
            int colon = fx.IndexOf(": ", StringComparison.Ordinal);
            if (colon > 0 && fx.IndexOf(". ", StringComparison.Ordinal) < 0 && fx.IndexOf(": ", colon + 2, StringComparison.Ordinal) < 0)
            {
                string cond = fx.Substring(0, colon);
                if ((cond.EndsWith("면", StringComparison.Ordinal) || cond.EndsWith("확률로", StringComparison.Ordinal)) && !cond.Contains(", ")) return Reg(head + " " + cond) + " " + fx.Substring(colon + 2) + limit;
            }
            // 결과 안에 조건 문장이 여럿이면(「X면: Y. Z면: W」) 계기 뒤를 쉼표로 끊어 계기가 조건에 섞이지 않게
            return Reg(head) + (NoParen(fx).Contains(": ") && !head.EndsWith(":") ? ", " : " ") + fx + limit;   // 괄호 속 「(주인: 다음 아군)」 은 조건 머리가 아니다(옛 「턴 시작 시, 드로우 1」)
        }

        /// <summary>
        /// 규칙의 계기 · 조건 글. 적에게 쌓는 고유 효과(carrier enemy)를 보는 조건이 처치 · 격파 계기와 붙으면 한 마디로 —
        /// 「적이 쓰러지면 「깃발」이 있으면」 → 「「깃발」이 있는 적이 쓰러지면」(엔진: 조건의 대상 = 쓰러진 적).
        /// </summary>
        (string when, string conds) HeadParts(PassiveRule r, bool alwaysWhen)
        {
            var conds = r.Conds.ToList();
            string when = alwaysWhen || r.When.On != "always" || conds.Count == 0 ? TrigName(r.When) ?? WhenText(r.When) : null;
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
            if (r.When?.On == "always" && when == WhenText(r.When)) when = null;   // 「항상」 은 쓰지 않는다(2026-10-12)
            return string.Join(" ", new[] { when, conds }.Where(x => !string.IsNullOrEmpty(x)));
        }
        static string RuleLimit(PassiveRule r) => r.Limit != null ? $" ({(r.Limit.Per == "fight" ? "전투당" : "턴당")} {r.Limit.N}회)" : "";
        /// <summary>조건 머리 · 여러 문장이 없는 효과 글(「, 」 로 이어 붙여도 되는 것).</summary>
        static string NoParen(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\([^()]*\)", "");
        static bool Plain(string fx) => fx.Length > 0 && !NoParen(fx).Contains(": ") && !fx.Contains(". ") && !fx.Contains("\n");
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
                if (r.When?.On == "always" && when == WhenText(r.When)) when = null;   // 「항상」 은 쓰지 않는다 — 계기 없는 규칙 = 늘 켜짐
                string fx = Aim(r.When, AlwaysFx(r)), limit = RuleLimit(r);
                var last = o.Count > 0 ? o[o.Count - 1] : null;
                if (last != null && (!byName || (r.Name != null && r.Name == last.Name)) && last.Conds == conds && last.Limit == limit)
                {
                    if (last.Whens.Count == 1 && last.Whens[0] == when && Plain(fx) && Plain(last.Fx)) { last.Fx += ", " + fx; continue; }
                    if (fx == last.Fx && limit.Length == 0 && fx.Length > 0 && when != null && when.EndsWith("면", StringComparison.Ordinal) && last.Whens.All(w => w != null && w.EndsWith("면", StringComparison.Ordinal))) { last.Whens.Add(when); continue; }
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
                if (head.StartsWith(subj, StringComparison.Ordinal) && head.Length > subj.Length && char.IsDigit(head[subj.Length]))
                {
                    string all = q + " 전부 소모";
                    var m = System.Text.RegularExpressions.Regex.Match(fx, System.Text.RegularExpressions.Regex.Escape(q) + @" (\d+) 소모");
                    if (fx.Contains(all)) { head += " 모두 써서"; fx = Cut(fx, all); }
                    else if (m.Success) { head += $" {m.Groups[1].Value}개 써서"; fx = Cut(fx, m.Value); }
                }
            }
            return fx.Length > 0 ? $"{Reg(head)} {fx}{g.Limit}" : $"{head}{g.Limit}";
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
            // 패시브를 고유 능력(keyword rules)으로 합친 사도(2026-10-10)는 패시브 칸을 두지 않는다 — 고유 효과도 패시브도 없을 때만 「없음」
            if (rest.Count == 0 && kws.Count == 0) o.Add(new TraitText { Kind = "패시브", Name = NoPassive, Body = "" });
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
            string dur = f.Turns > 0 ? $"{f.Turns}턴 지속" : f.Stance ? "풀릴 때까지" : "전투 끝까지";
            if (f.Until != null) dur += $" · {WhenText(f.Until).Replace("낼 때마다", "내면")} 풀림";
            s.Add(dur);
            if (f.OnEnter != null && f.OnEnter.Count > 0) s.Add($"{Reg("들 때")} {Fx(f.OnEnter)}");
            s.AddRange(FormDuring(f, true));
            if (f.Off != null && f.Off.Count > 0) s.Add($"{Reg("풀릴 때")} {Fx(f.Off)}");
            if (f.OffExpire != null && f.OffExpire.Count > 0) s.Add($"{Reg("시간이 다 되면")} {Fx(f.OffExpire)}");
            if (f.OffEnd != null && f.OffEnd.Count > 0) s.Add($"{Reg("스스로 끝내면")} {Fx(f.OffEnd)}");
            s.AddRange(StanceLines(f));
            bool byMax = data != null && data.Heroes.Values.Any(h => h.AllKeywords.Any(k => k.OnMax?.Form == f.Id));
            s.Add(byMax ? "변신 중에 다시 최대가 되어도 그대로" : "다시 쓰면 지속이 처음으로");
            return Bullets(s);
        }

        /// <summary>
        /// (태세) 전투 시작 태세 · 번갈아 바뀜(toggle) · 이 태세에서 덤이 붙는 카드(카드의 「「태세」: …」) — 데이터에서 찾는다.
        /// </summary>
        List<string> StanceLines(FormDef f)
        {
            var o = new List<string>();
            if (data == null || f?.Id == null) return o;
            var h = data.Heroes.Values.FirstOrDefault(x => x.Forms != null && x.Forms.Contains(f));
            if (h == null) return o;
            if (h.StartForm == f.Id) o.Add($"{Reg("전투 시작")} 이 {(f.Stance ? "태세" : "변신")}로 시작");
            if (f.Toggle != null && h.Forms.FirstOrDefault(x => x.Id == f.Toggle) is FormDef to)
                o.Add($"{h.Name}{(Ko.HasFinal(h.Name) ? "이" : "가")} 자기 카드를 내면 {Q(to.Name)}{Ro(to.Name)} 바뀜(태세를 바꾸지 않는 카드 · 변신 카드 빼고)");
            bool Has(List<Fx> fx) => fx != null && fx.Any(x => x != null && ((x.K == FxK.IfForm && x.Id == f.Id) || Has(x.Then)));
            var bon = new List<string>();
            foreach (var c in data.Cards.Values)
                if (c.Hero == h.Id && c.Name != null && (Has(c.Fx) || (c.Oracles ?? new List<OracleDef>()).Any(or => Has(or.Fx))) && !bon.Contains(Q(c.Name))) bon.Add(Q(c.Name));
            // 「덤이 붙는 카드 — …」 목록도 뺀다(2026-10-11 사용자) — 카드 글에 「「태세」:」 덤이 이미 보인다
            _ = bon;
            return o;
        }

        /// <summary>변신 자세히 — 설명 · 지속 · 풀리는 계기 · 그동안(능력치 · 카드 · 덤 · 패시브) · 풀릴 때.</summary>
        public string Form(FormDef f)
        {
            var s = new List<string>();
            if (!string.IsNullOrEmpty(f.Desc)) s.Add(f.Desc.TrimEnd('.'));
            s.Add(f.Turns > 0 ? $"{f.Turns}턴 지속" : f.Stance ? "풀릴 때까지" : "전투 끝까지");
            if (f.Until != null) s.Add($"{WhenText(f.Until).Replace("낼 때마다", "내면")} 풀림");
            var during = FormDuring(f, true);
            if (f.OnEnter != null && f.OnEnter.Count > 0) s.Add($"들 때: {Fx(f.OnEnter)}");
            if (during.Count > 0) s.Add("그동안 " + string.Join(" · ", during));
            if (f.Off != null && f.Off.Count > 0) s.Add($"풀릴 때: {Fx(f.Off)}");
            if (f.OffExpire != null && f.OffExpire.Count > 0) s.Add($"시간이 다 되면: {Fx(f.OffExpire)}");
            if (f.OffEnd != null && f.OffEnd.Count > 0) s.Add($"스스로 끝내면: {Fx(f.OffEnd)}");
            bool byMax = data != null && data.Heroes.Values.Any(h => h.AllKeywords.Any(k => k.OnMax?.Form == f.Id));
            s.Add(byMax ? "변신 중에 다시 최대가 되어도 그대로" : "다시 쓰면 지속이 처음으로");
            return string.Join(". ", s) + ".";
        }

        /// <summary>변신 글(= Form) — 짧은 글 따로 없음.</summary>
        public string Short(FormDef f) => Form(f);
        public string Detail(FormDef f) => Form(f);

        /// <summary>(엘다인 ▶E6) 변신 카드 줄 — 「매 턴 「맨주먹 연타」 2 · 「발 구르기」 1」 · 「「힘, 그 자체」 6 이상이 되면 그 턴 「기혈 폭발」 1」.</summary>
        public string FormMakeText(FormDef f)
        {
            if (f?.TurnMake == null || f.TurnMake.Count == 0) return null;
            var plain = f.TurnMake.Where(m => m.When == null).Select(m => $"{Q(CardName(m.Id))} {m.NOr1}").ToList();
            var cond = f.TurnMake.Where(m => m.When != null).Select(m => $"{Ko.J(Q(m.When.Stack), "이가")} {m.When.N} 이상이 되면 그 턴 {Q(CardName(m.Id))} {m.NOr1}").ToList();
            var parts = new List<string>();
            if (plain.Count > 0) parts.Add("매 턴 " + string.Join(" · ", plain));
            parts.AddRange(cond);
            return string.Join(" · ", parts);
        }

        List<string> FormDuring(FormDef f, bool full)
        {
            var s = new List<string>();
            if (FormMakeText(f) is string fm) s.Add(fm);
            if (f.Mods != null)
                foreach (var kv in f.Mods)
                {
                    int pct = Num.Round(kv.Value * 100);
                    s.Add($"자신의 {(STAT_KO.TryGetValue(kv.Key, out var k) ? k : kv.Key == "heal" ? "회복량" : kv.Key)} {StatPct(kv.Key, pct)}");
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
                if (b.Hits > 1) add.Add($"{b.Hits}연타");
                foreach (var t in b.Tags ?? new List<string>()) add.Add($"{t}.");
                if (b.Fx != null && b.Fx.Count > 0) add.Add(Fx(b.Fx));
                if (add.Count > 0) s.Add($"{Reg($"{Ko.J(who.TrimEnd(), "을를")} 내면")} {string.Join("", add.Select((x, i) => i == 0 ? x : (add[i - 1].EndsWith(".") ? " " : ", ") + x))}");   // 「피해 ×2, 적 1명 강인도 피해 1」(옛 쉼표 빠짐)
            }
            if (f.Replace) s.Add("원래 패시브는 쉼");
            if (f.Passives != null && f.Passives.Count > 0) s.Add(full ? Passives(f.Passives) : string.Join(" · ", f.Passives.Select(r => $"{r.Name ?? "패시브"}: {Short(r)}")));
            return s;
        }

        /// <summary>패시브 여럿(= Passives) — 짧은 글 따로 없음.</summary>
        public string ShortPassives(List<PassiveRule> rules) => Passives(rules);

        static bool IsPer(string k) => k == FxK.PerStack || k == FxK.PerRhythm || k == FxK.PerDiscarded || k == FxK.PerPaid || k == FxK.PerDebuff || k == FxK.PerApLeft || k == FxK.PerTag || k == FxK.PerPlayed || k == FxK.PerPile || k == FxK.PerCardSt || k == FxK.PerEvent || k == FxK.PerGuarded || k == FxK.PerOverheal || k == FxK.PerGone || k == FxK.PerX || k == FxK.PerFoe;

        /// <summary>효과 요약 — 조건 머리 앞까지, 소모는 빼고, 효과를 budget 글자까지 담고 남으면 「 등」.</summary>
        string ShortFx(List<Fx> fx, int budget)
        {
            if (fx == null || fx.Count == 0) return "";
            if (CondHead(fx[0]) != null) { var all = Fx(fx); int dot = all.IndexOf(". ", StringComparison.Ordinal); return dot > 0 ? all.Substring(0, dot) + " 등" : all; }
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
            if (units.Count == 0) { var all = Fx(fx); int dot = all.IndexOf(". ", StringComparison.Ordinal); return dot > 0 ? all.Substring(0, dot) : all; }
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
        /// <summary>손 따라가기(keyword hand)가 세는 카드 — 카드 id 면 그 이름, 아니면 태그.</summary>
        string HandQ(KeywordDef k) => Q(data?.Card(k.Hand) != null ? CardName(k.Hand) : k.Hand);
        List<string> KeywordLinesIn(KeywordDef k)
        {
            var s = new List<string>();
            string q = Q(k.Name), car = k.Carrier ?? "self";
            string Has(bool not = false) => $"{Ko.J(q, "이가")} {(not ? "없" : "있")}";
            // 쌓는 곳 · 최대
            var head = new List<string>();
            if (car == "enemy") head.Add(OwnerName(k) is string on0 ? $"적에게 거는 디버프({on0} 전용)" : "적에게 거는 디버프");   // 「표식」 은 뜻 없는 꾸밈말이라 뺌(2026-10-11 사용자)
            else if (car == "ally") head.Add("파티에 쌓임");
            else if (car == "hero") head.Add(k.Cap == 1 ? "아군에게 붙이는 표시" : "아군에게 붙임(사도마다 따로)");
            else if (k.Hand != null) head.Add($"손에 쥔 {HandQ(k)} 장수만큼(저절로 맞춰짐)");   // 손 따라가기(2026-10-13 시온 「탄창」)
            else head.Add(k.Mode ? "자신의 상태" : k.CardValue != null ? "카드에 붙음(그 카드에만)" : "자신에게 쌓임");   // 카드 값(비비(신성) · 티그(영웅) 「성장」)은 카드 한 장 한 장에
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
            if (k.HuntCarry == "lowEnemy") head.Add("쓰러지면 가장 약한 적에게 겹째로 옮김");
            if (k.Known) head.Add("적의 수 종류(공격 · 방어 · 강화 · 방해)를 적어 둠 · 전투 끝까지 · 1당 아는 종류 하나");
            if (k.Burn != null) head.Add($"{(car == "self" ? $"{OwnerName(k) ?? "주인"} 말고 동료가 카드를 낼 때마다" : "붙은 동료가 자기 카드를 낼 때마다")} 1 탐{(k.Burn.Shield > 0 ? $" · 실드 {P(k.Burn.Shield)}" : "")}{(k.Burn.Heal > 0 ? $" · 회복 {P(k.Burn.Heal)}" : "")}{(k.Burn.Shield > 0 || k.Burn.Heal > 0 ? $"({OwnerName(k) ?? "주인"}의 방어력)" : "")}");
            if (k.Reserve) head.Add("예약 — 재촉으로 감소");
            s.Add(string.Join(" · ", head));
            // 1당
            string owner = OwnerName(k);
            string byAtk = owner != null ? $"({owner}의 공격력)" : "";
            foreach (var p in k.Per)
            {
                if (p.Stat == "dot")
                    s.Add(car == "self" ? $"{Reg("내 턴이 끝날 때")} 무작위 적 1명에게 {q} 1당 피해 {P(p.Ratio)}{byAtk}"
                        : car == "enemy" ? $"{Reg("내 턴이 끝날 때")} {q} 1당 피해 {P(p.Ratio)}{byAtk}"
                        : $"{Reg("내 턴이 끝날 때")} 파티에 {q} 1당 피해 {P(p.Ratio)}");
                else if (p.Stat == "hot") s.Add($"{Reg("내 턴이 끝날 때")} {q} 1당 회복 {P(p.Ratio)}{(owner != null ? $"({owner}의 방어력)" : "")}");
                else
                {
                    string stat = STAT_KO.TryGetValue(p.Stat, out var sk) ? sk : p.Stat;
                    int pct = Num.Round(p.V * 100);
                    string v = StatPct(p.Stat, pct);
                    if (car == "enemy")
                    {
                        if (p.From == "owner")
                        {
                            s.Add($"{q} 1당 {owner ?? "건 사도"}에게 받는 피해 {v}");   // 한 명 대상 말 없이(2026-10-12 밤 — 「그 적이」 뺌)
                            if (p.Else != 0) { int e = Num.Round(p.Else * 100); s.Add($"{Has(true)}는 적은 {owner ?? "건 사도"}에게 받는 피해 {(e >= 0 ? "+" : "")}{e}%"); }
                        }
                        else if (p.From == "marked" && p.Mark != null)   // (2026-10-11 실비아 티파티)
                        {
                            s.Add($"{q} 1당 {Ko.J(Q(p.Mark), "이가")} 붙은 아군에게 받는 피해 {v}");
                            if (p.Else != 0) { int e = Num.Round(p.Else * 100); s.Add($"{Has(true)}는 적은 {Ko.J(Q(p.Mark), "이가")} 붙은 아군에게 받는 피해 {(e >= 0 ? "+" : "")}{e}%"); }
                        }
                        else s.Add($"{q} 1당 {stat} {v}");   // 「「수은 중독」 1당 받는 피해 +8%」(2026-10-12 밤 — 걸린 적 자신의 것이라 「그 적의」 뺌)
                    }
                    else if (car == "hero") s.Add($"{q} 1당 붙은 아군의 {stat} {v}");
                    else if (p.Who == "marked") s.Add($"{q} 1당 {Q(p.Mark)} 아군의 {stat} {v}");   // 표시된 아군만(엘다인 4차 ▶E81)
                    else if (p.Who == "allies" && p.Stat == "taken" && car != "ally" && car != "party") s.Add($"{q} 1당 파티가 받는 피해 {v}");   // 파티는 한 몸 — 받는 피해는 파티 것(2026-10-13 클로에 「바느질」)
                    else if (car == "ally" || p.Who == "allies") s.Add(car != "ally" && car != "party" ? $"{q} 1당 아군 모두의 {stat} {v}" : $"{q} 1당 {stat} {v}");   // 표시 없음 = 파티
                    else if (k.Hand != null) s.Add($"손에 쥔 {HandQ(k)} 1장당 자신의 {stat} {v}");   // 손 따라가기 — 「「탄창」 1당」 대신 손의 카드로 읽히게
                    else s.Add($"{q} 1당 자신의 {stat} {v}");
                }
            }
            // 성질
            if (k.WeakOwner) s.Add($"{Has()}는 적은 이 사도에게만 약점으로 맞음");
            if (k.Weakens) s.Add($"{Has()}는 적은 아군 카드에 약점으로 맞음 — 카드 1장이 칠 때마다 −1");
            if (k.Guard)
            {
                string per = k.Uses > 1 ? $"{k.Uses}대마다" : "한 대마다";
                s.Add(k.Cut > 0 ? $"{Reg("적의 공격 한 대마다")} 파티가 받는 피해 −{P(k.Cut)}, {per} {q} −1" : $"적의 공격 한 대를 대신 받음(파티는 안 맞음) · {per} {q} −1");
            }
            if (k.TagWhile != null) s.Add($"{Has()}으면 자신의 {k.TagType ?? "공격"} 카드에 「{k.TagWhile}」");
            if (k.Spread) s.Add($"{Ko.J(q, "이가")} 0이면 자신의 공격 카드(적 1명)가 적 전체를 침");
            if (k.CardValue != null && (k.CardValue.Grow != null || k.CardValue.Reset != null || k.CardValue.Cost != 0))   // 늘고 줄 규칙이 없으면(티그 「성장」 — 카드가 스스로 올림) 빈 줄을 안 쓴다
                s.Add($"카드 값 — {(k.CardValue.Grow == "handEnd" ? "턴 끝에 손에 있는 자신의 고유 카드마다 +1" : k.CardValue.Grow == "allyPlay" ? $"{(k.CardValue.Who == "other" ? "다른 아군이" : "아군이")} 카드를 낼 때마다 손의 자신의 고유 카드마다 +1{(k.CardValue.Max > 0 ? $"(최대 {k.CardValue.Max})" : "")}" : "")}{(k.CardValue.Cost != 0 ? $" · 1당 비용 {(k.CardValue.Cost > 0 ? "+" : "")}{k.CardValue.Cost}" : "")}{(k.CardValue.Grow != null && k.CardValue.Reset != null ? " · " : "")}{(k.CardValue.Reset == "play" ? "낸 그 카드만 0으로(손의 다른 카드는 그대로)" : "")}");
            if (k.StartMake != null) s.Add($"{Reg("전투 시작")} {Q(CardName(k.StartMake))} {Math.Max(1, k.StartMakeN)}장 생성");   // (2026-10-13 시온)
            if (k.Start is int ks && ks > 0) s.Add($"{Reg("전투 시작")} {(k.Stages != null && ks <= k.Stages.Count ? k.Stages[ks - 1] : ks.ToString())}");
            if (k.Share > 0) s.Add($"붙은 아군을 노린 적의 공격 피해 -{P(k.Share)} · 깎인 만큼 파티 실드");
            if (k.OnStage != null) for (int si = 0; si < k.OnStage.Count; si++) if (k.OnStage[si] != null && k.OnStage[si].Count > 0) s.Add($"{Reg((k.Stages != null && si < k.Stages.Count ? k.Stages[si] : (si + 1) + "칸") + "에 닿으면")} {Fx(k.OnStage[si])}");
            foreach (var sb in k.StageBonus ?? new List<StageBonus>())
            {
                var parts = new List<string>();
                if (sb.Ratio > 0) parts.Add($"피해 ×{N(sb.Ratio)}");
                if (sb.Fx != null && sb.Fx.Count > 0) parts.Add(Fx(sb.Fx));
                if (sb.Tags != null && sb.Tags.Count > 0) parts.Add(string.Join(" · ", sb.Tags.Select(Q)));
                int sbs = sb.Stage > 0 ? sb.Stage : k.StageBonus.IndexOf(sb) + 1;
                string stName = k.Stages != null && sbs >= 1 && sbs <= k.Stages.Count ? k.Stages[sbs - 1] : sbs + "칸";
                s.Add($"{Reg(stName + "이면")} 자신의 {(sb.Card != null ? Q(CardName(sb.Card)) + " " : sb.Type != null ? sb.Type + " " : "")}카드에 {string.Join(" · ", parts)}");
            }
            if (k.Tally != null && k.Tally.Count > 0 && k.TallyTag != null)   // 그 카드 · 태그만(2026-10-13 시온 — 「「마탄」 카드가 버려질 때 · 소멸할 때마다 +1」)
            {
                var when = new[] { ("play", "낼 때"), ("discard", "버려질 때"), ("exhaust", "소멸할 때"), ("bullet", "버려져 발사될 때") }.Where(x => k.Tally.Contains(x.Item1)).Select(x => x.Item2).ToList();
                s.Add($"{Reg($"{Q(data?.Card(k.TallyTag) != null ? CardName(k.TallyTag) : k.TallyTag)} 카드{(k.Tally.Contains("play") ? "를" : "가")} {string.Join(" · ", when)}마다")} +1");
            }
            else if (k.Tally != null && k.Tally.Count > 0)   // 엔진이 세는 수(엘다인 3차)
                s.Add($"{Reg(k.Tally.Contains("play") && k.Tally.Contains("discardBy") ? "자신이 카드를 내거나 자신의 효과로 카드가 버려질 때마다" : k.Tally.Contains("play") ? "자신이 카드를 낼 때마다" : "자신의 효과로 카드가 버려질 때마다")} +1");
            // 사라짐
            if (k.DecayAll) s.Add($"{Reg("적의 차례가 끝나면")} 모두 사라짐");
            else if (k.Decay > 0) s.Add($"{Reg("적의 차례가 끝나면")} −{k.Decay}");
            bool uses = car != "hero" && k.Per.Any(p => p.Stat == "dealt" || p.Stat == "atk" || p.Stat == "crit" || p.Stat == "taken");
            if (k.Consumes && uses)
            {
                bool critOnly = k.Per.All(p => p.Stat == "crit");
                string by = car == "self" ? (critOnly ? "자신의 공격 카드가 치명타를 내면" : "자신의 공격 카드를 내면(1당 덤이 실린 뒤)")
                    : car == "enemy" ? (critOnly ? "그 적이 치명타를 맞으면" : "그 적이 아군 카드에 맞으면")
                    : critOnly ? "공격 카드가 치명타를 내면" : "공격 카드를 내면";
                s.Add($"{Reg(by)} {(k.ConsumeAll ? "모두 사라짐" : $"−{k.Consume}")}");
            }
            if (k.Wipe) s.Add(k.EndClear ? $"{Reg("다른 사도의 카드를 내거나 내 턴이 끝나면")} 모두 사라짐" : $"{Reg("다른 사도의 카드를 내면")} 모두 사라짐");
            else if (k.EndClear) s.Add($"{Reg("내 턴이 끝나면")} 모두 사라짐");
            if (!k.EndClear && k.EndDecay > 0) s.Add($"{Reg("내 턴이 끝나면")} −{k.EndDecay}");
            // 줄어드는 길이 하나도 없으면 그렇다고 적는다(「언제 사라지나」 를 늘 보이게 — 2026-10-11 사용자 「설명 충분하게」)
            bool shrinks = k.Hand != null || k.Decays || k.EndClear || k.EndDecay > 0 || k.Wipe || (k.Consumes && uses) || k.Weakens || k.Guard || k.Burn != null;
            if (!shrinks && !k.Mode && !k.Wrap && !k.Known && k.CardValue == null)
                s.Add(car == "enemy" ? (k.HuntCarry != null ? "전투 끝까지 남음 · 적이 쓰러지면 옮겨 감" : "전투 끝까지 남음 · 그 적이 쓰러지면 사라짐") : "전투 끝까지 남음");   // 옛 「저절로 줄지 않음(전투 끝까지)」 — 괄호 덜기(2026-10-12)
            // s.AddRange(KwUseLines(k));   // 「얻는 카드 · 쓰는 카드 — …(신탁)」 목록은 뺀다(2026-10-11 사용자: 「얻는 카드 · 신탁 이런 거 없애 줘」) — 함수는 남겨 둔다
            if (k.OnMax != null && k.OnMax.Make != null) s.Add($"{Reg($"최대가 되면{(k.OnMax.Consume ? " 모두 써서" : "")}")} {Q(CardName(k.OnMax.Make))} {Math.Max(1, k.OnMax.N)}장 생성");
            else if (k.OnMax != null && k.OnMax.Empower != null) s.Add($"{Reg($"최대가 되면{(k.OnMax.Consume ? " 모두 써서" : "")}")} {(k.OnMax.Empower == "any" ? "파티의" : "자신의")} 다음 카드 효과 +{P(k.OnMax.Ratio)}");
            else if (k.OnMax != null) { var fd = data?.Form(k.OnMax.Form); string nm = fd?.Name ?? k.OnMax.Form; s.Add($"{Reg($"최대가 되면{(k.OnMax.Consume ? " 모두 써서" : "")}")} {Q(nm)}{Ro(nm)} 변신{(fd != null ? $"({FormDur(fd)})" : "")}"); }
            // 자기 규칙 — 계기 · 조건 · 횟수가 같은 잇단 규칙은 효과를, 효과가 같은 잇단 규칙은 계기를 한 줄로 묶는다
            bool a0 = kwAlly, m0 = mine, r0 = inRule; kwAlly = car == "ally"; mine = true; inRule = true;
            try { foreach (var g in Group(k.Rules, false, true)) s.Add(SelfRule(k.Name, g)); }
            finally { kwAlly = a0; mine = m0; inRule = r0; }
            return s;
        }

        /// <summary>
        /// 고유 효과를 얻는 곳 · 쓰는 곳(그 사도의 카드 · 고학년에서 찾는다) — 「얻는 카드 「A」 · 「B」」 · 「쓰는 카드 「C」」 · 「조건으로 보는 카드 …」.
        /// 신탁에만 있으면 「(신탁)」. 데이터가 바뀌면 글도 따라 바뀐다.
        /// </summary>
        List<string> KwUseLines(KeywordDef k)
        {
            var o = new List<string>();
            var h = ctxHero;
            if (data == null || h == null || k?.Name == null) return o;
            var gain = new List<string>(); var use = new List<string>(); var look = new List<string>();
            int Kind(List<Fx> fx)
            {   // 1 얻음 · 2 씀 · 4 봄(조건 · 1당)
                int m = 0;
                foreach (var f in fx ?? new List<Fx>())
                {
                    if (f == null) continue;
                    if (f.Id == k.Name)
                    {
                        if (f.K == FxK.Stack) m |= (f.IV < 0 ? 2 : 1);
                        else if (f.K == FxK.Spend) m |= 2;
                        else if (f.K == FxK.IfStack || f.K == FxK.PerStack) m |= 4;
                    }
                    m |= Kind(f.Then);
                    foreach (var r in f.Rules ?? new List<PassiveRule>()) m |= Kind(r.Fx);
                }
                return m;
            }
            void Put(string name, int baseM, int oraM)
            {
                string Lbl(int bit) => (baseM & bit) != 0 ? Q(name) : (oraM & bit) != 0 ? $"{Q(name)}(신탁)" : null;
                if (Lbl(1) is string g && !gain.Contains(g)) gain.Add(g);
                if (Lbl(2) is string u && !use.Contains(u)) use.Add(u);
                if ((baseM & 3) == 0 && (oraM & 3) == 0 && Lbl(4) is string l && !look.Contains(l)) look.Add(l);
            }
            var seen = new HashSet<string>();
            foreach (var c in data.Cards.Values)
            {
                if (c.Hero != h.Id || c.Name == null || !seen.Add(c.Name)) continue;
                int om = 0; foreach (var or in c.Oracles ?? new List<OracleDef>()) om |= Kind(or.Fx);
                Put(c.Name, Kind(c.Fx), om);
            }
            if (h.Ult != null) Put($"고학년 {h.Ult.Name}", Kind(h.Ult.Fx), 0);
            for (int i = 0; i < gain.Count; i++) gain[i] = gain[i].Replace("「고학년 ", "고학년 「");
            for (int i = 0; i < use.Count; i++) use[i] = use[i].Replace("「고학년 ", "고학년 「");
            if (gain.Count > 0) o.Add($"얻는 카드 — {string.Join(" · ", gain)}");
            if (use.Count > 0) o.Add($"쓰는 카드 — {string.Join(" · ", use)}");
            if (look.Count > 0) o.Add($"겹을 보는 카드 — {string.Join(" · ", look)}");
            return o;
        }

        // ── 적의 수 ───────────────────────────────────────────────────
        /// <summary>적의 수 — 머리 위 · 정보 창. shown 은 엔진이 셈한 실제 피해(Battle.IntentHit)가 있으면 그것.</summary>
        /// <param name="dmgx">층 배율(Rules.FoeScale 의 dmg) — 치는 수(Battle.FOE_HITS)의 값에 곱한다. 도감이 실제 전투 값을 보일 때(2026-10-13). shown 이 있으면 맨 위 수는 shown.</param>
        public string Intent(Intent it, int? shown = null, double dmgx = 1)
        {
            if (it == null) return "";
            int v = shown ?? (dmgx != 1 && Array.IndexOf(Battle.FOE_HITS, it.T) >= 0 ? Battle.FoeV(new Unit { Dmgx = dmgx }, it) : it.T == "attackAll" ? Num.Round(it.V * R.FOE_ALL_X) : it.V);
            string s = it.T switch
            {
                "attack" => $"공격 {v}",
                "back" => $"관통 {v}(방어 무시)",
                "attackAll" => $"전체 공격 {v}",
                "multi" => $"{v} × {Math.Max(1, it.N)}회",
                "charge" when it.Next?.T == "ult" => $"고학년 예고 — 다음 턴 {Intent(it.Next, null, dmgx)}",
                "charge" => $"힘 모으기 — 다음 턴 {Intent(it.Next, null, dmgx)}",
                "ult" => $"고학년 「{it.Say}」: {string.Join(" · ", (it.Then ?? new List<Intent>()).Select(x => Intent(x, null, dmgx)))}",
                "block" => $"방어 +{it.V}",
                "guard" => $"적 전체 방어 +{it.V}",
                "heal" => $"적 회복 {it.V}",
                "buff" => $"{(it.All ? "적 전체 " : "자신에게 ")}{it.Id} +{it.V}",
                "debuff" => $"파티 {it.Id} +{it.V}",
                "jam" => $"다음 턴 AP -{it.V}",
                "addCard" => $"{Q(CardName(it.Id))} {Math.Max(1, it.N)}장 → {(it.To == "hand" ? "손" : it.To == "draw" ? "뽑을 더미" : "버린 더미")}{(it.Max > 0 ? $"(덱에 {it.Max}장까지)" : "")}",
                "summon" => $"{data?.Enemy(it.Id)?.Name ?? it.Id} {Math.Max(1, it.N)} 부르기{(it.Max > 0 ? $"(최대 {it.Max})" : "")}",
                "count" => $"{it.Id} {(it.V >= 0 ? "+" : "")}{it.V}",
                "cardDebuff" => $"{(it.To == "draw" ? "뽑을 더미" : "손")} {(it.N > 0 ? it.N + "장" : "전부")}에 {it.Id}",
                "handCost" => $"손 {(it.N > 0 ? it.N + "장" : "전부")} 비용 {(it.V >= 0 ? "+" : "")}{it.V}",
                "reshuffle" => $"손패를 모두 버리고 섞기{(it.Id != null ? $" · {Q(CardName(it.Id))} {Math.Max(1, it.N)}장" : "")}",
                "autoPlay" => $"손패 {Math.Max(1, it.N)}장 저절로 나감",
                "shift" => $"수 바꾸기 — {Intent(it.Next, null, dmgx)}",
                "revive" => $"재 속 — {Math.Max(1, it.N)}턴 뒤 HP {(it.V > 0 ? it.V : 50)}%로 부활",
                "feign" => "쓰러진 척 — 적이 회복하면 일어섬",
                "thorns" => $"가시 {v}",
                "selfHeal" => $"스스로 회복 {it.V}",
                "brace" => $"강인도 회복 {it.V}",
                "seize" => $"카드 {Math.Max(1, it.N)}장 빼앗기(손 · 손이 비면 다음에 뽑을 카드) — 격파 · 처치 · 그 적 HP {(it.V > 0 ? it.V : R.SEIZE_PCT)}% 피해로 되찾음",
                _ => it.T,
            };
            if (it.Id != null && (it.T == "attack" || it.T == "back" || it.T == "attackAll")) s += $" · 파티 {it.Id} {Math.Max(1, it.N)}";
            if (it.Id != null && it.T == "multi") s += $" · 한 대마다 {it.Id} {Math.Max(1, it.Per)}";
            if (it.Tough > 0) s += $" · {(it.All ? "적 전체 " : "")}강인도 회복 {N(it.Tough)}";
            // 고학년 예고는 안의 고학년 글이 적는다 · 쏟는 수에 brk 가 있으면 그 글이 이미 적었다(두 번 찍지 않는다)
            if (it.Brk && !(it.T == "charge" && (it.Next?.T == "ult" || it.Next?.Brk == true))) s += " · 격파하면 끊김";
            if (it.If?.Allies != null) s += $" (적이 {it.If.Allies}명 이상일 때)";
            if (it.If?.PartyBlock != null) s += it.If.PartyBlock.Value ? " (파티에 방어 · 실드가 있을 때)" : " (파티에 방어 · 실드가 없을 때)";
            if (it.If?.SelfBlock != null) s += it.If.SelfBlock.Value ? " (자기에게 실드가 있을 때)" : " (자기에게 실드가 없을 때)";
            if (it.If?.Counter != null) s += $" ({it.If.Counter} {Math.Max(1, it.If.N)} 이상일 때)";
            return s;
        }

        // ── 이벤트 결과 ───────────────────────────────────────────────
        public string Outcomes(List<Outcome> outs) => outs == null || outs.Count == 0 ? "없음" : string.Join(" · ", outs.Where(x => x.K != "flag").Select(Outcome));

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
                case "shinPick": return $"축복 카드 {Math.Max(1, o.N)}{(o.Kind == "cost" ? " (비용)" : o.Kind == "power" ? " (위력)" : "")}{(o.Random ? " (고른 카드에 무작위 축복 · 신탁)" : "")}";   // 무작위는 이벤트에 「무작위」 라고 적힌 것만(2026-10-13) — 기본은 축복 · 신탁도 고른다
                case "curse": return $"골칫거리 {Q(CardName(o.Id))}";
                case "gift": return $"카드 {Q(CardName(o.Id))}";
                case "scout": return "지도 공개";
                case "shopGift": return $"다음 상점: 아티팩트 ({o.Grade})";
                case "rewardFlash": return "다음 보상: 신탁 1";
                case "next": return "다음 전투: " + Next(o.Next);
                case "mindBreak": return $"유령의 장난 {Math.Max(1, o.N)}: 다음 전투 {Math.Max(1, o.N)}번, 매 턴 {Q(data?.Card(R.MIND_CARD)?.Name ?? "망령의 속삭임")}";   // 2026-10-12 — 효과가 칩에 보이게
                case "flag": return "";   // 연속 이벤트 깃발 — 화면에 보이지 않는다
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

        /// <summary>성격 약점(적) 풀이 — 엔진 TurnStatusTick 이 내 턴 시작마다 1 줄인다.</summary>
        static string WeakTip(string nat) => $"적 · 겹 = 남은 턴 · 그동안 {nat} 사도의 카드가 이 적에게 약점 공격(피해 {Num.Round(R.WEAK_DMG * 100)}% 증가 · 강인도를 많이 깎음) · 내 턴이 시작될 때마다 −1";

        /// <summary>엔진 키워드 설명(Docs/키워드.md 뜻을 줄인 우리 말) — 칩 · 툴팁 · 밑줄 낱말 판. 「자세히」 없이 이 글 하나에 수치 · 지속이 다 든다.</summary>
        public static readonly IReadOnlyDictionary<string, string> TIPS = new Dictionary<string, string>
        {
            // 전투 규칙
            ["행동 카운트"] = "적이 행동하기까지 남은 카드 수 · 카드 1장마다 −1(신속 카드는 그대로) · 0이 되면 그 적이 바로 행동",
            ["혼란"] = "그 적의 다음 공격이 다른 적에게 감",
            ["함정"] = "그 적이 다음에 공격하면 터짐(한 번)",
            ["강인도"] = "적의 버티는 힘 · 카드 1장이 적 1명을 처음 칠 때 감소 — 약점 공격은 비용 1당 1(비용 0은 1/2), 아니면 1/3(비용 0은 0) · 적 전체 · 무작위 대상도 적마다 같음 · 추가 공격 · 협공 · 반격 · 소환물은 깎지 않음 · 고학년은 게이지 100%당 1 · 0이 되면 격파 · 저절로 차지 않음(회복 스킬이 있는 적만 되찾음)",
            ["격파"] = $"강인도가 0이 되면 AP +{R.TOUGH.Ap} · 「격파하면 끊김」 수를 끊음 · 행동은 그대로 · 다음 내 턴 시작에 강인도가 가득 참 · 처치하는 일격에 0이 되어도 AP +{R.TOUGH.Ap}(근면과 따로)",
            ["격파 상태"] = "강인도가 0이 된 적 · 행동은 그대로 함 · 다음 내 턴 시작에 강인도가 가득 차며 풀림",
            ["붕괴"] = "적을 격파하면 뒤의 효과 발동 — 카드에선 이 카드로 격파할 때, 고유 능력에선 아군 누구든(「붕괴(자신)」 은 자신만)", ["처치"] = "이 카드로 적을 쓰러뜨리면 뒤의 효과 발동", ["파괴"] = "적이 쓰러지면 뒤의 효과 발동 — 카드에선 이 카드의 대상이 쓰러져 있을 때, 고유 능력에선 아군 누구든 쓰러뜨리면(「파괴(자신)」 은 자신만)",
            ["약점 공격"] = $"약점을 치는 공격 · 피해 {Num.Round(R.WEAK_DMG * 100)}% 증가 · 강인도 피해 비용 1당 1(비용 0도 1 · 약점이 아니면 1/3) · 공명 사도의 카드는 어느 적이든 약점 공격",
            ["방어 기반 피해"] = $"카드 주인의 방어력 {Num.Round(R.DEF_DMG.def * 100)}% + 공격력 {Num.Round(R.DEF_DMG.atk * 100)}% 를 바탕으로 한 피해",
            ["고정 피해"] = "증감 · 상태 효과를 받지 않는 피해",
            ["고정 지속 피해"] = "증감 · 상태 · 실드를 모두 무시하고 HP에 바로 드는 피해", ["고정 실드"] = "증감을 받지 않는 실드",
            ["회복"] = "파티 HP를 채움 · 적힌 % = 카드 주인 방어력의 %(고유 효과 · 패시브는 그 사도의 방어력)",
            ["피해 기반 회복"] = $"이 카드가 준 피해의 적힌 %만큼 파티 HP 회복 · 한 번에 최대 HP {Pc("흡수Max")}%까지",
            ["부상"] = $"파티 HP가 최대의 {Pc("부상")}% 미만인 상태",
            ["추가 공격"] = $"카드와 따로 들어가는 공격(협공 · 표식 · 공명 …) · 사기 적용 · 소환물이 따라 치는 것은 카드 한 장에 {Battle.SUMMON_CAP}대까지",
            // 엘다인 문법(2026-10-11)
            ["파티 실드"] = "파티가 함께 쓰는 실드(방어 포함) · 「씀」 은 그만큼 실드를 깎아 그 양을 효과에 씀 — 방어부터 깎음 · 턴이 바뀌면 사라짐(실드 보존 · 유지 예외)",
            ["변신 중"] = "그 사도가 변신해 있는 동안 · 변신 카드(증발 · 소멸)가 드는 순간과 매 턴 시작에 손에 생김",
            ["은방울"] = "파티 · 실드가 깨지거나 내 턴 끝에 실드가 남아 있으면 그 겹만큼 피해 감소가 됨(한 번)",
            ["도발"] = "이번 적 차례의 적 공격이 그 사도를 노림 · 적힌 만큼 그 대에 파티 실드가 더 막음",
            ["처형"] = "그 적 HP 가 적힌 비율 이하면 바로 쓰러뜨림 · 보스 · 클론은 대신 적힌 덤 피해",
            ["낭독"] = $"「받아 적음」 은 바로 앞에 낸 다른 아군 카드를 쪽으로 남김(생성 · 상태 카드 빼고, 최대 {Battle.RECORD_MAX}쪽 — 넘치면 첫 쪽이 밀림) · 「낭독」 은 첫 쪽부터 그 카드 주인의 능력치로 적힌 %만큼 다시 발동하고 그 쪽을 지움",
            // 엘다인 4차(2026-10-11)
            ["게이지 정지"] = "이 전투 동안 그 사도의 카드로 고학년 게이지가 오르지 않음(낸 AP · 게이지 효과 둘 다)",
            ["아는 수"] = "적의 지금 수(공격 · 방어 · 강화 · 방해)가 「만년설의 앎」 에 적힌 종류면",
            ["태세"] = "카드로 들어가는 사도의 상태 — 그동안 그 사도 카드의 「「태세 이름」: …」 덤이 붙고, 「… 중에만 사용 가능」 카드를 낼 수 있음 · 적힌 일이 생기면 풀림(「n턴간 보존」 이면 그동안 안 풀림)",
            ["쥐고 넘기면"] = "턴 끝에 손에 있으면(보존 카드를 내지 않고 턴을 넘기면) 뒤의 효과 발동",
            ["이어 씀"] = "고른 만큼 쓰고 치는 동안 처치 등으로 그 고유 효과를 새로 얻으면 1씩 더 써서 한 대씩 더 침(남겨 둔 몫은 쓰지 않음 · 적힌 최대까지)",
            // 카드 태그
            ["보존"] = "턴이 끝나도 손에 남음", ["소멸"] = "내면 이번 전투에서 빠짐 · 소멸 N = N번 내면 빠짐", ["증발"] = "턴이 끝날 때 손에 있으면 소멸",
            ["회수"] = "내면 버린 더미 대신 손으로 돌아옴 · 회수 N = N번까지", ["망각"] = "소멸하는 대신 뽑을 더미 맨 위로", ["제거"] = "내면 이번 전투와 덱에서 완전히 빠짐",
            ["개전"] = "전투 시작 시 첫 손패에 포함", ["개막"] = "전투 시작 시 AP를 치르고 저절로 나감(AP가 모자라면 안 나감)",
            ["종극"] = "내면 바로 턴 종료", ["신속"] = "내도 적의 행동 카운트가 줄지 않음", ["주도"] = "턴 시작에 50% 확률로 비용 −1 · 다른 카드를 먼저 내면 사라짐",
            ["연결"] = "직접 내면 손의 다른 연결 카드를 모두 버림", ["봉인"] = "처음 내면 효과 없이 봉인만 풀림", ["사용 불가"] = "낼 수 없음",
            ["연계"] = "다른 사도의 카드가 나가면 발동 — 카드 태그면 손에서 저절로(AP 없이) 나가고, 고유 능력이면 뒤의 효과(괄호는 카드 종류)", ["천상"] = "비용 2 이상인 카드가 나가면 발동 — 카드 태그면 손에서 저절로 나가고, 고유 능력이면 뒤의 효과",
            ["연속"] = "바로 앞 카드와 성격이 같으면 뒤의 효과 발동(고유 능력은 그런 카드를 낼 때마다)", ["조율"] = "이 카드의 비용 = 남은 AP 이면 뒤의 효과 발동", ["안식"] = "효과로 버려지면 뒤의 효과 발동 — 고유 능력은 아군의 카드가 버려질 때마다(「안식(자신)」 은 자신의 카드만)",
            ["감응"] = "드로우하면 뒤의 효과 발동(턴 시작 드로우 · 뽑을 더미에서 손으로 포함, 생성 · 버린 더미에서 손으로는 아님) — 고유 능력은 아군의 카드가 뽑힐 때마다(괄호는 자신 · 카드 종류)", ["영감"] = "카드 효과로 드로우하면 뒤의 효과 발동", ["열정"] = "열정 카드가 나가면 손에 있는 이 카드의 「열정:」 효과 발동",
            ["소각"] = "소멸할 때 뒤의 효과 발동 — 고유 능력은 아군의 카드가 소멸할 때마다(「소각(자신)」 은 자신의 카드만)", ["연쇄"] = "다음 턴 시작에 같은 효과가 한 번 더(덤 없이)",
            ["축복"] = $"실드가 최대 HP {Pc("축복At")}% 미만일 때 손에서 버려지면 고정 실드(카드 주인 방어력 {Pc("축복")}%)",
            ["유일"] = "덱에 한 장만(복제 안 됨)", ["금기"] = "신탁 · 복제 · 제거 불가", ["봉인된 금기"] = "금기 · 보스를 처치하면 금기 카드로 바뀜",
            // 복제본(보스 뒤 복제 · 이벤트 복제 — Run.AddCopy, 2026-10-06 사용자)
            ["복제본"] = "복제한 순간의 신탁 · 축복 그대로 · 그 뒤로 따로 신탁 · 축복을 받음(원본이 나중에 받는 것은 따라오지 않음)",
            // 빛나는 카드(전투마다 run.RollEpiphany) — 한 전투에 사도마다 신탁 · 은총 가운데 하나만(2026-10-06 사용자)
            ["신탁"] = "전투 중 빛나는 고유 · 교주 카드를 내면 신탁 셋 가운데 하나를 얹음(이번에는 비용 0) · 내지 않으면 전투가 끝난 뒤 고름 · 한 전투에 사도마다 신탁 · 은총 가운데 하나만(교주 카드는 주인 사도 몫)",
            ["은총"] = "전투 중 빛나는 시작 카드를 내면 그 사도의 고유 카드를 손에 얻음(그 턴 비용 0) · 내지 않으면 전투가 끝난 뒤 받음 · 한 전투에 사도마다 신탁 · 은총 가운데 하나만",
            ["분쇄"] = $"실드가 있는 적을 치면 피해 {Pc("분쇄")}% 증가", ["탄환"] = $"사용하면 소멸. 버린 더미로 가면(턴 끝 포함) 무작위 적에게 추가 공격(공격력 {Pc("탄환")}%)",
            ["결속"] = "같은 사도의 결속 카드가 한 장으로 겹침(최대 5) · 3 이상이면 강해진 카드로 · 내면 처음으로",
            // 이로운 효과 — 「파티」 = 파티 하나에 걸려 셋 모두에게 든다
            ["사기"] = $"파티 · 사기 1당 카드 피해량 +{Pc("사기")}%(합연산 — 카드 계수에 더함, 최대 {Mx("사기")}) · 전투 끝까지",
            ["불굴"] = $"파티 · 불굴 1당 받는 피해 −{Pc("불굴")}%p(합쳐 최대 −{Pc("불굴Cap")}%) · 전투 끝까지",
            ["결의"] = $"파티 · 결의 1당 얻는 실드 +{R.SV("결의")}(고정값, 최대 {Mx("결의")}) · 전투 끝까지",
            ["결정화"] = $"파티 · 내 턴이 끝날 때 결정화 1당 고정 실드(방어력 {Pc("결정화")}%, 최대 {Mx("결정화")}) · 줄지 않음",
            ["고동"] = $"파티 · 내 턴이 끝날 때 고동 1당 적 전체에 고정 피해 {Pc("고동")}%(최대 {Mx("고동")}) · 줄지 않음",
            ["협공"] = $"파티 · 아군이 공격 카드를 내면 협공을 건 사도가 그 적에게 추가 공격(공격력 {Pc("협공")}%) · 쓸 때마다 −1",
            ["반격"] = $"파티 · 적에게 맞으면 반격을 건 사도가 그 적에게 방어 기반 피해 {Pc("반격")}%(실드로 다 막으면 {Pc("반격Full")}%, 치명 적용) · 쓸 때마다 −1 · 최대 {Mx("반격")}",
            ["피해 감소"] = $"파티 · 받는 피해 −{Pc("피해 감소")}% · 맞을 때마다 −1", ["면역"] = "파티 · 디버프 1개를 막음 · 막을 때마다 −1",
            ["실드 유지"] = $"파티 · 턴이 바뀔 때 실드의 {Pc("실드 유지")}%가 남음 · 쓸 때마다 −1",
            ["실드 보존"] = "파티 · 턴이 바뀔 때 실드가 그대로 남음 · 쓸 때마다 −1", ["저장"] = "파티 · 남은 AP를 다음 턴으로 넘김 · 쓸 때마다 −1",
            ["잔광"] = $"파티 · 공격 카드의 강인도 피해 +{R.TOUGH.Glow} · 격파 상태인 적이면 피해 {Pc("잔광")}% 증가 · 공격 카드 1장마다 −1",
            ["공명"] = $"파티 · 카드를 버리면 버린 사도가 추가 공격(공격력 {Pc("공명")}%) · 버린 카드 1장마다 −1",
            ["탄성"] = $"파티 · 아군이 추가 공격하면 탄성 1당 HP 회복(방어력 {Pc("탄성")}%, 최대 {Mx("탄성")}) · 한 번 들면 모두 사라짐",
            ["칼날 벼리기"] = $"파티 · 1턴 · 칼날 벼리기 1당 공격 카드의 방어 기반 피해 {Pc("칼날 벼리기")}% 증가(최대 {Mx("칼날 벼리기")})",
            ["빙벽"] = "파티 · 1턴 · 맞을 때마다 반격(반격 수를 쓰지 않음)", ["구속"] = "그 사도 · 자기 카드 말고는 AP를 얻거나 잃지 않음 · 턴마다 −1",
            ["형상 강화"] = $"파티 · 1턴 · 형상 강화 1당 만든 카드의 피해 · 실드 · 회복 +{Pc("형상 강화")}%(최대 {Mx("형상 강화")})",
            ["행동 둔화"] = $"파티 · 신속 카드를 내면 1턴간 적 전체 주는 피해 {Pc("행동 둔화")}% 감소 · 쓸 때마다 −1",
            ["급속"] = $"적 · 행동 카운트 −N(최대 {Mx("급속")}) · 그 적이 행동하면 사라짐",
            ["근면"] = "파티 · 적을 처치하면 AP +1 · 드로우 1 — 카드 하나에 한 번, 한 턴에 근면 수만큼 · 줄지 않음",
            ["계몽"] = "파티 · 고학년을 쓰면 드로우 1 · 줄지 않음",
            ["집중"] = $"파티 · 턴 시작에 파티 HP가 {Pc("집중")}% 미만이면 AP +1 · 줄지 않음", ["회피"] = "파티 · 적의 공격 한 대에 HP를 잃지 않음 · 한 대마다 −1",
            ["다음 턴 드로우"] = "파티 · 다음 턴 시작에 수만큼 더 드로우 후 사라짐",
            ["초재생"] = $"파티 · 턴 시작에 HP 회복(방어력 {Pc("초재생")}%) · 턴마다 −1", ["탐구심"] = $"이 카드 · 탐구심 1당 피해 {Pc("탐구심")}% 증가",
            ["절대 무적"] = "파티 · 1턴 · HP를 잃지 않음(치른 HP는 예외)", ["끈기"] = "파티 · 쓰러질 피해를 받으면 HP 1로 버팀 · 버틸 때마다 −1",
            // 해로운 효과 — 걸린 쪽(파티 · 적)
            ["취약"] = $"받는 피해 ×{1 + R.SV("취약"):0.##}(다른 증감과 따로 곱함) · 맞을 때마다 −1", ["약화"] = $"주는 피해 {Pc("약화")}% 감소 · 칠 때마다 −1 · 파티에 걸리면 모든 사도의 카드 피해가 줄고 카드 1장마다 −1",
            ["손상"] = $"얻는 실드 −{Pc("손상")}% · 얻을 때마다 −1",
            ["고통"] = $"턴이 끝날 때 고통 1당 고정 지속 피해 {Pc("고통")}%(건 쪽 공격력, 최대 {Mx("고통")}) · 그 뒤 절반으로", ["고통 각인"] = $"고통을 얻을 때마다 +{R.SV("고통 각인")} 더",
            ["균열"] = $"턴이 끝날 때 균열 1당 지속 피해 {Pc("균열")}%(최대 {Mx("균열")}) · 그 뒤 절반으로",
            ["그을림"] = $"행동 카운트가 1 줄 때마다 지속 피해 {Pc("그을림")}%(최대 {Mx("그을림")}) · 들 때마다 −1 · 턴이 끝나면 사라짐",
            ["표식"] = $"공격 카드에 처음 맞으면 추가 공격(공격력 {Pc("표식")}%) + 강인도 피해 · 맞을 때마다 −1",
            ["잔불"] = $"격파 상태에서 맞거나 쓰러질 공격을 맞으면 잔불 1당 그 피해 {Pc("잔불")}% 증가(최대 {Mx("잔불")}) · 한 번 들면 사라짐",
            ["충격"] = $"공격 카드의 대상이 되면 고정 피해 {Pc("충격")}%(실드가 있으면 +{Pc("충격Shield")}%) · 들 때마다 −1",
            ["충격파"] = $"카드로 공격받으면 다른 모두에게 고정 피해 {Pc("충격파")}% · 들 때마다 −1",
            ["응징"] = $"실드를 얻으면 그쪽 모두에게 고정 피해 {Pc("응징")}%(건 쪽 공격력) · 들 때마다 −1",
            ["포자증식"] = $"포자증식 1당 받는 피해 +{Pc("포자증식")}% · 한 번 들면 모두 사라짐",
            ["죽음의 낙인"] = $"낙인을 건 그 카드로 치면 피해 {Pc("죽음의 낙인")}% 증가", ["열정 약점"] = "1턴 · 열정 카드가 약점 공격으로 들어감",
            ["순수 약점"] = WeakTip("순수"), ["광기 약점"] = WeakTip("광기"), ["냉정 약점"] = WeakTip("냉정"),
            ["활발 약점"] = WeakTip("활발"), ["우울 약점"] = WeakTip("우울"), ["공명 약점"] = WeakTip("공명"),
            ["약점 노출"] = $"적 · 겹 = 남은 턴 · 그동안 모든 아군 카드가 이 적에게 약점 공격(피해 {Num.Round(R.WEAK_DMG * 100)}% 증가 · 강인도를 많이 깎음) · 내 턴이 시작될 때마다 −1",
            ["정신 붕괴"] = "고학년 · 신탁 사용 불가 · 턴마다 −1 · 남은 채 전투가 끝나면 다음 전투에 「유령의 장난」(매 턴 손에 「망령의 속삭임」)",
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
            foreach (var h in data.Heroes.Values) foreach (var f in h.Forms ?? new List<FormDef>()) if (f.Name != null && !o.ContainsKey(f.Name)) o[f.Name] = $"{h.Name}의 {(f.Stance ? "태세" : "변신")} — {Form(f)}";
            foreach (var e in data.Enemies.Values) foreach (var c in e.Counters) if (c.Name != null && !o.ContainsKey(c.Name)) o[c.Name] = Counter(c);
            // 엘다인 4차 — 「정각」(ifNth after id) · 「신성」(critDmg id) 같은 데이터 이름
            foreach (var cd in data.Cards.Values)
                foreach (var f in cd.Fx.Concat(cd.Oracles.SelectMany(or => or.Fx)))
                {
                    if (f.K == FxK.IfNth && f.After && f.Id != null && !o.ContainsKey(f.Id)) o[f.Id] = $"이번 턴 {f.N}번째 카드부터(앞서 낸 카드가 {f.N - 1}장 이상)";
                    if (f.K == FxK.IfCost && f.Id != null && !o.ContainsKey(f.Id)) o[f.Id] = f.Max == 0 ? "이 카드의 비용이 0 이 되어 낼 때" : $"이 카드를 비용 {f.Max} 이하로 낼 때";
                    if (f.K == FxK.CritDmg && f.Id != null && !o.ContainsKey(f.Id)) o[f.Id] = $"파티 · {f.TurnsOr1}턴 동안 아군 카드 · 반격의 치명 피해 +{P(f.V)}{(f.Off != null && f.Off.Count > 0 ? $" · 끝나면 {Fx(f.Off)}" : "")}";
                }
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
                        case FxK.When: Add(f.On == "draw" ? "영감" : f.On == "drawAny" ? "감응" : f.On == "discard" ? "안식" : f.On == "burn" ? "소각" : f.On == "passion" ? "열정" : f.On == "handEnd" && c.Hero != null ? "쥐고 넘기면" : null); break;
                        case FxK.IfNth: if (f.After && f.Id != null) Add(f.Id); break;
                        case FxK.IfCost: Add(f.Id); break;
                        case FxK.SwiftTurn: Add("신속"); break;
                        case FxK.GaugeLock: Add("게이지 정지"); break;
                        case FxK.CritDmg: Add(f.Id); Walk(f.Off); break;
                        case FxK.IfKill: Add("처치"); break;
                        case FxK.IfBreak: Add("붕괴"); break;
                        case FxK.IfBroken: Add("파괴"); break;
                        case FxK.IfTune: Add("조율"); break;
                        case FxK.IfChain: Add("연속"); break;
                        case FxK.IfWounded: Add("부상"); break;
                        case FxK.IfFoe: if (f.Id == "broken") Add("격파 상태"); break;
                        case FxK.Drain: Add("피해 기반 회복"); break;
                        case FxK.Extra: case FxK.Summon: Add("추가 공격"); if (f.K == FxK.Summon) Add(f.Id); break;
                        case FxK.Ripen: Add(f.Id); break;
                        case FxK.Status: case FxK.CardStatus: Add(f.Id); break;
                        case FxK.Dmg: if (f.Base == "def") Add("방어 기반 피해"); if (f.Dot) Add("고정 지속 피해"); else if (f.Fixed) Add("고정 피해"); break;
                        case FxK.Shield: if (f.Fixed) Add("고정 실드"); break;
                        case FxK.Stack: case FxK.Spend: case FxK.IfStack: case FxK.PerStack: Add(f.Id); break;
                        case FxK.Form: Add(data?.Form(f.Id)?.Name); break;
                        case FxK.IfForm: case FxK.FormKeep: { var sf = f.K == FxK.IfForm ? StanceOf(f.Id) : StanceOf(null); if (sf != null) { Add(sf.Name); Add("태세"); } break; }
                        case FxK.Power: foreach (var r in f.Rules ?? new List<PassiveRule>()) { bool m0 = mine; mine = true; try { var tn = TrigName(r.When); if (tn != null) Add(tn.Split('(', ':')[0]); } finally { mine = m0; } Walk(r.Fx); } break;
                    }
                    Walk(f.Then);
                }
            }
            var h0 = formHero; formHero = c.Hero;
            try { Walk(c.Fx); } finally { formHero = h0; }
            if (c.NeedForm != null && c.NeedForm != "*") Add(data?.Form(c.NeedForm)?.Name);
            if (c.Def.PayWith != null) Add(c.Def.PayWith);
            return o;
        }

        // ── 적 · 쌓이는 수치 ───────────────────────────────────────────
        /// <summary>쌓이는 수치 한 줄 — 「분노: 1개당 주는 피해 +10%. 공격이 아닌 카드를 낼 때마다 +1. 4가 되면 즉시: 공격 300.」</summary>
        public string Counter(CounterDef c)
        {
            var s = new List<string>();
            if (!string.IsNullOrEmpty(c.Desc)) s.Add(c.Desc.TrimEnd('.'));
            if (c.Dealt != 0) s.Add($"1개당 주는 피해 {P(Math.Abs(c.Dealt))} {(c.Dealt > 0 ? "증가" : "감소")}");
            if (c.Taken != 0) s.Add($"1개당 받는 피해 {(c.Taken > 0 ? "+" : "")}{P(c.Taken)}");
            if (c.Flat > 0) s.Add($"있으면 받는 피해가 {c.Flat}");
            if (c.OnHit != 0) s.Add($"맞을 때마다 {(c.OnHit > 0 ? "+" : "")}{c.OnHit}");
            if (c.OnCard != 0)
            {
                string ty = c.CardType == null ? "" : c.CardType.StartsWith("!", StringComparison.Ordinal) ? c.CardType.Substring(1) + "이 아닌 " : c.CardType + " ";
                s.Add($"{(c.AfterAct ? "행동한 뒤 " : "")}{ty}카드를 낼 때마다 {(c.OnCard > 0 ? "+" : "")}{c.OnCard}");
            }
            if (c.ResetTurnStart) s.Add($"턴 시작에 {c.Start}{(Ko.HasFinal(c.Start.ToString()) && c.Start % 10 != 1 && c.Start % 10 != 7 && c.Start % 10 != 8 ? "으로" : "로")}");
            if (c.OnTurnStart != 0) s.Add($"턴 시작에 {(c.OnTurnStart > 0 ? "+" : "")}{c.OnTurnStart}");
            if (c.ClearTurnEnd) s.Add("턴이 끝나면 사라짐");
            if (c.OnTurnEnd != 0) s.Add($"턴이 끝나면 {(c.OnTurnEnd > 0 ? "+" : "")}{c.OnTurnEnd}");
            if (c.StunAtZero) s.Add("0 이 되면 기절");
            if (c.ClearOnAttack) s.Add("공격하면 모두 사라짐");
            if (c.BreakCut > 0 || c.OnBreak != 0)
                s.Add("격파되면 " + string.Join(" · ", new[] { c.BreakCut > 0 ? (Math.Abs(c.BreakCut - 0.5) < 1e-9 ? "절반으로" : $"{P(c.BreakCut)} 줄어듦") : null, c.OnBreak != 0 ? $"{(c.OnBreak > 0 ? "+" : "")}{c.OnBreak}" : null }.Where(x => x != null)));
            if (c.BreakHold) s.Add("격파된 동안 오르지 않음");
            if (c.At > 0 && c.Act != null) s.Add($"{c.At}{(Ko.HasFinal(c.At.ToString()) ? "이" : "가")} 되면 {(c.Mode == "next" ? "다음 차례에" : c.Mode == "replace" ? "수를 바꿔" : "즉시")}: {Intent(c.Act)}");
            if (c.Max < 99) s.Add($"최대 {c.Max}");
            return $"{c.Name}: {string.Join(". ", s)}.";
        }

        /// <summary>적 정보 창의 덧붙임 — 쌓이는 수치 · 희귀종 · 영혼 공유 · 소환 연결 · 패시브(여러 줄).</summary>
        public string Enemy(EnemyDef e)
        {
            var l = new List<string>();
            foreach (var c in e.Counters) l.Add(Counter(c));
            if (R.FOE_RARE) foreach (var r in e.Rare) l.Add($"희귀종 — {(R.RARES.TryGetValue(r.Id ?? "", out var k) ? k : r.Id)}{(r.Card != null ? $" ({Q(CardName(r.Card))})" : "")}{(r.Id == "actDebuff" ? $" ({r.St ?? "취약"})" : "")}");
            if (e.Soul) l.Add("영혼 공유 — 맨 앞이 아니면 피해를 받지 않음");
            if (e.Tied) l.Add("세운 이가 쓰러지면 같이 쓰러짐");
            if (e.ToughTaken > 0 && e.ToughTaken != 1) l.Add($"받는 강인도 피해 ×{N(e.ToughTaken)}");
            // 같은 계기(계기 · 문턱 · 종류 · 대상 동료)의 패시브는 한 줄로 — 「결 깨짐 · 드러난 핵: 격파되면 수호 결 -5 · 자신에게 취약 +3」
            foreach (var g in e.Passives.GroupBy(p => (p.On, p.At, p.Type, p.Every, p.Same, p.Who)))
            {
                var ps = g.ToList();
                l.Add($"{string.Join(" · ", ps.Select(p => p.Name))}: {FoeOn(ps[0], data)} {string.Join(" · ", ps.Select(p => Intent(p.Do)))}");
            }
            return string.Join("\n", l);
        }

        /// <summary>적 패시브 하나의 글 — 계기 + 결과 + 횟수(「HP가 30% 이하가 되면 스스로 회복 300」 · 「맞으면 … (턴에 한 번)」). 정보 창 패시브 칸.</summary>
        public string FoePassive(EnemyPassive p)
        {
            if (p == null) return "";
            string s = $"{FoeOn(p, data)} {(p.Do != null ? Intent(p.Do) : "")}".Trim();
            // 횟수 — 엔진(BattleFoes.FoePassives)은 limit(없으면 1)을 턴마다 되돌린다(전투 시작 · HP 문턱은 전투에 한 번). 한 턴에 여러 번 올 수 있는 계기만 적는다
            bool many = p.On == "card" || p.On == "hurt" || p.On == "allyDown" || p.On == "allyBroken" || p.On == "debuffed" || p.On == "guardBreak";
            int lim = p.Limit ?? 1;
            if (many && lim > 0) s += lim == 1 ? " (턴에 한 번)" : $" (턴에 {lim}번)";
            return s;
        }

        static string FoeOn(EnemyPassive p, GameData d = null)
        {
            string who = p.Who == null ? "동료" : (d?.Enemy(p.Who)?.Name ?? p.Who);
            string ty = p.Type == null ? "" : p.Type.StartsWith("!", StringComparison.Ordinal) ? p.Type.Substring(1) + "이 아닌 " : p.Type + " ";
            switch (p.On)
            {
                case "fightStart": return "전투 시작 시";
                case "turnStart": return "턴 시작 시";
                case "turnEnd": return "턴이 끝나면";
                case "hurt": return "맞으면";
                case "lowHp": return $"HP가 {Num.Round(p.At * 100)}% 이하가 되면";
                case "allyDown": return $"{who}{(Ko.HasFinal(who) ? "이" : "가")} 쓰러지면";
                case "allyBroken": return $"{who}{(Ko.HasFinal(who) ? "이" : "가")} 격파되면";
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
