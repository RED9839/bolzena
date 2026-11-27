using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 맞음과 결과 — 타격 순간(Impact) · 사격 · 피해 뒤 결과 · 상태 · 회복 · 격파 · 쓰러짐 · 부활
    //   2026-11-17(12주차) BattleDirector.Acts.cs 에서 글자 그대로 옮겼다 — 동작 · 값 그대로(ADR-004).
    public partial class BattleDirector
    {
        // 맞음 이펙트 — 원작 공용 타격(구운 낱장, 웹판 sparkFx). 갈래: 둔기 · 마법 · 쏘기 · 그 밖은 베기
        static string FxHitKind(HitKind k) => k == HitKind.Blunt ? "blunt" : k == HitKind.Magic ? "magic" : k == HitKind.Shot ? "shot" : "slash";
        // 작은 공용 타격 한 장(전체 공격의 나머지 적) — 갈래마다 가벼운 것
        static string SmallHitSheet(HitKind k) => k == HitKind.Blunt ? "fx_common_hit_2_m" : k == HitKind.Magic ? "fx_common_hit_4_m" : k == HitKind.Shot ? "fx_common_hit_1_m" : "fx_common_hit_3_m";
        static bool CalmFx => Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;   // 움직임 줄이기 · 저사양 — 덧붙인 몸 반응(찌그러짐)을 끈다

        // 전체 공격 물결 — 적마다 늦추는 몫(적이 많으면 좁게)과 한 박자에서 늦출 합의 상한
        const float AOE_LAG_MAX = 0.24f;
        static float AoeLag(int n) => Mathf.Lerp(0.06f, 0.04f, Mathf.Clamp01((n - 2) / 3f));
        float crowdStopAt = -1f;     // 무리를 칠 때 멈칫은 한 번만(Clock.Now)
        static bool HitSheets => Bolzena.Fx.FxLibrary.Has("fx_common_hit_3_m");

        // 무리의 발 자리 — 살아 있는 대상들의 경계 상자 가운데(한 마리면 그 적). 전체 공격의 목표 지점
        Vector3 Mid(List<int> targets)
        {
            var alive = targets.FindAll(t => t < Enemies.Count && Enemies[t] != null && !Dead(t));
            if (alive.Count == 0) alive = targets.FindAll(t => t < Enemies.Count && Enemies[t] != null);
            if (alive.Count == 0) return new Vector3(3.5f, -1f, 0);
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var t in alive)
            {
                var f = Enemies[t].Feet;
                float hw = Enemies[t].Width() * 0.4f;
                x0 = Mathf.Min(x0, f.x - hw); x1 = Mathf.Max(x1, f.x + hw);
                y0 = Mathf.Min(y0, f.y); y1 = Mathf.Max(y1, f.y);
            }
            return new Vector3((x0 + x1) / 2, (y0 + y1) / 2, 0);
        }

        // 무리의 몸 가운데 — 경계 상자 가운데에 몸 높이의 중간(이펙트 · 충격파 · 카메라가 본다)
        Vector3 MidBody(List<int> targets)
        {
            var m = Mid(targets);
            float h = 0; int n = 0;
            foreach (var t in targets) if (t < Enemies.Count && Enemies[t] != null && !Dead(t)) { h += Enemies[t].Center.y - Enemies[t].Feet.y; n++; }
            return m + new Vector3(0, n > 0 ? h / n : 1f, 0);
        }

        Vector3? aoeAt;          // 전체 공격 중이면 무리 가운데 — 타격 이펙트를 거기 한 번에
        // 지금 몸짓의 사도 고유 맞음 장이 적에게 터지나(OrigFx 계획의 target 장) — null 이면 몸짓 밖(고학년 발동 cue 는 제 장을 트니 있는 셈 · 그 밖은 없는 셈)
        bool? ownHitFx;

        IEnumerator LinesOff(float after)
        {
            yield return Clock.WaitU(after);
            ScreenFx.I.Lines(0, Color.white, Vector2.zero);
        }

        // ── 맞는 순간 ──
        // fxHere — 전체 공격이면 첫 대상만 무리 가운데에 큰 이펙트를 내고, 나머지는 몸 반응 · 작은 타격 한 장 · 숫자만
        // shot — 쏘는 한 대(추가 공격 · 탄환 · 반격 · 소환물, ShotHit): 가볍게, shotFx 가 있으면 공용 타격 대신 그 사도의 원작 맞음 장
        void Impact(BattleEvent e, UnitView by, bool ult, bool last, string hitSfx, bool crowd, bool fxHere = true, bool shot = false, string shotFx = null, float shotScale = 0.85f)
        {
            int ti = e.Target.Index;
            if (ti >= Enemies.Count) return;
            var t = Enemies[ti];
            Trace?.Invoke("hit", $"피해 {e.Value}{(e.Crit ? "!" : "")} → {t.name.Replace("enemy_", "")}");
            var hud = EnemyHuds[ti];
            var kind = e.HitKind;
            var at = t.Center + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.2f, 0.25f), 0);
            bool crit = e.Crit;
            bool multi = e.Hits > 1;
            bool finale = multi && last && !ult && !shot;   // 다단의 마지막 대 — 앞 대들은 가볍게, 이 대는 세게(2026-10-13)
            bool light = (multi && !finale) || shot;
            float flip = Random.value < 0.5f ? 1 : -1;

            // 멈칫 · 흔들림 · 줌 펀치 — 무리를 치면 멈칫은 한 번만(전에는 0번 적일 때만 걸려 0번이 없는 무리는 멈칫이 없었다)
            float stop = crit ? 0.14f : ult ? 0.075f : shot ? 0.04f : finale ? 0.12f : multi ? 0.05f : 0.085f;
            if (!crowd || Clock.Now - crowdStopAt > 0.15f) { Clock.HitStop(stop); if (crowd) crowdStopAt = Clock.Now; }
            FieldRig.Shake(crit ? 0.5f : ult ? (ultPunchOn ? 0.15f : 0.25f) : finale ? 0.45f : shot ? 0.16f : multi ? 0.2f : 0.3f, crit ? 2.5f : finale ? 1.4f : 0.6f);
            FieldRig.Punch(aoeAt ?? at, crit ? 0.075f : finale ? 0.06f : ult ? 0.04f : shot ? 0.02f : 0.035f);

            // 몸 — 흰 번쩍임(반쯤만 — 온통 하얗게 채우면 실루엣이 날아간다) · 밀려남 · 붉게 · 찌그러짐(스파인 피격 애니가 없어 몸 크기로)
            // 고학년은 여러 대가 겹치니 한 대의 빛을 더 줄인다(fade)
            float fade = ult ? 0.6f : 1f;
            bool bigFoe = ti < Battle.Snapshot.Enemies.Count && Battle.Snapshot.Enemies[ti].Boss;   // 큰 몸은 흰 채우기가 더 넓게 번진다
            t.Flash(new Color(1f, 0.97f, 0.9f), crit ? 0.2f : finale ? 0.17f : 0.13f, (crit ? 0.46f : finale ? 0.42f : light ? 0.3f : 0.36f) * (ult ? 0.7f : 1f) * (bigFoe ? 0.6f : 1f));
            t.Knock(1f, crit ? 0.55f : finale ? 0.46f : light ? 0.2f : 0.36f, crit || finale ? 0.38f : 0.28f);
            if (!CalmFx) t.Squash((crit ? 0.13f : finale ? 0.11f : light ? 0.045f : 0.075f) * (bigFoe ? 0.6f : 1f), crit || finale ? 0.3f : 0.22f);
            if (!e.Crit) StartCoroutine(RedTint(t));

            // 전체 공격 — 이펙트는 무리 가운데에 한 번(조금 크게), 대상마다는 몸 반응 · 작은 타격 한 장 · 숫자
            var fxAt = aoeAt ?? at;
            // (2026-10-14 원작 비교) 원작에 없는 공용 타격을 뺀다 — 노란 고리(fx_common_hit_22 「세게」 · 고학년 첫 타격 덧불) · 노란 불꽃줄(hit_slash_3 · hit_3_m) ·
            //   치명 덧불(hit_shockwave_1)은 모든 평타 · 강공 · 스킬 · 고학년 타격에 같은 노란 연출로 겹쳤다. 사도 고유 맞음 장이 적에게 터지면(own) 그것만,
            //   없으면 아주 약한 공용 한 장(SmallHitSheet — 작게 · 짧게)만
            bool own = shot ? false : ownHitFx ?? ult;
            void Weak(Vector3 p) { if (!Bolzena.Fx.BolzenaFx.Calm && !Bolzena.Fx.BolzenaFx.OriginalOnly) Bolzena.Fx.BolzenaFx.Play(SmallHitSheet(kind), new Bolzena.Fx.FxPlayOptions { At = p, Scale = 0.5f, Until = 0.3f, Order = 182 + fxBoost, Parent = FieldRoot }); }
            if (shot && shotFx != null && Bolzena.Fx.BolzenaFx.Play(shotFx, new Bolzena.Fx.FxPlayOptions { At = at, Scale = shotScale, Until = 0.6f, Order = 182 + fxBoost, Parent = FieldRoot }) != null)
            {
                // 쏜 사도의 원작 맞음 장(_hit)만(공용 낱장을 겹치던 것은 뺐다)
                if (e.Blocked > 0) Bolzena.Fx.BolzenaFx.Common("shieldHit", t.Fx);
            }
            else if (!fxHere && aoeAt.HasValue)
            {
                // 전체 공격의 나머지 적 — 고유 장이 무리를 덮으면 몸 반응 · 숫자만, 없으면 약한 공용 한 장
                if (!own) Weak(at);
            }
            else if (HitSheets)
            {
                if (!own) Weak(fxAt);
                if (e.Blocked > 0) Bolzena.Fx.BolzenaFx.Common("shieldHit", t.Fx);
                if (crit)
                {
                    Vfx.Flash(new Color(1f, 0.9f, 0.7f, 0.06f), 0.12f);
                    PostFx.Kick(chroma: 0.6f, lens: -0.15f, bloom: 0.12f);
                    if (!ult) { ScreenFx.I.Lines(0.45f, new Color(1f, 0.97f, 0.9f), FieldRoot.TransformPoint(fxAt), 302, 0.3f); Clock.Run(LinesOff(0.28f)); }   // 고학년 타격 뒤 집중선은 원작에 없다
                }
                else if (ult) PostFx.Kick(chroma: 0.35f, bloom: 0.08f);
            }
            else if (fxHere || !aoeAt.HasValue)
            {
                // 원작 타격 낱장 + 빛 + 불꽃 + 파편
                Color sparkC;
                switch (kind)
                {
                    case HitKind.Blunt:
                        Vfx.Sheet("fx_common_hit_1_m", fxAt, crit ? 1.6f : 1.15f, 182 + fxBoost, Random.Range(-20f, 20f));
                        Vfx.Sheet("fx_common_hit_2_m", fxAt, 1.1f, 183 + fxBoost);
                        sparkC = new Color(1f, 0.85f, 0.4f);
                        break;
                    case HitKind.Magic:
                        Vfx.Sheet("fx_mago_hit_1", fxAt, crit ? 1.15f : 0.9f, 182 + fxBoost, 0, false, 1.2f, true, 1.05f, new Color(0.6f, 0.68f, 1f, 0.85f * fade));
                        Vfx.Sheet("fx_common_hit_4_m", fxAt, 0.95f, 183 + fxBoost, Random.Range(-30f, 30f), flip < 0);
                        sparkC = new Color(0.6f, 0.8f, 1f);
                        break;
                    default:
                        Vfx.Sheet("fx_common_hit_slash_3", fxAt, crit ? 1.5f : 1.15f, 182 + fxBoost, Random.Range(-35f, 25f), flip < 0, 1.2f);
                        Vfx.Sheet("fx_common_hit_3_m", fxAt, 1.0f, 183 + fxBoost, Random.Range(-15f, 15f));
                        sparkC = new Color(1f, 0.9f, 0.55f);
                        break;
                }
                // 가운데 빛은 작고 옅게 — 대상의 몸을 덮지 않을 만큼(블룸 문턱도 넘지 않게 세기 1.2 안팎)
                Vfx.Glow(fxAt, crit ? 1.5f : 1.05f, new Color(1f, 0.92f, 0.75f, (crit ? 0.5f : 0.4f) * fade), 0.14f, 1.2f, null, 181 + fxBoost, null, 1.25f);
                Vfx.Ring(fxAt, 0.3f, crit ? 3.0f : 2.0f, crit ? 0.3f : 0.22f, new Color(1f, 0.95f, 0.85f, 0.6f * fade), 1.5f, "FX_IN_RIng_Hit_wave_01", 184 + fxBoost);
                Vfx.Burst(fxAt, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Spark", Count = crit ? 22 : finale ? 18 : multi ? 8 : 12, Speed = new Vector2(7f, crit ? 20f : 15f), Life = new Vector2(0.12f, 0.3f),
                    Size = new Vector2(0.05f, 0.13f), C0 = new Color(1f, 0.97f, 0.85f), C1 = sparkC, Stretch = true, StretchK = 0.045f, Drag = 4f, Angle = 10, Spread = 150, Order = 186 + fxBoost, Boost = 2.2f,
                });
                Vfx.Burst(fxAt, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Fragment_02", Count = crit ? 10 : 5, Speed = new Vector2(2.5f, 6.5f), Life = new Vector2(0.4f, 0.7f), Size = new Vector2(0.08f, 0.18f),
                    C0 = sparkC, C1 = Color.white, Gravity = 1.8f, Spin = true, Angle = 60, Spread = 120, Order = 185 + fxBoost, Boost = 1.6f,
                });
                if (crit)
                {
                    // 치명 — 빛 대신 모양으로(번개 갈래 · 충격파 · 집중선 · 색수차). 화면 번쩍임은 아주 옅게
                    Vfx.Sheet("fx_common_hit_22", fxAt, 1.25f, 187 + fxBoost, Random.Range(-10f, 10f), false, 1f, true, 1.1f, new Color(1f, 0.85f, 0.5f, 0.85f));
                    Vfx.Sheet("fx_common_hit_shockwave_1", fxAt, 1.3f, 188 + fxBoost);
                    Vfx.Flash(new Color(1f, 0.9f, 0.7f, 0.06f), 0.12f);
                    PostFx.Kick(chroma: 0.6f, lens: -0.15f, bloom: 0.12f);
                    ScreenFx.I.Lines(0.45f, new Color(1f, 0.97f, 0.9f), FieldRoot.TransformPoint(fxAt), 302, 0.3f);
                    Clock.Run(LinesOff(0.28f));
                }
                else if (finale) Vfx.Sheet("fx_common_hit_22", fxAt, 1.1f, 187 + fxBoost, Random.Range(-10f, 10f), false, 1f, true, 1.05f, new Color(1f, 0.9f, 0.65f, 0.8f));   // 다단 마지막 대 — 세게 덧불
                else if (ult) PostFx.Kick(chroma: 0.35f, bloom: 0.08f);
            }

            // 소리
            Sfx.Play(crit ? "hit_crit" : kind == HitKind.Blunt ? "hit_blunt" : kind == HitKind.Magic ? "hit_magic" : "hit_slash", crit ? 0.95f : shot ? 0.55f : finale ? 0.9f : 0.8f, 1f, true);
            if (hitSfx != null) Sfx.Play(hitSfx, shot ? 0.45f : 0.6f, 1f, true);
            if (e.Blocked > 0) Sfx.Play("block_hit", 0.6f);

            // 숫자 · 막대
            // 숫자는 몸 위쪽에 — 가운데에 띄우면 큰 적(보스)의 몸을 가린다
            var numAt = Vector3.Lerp(t.Center, t.Top, 0.55f);
            float numY = Mathf.Max(at.y + 0.55f, numAt.y);
            string byNat = e.By >= 0 && e.By < Battle.Snapshot.Heroes.Count ? Battle.Snapshot.Heroes[e.By].Nature : null;   // 엔진이 피해를 셈한 사도의 성격(숫자 왼쪽 동그라미) — 사도가 아닌 피해면 없음
            if (EnemyHud.OldLayout) Vfx.Number(new Vector3(at.x + 0.2f, numY, 0), e.Value, crit, e.Hit, null, false, e.Blocked, 460, e.Weak, byNat);
            else
            {
                // 숫자는 머리 위 묶음 밑에서 떠올라 막대에 닿기 전에 사라지게(떠오르는 높이 · 연타 한 칸씩 · 글 높이만큼 아래),
                //   그래도 닿으면 묶음 밑으로 그린다(355 — 묶음 360~ 아래, 고학년 이펙트 325 · 사도 330 위)
                float room = hud.FieldBottom - 1.15f - (crit ? 0.2f : 0) - e.Hit * 0.32f;
                numY = Mathf.Max(t.Center.y, Mathf.Min(numY, room));
                Vfx.Number(new Vector3(at.x + 0.2f, numY, 0), e.Value, crit, e.Hit, null, false, e.Blocked, 355, e.Weak, byNat);
            }
            hud.SetHp(e.HpAfter);
            hud.SetBlock(e.BlockAfter);

            if (!firstHitShot && !crit && !ult) { firstHitShot = true; Emit("hit"); }
            if (!firstCritShot && crit) { firstCritShot = true; Emit("crit"); }
            if (ult) Emit(last ? "ult_last" : "ult_hit");
        }

        IEnumerator RedTint(UnitView u)
        {
            yield return Clock.Wait(0.06f);
            u.Tint(new Color(1f, 0.5f, 0.5f), 40f);
            yield return Clock.Wait(0.14f);
            u.Tint(Color.white, 8f);
        }

        // ── 쏘는 한 대(2026-10-13) — 추가 공격 · 탄환 · 반격 · 협공 · 소환물 · 패시브 피해를 누가 쏘는지 보이게 ──
        //   쏘는 사도 몸이 짧게 움찔(탄환은 반동으로 뒤로, 그 밖은 앞으로 반 발) · 빛 → 사도 원작 평타의 투사체(없으면 몸 앞 빛 한 번) →
        //   맞는 쪽(바로 뒤 Damage "shot" → ShotHit — 사도 원작 맞음 장, 없으면 공용 타격). 여러 발이 몰리면 간격을 좁혀 하나씩 줄 세운다
        // 날아가는 시간 — 0.1초면 캡처 칸(0.15초 간격)에 투사체가 거의 안 잡혔다(2026-10-13 캡처 검토) → 0.22초, 몰리면 0.1초까지
        const float SHOT_FLY = 0.22f, SHOT_FLY_MIN = 0.1f, SHOT_RUN_RESET = 0.5f;
        const int SHOT_RUN_FREE = 4, SHOT_RUN_CAP = 14;   // 4발까지는 0.22초씩, 그 뒤는 0.1초까지 줄이고 14발을 넘으면 기다리지 않는다
        const float SHOT_SCALE = 1.1f, SHOT_TRAIL_GAP = 0.03f;
        // 연계는 실제 평타 몸짓을 다시(2026-10-15 사용자 「불똥 날아가는 것 말고 일반공격 한 번 더」) — 몰린 차례 SHOT_MOTION_MAX 번째까지만, 그 뒤는 빠른 투사체로 줄 세운다(시온 연사 템포).
        //   사도가 다른 몸짓(카드 · 고학년) 중이면 몸짓 없이 투사체만
        const int SHOT_MOTION_MAX = 3;
        const float SHOT_MOTION_SPEED = 1.8f;   // 투사체 크기(전 0.8 — 작아 안 보였다) · 꼬리 빛 간격(초)
        float shotLast = -10f;
        int shotRun;
        static readonly Dictionary<string, (string proj, string hit)> shotLooks = new Dictionary<string, (string proj, string hit)>();

        static bool IsShot(BattleEvent e) => e != null && e.Kind == EventKind.Damage && e.Text == "shot";

        // 쏘는 한 대의 색 — 사도 원작 평타 장의 가장 진한 색(채도 × 크기). 성격 색(hs.Tint)은 그림과 달라(란 = 순수 초록인데 보라 칼빛) 꼬리 · 빛이 엉뚱한 색이었다
        static readonly Dictionary<string, Color> shotTints = new Dictionary<string, Color>();
        static Color ShotTint(string hid, Color fallback)
        {
            if (hid == null) return fallback;
            if (shotTints.TryGetValue(hid, out var got)) return got;
            Color best = fallback; float bw = 0;
            foreach (var n in Bolzena.Fx.FxLibrary.Get(hid, "attack"))
            {
                var fx = Bolzena.Fx.FxLibrary.Effect(n);
                if (fx == null) continue;
                foreach (var e in fx.Em)
                {
                    if (e.M3d || e.Col == null || e.Col.Length == 0) continue;
                    var c = new Color(e.Col[0].r * e.Tint.r, e.Col[0].g * e.Tint.g, e.Col[0].b * e.Tint.b);
                    float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    if (mx < 0.3f) continue;
                    float w = (mx - mn) / mx * Mathf.Min(10f, Mathf.Max(0.1f, e.Size.y));
                    if (w > bw) { bw = w; best = new Color(c.r / mx, c.g / mx, c.b / mx, 1f); }
                }
            }
            return shotTints[hid] = best;
        }
        static float ShotHitScale(string hid) => hid != null && SHOT_HIT.TryGetValue(hid, out var s) ? s.scale : 0.85f;

        static (string proj, string hit) ShotLook(string hid)
        {
            if (hid == null) return (null, null);
            if (shotLooks.TryGetValue(hid, out var got)) return got;
            string proj = null, hit = SHOT_HIT.TryGetValue(hid, out var sh) && Bolzena.Fx.FxLibrary.Has(sh.fx) ? sh.fx : null;
            bool projFixed = SHOT_PROJ.TryGetValue(hid, out var pf);
            if (projFixed) proj = string.IsNullOrEmpty(pf) ? null : pf;
            foreach (var n in Bolzena.Fx.FxLibrary.Get(hid, "attack"))
            {
                if (!projFixed && proj == null && (n.Contains("proj") || n.Contains("bullet")) && !n.Contains("trail")) proj = n;
                if (hit == null && n.Contains("hit") && !n.Contains("shockwave")) hit = n;
            }
            return shotLooks[hid] = (proj, hit);
        }

        IEnumerator ShotFx(BattleEvent e)
        {
            int hi = e.Actor.Index, ti = e.Target.Index;
            if (e.Actor.Side != Side.Party || hi < 0 || hi >= Heroes.Count || Heroes[hi] == null || ti < 0 || ti >= Enemies.Count || Enemies[ti] == null) yield break;
            var u = Heroes[hi];
            var t = Enemies[ti];
            var hs = hi < Battle.Snapshot.Heroes.Count ? Battle.Snapshot.Heroes[hi] : null;
            var look = ShotLook(hs?.Id);
            // 몰림 — 앞 발에서 얼마 안 지나 또 오면 줄 세운 차례가 이어진다
            if (Clock.Now - shotLast > SHOT_RUN_RESET) shotRun = 0;
            shotRun++;
            float fly = shotRun <= SHOT_RUN_FREE ? SHOT_FLY : shotRun > SHOT_RUN_CAP ? 0f
                : Mathf.Lerp(SHOT_FLY, SHOT_FLY_MIN, (shotRun - SHOT_RUN_FREE) / (float)(SHOT_RUN_CAP - SHOT_RUN_FREE));
            Trace?.Invoke("shot", $"{e.Text} {u.name.Replace("hero_", "")} → {t.name.Replace("enemy_", "")}");
            // 몸짓 — 자리 몫(Knock)만 더해 몸짓 중인 사도에게도 겹쳐 걸린다. 사도 색(원작 평타 장의 색)으로 살짝 빛난다
            var tint = ShotTint(hs?.Id, hs != null ? hs.Tint : Color.white);
            bool recoil = e.Text == "bullet";
            // 평타 몸짓 — 쉬는 중(도는 동작)인 사도만. 때리는 순간까지 기다린 뒤 쏜다(근접은 날지 않고 바로 맞음)
            var cur = u.SpineArt ? u.Sa.AnimationState.GetCurrent(0) : null;
            string a1 = u.SpineArt ? u.AnimFor(Motion.Attack1) : null;
            bool motion = shotRun <= SHOT_MOTION_MAX && a1 != null && (cur == null || cur.Loop) && !InUlt;
            bool melee = motion && Look.MeleeArt(u.name.Replace("hero_", ""));
            if (motion)
            {
                u.Play(a1, SHOT_MOTION_SPEED);
                var st = u.Strikes(u.F(a1));
                float wait = Mathf.Clamp((st.Count > 0 ? st[0] : 0.3f) / SHOT_MOTION_SPEED, 0.08f, 0.7f);
                yield return Clock.Wait(wait);
                if (melee) fly = 0;   // 칼 · 주먹 — 휘두르는 순간 맞는다
            }
            else
            {
                if (!Bolzena.Fx.BolzenaFx.OriginalOnly) { u.Knock(recoil ? -1f : 1f, recoil ? 0.14f : 0.22f, 0.22f); u.Flash(Color.Lerp(tint, Color.white, 0.5f), 0.16f, 0.4f); }
            }
            // 쏘는 자리 — 지금 무기 끝(MotionTables.WEAPON — 총열 · 칼끝 · 손, 2026-10-13) → 총구 → 평타 시전 본(Point_Attack1, 없으면 짐작한 몸 앞).
            //   시온은 Point_Attack1 이 발밑, 클로에 · 이드 · 비비는 몸 뒤라 엉뚱한 데서 나가던 것
            var from = u.Fx.Weapon?.Invoke() ?? u.Fx.Muzzle?.Invoke() ?? u.Fx.At(Bolzena.Fx.FxAnchor.Cast, "Attack1");
            var to = t.Center;
            float pScale = SHOT_SCALE;
            (float dy, float scale) dr = default;
            bool drop = hs != null && hs.Id != null && SHOT_DROP.TryGetValue(hs.Id, out dr);
            if (drop) { from = t.Center + new Vector3(-0.35f, dr.dy, 0); to = t.Center + new Vector3(0, 0.35f, 0); pScale = dr.scale; }
            if (hs != null && hs.Id != null && SHOT_BEAM.TryGetValue(hs.Id, out var beam) && fly > 0)
            {
                Bolzena.Fx.BolzenaFx.Play(beam, new Bolzena.Fx.FxPlayOptions { At = from, Aim = true, AimAt = to, Until = 0.45f, Order = 190 + fxBoost, Parent = FieldRoot, Bare = true });
                yield return Clock.Wait(0.12f);
                shotLast = Clock.Now;
                yield break;
            }
            if (hs != null && hs.Id != null && SHOT_MARK.TryGetValue(hs.Id, out var mark) && fly > 0)
            {
                Bolzena.Fx.BolzenaFx.Play(mark.fx, new Bolzena.Fx.FxPlayOptions { At = t.Center, Scale = 1.6f, Bare = true, Until = mark.wait + 0.05f, Order = 190 + fxBoost, Parent = FieldRoot });
                yield return Clock.Wait(Mathf.Min(mark.wait, fly + 0.15f));
                shotLast = Clock.Now;
                yield break;   // 맞는 장은 바로 뒤 ShotHit
            }
            bool flew = fly > 0 && look.proj != null && Bolzena.Fx.BolzenaFx.Play(look.proj, new Bolzena.Fx.FxPlayOptions
            {
                At = from, MoveTo = to, MoveDur = fly, Until = fly + 0.12f, Scale = pScale, Order = 190 + fxBoost, Parent = FieldRoot,
                Aim = !drop,   // 맞는 적 몸 쪽을 보게 돌린다(내리치는 탄은 짜인 세로 그대로)
            }) != null;
            if (!flew && !Bolzena.Fx.BolzenaFx.OriginalOnly) Vfx.Glow(from, 1.1f, new Color(tint.r, tint.g, tint.b, 0.75f), 0.16f, 2f, null, 190 + fxBoost, null, 1.4f);   // 투사체가 없는 사도 — 몸 앞 빛 한 번(입자 없음)
            var cast = HeroSfx(u.name.Replace("hero_", ""), Motion.Attack1, false);
            if (cast != null) Sfx.Play(cast, 0.35f, 1.08f, true);
            shotLast = Clock.Now;
            if (fly > 0 && Bolzena.Fx.BolzenaFx.OriginalOnly) yield return Clock.WaitU(fly);   // 원작 그대로 — 꼬리 빛 없이 투사체가 닿을 때까지만
            else if (fly > 0)
            {
                // 꼬리 — 나는 길을 따라 사도 색 빛을 떨군다(투사체가 없는 사도는 이 빛 줄이 곧 탄). 전투 시계 그대로
                float el = 0, next = 0;
                while (el < fly)
                {
                    if (el >= next)
                    {
                        float f = Mathf.Clamp01(el / fly), e2 = f * f * (3 - 2 * f);
                        Vfx.Glow(Vector3.LerpUnclamped(from, to, e2), flew ? 0.55f : 0.8f, new Color(tint.r, tint.g, tint.b, flew ? 0.45f : 0.7f), 0.2f, 2f, null, 189 + fxBoost, null, 1.1f);
                        next += SHOT_TRAIL_GAP;
                    }
                    yield return null;
                    el += Clock.UDt;
                }
            }
            shotLast = Clock.Now;
        }

        // 쏘는 한 대가 닿는다 — 쏜 사도의 원작 맞음 장 · 맞는 소리로 가볍게(전체 공격 박자 안이어도 이 한 대는 그 적에게)
        void ShotHit(BattleEvent e)
        {
            int ti = e.Target.Index;
            if (ti < 0 || ti >= Enemies.Count || Enemies[ti] == null) return;
            int hi = e.Actor.Side == Side.Party ? e.Actor.Index : -1;
            string fx = null, snd = null;
            float fxScale = 0.85f;
            if (hi >= 0 && hi < Heroes.Count && Heroes[hi] != null)
            {
                if (hi < Battle.Snapshot.Heroes.Count) { fx = ShotLook(Battle.Snapshot.Heroes[hi].Id).hit; fxScale = ShotHitScale(Battle.Snapshot.Heroes[hi].Id); }
                snd = HeroSfx(Heroes[hi].name.Replace("hero_", ""), Motion.Attack1, true);
            }
            var keep = aoeAt;
            aoeAt = null;
            Impact(e, null, false, true, snd, false, true, true, fx, fxScale);
            aoeAt = keep;
        }

        // 맞는 순간에 함께 걸리는 것 — 상태 · 강인도 · 격파 · 쓰러짐 · 게이지 …
        IEnumerator ApplyConsequence(BattleEvent e, UnitView by)
        {
            var s = Battle.Snapshot;
            switch (e.Kind)
            {
                case EventKind.Block:
                    ApplyBlock(e);
                    break;
                case EventKind.Status:
                    yield return StatusFx(e, s);
                    break;
                case EventKind.Heal:
                    HealFx(e);
                    break;
                case EventKind.Shot:
                    yield return ShotFx(e);
                    break;
                case EventKind.Damage:
                    if (IsShot(e)) { ShotHit(e); break; }   // 쏘는 한 대 — 그 사도의 맞음 장으로
                    // 몸짓 밖의 피해(지속 피해 · 반격 · 패시브) — 작게
                    if (e.Target.Index < Enemies.Count && Enemies[e.Target.Index] != null) Impact(e, null, false, false, null, true);
                    break;
                case EventKind.PartyHurt:
                    Hurt(e, e.Actor.Side == Side.Enemy && e.Actor.Index < Enemies.Count ? Enemies[e.Actor.Index].name.Replace("enemy_", "") : "fairymobcloserange", false);
                    break;
                case EventKind.Toughness:
                    if (e.Target.Index < EnemyHuds.Count && EnemyHuds[e.Target.Index] != null) EnemyHuds[e.Target.Index].SetTough(e.FAfter, !e.Up);
                    break;
                case EventKind.Break:
                    Hud.ApGain(e.Value);   // 격파 AP +1 — 수가 끝나야 숫자가 맞춰지니 그 순간 「+1」 로
                    yield return BreakFx(e.Target.Index);
                    // 격파하며 처치(core LethalKill — 쪽지 강인도 → 격파 → 죽음) — 빈 강인도 칸과 「격파!」 를 잠깐 보이고 쓰러진다
                    if (e.Target.Index < Battle.Snapshot.Enemies.Count && Battle.Snapshot.Enemies[e.Target.Index].Dead) yield return Clock.Wait(0.3f);
                    break;
                case EventKind.Death:
                    StartCoroutine(DeathFx(e.Target.Index, e.Boss));
                    // 보스가 쓰러지면 보스가 세운 소환물도 같이 쓰러진다(core FoeDeath) — 그 쪽지는 보스 연출(1.4초) 뒤에 와서,
                    //   보스 마지막 일격 화면에 소환물 체력바가 밝게 남고 보스가 사라진 뒤 따로 쓰러졌다. 같은 수에 쓰러진 다른 적을 지금 함께 쓰러뜨린다
                    if (e.Boss)
                    {
                        var ss = Battle.Snapshot.Enemies;
                        for (int j = 0; j < Enemies.Count && j < ss.Count; j++)
                            if (j != e.Target.Index && ss[j].Dead && Enemies[j] != null && Enemies[j].gameObject.activeSelf && !dyingFoes.Contains(Enemies[j]))
                                StartCoroutine(DeathFx(j, false));
                        yield return Clock.WaitU(1.4f);
                    }
                    break;
                case EventKind.Revive:
                    yield return ReviveFx(e.Target.Index, s);
                    break;
                case EventKind.UltGauge:
                    Hud.SetGauge(e.Value);
                    Hud.Ults[e.Actor.Index].Set(e.Value, s.Heroes[e.Actor.Index].UltMax);
                    break;
                case EventKind.UltReady:
                case EventKind.Discard:
                case EventKind.Exhaust:
                case EventKind.CardPlayed:
                    yield return Simple(e);
                    break;
                case EventKind.ApChanged:
                    Hud.SetAp(e.Value, s.MaxAp);
                    break;
                case EventKind.Draw:
                    if (e.Text != null && e.Text.StartsWith("grace")) { yield return GraceIn(e); break; }   // 은총으로 얻은 카드
                    yield return Deal(new List<CardInfo> { e.Card });
                    break;
                case EventKind.Intent:
                case EventKind.Recover:
                    yield return Simple(e);
                    break;
                case EventKind.FxCue:
                    yield return FxCueFx(e);
                    break;
                case EventKind.FoeUlt:
                    yield return FoeUltFx(e);
                    break;
                case EventKind.FoeCharge:
                    yield return FoeChargeFx(e);
                    break;
                case EventKind.Form:
                    yield return FormFx(e);
                    break;
            }
        }

        // 상태 — 적에게 거는 해로운 것은 색 번쩍 · 고리 · 떠오르는 글, 파티 · 사도에게 좋은 것은 초록 빛. 칩은 지금 모습으로
        IEnumerator StatusFx(BattleEvent e, BattleSnapshot s)
        {
            int i = e.Target.Index;
            string id = e.Text ?? "";
            bool good = e.Up || Bolzena.Core.R.BUFF_ST.Contains(id);
            var col = good ? new Color(0.55f, 1f, 0.6f) : id == "취약" ? new Color(1f, 0.5f, 0.4f) : new Color(0.75f, 0.55f, 1f);
            if (e.Target.Side == Side.Enemy)
            {
                if (i >= Enemies.Count || Enemies[i] == null) yield break;
                if (i < s.Enemies.Count) EnemyHuds[i].SetChips(s.Enemies[i].Chips);
                Sfx.Play(good ? "buff" : "debuff", 0.55f);
                Enemies[i].Flash(col, 0.4f, 0.55f);
                if (Bolzena.Fx.BolzenaFx.Common(good ? "buff" : "debuff", Enemies[i].Fx) == null)
                    Vfx.Glow(Enemies[i].Center, 2.4f, new Color(col.r, col.g, col.b, 0.7f), 0.5f, 1.8f, "FX_IN_Ring_ShockWave_03", 150);
                // 머리 위 HUD 묶음(HP 막대 · 숫자) 바로 위에서 떠오르게 — 머리 높이에서 띄우면 흐려지는 동안 막대에 겹쳐 이름처럼 보였다
                float wy = Enemies[i].Top.y + 0.25f;
                if (i < EnemyHuds.Count && EnemyHuds[i] != null) wy = Mathf.Min(Mathf.Max(wy, Enemies[i].transform.localPosition.y + EnemyHuds[i].LocalBox.yMax + 0.22f), 3.6f);
                Vfx.Word(new Vector3(Enemies[i].Top.x, wy, 0), id, 0.4f, col, new Color(0.15f, 0, 0.1f));
                string ic = ChipRow.IconOf(id, good);
                var icon = Make.Box("st", FieldRoot, ChipRow.IconSprite(ic), Enemies[i].Top + new Vector3(0.6f, 0, 0), new Vector2(0.6f, 0.6f), 470);
                Clock.Run(Clock.Tween(0.6f, t => { if (icon) { icon.transform.localPosition += new Vector3(0, Time.deltaTime * (good ? 0.3f : -0.3f), 0); Make.Alpha(icon, 1 - t); if (t >= 1) Destroy(icon.gameObject); } }));
            }
            else
            {
                var h = Heroes[Mathf.Clamp(i, 0, Heroes.Count - 1)];
                Hud.SetSnapshot(s);
                Sfx.Play(good ? "buff" : "debuff", 0.5f);
                h.Flash(col, 0.35f, 0.45f);
                if (Bolzena.Fx.BolzenaFx.Common(good ? "buff" : "debuff", h.Fx) == null)
                    Vfx.Glow(h.Center, 2.2f, new Color(col.r, col.g, col.b, 0.6f), 0.45f, 1.6f, "FX_IN_Ring_ShockWave_03", 150);
                Vfx.Word(h.Top + new Vector3(0, 0.3f, 0), id, 0.36f, col, new Color(0.05f, 0.12f, 0.05f));
            }
            yield return Clock.Wait(0.18f);
        }

        void HealFx(BattleEvent e)
        {
            if (e.Target.Side == Side.Party)
            {
                Hud.SetHp(e.HpAfter, false);
                Sfx.Play("buff", 0.5f, 1.1f);
                foreach (var h in Heroes)
                {
                    h.Flash(new Color(0.6f, 1f, 0.6f), 0.35f, 0.45f);
                    if (Bolzena.Fx.BolzenaFx.Common("heal", h.Fx) == null)
                    Vfx.Burst(h.Center, new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Glow", Count = 6, Speed = new Vector2(0.4f, 1.4f), Angle = 90, Spread = 60, Life = new Vector2(0.5f, 0.9f), Size = new Vector2(0.08f, 0.18f),
                        C0 = new Color(0.6f, 1f, 0.6f), C1 = Color.white, Order = 150, Boost = 1.6f, ShrinkTo = 0, Radius = 0.4f,
                    });
                }
                var at = Heroes[Mathf.Clamp(e.Target.Index, 0, Heroes.Count - 1)].Top;
                Vfx.Number(at + new Vector3(0, 0.3f, 0), e.Value, false, 0, new Color(0.6f, 1f, 0.6f));
            }
            else if (e.Target.Index < EnemyHuds.Count && EnemyHuds[e.Target.Index] != null)
            {
                EnemyHuds[e.Target.Index].SetHp(e.HpAfter, false);
                if (e.Target.Index < Enemies.Count && Enemies[e.Target.Index] != null) Bolzena.Fx.BolzenaFx.Common("heal", Enemies[e.Target.Index].Fx);
                Vfx.Number(Enemies[e.Target.Index].Top, e.Value, false, 0, new Color(0.6f, 1f, 0.6f));
            }
        }

        // 격파 — 칸이 다 깨졌다: 느려지고, 흑백으로 번쩍, 「격파!」, 조각이 흩어지고 적이 휘청(그로기)
        IEnumerator BreakFx(int i)
        {
            var t = Enemies[i];
            var at = t.Center;
            Clock.HitStop(0.12f);
            Clock.SlowMo(0.3f, 0.55f);
            Sfx.Play("break", 0.9f);
            FieldRig.Shake(0.65f, 3f);
            FieldRig.Punch(at, 0.09f);
            PostFx.Kick(chroma: 0.9f, lens: -0.28f, sat: -45f, bloom: 0.12f);
            Vfx.Flash(new Color(1f, 0.95f, 0.85f, 0.08f), 0.2f);
            t.Flash(new Color(1f, 0.9f, 0.5f), 0.45f, 0.6f);
            Bolzena.Fx.BolzenaFx.CommonOrderAdd = -150 + fxBoost;
            bool brk = Bolzena.Fx.BolzenaFx.Common("break", t.Fx) != null;   // 원작 기절 별(머리 위)
            Bolzena.Fx.BolzenaFx.CommonOrderAdd = -150;
            if (!brk) Vfx.Ring(at, 0.4f, 5f, 0.45f, new Color(1f, 0.85f, 0.4f, 0.85f), 1.8f, "FX_IN_Ring_ShockWave_01", 190 + fxBoost);
            if (!brk) Vfx.Glow(at, 3.0f, new Color(1f, 0.8f, 0.35f, 0.7f), 0.5f, 1.6f, "FX_IN_Crack_Round_Glow", 189 + fxBoost, null, 1.4f, Random.Range(0f, 360f));
            if (!brk) Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Sliced_Piece_Particle_Gray", Count = 22, Speed = new Vector2(4f, 11f), Life = new Vector2(0.5f, 0.9f), Size = new Vector2(0.12f, 0.32f),
                C0 = new Color(1f, 0.92f, 0.6f), C1 = new Color(0.8f, 0.9f, 1f), Gravity = 1.4f, Spin = true, Drag = 1.5f, Order = 191 + fxBoost, Boost = 2.4f,
            });
            var word = Vfx.Word(t.Top + new Vector3(0, 0.45f, 0), "격파!", 0.95f, new Color(1f, 0.9f, 0.45f), new Color(0.35f, 0.1f, 0), 1.4f, 1.6f, null, 475, 0.4f);
            word.colorGradient = new TMPro.VertexGradient(Color.white, Color.white, new Color(1f, 0.7f, 0.2f), new Color(1f, 0.7f, 0.2f));
            EnemyHuds[i].SetBroken(true);
            t.Loop(t.Resolve("Groggy"));
            Emit(i < Battle.Snapshot.Enemies.Count && Battle.Snapshot.Enemies[i].Boss ? "boss_break" : "break");   // 격파로 웨이브가 끝나면 스냅샷이 먼저 다음 웨이브(적이 더 적을 수 있다)다
            yield return Clock.Wait(0.35f);
        }

        // 쓰러지는 연출을 이미 건 적(보스와 함께 쓰러진 소환물이 뒤에 오는 제 쪽지로 두 번 쓰러지지 않게) — 웨이브마다 새 UnitView 라 비우지 않아도 된다
        readonly HashSet<UnitView> dyingFoes = new HashSet<UnitView>();

        IEnumerator DeathFx(int i, bool boss)
        {
            if (i < 0 || i >= Enemies.Count || Enemies[i] == null) yield break;
            var t = Enemies[i];
            if (!dyingFoes.Add(t)) yield break;
            var at = t.Center;
            EnemyHuds[i].Hide();
            Sfx.Play("death_enemy", 0.7f);
            if (boss)
            {
                // 마지막 일격 — 크게 느려지고, 폭발 · 하얀 화면
                Clock.SlowMo(0.18f, 1.4f);
                Clock.HitStop(0.3f);
                FieldRig.Shake(1.1f, 5f);
                FieldRig.Punch(at, 0.12f);
                // 마지막 일격 — 세상이 어두워지고 보스만 하얗게 남는다
                // 블룸 · 흰 구름은 보스 뒤(316)로 — 앞을 덮으면 무엇이 쓰러지는지 안 보인다
                PostFx.Kick(chroma: 1f, lens: -0.5f, bloom: 0.15f);
                t.SetOrder(320);
                ScreenFx.I.Dim(0.82f, 300, 30f);
                ScreenFx.I.Lines(0.7f, Color.white, FieldRoot.TransformPoint(at), 302, 0.3f);
                Clock.Run(BossKillLift(t));
                Vfx.Sheet("fx_common_hit_explosion_1_m", at, 2.3f, 316, 0, false, 1f, false, 1f, new Color(1f, 0.8f, 0.6f, 0.6f));
                Vfx.Glow(at, 5f, new Color(1f, 0.7f, 0.35f, 0.35f), 0.7f, 1.2f, null, 312, null, 1.3f);
                Vfx.Ring(at, 1f, 16f, 0.9f, new Color(1f, 0.9f, 0.7f, 0.8f), 1.6f, "FX_IN_Ring_Impact_wave_01", 342, null, 1f, true);
                Vfx.Burst(at, new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Spark", Count = 70, Speed = new Vector2(8f, 26f), Life = new Vector2(0.4f, 0.9f), Size = new Vector2(0.08f, 0.24f),
                    C0 = new Color(1f, 0.95f, 0.8f), C1 = new Color(1f, 0.7f, 0.3f), Stretch = true, StretchK = 0.05f, Drag = 2f, Order = 343, Boost = 2.2f,
                });
                Sfx.Play("ult_impact", 1f, 0.8f);
                Emit("boss_kill");
            }
            t.Flash(new Color(1f, 0.97f, 0.9f), 0.5f, boss ? 0.2f : 0.35f);
            float d = t.Play(t.Resolve("Die"), boss ? 1.4f : 1.6f, false);
            yield return Clock.Wait(Mathf.Min(d, 0.7f));
            if (!t || !dyingFoes.Contains(t)) yield break;   // 그사이 다음 웨이브가 들어와 지워졌다 · 되살아났다(ReviveFx)
            // 흰 실루엣으로 녹아 위로 흩어진다 — 원작 쓰러짐 연기, 없으면 자체 빛망울
            bool smoke = Bolzena.Fx.BolzenaFx.Common("kill", t.Fx) != null;
            for (int k = 0; k < (smoke ? 0 : 6); k++)
                Vfx.Burst(t.Feet + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, t.Height() * 0.8f), 0), new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Glow", Count = 4, Speed = new Vector2(0.5f, 2f), Angle = 90, Spread = 50, Life = new Vector2(0.5f, 1f), Size = new Vector2(0.1f, 0.25f),
                    C0 = new Color(1f, 0.95f, 0.85f), Order = 70, Boost = 2.5f, ShrinkTo = 0,
                });
            yield return Clock.Tween(0.5f, k => { if (t && dyingFoes.Contains(t)) { t.SetAlpha(1 - k); t.Flash(Color.white, 0.05f); } });
            if (t && dyingFoes.Contains(t)) t.gameObject.SetActive(false);
        }

        // 되살아남(core revive — 호바깅 · 불쌍한의 「재 속」 부활, 산사모 가사에서 일어섬) — 쓰러짐 표(dyingFoes)에서 빼고 몸 · 머리 위 묶음을 다시 켠다.
        //   연출: 발밑 불씨가 솟고 몸이 아래에서 떠오르며 흰빛으로 번쩍 → 「부활!」 · 체력 · 강인도 · 의도 칸이 지금 값으로
        IEnumerator ReviveFx(int i, BattleSnapshot s)
        {
            if (i < 0 || i >= Enemies.Count || Enemies[i] == null) yield break;
            var t = Enemies[i];
            dyingFoes.Remove(t);
            Debug.Log($"[Battle] 되살아남 적 {i} {t.name} · 쓰러짐 연출 중이었나 {!t.gameObject.activeSelf}");
            t.gameObject.SetActive(true);
            t.SetAlpha(0);
            t.Idle();
            var es = i < s.Enemies.Count ? s.Enemies[i] : null;
            if (i < EnemyHuds.Count && EnemyHuds[i] != null) EnemyHuds[i].Show(es);
            Sfx.Play("buff", 0.6f, 0.8f);
            Vfx.Burst(t.Feet + new Vector3(0, 0.1f, 0), new Vfx.BurstOpt
            {
                Tex = "FX_IN_Glow", Count = 18, Speed = new Vector2(1f, 3.5f), Angle = 90, Spread = 40, Life = new Vector2(0.5f, 1.0f), Size = new Vector2(0.1f, 0.26f),
                C0 = new Color(1f, 0.75f, 0.35f), C1 = new Color(1f, 0.95f, 0.8f), Order = 150, Boost = 2.2f, ShrinkTo = 0, Radius = 0.5f,
            });
            Vfx.Ring(t.Feet + new Vector3(0, 0.15f, 0), 0.3f, 3.2f, 0.5f, new Color(1f, 0.85f, 0.5f, 0.8f), 1.6f, "FX_IN_Ring_ShockWave_01", 149);
            var home = t.transform.localPosition;
            yield return Clock.Tween(0.45f, k =>
            {
                if (!t) return;
                t.SetAlpha(k);
                t.transform.localPosition = home + new Vector3(0, -0.35f * (1 - k), 0);
                t.Flash(Color.white, 0.6f * (1 - k));
            });
            if (!t) yield break;
            t.transform.localPosition = home;
            t.SetAlpha(1);
            t.Flash(new Color(1f, 0.95f, 0.8f), 0.4f, 0.4f);
            Vfx.Word(t.Top + new Vector3(0, 0.35f, 0), "부활!", 0.55f, new Color(1f, 0.85f, 0.45f), new Color(0.3f, 0.12f, 0), 1.1f, 1.3f);
            yield return Clock.Wait(0.35f);
        }

        IEnumerator BossKillLift(UnitView t)
        {
            yield return Clock.WaitU(1.1f);
            ScreenFx.I.Dim(0, 300, 2.5f);
            ScreenFx.I.Lines(0, Color.white, Vector2.zero);
        }
    }
}
