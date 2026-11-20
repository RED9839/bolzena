using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 연출 신호(엔진 cue) · 변신 — CUE2 표 · 신호 연출 · 턴 시작 발동 · 꿈결 형상
    //   2026-11-17(12주차) BattleDirector.Acts.cs 에서 글자 그대로 옮겼다 — 동작 · 값 그대로(ADR-004).
    public partial class BattleDirector
    {
        // ── 원작 방식 고학년 둘째 묶음(15명) cue ── 한 줄 = 원작 장 · 자리 · 띠 글 · 빛깔.
        //   At: caster(시전자) · aim(지금 몸짓의 대상 — 마무리 · 기절 직전) · mid(적 무리 가운데) · each(맞는 적마다 — 턴 발동의 피해 박자)
        //   Strike: 턴 시작 · 끝처럼 몸짓 밖에서 오면 뒤따르는 피해를 묶어 이 장과 함께 친다(Cap: V 대 · 1 · 살아 있는 적 전부)
        //   Finish: 마무리 일격(즉사 대신 「HP 낮은 적에게 큰 덤」 — 티그(영웅) · 로니 · 아르코 · 기절 직전) — 멈칫 · 확대 · 큰 띠
        class CueLook { public string Fx; public string At = "caster"; public string Word; public Color Tint = Color.white; public bool Strike, Finish; public int Cap; public float Until = 1.4f; }
        static readonly Dictionary<string, CueLook> CUE2 = new Dictionary<string, CueLook>
        {
            ["tig_finish"] = new CueLook { Fx = "fx_tighero_ultimate_cros_1", At = "aim", Word = "결착!", Tint = new Color(1f, 0.75f, 0.35f), Finish = true },
            ["roni_finish"] = new CueLook { Fx = "fx_ronnie_outlaw_2", At = "aim", Word = "수사 종결!", Tint = new Color(1f, 0.55f, 0.45f), Finish = true },
            ["arco_crit"] = new CueLook { Fx = "fx_arco_ultimate_ground_4", At = "aim", Word = "확정 치명!", Tint = new Color(1f, 0.85f, 0.4f), Finish = true },
            ["benny_honey_stun"] = new CueLook { Fx = "fx_benibeni_skill_explosion_2", At = "aim", Word = "꿀단지 강타!", Tint = new Color(1f, 0.82f, 0.3f), Finish = true },
            ["amelia_shock_stun"] = new CueLook { Fx = "fx_amelia_ultimate_laser_2", At = "aim", Word = "감전 기절!", Tint = new Color(0.6f, 0.85f, 1f), Finish = true },
            ["r41_shock_stun"] = new CueLook { Fx = "fx_ameliar41_ultimate_explosion_1", At = "aim", Word = "감전 기절!", Tint = new Color(0.6f, 0.85f, 1f), Finish = true },
            ["yomi_moon_summon"] = new CueLook { Fx = "fx_yomi_ultimate_1", At = "mid", Word = "구름 걷는 달빛", Tint = new Color(0.8f, 0.85f, 1f) },
            ["yomi_moonlight"] = new CueLook { Fx = "fx_yomi_ultimate_3", At = "each", Word = "달빛", Tint = new Color(0.8f, 0.85f, 1f), Strike = true },
            ["sherum_zone"] = new CueLook { Fx = "fx_sherum_ultimate_area_1", At = "mid", Word = "회한의 영역", Tint = new Color(0.75f, 0.6f, 1f), Until = 1.8f },
            ["sherum_regret"] = new CueLook { Fx = "fx_sherum_ultimate_ink_1", At = "each", Word = "흑역사!", Tint = new Color(0.75f, 0.6f, 1f), Strike = true, Cap = 1 },
            ["levi_papers"] = new CueLook { Fx = "fx_levigraduate_ultimate_1", At = "mid", Word = "서류 폭풍", Tint = new Color(0.95f, 0.95f, 0.85f), Strike = true, Until = 1.8f },
            ["ui_pond"] = new CueLook { Fx = "fx_uimemory_attack2_pond_1_default", At = "mid", Word = "기억의 연못", Tint = new Color(0.6f, 0.9f, 1f), Until = 1.8f },
            ["ui_pond_wave"] = new CueLook { Fx = "fx_uimemory_attack2_pond_1_default", At = "mid", Word = "일렁임", Tint = new Color(0.6f, 0.9f, 1f), Strike = true },
            ["epica_concert"] = new CueLook { Fx = "fx_epica_ultimate_buff_start", At = "caster", Word = "연주 시작!", Tint = new Color(1f, 0.8f, 0.5f) },
            ["epica_play"] = new CueLook { Fx = "fx_epica_ultimate_battlefield_epicon", At = "each", Word = "에피콘 합주", Tint = new Color(1f, 0.8f, 0.5f), Strike = true, Cap = -1 },
            ["epica_finale"] = new CueLook { Fx = "fx_epica_ultimate_buff_top", At = "caster", Word = "만족스러운 연주", Tint = new Color(1f, 0.85f, 0.55f) },
            ["prickle_vine"] = new CueLook { Fx = "fx_fricle_ultimate_vine_1", At = "mid", Word = "가시 덩굴", Tint = new Color(0.6f, 1f, 0.6f) },
            ["prickle_tentacle"] = new CueLook { Fx = "fx_fricle_ultimate_ground_01_start", At = "mid", Word = "가시 촉수!", Tint = new Color(0.6f, 1f, 0.6f) },
            ["heidi_next_scoop"] = new CueLook { Fx = "fx_heidi_ultimate_news_1", At = "mid", Word = "다음 특종!", Tint = new Color(1f, 0.9f, 0.6f) },
            ["skia_knot"] = new CueLook { Fx = "fx_skea_ultimate_orb_1", At = "aim", Word = "언약의 매듭", Tint = new Color(0.85f, 0.7f, 1f) },
            ["skia_knot_burst"] = new CueLook { Fx = "fx_skea_ultimate_explosion_bottom_1", At = "mid", Word = "매듭 폭발!", Tint = new Color(0.85f, 0.7f, 1f), Finish = true },
            ["alice_sun"] = new CueLook { Fx = "fx_alice_cardlight_1", At = "caster", Word = "태양", Tint = new Color(1f, 0.75f, 0.4f) },
            ["alice_tower"] = new CueLook { Fx = "fx_alice_cardlight_1", At = "caster", Word = "탑", Tint = new Color(0.75f, 0.8f, 1f) },
            ["alice_star"] = new CueLook { Fx = "fx_alice_cardlight_2", At = "caster", Word = "별", Tint = new Color(1f, 0.95f, 0.6f) },
        };

        Vector3 EnemyMid()
        {
            var all = new List<int>(); for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) all.Add(i);
            return all.Count > 0 ? MidBody(all) : new Vector3(4f, -0.6f, 0);
        }

        // 몸짓 안에서 온 cue(띠 · 장만) — 마무리면 멈칫 · 번쩍 · 흔들림
        IEnumerator Cue2Fx(BattleEvent e, UnitView h, CueLook c)
        {
            Vector3 at = c.At == "aim" ? actAim ?? EnemyMid() : c.At == "caster" ? h.Center : EnemyMid();
            if (c.Fx != null) Bolzena.Fx.BolzenaFx.Play(c.Fx, new Bolzena.Fx.FxPlayOptions { At = c.At == "mid" ? new Vector3(at.x, at.y - 0.6f, 0) : at, Until = c.Until, Order = 195, Parent = FieldRoot });
            if (c.Word != null) Vfx.Word((c.At == "caster" ? h.Top : at + new Vector3(0, 1.0f, 0)) + new Vector3(0, 0.4f, 0), c.Word, c.Finish ? 0.62f : 0.42f, c.Tint, new Color(0.25f, 0.08f, 0.02f), 1.2f, c.Finish ? 1.5f : 1.2f, null, 478, 0.35f);
            if (c.Finish)
            {
                Clock.HitStop(0.14f);
                Vfx.Flash(new Color(c.Tint.r, c.Tint.g, c.Tint.b, 0.3f), 0.2f);
                FieldRig.Shake(0.7f, 3f);
                FieldRig.Punch(at, 0.05f);
                Sfx.Play("ult_impact", 0.6f, 1.15f);
                yield return Clock.Wait(0.25f);
            }
            else Sfx.Play("buff", 0.45f, 1.1f);
        }

        // 몸짓 밖(턴 시작 · 끝)에서 온 발동 — 그 장과 함께 뒤따르는 피해를 친다
        IEnumerator Cue2Strike(BattleEvent cue, List<BattleEvent> group, CueLook c)
        {
            int i = Mathf.Clamp(cue.Actor.Index, 0, Heroes.Count - 1);
            var u = Heroes[i];
            var dmg = group.FindAll(x => x.Kind == EventKind.Damage && !IsShot(x));
            Debug.Log($"[FxCue] {cue.Text} {(u ? u.name : "-")} v={cue.Value} 피해 {dmg.Count}");
            Emit(cue.Text);
            if (u) u.Flash(c.Tint, 0.3f, 0.6f);
            var tgts = new List<int>(); foreach (var d in dmg) if (!tgts.Contains(d.Target.Index) && d.Target.Index < Enemies.Count) tgts.Add(d.Target.Index);
            Vector3 mid = tgts.Count > 0 ? MidBody(tgts) : EnemyMid();
            if (c.Word != null) Vfx.Word(mid + new Vector3(0, 1.5f, 0), c.Word + (cue.Value > 1 && c.Cap < 0 ? $" ×{cue.Value}" : ""), 0.42f, c.Tint, new Color(0.2f, 0.06f, 0.02f), 1.2f, 1.2f, null, 478, 0.35f);
            float t = 0, imp = c.Fx != null ? Bolzena.Fx.FxLibrary.ImpactSec(c.Fx) : 0;
            if (c.At != "each" && c.Fx != null) Bolzena.Fx.BolzenaFx.Play(c.Fx, new Bolzena.Fx.FxPlayOptions { At = new Vector3(mid.x, mid.y - 0.6f, 0), Until = c.Until, Order = 190, Parent = FieldRoot });
            float first = c.At == "each" ? Mathf.Max(0.3f, imp) : Mathf.Max(0.35f, imp);
            float gap = c.At == "each" ? 0.22f : 0.06f;
            int n = 0;
            foreach (var e in group)
            {
                if (e.Kind != EventKind.Damage || IsShot(e)) { yield return ApplyConsequence(e, u); continue; }
                float at = first + gap * n;
                var tgt = e.Target.Index < Enemies.Count ? Enemies[e.Target.Index] : null;
                if (c.At == "each" && c.Fx != null && tgt != null)
                {
                    while (t < at - imp) { yield return null; t += Time.deltaTime; }
                    Bolzena.Fx.BolzenaFx.Play(c.Fx, new Bolzena.Fx.FxPlayOptions { At = tgt.Feet, Until = 0.9f, Order = 190, Parent = FieldRoot });
                }
                while (t < at) { yield return null; t += Time.deltaTime; }
                Impact(e, u, true, n == dmg.Count - 1, null, false);
                n++;
            }
            while (t < first + gap * n + 0.3f) { yield return null; t += Time.deltaTime; }
        }

        public static bool UltDropPart(string hero, string part) => hero != null && part != null
            && ULT_STANDING.TryGetValue(hero, out var st) && st.Drop != null && part.Contains(st.Drop);   // 스탠딩이 대신 쏘는 장(비비(신성) 레이저)

        static bool IsCueStrike(string id) => id != null && CUE2.TryGetValue(id, out var c) && c.Strike;   // 턴 시작 발동 — 뒤따르는 피해를 묶는 cue
        int CueCap(BattleEvent e)
        {
            if (CUE2.TryGetValue(e.Text, out var c2) && c2.Cap != 0) return c2.Cap < 0 ? Mathf.Max(1, e.Value) : c2.Cap;
            int n = 0; for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i)) n++;
            return Mathf.Max(1, n);
        }

        // ── 연출 신호(엔진 cue) ── 사도 · 적 데이터의 {"k":"cue","id":…} → CUE2 표(Cue2Fx)
        IEnumerator FxCueFx(BattleEvent e)
        {
            int i = Mathf.Clamp(e.Actor.Index, 0, Heroes.Count - 1);
            var h = Heroes[i];
            if (h == null) yield break;
            Debug.Log($"[FxCue] {e.Text} {h.name} v={e.Value}");
            Emit("cue_" + e.Text);
            // 2026-11-10(11주차) · 11-17(12주차) 지금 콘텐츠 · 엔진에서 내는 곳이 없는 사도별 분기를 걷었다 — 미로 · 이드(재활) · 에슈르 · 오로라 · 키샤 ·
            //   ran_again · kidian_again · 턴 시작 발동(CueStrike · MiroBeam) · 원작 방식 시전 조각(ULT_SPLIT) · 교주의 천벌. 근거 · 되살리는 법: 포트폴리오_과제/11주차/ADR-001 · 12주차/ADR-004.
            if (CUE2.TryGetValue(e.Text, out var c2)) yield return Cue2Fx(e, h, c2);
        }

        Vector3? actAim;   // 지금 몸짓의 첫 대상 몸 가운데(Cue2Fx 의 aim)

        // ── 변신 ── 들 때: 빛(원작 신탁 빛기둥) · 「꿈결 형상!」 띠 · 그 모습의 쉬는 동작(고학년 끝 자세에서 이어서), 풀릴 때: 본 Idle 로
        IEnumerator FormFx(BattleEvent e)
        {
            int i = Mathf.Clamp(e.Actor.Index, 0, Heroes.Count - 1);
            var h = Heroes[i];
            if (h == null) yield break;
            if (e.Text == "on")
            {
                string tail = e.Anim != null && e.Anim.StartsWith("Idle_") ? e.Anim.Substring(5) : e.Anim;
                h.SetForm(tail);
                if (h.Has("AS1_Ultimate1_2")) h.FormAttack = "AS1_Ultimate1_2";   // 림(혼돈) — 변신 그림은 없고 평타가 원작 강화 공격(고학년 뒤 「차원 너머의 낫」)
                h.Flash(new Color(1f, 0.92f, 0.7f), 0.4f, 0.6f);
                Bolzena.Fx.BolzenaFx.Common("oracle", h.Fx);
                Sfx.Play("buff", 0.6f, 0.9f);
                Vfx.Word(h.Top + new Vector3(0, 0.55f, 0), (e.Say ?? "변신") + "!", 0.5f, new Color(1f, 0.9f, 0.55f), new Color(0.3f, 0.12f, 0), 1.3f, 1.4f, null, 476, 0.35f);
                if (e.Value > 0) Vfx.Word(h.Top + new Vector3(0, 0.15f, 0), $"{e.Value}턴", 0.28f, new Color(1f, 0.95f, 0.85f), new Color(0.2f, 0.1f, 0), 1.0f, 1.2f);
                Emit("form_on");
            }
            else
            {
                h.SetForm(null);
                h.FormAttack = null;
                h.Flash(Color.white, 0.25f, 0.4f);
                Vfx.Word(h.Top + new Vector3(0, 0.4f, 0), (e.Say ?? "변신") + " 풀림", 0.32f, new Color(0.85f, 0.9f, 1f), new Color(0.05f, 0.08f, 0.2f), 1.0f, 1.2f);
                Emit("form_off");
                // 시간이 다 돼 풀릴 때 몸짓(core 변신 offExpire — 카드가 아니라 act 쪽지가 없다) — 뒤따르는 피해 숫자가 때리는 순간에 뜨게 그때까지 기다린다
                string hidX = i < Battle.Snapshot.Heroes.Count ? Battle.Snapshot.Heroes[i].Id : null;
                if (e.Anim == "time" && hidX != null && FORM_EXPIRE_MOTION.TryGetValue(hidX, out var xa) && h.SpineArt && h.Has(xa))
                {
                    float sp = Bolzena.Fx.BolzenaFx.OriginalOnly ? 1f : 1.3f;
                    float len = h.Play(xa, sp);
                    var st = h.Strikes(xa);
                    yield return Clock.Wait(st.Count > 0 ? st[0] / sp : len * 0.5f);
                }
            }
            Hud.SetSnapshot(Battle.Snapshot);
            yield return Clock.Wait(0.2f);
        }
    }
}
