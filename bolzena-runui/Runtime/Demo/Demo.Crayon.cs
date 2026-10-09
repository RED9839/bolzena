using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 교주 보드 · 크레파스 보드 캡처(-demo-crayon, -save 로 따로 둔 진행만 쓴다) — 로비 진입점 → 보드(처음) → 칠하는 순간 · 뒤
    //   → 일부 칠한 보드(능력치 칸 단계 · 한 번 칸) → 설정(진행 코드) → 효과를 건 새 판 → 끝 화면 받은 크레파스. 끝나면 이 실행의 진행을 지운다.
    public partial class Demo
    {
        IEnumerator Crayon_()
        {
            // 진행 코드 왕복 — 빈 · 일부 · 꽉 참: 내보내기 → 지우기 → 불러오기 → 다시 내보낸 코드가 같다(보드 · 편성 프리셋 · 완주 기록)
            var keys = Roster.All.Where(h => h.Playable).Select(h => h.key).Take(6).ToList();
            for (int pass = 0; pass < 3; pass++)
            {
                CrayonStore.Reset(); PartyStore.ResetAll();
                if (pass >= 1)
                {
                    CrayonStore.Give(5, 2, 1, 0); CrayonStore.Save.Level["atk"] = 2; CrayonStore.Write();
                    PartyStore.SetRecent(keys.Take(3)); PartyStore.SetPreset(1, keys.Take(3)); PartyStore.SetTag(1, "광기"); PartyStore.SetCleared(keys[0], 2);
                }
                if (pass == 2)
                {
                    foreach (var c in CrayonStore.Table.Cells.Where(c => !c.Blank)) CrayonStore.Save.Level[c.Id] = c.Levels;
                    CrayonStore.Give(99, 40, 20, 9);
                    for (int i = 1; i <= PartyStore.PresetCount; i++) { PartyStore.SetPreset(i, keys.Skip(i % 3).Take(3)); PartyStore.SetTag(i, "냉정"); }
                    foreach (var k in keys) PartyStore.SetCleared(k, 5);
                }
                var rcode = CrayonStore.Export();
                CrayonStore.Reset(); PartyStore.ResetAll();
                var why = CrayonStore.Import(rcode);
                Expect(why == null && CrayonStore.Export() == rcode && rcode.Length > 10, $"진행 코드 왕복 — {(pass == 0 ? "빈" : pass == 1 ? "일부" : "꽉 참")} 상태({rcode.Length}자{(why != null ? " · " + why : "")})");
            }
            CrayonStore.Reset(); PartyStore.ResetAll();
            CrayonStore.Give(10, 2, 0, 0);
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.2f);
            yield return Shot("pastel_lobby");
            var menuIc = Hot("crayon") ? Hot("crayon").transform.Find("disc/icon")?.GetComponent<UnityEngine.UI.Image>() : null;
            Expect(menuIc != null && menuIc.sprite != null && menuIc.sprite.name == "RunArt/" + Theme.BoardIconArt, $"로비 「교주 보드」 아이콘 = 원작 크레파스({menuIc?.sprite?.name ?? "없음"})");
            yield return Press("crayon");
            yield return Screen_("crayon", 1.0f);
            yield return Shot("pastel_board");
            {
                var imgs = f.Stage.Root.GetComponentsInChildren<UnityEngine.UI.Image>(false);
                int abil = imgs.Count(im => im.sprite && im.sprite.name.StartsWith("RunArt/Board/"));
                int rep = imgs.Count(im => im.sprite && im.sprite.name == "RunArt/" + Theme.BoardIconArt && im.name == "boardicon");
                int cells = CrayonStore.Table.Cells.Count(c => !c.Blank);
                Expect(abil >= cells + 1, $"교주 보드 — 칸마다 원작 능력 아이콘(칸 {cells} + 자세히 1 · 보인 것 {abil})");
                Expect(rep >= 1, "교주 보드 제목 — 대표 아이콘(Theme.BoardIcon)");
            }
            Expect(CrayonArt(f.Stage.Root, out int bodd) >= 4 && bodd == 0, $"교주 보드 — 크레파스는 원작 그림만(원작 {CrayonArt(f.Stage.Root, out _)} · 다른 그림 {bodd})");
            yield return Press("crayon.cell0", 0.1f);
            yield return Screen_("crayon", 0.4f);
            yield return Press("crayon.paint", 0.25f);
            yield return Shot("pastel_painting");
            yield return Screen_("crayon", 0.8f);
            yield return Shot("pastel_painted");
            // 일부 칠함 — 공격 3단계 · 체력 2 · 방어 1 · 치명 확률 2 · 시작 골드 · 시작 학점
            CrayonStore.Give(40, 10, 4, 2);
            foreach (var (cell, n) in new[] { (0, 2), (1, 2), (2, 1), (3, 2), (6, 1), (9, 1) })
            {
                yield return Press("crayon.cell" + cell, 0.1f); yield return Screen_("crayon", 0.2f);
                for (int k = 0; k < n; k++) { yield return Press("crayon.paint", 0.1f); yield return Wait(1.0f); yield return Screen_("crayon", 0.2f); }
            }
            yield return Press("crayon.cell4", 0.1f);
            yield return Screen_("crayon", 0.8f);
            yield return Shot("pastel_partial");
            var sv = CrayonStore.Save;
            Expect(sv.LevelOf("atk") == 3 && sv.LevelOf("hp") == 2 && sv.LevelOf("def") == 1 && sv.LevelOf("crit") == 2 && sv.LevelOf("gold") == 1 && sv.LevelOf("credits") == 1,
                $"보드 — 공격 3 · 체력 2 · 방어 1 · 치명 2 · 골드 · 학점 (지금 {string.Join(", ", sv.Level.Select(kv => kv.Key + " " + kv.Value))})");
            // 높은 단계 칸 — 공격 13단계(다음은 최상급) · 체력 다 칠함 · 고른 칸은 공격
            sv.Level["atk"] = 13; sv.Level["hp"] = 15; CrayonStore.Write();
            yield return Press("crayon.cell0", 0.1f);
            yield return Screen_("crayon", 0.8f);
            yield return Shot("pastel_high");
            sv.Level["atk"] = 3; sv.Level["hp"] = 2; CrayonStore.Write();
            var code = CrayonStore.Export();
            Expect(Core.Crayon.Import(code, CrayonStore.Table).why == null, "진행 코드 — 내보낸 코드를 다시 읽는다");
            Expect(Core.Crayon.Import(code.Substring(0, code.Length - 1) + (code.EndsWith("0") ? "1" : "0"), CrayonStore.Table).why != null, "진행 코드 — 체크섬이 틀리면 거부");
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("settings", 0.8f);
            yield return Shot("pastel_settings");
            yield return Press("settings.code", 0.8f);
            yield return Shot("code_panel");
            yield return Press("code.copy", 0.6f);
            yield return Shot("code_copied");
            var copied = GUIUtility.systemCopyBuffer;
#if !UNITY_WEBGL || UNITY_EDITOR   // 웹은 브라우저 클립보드(jslib)라 systemCopyBuffer 가 비어 있다 — 복사 토스트 · 브라우저 클립보드로 따로 확인
            Expect(copied == CrayonStore.Export(), $"진행 코드 복사 — 클립보드에 코드가 들어갔다({copied?.Length ?? 0}자)");
#endif
            var keep = CrayonStore.Export();
            if (f.CodeInField) f.CodeInField.text = keep;
            yield return Press("code.load", 0.6f);
            yield return Shot("code_confirm");
            yield return Press("confirm.yes", 0.6f);
            Expect(CrayonStore.Export() == keep, "진행 코드 불러오기 — 덮어쓴 뒤 같은 진행");
            CloseModals();
            // 효과를 건 새 판 → 바로 끝(진 판)으로 크레파스 받기
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.6f);
            Debug.Log($"[Demo] 보드 판 — 골드 {f.P.S.Gold} · 학점 {f.P.S.Credits} · 최대 HP {f.P.S.PartyMaxHp} · {string.Join(", ", (f.P.S.Perks ?? new System.Collections.Generic.Dictionary<string, double>()).Select(kv => kv.Key + " " + kv.Value))}");
            var want = CrayonStore.Perks;   // 칠한 칸의 효과 그대로(값은 표에서 — 숫자를 바꿔도 단언은 그대로)
            Expect(want.Count > 0 && want.All(kv => System.Math.Abs(f.P.Run.Perk(kv.Key) - kv.Value) < 1e-9) && (!want.ContainsKey("credits") || f.P.S.Credits >= (int)want["credits"]), $"새 판 — 보드 효과가 걸렸다({string.Join(", ", want.Select(kv => kv.Key + " " + kv.Value))})");
            int before = CrayonStore.Save.Have.Sum();
            f.P.S.Grade = 5; f.P.S.Floor = 1;   // 2층 · 5학년에서 졌다고 치고
            f.P.S.Hist.Add(new Core.FightRecord { Kind = "elite", Result = "win" });
            f.P.S.Hist.Add(new Core.FightRecord { Kind = "boss", Result = "win" });
            f.End("lose");
            yield return Screen_("end_lose", 2.2f);
            yield return Shot("pastel_end");
            Expect(CrayonArt(f.Stage.Root, out int eodd) >= 4 && eodd == 0, $"끝 화면 — 받은 크레파스는 원작 그림({CrayonArt(f.Stage.Root, out _)}장)");
            Expect(CrayonStore.Save.Have.Sum() > before, $"끝 화면 — 크레파스를 받았다({before} → {CrayonStore.Save.Have.Sum()} · {string.Join("/", f.LastCrayons)})");
            CrayonStore.Reset(); PartyStore.ResetAll();
            Debug.Log("[Demo] 크레파스 캡처 끝");
            yield return Wait(0.4f);
            Application.Quit(0);
        }

        /// <summary>화면의 크레파스 그림 수 — 원작(RunArt/Item/Item_Crayon) 개수, odd 는 크레파스 칸(이름 pastel · tier · t0~3)에 원작 아닌 그림이 든 수.</summary>
        static int CrayonArt(Transform root, out int odd)
        {
            int n = 0; odd = 0;
            foreach (var im in root.GetComponentsInChildren<UnityEngine.UI.Image>(false))
            {
                var nm = im.sprite ? im.sprite.name : "";
                if (nm.StartsWith("RunArt/Item/Item_Crayon")) { n++; continue; }
                var par = im.transform.parent ? im.transform.parent.name : "";
                if ((par == "pastel" || par == "tier" || par == "stick") && im.name != "art") odd++;
            }
            return n;
        }
    }
}
