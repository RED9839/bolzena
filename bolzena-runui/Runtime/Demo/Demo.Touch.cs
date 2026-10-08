using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bolzena.RunUI
{
    // 로비 사도 만지기 캡처(-demo-touch) — 사도 몇(기본 에르핀 · 버터 · 앨리스, -touch-heroes erpin,butter,alice 로 그림 키)마다
    //   판정 자리(반투명) → 볼 당기는 중 → 더 멀리(상한) → 놓은 직후(튕김) → 간지럼 → 연속 볼 당기기 5번(화냄)을 찍는다.
    //   실제 OS 마우스 대신 가짜 포인터 이벤트를 그 화면 자리의 맨 위 UI 로 보낸다 — 맨 위가 사도 판(LobbyTouch)인지도 단언(단추와 겹침 확인).
    //   저장 · 설정은 건드리지 않는다(메인 사도는 Flow.DemoLobbyHero 로만).
    public partial class Demo
    {
        IEnumerator Touch_()
        {
            yield return Wait(0.5f);
            var arts = (Arg("-touch-heroes") ?? "erpin,butter,alice").Split(',');
            foreach (var art in arts)
            {
                var h = Roster.All.FirstOrDefault(x => x.art == art);
                if (h == null || !Flow.CanLobby(h)) { Expect(false, $"사도 {art} 없음"); continue; }
                Flow.DemoLobbyHero = h.key;
                // ① 판정 자리
                LobbyTouch.ShowZones = true;
                f.Lobby();
                yield return Screen_("lobby", 1.2f);
                yield return Shot($"touch_{art}_zones");
                LobbyTouch.ShowZones = false;
                f.Lobby();
                yield return Screen_("lobby", 1.2f);
                var t = Flow.LobbyTouchNow;
                if (t == null || t.Sg == null) { Expect(false, $"{art} 만지기 부품 없음"); continue; }
                float u = t.ScreenPerU();
                Debug.Log($"[Touch] {art} 얼굴 단위 {u:0.0}px · 볼 {t.ScreenOf("cheek")} · 몸 {t.ScreenOf("body")} · 화면 {Screen.width}×{Screen.height}");
                // 사도별 대사(hero_lines.json) — 로비 첫 줄 · 머리 쓰다듬기
                Debug.Log($"[Touch] {art} 대사 표 {(HeroLines.Has(h) ? "있음" : "없음")} · 말 단계 {HeroLines.Reg(h) ?? "-"} · 로비 「{HeroLines.Of(h, "lobby")[0]}」");
                Expect(HeroLines.Has(h), $"{art} 사도별 대사 있음");
                yield return Shot($"touch_{art}_lobby");
                // 꿀밤 — 정수리를 톡(누르고 바로 뗌)
                var hp = t.ScreenOf("head");
                Expect(TopAt(hp) == t.gameObject, $"{art} 머리 자리 맨 위 = 사도 판({TopAt(hp)?.name})");
                var kv = Down(hp); yield return Wait(0.05f); Up(kv);
                yield return Wait(0.3f);
                Expect(t.Last == "knock", $"{art} 머리 톡 = 꿀밤({t.Last})");
                yield return Shot($"touch_{art}_knock");
                yield return Wait(1.6f);
                // 쓰다듬기 — 머리를 누른 채 좌우로 끈다
                var pv = Down(hp);
                for (int i = 0; i < 4; i++) yield return DragTo(pv, pv.position, hp + new Vector2((i % 2 == 0 ? 1 : -1) * 0.8f * u, 0), 0.15f);
                yield return Wait(0.2f);
                Expect(t.Last == "pat", $"{art} 머리를 끌면 쓰다듬기({t.Last})");
                yield return Shot($"touch_{art}_patting");
                Up(pv);
                yield return Wait(0.35f);
                Expect(t.Last == "pat_end", $"{art} 떼면 쓰다듬기 끝({t.Last})");
                yield return Shot($"touch_{art}_pat");
                yield return Wait(4.5f);   // 꿀밤 짜증이 풀리게

                // 단추와 겹침 — 볼 · 몸 자리의 맨 위 UI 가 사도 판인지, 바꾸기 단추 자리는 단추가 받는지
                Expect(TopAt(t.ScreenOf("cheek")) == t.gameObject, $"{art} 볼 자리 맨 위 = 사도 판({TopAt(t.ScreenOf("cheek"))?.name})");
                Expect(TopAt(t.ScreenOf("body")) == t.gameObject, $"{art} 몸 자리 맨 위 = 사도 판({TopAt(t.ScreenOf("body"))?.name})");
                var swap = Hot("lobby.swap");
                if (swap != null) { var sp = RectTransformUtility.WorldToScreenPoint(null, swap.transform.position); Expect(TopAt(sp) != t.gameObject, $"바꾸기 단추 자리는 단추가 받음({TopAt(sp)?.name})"); }

                // ② 볼 당기기 — 볼 장치 쪽 볼을 잡아 얼굴 바깥 · 살짝 아래로
                float sd = t.Side;
                var from = t.ScreenOf("cheek", -0.3f, -0.1f);
                var ev = Down(from);
                yield return DragTo(ev, from, from + new Vector2(sd * 1.6f * u, -0.3f * u), 0.35f);
                yield return Wait(0.25f);
                float pull1 = t.PullPx;
                yield return Shot($"touch_{art}_pull");
                yield return DragTo(ev, ev.position, from + new Vector2(sd * 5f * u, -1f * u), 0.35f);
                yield return Wait(0.25f);
                float pull2 = t.PullPx;
                yield return Shot($"touch_{art}_pullfar");
                Debug.Log($"[Touch] {art} 볼 늘어남 {pull1:0}px(1.6u 끔) → {pull2:0}px(5u 끔) · {t.Last}");
                Expect(t.Last == "cheek" && pull1 > 4, $"{art} 볼이 끌림({pull1:0}px)");
                Expect(pull2 > pull1 && pull2 < pull1 * 2.6f, $"{art} 멀리 끌어도 상한({pull1:0} → {pull2:0}px)");
                Up(ev);
                yield return Wait(0.09f);
                yield return Shot($"touch_{art}_release");
                Debug.Log($"[Touch] {art} 놓은 직후 {t.PullPx:0}px · {t.Last}");
                yield return Wait(1.0f);
                Expect(t.PullPx < 1f, $"{art} 놓으면 제자리({t.PullPx:0.0}px)");
                yield return Shot($"touch_{art}_after");
                yield return Wait(1.6f);

                // ③ 간지럽히기 — 몸통을 좌우로 빠르게 오간다
                var c = t.ScreenOf("body");
                var tv = Down(c);
                for (int i = 0; i < 7; i++)
                {
                    float dir = i % 2 == 0 ? 1 : -1;
                    yield return DragTo(tv, tv.position, c + new Vector2(dir * 0.9f * u, (i % 3 - 1) * 0.15f * u), 0.09f);
                }
                yield return Wait(0.35f);
                Expect(t.Last == "tickle", $"{art} 문지르면 간지럼({t.Last})");
                yield return Shot($"touch_{art}_tickle");
                Up(tv);
                yield return Wait(0.4f);
                Expect(t.Last == "tickle_end", $"{art} 떼면 간지럼 끝({t.Last})");
                yield return Wait(3.5f);   // 짜증이 조금 풀리게

                // ④ 연속 볼 당기기 — 다섯 번이면 화낸다
                string lastBefore = null;
                for (int n = 0; n < 6 && t.Last != "angry"; n++)
                {
                    var p0 = t.ScreenOf("cheek", -0.2f, 0);
                    var e2 = Down(p0);
                    yield return DragTo(e2, p0, p0 + new Vector2(sd * 1.4f * u, 0.2f * u), 0.15f);
                    yield return Wait(0.1f);
                    Up(e2);
                    lastBefore = t.Last;
                    Debug.Log($"[Touch] {art} 연속 {n + 1}번째 → {t.Last} (짜증 {t.Annoy:0.0})");
                    if (t.Last == "angry") break;
                    yield return Wait(0.35f);
                }
                Expect(t.Last == "angry", $"{art} 연속으로 당기면 화냄({t.Last})");
                yield return Wait(0.45f);
                yield return Shot($"touch_{art}_angry");
                // 삐진 동안은 받지 않는다
                var p1 = t.ScreenOf("cheek");
                var e3 = Down(p1); yield return DragTo(e3, p1, p1 + new Vector2(sd * u, 0), 0.1f); Up(e3);
                Expect(t.Last == "angry", $"{art} 삐진 동안은 볼을 못 당김({t.Last})");
                yield return Wait(0.6f);
                yield return Shot($"touch_{art}_sulk");
            }
            Flow.DemoLobbyHero = null;
            Debug.Log($"[Demo] 만지기 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        // ── 가짜 포인터 ──
        readonly List<RaycastResult> rays = new List<RaycastResult>();
        GameObject TopAt(Vector2 pos)
        {
            var es = EventSystem.current;
            if (es == null) return null;
            rays.Clear();
            es.RaycastAll(new PointerEventData(es) { position = pos }, rays);
            return rays.Count > 0 ? ExecuteEvents.GetEventHandler<IPointerDownHandler>(rays[0].gameObject) : null;
        }

        PointerEventData Down(Vector2 pos)
        {
            var es = EventSystem.current;
            var e = new PointerEventData(es) { pointerId = 11, position = pos, pressPosition = pos, button = PointerEventData.InputButton.Left };
            rays.Clear();
            es.RaycastAll(e, rays);
            if (rays.Count == 0) { Debug.LogWarning($"[Touch] {pos} 에 받는 UI 없음"); return e; }
            e.pointerPressRaycast = e.pointerCurrentRaycast = rays[0];
            var target = ExecuteEvents.GetEventHandler<IPointerDownHandler>(rays[0].gameObject);
            e.pointerPress = target; e.rawPointerPress = rays[0].gameObject;
            e.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(rays[0].gameObject);
            ExecuteEvents.Execute(target, e, ExecuteEvents.pointerDownHandler);
            if (e.pointerDrag != null) ExecuteEvents.Execute(e.pointerDrag, e, ExecuteEvents.initializePotentialDrag);
            return e;
        }

        IEnumerator DragTo(PointerEventData e, Vector2 a, Vector2 b, float dur)
        {
            float t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var p = Vector2.Lerp(a, b, Mathf.Clamp01(t / dur));
                e.delta = p - e.position; e.position = p;
                if (e.pointerDrag != null) ExecuteEvents.Execute(e.pointerDrag, e, ExecuteEvents.dragHandler);
                yield return null;
            }
        }

        void Up(PointerEventData e)
        {
            if (e.pointerPress != null) ExecuteEvents.Execute(e.pointerPress, e, ExecuteEvents.pointerUpHandler);
        }
    }
}
