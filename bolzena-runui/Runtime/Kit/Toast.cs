using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 짧은 알림 — 화면 위쪽 가운데에 떴다가 사라진다(엔진이 돌려준 까닭 글 · 「조용한 길이었습니다」 따위).
    // 머리 띠(HUD)가 있는 화면에서는 머리 띠 가운데(화면 이름 자리)에 겹쳐 뜨고 그동안 화면 이름을 숨긴다 —
    // 머리 띠 바로 아래(y −112)는 이야기판 · 상점 진열 · 지도 칸이 쓰는 자리라 글을 가렸다. 머리 띠가 없는 화면은 그 아래 그대로.
    public static class Toast
    {
        public static RectTransform Layer;
        /// <summary>지금 화면의 머리 띠(W.StatusBar 가 건다 — 화면이 바뀌면 함께 사라진다).</summary>
        public static RectTransform Hud;
        /// <summary>머리 띠 가운데 화면 이름 — 알림이 떠 있는 동안 숨긴다.</summary>
        public static RectTransform HudTitle;
        static RectTransform cur;
        /// <summary>머리 띠 자리에 알림이 떠 있다.</summary>
        public static bool Showing => cur != null && cur.anchoredPosition.y > -60;

        public static void Show(string msg, float sec = 2.2f)
        {
            if (Layer == null || string.IsNullOrEmpty(msg)) { Debug.Log("[Toast] " + msg); return; }
            if (cur != null) Object.Destroy(cur.gameObject);
            bool hud = Hud != null && Hud.gameObject.activeInHierarchy;
            // 머리 띠 자리 — 왼쪽 파티 알약(끝 x≈442)과 오른쪽 칩(시작 x≈W−290) 사이에 들어가는 폭
            float maxW = hud ? Mathf.Max(420, Layer.rect.width - 2 * 456) : 1300;
            var bg = Ui.Img(Layer, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.97f), "toast");
            var rt = bg.rectTransform;
            rt.At(0.5f, 1, 0, hud ? -14 : -112, 640, 56);
            var ring = Ui.Img(rt, Theme.S("pill", 46), Theme.Gold.A(0.0f), "rim");
            ring.rectTransform.Fill();
            var t = Ui.Title(rt, msg, Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
            t.rectTransform.Fill(24, 4, 24, 4);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            float w = Mathf.Clamp(t.preferredWidth + 64, 260, maxW);
            if (t.preferredWidth + 64 > maxW) { t.enableAutoSizing = true; t.fontSizeMin = Theme.FsSm - 2; t.fontSizeMax = Theme.FsMd; }
            rt.sizeDelta = new Vector2(w, 56);
            cur = rt;
            Tw.Rise(rt, 0, hud ? 12 : 20, 0.3f, Vector2.up);
            var g = rt.Group();
            if (hud && HudTitle != null) { var tg0 = HudTitle.Group(); Tw.Run(HudTitle, 0.15f, k => { if (tg0) tg0.alpha = Mathf.Min(tg0.alpha, 1 - k); }); }
            Tw.After(sec, () =>
            {
                if (!rt) return;
                Tw.Run(rt, 0.3f, k =>
                {
                    if (g) g.alpha = 1 - k;
                    if (cur == rt && HudTitle != null) { var tg = HudTitle.Group(); tg.alpha = Mathf.Max(tg.alpha, k); }   // 마지막 알림이 걷힐 때 화면 이름을 다시
                }, Tw.Linear, 0, () => { if (rt) Object.Destroy(rt.gameObject); });
            });
            Debug.Log("[Toast] " + msg);
        }
    }
}
