using System;
using System.Linq;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 로비 메인 사도(2026-10 사용자: 「로비 메인 사도 변경도」) — 135명 가운데 하나를 골라 로비에 세운다.
    //   저장: PlayerPrefs bz.lobbyHero(Settings.LobbyHero). 기본 에르핀. 저장된 사도가 로스터에 없거나 스탠딩 스파인이 없으면 에르핀.
    //   스탠딩: 표(standing_fit · standing_head · standing_center)로 같은 구도 — 전신 · 같은 바닥선 · 키 배율 1(원작 비율: 작은 사도는 작게).
    //     크기는 캔버스 높이에 비례(저해상도 · 폰 캔버스 720 에서도 같은 구도). 기준 키 = 표 95%, 그보다 큰 사도는 칸에 맞게 줄인다.
    //   고르기 창: 도감과 같은 칸(정지 상반신) · 정렬(성급 → 가나다) · 검색(초성). 누르면 바로 바뀐다(확인 없음).
    public partial class Flow
    {
        const string DefaultLobbyHero = "에르핀";
        /// <summary>로비 스탠딩 상한(화면 높이 몫) — 2026-10-07 사용자: 예전 93% 는 「에르핀이 너무 크다」 · 넓은 화면에서 다리가 잘렸다 → 85%(기준 키 사도의 몸 = 화면 높이의 79%).</summary>
        const float LobbyCap = 0.85f;

        /// <summary>점검용(데모) — 저장(PlayerPrefs)을 건드리지 않고 로비 메인 사도를 바꿔 세운다.</summary>
        public static string DemoLobbyHero;
        /// <summary>점검용(데모) — 지금 로비의 사도 만지기 부품.</summary>
        public static LobbyTouch LobbyTouchNow;

        /// <summary>저장된 메인 사도 — 없거나 스탠딩 스파인이 없으면 에르핀.</summary>
        public static HeroInfo LobbyHeroInfo()
        {
            var h = Roster.ByKey(DemoLobbyHero ?? Settings.LobbyHero);
            if (h == null || h.art == null || SpineUi.Data("st_" + h.art) == null) h = Roster.ByKey(DefaultLobbyHero);
            return h;
        }

        /// <summary>그 사도를 메인 사도로 둘 수 있나(로스터 · 스탠딩 스파인).</summary>
        public static bool CanLobby(HeroInfo h) => h != null && h.art != null && SpineUi.Data("st_" + h.art) != null;

        /// <summary>로비 말풍선 대사 — 사도마다 원작 말투로(HeroLines · hero_lines.json). 없으면 존댓말 / 반말 공통 문구.</summary>
        static string[] LinesOf(HeroInfo h) => HeroLines.Of(h, "lobby");

        /// <summary>로비 스탠딩 — host(로비 왼쪽 62% 칸을 꽉 채운 것) 안에 전신으로. 스파인이 없으면 정지 그림.</summary>
        SkeletonGraphic LobbyStanding(RectTransform host, HeroInfo h)
        {
            float w = Stage.Size.x * 0.62f, hgt = Stage.Size.y;
            // 칸 폭은 높이로 묶는다(넓은 화면비에서 폭이 커져도 크기 · 자리는 높이 기준) — 왼쪽 62% 칸의 가운데
            float aw = Mathf.Min(w, hgt * 1.1f);
            var area = Ui.Rect("stand", host);
            area.anchorMin = area.anchorMax = area.pivot = Vector2.zero;
            area.sizeDelta = new Vector2(aw, hgt); area.anchoredPosition = new Vector2(40 + (w - aw) / 2, Stage.Size.y * 0.03f);   // 발이 화면 아래 끝에서 3% 위(2026-10-07 「에르핀 다리가 잘려」 — 예전 −1.2% 는 넓은 화면에서 발이 잘렸다)
            var sg = h != null ? SpineUi.Standing(area, h.art, aw, hgt * 0.86f, StandMode.Full, 0, 0, "lobby") : null;   // 떠 있는 사도는 얼굴을 기준 얼굴 높이에(standing_center_fix.json float · _faceRef)
            if (sg != null)
            {
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-testbig") >= 0) sg.rectTransform.localScale *= 2.2f;   // 점검: 배율이 어긋난 빌드 흉내(웹 「너무 큼」)
                // 마지막 울타리 — 그려진 크기가 화면 높이의 LobbyCap(85%) 를 넘거나 칸 폭을 넘으면 발을 두고 줄인다(빌드 · 배율이 달라도). 레이아웃이 자리 잡은 뒤 한 번 더
                SpineUi.ClampInto(sg, area, Stage.Root, LobbyCap, StandingFit.Floats(h.art));
                var sg0 = sg;
                Tw.After(0.1f, () => { if (sg0 && area) SpineUi.ClampInto(sg0, area, Stage.Root, LobbyCap, StandingFit.Floats(h.art)); });
                return sg;
            }
            var full = h != null ? CardArt.Standing(h.art) : null;
            if (full != null)
            {
                float fh = hgt * 0.89f, fw = fh * full.rect.width / full.rect.height;
                var im = Ui.Img(host, full, Color.white, "still"); im.rectTransform.At(0.5f, 0, 40, -10, fw, fh); im.preserveAspect = true;
            }
            else if (h?.Icon != null) { var im = Ui.Img(host, h.Icon, Color.white, "still"); im.rectTransform.At(0.5f, 0, 40, hgt * 0.38f, hgt * 0.58f, hgt * 0.58f); }
            return null;
        }

        /// <summary>메인 사도 고르기 창 — 고르면 저장하고 picked(사도)를 부른다. 창 밖 · 닫기로 닫는다.</summary>
        public void LobbyHeroPicker(Action<HeroInfo> picked)
        {
            var (body, close, _) = Stage.ModalBox("lobbyhero", Mathf.Min(1500, Stage.Size.x - 40), Stage.Size.y - Theme.C(60, 30), "메인 사도 바꾸기", "로비에 설 사도를 고릅니다 — 누르면 바로 바뀝니다");
            var cur = LobbyHeroInfo();
            // 위 — 검색 칸
            var tools = Ui.Rect("tools", body).At(1, 1, 0, 0, 900, 52);
            Ui.Row(tools, 10, TextAnchor.MiddleRight, null, false, true);
            var info = Ui.Text(body, "", Theme.FsMd, Theme.Sub, TextAlignmentOptions.MidlineLeft); info.rectTransform.At(0, 1, 6, 0, 520, 52);
            // 격자
            var area = Ui.Rect("grid", body).Fill(0, 0, 0, 64);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            float k = Theme.C(0.9f, 0.8f);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150 * k, 214 * k); grid.spacing = new Vector2(12, 12); grid.padding = new RectOffset(4, 16, 4, 12);
            var list = SortHeroes(Roster.All, "성급").ToList();
            int idx = 0;
            foreach (var h in list)
            {
                var hh = h;
                bool can = h.art != null && StandingFit.Has(h.art);   // 표에 있으면 스탠딩 스파인이 있다 — 135명 스켈레톤을 다 읽지 않게(누를 때만 CanLobby)
                var nc = NatureCol(h);
                var b = Btn.Make(content, null, BtnStyle.Ghost, () =>
                {
                    if (!CanLobby(hh)) { Toast.Show($"{hh.ko} — 스탠딩 그림이 아직 없습니다"); return; }
                    Settings.LobbyHero = hh.key;
                    PlayerPrefs.Save();
                    close();
                    picked?.Invoke(hh);
                }, 0, "hero " + h.key);
                b.Bg.sprite = Theme.Round; b.SetColor(can ? Color.Lerp(Theme.NavyWell, nc, 0.32f) : Theme.NavyCell);
                var rt = b.GetComponent<RectTransform>();
                var mask = Ui.Img(rt, Theme.Round, Color.white, "mask"); mask.rectTransform.Fill(2, 2, 2, 2); mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                HeroArt(mask.rectTransform, h, 150 * k - 4, 214 * k - 4, 0.5f, 0, 0, can ? (Color?)null : new Color(0.5f, 0.5f, 0.56f), false, true);
                var shade = Ui.Img(rt, Theme.S("fade_down"), Color.black.A(0.8f), "shade"); shade.rectTransform.Band(0, 70 * k, 2, 2, 2);
                var stars = Ui.Title(rt, $"<size=70%>★</size>{h.star}", Theme.FsLg, Theme.Gold, TextAlignmentOptions.BottomLeft); stars.rectTransform.At(0, 0, 8, 26 * k, 80, 30 * k); stars.Outline(0.25f);
                var nm = Ui.Title(rt, h.ko, Theme.FsBody, Color.white, TextAlignmentOptions.BottomRight); nm.rectTransform.Band(0, 26 * k, 6, 8, 6 * k);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 10; nm.fontSizeMax = Theme.FsBody; nm.Outline(0.25f);
                var rim = Ui.Img(rt, Theme.Frame, (can ? nc : Theme.Dim).A(0.85f), "rim"); rim.rectTransform.Fill();
                if (cur != null && h.key == cur.key)
                {
                    var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Gold, "now"); fr.rectTransform.Fill(-2, -2, -2, -2);
                    var (cb, ct) = Ui.Chip(rt, Theme.S("ic_check"), "지금", 26, Theme.Gold, Theme.Brown, Theme.FsCap); cb.rectTransform.At(1, 1, -6, -6, 70, 26); ct.color = Theme.Brown;
                }
                Stage.Hot["lobbyhero:" + h.key] = b;
                if (idx < 24) Tw.Pop(rt, 0.01f * idx, 0.9f, 0.25f);
                idx++;
            }
            info.text = $"{list.Count}명 · 지금 <color={Theme.GoldTag}>{cur?.ko}</color>";
            DexSearch(tools, "로비", content, area, n => n.StartsWith("hero ") && Roster.ByKey(n.Substring(5)) is HeroInfo x ? new[] { x.ko, x.key } : null, info, n => $"{n}명 · 지금 <color={Theme.GoldTag}>{cur?.ko}</color>");
            Stage.Hot["lobbyhero.close"] = Stage.Hot["modal.x"];
        }
    }
}
