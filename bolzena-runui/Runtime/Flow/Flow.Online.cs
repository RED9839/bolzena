using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 계정(프론티어) — 로비 오른쪽 위 알약 · 계정 창 · 닉네임 창 · PC 기기 코드 창. 서버 손잡이는 Kit/Online.cs, API 는 bolzena-unity/Docs/프론티어_서버.md.
    //   웹: 구글 · 디스코드 단추 → 같은 탭에서 /api/auth/<공급자>/start → 돌아오면 /api/me(쿠키).
    //   PC: 기기 코드 — 코드 · 주소를 보여 주고 브라우저를 연 뒤 5초마다 폴링 → 토큰.
    //   첫 로그인 뒤 needsName 이면 닉네임 창(규칙 · 400 까닭 · 409 겹침 · 429 남은 시간). 서버가 없으면(Off) 알약은 숨고 프론티어 · 저장은 「준비 중」.
    public partial class Flow
    {
        /// <summary>이번 실행에서 닉네임 창을 저절로 띄웠다(닫으면 다시 저절로 띄우지 않는다 — 단추를 누르면 띄운다).</summary>
        bool namePrompted;
        public const string NAME_RULE = "2~12자 · 한글 · 영문 · 숫자 · 공백 · _ - . 만 · 운영자 · 공식 사칭 금지 · 랭킹에 보인다 · 바꾸기는 하루 1번";
        public const string PRIVACY = "이메일 · 이름 · 프로필 사진은 받지 않는다 — 로그인 공급자의 사용자 번호를 가린 값과 닉네임만 둔다. 탈퇴하면 계정 · 저장 사도 · 점수를 바로 모두 지운다.";

        static string ProviderKo(string p) => p == "discord" ? "디스코드" : p == "google" ? "구글" : p == "test" ? "시험 계정" : p ?? "";

        /// <summary>로비 오른쪽 위(업데이트 내용 왼쪽) — 계정 알약. 서버가 없으면 숨는다. 상태가 바뀌면 글만 고친다.</summary>
        void LobbyAccount(RectTransform root)
        {
            float w = Theme.C(260, 230), h = Theme.C(46, 40), vw = Theme.C(290, 250);
            var b = Btn.Make(root, null, BtnStyle.PillDark, () => AccountPanel(), 0, "account");
            var rt = b.GetComponent<RectTransform>();
            rt.At(1, 1, -Theme.Gutter - 8 - LobbyCogW - vw - 10, -Theme.C(22, 14), w, h);
            var ic = Ui.Img(rt, Theme.S("ic_crown"), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 16, 0, 24, 24);
            var t = Ui.Title(rt, "", Theme.C(Theme.FsMd, Theme.FsSm), Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Fill(48, 2, 14, 2);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            Stage.Hot["lobby.account"] = b;
            void Paint()
            {
                if (!b) { Online.Changed -= Paint; return; }
                b.gameObject.SetActive(Online.Now != Online.State.Off && Online.Now != Online.State.Unknown);
                t.text = Online.Now == Online.State.In ? $"<color={Theme.SubTag}>교주</color>  {Online.Nick}"
                    : Online.Now == Online.State.NeedName ? $"<color={Theme.GoldTag}>닉네임 정하기</color>"
                    : "로그인  <size=78%><color=" + Theme.DimTag + ">프론티어</color></size>";
                // 가입 전 — 로비에 오면 한 번 저절로 닉네임 창(웹은 로그인하고 돌아온 바로 그때)
                if (Online.Now == Online.State.NeedName && !namePrompted && !Demo.Active && Stage.Current == "lobby" && Stage.ModalLayer.childCount == 0)
                {
                    namePrompted = true;
                    NamePanel(null);
                }
            }
            Online.Changed += Paint;
            Paint();
            Tw.Pop(rt, 0.55f, 0.85f, 0.4f);
        }

        /// <summary>로그인 · 가입이 된 뒤 then(되면 곧장). 로그인 전이면 계정 창, 가입 전이면 닉네임 창, 서버가 없으면 알림.</summary>
        public void NeedAccount(string why, Action then)
        {
            switch (Online.Now)
            {
                case Online.State.In: then?.Invoke(); return;
                case Online.State.NeedName: NamePanel(then); return;
                case Online.State.Out: AccountPanel(why, then); return;
                default: Toast.Show("온라인 기능은 준비 중이다 — " + (Online.OffWhy ?? "서버에 닿지 않는다"), 3f); return;
            }
        }

        /// <summary>데모 캡처 — PC 빌드에서도 웹의 구글 · 디스코드 단추 줄을 그린다(-demo-fix1019).</summary>
        public static bool DemoWebLogin;

        /// <summary>계정 창 — 상태마다: 로그인 단추(웹 구글 · 디스코드 / PC 기기 코드) · 닉네임 정하기 · 로그아웃 · 탈퇴.</summary>
        public void AccountPanel(string why = null, Action then = null)
        {
            var size = Stage.Size;
            float w = Mathf.Min(720, size.x - 48), h = Mathf.Min(Online.Now == Online.State.In ? 470 : Online.Now == Online.State.Out ? 420 : 500, size.y - 24);   // 로그인 전 = 단추 줄이 낮아져(10-19) 창도 줄인다
            var (body, close, foot) = Stage.ModalBox("account", w, h, "계정", "프론티어(보스 랭킹전) · 사도 저장", true, null, 0);
            var col = Ui.Rect("col", body).Fill(10, 6, 10, 0);
            Ui.Col(col, 12, TextAnchor.UpperCenter, null, true, false);
            TextMeshProUGUI Line(string s, float fs, Color c, float hh)
            {
                var t = Ui.Text(col, s, fs, c, TextAlignmentOptions.Center);
                t.Pref(-1, hh);
                return t;
            }
            if (why != null) Line(why, Theme.FsMd, Theme.Gold, 34);
            switch (Online.Now)
            {
                case Online.State.Off:
                case Online.State.Unknown:
                {
                    Line("온라인 기능은 준비 중이다", Theme.FsLg, Theme.Ink, 40);
                    Line(Online.OffWhy ?? "서버를 확인하는 중…", Theme.FsBody, Theme.Sub, 30);
                    Line("모험 · 도감은 그대로 할 수 있다", Theme.FsBody, Theme.Sub, 30);
                    var again = Btn.Make(col, "다시 확인", BtnStyle.PillGold, () => { close(); Online.Refresh(() => AccountPanel(why, then)); }, Theme.FsMd, "acct.retry");
                    again.Pref(260, Theme.BtnH);
                    Stage.Hot["acct.retry"] = again;
                    break;
                }
                case Online.State.Out:
                {
                    Line("로그인하면 완주한 사도를 저장하고 프론티어에서 점수를 겨룰 수 있다", Theme.FsMd, Theme.Ink, 54);
                    if (Online.Web || DemoWebLogin)
                    {
                        // 구글 먼저 · 보조 단추 크기(2026-10-19 사용자 — 너무 컸다). 폰(Compact)은 조금 키워 누를 만하게.
                        //   줄의 flexibleHeight 를 0 으로 — 안 그러면 세로 칸(col)이 남는 높이를 이 줄에 몰아줘 단추가 200 넘게 커졌다(예전에 「너무 크다」 의 원인)
                        var row = Ui.Rect("row", col); row.Pref(-1, Theme.C(46, 52), -1, 0);
                        Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, false, true);
                        var g = Btn.Make(row, "구글로 로그인", BtnStyle.PillGold, () => Online.WebLogin("google"), Theme.FsBody, "acct.google"); g.Pref(Theme.C(190, 210), -1);
                        var d = Btn.Make(row, "디스코드로 로그인", BtnStyle.PillGold, () => Online.WebLogin("discord"), Theme.FsBody, "acct.discord"); d.Pref(Theme.C(190, 210), -1);
                        Stage.Hot["acct.discord"] = d; Stage.Hot["acct.google"] = g;
                        Line("로그인 화면에 갔다가 돌아온다 — 진행 중인 모험은 저장돼 있다", Theme.FsSm, Theme.Sub, 28);
                    }
                    else
                    {
                        var row = Ui.Rect("row", col); row.Pref(-1, Theme.C(46, 52), -1, 0);   // 가운데 한 개 — 세로 칸이 너비를 늘리지 않게 줄에 담는다
                        Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, false, true);
                        var d = Btn.Make(row, "브라우저로 로그인", BtnStyle.PillGold, () => { close(); DevicePanel(then); }, Theme.FsBody, "acct.device");
                        d.Pref(Theme.C(220, 240), -1);
                        Stage.Hot["acct.device"] = d;
                        Line("브라우저에서 로그인한 뒤 게임에 뜬 코드를 확인해 승인한다", Theme.FsSm, Theme.Sub, 28);
                    }
                    var pv = Line(PRIVACY, Theme.FsCap, Theme.Dim, 56);
                    pv.enableAutoSizing = true; pv.fontSizeMin = 11; pv.fontSizeMax = Theme.FsCap;
                    break;
                }
                case Online.State.NeedName:
                {
                    Line("로그인했다 — 닉네임을 정하면 가입이 끝난다", Theme.FsLg, Theme.Ink, 40);
                    Line(NAME_RULE, Theme.FsSm, Theme.Sub, 48);
                    var nb = Btn.Make(col, "닉네임 정하기", BtnStyle.PillGold, () => { close(); NamePanel(then); }, Theme.FsMd, "acct.name");
                    nb.Pref(300, Theme.BtnMainH);
                    Stage.Hot["acct.name"] = nb;
                    AccountFoot(col, close, false);
                    break;
                }
                case Online.State.In:
                {
                    var nm = Line($"<color={Theme.SubTag}>닉네임</color>  {Online.Nick}", Theme.FsXl, Theme.Ink, 46);
                    nm.font = Theme.Title;
                    Line($"{ProviderKo(Online.Provider)} 계정{(Online.Web ? "" : " · 이 PC")} · 랭킹에는 닉네임만 보인다", Theme.FsSm, Theme.Sub, 28);
                    var rn = Btn.Make(col, "닉네임 바꾸기", BtnStyle.PillDark, () => { close(); NamePanel(null, true); }, Theme.FsMd, "acct.rename");
                    rn.Pref(300, Theme.BtnH);
                    Stage.Hot["acct.rename"] = rn;
                    AccountFoot(col, close, true);
                    break;
                }
            }
            if (!Online.Web)
            {
                var sv = Ui.Text(body, $"서버 {Online.Base}", Theme.FsCap, Theme.Dim, TextAlignmentOptions.Center);
                sv.rectTransform.Band(0, 22, 0, 0, 0);
            }
        }

        void AccountFoot(RectTransform col, Action close, bool named)
        {
            var row = Ui.Rect("foot", col); row.Pref(-1, Theme.BtnH);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, false, true);
            var lo = Btn.Make(row, "로그아웃", BtnStyle.PillDark, () => { close(); Online.Logout(() => Toast.Show("로그아웃했다")); }, Theme.FsMd, "acct.logout");
            lo.Pref(200, -1);
            var del = Btn.Make(row, "탈퇴", BtnStyle.PillRose, () =>
                Confirm("탈퇴할까?", "계정 · 저장한 사도 · 프론티어 점수를 모두 지운다 — 되돌릴 수 없다", "탈퇴", () =>
                {
                    close();
                    Online.DeleteAccount(r => Toast.Show(r.Ok ? "탈퇴했다 — 서버의 모든 기록을 지웠다" : "탈퇴하지 못했다 — " + Online.ErrorText(r), 3.5f));
                }, true), Theme.FsMd, "acct.delete");
            del.Pref(160, -1);
            Stage.Hot["acct.logout"] = lo; Stage.Hot["acct.delete"] = del;
            var pv = Ui.Text(col, PRIVACY, Theme.FsCap, Theme.Dim, TextAlignmentOptions.Center);
            pv.Pref(-1, 56); pv.enableAutoSizing = true; pv.fontSizeMin = 11; pv.fontSizeMax = Theme.FsCap;
        }

        // ── 닉네임 ──
        /// <summary>지금 열린 닉네임 칸(자동 데모가 글을 넣는다).</summary>
        public TMP_InputField NameField { get; private set; }
        /// <summary>닉네임 창의 마지막 오류 글(데모 단언).</summary>
        public string NameErrorShown { get; private set; }

        /// <summary>닉네임 창 — 규칙 한 줄 · 입력 칸 · 오류 줄 · 정하기. 되면 then.</summary>
        public void NamePanel(Action then, bool rename = false)
        {
            var size = Stage.Size;
            float w = Mathf.Min(700, size.x - 48), h = Mathf.Min(420, size.y - 24);
            var (body, close, foot) = Stage.ModalBox("nickname", w, h, rename ? "닉네임 바꾸기" : "닉네임 정하기",
                rename ? "하루(24시간)에 한 번 바꿀 수 있다" : "닉네임을 정하면 가입이 끝난다 — 랭킹에 이 이름이 보인다", true, null, Theme.BtnH + 24);
            var rule = Ui.Text(body, NAME_RULE, Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
            rule.rectTransform.Band(1, 50, 10, 10, -4);
            var box = Ui.Rect("field", body).Band(1, 60, 60, 60, -64);
            var field = NameInput(box, rename ? Online.Nick : "");
            NameField = field;
            var err = Ui.Text(body, "", Theme.FsSm, Theme.Bad, TextAlignmentOptions.Center);
            err.rectTransform.Band(1, 52, 10, 10, -132);
            NameErrorShown = null;
            var row = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, true, true);
            var no = Btn.Make(row, "나중에", BtnStyle.PillDark, close, Theme.FsMd, "name.no");
            Btn ok = null;
            ok = Btn.Make(row, rename ? "바꾸기" : "정하기", BtnStyle.PillGold, () =>
            {
                if (Online.Busy || !ok.Interactable) return;
                var want = field.text;
                ok.Interactable = false;
                err.color = Theme.Sub; err.text = "확인하는 중…";
                Online.SetName(want, r =>
                {
                    if (ok) ok.Interactable = true;
                    if (r.Ok)
                    {
                        close();
                        Toast.Show(rename ? $"닉네임을 「{Online.Nick}」 로 바꿨다" : $"가입 완료 — {Online.Nick} 교주", 3f);
                        then?.Invoke();
                        return;
                    }
                    NameErrorShown = Online.NameError(r);
                    if (err) { err.color = Theme.Bad; err.text = NameErrorShown; }
                    Debug.Log($"[Online] 닉네임 「{want}」 → {r.Code} — {NameErrorShown}");
                });
            }, Theme.FsMd, "name.ok");
            Stage.Hot["name.ok"] = ok; Stage.Hot["name.no"] = no;
        }

        TMP_InputField NameInput(RectTransform parent, string text, string placeholder = "닉네임", int limit = 20)
        {
            var bg = Ui.Img(parent, Theme.S("pill_dark", 46), Color.white.A(0.96f), "namefield", true);
            bg.rectTransform.Fill();
            bg.gameObject.SetActive(false);
            var area = Ui.Rect("text area", bg.transform).Fill(28, 6, 28, 6);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Ui.Text(area, placeholder, Theme.FsLg, Theme.Dim, TextAlignmentOptions.Center, false, "placeholder"); ph.rectTransform.Fill();
            var tx = Ui.Text(area, "", Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center, false, "text");
            tx.rectTransform.Fill(); tx.textWrappingMode = TextWrappingModes.NoWrap; tx.overflowMode = TextOverflowModes.Overflow; tx.richText = false;
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area; input.textComponent = tx; input.placeholder = ph;
            input.fontAsset = Theme.Body; input.pointSize = tx.fontSize;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false; input.characterLimit = limit;
            input.customCaretColor = true; input.caretColor = Theme.Gold; input.caretWidth = 2;
            input.selectionColor = Theme.Gold.A(0.35f);
            input.shouldHideMobileInput = false;
            input.targetGraphic = bg;
            input.text = text ?? "";
            bg.gameObject.SetActive(true);
            WebInput.Attach(input);
            return input;
        }

        // ── PC 기기 코드 ──
        /// <summary>지금 보이는 기기 코드(자동 데모 · 시험 로그).</summary>
        public string DeviceCodeShown { get; private set; }

        /// <summary>PC 로그인 — 코드 · 주소를 띄우고 브라우저를 연 뒤 폴링. 승인되면 토큰을 넣고 가입 전이면 닉네임 창, 아니면 then.</summary>
        public void DevicePanel(Action then)
        {
            var size = Stage.Size;
            float w = Mathf.Min(760, size.x - 48), h = Mathf.Min(500, size.y - 24);
            bool alive = true;
            var (body, close, foot) = Stage.ModalBox("device", w, h, "브라우저로 로그인", "PC 게임 로그인 — 기기 코드", true, () => alive = false, Theme.BtnH + 24);
            var lead = Ui.Text(body, "코드를 받는 중…", Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
            lead.rectTransform.Band(1, 60, 10, 10, -2);
            var codeBg = Ui.Img(body, Theme.Round, Theme.NavyWell, "codebg");
            codeBg.rectTransform.At(0.5f, 1, 0, -74, 420, 92);
            var code = Ui.Title(codeBg.transform, "", Theme.Fs3xl, Theme.Gold, TextAlignmentOptions.Center);
            code.rectTransform.Fill(); code.characterSpacing = 18; code.textWrappingMode = TextWrappingModes.NoWrap;
            var url = Ui.Text(body, "", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
            url.rectTransform.Band(1, 30, 10, 10, -180);
            var status = Ui.Text(body, "", Theme.FsBody, Theme.Sub, TextAlignmentOptions.Center);
            status.rectTransform.Band(1, 56, 10, 10, -214);
            var row = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, true, true);
            string verifyUrl = null;
            var reopen = Btn.Make(row, "브라우저 다시 열기", BtnStyle.PillDark, () => { if (verifyUrl != null) Application.OpenURL(verifyUrl); }, Theme.FsMd, "device.open");
            var cancel = Btn.Make(row, "취소", BtnStyle.PillDark, close, Theme.FsMd, "device.cancel");
            Stage.Hot["device.open"] = reopen; Stage.Hot["device.cancel"] = cancel;
            DeviceCodeShown = null;
            StartCoroutine(DeviceCo());

            IEnumerator DeviceCo()
            {
                Online.Resp r = null;
                yield return Online.CallCo("POST", "/api/auth/device", "{}", x => r = x);
                if (!alive) yield break;
                if (!r.Ok) { lead.text = "코드를 받지 못했다"; status.color = Theme.Bad; status.text = Online.ErrorText(r); yield break; }
                string deviceCode = r.Str("deviceCode"), user = r.Str("userCode");
                verifyUrl = r.Str("verifyUrl");
                int interval = Math.Max(2, (int?)r.Json["interval"] ?? 5), left = (int?)r.Json["expiresIn"] ?? 600;
                DeviceCodeShown = user;
                Debug.Log($"[Online] 기기 코드 {user} · {verifyUrl}");
                lead.text = "브라우저에서 로그인한 뒤 아래 코드가 같은지 보고 「승인」 을 누를 수 있다";
                code.text = user;
                url.text = verifyUrl;
                if (!Demo.Active) Application.OpenURL(verifyUrl);
                float until = Time.realtimeSinceStartup + left;
                var body0 = new JObject { ["deviceCode"] = deviceCode }.ToString(Newtonsoft.Json.Formatting.None);
                while (alive)
                {
                    float wait = interval;
                    while (wait > 0 && alive)
                    {
                        int rem = Mathf.Max(0, Mathf.CeilToInt(until - Time.realtimeSinceStartup));
                        if (status) status.text = $"승인을 기다리는 중 · 코드 유효 {rem / 60}:{rem % 60:00}";
                        wait -= Time.unscaledDeltaTime;
                        yield return null;
                    }
                    if (!alive) yield break;
                    Online.Resp p = null;
                    yield return Online.CallCo("POST", "/api/auth/device/poll", body0, x => p = x);
                    if (!alive) yield break;
                    if (p.Code == 202) continue;
                    if (p.Code == 429) { interval += 2; continue; }
                    if (p.Code == 200 && p.Str("token") is string token)
                    {
                        status.color = Theme.Good; status.text = "승인됐다 — 계정을 확인하는 중…";
                        bool done = false;
                        Online.TakeToken(token, () => done = true);
                        while (!done) yield return null;
                        alive = false;
                        close();
                        if (Online.Now == Online.State.NeedName) NamePanel(then);
                        else if (Online.Now == Online.State.In) { Toast.Show($"로그인했다 — {Online.Nick}"); then?.Invoke(); }
                        yield break;
                    }
                    status.color = Theme.Bad;
                    status.text = p.Code == 403 ? "브라우저에서 거절했다 — 창을 닫고 로그인을 다시 누를 수 있다"
                        : p.Code == 410 ? "코드가 지났다 — 창을 닫고 로그인을 다시 누를 수 있다" : Online.ErrorText(p);
                    yield break;
                }
            }
        }
    }
}
