using System;
using System.IO;
using System.Linq;
using Bolzena.Core;
using UnityEditor;
using UnityEngine;

namespace Bolzena.RunUI.EditorTools
{
    /// <summary>
    /// 플레이 기록 점검(배치) — 게임 데이터로 봇이 판을 몇 개 끝까지 하고, 판 끝마다 PlayRecord.Send 를 부른다.
    /// 에디터 · 시험 실행이라 보내지 않고(PlayRecord.Allowed = false) -recdir 폴더에 파일로 남는지, 남은 파일이 받는 쪽 검사를 통과하는지 본다.
    ///   Unity.exe -batchmode -quit -projectPath C:\projects\bolzena-unity -executeMethod Bolzena.RunUI.EditorTools.PlayRecordCheck.Run -recdir 폴더 [-recruns 3]
    /// </summary>
    public static class PlayRecordCheck
    {
        public static void Run()
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "-recdir");
            string dir = i >= 0 && i < a.Length - 1 ? a[i + 1] : null;
            int j = Array.IndexOf(a, "-recruns");
            int runs = j >= 0 && j < a.Length - 1 && int.TryParse(a[j + 1], out var n) ? n : 3;
            int code = 0;
            try
            {
                if (dir == null) throw new Exception("-recdir 폴더를 주세요");
                Debug.Log($"[RecCheck] 보내도 되는 곳인가: {PlayRecord.Allowed}(에디터 — 아니어야 한다) · 설정 켬 {PlayRecord.On}");
                if (PlayRecord.Allowed) throw new Exception("에디터에서 보내기가 열려 있다");
                var port = RunPort.Boot();
                var party = port.Data.Heroes.Keys.OrderBy(x => x, StringComparer.Ordinal).Take(3).ToList();
                int before = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json").Length : 0;
                for (int k = 0; k < runs; k++)
                {
                    new RunBot(port.Data).RunFull(party, 700 + k, new SimOpts
                    {
                        Skilled = true,
                        OnEnd = run =>
                        {
                            run.S.Rec.Began = RunRecord.Minute(DateTime.UtcNow.AddMinutes(-25));
                            run.S.Rec.FightSec = 600; run.S.Rec.FastSec = 150;
                            PlayRecord.Send(run, run.S.Done == "clear" ? "win" : "lose");
                        },
                    });
                }
                var files = new DirectoryInfo(dir).GetFiles("*.json").OrderByDescending(f => f.LastWriteTimeUtc).ToList();
                if (files.Count - before < runs) throw new Exception($"파일이 {files.Count - before}개 — {runs}개여야 한다");
                foreach (var f in files.Take(runs))
                {
                    var json = File.ReadAllText(f.FullName);
                    var why = RunRecord.Check(json);
                    var r = RunRecord.Parse(json);
                    Debug.Log($"[RecCheck] {f.Name} · {f.Length / 1024f:0.0}KB · {r.Result} · 싸움 {r.Fights.Count} · 턴 기록 {r.Fights.Count(x => x.Log != null)} · 선택 {r.Picks?.Count ?? 0} · 배속 {r.Fast} · {r.Min}분 · 검사 {(why ?? "통과")}");
                    if (why != null) code = 1;
                }
            }
            catch (Exception e) { Debug.LogError("[RecCheck] 실패: " + e); code = 1; }
            Debug.Log("[RecCheck] 끝 " + (code == 0 ? "통과" : "실패"));
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
