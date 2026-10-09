using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bolzena.Core
{
    /// <summary>
    /// 플레이 기록 v2(유니티판) — 한 판이 끝날 때(승리 · 패배 · 포기 · 메인으로) 한 장. 밸런스를 보려고 모은다(bolzena-unity/Docs/플레이 기록.md).
    /// 사람을 알아볼 정보는 없다 — 기기 무작위 익명 id(화면이 만든다, 진행 코드와 무관) · 판 내용 · 게임 버전 · 플랫폼 갈래뿐.
    /// RunRecord.Of 는 판(Run)과 화면이 주는 것(RecordMeta)만 보고 짓는 순수 함수다 — 봇 판에도 그대로 쓴다(bz records 비교).
    /// </summary>
    public sealed class PlayRecord
    {
        public string Kind = RunRecord.KIND;
        public int V = RunRecord.VERSION;
        public string App = "unity";
        /// <summary>기기 익명 id(16진 32자) · 게임 버전 · 빌드 시각(UTC).</summary>
        public string Anon, Ver, Build;
        /// <summary>web · pc · editor — 폰 · 저사양 모드.</summary>
        public string Os;
        public bool Phone, Low;
        public long Seed;
        public string Village, Nature;
        /// <summary>시작 · 끝(UTC, 분까지) · 걸린 분.</summary>
        public string Began, Ended;
        public int Min;
        /// <summary>win · lose · abandon(버리고 새 모험) · quit(메인으로 — 판은 이어할 수 있다).</summary>
        public string Result;
        /// <summary>편성 차례대로(Slot 1 = 맨 앞).</summary>
        public List<PartySlot> Party;
        /// <summary>도달한 층(1 · 2) · 싸움 세기(0..2, 3 = 보스) · 걸음 · 마지막 칸 종류.</summary>
        public int Floor, Node, Step;
        public string At;
        public int Grade, Credits;
        /// <summary>교주 보드(크레파스)에 칠한 단계 수(화면이 준다).</summary>
        public int Board;
        /// <summary>전투 시간 가운데 2배속이던 몫(0~1). 전투가 없었으면 없음.</summary>
        public double? Fast;
        public int Gold, Hp, MaxHp, Removals;
        public List<FightRecord> Fights;
        public List<PickLog> Picks;
        public List<DeckCard> Deck;
        /// <summary>사도 → 칸 → 아티팩트 id.</summary>
        public Dictionary<string, Dictionary<string, string>> Arts;
        public DeathRec Death;
        /// <summary>크기 상한 때문에 줄인 단계(0 = 그대로) — 1 일반 전투 턴 기록 · 2 진열 · 3 제시된 것 · 4 선택 기록 · 5 모든 턴 기록 · 6 덱.</summary>
        public int Trim;
    }

    public sealed class PartySlot { public string Id; public int Slot; }
    public sealed class DeckCard { public string Id; public int N, O; public string B; }
    /// <summary>쓰러진 곳 — 층 · 싸움 세기 · 종류 · 그때 적 · 파티 HP(그 싸움 시작 · 최대) · 몇 턴째.</summary>
    public sealed class DeathRec { public int F, N, Turn; public string K; public List<string> Foes; public int Hp, MaxHp; }

    /// <summary>화면이 아는 것 — 익명 id · 버전 · 플랫폼 · 끝난 시각 · 결과 · 교주 보드.</summary>
    public sealed class RecordMeta
    {
        public string Anon, Ver, Build, Os = "pc", Result;
        public bool Phone, Low;
        public DateTime Ended = DateTime.UtcNow;
        public int Board;
    }

    public static class RunRecord
    {
        public const string KIND = "bolzena-record";
        public const int VERSION = 2;
        /// <summary>한 장의 상한(바이트) — 받는 쪽(functions/api/record.js)과 같다.</summary>
        public const int MAX_BYTES = 64 * 1024;
        public static readonly string[] RESULTS = { "win", "lose", "abandon", "quit" };

        static readonly JsonSerializerSettings Js = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
        };
        // 읽을 때는 기본값을 채운다(kind · v 따위 — 빠졌으면 빈 값)
        static readonly JsonSerializerSettings JsIn = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static string Json(PlayRecord r) => JsonConvert.SerializeObject(r, Js);
        public static PlayRecord Parse(string json) => JsonConvert.DeserializeObject<PlayRecord>(json, JsIn);
        public static int Bytes(PlayRecord r) => System.Text.Encoding.UTF8.GetByteCount(Json(r));

        /// <summary>분까지 자른 UTC 시각 「yyyy-MM-ddTHH:mmZ」.</summary>
        public static string Minute(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture);

        /// <summary>판 한 장의 기록 — 크기 상한(maxBytes)을 넘으면 줄인다(Shrink).</summary>
        public static PlayRecord Of(Run run, RecordMeta m, int maxBytes = MAX_BYTES)
        {
            var S = run.S; var d = run.Data;
            m ??= new RecordMeta();
            var rec = S.Rec ?? new RecState();
            var r = new PlayRecord
            {
                Anon = m.Anon, Ver = m.Ver, Build = m.Build, Os = m.Os, Phone = m.Phone, Low = m.Low,
                Seed = S.Seed, Village = S.Village, Nature = S.EnemyNature,
                Began = rec.Began, Ended = Minute(m.Ended),
                Result = m.Result ?? (S.Done == "clear" ? "win" : S.PartyHp <= 0 ? "lose" : "quit"),
                Party = S.Party.Select((k, i) => new PartySlot { Id = k, Slot = i + 1 }).ToList(),
                Floor = S.Floor + 1, Node = S.Node, Step = S.Step, At = run.CurrentNode?.Type,
                Grade = R.GRADE_ON ? run.Grade : 0, Credits = R.GRADE_ON ? S.Credits : 0, Board = m.Board,   // 학년 꺼짐이면 0
                Fast = rec.FightSec > 0 ? Math.Round(Math.Min(1, rec.FastSec / rec.FightSec), 3) : (double?)null,
                Gold = S.Gold, Hp = Math.Max(0, S.PartyHp), MaxHp = S.PartyMaxHp, Removals = S.Removals,
                Fights = S.Hist.Select(CopyFight).ToList(),
                Picks = rec.Picks.Select(p => new PickLog { K = p.K, F = p.F, S = p.S, Card = p.Card, At = p.At, Offer = p.Offer?.ToList(), Pick = p.Pick }).ToList(),
                Deck = S.Deck.GroupBy(x => x).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new DeckCard
                {
                    Id = g.Key, N = g.Count(), O = S.Flash.TryGetValue(g.Key, out var f) ? f : 0, B = S.Shin.TryGetValue(g.Key, out var sh) ? sh : null,
                }).ToList(),
                Arts = S.Party.Where(k => run.GearOf(k).Count > 0).ToDictionary(k => k, k => new Dictionary<string, string>(run.GearOf(k))),
            };
            if (r.Arts.Count == 0) r.Arts = null;
            if (rec.Began != null && DateTime.TryParse(rec.Began, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var b0))
                r.Min = Math.Max(0, (int)Math.Round((m.Ended.ToUniversalTime() - b0).TotalMinutes));
            var last = S.Hist.LastOrDefault();
            if (r.Result == "lose" && last != null && last.Result != "win")
                r.Death = new DeathRec { F = last.Floor, N = last.Node, K = last.Kind, Turn = last.Turns, Foes = last.Foes.ToList(), Hp = last.HpBefore, MaxHp = last.HpMax };
            Shrink(r, maxBytes);
            return r;
        }

        static FightRecord CopyFight(FightRecord f) => new()
        {
            Floor = f.Floor, Node = f.Node, Kind = f.Kind, Foes = f.Foes?.ToList(), Result = f.Result, Turns = f.Turns,
            HpBefore = f.HpBefore, HpAfter = f.HpAfter, HpMax = f.HpMax, Plays = f.Plays, Ults = f.Ults, Deck = f.Deck?.ToList(),
            Log = f.Log?.Select(t => new TurnRec { T = t.T, C = t.C.ToList(), D = t.D, B = t.B, Hp = t.Hp, U = t.U, F = t.F?.Select(x => new FoeActRec { E = x.E, A = x.A, D = x.D }).ToList() }).ToList(),
        };

        /// <summary>
        /// 크기 상한까지 줄인다 — ① 일반 · 이벤트 싸움의 턴 기록(이긴 것, 앞에서부터) ② 상점 진열 ③ 제시된 것 ④ 선택 기록 ⑤ 모든 턴 기록 ⑥ 덱.
        /// 엘리트 · 보스 · 진 싸움의 턴 기록은 ⑤ 까지 남긴다.
        /// </summary>
        public static void Shrink(PlayRecord r, int maxBytes)
        {
            if (Bytes(r) <= maxBytes) return;
            foreach (var f in r.Fights.Where(f => f.Log != null && (f.Kind == "fight" || f.Kind == "event") && f.Result == "win").ToList())
            {
                f.Log = null; f.Deck = null; r.Trim = 1;
                if (Bytes(r) <= maxBytes) return;
            }
            if (r.Picks != null)
            {
                foreach (var p in r.Picks.Where(p => p.K == "shop")) p.Offer = null;
                r.Picks.RemoveAll(p => p.K == "shop");
                r.Trim = 2; if (Bytes(r) <= maxBytes) return;
                foreach (var p in r.Picks) p.Offer = null;
                r.Trim = 3; if (Bytes(r) <= maxBytes) return;
                r.Picks = null;
                r.Trim = 4; if (Bytes(r) <= maxBytes) return;
            }
            foreach (var f in r.Fights) { f.Log = null; f.Deck = null; }
            r.Trim = 5; if (Bytes(r) <= maxBytes) return;
            r.Deck = null;
            r.Trim = 6;
        }

        /// <summary>받는 쪽과 같은 검사 — 틀리면 까닭(보내기 전에 스스로 본다 · 시험).</summary>
        public static string Check(string json)
        {
            if (json == null || System.Text.Encoding.UTF8.GetByteCount(json) > MAX_BYTES) return "too big";
            PlayRecord r;
            try { r = Parse(json); } catch (Exception e) { return "bad json: " + e.Message; }
            if (r == null || r.Kind != KIND || r.V != VERSION) return "kind/v";
            if (r.Anon == null || r.Anon.Length < 16 || r.Anon.Length > 64 || !r.Anon.All(Uri.IsHexDigit)) return "anon";
            if (!RESULTS.Contains(r.Result)) return "result";
            if (r.Party == null || r.Party.Count == 0 || r.Party.Count > 4) return "party";
            if (r.Fights == null || r.Fights.Count > 80) return "fights";
            return null;
        }
    }

    public sealed partial class Run
    {
        // ── 플레이 기록 — 선택 적기(RecState.Picks) ──────────────────
        /// <summary>선택 하나를 적는다(넘김 · 아직이면 pick null).</summary>
        PickLog Pick(string k, string card, IEnumerable<string> offer, string pick, string at = null)
        {
            var p = new PickLog { K = k, F = S.Floor + 1, S = S.Step, Card = card, At = at, Offer = offer?.ToList(), Pick = pick };
            S.Rec.Picks.Add(p);
            return p;
        }

        /// <summary>이번 걸음에 제시만 된(pick 없는) 같은 갈래 · 대상의 기록을 채운다 — 없으면 새로 적는다(addIfNone).</summary>
        void Fill(string k, string card, string pick, IEnumerable<string> offer, bool addIfNone = true)
        {
            var ps = S.Rec.Picks;
            for (int i = ps.Count - 1; i >= 0 && ps[i].S == S.Step; i--)
                if (ps[i].K == k && ps[i].Pick == null && ps[i].At == null && ps[i].Card == card) { ps[i].Pick = pick; return; }
            if (addIfNone) Pick(k, card, offer, pick);
        }

        /// <summary>싸움을 닫으며 턴 녹화를 꺼낸다.</summary>
        static List<TurnRec> TapeOf(Battle b)
        {
            if (b.Tape == null) return null;
            b.Tape.TurnEnd(b);
            return b.Tape.Turns;
        }

        /// <summary>싸움 끝 — 전투 중 고른 빛(At fight) · 이겼으면 보상에 뜬 빛 · 전리품 아티팩트를 「제시됨」 으로(고르면 ClaimGlow · TakeEquip 이 채운다).</summary>
        void RecordFightPicks(Battle b, bool mind)
        {
            if (b.Tape != null) foreach (var e in b.Tape.Epis) Pick(e.Kind == "card" ? "oracle" : "grace", e.Card, e.Offer, e.Pick, "fight");
            if (b.Over != "win") return;
            if (!mind && b.Glow != null)
                foreach (var kv in b.Glow.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    var g = kv.Value;
                    if (g == null || g.Count == 0) continue;
                    Pick(g.Kind == "hero" ? "grace" : "oracle", GameData.NoInst(kv.Key), g.Kind == "card" ? g.Picks.Select(x => x.N.ToString()) : g.Options, null);
                }
            if (S.EventFight == null && S.Reward?.Equip != null && S.Reward.EquipTaken == null) Pick("equip", null, S.Reward.Equip, null);
        }
    }
}
