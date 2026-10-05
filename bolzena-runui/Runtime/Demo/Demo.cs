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
            bool shortRun = Has("-demo-roster");
            float limit = float.TryParse(Arg("-demo-timeout"), out var lim) ? lim : shortRun ? 150f : 420f;
            StartCoroutine(Watchdog(limit));
            StartCoroutine(shortRun ? Roster_() : Run());
        }

        // 짧은 데모(-demo-roster) — 사도 화면 셋 · 도감 버그 확인 · 카드(상세 · 덱 · 보상 · 신탁)만 보고 끝낸다
        //   버그 확인: 도감에서 사도 A 를 한 번 눌러 상세 → 닫기 → 사도 B 를 한 번 눌러 바로 B 상세가 떠야 한다 · 스크롤 자리는 그대로
        IEnumerator Roster_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
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
            yield return Press("zoom0", 1.0f);
            yield return Shot("hero_detail");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("hero_detail_cards");
            yield return Press("detail.close", 0.6f);

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
            yield return Press("tab:사도", 1.0f);
            yield return Shot("dex_back_heroes");
            if (tail == "") { StandingSheet(); yield return Wait(1.2f); yield return Shot("standing_sheet"); Destroy(sheet); }

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
