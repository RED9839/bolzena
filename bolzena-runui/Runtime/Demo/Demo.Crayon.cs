using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 교주 능력치 · 크레파스 보드 캡처(-demo-crayon, -save 로 따로 둔 진행만 쓴다) — 로비 진입점 → 보드(처음) → 칠하는 순간 · 뒤
    //   → 일부 칠한 보드(능력치 칸 단계 · 한 번 칸) → 설정(진행 코드) → 효과를 건 새 판 → 끝 화면 받은 크레파스. 끝나면 이 실행의 진행을 지운다.
    public partial class Demo
    {
        IEnumerator Crayon_()
        {
            CrayonStore.Reset();
            CrayonStore.Give(10, 2, 0, 0);
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.2f);
            yield return Shot("pastel_lobby");
            yield return Press("crayon");
            yield return Screen_("crayon", 1.0f);
            yield return Shot("pastel_board");
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
            Expect(want.Count > 0 && want.All(kv => System.Math.Abs(f.P.Run.Perk(kv.Key) - kv.Value) < 1e-9) && f.P.S.Credits >= (int)want["credits"], $"새 판 — 보드 효과가 걸렸다({string.Join(", ", want.Select(kv => kv.Key + " " + kv.Value))})");
            int before = CrayonStore.Save.Have.Sum();
            f.P.S.Grade = 5; f.P.S.Floor = 1;   // 2층 · 5학년에서 졌다고 치고
            f.P.S.Hist.Add(new Core.FightRecord { Kind = "elite", Result = "win" });
            f.P.S.Hist.Add(new Core.FightRecord { Kind = "boss", Result = "win" });
            f.End("lose");
            yield return Screen_("end_lose", 2.2f);
            yield return Shot("pastel_end");
            Expect(CrayonStore.Save.Have.Sum() > before, $"끝 화면 — 크레파스를 받았다({before} → {CrayonStore.Save.Have.Sum()} · {string.Join("/", f.LastCrayons)})");
            CrayonStore.Reset();
            Debug.Log("[Demo] 크레파스 캡처 끝");
            yield return Wait(0.4f);
            Application.Quit(0);
        }
    }
}
