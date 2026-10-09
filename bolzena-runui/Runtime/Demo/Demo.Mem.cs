using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 메모리 점검(-demo-mem, 2026-10-06 웹 OOM 「처음 사도 상세에 들어가면 오류」) — 큰 화면을 차례로 지나며 [Mem] 줄을 남긴다.
    //   로비 → 도감 사도 목록(끝까지 내려 135칸 굽기) → 상세 여럿(초상 줄 · 카드 탭) → 적 · 장비 · 교주 카드 도감 → 판(마을 · 편성 · 목록 · 상세 · 지도 · 이벤트 · 싸움)
    // 목록 카드 시트(-demo-listsheet) — 135명 목록 카드 그림을 한 장에 48명씩(얼굴 · 머리 · 가슴께가 보이는지 눈으로). -oldcrop 이면 예전 자르기.
    public partial class Demo
    {
        void M(string tag) => MemLog.Log(tag);

        IEnumerator Mem_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.5f);
            M("lobby");
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.5f);
            yield return Wait(2.5f);
            M("dex_list");
            yield return Shot("mem_dex_list");
            // 목록을 끝까지 내리며 보이는 칸을 굽게 한다
            var sr = f.Stage.ScreenLayer.GetComponentInChildren<ScrollRect>();
            if (sr != null)
            {
                float hmax = sr.content.rect.height;
                for (float y = 0; y <= hmax; y += 300)
                {
                    sr.content.anchoredPosition = new Vector2(sr.content.anchoredPosition.x, y);
                    yield return Wait(0.6f);
                }
                yield return Wait(2.0f);
                M("dex_list_scrolled");
                yield return Shot("mem_dex_list_end");
                sr.content.anchoredPosition = new Vector2(sr.content.anchoredPosition.x, 0);
                yield return Wait(1.0f);
            }
            // 상세 여럿 — 첫 상세가 예전 OOM 자리(초상 줄 135명을 한 프레임에 굽던 곳)
            var all = Roster.All.Where(h => h.art != null).ToList();
            var picks = new List<string> { all.Any(h => h.key == "벨라") ? "벨라" : all[3].key };
            for (int i = 10; i < all.Count && picks.Count < 6; i += 23) if (!picks.Contains(all[i].key)) picks.Add(all[i].key);
            int n = 0;
            foreach (var k in picks)
            {
                n++;
                yield return Press("hero:" + k, 2.5f);
                M("detail_" + k);
                if (n <= 2) yield return Shot("mem_detail_" + n);
                if (n == 1)
                {
                    // 초상 줄을 끝까지 내려 보기 → 아래쪽 사도로 넘기기
                    var rail = f.Stage.ModalLayer.GetComponentsInChildren<ScrollRect>().FirstOrDefault(s => s.name.Contains("faces") || s.transform.parent?.name == "faces");
                    if (rail != null)
                    {
                        float hm = rail.content.rect.height;
                        for (float y = 0; y <= hm; y += 400) { rail.content.anchoredPosition = new Vector2(0, y); yield return Wait(0.4f); }
                        yield return Wait(1.0f);
                        M("detail_rail_scrolled");
                    }
                    yield return Press("detail.face60", 2.0f);
                    M("detail_face60");
                    yield return Press("detail.tab:카드", 1.5f);
                    M("detail_cards");
                    yield return Shot("mem_detail_cards");
                }
                yield return Press("detail.close", 1.0f);
            }
            M("dex_after_details");
            yield return Press("tab:적", 2.0f);
            M("dex_foes");
            yield return Press("tab:아티팩트", 2.0f);
            M("dex_equips");
            yield return Press("tab:교주 카드", 2.0f);
            M("dex_cards");
            yield return Press("tab:사도", 2.0f);
            M("dex_heroes_again");

            // 판 — 마을 · 편성 · 목록 · 상세 · 지도 · 이벤트 · 싸움
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            M("lobby2");
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(3.0f);
            yield return Until(() => Hot("go") != null, 10);
            M("village");
            yield return Press("go");
            yield return Screen_("party", 1.2f);
            M("party");
            yield return Press("slot0", 2.0f);
            M("party_list");
            yield return Press("list.done", 0.8f);
            yield return PickParty(1.2f);
            M("party_full");
            yield return Shot("mem_party");
            yield return Press("zoom0", 1.5f);
            M("party_detail");
            yield return Press("detail.close", 0.8f);
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            M("map");
            // 싸움 먼저(전투 장면 — 붙어 있으면 그쪽 봇이 싸운다), 그다음 이벤트
            float t = 0;
            for (int fights = 0; fights < 2; fights++)
            {
                t = 0;
                while (t < 25 && (f.Stage.Busy || f.Stage.Current != "map")) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Stage.Current != "map") { Debug.LogWarning("[Demo] 지도가 아님 — " + f.Stage.Current); break; }
                var reach = f.P.Reachable();
                var map = f.P.Map();
                var fight = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "elite");
                if (fight == null) { Debug.LogWarning("[Demo] 갈 수 있는 싸움 칸 없음"); break; }
                M("before_fight" + fights);
                yield return Press("node:" + fight, 1.2f);
                yield return GoFight("win");
                yield return Wait(1.0f);
                M("after_fight" + fights);
            }
            t = 0;
            while (t < 25 && (f.Stage.Busy || f.Stage.Current != "map")) { t += Time.unscaledDeltaTime; yield return null; }
            var ev = f.P.Data.Events.FirstOrDefault(e => e.Npc != null) ?? f.P.Data.Events.FirstOrDefault();
            if (ev != null)
            {
                OpenEvent(ev.Id);
                yield return Screen_("event", 2.4f);
                M("event_" + ev.Id);
                yield return Shot("mem_event");
                yield return GoEvent();
                yield return Wait(1.0f);
                M("event_done");
            }
            M("end");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.5f);
            Application.Quit(0);
        }

        IEnumerator ListSheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            var heroes = Roster.All.Where(h => h.art != null).ToList();
            var size = f.Stage.Size;
            const int cols = 12, rows = 4, per = cols * rows;
            float k = Theme.C(1f, 0.86f);
            float ratio = (150 * k - 4) / (214 * k - 4);   // 목록 카드 그림 칸 비율(ListCard → HeroArt)
            float cw = Mathf.Floor((size.x - 40) / cols), aw = cw - 10, ah = aw / ratio, lab = 30;
            float ch = ah + lab + 6;
            string tag = StandingSnap.OldListCrop ? "before" : "after";
            for (int p = 0; p * per < heroes.Count; p++)
            {
                var layer = Ui.Rect("modal listsheet", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
                var made = new List<Image>();
                var page = heroes.Skip(p * per).Take(per).ToList();
                for (int i = 0; i < page.Count; i++)
                {
                    var h = page[i];
                    float x = 20 + (i % cols) * cw, y = -10 - (i / cols) * ch;
                    var card = Ui.Img(layer, Theme.Round, Color.Lerp(Theme.NavyWell, Theme.NatureOf(h.nature), 0.32f), "card " + h.art);
                    card.rectTransform.At(0, 1, x + 5, y, aw, ah);
                    card.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    var still = Ui.Img(card.rectTransform, null, Color.clear, "still"); still.rectTransform.Fill();
                    var ls = card.gameObject.AddComponent<LiveStanding>();
                    ls.Art = h.art; ls.W = aw; ls.H = ah; ls.Frac = 0.5f; ls.List = true; ls.WebFallback = true; ls.Still = still;
                    made.Add(still);
                    // 위 1/3 선(얼굴이 이 근처) — 옅게
                    var third = Ui.Img(card.rectTransform, Theme.White, Theme.Sky.A(0.35f), "third"); third.rectTransform.At(0, 1, 0, -ah / 3f, aw, 1);
                    third.rectTransform.pivot = new Vector2(0, 0.5f);
                    var t = Ui.Text(layer, $"{h.ko}\n<size=80%>{h.art}</size>", 12, Theme.Ink, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x, y - ah - 2, cw, lab);
                }
                float t0 = 0;
                while (t0 < 40 && made.Any(im => im.sprite == null && im.GetComponentInParent<LiveStanding>() != null && !Baked(im))) { t0 += Time.unscaledDeltaTime; yield return null; }
                yield return Wait(0.8f);
                M("listsheet_" + p);
                yield return Shot($"listsheet_{tag}_{p + 1}");
                Destroy(layer.gameObject);
                yield return Wait(0.3f);
            }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        // 그 칸의 LiveStanding 이 한 번 돌았는가(굽기에 실패해 그림이 없는 칸에서 기다리지 않게)
        static bool Baked(Image still)
        {
            var ls = still.GetComponentInParent<LiveStanding>();
            return ls == null || ls.Tried;
        }
    }
}
