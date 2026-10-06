using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 창들 — 설정(웹판 js/settings-panel.js) · 덱 보기 · 사도 한 장(heroSheet).
    public partial class Flow
    {
        // ── 설정 창 — 탭 셋(소리 · 화면 · 전투 연출). 항목은 한 줄씩: 왼쪽 이름 + 한 줄 설명, 오른쪽 스위치 · 막대 · 작은 분할 단추 ──
        //   창 높이는 화면에 맞추고(PC 820 · 폰 700 까지) 넘치면 탭 안에서 스크롤한다 — 잘리는 항목이 없게.
        //   값은 Settings · DisplayOptions 에 바로 저장한다(PlayerPrefs 키는 예전 그대로). 고른 탭은 이번 실행 동안 기억한다.
        static int settingsTab;
        static readonly string[] SettingsTabs = { "소리", "화면", "전투 연출" };

        public void SettingsPanel(bool inRun)
        {
            var size = Stage.Size;
            float w = Mathf.Min(Theme.C(1180, 1240), size.x - 48);
            float h = Mathf.Min(Theme.C(820, 700), size.y - 24);
            float footH = Theme.C(84, 80);
            var (body, close, bar) = Stage.ModalBox("settings", w, h, "설정", "바꾼 값은 바로 저장됩니다", true, null, footH);

            // 탭 줄 · 아래 스크롤 칸
            var tabs = Ui.Rect("tabs", body).Band(1, 56, 0, 0, 0);
            Ui.Row(tabs, 10, TextAnchor.MiddleLeft, new RectOffset(4, 4, 2, 2), false, true);
            var area = Ui.Rect("area", body).Fill(0, 0, 0, 68);
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 0, TextAnchor.UpperLeft, new RectOffset(6, 6, 0, 8), true, false);

            void Show(int i)
            {
                settingsTab = Mathf.Clamp(i, 0, SettingsTabs.Length - 1);
                Gone(tabs);
                for (int k = 0; k < SettingsTabs.Length; k++)
                {
                    int kk = k;
                    Stage.Hot["settings.tab" + k] = SetTab(tabs, SettingsTabs[k], k == settingsTab, () => { if (kk != settingsTab) Show(kk); });
                }
                Gone(content);
                content.anchoredPosition = Vector2.zero;   // 탭을 바꾸면 맨 위부터
                if (settingsTab == 0) SoundTab(content);
                else if (settingsTab == 1) ScreenTab(content);
                else BattleTab(content);
            }
            Show(settingsTab);

            // 아래 띠 — 왼쪽 안내 · 오른쪽 단추(로비로 · 닫기)
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, new RectOffset(10, 10, 14, 14), false, true);
            var hint = Ui.Text(bar, Theme.Compact ? "바깥을 누르면 닫힙니다" : "Esc 키나 바깥을 누르면 닫힙니다", Theme.FsSm, Theme.Dim, TextAlignmentOptions.MidlineLeft);
            hint.textWrappingMode = TextWrappingModes.NoWrap; hint.overflowMode = TextOverflowModes.Ellipsis;
            hint.Pref(0, -1, 1);
            if (inRun)
            {
                var lobby = Btn.Make(bar, "로비로(모험은 저장)", BtnStyle.Dark, () => { close(); Lobby(); }, Theme.FsMd);
                lobby.Pref(280);
                Stage.Hot["settings.lobby"] = lobby;
            }
            var ok = Btn.Make(bar, "닫기", BtnStyle.Gold, close, Theme.FsMd);
            ok.Pref(240);
            Stage.Hot["settings.close"] = ok;
        }

        // 탭을 바꿀 때 — 같은 프레임의 줄 배치에 옛 칸이 끼지 않게 먼저 끄고 지운다
        static void Gone(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) { var c = t.GetChild(i).gameObject; c.SetActive(false); Destroy(c); }
        }

        // 탭 하나 — 지금 탭은 금 알약, 나머지는 남색 알약
        Btn SetTab(RectTransform row, string label, bool on, Action go)
        {
            var b = Btn.Make(row, null, on ? BtnStyle.PillGold : BtnStyle.PillDark, go, 0, "tab " + label);
            var t = Ui.Title(b.transform, label, Theme.FsMd, on ? Theme.Brown : Theme.Ink, TextAlignmentOptions.Center);
            t.rectTransform.Fill(18, 2, 18, 2);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            b.Label = t;
            b.Pref(Theme.C(190, 200), 52);
            return b;
        }

        // ── 탭 속 ──
        void SoundTab(RectTransform c)
        {
            SetSlider(c, "전체 소리", TestMute.On ? "시험 실행이라 지금은 소리가 꺼져 있습니다" : "모든 소리의 크기입니다", Settings.Master, v => Settings.Master = v, "set.master");
            SetSlider(c, "효과음", "카드 · 타격 · 단추 소리의 크기입니다", Settings.SfxVol, v => Settings.SfxVol = v, "set.sfx");
            SetSlider(c, "목소리", "사도 목소리의 크기입니다", Settings.Voice, v => Settings.Voice = v, "set.voice");
        }

        void BattleTab(RectTransform c)
        {
            SetToggle(c, "움직임 줄이기", "화면 전환과 등장 연출을 짧게 합니다", Settings.ReduceMotion, v => Settings.ReduceMotion = v, "set.calm");
            SetToggle(c, "화면 흔들림 끄기", "타격과 고학년 때 화면이 흔들리지 않습니다", Settings.NoShake, v => Settings.NoShake = v, "set.shake");
            SetToggle(c, "컷인 건너뛰기", "고학년 컷인 없이 바로 씁니다", Settings.SkipCutin, v => Settings.SkipCutin = v, "set.cutin");
            SetToggle(c, "고학년 짧게 보기", "끄면 모험에서 처음 쓸 때만 길게 봅니다", Settings.UltShort, v => Settings.UltShort = v, "set.ultshort");
        }

        // 화면 모드 한 줄 풀이 — 설명 칸에 한 줄로 들어가게 줄였다(DisplayOptions.ModeHint 는 전투 창이 쓴다)
        static string ModeLine(int i) => i switch
        {
            0 => "고른 해상도 크기의 창으로 띄웁니다",
            1 => "모니터를 채우고 그리는 크기만 바꿉니다 · 알트탭이 빠릅니다",
            _ => "모니터 해상도를 바꿔 차지합니다 · 알트탭이 느립니다",
        };

        // 창 모드 · 해상도는 바꾸면 바로 시험 적용(Try)하고 「10초 안에 확인」 창을 띄운다 — 확인이 없으면 앞 화면으로.
        void ScreenTab(RectTransform c)
        {
            bool desk = !Application.isMobilePlatform && !DisplayOptions.Web;   // 폰은 창 · 해상도가 없다(웹은 전체 화면 스위치)
            var modes = new Btn[0];
            var presets = new Btn[0];
            var fps = new Btn[0];   // 먼저 비워 둔다 — 아래 칸들의 누름(Pick → Paint)이 이것을 읽는다
            TextMeshProUGUI modeDesc = null, resDesc = null, fullDesc = null, fpsDesc = null;
            int curMode = DisplayOptions.ModeIndex, curPreset = DisplayOptions.PresetIndex;
            string ResLine() => "지금 " + DisplayOptions.Describe();
            string FullLine() => $"지금 {Screen.width}×{Screen.height} · 전체 화면은 Esc 로 나옵니다";
            string FpsLine() => DisplayOptions.VSync ? "수직동기가 켜져 있어 쉽니다 · 끄면 고를 수 있습니다" : "초당 그리는 횟수입니다 · 낮추면 전기와 열이 줄어듭니다";

            if (DisplayOptions.Web)
                fullDesc = SetToggle(c, "전체 화면", FullLine(), Screen.fullScreen, v => DisplayOptions.SetWebFullScreen(v), "disp.full");
            if (desk)
            {
                var mc = SetRow(c, "화면 모드", ModeLine(curMode), Theme.C(520, 520), 50, out modeDesc, out _);
                modes = SetSegments(mc, DisplayOptions.Modes.Select(m => m.Name).ToArray(), k => Pick(k, -1), "disp.mode");
                for (int i = 0; i < modes.Length; i++)
                    if (!DisplayOptions.ModeAvailable(i)) { modes[i].Interactable = false; modes[i].Why = "이 기기에서는 쓸 수 없습니다"; }

                var mon = DisplayOptions.Monitor;
                var rc = SetRow(c, "해상도", ResLine(), Theme.C(520, 520), 50, out resDesc, out _);
                presets = SetSegments(rc, DisplayOptions.Presets.Select(p => p.Name.Replace(" (UHD)", "")).ToArray(), k => Pick(-1, k), "disp.res");
                for (int i = 0; i < presets.Length; i++)
                {
                    var s = DisplayOptions.SizeOf(i);
                    if (DisplayOptions.Fits(i)) continue;
                    presets[i].Interactable = false;
                    presets[i].Why = $"{s.x}×{s.y} — 이 모니터({mon.x}×{mon.y})보다 큽니다";
                    var lk = Ui.Img(presets[i].transform, Theme.S("ic_lock"), Theme.Sub, "lock"); lk.rectTransform.At(1, 1, -6, -5, 14, 14); lk.preserveAspect = true;
                }
            }
            var fc = SetRow(c, "프레임 제한", FpsLine(), Theme.C(500, 500), 50, out fpsDesc, out _);
            fps = SetSegments(fc, Enumerable.Range(0, DisplayOptions.FrameCaps.Count).Select(DisplayOptions.FrameCapName).ToArray(),
                k => { DisplayOptions.SetFrameCap(k); Paint(); }, "disp.fps");
            SetToggle(c, "수직동기", "모니터 주사율에 맞춰 찢김 없이 그립니다", DisplayOptions.VSync, v => { DisplayOptions.SetVSync(v); Paint(); }, "disp.vsync");
            SetToggle(c, "글자 크게", "판 화면 글자를 키웁니다 · 다음 화면부터 바뀝니다", Settings.BigText, v => Settings.BigText = v, "set.big");

            void Paint()
            {
                for (int i = 0; i < modes.Length; i++) SegOn(modes[i], i == curMode);
                for (int i = 0; i < presets.Length; i++) SegOn(presets[i], i == curPreset);
                bool vs = DisplayOptions.VSync;
                for (int i = 0; i < fps.Length; i++)
                {
                    SegOn(fps[i], !vs && i == DisplayOptions.FpsIndex);
                    fps[i].Interactable = !vs;
                    fps[i].Why = vs ? "수직동기를 끄면 고를 수 있습니다" : null;
                }
                if (modeDesc) modeDesc.text = ModeLine(curMode);
                if (resDesc) resDesc.text = ResLine();
                if (fullDesc) fullDesc.text = FullLine();
                if (fpsDesc) fpsDesc.text = FpsLine();
            }

            void Pick(int mode, int preset)
            {
                int m = mode >= 0 ? mode : curMode;
                int p = preset >= 0 ? preset : curPreset >= 0 ? curPreset : (m == 0 ? (DisplayOptions.Fits(2) ? 2 : 1) : 0);
                if (m == curMode && p == curPreset) return;
                int pm = curMode, pp = curPreset;
                if (!DisplayOptions.Try(m, p)) { Toast.Show("이 화면으로는 바꿀 수 없습니다"); return; }
                curMode = m; curPreset = p;
                Paint();
                KeepScreen(kept =>
                {
                    if (!kept) { curMode = pm; curPreset = pp; }
                    Paint();
                });
            }

            Paint();
            StartCoroutine(Watch());
            // 크기는 다음 프레임에 바뀐다 — 지금 화면 글은 실제 화면이 바뀔 때마다 다시 쓴다(탭을 바꾸거나 창이 닫히면 멈춘다)
            System.Collections.IEnumerator Watch()
            {
                string last = null;
                while (fpsDesc)
                {
                    var d = DisplayOptions.Describe() + Screen.fullScreen;
                    if (d != last) { last = d; Paint(); }
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }
        }

        // ── 한 줄 ── 왼쪽 이름(위) · 설명 한 줄(아래), 오른쪽 ctrlW × ctrlH 자리를 돌려준다. 줄 아래 가는 선
        RectTransform SetRow(RectTransform parent, string name, string desc, float ctrlW, float ctrlH, out TextMeshProUGUI descText, out RectTransform row)
        {
            float rh = Theme.C(88, 84);
            row = Ui.Rect("row " + name, parent);
            row.Pref(-1, rh);
            var line = Ui.Img(row, Theme.White, Theme.Line, "line"); line.rectTransform.Band(0, 1, 0, 0, 0);
            var text = Ui.Rect("text", row).Fill(14, 0, ctrlW + 44, 0);
            var nm = Ui.Title(text, name, Theme.FsLg, Theme.Ink, desc != null ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.MidlineLeft, "name");
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
            descText = null;
            if (desc != null)
            {
                var nr = nm.rectTransform;
                nr.anchorMin = new Vector2(0, 0.5f); nr.anchorMax = Vector2.one; nr.offsetMin = new Vector2(0, 3); nr.offsetMax = new Vector2(0, -6);
                var d = Ui.Text(text, desc, Theme.FsBody, Theme.Sub, TextAlignmentOptions.TopLeft, false, "desc");
                var dr = d.rectTransform;
                dr.anchorMin = Vector2.zero; dr.anchorMax = new Vector2(1, 0.5f); dr.offsetMin = new Vector2(0, 4); dr.offsetMax = new Vector2(0, -5);
                d.textWrappingMode = TextWrappingModes.NoWrap; d.overflowMode = TextOverflowModes.Ellipsis;
                d.lineSpacing = 0;
                d.enableAutoSizing = true; d.fontSizeMax = d.fontSize; d.fontSizeMin = Theme.FsSm * Settings.TextScale;   // 좁은 화면에서도 한 줄
                descText = d;
            }
            else nm.rectTransform.Fill();
            return Ui.Rect("ctrl", row).At(1, 0.5f, -14, 0, ctrlW, ctrlH);
        }

        // 스위치 줄 — 줄 어디를 눌러도 켬/끔(올리면 줄 둘레에 금 테). 돌려줌: 설명 글
        TextMeshProUGUI SetToggle(RectTransform parent, string name, string desc, bool on, Action<bool> set, string hot)
        {
            const float PW = 84, PH = 40, KS = 32;
            var ctrl = SetRow(parent, name, desc, PW, PH, out var descText, out var row);
            var pill = Ui.Img(ctrl, Theme.Pill, Color.white, "pill"); pill.rectTransform.Fill();
            var knob = Ui.Img(pill.transform, Theme.S("circle"), Color.white, "knob"); knob.rectTransform.At(0, 0.5f, 4, 0, KS, KS);
            var hit = Ui.Img(row, Theme.Frame, Theme.Gold.A(0), "hit", true); hit.rectTransform.Fill(0, 4, 0, 4);
            var b = hit.gameObject.AddComponent<Btn>();
            b.Hl = hit;
            bool state = on;
            void Paint(bool anim)
            {
                pill.color = state ? Theme.Gold : Theme.Hex("2E3A5E");
                float to = state ? PW - KS - 4 : 4;
                var krt = knob.rectTransform;
                if (!anim) krt.anchoredPosition = new Vector2(to, 0);
                else { var from = krt.anchoredPosition.x; Tw.Run(krt, 0.18f, k => { if (krt) krt.anchoredPosition = new Vector2(Mathf.Lerp(from, to, k), 0); }); }
            }
            Paint(false);
            b.OnClick = () => { state = !state; set(state); Paint(true); };
            if (hot != null) Stage.Hot[hot] = b;
            return descText;
        }

        // 막대 줄 — 0~100, 오른쪽 끝에 숫자
        void SetSlider(RectTransform parent, string name, string desc, float value, Action<float> set, string hot)
        {
            var ctrl = SetRow(parent, name, desc, Theme.C(460, 440), 44, out _, out _);
            var track = Ui.Img(ctrl, Theme.Pill, Theme.NavyWell, "track", true);
            var tr = track.rectTransform;
            tr.anchorMin = new Vector2(0, 0.5f); tr.anchorMax = new Vector2(1, 0.5f);
            tr.offsetMin = new Vector2(0, -9); tr.offsetMax = new Vector2(-78, 9);
            var fillArea = Ui.Rect("fillArea", tr).Fill(4, 3, 4, 3);
            var fill = Ui.Img(fillArea, Theme.Pill, Theme.Gold, "fill");
            fill.rectTransform.sizeDelta = Vector2.zero;   // 슬라이더는 앵커만 옮긴다 — 기본 크기(100×100)가 남으면 판 밖으로 부풀었다
            var handleArea = Ui.Rect("handleArea", tr).Fill(12, 0, 12, 0);
            var handle = Ui.Img(handleArea, Theme.S("circle"), Color.white, "handle", true);
            handle.rectTransform.sizeDelta = new Vector2(34, 16);   // 세로는 막대(18) + 16 = 34
            var hring = Ui.Img(handle.transform, Theme.S("ring"), Theme.GoldDeep, "ring"); hring.rectTransform.Fill();
            var s = track.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = 0; s.maxValue = 1; s.value = value;
            var num = Ui.Title(ctrl, Mathf.RoundToInt(value * 100) + "", Theme.FsLg, Theme.Gold, TextAlignmentOptions.MidlineRight);
            num.rectTransform.Column(1, 64);
            s.onValueChanged.AddListener(v => { set(v); num.text = Mathf.RoundToInt(v * 100) + ""; });
        }

        // 작은 분할 단추 한 줄 — 칸 폭은 글 길이만큼 나눈다. 고른 칸은 금 알약 + 갈색 글
        Btn[] SetSegments(RectTransform ctrl, string[] labels, Action<int> pick, string hot)
        {
            var well = Ui.Img(ctrl, Theme.Pill, Theme.NavyWell, "well"); well.rectTransform.Fill();
            var segs = Ui.Rect("segs", ctrl).Fill(4, 4, 4, 4);
            Ui.Row(segs, 4, TextAnchor.MiddleCenter, null, true, true);
            var bs = new Btn[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int k = i;
                var bg = Ui.Img(segs, Theme.Pill, new Color(1, 1, 1, 0), "seg " + labels[i], true);
                bg.Pref(0, -1, labels[i].Length + 3);
                var b = bg.gameObject.AddComponent<Btn>();
                b.Bg = bg; b.SetColor(new Color(1, 1, 1, 0));
                b.Hl = Ui.Img(bg.transform, Theme.FramePill, Theme.Gold.A(0), "hl");
                b.Hl.rectTransform.Fill();
                var t = Ui.Title(bg.transform, labels[i], Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
                t.rectTransform.Fill(6, 2, 6, 2);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                t.enableAutoSizing = true; t.fontSizeMin = Theme.FsCap; t.fontSizeMax = t.fontSize;
                b.Label = t;
                b.OnClick = () => pick(k);
                bs[i] = b;
                if (hot != null) Stage.Hot[hot + i] = b;
            }
            return bs;
        }

        static void SegOn(Btn b, bool on)
        {
            if (b == null) return;
            b.SetColor(on ? Theme.Gold : new Color(1, 1, 1, 0));
            if (b.Label) b.Label.color = on ? Theme.Brown : Theme.Ink;
        }

        /// <summary>바꾼 화면을 둘지 묻는다 — 10초 안에 「이대로」 를 누르지 않으면 앞 화면으로 돌아간다.</summary>
        void KeepScreen(Action<bool> done)
        {
            var (panel, close) = Stage.Modal("keepscreen", 640, 300, false);
            var t = Ui.Title(panel, "이 화면으로 둘까요?", Theme.FsXl, Theme.Gold, TextAlignmentOptions.Center);
            t.rectTransform.Band(1, 46, 30, 30, -30);
            var b = Ui.Text(panel, "", Theme.FsBody, Theme.Sub, TextAlignmentOptions.Center);
            b.rectTransform.Band(1, 76, 40, 40, -82);
            var row = Ui.Rect("row", panel).Band(0, 62, 36, 36, 30);
            Ui.Row(row, Theme.Gap + 4, TextAnchor.MiddleCenter, null, true, true);
            bool over = false;
            void End(bool keep)
            {
                if (over) return;
                over = true;
                if (keep) DisplayOptions.Keep(); else DisplayOptions.Revert();
                close();
                done?.Invoke(keep);
                if (!keep) Toast.Show("앞 화면으로 돌아갔습니다");
            }
            var no = Btn.Make(row, "되돌리기", BtnStyle.Dark, () => End(false), Theme.FsMd);
            var ok = Btn.Make(row, "이대로", BtnStyle.Gold, () => End(true), Theme.FsMd);
            Stage.Hot["disp.revert"] = no;
            Stage.Hot["disp.keep"] = ok;
            StartCoroutine(Count());
            System.Collections.IEnumerator Count()
            {
                float left = 10f;
                while (!over && left > 0)
                {
                    if (b) b.text = $"{DisplayOptions.Describe()}\n<color={Theme.GoldTag}><size=130%>{Mathf.CeilToInt(left)}</size></color>초 안에 누르지 않으면 앞 화면으로 돌아갑니다";
                    left -= Time.unscaledDeltaTime;
                    yield return null;
                }
                End(false);
            }
        }

        /// <summary>사도 한 장 — 머리 띠 초상을 누르면. 밝은 사도 상세(Flow.Roster.cs)로 연다(파티 셋을 위아래로 넘긴다).</summary>
        public void HeroSheet(string coreId)
        {
            var h0 = Roster.OfCore(coreId);
            if (h0 != null) { HeroDetail(h0.key, P.S?.Party?.Select(k => Roster.OfCore(k)?.key).Where(x => x != null).ToList(), null); return; }
            HeroSheetOld(coreId);
        }

        void HeroSheetOld(string coreId)
        {
            var h = Roster.OfCore(coreId);
            var d = P.Data.Hero(coreId);
            var (panel, close) = Stage.Modal("hero", 900, 620);
            var face = W.Face(panel, h, 112); face.At(0, 1, 30, -26, 112, 112);
            var nm = Ui.Title(panel, h.ko, Theme.Fs2xl, Theme.Ink); nm.rectTransform.At(0, 1, 164, -32, 600, 52);
            var sub = Ui.Text(panel, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(h.nature))}>{h.nature}</color> · {h.race} · {h.role}", Theme.FsBody, Theme.Sub);
            sub.rectTransform.At(0, 1, 166, -88, 600, 30);
            var x = Btn.Icon(panel, Theme.S("ic_x"), close, 44, "close");
            x.GetComponent<RectTransform>().At(1, 1, -16, -16, 44, 44);
            Stage.Hot["hero.close"] = x;
            var rule = Ui.Img(panel, Theme.White, Theme.Line, "rule"); rule.rectTransform.Band(1, 1, 24, 24, -154);
            // 고학년 · 고유 효과 · 패시브 — 수치가 다 든 한 가지 글(core CardText.HeroShort = Traits), 길면 칸 안에서 스크롤(「자세히」 없음)
            var area = Ui.Rect("area", panel).Fill(32, 70, 32, 170);
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 4, TextAnchor.UpperLeft, null, true, false);
            FullText(content, d != null ? P.Text.HeroShort(d) : h.blurb, Theme.FsSm + 1, Theme.Ink, coreId, "body", d != null);
            var gear = string.Join(" · ", P.GearOf(coreId).Select(kv => $"{kv.Key} {P.Data.Equip(kv.Value)?.Name}"));
            var rule2 = Ui.Img(panel, Theme.White, Theme.Line, "rule2"); rule2.rectTransform.Band(0, 1, 24, 24, 58);
            var g = Ui.Text(panel, $"<color={Theme.GoldTag}>장비</color>  " + (gear.Length > 0 ? gear : $"<color={Theme.DimTag}>없음</color>"), Theme.FsSm + 1, Theme.Ink);
            g.rectTransform.Band(0, 30, 32, 32, 16);
        }
    }
}
