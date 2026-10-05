namespace Bolzena.Core
{
    /// <summary>조사 — 받침에 따라 이/가 · 을/를 · 은/는 · 과/와 · 으로/로.</summary>
    public static class Ko
    {
        /// <summary>마지막 글자에 받침이 있나. 숫자 · 영문은 읽는 소리로.</summary>
        public static bool HasFinal(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            int i = word.Length - 1;
            while (i > 0 && (word[i] == '」' || word[i] == ')' || word[i] == ' ' || word[i] == '"')) i--;
            char c = word[i];
            if (c >= 0xAC00 && c <= 0xD7A3) return (c - 0xAC00) % 28 != 0;
            if (c >= '0' && c <= '9') return "013678".IndexOf(c) >= 0;
            if (char.IsLetter(c)) return "lmnrLMNR".IndexOf(c) >= 0;
            return false;
        }

        /// <summary>단어 + 조사 — pair 는 "이가" · "을를" · "은는" · "과와".</summary>
        public static string J(string word, string pair) => word + (HasFinal(word) ? pair[0] : pair[1]);
    }
}
