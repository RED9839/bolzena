using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 카드 확대(배치는 카제나 카드 확대를 따른다 · 그림 · 글은 우리 것) — 카드를 크게(화면 높이의 약 60%) 띄우고,
    // 오른쪽에 카드 글에 나온 키워드마다 설명 판을 세로로 쌓는다(이름 / 풀이, 수치 금빛, 해로운 상태는 위 테두리 붉게).
    // 오른쪽에 자리가 없으면 왼쪽에. 손패에서 올리거나 고를 때 · 더미 보기에서 누를 때 쓴다. 한 번에 하나.
    public static class CardZoom
    {
        static Transform root;
        static CardView card;
        static string shownId;
        public static bool Shown => root != null;
        public const int O = 1500;
        const float PanelW = 3.5f;

        /// <summary>at — 카드 가운데(월드). scale 0 이면 화면 높이에 맞춘다. 같은 카드면 자리만 옮긴다.</summary>
        public static void Show(Transform parent, CardInfo info, Vector3 at, float scale = 0, bool panelsLeft = false, string note = null)
        {
            float k = Tone.K;
            float s = scale > 0 ? scale : Mathf.Min(2.1f, (Tone.HalfH * 2 * 0.6f) / CardView.H);
            string key = info.Id + "|" + info.Text + "|" + note;
            if (root != null && shownId == key) { Place(at, s, panelsLeft); return; }
            Hide();
            shownId = key;
            root = Make.Node("CardZoom", parent);
            card = CardView.Create(root, info);
            card.ShowDesc = true; card.ShowPin = true; card.Playable = true;
            card.TargetScale = s; card.TargetRot = 0; card.TargetPos = Vector3.zero;
            card.Snap();
            card.SetOrder(O);
            Make.Box("shadow", root, Res.UI("soft"), new Vector3(0, -0.2f, 0), new Vector2(CardView.W * s * 1.5f, CardView.H * s * 1.2f), O - 5, new Color(0, 0, 0, 0.55f));

            // 키워드 판들
            var panels = Make.Node("panels", root);
            float y = 0;
            var terms = new List<Term>(info.Terms ?? new List<Term>());
            if (note != null) terms.Insert(0, new Term("지금은 못 냅니다", note, "no"));
            foreach (var t in terms)
            {
                bool bad = t.Kind == "no" || (t.Kind == "status" && Bolzena.Core.R.IsBadSt(t.Word));
                var head = Tone.Text("h", panels, t.Word, new Vector3(0.2f, y - 0.14f, 0), Tone.Md * k, O + 3, t.Kind == "flash" ? Tone.Gold : Tone.Ink, TextAlignmentOptions.TopLeft, false, PanelW * k);
                var body = Tone.Text("b", panels, Hilite(t.Text), new Vector3(0.2f, y - 0.14f - 0.34f * k, 0), Tone.Sm * k, O + 3, Tone.Sub, TextAlignmentOptions.TopLeft, true, (PanelW - 0.4f) * k);
                body.textWrappingMode = TextWrappingModes.Normal;
                body.lineSpacing = -2;
                body.rectTransform.sizeDelta = new Vector2((PanelW - 0.4f) * k, 10);
                body.ForceMeshUpdate();
                float h = 0.14f + 0.34f * k + body.preferredHeight + 0.18f;
                var bg = Make.Sliced("bg", panels, Res.UI("bar_fill_9s"), new Vector3(PanelW * k / 2, y - h / 2, 0), new Vector2(PanelW * k, h), O + 1, new Color(0.03f, 0.04f, 0.08f, 1f));
                Make.Box("top", panels, Res.UI("white"), new Vector3(PanelW * k / 2, y - 0.012f, 0), new Vector2(PanelW * k - 0.1f, 0.024f), O + 2,
                    bad ? new Color(1f, 0.38f, 0.42f) : t.Kind == "flash" ? Tone.Gold : new Color(1, 1, 1, 0.25f));
                y -= h + 0.08f;
            }
            panelsH = -y;
            Place(at, s, panelsLeft);
        }

        static float panelsH;

        static void Place(Vector3 at, float s, bool left)
        {
            if (root == null) return;
            float k = Tone.K, hw = Tone.HalfW, hh = Tone.HalfH;
            float cw = CardView.W * s, ch = CardView.H * s;
            at.x = Mathf.Clamp(at.x, -hw + cw / 2 + 0.15f, hw - cw / 2 - 0.15f);
            at.y = Mathf.Clamp(at.y, -hh + ch / 2 + 0.1f, hh - ch / 2 - 0.1f);
            root.localPosition = at;
            var panels = root.Find("panels");
            float pw = PanelW * k;
            bool rightFits = at.x + cw / 2 + 0.15f + pw <= hw - 0.1f;
            bool useLeft = left || !rightFits;
            float px = useLeft ? -cw / 2 - 0.15f - pw : cw / 2 + 0.15f;
            float py = Mathf.Min(ch / 2 - 0.1f, hh - 0.1f - at.y);
            if (at.y + py - panelsH < -hh + 0.1f) py = -hh + 0.1f - at.y + panelsH;
            panels.localPosition = new Vector3(px, py, 0);
        }

        public static void Hide()
        {
            if (root != null) Object.Destroy(root.gameObject);
            root = null; card = null; shownId = null;
        }

        // 풀이 글 — 수치 금빛
        static readonly System.Text.RegularExpressions.Regex num = new System.Text.RegularExpressions.Regex(@"(?<![#\w])([+\-]?\d+(?:\.\d+)?%?)");
        static string Hilite(string rich)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < rich.Length)
            {
                int lt = rich.IndexOf('<', i);
                string plain = lt < 0 ? rich.Substring(i) : rich.Substring(i, lt - i);
                sb.Append(num.Replace(plain, "<color=" + Tone.GoldTag + ">$1</color>"));
                if (lt < 0) break;
                int gt = rich.IndexOf('>', lt);
                if (gt < 0) { sb.Append(rich.Substring(lt)); break; }
                sb.Append(rich, lt, gt - lt + 1);
                i = gt + 1;
            }
            return sb.ToString();
        }
    }
}
