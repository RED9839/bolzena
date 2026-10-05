using System.Collections;
using System.Collections.Generic;
using Bolzena.View;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 고학년 컷인 — 화면이 어두워지고 집중선, 비스듬한 띠가 쏘아 들어오고 그 안으로 스탠딩(상반신)이 미끄러져 들어온다.
    // 기술 이름 · 사도 이름이 찍히고, 끝에 흰 번쩍임과 함께 띠가 튕겨 나간다. 시간은 화면 시간(멈칫과 무관).
    public static class UltCutin
    {
        const float Angle = 9f;
        const int O = 700;
        public static System.Action<string> OnStage;      // 단계마다(자동 데모가 캡처를 건다)

        public static IEnumerator Play(Transform parent, string hero, string heroName, string ultName, Color tint)
        {
            var root = Make.Node("Cutin", parent);
            var fx = ScreenFx.I;
            Sfx.Play("ult_cutin", 0.9f);
            Sfx.Voice(hero, "ultimate", "shout");

            // 1) 어둡게 + 집중선
            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(44, 16), O, new Color(0.02f, 0.01f, 0.05f, 0));
            var linesMat = Res.NewMat("Bolzena/FocusLines");
            linesMat.SetColor("_Color", Color.Lerp(tint, Color.white, 0.6f));
            linesMat.SetVector("_Center", new Vector4(0.42f, 0.55f, 0, 0));
            linesMat.SetFloat("_Inner", 0.3f);
            linesMat.SetFloat("_Density", 140);
            linesMat.SetFloat("_Boost", 1.4f);
            var lines = Make.Quad("lines", root, Vector3.zero, new Vector2(16.4f, 9.4f), linesMat, O + 1);
            ScreenFit.Add(lines.transform);
            fx.Bars(true);
            yield return Clock.Tween(0.14f, t =>
            {
                Make.Alpha(dim, 0.8f * t);
                linesMat.SetFloat("_Alpha", 0.75f * t);
            }, true);
            OnStage?.Invoke("ult1_dim");

            // 2) 띠 — 바탕(사도 빛깔) · 흰 줄 둘
            var band = Make.Node("band", root, new Vector3(0, 0.35f, 0));
            band.localRotation = Quaternion.Euler(0, 0, Angle);
            var bandBack = Make.Box("back", band, Res.UI("white"), new Vector3(-26, 0, 0), new Vector2(26, 3.6f), O + 2, Color.Lerp(tint, Color.black, 0.35f));
            var bandGlow = Make.Box("glow", band, Res.UI("soft"), new Vector3(-26, 0, 0), new Vector2(26, 5.5f), O + 3, new Color(tint.r, tint.g, tint.b, 0.55f), Res.SpriteMat(true, 1.4f));
            var lineA = Make.Box("lineA", band, Res.UI("white"), new Vector3(26, 1.95f, 0), new Vector2(26, 0.12f), O + 4, Color.white, Res.SpriteMat(false, 2.2f));
            var lineB = Make.Box("lineB", band, Res.UI("white"), new Vector3(26, -1.95f, 0), new Vector2(26, 0.07f), O + 4, Color.white, Res.SpriteMat(false, 2.2f));
            // 띠 안을 흐르는 빛줄
            var streaks = new List<SpriteRenderer>();
            for (int i = 0; i < 14; i++)
            {
                var s = Make.Box("streak", band, Res.FxTex("FX_UI_light_line_Blur"), new Vector3(Random.Range(-9f, 9f), Random.Range(-1.6f, 1.6f), 0),
                    new Vector2(Random.Range(2f, 5f), Random.Range(0.06f, 0.18f)), O + 5, new Color(1, 1, 1, Random.Range(0.25f, 0.6f)), Res.SpriteMat(true, 1.8f));
                streaks.Add(s);
            }
            // 띠 모양 마스크 — 스탠딩은 띠 안에서만 보인다(SpriteMask + 스파인 maskInteraction)
            var maskT = Make.Node("mask", band);
            var mask = maskT.gameObject.AddComponent<SpriteMask>();
            mask.sprite = Res.UI("white");
            maskT.localScale = new Vector3(40f / Res.UI("white").bounds.size.x, 3.6f / Res.UI("white").bounds.size.y, 1);
            yield return Clock.Tween(0.16f, t =>
            {
                float k = Ease.OutExpo(t);
                bandBack.transform.localPosition = new Vector3(Mathf.Lerp(-26, 0, k), 0, 0);
                bandGlow.transform.localPosition = new Vector3(Mathf.Lerp(-26, 0, k), 0, 0);
                lineA.transform.localPosition = new Vector3(Mathf.Lerp(26, 0, k), 1.95f, 0);
                lineB.transform.localPosition = new Vector3(Mathf.Lerp(26, 0, k), -1.95f, 0);
            }, true);
            Vfx.Flash(new Color(1, 1, 1, 0.3f), 0.16f, 1f, O + 20);

            // 3) 스탠딩
            SkeletonAnimation st = null;
            // 스탠딩이 없는 사도(원작 스탠딩은 무거워 시범 여섯 · 판 화면 NPC 만 옮겼다)는 초상으로 대신한다
            var data = Res.Spine("st_" + hero);
            bool standing = data != null;
            Transform stRoot = Make.Node("standing", band, new Vector3(9, 0, 0));
            stRoot.localRotation = Quaternion.Euler(0, 0, -Angle);
            if (data == null)
            {
                var face = Res.Sprite("Art/" + hero);
                if (face != null)
                {
                    var fs = Make.Box("face", stRoot, face, new Vector3(0, 0.1f, 0), new Vector2(3.4f, 3.4f), O + 8);
                    fs.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
            else
            {
                st = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                st.transform.SetParent(stRoot, false);
                var sd = data.GetSkeletonData(true);
                var wear = Bolzena.Fx.SpineMotion.Wear(sd, null, standing);
                if (wear != null) { st.Skeleton.SetSkin(wear); st.Skeleton.SetSlotsToSetupPose(); }
                string anim = null;
                foreach (var pre in new[] { "Angry_1", "Serious_1", "Pride_1", "Mad_1", "Happy_1", "Idle_1" })
                    if (sd.FindAnimation(pre) != null) { anim = pre; break; }
                if (anim != null) st.AnimationState.SetAnimation(0, anim, true);
                st.AnimationState.Apply(st.Skeleton);
                st.Skeleton.UpdateWorldTransform();
                float bx, by, bw, bh; float[] buf = null;
                st.Skeleton.GetBounds(out bx, out by, out bw, out bh, ref buf);
                bh = Mathf.Max(1f, bh);
                float scale = 10.5f / bh;             // 상반신이 띠를 꽉 채우게
                st.transform.localScale = new Vector3(scale, scale, 1);
                // 얼굴(높이 80%께)이 띠 가운데 조금 위에 오게
                st.transform.localPosition = new Vector3(-(bx + bw / 2) * scale, -(by + bh * 0.8f) * scale + 0.25f, 0);
                st.GetComponent<MeshRenderer>().sortingOrder = O + 8;
                st.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
            // 스탠딩 뒤 빛 · 그림자 실루엣
            var halo = Make.Box("halo", band, Res.UI("soft"), new Vector3(-2.2f, 0.2f, 0), new Vector2(7, 7), O + 6, new Color(tint.r, tint.g, tint.b, 0.0f), Res.SpriteMat(true, 2.2f));
            halo.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            // 4) 글 — 「고학년 스킬」 · 기술 이름 · 사도 이름
            var txtRoot = Make.Node("text", root, new Vector3(2.9f, 0.55f, 0));
            txtRoot.localRotation = Quaternion.Euler(0, 0, Angle);
            var small = Make.Text("label", txtRoot, "고학년 스킬", new Vector3(0, 0.95f, 0), 0.3f, O + 14, new Color(1f, 0.92f, 0.7f));
            Make.Outline(small, 0.25f, new Color(0, 0, 0, 0.9f));
            small.characterSpacing = 18;
            var big = Make.Text("ult", txtRoot, ultName, new Vector3(0, 0.05f, 0), 0.95f, O + 15, Color.white);
            big.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.85f, 0.5f));
            Make.Outline(big, 0.18f, Color.Lerp(tint, Color.black, 0.6f));
            Make.Glow(big, Color.white, 1.35f);
            var name = Make.Text("hero", txtRoot, heroName, new Vector3(1.6f, -0.8f, 0), 0.42f, O + 14, Color.Lerp(tint, Color.white, 0.4f), TextAlignmentOptions.Right);
            Make.Outline(name, 0.25f, new Color(0, 0, 0, 0.9f));
            foreach (var t in new[] { small, big, name }) t.alpha = 0;

            float stIn = 0.22f;
            float tTotal = 1.35f;
            float time = 0;
            bool shotName = false, shotSlide = false;
            Vector3 textFrom = new Vector3(5.5f, 0.55f, 0), textTo = new Vector3(2.9f, 0.55f, 0);
            while (time < tTotal)
            {
                float dt = Time.unscaledDeltaTime;
                time += dt;
                // 스탠딩 — 오른쪽에서 미끄러져 들어와 천천히 흐른다
                float k = Mathf.Clamp01(time / stIn);
                float x = Mathf.LerpUnclamped(9f, -2.3f, Ease.OutBack(k, 1.2f)) - 0.35f * Mathf.Max(0, time - stIn);
                stRoot.localPosition = new Vector3(x, 0, 0);
                Make.Alpha(halo, 0.6f * k);
                halo.transform.localPosition = new Vector3(x + 0.1f, 0.2f, 0);
                // 띠 안 빛줄 흐름
                foreach (var s in streaks)
                {
                    var p = s.transform.localPosition;
                    p.x -= dt * 26f;
                    if (p.x < -12) p.x += 24;
                    s.transform.localPosition = p;
                }
                // 글 — 0.18초부터
                float tk = Mathf.Clamp01((time - 0.18f) / 0.22f);
                txtRoot.localPosition = Vector3.LerpUnclamped(textFrom, textTo, Ease.OutBack(tk, 1.5f));
                big.characterSpacing = Mathf.Lerp(60, 2, Ease.OutCubic(tk));
                foreach (var tx in new[] { small, big, name }) tx.alpha = tk;
                if (!shotSlide && time > stIn * 0.6f) { shotSlide = true; OnStage?.Invoke("ult2_slide"); }
                if (!shotName && time > 0.55f) { shotName = true; OnStage?.Invoke("ult3_name"); }
                yield return null;
            }

            // 5) 끝 — 흰 번쩍임, 띠가 위아래로 갈라져 사라진다
            Vfx.Flash(new Color(1, 1, 1, 0.6f), 0.3f, 1f, O + 30);
            Sfx.Play("ult_impact", 0.5f, 1.3f);
            yield return Clock.Tween(0.16f, t =>
            {
                float k = Ease.InCubic(t);
                band.localScale = new Vector3(1 + k * 0.4f, 1 - k, 1);
                txtRoot.localScale = new Vector3(1 + k * 0.6f, 1 - k, 1);
                Make.Alpha(dim, 0.8f * (1 - k));
                linesMat.SetFloat("_Alpha", 0.75f * (1 - k));
            }, true);
            fx.Bars(false);
            Object.Destroy(root.gameObject);
        }
    }
}
