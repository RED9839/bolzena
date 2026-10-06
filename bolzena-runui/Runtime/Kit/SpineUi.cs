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

        // ── 스탠딩(st_) 데이터 붙들기 — 띄운 스탠딩 수로 센다 ──
        //   스탠딩 스켈레톤 데이터(파싱한 SkeletonData)는 사도 하나에 수 MB 이고 135명이다. 예전엔 띄운 스탠딩이 사라져도 캐시가 데이터를 붙들어
        //   도감 · 상세를 오갈수록 쌓였다(2026-10-06 웹 OOM). 마지막 스탠딩이 사라지면 캐시에서 빼고 파싱한 데이터를 비운다(Clear —
        //   관리 메모리는 프레임 끝 GC 에, 텍스처 · 원본은 가끔 도는 UnloadUnusedAssets 에 풀린다). 다시 쓰면 다시 읽는다.
        static readonly Dictionary<string, int> users = new Dictionary<string, int>();
        static int released;
        static AsyncOperation unloading;

        /// <summary>정리(UnloadUnusedAssets)가 도는 중 — 굽기를 잠깐 쉰다(LiveStanding).</summary>
        public static bool Unloading => unloading != null && !unloading.isDone;

        sealed class StandUser : MonoBehaviour
        {
            public string Folder;
            void OnDestroy() { if (Folder != null) Unuse(Folder); Folder = null; }
        }

        static void Use(GameObject go, string folder)
        {
            users[folder] = users.TryGetValue(folder, out var n) ? n + 1 : 1;
            go.AddComponent<StandUser>().Folder = folder;
        }

        static void Unuse(string folder)
        {
            if (!users.TryGetValue(folder, out var n)) return;
            if (n > 1) { users[folder] = n - 1; return; }
            users.Remove(folder);
            Release(folder);
        }

        /// <summary>쓰는 스탠딩이 없으면 캐시에서 빼고 파싱한 데이터를 비운다. 여럿 놓으면 UnloadUnusedAssets 를 한 번(비동기).</summary>
        public static void Release(string folder)
        {
            if (folder == null || users.ContainsKey(folder) || !cache.TryGetValue(folder, out var a)) return;
            cache.Remove(folder);
            if (a == null) return;
            try
            {
                if (a.atlasAssets != null) foreach (var aa in a.atlasAssets) if (aa != null) aa.Clear();
                a.Clear();
            }
            catch (System.Exception e) { Debug.LogWarning("[RunUI] 스탠딩 놓기 실패 " + folder + ": " + e.Message); }
            if (++released >= 12) UnloadSoon();
        }

        /// <summary>놓은 것들을 실제로 내린다(UnloadUnusedAssets — 비동기, 이미 돌고 있으면 건너뜀).</summary>
        public static void UnloadSoon()
        {
            if (Unloading) return;
            released = 0;
            unloading = Resources.UnloadUnusedAssets();
        }

        /// <summary>붙든 스탠딩 데이터 수(재기용).</summary>
        public static int Held { get { int n = 0; foreach (var k in cache.Keys) if (k.StartsWith("st_")) n++; return n; } }

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
            a = SpineStencil.Fix(SpineSource.Load(folder));   // 원작 스텐실 가리기(미로 거울) → 클리핑. 웹 빌드는 번들에서(SpineSource)
            if (a == null && SpineSource.IsPending(folder)) return null;   // 아직 받는 중 — 붙들지 않고 다음에 다시
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
            Use(g.gameObject, "st_" + art);
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
        public static SkeletonGraphic Standing(RectTransform area, string art, float w, float h, StandMode mode, float tallest = 0, float pad = 0, string lift = null)
        {
            var g = NewStanding(area, art);
            if (g == null) return null;
            if (StandingFit.Place(g, art, new Rect(0, 0, w, h), mode, tallest, pad, lift) > 0) return g;
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

        /// <summary>
        /// 사도 전투 SD 스파인(본 게임 Resources/Spine/&lt;사도 키&gt; — 원작 assets/spine/ingame/&lt;사도 키&gt;, 없으면 Spine/sd_&lt;그림 키&gt;) — 전투 화면과 같은 모습.
        ///   스킨 Normal(없으면 기본) · 쉬는 동작 Idle · 원작 전투처럼 모두 같은 배율(몸 키 중앙값 636 단위 = bodyPx). 발이 부모의 (0.5, 0).
        ///   faceRight = 오른쪽을 본다(파티 — 원작 SD 를 좌우로 뒤집는다), false = 원작 그대로 왼쪽(대상). 없으면 null(부르는 쪽이 정지 그림으로 대신).
        /// </summary>
        public static SkeletonGraphic Battle(Transform parent, HeroInfo h, float bodyPx, bool faceRight = true)
        {
            if (h == null) return null;
            // 시험 프로젝트(runui-test)는 Spine/<roster 키(한글)>, 본 게임(bolzena-unity copy_run_assets.py)은 Spine/<그림 키> 에 SD 전투 스파인을 둔다 —
            //   본 게임에서 앞 둘만 찾으면 이벤트 장면 · 캠프의 사도가 모두 정지 스탠딩으로 떨어졌다(2026-10-06 빠진 그림 조사)
            var data = (h.key != null ? DataQuiet(h.key) : null) ?? (h.art != null ? DataQuiet("sd_" + h.art) : null) ?? (h.art != null ? DataQuiet(h.art) : null);
            if (data == null || Mat == null) return null;
            SkeletonGraphic g;
            try { g = SkeletonGraphic.NewSkeletonGraphicGameObject(data, parent, Mat); }
            catch (System.Exception e) { Debug.LogWarning("[RunUI] 전투 스파인 실패 " + h.key + ": " + e.Message); return null; }
            g.name = "battle " + h.key;
            g.raycastTarget = false;
            var sk = g.Skeleton; var sd = sk.Data;
            var normal = sd.Skins.FirstOrDefault(x => string.Equals(x.Name, "Normal", System.StringComparison.OrdinalIgnoreCase)) ?? (sd.DefaultSkin == null ? sd.Skins.FirstOrDefault() : null);
            if (normal != null) { sk.SetSkin(normal); sk.SetSlotsToSetupPose(); }
            var anim = PickAnim(sd, "Idle", "idle", "Wait", "Stand");
            if (anim != null) g.AnimationState.SetAnimation(0, anim, true);
            g.Update(0); g.UpdateMesh();
            float unit = (g.skeletonDataAsset != null ? g.skeletonDataAsset.scale : 0.01f) * (g.MeshScale > 0 ? g.MeshScale : 100f);
            float ls = bodyPx / 636f / Mathf.Max(0.0001f, unit);
            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = new Vector3(faceRight ? -ls : ls, ls, 1);   // 원작 SD 는 왼쪽을 보고 그려졌다 — 오른쪽을 보려면 뒤집는다(전투 파티와 같이)
            return g;
        }

        static readonly Dictionary<string, SkeletonDataAsset> quiet = new Dictionary<string, SkeletonDataAsset>();
        static SkeletonDataAsset DataQuiet(string folder)
        {
            if (cache.TryGetValue(folder, out var a)) return a;
            if (quiet.TryGetValue(folder, out a)) return a;
            a = SpineStencil.Fix(SpineSource.Load(folder));
            if (a == null && SpineSource.IsPending(folder)) return null;   // 아직 받는 중
            quiet[folder] = a;
            return a;
        }

        /// <summary>
        /// 그려진 메시가 화면(screenRoot — 캔버스 뿌리) 높이의 maxShare 를 넘거나 box 가로를 넘으면 발(아래 끝)을 그대로 두고 줄이고, 가로는 box 안으로 민다.
        /// 표 · 캔버스 배율 · 스켈레톤 데이터 배율이 어긋나는 빌드(웹 · 통합 빌드)에서도 화면보다 크게 나오지 않게 하는 마지막 울타리(2026-10 「고해상도 에르핀 너무 큼」).
        /// 돌려줌: 줄인 비율(1 = 그대로).
        /// </summary>
        /// <param name="keepScale">떠 있는 사도(StandingFit.Floats — 얼굴을 기준 얼굴 높이에 맞춘 것): 줄이거나 위아래로 옮기지 않고 가로만 칸 안으로(2026-10-07 「얼굴 위치를 같게」 —
        ///   벨라 · 쥬비는 꼬리 · 날개 · 이펙트로 메시가 커서 줄이면 얼굴이 다른 사도보다 한참 아래로 내려갔다)</param>
        public static float ClampInto(SkeletonGraphic g, RectTransform box, RectTransform screenRoot, float maxShare = 0.93f, bool keepScale = false)
        {
            if (g == null || box == null) return 1;
            // 장면용 SD(SceneHero) 는 전투와 같은 고정 배율 · 원점 바닥선 — 메시 경계로 줄이거나 옮기지 않는다.
            //   메시에는 Idle 에서 투명한 슬롯(멜루나의 머리 위 3000 단위 슬롯 · 죠안의 사슬 · 에피카 …)도 들어 있어 경계가 몸보다 훨씬 크다(2026-10-06 「멜루나 엄청 작게」)
            if (g.GetComponent<SceneHero.FixedScale>() != null) return 1;
            g.UpdateMesh();
            var mesh = g.GetLastMesh();
            if (mesh == null || mesh.vertexCount == 0) return 1;
            var b = mesh.bounds;
            var space = screenRoot != null && screenRoot.rect.height > 100 ? screenRoot : box;
            var rt = g.rectTransform;
            Vector3 P(Vector3 local) => space.InverseTransformPoint(rt.TransformPoint(local));
            Vector3 lo = P(b.min), hi = P(b.max);
            float h = Mathf.Abs(hi.y - lo.y), lim = space.rect.height * maxShare;
            float s = h > lim && h > 1 && !keepScale ? lim / h : 1;
            Vector3 blo = space.InverseTransformPoint(box.TransformPoint(box.rect.min)), bhi = space.InverseTransformPoint(box.TransformPoint(box.rect.max));
            float w = Mathf.Abs(hi.x - lo.x) * s, bw = Mathf.Abs(bhi.x - blo.x);
            if (w > bw && w > 1 && !keepScale) s *= bw / w;
            if (s < 0.999f)
            {
                // 발(메시 아래 끝 · 가로 가운데)을 그 자리에 두고 줄인다
                var foot = new Vector3(b.center.x, b.min.y, 0);
                Vector3 before = rt.TransformPoint(foot);
                rt.localScale = rt.localScale * s;
                rt.position += before - rt.TransformPoint(foot);
            }
            // 위 끝이 화면 위 여백을 넘으면 내리고, 가로로 box 를 넘으면 안으로
            lo = P(b.min); hi = P(b.max);
            float top = space.rect.yMax - space.rect.height * (1 - maxShare) * 0.5f;
            if (hi.y > top && !keepScale) rt.position += space.TransformVector(new Vector3(0, top - hi.y, 0));
            float dx = lo.x < blo.x ? blo.x - lo.x : hi.x > bhi.x ? bhi.x - hi.x : 0;
            if (dx != 0) rt.position += space.TransformVector(new Vector3(dx, 0, 0));
            return s;
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
