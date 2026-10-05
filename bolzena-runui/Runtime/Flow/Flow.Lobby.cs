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
        static readonly string[] LobbyLines =
        {
            "교주님, 오늘도 모험 가요?", "준비는 다 됐어요. 언제든지요!", "에르피엔 숲이 요즘 시끄럽대요.",
            "헤헤, 간지러워요.", "교주님 손, 따뜻하네요.", "여왕이 직접 나섰다구! 다들 물러서!",
        };

        public void Lobby()
        {
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
                var holder = Ui.Rect("stand", stage).At(0.5f, 0, 40, -10, 10, 10);
                var hero = Roster.ByKey(Settings.LobbyHero) ?? Roster.ByKey("에르핀");
                SkeletonGraphic sg = SpineUi.Make(holder, "st_" + (hero?.art ?? "erpin"), null, 800, "Idle_1", "Idle");
                if (sg == null && hero?.Icon != null)
                {
                    var im = Ui.Img(holder, hero.Icon, Color.white, "still");
                    im.rectTransform.At(0.5f, 0, 0, 340, 520, 520);
                }
                var tap = Ui.Img(stage, Theme.White, new Color(0, 0, 0, 0), "tap", true);
                tap.rectTransform.At(0.5f, 0, 40, 0, 460, 800);
                var tb = tap.gameObject.AddComponent<Btn>();
                tb.Bg = null;

                // 말풍선
                var bubbleT = W.Bubble(stage, hero?.ko ?? "에르핀", LobbyLines[0], 360, out var bubble);
                bubble.At(0, 1, 70, -200, 360, 100);
                Tw.Pop(bubble, 0.5f, 0.7f, 0.5f);
                int li = 0;
                tb.OnClick = () =>
                {
                    li = (li + 1) % LobbyLines.Length;
                    bubbleT.text = LobbyLines[li];
                    Tw.Pop(bubble, 0, 0.85f, 0.35f);
                    if (sg != null)
                    {
                        var hit = SpineUi.PickAnim(sg.Skeleton.Data, "Happy_1", "Smile_1", "Touch", "Happy");
                        sg.AnimationState.SetAnimation(0, hit, false);
                        sg.AnimationState.AddAnimation(0, SpineUi.PickAnim(sg.Skeleton.Data, "Idle_1", "Idle"), true, 0);
                    }
                };
                Stage.Hot["hero"] = tb;

                // 이름판
                var plate = Ui.Img(stage, Theme.S("pill_dark", 46), Color.white, "plate");
                plate.rectTransform.At(0, 0, Theme.Gutter + 6, Theme.Gutter + 10, 430, 56);
                var pt = Ui.Title(plate.transform, $"<size=68%><color={Theme.SubTag}>메인 사도</color></size>  {hero?.ko ?? "에르핀"}   <size=60%><color={Theme.SubTag}>누르면 반응합니다</color></size>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                pt.rectTransform.Fill(26, 0, 10, 0);
                pt.textWrappingMode = TextWrappingModes.NoWrap;
                Tw.Rise(plate.rectTransform, 0.3f, 20, 0.4f, Vector2.down);

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
                    Stage.Hot["new"] = MenuItem(menu, Theme.S("ic_spark"), "새 모험", "지금 판을 버리고 사도 셋을 새로 고릅니다", new Color(0.95f, 0.75f, 0.35f), () =>
                        Confirm("지금 판을 버릴까요?", "이어하던 판은 사라집니다. 새 마을로 떠납니다.", "버리고 떠납니다", () => { RunPort.ClearSave(); NewAdventure(); }, true));
                Stage.Hot["dex"] = MenuItem(menu, Theme.S("ic_book"), "도감", $"사도 {Roster.All.Count}명 · 적 · 장비 · 교주 카드", Theme.Hex("7FE0B4"), () => Dex(Lobby));
                Stage.Hot["settings"] = MenuItem(menu, Theme.S("ic_cog"), "설정", "소리 · 움직임 · 글자 · 화면", Theme.Hex("B9A8FF"), () => SettingsPanel(false));

                // 마을 목록
                var route = Ui.Panel(menu, Theme.Glass, new Color(1, 1, 1, 0.95f), "route");
                Ui.Col(route.rectTransform, 4, TextAnchor.UpperLeft, new RectOffset(22, 22, 14, 14));
                Ui.Title(route.transform, "마을 — 모험마다 하나", Theme.FsSm, Theme.Sub).Pref(-1, 24);
                foreach (var v in P.Villages)
                {
                    var line = Ui.Text(route.transform, $"<color={Theme.GoldTag}>★</color> {v.Name}  <size=82%><color={Theme.SubTag}>{string.Join(" → ", v.Floors.Select(f => f.Name))}</color></size>", Theme.FsBody, Theme.Ink);
                    line.Pref(-1, 28);
                    line.textWrappingMode = TextWrappingModes.NoWrap;
                }
                int i = 0;
                foreach (Transform c in menu) Tw.Pop((RectTransform)c, 0.15f + 0.07f * i++, 0.85f, 0.4f);

                logo.SetAsLastSibling();
                var legal = Ui.Text(root, $"<color={Theme.GoldTag}>비공식 팬 게임 · 비영리</color> — 트릭컬 리바이브의 그림 · 음성 · 설정의 저작권은 EPID Games 에 있습니다. 공식과 무관하며, 권리자의 요청이 있으면 즉시 내립니다.", Theme.FsCap, Theme.Sub, TextAlignmentOptions.BottomRight);
                legal.rectTransform.At(1, 0, -Theme.Gutter - 8, 14, 640, 44);
                legal.Outline(0.2f);
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
            var ic = Ui.Img(disc.transform, icon, tone, "icon");
            ic.rectTransform.Fill(12, 12, 12, 12); ic.preserveAspect = true;
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
            var row = Ui.Rect("row", panel).Band(0, 62, 36, 36, 30);
            Ui.Row(row, Theme.Gap + 4, TextAnchor.MiddleCenter, null, true, true);
            var no = Btn.Make(row, "그만둡니다", BtnStyle.Dark, close, Theme.FsMd);
            var ok = Btn.Make(row, yes, danger ? BtnStyle.Red : BtnStyle.Gold, () => { close(); onYes(); }, Theme.FsMd);
            Stage.Hot["confirm.yes"] = ok;
            Stage.Hot["confirm.no"] = no;
        }
    }
}
