using System.Collections;
using Bolzena.View;
using UnityEngine;

namespace Bolzena.UI
{
    // 16:9 로 만든 화면 크기 조각(테두리 빛 · 집중선)을 지금 화면(21:9 · 16:10 …)에 맞춰 늘린다
    public class ScreenFit : MonoBehaviour
    {
        Vector3 s0;
        public static void Add(Transform t)
        {
            var f = t.gameObject.AddComponent<ScreenFit>();
            f.s0 = t.localScale;
            f.LateUpdate();
        }
        void LateUpdate()
        {
            float kx = (Tone.HalfW * 2 + 0.4f) / 16.4f, ky = (Tone.HalfH * 2 + 0.4f) / 9.4f;
            transform.localScale = new Vector3(s0.x * kx, s0.y * ky, s0.z);
        }
    }

    // 화면 덮개 — 어둡게 · 검은 페이드 · 집중선 · 맞았을 때 붉은 테두리 · 위아래 검은 띠(영화 띠)
    public class ScreenFx : MonoBehaviour
    {
        public static ScreenFx I;
        SpriteRenderer fieldDim, fade, hurt, barTop, barBot;
        MeshRenderer lines;
        public Material LinesMat;
        float dimA, dimTo, fadeA, fadeTo, hurtA, barK, barTo, linesA, linesTo;
        public float DimSpeed = 6f, FadeSpeed = 3f;
        public static readonly Vector2 Size = new Vector2(16f, 9f);

        public static ScreenFx Create(Transform parent)
        {
            var t = Make.Node("ScreenFx", parent);
            var s = t.gameObject.AddComponent<ScreenFx>();
            I = s;
            var big = new Vector2(44, 16);
            s.fieldDim = Make.Box("dim", t, Res.UI("white"), Vector3.zero, big, 300, new Color(0.02f, 0.02f, 0.05f, 0));
            s.hurt = Make.Box("hurt", t, Res.UI("vignette"), Vector3.zero, new Vector2(16.4f, 9.4f), 455, new Color(0.9f, 0.05f, 0.05f, 0));
            ScreenFit.Add(s.hurt.transform);
            s.LinesMat = Res.NewMat("Bolzena/FocusLines");
            s.lines = Make.Quad("lines", t, Vector3.zero, new Vector2(16.4f, 9.4f), s.LinesMat, 302);
            Make.Own(s.lines.gameObject, s.LinesMat);
            ScreenFit.Add(s.lines.transform);
            s.LinesMat.SetFloat("_Alpha", 0);
            s.barTop = Make.Box("barTop", t, Res.UI("white"), new Vector3(0, 5.2f, 0), new Vector2(44, 1.2f), 303, Color.black);
            s.barBot = Make.Box("barBot", t, Res.UI("white"), new Vector3(0, -5.2f, 0), new Vector2(44, 1.2f), 303, Color.black);
            s.fade = Make.Box("fade", t, Res.UI("white"), Vector3.zero, big, 1000, new Color(0, 0, 0, 1));
            s.fadeA = s.fadeTo = 1;
            return s;
        }

        public void Dim(float a, int order = 300, float speed = 6f) { dimTo = a; fieldDim.sortingOrder = order; DimSpeed = speed; }
        public void Fade(float a, float speed = 3f) { fadeTo = a; FadeSpeed = speed; }
        public void Hurt(float a) { hurtA = Mathf.Max(hurtA, a * 0.55f); }
        public void Bars(bool on) { barTo = on ? 1 : 0; }

        // 집중선 — center 는 화면 월드 좌표
        public void Lines(float a, Color c, Vector2 center, int order = 302, float inner = 0.28f)
        {
            linesTo = a;
            LinesMat.SetColor("_Color", c);
            LinesMat.SetVector("_Center", new Vector4(center.x / Size.x + 0.5f, center.y / Size.y + 0.5f, 0, 0));
            LinesMat.SetFloat("_Inner", inner);
            lines.sortingOrder = order;
        }

        public IEnumerator FadeTo(float a, float dur)
        {
            fadeTo = a;
            float from = fadeA;
            yield return Clock.Tween(dur, t => fadeA = Mathf.Lerp(from, a, t), true);
            fadeA = a;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            dimA = Mathf.MoveTowards(dimA, dimTo, dt * DimSpeed);
            fadeA = Mathf.MoveTowards(fadeA, fadeTo, dt * FadeSpeed);
            hurtA = Mathf.MoveTowards(hurtA, 0, dt * 1.6f);
            barK = Mathf.MoveTowards(barK, barTo, dt * 5f);
            linesA = Mathf.MoveTowards(linesA, linesTo, dt * 8f);
            Make.Alpha(fieldDim, dimA);
            Make.Alpha(fade, fadeA);
            Make.Alpha(hurt, hurtA);
            LinesMat.SetFloat("_Alpha", linesA);
            lines.enabled = linesA > 0.001f;
            float k = Ease.OutCubic(barK);
            float hh = Tone.HalfH;   // 16:10 처럼 키가 큰 화면에서도 끝에 붙게
            barTop.transform.localPosition = new Vector3(0, hh + 0.6f - 0.75f * k, 0);
            barBot.transform.localPosition = new Vector3(0, -hh - 0.6f + 0.75f * k, 0);
        }
    }
}
