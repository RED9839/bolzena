using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 로딩 화면(2026-10-07 「처음 모험에 진입하면 응답 없음 · 검은 화면에 걸린다」) — 전투로 넘어가는 동안 덮는 판: 제목 · 진행 막대 · 지금 하는 일 · 알아 두기 한 줄.
    //   장면을 넘어 산다(DontDestroyOnLoad). 무거운 일은 부르는 쪽이 여러 프레임에 나눠 하고 Progress 로 알린다 — 막대는 프레임마다 목표로 다가가
    //   한 단계가 조금 길어도 멈춘 화면으로 보이지 않는다. 화면 맨 위(정렬 32000)에 그려 아래 장면(전투)이 뒤에서 세워지고 그려지는 동안 가린다.
    public static class LoadingScreen
    {
        static Canvas canvas;
        static CanvasGroup group;
        static RectTransform fill;
        static TextMeshProUGUI title, step, pct, tip;
        static float target, shown;
        static Pump pump;

        /// <summary>떠 있나(걷는 중 포함).</summary>
        public static bool Visible => canvas != null && canvas.enabled;

        sealed class Pump : MonoBehaviour
        {
            public float FadeLeft, FadeDur;
            void Update()
            {
                if (canvas == null || !canvas.enabled) return;
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                // 목표로 다가가되(빠르게) 한 단계 안에서도 조금씩 앞으로(목표의 98% 까지) — 긴 프레임 뒤에도 막대가 살아 있게
                shown = Mathf.MoveTowards(shown, target, dt * 1.6f);
                SetBar(shown);
                if (FadeLeft > 0)
                {
                    FadeLeft -= Time.unscaledDeltaTime;
                    group.alpha = Mathf.Clamp01(FadeLeft / Mathf.Max(0.01f, FadeDur));
                    if (FadeLeft <= 0) { canvas.enabled = false; group.alpha = 1; }
                }
            }
        }

        static void SetBar(float p)
        {
            p = Mathf.Clamp01(p);
            if (fill != null) fill.anchorMax = new Vector2(p, 1);
            if (pct != null) pct.text = Mathf.RoundToInt(p * 100) + "%";
        }

        static void Build()
        {
            var go = new GameObject("RunUI Loading", typeof(RectTransform));
            Object.DontDestroyOnLoad(go);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = Theme.Compact ? new Vector2(1280, 720) : new Vector2(1600, 900);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();   // 덮은 동안 아래를 누르지 못하게
            group = go.AddComponent<CanvasGroup>();
            pump = go.AddComponent<Pump>();
            var root = (RectTransform)go.transform;

            var bg = Ui.Img(root, Theme.White, new Color(0.02f, 0.015f, 0.04f, 1), "bg", true);
            bg.rectTransform.Fill();

            title = Ui.Title(root, "", 46, Theme.Gold, TextAlignmentOptions.Center, "title");
            title.rectTransform.At(0.5f, 0.5f, 0, 70, 900, 70);

            var well = Ui.Img(root, Theme.White, Theme.NavyWell, "bar").rectTransform;
            well.At(0.5f, 0.5f, 0, 0, 640, 14);
            var edge = well.gameObject.AddComponent<Outline>();
            edge.effectColor = Theme.Edge.A(0.55f);
            edge.effectDistance = new Vector2(1, -1);
            fill = Ui.Img(well, Theme.White, Theme.Gold, "fill").rectTransform;
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            pct = Ui.Text(root, "0%", 22, Theme.Sub, TextAlignmentOptions.Center, false, "pct");
            pct.rectTransform.At(0.5f, 0.5f, 0, -34, 300, 34);
            step = Ui.Text(root, "", 20, Theme.Dim, TextAlignmentOptions.Center, false, "step");
            step.rectTransform.At(0.5f, 0.5f, 0, -66, 900, 30);

            tip = Ui.Text(root, "", 24, Theme.Sub, TextAlignmentOptions.Center, false, "tip");
            tip.rectTransform.At(0.5f, 0f, 0, 120, 1180, 120);
            tip.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>덮는다(곧바로 불투명). 이미 떠 있으면 글만 바꾼다. tip 이 null 이면 키워드 풀이 하나를 고른다.</summary>
        public static void Show(string head, string tipText = null)
        {
            if (canvas == null) Build();
            canvas.enabled = true;
            group.alpha = 1;
            pump.FadeLeft = 0;
            title.text = head ?? "";
            if (tip.text.Length == 0 || tipText != null) tip.text = tipText ?? RandomTip();
            step.text = "";
            target = shown = 0;
            SetBar(0);
        }

        /// <summary>진행(0~1)과 지금 하는 일. 뒤로 가지는 않는다.</summary>
        public static void Progress(float p, string what = null)
        {
            if (canvas == null) return;
            target = Mathf.Max(target, Mathf.Clamp01(p));
            if (what != null && step != null) step.text = what;
        }

        /// <summary>걷는다(dur 초 동안 옅어짐, 0 이면 곧바로).</summary>
        public static void Hide(float dur = 0.25f)
        {
            if (canvas == null || !canvas.enabled) return;
            target = shown = 1;
            SetBar(1);
            if (dur <= 0) { canvas.enabled = false; tip.text = ""; return; }
            pump.FadeDur = pump.FadeLeft = dur;
            pump.StartCoroutine(ClearTipLater(dur));   // 다음 Show 가 새 팁을 고르게 걷힌 뒤 비운다
        }

        static IEnumerator ClearTipLater(float dur)
        {
            yield return new WaitForSecondsRealtime(dur + 0.05f);
            if (canvas != null && !canvas.enabled) tip.text = "";
        }

        /// <summary>알아 두기 — 엔진 키워드 풀이(CardText.TIPS) 하나.</summary>
        public static string RandomTip()
        {
            var all = Bolzena.Core.CardText.TIPS;
            if (all == null || all.Count == 0) return "";
            var kv = all.ElementAt(Random.Range(0, all.Count));
            return $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.Gold)}>알아 두기</color>  「{kv.Key}」 {kv.Value}";
        }
    }
}
