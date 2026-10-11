using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 콘텐츠 검사 — 잘못 쓴 데이터를 전투에 들이기 전에 잡는다. 읽히지 않는 것이 조용히 안 도는 일이 없게.
    /// Errors 는 고쳐야 하는 것(없는 id · 모르는 효과 · 빠진 값), Warnings 는 규칙(docs/18)에서 벗어난 것.
    /// </summary>
    public sealed class Validator
    {
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();
        readonly GameData d;
        readonly HashSet<string> keywords;

        static readonly HashSet<string> TARGETS = new() { "oneEnemy", "allEnemies", "randomEnemy", "topEnemy", "lowEnemy", "self", "oneAlly", "allAllies", "otherAllies", "party", "auto", "otherEnemy", "nextEnemy", "slowestEnemy", "markedEnemy", "strongestAlly", "farEnemy", "bigHitEnemy", "holder" };
        static readonly HashSet<string> ALLY_TARGETS = new() { "self", "oneAlly", "allAllies", "otherAllies" };
        static readonly HashSet<string> WHEN_ON = new() { "fightStart", "turnStart", "turnEnd", "play", "guard", "break", "kill", "hurt", "lowHp", "rush", "ult", "debuff", "overheal", "stackReach", "stackGone", "stackOver", "reserveGone", "switch", "rhythm", "always",
            "discard", "pay", "exhaust", "huntDown", "blocked", "spend", "link", "crit", "make",
            "drawn", "shuffle", "extra", "hit", "shieldBreak", "foeShieldBreak", "foeGuard", "foeAct", "foeActBefore", "endure", "unwound",
            "tally", "keepAp", "reserveFire", "grow", "fightEnd", "guardSum", "summonAct", "pulled", "formOn", "counter", "cutHit", "formLap" };
        static readonly HashSet<string> CONDS = new() { "stack", "hp", "hpMin", "status", "foes", "foesMax", "playedMin", "playedMax", "ownNone", "apLeft", "gauge", "guarded", "rushed", "hurtLast", "killedLast", "firstTurn", "targetBroken",
            "repeat", "held", "spent", "balanced", "debuffs", "paid", "wounded", "onlyMe", "ally", "inDebt", "heldCards", "idleLast", "typeNew", "goneMin", "pricier", "gained" };
        /// <summary>사도 갈래(style) — 18갈래 v2(2026-10-08). 같은 갈래끼리의 통계 · 반응이 본다.</summary>
        public static readonly HashSet<string> STYLES = new() { "쌓고 고르기", "박자형", "아껴 두기형", "버리기형", "계산형", "대가형", "예약형", "손 만들기형", "표적형",
            "거드는 형", "두 얼굴형", "버티형", "격파형", "소멸형", "성장형", "돈형", "소환형", "회복형" };
        static readonly HashSet<string> INTENTS = new() { "attack", "back", "attackAll", "multi", "charge", "block", "guard", "heal", "buff", "debuff", "jam", "addCard", "summon",
            "count", "cardDebuff", "handCost", "reshuffle", "autoPlay", "shift", "brace", "seize" };
        static readonly HashSet<string> FOE_ON = new() { "fightStart", "turnStart", "turnEnd", "hurt", "lowHp", "allyDown", "card", "rushed", "debuffed", "broken", "recover",
            "death", "guardBreak", "act", "afterDraw", "allyBroken" };
        static readonly HashSet<string> OUT_K = new() { "none", "gold", "hp", "maxHp", "remove", "dupe", "unique", "neutral", "equip", "flash", "shin", "noShin", "shinNow", "shinPick", "curse", "gift", "scout", "shopGift", "rewardFlash", "next", "mindBreak", "flag" };
        static readonly HashSet<string> BLESS_KIND = new() { "power", "cost", "weakSpot", "frost", "ap", "draw", "heal", "guard", "atkUp", "defUp" };
        static readonly HashSet<string> PER_STAT = new() { "dealt", "taken", "atk", "def", "crit", "guard", "dot", "hot", "tough", "counter", "ultCost" };
        static readonly HashSet<string> CARRIERS = new() { "self", "enemy", "ally", "hero" };
        static readonly HashSet<string> PILES = new() { "draw", "discard", "gone", "hand", "discarded", "token", "broken", "drawn", "discards", "morale" };

        Validator(GameData d)
        {
            this.d = d;
            keywords = new HashSet<string>(d.Heroes.Values.SelectMany(h => h.AllKeywords).Select(k => k.Name).Where(n => n != null));
        }

        public static Validator Check(GameData d)
        {
            var v = new Validator(d);
            v.Run();
            return v;
        }

        public bool Ok => Errors.Count == 0;
        public override string ToString() => string.Join("\n", Errors.Select(e => "오류 " + e).Concat(Warnings.Select(w => "주의 " + w)));

        void E(string s) => Errors.Add(s);
        void W(string s) => Warnings.Add(s);

        void Run()
        {
            foreach (var g in d.Heroes.Values.SelectMany(h => h.AllKeywords.Select(k => (h, k))).Where(x => x.k.Name != null).GroupBy(x => x.k.Name).Where(g => g.Count() > 1))
                E($"고유 효과 「{g.Key}」 를 여럿이 쓴다({string.Join(" · ", g.Select(x => x.h.Id))}) — 고유 효과 이름은 사도 사이에서 겹치면 안 된다");
            foreach (var g in d.Heroes.Values.SelectMany(h => (h.Forms ?? new List<FormDef>()).Select(f => (h, f))).Where(x => x.f.Id != null).GroupBy(x => x.f.Id).Where(g => g.Count() > 1))
                E($"변신 id 「{g.Key}」 가 겹친다({string.Join(" · ", g.Select(x => x.h.Id))}) — 변신 id 는 사도 사이에서 겹치면 안 된다");
            foreach (var h in d.Heroes.Values) Hero(h);
            Eldain();
            foreach (var h in d.CloneHeroes.Values) CloneHero(h);
            foreach (var c in d.Cards.Values) Card(c);
            foreach (var e in d.Enemies.Values) Enemy(e);
            foreach (var v in d.Villages.Values) Village(v);
            var ids = new HashSet<string>();
            foreach (var e in d.Events) { if (!ids.Add(e.Id)) E($"이벤트 {e.Id}: id 가 겹친다"); Event(e); }
            Flags();
            foreach (var e in d.Equips.Values) Equip(e);
        }

        // ── 효과 조각 ──────────────────────────────────────────────────
        void FxList(string at, List<Fx> fx)
        {
            if (fx == null) { E($"{at}: fx 가 없다"); return; }
            int effects = 0;
            for (int i = 0; i < fx.Count; i++)
            {
                var f = fx[i];
                string w = $"{at} fx[{i}] {f.K}";
                if (f.K == null || !FxK.All.Contains(f.K)) { E($"{w}: 모르는 효과 조각"); continue; }
                if (f.OfStack != null && (!keywords.Contains(f.OfStack) || !(f.OfEvent > 0) || (f.K != FxK.Dmg && f.K != FxK.Shield))) E($"{w}: ofStack 은 dmg · shield 에, 사도 키워드 이름 + ofEvent 와 같이 — 「{f.OfStack}」");
                if (f.Target != null && !TARGETS.Contains(f.Target) && !(f.Target.StartsWith("hero:") && d.Hero(f.Target.Substring(5)) != null)) E($"{w}: 모르는 대상 {f.Target}");
                if (f.K == FxK.Empower) { if (f.Who != null && f.Who != "self" && f.Who != "any" && f.Who != "other") E($"{w}: empower 의 who 는 self(그 사도의 다음 카드 · 기본) · any(파티의 다음 카드) · other(다른 아군의 다음 카드)"); if (f.MinCost > 0 && f.Who != "other") E($"{w}: empower 의 minCost 는 who other 와 같이"); }
                else if (f.Who == "any" && (f.K == FxK.IfStack || f.K == FxK.PerStack || f.K == FxK.Spend)) { if (f.Id == null || d.CarrierOf(f.Id) != "hero") E($"{w}: who any(동료 누구에게든)는 아군 표시(carrier hero)에"); }
                else if (f.Who != null && f.Who != "self" && f.Who != "other" && d.Hero(f.Who) == null) E($"{w}: who 는 self · other · 사도 id(ifStack · perStack · spend 의 아군 표시는 any)");
                if (f.Owner != null && f.K != FxK.Learn && (f.K != FxK.Make || (f.Owner != "self" && f.Owner != "other" && f.Owner != "strongestAlly" && d.Hero(f.Owner) == null))) E($"{w}: owner 는 make 에만 — self · other · strongestAlly · 사도 id");
                if (f.Pick && f.K != FxK.SpendShield && f.K != FxK.Stack && !(f.K == FxK.CardStatus && f.To == "hand") && (f.K != FxK.Spend || f.All || (f.Id != null && keywords.Contains(f.Id) && d.CarrierOf(f.Id) != "self"))) E($"{w}: pick(소모량 고르기)은 spend 에만 · all 과 같이 쓰지 않는다 · 자기 주머니(carrier self) 고유 효과만");
                if (f.Else != null) FxList(w + " else", f.Else);
                // 엘다인 문법(2026-10-11, 데이터.md §22)
                if (f.OfShield && (!(f.OfEvent > 0) || (f.K != FxK.Dmg && f.K != FxK.Shield))) E($"{w}: ofShield 는 dmg · shield 에 ofEvent 와 같이");
                if (f.HitFx != null) { if (f.K != FxK.Dmg) E($"{w}: hitFx 는 dmg 에만"); else FxList(w + " hitFx", f.HitFx); }
                if ((f.HitsPer != null || f.HitsAdd != 0) && (f.K != FxK.Dmg || f.HitsPer == null || !keywords.Contains(f.HitsPer))) E($"{w}: hitsPer(사도 고유 효과 이름) · hitsAdd 는 dmg 에 — 「{f.HitsPer}」");
                if (f.Carry && f.K != FxK.Stack) E($"{w}: carry 는 stack 에만");
                if (f.Set != null && f.K != FxK.Stack && f.K != FxK.CardStatus) E($"{w}: set 은 stack · cardStatus 에만");
                if (f.Half && f.K != FxK.SpendShield && f.K != FxK.CardStatus) E($"{w}: half 는 spendShield · cardStatus 에만");
                if (f.Loop && !(f.K == FxK.Spend && f.Pick)) E($"{w}: loop 는 spend pick 에만");
                if (f.Turn && f.K != FxK.PerGone) E($"{w}: turn 은 perGone 에만");
                // 엘다인 3차(2026-10-11, 데이터.md §23)
                if (f.Choose != null && (f.Choose != "priciest" || (f.K != FxK.CostMod && f.K != FxK.CardStatus))) E($"{w}: choose 는 costMod · cardStatus 에 priciest(손의 가장 비싼 카드)");
                if (f.Keep && f.K != FxK.CardStatus && f.K != FxK.Replay) E($"{w}: keep 은 cardStatus(붙은 동안 보존) · replay(쪽을 지우지 않음)에만");
                if (f.K == FxK.CounterHits && f.V < 2) E($"{w}: counterHits 의 v 는 2 이상(나눠 칠 대 수)");
                if (f.K == FxK.CounterMod && f.V == 0) E($"{w}: counterMod 에 v(반격 피해 ±비율)가 없다");
                if (f.K == FxK.Replay && f.Ratio < 0) E($"{w}: replay 의 ratio 는 0 이상(없으면 100%)");
                if (f.K == FxK.IfEvent && f.N <= 0) E($"{w}: ifEvent 에 n 이 없다(일의 값 n 이상)");
                // 원작 검토(E63~E75)
                // 엘다인 4차(E76~E87 · 데이터 담당 요청)
                if (f.OfForm != 0 && (f.K != FxK.Dmg || f.OfForm < 0)) E($"{w}: ofForm 은 dmg 에 양수(이번 변신 중 준 피해 × 비율)");
                if (f.After && f.K != FxK.IfNth) E($"{w}: after 는 ifNth 에만");
                if (f.KillMore && f.K != FxK.Dmg) E($"{w}: killMore 는 dmg 에만");
                if (f.Off != null && f.K != FxK.CritDmg) E($"{w}: off 는 critDmg 에만");
                if ((f.Now || f.FoeStack != null) && f.K != FxK.CritNext) E($"{w}: now · foeStack 은 critNext 에만");
                if (f.FoeStack != null && !f.Now) E($"{w}: critNext 의 foeStack 은 now 와 같이(이 카드가 친 그 표식 적만 치명)");
                if (f.FoeStack != null && !keywords.Contains(f.FoeStack) && !R.ALL_ST.Contains(f.FoeStack)) E($"{w}: foeStack 은 적 표식 키워드 · 상태 이름 — 「{f.FoeStack}」");
                if (f.MinCost < 0) E($"{w}: minCost 는 0 이상");
                if (f.Target == "holder" && (f.Id == null || d.CarrierOf(f.Id) != "hero")) E($"{w}: 대상 holder 는 아군 표시(carrier hero) 조각에(id 의 그 표시가 붙은 동료)");
                if (f.KnownHits != 0 && (f.K != FxK.Dmg || f.KnownHits < 2)) E($"{w}: knownHits 는 dmg 에 2 이상");
                if (f.Once != null && (f.Once != "foeTurn" || !(f.K == FxK.Dmg && f.KnownHits > 1 || f.K == FxK.IfFoe && f.Id == "known"))) E($"{w}: once 는 ifFoe known · knownHits 에 foeTurn");
                if (f.HitsBy != null && (f.K != FxK.IfFoe || f.HitsBy != "self" || f.N <= 0)) E($"{w}: hitsBy 는 ifFoe 에 self · n(그 적을 친 횟수 n 이상)");
                if (f.BossRatio != 0 && f.K != FxK.Execute) E($"{w}: bossRatio 는 execute 에만");
                if (f.K == FxK.Execute && !(f.Pct > 0 && f.Pct < 1)) E($"{w}: execute 의 pct 는 0~1(그 적 HP 비율 이하면 쓰러뜨림)");
                if (f.K == FxK.CritNext && f.Who != null && f.Who != "other" && f.Who != "self") E($"{w}: critNext 의 who 는 other · self");
                if (f.K == FxK.Stack && f.Pick && (f.Id == null || !(d.Heroes.Values.SelectMany(h => h.AllKeywords).FirstOrDefault(k => k.Name == f.Id)?.Cap > 0))) E($"{w}: stack pick(단계 고르기)은 최대(cap)가 있는 고유 효과에");
                if (f.Lock && (f.K != FxK.Stack || (f.Set == null && !f.Pick))) E($"{w}: lock 은 stack 의 set · pick 과 같이(그 값으로 정하고 turns 턴 고정)");
                if (f.K == FxK.IfPile && f.From == "record") { }
                switch (f.K)
                {
                    case FxK.Dmg: if (!(f.Ratio > 0)) E($"{w}: ratio 가 없다"); if (f.Base != null && f.Base != "def") E($"{w}: base 는 def 만"); effects++; break;
                    case FxK.Block: case FxK.Shield: case FxK.Heal: if (!(f.Ratio > 0)) E($"{w}: ratio 가 없다"); effects++; break;
                    case FxK.Status:
                        if (f.Id == null || !R.ALL_ST.Contains(f.Id)) E(R.IsCardSt(f.Id) ? $"{w}: 「{f.Id}」 는 카드에 붙는 상태 — cardStatus 로" : $"{w}: 모르는 상태 {f.Id}");
                        if (f.V <= 0) E($"{w}: v(겹)가 없다");
                        // 층 — 이로운 · 해로운 효과는 파티 층(대상 「파티」 = 안 적음). 개인 층(구속)만 「자신」
                        if (f.Id != null && R.ALL_ST.Contains(f.Id) && !R.IsBadSt(f.Id) && f.Target != null)
                        {
                            if (!R.IsHeroSt(f.Id) && ALLY_TARGETS.Contains(f.Target)) E($"{w}: {f.Id} 는 파티 층 상태 — 대상은 「파티」(안 적으면 파티 · 파티원 전원에게 든다). 「{f.Target}」 은 개인 층(구속)만");
                            if (R.IsHeroSt(f.Id) && f.Target == "party") E($"{w}: {f.Id} 는 개인 층 상태 — 대상은 「자신」 · 「아군 1명」 · 「아군」");
                        }
                        effects++; break;
                    case FxK.CardStatus:
                        if (string.IsNullOrEmpty(f.Id) || (R.ALL_ST.Contains(f.Id) && !R.IsCardSt(f.Id))) E($"{w}: 카드에 붙는 상태(독 · 봉쇄 · 침체 · 빙결 · 탐구심 · 비용) 또는 데이터가 지은 카드 값 이름이어야 한다 — 「{f.Id}」");
                        if (f.To != null && f.To != "this" && f.To != "hand" && f.To != "draw" && f.To != "pulled") E($"{w}: to 는 this · hand · draw · pulled");
                        if (f.V == 0 && f.Set == null && !f.Half) E($"{w}: v(또는 set · half)가 없다");
                        if (f.V < 0 && R.IsCardSt(f.Id)) E($"{w}: {f.Id} 는 음수로 걸 수 없다(비용 · 카드 값만)");
                        effects++; break;
                    case FxK.IfKill: if (f.Id != null && f.Id != "elite" && f.Id != "boss") E($"{w}: id 는 elite(엘리트 · 보스) · boss(보스만)"); break;
                    case FxK.IfBreak: case FxK.IfAllHeroes: break;
                    // 시범 16(2026-10-08 2단계)
                    case FxK.IfPricier: break;
                    case FxK.IfGained: if (f.Id == null || !keywords.Contains(f.Id)) E($"{w}: 사도 키워드가 아니다 — 「{f.Id}」"); if (f.N <= 0) E($"{w}: n 이 없다(이번 전투에 쌓은 양 n 이상)"); break;
                    case FxK.PerGone: if (f.Who != null && f.Who != "self") E($"{w}: who 는 self(자신의 카드만) 또는 없음(파티 전체)"); if (f.Id != null && (!f.Turn || d.Card(f.Id) == null)) E($"{w}: id(카드)는 turn 과 같이 — 「{f.Id}」"); break;
                    case FxK.Ripen:
                        {
                            var rk = d.Heroes.Values.SelectMany(h => h.AllKeywords).FirstOrDefault(k => k.Name == f.Id);
                            if (rk == null) E($"{w}: 사도 키워드가 아니다 — 「{f.Id}」");
                            else if (!rk.Reserve) E($"{w}: 당겨 쓰기는 예약 키워드(reserve)만 — 「{f.Id}」");
                            if (f.V < 0 || f.V > 1) E($"{w}: v 는 0~1(남은 칸 1당 효과를 깎는 비율, 기본 0.25)");
                            effects++; break;
                        }
                    case FxK.Summon:
                        {
                            var sk = d.Heroes.Values.SelectMany(h => h.AllKeywords).FirstOrDefault(k => k.Name == f.Id);
                            if (sk == null) E($"{w}: 사도 키워드가 아니다 — 「{f.Id}」");
                            if (!(f.Ratio > 0)) E($"{w}: ratio 가 없다(소환물 한 대의 공격력 비율)");
                            if (f.Base != null && f.Base != "def") E($"{w}: base 는 def 만");
                            if (f.Max < 0 || f.Max > Battle.SUMMON_CAP) E($"{w}: max 는 0~{Battle.SUMMON_CAP}(한 카드에 따라 치는 대 수 상한)");
                            effects++; break;
                        }
                    case FxK.Roll: if (f.N < 2 || f.N > 6) E($"{w}: n 은 2~6(셋 중 하나면 3)"); break;
                    case FxK.IfRoll: if (f.N < 1) E($"{w}: n 은 1 이상(굴린 눈)"); if (!fx.Take(i).Any(x => x.K == FxK.Roll)) E($"{w}: 앞에 roll 이 있어야 한다"); break;
                    case FxK.IfPrevSame: case FxK.IfLastMine: case FxK.IfPulled: case FxK.IfShield: case FxK.IfDebt: case FxK.IfTypeNew: break;
                    case FxK.IfInHand: if (d.Card(f.Id) == null) E($"{w}: 카드가 없다 — {f.Id}"); break;
                    case FxK.IfBond: if (f.N < 1 || f.N > 5) E($"{w}: n 은 1~5"); break;
                    case FxK.Recast: if (!(f.Ratio > 0 && f.Ratio <= 1)) E($"{w}: ratio 는 0~1"); effects++; break;
                    case FxK.CostMod: if (f.V == 0) E($"{w}: v(±비용)가 없다"); effects++; break;
                    case FxK.AddTag: if (f.Id == null || Array.IndexOf(Tag.All, f.Id) < 0) E($"{w}: 태그 이름이어야 한다 — 「{f.Id}」"); break;
                    case FxK.CutHit: if (!(f.V > 0 && f.V <= 1)) E($"{w}: v 는 0~1(깎을 비율)"); effects++; break;
                    case FxK.ClearDebt: effects++; break;
                    case FxK.HealMod: if (f.V == 0 || Math.Abs(f.V) > 3) E($"{w}: v 는 비율(0.5 = +50%)"); effects++; break;
                    case FxK.IfWounded: if (f.Target != null && f.Target != "oneEnemy" && f.Target != "party") E($"{w}: target 은 party(기본) · oneEnemy"); break;
                    case FxK.PerTag: if (f.Id == null || (Array.IndexOf(Tag.All, f.Id) < 0 && d.Card(f.Id) == null)) E($"{w}: 태그 이름 또는 카드 id(생성물)여야 한다 — 「{f.Id}」"); break;
                    case FxK.Drain: if (!(f.Ratio > 0 && f.Ratio <= 1)) E($"{w}: ratio 는 0~1(준 피해의 비율)"); effects++; break;
                    case FxK.Extra: if (!(f.Ratio > 0)) E($"{w}: ratio 가 없다"); if (f.Base != null && f.Base != "def") E($"{w}: base 는 def 만"); effects++; break;
                    case FxK.Transform: if (d.Card(f.Id) == null) E($"{w}: 바뀔 카드가 없다 — {f.Id}"); if (f.From != null && f.From != "hand" && !f.From.StartsWith("@") && d.Card(f.From) == null) E($"{w}: from 은 카드 id · hand(거르개와) · @종류 — {f.From}"); effects++; break;
                    case FxK.IfChoice: if (f.N != 1 && f.N != 2) E($"{w}: n 은 1 · 2(갈래)"); break;
                    case FxK.IfRandom: if (!(f.Pct > 0 && f.Pct < 1)) E($"{w}: pct 는 0~1(확률)"); break;
                    case FxK.IfHand: if (f.N < 0) E($"{w}: n 은 0 이상(손패 n 장 이하)"); break;
                    case FxK.IfPile: case FxK.PerPile: if (f.From != null && !PILES.Contains(f.From) && !(f.K == FxK.IfPile && f.From == "record")) E($"{w}: from 은 draw · discard · gone · hand"); if (f.K == FxK.IfPile && f.N <= 0) E($"{w}: n 이 없다"); break;
                    case FxK.IfNth: if (f.N <= 0) E($"{w}: n 이 없다(1 이상)"); if (f.Id != null && !f.After) E($"{w}: id(글 이름)는 after 와 같이"); break;
                    case FxK.IfStreak: if (f.N < 2) E($"{w}: n 은 2 이상(같은 사도 카드를 잇달아 n 장째)"); if (f.Type != null && f.Type != "공격" && f.Type != "스킬" && f.Type != "강화") E($"{w}: type 은 공격 · 스킬 · 강화"); break;
                    case FxK.IfFoe: if (!(f.Id == null && (f.HpBelow > 0 || f.HitsBy != null)) && f.Id != "broken" && f.Id != "tough" && f.Id != "guarded" && f.Id != "attack" && f.Id != "hp" && f.Id != "hpMob" && f.Id != "alone" && f.Id != "anyAttack" && f.Id != "known" && !R.ALL_ST.Contains(f.Id)) E($"{w}: id 는 broken · tough · guarded · attack · anyAttack · known · hp · hpMob · alone · 상태 이름"); break;
                    case FxK.IfCardSt: case FxK.PerCardSt: if (string.IsNullOrEmpty(f.Id)) E($"{w}: id(카드 값 이름)가 없다"); if (f.From != null && (f.K != FxK.PerCardSt || f.From != "hand")) E($"{w}: from 은 perCardSt 의 hand(손의 카드 값 합)만"); break;
                    // 엘다인 문법(2026-10-11)
                    case FxK.SpendShield: if (!f.Half && !f.All && !f.Pick && f.V <= 0) E($"{w}: half · all · pick · v 가운데 하나가 필요하다"); effects++; break;
                    case FxK.PerShield: if (f.Per < 0) E($"{w}: per 는 양수(파티 실드 몇 마다)"); if (f.Max < 0) E($"{w}: max 는 0 이상"); break;
                    case FxK.IfForm: if (f.Id != null && d.Form(f.Id) == null) E($"{w}: 없는 변신 — 「{f.Id}」(사도의 forms id)"); break;
                    case FxK.FormExtend: if (f.Id != null && d.Form(f.Id) == null) E($"{w}: 없는 변신 — 「{f.Id}」"); if (f.V < 0 || f.Max < 0) E($"{w}: v · max 는 0 이상"); effects++; break;
                    case FxK.NoForget: break;
                    case FxK.CounterHits: case FxK.CounterAll: case FxK.CounterMod: case FxK.GaugeLock: case FxK.Record: case FxK.Replay: effects++; break;
                    case FxK.IfEvent: break;
                    case FxK.Taunt: case FxK.CritNext: case FxK.Execute: effects++; break;
                    // 엘다인 4차(E76~E87)
                    case FxK.NthNext: effects++; break;
                    case FxK.FormKeep: effects++; break;
                    case FxK.NoToggle: break;
                    case FxK.Learn: if (f.Owner != null && d.Hero(f.Owner) == null) E($"{w}: learn 의 owner 는 사도 id"); effects++; break;
                    case FxK.MoveMark: if (f.Id == null || d.CarrierOf(f.Id) != "hero") E($"{w}: moveMark 의 id 는 아군 표시(carrier hero) 고유 효과"); effects++; break;
                    case FxK.IfCost: if (f.Max < 0) E($"{w}: ifCost 의 max 는 0 이상(이 카드를 비용 max 이하로 내면)"); if (f.Id != null && f.Id.Length == 0) E($"{w}: id(글 이름)가 비었다"); break;
                    case FxK.UltCostMod: if (f.V == 0 || f.V <= -1) E($"{w}: ultCostMod 의 v 는 0 이 아닌 비율(-0.25 = 요구 게이지 -25%, 하한 50%)"); effects++; break;
                    case FxK.SwiftTurn: effects++; break;
                    case FxK.CritDmg: if (!(f.V > 0)) E($"{w}: critDmg 의 v 는 양수(치명 피해 +비율)"); if (f.Off != null) FxList(w + " off", f.Off); effects++; break;
                    case FxK.IfHitTaken: if (f.N <= 0) E($"{w}: ifHitTaken 에 n 이 없다(맞은 대 수 n 이상)"); break;
                    case FxK.StackLock: if (f.Id == null || !keywords.Contains(f.Id)) E($"{w}: 사도 키워드가 아니다 — 「{f.Id}」"); break;
                    case FxK.PerPlayed: if (f.Id != null && Array.IndexOf(Tag.All, f.Id) < 0) E($"{w}: id 는 태그 이름(없으면 낸 카드 전부)"); break;
                    case FxK.PerEvent: break;
                    case FxK.PerGuarded: if (f.Per < 0) E($"{w}: per 는 양수(막아 낸 양 몇 마다)"); if (f.Max < 0) E($"{w}: max 는 0 이상(최대 몇 번)"); break;
                    case FxK.PerOverheal: if (f.Per < 0) E($"{w}: per 는 양수(넘친 회복 몇 마다)"); if (f.Max < 0) E($"{w}: max 는 0 이상(최대 몇 번)"); if (f.Pct < 0 || f.Pct >= 1) E($"{w}: pct 는 0~1(회복 뒤 HP 그 비율을 넘은 몫 — 0 이면 최대 HP 를 넘친 몫)"); break;
                    case FxK.Empower: if (!(f.Ratio > 0 && f.Ratio <= 3)) E($"{w}: ratio 는 0~3(다음 카드 피해 · 실드 · 회복 +비율)"); effects++; break;
                    case FxK.Later: case FxK.AfterCards: case FxK.Trap:
                        if (f.Then == null || f.Then.Count == 0) E($"{w}: then(안에 든 효과)이 없다"); else FxList(w + " then", f.Then);
                        if (f.K != FxK.Trap && f.N <= 0) E($"{w}: n 이 없다(턴 · 장수)");
                        effects++; break;
                    case FxK.Confuse: case FxK.AutoPlay: case FxK.CastOther: case FxK.Dispel: effects++; break;
                    case FxK.Pull: case FxK.ExileFrom:
                        if (f.From != null && !PILES.Contains(f.From) && !(f.K == FxK.ExileFrom && f.From == "pulled") && !(f.K == FxK.Pull && f.From == "discarded")) E($"{w}: from 은 draw · discard · gone · hand (exileFrom 은 pulled 도 · pull 은 discarded 도)");
                        if (f.K == FxK.Pull && f.To != null && f.To != "hand" && f.To != "top") E($"{w}: to 는 hand · top");
                        if (f.At != null && f.At != "top" && f.At != "bottom" && f.At != "random") E($"{w}: at 은 top · bottom · random");
                        effects++; break;
                    case FxK.GrowRun: if (f.Id != null && f.Id != "atk" && f.Id != "def" && f.Id != "crit") E($"{w}: id 는 atk · def · crit"); if (f.V == 0) E($"{w}: v 가 없다"); effects++; break;
                    case FxK.Stack: case FxK.Spend:
                        if (f.Id == null || !(keywords.Contains(f.Id) || (f.K == FxK.Spend && R.ALL_ST.Contains(f.Id) && !f.Pick))) E($"{w}: 사도 키워드가 아니다 — 「{f.Id}」{(f.K == FxK.Spend ? "(상태를 쓰려면 엔진 상태 이름 · all · v)" : "")}");
                        if (f.K == FxK.Spend && !f.All && !f.Pick && f.V <= 0) E($"{w}: v · all · pick 가운데 하나가 필요하다");
                        if (f.K == FxK.Stack && f.V == 0 && !f.Carry && f.Set == null && !f.Pick) E($"{w}: v(또는 carry · set)가 없다");   // 음수 v 는 조용히 깎기(▶E21 — spend 계기를 안 깨운다)
                        effects++; break;
                    case FxK.IfStack: case FxK.PerStack:
                        if (f.Id == null || !keywords.Contains(f.Id)) E($"{w}: 사도 키워드가 아니다 — 「{f.Id}」");
                        break;
                    case FxK.Make: if (d.Card(f.Id) == null) E($"{w}: 만들 카드가 없다 — {f.Id}"); if (f.To != null && f.To != "hand" && f.To != "draw" && f.To != "top" && f.To != "discard") E($"{w}: to 는 hand · draw · top · discard"); effects++; break;
                    case FxK.IfPrev: if (f.Type != "공격" && f.Type != "스킬" && f.Type != "강화") E($"{w}: type 은 공격 · 스킬 · 강화"); break;
                    case FxK.When: if (Array.IndexOf(FxK.WHEN_ON, f.On) < 0) E($"{w}: on 은 {string.Join(" · ", FxK.WHEN_ON)}"); break;
                    case FxK.IfRhythm: if (f.N <= 0) E($"{w}: n 이 없다"); break;
                    case FxK.Draw: case FxK.Ap: case FxK.NextCheaper: case FxK.Gauge: case FxK.Tough: case FxK.RushDown: case FxK.Hasten: case FxK.PayHp:
                        if (f.V == 0) E($"{w}: v 가 없다"); effects++; break;
                    case FxK.Discard: case FxK.SpendRhythm: if (!f.All && f.V <= 0) E($"{w}: v 또는 all 이 필요하다"); effects++; break;
                    case FxK.DealtMod: case FxK.TakenMod: case FxK.AtkMod: case FxK.DefMod: case FxK.CritMod:
                        if (f.V == 0) E($"{w}: v 가 없다"); if (Math.Abs(f.V) > 3) E($"{w}: v 는 비율(0.1 = 10%)"); effects++; break;
                    case FxK.IfHeld: case FxK.IfSpent: case FxK.IfDebuffs: if (f.N <= 0) E($"{w}: n 이 없다(1 이상)"); break;
                    case FxK.IfPlayedMax: if (f.N < 0) E($"{w}: n 은 0 이상"); break;
                    case FxK.IfApLeft: if (f.N <= 0) E($"{w}: n 이 없다(1 이상)"); break;
                    case FxK.NextAp: if (f.V == 0) E($"{w}: v 가 없다(다음 턴 AP ±v)"); effects++; break;
                    case FxK.Burn: if (!f.All && f.V <= 0) E($"{w}: v 또는 all 이 필요하다"); effects++; break;
                    case FxK.Reflect: if (!(f.Ratio > 0)) E($"{w}: ratio 가 없다(받은 피해의 비율)"); effects++; break;
                    case FxK.PerPaid: if (f.Per < 0) E($"{w}: per 는 양수(HP 몇 마다)"); break;
                    case FxK.IfHp: if (!(f.Pct > 0 && f.Pct < 1)) E($"{w}: pct 는 0~1(0.5 = 50% 이하)"); break;
                    case FxK.Feed: if (f.V <= 0) E($"{w}: v 가 없다"); effects++; break;
                    case FxK.Form: if (d.Form(f.Id) == null) E($"{w}: 없는 변신 — 「{f.Id}」(사도의 forms id)"); effects++; break;
                    case FxK.FormEnd: break;
                    case FxK.Cue: if (string.IsNullOrEmpty(f.Id)) E($"{w}: cue 에 id(연출 이름)가 없다"); if (f.XStack != null && !keywords.Contains(f.XStack)) E($"{w}: xStack 은 사도 키워드 — 「{f.XStack}」"); break;
                    case FxK.Power:
                        if (!powerOk) E($"{w}: power(강화 지속 규칙)는 강화 카드 · 그 신탁(· 「강화 카드.」 신탁)의 효과에만");
                        if (f.Rules == null || f.Rules.Count == 0) { E($"{w}: rules(전투 동안 켜질 규칙)가 없다"); effects++; break; }
                        {
                            bool ok0 = powerOk; powerOk = false;
                            try
                            {
                                for (int j = 0; j < f.Rules.Count; j++)
                                {
                                    var r = f.Rules[j];
                                    string rat = $"{w} rules[{j}]";
                                    Rule(rat, r);
                                    if (r.When?.On == "fightStart") E($"{rat}: 강화는 카드를 낸 뒤에 켜진다 — fightStart 는 돌지 않는다(turnStart 를)");
                                    if (r.When?.On == "always" && r.Fx.Any(x => !FxK.Mods.Contains(x.K) || x.K == FxK.HealMod)) E($"{rat}: always 규칙은 증감(dealtMod · takenMod · atkMod · defMod · critMod)만");
                                    if (r.Fx.Count == 0) E($"{rat}: fx 가 없다");
                                }
                            }
                            finally { powerOk = ok0; }
                        }
                        effects++; break;
                    default: if (!FxK.Conditions.Contains(f.K) && !FxK.Pers.Contains(f.K)) effects++; break;
                }
                if (FxK.Pers.Contains(f.K) && !(i + 1 < fx.Count && (fx[i + 1].K == FxK.Dmg || fx[i + 1].K == FxK.Block || fx[i + 1].K == FxK.Shield || fx[i + 1].K == FxK.Heal || fx[i + 1].K == FxK.Make || fx[i + 1].K == FxK.Extra || fx[i + 1].K == FxK.Stack || fx[i + 1].K == FxK.Tough)))
                    E($"{w}: 「1개당」 바로 뒤에는 피해 · 방어 · 실드 · 회복 · 추가 공격 · 만들기 · 쌓기가 와야 한다");
            }
            // 고학년은 넷까지 — 원작 피해를 얹고 효과 셋을 그대로 둔다(2026-10-06 사용자 결정: 피해 없던 고학년에 원작 피해)
            int maxFx = at.EndsWith(" 고학년") ? 4 : 3;
            if (effects > maxFx) W($"{at}: 효과가 {effects}개 — 카드당 셋까지 · 고학년은 넷까지(docs/18 §3)");
        }

        void Tags(string at, List<string> tags)
        {
            foreach (var t in tags ?? new List<string>())
            {
                var (id, n) = Tag.Parse(t);
                if (Array.IndexOf(Tag.All, id) < 0) E($"{at}: 모르는 태그 「{t}」");
                if (n > 0 && id != Tag.Exhaust && id != Tag.Recall) E($"{at}: 수가 붙는 태그는 소멸 · 회수뿐 — 「{t}」");
                if (n > 0 && Tag.Counted.TryGetValue(id, out var ok) && Array.IndexOf(ok, n) < 0) W($"{at}: 「{t}」 — 사전의 수는 {id} {string.Join(" · ", ok)}");
            }
        }

        /// <summary>지금 보는 효과가 power(강화 지속 규칙)를 써도 되는 자리인가 — 강화 카드 · 그 신탁.</summary>
        bool powerOk;

        /// <summary>강화 카드에 전투 동안 남는 효과가 있나 — power 규칙 · 세기형 버프(사기 · 결의 …) · 전투 내내 증감.</summary>
        static readonly HashSet<string> LASTING_ST = new() { "사기", "불굴", "결의", "결정화", "고동", "근면", "계몽", "집중" };
        static bool HasLasting(List<Fx> fx) => fx != null && fx.Any(f => f.K == FxK.Power || (f.K == FxK.Status && f.Target == null && LASTING_ST.Contains(f.Id ?? "")) || (FxK.Mods.Contains(f.K) && f.Run));

        /// <summary>다 차면 저절로 터지나 — stackReach · stackOver 규칙에 만들기 · 강화 · 소모 · 연출 말고 다른 효과가 있거나, onMax 가 변신이면.</summary>
        static readonly HashSet<string> NOT_BURST = new() { FxK.Make, FxK.Empower, FxK.Spend, FxK.Cue };
        public static bool AutoBurst(HeroDef h) =>
            h.Passives.Concat(h.AllKeywords.SelectMany(k => k.Rules)).Any(r => r.When != null && (r.When.On == "stackReach" || r.When.On == "stackOver")
                && r.Fx.Any(f => !NOT_BURST.Contains(f.K) && !FxK.Conditions.Contains(f.K) && !FxK.Pers.Contains(f.K)))
            || h.AllKeywords.Any(k => k.OnMax != null && k.OnMax.Form != null);

        void Rule(string at, PassiveRule r)
        {
            if (r.When == null || r.When.On == null || !WHEN_ON.Contains(r.When.On)) E($"{at}: 모르는 언제 「{r.When?.On}」");
            if (r.When != null)
            {
                if ((r.When.On == "stackReach" || r.When.On == "stackGone" || r.When.On == "stackOver") && (r.When.Id == null || !keywords.Contains(r.When.Id))) E($"{at}: 키워드가 아니다 — 「{r.When.Id}」");
                if (r.When.On == "stackReach" && r.When.N <= 0) E($"{at}: stackReach 에 n 이 없다");
                if (r.When.On == "rhythm" && r.When.N <= 0) E($"{at}: rhythm 에 n 이 없다");
                if (r.When.On == "lowHp" && !(r.When.Pct > 0 && r.When.Pct < 1)) E($"{at}: lowHp 의 pct 는 0~1");
                if (r.When.On == "keepAp" && r.When.Kind != null && r.When.Kind != "ap" && r.When.Kind != "keep") E($"{at}: keepAp 의 kind 는 ap(AP 를 남김) · keep(보존 카드를 쥐고 넘김) — 없으면 둘 다");
                if (r.When.On == "summonAct" && r.When.Kind != null && r.When.Kind != "atk" && r.When.Kind != "guard" && r.When.Kind != "lost") E($"{at}: summonAct 의 kind 는 atk(따라 침) · guard(대신 맞음) · lost(하나가 사라짐) — 없으면 모두");
                if (r.When.On == "overheal" && (r.When.Pct < 0 || r.When.Pct >= 1)) E($"{at}: overheal 의 pct 는 0~1(회복 뒤 HP 가 그 비율 이상이면 — 0 이면 최대 HP 를 넘칠 때)");
                if (r.When.Seq != null && (r.When.Seq.Count < 2 || r.When.Seq.Count > 3)) E($"{at}: seq 는 종류 2~3");
                if (r.When.On == "huntDown" && r.When.Id != null && !keywords.Contains(r.When.Id)) E($"{at}: 키워드가 아니다 — 「{r.When.Id}」");
                if (r.When.Fresh && r.When.On != "debuff") E($"{at}: fresh 는 debuff 에만");
                if (r.When.Repeat && r.When.On != "play") E($"{at}: repeat 는 play 에만");
                if ((r.When.Chain || r.When.SameHero) && r.When.On != "play") E($"{at}: chain · sameHero 는 play 에만");
                if (r.When.Who != null && r.When.Who != "any" && r.When.Who != "other") E($"{at}: who 는 any · other");
                if (r.When.Who == "other" && !new[] { "play", "spend", "link", "crit", "make", "hit", "drawn", "extra", "unwound", "exhaust", "discard", "kill", "break", "formOn", "debuff" }.Contains(r.When.On)) E($"{at}: who other 는 play · spend · link · crit · make · hit · drawn · extra · unwound · exhaust · discard · kill · break · formOn · debuff 에만");
                if (r.When.CardSt != null && r.When.On != "play") E($"{at}: cardSt 는 play 에만");
                foreach (var c in r.Conds) if (c.C == "ally" && d.Hero(c.Id) == null) W($"{at}: 조건 ally 의 사도 {c.Id} 가 없다");
                if (r.When.Guarded && r.When.On != "hurt") E($"{at}: guarded 는 hurt 에만");
                if (r.When.Marked != null && (!keywords.Contains(r.When.Marked) || d.CarrierOf(r.When.Marked) != "hero")) E($"{at}: marked 는 사도 표시(carrier hero) 고유 효과 이름 — 「{r.When.Marked}」");
                if (r.When.Tag != null && Array.IndexOf(Tag.All, r.When.Tag) < 0 && d.Card(r.When.Tag) == null) E($"{at}: 모르는 태그 「{r.When.Tag}」(태그 이름 · 카드 id)");
                if (r.When.By != null && ((r.When.By != "self" && r.When.By != "other") || (r.When.On != "discard" && r.When.On != "exhaust"))) E($"{at}: by 는 discard · exhaust 에 self · other");
                if (r.When.Token == false && r.When.On != "play") E($"{at}: token false 는 play 에만");
                if (r.When.PaidMax != null && (r.When.On != "play" || r.When.PaidMax < 0)) E($"{at}: paidMax 는 play 에 0 이상(낼 때의 비용)");
                if (r.When.FoeStack != null && (r.When.On != "hit" || d.CarrierOf(r.When.FoeStack) != "enemy")) E($"{at}: foeStack 은 hit 에 적 표식(carrier enemy) 이름");
                if (r.When.On == "formOn" && r.When.Id != null && d.Form(r.When.Id) == null) E($"{at}: 없는 변신 — 「{r.When.Id}」");
            }
            foreach (var c in r.Conds) if (c.C == null || !CONDS.Contains(c.C)) E($"{at}: 모르는 조건 「{c.C}」");
            if (r.Limit != null && r.Limit.Per != "turn" && r.Limit.Per != "fight") E($"{at}: limit.per 는 turn · fight");
            FxList(at, r.Fx);
        }

        // ── 엘다인 설계 규칙(2026-10-11, 데이터.md §22-7 · ▶E29) ──────────────
        /// <summary>엘다인 사도인가 — Run.NO_CLONE(엘다인 20명 id).</summary>
        public static bool IsEldain(string heroId) => heroId != null && Bolzena.Core.Run.NO_CLONE.Contains(heroId);

        static IEnumerable<Fx> WalkAll(IEnumerable<Fx> fx) => (fx ?? Enumerable.Empty<Fx>()).SelectMany(f => new[] { f }
            .Concat(WalkAll(f.Then)).Concat(WalkAll(f.Else)).Concat(WalkAll(f.HitFx)).Concat((f.Rules ?? new List<PassiveRule>()).SelectMany(r => WalkAll(r.Fx))));

        /// <summary>기본 카드 — 그 사도의 고유 · 생성이 아닌 카드.</summary>
        IEnumerable<CardDef> BasicsOf(HeroDef h) => d.Cards.Values.Where(c => c.Hero == h.Id && !c.Unique && !c.Token);

        /// <summary>엘다인 3차(2026-10-11 카제나 재정렬) — 「전부 써서」 사도 수 · 겹 자원 주 장치 사도 수 · 고유 카드 글 길이(공백 뺀) · AP 신탁 상한 예외(AP 가 주 장치).</summary>
        public const int ALL_MAX = 3, STACK_MAIN_MAX = 4, TEXT_MAX = 45;
        public static readonly HashSet<string> AP_ORACLE_FREE = new() { "우이", "실비아" };   // 실비아 — AP 회복 탱커(사용자 2026-10-11)
        /// <summary>끈기(쓰러질 피해 1번 무효)를 줘도 되는 딜러(사용자 결정 ▶E67 — 비비(신성) 「영원한 포옹」).</summary>
        public static readonly HashSet<string> ENDURE_FREE = new() { "비비_신성" };

        /// <summary>
        /// (엘다인 4차 ▶E86) 소유 키워드 — 사전 키워드 하나에 보상받는(정산하는) 사도는 하나(content-v2 _measure/엘다인_엔진/07 §3).
        /// 다른 엘다인 사도의 카드 · 규칙에 그 키워드의 정산 조각(1개당 · 그 상태인 적이면 · 그것이 터지면 …)이 있으면 주의. 걸어 주기(stack · status 로 거는 것)는 괜찮다.
        /// </summary>
        public static readonly Dictionary<string, string> OWNED_KW = new()
        {
            ["탄환"] = "시온더다크불릿", ["천상"] = "이드", ["실드 소모"] = "이드", ["약점 노출"] = "아야", ["연계"] = "에피카",
            ["반격"] = "비비", ["도발"] = "비비", ["취약"] = "비비", ["탐구심"] = "클로에", ["넘친 회복"] = "우이", ["연쇄"] = "우이",
            ["고학년 게이지"] = "우이_기억", ["신속"] = "리뉴아", ["협공"] = "실비아", ["치명"] = "비비_신성", ["근면"] = "에르핀_왕도", ["X"] = "에르핀_왕도",
            ["약화"] = "란", ["연속"] = "디아나_왕년", ["축복"] = "죠안", ["피해 기반 회복"] = "죠안",
            ["처형"] = "티그_영웅", ["격파"] = "네르_빡침", ["둔화"] = "요미", ["존속"] = "벨라",
        };
        static readonly string[] OWNED_TAGS = { Tag.Bullet, Tag.Heaven, Tag.Link, Tag.Swift, Tag.Echo, Tag.Blessing };   // 망각은 주인 없음(4차 아야 새 컨셉)
        static readonly string[] OWNED_FOE_ST = { "취약", "약화", "둔화", "약점 노출" };

        /// <summary>효과 조각 하나가 정산하는 소유 키워드(없으면 null).</summary>
        static string OwnedOf(Fx f) => f.K switch
        {
            FxK.CounterHits or FxK.CounterAll or FxK.CounterMod => "반격",
            FxK.Spend or FxK.PerStack or FxK.IfStack when f.Id == "반격" => "반격",
            FxK.Taunt => "도발",
            FxK.IfFoe or FxK.PerStack when f.Id != null && Array.IndexOf(OWNED_FOE_ST, f.Id) >= 0 => f.Id,
            FxK.IfFoe when f.Id == "broken" => "격파",
            FxK.IfFoe when f.HitsBy != null => "처형",
            FxK.IfCardSt or FxK.PerCardSt when f.Id == "탐구심" => "탐구심",
            FxK.PerOverheal => "넘친 회복",
            FxK.UltCostMod => "고학년 게이지",
            FxK.CritDmg => "치명",
            FxK.Status when f.Id == "근면" => "근면",
            FxK.IfChain => "연속",
            FxK.Drain => "피해 기반 회복",
            FxK.Execute => "처형",
            FxK.IfBreak => "격파",
            FxK.SpendShield or FxK.PerShield => "실드 소모",
            FxK.Dmg or FxK.Shield when f.OfShield => "실드 소모",
            _ => null,
        };
        /// <summary>규칙 계기가 정산하는 소유 키워드(없으면 null).</summary>
        static string OwnedOf(When w) => w == null ? null : w.On switch
        {
            "counter" => "반격", "overheal" => "넘친 회복", "ult" => "고학년 게이지", "crit" => "치명", "endure" => "끈기", "break" => "격파",
            "hit" when w.Marked != null => "협공",
            _ => w.Chain ? "연속" : null,
        };

        /// <summary>그 엘다인 사도가 정산하는 남의 소유 키워드 — (키워드, 어디) 목록.</summary>
        IEnumerable<(string kw, string loc)> OwnedMisuse(HeroDef h, List<CardDef> cards)
        {
            var hits = new List<(string, string)>();
            void Fxs(string loc, IEnumerable<Fx> fx) { foreach (var f in WalkAll(fx)) { var k = OwnedOf(f); if (k != null) hits.Add((k, loc)); if (f.K == FxK.Power) foreach (var pr in f.Rules ?? new List<PassiveRule>()) { var k2 = OwnedOf(pr.When); if (k2 != null) hits.Add((k2, loc + " 강화 규칙")); } } }
            foreach (var c in cards.Where(c => c.Unique || c.Token))
            {
                Fxs(c.Id, c.Fx);
                for (int i = 0; i < c.Oracles.Count; i++) { Fxs($"{c.Id} 신탁{i + 1}", c.Oracles[i].Fx); foreach (var t in c.Oracles[i].Tags) if (Array.IndexOf(OWNED_TAGS, Tag.Parse(t).id) >= 0) hits.Add((Tag.Parse(t).id, $"{c.Id} 신탁{i + 1} 태그")); }
                foreach (var t in c.Tags) if (Array.IndexOf(OWNED_TAGS, Tag.Parse(t).id) >= 0) hits.Add((Tag.Parse(t).id, $"{c.Id} 태그"));
                if (c.X) hits.Add(("X", c.Id));
            }
            if (h.Ult != null) Fxs("고학년", h.Ult.Fx);
            foreach (var r in h.Passives.Concat(h.AllKeywords.SelectMany(k => k.Rules)))
            {
                var k = OwnedOf(r.When); if (k != null) hits.Add((k, $"규칙 {r.Name}"));
                Fxs($"규칙 {r.Name}", r.Fx);
            }
            foreach (var k in h.AllKeywords) foreach (var p in k.Per) if (p.Stat == "ultCost") hits.Add(("고학년 게이지", $"「{k.Name}」 1개당"));
            return hits.Where(x => OWNED_KW.TryGetValue(x.Item1, out var o) && o != h.Id).Distinct();
        }

        static readonly HashSet<string> BASIC_KEYS = new() { "k", "ratio", "target", "base" };
        static readonly HashSet<string> GUARD_ST = new() { "불굴", "끈기", "피해 감소" };

        /// <summary>% 단위 — 10% 단위, 20% 미만은 5% 단위. 어기면 그 % 를 돌려준다(맞으면 null).</summary>
        static string PctOff(double v)
        {
            double x = Math.Abs(v) * 100;
            if (x < 1e-9) return null;
            double unit = x < 20 - 1e-9 ? 5 : 10;
            double q = x / unit;
            return Math.Abs(q - Math.Round(q)) > 1e-6 ? $"{Math.Round(x, 2)}%" : null;
        }

        void PctCheck(string at, IEnumerable<Fx> fx)
        {
            foreach (var f in WalkAll(fx))
            {
                if (f.K == FxK.Ripen || f.K == FxK.Stack || FxK.Conditions.Contains(f.K) || FxK.Pers.Contains(f.K)) continue;
                string bad = PctOff(f.Ratio) ?? (f.K == FxK.Dmg || f.K == FxK.Shield || f.K == FxK.Block ? PctOff(f.OfEvent) ?? PctOff(f.OfForm) : null) ?? (FxK.Mods.Contains(f.K) || f.K == FxK.HealMod || f.K == FxK.CritDmg ? PctOff(f.V) : null);
                if (bad != null) E($"{at} {f.K}: % 는 10% 단위(20% 미만은 5%) — {bad}");
            }
        }

        /// <summary>딜러가 주면 안 되는 것 — 실드 · 방어 · 회복 · 흡수 · 아군 받는 피해 감소 · 불굴 · 끈기 · 피해 감소.</summary>
        static string GuardOf(Fx f)
        {
            if (f.K == FxK.Shield || f.K == FxK.Block || f.K == FxK.Heal || f.K == FxK.Drain) return f.K;
            if (f.K == FxK.TakenMod && f.V < 0 && (f.Target == null || f.Target == "auto" || ALLY_TARGETS.Contains(f.Target) || f.Target == "party")) return "받는 피해 감소";
            if (f.K == FxK.Status && GUARD_ST.Contains(f.Id ?? "")) return f.Id;
            return null;
        }

        static bool HitsRule(PassiveRule r) => WalkAll(r.Fx).Any(f => f.K == FxK.Extra || f.K == FxK.Summon || (f.K == FxK.Dmg && f.Target != "self" && f.Target != "party" && !ALLY_TARGETS.Contains(f.Target ?? "")));
        static string WhenSig(When w) => w == null ? "" : $"{w.On}|{w.Id}|{w.Who}|{w.Type}|{w.Tag}|{w.N}|{w.By}|{w.Kind}";

        /// <summary>「전부 써서 1개당」 — spend · discard · burn 의 all 뒤에 비례가 오나.</summary>
        static bool SpendAllPer(List<Fx> fx)
        {
            if (fx == null) return false;
            for (int i = 0; i < fx.Count; i++)
                if (fx[i].All && (fx[i].K == FxK.Spend || fx[i].K == FxK.Discard || fx[i].K == FxK.Burn) && fx.Skip(i + 1).Any(x => FxK.Pers.Contains(x.K))) return true;
            return fx.Any(f => SpendAllPer(f.Then) || SpendAllPer(f.Else));
        }

        void Eldain()
        {
            var el = d.Heroes.Values.Where(h => IsEldain(h.Id)).ToList();
            var allAll = new List<string>();
            var stackMain = new List<string>();
            var tx = new CardText(d);
            var names = d.Heroes.Values.Concat(d.CloneHeroes.Values).Where(x => !string.IsNullOrEmpty(x.Name)).Select(x => (x.Id, x.Name)).ToList();
            foreach (var h in el)
            {
                string at = $"엘다인 {h.Id}{d.At("hero", h.Id)}";
                var cards = d.Cards.Values.Where(c => c.Hero == h.Id).ToList();
                var basics = BasicsOf(h).ToList();
                var uniques = cards.Where(c => c.Unique).ToList();
                // 1. 기본 카드 — 서로 다른 2~3종, 시작 덱에 기본 3장(같은 카드 2장 가능 — 사용자 2026-10-11 카제나식). 조각 하나(dmg · shield · heal), 태그 · 조건 · 신탁 · 축복 · 덧효과 없음
                var starterBasics = h.Starter.Where(id => basics.Any(c => c.Id == id)).ToList();
                if (basics.Count < 2 || basics.Count > 3) E($"{at}: 기본 카드는 서로 다른 2~3종 — 지금 {basics.Count}종({string.Join(" · ", basics.Select(c => c.Id))})");
                if (starterBasics.Count != 3) E($"{at}: 시작 덱의 기본 카드는 3장 — 지금 {starterBasics.Count}장");
                foreach (var c in basics)
                {
                    string ca = $"{at} 기본 카드 {c.Id}";
                    if (c.Tags.Count > 0) E($"{ca}: 기본 카드에 태그를 달지 않는다 — {string.Join(" · ", c.Tags)}");
                    if (c.Oracles.Count > 0 || c.Blesses.Count > 0) E($"{ca}: 기본 카드에 신탁 · 축복이 없어야 한다");
                    if (c.Fx.Count != 1) E($"{ca}: 기본 카드는 효과 조각 하나(피해 · 실드 · 회복) — 지금 {c.Fx.Count}개");
                    foreach (var f in c.Fx)
                    {
                        if (f.K != FxK.Dmg && f.K != FxK.Shield && f.K != FxK.Heal) E($"{ca}: 기본 카드는 dmg · shield · heal 만 — {f.K}");
                        var keys = Newtonsoft.Json.Linq.JObject.Parse(GameData.ToJson(f)).Properties().Select(p => p.Name).Where(k => !BASIC_KEYS.Contains(k)).ToList();
                        if (keys.Count > 0) E($"{ca}: 기본 카드에 덧효과 · 조건을 달지 않는다 — {string.Join(" · ", keys)}");
                    }
                    if (c.X || c.PayWith != null || c.Debt || c.Choices != null || c.Evolve != null || c.CountAs != 0) E($"{ca}: 기본 카드는 한 줄 수치만(x · payWith · debt · choices · evolve · countAs 없음)");
                    if (!h.Starter.Contains(c.Id)) E($"{ca}: 기본 카드는 모두 시작 덱에");
                }
                // 1-2. 가벼운 덱 · 무거운 덱(사용자 2026-10-11, 4차) — 가벼운 = 1코 공격 1종 × 2장 + 1코 방어 1종 · 무거운 = 1코 공격 · 2코 공격 · 1코 방어 각 1장.
                //      방어는 실드 · 회복. 수치는 기준(1코 공격 100% · 2코 공격 220% · 1코 실드 · 회복 140%) ±20%, 탱커 공격은 방어 기반 · 기본 공격은 적 1명
                if (starterBasics.Count == 3 && basics.All(c => c.Fx.Count == 1))
                {
                    CardDef B(string id) => basics.First(c => c.Id == id);
                    var slots = starterBasics.Select(B).ToList();
                    bool Atk(CardDef c) => c.Fx[0].K == FxK.Dmg;
                    var atk1 = slots.Where(c => Atk(c) && c.Cost == 1).ToList();
                    var atk2 = slots.Where(c => Atk(c) && c.Cost == 2).ToList();
                    var def1 = slots.Where(c => !Atk(c) && c.Cost == 1).ToList();
                    bool light = atk1.Count == 2 && atk1.Select(c => c.Id).Distinct().Count() == 1 && def1.Count == 1;
                    bool heavy = atk1.Count == 1 && atk2.Count == 1 && def1.Count == 1;
                    if (!light && !heavy) E($"{at}: 기본 덱은 가벼운(1코 공격 1종 × 2장 + 1코 방어) · 무거운(1코 공격 · 2코 공격 · 1코 방어 각 1장) — 지금 {string.Join(" · ", slots.Select(c => $"{c.Cost}코 {(Atk(c) ? "공격" : "방어")}"))}");
                    foreach (var c in basics)
                    {
                        var f = c.Fx[0];
                        bool atk = f.K == FxK.Dmg;
                        double want = atk ? (c.Cost == 2 ? 2.2 : 1.0) : (c.Cost == 2 ? 3.0 : 1.7);   // 2026-10-12 실드 · 회복 ×1.2(옛 1.4 · 2.5)
                        if (Math.Abs(f.Ratio - want) > (atk ? 0.2 : 0.3 * (c.Cost == 2 ? 2 : 1)) + 1e-9) E($"{at} 기본 카드 {c.Id}: {c.Cost}코 {(atk ? "공격" : "방어")}는 {Math.Round(want * 100)}% 근처(±20%) — 지금 {Math.Round(f.Ratio * 100)}%");
                        if (atk && h.Role == "탱커" && f.Base != "def") E($"{at} 기본 카드 {c.Id}: 탱커의 기본 공격은 방어 기반(base def)");
                        if (atk && h.Role != "탱커" && f.Base == "def") E($"{at} 기본 카드 {c.Id}: 방어 기반 기본 공격은 탱커만");
                        if (atk && (f.Target ?? "oneEnemy") != "oneEnemy") E($"{at} 기본 카드 {c.Id}: 기본 공격은 적 1명");
                    }
                }
                // 2. 시작 덱 = 기본 3장 + 시동(고유) 0~1장 — 시동을 넣을지는 사도별(사용자 2026-10-11)
                var opener = uniques.Where(c => h.Starter.Contains(c.Id)).ToList();
                // 사용자 2026-10-11 — 20명 모두 시작 덱 = 기본 3 + 시동 1(고유 카드 가운데 아무 것이나 · 정확히 4장)
                if (opener.Count != 1) E($"{at}: 시작 덱의 고유 카드(시동)는 꼭 1장 — 지금 {opener.Count}장");
                if (h.Starter.Count != 4 || h.Starter.Any(id => d.Card(id) is CardDef sc && sc.Hero == h.Id && sc.Token)) E($"{at}: 시작 덱은 기본 3 + 시동 1(정확히 4장) — 지금 {h.Starter.Count}장");
                // 같은 카드 2장은 가벼운 덱의 1코 기본 공격만
                foreach (var g in h.Starter.GroupBy(x => x).Where(g => g.Count() > 1))
                    if (g.Count() > 2 || !(basics.FirstOrDefault(c => c.Id == g.Key) is CardDef dc && dc.Cost == 1 && dc.Fx.Count == 1 && dc.Fx[0].K == FxK.Dmg)) E($"{at}: 시작 덱에 같은 카드 {g.Key} 가 {g.Count()}장 — 같은 카드 2장은 가벼운 덱의 1코 기본 공격만");
                // 3. 고유 4~5장, 완성형 없음 · 모두 신탁 5개(사용자 2026-10-11 — 엘다인 20명 모두 완성형을 뺀다 · 고유 4~5)
                if (uniques.Count < 4 || uniques.Count > 5) E($"{at}: 고유 카드는 4~5장(완성형 없이 모두 신탁 5개) — 지금 {uniques.Count}장");
                foreach (var c in uniques.Where(c => c.Complete)) E($"{at} {c.Id}: 엘다인은 완성형(complete)을 두지 않는다 — 신탁 5개 카드로");
                foreach (var c in uniques.Where(c => !c.Complete && c.Oracles.Count != 5)) E($"{at} {c.Id}: 고유 카드는 신탁 5개 — 지금 {c.Oracles.Count}개");
                // 4. % 단위
                foreach (var c in cards)
                {
                    PctCheck($"{at} {c.Id}", c.Fx);
                    for (int i = 0; i < c.Oracles.Count; i++) PctCheck($"{at} {c.Id} 신탁{i + 1}", c.Oracles[i].Fx);
                    foreach (var b in c.Blesses) PctCheck($"{at} {c.Id} 축복 {b.Name}", b.Fx);
                }
                if (h.Ult != null) PctCheck($"{at} 고학년", h.Ult.Fx);
                var rules = h.Passives.Select(r => ("패시브", r)).Concat(h.AllKeywords.SelectMany(k => k.Rules.Select(r => ("고유 능력", r))))
                    .Concat((h.Forms ?? new List<FormDef>()).SelectMany(fm => (fm.Passives ?? new List<PassiveRule>()).Select(r => ("변신 " + fm.Id, r)))).ToList();
                foreach (var (w, r) in rules) PctCheck($"{at} {w} {r.Name}", r.Fx);
                foreach (var k in h.AllKeywords) foreach (var pp in k.Per.Where(x => x.Stat != "ultCost")) { var b = PctOff(pp.V) ?? PctOff(pp.Ratio); if (b != null) E($"{at} 「{k.Name}」 1개당: % 는 10% 단위(20% 미만은 5%) — {b}"); }
                foreach (var fm in h.Forms ?? new List<FormDef>())
                {
                    foreach (var kv in fm.Mods ?? new Dictionary<string, double>()) { var b = PctOff(kv.Value); if (b != null) E($"{at} 변신 {fm.Id} mods {kv.Key}: % 는 10% 단위 — {b}"); }
                    PctCheck($"{at} 변신 {fm.Id} 풀릴 때", (fm.Off ?? new List<Fx>()).Concat(fm.OffExpire ?? new List<Fx>()).Concat(fm.OffEnd ?? new List<Fx>()));
                }
                // 5. 딜러 실드 금지(기본 「방어 자세」 만 예외)
                if (h.Role == "딜러")
                {
                    void Guard(string w, IEnumerable<Fx> fx) { foreach (var f in WalkAll(fx)) { var g = GuardOf(f); if (g != null && !(g == "끈기" && ENDURE_FREE.Contains(h.Id))) E($"{at} {w}: 딜러는 실드 · 회복 · 피해 감소를 주지 않는다 — {g}"); } }
                    foreach (var c in cards.Where(c => c.Unique || c.Token))
                    {
                        Guard(c.Id, c.Fx);
                        for (int i = 0; i < c.Oracles.Count; i++) Guard($"{c.Id} 신탁{i + 1}", c.Oracles[i].Fx);
                        foreach (var b in c.Blesses) Guard($"{c.Id} 축복 {b.Name}", b.Fx);
                    }
                    if (h.Ult != null) Guard("고학년", h.Ult.Fx);
                    foreach (var (w, r) in rules) Guard($"{w} {r.Name}", r.Fx);
                    foreach (var fm in h.Forms ?? new List<FormDef>())
                    {
                        Guard($"변신 {fm.Id}", (fm.Off ?? new List<Fx>()).Concat(fm.OffExpire ?? new List<Fx>()).Concat(fm.OffEnd ?? new List<Fx>()));
                        if (fm.Mods != null && fm.Mods.TryGetValue("taken", out var tk) && tk < 0) E($"{at} 변신 {fm.Id}: 딜러는 받는 피해 감소를 주지 않는다");
                    }
                    foreach (var k in h.AllKeywords) if (k.Per.Any(pp => (pp.Stat == "taken" && pp.V < 0) || pp.Stat == "hot")) E($"{at} 「{k.Name}」: 딜러는 받는 피해 감소 · 회복을 주지 않는다");
                    foreach (var k in h.AllKeywords.Where(k => k.Guard)) E($"{at} 「{k.Name}」: 딜러는 막는 소환물(guard)을 두지 않는다");
                }
                // 6. 「전부 써서 1개당」 — 사도 수(아래에서 합쳐 센다)
                if (cards.Any(c => SpendAllPer(c.Fx) || c.Oracles.Any(o => SpendAllPer(o.Fx))) || (h.Ult != null && SpendAllPer(h.Ult.Fx)) || rules.Any(x => SpendAllPer(x.r.Fx))) allAll.Add(h.Id);
                // 7. 자동 추가타 — 피해를 내는 자동 규칙(같은 계기는 하나) + 1개당 dot
                var autos = rules.Where(x => HitsRule(x.r)).Select(x => WhenSig(x.r.When)).Distinct().ToList();
                int dots = h.AllKeywords.Count(k => k.Per.Any(pp => pp.Stat == "dot"));
                if (autos.Count + dots > 1) E($"{at}: 자동 피해 · 추가 공격은 사도당 1개 이하 — 지금 {autos.Count + dots}개({string.Join(" · ", rules.Where(x => HitsRule(x.r)).Select(x => x.r.Name).Distinct())}{(dots > 0 ? " · 1개당 지속 피해" : "")})");
                // ▶E62 — 고유 능력 규칙(keyword.rules · passives)은 사도당 1개 이하(엔진은 카드가 돌린다 · tally 는 규칙이 아니다)
                int ruleN = h.Passives.Count + h.AllKeywords.Sum(k => k.Rules.Count);
                // 사용자 2026-10-11 — 엘다인 20명 모두 고유 능력(자동 규칙) 2개씩: 3개 이상이면 오류, 1개 이하면 주의(자동 피해 규칙은 따로 1개 이하)
                if (ruleN < 2) W($"{at}: 고유 능력 규칙은 2개를 권장 — 지금 {ruleN}개");
                if (ruleN > 2) E($"{at}: 고유 능력 규칙은 사도당 2개 이하 — 지금 {ruleN}개({string.Join(" · ", h.Passives.Select(r => r.Name).Concat(h.AllKeywords.SelectMany(k => k.Rules.Select(r => r.Name))))})");
                // ▶E62 — 고유 카드 글(공백 뺀) 45자 이하 · 신탁마다
                foreach (var c in uniques)
                    for (int o = 0; o <= c.Oracles.Count; o++)
                    {
                        var view = d.View(c.Id, o);
                        if (view == null) continue;
                        int len = new string(tx.Card(view).Where(ch => !char.IsWhiteSpace(ch)).ToArray()).Length;
                        if (len > TEXT_MAX) E($"{at} {c.Id}{(o > 0 ? $" 신탁{o}" : "")}: 카드 글이 {len}자 — 공백 뺀 {TEXT_MAX}자 이하");
                    }
                // 겹 자원 주 장치(대충) — 쌓는 자기 고유 효과(순환 · 모드 빼고)를 고유 카드 셋 이상이 다룬다
                foreach (var k in h.AllKeywords.Where(k => (k.Carrier ?? "self") == "self" && !k.Wrap && !k.Mode && k.Cap != null))
                    if (uniques.Count(c => WalkAll(c.Fx).Any(f => f.Id == k.Name && (f.K == FxK.Stack || f.K == FxK.Spend || f.K == FxK.IfStack || f.K == FxK.PerStack))) >= 3) { stackMain.Add(h.Id); break; }
                // E29(3차) — AP 를 주는 신탁은 사도당 2개 이하(우이 빼고) · 0코 + AP 신탁은 소멸
                int apOr = 0;
                foreach (var c in cards)
                    for (int o = 0; o < c.Oracles.Count; o++)
                    {
                        var or = c.Oracles[o];
                        if (!WalkAll(or.Fx).Any(f => (f.K == FxK.Ap || f.K == FxK.NextAp) && f.V > 0)) continue;
                        apOr++;
                        if ((or.Cost ?? c.Cost) == 0 && !or.Tags.Any(t => Tag.Parse(t).id == Tag.Exhaust)) E($"{at} {c.Id} 신탁{o + 1}: 0코 + AP 신탁은 「소멸」 이어야 한다");
                    }
                if (apOr > 2 && !AP_ORACLE_FREE.Contains(h.Id)) E($"{at}: AP 를 주는 신탁은 사도당 2개 이하 — 지금 {apOr}개(우이처럼 AP 가 주 장치인 사도만 예외)");
                // 8. 횟수 제한 없음
                foreach (var (w, r) in rules) if (r.Limit != null && !(r.When?.On == "formLap" && r.Limit.Per == "turn")) E($"{at} {w} {r.Name}: 횟수 제한(limit)을 쓰지 않는다 — 고리는 설계로 막는다(태세 한 바퀴 formLap 의 「한 턴 n번」 만 예외)");
                foreach (var c in cards) foreach (var f in WalkAll(c.Fx.Concat(c.Oracles.SelectMany(o => o.Fx)))) if (f.K == FxK.Power) foreach (var r in f.Rules ?? new List<PassiveRule>()) if (r.Limit != null) E($"{at} {c.Id} 강화 규칙: 횟수 제한(limit)을 쓰지 않는다");
                // 9. 기본 카드를 바꾸지 않는다
                var basicIds = new HashSet<string>(basics.Select(c => c.Id));
                foreach (var fm in h.Forms ?? new List<FormDef>()) if (fm.Cards != null && fm.Cards.Count > 0) E($"{at} 변신 {fm.Id}: 카드 바꿔치기(cards)를 쓰지 않는다 — 변신 카드는 turnMake 로");
                var allFx = cards.SelectMany(c => c.Fx.Concat(c.Oracles.SelectMany(o => o.Fx)).Concat(c.Blesses.SelectMany(b => b.Fx))).Concat(h.Ult?.Fx ?? new List<Fx>()).Concat(rules.SelectMany(x => x.r.Fx));
                foreach (var f in WalkAll(allFx)) if (f.K == FxK.Transform && f.From != null && basicIds.Contains(f.From)) E($"{at}: 기본 카드 「{f.From}」 를 바꾸지 않는다(transform)");
                // 10. 변신 = 매 턴 증발 · 소멸 토큰
                foreach (var fm in h.Forms ?? new List<FormDef>())
                {
                    if (!fm.Stance && (fm.TurnMake == null || fm.TurnMake.Count == 0)) E($"{at} 변신 {fm.Id}: 변신은 매 턴 변신 카드를 만든다 — turnMake 가 없다(태세 stance 는 없어도 된다 — 4차)");
                    foreach (var m in fm.TurnMake ?? new List<FormMake>())
                    {
                        var mc = d.Card(m.Id);
                        if (mc != null && !(mc.Tags.Any(t => Tag.Parse(t).id == Tag.Evaporate) && mc.Tags.Any(t => Tag.Parse(t).id == Tag.Exhaust))) E($"{at} 변신 {fm.Id}: 변신 카드 {m.Id} 는 「증발」 · 「소멸」 태그가 있어야 한다");
                    }
                }
                // 카드 종류와 적 피해(사용자 2026-10-11 — 비비(신성) 「영원한 포옹」): 스킬인데 적을 치면 오류 · 공격인데 적을 안 치면 주의. 신탁은 type · power 를 얹은 모습으로
                foreach (var c in cards)
                    for (int o = 0; o <= c.Oracles.Count; o++)
                    {
                        var view = d.View(c.Id, o);
                        if (view == null) continue;
                        string w = $"{at} {c.Id}{(o > 0 ? $" 신탁{o}" : "")}";
                        bool hits = GameData.HitsFoe(view.Fx);
                        if (view.Type == "스킬" && hits) E($"{w}: 스킬인데 적에게 피해를 준다 — 적을 치면 공격 카드로(type 공격 · 신탁이면 type 「공격」)");
                        else if (view.Type == "공격" && !hits && !WalkAll(view.Fx).Any(f => f.K == FxK.Summon)) E($"{w}: 공격인데 적 피해가 없다 — 스킬로(신탁이면 type 「스킬」)");   // 사용자 2026-10-11 — 주의에서 오류로
                    }
                // ▶E86(4차) — 소유 키워드: 남의 키워드를 정산하면 주의(도발은 비비만 — D13)
                foreach (var g in OwnedMisuse(h, cards).GroupBy(x => x.kw))
                    W($"{at}: {Ko.J("「" + g.Key + "」", "은는")} {d.Hero(OWNED_KW[g.Key])?.Name ?? OWNED_KW[g.Key]}의 소유 키워드 — 정산 조각은 주인만({string.Join(" · ", g.Select(x => x.loc).Distinct().Take(4))})");
                // 11. 고유 효과 이름 — 다른 사도 이름 금지
                foreach (var nm in h.AllKeywords.Select(k => k.Name).Concat((h.Forms ?? new List<FormDef>()).Select(fm => fm.Name)).Where(x => x != null))   // 변신 · 태세 이름도(엘다인 4차)
                    foreach (var (oid, on) in names)
                        if (oid != h.Id && on != h.Name && on.Length >= 2 && nm.Contains(on)) E($"{at}: 고유 효과 · 태세 「{nm}」 에 다른 사도 이름 「{on}」 이 들어 있다");
            }
            if (allAll.Count > ALL_MAX) E($"엘다인: 「전부 써서 1개당」 사도는 {ALL_MAX}명까지(카제나 재정렬) — 지금 {allAll.Count}명({string.Join(" · ", allAll)})");
            // 겹 자원(쌓아서 쓰기)이 주 장치인 사도 — 4명 이하(▶E62). 자동 판정은 대충이라 주의: 「쌓는 고유 효과를 고유 카드 셋 이상이 쌓고 · 쓰고 · 문턱으로 읽는다」
            if (stackMain.Count > STACK_MAIN_MAX) W($"엘다인: 겹 자원이 주 장치인 사도로 보이는 것이 {stackMain.Count}명 — {STACK_MAIN_MAX}명 이하(카제나 분포, 05 §3) — {string.Join(" · ", stackMain)}");
        }

        // ── 사도 ───────────────────────────────────────────────────────
        /// <summary>클론 전용 사도(cloneHeroes — 2026-10-11) — 보스 클론이 쓰는 이름 · 종족 · 성격 · 성급 · 고학년만 본다. 플레이어 사도와 같은 id 면 오류(되살릴 때 클론 쪽 줄을 지운다).</summary>
        void CloneHero(HeroDef h)
        {
            string at = $"클론 사도 {h.Id}{d.At("cloneHero", h.Id)}";
            if (d.Heroes.ContainsKey(h.Id)) E($"{at}: 플레이어 사도(heroes)에도 있다 — 되살린 사도는 cloneHeroes 에서 지운다");
            if (string.IsNullOrEmpty(h.Name) || string.IsNullOrEmpty(h.Race) || string.IsNullOrEmpty(h.Nature)) E($"{at}: name · race · nature 가 있어야 한다");
            if (h.Star < 1 || h.Star > 3) E($"{at}: star 는 1~3");
            if (h.Starter.Count > 0 || h.Passives.Count > 0) W($"{at}: 클론 전용 사도는 starter · passives 를 쓰지 않는다");
            if (!d.Enemies.Values.Any(e => e.Clone == h.Id)) W($"{at}: 이 사도의 클론 적(clone)이 없다");
        }

        void Hero(HeroDef h)
        {
            string at = $"사도 {h.Id}{d.At("hero", h.Id)}";
            if (string.IsNullOrEmpty(h.Name)) E($"{at}: name 이 없다");
            if (Array.IndexOf(R.ROLES, h.Role) < 0) E($"{at}: role 은 탱커 · 서포터 · 딜러");
            if (h.Nature != null && Array.IndexOf(R.NATURES, h.Nature) < 0) E($"{at}: 모르는 성격 {h.Nature}");
            if (h.HadRow) W($"{at}: row 는 이제 쓰지 않는다(사도 열은 없다 — 맞는 모습은 편성 순서) — 지워라");
            // 갈래(18갈래 v2) — 이름 확인 · 쌓고 고르기 밖에서 「다 차면 저절로 터짐」(stackReach · stackOver · onMax)은 주의
            if (h.Style != null && !STYLES.Contains(h.Style)) E($"{at}: 모르는 갈래 style 「{h.Style}」 — {string.Join(" · ", STYLES)}");
            // 1단계(2026-10-08 사용자) — 쌓고 고르기도 센다. 다 차면 「카드 만들기 · 다음 카드 강화」(make · empower · onMax make/empower)로 바꾼 것은 터짐이 아니다
            if (h.Style != null && AutoBurst(h)) W($"{at}: {h.Style} 인데 키워드가 다 차면 저절로 터진다(stackReach · stackOver · onMax 변신) — 「다 차면 카드 만들기(make) · 다음 카드 강화(empower)」 로 바꾼다");
            if (h.Hp <= 0 || h.Atk <= 0) E($"{at}: hp · atk 가 없다");
            foreach (var id in h.Starter) { var c = d.Card(id); if (c == null) E($"{at}: 없는 시작 카드 {id}"); else if (c.Hero != h.Id) E($"{at}: 시작 카드 {id} 의 주인이 다르다"); }
            if (h.Starter.Count == 0) W($"{at}: 시작 카드가 없다");
            if (h.StartForm != null && !(h.Forms?.Any(x => x.Id == h.StartForm) ?? false)) E($"{at}: startForm 은 이 사도의 변신 · 태세 id — 「{h.StartForm}」");
            if (h.Ult != null) { if (h.Ult.Cost <= 0 || h.Ult.Cost > R.GAUGE_MAX) E($"{at}: 고학년 cost 는 1~{R.GAUGE_MAX}"); FxList(at + " 고학년", h.Ult.Fx); }
            if (!h.AllKeywords.Any()) W($"{at}: 고유 효과(keyword)가 없다 — 사도마다 자기 고유 효과를 하나씩");
            foreach (var k in h.AllKeywords)
            {
                if (string.IsNullOrEmpty(k.Name)) E($"{at}: 키워드 name 이 없다");
                else if (R.ALL_ST.Contains(k.Name) || R.IsCardSt(k.Name) || Array.IndexOf(Tag.All, k.Name) >= 0) E($"{at}: 고유 효과 이름 「{k.Name}」 이 엔진 키워드(상태 · 태그)와 같다 — 새 이름으로");
                else if (!Used(h, k.Name)) W($"{at}: 고유 효과 「{k.Name}」 이 어디에도 안 쓰인다(카드 · 패시브 · 고학년 · 규칙의 stack · spend · ifStack · perStack · payWith …)");
                if (!CARRIERS.Contains(k.Carrier)) E($"{at}: 키워드 carrier 는 self · enemy · ally · hero");
                if (k.Wrap && k.Cap == null) E($"{at}: wrap(순환)은 cap 이 있어야 한다");
                if (k.Stages != null && (k.Cap == null || k.Stages.Count != k.Cap || k.Stages.Any(string.IsNullOrWhiteSpace))) E($"{at}: 「{k.Name}」 stages(단계 이름)는 cap 과 개수가 같아야 한다");
                if (k.Weakens && k.Carrier != "enemy") E($"{at}: weakens 는 적에게 거는 표식(carrier enemy)에만");
                if (k.Guard && k.Carrier != "self" && k.Carrier != "hero") E($"{at}: guard(소환물)는 carrier self · hero 에만");
                if (k.Cut > 0 && (!k.Guard || k.Cut > 1)) E($"{at}: cut 은 guard 와 같이(0~1)");
                if (k.Uses != 0 && (!k.Guard || k.Uses < 1 || k.Uses > 5)) E($"{at}: uses 는 guard 와 같이(1~5 — 소환물 하나가 받는 대 수)");
                if (k.TagWhile != null && Array.IndexOf(Tag.All, k.TagWhile) < 0) E($"{at}: tagWhile 은 태그 이름 — 「{k.TagWhile}」");
                if (k.Spread && k.Carrier != "self") E($"{at}: spread 는 carrier self 에만");
                if (k.Tally != null && (k.Carrier ?? "self") != "self") E($"{at}: tally(엔진이 세는 수)는 carrier self 에만");
                if (k.Share != 0 && (k.Carrier != "hero" || k.Share < 0 || k.Share >= 1)) E($"{at}: 「{k.Name}」 share 는 아군 표시(carrier hero)에 0~1");
                if (k.OnStage != null) { if (!k.Wrap || k.Cap == null || k.OnStage.Count > k.Cap) E($"{at}: 「{k.Name}」 onStage 는 순환(wrap · cap) 고유 효과에 cap 개까지"); else foreach (var (sf, si) in k.OnStage.Select((x, i) => (x, i))) if (sf != null && sf.Count > 0) FxList($"{at} 「{k.Name}」 칸{si + 1}", sf); }
                foreach (var sb in k.StageBonus ?? new List<StageBonus>())
                {
                    if (k.Cap == null || sb.Stage < 0 || sb.Stage > k.Cap || (sb.Stage == 0 && k.StageBonus.Count > k.Cap)) E($"{at}: 「{k.Name}」 stageBonus 의 stage 는 1~cap(없으면 목록 차례)");
                    if (sb.Ratio == 0 && (sb.Fx == null || sb.Fx.Count == 0) && (sb.Tags == null || sb.Tags.Count == 0)) E($"{at}: 「{k.Name}」 stageBonus 에 ratio · fx · tags 가운데 하나");
                    if (sb.Fx != null && sb.Fx.Count > 0) FxList($"{at} 「{k.Name}」 단계 덤", sb.Fx);
                    Tags($"{at} 「{k.Name}」 단계 덤", sb.Tags);
                }
                if (k.CardValue != null)
                {   // 카드 값 키워드(엘다인 3차 · 4차 리뉴아 「시간」)
                    var cv = k.CardValue;
                    if (cv.Grow != null && cv.Grow != "handEnd" && cv.Grow != "allyPlay") E($"{at}: 「{k.Name}」 cardValue.grow 는 handEnd · allyPlay — 「{cv.Grow}」");
                    if (cv.Reset != null && cv.Reset != "play") E($"{at}: 「{k.Name}」 cardValue.reset 는 play — 「{cv.Reset}」");
                    if (cv.Who != null && (cv.Who != "any" && cv.Who != "other" || cv.Grow != "allyPlay")) E($"{at}: 「{k.Name}」 cardValue.who 는 allyPlay 에 any · other");
                    if (cv.Max < 0) E($"{at}: 「{k.Name}」 cardValue.max 는 0 이상");
                    if (cv.Cost > 0) W($"{at}: 「{k.Name}」 cardValue.cost 가 양수 — 값 1당 비용이 오른다");
                }
                if (k.Start != null && (k.Start < 0 || (k.Cap != null && k.Start > k.Cap) || (k.Carrier ?? "self") != "self")) E($"{at}: 「{k.Name}」 start 는 0~cap(carrier self)");
                foreach (var tl in k.Tally ?? new List<string>()) if (tl != "play" && tl != "discardBy") E($"{at}: tally 는 play · discardBy — 「{tl}」");
                foreach (var p in k.Per) { if (p.From != null && ((p.From != "owner" && p.From != "marked") || k.Carrier != "enemy" || (p.From == "marked" && (p.Mark == null || d.CarrierOf(p.Mark) != "hero")))) E($"{at}: per.from 은 owner · marked(+ mark = 아군 표시) — 적 표식에만"); if (p.Stat == "tough" && k.Carrier != "enemy") E($"{at}: per stat tough 는 적 표식에만"); }
                foreach (var p in k.Per) if (!PER_STAT.Contains(p.Stat)) E($"{at}: 키워드 1개당 — 모르는 stat {p.Stat}");
                foreach (var p in k.Per)
                {   // 엘다인 4차 ▶E81 · E78
                    if (p.Who == "marked" && (p.Mark == null || d.CarrierOf(p.Mark) != "hero")) E($"{at}: per who marked 의 mark 는 아군 표시(carrier hero) 고유 효과 이름 — 「{p.Mark}」");
                    if (p.Who != null && p.Who != "holder" && p.Who != "allies" && p.Who != "marked") E($"{at}: per who 는 holder · allies · marked");
                    if (p.Mark != null && p.Who != "marked" && p.From != "marked") E($"{at}: per mark 는 who marked · from marked 와 같이");
                    if (p.Stat == "ultCost" && p.V >= 0) W($"{at}: per stat ultCost 는 보통 음수(1개당 고학년 요구 게이지 -비율)");
                }
                if (k.Reserve && !k.Decays) W($"{at}: 예약 키워드는 「적의 차례가 끝나면 N 감소」 와 같이 쓴다");
                if (k.Burn != null && (k.Carrier != "hero" || k.Burn.On != "play" || k.Burn.Shield < 0 || k.Burn.Heal < 0)) E($"{at}: 「{k.Name}」 burn 은 아군 표시(carrier hero)에 on play · shield · heal 0 이상");
                if (k.HuntCarry != null && (k.HuntCarry != "lowEnemy" || !k.Hunt || k.Carrier != "enemy")) E($"{at}: 「{k.Name}」 huntCarry 는 적 사냥감(hunt · carrier enemy)에 lowEnemy");
                if (k.Hunt && k.Carrier != "enemy" && k.Carrier != "hero") E($"{at}: 찍기(hunt)는 적에게 거는 표식(carrier enemy) · 아군 표시(carrier hero)에만");
                if (k.Per.Where(p => p.Stat != "dot" && p.Stat != "hot" && p.Stat != "ultCost").Any(p => Math.Abs(p.V) * (k.Cap ?? 1) > (k.Consumes ? 1.0 : 0.6) + 1e-9))
                    W($"{at}: 키워드 1개당 % × 최대 겹이 상한(+60%, 발동하면 사라지면 +100%)을 넘는다");
                foreach (var r in k.Rules) Rule($"{at} 키워드 규칙", r);
            }
            for (int i = 0; i < h.Passives.Count; i++) Rule($"{at} 패시브[{i}] {h.Passives[i].Name}", h.Passives[i]);
            if (h.Passives.Select(p => p.Name).Distinct().Count() > 2) W($"{at}: 패시브는 둘까지(docs/18 §3)");
            foreach (var f in h.Forms ?? new List<FormDef>()) Form(h, f, at);
            // 그 사도의 고학년 · 카드가 부르는 변신은 그 사도의 것이어야 한다
            var mine = new HashSet<string>((h.Forms ?? new List<FormDef>()).Select(f => f.Id).Where(x => x != null));
            IEnumerable<Fx> Walk(IEnumerable<Fx> fx) => (fx ?? Enumerable.Empty<Fx>()).SelectMany(f => new[] { f }.Concat(Walk(f.Then)).Concat(Walk(f.Else)));
            var calls = Walk(h.Ult?.Fx).Concat(h.Passives.SelectMany(r => Walk(r.Fx)))
                .Concat(d.Cards.Values.Where(c => c.Hero == h.Id).SelectMany(c => Walk(c.Fx).Concat(c.Oracles.SelectMany(o => Walk(o.Fx))).Concat(c.Blesses.SelectMany(b => Walk(b.Fx)))))
                .Where(f => f.K == FxK.Form).Select(f => f.Id)
                .Concat(h.AllKeywords.Where(k => k.OnMax != null).Select(k => k.OnMax.Form)).ToList();
            foreach (var k in h.AllKeywords.Where(k => k.OnMax != null))
            {
                var om = k.OnMax;
                int kinds = (om.Form != null ? 1 : 0) + (om.Make != null ? 1 : 0) + (om.Empower != null ? 1 : 0);
                if (kinds != 1) E($"{at}: 「{k.Name}」 onMax 는 form · make · empower 가운데 하나만");
                if (om.Make != null && d.Card(om.Make) == null) E($"{at}: 「{k.Name}」 onMax make — 없는 카드 「{om.Make}」");
                if (om.Empower != null && om.Empower != "next" && om.Empower != "any") E($"{at}: 「{k.Name}」 onMax empower 는 next(그 사도의 다음 카드) · any(파티의 다음 카드)");
                if (om.Empower != null && !(om.Ratio > 0 && om.Ratio <= 3)) E($"{at}: 「{k.Name}」 onMax empower 의 ratio 는 0~3");
                if (om.Make == null && om.N != 0) E($"{at}: 「{k.Name}」 onMax n 은 make 와만");
                if (om.Make != null && !om.Consume) W($"{at}: 「{k.Name}」 onMax make — consume 이 없으면 최대에 머물러 다시 만들지 않는다(다시 차려면 겹이 줄어야)");
                if (k.Cap == null || k.Cap <= 0 || k.Mode || k.Wrap) E($"{at}: 「{k.Name}」 onMax 는 최대(cap)가 있는 쌓이는 고유 효과에만(mode · wrap 아님)");
                if ((k.Carrier ?? "self") != "self") E($"{at}: 「{k.Name}」 onMax 는 carrier self 에만");
                if (k.Consumes) W($"{at}: 「{k.Name}」 onMax — 발동하면 사라지는 고유 효과는 최대까지 잘 안 찬다");
            }
            foreach (var id in calls.Distinct()) if (id != null && d.Form(id) != null && !mine.Contains(id)) E($"{at}: 다른 사도의 변신 「{id}」 을 부른다 — 변신은 제 것만");
            foreach (var id in mine) if (!calls.Contains(id)) W($"{at}: 변신 「{id}」 을 부르는 효과(form)가 없다 — 고학년에 {{ k: \"form\", id }} 로");
            // 값어치 — 변신을 든 고학년이 다른 고학년들보다 크게 세거나 약하지 않게(변신 값은 CardValue.FormValue)
            if (h.Ult != null && h.Ult.Fx.Any(f => f.K == FxK.Form))
            {
                double mineV = UltWorth(h);
                var others = d.Heroes.Values.Where(x => x.Ult != null && !x.Ult.Fx.Any(f => f.K == FxK.Form)).Select(x => CardValue.ValueOf(x.Ult.Fx)).OrderBy(x => x).ToList();
                if (others.Count >= 5)
                {
                    double med = others[others.Count / 2], top = others[others.Count * 9 / 10];
                    if (mineV > top * 1.15) W($"{at}: 변신을 든 고학년 값어치 {mineV:0.00} — 다른 고학년 상위 10%({top:0.00})의 1.15배를 넘는다(너무 세다)");
                    if (mineV < med * 1.15) W($"{at}: 변신을 든 고학년 값어치 {mineV:0.00} — 다른 고학년 가운데값({med:0.00})의 1.15배보다 낮다(변신하는 만큼 세야)");
                }
            }
        }

        /// <summary>고학년 값어치 — 변신(form)은 CardValue.FormValue 로 센다.</summary>
        double UltWorth(HeroDef h) =>
            CardValue.ValueOf(h.Ult.Fx.Where(f => f.K != FxK.Form).ToList()) + h.Ult.Fx.Where(f => f.K == FxK.Form).Sum(f => CardValue.FormValue(d.Form(f.Id), d, h.Id));

        static readonly HashSet<string> FORM_STATS = new() { "dealt", "taken", "atk", "def", "crit", "guard", "heal" };

        /// <summary>변신 하나 — 이름 · 지속 · 풀리는 계기 · 능력치 · 카드 바꾸기(값어치 1.15~1.8배) · 덤 · 패시브 · 풀릴 때.</summary>
        void Form(HeroDef h, FormDef f, string at0)
        {
            string at = $"{at0} 변신 {f.Id}";
            if (string.IsNullOrEmpty(f.Id)) E($"{at}: id 가 없다");
            if (string.IsNullOrEmpty(f.Name)) E($"{at}: name 이 없다");
            else if (keywords.Contains(f.Name) || R.ALL_ST.Contains(f.Name) || R.IsCardSt(f.Name) || Array.IndexOf(Tag.All, f.Name) >= 0) E($"{at}: 이름 「{f.Name}」 이 고유 효과 · 상태 · 태그와 같다 — 칩 설명이 겹친다");
            if (f.Turns < 0) E($"{at}: turns 는 0(전투 끝까지) 이상");
            if (f.Toggle != null && (!f.Stance || !(h.Forms?.Any(x => x.Id == f.Toggle && x.Stance && x.Id != f.Id) ?? false))) E($"{at}: toggle 은 태세(stance)끼리 — 짝 태세 id 「{f.Toggle}」");
            if (f.Stance && f.Turns == 0 && f.Until == null && f.Toggle == null) W($"{at}: 태세에 풀리는 계기(until)도 지속(turns)도 없다 — 전투 끝까지 안 풀린다");
            if (f.Turns > 6) W($"{at}: turns {f.Turns} — 길면 0(전투 끝까지)으로");
            if (f.Until != null)
            {
                if (f.Until.On == null || !WHEN_ON.Contains(f.Until.On) || f.Until.On == "always") E($"{at}: until 의 on 이 맞지 않다 — 「{f.Until.On}」");
                if ((f.Until.On == "stackReach" || f.Until.On == "stackGone") && (f.Until.Id == null || !keywords.Contains(f.Until.Id))) E($"{at}: until 키워드가 아니다 — 「{f.Until.Id}」");
            }
            foreach (var kv in f.Mods ?? new Dictionary<string, double>())
            {
                if (!FORM_STATS.Contains(kv.Key)) E($"{at}: mods 의 능력치는 {string.Join(" · ", FORM_STATS)} — 「{kv.Key}」");
                if (kv.Value == 0 || Math.Abs(kv.Value) > 1) E($"{at}: mods {kv.Key} 는 비율(0.2 = +20%, 1 이하)");
            }
            foreach (var kv in f.Cards ?? new Dictionary<string, string>())
            {
                var a = d.Card(kv.Key); var b = d.Card(kv.Value);
                if (a == null || a.Hero != h.Id) { E($"{at}: cards 의 「{kv.Key}」 는 이 사도의 카드가 아니다"); continue; }
                if (b == null) { E($"{at}: 변신판 카드가 없다 — {kv.Value}"); continue; }
                if (b.Hero != h.Id) E($"{at}: 변신판 카드 {kv.Value} 의 주인이 다르다");
                if (!b.Token) E($"{at}: 변신판 카드 {kv.Value} 는 token(덱에 안 드는 카드)이어야 한다");
                // 값어치 — 변신판은 신탁처럼 코스트 기준 1.15배 이상, 1.8배 이하(너무 세지 않게)
                var va = d.View(a.Id); var vb = d.View(b.Id);
                double r = (CardValue.CardWorth(vb) / CardValue.BaseValue(vb.X ? 3 : vb.Cost)) / (Math.Max(0.05, CardValue.CardWorth(va)) / CardValue.BaseValue(va.X ? 3 : va.Cost));
                if (r < 1.15) W($"{at}: 변신판 「{b.Name}」 이 「{a.Name}」 보다 낫지 않다(코스트 기준 {r:0.00}배 · 1.15배 이상)");
                if (r > 1.8) W($"{at}: 변신판 「{b.Name}」 이 「{a.Name}」 의 {r:0.00}배 — 1.8배 이하로");
            }
            for (int i = 0; i < (f.Bonus?.Count ?? 0); i++)
            {
                var b = f.Bonus[i]; string bt = $"{at} bonus[{i}]";
                if (b.Card != null && d.Card(b.Card)?.Hero != h.Id) E($"{bt}: card 는 이 사도의 카드 id — 「{b.Card}」");
                if (b.Type != null && Array.IndexOf(R.CARD_TYPES, b.Type) < 0) E($"{bt}: 모르는 type {b.Type}");
                if (b.Tag != null && Array.IndexOf(Tag.All, b.Tag) < 0) E($"{bt}: 모르는 태그 「{b.Tag}」");
                if (b.Ratio < 0 || b.Ratio > 2) E($"{bt}: ratio 는 피해 배율(1.2 = ×1.2, 2 이하)");
                if (b.Ratio == 0 && (b.Fx == null || b.Fx.Count == 0) && (b.Tags == null || b.Tags.Count == 0)) E($"{bt}: ratio · fx · tags 가운데 하나는 있어야 한다");
                Tags(bt, b.Tags);
                if (b.Fx != null && b.Fx.Count > 0) FxList(bt, b.Fx);
            }
            for (int i = 0; i < (f.Passives?.Count ?? 0); i++)
            {
                var r = f.Passives[i];
                if (r.When?.On == "always") E($"{at} 패시브[{i}]: 변신의 「항상」 은 mods 로");
                Rule($"{at} 패시브[{i}] {r.Name}", r);
            }
            if (f.OnEnter != null && f.OnEnter.Count > 0) FxList($"{at} 들 때", f.OnEnter);
            foreach (var (off, nm) in new[] { (f.Off, "풀릴 때"), (f.OffExpire, "시간이 다 되면"), (f.OffEnd, "스스로 끝내면") })
            {
                if (off != null && off.Count > 0) FxList($"{at} {nm}", off);
                if (off != null && off.Any(x => x.K == FxK.Form || x.K == FxK.FormEnd)) E($"{at}: {nm}(off)에 form · formEnd 를 쓰지 않는다");
            }
            foreach (var m in f.TurnMake ?? new List<FormMake>())
            {
                var mc = d.Card(m.Id);
                if (mc == null) { E($"{at}: turnMake — 없는 카드 「{m.Id}」"); continue; }
                if (mc.Hero != h.Id) E($"{at}: turnMake 카드 {m.Id} 의 주인이 다르다");
                if (!mc.Token) E($"{at}: turnMake 카드 {m.Id} 는 token(덱에 안 드는 카드)이어야 한다");
                if (m.N < 0) E($"{at}: turnMake n 은 1 이상");
                if (m.When != null && (m.When.Stack == null || !keywords.Contains(m.When.Stack) || m.When.N <= 0)) E($"{at}: turnMake when 은 stack(고유 효과 이름) · n — 「{m.When.Stack}」");
            }
            if (f.Mods == null && f.Cards == null && (f.Bonus == null || f.Bonus.Count == 0) && (f.Passives == null || f.Passives.Count == 0) && (f.TurnMake == null || f.TurnMake.Count == 0)) W($"{at}: 바뀌는 것이 없다(mods · cards · bonus · passives · turnMake)");
        }

        /// <summary>그 사도의 고유 효과가 데이터 어딘가에서 쓰이나(stack · spend · ifStack · perStack · xStack · payWith · marked · 규칙의 id).</summary>
        bool Used(HeroDef h, string name)
        {
            bool InFx(IEnumerable<Fx> fx) => fx != null && fx.Any(f => (f.Id == name && (f.K == FxK.Stack || f.K == FxK.Spend || f.K == FxK.IfStack || f.K == FxK.PerStack || f.K == FxK.CardStatus || f.K == FxK.PerCardSt)) || f.XStack == name || f.K == FxK.Feed || InFx(f.Then) || (f.Rules != null && f.Rules.Any(r => InFx(r.Fx) || r.When.Id == name || r.When.Marked == name || r.Conds.Any(c => c.Id == name))));
            bool InRules(IEnumerable<PassiveRule> rs) => rs != null && rs.Any(r => InFx(r.Fx) || r.When.Id == name || r.When.Marked == name || r.Conds.Any(c => c.Id == name));
            var cards = d.Cards.Values.Where(c => c.Hero == h.Id).ToList();
            return cards.Any(c => InFx(c.Fx) || c.PayWith == name || c.Oracles.Any(o => InFx(o.Fx)) || c.Blesses.Any(b => InFx(b.Fx)))
                || InRules(h.Passives) || (h.Ult != null && InFx(h.Ult.Fx)) || h.AllKeywords.Any(k => InRules(k.Rules))
                || d.Equips.Values.Any(e => InRules(e.Effect) || InRules(e.AffinityEffect));
        }

        // ── 카드 ───────────────────────────────────────────────────────
        void Card(CardDef c)
        {
            // (엘다인 4차 태세) 그 변신 · 태세 중에만 사용 가능 — 그 사도의 변신 id 또는 「*」
            foreach (var (nf, w) in new[] { (c.NeedForm, "") }.Concat(c.Oracles.Select((o, i) => (o.NeedForm, $" 신탁{i + 1}"))))
                if (nf != null && nf != "*" && (c.Hero == null || !(d.Hero(c.Hero)?.Forms?.Any(x => x.Id == nf) ?? false))) E($"카드 {c.Id}{w}: needForm 은 그 사도의 변신 · 태세 id(또는 「*」) — 「{nf}」");
            string at = $"카드 {c.Id}{d.At("card", c.Id)}";
            if (string.IsNullOrEmpty(c.Name)) E($"{at}: name 이 없다");
            if (Array.IndexOf(R.CARD_TYPES, c.Type) < 0) E($"{at}: type 은 공격 · 스킬 · 강화 · 상태 · 저주");
            if (c.Cost < 0) E($"{at}: cost 가 음수");
            if (c.Hero != null && d.Hero(c.Hero) == null) E($"{at}: 없는 사도 {c.Hero}");
            if (c.Id.EndsWith(GameData.PLAIN) || c.Id.EndsWith(GameData.COPY)) E($"{at}: id 끝에 ~ · ^ 를 쓰지 않는다(엔진이 쓴다)");
            if (c.Id.Contains(GameData.OWNER)) E($"{at}: id 에 @ 를 쓰지 않는다(교주 카드 주인 표시)");
            Tags(at, c.Tags);
            powerOk = c.Type == "강화";
            FxList(at, c.Fx);
            if (c.Fx.Count == 0 && !c.Tags.Contains(Tag.Unplayable) && c.Type != "저주" && c.Type != "상태") W($"{at}: 효과가 없다");
            // 연쇄 상한(1단계) — 0코로 태우고(burn · exileFrom) 뽑거나 꺼내는 카드는 「소멸」 을 단다(소멸 더미 되살리기와 도는 0코 순환)
            if (c.Cost == 0 && !c.X && c.Fx.Any(f => f.K == FxK.Burn || f.K == FxK.ExileFrom) && c.Fx.Any(f => f.K == FxK.Draw || f.K == FxK.Pull) && !c.Tags.Any(t => Tag.Parse(t).id == Tag.Exhaust || Tag.Parse(t).id == Tag.Evaporate))
                W($"{at}: 0코 태우기 + 뽑기 카드는 「소멸」 태그를 단다(소멸 더미 되살리기와 0코 순환이 된다)");
            if (c.Fx.Any(f => f.XHits) && !c.X) E($"{at}: xHits 는 X 코스트 카드에만");
            if (c.Oracles.Count != 0 && c.Oracles.Count != 5) E($"{at}: 신탁은 다섯(①~⑤) — 지금 {c.Oracles.Count}");
            if (c.Unique && c.Hero != null && c.Oracles.Count == 0 && !c.Complete) W($"{at}: 고유 카드에 신탁이 없다(완성형이면 complete)");
            if (c.Complete && c.Oracles.Count > 0) E($"{at}: 완성형(complete)은 신탁이 없어야 한다");
            if (c.CountAs < 0) E($"{at}: countAs 는 0 이상");
            for (int i = 0; i < c.Oracles.Count; i++) { var o = c.Oracles[i]; if (o.Type != null && o.Type != "공격" && o.Type != "스킬" && o.Type != "강화") E($"{at} 신탁{i + 1}: type 은 공격 · 스킬 · 강화"); if (o.Type == "강화" && !o.Power) W($"{at} 신탁{i + 1}: 강화 카드로 바꾸는 신탁은 power: true 로"); Tags($"{at} 신탁{i + 1}", o.Tags); powerOk = c.Type == "강화" || o.Power; FxList($"{at} 신탁{i + 1}", o.Fx); if (string.IsNullOrEmpty(o.Name)) E($"{at} 신탁{i + 1}: name 이 없다"); }
            powerOk = false;
            // 강화 카드 = 낼 때 받는 작은 효과(선택) + 전투 동안 남는 효과(필수)
            if (c.Type == "강화" && c.Hero != null && c.Unique)
            {
                if (!HasLasting(c.Fx)) W($"{at}: 강화 카드에 전투 동안 남는 효과가 없다 — power 규칙 · 세기형 버프 · 전투 내내 증감 가운데 하나");
                for (int i = 0; i < c.Oracles.Count; i++) if (!HasLasting(c.Oracles[i].Fx)) W($"{at} 신탁{i + 1}: 강화 카드에 전투 동안 남는 효과가 없다");
            }
            if (c.Oracles.Count == 5) OracleRules(c);
            if (c.Blesses.Count > 3) E($"{at}: 축복은 셋까지");
            foreach (var b in c.Blesses)
            {
                if (b.Kind != null && !BLESS_KIND.Contains(b.Kind)) E($"{at} 축복 {b.Name}: 모르는 kind {b.Kind}");
                if (b.Tags.Any(t => Tag.Parse(t).id == Tag.Exhaust)) E($"{at} 축복 {b.Name}: 축복에 소멸을 달지 않는다");
                Tags($"{at} 축복", b.Tags); FxList($"{at} 축복 {b.Name}", b.Fx);
            }
            if (c.Neutral && (c.Grade == null || !R.SHOP_GRADE_WEIGHT.ContainsKey(c.Grade))) E($"{at}: 교주 카드는 grade(일반 · 고급 · 희귀 · 전설)가 있어야 한다");
            if (c.Neutral && c.Price <= 0) E($"{at}: 교주 카드는 price 가 있어야 한다");
            // 사도 고유 효과 틀 · 키워드 사전
            if (c.Token && c.Hero == null) E($"{at}: token(사도 전용 생성 카드)은 hero 가 있어야 한다");
            if (c.Token && c.Unique) E($"{at}: token 은 고유 카드가 아니다(unique 빼기)");
            if (c.Token && d.Heroes.Values.Any(h => h.Starter.Contains(c.Id))) E($"{at}: token 은 시작 덱에 넣지 않는다");
            if (c.Evolve != null && (c.Evolve.Into == null || d.Card(c.Evolve.Into) == null)) E($"{at}: evolve.into 카드가 없다");
            if (c.Evolve != null && c.Evolve.N < 2) E($"{at}: evolve.n 은 2 이상");
            if (c.BondCard != null && d.Card(c.BondCard) == null) E($"{at}: bondCard(강해진 카드)가 없다 — {c.BondCard}");
            if (c.Tags.Contains(Tag.Bond) && c.BondCard == null) W($"{at}: 결속 카드에 bondCard(겹친 수 3 이상의 강해진 카드)가 없다");
            if (c.Tags.Contains(Tag.Bond) && c.Hero == null) E($"{at}: 결속은 사도 카드에만");
            if (c.Becomes != null && d.Card(c.Becomes) == null) E($"{at}: becomes(금기 카드)가 없다 — {c.Becomes}");
            if (c.Tags.Contains(Tag.SealedTaboo) && c.Becomes == null) E($"{at}: 봉인된 금기는 becomes(보스를 처치하면 바뀔 금기 카드)가 있어야 한다");
            if (c.PayRate < 0) E($"{at}: payRate 는 1 이상");
            if (c.PayWith != null && (c.Hero == null || !d.Hero(c.Hero)?.AllKeywords.Any(k => k.Name == c.PayWith && (k.Carrier ?? "self") == "self") == true)) E($"{at}: payWith 는 그 사도의 고유 효과(carrier self) 이름 — 「{c.PayWith}」");
            if (c.Choices != null && c.Choices.Count != 2) E($"{at}: choices 는 갈래 이름 둘");
            if (c.Choices != null && !c.Fx.Any(f => f.K == FxK.IfChoice)) W($"{at}: choices 가 있는데 효과에 ifChoice 가 없다");
        }

        /// <summary>
        /// 신탁 규칙(docs/18 §3) — ① 코스트 기준 값어치가 기본의 1.15배 이상 ② 코스트를 올렸으면 값어치 합 1.6배 이상
        /// ③ 기본에 없는 소멸은 2배 넘는 한 방에만 · 코스트를 내린 신탁엔 안 붙인다 · 카드당 하나. 어기면 주의(값어치는 대충의 셈이다).
        /// </summary>
        void OracleRules(CardDef c)
        {
            var b = d.View(c.Id);
            double vb = Math.Max(0.05, CardValue.CardWorth(b));
            int gones = 0;
            for (int n = 1; n <= 5; n++)
            {
                var o = d.View(c.Id, n);
                double vo = CardValue.CardWorth(o);
                double r = (vo / CardValue.BaseValue(o.X ? 3 : o.Cost)) / (vb / CardValue.BaseValue(b.X ? 3 : b.Cost));
                string at = $"카드 {c.Id}{d.At("card", c.Id)} 신탁{n} 「{c.Oracles[n - 1].Name}」";
                if (r < 1.15) W($"{at}: 기본보다 낫지 않다(코스트 기준 {r:0.00}배 · 1.15배 이상)");
                if (o.Cost > b.Cost && vo / vb < 1.6) W($"{at}: 코스트를 올렸으면 값어치가 기본의 1.6배 이상(지금 {vo / vb:0.00}배)");
                bool gone = o.TagN(Tag.Exhaust) == 0 && b.TagN(Tag.Exhaust) != 0;
                if (gone) { gones++; if (o.Cost < b.Cost) W($"{at}: 코스트를 내린 신탁에 소멸을 붙이지 않는다"); else if (vo / 0.7 / vb < 2) W($"{at}: 기본에 없는 소멸은 2배 넘는 한 방에만"); }
            }
            if (gones > 1) W($"카드 {c.Id}: 소멸 신탁이 {gones}개 — 카드당 하나까지");
        }

        // ── 적 ─────────────────────────────────────────────────────────
        void Intent(string at, Intent it, bool passive)
        {
            if (it == null) { E($"{at}: 수가 없다"); return; }
            bool known = INTENTS.Contains(it.T) || (passive && (it.T == "thorns" || it.T == "selfHeal" || it.T == "revive" || it.T == "feign"));
            if (!known) { E($"{at}: 모르는 수 {it.T}"); return; }
            if ((it.T == "attack" || it.T == "back" || it.T == "attackAll" || it.T == "multi" || it.T == "block" || it.T == "guard" || it.T == "heal" || it.T == "jam" || it.T == "brace") && it.V <= 0) E($"{at}: {it.T} 에 v 가 없다");
            if (it.T == "charge") { if (it.Next == null) E($"{at}: charge 에 next 가 없다"); else Intent(at + " next", it.Next, false); }
            if ((it.T == "buff" || it.T == "debuff") && (it.Id == null || !R.ALL_ST.Contains(it.Id))) E($"{at}: 모르는 상태 {it.Id}");
            if (it.T == "addCard") { var c = d.Card(it.Id); if (c == null) E($"{at}: 없는 카드 {it.Id}"); else if (!c.IsStatusCard) W($"{at}: 끼워 넣는 카드는 상태 카드여야 한다 — {it.Id}"); if (it.To != null && it.To != "draw" && it.To != "discard" && it.To != "hand") E($"{at}: to 는 draw · discard · hand"); }
            if (it.Id != null && (it.T == "attack" || it.T == "back" || it.T == "attackAll" || it.T == "multi") && !R.ALL_ST.Contains(it.Id)) E($"{at}: 모르는 상태 {it.Id}");
            if (it.T == "summon" && d.Enemy(it.Id) == null) E($"{at}: 세울 적이 없다 — {it.Id}");
            if (it.NoTough && it.T != "summon") E($"{at}: noTough 는 수 summon(강인도 없는 소환물)에만");
            if (it.T == "cardDebuff" && !R.IsCardSt(it.Id)) E($"{at}: cardDebuff 의 id 는 카드에 붙는 상태(독 · 봉쇄 · 침체 · 빙결)");
            if (it.T == "cardDebuff" && it.To != null && it.To != "hand" && it.To != "draw") E($"{at}: cardDebuff 의 to 는 hand · draw");
            if (it.T == "handCost" && it.V == 0) E($"{at}: handCost 에 v(±비용)가 없다");
            if (it.T == "reshuffle" && it.Id != null && d.Card(it.Id) == null) E($"{at}: reshuffle 에 넣을 카드가 없다 — {it.Id}");
            if (it.T == "shift" && it.Next == null) E($"{at}: shift 에 next(바뀔 수)가 없다");
            if (it.T == "shift" && it.Next != null) Intent(at + " next", it.Next, false);
            if (it.If?.Counter != null && it.If.N <= 0) E($"{at}: if.counter 에 n 이 없다");
            if (it.If != null && it.If.Allies == null && it.If.PartyBlock == null && it.If.SelfBlock == null && it.If.Counter == null) E($"{at}: if 에 allies · partyBlock · selfBlock · counter 가운데 하나가 있어야 한다");
            if (it.Rush != null && it.Rush != 0 && it.Rush < R.ENEMY_RUSH_MIN) W($"{at}: rush 는 0 또는 {R.ENEMY_RUSH_MIN} 이상(엔진이 올린다)");
        }

        void Enemy(EnemyDef e)
        {
            string at = $"적 {e.Id}{d.At("enemy", e.Id)}";
            if (string.IsNullOrEmpty(e.Name)) E($"{at}: name 이 없다");
            if (e.Hp <= 0) E($"{at}: hp 가 없다");
            if (e.Row != "front" && e.Row != "back") E($"{at}: row 는 front · back");
            if (e.Nature != null && Array.IndexOf(R.NATURES, e.Nature) < 0) E($"{at}: 모르는 성격 {e.Nature}");
            if (e.Weak != null) foreach (var w in e.Weak) if (Array.IndexOf(R.NATURES, w) < 0) E($"{at}: 모르는 약점 성격 {w}");
            if (e.Nature == null && (e.Weak == null || e.Weak.Count == 0)) W($"{at}: 성격도 약점도 없다 — 약점 공격이 안 걸린다");
            if (e.Pick != "cycle" && e.Pick != "shuffle") E($"{at}: pick 은 cycle · shuffle");
            if (e.Intents.Count == 0) E($"{at}: intents 가 없다");
            for (int i = 0; i < e.Intents.Count; i++) Intent($"{at} 수[{i}]", e.Intents[i], false);
            if (e.Open != null) Intent($"{at} open", e.Open, false);
            foreach (var (ph, name) in new[] { (e.Phase, "phase"), (e.Phase2, "phase2") })
            {
                if (ph == null) continue;
                if (!(ph.At > 0 && ph.At < 1)) E($"{at} {name}: at 은 0~1");
                if (ph.Intents.Count == 0) E($"{at} {name}: intents 가 없다");
                for (int i = 0; i < ph.Intents.Count; i++) Intent($"{at} {name}[{i}]", ph.Intents[i], false);
            }
            if (e.Phase2 != null && e.Phase == null) E($"{at}: phase2 는 phase 뒤에");
            var cnames = new HashSet<string>();
            foreach (var c in e.Counters)
            {
                string ca = $"{at} 쌓이는 수치 {c.Name}";
                if (string.IsNullOrEmpty(c.Name)) { E($"{at}: 쌓이는 수치에 name 이 없다"); continue; }
                if (!cnames.Add(c.Name)) E($"{ca}: 이름이 겹친다");
                if (R.ALL_ST.Contains(c.Name) || R.IsCardSt(c.Name)) E($"{ca}: 엔진 상태 이름과 같다 — 새 이름으로");
                if (c.Max <= 0 || c.Start < 0 || c.Start > c.Max) E($"{ca}: start 0~max · max 1 이상");
                if (c.At > 0 && c.Act == null) E($"{ca}: at 이 있으면 act(그때 할 수)가 있어야 한다");
                if (c.Act != null) { if (c.At <= 0) E($"{ca}: act 가 있으면 at(문턱)이 있어야 한다"); Intent(ca + " act", c.Act, true); }
                if (c.Mode != null && c.Mode != "now" && c.Mode != "next" && c.Mode != "replace") E($"{ca}: mode 는 now · next · replace");
                if (c.CardType != null && !new[] { "공격", "스킬", "강화", "!공격", "!스킬", "!강화" }.Contains(c.CardType)) E($"{ca}: cardType 은 공격 · 스킬 · 강화 (앞에 ! 면 그것이 아닌 것)");
                if (c.Dealt == 0 && c.Taken == 0 && c.Flat == 0 && c.At == 0 && !c.StunAtZero) W($"{ca}: 하는 일이 없다(dealt · taken · flat · at · stunAtZero)");
            }
            foreach (var r in e.Rare)
            {
                if (r.Id == null || !R.RARES.ContainsKey(r.Id)) E($"{at}: 모르는 희귀종 덧붙임 「{r.Id}」 — {string.Join(" · ", R.RARES.Keys)}");
                if ((r.Id == "reshuffle" || r.Id == "anxietyHits") && (r.Card == null || d.Card(r.Card) == null)) E($"{at}: 희귀종 {r.Id} 에 card(상태 카드)가 없다");
                if (r.Id == "actDebuff" && r.St != null && r.St != "취약" && r.St != "약화") E($"{at}: 희귀종 actDebuff 의 st 는 취약 · 약화");
            }
            if (e.ToughTaken < 0 || e.ToughTaken > 2) E($"{at}: toughTaken 은 0~2(받는 강인도 피해 배율)");
            if (e.Clone != null) { var ch = d.CloneHero(e.Clone); if (ch == null) E($"{at}: clone 은 사도 키(heroes · cloneHeroes) — 없는 사도 {e.Clone}"); else if (e.Nature != null && ch.Nature != null && e.Nature != ch.Nature) E($"{at}: 클론 성격 {e.Nature} 이 사도 {e.Clone} 의 성격 {ch.Nature} 와 다르다"); }
            bool bigFoe = e.Boss || e.Clone != null || e.Id != null && e.Id.EndsWith("_elite", StringComparison.Ordinal);
            if (e.Tough != 0 && e.Tough < (bigFoe ? R.MIN_TOUGH_BIG : R.MIN_TOUGH_FIGHT)) W($"{at}: tough {e.Tough} — 최소 강인도({(bigFoe ? "엘리트 · 보스 · 클론 " + R.MIN_TOUGH_BIG : "일반 " + R.MIN_TOUGH_FIGHT)}) 아래라 엔진이 최소로 올린다");
            // 2026-10-10 사용자 — 최소 강인도는 일반 3 · 엘리트 · 보스 · 클론 5, 낮으면 위의 주의(엔진이 최소로 올린다). 옛 오류 「tough 3 이상」 · 「보스 7 이상」 은 없앴다
            foreach (var p in e.Passives)
            {
                if (p.On == null || !FOE_ON.Contains(p.On)) E($"{at} 패시브 {p.Name}: 모르는 on {p.On}");
                if (string.IsNullOrEmpty(p.Name)) E($"{at}: 패시브 name 이 없다(횟수를 이름으로 센다)");
                if (p.On == "lowHp" && !(p.At > 0 && p.At < 1)) E($"{at} 패시브 {p.Name}: lowHp 의 at 은 0~1");
                if (p.Who != null && ((p.On != "allyDown" && p.On != "allyBroken") || d.Enemy(p.Who) == null)) E($"{at} 패시브 {p.Name}: who 는 allyDown · allyBroken 의 적 id");
                Intent($"{at} 패시브 {p.Name}", p.Do, true);
            }
        }

        // ── 마을 ───────────────────────────────────────────────────────
        void Foes(string at, List<string> ids)
        {
            if (ids == null || ids.Count == 0) { E($"{at}: 적이 없다"); return; }
            foreach (var id in ids) if (d.Enemy(id) == null) E($"{at}: 없는 적 {id}");
        }

        void Village(VillageDef v)
        {
            string at = $"마을 {v.Id}{d.At("village", v.Id)}";
            if (v.Floors.Count != 2) E($"{at}: 층은 둘(바깥 · 안쪽)");
            for (int i = 0; i < v.Floors.Count; i++)
            {
                var f = v.Floors[i]; string fa = $"{at} {i + 1}층";
                if (f.Pools.Count != 3) E($"{fa}: pools 는 세기 셋(약 · 중 · 강)");
                for (int t = 0; t < f.Pools.Count; t++) { if (f.Pools[t].Count == 0) E($"{fa} 세기{t}: 싸움이 없다"); foreach (var p in f.Pools[t]) Foes($"{fa} 세기{t}", p); }
                if (f.Elites.Count == 0) W($"{fa}: 엘리트가 없다(강 세기를 쓴다)");
                foreach (var p in f.Elites)
                {
                    Foes($"{fa} 엘리트", p);
                    // 엘리트 싸움에는 엘리트 몬스터(강인도 MinElite 이상 — tough 를 안 적으면 엘리트 기본 칸)가 하나는 선다(사용자 2026-10-06)
                    var ts = p.Select(id => d.Enemy(id)).Where(x => x != null).Select(x => x.Tough > 0 ? x.Tough : R.TOUGH.Elite).ToList();
                    if (ts.Count > 0 && ts.Max() < R.TOUGH.MinElite) E($"{fa} 엘리트 [{string.Join(",", p)}]: 강인도 {R.TOUGH.MinElite} 이상인 엘리트 몬스터가 없다");
                }
                Foes($"{fa} 보스", f.Boss);
                if (!f.BossElite && f.Boss.Count > 0 && !f.Boss.Any(id => d.Enemy(id)?.Boss == true)) W($"{fa}: 보스 칸에 boss 적이 없다 — bossElite 를 쓰나?");
                // 보스 전투에는 보스 하나만(사용자 2026-10-06) — 졸개를 같이 세우지 않는다. 싸움 중에 세우는 것은 수 summon(noTough)으로
                if (f.Boss.Count > 1) E($"{fa} 보스: 보스 칸은 하나(졸개 없음 — 소환은 보스의 수 summon 으로) — 지금 {string.Join(",", f.Boss)}");
            }
            // 판의 적 속성 다섯이 모두 열려야 한다(사용자 2026-10-06) — 보스 클론을 마을 종족 · 다른 종족의 같은 속성 사도로 채울 수 있나
            if (Bolzena.Core.Run.CloneSlots(d, v.Id).Count > 0)
            {
                var shut = R.FOE_NATURES.Except(Bolzena.Core.Run.NaturesFor(d, v.Id)).ToList();
                if (shut.Count > 0) W($"{at}: 보스 클론을 못 채워 닫힌 적 속성 {string.Join(" · ", shut)} — 그 속성 사도(1~2성 · 3성)가 모자란다");
                // 빌린 몸 — 보스 후보인데 제 클론 키트가 없어 원래 클론의 수 · 패시브를 빌려 쓰는 사도(2026-10-09: 0 이어야 — 생성기 content _gen/clones)
                var kits = new HashSet<string>(d.Enemies.Values.Where(x => x.Clone != null).Select(x => x.Clone));
                var borrowed = Bolzena.Core.Run.CloneSlots(d, v.Id).Select(s => s.floor).Distinct()
                    .SelectMany(fl => R.FOE_NATURES.SelectMany(n => Bolzena.Core.Run.FloorCandidates(d, v.Id, n, fl)))
                    .Where(h => !kits.Contains(h)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
                if (borrowed.Count > 0) W($"{at}: 빌린 몸 보스 클론 {borrowed.Count}명(제 클론 키트 없음) — {string.Join(" · ", borrowed)}");
            }
        }

        // ── 이벤트 ─────────────────────────────────────────────────────
        void Outs(string at, List<Outcome> outs)
        {
            foreach (var o in outs ?? new List<Outcome>())
            {
                if (o.K == null || !OUT_K.Contains(o.K)) { E($"{at}: 모르는 결과 {o.K}"); continue; }
                if ((o.K == "equip" || o.K == "shopGift") && Array.IndexOf(R.GRADES, o.Grade) < 0) E($"{at}: {o.K} 의 grade");
                if (o.K == "equip" && o.Slot != null && Array.IndexOf(R.SLOTS, o.Slot) < 0) E($"{at}: 모르는 칸 {o.Slot}");
                if (o.K == "neutral" && o.Grade != null && Array.IndexOf(R.GRADES, o.Grade) < 0) E($"{at}: 교주 카드 grade");
                if (o.K == "curse") { var c = d.Card(o.Id); if (c == null || !c.IsCurse) E($"{at}: 골칫거리(저주 카드)가 아니다 — {o.Id}"); }
                if (o.K == "gift") { var c = d.Card(o.Id); if (c == null || !c.Gift) E($"{at}: 선물 카드(gift)가 아니다 — {o.Id}"); }
                if (o.K == "next" && o.Next == null) E($"{at}: next 의 내용이 없다");
                if (o.K == "flag") { if (string.IsNullOrEmpty(o.Id)) E($"{at}: flag 의 id 가 없다"); else flagSet.Add(o.Id); }
                if (o.K == "flash" && o.All) E($"{at}: flash 의 all(다섯 중 고르기)은 없앴다 — 신탁은 늘 무작위 셋 가운데 하나(2026-10-05)");
                if (o.K == "next" && o.Next?.Buff != null) foreach (var b in o.Next.Buff.Keys) if (!R.ALL_ST.Contains(b)) E($"{at}: 모르는 상태 {b}");
            }
        }

        void Event(EventDef e)
        {
            string at = $"이벤트 {e.Id}{d.At("event", e.Id)}";
            if (string.IsNullOrEmpty(e.Name)) E($"{at}: name 이 없다");
            if (e.Options.Count == 0) E($"{at}: 선택지가 없다");
            if (e.Pool != "공용" && !d.Villages.ContainsKey(e.Pool ?? "") && !d.Villages.Values.SelectMany(v => v.Floors).Any(f => f.Land == e.Pool)) W($"{at}: pool 「{e.Pool}」 — 마을 id 도 땅도 아니다");
            if (e.Floor < 0 || e.Floor > 2) E($"{at}: floor 는 0(두 층) · 1 · 2");
            Outs(at + " 떠나기", e.LeaveOut);
            for (int i = 0; i < e.Options.Count; i++)
            {
                var o = e.Options[i]; string oa = $"{at} 선택지[{i}]";
                if (string.IsNullOrEmpty(o.Label)) E($"{oa}: label 이 없다");
                Outs(oa, o.Out);
                if (o.Hero != null) foreach (var h in o.Hero) if (d.Hero(h) == null) W($"{oa}: 없는 사도 {h}");
                if (o.Gamble != null)
                {
                    double sum = o.Gamble.Sum(g => g.P);
                    if (Math.Abs(sum - 1) > 0.01) E($"{oa}: gamble 의 p 합이 1 이 아니다({sum:0.##})");
                    foreach (var g in o.Gamble) Outs(oa + " gamble", g.Out);
                }
                if (o.Judge != null) { if (o.Judge.By != "atk-max" && o.Judge.By != "hp-party") E($"{oa}: judge.by 는 atk-max · hp-party"); Outs(oa + " 성공", o.Judge.Pass); Outs(oa + " 실패", o.Judge.Fail); }
                if (o.Fight != null)
                {
                    if (o.Fight.ByLand != null) foreach (var kv in o.Fight.ByLand) Foes($"{oa} 싸움({kv.Key})", kv.Value);
                    else Foes(oa + " 싸움", o.Fight.Enemies);
                    Outs(oa + " 이기면", o.Fight.Win);
                }
                if (o.Price != null) Outs(oa + " price", o.Price.Out);
                // by — 대사별 말하는 이. "hero" 는 사도 조건(hero · price.hero)이 있는 선택지에서만
                foreach (var b in (o.By ?? new List<string>()).Concat(o.Gamble?.SelectMany(g => g.By ?? new List<string>()) ?? Enumerable.Empty<string>()))
                    if (b == "hero" && (o.Hero == null || o.Hero.Count == 0) && o.Price?.Hero == null) E($"{oa}: by 「hero」 는 hero · price.hero 가 있는 선택지에서만");
                if (o.When != null && o.When != "hp30") E($"{oa}: when 은 hp30 만");
                if (o.Flag != null) flagUse[o.Flag] = oa;
                if (o.NoFlag != null) flagUse[o.NoFlag] = oa;
            }
            if (e.NeedFlag != null) flagUse[e.NeedFlag] = at;
            if (e.FlagWeight < 0) E($"{at}: flagWeight 는 0 이상");
            foreach (var o in e.Options) foreach (var L in new[] { o.Out }.Concat(o.Gamble?.Select(g => g.Out) ?? Enumerable.Empty<List<Outcome>>()).Concat(o.Judge != null ? new[] { o.Judge.Pass } : new List<Outcome>[0]).Concat(o.Fight != null ? new[] { o.Fight.Win } : new List<Outcome>[0]))
                foreach (var x in L ?? new List<Outcome>()) if (x.K == "flag" && x.Id != null) flagFloor[x.Id] = Math.Max(flagFloor.TryGetValue(x.Id, out var ff) ? ff : 0, e.Floor);
            foreach (var f in e.Options.Select(o => o.Flag).Where(x => x != null).Append(e.NeedFlag).Where(x => x != null).Distinct()) flagRead.Add((f, e.Floor, at));
        }

        // ── 연속 이벤트 깃발 — 세우는 곳 없이 읽기만 하는 깃발 · 세우기만 하고 아무도 안 읽는 깃발 ──
        readonly HashSet<string> flagSet = new();
        readonly Dictionary<string, string> flagUse = new();
        readonly Dictionary<string, int> flagFloor = new();
        readonly List<(string flag, int floor, string at)> flagRead = new();
        void Flags()
        {
            // 줄기 뒤 이벤트가 뜰 수 있는가 — 깃발을 세우는 이벤트가 2층에서만 나오는데 읽는 이벤트가 1층에서만 나오면 그 깃발은 쓸모가 없다
            foreach (var (f, fl, at) in flagRead)
                if (flagFloor.TryGetValue(f, out var sf) && sf > 0 && fl > 0 && fl < sf) W($"{at}: 깃발 「{f}」 을 세우는 이벤트는 {sf}층인데 읽는 이벤트는 {fl}층 — 뜰 수 없다");
            foreach (var kv in flagUse) if (!flagSet.Contains(kv.Key)) W($"{kv.Value}: 깃발 「{kv.Key}」 을 세우는 결과(flag)가 어느 이벤트에도 없다");
            foreach (var f in flagSet) if (!flagUse.ContainsKey(f)) W($"깃발 「{f}」: 세우기만 하고 읽는 이벤트(flag · noFlag · needFlag)가 없다");
        }

        // ── 장비 ───────────────────────────────────────────────────────
        void Equip(EquipDef e)
        {
            string at = $"장비 {e.Id}{d.At("equip", e.Id)}";
            if (string.IsNullOrEmpty(e.Name)) E($"{at}: name 이 없다");
            if (Array.IndexOf(R.SLOTS, e.Slot) < 0) E($"{at}: slot 은 무기 · 방어구 · 장신구");
            if (Array.IndexOf(R.GRADES, e.Grade) < 0) E($"{at}: grade 는 일반 · 고급 · 희귀 · 전설");
            if (e.Affinity != null && d.Hero(e.Affinity) == null) W($"{at}: 애착 사도 {e.Affinity} 가 없다");
            for (int i = 0; i < e.Effect.Count; i++) Rule($"{at} 효과[{i}]", e.Effect[i]);
            for (int i = 0; i < e.AffinityEffect.Count; i++) Rule($"{at} 애착[{i}]", e.AffinityEffect[i]);
        }
    }
}
