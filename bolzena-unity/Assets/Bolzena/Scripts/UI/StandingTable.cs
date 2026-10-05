using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Bolzena.UI
{
    // 스탠딩 맞춤 표(runui Resources/RunUI/standing_fit.json — 「스탠딩 측정」 에이전트가 만든 것, 규칙 runui Docs/스탠딩맞춤.md)를 읽는다.
    //   사도(그림 키)마다 scale(인게임 SD 몸 키 비를 따르는 배율) · footY(바닥) · centerX(몸 중심) · headY · bounds(불투명 경계) — 스파인 원래 단위.
    //   쓰는 법: 원래 단위 1 당 화면 배율 sr = (중앙값 몸 키를 그릴 화면 길이 ÷ med_st) × scale, 원점 x = 칸 가운데 − centerX·sr, y = 바닥선 − footY·sr.
    public static class StandingTable
    {
        public struct Fit { public float Scale, FootY, CenterX, HeadY, BodyH, LegY, TopY; public Rect Bounds; }
        static Dictionary<string, Fit> table;
        public static float MedSt = 630f;

        static float Num(string block, string key, float def)
        {
            int i = block.IndexOf("\"" + key + "\"", System.StringComparison.Ordinal);
            if (i < 0) return def;
            i = block.IndexOf(':', i) + 1;
            while (i < block.Length && char.IsWhiteSpace(block[i])) i++;
            int j = i; while (j < block.Length && "0123456789.-eE".IndexOf(block[j]) >= 0) j++;
            return float.TryParse(block.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
        }

        static Dictionary<string, Fit> Load()
        {
            if (table != null) return table;
            table = new Dictionary<string, Fit>();
            var ta = Resources.Load<TextAsset>("RunUI/standing_fit");
            if (ta == null) { Debug.LogWarning("[StandingTable] standing_fit.json 없음 — 다리 기준으로 대신"); return table; }
            var t = ta.text;
            int mi = t.IndexOf("\"_meta\"", System.StringComparison.Ordinal);
            if (mi >= 0) { int me = t.IndexOf('}', mi); MedSt = Num(t.Substring(mi, me - mi), "med_st", 630f); }
            // 맨 위 칸 "<그림 키>": { … "sd": { … } } — 사도 블록마다 sd 앞까지만 읽는다(sd 안에도 footY · centerX 가 있다)
            int depth = 0;
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '{') depth++;
                else if (c == '}') depth--;
                else if (c == '"' && depth == 1)
                {
                    int j = t.IndexOf('"', i + 1);
                    var key = t.Substring(i + 1, j - i - 1);
                    int open = t.IndexOf('{', j);
                    int colon = t.IndexOf(':', j);
                    if (open < 0 || key.StartsWith("_") || t.Substring(colon + 1, open - colon - 1).Trim().Length > 0) { i = j; continue; }
                    // 블록 끝 찾기
                    int d = 0, k = open;
                    for (; k < t.Length; k++) { if (t[k] == '{') d++; else if (t[k] == '}') { d--; if (d == 0) break; } }
                    var block = t.Substring(open, k - open + 1);
                    int sd = block.IndexOf("\"sd\"", System.StringComparison.Ordinal);
                    var top = sd > 0 ? block.Substring(0, sd) : block;
                    var f = new Fit { Scale = Num(top, "scale", 1), FootY = Num(top, "footY", 0), CenterX = Num(top, "centerX", 0), HeadY = Num(top, "headY", 0), BodyH = Num(top, "bodyH_st", 0), LegY = Num(top, "legY", 0), TopY = Num(top, "topY", 0) };
                    int bi = top.IndexOf("\"bounds\"", System.StringComparison.Ordinal);
                    if (bi >= 0)
                    {
                        int a = top.IndexOf('[', bi), b = top.IndexOf(']', a);
                        var p = top.Substring(a + 1, b - a - 1).Split(',');
                        if (p.Length == 4)
                        {
                            float x0 = float.Parse(p[0], CultureInfo.InvariantCulture), y0 = float.Parse(p[1], CultureInfo.InvariantCulture);
                            float x1 = float.Parse(p[2], CultureInfo.InvariantCulture), y1 = float.Parse(p[3], CultureInfo.InvariantCulture);
                            f.Bounds = new Rect(x0, y0, x1 - x0, y1 - y0);
                        }
                    }
                    table[key] = f;
                    i = k;
                    depth = 1;
                }
            }
            Debug.Log($"[StandingTable] {table.Count}명 · med_st {MedSt}");
            return table;
        }

        public static bool TryGet(string art, out Fit f) { f = default; return art != null && Load().TryGetValue(art, out f); }
    }
}
