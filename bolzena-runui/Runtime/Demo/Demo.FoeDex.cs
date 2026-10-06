using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 적 도감 점검(-demo-foedex, 2026-10-06 리뉴얼) — 몬스터 탭(성격 모습마다 한 장 · 카드 안 일반/엘리트 전환 · 상세) · 보스 클론 탭(사도 하나하나 · 마을 × 속성 × 층 필터 · 클론 상세)
    //   · 검색(초성) · 편성 화면 「나오는 적」 → 도감 항목 링크까지 찍고 단언한다.
    public partial class Demo
    {
        IEnumerator FoeDex_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.0f);
            yield return Press("tab:적", 1.2f);
            int mons = f.Stage.Hot.Keys.Count(k => k.StartsWith("dexmon:") && f.Stage.Hot[k] != null);
            Expect(mons >= 150, $"몬스터 탭 — 성격 모습마다 한 장 {mons}장");
            Expect(Hot("dexmon:furrywarriorcloserange|광기") != null && Hot("dexmon:furrywarriorcloserange|순수") != null, "수인 광전사(광기) · (순수) 카드가 따로 있다");
            Expect(Hot("dexmon:nururingtanker_furry|") != null, "누루링 수액은 종족 스킨마다 한 장");
            Expect(!f.Stage.Hot.Keys.Any(k => k.StartsWith("dexmon:clone")), "보스 몸은 몬스터 탭에 없다");
            yield return Shot("foedex_monsters");
            // 카드 안 일반 → 엘리트
            yield return Press("dexvar:furrywarriorcloserange|광기:엘리트", 0.6f);
            yield return Press("nat:광기", 1.0f);
            yield return Shot("foedex_monsters_mad");
            Expect(f.Stage.Hot.Keys.Where(k => k.StartsWith("dexmon:") && f.Stage.Hot[k] != null).All(k => k.EndsWith("|광기") || k.EndsWith("|")), "속성 필터 광기 — 광기 카드(+ 누루링)만");
            yield return Press("dexmon:furrywarriorcloserange|광기", 1.0f);
            yield return Shot("foedex_monster_detail");
            yield return Press("zoom.var0", 0.8f);
            yield return Press("zoom.look:냉정", 0.8f);
            yield return Shot("foedex_monster_detail_cool");
            CloseModals(); yield return Wait(0.4f);
            yield return Press("nat:광기", 0.8f);   // 다시 누르면 풀린다
            // 검색 — 초성
            yield return Type("ㄹㅋㅇㅊ");
            Expect(f.DexSearchShown.Count > 0 && f.DexSearchShown.All(n => HangulSearch.Match(n, "ㄹㅋㅇㅊ") || n.StartsWith("라쿤아치")), $"몬스터 초성 「ㄹㅋㅇㅊ」 → {string.Join(", ", f.DexSearchShown.Take(6))}");
            yield return Shot("foedex_search_monster");
            yield return Type("");

            // 보스 클론 탭
            yield return Press("foetab:보스 클론", 1.2f);
            int clones = f.Stage.Hot.Keys.Count(k => k.StartsWith("dexclone:") && f.Stage.Hot[k] != null);
            Expect(clones >= 60, $"보스 클론 탭 — 사도 하나하나 {clones}명");
            Expect(Hot("dexclone:다야_퓨어샤인") != null, "다야(퓨어샤인) 클론 카드가 있다");
            Expect(Hot("dexclone:란") == null && Hot("dexclone:티그_영웅") == null, "엘다인은 클론 후보가 아니다");
            Expect(Hot("dexclone:clone_tig") == null, "보스 몸은 항목이 아니다");
            yield return Shot("foedex_clones");
            // 마을 × 속성 × 층 — 유령 늪 · 광기
            yield return Press("filter:" + f.P.Data.Villages["ghost"].Name, 0.8f);
            yield return Press("nat:광기", 1.0f);
            yield return Shot("foedex_clones_ghost_mad");
            yield return Press("nat:순수", 1.0f);
            yield return Shot("foedex_clones_ghost_naive_closed");
            yield return Press("filter:2층", 1.0f);
            yield return Press("nat:순수", 0.6f);
            yield return Shot("foedex_clones_ghost_f2");
            yield return Press("filter:2층", 0.6f);
            // 용족 마을 · 광기 → 다야(퓨어샤인) 상세
            yield return Press("filter:" + f.P.Data.Villages["dragon"].Name, 0.8f);
            yield return Press("nat:광기", 1.0f);
            yield return Shot("foedex_clones_dragon_mad");
            yield return Press("dexclone:다야_퓨어샤인", 1.4f);
            Expect(f.Stage.ModalLayer.childCount > 0, "다야(퓨어샤인) 클론 상세가 열린다");
            yield return Shot("foedex_clone_daya_pure");
            yield return Press("zoom.floor0", 1.4f);
            yield return Shot("foedex_clone_daya_pure_f1");
            CloseModals(); yield return Wait(0.4f);
            // 소환하는 보스(다야 몸 골렘) — 다야 클론
            yield return Press("nat:순수", 1.0f);
            if (Hot("dexclone:다야") != null) { yield return Press("dexclone:다야", 1.4f); yield return Press("zoom.floor1", 1.4f); yield return Shot("foedex_clone_daya_summon");
                var msr = f.Stage.ModalLayer.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().FirstOrDefault();
                if (msr != null) { msr.verticalNormalizedPosition = 0.55f; yield return Wait(0.4f); yield return Shot("foedex_clone_daya_summon_scrolled"); }
                Expect(f.Stage.ModalLayer.GetComponentsInChildren<RectTransform>(true).Any(t => t.name == "summon golem"), "다야 몸 — 소환물(골렘)이 클론 상세 아래에 붙는다");
                CloseModals(); yield return Wait(0.4f); }
            yield return Press("filter:ALL", 0.6f);
            yield return Press("nat:순수", 0.6f);
            yield return Type("ㄷㅇ");
            Expect(f.DexSearchShown.Contains("다야(퓨어샤인)"), $"클론 초성 「ㄷㅇ」 → {string.Join(", ", f.DexSearchShown.Take(8))}");
            yield return Shot("foedex_search_clone");
            yield return Type("");

            // 편성 화면 「나오는 적」 → 도감 항목
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            var mon = f.Stage.Hot.Keys.Where(k => k.StartsWith("partyfoe:") && f.Stage.Hot[k] != null).OrderBy(k => k, System.StringComparer.Ordinal)
                .FirstOrDefault(k => f.P.Data.Enemy(k.Substring(9)) is Core.EnemyDef e && e.Clone == null);
            Expect(mon != null, "편성 — 나오는 적(몬스터) 칸 " + (mon ?? "없음"));
            if (mon != null)
            {
                yield return Press(mon, 1.0f);
                Expect(f.Stage.ModalLayer.childCount > 0, "편성 적 칸 → 몬스터 상세");
                yield return Shot("foedex_party_link_monster");
                yield return Press("zoom.close", 0.6f);
            }
            var boss = f.Stage.Hot.Keys.Where(k => k.StartsWith("partyfoe:") && f.Stage.Hot[k] != null).FirstOrDefault(k => f.P.Data.Enemy(k.Substring(9))?.Clone != null);
            if (boss != null)
            {
                yield return Press(boss, 1.2f);
                Expect(f.Stage.ModalLayer.childCount > 0, "편성 보스 칸 → 클론 상세");
                yield return Shot("foedex_party_link_clone");
                yield return Press("zoom.close", 0.6f);
            }
            Debug.Log($"[Demo] 적 도감 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }
    }
}
