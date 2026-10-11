using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 전투 기록(데미지 지표, 2026-10-11 사용자 「전투 중 + 끝난 뒤 둘 다」) — 사도별 피해 · 실드 · 회복 수치와 막대.
    //   전투 중: 위 오른쪽 「전투 기록」 단추(막대 셋 · 단축키 D)로 여닫는 작은 표 — 위 가운데에 매달린다. 창(Modal)이 아니라 싸움을 막지 않고,
    //            행동마다 새 값(BattleDirector.RefreshHud → Set). 파티 HP 막대 오른쪽 끝을 넘지 않게 가운데에서 오른쪽으로 비킨다.
    //   이긴 뒤: 끝 자세의 왼쪽 아래에 같은 표를 조금 크게 — 가장 많이 친 사도 줄을 금빛 띠 + 별로(판 화면의 보상 줄 · 떠나기와 겹치지 않는 자리).
    //   탭: 「이번 싸움」 · 「판 전체」(판에서 연 싸움만 — 전투 시범에는 판 탭이 없다). 막대는 열마다 가장 큰 값 기준, 0 은 「─」.
    //   셈의 기준은 core BattleTally.cs — 피해는 실제로 깎인 HP(넘친 피해 빼고) + 벗긴 적 실드, 실드는 얹은 방어 · 실드, 회복은 실제로 채운 HP.
    public class MeterPanel : MonoBehaviour
    {
        public bool Summary { get; private set; }
        BattleSnapshot s;
        int tab;
        Transform body;
        float w, h;   // 판 크기(배율 1 기준 — 실제는 × Scale)
        const int O = 640;   // HUD(560~) 위 · 손패 AP(692) 아래 · 창(850) 아래
        // 열 자리(판 왼쪽 끝에서)
        const float PadX = 0.16f, FaceX = 0.34f, NameX = 0.58f, BarX0 = 1.5f, BarX1 = 2.62f, DealtR = 3.42f, ShieldR = 4.12f, HealR = 4.82f, Wd = 4.98f;
        const float ColW = 0.56f;   // 실드 · 회복 밑줄 막대 최대 폭

        public static MeterPanel Create(Transform parent, bool summary)
        {
            var go = Make.Node(summary ? "MeterSummary" : "Meter", parent);
            var m = go.gameObject.AddComponent<MeterPanel>();
            m.Summary = summary;
            return m;
        }

        float Scale => Tone.K * (Summary ? 1.18f : 1f);

        public void Set(BattleSnapshot snap)
        {
            s = snap;
            if (s == null) return;
            if (s.MeterRun == null) tab = 0;
            Draw();
        }

        /// <summary>점검 · 단추 — 탭 바꾸기(0 이번 싸움 · 1 판 전체).</summary>
        public void SetTab(int t) { if (s == null || (t == 1 && s.MeterRun == null) || t == tab) return; tab = t; Draw(); }
        public int Tab => tab;

        /// <summary>이 판 위인가(전투 화면이 판 위의 누름을 싸움터로 넘기지 않게).</summary>
        public bool Over(Vector2 p)
        {
            if (!gameObject.activeInHierarchy || body == null) return false;
            float k = Scale; var o = (Vector2)transform.position;
            return p.x >= o.x && p.x <= o.x + w * k && p.y <= o.y && p.y >= o.y - h * k;
        }

        TextMeshPro T(string name, string text, Vector3 at, float size, Color c, TextAlignmentOptions al = TextAlignmentOptions.Left, bool bodyFont = false, float width = 3f)
        {
            var t = Tone.Text(name, body, text, at, size, O + 4, c, al, bodyFont, width);
            return t;
        }

        void Draw()
        {
            if (body != null) Destroy(body.gameObject);
            body = Make.Node("body", transform);
            var rows = tab == 1 && s.MeterRun != null ? s.MeterRun : s.Meter;
            int n = rows.Count;
            float headH = 0.42f, colH = 0.26f, rowH = Summary ? 0.42f : 0.38f, footH = Summary ? 0.3f : 0f;
            w = Wd; h = headH + colH + n * rowH + footH + 0.12f;
            // 판 — 남색 반투명 + 얇은 금 테(끝 자세 판은 조금 더 짙게)
            Make.Sliced("bg", body, Res.UI("bar_fill_9s"), new Vector3(w / 2, -h / 2, 0), new Vector2(w, h), O, new Color(0.03f, 0.04f, 0.1f, Summary ? 0.9f : 0.84f));
            Make.Box("edge", body, Res.UI("white"), new Vector3(w / 2, -0.012f, 0), new Vector2(w - 0.16f, 0.024f), O + 1, Tone.A(Tone.Edge, 0.75f));
            // 머리 — 제목 + 탭
            float hy = -headH / 2 - 0.02f;
            T("title", "전투 기록", new Vector3(PadX, hy, 0), Tone.Md, Tone.Gold);
            if (s.MeterRun != null)
            {
                Pill("tab1", "판 전체", new Vector3(w - PadX - 0.5f, hy, 0), 1.0f, tab == 1, () => SetTab(1));
                Pill("tab0", "이번 싸움", new Vector3(w - PadX - 1.6f, hy, 0), 1.1f, tab == 0, () => SetTab(0));
            }
            else T("sub", "이번 싸움", new Vector3(w - PadX, hy, 0), Tone.Cap, Tone.Sub, TextAlignmentOptions.Right, true);
            // 열 머리
            float y = -headH;
            Make.Box("hl", body, Res.UI("white"), new Vector3(w / 2, y, 0), new Vector2(w - 0.2f, 0.012f), O + 1, Tone.Line);
            float cy = y - colH / 2;
            T("c0", "사도", new Vector3(PadX, cy, 0), Tone.Cap, Tone.Sub, TextAlignmentOptions.Left, true);
            T("c1", "피해", new Vector3(DealtR, cy, 0), Tone.Cap, Tone.Bad, TextAlignmentOptions.Right, true);
            T("c2", "실드", new Vector3(ShieldR, cy, 0), Tone.Cap, Tone.Sky, TextAlignmentOptions.Right, true);
            T("c3", "회복", new Vector3(HealR, cy, 0), Tone.Cap, Tone.Good, TextAlignmentOptions.Right, true);
            y -= colH;
            // 열마다 가장 큰 값 · 가장 많이 친 사도
            int maxD = 0, maxS = 0, maxH = 0, top = -1, sumD = 0, sumS = 0, sumH = 0;
            for (int i = 0; i < n; i++)
            {
                var r = rows[i];
                if (r.Dealt > maxD) { maxD = r.Dealt; top = i; }
                maxS = Mathf.Max(maxS, r.Shield); maxH = Mathf.Max(maxH, r.Heal);
                sumD += r.Dealt; sumS += r.Shield; sumH += r.Heal;
            }
            for (int i = 0; i < n; i++)
            {
                var r = rows[i];
                float ry = y - rowH / 2;
                bool best = Summary && i == top;
                if (best) Make.Sliced("best", body, Res.UI("bar_fill_9s"), new Vector3(w / 2, ry, 0), new Vector2(w - 0.12f, rowH - 0.04f), O + 1, new Color(0.95f, 0.75f, 0.3f, 0.12f));
                else if (i % 2 == 1) Make.Sliced("band", body, Res.UI("white"), new Vector3(w / 2, ry, 0), new Vector2(w - 0.12f, rowH - 0.04f), O + 1, new Color(1, 1, 1, 0.04f));
                float fd = rowH - 0.1f;
                Make.Box("fr", body, Res.UI("circle"), new Vector3(FaceX, ry, 0), new Vector2(fd + 0.04f, fd + 0.04f), O + 2, best ? Tone.Gold : new Color(1, 1, 1, 0.45f));
                Tone.RoundFace("face", body, r.Key, new Vector3(FaceX, ry, 0), fd, O + 4);
                var nm = T("name", r.Name, new Vector3(NameX, ry, 0), Tone.Sm, best ? Tone.Gold : Tone.Ink, TextAlignmentOptions.Left, false, BarX0 - NameX - 0.06f);
                nm.overflowMode = TextOverflowModes.Ellipsis;
                if (best) Make.Box("star", body, Res.UI("ic_star"), new Vector3(FaceX + fd / 2, ry + fd / 2 - 0.02f, 0), new Vector2(0.18f, 0.18f), O + 6, Tone.Gold);
                // 피해 — 굵은 막대(가장 큰 값 기준) + 수치
                float bw = BarX1 - BarX0;
                Make.Sliced("dbg", body, Res.UI("bar_bg_9s"), new Vector3(BarX0 + bw / 2, ry, 0), new Vector2(bw, 0.13f), O + 2, new Color(1, 1, 1, 0.5f));
                if (r.Dealt > 0)
                {
                    float f = Mathf.Max(0.06f, bw * r.Dealt / Mathf.Max(1f, maxD));
                    Make.Sliced("dbar", body, Res.UI("bar_fill_9s"), new Vector3(BarX0 + f / 2, ry, 0), new Vector2(f, 0.11f), O + 3, best ? Tone.Gold : new Color(1f, 0.48f, 0.42f));
                }
                Num("dv", r.Dealt, new Vector3(DealtR, ry + 0.005f, 0), Tone.Bad);
                // 실드 · 회복 — 수치 + 밑줄 막대
                Num("sv", r.Shield, new Vector3(ShieldR, ry + 0.035f, 0), Tone.Sky);
                Under("sb", r.Shield, maxS, ShieldR, ry - 0.12f, Tone.Sky);
                Num("hv", r.Heal, new Vector3(HealR, ry + 0.035f, 0), Tone.Good);
                Under("hb", r.Heal, maxH, HealR, ry - 0.12f, Tone.Good);
                y -= rowH;
            }
            // 끝 자세 — 합계 한 줄
            if (Summary)
            {
                Make.Box("fl", body, Res.UI("white"), new Vector3(w / 2, y - 0.02f, 0), new Vector2(w - 0.2f, 0.012f), O + 1, Tone.Line);
                float fy = y - footH / 2 - 0.02f;
                T("ft", "합계", new Vector3(PadX, fy, 0), Tone.Cap, Tone.Sub, TextAlignmentOptions.Left, true);
                T("fd", sumD.ToString("N0"), new Vector3(DealtR, fy, 0), Tone.Sm, Tone.Ink, TextAlignmentOptions.Right);
                T("fs", sumS > 0 ? sumS.ToString("N0") : "─", new Vector3(ShieldR, fy, 0), Tone.Sm, sumS > 0 ? Tone.Ink : Tone.Dim, TextAlignmentOptions.Right);
                T("fh", sumH > 0 ? sumH.ToString("N0") : "─", new Vector3(HealR, fy, 0), Tone.Sm, sumH > 0 ? Tone.Ink : Tone.Dim, TextAlignmentOptions.Right);
            }
            Place();
        }

        void Num(string name, int v, Vector3 at, Color c)
        {
            var t = T(name, v > 0 ? v.ToString("N0") : "─", at, Tone.Sm, v > 0 ? Color.Lerp(Color.white, c, 0.35f) : Tone.Dim, TextAlignmentOptions.Right);
            if (v > 0) Make.Outline(t, 0.2f, Tone.Outline);
        }

        void Under(string name, int v, int max, float right, float y, Color c)
        {
            if (v <= 0 || max <= 0) return;
            float f = Mathf.Max(0.05f, ColW * v / max);
            Make.Box(name, body, Res.UI("white"), new Vector3(right - f / 2, y, 0), new Vector2(f, 0.035f), O + 3, Tone.A(c, 0.85f));
        }

        // 탭 알약 — 고른 쪽은 금빛 바탕 · 남색 글
        void Pill(string name, string text, Vector3 at, float pw, bool on, System.Action click)
        {
            var bg = Make.Sliced(name, body, Res.UI("bar_fill_9s"), at, new Vector2(pw, 0.28f), O + 2, on ? new Color(0.95f, 0.8f, 0.45f, 0.95f) : new Color(1, 1, 1, 0.1f));
            T(name + "t", text, at + new Vector3(0, -0.005f, 0), Tone.Cap, on ? Tone.Night : Tone.Sub, TextAlignmentOptions.Center, true, pw);
            var b = bg.gameObject.AddComponent<Button>();
            b.Size = new Vector2(pw + 0.06f, 0.36f);
            b.OnClick = click;
        }

        // 자리 — 전투 중: 위 가운데(파티 HP 막대 오른쪽 끝보다 오른쪽) · 끝 자세: 왼쪽 아래
        void Place()
        {
            float k = Scale, hw = Tone.HalfW, hh = Tone.HalfH;
            transform.localScale = new Vector3(k, k, 1);
            if (Summary) { transform.localPosition = new Vector3(-hw + 0.3f * Tone.K, -hh + 0.3f * Tone.K + h * k, 0); return; }
            float left = -hw + PartyHud.HudRight * Tone.K + 0.2f;   // 파티 HP 막대 · 실드 칸 오른쪽 끝(16:9 기준 좌표를 모서리 무리 배율로)
            float x = Mathf.Max(-w * k / 2, left);
            transform.localPosition = new Vector3(x, hh - 0.14f * Tone.K, 0);
        }

        float placedW, placedH, placedK;
        void LateUpdate()
        {
            if (body == null) return;
            if (Tone.HalfW != placedW || Tone.HalfH != placedH || Tone.K != placedK) { placedW = Tone.HalfW; placedH = Tone.HalfH; placedK = Tone.K; Place(); }
        }
    }
}
