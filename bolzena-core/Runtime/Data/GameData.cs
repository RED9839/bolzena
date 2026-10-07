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
        /// <summary>카드 정의 id — 꼬리(~ · ^)와 주인(@사도)을 뗀 것. 그림 찾기 · 「같은 카드」 비교.</summary>
        public static string BaseId(string id)
        {
            var s = NoInstCore(Untail(id));
            int at = s?.IndexOf(OWNER, StringComparison.Ordinal) ?? -1;
            return at > 0 ? s.Substring(0, at) : s;
        }
        public static bool IsCopy(string id) => id != null && id.EndsWith(COPY, StringComparison.Ordinal);
        public static bool IsPlain(string id) => id != null && id.EndsWith(PLAIN, StringComparison.Ordinal);
        /// <summary>복제본 번호 — 「x^」 1 · 「x^^」 2 …(복제할 때 원본 스펙이 앞 복제본과 다르면 꼬리가 하나 는다). 복제본이 아니면 0.</summary>
        public static int CopyNo(string id) => IsCopy(id) ? TailLen(id) : 0;
        /// <summary>꼬리 길이 — 「~」 은 1, 「^」 는 이어진 수만큼(이름 한 글자는 남긴다).</summary>
        static int TailLen(string id)
        {
            if (id == null || id.Length < 2) return 0;
            if (id.EndsWith(PLAIN, StringComparison.Ordinal)) return 1;
            int n = 0;
            while (n < id.Length - 1 && id[id.Length - 1 - n] == COPY[0]) n++;
            return n;
        }
        static string Untail(string id) => id == null ? null : id.Substring(0, id.Length - TailLen(id));

        /// <summary>교주 카드 인스턴스의 주인 — 덱의 id 가 「카드@사도」(꼬리는 그 뒤: 「n_x@rico^」). 주인을 정한 교주 카드는 그 사도의 카드로 낸다.</summary>
        public const string OWNER = "@";
        /// <summary>id 에 적힌 주인 사도(없으면 null).</summary>
        public static string OwnerOf(string id)
        {
            var s = NoInstCore(Untail(id));
            int at = s?.IndexOf(OWNER, StringComparison.Ordinal) ?? -1;
            return at > 0 && at < s.Length - 1 ? s.Substring(at + 1) : null;
        }
        /// <summary>주인을 붙인(바꾼) id — 꼬리(~ · ^)는 그대로.</summary>
        public static string WithOwner(string id, string heroKey)
        {
            if (id == null) return null;
            string tail = id.Substring(id.Length - TailLen(id));
            return BaseId(id) + (heroKey != null ? OWNER + heroKey : "") + InstOf(id) + tail;
        }

        /// <summary>
        /// 같은 카드 여러 장 가운데 한 장 — 「카드[@사도]#n[꼬리]」. 신탁 · 은총의 빛은 카드 한 장에만 선다(2026-10-07 사용자 제보 —
        /// 같은 기본 카드가 덱에 여럿이면 id 가 같아 모두 빛났다). 빛나는 한 장 · 신탁을 받은 한 장이 이 번호로 나머지와 갈린다.
        /// 그림 · 정의 · 주인은 그대로(BaseId · OwnerOf 가 뗀다).
        /// </summary>
        public const string INST = "#";
        static string NoInstCore(string core)
        {
            int k = core?.IndexOf(INST, StringComparison.Ordinal) ?? -1;
            return k > 0 ? core.Substring(0, k) : core;
        }
        /// <summary>id 의 한 장 번호 꼬리(「#2」) — 없으면 "".</summary>
        public static string InstOf(string id)
        {
            var core = Untail(id);
            int k = core?.IndexOf(INST, StringComparison.Ordinal) ?? -1;
            return k > 0 ? core.Substring(k) : "";
        }
        public static bool IsInst(string id) => InstOf(id).Length > 0;
        /// <summary>한 장 번호만 뗀 id(주인 · 꼬리는 그대로) — 「같은 카드 여러 장」 비교.</summary>
        public static string NoInst(string id)
        {
            if (id == null) return null;
            var inst = InstOf(id);
            if (inst.Length == 0) return id;
            var core = Untail(id);
            return core.Substring(0, core.Length - inst.Length) + id.Substring(core.Length);
        }
        /// <summary>한 장 번호 n 을 붙인 id(이미 있으면 바꾼다).</summary>
        public static string WithInst(string id, int n)
        {
            var b = NoInst(id);
            var core = Untail(b);
            return core + INST + n + b.Substring(core.Length);
        }
        /// <summary>주인만 뗀 id(꼬리는 그대로) — 「덱에 한 장」(유일) 비교.</summary>
        public static string NoOwner(string id) => WithOwner(id, null);

        public CardDef Card(string id) => id != null && Cards.TryGetValue(BaseId(id), out var c) ? c : null;
        public HeroDef Hero(string id) => id != null && Heroes.TryGetValue(id, out var h) ? h : null;
        public EnemyDef Enemy(string id) => id == null ? null : Enemies.TryGetValue(id, out var e) ? e : id.StartsWith(CLONE_MARK, StringComparison.Ordinal) ? made.GetOrAdd(id, MakeClone) : null;

        // ── 빌린 몸 클론 ───────────────────────────────────────────────
        /// <summary>클론 데이터가 없는 사도의 보스 클론 id — 「clone~사도키~몸」(몸 = 그 자리 원래 클론 적 id). Enemy(id) 가 그 자리에서 만든다.</summary>
        public const string CLONE_MARK = "clone~";
        public static string CloneId(string heroKey, string bodyId) => CLONE_MARK + heroKey + "~" + bodyId;
        readonly System.Collections.Concurrent.ConcurrentDictionary<string, EnemyDef> made = new();

        /// <summary>
        /// 빌린 몸 클론 — 몸(원래 클론)의 체력 · 수 · 판 · 패시브 · 강인도는 그대로, 이름 · 성격 · clone 은 그 사도. 약점(weak)은 지운다(성격에서).
        /// 그림(Art)은 비운다 — 화면은 Clone(사도 키)으로 그 사도 그림을 찾는다.
        /// </summary>
        EnemyDef MakeClone(string id)
        {
            var p = id.Substring(CLONE_MARK.Length).Split('~');
            if (p.Length != 2) return null;
            var h = Hero(p[0]);
            if (h == null || !Enemies.TryGetValue(p[1], out var body)) return null;
            var e = FromJson<EnemyDef>(ToJson(body));
            e.Id = id; e.Name = h.Name + " (클론)"; e.Nature = h.Nature; e.Clone = h.Id; e.Weak = null; e.Art = null;
            e.Blurb = h.Name + "(클론) — " + body.Name + " 의 수를 빌려 쓰는 클론. " + body.Blurb;
            return e;
        }
        /// <summary>변신 정의(id 는 사도 사이에서 겹치지 않는다) — 없으면 null. HeroOfForm 은 그 변신을 가진 사도.</summary>
        public FormDef Form(string id) => id == null ? null : Heroes.Values.SelectMany(h => h.Forms ?? new List<FormDef>()).FirstOrDefault(f => f.Id == id);
        public HeroDef HeroOfForm(string id) => id == null ? null : Heroes.Values.FirstOrDefault(h => h.Forms != null && h.Forms.Any(f => f.Id == id));
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
        // id 하나에 (신탁 번호 · 주인) 갈래 몇 개. 열쇠는 string 하나(튜플 열쇠는 문자열 둘을 매번 해시한다),
        // 읽기는 잠금 없이(시뮬 스레드끼리 잠금 다툼) — 갈래 배열은 새로 만들어 바꿔 끼우기만 한다
        readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int flash, string owner, CardView v)[]> views = new();

        /// <summary>
        /// 그 카드 id 의 지금 모습 — flash 는 신탁 번호(1~5, 0 이면 기본).
        /// owner — id 에 주인이 없는 교주 카드(옛 저장 · 손으로 짠 덱)를 누구 카드로 볼지(전투는 첫 사도). id 에 주인이 있으면 그것이 이긴다.
        /// </summary>
        public CardView View(string id, int flash = 0, string owner = null)
        {
            if (id == null) return null;
            if (IsPlain(id)) flash = 0;
            if (views.TryGetValue(id, out var arr))
                foreach (var e in arr) if (e.flash == flash && e.owner == owner) return e.v;
            lock (views)   // 시뮬은 여러 스레드가 같은 데이터를 본다 — 만들기만 잠근다(같은 열쇠는 늘 같은 CardView)
            {
                if (views.TryGetValue(id, out arr))
                    foreach (var e in arr) if (e.flash == flash && e.owner == owner) return e.v;
                var c = Card(id);
                if (c == null) return null;
                var v = new CardView(id, c, flash > 0 && flash <= c.Oracles.Count ? c.Oracles[flash - 1] : null, flash,
                    c.Neutral ? OwnerOf(id) ?? owner : null);
                v.Target = TargetOf(v.Fx);
                var na = new (int, string, CardView)[(arr?.Length ?? 0) + 1];
                arr?.CopyTo(na, 0);
                na[na.Length - 1] = (flash, owner, v);
                views[id] = na;
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

        /// <summary>교주 카드의 주인 사도(덱에 넣을 때 고른 사도) — 사도 카드 · 주인 없는 교주 카드는 null. 카드 색 · 핀은 이 사도로.</summary>
        public readonly string Owner;

        /// <summary>모습을 덧입힌 카드(변신 중 카드 덤) — 다른 것은 그대로, 태그 · 효과만 바꾼다.</summary>
        internal CardView(CardView b, List<string> tags, List<Fx> fx)
        {
            Id = b.Id; Def = b.Def; Oracle = b.Oracle; FlashN = b.FlashN; Owner = b.Owner; Cost = b.Cost; Type = b.Type;
            Tags = tags; Fx = fx;
        }

        public CardView(string id, CardDef def, OracleDef oracle, int flashN, string owner = null)
        {
            Id = id; Def = def; Oracle = oracle; FlashN = oracle != null ? flashN : 0; Owner = owner;
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
        /// <summary>이 카드를 내는 사도 — 사도 카드면 그 사도, 교주 카드면 주인(Owner). 피해 · 실드 · 연계 · 「다른 사도 카드」 가 이것을 본다.</summary>
        public string Hero => Owner ?? Def.Hero;
        /// <summary>교주 카드인가(주인이 있어도).</summary>
        public bool Neutral => Def.Neutral;
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
