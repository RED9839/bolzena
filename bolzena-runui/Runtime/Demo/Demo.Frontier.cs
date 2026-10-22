using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 프론티어 데모(-demo-frontier, 로컬 서버 -server http://127.0.0.1:8799 — bolzena-unity/Web/test/frontier-ui.mjs 가 띄우고 기기 코드 승인 · bz verify 를 맡는다).
    //   로비 → 계정 → 기기 코드(바깥 시험이 승인) → 닉네임(금지어 400 → 정함) → 엔진 봇이 완주한 판으로 끝 화면 → 사도 셋 새로 저장
    //   → 같은 사도 한 번 더(이름을 붙여 둘째 칸) → 덮어쓰기 확인
    //   → 프론티어 입구(보스 스파인 · 주요 패턴 목록 · 랭킹) → 저장함(사도 거르기 · 고른 칸의 카드 덱이 바로) → 진입 → 편성(사도 고르기 — 같은 사도 막힘 · 덱 칸 → 저장 데이터 창 · 칸 둘 · 이름 바꾸기 · 장착)
    //   → 도전(서버 씨앗) → 전투(봇 손가락 — 턴 제한까지) → 결과 · 제출(검증 대기) → 검증(바깥 bz verify — 바깥 시험이 다른 교주 「밥」 의 검증된 기록도 심는다)
    //   → 랭킹(내 줄 · 밥의 줄을 눌러 남의 편성 · 읽기 전용 저장 데이터) → 입구 랭킹 칸(내 줄) → 내 기록(목록 → 그때의 편성 → 읽기 전용 저장 데이터)
    //   → 입구 랭킹 줄 → 남의 편성 → 편성(칸 최고 = 서버 검증 점수) → 저장함(확인) → 로비 「사도 저장함」(칸 상세 카드 그리드)
    //   → 사도마다 10칸(판 끝 저장 창 — 칸 셋일 때 · 열 칸 찬 때(새로 저장 없음 · 서버 409) · 칸 지우기 → 다시 「새로 저장」) → 계정(탈퇴 확인 창 · 로그아웃). 서버가 없으면(Off) 입구(스파인 · 「준비 중」) · 저장함만 찍는다.
    public partial class Demo
    {
        bool ModalUp(string name) => f.Stage.ModalLayer.Find("modal " + name) != null;

        IEnumerator Frontier_()
        {
            int f0 = fails;
            yield return Wait(0.5f);
            yield return Until(() => Online.Now != Online.State.Unknown && !Online.Busy, 30);
            Debug.Log($"[Demo] 온라인 {Online.Now} · {Online.OffWhy} · 서버 {Online.Base}");
            if (!Online.Up) { yield return FrontierOff_(); yield break; }
            // 기기 사본을 비우고 시작(새 로컬 D1 — 지난 실행의 사본이 섞이지 않게)
            SlotStore.ClearCopies(); SlotStore.DropWaiting();
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("f01_lobby");

            // ── 로그인(기기 코드) ──
            if (Online.HasToken) { Online.Logout(); yield return Until(() => Online.Now == Online.State.Out, 10); }
            yield return Press("lobby.account", 0.9f);
            yield return Shot("f02_account");
            yield return Press("acct.device", 0.6f);
            yield return Until(() => f.DeviceCodeShown != null, 20);
            yield return Wait(1.0f);
            yield return Shot("f03_device_code");
            Debug.Log($"[Demo] 기기 코드 승인을 기다림 — {f.DeviceCodeShown}");
            yield return Until(() => Online.Now == Online.State.NeedName || Online.Now == Online.State.In, 120);
            Expect(Online.Now == Online.State.NeedName, $"기기 코드 승인 → 가입 전(needsName) — {Online.Now}");
            yield return Until(() => ModalUp("nickname") && f.NameField != null, 15);
            yield return Wait(0.8f);
            yield return Shot("f04_nickname");
            f.NameField.text = "운영자";
            yield return Press("name.ok", 0.2f);
            yield return Until(() => f.NameErrorShown != null, 15);
            yield return Wait(0.5f);
            yield return Shot("f05_nickname_error");
            Expect(f.NameErrorShown != null && f.NameErrorShown.Contains("사칭"), $"금지어 닉네임 → 400 까닭 「{f.NameErrorShown}」");
            f.NameField.text = "앨리스";
            yield return Press("name.ok", 0.2f);
            yield return Until(() => Online.Now == Online.State.In, 15);
            Expect(Online.Now == Online.State.In && Online.Nick == "앨리스", $"닉네임 정함 → 가입 끝 — {Online.Now} {Online.Nick}");
            yield return Wait(1.2f);
            yield return Shot("f06_lobby_in");

            // ── 시험용 완주 판(엔진 숙련 봇) → 끝 화면 ──
            Core.Run end = null;
            bool botDone = false;
            var data = f.P.Data;
            var players = Roster.Players.Select(h => h.CoreId).Where(id => id != null && data.Hero(id) != null).Distinct().ToList();
            var th = new System.Threading.Thread(() =>
            {
                try
                {
                    for (int i = 0; i < 60 && end == null; i++)
                    {
                        var party = new List<string> { players[(i * 3) % players.Count], players[(i * 3 + 1) % players.Count], players[(i * 3 + 2) % players.Count] };
                        if (party.Distinct().Count() < 3) continue;
                        Core.Run last = null;
                        new RunBot(data).RunFull(party, 7100 + i, new SimOpts { Skilled = true, OnEnd = r => last = r });
                        if (last?.S.Done == "clear") end = last;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("[Demo] 봇 판 실패 — " + e); }
                botDone = true;
            });
            th.IsBackground = true;
            th.Start();
            yield return Until(() => botDone, 600);
            Expect(end != null, "엔진 봇이 완주한 판");
            if (end == null) { Bail(7, "완주한 봇 판이 없다"); yield break; }
            Debug.Log($"[Demo] 봇 완주 판 — 씨앗 {end.S.Seed} · 파티 [{string.Join(", ", end.S.Party)}] · 덱 {end.S.Deck.Count}");
            f.P.Adopt(end);
            f.End("clear");
            yield return Screen_("end_clear", 2.6f);
            yield return Shot("f07_end_clear");
            var heroes = end.S.Party.ToList();
            Expect(SlotStore.Wait != null && SlotStore.Wait.Party.Count == heroes.Count, "판 끝 사도 셋을 저장 기다림에 둠");

            // ── 사도 저장(새로 저장) · 같은 사도 둘째 칸(이름) · 덮어쓰기 ──
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                yield return Press("end.save:" + h, 0.2f);
                yield return Until(() => ModalUp("savepick"), 15);
                yield return Wait(0.8f);
                if (i == 0) yield return Shot("f08_savepick");
                yield return Press("savepick.new", 0.2f);
                yield return Until(() => !ModalUp("savepick") && SlotStore.Of(h).Any(), 15);
                Expect(SlotStore.Of(h).FirstOrDefault()?.Status == "pending" && SlotStore.Of(h).First().Id > 0, $"{h} 새로 저장 → 서버 id {SlotStore.Of(h).FirstOrDefault()?.Id} · 검증 대기");
                yield return Wait(0.6f);
            }
            yield return Press("end.save:" + heroes[0], 0.2f);
            yield return Until(() => ModalUp("savepick"), 15);
            yield return Wait(0.6f);
            Expect(f.SavePickRows == 1, $"저장 창 — 덮어쓸 칸 목록 {f.SavePickRows}");
            f.SaveNameField.text = "보스용 덱";
            yield return Wait(0.3f);
            yield return Shot("f09_savepick_named");
            yield return Press("savepick.new", 0.2f);
            yield return Until(() => !ModalUp("savepick") && SlotStore.Of(heroes[0]).Count() == 2, 15);
            Expect(SlotStore.Of(heroes[0]).Count() == 2 && SlotStore.Of(heroes[0]).Any(x => x.Name == "보스용 덱" && x.N == 1), $"같은 사도 둘째 칸(이름 「보스용 덱」) — {string.Join(", ", SlotStore.Of(heroes[0]).Select(x => x.Id + ":" + x.Label))}");
            yield return Wait(0.6f);
            yield return Press("end.save:" + heroes[0], 0.2f);
            yield return Until(() => ModalUp("savepick"), 15);
            yield return Wait(0.8f);
            yield return Shot("f10_savepick_filled");
            Expect(f.SavePickRows == 2, $"저장 창 — 덮어쓸 칸 둘 {f.SavePickRows}");
            yield return Press("savepick.over0", 0.6f);
            Expect(ModalUp("confirm"), "기존 칸을 고르면 덮어쓰기 확인");
            yield return Shot("f10b_overwrite_confirm");
            yield return Press("confirm.yes", 0.2f);
            yield return Until(() => !ModalUp("savepick"), 15);
            Expect(SlotStore.Of(heroes[0]).Count() == 2, $"덮어써도 칸 수 그대로 — {SlotStore.Of(heroes[0]).Count()}");
            yield return Wait(1.0f);
            yield return Shot("f11_end_saved");

            // ── 프론티어 입구(보스 스파인 · 랭킹) · 저장함 ──
            yield return Press("end.lobby");
            yield return Screen_("lobby", 1.2f);
            yield return Press("frontier");
            yield return Screen_("frontier", 2.0f);
            yield return Shot("f12_frontier_entry");
            Expect(f.FrontierBossSpine, "입구 — 보스 인게임 스파인이 섰다");
            Expect(f.FrontierMarks >= 7, $"입구 — 주요 패턴 목록 {f.FrontierMarks}줄(판 안 스크롤 {f.FrontierMarksScroll})");
            Expect(f.FrontierGap >= 0, $"입구 — 보스 판 · 내 기록 판 안 겹침(틈 {f.FrontierGap:0})");
            Expect(Hot("frontier.enter") != null && Hot("frontier.enter").Interactable, "입구 — 「진입」 이 열려 있다");
            yield return Press("frontier.box");
            yield return Screen_("slotbox", 1.2f);
            yield return Shot("f13_slotbox");
            Expect(f.SlotBoxCards == 4, $"저장함 — 칸 넷(사도 셋 · 한 사도는 둘) {f.SlotBoxCards}");
            yield return Press($"box.f:{heroes[0]}", 0.2f);
            yield return Screen_("slotbox", 0.8f);
            Expect(f.SlotBoxCards == 2, $"저장함 — 사도 거르기 → {f.SlotBoxCards}칸");
            yield return Shot("f13b_slotbox_hero");
            yield return Press("box.f:all", 0.2f);
            yield return Screen_("slotbox", 0.8f);
            yield return Press("box.card1", 0.2f);
            yield return Screen_("slotbox", 1.2f);
            Expect(f.SlotBoxDetailCards > 0 && Hot("box.name") != null && Hot("box.del") != null, $"저장함 — 칸을 고르면 카드 덱이 바로(카드 {f.SlotBoxDetailCards}장) · 이름 · 지우기 단추");
            yield return Shot("f14_slotbox_detail");
            yield return Press("back");
            yield return Screen_("frontier", 1.0f);

            // ── 진입 → 편성(사도 카드 셋 · 덱 칸) ──
            for (int i = 0; i < 3; i++) f.SetFrontierPick(i, null, 0);   // 지난 실행의 편성 기억을 비운다
            if (SlotStore.Order != SlotStore.Sort.Recent) SlotStore.Order = SlotStore.Sort.Recent;
            yield return Press("frontier.enter");
            yield return Screen_("frontier_team", 1.4f);
            yield return Shot("f15_team_empty");
            yield return Press("frontier.seat0", 1.0f);
            yield return Shot("f16_hero_pick");
            yield return Press($"fhero:{heroes[0]}", 0.2f);
            yield return Screen_("frontier_team", 0.6f);
            yield return Press("frontier.seat1", 1.0f);
            Expect(Hot($"fhero:{heroes[0]}") != null && !Hot($"fhero:{heroes[0]}").Interactable, "다른 자리에 든 사도는 못 고른다(같은 사도 둘 금지)");
            yield return Shot("f17_hero_pick_same");
            yield return Press($"fhero:{heroes[1]}", 0.2f);
            yield return Screen_("frontier_team", 0.6f);
            yield return Press("frontier.seat2", 1.0f);
            yield return Press($"fhero:{heroes[2]}", 0.2f);
            yield return Screen_("frontier_team", 1.4f);
            yield return Shot("f18_team_ready");
            // 덱 칸 → 저장 데이터 창 → 장착(이미 장착한 칸이면 「장착 중」 — 눌러 닫는다)
            yield return Press("frontier.deck0", 1.4f);
            Expect(ModalUp("savedata"), "덱 칸을 누르면 저장 데이터 창");
            Expect(f.SaveDataRows == 2, $"저장 데이터 창 — {heroes[0]} 칸 둘 {f.SaveDataRows}");
            yield return Shot("f19_savedata");
            Expect(f.SaveDataEquipped, "장착한 칸은 「장착 중」");
            // 이름 바꾸기(연필)
            yield return Press("sdata.name", 0.8f);
            Expect(ModalUp("slotname") && f.SlotNameField != null, "연필 → 칸 이름 창");
            f.SlotNameField.text = "레이드 <b>!";
            yield return Press("sname.ok", 0.2f);
            yield return Until(() => f.SlotNameError != null, 10);
            yield return Wait(0.3f);
            Expect(f.SlotNameError != null && ModalUp("slotname"), $"쓸 수 없는 칸 이름 → 400 까닭 「{f.SlotNameError}」");
            yield return Shot("f19b_slotname_error");
            f.SlotNameField.text = "레이드 A";
            yield return Press("sname.ok", 0.2f);
            yield return Until(() => !ModalUp("slotname"), 10);
            yield return Wait(1.0f);
            var picked0 = f.FrontierPickId(0);
            Expect(picked0 != null && SlotStore.Get(picked0.Value)?.Name == "레이드 A", $"칸 이름 바꿈 — {SlotStore.Get(picked0 ?? 0)?.Label}");
            yield return Shot("f19c_savedata_renamed");
            yield return Press("sdata.equip", 0.6f);
            Expect(!ModalUp("savedata"), "「장착 중」 을 누르면 창을 닫는다");
            yield return Screen_("frontier_team", 0.8f);

            // ── 도전 → 전투(봇 손가락) → 결과 · 제출 ──
            yield return Press("frontier.go", 0.5f);
            yield return Until(() => f.Stage.Current == "frontier_result", 900);
            yield return Screen_("frontier_result", 0.5f);
            yield return Until(() => f.LastSubmit != null && f.LastSubmit != "sending", 30);
            yield return Wait(1.2f);
            yield return Shot("f20_result");
            Expect(f.LastFrontier?.Tape != null && f.LastFrontier.Over != null, $"프론티어 끝 — {f.LastFrontier?.Over} · {f.LastFrontier?.Played}턴 · 점수 {f.LastFrontier?.Score}");
            Expect(f.LastSubmit == "pending", $"제출 → 검증 대기 — {f.LastSubmit}");

            // ── 검증 전 — 낸 점수가 곧바로 랭킹에 「검증 전」 으로(2026-10-19 「중간 방법 1」). 바깥 시험은 아래 「검증을 기다림」 을 본 뒤에 검증기를 돌린다 ──
            yield return Press("fres.rank");
            yield return Screen_("ranking", 1.6f);
            yield return Shot("f20b_ranking_unverified");
            Expect(f.RankingMine && f.RankingPending >= 1, $"검증 전 랭킹 — 내 줄 {f.RankingMine} · 「검증 전」 줄 {f.RankingPending} / {f.RankingRows}");
            yield return Press("back");
            yield return Screen_("frontier", 2.0f);
            yield return Until(() => f.EntryRankRows > 0, 15);
            yield return Wait(0.8f);
            yield return Shot("f20c_entry_unverified");
            Expect(f.EntryRankMine && f.EntryRankPending >= 1, $"검증 전 입구 랭킹 칸 — 내 줄 {f.EntryRankMine} · 「검증 전」 줄 {f.EntryRankPending}");
            yield return Press("frontier.history", 0.3f);
            yield return Screen_("frontier_history", 1.4f);
            yield return Shot("f20d_history_unverified");
            yield return Press("back");
            yield return Screen_("frontier", 1.2f);

            // ── 검증(바깥 bz verify) 기다림 ──
            Debug.Log("[Demo] 검증을 기다림(bz verify)");
            JObject me = null;
            for (int k = 0; k < 90; k++)
            {
                Online.Resp r = null;
                yield return Online.CallCo("GET", "/api/frontier/me", null, x => r = x);
                me = r.Json;
                var st = (me?["scores"] as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["status"] != "skipped")?["status"]?.ToString();
                if (st != null && st != "pending") break;
                yield return Wait(3f);
            }
            var sc0 = (me?["scores"] as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["status"] != "skipped");
            Expect(sc0?["status"]?.ToString() == "verified", $"유니티 테이프가 .NET 검증기에서 — {sc0?["status"]} {sc0?["why"]} (점수 {sc0?["score"]} · 주장 {sc0?["claimed"]})");
            int myScore = (int?)sc0?["score"] ?? -1;
            yield return Press("frontier.rank");
            yield return Screen_("ranking", 1.6f);
            yield return Shot("f21_ranking");
            Expect(f.RankingRows > 0 && f.RankingMine, $"랭킹에 내 줄 — 줄 {f.RankingRows} · 내 줄 {f.RankingMine}");
            Expect(f.RankingPending == 0, $"검증 뒤 — 「검증 전」 표시가 사라짐(남은 줄 {f.RankingPending})");
            // ── 남의 편성(랭킹 줄) — 바깥 시험이 심은 「밥」 의 검증된 기록 ──
            int other = f.RankingNames.FindIndex(n => n != Online.Nick) + 1;
            Expect(other > 0, $"랭킹에 다른 교주 줄 — [{string.Join(", ", f.RankingNames)}]");
            if (other > 0)
            {
                yield return Press("rank.row" + other, 0.3f);
                yield return Screen_("frontier_attempt", 1.6f);
                yield return Shot("f21a_other_team");
                var aj = f.AttemptShown;
                Expect(aj != null && aj["mine"]?.ToObject<bool>() == false && (string)aj["name"] == f.RankingNames[other - 1] && (aj["slots"] as JArray)?.Count == 3 && aj["seed"] == null && aj["tape"] == null,
                    $"랭킹 줄 → 남의 편성(사도 셋 · 씨앗 · 테이프 없음) — {aj?["name"]} · {aj?["score"]} · [{string.Join(", ", (aj?["heroes"] as JArray)?.Select(x => (string)x) ?? new string[0])}]");
                yield return Press("fatt.hero0", 1.4f);
                Expect(ModalUp("slotread") && f.SlotReadCards > 0, $"남의 사도 → 읽기 전용 저장 데이터 — {f.SlotReadHero} · 카드 {f.SlotReadCards}장");
                Expect(Hot("sdata.equip") == null && Hot("sdata.del") == null, "읽기 전용 — 장착 · 지우기 단추 없음");
                yield return Shot("f21b_other_savedata");
                yield return Press("sread.hero2", 1.2f);
                yield return Shot("f21c_other_savedata_hero3");
                CloseModals();
                yield return Wait(0.4f);
                yield return Press("back");
                yield return Screen_("ranking", 1.2f);
            }
            yield return Press("back");
            yield return Screen_("frontier", 2.0f);
            yield return Until(() => f.EntryRankRows > 0, 15);
            yield return Wait(0.8f);
            yield return Shot("f22_frontier_entry_after");
            Expect(f.EntryRankRows > 0 && f.EntryRankMine, $"입구 랭킹 칸에 내 줄 — 줄 {f.EntryRankRows} · 내 줄 {f.EntryRankMine}");
            // ── 내 기록 → 그때의 편성 → 읽기 전용 저장 데이터 ──
            Expect(Hot("frontier.history") != null && Hot("frontier.history").Interactable, "입구 — 「기록 보기」 가 열려 있다");
            yield return Press("frontier.history", 0.3f);
            yield return Screen_("frontier_history", 1.4f);
            yield return Shot("f22a_history");
            Expect(f.HistoryRows >= 1, $"내 기록 줄 — {f.HistoryRows}");
            // 바깥 시험(frontier-ui.mjs)이 검증 전에 이 계정으로 낮은 점수 하나를 더 낸다 — 내 최고보다 높지 않아 「검증 안 함」(skipped)
            Expect(f.HistoryStatus.Contains("skipped"), $"내 기록 — 「검증 안 함」 줄 [{string.Join(", ", f.HistoryStatus)}]");
            int vrow = Mathf.Max(0, f.HistoryStatus.IndexOf("verified"));
            yield return Press("hist.row" + vrow, 0.3f);
            yield return Screen_("frontier_attempt", 1.6f);
            yield return Shot("f22b_my_attempt");
            var mj = f.AttemptShown;
            Expect(mj != null && mj["mine"]?.ToObject<bool>() == true && (string)mj["status"] == "verified" && (int?)mj["score"] == myScore && (mj["slots"] as JArray)?.Count == heroes.Count,
                $"내 기록 → 그때의 편성 — {mj?["status"]} · {mj?["score"]} · 사도 {(mj?["slots"] as JArray)?.Count}");
            yield return Press("fatt.hero1", 1.4f);
            Expect(ModalUp("slotread") && f.SlotReadHero == heroes[1] && f.SlotReadCards == (SlotStore.Of(heroes[1]).FirstOrDefault()?.Slot?.Cards.Count ?? -1), $"그때의 사도 → 읽기 전용 저장 데이터 — {f.SlotReadHero} · 카드 {f.SlotReadCards}장");
            yield return Shot("f22c_my_savedata");
            CloseModals();
            yield return Wait(0.4f);
            yield return Press("back");
            yield return Screen_("frontier_history", 1.0f);
            yield return Press("back");
            yield return Screen_("frontier", 1.6f);
            // 입구 랭킹 칸의 남의 줄 → 그 편성
            if (other > 0)
            {
                yield return Until(() => f.EntryRankRows > 0, 15);
                yield return Press("frank.row" + other, 0.3f);
                yield return Screen_("frontier_attempt", 1.4f);
                Expect(f.AttemptShown?["mine"]?.ToObject<bool>() == false, "입구 랭킹 줄 → 남의 편성");
                yield return Shot("f22d_entry_rank_other");
                yield return Press("back");
                yield return Screen_("frontier", 1.6f);
            }
            yield return Press("frontier.enter");
            yield return Screen_("frontier_team", 1.4f);
            yield return Shot("f23_team_after");
            var picks = Enumerable.Range(0, 3).Select(i => f.FrontierPickId(i)).ToList();
            Expect(SlotStore.FromServer && picks.All(id => id != null && SlotStore.Get(id.Value)?.Best == myScore), $"편성 덱 칸 「이 칸 최고」 = 서버 검증 점수 {myScore} — {string.Join(", ", picks.Select(id => id + ":" + (id != null ? SlotStore.Get(id.Value)?.Best : null)))}");
            yield return Press("frontier.deck0", 1.4f);
            yield return Shot("f24_savedata_after");
            yield return Press("sdata.sort", 1.0f);
            Expect(SlotStore.Order == SlotStore.Sort.Score && ModalUp("savedata"), "저장 데이터 창 — 점수순으로");
            yield return Shot("f24b_savedata_score");
            SlotStore.Order = SlotStore.Sort.Recent;
            CloseModals();
            yield return Wait(0.4f);
            yield return Press("back");
            yield return Screen_("frontier", 1.0f);
            yield return Press("frontier.box");
            yield return Screen_("slotbox", 1.4f);
            yield return Shot("f25_slotbox_checked");
            Expect(SlotStore.Copies.Count == 4 && SlotStore.Copies.All(x => x.Status == "ok"), $"검증 뒤 칸 넷 모두 확인 — {string.Join(", ", SlotStore.Copies.Select(x => x.Label + ":" + x.Status))}");
            yield return Press("back");
            yield return Screen_("frontier", 0.8f);
            yield return Press("back");
            yield return Screen_("lobby", 1.2f);

            // ── 로비 「사도 저장함」(프론티어 밖) — 칸을 고르면 카드 덱 · 장비 · 성장이 바로 ──
            Expect(Hot("lobby.box") != null, "로비 메뉴에 「보관함」");
            yield return Shot("f29_lobby_menu");
            yield return Press("lobby.box");
            yield return Screen_("slotbox", 1.4f);
            yield return Press("box.f:" + heroes[0], 0.2f);
            yield return Screen_("slotbox", 1.2f);
            Expect(f.SlotBoxCards == 2 && f.SlotBoxDetailCards > 0, $"로비 저장함 — {heroes[0]} 칸 {f.SlotBoxCards} · 고른 칸 카드 {f.SlotBoxDetailCards}장");
            yield return Shot("f30_lobby_slotbox");
            yield return Press("box.card1", 0.2f);
            yield return Screen_("slotbox", 1.2f);
            yield return Shot("f31_slot_detail");
            yield return Press("back");
            yield return Screen_("lobby", 1.2f);

            // ── 사도마다 10칸 — 판 끝 저장 창(칸 셋 · 열 칸 찬 때 · 지우면 다시 새로 저장) ──
            string h0 = heroes[0], j0 = SlotStore.Wait?.Slots.TryGetValue(h0, out var jj) == true ? jj : null;
            Expect(j0 != null, "마지막 완주 판의 슬롯 글이 남아 있다(저장 창 시험)");
            if (j0 != null)
            {
                f.SavePick(h0, j0, null);
                yield return Until(() => ModalUp("savepick"), 15);
                yield return Wait(0.6f);
                yield return Press("savepick.new", 0.2f);
                yield return Until(() => !ModalUp("savepick") && SlotStore.Of(h0).Count() == 3, 15);
                f.SavePick(h0, j0, null);
                yield return Until(() => ModalUp("savepick"), 15);
                yield return Wait(0.8f);
                Expect(f.SavePickRows == 3 && Hot("savepick.new") != null, $"저장 창 — 칸 셋 + 끝에 「새로 저장 3/10」 하나(빈 칸 없음) — {f.SavePickRows}");
                yield return Shot("f32_savepick_3");
                CloseModals();
                // 나머지 일곱 칸은 서버에 바로(같은 슬롯 글)
                string q = "/api/slots?hero=" + System.Uri.EscapeDataString(h0);
                int made = 0;
                for (int k = 3; k < SlotStore.PerHero; k++) { Online.Resp r = null; yield return Online.CallCo("POST", q, j0, x => r = x); if (r != null && r.Code == 201) made++; }
                Online.Resp r11 = null;
                yield return Online.CallCo("POST", q, j0, x => r11 = x);
                Expect(made == SlotStore.PerHero - 3 && r11?.Code == 409 && r11.Error == "hero slots full", $"서버 — 10칸까지 저장 · 11번째 409 {r11?.Code} {r11?.Error}");
                f.SavePick(h0, j0, null);
                yield return Until(() => ModalUp("savepick"), 15);
                yield return Wait(0.8f);
                Expect(f.SavePickRows == SlotStore.PerHero && Hot("savepick.new") == null && Hot("savepick.del0") != null, $"저장 창 — 10칸 찼다: 「새로 저장」 없음 · 칸마다 지우기 — {f.SavePickRows}칸");
                yield return Shot("f33_savepick_full");
                yield return Press("savepick.del0", 0.6f);
                Expect(ModalUp("confirm"), "칸 지우기 확인");
                yield return Press("confirm.yes", 0.2f);
                yield return Until(() => SlotStore.Of(h0).Count() == SlotStore.PerHero - 1 && ModalUp("savepick"), 15);
                yield return Wait(0.8f);
                Expect(f.SavePickRows == SlotStore.PerHero - 1 && Hot("savepick.new") != null, $"한 칸 지우면 다시 「새로 저장 9/10」 — {f.SavePickRows}칸");
                yield return Shot("f34_savepick_after_delete");
                CloseModals();
                yield return Wait(0.4f);
                // 저장함에서도 같은 줄 — 사도 거름 · 끝에 「새로 저장 9/10」
                f.SlotBox(f.Lobby, h0);
                yield return Screen_("slotbox", 1.4f);
                Expect(f.SlotBoxCards == SlotStore.PerHero - 1 && Hot("box.new") != null, $"저장함 — {h0} {f.SlotBoxCards}칸 · 끝 줄");
                yield return Shot("f35_slotbox_9");
                yield return Press("back");
                yield return Screen_("lobby", 1.2f);
            }

            // ── 계정 — 탈퇴 확인(취소) · 로그아웃 ──
            yield return Press("lobby.account", 0.9f);
            yield return Shot("f26_account_in");
            yield return Press("acct.delete", 0.7f);
            yield return Shot("f27_delete_confirm");
            yield return Press("confirm.no", 0.5f);
            yield return Press("acct.logout", 0.3f);
            yield return Until(() => Online.Now == Online.State.Out, 15);
            yield return Wait(1.0f);
            yield return Shot("f28_lobby_out");
            Expect(Online.Now == Online.State.Out, "로그아웃");

            Debug.Log($"[Demo] 프론티어 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.5f);
            Application.Quit(fails - f0 == 0 ? 0 : 6);
        }

        // 서버 없음(Off) — 로비 메뉴 「준비 중」 · 프론티어(보스 소개 · 단추 막힘) · 저장함(기기 사본)
        IEnumerator FrontierOff_()
        {
            int f0 = fails;
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("off01_lobby");
            Expect(Hot("lobby.account") == null || !Hot("lobby.account").gameObject.activeSelf, "서버가 없으면 계정 알약을 숨김");
            yield return Press("frontier");
            yield return Screen_("frontier", 2.0f);
            yield return Shot("off02_frontier");
            Expect(Hot("frontier.enter") != null && !Hot("frontier.enter").Interactable, "서버가 없으면 「진입」 이 「준비 중」");
            Expect(f.FrontierBossSpine, "서버가 없어도 보스 스파인은 선다");
            Expect(Hot("frontier.history") != null && !Hot("frontier.history").Interactable, "서버가 없으면 「기록 보기」 가 막힘");
            yield return Press("frontier.box");
            yield return Screen_("slotbox", 1.2f);
            yield return Shot("off03_slotbox");
            Debug.Log($"[Demo] 프론티어(서버 없음) 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.5f);
            Application.Quit(fails - f0 == 0 ? 0 : 6);
        }
    }
}
