using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Bolzena.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 프론티어 검증기(3단계 — bolzena-unity/Docs/프론티어_서버.md §검증기).
///   verify [--url https://bolzena.pages.dev] [--token 관리자토큰] [--limit 20] [--log 파일] [--strict-version] [--dry]
///     주소 · 토큰은 인자가 없으면 환경 변수 BZ_VERIFY_URL · BZ_VERIFY_TOKEN. 데이터는 --data(없으면 bolzena-content-v2).
///     서버 GET /api/verify/pending 에서 검증 대기 슬롯 · 점수를 받아 —
///       슬롯: HeroSlot.Parse(모르는 키는 오류) · 사도 키가 칸과 같은지 · HeroSlot.Check → ok / bad(까닭).
///       점수: 서버가 준 씨앗 · 시즌 · 시작 때 떠 둔 슬롯(과 성장 prog — 0010, 있으면)으로 Frontier.Verify(셋업을 다시 지어 견주고 처음부터 다시 돌림) → verified(다시 돌린 점수) / rejected(까닭 · 어긋난 턴).
///     POST /api/verify/result 로 올린다. 한 번 돌고 끝난다(예약 작업이 주기적으로 부른다). 같은 PC 에서 같은 서버로 겹쳐 돌지 않게 서버 주소마다 이름 붙은 뮤텍스를 잡는다(예약 작업 verify-task.ps1 이 5분마다 부른다).
///     규칙 · 콘텐츠 지문이 이 검증기와 다른 테이프(다른 게임 버전)는 기본으로 건너뛴다(대기로 남김 — 검증기를 새 버전으로 바꾸면 그때 본다).
///     --strict-version 이면 거절한다. 시즌에 지문(rules · content)이 박혀 있고 이 검증기와 다르면 그 시즌 점수는 늘 건너뛴다(검증기를 맞출 것).
///     --dry: 서버에 올리지 않고 결과만 찍는다.
///   끝 코드: 0 = 돌았음(건너뜀 포함) · 1 = 서버 · 네트워크 오류 · 2 = 설정 오류 · 3 = 이미 다른 검증기가 도는 중.
/// </summary>
static class Verify
{
    static StreamWriter log;
    static void Log(string m)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {m}";
        Console.WriteLine(line);
        log?.WriteLine(line); log?.Flush();
    }

    public static int Run(GameData data, Func<string, string, string> opt, Func<string, bool> flag)
    {
        string url = opt("url", Environment.GetEnvironmentVariable("BZ_VERIFY_URL"));
        string token = opt("token", Environment.GetEnvironmentVariable("BZ_VERIFY_TOKEN"));
        string logPath = opt("log", null);
        int limit = int.Parse(opt("limit", "20"));
        bool strict = flag("strict-version"), dry = flag("dry");
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token)) { Console.WriteLine("verify — 주소 · 토큰이 없다(--url · --token 또는 환경 변수 BZ_VERIFY_URL · BZ_VERIFY_TOKEN)"); return 2; }
        if (logPath != null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath))); log = new StreamWriter(logPath, append: true, new UTF8Encoding(false)); }
        // 서버마다 하나 — 예약 작업(공개판)이 도는 동안에도 로컬 시험(127.0.0.1)의 검증기는 따로 돈다
        string mutexName = "Local\\bolzena-bz-verify-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(url.TrimEnd('/').ToLowerInvariant())))[..12];
        using var mutex = new Mutex(false, mutexName);
        bool mine = false;
        try { mine = mutex.WaitOne(0); } catch (AbandonedMutexException) { mine = true; }
        if (!mine) { Log("verify — 다른 검증기가 이미 돈다, 이번엔 쉰다"); log?.Dispose(); return 3; }
        try { return Loop(data, url.TrimEnd('/'), token, limit, strict, dry); }
        catch (Exception e) { Log("verify — 오류: " + e.Message); return 1; }
        finally { mutex.ReleaseMutex(); log?.Dispose(); log = null; }
    }

    static int Loop(GameData data, string url, string token, int limit, bool strict, bool dry)
    {
        string rules = Fingerprint.Rules(), content = data.ContentHash;
        Log($"verify — {url} · 규칙 {rules} · 콘텐츠 {content ?? "-"} · 사도 {data.Heroes.Count}{(dry ? " · 올리지 않음(dry)" : "")}");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("bolzena-bz-verify/1");
        long slotAfter = 0, scoreAfter = 0;
        int ok = 0, bad = 0, verified = 0, rejected = 0, skipped = 0;
        for (int page = 0; page < 1000; page++)
        {
            var resp = http.GetAsync($"{url}/api/verify/pending?slotAfter={slotAfter}&scoreAfter={scoreAfter}&limit={limit}").Result;
            var body = resp.Content.ReadAsStringAsync().Result;
            if (!resp.IsSuccessStatusCode) { Log($"verify — pending {(int)resp.StatusCode}: {Cut(body, 200)}"); return 1; }
            var p = JObject.Parse(body);
            var slots = (JArray)p["slots"] ?? new JArray();
            var scores = (JArray)p["scores"] ?? new JArray();
            if (slots.Count == 0 && scores.Count == 0) break;
            var outSlots = new JArray(); var outScores = new JArray();

            foreach (var s in slots)
            {
                long id = (long)s["id"]; slotAfter = Math.Max(slotAfter, id);
                var (status, why) = CheckSlot(data, (string)s["hero"], (string)s["json"]);
                if (status == "ok") ok++; else bad++;
                Log($"슬롯 #{id} {(string)s["hero"]} — {status}{(why != null ? " · " + why : "")}");
                outSlots.Add(new JObject { ["id"] = id, ["hash"] = (string)s["hash"], ["status"] = status, ["why"] = status == "bad" ? Cut(why, 500) : null });
            }

            foreach (var s in scores)
            {
                long id = (long)s["id"]; scoreAfter = Math.Max(scoreAfter, id);
                var r = CheckScore(data, s, rules, content, strict);
                if (r.status == null) { skipped++; Log($"점수 #{id} — 건너뜀 · {r.why}"); continue; }
                if (r.status == "verified") verified++; else rejected++;
                Log($"점수 #{id} {string.Join(",", ((JArray)s["heroes"]).Select(x => (string)x))} 주장 {(int)s["claimed"]:N0} — {r.status}{(r.status == "verified" ? $" {r.score:N0}" : $" · {r.why}{(r.turn >= 0 ? $" · 턴 지문 {r.turn}" : "")}")}");
                var o = new JObject { ["id"] = id, ["status"] = r.status };
                if (r.status == "verified") o["score"] = r.score; else { o["why"] = Cut(r.why, 500); o["turn"] = r.turn; }
                outScores.Add(o);
            }

            if (!dry && (outSlots.Count > 0 || outScores.Count > 0))
            {
                var payload = new JObject { ["slots"] = outSlots, ["scores"] = outScores }.ToString(Formatting.None);
                var post = http.PostAsync($"{url}/api/verify/result", new StringContent(payload, Encoding.UTF8, "application/json")).Result;
                var pb = post.Content.ReadAsStringAsync().Result;
                if (!post.IsSuccessStatusCode) { Log($"verify — result {(int)post.StatusCode}: {Cut(pb, 200)}"); return 1; }
                Log($"올림 — {pb}");
            }
            if (slots.Count < limit && scores.Count < limit) break;
        }
        Log($"verify 끝 — 슬롯 ok {ok} · bad {bad} · 점수 verified {verified} · rejected {rejected} · 건너뜀 {skipped}");
        return 0;
    }

    /// <summary>슬롯 하나 — (ok|bad, 까닭 또는 주의).</summary>
    static (string status, string why) CheckSlot(GameData data, string hero, string json)
    {
        HeroSlot s;
        try { s = HeroSlot.Parse(json); }
        catch (Exception e) { return ("bad", "슬롯 글을 읽을 수 없다 — " + Cut(e.Message, 200)); }
        if (s.Hero != hero) return ("bad", $"슬롯의 사도 「{s.Hero}」 가 칸의 사도 「{hero}」 와 다르다");
        var c = HeroSlot.Check(data, s);
        return c.Ok ? ("ok", c.Warnings.Count > 0 ? "주의 " + string.Join(" / ", c.Warnings) : null) : ("bad", string.Join(" / ", c.Errors));
    }

    /// <summary>점수 하나 — status null 이면 건너뜀(대기로 남김).</summary>
    static (string status, string why, int score, int turn) CheckScore(GameData data, JToken s, string rules, string content, bool strict)
    {
        var se = s["season"];
        if (se == null || se.Type == JTokenType.Null) return ("rejected", "시즌이 없다", 0, -1);
        string seRules = (string)se["rules"], seContent = (string)se["content"];
        if (seRules != null && seRules != rules) return (null, $"시즌 규칙 지문 {seRules} ≠ 검증기 {rules} — 검증기를 그 시즌 엔진으로", 0, -1);
        if (seContent != null && seContent != content) return (null, $"시즌 콘텐츠 지문 {seContent} ≠ 검증기 {content ?? "-"} — 검증기 --data 를 그 시즌 스냅샷으로", 0, -1);
        var season = new FrontierSeason { Id = (string)se["id"], Boss = (string)se["boss"], Turns = (int)se["turns"], Hp = (double)se["hp"], Dmg = (double)se["dmg"] };
        FrontierTape tape; BattleInput input;
        try { tape = FrontierTape.Parse((string)s["tape"]); input = tape?.Input(); }
        catch (Exception e) { return ("rejected", "테이프를 읽을 수 없다 — " + Cut(e.Message, 200), 0, -1); }
        if (tape == null || input == null) return ("rejected", "테이프가 비었다", 0, -1);
        // 지문 — 다른 게임 버전의 테이프(시즌이 지문을 박았으면 그 지문이어야)
        string want = seRules ?? rules, wantC = seContent ?? content;
        if (input.Rules != want || (wantC != null && input.Content != null && input.Content != wantC))
        {
            string why = $"게임 버전이 다르다(규칙 {input.Rules ?? "-"} · 콘텐츠 {input.Content ?? "-"} — 검증 기준 {want} · {wantC ?? "-"})";
            if (seRules != null || seContent != null || strict) return ("rejected", why, 0, -1);
            return (null, why, 0, -1);
        }
        if (tape.Seed != (long)s["seed"]) return ("rejected", $"서버가 준 씨앗이 아니다({tape.Seed} ≠ {(long)s["seed"]})", 0, -1);
        if (tape.Score != (int)s["claimed"]) return ("rejected", "주장 점수가 테이프 점수와 다르다", 0, -1);
        var heroes = ((JArray)s["heroes"]).Select(x => (string)x).ToList();
        var slots = new List<HeroSlot>();
        try { foreach (var j in (JArray)s["slots"]) slots.Add(HeroSlot.Parse((string)j)); }
        catch (Exception e) { return ("rejected", "시작 때 슬롯을 읽을 수 없다 — " + Cut(e.Message, 200), 0, -1); }
        if (!slots.Select(x => x.Hero).SequenceEqual(heroes)) return ("rejected", "시작 때 편성과 슬롯이 다르다", 0, -1);
        // 사도 성장(서버가 시도를 열 때 떠 둔 값 — migrations/0010 prog_json). 없으면(null) 성장 끔 = 지금과 같은 셋업
        ProgState prog = null;
        var pj = s["prog"];
        if (pj != null && pj.Type == JTokenType.Object)
            try { prog = GameData.FromJson<ProgState>(pj.ToString(Newtonsoft.Json.Formatting.None)); }
            catch (Exception e) { return ("rejected", "떠 둔 성장을 읽을 수 없다 — " + Cut(e.Message, 200), 0, -1); }
        FrontierCheck chk;
        try { chk = Frontier.Verify(data, tape, season, slots, prog); }
        catch (Exception e) { return ("rejected", "다시 돌리다 멈췄다 — " + Cut(e.GetType().Name + ": " + e.Message, 200), 0, -1); }
        return chk.Ok ? ("verified", null, chk.Score, -1) : ("rejected", chk.Why, 0, chk.Turn);
    }

    static string Cut(string s, int n) => s == null ? null : s.Length <= n ? s : s.Substring(0, n) + "…";
}
