using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bolzena.RunUI
{
    // 로비 사도 만지기 — 트릭컬 대표 상호작용 둘(볼 당기기 · 간지럽히기)을 원작 스탠딩 스파인의 교감 장치 그대로 쓴다.
    //   원작 스탠딩(135명 전부)에 있는 것: 조작 본 Character_Ball_Move(볼) · Character_Tickle(배) · Character_Pat(정수리),
    //   동작 Touch_Idle/Touch_End(볼 잡힘 · 놓음) · Tickle_Idle_1/Tickle_End(간지럼) · Angry_1 · Happy_1.
    //   볼: Touch_Idle 이 제약(Face_CT ← Character_Ball_Move)을 켠다 → 그 본을 손가락 쪽으로 끌면 얼굴 · 볼이 따라 늘어난다(사도 데스크 mascot.js 와 같은 방식).
    //     끈 거리 → 본 이동은 부드러운 상한(R·(1−e^(−d/R))) · 놓으면 감쇠 진동으로 탄성 있게 돌아온다.
    //   간지럼: 몸통(Character_Tickle 둘레)을 좌우로 빠르게 두 번 넘게 오가면 Tickle_Idle_1 반복 → 떼면 Tickle_End.
    //   판정 자리는 사도마다 뼈(머리 · 눈 · 볼 · 배 기준점)에서 잡는다 — 그림 크기 · 자리 · 화면비가 달라도 맞는다.
    //   머리: 톡 = 꿀밤(Smash_End_1 → Smash_End_2), 누른 채 끌기 = 쓰다듬기(Pat_Idle — 정수리 본 Character_Pat 이 손을 따라감 → 떼면 Pat_End). 원작과 같다.
    //   대사는 사도마다 원작 말투로(HeroLines · hero_lines.json — lobby · cheek · tickle · angry · knock · pat).
    //   너무 자주(볼 5번 · 간지럼 4번 · 꿀밤 4번 쯤 짧은 사이에) 하면 화난 동작 + 「그만해요!」 — 시간이 지나면 풀린다.
    //   입력은 이 칸(로비 사도 칸의 투명 판)으로만 — 이름판 · 바꾸기 단추 · 말풍선은 위에 있어 먼저 받는다.
    public class LobbyTouch : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public SkeletonGraphic Sg;
        public HeroInfo Hero;
        /// <summary>말풍선에 한 줄.</summary>
        public Action<string> Say;
        /// <summary>그냥 톡(끌지 않고 뗌 · 머리 · 판정 밖) — 예전 반응(웃는 동작 + 말 돌리기).</summary>
        public Action Tap;
        /// <summary>점검(데모) — 판정 자리를 반투명으로 그린다.</summary>
        public static bool ShowZones;
        /// <summary>점검 — 마지막 반응 이름(cheek · cheek_end · tickle · tickle_end · angry · tap).</summary>
        public string Last { get; private set; }
        /// <summary>점검 — 지금 볼 본이 셋업 자리에서 벗어난 거리(캔버스 단위).</summary>
        public float PullPx { get; private set; }
        public float Annoy => annoy;

        enum Mode { None, Wait, Cheek, Tickle, Pat }
        Mode mode;
        int pid = int.MinValue;
        Vector2 downSk, lastSk;          // 스켈레톤 단위(누른 자리 · 마지막 자리)
        Vector2 downCv;                  // 캔버스 단위(톡 판정)
        string zone;
        // 볼 당기기 — 본 오프셋(부모 로컬 · 스켈레톤 단위)
        Vector2 pullWorld;               // 누른 자리부터 끈 양(스켈레톤 월드)
        Vector2 offLocal;                // 지금 본에 더하는 값
        Vector2 relFrom; float relT = -1; // 놓은 뒤 탄성 복귀
        // 쓰다듬기 — 정수리 본(Character_Pat)을 손 쪽으로(Pat_Idle 이 제약 Face_CT ← Character_Pat 을 켠다 → 얼굴이 손을 따라온다)
        Vector2 patWorld, patOff;
        Spine.Bone ball, tickle, pat, head;
        readonly List<Spine.Bone> eyes = new List<Spine.Bone>();
        Spine.Bone ballL, ballR;
        // 문지르기
        float rubLastX; int rubDir; float rubTravel; readonly List<float> turns = new List<float>();
        float tickleT;
        // 짜증 — 볼 1 · 간지럼 0.8 씩 쌓이고 초당 0.22 씩 풀린다. 4.5 넘으면 화낸다
        float annoy, sulkUntil;
        int cheekCount;
        const float AngryAt = 4.5f, Cool = 0.22f;
        SkeletonGraphic bound;
        RectTransform zoneLayer;

        // 반응 대사 — 사도마다 원작 말투로(HeroLines · Resources/RunUI/hero_lines.json). 없으면 존댓말 / 반말 공통 문구
        string lastLine;
        string Line(string kind)
        {
            var a = HeroLines.Of(Hero, kind);
            var s = a[UnityEngine.Random.Range(0, a.Length)];
            if (a.Length > 1 && s == lastLine) s = a[(Array.IndexOf(a, s) + 1) % a.Length];   // 같은 줄 연달아 안 나오게
            return lastLine = s;
        }

        /// <summary>스탠딩이 바뀌면(사도 바꾸기 · 창 크기) 새로 묶는다.</summary>
        public void Bind(SkeletonGraphic sg, HeroInfo h)
        {
            if (bound != null) { bound.BeforeApply -= Reset; bound.UpdateLocal -= Push; }
            Sg = sg; Hero = h; bound = sg;
            mode = Mode.None; pid = int.MinValue; offLocal = Vector2.zero; patOff = patWorld = Vector2.zero; relT = -1; PullPx = 0; annoy = 0; cheekCount = 0; sulkUntil = 0;
            ball = tickle = pat = head = ballL = ballR = null; eyes.Clear();
            if (sg == null) { DrawZones(); return; }
            foreach (var b in sg.Skeleton.Bones)
            {
                var n = b.Data.Name;
                if (n == "Character_Ball_Move") ball = b;
                else if (n == "Character_Tickle") tickle = b;
                else if (n == "Character_Pat") pat = b;
                else if (head == null && System.Text.RegularExpressions.Regex.IsMatch(n, "^(S\\d_)?Head$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) head = b;
                else if (ballL == null && System.Text.RegularExpressions.Regex.IsMatch(n, "^(S\\d_)?Ball_L(_Root)?$")) ballL = b;
                else if (ballR == null && System.Text.RegularExpressions.Regex.IsMatch(n, "^(S\\d_)?Ball_R(_Root)?$")) ballR = b;
                var lo = n.ToLowerInvariant();
                if (lo.Contains("eye") && !lo.Contains("brow") && !lo.Contains("blow") && !lo.Contains("lash") && !lo.Contains("light") && !lo.Contains("high") && !lo.Contains("shadow") && !lo.Contains("line")) eyes.Add(b);
            }
            sg.BeforeApply += Reset;
            sg.UpdateLocal += Push;
            Debug.Log($"[LobbyTouch] {h?.key}({h?.art}) 볼 본 {(ball != null ? "있음" : "없음")} · 배 본 {(tickle != null ? "있음" : "없음")} · 머리 {head?.Data.Name ?? "-"} · 눈 {eyes.Count}");
            Tw.After(0.15f, DrawZones);   // 첫 동작이 한 번 돈 뒤(뼈 자리가 잡힌 뒤)
        }

        void OnDestroy() { if (bound != null) { bound.BeforeApply -= Reset; bound.UpdateLocal -= Push; } }

        // ── 판정(스켈레톤 단위) ──
        float Sc => Sg != null && Sg.skeletonDataAsset != null ? Sg.skeletonDataAsset.scale : 0.01f;
        float MeshK => Sg != null && Sg.MeshScale > 0 ? Sg.MeshScale : 100f;

        struct Geo { public float neckY, headX, eyeY, browY, u, faceX, faceY, tx, ty, side, bx, by; }
        Geo G()
        {
            var g = new Geo();
            float k = Sc;
            g.neckY = head != null ? head.WorldY : ball != null ? ball.WorldY - 45 * k : 0;
            g.headX = pat != null ? pat.WorldX : head != null ? head.WorldX : 0;
            float d = 0;
            if (eyes.Count > 0) { float s = 0; foreach (var e in eyes) s += e.WorldY; d = s / eyes.Count - g.neckY; }
            if (d < 20 * k) d = 65 * k;
            g.u = Mathf.Max(d, 60 * k);
            g.eyeY = g.neckY + d;
            g.browY = g.neckY + d + 0.6f * g.u;
            if (pat != null && ball != null && pat.WorldY > ball.WorldY) g.browY = Mathf.Min(g.browY, pat.WorldY - 0.1f * (pat.WorldY - ball.WorldY));
            // 얼굴 가운데 — 두 볼 뼈 사이(없으면 머리 x · 목과 눈 사이)
            if (ballL != null && ballR != null) { g.faceX = (ballL.WorldX + ballR.WorldX) / 2; g.faceY = (ballL.WorldY + ballR.WorldY) / 2; }
            else { g.faceX = g.headX; g.faceY = (g.neckY + g.eyeY) / 2; }
            g.tx = tickle != null ? tickle.WorldX : g.headX;
            g.ty = tickle != null ? tickle.WorldY : g.neckY - 2.5f * g.u;
            // 볼 장치는 한쪽 볼에만 있다(원작 리그: Character_Ball_Move 가 사도 오른볼 = 화면 왼쪽 볼 뼈 아래) — 그 볼이 얼굴 가운데의 어느 쪽인가
            g.side = ball != null && Mathf.Abs(ball.WorldX - g.faceX) > 0.05f * g.u ? Mathf.Sign(ball.WorldX - g.faceX) : -1;
            g.bx = ball != null ? ball.WorldX : g.faceX + g.side * 0.9f * g.u;
            g.by = ball != null ? ball.WorldY : g.faceY;
            return g;
        }

        /// <summary>그 자리(스켈레톤 단위)가 어디인가 — cheek(볼 장치 쪽 얼굴) · face(반대쪽 얼굴) · body(몸통) · head(이마 위) · out.</summary>
        string ZoneAt(Vector2 p)
        {
            if (Sg == null) return "out";
            var g = G();
            float dx = Mathf.Abs(p.x - g.faceX);
            // 얼굴 — 볼 장치가 있는 쪽 절반(가운데에서 조금 넘어까지)만 볼 당기기, 반대쪽은 톡
            if (p.y >= g.neckY - 0.25f * g.u && p.y <= g.browY && dx <= 1.9f * g.u) return (p.x - g.faceX) * g.side >= -0.2f * g.u ? "cheek" : "face";
            if (p.y > g.browY && Mathf.Abs(p.x - g.headX) <= 2.6f * g.u) return "head";
            float bx = Mathf.Abs(p.x - g.tx);
            if (p.y < g.neckY - 0.25f * g.u && p.y >= g.ty - 2.6f * g.u && bx <= 2.6f * g.u) return "body";
            return "out";
        }

        bool ToSk(PointerEventData e, out Vector2 sk)
        {
            sk = default;
            if (Sg == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Sg.rectTransform, e.position, e.pressEventCamera, out var lp)) return false;
            sk = lp / MeshK;
            return true;
        }

        Vector2 ToCanvas(Vector2 screen)
        {
            var cv = GetComponentInParent<Canvas>();
            return screen / (cv != null ? cv.rootCanvas.scaleFactor : 1f);
        }

        /// <summary>점검(데모) — 그 판정 자리의 가운데 화면 좌표(cheek · body · head).</summary>
        public Vector2 ScreenOf(string z, float ox = 0, float oy = 0)
        {
            if (Sg == null) return Vector2.zero;
            var g = G();
            Vector2 sk = z == "cheek" ? new Vector2(g.bx + g.side * ox * g.u, g.by + oy * g.u)   // ox + = 얼굴 바깥쪽
                : z == "body" ? new Vector2(g.tx + ox * g.u, g.ty + oy * g.u)
                : new Vector2(g.headX + ox * g.u, g.browY + (0.8f + oy) * g.u);
            var w = Sg.rectTransform.TransformPoint(sk * MeshK);
            var cv = Sg.canvas;
            var cam = cv != null && cv.renderMode != RenderMode.ScreenSpaceOverlay ? cv.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, w);
        }

        /// <summary>볼 장치가 얼굴 가운데의 어느 쪽인가(화면 x 부호 — 바깥으로 끌 방향).</summary>
        public float Side => Sg != null ? G().side : -1;

        /// <summary>점검 — 화면 픽셀 한 단위가 얼굴 단위(u)로 몇인가(데모가 끄는 거리를 얼굴 크기에 맞춘다).</summary>
        public float ScreenPerU()
        {
            if (Sg == null) return 1;
            var a = ScreenOf("cheek"); var b = ScreenOf("cheek", 1, 0);
            return Vector2.Distance(a, b);
        }

        // ── 입력 ──
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = false; }

        public void OnPointerDown(PointerEventData e)
        {
            if (pid != int.MinValue) return;   // 두 번째 손가락은 무시
            if (!ToSk(e, out var sk)) return;
            pid = e.pointerId;
            downSk = lastSk = sk; downCv = ToCanvas(e.position);
            zone = ZoneAt(sk);
            rubLastX = downCv.x; rubDir = 0; rubTravel = 0; turns.Clear();
            mode = Mode.Wait;
            if (Time.unscaledTime < sulkUntil) return;
            if (zone == "cheek" && ball != null)
            {
                mode = Mode.Cheek; pullWorld = Vector2.zero; relT = -1;
                Play("Touch_Idle", true);
                Last = "cheek";
            }
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pid || !ToSk(e, out var sk)) return;
            lastSk = sk;
            var cvp = ToCanvas(e.position);
            if (mode == Mode.Cheek) { pullWorld = sk - downSk; return; }
            if (mode == Mode.Pat) { patWorld = sk - downSk; return; }
            // 머리를 누른 채 끌면 쓰다듬기(톡 하고 떼면 꿀밤 — 원작과 같다)
            if (mode == Mode.Wait && zone == "head" && Time.unscaledTime >= sulkUntil && Vector2.Distance(cvp, downCv) > 8) { StartPat(); return; }
            if (zone != "body" || Time.unscaledTime < sulkUntil) return;
            // 문지르기 — 좌우 방향이 바뀐 때를 센다(최근 1.1초 안에 둘 이상 + 오간 길이)
            float d = cvp.x - rubLastX; rubLastX = cvp.x;
            if (Mathf.Abs(d) >= 2)
            {
                int s = d > 0 ? 1 : -1;
                if (rubDir != 0 && s != rubDir) turns.Add(Time.unscaledTime);
                rubDir = s; rubTravel += Mathf.Abs(d);
            }
            turns.RemoveAll(t => Time.unscaledTime - t > 1.1f);
            if (mode == Mode.Wait && turns.Count >= 2 && rubTravel >= 70) StartTickle();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pid) return;
            pid = int.MinValue;
            var m = mode; mode = Mode.None;
            float moved = Vector2.Distance(ToCanvas(e.position), downCv);
            if (m == Mode.Cheek)
            {
                if (moved < 10) { pullWorld = Vector2.zero; Idle(); Tap?.Invoke(); Last = "tap"; return; }   // 볼을 톡 — 끌지 않았다
                relFrom = offLocal; relT = 0;   // 탄성 복귀
                cheekCount++;
                annoy += 1f;
                if (annoy >= AngryAt) { Angry(false); return; }
                var te = Play("Touch_End", false);
                if (te != null) te.MixDuration = 0.22f;   // 볼 제약이 한 번에 꺼지지 않게 — 늘어난 볼이 튕기며(감쇠 진동) 돌아오는 게 보이게
                AddIdle();
                Say?.Invoke(Line("cheek"));
                HeroVoice.Speak(Hero, "touch1", "touch");
                Last = "cheek_end";
                return;
            }
            if (m == Mode.Tickle)
            {
                annoy += 0.8f + Mathf.Min(1.2f, tickleT * 0.25f);
                if (annoy >= AngryAt) { Angry(true); return; }
                Play("Tickle_End", false); AddIdle();
                Last = "tickle_end";
                return;
            }
            if (m == Mode.Pat) { EndPat(); return; }
            if (Time.unscaledTime < sulkUntil) { Say?.Invoke("흥."); return; }
            if (moved < 14 && zone == "head") { Knock(); return; }
            if (moved < 14) { Tap?.Invoke(); Last = "tap"; }
        }

        // 원작 교감(사도 데스크 mascot.js · anim-pools.js 조사): 머리 톡 = 꿀밤 Smash_End_1(맞는 순간) → Smash_End_2(머리 감싸기) + dutchrubend 목소리,
        //   머리를 누른 채 끌기 = 쓰다듬기 Pat_Idle(정수리 본이 손을 따라감) → 떼면 Pat_End + touch2 목소리.
        /// <summary>꿀밤(정수리를 톡) — 맞는 동작 2단 + 사도별 꿀밤 대사. 짜증이 쌓인다(볼보다 조금 더).</summary>
        void Knock()
        {
            annoy += 1.2f;
            if (annoy >= AngryAt) { Angry(false); return; }
            var a1 = Play("Smash_End_1", false);
            if (a1 == null) a1 = Play("Smash_End", false);
            if (a1 != null && Sg != null && Sg.Skeleton.Data.FindAnimation("Smash_End_2") != null) Sg.AnimationState.AddAnimation(0, "Smash_End_2", false, 0);
            AddIdle();
            Say?.Invoke(Line("knock"));
            HeroVoice.Speak(Hero, "dutchrubend", "anger", "no");
            Last = "knock";
        }

        void StartPat()
        {
            mode = Mode.Pat; patWorld = Vector2.zero;
            if (Play("Pat_Idle", true) == null) Play("Happy_1", false);
            Last = "pat";
        }

        void EndPat()
        {
            if (Play("Pat_End", false) == null) Play("Happy_1", false);
            AddIdle();
            annoy = Mathf.Max(0, annoy - 1f);
            Say?.Invoke(Line("pat"));
            HeroVoice.Speak(Hero, "touch2", "pat", "joy");
            Last = "pat_end";
        }

        void StartTickle()
        {
            mode = Mode.Tickle; tickleT = 0;
            Play("Tickle_Idle_1", true);
            Say?.Invoke(Line("tickle"));
            HeroVoice.Speak(Hero, "ticklestart", "tickleduring", "joy");
            Last = "tickle";
        }

        void Angry(bool fromTickle)
        {
            Play("Angry_1", false); AddIdle();
            Say?.Invoke(Line("angry"));
            HeroVoice.Speak(Hero, "anger", "no");
            annoy = 1.5f; cheekCount = 0;
            sulkUntil = Time.unscaledTime + 1.6f;   // 잠깐 삐져서 받지 않는다
            Last = "angry";
            Debug.Log($"[LobbyTouch] {Hero?.key} 화냄({(fromTickle ? "간지럼" : "볼")})");
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (mode == Mode.Tickle) tickleT += dt;
            else if (annoy > 0) annoy = Mathf.Max(0, annoy - Cool * dt);
            if (annoy <= 0.01f) cheekCount = 0;
            if (relT >= 0) relT += dt;
            if (mode == Mode.Tickle && Sg != null && !Settings.ReduceMotion)
            {
                // 몸 흔들림 — 동작 위에 아주 작게 좌우로(웃음에 몸이 들썩이는 느낌)
                float a = Mathf.Sin(Time.unscaledTime * 34f) * 0.9f;
                Sg.rectTransform.localEulerAngles = new Vector3(0, 0, a);
            }
            else if (Sg != null && Sg.rectTransform.localEulerAngles.z != 0) Sg.rectTransform.localEulerAngles = Vector3.zero;
        }

        // ── 볼 본 구동 ── BeforeApply 에서 셋업 자리로(키가 없는 본은 동작이 안 건드려 오프셋이 쌓인다) → UpdateLocal 에서 끈 만큼 더한다
        void Reset(ISkeletonAnimation _)
        {
            if (pat != null) { pat.X = pat.Data.X; pat.Y = pat.Data.Y; }
            if (ball == null) return;
            ball.X = ball.Data.X; ball.Y = ball.Data.Y;
        }

        /// <summary>끈 양(스켈레톤 월드) → 그 본 부모 로컬 오프셋(상한 R · 고무줄처럼).</summary>
        static bool ToLocal(Spine.Bone b, Vector2 w, float R, out Vector2 o)
        {
            o = default;
            var p = b.Parent;
            if (p == null) return false;
            float det = p.A * p.D - p.B * p.C;
            if (Mathf.Abs(det) < 1e-9f) return false;
            float lx = (w.x * p.D - w.y * p.B) / det, ly = (w.y * p.A - w.x * p.C) / det, len = Mathf.Sqrt(lx * lx + ly * ly);
            if (len > 1e-6f) { float s = R * (1 - Mathf.Exp(-len / R)) / len; lx *= s; ly *= s; }
            o = new Vector2(lx, ly);
            return true;
        }

        void Push(ISkeletonAnimation _)
        {
            if (pat != null)
            {
                // 쓰다듬기 — 정수리 본이 손을 따라간다(상한 110 · 위아래는 덜). 놓으면 부드럽게 제자리
                var want = Vector2.zero;
                if (mode == Mode.Pat && ToLocal(pat, new Vector2(patWorld.x, patWorld.y * 0.6f), 110f * Sc, out var o)) want = o;
                patOff = Vector2.Lerp(patOff, want, 1 - Mathf.Exp(-Time.unscaledDeltaTime / (mode == Mode.Pat ? 0.05f : 0.12f)));
                pat.X += patOff.x; pat.Y += patOff.y;
            }
            if (ball == null) return;
            if (mode == Mode.Cheek)
            {
                var p = ball.Parent;
                if (p == null) return;
                float det = p.A * p.D - p.B * p.C;
                if (Mathf.Abs(det) < 1e-9f) return;
                // 끈 방향 — 얼굴 바깥쪽(볼이 늘어나는 쪽)은 그대로, 안쪽은 조금만(안으로 밀면 얼굴 가죽이 접혀 이음매가 보인다) · 위아래는 덜
                float side = G().side;
                var pw = pullWorld;
                if (pw.x * side < 0) pw.x *= 0.22f;
                pw.y *= 0.7f;
                // 부모 월드 행렬(앞 프레임) 역변환 → 부모 로컬
                float lx = (pw.x * p.D - pw.y * p.B) / det, ly = (pw.y * p.A - pw.x * p.C) / det;
                float R = 150f * Sc, len = Mathf.Sqrt(lx * lx + ly * ly);   // 상한 — 고무줄처럼 갈수록 덜 늘어난다
                if (len > 1e-6f) { float s = R * (1 - Mathf.Exp(-len / R)) / len; lx *= s; ly *= s; }
                offLocal = Vector2.Lerp(offLocal, new Vector2(lx, ly), 1 - Mathf.Exp(-Time.unscaledDeltaTime / 0.035f));
            }
            else if (relT >= 0)
            {
                // 놓은 뒤 — 감쇠 진동(넘쳐 튕겼다가 제자리)
                float t = relT;
                float f = Mathf.Exp(-t / 0.13f) * Mathf.Cos(t * Mathf.PI * 2 * 4.2f);
                if (f < 0) f *= 0.4f;   // 넘쳐 튕기는 쪽(얼굴 안쪽)은 작게 — 안으로 접히면 이음매가 보인다
                offLocal = relFrom * f;
                if (t > 0.9f) { offLocal = Vector2.zero; relT = -1; }
            }
            else offLocal = Vector2.zero;
            ball.X += offLocal.x; ball.Y += offLocal.y;
            var pp = ball.Parent;
            var rootT = Sg.canvas != null ? Sg.canvas.rootCanvas.transform : transform;
            PullPx = pp != null ? new Vector2(offLocal.x * pp.A + offLocal.y * pp.B, offLocal.x * pp.C + offLocal.y * pp.D).magnitude * MeshK * Sg.rectTransform.lossyScale.y / Mathf.Max(1e-6f, rootT.lossyScale.y) : 0;   // 캔버스 단위
        }

        // ── 동작 ──
        Spine.TrackEntry Play(string anim, bool loop)
        {
            if (Sg == null) return null;
            var a = SpineUi.PickAnim(Sg.Skeleton.Data, anim);
            if (a == null) return null;
            return Sg.AnimationState.SetAnimation(0, a, loop);
        }
        void AddIdle() { if (Sg != null) Sg.AnimationState.AddAnimation(0, SpineUi.PickAnim(Sg.Skeleton.Data, "Idle_1", "Idle"), true, 0); }
        void Idle() { if (Sg != null) Sg.AnimationState.SetAnimation(0, SpineUi.PickAnim(Sg.Skeleton.Data, "Idle_1", "Idle"), true); }

        // ── 점검: 판정 자리 그리기 ──
        void DrawZones()
        {
            if (zoneLayer != null) Destroy(zoneLayer.gameObject);
            if (!ShowZones || Sg == null || this == null) return;
            zoneLayer = Ui.Rect("zones", Sg.rectTransform);
            zoneLayer.anchorMin = Vector2.zero; zoneLayer.anchorMax = Vector2.one; zoneLayer.offsetMin = zoneLayer.offsetMax = Vector2.zero;
            var g = G(); float K = MeshK;
            // SkeletonGraphic 의 메시 원점 = 그 RectTransform 의 피벗 자리 → 칸 왼쪽 아래에서 (피벗 × 크기) 만큼
            var piv = Sg.rectTransform.pivot; var sz = Sg.rectTransform.rect.size;
            void Box(float x0, float y0, float x1, float y1, Color c)
            {
                var im = Ui.Img(zoneLayer, Theme.White, c, "z"); im.raycastTarget = false;
                var r = im.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
                r.anchoredPosition = new Vector2(x0 * K + piv.x * sz.x, y0 * K + piv.y * sz.y);
                r.sizeDelta = new Vector2((x1 - x0) * K, (y1 - y0) * K);
            }
            float c0 = g.faceX - 0.2f * g.u * g.side, c1 = g.faceX + 1.9f * g.u * g.side;
            Box(Mathf.Min(c0, c1), g.neckY - 0.25f * g.u, Mathf.Max(c0, c1), g.browY, new Color(1f, 0.35f, 0.5f, 0.3f));
            float f0 = g.faceX - 0.2f * g.u * g.side, f1 = g.faceX - 1.9f * g.u * g.side;
            Box(Mathf.Min(f0, f1), g.neckY - 0.25f * g.u, Mathf.Max(f0, f1), g.browY, new Color(1f, 1f, 1f, 0.18f));
            Box(g.tx - 2.6f * g.u, g.ty - 2.6f * g.u, g.tx + 2.6f * g.u, g.neckY - 0.25f * g.u, new Color(0.3f, 0.8f, 1f, 0.22f));
            Box(g.faceX - 0.12f * g.u, g.faceY - 0.12f * g.u, g.faceX + 0.12f * g.u, g.faceY + 0.12f * g.u, new Color(1, 0, 0, 0.9f));
            if (ball != null) Box(ball.WorldX - 0.1f * g.u, ball.WorldY - 0.1f * g.u, ball.WorldX + 0.1f * g.u, ball.WorldY + 0.1f * g.u, new Color(1, 1, 0, 0.95f));
        }
    }
}
