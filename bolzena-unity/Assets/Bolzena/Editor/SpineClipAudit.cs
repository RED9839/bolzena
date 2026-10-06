using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 클리핑 첨부 점검 — Resources/Spine 의 모든 SD 스파인에서 ClippingAttachment(가리기 모양)를 쓰는 스킨 · 슬롯과,
    // 그 슬롯을 켜는 애니를 적는다(미로 거울 · 2026-10-06). batchmode:
    //   Unity.exe -batchmode -quit -projectPath . -executeMethod Bolzena.EditorTools.SpineClipAudit.Run
    // → C:\projects\bolzena-unity-tmp\spine_clipping.tsv
    public static class SpineClipAudit
    {
        public static void Run()
        {
            var rows = new List<string> { "folder\tskin\tslot\tattachment\tendSlot\tanims" };
            foreach (var guid in AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { "Assets/Bolzena/Resources/Spine" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                SkeletonData d;
                try { d = a.GetSkeletonData(true); } catch { continue; }
                if (d == null) continue;
                var folder = Path.GetFileName(Path.GetDirectoryName(path));
                foreach (var skin in d.Skins)
                    foreach (var e in skin.Attachments)
                    {
                        if (!(e.Attachment is ClippingAttachment c)) continue;
                        var slot = d.Slots.Items[e.SlotIndex];
                        var anims = new List<string>();
                        foreach (var an in d.Animations)
                            foreach (var tl in an.Timelines)
                                if (tl is AttachmentTimeline at && at.SlotIndex == e.SlotIndex && at.AttachmentNames.Any(n => n == e.Name)) { anims.Add(an.Name); break; }
                        bool setup = slot.AttachmentName == e.Name;
                        rows.Add($"{folder}\t{skin.Name}\t{slot.Name}\t{e.Name}{(setup ? "(처음부터)" : "")}\t{(c.EndSlot != null ? c.EndSlot.Name : "-")}\t{string.Join(",", anims)}");
                    }
            }
            // 미로 — 슬롯 그리는 차례와 각 슬롯의 첨부 · 블렌드(MaskStart · MaskEnd 둘레를 본다)
            var md = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Bolzena/Resources/Spine/miro/miro_SkeletonData.asset")?.GetSkeletonData(true);
            if (md != null)
            {
                var lines = new List<string>();
                for (int i = 0; i < md.Slots.Count; i++)
                {
                    var sl = md.Slots.Items[i];
                    var atts = new List<string>();
                    foreach (var sk in md.Skins) foreach (var e in sk.Attachments) if (e.SlotIndex == i) atts.Add(sk.Name + ":" + e.Name + "(" + e.Attachment.GetType().Name + ")");
                    lines.Add($"{i}\t{sl.Name}\tbone={sl.BoneData.Name}\tsetup={sl.AttachmentName}\tblend={sl.BlendMode}\t{string.Join(" ", atts)}");
                }
                File.WriteAllLines(@"C:\projects\bolzena-unity-tmp\miro_slots.tsv", lines);
            }
            Directory.CreateDirectory(@"C:\projects\bolzena-unity-tmp");
            File.WriteAllLines(@"C:\projects\bolzena-unity-tmp\spine_clipping.tsv", rows);
            Debug.Log($"[ClipAudit] 클리핑 첨부 {rows.Count - 1}개");
        }
    }
}
