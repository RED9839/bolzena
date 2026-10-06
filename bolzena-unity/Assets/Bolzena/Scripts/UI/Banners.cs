using System.Collections;
using System.Collections.Generic;
using Bolzena.View;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 띠 알림 — 턴 띠, 보스 등장(경고 띠 + 보스 크게 + 이름), 승리
    public static class Banners
    {
        const int O = 900;
        public static System.Action<string> OnStage;

        // 남색 띠(양 끝으로 사라짐) + 위아래 금빛 실선 — 턴 · 승리 · 패배가 같이 쓴다
        static (SpriteRenderer band, SpriteRenderer top, SpriteRenderer bot) Band(Transform root, float h, Color line, int order)
        {
            var band = Make.Box("band", root, Res.UI("band"), Vector3.zero, new Vector2(18f, h), order, new Color(1, 1, 1, 0));
            var top = Make.Box("top", root, Res.UI("band_line"), new Vector3(0, h / 2, 0), new Vector2(14f, 0.03f), order + 1, new Color(line.r, line.g, line.b, 0), Res.SpriteMat(false, 1.4f));
            var bot = Make.Box("bot", root, Res.UI("band_line"), new Vector3(0, -h / 2, 0), new Vector2(14f, 0.03f), order + 1, new Color(line.r, line.g, line.b, 0), Res.SpriteMat(false, 1.4f));
            return (band, top, bot);
        }

        static void BandAlpha((SpriteRenderer band, SpriteRenderer top, SpriteRenderer bot) b, float a, float open)
        {
            Make.Alpha(b.band, a);
            Make.Alpha(b.top, a);
            Make.Alpha(b.bot, a);
            b.band.transform.parent.localScale = new Vector3(1, Mathf.Max(0.001f, open), 1);
        }

        // 짧은 턴 띠 — 「PLAYER TURN」
        public static IEnumerator Turn(Transform parent, string text, Color c)
        {
            var root = Make.Node("TurnBanner", parent, new Vector3(0, 0.9f, 0));
            var bandRoot = Make.Node("b", root);
            var b = Band(bandRoot, 0.86f, Color.Lerp(c, new Color(0.95f, 0.83f, 0.55f), 0.5f), O);
            var glow = Make.Box("glow", root, Res.FxTex("FX_UI_light_line_Blur"), Vector3.zero, new Vector2(10, 0.9f), O + 1, new Color(c.r, c.g, c.b, 0), Res.SpriteMat(true, 1.4f));
            var t = Make.Text("t", root, text, Vector3.zero, 0.46f, O + 2, Color.white);
            t.colorGradient = new VertexGradient(Color.white, Color.white, Color.Lerp(c, Color.white, 0.35f), Color.Lerp(c, Color.white, 0.35f));
            Make.Outline(t, 0.18f, new Color(0.02f, 0.03f, 0.08f, 0.9f));
            t.characterSpacing = 30;
            yield return Clock.Tween(0.2f, k =>
            {
                BandAlpha(b, k, Ease.OutBack(k));
                Make.Alpha(glow, 0.55f * k);
                t.alpha = k;
                t.characterSpacing = Mathf.Lerp(80, 30, Ease.OutCubic(k));
            }, true);
            yield return Clock.WaitU(0.45f);
            yield return Clock.Tween(0.2f, k =>
            {
                BandAlpha(b, 1 - k, 1);
                Make.Alpha(glow, 0.55f * (1 - k));
                t.alpha = 1 - k;
                root.localPosition = new Vector3(k * 2f, 0.9f, 0);
            }, true);
            Object.Destroy(root.gameObject);
        }

        // 패배 — 붉은 실선의 남색 띠 · 「DEFEAT」, 남아 있는다(판으로 돌아갈 때까지)
        public static IEnumerator Defeat(Transform parent)
        {
            var root = Make.Node("Defeat", parent, new Vector3(0, 1.0f, 0));
            var dim = Make.Box("dim", root, Res.UI("white"), new Vector3(0, -1.0f, 0), new Vector2(44, 16), O - 1, new Color(0.02f, 0.02f, 0.05f, 0));
            var bandRoot = Make.Node("b", root);
            var b = Band(bandRoot, 1.5f, new Color(1f, 0.42f, 0.42f), O);
            var t = Make.Text("t", root, "DEFEAT", new Vector3(0, 0.12f, 0), 0.95f, O + 3, Color.white);
            t.colorGradient = new VertexGradient(new Color(0.95f, 0.92f, 0.95f), new Color(0.95f, 0.92f, 0.95f), new Color(1f, 0.45f, 0.45f), new Color(1f, 0.45f, 0.45f));
            Make.Outline(t, 0.15f, new Color(0.12f, 0, 0.02f));
            var sub = Make.Text("sub", root, "파티가 쓰러졌습니다", new Vector3(0, -0.5f, 0), 0.22f, O + 3, new Color(0.8f, 0.82f, 0.92f));
            sub.characterSpacing = 12;
            t.alpha = 0; sub.alpha = 0;
            yield return Clock.Tween(0.5f, k =>
            {
                Make.Alpha(dim, 0.5f * k);
                BandAlpha(b, k, Ease.OutCubic(k));
                t.alpha = k; sub.alpha = k;
                t.characterSpacing = Mathf.Lerp(60, 14, Ease.OutCubic(k));
            }, true);
            OnStage?.Invoke("defeat");
            yield return Clock.WaitU(0.6f);
        }

        // 보스 등장 — 위아래 경고 띠가 흐르고, 비스듬한 띠 안으로 보스가 크게(검은 실루엣 → 드러남), 이름이 찍힌다
        public static IEnumerator Boss(Transform parent, string spine, string skin, string name, string sub)
        {
            var root = Make.Node("BossBanner", parent);
            Sfx.Play("boss_entry", 0.9f);
            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(44, 16), O, new Color(0.05f, 0, 0, 0));
            // 경고 띠
            var warns = new List<Transform>();
            foreach (float y in new[] { 3.55f, -3.55f })
            {
                var w = Make.Node("warn", root, new Vector3(0, y, 0));
                Make.Box("bg", w, Res.UI("white"), Vector3.zero, new Vector2(44, 0.62f), O + 1, new Color(0.75f, 0.05f, 0.08f, 0.92f));
                var txt = Make.Text("t", w, string.Concat(System.Linq.Enumerable.Repeat("WARNING  //  BOSS  //  ", 8)), new Vector3(0, -0.02f, 0), 0.3f, O + 2, new Color(1f, 0.85f, 0.8f), TextAlignmentOptions.Center, 60);
                txt.characterSpacing = 8;
                w.localScale = new Vector3(1, 0, 1);
                warns.Add(w);
            }
            // 비스듬한 띠 + 보스
            var band = Make.Node("band", root, new Vector3(0, 0.1f, 0));
            band.localRotation = Quaternion.Euler(0, 0, -7);
            var back = Make.Box("back", band, Res.UI("white"), new Vector3(26, 0, 0), new Vector2(26, 4.4f), O + 3, new Color(0.12f, 0.02f, 0.04f, 0.95f));
            var edgeA = Make.Box("edgeA", band, Res.UI("white"), new Vector3(-26, 2.25f, 0), new Vector2(26, 0.1f), O + 4, new Color(1f, 0.3f, 0.25f), Res.SpriteMat(false, 2.5f));
            var edgeB = Make.Box("edgeB", band, Res.UI("white"), new Vector3(-26, -2.25f, 0), new Vector2(26, 0.1f), O + 4, new Color(1f, 0.3f, 0.25f), Res.SpriteMat(false, 2.5f));
            SkeletonAnimation boss = null;
            var bossRoot = Make.Node("boss", band, new Vector3(9f, -2.9f, 0));
            bossRoot.localRotation = Quaternion.Euler(0, 0, 7);
            var data = Res.Spine(spine);
            var bossMr = (MeshRenderer)null;
            var block = new MaterialPropertyBlock();
            if (data != null)
            {
                boss = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                boss.transform.SetParent(bossRoot, false);
                boss.transform.localScale = Vector3.one * 0.5f;
                { var wear = Bolzena.Fx.SpineMotion.Wear(data.GetSkeletonData(true), skin); if (wear != null) { boss.Skeleton.SetSkin(wear); boss.Skeleton.SetSlotsToSetupPose(); } }
                var a = data.GetSkeletonData(true).FindAnimation("Idle");
                if (a != null) boss.AnimationState.SetAnimation(0, a, true);
                bossMr = boss.GetComponent<MeshRenderer>();
                bossMr.sortingOrder = O + 5;
            }
            var maskT = Make.Node("mask", band);
            var mask = maskT.gameObject.AddComponent<SpriteMask>();
            mask.sprite = Res.UI("white");
            maskT.localScale = new Vector3(40f / Res.UI("white").bounds.size.x, 4.4f / Res.UI("white").bounds.size.y, 1);
            if (boss != null) boss.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            var nameT = Make.Text("name", root, name, new Vector3(-3.2f, 0.25f, 0), 1.15f, O + 8, Color.white);
            nameT.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.45f, 0.35f), new Color(1f, 0.45f, 0.35f));
            Make.Outline(nameT, 0.18f, new Color(0.25f, 0, 0.02f));
            Make.Glow(nameT, Color.white, 1.3f);
            var subT = Make.Text("sub", root, sub, new Vector3(-3.2f, -0.75f, 0), 0.32f, O + 8, new Color(1f, 0.8f, 0.75f));
            Make.Outline(subT, 0.3f, Color.black);
            subT.characterSpacing = 20;
            nameT.alpha = 0; subT.alpha = 0;

            yield return Clock.Tween(0.25f, t =>
            {
                Make.Alpha(dim, 0.6f * t);
                foreach (var w in warns) w.localScale = new Vector3(1, Ease.OutBack(t), 1);
                float k = Ease.OutExpo(t);
                back.transform.localPosition = new Vector3(Mathf.Lerp(26, 0, k), 0, 0);
                edgeA.transform.localPosition = new Vector3(Mathf.Lerp(-26, 0, k), 2.25f, 0);
                edgeB.transform.localPosition = new Vector3(Mathf.Lerp(-26, 0, k), -2.25f, 0);
                
            }, true);

            float time = 0, total = 2.3f;
            bool shot = false, flashed = false;
            while (time < total)
            {
                float dt = Time.unscaledDeltaTime;
                time += dt;
                foreach (var w in warns)
                {
                    var tx = w.GetChild(1);
                    float dir = w.localPosition.y > 0 ? -1 : 1;
                    tx.localPosition = new Vector3(Mathf.Repeat(time * 2.2f * dir, 6f) - 3f, -0.02f, 0);
                }
                // 보스 — 실루엣으로 미끄러져 들어와 0.55초에 번쩍 드러난다
                float k = Mathf.Clamp01(time / 0.5f);
                bossRoot.localPosition = new Vector3(Mathf.Lerp(9f, 3.6f, Ease.OutCubic(k)) - time * 0.2f, -2.9f, 0);
                if (bossMr != null)
                {
                    float fill = time < 0.55f ? 1f : Mathf.Clamp01(1 - (time - 0.55f) / 0.35f);
                    block.SetColor("_FillColor", time < 0.55f ? new Color(0.05f, 0, 0.02f) : Color.white);
                    block.SetFloat("_FillPhase", fill);
                    bossMr.SetPropertyBlock(block);
                }
                if (!flashed && time >= 0.55f)
                {
                    flashed = true;
                    Vfx.Flash(new Color(1f, 0.6f, 0.5f, 0.35f), 0.25f, 1f, O + 20);
                    FieldRig.Shake(0.5f, 2f);
                    PostFx.Kick(chroma: 0.8f, lens: -0.25f);
                }
                float tk = Mathf.Clamp01((time - 0.6f) / 0.25f);
                nameT.alpha = tk; subT.alpha = tk;
                nameT.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.8f, 1f, Ease.OutBack(tk, 2f));
                nameT.characterSpacing = Mathf.Lerp(40, 4, Ease.OutCubic(tk));
                if (!shot && time > 1.0f) { shot = true; OnStage?.Invoke("boss_entry"); }
                yield return null;
            }
            yield return Clock.Tween(0.25f, t =>
            {
                Make.Alpha(dim, 0.6f * (1 - t));
                foreach (var w in warns) w.localScale = new Vector3(1, 1 - t, 1);
                band.localScale = new Vector3(1, 1 - Ease.InCubic(t), 1);
                nameT.alpha = 1 - t; subT.alpha = 1 - t;
            }, true);
            Object.Destroy(root.gameObject);
        }

        // 승리
        public static IEnumerator Victory(Transform parent)
        {
            var root = Make.Node("Victory", parent);
            Sfx.Play("victory", 0.8f);
            var raysMat = Res.NewMat("Bolzena/Rays");
            raysMat.SetColor("_Color", new Color(1f, 0.85f, 0.45f, 1f));
            raysMat.SetFloat("_Count", 18);
            raysMat.SetFloat("_Spin", 0.06f);
            raysMat.SetFloat("_Boost", 1.6f);
            var rays = Make.Quad("rays", root, new Vector3(0, 1.4f, 0), new Vector2(16, 16), raysMat, O);
            Make.Own(rays.gameObject, raysMat);
            var bandRoot = Make.Node("b", root, new Vector3(0, 1.4f, 0));
            var band = Band(bandRoot, 1.7f, new Color(0.95f, 0.83f, 0.55f), O + 1);
            var sub = Make.Text("sub", root, "승리", new Vector3(0, 0.82f, 0), 0.24f, O + 3, new Color(0.95f, 0.88f, 0.7f));
            sub.characterSpacing = 30;
            sub.alpha = 0;
            var t = Make.Text("t", root, "VICTORY", new Vector3(0, 1.58f, 0), 1.05f, O + 3, Color.white);
            t.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.75f, 0.3f), new Color(1f, 0.75f, 0.3f));
            Make.Outline(t, 0.15f, new Color(0.3f, 0.12f, 0));
            Make.Glow(t, Color.white, 1.5f);
            t.alpha = 0;
            yield return Clock.Tween(0.4f, k =>
            {
                raysMat.SetFloat("_Alpha", 0.6f * k);
                BandAlpha(band, k, Ease.OutCubic(k));
                sub.alpha = k;
                t.alpha = k;
                t.characterSpacing = Mathf.Lerp(70, 10, Ease.OutCubic(k));
                t.transform.localScale = Vector3.one * Mathf.LerpUnclamped(2f, 1f, Ease.OutBack(k, 1.8f));
            }, true);
            Vfx.Burst(new Vector3(0, 1.4f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_UI_star_02", Count = 40, Speed = new Vector2(2f, 7f), Life = new Vector2(0.8f, 1.4f), Size = new Vector2(0.12f, 0.3f),
                C0 = new Color(1f, 0.85f, 0.4f), C1 = Color.white, Drag = 1.2f, Gravity = 0.15f, Order = O + 4, Boost = 3f, Parent = root, ShrinkTo = 0, Spin = true,
            });
            OnStage?.Invoke("victory");
        }
    }
}
