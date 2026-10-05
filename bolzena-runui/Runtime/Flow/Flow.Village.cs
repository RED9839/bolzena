using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 마을 공개 — 웹판 ui.js villageScreen(docs/20-마을.md §0). 모험을 시작하면 마을 하나가 무작위로 정해져 보인다.
    // 웹판보다: 마을 이름이 슬롯처럼 돌다가 멈추고, 두 층이 차례로 펼쳐진다.
    public partial class Flow
    {
        public void VillageReveal(string villageId, Action onGo)
        {
            var v = P.Village(villageId);
            var f0 = v.Floors[0];
            Stage.SetBg(f0.Bg != null && f0.Bg.TryGetValue("fight", out var bg) ? bg : "stage3_2", 0.55f);
            Stage.Show("village", root =>
            {
                var panel = Ui.Panel(root, Theme.Panel, null, "card").rectTransform;
                panel.At(0.5f, 0.5f, 0, 0, 680, 560);
                Ui.Shadow(panel, 30, -14, 0.7f);
                Tw.Pop(panel, 0.05f, 0.85f, 0.5f);

                var kick = Ui.Title(panel, "이번 모험의 마을", Theme.FsMd, Theme.Gold, TextAlignmentOptions.Center);
                kick.rectTransform.Band(1, 30, 0, 0, -30);
                var name = Ui.Title(panel, v.Name, 72, Theme.Ink, TextAlignmentOptions.Center);
                name.rectTransform.Band(1, 90, 0, 0, -62);
                name.Outline(0.12f);
                var race = Ui.Chip(panel, null, v.Race ?? "", 34, Theme.NavyCell, null, Theme.FsSm);
                race.bg.rectTransform.At(0.5f, 1, 0, -160, 200, 36);
                var fit = race.bg.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                fit.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                var line = Ui.Text(panel, v.Line ?? "", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center);
                line.rectTransform.Band(1, 60, 40, 40, -206);

                var floors = Ui.Rect("floors", panel).Band(1, 120, 40, 40, -276);
                Ui.Col(floors, 10, TextAnchor.UpperCenter, null, true, false);
                for (int i = 0; i < v.Floors.Count; i++)
                {
                    var fl = v.Floors[i];
                    var row = Ui.Img(floors, Theme.Cell, Color.white, "floor" + i);
                    row.Pref(-1, 54);
                    var t = Ui.Title(row.transform, $"<color={Theme.GoldTag}>{i + 1}층</color>    {fl.Name}   <size=68%><color={Theme.SubTag}>{fl.Sub}</color></size>", Theme.FsLg, Theme.Ink);
                    t.rectTransform.Fill(22, 0, 10, 0);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    Tw.Pop(row.rectTransform, 1.1f + i * 0.18f, 0.8f, 0.4f);
                }
                var note = Ui.Text(panel, $"1-1 부터 {v.Floors.Count}-10 까지 이 마을의 적만 나옵니다. {v.Floors.Count}층 보스를 이기면 판을 이깁니다.", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
                note.rectTransform.Band(0, 30, 30, 30, 112);
                var row2 = Ui.Rect("buttons", panel).Band(0, 66, 40, 40, 34);
                Ui.Row(row2, 16, TextAnchor.MiddleCenter, null, false, true);
                var back = Btn.Make(row2, "로비로", BtnStyle.Dark, Lobby, Theme.FsMd);
                back.Pref(200, 62);
                var go = Btn.Make(row2, "파티를 짭니다", BtnStyle.Gold, onGo, Theme.FsLg);
                go.Pref(380, 62);
                Stage.Hot["go"] = go;

                // 이름 굴리기 — 다른 마을 이름(없으면 층 이름)을 돌리다가 멈춘다
                var pool = P.Villages.Select(x => x.Name).Concat(v.Floors.Select(x => x.Name)).Distinct().ToList();
                StartCoroutine(Roll(name, pool, v.Name));
            }, 1.6f);
        }

        IEnumerator Roll(TextMeshProUGUI t, System.Collections.Generic.List<string> pool, string final)
        {
            float dt = 0.05f;
            int i = 0;
            float total = Settings.ReduceMotion ? 0.1f : 0.9f, el = 0;
            while (el < total && t != null)
            {
                t.text = pool[i++ % pool.Count];
                t.color = Theme.Sub;
                yield return new WaitForSecondsRealtime(dt);
                el += dt;
                dt *= 1.18f;
            }
            if (t == null) yield break;
            t.text = final;
            t.color = Theme.Ink;
            var rt = t.rectTransform;
            Tw.Run(rt, 0.4f, k => { if (rt) rt.localScale = Vector3.one * Mathf.LerpUnclamped(1.35f, 1, k); }, Tw.OutBack);
            var glow = Ui.Img(rt.parent, Theme.S("soft"), new Color(1f, 0.85f, 0.45f, 0), "flash");
            glow.rectTransform.At(0.5f, 1, 0, -60, 520, 160);
            glow.transform.SetSiblingIndex(0);
            Tw.Run(glow, 0.9f, k => { if (glow) glow.color = new Color(1f, 0.85f, 0.45f, 0.7f * (1 - k)); }, Tw.Linear);
            Sfx.Play("reveal");
        }
    }
}
