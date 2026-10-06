using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 고정 캔버스 단위로 지은 판(창 · 공개 판)을 화면(캔버스 Stage.Size)에 비례하게 — PC(높이 900)에서 보이는 몫을 그대로 지키도록 줄인다.
    ///   폰 · 작은 창(Compact)은 캔버스가 1280×720 이라 같은 단위 판이 화면을 거의 다 덮었다(2026-10-06 「편성 화면 적 클론 창이 해상도에 따라 안 작아짐」 —
    ///   로비 스탠딩과 같은 원인: 고정 캔버스 단위 + Compact 에서 캔버스 높이 720).
    ///   판을 꽉 찬 받침(fit) 안으로 옮겨 받침을 줄인다 — 판에 걸린 등장 연출(Tw.Pop 이 판의 localScale 을 직접 만짐)과 겹치지 않는다.
    /// </summary>
    public static class FitScale
    {
        /// <summary>판(panel, 크기 w×h 단위)이 캔버스 높이의 shareH · 너비의 shareW 를 넘지 않게 줄인다(키우지는 않음). 돌려줌: 배율.</summary>
        public static float Fit(RectTransform panel, Vector2 canvas, float w, float h, float shareH, float shareW = 0.92f)
        {
            if (panel == null || panel.parent == null || canvas.y < 100) return 1;
            float K(Vector2 c) => Mathf.Min(1f, c.y * shareH / Mathf.Max(1, h), c.x * shareW / Mathf.Max(1, w));
            float k = K(canvas);
            // 받침은 늘 만든다 — 창 크기가 바뀌면(전체 화면 전환 등) 지금 캔버스로 배율만 다시 건다(2026-10-07)
            var fit = Ui.Rect("fit", panel.parent).Fill();
            fit.SetSiblingIndex(panel.GetSiblingIndex());
            fit.localScale = new Vector3(k, k, 1);
            panel.SetParent(fit, false);
            var st = Stage.Main;
            if (st != null) st.WhenResized(fit, () => { if (!fit) return; float k2 = K(st.Size); fit.localScale = new Vector3(k2, k2, 1); });
            return k;
        }
    }
}
