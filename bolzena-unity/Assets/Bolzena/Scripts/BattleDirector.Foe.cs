using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 적 몸짓 — 적 차례 동작 · 적 고학년 · 힘 모으기 · 클론 · 사도가 맞는 쪽(Hurt)
    //   2026-11-17(12주차) BattleDirector.Acts.cs 에서 글자 그대로 옮겼다 — 동작 · 값 그대로(ADR-004).
    public partial class BattleDirector
    {
        // ── 보스 클론 고학년(엔진 cue foeUlt* · 보스_고학년.md) ── 예고: 의도 칸이 얼굴 · 이름으로 붉게 맥동(EnemyHud) + 머리 위 경고 ·
        //   사용: 붉은 테 컷인(오른쪽에서) → 그 사도의 고학년 몸짓 → 치는 수마다 붉은 번쩍 · 흔들림 → 끝: 테를 걷음 · 끊김: 「끊김!」 · 깨지는 조각
        readonly List<SpriteRenderer> foeFrame = new List<SpriteRenderer>();
        IEnumerator FoeUltFx(BattleEvent e)
        {
            int ei = e.Actor.Index;
            var u = ei >= 0 && ei < Enemies.Count ? Enemies[ei] : null;
            var es = ei >= 0 && ei < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[ei] : null;
            Debug.Log($"[FoeUlt] {e.Text} {(u ? u.name : "-")} 「{e.Say}」 v={e.Value} {e.Anim}");
            Emit("foeult_" + e.Text);
            switch (e.Text)
            {
                case "warn":
                    RefreshHud();
                    if (u)
                    {
                        u.Flash(new Color(1f, 0.3f, 0.25f), 0.4f, 0.8f);
                        Vfx.Glow(u.Center, 3.2f, new Color(1f, 0.2f, 0.15f, 0.8f), 0.5f, 2.5f, "FX_IN_Ring_ShockWave_03", 186);
                        Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 1.0f, 3.3f), 0), "고학년 예고 「" + e.Say + "」", 0.36f, new Color(1f, 0.55f, 0.45f), new Color(0.3f, 0, 0), 1.6f, 1.3f, null, 476, 0.4f);
                        Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 0.55f, 2.85f), 0), $"다음 턴 {e.Value}", 0.3f, new Color(1f, 0.85f, 0.8f), new Color(0.3f, 0, 0), 1.6f, 1.2f, null, 476, 0.4f);
                    }
                    Sfx.Play("turn_start", 0.6f, 0.75f);
                    yield return Clock.Wait(0.6f);
                    break;
                case "start":
                {
                    FoeFrame(true);   // 적 클론 고학년은 컷인 화면 없이 바로 몸짓으로(2026-10-12 사용자)
                    break;
                }
                case "hit":
                    break;   // 치는 때는 EnemyAct 가 맞는 순간에(FoeUltBeat)
                case "end":
                    FoeFrame(false);
                    break;
                case "cut":
                {
                    FoeFrame(false);
                    RefreshHud();
                    yield return CutFx(u, $"{e.Anim ?? "격파"} — 「{e.Say}」");
                    break;
                }
            }
        }

        // 「끊김!」 — 예고한 큰 수를 격파로 끊었다(보스 고학년 foeUltCut · 엘리트 · 일반 힘 모으기). 깨지는 조각 · 흰 번쩍 · 약 1초
        //   underBreak — 바로 앞 「격파!」 글(머리 위 Top+0.45)과 겹치지 않게 글을 몸 쪽으로 내린다(엘리트 · 일반 — 키가 작아 Center+1.2 가 머리 위와 겹쳤다)
        IEnumerator CutFx(UnitView u, string sub, bool underBreak = false)
        {
            var at = u ? u.Center : new Vector3(4f, 0, 0);
            float wy = 1.2f;
            if (underBreak && u) wy = Mathf.Min(wy, u.Top.y - 0.5f - at.y);
            Clock.HitStop(0.12f);
            FieldRig.Shake(0.5f, 2f);
            Vfx.Flash(new Color(1f, 1f, 1f, 0.35f), 0.2f);
            Vfx.Burst(at, new Vfx.BurstOpt
            {
                Tex = "FX_IN_Spark", Count = 26, Speed = new Vector2(4f, 11f), Life = new Vector2(0.35f, 0.7f), Size = new Vector2(0.08f, 0.22f),
                C0 = new Color(1f, 0.6f, 0.5f), C1 = new Color(0.6f, 0.1f, 0.1f), Gravity = 6f, Spin = true, Angle = 90, Spread = 360, Order = 476, Boost = 2.5f,
            });
            Vfx.Glow(at, 3.5f, new Color(1f, 0.85f, 0.7f, 0.9f), 0.35f, 3f, "FX_IN_Ring_ShockWave_03", 475);
            Vfx.Word(at + new Vector3(0, wy, 0), "끊김!", 0.8f, new Color(1f, 0.95f, 0.85f), new Color(0.45f, 0.05f, 0.05f), 1.0f, 1.6f, null, 480, 0.3f);
            Vfx.Word(at + new Vector3(0, wy - 0.65f, 0), sub, 0.3f, new Color(1f, 0.8f, 0.7f), new Color(0.2f, 0, 0), 1.0f, 1.2f, null, 478, 0.35f);
            Sfx.Play("block_hit", 0.8f, 0.7f);
            Sfx.Play("ult_impact", 0.5f, 0.8f);
            yield return Clock.Wait(0.7f);
        }

        // ── 엘리트 · 일반 적 힘 모으기(엔진 cue foeChargeWarn · API v2.8) ── 보스 고학년보다 한 단계 약하게: 붉은 대신 주황, 얼굴 · 컷인 없음.
        //   모으기: 주황 번쩍 · 고리 하나 + 머리 위 짧은 「힘 모으는 중」 띠(의도 칸은 EnemyHud 가 「다음 차례」 · 은은한 맥동) ·
        //   쏟기: EnemyAct(Anim "charge") 가 그 수 이름 띠 · 조금 센 타격 · 끊김: 「끊김!」(CutFx)
        IEnumerator FoeChargeFx(BattleEvent e)
        {
            int ei = e.Actor.Index;
            var u = ei >= 0 && ei < Enemies.Count ? Enemies[ei] : null;
            Debug.Log($"[FoeCharge] {e.Text} {(u ? u.name : "-")} 「{e.Say}」 v={e.Value} {e.Anim}");
            Emit("foecharge_" + e.Text);
            // 그 적의 머리 위만 새로(RefreshHud 는 손까지 맞춰서, 적 차례 중간에 부르면 아직 안 뽑은 손이 먼저 깔린다)
            var snapE = ei >= 0 && ei < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[ei] : null;
            if (snapE != null && ei < EnemyHuds.Count && EnemyHuds[ei] != null && !snapE.Dead) { EnemyHuds[ei].SetIntent(snapE); EnemyHuds[ei].SetChips(snapE.Chips); }
            switch (e.Text)
            {
                case "warn":
                    if (!u) break;
                    u.Flash(new Color(1f, 0.62f, 0.3f), 0.3f, 0.6f);
                    Vfx.Glow(u.Center, 2.4f, new Color(1f, 0.55f, 0.2f, 0.6f), 0.45f, 2.2f, "FX_IN_Ring_ShockWave_03", 186);
                    ChargeBand(u, "힘 모으는 중", 0.28f, 1.3f);
                    Sfx.Play("turn_start", 0.4f, 0.9f);
                    yield return Clock.Wait(0.4f);
                    break;
                case "cut":
                    yield return CutFx(u, $"격파 — 「{e.Say}」", true);
                    break;
            }
        }

        // 머리 위 짧은 띠 — 머리 위 묶음(EnemyHud) 위 끝 바로 위에, 화면 위 HUD 에 닿지 않게
        void ChargeBand(UnitView u, string text, float size, float life)
        {
            float y = u.Top.y + 0.75f;
            int ei = Enemies.IndexOf(u);
            if (ei >= 0 && ei < EnemyHuds.Count && EnemyHuds[ei] != null)
                y = u.transform.localPosition.y + EnemyHuds[ei].LocalBox.yMax + 0.22f;
            Vfx.Word(new Vector3(u.Top.x, Mathf.Min(y, 3.35f), 0), text, size, new Color(1f, 0.78f, 0.45f), new Color(0.32f, 0.12f, 0), life, 1.15f, null, 474, 0.3f);
        }

        // 보스 고학년 한 수 — 붉은 번쩍 · 흔들림 · 붉은 충격 고리(마지막 수는 더 크게)
        void FoeUltBeat(UnitView u, bool last)
        {
            Vfx.Flash(new Color(1f, 0.15f, 0.1f, last ? 0.35f : 0.22f), last ? 0.3f : 0.18f);
            FieldRig.Shake(last ? 1.0f : 0.6f, last ? 3.5f : 2.5f);
            PostFx.Kick(chroma: last ? 1f : 0.6f, lens: -0.15f);
            var mid = Vector3.zero; int n = 0;
            foreach (var h in Heroes) if (h) { mid += h.Center; n++; }
            if (n > 0) Vfx.Glow(mid / n, last ? 6f : 4.5f, new Color(1f, 0.25f, 0.2f, 0.85f), 0.35f, 2.5f, "FX_IN_Ring_ShockWave_03", 188);
        }

        // 붉은 테 — 보스 고학년 동안 화면 가장자리를 붉게(맥동은 ScreenFit 이 따라 늘인다)
        void FoeFrame(bool on)
        {
            foreach (var f in foeFrame) if (f) Destroy(f.gameObject);
            foeFrame.Clear();
            if (!on || ScreenRoot == null) return;
            float hw = Tone.HalfW, hh = Tone.HalfH, t = 0.22f;
            var c = new Color(0.9f, 0.08f, 0.06f, 0.75f);
            foeFrame.Add(Make.Box("foeTop", ScreenRoot, Res.UI("white"), new Vector3(0, hh - t / 2, 0), new Vector2(hw * 2 + 1, t), 690, c));
            foeFrame.Add(Make.Box("foeBot", ScreenRoot, Res.UI("white"), new Vector3(0, -hh + t / 2, 0), new Vector2(hw * 2 + 1, t), 690, c));
            foeFrame.Add(Make.Box("foeL", ScreenRoot, Res.UI("white"), new Vector3(-hw + t / 2, 0, 0), new Vector2(t, hh * 2), 690, c));
            foeFrame.Add(Make.Box("foeR", ScreenRoot, Res.UI("white"), new Vector3(hw - t / 2, 0, 0), new Vector2(t, hh * 2), 690, c));
            Clock.Run(FoeFramePulse());
        }

        IEnumerator FoeFramePulse()
        {
            float t = 0;
            while (foeFrame.Count > 0)
            {
                t += Time.unscaledDeltaTime;
                float a = 0.45f + 0.35f * Mathf.Sin(t * 7f);
                foreach (var f in foeFrame) if (f) Make.Alpha(f, a);
                yield return null;
            }
        }

        // ── 적의 몸짓 ──
        IEnumerator EnemyAct(BattleEvent act, List<BattleEvent> group)
        {
            int ei = act.Actor.Index;
            if (ei >= Enemies.Count) SpawnSummons();
            if (ei < 0 || ei >= Enemies.Count) yield break;
            var u = Enemies[ei];
            int seq = BeginAct(u);
            Split(group, out var pre, out var byHit, out int hits);
            bool foeUlt = act.Text == "foeult";
            if (foeUlt) Clock.Base = 1f;          // 보스 클론 고학년 — 원작 길이 원칙(사도 고학년과 같이 템포를 걸지 않는다)
            bool spend = act.Anim == "charge";   // 모은 힘을 쏟는 턴(엘리트 · 일반 힘 모으기)
            bool heavy = act.Text == "heavy" || foeUlt;
            string anim = u.AnimFor(act.Motion);
            float speed = foeUlt ? 1.05f : heavy ? 1.1f : 1.25f;
            var marks = u.Strikes(anim);
            if (!OldTempo && hits >= 1 && marks.Count > hits) marks = marks.GetRange(0, hits);   // 타수만큼 앞 타격에(1타 수가 다타 몸짓의 마지막 휘두름까지 기다리지 않게)
            var times = HitTimes(marks, Mathf.Max(1, hits), speed);
            string key = u.name.Replace("enemy_", "");
            // 클론(적이 된 사도) — 그 사도의 원작 이펙트(카드 · 고학년) · 타격 갈래 · 근접이면 뛰어들기를 빌린다(2026-10-14 사용자 「클론에 사도 이펙트」)
            var es = ei < Battle.Snapshot.Enemies.Count ? Battle.Snapshot.Enemies[ei] : null;
            string cloneHero = CloneHeroOf(es?.Id);
            var cloneLook = cloneHero != null ? Look.Hero(cloneHero) : null;
            string hitKind = cloneLook != null ? CLONE_HIT[(int)cloneLook.Hit] : null;
            Debug.Log($"[Tempo] 적 몸짓 {key} {anim} — 타수 {hits} · 애니 타격 {u.Strikes(anim).Count} · 애니 {u.Duration(u.F(anim)):F2}s · 첫 타격 {(marks.Count > 0 ? marks[0] : 0):F2}s{(foeUlt ? " · 고학년" : heavy ? " · 강타" : "")}");

            u.SetOrder(60);
            foreach (var en in Enemies) if (en != u) en.Tint(new Color(0.62f, 0.62f, 0.68f));
            if (act.Up)
            {
                Vfx.Word(u.Top + new Vector3(0, 1.0f, 0), "즉시 행동!", 0.42f, new Color(1f, 0.85f, 0.3f), new Color(0.3f, 0.1f, 0), 1.0f, 1.2f);
                Sfx.Play("turn_start", 0.5f, 1.3f);
                yield return Clock.Wait(OldTempo ? 0.35f : 0.25f);
            }
            if (spend)
            {
                // 모은 힘을 쏟는다 — 그 수 이름 띠(주황, 보통 수 이름보다 크게) · 주황 고리 · 집중선
                Emit("foecharge_spend");
                ChargeBand(u, "「" + (act.Say ?? "") + "」", 0.42f, 1.5f);
                u.Flash(new Color(1f, 0.6f, 0.25f), 0.35f, 0.5f);
                Vfx.Glow(u.Center, 3f, new Color(1f, 0.5f, 0.15f, 0.75f), 0.4f, 2.6f, "FX_IN_Ring_ShockWave_03", 186);
                if (heavy) ScreenFx.I.Lines(0.6f, new Color(1f, 0.55f, 0.25f), FieldRoot.TransformPoint(u.Center), 302, 0.3f);
                Sfx.Play("turn_start", 0.45f, 0.8f);
                yield return Clock.Wait(OldTempo ? 0.4f : 0.3f);
            }
            else if (!string.IsNullOrEmpty(act.Say))
                Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 1.2f, 3.45f), 0), "「" + act.Say + "」", 0.24f, new Color(1f, 0.95f, 0.88f), new Color(0.1f, 0.05f, 0.1f), 1.3f, 1f, null, 472, 0.2f);
            if (heavy && !foeUlt && !spend)
            {
                // 큰 수 — 붉은 경고, 집중선
                Vfx.Word(u.Top + new Vector3(0, 0.6f, 0), "강타!", 0.7f, new Color(1f, 0.5f, 0.35f), new Color(0.3f, 0, 0), 1.0f, 1.5f);
                ScreenFx.I.Lines(0.55f, new Color(1f, 0.4f, 0.3f), FieldRoot.TransformPoint(u.Center), 302, 0.3f);
                yield return Clock.Wait(OldTempo ? 0.3f : 0.2f);
            }
            float dur = u.Play(anim, speed);
            bool attack = hits > 0;
            Sfx.Play("monster/" + key + "/" + key + (attack ? (heavy ? "_skillcast" : "_basicattack") : "_skillcast"), 0.7f);
            bool lunge = attack && (key == "fairymobcloserange" || (cloneLook != null && cloneLook.Melee && !foeUlt));   // 근접 사도 클론도 뛰어든다(고학년은 몸짓이 알아서 움직인다)
            if (lunge) StartCoroutine(u.MoveTo(u.Home + new Vector3(-2.4f, 0, 0), 0.18f, Ease.InCubic));
            else if (attack) StartCoroutine(u.MoveTo(u.Home + new Vector3(-0.4f, 0, 0), 0.15f));
            var clk = cloneHero != null && attack ? CloneFx(u, cloneHero, anim, marks, speed, foeUlt, times, byHit) : null;

            foreach (var e in pre) yield return ApplyConsequence(e, u);
            float elapsed = 0;
            int k = 0;
            foreach (var kv in byHit)
            {
                float at = k < times.Count ? times[k] : times[times.Count - 1] + 0.12f;
                while (elapsed < at) { yield return null; elapsed += Time.deltaTime; if (clk != null) clk.Ms = elapsed * 1000f; }
                if (foeUlt) FoeUltBeat(u, k == byHit.Count - 1);
                else if (spend && k == 0) { Vfx.Flash(new Color(1f, 0.55f, 0.2f, 0.2f), 0.15f); FieldRig.Shake(0.45f, 2f); }   // 쏟는 첫 수 — 조금 센 타격감(보스 고학년 붉은 번쩍보다 약하게)
                foreach (var e in kv.Value)
                {
                    if (e.Kind == EventKind.PartyHurt) Hurt(e, key, heavy, hitKind);
                    else yield return ApplyConsequence(e, u);
                }
                k++;
            }
            if (heavy) ScreenFx.I.Lines(0, Color.white, Vector2.zero);
            float tail = foeUlt ? dur : Mathf.Min(dur, elapsed + 0.4f);   // 보스 고학년은 몸짓을 끝까지
            // 보통 수 — 맞힌 뒤 잠깐만 붙들고 다음 적이 바로 시작한다(몸짓 꼬리 · 복귀는 뒤에서 — 적끼리 겹친다)
            if (!foeUlt && !OldTempo)
            {
                float hold = Mathf.Min(tail, elapsed + HOLD_AFTER_HIT);
                while (elapsed < hold) { yield return null; elapsed += Time.deltaTime; if (clk != null) clk.Ms = elapsed * 1000f; }
                StartCoroutine(Finish(u, seq, Time.time - elapsed, tail, clk, false, 0.2f, u.BaseOrder, false));
                yield break;
            }
            while (elapsed < tail) { yield return null; elapsed += Time.deltaTime; if (clk != null) clk.Ms = elapsed * 1000f; }
            if (clk != null) clk.Ms = tail * 1000f;   // 사도 쪽 Finish 와 같이 몸짓 끝까지
            if (foeUlt) u.Idle();
            if (u.Feet != u.Home)
            {
                yield return u.Return(0.2f);
            }
            u.SetOrder(u.BaseOrder);
            foreach (var en in Enemies) if (en) en.Tint(Color.white);
            if (foeUlt) Clock.Base = Clock.TEMPO;
            yield return Clock.Wait(0.15f);
        }

        // 클론 타격 갈래 — HitKind(Slash · Blunt · Magic · Shot) 차례 → 원작 공용 타격 이름(MotionTables.HIT_FX)
        static readonly string[] CLONE_HIT = { "slash", "blunt", "magic", "shot" };

        /// <summary>그 적이 사도 클론이면 그 사도 키(「루드」), 아니면 null.</summary>
        string CloneHeroOf(string enemyId) => enemyId == null ? null : Look.Data?.Enemy(enemyId)?.Clone;

        /// <summary>클론 몸짓에 그 사도의 원작 이펙트를 건다 — 공격은 그 몸짓(Attack1 · Skill1)의 카드 이펙트, 고학년은 원작 고학년 한 벌.
        /// 맞는 쪽은 클론이 때린 사도들. 라이브러리가 시전자 → 대상 방향으로 좌우를 뒤집는다(FxRun flip = to.x &lt; from.x). 튼 장이 없으면 null</summary>
        Bolzena.Fx.UltClock CloneFx(UnitView u, string hero, string anim, List<float> marks, float speed, bool ult, List<float> times, SortedDictionary<int, List<BattleEvent>> byHit)
        {
            var tfx = new List<Bolzena.Fx.FxActor>();
            var seen = new HashSet<int>();
            foreach (var kv in byHit)
                foreach (var e in kv.Value)
                    if (e.Kind == EventKind.PartyHurt && e.Target.Index >= 0 && e.Target.Index < Heroes.Count && Heroes[e.Target.Index] && seen.Add(e.Target.Index)) tfx.Add(Heroes[e.Target.Index].Fx);
            if (tfx.Count == 0) return null;
            var clk = new Bolzena.Fx.UltClock();
            var ms = new List<int>();
            foreach (var m in marks) ms.Add(Mathf.RoundToInt(m * 1000));
            var hitMs = new List<float>();
            foreach (var t in times) hitMs.Add(t * 1000f);
            var sync = new Bolzena.Fx.FxSync { Anim = anim, Impact = ms.Count > 0 ? ms[0] : (int?)null, Marks = ms, End = ms.Count > 0 ? ms[ms.Count - 1] : (int?)null, Speed = speed };
            bool aoe = tfx.Count > 1;
            var c = ult && Bolzena.Fx.FxLibrary.HasUlt(hero)
                ? Bolzena.Fx.BolzenaFx.Ult(hero, u.Fx, tfx, null, sync, aoe, null, hitMs, clk)
                : Bolzena.Fx.BolzenaFx.Card(hero, u.Fx, tfx, sync, null, aoe, hitMs, clk);
            Debug.Log($"[CloneFx] {hero} {anim}{(ult ? " 고학년" : "")} — 장 {(c != null ? c.Parts : 0)} · 대상 {tfx.Count}");
            if (c != null && c.Parts > 0) Emit(ult ? "clone_fx_ult" : "clone_fx_atk");   // 데모 — 그 몸짓을 프레임으로 찍는다
            return c != null && c.Parts > 0 ? clk : null;
        }

        void Hurt(BattleEvent e, string enemyKey, bool heavy, string hitKind = null)
        {
            var h = Heroes[e.Target.Index];
            var at = h.Center + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.2f), 0);
            Clock.HitStop(heavy ? 0.13f : 0.07f);
            FieldRig.Shake(heavy ? 0.6f : 0.32f, heavy ? 3f : 1f);
            FieldRig.Punch(at, heavy ? 0.06f : 0.03f);
            h.Flash(e.Value > 0 ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 0.85f, 1f), 0.16f, 0.5f);
            h.Knock(-1f, heavy ? 0.45f : 0.25f);
            if (e.Value > 0)
            {
                ScreenFx.I.Hurt(heavy ? 0.85f : 0.55f);
                if (HitSheets) { Bolzena.Fx.BolzenaFx.HitOrder = 182; Bolzena.Fx.BolzenaFx.Hit(h.Fx, hitKind ?? Bolzena.Fx.MotionTables.HitKindEnemy(enemyKey), heavy); }   // 원작 공용 타격(사도가 맞으면 뒤집힌다) · 클론은 그 사도의 갈래(마법 사도 클론이 베기로 나오던 것)
                else
                {
                    Vfx.Sheet("fx_common_hit_1_m", at, 1.0f, 182, Random.Range(-20f, 20f), true, 1f, false, 1f, new Color(1f, 0.6f, 0.5f));
                    Vfx.Burst(at, new Vfx.BurstOpt
                    {
                        Tex = "FX_IN_Spark", Count = 10, Speed = new Vector2(6f, 12f), Life = new Vector2(0.12f, 0.3f), Size = new Vector2(0.06f, 0.14f),
                        C0 = Color.white, C1 = new Color(1f, 0.4f, 0.3f), Stretch = true, StretchK = 0.045f, Drag = 4f, Angle = 170, Spread = 140, Order = 186, Boost = 3.5f,
                    });
                }
                if (Random.value < 0.4f) Sfx.Voice(h.name.Replace("hero_", ""), "hit");
            }
            if (e.Blocked > 0)
            {
                if (Bolzena.Fx.BolzenaFx.Common("shieldHit", h.Fx) == null) Vfx.Glow(at, 2f, new Color(0.5f, 0.8f, 1f, 0.9f), 0.25f, 3f, "FX_IN_Ring_ShockWave_03", 187);
                Sfx.Play("block_hit", 0.7f);
            }
            Sfx.Play("monster/" + enemyKey + "/" + enemyKey + "_basicattack_hit", 0.7f, 1f, true);
            Sfx.Play("hurt", 0.5f, 1f, true);
            Vfx.Number(at + new Vector3(0, 0.5f, 0), e.Value, false, e.Hit, null, true, e.Blocked);
            Hud.SetHp(e.HpAfter);
            Hud.SetBlock(e.BlockAfter);
            if (heavy)
            {
                Vfx.Flash(new Color(1f, 0.3f, 0.2f, 0.25f), 0.25f);
                PostFx.Kick(chroma: 0.8f, lens: -0.2f);
            }
            if (!firstHurtShot) { firstHurtShot = true; Emit("enemy_attack"); }
        }
    }
}
