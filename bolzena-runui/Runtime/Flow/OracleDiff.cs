using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bolzena.RunUI
{
    /// <summary>신탁 갈래 글에서 기본 카드 글과 달라진 부분(오른 수치 · 새 구절)을 표시한다 — 표시 문자 On … Off. Tmp 로 바꾸면 연두(카제나식).</summary>
    public static class OracleDiff
    {
        public const char On = '', Off = '';
        public const string Green = "#9BE564";
        static readonly Regex strip = new Regex("<color=[^>]*>|</color>");

        // 신탁으로 바뀐 부분 — 기본 카드 글과 갈래 글을 낱말 단위로 맞춰, 새로 붙은 구절 · 오른 수치를 표시(\u0001 … \u0002, Tone.CardText 가 연두로).
        //   내려간 수치 · 그대로인 것은 표시하지 않는다.
        static readonly System.Text.RegularExpressions.Regex tokRx = new System.Text.RegularExpressions.Regex(@"「[^」]*」|[+\-]?\d+(?:\.\d+)?%?|[^\s,·()「」]+|[\s,·()]+");
        static bool IsSep(string t) => t.Length > 0 && (char.IsWhiteSpace(t[0]) || t[0] == ',' || t[0] == '·' || t[0] == '(' || t[0] == ')');
        static bool NumTok(string t, out float v) => float.TryParse(t.TrimStart('+').TrimEnd('%'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v);
        public static string Mark(string baseRaw, string now)
        {
            if (string.IsNullOrEmpty(now) || string.IsNullOrEmpty(baseRaw) || baseRaw == now) return now;
            var bt = tokRx.Matches(baseRaw).Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToList();
            var nt = tokRx.Matches(now).Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToList();
            var bw = bt.Where(t => !IsSep(t)).ToList(); var nw = nt.Where(t => !IsSep(t)).ToList();
            int n = bw.Count, m2 = nw.Count;
            var L = new int[n + 1, m2 + 1];
            for (int i = n - 1; i >= 0; i--) for (int j = m2 - 1; j >= 0; j--) L[i, j] = bw[i] == nw[j] ? L[i + 1, j + 1] + 1 : Math.Max(L[i + 1, j], L[i, j + 1]);
            var mark = new bool[m2]; var baseUsed = new bool[n];
            { int i = 0, j = 0; while (i < n && j < m2) { if (bw[i] == nw[j]) { baseUsed[i] = true; i++; j++; } else if (L[i + 1, j] >= L[i, j + 1]) i++; else { mark[j] = true; j++; } } while (j < m2) mark[j++] = true; }
            // 수치 짝 — 바뀐 수치는 같은 차례의 바뀐 기본 수치와 견줘 오른 것만 남긴다
            var baseNums = new List<float>(); for (int i = 0; i < n; i++) if (!baseUsed[i] && NumTok(bw[i], out var bv)) baseNums.Add(bv);
            int bi = 0;
            for (int j = 0; j < m2; j++)
                if (mark[j] && NumTok(nw[j], out var nv))
                {
                    if (bi < baseNums.Count) { if (nv <= baseNums[bi]) mark[j] = false; bi++; }
                }
            var sb = new System.Text.StringBuilder(); int wi = 0; bool open = false;
            for (int k = 0; k < nt.Count; k++)
            {
                if (IsSep(nt[k])) { sb.Append(nt[k]); continue; }
                bool mk = mark[wi++];
                // 다음 낱말도 표시면 사이 구분은 같은 구절로 이어 둔다
                if (mk && !open) { sb.Append(On); open = true; }
                if (!mk && open) { // 직전 구분 뒤로 닫는다
                    int cut = sb.Length; while (cut > 0 && IsSep(sb[cut - 1].ToString())) cut--; sb.Insert(cut, Off); open = false; }
                sb.Append(nt[k]);
            }
            if (open) sb.Append(Off);
            return sb.ToString();
        }


        /// <summary>표시 문자를 TMP 연두 태그로 — 표시 안의 다른 색(수치 · 낱말)은 연두가 이기게 걷는다.</summary>
        public static string ToTmp(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf(On) < 0) return Strip(s);
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                int a = s.IndexOf(On, i);
                if (a < 0) { sb.Append(s, i, s.Length - i); break; }
                sb.Append(s, i, a - i);
                int b = s.IndexOf(Off, a + 1); if (b < 0) b = s.Length;
                sb.Append("<color=" + Green + ">").Append(strip.Replace(s.Substring(a + 1, b - a - 1), "")).Append("</color>");
                i = Math.Min(s.Length, b + 1);
            }
            return sb.ToString();
        }
        public static string Strip(string s) => string.IsNullOrEmpty(s) ? s : s.Replace(On.ToString(), "").Replace(Off.ToString(), "");
    }
}
