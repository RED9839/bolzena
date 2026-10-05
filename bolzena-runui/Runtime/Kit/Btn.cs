using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    public enum BtnStyle { Gold, Brown, Dark, Red, Cell, Ghost, PillGold, PillRose, PillDark, Glass }

    // 버튼 — 올리면 살짝 커지고 밝아지며 금빛 테두리가 켜지고(Hl), 누르면 눌렸다가 튀어 오른다. 못 누르는 버튼은 흐리고 누르면 도리질.
    //   주 동작 = Gold · PillGold(금) · 보조 = Dark · PillDark(남색 + 금 테두리) · 위험 = Red · PillRose · 칸 = Cell · Glass
    // 자동 데모는 Press() 로 같은 반응을 거쳐 누른다(가짜 손가락).
    public class Btn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Action OnClick;
        /// <summary>길게 누르기(0.45초) — 있으면 길게 눌렀을 때 이것을 부르고 그 뗌은 누름(OnClick)으로 치지 않는다.</summary>
        public Action OnHold;
        float held; bool holdFired;
        public Image Bg;
        public TextMeshProUGUI Label;
        public Image Glow;
        /// <summary>올리면 켜지는 금빛 테두리(남색 버튼 · 칸).</summary>
        public Image Hl;
        float hl;
        bool on = true, hover, down;
        float scale = 1, target = 1;
        Color baseColor;
        /// <summary>못 누르는 까닭 — 누르면 짧게 알린다.</summary>
        public string Why;

        public bool Interactable
        {
            get => on;
            set
            {
                on = value;
                var g = this.Group();
                g.alpha = on ? 1 : 0.5f;
            }
        }

        public static Btn Make(Transform parent, string label, BtnStyle style, Action onClick, float fontSize = 26, string name = null)
        {
            var sprite = style switch
            {
                BtnStyle.Gold => Theme.S("btn_gold", 26),
                BtnStyle.Brown => Theme.S("btn_brown", 26),
                BtnStyle.Red => Theme.S("btn_red", 26),
                BtnStyle.Cell => Theme.Cell,
                BtnStyle.Ghost => Theme.S("btn_dark", 26),
                BtnStyle.PillGold => Theme.S("pill_gold", 46),
                BtnStyle.PillRose => Theme.S("pill_rose", 46),
                BtnStyle.PillDark => Theme.S("pill_dark", 46),
                BtnStyle.Glass => Theme.Glass,
                _ => Theme.S("btn_dark", 26),
            };
            var bg = Ui.Img(parent, sprite, style == BtnStyle.Ghost ? new Color(1, 1, 1, 0.6f) : Color.white, name ?? ("btn " + label), true);
            var b = bg.gameObject.AddComponent<Btn>();
            b.Bg = bg;
            b.baseColor = bg.color;
            b.OnClick = onClick;
            if (style != BtnStyle.Gold && style != BtnStyle.PillGold && style != BtnStyle.Red && style != BtnStyle.PillRose)
            {
                bool pill = style == BtnStyle.PillDark;
                b.Hl = Ui.Img(bg.transform, pill ? Theme.FramePill : Theme.Frame, Theme.Gold.A(0), "hl");
                b.Hl.rectTransform.Fill();
                b.Hl.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;   // 버튼에 줄 배치를 걸어도 테두리는 빠진다
            }
            if (label != null)
            {
                var col = style == BtnStyle.Gold || style == BtnStyle.PillGold ? Theme.Brown : style == BtnStyle.Red || style == BtnStyle.PillRose ? Color.white : Theme.Ink;
                b.Label = Ui.Title(bg.transform, label, fontSize, col, TextAlignmentOptions.Center);
                b.Label.rectTransform.Fill(14, 4, 14, 4);
                b.Label.textWrappingMode = TextWrappingModes.NoWrap;
                b.Label.overflowMode = TextOverflowModes.Ellipsis;
            }
            return b;
        }

        /// <summary>갈색 원(아이콘 버튼 — 뒤로 · 설정 · 전체화면).</summary>
        public static Btn Icon(Transform parent, Sprite icon, Action onClick, float size = 56, string name = "iconbtn")
        {
            var bg = Ui.Img(parent, Theme.S("circle"), Theme.NavyCell.A(0.92f), name, true);
            var ring = Ui.Img(bg.transform, Theme.S("ring"), Theme.Edge.A(0.7f), "ring");
            ring.rectTransform.Fill();
            var ic = Ui.Img(bg.transform, icon, Theme.Ink, "icon");
            ic.rectTransform.Fill(size * 0.26f, size * 0.26f, size * 0.26f, size * 0.26f);
            ic.preserveAspect = true;
            bg.Pref(size, size);
            bg.rectTransform.sizeDelta = new Vector2(size, size);
            var b = bg.gameObject.AddComponent<Btn>();
            b.Bg = bg; b.baseColor = bg.color; b.OnClick = onClick;
            var hlr = Ui.Img(bg.transform, Theme.S("ring"), Theme.Gold.A(0), "hl");
            hlr.rectTransform.Fill();
            hlr.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            b.Hl = hlr;
            return b;
        }

        public void SetLabel(string s) { if (Label) Label.text = s; }
        public void SetColor(Color c) { baseColor = c; if (Bg) Bg.color = c; }

        void Update()
        {
            if (down && on && OnHold != null && !holdFired)
            {
                held += Time.unscaledDeltaTime;
                if (held >= 0.45f) { holdFired = true; down = false; Sfx.Click(); OnHold(); }
            }
            target = !on ? 1 : down ? 0.94f : hover ? 1.035f : 1;
            if (Mathf.Abs(scale - target) < 0.0005f && !hover && !down)
            {
                if (scale != 1) { scale = 1; transform.localScale = Vector3.one; }
            }
            else
            {
                scale = Mathf.Lerp(scale, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 22));
                transform.localScale = new Vector3(scale, scale, 1);
            }
            if (Hl)
            {
                float want = !on ? 0 : down ? 1 : hover ? 0.9f : 0;
                hl = Mathf.Lerp(hl, want, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 18));
                Hl.color = Theme.Gold.A(hl);
            }
            if (Bg)
            {
                var c = baseColor;
                float k = !on ? 1 : down ? 0.82f : hover ? 1.12f : 1;
                Bg.color = new Color(Mathf.Min(1, c.r * k), Mathf.Min(1, c.g * k), Mathf.Min(1, c.b * k), c.a);
            }
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; }
        public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
        public void OnPointerDown(PointerEventData e) { down = true; held = 0; holdFired = false; }
        public void OnPointerUp(PointerEventData e) { down = false; }
        public void OnPointerClick(PointerEventData e) { if (holdFired) { holdFired = false; return; } Fire(); }

        void Fire()
        {
            if (!on)
            {
                Tw.Shake((RectTransform)transform, 6);
                if (!string.IsNullOrEmpty(Why)) Toast.Show(Why);
                return;
            }
            scale = 1.08f;    // 튀어 오르기
            Sfx.Click();
            OnClick?.Invoke();
        }

        /// <summary>올림 상태만 켠다 · 끈다(자동 데모가 버튼 상태를 찍을 때).</summary>
        public void Hover(bool v) { hover = v; if (!v) down = false; }

        /// <summary>가짜 손가락 — 길게 누른다(자동 데모).</summary>
        public System.Collections.IEnumerator LongPress()
        {
            hover = true;
            yield return new WaitForSecondsRealtime(0.15f);
            down = true; held = 0; holdFired = false;
            float t = 0;
            while (!holdFired && t < 1.2f) { t += Time.unscaledDeltaTime; yield return null; }
            down = false; holdFired = false; hover = false;
        }

        /// <summary>가짜 손가락 — 올리고 · 누르고 · 떼는 반응을 거쳐 누른다(자동 데모).</summary>
        public System.Collections.IEnumerator Press(float hold = 0.12f)
        {
            hover = true;
            yield return new WaitForSecondsRealtime(0.18f);
            down = true;
            yield return new WaitForSecondsRealtime(hold);
            down = false;
            Fire();
            yield return new WaitForSecondsRealtime(0.05f);
            hover = false;
        }
    }
}
