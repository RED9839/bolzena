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

        /// <summary>그림 표의 카드 id — 원작 그림(pics) · 스킬 아이콘(cards, pics 에 없는 것). 전후 시트용.</summary>
        public static List<string> PicCards() { Icons(); return new List<string>(pics.Keys); }
        public static List<string> IconCards() { var t = Icons(); var l = new List<string>(); foreach (var k in t.Keys) if (!pics.ContainsKey(k)) l.Add(k); return l; }

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

        // ── 그림 자리 규칙(2026-10-07 「몇몇 카드가 카드 이미지가 너무 붕 뜬 것」 — Docs/카드그림.md 「그림 자리」) ──
        //   판 W.Card · 전투 CardView 가 같은 Place 를 쓴다. 칸 = 위 글 · 칩 아래(top) ~ 효과 판 장식 선(bottom) — 둘 다 그림 창 높이에 대한 위에서부터의 몫.
        //   사물 · SD(CardObj — 알파 경계로 자른 것) · 스킬 아이콘 판(정사각): 칸 높이의 FillH 를 채우고(너비는 창의 MaxW 까지) 아래를 장식 선 Gap 위에 붙인다.
        //   늘리는 상한: 화면마다 가장 크게 보이는 그림 창 높이(RefBattle — 1440p 전투 확대 · RefBoard — 4K 덱 보기)에서 원본 내용 px 의 MaxUp 배까지 — 넘으면 그 크기에서 멈춘다(capped).
        //   장면 그림(story · cg — CardPic)은 창을 덮는다(cover · 미리 자른 한 장).
        //   카드별 손보정: Resources/RunUI/cardart_fit.json {"<그림 · 아이콘 이름>": [배율, 아래로 옮길 몫]}
        public const float FillH = 0.95f, MaxW = 0.92f, Gap = 0.012f, RefBattle = 830f, RefBoard = 460f, MaxUp = 2.8f;
        /// <summary>-oldpicfit — 예전 자리(한 장 굽기 · 가운데 아이콘 판)로 그린다(전후 시트용).</summary>
        public static readonly bool OldFit = Array.IndexOf(Environment.GetCommandLineArgs(), "-oldpicfit") >= 0;

        public struct ObjPic
        {
            public Sprite Sprite;
            public Rect Content;     // 판 안 내용 자리(px · y 는 위에서)
            public Vector2 Tex;      // 판 크기(px)
            public Vector2 Src;      // 원본 내용 크기(px — 늘리는 상한 기준)
            public int Nature;       // 바탕 번호(Back)
        }

        static Dictionary<string, float[]> objMeta, fitTable;
        static readonly Dictionary<string, ObjPic> objs = new Dictionary<string, ObjPic>();

        static Dictionary<string, float[]> ObjMeta()
        {
            if (objMeta != null) return objMeta;
            objMeta = new Dictionary<string, float[]>();
            var ta = Resources.Load<TextAsset>("RunArt/CardObj/_meta");
            if (ta != null)
                try { foreach (var kv in MiniJson.Numbers(ta.text)) objMeta[kv.Key] = kv.Value; }
                catch (Exception e) { Debug.LogWarning("[CardArt] 사물 그림 표를 못 읽었습니다: " + e.Message); }
            return objMeta;
        }

        /// <summary>사물 · SD 그림(알파 경계로 자른 것 — RunArt/CardObj). 없거나 -oldpicfit 이면 false.</summary>
        public static bool Obj(string name, out ObjPic o)
        {
            o = default;
            if (OldFit || string.IsNullOrEmpty(name)) return false;
            if (objs.TryGetValue(name, out o)) return o.Sprite != null;
            if (ObjMeta().TryGetValue(name, out var m) && m.Length >= 9)
            {
                var tex = Resources.Load<Texture2D>("RunArt/CardObj/" + name);
                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    o.Sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                    o.Sprite.name = "cardobj " + name;
                    float sx = tex.width / Mathf.Max(1, m[4]), sy = tex.height / Mathf.Max(1, m[5]);   // 가져오기가 줄였으면 그 비율로
                    o.Content = new Rect(m[0] * sx, m[1] * sy, m[2] * sx, m[3] * sy);
                    o.Tex = new Vector2(tex.width, tex.height);
                    o.Src = new Vector2(m[6], m[7]);
                    o.Nature = (int)m[8];
                }
            }
            objs[name] = o;
            return o.Sprite != null;
        }

        /// <summary>사물 그림 뒤 성격 바탕(그라데이션 + 둥근 빛 — RunArt/CardPic/_back_N).</summary>
        public static Sprite Back(int nature) => Pic("_back_" + nature);

        /// <summary>고른 원작 그림이 있나(장면 한 장 또는 사물).</summary>
        public static bool HasPic(string name) => !string.IsNullOrEmpty(name) && (Obj(name, out _) || Pic(name) != null);

        static bool Fit(string key, out float scale, out float offY)
        {
            scale = 1; offY = 0;
            if (fitTable == null)
            {
                fitTable = new Dictionary<string, float[]>();
                var ta = Resources.Load<TextAsset>("RunUI/cardart_fit");
                if (ta != null)
                    try { foreach (var kv in MiniJson.Numbers(ta.text)) fitTable[kv.Key] = kv.Value; }
                    catch (Exception e) { Debug.LogWarning("[CardArt] cardart_fit.json 을 못 읽었습니다: " + e.Message); }
            }
            if (key == null || !fitTable.TryGetValue(key, out var f)) return false;
            if (f.Length > 0 && f[0] > 0) scale = f[0];
            if (f.Length > 1) offY = f[1];
            return true;
        }

        /// <summary>
        /// 그림 내용 자리(창 몫 — x 왼쪽부터 · y 위에서부터). cw · ch = 내용 가로세로(비율만 쓴다) · srcH = 원본 내용 높이 px(늘리는 상한, 0 = 상한 없음)
        /// · aspect = 창 가로/세로 · top = 위 글 · 칩 끝 · bottom = 효과 판 장식 선(창 몫) · refPx = 그 화면에서 가장 크게 보이는 창 높이. capped = 늘리는 상한에 걸렸다.
        /// </summary>
        public static Rect Place(float cw, float ch, float srcH, float aspect, float top, float bottom, string key, out bool capped, float refPx = RefBattle)
        {
            float r = cw / Mathf.Max(1e-3f, ch);
            float floor = bottom - Gap;
            float h = FillH * Mathf.Max(0.08f, floor - top);
            h = Mathf.Min(h, MaxW * aspect / r);
            capped = false;
            if (srcH > 0 && h > MaxUp * srcH / refPx) { h = MaxUp * srcH / refPx; capped = true; }
            if (Fit(key, out var sc, out var oy)) { h *= sc; floor += oy; }
            float w = h * r / aspect;
            return new Rect(0.5f - w / 2, floor - h, w, h);
        }

        /// <summary>사물 그림 — 내용 자리(Place)에서 판 전체(그림자 여백 포함) 자리로.</summary>
        public static Rect Full(ObjPic o, Rect content)
        {
            float kx = content.width / o.Content.width, ky = content.height / o.Content.height;
            return new Rect(content.x - o.Content.x * kx, content.y - o.Content.y * ky, o.Tex.x * kx, o.Tex.y * ky);
        }

        /// <summary>사물 그림 자리(판 전체 · 창 몫) 한 번에.</summary>
        public static Rect PlaceObj(string name, ObjPic o, float aspect, float top, float bottom, out bool capped, float refPx = RefBattle) =>
            Full(o, Place(o.Content.width, o.Content.height, o.Src.y, aspect, top, bottom, name, out capped, refPx));

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
            if (HasPic(PicOf(cardId))) return Kind.Pic;   // 장면 한 장(CardPic) 또는 사물(CardObj)
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
        /// 카드 그림(시작 카드 · 생성 카드의 사도 스탠딩) — 판 화면 W.Card · 전투 CardView 가 같은 한 장(StandingSnap.CardRect · 비율 0.70):
        /// 얼굴이 위 비용 · 이름 · 종류 알약과 아래 효과 판 사이 안전 구역(위에서 26~61%)에, 머리 전체와 어깨 · 가슴께까지.
        /// 굽지 못하면(표 · 스파인 없음) 웹판 정지 렌더 자르기로.
        /// </summary>
        public static Sprite Card(string art)
        {
            if (string.IsNullOrEmpty(art)) return null;
            return StandingSnap.Card(art) ?? Upper(art, StandingSnap.CardRatio, 0.56f, false);
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
