using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Spine;
using Spine.Unity;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena.View
{
    // 싸움터의 사도 · 적 하나 — SD 스파인, 그림자, 내딛기 · 밀려남, 흰 번쩍임(Spine/Skeleton Fill), 잔상, 몸짓 이름 고르기, 때리는 순간 읽기
    public class UnitView : MonoBehaviour
    {
        public UnitRef Ref;
        public SkeletonAnimation Sa;
        public Transform Art;                // 몸(내딛기 · 밀려남은 이 마디를 옮긴다)
        public string Mood;                  // 적의 성격 끝말(Mad · Cool …)
        public float Scale;
        public bool Facing;                  // true 면 오른쪽을 본다(사도)
        public Vector3 Home;
        public Vector3 Slot;                 // 처음 받은 자리(표) — 배치(EnemyLayout)가 언제 다시 짜도 여기서 시작한다
        public int BaseOrder;
        SkeletonDataAsset data;
        string skin;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float fillPhase;
        Color fillColor = Color.white;
        Color tint = Color.white, tintTarget = Color.white;
        Vector3 offset, knock;
        public Vector3 Nudge;                // 화면 밖 막기가 미는 몫(고학년 루트 이동)
        SpriteRenderer shadow;
        float heightCache = -1;
        SpriteRenderer iconArt;              // 스킨이 없는 적 — 스파인 대신 정지 아이콘(또는 이름 판)
        float iconW;

        /// <param name="icon">적만 — 요청한 스킨이 그 스파인에 없을 때 대신 세울 정지 아이콘(Resources/Art/Monster). "" 이면 이름 판. null 이면 옛 규칙(옷 입히기).</param>
        public static UnitView Create(Transform parent, string name, string spineFolder, string skin, bool faceRight, float scale, Vector3 pos, int order, string icon = null, string label = null, bool standIn = false)
        {
            var root = Make.Node(name, parent, pos);
            var v = root.gameObject.AddComponent<UnitView>();
            v.data = Res.Spine(spineFolder) ?? Res.Spine(faceRight ? "ricota" : "fairymobcloserange");   // 그림이 없으면 대역
            v.skin = skin;
            v.Scale = scale;
            v.Facing = faceRight;
            v.Home = v.Slot = pos;
            v.BaseOrder = order;
            v.shadow = Make.Sprite("shadow", root, Res.UI("shadow"), new Vector3(0, 0.02f, 0), order - 1, new Color(1, 1, 1, 0.75f));
            v.Art = Make.Node("art", root);
            v.Sa = SkeletonAnimation.NewSkeletonAnimationGameObject(v.data);
            v.Sa.transform.SetParent(v.Art, false);
            v.Sa.transform.localScale = new Vector3(scale, scale, 1);
            var sdata = v.data.GetSkeletonData(true);
            bool skinMissing = icon != null && !string.IsNullOrEmpty(skin) && !HasSkin(sdata, skin);
            // 제 스파인이 없어 대역이 선 적 — 원작 아이콘이 있으면 대역 대신 아이콘(확실한 그림만)
            if (standIn && !string.IsNullOrEmpty(icon) && Res.Sprite("Art/Monster/" + icon) != null) { skinMissing = true; Debug.Log($"[Look] {name}: 제 스파인 없음 — 대역 대신 아이콘 {icon}"); }
            else if (skinMissing)
                Debug.LogWarning($"[Look] 스킨 없음 {name}: {spineFolder} 에 「{skin}」 이 없다 — 추측 그림 대신 " + (icon.Length > 0 ? "아이콘 " + icon : "이름 판"));
            if (!string.IsNullOrEmpty(skin) && sdata.FindSkin(skin) != null)
            {
                v.Sa.Skeleton.SetSkin(skin);
                v.Sa.Skeleton.SetSlotsToSetupPose();
            }
            else
            {
                // 이름 그대로인 스킨이 없으면 웹판 옷 입히기 규칙(com.bolzena.fx SpineMotion.Wear — 대소문자 무시 · Normal · 덧스킨)
                var wear = Bolzena.Fx.SpineMotion.Wear(v.data.GetSkeletonData(true), skin);
                if (wear != null) { v.Sa.Skeleton.SetSkin(wear); v.Sa.Skeleton.SetSlotsToSetupPose(); }
            }
            v.Sa.Skeleton.ScaleX = faceRight ? -1 : 1;     // 원작 SD 는 왼쪽을 본다
            v.mr = v.Sa.GetComponent<MeshRenderer>();
            v.mr.sortingOrder = order;
            v.mpb = new MaterialPropertyBlock();
            v.Idle();
            v.Sa.AnimationState.Update(Random.value * 1.5f);
            if (skinMissing) v.UseIcon(icon, label);
            float sw = Mathf.Max(1.4f, v.Width() * 0.75f);
            Make.Fit(v.shadow, new Vector2(sw, sw * 0.28f));
            return v;
        }

        static bool HasSkin(SkeletonData d, string skin)
        {
            if (d.FindSkin(skin) != null) return true;
            foreach (var k in d.Skins) if (string.Equals(k.Name, skin, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // 스파인 몸은 그대로 두고(뼈 · 몸짓 자리) 그리지 않는다 — 그 자리에 정지 아이콘 · 이름 판
        void UseIcon(string icon, string label)
        {
            mr.enabled = false;
            var sp = string.IsNullOrEmpty(icon) ? null : Res.Sprite("Art/Monster/" + icon);
            const float S = 1.7f;
            if (sp != null)
            {
                iconArt = Make.Box("icon", Art, sp, new Vector3(0, S * 0.55f, 0), new Vector2(S, S), BaseOrder);
                iconW = S;
                heightCache = S * 1.1f;
                return;
            }
            iconArt = Make.Sliced("plate", Art, Res.UI("bar_fill_9s"), new Vector3(0, 0.7f, 0), new Vector2(1.6f, 0.5f), BaseOrder, new Color(0.06f, 0.08f, 0.16f, 0.9f));
            var t = Make.Text("name", Art, label ?? name, new Vector3(0, 0.7f, 0), 0.2f, BaseOrder + 1, Color.white);
            Make.Outline(t, 0.3f, new Color(0, 0, 0, 0.9f));
            plateText = t;
            iconW = 1.6f;
            heightCache = 1.4f;
        }

        // ── 동작 ──
        public bool Has(string anim) => data.GetSkeletonData(true).FindAnimation(anim) != null;

        public string Resolve(string baseName)
        {
            if (!string.IsNullOrEmpty(Mood) && Has(baseName + "_" + Mood)) return baseName + "_" + Mood;
            if (Has(baseName)) return baseName;
            foreach (var a in data.GetSkeletonData(true).Animations)
                if (a.Name.StartsWith(baseName, System.StringComparison.OrdinalIgnoreCase)) return a.Name;
            return null;
        }

        // 변신 중 평타 · 강공 대신 트는 동작(림(혼돈) 「차원 너머의 낫」 — 원작 강화 공격 AS1_Ultimate1_2). 없으면 null
        public string FormAttack;

        public string AnimFor(Motion m)
        {
            if (FormAttack != null && (m == Motion.Attack1 || m == Motion.Attack2) && Has(FormAttack)) return FormAttack;
            switch (m)
            {
                case Motion.Attack1: return Resolve("Attack1_1");
                case Motion.Attack2: return Resolve("Attack2_1") ?? Resolve("Attack1_1");
                case Motion.Skill1: return Resolve("Skill1_1") ?? Resolve("Attack1_1");
                case Motion.Ultimate: return Resolve("Ultimate1_1") ?? Resolve("Skill1_1");
            }
            return null;
        }

        public void Idle()
        {
            var n = IdleName();
            if (n != null) Sa.AnimationState.SetAnimation(0, n, true);
        }

        // ── 변신 ── 스킨은 따로 없고 변신 동작(꼬리 _DreamForm · _Change)이 그림을 바꾼다. 변신 중엔 언제나 꼬리 동작(있으면)만 튼다
        public string FormTail;
        public string F(string anim)
        {
            if (FormTail == null || anim == null || anim.EndsWith("_" + FormTail)) return anim;
            var f = anim + "_" + FormTail;
            return Has(f) ? f : anim;
        }
        string IdleName() { var n = Has("Idle") ? "Idle" : Resolve("Idle"); return F(n); }

        // 변신 들고 남 — 하던 몸짓(고학년 끝 자세 = 변신 모습)은 그대로 두고 그 뒤의 쉬는 동작만 바꾼다. 풀리면 본 Idle 로 섞기 0.2초
        public void SetForm(string tail)
        {
            FormTail = tail;
            if (!SpineArt) return;
            var st = Sa.AnimationState;
            var cur = st.GetCurrent(0);
            var idle = IdleName();
            if (idle == null) return;
            if (cur != null && !cur.Loop)
            {
                var e = cur;
                while (e.Next != null && !e.Next.Loop) e = e.Next;
                st.ClearNext(e);
                st.AddAnimation(0, idle, true, 0).MixDuration = tail != null ? 0f : 0.2f;
            }
            else st.SetAnimation(0, idle, true).MixDuration = tail != null ? 0.1f : 0.2f;
        }

        // 한 번 하고 쉬는 동작으로. 길이(초, 속도 반영)를 돌려준다
        public float Play(string anim, float speed = 1f, bool thenIdle = true)
        {
            if (anim == null) return 0;
            StopCut();
            var e = Sa.AnimationState.SetAnimation(0, F(anim), false);
            e.TimeScale = speed;
            e.MixDuration = 0.08f;
            if (thenIdle)
            {
                var idle = IdleName();
                if (idle != null) Sa.AnimationState.AddAnimation(0, idle, true, 0).MixDuration = 0.2f;
            }
            return e.Animation.Duration / Mathf.Max(0.01f, speed);
        }

        public Spine.SkeletonData SkelData => data != null ? data.GetSkeletonData(true) : null;
        public bool SpineArt => Sa != null && Sa.gameObject.activeSelf;

        /// <summary>조각을 이어 튼다(고학년 — 에르핀 1_1 → 1_2_Loop ×2 → 1_3). 전체 길이(초, 속도 반영)를 돌려준다.</summary>
        public float PlayChain(string first, IList<string> chain, float speed = 1f)
        {
            if (first == null) return 0;
            StopCut();
            var e = Sa.AnimationState.SetAnimation(0, F(first), false);
            e.TimeScale = speed; e.MixDuration = 0.08f;
            float total = e.Animation.Duration;
            if (chain != null)
                foreach (var n in chain)
                {
                    var a = data.GetSkeletonData(true).FindAnimation(F(n));
                    if (a == null) continue;
                    var c = Sa.AnimationState.AddAnimation(0, a, false, 0);
                    c.TimeScale = speed; c.MixDuration = 0;
                    total += a.Duration;
                }
            var idle = IdleName();
            if (idle != null) Sa.AnimationState.AddAnimation(0, idle, true, 0).MixDuration = 0.2f;
            return total / Mathf.Max(0.01f, speed);
        }

        // 고학년 줄이기 — 조각 안의 빈 구간(skips, 이어 튼 전체 시각 초)을 건너뛰고 endAt 초에서 쉬는 동작으로. PlayChain 바로 뒤에 부른다
        public void CutChain(string first, IList<string> chain, List<(float from, float to)> skips, float endAt)
        {
            var names = new List<string> { first }; if (chain != null) names.AddRange(chain);
            var offs = new List<float>(); float o = 0;
            foreach (var n in names) { offs.Add(o); var a = data.GetSkeletonData(true).FindAnimation(F(n)); o += a != null ? a.Duration : 0; }
            StopCut();
            cutCo = StartCoroutine(CutCo(names, offs, skips, endAt));
        }

        // 앞 몸짓의 줄이기(CutCo)가 아직 돌면 멈춘다 — 다음 수가 뒷정리 전에 오면(템포 2026-10-09) 새 몸짓을 앞 몸짓의 조각으로 잘못 세어 끊었다
        Coroutine cutCo;
        void StopCut() { if (cutCo != null) { StopCoroutine(cutCo); cutCo = null; } }

        IEnumerator CutCo(List<string> names, List<float> offs, List<(float from, float to)> skips, float endAt)
        {
            var st = Sa.AnimationState;
            var done = new HashSet<int>();
            int piece = 0;
            TrackEntry last = null;
            while (true)
            {
                var cur = st.GetCurrent(0);
                if (cur == null || cur.Loop) yield break;                 // 쉬는 동작까지 왔다
                if (cur != last) { if (last != null) piece++; last = cur; }
                if (piece >= names.Count) yield break;
                float g = offs[piece] + cur.TrackTime;
                if (g >= endAt)
                {
                    var idle = IdleName();
                    if (idle != null) st.SetAnimation(0, idle, true).MixDuration = 0.25f;
                    yield break;
                }
                for (int i = 0; i < skips.Count; i++)
                    if (!done.Contains(i) && g >= skips[i].from && g < skips[i].to)
                    {
                        done.Add(i);
                        cur.TrackTime += skips[i].to - g;                      // 같은 조각 안에서만 건너뛴다(만들 때 그렇게 고른다)
                    }
                yield return null;
            }
        }

        // 조각들의 모든 스파인 이벤트 시각(이어 튼 전체 초)과 조각 경계
        public (List<float> evs, List<float> offs, float total) ChainEvents(IList<string> names)
        {
            var evs = new List<float>(); var offs = new List<float>(); float o = 0;
            foreach (var n in names)
            {
                offs.Add(o);
                var a = data.GetSkeletonData(true).FindAnimation(F(n));
                if (a == null) continue;
                foreach (var tl in a.Timelines) if (tl is EventTimeline et) foreach (var ev in et.Events) evs.Add(o + ev.Time);
                o += a.Duration;
            }
            evs.Sort();
            return (evs, offs, o);
        }

        // 한 조각의 가운데부터 끝까지(원작 방식 고학년의 발동 조각 — 키샤 열기 폭발 · 이드(재활) 깨기). 길이(초, 속도 반영)를 돌려준다
        public float PlayPart(string anim, float from, float speed = 1f)
        {
            var a = anim == null ? null : data.GetSkeletonData(true).FindAnimation(F(anim));
            if (a == null) return 0;
            StopCut();
            from = Mathf.Clamp(from, 0, a.Duration);
            var e = Sa.AnimationState.SetAnimation(0, a, false);
            e.TimeScale = speed; e.MixDuration = 0.12f;
            e.AnimationStart = from; e.AnimationLast = from;
            var idle = IdleName();
            if (idle != null) Sa.AnimationState.AddAnimation(0, idle, true, 0).MixDuration = 0.2f;
            return (a.Duration - from) / Mathf.Max(0.01f, speed);
        }

        // 한 조각의 한 구간을 되풀이(이드(재활) 꿈속에서 잠 — 다음 차례까지)
        public void LoopPart(string anim, float from, float to)
        {
            var a = anim == null ? null : data.GetSkeletonData(true).FindAnimation(F(anim));
            if (a == null) return;
            var e = Sa.AnimationState.SetAnimation(0, a, true);
            e.AnimationStart = Mathf.Clamp(from, 0, a.Duration); e.AnimationEnd = Mathf.Clamp(to, e.AnimationStart + 0.05f, a.Duration);
            e.AnimationLast = e.AnimationStart; e.MixDuration = 0.1f;
        }

        public void Loop(string anim)
        {
            anim = F(anim);
            if (anim != null && Has(anim)) Sa.AnimationState.SetAnimation(0, anim, true).MixDuration = 0.15f;
        }

        public float Duration(string anim)
        {
            var a = anim == null ? null : data.GetSkeletonData(true).FindAnimation(anim);
            return a == null ? 0 : a.Duration;
        }

        // 때리는 순간 — 원작 스파인 이벤트로 짐작한다(웹판 strikeOf 와 같은 규칙).
        // 둘째 효과음(SFX 2 …)이 처음 나는 때, 없으면 이 유닛만의 첫 Event(값 ≥ 1000100), 그것도 없으면 길이의 45%.
        // marks — 그 뒤 이 유닛만의 Event · SFX 시각들(여러 번 때리는 창), 길어야 2.6초 뒤까지
        public List<float> Strikes(string anim)
        {
            var res = new List<float>();
            var a = anim == null ? null : data.GetSkeletonData(true).FindAnimation(anim);
            if (a == null) { res.Add(0.3f); return res; }
            var evs = new List<Spine.Event>();
            foreach (var tl in a.Timelines)
                if (tl is EventTimeline et) evs.AddRange(et.Events);
            evs.Sort((x, y) => x.Time.CompareTo(y.Time));
            bool Own(Spine.Event e)
            {
                if (e.Data.Name != "Event" && e.Data.Name != "Event_2") return false;
                foreach (var s in (e.String ?? "").Split(','))
                    if (int.TryParse(s, out var v) && v >= 1000100 && v != 1000013 && v != 1000014) return true;
                return false;
            }
            bool Sfx2(Spine.Event e) => e.Data.Name == "SFX" && int.TryParse(e.String, out var v) && v >= 2;
            Spine.Event hit = evs.Find(Sfx2) ?? evs.Find(Own);
            // 맞는 소리(SFX2)와 이 유닛 이펙트 표시가 0.4초 안에 둘 다 있으면 늦은 쪽 — 이펙트를 내기 시작하는 표시가 주먹이 닿기 전에 오는 일이 있다(디아나 2026-10-06)
            var s2 = evs.Find(Sfx2); var ow = evs.Find(Own);
            if (s2 != null && ow != null && Mathf.Abs(s2.Time - ow.Time) <= 0.4f) hit = s2.Time >= ow.Time ? s2 : ow;
            float at = hit != null ? hit.Time : a.Duration * 0.45f;
            res.Add(at);
            foreach (var e in evs)
            {
                if (e.Time <= at + 0.001f || e.Time > at + 2.6f) continue;
                if (!(Own(e) || Sfx2(e))) continue;
                if (res.Count > 0 && e.Time - res[res.Count - 1] < 0.05f) continue;
                res.Add(e.Time);
            }
            return res;
        }

        // ── 위치 ──
        public Vector3 Bone(string name, Vector3 fallbackLocal)
        {
            var b = Sa.Skeleton.FindBone(name);
            if (b == null) return transform.parent.InverseTransformPoint(Art.TransformPoint(fallbackLocal));
            var w = b.GetWorldPosition(Sa.transform);
            return transform.parent.InverseTransformPoint(w);
        }

        public float Height()
        {
            if (heightCache > 0) return heightCache;
            var b = mr.bounds;
            heightCache = Mathf.Clamp(b.size.y / transform.lossyScale.y, 1.2f, 6f);
            return heightCache;
        }

        /// <summary>지금 그려진 그림의 경계(월드) — 판정 상자에 쓴다. Height() 는 처음 잰 값을 붙들어 두어(등장 · 웅크림 때 재면) 그림보다 작을 수 있다.</summary>
        public Bounds ArtBounds => iconArt != null ? iconArt.bounds : mr.bounds;

        // ── 보이는 그림 상자 ── 쉬는 동작 그대로 한 번 그려(화면 밖 렌더 텍스처) 투명이 아닌 픽셀의 위아래 · 좌우 끝을 잰다.
        //   스파인 메시 상자는 그림 둘레 빈칸까지 들어 실제보다 크다 — 적 머리 위 묶음(HP 막대 …)과 적 자리를 이것으로 정한다.
        //   값은 발(이 오브젝트 자리) 기준 싸움터 단위. 같은 그림 · 스킨 · 크기 · 방향이면 한 번만 잰다(정적 표)
        static readonly Dictionary<string, Rect> visCache = new Dictionary<string, Rect>();
        Rect? vis;
        float scaleMul = 1f, scale0 = -1, iconW0 = -1;
        public float ScaleMul => scaleMul;

        /// <summary>보이는 그림 상자(발 기준, 지금 배율). 처음 부를 때 잰다.</summary>
        public Rect Visible
        {
            get
            {
                if (!vis.HasValue) vis = MeasureVisible();
                var r = vis.Value;
                return new Rect(r.x * scaleMul, r.y * scaleMul, r.width * scaleMul, r.height * scaleMul);
            }
        }

        /// <summary>몸 크기를 처음 크기의 mul 배로(적이 많아 화면에 다 못 서면 배치가 조금 줄인다).</summary>
        public void Rescale(float mul)
        {
            if (scale0 < 0) { scale0 = Scale; iconW0 = iconW; }
            scaleMul = mul;
            Art.localScale = new Vector3(mul, mul, 1);
            Scale = scale0 * mul;
            iconW = iconW0 * mul;
            heightCache = -1;
            travelCache.Clear();
        }

        Rect MeasureVisible()
        {
            var parent = transform.parent;
            Rect ToLocal(Bounds b)
            {
                var a = parent != null ? parent.InverseTransformPoint(b.min) : b.min;
                var c = parent != null ? parent.InverseTransformPoint(b.max) : b.max;
                var o = transform.localPosition + Art.localPosition;
                return Rect.MinMaxRect(a.x - o.x, a.y - o.y, c.x - o.x, c.y - o.y);
            }
            if (iconArt != null) return ToLocal(iconArt.bounds);
            string key = data.name + "|" + skin + "|" + Scale.ToString("F3") + "|" + Facing;
            if (visCache.TryGetValue(key, out var got)) return got;
            // 쉬는 동작을 몇 장면 짚어(통통 튀는 젤리처럼 몸이 오르내리는 적) 그 모두를 합친 실루엣으로 잰다
            var entry = Sa.AnimationState.GetCurrent(0);
            float t0 = entry != null ? entry.TrackTime : 0, dur = entry != null && entry.Animation != null ? entry.Animation.Duration : 0;
            int Samples = Mathf.Clamp(Mathf.CeilToInt(dur * 15f), 8, 40);   // 초당 15장면(짧게 튀어 오르는 순간도 잡게)
            int ns = dur > 0.05f ? Samples : 1;
            void Pose(int si) { if (entry != null && ns > 1) entry.TrackTime = t0 + dur * si / ns; Sa.Update(0); Sa.LateUpdate(); }
            Bounds wb = default;
            for (int si = 0; si < ns; si++) { Pose(si); if (si == 0) wb = mr.bounds; else wb.Encapsulate(mr.bounds); }
            Rect res = ToLocal(wb);
            if (wb.size.x > 0.01f && wb.size.y > 0.01f && SystemInfo.supportsRenderTextures)
            {
                const float Ppu = 96f;
                int w = Mathf.Clamp(Mathf.CeilToInt(wb.size.x * Ppu), 8, 512), h = Mathf.Clamp(Mathf.CeilToInt(wb.size.y * Ppu), 8, 512);
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                Texture2D tex = null;
                try
                {
                    for (int si = 0; si < ns; si++)
                    {
                        Pose(si);
                        var cb = new UnityEngine.Rendering.CommandBuffer { name = "unit visible box" };
                        cb.SetRenderTarget(rt);
                        if (si == 0) cb.ClearRenderTarget(false, true, Color.clear);
                        cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(wb.min.x, wb.max.x, wb.min.y, wb.max.y, -1000f, 1000f));
                        var mats = mr.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++) if (mats[i] != null) cb.DrawRenderer(mr, mats[i], i, 0);
                        Graphics.ExecuteCommandBuffer(cb);
                        cb.Release();
                    }
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                    RenderTexture.active = prev;
                    var px = tex.GetPixels32();
                    int x0 = w, x1 = -1, y0 = h, y1 = -1;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            if (px[y * w + x].a > 40) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
                    if (x1 >= x0 && y1 >= y0)
                    {
                        var vb = new Bounds();
                        vb.SetMinMax(new Vector3(wb.min.x + x0 * wb.size.x / w, wb.min.y + y0 * wb.size.y / h, 0),
                                     new Vector3(wb.min.x + (x1 + 1) * wb.size.x / w, wb.min.y + (y1 + 1) * wb.size.y / h, 0));
                        res = ToLocal(vb);
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("[Unit] 그림 상자 재기 실패 " + name + " — " + e.Message); }
                finally
                {
                    RenderTexture.ReleaseTemporary(rt);
                    if (tex != null) Destroy(tex);
                    if (entry != null && ns > 1) { entry.TrackTime = t0; Sa.Update(0); Sa.LateUpdate(); }
                }
            }
            Debug.Log($"[Unit] 그림 상자 {name}: 메시 {ToLocal(wb)} → 보임 {res} (쉬는 동작 {ns}장면)");
            visCache[key] = res;
            return res;
        }

        /// <summary>그려진 몸(스파인 메시)의 월드 경계 — 화면 밖 막기(고학년 몸짓의 루트 이동)</summary>
        public Bounds MeshBounds => mr != null ? mr.bounds : new Bounds(transform.position, Vector3.zero);

        public float Width() => iconArt != null ? iconW : Mathf.Clamp(mr.bounds.size.x / transform.lossyScale.x, 0.8f, 6f);

        // 몸 가운데(싸움터 좌표) — 맞는 자리
        public Vector3 Center => Bone("Point_Middle", new Vector3(0, Height() * 0.42f, 0));
        public Vector3 Top => Bone("Point_Top", new Vector3(0, Height() * 0.85f, 0));
        public Vector3 Feet => transform.localPosition + Art.localPosition;

        // 원작 이펙트(com.bolzena.fx) 쪽 몸 — 자리는 모두 싸움터 좌표(BolzenaFx.Parent = FieldRoot).
        // 원작 SD 의 자리 본(Point_Bottom · Middle · Top · Front · Attack1 · Skill1 · Ult1 …)을 그대로 쓴다. 고학년 · 카드 · 맞음 · 공용이 여기를 본다
        Bolzena.Fx.FxActor fx;
        readonly Dictionary<string, string> pointBones = new Dictionary<string, string>();
        public string FxKey;                 // 사도 id(에르핀) · 적 키 — 총구 표(MotionTables.MUZZLE)를 찾는다
        public Bolzena.Fx.FxActor Fx
        {
            get
            {
                if (fx != null) return fx;
                System.Func<Vector3?> muzzle = null;
                if (SpineArt && Facing)
                {
                    var m = Bolzena.Fx.SpineMotion.Muzzle(Sa, FxKey);
                    if (m != null) muzzle = () => { var w = m(); return w.HasValue && this ? transform.parent.InverseTransformPoint(w.Value) : (Vector3?)null; };
                }
                fx = new Bolzena.Fx.FxActor
                {
                    FeetAt = () => this ? Feet : Vector3.zero, Height = Height(), Party = Facing, Key = FxKey ?? name,
                    Point = PointAt, Muzzle = muzzle,
                };
                return fx;
            }
        }

        // 몸짓이 스스로 앞으로 가는 거리(싸움터 단위) — 조각들을 처음 자세에서 이어 돌려 몸(Point_Middle, 없으면 뿌리 아래 첫 뼈)이
        // 앞(적 쪽)으로 가장 멀리 간 만큼. 순간이동 · 뛰어들기처럼 애니가 제 몸을 옮기는 고학년은 크다(그러면 따로 달려가지 않는다)
        readonly Dictionary<string, float> travelCache = new Dictionary<string, float>();
        public float Travel(IList<string> names)
        {
            float best = 0;
            if (names != null) foreach (var n in names) best = Mathf.Max(best, TravelOne(n));
            return best;
        }

        // 조각 하나 — 처음 자세에서 돌려 본 몸의 앞쪽 최대 이동(싸움터 단위). 싸움을 열 때 미리 재 둔다(PrewarmTravel)
        public float TravelOne(string n)
        {
            if (!SpineArt || n == null) return 0;
            if (travelCache.TryGetValue(n, out var got)) return got;
            var sd = data.GetSkeletonData(true);
            var a = sd.FindAnimation(n);
            if (a == null) return travelCache[n] = 0;
            var sk = new Skeleton(sd);
            sk.SetToSetupPose(); sk.UpdateWorldTransform();
            var b = sk.FindBone("Point_Middle") ?? (sk.RootBone.Children.Count > 0 ? sk.RootBone.Children.Items[0] : sk.RootBone);
            float x0 = b.WorldX, best = 0;
            for (float t = 0; t <= a.Duration + 1e-4f; t += 1f / 15f)
            {
                sk.SetToSetupPose();
                a.Apply(sk, t, t, false, null, 1f, MixBlend.Setup, MixDirection.In);
                sk.UpdateWorldTransform();
                best = Mathf.Max(best, -(b.WorldX - x0));   // 원작 SD 는 왼쪽을 본다 — 앞 = -x
            }
            return travelCache[n] = best * Scale;
        }

        public void PrewarmTravel()
        {
            if (!SpineArt) return;
            foreach (var a in data.GetSkeletonData(true).Animations)
                if (a.Name.StartsWith("Ultimate", System.StringComparison.OrdinalIgnoreCase)) TravelOne(a.Name);
        }

        // 자리 본 「Point_<p>」(없으면 그것으로 시작하는 본 — Point_Attack1_Shot · 뒤가 _T 인 대상 표시는 빼고). 그림 한 장이면 null
        public static System.Action<string, string> BoneMiss;
        Vector3? PointAt(string p)
        {
            if (!this || !SpineArt || Sa.Skeleton == null) return null;
            if (!pointBones.TryGetValue(p, out var bn) && (p == "WeaponTip" || p == "HandR"))
            {
                // 무기 끝 · 오른손 — 원작 고학년 쏘는 자리가 몸 앞 고정 점(Point_Attack1)뿐인 사도(에르핀(왕도) · 아일라 · 포셔 …)를 위해:
                // 이름에 Weapon · Gun · Barrel · Staff · Wand 가 든 뼈 가운데 자식이 없는(끝) 것 중 처음 자세에서 몸 가운데에서 가장 먼 것
                bn = null;
                if (p == "WeaponTip")
                {
                    var mid = Sa.Skeleton.FindBone("Point_Middle");
                    float best = -1;
                    foreach (var b in Sa.Skeleton.Bones)
                    {
                        var nm = b.Data.Name;
                        if (b.Children.Count > 0 || !System.Text.RegularExpressions.Regex.IsMatch(nm, "(?i)(weapon|gun|barrel|staff|wand|cane)") || nm.EndsWith("_T")) continue;
                        float dd = mid != null ? (b.WorldX - mid.WorldX) * (b.WorldX - mid.WorldX) + (b.WorldY - mid.WorldY) * (b.WorldY - mid.WorldY) : b.WorldX * b.WorldX + b.WorldY * b.WorldY;
                        if (dd > best) { best = dd; bn = nm; }
                    }
                }
                else bn = Sa.Skeleton.FindBone("Hand_R") != null ? "Hand_R" : null;
                pointBones[p] = bn;
            }
            if (!pointBones.TryGetValue(p, out bn))
            {
                string want = "Point_" + p;
                bn = Sa.Skeleton.FindBone(want) != null ? want : null;
                if (bn == null)
                    foreach (var b in Sa.Skeleton.Bones)
                        if (b.Data.Name.StartsWith(want, System.StringComparison.OrdinalIgnoreCase) && !b.Data.Name.EndsWith("_T")) { bn = b.Data.Name; break; }
                pointBones[p] = bn;
                if (bn == null) BoneMiss?.Invoke(FxKey ?? name, p);   // 점검 — 이펙트가 붙을 자리 본을 못 찾음(대신 짐작한 자리)
            }
            return bn != null ? Bone(bn, Vector3.zero) : (Vector3?)null;
        }

        TMPro.TextMeshPro plateText;
        public void SetOrder(int order)
        {
            mr.sortingOrder = order;
            shadow.sortingOrder = order - 1;
            if (iconArt != null) iconArt.sortingOrder = order;             // 스파인 대신 선 아이콘 · 이름 판도 같은 차례로
            if (plateText != null) plateText.sortingOrder = order + 1;
        }

        // ── 색 · 번쩍임 ──
        // peak — 채우기 세기(1 이면 온통 그 색 실루엣). 맞는 순간은 0.5 안팎으로 둬야 몸이 읽힌다
        public void Flash(Color c, float dur = 0.14f, float peak = 1f)
        {
            fillColor = c;
            fillPhase = Mathf.Max(fillPhase, peak);
            fillDecay = peak / Mathf.Max(0.01f, dur);
        }
        float fillDecay = 7f;

        public void Tint(Color c, float speed = 10f) { tintTarget = c; tintSpeed = speed; }
        float tintSpeed = 10f;

        public void SetAlpha(float a) { alpha = a; }
        float alpha = 1f;

        // ── 움직임 ──
        // 밀려남 — dir 쪽으로 dist 만큼 휙 밀렸다 튕겨 돌아온다
        public void Knock(float dir, float dist, float dur = 0.28f)
        {
            StartCoroutine(KnockCo(dir, dist, dur));
        }

        IEnumerator KnockCo(float dir, float dist, float dur)
        {
            yield return Clock.Tween(dur, t =>
            {
                float k = t < 0.18f ? Ease.OutCubic(t / 0.18f) : 1 - Ease.InOutCubic((t - 0.18f) / 0.82f);
                knock = new Vector3(dir * dist * k, -0.02f * k, 0);
            });
            knock = Vector3.zero;
        }

        // 몸을 to(싸움터 좌표 — 발 자리)로 dur 동안 옮긴다
        public IEnumerator MoveTo(Vector3 to, float dur, System.Func<float, float> ease = null)
        {
            var from = offset;
            var target = to - Home;
            ease = ease ?? Ease.OutCubic;
            yield return Clock.Tween(dur, t => offset = Vector3.LerpUnclamped(from, target, ease(t)));
        }

        public IEnumerator Return(float dur = 0.25f) => MoveTo(Home, dur, Ease.InOutCubic);

        // 잔상 — 지금 몸짓 그대로 멈춘 실루엣을 남기고 흐려진다
        public void Ghost(Color c, float life = 0.32f)
        {
            var g = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            g.transform.SetParent(transform.parent, false);
            g.transform.position = Sa.transform.position;
            g.transform.localScale = new Vector3(Scale, Scale, 1);
            { var wear = Bolzena.Fx.SpineMotion.Wear(g.Skeleton.Data, skin); if (wear != null) { g.Skeleton.SetSkin(wear); g.Skeleton.SetSlotsToSetupPose(); } }
            g.Skeleton.ScaleX = Sa.Skeleton.ScaleX;
            var cur = Sa.AnimationState.GetCurrent(0);
            if (cur != null)
            {
                var e = g.AnimationState.SetAnimation(0, cur.Animation, false);
                e.TrackTime = cur.TrackTime;
                g.AnimationState.Apply(g.Skeleton);
                g.Skeleton.UpdateWorldTransform();
            }
            g.timeScale = 0;
            var r = g.GetComponent<MeshRenderer>();
            r.sortingOrder = mr.sortingOrder - 2;
            var block = new MaterialPropertyBlock();
            StartCoroutine(GhostCo(g, r, block, c, life));
        }

        IEnumerator GhostCo(SkeletonAnimation g, MeshRenderer r, MaterialPropertyBlock block, Color c, float life)
        {
            yield return Clock.Tween(life, t =>
            {
                if (g == null) return;
                g.Skeleton.A = c.a * (1 - t);
                block.SetColor("_FillColor", c);
                block.SetFloat("_FillPhase", 1f);
                r.SetPropertyBlock(block);
            });
            if (g != null) Destroy(g.gameObject);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Art.localPosition = offset + knock + Nudge;
            if (fillPhase > 0) fillPhase = Mathf.Max(0, fillPhase - dt * fillDecay);
            mpb.SetColor("_FillColor", fillColor);
            mpb.SetFloat("_FillPhase", fillPhase);
            mr.SetPropertyBlock(mpb);
            tint = Color.Lerp(tint, tintTarget, 1 - Mathf.Exp(-Time.unscaledDeltaTime * tintSpeed));
            Sa.Skeleton.R = tint.r;
            Sa.Skeleton.G = tint.g;
            Sa.Skeleton.B = tint.b;
            Sa.Skeleton.A = alpha;
            var sc = shadow.color;
            sc.a = 0.75f * alpha;
            shadow.color = sc;
            shadow.transform.localPosition = new Vector3(offset.x, offset.y + 0.02f, 0);
        }
    }
}
