using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Bolzena.Fx
{
    // 고학년 한 벌의 옵션 — 웹판 playUltFx(heroKey, o) 의 o
    public class UltOptions
    {
        public Vector3 From;                 // 시전자 발밑(월드)
        public Vector3? To;                  // 대상 지점(발밑 높이) — 단일 대상은 그 대상, 전체 공격은 적 무리의 정중앙(없으면 From)
        public float ToWidth;                // 전체 공격 — 적 무리의 가로 폭(월드). 대상 쪽 이펙트가 이 폭을 덮도록 벌린다(0 = 단일 대상)
        public float Scale = 1f;
        public float? Top;                   // 이펙트가 넘지 않을 위끝(월드 y)
        public float? Impact;                // SD 동작이 쏘는/때리는 순간(ms, 동작 시작에서 — 스파인 이벤트). 주면 그 순간에 맞춘다
        public float? End;                   // 여러 번 때리는 창의 끝(ms)
        public IList<float> Marks;           // 그 창의 이벤트 시각들(ms) — 레이저 · 타격을 다시 튼다
        public UltPick? Pick;                 // SD 고학년 갈래(앨리스 불 · 번개 · 바람)
        public Func<Vector3?> Muzzle;        // 시전자 총구(월드). 주면 총구 쪽 이펙트가 붙어 따라간다
        public bool Dash;                    // 시전자가 달려가 부딪친다 — To 가 부딪치는 자리
        public int Order = 300;
        public string SortingLayer;
        public Transform Parent;
        public Vector3? ShotFrom;            // 투사체가 떠나는 자리(시전 본 — 없으면 From)
        public Vector3? ToBody;              // 대상 몸 가운데 — 몸에 맞는 이펙트(UltPart.Body)가 여기
        public Action<FxRun> OnPart;         // 한 장 틀 때마다(시험 · 재기)
        public IList<Func<Vector3>> Allies;  // 아군 발밑들 — 아군 쪽 판(UltPart.Ally)을 시전자와 함께 아군 각자에게도 튼다
    }

    public struct UltPick { public int I, N; public UltPick(int i, int n) { I = i; N = n; } }

    // 고학년 이펙트 한 장의 자리 · 차례 — 웹판 ultPlan 의 한 줄
    public class UltPart
    {
        public string Name;
        public string At;                    // "caster" · "target" · "move"(투사체)
        public bool Pre;                     // 준비(모으기 · 시전) — 바로
        public bool Again;                   // 때리는 창 동안 다시 튼다(레이저 · 타격 · 베기)
        public bool Muzzle;                  // 총구에 붙는다
        public bool Body;                    // 대상 발밑이 아니라 몸 가운데(원작 타격 프리팹 prefabeffecthit — 카드에서만)
        public bool Ally;                    // 아군 쪽 판(_a · heal · buff · revival) — 시전자(와 UltOptions.Allies)에게
        public float Dur;
        public override string ToString() => $"{Name} [{At}{(Pre ? " pre" : "")}{(Again ? " again" : "")}{(Muzzle ? " muzzle" : "")}] {Dur:0.##}s";
    }

    // 웹판 js/fx-burst.js 의 ultPlan · ultLagMs · ultImpactMs · playUltFx 를 그대로 옮겼다.
    // 이름 낱말로 자리와 차례를 가른다 — 앞(준비 · 시전 · 충전)은 바로, 본(폭발 · 타격 · 바닥 …)은 때리는 순간.
    // 투사체는 시전자에서 대상으로 날아가고, 레이저는 시전자에서 대상 쪽을 보고 뻗는다.
    public static class UltFx
    {
        static readonly Regex AT_CASTER = new Regex("(ready|cast|charge|start|aura|buff|player|meditation|book|energy|craft|happy|spawn|muzzle|rise|summon|wing|shield|heal|barrier|roar|breath|clap|personal)");
        static readonly Regex AT_TARGET = new Regex("(explosion|hit|ground|crash|impact|rain|thunder|stomp|meteor|slash|attack|bomb|burst|boom|crack|wave|field|area|decal|target|fall|strike|drop|lightning|fire|ice|smoke|trace)");
        static readonly Regex PRE = new Regex("(ready|cast|charge|start|meditation|aura)");
        static readonly Regex PROJ = new Regex("(proj|projectile|bullet|missile|shot|arrow|throw|(^|_)wave(_|$))");
        static readonly Regex BEAM = new Regex(@"(laser|lazer|beam|ray\b|line)");
        static readonly Regex MUZZLE = new Regex(@"(charge|muzzle|shot|fire|laser|lazer|beam|ray\b)");
        static readonly Regex CAST_OR_CHARGE = new Regex("(cast|charge)");
        static readonly Regex AGAIN = new Regex("(hit|slash|stab)");
        static readonly Regex SCREEN = new Regex(@"(^|_)(camera|screen|bg|fullscreen)(_|$|\d)");   // 화면 전체(카메라 앞) — 볼제나 추가, 웹판에는 없던 갈래
        static readonly Regex ALLY = new Regex(@"(_a$|_a_|(^|_)(heal|buff|revival)(_|$|\d))");   // 볼제나 추가(2026-10 벨라 고학년 제보)
        static readonly Regex WORD = new Regex("^fx_[a-z0-9]+_(ultimate_|ult_|skill_?|personal_?)?");
        public const int MAX_ULT = 8;
        public const int PROJ_LAG = 330;

        static string Word(string name) => WORD.Replace(name, "", 1);
        static string Head(string name) => Word(name).Split('_')[0];

        public static List<UltPart> Plan(string heroKey, UltPick? pick = null)
            => FxLibrary.HasUlt(heroKey) ? PlanNames(FxLibrary.Get(heroKey).ToList(), pick, MAX_ULT) : new List<UltPart>();

        // 이름들로 자리 · 차례를 가른다(고학년 · 카드 같이 쓴다). max 넘으면 가장 큰 갈래 하나만
        public static List<UltPart> PlanNames(List<string> names, UltPick? pick = null, int max = MAX_ULT)
        {
            var res = new List<UltPart>();
            if (names == null || names.Count == 0) return res;
            if (pick.HasValue && pick.Value.N > 1)
            {
                var cnt = new Dictionary<string, int>();
                var order = new List<string>();
                foreach (var x in names) { var k = Head(x); if (!cnt.ContainsKey(k)) { cnt[k] = 0; order.Add(k); } cnt[k]++; }
                var big = order.Where(k => cnt[k] >= 3).ToList();
                List<string> ways = big.Count == pick.Value.N ? big : cnt.Count == pick.Value.N ? order : null;
                if (ways != null) { var w = ways[pick.Value.I % pick.Value.N]; names = names.Where(x => Head(x) == w).ToList(); }
            }
            // 여러 갈래면 가장 큰 갈래 하나만 — 다 틀면 화면이 엉킨다
            if (names.Count > max)
            {
                var n = new Dictionary<string, int>();
                var order = new List<string>();
                foreach (var x in names) { var k = Head(x); if (!n.ContainsKey(k)) { n[k] = 0; order.Add(k); } n[k]++; }
                var top = order.OrderByDescending(k => n[k]).First();   // 같으면 먼저 나온 것(안정 정렬)
                if (n[top] >= 3) names = names.Where(x => Head(x) == top).ToList();
                names = names.Take(max).ToList();
            }
            foreach (var name in names)
            {
                var w = Word(name);
                bool proj = PROJ.IsMatch(w), beam = !proj && BEAM.IsMatch(w);
                bool ally = ALLY.IsMatch(w);   // 아군 쪽 판(벨라 hit_a · proj_a — 적 쪽은 _e) · 회복 · 강화 — 적에게 틀면 엉뚱한 색이 겹친다
                if (ally) { proj = false; beam = false; }
                string at = SCREEN.IsMatch(w) ? "screen" : ally ? "caster" : proj ? "move" : beam ? "caster" : AT_TARGET.IsMatch(w) && !CAST_OR_CHARGE.IsMatch(w) ? "target" : AT_CASTER.IsMatch(w) ? "caster" : "target";
                bool pre = !proj && PRE.IsMatch(w);
                res.Add(new UltPart
                {
                    Name = name, At = at, Pre = pre, Again = !pre && !ally && (beam || AGAIN.IsMatch(w)), Ally = ally,
                    Muzzle = at == "caster" && MUZZLE.IsMatch(w), Dur = FxLibrary.Duration(name),
                });
            }
            return res;
        }

        // 투사체가 있으면 쏘고 나서 대상에 닿기까지(ms) — 쏘는 순간을 아는 쪽(SD 이벤트)이 타격을 이만큼 늦춘다
        public static int LagMs(string heroKey, UltPick? pick = null) => Plan(heroKey, pick).Any(p => p.At == "move") ? PROJ_LAG : 0;

        // SD 이벤트를 모를 때 본 이펙트가 터지기까지(ms) — 화면이 타격(숫자 · 체력)을 여기에 맞춘다
        public static int ImpactMs(string heroKey) => FxLibrary.HasUlt(heroKey) ? ImpactMs(Plan(heroKey)) : 0;
        public static int ImpactMs(List<UltPart> plan)
        {
            if (plan == null || plan.Count == 0) return 0;
            var pre = plan.Where(p => p.Pre).ToList();
            float t = pre.Count > 0 ? 1000 * Mathf.Min(0.6f, pre.Max(p => p.Dur) * 0.5f) : 0;
            if (plan.Any(p => p.At == "move")) t += PROJ_LAG;
            return Mathf.RoundToInt(t);
        }

        // ── 한 벌 틀기 ── 시각표를 먼저 다 세우고(ms), 전투 시계로 흘려 보내며 튼다
        public static IEnumerator Run(string heroKey, UltOptions o)
        {
            if (BolzenaFx.Calm || !FxLibrary.HasUlt(heroKey)) return Nothing();
            FxLibrary.PreloadUlt(heroKey);
            return RunPlan(Plan(heroKey, o.Pick), o);
        }

        static IEnumerator Nothing() { yield break; }

        // 계획 하나를 시각표대로 — 고학년 · 카드가 같이 쓴다
        public static IEnumerator RunPlan(List<UltPart> plan, UltOptions o)
        {
            if (BolzenaFx.Calm || plan == null || plan.Count == 0) yield break;
            var from = o.From;
            var to = o.To ?? o.From;
            bool flip = to.x < from.x;
            bool sync = o.Impact.HasValue;
            int lag = plan.Any(p => p.At == "move") ? PROJ_LAG : 0;
            float impact = o.Impact ?? 0, endMs = o.End ?? 0;
            float end = sync ? Mathf.Max(1.6f, (Mathf.Max(impact, endMs) + lag) / 1000f + 0.9f)
                             : Mathf.Max(1.6f, ImpactMs(plan) / 1000f + 1.2f);
            var sched = new List<KeyValuePair<float, UltPart>>();
            void Fire(IEnumerable<UltPart> list, float at, float gap) { int i = 0; foreach (var p in list) sched.Add(new KeyValuePair<float, UltPart>(at + i++ * gap, p)); }
            var pre = plan.Where(p => p.Pre).ToList();
            var main = plan.Where(p => !p.Pre && p.At != "move").ToList();
            var proj = plan.Where(p => p.At == "move").ToList();
            if (sync)
            {
                if (o.Dash)
                {
                    Fire(pre.Where(p => p.At == "caster"), 0, 60);
                    var near = pre.Where(p => p.At != "caster").ToList();
                    if (near.Count > 0) Fire(near, Mathf.Max(0, impact - 250), 60);
                }
                else Fire(pre, 0, 60);
                Fire(proj, impact, 80);
                Fire(main.Where(p => p.At == "caster" || p.At == "screen"), impact, 60);
                Fire(main.Where(p => p.At == "target"), impact + lag, 90);
                var again = main.Where(p => p.Again).ToList();
                if (again.Count > 0 && endMs > impact && o.Marks != null)
                {
                    float gap = Mathf.Max(500, 800 * again.Max(p => p.Dur));
                    float next = impact + gap;
                    foreach (var m in o.Marks)
                    {
                        if (m < next || m > endMs) continue;
                        Fire(again, m + (again.Any(p => p.At == "target") ? lag : 0), 60);
                        next = m + gap;
                    }
                }
            }
            else
            {
                float at = 0;
                if (pre.Count > 0) { Fire(pre, 0, 60); at += 1000 * Mathf.Min(0.6f, pre.Max(p => p.Dur) * 0.5f); }
                if (proj.Count > 0) { Fire(proj, at, 80); at += PROJ_LAG; }
                Fire(main.Where(p => p.At == "caster" || p.At == "screen"), at, 60);
                Fire(main.Where(p => p.At == "target"), at, 90);
            }
            sched.Sort((a, b) => a.Key.CompareTo(b.Key));
            var runs = new List<KeyValuePair<FxRun, int>>();   // 무대 · 그때의 세대(풀에서 되쓰이면 세대가 바뀐다)
            float now = 0;       // ms
            int next2 = 0;
            while (true)
            {
                while (next2 < sched.Count && sched[next2].Key <= now)
                {
                    var r = FirePart(sched[next2].Value, o, from, to, flip, Mathf.Max(FxRun.FADE, end - now / 1000f));
                    if (r != null) o.OnPart?.Invoke(r);
                    if (r != null) runs.Add(new KeyValuePair<FxRun, int>(r, r.Gen));
                    next2++;
                }
                if (next2 >= sched.Count && runs.All(r => r.Key == null || r.Key.Gen != r.Value || r.Key.IsDone)) yield break;
                yield return null;
                now += 1000f * Mathf.Min(1f / 20f, Mathf.Max(0, BolzenaFx.DeltaTime())) * BolzenaFx.Rate;
            }
        }

        static FxRun FirePart(UltPart p, UltOptions o, Vector3 from, Vector3 to, bool flip, float until)
        {
            var b = new FxPlayOptions { Scale = o.Scale, Top = o.Top, Flip = flip, Until = until, Order = o.Order, SortingLayer = o.SortingLayer, Parent = o.Parent };
            if (p.At == "move") { b.At = o.ShotFrom ?? from; b.MoveTo = o.ToBody ?? to; b.MoveDur = 0.35f; return BolzenaFx.Play(p.Name, b); }
            if (p.Ally && o.Allies != null && o.Allies.Count > 0)
            {
                b.At = from; b.Flip = false;
                var first = BolzenaFx.Play(p.Name, b);
                foreach (var a in o.Allies)
                {
                    var r = BolzenaFx.Play(p.Name, new FxPlayOptions { At = a(), Scale = o.Scale, Top = o.Top, Until = until, Order = o.Order, SortingLayer = o.SortingLayer, Parent = o.Parent });
                    if (r != null) o.OnPart?.Invoke(r);
                }
                return first;
            }
            if (p.At == "screen") { b.At = BolzenaFx.ScreenCenter(); b.Flip = false; b.Top = null; return BolzenaFx.Play(p.Name, b); }
            Vector3? m = p.Muzzle && o.Muzzle != null ? o.Muzzle() : null;
            if (m.HasValue) { b.At = m.Value; b.Track = o.Muzzle; b.Bare = true; return BolzenaFx.Play(p.Name, b); }
            b.At = p.At == "caster" ? from : p.Body && o.ToBody.HasValue ? o.ToBody.Value : to;
            b.Center = p.At == "target" && !o.Dash && !p.Body;
            if (p.At == "target" && o.ToWidth > 0) b.SpreadWidth = o.ToWidth;
            return BolzenaFx.Play(p.Name, b);
        }
    }
}
