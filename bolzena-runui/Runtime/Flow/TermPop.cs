using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 글 속 밑줄 낱말 — PC 는 올리기만 해도, 폰은 누르면 그 낱말 옆에 작은 설명 판(CardTerms).
    //   키워드 · 상태 · 사도 고유 효과 = 이름 · 한두 줄(고유 효과는 짧은 글 + 「자세히」 펼침) · 해로운 것은 붉은 테.
    //   생성 카드 = 그 카드를 작은 카드 모양으로(W.Card — 코스트 · 이름 · 종류 · 효과 글).
    //   PC 는 판 위에 올라가 있는 동안 닫지 않는다(「자세히」를 누를 수 있게). 폰은 바깥을 누르면 닫힌다.
    //   낱말이 아닌 곳을 누르면 부모(카드 단추 Btn)로 그대로 넘긴다(누름 · 뗌 · 클릭 모두).
    //
    // 전투 화면도 같은 판을 쓴다(Flow 없이):
    //   TermPop.Attach(tmp, terms, layer, cardMaker)                 — 글 하나에 붙이기(Mark 는 미리 해 둔다)
    //   TermPop.MarkAndAttach(tmp, text, terms, layer, cardMaker)   — Mark + Prepare + Attach 한 번에
    //   TermPop.ShowAt(layer, screenPos, terms, cardMaker, cam)     — 낱말 여럿을 세로로 쌓아 띄우기(툴팁 · 길게 누르기)
    //   TermPop.Box(parent, term, width)                            — 판 하나만 만들어 원하는 곳에 끼우기(카드 크게 옆 목록)
    //   TermPop.Close()
    public class TermPop : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>작은 카드 그리기(부모, 카드 id, 너비) → 카드 RectTransform. 판 화면은 W.Card, 전투는 CardView 를 넘긴다.</summary>
        public delegate RectTransform CardMaker(Transform parent, string cardId, float width);

        TextMeshProUGUI tmp;
        List<CardTerms.Term> terms;
        RectTransform layer;
        CardMaker cards;
        Canvas canvas;
        static RectTransform open;          // 띄운 판(바탕 덮개 또는 판 묶음)
        static RectTransform openBox;       // 그 안의 판(PC — 올라가 있으면 닫지 않는다)
        static TermPop owner;
        static int openIdx = -1;
        static float leaveT;
        int downIdx = -1;

        /// <summary>판 화면 — Flow 의 알림 층 · W.Card 로.</summary>
        public static TermPop Attach(Flow f, TextMeshProUGUI t, List<CardTerms.Term> terms)
            => Attach(t, terms, f != null ? f.Stage.ToastLayer : null, f != null ? (p, id, w) => W.Card(p, f, id, w, "termcard") : (CardMaker)null);

        /// <summary>글(이미 CardTerms.Mark 한 것)에 붙인다. layer = 판을 띄울 층(화면을 덮는 RectTransform), cardMaker 가 없으면 카드 낱말은 이름 판으로.</summary>
        public static TermPop Attach(TextMeshProUGUI t, List<CardTerms.Term> terms, RectTransform layer, CardMaker cardMaker = null)
        {
            if (t == null || terms == null || terms.Count == 0) return null;
            var p = t.GetComponent<TermPop>() ?? t.gameObject.AddComponent<TermPop>();
            p.tmp = t; p.terms = terms; p.layer = layer; p.cards = cardMaker;
            p.canvas = t.canvas;
            CardTerms.Prepare(t);
            t.raycastTarget = true;
            return p;
        }

        /// <summary>글에 낱말 표시를 입히고(Mark · 아이콘) 붙인다. terms 가 비면 글만 넣는다.</summary>
        public static TermPop MarkAndAttach(TextMeshProUGUI t, string text, List<CardTerms.Term> terms, RectTransform layer, CardMaker cardMaker = null)
        {
            if (t == null) return null;
            if (terms == null || terms.Count == 0) { t.text = text; return null; }
            CardTerms.Prepare(t);
            t.text = CardTerms.Mark(text, terms);
            return Attach(t, terms, layer, cardMaker);
        }

        static bool Touch => UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Mouse.current == null || Theme.Compact;

        static Camera CamOf(Canvas c) => c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
        Camera Cam => CamOf(canvas != null ? canvas : (canvas = tmp.canvas));

        int LinkAt(Vector2 screen)
        {
            int li = TMP_TextUtilities.FindIntersectingLink(tmp, screen, Cam);
            if (li < 0) return -1;
            var id = tmp.textInfo.linkInfo[li].GetLinkID();
            return id.StartsWith("t:") && int.TryParse(id.Substring(2), out var n) && n < terms.Count ? n : -1;
        }

        static Vector2 Pointer => UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;

        void Update()
        {
            if (Touch || tmp == null) return;
            // PC — 올리면 띄우고, 낱말 · 판을 벗어나면 잠깐 뒤 닫는다
            var p = Pointer;
            bool over = RectTransformUtility.RectangleContainsScreenPoint(tmp.rectTransform, p, Cam);
            int n = over ? LinkAt(p) : -1;
            if (n >= 0) { leaveT = 0; if (owner != this || openIdx != n) Show(n, p); }
            else if (owner == this && open != null && !demoHold)
            {
                bool onBox = openBox != null && RectTransformUtility.RectangleContainsScreenPoint(openBox, p, CamOf(openBox.GetComponentInParent<Canvas>()));
                if (onBox) leaveT = 0;
                else { leaveT += Time.unscaledDeltaTime; if (leaveT > 0.28f) Close(); }
            }
        }

        // 낱말 위 누름은 여기서 받고(부모 카드가 눌리지 않게), 아니면 부모로 넘긴다
        public void OnPointerDown(PointerEventData e)
        {
            downIdx = LinkAt(e.position);
            if (downIdx < 0) Forward(e, ExecuteEvents.pointerDownHandler);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (downIdx < 0) Forward(e, ExecuteEvents.pointerUpHandler);
        }

        public void OnPointerClick(PointerEventData e)
        {
            int n = downIdx >= 0 ? downIdx : LinkAt(e.position);
            downIdx = -1;
            if (n >= 0)
            {
                if (owner == this && openIdx == n) Close(); else Show(n, e.position);
                return;
            }
            Forward(e, ExecuteEvents.pointerClickHandler);
        }

        void Forward<T>(PointerEventData e, ExecuteEvents.EventFunction<T> fn) where T : IEventSystemHandler
        {
            if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, fn);
        }

        void OnDisable() { if (owner == this) Close(); }

        /// <summary>자동 데모 — 화면의 밑줄 낱말 하나(card 면 생성 카드 · hero 면 사도 고유 효과)를 그 글자 자리에서 연다. 없으면 false.</summary>
        public static bool DemoOpen(bool card, bool hero = false)
        {
            foreach (var p in FindObjectsByType<TermPop>(FindObjectsSortMode.None))
            {
                if (!p.isActiveAndEnabled || p.tmp == null) continue;
                int n = p.terms.FindIndex(t => t.IsCard == card && (!hero || t.Hero));
                if (n < 0) continue;
                p.tmp.ForceMeshUpdate();
                var li = p.tmp.textInfo.linkInfo;
                for (int i = 0; i < p.tmp.textInfo.linkCount; i++)
                {
                    if (li[i].GetLinkID() != "t:" + n) continue;
                    int ci = li[i].linkTextfirstCharacterIndex + Mathf.Max(0, li[i].linkTextLength - 1);
                    var ch = p.tmp.textInfo.characterInfo[ci];
                    var world = p.tmp.rectTransform.TransformPoint((ch.bottomLeft + ch.topRight) / 2);
                    p.Show(n, RectTransformUtility.WorldToScreenPoint(p.Cam, world));
                    demoHold = true;
                    return true;
                }
            }
            return false;
        }
        static bool demoHold;   // 데모가 연 판은 마우스가 없어도 닫지 않는다

        /// <summary>자동 데모 — 열린 판의 「자세히」를 펼친다(있으면 true).</summary>
        public static bool DemoMore()
        {
            if (openBox == null) return false;
            var b = openBox.GetComponentInChildren<Btn>();
            if (b == null) return false;
            b.OnClick?.Invoke();
            return true;
        }

        public static bool IsOpen => open != null;

        public static void Close()
        {
            if (open != null) Destroy(open.gameObject);
            open = null; openBox = null; owner = null; openIdx = -1; demoHold = false; leaveT = 0;
        }

        void Show(int n, Vector2 screen)
        {
            var lay = layer != null ? layer : tmp.canvas != null ? (RectTransform)tmp.canvas.transform : null;
            if (lay == null) return;
            ShowAt(lay, screen, new[] { terms[n] }, cards, CamOf(lay.GetComponentInParent<Canvas>()), true);
            owner = this; openIdx = n;
        }

        /// <summary>
        /// 낱말 판(여럿이면 세로로 쌓는다)을 screen 자리 오른쪽 위에(넘치면 왼쪽 · 화면 안으로) 띄운다. 앞에 띄운 판은 닫는다.
        /// 폰(터치)은 바깥을 누르면 닫히는 덮개를 깐다. 돌려줌: 판 묶음.
        /// </summary>
        public static RectTransform ShowAt(RectTransform layer, Vector2 screen, IList<CardTerms.Term> list, CardMaker cardMaker = null, Camera cam = null, bool hover = false)
        {
            Close();
            if (layer == null || list == null || list.Count == 0) return null;
            RectTransform root;
            if (Touch || !hover)   // 올림으로 띄운 판(PC)만 덮개 없이 — 그 밖은 바깥을 누르면 닫힌다
            {
                var shade = Ui.Img(layer, Theme.White, new Color(0, 0, 0, 0.001f), "termpop shade", true);
                shade.rectTransform.Fill();
                var b = shade.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(Close);
                root = shade.rectTransform;
            }
            else root = Ui.Rect("termpop", layer).Fill();
            open = root;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, cam, out var lp);
            var stack = Ui.Rect("termstack", root);
            stack.anchorMin = stack.anchorMax = new Vector2(0.5f, 0.5f);
            stack.pivot = Vector2.zero;
            float y = 0, wMax = 0;
            var boxes = new List<RectTransform>();
            foreach (var t in list)
            {
                var bx = t.IsCard && cardMaker != null ? CardBox(stack, t, cardMaker) : Box(stack, t, Theme.C(400, 440), () => Reflow(stack));
                boxes.Add(bx);
            }
            // 세로로 쌓기(위에서 아래로)
            float total = 0;
            foreach (var bx in boxes) { total += bx.sizeDelta.y; wMax = Mathf.Max(wMax, bx.sizeDelta.x); }
            total += (boxes.Count - 1) * 8;
            y = total;
            foreach (var bx in boxes)
            {
                bx.anchorMin = bx.anchorMax = new Vector2(0, 0); bx.pivot = new Vector2(0, 1);
                bx.anchoredPosition = new Vector2(0, y);
                y -= bx.sizeDelta.y + 8;
            }
            stack.sizeDelta = new Vector2(wMax, total);
            openBox = stack;
            Place(stack, lp, layer.rect.size, stack.sizeDelta);
            Tw.Pop(stack, 0, 0.92f, 0.16f);
            return stack;
        }

        // 「자세히」를 펼쳐 판 높이가 바뀌면 다시 쌓고 화면 안으로
        static void Reflow(RectTransform stack)
        {
            if (stack == null) return;
            float total = 0, wMax = 0;
            var kids = new List<RectTransform>();
            foreach (RectTransform c in stack) { kids.Add(c); total += c.sizeDelta.y; wMax = Mathf.Max(wMax, c.sizeDelta.x); }
            total += Mathf.Max(0, kids.Count - 1) * 8;
            float top = stack.anchoredPosition.y + stack.sizeDelta.y;   // 위 끝을 그대로 두고 아래로 늘린다
            float y = total;
            foreach (var c in kids) { c.anchoredPosition = new Vector2(0, y); y -= c.sizeDelta.y + 8; }
            stack.sizeDelta = new Vector2(wMax, total);
            var layer = stack.parent != null ? stack.parent.parent as RectTransform : null;
            var size = layer != null ? layer.rect.size : new Vector2(1600, 900);
            float ny = top - total;
            if (ny < -size.y / 2 + 12) ny = -size.y / 2 + 12;
            stack.anchoredPosition = new Vector2(stack.anchoredPosition.x, ny);
        }

        static RectTransform CardBox(RectTransform parent, CardTerms.Term t, CardMaker maker)
        {
            float cw = Theme.C(210, 200);
            var holder = Ui.Rect("termcard " + t.CardId, parent);
            holder.sizeDelta = new Vector2(cw, cw * 1.4f + (t.Rel != null ? 30 : 0));
            var sh = Ui.Img(holder, Theme.S("shadow", 40), Color.black.A(0.6f), "shadow"); sh.rectTransform.Fill(-18, -26, -18, -10);
            var card = maker(holder, t.CardId, cw);
            if (card != null)
            {
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0);
                card.anchoredPosition = Vector2.zero; card.sizeDelta = new Vector2(cw, cw * 1.4f);
                // 미리보기 카드 속 낱말은 다시 띄우지 않는다 · 누름도 받지 않는다
                foreach (var tp in card.GetComponentsInChildren<TermPop>(true)) Destroy(tp);
                foreach (var g in card.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            }
            if (t.Rel != null)
            {
                var (pb, pt) = Ui.Chip(holder, Theme.S("ic_spark"), t.Rel, 26, Theme.NavyPanel.A(0.95f), Theme.Gold, Theme.FsCap);
                pb.rectTransform.anchorMin = pb.rectTransform.anchorMax = pb.rectTransform.pivot = new Vector2(0.5f, 1);
                pb.rectTransform.anchoredPosition = new Vector2(0, 0); pb.raycastTarget = false;
                pt.color = Theme.Gold;
            }
            return holder;
        }

        /// <summary>
        /// 낱말 판 하나 — 이름(금 · 해로운 것은 붉게) · 한두 줄 · (고유 효과면) 「자세히」 펼침. 크기는 글에 맞춘다(width 고정).
        /// relayout 은 「자세히」로 높이가 바뀐 뒤 부른다(쌓기 다시). 생성 카드 낱말을 카드 그림 없이 그릴 때도 이것.
        /// </summary>
        public static RectTransform Box(Transform parent, CardTerms.Term t, float width, Action relayout = null)
        {
            var bg = Ui.Img(parent, Theme.Round, Theme.NavyPanel.A(0.97f), "termbox " + t.Name, true);
            bg.pixelsPerUnitMultiplier = 2f;
            var box = bg.rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0, 1); box.pivot = new Vector2(0, 1);
            bg.gameObject.AddComponent<Swallow>();   // 판을 눌러도 뒤(폰 덮개)로 새지 않게
            Color edge = t.Bad ? Theme.Bad : t.Hero ? Theme.Gold : t.IsCard ? Theme.Sky : Theme.Edge;
            var rim = Ui.Img(box, Theme.Frame, edge, "rim"); rim.rectTransform.Fill();
            var top = Ui.Img(box, Theme.White, edge.A(t.Bad || t.Hero ? 1 : 0.6f), "bar"); top.rectTransform.Band(1, 3, 8, 8, -3);
            bool more = false;
            float inner = width - 36;
            var col = Ui.Rect("col", box).Fill(18, 14, 18, 14);

            void Build()
            {
                Ui.Clear(col);
                float h = 28;
                string kind = t.Kind ?? (t.Hero ? $"고유 효과{(t.Owner != null ? " · " + t.Owner : "")}" : t.IsCard ? "카드" : t.Bad ? "해로운 효과" : null);
                var nm = Ui.Title(col, t.Name + (kind != null ? $"  <size=66%><color={Theme.SubTag}>{kind}</color></size>" : ""), Theme.FsMd, t.Bad ? Theme.Bad : t.IsCard ? Theme.Sky : Theme.Gold, TextAlignmentOptions.TopLeft, "name");
                nm.textWrappingMode = TextWrappingModes.Normal;
                float y = 0;
                void Put(TextMeshProUGUI tx)
                {
                    float ph = tx.GetPreferredValues(tx.text, inner, 0).y;
                    tx.rectTransform.anchorMin = new Vector2(0, 1); tx.rectTransform.anchorMax = new Vector2(1, 1); tx.rectTransform.pivot = new Vector2(0.5f, 1);
                    tx.rectTransform.offsetMin = new Vector2(0, 0); tx.rectTransform.offsetMax = new Vector2(0, 0);
                    tx.rectTransform.sizeDelta = new Vector2(0, ph); tx.rectTransform.anchoredPosition = new Vector2(0, -y);
                    y += ph + 6;
                }
                Put(nm);
                string bodyText = t.Body ?? (t.IsCard ? "만들어지는 카드" : "");
                var body = Ui.Text(col, bodyText, Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft, false, "body");
                body.textWrappingMode = TextWrappingModes.Normal; body.overflowMode = TextOverflowModes.Overflow; body.lineSpacing = 2;
                Put(body);
                bool hasMore = !string.IsNullOrEmpty(t.Detail) && t.Detail != t.Body;
                if (hasMore && more)
                {
                    var rule = Ui.Img(col, Theme.White, Theme.Line, "rule");
                    rule.rectTransform.anchorMin = new Vector2(0, 1); rule.rectTransform.anchorMax = new Vector2(1, 1); rule.rectTransform.pivot = new Vector2(0.5f, 1);
                    rule.rectTransform.sizeDelta = new Vector2(0, 1); rule.rectTransform.anchoredPosition = new Vector2(0, -y - 1); y += 8;
                    var dt = Ui.Text(col, t.Detail, Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft, false, "detail");
                    dt.textWrappingMode = TextWrappingModes.Normal; dt.overflowMode = TextOverflowModes.Overflow; dt.lineSpacing = 2;
                    Put(dt);
                }
                if (hasMore)
                {
                    var mb = MoreChip(col, more);
                    var mrt = mb.GetComponent<RectTransform>();
                    mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(0, 1);
                    mrt.anchoredPosition = new Vector2(0, -y);
                    y += mrt.sizeDelta.y + 4;
                    mb.OnClick = () => { more = !more; Build(); relayout?.Invoke(); };
                }
                h += y;
                box.sizeDelta = new Vector2(width, h);
            }
            Build();
            return box;
        }

        /// <summary>「자세히 ▼ / 접기 ▲」 작은 알약 단추.</summary>
        public static Btn MoreChip(Transform parent, bool open, string name = "more")
        {
            var b = Btn.Make(parent, null, BtnStyle.PillDark, null, 0, name);
            var rt = b.GetComponent<RectTransform>();
            var t = Ui.Title(rt, open ? "접기 ▲" : "자세히 ▼", Theme.FsCap + 1, Theme.Gold, TextAlignmentOptions.Center);
            t.rectTransform.Fill(12, 0, 12, 0); t.textWrappingMode = TextWrappingModes.NoWrap;
            b.Label = t;
            float w = t.GetPreferredValues(t.text).x + 30;
            rt.sizeDelta = new Vector2(w, 30);
            b.Pref(w, 30);
            return b;
        }

        /// <summary>누름을 받아 삼킨다(부모로 올라가지 않게).</summary>
        public class Swallow : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
        {
            public void OnPointerClick(PointerEventData e) { }
            public void OnPointerDown(PointerEventData e) { }
        }

        // 낱말 오른쪽 위에, 화면 안으로
        static void Place(RectTransform box, Vector2 lp, Vector2 size, Vector2 bs)
        {
            float x = lp.x + 18, y = lp.y + 12;
            if (x + bs.x > size.x / 2 - 12) x = lp.x - 18 - bs.x;
            if (y + bs.y > size.y / 2 - 12) y = size.y / 2 - 12 - bs.y;
            if (y < -size.y / 2 + 12) y = -size.y / 2 + 12;
            if (x < -size.x / 2 + 12) x = -size.x / 2 + 12;
            box.anchoredPosition = new Vector2(x, y);
        }
    }
}
