using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Bolzena.RunUI
{
    // 카드 글 속 낱말 안내(2026-10 사용자: 「키워드를 누르면 무슨 효과인지 · 생성 카드 낱말을 누르면 무슨 카드인지」).
    //   판 화면(W.Card · TermPop)과 전투(CoreBattle → CardZoom)가 같이 쓴다. core 는 고치지 않고 core 의 표 · 데이터로 찾는다:
    //   · 키워드 = core CardText.Chips(카드) 의 이름들 — 풀이는 CardText.Tips()(엔진 키워드 · 상태)
    //   · 사도 고유 효과(자원 · 표식) = 사도 데이터 keywords — 부제(desc) + CardText.Trait(사도, k)(그 키워드를 쓰는 패시브까지, 수치가 다 든 한 가지 글 — 「자세히」 없음)
    //   · 사도 변신(성전 모드 · 맨주먹 전성기 …) = 사도 데이터 forms — 부제(desc) + CardText.FormBody(규칙에서). 변신이 바꿔 넣은 카드(맨주먹 연타 …)에도 붙는다
    //   · 생성 카드 = 카드 효과 make · transform 의 id, 카드 정의 Evolve.Into(★→★★) · BondCard(결속 강해진 카드) · Becomes(봉인된 금기)
    //     글에 이름이 없는 것(진화 · 결속 · 금기)은 Extra 가 「진화 → 「이름」」 한 줄로 덧붙인다.
    //   · 자유 글(고유 효과 · 패시브 · 고학년 설명) = OfText — 「」 속 카드 이름 · 사도 고유 효과 이름 · 엔진 키워드 이름을 글에서 찾는다.
    //   Mark 가 글에 <link="t:n"> 밑줄 · 색을 입힌다(해로운 것은 붉게 · 카드는 하늘색 + 카드 아이콘). 같은 낱말은 처음 한 번만.
    public static class CardTerms
    {
        public sealed class Term
        {
            public string Name, Body;
            /// <summary>옛 「자세히」 글 — 이제 쓰지 않는다(늘 null, Body 하나에 다 든다). 전투 쪽 옛 코드가 읽어도 되게 남겨 둔다.</summary>
            public string Detail;
            public bool Bad, Hero;          // 해로운 상태 · 사도 고유 효과
            public string CardId;           // 생성 카드면 그 카드 id
            public string Rel;              // 글에 이름이 없는 카드의 관계(「진화」 · 「결속 3 이상」 · 「보스 처치 뒤」) — Extra 가 쓴다
            public string Owner;            // 사도 고유 효과의 주인 이름
            public string Kind;             // 판 머리의 작은 글(없으면 고유 효과 · 카드 · 해로운 효과 가운데 저절로)
            public bool IsCard => CardId != null;
        }

        /// <summary>부제(작은 흐린 글) + 본문 — 낱말 판 · 사도 상세 칸이 같은 꼴로.</summary>
        public static string WithSub(string sub, string body)
        {
            sub = sub?.TrimEnd('.');
            return string.IsNullOrEmpty(sub) ? body ?? "" : $"<size=90%><color={Theme.SubTag}>{sub}</color></size>\n{body}";
        }

        public const string KwHex = "#FFE3A0", BadHex = "#FF9AA2", CardHex = "#8FD3FF";

        static Dictionary<string, string> tips;
        static CardText tipsOf;
        static Dictionary<string, string> Tips(CardText t)
        {
            if (tips == null || tipsOf != t) { tips = t.Tips(); tipsOf = t; }
            return tips;
        }

        // 사도 고유 효과는 그 글 · 카드 주인(heroId)의 것만 — 다른 사도의 같은 이름(디아나 「제자」 ↔ 밍스 「제자」)으로 잘못 잇지 않게.
        static Term KwTerm(GameData d, CardText t, string name, string heroId)
        {
            if (heroId != null && d.Heroes.TryGetValue(heroId, out var h))
            {
                foreach (var k in h.AllKeywords)
                    if (k.Name == name) return new Term { Name = name, Body = WithSub(k.Desc, t.Trait(h, k)), Hero = true, Owner = h.Name };
                // 변신(2026-10-07 「성전 모드 · 맨주먹 전성기가 뭔지 안 나온다」) — 부제 = desc, 본문 = core CardText.FormBody(규칙에서)
                foreach (var fm in h.Forms ?? new List<FormDef>())
                    if (fm.Name == name) return new Term { Name = name, Body = WithSub(fm.Desc, t.FormBody(fm)), Hero = true, Owner = h.Name, Kind = $"변신 · {h.Name}" };
            }
            if (Tips(t).TryGetValue(name, out var tip)) return new Term { Name = name, Body = tip, Bad = R.IsBadSt(name) };
            return null;
        }

        /// <summary>카드 한 장의 낱말 — 생성 카드 · 키워드(Chips) · 상태 · 사도 고유 효과. text 를 주면 그 글에 나오는 카드 주인의 고유 효과 이름도 잡는다.</summary>
        public static List<Term> Of(GameData d, CardText t, CardView v, string text = null)
        {
            var res = new List<Term>();
            if (v == null || d == null || t == null) return res;
            var seen = new HashSet<string>();
            // 생성 카드
            var ids = new List<(string id, string rel)>();
            void Walk(List<Fx> fx)
            {
                foreach (var f in fx ?? new List<Fx>())
                {
                    if (f == null) continue;
                    if ((f.K == FxK.Make || f.K == FxK.Transform) && f.Id != null) ids.Add((f.Id, null));
                    Walk(f.Then);
                }
            }
            Walk(v.Fx);
            if (v.Def.Evolve?.Into != null) ids.Add((v.Def.Evolve.Into, "진화"));
            if (v.Def.BondCard != null) ids.Add((v.Def.BondCard, "결속 3 이상"));
            if (v.Def.Becomes != null) ids.Add((v.Def.Becomes, "보스 처치 뒤"));
            foreach (var (id, rel) in ids)
            {
                var c = d.Card(id);
                if (c == null || id == v.Id || !seen.Add("card:" + id)) continue;
                res.Add(new Term { Name = c.Name, CardId = id, Rel = rel });
            }
            // 키워드 · 상태 · 사도 고유 효과
            List<string> chips;
            try { chips = t.Chips(v); } catch (Exception) { chips = new List<string>(); }
            // 글에 나오는 카드 주인의 고유 효과 이름(자원 · 표식 — 조건 머리 · 배율 글 속)
            var hero = v.Def.Hero ?? v.Owner;
            if (hero != null && d.Heroes.TryGetValue(hero, out var hd))
            {
                if (text != null)
                {
                    foreach (var k in hd.AllKeywords) if (k.Name != null && text.Contains(k.Name)) chips.Add(k.Name);
                    foreach (var fm in hd.Forms ?? new List<FormDef>()) if (fm.Name != null && text.Contains(fm.Name)) chips.Add(fm.Name);
                }
                var baseId = GameData.BaseId(v.Id);
                foreach (var fm in hd.Forms ?? new List<FormDef>())
                    if (fm.Name != null && fm.Cards != null && fm.Cards.Values.Contains(baseId)) chips.Insert(0, fm.Name);
            }
            foreach (var name in chips)
            {
                if (name == null || !seen.Add(name)) continue;
                var tm = KwTerm(d, t, name, hero);
                if (tm != null) res.Add(tm);
            }
            return res;
        }

        static readonly Regex Quoted = new Regex("「([^「」]+)」");
        static Dictionary<string, string> cardByName;
        static GameData cardByNameOf;

        /// <summary>
        /// 자유 글(고유 효과 · 패시브 · 고학년 · 장비 효과 설명)의 낱말 — 「」 속 카드 이름 → 생성 카드, 사도 고유 효과 이름, 엔진 키워드 · 상태 이름(두 글자 이상).
        /// 사도 고유 효과는 heroId 사도의 것만 찾는다(다른 사도의 같은 이름은 잇지 않음).
        /// </summary>
        public static List<Term> OfText(GameData d, CardText t, string text, string heroId = null)
        {
            var res = new List<Term>();
            if (string.IsNullOrEmpty(text) || d == null || t == null) return res;
            var seen = new HashSet<string>();
            if (cardByName == null || cardByNameOf != d)
            {
                cardByName = new Dictionary<string, string>();
                foreach (var c in d.Cards.Values) if (c.Name != null && !cardByName.ContainsKey(c.Name)) cardByName[c.Name] = c.Id;
                cardByNameOf = d;
            }
            foreach (Match m in Quoted.Matches(text))
            {
                var nm = m.Groups[1].Value;
                if (cardByName.TryGetValue(nm, out var id) && seen.Add("card:" + id)) res.Add(new Term { Name = nm, CardId = id });
            }
            var names = new List<string>();
            if (heroId != null && d.Heroes.TryGetValue(heroId, out var hd))
            {
                names.AddRange(hd.AllKeywords.Select(k => k.Name));
                names.AddRange((hd.Forms ?? new List<FormDef>()).Select(fm => fm.Name));
            }
            names.AddRange(CardText.TIPS.Keys);
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name) || name.Length < 2 || !text.Contains(name) || !seen.Add(name)) continue;
                var tm = KwTerm(d, t, name, heroId);
                if (tm != null) res.Add(tm);
            }
            return res;
        }

        /// <summary>글에 이름이 안 나오는 관계 카드(진화 · 결속 · 금기) 한 줄씩 — 「진화 → 「이름」」. 없으면 "".</summary>
        public static string Extra(string text, List<Term> terms)
        {
            if (terms == null) return "";
            var sb = new StringBuilder();
            foreach (var tm in terms)
            {
                if (!tm.IsCard || tm.Rel == null || (text != null && text.Contains(tm.Name))) continue;
                sb.Append('\n').Append($"{tm.Rel} → 「{tm.Name}」");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 글(리치 텍스트일 수 있다)에 낱말마다 밑줄 · 색 · 링크(「t:번호」)를 입힌다 — 긴 이름부터, 태그 안 · 이미 입힌 곳은 건너뛰고 처음 한 번만.
        /// 생성 카드는 이름 앞에 카드 아이콘(TMP 스프라이트 — 글에 Prepare 를 건다)을 붙인다.
        /// </summary>
        public static string Mark(string text, List<Term> terms)
        {
            if (string.IsNullOrEmpty(text) || terms == null || terms.Count == 0) return text;
            // 글자마다 「쓸 수 있나」(태그 밖)
            var free = new bool[text.Length];
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '<') inTag = true;
                free[i] = !inTag;
                if (text[i] == '>') inTag = false;
            }
            var spans = new List<(int at, int len, int idx)>();
            foreach (var (term, idx) in terms.Select((x, i) => (x, i)).OrderByDescending(p => p.x.Name.Length))
            {
                int from = 0;
                while (true)
                {
                    int at = text.IndexOf(term.Name, from, StringComparison.Ordinal);
                    if (at < 0) break;
                    bool ok = true;
                    for (int j = at; j < at + term.Name.Length; j++) if (!free[j]) { ok = false; break; }
                    if (ok)
                    {
                        spans.Add((at, term.Name.Length, idx));
                        for (int j = at; j < at + term.Name.Length; j++) free[j] = false;
                        break;
                    }
                    from = at + 1;
                }
            }
            if (spans.Count == 0) return text;
            spans.Sort((a, b) => a.at.CompareTo(b.at));
            var sb = new StringBuilder();
            int p0 = 0;
            foreach (var (at, len, idx) in spans)
            {
                sb.Append(text, p0, at - p0);
                var tm = terms[idx];
                string hex = tm.IsCard ? CardHex : tm.Bad ? BadHex : KwHex;
                sb.Append($"<link=\"t:{idx}\"><color={hex}>");
                if (tm.IsCard) sb.Append(CardGlyph);
                sb.Append("<u>").Append(text, at, len).Append("</u></color></link>");
                p0 = at + len;
            }
            sb.Append(text, p0, text.Length - p0);
            return sb.ToString();
        }

        /// <summary>
        /// 점검용 옛 줄바꿈(-oldwrap) — 글자 단위로 꺾던 예전 모습(TMP 옛 한글 규칙 + KeepWords 없음)을 전후 캡처로 비교할 때만.
        /// </summary>
        public static readonly bool OldWrap = Array.IndexOf(Environment.GetCommandLineArgs(), "-oldwrap") >= 0;
        static bool wrapSet;
        /// <summary>-oldwrap 이면 TMP 설정의 새 한글 줄바꿈(어절 단위)을 끈다(설정 파일은 켜 둔 채 — 2026-10-07).</summary>
        public static void ApplyWrapMode()
        {
            if (wrapSet) return;
            wrapSet = true;
            if (!OldWrap) return;
            try
            {
                var inst = TMP_Settings.instance;
                typeof(TMP_Settings).GetField("m_UseModernHangulLineBreakingRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(inst, false);
            }
            catch (Exception e) { Debug.LogWarning("[CardTerms] 옛 줄바꿈 설정 실패: " + e.Message); }
        }

        /// <summary>
        /// 카드 글 · 낱말 판 글을 어절 단위로 꺾되, 끊으면 안 되는 덩이는 &lt;nobr&gt; 로 묶는다(2026-10-07 사용자 「『사기』 가 『사 / 기』 로 갈린다」).
        /// 줄바꿈 자체는 TMP 설정의 새 한글 규칙(어절 단위 — TMP Settings useModernHangulLineBreakingRules)이 하고, 여기서는 덩이만:
        ///   · 「…」 이름 · 밑줄 낱말(&lt;link&gt;) 안의 띄어쓰기는 끊지 않음(「첩보원 모드」 · 「방어 기반 피해」)
        ///   · 수치 + 단위는 앞 낱말에 붙임(「피해 120%」 · 「사기 1」 · 「「당」 1」)
        ///   · 한 글자 낱말은 뒤 낱말과(「그 적에게」), 「더」 는 앞 낱말과(「피해 90% 더」), 문단 끝 2글자 이하는 앞 낱말과(「피해」 가 홀로 남지 않게)
        ///   덩이가 너무 길어지지 않게(이름 · 낱말 안이 아니면) 12글자까지만 붙인다. Mark · 수치 색을 입힌 뒤 맨 나중에 부른다.
        /// </summary>
        public static string KeepWords(string rich)
        {
            ApplyWrapMode();
            if (string.IsNullOrEmpty(rich) || OldWrap) return rich;
            var sb = new StringBuilder(rich.Length + 96);
            var lines = rich.Split('\n');
            for (int li = 0; li < lines.Length; li++)
            {
                var line = lines[li];
                if (li > 0) sb.Append('\n');
                var words = new List<string>();
                var cur = new StringBuilder();
                bool inTag = false;
                foreach (char ch in line)
                {
                    if (inTag) { cur.Append(ch); if (ch == '>') inTag = false; continue; }
                    if (ch == '<') { inTag = true; cur.Append(ch); continue; }
                    if (ch == ' ') { words.Add(cur.ToString()); cur.Clear(); continue; }
                    cur.Append(ch);
                }
                words.Add(cur.ToString());
                var groups = new List<string>();
                var gVis = new List<int>();
                int open = 0, link = 0;   // 열린 「 · <link> 수(앞 낱말까지)
                for (int i = 0; i < words.Count; i++)
                {
                    var w = words[i];
                    int vis = Vis(w);
                    string pw = Plain(w), prev = i > 0 ? Plain(words[i - 1]) : "";
                    char first = pw.Length > 0 ? pw[0] : ' ';
                    bool numeric = char.IsDigit(first) || first == '+' || first == '-' || first == '−' || first == '×';
                    bool inside = open > 0 || link > 0;
                    // 앞에 붙임: 수치 · 「더」 · 앞 낱말이 한 글자(→ · 그 · 적 — 뒤 낱말과 한 덩이) · 문단 끝 2글자 이하(홀로 남는 줄 막기)
                    bool want = inside || numeric || pw == "더" || (prev.Length == 1 && prev != "더") || (i == words.Count - 1 && vis <= 2);
                    int cap = i == words.Count - 1 && vis <= 2 ? 16 : 12;   // 문단 끝 짧은 낱말은 조금 더 길게 붙여도 홀로 두지 않는다
                    bool glue = groups.Count > 0 && want && (inside || gVis[gVis.Count - 1] + 1 + vis <= cap);
                    if (glue) { groups[groups.Count - 1] += " " + w; gVis[gVis.Count - 1] += 1 + vis; }
                    else { groups.Add(w); gVis.Add(vis); }
                    foreach (char c in pw) { if (c == '「') open++; else if (c == '」') open = Math.Max(0, open - 1); }
                    link += Count(w, "<link") - Count(w, "</link>");
                    if (link < 0) link = 0;
                }
                for (int g = 0; g < groups.Count; g++)
                {
                    if (g > 0) sb.Append(' ');
                    if (gVis[g] <= 1) sb.Append(groups[g]);
                    else sb.Append("<nobr>").Append(groups[g]).Append("</nobr>");
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 카드 글 · 낱말 판 글의 최종 모습 — 조건 줄 나눔 + KeepWords.
        /// 2026-10-07 화살표 없앰(사용자 「카드 설명에 화살표랑 + 없애 줘」) — 엔진은 「조건 결과」 를 띄어쓰기 하나로 잇는다:
        ///   · 「조건 결과」 한 줄이 칸 폭(maxW)에 다 들어가면 한 줄 그대로
        ///   · 안 들어가면 언제나 「조건,」 / 「결과」 로 나눈다 — 조건 줄 끝의 쉼표가 「다음 줄로 이어짐」 표시(가운데 맞춤 칸이라 들여쓰기는 안 보임).
        ///     조건이 어디서 끝나는지는 엔진이 지은 머리 목록(core CardText.HeadEnd)으로 찾는다. 결과 줄이 또 넘치면 결과 안에서 어절 단위로 꺾인다.
        /// lineWidth = 그 글 한 줄의 폭(꺾지 않고 잰 것) — 화면이 그릴 글자 크기로 잰다(LineWidth).
        /// </summary>
        public static string Fit(string rich, Func<string, float> lineWidth, float maxW)
        {
            ApplyWrapMode();
            if (string.IsNullOrEmpty(rich) || OldWrap) return rich;
            var lines = rich.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lineWidth(lines[i]) <= maxW - 0.5f * 0.01f * maxW) continue;
                int at = CondBreak(lines[i]);
                if (at > 0) lines[i] = lines[i].Substring(0, at) + ",\n" + lines[i].Substring(at + 1);
            }
            return KeepWords(string.Join("\n", lines));
        }

        /// <summary>리치 글 한 줄에서 조건 머리 뒤 띄어쓰기의 자리(태그 밖) — 없으면 -1.</summary>
        static int CondBreak(string rich)
        {
            var plain = new StringBuilder(rich.Length);
            var map = new List<int>(rich.Length);
            bool tag = false, sprite = false;
            for (int k = 0; k < rich.Length; k++)
            {
                char c = rich[k];
                if (tag) { if (c == '>') tag = false; continue; }
                if (c == '<') { tag = true; sprite = string.CompareOrdinal(rich, k, "<sprite", 0, 7) == 0; continue; }
                if (sprite) { sprite = false; if (c == ' ') continue; }   // 카드 아이콘(Mark 의 CardGlyph) 뒤 띄어쓰기는 엔진 글에 없다
                plain.Append(c); map.Add(k);
            }
            int p = CardText.HeadEnd(plain.ToString());
            return p >= 0 && p < map.Count && rich[map[p]] == ' ' ? map[p] : -1;
        }

        /// <summary>글 한 줄을 꺾지 않고 잰 폭 — t 의 글꼴 · 크기 size 로(자동 크기는 잠깐 끈다).</summary>
        public static float LineWidth(TMP_Text t, string line, float size)
        {
            bool auto = t.enableAutoSizing; float fs = t.fontSize;
            t.enableAutoSizing = false; t.fontSize = size;
            float w = t.GetPreferredValues(line).x;
            t.enableAutoSizing = auto; t.fontSize = fs;
            return w;
        }

        /// <summary>t 에 넣을 글 — 조건 줄 나눔을 t 의 지금 크기 · 폭으로(Fit).</summary>
        public static string FitFor(TMP_Text t, string rich, float maxW, float size) => Fit(rich, l => LineWidth(t, l, size), maxW);

        static string Plain(string w) { var o = new StringBuilder(); bool t = false; foreach (char c in w) { if (t) { if (c == '>') t = false; continue; } if (c == '<') { t = true; continue; } o.Append(c); } return o.ToString(); }
        static int Vis(string w) => Plain(w).Length;
        static char FirstVis(string w) { var p = Plain(w); return p.Length > 0 ? p[0] : ' '; }
        static int Count(string s, string sub) { int n = 0, i = 0; while ((i = s.IndexOf(sub, i, StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; } return n; }

        // ── 카드 아이콘(글 속) ── ic_deck 한 장짜리 TMP 스프라이트 애셋을 런타임에 만든다. 셰이더가 없으면 글리프 「▣」로 대신
        static TMP_SpriteAsset icons;
        static bool iconsTried;
        public static TMP_SpriteAsset Icons
        {
            get
            {
                if (iconsTried) return icons;
                iconsTried = true;
                try
                {
                    var sh = Shader.Find("TextMeshPro/Sprite");
                    var tex = Resources.Load<Texture2D>("RunUI/Sprites/ic_deck");
                    if (sh == null || tex == null) return null;
                    var sa = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                    sa.name = "RunUI term icons (runtime)";
                    // 옛 꼴 올리기(UpgradeSpriteAsset)가 표를 지우지 않게 판 번호를 먼저 적는다
                    typeof(TMP_SpriteAsset).GetField("m_Version", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sa, "1.1.0");
                    sa.spriteSheet = tex;
                    var mat = new Material(sh) { name = "RunUI term icons" };
                    mat.SetTexture(ShaderUtilities.ID_MainTex, tex);
                    sa.material = mat;
                    float w = tex.width, h = tex.height;
                    var glyph = new TMP_SpriteGlyph(0, new GlyphMetrics(w, h, w * 0.04f, h * 0.84f, w * 1.12f), new GlyphRect(0, 0, (int)w, (int)h), 1f, 0);
                    sa.spriteGlyphTable.Add(glyph);
                    var ch = new TMP_SpriteCharacter(0xE000, sa, glyph) { name = "card", scale = 0.92f };
                    sa.spriteCharacterTable.Add(ch);
                    sa.UpdateLookupTables();
                    icons = sa;
                }
                catch (Exception e) { Debug.LogWarning("[RunUI] 카드 아이콘 스프라이트 실패 — 글리프로 대신: " + e.Message); icons = null; }
                return icons;
            }
        }

        static string CardGlyph => Icons != null ? "<sprite index=0 tint=1> " : "▣ ";

        /// <summary>Mark 한 글을 담을 TMP 에 아이콘 애셋을 건다(Mark 전에 · 뒤에 아무 때나).</summary>
        public static void Prepare(TMP_Text t)
        {
            if (t != null && Icons != null) t.spriteAsset = Icons;
        }
    }
}
