using UnityEngine;

namespace Bolzena.View
{
    // 낱장 넘기기(플립북) — 다 돌면 사라진다. 시간은 Time.deltaTime(히트스톱이면 같이 멈춘다)
    public class SheetAnim : MonoBehaviour
    {
        public Sprite[] Frames;
        public float Fps = 30f;
        public bool Loop;
        public float FadeTail = 0.25f;     // 끝 몇 %에서 흐려지기
        public int StartFrame = 1;         // 원작 낱장은 첫 칸이 비어 있다 — 멈칫 때 빈 칸에 멈추지 않게
        SpriteRenderer sr;
        float t;
        Color baseColor;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            baseColor = sr.color;
            t = StartFrame / Mathf.Max(1f, Fps);
            if (Frames != null && Frames.Length > 0) sr.sprite = Frames[0];
        }

        void Update()
        {
            if (Frames == null || Frames.Length == 0) return;
            t += Time.deltaTime;
            int n = Frames.Length;
            int i = Mathf.FloorToInt(t * Fps);
            if (!Loop && i >= n) { Destroy(gameObject); return; }
            sr.sprite = Frames[i % n];
            if (!Loop && FadeTail > 0)
            {
                float k = (t * Fps) / n;
                float a = k < 1 - FadeTail ? 1 : Mathf.Clamp01((1 - k) / FadeTail);
                var c = baseColor;
                c.a *= a;
                sr.color = c;
            }
        }
    }
}
