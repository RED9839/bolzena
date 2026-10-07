using System;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    /// <summary>
    /// 오른쪽 아래 턴 종료 단추(2026-10-07 리메이크) — 남색 판 · 금 테 · 성격 색 포인트. 그림은 Tools/make_endturn.py 가 코드로 그린 것(UI/EndTurn).
    ///   (2026-10-08 트릭컬 결로 다시) Soft(기본) 분홍 알약 / Mint 민트 알약 / Bubble 노랑 방울 + 이름표 — 굵은 갈색 · 크림 테, 말랑하게 튀는 눌림
    ///   안 A(Medal) — 금속 금 테 메달 + 안쪽 「다음 턴」 고리 화살(▶) + 아래 「턴 종료」 이름표
    ///   안 B(Hourglass)   — 같은 메달 + 모래시계
    ///   안 C(Pill)        — 큰 「턴 종료」 글 + ▶▶ 의 가로 알약
    ///   Old               — 옛 파란 고리 ✓(비교용)
    /// 점검 인자 -endturnstyle medal|hourglass|pill|old 로 바꿔 본다.
    /// 상태: 보통 · 올림(밝아지고 조금 커짐) · 누름(작아지고 어두움) · 끝낼 때(쓸 카드 없음 — 금빛 맥동 + 테를 도는 빛) ·
    ///       잠김(적 차례 · 연출 중 — 흐리게, 메달은 모래시계로 · 「적 차례」) · 자동 전투(고리가 천천히 돌고 「자동 전투」).
    ///       저사양 · 움직임 줄이기 — 맥동 폭 0.3배 · 느리게, 도는 빛 · 크기 숨쉬기 없음.
    /// </summary>
    public class EndTurnButton : MonoBehaviour
    {
        public enum Style { Soft, Mint, Bubble, Medal, Hourglass, Pill, Old }

        public static Style Chosen
        {
            get
            {
                var a = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(a, "-endturnstyle");
                if (i >= 0 && i + 1 < a.Length)
                    switch (a[i + 1].ToLowerInvariant())
                    {
                        case "hourglass": case "b": return Style.Hourglass;
                        case "pill": case "c": return Style.Pill;
                        case "old": return Style.Old;
                        case "medal": case "a": return Style.Medal;
                        case "mint": return Style.Mint;
                        case "bubble": return Style.Bubble;
                    }
                return Style.Soft;
            }
        }

        // 지금 상태 — PartyHud 가 매 프레임 넣는다(Frozen 이면 점검이 직접 넣은 값을 그대로 둔다)
        public bool Hover, Pressed, Ready, Locked, EnemyTurn, Auto;
        public bool Frozen;
        /// <summary>점검용 — 저사양 · 움직임 줄이기 설정과 상관없이 잔잔한 모양으로.</summary>
        public static bool ForceCalm;
        static bool Calm => ForceCalm || Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;

        public Style Kind { get; private set; }
        /// <summary>누르는 자리 반폭 · 반높이(부모 무리의 16:9 단위) — 손패 오른쪽 끝도 이것으로 정한다.</summary>
        public Vector2 Half { get; private set; }
        /// <summary>단추 몸통(툴팁 자리).</summary>
        public SpriteRenderer Body { get; private set; }
        /// <summary>점검용 — 빛 고리 알파 · 몸통 크기(맥동 폭 재기).</summary>
        public float HaloA => halo != null ? halo.color.a : 0;
        public float FaceScale => face != null ? face.localScale.x : 1;

        float D;                                   // 메달 지름
        Color accent;
        SpriteRenderer halo, disc, dial, rim, sweep, gem, glyph, glyph2, tag, line, chev, glint;
        Ring oldRing;
        TextMeshPro label;
        Transform face;                           // 크기 · 누름을 받는 마디
        SpriteRenderer sFill, sGloss, sShadow, sCream, sOuter;
        float bounce, bounceV, prevPress;
        float lockF, readyF, hoverF, pressF, autoF, lockHold, spin;
        string labelNow;

        static readonly Color Gold = Tone.Gold, GoldDeep = Tone.GoldDeep;
        static readonly Color GlyphInk = new Color(0.93f, 0.96f, 1f), GlyphGold = new Color(1f, 0.92f, 0.7f);

        public static EndTurnButton Create(Transform parent, Vector3 at, int order, string leaderNature, Style kind)
        {
            var t = Make.Node("EndTurn", parent, at);
            var b = t.gameObject.AddComponent<EndTurnButton>();
            b.Build(order, leaderNature, kind);
            return b;
        }

        static Sprite S(string n) => Res.UI("EndTurn/" + n);

        void Build(int O, string nature, Style kind)
        {
            Kind = kind;
            // 성격 색 포인트 — 앞자리 사도의 성격 색을 하늘빛 쪽으로 조금 당겨(남색 위에서 또렷하게)
            accent = Color.Lerp(Tone.Nature(nature), Tone.Sky, 0.3f);
            var add = Res.SpriteMat(true, 1.4f);
            face = Make.Node("face", transform);
            var f = face;
            switch (kind)
            {
                case Style.Soft:
                case Style.Mint:
                case Style.Bubble:
                    BuildSoft(f, O, kind);
                    break;
                case Style.Pill:
                {
                    const float W = 1.62f, H = W * 232f / 760f;
                    Half = new Vector2(W / 2, H / 2 + 0.03f);
                    halo = Make.Box("halo", f, Res.UI("soft"), Vector3.zero, new Vector2(W * 1.5f, H * 2.6f), O, A(accent, 0), add);
                    disc = Body = Make.Box("pill", f, S("pill"), Vector3.zero, new Vector2(W, H), O + 2);
                    line = Make.Box("line", f, S("pill_line"), Vector3.zero, new Vector2(W, H), O + 3, A(accent, 0.75f));
                    glint = Make.Box("glint", f, Res.UI("soft"), Vector3.zero, new Vector2(0.5f, H * 0.9f), O + 4, A(Gold, 0), add);
                    chev = Make.Box("chev", f, S("chev"), new Vector3(W * 0.31f, 0, 0), new Vector2(H * 0.6f, H * 0.6f), O + 4, accent);
                    label = Make.Text("label", f, "턴 종료", new Vector3(-W * 0.08f, 0.0f, 0), 0.25f, O + 5, Tone.Ink);
                    Make.Outline(label, 0.22f, Tone.Outline);
                    break;
                }
                case Style.Old:
                {
                    D = 1.22f;
                    Half = new Vector2(D / 2 + 0.04f, D / 2 + 0.04f);
                    halo = Make.Box("endglow", f, Res.UI("soft"), Vector3.zero, new Vector2(2.4f, 2.4f), O, new Color(0.45f, 0.75f, 1f, 0f), Res.SpriteMat(true, 1.6f));
                    disc = Body = Make.Box("end", f, Res.UI("circle"), Vector3.zero, new Vector2(D, D), O + 2, new Color(0.05f, 0.08f, 0.17f, 0.95f));
                    oldRing = Tone.Ring("endring", f, Vector3.zero, D, 0.05f, O + 3, new Color(0.62f, 0.82f, 1f), new Color(0, 0, 0, 0));
                    oldRing.Set(1);
                    var inner = Tone.Ring("endring2", f, Vector3.zero, D - 0.22f, 0.025f, O + 3, new Color(0.62f, 0.82f, 1f, 0.6f), new Color(0, 0, 0, 0));
                    inner.Set(1);
                    glyph = Make.Box("check", f, Res.UI("ic_check"), Vector3.zero, new Vector2(0.56f, 0.56f), O + 4, new Color(0.88f, 0.95f, 1f));
                    label = Make.Text("endt", transform, "턴 종료", new Vector3(0, -D / 2 - 0.14f, 0), 0.13f, O + 4, Tone.Sub);
                    Make.Outline(label, 0.3f, Tone.Outline);
                    break;
                }
                default:
                {
                    D = 1.18f;
                    Half = new Vector2(D / 2 + 0.04f, D / 2 + 0.04f);
                    halo = Make.Box("halo", f, S("halo"), Vector3.zero, new Vector2(D * 1.4f, D * 1.4f), O, A(accent, 0), add);
                    disc = Body = Make.Box("disc", f, S("disc"), Vector3.zero, new Vector2(D, D), O + 2);
                    dial = Make.Box("dial", f, S("dial"), Vector3.zero, new Vector2(D, D), O + 3, A(accent, 0.5f));
                    rim = Make.Box("rim", f, S("rim"), Vector3.zero, new Vector2(D, D), O + 4);
                    sweep = Make.Box("sweep", f, S("sweep"), Vector3.zero, new Vector2(D, D), O + 5, A(Gold, 0), add);
                    gem = Make.Box("gem", f, S("gem"), new Vector3(0, D * 0.443f, 0), new Vector2(D * 0.12f, D * 0.12f), O + 6, accent);
                    var main = kind == Style.Hourglass ? "hourglass" : "arrow";
                    glyph = Make.Box("glyph", f, S(main), Vector3.zero, new Vector2(D * 0.6f, D * 0.6f), O + 6, GlyphInk);
                    // 잠김 · 자동 전투 때 바꿔 끼우는 둘째 글리프(메달 A 는 모래시계, B 는 고리 화살)
                    glyph2 = Make.Box("glyph2", f, S(kind == Style.Hourglass ? "arrow" : "hourglass"), Vector3.zero, new Vector2(D * 0.6f, D * 0.6f), O + 6, A(GlyphInk, 0));
                    float tw = D * 0.94f;
                    tag = Make.Box("tag", transform, S("tag"), new Vector3(0, -D * 0.53f, 0), new Vector2(tw, tw * 88f / 320f), O + 7);
                    label = Make.Text("label", transform, "턴 종료", new Vector3(0, -D * 0.53f - 0.005f, 0), 0.155f, O + 8, Tone.Ink);
                    Make.Outline(label, 0.2f, Tone.Outline);
                    Half = new Vector2(D / 2 + 0.04f, D / 2 + 0.04f);
                    break;
                }
            }
            labelNow = label.text;
        }


        // ── 트릭컬 결 — 둥글고 말랑한 모양, 굵은 갈색 · 크림 테, 파스텔 바탕 ──
        static readonly System.Collections.Generic.Dictionary<int, Sprite> rounds = new System.Collections.Generic.Dictionary<int, Sprite>();
        /// <summary>하얀 둥근 사각(9칸 늘림) — r 픽셀 반지름. 색은 렌더러 색으로.</summary>
        static Sprite Round(int r)
        {
            if (rounds.TryGetValue(r, out var sp) && sp) return sp;
            int n = r * 2 + 4;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "endturn_round" + r };
            var px = new Color32[n * n];
            float c = (n - 1) / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - c) - (c - r)), dy = Mathf.Max(0, Mathf.Abs(y - c) - (c - r));
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - r;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            sp = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r + 1, r + 1, r + 1, r + 1));
            rounds[r] = sp;
            return sp;
        }
        static SpriteRenderer RBox(string name, Transform p, Vector3 pos, Vector2 size, int order, Color c)
        {
            int r = Mathf.Max(4, Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 50f) - 2);   // 반지름 = 짧은 쪽의 절반(알약)
            return Make.Sliced(name, p, Round(r), pos, size, order, c);
        }
        static readonly Color Brown = new Color(0.43f, 0.26f, 0.15f), Cream = new Color(1f, 0.957f, 0.855f);
        Color fillOn, fillGrey = new Color(0.8f, 0.76f, 0.72f);

        void BuildSoft(Transform f, int O, Style kind)
        {
            bool bubble = kind == Style.Bubble;
            fillOn = kind == Style.Mint ? new Color(0.66f, 0.9f, 0.78f) : bubble ? new Color(1f, 0.88f, 0.5f) : new Color(1f, 0.72f, 0.7f);
            var add = Res.SpriteMat(true, 1.4f);
            halo = Make.Box("halo", f, Res.UI("soft"), Vector3.zero, bubble ? new Vector2(2.3f, 2.3f) : new Vector2(2.9f, 1.5f), O, A(fillOn, 0), add);
            if (!bubble)
            {
                const float W = 1.8f, H = 0.66f;
                Half = new Vector2(W / 2 + 0.02f, H / 2 + 0.04f);
                sShadow = RBox("shadow", f, new Vector3(0, -0.06f, 0), new Vector2(W, H), O + 1, new Color(0.25f, 0.13f, 0.05f, 0.4f));
                sOuter = Body = RBox("outer", f, Vector3.zero, new Vector2(W, H), O + 2, Brown);
                sCream = RBox("cream", f, Vector3.zero, new Vector2(W - 0.1f, H - 0.1f), O + 3, Cream);
                sFill = RBox("fill", f, new Vector3(0, -0.005f, 0), new Vector2(W - 0.22f, H - 0.22f), O + 4, fillOn);
                sGloss = RBox("gloss", f, new Vector3(0, 0.085f, 0), new Vector2(W - 0.42f, 0.13f), O + 5, new Color(1, 1, 1, 0.45f));
                label = Make.Text("label", f, "턴 종료", new Vector3(-0.12f, -0.01f, 0), 0.26f, O + 6, Color.white);
                Make.Outline(label, 0.38f, Brown);
                chev = Make.Box("chev", f, S("chev"), new Vector3(W * 0.33f, -0.005f, 0), new Vector2(0.3f, 0.3f), O + 6, Color.white);
                disc = sOuter;
            }
            else
            {
                const float D2 = 1.1f;
                Half = new Vector2(D2 / 2 + 0.04f, D2 / 2 + 0.04f);
                var circ = Res.UI("circle");
                sShadow = Make.Box("shadow", f, circ, new Vector3(0, -0.06f, 0), new Vector2(D2, D2), O + 1, new Color(0.25f, 0.13f, 0.05f, 0.4f));
                sOuter = Body = Make.Box("outer", f, circ, Vector3.zero, new Vector2(D2, D2), O + 2, Brown);
                sCream = Make.Box("cream", f, circ, Vector3.zero, new Vector2(D2 - 0.1f, D2 - 0.1f), O + 3, Cream);
                sFill = Make.Box("fill", f, circ, Vector3.zero, new Vector2(D2 - 0.22f, D2 - 0.22f), O + 4, fillOn);
                sGloss = RBox("gloss", f, new Vector3(0, 0.25f, 0), new Vector2(0.5f, 0.16f), O + 5, new Color(1, 1, 1, 0.5f));
                chev = Make.Box("chev", f, S("chev"), new Vector3(0.02f, -0.02f, 0), new Vector2(0.4f, 0.4f), O + 6, Brown);
                tag = RBox("tag", transform, new Vector3(0, -D2 / 2 - 0.02f, 0), new Vector2(0.98f, 0.3f), O + 7, Brown);
                RBox("tag2", transform, new Vector3(0, -D2 / 2 - 0.02f, 0), new Vector2(0.9f, 0.22f), O + 8, Cream);
                label = Make.Text("label", transform, "턴 종료", new Vector3(0, -D2 / 2 - 0.03f, 0), 0.16f, O + 9, Brown);
                disc = sOuter;
            }
        }

        void UpdateSoft(float pulse, float dt, bool calm)
        {
            // 말랑한 눌림 — 누르면 납작해지고, 떼면 위로 튀었다 가라앉는다(스프링). 잔잔 모드는 튐 없음
            if (prevPress > 0.5f && pressF < prevPress && !calm) bounceV = 6f;
            prevPress = pressF;
            bounceV += (-bounce * 90f - bounceV * 9f) * dt;
            bounce += bounceV * dt * 0.1f;
            float sx = 1 + 0.05f * hoverF + 0.04f * pressF + bounce * 0.6f + (calm ? 0 : readyF * 0.02f * Mathf.Sin(Clock.Now * 3f));
            float sy = 1 + 0.05f * hoverF - 0.1f * pressF - bounce;
            face.localScale = new Vector3(sx, sy, 1);
            face.localPosition = new Vector3(0, -0.03f * pressF, 0);
            var fill = Color.Lerp(Color.Lerp(fillOn, Color.Lerp(fillOn, Color.white, 0.35f), Mathf.Max(hoverF * 0.6f, pulse)), fillGrey, lockF);
            fill = Color.Lerp(fill, fill * 0.88f, pressF); fill.a = 1;
            sFill.color = fill;
            sCream.color = Color.Lerp(Cream, new Color(0.86f, 0.83f, 0.79f), lockF);
            sOuter.color = Color.Lerp(Brown, new Color(0.5f, 0.42f, 0.36f), lockF);
            sGloss.color = A(Color.white, 0.45f * (1 - 0.6f * lockF));
            if (halo != null) halo.color = A(fillOn, (0.1f * hoverF + 0.5f * pulse) * (1 - lockF));
            if (chev != null)
            {
                chev.color = Kind == Style.Bubble ? Color.Lerp(Brown, new Color(0.55f, 0.5f, 0.46f), lockF) : Color.Lerp(Color.white, new Color(0.9f, 0.88f, 0.85f), lockF);
                float push = calm ? 0 : readyF * 0.04f * Mathf.Max(0, Mathf.Sin(Clock.Now * 4f));
                chev.transform.localPosition = new Vector3((Kind == Style.Bubble ? 0.02f : Half.x * 0.66f - 0.02f) + push, chev.transform.localPosition.y, 0);
            }
            if (Kind != Style.Bubble) label.color = Color.Lerp(Color.white, new Color(0.92f, 0.9f, 0.88f), lockF);
            else label.color = Color.Lerp(Brown, new Color(0.5f, 0.45f, 0.4f), lockF);
            if (tag != null) tag.color = Color.Lerp(Brown, new Color(0.5f, 0.42f, 0.36f), lockF);
        }

        static Color A(Color c, float a) { c.a = a; return c; }

        /// <summary>월드 점이 단추 위인가(메달은 원 + 이름표, 알약은 둥근 사각).</summary>
        public bool Over(Vector2 world)
        {
            var lp = transform.InverseTransformPoint(world);
            if (Kind == Style.Pill || Kind == Style.Soft || Kind == Style.Mint) return Mathf.Abs(lp.x) < Half.x && Mathf.Abs(lp.y) < Half.y;
            if (Kind == Style.Bubble) return Mathf.Abs(lp.x) < Half.x && lp.y > -Half.y - 0.3f && lp.y < Half.y;
            if (tag != null && Mathf.Abs(lp.x) < D * 0.47f && lp.y < -D * 0.4f && lp.y > -D * 0.53f - 0.17f) return true;
            return lp.magnitude < Half.x;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool calm = Calm;
            // 잠김은 0.3초 뒤에 흐려진다 — 카드 한 장 내는 짧은 연출마다 깜빡이지 않게(적 차례는 바로)
            bool lockNow = Locked || EnemyTurn;
            lockHold = lockNow ? lockHold + dt : 0;
            bool lockShow = EnemyTurn || (lockNow && lockHold > 0.3f);
            lockF = Mathf.MoveTowards(lockF, lockShow ? 1 : 0, dt * 4f);
            bool readyOn = Ready && !lockNow && !Auto;
            readyF = Mathf.MoveTowards(readyF, readyOn ? 1 : 0, dt * 3f);
            hoverF = Mathf.MoveTowards(hoverF, Hover && !lockNow ? 1 : 0, dt * 8f);
            pressF = Mathf.MoveTowards(pressF, Pressed && !lockNow ? 1 : 0, dt * 14f);
            autoF = Mathf.MoveTowards(autoF, Auto ? 1 : 0, dt * 3f);

            // 맥동 — 끝낼 때 금빛으로 은은하게(잔잔 모드는 폭 0.3배 · 느리게)
            float speed = calm ? 1.6f : 3.2f;
            float wave = 0.5f + 0.5f * Mathf.Sin(Clock.Now * speed);
            float amp = calm ? 0.3f : 1f;
            float pulse = readyF * (0.55f + 0.45f * (1 - amp + amp * wave));

            // 크기 — 올림 1.05 · 누름 0.93 · 끝낼 때 숨쉬기(잔잔 모드 없음)
            float breathe = calm ? 0 : readyF * 0.025f * wave;
            float s = 1 + 0.05f * hoverF - 0.07f * pressF + breathe;
            face.localScale = new Vector3(s, s, 1);
            face.localPosition = new Vector3(0, -0.02f * pressF, 0);

            string want = EnemyTurn ? "적 차례" : Auto ? "자동 전투" : "턴 종료";
            if (want != labelNow) { label.text = want; labelNow = want; }
            float dim = Mathf.Lerp(1f, 0.5f, lockF);
            if (Kind > Style.Bubble) label.color = Color.Lerp(Color.Lerp(Kind == Style.Old ? Tone.Sub : Tone.Ink, Gold, Mathf.Max(readyF, hoverF * 0.6f)), Tone.Dim, lockF);

            if (Kind == Style.Old) { UpdateOld(pulse); return; }
            if (Kind <= Style.Bubble) { UpdateSoft(pulse, dt, calm); return; }

            var acc = Color.Lerp(accent, Gold, readyF);
            Color grey = new Color(0.55f, 0.58f, 0.66f);
            // 빛 고리 — 보통은 아주 옅게, 올림 · 끝낼 때 밝게
            if (halo != null)
            {
                float ha = 0.16f + 0.3f * hoverF + 0.55f * pulse;
                halo.color = A(Color.Lerp(acc, grey, lockF), ha * (1 - 0.8f * lockF));
            }
            var body = Color.Lerp(Color.white, new Color(0.6f, 0.62f, 0.7f), lockF) * (1 - 0.18f * pressF);
            body.a = 1;
            disc.color = body;
            if (rim != null) rim.color = Color.Lerp(Color.Lerp(Color.white, new Color(1.12f, 1.08f, 1f), hoverF), new Color(0.55f, 0.55f, 0.6f), lockF);
            if (tag != null) tag.color = Color.Lerp(Color.white, new Color(0.6f, 0.6f, 0.66f), lockF);
            if (dial != null)
            {
                dial.color = A(Color.Lerp(acc, grey, lockF), 0.45f + 0.25f * hoverF + 0.3f * readyF);
                // 자동 전투 — 눈금 고리가 천천히 돈다(잔잔 모드는 멈춤)
                if (!calm) spin += dt * (autoF * 40f);
                dial.transform.localRotation = Quaternion.Euler(0, 0, -spin);
            }
            if (gem != null) gem.color = Color.Lerp(acc, grey, lockF);
            if (sweep != null)
            {
                // 테를 도는 빛 — 끝낼 때(금) · 자동 전투(성격 색). 잔잔 모드는 돌지 않고 끈다
                float sa = calm ? 0 : Mathf.Max(readyF * 0.9f, autoF * 0.55f) * (1 - lockF);
                sweep.color = A(readyF > 0.01f ? Gold : accent, sa);
                sweep.transform.localRotation = Quaternion.Euler(0, 0, -Clock.Now * (readyF > 0.01f ? 150f : 90f));
            }
            if (glyph != null)
            {
                // 잠김이면 둘째 글리프(A — 모래시계)로 바꿔 끼운다. 자동 전투는 A 의 고리 화살 그대로(고리가 돈다)
                float swap = Kind == Style.Medal ? lockF : Kind == Style.Hourglass ? Mathf.Max(0, autoF - lockF) : 0;
                var gc = Color.Lerp(Color.Lerp(GlyphInk, GlyphGold, Mathf.Max(readyF, hoverF * 0.5f)), new Color(0.62f, 0.65f, 0.74f), lockF);
                glyph.color = A(gc, 1 - swap);
                if (glyph2 != null) glyph2.color = A(gc, swap);
                // B 의 모래시계 — 잠김이면 천천히 뒤집힌다(잔잔 모드 없음)
                float rot = 0;
                if (Kind == Style.Hourglass && !calm && lockF > 0.5f)
                {
                    float c = Clock.Now * 0.5f, n = Mathf.Floor(c);
                    rot = (n + Mathf.SmoothStep(0, 1, Mathf.Clamp01((c - n) / 0.15f))) * 180f;
                }
                glyph.transform.localRotation = Quaternion.Euler(0, 0, rot);
            }
            if (line != null) line.color = A(Color.Lerp(acc, grey, lockF), 0.7f + 0.3f * readyF);
            if (chev != null)
            {
                chev.color = Color.Lerp(acc, grey, lockF);
                // 알약의 ▶▶ — 끝낼 때 앞으로 살짝 밀었다 돌아온다(잔잔 모드 없음)
                float push = calm ? 0 : readyF * 0.05f * Mathf.Max(0, Mathf.Sin(Clock.Now * speed * 1.2f));
                chev.transform.localPosition = new Vector3(Half.x * 2 * 0.31f + push + 0.03f * hoverF, 0, 0);
            }
            if (glint != null)
            {
                // 알약 위를 가로지르는 빛(끝낼 때) — 잔잔 모드는 없음
                float k = Mathf.Repeat(Clock.Now * 0.45f, 1f);
                glint.transform.localPosition = new Vector3(Mathf.Lerp(-Half.x * 0.8f, Half.x * 0.8f, k), 0, 0);
                glint.color = A(Gold, calm ? 0 : readyF * 0.35f * Mathf.Sin(k * Mathf.PI));
            }
        }

        void UpdateOld(float pulse)
        {
            var g = halo.color;
            g.a = Ready && !Locked ? 0.35f + 0.25f * Mathf.Sin(Clock.Now * 3.5f) : Mathf.MoveTowards(g.a, 0.12f, Time.unscaledDeltaTime * 3f);
            if (Hover) g.a = 0.9f;
            halo.color = g;
            oldRing.SetColors(Ready || Hover ? new Color(0.75f, 0.9f, 1f) : new Color(0.5f, 0.7f, 0.95f), new Color(0, 0, 0, 0), Ready || Hover ? 1.6f : 1.1f);
        }
    }
}
