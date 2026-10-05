using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 캔버스 위 스파인(SkeletonGraphic) — 미니미(지도) · 스탠딩(로비 · 상점 · 이벤트).
    // 그림은 곧은 알파로 풀어 넣었다(copy_assets.py) — 재질은 Straight Alpha. 없으면 null(화면은 초상으로 대신한다).
    public static class SpineUi
    {
        static Material mat;
        static readonly Dictionary<string, SkeletonDataAsset> cache = new Dictionary<string, SkeletonDataAsset>();

        /// <summary>곧은 알파 SkeletonGraphic 재질(판 화면 스파인 · 스탠딩 굽기가 같이 쓴다).</summary>
        public static Material StraightMat => Mat;

        /// <summary>캐시에서 뺀다(굽고 난 스탠딩 — Resources.UnloadUnusedAssets 로 풀리게).</summary>
        public static void Forget(string folder) { cache.Remove(folder); }

        static Material Mat
        {
            get
            {
                if (mat != null) return mat;
                // 곧은 알파 재질은 애셋(Resources/RunUI/SpineStraight.mat)으로 싣는다 — 런타임에 키워드만 켜면 빌드가 _STRAIGHT_ALPHA_INPUT 변형을
                // 빼 버릴 수 있고(shader_feature), 그러면 곧은 알파 그림이 PMA 로 그려져 가장자리마다 흰 테두리가 생긴다(2026-10 사용자 제보)
                var asset = Resources.Load<Material>("RunUI/SpineStraight");
                if (asset != null && asset.shader != null && asset.shader.isSupported)
                {
                    mat = new Material(asset) { name = "RunUI SkeletonGraphic (straight)" };
                    mat.SetFloat("_StraightAlphaInput", 1);
                    mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                    return mat;
                }
                var sh = Shader.Find("Spine/SkeletonGraphic");
                if (sh == null) { Debug.LogWarning("[RunUI] Spine/SkeletonGraphic 셰이더 없음"); return null; }
                mat = new Material(sh) { name = "RunUI SkeletonGraphic (straight)" };
                mat.SetFloat("_StraightAlphaInput", 1);
                mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                return mat;
            }
        }

        public static SkeletonDataAsset Data(string folder)
        {
            if (cache.TryGetValue(folder, out var a)) return a;
            var all = Resources.LoadAll<SkeletonDataAsset>("Spine/" + folder);
            a = all.Length > 0 ? all[0] : null;
            if (a == null) Debug.Log("[RunUI] 스파인 없음: " + folder);
            cache[folder] = a;
            return a;
        }

        /// <summary>발이 부모의 (0.5, 0) 에 닿게, 키가 height 픽셀이 되게 세운다. skin 이 없으면 Normal(웹판 규칙과 같이).</summary>
        public static SkeletonGraphic Make(Transform parent, string folder, string skin, float height, params string[] anims)
        {
            var data = Data(folder);
            if (data == null || Mat == null) return null;
            SkeletonGraphic g;
            try
            {
                g = SkeletonGraphic.NewSkeletonGraphicGameObject(data, parent, Mat);
            }
            catch (System.Exception e) { Debug.LogWarning("[RunUI] 스파인 실패 " + folder + ": " + e.Message); return null; }
            g.name = "spine " + folder + (skin != null ? " " + skin : "");
            g.raycastTarget = false;
            var sk = g.Skeleton;
            var sd = sk.Data;
            Spine.Skin wear = null;
            if (skin != null) wear = sd.FindSkin(skin) ?? sd.Skins.FirstOrDefault(s => string.Equals(s.Name, skin, System.StringComparison.OrdinalIgnoreCase));
            if (wear == null)
            {
                var normal = sd.Skins.FirstOrDefault(s => string.Equals(s.Name, "Normal", System.StringComparison.OrdinalIgnoreCase));
                wear = normal ?? (sd.DefaultSkin == null ? sd.Skins.FirstOrDefault() : null);
            }
            if (wear != null) { sk.SetSkin(wear); sk.SetSlotsToSetupPose(); }
            var anim = PickAnim(sd, anims);
            if (anim != null) g.AnimationState.SetAnimation(0, anim, true);
            g.Update(0);
            g.LateUpdate();

            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = Vector2.zero;
            sk.UpdateWorldTransform();
            float[] buf = null;
            sk.GetBounds(out float bx, out float by, out float bw, out float bh, ref buf);
            float ppu = 100f;
            if (bh > 0.0001f)
            {
                float k = height / (bh * ppu);
                rt.localScale = new Vector3(k, k, 1);
            }
            return g;
        }

        /// <summary>
        /// 사도 스탠딩 스파인(Spine/st_<그림 키> — Normal 스킨 · Idle_1 반복)을 area 안에 **다리 기준**으로 세운다(StandingFit).
        ///   legPx = 다리(골반→바닥) 화면 길이 · floorPx = 발바닥이 area 아래에서 얼마 위(음수면 아래로 — 무릎께 자르기) · 골반 x 는 area 가운데.
        ///   fitAll 이면 전신이 area 를 넘을 때 줄인다. headMax(0~1) 를 주면 머리 뼈가 area 높이의 그 몫 위로 넘지 않게 내린다(상반신 창).
        ///   스파인이 없으면 null(화면은 정지 그림으로).
        /// </summary>
        /// <summary>스탠딩 스파인 하나(Normal 스킨 · Idle_1 반복)를 area 아래에 만든다 — 자리는 아직 안 잡는다. 없으면 null.</summary>
        public static SkeletonGraphic NewStanding(RectTransform area, string art)
        {
            if (string.IsNullOrEmpty(art)) return null;
            var data = Data("st_" + art);
            if (data == null || Mat == null) return null;
            SkeletonGraphic g;
            try { g = SkeletonGraphic.NewSkeletonGraphicGameObject(data, area, Mat); }
            catch (System.Exception e) { Debug.LogWarning("[RunUI] 스탠딩 실패 " + art + ": " + e.Message); return null; }
            g.name = "standing " + art;
            g.raycastTarget = false;
            var sk = g.Skeleton;
            var sd = sk.Data;
            // 옷은 Normal 만(덧스킨 · 이벤트 소품은 입히지 않는다 — 웹판 stand 규칙)
            var normal = sd.Skins.FirstOrDefault(x => string.Equals(x.Name, "Normal", System.StringComparison.OrdinalIgnoreCase)) ?? (sd.DefaultSkin == null ? sd.Skins.FirstOrDefault() : null);
            if (normal != null) { sk.SetSkin(normal); sk.SetSlotsToSetupPose(); }
            var anim = PickAnim(sd, "Idle_1", "Idle");
            if (anim != null) g.AnimationState.SetAnimation(0, anim, true);
            g.Update(0);
            g.LateUpdate();
            return g;
        }

        /// <summary>
        /// 사도 스탠딩 스파인을 area 의 w×h 칸(왼쪽 아래 0,0)에 표(standing_fit.json)대로 세운다 — 모드 Full · Knee · Upper,
        /// tallest = 기준 키(StandingFit.Tallest(함께 보일 사도들) — 같은 값을 주면 같은 배율 · 같은 바닥선, 0 이면 표 전체 95%). 표에 없으면 그 자리에서 재서 대신.
        /// </summary>
        public static SkeletonGraphic Standing(RectTransform area, string art, float w, float h, StandMode mode, float tallest = 0, float pad = 0)
        {
            var g = NewStanding(area, art);
            if (g == null) return null;
            if (StandingFit.Place(g, art, new Rect(0, 0, w, h), mode, tallest, pad) > 0) return g;
            // 표에 없는 사도 — 옛 다리 기준(전신 = 다리 키 몫 · 무릎께 · 상반신은 발을 칸 아래로)
            float bodyPx = h * StandingFit.Fill(mode) * 0.85f;
            float leg = bodyPx / 2.6f;
            Destroy(g.gameObject);
            return mode == StandMode.Full ? Standing(area, art, w, h, leg, pad, true)
                : Standing(area, art, w, h, leg, h - (pad > 0 ? pad : h * 0.06f) - bodyPx * 1.02f, false, 0.86f);
        }

        static void Destroy(Object o) { if (o != null) Object.Destroy(o); }

        public static SkeletonGraphic Standing(RectTransform area, string art, float w, float h, float legPx, float floorPx = 0, bool fitAll = true, float headMax = 0)
        {
            var g = NewStanding(area, art);
            if (g == null) return null;
            var sk = g.Skeleton;
            // 표(standing_fit.json)에 있으면 그 값(바닥 · 중심 · 머리 · 몸 키 배율), 없으면 그 자리에서 잰다
            var m = StandingFit.Measure(sk, art, g.skeletonDataAsset != null ? g.skeletonDataAsset.scale : 0.01f);
            const float ppu = 100f;
            var box = new Rect(0, 0, w / ppu, h / ppu);
            float s;
            Vector2 off;
            if (fitAll) s = StandingFit.Place(m, box, legPx / ppu, 1f, floorPx / ppu, out off, out _);
            else
            {
                s = legPx / ppu / Mathf.Max(0.0001f, m.Leg);
                off = new Vector2(box.center.x - m.HipX * s, floorPx / ppu - m.Floor * s);
            }
            if (headMax > 0)
            {
                float head = off.y + m.HeadY * s, lim = box.height * headMax;
                if (head > lim) off.y -= head - lim;
            }
            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.localScale = new Vector3(s, s, 1);
            rt.anchoredPosition = off * ppu;
            rt.sizeDelta = new Vector2(w, h) / Mathf.Max(0.0001f, s);   // 칸을 덮는 크기(RectMask2D 가 통째로 걸러 내지 않게)
            return g;
        }

        public static string PickAnim(Spine.SkeletonData sd, params string[] prefs)
        {
            var names = sd.Animations.Select(a => a.Name).ToList();
            foreach (var p in prefs)
            {
                var hit = names.FirstOrDefault(n => string.Equals(n, p, System.StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            foreach (var p in prefs)
            {
                var hit = names.FirstOrDefault(n => n.ToLowerInvariant().Contains(p.ToLowerInvariant()));
                if (hit != null) return hit;
            }
            return names.FirstOrDefault();
        }

        public static void Play(SkeletonGraphic g, bool loop, params string[] prefs)
        {
            if (g == null) return;
            var a = PickAnim(g.Skeleton.Data, prefs);
            if (a != null) g.AnimationState.SetAnimation(0, a, loop);
        }
    }
}
