using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 자동 데모(-demo) — 가짜 손가락으로 판 한 바퀴를 지나며 화면마다 캡처한다(Captures/NN_이름.png, -phone 이면 _phone 꼬리).
    //   로비 → 마을 공개 → 편성(빈 칸 · 셋) → 지도 → 싸움 → 보상 · 장비 → 이벤트 → 캠프 → 상점(사기 · 제거) → … → 층 보스 → 복제
    //   → 2층 → 마지막 보스 → 끝(완주) → 도감 · 설정 · 덱 → 새 판에서 지기 → 끝(패배)
    public class Demo : MonoBehaviour
    {
        public static bool Active;
        public static Demo Me { get; private set; }
        /// <summary>지금 싸움을 어떻게 받을지 — win(이기게) · lose(지게). 전투 화면(FightScreen)이 열 때 읽는다.</summary>
        public static string FightMode = "win";
        Flow f;
        string dir, tail = "";
        int no;
        readonly HashSet<string> seen = new HashSet<string>();

        public static void Attach(Flow flow)
        {
            Active = true;
            var d = flow.gameObject.AddComponent<Demo>();
            d.f = flow;
            Me = d;
        }

        /// <summary>다른 화면(전투)이 같은 번호 줄로 찍는다 — 같은 이름은 한 번만.</summary>
        public void Snap(string name) => StartCoroutine(Shot(name));

        void Start()
        {
            dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            if (Has("-phone")) tail = "_phone";
            Directory.CreateDirectory(dir);
            Debug.Log($"[Demo] captures → {dir} ({Screen.width}×{Screen.height})");
            RunPort.ClearSave();
            PlayerPrefs.DeleteKey("bz.calm");
            // 감시 — 예외가 나거나 시간이 넘으면 오류 코드로 스스로 꺼진다(먹통으로 남지 않게)
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Exception) Bail(2, "예외 — " + msg);
            };
            bool shortRun = Has("-demo-roster"), quick = Has("-demo-quick");
            float limit = float.TryParse(Arg("-demo-timeout"), out var lim) ? lim : shortRun ? 150f : quick ? 240f : 420f;
            StartCoroutine(Watchdog(limit));
            StartCoroutine(AutoOwner());
            StartCoroutine(Has("-demo-search") ? Search_() : Has("-demo-sheet") ? Sheet_() : quick ? Quick() : shortRun ? Roster_() : Run());
        }

        // 도감 검색(-demo-search) — 초성 · 부분 이름 · 섞어 쓰기 · 괄호 이름 · 결과 없음 · 도감마다 따로 기억을 캡처하고 단언한다
        int fails;
        void Expect(bool ok, string what) { Debug.Log($"[Demo] 단언 {(ok ? "OK" : "실패")} — {what}"); if (!ok) fails++; }
        IEnumerator Type(string q)
        {
            var fld = f.DexSearchField;
            if (fld == null) { Expect(false, "검색 칸 없음"); yield break; }
            fld.text = q;
            yield return Wait(0.5f);
        }
        IEnumerator Search_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Shot("lobby_menu");   // 메뉴 이름 「도감」
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.2f);
            yield return Shot("search_empty");
            var all = f.DexSearchShown.Count;
            yield return Type("ㅌㄱ");
            yield return Shot("search_chosung");
            Expect(f.DexSearchShown.Contains("티그") && f.DexSearchShown.All(n => HangulSearch.Match(n, "ㅌㄱ")), $"초성 「ㅌㄱ」 → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("티ㄱ");
            Expect(f.DexSearchShown.Contains("티그"), $"섞어 쓰기 「티ㄱ」 → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("왕년");
            yield return Shot("search_partial");
            Expect(f.DexSearchShown.Count >= 1 && f.DexSearchShown.All(n => n.Contains("왕년")), $"부분 이름 「왕년」 → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("ㄷㅇㄴ");
            Expect(f.DexSearchShown.Any(n => n.StartsWith("디아나")), $"초성 「ㄷㅇㄴ」(괄호 이름) → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("ㅋㅋㅋ없는사도");
            yield return Shot("search_none");
            var nm = f.Stage.ScreenLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false).Any(t => t.text == "찾는 것이 없습니다");
            Expect(f.DexSearchShown.Count == 0 && nm, "결과 없음 → 「찾는 것이 없습니다」");
            yield return Type("ㄷㅇㄴ");
            // 도감을 바꾸면 그 도감의 검색어(비어 있음), 돌아오면 사도 검색어 그대로
            yield return Press("tab:장비", 1.0f);
            Expect(f.DexSearchField != null && f.DexSearchField.text == "", "장비 도감 검색어는 따로(빈 칸)");
            var eqName = f.DexSearchShown.FirstOrDefault();
            if (eqName != null && eqName.Length >= 2)
            {
                var part = eqName.Substring(eqName.Length - 2);
                yield return Type(part);
                yield return Shot("search_equip");
                Expect(f.DexSearchShown.Contains(eqName), $"장비 부분 이름 「{part}」 → {string.Join(", ", f.DexSearchShown)}");
            }
            yield return Press("tab:교주 카드", 1.0f);
            var cName = f.DexSearchShown.FirstOrDefault();
            if (cName != null) { var q = new string(cName.Take(2).Select(HangulSearch.Initial).ToArray()); yield return Type(q); Expect(f.DexSearchShown.Contains(cName), $"교주 카드 초성 「{q}」 → {string.Join(", ", f.DexSearchShown)}"); yield return Shot("search_cards"); }
            yield return Press("tab:적", 1.0f);
            var fName = f.DexSearchShown.FirstOrDefault();
            if (fName != null) { yield return Type(fName.Substring(0, 1)); Expect(f.DexSearchShown.Contains(fName), $"적 「{fName.Substring(0, 1)}」 → {f.DexSearchShown.Count}종"); yield return Shot("search_foes"); }
            yield return Press("tab:사도", 1.0f);
            Expect(f.DexSearchField != null && f.DexSearchField.text == "ㄷㅇㄴ", $"사도 도감으로 돌아오면 검색어 그대로 — 「{f.DexSearchField?.text}」");
            yield return Shot("search_back");
            if (Hot("dex.search.clear") != null) { yield return Press("dex.search.clear", 0.5f); Expect(f.DexSearchShown.Count == all, $"지우기(×) → {f.DexSearchShown.Count}/{all}명"); }
            Debug.Log($"[Demo] 검색 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        // 스탠딩 맞춤 시트만(-demo-sheet) — 숙인 · 기운 사도의 전신 · 무릎께 · 카드 그림 · 얼굴 칸을 한 장으로(-nohead 와 같이 돌리면 고치기 전)
        IEnumerator Sheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            if (Has("-sheet-compare")) { CompareSheet(); yield return Wait(1.5f); yield return Shot("height_compare"); }
            else { LiveSheet(); yield return Wait(1.5f); yield return Shot("standing_sheet_bent"); }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        // 짧은 데모(-demo-roster) — 사도 화면 셋 · 도감 버그 확인 · 카드(상세 · 덱 · 보상 · 신탁)만 보고 끝낸다
        //   버그 확인: 도감에서 사도 A 를 한 번 눌러 상세 → 닫기 → 사도 B 를 한 번 눌러 바로 B 상세가 떠야 한다 · 스크롤 자리는 그대로
        IEnumerator Roster_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Shot("lobby");   // 로비 스탠딩 가장자리(흰 테두리) 확인
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(3.0f);
            yield return Shot("village_settled");   // 층 · 적 속성이 다 펼쳐진 뒤
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Shot("party_empty");
            var keys = Roster.All.Where(h => h.Playable).Select(h => h.key).Take(3).ToList();
            yield return Press("slot0", 1.0f);
            yield return Shot("herolist");
            foreach (var k in keys) yield return Press("hero:" + k, 0.4f);
            yield return Wait(0.4f);
            yield return Shot("herolist_picked");
            yield return Press("list.done", 0.8f);
            yield return Wait(0.5f);
            yield return Shot("party_full");
            // 편성 큰 카드의 「고유 효과」 — 짧은 글 판 셋(세로) → 첫 판 자세히
            if (Hot("party.traits0") != null)
            {
                yield return Press("party.traits0", 0.6f);
                yield return Shot("party_traits");
                if (TermPop.DemoMore()) { yield return Wait(0.4f); yield return Shot("party_traits_more"); }
                TermPop.Close(); yield return Wait(0.2f);
            }
            yield return Press("zoom0", 1.0f);
            yield return Shot("hero_detail");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("hero_detail_cards");
            // 카드 글 속 낱말 설명 판(TermPop) — 키워드 하나 · 사도 고유 효과(자세히 펼침) · 생성 카드 하나(있으면)
            if (TermPop.DemoOpen(false)) { yield return Wait(0.5f); yield return Shot("termpop_keyword"); }
            if (TermPop.DemoOpen(false, true)) { yield return Wait(0.5f); yield return Shot("termpop_hero"); if (TermPop.DemoMore()) { yield return Wait(0.4f); yield return Shot("termpop_hero_more"); } }
            if (TermPop.DemoOpen(true)) { yield return Wait(0.5f); yield return Shot("termpop_card"); }
            TermPop.Close();
            if (Hot("detail.hexmore0") != null) { yield return Press("detail.hexmore0", 0.6f); yield return Shot("hero_detail_hexmore"); TermPop.Close(); yield return Wait(0.2f); }
            // 고유 효과 탭 — 짧은 글이 기본, 「자세히」로 펼침
            yield return Press("detail.tab:고유 효과", 1.0f);
            yield return Shot("hero_traits");
            if (Hot("detail.more0") != null) { yield return Press("detail.more0", 0.6f); yield return Shot("hero_traits_more"); }
            // 다른 사도(생성 카드가 있는 사도 — 시온)의 카드 탭에서 생성 카드 판
            var gen = keys.Select(Roster.ByKey).FirstOrDefault(h => h?.CoreId != null && f.P.Data.UniquesOf(h.CoreId).Concat(f.P.Data.Hero(h.CoreId).Starter)
                .Any(id => f.P.Data.Card(id)?.Fx.Any(x => x.K == Core.FxK.Make || x.K == Core.FxK.Transform) == true || f.P.Data.Card(id)?.Evolve != null || f.P.Data.Card(id)?.BondCard != null));
            yield return Press("detail.close", 0.6f);
            if (gen != null)
            {
                f.HeroDetail(gen.key, null, null, "카드");
                yield return Wait(1.2f);
                yield return Shot("hero_detail_cards_gen");
                if (TermPop.DemoOpen(true)) { yield return Wait(0.6f); yield return Shot("termpop_card_gen"); }
                TermPop.Close();
                var cz = FirstHot("detail.card고유 카드");
                if (Hot(cz) != null) { yield return Press(cz, 1.0f); yield return Shot("card_zoom_terms"); if (Hot("zoom.more0") != null) { yield return Press("zoom.more0", 0.5f); yield return Shot("card_zoom_terms_more"); } yield return Press("zoom.close", 0.5f); }
                yield return Press("detail.close", 0.6f);
            }

            // 도감 — 버그 확인(가비아 → 닫기 → 그윈, 한 번씩)
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.0f);
            var sr = f.Stage.ScreenLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            string a = Roster.All.Any(h => h.key == "가비아") ? "가비아" : Roster.All[5].key;
            string b = Roster.All.Any(h => h.key == "그윈") ? "그윈" : Roster.All[6].key;
            // A 가 보이게 내려 둔다(스크롤이 돌아가면 목록이 「초기화」된 것처럼 보인다)
            if (sr != null && Hot("hero:" + a) != null)
            {
                var ca = (RectTransform)Hot("hero:" + a).transform;
                sr.content.anchoredPosition = new Vector2(0, Mathf.Max(0, -ca.anchoredPosition.y - 120));
                yield return Wait(0.3f);
            }
            float y0 = sr != null ? sr.content.anchoredPosition.y : 0;
            yield return Shot("dex_list");
            yield return Press("hero:" + a, 1.0f);
            Check(a, "첫 누름");
            yield return Shot("dex_detail_a");
            yield return Press("detail.close", 0.6f);
            sr = f.Stage.ScreenLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            float y1 = sr != null ? sr.content.anchoredPosition.y : 0;
            Debug.Log($"[Demo] 확인 — 상세에서 돌아온 스크롤 {y0:F0} → {y1:F0} {(Mathf.Abs(y0 - y1) < 2 ? "그대로" : "달라짐(버그)")}");
            yield return Shot("dex_back");
            yield return Press("hero:" + b, 1.0f);
            Check(b, "돌아와 다음 사도 첫 누름");
            yield return Shot("dex_detail_b");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("dex_detail_b_cards");
            yield return Press("detail.close", 0.6f);

            // 도감 탭 — 교주 카드 · 장비(같은 화면 틀) · 상세
            yield return Press("tab:교주 카드", 1.0f);
            yield return Shot("dex_cards");
            yield return Press(FirstHot("dexcard:"), 1.0f);
            yield return Shot("dex_card_zoom");
            yield return Press("zoom.close", 0.5f);
            yield return Press("tab:장비", 1.0f);
            yield return Shot("dex_equips");
            yield return Press("filter:무기", 0.8f);
            yield return Press("filter:전설", 0.8f);
            yield return Shot("dex_equips_filter");
            yield return Press(FirstHot("dexequip:"), 1.0f);
            yield return Shot("dex_equip_detail");
            yield return Press("zoom.close", 0.5f);
            // 적 도감 — 전체 · 마을 하나 + 보스 · 상세
            yield return Press("tab:적", 1.0f);
            yield return Shot("dex_foes");
            yield return Press(FirstHot("dexfoe:"), 1.0f);
            yield return Shot("dex_foe_detail");
            yield return Press("zoom.close", 0.5f);
            yield return Press("filter:보스", 0.8f);
            yield return Shot("dex_foes_boss");
            yield return Press(FirstHot("dexfoe:"), 1.0f);
            yield return Shot("dex_foe_boss_detail");
            yield return Press("zoom.close", 0.5f);
            yield return Press("filter:소환물", 0.8f);
            yield return Press(FirstHot("dexfoe:"), 1.0f);
            yield return Shot("dex_foe_summon_detail");
            yield return Press("zoom.close", 0.5f);
            yield return Press("tab:사도", 1.0f);
            yield return Shot("dex_back_heroes");
            if (tail == "") { StandingSheet(); yield return Wait(1.2f); yield return Shot("standing_sheet"); Destroy(sheet); }
            // 스탠딩 스파인 맞춤(standing_fit.json) — 같은 몸 키 px 로 전신 · 상반신을 나란히(바닥선 · 몸 크기 비 확인)
            LiveSheet(); yield return Wait(1.5f); yield return Shot("standing_live"); Destroy(sheet);

            // 판 하나 — 지도 · 덱 보기 · 카드 크게 · 싸움 · 보상(신탁 카드)
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return Press("party.auto", 0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            yield return Press("deck", 1.2f);
            yield return Shot("deck");
            if (Hot("deck.card0") != null) { yield return Press("deck.card0", 0.9f); yield return Shot("card_zoom"); yield return Press("zoom.close", 0.4f); }
            yield return Press("deck.close", 0.6f);
            yield return OwnerPick();
            // 상점 · 장비 고르기(장비 · 교주 카드 그림) — 캠프 상점을 바로 연다
            f.Camp("campshop");
            yield return Screen_("camp", 1.0f);
            yield return Press("camp.shop");
            yield return Screen_("shop", 1.2f);
            yield return Shot("shop");
            for (int i = 0; i < 8; i++)
            {
                var sb = Hot("shop.item" + i);
                if (sb == null || !sb.Interactable) continue;
                yield return Press("shop.item" + i, 0.9f);
                if (Hot("gear.ok") != null && f.Stage.ModalLayer.childCount > 0) { yield return Wait(0.6f); yield return Shot("gear_shop"); yield return Press("gear.ok", 0.6f); break; }
            }
            if (f.FightScreen != null) { Debug.Log("[Demo] 끝(전투는 -battle 시범이 찍는다)"); yield return Wait(0.4f); Application.Quit(0); yield break; }
            yield return Press("shop.back");
            yield return Screen_("camp", 0.8f);
            yield return Press("camp.leave");
            yield return Screen_("map", 1.2f);
            var reach = f.P.Reachable();
            var map = f.P.Map();
            var fight = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.First();
            yield return Press("node:" + fight, 1.2f);
            yield return GoFight("win");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.4f);
            Application.Quit(0);
        }

        // 교주 카드 주인 고르기 — core API 전이라 가짜 카드(피해 · 방어 배율)로 창만 찍는다. 그 뒤 진짜 교주 카드 한 장을 주인 셋 · 없음으로 나란히(틀 색 · 핀)
        IEnumerator OwnerPick()
        {
            var party = f.P.S.Party.ToList();
            var def = new Core.CardDef { Id = "demo_leader", Name = "교주의 일격", Type = "공격", Cost = 1, Grade = "고급" };
            def.Fx.Add(new Core.Fx { K = Core.FxK.Dmg, Ratio = 0.7, Hits = 2 });
            def.Fx.Add(new Core.Fx { K = Core.FxK.Block, Ratio = 0.8 });
            var fv = new Core.CardView("demo_leader", def, null, 0);
            string got = null;
            holdOwner = true;
            f.OwnerWindow("demo_leader", k => got = k, fv);
            yield return Wait(1.2f);
            yield return Shot("owner_pick");
            if (Hot("owner.traits:" + party[0]) != null) { yield return Press("owner.traits:" + party[0], 0.6f); yield return Shot("owner_traits"); TermPop.Close(); yield return Wait(0.3f); }
            if (party.Count > 1) yield return Press("owner:" + party[1], 0.25f);
            yield return Shot("owner_picked");
            yield return Wait(1.0f);
            Debug.Log($"[Demo] 확인 — 교주 카드 주인 고르기(창): {(got == party.ElementAtOrDefault(1) ? "골랐음 " + got : "안 골라짐(버그)")}");

            var leader = f.P.Data.Cards.Values.Where(c => c.Neutral).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            // 진짜 흐름(core API) — 교주 카드를 얻으면 PendingNeutral → 고르기 창 → AssignNeutral → 덱에 「id@사도」
            if (leader != null && party.Count > 2)
            {
                f.P.Run.GainCard(leader);
                bool fin = false;
                f.PickOwners(() => fin = true);
                yield return Wait(1.2f);
                yield return Shot("owner_real");
                yield return Press("owner:" + party[2], 1.2f);
                var owned = f.P.S.Deck.FirstOrDefault(x => Core.GameData.BaseId(x) == leader);
                Debug.Log($"[Demo] 확인 — 교주 카드 주인(core): 덱 id {owned} · 주인 {Core.GameData.OwnerOf(owned)} · 기다림 {f.P.PendingNeutral ?? "없음"} · 끝 {fin}");
                yield return Press("deck", 1.2f);
                var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                if (sr != null) { sr.verticalNormalizedPosition = 0; yield return Wait(0.4f); }
                yield return Shot("deck_leader_owner");
                yield return Press("deck.close", 0.6f);
            }
            holdOwner = false;
            if (leader != null)
            {
                var (panel, close) = f.Stage.Modal("ownertint", 1100, 470);
                var row = Ui.Rect("row", panel).Fill(20, 20, 20, 20);
                Ui.Row(row, 24, TextAnchor.MiddleCenter, null, false, false);
                foreach (var k in party.Take(3).Prepend(null))
                {
                    var col = Ui.Rect("col", row); col.Pref(220, 420);
                    var c = W.Card(col, f, Core.GameData.WithOwner(leader, k), 220, "card"); c.At(0.5f, 1, 0, 0, 220, 308);
                    var t = Ui.Text(col, k == null ? "주인 없음(금빛 중립)" : Roster.OfCore(k).ko + " 덱", Theme.FsBody, Theme.Sub, TMPro.TextAlignmentOptions.Center);
                    t.rectTransform.At(0.5f, 0, 0, 40, 220, 30);
                }
                yield return Wait(0.8f);
                yield return Shot("owner_tint");
                close();
                yield return Wait(0.4f);
            }
        }

        // 교주 카드 주인 고르기 창이 뜨면 첫 사도로 저절로(긴 데모가 멈추지 않게) — OwnerPick 이 찍는 동안은 쉰다
        bool holdOwner;
        IEnumerator AutoOwner()
        {
            while (true)
            {
                yield return Wait(0.6f);
                if (holdOwner || f.P?.S == null || f.Stage.ModalLayer.Find("modal ownerpick") == null) continue;
                var k = f.P.S.Party.FirstOrDefault(x => Hot("owner:" + x) != null);
                if (k != null) yield return Press("owner:" + k, 0.8f);
            }
        }

        string FirstHot(string prefix) => f.Stage.Hot.Where(kv => kv.Key.StartsWith(prefix) && kv.Value != null).Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault() ?? prefix;

        // 135명 상반신 모아 보기(카드 자르기와 같은 규칙 — CardArt.Upper 0.7 비율 · 위 5.6할) — 머리 자리 판정을 눈으로 확인
        GameObject sheet;
        void StandingSheet()
        {
            var layer = Ui.Rect("modal standingsheet", f.Stage.ModalLayer).Fill();
            sheet = layer.gameObject;
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            var size = f.Stage.Size;
            int cols = 20;
            int rows = Mathf.CeilToInt(Roster.All.Count / (float)cols);
            float ch = Mathf.Floor(Mathf.Min((size.x - 20) / cols / 0.7f, (size.y - 10) / rows - 12)), cw = Mathf.Floor(ch * 0.7f);
            int i = 0;
            foreach (var h in Roster.All.OrderBy(x => x.art, StringComparer.Ordinal))
            {
                float x = 10 + (i % cols) * cw, y = -6 - (i / cols) * (ch + 12);
                var sp = CardArt.Upper(h.art, 0.7f, 0.56f);
                var im = Ui.Img(layer, sp ?? h.Icon, sp != null ? Color.white : Color.white.A(0.5f), h.key);
                im.rectTransform.At(0, 1, x + 1, y, cw - 2, ch);
                var t = Ui.Text(layer, h.ko, 10, sp != null ? Theme.Ink : Theme.Bad, TMPro.TextAlignmentOptions.Top);
                t.rectTransform.At(0, 1, x, y - ch, cw, 12); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 6; t.fontSizeMax = 10;
                i++;
            }
        }

        // 키 비교 판(-sheet-compare) — 묶음마다 [티그(영웅) | 원판 | 다른 판], 같은 배율 · 같은 바닥선, 머리 꼭대기 가는 선(금 = 티그 기준)
        void CompareSheet()
        {
            var layer = Ui.Rect("modal comparesheet", f.Stage.ModalLayer).Fill();
            sheet = layer.gameObject;
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            const string tig = "tighero";
            var groups = new[]
            {
                new[] { "vivi", "vividivine" }, new[] { "daya", "dayapureshine" }, new[] { "erpin", "erpinroyale" },
                new[] { "ed", "edrehab" }, new[] { "diana", "dianayester" }, new[] { "ui", "uimemory" },
                new[] { "amelia", "ameliar41" }, new[] { "ner", "nerrage" }, new[] { "lethe", "rollett" },
            };
            var all = groups.SelectMany(g => g).Append(tig).ToList();
            float tall = StandingFit.Tallest(all);
            var size = f.Stage.Size;
            int rows = 3, perRow = 3;
            float top = 56, rowH = (size.y - top - 10) / rows, labelH = 46, figH = rowH - labelH - 8;
            float groupW = (size.x - 40) / perRow, cw = (groupW - 30) / 3;
            var head = Ui.Title(layer, "키 비교 — 묶음마다 티그(영웅) · 원판 · 다른 판 (손보정 · 짝 적용, 같은 배율 · 같은 바닥선) · 금빛 선 = 티그(영웅) 머리 꼭대기, 흰 선 = 그 사도 머리 꼭대기", Theme.FsMd, Theme.Gold, TMPro.TextAlignmentOptions.MidlineLeft);
            head.rectTransform.At(0, 1, 24, -10, size.x - 48, 36);
            StandingFit.TryGet(tig, out var tf);
            var log = new System.Text.StringBuilder("[Demo] 키 비교(티그(영웅) 몸 " + tf.Body.ToString("0") + "):");
            for (int gi = 0; gi < groups.Length; gi++)
            {
                int r = gi / perRow, c = gi % perRow;
                float gx = 20 + c * groupW, gy = -(top + r * rowH);
                var box = Ui.Img(layer, Theme.Round, Theme.NavyCell.A(0.45f), "group"); box.rectTransform.At(0, 1, gx + 4, gy, groupW - 8, rowH - 6);
                var members = new[] { tig }.Concat(groups[gi]).ToArray();
                float floorY = gy - figH;   // 칸 아래(바닥선)
                float tigTop = 0;
                for (int i = 0; i < members.Length; i++)
                {
                    var a = members[i];
                    float x = gx + 15 + i * cw;
                    var slot = Ui.Rect("fig " + a, layer).At(0, 1, x, gy, cw, figH);
                    SpineUi.Standing(slot, a, cw, figH, StandMode.Full, tall);
                    StandingFit.TryGet(a, out var fit);
                    if (StandingFit.Solve(a, new Rect(0, 0, cw, figH), StandMode.Full, tall, 0, out var k, out var o))
                    {
                        float yTop = o.y + fit.TopY * k;   // 칸 아래에서 머리 꼭대기까지
                        if (i == 0) tigTop = yTop;
                        var ln = Ui.Img(layer, Theme.White, (i == 0 ? Theme.Gold : Color.white).A(i == 0 ? 0.9f : 0.75f), "headline");
                        if (i == 0) ln.rectTransform.At(0, 1, gx + 10, floorY + tigTop, groupW - 20, 2);
                        else ln.rectTransform.At(0, 1, x + 6, floorY + yTop, cw - 12, 2);
                    }
                    string ko = Roster.All.FirstOrDefault(h => h.art == a)?.ko ?? fit.Hero ?? a;
                    float ratio = fit.Body / Mathf.Max(1, tf.Body);
                    var t = Ui.Text(layer, $"<b>{ko}</b>\n<size=80%><color={Theme.SubTag}>배율 ×{fit.Scale:0.00} · 몸 {fit.Body:0} · 티그 대비 {ratio * 100:0}%</color></size>", Theme.FsSm, i == 0 ? Theme.Gold : Theme.Ink, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x - 6, floorY - 4, cw + 12, labelH); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 9; t.fontSizeMax = Theme.FsSm;
                    if (i > 0) log.Append($" {ko} {fit.Body:0}({ratio * 100:0}%)");
                }
                var fl = Ui.Img(layer, Theme.White, Theme.Gold.A(0.6f), "floor"); fl.rectTransform.At(0, 1, gx + 10, floorY, groupW - 20, 2);
            }
            // 이름 글은 맨 위로(바위 · 탈것이 바닥선 아래로 내려와도 글을 덮지 않게)
            foreach (var tx in layer.GetComponentsInChildren<TMPro.TextMeshProUGUI>()) tx.transform.SetAsLastSibling();
            Debug.Log(log.ToString());
        }

        void LiveSheet()
        {
            var layer = Ui.Rect("modal livesheet", f.Stage.ModalLayer).Fill();
            sheet = layer.gameObject;
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            // 숙인 · 기운 사도(얼굴 잘림 제보) + 키 짝(네르 · 네르_빡침)을 먼저 — 줄: 전신 · 편성 무릎께 · 카드 그림 · 얼굴 칸
            //   -sheet-heights: 키 짝(원판 · 다른 판 나란히) + 키 손보정 사도
            var want = Has("-sheet-heights")
                ? new[] { "vivi", "vividivine", "daya", "dayapureshine", "erpin", "erpinroyale", "ed", "edrehab", "diana", "dianayester", "ui", "uimemory", "amelia", "ameliar41", "ner", "nerrage", "lethe", "rollett" }
                : new[] { "selline", "diana", "dianayester", "epica", "ran", "naia", "rude", "ed", "sist", "ner", "nerrage" };
            var arts = want.Concat(Roster.All.Select(h => h.art)).Where(a => a != null && SpineUi.Data("st_" + a) != null).Distinct().Take(Has("-sheet-heights") ? want.Length : 12).ToList();
            var size = f.Stage.Size;
            float cw = Mathf.Min(220, (size.x - 40) / Mathf.Max(1, arts.Count)), fullH = size.y * 0.3f, kneeH = (cw - 10) * 1.4f, cardH = (cw - 16) / 0.71f, faceS = Mathf.Min(70, cw - 30);
            float tall = StandingFit.Tallest(arts);
            var floor = Ui.Img(layer, Theme.White, Theme.Gold.A(0.6f), "floor"); floor.rectTransform.At(0, 1, 20, -(14 + fullH), size.x - 40, 2);
            for (int i = 0; i < arts.Count; i++)
            {
                var a = arts[i];
                float x = 20 + i * cw, y = -14;
                var full = Ui.Rect("full " + a, layer).At(0, 1, x, y, cw, fullH);
                SpineUi.Standing(full, a, cw, fullH, StandMode.Full, tall);
                y -= fullH + 6;
                var knee = Ui.Img(layer, Theme.Round, Theme.NavyCell, "knee " + a); knee.rectTransform.At(0, 1, x + 5, y, cw - 10, kneeH);
                knee.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
                SpineUi.Standing(knee.rectTransform, a, cw - 10, kneeH, StandMode.Knee, tall);
                y -= kneeH + 6;
                var card = Ui.Img(layer, CardArt.Upper(a, 0.71f, 0.56f), Color.white, "card " + a); card.rectTransform.At(0, 1, x + 8, y, cw - 16, cardH);
                y -= cardH + 6;
                var face = Ui.Img(layer, Theme.S("circle"), Theme.NavyCell, "face " + a); face.rectTransform.At(0, 1, x + (cw - faceS) / 2, y, faceS, faceS);
                face.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
                var fi = Ui.Img(face.rectTransform, CardArt.Upper(a, 1f, 0.34f), Color.white, "pic"); fi.rectTransform.Fill();
                y -= faceS + 2;
                StandingFit.TryGet(a, out var fit);
                var t = Ui.Text(layer, $"{a}{(fit.Bent ? " ·숙임" : "")}\n×{fit.Scale:0.00} 몸 {fit.Body:0}", 11, fit.Bent ? Theme.Gold : Theme.Ink, TMPro.TextAlignmentOptions.Top);
                t.rectTransform.At(0, 1, x, y, cw, 30);
            }
        }

        void Check(string key, string what)
        {
            var open = f.Stage.ModalLayer.Cast<Transform>().LastOrDefault(t => t.name == "modal herodetail");
            var name = open != null ? open.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Select(t => t.text).FirstOrDefault(t => t == Roster.ByKey(key)?.ko) : null;
            Debug.Log($"[Demo] 확인 — {what}: {key} 상세 {(open != null && name != null ? "열림" : "안 열림(버그)")}");
        }

        bool bailing;
        void Bail(int code, string why)
        {
            if (bailing) return;
            bailing = true;
            Debug.LogError($"[Demo] 멈춤({code}) — {why}");
            StartCoroutine(BailCo(code));
        }
        IEnumerator BailCo(int code)
        {
            yield return Shot("zz_bail_" + code, false);
            Application.Quit(code);
        }
        IEnumerator Watchdog(float limit)
        {
            yield return new WaitForSecondsRealtime(limit);
            Bail(3, $"{limit:F0}초가 넘었습니다 (지금 화면 {f.Stage.Current})");
        }

        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length - 1; i++) if (a[i] == n) return a[i + 1]; return null; }
        static bool Has(string n) => Environment.GetCommandLineArgs().Contains(n);

        IEnumerator Shot(string name, bool once = true)
        {
            if (once && !seen.Add(name)) yield break;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture(int.TryParse(Arg("-supersize"), out var ss) && ss > 1 ? ss : 1);   // -supersize 2 = 1920×1080 창을 3840×2160 으로(모니터보다 큰 4K 확인)
            var path = Path.Combine(dir, $"{++no:D2}_{name}{tail}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log("[Capture] " + Path.GetFileName(path));
        }

        IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        IEnumerator Screen_(string name, float settle = 1.1f, float timeout = 25)
        {
            float t = 0;
            while ((f.Stage.Current != name || f.Stage.Busy) && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            if (t >= timeout) Debug.LogWarning($"[Demo] 화면 {name} 을 기다리다 지침 (지금 {f.Stage.Current})");
            yield return Wait(settle);
        }

        Btn Hot(string key) => f.Stage.Hot.TryGetValue(key, out var b) && b != null ? b : null;

        IEnumerator Press(string key, float after = 0.5f)
        {
            var b = Hot(key);
            if (b == null) { Debug.LogWarning("[Demo] 버튼 없음: " + key); yield break; }
            Debug.Log("[Demo] 누름 " + key);
            yield return b.Press();
            yield return Wait(after);
        }

        // 짧은 판(-demo-quick) — 새 규칙이 판 처음부터 싸움까지 이어지는지만: 로비 → 마을 공개(판 속성 · 두 보스) → 편성(추천) → 판 열기
        //   (미리 보인 판 속성 · 보스 줄 = 실제 판의 것인지 대 본다) → 지도 → 싸움 둘(전투 화면이 붙으면 그쪽 봇) → 지도로 돌아오면 끝
        IEnumerator Quick()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(3.0f);
            yield return Shot("quick_village");
            string natShown = f.FoeNature;
            var bossShown = f.FoeBosses?.Select(l => string.Join(",", l)).ToList();
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Press("party.auto", 0.6f);
            yield return Shot("quick_party");
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            yield return Shot("quick_map");
            var run = f.P.Run;
            var bossReal = run.Bosses.Select(l => string.Join(",", l)).ToList();
            bool same = natShown == run.EnemyNature && bossShown != null && bossShown.SequenceEqual(bossReal);
            Debug.Log($"[Demo] 판 속성 화면 {natShown} · 실제 {run.EnemyNature} · 보스 화면 [{string.Join(" | ", bossShown ?? new List<string>())}] · 실제 [{string.Join(" | ", bossReal)}] · 클론 사도 [{string.Join(", ", run.BossHeroes.Select(x => x ?? "-"))}] → {(same ? "일치" : "어긋남")}");
            if (!same) { Bail(4, "마을 공개 화면의 판 속성 · 보스가 실제 판과 다르다"); yield break; }
            int fights = 0;
            for (int step = 0; step < 6 && fights < 2; step++)
            {
                float t = 0;
                while (t < 25 && (f.Stage.Busy || f.Stage.Current != "map")) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Stage.Current != "map") break;
                var reach = f.P.Reachable();
                var map = f.P.Map();
                var pick = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "elite") ?? reach.First();
                var type = Core.Run.NodeById(map, pick).Type;
                yield return Press("node:" + pick, 0.2f);
                yield return Wait(1f);
                switch (type)
                {
                    case "fight": case "elite": case "boss": yield return GoFight("win", type); fights++; break;
                    case "event": yield return GoEvent(); break;
                    case "camp": case "campshop": yield return GoCamp(type); break;
                }
                Debug.Log($"[Demo] {type} 지남 — 화면 {f.Stage.Current}");
            }
            yield return Wait(1.0f);
            yield return Shot("quick_after");
            Debug.Log($"[Demo] 끝(짧은 판 — 싸움 {fights}번)");
            yield return Wait(0.4f);
            Application.Quit(fights > 0 ? 0 : 5);
        }

        IEnumerator Run()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("lobby");
            yield return Press("hero", 0.6f);
            yield return Shot("lobby_touch");
            // 버튼 상태 — 메뉴 하나에 올림(금 테두리)을 켜 둔 채 찍는다
            var hv = Hot("dex");
            if (hv != null) { hv.Hover(true); yield return Wait(0.45f); yield return Shot("lobby_hover"); hv.Hover(false); yield return Wait(0.2f); }
            yield return Press("start");

            yield return Screen_("village", 0.4f);
            yield return Shot("village_roll");
            yield return Wait(1.6f);
            yield return Shot("village");
            yield return Press("go");

            yield return Screen_("party", 1.2f);
            yield return Shot("party_empty");
            var keys = Roster.All.Where(h => h.Playable).Select(h => h.key).Take(3).ToList();
            // 칸을 누르면 사도 목록(빠른 편성) → 셋 넣기 → 완료
            yield return Press("slot0", 1.0f);
            yield return Shot("herolist");
            foreach (var k in keys) yield return Press("hero:" + k, 0.5f);
            yield return Wait(0.5f);
            yield return Shot("herolist_picked");
            yield return Press("list.done", 0.8f);
            yield return Wait(0.6f);
            yield return Shot("party_full");
            yield return Press("zoom0", 1.0f);
            yield return Shot("hero_detail");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("hero_detail_cards");
            yield return Press("detail.tab:고유 효과", 0.8f);
            yield return Shot("hero_detail_traits");
            yield return Press("detail.close", 0.8f);
            yield return Press("party.go");

            yield return Travel(true);
            yield return Screen_("end_clear", 2.5f);
            yield return Shot("end_clear");

            // 로비 · 도감 · 설정
            yield return Press("end.lobby");
            yield return Screen_("lobby", 1.2f);
            yield return Press("dex");
            yield return Screen_("dex", 1.2f);
            yield return Shot("dex_list");
            yield return Press("hero:" + keys[0], 0.8f);
            yield return Press("list.detail", 1.0f);
            yield return Shot("dex_hero");
            yield return Press("detail.close", 0.8f);
            yield return Press("tab:교주 카드");
            yield return Wait(1.2f);
            yield return Shot("dex_cards");
            yield return Press("tab:장비");
            yield return Wait(1.2f);
            yield return Shot("dex_equips");
            yield return Press("back");
            yield return Screen_("lobby", 1f);
            yield return Press("settings", 0.8f);
            yield return Shot("settings");
            if (!Has("-no-display-try"))
            {
                // 해상도 바꿔 보기 → 「이 화면으로 둘까요?」 → 되돌리기(데모는 창 크기를 남기지 않는다)
                int pick = DisplayOptions.PresetIndex == 1 ? 2 : 1;
                if (DisplayOptions.Fits(pick))
                {
                    yield return Press("disp.res" + pick, 1.4f);
                    yield return Shot("settings_keep");
                    yield return Press("disp.revert", 1.4f);
                    yield return Shot("settings_reverted");
                }
            }
            yield return Press("settings.close");

            // 새 판에서 지기 · 이어하기
            yield return Press("start");
            yield return Screen_("village", 2f);
            yield return Press("go");
            yield return Screen_("party", 1f);
            yield return Press("slot0", 0.9f);
            foreach (var k in keys.AsEnumerable().Reverse()) yield return Press("hero:" + k, 0.3f);
            yield return Press("list.done", 0.8f);
            yield return Press("party.go");
            yield return Screen_("map", 1.6f);
            yield return Press("settings", 0.6f);
            yield return Press("settings.lobby");
            yield return Screen_("lobby", 1.2f);
            yield return Shot("lobby_resume");
            yield return Press("start");
            yield return Screen_("map", 1.4f);
            yield return Shot("map_resumed");
            var mp = f.P.Map();
            var reachNow = f.P.Reachable();
            var any = reachNow.FirstOrDefault(id => Core.Run.NodeById(mp, id)?.Type == "fight") ?? reachNow.FirstOrDefault();
            yield return Press("node:" + any, 1.5f);
            yield return GoFight("lose");
            yield return Screen_("end_lose", 2.2f);
            yield return Shot("end_lose");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.5f);
            Application.Quit(0);
        }

        // 한 판을 끝까지 — 지도마다 아직 못 본 칸 종류를 먼저 고른다
        IEnumerator Travel(bool win)
        {
            var wanted = new List<string> { "fight", "event", "campshop", "camp", "elite" };
            int guard = 0;
            while (guard++ < 60)
            {
                float t = 0;
                while (t < 25 && (f.Stage.Busy || (f.Stage.Current != "map" && !f.Stage.Current.StartsWith("end")))) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Stage.Current != "map") break;
                yield return Wait(1.5f);
                yield return Shot(f.P.S.Floor == 0 ? "map" : "map_floor2");
                if (f.P.S.Floor == 1 && !seen.Contains("deck") && Hot("deck") != null)
                {
                    // 덱 보기 — 사도별 묶음(기본 → 고유) · 맨 끝 교주 카드 · 카드 크게
                    yield return Press("deck", 1.2f);
                    yield return Shot("deck");
                    if (Hot("deck.card0") != null) { yield return Press("deck.card0", 0.9f); yield return Shot("card_zoom"); yield return Press("zoom.close", 0.4f); }
                    yield return Press("deck.close", 0.6f);
                }
                var reach = f.P.Reachable();
                var map = f.P.Map();
                string pick = null;
                foreach (var w in wanted)
                {
                    pick = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == w);
                    if (pick != null) { wanted.Remove(w); break; }
                }
                pick ??= reach.OrderBy(id => Core.Run.NodeById(map, id)?.Type == "campshop" ? 0 : Core.Run.NodeById(map, id)?.Type == "event" ? 2 : 1).First();
                var type = Core.Run.NodeById(map, pick).Type;
                if (!seen.Contains("map_walk"))
                {
                    var b = Hot("node:" + pick);
                    StartCoroutine(b.Press());
                    yield return Wait(0.62f);
                    yield return Shot("map_walk");
                }
                else yield return Press("node:" + pick, 0.2f);
                yield return Wait(1f);
                switch (type)
                {
                    case "fight": case "elite": case "boss": yield return GoFight(win ? "win" : "lose", type); break;
                    case "event": yield return GoEvent(); break;
                    case "camp": case "campshop": yield return GoCamp(type); break;
                }
                if (f.Stage.Current != null && f.Stage.Current.StartsWith("end")) break;
            }
        }

        IEnumerator GoFight(string mode, string type = "fight")
        {
            if (f.FightScreen != null)
            {
                // 전투 화면이 붙었다 — 그쪽 봇이 싸운다(캡처도 그쪽이 Snap 으로). 판 화면으로 돌아올 때까지 기다린다
                FightMode = mode;
                float t = 0;
                while (!f.Fighting && t < 15) { t += Time.unscaledDeltaTime; yield return null; }
                if (!f.Fighting) { Debug.LogWarning("[Demo] 싸움이 열리지 않았습니다 (지금 화면 " + f.Stage.Current + ")"); yield break; }
                t = 0;
                while (f.Fighting && t < 400) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Fighting) { Bail(3, "전투가 400초 안에 끝나지 않았습니다"); yield break; }
                FightMode = "win";
                yield return Wait(0.6f);
                if (mode == "lose") yield break;
                if (f.P.S.Event != null && f.P.S.Event.Phase != "choose") yield break;
            }
            else
            {
            yield return Screen_("fight", 1.2f);
            yield return Shot(type == "boss" ? (f.P.S.Floor == 0 ? "fight_boss" : "fight_boss2") : type == "elite" ? "fight_elite" : "fight");
            if (mode == "lose") { yield return Press("fight.lose", 0.6f); yield return Press("confirm.yes", 0.4f); }
            else yield return Press("fight.win", 0.1f);
            yield return Wait(0.45f);
            yield return Shot(mode == "lose" ? "banner_lose" : "banner_win");
            if (mode == "lose") yield break;
            if (f.P.S.Event != null && f.P.S.Event.Phase != "choose") yield break;   // 이벤트 싸움 — 결과로 돌아간다
            }
            yield return Screen_("reward", 1.2f);
            if (f.Stage.Current != "reward") yield break;
            yield return Shot("reward");
            yield return Press("reward.gold", 0.9f);
            if (Hot("reward.equip") != null && Hot("reward.equip").Interactable)
            {
                yield return Press("reward.equip", 0.8f);
                yield return Gear("gear_reward");
            }
            for (int i = 0; i < 4 && Hot("reward.glow") != null; i++)
            {
                yield return Shot("reward_glow");
                yield return Press("reward.glow", 0.9f);
                yield return Shot("reward_glow_pick");
                yield return Press("glow.opt0", 0.9f);
            }
            yield return Shot("reward_taken");
            bool boss = f.P.IsBoss;
            yield return Press("reward.next", 0.5f);
            if (boss && !f.P.IsLastFloor)
            {
                yield return Screen_("bosscopy", 1.2f);
                if (f.Stage.Current == "bosscopy")
                {
                    yield return Shot("bosscopy");
                    yield return Press("copy0", 0.6f);
                    yield return Shot("bosscopy_pick");
                    yield return Press("copy.go", 0.5f);
                }
            }
        }

        IEnumerator Gear(string shot)
        {
            yield return Wait(0.6f);
            yield return Shot(shot);
            yield return Press("gear:1", 0.4f);
            yield return Shot(shot + "_pick");
            var cmp = Hot("gear.cmp");
            if (cmp != null && !seen.Contains("gear_compare"))
            {
                yield return cmp.LongPress();
                yield return Wait(0.6f);
                yield return Shot("gear_compare");
                yield return Press("compare.close", 0.4f);
            }
            yield return Press("gear.ok", 0.8f);
        }

        IEnumerator GoEvent()
        {
            yield return Wait(1.2f);
            if (f.Stage.Current != "event") yield break;
            yield return Screen_("event", 1.2f);
            yield return Shot("event");
            // 잠기지 않은 첫 선택지(떠나기가 아닌 것)
            int pick = 0;
            for (int i = 0; i < 6; i++) { var b = Hot("event.opt" + i); if (b != null && b.Interactable) { pick = i; break; } }
            yield return Press("event.opt" + pick, 0.6f);
            yield return Shot("event_pick");
            yield return Press("event.ok", 0.8f);
            if (f.Stage.Current == "fight" || f.Fighting) { yield return GoFight("win"); yield return Screen_("event", 1.2f); }
            for (int i = 0; i < 4; i++)
            {
                yield return Wait(0.5f);
                if (f.Stage.ModalLayer.childCount > 0 && Hot("gear.ok") != null) { yield return Gear("gear_event"); continue; }
                if (Hot("pending0") != null) { yield return Shot("event_pending"); yield return Press("pending0", 0.8f); continue; }
                break;
            }
            yield return Wait(0.6f);
            yield return Shot("event_result");
            yield return Press("event.leave", 0.4f);
        }

        IEnumerator GoCamp(string kind)
        {
            yield return Screen_("camp", 1.4f);
            yield return Shot(kind == "campshop" ? "camp_shop" : "camp");
            bool canTrain = Hot("camp.trainopen") != null && Hot("camp.trainopen").Interactable;
            bool canRest = Hot("camp.rest") != null && Hot("camp.rest").Interactable;
            if (canTrain && (seen.Contains("camp_rest") || !canRest))
            {
                yield return Press("camp.trainopen", 0.9f);
                yield return Shot("camp_train");
                yield return Press("camp.train0", 1f);
            }
            else if (canRest) { yield return Press("camp.rest", 1.2f); yield return Shot("camp_rest"); }
            if (!seen.Contains("gearview"))
            {
                yield return Press("camp.gear", 0.8f);
                yield return Shot("gearview");
                yield return Press("gearview.close", 0.4f);
            }
            if (kind == "campshop")
            {
                yield return Press("camp.shop");
                yield return Screen_("shop", 1.4f);
                if (!seen.Contains("shop"))
                {
                    yield return Shot("shop");
                    // 살 수 있는 것 하나(장비면 끼기 창)
                    for (int i = 0; i < 8; i++)
                    {
                        var b = Hot("shop.item" + i);
                        if (b == null || !b.Interactable) continue;
                        yield return Press("shop.item" + i, 0.9f);
                        if (Hot("gear.ok") != null && f.Stage.ModalLayer.childCount > 0) yield return Gear("gear_shop");
                        break;
                    }
                    yield return Wait(0.8f);
                    yield return Shot("shop_bought");
                    if (Hot("shop.remove") != null && Hot("shop.remove").Interactable)
                    {
                        yield return Press("shop.remove", 0.9f);
                        yield return Shot("shop_remove");
                        yield return Press("remove0", 0.5f);
                        yield return Press("confirm.yes", 0.9f);
                    }
                }
                yield return Press("shop.back");
                yield return Screen_("camp", 1f);
            }
            yield return Press("camp.leave");
        }
    }
}
