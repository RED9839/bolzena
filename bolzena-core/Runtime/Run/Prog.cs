using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    // ─────────────────────────────────────────────────────────────────────
    // 사도 성장(카제나판 — bolzena-unity/Docs/성장_설계_카제나판.md).
    // 2026-10-21 밤 사용자 최종 결정: 판 밖 성장은 잠재력 하나만 — 10칸(카제나식 — 줄기 01~07 + 갈래 03-1 · 05-1 · 05-2).
    // 2026-10-22 사용자 「치확 잠재도 만들어 줘」: 사도 공통 갈래 04-1 「치명 숙련」(치명 확률 +3%p × 5) 을 더해 11칸.
    // 레벨 · 진급 · 교주 레벨 · 던전 · 추억 조각은 없앴다. 사도 능력치 = 공개판 기본치 그대로, 잠재력은 그 위에 얹는 덤.
    // 어사이드 · 에고 발현 · 뽑기는 만들지 않는다(교주 보드는 그 전에 없앴다).
    // 표(숫자)는 콘텐츠(content-v2 world/성장/*.json 의 「prog」)에 있다 — 콘텐츠 지문에 들어간다.
    // Prog.Build 가 성장 상태 → 사도별 ProgMods(BattleSetup.Prog)로 바꾸는 순수 함수다. 판(Run.ApplyProg) · 프론티어(Frontier.Setup) · 봇 · 검증기가 같은 함수를 쓴다.
    // 성장을 안 주면(null) 지금 판과 완전히 같다 — 셋업 JSON 에 prog 칸도 안 생긴다.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 사도 하나에 붙는 영구 성장 몫(전투가 읽는 것 — BattleSetup.Prog). Prog.Build 가 짓는다.
    /// Base — 사도 기본치에 더하는 몫(잠재력 05 · 06 능력치). 장비 몫(Unit.GearAdd)과 달리 기본치로 친다(실드 · 회복 바탕 방어에 그대로).
    /// Basic · Neutral · Unique — 그 사도가 낸 기본 · 교주(중립) · 고유 카드의 피해 · 실드 · 치유 배율 덤(0.2 = +20%).
    /// Stat — 전투 능력치 증감(StatMod 에 더함): dealt · guard · heal · weak · critDmg.
    /// Rules — 패시브(잠재력 사도 칸).
    /// </summary>
    public sealed class ProgMods
    {
        public Stats Base = new();
        public double Basic, Neutral, Unique;
        public Dictionary<string, double> Stat;
        public List<PassiveRule> Rules;
        /// <summary>(잠재력 05-2 — 카제나 「신 번뜩임 적응」) 그 사도 카드에 신탁 후보가 뜰 때 축복이 얹힐 확률 배율 덤(0.25 = ×1.25). 판(Run.RollOracles)만 읽는다.</summary>
        public double Shin;
        /// <summary>(잠재력 사도 칸 — 2026-10-20 카제나식) 그 사도 카드의 모습을 바꾸는 몫(Battle.CardOf 가 얹는다). null = 없음.</summary>
        public List<ProgCardMod> Cards;

        public double StatOf(string k) => Stat != null && Stat.TryGetValue(k, out var v) ? v : 0;
        /// <summary>바뀌는 것이 하나도 없나(잠재력 없음).</summary>
        [Newtonsoft.Json.JsonIgnore] public bool Empty => Base.Hp == 0 && Base.Atk == 0 && Base.Def == 0 && Base.Crit == 0 && Basic == 0 && Neutral == 0 && Unique == 0
            && (Stat == null || Stat.Values.All(v => v == 0)) && (Rules == null || Rules.Count == 0) && Shin == 0 && (Cards == null || Cards.Count == 0);
    }

    /// <summary>
    /// (잠재력 사도 칸) 그 사도 카드 하나(card — 카드 정의 id) 또는 거르개(type · tag · unique · basic · token)에 맞는 그 사도 카드의 모습 덤.
    /// up — 피해 · 추가 공격 · 방어 · 실드 · 회복 조각의 비율 +up(0.2 = +20%, 일의 값 ofEvent 조각은 그대로).
    /// fx — 덧붙이는 조각(첫 조건 조각 앞 — 늘 든다) · tail — 끝에 덧붙임(앞 조건이 서야 든다) · tags — 붙이는 태그 · cost — 비용 증감(−1).
    /// </summary>
    public sealed class ProgCardMod
    {
        public string Card, Type, Tag;
        public bool Unique, Basic, Token;
        public double Up;
        public List<Fx> Fx = new();
        public List<Fx> Tail = new();
        public List<string> Tags = new();
        public int Cost;

        /// <summary>그 사도(hero)의 카드 모습 v(정의 id baseId)에 맞는 덤인가.</summary>
        public bool Hits(CardView v, string baseId)
        {
            if (v?.Def == null || v.Def.Hero == null) return false;
            if (Card != null && baseId != Card) return false;
            if (Type != null && v.Type != Type) return false;
            if (Tag != null && !v.HasTag(Tag)) return false;
            if (Unique && !v.Def.Unique) return false;
            if (Token && !v.Def.Token) return false;
            if (Basic && (v.Def.Unique || v.Def.Token)) return false;
            return true;
        }
    }

    /// <summary>
    /// (잠재력 사도 칸 — 카제나 「숙련 · 고유 능력치 강화」 칸) 사도 하나의 칸 효과. 표 potential 의 kind hero 칸이 사도마다 이것을 읽는다(prog.heroes[사도][칸]).
    /// cards — 카드 덤 · rules — 패시브 줄(키워드 문법) · stat — 능력치 열쇠(hp · atk · def · crit 고정값, hp% · atk% · def% 비율).
    /// (2026-10-22 사용자 결정: 능력치 문턱 덤(need · needV · needX · up — 「문턱을 넘으면 +x% 더」)은 없앴다 — 레벨이 없어 판 밖 능력치가 거의 안 올라 죽은 칸이었다. 표에 남아 있으면 읽기부터 실패한다.)
    /// </summary>
    public sealed class ProgHeroNodeT
    {
        public string Name, Desc;
        public List<ProgCardMod> Cards = new();
        public List<PassiveRule> Rules = new();
        public Dictionary<string, double> Stat = new();
    }

    // ── 성장 상태(계정 — 서버가 주인. 시험 · 봇은 Prog.Preset) ──

    /// <summary>계정 성장 — 사도별 잠재력.</summary>
    public sealed class ProgState
    {
        public Dictionary<string, ProgHero> Heroes = new();
    }

    /// <summary>사도 하나의 성장 — 잠재력 칸(id → 단계).</summary>
    public sealed class ProgHero
    {
        public Dictionary<string, int> Pot = new();
    }

    // ── 표(콘텐츠) ──

    /// <summary>성장 표 한 벌(콘텐츠 「prog」).</summary>
    public sealed class ProgTable
    {
        public string Id, Name;
        public List<ProgNodeT> Potential = new();
        /// <summary>봇 측정 묶음(none · q1 · half · q3 · max — 잠재력 연 비율) — 사도 공통 꼴.</summary>
        public Dictionary<string, ProgPresetT> Presets = new();
        /// <summary>(잠재력 사도 칸) 사도 → 칸 id → 그 사도의 칸 효과(potential 의 kind hero 칸).</summary>
        public Dictionary<string, Dictionary<string, ProgHeroNodeT>> Heroes = new();
        public ProgNodeT Node(string id) => Potential.FirstOrDefault(n => n.Id == id);
        /// <summary>그 사도의 그 칸 효과(kind hero) — 없으면 null.</summary>
        public ProgHeroNodeT HeroNode(string hero, string node) => hero != null && Heroes.TryGetValue(hero, out var m) && m != null && m.TryGetValue(node, out var n) ? n : null;
    }

    /// <summary>
    /// 잠재력 칸 하나. kind:
    ///   start — 모험 시작 때 그 사도의 고유 카드 1장 더(판만, 프론티어는 저장 덱 그대로).
    ///   card — of(basic · neutral · unique) 카드의 피해 · 실드 · 치유 +V × 단계.
    ///   stat — of(hp · atk · def · role — 역할 주 능력치) +V × 단계(% — 기본치 비율). of crit 은 치명 확률 +V%p × 단계(고정값 — 04-1 「치명 숙련」, 2026-10-22).
    ///   skill — to(카드군 basic · neutral · unique 또는 능력치 증감 dealt · guard · heal · weak · critDmg) +V × 단계(문턱형 V2 는 10-22 없앰).
    ///   stat 의 of hero — (2026-10-20 카제나식 05 · 06 「○○ 숙련」) 사도마다 다른 능력치 — heroes[사도][이 칸].stat 의 열쇠 · 단계당 값 × 단계.
    ///   shin — (05-2 「신 번뜩임 적응」) 그 사도 카드 신탁 후보의 축복 확률 ×(1 + V × 단계).
    ///   hero — (2026-10-20 카제나식) 사도마다 다른 칸 — 효과는 표 heroes[사도][이 칸](ProgHeroNodeT). 그 사도 줄이 없으면 아무것도 안 든다.
    /// 여는 조건 = 앞 칸 순서(after — 앞 칸 1단계 이상) + tier(도전 사다리 — 그 계정이 완주한 가장 높은 난이도 단계가 tier 이상, 2026-10-22 사용자 결정 — 예전 진급 문턱 자리).
    /// 엔진은 받은 단계 그대로 셈한다(여는 조건은 서버 lib/prog.js potCheck 가 지킨다 — 계정 기록이라).
    /// </summary>
    public sealed class ProgNodeT
    {
        public string Id, Name, Kind, Of, Desc;
        /// <summary>이 칸을 여는 데 필요한 도전 사다리 완주 단계(0 = 조건 없음). 서버 POTENTIAL tier 와 같다.</summary>
        public int Tier;
        /// <summary>(2026-10-20 카제나식 갈래) 이 칸을 열려면 1단계 이상이어야 하는 앞 칸 id — 없으면 표에서 바로 앞 칸(첫 칸은 조건 없음). 서버 lib/prog.js POTENTIAL after 와 같다.</summary>
        public string After;
        public List<string> To = new();
        public double V;
        public int Max = 1;
    }

    /// <summary>측정 묶음 — 사도 공통 잠재력 단계.</summary>
    public sealed class ProgPresetT
    {
        public string Desc;
        public Dictionary<string, int> Pot = new();
    }

    /// <summary>Prog.Build 의 결과 — 사도별 전투 몫 · 모험 시작 때 더할 고유 카드 수 · 사도별 요약(문서 · 화면).</summary>
    public sealed class ProgBuild
    {
        public Dictionary<string, ProgMods> Mods = new();
        public Dictionary<string, int> StartUniques = new();
        public Dictionary<string, ProgSum> Sum = new();
    }

    /// <summary>사도 하나의 성장 요약 — 최종 능력치(판 장비 빼고) · 잠재력 실효 단계.</summary>
    public sealed class ProgSum
    {
        public int Hp, Atk, Def, Crit;
        public Dictionary<string, int> Pot = new();
    }

    public static class Prog
    {
        public static readonly string[] CARD_KINDS = { "basic", "neutral", "unique" };
        public static readonly string[] STAT_KINDS = { "dealt", "guard", "heal", "weak", "critDmg" };
        /// <summary>잠재력 사도 칸의 능력치 열쇠 — 고정값 hp · atk · def · crit(%p), 비율 hp% · atk% · def%, 증감 · 카드군.</summary>
        public static readonly string[] FLAT = { "hp", "atk", "def", "crit" }, PCT = { "hp%", "atk%", "def%" };
        public static bool KnownStat(string k) => FLAT.Contains(k) || PCT.Contains(k) || STAT_KINDS.Contains(k) || CARD_KINDS.Contains(k);

        /// <summary>역할 주 능력치(잠재력 stat role · 문턱) — 딜러 공격, 탱커 · 서포터 방어(실드 · 회복이 방어 기준).</summary>
        public static string RoleStat(string role) => role == "딜러" ? "atk" : "def";

        // ── 측정 묶음 → 상태 ──

        /// <summary>측정 묶음(표의 presets) → 그 사도들의 성장 상태. 없는 묶음이면 null.</summary>
        public static ProgState Preset(GameData d, ProgTable t, string name, IEnumerable<string> heroes)
        {
            if (t == null || name == null || !t.Presets.TryGetValue(name, out var p)) return null;
            var s = new ProgState();
            foreach (var k in heroes)
                if (d.Hero(k) != null) s.Heroes[k] = new ProgHero { Pot = new Dictionary<string, int>(p.Pot) };
            return s;
        }

        // ── 상태 → 전투 몫 ──

        /// <summary>
        /// 성장 상태 → 사도별 전투 몫(순수). heroes — 이 싸움 · 판의 사도들(상태에 없는 사도는 성장 없음 = 공개판 기본치 맨몸 = 지금 판).
        /// 잠재력 단계는 0 ~ 칸 max 로 자른다(여는 차례 after 는 서버가 지키고 CheckState 가 본다).
        /// </summary>
        public static ProgBuild Build(GameData d, ProgTable t, ProgState s, IEnumerable<string> heroes)
        {
            var o = new ProgBuild();
            if (t == null || s == null) return o;
            foreach (var key in heroes)
            {
                var h = d.Hero(key);
                if (h == null || !s.Heroes.TryGetValue(key, out var ph) || ph == null) continue;
                var (m, starts, sum) = One(t, h, ph);
                o.Mods[key] = m; o.Sum[key] = sum;
                if (starts > 0) o.StartUniques[key] = starts;
            }
            return o;
        }

        static (ProgMods m, int starts, ProgSum sum) One(ProgTable t, HeroDef h, ProgHero ph)
        {
            var pct = new Dictionary<string, double>();    // hp · atk · def → 비율(기본치에 곱)
            var flat = new Dictionary<string, double>();   // hp · atk · def · crit → 고정값
            var stat = new Dictionary<string, double>();   // dealt · guard · heal · weak · critDmg
            var card = new Dictionary<string, double>();   // basic · neutral · unique
            var rules = new List<PassiveRule>();
            var cards = new List<ProgCardMod>();
            int starts = 0;
            double shin = 0;
            void Add(Dictionary<string, double> m, string key, double v) { if (key != null && v != 0) m[key] = (m.TryGetValue(key, out var x) ? x : 0) + v; }
            // 열쇠 하나를 맞는 칸으로(잠재력 사도 칸)
            void Put(string key, double v)
            {
                if (key == null || v == 0) return;
                if (key.EndsWith("%")) Add(pct, key.TrimEnd('%'), v);
                else if (Array.IndexOf(FLAT, key) >= 0) Add(flat, key, v);
                else if (Array.IndexOf(CARD_KINDS, key) >= 0) Add(card, key, v);
                else Add(stat, key, v);
            }

            var sum = new ProgSum();
            foreach (var n in t.Potential)
            {
                int lvN = ph.Pot != null && ph.Pot.TryGetValue(n.Id, out var pl) ? Math.Max(0, Math.Min(n.Max, pl)) : 0;
                sum.Pot[n.Id] = lvN;
                if (lvN <= 0) continue;
                switch (n.Kind)
                {
                    case "start": starts += lvN; break;
                    case "card": Add(card, n.Of, n.V * lvN); break;
                    case "stat":
                        if (n.Of == "hero") { if (t.HeroNode(h.Id, n.Id) is ProgHeroNodeT sn) foreach (var kv in sn.Stat) Put(kv.Key, kv.Value * lvN); }   // 사도마다 다른 능력치(05 · 06)
                        else if (n.Of == "crit") Add(flat, "crit", n.V * lvN);   // (04-1 「치명 숙련」 — 2026-10-22) 사도 공통 치명 확률 +V%p × 단계
                        else Add(pct, n.Of == "role" ? RoleStat(h.Role) : n.Of, n.V * lvN);
                        break;
                    case "shin": shin += n.V * lvN; break;
                    case "skill": foreach (var to in n.To) Put(to, n.V * lvN); break;
                    case "hero":
                        if (t.HeroNode(h.Id, n.Id) is ProgHeroNodeT hn)
                        {   // 사도 칸 — 카드 덤 · 패시브 · 능력치
                            cards.AddRange(hn.Cards);
                            rules.AddRange(hn.Rules);
                            foreach (var kv in hn.Stat) Put(kv.Key, kv.Value);
                        }
                        break;
                }
            }

            double P(string s0) => pct.TryGetValue(s0, out var v) ? v : 0;
            double Fl(string s0) => flat.TryGetValue(s0, out var v) ? v : 0;
            int hp = Num.Round(h.Hp * (1 + P("hp")) + Fl("hp")), atk = Math.Max(1, Num.Round(h.Atk * (1 + P("atk")) + Fl("atk"))), def = Math.Max(0, Num.Round(h.Def * (1 + P("def")) + Fl("def")));
            int crit = Num.Round(Fl("crit"));

            var m = new ProgMods
            {
                Base = new Stats { Hp = hp - h.Hp, Atk = atk - h.Atk, Def = def - h.Def, Crit = crit },
                Basic = Math.Round(card.TryGetValue("basic", out var cb) ? cb : 0, 4), Neutral = Math.Round(card.TryGetValue("neutral", out var cn) ? cn : 0, 4), Unique = Math.Round(card.TryGetValue("unique", out var cu) ? cu : 0, 4),
                Stat = stat.Count > 0 ? stat.Where(kv => kv.Value != 0).ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 4)) : null,
                Rules = rules.Count > 0 ? rules : null,
                Cards = cards.Count > 0 ? cards : null, Shin = Math.Round(shin, 4),
            };
            sum.Hp = hp; sum.Atk = atk; sum.Def = def; sum.Crit = h.Crit + crit;
            return (m, starts, sum);
        }

        /// <summary>
        /// (잠재력 사도 칸) 카드 모습에 그 사도의 카드 덤(ProgMods.Cards)을 얹는다 — 전투(Battle.CardOf) · 화면(카드 상세)이 같은 함수를 쓴다.
        /// 맞는 덤이 없으면 v 그대로(같은 것). 비용 덤(cost)은 모습이 아니라 Battle.CostOf 가 센다(CostOfMods).
        /// </summary>
        public static CardView CardWith(GameData d, CardView v, string baseId, List<ProgCardMod> mods)
        {
            if (v == null || mods == null || mods.Count == 0) return v;
            var o = v;
            foreach (var cm in mods)
            {
                if (!cm.Hits(o, baseId)) continue;
                var fx = o.Fx.Select(f => cm.Up != 0 && f.OfEvent <= 0 && (f.K == FxK.Dmg || f.K == FxK.Extra || f.K == FxK.Shield || f.K == FxK.Heal || f.K == FxK.Block) ? UpFx(f, cm.Up) : f).ToList();
                if (cm.Fx.Count > 0) { int at = fx.FindIndex(f => FxK.Conditions.Contains(f.K)); fx.InsertRange(at < 0 ? fx.Count : at, cm.Fx); }
                if (cm.Tail.Count > 0) fx.AddRange(cm.Tail);
                var tags = o.Tags.ToList();
                foreach (var tg in cm.Tags) if (!tags.Contains(tg)) tags.Add(tg);
                o = new CardView(o, tags, fx) { Target = d.TargetOf(fx) };
            }
            return o;
        }
        static Fx UpFx(Fx f, double up) { var c = f.Copy(); c.Ratio = Math.Round(f.Ratio * (1 + up), 4); c.Plus = Math.Round(f.Plus * (1 + up), 4); return c; }

        /// <summary>(잠재력 사도 칸) 그 카드 모습에 맞는 비용 덤 합(−1 …).</summary>
        public static int CostOfMods(CardView v, string baseId, List<ProgCardMod> mods)
        {
            if (v == null || mods == null) return 0;
            int n = 0;
            foreach (var cm in mods) if (cm.Cost != 0 && cm.Hits(v, baseId)) n += cm.Cost;
            return n;
        }

        /// <summary>그 칸을 열려면 1단계 이상이어야 하는 앞 칸 id — after 가 없으면 표에서 바로 앞 칸(첫 칸은 null).</summary>
        public static string AfterOf(ProgTable t, ProgNodeT n)
        {
            if (n.After != null) return n.After;
            int i = t.Potential.IndexOf(n);
            return i > 0 ? t.Potential[i - 1].Id : null;
        }

        // ── 검사 ──

        /// <summary>표 검사 — 칸 id 겹침 · 모르는 종류 · 열쇠 · 묶음이 모르는 칸을 가리킴. 패시브 문법은 Validator 가 본다.</summary>
        public static List<string> Check(GameData d, ProgTable t)
        {
            var e = new List<string>();
            if (t == null) return e;
            var ids = new HashSet<string>();
            foreach (var n in t.Potential)
            {
                string at = $"잠재력 {n.Id}";
                if (!ids.Add(n.Id ?? "")) e.Add($"{at}: id 가 겹친다");
                if (n.Max < 1) e.Add($"{at}: max 는 1 이상");
                if (n.Tier < 0 || n.Tier > Tiers.MAX) e.Add($"{at}: tier 는 0 ~ {Tiers.MAX}(도전 사다리 완주 단계)");
                if (AfterOf(t, n) is string ai && t.Node(ai) is ProgNodeT an && an.Tier > n.Tier) e.Add($"{at}: tier {n.Tier} 가 앞 칸 {ai} 의 tier {an.Tier} 보다 낮다");
                if (n.After != null && (t.Node(n.After) == null || t.Potential.IndexOf(t.Node(n.After)) >= t.Potential.IndexOf(n))) e.Add($"{at}: after 「{n.After}」 는 표에서 앞에 있는 칸이어야");
                switch (n.Kind)
                {
                    case "start": break;
                    case "card": if (!CARD_KINDS.Contains(n.Of)) e.Add($"{at}: card 의 of 는 basic · neutral · unique"); break;
                    case "stat": if (n.Of != "role" && n.Of != "hp" && n.Of != "atk" && n.Of != "def" && n.Of != "crit" && n.Of != "hero") e.Add($"{at}: stat 의 of 는 hp · atk · def · crit · role · hero"); break;
                    case "shin": if (n.V <= 0) e.Add($"{at}: shin 의 v 는 0 보다 커야"); break;
                    case "skill":
                        if (n.To.Count == 0 || n.To.Any(x => !CARD_KINDS.Contains(x) && !STAT_KINDS.Contains(x))) e.Add($"{at}: skill 의 to 는 카드군 · 증감({string.Join(" · ", CARD_KINDS.Concat(STAT_KINDS))})");
                        break;
                    case "hero":
                        if (n.Max != 1) e.Add($"{at}: hero 칸은 max 1");
                        break;
                    default: e.Add($"{at}: 모르는 kind {n.Kind}"); break;
                }
            }
            // 사도 칸(kind hero · stat of hero)이 있으면 모든 사도가 그 칸을 다 가져야 — 빠지면 그 사도만 칸이 비어 보인다
            var perHero = t.Potential.Where(n => n.Kind == "hero" || (n.Kind == "stat" && n.Of == "hero")).Select(n => n.Id).ToList();
            if (perHero.Count > 0)
                foreach (var hid in d.Heroes.Keys)
                    foreach (var nid in perHero)
                        if (t.HeroNode(hid, nid) == null) e.Add($"사도 칸: {hid} 에 칸 {nid} 이 없다");
            foreach (var hk in t.Heroes)
            {
                var hd = d.Hero(hk.Key);
                if (hd == null) { e.Add($"사도 칸: 없는 사도 {hk.Key}"); continue; }
                foreach (var nk in hk.Value)
                {
                    string at = $"사도 칸 {hk.Key} {nk.Key}";
                    var tn = t.Node(nk.Key);
                    if (tn == null || (tn.Kind != "hero" && !(tn.Kind == "stat" && tn.Of == "hero"))) e.Add($"{at}: 표 potential 에 kind hero(또는 stat of hero) 칸이 아니다");
                    if (string.IsNullOrEmpty(nk.Value?.Name) || string.IsNullOrEmpty(nk.Value?.Desc)) e.Add($"{at}: name · desc 가 있어야(화면 효과 글)");
                    var hn = nk.Value;
                    if (hn != null)
                    {
                        foreach (var cm in hn.Cards)
                        {
                            if (cm.Card != null && (d.Card(cm.Card) is not CardDef cd || cd.Hero != hk.Key)) e.Add($"{at}: 카드 덤 card {cm.Card} 는 이 사도 카드가 아니다");
                            if (cm.Card == null && cm.Type == null && cm.Tag == null && !cm.Unique && !cm.Basic && !cm.Token) e.Add($"{at}: 카드 덤에 card 또는 거르개가 있어야");
                            if (cm.Up < 0 || cm.Up > 2) e.Add($"{at}: 카드 덤 up {cm.Up} — 0~2");
                            if (cm.Cost > 0 || cm.Cost < -2) e.Add($"{at}: 카드 덤 cost {cm.Cost} — −2~0");
                        }
                        foreach (var s in hn.Stat.Keys) if (!KnownStat(s)) e.Add($"{at}: 모르는 능력치 {s}");
                        if (tn?.Kind == "stat" && hn.Stat.Count == 0) e.Add($"{at}: 능력치 칸(stat of hero)은 stat 에 열쇠 하나 이상");
                    }
                }
            }
            foreach (var kv in t.Presets)
            {
                string at = $"묶음 {kv.Key}";
                foreach (var n in kv.Value.Pot)
                    if (t.Node(n.Key) is not ProgNodeT tn) e.Add($"{at}: 모르는 잠재력 칸 {n.Key}");
                    else if (n.Value < 0 || n.Value > tn.Max) e.Add($"{at}: 잠재력 {n.Key} 단계 {n.Value} — 0~{tn.Max}");
            }
            return e;
        }

        /// <summary>
        /// 성장 상태 검사(서버가 떠 둔 값 · 측정 묶음) — 없는 사도 · 모르는 잠재력 칸 · 단계 범위 · 여는 차례(앞 칸이 1단계 이상).
        /// </summary>
        public static List<string> CheckState(GameData d, ProgTable t, ProgState s)
        {
            var e = new List<string>();
            if (t == null || s == null) return e;
            foreach (var kv in s.Heroes)
            {
                string at = kv.Key;
                if (d.Hero(kv.Key) == null) { e.Add($"{at}: 없는 사도"); continue; }
                var pot = kv.Value?.Pot ?? new Dictionary<string, int>();
                foreach (var p in pot)
                {
                    var n = t.Node(p.Key);
                    if (n == null) { e.Add($"{at}: 모르는 잠재력 칸 {p.Key}"); continue; }
                    if (p.Value < 0 || p.Value > n.Max) e.Add($"{at}: 잠재력 {p.Key} 단계 {p.Value} — 0~{n.Max}");
                    var before = AfterOf(t, n);
                    if (p.Value > 0 && before != null && (!pot.TryGetValue(before, out var bl) || bl < 1)) e.Add($"{at}: 잠재력 {p.Key} 는 앞 칸 {before} 을 먼저 열어야");
                }
            }
            return e;
        }
    }

    public sealed partial class Run
    {
        /// <summary>
        /// 새 판에 사도 성장을 건다(Run.New 뒤 한 번) — 사도별 전투 몫을 판에 적고(RunState.Prog — 싸움마다 BattleSetup.Prog 로),
        /// 파티 최대 HP 를 성장 몫(잠재력 HP)만큼 옮기고, 잠재력 01(시작 고유 카드)만큼 그 사도의 고유 카드를 무작위로 더한다.
        /// 성장이 없으면(null · 빈) 아무것도 안 한다 — 난수 · 판이 예전과 같다.
        /// </summary>
        public void ApplyProg(ProgBuild pb)
        {
            if (pb == null || pb.Mods.Count == 0) return;
            S.Prog = pb.Mods.Where(kv => S.Party.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            if (S.Prog.Count == 0) { S.Prog = null; return; }
            ShiftHp(S.Prog.Values.Sum(m => m.Base.Hp));
            foreach (var key in S.Party)
                for (int n = pb.StartUniques.TryGetValue(key, out var c) ? c : 0; n > 0; n--)
                {
                    var left = UniquesLeft(key);
                    if (left.Count == 0) break;
                    GainCard(left[RndInt(left.Count)]);
                }
        }
    }
}
