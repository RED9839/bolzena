using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 사도 목록 무게 재기(-demo-listperf, 2026-10-07 「사도 목록 보는 게 너무 무겁다」) — 화면을 처음 열 때부터 보이는 칸 그림이 다 찰 때까지의
    //   걸린 시간 · 최장 프레임 · 50ms 넘은 프레임 수, 끝까지 일정한 속도로 내릴 때의 프레임(평균 · 95% · 최장)을 [ListPerf] 줄로 남긴다.
    //   차례: 도감 사도 목록(처음 — 차가운 상태) → 끝까지 내리기 → 상세(왼쪽 초상 줄) → 로비 메인 사도 고르기.
    //   -nosnap 을 함께 주면 미리 구운 그림을 쓰지 않는다(예전 그대로 — 전후 비교).
    public partial class Demo
    {
        static readonly Vector3[] lpC = new Vector3[4];
        static Rect LpWorld(RectTransform rt) { rt.GetWorldCorners(lpC); return Rect.MinMaxRect(lpC[0].x, lpC[0].y, lpC[2].x, lpC[2].y); }

        // 보이는 칸(뷰포트에 걸친 칸)의 그림이 다 찼는가 — 굽기 대기열까지 비어야
        static bool LpVisibleReady(ScrollRect sr, out int shown, out int ready)
        {
            shown = ready = 0;
            var view = LpWorld(sr.viewport != null ? sr.viewport : (RectTransform)sr.transform);
            foreach (var ls in sr.content.GetComponentsInChildren<LiveStanding>())
            {
                var r = LpWorld((RectTransform)ls.transform);
                if (!(r.xMax > view.xMin && r.xMin < view.xMax && r.yMax > view.yMin && r.yMin < view.yMax)) continue;
                shown++;
                if (ls.Tried && (ls.Still == null || ls.Still.sprite != null)) ready++;
            }
            return shown > 0 && ready == shown && StandingSnap.Pending == 0;
        }

        sealed class LpStat
        {
            readonly List<float> dts = new List<float>();
            public void Add(float dt) => dts.Add(dt * 1000f);
            public int N => dts.Count;
            public float Max => dts.Count > 0 ? dts.Max() : 0;
            public float Avg => dts.Count > 0 ? dts.Average() : 0;
            public float P95 { get { if (dts.Count == 0) return 0; var s = dts.OrderBy(x => x).ToList(); return s[Mathf.Clamp(Mathf.CeilToInt(s.Count * 0.95f) - 1, 0, s.Count - 1)]; } }
            public int Over(float ms) => dts.Count(x => x > ms);
            public override string ToString() => $"frames={N} avg={Avg:F1}ms p95={P95:F1}ms max={Max:F0}ms over50={Over(50)} over100={Over(100)}";
        }

        /// <summary>목록을 연 순간부터 보이는 칸이 다 찰 때까지 — 걸린 시간 · 프레임.</summary>
        IEnumerator LpUntilReady(string tag, System.Func<ScrollRect> find, float t0, float timeout = 30)
        {
            var st = new LpStat();
            ScrollRect sr = null;
            int shown = 0, ready = 0;
            float firstPic = -1;
            while (Time.realtimeSinceStartup - t0 < timeout)
            {
                yield return null;
                st.Add(Time.unscaledDeltaTime);
                if (sr == null) sr = find();
                if (sr == null) continue;
                bool ok = LpVisibleReady(sr, out shown, out ready);
                if (firstPic < 0 && ready > 0) firstPic = Time.realtimeSinceStartup - t0;
                if (ok) break;
            }
            float ms = (Time.realtimeSinceStartup - t0) * 1000f;
            Debug.Log($"[ListPerf] {tag} open→ready={ms:F0}ms firstPic={firstPic * 1000f:F0}ms visible={ready}/{shown} {st} baked={StandingSnap.Baked} prebaked={StandingSnap.Prebaked}");
        }

        /// <summary>끝까지 일정한 속도(초당 speed 단위)로 내리고 다시 위로 — 프레임.</summary>
        IEnumerator LpScroll(string tag, ScrollRect sr, float speed = 1400f)
        {
            if (sr == null) yield break;
            Canvas.ForceUpdateCanvases();
            float hmax = Mathf.Max(0, sr.content.rect.height - ((RectTransform)sr.viewport).rect.height);
            var st = new LpStat();
            int baked0 = StandingSnap.Baked, pre0 = StandingSnap.Prebaked;
            float y = 0;
            sr.StopMovement();
            while (y < hmax)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                st.Add(dt);
                y = Mathf.Min(hmax, y + speed * Mathf.Min(dt, 0.1f));
                sr.content.anchoredPosition = new Vector2(sr.content.anchoredPosition.x, y);
            }
            float t0 = Time.realtimeSinceStartup;
            int shown = 0, ready = 0;
            while (Time.realtimeSinceStartup - t0 < 20 && !LpVisibleReady(sr, out shown, out ready)) { yield return null; st.Add(Time.unscaledDeltaTime); }
            Debug.Log($"[ListPerf] {tag} scroll h={hmax:F0} {st} tail={(Time.realtimeSinceStartup - t0) * 1000f:F0}ms baked+={StandingSnap.Baked - baked0} prebaked+={StandingSnap.Prebaked - pre0}");
        }

        IEnumerator ListPerf_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 2.5f);
            MemLog.Log("listperf_lobby");
            bool shots = !Has("-noshots");

            // 1) 도감 사도 목록 — 처음 열기
            float t0 = Time.realtimeSinceStartup;
            f.Dex(f.Lobby);
            yield return LpUntilReady("dex_list", () => f.Stage.ScreenLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.content != null && s.content.GetComponent<GridLayoutGroup>() != null), t0);
            MemLog.Log("listperf_dex_ready");
            if (shots) yield return Shot("listperf_dex");
            var sr = f.Stage.ScreenLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.content != null && s.content.GetComponent<GridLayoutGroup>() != null);
            yield return Wait(0.5f);
            yield return LpScroll("dex_list", sr);
            MemLog.Log("listperf_dex_scrolled");
            if (shots) yield return Shot("listperf_dex_end");
            if (sr != null) sr.content.anchoredPosition = new Vector2(sr.content.anchoredPosition.x, 0);
            yield return Wait(0.8f);

            // 2) 상세 — 왼쪽 초상 줄(얼굴 칸 135)
            var first = Roster.All.Where(h => h.art != null).Select(h => h.key).FirstOrDefault(k => Hot("hero:" + k) != null);
            if (first != null)
            {
                t0 = Time.realtimeSinceStartup;
                Debug.Log("[Demo] 누름 hero:" + first);
                StartCoroutine(Hot("hero:" + first).Press());
                yield return LpUntilReady("detail_rail", () => f.Stage.ModalLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.transform.name == "faces" || s.transform.parent?.name == "faces" || s.GetComponentInParent<RectTransform>()?.name == "faces"), t0);
                MemLog.Log("listperf_detail");
                if (shots) yield return Shot("listperf_detail");
                var rail = f.Stage.ModalLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.transform.name == "faces" || s.transform.parent?.name == "faces");
                yield return LpScroll("detail_rail", rail, 1200f);
                yield return Press("detail.close", 1.0f);
            }

            // 3) 로비 메인 사도 고르기(같은 목록 카드 — 이미 구운 그림은 다시 쓴다)
            f.Lobby();
            yield return Screen_("lobby", 1.5f);
            if (Hot("lobby.swap") != null)
            {
                t0 = Time.realtimeSinceStartup;
                Debug.Log("[Demo] 누름 lobby.swap");
                StartCoroutine(Hot("lobby.swap").Press());
                yield return LpUntilReady("lobby_picker", () => f.Stage.ModalLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.content != null && s.content.GetComponent<GridLayoutGroup>() != null), t0);
                MemLog.Log("listperf_lobby_picker");
                if (shots) yield return Shot("listperf_lobby_picker");
                var lsr = f.Stage.ModalLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.content != null && s.content.GetComponent<GridLayoutGroup>() != null);
                yield return LpScroll("lobby_picker", lsr);
            }
            MemLog.Log("listperf_end");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.5f);
            Application.Quit(0);
        }
    }
}
