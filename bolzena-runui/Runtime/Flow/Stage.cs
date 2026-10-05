using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 무대 — 캔버스 한 장(기준 1600×900, Expand)과 층: 배경 · 화면 · 창(모달) · 알림 · 페이드.
    // 화면을 바꿀 때는 어둡게 내렸다가(0.2초) 새 화면을 세우고 걷는다. 배경은 천천히 다가온다(켄 번스).
    public class Stage : MonoBehaviour
    {
        public RectTransform Root, BgLayer, ScreenLayer, ModalLayer, ToastLayer;
        public Canvas Canvas;
        Image bgA, bgB, fade, tint;
        string bgName;
        public bool Busy { get; private set; }
        /// <summary>새 화면이 다 서고 등장 연출이 끝났다(자동 데모가 찍는 때).</summary>
        public event Action<string> Shown;
        public string Current { get; private set; }
        /// <summary>이름 붙인 버튼 — 자동 데모가 누른다. 화면을 바꿀 때 비운다.</summary>
        public readonly Dictionary<string, Btn> Hot = new Dictionary<string, Btn>();

        public static Stage Create()
        {
            var go = new GameObject("RunUI Stage");
            var st = go.AddComponent<Stage>();
            st.Build();
            return st;
        }

        void Build()
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            Canvas = cgo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var sc = cgo.AddComponent<CanvasScaler>();
            scaler = sc;
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            Theme.MeasureCompact();
            sc.referenceResolution = Theme.Compact ? new Vector2(1280, 720) : new Vector2(1600, 900);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            sc.referencePixelsPerUnit = 100;
            cgo.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)cgo.transform;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
                es.transform.SetParent(transform, false);
            }

            BgLayer = Ui.Rect("bg", Root).Fill();
            var night = Ui.Img(BgLayer, Theme.White, Theme.Night, "night");
            night.rectTransform.Fill();
            bgA = MakeBg("bgA");
            bgB = MakeBg("bgB");
            tint = Ui.Img(BgLayer, Theme.S("vignette"), new Color(1, 1, 1, 0.85f), "vignette");
            tint.rectTransform.Fill();
            ScreenLayer = Ui.Rect("screen", Root).Fill();
            ModalLayer = Ui.Rect("modal", Root).Fill();
            ToastLayer = Ui.Rect("toast", Root).Fill();
            Toast.Layer = ToastLayer;
            fade = Ui.Img(Root, Theme.White, new Color(0.02f, 0.015f, 0.04f, 1), "fade", true);
            fade.rectTransform.Fill();
        }

        CanvasScaler scaler;
        Vector2Int lastScreen;
        /// <summary>창 크기가 바뀌면(폰 ↔ PC 크기) 기준 해상도를 다시 고른다 — 새 화면부터 맞는 크기로 선다.</summary>
        void Update()
        {
            var now = new Vector2Int(Screen.width, Screen.height);
            if (now == lastScreen) return;
            lastScreen = now;
            if (Theme.MeasureCompact() && scaler != null)
                scaler.referenceResolution = Theme.Compact ? new Vector2(1280, 720) : new Vector2(1600, 900);
        }

        /// <summary>지금 캔버스 크기(단위) — PC 는 높이 900, 폰은 720 언저리.</summary>
        public Vector2 Size => Root != null && Root.rect.width > 100 ? Root.rect.size : (Theme.Compact ? new Vector2(1560, 720) : new Vector2(1600, 900));

        Image MakeBg(string name)
        {
            var im = Ui.Img(BgLayer, null, new Color(1, 1, 1, 0), name);
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var f = im.gameObject.AddComponent<AspectRatioFitter>();
            f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            f.aspectRatio = 16f / 9f;
            return im;
        }

        /// <summary>배경 — 같은 그림이면 그대로, 다르면 겹쳐 바꾼다. dim 은 배경 위의 어둠(0~1).</summary>
        public void SetBg(string name, float dim = 0.35f, float blurLike = 0)
        {
            var sp = name != null ? Theme.Bg(name) : null;
            if (name != bgName)
            {
                bgName = name;
                // 새 그림은 B 에 놓고 A 와 바꾼다
                (bgA, bgB) = (bgB, bgA);
                bgA.transform.SetSiblingIndex(2);
                bgA.sprite = sp;
                if (sp != null) bgA.GetComponent<AspectRatioFitter>().aspectRatio = sp.rect.width / sp.rect.height;
                var a = bgA; var b = bgB;
                a.rectTransform.localScale = Vector3.one * 1.06f;
                float b0 = b.color.a;
                Tw.Run(a, 0.6f, t => { if (a) a.color = a.color.A(sp != null ? t : 0); if (b) b.color = b.color.A(b0 * (1 - t)); });
                if (burns != null) StopCoroutine(burns);
                burns = StartCoroutine(KenBurns(a));
            }
            Tw.Run(tint, 0.5f, t => { if (tint) tint.color = new Color(0, 0, 0, Mathf.Lerp(tint.color.a, Mathf.Clamp01(0.5f + dim), t)); });
            darkness = dim;
        }

        float darkness;
        Coroutine burns;
        IEnumerator KenBurns(Image im)
        {
            float t = 0;
            while (im != null)
            {
                t += Time.unscaledDeltaTime;
                if (!Settings.ReduceMotion) im.rectTransform.localScale = Vector3.one * (1.06f - 0.04f * Mathf.Sin(t * 0.05f));
                // 배경 위 어둠은 tint(가장자리) + 그림 자체의 밝기로
                float v = 1 - darkness * 0.75f;
                var c = im.color; im.color = new Color(v, v, v * 1.02f, c.a);
                yield return null;
            }
        }

        /// <summary>화면을 바꾼다 — 어둡게 내리고 · 비우고 · build 로 세우고 · 걷는다.</summary>
        /// <summary>instant 면 어둡게 내리지 않고 곧장 바꾼다(전투 장면 위에 보상을 띄울 때 — 밑 장면이 검게 가려지지 않게).</summary>
        public void Show(string name, Action<RectTransform> build, float shownAfter = 0.9f, bool instant = false)
        {
            StartCoroutine(ShowCo(name, build, shownAfter, instant));
        }

        /// <summary>배경 층(밤색 바탕 · 배경 그림 · 테두리 어둠)을 숨긴다 — 밑의 다른 장면(전투)이 비쳐 보이게.</summary>
        public bool Bare
        {
            get => BgLayer != null && !BgLayer.gameObject.activeSelf;
            set { if (BgLayer != null) BgLayer.gameObject.SetActive(!value); }
        }

        IEnumerator ShowCo(string name, Action<RectTransform> build, float shownAfter, bool instant = false)
        {
            Busy = true;
            fade.raycastTarget = true;
            float a0 = fade.color.a;
            if (a0 < 0.99f && !instant)
            {
                yield return Lerp(0.2f, t => SetFade(Mathf.Lerp(a0, 1, t)));
            }
            Hot.Clear();
            Ui.Clear(ScreenLayer);
            Ui.Clear(ModalLayer);
            Current = name;
            var root = Ui.Rect(name, ScreenLayer).Fill();
            try { build(root); }
            catch (Exception e) { Debug.LogException(e); Toast.Show("화면을 세우지 못했습니다 — " + e.Message); }
            yield return null;
            if (instant) SetFade(0);
            else yield return Lerp(0.32f, t => SetFade(1 - t));
            fade.raycastTarget = false;
            Busy = false;
            Debug.Log("[Stage] 화면 " + name);
            float w = 0;
            while (w < shownAfter) { w += Time.unscaledDeltaTime; yield return null; }
            if (Current == name) Shown?.Invoke(name);
        }

        void SetFade(float a) { fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, a); }

        IEnumerator Lerp(float dur, Action<float> f)
        {
            if (Settings.ReduceMotion) dur *= 0.3f;
            float t = 0;
            while (t < dur) { f(Tw.OutCubic(t / dur)); t += Time.unscaledDeltaTime; yield return null; }
            f(1);
        }

        // ── 창(모달) ──
        /// <summary>어둡게 깐 위에 가운데 판을 띄운다(캔버스보다 크면 줄인다). 돌려줌: 판(안에 채운다)과 닫기.</summary>
        public (RectTransform panel, Action close) Modal(string name, float w, float h, bool closeOnDim = true, Action onClose = null)
        {
            var size = Size;
            w = Mathf.Min(w, size.x - 32);
            h = Mathf.Min(h, size.y - 24);
            var layer = Ui.Rect("modal " + name, ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0.01f, 0.015f, 0.04f, 0), "dim", true);
            dim.rectTransform.Fill();
            var panel = Ui.Panel(layer, Theme.Panel, null, "panel").rectTransform;
            panel.At(0.5f, 0.5f, 0, 0, w, h);
            Ui.Shadow(panel, 30, -12, 0.7f);
            bool closed = false;
            Action close = null;
            close = () =>
            {
                if (closed) return;
                closed = true;
                var g = layer.Group();
                Tw.Run(layer, 0.18f, t => { if (g) g.alpha = 1 - t; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); onClose?.Invoke(); });
            };
            if (closeOnDim) { var b = dim.gameObject.AddComponent<Btn>(); b.Bg = null; b.OnClick = close; }
            Tw.Run(dim, 0.25f, t => { if (dim) dim.color = new Color(0.01f, 0.015f, 0.04f, 0.74f * t); });
            Tw.Pop(panel, 0, 0.92f, 0.3f);
            return (panel, close);
        }

        /// <summary>머리가 붙은 창 — 금빛 제목 · 회색 부제 · 오른쪽 위 닫기(×) · 가는 나눔 선. 돌려줌: 몸통(머리 아래)과 닫기.
        /// foot 를 주면 아래에 그 높이만큼 단추 띠를 비워 두고 foot 으로 돌려준다.</summary>
        public (RectTransform body, Action close, RectTransform foot) ModalBox(string name, float w, float h, string title, string sub = null,
            bool closeOnDim = true, Action onClose = null, float footH = 0, bool closeX = true)
        {
            var (panel, close) = Modal(name, w, h, closeOnDim, onClose);
            float headH = sub != null ? 86 : 66;
            float rightPad = closeX ? 80 : 28;
            var head = Ui.Rect("head", panel).Band(1, headH);
            var t = Ui.Title(head, title, Theme.FsXl, Theme.Gold, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Band(1, 42, 28, rightPad, sub != null ? -10 : -12);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true; t.fontSizeMin = Theme.FsMd; t.fontSizeMax = Theme.FsXl;
            if (sub != null)
            {
                var s = Ui.Text(head, sub, Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                s.rectTransform.Band(1, 26, 30, rightPad, -52);
                s.textWrappingMode = TextWrappingModes.NoWrap; s.overflowMode = TextOverflowModes.Ellipsis;
            }
            var line = Ui.Img(panel, Theme.White, Theme.Line, "rule");
            line.rectTransform.Band(1, 1, 20, 20, -headH);
            if (closeX)
            {
                var x = Btn.Icon(panel, Theme.S("ic_x"), close, 44, "close");
                x.GetComponent<RectTransform>().At(1, 1, -18, -14, 44, 44);
                Hot["modal.x"] = x;
            }
            RectTransform foot = null;
            if (footH > 0)
            {
                foot = Ui.Rect("foot", panel).Band(0, footH, 20, 20, 0);
                var fl = Ui.Img(panel, Theme.White, Theme.Line, "rule2");
                fl.rectTransform.Band(0, 1, 20, 20, footH);
            }
            var body = Ui.Rect("body", panel).Fill(20, footH + (footH > 0 ? 8 : 18), 20, headH + 12);
            return (body, close, foot);
        }
    }
}
