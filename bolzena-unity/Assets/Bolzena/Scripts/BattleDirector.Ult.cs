using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 고학년 재생 — 흐름(UltFlow) · 스탠딩 · 레이저 · 한 바퀴 · 줄이기(PlanCut) · 이동 · 소리 · 마무리
    //   2026-11-17(12주차) BattleDirector.Acts.cs 에서 글자 그대로 옮겼다 — 동작 · 값 그대로(ADR-004).
    public partial class BattleDirector
    {
        const int STANDING_ORDER = 345;   // 사도(330) · 고학년 이펙트(325) 위, 숫자(355) · 글(475) 아래

        // 레이저 줄기 — 원작 레이저 장의 줄(Custom_Laser · FX_UI_PopupBreadCrumbRuinsGacha_Line)은 입체 메시라 이펙트 판이 못 그리고 빼 왔다(2026-10-14 원작 대조:
        //   비비(신성) 고학년 레이저가 한 번도 안 보였다). 그 텍스처를 손끝 → 적으로 늘여 굵은 청색 줄기로 그린다. 텍스처가 없으면 흰 판
        static Sprite laserSprite;
        IEnumerator LaserBeamCo(string fxName, Vector3 from, Vector3 to, float dur)
        {
            if (laserSprite == null)
            {
                Texture2D tex = null;
                var fx = Bolzena.Fx.FxLibrary.Effect(fxName);
                if (fx != null) foreach (var e in fx.Em) if (e.M3d && e.Tex != null && e.N != null && e.N.Contains("Laser")) { tex = e.Tex; break; }
                laserSprite = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : Res.UI("white");
            }
            var lf = FieldRoot.InverseTransformPoint(from); var lt = FieldRoot.InverseTransformPoint(to);   // 손끝은 월드 좌표(스탠딩 본) — 싸움터 좌표로
            var dir = lt - lf; dir.z = 0;
            float len = dir.magnitude + 2.5f, ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var root = Make.Node("laser_beam", FieldRoot, new Vector3(lf.x, lf.y, 0));
            root.localRotation = Quaternion.Euler(0, 0, ang);
            var core = Make.Box("core", root, laserSprite, new Vector3(len / 2, 0, 0), new Vector2(len, 0.55f), STANDING_ORDER + 3, new Color(0.85f, 0.97f, 1f, 0f), Res.SpriteMat(true, 2.2f));
            var glow = Make.Box("glow", root, laserSprite, new Vector3(len / 2, 0, 0), new Vector2(len, 1.6f), STANDING_ORDER + 2, new Color(0.3f, 0.65f, 1f, 0f), Res.SpriteMat(true, 1.6f));
            core.transform.localPosition = glow.transform.localPosition = new Vector3(len / 2, 0, 0);
            float t = 0;
            while (t < dur && InUlt)
            {
                float a = Mathf.Min(1f, t / 0.12f) * (t > dur - 0.25f ? Mathf.Max(0, (dur - t) / 0.25f) : 1f);
                float pulse = 1f + 0.12f * Mathf.Sin(t * 40f);
                Make.Alpha(core, a); Make.Alpha(glow, a * 0.8f);
                root.localScale = new Vector3(Mathf.Min(1f, t / 0.12f), pulse, 1);
                t += Time.deltaTime; yield return null;
            }
            if (root) Destroy(root.gameObject);
        }

        // 레이저 여러 줄기 — 화면 왼쪽 밖 높이를 바꿔(가운데 → 위 → 아래 → 가운데 넓게) 적 무리 쪽으로. 줄기마다 원작 레이저 장도 그 시작점에서
        static readonly float[] VOLLEY_Y = { 0.55f, -0.5f, 0.25f, -0.15f };   // 원작은 위 · 아래로 비스듬히 엇갈린다
        // (10-16) 원작 레이저 4줄기 = fx_vividivine_ultimate_full_laser_1~4 — 제 크기(약 1배)로 싸움터 안 자리 · 방향까지 짜인 장. 전에 쓰던 _one_jolly_default 는
        //   0.024배 작은 판이라 「원작 레이저는 2 단위뿐」으로 알고 그림을 늘여 직접 그렸다(사용자 「원작에 없어서 임의로 그리는 건가」). 원작 파티클을 쓰면 4줄기 장을 적 무리 가운데에 그대로
        // 원작 장 안 맞음 지점(FL 이미터 자리 × 0.3 · 카메라 기울기 −20° 기준, 우리 단위 — y′ = (y·cos20 − z·sin20) × 0.3) — PsLog 로 잰 laser_1 (9.2, 4.0)과 같은 셈
        static readonly Vector2[] VIVI_LASER_HIT = { new Vector2(9.24f, 3.06f), new Vector2(9.45f, -3.01f), new Vector2(-3.21f, -3.2f), new Vector2(-10.35f, -2.06f) };
        // 줄기 방향(장 안 축 · 카메라 기울기 −20° 반영, 우리 단위) — 1 · 2 는 FL(맞음)에서 왼쪽으로, 3 · 4 는 FL(쏘는 끝)에서 오른쪽으로 뻗는다(프리팹 Custom_Laser_* · Custom_FL 위치 · 회전으로 셈, 10-18)
        static readonly Vector2[] VIVI_LASER_DIR = { new Vector2(-0.995f, 0.09f), new Vector2(-0.983f, 0.18f), new Vector2(0.988f, 0.152f), new Vector2(0.948f, 0.317f) };
        static readonly string[] VIVI_LASERS = { "fx_vividivine_ultimate_full_laser_1", "fx_vividivine_ultimate_full_laser_2", "fx_vividivine_ultimate_full_laser_3", "fx_vividivine_ultimate_full_laser_4" };
        IEnumerator LaserVolleyCo(string fxName, Transform root, float hw, float hh, Vector3 aim, List<int> targets, float dur)
        {
            if (Bolzena.Fx.BolzenaFx.UsePs && System.Array.TrueForAll(VIVI_LASERS, n => Bolzena.Fx.FxLibrary.Has(n)))
            {
                // (10-18 사용자 4차 「레이저 개수가 너무 적다」) 원작 영상 3.27~4.07초 약 8줄기(0.07~0.13초 간격) — 4줄기를 두 바퀴, 둘째 바퀴는 대상을 하나씩 밀어 다른 적에 꽂힌다
                const int ROUNDS = 2;
                int n = VIVI_LASERS.Length * ROUNDS;
                float g = Mathf.Min(0.12f, dur / (n + 1));
                for (int s = 0; s < n && InUlt; s++)
                {
                    int i = s % VIVI_LASERS.Length, r = s / VIVI_LASERS.Length;
                    // 장마다 섬광 · 맞음 지점(FL)이 원점에서 떨어져 짜였다(원작 4줄기가 싸움터 여기저기에 꽂힌다) — 빛줄기 방향은 원작 그대로 두고 그 지점이 맞는 적 몸에 오게 원점을 옮긴다
                    var hit = targets.Count > 0 ? Enemies[targets[(i + r) % targets.Count]].Center : aim;
                    var off = VIVI_LASER_HIT[i] * (Bolzena.Fx.BolzenaFx.UnitWorld / 0.3f);
                    float life = Mathf.Max(0.35f, Mathf.Min(0.6f, dur - s * g));   // 원작 한 줄기는 0.3~0.5초 보이고 다음 줄기로 — 1초 가까이 남기면 굵은 몇 줄로만 보였다
                    var at = hit - (Vector3)off;
                    if (VIVI_LASER_DIR[i].x > 0)   // (10-18 사용자 「레이저가 대상에서 나온다」) 3 · 4 — FL 이 쏘는 끝: 화면 왼쪽 밖에 두고 줄기가 이 적을 지나게(원작: 모든 줄기가 왼쪽 밖에서)
                    {
                        var cam = Camera.main;
                        // (10-18 2차) 화면 왼끝은 월드 — 싸움터 좌표(hit)와 섞지 않게 옮긴다(고학년 카메라가 싸움터를 확대 · 이동한다)
                        float left = cam != null && cam.orthographic ? FieldRoot.InverseTransformPoint(new Vector3(cam.transform.position.x - cam.orthographicSize * cam.aspect - 0.5f, 0, 0)).x : hit.x - 9f;
                        float d = (hit.x - left) / VIVI_LASER_DIR[i].x;
                        at -= (Vector3)(VIVI_LASER_DIR[i] * d);
                    }
                    // (10-18 2차) 자리(hit · at)는 싸움터 좌표 — 싸움터 아래에 틀어야 고학년 확대 · 이동 동안 맞는 적에 그대로 꽂힌다(전엔 부모 없이 월드로 놓였다)
                    Bolzena.Fx.BolzenaFx.Play(VIVI_LASERS[i], new Bolzena.Fx.FxPlayOptions { At = at, Until = life, Order = STANDING_ORDER + 2, Parent = FieldRoot });
                    // 빛줄기 몸통 — 원작 장의 레이저 이미터는 굵기 0 으로 비어 있고 원작 게임이 실행 중에 코드로 채운다(10-16 확인). 그 몫만 원작 레이저 그림을 늘여 대신 그린다(화면 왼쪽 밖 → 그 적)
                    // (10-17 3차 조사) 원작 줄기는 메시 입자(원통을 시작 크기 x 0 으로 눌러 띠로 — 비어 있는 게 아니었다) — 직접 그리던 줄기(LaserBeamCo)는 원작이 아니라 뺐다
                    yield return Clock.Wait(g);
                }
                yield break;
            }
            float gap = Mathf.Min(0.28f, dur / (VOLLEY_Y.Length + 1));
            for (int i = 0; i < VOLLEY_Y.Length && InUlt; i++)
            {
                var from = root.TransformPoint(new Vector3(-hw - 1.5f, hh * VOLLEY_Y[i], 0));
                var to = targets.Count > 1 ? Enemies[targets[i % targets.Count]].Center : aim + new Vector3(0, (i % 2 == 0 ? 0.25f : -0.25f), 0);
                float life = Mathf.Max(0.45f, dur - i * gap);
                Bolzena.Fx.BolzenaFx.Play(fxName, new Bolzena.Fx.FxPlayOptions { At = from, Aim = true, AimAt = to, Until = life, Order = STANDING_ORDER + 2 });
                StartCoroutine(LaserBeamCo(fxName, from, to, life));   // 원작 레이저 메시는 프리팹 안에서 2 단위뿐이고 원작은 코드로 적까지 늘인다 — 원작 그림을 적까지 늘여 그대로(원작 그대로 모드에서도)
                yield return Clock.Wait(gap);
            }
        }

        IEnumerator UltStandingCo(UltStanding c, float tStart, float speed, List<int> targets)
        {
            float S() => (Time.time - tStart) * speed;   // 스파인 초(1배속) — 몸짓 · 이펙트 · 타격이 같이 보는 시계
            while (S() < c.Start) { if (!InUlt) yield break; yield return null; }
            var data = Res.Spine(c.Spine);
            float hw = Tone.HalfW, hh = Tone.HalfH;
            var root = Make.Node("UltStanding", ScreenRoot);
            Spine.Unity.SkeletonAnimation st = null;
            Spine.Bone point = null;
            float len = c.Out - c.Start + 0.6f;
            if (data != null)
            {
                st = Spine.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                st.transform.SetParent(root, false);
                var sd = data.GetSkeletonData(true);
                var anim = sd.FindAnimation("Highlight_1") ?? (sd.Animations.Count > 0 ? sd.Animations.Items[0] : null);
                if (anim != null)
                {
                    var tr = st.AnimationState.SetAnimation(0, anim, false);
                    tr.TrackTime = S() - c.Start;   // 기다리다 늦은 몫만큼 앞으로(시계에 맞춤)
                    len = anim.Duration;
                }
                st.timeScale = speed;
                // 화면 높이(2hh) 기준 — 영상 px ÷ 428 = 화면 높이 비율. 스파인 1 단위(원본) = Unit × 2hh, 유니티 데이터는 data.scale 배로 읽혀 있다
                float sc = c.Unit * hh * 2 / Mathf.Max(0.0001f, data.scale);
                st.transform.localScale = new Vector3(sc, sc, 1);
                st.transform.localPosition = new Vector3(-hw + c.OX * hh * 2, hh - c.OY * hh * 2, 0);
                st.GetComponent<MeshRenderer>().sortingOrder = STANDING_ORDER;
                point = st.Skeleton.FindBone("Point_Ult");
            }
            ScreenFx.I.Dim(0.88f, 300, 8f);   // 원작 — 클로즈업 동안 싸움터가 거의 검게
            // 레이저 — Point_Ult 에서 맞는 적(무리 가운데 몸) 쪽으로
            while (S() < c.Laser) { if (!InUlt) break; yield return null; }
            if (InUlt && c.Fx != null && targets.Count > 0)
            {
                var aim = targets.Count > 1 ? MidBody(targets) : Enemies[targets[0]].Center;
                // 원작 — 레이저는 클로즈업 손에서 나가지 않는다. 화면 왼쪽 밖(클로즈업 뒤)에서 여러 줄기가 높이 · 각을 바꿔 차례로 적에게 꽂힌다(원작 영상 3.3~4.2초, 2026-10-15 사용자 「자기 손에서 나가는 게 아닐 텐데」)
                StartCoroutine(LaserVolleyCo(Bolzena.Fx.UltFx.MoodName(c.Fx), root, hw, hh, aim, targets, Mathf.Min(1.0f, Mathf.Max(0.6f, (c.Out - c.Laser) / speed - 0.2f))));   // 원작 레이저는 약 1초(3.3 → 4.3초) 뒤 세로 빛기둥으로 넘어간다
            }
            // 클로즈업이 위로 날아 나가기 시작하면 싸움터 어둠은 고학년 어둠으로
            while (S() < c.Out) { if (!InUlt) break; yield return null; }
            ScreenFx.I.Dim(0.7f, 300, 5f);
            // 몸짓 끝까지(나가는 몸짓이 스파인 안에 있다). 고학년이 먼저 끝나면 0.15초에 걷는다
            while (S() < c.Start + len) { if (!InUlt) break; yield return null; }
            if (st != null && S() < c.Start + len)
            {
                float t0 = Time.time;
                while (Time.time - t0 < 0.15f) { st.Skeleton.A = 1 - (Time.time - t0) / 0.15f; yield return null; }
            }
            Destroy(root.gameObject);
        }

        IEnumerator UltLoopCo(UnitView u, UltLoop lp, float dur)
        {
            var cam = Camera.main;
            if (u == null || u.Art == null || cam == null || u.Sa == null) yield break;
            var shadow = u.Sa.Skeleton.FindBone("Point_Shadow") ?? u.Sa.Skeleton.RootBone;
            var topB = u.Sa.Skeleton.Data.FindBone("Point_Top");
            float hw = (topB != null && Mathf.Abs(topB.Y) > 1f ? Mathf.Abs(topB.Y) : 600f) * Mathf.Abs(u.Sa.transform.lossyScale.y);   // 키 H(월드)
            float push = 0, t = 0, shift = float.NaN;   // push — 화면 밖 막기(Art 단위) · shift — 무리 뒤로 옮긴 몫(월드)
            while (t < dur && u)
            {
                float sx = Mathf.Max(0.001f, u.Art.parent.lossyScale.x);
                var cur = u.Sa.AnimationState.GetCurrent(0);
                float ms = cur != null ? cur.TrackTime * 1000f : 0;
                float nudgeW = u.Nudge.x * sx;
                float halfW = cam.orthographicSize * cam.aspect, camX = cam.transform.position.x;
                if (float.IsNaN(shift) && ms >= lp.From)
                {
                    // 숨은 첫 프레임 — 그림자 본(옮김 뺀)이 무리 오른끝 + 몸 반폭(0.7H) + Gap 에 오게. 나타날 때 몸 오른끝(+0.9H)이 화면 밖으로 안 나가게
                    float sxW = u.Sa.transform.TransformPoint(new Vector3(shadow.WorldX, shadow.WorldY, 0)).x - nudgeW;
                    float right = float.MinValue;
                    for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) right = Mathf.Max(right, Enemies[i].MeshBounds.max.x);
                    shift = right > float.MinValue ? Mathf.Max(0, Mathf.Min(right + (0.7f + lp.Gap) * hw, camX + halfW - 0.25f - 0.9f * hw) - sxW) : 0;
                }
                float w = float.IsNaN(shift) || ms < lp.From ? 0 : ms <= lp.Hold ? 1 : ms >= lp.Back ? 0 : 1 - Mathf.SmoothStep(0, 1, (ms - lp.Hold) / Mathf.Max(1, lp.Back - lp.Hold));
                float moveW = shift > 0 ? shift * w : 0;
                // 화면 밖 막기 — 몸이 보일 때만(숨은 동안 경계는 0)
                var b = u.MeshBounds;
                float need = 0;
                if (b.size.x > 0.01f)
                {
                    float min0 = b.min.x - nudgeW + moveW, max0 = b.max.x - nudgeW + moveW;   // 밀지 않았다면(옮김은 넣고)
                    need = min0 < camX - halfW + 0.25f ? camX - halfW + 0.25f - min0 : max0 > camX + halfW - 0.25f ? camX + halfW - 0.25f - max0 : 0;
                }
                push = Mathf.MoveTowards(push, need / sx, Time.deltaTime * 40f);
                u.Nudge = new Vector3(moveW / sx + push, 0, 0);
                yield return null; t += Time.deltaTime;
            }
            if (!u) yield break;
            float from = u.Nudge.x;
            yield return Clock.Tween(0.25f, k => { if (u) u.Nudge = new Vector3(from * (1 - k), 0, 0); });
            if (u) u.Nudge = Vector3.zero;
        }
        IEnumerator KeepOnScreen(UnitView u, float dur)
        {
            var cam = Camera.main;
            if (u == null || u.Art == null || cam == null) yield break;
            float push = 0, t = 0;
            while (t < dur && u)
            {
                float sx = Mathf.Max(0.001f, u.Art.parent.lossyScale.x);
                float left = cam.transform.position.x - cam.orthographicSize * cam.aspect + 0.25f;
                float out0 = left - (u.MeshBounds.min.x - push * sx);   // 밀지 않았다면 왼끝이 화면 밖으로 나간 몫(월드)
                push = Mathf.MoveTowards(push, Mathf.Max(0, out0) / sx, Time.deltaTime * 40f);   // 12 → 40(2026-10-13 우로스 2.0초 — 몸짓이 순간 뒤로 물러날 때 못 따라가 반쯤 걸렸다)
                u.Nudge = new Vector3(push, 0, 0);
                yield return null; t += Time.deltaTime;
            }
            if (!u) yield break;
            float from = push;
            yield return Clock.Tween(0.25f, k => { if (u) u.Nudge = new Vector3(from * (1 - k), 0, 0); });
            if (u) u.Nudge = Vector3.zero;
        }

        // ── 긴 고학년 줄이기 ── 이어 튼 조각에서 아무 일(스파인 이벤트 · 원작 타격)도 없는 2.5초 넘는 구간은 가운데를 건너뛰고(앞뒤 0.5초 남김),
        // 마지막 일 뒤로 1.5초 넘게 남으면 1초 뒤에 쉬는 동작으로. 시각은 모두 Rm 으로 옮긴다(타격 · 이펙트 · 소리 · 이동)
        readonly List<(float from, float to)> cutSkips = new List<(float, float)>();
        float cutEnd = float.MaxValue;
        public int UltCutSkipMs, UltCutFrontMs, UltCutBackMs;   // 줄인 몫 — 앞(첫 타격 전) · 뒤(마지막 타격 뒤)                        // 점검 — 줄인 몫(ms, 1배속)
        // ── 고학년 보기 길이(설정) ── 기본: 판에서 처음 쓰는 고학년만 길게(원작 길이 · 7초 원칙), 두 번째부터 짧게(ULT_SHORT 초 남짓 · 1.15배).
        //   설정 「고학년 짧게 보기」 를 켜면 늘 짧게. 판 밖(전투 시범 · 점검)은 늘 길게(-ultshort 면 짧게 — 점검)
        const float ULT_SHORT = 3.5f;
        public bool UltShortShown;                       // 점검 — 이번 고학년을 짧게 보였나
        static long ultSeenRun = long.MinValue;
        static readonly HashSet<string> ultSeen = new HashSet<string>();
        static bool UltShortNow(string hid)
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultshort") >= 0) return true;
            if (Bolzena.RunUI.Settings.UltShort) return true;
            var seed = BattleBridge.Fight?.Port?.S?.Seed;
            if (seed == null) return false;
            if (seed.Value != ultSeenRun) { ultSeenRun = seed.Value; ultSeen.Clear(); }
            return !ultSeen.Add(hid);
        }

        void PlanCut(UnitView u, string hidUlt, float need = float.MaxValue)
        {
            var names = new List<string> { UltPlan.Anim }; names.AddRange(UltPlan.Chain);
            var (evs, offs, total) = u.ChainEvents(names);
            var uh = Bolzena.Fx.UltMotion.Hits(hidUlt);
            var hitsS = new List<float>();
            if (UltFitNow != null && UltFitNow.Hits != null && UltFitNow.Hits.Length > 0) foreach (var t in UltFitNow.Hits) hitsS.Add(t / 1000f);   // 원작 영상 맞춤 타격
            else if (uh != null && uh.Times.Count > 0) foreach (var t in uh.Times) hitsS.Add(t / 1000f);
            else if (UltPlan.Table != null) foreach (var t in UltPlan.Table.Strikes()) hitsS.Add(t / 1000f);
            if (need < float.MaxValue) { PlanCutNeed(evs, offs, total, hitsS, need); return; }
            evs.AddRange(hitsS);
            if (UltPlan.Table != null && UltPlan.Table.Moves) { evs.Add(UltPlan.Table.GoMs / 1000f); evs.Add(UltPlan.Table.LandMs / 1000f); }
            evs.Sort();
            if (evs.Count == 0) return;
            int PieceOf(float t) { int p = 0; for (int i = 0; i < offs.Count; i++) if (t >= offs[i]) p = i; return p; }
            float skipped = 0;
            if (CUT_FIX.TryGetValue(hidUlt, out var fix)) foreach (var k in fix) { cutSkips.Add(k); skipped += k.to - k.from; }
            // 앞 — 첫 타격 전 준비(기 모으기 · 대기)만. 때리는 가운데(첫 타격 ~ 마지막 타격)는 건드리지 않는다(사용자 원칙 2026-10-06).
            // 첫 타격이 2초 넘게 늦으면 그 앞 0.6초 ~ 첫 타격 0.8초 전을 건너뛴다(같은 조각 안일 때만 — 조각 경계를 넘으면 앞 조각 끝까지)
            UltCutFrontMs = 0; UltCutBackMs = 0;
            float firstHit = hitsS.Count > 0 ? hitsS[0] : (evs.Count > 0 ? evs[0] : 0);
            if (firstHit > 2f)
            {
                float a = 0.6f, b = firstHit - 0.8f;
                int pa = PieceOf(a), pb = PieceOf(b);
                if (pa != pb) b = offs[pa + 1] - 0.05f;
                if (b - a > 0.3f) { cutSkips.Add((a, b)); skipped += b - a; UltCutFrontMs = Mathf.RoundToInt((b - a) * 1000); }
            }
            // 꼬리 — 마지막 타격 뒤 2초 넘게 남으면 1.2초 뒤에 쉬는 동작으로(림(혼돈) 공중에서 2초 서 있던 것). 타격을 모르면 마지막 이벤트 뒤 1.5초
            float lastHit = hitsS.Count > 0 ? hitsS[hitsS.Count - 1] : evs[evs.Count - 1];
            float keep = hitsS.Count > 0 ? 1.2f : 1f;
            if (total - lastHit > 2f) { cutEnd = lastHit + keep; skipped += total - cutEnd; UltCutBackMs = Mathf.RoundToInt((total - cutEnd) * 1000); }
            UltCutSkipMs = Mathf.RoundToInt(skipped * 1000);
        }
        float Rm(float sec)
        {
            float t = sec;
            foreach (var k in cutSkips) { if (sec >= k.to) t -= k.to - k.from; else if (sec > k.from) t -= sec - k.from; }
            if (cutEnd < float.MaxValue) t = Mathf.Min(t, Rm0(cutEnd));
            return t;
        }
        float Rm0(float sec) { float t = sec; foreach (var k in cutSkips) { if (sec >= k.to) t -= k.to - k.from; else if (sec > k.from) t -= sec - k.from; } return t; }

        // ── 고학년 점검 몫 ──
        public string UltMoveWhy = "";                   // 이동형 판정 근거
        public bool UltMoved;
        public float UltTravel;                          // 몸짓이 스스로 앞으로 간 거리(싸움터 단위)
        public int UltMotionMs, UltCutMs, UltSndTailMs;  // 몸짓 길이 · 끝까지 못 하고 잘린 ms · 몸짓 끝에 줄여 끊은 소리 꼬리 ms
        public string UltSndMode = "";                   // events(스파인 SFX 칸) · fallback(시전 소리 + 맞는 소리)
        public int UltSndSpanMs;                         // 칸 소리 중 가장 늦게 끝나는 때(ms, 몸짓 시작에서)
        readonly List<AudioSource> ultHeld = new List<AudioSource>();
        const float SELF_TRAVEL = 2.5f;                  // 몸짓이 이만큼 넘게 제 몸을 옮기면(순간이동 · 뛰어들기) 따로 달려가지 않는다

        bool UltMover(UnitView u, string key, string hid, string anim, List<int> targets)
        {
            var names = new List<string>();
            if (UltPlan != null && UltPlan.Anim != null) { names.Add(UltPlan.Anim); names.AddRange(UltPlan.Chain); } else if (anim != null) names.Add(anim);
            if (UltPlan != null && UltPlan.Table != null)
            {
                var tw = UltPlan.Table;
                UltTravel = 0;
                UltMoved = tw.Moves && targets.Count > 0;
                UltMoveWhy = "표 " + tw.MoveType + (tw.Moves ? $" → {tw.Dest} 가기 {tw.GoMs} · 닿기 {tw.LandMs} · 돌아오기 {tw.ReturnMs}+{tw.BackMs}" : "");
                return UltMoved;
            }
            UltTravel = u.Travel(names);
            bool front = Look.MeleeArt(key);
            var byFx = Bolzena.Fx.FxRules.MeleeByFx(hid);
            bool shot = u.SpineArt && u.Sa.Skeleton.FindBone("Point_Attack1_Shot") != null;
            bool melee = front || (byFx == true && !shot);
            UltMoved = melee && targets.Count > 0 && UltTravel < SELF_TRAVEL;
            UltMoveWhy = (front ? "앞줄" : byFx == true && !shot ? "원작 평타 근접" : byFx == false || shot ? "원작 평타 원거리" : "모름") + $" · 몸짓 이동 {UltTravel:F1}" + (melee && UltTravel >= SELF_TRAVEL ? " (스스로 감)" : "");
            return UltMoved;
        }

        // 고학년 소리 — 스파인 SFX(n) 이벤트 칸을 그 사도의 고학년 소리 파일에 차례로 맞춰(웹판 sfx.js action · SfxMap.SlotMap) 그 시각에 튼다.
        // 맞는 소리 칸은 건너뛴다(맞는 순간 Impact 가 낸다). 칸 수가 파일 수와 안 맞으면 false — 예전처럼 시전 소리 + 맞는 소리
        bool UltSounds(string key, float speed)
        {
            UltSndMode = "fallback"; UltSndSpanMs = 0;
            if (UltPlan != null && UltPlan.Table != null && UltPlan.Table.Plays.Count > 0)
            {
                var (tkeys, tlen) = HeroClips(key);
                foreach (var pl in UltPlan.Table.Plays)
                {
                    var path = "hero/" + key + "/" + pl.Value;
                    // 표의 파일 이름이 원작 오타(마리: maire_ultimate02)면 그 사도 파일 가운데 꼬리가 같은 것(marie_ultimate02)으로.
                    //   없는 이름을 고학년 순간에 Resources.Load 하지 않는다(실패 · 경고가 그 프레임에 든다)
                    if (!tlen.ContainsKey(path))
                    {
                        int us = pl.Value.IndexOf('_');
                        string tail = us >= 0 ? pl.Value.Substring(us) : null;
                        string alt = tail == null ? null : tkeys.Find(k => k.EndsWith(tail, System.StringComparison.Ordinal));
                        if (alt == null) continue;
                        path = alt;
                    }
                    float at = Rm(pl.Key / 1000f) / Mathf.Max(0.01f, speed);
                    StartCoroutine(SndAt(path, at));
                    UltSndSpanMs = Mathf.Max(UltSndSpanMs, Mathf.RoundToInt((at + (tlen.TryGetValue(path, out var l) ? l : 0)) * 1000));
                }
                UltSndMode = "표 " + UltPlan.Table.SoundMode;
                return true;
            }
            if (UltPlan == null || UltPlan.Snd == null || UltPlan.Snd.Count == 0) return false;
            var (keys, len) = HeroClips(key);
            var slots = Bolzena.Fx.SfxMap.SlotsIn(keys, key, "ult");
            var ns = new List<int>();
            foreach (var e in UltPlan.Snd) ns.Add(e.N);
            var m = Bolzena.Fx.SfxMap.SlotMap(slots, ns);
            if (m == null) return false;
            foreach (var e in UltPlan.Snd)
                if (m.TryGetValue(e.N, out var path) && path != "hit")
                {
                    float at = Rm(e.T / 1000f) / Mathf.Max(0.01f, speed);   // 배속이면 몸짓이 빨라진 만큼 당긴다(높낮이는 그대로)
                    StartCoroutine(SndAt(path, at));
                    UltSndSpanMs = Mathf.Max(UltSndSpanMs, Mathf.RoundToInt((at + (len.TryGetValue(path, out var l) ? l : 0)) * 1000));
                }
            UltSndMode = "events";
            return true;
        }
        static (List<string>, Dictionary<string, float>) HeroClips(string key)
        {
            if (heroClips.TryGetValue(key, out var got)) return got;
            var keys = new List<string>();
            var len = new Dictionary<string, float>();
            foreach (var c in Resources.LoadAll<AudioClip>("Sfx/hero/" + key)) { var k = "hero/" + key + "/" + c.name; keys.Add(k); len[k] = c.length; }
            return heroClips[key] = (keys, len);
        }

        // 표대로 오가기 — dash: goMs 에 출발해 landMs 에 적 앞(잔상) · teleport/leap: goMs~landMs(몸이 안 보이는 때) 가운데에 옮김.
        // returnMs(leap 은 homeMs)에 제자리로 — backMs 가 있으면 돌아서 Move 로 뛰어오고, 없으면 그 자리에서 옮긴다. 시각은 ÷ 배속
        bool travelling;
        IEnumerator UltTravelCo(UnitView u, Bolzena.Fx.UltWay w, Vector3 dest, float speed, float lastHit = 0)
        {
            travelling = true;
            float k = 1f / 1000f / Mathf.Max(0.01f, speed);
            float t0 = Time.time;
            float El() => Time.time - t0;
            float sp = 1f / Mathf.Max(0.01f, speed);
            float go = Rm(w.GoMs / 1000f) * sp, land = Mathf.Max(go, Rm(w.LandMs / 1000f) * sp);
            float home = Rm((w.HomeMs ?? w.ReturnMs) / 1000f) * sp, back = w.BackMs * k;
            if (UltFitNow == null || !UltFitNow.RetHidden) home = Mathf.Max(home, lastHit + 0.25f);   // 마지막 타격 뒤에 돌아온다(루포 · 캐시 — 돌아온 뒤 제자리에서 때리던 것) — 숨은 틈 복귀(에르핀(왕도))는 그대로
            var homePos = u.Home;
            while (El() < go) yield return null;
            Trace?.Invoke("move", "가기(" + w.MoveType + ")");
            if (w.MoveType == "dash")
            {
                float d = Mathf.Max(0.06f, land - go);
                if (DASH_GHOSTS) StartCoroutine(Ghosts(u, d, new Color(0.75f, 0.9f, 1f, 0.22f)));
                yield return u.MoveTo(dest, d, Ease.InCubic);
            }
            else
            {
                while (El() < (go + land) / 2) yield return null;
                yield return u.MoveTo(dest, 0.01f);
            }
            Trace?.Invoke("move", "도착");
            while (El() < home) yield return null;
            Trace?.Invoke("move", "복귀");
            if (back > 0.02f && UltFitNow != null && UltFitNow.RetFly)   // (10-18 사용자 2차 에르핀(왕도) 「터지며 날아감」) 폭발에 날려 제자리로 — 몸짓은 그대로(돌아서 뛰기 · Idle 없음)
            { yield return u.MoveTo(homePos, back, Ease.OutCubic); travelling = false; yield break; }
            if (back > 0.02f)
            {
                u.Loop("Move");
                u.Sa.Skeleton.ScaleX = -u.Sa.Skeleton.ScaleX;          // 돌아서 뛴다
                yield return u.MoveTo(homePos, back, Ease.InOutCubic);
                u.Sa.Skeleton.ScaleX = -u.Sa.Skeleton.ScaleX;
                u.Idle();
            }
            else yield return u.MoveTo(homePos, 0.01f);
            travelling = false;
        }

        IEnumerator SndAt(string path, float at)
        {
            if (at > 0) yield return Clock.Wait(at);
            var a = Sfx.PlayHeld(path, 0.75f);
            if (a != null) ultHeld.Add(a);
        }

        // 고학년 모으기 — 시전자 둘레로 빛이 모이고 바닥이 빛난다
        IEnumerator UltCharge(UnitView u, float until)
        {
            var heroColor = Battle.Snapshot.Heroes[u.Ref.Index].Tint;
            Vfx.Glow(u.Feet + new Vector3(0, 0.1f, 0), 4f, new Color(heroColor.r, heroColor.g, heroColor.b, 0.8f), until + 0.3f, 2f, "FX_IN_Crack_Round_Glow", 310, null, 1.2f);
            float t = 0;
            while (t < until)
            {
                Vfx.Burst(u.Center, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Spark", Count = 5, Speed = new Vector2(-9f, -5f), Life = new Vector2(0.15f, 0.25f), Size = new Vector2(0.06f, 0.14f), Radius = 1.6f,
                    C0 = heroColor, C1 = Color.white, Stretch = true, StretchK = 0.05f, Order = 335, Boost = 3.5f,
                });
                yield return Clock.Wait(0.06f);
                t += 0.06f;
            }
        }

        // 고학년 끝 충격파
        void UltFinish(Vector3 at, UnitView caster, bool orig = false)
        {
            var tint = Battle.Snapshot.Heroes[caster.Ref.Index].Tint;
            Clock.HitStop(0.24f);
            FieldRig.Shake(0.9f, 4f);
            FieldRig.Punch(at, 0.1f);
            // 세기는 크기 · 흔들림 · 고리 · 집중선으로 내고, 빛(블룸 · 흰 덮개)은 눌러 둔다 — 대상(320)의 실루엣과 숫자가 읽혀야 한다.
            // 흰 폭발 구름은 대상 뒤(315)에 깐다: 몸 둘레로 터져 나오고 몸을 덮지 않는다
            // (2026-10-14 원작 비교) 마지막 타격 뒤 흰 집중선(속도선)과 사도 성격 색(녹 · 보라 · 빨강) 화면 덮개는 원작 고학년에 없어 뺐다 — 흔들림 · 멈칫 · 소리는 둔다
            PostFx.Kick(chroma: 0.5f, lens: -0.2f, bloom: 0.1f);
            Sfx.Play("ult_impact", 1f);
            Emit("ult_impact");
            Emit("ult_impact_" + caster.name.Replace("hero_", ""));
            if (orig) return;   // 원작 고학년 이펙트가 터지는 중 — 자체 고리 · 구름 · 불꽃은 원작이 없을 때만
            Vfx.Ring(at, 0.6f, 11f, 0.6f, new Color(1f, 0.95f, 0.85f, 0.75f), 1.6f, "FX_IN_Ring_Impact_wave_01", 340);
            Vfx.Ring(at, 0.3f, 7f, 0.5f, new Color(tint.r, tint.g, tint.b, 0.85f), 1.8f, "FX_IN_Ring_Impact_wave_01", 341);
            Vfx.Ring(at + new Vector3(0, -1f, 0), 0.5f, 12f, 0.7f, new Color(1f, 0.9f, 0.7f, 0.7f), 1.6f, "FX_IN_Ring_ShockWave_02", 339, null, 0.28f);
            Vfx.Sheet("fx_common_hit_explosion_1_m", at + new Vector3(0, -0.3f, 0), 2.0f, 315);
            Vfx.Sheet("fx_common_hit_22", at, 1.7f, 343, 0, false, 1f, true, 1.15f, new Color(tint.r, tint.g, tint.b, 0.8f));
            Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Spark", Count = 50, Speed = new Vector2(10f, 24f), Life = new Vector2(0.3f, 0.6f), Size = new Vector2(0.08f, 0.22f),
                C0 = new Color(1f, 0.97f, 0.88f), C1 = tint, Stretch = true, StretchK = 0.05f, Drag = 3f, Order = 344, Boost = 2.2f,
            });
            Vfx.Burst(at + new Vector3(0, -0.8f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_IN_Fragment_02", Count = 22, Speed = new Vector2(5f, 11f), Angle = 90, Spread = 140, Life = new Vector2(0.6f, 1f),
                Size = new Vector2(0.12f, 0.3f), C0 = new Color(0.75f, 0.65f, 0.55f), C1 = new Color(0.45f, 0.4f, 0.35f), Gravity = 2.2f, Spin = true, Additive = false, Boost = 1f, Order = 345,
            });
        }

        // 뒷정리 — 남은 몸짓(tail 까지, 이펙트 시계도) → 제자리로 → 앞뒤 차례 · 색. 그 유닛이 그새 다시 움직이면(Stale) 자리 · 몸짓은 새 수에 맡긴다.
        //   색 · 사도 차례는 그 뒤 아무도 새로 움직이지 않았을 때만 되돌린다(새 수가 끝날 때 되돌린다)
        IEnumerator Finish(UnitView u, int seq, float tStart, float tail, Bolzena.Fx.UltClock clk, bool dash, float back, int order, bool hero)
        {
            while (Time.time - tStart < tail)
            {
                if (clk != null) clk.Ms = (Time.time - tStart) * 1000f;
                if (Stale(u, seq) && clk == null) yield break;
                yield return null;
            }
            if (clk != null) clk.Ms = tail * 1000f;
            if (Stale(u, seq)) yield break;
            if (u.Feet != u.Home)
            {
                if (dash && !Over) u.Loop("Move");
                // u.Return 을 이 코루틴 안에서 한 칸씩 — 새 수가 오면 바로 놓는다(따로 띄운 이동끼리 자리를 다투지 않게)
                var stack = new Stack<IEnumerator>();
                stack.Push(u.Return(back));
                while (stack.Count > 0)
                {
                    if (Stale(u, seq)) yield break;
                    var top = stack.Peek();
                    if (!top.MoveNext()) { stack.Pop(); continue; }
                    if (top.Current is IEnumerator inner) stack.Push(inner); else yield return top.Current;
                }
                if (dash && !Over) u.Idle();
            }
            if (Stale(u, seq)) yield break;
            if (!dyingFoes.Contains(u)) u.SetOrder(order);
            if (seq != actSeq) yield break;
            foreach (var h in Heroes) if (h) h.Tint(Color.white);
            foreach (var en in Enemies) if (en) en.Tint(Color.white);
            if (hero) ResetHeroOrder();
        }

        // ── 고학년 ──
        public Bolzena.Fx.ActPlan UltPlan;               // 마지막 고학년 몸짓 계획(점검 로그)
        public string UltAnimLog;                        // 마지막 고학년에 튼 조각들
        public int UltTargets;                           // 마지막 고학년이 맞힌 적 수
        IEnumerator UltFlow(int hero, int target)
        {
            var s = Battle.Snapshot;
            var hs = s.Heroes[hero];
            // 단계를 고르는 쌓기(stack pick)가 든 고학년(요미 「지극정성의 마중」) — 사람은 컷인 전에 단계를 고른다 · 무르면 고학년도 안 쓴다 · 자동 전투는 다음 단계(2026-10-13)
            if (!Auto && Battle is CoreBattle cbu)
            {
                var stageNames = cbu.UltStagePickNames(hero);
                if (stageNames.Count > 0)
                {
                    int stagePick = 0;
                    var stId = cbu.UltStagePickId(hero);
                    var stIcons = new List<Sprite>(); for (int si = 1; si <= stageNames.Count; si++) stIcons.Add(ChipRow.StageIcon(stId, si));   // 단계마다 아이콘(요미 달)
                    yield return ChoiceWindow.Run(UiRoot, null, stageNames, $"「{hs.UltName}」 — 단계 고르기", v => stagePick = v, DemoBranchPick, stIcons);
                    if (stagePick <= 0) yield break;   // 물렀다
                    cbu.PendingStage = stagePick;
                }
            }
            InUlt = true;
            Clock.Base = 1f;   // 고학년은 원작 길이 원칙(ULT_MAX · ULT_SPEED_MAX) — 기본 템포를 걸지 않는다(배속은 그대로)
            Emit("ult_start");
            if (!Bolzena.RunUI.Settings.SkipCutin) yield return UltCutin.Play(ScreenRoot, hs.Key, hs.Name, hs.UltName, hs.Tint);
            else { Sfx.Voice(hs.Key, "ultimate", "shout"); Vfx.Flash(new Color(1, 1, 1, 0.25f), 0.15f); }   // 컷인 건너뛰기(설정) — 목소리 · 번쩍만
            Demo.UltLap.Begin("SD 시작 " + hs.Key);
            var evs = Battle.UseUlt(hero, target);
            Demo.UltLap.Lap("UseUlt");
            // Act 를 고학년으로 연출하고, 나머지는 보통으로
            yield return Present(evs);
            Clock.Base = Clock.TEMPO;
            InUlt = false;
        }

        public void RequestUlt(int hero) => request = (ReqKind.Ult, hero, UltAim >= 0 ? UltAim : FirstAliveEnemy());
        public void DemoSelectUlt(int hero) => TapUlt(hero);
        public bool InUlt { get; private set; }            // 고학년 연출 중(프레임 재기)
    }
}
