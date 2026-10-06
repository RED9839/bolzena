using System.Collections.Generic;
using Bolzena.View;
using UnityEngine;

namespace Bolzena.UI
{
    // 대상 지정 화살표 — 카드에서 적으로 휘는 곡선 위에 갈매기 표가 흘러가고, 끝에 화살촉
    public class TargetArrow : MonoBehaviour
    {
        readonly List<SpriteRenderer> chevrons = new List<SpriteRenderer>();
        SpriteRenderer head;
        const int N = 16;

        public static TargetArrow Create(Transform parent)
        {
            var root = Make.Node("Arrow", parent);
            var a = root.gameObject.AddComponent<TargetArrow>();
            for (int i = 0; i < N; i++)
                a.chevrons.Add(Make.Box("c" + i, root, Res.UI("chevron"), Vector3.zero, new Vector2(0.32f, 0.32f), 680, Color.white, Res.SpriteMat(false, 1.5f)));
            a.head = Make.Box("head", root, Res.UI("arrowhead"), Vector3.zero, new Vector2(0.62f, 0.62f), 681, Color.white, Res.SpriteMat(false, 1.8f));
            a.Hide();
            return a;
        }

        public void Hide()
        {
            foreach (var c in chevrons) c.enabled = false;
            head.enabled = false;
        }

        static Vector3 Bez(Vector3 a, Vector3 b, Vector3 c, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;

        public void Show(Vector3 from, Vector3 to, bool locked) => Show(from, to, locked, null);

        /// <summary>tint 를 주면 그 빛깔의 호(겨누면 짙게, 아니면 옅게) — 끌기(적 붉게 · 아군 초록 파랑). 호는 위로 볼록하다.</summary>
        public void Show(Vector3 from, Vector3 to, bool locked, Color? tint)
        {
            var mid = (from + to) / 2 + new Vector3(0, 1.6f + Vector3.Distance(from, to) * 0.12f, 0);
            var col = tint.HasValue ? (locked ? tint.Value : Color.Lerp(tint.Value, Color.white, 0.35f))
                    : locked ? new Color(1f, 0.38f, 0.3f) : new Color(1f, 0.95f, 0.85f);
            float flow = (Clock.Now * 1.6f) % 1f;
            for (int i = 0; i < N; i++)
            {
                float t = (i + flow) / (N + 1);
                var p = Bez(from, mid, to, t);
                var p2 = Bez(from, mid, to, Mathf.Min(1, t + 0.01f));
                var d = p2 - p;
                var c = chevrons[i];
                c.enabled = true;
                c.transform.position = p;
                c.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                float s = Mathf.Lerp(0.22f, 0.4f, t);
                Make.Fit(c, new Vector2(s, s));
                var cc = col;
                cc.a = Mathf.Clamp01(t * 3f) * 0.95f;
                c.color = cc;
            }
            var e = to - Bez(from, mid, to, 0.96f);
            head.enabled = true;
            head.transform.position = to;
            head.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(e.y, e.x) * Mathf.Rad2Deg);
            head.color = col;
        }
    }
}
