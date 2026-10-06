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
        public void SettingsPanel(bool inRun)
        {
            var (body, close, bar) = Stage.ModalBox("settings", 1200, 640, "설정", "소리 · 움직임 · 글자 · 화면 — 바로 저장됩니다", true, null, 84);
            // 왼쪽 = 소리 · 움직임, 오른쪽 = 화면(가운데 가는 세로 선)
            var left = Ui.Rect("left", body); left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0.45f, 1); left.offsetMin = Vector2.zero; left.offsetMax = new Vector2(-20, 0);
            var right = Ui.Rect("right", body); right.anchorMin = new Vector2(0.45f, 0); right.anchorMax = new Vector2(1, 1); right.offsetMin = new Vector2(20, 0); right.offsetMax = Vector2.zero;
            var vr = Ui.Img(body, Theme.White, Theme.Line, "vrule"); vr.rectTransform.anchorMin = new Vector2(0.45f, 0); vr.rectTransform.anchorMax = new Vector2(0.45f, 1);
            vr.rectTransform.sizeDelta = new Vector2(1, -16);
            Ui.Col(left, Theme.C(10, 8), TextAnchor.UpperLeft, new RectOffset(6, 6, 0, 0), true, false);
            W.Section(left, "소리 · 움직임", null, 40);
            SliderRow(left, "전체 소리", Settings.Master, v => Settings.Master = v);
            SliderRow(left, "효과음", Settings.SfxVol, v => Settings.SfxVol = v);
            SliderRow(left, "목소리", Settings.Voice, v => Settings.Voice = v);
            Toggle(left, "움직임 줄이기", "전환 · 등장 연출을 짧게", Settings.ReduceMotion, v => Settings.ReduceMotion = v);
            Toggle(left, "화면 흔들림 끄기", "타격 · 고학년 흔들림 없이", Settings.NoShake, v => Settings.NoShake = v);
            Toggle(left, "글자 크게", "다음 화면부터", Settings.BigText, v => Settings.BigText = v);
            Toggle(left, "컷인 건너뛰기", "고학년 컷인 없이 바로", Settings.SkipCutin, v => Settings.SkipCutin = v);
            Toggle(left, "고학년 짧게 보기", "끄면 판에서 처음 쓸 때만 길게", Settings.UltShort, v => Settings.UltShort = v);
            DisplaySection(right);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, new RectOffset(10, 10, 14, 14), false, true);
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

        void SliderRow(RectTransform parent, string label, float value, Action<float> set)
        {
            var row = Ui.Rect(label, parent); row.Pref(-1, Theme.C(52, 48));
            var t = Ui.Title(row, label, Theme.FsMd, Theme.Ink); t.rectTransform.Column(0, 160);
            var track = Ui.Img(row, Theme.Pill, Theme.NavyWell, "track", true);
            track.rectTransform.anchorMin = new Vector2(0, 0.5f); track.rectTransform.anchorMax = new Vector2(1, 0.5f);
            track.rectTransform.offsetMin = new Vector2(180, -8); track.rectTransform.offsetMax = new Vector2(-70, 8);
            var fillArea = Ui.Rect("fillArea", track.rectTransform).Fill(4, 3, 4, 3);
            var fill = Ui.Img(fillArea, Theme.Pill, Theme.Gold, "fill");
            fill.rectTransform.sizeDelta = Vector2.zero;   // 슬라이더는 앵커만 옮긴다 — 기본 크기(100×100)가 남으면 판 밖으로 부풀었다
            var handleArea = Ui.Rect("handleArea", track.rectTransform).Fill(10, 0, 10, 0);
            var handle = Ui.Img(handleArea, Theme.S("circle"), Color.white, "handle", true);
            handle.rectTransform.sizeDelta = new Vector2(30, 14);   // 세로는 막대(16) + 14 = 30
            var hring = Ui.Img(handle.transform, Theme.S("ring"), Theme.GoldDeep, "ring"); hring.rectTransform.Fill();
            var s = track.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = 0; s.maxValue = 1; s.value = value;
            var num = Ui.Title(row, Mathf.RoundToInt(value * 100) + "", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineRight);
            num.rectTransform.Column(1, 60);
            s.onValueChanged.AddListener(v => { set(v); num.text = Mathf.RoundToInt(v * 100) + ""; });
        }

        void Toggle(RectTransform parent, string label, string sub, bool on, Action<bool> set)
        {
            var b = Btn.Make(parent, null, BtnStyle.Cell, null, 0, label);
            b.Pref(-1, Theme.C(56, 52));
            var t = Ui.Title(b.transform, label + (sub != null ? $"  <size=68%><color={Theme.SubTag}>{sub}</color></size>" : ""), Theme.FsMd, Theme.Ink); t.rectTransform.Fill(20, 0, 110, 0);
            var pill = Ui.Img(b.transform, Theme.Pill, Color.white, "pill"); pill.rectTransform.At(1, 0.5f, -18, 0, 76, 36);
            var knob = Ui.Img(pill.transform, Theme.S("circle"), Color.white, "knob"); knob.rectTransform.At(0, 0.5f, 4, 0, 28, 28);
            bool state = on;
            void Paint(bool anim)
            {
                pill.color = state ? Theme.Gold : Theme.Hex("2E3A5E");
                var to = state ? 44f : 4f;
                var krt = knob.rectTransform;
                if (!anim) krt.anchoredPosition = new Vector2(to, 0);
                else { var from = krt.anchoredPosition.x; Tw.Run(krt, 0.18f, k => { if (krt) krt.anchoredPosition = new Vector2(Mathf.Lerp(from, to, k), 0); }); }
            }
            Paint(false);
            b.OnClick = () => { state = !state; set(state); Paint(true); };
        }

        // ── 화면(DisplayOptions) — 창 모드 · 해상도 · 프레임 · 수직동기 ──
        // 창 모드 · 해상도는 바꾸면 바로 시험 적용(Try)하고 「10초 안에 확인」 창을 띄운다 — 확인이 없으면 앞 화면으로.
        void DisplaySection(RectTransform col)
        {
            Ui.Col(col, Theme.C(8, 6), TextAnchor.UpperLeft, new RectOffset(6, 6, 0, 0), true, false);
            var head = W.Section(col, "화면", DisplayOptions.Describe(), 40);
            void Head() { if (head) head.text = $"화면  <size=68%><color={Theme.SubTag}>{DisplayOptions.Describe()}</color></size>"; }

            bool desk = !Application.isMobilePlatform && !DisplayOptions.Web;   // 폰은 창 · 해상도가 없다 — 프레임 · 수직동기만(웹은 전체 화면 스위치)
            if (DisplayOptions.Web)
                Toggle(col, "전체 화면", "브라우저 창을 꽉 채웁니다 · Esc 로 나옵니다", Screen.fullScreen, v => { DisplayOptions.SetWebFullScreen(v); Head(); });
            // 창 모드
            if (desk) Label(col, "화면 모드", null);
            var modes = new Btn[DisplayOptions.Modes.Count];
            var presets = new Btn[DisplayOptions.Presets.Count];
            var fps = new Btn[DisplayOptions.FrameCaps.Count];
            int curMode = DisplayOptions.ModeIndex, curPreset = DisplayOptions.PresetIndex;
            TextMeshProUGUI hint = null;
            if (desk)
            {
                var mrow = Ui.Rect("modes", col); mrow.Pref(-1, 50);
                Ui.Row(mrow, 8, TextAnchor.MiddleLeft, null, true, true);
                for (int i = 0; i < modes.Length; i++)
                {
                    int k = i;
                    var b = Choice(mrow, DisplayOptions.Modes[i].Name, null, () => Pick(k, -1));
                    if (!DisplayOptions.ModeAvailable(i)) { b.Interactable = false; b.Why = "이 기기에서는 쓸 수 없습니다"; }
                    modes[i] = b;
                    Stage.Hot["disp.mode" + i] = b;
                }
                hint = Ui.Text(col, "", Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                hint.Pref(-1, 20);
                hint.textWrappingMode = TextWrappingModes.NoWrap; hint.overflowMode = TextOverflowModes.Ellipsis;

                // 해상도
                var mon = DisplayOptions.Monitor;
                Label(col, "해상도", $"이 모니터 {mon.x}×{mon.y} · 더 큰 것은 잠깁니다");
                var prow = Ui.Rect("presets", col); prow.Pref(-1, 72);
                Ui.Row(prow, 8, TextAnchor.MiddleLeft, null, true, true);
                for (int i = 0; i < presets.Length; i++)
                {
                    int k = i;
                    var p = DisplayOptions.Presets[i];
                    var s = DisplayOptions.SizeOf(i);
                    var b = Choice(prow, p.Name, $"{s.x}×{s.y}", () => Pick(-1, k));
                    if (!DisplayOptions.Fits(i))
                    {
                        b.Interactable = false;
                        b.Why = $"이 모니터({mon.x}×{mon.y})보다 큽니다";
                        var lk = Ui.Img(b.transform, Theme.S("ic_lock"), Theme.Sub, "lock"); lk.rectTransform.At(1, 1, -8, -8, 16, 16); lk.preserveAspect = true;
                    }
                    presets[i] = b;
                    Stage.Hot["disp.res" + i] = b;
                }
            }

            // 프레임 · 수직동기
            Label(col, "프레임 제한", "초당 그리는 횟수 — 낮추면 전기 · 열이 줄어듭니다");
            var frow = Ui.Rect("fps", col); frow.Pref(-1, 46);
            Ui.Row(frow, 8, TextAnchor.MiddleLeft, null, true, true);
            for (int i = 0; i < fps.Length; i++)
            {
                int k = i;
                fps[i] = Choice(frow, DisplayOptions.FrameCapName(i), null, () => { DisplayOptions.SetFrameCap(k); Paint(); });
                Stage.Hot["disp.fps" + i] = fps[i];
            }
            Toggle(col, "수직동기", "주사율에 맞춰 찢김 없이 그립니다 · 켜면 프레임 제한은 쉽니다", DisplayOptions.VSync, v => { DisplayOptions.SetVSync(v); Paint(); });

            void Paint()
            {
                for (int i = 0; i < modes.Length; i++) Chosen(modes[i], i == curMode);
                if (hint) hint.text = "· " + DisplayOptions.ModeHint(curMode);
                for (int i = 0; i < presets.Length; i++) Chosen(presets[i], i == curPreset);
                bool vs = DisplayOptions.VSync;
                for (int i = 0; i < fps.Length; i++)
                {
                    if (fps[i] == null) continue;
                    Chosen(fps[i], !vs && i == DisplayOptions.FpsIndex);
                    fps[i].Interactable = !vs;
                    fps[i].Why = vs ? "수직동기를 끄면 고를 수 있습니다" : null;
                }
                Head();
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
            // 크기는 다음 프레임에 바뀐다 — 머리 줄은 실제 화면이 바뀔 때마다 다시 쓴다(창이 닫히면 멈춘다)
            System.Collections.IEnumerator Watch()
            {
                string last = null;
                while (head)
                {
                    var d = DisplayOptions.Describe();
                    if (d != last) { last = d; Head(); }
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }
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

        // 고르기 칸 — 위 이름(Jua) · 아래 작은 설명. 고르면 금 3 테두리 판(cell_on) + 금빛 이름
        Btn Choice(RectTransform row, string name, string sub, Action go)
        {
            var b = Btn.Make(row, null, BtnStyle.Cell, go, 0, "choice " + name);
            var t = Ui.Title(b.transform, name, Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true; t.fontSizeMin = Theme.FsCap; t.fontSizeMax = Theme.FsMd;
            if (sub != null)
            {
                t.rectTransform.Fill(6, 30, 6, 8);
                var s = Ui.Text(b.transform, sub, Theme.FsCap, Theme.Sub, TextAlignmentOptions.Center);
                s.rectTransform.Fill(4, 8, 4, 40);
                s.textWrappingMode = TextWrappingModes.NoWrap; s.overflowMode = TextOverflowModes.Overflow;
                s.enableAutoSizing = true; s.fontSizeMin = 11; s.fontSizeMax = Theme.FsCap;
            }
            else t.rectTransform.Fill(6, 2, 6, 2);
            b.Label = t;
            return b;
        }

        static void Chosen(Btn b, bool on)
        {
            if (b == null || b.Bg == null) return;
            b.Bg.sprite = on ? Theme.CellOn : Theme.Cell;
            if (b.Label) b.Label.color = on ? Theme.Gold : Theme.Ink;
        }

        static void Label(RectTransform col, string title, string sub)
        {
            var t = Ui.Text(col, $"<color={Theme.SubTag}>{title}</color>" + (sub != null ? $"  <size=86%><color={Theme.DimTag}>{sub}</color></size>" : ""), Theme.FsSm, Theme.Sub, TextAlignmentOptions.BottomLeft);
            t.Pref(-1, 24);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
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
            var sub = Ui.Text(panel, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(h.nature))}>{h.nature}</color> · {h.race} · {h.role} · {h.RowKo}", Theme.FsBody, Theme.Sub);
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
