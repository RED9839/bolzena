using System;
using System.Collections.Generic;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 툴팁 — 올려 두면(터치는 길게 누르면) 뜨는 작은 풀이 상자. 화면 UI 층(흔들리지 않는다), 화면 밖으로 안 나간다.
    // 띄울 자리는 TipZone(월드 사각형 + 글 만드는 손)으로 등록한다 — 카드 · 상태 칩 · 적의 수 · 강인도 · AP · 고학년 …
    public class Tooltip : MonoBehaviour
    {
        public static Tooltip I;
        public static readonly List<TipZone> Zones = new List<TipZone>();
        public static bool Suppress;                    // 창이 열려 있거나 끄는 중
        public static Action<string> OnShown;           // 자동 데모 캡처
        public const float DefaultCeiling = 4.4f;
        public static float Ceiling = DefaultCeiling;   // 툴팁 위 끝이 넘지 않을 높이(더미 보기는 탭 줄 아래로)
        SpriteRenderer panel, edge;
        TextMeshPro body;
        TipZone hot, shown;
        float hotT;
        Vector3 pinnedAt;
        string pinnedText;
        const int O = 2000;
        const float W = 3.9f;

        public static Tooltip Create(Transform parent)
        {
            var t = Make.Node("Tooltip", parent);
            var tip = t.gameObject.AddComponent<Tooltip>();
            // 판 — 톤.md 의 큰 판(남색 + 얇은 금 테), 글은 본문 글꼴(Noto) 16
            tip.panel = Make.Sliced("panel", t, Res.UI("panel_9s"), Vector3.zero, new Vector2(W, 1), O);
            tip.edge = Make.Sliced("edge", t, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(0.05f, 1), O + 1, new Color(1, 1, 1, 0));
            tip.body = Tone.Text("body", t, "", Vector3.zero, Tone.Sm, O + 2, Tone.Ink, TextAlignmentOptions.TopLeft, true, W - 0.4f);
            tip.body.textWrappingMode = TextWrappingModes.Normal;
            tip.body.lineSpacing = -2;
            tip.Hide();
            I = tip;
            return tip;
        }

        public void Hide()
        {
            panel.enabled = edge.enabled = body.enabled = false;
            shown = null;
            pinnedText = null;
        }

        // 손으로 띄운다(올린 카드 · 든 카드) — Unpin 까지. 올려 둔 자리(TipZone)가 있으면 그것이 먼저다
        public void Pin(Vector3 anchor, string text, bool preferLeft = false)
        {
            bool changed = pinnedText != text || (pinnedAt - anchor).sqrMagnitude > 0.0004f;
            pinnedAt = anchor;
            pinnedText = text;
            pinLeft = preferLeft;
            if (changed && shown == null && !Suppress) { Layout(anchor, text, preferLeft); OnShown?.Invoke("card"); }
        }
        bool pinLeft;

        public void Unpin()
        {
            if (pinnedText == null) return;
            pinnedText = null;
            if (shown == null) panel.enabled = edge.enabled = body.enabled = false;
        }

        // 판의 왼쪽 위를 자리로 — 폰에서는 판째 K 배(글이 1.25 배)
        void Layout(Vector3 anchor, string text, bool preferLeft)
        {
            float k = Tone.K;
            transform.localScale = new Vector3(k, k, 1);
            body.text = text;
            body.rectTransform.sizeDelta = new Vector2(W - 0.4f, 10);
            body.ForceMeshUpdate();
            float h = Mathf.Max(0.5f, body.preferredHeight + 0.3f);
            float wk = W * k, hk = h * k;
            float x = preferLeft ? anchor.x - wk - 0.15f : anchor.x + 0.15f;
            float y = anchor.y + hk * 0.5f;
            // 화면 안으로(가로는 화면 폭 · 세로 ±4.4, 위 끝은 Ceiling)
            float hw = Tone.HalfW - 0.1f;
            if (x + wk > hw) x = anchor.x - wk - 0.15f;
            if (x < -hw) x = -hw;
            float hh = Tone.HalfH - 0.1f;
            float top = Ceiling >= DefaultCeiling ? hh : Ceiling;
            y = Mathf.Clamp(y, -hh + hk, Mathf.Max(-hh + hk, top));
            transform.position = new Vector3(x, y, 0);
            panel.size = new Vector2(W, h);
            panel.transform.localPosition = new Vector3(W / 2, -h / 2, 0);
            edge.size = new Vector2(0.05f, h - 0.16f);
            edge.transform.localPosition = new Vector3(0.1f, -h / 2, 0);
            body.rectTransform.sizeDelta = new Vector2(W - 0.4f, h);
            body.transform.localPosition = new Vector3(0.2f, -0.15f, 0);
            panel.enabled = edge.enabled = body.enabled = true;
        }

        void Update()
        {
            var p = PointerInput.Pos;
            TipZone best = null;
            foreach (var z in Zones)
            {
                if (z == null || !z.isActiveAndEnabled || !z.Hit(p)) continue;
                if (Suppress && z.Priority < 100) continue;      // 창이 열려 있으면 창 안의 것(100 이상)만
                if (best == null || z.Priority > best.Priority) best = z;
            }
            if (best != hot) { hot = best; hotT = 0; }
            hotT += Time.unscaledDeltaTime;
            bool want = hot != null && (PointerInput.Touch ? PointerInput.LongFired && PointerInput.Held : hotT > hot.Delay);
            if (want)
            {
                var txt = hot.Text?.Invoke();
                if (string.IsNullOrEmpty(txt)) { if (shown != null) Hide(); return; }
                if (shown != hot || body.text != txt)
                {
                    Layout(hot.Anchor(), txt, hot.PreferLeft);
                    if (shown != hot) OnShown?.Invoke(hot.name);
                    shown = hot;
                }
            }
            else
            {
                if (shown != null) { shown = null; panel.enabled = edge.enabled = body.enabled = false; }
                if (pinnedText != null && !Suppress) { if (!panel.enabled || body.text != pinnedText) Layout(pinnedAt, pinnedText, pinLeft); }
                else if (panel.enabled) panel.enabled = edge.enabled = body.enabled = false;
            }
        }
    }

    // 툴팁이 뜨는 자리 — 월드 사각형(가운데 · 크기)을 이 물체의 자리에 둔다
    public class TipZone : MonoBehaviour
    {
        public Vector2 Size = new Vector2(1, 1);
        public Vector2 Offset;
        public Func<string> Text;
        public int Priority;
        public float Delay = 0.3f;
        public bool PreferLeft;

        public static TipZone Add(Component on, Vector2 size, Func<string> text, int priority = 0, Vector2 offset = default)
        {
            var z = on.gameObject.AddComponent<TipZone>();
            z.Size = size; z.Text = text; z.Priority = priority; z.Offset = offset;
            return z;
        }

        void OnEnable() { if (!Tooltip.Zones.Contains(this)) Tooltip.Zones.Add(this); }
        void OnDisable() { Tooltip.Zones.Remove(this); }

        Vector2 C => (Vector2)transform.position + Offset * (Vector2)transform.lossyScale;
        Vector2 S => Size * new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        public bool Hit(Vector2 p)
        {
            var c = C; var s = S;
            return Mathf.Abs(p.x - c.x) < s.x / 2 && Mathf.Abs(p.y - c.y) < s.y / 2;
        }

        public Vector3 Anchor() { var c = C; var s = S; return PreferLeft ? new Vector3(c.x - s.x / 2, c.y + s.y / 2, 0) : new Vector3(c.x + s.x / 2, c.y + s.y / 2, 0); }
    }

    // 툴팁 글 꾸미기
    public static class Tip
    {
        public static string Head(string s, string color = "#f3d48c") => $"<color={color}><size=118%>{s}</size></color>";
        public static string Dim(string s) => $"<color=#9aa6c8>{s}</color>";
        public static string Terms(IEnumerable<Battle.Term> terms)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in terms)
            {
                if (sb.Length > 0) sb.Append("\n\n");
                string col = t.Kind == "status" ? "#ffb09a" : t.Kind == "tag" ? "#9fd3ff" : t.Kind == "flash" ? "#ffe07a" : t.Kind == "no" ? "#ff8080" : "#ffd98a";
                // 키워드 칩 — 낱말을 옅은 바탕에 얹어 본문과 가른다
                sb.Append("<mark=").Append(col).Append("2e><color=").Append(col).Append("> ").Append(t.Word).Append(" </color></mark>\n").Append(t.Text);
            }
            return sb.ToString();
        }
    }
}
