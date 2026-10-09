using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 골디의 상점 — 웹판 shopscreen2 의 배치: 상인(골디)이 왼쪽에 서서 말하고, 오른쪽 판에 「교주 카드」 · 「장비」 진열이 칸으로 나뉜다.
    // 진열 한 칸 = 그림(카드 · 장비) + 이름 · 설명 + 값(금빛 알약). 아래 띠에 카드 제거 · 새로고침(서비스), 오른쪽 아래 떠나기.
    // 산 장비는 곧장 끼기(팔 수 없다). 카드 제거는 한 번 들를 때 한 장 — 덱 고르기 창.
    public partial class Flow
    {
        static readonly string[] GoldyLines =
        {
            "어서 오세요, 고객님! 오늘 들어온 물건은 전부 정품이에요.", "눈이 높으시네요~ 그건 제가 아끼던 건데!",
            "골드는 정확하게! 외상은 안 돼요.", "또 와 주실 거죠? 단골 할인은… 생각해 볼게요.",
        };

        void ShopScreen(string kind)
        {
            Stage.SetBg(P.Floor.Bg != null && P.Floor.Bg.TryGetValue("fight", out var bg) ? bg : "stage3_2", 0.85f);
            Stage.Show("shop", root => BuildShop(root, kind, 0), 1.2f);
        }

        void BuildShop(RectTransform root, string kind, int lineIdx)
        {
            Ui.Clear(root);
            var shop = P.S.Shop;
            W.StatusBar(root, this, true, false, false, "골디의 상점", "황금에서 태어난 용족 상인 · 정품만 팝니다");
            var size = Stage.Size;
            float L = Theme.C(430, 380);          // 왼쪽 상인 자리
            float top = 104, foot = Theme.C(100, 92);

            // 왼쪽 — 골디가 서서 말한다(누르면 다른 말)
            var floor = Ui.Img(root, Theme.S("soft"), new Color(0, 0, 0, 0.5f), "floor");
            floor.rectTransform.At(0, 0, L * 0.5f - 200, 8, 400, 80);
            var stand = Ui.Rect("goldy", root).At(0, 0, L * 0.5f, Theme.C(-10, -20), 10, 10);
            var g = SpineUi.Make(stand, "st_goldy", null, Theme.C(600, 470), "Idle_1", "Idle");
            if (g != null) Tw.FadeIn(g, 0.5f, 0.05f);
            var bubbleT = W.Bubble(root, "골디", GoldyLines[lineIdx % GoldyLines.Length], L - 40, out var bubble);
            bubble.At(0, 1, Theme.Gutter, -top - 6, L - 40, 100);
            Tw.Pop(bubble, 0.25f, 0.8f, 0.4f);
            var tap = Ui.Img(root, Theme.White, new Color(0, 0, 0, 0), "tap", true);
            tap.rectTransform.At(0, 0, 40, 0, L - 80, Theme.C(540, 400));
            var tb = tap.gameObject.AddComponent<Btn>(); tb.Bg = null;
            int li = lineIdx;
            tb.OnClick = () =>
            {
                li++;
                bubbleT.text = GoldyLines[li % GoldyLines.Length];
                Tw.Pop(bubble, 0, 0.85f, 0.3f);
                if (g != null) { SpineUi.Play(g, false, "Happy_1", "Smile_1", "Happy"); g.AnimationState.AddAnimation(0, SpineUi.PickAnim(g.Skeleton.Data, "Idle_1", "Idle"), true, 0); }
            };

            void Bought(string msg)
            {
                Sfx.Play("coin");
                Toast.Show(msg);
                P.Save("shop", kind);
                Settle(() => BuildShop(root, kind, li + 1));
            }

            // 오른쪽 — 진열 판(교주 카드 · 장비 두 칸)
            var panel = Ui.Panel(root, Theme.Panel, new Color(1, 1, 1, 0.96f), "shelf").rectTransform;
            panel.anchorMin = new Vector2(0, 0); panel.anchorMax = new Vector2(1, 1);
            panel.offsetMin = new Vector2(L + 8, foot); panel.offsetMax = new Vector2(-Theme.Gutter, -top);
            Tw.Rise(panel, 0.1f, 24, 0.45f, Vector2.right);
            float panelW = size.x - L - 8 - Theme.Gutter, panelH = size.y - top - foot;
            var cards = Enumerable.Range(0, shop.Items.Count).Where(i => shop.Items[i].Kind == "neutral").ToList();
            var gears = Enumerable.Range(0, shop.Items.Count).Where(i => shop.Items[i].Kind != "neutral").ToList();
            int sections = (cards.Count > 0 ? 1 : 0) + (gears.Count > 0 ? 1 : 0);
            const float pad = 18, secH = 34, secGap = 14;
            float rowH = sections == 0 ? 0 : (panelH - pad * 2 - sections * (secH + 8) - (sections - 1) * secGap) / sections;
            float y = -pad;
            int n = 0;
            void Shelf(string title, string sub, System.Collections.Generic.List<int> idxs)
            {
                if (idxs.Count == 0) return;
                var sec = W.Section(panel, title, sub, secH);
                var srt = (RectTransform)sec.transform.parent;
                srt.At(0, 1, pad, y, panelW - pad * 2, secH);
                y -= secH + 8;
                int cols = Mathf.Max(3, idxs.Count);
                float tw = (panelW - pad * 2 - (cols - 1) * Theme.Gap) / cols;
                for (int c = 0; c < idxs.Count; c++)
                {
                    int idx = idxs[c];
                    var it = shop.Items[idx];
                    var tile = OfferTile(panel, it, tw, rowH);
                    tile.At(0, 1, pad + c * (tw + Theme.Gap), y, tw, rowH);
                    var pb = PriceBtn(tile, it, () =>
                    {
                        var cs = CardSnap();
                        var why = P.Buy(idx);
                        if (why != null) { Toast.Show(why); return; }
                        // 교주 카드는 가운데에 크게 → 누구 덱에 넣을지 고른 뒤(GainCards → PendingNeutral → AssignNeutral)
                        GainCards(NewCards(cs), () => Bought(it.Kind == "neutral" ? $"「{P.Data.Card(it.Id)?.Name}」 — 덱에 넣었습니다" : $"「{P.Data.Equip(it.Id)?.Name}」 — 샀습니다"));
                    });
                    pb.GetComponent<RectTransform>().At(1, 0, -12, 12, Theme.C(128, 120), Theme.C(42, 44));
                    Stage.Hot["shop.item" + idx] = pb;
                    Tw.Pop(tile, 0.2f + n++ * 0.05f, 0.9f, 0.35f);
                }
                y -= rowH + secGap;
            }
            Shelf("교주 카드", "교주님이 직접 쓰는 카드 · 사면 덱에 들어갑니다", cards);
            Shelf("아티팩트", "사면 곧장 사도에게 낍니다 · 산 아티팩트는 팔 수 없습니다", gears);
            if (sections == 0) { var none = Ui.Text(panel, "오늘은 다 팔렸습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center); none.rectTransform.Fill(); }

            // 아래 띠 — 서비스(카드 제거 · 새로고침) · 떠나기
            var bar = Ui.Rect("services", root);
            bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(0.5f, 0);
            bar.offsetMin = new Vector2(L + 8, 18); bar.offsetMax = new Vector2(-Theme.Gutter, 18 + Theme.C(66, 64));
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleLeft, null, false, true);
            var rm = ServiceBtn(bar, Theme.S("ic_trash"), "카드 제거", shop.RemoveUsed ? "이번에는 뺐습니다" : "덱에서 한 장 · 쓸수록 오릅니다", P.RemovePrice, !shop.RemoveUsed, () => RemovePicker(root, kind, li), P.Run.RemoveBase);
            rm.Pref(Theme.C(380, 380), -1);
            Stage.Hot["shop.remove"] = rm;
            var rr = ServiceBtn(bar, Theme.S("ic_refresh"), "새로고침", "진열을 통째로 바꿉니다", P.RerollPrice, true, () =>
            {
                var why = P.Reroll();
                if (why != null) { Toast.Show(why); return; }
                Sfx.Play("coin");
                P.Save("shop", kind);
                BuildShop(root, kind, li + 1);
            }, P.Run.RerollBase);
            rr.Pref(Theme.C(340, 340), -1);
            Stage.Hot["shop.reroll"] = rr;
            if (P.Run.ShopOff > 0)   // 교주 보드 「골디 할인권」 — 값에 취소선 + 깎은 값, 여기엔 할인율 하나
            {
                var off = Ui.Text(bar, $"골디 할인권 -{P.Run.ShopOff * 100:0.#}%", Theme.FsSm, Theme.Gold, TextAlignmentOptions.MidlineLeft);
                off.textWrappingMode = TextWrappingModes.NoWrap; off.Pref(Theme.C(190, 170), -1);
            }
            W.Spacer(bar, -1, -1, 1);
            var back = Btn.Make(bar, "휴식으로", BtnStyle.PillDark, () => Camp(kind), Theme.FsLg);
            back.Pref(220, -1);
            Stage.Hot["shop.back"] = back;
            Tw.Rise(bar, 0.35f, 16, 0.4f);
        }

        /// <summary>진열 한 칸 — 왼쪽에 그림(카드면 카드 한 장, 장비면 등급 빛 우물 + 칸 아이콘), 오른쪽에 이름 · 종류 · 설명. 값은 오른쪽 아래(PriceBtn).</summary>
        RectTransform OfferTile(RectTransform parent, ShopItem it, float w, float h)
        {
            var bg = Ui.Img(parent, Theme.Cell, Color.white, "offer", true);
            var rt = bg.rectTransform;
            float vis;
            string name, sub, body;
            if (it.Kind == "neutral")
            {
                float cw = Mathf.Min(Theme.C(150, 112), (h - 24) / 1.4f);
                var card = W.Card(rt, this, it.Id, cw);
                card.At(0, 0.5f, 12, 0, cw, cw * 1.4f);
                var cid = it.Id; card.gameObject.AddComponent<Btn>().OnClick = () => CardZoom(cid);   // 카드 그림을 누르면 카드 상세(사기는 값 단추)
                vis = cw + 24;
                var v = P.View(it.Id);
                name = v?.Name ?? it.Id;
                sub = $"{v?.Def?.Grade ?? "교주"} · {v?.Type ?? "스킬"} · 비용 {(v == null ? "?" : v.X ? "X" : v.Cost.ToString())}";
                body = v != null ? P.Text.Card(v) : "";
            }
            else
            {
                var e = P.Data.Equip(it.Id);
                var gc = Theme.GradeOf(e?.Grade);
                float ws = Mathf.Min(Theme.C(96, 80), h - 70);
                var well = Ui.Img(rt, Theme.Round, Theme.NavyWell, "well");
                well.rectTransform.At(0, 1, 14, -14, ws, ws);
                Ui.Img(well.transform, Theme.S("soft"), gc.A(0.4f), "glow").rectTransform.Fill(-10, -10, -10, -10);
                var spic = CardArt.Equip(e?.Id);   // 원작 아이콘(없으면 칸 무늬)
                float sp = spic != null ? ws * 0.06f : ws * 0.2f;
                var ic = Ui.Img(well.transform, spic ?? W.SlotIcon(e?.Slot), spic != null ? Color.white : gc, "icon"); ic.rectTransform.Fill(sp, sp, sp, sp); ic.preserveAspect = true;
                Ui.Img(rt, Theme.Round, gc, "grade").rectTransform.Band(1, 3, 14, 14, -1);
                vis = ws + 28;
                name = e?.Name ?? it.Id;
                sub = $"{e?.Slot} · <color=#{ColorUtility.ToHtmlStringRGB(gc)}>{e?.Grade}</color>" + (e?.Affinity != null ? $" · 애착 {Roster.OfCore(e.Affinity).ko}" : "");
                string eff = e != null && e.Effect.Count > 0 ? P.Text.Passives(e.Effect) : (e?.Blurb ?? "");
                body = $"<color={Theme.GoodTag}>{W.StatLine(e?.Stats)}</color>\n{eff}";
            }
            var nm = Ui.Title(rt, name, Theme.FsMd, Theme.Ink, TextAlignmentOptions.TopLeft);
            nm.rectTransform.Band(1, 28, vis, 12, -12);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 14; nm.fontSizeMax = Theme.FsMd;
            var st = Ui.Text(rt, sub, Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft);
            st.rectTransform.Band(1, 22, vis, 12, -40);
            st.textWrappingMode = TextWrappingModes.NoWrap;
            var bd = Ui.Text(rt, body, Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft);
            bd.rectTransform.Fill(vis, Theme.C(62, 64), 12, 68);
            bd.enableAutoSizing = true; bd.fontSizeMin = 11; bd.fontSizeMax = Theme.FsSm; bd.lineSpacing = 0;
            // 카드 글 속 낱말(키워드 · 상태 · 생성 카드) — 카드 그림이 작은 폰에서도 옆 글로 누를 수 있게
            if (it.Kind == "neutral" && P.View(it.Id) is Core.CardView tv)
            {
                var terms = CardTerms.Of(P.Data, P.Text, tv, body);
                if (terms.Count > 0) TermPop.MarkAndAttach(bd, body + CardTerms.Extra(body, terms), terms, Stage.ToastLayer, (p, id, w) => W.Card(p, this, id, w, "termcard"));
            }
            if (it.Sold)
            {
                var sold = Ui.Img(rt, Theme.Round, new Color(0.02f, 0.03f, 0.07f, 0.66f), "sold"); sold.rectTransform.Fill(2, 2, 2, 2);
                var stx = Ui.Title(sold.transform, "팔렸습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center); stx.rectTransform.Fill();
            }
            return rt;
        }

        Btn PriceBtn(RectTransform parent, ShopItem it, System.Action buy)
        {
            bool can = !it.Sold && P.S.Gold >= it.Price;
            var b = Btn.Make(parent, null, it.Sold ? BtnStyle.PillDark : BtnStyle.PillGold, buy, 0, "price");
            var row = Ui.Row(b.GetComponent<RectTransform>(), 6, TextAnchor.MiddleCenter, new RectOffset(8, 8, 4, 4), false, false);
            if (it.Sold) { var t = Ui.Title(b.transform, "팔렸습니다", Theme.FsSm, Theme.Sub); t.textWrappingMode = TextWrappingModes.NoWrap; }
            else
            {
                var ic = Ui.Img(b.transform, Theme.Icon("gold"), Color.white, "coin"); ic.Pref(24, 24); ic.preserveAspect = true;
                var t = Ui.Title(b.transform, it.Delivery ? "무료" : it.Base > it.Price ? $"<size=70%><s>{it.Base}</s></size> {it.Price}" : it.Price.ToString(), Theme.FsMd, can ? Theme.Brown : Theme.Hex("7A2A1A"));
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            }
            b.Interactable = can;
            b.Why = it.Sold ? "이미 팔린 물건입니다" : "골드가 모자랍니다";
            return b;
        }

        /// <summary>서비스 띠 한 칸(카드 제거 · 새로고침) — 아이콘 · 이름 · 설명, 오른쪽에 값 알약.</summary>
        Btn ServiceBtn(RectTransform parent, Sprite icon, string title, string sub, int price, bool can, System.Action go, int basePrice = 0)
        {
            var b = Btn.Make(parent, null, BtnStyle.Glass, go, 0, title);
            var ic = Ui.Img(b.transform, icon, Theme.Gold, "ic"); ic.rectTransform.At(0, 0.5f, 18, 0, 30, 30); ic.preserveAspect = true;
            var t = Ui.Title(b.transform, title, Theme.FsMd, Theme.Ink, TextAlignmentOptions.BottomLeft); t.rectTransform.Fill(62, 30, 112, 6);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var s = Ui.Text(b.transform, sub, Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft); s.rectTransform.Fill(62, 6, 112, 36);
            s.textWrappingMode = TextWrappingModes.NoWrap;
            bool off = basePrice > price;   // 골디 할인권 — 원래 값 취소선
            var (pb, pt) = Ui.Chip(b.transform, Theme.Icon("gold"), off ? $"<size=70%><s>{basePrice}</s></size> {price}" : price.ToString(), 40, Theme.NavyWell.A(0.95f), null, Theme.FsMd);
            pb.rectTransform.At(1, 0.5f, -12, 0, off ? 128 : 92, 40);
            pt.color = P.S.Gold >= price ? Theme.Gold : Theme.Bad;
            b.Interactable = can && P.S.Gold >= price;
            b.Why = !can ? "이번에는 더 할 수 없습니다" : "골드가 모자랍니다";
            return b;
        }

        void RemovePicker(RectTransform root, string kind, int li)
        {
            float mw = Theme.Compact ? 1240 : Mathf.Min(Stage.Size.x - 40, Mathf.Max(1240, 8 * (170 + 12) + 80));   // 넓은 화면은 한 줄 6~8칸
            var (area, close, _) = Stage.ModalBox("remove", mw, 780, "뺄 카드를 고르세요", $"{P.RemovePrice} 골드 · 한 번 들를 때 한 장 · 누르면 한 번 더 묻습니다");
            var content = Ui.Scroll(area, out _);
            // 사도별 묶음(기본 → 고유) · 상태 · 저주 · 교주 카드 순 — CardOrder
            CardGroups(content, P.S.Deck, 170, Theme.Compact ? 6 : GridCols(mw - 80, 170, 12, 6, 8), (c, id, i) =>   // 같은 카드도 한 장마다 한 칸
            {
                var b = c.gameObject.AddComponent<Btn>();
                b.OnClick = () => Confirm($"「{P.Data.Card(id)?.Name}」 을 빼겠습니까?", $"{P.RemovePrice} 골드 — 덱에서 한 장이 빠집니다.", "빼기", () =>
                {
                    var why = P.Remove(id);
                    if (why != null) { Toast.Show(why); return; }
                    close();
                    Sfx.Play("coin");
                    Toast.Show($"「{P.Data.Card(id)?.Name}」 — 덱에서 뺐습니다");
                    P.Save("shop", kind);
                    BuildShop(root, kind, li + 1);
                }, true);
                Stage.Hot["remove" + i] = b;
            });
        }
    }
}
