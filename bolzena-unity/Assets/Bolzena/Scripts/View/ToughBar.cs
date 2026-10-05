using Bolzena.UI;
using TMPro;
using UnityEngine;

namespace Bolzena.View
{
    // 강인도 막대 — 카제나 배치(HP 막대 바로 밑, 칸 눈금으로 나뉜 옅은 보라 막대) · 우리 그림.
    //   칸 수 = 최대 강인도. 깎이면 칸이 오른쪽부터 비고, 약점이 아닌 공격의 1/3 칸도 그대로 줄어든다(칸 속 1/3 눈금).
    //   겨누면 깎일 몫이 금빛으로 숨쉬고(격파까지 가면 테도 금빛), 격파되면 조각나며 회색 칸 + 금 테 깜빡임 + 「격파」.
    //   일어나면 왼쪽부터 다시 차오른다. 적 머리 위(EnemyHud)와 적 상세 창(InfoPanel)이 같이 쓴다.
    public class ToughBar : MonoBehaviour
    {
        public static readonly Color Lilac = new Color(0.84f, 0.76f, 1f);          // #D6C2FF — 찬 칸(밝은 보라)
        static readonly Color SlotC = new Color(0.20f, 0.21f, 0.25f, 0.98f);       // 빈 칸(어두운 회색 — 찬 칸과 대비를 크게)
        static readonly Color FrameC = new Color(0.62f, 0.58f, 0.78f, 0.95f);      // 칸 테두리
        static readonly Color ShineC = new Color(1f, 1f, 1f, 0.38f);               // 찬 칸 위쪽 반짝 띠
        static readonly Color LagC = new Color(1f, 1f, 1f, 0.92f);                 // 막 깎인 자리(흰 꼬리)
        static readonly Color GhostC = new Color(1f, 0.86f, 0.35f);                // 겨눔 — 깎일 몫
        static readonly Color BrokenC = new Color(0.33f, 0.34f, 0.40f, 0.96f);     // 격파 — 회색 칸
        static readonly Color TickC = new Color(0.10f, 0.07f, 0.22f, 0.55f);       // 1/3 눈금
        const float Gap = 0.026f;   // 칸 사이 어두운 틈(3440×1440 에서 4px 남짓)

        float w, h, cw, ins;
        int n;
        public float Max { get; private set; }
        public int Cells => n;
        public float Value => target;
        float target, shown, lag, lagHold, pv, breakT = -1;
        bool pvBreak, broken;
        SpriteRenderer back, rim;
        SpriteRenderer[] frame, slot, lagR, fill, ghost, shine;
        TextMeshPro label;
        Vector3 home;
        public Transform FxSpace;   // 조각을 뿌릴 공간(싸움터 뿌리) — 없으면 조각 없이 번쩍 · 흔들림만

        public static ToughBar Create(Transform parent, Vector3 center, float width, float height, float max, int order, bool showLabel = true)
        {
            var t = Make.Node("tough", parent, center);
            var b = t.gameObject.AddComponent<ToughBar>();
            b.home = center;
            b.w = width;
            b.h = height;
            b.Max = Mathf.Max(0, max);
            b.n = Mathf.Max(1, Mathf.CeilToInt(b.Max - 0.001f));
            b.cw = (width - Gap * (b.n - 1)) / b.n;
            b.ins = Mathf.Max(0.008f, height * 0.13f);   // 칸 테두리 두께
            b.target = b.shown = b.lag = b.Max;
            var white = Res.UI("white");
            b.rim = Make.Box("rim", t, white, Vector3.zero, new Vector2(width + 0.05f, height + 0.05f), order, new Color(Tone.Gold.r, Tone.Gold.g, Tone.Gold.b, 0));
            b.back = Make.Box("back", t, white, Vector3.zero, new Vector2(width + 0.026f, height + 0.026f), order + 1, new Color(0.01f, 0.01f, 0.03f, 0.96f));
            b.frame = new SpriteRenderer[b.n];
            b.shine = new SpriteRenderer[b.n];
            b.slot = new SpriteRenderer[b.n];
            b.lagR = new SpriteRenderer[b.n];
            b.fill = new SpriteRenderer[b.n];
            b.ghost = new SpriteRenderer[b.n];
            for (int i = 0; i < b.n; i++)
            {
                float cap = b.Cap(i);
                b.frame[i] = Make.Box("frame" + i, t, white, new Vector3(b.Left(i) + b.cw * cap / 2, 0, 0), new Vector2(b.cw * cap, height), order + 1, FrameC);
                b.slot[i] = Make.Box("slot" + i, t, white, new Vector3(b.Left(i) + b.cw * cap / 2, 0, 0), new Vector2(b.cw * cap - b.ins * 2, height - b.ins * 2), order + 2, SlotC);
                b.shine[i] = Make.Box("shine" + i, t, white, Vector3.zero, new Vector2(0.01f, height), order + 5, ShineC);
                b.lagR[i] = Make.Box("lag" + i, t, white, Vector3.zero, new Vector2(0.01f, height), order + 3, LagC);
                b.fill[i] = Make.Box("fill" + i, t, white, Vector3.zero, new Vector2(0.01f, height), order + 4, Lilac);
                b.ghost[i] = Make.Box("ghost" + i, t, white, Vector3.zero, new Vector2(0.01f, height), order + 5, GhostC);
                b.ghost[i].enabled = false;
                // 1/3 눈금 — 약점이 아닌 공격 한 번이 깎는 몫
                if (b.cw > 0.12f)
                    for (int k = 1; k < 3; k++)
                        if (k / 3f < cap - 0.01f)
                            Make.Box("tick" + i + "_" + k, t, white, new Vector3(b.Left(i) + b.ins + (b.cw - b.ins * 2) * k / 3f, 0, 0), new Vector2(Mathf.Max(0.005f, height * 0.09f), (height - b.ins * 2) * 0.5f), order + 6, TickC);
            }
            if (showLabel)
            {
                b.label = Make.Text("label", t, "격파", new Vector3(0, -0.004f, 0), Mathf.Max(0.12f, height * 2.3f), order + 7, Tone.Gold);
                Make.Outline(b.label, 0.35f, new Color(0.18f, 0.09f, 0f, 1f));
                b.label.enabled = false;
            }
            b.Paint();
            return b;
        }

        float Left(int i) => -w / 2 + i * (cw + Gap);
        float Cap(int i) => Mathf.Clamp01(Max - i);

        /// <summary>칸 i 의 frac 자리(0 왼쪽 끝 ~ 1 오른쪽 끝) — 월드 좌표.</summary>
        public Vector3 CellPos(int i, float frac) => transform.TransformPoint(new Vector3(Left(Mathf.Clamp(i, 0, n - 1)) + cw * frac, 0, 0));

        /// <summary>남은 강인도. 줄면 바로 줄고 흰 꼬리가 뒤따른다. 늘면(일어남) 왼쪽부터 차오른다.</summary>
        public void Set(float v, bool hit)
        {
            v = Mathf.Clamp(v, 0, Max);
            if (v < target - 0.0001f)
            {
                if (hit) lagHold = 0.35f; else lag = v;
                if (v < shown) shown = v;
            }
            target = v;
            Paint();
        }

        /// <summary>겨눔 — 깎일 몫(v)을 막대 끝에서 빛낸다. brk 면 격파까지 — 테도 금빛.</summary>
        public void Preview(float v, bool brk)
        {
            pv = Mathf.Max(0, v);
            pvBreak = brk;
            Paint();
        }

        /// <summary>격파 — fx 면 칸이 하얗게 번쩍 · 흔들리며 회색으로 깨진다. 풀리면(false) 다음 Set 에서 0 부터 차오른다.</summary>
        public void SetBroken(bool b, bool fx)
        {
            if (b && fx && !broken)
            {
                breakT = 0;
                for (int i = 0; i < n; i++)
                {
                    if (FxSpace == null) break;
                    var at = FxSpace.InverseTransformPoint(CellPos(i, Cap(i) / 2));
                    Vfx.Burst(at, new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Fragment_01", Count = 6, Speed = new Vector2(1.5f, 4f), Life = new Vector2(0.3f, 0.6f),
                        Size = new Vector2(0.05f, 0.11f), C0 = Lilac, C1 = new Color(1f, 0.85f, 0.4f), Gravity = 1.4f, Spin = true, Order = slot[0].sortingOrder + 8, Boost = 2.2f,
                    });
                }
            }
            if (b) { shown = lag = target = 0; }
            broken = b;
            if (label != null) label.enabled = b;
            Paint();
        }

        // 칸 i 의 [a, b] 몫(0~1) — 테두리 안쪽에 그린다. hh · y 는 안쪽 높이에 대한 비율 · 자리(반짝 띠)
        void Seg(SpriteRenderer r, int i, float a, float b, float hh = 1, float y = 0)
        {
            if (b - a <= 0.002f) { r.enabled = false; return; }
            r.enabled = true;
            float iw = cw - ins * 2, ih = h - ins * 2;
            Make.Fit(r, new Vector2(iw * (b - a), ih * hh));
            r.transform.localPosition = new Vector3(Left(i) + ins + iw * (a + b) / 2, ih * y, 0);
        }

        void Paint()
        {
            if (slot == null) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Clock.Now * 11f);
            for (int i = 0; i < n; i++)
            {
                float cap = Cap(i);
                Seg(fill[i], i, 0, Mathf.Min(cap, Mathf.Clamp01(shown - i)));
                Seg(shine[i], i, 0, Mathf.Min(cap, Mathf.Clamp01(shown - i)), 0.3f, 0.32f);
                Seg(lagR[i], i, 0, Mathf.Min(cap, Mathf.Clamp01(lag - i)));
                if (pv > 0.001f && !broken)
                {
                    Seg(ghost[i], i, Mathf.Clamp01(shown - pv - i), Mathf.Min(cap, Mathf.Clamp01(shown - i)));
                    ghost[i].color = Color.Lerp(GhostC, Color.white, 0.45f * pulse);
                }
                else ghost[i].enabled = false;
                var sc = broken ? BrokenC : SlotC;
                if (breakT >= 0 && breakT < 0.5f) sc = Color.Lerp(Color.white, BrokenC, breakT / 0.5f);
                slot[i].color = sc;
                frame[i].color = broken ? new Color(0.45f, 0.45f, 0.5f, 0.95f) : FrameC;
            }
            // 테 — 격파 중 금빛 깜빡임, 겨눔이 격파까지 가면 금빛 숨
            float ra = broken ? 0.25f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Clock.Now * 6f)) : pvBreak && pv > 0 ? 0.35f + 0.5f * pulse : 0;
            rim.color = new Color(Tone.Gold.r, Tone.Gold.g, Tone.Gold.b, ra);
            if (label != null && broken) label.alpha = 0.75f + 0.25f * Mathf.Sin(Clock.Now * 6f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (shown < target) { shown = Mathf.MoveTowards(shown, target, dt * Mathf.Max(3f, Max * 1.6f)); lag = shown; }
            if (lagHold > 0) lagHold -= dt;
            else lag = Mathf.MoveTowards(lag, shown, dt * 2.2f);
            if (lag < shown) lag = shown;
            if (breakT >= 0)
            {
                breakT += dt;
                float k = Mathf.Clamp01(breakT / 0.4f);
                transform.localPosition = home + new Vector3(Random.Range(-1f, 1f) * 0.035f * (1 - k), Random.Range(-1f, 1f) * 0.02f * (1 - k), 0);
                if (breakT > 0.6f) { breakT = -1; transform.localPosition = home; }
            }
            Paint();
        }
    }
}
