using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 사도 목록 상반신 — 정지 그림(2026-10 사용자: 「목록은 움직이지 않는 그림으로」). 스크롤 창에 들어온 칸만 스탠딩을 표대로 구워(StandingSnap)
    //   Still 에 넣는다 — 135칸을 한꺼번에 굽지 않게 한 프레임에 둘까지만. 한 번 구운 그림은 StandingSnap 이 붙잡아 두어 다시 그려도 바로 뜬다.
    //   스파인 · 표가 없는 사도는 Fallback(웹판 정지 렌더 자르기)을 그대로 둔다.
    public class LiveStanding : MonoBehaviour
    {
        public string Art;
        public float W, H, Frac = 0.5f;
        public Image Still;
        public Sprite Fallback;
        public Color Tint = Color.white;
        RectTransform view;
        bool done;
        float next;
        static int madeFrame = -1, madeCount;
        const int PerFrame = 2;

        void Start()
        {
            var sr = GetComponentInParent<ScrollRect>();
            view = sr != null ? (sr.viewport != null ? sr.viewport : (RectTransform)sr.transform) : null;
            next = 0;
        }

        bool Visible()
        {
            if (view == null) return true;
            var a = World((RectTransform)transform);
            var b = World(view);
            float pad = a.height;   // 위아래로 한 칸 넉넉히(스크롤로 막 들어오는 칸을 미리)
            return a.xMax > b.xMin && a.xMin < b.xMax && a.yMax > b.yMin - pad && a.yMin < b.yMax + pad;
        }

        static readonly Vector3[] c4 = new Vector3[4];
        static Rect World(RectTransform rt)
        {
            rt.GetWorldCorners(c4);
            return Rect.MinMaxRect(c4[0].x, c4[0].y, c4[2].x, c4[2].y);
        }

        void Update()
        {
            if (done) return;
            next -= Time.unscaledDeltaTime;
            if (next > 0) return;
            next = 0.1f;
            if (!Visible()) return;
            if (madeFrame != Time.frameCount) { madeFrame = Time.frameCount; madeCount = 0; }
            if (madeCount >= PerFrame) { next = 0; return; }
            madeCount++;
            done = true;
            var cv = GetComponentInParent<Canvas>();
            float sf = cv != null ? cv.rootCanvas.scaleFactor : 1f;
            int px = Mathf.Clamp(Mathf.RoundToInt(H * sf * 1.1f), 96, 1024);
            var sp = StandingSnap.Upper(Art, W / H, Frac, px);
            if (Still == null) return;
            Still.sprite = sp != null ? sp : Fallback;
            Still.color = Still.sprite != null ? Tint : Color.clear;
        }
    }
}
