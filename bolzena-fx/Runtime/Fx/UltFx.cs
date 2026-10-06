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
        public IList<Func<Vector3>> Allies;
        // 타격 맞추기 — 전투가 정한 맞는 시각들(ms, 몸짓 시작에서 · 투사체 몫까지 넣은 것). 주면 대상 쪽 이펙트를 그 시각에(이펙트 안의 터지는 때만큼 일찍),
        // 다시 틀기를 그 타격들에 맞춰 튼다. Clock 은 전투 시계 — 전투가 매 프레임 Ms 를 적으면 이펙트 시계가 뒤처지지 않게 따라간다
        public IList<float> HitMs;
        public UltClock Clock;
        public Action<UltPart, float> OnFire;
        // 시전자 몸(자리 본) — 시전자 쪽 장을 발밑 대신 뼈에 붙여 매 프레임 따라가게(이름 안의 Point 이름 → 뜻 낱말 → 이 동작의 시전 본 → 발밑)
        public FxActor CasterActor;
        public string CastPoint;
        public IList<Vector3> TargetFeet;    // 맞는 적들 발밑(앞 → 뒤)
        public bool SeqPerTarget;            // 번호 장을 적마다 차례로(아야 · 아네트)
        public readonly Dictionary<UltPart, int> SeqIndex = new Dictionary<UltPart, int>();  // 틀 때(그 장 · 이펙트 시계 ms) — 점검  // 아군 발밑들 — 아군 쪽 판(UltPart.Ally)을 시전자와 함께 아군 각자에게도 튼다
    }

    public class UltClock { public float Ms = -1; }

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
        public float? AtMs;                  // 이 때(ms, 이펙트 시계) 튼다 — 원작 영상 맞춤(BolzenaFx.UltTweak)이 정한 장. 없으면 갈래대로
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
        static readonly Regex BEAM = new Regex(@"(laser|lazer|beam|ray\b|line|(?<!field_)light(_\d+)?$)");   // light — 레테 손전등 빛(2026-10-06)
        static readonly Regex MUZZLE = new Regex(@"(charge|muzzle|shot|fire|laser|lazer|beam|ray\b|(?<!field_)light(_\d+)?$)");
        static readonly Regex CAST_OR_CHARGE = new Regex("(cast|charge)");
        static readonly Regex AGAIN = new Regex("(hit|slash|stab)");
        static readonly Regex SCREEN = new Regex(@"(^|_)(camera|screen|bg|fullscreen)(_|$|\d)");   // 화면 전체(카메라 앞) — 볼제나 추가, 웹판에는 없던 갈래
        static readonly Regex ALLY = new Regex(@"(_a$|_a_|(^|_)(heal|buff|revival)(_|$|\d))");   // 볼제나 추가(2026-10 벨라 고학년 제보)
        static readonly Regex SEQ_TAIL = new Regex(@"_\d+$");
        static readonly Regex WORD = new Regex(@"^fx_[a-z0-9]+_(ultimate\d*_|ult_|skill\d*_?|personal_?|attack\d*_?)?");   // attack_ — 카드 attack_hand_1 이 「attack」 낱말로 대상에 가던 것
        public const int MAX_ULT = 8;
        public const int PROJ_LAG = 330;

        static string Word(string name) => WORD.Replace(name, "", 1);
        static string Head(string name) => Word(name).Split('_')[0];

        public static List<UltPart> Plan(string heroKey, UltPick? pick = null)
            => FxLibrary.HasUlt(heroKey) ? PlanNames(FxLibrary.Get(heroKey).ToList(), pick, MAX_BY.TryGetValue(heroKey, out var mx) ? mx : MAX_ULT) : new List<UltPart>();
        // 갈래가 둘 다 원작 한 벌인 사도 — 가장 큰 갈래 하나만 남기면 끝 장이 빠진다(아일라: 분노 5 + 운석 4 — 끝의 운석 낙하 · 폭발이 없었다)
        static readonly Dictionary<string, float> BIG = new Dictionary<string, float> { ["elena_ultimate_drone_2"] = 2.2f };
        static readonly Dictionary<string, float> SWEEP = new Dictionary<string, float>
        {
            ["rohnemayor_ultimate_crowd_emoji"] = 1.4f, ["rohnemayor_ultimate_object"] = 1.4f, ["rohnemayor_ultimate_smoke"] = 1.4f,
            ["shasha_ultimate_waterfall"] = 1.6f,
        };
        static readonly Regex NO_BONE = new Regex("vividivine_ultimate_full_laser");
        static readonly Dictionary<string, int> MAX_BY = new Dictionary<string, int> { ["아일라"] = 10 };

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
            // 시각을 정해 받은 장(원작 영상 맞춤)은 갈래 차례에서 빼고 그 시각에
            foreach (var p in plan) if (p.AtMs.HasValue) sched.Add(new KeyValuePair<float, UltPart>(Mathf.Max(0, p.AtMs.Value), p));
            if (plan.Any(p => p.AtMs.HasValue)) plan = plan.Where(p => !p.AtMs.HasValue).ToList();
            var pre = plan.Where(p => p.Pre).ToList();
            var main = plan.Where(p => !p.Pre && p.At != "move").ToList();
            var proj = plan.Where(p => p.At == "move").ToList();
            var hitMs = o.HitMs != null && o.HitMs.Count > 0 ? o.HitMs : null;
            if (hitMs != null)
            {
                float h0 = hitMs[0], shot = Mathf.Max(0, h0 - lag);
                float Off(UltPart p) => 1000f * FxLibrary.ImpactSec(p.Name);
                void At(UltPart p, float ms) => sched.Add(new KeyValuePair<float, UltPart>(Mathf.Max(0, ms), p));
                if (o.Dash)
                {
                    Fire(pre.Where(p => p.At == "caster"), 0, 60);
                    var near = pre.Where(p => p.At != "caster").ToList();
                    if (near.Count > 0) Fire(near, Mathf.Max(0, shot - 250), 60);
                }
                else Fire(pre, 0, 60);
                Fire(proj, shot, 0);
                Fire(main.Where(p => p.At == "caster" || p.At == "screen"), shot, 0);
                // 번호만 다른 판(슈로 slash_1 · _2 · _3)은 한꺼번에 말고 타격마다 하나씩 차례로(2026-10-06 사용자 「슈로 임팩트 이상」)
                var seq = new HashSet<UltPart>();
                foreach (var g in main.Where(p => p.At == "target").GroupBy(p => SEQ_TAIL.Replace(Word(p.Name), "")))
                {
                    var l = g.OrderBy(p => p.Name).ToList();
                    if (l.Count < 2 || hitMs.Count < 2) continue;
                    for (int j = 0; j < l.Count; j++) { int hi = Mathf.RoundToInt(j * (hitMs.Count - 1f) / (l.Count - 1)); At(l[j], hitMs[hi] - Off(l[j])); seq.Add(l[j]); if (o.SeqPerTarget) o.SeqIndex[l[j]] = j; }
                }
                foreach (var p in main.Where(p => p.At == "target" && !seq.Contains(p))) At(p, h0 - Off(p));   // 터지는 때가 첫 타격에
                var again = main.Where(p => p.Again && !seq.Contains(p)).ToList();
                if (again.Count > 0 && hitMs.Count > 1)
                {
                    float gap = Mathf.Max(400, 700 * again.Max(p => p.Dur));
                    float last = h0;
                    for (int k = 1; k < hitMs.Count; k++)
                    {
                        if (hitMs[k] - last < gap && k < hitMs.Count - 1) continue;   // 몰아치면 몇 번 건너뛰되, 마지막 타격에는 꼭
                        foreach (var p in again) At(p, p.At == "target" ? hitMs[k] - Off(p) : hitMs[k] - lag);
                        last = hitMs[k];
                    }
                }
                end = Mathf.Max(1.6f, hitMs[hitMs.Count - 1] / 1000f + 0.9f);
            }
            else if (sync)
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
                    o.OnFire?.Invoke(sched[next2].Value, now);
                    var r = FirePart(sched[next2].Value, o, from, to, flip, Mathf.Max(FxRun.FADE, end - now / 1000f));
                    if (r != null) o.OnPart?.Invoke(r);
                    if (r != null) runs.Add(new KeyValuePair<FxRun, int>(r, r.Gen));
                    next2++;
                }
                if (next2 >= sched.Count && runs.All(r => r.Key == null || r.Key.Gen != r.Value || r.Key.IsDone)) yield break;
                yield return null;
                now += 1000f * Mathf.Min(0.1f, Mathf.Max(0, BolzenaFx.DeltaTime())) * BolzenaFx.Rate;
                if (o.Clock != null && o.Clock.Ms > now) now = o.Clock.Ms;   // 전투 시계보다 뒤처지지 않게(긴 프레임 · 멈칫 뒤)
            }
        }

        // 뼈 찾기 사슬 — ① 이름 낱말(한 낱말 · 두 낱말 붙임)이 시전자 자리 본(Point_LeftEye · Wind · Doll …)이면 그것 ② 뜻 낱말 → 그 뼈들
        // ③ (시전자 쪽만) 이 동작의 시전 본(Point_Ult1 · Attack1 · Skill1). 없으면 null(발밑)
        static readonly HashSet<string> NOT_BONE = new HashSet<string> { "top", "bottom", "middle", "front", "back", "shadow", "step", "fixtop", "ultimate", "skill", "attack",
            "hit", "fx", "start", "end", "loop", "cast", "ground", "explosion", "default", "smoke", "proj", "slash", "impact", "area", "field", "aura", "buff", "heal" };
        static readonly (Regex re, string[] pts)[] MEAN =
        {
            (new Regex("(cast|ready|charge)"), new[] { "SkillReady", "Cast", "Charge" }),
            (new Regex("(light|laser|lazer|beam)"), new[] { "UltLaser", "Light", "Laser" }),
            (new Regex("(shot|muzzle|bullet|gun)"), new[] { "Ult_Shot", "Attack1_Shot", "Muzzle", "Gun", "Shot" }),
            (new Regex("(hand|punch|fist)"), new[] { "Hand", "Punch", "Fist" }),
        };
        static Func<Vector3?> CasterBone(UltPart p, UltOptions o, bool caster)
        {
            var a = o.CasterActor;
            if (a?.Point == null) return null;
            Func<Vector3?> Of(string n) => a.Point(n).HasValue ? () => a.Point(n) : (Func<Vector3?>)null;
            var toks = Word(p.Name).Split('_').Where(t => t.Length >= 3 && !t.All(char.IsDigit)).Select(t => t.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9')).ToList();
            for (int i = 0; i + 1 < toks.Count; i++) { var f = Of(toks[i] + toks[i + 1]); if (f != null) return f; }
            foreach (var t in toks) if (t.Length >= 3 && !NOT_BONE.Contains(t)) { var f = Of(t); if (f != null) return f; }
            if (!caster) return null;
            var w = Word(p.Name);
            foreach (var (re, pts) in MEAN) if (re.IsMatch(w)) foreach (var n in pts) { var f = Of(n); if (f != null) return f; }
            // (동작 시전 본으로 붙이던 것은 뺐다 — 슈로 Point_Ult1 이 적 앞 고정 점이라 오라가 적 쪽에 떴다. 뜻 낱말이 없으면 발밑을 따라간다)
            return null;
        }

        static FxRun FirePart(UltPart p, UltOptions o, Vector3 from, Vector3 to, bool flip, float until)
        {
            var b = new FxPlayOptions { Scale = o.Scale, Top = o.Top, Flip = flip, Until = until, Order = o.Order, SortingLayer = o.SortingLayer, Parent = o.Parent };
            if (p.At == "move") { b.At = (o.Muzzle != null ? o.Muzzle() : null) ?? o.ShotFrom ?? from;   // 쏘는 그 순간의 총구 · 쏘는 자리(캬롯 — 몸 가운데에서 나가던 것)
b.MoveTo = o.ToBody ?? to; b.MoveDur = 0.35f;
                if (p.Name.Contains("meteor")) { b.At = (o.ToBody ?? to) + new Vector3(-2.5f, 7f, 0); b.MoveDur = 0.45f; }   // 운석(아일라) — 하늘에서 떨어진다
                return BolzenaFx.Play(p.Name, b); }
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
            // 크게 터지는 장 — 원작은 화면 오른쪽(적 무리 전체)을 덮는다(엘레나 마지막 드론 폭발)
            foreach (var bs in BIG)
                if (p.Name.Contains(bs.Key)) { b.Scale *= bs.Value; b.At = to; b.Center = true; b.SpreadWidth = o.ToWidth; return BolzenaFx.Play(p.Name, b); }
            // 쓸고 가는 장 — 원작은 사도 쪽에서 나와 적들을 가로질러 오른쪽으로 빠진다(로네(시장) 군중 · 샤샤 물줄기). 원작 영상 비교 2026-10-06
            foreach (var sw in SWEEP)
                if (p.Name.Contains(sw.Key))
                {
                    b.At = new Vector3(from.x + 0.8f, to.y, to.z);
                    b.MoveTo = new Vector3(to.x + Mathf.Max(1.2f, o.ToWidth / 2) + 1.5f, to.y, to.z); b.MoveDur = sw.Value;
                    return BolzenaFx.Play(p.Name, b);
                }
            // 뼈에 붙이면 엉뚱한 데로 가는 장 — 몸 앞(가슴 높이)에 그대로(비비(신성) 거대 형상의 레이저: 원작은 손끝에서 적 쪽으로 가로)
            if (NO_BONE.IsMatch(p.Name) && o.CasterActor != null) { b.At = o.CasterActor.At(FxAnchor.Front); b.Bare = true; return BolzenaFx.Play(p.Name, b); }
            Vector3? m = p.Muzzle && o.Muzzle != null ? o.Muzzle() : null;
            if (m.HasValue) { b.At = m.Value; b.Track = o.Muzzle; b.Bare = true; return BolzenaFx.Play(p.Name, b); }
            // 시전자 쪽 · 이름에 시전자 뼈가 든 대상 쪽 장 — 그 뼈에 붙여 따라간다(전수 조사 B · C · G). 이미 높이 짜인 판(이미터 3 단위 넘게 위)은 발밑 그대로
            if (o.CasterActor != null && FxLibrary.EmitterTop(p.Name) < 3f)
            {
                var bone = CasterBone(p, o, p.At == "caster");
                var topY = BolzenaFx.TopY();   // 화면 위로 나간 뼈(네르(빡침) SkillReady)는 안 쓴다
                if (bone != null) { var at0 = bone(); if (at0.HasValue && (!topY.HasValue || at0.Value.y < topY.Value)) { b.At = at0.Value; b.Track = bone; b.Bare = true; return BolzenaFx.Play(p.Name, b); } }
                if (p.At == "caster" && !o.Dash) { var a = o.CasterActor; Func<Vector3?> feet = () => a.Feet; b.At = a.Feet; b.Track = feet; return BolzenaFx.Play(p.Name, b); }   // 몸이 움직여도 발밑을 따라
            }
            b.At = p.At == "caster" ? from : p.Body && o.ToBody.HasValue ? o.ToBody.Value : to;
            b.Center = p.At == "target" && !o.Dash && !p.Body;
            // 번호 장을 적마다 차례로(아야 연꽃 — 앞 적 먼저, 다음은 뒤 적) · 번호 없는 대상 장은 맨 앞 적에(아네트 금지 표지)
            if (o.SeqPerTarget && p.At == "target" && o.TargetFeet != null && o.TargetFeet.Count > 1)
            {
                int j = o.SeqIndex.TryGetValue(p, out var sj) ? sj : 0;
                b.At = o.TargetFeet[j % o.TargetFeet.Count]; b.Center = true;
                return BolzenaFx.Play(p.Name, b);
            }
            if (p.At == "target" && o.ToWidth > 0) b.SpreadWidth = o.ToWidth;
            return BolzenaFx.Play(p.Name, b);
        }
    }
}
