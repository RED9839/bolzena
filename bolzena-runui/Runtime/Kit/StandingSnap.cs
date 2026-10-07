using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bolzena.RunUI
{
    // 정지 스탠딩 그림 — 스탠딩 스파인(Normal · Idle_1 첫 프레임)을 그 자리에서 한 번 구워 Sprite 로(2026-10 사용자: 「목록은 움직이지 않는 그림으로 ·
    //   기본 카드 · 편성표 사도 그림도 수정한 크기로」). 자르기는 스탠딩 맞춤 표(standing_fit.json — 중심 centerX · 몸 꼭대기 topY · 머리 headY)로:
    //     · 얼굴(frac < 0.45 — 초상 줄 · 머리표): 그 사도의 머리(topY − headY)로 머리 · 어깨가 차게
    //     · 그 밖(목록 상반신 · 카드 그림 · 마을 공개): 모두 같은 배율(안 B — 원작 키 그대로) — 자르는 높이 = frac × 기준 몸(705 단위), 위 끝 = 몸 꼭대기 위 조금
    //   웹판 정지 렌더(RunArt/Standing)는 프레임 · 테두리 자르기가 사도마다 달라 표와 맞지 않는다(몸이 비거나 엉뚱한 곳이 잘림) — 표가 있는 사도는 이것만 쓴다.
    //   그리기: 캔버스 없이 SkeletonGraphic 메시를 CommandBuffer 로 RenderTexture 에 그리고(곧은 알파 재질 — 흰 테두리 없음) 읽어 곧은 알파 Sprite 로.
    //   한 번 구운 것은 (그림 키 · 비율 · frac · 크기)로 붙잡아 둔다. 굽고 나면 스켈레톤 데이터는 캐시에서 뺀다.
    //   굽기는 한 프레임에 둘까지(PerFrame) — 그보다 많이 부르면 빈(투명) 그림을 먼저 돌려주고 다음 프레임들에 그 텍스처에 채워 넣는다(대기열).
    //   웹은 관리 메모리 GC 가 프레임 끝에만 돌아, 한 프레임에 스탠딩 수십 명을 구우면 파싱한 스켈레톤 데이터(사도 하나 수십 MB 의 임시 메모리)가
    //   쌓여 WASM 힙이 2GB 를 넘었다(2026-10-06 「처음 사도 상세에 들어가면 오류」 abort("OOM") — 상세 초상 줄 135명 · 적 도감 보스 클론).
    public static class StandingSnap
    {
        static RectTransform host;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static float[] lin;
        static int baked;

        // ── 한 프레임 굽기 한도 · 대기열 ──
        const int PerFrame = 2;
        static int bakeFrame = -1, bakeInFrame;
        sealed class Job { public string Art; public Rect Src; public Texture2D Tex; }
        static readonly Queue<Job> jobs = new Queue<Job>();
        static readonly Dictionary<int, Color32[]> zeros = new Dictionary<int, Color32[]>();

        /// <summary>이 프레임에 더 구워도 되는가(한 프레임 둘까지 · 정리(UnloadUnusedAssets) 도는 동안은 쉰다).</summary>
        public static bool CanBakeNow
        {
            get
            {
                if (bakeFrame != Time.frameCount) { bakeFrame = Time.frameCount; bakeInFrame = 0; }
                return bakeInFrame < PerFrame && !SpineUi.Unloading;
            }
        }

        /// <summary>아직 굽지 않은 대기열 수(재기용).</summary>
        public static int Pending => jobs.Count;

        sealed class Pump : MonoBehaviour
        {
            void Update()
            {
                while (jobs.Count > 0 && CanBakeNow)
                {
                    var j = jobs.Dequeue();
                    if (j.Tex == null) continue;   // 그새 정리됨
                    if (Bake(j.Art, j.Src, j.Tex.width, j.Tex.height, j.Tex) == null) j.Tex.Apply(false, true);   // 못 구우면 빈 그림 그대로
                }
            }
        }

        /// <summary>얼굴이 아닌 자르기의 기준 몸(원본 단위) — frac 1 = 이 높이.</summary>
        public const float RefBody = 705f;

        static RectTransform Host
        {
            get
            {
                if (host != null) return host;
                var go = new GameObject("RunUI standing snap", typeof(RectTransform));
                if (Application.isPlaying) { go.AddComponent<Pump>(); Object.DontDestroyOnLoad(go); }
                else go.hideFlags = HideFlags.HideAndDontSave;   // 에디터 미리 굽기(SnapPrebake) — 장면에 남기지 않는다
                host = (RectTransform)go.transform;
                return host;
            }
        }

        /// <summary>지금까지 구운 수(재기용).</summary>
        public static int Baked => baked;

        // ── 미리 구운 그림(2026-10-07 「사도 목록 보는 게 너무 무겁다」) ──
        //   에디터가 빌드 때 사도마다 목록 카드(list) · 얼굴 칸(face — 비율 1 · frac 0.34) · 카드 그림(card) 셋을 이 같은 자르기 · Bake 로 구워
        //   Resources/RunArt/Snap/<kind>/<art>.png 에 두고(원작 파생 — 프로젝트의 Assets/Resources 는 git 밖), 목록 index.txt 에 그때의 비율 · frac · 자르기 사각형을 적는다.
        //   실행 중에는 그 작은 그림만 읽는다(스탠딩 스파인 파싱 · 굽기 0). 표(standing_fit · center_fix)나 자르기 값이 바뀌어 지금 사각형이 적힌 것과 다르면
        //   (또는 그림이 없으면) 예전처럼 그 자리에서 굽는다. 「-nosnap」 이면 미리 구운 그림을 쓰지 않는다(전후 비교).
        public const string SnapRoot = "RunArt/Snap";
        public static readonly bool NoSnap = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-nosnap") >= 0;
        public struct SnapEntry { public float Ratio, Frac; public Rect Src; }
        static Dictionary<string, SnapEntry> snapIndex;
        static int prebaked;
        /// <summary>미리 구운 그림을 읽은 수(재기용).</summary>
        public static int Prebaked => prebaked;

        static Dictionary<string, SnapEntry> SnapIndex
        {
            get
            {
                if (snapIndex != null) return snapIndex;
                snapIndex = new Dictionary<string, SnapEntry>();
                if (NoSnap) return snapIndex;
                var ta = Resources.Load<TextAsset>(SnapRoot + "/index");
                if (ta == null) return snapIndex;
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                foreach (var line in ta.text.Split('\n'))
                {
                    var p = line.Trim().Split('\t');
                    if (p.Length < 8 || p[0].StartsWith("#")) continue;
                    float F(int i) => float.Parse(p[i], inv);
                    try { snapIndex[p[0] + "|" + p[1]] = new SnapEntry { Ratio = F(2), Frac = F(3), Src = new Rect(F(4), F(5), F(6), F(7)) }; }
                    catch (System.FormatException) { }
                }
                Resources.UnloadAsset(ta);
                return snapIndex;
            }
        }

        /// <summary>이 부름이 미리 굽는 셋 가운데 어느 것인가 — list · face · card, 아니면 null.</summary>
        public static string SnapKind(float ratio, float frac, bool list, bool card)
            => card ? "card" : list ? "list" : Mathf.Abs(ratio - 1f) < 0.001f && Mathf.Abs(frac - 0.34f) < 0.001f ? "face" : null;

        /// <summary>그 사도 · 그 자르기의 미리 구운 그림이 지금 표와 맞는가(읽지 않고 답한다).</summary>
        public static bool HasPrebaked(string art, float ratio, float frac, bool list, bool card = false)
        {
            var kind = SnapKind(ratio, frac, list, card);
            if (kind == null || art == null || !SnapIndex.TryGetValue(kind + "|" + art, out var e)) return false;
            if (Mathf.Abs(ratio / e.Ratio - 1f) > 0.006f) return false;   // 비율이 거의 같아야(목록 카드 0.694~0.696 — 0.6% 안 늘림은 보이지 않는다)
            if (!CropRect(art, e.Ratio, e.Frac, out var r, kind == "list", kind == "card")) return false;
            const float tol = 0.05f;
            return Mathf.Abs(r.x - e.Src.x) < tol && Mathf.Abs(r.y - e.Src.y) < tol && Mathf.Abs(r.width - e.Src.width) < tol && Mathf.Abs(r.height - e.Src.height) < tol;
        }

        static Sprite LoadPrebaked(string art, string kind)
        {
            using var _h = Hitch.Span("미리 구운 그림 읽기");
            var tex = Resources.Load<Texture2D>(SnapRoot + "/" + kind + "/" + art);
            if (tex == null) return null;
            prebaked++;
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
            s.name = "snap " + kind + " " + art;
            return s;
        }

        /// <summary>미리 굽기용 — 이 사도 · 자르기의 index 줄(그림 키 뒤 비율 · frac · 사각형). 표에 없으면 null.</summary>
        public static string SnapLine(string kind, string art, float ratio, float frac)
        {
            if (!CropRect(art, ratio, frac, out var r, kind == "list", kind == "card")) return null;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return string.Join("\t", kind, art, ratio.ToString("R", inv), frac.ToString("R", inv), r.x.ToString("R", inv), r.y.ToString("R", inv), r.width.ToString("R", inv), r.height.ToString("R", inv));
        }

        /// <summary>에디터 미리 굽기가 끝나면 숨은 굽기 판을 치운다.</summary>
        public static void ResetHost() { if (host != null) Object.DestroyImmediate(host.gameObject); host = null; }

        /// <summary>사도 목록 카드(세로 칸)가 담는 몫 — 기준 몸(705)의 이만큼(모두 같은 배율 · 안 B). 머리 전체 + 어깨 · 가슴께.</summary>
        public const float ListFrac = 0.64f;

        /// <summary>
        /// 사도 목록 카드 자르기(2026-10-06 사용자: 「얼굴을 중심으로 하되 너무 줌하지 말고 전체적으로 잘 나오게」 — 예전 상반신 0.5 는 머리가 칸을 꽉 채워
        /// 정수리가 잘리고, 벨라처럼 머리 상자가 머리카락 · 장식까지 커 위 끝이 몸 꼭대기에 묶인 사도는 얼굴이 아래로 밀렸다).
        ///   높이 = ListFrac × 기준 몸 × 배율(모든 사도 같은 배율), 위 = 머리 둘레 위 끝(머리카락 · 귀 · 모자, 몸 꼭대기 위로 높이의 18% 까지 — 긴 뿔 · 장식은 잘림) + 여백 4%.
        ///   그다음 얼굴 상자 가운데가 위에서 24~44% 에 오게 위 끝을 민다(얼굴이 위 1/3 께) · 가로는 얼굴 쪽(상반신과 같은 규칙).
        /// </summary>
        static bool ListRect(StandingFit.Fit f, float ratio, out Rect r)
        {
            var q = ListTune;
            float h = q[0] * RefBody * f.Scale;
            float faceY = f.FaceY != 0 ? f.FaceY : f.HasHead ? f.FaceBox.center.y : f.HeadY;
            float headTop = f.HasHead ? f.HeadBox.yMax : f.HairTop;
            float top = Mathf.Min(headTop, f.TopY + h * q[1]) + h * 0.04f;
            top = Mathf.Clamp(top, faceY + h * q[3], faceY + h * q[2]);
            if (f.ListTop != 0) top = f.ListTop;   // 손보정(standing_center_fix.json listTop — 머리 상자가 머리카락을 다 못 담은 사도)
            float w = h * ratio;
            float cx = f.HasHead ? StandingFit.CropCenterX(f, StandMode.Upper, false) : f.CenterX;
            if (f.HasHead)
            {
                float m = w * 0.06f;
                if (f.FaceBox.xMin < cx - w / 2 + m) cx = f.FaceBox.xMin + w / 2 - m;
                else if (f.FaceBox.xMax > cx + w / 2 - m) cx = f.FaceBox.xMax - w / 2 + m;
            }
            r = new Rect(cx - w / 2, top - h, w, h);
            return true;
        }

        // 목록 자르기 값 — [몫, 몸 꼭대기 위 머리 한도, 얼굴 아래 한도, 얼굴 위 한도]. 점검용으로 「-listcrop 0.64,0.18,0.44,0.24」 처럼 바꿔 볼 수 있다
        static float[] listTune;
        static float[] ListTune
        {
            get
            {
                if (listTune != null) return listTune;
                listTune = new[] { ListFrac, 0.18f, 0.44f, 0.24f };
                var a = System.Environment.GetCommandLineArgs();
                int i = System.Array.IndexOf(a, "-listcrop");
                if (i >= 0 && i + 1 < a.Length)
                {
                    var p = a[i + 1].Split(',');
                    for (int j = 0; j < p.Length && j < 4; j++)
                        if (float.TryParse(p[j], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) listTune[j] = v;
                }
                return listTune;
            }
        }

        /// <summary>목록 카드 자르기를 끈다(전후 비교 — 「-oldcrop」 이면 예전 상반신 0.5).</summary>
        public static readonly bool OldListCrop = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldcrop") >= 0;

        // ── 카드 그림 자르기(CardRect) ──
        /// <summary>카드 그림 창 비율(가로/세로) — 판 화면 카드(W.Card 196×276 = 0.710)와 전투 카드(CardView 1.83×2.63 = 0.696)가 같은 그림 하나를 쓴다(1.4% 늘림 — 굽기 · 메모리 한 벌).</summary>
        public const float CardRatio = 0.70f;
        /// <summary>카드 자르기를 끈다(전후 비교 — 「-oldcardcrop」 이면 예전 frac 0.56 상반신).</summary>
        public static readonly bool OldCardCrop = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldcardcrop") >= 0;

        /// <summary>
        /// 카드 그림 자르기(2026-10-07 사용자: 「사도들 위에 글자랑 공격 · 스킬 이 글자에 안 가리게」 — 예전 frac 0.56 은 머리가 카드 위 반을 채워
        /// 얼굴이 비용 · 이름 · 종류 알약 밑에 깔렸고, 벨라처럼 머리 상자가 큰 사도는 몸통이 나왔다).
        ///   안전 구역(카드 높이에 대한 위에서부터의 몫 — 전투 카드 CardView 와 판 화면 W.Card 둘 다 만족):
        ///     위 글 · 칩 끝 = 전투 26%(종류 알약 아래 끝 H/2−0.695 / 2.7) · 판 화면 19%(알약 아래 54/280)
        ///     아래 효과 판 위 끝 = 전투 확대 61%(태그 줄이 있을 때 장식 선 −H/2+1.06) · 판 화면 기본 카드 67~74%(글 2~3줄)
        ///     → 얼굴 상자(눈 · 입 · 앞머리)가 26~61% 안에, 얼굴 가운데가 45% 께(얼굴 상자 가운데는 눈보다 조금 아래 — 눈이 알약 바로 밑에 붙지 않게).
        ///   높이 = CardTune[0] × 기준 몸(705) × 배율(모두 같은 배율 — 예전 0.56 보다 덜 확대해 머리 전체 + 어깨 · 가슴께),
        ///   위 끝 = 얼굴 가운데 + 높이 × CardTune[1](얼굴 가운데가 위에서 그 몫). 머리 꼭대기(머리 둘레 위 끝 · 몸 꼭대기 위 18% 까지)가 잘리면
        ///   얼굴을 CardTune[2] 까지 내리고, 그래도 모자라면 높이를 늘린다. 얼굴 상자가 안전 구역(35%)보다 크면 높이를 늘린다.
        ///   가로는 얼굴 쪽(목록 카드와 같은 규칙). 손보정 standing_center_fix.json cardFaceY(카드용 얼굴 높이) · faceY · cardTop(위 끝)이 있으면 그것.
        /// </summary>
        static bool CardRect(StandingFit.Fit f, float ratio, out Rect r)
        {
            var q = CardTune;
            float h = q[0] * RefBody * f.Scale;
            float faceY = f.CardFaceY != 0 ? f.CardFaceY : f.FaceY != 0 ? f.FaceY : f.HasHead ? f.FaceBox.center.y : f.HeadY;   // 손보정 cardFaceY(얼굴 상자가 틀린 사도)
            float headTop = Mathf.Min(f.HasHead ? f.HeadBox.yMax : f.HairTop, f.TopY + h * q[3]);
            if (f.HasHead && f.FaceY == 0 && f.CardFaceY == 0) h = Mathf.Max(h, Mathf.Min(f.FaceBox.height / 0.40f, h * 1.2f));   // 얼굴 상자가 안전 구역(26~61%)에 들게(1.2배까지)
            float need = headTop - faceY;                                               // 얼굴 가운데 → 머리 꼭대기
            if (need > 0 && need + h * 0.03f > h * q[2]) h = need / (q[2] - 0.03f);      // 머리 꼭대기까지 담으려면 덜 확대
            float top = faceY + h * q[1];
            if (top < headTop + h * 0.03f) top = Mathf.Min(headTop + h * 0.03f, faceY + h * q[2]);
            if (f.CardTop != 0) top = f.CardTop;   // 손보정(standing_center_fix.json cardTop)
            float w = h * ratio;
            float cx = f.HasHead ? StandingFit.CropCenterX(f, StandMode.Upper, false) : f.CenterX;
            if (f.HasHead)
            {
                float m = w * 0.08f;
                if (f.FaceBox.xMin < cx - w / 2 + m) cx = f.FaceBox.xMin + w / 2 - m;
                else if (f.FaceBox.xMax > cx + w / 2 - m) cx = f.FaceBox.xMax - w / 2 + m;
            }
            r = new Rect(cx - w / 2, top - h, w, h);
            return true;
        }

        // 카드 자르기 값 — [몫, 얼굴 가운데 자리(위에서), 얼굴 가운데 아래 한도, 몸 꼭대기 위 머리 한도]. 점검용 「-cardcrop 0.88,0.45,0.50,0.18」
        static float[] cardTune;
        static float[] CardTune
        {
            get
            {
                if (cardTune != null) return cardTune;
                cardTune = new[] { 0.88f, 0.45f, 0.50f, 0.18f };
                var a = System.Environment.GetCommandLineArgs();
                int i = System.Array.IndexOf(a, "-cardcrop");
                if (i >= 0 && i + 1 < a.Length)
                {
                    var p = a[i + 1].Split(',');
                    for (int j = 0; j < p.Length && j < 4; j++)
                        if (float.TryParse(p[j], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) cardTune[j] = v;
                }
                return cardTune;
            }
        }

        /// <summary>카드 그림 자르기 사각형(원본 단위) — 시트 · 점검용.</summary>
        public static bool CardCropRect(string art, out Rect r) { r = default; return StandingFit.TryGet(art, out var f) && CardRect(f, CardRatio, out r); }

        /// <summary>카드 그림(시작 카드 · 생성 카드의 사도 스탠딩) — CardRatio 비율 · 640px. 표 · 스파인이 없으면 null.</summary>
        public static Sprite Card(string art) => OldCardCrop ? Upper(art, CardRatio, 0.56f) : Upper(art, CardRatio, 0f, 0, false, true);

        /// <summary>그 사도의 자르기 사각형(원본 단위, y 위로). 표에 없으면 false. list = 사도 목록 카드(frac 은 무시 — ListRect) · card = 카드 그림(CardRect).</summary>
        public static bool CropRect(string art, float ratio, float frac, out Rect r, bool list = false, bool card = false)
        {
            r = default;
            if (!StandingFit.TryGet(art, out var f)) return false;
            if (card) return CardRect(f, ratio, out r);
            if (list && !OldListCrop) return ListRect(f, ratio, out r);
            if (list) frac = 0.5f;
            float h, top;
            bool face = frac < 0.45f;
            if (face && f.HasHead && !OldFaceCrop) return FaceRect(f, ratio, frac, out r);
            if (f.HasHead)
            {
                // 머리 상자 기준(standing_head.json) — 숙이거나 기운 포즈도 얼굴이 창 안에: 위 = 머리 둘레 상자 위 끝(머리카락 · 귀 · 뿔, 장신구 뺌),
                //   가로 = 얼굴 쪽(얼굴 칸은 얼굴 가운데 · 상반신은 몸과 얼굴 사이 얼굴 쪽 7할), 그래도 얼굴 상자가 옆으로 나가면 민다
                float R = Mathf.Clamp(f.TopY - f.HeadY, 100, 360);   // 머리 반지름은 몸 꼭대기(뿔 · 귀 뺌)까지 — 사슴뿔 · 큰 뿔이 얼굴을 밀어내지 않게
                h = face ? R * 1.35f * (frac / 0.34f) : frac * RefBody * f.Scale;
                // 위 = 머리 둘레 상자 위 끝(머리카락 · 귀 · 뿔)까지, 다만 몸 꼭대기 위로 창 높이의 8%(얼굴 칸 4%)까지만 — 뿔이 길면 뿔 끝이 잘린다(얼굴이 먼저)
                top = Mathf.Min(f.HeadBox.yMax, f.TopY + h * (face ? 0.04f : 0.08f)) + h * 0.03f;
                if (face && f.FaceY != 0) top = f.FaceY + h * 0.55f;   // 얼굴이 몸 위 끝이 아닌 사도(꿀벌 쥬비) — 얼굴을 칸 가운데 조금 위에
                float w0 = h * ratio;
                float cx = StandingFit.CropCenterX(f, StandMode.Upper, face);
                float m = w0 * 0.04f;
                if (f.FaceBox.xMin < cx - w0 / 2 + m) cx = f.FaceBox.xMin + w0 / 2 - m;
                else if (f.FaceBox.xMax > cx + w0 / 2 - m) cx = f.FaceBox.xMax - w0 / 2 + m;
                r = new Rect(cx - w0 / 2, top - h, w0, h);
                return true;
            }
            if (face)
            {
                float head = Mathf.Clamp(f.TopY - f.HeadY, 120, 320);
                h = head * (frac / 0.34f) * 1.55f;   // 머리 · 어깨(이등신이라 머리가 크다)
                top = f.TopY + h * 0.05f;
            }
            else
            {
                h = frac * RefBody * f.Scale;
                top = f.TopY + h * 0.06f;
            }
            // (머리 상자가 없는 사도) 머리카락 위 끝까지 담되, 상반신 창에서 머리 뼈가 창 위 62% 아래로 내려가지는 않게
            top = Mathf.Max(top, f.HairTop + h * 0.03f);
            if (!face) top = Mathf.Min(top, Mathf.Max(f.TopY + h * 0.02f, f.HeadY + h * 0.62f));
            float w = h * ratio;
            r = new Rect(f.CenterX - w / 2, top - h, w, h);
            return true;
        }

        // ── 얼굴 칸 자르기(FaceRect) ──
        /// <summary>얼굴 칸 자르기를 끈다(전후 비교 — 「-oldface」 이면 예전 머리 반지름 × 1.35).</summary>
        public static readonly bool OldFaceCrop = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldface") >= 0;

        /// <summary>
        /// 얼굴 칸 자르기(사도 상세 초상 줄 · 편성 머리표 · 파티 HUD · 덱 칩 — frac 0.34). 2026-10-07 사용자: 벨라 · 아일라 · 마에스트로 2호 · 빅우드 · 쥬비
        /// 「너무 확대되어 있다」 — 예전엔 높이 = 머리 반지름(몸 꼭대기 − 머리 뼈) × 1.35 라 턱 · 머리 위가 잘리고 얼굴만 칸을 꽉 채웠다.
        ///   이제: 머리 위 끝(머리 둘레 상자 위 끝, 몸 꼭대기 위로는 머리 키의 FaceTune[3] 까지 — 긴 뿔 · 귀 · 장식은 잘림) ~ 턱(얼굴 상자 아래 끝)의
        ///   머리 키에 FaceTune[0] 을 곱한 높이로, 위 끝은 머리 위 끝 + 높이 × FaceTune[1] — 머리 전체 + 목 · 어깨 조금.
        ///   얼굴 상자 가운데가 위에서 FaceTune[2] 보다 아래로 내려가면(머리카락 · 모자가 큰 사도) 위 끝을 내려 얼굴을 올린다.
        ///   손보정(standing_center_fix.json): iconFaceY = 얼굴 칸의 얼굴 가운데 높이(얼굴이 위에서 FaceTune[2] − 0.05 자리에) · iconScale = 높이 배수 ·
        ///   cardFaceY(카드 그림용 얼굴 높이) · faceY(쥬비 — 얼굴이 몸 위 끝이 아닌 사도)도 같은 자리에. 가로는 얼굴 가운데(옆으로 나가면 민다).
        ///   2026-10-07 사용자 확정: 「얼굴 잘 나오게」 — 얼굴이 가운데쯤, 머리 전체 + 어깨 살짝, 모자 · 뿔 · 큰 머리장식은 잘려도 얼굴이 작아지지 않게.
        /// </summary>
        static bool FaceRect(StandingFit.Fit f, float ratio, float frac, out Rect r)
        {
            var q = FaceTune;
            float chin = f.FaceBox.yMin;
            float rawTop = f.HeadBox.yMax;
            float headH = Mathf.Max(120f, Mathf.Min(rawTop, f.TopY + 60f) - chin);
            float headTop = Mathf.Min(rawTop, f.TopY + headH * q[3]);
            headH = Mathf.Clamp(headTop - chin, 120f, 520f);
            float h = headH * q[0] * (f.IconScale > 0 ? f.IconScale : 1f) * (frac / 0.34f);
            float top = headTop + h * q[1];
            float faceC = IconFace(f, out bool manual);
            if (manual) top = faceC + h * (q[2] - 0.05f);
            else if (top - faceC > h * q[2]) top = faceC + h * q[2];
            float w = h * ratio;
            float cx = StandingFit.CropCenterX(f, StandMode.Upper, true);
            float m = w * 0.06f;
            if (f.FaceBox.xMin < cx - w / 2 + m) cx = f.FaceBox.xMin + w / 2 - m;
            else if (f.FaceBox.xMax > cx + w / 2 - m) cx = f.FaceBox.xMax - w / 2 + m;
            r = new Rect(cx - w / 2, top - h, w, h);
            return true;
        }

        /// <summary>얼굴 칸이 가운데에 둘 얼굴 높이 — 손보정 iconFaceY · cardFaceY(카드 그림에서 얼굴 상자가 틀렸다고 고친 사도) · faceY, 없으면 얼굴 상자 가운데. manual = 손보정.</summary>
        public static float IconFace(StandingFit.Fit f, out bool manual)
        {
            manual = f.IconFaceY != 0 || f.CardFaceY != 0 || f.FaceY != 0;
            return f.IconFaceY != 0 ? f.IconFaceY : f.CardFaceY != 0 ? f.CardFaceY : f.FaceY != 0 ? f.FaceY : f.FaceBox.center.y;
        }

        // 얼굴 칸 자르기 값 — [머리 키 배수, 머리 위 여백, 얼굴 가운데 아래 한도(위에서), 몸 꼭대기 위 머리 한도(머리 키 몫)]. 점검용 「-facecrop 1.3,0.05,0.48,0.12」
        static float[] faceTune;
        static float[] FaceTune
        {
            get
            {
                if (faceTune != null) return faceTune;
                faceTune = new[] { 1.3f, 0.05f, 0.48f, 0.12f };
                var a = System.Environment.GetCommandLineArgs();
                int i = System.Array.IndexOf(a, "-facecrop");
                if (i >= 0 && i + 1 < a.Length)
                {
                    var p = a[i + 1].Split(',');
                    for (int j = 0; j < p.Length && j < 4; j++)
                        if (float.TryParse(p[j], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) faceTune[j] = v;
                }
                return faceTune;
            }
        }

        /// <summary>얼굴 칸 자르기 사각형(원본 단위) — 시트 · 점검용. old = 예전 자르기(머리 반지름 × 1.35).</summary>
        public static bool FaceCropRect(string art, bool old, out Rect r)
        {
            r = default;
            if (!StandingFit.TryGet(art, out var f)) return false;
            if (!old && f.HasHead) return FaceRect(f, 1f, 0.34f, out r);
            float R = Mathf.Clamp(f.TopY - f.HeadY, 100, 360), h = R * 1.35f;
            float top = Mathf.Min(f.HeadBox.yMax, f.TopY + h * 0.04f) + h * 0.03f;
            if (f.FaceY != 0) top = f.FaceY + h * 0.55f;
            float cx = StandingFit.CropCenterX(f, StandMode.Upper, true), m = h * 0.04f;
            if (f.FaceBox.xMin < cx - h / 2 + m) cx = f.FaceBox.xMin + h / 2 - m;
            else if (f.FaceBox.xMax > cx + h / 2 - m) cx = f.FaceBox.xMax - h / 2 + m;
            r = new Rect(cx - h / 2, top - h, h, h);
            return f.HasHead;
        }

        /// <summary>
        /// 위쪽 자르기 Sprite — ratio = 가로/세로, frac = 얼마나 담을지(CardArt.Upper 와 같은 뜻 — 0.34 얼굴 · 0.5 상반신). card = 카드 그림(CardRect — frac 무시).
        /// pxH = 구울 세로 픽셀(0 이면 얼굴 224 · 그 밖 640). 표 · 스파인이 없으면 null(부르는 쪽이 웹판 렌더로 대신).
        /// </summary>
        public static Sprite Upper(string art, float ratio, float frac, int pxH = 0, bool list = false, bool card = false)
        {
            if (string.IsNullOrEmpty(art) || ratio <= 0) return null;
            // 미리 구운 그림(목록 카드 · 얼굴 칸 · 카드 그림)이 지금 표와 맞으면 그것 — 픽셀 크기는 미리 구운 것 하나(화면이 늘이고 줄인다)
            var kind = SnapKind(ratio, frac, list, card);
            if (kind != null)
            {
                string pk = art + "|pre|" + kind;
                if (cache.TryGetValue(pk, out var ps) && ps != null && ps.texture != null) return ps;
                if (HasPrebaked(art, ratio, frac, list, card) && (ps = LoadPrebaked(art, kind)) != null)
                {
                    if (cache.Count >= 320) cache.Clear();
                    cache[pk] = ps;
                    return ps;
                }
            }
            if (pxH <= 0) pxH = frac < 0.45f && !list && !card ? 224 : 640;
            string key = $"{art}|{ratio:F3}|{(card ? "card" : list ? "list" : frac.ToString("F2"))}|{pxH}";
            if (cache.TryGetValue(key, out var s) && (s == null || s.texture != null)) return s;
            s = null;
            if (CropRect(art, ratio, frac, out var src, list, card) && SpineUi.StraightMat != null && SystemInfo.supportsRenderTextures)
            {
                int pxW = Mathf.Max(8, Mathf.RoundToInt(pxH * ratio));
                Texture2D tex;
                if (CanBakeNow) tex = Bake(art, src, pxW, pxH);
                else
                {
                    // 이 프레임 한도를 넘었다 — 빈 그림을 먼저 주고 대기열에서 채운다(같은 텍스처라 화면이 쥔 Sprite 에 그대로 나타난다)
                    tex = NewTex(art, pxW, pxH);
                    if (!zeros.TryGetValue(pxW * pxH, out var z)) { z = new Color32[pxW * pxH]; if (zeros.Count > 16) zeros.Clear(); zeros[pxW * pxH] = z; }
                    tex.SetPixels32(z);
                    tex.Apply(false, false);
                    jobs.Enqueue(new Job { Art = art, Src = src, Tex = tex });
                    _ = Host;   // 대기열을 돌릴 Pump
                }
                if (tex != null)
                {
                    s = Sprite.Create(tex, new Rect(0, 0, pxW, pxH), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
                    s.name = "snap " + art;
                }
            }
            if (cache.Count >= 320) cache.Clear();   // 상한(목록 135 + 초상 줄 135 가 다 들게) — 띄운 것은 화면이 쥐어 살고, 나머지 구운 그림은 다음 정리(UnloadUnusedAssets) 때 풀린다
            cache[key] = s;
            return s;
        }

        static Texture2D NewTex(string art, int w, int h)
            => new Texture2D(w, h, TextureFormat.RGBA32, false, false) { name = "snap " + art, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

        /// <summary>스탠딩 스파인을 src(원본 단위)만큼 w×h 픽셀 Texture2D(곧은 알파 · sRGB)로 굽는다(into 가 있으면 거기에 — 읽을 수 있는 같은 크기). 스파인이 없으면 null.</summary>
        public static Texture2D Bake(string art, Rect src, int w, int h, Texture2D into = null, bool readable = false)
        {
            var mat0 = SpineUi.StraightMat;
            if (mat0 == null || !SystemInfo.supportsRenderTextures) return null;
            using var _h = Hitch.Span("스탠딩 굽기");
            _ = CanBakeNow;   // 프레임 셈 맞추기
            bakeInFrame++;
            SkeletonGraphic g = SpineUi.NewStanding(Host, art);
            if (g == null) return null;
            RenderTexture rt = null;
            Material mat = null;
            try
            {
                g.Update(0);
                g.UpdateMesh();
                var mesh = g.GetLastMesh();
                var mainTex = g.mainTexture;
                if (mesh == null || mainTex == null) return null;
                // 메시 꼭짓점 = 원본 단위 × 데이터 배율 × meshScale(캔버스 없음 = 100)
                float u = (g.skeletonDataAsset != null ? g.skeletonDataAsset.scale : 0.01f) * g.MeshScale;
                mat = new Material(mat0) { mainTexture = mainTex };
                mat.SetVector("_ClipRect", new Vector4(-1e7f, -1e7f, 1e7f, 1e7f));
                mat.SetVector("_TextureSampleAdd", Vector4.zero);
                mat.SetColor("_Color", Color.white);
                rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.filterMode = FilterMode.Bilinear;
                var cb = new CommandBuffer { name = "RunUI standing snap" };
                cb.SetRenderTarget(rt);
                cb.ClearRenderTarget(false, true, Color.clear);
                var proj = Matrix4x4.Ortho(src.xMin * u, src.xMax * u, src.yMin * u, src.yMax * u, -1000f, 1000f);
                cb.SetViewProjectionMatrices(Matrix4x4.identity, proj);
                for (int sm = 0; sm < Mathf.Max(1, mesh.subMeshCount); sm++) cb.DrawMesh(mesh, Matrix4x4.identity, mat, sm, 0);
                Graphics.ExecuteCommandBuffer(cb);
                cb.Release();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = into != null ? into : NewTex(art, w, h);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                RenderTexture.active = prev;
                Unpremultiply(tex);
                tex.Apply(false, !readable);   // 읽기 전용(시스템 메모리 사본 버림) — 미리 굽기는 PNG 로 쓰려고 읽을 수 있게 둔다
                return tex;
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (mat != null) { if (Application.isPlaying) Object.Destroy(mat); else Object.DestroyImmediate(mat); }
                // 스켈레톤 데이터는 무겁다(사도 하나 수 MB) — 굽자마자 놓는다: 바로 지워(DestroyImmediate) 스탠딩 수를 줄이면,
                //   이 사도를 띄운 화면이 따로 없을 때 SpineUi 가 파싱한 데이터를 비우고 12명마다 UnloadUnusedAssets 를 돌린다
                Object.DestroyImmediate(g.gameObject);
                baked++;
            }
        }

        // 그린 결과는 PMA(선형 공간에서 곱함 → sRGB 로 저장) — UI 기본 재질용 곧은 알파로: srgb(lin(c) / a)
        static void Unpremultiply(Texture2D t)
        {
            if (lin == null)
            {
                lin = new float[256];
                for (int i = 0; i < 256; i++) lin[i] = QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.GammaToLinearSpace(i / 255f) : i / 255f;
            }
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a == 0) { px[i] = new Color32(0, 0, 0, 0); continue; }
                if (c.a == 255) continue;
                float a = c.a / 255f;
                byte F(byte v)
                {
                    float x = Mathf.Min(1f, lin[v] / a);
                    if (linear) x = Mathf.LinearToGammaSpace(x);
                    return (byte)Mathf.Clamp(Mathf.RoundToInt(x * 255f), 0, 255);
                }
                px[i] = new Color32(F(c.r), F(c.g), F(c.b), c.a);
            }
            t.SetPixels32(px);
        }
    }
}
