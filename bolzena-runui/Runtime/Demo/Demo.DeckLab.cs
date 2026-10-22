using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 덱 만들기 데모(2026-10-20 — bolzena-unity/Web/test/decklab-ui.mjs 가 띄운다).
    //   -demo-decklab -decklab(이 PC 시험 모드 · 서버 없이): 로비 진입점 → 빈 목록 → 새 덱 → 자리 셋 사도 고르기 · 카드 담기(고유 · 복제 · 교주) · 신탁 고르기 · 장비 3칸 · 성장
    //     · 규칙 밖(성장 상한 넘김) → 저장 → 시험 전투(씨앗 입력 · M.E.O.W) → 결과(점수 · 사도별 딜) → 목록.
    //   -demo-decklab-online -server <로컬>(실행 인자 -decklab 없이 — 공개판과 같은 조건): 일반 계정(lab_plain)은 진입점 없음 → 시험 계정(lab_tester — 바깥 시험이 운영 role 로 준다)은 보임
    //     → 덱 만들기 → 서버 시험 도전(시험 칸 저장 · 도전 · 제출) → 시험 기록(랭킹 밖) → 내 기록 「시험」.
    public partial class Demo
    {
        IEnumerator DeckLab_()
        {
            int f0 = fails;
            yield return Wait(0.5f);
            yield return Until(() => Online.Now != Online.State.Unknown && !Online.Busy, 30);
            Debug.Log($"[Demo] 덱 만들기 — 온라인 {Online.Now} · 시험 모드 {Flow.DeckLabLocal} · 시험 계정 {Online.Tester}");
            DeckLabStore.Clear();
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("d01_lobby_entry");
            Expect(Hot("lobby.decklab") != null, "이 PC 시험 모드(-decklab) — 로비에 「덱 만들기」");
            yield return Press("lobby.decklab", 0.2f);
            yield return Screen_("decklab", 1.2f);
            yield return Shot("d02_list_empty");
            Expect(f.LabDeckRows == 0, $"빈 덱 목록 — {f.LabDeckRows}");

            // ── 새 덱 — 자리 셋 ──
            yield return Press("decklab.new", 0.2f);
            yield return Screen_("decklab_edit", 1.0f);
            var data = f.P.Data;
            var heroes = Roster.Players.Select(h => h.CoreId).Where(k => k != null && data.Hero(k) != null)
                .Where(k => data.Cards.Values.Count(c => c.Hero == k && c.Unique && c.Oracles.Count > 0 && !c.Complete) >= 2).Distinct().Take(3).ToList();
            Expect(heroes.Count == 3, $"고유 카드(신탁 있는) 둘 이상인 사도 셋 — [{string.Join(", ", heroes)}]");
            for (int seat = 0; seat < heroes.Count; seat++)
            {
                yield return Press("lab.seat" + seat, 0.2f);
                yield return Screen_("decklab_edit", 0.6f);
                yield return Press("lab.hero", 1.0f);
                if (seat == 0) yield return Shot("d03_hero_pick");
                yield return Press("labhero:" + heroes[seat], 0.2f);
                yield return Screen_("decklab_edit", 0.8f);
                Expect(f.LabSlotShown?.Hero == heroes[seat] && f.LabDeckCards > 0, $"자리 {seat + 1} — {heroes[seat]} · 기본 카드 {f.LabDeckCards}장으로 시작");
                // 고유 카드 둘 + 첫째 한 장 더(복제본)
                yield return Press("lab.filter:unique", 0.2f);
                yield return Screen_("decklab_edit", 0.6f);
                int n0 = f.LabDeckCards;
                yield return Press("lab.cat0", 0.3f);
                yield return Press("lab.cat1", 0.3f);
                yield return Press("lab.cat0", 0.3f);
                Expect(f.LabDeckCards == n0 + 3, $"고유 카드 담기 · 한 장 더(복제본 「^」 · 유일은 「#n」) — {f.LabDeckCards}장 [{string.Join(", ", f.LabSlotShown.Cards.Skip(n0).Select(c => c.Id))}]");
                if (seat == 0)
                {
                    // 교주 카드
                    yield return Press("lab.filter:leader", 0.2f);
                    yield return Screen_("decklab_edit", 0.6f);
                    yield return Press("lab.cat0", 0.3f);
                    Expect(f.LabSlotShown.Cards.Any(c => GameData.OwnerOf(c.Id) == heroes[0]), "교주 카드는 「@사도」 로 담긴다");
                    yield return Press("lab.filter:all", 0.2f);
                    yield return Screen_("decklab_edit", 0.6f);
                    yield return Shot("d04_cards");
                    Expect(f.LabCatalogCards > 0, $"카드 목록(기본 · 고유 · 교주) {f.LabCatalogCards}장");
                }
                // 신탁 — 신탁 있는 고유 카드(원본)를 눌러 갈래 2
                int k = f.LabSlotShown.Cards.FindIndex(c => !GameData.IsCopy(c.Id) && data.Card(c.Id) is CardDef d && d.Unique && d.Oracles.Count >= 2 && !d.Complete);
                if (k < 0) k = f.LabSlotShown.Cards.FindIndex(c => data.Card(c.Id) is CardDef d && d.Oracles.Count >= 1 && !d.Complete);
                if (k >= 0)
                {
                    yield return Press("lab.deck" + k, 1.0f);
                    if (seat == 0) yield return Shot("d05_oracle_pick");
                    int o = System.Math.Min(2, data.Card(f.LabSlotShown.Cards[k].Id).Oracles.Count);
                    yield return Press("laboracle" + o, 0.6f);
                    Expect(f.LabSlotShown.Cards[k].O == o, $"카드 「{f.LabSlotShown.Cards[k].Id}」 신탁 갈래 {o}");
                }
                // 장비 3칸 — 무기는 가장 높은 등급(목록 끝), 나머지는 첫 장비
                foreach (var sn in R.SLOTS)
                {
                    yield return Press("lab.gear:" + sn, 1.0f);
                    if (seat == 0 && sn == "무기") yield return Shot("d06_equip_pick");
                    int cnt = data.Equips.Values.Count(e => e.Slot == sn);
                    yield return Press("labeq" + (sn == "무기" ? cnt - 1 : 0), 0.5f);
                }
                Expect(f.LabSlotShown.Gear.Count == 3, $"장비 3칸 — {string.Join(", ", f.LabSlotShown.Gear.Select(kv => kv.Key + ":" + kv.Value))}");
                yield return Press("lab.grow.atk+", 0.2f);
                yield return Press("lab.grow.atk+", 0.2f);
                yield return Press("lab.grow.def+", 0.3f);
                Expect(f.LabSlotShown.Growth.Atk == 10 && f.LabSlotShown.Growth.Def == 5, $"판 성장 — 공 {f.LabSlotShown.Growth.Atk} · 방 {f.LabSlotShown.Growth.Def}");
            }
            // 규칙 밖 — 셋째 자리 치명 성장을 상한(500) 넘게
            f.DemoLabGrow("crit", 600);
            yield return Wait(0.6f);
            Expect(f.LabRuleOut, $"성장 상한을 넘기면 「규칙 밖」 — {f.LabRuleWhy}");
            f.LabNameField.text = "보스 시험 덱";
            yield return Wait(0.3f);
            yield return Press("lab.seat0", 0.2f);
            yield return Screen_("decklab_edit", 1.0f);
            yield return Shot("d07_edit_seat1");
            yield return Press("lab.seat2", 0.2f);
            yield return Screen_("decklab_edit", 1.0f);
            yield return Shot("d07b_edit_seat3_ruleout");
            yield return Press("lab.save", 0.8f);
            var saved = DeckLabStore.All.FirstOrDefault();
            Expect(saved != null && saved.Name == "보스 시험 덱" && saved.Party().Count == 3, $"저장 — 「{saved?.Name}」 · 자리 {saved?.Party().Count}");

            // ── 시험 전투(씨앗 입력) ──
            yield return Press("lab.fight", 1.0f);
            Expect(ModalUp("labfight"), "시험 전투 창");
            f.LabSeedField.text = "20261020";
            yield return Wait(0.3f);
            yield return Shot("d08_fight_panel");
            yield return Press("labfight.go", 0.5f);
            yield return Until(() => f.Stage.Current == "decklab_result", 900);
            yield return Screen_("decklab_result", 1.6f);
            yield return Shot("d09_result_meter");
            var res = f.LabLast;
            long sum = res?.Meter?.Values.Sum(a => a[0]) ?? -1;
            Expect(res != null && res.Over != null && res.Tape != null, $"시험 전투 끝 — {res?.Over} · {res?.Played}턴 · 점수 {res?.Score}");
            Expect(res?.Meter != null && f.LabMeterRows >= 2, $"사도별 딜 줄 {f.LabMeterRows} — [{string.Join(", ", (res?.Meter ?? new Dictionary<string, long[]>()).Select(kv => kv.Key + " " + kv.Value[0]))}]");
            Expect(sum == res?.Score, $"사도별 딜 합 {sum} = 점수 {res?.Score}(측정이 점수와 같은 셈)");
            yield return Press("labres.back", 0.3f);
            yield return Screen_("decklab", 1.2f);
            yield return Shot("d10_list");
            Expect(f.LabDeckRows == 1, $"덱 목록 한 벌 — {f.LabDeckRows}");
            Debug.Log($"[Demo] 덱 만들기 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }

        IEnumerator DeckLabOnline_()
        {
            int f0 = fails;
            yield return Wait(0.5f);
            yield return Until(() => Online.Now != Online.State.Unknown && !Online.Busy, 30);
            Expect(Online.Up, $"로컬 서버에 닿음 — {Online.Now} {Online.OffWhy}");
            Expect(!Flow.DeckLabLocal, "실행 인자 -decklab 없음(공개판과 같은 조건)");
            DeckLabStore.Clear();
            IEnumerator Login(string sub)
            {
                if (Online.HasToken) { Online.Logout(); yield return Until(() => Online.Now == Online.State.Out, 10); }
                bool done = false;
                Online.TestLogin(sub, _ => done = true);
                yield return Until(() => done && !Online.Busy, 20);
                yield return Wait(0.5f);
            }
            // ① 일반 계정 — 진입점 없음
            yield return Login("lab_plain");
            Expect(Online.Ready && !Online.Tester, $"일반 계정 로그인 — {Online.Now} {Online.Nick} · 역할 {Online.Role ?? "없음"}");
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("o01_lobby_plain");
            Expect(Hot("lobby.decklab") == null, "일반 계정 — 로비에 「덱 만들기」 없음");
            // ② 시험 계정 — 진입점 보임
            yield return Login("lab_tester");
            Expect(Online.Tester, $"시험 계정 로그인 — {Online.Now} {Online.Nick} · 역할 {Online.Role}");
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("o02_lobby_tester");
            Expect(Hot("lobby.decklab") != null, "시험 계정 — 로비에 「덱 만들기」 보임");
            yield return Press("lobby.decklab", 0.2f);
            yield return Screen_("decklab", 1.2f);
            // ③ 덱 — 자리 둘(기본 카드 + 고유 한 장)
            yield return Press("decklab.new", 0.2f);
            yield return Screen_("decklab_edit", 1.0f);
            var data = f.P.Data;
            var heroes = Roster.Players.Select(h => h.CoreId).Where(k => k != null && data.Hero(k) != null && data.Cards.Values.Any(c => c.Hero == k && c.Unique)).Distinct().Skip(3).Take(2).ToList();
            for (int seat = 0; seat < heroes.Count; seat++)
            {
                yield return Press("lab.seat" + seat, 0.2f);
                yield return Screen_("decklab_edit", 0.6f);
                yield return Press("lab.hero", 1.0f);
                yield return Press("labhero:" + heroes[seat], 0.2f);
                yield return Screen_("decklab_edit", 0.8f);
                yield return Press("lab.filter:unique", 0.2f);
                yield return Screen_("decklab_edit", 0.6f);
                yield return Press("lab.cat0", 0.3f);
            }
            f.LabNameField.text = "서버 시험 덱";
            yield return Press("lab.save", 0.6f);
            DeckLabScreenNow();
            yield return Screen_("decklab", 1.2f);
            yield return Shot("o03_list_tester");
            Expect(Hot("decklab.server0") != null, "시험 계정 — 「서버 시험」 단추");
            // ④ 서버 시험 도전 — 시험 칸 저장 · 도전 · 제출
            yield return Press("decklab.server0", 0.5f);
            yield return Until(() => f.Stage.Current == "decklab_result", 900);
            yield return Screen_("decklab_result", 1.0f);
            yield return Until(() => f.LabSubmit != null && f.LabSubmit != "sending", 30);
            yield return Wait(1.0f);
            yield return Shot("o04_server_result");
            Expect(f.LabSubmit == "test", $"서버 시험 제출 → 시험 기록 — {f.LabSubmit}");
            Expect(DeckLabStore.All.FirstOrDefault()?.Test.Count(x => x > 0) == heroes.Count, $"자리마다 서버 시험 칸 id — [{string.Join(", ", DeckLabStore.All.FirstOrDefault()?.Test ?? new List<long>())}]");
            Online.Resp rk = null, ts = null;
            yield return Online.CallCo("GET", "/api/frontier/ranking", null, x => rk = x);
            yield return Online.CallCo("GET", "/api/test/slots", null, x => ts = x);
            var names = (rk?.Json?["rows"] as JArray)?.Select(x => (string)x["name"]).ToList() ?? new List<string>();
            Expect(rk != null && rk.Ok && !names.Contains(Online.Nick), $"시험 점수는 랭킹에 없다 — [{string.Join(", ", names)}] · 나 {Online.Nick}");
            Expect(ts != null && ts.Ok && (int?)ts.Json?["count"] == heroes.Count, $"서버 시험 칸 {ts?.Json?["count"]}");
            // ⑤ 내 기록 — 「시험」
            f.HistoryScreen(f.Lobby);
            yield return Screen_("frontier_history", 1.6f);
            yield return Shot("o05_history_test");
            Debug.Log($"[Demo] 덱 만들기(시험 계정) 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }

        void DeckLabScreenNow() => f.DeckLabScreen();
    }
}
