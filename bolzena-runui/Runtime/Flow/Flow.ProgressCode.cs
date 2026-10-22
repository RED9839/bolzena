using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 진행 코드 창(설정 → 「진행 코드」) — 편성 프리셋 · 최근 편성 · 완주 기록을 다른 기기로 옮긴다(코어 ProgressCode · PartyStore.ExportCode).
    //   (교주 보드 화면 · 크레파스는 2026-10-21 사용자 결정으로 없앴다 — 옛 코드의 보드 몫은 읽을 때 버린다.)
    public partial class Flow
    {
        /// <summary>진행 코드 창의 칸(데모 · 시험이 읽는다).</summary>
        public TMP_InputField CodeShowField { get; private set; }
        public TMP_InputField CodeInField { get; private set; }

        /// <summary>코드 입력 칸 — 남색 판 · 여러 줄 · 웹은 HTML 입력이 겹쳐 붙여넣기 · 길게 눌러 복사가 된다(Kit/WebInput.cs).</summary>
        TMP_InputField CodeField(RectTransform parent, string placeholder, string text, float h)
        {
            var bg = Ui.Img(parent, Theme.S("round", 20), Theme.NavyWell, "codefield", true);
            bg.gameObject.SetActive(false);
            bg.rectTransform.Fill();
            var area = Ui.Rect("text area", bg.transform).Fill(16, 8, 16, 8);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Ui.Text(area, placeholder, Theme.FsSm, Theme.Dim, TextAlignmentOptions.TopLeft, false, "placeholder"); ph.rectTransform.Fill();
            var tx = Ui.Text(area, "", Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft, false, "text"); tx.rectTransform.Fill();
            tx.textWrappingMode = TextWrappingModes.Normal; tx.overflowMode = TextOverflowModes.Overflow; tx.richText = false;
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area; input.textComponent = tx; input.placeholder = ph;
            input.fontAsset = Theme.Body; input.pointSize = tx.fontSize;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.richText = false; input.characterLimit = 4000;
            input.customCaretColor = true; input.caretColor = Theme.Gold; input.caretWidth = 2;
            input.selectionColor = Theme.Gold.A(0.35f);
            input.shouldHideMobileInput = false;
            input.targetGraphic = bg;
            input.text = text ?? "";
            bg.gameObject.SetActive(true);
            WebInput.Attach(input);
            return input;
        }

        /// <summary>
        /// 진행 코드 창(설정 → 「진행 코드」) — 위: 내 코드(보이고 · 고를 수 있고) · 「복사」. 아래: 코드 붙여 넣는 칸 · 「클립보드에서」 · 「불러오기」.
        /// 웹은 브라우저 클립보드(WebClipboard — 실패하면 다음 터치 때 한 번 더, 그래도 안 되면 칸을 길게 눌러 직접 복사). 편성 기억이 비어도 올바른 코드가 나온다.
        /// </summary>
        public void ProgressCodePanel()
        {
            var size = Stage.Size;
            float w = Mathf.Min(1000, size.x - 48), h = Mathf.Min(620, size.y - 24);
            var (body, close, foot) = Stage.ModalBox("progresscode", w, h, "진행 코드", "편성 프리셋 · 완주 기록을 다른 기기로 옮긴다 — 소리 · 화면 설정은 뺀다", true, null, 0);
            string code = PartyStore.ExportCode();
            var inner = Ui.Rect("inner", body).Fill(24, 48, 24, 8);
            RectTransform Half(string n, float y0, float y1) { var r = Ui.Rect(n, inner); r.anchorMin = new Vector2(0, y0); r.anchorMax = new Vector2(1, y1); r.offsetMin = r.offsetMax = Vector2.zero; return r; }
            var top = Half("mine", 0.52f, 1); var bot = Half("load", 0, 0.48f);
            // 내 코드
            var t1 = Ui.Title(top, "내 진행 코드", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); t1.rectTransform.Band(1, 30, 0, 0, 0);
            var showBox = Ui.Rect("show", top).Fill(0, 0, 200, 34);
            CodeShowField = CodeField(showBox, "", code, 0);
            var copy = Btn.Make(top, "복사", BtnStyle.PillGold, () =>
            {
                var now = PartyStore.ExportCode();
                if (CodeShowField) CodeShowField.text = now;
                WebClipboard.Copy(now, r =>
                {
                    if (r == "copied") Toast.Show("진행 코드를 복사했다");
                    else if (r == "retry") Toast.Show("화면을 한 번 더 누르면 복사한다", 3f);
                    else Toast.Show("복사하지 못했다 — 위 칸을 길게 누르거나 Ctrl+A · Ctrl+C 로 복사할 수 있다", 4f);
                });
            }, Theme.FsLg, "copy");
            copy.GetComponent<RectTransform>().At(1, 0.5f, 0, -17, 180, 60);
            Stage.Hot["code.copy"] = copy;
            // 불러오기
            var t2 = Ui.Title(bot, "코드 불러오기", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); t2.rectTransform.Band(1, 30, 0, 0, 0);
            var inBox = Ui.Rect("in", bot).Fill(0, 0, 200, 34);
            CodeInField = CodeField(inBox, "BZP1- 로 시작하는 코드를 붙여 넣기", "", 0);
            var paste = Btn.Make(bot, "클립보드에서", BtnStyle.PillDark, () => WebClipboard.Paste(t =>
            {
                if (string.IsNullOrWhiteSpace(t)) { Toast.Show("클립보드를 읽지 못했다 — 칸을 길게 눌러 붙여 넣을 수 있다", 3.5f); return; }
                if (CodeInField) CodeInField.text = t.Trim();
            }), Theme.FsMd, "paste");
            paste.GetComponent<RectTransform>().At(1, 1, 0, -34, 180, 52);
            Stage.Hot["code.paste"] = paste;
            var load = Btn.Make(bot, "불러오기", BtnStyle.PillGold, () =>
            {
                var c = CodeInField ? CodeInField.text.Trim() : "";
                if (c.Length == 0) { Toast.Show("코드를 먼저 붙여 넣어야 한다"); return; }
                var chk = ProgressCode.Import(c);
                if (chk.why != null) { Toast.Show("받지 않았다 — " + chk.why, 3.5f); return; }
                Confirm("지금 진행을 덮어쓸까?", $"편성 프리셋 {Enumerable.Range(1, 5).Count(i => chk.extra.ContainsKey("preset" + i))}칸 · 완주 기록까지 — 지금 기억은 사라진다", "덮어쓰기", () =>
                {
                    var why = PartyStore.ImportCode(c);
                    Toast.Show(why ?? "진행을 불러왔다");
                    if (why == null && CodeShowField) CodeShowField.text = PartyStore.ExportCode();
                }, true);
            }, Theme.FsLg, "load");
            load.GetComponent<RectTransform>().At(1, 0, 0, 0, 180, 60);
            Stage.Hot["code.load"] = load;
            var hint = Ui.Text(body, "코드에는 편성 프리셋 · 최근 편성 · 완주 기록과 체크섬이 들어 있다 — 틀린 코드는 받지 않는다", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
            hint.rectTransform.Band(0, 30, 24, 24, 10);
            hint.textWrappingMode = TextWrappingModes.NoWrap; hint.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
