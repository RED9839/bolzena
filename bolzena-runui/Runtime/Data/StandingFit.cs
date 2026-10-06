using System;
using System.Collections.Generic;
using System.Globalization;
using Spine.Unity;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>스탠딩을 칸에 세우는 방식 — Full = 전신(발이 바닥선) · Knee = 무릎께부터(편성 큰 카드) · Upper = 상반신(목록 · 주인 고르기 · 컷인).</summary>
    public enum StandMode { Full, Knee, Upper }

    // 스탠딩 맞춤(2026-10-05 「스탠딩 측정」 표 — Resources/RunUI/standing_fit.json, 규칙 Docs/스탠딩맞춤.md).
    //   사도마다 Normal · Idle_1 첫 프레임에서 잰 바닥(footY) · 중심(centerX) · 머리 뼈(headY) · 몸 꼭대기(topY) — 스켈레톤 원본 단위(배율 1, y 위로).
    //   크기는 사용자 결정 「안 B — 설정 키(스탠딩 원래 비율)」: standing_fit_height.json 의 scaleB(모두 1 — 원작 스탠딩을 같은 배율로).
    //     리코타 · 아야 · 실라 · 블랑셰가 크고 아이시아 · 요미 · 키디언 · 실비아가 작다(최대 1:1.65) — 작은 사도는 작게 보이는 게 의도.
    //   세우기: 같은 화면의 사도는 모두 같은 배율 k(원본 단위 1 당 px)와 같은 바닥선에 선다. k 는 「기준 키」(tallest — 그 화면에서 가장 큰 사도의 몸 키,
    //     안 주면 표 전체의 95% 값)가 칸에 들어가게 정한다 — Full: 칸 높이의 92% · Knee(무릎께): 칸 높이의 1.2배(위 6% 여백) · Upper(상반신): 1.7배.
    //     원점 x = 칸 가운데 − centerX × k, 원점 y = 바닥선 − footY × k. 기준보다 큰 사도(괴물 · 거목)만 칸에 맞게 줄인다.
    //   옛 손보정 표(standing_scale.json · 다리 기준 재기)는 이 표로 바뀌었다 — 표에 없는 사도만 그 자리에서 재는 값(Measure)으로 세운다.
    //
    // 전투 쪽(컷인)도 같은 함수를 쓴다:
    //   StandingFit.Place(SkeletonGraphic g, string art, Rect slot, StandMode mode, float tallest = 0, float pad = 0) → k
    //   StandingFit.Place(RectTransform skeletonRt, float unitPx, string art, Rect slot, StandMode mode, float tallest = 0, float pad = 0) → k
    //   lift = 화면 이름("detail" · "lobby" — 스탠딩 전신 화면만, 이벤트는 사도가 전투 SD)이면 떠 있는 사도(standing_center_fix.json float)의 얼굴을 그 화면 기준 얼굴 높이(_faceRef)에 맞춘다
    //   StandingFit.Tallest(IEnumerable<string> arts) — 함께 보일 사도들의 기준 키 · StandingFit.TryGet(art, out Fit) · Has(art) · MedSt
    public static class StandingFit
    {
        /// <summary>표 한 줄(원본 단위). Scale = 쓰는 배율(안 B = scaleB, 모두 1), ScaleA = SD 기준 배율(안 A — 비교용).</summary>
        public struct Fit
        {
            public float Scale, ScaleA, FootY, CenterX, HeadY, TopY, LegY, HairTop;
            public Rect Bounds;   // 뺀 것(날개 · 무기 …) 포함 전체
            public string Hero;
            // 머리 상자(standing_head.json — Editor/StandHead.cs): 머리 뼈 · 머리 둘레 상자(머리 · 머리카락 · 얼굴, 장신구 뺌) · 얼굴 상자 · 얼굴 가운데 x
            public bool HasHead;
            public float HeadX, FaceCx;
            public float FaceY;   // 얼굴(눈 · 입) 가운데 높이 — 손보정에만(0 = 없음). 얼굴 칸 자르기가 이 높이를 가운데 조금 아래에 둔다
            public float ListTop; // 사도 목록 카드 위 끝 — 손보정에만(0 = 없음, StandingSnap.ListRect 가 머리 · 얼굴로 정한다)
            public float CardTop; // 카드 그림 위 끝 — 손보정에만(0 = 없음, StandingSnap.CardRect 가 머리 · 얼굴로 정한다)
            public float CardFaceY; // 카드 그림의 얼굴 가운데 높이 — 손보정에만(0 = 없음, 얼굴 상자 가운데)
            public float IconFaceY; // 얼굴 칸(초상 줄 · 머리표)의 얼굴 가운데 높이 — 손보정에만(0 = 없음, 얼굴 상자로 정한다)
            public float IconScale; // 얼굴 칸 자르기 높이 배수(1 보다 크면 덜 확대) — 손보정에만(0 = 1)
            public float Lift;      // 떠 있는 사도 표시(손보정 「float」 — 값은 기준 얼굴 높이가 없을 때만 쓰는 공중 높이). 전신에서 lift(화면 이름)를 주면 얼굴을 그 화면의 기준 얼굴 높이에
            public Rect HeadBox, FaceBox;
            public bool Bent;     // 숙이거나 기운 포즈(머리가 몸 가운데선에서 옆으로 · 옛 자르기에서 얼굴이 잘리던 사도)
            public string Link;   // 키를 맞춘 원판(standing_height_link.json)
            public float BodyH => TopY - FootY;
            /// <summary>배율을 곱한 몸 키(원본 단위) — 기준 키를 셀 때.</summary>
            public float Body => (TopY - FootY) * Scale;
        }

        static Dictionary<string, Fit> table;
        static readonly Dictionary<string, float> faceRef = new Dictionary<string, float>();   // 화면별 기준 얼굴 높이(바닥선 위, 원본 단위) — 떠 있는 사도를 맞출 때
        static float medSt = 630f, p95 = 760f;

        /// <summary>스탠딩 몸 키 중앙값(원본 단위) — 표의 _meta.med_st.</summary>
        public static float MedSt { get { Load(); return medSt; } }

        public static bool Has(string art) { Load(); return art != null && table.ContainsKey(art); }

        /// <summary>떠 있는 사도인가(손보정 float · -nolift 아님) — 상세 · 로비 전신이 얼굴을 기준 얼굴 높이에 맞추고 ClampInto 로 줄이지 않는다.</summary>
        public static bool Floats(string art) => !NoLift && TryGet(art, out var f) && f.Lift != 0;

        public static bool TryGet(string art, out Fit f)
        {
            Load();
            f = default;
            return art != null && table.TryGetValue(art, out f);
        }

        /// <summary>함께 보일 사도들 가운데 가장 큰 몸 키(원본 단위 · 배율 곱함). 비거나 표에 없으면 표 전체의 95% 값.</summary>
        public static float Tallest(IEnumerable<string> arts = null)
        {
            Load();
            float m = 0;
            if (arts != null) foreach (var a in arts) if (a != null && table.TryGetValue(a, out var f)) m = Mathf.Max(m, f.Body);
            return m > 0 ? m : p95;
        }

        static void Load()
        {
            if (table != null) return;
            table = new Dictionary<string, Fit>();
            var ta = Resources.Load<TextAsset>("RunUI/standing_fit");
            if (ta == null) { Debug.Log("[RunUI] standing_fit.json 없음 — 스탠딩은 그 자리에서 잰다"); return; }
            try
            {
                if (!(FitJson.Parse(ta.text) is Dictionary<string, object> root)) return;
                if (root.TryGetValue("_meta", out var mo) && mo is Dictionary<string, object> meta && Num(meta, "med_st") is float ms && ms > 0) medSt = ms;
                // 안 B 배율(설정 키) — 없으면 1
                Dictionary<string, object> hroot = null;
                var th = Resources.Load<TextAsset>("RunUI/standing_fit_height");
                if (th != null) hroot = FitJson.Parse(th.text) as Dictionary<string, object>;
                foreach (var kv in root)
                {
                    if (kv.Key.StartsWith("_") || !(kv.Value is Dictionary<string, object> o)) continue;
                    float? b = hroot != null && hroot.TryGetValue(kv.Key, out var ho) && ho is Dictionary<string, object> hd ? Num(hd, "scaleB") : null;
                    var f = new Fit
                    {
                        Scale = b ?? 1f,
                        ScaleA = Num(o, "scale") ?? 1f,
                        FootY = Num(o, "footY") ?? 0f,
                        CenterX = Num(o, "centerX") ?? 0f,
                        HeadY = Num(o, "headY") ?? 0f,
                        TopY = Num(o, "topY") ?? 0f,
                        LegY = Num(o, "legY") ?? 0f,
                        Hero = o.TryGetValue("hero", out var hn) ? hn as string : null,
                    };
                    f.HairTop = Num(o, "hairTop") ?? f.TopY;
                    if (o.TryGetValue("bounds", out var bo) && bo is List<object> bl && bl.Count == 4)
                        f.Bounds = Rect.MinMaxRect(Convert.ToSingle(bl[0]), Convert.ToSingle(bl[1]), Convert.ToSingle(bl[2]), Convert.ToSingle(bl[3]));
                    else f.Bounds = Rect.MinMaxRect(f.CenterX - 300, f.FootY, f.CenterX + 300, f.HairTop);
                    if (f.Scale <= 0) f.Scale = 1;
                    if (f.TopY <= f.FootY) f.TopY = f.FootY + medSt;
                    table[kv.Key] = f;
                }
                // 머리 상자 — 숙인 · 기운 포즈에서도 얼굴을 칸 안에(상반신 · 얼굴 칸 · 편성 큰 카드)
                bool noHead = Array.IndexOf(Environment.GetCommandLineArgs(), "-nohead") >= 0;   // 점검용 — 머리 상자 · 키 짝 없이(전후 비교)
                var hta = noHead ? null : Resources.Load<TextAsset>("RunUI/standing_head");
                int heads = 0;
                if (hta != null && FitJson.Parse(hta.text) is Dictionary<string, object> hj)
                    foreach (var kv in hj)
                    {
                        if (!(kv.Value is Dictionary<string, object> o) || !table.TryGetValue(kv.Key, out var f)) continue;
                        f.HasHead = true;
                        f.HeadX = Num(o, "headX") ?? f.CenterX;
                        f.FaceCx = Num(o, "cx") ?? f.HeadX;
                        f.HeadBox = Rect.MinMaxRect(Num(o, "l") ?? f.CenterX - 150, Num(o, "b") ?? f.HeadY - 100, Num(o, "r") ?? f.CenterX + 150, Num(o, "t") ?? f.HairTop);
                        f.FaceBox = Rect.MinMaxRect(Num(o, "fl") ?? f.FaceCx - 100, Num(o, "fb") ?? f.HeadY - 60, Num(o, "fr") ?? f.FaceCx + 100, Num(o, "ft") ?? f.TopY);
                        f.Bent = o.TryGetValue("flag", out var fl) && fl is bool fb && fb;
                        table[kv.Key] = f;
                        heads++;
                    }
                // 중심 손보정(standing_center_fix.json — 벨라 · 클로에 · 쥬비 · 슈팡): 바닥 · 몸 꼭대기 · 몸 가운데 · 얼굴 가운데 · 목록 카드 위 끝 가운데 적은 것만 덮는다
                bool noCenter = noHead || Array.IndexOf(Environment.GetCommandLineArgs(), "-nocenter") >= 0;
                var cta = noCenter ? null : Resources.Load<TextAsset>("RunUI/standing_center_fix");
                int centers = 0;
                if (cta != null && FitJson.Parse(cta.text) is Dictionary<string, object> cj)
                {
                    if (cj.TryGetValue("_faceRef", out var fro) && fro is Dictionary<string, object> frd)
                        foreach (var fk in frd) if (fk.Value is double fv) faceRef[fk.Key] = (float)fv;
                    foreach (var kv in cj)
                    {
                        if (kv.Key.StartsWith("_") || !(kv.Value is Dictionary<string, object> o) || !table.TryGetValue(kv.Key, out var f)) continue;
                        if (Num(o, "footY") is float fy) { f.FootY = fy; f.Bounds = Rect.MinMaxRect(f.Bounds.xMin, fy, f.Bounds.xMax, f.Bounds.yMax); }
                        if (Num(o, "topY") is float ty)
                        {
                            f.TopY = ty;
                            f.HairTop = Mathf.Min(f.HairTop, ty + 20);
                            if (f.HasHead) { f.HeadBox = Rect.MinMaxRect(f.HeadBox.xMin, f.HeadBox.yMin, f.HeadBox.xMax, Mathf.Min(f.HeadBox.yMax, ty + 20)); f.FaceBox = Rect.MinMaxRect(f.FaceBox.xMin, f.FaceBox.yMin, f.FaceBox.xMax, Mathf.Min(f.FaceBox.yMax, ty)); }
                        }
                        if (Num(o, "centerX") is float cx) f.CenterX = cx;
                        if (Num(o, "faceY") is float fyy) f.FaceY = fyy;
                        if (Num(o, "listTop") is float lt) f.ListTop = lt;
                        if (Num(o, "cardTop") is float ct) f.CardTop = ct;
                        if (Num(o, "cardFaceY") is float cf) f.CardFaceY = cf;
                        if (Num(o, "iconFaceY") is float ify) f.IconFaceY = ify;
                        if (Num(o, "iconScale") is float isc && isc > 0) f.IconScale = isc;
                        if (Num(o, "float") is float lf) f.Lift = lf;
                        if (Num(o, "faceCx") is float fc && f.HasHead)
                        {
                            float d = fc - f.FaceCx;
                            f.FaceCx = fc; f.HeadX = fc;
                            f.FaceBox = new Rect(f.FaceBox.x + d, f.FaceBox.y, f.FaceBox.width, f.FaceBox.height);
                        }
                        table[kv.Key] = f;
                        centers++;
                    }
                }
                // 키 손보정(standing_height_fix.json) — 앉거나 숙여 키가 줄었거나 탈것 · 의자를 키에 넣은 사도는 「편 키」(서 있을 때 몸 키)로. 짝보다 먼저
                var fta = noHead ? null : Resources.Load<TextAsset>("RunUI/standing_height_fix");
                int fixes = 0;
                if (fta != null && FitJson.Parse(fta.text) is Dictionary<string, object> fj)
                    foreach (var kv in fj)
                    {
                        if (kv.Key.StartsWith("_") || !(kv.Value is Dictionary<string, object> o) || !(Num(o, "body") is float body) || body <= 0 || !table.TryGetValue(kv.Key, out var f)) continue;
                        f.Scale = body / Mathf.Max(1, f.BodyH);
                        table[kv.Key] = f;
                        fixes++;
                    }
                // 같은 키 짝(standing_height_link.json — 「네르(빡침)과 네르 키 똑같게」): 그 판의 화면 몸 키를 원판과 같게. 표를 다시 재도 이 짝은 남는다
                var lta = noHead ? null : Resources.Load<TextAsset>("RunUI/standing_height_link");
                int links = 0;
                if (lta != null && FitJson.Parse(lta.text) is Dictionary<string, object> lj)
                    foreach (var kv in lj)
                    {
                        if (kv.Key.StartsWith("_") || !(kv.Value is string to) || !table.TryGetValue(kv.Key, out var f) || !table.TryGetValue(to, out var t0)) continue;
                        f.Scale = t0.Body / Mathf.Max(1, f.BodyH);
                        f.Link = to;
                        table[kv.Key] = f;
                        links++;
                    }
                var bodies = new List<float>();
                foreach (var f in table.Values) bodies.Add(f.Body);
                bodies.Sort();
                if (bodies.Count > 0) p95 = bodies[Mathf.Clamp(Mathf.RoundToInt(bodies.Count * 0.95f) - 1, 0, bodies.Count - 1)];
                Debug.Log($"[RunUI] 스탠딩 맞춤 표 {table.Count}명 · med_st {medSt} · 기준 키(95%) {p95:0} · 안 B {(hroot != null ? "standing_fit_height.json" : "없음 — 배율 1")} · 머리 상자 {heads}명 · 중심 손보정 {centers} · 키 손보정 {fixes} · 키 짝 {links}");
            }
            catch (Exception e) { Debug.LogWarning("[RunUI] standing_fit.json 을 읽지 못했습니다 — 그 자리에서 잽니다: " + e.Message); table.Clear(); }
        }

        /// <summary>공중 높이를 끈다(전후 비교 — 「-nolift」).</summary>
        public static readonly bool NoLift = Array.IndexOf(Environment.GetCommandLineArgs(), "-nolift") >= 0;

        static float? Num(Dictionary<string, object> o, string n) => o.TryGetValue(n, out var v) && v is double d ? (float)d : (float?)null;

        /// <summary>
        /// 칸 가운데에 둘 x(원본 단위) — 전신은 몸 가운데(centerX), 무릎께 · 상반신 · 얼굴은 얼굴 쪽으로 옮긴다(숙이거나 기운 포즈에서 얼굴이 반 잘리지 않게).
        /// 무릎께 = 몸 · 얼굴 중간, 상반신 = 얼굴 쪽 7할, 얼굴 칸 = 얼굴 가운데.
        /// </summary>
        public static float CropCenterX(Fit f, StandMode mode, bool face = false)
        {
            if (!f.HasHead || mode == StandMode.Full) return f.CenterX;
            float w = face ? 1f : mode == StandMode.Knee ? 0.5f : 0.7f;
            return Mathf.Lerp(f.CenterX, f.FaceCx, w);
        }

        /// <summary>모드별로 기준 키(가장 큰 사도)가 칸 높이의 몇 배인가 — Full 0.92 · Knee 1.2 · Upper 1.7.</summary>
        public static float Fill(StandMode mode) => mode == StandMode.Full ? 0.92f : mode == StandMode.Knee ? 1.25f : 1.7f;

        /// <summary>
        /// 원본 단위 1 당 화면 px(k)과 원점 자리를 정한다 — slot 은 스켈레톤을 둘 부모 안의 칸(부모의 왼쪽 아래가 0,0).
        /// tallest = 기준 키(원본 단위 — Tallest(함께 보일 사도들)), 0 이면 표 전체 95% 값. 같은 tallest 를 주면 사도끼리 같은 배율 · 같은 바닥선.
        /// Full: 발이 칸 아래 + pad. Knee · Upper: 기준 키 사도의 몸 꼭대기가 칸 위 끝에서 pad(없으면 칸 높이 6%) 아래가 되는 바닥선.
        /// 기준보다 큰 사도는 칸에 맞게 줄인다. 표에 없는 사도면 false.
        /// </summary>
        public static bool Solve(string art, Rect slot, StandMode mode, float tallest, float pad, out float k, out Vector2 origin, string lift = null)
        {
            k = 0; origin = Vector2.zero;
            if (!TryGet(art, out var f)) return false;
            // 떠 있는 사도(손보정 float — 쥬비 · 벨라 · 아일라 · 다야 · 에르핀(왕도), 2026-10-07 사용자: 「공중에 뜬 만큼 다시 내려서 다른 사도와 얼굴 위치를 같게」):
            //   전신이면 얼굴(iconFaceY · cardFaceY · faceY · 얼굴 상자 가운데)이 바닥선에서 그 화면의 기준 얼굴 높이(standing_center_fix.json _faceRef — 보통 사도 평균, 원본 단위)에
            //   오게 발 자리를 옮긴다. 배율은 그대로 — 몸 아래(꼬리 · 연기 · 바위)가 바닥선 아래로 빠지면 그 부분은 잘린다. 기준이 없으면 float 값만큼 띄운다.
            if (NoLift) lift = null;
            if (lift != null && mode == StandMode.Full && f.Lift != 0)
            {
                float face = f.IconFaceY != 0 ? f.IconFaceY : f.CardFaceY != 0 ? f.CardFaceY : f.FaceY != 0 ? f.FaceY : f.HasHead ? f.FaceBox.center.y : f.HeadY;
                f.FootY = faceRef.TryGetValue(lift, out var fr) && fr > 0 ? face - fr : f.FootY - f.Lift;
                f.HairTop = Mathf.Max(f.HairTop, f.FootY + 1);
            }
            if (tallest <= 0) tallest = p95;
            float top = mode == StandMode.Full ? 0 : pad > 0 ? pad : slot.height * 0.06f;
            float avail = mode == StandMode.Full ? slot.height - pad : slot.height - top;
            float kc = slot.height * Fill(mode) / tallest;          // 함께 쓰는 배율
            float floor = mode == StandMode.Full ? slot.yMin + pad : slot.yMax - top - tallest * kc;   // 함께 쓰는 바닥선
            k = kc * f.Scale;
            // 기준보다 큰 사도 — 몸 꼭대기가 칸 위를 넘으면 그 사도만 줄인다(바닥선은 그대로)
            float reach = mode == StandMode.Full ? (f.HairTop - f.FootY) : f.Body / f.Scale;
            float room = mode == StandMode.Full ? avail : slot.yMax - top - floor;
            if (reach * k > room) k = room / Mathf.Max(1, reach);
            origin = new Vector2(slot.center.x - CropCenterX(f, mode) * k, floor - f.FootY * k);
            // 얼굴 높이 손보정이 있는 사도(꿀벌 쥬비 — 얼굴이 몸 왼쪽 아래): 무릎께 · 상반신은 얼굴을 칸 가운데 위 1/3 에, 몸 전체가 칸 안에 들게
            if (f.FaceY != 0 && mode != StandMode.Full)
            {
                origin = new Vector2(slot.center.x - f.FaceCx * k, slot.yMax - slot.height / 3f - f.FaceY * k);
                float l = origin.x + f.Bounds.xMin * k, r = origin.x + f.Bounds.xMax * k;
                if (r - l <= slot.width) { if (l < slot.xMin) origin.x += slot.xMin - l; else if (r > slot.xMax) origin.x -= r - slot.xMax; }
                else origin.x = slot.center.x - f.Bounds.center.x * k;
                float b = origin.y + f.FootY * k, t = origin.y + f.Bounds.yMax * k;
                if (b < slot.yMin) origin.y += slot.yMin - b;
                else if (t > slot.yMax && b - (t - slot.yMax) >= slot.yMin) origin.y -= t - slot.yMax;
                return true;
            }
            // 얼굴이 칸 옆으로 나가면(숙인 · 기운 포즈) 안쪽으로 민다
            if (f.HasHead && mode != StandMode.Full)
            {
                float m = slot.width * 0.04f, l = origin.x + f.FaceBox.xMin * k, r = origin.x + f.FaceBox.xMax * k;
                if (l < slot.xMin + m) origin.x += slot.xMin + m - l;
                else if (r > slot.xMax - m) origin.x -= r - (slot.xMax - m);
            }
            return true;
        }

        /// <summary>
        /// 스켈레톤(SkeletonGraphic)을 칸에 세운다 — 앵커 · 피벗을 부모 왼쪽 아래(0,0)로 두고 배율 · 자리를 건다. 돌려줌: k(원본 단위 1 당 px), 표에 없으면 0(그대로 둔다).
        /// tallest = 기준 키(Tallest(함께 보일 사도들), 0 이면 표 95%). 컷인 · 판 화면 모두 이것.
        /// </summary>
        public static float Place(SkeletonGraphic g, string art, Rect slot, StandMode mode, float tallest = 0, float pad = 0, string lift = null)
        {
            if (g == null) return 0;
            float asScale = g.skeletonDataAsset != null ? g.skeletonDataAsset.scale : 0.01f;
            g.UpdateMesh();   // 꼭짓점 배율(MeshScale = 캔버스 referencePixelsPerUnit × 레이아웃 배율)을 실제 값으로
            float ppu = g.MeshScale > 0 ? g.MeshScale : g.canvas != null ? g.canvas.referencePixelsPerUnit : 100f;
            return Place(g.rectTransform, asScale * ppu, art, slot, mode, tallest, pad, lift);
        }

        /// <summary>같은 것(RectTransform 판) — unitPx = 원본 단위 1 이 배율 1 에서 몇 px 인가(SkeletonGraphic: 데이터 배율 0.01 × 캔버스 100 = 1).</summary>
        public static float Place(RectTransform rt, float unitPx, string art, Rect slot, StandMode mode, float tallest = 0, float pad = 0, string lift = null)
        {
            if (rt == null || !Solve(art, slot, mode, tallest, pad, out var k, out var o, lift)) return 0;
            float ls = k / Mathf.Max(0.0001f, unitPx);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.localScale = new Vector3(ls, ls, 1);
            rt.anchoredPosition = o;
            rt.sizeDelta = new Vector2(slot.xMax, slot.yMax) / Mathf.Max(0.0001f, ls);   // 칸을 덮는 크기(RectMask2D 가 통째로 걸러 내지 않게)
            return k;
        }

        // ═══════════ 표에 없는 사도 — 그 자리에서 재기(옛 다리 기준 방식, 대신 쓰는 길) ═══════════
        public struct M
        {
            public float Hip, Floor, Top, Left, Right, Leg, HipX;
            public float HeadX, HeadY;
            public bool HasHead;
            public string How;            // "bones" · "pelvis" · "bounds" · "file"
            public float Height => Top - Floor;
            public float Width => Right - Left;
        }

        static readonly string[][] Thighs = { new[] { "Leg_L_1", "Leg_R_1" }, new[] { "Thigh_L", "Thigh_R" }, new[] { "Leg_L", "Leg_R" } };
        static readonly string[] Pelvis = { "Pelvis", "Pelvis_CT", "Pelvis_HCT", "Pelvis_VCT", "Hip", "Hips" };
        static readonly string[] Heads = { "Head", "Head_Root", "Face", "Face_Root" };
        public const float FallbackShare = 0.32f;

        static Dictionary<string, Spine.Bone> Bones(Spine.Skeleton sk)
        {
            var d = new Dictionary<string, Spine.Bone>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in sk.Bones)
            {
                var n = b.Data.Name;
                var bare = System.Text.RegularExpressions.Regex.Replace(n, @"^S\d+_", "");
                if (!d.ContainsKey(n)) d[n] = b;
                if (!d.ContainsKey(bare)) d[bare] = b;
            }
            return d;
        }

        /// <summary>입히고 쉬는 동작 첫 자세를 건 스켈레톤을 잰다(스켈레톤 좌표). 표에 있는 사도면 표의 값(데이터 배율 곱함)으로 채운다.</summary>
        public static M Measure(Spine.Skeleton sk, string art = null, float assetScale = 0.01f)
        {
            sk.UpdateWorldTransform();
            float[] buf = null;
            sk.GetBounds(out float bx, out float by, out float bw, out float bh, ref buf);
            var m = new M { Floor = by, Top = by + bh, Left = bx, Right = bx + bw, HipX = bx + bw / 2, How = "bounds" };
            if (art != null && TryGet(art, out var f))
            {
                float a = assetScale;
                m.Floor = f.FootY * a; m.Top = f.HairTop * a; m.HipX = f.CenterX * a; m.HeadX = f.CenterX * a; m.HeadY = f.HeadY * a; m.HasHead = true;
                m.Left = f.Bounds.xMin * a; m.Right = f.Bounds.xMax * a;
                // 다리 길이는 「몸 키 중앙값 사도 기준」으로 — legPx 로 세우는 옛 호출이 몸 키 비를 따르게(몸 키 ≈ 다리 × 2.6)
                m.Leg = medSt / 2.6f / f.Scale * a; m.Hip = m.Floor + m.Leg;
                m.How = "file";
                return m;
            }
            var bones = Bones(sk);
            Spine.Bone Find(string name) => bones.TryGetValue(name, out var b0) ? b0 : null;
            float sy = 0, sx = 0; int n = 0;
            foreach (var pair in Thighs)
            {
                foreach (var name in pair) { var b = Find(name); if (b == null) continue; sy += b.WorldY; sx += b.WorldX; n++; }
                if (n > 0) break;
            }
            if (n > 0) { m.Hip = sy / n; m.HipX = sx / n; m.How = "bones"; }
            else foreach (var name in Pelvis) { var b = Find(name); if (b == null) continue; m.Hip = b.WorldY; m.HipX = b.WorldX; m.How = "pelvis"; n = 1; break; }
            m.Leg = m.Hip - m.Floor;
            foreach (var name in Heads) { var b = Find(name); if (b == null) continue; m.HeadX = b.WorldX; m.HeadY = b.WorldY; m.HasHead = true; break; }
            if (n == 0 || bh <= 0 || m.Leg < bh * 0.12f || m.Leg > bh * 0.75f)
            {
                m.Leg = Mathf.Max(0.01f, bh * FallbackShare);
                m.Hip = m.Floor + m.Leg;
                if (n == 0) m.HipX = bx + bw / 2;
                m.How = "bounds";
            }
            if (!m.HasHead) { m.HeadX = m.HipX; m.HeadY = m.Top - bh * 0.15f; }
            return m;
        }

        /// <summary>옛 손보정 배율 — standing_fit.json 으로 바뀌어 늘 1.</summary>
        public static float Fix(string art) => 1f;

        /// <summary>칸(box)에 세우는 배율과 자리 — 다리가 legLen 이 되게, 발은 box 아래 + floorPad. 넘치면 줄이고(shrunk) 가로로 넘치면 민다.</summary>
        public static float Place(M m, Rect box, float legLen, float fixMul, float floorPad, out Vector2 offset, out bool shrunk)
            => PlaceAt(m, box, legLen / Mathf.Max(0.0001f, m.Leg) * fixMul, floorPad, out offset, out shrunk);

        /// <summary>배율 s 로 세운다 — 발은 box 아래 + floorPad, 골반 x 는 가운데, 넘치면 줄이고 가로로 넘치면 민다.</summary>
        public static float PlaceAt(M m, Rect box, float s, float floorPad, out Vector2 offset, out bool shrunk)
        {
            shrunk = false;
            float maxH = box.height - floorPad, maxW = box.width;
            if (m.Height * s > maxH) { s = maxH / m.Height; shrunk = true; }
            if (m.How != "file" && m.Width * s > maxW) { s = maxW / m.Width; shrunk = true; }   // 표 사도는 날개 · 무기 폭으로 줄이지 않는다
            float x = box.center.x - m.HipX * s;
            if (m.How != "file")
            {
                float l = x + m.Left * s, r = x + m.Right * s;
                if (l < box.xMin) x += box.xMin - l;
                else if (r > box.xMax) x -= r - box.xMax;
            }
            offset = new Vector2(x, box.yMin + floorPad - m.Floor * s);
            return s;
        }

        /// <summary>몸 키 표의 스켈레톤 배율(데이터 배율 0.01 · 캔버스 100 기준) — 몸 키 중앙값 사도의 몸 키가 bodyRef px 가 되게. 표에 없으면 0.</summary>
        public static float BodyScale(string art, float bodyRef) => TryGet(art, out var f) ? bodyRef / MedSt * f.Scale : 0;

        /// <summary>「전신(머리카락 위 끝) / 몸 키」 중앙값 근사 — 칸 높이에서 몸 키를 정할 때.</summary>
        public static float FullOverBody => 1.08f;
    }

    /// <summary>작은 JSON 읽개 — 객체 → Dictionary · 배열 → List · 수 → double · 글 · true/false · null.</summary>
    public static class FitJson
    {
        public static object Parse(string s) { int i = 0; return Val(s, ref i); }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); var k = Str(s, ref i); Ws(s, ref i);
                    if (s[i] != ':') throw new FormatException(": 없음 @" + i);
                    i++; d[k] = Val(s, ref i); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw new FormatException(", } 없음 @" + i);
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Val(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return l; }
                    throw new FormatException(", ] 없음 @" + i);
                }
            }
            if (c == '"') return Str(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        static string Str(string s, ref int i)
        {
            i++;
            var sb = new System.Text.StringBuilder();
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++; char e = s[i];
                    if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                    else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e);
                }
                else sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }
    }
}
