using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bolzena.Core.EditorTools
{
    /// <summary>
    /// 유니티 batchmode 에서 도는 시뮬 · 검사 입구.
    ///   Unity.exe -batchmode -nographics -projectPath &lt;프로젝트&gt; -executeMethod Bolzena.Core.EditorTools.SimCli.MetaSim
    ///       [-data &lt;JSON 폴더&gt;] [-rounds 30] [-seed 0] [-hp 1] [-dmg 1] [-threads N] [-with 사도] [-out &lt;결과 파일&gt;] -logFile &lt;로그&gt;
    ///   … -executeMethod Bolzena.Core.EditorTools.SimCli.Validate [-data &lt;JSON 폴더&gt;] [-out &lt;결과 파일&gt;]
    /// -data 가 없으면 패키지의 Data/Sample. 결과는 로그와 -out 파일(없으면 프로젝트의 meta-sim.txt)에 쓴다. 끝나면 유니티를 닫는다(실패면 종료 코드 1).
    /// </summary>
    public static class SimCli
    {
        static string Arg(string name, string d)
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "-" + name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : d;
        }

        static GameData Load()
        {
            var dir = Arg("data", null) ?? Path.GetFullPath("Packages/com.bolzena.core/Data/Sample");
            return GameData.FromFolders(dir.Split(';').Where(x => x.Length > 0));   // 폴더 · 파일, ; 로 여럿
        }

        static void Write(string text, string fallback)
        {
            var path = Arg("out", fallback);
            File.WriteAllText(path, text);
            Debug.Log(text);
            Debug.Log($"→ {Path.GetFullPath(path)}");
        }

        public static void MetaSim()
        {
            int code = 0;
            try
            {
                var d = Load();
                var v = Validator.Check(d);
                if (!v.Ok) throw new Exception("데이터 검사 실패\n" + v);
                int rounds = int.Parse(Arg("rounds", "30")), seed = int.Parse(Arg("seed", "0")), threads = int.Parse(Arg("threads", "0"));
                double hp = double.Parse(Arg("hp", "1"), System.Globalization.CultureInfo.InvariantCulture);
                double dmg = double.Parse(Arg("dmg", "1"), System.Globalization.CultureInfo.InvariantCulture);
                var r = Core.MetaSim.Run(d, rounds, seed, hp, dmg, threads, with: Arg("with", null));
                Write(Core.MetaSim.Report(d, r), "meta-sim.txt");
            }
            catch (Exception e) { Debug.LogError(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public static void Validate()
        {
            int code = 0;
            try
            {
                var d = Load();
                var v = Validator.Check(d);
                var tx = new CardText(d);
                var lines = d.Cards.Values.Select(c => $"{c.Id} [{c.Cost}] {c.Name}: {tx.Card(c)}").ToList();
                Write((v.Ok ? "검사 통과" : "검사 실패") + "\n" + v + "\n\n" + string.Join("\n", lines), "validate.txt");
                if (!v.Ok) code = 1;
            }
            catch (Exception e) { Debug.LogError(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
