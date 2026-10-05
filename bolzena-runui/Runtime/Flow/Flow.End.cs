using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 끝 화면 — 웹판 ui.js endScreen. 이긴 판 · 진 판. 파티 · 지나온 싸움 · 덱 · 골드를 한 장에.
    public partial class Flow
    {
        void EndScreen(string kind)
        {
            bool win = kind == "clear";
            var f = P.Floor;
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue(win ? "boss" : "fight", out var bg) ? bg : "stage3_3", win ? 0.4f : 0.8f);
            Stage.Show("end_" + kind, root =>
            {
                var glow = Ui.Img(root, Theme.S("soft"), (win ? Theme.Gold : Theme.Bad).A(0.35f), "glow");
                glow.rectTransform.At(0.5f, 1, 0, 40, 1300, 520);
                Tw.Pulse(glow, 0.2f, 0.45f, 2.2f);
                var t = Ui.Title(root, win ? "모험 완주!" : "쓰러졌습니다", Theme.C(Theme.FsDisplay, 72), Color.white, TextAlignmentOptions.Center);
                t.rectTransform.At(0.5f, 1, 0, Theme.C(-36, -18), 1200, Theme.C(116, 92));
                var c = win ? Theme.Gold : Theme.Bad;
                t.colorGradient = new VertexGradient(Color.white, Color.white, c, c);
                t.Outline(0.12f, new Color(0.15f, 0.05f, 0, 1));
                Tw.Pop(t.rectTransform, 0.1f, 1.6f, 0.6f);
                var v = P.VillageDef;
                var s = Ui.Text(root, win ? $"{v.Name} 두 층을 모두 넘었습니다 — {string.Join(" → ", v.Floors.Select(x => x.Name))}"
                    : $"{v.Name} {P.S.Floor + 1}층 {f.Name} · {(P.IsBoss ? "보스" : $"{P.S.Step}번째 칸")}에서 멈췄습니다.", Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
                s.rectTransform.At(0.5f, 1, 0, Theme.C(-156, -112), 1200, 32);
                s.Outline(0.2f);

                var panel = Ui.Panel(root, Theme.Panel, null, "summary").rectTransform;
                panel.At(0.5f, 1, 0, Theme.C(-206, -154), 1180, Stage.Size.y - Theme.C(206, 154) - Theme.C(128, 104));
                Tw.Rise(panel, 0.3f, 40, 0.5f);
                // 파티
                var party = Ui.Rect("party", panel).Column(0, 360, 24, 24, 24);
                Ui.Col(party, 10, TextAnchor.UpperLeft, null, true, false);
                W.Section(party, "파티", null, 34);
                foreach (var k in P.S.Party)
                {
                    var h = Roster.OfCore(k);
                    var row = Ui.Rect(k, party); row.Pref(-1, Theme.C(74, 62));
                    float fs = Theme.C(64, 54);
                    var face = W.Face(row, h, fs); face.At(0, 0.5f, 0, 0, fs, fs);
                    var nm = Ui.Title(row, h.ko, Theme.FsLg, Theme.Ink); nm.rectTransform.At(0, 1, fs + 16, -4, 260, 30);
                    var gear = P.GearOf(k).Values.Select(id => P.Data.Equip(id)?.Name).Where(x => x != null);
                    var gt = Ui.Text(row, gear.Any() ? string.Join(" · ", gear) : "장비 없음", Theme.FsCap, Theme.Sub); gt.rectTransform.At(0, 1, fs + 18, -36, 270, 24);
                    gt.textWrappingMode = TextWrappingModes.NoWrap;
                }
                var nums = Ui.Text(party, $"<color={Theme.GoldTag}>골드</color> {P.S.Gold:N0}  ·  <color={Theme.GoldTag}>덱</color> {P.S.Deck.Count}장  ·  <color={Theme.GoldTag}>HP</color> {P.S.PartyHp:N0}/{P.S.PartyMaxHp:N0}", Theme.FsSm, Theme.Ink);
                nums.Pref(-1, 50);
                // 지나온 싸움
                var vr = Ui.Img(panel, Theme.White, Theme.Line, "vrule"); vr.rectTransform.Column(0, 1, 24, 24, 392);
                var hist = Ui.Rect("hist", panel).Fill(412, 24, 24, 24);
                Ui.Col(hist, 4, TextAnchor.UpperLeft, null, true, false);
                W.Section(hist, "지나온 싸움", null, 34);
                foreach (var r in P.S.Hist.TakeLast(Theme.Compact ? 7 : 9))
                {
                    string kindKo = r.Kind == "boss" ? "보스" : r.Kind == "elite" ? "엘리트" : r.Kind == "event" ? "이벤트" : "일반";
                    var line = Ui.Text(hist, $"<color=#F2CF7A>{r.Floor}층</color>  {kindKo} · {string.Join(" · ", r.Foes.Select(id => P.Data.Enemy(id)?.Name ?? id).Distinct())}  <color=#A7B1CC>{r.Turns}턴 · HP {r.HpBefore}→{r.HpAfter}</color>  {(r.Result == "win" ? "<color=#6EE0A0>승리</color>" : "<color=#FF7A86>패배</color>")}", Theme.FsSm, Theme.Ink);
                    line.Pref(-1, Theme.C(34, 30));
                    line.textWrappingMode = TextWrappingModes.NoWrap;
                }
                if (P.S.Hist.Count == 0) Ui.Text(hist, "싸움이 없었습니다", 18, Theme.Sub).Pref(-1, 30);

                var go = Btn.Make(root, "로비로", BtnStyle.PillGold, Lobby, Theme.FsLg);
                go.GetComponent<RectTransform>().At(0.5f, 0, 0, Theme.C(34, 22), 340, Theme.C(70, 64));
                Stage.Hot["end.lobby"] = go;
                Tw.Rise(go.GetComponent<RectTransform>(), 0.6f, 20);
                if (win) StartCoroutine(Confetti(root));
            }, 1.6f);
        }

        System.Collections.IEnumerator Confetti(RectTransform root)
        {
            var cols = new[] { Theme.Gold, Theme.Sky, Theme.Good, new Color(1f, 0.6f, 0.8f), Color.white };
            for (int n = 0; n < 90 && root; n++)
            {
                var p = Ui.Img(root, Theme.Round, cols[n % cols.Length], "c");
                var rt = p.rectTransform;
                rt.At(0.5f, 1, Random.Range(-800f, 800f), 20, Random.Range(8, 14), Random.Range(14, 22));
                var start = rt.anchoredPosition;
                float fall = Random.Range(700f, 1000f), sway = Random.Range(-80f, 80f), spin = Random.Range(-500f, 500f);
                Tw.Run(rt, Random.Range(2.2f, 3.6f), k =>
                {
                    if (!rt) return;
                    rt.anchoredPosition = start + new Vector2(sway * Mathf.Sin(k * 6), -fall * k);
                    rt.localRotation = Quaternion.Euler(0, 0, spin * k);
                    p.color = p.color.A(1 - k * k);
                }, Tw.Linear, 0, () => { if (rt) Destroy(rt.gameObject); });
                yield return new WaitForSecondsRealtime(0.025f);
            }
        }
    }
}
