using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;

namespace Bolzena.Fx
{
    // 스파인 유닛 → 이펙트 쪽(FxActor · FxSync). 원작 SD 는 모두 Point_Bottom · Middle · Top · Front · Back 과
    // 동작마다 Point_Attack1 · Point_Skill1 · Point_Ult1 · Point_SkillReady 같은 본을 가진다 — 원작 이펙트가 붙는 자리
    public static class SpineFx
    {
        // height: 그림 높이(월드) — 0 이면 처음 자세의 뼈대 경계에서 잰다
        public static FxActor Actor(SkeletonAnimation sa, string key, bool party, float height = 0)
        {
            if (sa == null) return null;
            var tr = sa.transform;
            if (height <= 0)
            {
                float h = 0;
                try
                {
                    var b = sa.GetComponent<MeshRenderer>();
                    if (b != null && b.bounds.size.y > 0) h = b.bounds.size.y;
                }
                catch { }
                height = h > 0.1f ? h : 1.5f;
            }
            var cache = new Dictionary<string, string>();
            string Find(string point)
            {
                if (cache.TryGetValue(point, out var hit)) return hit;
                var sk = sa.Skeleton;
                string want = "Point_" + point;
                // 이름 그대로 → 그것으로 시작하는 것(Point_Attack1_Shot · Point_Ult1) — 뒤의 _T(대상 쪽 표시)는 뺀다
                hit = sk.FindBone(want) != null ? want
                    : sk.Bones.Select(x => x.Data.Name).FirstOrDefault(n => n.StartsWith(want, System.StringComparison.OrdinalIgnoreCase) && !n.EndsWith("_T"));
                cache[point] = hit;
                return hit;
            }
            return new FxActor
            {
                FeetAt = () => tr != null ? tr.position : Vector3.zero,
                Height = height, Party = party, Key = key,
                Point = p => { if (tr == null) return null; var n = Find(p); return n != null ? SpineMotion.BoneWorld(sa, n) : null; },
                Muzzle = SpineMotion.Muzzle(sa, key),
            };
        }

        // 몸짓 계획 → 이펙트 시각. dashFrom · dashTo 는 달려가는 고학년이면 처음 자리 · 부딪치는 자리
        public static FxSync Sync(ActPlan plan, Vector3? dashFrom = null, Vector3? dashTo = null)
        {
            if (plan == null) return null;
            return new FxSync
            {
                Anim = plan.Anim, Tier = plan.Motion?.Tier, Group = plan.Group,
                Impact = plan.S?.At, End = plan.S?.End, Marks = plan.S?.Marks, Total = plan.S?.Total ?? 0,
                Pick = plan.Pick, Dash = plan.Dash != null || (plan.Table != null && plan.Table.Moves), DashFrom = dashFrom, DashTo = dashTo, Snd = plan.Snd,
            };
        }
    }
}
