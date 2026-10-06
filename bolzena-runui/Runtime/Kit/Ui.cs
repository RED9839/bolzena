using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // uGUI 조각을 코드로 만드는 손 — 사각형 · 그림 · 글 · 버튼 · 줄/칸 배치 · 스크롤.
    // 좌표는 캔버스 기준 픽셀(기준 해상도 1600×900, Expand). 앵커를 써서 화면 비가 달라도(폰 가로 844×390) 가장자리에 붙는다.
    public static class Ui
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        // ── 배치 ──
        /// <summary>부모를 꽉 채운다(안쪽 여백 l · b · r · t).</summary>
        public static RectTransform Fill(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>한 점(앵커 = 피벗)에 크기 w×h 로, pos 만큼 밀어서.</summary>
        public static RectTransform At(this RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, ay);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>가로로 늘인 띠 — 위(ay=1) 또는 아래(ay=0)에 높이 h.</summary>
        public static RectTransform Band(this RectTransform rt, float ay, float h, float l = 0, float r = 0, float y = 0)
        {
            rt.anchorMin = new Vector2(0, ay); rt.anchorMax = new Vector2(1, ay); rt.pivot = new Vector2(0.5f, ay);
            rt.offsetMin = new Vector2(l, 0); rt.offsetMax = new Vector2(-r, 0);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            return rt;
        }

        /// <summary>세로로 늘인 기둥 — 왼쪽(ax=0) · 오른쪽(ax=1)에 너비 w.</summary>
        public static RectTransform Column(this RectTransform rt, float ax, float w, float b = 0, float t = 0, float x = 0)
        {
            rt.anchorMin = new Vector2(ax, 0); rt.anchorMax = new Vector2(ax, 1); rt.pivot = new Vector2(ax, 0.5f);
            rt.offsetMin = new Vector2(0, b); rt.offsetMax = new Vector2(0, -t);
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            return rt;
        }

        public static LayoutElement Pref(this Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) le.preferredHeight = h;
            if (h >= 0) le.minHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        // ── 그림 ──
        public static Image Img(Transform parent, Sprite s, Color? c = null, string name = "img", bool raycast = false)
        {
            var rt = Rect(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s;
            im.color = c ?? Color.white;
            im.raycastTarget = raycast;
            if (s != null && s.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        /// <summary>판 — 어두운 판(panel) · 칸(cell) · 종이(paper) …</summary>
        public static Image Panel(Transform parent, Sprite s = null, Color? c = null, string name = "panel")
        {
            var im = Img(parent, s ?? Theme.Panel, c, name, true);
            return im;
        }

        /// <summary>그림자를 깐 판 — 아래로 살짝 떨어진 부드러운 그림자.</summary>
        public static Image Shadow(RectTransform target, float spread = 22, float dy = -8, float alpha = 0.55f)
        {
            var sh = Img(target.parent, Theme.S("shadow", 40), new Color(0, 0, 0, alpha), target.name + "_shadow");
            sh.rectTransform.SetSiblingIndex(target.GetSiblingIndex());
            sh.rectTransform.anchorMin = target.anchorMin; sh.rectTransform.anchorMax = target.anchorMax; sh.rectTransform.pivot = target.pivot;
            sh.rectTransform.anchoredPosition = target.anchoredPosition + new Vector2(0, dy);
            sh.rectTransform.sizeDelta = target.sizeDelta + new Vector2(spread * 2, spread * 2);
            return sh;
        }

        // ── 글 ──
        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color? c = null,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool title = false, string name = "text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = title ? Theme.Title : Theme.Body;
            CardTerms.Prepare(t);   // 글 속 카드 아이콘(<sprite>)이 글자로 새지 않게 — 판 화면 글 모두
            t.text = text;
            t.fontSize = size * Settings.TextScale;
            t.color = c ?? Theme.Ink;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.richText = true;
            t.lineSpacing = title ? 0 : 6;
            return t;
        }

        public static TextMeshProUGUI Title(Transform parent, string text, float size, Color? c = null,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, string name = "title")
            => Text(parent, text, size, c, align, true, name);

        /// <summary>글 둘레에 테두리(배경 위에 바로 얹는 제목).</summary>
        //   글마다 재질을 복제하지 않고(outlineWidth 세터는 글마다 「(Instance)」 재질을 만든다) 글꼴 재질 · 굵기 · 색마다 하나를 나눠 쓴다 —
        //   복제본은 화면을 세울 때마다 쌓여 전투로 넘어갈 때의 정리(UnloadUnusedAssets)를 길게 했다(2026-10-06 연속 싸움 시험)
        static readonly System.Collections.Generic.Dictionary<string, Material> outlineMats = new System.Collections.Generic.Dictionary<string, Material>();
        public static TextMeshProUGUI Outline(this TextMeshProUGUI t, float w = 0.18f, Color? c = null)
        {
            var col = c ?? new Color(0.05f, 0.03f, 0.08f, 0.9f);
            var src = t.fontSharedMaterial;
            if (src == null) { t.outlineWidth = w; t.outlineColor = col; return t; }
            string key = src.GetEntityId() + "|" + w.ToString("F3") + "|" + (Color32)col;
            if (!outlineMats.TryGetValue(key, out var m) || m == null)
            {
                m = new Material(src) { name = src.name + " outline" };

                m.SetFloat(ShaderUtilities.ID_OutlineWidth, w);
                m.SetColor(ShaderUtilities.ID_OutlineColor, col);
                m.hideFlags = HideFlags.DontUnloadUnusedAsset;   // 나눠 쓰는 것 — 화면이 바뀌어도 둔다(가짓수는 몇 안 된다)
                outlineMats[key] = m;
            }
            t.fontSharedMaterial = m;
            return t;
        }

        // ── 줄 · 칸 ──
        public static HorizontalLayoutGroup Row(RectTransform rt, float gap = 10, TextAnchor align = TextAnchor.MiddleLeft, RectOffset pad = null,
            bool expandW = false, bool expandH = true)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = gap; g.childAlignment = align; g.padding = pad ?? new RectOffset();
            g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            return g;
        }

        public static VerticalLayoutGroup Col(RectTransform rt, float gap = 10, TextAnchor align = TextAnchor.UpperLeft, RectOffset pad = null,
            bool expandW = true, bool expandH = false)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = gap; g.childAlignment = align; g.padding = pad ?? new RectOffset();
            g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            return g;
        }

        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
        public static RectOffset Pad(int x, int y) => new RectOffset(x, x, y, y);

        /// <summary>세로 스크롤 — content 에 Col 을 걸어 쓴다. 돌려줌: content.</summary>
        public static RectTransform Scroll(RectTransform area, out ScrollRect sr, bool horizontal = false)
        {
            var view = Rect("view", area).Fill();
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var content = Rect("content", view);
            if (horizontal)
            {
                content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
                content.offsetMin = content.offsetMax = Vector2.zero;
            }
            else
            {
                content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
                content.offsetMin = content.offsetMax = Vector2.zero;
            }
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            if (horizontal) fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            else fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr = area.gameObject.AddComponent<ScrollRect>();
            sr.viewport = view; sr.content = content;
            sr.horizontal = horizontal; sr.vertical = !horizontal;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            sr.inertia = true;
            return content;
        }

        public static CanvasGroup Group(this Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            return g != null ? g : c.gameObject.AddComponent<CanvasGroup>();
        }

        // ── 아이콘 + 글 알약(골드 · 덱 수 …) ──
        public static (Image bg, TextMeshProUGUI text) Chip(Transform parent, Sprite icon, string text, float h = 44, Color? bg = null, Color? iconTint = null, float size = 22)
        {
            var b = Img(parent, Theme.Pill, bg ?? new Color(0.08f, 0.07f, 0.13f, 0.85f), "chip", true);
            Row(b.rectTransform, 8, TextAnchor.MiddleCenter, new RectOffset(icon != null ? 10 : 18, 18, 4, 4), false, false);
            if (icon != null)
            {
                var ic = Img(b.transform, icon, iconTint ?? Color.white, "icon");
                ic.preserveAspect = true;
                ic.Pref(h - 14, h - 14);
            }
            var t = Title(b.transform, text, size, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            b.Pref(-1, h);
            return (b, t);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string Gold(int n) => n.ToString("N0");
    }
}
