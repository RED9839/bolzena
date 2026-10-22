using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 사도 저장 슬롯의 기기 쪽(프론티어 — bolzena-unity/Docs/프론티어_설계.md §5.3 · 서버 문서 §1 「사도 슬롯」).
    ///
    /// - 사본(Copies): 서버에 올린 칸의 기기 사본 — 서버 id · 사도 · 저장 차례(N) · 이름 · 슬롯 글(HeroSlot.Json) · 상태(pending 검증 대기 · ok 확인 · bad 문제) · 까닭 · 지문.
    ///   사도마다 10칸까지(2026-10-19 사용자 — PerHero, 서버 LIMIT.SLOTS_PER_HERO 와 같다 · 목록 응답 max.perHero 로 맞춘다).
    ///   빈 칸을 그려 두지 않는다 — 있는 칸만 보이고 끝에 「새로 저장 n/10」 하나. 이미 10칸을 넘게 가진 사도는 읽기 · 덮어쓰기 · 지우기만(새로 저장 막힘).
    ///   서버가 닿으면 서버 목록으로 갈아 끼우고(SyncFrom), 닿지 않으면 이것만 저장함에 보인다.
    /// - 기다림(Waiting): 마지막 완주 판의 사도 셋(HeroSlot.Of 로 뜬 것) — 판 끝 화면에서 로그인 전이었거나 서버가 없었어도
    ///   로그인한 뒤 저장함에서 마저 저장할 수 있게 다음 완주까지 들고 있는다(웹은 로그인이 페이지를 다시 띄운다).
    /// - 파일: persistentDataPath/hero-slots(-save 꼬리)/ copies.json · waiting.json — 판 세이브(bolzena-run.json)와 섞지 않는다.
    /// </summary>
    public static class SlotStore
    {
        public sealed class Entry
        {
            /// <summary>서버가 준 칸 id(옛 사본처럼 없으면 음수 임시 번호 — 서버 요청에는 못 쓴다).</summary>
            public long Id;
            public string Hero;
            /// <summary>그 사도의 저장 차례(0부터) — 이름이 없을 때 「〇〇 저장 N+1」.</summary>
            public int N;
            /// <summary>칸 이름(선택 · 16자). 없으면 null.</summary>
            public string Name;
            public string Json;
            public string Status = "pending";
            public string Why, Hash;
            public long Created, Updated;
            /// <summary>서버가 검증한 이 칸(지금 글 — 지문이 같은 것)의 지금 시즌 최고 점수(GET /api/slots 의 best). 기기에는 적지 않는다.</summary>
            [JsonIgnore] public int? Best;
            /// <summary>그 최고의 상태 — verified · pending(검증 전) · skipped(검증 안 함). 서버는 검증된 것을 먼저 준다.</summary>
            [JsonIgnore] public string BestStatus;
            [JsonIgnore] HeroSlot parsed;
            [JsonIgnore] public HeroSlot Slot { get { if (parsed == null && Json != null) { try { parsed = HeroSlot.Parse(Json); } catch (Exception) { } } return parsed; } }
            [JsonIgnore] public bool OnServer => Id > 0;
            /// <summary>보이는 이름 — 이름이 없으면 「〇〇 저장 n」.</summary>
            [JsonIgnore] public string Label => !string.IsNullOrEmpty(Name) ? Name : $"{Roster.OfCore(Hero)?.ko ?? Hero} 저장 {N + 1}";
            public void SetJson(string json) { Json = json; parsed = null; }
        }

        public sealed class Waiting
        {
            public string Ended, Village;
            public long Seed;
            public List<string> Party = new List<string>();
            /// <summary>사도 → 슬롯 글.</summary>
            public Dictionary<string, string> Slots = new Dictionary<string, string>();
            /// <summary>사도 → 저장한 칸 id(저장 안 했으면 없음).</summary>
            public Dictionary<string, long> SavedId = new Dictionary<string, long>();
            [JsonIgnore] public bool AllSaved => Party.All(h => SavedId.ContainsKey(h));
        }

        static string Tail { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-save"); return i >= 0 && i < a.Length - 1 ? "." + a[i + 1] : ""; } }
        static string Dir => Path.Combine(Application.persistentDataPath, "hero-slots" + Tail);
        static string CopiesPath => Path.Combine(Dir, "copies.json");
        static string WaitingPath => Path.Combine(Dir, "waiting.json");

        static List<Entry> copies;
        static Waiting waiting;
        static bool waitingRead;

        static T Read<T>(string path) where T : class
        {
            try { return File.Exists(path) ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) : null; }
            catch (Exception e) { Debug.LogWarning("[SlotStore] 읽지 못함 " + path + " — " + e.Message); return null; }
        }
        static void Write(string path, object v)
        {
            try { Directory.CreateDirectory(Dir); File.WriteAllText(path, JsonConvert.SerializeObject(v, Formatting.Indented)); }
            catch (Exception e) { Debug.LogWarning("[SlotStore] 쓰지 못함 " + path + " — " + e.Message); }
        }

        // ── 사본 ──
        public static List<Entry> Copies
        {
            get
            {
                if (copies != null) return copies;
                copies = Read<List<Entry>>(CopiesPath) ?? new List<Entry>();
                // 0005 전 사본(id 없음) — 서로 갈리게 음수 임시 번호(서버에 닿으면 서버 목록으로 갈아 끼운다)
                for (int i = 0; i < copies.Count; i++) if (copies[i].Id == 0) copies[i].Id = -(i + 1);
                return copies;
            }
        }
        public static Entry Get(long id) => Copies.FirstOrDefault(e => e.Id == id);
        public static IEnumerable<Entry> Of(string hero) => Copies.Where(e => e.Hero == hero).OrderBy(e => e.N);
        /// <summary>저장한 칸이 있는 사도들(로스터 이름 차례).</summary>
        public static List<string> Heroes => Copies.Select(e => e.Hero).Distinct().OrderBy(h => Roster.OfCore(h)?.ko ?? h, StringComparer.Ordinal).ToList();

        public static void Put(Entry e)
        {
            Copies.RemoveAll(x => x.Id == e.Id);
            Copies.Add(e);
            Write(CopiesPath, Copies);
        }
        public static void Remove(long id)
        {
            Copies.RemoveAll(x => x.Id == id);
            Write(CopiesPath, Copies);
        }
        /// <summary>이번 실행에서 서버 목록으로 맞췄나 — 그러면 칸별 최고는 서버의 검증된 값(Entry.Best)을 쓴다.</summary>
        public static bool FromServer { get; private set; }

        public static void ClearCopies()
        {
            copies = new List<Entry>();
            FromServer = false;
            try { if (File.Exists(CopiesPath)) File.Delete(CopiesPath); } catch { }
        }

        /// <summary>서버 칸 한 줄(GET /api/slots · POST · PUT · PATCH 응답) → 사본. 슬롯 글이 없는 응답(PATCH)이면 json 을 넘긴다.</summary>
        public static Entry FromRow(JObject r, string json = null)
        {
            try { if (r["slot"] is JObject so) json = HeroSlot.Parse(so.ToString(Formatting.None)).Json(); } catch (Exception) { }
            string S(string k) => r[k]?.Type == JTokenType.String ? (string)r[k] : null;
            return new Entry
            {
                Id = (long?)r["id"] ?? 0, Hero = S("hero"), N = (int?)r["n"] ?? 0, Name = S("name"), Json = json, Status = S("status") ?? "pending",
                Why = S("why"), Hash = S("hash"), Created = (long?)r["created"] ?? 0, Updated = (long?)r["updated"] ?? 0,
                Best = r["best"] is JObject b && b["score"]?.Type == JTokenType.Integer ? (int)b["score"] : (int?)null,
                BestStatus = r["best"] is JObject b2 && b2["status"]?.Type == JTokenType.String ? (string)b2["status"] : "verified",
            };
        }

        /// <summary>서버 목록(GET /api/slots 의 slots)으로 사본을 갈아 끼운다 — 서버가 진실.</summary>
        public static void SyncFrom(JArray rows)
        {
            copies = rows.OfType<JObject>().Select(r => FromRow(r)).ToList();
            FromServer = true;
            Write(CopiesPath, copies);
        }

        // ── 차례(저장함 · 저장 데이터 창이 같이 쓴다 — 기기에 기억) ──
        public enum Sort { Recent, Score }
        public static Sort Order
        {
            get => PlayerPrefs.GetInt("bz.slots.sort" + Tail, 0) == 1 ? Sort.Score : Sort.Recent;
            set { PlayerPrefs.SetInt("bz.slots.sort" + Tail, value == Sort.Score ? 1 : 0); PlayerPrefs.Save(); }
        }
        public static string OrderKo(Sort s) => s == Sort.Score ? "점수순" : "최신순";
        /// <summary>최신순(마지막 저장 · 덮어쓰기 때) 또는 점수순(최고 점수 — 없는 칸은 뒤, 그다음 최신).</summary>
        public static List<Entry> Sorted(IEnumerable<Entry> list, Sort s, Func<Entry, int?> best)
            => s == Sort.Score
                ? list.OrderByDescending(e => best(e) ?? -1).ThenByDescending(e => e.Updated).ThenByDescending(e => e.Id).ToList()
                : list.OrderByDescending(e => e.Updated).ThenByDescending(e => e.Id).ToList();

        // ── 기다림(마지막 완주 판) ──
        public static Waiting Wait
        {
            get
            {
                if (!waitingRead) { waitingRead = true; waiting = Read<Waiting>(WaitingPath); }
                return waiting;
            }
        }

        /// <summary>완주한 판을 끝낼 때(Flow.End clear) — 사도 셋을 떠 둔다. 뜰 수 없는 사도는 건너뛴다.</summary>
        public static Waiting HoldClear(Run run)
        {
            if (run?.S == null || run.S.Done != "clear") return null;
            var w = new Waiting { Ended = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm") + "Z", Seed = run.S.Seed, Village = run.S.Village };
            foreach (var h in run.S.Party)
            {
                if (HeroSlot.WhyNot(run, h) != null) continue;
                try
                {
                    var s = HeroSlot.Of(run, h, w.Ended);
                    w.Party.Add(h);
                    w.Slots[h] = s.Json();
                }
                catch (Exception e) { Debug.LogWarning($"[SlotStore] {h} 슬롯을 뜨지 못함 — {e.Message}"); }
            }
            waiting = w; waitingRead = true;
            Write(WaitingPath, w);
            Debug.Log($"[SlotStore] 완주 판 사도 {w.Party.Count}명을 저장 기다림에 둠 — 씨앗 {w.Seed}");
            return w;
        }

        public static void MarkSaved(string hero, long id)
        {
            if (Wait == null || !Wait.Slots.ContainsKey(hero)) return;
            Wait.SavedId[hero] = id;
            Write(WaitingPath, Wait);
        }

        public static void DropWaiting()
        {
            waiting = null; waitingRead = true;
            try { if (File.Exists(WaitingPath)) File.Delete(WaitingPath); } catch { }
        }

        // ── 보이기 ──
        public static string StatusKo(string status) => status == "ok" ? "확인" : status == "bad" ? "문제" : "검증 대기";
        public static Color StatusColor(string status) => status == "ok" ? Theme.Good : status == "bad" ? Theme.Bad : Theme.Gold;
        public static string Date(long unix) => unix > 0 ? DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("MM-dd HH:mm") : "";
        /// <summary>칸 이름 글자(서버 lib/slot.js 와 같은 규칙 — 화면에서 먼저 막는다).</summary>
        public const int NAME_MAX = 16;
        /// <summary>사도마다 칸 수 한도(서버 목록 응답 max.perHero 가 오면 그 값).</summary>
        public static int PerHero = 10;
        /// <summary>그 사도의 칸 수 · 새로 저장할 수 있나(한도 아래).</summary>
        public static int CountOf(string hero) => Copies.Count(e => e.Hero == hero);
        public static bool CanAdd(string hero) => CountOf(hero) < PerHero;
        /// <summary>「n / 10」 — 칸 수 표시(넘으면 그대로 12 / 10).</summary>
        public static string CountKo(string hero) => $"{CountOf(hero)} / {PerHero}";
    }
}
