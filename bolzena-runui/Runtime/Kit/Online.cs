using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 온라인(프론티어 — 계정 · 사도 슬롯 · 시도 · 랭킹)의 서버 손잡이. API 는 bolzena-unity/Docs/프론티어_서버.md.
    ///
    /// - 서버 주소: 웹(WebGL)은 같은 출처(지금 페이지의 주소 — 쿠키 bz_s 를 브라우저가 붙인다), PC · 에디터는 설정 값(PlayerPrefs bz.online.server,
    ///   기본 https://bolzena.pages.dev) · 명령줄 -server 주소가 이긴다(시험 — http://127.0.0.1:8799).
    /// - 로그인: 웹 = 쿠키, PC = 기기 코드로 받은 토큰(Authorization: Bearer — PlayerPrefs bz.online.token, -save 꼬리를 붙여 시험 실행과 나눈다).
    /// - 상태(Now): 서버에 닿지 않거나 D1 이 없으면(503 no db) Off — 화면은 프론티어 · 저장 단추를 「준비 중」으로 둔다(게임은 그대로).
    ///   닿으면 /api/me 로 Out(로그인 전) · NeedName(가입 전 — 닉네임 창) · In.
    /// - 시험 실행(-demo 따위 TestMute 인자)은 -server 를 주지 않으면 서버를 부르지 않는다(공개 서버에 시험 요청이 가지 않게).
    /// </summary>
    public sealed class Online : MonoBehaviour
    {
        public enum State { Unknown, Off, Out, NeedName, In }
        public const string DEFAULT_SERVER = "https://bolzena.pages.dev";
        const string KServer = "bz.online.server", KToken = "bz.online.token";

        public static State Now { get; private set; } = State.Unknown;
        /// <summary>닉네임(가입 전이면 null).</summary>
        public static string Nick { get; private set; }
        public static string Provider { get; private set; }
        /// <summary>계정 역할(/api/me 의 user.role — 「tester」 = 시험 계정, migrations/0009) · 없으면 null. 화면 표시용 — 권한은 서버가 주소마다 다시 본다.</summary>
        public static string Role { get; private set; }
        /// <summary>시험 계정으로 로그인했나 — 「덱 만들기」 를 웹 · PC 어디서나 보인다(Flow.DeckLab).</summary>
        public static bool Tester => Ready && Role == "tester";
        /// <summary>Off 인 까닭(로그 · 화면 작은 글).</summary>
        public static string OffWhy { get; private set; }
        /// <summary>지금 시즌(/api/frontier/season 의 season) — Off 면 null.</summary>
        public static JObject Season { get; private set; }
        /// <summary>상태가 바뀔 때마다(로비의 계정 알약 · 메뉴 글이 다시 쓴다).</summary>
        public static event Action Changed;
        public static bool Busy { get; private set; }
        public static bool Ready => Now == State.In;
        public static bool Up => Now == State.Out || Now == State.NeedName || Now == State.In;

        static Online me;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (me != null) return;
            var go = new GameObject("Online");
            DontDestroyOnLoad(go);
            me = go.AddComponent<Online>();
            Refresh();
        }

        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i < a.Length - 1 ? a[i + 1] : null; }
        static string Tail { get { var s = Arg("-save"); return s != null ? "." + s : ""; } }

        public static bool Web => Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor;
        /// <summary>시험 실행인데 서버를 주지 않았다 — 부르지 않는다.</summary>
        public static bool Disabled => TestMute.On && Arg("-server") == null;

        /// <summary>PC 서버 주소(설정 값). 명령줄 -server 가 이긴다.</summary>
        public static string Server
        {
            get { var a = Arg("-server"); if (!string.IsNullOrEmpty(a)) return a; var s = PlayerPrefs.GetString(KServer, ""); return s.Length > 0 ? s : DEFAULT_SERVER; }
            set { PlayerPrefs.SetString(KServer, value ?? ""); PlayerPrefs.Save(); }
        }

        /// <summary>요청 앞부분 — 웹은 지금 페이지의 출처(같은 사이트), PC 는 Server.</summary>
        public static string Base
        {
            get
            {
                if (Web && Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var u)) return u.GetLeftPart(UriPartial.Authority);
                return Server.TrimEnd('/');
            }
        }

        static string Token
        {
            get => PlayerPrefs.GetString(KToken + Tail, "");
            set { if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(KToken + Tail); else PlayerPrefs.SetString(KToken + Tail, value); PlayerPrefs.Save(); }
        }
        public static bool HasToken => Token.Length > 0;

        // ── 요청 ──
        public sealed class Resp
        {
            public long Code;
            public string Text, Error;
            public JObject Json;
            public bool Ok => Code >= 200 && Code < 300;
            /// <summary>서버에 닿지 못했다(네트워크 · 시간 초과).</summary>
            public bool Net => Code == 0;
            public string Str(string k) { var t = Json?[k]; return t == null || t.Type == JTokenType.Null ? null : t.ToString(); }
        }

        public static void Call(string method, string path, string body, Action<Resp> done)
        {
            if (me == null) Init();
            me.StartCoroutine(CallCo(method, path, body, done));
        }

        public static IEnumerator CallCo(string method, string path, string body, Action<Resp> done)
        {
            var res = new Resp();
            if (Disabled) { res.Error = "시험 실행 — 서버 없음"; done?.Invoke(res); yield break; }
            UnityWebRequest r;
            try
            {
                r = new UnityWebRequest(Base + path, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 25 };
                if (body != null)
                {
                    r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)) { contentType = "application/json" };
                    r.SetRequestHeader("Content-Type", "application/json");
                }
                if (!Web && Token.Length > 0) r.SetRequestHeader("Authorization", "Bearer " + Token);
            }
            catch (Exception e) { res.Error = e.Message; done?.Invoke(res); yield break; }
            using (r)
            {
                yield return r.SendWebRequest();
                res.Code = r.result == UnityWebRequest.Result.ConnectionError ? 0 : r.responseCode;
                res.Text = r.downloadHandler != null ? r.downloadHandler.text : null;
                try { if (!string.IsNullOrEmpty(res.Text) && res.Text.TrimStart().StartsWith("{")) res.Json = JObject.Parse(res.Text); } catch { }
                res.Error = res.Str("error") ?? (res.Ok ? null : r.error);
            }
            if (res.Code != 200 && res.Code != 202 && res.Code != 204) Debug.Log($"[Online] {method} {path} → {res.Code} {res.Error}");
            // 세션이 끝났다(만료 · 다른 기기에서 모두 로그아웃) — 로그인 전으로
            if (res.Code == 401 && (Now == State.In || Now == State.NeedName) && path != "/api/auth/logout") { if (!Web) Token = ""; Nick = null; Set(State.Out); }
            // 가입(닉네임) 전에 쓰는 요청 — 닉네임 창을 띄우게
            if (res.Code == 403 && res.Json?["needsName"]?.Type == JTokenType.Boolean && (bool)res.Json["needsName"] && Now == State.In) Set(State.NeedName);
            // 계정 정지(운영 조치) — 쓰기가 403 { banned, why, until, forever }. 어느 화면에서 막혔든 까닭 · 풀리는 때를 알린다
            if (Banned(res)) Toast.Show(BanText(res), 4f);
            done?.Invoke(res);
        }

        // ── 상태 ──
        /// <summary>시즌(서버 · D1 이 있나) → 로그인 여부 · 닉네임을 다시 본다.</summary>
        public static void Refresh(Action done = null)
        {
            if (me == null) { Init(); }
            if (Disabled) { Season = null; Set(State.Off, "시험 실행 — 서버를 주지 않음(-server)"); done?.Invoke(); return; }
            me.StartCoroutine(RefreshCo(done));
        }

        static IEnumerator RefreshCo(Action done)
        {
            Busy = true;
            Resp s = null;
            yield return CallCo("GET", "/api/frontier/season", null, x => s = x);
            if (!s.Ok || !(s.Json?["season"] is JObject season))
            {
                Season = null;
                Busy = false;
                Set(State.Off, s.Code == 503 ? "서버 준비 중(D1 없음)" : s.Net ? "서버에 닿지 않음" : $"서버 응답 {s.Code}");
                done?.Invoke();
                yield break;
            }
            Season = season;
            if (!Web && Token.Length == 0) { Nick = null; Busy = false; Set(State.Out); done?.Invoke(); yield break; }
            Resp m = null;
            yield return CallCo("GET", "/api/me", null, x => m = x);
            Busy = false;
            if (m.Code == 401) { if (!Web) Token = ""; Nick = null; Set(State.Out); }
            else if (m.Ok && m.Json?["user"] is JObject u) TakeUser(u, m.Json["needsName"]?.Type == JTokenType.Boolean && (bool)m.Json["needsName"]);
            else Set(State.Off, m.Net ? "서버에 닿지 않음" : $"계정 확인 실패 {m.Code}");
            done?.Invoke();
        }

        static void TakeUser(JObject u, bool needsName)
        {
            var n = u["name"];
            Nick = n == null || n.Type == JTokenType.Null || n.ToString().Length == 0 ? null : n.ToString();
            Provider = u["provider"]?.ToString();
            Role = u["role"]?.Type == JTokenType.String ? u["role"].ToString() : null;
            Set(needsName || Nick == null ? State.NeedName : State.In);
        }

        /// <summary>데모 캡처(-demo-fix1019) — 서버 없이 상태만 바꿔 계정 창 모양을 찍는다.</summary>
        public static void DemoSet(State s) { if (Demo.Active) Set(s); }

        static void Set(State s, string why = null)
        {
            bool changed = s != Now || why != OffWhy;
            Now = s; OffWhy = s == State.Off ? why : null;
            if (changed) Debug.Log($"[Online] 상태 {s}{(why != null ? " — " + why : "")}{(Nick != null ? " · " + Nick : "")} · 서버 {Base}");
            Changed?.Invoke();
        }

        // ── 로그인 · 계정 ──
        /// <summary>웹 — 같은 탭에서 공급자 로그인으로(돌아오면 게임이 다시 뜬다 — 판 저장은 칸마다 되어 있다).</summary>
        public static void WebLogin(string provider)
        {
            string back = "/";
            if (Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var u)) back = u.AbsolutePath;
            WebNav.Go(Base + "/api/auth/" + provider + "/start?back=" + Uri.EscapeDataString(back));
        }

        /// <summary>PC 기기 코드로 받은 토큰을 넣는다.</summary>
        public static void TakeToken(string token, Action done = null) { Token = token; Refresh(done); }

        /// <summary>로컬 시험 전용 가짜 로그인(TEST_LOGIN=1 · localhost 서버만 받는다) — PC 토큰.</summary>
        public static void TestLogin(string sub, Action<Resp> done)
        {
            Call("GET", "/api/auth/test?sub=" + Uri.EscapeDataString(sub) + "&kind=pc", null, r =>
            {
                if (r.Ok && r.Str("token") is string t) TakeToken(t, () => done?.Invoke(r));
                else done?.Invoke(r);
            });
        }

        public static void Logout(Action done = null)
        {
            Call("POST", "/api/auth/logout", null, r =>
            {
                if (!Web) Token = "";
                Nick = null;
                Set(State.Out);
                done?.Invoke();
            });
        }

        /// <summary>탈퇴 — 서버의 세션 · 슬롯 · 시도 · 점수를 모두 지운다(204). 기기 사본도 지운다.</summary>
        public static void DeleteAccount(Action<Resp> done)
        {
            Call("DELETE", "/api/me", null, r =>
            {
                if (r.Ok)
                {
                    if (!Web) Token = "";
                    Nick = null;
                    SlotStore.ClearCopies();
                    Set(State.Out);
                }
                done?.Invoke(r);
            });
        }

        /// <summary>닉네임 정하기(= 가입 끝) · 바꾸기.</summary>
        public static void SetName(string name, Action<Resp> done)
        {
            var body = new JObject { ["name"] = name ?? "" }.ToString(Newtonsoft.Json.Formatting.None);
            Call("POST", "/api/me/name", body, r =>
            {
                if (r.Ok && r.Json?["user"] is JObject u) TakeUser(u, false);
                done?.Invoke(r);
            });
        }

        // ── 오류를 알기 쉬운 글로 ──
        /// <summary>닉네임 요청의 오류 글.</summary>
        public static string NameError(Resp r)
        {
            if (r.Code == 400) return r.Str("why") ?? "쓸 수 없는 닉네임이다";
            if (r.Code == 409) return "이미 쓰는 닉네임이다 — 대소문자 · 전각/반각 · 공백 · _ - . 만 다른 이름도 같은 이름으로 본다";
            if (r.Code == 429)
            {
                long sec = r.Json?["retryAfter"]?.Type == JTokenType.Integer ? (long)r.Json["retryAfter"] : 0;
                if (sec > 0) { var t = TimeSpan.FromSeconds(sec); return $"닉네임은 하루에 한 번만 바꿀 수 있다 — {(t.TotalHours >= 1 ? $"{(int)t.TotalHours}시간 {t.Minutes}분" : $"{Math.Max(1, t.Minutes)}분")} 뒤에 다시 할 수 있다"; }
                return "요청이 너무 잦다 — 잠시 뒤 다시 할 수 있다";
            }
            return ErrorText(r);
        }

        /// <summary>정지된 계정의 쓰기 거절(403 banned)인가.</summary>
        public static bool Banned(Resp r) => r != null && r.Code == 403 && r.Json?["banned"]?.Type == JTokenType.Boolean && (bool)r.Json["banned"];

        /// <summary>「계정이 정지되었습니다 — 까닭 · 풀리는 때」(한 줄 알림이라 까닭은 40자까지).</summary>
        public static string BanText(Resp r)
        {
            string why = r.Str("why");
            if (why != null && why.Length > 40) why = why.Substring(0, 40) + "…";
            bool forever = r.Json?["forever"]?.Type == JTokenType.Boolean && (bool)r.Json["forever"];
            string when = forever ? "영구 정지" : r.Json?["until"]?.Type == JTokenType.Integer
                ? DateTimeOffset.FromUnixTimeSeconds((long)r.Json["until"]).ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " 에 풀린다" : "풀리는 때 모름";
            return $"계정이 정지됐다 — {(why != null ? why + " · " : "")}{when}";
        }

        /// <summary>그 밖의 요청 오류 글.</summary>
        public static string ErrorText(Resp r)
        {
            if (r == null) return "알 수 없는 오류";
            if (r.Net) return "서버에 닿지 않는다 — 인터넷 연결을 확인하고 다시 할 수 있다";
            if (r.Code == 401) return "로그인이 끝났다 — 다시 로그인할 수 있다";
            if (r.Code == 403 && r.Json?["needsName"] != null) return "닉네임을 먼저 정해야 한다";
            if (Banned(r)) return BanText(r);
            if (r.Code == 403) return "허용되지 않은 요청이다";
            if (r.Code == 413) return "보낼 내용이 너무 크다";
            if (r.Code == 429) return "요청이 너무 잦다 — 잠시 뒤 다시 할 수 있다";
            if (r.Code == 503) return "서버가 준비 중이다";
            if (r.Code >= 500) return $"서버 오류({r.Code}) — 잠시 뒤 다시 할 수 있다";
            return r.Error ?? $"요청 실패({r.Code})";
        }
    }

    /// <summary>웹 — 같은 탭에서 다른 주소로(공급자 로그인). 웹이 아니면 기본 브라우저로 연다.</summary>
    public static class WebNav
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void BzNavigate(string url);
        public static void Go(string url) => BzNavigate(url);
#else
        public static void Go(string url) => Application.OpenURL(url);
#endif
    }
}
