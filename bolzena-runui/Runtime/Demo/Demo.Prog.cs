using System.Collections;
using System.IO;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 성장 화면 캡처(-demo-prog [-prog-samples 폴더]) — 서버 없이. 2026-10-21 밤 「판 밖 성장 = 잠재력만」.
    //   성장 켬(콘텐츠에 성장 표)이면 로비 잠재력 판 → 모험 시작 → 도전 사다리(단계 고르기) → 마을 공개 → 판(지도 머리 단계) → 판 끝 잠재력 재료
    //   → 도감 사도 상세(잠재력 요약 · 「잠재력」 단추) → 「잠재력」 탭(10칸 · 칸 고르기 · 열기 · 잠긴 칸) → 카드 상세 실제 수치.
    //   서버 응답은 -prog-samples 폴더의 JSON(test/run-local.mjs 가 로컬 성장 켬 서버에서 받아 남긴 진짜 응답 — view · earn · pot_view · potential)을 그대로 넣는다.
    //   한 바퀴 돌리기: Web/test/prog-ui.mjs [--off] [--phone].
    // 성장 끔(공개판 콘텐츠)이면 같은 길을 가며 단계 · 잠재력이 하나도 안 보이는지 단언한다(스위치 끔 = 지금 게임).
    // 어디서나: 없앤 성장(레벨 · 진급 · 교주 레벨 · 던전 · 왕사탕 · 모카롱 · 교본 · 골드)의 글 · 단추가 성장 화면에 없는지 본다.
    public partial class Demo
    {
        /// <summary>그 이름의 화면 조각 아래 글에 없앤 성장 낱말이 있으면 그 글(없으면 null).</summary>
        string GoneWords(string under)
        {
            string[] gone = { "교주 Lv", "교주 레벨", "진급", "레벨업", "왕사탕", "모카롱", "교본", "골드", "던전", "Lv." };
            foreach (var r in f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Where(x => under == null || x.name == under))
                foreach (var t in r.GetComponentsInChildren<TMP_Text>(true))
                    foreach (var w in gone) if (t.text != null && t.text.Contains(w)) return $"{r.name}: 「{w}」 — {t.text}";
            return null;
        }

        IEnumerator Prog_()
        {
            int f0 = fails;
            yield return Wait(0.5f);
            bool on = ProgStore.On;
            Debug.Log($"[Demo] 성장 데모 — 스위치 {(on ? "켬(성장 표 있음)" : "끔(성장 표 없음)")}");
            string sd = Arg("-prog-samples");
            JObject J(string n) { try { var p = sd != null ? Path.Combine(sd, n + ".json") : null; return p != null && File.Exists(p) ? JObject.Parse(File.ReadAllText(p)) : null; } catch { return null; } }
            var view = J("view"); var earn = J("earn");
            if (on) Expect(view != null && earn != null, "서버 응답 견본(-prog-samples view · earn)");
            if (on && view != null) ProgStore.DemoSet(view, null);

            f.Lobby();
            yield return Screen_("lobby", 1.4f);
            yield return Shot(on ? "prog_lobby" : "progoff_lobby");
            var lobbyProg = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "prog");
            Expect(lobbyProg == on, on ? "로비에 잠재력 판" : "성장 끔 — 로비에 잠재력 판 없음");
            Expect(Hot("dungeon") == null, "로비에 던전 단추 없음");
            if (on) Expect(GoneWords("prog") == null, "로비 잠재력 판 — 없앤 성장 글 없음 " + GoneWords("prog"));

            yield return Press("start", 0.3f);
            yield return Screen_("vilpick", 1.6f);
            if (on)
            {   // 도전 사다리 — 마을 고르기 상세 판 안의 단계 줄(2026-10-22 — 따로 고르는 화면 없음). 처음 고른 것은 「무작위」 마을이어도 줄이 있다
                yield return Shot("prog_vilpick_tier");
                bool compact = Theme.Compact;   // 폰 — 「− 단계 칩 +」 한 줄
                Expect((compact ? Hot("tier.minus") != null && Hot("tier.plus") != null : Hot("tier.1") != null && Hot("tier.9") != null && !Hot("tier.9").Interactable) && Hot("vilpick.go") != null, compact ? "마을 고르기(무작위) 상세 판(폰) — 단계 − · + · 출발 단추" : "마을 고르기(무작위) 상세 판 — 단계 줄 1~9 · 잠긴 단계 9 · 출발 단추");
                Expect(Hot("tier.go") == null, "따로 고르는 단계 화면 없음");
                var tierTexts = f.Stage.transform.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text ?? "").ToList();
                var tierChips = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Count(r => r.name.StartsWith("chip mm"));
                Expect(tierChips == 3 && !tierTexts.Any(t => t.Contains("알맞은 성장")) && tierTexts.Any(t => t.Contains("잠재력을 다 연 기준")), $"단계 줄 — 고른 단계의 완주 마시멜로 칩 셋({tierChips}) · 「잠재력을 다 연 기준」 한 줄");
                Expect(f.Stage.transform.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i => i.name == "ic" && i.sprite != null && i.transform.parent != null && i.transform.parent.name.StartsWith("chip mm")) == tierChips, "마시멜로 그림이 칩마다 들어갔다(RunArt/Currency/mm1~3)");
                Expect(GoneWords("tierrow") == null, "단계 줄 — 없앤 성장 글 없음 " + GoneWords("tierrow"));
                if (compact) { for (int k = 0; k < Tiers.MAX && f.RunTier > 1; k++) yield return Press("tier.minus", 0.3f); }
                else yield return Press("tier.1", 0.6f);
                Expect(f.RunTier == 1, $"단계 1 을 고르면 RunTier 1({f.RunTier})");
                var vk = f.P.Villages.FirstOrDefault()?.Id;
                if (vk != null) { yield return Press("vilpick." + vk, 0.8f); Expect((Hot("tier.1") != null || Hot("tier.minus") != null) && f.RunTier == 1, "마을을 바꿔도 단계 줄 · 고른 단계 그대로"); }
                yield return Shot("prog_tier_pick1");
            }
            else Expect(Hot("tier.1") == null && f.Stage.transform.GetComponentsInChildren<RectTransform>(true).All(r => r.name != "tierrow"), "성장 끔 — 마을 고르기에 단계 줄 없음(지금 공개판 그대로)");
            yield return Press("vilpick.go", 0.3f);
            yield return Screen_("village", 2.4f);
            Expect(f.Stage.Current == "village", "마을 고르기 → 마을 공개");
            Expect(f.RunTier == (on ? 1 : 0), $"고른 단계 {f.RunTier}");

            // 판 — 데모용 편성(아야 · 이드 · 실비아)으로 곧장 연다
            var party = new System.Collections.Generic.List<string> { "아야", "이드", "실비아" }.Where(k => f.P.Data.Hero(k) != null).ToList();
            if (party.Count < 3) party = f.P.Data.Heroes.Keys.Take(3).ToList();
            string village = f.P.RollVillage();
            f.FoeNature = null;   // 마을을 새로 굴렸으니 적 속성 없이(씨앗은 판이 정한다)
            f.StartRun(party, village);
            yield return Screen_("map", 1.4f);
            yield return Shot(on ? "prog_map" : "progoff_map");
            Expect(f.P.S.Tier == (on ? 1 : 0), $"판 단계 {f.P.S.Tier}");
            int baseHp = f.P.Run.Data.Heroes.Values.Where(h => party.Contains(h.Id)).Sum(h => h.Hp);
            Expect(f.P.S.PartyMaxHp >= baseHp, $"파티 최대 HP {f.P.S.PartyMaxHp} — 공개판 기본치({baseHp}) 이상(레벨 없음 · 잠재력은 덤)");
            if (!on) Expect(f.P.S.Prog == null, "성장 끔 — 판에 성장 몫 없음");

            // 판 끝 — 잠재력 재료
            f.End("clear");
            yield return Screen_("end_clear", 0.6f);
            if (on && earn != null) ProgStore.DemoSet(view, earn);
            yield return Wait(1.6f);
            yield return Shot(on ? "prog_end" : "progoff_end");
            var endProg = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "progreward");
            Expect((endProg != null) == on, on ? "판 끝 잠재력 재료 판" : "성장 끔 — 판 끝 재료 판 없음");
            if (on && endProg != null)
            {
                string et = string.Join(" ", endProg.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text));
                int endChips = endProg.GetComponentsInChildren<RectTransform>(true).Count(r => r.name.StartsWith("chip mm"));
                Expect(et.Contains("마시멜로") && endChips == 3 && GoneWords("progreward") == null && !et.Contains("경험") && !et.Contains("★"), $"판 끝 — 마시멜로 칩 셋({endChips}) · 경험 · 모카롱 · 골드 · 교본 · 성격 재료 없음: " + et.Replace("\n", " / "));
            }

            // 도감 → 사도 상세(아야) — 잠재력 요약 한 줄 · 「잠재력」 단추(레벨 · 진급 단추 없음)
            f.Dex(null);
            yield return Screen_("dex", 1.2f);
            var aya = Roster.OfCore("아야") ?? Roster.Players.FirstOrDefault();
            f.HeroDetail(aya.key, null, null);
            yield return Wait(1.8f);
            yield return Shot(on ? "prog_dex_detail" : "progoff_dex_detail");
            Expect((Hot("detail.pot") != null) == on, on ? "사도 상세 「잠재력」 단추" : "성장 끔 — 잠재력 단추 없음");
            Expect(Hot("detail.promote") == null && Hot("detail.lv1") == null && Hot("lvup.go") == null, "레벨업 · 진급 단추 없음");
            var potLine = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "potline");
            Expect(potLine == on, on ? "오른쪽 판 이름 아래 잠재력 요약 줄(레벨 줄 자리)" : "성장 끔 — 잠재력 요약 줄 없음");
            if (on) Expect(GoneWords("stats") == null, "사도 상세 오른쪽 판 — 없앤 성장 글 없음 " + GoneWords("stats"));
            if (on)
            {
                // 잠재력 탭 — 견본 pot_view(칸을 연 바로 앞 상태) · potential(그 칸을 연 응답)
                var potView = J("pot_view"); var potResp = J("potential");
                Expect(potView != null && potResp != null, "서버 응답 견본(-prog-samples pot_view · potential)");
                if (potView != null)
                {
                    ProgStore.DemoSet(potView, null);
                    ProgStore.DemoPost = (path, body) => new Online.Resp { Code = 200, Json = potResp, Text = "" };
                    yield return Press("detail.pot", 1.8f);
                    yield return Shot("prog_pot_grid");
                    Expect(Hot("pot.01") != null && Hot("pot.07") != null && Hot("pot.up") != null, "「잠재력」 단추 → 잠재력 노드 판(10칸) · 올리기 단추");
                    Expect(GoneWords("potpanel") == null && GoneWords("potboard") == null, "잠재력 화면 — 없앤 성장 글(진급 · 골드 …) 없음 " + (GoneWords("potpanel") ?? GoneWords("potboard")));
                    string node = (string)potResp?["node"] ?? "03";
                    yield return Press("pot." + node, 1.2f);
                    yield return Shot("prog_pot_node");
                    Expect(Hot("pot.up") != null && Hot("pot.up").Interactable, $"잠재력 {node} — 올릴 수 있음(재료 · 앞 칸)");
                    yield return Press("pot.up", 1.8f);
                    yield return Shot("prog_pot_open");
                    Expect(ProgStore.PotOf("아야").TryGetValue(node, out var after) && after == (int?)potResp?["lv"]?[1], $"잠재력 {node} 단계가 응답대로 오름");
                    yield return Press("pot.07", 1.0f);
                    yield return Shot("prog_pot_locked");
                    Expect(Hot("pot.up") != null && !Hot("pot.up").Interactable && Hot("pot.up").Label.text.Contains("난이도 단계 7"), $"잠긴 칸(07 — 난이도 단계 7 완주 · 앞 칸 06) — 단추 꺼짐 · 「{Hot("pot.up")?.Label?.text}」");
                    Expect(Hot("pot.05-2") != null && Hot("pot.05-1") != null, "카제나식 10칸 — 갈래 05-1 · 05-2");

                    // 다 연 판(카제나식 노드 판) — 사도 셋(딜러 · 탱커 · 서포터): 견본 pot_view 를 데모에서만 「다 연 사도」 로 고쳐 넣는다(서버 값 아님 — 화면 캡처용)
                    var full = (JObject)potView.DeepClone();
                    var trio = new[] { "시온더다크불릿", "벨라", "요미" };
                    foreach (var hk in trio)
                    {
                        if (!(full["heroes"]?[hk] is JObject hv)) continue;
                        var po = new JObject(); foreach (var n in ProgStore.Table.Potential) po[n.Id] = n.Max;
                        hv["pot"] = po;
                        if (hv["potential"] is JArray pa) foreach (var x in pa) { x["lv"] = x["max"]; x["locked"] = false; x["ok"] = false; x["next"] = null; x["why"] = "최대 단계다"; }
                    }
                    ProgStore.DemoSet(full, null);
                    for (int k = 0; k < trio.Length; k++)
                    {
                        var hi = Roster.OfCore(trio[k]);
                        if (hi == null) continue;
                        CloseModals();
                        f.HeroDetail(hi.key, null, null);
                        yield return Wait(1.0f);
                        if (k == 0) { yield return Shot("prog_dex_detail_full"); Expect(f.Stage.transform.GetComponentsInChildren<TMP_Text>(true).Any(t => t.name == "potsub" && t.text.Contains("모두 열었다")), "다 연 사도 — 요약 줄 「모두 열었다」"); }
                        yield return Press("detail.tab:잠재력", 1.6f);
                        yield return Shot($"prog_pot_board_{k + 1}");
                        Expect(Hot("pot.07") != null, $"{trio[k]} — 노드 판");
                        yield return Press("pot.07", 1.0f);
                        yield return Shot($"prog_pot_hero07_{k + 1}");
                        yield return Press("pot.03-1", 1.0f);
                        yield return Shot($"prog_pot_hero031_{k + 1}");
                        var m = ProgStore.ModsOf(trio[k]);
                        Expect(m?.Cards != null && m.Cards.Count >= 3, $"{trio[k]} — 사도 칸 카드 덤 {m?.Cards?.Count ?? 0}(03-1 · 05-1 · 07)");
                        Expect(ProgStore.Table.HeroNode(trio[k], "07")?.Desc?.Contains("더") == false, $"{trio[k]} — 07 효과 글에 문턱 덤 없음(10-22)");
                    }
                    CloseModals();
                    // 사도 칸 03-1 이 붙은 기본 카드(시온 「저격」 — 마탄 1장) · 07 이 붙은 생성 카드(「마탄」)의 실제 수치
                    foreach (var cid in new[] { "시온더다크불릿_s1", "시온더다크불릿_t1" })
                    {
                        if (f.P.Data.Card(cid) == null) continue;
                        f.CardZoomPlain(cid);
                        yield return Wait(1.2f);
                        yield return Shot("prog_card_real_hero_" + cid.Split('_').Last());
                        Expect(f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "term 실제 수치"), $"{cid} — 실제 수치(사도 칸 덤)");
                        CloseModals();
                    }
                    ProgStore.DemoSet(potResp?["prog"] as JObject ?? potView, null);
                }
            }
            CloseModals();
            // 카드 상세 — 실제 수치
            var uid = f.P.Data.UniquesOf(aya.CoreId ?? "아야").FirstOrDefault();
            if (uid != null)
            {
                f.CardZoomPlain(uid);
                yield return Wait(1.2f);
                yield return Shot(on ? "prog_card_real" : "progoff_card");
                var real = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "term 실제 수치");
                Expect(real == on, on ? "카드 상세 실제 수치 상자" : "성장 끔 — 실제 수치 상자 없음");
            }
            CloseModals();
            // 기본 카드 — 잠재력 02(기본 카드 +%)가 실제 수치에 들어가는지
            var sid = f.P.Data.Hero(aya.CoreId ?? "아야")?.Starter?.FirstOrDefault();
            if (on && sid != null)
            {
                f.CardZoomPlain(sid);
                yield return Wait(1.2f);
                yield return Shot("prog_card_real_pot");
                var mods = ProgStore.ModsOf(aya.CoreId ?? "아야");
                Expect(mods != null && mods.Basic > 0, $"잠재력 기본 카드 덤 +{(mods?.Basic ?? 0) * 100:0.#}% — 실제 수치에 곱함");
                CloseModals();
            }
            ProgStore.DemoPost = null;
            Debug.Log($"[Demo] 성장 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }

        // 성장 시험 빌드 점검(-demo-prog-live -server <로컬>, 2026-10-21 밤 잠재력만) — C:/projects/bolzena-play-prog/launch.mjs 가 띄운 로컬 서버(PROG=1 · TEST_LOGIN · 시험 계정 · 재료)에 붙어
        //   DemoPost 없이 진짜 서버 호출로: 열린 가장 높은 단계로 모험을 열어 완주 처리 → 모험 끝 재료 받기(지갑이 준 만큼 오름) · 다음 단계 열림
        //   → 사도 상세 「잠재력」 → 지금 열 수 있는 칸 하나 열기. 사도는 -live-hero(기본 아야).
        IEnumerator ProgLive_()
        {
            int f0 = fails;
            float t0 = Time.time;
            while (!(Online.Ready && ProgStore.Have) && Time.time - t0 < 60) yield return Wait(0.5f);
            Expect(ProgStore.On, "성장 켬(시험 빌드 콘텐츠에 성장 표)");
            Expect(Online.Ready && ProgStore.Have, $"로컬 서버 로그인 · 성장 받음({Online.Base})");
            var hk = Arg("-live-hero") ?? "아야";
            var hi = Roster.OfCore(hk);
            if (ProgStore.Have && hi != null)
            {
                IEnumerator Until(System.Func<bool> ok, float max = 15f) { float t = Time.time; while (!ok() && Time.time - t < max) yield return Wait(0.3f); yield return Wait(1.2f); }
                // ① 모험 끝 재료 — 열린 가장 높은 단계로 판을 열고(사도 셋: hk · 이드 · 실비아) 완주로 끝낸다(걸린 분 = 12분으로 — 서버 모양 검사 3분)
                f.Lobby();
                yield return Screen_("lobby", 1.2f);
                yield return Shot("live_lobby");
                int best0 = ProgStore.BestTier, tier = ProgStore.OpenTier;
                var party = new System.Collections.Generic.List<string> { hk, "이드", "실비아" }.Distinct().Where(k => f.P.Data.Hero(k) != null).Take(3).ToList();
                int w3 = ProgStore.Wallet("mm3");
                yield return Press("start", 0.3f);
                yield return Screen_("vilpick", 1.6f);
                yield return Shot("live_tier");
                Expect(Hot("tier." + tier) != null && Hot("tier." + tier).Interactable, $"단계 {tier} 열림(최고 완주 {best0})");
                if (tier < Tiers.MAX) Expect(Hot("tier." + (tier + 1)) != null && !Hot("tier." + (tier + 1)).Interactable, $"단계 {tier + 1} 잠김");
                yield return Press("tier." + tier, 0.4f);
                Expect(f.RunTier == tier, $"마을 고르기 단계 줄 — 단계 {tier} 고름");
                f.FoeNature = null;
                f.StartRun(party, f.P.RollVillage());
                yield return Screen_("map", 1.4f);
                Expect(f.P.S.Tier == tier, $"판 단계 {f.P.S.Tier}");
                f.P.S.Rec.Began = System.DateTime.UtcNow.AddMinutes(-12).ToString("yyyy-MM-dd'T'HH:mm'Z'", System.Globalization.CultureInfo.InvariantCulture);
                f.End("clear");
                yield return Screen_("end_clear", 0.6f);
                yield return Until(() => !ProgStore.EarnPending && (ProgStore.LastEarn != null || ProgStore.EarnError != null));
                yield return Shot("live_end");
                var e = ProgStore.LastEarn;
                Expect(e != null && e["dup"]?.Type == JTokenType.Boolean && !(bool)e["dup"], "모험 끝 — 서버가 재료를 줬다 " + (ProgStore.EarnError ?? ""));
                int give3 = (int?)e?["reward"]?["give"]?["items"]?["mm3"] ?? 0;
                Expect(give3 == ProgStore.WinMat(tier, 3) || ((JArray)e?["reward"]?["capped"])?.Count > 0, $"상급 마시멜로 +{give3}(단계 {tier} 완주 = {ProgStore.WinMat(tier, 3)} — 화면 미리 보기와 서버가 같은 식)");
                Expect(ProgStore.Wallet("mm3") == w3 + give3, $"지갑 상급 마시멜로 {w3} → {ProgStore.Wallet("mm3")}");
                if (tier < Tiers.MAX) Expect((int?)e?["opened"] == tier + 1 && ProgStore.OpenTier == tier + 1, $"단계 {tier} 완주 → 단계 {tier + 1} 열림");
                var endBox = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "progreward");
                Expect(endBox != null && GoneWords("progreward") == null, "판 끝 재료 판 — 없앤 성장 글 없음 " + GoneWords("progreward"));
                // 단계 화면 다시 — 새로 열린 단계
                f.Lobby();
                yield return Screen_("lobby", 1.0f);
                yield return Press("start", 0.3f);
                yield return Screen_("vilpick", 1.6f);
                yield return Shot("live_tier_after");
                if (tier < Tiers.MAX) Expect(Hot("tier." + (tier + 1)) != null && Hot("tier." + (tier + 1)).Interactable, $"마을 고르기 단계 줄 — 단계 {tier + 1} 이 열렸다");
                CloseModals();

                // ② 잠재력 칸 열기 — 아직 다 안 연 칸 가운데 지금 열 수 있는 첫 칸
                f.Dex(null);
                yield return Screen_("dex", 1.2f);
                f.HeroDetail(hi.key, null, null);
                yield return Wait(1.8f);
                yield return Shot("live_stats");
                Expect(Hot("detail.pot") != null && Hot("detail.promote") == null, "사도 상세 — 「잠재력」 단추(레벨업 · 진급 없음)");
                yield return Press("detail.pot", 1.8f);
                var node = ProgStore.Table.Potential.Select(n => n.Id).FirstOrDefault(id => ProgStore.PotNode(hk, id)?["ok"]?.Type == JTokenType.Boolean && (bool)ProgStore.PotNode(hk, id)["ok"]) ?? "01";
                int n0 = ProgStore.PotOf(hk).TryGetValue(node, out var a0) ? a0 : 0;
                yield return Press("pot." + node, 1.2f);
                yield return Shot("live_pot_pick");
                Expect(Hot("pot.up") != null && Hot("pot.up").Interactable, $"잠재력 {node} — 단추 켜짐");
                yield return Press("pot.up", 0.2f);
                yield return Until(() => (ProgStore.PotOf(hk).TryGetValue(node, out var a1) ? a1 : 0) > n0);
                Expect((ProgStore.PotOf(hk).TryGetValue(node, out var a2) ? a2 : 0) == n0 + 1, $"서버 잠재력 {node} — 단계 {n0} → {n0 + 1}");
                yield return Shot("live_pot_done");
            }
            else Expect(hi != null, $"사도 {hk}");
            Debug.Log($"[Demo] 성장 시험 빌드 점검 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }
    }
}
