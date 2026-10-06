using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 애니 점검 — 고른 SD 스파인의 애니마다 길이 · 이벤트 시각 · 끝 자세의 첨부(슬롯 → 첨부)를 적는다.
    // 변신 쉬는 동작 고르기(고학년 끝 자세와 첫 자세가 같은 Idle_*) · 원작 방식 고학년 조각 나누기에 쓴다. batchmode:
    //   Unity.exe -batchmode -quit -projectPath . -executeMethod Bolzena.EditorTools.SpineAnimDump.Run
    // → C:\projects\bolzena-unity-tmp\spine_anim_dump.txt
    public static class SpineAnimDump
    {
        static readonly string[] FOLDERS = { "rimchaos", "laika", "edrehab", "kishya", "aurora", "ashurmagi", "ran", "kidian" };

        public static void Run()
        {
            var lines = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { "Assets/Bolzena/Resources/Spine" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var folder = Path.GetFileName(Path.GetDirectoryName(path));
                if (!FOLDERS.Contains(folder)) continue;
                var a = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                SkeletonData d;
                try { d = a.GetSkeletonData(true); } catch { continue; }
                if (d == null) continue;
                lines.Add($"## {folder}");
                var sk = new Skeleton(d);
                sk.SetSkin(d.DefaultSkin ?? d.Skins.Items[0]);
                foreach (var an in d.Animations)
                {
                    var evs = new List<string>();
                    foreach (var tl in an.Timelines) if (tl is EventTimeline et) foreach (var ev in et.Events) evs.Add($"{ev.Data.Name}@{Mathf.RoundToInt(ev.Time * 1000)}");
                    lines.Add($"{an.Name}\t{Mathf.RoundToInt(an.Duration * 1000)}ms\t{string.Join(" ", evs)}");
                    // 처음 · 끝 자세의 첨부
                    foreach (var (tag, t) in new[] { ("첫", 0f), ("끝", an.Duration) })
                    {
                        sk.SetToSetupPose();
                        an.Apply(sk, 0, t, false, null, 1f, MixBlend.Setup, MixDirection.In);
                        var on = new List<string>();
                        foreach (var s in sk.Slots) if (s.Attachment != null && s.A > 0.01f) on.Add(s.Data.Name + "=" + s.Attachment.Name);
                        lines.Add($"   {tag}\t{on.Count}\t{string.Join(" ", on)}");
                    }
                }
            }
            Directory.CreateDirectory(@"C:\projects\bolzena-unity-tmp");
            File.WriteAllLines(@"C:\projects\bolzena-unity-tmp\spine_anim_dump.txt", lines);
            Debug.Log($"[AnimDump] {lines.Count}줄");
        }
    }
}
