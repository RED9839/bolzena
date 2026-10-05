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
    //   · 사도 고유 효과(자원 · 표식) = 사도 데이터 keywords — 짧은 글 CardText.Short(k), 자세히 Detail(k)
    //   · 생성 카드 = 카드 효과 make · transform 의 id, 카드 정의 Evolve.Into(★→★★) · BondCard(결속 강해진 카드) · Becomes(봉인된 금기)
    //     글에 이름이 없는 것(진화 · 결속 · 금기)은 Extra 가 「진화 → 「이름」」 한 줄로 덧붙인다.
    //   · 자유 글(고유 효과 · 패시브 · 고학년 설명) = OfText — 「」 속 카드 이름 · 사도 고유 효과 이름 · 엔진 키워드 이름을 글에서 찾는다.
    //   Mark 가 글에 <link="t:n"> 밑줄 · 색을 입힌다(해로운 것은 붉게 · 카드는 하늘색 + 카드 아이콘). 같은 낱말은 처음 한 번만.
    public static class CardTerms
    {
        public sealed class Term
        {
            public string Name, Body, Detail;
            public bool Bad, Hero;          // 해로운 상태 · 사도 고유 효과
            public string CardId;           // 생성 카드면 그 카드 id
            public string Rel;              // 글에 이름이 없는 카드의 관계(「진화」 · 「결속 3 이상」 · 「보스 처치 뒤」) — Extra 가 쓴다
            public string Owner;            // 사도 고유 효과의 주인 이름
            public string Kind;             // 판 머리의 작은 글(없으면 고유 효과 · 카드 · 해로운 효과 가운데 저절로)
            public bool IsCard => CardId != null;
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
                foreach (var k in h.AllKeywords)
                    if (k.Name == name) return new Term { Name = name, Body = t.Short(k), Detail = t.Detail(k), Hero = true, Owner = h.Name };
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
            if (text != null && hero != null && d.Heroes.TryGetValue(hero, out var hd))
                foreach (var k in hd.AllKeywords) if (k.Name != null && text.Contains(k.Name)) chips.Add(k.Name);
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
            if (heroId != null && d.Heroes.TryGetValue(heroId, out var hd)) names.AddRange(hd.AllKeywords.Select(k => k.Name));
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
