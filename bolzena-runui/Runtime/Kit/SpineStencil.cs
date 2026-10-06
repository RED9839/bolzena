using System.Collections.Generic;
using Spine;
using Spine.Unity;

namespace Bolzena.RunUI
{
    // 원작 스텐실 가리기 → 스파인 클리핑 — 미로(거울) · 셰이디(역전) SD 는 원작 게임의 스텐실 표시 슬롯(MaskStart · MaskStartNext · MaskEnd · MaskEndNext,
    // 첨부 「stencil」 메시)로 「거울 안에서만 보이기」를 한다. 우리 렌더러는 그 표시를 모르니(그냥 메시로 그리거나 무시) 거울 밖으로 몸이 삐져나왔다(2026-10-06 사용자).
    // 스파인 표준 ClippingAttachment 로 바꿔 끼운다: MaskStart* 의 stencil 메시 외곽선 = 가리기 모양, 끝 = 짝 MaskEnd*. 그리는 차례 타임라인이 이 표시들 사이에
    // 몸 슬롯을 넣고 빼므로 모든 동작(쉬기 · 공격 · 스킬 · 고학년 · 피격)에 같이 먹는다. MaskEnd* 의 stencil 은 그리지 않게 비운다.
    // SkeletonData 는 SkeletonDataAsset 이 붙들고 있어 한 번 고치면 전투 · 판 화면이 함께 쓴다.
    public static class SpineStencil
    {
        // 고친 SkeletonData 표시 — 약한 참조(ConditionalWeakTable). 예전엔 HashSet 이 한 번 고친 SkeletonData 를 모두 붙들어
        //   스탠딩 · 전투 스파인 데이터가 화면을 떠나도 영영 풀리지 않았다(2026-10-06 웹 OOM — 도감 사도 목록 → 상세)
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SkeletonData, object> done = new System.Runtime.CompilerServices.ConditionalWeakTable<SkeletonData, object>();
        static readonly object mark = new object();

        public static SkeletonDataAsset Fix(SkeletonDataAsset a)
        {
            if (a != null) { try { Fix(a.GetSkeletonData(true)); } catch { } }
            return a;
        }

        public static void Fix(SkeletonData d)
        {
            if (d == null || done.TryGetValue(d, out _)) return;
            done.Add(d, mark);
            var skin = d.DefaultSkin;
            if (skin == null) return;
            foreach (var pair in new[] { ("MaskStart", "MaskEnd"), ("MaskStartNext", "MaskEndNext") })
            {
                var start = d.FindSlot(pair.Item1);
                var end = d.FindSlot(pair.Item2);
                if (start == null || end == null) continue;
                if (!(skin.GetAttachment(start.Index, "stencil") is MeshAttachment m)) continue;
                var clip = new ClippingAttachment("stencil") { EndSlot = end };
                CopyHull(m, clip);
                skin.SetAttachment(start.Index, "stencil", clip);
                skin.RemoveAttachment(end.Index, "stencil");   // 끝 표시는 그리지 않는다
            }
        }

        // 메시 외곽(HullLength) 꼭짓점만 차례대로 — 클리핑은 다각형 하나
        static void CopyHull(MeshAttachment m, ClippingAttachment c)
        {
            int hull = m.HullLength / 2;
            if (m.Bones == null)
            {
                var v = new float[hull * 2];
                System.Array.Copy(m.Vertices, v, hull * 2);
                c.Vertices = v;
                c.WorldVerticesLength = hull * 2;
                return;
            }
            var bones = new List<int>(); var verts = new List<float>();
            int bi = 0, vi = 0;
            for (int i = 0; i < m.WorldVerticesLength / 2; i++)
            {
                int n = m.Bones[bi];
                if (i < hull)
                {
                    bones.Add(n);
                    for (int k = 0; k < n; k++) { bones.Add(m.Bones[bi + 1 + k]); verts.Add(m.Vertices[vi + k * 3]); verts.Add(m.Vertices[vi + k * 3 + 1]); verts.Add(m.Vertices[vi + k * 3 + 2]); }
                }
                bi += n + 1; vi += n * 3;
            }
            c.Bones = bones.ToArray();
            c.Vertices = verts.ToArray();
            c.WorldVerticesLength = hull * 2;
        }
    }
}
