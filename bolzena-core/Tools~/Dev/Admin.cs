using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 운영 명령 — 서버 운영 주소(/api/admin/… — ADMIN_TOKEN 시크릿 Bearer)를 부른다(bolzena-unity/Docs/프론티어_서버.md §운영).
///   admin user &lt;대상&gt;                                   찾기 · 보기(닉네임 부분 일치 · 사용자 번호 · 시도 id · 칸 id)
///   admin rename &lt;대상&gt; &lt;새 이름&gt; --reason 까닭           닉네임 강제 변경(규칙 · 겹침 검사, 하루 한 번 제한 없음)
///   admin clearname &lt;대상&gt; --reason 까닭                  닉네임 비우기(본인이 다시 정하게)
///   admin score delete|reject &lt;시도 id&gt; --reason 까닭       점수 줄 지우기 · rejected 로
///   admin hide|unhide &lt;대상&gt; [--season id] --reason 까닭   랭킹에서 빼기 · 되돌리기(season 이 없으면 모든 시즌)
///   admin slot delete &lt;칸 id&gt; --reason 까닭                칸 하나 지우기
///   admin slots purge &lt;대상&gt; --confirm --reason 까닭        그 사용자 칸 전부 지우기
///   admin ban &lt;대상&gt; (--days N | --forever) --reason 까닭   정지 · admin unban &lt;대상&gt; --reason 까닭
///   admin role &lt;대상&gt; tester|none --reason 까닭           시험 계정 부여 · 회수(migrations/0009 — 「덱 만들기」 · 시험 칸, 그 사람의 도전은 랭킹에서 빠진다)
///   admin log [대상] [-n 50]                              운영 기록
/// 대상: 숫자 · id:12 = 사용자 번호 · 32자리 16진 · attempt:… = 시도 id(랭킹 줄) · slot:34 = 칸 id · nick:1234(숫자뿐인 닉네임) · 그 밖 = 닉네임.
/// 주소 · 토큰: --url · --token, 없으면 환경 변수 BZ_ADMIN_URL · BZ_ADMIN_TOKEN(검증기 BZ_VERIFY_* 와 따로).
/// 쓰기 조치는 **기본이 미리 보기**(서버에 dry 로 물어 무엇을 할지만 보여 주고 끝) — 실제로 하려면 --yes. --json 은 서버 응답 그대로.
/// 끝 코드: 0 = 됨 · 1 = 서버가 거절 · 네트워크 오류 · 2 = 쓰는 법 · 설정 오류.
/// </summary>
static class Admin
{
    static List<string> rest;
    static string Opt(params string[] names)
    {
        foreach (var n in names)
        {
            int i = rest.IndexOf(n);
            if (i < 0) continue;
            var v = i + 1 < rest.Count ? rest[i + 1] : null;
            rest.RemoveRange(i, v == null ? 1 : 2);
            return v ?? "";
        }
        return null;
    }
    static bool Flag(string n) { int i = rest.IndexOf(n); if (i < 0) return false; rest.RemoveAt(i); return true; }

    const string USAGE = "admin user <대상> · rename <대상> <새 이름> · clearname <대상> · score delete|reject <시도 id> · hide|unhide <대상> [--season id] · slot delete <칸 id> · slots purge <대상> --confirm · ban <대상> --days N|--forever · unban <대상> · role <대상> tester|none · log [대상] [-n 50]\n"
        + "  쓰기 조치는 --reason \"까닭\" 이 꼭 있어야 하고, --yes 가 없으면 미리 보기만 한다. 주소 · 토큰: --url · --token 또는 BZ_ADMIN_URL · BZ_ADMIN_TOKEN. --json = 응답 그대로.\n"
        + "  대상: 숫자 · id:12(사용자 번호) · 32자리 16진 · attempt:…(시도 id) · slot:34(칸 id) · nick:1234 · 그 밖은 닉네임";

    static HttpClient http;
    static string url;
    static bool raw;

    public static int Run(string[] args)
    {
        rest = args.ToList();
        url = Opt("--url") ?? Environment.GetEnvironmentVariable("BZ_ADMIN_URL");
        string token = Opt("--token") ?? Environment.GetEnvironmentVariable("BZ_ADMIN_TOKEN");
        raw = Flag("--json");
        if (rest.Count == 0 || rest[0] == "help" || rest[0] == "--help") { Console.WriteLine(USAGE); return rest.Count == 0 ? 2 : 0; }
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token)) { Console.WriteLine("admin — 주소 · 토큰이 없다(--url · --token 또는 환경 변수 BZ_ADMIN_URL · BZ_ADMIN_TOKEN)"); return 2; }
        url = url.TrimEnd('/');
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("bolzena-bz-admin/1");
        try { return Dispatch(); }
        catch (HttpRequestException e) { Console.WriteLine("admin — 서버에 닿지 않는다: " + e.Message); return 1; }
        catch (System.Threading.Tasks.TaskCanceledException) { Console.WriteLine("admin — 서버 응답 시간 초과(30초)"); return 1; }
        finally { http.Dispose(); }
    }

    static int Dispatch()
    {
        string cmd = rest[0]; rest.RemoveAt(0);
        string reason = Opt("--reason");
        bool yes = Flag("--yes");
        switch (cmd)
        {
            case "user":
                {
                    if (rest.Count != 1) return Usage("admin user <대상>");
                    var (code, j) = Get("/api/admin/user?" + Query(rest[0]));
                    if (code != 200) return Fail(code, j);
                    if (raw) { Console.WriteLine(j.ToString(Formatting.Indented)); return 0; }
                    var users = (JArray)j["users"];
                    if (users.Count == 0) { Console.WriteLine($"없음 — {rest[0]}"); return 1; }
                    if ((string)j["by"] == "nick") Console.WriteLine(j.Value<bool>("exact") ? $"닉네임 「{rest[0]}」 — 같은 이름 1 · 부분 일치 {users.Count - 1}" : $"닉네임 「{rest[0]}」 — 같은 이름 없음 · 부분 일치 {users.Count}");
                    foreach (var u in users) { Console.WriteLine(); PrintUser((JObject)u, true); }
                    return 0;
                }
            case "log":
                {
                    string n = Opt("-n", "--n") ?? "50";
                    if (rest.Count > 1) return Usage("admin log [대상] [-n 50]");
                    string q = "n=" + Uri.EscapeDataString(n);
                    if (rest.Count == 1) { var (uid, err) = Resolve(rest[0]); if (err != 0) return err; q += "&user=" + uid; }
                    var (code, j) = Get("/api/admin/log?" + q);
                    if (code != 200) return Fail(code, j);
                    if (raw) { Console.WriteLine(j.ToString(Formatting.Indented)); return 0; }
                    var rows = (JArray)j["rows"];
                    Console.WriteLine($"운영 기록 {rows.Count}줄{(rest.Count == 1 ? " — " + rest[0] : "")}");
                    foreach (var r in rows) Console.WriteLine("  " + LogLine((JObject)r));
                    return 0;
                }
            case "rename":
                {
                    if (rest.Count != 2) return Usage("admin rename <대상> <새 이름> --reason 까닭");
                    var (uid, err) = Resolve(rest[0]); if (err != 0) return err;
                    return Act(new JObject { ["action"] = "rename", ["user"] = uid, ["name"] = rest[1] }, reason, yes);
                }
            case "clearname":
            case "unban":
                {
                    if (rest.Count != 1) return Usage($"admin {cmd} <대상> --reason 까닭");
                    var (uid, err) = Resolve(rest[0]); if (err != 0) return err;
                    return Act(new JObject { ["action"] = cmd, ["user"] = uid }, reason, yes);
                }
            case "score":
                {
                    if (rest.Count != 2 || (rest[0] != "delete" && rest[0] != "reject")) return Usage("admin score delete|reject <시도 id> --reason 까닭");
                    string a = rest[1].StartsWith("attempt:") ? rest[1].Substring(8) : rest[1];
                    return Act(new JObject { ["action"] = "score." + rest[0], ["attempt"] = a.ToLowerInvariant() }, reason, yes);
                }
            case "hide":
            case "unhide":
                {
                    string season = Opt("--season");
                    if (rest.Count != 1 || season == "") return Usage($"admin {cmd} <대상> [--season id] --reason 까닭");
                    var (uid, err) = Resolve(rest[0]); if (err != 0) return err;
                    var b = new JObject { ["action"] = cmd, ["user"] = uid };
                    if (season != null) b["season"] = season;
                    return Act(b, reason, yes);
                }
            case "slot":
                {
                    if (rest.Count != 2 || rest[0] != "delete" || !long.TryParse(rest[1].StartsWith("slot:") ? rest[1].Substring(5) : rest[1], out var sid) || sid < 1) return Usage("admin slot delete <칸 id> --reason 까닭");
                    return Act(new JObject { ["action"] = "slot.delete", ["slot"] = sid }, reason, yes);
                }
            case "slots":
                {
                    bool confirm = Flag("--confirm");
                    if (rest.Count != 2 || rest[0] != "purge") return Usage("admin slots purge <대상> --confirm --reason 까닭");
                    var (uid, err) = Resolve(rest[1]); if (err != 0) return err;
                    if (yes && !confirm) { Console.WriteLine("admin slots purge — 칸 전부 지우기는 --confirm 이 있어야 한다"); return 2; }
                    return Act(new JObject { ["action"] = "slots.purge", ["user"] = uid, ["confirm"] = confirm }, reason, yes);
                }
            case "role":
                {
                    if (rest.Count != 2 || (rest[1] != "tester" && rest[1] != "none")) return Usage("admin role <대상> tester|none --reason 까닭");
                    var (uid, err) = Resolve(rest[0]); if (err != 0) return err;
                    return Act(new JObject { ["action"] = "role", ["user"] = uid, ["role"] = rest[1] == "none" ? JValue.CreateNull() : (JToken)"tester" }, reason, yes);
                }
            case "ban":
                {
                    string days = Opt("--days"); bool forever = Flag("--forever");
                    if (rest.Count != 1 || (days == null) == !forever) return Usage("admin ban <대상> (--days N | --forever) --reason 까닭");
                    int d = 0;
                    if (days != null && (!int.TryParse(days, out d) || d < 1 || d > 3650)) return Usage("--days 는 1~3650");
                    var (uid, err) = Resolve(rest[0]); if (err != 0) return err;
                    var b = new JObject { ["action"] = "ban", ["user"] = uid };
                    if (forever) b["forever"] = true; else b["days"] = d;
                    return Act(b, reason, yes);
                }
            default:
                Console.WriteLine(USAGE);
                return 2;
        }
    }

    static int Usage(string u) { Console.WriteLine("쓰는 법: " + u); return 2; }

    /// <summary>대상 글 → 찾기 질의.</summary>
    static string Query(string t)
    {
        string E(string s) => Uri.EscapeDataString(s);
        if (t.StartsWith("id:")) return "id=" + E(t.Substring(3));
        if (t.StartsWith("nick:")) return "nick=" + E(t.Substring(5));
        if (t.StartsWith("slot:")) return "slot=" + E(t.Substring(5));
        if (t.StartsWith("attempt:")) return "attempt=" + E(t.Substring(8).ToLowerInvariant());
        if (t.Length > 0 && t.All(char.IsDigit)) return "id=" + E(t);
        if (t.Length == 32 && t.All(Uri.IsHexDigit)) return "attempt=" + E(t.ToLowerInvariant());
        return "nick=" + E(t);
    }

    /// <summary>대상 → 사용자 번호 하나(쓰기 조치 · 기록 거르기). 닉네임은 같은 이름(열쇠)이 있거나 부분 일치가 하나일 때만 — 여럿이면 목록을 보여 주고 멈춘다.</summary>
    static (long id, int err) Resolve(string t)
    {
        var (code, j) = Get("/api/admin/user?" + Query(t));
        if (code != 200) return (0, Fail(code, j));
        var users = (JArray)j["users"];
        if (users.Count == 0) { Console.WriteLine($"대상 없음 — {t}"); return (0, 1); }
        bool exact = (string)j["by"] != "nick" || j.Value<bool>("exact");
        if (!exact && users.Count > 1)
        {
            Console.WriteLine($"「{t}」 와 같은 닉네임은 없고 부분 일치가 {users.Count}명 — 사용자 번호(id:N)나 정확한 닉네임으로:");
            foreach (var u in users) Console.WriteLine($"  #{u["id"]} {u["name"]}");
            return (0, 2);
        }
        var one = users[0];
        if (!exact) Console.WriteLine($"(부분 일치 하나 — 「{t}」 → #{one["id"]} {one["name"]})");
        return ((long)one["id"], 0);
    }

    /// <summary>쓰기 조치 — --yes 가 없으면 dry 로 물어 계획만.</summary>
    static int Act(JObject body, string reason, bool yes)
    {
        if (string.IsNullOrWhiteSpace(reason)) { Console.WriteLine("쓰기 조치는 --reason \"까닭\" 이 있어야 한다(운영 기록에 남는다)"); return 2; }
        body["reason"] = reason;
        body["dry"] = !yes;
        var (code, j) = Post("/api/admin/act", body);
        if (code != 200) return Fail(code, j);
        if (raw) { Console.WriteLine(j.ToString(Formatting.Indented)); return 0; }
        string action = (string)j["action"];
        Console.WriteLine(yes ? $"한 일 — {action} · 운영 기록 #{j["log"]}" : $"미리 보기(바꾸지 않음) — {action}");
        Console.WriteLine("  " + PlanText(action, (JObject)j["plan"]));
        Console.WriteLine("  까닭: " + reason);
        if (j["user"] is JObject u) { Console.WriteLine(yes ? "  조치 뒤:" : "  대상:"); PrintUser(u, false, "    "); }
        if (!yes) Console.WriteLine("실제로 하려면 같은 명령에 --yes 를 붙인다.");
        return 0;
    }

    static string PlanText(string action, JObject p)
    {
        switch (action)
        {
            case "rename": return $"닉네임 「{p["from"]}」 → 「{p["to"]}」";
            case "clearname": return $"닉네임 「{p["from"]}」 비움 — 본인이 다시 정해야 한다(그동안 랭킹에서 빠짐)";
            case "score.delete": { var d = (JObject)p["delete"]; return $"점수 줄 지움 — 시즌 {d["season"]} · [{Heroes(d["heroes"])}] · {d["status"]} · 주장 {Num(d["claimed"])} · 점수 {Num(d["score"])}"; }
            case "score.reject": return $"점수 {p["from"]} → rejected(본인 기록에 「{p["why"]}」)";
            case "hide": return $"랭킹에서 뺌 — 시즌 {Season((string)p["season"])}{(p.Value<bool>("already") ? "(이미 숨김 — 까닭 · 때만 새로)" : "")}";
            case "unhide": return $"랭킹 숨김 풂 — 시즌 {string.Join(", ", ((JArray)p["seasons"]).Select(x => Season((string)x)))}";
            case "slot.delete": { var d = (JObject)p["delete"]; return $"칸 지움 — {d["hero"]} 저장 {(int)d["n"] + 1}{(d["name"]?.Type == JTokenType.String ? $"(「{d["name"]}」)" : "")} · {d["status"]} · 지문 {d["hash"]}"; }
            case "slots.purge": return $"칸 {p["delete"]}개 전부 지움{(p.Value<bool>("confirm") ? "" : " — 실제로 하려면 --confirm 도")}";
            case "ban": { var b = (JObject)p["ban"]; return $"정지 — {(b.Value<bool>("forever") ? "영구" : "풀리는 때 " + T(b["until"]))}{(p["before"] is JObject pb ? $"(이전 정지 {BanText(pb)} 를 바꿈)" : "")}"; }
            case "unban": return $"정지 풂 — 지금 정지: {BanText((JObject)p["unban"])}";
            case "role": return $"역할 {Role(p["from"])} → {Role(p["to"])}{(p["to"]?.Type == JTokenType.String ? " — 「덱 만들기」 · 시험 칸이 열리고, 도전 · 점수는 랭킹 · 남의 편성 · 내 순위에서 빠진다" : " — 시험 칸 주소가 막힌다(칸은 남는다 · 이미 낸 시험 점수는 계속 랭킹 밖)")}";
            default: return p.ToString(Formatting.None);
        }
    }

    static void PrintUser(JObject u, bool full, string ind = "")
    {
        Console.WriteLine($"{ind}#{u["id"]} {(u["name"]?.Type == JTokenType.String ? "「" + u["name"] + "」" : "(닉네임 없음 — 가입 전 · 비움)")}{(u["role"]?.Type == JTokenType.String ? $" · 역할 {u["role"]}" : "")} · {u["provider"]} · 가입 {T(u["created"])} · 마지막 활동 {T(u["lastActive"])}{(u["nameChanged"]?.Type == JTokenType.Integer ? " · 닉네임 정한 때 " + T(u["nameChanged"]) : "")}");
        Console.WriteLine($"{ind}  정지: {(u["ban"] is JObject b ? BanText(b) : "없음")} · 랭킹 숨김: {(((JArray)u["hidden"]).Count == 0 ? "없음" : string.Join(", ", u["hidden"].Select(h => $"{Season((string)h["season"])}({h["reason"]})")))}");
        var s = u["slots"]; var c = u["scores"]; var z = u["sessions"];
        Console.WriteLine($"{ind}  세션 {z["live"]}(웹 {z["web"]} · PC {z["pc"]}) · 슬롯 {s["count"]}칸(ok {s["ok"]} · 대기 {s["pending"]} · bad {s["bad"]}){(u["testSlots"]?.Type == JTokenType.Integer && (long)u["testSlots"] > 0 ? $" · 시험 칸 {u["testSlots"]}" : "")} · 시도 {u["attempts"]} · 점수 {c["count"]}(검증 {c["verified"]} · 대기 {c["pending"]} · 거절 {c["rejected"]}{(c["test"]?.Type == JTokenType.Integer && (long)c["test"] > 0 ? $" · 시험 {c["test"]}" : "")})");
        if (u["board"] is JObject bd) Console.WriteLine($"{ind}  교주 보드 진행 있음 · {Num(bd["bytes"])}바이트 · 판 {bd["rev"]} · 바꾼 때 {T(bd["updated"])}");
        var best = (JArray)u["best"];
        if (best.Count > 0) Console.WriteLine($"{ind}  시즌 최고: {string.Join(" · ", best.Select(x => $"{x["season"]} {Num(x["score"])}(검증 {x["verified"]})"))}");
        if (!full) return;
        var recent = (JArray)u["recent"];
        if (recent.Count > 0) { Console.WriteLine($"{ind}  최근 시도:"); foreach (var r in recent) Console.WriteLine($"{ind}    {r["attempt"]} {T(r["at"])} [{Heroes(r["heroes"])}] {r["status"]}{(r["test"]?.Type == JTokenType.Boolean && (bool)r["test"] ? "(시험)" : "")}{(r["score"]?.Type == JTokenType.Integer ? " " + Num(r["score"]) : r["claimed"]?.Type == JTokenType.Integer ? " 주장 " + Num(r["claimed"]) : "")}"); }
        var log = (JArray)u["log"];
        if (log.Count > 0) { Console.WriteLine($"{ind}  최근 운영 기록:"); foreach (var l in log) Console.WriteLine($"{ind}    " + LogLine((JObject)l)); }
    }

    static string LogLine(JObject l)
    {
        string who = l["user"]?.Type == JTokenType.Integer ? $"#{l["user"]}{(l["name"]?.Type == JTokenType.String ? " " + l["name"] : "")}" : "-";
        string detail = l["detail"] is JObject d ? " · " + d.ToString(Formatting.None) : "";
        return $"#{l["id"]} {T(l["at"])} {l["actor"]} {l["action"]} {who} ({l["target"]}) — {l["reason"]}{detail}";
    }

    static string Role(JToken t) => t?.Type == JTokenType.String ? (string)t : "없음";
    static string BanText(JObject b) => $"{(b.Value<bool>("forever") ? "영구" : "~" + T(b["until"]))}(까닭 {b["why"]})";
    static string Season(string s) => s == "*" ? "모든 시즌" : s;
    static string Heroes(JToken h) => h is JArray a ? string.Join(",", a.Select(x => (string)x)) : "";
    static string Num(JToken t) => t?.Type == JTokenType.Integer ? ((long)t).ToString("N0", CultureInfo.InvariantCulture) : "-";
    static string T(JToken t) => t?.Type == JTokenType.Integer ? DateTimeOffset.FromUnixTimeSeconds((long)t).ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "-";

    static int Fail(int code, JObject j)
    {
        if (raw && j != null) Console.WriteLine(j.ToString(Formatting.Indented));
        string e = j?["error"]?.ToString() ?? "", why = j?["why"]?.ToString();
        string hint = code == 401 ? " — 토큰이 틀렸다(BZ_ADMIN_TOKEN)" : code == 503 ? " — 서버에 ADMIN_TOKEN 시크릿이 없거나 DB 가 없다" : "";
        Console.WriteLine($"서버 {code} {e}{(why != null ? " · " + why : "")}{hint}");
        return code == 400 ? 2 : 1;
    }

    static (int, JObject) Get(string path) => Send(new HttpRequestMessage(HttpMethod.Get, url + path));
    static (int, JObject) Post(string path, JObject body) =>
        Send(new HttpRequestMessage(HttpMethod.Post, url + path) { Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json") });

    static (int, JObject) Send(HttpRequestMessage m)
    {
        HttpResponseMessage r;
        try { r = http.SendAsync(m).Result; }
        catch (AggregateException e) when (e.InnerException != null) { throw e.InnerException; }
        var text = r.Content.ReadAsStringAsync().Result;
        JObject j = null;
        try { if (text.TrimStart().StartsWith("{")) j = JObject.Parse(text); } catch (JsonException) { }
        return ((int)r.StatusCode, j);
    }
}
