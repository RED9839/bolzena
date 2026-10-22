using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 카제나식 화면 틀(-demo-czn, 2026-10-21 — Docs/참고_카제나_UI.md §공통 화면 틀 · 전투원 상세 · 결과 화면) —
    //   로비 → 도감(사도 · 교주 카드) → 사도 상세(능력치 · 카드) → 사도 저장함 → 프론티어 → 모험 시작 → (단계) → 마을 고르기 → 편성 →
    //   판 끝(완주 · 전투 기록 창) → 새 판 끝(실패). 화면마다 머리(왼쪽 · 오른쪽)와 오른쪽 위 도구 줄이 겹치지 않는지 단언한다.
    public partial class Demo
    {
        IEnumerator Czn_()
        {
            int f0 = fails;
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.2f);
            Expect(Hot("dungeon") == null && Hot("frontier") != null, "로비 오른쪽 메뉴 — 던전 단추 없음(2026-10-21 밤 던전 없앰)");
            yield return Shot("czn_lobby");
            Expect(Hot("crayon") == null, "로비 — 교주 보드 칸 없음(2026-10-21 없앰)");

            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.6f);
            yield return Shot("czn_dex");
            HeadCheck("도감 사도");
            yield return Press("tab:교주 카드", 1.0f);
            yield return Shot("czn_dex_cards");
            HeadCheck("도감 교주 카드");
            yield return Press("tab:적", 1.0f);
            yield return Shot("czn_dex_foes");
            HeadCheck("도감 적");

            var aya = Roster.OfCore("아야") ?? Roster.Players.FirstOrDefault();
            var keys = Roster.Players.Take(6).Select(h => h.key).ToList();
            if (!keys.Contains(aya.key)) keys.Insert(0, aya.key);
            f.HeroDetail(aya.key, keys, null);
            yield return Wait(1.8f);
            yield return Shot("czn_detail_stats");
            HeadCheck("사도 상세 능력치");
            Expect(Hot("detail.tab:능력치") != null && Hot("detail.tab:카드") != null, "사도 상세 — 능력치 · 카드 탭");
            Expect(Hot("detail.tab:잠재력") != null, ProgStore.On ? "성장 켬 — 잠재력 탭" : "성장 끔 — 잠재력 탭(보기 전용, 10-22)");
            Expect(Hot("detail.lv1") == null, "사도 상세 — 레벨 단추 없음(레벨은 없앴다)");
            DetailCheck("사도 상세 능력치");
            Expect(Hot("detail.statinfo") != null && Hot("detail.profile") != null, "사도 상세 — 능력치 (i) · ID 카드 단추");
            Expect(Hot("detail.list") != null, "도감 사도 상세 — ▦ 사도 목록으로");
            Expect(Hot("detail.saved") != null && Hot("detail.savedcard") != null, "도감 사도 상세 — 저장한 덱 탭 · 카드");
            Expect(Hot("detail.tab:저장한 덱") == Hot("detail.saved"), "저장한 덱 탭 = detail.saved");
            // 저장한 덱 → 뒤로 — 상세 초상 줄이 그 사도 하나로 줄지 않는다(10-21 사용자 버그: keys 를 안 넘겼다)
            if (Hot("detail.saved") != null)
            {
                yield return Press("detail.saved", 0.3f);
                yield return Screen_("slotbox", 1.2f);
                yield return Press("back");
                yield return Wait(1.6f);
                Expect(f.Stage.ModalLayer.Find("modal herodetail") != null, "저장한 덱에서 뒤로 → 사도 상세");
                Expect(Hot("detail.face1") != null, "저장한 덱에서 뒤로 → 초상 줄이 1명보다 많다(detail.face1)");
                yield return Shot("czn_detail_saved_back");
            }
            yield return Press("detail.statinfo", 0.8f);
            yield return Shot("czn_detail_statinfo");
            Expect(f.Stage.ModalLayer.Cast<Transform>().Any(t => t.name.Contains("statinfo")), "능력치 (i) → 자세한 능력치 창");
            f.Stage.Back();
            yield return Wait(0.4f);
            yield return Press("detail.profile", 0.8f);
            yield return Shot("czn_detail_profile");
            Expect(f.Stage.ModalLayer.Cast<Transform>().Any(t => t.name.Contains("profile")), "ID 카드 → 프로필 창");
            f.Stage.Back();
            yield return Wait(0.4f);
            yield return Press("detail.tab:카드", 1.4f);
            yield return Shot("czn_detail_cards");
            DetailCheck("사도 상세 카드");
            // 잠재력 탭 — 성장 꺼짐(공개판)이어도 보기 전용으로 보인다(2026-10-22): 판 · 칸 이름 · 효과 · 필요한 마시멜로 칩, 단추는 흐림 + 까닭
            Expect(Hot("detail.tab:잠재력") != null, "사도 상세 — 「잠재력」 탭(성장 꺼짐이어도 보기 전용)");
            yield return Press("detail.tab:잠재력", 1.6f);
            yield return Shot("czn_detail_potential");
            {
                var up = Hot("pot.up");
                string why = up?.Label != null ? up.Label.text : "";
                bool off = !ProgStore.On;
                Expect(Hot("pot.01") != null && Hot("pot.07") != null && Hot("pot.05-2") != null, "잠재력 탭 — 노드 판 10칸");
                Expect(up != null && !up.Interactable && (off ? why.Contains("성장이 열리면 키울 수 있다") : why.Length > 0), $"잠재력 탭 — 열기 단추 흐림 · 까닭 「{why}」");
                Expect(f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "mat mm1"), "잠재력 탭 — 01 에 필요한 하급 마시멜로 칸(그림)");
                yield return Press("pot.07", 0.8f);
                Expect(f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "mat mm3"), "잠재력 탭 — 07 에 필요한 상급 마시멜로 칸");
                yield return Shot("czn_detail_potential_07");
                Expect(!off || f.P.Data.Prog == null, "보기 전용 — 엔진 성장 표는 비어 있다(콘텐츠에 넣지 않음 · 화면 사본만)");
            }
            yield return Press("detail.tab:카드", 1.0f);
            yield return Press("detail.face1", 1.4f);
            yield return Shot("czn_detail_next");
            DetailCheck("사도 상세 다음 사도");
            yield return Press("detail.list", 0.6f);
            Expect(!f.Stage.ModalLayer.Cast<Transform>().Any(t => t.name == "modal herodetail"), "▦ → 상세를 닫고 도감 사도 목록");
            CloseModals();
            yield return Wait(0.3f);

            f.SlotBox(f.Lobby);
            yield return Screen_("slotbox", 1.4f);
            yield return Shot("czn_slotbox");
            HeadCheck("보관함");
            yield return HomeCheck();

            f.FrontierScreen();
            yield return Screen_("frontier", 1.6f);
            yield return Shot("czn_frontier");
            HeadCheck("프론티어");


            // 모험 시작 → 마을 고르기(성장 켬이면 상세 판에 단계 줄) → 마을 공개 → 편성
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start", 0.3f);
            yield return Screen_("vilpick", 1.6f);
            yield return Shot("czn_vilpick");
            Expect((Hot("tier.1") != null) == ProgStore.On && Hot("tier.go") == null, ProgStore.On ? "마을 고르기 상세 판에 난이도 단계 줄(따로 고르는 화면 없음)" : "성장 끔 — 마을 고르기에 단계 줄 없음 · 단계 화면 없음");
            HeadCheck("마을 고르기");
            yield return Press("vilpick.go", 0.2f);
            yield return Screen_("village", 2.6f);
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Shot("czn_party");
            HeadCheck("편성");
            yield return PickParty(0.6f);
            yield return Shot("czn_party_full");
            // 편성 카드 돋보기 → 상세(목록이 밑에 없다 — ▦ 은 그 칸의 사도 목록을 연다)
            yield return Press("zoom0", 1.6f);
            yield return Shot("czn_party_detail");
            HeadCheck("편성 사도 상세");
            DetailCheck("편성 사도 상세");
            Expect(Hot("detail.saved") == null && Hot("detail.savedcard") == null, "편성 사도 상세 — 저장한 덱 없음(판 화면)");
            Expect(Hot("detail.tab:잠재력") != null, "편성 돋보기에서 연 사도 상세 — 「잠재력」 탭(보기 전용)");
            if (Hot("detail.list") != null)
            {
                yield return Press("detail.list", 0.8f);
                Expect(f.Stage.ModalLayer.Cast<Transform>().Any(t => t.name == "modal herolist"), "편성 사도 상세 ▦ → 사도 목록");
            }
            else Expect(false, "편성 사도 상세 — ▦ 단추");
            CloseModals();
            yield return Wait(0.3f);
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            // 지도에서 연 상세(판 안 — 홈 없음 · ▦ 없음)
            var mapKeys = f.P.S.Party.Select(k => Roster.OfCore(k)?.key).Where(x => x != null).ToList();
            if (mapKeys.Count > 0)
            {
                f.HeroDetail(mapKeys[0], mapKeys, null);
                yield return Wait(1.6f);
                yield return Shot("czn_map_detail");
                HeadCheck("지도 사도 상세");
                DetailCheck("지도 사도 상세");
                Expect(Hot("detail.list") == null, "지도 사도 상세 — ▦ 없음");
                Expect(Hot("detail.tab:잠재력") != null, "지도에서 연 사도 상세 — 「잠재력」 탭(보기 전용)");
                CloseModals();
                yield return Wait(0.3f);
            }

            // 판 끝(완주) — 지나온 전투 몇 줄을 넣고 끝낸다(낸 카드 기록은 첫 줄만 — 사도별 「쓴 카드」 칸)
            var party = f.P.S.Party.ToList();
            var foes = f.P.Data.Enemies.Keys.Take(4).ToList();
            var played = party.SelectMany(k => f.P.Data.Hero(k)?.Starter ?? new List<string>()).ToList();
            for (int i = 0; i < 9; i++)
                f.P.S.Hist.Add(new Core.FightRecord { Floor = 1 + i / 5, Kind = i == 4 ? "elite" : i == 8 ? "boss" : "fight", Result = "win", Turns = 3 + i % 4, HpBefore = 300 - i * 10, HpAfter = 280 - i * 10, Foes = foes.Skip(i % 3).Take(2).ToList(),
                    Plays = played.Count, Log = i == 0 ? new List<Core.TurnRec> { new Core.TurnRec { T = 1, C = played } } : null });
            f.End("clear");
            yield return Screen_("end_clear", 2.6f);
            yield return Shot("czn_end_clear");
            Expect(Hot("end.lobby") != null, "판 끝 — 「로비로」");
            Expect(Hot("end.hist") != null, "판 끝 — 「전투 기록」 단추");
            EndCheck();
            if (Hot("end.hist") != null)
            {
                yield return Press("end.hist", 0.8f);
                yield return Shot("czn_end_hist");
                CloseModals();
                yield return Wait(0.3f);
            }

            // 판 끝(실패) — 새 판을 곧장 열고 바로 진다
            var village = f.P.RollVillage();
            f.FoeNature = null;
            f.StartRun(party, village);
            yield return Screen_("map", 1.2f);
            f.P.S.Hist.Add(new Core.FightRecord { Floor = 1, Kind = "fight", Result = "lose", Turns = 5, HpBefore = 120, HpAfter = 0, Foes = foes.Take(2).ToList() });
            f.End("lose");
            yield return Screen_("end_lose", 2.4f);
            yield return Shot("czn_end_lose");
            EndCheck();
            yield return Press("end.lobby", 0.3f);
            yield return Screen_("lobby", 0.8f);
            Expect(f.Stage.Current == "lobby", "판 끝 「로비로」 → 로비");

            Debug.Log($"[Demo] 카제나 화면 틀 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }

        static Rect WorldRect(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }

        /// <summary>머리 왼쪽(head) · 오른쪽(headR) · 오른쪽 위 도구 줄(tools · partytools 의 보이는 칸)이 서로 겹치지 않고 화면 안에 있는지 — 맨 위 층(창이 있으면 창)만.</summary>
        void HeadCheck(string what)
        {
            var ml = f.Stage.ModalLayer;
            Transform top = ml.childCount > 0 ? ml.GetChild(ml.childCount - 1) : f.Stage.ScreenLayer;
            var all = top.GetComponentsInChildren<RectTransform>(true);
            var head = all.FirstOrDefault(r => r.name == "head");
            var right = all.FirstOrDefault(r => r.name == "headR");
            Expect(head != null, $"{what} — 머리");
            Expect(right != null && right.Find("home") != null == (f.Stage.Current != "map"), $"{what} — 오른쪽 위 홈 단추");
            Expect(right != null && right.Find("settings") != null, $"{what} — 오른쪽 위 설정");
            if (head == null || right == null) return;
            var scr = WorldRect((RectTransform)f.Stage.ScreenLayer);
            // 머리 왼쪽은 글 끝(제목 · 풀이)까지만 잰다 — head 칸 자체는 넓게 잡혀 있다
            float headEnd = head.Cast<RectTransform>().Where(c => c.gameObject.activeSelf).Select(c => WorldRect(c).xMax).DefaultIfEmpty(WorldRect(head).xMin).Max();
            var rr = WorldRect(right);
            Expect(rr.xMax <= scr.xMax + 1 && rr.yMax <= scr.yMax + 1, $"{what} — 오른쪽 위 묶음이 화면 안");
            Expect(headEnd <= rr.xMin + 1, $"{what} — 머리 글({headEnd:F0})이 오른쪽 묶음({rr.xMin:F0})과 겹치지 않음");
            foreach (var tools in all.Where(r => r.name == "tools" || r.name == "partytools"))
            {
                var tr = WorldRect(tools);
                if (tr.yMin < rr.yMin - 40) continue;   // 위 띠에 있는 도구 줄만
                var kids = tools.Cast<RectTransform>().Where(c => c.gameObject.activeSelf).Select(WorldRect).ToList();
                if (kids.Count == 0) continue;
                float l = kids.Min(k => k.xMin), r = kids.Max(k => k.xMax);
                Expect(r <= rr.xMin + 1, $"{what} — 도구 줄 끝({r:F0})이 오른쪽 묶음({rr.xMin:F0}) 왼쪽");
                Expect(l >= headEnd - 1, $"{what} — 도구 줄 처음({l:F0})이 머리 글 끝({headEnd:F0}) 오른쪽");
            }
        }

        /// <summary>사도 상세 틀 — 사이드 판 · 오른쪽 판 · 가운데(스탠딩 칸 · 카드 묶음 · 잠재력 판)가 서로 겹치지 않고 화면 안,
        /// 초상 줄 · 탭 · ▦ 은 사이드 판 안, 오른쪽 판 내용(세로 줄)은 판 안, 머리 오른쪽 묶음은 판과 겹치지 않음.</summary>
        void DetailCheck(string what)
        {
            var top = f.Stage.ModalLayer.Cast<Transform>().LastOrDefault(t => t.name == "modal herodetail");
            if (top == null) { Expect(false, $"{what} — 상세 창"); return; }
            var all = top.GetComponentsInChildren<RectTransform>(true);
            RectTransform Find(string n) => all.FirstOrDefault(r => r.name == n);
            var scr = WorldRect((RectTransform)f.Stage.ScreenLayer);
            bool Inside(Rect a, Rect b) => a.xMin >= b.xMin - 1 && a.xMax <= b.xMax + 1 && a.yMin >= b.yMin - 1 && a.yMax <= b.yMax + 1;
            bool Cross(Rect a, Rect b) => a.xMin < b.xMax - 1 && b.xMin < a.xMax - 1 && a.yMin < b.yMax - 1 && b.yMin < a.yMax - 1;
            var side = Find("sidepanel");
            var panel = Find("stats") ?? Find("side") ?? Find("potpanel");
            var mid = Find("figure") ?? Find("cards") ?? Find("potboard");
            Expect(side != null && panel != null && mid != null, $"{what} — 사이드 판 · 오른쪽 판 · 가운데");
            if (side == null || panel == null || mid == null) return;
            Rect s = WorldRect(side), p = WorldRect(panel), m = WorldRect(mid);
            var sideX = Rect.MinMaxRect(s.xMin, s.yMin, s.xMax + 10, s.yMax);   // 고른 탭 알약은 판 끝을 조금 넘는다
            int bad = 0;
            void Bad(string why) { bad++; Debug.Log($"[Demo] 상세 배치 — {what}: {why}"); }
            if (!Inside(s, scr)) Bad("사이드 판이 화면 밖");
            if (!Inside(p, scr)) Bad("오른쪽 판이 화면 밖");
            if (!Inside(m, scr)) Bad("가운데가 화면 밖");
            if (Cross(s, p)) Bad("사이드 판 × 오른쪽 판");
            if (Cross(m, s)) Bad("가운데 × 사이드 판");
            if (Cross(m, p)) Bad("가운데 × 오른쪽 판");
            var headR = Find("headR");
            if (headR != null && (Cross(WorldRect(headR), p) || Cross(WorldRect(headR), s))) Bad("머리 오른쪽 묶음 × 판");
            foreach (var t in all.Where(r => r.gameObject.activeInHierarchy && (r.name.StartsWith("tab ") || r.name == "tolist" || r.name == "faces")))
                if (!Inside(WorldRect(t), sideX)) Bad($"{t.name} 이(가) 사이드 판 밖");
            var body = (panel.Find("body") ?? panel.Find("area")) as RectTransform;
            if (body != null) foreach (RectTransform c in body) if (c.gameObject.activeSelf && !Inside(WorldRect(c), p)) Bad($"오른쪽 판 내용 {c.name} 이(가) 판 밖");
            Expect(bad == 0, $"{what} — 상세 배치 겹침 · 화면 밖 {bad}");
        }

        /// <summary>홈 단추 → 로비.</summary>
        IEnumerator HomeCheck()
        {
            if (Hot("home") == null) { Expect(false, "홈 단추(Hot home)"); yield break; }
            yield return Press("home", 0.2f);
            yield return Screen_("lobby", 0.8f);
            Expect(f.Stage.Current == "lobby", "홈 → 로비");
        }

        /// <summary>판 끝 — 사도 카드 셋이 서로 · 제목 · 아래 띠와 겹치지 않고 화면 안에.</summary>
        void EndCheck()
        {
            var all = f.Stage.ScreenLayer.GetComponentsInChildren<RectTransform>(true);
            var cards = all.Where(r => r.name.StartsWith("endhero ")).ToList();
            Expect(cards.Count == 3, $"판 끝 — 사도 카드 {cards.Count}장");
            var scr = WorldRect((RectTransform)f.Stage.ScreenLayer);
            var rs = cards.Select(WorldRect).ToList();
            int bad = 0;
            for (int i = 0; i < rs.Count; i++)
            {
                if (rs[i].xMin < scr.xMin || rs[i].xMax > scr.xMax || rs[i].yMin < scr.yMin || rs[i].yMax > scr.yMax) bad++;
                for (int j = i + 1; j < rs.Count; j++) if (rs[i].Overlaps(rs[j])) bad++;
            }
            foreach (var n in new[] { "end.title", "endfoot" })
            {
                var o = all.FirstOrDefault(r => r.name == n);
                if (o == null) { Expect(false, $"판 끝 — {n}"); continue; }
                var orr = WorldRect(o);
                foreach (var c in rs) if (c.Overlaps(orr)) { bad++; Debug.Log($"[Demo] 판 끝 겹침 — 사도 카드 × {n}"); }
            }
            Expect(bad == 0, $"판 끝 — 카드 겹침 · 화면 밖 {bad}");
        }
    }
}
