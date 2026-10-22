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
                stage.anchorMin = new Vector2(0, 0); stage.anchorMax = new Vector2(0.7f, 1);   // 메뉴가 얇은 아이콘 기둥이라(카제나식) 사도 칸을 넓혔다
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
                var pt = Ui.Title(plate.transform, $"<size=68%><color={Theme.SubTag}>메인 사도</color></size>  {hero?.ko ?? "에르핀(왕도)"}", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
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
                    pt.text = $"<size=68%><color={Theme.SubTag}>메인 사도</color></size>  {hero?.ko ?? "에르핀(왕도)"}";
                    bubble.Close();
                    bubble = HeroBubble.Show(stage, hero, lines[0], new Vector2(0, 1), new Vector2(70, -200), new Vector2(0, 1), 0, 0.25f, "lobby");
                });

                // 카제나식 배치(2026-10-21, Docs/참고_카제나_UI.md §메인 로비) — 오른쪽 끝 아이콘 세로 메뉴 하나에 모험 · 프론티어 · 도감 · 보관함 차례로
                //   (2026-10-22 사용자 — 큰 「모험 시작」 단추 없앰). 칸의 자세한 상태는 귀퉁이 배지로, 설정은 오른쪽 위 톱니
                bool hasSave = RunPort.HasSave;
                string summary = hasSave ? P.SaveSummary() : null;
                if (hasSave && summary == null) { RunPort.ClearSave(); hasSave = false; }
                float legalTop = 12 + 62 + Theme.C(14, 10);
                float pillH = Theme.C(50, 44);
                float railBottom = legalTop + (hasSave ? pillH + Theme.Gap : 0);
                if (hasSave)
                {
                    // 이어할 판이 있으면 메뉴 아래에 「새 모험」 — 지금 판을 버린다고 한 번 묻는다
                    var nw = Btn.Make(root, "새 모험", BtnStyle.PillDark, () =>
                        Confirm("지금 모험을 버릴까?", "이어하던 모험은 사라진다", "버리고 출발", () => { var sr = P.SavedRun(); PlayRecord.Send(sr, "abandon"); ProgStore.Earn(sr, "abandon"); RunPort.ClearSave(); NewAdventure(); }, true), Theme.FsSm, "new");
                    nw.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, legalTop, Theme.C(118, 100), pillH);
                    Stage.Hot["new"] = nw;
                }

                // 오른쪽 끝 — 아이콘 세로 메뉴. 칸 높이는 위 띠(버전 알약) 아래 · 안내 판 위 남은 높이로 나눈다(폰 Compact 에서 특히)
                const int items = 4;   // 모험 · 프론티어 · 도감 · 보관함
                float railTop = Theme.C(22, 14) + Theme.C(46, 40) + Theme.Gap + 8;
                float railH = Stage.Size.y - railTop - railBottom;
                menuItemH = Mathf.Min(Theme.C(104, 86), (railH - Theme.Gap * (items - 1)) / items);
                var menu = Ui.Rect("menu", root);
                menu.anchorMin = new Vector2(1, 0); menu.anchorMax = new Vector2(1, 1); menu.pivot = new Vector2(1, 0.5f);
                menu.offsetMin = new Vector2(-Theme.Gutter - Theme.C(118, 100), railBottom); menu.offsetMax = new Vector2(-Theme.Gutter, -railTop);
                Ui.Col(menu, Theme.Gap, TextAnchor.MiddleRight, null, true, false);

                // 모험 — 이어할 판이 있으면 이어하기(배지), 없으면 새 모험(마을 고르기부터)
                var adv = MenuItem(menu, Theme.S("ic_swords"), "모험", Theme.Gold, () => { if (hasSave) Resume(); else NewAdventure(); });
                MenuBadge(adv, hasSave ? "이어하기" : "");
                Stage.Hot["start"] = adv;
                // 프론티어(보스 랭킹전 — Flow.Frontier.cs) — 서버가 열려 있으면 시즌 이름, 없으면 「준비 중」 배지(보스 소개 · 보관함 기기 사본은 열린다)
                var fr = MenuItem(menu, Theme.S("ic_crown"), "프론티어", Theme.Hex("FF9A7A"), FrontierScreen);
                Stage.Hot["frontier"] = fr;
                void PaintFr()
                {
                    if (!fr) { Online.Changed -= PaintFr; return; }
                    MenuBadge(fr, Online.Up ? (Online.Season?["label"]?.ToString() ?? "프리시즌") : Online.Now == Online.State.Unknown ? "…" : "준비 중", !Online.Up);
                }
                Online.Changed += PaintFr;
                PaintFr();
                Stage.Hot["dex"] = MenuItem(menu, Theme.S("ic_book"), "도감", Theme.Hex("7FE0B4"), () => Dex(Lobby));
                // 보관함(옛 「사도 저장함」, 2026-10-19 — 프론티어 밖에서도) — 로그인했으면 계정 칸, 아니면 이 기기 사본(Flow.HeroSave.cs). 배지 = 저장한 덱 수
                var bx = MenuItem(menu, Theme.S("ic_deck"), "보관함", Theme.Hex("8FC8FF"), () => SlotBox(Lobby));
                Stage.Hot["lobby.box"] = bx;
                void PaintBox()
                {
                    if (!bx) { Online.Changed -= PaintBox; return; }
                    MenuBadge(bx, SlotStore.Copies.Count > 0 ? SlotStore.Copies.Count.ToString() : "");
                }
                Online.Changed += PaintBox;
                PaintBox();
                // 설정 — 오른쪽 위 끝 톱니(카제나 ☰ 자리). 버전 · 계정 알약은 그 왼쪽으로 한 칸 비킨다(LobbyCogW)
                var cog = Btn.Icon(root, Theme.S("ic_cog"), () => SettingsPanel(false), Theme.C(46, 40), "settings");
                cog.GetComponent<RectTransform>().At(1, 1, -Theme.Gutter - 8, -Theme.C(22, 14), Theme.C(46, 40), Theme.C(46, 40));
                Stage.Hot["settings"] = cog;
                Tw.Pop((RectTransform)cog.transform, 0.5f, 0.85f, 0.4f);

                int i = 0;
                foreach (Transform c in menu) Tw.Pop((RectTransform)c, 0.2f + 0.06f * i++, 0.85f, 0.4f);

                logo.SetAsLastSibling();
                // 비공식 팬게임 안내 — 배경 위에서도 읽히게 마을 공개 창과 같은 남색 판(금 테 · 둥근 모서리)을 깔고 오른쪽 아래 작게(사용자 2026-10-07)
                var legalBox = Ui.Panel(root, Theme.Panel, null, "legal").rectTransform;
                legalBox.At(1, 0, -Theme.Gutter, 12, Mathf.Min(560, Stage.Size.x - 2 * Theme.Gutter), 62);
                legalBox.GetComponent<UnityEngine.UI.Image>().color = Color.white.A(0.9f);
                var legal = Ui.Text(legalBox, $"<color={Theme.GoldTag}>비공식 팬 게임 · 비영리</color> — 트릭컬 리바이브의 그림 · 음성 · 설정의 저작권은 EPID Games 에 있다. 공식과 무관하며, 권리자의 요청이 있으면 즉시 내린다.", Theme.FsCap - 1, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                legal.rectTransform.Fill(16, 6, 14, 6); legal.textWrappingMode = TextWrappingModes.Normal;
                legal.enableAutoSizing = true; legal.fontSizeMin = 10; legal.fontSizeMax = Theme.FsCap - 1;
                LobbyVersion(root);   // 오른쪽 위 — 버전 · 업데이트 내용(Flow.PatchNotes.cs)
                LobbyAccount(root);   // 그 왼쪽 — 계정(프론티어 로그인 · 닉네임, Flow.Online.cs)
                LobbyDeckLab(root);   // 왼쪽 위 이름 아래 — 「덱 만들기 · 시험」(이 PC 시험 모드 -decklab · 시험 계정만 보인다, Flow.DeckLab.cs)
                LobbyProg(root);      // 왼쪽 위 이름 아래 — 잠재력 · 재료 · 최고 완주 단계(성장 켬일 때만, Flow.Prog.cs)
            });
        }

        /// <summary>오른쪽 위 설정 톱니가 차지하는 너비(간격 포함) — 버전 · 계정 알약이 이만큼 왼쪽으로 비킨다.</summary>
        static float LobbyCogW => Theme.C(46, 40) + 10;

        float menuItemH = 104;
        // 아이콘 세로 메뉴 한 칸 — 위에 동그란 그림, 아래에 이름. 상태는 오른쪽 위 귀퉁이 배지(MenuBadge)
        Btn MenuItem(Transform parent, Sprite icon, string title, Color tone, System.Action go)
        {
            var b = Btn.Make(parent, null, BtnStyle.Dark, go, 0, "item " + title);
            float h = menuItemH, d = Mathf.Min(Theme.C(56, 46), h * 0.56f);
            b.Pref(-1, h);
            var disc = Ui.Img(b.transform, Theme.S("circle"), Theme.NavyWell, "disc");
            disc.rectTransform.At(0.5f, 1, 0, -h * 0.1f, d, d);
            var ring = Ui.Img(disc.transform, Theme.S("ring"), tone.A(0.9f), "ring"); ring.rectTransform.Fill();
            // 원작 그림(RunArt)은 제 색 그대로 크게, 화면 아이콘(RunUI/Sprites)은 칸 색으로 칠한다
            bool art = icon != null && icon.name.StartsWith("RunArt/");
            float pad = art ? 4 : d * 0.24f;
            var ic = Ui.Img(disc.transform, icon, art ? Color.white : tone, "icon");
            ic.rectTransform.Fill(pad, pad, pad, pad); ic.preserveAspect = true;
            var t = Ui.Title(b.transform, title, Theme.C(Theme.FsSm + 1, Theme.FsCap), Theme.Ink, TextAlignmentOptions.Bottom);
            t.rectTransform.Fill(4, h * 0.08f, 4, h * 0.7f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true; t.fontSizeMin = Theme.FsCap - 2; t.fontSizeMax = t.fontSize;
            return b;
        }

        /// <summary>메뉴 칸 오른쪽 위 귀퉁이 배지 — 빈 글이면 숨긴다. dim = 못 쓰는 상태(준비 중 · 로그인 전)라 흐린 글.</summary>
        static void MenuBadge(Btn item, string text, bool dim = false)
        {
            var host = item.transform.Find("badge");
            TextMeshProUGUI t;
            if (host == null)
            {
                var bg = Ui.Img(item.transform, Theme.S("pill_dark", 46), Color.white, "badge");
                bg.rectTransform.At(1, 1, 6, -4, 40, 24);
                t = Ui.Title(bg.transform, "", Theme.FsCap - 1, Theme.Gold, TextAlignmentOptions.Center);
                t.rectTransform.Fill(8, 0, 8, 0);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                host = bg.transform;
            }
            else t = host.GetComponentInChildren<TextMeshProUGUI>();
            host.gameObject.SetActive(!string.IsNullOrEmpty(text));
            t.text = text;
            t.color = dim ? Theme.Sub : Theme.Gold;
            ((RectTransform)host).sizeDelta = new Vector2(Mathf.Max(34, t.GetPreferredValues(text).x + 18), 24);
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
