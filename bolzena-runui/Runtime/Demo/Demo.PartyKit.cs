using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bolzena.RunUI
{
    // 팀 편성 편의 캡처(-demo-partykit, 늘 -save partykit 처럼 따로 둔 저장으로) — 끌어서 자리 바꾸기 전 · 중 · 후 / 프리셋 창 / 시작 덱 목록 · 카드 상세 /
    //   보드 요약 줄(누르면 보드) · 완주 기록(칸 · 사도 목록) · 편성 목소리 호출 로그. 실제 OS 마우스는 쓰지 않는다(EventSystem 끌기 이벤트를 직접 보낸다).
    public partial class Demo
    {
        static Vector2 ScreenCenter(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return (c[0] + c[2]) * 0.5f; }

        IEnumerator DragSlot(int from, int to, string midShot)
        {
            var drags = Object.FindObjectsByType<Flow.PartySlotDrag>(FindObjectsSortMode.None);
            var d = drags.FirstOrDefault(x => x.Index == from);
            if (d == null) { Debug.LogWarning("[Demo] 끌 칸 없음 " + from); yield break; }
            var a = ScreenCenter(d.Slot); var b = ScreenCenter(d.Slots[to]);
            var go = d.gameObject;
            var ped = new PointerEventData(EventSystem.current) { position = a, pressPosition = a, button = PointerEventData.InputButton.Left };
            Debug.Log($"[Demo] 끌기 {from + 1} → {to + 1} (가짜 포인터 — 실제 마우스 아님)");
            ExecuteEvents.Execute(go, ped, ExecuteEvents.beginDragHandler);
            for (int i = 1; i <= 10; i++)
            {
                ped.position = Vector2.Lerp(a, b, i / 10f);
                ExecuteEvents.Execute(go, ped, ExecuteEvents.dragHandler);
                yield return null;
            }
            yield return Wait(0.25f);
            if (midShot != null) yield return Shot(midShot);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.endDragHandler);
            yield return Wait(0.8f);
        }

        IEnumerator PartyKit_()
        {
            var voice = new List<string>();
            HeroVoice.OnPlayed = s => voice.Add(s);
            PartyStore.ResetAll(); CrayonStore.Reset();
            // 보드 — 몇 칸 칠해 둔다(능력치 · 시작 골드 · 학점)
            CrayonStore.Give(60, 12, 4, 2);
            foreach (var (kind, n) in new[] { ("atk", 3), ("hp", 2), ("def", 1), ("crit", 2), ("gold", 1), ("credits", 1) })
            {
                var cell = CrayonStore.Table.Cells.FirstOrDefault(c => c.Kind == kind);
                for (int k = 0; cell != null && k < n; k++) CrayonStore.Paint(cell.Id);
            }
            var playable = Roster.All.Where(h => h.Playable).Select(h => h.key).ToList();
            var three = playable.Take(3).ToList();
            PartyStore.SetCleared(three[0], 3); PartyStore.SetCleared(three[1], 12); PartyStore.SetCleared(three[2], 5); PartyStore.SetCleared(playable[3], 1);
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Shot("pk_empty");
            yield return PickParty(1.0f);
            Expect(voice.Count >= 3 && voice.All(v => !v.StartsWith("!")), $"편성 목소리 — 사도를 넣을 때마다 재생 호출({string.Join(", ", voice)})");
            yield return Shot("pk_full");
            // 완주 기록이 사도 목록에도 — 칸을 눌러 목록을 열었다가 닫는다
            yield return Press("slot1", 1.2f);
            yield return Shot("pk_list_clears");
            yield return Press("list.done", 0.8f);
            // 끌어서 자리 바꾸기(1 ↔ 3)
            var before = f.PartySlotsNow.ToArray();
            yield return DragSlot(0, 2, "pk_drag_mid");
            var after = f.PartySlotsNow.ToArray();
            yield return Shot("pk_drag_after");
            Expect(after[0] == before[2] && after[2] == before[0] && after[1] == before[1], $"자리 바꿈 — {string.Join("/", before)} → {string.Join("/", after)}");
            // 제자리에 놓으면 그대로
            yield return DragSlot(1, 1, null);
            Expect(f.PartySlotsNow.SequenceEqual(after), "제자리에 놓으면 그대로");
            // 프리셋
            yield return Press("party.presets", 0.8f);
            yield return Shot("pk_presets_empty");
            yield return Press("preset.save1", 0.5f);
            yield return Press("preset.save3", 0.5f);
            yield return Press("preset.save5", 0.5f);
            // 성격 아이콘 — 단추를 눌러 붙이고, 이번 적의 약점과 같은 것은 강조
            var weak = RunPort.WeakTo(f.FoeNature);
            yield return Press("preset.tag1", 0.3f);
            Expect(PartyStore.Tag(1) == "순수", "프리셋 1 성격 아이콘 한 번 누르면 순수 (" + PartyStore.Tag(1) + ")");
            PartyStore.SetTag(1, "광기"); PartyStore.SetTag(3, weak.Count > 0 ? weak[0] : "냉정"); PartyStore.SetTag(4, "우울");
            Debug.Log($"[Demo] 적 속성 {f.FoeNature} · 약점 {string.Join(",", weak)}");
            CloseModals(); yield return Wait(0.4f);
            yield return Press("party.presets", 0.8f);
            Expect(PartyStore.Preset(1).SequenceEqual(after) && PartyStore.Preset(5).SequenceEqual(after), "프리셋 1 · 5 저장");
            yield return Wait(0.5f);
            yield return Shot("pk_presets");
            CloseModals(); yield return Wait(0.5f);
            yield return Press("party.random", 1.2f);
            var rnd = f.PartySlotsNow.ToArray();
            Expect(rnd.All(k => k != null) && rnd.Distinct().Count() == 3, $"무작위 편성 — 서로 다른 셋({string.Join("/", rnd)})");
            yield return Shot("pk_random");
            int vBefore = voice.Count;
            yield return Press("party.presets", 0.8f);
            yield return Press("preset.load1", 1.0f);
            Expect(f.PartySlotsNow.SequenceEqual(after) && voice.Count == vBefore, "프리셋 1 불러오기 — 편성 복원 · 소리 없음");
            // 시작 덱 목록
            yield return Press("party.deck", 1.0f);
            yield return Shot("pk_deck");
            yield return Press("deck.card0", 1.0f);
            yield return Shot("pk_deck_card");
            CloseModals(); yield return Wait(0.5f); CloseModals(); yield return Wait(0.5f);
            yield return Shot("pk_board_row");
            // 모험 시작 → 최근 편성 기억
            yield return Press("party.go", 0.5f);
            yield return Screen_("map", 1.0f);
            Expect(PartyStore.Recent.SequenceEqual(after), "최근 편성 기억");
            // 완주 세기 — 끝(clear) 한 번에 파티 셋 +1(같은 판을 두 번 부르면 한 번만)
            int c0 = PartyStore.Cleared(after[0]);
            f.End("clear");
            yield return Screen_("end_clear", 1.5f);
            Expect(PartyStore.Cleared(after[0]) == c0 + 1, $"완주 +1 ({c0} → {PartyStore.Cleared(after[0])})");
            f.End("clear");
            yield return Wait(0.5f);
            Expect(PartyStore.Cleared(after[0]) == c0 + 1, "같은 판은 두 번 세지 않음");
            // 보드 요약 줄 → 보드 화면
            f.Party(f.P.S.Village);
            yield return Screen_("party", 1.2f);
            yield return Press("party.board", 1.0f);
            Expect(f.Stage.Current == "crayon", "보드 요약 줄을 누르면 보드 화면 (지금 " + f.Stage.Current + ")");
            PartyStore.ResetAll(); CrayonStore.Reset();
            Debug.Log($"[Demo] 편성 편의 캡처 끝 · 실패 {fails}");
            yield return Wait(0.4f);
            Application.Quit(fails == 0 ? 0 : 6);
        }
    }
}
