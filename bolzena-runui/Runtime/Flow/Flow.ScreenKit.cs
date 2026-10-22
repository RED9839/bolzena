using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 화면 틀(2026-10-21 사용자 「UI 들 다듬고 통일해 줘, 깔끔하게」) — 로비에서 들어가는 메뉴 화면(편성 · 사도 목록 · 도감 · 단계 고르기 ·
    // 프론티어 · 사도 저장함 · 덱 만들기 · 기록 · 랭킹)이 모두 같은 머리 · 판 · 칩 · 빈 상태 · 잠김 표시를 쓴다(Docs/톤.md §11).
    // 판 안 화면(지도 · 캠프 · 상점 · 이벤트 · 보상)은 머리 띠(HUD)를, 창은 Stage.ModalBox 를 쓴다.
    public partial class Flow
    {
        /// <summary>내용이 시작하는 높이(머리 아래) · 판 안쪽 여백 · 판 사이 틈.</summary>
        const float FTop = 100, FPad = 24, FGap = 12;
        /// <summary>판 짙기 — 배경 그림 위에서도 글이 읽히게.</summary>
        const float FPanelAlpha = 0.93f;
        /// <summary>머리 칩 높이 · 빈 상태 기본 높이.</summary>
        const float HeadChipH = 40, EmptyH = 120;

        /// <summary>오른쪽 위 묶음(재화 칩 · 홈 │ 설정)의 너비 — 마지막 ScreenHead 가 정한다. 오른쪽 위에 도구 줄(정렬 · 탭)을 두는 화면은 HeadToolsX 만큼 비킨다.</summary>
        float HeadRightW;
        /// <summary>오른쪽 위 도구 줄의 오른쪽 끝 x(At(1, 1, x, …)) — 오른쪽 묶음 왼쪽.</summary>
        float HeadToolsX => -Theme.Gutter - HeadRightW - 14;

        /// <summary>판 안 화면 — 홈 단추를 두지 않고, 설정은 판 안 설정(모험 포기 등)으로 연다.</summary>
        bool InRunScreen => Stage.Current is "map" or "camp" or "shop" or "event" or "fight" or "reward" or "bosscopy";

        /// <summary>
        /// 화면 머리(카제나 공통 화면 틀 — Docs/참고_카제나_UI.md §2-1) — 왼쪽 위 뒤로(54) · 제목(Fs2xl−4) · (정보 표시) · (칩) · 짧은 풀이 한 줄(Sm · 부글),
        /// 오른쪽 위 「그 화면의 재화 칩(wallet — 넘긴 화면만) · 홈 │ 설정」. 홈은 로비로(판 안 화면이면 없다). 모든 메뉴 화면이 같은 자리 · 크기.
        /// 오른쪽 위에 다른 단추 줄을 두는 화면은 HeadToolsX 에 붙인다. onBack: 뒤로 키도 이 동작(창 층 위에 세운 머리는 false — 창은 BackClose 가 닫는다).
        /// </summary>
        RectTransform ScreenHead(RectTransform root, string title, string hint, Action back, Action<RectTransform> chips = null, bool info = false, bool onBack = true, Action<RectTransform> wallet = null)
        {
            // 오른쪽 위 — 재화 칩 · 홈 │ 설정(톱니). 너비를 먼저 재서 왼쪽 머리가 그만큼 비킨다
            bool inRun = InRunScreen;
            // 창 층 위 머리(사도 목록 · 상세)는 밑 화면의 홈 · 설정 키를 덮지 않는다 — 창을 닫은 뒤에도 밑 화면 단추가 그대로 눌리게(자동 데모 Press)
            bool keep(string k) => root.IsChildOf(Stage.ModalLayer) && Stage.Hot.TryGetValue(k, out var b0) && b0 != null;
            var right = Ui.Rect("headR", root).At(1, 1, -Theme.Gutter, -18, 10, 64);
            right.pivot = new Vector2(1, 1);
            Ui.Row(right, 10, TextAnchor.MiddleRight, null, false, false);
            wallet?.Invoke(right);
            if (!inRun)
            {
                if (wallet != null) { var gap = Ui.Rect("gap", right); gap.Pref(4, 10); }
                var home = Btn.Icon(right, Theme.S("ic_home"), Lobby, Theme.IconBtn, "home");
                if (!keep("home")) Stage.Hot["home"] = home;
                var bar = Ui.Img(right, Theme.White, Theme.Edge.A(0.45f), "bar"); bar.Pref(2, 30);
            }
            var cog = Btn.Icon(right, Theme.S("ic_cog"), () => SettingsPanel(inRun), Theme.IconBtn, "settings");
            if (!keep("settings")) Stage.Hot["settings"] = cog;
            LayoutRebuilder.ForceRebuildLayoutImmediate(right);
            HeadRightW = LayoutUtility.GetPreferredWidth(right);
            right.sizeDelta = new Vector2(HeadRightW, 64);

            float headW = Stage.Size.x - Theme.Gutter * 2 - HeadRightW - 16;
            var head = Ui.Rect("head", root).At(0, 1, Theme.Gutter, -18, headW, 64);
            Ui.Row(head, 14, TextAnchor.MiddleLeft, null, false, false);
            var bk = Btn.Icon(head, Theme.S("ic_back"), () => back(), Theme.IconBtn, "back");
            Stage.Hot["back"] = bk;
            if (onBack) Stage.OnBack = () => back();
            var t = Ui.Title(head, title, Theme.Fs2xl - 4, Theme.Ink); t.Outline(0.2f); t.textWrappingMode = TextWrappingModes.NoWrap; t.Pref(t.preferredWidth + (info ? 0 : 12), 60);
            t.name = "title";
            if (info) { var ii = Ui.Img(head, Theme.S("ic_info"), Theme.Dim, "info"); ii.preserveAspect = true; ii.Pref(20, 20); }
            chips?.Invoke(head);
            if (!string.IsNullOrEmpty(hint))
            {
                var h = Ui.Text(head, hint, Theme.FsSm, Theme.Sub);
                h.name = "hint";
                h.textWrappingMode = TextWrappingModes.NoWrap; h.Pref(h.preferredWidth + 8, 40); h.Outline(0.25f);
                // 머리 칸보다 길면 풀이만 줄인다(끝은 「…」) — 오른쪽 묶음 · 도구 줄을 넘지 않게
                LayoutRebuilder.ForceRebuildLayoutImmediate(head);
                float over = LayoutUtility.GetPreferredWidth(head) - headW;
                if (over > 0) { h.Pref(Mathf.Max(80, h.preferredWidth + 8 - over), 40); h.overflowMode = TextOverflowModes.Ellipsis; }
            }
            return head;
        }

        /// <summary>머리 칩의 빛깔 — 정보(남색 · 부글) · 금(시즌 · 다 참) · 경고(규칙 밖) · 하늘(읽기 전용) · 시험(덱 만들기).</summary>
        enum ChipKind { Info, Gold, Warn, Sky, Test }

        /// <summary>머리 칩 — 높이 40 · 글 Sm · 빛깔은 ChipKind 하나로. 머리(ScreenHead 의 chips) 안에서 부른다.</summary>
        (Image bg, TextMeshProUGUI text) HeadChip(RectTransform head, string text, ChipKind kind = ChipKind.Info, string icon = null)
        {
            Color bg, ink, ic;
            switch (kind)
            {
                case ChipKind.Gold: bg = Theme.Gold; ink = Theme.Brown; ic = Theme.Brown; break;
                case ChipKind.Warn: bg = Theme.Bad.A(0.85f); ink = Color.white; ic = Color.white; break;
                case ChipKind.Sky: bg = Theme.Sky.A(0.22f); ink = Theme.Sky; ic = Theme.Sky; break;
                case ChipKind.Test: bg = Theme.Hex("FF9A7A"); ink = Theme.Brown; ic = Theme.Brown; break;
                default: bg = Theme.NavyWell.A(0.9f); ink = Theme.Sub; ic = Theme.Gold; break;
            }
            var (b, t) = Ui.Chip(head, icon != null ? Theme.S(icon) : null, text, HeadChipH, bg, ic, Theme.FsSm);
            t.color = ink;
            return (b, t);
        }

        /// <summary>판 — 같은 테두리 · 짙기.</summary>
        RectTransform FPanel(Transform parent, string name)
        {
            var img = Ui.Panel(parent, Theme.Panel, null, name); img.color = img.color.A(FPanelAlpha);
            return img.rectTransform;
        }
        RectTransform EntryPanel(RectTransform parent, string name) => FPanel(parent, name);

        /// <summary>판 머리 글(금빛 · 같은 크기) — 판 위 왼쪽.</summary>
        TextMeshProUGUI FLabel(RectTransform panel, string text, float y = 16, string icon = null)
        {
            float x = FPad;
            if (icon != null) { var ic = Ui.Img(panel, Theme.S(icon), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 1, FPad, -(y + 3), 24, 24); x += 32; }
            var t = Ui.Title(panel, text, Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Band(1, 30, x, FPad, -y); t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>
        /// 빈 상태 — 가운데 큰 글(Lg · 부글) + 얻는 법 한 줄(Sm · 흐린 글). 「○○이 없다」 + 「△△하면 □□할 수 있다」(Docs/화면_글_규칙.md §4).
        /// h > 0 이면 세로 줄(레이아웃) 안의 한 칸으로(높이 h), 아니면 parent 를 채운다.
        /// </summary>
        RectTransform EmptyNote(RectTransform parent, string text, string how = null, float h = -1)
        {
            var box = Ui.Rect("empty", parent);
            if (h > 0) box.Pref(-1, h); else box.Fill();
            var t = Ui.Title(box, text, Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center);
            t.name = "empty.text";
            if (string.IsNullOrEmpty(how)) t.rectTransform.Fill(16, 0, 16, 0);
            else
            {
                t.rectTransform.anchorMin = new Vector2(0, 0.5f); t.rectTransform.anchorMax = new Vector2(1, 1);
                t.rectTransform.offsetMin = new Vector2(16, 2); t.rectTransform.offsetMax = new Vector2(-16, 0);
                t.alignment = TextAlignmentOptions.Bottom;
                var s = Ui.Text(box, how, Theme.FsSm, Theme.Dim, TextAlignmentOptions.Top);
                s.name = "empty.how";
                s.rectTransform.anchorMin = new Vector2(0, 0); s.rectTransform.anchorMax = new Vector2(1, 0.5f);
                s.rectTransform.offsetMin = new Vector2(16, 0); s.rectTransform.offsetMax = new Vector2(-16, -6);
            }
            return box;
        }

        /// <summary>잠김 표시 — 흐린 자물쇠(크기 size) 하나. 자리는 rt 의 (ax, ay) 구석에서 (x, y). 까닭 글은 「○○하면 열린다」 로 따로 쓴다.</summary>
        Image LockBadge(RectTransform rt, float size = 28, float ax = 1, float ay = 0, float x = -30, float y = 28)
        {
            var lk = Ui.Img(rt, Theme.S("ic_lock"), Theme.Dim, "lock"); lk.preserveAspect = true; lk.raycastTarget = false;
            lk.rectTransform.At(ax, ay, x, y, size, size);
            return lk;
        }
    }
}
