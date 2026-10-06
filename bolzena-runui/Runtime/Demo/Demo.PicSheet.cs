using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 원작 그림 카드 전후 시트(-demo-picsheet, 2026-10-07 「몇몇 카드가 카드 이미지가 너무 붕 뜬 것도 있음」) — 그림 표(cardart.json)의
    //   원작 그림 카드(pics) · 스킬 아이콘 카드(cards)를 판 화면 카드(W.Card, 폭 200 — 실제 크기)로 한 쪽에 27장씩 찍는다.
    //   -oldpicfit 이면 예전 자리(한 장 굽기 · 가운데 아이콘 판). -picset pics|icons(없으면 둘 다) · -cards a,b,c 로 고른다.
    //   안내선: 하늘 = 그림 칸 위 끝(종류 알약 아래). [PicSheet] 줄 = 카드마다 장식 선 자리 · 그림 내용 자리(창 몫).
    public partial class Demo
    {
        IEnumerator PicSheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            string tag = CardArt.OldFit ? "before" : "after";
            string set = Arg("-picset");
            var ids = new List<(string id, string set)>();
            if (Arg("-cards") is string list) ids.AddRange(list.Split(',').Select(x => (x, "pick")));
            else
            {
                if (set != "icons") ids.AddRange(CardArt.PicCards().OrderBy(x => x, System.StringComparer.Ordinal).Select(x => (x, "pics")));
                if (set != "pics") ids.AddRange(CardArt.IconCards().OrderBy(x => x, System.StringComparer.Ordinal).Select(x => (x, "icons")));
            }
            var cards = ids.Where(x => { try { return f.P.View(x.id) != null; } catch { return false; } }).ToList();
            Debug.Log($"[PicSheet] board {tag} 카드 {cards.Count}장(표 {ids.Count})");
            var size = f.Stage.Size;
            float w = 200, ch = 280, lab = 24;
            int cols = Mathf.Max(1, Mathf.FloorToInt((size.x - 16) / (w + 8))), rows = Mathf.Max(1, Mathf.FloorToInt((size.y - 12) / (ch + lab + 6)));
            int per = cols * rows;
            float x0 = (size.x - cols * (w + 8)) / 2 + 4;
            var log = new System.Text.StringBuilder();
            for (int p = 0; p * per < cards.Count; p++)
            {
                var layer = Ui.Rect("modal picsheet", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
                var page = cards.Skip(p * per).Take(per).ToList();
                for (int i = 0; i < page.Count; i++)
                {
                    var (id, s) = page[i];
                    float x = x0 + (i % cols) * (w + 8), y = -6 - (i / cols) * (ch + lab + 6);
                    var card = W.Card(layer, f, id, w, "card " + id);
                    card.At(0, 1, x, y, w, ch);
                    float k = w / 200f, wh = ch - 4 * k, ww = w - 4 * k, top = 56 * k / wh;
                    var guide = Ui.Img(card, Theme.White, Theme.Sky.A(0.8f), "guide"); guide.rectTransform.At(0, 1, 2 * k, -(2 * k + wh * top), ww, 1);
                    var t = Ui.Text(layer, $"{f.P.View(id)?.Name} <size=80%><color={Theme.SubTag}>{id}</color></size>", 11, Theme.Ink, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x - 4, y - ch - 2, w + 8, lab); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 7; t.fontSizeMax = 11;
                    // 기록 — 장식 선(카드 아래에서 decoY) → 창 몫 · 그림 내용 자리는 같은 규칙으로 다시 잰다
                    var deco = card.Find("deco") as RectTransform;
                    float bottom = deco != null ? (ch - 2 * k - deco.anchoredPosition.y) / wh : 0;
                    Rect r = default; bool cap = false; string mode = "cover";
                    var pn = CardArt.PicOf(id);
                    if (CardArt.Obj(pn, out var op)) { mode = "obj"; r = CardArt.Place(op.Content.width, op.Content.height, op.Src.y, ww / wh, top, bottom, pn, out cap, CardArt.RefBoard); }
                    else if (pn == null && !CardArt.OldFit && CardArt.Icon(CardArt.IconOf(id)) is Sprite ic) { mode = "icon"; r = CardArt.Place(1, 1, ic.rect.height, ww / wh, top, bottom, CardArt.IconOf(id), out cap, CardArt.RefBoard); }
                    log.Append($"[PicSheet] board\t{tag}\t{s}\t{id}\t{mode}:{pn ?? CardArt.IconOf(id)}\t{bottom:0.000}\t{r.x:0.000}\t{r.y:0.000}\t{r.width:0.000}\t{r.height:0.000}\t{(cap ? 1 : 0)}\n");
                }
                yield return Wait(0.8f);
                yield return Shot($"picsheet_board_{tag}_{p + 1:00}");
                Destroy(layer.gameObject);
                yield return Wait(0.2f);
            }
            Debug.Log(log.ToString());
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }
    }
}
