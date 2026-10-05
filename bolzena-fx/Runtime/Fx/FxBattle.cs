using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.Fx
{
    // 몸짓 계획에서 이펙트가 알아야 할 몫 — 스파인 쪽 ActPlan 에서 뽑는다(Bolzena.Fx.SpineFx.Sync(plan)). 없으면 표의 기본 시각
    public class FxSync
    {
        public string Anim;                  // 첫 조각(Attack1_1 · Skill1_1 · Ultimate1_1) — 카드 이펙트 갈래 · 시전 본
        public string Tier;                  // CardMotion.Pick 의 Tier(sig · heavy · light …)
        public string Group;                 // 사도 소리 갈래(attack · power · skill · ult)
        public int? Impact, End;             // 때리는 순간 · 여러 번 때리는 창 끝(ms, 동작 시작에서)
        public IList<int> Marks;             // 그 창의 이벤트 시각
        public int Total;                    // 동작 전체 길이(ms)
        public UltPick? Pick;                // 고학년 갈래
        public bool Dash;                    // 달려가 부딪친다(에르핀 · 에르핀_왕도)
        public Vector3? DashFrom, DashTo;    // 달리기 처음 자리 · 부딪치는 자리
        public IList<SoundEvent> Snd;        // 스파인 SFX 칸
        public float Speed = 1f;             // 동작을 이 배속으로 튼다(전투가 고학년 1.05 · 카드 1.3) — 시각을 이만큼 당긴다

        internal FxSync Scaled()
        {
            if (Mathf.Approximately(Speed, 1f) || Speed <= 0) return this;
            int S(int v) => Mathf.RoundToInt(v / Speed);
            var c = (FxSync)MemberwiseClone();
            c.Speed = 1f;
            c.Impact = Impact.HasValue ? S(Impact.Value) : (int?)null;
            c.End = End.HasValue ? S(End.Value) : (int?)null;
            c.Marks = Marks?.Select(S).ToList();
            c.Total = S(Total);
            c.Snd = Snd?.Select(e => new SoundEvent { N = e.N, T = S(e.T) }).ToList();
            return c;
        }
    }

    // 한 번 부른 연출 — 맞는 시각들과 멈추기
    public class FxCall
    {
        public List<int> HitTimes = new List<int>();   // 동작 시작에서 ms — onHit(i) 가 이 시각에 불린다
        public int HitAt => HitTimes.Count > 0 ? HitTimes[0] : 0;
        public int Parts;                               // 계획한 이펙트 장 수(이펙트가 없으면 0 — 맞는 시각만 준다)
        public int Played => Runs.Count;                 // 실제로 튼 장 수(다시 틀기 포함)
        public bool Done { get; internal set; }
        internal Coroutine Co, HitCo;
        internal bool FxLeft, HitsLeft;   // 아직 도는 몫 — 둘 다 끝나면 Done
        internal void Check() { if (!FxLeft && !HitsLeft) Done = true; }
        internal readonly List<KeyValuePair<FxRun, int>> Runs = new List<KeyValuePair<FxRun, int>>();
        public void Stop(float fade = 0.25f)
        {
            if (FxDriver.I != null) { if (Co != null) FxDriver.I.StopCoroutine(Co); if (HitCo != null) FxDriver.I.StopCoroutine(HitCo); }
            foreach (var r in Runs) if (r.Key != null && r.Key.Gen == r.Value && !r.Key.IsDone) r.Key.Stop(fade);
            Done = true;
        }
    }

    // 코루틴을 돌릴 숨은 몸(장면이 바뀌어도 남는다)
    public class FxDriver : MonoBehaviour
    {
        static FxDriver inst;
        public static FxDriver I
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("BolzenaFxDriver") { hideFlags = HideFlags.HideAndDontSave };
                    if (Application.isPlaying) DontDestroyOnLoad(go);
                    inst = go.AddComponent<FxDriver>();
                }
                return inst;
            }
        }
    }

    // ── 전투가 부르는 것 ── (BattleDirector · UnitView 가 몇 줄로)
    //   BolzenaFx.Prewarm(party)                                    싸움을 열 때
    //   var c = BolzenaFx.Ult(key, caster, targets, i => Land(i), SpineFx.Sync(plan), aoe)   고학년 — onHit(i) 에 숫자 · 체력 · 맞음
    //   var c = BolzenaFx.Card(key, caster, targets, SpineFx.Sync(plan), i => Land(i), aoe)  카드 몸짓
    //   BolzenaFx.Hit(target, "slash", heavy, crit, ult, first)     맞는 순간(onHit 안에서)
    //   BolzenaFx.Common("heal" | "shield" | "shieldHit" | "buff" | "debuff" | "break" | "kill" | "oracle" | "revive", unit)
    public static partial class BolzenaFx
    {
        // 소리도 같이 낸다(BolzenaAudio — 겹침 · 꼬리 · 믹서 규칙은 그쪽). 전투가 소리를 따로 내면 끈다
        public static bool Sound = true;
        // 전투 쪽 자리 — 이펙트를 이 마디 아래에 둔다(싸움터가 흔들리고 줌하면 같이). FxActor 의 자리도 이 마디 좌표로 준다
        public static Transform Parent;
        // 그리는 차례(sortingOrder) — 고학년 · 카드 · 맞음. 공용은 FxRules.COMMON 의 Order + CommonOrderAdd
        public static int UltOrder = 300, CardOrder = 305, HitOrder = 320, CommonOrderAdd = 0;

        // ── 미리 데우기 ── 파티 사도의 고학년 · 카드 이펙트 + 공용(맞음 · 회복 …) 텍스처 · 셰이더 · 코드. 돌려주는 것: 올린 텍스처 수
        public static int Prewarm(IEnumerable<string> party, bool sounds = true)
        {
            var names = new List<string>();
            var keys = party?.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList() ?? new List<string>();
            foreach (var k in keys)
                foreach (var kind in new[] { "ult", "attack", "power", "skill", "sig" }) names.AddRange(FxLibrary.Get(k, kind));
            foreach (var l in MotionTables.HIT_FX.Values) names.AddRange(l);
            foreach (var kind in FxRules.COMMON.Keys) names.AddRange(FxRules.CommonNames(kind));
            int n = Prewarm(names.Distinct());
            foreach (var k in keys) { UltFx.Plan(k); FxRules.CardPlan(k, "Skill1_1", null); if (sounds) BolzenaAudio.PreloadHero(k); }
            // 코드(JIT) · 풀 — 화면 밖에서 몇 장 틀고 바로 걷는다
            int warmed = 0;
            foreach (var x in names.Distinct())
            {
                var r = Play(x, new FxPlayOptions { At = new Vector3(1e5f, 1e5f, 0), Until = 0.02f });
                if (r != null && ++warmed >= 4) break;
            }
            return n;
        }

        // 맞는 시각들 — 웹판과 같다: 때리는 순간 + 투사체 몫 + 60, 창 안 이벤트마다 한 번 더
        static List<int> HitTimes(FxSync s, int lag, int fallback)
        {
            var res = new List<int>();
            if (s == null || !s.Impact.HasValue) { res.Add(fallback + FxRules.HIT_AFTER); return res; }
            res.Add(s.Impact.Value + lag + FxRules.HIT_AFTER);
            if (s.Marks != null && s.Marks.Count > 1)
                foreach (var m in s.Marks.Skip(1)) if (m > s.Impact.Value && (!s.End.HasValue || m <= s.End.Value)) res.Add(m + lag + FxRules.HIT_AFTER);
            return res;
        }

        // 대상 지점 — 단일: 첫 대상 발밑 · 전체(aoe 또는 대상 없음): 살아 있는 대상 발밑의 가운데, 폭 = 무리 폭 + 1.4
        static void Aim(FxActor caster, IList<FxActor> targets, bool aoe, out Vector3 to, out Vector3? body, out float width)
        {
            var live = targets?.Where(t => t != null && t.Alive).ToList() ?? new List<FxActor>();
            width = 0; body = null;
            if (live.Count == 0) { to = caster.Feet; return; }
            if (!aoe || live.Count == 1)
            {
                to = live[0].Feet; body = live[0].At(FxAnchor.Body);
                return;
            }
            var feet = live.Select(t => t.Feet).ToList();
            to = new Vector3(feet.Average(p => p.x), feet.Average(p => p.y), feet[0].z);
            body = new Vector3(to.x, live.Average(t => t.At(FxAnchor.Body).y), to.z);
            width = feet.Max(p => p.x) - feet.Min(p => p.x) + 1.4f;
        }

        static FxCall Launch(List<UltPart> plan, UltOptions o, List<int> hits, Action<int> onHit)
        {
            var call = new FxCall { HitTimes = hits, Parts = plan.Count };
            var d = FxDriver.I;
            o.OnPart = r => call.Runs.Add(new KeyValuePair<FxRun, int>(r, r.Gen));
            call.FxLeft = plan.Count > 0 && !Calm;
            call.HitsLeft = onHit != null;
            if (call.FxLeft) call.Co = d.StartCoroutine(Wrap(UltFx.RunPlan(plan, o), call));
            if (call.HitsLeft) call.HitCo = d.StartCoroutine(HitClock(hits, onHit, call));
            call.Check();
            return call;
        }

        static IEnumerator Wrap(IEnumerator run, FxCall call)
        {
            while (run.MoveNext()) yield return run.Current;
            call.FxLeft = false; call.Check();
        }

        // 전투 시계로 흘려 onHit(i) — 이펙트 재생과 같은 시계(DeltaTime · Rate)
        static IEnumerator HitClock(List<int> hits, Action<int> onHit, FxCall call)
        {
            float now = 0;
            int i = 0;
            while (i < hits.Count)
            {
                while (i < hits.Count && hits[i] <= now) { try { onHit(i); } catch (Exception ex) { Debug.LogException(ex); } i++; }
                if (i >= hits.Count) break;
                yield return null;
                now += 1000f * Mathf.Min(1f / 20f, Mathf.Max(0, DeltaTime())) * Rate;
            }
            call.HitsLeft = false; call.Check();
        }

        // ── 고학년 ── 원작 이펙트 한 벌 + 맞는 시각. sync 를 주면 스파인 이벤트에 맞춘다(없으면 UltFx.ImpactMs)
        // allies — 아군 쪽 판(벨라 hit_a · 회복 · 강화 · 부활)을 시전자와 함께 이 아군들에게도
        public static FxCall Ult(string heroKey, FxActor caster, IList<FxActor> targets, Action<int> onHit = null, FxSync sync = null, bool aoe = false, IList<FxActor> allies = null)
        {
            if (caster == null) return null;
            sync = sync?.Scaled();
            FxLibrary.PreloadUlt(heroKey);
            var plan = UltFx.Plan(heroKey, sync?.Pick);
            int lag = plan.Any(p => p.At == "move") ? UltFx.PROJ_LAG : 0;
            Aim(caster, targets, aoe, out var to, out _, out var width);
            var o = new UltOptions
            {
                From = sync != null && sync.Dash && sync.DashFrom.HasValue ? sync.DashFrom.Value : caster.Feet,
                To = sync != null && sync.Dash && sync.DashTo.HasValue ? sync.DashTo.Value : to,
                ToWidth = aoe ? width : 0, Top = TopY(),
                Impact = sync?.Impact, End = sync?.End, Marks = sync?.Marks?.Select(x => (float)x).ToList(),
                Pick = sync?.Pick, Dash = sync != null && sync.Dash, Muzzle = caster.Muzzle,
                Order = UltOrder, Parent = Parent,
                Allies = allies?.Where(a => a != null && a != caster && a.Alive).Select(a => (Func<Vector3>)(() => a.At(FxAnchor.Feet))).ToList(),
            };
            int fallback = sync?.Impact ?? (plan.Count > 0 ? UltFx.ImpactMs(plan) : MotionTables.ULT_HIT);
            var hits = HitTimes(sync, lag, fallback);
            if (Sound)
            {
                BolzenaAudio.SpeakFor(heroKey, sync?.Anim ?? "Ultimate1_1");
                BolzenaAudio.Action(heroKey, "ult", sync?.Snd, null, new[] { "ult2", "ultBoom" }, hits[0] - FxRules.HIT_AFTER);
            }
            return Launch(plan, o, hits, onHit);
        }

        // ── 카드 몸짓 ── 그 사도의 원작 평타 · 센 공격 · 스킬 · 시그니처 이펙트(동작 이름으로 갈래). 원작 이펙트가 없는 동작이면
        // 이펙트 없이 맞는 시각만(onHit 은 그대로 불린다). 강화 · 방어처럼 대상이 아군이면 targets 에 아군을 넣는다
        public static FxCall Card(string heroKey, FxActor caster, IList<FxActor> targets, FxSync sync, Action<int> onHit = null, bool aoe = false)
        {
            if (caster == null) return null;
            sync = sync?.Scaled();
            var plan = FxRules.CardPlan(heroKey, sync?.Anim, sync?.Tier);
            int lag = plan.Any(p => p.At == "move") ? UltFx.PROJ_LAG : 0;
            Aim(caster, targets, aoe, out var to, out var body, out var width);
            var castPoint = FxRules.CastPoint(sync?.Anim);
            var o = new UltOptions
            {
                From = caster.Feet, To = to, ToBody = body, ToWidth = aoe ? width : 0, Top = TopY(),
                ShotFrom = caster.At(FxAnchor.Cast, castPoint),
                Impact = sync?.Impact ?? FxRules.CARD_HIT, End = sync?.End, Marks = sync?.Marks?.Select(x => (float)x).ToList(),
                Muzzle = caster.Muzzle, Order = CardOrder, Parent = Parent,
            };
            var hits = HitTimes(sync, lag, FxRules.CARD_HIT);
            if (Sound && sync != null && sync.Group != null) BolzenaAudio.Action(heroKey, sync.Group, sync.Snd, sync.Group);
            return Launch(plan, o, hits, onHit);
        }

        // ── 맞음 ── 웹판 sparkFx 그대로: 공용 타격 낱장(갈래) + 치명 덧불 · 세게 맞음(또는 고학년 첫 타격) 덧불, 몸 가운데를 조금 흩어.
        // kind: MotionTables.HitKindHero(dmgType, backRow) · HitKindEnemy(enemyKey). 사도가 맞으면 좌우를 뒤집는다
        public static void Hit(FxActor target, string kind, bool heavy = false, bool crit = false, bool ult = false, bool first = false, BolzenaAudio.HitInfo? sound = null, string byHero = null, string byEnemy = null, string group = null)
        {
            if (target == null) return;
            if (!Calm)
            {
                kind = kind != null && MotionTables.HIT_FX.ContainsKey(kind) ? kind : "slash";
                var p = target.At(FxAnchor.Body);
                p.x += UnityEngine.Random.Range(-1f, 1f) * target.Height * 0.6f * FxRules.HIT_JITTER_X;
                p.y += UnityEngine.Random.Range(-1f, 1f) * target.Height * FxRules.HIT_JITTER_Y;
                float sc = (ult ? FxRules.HIT_SCALE_ULT : FxRules.HIT_SCALE) * (crit ? FxRules.HIT_CRIT : heavy ? FxRules.HIT_HEAVY : 1);
                bool flip = target.Party;
                foreach (var n in MotionTables.HIT_FX[kind]) Play(n, new FxPlayOptions { At = p, Scale = sc, Flip = flip, Order = HitOrder, Parent = Parent });
                if (crit) foreach (var n in MotionTables.HIT_FX["crit"]) Play(n, new FxPlayOptions { At = p, Scale = FxRules.CRIT_SCALE, Flip = flip, Order = HitOrder + 1, Parent = Parent });
                else if (heavy || (ult && first))
                    foreach (var n in MotionTables.HIT_FX[kind == "blunt" ? "bigBlunt" : "big"])
                        Play(n, new FxPlayOptions { At = p + new Vector3(0, FxRules.BIG_DY, 0), Scale = ult ? FxRules.BIG_SCALE_ULT : FxRules.BIG_SCALE, Flip = flip, Order = HitOrder + 1, Parent = Parent });
            }
            if (Sound && sound.HasValue) BolzenaAudio.Land(sound.Value, byHero, byEnemy, ult, heavy, group, kind == "magic");
        }

        // ── 공용 ── 회복 · 실드 · 실드 맞음 · 강화 · 약화 · 격파 · 쓰러짐 · 신탁 · 부활(FxRules.COMMON). 튼 첫 장을 돌려준다
        public static FxRun Common(string kind, FxActor at, float scale = 1f)
        {
            if (at == null || Calm || kind == null) return null;
            FxRules.COMMON.TryGetValue(kind, out var rule);
            rule = rule ?? new CommonRule();
            FxRun first = null;
            foreach (var n in FxRules.CommonNames(kind))
            {
                var r = Play(n, new FxPlayOptions { At = at.At(rule.At), Scale = rule.Scale * scale, Flip = at.Party ? false : true, Until = rule.Until, Order = (rule.Front ? rule.Order : 20) + CommonOrderAdd, Top = TopY(), Parent = Parent });
                first = first ?? r;
            }
            if (Sound && rule.Sound != null) BolzenaAudio.Play(rule.Sound);
            return first;
        }
    }
}
