using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bolzena.Core
{
    /// <summary>
    /// 프론티어 시즌 — 보스 하나 · 턴 제한 · 적 배율. 적 배율은 판의 층 배율(R.FoeScale)과 떼어 둔다(판 밸런스를 고쳐도 시즌 보스가 안 바뀐다) —
    /// 보스 데이터의 hp · 수 값이 그대로 실제 값(Hp · Dmg 1).
    /// </summary>
    public sealed class FrontierSeason
    {
        public string Id, Boss;
        /// <summary>파티가 낼 수 있는 턴 수 — 이만큼 턴을 넘기면(N번째 EndTurn 이 끝나면) 「시간 끝」.</summary>
        public int Turns;
        public double Hp = 1, Dmg = 1;
    }

    /// <summary>
    /// 프론티어 전투(2단계 — bolzena-unity/Docs/프론티어_설계.md §5 · §7) — 저장 사도 슬롯 여러 장(같은 사도 둘 금지)으로 파티를 짜 시즌 보스 하나와 정해진 턴만큼 싸운다.
    /// 씨앗은 받기만 한다(시도마다 서버가 무작위로 — 문서 결정 6 추천). 보스는 성격 상성 없음(BattleSetup.NoNature) — 카드 효과로 붙는 약점(약점 노출 · 「약점」 태그 · 표식 · 「성격 약점」 상태)은 그대로 통한다.
    /// 신탁 · 은총은 뜨지 않는다(덱 고정 — 결정 8 추천) · 고학년 게이지 0 · 교주 보드 없음 · 이벤트 덤 없음 · 파티 HP 는 슬롯 사도 몫(HP + 장비 HP) 합으로 가득.
    /// 끝: 보스(첫 적)가 쓰러짐(win) · 파티가 쓰러짐(lose) · 턴 제한(time — 엔진 밖에서 정한다, Battle.Over 는 null 그대로).
    /// 점수: 보스가 실제로 잃은 HP 합(Unit.Lost — 방어 · 실드가 막은 몫 · 넘친 피해 빼고, 주인 모를 피해 · 지속 피해 · 반사 포함). 보스를 쓰러뜨리면 그때까지 잃은 HP(= 최대 HP)로 끝 — 남은 턴 덤 없음(결정 9 추천 그대로).
    /// </summary>
    public static class Frontier
    {
        /// <summary>프리시즌(판 전체 검증 전 시범 시즌 — 2026-10-18 사용자) 보스 「M.E.O.W」.</summary>
        public static readonly FrontierSeason PRESEASON = new FrontierSeason { Id = "preseason", Boss = "frontier_meow", Turns = 10 };
        public const int PARTY_MAX = 3;
        public const string TIME = "time";

        /// <summary>
        /// 그 슬롯들로 프론티어를 열 수 없는 까닭(열 수 있으면 null) — 1~3명 · 같은 사도 둘 금지 · 슬롯마다 규칙 검사 · 시즌 보스가 데이터에 있음 · 슬롯끼리 같은 카드 id 에 다른 신탁 · 축복 · 카드 값이 없음.
        /// strict false = 시험 덱(2026-10-20 「덱 만들기」 — 이 PC 시험 전투 · 시험 계정의 시험 칸): 규칙 검사(HeroSlot.Check) 대신 전투를 지을 수 있는지만 본다
        /// (있는 사도 · 있는 카드 · 있는 장비 · 칸이 맞는 장비 · 신탁 번호 범위). 서버 검증(Verify)은 늘 규칙 검사를 한다 — 시험 점수는 검증하지 않는다(서버 문서 §시험 계정).
        /// </summary>
        public static string WhyNot(GameData d, IList<HeroSlot> slots, FrontierSeason season, bool strict = true)
        {
            if (season == null || season.Turns <= 0) return "시즌(턴 제한)이 없다";
            if (d.Enemy(season.Boss) == null) return $"시즌 보스 「{season.Boss}」 가 데이터에 없다";
            if (slots == null || slots.Count == 0) return "슬롯이 없다";
            if (slots.Count > PARTY_MAX) return $"사도는 {PARTY_MAX}명까지";
            var seen = new HashSet<string>();
            foreach (var s in slots)
            {
                if (s == null) return "빈 슬롯";
                if (!seen.Add(s.Hero)) return $"같은 사도 둘은 한 파티에 넣을 수 없다 — {s.Hero}";
                if (strict)
                {
                    var c = HeroSlot.Check(d, s);
                    if (!c.Ok) return $"{s.Hero} 슬롯 — {string.Join(" / ", c.Errors)}";
                }
                else if (Buildable(d, s) is string bw) return $"{s.Hero} 슬롯 — {bw}";
            }
            var o = new Dictionary<string, int>(); var b = new Dictionary<string, string>();
            foreach (var s in slots) foreach (var c in s.Cards)
                {
                    if (o.TryGetValue(c.Id, out var o0) && o0 != c.O) return $"슬롯끼리 같은 카드 「{c.Id}」 에 다른 신탁";
                    if (b.TryGetValue(c.Id, out var b0) && b0 != c.B) return $"슬롯끼리 같은 카드 「{c.Id}」 에 다른 축복";
                    o[c.Id] = c.O; b[c.Id] = c.B;
                }
            return null;
        }

        /// <summary>시험 덱이 전투를 지을 수 있나(규칙 검사 밖 — 장수 · 고유 원본 · 유일 · 주인 · 성장 상한은 보지 않는다). 못 지으면 까닭.</summary>
        internal static string Buildable(GameData d, HeroSlot s)
        {
            if (d.Hero(s.Hero) == null) return $"없는 사도 키 「{s.Hero}」";
            var cards = s.Cards ?? new List<SlotCard>();
            if (cards.Count == 0) return "카드가 없다";
            if (cards.Count > HeroSlot.MAX_CARDS) return $"카드가 {cards.Count}장 — 상한 {HeroSlot.MAX_CARDS}";
            foreach (var c in cards)
            {
                var def = c?.Id != null ? d.Card(c.Id) : null;
                if (def == null) return $"없는 카드 「{c?.Id}」";
                if (c.O < 0 || c.O > def.Oracles.Count) return $"카드 「{c.Id}」 신탁 {c.O} — 0~{def.Oracles.Count}";
            }
            foreach (var kv in s.Gear ?? new Dictionary<string, string>())
            {
                var e = d.Equip(kv.Value);
                if (e == null) return $"없는 장비 「{kv.Value}」";
                if (e.Slot != kv.Key) return $"장비 「{kv.Value}」 는 {e.Slot} 칸 — {kv.Key} 칸에 낄 수 없다";
            }
            return null;
        }

        /// <summary>슬롯 → 전투 셋업(순수 — 서버 검증기가 같은 슬롯 · 씨앗에서 똑같이 짓는다). 열 수 없으면 ArgumentException. strict false = 시험 덱(WhyNot).</summary>
        public static BattleSetup Setup(GameData d, IList<HeroSlot> slots, FrontierSeason season, long seed, bool strict = true, ProgState prog = null)
        {
            var why = WhyNot(d, slots, season, strict);
            if (why != null) throw new ArgumentException(why);
            var st = new BattleSetup
            {
                Party = slots.Select(s => s.Hero).ToList(), Enemies = new List<string> { season.Boss }, Seed = seed,
                NoNature = true, EnemyHp = season.Hp, EnemyDmg = season.Dmg, Floor = 2,
            };
            PutSlots(d, st, slots, prog);
            return st;
        }

        /// <summary>
        /// 슬롯들의 덱 · 장비 · 판 성장 · 신탁 · 축복 · 카드 값과 사도 영구 성장(prog)을 셋업에 담는다 — 프론티어가 짓는다.
        /// 신탁(Glow)은 없다(덱 고정). 차례는 슬롯 차례 · 장비 칸 차례 그대로(셋업 JSON 이 판마다 같게).
        /// </summary>
        internal static void PutSlots(GameData d, BattleSetup st, IList<HeroSlot> slots, ProgState prog)
        {
            st.Gear = new Dictionary<string, Stats>(); st.GearRules = new Dictionary<string, List<PassiveRule>>();
            st.Flash = new Dictionary<string, int>(); st.Shin = new Dictionary<string, string>(); st.Glow = null;
            st.Growth = new Dictionary<string, Stats>(); st.CardVals = new Dictionary<string, Dictionary<string, int>>();
            foreach (var s in slots)
            {
                var k = s.Hero;
                var gear = HeroSlot.SlotOrder(s.Gear.Keys).Select(sl => s.Gear[sl]).ToList();
                st.Gear[k] = gear.Aggregate(new Stats(), (a, id) => a + Run.StatsOf(d, id, k));
                var rules = Run.GearRulesOf(d, k, gear);
                if (rules.Count > 0) st.GearRules[k] = rules;
                if (s.Growth.Atk != 0 || s.Growth.Def != 0 || s.Growth.Crit != 0) st.Growth[k] = new Stats { Atk = s.Growth.Atk, Def = s.Growth.Def, Crit = s.Growth.Crit };
                foreach (var c in s.Cards)
                {
                    st.Deck.Add(c.Id);
                    if (c.O > 0) st.Flash[c.Id] = c.O;
                    if (c.B != null) st.Shin[c.Id] = c.B;
                    if (c.Vals != null && c.Vals.Count > 0) st.CardVals[c.Id] = new Dictionary<string, int>(c.Vals);
                }
            }
            // 사도 영구 성장(Prog — 서버가 시도를 열 때 떠 둔 값) — 없으면 칸도 안 생긴다(성장 전 테이프와 같은 셋업)
            if (prog != null)
            {
                var pb = Prog.Build(d, d.Prog, prog, st.Party);
                if (pb.Mods.Count > 0) st.Prog = pb.Mods;
            }
        }

        /// <summary>프론티어 전투를 연다 — 셋업을 짓고 Battle.Start 뒤 입력 테이프(Record)를 단다. strict false = 시험 덱(규칙 검사 밖 — WhyNot).</summary>
        public static Battle Start(GameData d, IList<HeroSlot> slots, FrontierSeason season, long seed, List<Cue> cues = null, Action<Cue> onCue = null, bool strict = true, ProgState prog = null)
        {
            var st = Setup(d, slots, season, seed, strict, prog);
            var b = Battle.Start(d, st, cues, onCue);
            b.Record(st);
            return b;
        }

        /// <summary>끝났나 — win(보스 쓰러짐) · lose(파티 쓰러짐) · time(턴 제한을 다 씀) · null(아직). 화면 · 봇은 EndTurn 뒤마다 묻는다.</summary>
        public static string Over(Battle b, FrontierSeason season) => b.Over ?? (b.Turn > season.Turns ? TIME : null);

        /// <summary>남은 턴(지금 턴 포함) — 화면 표시.</summary>
        public static int TurnsLeft(Battle b, FrontierSeason season) => Math.Max(0, season.Turns - b.Turn + 1);

        /// <summary>점수 — 보스(첫 적)가 이 전투에서 실제로 잃은 HP 합. 소환물이 잃은 HP 는 세지 않는다.</summary>
        public static int Score(Battle b) => b.Enemies.Count > 0 ? b.Enemies[0].Lost : 0;

        /// <summary>끝난 전투의 기록(서버에 낼 것) — 입력 테이프를 닫고 시즌 · 씨앗 · 슬롯 지문 · 점수 · 끝을 함께.</summary>
        public static FrontierTape Tape(Battle b, FrontierSeason season, IList<HeroSlot> slots)
        {
            if (b.Input == null) throw new InvalidOperationException("입력 테이프가 없는 전투 — Frontier.Start 로 연 전투가 아니다");
            b.Input.Close(b);
            return new FrontierTape
            {
                Season = season.Id, Boss = season.Boss, Turns = season.Turns, Seed = b.Input.Setup().Seed,
                Heroes = slots.Select(s => s.Hero).ToList(), Slots = slots.Select(s => s.Hash()).ToList(),
                Score = Score(b), Over = Over(b, season), Played = Math.Min(b.Turn, season.Turns),
                InputJson = JToken.Parse(b.Input.Json()),
            };
        }

        /// <summary>
        /// 다시 돌려 확인(서버 검증기 · bz frontier) — slots 를 주면 그 슬롯 · 시즌 · 씨앗으로 셋업을 다시 지어 테이프의 셋업과 같은지부터 본다(클라이언트가 셋업을 고쳤나).
        /// 그다음 처음부터 다시 돌려: 턴 지문 · 끝 지문이 같고, 턴 제한 · 전투 끝 뒤의 수가 없고, 끝(win · lose · time)과 점수가 같아야 Ok.
        /// </summary>
        public static FrontierCheck Verify(GameData d, FrontierTape tape, FrontierSeason season, IList<HeroSlot> slots = null, ProgState prog = null)
        {
            var r = new FrontierCheck();
            if (tape == null) return r.Fail("테이프가 없다");
            if (tape.Kind != FrontierTape.KIND || tape.V != FrontierTape.VERSION) return r.Fail($"모르는 테이프 {tape.Kind} v{tape.V}");
            if (tape.Season != season.Id || tape.Boss != season.Boss || tape.Turns != season.Turns) return r.Fail($"다른 시즌 — {tape.Season}/{tape.Boss}/{tape.Turns}턴 (지금 {season.Id}/{season.Boss}/{season.Turns}턴)");
            var input = tape.Input();
            if (input == null) return r.Fail("입력 테이프가 없다");
            var st = input.Setup();
            if (st.Seed != tape.Seed) return r.Fail("씨앗이 셋업과 다르다");
            if (slots != null)
            {
                if (!slots.Select(s => s.Hero).SequenceEqual(tape.Heroes ?? new List<string>())) return r.Fail("편성이 다르다");
                if (!slots.Select(s => s.Hash()).SequenceEqual(tape.Slots ?? new List<string>())) return r.Fail("슬롯 지문이 다르다");
                BattleSetup want;
                try { want = Setup(d, slots, season, tape.Seed, prog: prog); }
                catch (ArgumentException e) { return r.Fail("슬롯으로 열 수 없다 — " + e.Message); }
                if (!JToken.DeepEquals(BattleInput.Of(want).SetupJson, input.SetupJson)) return r.Fail("셋업이 슬롯 · 시즌 · 씨앗으로 지은 것과 다르다");
            }
            else if (!st.NoNature || st.Enemies.Count != 1 || st.Enemies[0] != season.Boss) return r.Fail("프론티어 셋업이 아니다(보스 · 성격 상성)");
            string late = null;
            var rp = BattleInput.Replay(d, input, b => { if (late == null && Over(b, season) != null) late = $"전투가 끝난 뒤({Over(b, season)} · {b.Turn}턴)의 수"; });
            r.Battle = rp.Battle; r.Turn = rp.Turn;
            if (!rp.Same) return r.Fail(rp.Why);
            if (late != null) return r.Fail(late);
            r.Over = Over(rp.Battle, season); r.Score = Score(rp.Battle);
            if (r.Over == null) return r.Fail($"끝나지 않은 테이프({rp.Battle.Turn}턴)");
            if (r.Over != tape.Over) return r.Fail($"끝이 다르다({tape.Over} → {r.Over})");
            if (r.Score != tape.Score) return r.Fail($"점수가 다르다({tape.Score} → {r.Score})");
            r.Ok = true;
            return r;
        }
    }

    /// <summary>프론티어 한 번의 기록 — 시즌 · 보스 · 턴 제한 · 씨앗 · 편성 · 슬롯 지문 · 점수 · 끝(win · lose · time) · 낸 턴 · 입력 테이프(BattleInput JSON 그대로).</summary>
    public sealed class FrontierTape
    {
        public const string KIND = "bolzena-frontier";
        public const int VERSION = 1;
        public string Kind = KIND;
        public int V = VERSION;
        public string Season, Boss;
        public int Turns;
        public long Seed;
        public List<string> Heroes = new(), Slots = new();
        public int Score, Played;
        public string Over;
        [JsonProperty("input")] public JToken InputJson;

        static readonly JsonSerializerSettings Js = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false } },
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            FloatParseHandling = FloatParseHandling.Double,
        };
        public string Json() => JsonConvert.SerializeObject(this, Js);
        public static FrontierTape Parse(string json) => JsonConvert.DeserializeObject<FrontierTape>(json, Js);
        public BattleInput Input() => InputJson == null ? null : BattleInput.Parse(InputJson.ToString(Formatting.None));
    }

    public sealed class FrontierCheck
    {
        public bool Ok;
        public string Why, Over;
        public int Score, Turn = -1;
        public Battle Battle;
        internal FrontierCheck Fail(string why) { Ok = false; Why = why; return this; }
    }
}
