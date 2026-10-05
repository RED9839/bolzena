using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bolzena.Core
{
    /// <summary>
    /// 콘텐츠 한 벌 — 사도 · 카드 · 적 · 마을 · 이벤트 · 장비. JSON 폴더(heroes.json · cards.json · enemies.json · villages.json ·
    /// events.json · equips.json — 각각 배열)에서 읽는다. 엔진 · 판 · 봇은 이것 하나만 본다.
    /// </summary>
    public sealed class GameData
    {
        public readonly Dictionary<string, HeroDef> Heroes = new();
        public readonly Dictionary<string, CardDef> Cards = new();
        public readonly Dictionary<string, EnemyDef> Enemies = new();
        public readonly Dictionary<string, VillageDef> Villages = new();
        public readonly List<EventDef> Events = new();
        public readonly Dictionary<string, EquipDef> Equips = new();

        /// <summary>전투 중 만든 카드(패시브 · 키워드의 「카드 생성」) — 신탁 · 축복이 안 붙는 맨 카드. id 뒤에 붙인다.</summary>
        public const string PLAIN = "~";
        /// <summary>복제본 — 원본의 신탁 · 축복을 옮겨 받고 다시는 빛나지 않는다.</summary>
        public const string COPY = "^";

        public static readonly JsonSerializerSettings Json = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,   // 모르는 키는 잘못 쓴 것 — 조용히 넘기지 않는다
            Formatting = Formatting.Indented,
        };

        public static readonly string[] FILES = { "heroes", "cards", "enemies", "villages", "events", "equips" };

        /// <summary>id → 그 id 가 든 파일(폴더에서 읽었을 때). 키는 "hero:id" · "card:id" · "enemy:id" · "village:id" · "event:id" · "equip:id".</summary>
        public readonly Dictionary<string, string> Sources = new();
        public string SourceOf(string kind, string id) => id != null && Sources.TryGetValue(kind + ":" + id, out var f) ? f : null;

        // ── 읽기 ──────────────────────────────────────────────────────
        /// <summary>
        /// 폴더를 통째로 읽는다 — 그 아래 모든 *.json 을 재귀로(경로 차례). 파일 하나는 둘 중 하나의 꼴:
        ///   ① 묶음 — 객체 { "heroes": [...], "cards": [...], "enemies": [...], "villages": [...], "events": [...], "equips": [...] } 의 일부 키
        ///   ② 배열 — 파일 이름이 heroes.json · cards.json … 이면 그 종류의 배열(옛 꼴)
        /// 같은 id 가 두 번 나오면 오류(어느 파일끼리인지 적는다). 이름이 _ 로 시작하는 파일 · 폴더와 *.schema.json 은 건너뛴다.
        /// 경로가 파일이면 그 파일 하나만.
        /// </summary>
        public static GameData FromFolder(string path) => FromFolders(new[] { path });

        /// <summary>여러 경로를 한 벌로 — 같은 id 가 경로 사이에서 겹쳐도 오류다.</summary>
        public static GameData FromFolders(IEnumerable<string> paths)
        {
            var d = new GameData();
            var errs = new List<string>();
            foreach (var p in paths) d.ReadPath(p, errs);
            if (errs.Count > 0) throw new FormatException("데이터를 읽지 못했다" + NL + string.Join(NL, errs));
            d.views.Clear();
            return d;
        }

        void ReadPath(string path, List<string> errs)
        {
            var d = this;
            IEnumerable<string> files;
            if (File.Exists(path)) files = new[] { path };
            else if (Directory.Exists(path))
                files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
                    .Where(f => !Path.GetRelativePath(path, f).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(x => x.StartsWith("_")))
                    .OrderBy(f => Slash(f), StringComparer.Ordinal);
            else { errs.Add($"데이터 경로가 없다: {path}"); return; }
            foreach (var f in files)
            {
                string rel = File.Exists(path) ? Slash(Path.GetFullPath(f)) : Slash(Path.GetRelativePath(path, f));
                Newtonsoft.Json.Linq.JToken tok;
                try { tok = Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(f)); }
                catch (JsonException e) { errs.Add($"{rel}: JSON 꼴이 아니다 — {e.Message}"); continue; }
                if (tok is Newtonsoft.Json.Linq.JArray arr)
                {
                    var kind = Path.GetFileNameWithoutExtension(f);
                    if (Array.IndexOf(FILES, kind) < 0) { errs.Add($"{rel}: 배열 파일은 이름이 {string.Join(" · ", FILES)}.json 이어야 한다(아니면 묶음 객체로)"); continue; }
                    d.AddFrom(kind, arr.ToString(), rel, errs);
                }
                else if (tok is Newtonsoft.Json.Linq.JObject obj)
                {
                    foreach (var prop in obj.Properties())
                    {
                        if (prop.Name.StartsWith("$")) continue;   // $schema 따위
                        if (Array.IndexOf(FILES, prop.Name) < 0) { errs.Add($"{rel}: 모르는 키 「{prop.Name}」 — {string.Join(" · ", FILES)} 가운데"); continue; }
                        d.AddFrom(prop.Name, prop.Value.ToString(), rel, errs);
                    }
                }
                else errs.Add($"{rel}: 객체나 배열이 아니다");
            }
        }

        const string NL = "\n";
        static string Slash(string p) => p.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

        /// <summary>그 종류의 사람 이름 — 검사기 메시지에 「(파일)」 을 붙인다.</summary>
        public string At(string kind, string id)
        {
            var f = SourceOf(kind, id);
            return f != null ? $" ({f})" : "";
        }

        static readonly Dictionary<string, string> KIND1 = new() { ["heroes"] = "hero", ["cards"] = "card", ["enemies"] = "enemy", ["villages"] = "village", ["events"] = "event", ["equips"] = "equip" };

        void AddFrom(string kind, string json, string file, List<string> errs)
        {
            void Put<T>(Dictionary<string, T> dict, T v, string id)
            {
                string key = KIND1[kind] + ":" + id;
                if (string.IsNullOrEmpty(id)) { errs.Add($"{file}: {KIND1[kind]} 에 id 가 없다"); return; }
                if (Sources.TryGetValue(key, out var was)) { errs.Add($"{file}: {KIND1[kind]} id 「{id}」 가 겹친다 — {was} 에도 있다"); return; }
                Sources[key] = file;
                if (dict != null) dict[id] = v;
            }
            try
            {
                switch (kind)
                {
                    case "heroes": foreach (var x in Parse<HeroDef>(json, file)) Put(Heroes, x, x.Id); break;
                    case "cards": foreach (var x in Parse<CardDef>(json, file)) Put(Cards, x, x.Id); break;
                    case "enemies": foreach (var x in Parse<EnemyDef>(json, file)) Put(Enemies, x, x.Id); break;
                    case "villages": foreach (var x in Parse<VillageDef>(json, file)) Put(Villages, x, x.Id); break;
                    case "events": foreach (var x in Parse<EventDef>(json, file)) { int n = Sources.Count; Put<EventDef>(null, x, x.Id); if (Sources.Count > n) Events.Add(x); } break;
                    case "equips": foreach (var x in Parse<EquipDef>(json, file)) Put(Equips, x, x.Id); break;
                }
            }
            catch (FormatException e) { errs.Add(e.Message); }
        }

        /// <summary>JSON 글 여섯(없는 것은 null)을 더한다. 같은 id 는 뒤의 것이 이긴다(시험 · 덧씌우기용).</summary>
        public GameData Add(string heroes, string cards, string enemies, string villages, string events, string equips)
        {
            foreach (var h in Parse<HeroDef>(heroes, "heroes")) Heroes[h.Id] = h;
            foreach (var c in Parse<CardDef>(cards, "cards")) Cards[c.Id] = c;
            foreach (var e in Parse<EnemyDef>(enemies, "enemies")) Enemies[e.Id] = e;
            foreach (var v in Parse<VillageDef>(villages, "villages")) Villages[v.Id] = v;
            foreach (var e in Parse<EventDef>(events, "events")) { Events.RemoveAll(x => x.Id == e.Id); Events.Add(e); }
            foreach (var e in Parse<EquipDef>(equips, "equips")) Equips[e.Id] = e;
            views.Clear();
            return this;
        }

        static List<T> Parse<T>(string json, string what)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            try { return JsonConvert.DeserializeObject<List<T>>(json, Json) ?? new List<T>(); }
            catch (JsonException e) { throw new FormatException($"{(what.EndsWith(".json") ? what : what + ".json")} 을 읽지 못했다: {e.Message}", e); }
        }

        public static string ToJson(object o) => JsonConvert.SerializeObject(o, Json);
        public static T FromJson<T>(string s) => JsonConvert.DeserializeObject<T>(s, Json);

        // ── 찾기 ──────────────────────────────────────────────────────
        public static string BaseId(string id) =>
            id != null && id.Length > 1 && (id.EndsWith(PLAIN) || id.EndsWith(COPY)) ? id.Substring(0, id.Length - 1) : id;
        public static bool IsCopy(string id) => id != null && id.EndsWith(COPY);
        public static bool IsPlain(string id) => id != null && id.EndsWith(PLAIN);

        public CardDef Card(string id) => id != null && Cards.TryGetValue(BaseId(id), out var c) ? c : null;
        public HeroDef Hero(string id) => id != null && Heroes.TryGetValue(id, out var h) ? h : null;
        public EnemyDef Enemy(string id) => id != null && Enemies.TryGetValue(id, out var e) ? e : null;
        public EquipDef Equip(string id) => id != null && Equips.TryGetValue(id, out var e) ? e : null;
        public EventDef Event(string id) => Events.FirstOrDefault(e => e.Id == id);

        /// <summary>그 사도의 고유 카드(id 차례).</summary>
        public List<string> UniquesOf(string heroId) =>
            Cards.Values.Where(c => c.Hero == heroId && c.Unique).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        public List<string> NeutralIds() => Cards.Values.Where(c => c.Neutral).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>시작 덱 — 사도마다 시작 카드.</summary>
        public List<string> BuildDeck(IEnumerable<string> party) => party.SelectMany(k => Hero(k)?.Starter ?? new List<string>()).ToList();

        /// <summary>키워드가 어디 붙나(self · enemy · ally) — 없으면 null.</summary>
        public string CarrierOf(string kw)
        {
            foreach (var h in Heroes.Values) foreach (var k in h.AllKeywords) if (k.Name == kw) return k.Carrier ?? "self";
            return null;
        }

        /// <summary>화면이 대상을 물어야 하나 — "적"(적 1명을 고른다) · "아군" · "없음".</summary>
        public string TargetOf(List<Fx> fx)
        {
            bool enemy = false, ally = false;
            foreach (var f in fx)
            {
                string t = f.Target;
                if (t == null)
                {
                    switch (f.K)
                    {
                        case FxK.Dmg: case FxK.Strip: case FxK.Tough: case FxK.RushDown: t = "oneEnemy"; break;
                        case FxK.Status: if (R.IsBadSt(f.Id)) t = "oneEnemy"; break;
                        case FxK.TakenMod: if (f.V > 0) t = "oneEnemy"; break;
                        case FxK.DealtMod: if (f.V < 0) t = "oneEnemy"; break;
                        case FxK.Stack: if (CarrierOf(f.Id) == "enemy") t = "oneEnemy"; break;
                    }
                }
                if (f.K == FxK.IfHunted || f.K == FxK.IfDebuffs || f.K == FxK.PerDebuff) t = "oneEnemy";   // 고른 적을 본다
                if (t == "oneEnemy") enemy = true;
                if (t == "oneAlly") ally = true;
            }
            return enemy ? "적" : ally ? "아군" : "없음";
        }

        // ── 카드의 실제 모습(신탁을 얹은 것) ─────────────────────────────
        readonly Dictionary<(string, int), CardView> views = new();

        /// <summary>그 카드 id 의 지금 모습 — flash 는 신탁 번호(1~5, 0 이면 기본).</summary>
        public CardView View(string id, int flash = 0)
        {
            if (id == null) return null;
            if (IsPlain(id)) flash = 0;
            lock (views)   // 시뮬은 여러 스레드가 같은 데이터를 본다
            {
                if (views.TryGetValue((id, flash), out var v)) return v;
                var c = Card(id);
                if (c == null) return null;
                v = new CardView(id, c, flash > 0 && flash <= c.Oracles.Count ? c.Oracles[flash - 1] : null, flash);
                v.Target = TargetOf(v.Fx);
                views[(id, flash)] = v;
                return v;
            }
        }
    }

    /// <summary>
    /// 카드의 지금 모습 — 기본 카드에 고른 신탁을 얹은 것. 신탁을 고른 카드는 신탁의 글(태그 · 효과)이 전문이다.
    /// 유일은 카드 종류에 붙는다 — 기본이 유일이면 신탁을 골라도 유일.
    /// </summary>
    public sealed class CardView
    {
        public readonly string Id;
        public readonly CardDef Def;
        public readonly OracleDef Oracle;
        public readonly int FlashN;
        public readonly int Cost;
        public readonly string Type;
        public readonly List<string> Tags;
        public readonly List<Fx> Fx;

        public CardView(string id, CardDef def, OracleDef oracle, int flashN)
        {
            Id = id; Def = def; Oracle = oracle; FlashN = oracle != null ? flashN : 0;
            Cost = oracle?.Cost ?? def.Cost;
            Type = oracle != null && oracle.Power ? "강화" : def.Type;
            var tags = oracle != null ? oracle.Tags.ToList() : def.Tags.ToList();
            if (oracle != null && def.Tags.Contains(Tag.Unique) && !tags.Contains(Tag.Unique)) tags.Insert(0, Tag.Unique);
            Tags = tags;
            Fx = oracle != null ? oracle.Fx : def.Fx;
        }

        /// <summary>화면이 대상을 물어야 하나 — "적" · "아군" · "없음"(GameData.TargetOf).</summary>
        public string Target { get; internal set; }
        public string Name => Def.Name;
        public string Hero => Def.Hero;
        public bool X => Def.X;
        public bool Unique => Def.Unique;
        public bool Signature => Def.Signature;
        public bool IsCopy => GameData.IsCopy(Id);
        public bool IsStatus => Def.IsStatusCard;
        public bool IsCurse => Def.IsCurse;

        public bool HasTag(string t) => TagN(t) >= 0;

        /// <summary>그 태그의 수(「소멸 2」 → 2, 「보존」 → 0). 없으면 -1.</summary>
        public int TagN(string t)
        {
            foreach (var s in Tags) { var (id, n) = Tag.Parse(s); if (id == t) return n; }
            return -1;
        }

        /// <summary>사도의 강화 카드 — 내면 이 전투에서 사라진다 · 덱에 한 장(유일).</summary>
        public bool IsPower => Def.Unique && Def.Hero != null && Type == "강화";
        /// <summary>덱에 한 장만.</summary>
        public bool IsOnly => IsPower || HasTag(Tag.Unique);
        public bool IsTaboo => HasTag(Tag.Taboo) || HasTag(Tag.SealedTaboo);
        /// <summary>두 갈래 카드의 갈래 이름(없으면 null) — 화면이 낼 때 고르게 하고 PlayOpts.Choice(1 · 2)로 넘긴다.</summary>
        public List<string> Choices => Def.Choices;
    }
}
