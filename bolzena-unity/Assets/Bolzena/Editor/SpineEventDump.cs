using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 원작 스파인 이벤트 표 — 엘다인 20명의 동작마다 이벤트 이름 · 시각 · 문자열(원작 ID 목록) · 정수 · 실수를 적는다.
    // 「이벤트 ID → 이펙트」 짝 표를 만드는 바탕. batchmode:
    //   Unity.exe -batchmode -quit -projectPath . -executeMethod Bolzena.EditorTools.SpineEventDump.Run
    // → C:\projects\bolzena-unity-tmp\spine_events\<폴더>.tsv (동작 · 길이ms · 이벤트 · 시각ms · int · float · string)
    public static class SpineEventDump
    {
        static readonly string[] FOLDERS = {
            "aya", "xxionx", "epica", "vivi", "chloe", "ed", "ui", "uimemory", "renewaawaken", "silvia",
            "vividivine", "erpinroyale", "ran", "dianayester", "vela", "joanne", "uros", "tighero", "nerrage", "yomi" };

        public static void Run()
        {
            var outDir = @"C:\projects\bolzena-unity-tmp\spine_events";
            Directory.CreateDirectory(outDir);
            var only = System.Environment.GetEnvironmentVariable("SPINE_DUMP_FOLDERS");
            var want = string.IsNullOrEmpty(only) ? FOLDERS : only.Split(',');
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { "Assets/Bolzena/Resources/Spine" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var folder = Path.GetFileName(Path.GetDirectoryName(path));
                if (!want.Contains(folder)) continue;
                var a = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                SkeletonData d;
                try { d = a.GetSkeletonData(true); } catch { continue; }
                if (d == null) continue;
                var lines = new List<string> { "anim\tdur_ms\tevent\tt_ms\tint\tfloat\tstring" };
                foreach (var an in d.Animations)
                {
                    int dur = Mathf.RoundToInt(an.Duration * 1000);
                    bool any = false;
                    foreach (var tl in an.Timelines)
                        if (tl is EventTimeline et)
                            foreach (var ev in et.Events)
                            {
                                lines.Add($"{an.Name}\t{dur}\t{ev.Data.Name}\t{Mathf.RoundToInt(ev.Time * 1000)}\t{ev.Int}\t{ev.Float:0.###}\t{ev.String}");
                                any = true;
                            }
                    if (!any) lines.Add($"{an.Name}\t{dur}\t\t\t\t\t");
                }
                File.WriteAllLines(Path.Combine(outDir, folder + ".tsv"), lines);
                n++;
            }
            Debug.Log($"[EventDump] {n}개 폴더");
        }
    }
}
