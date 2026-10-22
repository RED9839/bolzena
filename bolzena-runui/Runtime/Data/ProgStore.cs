using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 사도 성장 — 잠재력 하나만(2026-10-21 밤 사용자 최종 결정) + 도전 사다리(난이도 단계) — 화면 쪽 손잡이.
    /// 설계: bolzena-unity/Docs/성장_설계_카제나판.md 맨 위 결정 상자 · §12. 서버: /api/prog(Web/functions/api/prog · lib/prog.js · migrations/0010).
    /// 레벨 · 진급 · 교주 레벨 · 던전 · 왕사탕 · 모카롱 · 교본 · 판 밖 골드는 없앴다 — 사도 능력치는 공개판 기본치 그대로, 잠재력은 그 위 덤.
    ///
    /// **스위치(On)** — 콘텐츠에 성장 표(prog)가 있을 때만 켠다. 꺼져 있으면 단계 고르기 · 단계 표시 · 잠재력 · 보상 화면이 하나도 안 보이고
    /// 판은 지금과 완전히 같다(단계 0 · 성장 없음 → 엔진이 셋업에 칸도 안 만든다). 공개판 content-v2 에는 성장 표가 없다 — 켜는 날 넣는다.
    ///
    /// 켜졌을 때:
    /// - 도전 사다리: 고를 수 있는 단계 = Tiers.Open(최고 완주 단계). 최고 완주 단계는 로그인이면 서버 계정(bestTier), 아니면 이 기기(PlayerPrefs bz.tier.best).
    /// - 잠재력: 로그인 + 서버 켬이면 서버 값, 아니면 잠재력 없음(공개판 기본치 — 로그인 안 하면 성장 없음, 설계 G10).
    ///   판을 열 때 편성 사도 셋의 잠재력을 Run.ApplyProg 로 판에 넣는다.
    /// - 판 끝: 서버에 판 결과만 보낸다(POST /api/prog/earn) — 잠재력 재료 보상은 서버가 계산(단계 배율 · 진행 비율 · 주간 상한). 결과는 LastEarn(판 끝 화면이 그린다).
    /// -save 로 따로 둔 시험 실행은 그 이름을 붙인 키를 쓴다.
    /// </summary>
    public static class ProgStore
    {
        /// <summary>스위치 — 콘텐츠에 성장 표가 있다.</summary>
        public static bool On => Flow.Me != null && Flow.Me.P?.Data?.Prog != null;
        public static ProgTable Table => Flow.Me?.P?.Data?.Prog;

        // ── 보기 전용 잠재력 표(2026-10-22 사용자 — 성장이 꺼진 공개판 · 로그인 전에도 사도 상세 「잠재력」 탭을 보인다) ──
        //   콘텐츠에 성장 표가 있으면 그것(켜는 날), 없으면 runui Resources/RunUI/pot_preview.json(Tools~/make_pot_preview.mjs — staging 성장 표 · 서버 비용 사본).
        //   화면만 읽는다 — 엔진 판정 · 서버 · 콘텐츠/규칙 지문과 상관없다(콘텐츠 폴더에 넣지 않는다).
        static ProgTable preview; static Dictionary<string, int[][]> previewCost; static bool previewTried;
        static void LoadPreview()
        {
            if (previewTried) return;
            previewTried = true;
            try
            {
                var ta = Resources.Load<TextAsset>("RunUI/pot_preview");
                if (ta == null) return;
                var j = JObject.Parse(ta.text);
                preview = GameData.FromJson<ProgTable>(new JObject { ["potential"] = j["potential"], ["heroes"] = j["heroes"] }.ToString());
                previewCost = j["cost"]?.ToObject<Dictionary<string, int[][]>>();
            }
            catch (Exception e) { Debug.LogWarning("[Prog] 잠재력 미리 보기 표를 못 읽음 — " + e.Message); preview = null; }
        }
        /// <summary>화면에 그릴 잠재력 표 — 콘텐츠 성장 표(켬) 또는 미리 보기 사본(꺼짐). 둘 다 없으면 null(탭 없음).</summary>
        public static ProgTable ViewTable { get { if (Table != null) return Table; LoadPreview(); return preview; } }
        /// <summary>그 칸을 L 단계로 올리는 마시멜로(미리 보기 사본 — 서버 POT_COST 와 같은 값). 없으면 null.</summary>
        public static Dictionary<string, int> PreviewCost(string node, int L)
        {
            LoadPreview();
            if (previewCost == null || !previewCost.TryGetValue(node, out var rows) || L < 1 || L > rows.Length) return null;
            var o = new Dictionary<string, int>();
            for (int g = 0; g < 3 && g < rows[L - 1].Length; g++) if (rows[L - 1][g] > 0) o[MM[g].key] = rows[L - 1][g];
            return o;
        }

        /// <summary>서버의 내 성장(GET /api/prog 꼴) — 로그인 안 했거나 서버가 끔이면 null.</summary>
        public static JObject View { get; private set; }
        public static bool Have => View != null;
        /// <summary>성장 · 최고 단계가 바뀌었다(로비 · 도감이 다시 그린다).</summary>
        public static event Action Changed;
        /// <summary>마지막 판 끝 보상(earn 응답) — 판 끝 화면. 보내는 중이면 Pending.</summary>
        public static JObject LastEarn { get; private set; }
        public static bool EarnPending { get; private set; }
        public static string EarnError { get; private set; }

        static string Tail { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-save"); return i >= 0 && i < a.Length - 1 ? "." + a[i + 1] : ""; } }

        // ── 도전 사다리(난이도 단계) ──
        /// <summary>완주한 가장 높은 단계(0 = 없음) — 로그인이면 서버, 아니면 이 기기.</summary>
        public static int BestTier => Have ? (int?)View["bestTier"] ?? 0 : Mathf.Clamp(PlayerPrefs.GetInt("bz.tier.best" + Tail, 0), 0, Tiers.MAX);
        /// <summary>고를 수 있는 가장 높은 단계.</summary>
        public static int OpenTier => Tiers.Open(BestTier);
        /// <summary>단계 고르기의 기본 — 연 가장 높은 단계.</summary>
        public static int DefaultTier => OpenTier;
        /// <summary>데모 · 캡처 — 이 기기 최고 완주 단계를 정한다.</summary>
        public static void SetLocalBest(int t) { PlayerPrefs.SetInt("bz.tier.best" + Tail, Mathf.Clamp(t, 0, Tiers.MAX)); PlayerPrefs.Save(); Changed?.Invoke(); }

        public static string FoeX(int t) => "×" + Tiers.Scale(t).hp.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        // ── 모험 끝 마시멜로 보상(서버 Web/lib/prog.js MATS · progressOf 와 같은 표 — 화면 미리 보기용, 실제 값은 서버가 준다) ──
        /// <summary>단계별 완주 한 판의 마시멜로 [하급, 중급, 상급](편성 크기와 상관없이 판마다). 칸 0 은 안 쓴다.</summary>
        public static readonly int[][] MATS = { null, new[] { 12, 28, 24 }, new[] { 12, 28, 24 }, new[] { 4, 34, 36 }, new[] { 4, 34, 36 }, new[] { 3, 16, 46 }, new[] { 3, 16, 50 }, new[] { 3, 14, 54 }, new[] { 3, 14, 60 }, new[] { 3, 14, 64 } };
        /// <summary>그 단계를 완주한 한 판의 등급 g(1~3) 마시멜로 수.</summary>
        public static int WinMat(int tier, int g) => tier >= 1 && tier < MATS.Length ? MATS[tier][g - 1] : 0;

        // ── 잠재력 ──
        /// <summary>사도 하나의 서버 성장 줄(없으면 null).</summary>
        public static JObject HeroOf(string coreId) => Have ? View["heroes"]?[coreId] as JObject : null;

        /// <summary>그 사도의 잠재력 칸 단계(서버 pot — 없으면 빈 표).</summary>
        public static Dictionary<string, int> PotOf(string coreId)
        {
            var o = new Dictionary<string, int>();
            if (HeroOf(coreId)?["pot"] is JObject p) foreach (var kv in p) if (kv.Value?.Type == JTokenType.Integer && (int)kv.Value > 0) o[kv.Key] = (int)kv.Value;
            return o;
        }
        /// <summary>그 사도의 잠재력 10칸 서버 줄(GET /api/prog heroes[사도].potential — 칸마다 lv · max · after · next{items} · ok · why · locked). 없으면 null.</summary>
        public static JObject PotNode(string coreId, string node)
        {
            if (!(HeroOf(coreId)?["potential"] is JArray a)) return null;
            foreach (var x in a) if (x is JObject o && (string)o["id"] == node) return o;
            return null;
        }
        /// <summary>그 사도의 잠재력 진행 — 연 칸 수 · 단계 합 · 단계 합 최대(표 기준).</summary>
        public static (int open, int lv, int max) PotProgress(string coreId)
        {
            var t = ViewTable; if (t == null) return (0, 0, 0);
            var pot = PotOf(coreId);
            int open = 0, lv = 0;
            foreach (var n in t.Potential) if (pot.TryGetValue(n.Id, out var l) && l > 0) { open++; lv += Math.Min(n.Max, l); }
            return (open, lv, t.Potential.Sum(n => n.Max));
        }

        /// <summary>그 사도들의 성장 상태(엔진 ProgState — 잠재력) — 없는 사도는 잠재력 없음. 프론티어는 서버가 떠 둔 값을 따로 쓴다.</summary>
        public static ProgState StateFor(IEnumerable<string> heroes)
        {
            var s = new ProgState();
            foreach (var k in heroes) s.Heroes[k] = new ProgHero { Pot = PotOf(k) };
            return s;
        }

        /// <summary>그 사도의 지금 성장 전투 몫(카드군 % · 능력치 — 판 밖 카드 상세 실제 수치). 꺼져 있으면 null.</summary>
        public static ProgMods ModsOf(string coreId)
        {
            var d = Flow.Me?.P?.Data;
            if (!On || d?.Hero(coreId) == null) return null;
            var pb = Prog.Build(d, d.Prog, StateFor(new[] { coreId }), new[] { coreId });
            return pb.Mods.TryGetValue(coreId, out var m) ? m : null;
        }

        /// <summary>그 카드에 붙는 카드군 배율 덤(잠재력 02 · 03 · 04 — 엔진 Battle.ProgCardOf 와 같은 갈래): 사도 카드면 고유(생성 포함) · 기본, 중립이면 교주.</summary>
        public static double CardBonus(ProgMods m, CardDef def, bool neutralOwned)
        {
            if (m == null || def == null || def.IsStatusCard || def.IsCurse) return 0;
            if (def.Hero != null) return def.Unique || def.Token ? m.Unique : m.Basic;
            return neutralOwned ? m.Neutral : 0;
        }

        /// <summary>그 사도의 지금 성장 능력치(판 장비 빼고 — 공개판 기본치 + 잠재력 05 · 06) — 꺼져 있으면 null.</summary>
        public static ProgSum SumOf(string coreId)
        {
            var d = Flow.Me?.P?.Data;
            if (!On || d?.Hero(coreId) == null) return null;
            var pb = Prog.Build(d, d.Prog, StateFor(new[] { coreId }), new[] { coreId });
            return pb.Sum.TryGetValue(coreId, out var s) ? s : null;
        }

        /// <summary>새 판에 단계 · 잠재력을 건다(RunPort.NewRun — Run.New 뒤). 꺼져 있으면 아무것도 안 한다(지금 판과 같다).</summary>
        public static void ApplyTo(Run run, int tier)
        {
            if (!On || run == null) return;
            var why = run.SetTier(Mathf.Clamp(tier, 1, Tiers.MAX));
            if (why != null) Debug.LogWarning("[Prog] 단계를 못 정함 — " + why);
            run.ApplyProg(Prog.Build(run.Data, run.Data.Prog, StateFor(run.S.Party), run.S.Party));
            Debug.Log($"[Prog] 판 — 단계 {run.S.Tier}(적 {FoeX(run.S.Tier)}) · {string.Join(" · ", run.S.Party.Select(k => $"{k} 잠재력 {PotProgress(k).lv}"))} · 파티 HP {run.S.PartyMaxHp}");
        }

        // ── 서버 ──
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            Online.Changed -= OnOnline;
            Online.Changed += OnOnline;
        }
        static bool wasReady;
        static void OnOnline()
        {
            bool ready = Online.Ready;
            if (ready && !wasReady) Refresh();
            if (!ready && wasReady) { View = null; Changed?.Invoke(); }
            wasReady = ready;
        }

        /// <summary>서버 성장을 다시 받는다(로그인했고 켜졌을 때만).</summary>
        public static void Refresh(Action done = null)
        {
            if (!On || !Online.Ready) { done?.Invoke(); return; }
            Online.Call("GET", "/api/prog", null, r =>
            {
                if (r.Ok && r.Json?["on"]?.Type == JTokenType.Boolean) Take(r.Json);
                else if (r.Code == 404) { View = null; Changed?.Invoke(); Debug.Log("[Prog] 서버 성장이 꺼져 있습니다(prog off) — 잠재력 없이"); }
                done?.Invoke();
            });
        }

        static void Take(JObject view)
        {
            View = view;
            Changed?.Invoke();
        }

        /// <summary>판 끝 — 판 결과를 서버에 보내 잠재력 재료를 받는다(로그인 · 켬일 때). 로그인 안 했으면 이 기기 최고 단계만 올린다.</summary>
        public static void Earn(Run run, string result, Action done = null)
        {
            LastEarn = null; EarnError = null; EarnPending = false;
            if (!On || run?.S == null || run.S.Tier <= 0) { done?.Invoke(); return; }
            if (result != "win" && run.S.Hist.Count == 0) { done?.Invoke(); return; }   // 싸움 없이 버린 판 — 보상 없음
            var S = run.S;
            bool win = result == "win";
            if (win && !Have && S.Tier > BestTier) { PlayerPrefs.SetInt("bz.tier.best" + Tail, S.Tier); PlayerPrefs.Save(); Changed?.Invoke(); }
            if (!Online.Ready || !Have) { EarnError = Online.Ready ? "서버 성장이 꺼져 있다" : null; done?.Invoke(); return; }
            int min = 0;
            if (S.Rec?.Began != null && DateTime.TryParse(S.Rec.Began, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var b0))
                min = Mathf.Clamp((int)Math.Round((DateTime.UtcNow - b0).TotalMinutes), 0, 24 * 60);
            var body = new JObject
            {
                ["ref"] = $"run-{S.Seed}-{S.Hist.Count}-{S.Step}",   // 같은 판을 두 번 받지 못한다(씨앗은 판마다 무작위)
                ["tier"] = S.Tier, ["result"] = win ? "win" : result == "abandon" ? "abandon" : "lose",
                ["floor"] = Mathf.Clamp(S.Floor + 1, 1, 2), ["min"] = min, ["party"] = new JArray(S.Party.ToArray()),
            };
            EarnPending = true;
            Online.Call("POST", "/api/prog/earn", body.ToString(Newtonsoft.Json.Formatting.None), r =>
            {
                EarnPending = false;
                if (r.Ok && r.Json != null)
                {
                    LastEarn = r.Json;
                    if (r.Json["prog"] is JObject p) Take(p);
                    Debug.Log($"[Prog] 판 끝 보상 — {r.Text}");
                }
                else { EarnError = r.Str("why") ?? Online.ErrorText(r); Debug.Log($"[Prog] 판 끝 보상 실패 {r.Code} {r.Text}"); }
                EarnDone?.Invoke();
                done?.Invoke();
            });
        }
        /// <summary>보상 응답이 왔다(판 끝 화면이 그 자리를 채운다).</summary>
        public static event Action EarnDone;

        /// <summary>데모 · 캡처 — 서버 없이 보상 응답 · 성장 꼴을 넣는다.</summary>
        public static void DemoSet(JObject view, JObject earn) { if (!Demo.Active) return; View = view; LastEarn = earn; EarnPending = false; EarnError = null; Changed?.Invoke(); EarnDone?.Invoke(); }

        /// <summary>잠재력 칸 하나 한 단계(POST /api/prog/potential) — 조건 · 값은 서버가 판단(lib/prog.js potCheck).</summary>
        public static void Potential(string hero, string node, Action<Online.Resp> done) => Post("/api/prog/potential", new JObject { ["hero"] = hero, ["node"] = node }, done);

        static void Post(string path, JObject body, Action<Online.Resp> done)
        {
            if (Demo.Active && DemoPost != null) { var fake = DemoPost(path, body); if (fake?.Json?["prog"] is JObject fp) Take(fp); done?.Invoke(fake); return; }
            Online.Call("POST", path, body.ToString(Newtonsoft.Json.Formatting.None), r =>
            {
                if (r.Ok && r.Json?["prog"] is JObject p) Take(p);
                done?.Invoke(r);
            });
        }
        /// <summary>데모 — 서버 대신 응답을 짓는다(잠재력 칸 열기 캡처).</summary>
        public static Func<string, JObject, Online.Resp> DemoPost;

        // ── 재화 — 잠재력 재료 = 마시멜로 하급 · 중급 · 상급(2026-10-22 사용자 — 성격 재료 · 특수 재료를 셋으로 줄였다). 이름은 여기 MM 과 서버 Web/lib/prog.js MM 두 곳 ──
        public static readonly (string key, string name, string shortName)[] MM = { ("mm1", "하급 마시멜로", "하급"), ("mm2", "중급 마시멜로", "중급"), ("mm3", "상급 마시멜로", "상급") };
        /// <summary>재화 그림(RunArt/Currency/<열쇠> — 원작 선택 상자 SelectBox_Marshmallow1~3 의 「?」 배지를 지운 사본, runui Tools~/copy_assets.py)이 있는 열쇠 — 글 속 아이콘 <sprite name="cur_열쇠">(CardTerms.Icons).</summary>
        public static readonly string[] ICON_KEYS = { "mm1", "mm2", "mm3" };
        /// <summary>등급 색(칩 바탕) — 하급 하늘 · 중급 금 · 상급 무지개(분홍).</summary>
        public static Color GradeColor(string key) => key == "mm1" ? new Color(0.55f, 0.84f, 0.88f) : key == "mm2" ? new Color(0.95f, 0.76f, 0.30f) : key == "mm3" ? new Color(0.96f, 0.56f, 0.70f) : Theme.Edge;
        /// <summary>글 속 재화 아이콘 태그(그림이 없으면 빈 글).</summary>
        public static string Ico(string key) => CardTerms.HasIcon("cur_" + key) ? $"<sprite name=\"cur_{key}\">" : "";
        /// <summary>아이콘 + 이름 — 보상 · 비용 글.</summary>
        public static string NameIco(string key) { var i = Ico(key); return i.Length > 0 ? i + " " + NameOf(key) : NameOf(key); }
        public static string NameOf(string key) { foreach (var m in MM) if (m.key == key) return m.name; return key; }
        /// <summary>짧은 이름 — 「하급」 · 「중급」 · 「상급」.</summary>
        public static string ShortOf(string key) { foreach (var m in MM) if (m.key == key) return m.shortName; return key; }
        /// <summary>지갑 값(로그인 안 했으면 0).</summary>
        public static int Wallet(string key) => (int?)View?["wallet"]?[key] ?? 0;

        // ── 글 ──
        public static int IntOf(JToken t, string k) => (int?)t?[k] ?? 0;
        public static string N0(int v) => v.ToString("N0");
    }
}
