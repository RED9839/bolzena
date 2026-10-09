using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 플레이 기록 캡처 · 점검(-demo-rec) — 처음 실행 안내 한 줄 · 설정의 「플레이 기록 보내기(익명)」 스위치를 찍고,
    //   봇 싸움으로 짧은 판을 끝까지(지기) 해서 판 끝 기록이 파일로 남는지(보내기는 막힌 상태 — 데모) 본다. -recdir 폴더를 주면 거기에.
    //   설정 값은 바꾸지 않는다(스위치는 두 번 눌러 되돌린다).
    public partial class Demo
    {
        IEnumerator Rec_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Wait(1.6f);
            Toast.Show(PlayRecord.NOTICE, 7f);   // 처음 실행 안내(데모는 NoticeOnce 가 건너뛰니 직접 띄운다)
            yield return Wait(0.6f);
            yield return Shot("rec_notice");
            f.SettingsPanel(false);
            yield return Wait(0.8f);
            yield return Press("settings.tab1", 0.7f);
            var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            if (sr != null) { sr.verticalNormalizedPosition = 0; yield return Wait(0.4f); }
            yield return Shot("rec_settings");
            bool was = PlayRecord.On;
            yield return Press("set.rec", 0.5f);
            Expect(PlayRecord.On != was, "기록 스위치가 값을 바꿈");
            yield return Shot("rec_settings_off");
            yield return Press("set.rec", 0.5f);
            Expect(PlayRecord.On == was, "기록 스위치가 처음 값으로");
            yield return Press("settings.close", 0.5f);

            // 짧은 판 — 봇이 지게 싸워 판을 끝낸다(End("lose") → PlayRecord.Send → 데모라 파일)
            Expect(!PlayRecord.Allowed, "데모에서는 보내지 않음");
            var P = f.P;
            var party = Roster.All.Where(h => h.Playable).Take(3).Select(h => h.CoreId).ToList();
            P.NewRun(party, P.RollVillage(), 4242);
            for (int k = 0; k < 6 && P.S.PartyHp > 0; k++)
            {
                P.Map();
                var id = P.Reachable().FirstOrDefault(x => { var n = Bolzena.Core.Run.NodeById(P.Map(), x); return n.Type == "fight" || n.Type == "elite" || n.Type == "boss"; }) ?? P.Reachable().First();
                var node = P.Enter(id);
                if (node.Type != "fight" && node.Type != "elite" && node.Type != "boss") continue;
                var o = P.AutoFight(k >= 1 ? "lose" : "bot");
                if (!o.Won) break;
                P.TakeGold();
                P.ClearElite();
            }
            string dir = Arg("-recdir") ?? Path.Combine(Application.persistentDataPath, "play-records");
            int before = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json").Length : 0;
            f.End("lose");
            yield return Wait(2.5f);
            yield return Shot("rec_end");
            var files = Directory.Exists(dir) ? new DirectoryInfo(dir).GetFiles("*.json").OrderByDescending(x => x.LastWriteTimeUtc).ToList() : new System.Collections.Generic.List<FileInfo>();
            Expect(files.Count > before, "판 끝 기록이 파일로 남음");
            if (files.Count > 0)
            {
                var json = File.ReadAllText(files[0].FullName);
                var why = Bolzena.Core.RunRecord.Check(json);
                Expect(why == null, "남은 기록이 받는 쪽 검사를 통과 " + why);
                var r = Bolzena.Core.RunRecord.Parse(json);
                Debug.Log($"[RecDemo] {files[0].FullName} · {files[0].Length / 1024f:0.0}KB · {r.Result} · 싸움 {r.Fights.Count} · 선택 {r.Picks?.Count ?? 0} · 시작 {r.Began} · 끝 {r.Ended}");
            }
            Debug.Log("[Demo] 플레이 기록 단언 실패 " + fails);
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }
    }
}
