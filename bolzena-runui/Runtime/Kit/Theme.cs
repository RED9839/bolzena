using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 판 화면의 색 · 글꼴 · 그림 — 어두운 남색 판 + 얇은 금빛 테두리 · Jua 글꼴(값의 근거와 쓰임은 Docs/톤.md).
    // 그림은 Resources 에서 Texture2D 로 읽어 런타임에 Sprite 를 만든다(9칸 테두리도 여기서) — 가져오기 설정에 기대지 않는다.
    public static class Theme
    {
        // ── 색 ── 판 화면 톤(Docs/톤.md) — 어두운 남색 판 + 얇은 금빛 테두리. 전투 화면도 같은 값을 쓴다
        public static readonly Color Ink = Hex("EEF1FA");          // 본문 글
        public static readonly Color Sub = Hex("A7B1CC");          // 덜 중요한 글(부제 · 설명)
        public static readonly Color Dim = Hex("6B7591");          // 잠긴 · 빈 칸 글
        public static readonly Color Gold = Hex("F2CF7A");         // 강조 글 · 고른 테두리 · 갈 수 있는 칸
        public static readonly Color GoldDeep = Hex("D9A441");
        public static readonly Color Edge = Hex("C9A35A");         // 판 테두리 금(얇게)
        public static readonly Color Good = Hex("6EE0A0");
        public static readonly Color Bad = Hex("FF7A86");
        public static readonly Color Sky = Hex("78C6FF");
        public static readonly Color Brown = Hex("2A1A05");        // 금빛 버튼 위 글
        public static readonly Color Night = Hex("070A14");        // 맨 뒤 바탕
        public static readonly Color NavyPanel = Hex("101830");    // 판(평균) — 그림 없이 칠할 때
        public static readonly Color NavyCell = Hex("1A2340");     // 칸(평균)
        public static readonly Color NavyWell = Hex("0A0F1C");     // 판 속 우물(그림 창 · 막대 바탕)
        public static readonly Color Line = new Color(1, 1, 1, 0.10f);   // 판 속 가는 나눔 선
        // 글 안에 쓰는 색(리치 텍스트)
        public const string GoldTag = "#F2CF7A", SubTag = "#A7B1CC", DimTag = "#6B7591", GoodTag = "#6EE0A0", BadTag = "#FF7A86";

        // ── 글자 크기 단계 ── 캔버스 단위(PC 1600×900 · 폰 1280×720 기준이라 폰에서는 저절로 1.25배 크게 보인다)
        public const float FsCap = 14, FsSm = 16, FsBody = 18, FsMd = 20, FsLg = 24, FsXl = 30, Fs2xl = 40, Fs3xl = 56, FsDisplay = 88;
        // ── 테두리 · 둥글기 ── 판 2 · 고른 칸 3 · 모서리 판 12 · 칸 10 · 알약 끝까지
        public const float EdgeW = 2, EdgeOnW = 3, RadPanel = 12, RadCell = 10;
        // ── 여백 ── 화면 가장자리 24 · 판 속 20 · 칸 사이 12 · 머리 띠 높이 84
        public const float Gutter = 24, PadIn = 20, Gap = 12, TopBand = 84;

        /// <summary>폰(가로) — 캔버스를 1280×720 기준으로 세워 글 · 버튼이 1.25배 크게 보인다. 화면을 세울 때 높이 맞춤에 쓴다.</summary>
        public static bool Compact { get; private set; }
        public static bool MeasureCompact()
        {
            float h = Screen.height, dpi = Screen.dpi;
            bool c = Application.isMobilePlatform || h <= 600 || (dpi > 0 && h / dpi < 3.0f)
                     || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-phone") >= 0;
            bool changed = c != Compact;
            Compact = c;
            return changed;
        }
        /// <summary>PC 값 · 폰 값 가운데 지금 것.</summary>
        public static float C(float pc, float phone) => Compact ? phone : pc;

        // 성격 색 — 웹판 lobby.js TONES
        public static readonly Dictionary<string, Color> Nature = new Dictionary<string, Color>
        {
            ["순수"] = Hex("a8d8b5"), ["광기"] = Hex("e89e94"), ["냉정"] = Hex("99cadc"),
            ["우울"] = Hex("bfb2e0"), ["활발"] = Hex("edce86"), ["공명"] = Hex("dbd2bb"),
        };
        public static Color NatureOf(string n) => n != null && Nature.TryGetValue(n, out var c) ? c : Sub;

        // 성격 색(진하게) — 카드 틀 · 편성 필터 칩. 원작 성격 아이콘의 색에 맞췄다(Docs/톤.md §1 「성격 색」). 전투 화면도 이 표를 쓴다
        public static readonly Dictionary<string, Color> NatureCard = new Dictionary<string, Color>
        {
            ["순수"] = Hex("4CB83A"), ["광기"] = Hex("E04848"), ["냉정"] = Hex("18C2E6"),
            ["우울"] = Hex("8A5CE6"), ["활발"] = Hex("E6C21A"), ["공명"] = Hex("F0A070"),
        };
        /// <summary>교주 카드 틀(금 · 중립) · 상태 · 저주 카드 틀(어두운 따로 색).</summary>
        public static readonly Color LeaderCard = Hex("D9A441"), StatusCard = Hex("5A4C72");
        public static Color NatureCardOf(string n) => n != null && NatureCard.TryGetValue(n, out var c) ? c : LeaderCard;

        public static readonly Dictionary<string, Color> Grade = new Dictionary<string, Color>
        {
            ["일반"] = new Color(0.8f, 0.8f, 0.85f), ["고급"] = new Color(0.5f, 0.85f, 1f),
            ["희귀"] = new Color(0.78f, 0.6f, 1f), ["전설"] = new Color(1f, 0.75f, 0.3f),
        };
        public static Color GradeOf(string g) => g != null && Grade.TryGetValue(g, out var c) ? c : Sub;

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        public static Color A(this Color c, float a) { c.a = a; return c; }

        // ── 글꼴 ── 제목 · 버튼은 Jua(시범과 같은 글꼴), 본문은 Noto Sans KR. Jua 에 없는 글자(「」 · — 따위)는 Noto 로 떨어진다
        static TMP_FontAsset title, body;
        public static TMP_FontAsset Title => title ??= MakeFont("Jua-Regular", true);
        public static TMP_FontAsset Body => body ??= MakeFont("NotoSansKR-SemiBold-sub", false);

        static TMP_FontAsset MakeFont(string file, bool withFallback)
        {
            var ttf = Resources.Load<Font>("RunUI/Fonts/" + file);
            var fa = TMP_FontAsset.CreateFontAsset(ttf, 64, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, true);
            fa.name = file + " SDF (runtime)";
            var sh = Shader.Find("TextMeshPro/Distance Field");
            if (sh != null) fa.material.shader = sh;
            if (withFallback) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { Body };
            return fa;
        }

        // ── 그림 ──
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        /// <summary>패키지의 화면 그림(Tools~/make_ui.py) — border 를 주면 9칸으로 늘린다.</summary>
        public static Sprite S(string name, float border = 0) => Load("RunUI/Sprites/" + name, border);

        /// <summary>원작 그림(시험 프로젝트 Resources/RunArt — Tools~/copy_assets.py). 없으면 null.</summary>
        public static Sprite Art(string path) => Load("RunArt/" + path, 0, quiet: true);
        public static Sprite Bg(string name) => Art("Bg/" + name);
        public static Sprite HeroIcon(string art) => art == null ? null : Art("Heroes/" + art);
        public static Sprite Icon(string name) => Art("Icons/" + name);

        static Sprite Load(string path, float border, bool quiet = false)
        {
            string key = path + "|" + border;
            if (sprites.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                if (!quiet) Debug.LogWarning("[RunUI] 그림 없음: " + path);
                sprites[key] = null;
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            var b = new Vector4(border, border, border, border);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, b);
            s.name = path;
            sprites[key] = s;
            return s;
        }

        // 판 종류 → (그림, 9칸 테두리)
        public static Sprite Panel => S("panel", 24);
        public static Sprite Cell => S("cell", 20);
        public static Sprite CellOn => S("cell_on", 20);
        public static Sprite Pill => S("pill", 46);
        public static Sprite Round => S("round", 20);
        public static Sprite Paper => S("paper", 24);
        public static Sprite White => S("white");
        public static Sprite Glass => S("glass", 14);
        public static Sprite GlassOn => S("glass_on", 14);
        public static Sprite GlassDim => S("glass_dim", 14);
        public static Sprite Frame => S("frame", 24);
        public static Sprite FramePill => S("frame_pill", 46);
        public static readonly Color Rose = Hex("E8566A");
    }
}
