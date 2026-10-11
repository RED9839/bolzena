using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 지도 길이 점검(-demo-maplen, 2026-10-10 층 10 → 17 칸) — 1층 지도를 처음 · 중간(1-8) · 보스 앞(1-16)에서 찍는다.
    //   지나온 길은 출발에서 첫 갈래를 따라 걸은 것으로 채운다(지나온 칸 · 금빛 길 · 지금 칸 · 갈 수 있는 칸이 함께 보이게).
    public partial class Demo
    {
        IEnumerator MapLen_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(2.0f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.8f);
            yield return Shot("maplen_start");

            var m = f.P.Map();
            int last = m.Rows.Count - 1;
            foreach (var (tag, row) in new[] { ("mid", last / 2), ("bossfront", last - 1) })
            {
                var at = m.Rows[0][0];
                m.Seen.Clear(); m.Seen.Add(at.Id);
                while (at.Row < row) { var id = at.Next[0]; at = m.Rows.SelectMany(r => r).First(n => n.Id == id); m.Seen.Add(at.Id); }
                m.At = at.Id;
                f.Map();
                yield return Screen_("map", 2.2f);
                yield return Shot("maplen_" + tag);
                Debug.Log($"[Demo] 지도 {tag} — 칸 {at.Id} ({f.P.StageName(at)} / {f.P.S.Floor + 1}-{last})");
            }
            Debug.Log("[Demo] 지도 길이 점검 끝");
            yield return Wait(0.4f);
            Application.Quit(0);
        }
    }
}
