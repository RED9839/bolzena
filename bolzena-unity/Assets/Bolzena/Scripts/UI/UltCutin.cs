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
        public static string LastArt;                     // 마지막 컷인 그림 — spine(스탠딩 스파인) · full(스탠딩 렌더 전신) · icon(초상) · none(점검 로그)
        public static float LastFit, LastLeg, LastHeight, FloorY, HipY;   // 맞춘 배율 · 다리(스켈레톤 단위) · 키/다리 · 바닥선 · 골반선(월드 y) — 점검 로그
        public static string LastHow;
        public static float CenterX, HipXf, HeadXf, HeadYf;   // 칸 가운데(월드 x) · 골반 x · 머리 x · 머리 y(경계 안 비율, 위 = 0)
        // 다리(골반→바닥)가 컷인 칸 높이의 이만큼 — 135명 키/다리 중앙값으로 고른 값(대부분이 칸 안에 전신으로 들어가게)
        public static float LegShare = 0.27f;
        // 스탠딩 맞춤 표가 있으면 — 몸 키 중앙값 사도의 몸 키가 칸 높이의 이만큼(머리카락 · 장식은 그 위로 남는 몫)
        public static float BodyShare = 0.74f;
        // 티그(영웅) 기준 — 그 사도의 몸(다리 바닥 ~ 몸 꼭대기)이 칸 높이의 이만큼. 원작 키 안(-cutinsize height)에서 가장 큰 빅우드(1.39배)도 몸은 칸 안
        public const string RefArt = "tighero";
        public static float RefBodyShare = 0.66f;
        // 컷인 손보정 — (가) 티그 기준에서 몸 키(다리 바닥 ~ 몸 꼭대기)로는 잴 수 없는 모양이라 티그(영웅)보다 크게 보이던 사도(2026-10-05 모아 보기로 눈으로 맞춤)
        //   쥬비 = 벌 몸통(다리 없음) · 이드 = 달 껍데기 안에 앉음 · 다야 · 리스티 · 스패럿 = 바위 · 드론 · 상자에 앉음 · 마에스트로2호 = 로봇 · 오팔 · 아일라 = 머리가 큼
        public static readonly System.Collections.Generic.Dictionary<string, float> CutinFix = new System.Collections.Generic.Dictionary<string, float>
        {
            ["jubee"] = 0.55f, ["ed"] = 0.72f, ["daya"] = 0.82f, ["risty"] = 0.82f, ["sparrot"] = 0.82f, ["maestromk2"] = 0.82f, ["opal"] = 0.92f, ["ayla"] = 0.9f,
        };
        public static bool ByHeight
        {
            get
            {
                var a = System.Environment.GetCommandLineArgs();
                int i = System.Array.IndexOf(a, "-cutinsize");
                return i >= 0 && i < a.Length - 1 && a[i + 1] == "height";
            }
        }

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

            // 3) 스탠딩 — 기본 스탠딩(Normal · Idle_1) **전신**을 화면 왼쪽 칸에 통째로 맞춘다(날개 · 무기 · 꼬리 포함, 잘리지 않게 — 2026-10 사용자).
            //   띠(마스크) 밖의 기울지 않은 층에 두어 띠는 뒤 배경으로만 깔린다. 칸 = 화면 왼쪽 끝 ~ 가운데 조금 오른쪽, 위아래 여백 4%.
            //   그림: 스탠딩 스파인(st_<키>, 시범 몇 명) → 판 화면 스탠딩 렌더(RunArt/Standing, 135명 — 투명 가장자리를 잘라 둔 전신) → 초상
            float hw = Tone.HalfW, hh = Tone.HalfH;
            float boxL = -hw + 0.25f, boxR = Mathf.Min(1.2f, hw * 0.12f), boxB = -hh * 0.96f, boxT = hh * 0.9f;   // 위는 여백 1할 — 쉬는 동작이 첫 자세보다 커지는 사도(이프리트 불꽃)
            float boxW = boxR - boxL, boxH = boxT - boxB;
            var boxC = new Vector3((boxL + boxR) / 2, (boxB + boxT) / 2, 0);
            Transform stRoot = Make.Node("standing", root, boxC);
            SkeletonAnimation st = null;
            var data = Res.Spine("st_" + hero);
            LastArt = data != null ? "spine" : "none";
            LastFit = 0; LastHow = null;
            if (data != null)
            {
                st = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                st.transform.SetParent(stRoot, false);
                var sd = data.GetSkeletonData(true);
                var wear = Bolzena.Fx.SpineMotion.Wear(sd, null, true);
                if (wear != null) { st.Skeleton.SetSkin(wear); st.Skeleton.SetSlotsToSetupPose(); }
                string anim = null;
                foreach (var pre in new[] { "Idle_1", "Idle" })
                    if (sd.FindAnimation(pre) != null) { anim = pre; break; }
                if (anim != null) st.AnimationState.SetAnimation(0, anim, true);
                st.AnimationState.Apply(st.Skeleton);
                st.Skeleton.UpdateWorldTransform();
                // 다리 기준 — 골반→바닥이 칸 높이의 LegShare 가 되게, 발은 칸 아래 바닥선에(전신이 넘치면 줄인다)
                float floorPad = boxH * 0.015f;
                FloorY = boxB + floorPad; CenterX = boxC.x;
                if (StandingTable.TryGet(hero, out var tf) && tf.BodyH > 0 && StandingTable.TryGet(RefArt, out var tg) && tg.BodyH > 0)
                {
                    // 컷인 크기 — 티그(영웅) 기준(2026-10 사용자). 몸(standing_fit.json 의 다리 바닥 legY ~ 몸 꼭대기 topY — 날개 · 무기 · 장식 제외)을
                    //   (가) 기본: 모두 티그(영웅)와 같은 화면 길이로(들쭉날쭉 없애기)
                    //   (나) -cutinsize height: 티그(영웅) = 1, 나머지는 원작 스탠딩 원래 비율(설정 키) 그대로
                    //   다리 바닥은 바닥선에, 몸 중심(centerX — 골반 · 몸통 뼈)은 칸 가운데에. 장식 · 날개는 넘쳐도 되고 몸은 칸 안에 든다
                    float refBody = boxH * RefBodyShare;
                    float sr = ByHeight ? refBody / tg.BodyH : refBody / tf.BodyH;     // 원래 단위 1 당 화면 길이
                    if (!ByHeight && CutinFix.TryGetValue(hero, out var fixMul)) sr *= fixMul;
                    float ox = boxC.x - tf.CenterX * sr;
                    float oy = FloorY - tf.LegY * sr;
                    float ls = sr / Mathf.Max(0.0001f, data.scale);                    // 스켈레톤 배율(유니티 단위 = 원래 단위 × asset.scale)
                    st.transform.localScale = new Vector3(ls, ls, 1);
                    st.transform.localPosition = new Vector3(ox, oy, 0) - boxC;
                    LastFit = ls; LastLeg = tf.BodyH; LastHow = ByHeight ? "원작 키" : "티그 기준"; LastHeight = tf.BodyH / tg.BodyH;
                    HipY = FloorY + tf.BodyH * sr;                                     // 점검 선 — 몸 꼭대기
                    var bd = tf.Bounds;
                    HipXf = bd.width > 0 ? (tf.CenterX - bd.xMin) / bd.width : 0.5f; HeadXf = HipXf; HeadYf = bd.height > 0 ? (bd.yMax - tf.HeadY) / bd.height : 0;
                }
                else
                {
                    // 표에 없는 사도 — 스켈레톤 경계로 칸에 통째로(발 = 바닥선, 가로 가운데)
                    st.Skeleton.UpdateWorldTransform();
                    float[] buf = null;
                    st.Skeleton.GetBounds(out float bx, out float by, out float bw, out float bh, ref buf);
                    float scale = Mathf.Min((boxH - floorPad) / Mathf.Max(0.01f, bh), boxW / Mathf.Max(0.01f, bw));
                    st.transform.localScale = new Vector3(scale, scale, 1);
                    st.transform.localPosition = new Vector3(boxC.x - (bx + bw / 2) * scale, FloorY - by * scale, 0) - boxC;
                    LastFit = scale; LastLeg = 0; LastHow = "경계(표 없음)"; LastHeight = 0;
                    HipY = FloorY + bh * scale; HipXf = 0.5f; HeadXf = 0.5f; HeadYf = 0;
                }
                st.GetComponent<MeshRenderer>().sortingOrder = O + 8;
            }
            else
            {
                var full = Bolzena.RunUI.CardArt.Standing(hero);
                var sp = full ?? Res.Sprite("Art/" + hero);
                if (sp != null)
                {
                    float r = sp.rect.width / Mathf.Max(1f, sp.rect.height);
                    float hgt = Mathf.Min(boxH, boxW / r);
                    var fs = Make.Box(full != null ? "full" : "face", stRoot, sp, Vector3.zero, new Vector2(hgt * r, hgt), O + 8);
                    LastArt = full != null ? "full" : "icon";
                    LastFit = hgt / boxH;
                }
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
                // 전신은 칸 자리에서 — 오른쪽에서 들어와 조금 흐른다(흐름은 칸 여백 안: 0.12 까지만)
                // (넘쳐 들어오지 않게 OutCubic — 넘치면 잠깐 왼쪽 끝이 잘린다)
                stRoot.localPosition = boxC + new Vector3(Mathf.Lerp(11.3f, 0f, Ease.OutCubic(k)) - Mathf.Min(0.12f, 0.12f * Mathf.Max(0, time - stIn)), 0, 0);
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
                stRoot.localScale = new Vector3(1 + k * 0.15f, 1 - k, 1);
                txtRoot.localScale = new Vector3(1 + k * 0.6f, 1 - k, 1);
                Make.Alpha(dim, 0.8f * (1 - k));
                linesMat.SetFloat("_Alpha", 0.75f * (1 - k));
            }, true);
            fx.Bars(false);
            Object.Destroy(root.gameObject);
        }
    }
}
