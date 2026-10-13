using System.Collections;
using System.Collections.Generic;
using System.Text;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 모션 확인 화면(Demo.MotionLab, 2026-10-13) — 카드 · 엔진 없이 실제 전투와 같은 길로 몸짓을 튼다.
    //   엔진이 내주는 것과 같은 꼴의 결과 쪽지(Act + Damage · PartyHurt)를 지어 Present 에 넘긴다 — 그 뒤는 실제 전투 그대로
    //   (HeroAct: 몸짓 · 조각 잇기 · 줄이기 · 돌진 · 사도 이펙트 · 타격 · 소리 / EnemyAct · Hurt / 몸짓 밖 피해 Impact).
    //   피해 쪽지의 남은 체력 · 방어는 지금 값 그대로라 판(엔진)은 바뀌지 않고 아무도 쓰러지지 않는다.
    public partial class BattleDirector
    {
        public enum LabMove { Attack1, Attack2, Skill1, Ult, Extra, Hurt }

        /// <summary>마지막으로 튼 것의 점검 글(이름 · 길이 · 이펙트 · 타격 갈래).</summary>
        public string LabInfo = "";
        /// <summary>마지막 몸짓이 남은 꼬리까지 끝나는 게임 시각(Time.time) — 반복 틀기가 이때까지 기다린다.</summary>
        public float LabDoneAt;
        /// <summary>마지막 몸짓을 시작한 게임 시각(Time.time — 고학년은 컷인이 끝난 뒤). 캡처 모드가 몸짓 시작부터 찍는다.</summary>
        public float LabActAt = -1;

        /// <summary>사도(첫 자리)의 몸짓 하나를 targets 에게 hits 타로. 고학년은 컷인(cutin)까지 UltFlow 와 같은 차례.
        /// 추가 공격(Extra)은 쏘는 한 대 — shotLabel 종류(bullet · assist · resonance · extra · counter · summon)로 hits 발(적이 여럿이면 번갈아).</summary>
        public IEnumerator LabPlay(LabMove mv, List<int> targets, int hits, bool cutin, int dmg = 10, string shotLabel = "extra")
        {
            var s = Battle.Snapshot;
            if (s.Heroes.Count == 0 || Heroes.Count == 0 || Heroes[0] == null) yield break;
            var hs = s.Heroes[0];
            var kind = Look.Hero(hs.Id).Hit;
            hits = Mathf.Clamp(hits, 1, mv == LabMove.Extra ? 20 : 8);
            var tg = new List<int>();
            foreach (var t in targets) if (t >= 0 && t < Enemies.Count && t < s.Enemies.Count && Enemies[t] != null && !tg.Contains(t)) tg.Add(t);
            if (tg.Count == 0 && Enemies.Count > 0 && s.Enemies.Count > 0) tg.Add(0);
            if (tg.Count == 0) yield break;

            // 지난 값이 남아 엉뚱한 글을 보이지 않게 — 점검 칸을 비우고 시작
            CardAnimLog = ""; CardMoveLog = ""; CardShownMs = 0; CardNaturalMs = 0; CardHitsShown = 0; CardSpeed = 1.3f;
            UltAnimLog = ""; UltMotionMs = 0; UltNaturalMs = 0; UltShownBy = ""; UltMoveWhy = ""; UltEngineHits = 0; UltOrigHits = 0;
            LastFxCall = null; LastFxOrig = false; LastFxParts = 0;

            var evs = new List<BattleEvent>();
            if (mv == LabMove.Hurt)
            {
                // 적(첫 자리)이 사도를 친다 — 적 차례의 보통 공격과 같은 쪽지(Translate: Text "attack" · Motion Attack1)
                evs.Add(new BattleEvent { Kind = EventKind.Act, Actor = UnitRef.Enemy(tg[0]), Target = UnitRef.Party(0), Motion = Motion.Attack1, Text = "attack" });
                for (int k = 0; k < hits; k++)
                    evs.Add(new BattleEvent { Kind = EventKind.PartyHurt, Actor = UnitRef.Enemy(tg[0]), Target = UnitRef.Party(0), Value = dmg, Hit = k, Hits = k + 1, HpAfter = s.PartyHp, BlockAfter = s.PartyBlock });
            }
            else
            {
                var m = mv == LabMove.Attack1 ? Motion.Attack1 : mv == LabMove.Attack2 ? Motion.Attack2 : mv == LabMove.Skill1 ? Motion.Skill1 : Motion.Ultimate;
                bool ult = mv == LabMove.Ult;
                if (mv == LabMove.Extra)
                {
                    // 추가 공격 — 엔진의 쏘는 한 대(HurtOpts.Shot)를 옮긴 꼴 그대로(CoreBattle hurt): Shot(누가 · 누구에게 · 종류) 다음 Damage(Text "shot").
                    //   Present → Simple → ApplyConsequence → ShotFx · ShotHit(몰리면 줄 세움)
                    for (int k = 0; k < hits; k++)
                    {
                        int ts = tg[k % tg.Count];
                        var es = s.Enemies[ts];
                        evs.Add(new BattleEvent { Kind = EventKind.Shot, Actor = UnitRef.Party(0), Target = UnitRef.Enemy(ts), Text = shotLabel });
                        evs.Add(new BattleEvent { Kind = EventKind.Damage, Actor = UnitRef.Party(0), Target = UnitRef.Enemy(ts), Value = dmg, HpAfter = es.Hp, BlockAfter = es.Block, Hit = 0, Hits = 1, HitKind = kind, Text = "shot" });
                    }
                }
                else evs.Add(new BattleEvent { Kind = EventKind.Act, Actor = UnitRef.Party(0), Target = UnitRef.Enemy(tg[0]), Motion = m, HitKind = kind, Text = ult ? "ult" : null, Say = ult ? hs.UltName : null });
                if (mv != LabMove.Extra)
                for (int k = 0; k < hits; k++)
                    foreach (var t in tg)
                    {
                        var es = s.Enemies[t];
                        evs.Add(new BattleEvent { Kind = EventKind.Damage, Actor = UnitRef.Party(0), Target = UnitRef.Enemy(t), Value = dmg, Hit = k, Hits = k + 1, HpAfter = es.Hp, BlockAfter = es.Block, HitKind = kind });
                    }
            }

            float t0;
            if (mv == LabMove.Ult)
            {
                // UltFlow 와 같은 차례 — 엔진 UseUlt 대신 지은 쪽지
                InUlt = true;
                Clock.Base = 1f;
                Emit("ult_start");
                if (cutin && !Bolzena.RunUI.Settings.SkipCutin) yield return UltCutin.Play(ScreenRoot, hs.Key, hs.Name, hs.UltName, hs.Tint);
                else { Sfx.Voice(hs.Key, "ultimate", "shout"); Vfx.Flash(new Color(1, 1, 1, 0.25f), 0.15f); }
                t0 = Time.time;
                LabActAt = t0;
                yield return Present(evs);
                Clock.Base = Clock.TEMPO;
                InUlt = false;
            }
            else
            {
                t0 = Time.time;
                LabActAt = t0;
                yield return Present(evs);
            }
            float spent = Time.time - t0;
            // 카드 몸짓은 마지막 타격 뒤 곧 돌려주고 꼬리 · 복귀는 뒤에서 돈다(Finish) — 그 끝까지
            float tail = mv == LabMove.Ult ? 0 : mv == LabMove.Hurt ? 0.6f : mv == LabMove.Extra ? 0.4f : CardShownMs / 1000f + 0.3f;
            LabDoneAt = Mathf.Max(Time.time, t0 + tail);
            LabInfo = LabReport(mv, hs, tg.Count, hits, spent, shotLabel);
        }

        string LabReport(LabMove mv, HeroState hs, int nTargets, int hits, float spent, string shotLabel)
        {
            var sb = new StringBuilder();
            string hid = hs.Id;
            string who = nTargets > 1 ? $"적 {nTargets}명" : "적 1명";
            string tempo = $"게임 시계 · 보통 화면은 이것 / {Clock.TEMPO:0.0#}";
            List<Bolzena.Fx.UltPart> plan = null;
            switch (mv)
            {
                case LabMove.Hurt:
                    sb.Append($"<b>피격</b> — 적 「{(Battle.Snapshot.Enemies.Count > 0 ? Battle.Snapshot.Enemies[0].Name : "?")}」 보통 공격 {hits}타 → {hs.Name}\n");
                    sb.Append("사도 몸: 붉은 번쩍 · 밀려남(맞는 몸짓 없음) · 원작 공용 타격 낱장(적 갈래) · 목소리 40%\n");
                    break;
                case LabMove.Extra:
                {
                    var sl = ShotLook(hid);
                    sb.Append($"<b>추가 공격</b> 종류 {shotLabel} — {who} · {hits}발(Shot → Damage shot · 몰리면 0.1초씩, 4발 넘으면 0.05초까지 줄 세움)\n");
                    sb.Append($"쏘는 장: {sl.proj ?? "없음(몸 앞 빛)"} · 맞는 장: {sl.hit ?? "없음(공용 타격)"} · 몸 움찔: {(shotLabel == "bullet" ? "반동(뒤로)" : "앞으로 반 발")}\n");
                    break;
                }
                case LabMove.Ult:
                    sb.Append($"<b>고학년</b> 「{hs.UltName}」 — {who} · 엔진 {hits}타\n");
                    sb.Append($"몸짓: {(string.IsNullOrEmpty(UltAnimLog) ? "-" : UltAnimLog)}\n");
                    sb.Append($"길이: {UltMotionMs / 1000f:F2}초({tempo} — 고학년은 템포 1) · 원래 {UltNaturalMs / 1000f:F2}초 · {UltSpeed:0.##}배속{(UltShownBy != "" ? " · 타격 시각 " + UltShownBy : "")}\n");
                    sb.Append($"보이는 타수: 엔진 {UltEngineHits} · 원작 {UltOrigHits} · 이동: {(UltMoveWhy == "" ? "-" : UltMoveWhy)}\n");
                    plan = Bolzena.Fx.UltFx.Plan(hid);
                    break;
                default:
                {
                    var m = mv == LabMove.Attack1 ? Motion.Attack1 : mv == LabMove.Attack2 ? Motion.Attack2 : Motion.Skill1;
                    string label = mv == LabMove.Attack1 ? "평타" : mv == LabMove.Attack2 ? "강공" : "스킬(저학년)";
                    sb.Append($"<b>{label}</b> {m} — {who} · 엔진 {hits}타\n");
                    sb.Append($"몸짓: {(string.IsNullOrEmpty(CardAnimLog) ? Heroes[0].AnimFor(m) ?? "-" : CardAnimLog)} · 이동: {(CardMoveLog == "" ? "-" : CardMoveLog)}{(CardSelf(hid, m) ? " · 자기형" : "")}\n");
                    sb.Append($"길이: {CardShownMs / 1000f:F2}초({tempo}) · 원래 {CardNaturalMs / 1000f:F2}초 · {CardSpeed:0.##}배속 · 상한 {CardCap(m):0.0}초 · 보이는 타수 {CardHitsShown}\n");
                    string first = string.IsNullOrEmpty(CardAnimLog) ? Heroes[0].AnimFor(m) : CardAnimLog.Split(new[] { " → " }, System.StringSplitOptions.None)[0];
                    string fxAnim = CardFxAnim(hid, m, first);
                    plan = Bolzena.Fx.FxRules.CardPlan(hid, fxAnim, null);
                    sb.Append($"이펙트 갈래 고른 이름: {fxAnim} → [{string.Join(" · ", Bolzena.Fx.FxRules.CardKinds(fxAnim, null))}]\n");
                    break;
                }
            }
            var look = Look.Hero(hid);   // 사도별 표(Look STYLE — 결 · 돌진), 표에 없으면 서는 줄로 짐작
            sb.Append($"타격 갈래(사도 표): {look.Hit}({FxHitKind(look.Hit)}) · {(look.Melee ? "근접(돌진)" : "원거리(제자리)")} · 공용 타격 낱장 {(HitSheets ? "있음" : "없음(자체)")}\n");
            if (plan != null)
            {
                var names = new List<string>();
                foreach (var p in plan) names.Add($"{p.Name}[{p.At}{(p.Pre ? " pre" : "")}]");
                sb.Append($"이펙트 계획({plan.Count}장): {(names.Count > 0 ? string.Join(", ", names) : "없음 — 원작 이펙트 없는 몸짓")}\n");
                sb.Append($"사도 이펙트: {(LastFxOrig ? $"원작 {LastFxParts}장" : "없음(자체 연출)")}\n");
            }
            sb.Append($"걸린 시간: {spent:F2}초(게임 시계 — 입력을 다시 받기까지)");
            return sb.ToString();
        }

        /// <summary>지금까지 실제로 틀어진 사도 이펙트 장(이름 @ms) — 이펙트가 시간차로 터지니 화면이 매 프레임 읽는다.</summary>
        public string LabFired()
        {
            var c = LastFxCall;
            if (c == null || c.Fired.Count == 0) return "";
            var parts = new List<string>();
            foreach (var f in c.Fired) parts.Add($"{f.Name}@{f.Ms:0}{(f.Target ? "*" : "")}");
            return string.Join(", ", parts);
        }
    }
}
