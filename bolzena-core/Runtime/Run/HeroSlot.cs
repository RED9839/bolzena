using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bolzena.Core
{
    /// <summary>
    /// 사도 저장 슬롯(프론티어 2단계 — bolzena-unity/Docs/프론티어_설계.md §2.3) — 완주한 판에서 사도 한 명을 떠 둔 것.
    /// 담는 것: 그 사도가 주인인 카드(사도 카드 CardDef.Hero · 교주 · 선물 카드 「id@사도」 · 복제본 · 한 장 번호 그대로)와 카드마다 신탁 번호 · 축복 · 카드 값,
    /// 장비(칸 → 장비 id), 판 단위 성장(공격 · 방어 · 치명). 숫자 능력치(HP · 공격 …)는 적지 않는다 — 데이터 + 장비 + 성장에서 다시 센다.
    /// 주인 없는 상태 · 저주 카드(「짐」)는 버리지 않고 판의 편성 차례로 돌려 나눈다(덱 차례로 k번째 짐 → 편성[k % 사도 수]) — 판에서 진 짐은 어느 사도를 떠도 몫만큼 따라온다.
    /// 성장은 판 성장(RunState.Growth)만 담는다.
    /// </summary>
    public sealed class HeroSlot
    {
        public const string KIND = "bolzena-hero-slot";
        public const int VERSION = 1;
        /// <summary>한 슬롯의 카드 상한(규칙 검사) — 봇 판 끝 덱이 사도 한 명 몫으로 10장 안팎, 넉넉히.</summary>
        public const int MAX_CARDS = 40;
        /// <summary>판 단위 성장 한 능력치의 상한(규칙 검사) — 판 하나의 growRun 합으로 닿기 어려운 값.</summary>
        public const int MAX_GROWTH = 500;

        public string Kind = KIND;
        public int V = VERSION;
        public string Hero;
        /// <summary>만든 때의 규칙 지문(Fingerprint.Rules) · 콘텐츠 지문(GameData.ContentHash — 폴더에서 읽었을 때만).</summary>
        public string Rules, Content;
        public SlotFrom From = new();
        public List<SlotCard> Cards = new();
        /// <summary>칸(R.SLOTS) → 장비 id.</summary>
        public Dictionary<string, string> Gear = new();
        public SlotGrowth Growth = new();

        static readonly JsonSerializerSettings Js = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false } },
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            MissingMemberHandling = MissingMemberHandling.Error,   // 모르는 키는 잘못 쓴 것
        };

        public string Json(bool indent = false) => JsonConvert.SerializeObject(this, indent ? Formatting.Indented : Formatting.None, Js);
        public static HeroSlot Parse(string json)
        {
            var s = JsonConvert.DeserializeObject<HeroSlot>(json, Js) ?? throw new FormatException("빈 슬롯");
            // null 로 적힌 칸을 빈 것으로
            s.From ??= new SlotFrom(); s.Cards ??= new List<SlotCard>(); s.Gear ??= new Dictionary<string, string>(); s.Growth ??= new SlotGrowth();
            return s;
        }
        /// <summary>슬롯 지문 — 같은 슬롯인지(프론티어 테이프가 어느 슬롯으로 싸웠는지 적는다). 들여쓰지 않은 JSON 의 FNV-64.</summary>
        public string Hash() => Fingerprint.Hash(Json());

        // ── 뜨기 ──────────────────────────────────────────────────────
        /// <summary>그 판 · 그 사도를 지금 뜰 수 없는 까닭(뜰 수 있으면 null) — 완주(Done "clear" — 2층 보스 + 복제 단계 뒤)만, 파티에 있는 사도만.</summary>
        public static string WhyNot(Run run, string heroKey)
        {
            if (run == null) return "판이 없다";
            if (run.S.Done != "clear") return "완주한 판만 저장할 수 있다(2층 보스 + 복제 뒤)";
            if (heroKey == null || !run.S.Party.Contains(heroKey)) return "파티에 없는 사도다";
            if (run.Data.Hero(heroKey) == null) return "데이터에 없는 사도다";
            return null;
        }

        /// <summary>판의 덱 한 장의 주인 — 사도 카드는 CardDef.Hero, 교주 · 선물 카드는 「@사도」, 상태 · 저주(짐)는 null.</summary>
        public static string OwnerOf(GameData d, string cardId) => d.Card(cardId)?.Hero ?? GameData.OwnerOf(cardId);

        /// <summary>
        /// 완주한 판(RunRecord.Of 를 부르는 자리 — 모든 AfterFight · 복제 단계가 끝난 뒤)에서 사도 한 명을 뜬다. 판을 바꾸지 않는 순수 함수(봇 판에도 그대로).
        /// 뜰 수 없으면(WhyNot) InvalidOperationException.
        /// </summary>
        public static HeroSlot Of(Run run, string heroKey, string ended = null)
        {
            var why = WhyNot(run, heroKey);
            if (why != null) throw new InvalidOperationException(why);
            var S = run.S; var d = run.Data;
            var slot = new HeroSlot
            {
                Hero = heroKey, Rules = Fingerprint.Rules(), Content = d.ContentHash,
                From = new SlotFrom { Seed = S.Seed, Result = S.Done, Village = S.Village, Nature = S.EnemyNature, Floor = S.Floor + 1, Party = S.Party.ToList(), Ended = ended },
            };
            int burden = 0;
            foreach (var id in S.Deck)
            {
                var owner = OwnerOf(d, id);
                if (owner == null) owner = S.Party[burden++ % S.Party.Count];   // 짐 — 편성 차례로 돌려 나눈다
                if (owner != heroKey) continue;
                var c = new SlotCard { Id = id };
                if (S.Flash.TryGetValue(id, out var n) && n > 0) c.O = n;
                if (S.Shin.TryGetValue(id, out var sh)) c.B = sh;
                if (S.CardVals.TryGetValue(id, out var cv) && cv.Count > 0) c.Vals = new Dictionary<string, int>(cv);
                slot.Cards.Add(c);
            }
            if (S.Gear.TryGetValue(heroKey, out var g))
                foreach (var sl in SlotOrder(g.Keys)) slot.Gear[sl] = g[sl];
            if (S.Growth.TryGetValue(heroKey, out var gr)) slot.Growth = new SlotGrowth { Atk = gr.Atk, Def = gr.Def, Crit = gr.Crit };
            return slot;
        }

        /// <summary>장비 칸 차례 — R.SLOTS 차례, 모르는 칸은 뒤에 이름 차례(장비 효과가 도는 차례가 판마다 같게).</summary>
        public static List<string> SlotOrder(IEnumerable<string> slots) =>
            slots.OrderBy(s => { int i = Array.IndexOf(R.SLOTS, s); return i < 0 ? 99 : i; }).ThenBy(s => s, StringComparer.Ordinal).ToList();

        // ── 규칙 검사 ─────────────────────────────────────────────────
        /// <summary>
        /// 슬롯이 말이 되나(서버 · 검증기 · 프론티어 입구가 쓴다) — 모양(종류 · 판) · 사도 키 · 완주 표시 · 카드(있는 카드 · 주인 · 만든 카드 아님 · 토큰 아님 · 수 상한 ·
        /// 유일 한 장 · 고유 카드 원본 한 장 · 복제본은 복제할 수 있는 카드만) · 신탁 번호(1 ~ 그 카드 신탁 수, 완성형엔 없음) · 축복(그 카드 종류의 공용 꼴 또는 own*) ·
        /// 카드 값(카드 상태 이름 아님) · 장비(있는 장비 · 칸이 맞음) · 성장(0 ~ 상한). 지문이 지금과 다르면 주의(옛 데이터로 뜬 슬롯 — 지금 데이터로 해석한다).
        /// 「합법한 최강 덱」 위조는 못 막는다 — 판 전체 검증(7단계)이 할 일.
        /// </summary>
        public static SlotCheck Check(GameData d, HeroSlot s)
        {
            var r = new SlotCheck();
            void E(string m) => r.Errors.Add(m);
            void W(string m) => r.Warnings.Add(m);
            if (s == null) { E("슬롯이 없다"); return r; }
            if (s.Kind != KIND) E($"kind 가 「{KIND}」 가 아니다 — 「{s.Kind}」");
            if (s.V != VERSION) E($"모르는 판 v{s.V}(지금 v{VERSION})");
            var h = d.Hero(s.Hero);
            if (h == null) { E($"없는 사도 키 「{s.Hero}」"); return r; }
            if (s.From?.Result != "clear") E("완주한 판(result clear)의 슬롯이 아니다");
            if (s.From?.Party != null && s.From.Party.Count > 0 && !s.From.Party.Contains(s.Hero)) E("그 판 편성에 이 사도가 없다");
            if (s.Rules != null && s.Rules != Fingerprint.Rules()) W($"규칙 지문이 지금과 다르다({s.Rules} → {Fingerprint.Rules()}) — 지금 규칙으로 해석한다");
            if (s.Content != null && d.ContentHash != null && s.Content != d.ContentHash) W($"콘텐츠 지문이 지금과 다르다({s.Content} → {d.ContentHash}) — 지금 데이터로 해석한다");

            var cards = s.Cards ?? new List<SlotCard>();
            if (cards.Count > MAX_CARDS) E($"카드가 {cards.Count}장 — 상한 {MAX_CARDS}");
            var ids = new HashSet<string>();
            var originals = new Dictionary<string, int>();   // 고유 카드 원본(복제본 아님) — 맨 id → 장수
            var onlyN = new Dictionary<string, int>();        // 유일(강화 · 「유일」) — 맨 id → 장수(복제본 · 한 장 번호 포함)
            foreach (var c in cards)
            {
                string at = $"카드 「{c?.Id}」";
                if (c == null || string.IsNullOrEmpty(c.Id)) { E("id 가 없는 카드"); continue; }
                var def = d.Card(c.Id);
                if (def == null) { E($"{at}: 없는 카드"); continue; }
                if (GameData.IsPlain(c.Id)) E($"{at}: 전투 중 만든 카드(~)는 덱에 남지 않는다");
                if (def.Token) E($"{at}: 토큰(생성 전용) 카드는 덱에 들지 않는다");
                string owner = OwnerOf(d, c.Id);
                bool burden = owner == null && (def.IsCurse || def.IsStatusCard);
                if (def.Hero != null && GameData.OwnerOf(c.Id) != null && GameData.OwnerOf(c.Id) != def.Hero) E($"{at}: 사도 카드에 다른 주인 꼬리");
                if (owner == null && !burden) E($"{at}: 주인이 없다(교주 · 선물 카드는 「@{s.Hero}」)");
                else if (owner != null && owner != s.Hero) E($"{at}: 주인이 {owner} — 이 슬롯은 {s.Hero}");
                if (def.Ownable && GameData.OwnerOf(c.Id) == null) E($"{at}: 교주 · 선물 카드에 주인 꼬리(@사도)가 없다");
                var view = GameData.WithBless(d.View(c.Id, c.O), c.B != null ? Battle.BlessOf(def, c.B) : null);
                string baseId = GameData.BaseId(c.Id);
                bool copy = GameData.IsCopy(c.Id);
                if (copy && !(def.Unique && def.Hero != null && !def.IsCurse && !def.IsStatusCard))
                    E($"{at}: 복제본은 사도 고유 카드만(기본 · 교주 · 상태 · 저주 카드는 복제되지 않는다)");
                if (copy && (view.IsOnly || view.IsTaboo)) E($"{at}: 유일 · 금기 카드는 복제되지 않는다");
                if (def.Unique && def.Hero != null && !copy) originals[baseId] = (originals.TryGetValue(baseId, out var on) ? on : 0) + 1;
                if (view.IsOnly) onlyN[GameData.NoInst(c.Id)] = 0;   // 판과 같은 셈(Run.FlashOk · PowerWhy) — 한 장 번호만 떼고, 복제본(^)은 다른 카드
                // 신탁
                if (c.O != 0)
                {
                    if (c.O < 1 || c.O > def.Oracles.Count) E($"{at}: 신탁 {c.O} — 이 카드의 신탁은 1~{def.Oracles.Count}{(def.Oracles.Count == 0 ? "(없음)" : "")}");
                    if (def.Complete) E($"{at}: 완성형 카드에는 신탁이 붙지 않는다");
                }
                // 축복
                if (c.B != null)
                {
                    bool own = c.B.StartsWith("own", StringComparison.Ordinal);
                    if (own ? Battle.BlessOf(def, c.B) == null : !Run.DivineKindsFor(d.View(c.Id, c.O)).Contains(c.B))
                        E($"{at}: 이 카드에 붙을 수 없는 축복 「{c.B}」");
                }
                // 카드 값
                if (c.Vals != null)
                    foreach (var kv in c.Vals)
                        if (R.IsCardSt(kv.Key) || kv.Key == "비용") E($"{at}: 카드 값 「{kv.Key}」 는 전투 안의 카드 상태(판에 남지 않는다)");
                if (!ids.Add(c.Id) && (c.O != 0 || c.B != null || c.Vals != null))
                {   // 같은 id 여러 장은 신탁 · 축복 · 카드 값을 나눠 쓴다 — 서로 다르면 말이 안 된다
                    var first = cards.First(x => x.Id == c.Id);
                    if (first.O != c.O || first.B != c.B || !SameVals(first.Vals, c.Vals)) E($"{at}: 같은 id 의 카드끼리 신탁 · 축복 · 카드 값이 다르다");
                }
            }
            foreach (var kv in originals) if (kv.Value > 1) E($"고유 카드 「{kv.Key}」 원본이 {kv.Value}장 — 한 장만(더 있으면 복제본 ^)");
            foreach (var k in onlyN.Keys.ToList())
            {
                int n = cards.Count(c => c != null && c.Id != null && GameData.NoInst(c.Id) == k);
                if (n > 1) E($"유일 카드 「{k}」 가 {n}장 — 덱에 한 장만");
            }
            // 장비
            foreach (var kv in s.Gear ?? new Dictionary<string, string>())
            {
                var e = d.Equip(kv.Value);
                if (e == null) { E($"없는 장비 「{kv.Value}」({kv.Key} 칸)"); continue; }
                if (e.Slot != kv.Key) E($"장비 「{kv.Value}」 는 {e.Slot} 칸 — {kv.Key} 칸에 낄 수 없다");
            }
            // 성장
            var g = s.Growth ?? new SlotGrowth();
            foreach (var (name, v) in new[] { ("atk", g.Atk), ("def", g.Def), ("crit", g.Crit) })
                if (v < 0 || v > MAX_GROWTH) E($"성장 {name} {v} — 0~{MAX_GROWTH}");
            return r;
        }

        static bool SameVals(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) return true;
            if (a == null || b == null || a.Count != b.Count) return false;
            foreach (var kv in a) if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
            return true;
        }
    }

    /// <summary>슬롯 카드 한 장 — Id 는 덱에 있던 그대로(한 장 번호 「#n」 · 주인 「@사도」 · 복제 「^」 꼬리 포함). O 신탁 번호(0 = 없음) · B 축복 · Vals 카드 값.</summary>
    public sealed class SlotCard
    {
        public string Id;
        public int O;
        public string B;
        public Dictionary<string, int> Vals;
    }

    /// <summary>슬롯을 뜬 판 — 씨앗 · 결과(clear) · 마을 · 적 속성 · 층(2) · 편성 · 끝난 시각(화면이 채운다, 봇은 없음).</summary>
    public sealed class SlotFrom
    {
        public long Seed;
        public string Result, Village, Nature, Ended;
        public int Floor;
        public List<string> Party;
    }

    public sealed class SlotGrowth { public int Atk, Def, Crit; }

    public sealed class SlotCheck
    {
        public List<string> Errors = new(), Warnings = new();
        public bool Ok => Errors.Count == 0;
        public override string ToString() => Ok ? "통과" + (Warnings.Count > 0 ? " · 주의 " + string.Join(" / ", Warnings) : "") : string.Join(" / ", Errors);
    }
}
