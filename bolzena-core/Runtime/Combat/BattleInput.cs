using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bolzena.Core
{
    /// <summary>
    /// 입력 테이프(프론티어 1단계 — Docs/프론티어_설계.md §7) — 시작 셋업 + 전투를 바꾸는 바깥 호출 넷(ApplyEpiphany · PlayCard · UseUlt · EndTurn)을 차례대로.
    /// 같은 콘텐츠 · 같은 규칙 값이면 BattleInput.Replay 가 처음부터 다시 돌려 같은 전투를 낸다. 화면 · 봇은 따로 적을 일이 없다 — 네 호출이 스스로 적는다.
    /// 판(Run.OpenFight)이 붙이고, 그 밖의 전투는 Battle.Start 뒤 Record(setup) 로 붙인다. 봇 · 미리보기가 뜨는 복사본(Battle.Clone)에는 안 따라간다.
    /// 턴 지문(Digests): [0] 은 연 직후, 그 뒤는 EndTurn 마다 한 줄 — 다시 돌린 것과 몇 번째에서 갈렸는지 본다.
    /// </summary>
    public sealed class BattleInput
    {
        public int V = 1;
        /// <summary>규칙 지문(Fingerprint.Rules — R · BossUlt 의 정적 값) · 콘텐츠 지문(GameData.ContentHash — 폴더에서 읽었을 때만).</summary>
        public string Rules, Content;
        /// <summary>시작 셋업 — 연 순간을 떠 둔 것(판이 뒤에 고치는 표를 나눠 쓰지 않게). Setup() 으로 되살린다.</summary>
        [JsonProperty("setup")] public JToken SetupJson;
        public List<InputOp> Ops = new();
        public List<string> Digests = new();
        /// <summary>전투가 끝났을 때(Close) — 끝 지문 · 결과(win · lose, 봇의 턴 끊김은 엔진 밖에서 정한 lose).</summary>
        public string End, Over;

        // ── 직렬화 ────────────────────────────────────────────────────
        /// <summary>셋업용 — 기본값 · null 까지 다 적고(콘텐츠의 초깃값과 어긋날 여지를 없앤다) 읽을 때는 통째로 바꾼다. 사전 열쇠(사도 · 카드 id)는 그대로.</summary>
        static readonly JsonSerializer SetupSer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false } },
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            MissingMemberHandling = MissingMemberHandling.Error,
            FloatParseHandling = FloatParseHandling.Double,
        });
        static readonly JsonSerializerSettings TapeJs = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false } },
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            FloatParseHandling = FloatParseHandling.Double,
        };

        public static BattleInput Of(BattleSetup st) => new BattleInput { SetupJson = JToken.FromObject(st, SetupSer), Rules = Fingerprint.Rules() };
        public BattleSetup Setup() => SetupJson.ToObject<BattleSetup>(SetupSer);
        public string Json() => JsonConvert.SerializeObject(this, TapeJs);
        public static BattleInput Parse(string json) => JsonConvert.DeserializeObject<BattleInput>(json, TapeJs);

        // ── 적기(Battle 의 네 입구가 부른다) ─────────────────────────────
        /// <summary>(개발) 수마다 그 수 앞의 지문도 적는다(InputOp.Pre) — 어긋난 수를 한 수 단위로 찾을 때(bz replay --fine). 기본은 끔(턴 지문만).</summary>
        public static bool Fine;

        void Add(Battle b, InputOp op) { if (Fine) op.Pre = b.Digest(); Ops.Add(op); }
        internal void Epi(Battle b, string cardId, int choice) => Add(b, new InputOp { K = InputOp.EPI, Id = cardId, I = choice });
        internal void Play(Battle b, int handIdx, int targetIdx, PlayOpts o) => Add(b, new InputOp
        {
            K = InputOp.PLAY, I = handIdx, T = targetIdx, Id = handIdx >= 0 && handIdx < b.Hand.Count ? b.Hand[handIdx] : null,
            Choice = o.Choice, Spend = o.Spend, Stage = o.Stage, Ally = o.Ally, Discard = o.Discard?.ToList(), Spread = o.Spread?.ToList(),
        });
        internal void Ult(Battle b, string heroKey, int targetIdx, int? stage) => Add(b, new InputOp { K = InputOp.ULT, Id = heroKey, T = targetIdx, Stage = stage });
        internal void EndOp(Battle b) => Add(b, new InputOp { K = InputOp.END });

        /// <summary>전투가 끝났다 — 끝 지문과 결과를 적는다(판은 AfterFight 첫머리에서 부른다). 여러 번 불러도 마지막 것.</summary>
        public void Close(Battle b) { End = b.Digest(); Over = b.Over; }

        // ── 다시 돌리기 ───────────────────────────────────────────────
        /// <summary>
        /// 셋업 + 테이프로 처음부터 다시 돈다. before — 수마다 그 수 앞에서 부른다(미리보기를 끼워 보는 시험).
        /// Same: 턴 지문이 모두 같고(테이프에 있는 데까지) 끝 지문도 같다(있으면). 어긋나면 Turn(지문 번호) · Why.
        /// </summary>
        public static ReplayResult Replay(GameData data, BattleInput tape, Action<Battle> before = null)
        {
            var st = tape.Setup();
            var b = Battle.Start(data, st);
            b.Record(st);
            var r = new ReplayResult { Battle = b, Same = true };
            for (int k = 0; k < tape.Ops.Count; k++)
            {
                var op = tape.Ops[k];
                before?.Invoke(b);
                if (op.Pre != null && op.Pre != b.Digest()) return r.Fail(b.Input.Digests.Count - 1, $"{k}번째 수({op.K} {op.Id}) 앞 지문이 다르다 — 그 앞 수 또는 수 사이에 갈렸다");
                switch (op.K)
                {
                    case InputOp.EPI: b.ApplyEpiphany(op.Id, op.I); break;
                    case InputOp.ULT: b.UseUlt(op.Id, op.T, op.Stage); break;
                    case InputOp.END: b.EndTurn(); break;
                    case InputOp.PLAY:
                        if (op.Id != null && (op.I < 0 || op.I >= b.Hand.Count || b.Hand[op.I] != op.Id))
                            return r.Fail(b.Input.Digests.Count - 1, $"{k}번째 수 — 손 {op.I}번이 「{op.Id}」 가 아니다({(op.I >= 0 && op.I < b.Hand.Count ? b.Hand[op.I] : "없음")})");
                        b.PlayCard(op.I, op.T, new PlayOpts { Choice = op.Choice, Spend = op.Spend, Stage = op.Stage, Ally = op.Ally, Discard = op.Discard?.ToList(), Spread = op.Spread?.ToList() });
                        break;
                    default: return r.Fail(-1, $"{k}번째 수 — 모르는 종류 「{op.K}」");
                }
                if (op.K == InputOp.END)
                {
                    int i = b.Input.Digests.Count - 1;
                    if (i < tape.Digests.Count && tape.Digests[i] != b.Input.Digests[i]) return r.Fail(i, $"{i}번째 턴 지문이 다르다({k}번째 수 뒤)");
                }
            }
            if (tape.Digests.Count > 0 && b.Input.Digests[0] != tape.Digests[0]) return r.Fail(0, "연 직후 지문이 다르다");
            if (b.Input.Digests.Count != tape.Digests.Count) return r.Fail(Math.Min(b.Input.Digests.Count, tape.Digests.Count), $"턴 지문 수가 다르다({tape.Digests.Count} → {b.Input.Digests.Count})");
            b.Input.Close(b);
            if (tape.End != null && tape.End != b.Input.End) return r.Fail(b.Input.Digests.Count, "끝 지문이 다르다");
            return r;
        }

        /// <summary>
        /// 화면 · 봇이 수 사이에 부르는 조회 · 미리보기를 한 바퀴(「미리보기를 끼워도 같은 결과」 시험 — Replay 의 before 로).
        /// 카드마다 낼 수 있나 · 비용 · 피해 미리보기(적마다) · 파티 미리보기 · 고르기 후보 · 신탁 선택지, 사도마다 고학년 미리보기 · 변신 · 강화 · 상태,
        /// 적마다 예고 피해 · 강인도 · 상태, 봇의 둘 수 전부와 앞 몇 수를 복사본에 둬 보기.
        /// </summary>
        public static void Poke(Battle b, Bots bots = null, int botMoves = 4)
        {
            if (b.Over != null) return;
            var foes = b.AliveEnemies();
            for (int i = 0; i < b.Hand.Count; i++)
            {
                var id = b.Hand[i];
                b.CanPlay(id, false, i); b.CostOf(id, i); b.CostUpWhy(id); b.MarkOf(id);
                foreach (var e in foes) b.PreviewCard(i, e.Idx);
                b.PreviewCard(i, 0); b.PreviewPartyOf(i, 0);
                b.DiscardChoice(i); b.SpendChoices(id); b.ShieldChoices(id); b.StageChoices(id);
                if (b.GlowOf(id) != null) b.EpiphanyOptions(id);
            }
            foreach (var u in b.Party)
            {
                b.CanUlt(u.Key); b.UltCostOf(u.Key);
                foreach (var e in foes) b.PreviewUlt(u.Key, e.Idx);
                b.FormOf(u.Key); b.PowersOf(u.Key); b.EmpowerOf(u.Key); b.StatusViews(u); b.AtkNow(u); b.DefNow(u); b.StatMod(u, "crit");
            }
            b.StatusViews(b.Pool); b.PowersOf();
            foreach (var e in b.Enemies) { b.IntentHit(e); b.ChargeHit(e); b.ToughViewOf(e); b.StatusViews(e); b.RushOf(e); b.SeizedOf(e); b.SeizeLeft(e); }
            if (bots == null) return;
            var moves = bots.Moves(b);
            for (int k = 0; k < moves.Count && k < botMoves; k++) bots.Apply(bots.CloneFor(b), moves[k]);
        }
    }

    /// <summary>입력 한 수 — K: epi(신탁 고르기 Id 카드 · I 선택지) · play(I 손 번호 · T 대상 · Id 그때 그 자리의 카드 + PlayOpts) · ult(Id 사도 · T 대상 · Stage) · end(턴 넘김).</summary>
    public sealed class InputOp
    {
        public const string EPI = "epi", PLAY = "play", ULT = "ult", END = "end";
        public string K;
        public string Id;
        public int I, T;
        public int? Choice, Spend, Stage, Ally;
        public List<string> Discard, Spread;
        /// <summary>(BattleInput.Fine) 이 수 앞의 지문.</summary>
        public string Pre;
    }

    public sealed class ReplayResult
    {
        public Battle Battle;
        public bool Same;
        /// <summary>처음 어긋난 지문 번호(0 = 연 직후, n = n번째 EndTurn 뒤) — 같으면 -1.</summary>
        public int Turn = -1;
        public string Why;
        internal ReplayResult Fail(int turn, string why) { Same = false; Turn = turn; Why = why; return this; }
    }

    public sealed partial class Battle
    {
        /// <summary>입력 테이프(BattleInput) — null 이면 적지 않는다(봇 · 미리보기 복사본).</summary>
        [JsonIgnore] public BattleInput Input;

        /// <summary>이 전투에 입력 테이프를 단다 — Battle.Start 바로 뒤에, 연 그 셋업으로. 연 직후 지문을 [0] 으로.</summary>
        public BattleInput Record(BattleSetup st)
        {
            Input = BattleInput.Of(st);
            Input.Content = Data.ContentHash;
            Input.Digests.Add(Digest());
            return Input;
        }

        /// <summary>턴을 넘긴다 — 턴 끝 효과 · 적의 차례 · 다음 턴 시작까지(EndTurnRun). 입력 테이프에 적고 턴 지문을 남긴다.</summary>
        public Battle EndTurn()
        {
            Input?.EndOp(this);
            var r = EndTurnRun();
            Input?.Digests.Add(Digest());
            return r;
        }

        /// <summary>
        /// 턴 지문 — 난수 상태 · 턴 · AP · 게이지 · 파티 몸 · 사도 · 적 · 네 더미(차례 그대로) · 겹 · 카드 상태 · 신탁 · 축복을 묶은 64비트 해시(16자).
        /// 사전은 열쇠 차례(Ordinal)로 — 넣은 차례가 런타임마다 달라도 값이 같으면 같다. 실수는 비트 그대로. 결과(Over)는 넣지 않는다(봇의 턴 끊김은 엔진 밖).
        /// </summary>
        public string Digest()
        {
            var h = new Fnv();
            h.U(Rng.State); h.I(Turn); h.I(Ap); h.I(ApJam); h.I(ApCarry); h.I(StartAp); h.I(Opening); h.I(Gauge); h.I(NextCheaper);
            h.I(PlaysTotal); h.I(UltsUsed); h.I(GoneN); h.I(SeqN); h.S(LastUlt); h.S(LastHero);
            UnitHash(h, Pool);
            foreach (var u in Party) { UnitHash(h, u); h.I(u.Atk); h.I(u.Def); h.I(u.Crit); }
            foreach (var e in Enemies)
            {
                UnitHash(h, e);
                h.D(e.Tough); h.D(e.ToughMax); h.B(e.Broken); h.B(e.BreakWait); h.B(e.Sealed); h.I(e.Step); h.I(e.RushCnt); h.I(e.UltAt); h.I(e.ReviveIn);
                if (e.Intent != null) { h.S(e.Intent.T); h.I(e.Intent.V); h.I(e.Intent.N); h.S(e.Intent.Id); } else h.S(null);
                h.L(e.Seized);
            }
            h.L(Hand); h.L(Draw); h.L(Discard); h.L(Gone); h.L(Used);
            h.M(Flash); h.M(Shin);
            foreach (var k in Keys(Stacks)) { h.S(k); h.M(Stacks[k]); }
            foreach (var k in Keys(CardSt)) { h.S(k); h.M(CardSt[k]); }
            h.M(Counts); h.M(Frozen); h.M(Bond);
            h.L(Glow.Keys.OrderBy(x => x, StringComparer.Ordinal));
            h.L(Forms.Keys.OrderBy(x => x, StringComparer.Ordinal));
            h.I(Powers.Count); h.I(Later.Count);
            return h.Hex();
        }

        static void UnitHash(Fnv h, Unit u)
        {
            h.S(u.Key); h.I(u.Hp); h.I(u.MaxHp); h.I(u.Block); h.I(u.Shield); h.B(u.Dead);
            h.M(u.Status);
            foreach (var m in u.Mods) { h.S(m.Stat); h.D(m.V); h.I(m.Left); h.B(m.Run); }
        }

        static List<string> Keys<T>(Dictionary<string, T> d) { var k = new List<string>(d.Keys); k.Sort(StringComparer.Ordinal); return k; }
    }

    /// <summary>FNV-1a 64 — 플랫폼 · 런타임이 달라도 같은 값(내장 GetHashCode 는 런타임마다 다르다).</summary>
    internal sealed class Fnv
    {
        ulong h = 14695981039346656037UL;
        public void Byte(byte b) { h ^= b; h *= 1099511628211UL; }
        public void U(ulong v) { for (int i = 0; i < 8; i++) { Byte((byte)v); v >>= 8; } }
        public void I(long v) => U(unchecked((ulong)v));
        public void B(bool v) => Byte(v ? (byte)1 : (byte)0);
        public void D(double v) => I(BitConverter.DoubleToInt64Bits(v));
        public void S(string s)
        {
            if (s == null) { I(-1); return; }
            I(s.Length);
            foreach (var c in s) { Byte((byte)c); Byte((byte)(c >> 8)); }
        }
        public void L(IEnumerable<string> l) { int n = 0; foreach (var s in l) { S(s); n++; } I(n); }
        public void M(Dictionary<string, int> d)
        {
            var k = new List<string>(d.Keys); k.Sort(StringComparer.Ordinal);
            foreach (var x in k) { S(x); I(d[x]); }
            I(k.Count);
        }
        public void M(Dictionary<string, string> d)
        {
            var k = new List<string>(d.Keys); k.Sort(StringComparer.Ordinal);
            foreach (var x in k) { S(x); S(d[x]); }
            I(k.Count);
        }
        public string Hex() => h.ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 규칙 · 콘텐츠 지문 — 다시 돌리기가 같은 규칙 값 · 같은 콘텐츠로 돌았는지.
    /// Rules: R 과 BossUlt 의 정적 필드 전부(const · readonly 표의 속 · 바꿀 수 있는 손잡이 FLOOR_NODES …)를 이름 차례로 적어 해시.
    ///   코드에서 지운 꺼진 손잡이는 RETIRED 에 그때 값 그대로 남겨 같은 자리에 적는다 — 판정이 같은 정리로 지문(공개판 · 검증기 · 옛 테이프)이 갈리지 않게.
    /// Content: GameData 가 폴더에서 읽은 파일들(경로 + 글, \r 뺌)을 읽은 차례로 해시(GameData.ContentHash).
    /// </summary>
    public static class Fingerprint
    {
        static readonly Type[] RULE_TYPES = { typeof(R), typeof(BossUlt) };
        /// <summary>지운 규칙 칸 — (형, 이름, 지문 글). R.GRADE_ON: 학점제 학년 스위치(2026-10-09 끔 · 2026-10-22 코드에서 지움, 값 false).</summary>
        static readonly (Type t, string name, string text)[] RETIRED = { (typeof(R), "GRADE_ON", "F") };

        public static string Rules() => Hash(RulesText());

        /// <summary>지문의 바탕 글(한 줄에 필드 하나) — 두 런타임이 어디서 갈렸는지 볼 때.</summary>
        public static string RulesText()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in RULE_TYPES)
            {
                var lines = new List<(string name, string text)>();
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                    if (f.FieldType.Name.StartsWith("ConcurrentDictionary", StringComparison.Ordinal)) continue;   // 메모 캐시(R.MAX_KEYS — 부를수록 찬다) — 규칙 값이 아니다
                    var v = new System.Text.StringBuilder();
                    Write(v, f.IsLiteral ? f.GetRawConstantValue() : f.GetValue(null), 0);
                    lines.Add((f.Name, v.ToString()));
                }
                foreach (var x in RETIRED) if (x.t == t) lines.Add((x.name, x.text));
                foreach (var (name, text) in lines.OrderBy(l => l.name, StringComparer.Ordinal))
                    sb.Append(t.Name).Append('.').Append(name).Append('=').Append(text).Append('\n');
            }
            return sb.ToString();
        }

        static void Write(System.Text.StringBuilder sb, object v, int depth)
        {
            if (v == null) { sb.Append('~'); return; }
            if (depth > 8) { sb.Append('…'); return; }
            switch (v)
            {
                case string s: sb.Append('"').Append(s).Append('"'); return;
                case double d: sb.Append("d").Append(BitConverter.DoubleToInt64Bits(d).ToString("x", CultureInfo.InvariantCulture)); return;
                case float fl: sb.Append("f").Append(BitConverter.DoubleToInt64Bits(fl).ToString("x", CultureInfo.InvariantCulture)); return;
                case bool b: sb.Append(b ? "T" : "F"); return;
                case Enum e: sb.Append(e.ToString()); return;
                case IFormattable n when v.GetType().IsPrimitive: sb.Append(n.ToString(null, CultureInfo.InvariantCulture)); return;
                case Delegate _: sb.Append("fn"); return;
                case IDictionary dict:
                    {
                        var ents = new List<(string k, object v)>();
                        foreach (DictionaryEntry de in dict) { var ks = new System.Text.StringBuilder(); Write(ks, de.Key, depth + 1); ents.Add((ks.ToString(), de.Value)); }
                        sb.Append('{');
                        foreach (var (k, x) in ents.OrderBy(x => x.k, StringComparer.Ordinal)) { sb.Append(k).Append(':'); Write(sb, x, depth + 1); sb.Append(','); }
                        sb.Append('}');
                        return;
                    }
                case IEnumerable en:
                    sb.Append('[');
                    foreach (var x in en) { Write(sb, x, depth + 1); sb.Append(','); }
                    sb.Append(']');
                    return;
            }
            var ty = v.GetType();
            if (!ty.IsPrimitive)
            {   // 객체 · 튜플 — 공개 필드를 이름 차례로
                sb.Append('(');
                foreach (var f in ty.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name, StringComparer.Ordinal)) { sb.Append(f.Name).Append(':'); Write(sb, f.GetValue(v), depth + 1); sb.Append(','); }
                sb.Append(')');
                return;
            }
            sb.Append(v);
        }

        public static string Hash(string s) { var h = new Fnv(); h.S(s); return h.Hex(); }
    }
}
