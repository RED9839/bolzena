using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 화면비 점검(-demo-aspect, 2026-10-12 「화면비마다 사도 배치」) — 창 크기 하나로 사도가 보이는 화면을 차례로 찍는다.
    //   로비(-aspect-lobby 키,키 — 체형이 다른 메인 사도) → 마을 → 편성(-aspect-party 키,키,키) → 지도 → 휴식 → 이벤트(대상 사도 있는 것)
    //   → 싸움(시작 · 몇 초 뒤) → 보상 → 끝(패배).
    //   -aspect-resize WxH: 화면마다 한 번 그 크기로 바꿔 찍고 처음 크기로 되돌린다(Stage.WhenResized 다시 맞추기 점검).
    //   [Aspect] 줄 — 화면 · 캔버스 크기.
    public partial class Demo
    {
        int[] aspectTo;

        IEnumerator Aspect_()
        {
            var rs = Arg("-aspect-resize")?.Split('x');
            if (rs != null && rs.Length == 2 && int.TryParse(rs[0], out var rw) && int.TryParse(rs[1], out var rh)) aspectTo = new[] { rw, rh };
            var lobbyKeys = (Arg("-aspect-lobby") ?? "에르핀_왕도").Split(',');
            var partyKeys = (Arg("-aspect-party") ?? "").Split(',').Where(k => k.Length > 0).ToList();
            Debug.Log($"[Aspect] 창 {Screen.width}x{Screen.height} · 캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0}");
            yield return Wait(0.5f);

            // ① 로비 — 메인 사도마다
            foreach (var k in lobbyKeys)
            {
                if (Roster.ByKey(k) == null) { Debug.LogWarning("[Aspect] 로비 사도 없음: " + k); continue; }
                Flow.DemoLobbyHero = k;
                f.Lobby();
                yield return Screen_("lobby", 1.6f);
                yield return AspectShot("lobby_" + k);
            }
            Flow.DemoLobbyHero = lobbyKeys[0];
            f.Lobby();
            yield return Screen_("lobby", 0.8f);

            // ② 마을 → 편성
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.6f);
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Press("slot0", 0.6f);
            var pick = partyKeys.Count == 3 ? partyKeys : Roster.Players.Take(3).Select(h => h.key).ToList();
            foreach (var k in pick) yield return Press("hero:" + k, 0.3f);
            yield return Press("list.done", 1.0f);
            yield return AspectShot("party");
            Flow.DemoLobbyHero = null;

            // ③ 지도
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            yield return AspectShot("map");

            // ④ 휴식
            f.P.S.Camp = null;
            f.Camp("camp");
            yield return Screen_("camp", 1.6f);
            yield return AspectShot("camp");

            // ⑤ 이벤트 — 대상 사도(Npc)가 있는 첫 이벤트
            var ev = f.P.Data.Events.FirstOrDefault(e => e.Npc != null && Roster.All.Any(h => h.ko == e.Npc)) ?? f.P.Data.Events.FirstOrDefault();
            if (ev != null)
            {
                string key = $"{f.P.S.Floor}:{(f.P.S.Map?.At ?? f.P.S.Node.ToString())}";
                f.P.S.Event = new Core.EventState { Key = key, Choices = new List<string> { ev.Id }, Id = ev.Id };
                f.EventStop();
                yield return Screen_("event", 1.8f);
                yield return AspectShot("event_" + ev.Id);
                f.P.S.Event = null;
            }

            // ⑥ 싸움 — 지도에서 싸움 칸
            f.MapStep();
            yield return Screen_("map", 1.2f);
            var reach = f.P.Reachable();
            var map = f.P.Map();
            var node = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "elite");
            if (node != null)
            {
                FightMode = "win";
                yield return Press("node:" + node, 0.2f);
                float t = 0;
                while (!f.Fighting && t < 20) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Fighting)
                {
                    yield return Wait(4.0f);
                    yield return AspectShot("battle");
                    yield return Wait(5.0f);
                    yield return Shot("battle_later");
                    t = 0;
                    while (f.Fighting && t < 300) { t += Time.unscaledDeltaTime; yield return null; }
                    yield return Screen_("reward", 1.8f);
                    if (f.Stage.Current == "reward") yield return AspectShot("reward");
                }
                else if (f.Stage.Current == "fight")
                {   // 전투 화면이 없는 판 화면 싸움(사도 셋을 세운 칸)
                    yield return Wait(1.2f);
                    yield return AspectShot("fight");
                }
            }
            else Debug.LogWarning("[Aspect] 싸움 칸 없음");

            // ⑦ 끝(패배)
            CloseModals();
            f.End("lose");
            yield return Wait(2.5f);
            yield return AspectShot("end_lose");

            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        /// <summary>찍고, -aspect-resize 가 있으면 그 크기로 바꿔 한 장 더 찍고 처음 크기로 되돌린다.</summary>
        IEnumerator AspectShot(string name)
        {
            Debug.Log($"[Aspect] {name} — 창 {Screen.width}x{Screen.height} · 캔버스 {f.Stage.Size.x:0}x{f.Stage.Size.y:0}");
            yield return Shot(name);
            if (aspectTo == null) yield break;
            int w0 = Screen.width, h0 = Screen.height;
            yield return ResizeTo(aspectTo[0], aspectTo[1]);
            yield return Wait(0.6f);
            yield return Shot(name + "_resized");
            yield return ResizeTo(w0, h0);
            yield return Wait(0.6f);
            yield return Shot(name + "_back");
        }
    }
}
