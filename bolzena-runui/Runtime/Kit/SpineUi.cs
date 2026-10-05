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

        static Material Mat
        {
            get
            {
                if (mat != null) return mat;
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
