using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 업데이트 내용 — 로비 오른쪽 위 작은 알약(지금 버전)과, 그것을 누르면 / 새 버전으로 처음 로비에 오면 한 번 뜨는 창.
    //   글은 Resources/RunUI/patchnotes.txt(PatchNotes.cs). 창은 지금 판이 맨 위, 지난 판은 아래로 스크롤.
    //   처음 실행(플레이 기록 안내도 아직 안 본 사람)은 소식이 낯서니 띄우지 않고 「봤음」 만 남긴다. 시험 실행(데모 · 음소거 점검)은 띄우지 않는다.
    public partial class Flow
    {
        /// <summary>로비 오른쪽 위 — 버전 알약. 누르면 업데이트 내용 창. 새 버전이면 조금 뒤 창을 한 번 띄운다.</summary>
        void LobbyVersion(RectTransform root)
        {
            float w = Theme.C(290, 250), h = Theme.C(46, 40);
            var b = Btn.Make(root, null, BtnStyle.PillDark, PatchNotesPanel, 0, "version");
            b.GetComponent<RectTransform>().At(1, 1, -Theme.Gutter - 8, -Theme.C(22, 14), w, h);
            var t = Ui.Title(b.transform, $"업데이트 내용  <size=78%><color={Theme.DimTag}>{PatchNotes.Version}</color></size>", Theme.C(Theme.FsMd, Theme.FsSm), Theme.Ink, TextAlignmentOptions.Center);
            t.rectTransform.Fill(16, 2, 16, 2);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true; t.fontSizeMin = Theme.FsCap; t.fontSizeMax = t.fontSize;
            Stage.Hot["lobby.version"] = b;
            Tw.Pop((RectTransform)b.transform, 0.5f, 0.85f, 0.4f);

            if (!PatchNotes.Unseen || Demo.Active || TestMute.On) return;
            if (PatchNotes.FirstRun) { PatchNotes.MarkSeen(); return; }   // 처음 온 사람 — 지난 소식 없이 바로 놀게
            Tw.After(0.9f, () =>
            {
                // 그새 다른 화면으로 넘어갔으면(로비 단추가 사라짐) 다음 로비에서
                if (!Stage.Hot.TryGetValue("lobby.version", out var vb) || vb == null || vb != b) return;
                PatchNotesPanel();
            });
        }

        /// <summary>업데이트 내용 창 — 판마다 제목 · 버전 · 소개, 분류(새 기능 · 밸런스 · 편의 · 고친 것)별 항목.</summary>
        public void PatchNotesPanel()
        {
            PatchNotes.MarkSeen();
            var size = Stage.Size;
            float w = Mathf.Min(Theme.C(1040, 1100), size.x - 48);
            float h = Mathf.Min(Theme.C(800, 700), size.y - 24);
            float footH = Theme.C(84, 72);
            var (body, close, bar) = Stage.ModalBox("patchnotes", w, h, "업데이트 내용", $"지금 버전 {PatchNotes.Version} · 지난 소식은 아래로", true, null, footH);

            var content = Ui.Scroll(body, out _);
            Ui.Col(content, 4, TextAnchor.UpperLeft, new RectOffset(12, 18, 4, 16), true, false);
            var notes = PatchNotes.All;
            if (notes.Count == 0)
                Ui.Text(content, "아직 적힌 소식이 없습니다", Theme.FsBody, Theme.Dim);
            for (int i = 0; i < notes.Count; i++)
            {
                var e = notes[i];
                if (i > 0)
                {
                    var gap = Ui.Rect("gap", content); gap.Pref(-1, 26);
                    var rule = Ui.Img(gap, Theme.White, Theme.Line, "rule"); rule.rectTransform.Band(0.5f, 1);
                }
                string meta = e.Version + (e.Date.Length > 0 && e.Date.Replace('-', '.') != e.Version ? " · " + e.Date : "");   // 날짜가 버전과 같으면 한 번만
                var head = Ui.Title(content, $"{e.Title}  <size=62%><color={Theme.DimTag}>{meta}</color></size>", Theme.FsXl, i == 0 ? Theme.Gold : Theme.Ink, TextAlignmentOptions.TopLeft, "head");
                head.overflowMode = TextOverflowModes.Overflow;
                foreach (var line in e.Intro)
                {
                    var intro = Ui.Text(content, line, Theme.FsBody, Theme.Sub, TextAlignmentOptions.TopLeft, false, "intro");
                    intro.overflowMode = TextOverflowModes.Overflow;
                }
                foreach (var s in e.Sections)
                {
                    if (s.Name.Length > 0)
                    {
                        var sp = Ui.Rect("sp", content); sp.Pref(-1, 8);
                        var sec = Ui.Title(content, $"<color={NoteTag(s.Name)}>●</color> {s.Name}", Theme.FsLg, Theme.Ink, TextAlignmentOptions.TopLeft, "section");
                        sec.overflowMode = TextOverflowModes.Overflow;
                    }
                    foreach (var it in s.Items)
                    {
                        if (it.StartsWith("###"))   // 작은 머리(사도 이름 등)
                        {
                            var sp2 = Ui.Rect("sp", content); sp2.Pref(-1, 4);
                            var sub = Ui.Title(content, $"<indent=0.5em>{it.Substring(3)}</indent>", Theme.FsMd, i == 0 ? Theme.Gold : Theme.Ink, TextAlignmentOptions.TopLeft, "sub");
                            sub.overflowMode = TextOverflowModes.Overflow;
                            continue;
                        }
                        bool deep = it.StartsWith("--");   // 딸린 항목 — 한 칸 더 들이고 흐린 글
                        var item = deep
                            ? Ui.Text(content, $"<indent=1.5em>-</indent><indent=2.5em>{it.Substring(2)}</indent>", Theme.FsBody, Theme.Sub, TextAlignmentOptions.TopLeft, false, "item")
                            : Ui.Text(content, $"<indent=0.5em>·</indent><indent=1.5em>{it}</indent>", Theme.FsBody, Theme.Ink, TextAlignmentOptions.TopLeft, false, "item");
                        item.overflowMode = TextOverflowModes.Overflow;
                        item.lineSpacing = 2;
                    }
                }
            }

            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, new RectOffset(10, 10, 14, 14), false, true);
            var hint = Ui.Text(bar, "로비 오른쪽 위 단추로 다시 볼 수 있습니다", Theme.FsSm, Theme.Dim, TextAlignmentOptions.MidlineLeft);
            hint.textWrappingMode = TextWrappingModes.NoWrap; hint.overflowMode = TextOverflowModes.Ellipsis;
            hint.Pref(0, -1, 1);
            var ok = Btn.Make(bar, "확인", BtnStyle.PillGold, close, Theme.FsMd);
            ok.Pref(Theme.C(220, 180));
            Stage.Hot["patch.close"] = ok;
        }

        // 분류 점 색 — 새 기능 금 · 밸런스 분홍 · 편의 초록 · 고친 것 하늘, 그 밖은 회색
        static string NoteTag(string section) => "#" + ColorUtility.ToHtmlStringRGB(section switch
        {
            "새 기능" => Theme.Gold,
            "밸런스" => Theme.Bad,
            "편의" => Theme.Good,
            "고친 것" => Theme.Sky,
            "UI 통일 · 개선" => Theme.Good,   // (2026-10-13) 편의와 같은 초록
            "연출" => Theme.Gold,
            _ => Theme.Sub,
        });
    }
}
