using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 몸짓과 타격 — 내딛기 · 잔상 · 때리는 순간(스파인 이벤트) · 흰 번쩍임 · 히트스톱 · 흔들림/줌 펀치 · 파편 · 숫자 · 치명 · 격파 · 쓰러짐
    public partial class BattleDirector
    {
        bool firstHitShot, firstCritShot, firstBreakShot, firstHurtShot;
        int fxBoost;                     // 고학년 동안 이펙트를 어둠막(300) 위로

        // 맞는 차례 k 를 동작의 타격 시각 중 어디에 둘까
        static List<float> HitTimes(List<float> marks, int hits, float speed)
        {
            var res = new List<float>();
            int m = marks.Count;
            for (int k = 0; k < hits; k++)
            {
                float t;
                if (hits == 1) t = marks[0];
                else if (hits <= m) t = marks[Mathf.RoundToInt(k * (m - 1) / (float)(hits - 1))];
                else
                {
                    float span = m > 1 ? marks[m - 1] - marks[0] : 0.12f * (hits - 1);
                    t = marks[0] + span * k / (hits - 1);
                }
                res.Add(t / speed);
            }
            return res;
        }

        // 결과를 맞는 차례별로 — 첫 타격 전의 것(pre)과 차례마다의 묶음
        static void Split(List<BattleEvent> group, out List<BattleEvent> pre, out SortedDictionary<int, List<BattleEvent>> byHit, out int hits)
        {
            pre = new List<BattleEvent>();
            byHit = new SortedDictionary<int, List<BattleEvent>>();
            hits = 0;
            int cur = -1;
            foreach (var e in group)
            {
                if (e.Kind == EventKind.Damage || e.Kind == EventKind.PartyHurt)
                {
                    cur = e.Hit;
                    hits = Mathf.Max(hits, e.Hits);
                }
                if (cur < 0) { pre.Add(e); continue; }
                if (!byHit.TryGetValue(cur, out var l)) byHit[cur] = l = new List<BattleEvent>();
                l.Add(e);
            }
        }

        static readonly Dictionary<string, string> heroSfx = new Dictionary<string, string>();

        static string HeroSfx(string key, Motion m, bool hit)
        {
            string ck = key + "|" + m + "|" + hit;
            if (heroSfx.TryGetValue(ck, out var got)) return got;
            return heroSfx[ck] = HeroSfxFind(key, m, hit);
        }

        static string HeroSfxFind(string key, Motion m, bool hit)
        {
            string pre = m == Motion.Attack1 ? "basicattack" : m == Motion.Attack2 ? "powerattack" : m == Motion.Skill1 ? "spskill" : "ultimate";
            var all = Resources.LoadAll<AudioClip>("Sfx/hero/" + key);
            string best = null;
            foreach (var c in all)
            {
                var n = c.name.Substring(key.Length + 1);
                if (!n.StartsWith(pre)) continue;
                if (n.Contains("hit") != hit) continue;
                if (best == null || string.CompareOrdinal(c.name, best) < 0) best = c.name;
            }
            if (best == null && m == Motion.Ultimate) return HeroSfxFind(key, Motion.Skill1, hit);   // 고학년 효과음이 없는 사도(이프리트) — 스킬 소리로
            return best == null ? null : "hero/" + key + "/" + best;
        }

        // ── 사도의 몸짓 ──
        IEnumerator HeroAct(BattleEvent act, List<BattleEvent> group, bool ult = false)
        {
            var u = Heroes[act.Actor.Index];
            string key = u.name.Replace("hero_", "");
            Split(group, out var pre, out var byHit, out int hits);
            ult |= act.Text == "ult";

            // 대상들
            var targets = new List<int>();
            foreach (var e in group) if (e.Kind == EventKind.Damage && !targets.Contains(e.Target.Index)) targets.Add(e.Target.Index);
            if (targets.Count == 0 && act.Motion != Motion.None && act.Target.Side == Side.Enemy && act.Target.Index < Enemies.Count) targets.Add(act.Target.Index);

            if (act.Motion == Motion.None)
            {
                // 몸짓 없는 스킬 — 빛이 감돌고 곧바로
                u.Flash(new Color(1f, 0.95f, 0.75f), 0.35f);
                Vfx.Glow(u.Center, 2.4f, new Color(1f, 0.9f, 0.6f, 0.8f), 0.4f, 2.4f);
                Vfx.Burst(u.Center, new Vfx.BurstOpt { Tex = "FX_UI_star_02", Count = 10, Speed = new Vector2(1f, 3f), Angle = 90, Spread = 120, Life = new Vector2(0.4f, 0.7f), Size = new Vector2(0.08f, 0.18f), C0 = new Color(1f, 0.9f, 0.6f), Order = 150, Boost = 3f, ShrinkTo = 0 });
                foreach (var e in pre) yield return ApplyConsequence(e, u);
                foreach (var kv in byHit) foreach (var e in kv.Value) yield return ApplyConsequence(e, u);
                yield return Clock.Wait(0.25f);
                yield break;
            }

            string anim = u.AnimFor(act.Motion);
            float speed = ult ? (key == "ed" ? 1.35f : 1.05f) : act.Motion == Motion.Attack1 && key == "leets" ? 1.7f : 1.3f;
            var marks = u.Strikes(anim);
            // 고학년 — 원작 조각을 웹판처럼 잇는다(SpineMotion.PlanUlt: Ultimate1_1 → 1_2(_Loop) … · 갈래가 여럿이면 하나 · 달려가는 사도는 고리 횟수까지)
            //   때리는 순간도 그 계획(스파인 SFX · Event)으로. 전에는 Ultimate1_1 한 조각만 틀어 몸짓이 중간에 끊겼다
            UltPlan = null;
            if (ult && u.SpineArt && u.SkelData != null)
            {
                try { UltPlan = Bolzena.Fx.SpineMotion.PlanUlt(u.SkelData, Battle.Snapshot.Heroes[act.Actor.Index].Id); }
                catch (System.Exception ex) { Debug.LogWarning("[Ult] 계획 실패 " + key + " — " + ex.Message); }
                if (UltPlan != null && UltPlan.Anim != null)
                {
                    anim = UltPlan.Anim;
                    if (UltPlan.S != null)
                    {
                        marks = new List<float> { UltPlan.S.At / 1000f };
                        foreach (var m in UltPlan.S.Marks) if (m > UltPlan.S.At) marks.Add(m / 1000f);
                    }
                }
            }
            var times = HitTimes(marks, Mathf.Max(1, hits), speed);

            // 다른 유닛은 살짝 물러서게(어둡게)
            foreach (var h in Heroes) if (h != u) h.Tint(new Color(0.62f, 0.62f, 0.68f));
            for (int i = 0; i < Enemies.Count; i++) if (!targets.Contains(i)) Enemies[i].Tint(new Color(0.62f, 0.62f, 0.68f));

            // 내딛기 — 근접은 대상 앞까지 달려가 잔상을 남긴다, 마법은 반 걸음
            Vector3 dest = u.Home;
            // 고학년은 원작대로 — 근접(앞줄 또는 원작 평타가 투사체 없는 사도)이고 몸짓이 제 몸을 멀리 옮기지 않으면 적 앞까지 가서 한다(UltMover)
            bool dash = ult ? UltMover(u, key, Battle.Snapshot.Heroes[act.Actor.Index].Id, anim, targets) : Look.MeleeArt(key) && targets.Count > 0;
            if (targets.Count > 0)
            {
                var tp = targets.Count == 1 ? Enemies[targets[0]].Feet : Mid(targets);
                float reach = targets.Count == 1 ? Enemies[targets[0]].Width() * 0.35f + 1.25f : 2.4f;
                dest = dash ? new Vector3(tp.x - reach, tp.y - 0.02f, 0) : u.Home + new Vector3(0.5f, 0, 0);
            }
            var table = ult && UltPlan != null ? UltPlan.Table : null;   // 고학년 몸짓 표(ult_motion.json)
            if (table != null && !dash) dest = u.Home;                       // 표에서 제자리형이면 반 걸음도 안 나간다
            int baseOrder = 46;
            u.SetOrder(ult ? 330 : 60);
            if (ult)
            {
                fxBoost = 160;
                foreach (var ti in targets) Enemies[ti].SetOrder(320);
                ScreenFx.I.Dim(0.7f, 300, 5f);
                FieldRig.I.Zoom = 1f;
            }

            float dashTime = dash ? Mathf.Min(0.16f, times[0] * 0.8f + 0.06f) : 0.12f;
            float t0 = Clock.Now;
            float duration = ult && UltPlan != null && UltPlan.Anim != null ? u.PlayChain(UltPlan.Anim, UltPlan.Chain, speed) : u.Play(anim, speed);
            if (ult) { UltAnimLog = UltPlan != null && UltPlan.Anim != null ? UltPlan.Anim + (UltPlan.Chain.Count > 0 ? " → " + string.Join(" → ", UltPlan.Chain) : "") : anim; UltTargets = targets.Count; }
            bool orig = OrigFx(act, u, anim, marks, speed, ult, targets, dash ? dest : (Vector3?)null);
            var cast = HeroSfx(key, act.Motion, false);
            bool evSnd = ult && UltSounds(key, speed);                 // 고학년 소리 — 스파인 SFX 칸 시각대로(되면 시전 소리를 따로 안 낸다)
            if (cast != null && !evSnd) { var hs0 = Sfx.PlayHeld(cast, 0.7f); if (ult && hs0 != null) ultHeld.Add(hs0); }
            if (act.Motion == Motion.Attack2 || act.Motion == Motion.Skill1) Sfx.Voice(key, act.Motion == Motion.Skill1 ? "spskill" : "powerattack", "shout");
            bool tableMove = table != null && dash;
            if (tableMove) StartCoroutine(UltTravelCo(u, table, dest, speed));   // 원작 시각대로 가고(달리기 · 순간이동 · 뛰어들기) 돌아온다
            else
            {
                StartCoroutine(u.MoveTo(dest, dashTime, Ease.InCubic));
                if (dash) StartCoroutine(Ghosts(u, dashTime, new Color(0.75f, 0.9f, 1f, 0.22f)));
            }
            if (ult && !orig) StartCoroutine(UltCharge(u, times[0]));   // 원작 고학년 이펙트가 없을 때만(우이) 자체 모으기

            // 고학년 카메라 — 시전자에게 당겼다가 첫 타격에 적 쪽으로
            if (ult) StartCoroutine(CamTo(u.Center + new Vector3(1.2f, 0, 0), 1.18f, 0.35f));

            foreach (var e in pre) yield return ApplyConsequence(e, u);

            // 타격들
            float elapsed = 0;
            int k = 0;
            foreach (var kv in byHit)
            {
                float at = k < times.Count ? times[k] : times[times.Count - 1] + 0.1f * (k - times.Count + 1);
                while (elapsed < at) { yield return null; elapsed += Time.deltaTime; }
                if (ult && k == 0) StartCoroutine(CamTo(MidBody(targets) + new Vector3(-0.8f, 0.2f, 0), 1.08f, 0.25f));
                aoeAt = targets.Count > 1 ? MidBody(targets) : (Vector3?)null;
                int aoeHitTotal = 0;
                bool aoeFxDone = false;
                bool last = k == byHit.Count - 1;
                var hitSfx = HeroSfx(key, act.Motion, true);
                foreach (var e in kv.Value)
                {
                    if (e.Kind == EventKind.Damage) { Impact(e, u, ult, last, hitSfx, kv.Value.Count > 3, !aoeFxDone); if (aoeAt.HasValue) { aoeFxDone = true; aoeHitTotal += e.Value; } }
                    else yield return ApplyConsequence(e, u);
                }
                // 전체 공격 — 한 대의 합을 무리 가운데에 크게(적마다 숫자는 그 적 위에 그대로)
                if (aoeAt.HasValue && aoeHitTotal > 0 && (ult || last))
                    Vfx.Word(aoeAt.Value + new Vector3(0, 1.3f + k * 0.3f, 0), "합 " + aoeHitTotal.ToString("N0"), ult ? 0.5f : 0.36f, new Color(1f, 0.92f, 0.7f), new Color(0.3f, 0.1f, 0), 1.0f, 1f, null, 480, 0.5f);
                if (ult && last) UltFinish(targets.Count > 1 ? MidBody(targets) : Enemies[targets.Count > 0 ? targets[0] : 0].Center, Heroes[act.Actor.Index], orig);
                aoeAt = null;
                k++;
            }
            // 남은 몸짓 — 카드는 마지막 타격 뒤 조금 더 보고 돌아온다. 고학년은 몸짓을 끝까지(조각 · 고리 다) 하고 돌아온다
            //   (웹판: 달려간 고학년은 total + back 까지. 전에는 타격 + 0.6초에 Idle 로 끊어 끝 조각이 잘렸다)
            float tail = ult ? Mathf.Max(duration, table != null ? table.TotalMs / 1000f / speed : 0) : Mathf.Min(duration, elapsed + 0.32f);
            while (elapsed < tail) { yield return null; elapsed += Time.deltaTime; }
            if (ult)
            {
                UltMotionMs = Mathf.RoundToInt(duration * 1000);
                UltCutMs = Mathf.Max(0, Mathf.RoundToInt((duration - elapsed) * 1000));
                // 소리 꼬리 — 몸짓이 끝났는데 남은 고학년 소리는 0.3초에 줄여 끊는다(짧은 소리는 그대로 — 늘이지 않는다)
                int faded = 0;
                foreach (var a in ultHeld) if (a != null && a.isPlaying) { faded = Mathf.Max(faded, Mathf.RoundToInt((a.clip.length - a.time) * 1000)); StartCoroutine(Sfx.FadeOut(a, 0.3f)); }
                UltSndTailMs = faded;
                ultHeld.Clear();
            }
            if (tableMove) { while (travelling) { yield return null; } }   // 표대로 돌아오는 중이면 끝까지
            else if (u.Feet != u.Home)
            {
                if (dash) u.Loop("Move");
                yield return u.Return(dash ? 0.24f : 0.15f);
                if (dash) u.Idle();   // 반 걸음만 나갔으면 하던 몸짓(뒤에 Idle 이 이어져 있다)을 끊지 않는다
            }
            u.SetOrder(baseOrder - act.Actor.Index * 2);
            foreach (var h in Heroes) h.Tint(Color.white);
            foreach (var en in Enemies) if (en) en.Tint(Color.white);
            if (ult)
            {
                ScreenFx.I.Dim(0, 300, 3f);
                fxBoost = 0;
                yield return CamTo(Vector3.zero, 1f, 0.4f);
                for (int i = 0; i < Enemies.Count; i++) if (!Dead(i)) Enemies[i].SetOrder(Enemies[i].BaseOrder);   // 쓰러지는 중인 적은 그대로(마지막 일격 구름이 앞으로 나오지 않게)
            }
            ResetHeroOrder();
        }

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
                h.Flash(Color.white, 0.25f, 0.4f);
                Vfx.Word(h.Top + new Vector3(0, 0.4f, 0), (e.Say ?? "변신") + " 풀림", 0.32f, new Color(0.85f, 0.9f, 1f), new Color(0.05f, 0.08f, 0.2f), 1.0f, 1.2f);
                Emit("form_off");
            }
            Hud.SetSnapshot(Battle.Snapshot);
            yield return Clock.Wait(0.2f);
        }

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
                UltMoveWhy = "표 " + tw.MoveType + (tw.Moves ? $" 가기 {tw.GoMs} · 닿기 {tw.LandMs} · 돌아오기 {tw.ReturnMs}+{tw.BackMs}" : "");
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
                var (_, tlen) = HeroClips(key);
                foreach (var pl in UltPlan.Table.Plays)
                {
                    var path = "hero/" + key + "/" + pl.Value;
                    float at = pl.Key / 1000f / Mathf.Max(0.01f, speed);
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
                    float at = e.T / 1000f / Mathf.Max(0.01f, speed);   // 배속이면 몸짓이 빨라진 만큼 당긴다(높낮이는 그대로)
                    StartCoroutine(SndAt(path, at));
                    UltSndSpanMs = Mathf.Max(UltSndSpanMs, Mathf.RoundToInt((at + (len.TryGetValue(path, out var l) ? l : 0)) * 1000));
                }
            UltSndMode = "events";
            return true;
        }

        // 사도 소리 파일 목록 · 길이 — 싸움을 열 때 한 번(고학년 순간에 Resources.LoadAll 을 하지 않게)
        static readonly Dictionary<string, (List<string>, Dictionary<string, float>)> heroClips = new Dictionary<string, (List<string>, Dictionary<string, float>)>();
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
        IEnumerator UltTravelCo(UnitView u, Bolzena.Fx.UltWay w, Vector3 dest, float speed)
        {
            travelling = true;
            float k = 1f / 1000f / Mathf.Max(0.01f, speed);
            float t0 = Time.time;
            float El() => Time.time - t0;
            float go = w.GoMs * k, land = Mathf.Max(go, w.LandMs * k);
            float home = (w.HomeMs ?? w.ReturnMs) * k, back = w.BackMs * k;
            var homePos = u.Home;
            while (El() < go) yield return null;
            if (w.MoveType == "dash")
            {
                float d = Mathf.Max(0.06f, land - go);
                StartCoroutine(Ghosts(u, d, new Color(0.75f, 0.9f, 1f, 0.22f)));
                yield return u.MoveTo(dest, d, Ease.InCubic);
            }
            else
            {
                while (El() < (go + land) / 2) yield return null;
                yield return u.MoveTo(dest, 0.01f);
            }
            while (El() < home) yield return null;
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

        // ── 원작 이펙트(com.bolzena.fx) ── 몸짓 시작에 건다. 고학년은 사도의 원작 고학년 한 벌(시각은 스파인 계획 · 이 몸짓의 배속),
        // 카드는 그 동작(Attack1 · Attack2 · Skill1)의 원작 이펙트. 원작 것이 없으면 false — 자체 Vfx 가 대신한다
        public bool LastFxOrig;                          // 점검 — 마지막 몸짓에 원작 이펙트를 틀었나
        public int LastFxParts;
        bool OrigFx(BattleEvent act, UnitView u, string anim, List<float> marks, float speed, bool ult, List<int> targets, Vector3? dashTo)
        {
            LastFxOrig = false; LastFxParts = 0;
            var hs = Battle.Snapshot.Heroes[act.Actor.Index];
            string hid = hs.Id;
            var caster = u.Fx;
            if (dashTo.HasValue)   // 달려가 때리는 사도 — 이펙트는 닿는 자리에서(몸짓 시작 0.16초 안에 거기 간다)
            {
                var d = dashTo.Value; var b = caster;
                caster = new Bolzena.Fx.FxActor { FeetAt = () => d, Height = b.Height, Party = true, Key = b.Key, Point = b.Point, Muzzle = b.Muzzle };
            }
            var tfx = new List<Bolzena.Fx.FxActor>();
            foreach (var ti in targets) if (ti < Enemies.Count && Enemies[ti] != null) { var f = Enemies[ti].Fx; f.Alive = !Dead(ti); tfx.Add(f); }
            bool aoe = targets.Count > 1;
            Bolzena.Fx.FxCall c;
            if (ult)
            {
                if (!Bolzena.Fx.FxLibrary.HasUlt(hid)) return false;
                var sync = UltPlan != null && UltPlan.Anim != null ? Bolzena.Fx.SpineFx.Sync(UltPlan) : null;
                if (sync != null) sync.Speed = speed;
                var allies = new List<Bolzena.Fx.FxActor>();
                foreach (var h in Heroes) if (h != u && h) allies.Add(h.Fx);
                c = Bolzena.Fx.BolzenaFx.Ult(hid, caster, tfx, null, sync, aoe, allies);
            }
            else
            {
                if (tfx.Count == 0 && act.Target.Side == Side.Party && act.Target.Index < Heroes.Count) tfx.Add(Heroes[act.Target.Index].Fx);   // 아군에게 거는 카드
                var ms = new List<int>();
                foreach (var m in marks) ms.Add(Mathf.RoundToInt(m * 1000));
                var sync = new Bolzena.Fx.FxSync { Anim = anim, Impact = ms.Count > 0 ? ms[0] : (int?)null, Marks = ms, End = ms.Count > 0 ? ms[ms.Count - 1] : (int?)null, Speed = speed };
                c = Bolzena.Fx.BolzenaFx.Card(hid, caster, tfx, sync, null, aoe);
            }
            LastFxParts = c != null ? c.Parts : 0;
            LastFxOrig = LastFxParts > 0;
            return LastFxOrig;
        }

        // 맞음 이펙트 — 원작 공용 타격(구운 낱장, 웹판 sparkFx). 갈래: 둔기 · 마법 · 그 밖은 베기
        static string FxHitKind(HitKind k) => k == HitKind.Blunt ? "blunt" : k == HitKind.Magic ? "magic" : "slash";
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

        IEnumerator Ghosts(UnitView u, float dur, Color c)
        {
            float t = 0;
            while (t < dur + 0.04f)
            {
                u.Ghost(c, 0.22f);
                yield return Clock.Wait(0.045f);
                t += 0.045f;
            }
        }

        IEnumerator CamTo(Vector3 center, float zoom, float dur)
        {
            var rig = FieldRig.I;
            Vector2 c0 = rig.Center;
            float z0 = rig.Zoom;
            Vector2 c1 = new Vector2(center.x, center.y) * (zoom > 1.001f ? 0.55f : 0f);
            yield return Clock.Tween(dur, t =>
            {
                float k = Ease.InOutCubic(t);
                rig.Center = Vector2.Lerp(c0, c1, k);
                rig.Zoom = Mathf.Lerp(z0, zoom, k);
            });
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
            PostFx.Kick(chroma: 1f, lens: -0.4f, bloom: 0.2f);
            Vfx.Flash(new Color(tint.r, tint.g, tint.b, 0.08f), 0.2f, 1f, 305);
            ScreenFx.I.Lines(0.75f, Color.white, FieldRoot.TransformPoint(at), 302, 0.26f);
            Clock.Run(LinesOff(0.5f));
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

        IEnumerator LinesOff(float after)
        {
            yield return Clock.WaitU(after);
            ScreenFx.I.Lines(0, Color.white, Vector2.zero);
        }

        // ── 맞는 순간 ──
        // fxHere — 전체 공격이면 첫 대상만 무리 가운데에 큰 이펙트를 내고, 나머지는 몸 반응 · 숫자만
        void Impact(BattleEvent e, UnitView by, bool ult, bool last, string hitSfx, bool crowd, bool fxHere = true)
        {
            int ti = e.Target.Index;
            if (ti >= Enemies.Count) return;
            var t = Enemies[ti];
            var hud = EnemyHuds[ti];
            var kind = e.HitKind;
            var at = t.Center + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.2f, 0.25f), 0);
            bool crit = e.Crit;
            bool multi = e.Hits > 1;
            float flip = Random.value < 0.5f ? 1 : -1;

            // 멈칫 · 흔들림 · 줌 펀치
            float stop = crit ? 0.14f : ult ? 0.075f : multi ? 0.05f : 0.085f;
            if (!crowd || e.Target.Index == 0) Clock.HitStop(stop);
            FieldRig.Shake(crit ? 0.5f : ult ? 0.38f : multi ? 0.2f : 0.3f, crit ? 2.5f : 0.6f);
            FieldRig.Punch(aoeAt ?? at, crit ? 0.075f : ult ? 0.04f : 0.035f);

            // 몸 — 흰 번쩍임(반쯤만 — 온통 하얗게 채우면 실루엣이 날아간다) · 밀려남 · 붉게
            // 고학년은 여러 대가 겹치니 한 대의 빛을 더 줄인다(fade)
            float fade = ult ? 0.6f : 1f;
            bool bigFoe = ti < Battle.Snapshot.Enemies.Count && Battle.Snapshot.Enemies[ti].Boss;   // 큰 몸은 흰 채우기가 더 넓게 번진다
            t.Flash(new Color(1f, 0.97f, 0.9f), crit ? 0.18f : 0.11f, (crit ? 0.42f : 0.32f) * (ult ? 0.7f : 1f) * (bigFoe ? 0.6f : 1f));
            t.Knock(1f, crit ? 0.5f : multi ? 0.18f : 0.3f, crit ? 0.36f : 0.26f);
            if (!e.Crit) StartCoroutine(RedTint(t));

            // 전체 공격 — 이펙트는 무리 가운데에 한 번(조금 크게), 대상마다는 몸 반응 · 숫자만
            var fxAt = aoeAt ?? at;
            if ((fxHere || !aoeAt.HasValue) && HitSheets)
            {
                // 원작 공용 타격(웹판 sparkFx) — 자리는 원작 본 Point_Middle(전체 공격이면 무리 가운데)
                var tf = t.Fx;
                if (aoeAt.HasValue) { var p0 = fxAt; tf = new Bolzena.Fx.FxActor { FeetAt = () => t.Feet, Height = t.Height(), Party = false, Point = n => n == "Middle" ? p0 : (Vector3?)null }; }
                Bolzena.Fx.BolzenaFx.HitOrder = 182 + fxBoost;
                Bolzena.Fx.BolzenaFx.Hit(tf, FxHitKind(kind), !ult && !multi && last && by != null, crit, ult, e.Hit == 0);
                if (e.Blocked > 0) Bolzena.Fx.BolzenaFx.Common("shieldHit", t.Fx);
                if (crit)
                {
                    Vfx.Flash(new Color(1f, 0.9f, 0.7f, 0.06f), 0.12f);
                    PostFx.Kick(chroma: 0.6f, lens: -0.15f, bloom: 0.12f);
                    ScreenFx.I.Lines(0.45f, new Color(1f, 0.97f, 0.9f), FieldRoot.TransformPoint(fxAt), 302, 0.3f);
                    Clock.Run(LinesOff(0.28f));
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
                    Tex = "FX_IN_Spark", Count = crit ? 22 : multi ? 8 : 12, Speed = new Vector2(7f, crit ? 20f : 15f), Life = new Vector2(0.12f, 0.3f),
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
                else if (ult) PostFx.Kick(chroma: 0.35f, bloom: 0.08f);
            }

            // 소리
            Sfx.Play(crit ? "hit_crit" : kind == HitKind.Blunt ? "hit_blunt" : kind == HitKind.Magic ? "hit_magic" : "hit_slash", crit ? 0.95f : 0.8f, 1f, true);
            if (hitSfx != null) Sfx.Play(hitSfx, 0.6f, 1f, true);
            if (e.Blocked > 0) Sfx.Play("block_hit", 0.6f);

            // 숫자 · 막대
            // 숫자는 몸 위쪽에 — 가운데에 띄우면 큰 적(보스)의 몸을 가린다
            var numAt = Vector3.Lerp(t.Center, t.Top, 0.55f);
            Vfx.Number(new Vector3(at.x + 0.2f, Mathf.Max(at.y + 0.55f, numAt.y), 0), e.Value, crit, e.Hit, null, false, e.Blocked);
            hud.SetHp(e.HpAfter);
            hud.SetBlock(e.BlockAfter);

            if (!firstHitShot && !crit && !ult) { firstHitShot = true; Emit("hit"); }
            if (!firstCritShot && crit) { firstCritShot = true; Emit("crit"); }
            if (ult) Emit(last ? "ult_last" : "ult_hit");
        }

        IEnumerator RedTint(UnitView u)
        {
            yield return Clock.Wait(0.06f);
            u.Tint(new Color(1f, 0.55f, 0.55f), 40f);
            yield return Clock.Wait(0.12f);
            u.Tint(Color.white, 8f);
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
                case EventKind.Damage:
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
                    yield return BreakFx(e.Target.Index);
                    break;
                case EventKind.Death:
                    StartCoroutine(DeathFx(e.Target.Index, e.Boss));
                    if (e.Boss) yield return Clock.WaitU(1.4f);
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
                    yield return Deal(new List<CardInfo> { e.Card });
                    break;
                case EventKind.Intent:
                case EventKind.Recover:
                    yield return Simple(e);
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
                Vfx.Word(Enemies[i].Top + new Vector3(0, 0.25f, 0), id, 0.4f, col, new Color(0.15f, 0, 0.1f));
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
            Emit(Battle.Snapshot.Enemies[i].Boss ? "boss_break" : "break");
            yield return Clock.Wait(0.35f);
        }

        IEnumerator DeathFx(int i, bool boss)
        {
            var t = Enemies[i];
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
            if (!t) yield break;   // 그사이 다음 웨이브가 들어와 지워졌다
            // 흰 실루엣으로 녹아 위로 흩어진다 — 원작 쓰러짐 연기, 없으면 자체 빛망울
            bool smoke = Bolzena.Fx.BolzenaFx.Common("kill", t.Fx) != null;
            for (int k = 0; k < (smoke ? 0 : 6); k++)
                Vfx.Burst(t.Feet + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, t.Height() * 0.8f), 0), new Vfx.BurstOpt
                {
                    Tex = "FX_IN_Glow", Count = 4, Speed = new Vector2(0.5f, 2f), Angle = 90, Spread = 50, Life = new Vector2(0.5f, 1f), Size = new Vector2(0.1f, 0.25f),
                    C0 = new Color(1f, 0.95f, 0.85f), Order = 70, Boost = 2.5f, ShrinkTo = 0,
                });
            yield return Clock.Tween(0.5f, k => { if (t) { t.SetAlpha(1 - k); t.Flash(Color.white, 0.05f); } });
            if (t) t.gameObject.SetActive(false);
        }

        IEnumerator BossKillLift(UnitView t)
        {
            yield return Clock.WaitU(1.1f);
            ScreenFx.I.Dim(0, 300, 2.5f);
            ScreenFx.I.Lines(0, Color.white, Vector2.zero);
        }

        // ── 적의 몸짓 ──
        IEnumerator EnemyAct(BattleEvent act, List<BattleEvent> group)
        {
            int ei = act.Actor.Index;
            if (ei >= Enemies.Count) SpawnSummons();
            if (ei < 0 || ei >= Enemies.Count) yield break;
            var u = Enemies[ei];
            Split(group, out var pre, out var byHit, out int hits);
            bool heavy = act.Text == "heavy";
            string anim = u.AnimFor(act.Motion);
            float speed = heavy ? 1.1f : 1.25f;
            var marks = u.Strikes(anim);
            var times = HitTimes(marks, Mathf.Max(1, hits), speed);
            string key = u.name.Replace("enemy_", "");

            u.SetOrder(60);
            foreach (var en in Enemies) if (en != u) en.Tint(new Color(0.62f, 0.62f, 0.68f));
            if (act.Up)
            {
                Vfx.Word(u.Top + new Vector3(0, 1.0f, 0), "즉시 행동!", 0.42f, new Color(1f, 0.85f, 0.3f), new Color(0.3f, 0.1f, 0), 1.0f, 1.2f);
                Sfx.Play("turn_start", 0.5f, 1.3f);
                yield return Clock.Wait(0.35f);
            }
            if (!string.IsNullOrEmpty(act.Say))
                Vfx.Word(new Vector3(u.Top.x, Mathf.Min(u.Top.y + 1.2f, 3.45f), 0), "「" + act.Say + "」", 0.24f, new Color(1f, 0.95f, 0.88f), new Color(0.1f, 0.05f, 0.1f), 1.3f, 1f, null, 472, 0.2f);
            if (heavy)
            {
                // 큰 수 — 붉은 경고, 집중선
                Vfx.Word(u.Top + new Vector3(0, 0.6f, 0), "강타!", 0.7f, new Color(1f, 0.5f, 0.35f), new Color(0.3f, 0, 0), 1.0f, 1.5f);
                ScreenFx.I.Lines(0.55f, new Color(1f, 0.4f, 0.3f), FieldRoot.TransformPoint(u.Center), 302, 0.3f);
                yield return Clock.Wait(0.3f);
            }
            float dur = u.Play(anim, speed);
            bool attack = hits > 0;
            Sfx.Play("monster/" + key + "/" + key + (attack ? (heavy ? "_skillcast" : "_basicattack") : "_skillcast"), 0.7f);
            bool lunge = attack && key == "fairymobcloserange";
            if (lunge) StartCoroutine(u.MoveTo(u.Home + new Vector3(-2.4f, 0, 0), 0.18f, Ease.InCubic));
            else if (attack) StartCoroutine(u.MoveTo(u.Home + new Vector3(-0.4f, 0, 0), 0.15f));

            foreach (var e in pre) yield return ApplyConsequence(e, u);
            float elapsed = 0;
            int k = 0;
            foreach (var kv in byHit)
            {
                float at = k < times.Count ? times[k] : times[times.Count - 1] + 0.12f;
                while (elapsed < at) { yield return null; elapsed += Time.deltaTime; }
                foreach (var e in kv.Value)
                {
                    if (e.Kind == EventKind.PartyHurt) Hurt(e, key, heavy);
                    else yield return ApplyConsequence(e, u);
                }
                k++;
            }
            if (heavy) ScreenFx.I.Lines(0, Color.white, Vector2.zero);
            float tail = Mathf.Min(dur, elapsed + 0.4f);
            while (elapsed < tail) { yield return null; elapsed += Time.deltaTime; }
            if (u.Feet != u.Home)
            {
                yield return u.Return(0.2f);
            }
            u.SetOrder(u.BaseOrder);
            foreach (var en in Enemies) if (en) en.Tint(Color.white);
            yield return Clock.Wait(0.15f);
        }

        void Hurt(BattleEvent e, string enemyKey, bool heavy)
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
                if (HitSheets) { Bolzena.Fx.BolzenaFx.HitOrder = 182; Bolzena.Fx.BolzenaFx.Hit(h.Fx, Bolzena.Fx.MotionTables.HitKindEnemy(enemyKey), heavy); }   // 원작 공용 타격(사도가 맞으면 뒤집힌다)
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

        // ── 고학년 ──
        public Bolzena.Fx.ActPlan UltPlan;               // 마지막 고학년 몸짓 계획(점검 로그)
        public string UltAnimLog;                        // 마지막 고학년에 튼 조각들
        public int UltTargets;                           // 마지막 고학년이 맞힌 적 수
        IEnumerator UltFlow(int hero, int target)
        {
            var s = Battle.Snapshot;
            var hs = s.Heroes[hero];
            InUlt = true;
            Emit("ult_start");
            yield return UltCutin.Play(ScreenRoot, hs.Key, hs.Name, hs.UltName, hs.Tint);
            var evs = Battle.UseUlt(hero, target);
            // Act 를 고학년으로 연출하고, 나머지는 보통으로
            yield return Present(evs);
            InUlt = false;
        }

        public void RequestUlt(int hero) => request = (ReqKind.Ult, hero, UltAim >= 0 ? UltAim : FirstAliveEnemy());
        public void DemoSelectUlt(int hero) => TapUlt(hero);
        public bool InUlt { get; private set; }            // 고학년 연출 중(프레임 재기)
        public void RequestEnd() => request = (ReqKind.End, 0, 0);
    }
}
