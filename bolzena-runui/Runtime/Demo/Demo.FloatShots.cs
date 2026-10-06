using System.Collections;
using System.Linq;
using Spine.Unity;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 떠 있는 사도 로비 · 사도 상세 캡처(-demo-floatshots, 2026-10-07 「공중에 뜬 만큼 다시 내려서 다른 사도와 얼굴 위치를 같게」).
    //   쥬비 · 벨라 · 아일라 + 보통 사도 에르핀을 저장(PlayerPrefs)을 건드리지 않고(Flow.DemoLobbyHero) 로비 · 사도 상세에 세워 찍는다. -nolift 면 맞춤 없이(전).
    //   이벤트는 사도가 전투 SD 로 서서(겨우살이만 스탠딩) 대상이 아니다.
    //   [FloatShots] 줄 — 얼굴 · 그린 메시 아래 끝 · 위 끝(화면 높이 몫)과 칸 바닥(화면 높이 몫).
    public partial class Demo
    {
        static readonly string[] FloatArts = { "jubee", "vela", "ayla", "erpin" };   // 에르핀 = 보통 사도(얼굴 높이 비교)

        IEnumerator FloatShots_()
        {
            string tag = StandingFit.NoLift ? "before" : "after";
            var log = new System.Text.StringBuilder($"[FloatShots] {tag} — 화면 · 사도 · 메시 아래/위(화면 높이 몫) · 칸 바닥\n");
            yield return Wait(0.5f);
            foreach (var a in FloatArts)
            {
                var h = Roster.All.FirstOrDefault(x => x.art == a);
                if (h == null) continue;
                Flow.DemoLobbyHero = h.key;
                f.Lobby();
                yield return Screen_("lobby", 1.6f);
                log.Append($"  lobby\t{a}\t{Span("stand")}\n");
                yield return Shot($"lobby_{a}_{tag}");
            }
            Flow.DemoLobbyHero = null;
            f.Lobby();
            yield return Screen_("lobby", 0.6f);
            foreach (var a in FloatArts)
            {
                var h = Roster.All.FirstOrDefault(x => x.art == a);
                if (h == null) continue;
                CloseModals();
                f.HeroDetail(h.key, null, null);
                yield return Wait(1.6f);
                log.Append($"  detail\t{a}\t{Span("figure")}\n");
                yield return Shot($"detail_{a}_{tag}");
            }
            CloseModals();
            Debug.Log(log.ToString());
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        /// <summary>이름이 area 인 칸 안 스탠딩 메시의 아래 · 위 끝과 칸 바닥(화면 높이 몫).</summary>
        string Span(string area)
        {
            var root = f.Stage.Root;
            var box = root.GetComponentsInChildren<RectTransform>().LastOrDefault(t => t.name == area);
            var g = box != null ? box.GetComponentInChildren<SkeletonGraphic>() : null;
            if (g == null) return "스파인 없음";
            var mesh = g.GetLastMesh();
            if (mesh == null || mesh.vertexCount == 0) return "메시 없음";
            var b = mesh.bounds;
            float H = root.rect.height;
            float Y(Vector3 l) => (root.InverseTransformPoint(g.rectTransform.TransformPoint(l)).y - root.rect.yMin) / H;
            float floor = (root.InverseTransformPoint(box.TransformPoint(box.rect.min)).y - root.rect.yMin) / H;
            string face = "-";
            var art = FloatArts.FirstOrDefault(x => g.skeletonDataAsset != null && g.skeletonDataAsset.name.Contains(x));
            if (art != null && StandingFit.TryGet(art, out var fit))
            {
                float fy = fit.IconFaceY != 0 ? fit.IconFaceY : fit.CardFaceY != 0 ? fit.CardFaceY : fit.FaceY != 0 ? fit.FaceY : fit.FaceBox.center.y;
                float u = (g.skeletonDataAsset.scale) * g.MeshScale;
                face = $"{Y(new Vector3(fit.FaceCx * u, fy * u, 0)):0.000}";
            }
            return $"얼굴 {face} · 아래 {Y(b.min):0.00} · 위 {Y(b.max):0.00} · 칸 바닥 {floor:0.00}";
        }
    }
}
