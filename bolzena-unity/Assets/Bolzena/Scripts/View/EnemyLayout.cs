using System.Collections.Generic;
using System.Text;
using Bolzena.UI;
using UnityEngine;

namespace Bolzena.View
{
    // 적 자리 · 머리 위 묶음 자리 — 몸(보이는 그림 상자, UnitView.Visible)과 머리 위 묶음 최대 상자(EnemyHud.LocalBox)가
    //   서로 다른 적끼리 겹치지 않게 정한다(2026-10-06 사용자: 「앞뒤 캐릭터랑 겹치지 않게 공간 배치」 · 「체력바를 이미지 위에」).
    //   ① 몸 — 원래 자리(표 · 엇갈림)의 차례(x)와 줄(앞 · 뒤)은 그대로 두고, 이웃 몸 상자와 겹치면 오른쪽으로 민다.
    //      화면 폭을 넘으면 먼저 뒷줄을 더 위로(앞뒤 y 차이를 키워 앞줄 몸과 높이로 갈리게), 그래도 안 되면 몸을 조금씩 줄인다.
    //   ② 묶음 — 그 적 그림 꼭대기 바로 위. 앞줄부터 놓고, 다른 적의 몸 · 이미 놓은 묶음과 겹치면 그 위로 올린다.
    //      화면 위 한계(파티 HP · 메뉴 띠)를 넘으면 옆으로 비켜 본다.
    //   ③ 그리는 차례 — 앞줄(발이 낮은) 묶음이 뒷줄 묶음 위에.
    public static class EnemyLayout
    {
        public const float Gap = 0.1f;           // 서로 다른 적의 상자끼리 띄울 틈
        static readonly float[] DyTry = { 0f, 0.3f, 0.6f, 0.9f };
        // (묶음 배율, 몸 배율) — 앞에서부터 해 본다. 앞뒤 y 차이(DyTry)를 먼저 키우고, 그래도 안 되면 묶음 → 몸 차례로 조금씩 줄인다
        static readonly (float hud, float body)[] SizeTry = { (1f, 1f), (0.9f, 1f), (0.82f, 1f), (0.82f, 0.92f), (0.74f, 0.92f), (0.74f, 0.85f), (0.66f, 0.85f), (0.66f, 0.78f), (0.6f, 0.78f), (0.6f, 0.72f), (0.6f, 0.66f), (0.56f, 0.6f) };
        static readonly float[] SideTry = { 0f, -0.35f, 0.35f, -0.7f, 0.7f };

        public static float XMin => 0.0f;                              // 맨 앞 사도(발 -1.55) 오른쪽 몸 끝 너머
        public static float XMax => Tone.HalfW - 0.9f;                 // 오른쪽 더미 아이콘 앞까지
        public static float TopLimit => Tone.HalfH - 0.85f;            // 위 파티 HP 띠 · 메뉴 아래까지

        public class Item
        {
            public UnitView U;
            public EnemyHud Hud;
            public Vector3 Want;     // 원래 자리(표)
            public bool Back;        // 뒷줄
            public Vector3 Pos;      // 정한 자리
            public Vector3 Bar;      // 정한 막대 자리(유닛 기준)
            public Rect Body => Shift(U.Visible, Pos);
            public Rect HudAt(Vector3 bar) { var b = Hud.LocalBox; var p = Hud.BarPos; return Shift(new Rect(b.x - p.x + bar.x, b.y - p.y + bar.y, b.width, b.height), Pos); }
        }

        static Rect Shift(Rect r, Vector3 p) => new Rect(r.x + p.x, r.y + p.y, r.width, r.height);
        static bool Hit(Rect a, Rect b, float gap) => a.xMin < b.xMax + gap && b.xMin < a.xMax + gap && a.yMin < b.yMax + gap && b.yMin < a.yMax + gap;

        /// <summary>웨이브 — 모든 적의 자리 · 배율 · 묶음 자리를 정해 넣는다(Home · 막대). 돌려줌: 로그 한 줄.</summary>
        public static string Place(List<Item> items)
        {
            if (items.Count == 0) return "";
            float frontY = float.MaxValue;
            foreach (var it in items) frontY = Mathf.Min(frontY, it.Want.y);
            foreach (var it in items) it.Back = it.Want.y > frontY + 0.2f;
            var order = new List<Item>(items);
            order.Sort((a, b) => a.Want.x.CompareTo(b.Want.x));
            float bestScore = float.MaxValue; (float hud, float body) bestSz = SizeTry[0]; float bestDy = 0;
            foreach (var sz in SizeTry)
                foreach (var dy in DyTry)
                {
                    foreach (var it in items) { it.U.Rescale(sz.body); it.Hud.SetHudMul(sz.hud); }
                    bool fit = Bodies(order, dy);
                    Huds(items);
                    float over = 0;
                    foreach (var it in items) over += Mathf.Max(0, it.HudAt(it.Bar).yMax - TopLimit);
                    float ov = Overlap(items, null);
                    if (fit && over < 1e-3f && ov < 1e-4f) goto done;
                    float score = ov * 100 + over * 30 + (fit ? 0 : 50) + (1 - sz.hud) * 4 + (1 - sz.body) * 6 + dy * 0.5f;
                    if (score < bestScore) { bestScore = score; bestSz = sz; bestDy = dy; }
                }
            foreach (var it in items) { it.U.Rescale(bestSz.body); it.Hud.SetHudMul(bestSz.hud); }
            Bodies(order, bestDy);
            Huds(items);
        done:
            foreach (var it in items) { it.U.Home = it.Pos; it.Hud.BarPos = it.Bar; }
            Layer(items);
            return Report(items);
        }

        // 몸 — 차례대로, 겹치는 앞 이웃의 오른끝 너머로. 다 놓고 화면 폭에 맞춰 옮긴다. 들어가면 true
        static bool Bodies(List<Item> order, float dy)
        {
            for (int i = 0; i < order.Count; i++)
            {
                var it = order[i];
                var v = it.U.Visible;
                float y = it.Want.y + (it.Back ? dy : 0);
                float x = i == 0 ? XMin - v.xMin : order[i - 1].Pos.x;   // 왼쪽부터 촘촘히(이웃과 겹치면 민다) — 다 놓고 화면 폭에 고르게 편다
                for (int pass = 0; pass < 3; pass++)
                    for (int j = 0; j < i; j++)
                    {
                        var o = order[j].Body;
                        var me = Shift(v, new Vector3(x, y, 0));
                        if (Hit(me, o, Gap)) x = o.xMax + Gap - v.xMin + 0.001f;
                    }
                it.Pos = new Vector3(x, y, 0);
            }
            // 좌우 끝(몸 · 묶음 폭) — 남는 폭은 사이사이에 고르게(한 칸에 1.4 까지) 나눠 머리 위 묶음이 나란히 설 자리를 만든다
            float Left() { float l = float.MaxValue; foreach (var it in order) { var b = it.Body; var h = it.HudAt(new Vector3(0, 0, 0)); l = Mathf.Min(l, Mathf.Min(b.xMin, h.xMin)); } return l; }
            float Right() { float r = float.MinValue; foreach (var it in order) { var b = it.Body; var h = it.HudAt(new Vector3(0, 0, 0)); r = Mathf.Max(r, Mathf.Max(b.xMax, h.xMax)); } return r; }
            float l0 = Left(), r0 = Right();
            float slack = (XMax - XMin) - (r0 - l0);
            if (order.Count > 1 && slack > 0)
            {
                float step = Mathf.Min(1.4f, slack / (order.Count - 1));
                for (int i = 0; i < order.Count; i++) order[i].Pos += new Vector3(step * i, 0, 0);
            }
            // 무리 가운데를 원래 자리 가운데 쪽으로, 화면 안에서
            float wantC = 0; foreach (var it in order) wantC += it.Want.x; wantC /= order.Count;
            float l1 = Left(), r1 = Right();
            float sh = wantC - (l1 + r1) / 2;
            if (r1 + sh > XMax) sh = XMax - r1;
            if (l1 + sh < XMin) sh = XMin - l1;
            foreach (var it in order) it.Pos += new Vector3(sh, 0, 0);
            return r1 + sh <= XMax + 1e-3f && l1 + sh >= XMin - 1e-3f;
        }

        /// <summary>묶음 — 앞줄부터 그림 꼭대기 위, 겹치면 올리고, 위 한계를 넘으면 옆으로. 다 들어가면 true.</summary>
        public static bool Huds(List<Item> items, System.Func<Item, bool> alive = null)
        {
            var order = new List<Item>(items);
            order.Sort((a, b) => a.Pos.y != b.Pos.y ? a.Pos.y.CompareTo(b.Pos.y) : a.Pos.x.CompareTo(b.Pos.x));
            var placed = new List<Rect>();
            bool ok = true;
            foreach (var it in order)
            {
                if (alive != null && !alive(it)) { it.Bar = it.Hud.BarPos; continue; }
                float k = Tone.K * it.Hud.HudMul;
                float baseY = EnemyHud.BaseBarY(it.U, k);
                Vector3 best = new Vector3(0, baseY, 0); float bestTop = float.MaxValue; bool found = false;
                foreach (var sx in SideTry)
                {
                    var bar = new Vector3(sx, baseY, 0);
                    for (int guard = 0; guard < 30; guard++)
                    {
                        var me = it.HudAt(bar);
                        float up = 0;
                        foreach (var o in items)
                        {
                            if (o == it || (alive != null && !alive(o))) continue;
                            var ob = o.Body;
                            if (Hit(me, ob, Gap)) up = Mathf.Max(up, ob.yMax + Gap - me.yMin + 0.001f);
                        }
                        foreach (var pr in placed) if (Hit(me, pr, Gap)) up = Mathf.Max(up, pr.yMax + Gap - me.yMin + 0.001f);
                        if (up <= 0) break;
                        bar.y += up;
                    }
                    var top = it.HudAt(bar).yMax;
                    var box = it.HudAt(bar);
                    bool inside = box.xMin >= XMin - 0.3f && box.xMax <= XMax + 0.6f;
                    if (top <= TopLimit && inside) { best = bar; found = true; break; }
                    if (top < bestTop) { bestTop = top; best = bar; }
                }
                if (!found) ok = false;
                it.Bar = best;
                placed.Add(it.HudAt(best));
            }
            return ok;
        }

        // 그리는 차례 — 발이 낮은(앞) 묶음이 위에
        public static void Layer(List<Item> items)
        {
            var order = new List<Item>(items);
            order.Sort((a, b) => a.Pos.y.CompareTo(b.Pos.y));
            for (int r = 0; r < order.Count; r++) order[r].Hud.SetLayer(-10 * Mathf.Min(r, 6));
        }

        /// <summary>서로 다른 적끼리 겹친 넓이의 합(몸-몸 · 몸-묶음 · 묶음-묶음). sb 가 있으면 쌍마다 적는다.</summary>
        public static float Overlap(List<Item> items, StringBuilder sb, System.Func<Item, bool> alive = null)
        {
            float sum = 0;
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i]; var b = items[j];
                    if (alive != null && (!alive(a) || !alive(b))) continue;
                    var ab = a.Body; var bb = b.Body; var ah = a.HudAt(a.Bar); var bh = b.HudAt(b.Bar);
                    float bbA = Area(ab, bb), bhA = Area(ab, bh) + Area(ah, bb), hhA = Area(ah, bh);
                    float s = bbA + bhA + hhA;
                    sum += s;
                    if (sb != null && s > 1e-4f) sb.Append($" {i + 1}-{j + 1}(몸 {bbA:F2} · 몸/묶음 {bhA:F2} · 묶음 {hhA:F2})");
                }
            return sum;
        }

        static float Area(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0 && h > 0 ? w * h : 0;
        }

        public static string Report(List<Item> items, System.Func<Item, bool> alive = null)
        {
            var sb = new StringBuilder();
            float ov = Overlap(items, sb, alive);
            float top = float.MinValue;
            foreach (var it in items) top = Mathf.Max(top, it.HudAt(it.Bar).yMax);
            var head = new StringBuilder($"적 {items.Count} · 몸 배율 {(items.Count > 0 ? items[0].U.ScaleMul : 1):F2} · 묶음 배율 {(items.Count > 0 ? items[0].Hud.HudMul : 1):F2} · 화면 {Screen.width}x{Screen.height} · 묶음 위끝 {top:F2}/{TopLimit:F2} · 겹침 {ov:F3}");
            foreach (var it in items) head.Append($" | {it.U.name.Replace("enemy_", "")} 발({it.Pos.x:F2},{it.Pos.y:F2}) 그림 위 {it.Body.yMax:F2} 막대 {it.Pos.y + it.Bar.y:F2}");
            if (sb.Length > 0) head.Append(" ·· 겹친 쌍:").Append(sb);
            return head.ToString();
        }
    }
}
