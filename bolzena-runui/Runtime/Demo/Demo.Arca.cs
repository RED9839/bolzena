using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 소개 글용 캡처(-demo-arca, 2026-10-07) — 점검 표시 없이 판 화면을 찍는다: 로비 → 마을 공개(적 속성 · 1 · 2층 보스 클론) → 편성(고른 셋)
    //   → 사도 상세 → 지도(처음 · 보스 칸까지 민 것) → 적 도감 보스 클론 탭 → 사도 도감.
    //   -arca-party 에르핀,네르,다야 — 편성할 사도(없으면 고를 수 있는 앞 셋). -arca-lobby 에르핀 — 로비 메인 사도(저장은 건드리지 않는다).
    //   저장(이어하기 · PlayerPrefs)을 지우지 않는다 — -save 로 따로 둔 저장 파일만 쓴다.
    public partial class Demo
    {
        IEnumerator Arca_()
        {
            var want = (Arg("-arca-party") ?? "").Split(',').Select(k => Roster.ByKey(k.Trim())?.key).Where(k => k != null).Distinct().ToList();
            foreach (var k in Roster.All.Where(h => h.Playable).Select(h => h.key)) { if (want.Count >= 3) break; if (!want.Contains(k)) want.Add(k); }
            Flow.DemoLobbyHero = Arg("-arca-lobby") ?? "에르핀";
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 2.4f);
            yield return Shot("arca_lobby");

            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(3.2f);
            yield return Shot("arca_village");
            yield return Press("go");

            yield return Screen_("party", 1.2f);
            yield return Press("slot0", 1.0f);
            foreach (var k in want) yield return Press("hero:" + k, 0.4f);
            yield return Wait(0.6f);
            yield return Shot("arca_herolist");
            yield return Press("list.done", 0.8f);
            yield return Wait(1.0f);
            yield return Shot("arca_party");
            yield return Press("zoom0", 1.4f);
            yield return Shot("arca_hero_detail");
            yield return Press("detail.tab:카드", 1.2f);
            yield return Shot("arca_hero_detail_cards");
            yield return Press("detail.close", 0.8f);
            yield return Press("party.go");

            yield return Screen_("map", 2.0f);
            yield return Shot("arca_map");
            var sr = f.Stage.Root.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.horizontal);
            if (sr != null)
            {
                sr.StopMovement();
                sr.horizontalNormalizedPosition = 1f;
                yield return Wait(1.0f);
                yield return Shot("arca_map_boss");
            }

            // 도감 — 보스 클론 탭 · 몬스터 탭 · 사도
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.4f);
            yield return Shot("arca_dex_heroes");
            yield return Press("tab:적", 1.4f);
            yield return Shot("arca_dex_monsters");
            yield return Press("foetab:보스 클론", 1.6f);
            yield return Shot("arca_dex_clones");
            Debug.Log("[Demo] 끝(소개 캡처)");
            if (Arg("-save") != null) RunPort.ClearSave();   // -save 로 따로 둔 이 실행의 저장만
            Flow.DemoLobbyHero = null;
            yield return Wait(0.4f);
            Application.Quit(0);
        }
    }
}
