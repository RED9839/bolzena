using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Bolzena.Fx
{
    // 때리는 순간 — 동작 시작에서 ms
    public class StrikeInfo
    {
        public int At;                       // 때리는/쏘는 순간
        public int End;                      // 여러 번 때리는 창의 끝(길어야 At + 2600)
        public List<int> Marks = new List<int>();   // 그 창의 이벤트 시각들
        public int Total;                    // 동작(이어지는 조각까지) 전체 길이
        public override string ToString() => $"at {At} · end {End} · marks [{string.Join(",", Marks)}] · total {Total}";
    }

    public class DashTiming
    {
        public int Go, Hit, Land;            // 달리기 시작 · 닿음(타격) · 옮기기 끝(ms)
        public int[] Home;                   // 돌아오는 창 [시작, 끝] 또는 null
        public DashCfg Cfg;
    }

    // 한 몸짓의 계획 — 웹판 planAct 가 돌려주는 것과 같다
    public class ActPlan
    {
        public string Anim;                  // 첫 조각(null = 제자리)
        public List<string> Chain = new List<string>();   // 이어지는 조각(고리를 여러 번 돌리면 그만큼 되풀이)
        public UltPick? Pick;                // 고학년 갈래 i / n
        public DashTiming Dash;
        public StrikeInfo S;
        public string Group;                 // 사도 소리 갈래 attack · power · skill · ult
        public List<SoundEvent> Snd = new List<SoundEvent>();
        public MotionPick? Motion;
        public UltWay Table;                 // 고학년 몸짓 표(ult_motion.json)에서 온 계획이면 그 갈래 — 이동 · 소리 · 전체 길이
        public int CutMs;
        public float From;
        public IEnumerable<string> Names { get { if (Anim != null) yield return Anim; foreach (var c in Chain) yield return c; } }
        public override string ToString() => $"{Anim} +[{string.Join(",", Chain)}] pick {(Pick.HasValue ? Pick.Value.I + "/" + Pick.Value.N : "-")} · {S} · dash {(Dash != null ? $"{Dash.Go}->{Dash.Hit}" : "-")} · {Group}";
    }

    // 스파인 이벤트로 몸짓 · 때리는 순간 · 소리 칸을 정한다 — 웹판 js/fight-screen.js 의 waysOf · strikeOf · planAct · actName,
    // 총구는 spine-view 의 boneScreen 과 같은 셈(본 끝 = 본 길이만큼 앞)
    public static class SpineMotion
    {
        public struct Ev { public string Name; public float Time; public string S; public int I; }

        // 웹판 spine-view findAnim — 이름 그대로, 없으면 대소문자 무시
        public static Spine.Animation Find(SkeletonData d, string n)
        {
            if (d == null || n == null) return null;
            var a = d.FindAnimation(n);
            if (a != null) return a;
            foreach (var x in d.Animations) if (string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase)) return x;
            return null;
        }

        // 웹판 spine-view 와 같은 옷 — 「Normal」(없고 기본 스킨도 비면 첫 스킨) 위에, 자리가 겹치지 않는 덧스킨(코미 Weapon 같은 부품)을 겹쳐 입는다.
        // 스탠딩(standing=true)은 Normal 만. skin 을 주면 그 스킨만(이름 그대로 · 대소문자 무시). 입힌 스킨(또는 null)을 돌려준다
        public static Skin Wear(SkeletonData d, string skin = null, bool standing = false)
        {
            if (d == null) return null;
            Skin ByName(string n) { if (n == null) return null; var x = d.FindSkin(n); if (x != null) return x; foreach (var k in d.Skins) if (string.Equals(k.Name, n, StringComparison.OrdinalIgnoreCase)) return k; return null; }
            var wear = ByName(skin);
            if (wear != null) return wear;
            var extras = new List<Skin>();
            foreach (var k in d.Skins) if (k != d.DefaultSkin) extras.Add(k);
            var normal = extras.Find(k => string.Equals(k.Name, "normal", StringComparison.OrdinalIgnoreCase));
            var bas = normal ?? (d.DefaultSkin != null ? null : (extras.Count > 0 ? extras[0] : null));
            if (bas == null) return null;
            wear = new Skin("wear");
            wear.AddSkin(bas);
            var used = new HashSet<int>();
            foreach (var e in bas.Attachments) used.Add(e.SlotIndex);
            if (!standing)
                foreach (var x in extras)
                {
                    if (x == bas) continue;
                    bool clash = false;
                    foreach (var e in x.Attachments) if (used.Contains(e.SlotIndex)) { clash = true; break; }
                    if (clash) continue;
                    wear.AddSkin(x);
                    foreach (var e in x.Attachments) used.Add(e.SlotIndex);
                }
            return wear;
        }

        // 입히기 — SkeletonAnimation 에 Wear 를 입힌다. 웹판처럼 섞기(mix) 0 이 기본(조각 사이를 섞지 않는다)
        public static void Dress(SkeletonAnimation sa, string skin = null, bool standing = false, float mix = 0f)
        {
            var w = Wear(sa.Skeleton.Data, skin, standing);
            if (w != null) { sa.Skeleton.SetSkin(w); sa.Skeleton.SetSlotsToSetupPose(); }
            sa.AnimationState.Data.DefaultMix = mix;
        }

        // 쉬는 동작 — 웹판 rest: Idle → Idle_1 → Idle_숫자 → 첫 동작
        public static string Rest(SkeletonData d)
        {
            var a = Find(d, "Idle") ?? Find(d, "Idle_1");
            if (a != null) return a.Name;
            foreach (var x in d.Animations) if (System.Text.RegularExpressions.Regex.IsMatch(x.Name, @"^Idle_\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return x.Name;
            return d.Animations.Count > 0 ? d.Animations.Items[0].Name : null;
        }
        public static bool Has(SkeletonData d, string n) => Find(d, n) != null;
        // 고유 이름이 없고 _Full 로만 있는 사도도 있다(네티)
        public static string AnimIn(SkeletonData d, string n) => Has(d, n) ? n : Has(d, n + "_Full") ? n + "_Full" : null;
        public static float Duration(SkeletonData d, string n) { var a = Find(d, n); return a == null ? 0 : a.Duration; }

        public static List<Ev> Events(SkeletonData d, string anim)
        {
            var res = new List<Ev>();
            var a = Find(d, anim);
            if (a == null) return res;
            foreach (var tl in a.Timelines)
                if (tl is EventTimeline et)
                    foreach (var e in et.Events)
                        res.Add(new Ev { Name = e.Data.Name, Time = e.Time, S = e.String ?? "", I = e.Int });
            res.Sort((x, y) => x.Time.CompareTo(y.Time));
            return res;
        }

        static double Num(string s) => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (string.IsNullOrEmpty(s) ? 0 : double.NaN);
        static readonly Regex FRESH = new Regex("(^|,)100000[34](,|$)");

        // 조각 — base_1 → base_2(_Loop) … 를 잇되, 목소리 · 표시(1000003 스킬 · 1000004 고학년)부터 다시 시작하는 조각은 다른 갈래다
        public static List<List<string>> WaysOf(SkeletonData d, string bas)
        {
            var parts = new List<string>();
            for (int k = 1; k < 30; k++)
            {
                string n = Has(d, $"{bas}_{k}") ? $"{bas}_{k}" : Has(d, $"{bas}_{k}_Loop") ? $"{bas}_{k}_Loop" : null;
                if (n == null) break;
                parts.Add(n);
            }
            bool Fresh(string n) => Events(d, n).Any(e => e.Time < 0.15f && ((e.Name == "Voice" && e.S == "1") || FRESH.IsMatch(e.S)));
            var ways = new List<List<string>>();
            foreach (var n in parts) if (ways.Count == 0 || Fresh(n)) ways.Add(new List<string> { n }); else ways[ways.Count - 1].Add(n);
            return ways;
        }

        // 때리는 순간 — 둘째 효과음(SFX 2 …)이 처음 나는 때, 없으면 이 사도만의 첫 Event(값 ≥ 1000100), 그것도 없으면 첫 조각 길이의 45%.
        // 고리(_Loop)가 있으면 그 뒤 조각에서 찾는다. fixAt(ms) 을 주면 그것(달려가 닿는 고학년)
        public static StrikeInfo StrikeOf(SkeletonData d, IList<string> names, int? fixAt = null)
        {
            if (d == null || names == null || names.Count == 0) return null;
            float off = 0, from = 0;
            var evs = new List<Ev>();
            foreach (var n in names)
            {
                foreach (var e in Events(d, n)) { var x = e; x.Time = off + e.Time; evs.Add(x); }
                off += Duration(d, n);
                if (n.EndsWith("_loop", StringComparison.OrdinalIgnoreCase)) from = off;
            }
            bool Own(Ev e) => e.Name == "Event" && e.S.Split(',').Any(x => Num(x) >= 1000100);
            var late = evs.Where(e => e.Time >= from - 1e-3f).ToList();
            Ev? hit = null;
            foreach (var e in late) if (e.Name == "SFX" && (Num(e.S) >= 2 || from > 0)) { hit = e; break; }
            if (hit == null) foreach (var e in late) if (Own(e)) { hit = e; break; }
            int at = fixAt ?? Mathf.RoundToInt(1000 * (hit.HasValue ? hit.Value.Time : Duration(d, names[0]) * 0.45f));
            var marks = new List<int>();
            foreach (var e in late)
            {
                if (!(Own(e) || e.Name == "SFX")) continue;
                if (e.Time * 1000 < at - 1) continue;
                int m = Mathf.RoundToInt(e.Time * 1000);
                if (!marks.Contains(m)) marks.Add(m);
            }
            int end = Math.Min(marks.Count > 0 ? marks[marks.Count - 1] : at, at + 2600);
            return new StrikeInfo { At = at, End = end, Marks = marks.Where(m => m <= end).ToList(), Total = Mathf.RoundToInt(off * 1000) };
        }

        // 고학년 — 조각을 잇되 갈래는 하나(way, 없으면 무작위)
        public static ActPlan PlanUlt(SkeletonData d, string heroKey, int? way = null)
        {
            // 표가 있으면 원작 갈래 그대로(NextAni 순서 · 고리 횟수까지 펼친 조각) — 갈래를 여기서 나누지 않는다
            var tw = UltMotion.Ways(heroKey);
            if (tw != null)
            {
                int ti = way.HasValue ? ((way.Value % tw.Count) + tw.Count) % tw.Count : UnityEngine.Random.Range(0, tw.Count);
                var w = tw[ti];
                var names = w.Names.Where(n => Has(d, n)).ToList();
                if (names.Count > 0) return FromTable(d, w, names, new UltPick(ti, tw.Count));
            }
            if (!Has(d, "Ultimate1_1")) return PlanNames(d, heroKey, new List<string> { Has(d, "Skill1_1") ? "Skill1_1" : "Attack1_1" }, "ult", null, true);   // 웹판 actName 그대로
            var ways = WaysOf(d, "Ultimate1");
            int i = way.HasValue ? ((way.Value % ways.Count) + ways.Count) % ways.Count : UnityEngine.Random.Range(0, ways.Count);
            return PlanNames(d, heroKey, ways[i], "ult", new UltPick(i, ways.Count), true);
        }

        // 카드 — 무엇을 하는 카드인지 보고 동작 · 소리 갈래를 고른다(CardMotion). 스킬은 첫 갈래의 조각을 이어서
        public static ActPlan PlanCard(SkeletonData d, string heroKey, string role, MotionCard card)
        {
            var m = CardMotion.Pick(card, role, heroKey, n => AnimIn(d, n) != null);
            var name = m.Anim != null ? AnimIn(d, m.Anim) : null;
            if (name == null)
            {
                if (m.Tier != "light" && m.Tier != "heavy" && m.Tier != "sig") return new ActPlan { Motion = m };
                name = AnimIn(d, "Attack1_1") ?? AnimIn(d, "Skill1_1");
                if (name == null) return new ActPlan { Motion = m };
            }
            var names = new List<string> { name };
            if (name == "Skill1_1") { var w = WaysOf(d, "Skill1"); if (w.Count > 0) names.AddRange(w[0].Skip(1)); }
            var p = PlanNames(d, heroKey, names, m.Group ?? (name.StartsWith("Attack2") ? "power" : name.StartsWith("Attack") ? "attack" : "skill"), null, false);
            p.Motion = m;
            p.CutMs = m.CutMs;
            p.From = m.From;
            return p;
        }

        static ActPlan FromTable(SkeletonData d, UltWay w, List<string> names, UltPick pick)
        {
            var p = new ActPlan { Group = "ult", Pick = pick, Table = w, Anim = names[0], Chain = names.Skip(1).ToList() };
            float off = 0;
            foreach (var n in names)
            {
                foreach (var e in Events(d, n))
                    if (e.Name == "SFX" && Num(e.S) > 0) p.Snd.Add(new SoundEvent { N = (int)Num(e.S), T = Mathf.RoundToInt(1000 * (off + e.Time)) });
                off += Duration(d, n);
            }
            p.S = new StrikeInfo { At = w.At, End = Math.Max(w.At, w.End), Marks = w.Marks.Count > 0 ? new List<int>(w.Marks) : new List<int> { w.At }, Total = Math.Max(w.MotionMs, Mathf.RoundToInt(off * 1000)) };
            return p;
        }

        // 이름들로 계획 — 달리기(DASH) · 때리는 순간 · 소리 칸
        public static ActPlan PlanNames(SkeletonData d, string heroKey, IList<string> names0, string group, UltPick? pick, bool ult)
        {
            var p = new ActPlan { Group = group, Pick = pick };
            if (names0 == null || names0.Count == 0 || names0[0] == null) return p;
            DashCfg dc = null;
            if (ult && heroKey != null) MotionTables.DASH.TryGetValue(heroKey, out dc);
            var chain = names0.Skip(1).ToList();
            if (dc != null && dc.Loop != null) chain = chain.SelectMany(n => Enumerable.Repeat(n, dc.Loop.TryGetValue(n, out var c) ? c : 1)).ToList();
            p.Anim = names0[0];
            p.Chain = chain;
            var names = p.Names.ToList();
            int? When(string anim, float sec)
            {
                if (anim == null || sec < 0) return null;
                float off = 0;
                foreach (var n in names) { if (n == anim) return Mathf.RoundToInt(1000 * (off + sec)); off += Duration(d, n); }
                return null;
            }
            if (dc != null)
            {
                int? go = When(dc.GoAnim, dc.GoSec), hit = When(dc.HitAnim, dc.HitSec), land = When(dc.LandAnim, dc.LandSec);
                int? h0 = When(dc.HomeFromAnim, dc.HomeFromSec), h1 = When(dc.HomeToAnim, dc.HomeToSec);
                if (go.HasValue && hit.HasValue && hit > go)
                    p.Dash = new DashTiming
                    {
                        Go = go.Value, Hit = hit.Value, Land = land.HasValue && land > go && land <= hit ? land.Value : hit.Value,
                        Home = h0.HasValue && h1.HasValue && h1 > h0 ? new[] { h0.Value, h1.Value } : null, Cfg = dc,
                    };
            }
            float off2 = 0;
            foreach (var n in names)
            {
                foreach (var e in Events(d, n))
                    if (e.Name == "SFX" && Num(e.S) > 0) p.Snd.Add(new SoundEvent { N = (int)Num(e.S), T = Mathf.RoundToInt(1000 * (off2 + e.Time)) });
                off2 += Duration(d, n);
            }
            p.S = StrikeOf(d, names, p.Dash != null ? p.Dash.Hit : (int?)null);
            return p;
        }

        // ── 본 자리 ── tip 이면 본 끝(총 몸통 본이면 총구)
        public static Vector3? BoneWorld(SkeletonAnimation sa, string bone, bool tip = false)
        {
            if (sa == null || sa.Skeleton == null || string.IsNullOrEmpty(bone)) return null;
            var b = sa.Skeleton.FindBone(bone);
            if (b == null) return null;
            float L = tip ? b.Data.Length : 0;
            float wx = b.WorldX + b.A * L, wy = b.WorldY + b.C * L;
            return sa.transform.TransformPoint(new Vector3(wx, wy, 0));
        }

        // 그 사도의 총구 — 표(MotionTables.MUZZLE), 없으면 이름(muzzle · barrel)으로 찾은 본. 없으면 null
        public static Func<Vector3?> Muzzle(SkeletonAnimation sa, string heroKey)
        {
            if (sa == null || sa.Skeleton == null) return null;
            string bone = null; bool tip = false;
            if (heroKey != null && MotionTables.MUZZLE.TryGetValue(heroKey, out var mz)) { bone = mz.Key; tip = mz.Value; }
            else
            {
                foreach (var b in sa.Skeleton.Bones) if (MotionTables.MUZZLE_RE.IsMatch(b.Data.Name)) { bone = b.Data.Name; break; }
            }
            if (bone == null) return null;
            return () => BoneWorld(sa, bone, tip);
        }
    }
}
