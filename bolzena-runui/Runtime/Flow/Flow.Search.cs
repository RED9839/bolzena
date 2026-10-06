using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 한글 이름 찾기 — 이름 일부 · 초성(「ㅌㄱ」 → 티그) · 초성과 글자 섞기(「티ㄱ」) · 괄호 이름(「디아나(왕년)」 은 「왕년」 · 「ㄷㅇㄴ」).
    /// 띄어쓰기 · 괄호 · 밑줄은 무시한다(「디아나왕년」 도 잡힌다). 영문은 대소문자 무시.
    /// </summary>
    public static class HangulSearch
    {
        const string Cho = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

        static bool IsJamo(char c) => c >= 'ㄱ' && c <= 'ㅎ';
        static bool IsSyllable(char c) => c >= '가' && c <= '힣';
        public static char Initial(char c) => IsSyllable(c) ? Cho[(c - '가') / 588] : c;

        static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsWhiteSpace(ch) || ch == '(' || ch == ')' || ch == '_' || ch == '·' || ch == '-' || ch == '.') continue;
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        static bool Eq(char q, char n)
        {
            if (q == n) return true;
            if (IsJamo(q) && IsSyllable(n)) return Initial(n) == q;
            return false;
        }

        /// <summary>name 의 어느 자리에서든 query 가 이어서 맞으면 true. 빈 질의는 모두 맞다.</summary>
        public static bool Match(string name, string query)
        {
            var q = Norm(query);
            if (q.Length == 0) return true;
            var n = Norm(name);
            for (int i = 0; i + q.Length <= n.Length; i++)
            {
                int j = 0;
                while (j < q.Length && Eq(q[j], n[i + j])) j++;
                if (j == q.Length) return true;
            }
            return false;
        }

        /// <summary>이름 여럿 가운데 하나라도 맞으면.</summary>
        public static bool MatchAny(string query, params string[] names) => names.Any(x => x != null && Match(x, query));
    }

    // 도감 검색 칸(2026-10 사용자: 「도감에 검색 · 초성으로도」) — 사도 · 교주 카드 · 장비 · 적 도감 위 오른쪽 도구 줄 맨 앞.
    //   입력 즉시 걸러진다 — 격자를 다시 그리지 않고 칸을 숨기기만 해서(정렬 · 스크롤 · 입력 칸 초점이 그대로) 폰 자판도 닫히지 않는다.
    //   검색어는 도감마다 따로 기억한다(dexQuery). 결과가 없으면 「찾는 것이 없습니다」, 지우기 단추(×).
    public partial class Flow
    {
        readonly Dictionary<string, string> dexQuery = new Dictionary<string, string>();

        /// <summary>지금 화면의 도감 검색 칸(자동 데모 · 점검이 글을 넣는다).</summary>
        public TMP_InputField DexSearchField { get; private set; }
        /// <summary>지금 보이는(검색으로 걸러진) 칸 이름들 — 데모 단언용.</summary>
        public List<string> DexSearchShown { get; } = new List<string>();

        /// <summary>
        /// 검색 칸을 row(도구 줄) 맨 앞에 두고, content 의 칸(GameObject 이름 → 찾을 이름들)을 걸러 숨긴다.
        /// info = 아래 띠 글(고른 것이 없을 때 개수를 고친다), unit = 「명」 · 「장」 …
        /// </summary>
        void DexSearch(RectTransform row, string tab, RectTransform content, RectTransform area, Func<string, string[]> namesOf, TextMeshProUGUI info, Func<int, string> countText)
        {
            var cells = new List<(GameObject go, string[] names)>();
            foreach (Transform c in content)
            {
                var names = namesOf(c.name);
                if (names != null) cells.Add((c.gameObject, names));
            }
            var input = SearchInput(row, "이름 · 초성 찾기", out var clear);
            input.transform.SetAsFirstSibling();
            DexSearchField = input;
            var empty = Ui.Text(area, "찾는 것이 없습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center, false, "nomatch");
            empty.rectTransform.At(0.5f, 1, 0, -60, 600, 60);
            string infoBase = info != null ? info.text : null;
            void Apply(string q)
            {
                dexQuery[tab] = q ?? "";
                int n = 0;
                DexSearchShown.Clear();
                foreach (var (go, names) in cells)
                {
                    bool on = HangulSearch.MatchAny(q, names);
                    if (go.activeSelf != on) go.SetActive(on);
                    if (on) { n++; DexSearchShown.Add(names[0]); }
                }
                bool hasQ = !string.IsNullOrEmpty(q);
                empty.gameObject.SetActive(hasQ && n == 0);
                clear.gameObject.SetActive(hasQ);
                if (info != null && countText != null && infoBase != null && !infoBase.StartsWith("<b>")) info.text = hasQ ? countText(n) + $"  <color={Theme.SubTag}>— 「{q}」</color>" : infoBase;
                LayoutRebuilder.MarkLayoutForRebuild(content);
            }
            input.text = dexQuery.TryGetValue(tab, out var saved) ? saved : "";
            input.onValueChanged.AddListener(Apply);
            clear.OnClick = () => { input.text = ""; };
            Apply(input.text);
            Stage.Hot["dex.search.clear"] = clear;
        }

        /// <summary>검색 입력 칸(남색 알약 · 돋보기 · 지우기 ×) — TMP_InputField(폰은 화면 자판).</summary>
        TMP_InputField SearchInput(Transform parent, string placeholder, out Btn clear)
        {
            float w = Theme.C(300, 280), h = 52;
            var bg = Ui.Img(parent, Theme.S("pill_dark", 46), Color.white.A(0.96f), "search", true);
            bg.Pref(w, h);
            bg.gameObject.SetActive(false);   // 칸 · 글을 다 붙인 뒤 켠다(TMP_InputField 가 빈 글 칸으로 켜지지 않게)
            var ic = Ui.Img(bg.transform, Theme.S("ic_zoom"), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 18, 0, 22, 22);
            var areaRt = Ui.Rect("text area", bg.transform).Fill(48, 6, 44, 6);
            areaRt.gameObject.AddComponent<RectMask2D>();
            var ph = Ui.Text(areaRt, placeholder, Theme.FsBody, Theme.Dim, TextAlignmentOptions.MidlineLeft, false, "placeholder");
            ph.rectTransform.Fill(); ph.textWrappingMode = TextWrappingModes.NoWrap; ph.fontStyle = FontStyles.Normal;
            var tx = Ui.Text(areaRt, "", Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineLeft, false, "text");
            tx.rectTransform.Fill(); tx.textWrappingMode = TextWrappingModes.NoWrap; tx.overflowMode = TextOverflowModes.Overflow; tx.richText = false;
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = tx;
            input.placeholder = ph;
            input.fontAsset = Theme.Body;
            input.pointSize = tx.fontSize;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            input.characterLimit = 24;
            input.customCaretColor = true; input.caretColor = Theme.Gold; input.caretWidth = 2;
            input.selectionColor = Theme.Gold.A(0.35f);
            input.shouldHideMobileInput = false;
            input.targetGraphic = bg;
            var cb = Btn.Icon(bg.transform, Theme.S("ic_x"), null, 32, "clear");
            cb.GetComponent<RectTransform>().At(1, 0.5f, -10, 0, 32, 32);
            cb.gameObject.SetActive(false);
            clear = cb;
            bg.gameObject.SetActive(true);
            WebInput.Attach(input);   // 한글 입력 — PC 는 IME 켜기 · 조합 글자, 웹은 HTML input 겹치기(Kit/WebInput.cs)
            return input;
        }
    }
}
