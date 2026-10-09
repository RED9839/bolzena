using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 로비 — 웹판 js/lobby.js. 메인 사도(스탠딩 스파인)가 크게 서고 오른쪽에 메뉴가 세로로 선다.
    // 사도를 누르면 반응 동작 + 말풍선. 이어할 판이 있으면 「이어하기」 가 맨 위에, 「새 모험」 은 그 판을 버린다고 한 번 묻는다.
    public partial class Flow
    {
        public void Lobby()
        {
            Tw.After(1.2f, PlayRecord.NoticeOnce);   // 처음 실행 때 한 번 — 플레이 기록 안내
            Stage.SetBg("stage1_1", 0.1f);
            Stage.Show("lobby", root =>
            {
                // 왼쪽 위 — 이름
                var logo = Ui.Rect("logo", root).At(0, 1, 34, -24, 520, 110);
                var lstar = Ui.Img(logo, Theme.S("ic_spark"), Theme.Gold, "spark");
                lstar.rectTransform.At(0, 1, 0, -14, 46, 46);
                Tw.Breathe(lstar.transform, 0.1f, 1.8f);
                var lt = Ui.Title(logo, "볼제나", 64, Theme.Ink, TextAlignmentOptions.TopLeft);
                lt.rectTransform.Band(1, 76, 56);
                lt.Outline(0.22f);
                lt.textWrappingMode = TextWrappingModes.NoWrap;
                var ls = Ui.Title(logo, "사도들과 떠나는 카드 모험", 22, Theme.Ink, TextAlignmentOptions.TopLeft);
                ls.rectTransform.Band(1, 30, 6, 0, -78);
                ls.Outline(0.25f);
                Tw.Rise(logo, 0.05f, 20, 0.5f, Vector2.left);

                // 가운데 — 메인 사도
                var stage = Ui.Rect("hero", root);
                stage.anchorMin = new Vector2(0, 0); stage.anchorMax = new Vector2(0.62f, 1);
                stage.offsetMin = Vector2.zero; stage.offsetMax = Vector2.zero;
                var floor = Ui.Img(stage, Theme.S("soft"), new Color(0, 0, 0, 0.45f), "shadow");
                floor.rectTransform.At(0.5f, 0, 40, 10, 520, 90);
                // 스탠딩 — 캔버스 높이에 비례(예전 800 고정은 폰 · 저해상도 캔버스 720 에서 줄지 않았다) · 표(standing_fit)로 같은 구도
                var hero = LobbyHeroInfo();
                var standHost = Ui.Rect("standhost", stage).Fill();
                SkeletonGraphic sg = LobbyStanding(standHost, hero);
                var tap = Ui.Img(stage, Theme.White, new Color(0, 0, 0, 0), "tap", true);
                tap.rectTransform.At(0.5f, 0, 40, 0, Stage.Size.y * 0.52f, Stage.Size.y * 0.89f);
                // 사도 만지기(LobbyTouch) — 볼 당기기 · 간지럽히기 · 톡. 판정 자리는 사도 뼈에서 잡는다 — 이름판 · 바꾸기 단추 · 말풍선은 위에 있어 먼저 받는다
                var touch = tap.gameObject.AddComponent<LobbyTouch>();
                // 창 크기가 바뀌면(전체 화면 전환 · 창 늘이기) 스탠딩을 지금 캔버스 크기로 다시 세운다 — 칸 · 배율 · ClampInto 모두 Stage.Size 로 정하므로
                Stage.WhenResized(standHost, () =>
                {
                    foreach (Transform c in standHost) Destroy(c.gameObject);
                    sg = LobbyStanding(standHost, hero);
                    if (tap) tap.rectTransform.At(0.5f, 0, 40, 0, Stage.Size.y * 0.52f, Stage.Size.y * 0.89f);
                    if (touch) touch.Bind(sg, hero);
                });
                var tb = tap.gameObject.AddComponent<Btn>();
                tb.Bg = null;
                tb.enabled = false;   // 손가락은 LobbyTouch 가 받는다 — Btn 은 자동 데모의 Press("hero")(가짜 톡)만

                // 말풍선
                var lines = LinesOf(hero);
                var bubble = HeroBubble.Show(stage, hero, lines[0], new Vector2(0, 1), new Vector2(70, -200), new Vector2(0, 1), 0, 0.5f, "lobby");   // 사도 말풍선 진입점(HeroBubble.cs)
                int li = 0;
                tb.OnClick = () =>
                {
                    li = (li + 1) % lines.Length;
                    bubble.SetLine(lines[li], 0.35f);
                    if (sg != null)
                    {
                        var hit = SpineUi.PickAnim(sg.Skeleton.Data, "Happy_1", "Smile_1", "Touch", "Happy");
                        sg.AnimationState.SetAnimation(0, hit, false);
                        sg.AnimationState.AddAnimation(0, SpineUi.PickAnim(sg.Skeleton.Data, "Idle_1", "Idle"), true, 0);
                    }
                };
                Stage.Hot["hero"] = tb;
                touch.Tap = () => tb.OnClick?.Invoke();
                touch.Say = line => { if (bubble.Alive) bubble.SetLine(line); };
                touch.Bind(sg, hero);
                LobbyTouchNow = touch;

                // 이름판
                var plate = Ui.Img(stage, Theme.S("pill_dark", 46), Color.white, "plate");
                plate.rectTransform.At(0, 0, Theme.Gutter + 6, Theme.Gutter + 10, 430, 56);
                var pt = Ui.Title(plate.transform, $"<size=68%><color={Theme.SubTag}>메인 사도</color></size>  {hero?.ko ?? "에르핀"}", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                pt.rectTransform.Fill(26, 0, 10, 0);
                pt.textWrappingMode = TextWrappingModes.NoWrap;
                Tw.Rise(plate.rectTransform, 0.3f, 20, 0.4f, Vector2.down);
                // 메인 사도 바꾸기 — 이름판 오른쪽 동그란 단추 → 사도 고르기 창(Flow.LobbyHero.cs). 고르면 스탠딩만 부드럽게 바꾼다
                var swap = Btn.Icon(stage, Theme.S("ic_refresh"), null, 56, "swaphero");
                swap.GetComponent<RectTransform>().At(0, 0, Theme.Gutter + 6 + 430 + 12, Theme.Gutter + 10, 56, 56);
                Stage.Hot["lobby.swap"] = swap;
                swap.OnClick = () => LobbyHeroPicker(newHero =>
                {
                    hero = newHero;
                    lines = LinesOf(hero); li = 0;
                    var old = sg;
                    sg = null;
                    if (old != null) Tw.Run(old.rectTransform, 0.22f, t => { if (old) old.color = new Color(1, 1, 1, 1 - t); }, Tw.Linear, 0, () => { if (old) Destroy(old.gameObject); });
                    foreach (Transform c in standHost) if (c.name.StartsWith("still") || (c.name == "stand" && (old == null || !old.transform.IsChildOf(c)))) Destroy(c.gameObject);
                    sg = LobbyStanding(standHost, hero);   // 칸은 지금 캔버스 크기(Stage.Size)로 새로 만든다
                    if (tap) tap.rectTransform.At(0.5f, 0, 40, 0, Stage.Size.y * 0.52f, Stage.Size.y * 0.89f);
                    if (touch) touch.Bind(sg, hero);
                    if (sg != null) { sg.color = new Color(1, 1, 1, 0); var ng = sg; Tw.Run(ng.rectTransform, 0.3f, t => { if (ng) ng.color = new Color(1, 1, 1, t); }, Tw.Linear, 0.12f); Tw.Rise(ng.rectTransform, 0.12f, 18, 0.35f); }
                    pt.text = $"<size=68%><color={Theme.SubTag}>메인 사도</color></size>  {hero?.ko ?? "에르핀"}";
                    bubble.Close();
                    bubble = HeroBubble.Show(stage, hero, lines[0], new Vector2(0, 1), new Vector2(70, -200), new Vector2(0, 1), 0, 0.25f, "lobby");
                });

                // 오른쪽 — 메뉴
                var menu = Ui.Rect("menu", root);
                menu.anchorMin = new Vector2(1, 0); menu.anchorMax = new Vector2(1, 1); menu.pivot = new Vector2(1, 0.5f);
                menu.offsetMin = new Vector2(-470, Theme.C(110, 70)); menu.offsetMax = new Vector2(-Theme.Gutter - 8, Theme.C(-140, -90));
                Ui.Col(menu, Theme.Gap, TextAnchor.UpperRight, null, true, false);

                bool hasSave = RunPort.HasSave;
                string summary = hasSave ? P.SaveSummary() : null;
                if (hasSave && summary == null) { RunPort.ClearSave(); hasSave = false; }
                var start = BigStart(menu, hasSave ? "이어하기" : "모험 시작", hasSave ? summary : "마을 하나가 정해지면 사도 셋을 고릅니다",
                    () => { if (hasSave) Resume(); else NewAdventure(); });
                Stage.Hot["start"] = start;
                if (hasSave)
                    Stage.Hot["new"] = MenuItem(menu, Theme.S("ic_spark"), "새 모험", "지금 모험을 버리고 사도 셋을 새로 고릅니다", new Color(0.95f, 0.75f, 0.35f), () =>
                        Confirm("지금 모험을 버리겠습니까?", "이어하던 모험은 사라집니다. 새 마을로 떠납니다.", "버리고 출발", () => { PlayRecord.Send(P.SavedRun(), "abandon"); RunPort.ClearSave(); NewAdventure(); }, true));
                { var cs = CrayonStore.Save; var ct = CrayonStore.Table;
                  Stage.Hot["crayon"] = MenuItem(menu, Theme.BoardIcon, "교주 보드", $"크레파스 보드로 파티 영구 강화 · 칠한 단계 {ct.Cells.Where(x => !x.Blank).Sum(x => System.Math.Min(x.Levels, cs.LevelOf(x.Id)))}/{ct.Cells.Where(x => !x.Blank).Sum(x => x.Levels)}", Theme.Hex("FFB648"), CrayonScreen); }
                Stage.Hot["dex"] = MenuItem(menu, Theme.S("ic_book"), "도감", $"사도 {Roster.All.Count}명 · 적 · 아티팩트 · 교주 카드", Theme.Hex("7FE0B4"), () => Dex(Lobby));
                Stage.Hot["settings"] = MenuItem(menu, Theme.S("ic_cog"), "설정", "소리 · 움직임 · 글자 · 화면", Theme.Hex("B9A8FF"), () => SettingsPanel(false));

                int i = 0;
                foreach (Transform c in menu) Tw.Pop((RectTransform)c, 0.15f + 0.07f * i++, 0.85f, 0.4f);

                logo.SetAsLastSibling();
                // 비공식 팬게임 안내 — 배경 위에서도 읽히게 마을 공개 창과 같은 남색 판(금 테 · 둥근 모서리)을 깔고 오른쪽 아래 작게(사용자 2026-10-07)
                var legalBox = Ui.Panel(root, Theme.Panel, null, "legal").rectTransform;
                legalBox.At(1, 0, -Theme.Gutter, 12, Mathf.Min(560, Stage.Size.x - 2 * Theme.Gutter), 62);
                legalBox.GetComponent<UnityEngine.UI.Image>().color = Color.white.A(0.9f);
                var legal = Ui.Text(legalBox, $"<color={Theme.GoldTag}>비공식 팬 게임 · 비영리</color> — 트릭컬 리바이브의 그림 · 음성 · 설정의 저작권은 EPID Games 에 있습니다. 공식과 무관하며, 권리자의 요청이 있으면 즉시 내립니다.", Theme.FsCap - 1, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                legal.rectTransform.Fill(16, 6, 14, 6); legal.textWrappingMode = TextWrappingModes.Normal;
                legal.enableAutoSizing = true; legal.fontSizeMin = 10; legal.fontSizeMax = Theme.FsCap - 1;
            });
        }

        Btn BigStart(Transform parent, string title, string sub, System.Action go)
        {
            var b = Btn.Make(parent, null, BtnStyle.Gold, go, 0, "start");
            float h = Theme.C(124, 108);
            b.Pref(-1, h);
            var t = Ui.Title(b.transform, title, Theme.Fs2xl, Theme.Brown, TextAlignmentOptions.BottomLeft);
            t.rectTransform.Fill(30, h * 0.42f, 90, 10);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var s = Ui.Text(b.transform, sub, Theme.FsSm, Theme.Hex("5A3A10"), TextAlignmentOptions.TopLeft);
            s.rectTransform.Fill(32, 10, 90, h * 0.6f);
            s.textWrappingMode = TextWrappingModes.NoWrap;
            var glow = Ui.Img(b.transform, Theme.S("soft"), new Color(1f, 0.95f, 0.7f, 0.4f), "glow");
            glow.rectTransform.At(1, 0.5f, -50, 0, 140, 140);
            Tw.Pulse(glow, 0.1f, 0.5f, 1.8f);
            var play = Ui.Img(b.transform, Theme.S("ic_play"), new Color(0.35f, 0.18f, 0.04f, 0.85f), "play");
            play.rectTransform.At(1, 0.5f, -46, 0, 44, 44);
            return b;
        }

        Btn MenuItem(Transform parent, Sprite icon, string title, string sub, Color tone, System.Action go)
        {
            var b = Btn.Make(parent, null, BtnStyle.Dark, go, 0, "item " + title);
            float h = Theme.C(80, 70);
            b.Pref(-1, h);
            var disc = Ui.Img(b.transform, Theme.S("circle"), Theme.NavyWell, "disc");
            disc.rectTransform.At(0, 0.5f, 18, 0, 48, 48);
            var ring = Ui.Img(disc.transform, Theme.S("ring"), tone.A(0.9f), "ring"); ring.rectTransform.Fill();
            // 원작 그림(RunArt — 교주 보드의 크레파스)은 제 색 그대로 크게, 화면 아이콘(RunUI/Sprites)은 칸 색으로 칠한다
            bool art = icon != null && icon.name.StartsWith("RunArt/");
            var ic = Ui.Img(disc.transform, icon, art ? Color.white : tone, "icon");
            ic.rectTransform.Fill(art ? 4 : 12, art ? 4 : 12, art ? 4 : 12, art ? 4 : 12); ic.preserveAspect = true;
            var t = Ui.Title(b.transform, title, Theme.FsLg + 2, Theme.Ink, TextAlignmentOptions.BottomLeft);
            t.rectTransform.Fill(84, h * 0.46f, 16, 6);
            var s = Ui.Text(b.transform, sub, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft);
            s.rectTransform.Fill(86, 6, 16, h * 0.56f);
            s.textWrappingMode = TextWrappingModes.NoWrap;
            return b;
        }

        /// <summary>두 번 묻기 — 「정말?」 창.</summary>
        public void Confirm(string title, string body, string yes, System.Action onYes, bool danger = false)
        {
            var (panel, close) = Stage.Modal("confirm", 620, 290);
            var t = Ui.Title(panel, title, Theme.FsXl, danger ? Theme.Ink : Theme.Gold, TextAlignmentOptions.Center);
            t.rectTransform.Band(1, 46, 30, 30, -34);
            var b = Ui.Text(panel, body, Theme.FsBody, Theme.Sub, TextAlignmentOptions.Center);
            b.rectTransform.Band(1, 70, 40, 40, -88);
            var row = Ui.Rect("row", panel).Band(0, Theme.BtnH, 36, 36, 30);
            Ui.Row(row, Theme.Gap + 4, TextAnchor.MiddleCenter, null, true, true);
            var no = Btn.Make(row, "취소", BtnStyle.PillDark, close, Theme.FsMd);
            var ok = Btn.Make(row, yes, danger ? BtnStyle.PillRose : BtnStyle.PillGold, () => { close(); onYes(); }, Theme.FsMd);
            Stage.Hot["confirm.yes"] = ok;
            Stage.Hot["confirm.no"] = no;
        }
    }
}
