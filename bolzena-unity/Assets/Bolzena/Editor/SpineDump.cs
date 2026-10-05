using System.IO;
using System.Linq;
using System.Text;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

// 스파인 자료 훑기 — 동작 · 이벤트 · 스킨 · 크기를 글로 뽑는다(명령줄 -executeMethod SpineDump.Run)
namespace Bolzena.EditorTools {
public static class SpineDump {
  public static void Run() {
    var sb = new StringBuilder();
    foreach (var guid in AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { "Assets/Resources/Spine" })) {
      var path = AssetDatabase.GUIDToAssetPath(guid);
      var a = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
      var d = a.GetSkeletonData(false);
      sb.AppendLine("=== " + path + "  scale=" + a.scale);
      if (d == null) { sb.AppendLine("  (못 읽음)"); continue; }
      sb.AppendLine("  skins: " + string.Join(", ", d.Skins.Select(s => s.Name)));
      var sk = new Skeleton(d);
      sk.SetToSetupPose(); sk.UpdateWorldTransform();
      float x, y, w, h; float[] buf = null;
      sk.GetBounds(out x, out y, out w, out h, ref buf);
      sb.AppendLine($"  setup bounds x={x:F0} y={y:F0} w={w:F0} h={h:F0}");
      foreach (var an in d.Animations) {
        var evs = an.Timelines.OfType<EventTimeline>().SelectMany(t => t.Events.Select(e => e.Data.Name + "(" + e.Int + "|" + e.String + ")@" + e.Time.ToString("F2")));
        sb.AppendLine($"  anim {an.Name} {an.Duration:F2}s  {string.Join(" ", evs)}");
      }
      sb.AppendLine("  bones(point/fx): " + string.Join(", ", d.Bones.Select(b => b.Name).Where(n => n.ToLower().Contains("point") || n.ToLower().Contains("fx") || n.ToLower().Contains("hit"))));
    }
    File.WriteAllText("C:/projects/bolzena-unity-tmp/spinedump.txt", sb.ToString());
  }
}
}
