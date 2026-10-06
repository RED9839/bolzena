using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 카드 · 사도 그림(임시 규칙 — Docs/카드그림.md). 판 화면(W.Card)과 전투 화면(CardView)이 같이 쓴다.
    //   시작 카드(사도의 기본 카드) = 그 사도 스탠딩의 상반신(카드 비율로 자른다)
    //   고유 · 생성 카드          = 카드 그림 표(Resources/RunUI/cardart.json)의 "pics" — 카드와 어울리는 원작 그림(사도마다 대조 시트로 골랐다 · Tools~/cardpic_picks.json)
    //                             표에 없거나 어울리는 그림이 없으면 "cards" 의 원작 스킬 아이콘 — 흐린 확대 바탕 + 가운데 선명한 아이콘
    //   교주 · 상태 · 저주 · 선물 = 장비 · 교주 카드 그림 표(Resources/RunUI/itemart.json)의 원작 아이콘 — 없으면 종류 무늬
    //   장비                    = 같은 표의 장비 아이콘(Equip)
    // 그림 파일(원작, git 밖)은 Tools~/copy_assets.py 가 Resources/RunArt/Standing · Skill 로 복사한다. 없으면 null — 화면이 SD 초상 · 무늬로 떨어진다.
    public static class CardArt
    {
        public enum Kind { None, Standing, Icon, Pic }

        static Dictionary<string, string> icons, pics, itemCards, itemEquips;
        static Dictionary<string, float[]> meta;

        static Dictionary<string, string> Icons()
        {
            if (icons != null) return icons;
            icons = new Dictionary<string, string>(); pics = new Dictionary<string, string>();
            var ta = Resources.Load<TextAsset>("RunUI/cardart");
            if (ta == null) { Debug.LogWarning("[CardArt] cardart.json 없음 — Tools~/build_cardart.py"); return icons; }
            try
            {
                var o = MiniJson.Object(ta.text, "cards");
                foreach (var kv in o) icons[kv.Key] = kv.Value;
                if (ta.text.Contains("\"pics\""))
                    foreach (var kv in MiniJson.Object(ta.text, "pics")) pics[kv.Key] = kv.Value;
            }
            catch (Exception e) { Debug.LogWarning("[CardArt] 표를 못 읽었습니다: " + e.Message); }
            return icons;
        }

        /// <summary>고유 · 생성 카드의 원작 그림 이름(표에 없으면 null). id 는 신탁 꼬리가 붙어도 된다.</summary>
        public static string PicOf(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            Icons();
            var b = Core.GameData.BaseId(cardId);
            return pics.TryGetValue(cardId, out var n) || (b != cardId && pics.TryGetValue(b, out n)) ? n : null;
        }

        /// <summary>원작 그림 한 장 — 카드 그림 창 비율(0.71)로 미리 잘라 둔 것(Tools~/copy_assets.py → RunArt/CardPic). 없으면 null.</summary>
        public static Sprite Pic(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string key = "pic|" + name;
            if (crops.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>("RunArt/CardPic/" + name);
            if (tex != null) { tex.wrapMode = TextureWrapMode.Clamp; s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100); s.name = "cardpic " + name; }
            Keep(); crops[key] = s;
            return s;
        }

        // 장비 · 교주 카드(상태 · 저주 · 선물) 그림 표 — Resources/RunUI/itemart.json(Tools~/build_itemart.py)
        static void Items()
        {
            if (itemCards != null) return;
            itemCards = new Dictionary<string, string>(); itemEquips = new Dictionary<string, string>();
            var ta = Resources.Load<TextAsset>("RunUI/itemart");
            if (ta == null) { Debug.LogWarning("[CardArt] itemart.json 없음 — Tools~/build_itemart.py"); return; }
            try
            {
                foreach (var kv in MiniJson.Object(ta.text, "cards")) itemCards[kv.Key] = kv.Value;
                foreach (var kv in MiniJson.Object(ta.text, "equips")) itemEquips[kv.Key] = kv.Value;
            }
            catch (Exception e) { Debug.LogWarning("[CardArt] 장비 · 교주 카드 표를 못 읽었습니다: " + e.Message); }
        }

        /// <summary>장비 그림(없으면 null — 화면은 칸 무늬로).</summary>
        public static Sprite Equip(string equipId)
        {
            Items();
            return equipId != null && itemEquips.TryGetValue(equipId, out var n) ? Icon(n) : null;
        }

        static Dictionary<string, float[]> Meta()
        {
            if (meta != null) return meta;
            meta = new Dictionary<string, float[]>();
            var ta = Resources.Load<TextAsset>("RunArt/Standing/_meta");
            if (ta == null) return meta;
            try { foreach (var kv in MiniJson.Numbers(ta.text)) meta[kv.Key] = kv.Value; }
            catch (Exception e) { Debug.LogWarning("[CardArt] 스탠딩 표를 못 읽었습니다: " + e.Message); }
            return meta;
        }

        /// <summary>고유 카드의 아이콘 이름(표에 없으면 null). id 는 신탁 꼬리가 붙어도 된다.</summary>
        public static string IconOf(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            var t = Icons();
            Items();
            var b = Core.GameData.BaseId(cardId);
            if (t.TryGetValue(cardId, out var n) || (b != cardId && t.TryGetValue(b, out n))) return n;
            return itemCards.TryGetValue(cardId, out n) || (b != cardId && itemCards.TryGetValue(b, out n)) ? n : null;
        }

        /// <summary>카드 그림 종류 — heroArt 는 주인 사도의 그림 키(roster art), 없으면 교주 · 상태 카드.</summary>
        //   시작 카드(사도의 기본 카드)는 늘 스탠딩 — 그림 표(pics · cards)에는 고유 · 생성 카드만 있다
        public static Kind Of(string cardId, string heroArt, bool unique)
        {
            if (string.IsNullOrEmpty(heroArt)) return Icon(IconOf(cardId)) != null ? Kind.Icon : Kind.None;   // 교주 · 상태 · 저주 · 선물 — 표에 그림이 있으면 아이콘 꼴
            if (Pic(PicOf(cardId)) != null) return Kind.Pic;
            if (unique) return IconOf(cardId) != null && Icon(IconOf(cardId)) != null ? Kind.Icon : Kind.None;
            if (IconOf(cardId) != null && Icon(IconOf(cardId)) != null) return Kind.Icon;   // 생성 카드에 고른 원작 스킬 아이콘
            return Standing(heroArt) != null ? Kind.Standing : Kind.None;
        }

        public static Sprite Icon(string name) => name == null ? null : Theme.Art("Skill/" + name) ?? Theme.Art("Item/" + name);
        public static Sprite Blur(string name) => name == null ? null : Theme.Art("Skill/" + name + "_blur") ?? Theme.Art("Item/" + name + "_blur");

        static readonly Dictionary<string, Sprite> crops = new Dictionary<string, Sprite>();
        // 자른 그림 · 스탠딩은 넘치면 비운다(쓰는 화면이 쥔 것은 산다) — 판을 오래 하면 사도 · 카드 그림이 끝없이 쌓였다
        static void Keep() { if (crops.Count >= 200) crops.Clear(); }

        // 스탠딩 그림 자리 — 2의 거듭제곱 판 왼쪽 위에 붙여 두었다(_meta.json 의 너비 · 높이). 표가 없으면 판 전체
        static bool Content(string art, out Texture2D tex, out Rect r, out float cx, out float tp)
        {
            tex = string.IsNullOrEmpty(art) ? null : Resources.Load<Texture2D>("RunArt/Standing/" + art);
            r = default; cx = 0.5f; tp = 0f;
            if (tex == null) return false;
            tex.wrapMode = TextureWrapMode.Clamp;
            float w = tex.width, h = tex.height;
            if (Meta().TryGetValue(art, out var m))
            {
                if (m.Length > 0) cx = m[0];
                if (m.Length >= 3) { w = Mathf.Min(m[1], tex.width); h = Mathf.Min(m[2], tex.height); }
                if (m.Length >= 4) tp = m[3];
            }
            r = new Rect(0, tex.height - h, w, h);
            return true;
        }

        /// <summary>사도 스탠딩 한 장(전신 · 투명 가장자리를 자른 것 — 위 = 머리 끝).</summary>
        public static Sprite Standing(string art)
        {
            if (string.IsNullOrEmpty(art)) return null;
            string key = art + "|full";
            if (crops.TryGetValue(key, out var s)) return s;
            s = Content(art, out var tex, out var r, out _, out _) ? Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100) : null;
            if (s != null) s.name = "standing " + art;
            Keep(); crops[key] = s;
            return s;
        }

        /// <summary>
        /// 스탠딩의 위쪽을 ratio(가로/세로) 로 자른 그림 — frac 은 그림 높이의 몇 할을 담을지(0.5 = 상반신, 0.8 = 무릎께).
        /// 가로 가운데 · 위 끝은 머리 자리(_meta.json — 자동 판정 + Tools~/standing_fix.json). 그림이 좁아 비율을 못 채우면 높이를 줄인다.
        /// </summary>
        public static Sprite Upper(string art, float ratio, float frac, bool snapFirst = true)
        {
            if (string.IsNullOrEmpty(art)) return null;
            // 스탠딩 맞춤 표에 있는 사도는 스파인 첫 프레임을 표대로 구운 그림(StandingSnap) — 중심 · 머리 · 크기(안 B)가 화면마다 같다
            if (snapFirst) { var snap = StandingSnap.Upper(art, ratio, frac); if (snap != null) return snap; }
            string key = art + "|" + ratio.ToString("F3") + "|" + frac.ToString("F2");
            if (crops.TryGetValue(key, out var s)) return s;
            if (!Content(art, out var tex, out var r, out var hx, out var tp)) { Keep(); crops[key] = null; return null; }
            float w = r.width, h = r.height;
            float ch = h * Mathf.Clamp01(frac), cw = ch * ratio;
            if (cw > w) { cw = w; ch = cw / ratio; }
            float x = Mathf.Clamp(hx * w - cw / 2, 0, w - cw);
            float top = Mathf.Clamp(tp * h - h * 0.03f, 0, h - ch);   // 머리 끝(가는 장식은 건너뛴 자리) 조금 위부터
            s = Sprite.Create(tex, new Rect(r.x + x, r.y + h - top - ch, cw, ch), new Vector2(0.5f, 0.5f), 100);
            s.name = "upper " + art;
            Keep(); crops[key] = s;
            return s;
        }

        // 아주 작은 JSON 읽기 — {"cards": {"a": "b", …}} · {"k": [x, y], …} 꼴만(JsonUtility 는 사전을 못 읽는다)
        static class MiniJson
        {
            public static Dictionary<string, string> Object(string json, string field)
            {
                int i = json.IndexOf("\"" + field + "\"", StringComparison.Ordinal);
                if (i < 0) throw new Exception(field + " 없음");
                i = json.IndexOf('{', i);
                var d = new Dictionary<string, string>();
                while (true)
                {
                    var k = Str(json, ref i); if (k == null) break;
                    i = json.IndexOf(':', i) + 1;
                    var v = Str(json, ref i); if (v == null) break;
                    d[k] = v;
                    int comma = json.IndexOf(',', i), close = json.IndexOf('}', i);
                    if (close >= 0 && (comma < 0 || close < comma)) break;
                }
                return d;
            }

            public static Dictionary<string, float[]> Numbers(string json)
            {
                var d = new Dictionary<string, float[]>();
                int i = 0;
                while (true)
                {
                    var k = Str(json, ref i); if (k == null) break;
                    int a = json.IndexOf('[', i), b = json.IndexOf(']', a);
                    if (a < 0 || b < 0) break;
                    var parts = json.Substring(a + 1, b - a - 1).Split(',');
                    var f = new float[parts.Length];
                    for (int j = 0; j < parts.Length; j++) float.TryParse(parts[j].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f[j]);
                    d[k] = f;
                    i = b + 1;
                }
                return d;
            }

            // i 뒤의 다음 "…" 를 읽는다(이스케이프는 \" · \\ 만)
            static string Str(string s, ref int i)
            {
                int a = s.IndexOf('"', i);
                if (a < 0) return null;
                var sb = new System.Text.StringBuilder();
                int j = a + 1;
                for (; j < s.Length; j++)
                {
                    char c = s[j];
                    if (c == '\\' && j + 1 < s.Length) { sb.Append(s[++j]); continue; }
                    if (c == '"') break;
                    sb.Append(c);
                }
                i = j + 1;
                return sb.ToString();
            }
        }
    }
}
