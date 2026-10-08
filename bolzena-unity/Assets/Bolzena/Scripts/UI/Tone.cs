using System;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 전투 화면의 톤 — 판 화면(bolzena-runui Docs/톤.md)과 같은 값. 판과 전투를 오가도 같은 게임처럼 보이게.
    //   월드 1 단위 = 톤.md 캔버스 100 (카메라 높이 9 = 900). 글자 단계도 그대로 0.01 배.
    //   폰(Compact) — 판 화면처럼 1280×720 기준으로 세운 것과 같게, 화면 가장자리에 붙은 HUD 무리 · 정보 창 · 적 머리 위를 K(1.25)배로 키운다.
    public static class Tone
    {
        public static readonly Color Night = Hex("070A14");
        public static readonly Color Navy = Hex("101830");
        public static readonly Color Cell = Hex("1A2340");
        public static readonly Color Well = Hex("0A0F1C");
        public static readonly Color Edge = Hex("C9A35A");
        public static readonly Color Gold = Hex("F2CF7A");
        public static readonly Color GoldDeep = Hex("D9A441");
        public static readonly Color Ink = Hex("EEF1FA");
        public static readonly Color Sub = Hex("A7B1CC");
        public static readonly Color Dim = Hex("6B7591");
        public static readonly Color Good = Hex("6EE0A0");
        public static readonly Color Bad = Hex("FF7A86");
        public static readonly Color Sky = Hex("78C6FF");
        public static readonly Color Rose = Hex("E8566A");
        public static readonly Color Brown = Hex("2A1A05");
        public static readonly Color Line = new Color(1, 1, 1, 0.10f);
        public static readonly Color Outline = new Color(0.01f, 0.02f, 0.06f, 0.95f);
        public const string GoldTag = "#F2CF7A", SubTag = "#A7B1CC", DimTag = "#6B7591", GoodTag = "#6EE0A0", BadTag = "#FF7A86", SkyTag = "#78C6FF";

        // 글자 단계(월드 단위) — 톤.md 3 절 Cap 14 · Sm 16 · Body 18 · Md 20 · Lg 24 · Xl 30
        public const float Cap = 0.14f, Sm = 0.16f, Body = 0.18f, Md = 0.20f, Lg = 0.24f, Xl = 0.30f;

        // 성격 색(초상 테 · 점) — 톤.md 1 절
        public static Color Nature(string n)
        {
            switch (n)
            {
                case "순수": return Hex("A8D8B5");
                case "광기": return Hex("E89E94");
                case "냉정": return Hex("99CADC");
                case "우울": return Hex("BFB2E0");
                case "활발": return Hex("EDCE86");
                case "공명": return Hex("DBD2BB");
            }
            return Sub;
        }

        // ── 폰(Compact) — 판 화면 Theme.MeasureCompact 와 같은 판정(높이 600 이하 · 3인치 아래 · 모바일 · -phone) ──
        static int measuredH = -1, measuredW = -1;
        static bool compact;
        public static bool Compact
        {
            get
            {
                if (Screen.height != measuredH || Screen.width != measuredW)
                {
                    measuredH = Screen.height; measuredW = Screen.width;
                    Bolzena.RunUI.Theme.MeasureCompact();
                    compact = Bolzena.RunUI.Theme.Compact;
                }
                return compact;
            }
        }
        /// <summary>HUD · 정보 창 배율 — PC 1, 폰 1.25(1600/1280).</summary>
        public static float K => Compact ? 1.25f : 1f;
        public static float C(float pc, float phone) => Compact ? phone : pc;

        /// <summary>지금 화면의 반폭 · 반높이(월드).</summary>
        public static float HalfW { get { var c = Camera.main; return c != null ? c.orthographicSize * c.aspect : 8f; } }
        public static float HalfH { get { var c = Camera.main; return c != null ? c.orthographicSize : 4.5f; } }
        /// <summary>카메라 높이 — 16:9 보다 좁은 화면(16:10 · 4:3)은 폭 16 을 지키려고 위아래를 더 보여 준다.</summary>
        public static float CamSize(float aspect) => Mathf.Max(4.5f, 8f / Mathf.Max(0.5f, aspect));

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        public static Color A(Color c, float a) { c.a = a; return c; }

        // ── 글 ──
        /// <summary>제목 · 숫자 — Jua. 본문 — Noto Sans KR SemiBold(Res.Body).</summary>
        public static TextMeshPro Text(string name, Transform p, string text, Vector3 at, float size, int order, Color c,
                                       TextAlignmentOptions align = TextAlignmentOptions.Center, bool body = false, float width = 10f, float outline = 0f)
        {
            var t = Make.Text(name, p, text, at, size, order, c, align, width);
            if (body) { t.font = Res.Body; t.fontSize = size * 10f * 0.92f; }
            if (align == TextAlignmentOptions.Left || align == TextAlignmentOptions.TopLeft) t.rectTransform.pivot = new Vector2(0, align == TextAlignmentOptions.TopLeft ? 1 : 0.5f);
            else if (align == TextAlignmentOptions.Right) t.rectTransform.pivot = new Vector2(1, 0.5f);
            if (outline > 0) Make.Outline(t, outline, Outline);
            return t;
        }

        /// <summary>판 없이 배경 위에 얹는 글 — 아래로 떨어지는 부드러운 그림자(톤.md 의 「판 대신 그림자」).</summary>
        public static TextMeshPro Shadowed(TextMeshPro t)
        {
            Make.Outline(t, 0.18f, Outline);
            return t;
        }

        // ── 카드 효과 글 — 수치(숫자 · % · ×N)만 하늘색으로(태그 밖에서만). 「낱말」 금빛은 규칙 쪽 서식 그대로 ──
        static readonly System.Text.RegularExpressions.Regex num = new System.Text.RegularExpressions.Regex(@"(?<![#\w])([+\-]?\d+(?:\.\d+)?%?(?:\s?×\s?\d+)?)");
        /// <summary>신탁으로 바뀐 부분 표시(\u0001 … \u0002) — 연두. 안쪽의 다른 색(수치 · 「낱말」)은 연두가 이긴다.</summary>
        public const char DiffOn = '\u0001', DiffOff = '\u0002';
        public const string ChangedTag = "#9BE564";
        /// <summary>다음 카드 강화로 커진 수치(\u0003 ... \u0004) — 주황. 신탁 연두 · 수치 하늘색과 다른 색. 연두 안에서는 무시한다.</summary>
        public const char EmpOn = '\u0003', EmpOff = '\u0004';
        public const string EmpTag = "#FF9A52";
        public static readonly Color Emp = Hex("FF7A1F");
        public static string StripDiff(string s) => string.IsNullOrEmpty(s) ? s : s.Replace(DiffOn.ToString(), "").Replace(DiffOff.ToString(), "").Replace(EmpOn.ToString(), "").Replace(EmpOff.ToString(), "");
        public static string CardText(string rich)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            var sb = new System.Text.StringBuilder(rich.Length + 64);
            int i = 0; bool green = false, hot = false; int skipClose = 0;
            while (i < rich.Length)
            {
                int lt = rich.IndexOf('<', i);
                string plain = lt < 0 ? rich.Substring(i) : rich.Substring(i, lt - i);
                // 평문 조각 안의 표시 문자로 나눠 처리
                int p = 0;
                while (p < plain.Length)
                {
                    int m = plain.IndexOfAny(new[] { DiffOn, DiffOff, EmpOn, EmpOff }, p);
                    string seg = m < 0 ? plain.Substring(p) : plain.Substring(p, m - p);
                    sb.Append(green || hot ? seg : num.Replace(seg, "<color=" + SkyTag + ">$1</color>"));
                    if (m < 0) break;
                    if (plain[m] == DiffOn && !green && !hot) { green = true; skipClose = 0; sb.Append("<color=" + ChangedTag + ">"); }
                    else if (plain[m] == DiffOff && green) { green = false; sb.Append("</color>"); }
                    else if (plain[m] == EmpOn && !green && !hot) { hot = true; sb.Append("<color=" + EmpTag + ">"); }
                    else if (plain[m] == EmpOff && hot) { hot = false; sb.Append("</color>"); }
                    p = m + 1;
                }
                if (lt < 0) break;
                int gt = rich.IndexOf('>', lt);
                if (gt < 0) { sb.Append(rich.Substring(lt)); break; }
                string tag = rich.Substring(lt, gt - lt + 1);
                if (green && tag.StartsWith("<color=")) skipClose++;
                else if (green && tag == "</color>" && skipClose > 0) skipClose--;
                else sb.Append(tag);
                i = gt + 1;
            }
            return sb.ToString();
        }

        /// <summary>키워드 태그 줄 — 「[ 회수 / 소멸 ]」(없으면 빈 글).</summary>
        public static string TagLine(System.Collections.Generic.List<string> tags)
        {
            if (tags == null || tags.Count == 0) return "";
            var l = tags.FindAll(t => !string.IsNullOrEmpty(t) && t != "사용 불가");
            return l.Count == 0 ? "" : "[ " + string.Join(" / ", l) + " ]";
        }

        // ── 둥근 게이지 링(Bolzena/Ring) ──
        public static Ring Ring(string name, Transform p, Vector3 at, float d, float thick, int order, Color fill, Color back)
        {
            var mat = Res.NewMat("Bolzena/Ring");
            var mr = Make.Quad(name, p, at, new Vector2(d, d), mat, order);
            var r = mr.gameObject.AddComponent<Ring>();
            r.Init(mat, d, thick, fill, back);
            return r;
        }

        // ── 동그란 얼굴 — 사각 얼굴 그림을 원(SpriteMask)으로 오린다 ──
        public static SpriteRenderer RoundFace(string name, Transform p, string heroKey, Vector3 at, float d, int order)
        {
            var node = Make.Node(name, p, at);
            var sp = CardView.Face(heroKey, 1f);
            var mask = node.gameObject.AddComponent<SpriteMask>();
            mask.sprite = Res.UI("circle");
            mask.isCustomRangeActive = true;
            mask.frontSortingOrder = order + 1;
            mask.backSortingOrder = order - 1;
            var b = mask.sprite.bounds.size;
            node.localScale = new Vector3(d / b.x, d / b.y, 1);
            if (sp == null) return null;
            var f = Make.Sprite("face", node, sp, Vector3.zero, order);
            var fb = sp.bounds.size;
            // 원보다 살짝 크게(가장자리 빈틈 없이) — 부모 배율을 되돌려 월드 d 에 맞춘다
            f.transform.localScale = new Vector3(d * 1.02f / fb.x / node.localScale.x, d * 1.02f / fb.y / node.localScale.y, 1);
            f.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return f;
        }
    }

    // 고리 하나 — 채운 몫 · 색을 바꾼다(재질은 고리마다 하나)
    public class Ring : MonoBehaviour
    {
        Material mat;
        float d, thick;
        public float Fill { get; private set; } = -1;
        public MeshRenderer Renderer { get; private set; }
        static readonly int FillId = Shader.PropertyToID("_Fill"), ColorId = Shader.PropertyToID("_Color"), BackId = Shader.PropertyToID("_Back"),
            InnerId = Shader.PropertyToID("_Inner"), OuterId = Shader.PropertyToID("_Outer"), BoostId = Shader.PropertyToID("_Boost");

        public void Init(Material m, float diameter, float thickness, Color fill, Color back)
        {
            mat = m; d = diameter; thick = thickness;
            Renderer = GetComponent<MeshRenderer>();
            mat.SetFloat(OuterId, 0.5f);
            mat.SetFloat(InnerId, Mathf.Max(0, 0.5f - thick / d));
            SetColors(fill, back);
            Set(0);
        }

        public void Set(float f)
        {
            f = Mathf.Clamp01(f);
            if (Mathf.Abs(f - Fill) < 0.0005f) return;
            Fill = f;
            mat.SetFloat(FillId, f);
        }

        public void SetColors(Color fill, Color back, float boost = 1f)
        {
            mat.SetColor(ColorId, fill);
            mat.SetColor(BackId, back);
            mat.SetFloat(BoostId, boost);
        }

        void OnDestroy() { if (mat) Destroy(mat); }
    }
}
