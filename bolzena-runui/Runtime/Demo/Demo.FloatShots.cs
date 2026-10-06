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

        // 창 크기 바꾸기 점검(-demo-resize, 2026-10-07 「전체 화면으로 바꾸니 에르핀이 화면을 꽉 채운다」) — -resize-to WxH(기본 3440x1440).
        //   웹: 「[Resize] want WxH」 줄을 찍으면 하네스(run_lobby.mjs)가 창을 바꾼다 · PC: 창 모드 Screen.SetResolution.
        //   ① 처음 크기 로비(에르핀) → ② 바꾼 뒤 로비 → ③ 바꾼 뒤 사도 바꾸기(벨라 → 에르핀, 저장은 되돌림) → ④ 바꾼 크기에서 떠 있는 사도 + 에르핀 로비 · 상세
        //   → ⑤ 상세(에르핀)를 연 채로 처음 크기 ↔ 바꾼 크기. -norefit 이면 다시 맞추기 없이(전).
        static readonly string[] ResizeArts = { "erpin", "vela", "jubee", "ayla", "daya", "erpinroyale" };

        IEnumerator Resize_()
        {
            string tag = Stage.NoRefit ? "before" : "after";
            var to = (Arg("-resize-to") ?? "3440x1440").Split('x');
            int w1 = int.Parse(to[0]), h1 = int.Parse(to[1]);
            int w0 = Screen.width, h0 = Screen.height;
            var log = new System.Text.StringBuilder($"[ResizeShots] {tag} — 처음 {w0}x{h0} → {w1}x{h1} · 사도 · 얼굴 · 메시 아래/위(화면 높이 몫) · 칸 바닥 · 배율\n");
            string saved = Settings.LobbyHero;
            yield return Wait(0.5f);
            Flow.DemoLobbyHero = null;
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            log.Append($"  ① 처음\tlobby\t{Span("stand")}\t캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0}\n");
            yield return Shot($"r1_lobby_first_{tag}");
            yield return ResizeTo(w1, h1);
            log.Append($"  ② 바꾼 뒤\tlobby\t{Span("stand")}\t캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0}\n");
            yield return Shot($"r2_lobby_resized_{tag}");
            // ③ 사도 바꾸기(고르기 창 → 벨라 → 에르핀) — 저장은 끝에 되돌린다
            foreach (var k in new[] { "벨라", "에르핀" })
            {
                yield return Press("lobby.swap", 1.0f);
                var fld = f.DexSearchField;
                if (fld != null) { fld.text = k; yield return Wait(0.4f); }
                yield return Press("lobbyhero:" + k, 1.4f);
                log.Append($"  ③ 바꾼 뒤 사도 바꾸기\t{k}\t{Span("stand")}\n");
                yield return Shot($"r3_lobby_swap_{k}_{tag}");
            }
            Settings.LobbyHero = saved; PlayerPrefs.Save();
            // ④ 바꾼 크기 — 떠 있는 사도 로비 · 상세
            foreach (var a in ResizeArts)
            {
                var h = Roster.All.FirstOrDefault(x => x.art == a);
                if (h == null) { log.Append($"  ④ {a} 없음\n"); continue; }
                Flow.DemoLobbyHero = h.key;
                f.Lobby();
                yield return Screen_("lobby", 1.6f);
                log.Append($"  ④ lobby\t{a}\t{Span("stand")}\n");
                yield return Shot($"r4_lobby_{a}_{tag}");
            }
            Flow.DemoLobbyHero = null;
            f.Lobby();
            yield return Screen_("lobby", 0.6f);
            foreach (var a in ResizeArts)
            {
                var h = Roster.All.FirstOrDefault(x => x.art == a);
                if (h == null) continue;
                CloseModals();
                f.HeroDetail(h.key, null, null);
                yield return Wait(1.6f);
                log.Append($"  ④ detail\t{a}\t{Span("figure")}\n");
                yield return Shot($"r4_detail_{a}_{tag}");
            }
            // ⑤ 상세(벨라)를 연 채로 처음 크기 → 바꾼 크기
            CloseModals();
            var vh = Roster.All.FirstOrDefault(x => x.art == "vela");
            if (vh != null)
            {
                f.HeroDetail(vh.key, null, null);
                yield return Wait(1.2f);
                yield return ResizeTo(w0, h0);
                log.Append($"  ⑤ detail → 처음 크기\tvela\t{Span("figure")}\n");
                yield return Shot($"r5_detail_vela_first_{tag}");
                yield return ResizeTo(w1, h1);
                log.Append($"  ⑤ detail → 바꾼 크기\tvela\t{Span("figure")}\n");
                yield return Shot($"r5_detail_vela_resized_{tag}");
            }
            CloseModals();
            // ⑥ 마을 공개 · 편성 — 바꾼 크기에서 세우고 처음 크기 ↔ 바꾼 크기(다시 맞추기 · 예외 없음 확인)
            var vid = f.P.Data.Villages.Keys.FirstOrDefault();
            if (vid != null)
            {
                f.VillageReveal(vid, () => { });
                yield return Screen_("village", 1.4f);
                yield return Shot($"r6_village_resized_{tag}");
                yield return ResizeTo(w0, h0);
                yield return Shot($"r6_village_first_{tag}");
                yield return ResizeTo(w1, h1);
                f.Party(vid);
                yield return Screen_("party", 1.6f);
                yield return Shot($"r6_party_resized_{tag}");
                yield return ResizeTo(w0, h0);
                yield return Shot($"r6_party_first_{tag}");
                yield return ResizeTo(w1, h1);
                yield return Shot($"r6_party_back_{tag}");
                log.Append($"  ⑥ 마을 공개 · 편성 — 캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0}\n");
            }
            Debug.Log(log.ToString());
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        /// <summary>창을 w×h 로 바꾸고(웹은 하네스에 부탁) 캔버스가 바뀌어 다시 맞추기가 끝날 때까지 기다린다.</summary>
        IEnumerator ResizeTo(int w, int h)
        {
            int before = f.Stage.Refits;
            if (DisplayOptions.Web) Debug.Log($"[Resize] want {w}x{h}");
            else Screen.SetResolution(w, h, FullScreenMode.Windowed);
            float t = 0;
            while (f.Stage.Refits == before && t < 8) { t += Time.unscaledDeltaTime; yield return null; }
            Debug.Log($"[Resize] 바뀜 {Screen.width}x{Screen.height} · 캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0} · {t:0.0}s");
            yield return Wait(0.8f);
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
            var art = g.name.StartsWith("standing ") ? g.name.Substring(9) : FloatArts.FirstOrDefault(x => g.skeletonDataAsset != null && g.skeletonDataAsset.name.Contains(x));
            if (art != null && StandingFit.TryGet(art, out var fit))
            {
                float fy = fit.IconFaceY != 0 ? fit.IconFaceY : fit.CardFaceY != 0 ? fit.CardFaceY : fit.FaceY != 0 ? fit.FaceY : fit.FaceBox.center.y;
                float u = (g.skeletonDataAsset.scale) * g.MeshScale;
                face = $"{Y(new Vector3(fit.FaceCx * u, fy * u, 0)):0.000}";
            }
            float unit = g.skeletonDataAsset != null ? g.skeletonDataAsset.scale * g.MeshScale : 1;   // 원본 단위 1 = 메시 로컬 몇 단위
            float scale = Mathf.Abs(root.InverseTransformVector(g.rectTransform.TransformVector(new Vector3(0, unit, 0))).y) / H * 1000;   // 원본 1000 단위 = 화면 높이의 몇 배
            return $"얼굴 {face} · 아래 {Y(b.min):0.00} · 위 {Y(b.max):0.00} · 칸 바닥 {floor:0.00} · 배율 {scale:0.000}";
        }
    }
}
