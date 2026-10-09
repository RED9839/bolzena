using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 판 화면 한 바퀴(-demo-uitour) — UI 통일 · 지도 여백 전후를 같은 자리에서 찍는다.
    //   로비 · 교주 보드 · 도감 · 설정 → 마을 공개 → 편성 → 지도(처음 · 맨 위 줄 칸 · 맨 아래 줄 칸) → 덱 · 장비 · 학년 표 · 판 설정 · 사도 상세
    //   → 휴식 · 상점 → 이벤트 → 싸움(전투 HUD · 보상은 GoFight 가 찍는다)
    //   지도 「맨 위 줄 칸」 은 파티 미니미가 머리 띠(파티 알약 · 학년 카드) 바로 밑에 서는 자리 — 2026-10-09 사용자 캡처의 잘림을 다시 본다.
    public partial class Demo
    {
        IEnumerator UiTour_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.2f);
            yield return Shot("ui_lobby");
            if (Hot("crayon") != null)
            {
                yield return Press("crayon", 0.2f);
                yield return Wait(1.4f);
                yield return Shot("ui_crayon");
                f.Lobby();
                yield return Screen_("lobby", 0.8f);
            }
            yield return Press("dex", 0.2f);
            yield return Wait(1.6f);
            yield return Shot("ui_dex");
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("settings", 0.9f);
            yield return Shot("ui_settings_lobby");
            CloseModals();
            yield return Wait(0.3f);

            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(2.6f);
            yield return Shot("ui_village");
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Wait(0.6f);
            yield return Shot("ui_party");
            yield return Press("party.go");
            yield return Screen_("map", 1.8f);
            yield return Shot("ui_map_start");

            // 맨 위 줄 · 맨 아래 줄 칸에 파티를 세워 다시 그린다(갈림이 가장 많은 줄 — 맨 위 칸 X 가 가장 작다)
            var m = f.P.Map();
            var row = m.Rows.Skip(1).Take(m.Rows.Count - 2).OrderByDescending(r => r.Count).First();
            foreach (var (tag, node) in new[] { ("top", row.OrderBy(n => n.X).First()), ("bottom", row.OrderByDescending(n => n.X).First()) })
            {
                m.At = node.Id;
                if (!m.Seen.Contains(node.Id)) m.Seen.Add(node.Id);
                f.Map();
                yield return Screen_("map", 2.2f);
                yield return Shot("ui_map_" + tag);
                Debug.Log($"[Demo] 지도 {tag} — 칸 {node.Id} (줄 {node.Row} · X {node.X:F2} · 그 줄 {row.Count}칸)");
            }

            yield return Press("deck", 1.0f);
            yield return Shot("ui_deck");
            CloseModals();
            yield return Wait(0.3f);
            yield return Press("gear", 1.0f);
            yield return Shot("ui_gear");
            CloseModals();
            yield return Wait(0.3f);
            if (Hot("grade") != null) { yield return Press("grade", 0.9f); yield return Shot("ui_grade_table"); CloseModals(); yield return Wait(0.3f); }
            yield return Press("settings", 0.9f);
            yield return Shot("ui_settings_run");
            CloseModals();
            yield return Wait(0.3f);
            f.HeroSheet(f.P.S.Party[0]);
            yield return Wait(1.4f);
            yield return Shot("ui_herosheet");
            CloseModals();
            f.Map();
            yield return Screen_("map", 1.0f);

            // 휴식 · 상점 · 이벤트 — 지도 칸을 거치지 않고 화면만 연다(돌아오면 지도)
            f.Camp("campshop");
            yield return Screen_("camp", 1.4f);
            yield return Shot("ui_camp");
            f.Shop("campshop");
            yield return Screen_("shop", 1.4f);
            yield return Shot("ui_shop");
            f.Map();
            yield return Screen_("map", 1.0f);
            var ev = m.Rows.SelectMany(r => r).FirstOrDefault(n => n.Type == "event");
            if (ev != null)
            {
                m.At = ev.Id;
                f.EventStop();
                yield return Screen_("event", 1.6f);
                yield return Shot("ui_event");
                CloseModals();
            }
            f.Map();
            yield return Screen_("map", 1.0f);
            yield return FightOnce();
            yield return Screen_("map", 1.2f);
            yield return Shot("ui_map_after");
            Debug.Log("[Demo] UI 한 바퀴 끝");
            yield return Wait(0.4f);
            Application.Quit(0);
        }
    }
}
