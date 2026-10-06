using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Battle;
using Bolzena.Core;
using Bolzena.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bolzena.Demo
{
    // 고학년 점검(-ultaudit) — 사도마다 전투를 새로 열어 고학년을 한 번 쓰고, 연출이 제대로 나오는지 적는다.
    //   ./Bolzena.exe -ultaudit [-ultaudit-from N] [-ultaudit-n M] -captures <폴더> -screen-width 1280 -screen-height 720 -screen-fullscreen 0
    //   파티 = [그 사도, 채우기 둘] · 적 = 마을 1층의 셋짜리 무리(전체 공격 조준 확인) · 게이지 가득 · 적 체력 크게(맞는 걸 끝까지 보게)
    //   한 사도마다: 컷인 그림(스파인 · 스탠딩 렌더 · 초상) · 튼 조각(Ultimate1_1 → …) · 그 스켈레톤의 고학년 동작 목록 · 때리는 순간 ·
    //   맞힌 적 수 · 끝 충격(히트스톱 · 흔들림 · 숫자) · 소리(효과음 · 목소리 — 못 찾은 것) · 프레임 최악 ms · 캡처 두 장(컷인 · 충격)
    //   → <폴더>/ultaudit_<from>.tsv 한 줄씩 + 로그 [UltAudit]
    public class UltAudit : MonoBehaviour
    {
        public static bool On => Cutins || Measure || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultaudit") >= 0;
        // -cutinaudit — 전투 한 판에서 사도 135명 컷인만 차례로 틀고, 이름이 찍히는 순간(스탠딩이 다 들어온 뒤) 찍는다.
        //   스탠딩 경계(렌더러 bounds)가 화면 안에 다 들어오는지 재어 cutin_<가로>x<세로>.tsv 에 적는다(잘림 0 확인)
        public static bool Cutins => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-cutinaudit") >= 0;
        // -standmeasure — 사도 135명의 인게임 SD · 스탠딩 스파인 몸 키(발 뼈 → 머리 뼈)를 재어 standmeasure.tsv 로(runui Tools~/standing_measure.py 가 표로)
        public static bool Measure => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-standmeasure") >= 0;

        static List<string> order;           // 점검할 사도 id(정렬)
        static int idx = -1, end;
        static string dir, tsv;
        static GameData data;
        static bool warm;                    // 예열 판(첫 칸) — 시트 · 줄을 적지 않는다
        static bool Warming => warm && idx == 0;
        static readonly List<string> played = new List<string>();
        static string cutStage = "";
        static bool hooked;

        BattleDirector d;
        Capture cap;
        readonly List<string> moments = new List<string>();
        readonly List<float> ms = new List<float>();
        int skip;
        int fxPeak;
        // -ultsheet — 컷인 뒤 첫 프레임부터 고학년이 끝날 때까지 100ms 마다 작게 찍고(frames) 그때 일어난 일을 적는다(tsv). 시트는 Tools/ult_sheet.py 가 붙인다
        public static bool SheetMode => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultsheet") >= 0;
        static string sheetDir;
        readonly List<(float ms, string kind, string text)> evs = new List<(float, string, string)>();
        float sheetT0 = -1, nextShot;
        // crop — 화면의 그 몫만(0~1, 왼쪽 아래 기준) w×h 로(카드 몸짓 시트는 싸움터 가운데 띠를 크게)
        IEnumerator Small(string path) => Small(path, new Rect(0, 0, 1, 1), 320, 180);
        IEnumerator Small(string path, Rect crop, int W, int H)
        {
            yield return new WaitForEndOfFrame();
            var rt = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D tex = null;
            if (Application.isBatchMode)
            {
                // -batchmode 는 화면(백버퍼)이 없다 — 카메라들을 깊이 차례로 직접 그린다(화면 덮개 UI 는 빠진다)
                var big = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                foreach (var c in Camera.allCameras.OrderBy(c => c.depth)) { var keep = c.targetTexture; c.targetTexture = big; c.Render(); c.targetTexture = keep; }
                Graphics.Blit(big, rt, crop.size, crop.position);
                RenderTexture.ReleaseTemporary(big);
            }
            else
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                Graphics.Blit(tex, rt, crop.size, crop.position);
            }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var o = new Texture2D(W, H, TextureFormat.RGB24, false);
            o.ReadPixels(new Rect(0, 0, W, H), 0, 0); o.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, o.EncodeToJPG(80));
            Destroy(o); if (tex != null) Destroy(tex);
        }                          // 원작 이펙트 입자 최대
        // 프레임 시간은 CPU 시계(Stopwatch)로 Update 사이를 잰다 — unscaledDeltaTime 은 DXGI 프레임 통계(화면 vblank)로 재다가
        //   중간에 「GetFrameStatistics is broken」 으로 CPU 시계로 바뀌어(2026-10-05 ultaudit3: 앞 20명만 6.94ms 배수) 사도끼리 비교가 안 됐다
        readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        double lastTick = -1;
        readonly List<float> dts = new List<float>();
        float worstM; string worstAt = "-";
        int cutEnds0, cutEndFrame = -1;      // 컷인이 끝난 프레임(ms 번호)
        float afterCutWorst;                 // 컷인 끝 뒤 10프레임 안 최악
        public static bool NoCap => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultaudit-nocap") >= 0;
        UltProbe probe;

        static string Arg(string n)
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, n);
            return i >= 0 && i < a.Length - 1 ? a[i + 1] : null;
        }

        public static void Attach(BattleDirector director)
        {
            var go = new GameObject("UltAudit");
            var r = go.AddComponent<UltAudit>();
            r.d = director;
        }

        // 차림 — BattleDirector 가 전투를 만들 때 부른다(CoreBattle.Fixture.Override)
        public static void Prepare() => Init();

        static void Init()
        {
            if (order != null) return;
            data = CoreBattle.LoadData();
            order = data.Heroes.Values.Where(h => h.Ult != null).Select(h => h.Id).OrderBy(x => x, System.StringComparer.Ordinal).ToList();
            // -ultaudit-only a,b,c — 그 사도만(쉼표로, 정렬 순서 그대로)
            var only = Arg("-ultaudit-only");
            if (!string.IsNullOrEmpty(only)) { var want = new HashSet<string>(only.Split(',').Select(x => x.Trim())); order = order.Where(want.Contains).ToList(); }
            // 시트는 한 실행의 첫 사도만 시각 원점이 약 300ms 밀린다(첫 전투 예열) — 첫 사도를 한 번 더 앞에 두고 그 판은 적지 않는다
            warm = SheetMode && order.Count > 0 && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultaudit-nowarm") < 0;
            if (warm) order.Insert(0, order[0]);
            int from = int.TryParse(Arg("-ultaudit-from"), out var f) ? f : 0;
            int n = int.TryParse(Arg("-ultaudit-n"), out var c) ? c : order.Count;
            idx = Mathf.Clamp(from, 0, order.Count);
            end = Mathf.Min(order.Count, idx + n);
            dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            Directory.CreateDirectory(dir);
            tsv = Path.Combine(dir, $"ultaudit_{from:000}.tsv");
            File.WriteAllText(tsv, "no\thero\tart\tult\tcost\taoe\tcutin\tanims\tplayed\tpick\tstrikeMs\ttargets\timpact\tsfx\tvoice\tmissing\tworstMs\tavgMs\tfxOrig\tfxParts\tfxPeak\tmoved\tmoveWhy\tmotionMs\tcutMs\tnotInPlan\tsndMode\tsndSpanMs\tsndTailMs\tfoeMinGap\tdtWorstMs\tworstAt\tafterCutMs\tnaturalMs\tspeed\tcutFrontMs\tcutBackMs\tengineHits\torigHits\tshownBy\thits\tfxImpacts\tsyncFirstMs\tsyncMaxMs\tsyncEach\n");
            CoreBattle.Fixture.Override = Make;
            if (!hooked)
            {
                hooked = true;
                Sfx.OnPlayed += s => { played.Add(s); if (cur != null && SheetMode) cur.evs.Add((Time.time * 1000f, s.StartsWith("voice:") ? "voice" : "snd", s)); };
                // 뼈 못 찾음은 마지막 자리(이 뒤로는 짐작)일 때만 — 고학년 쏘는 자리는 Ult_Shot → … → Attack1 차례로 찾으니 그 끝
                Bolzena.View.UnitView.BoneMiss += (h, p) =>
                {
                    if (System.Array.IndexOf(new[] { "Attack1", "Middle", "Top", "Bottom", "Front" }, p) < 0) return;
                    File.AppendAllText(Path.Combine(dir, "bone_miss.tsv"), $"{order[System.Math.Max(0, System.Math.Min(idx, order.Count - 1))]}\t{h}\tPoint_{p}\t{(p == "Attack1" ? "쏘는 자리 못 찾음 → 총구 · Point_Front · 짐작" : "짐작(그림 높이 비율)")}\n");
                };
                BattleDirector.Trace += (k, s) => { if (cur != null && SheetMode) cur.evs.Add((Time.time * 1000f, k, s)); };
                UltCutin.OnStage += s => cutStage = s;
            }
        }

        static CoreBattle.Fixture Make(GameData d)
        {
            Init();
            if (idx >= end) return null;
            var hid = order[idx];
            var fillers = d.Heroes.Values.Where(h => h.Id != hid && h.Name != d.Hero(hid).Name && h.Ult != null).OrderBy(h => h.Id, System.StringComparer.Ordinal).Take(2).Select(h => h.Id).ToList();
            var f = new CoreBattle.Fixture { Party = new List<string> { hid, fillers[0], fillers[1] }, Seed = 7 + idx };
            f.Deck = d.BuildDeck(f.Party);
            // 적 셋 — 마을 1층 일반 무리 가운데 가장 큰 것(전체 공격이면 무리 가운데를 노리는지 본다)
            List<string> best = null;
            foreach (var v in d.Villages.Values.OrderBy(v => v.Id, System.StringComparer.Ordinal))
                foreach (var g in v.Floors[0].Pools.SelectMany(p => p))
                    if (best == null || g.Count > best.Count) best = g.ToList();
            var foes = Arg("-ultaudit-foes");   // -ultaudit-foes clone_shady — 그 적들로(보스 클론 고학년 보기)
            f.Waves.Add(foes != null ? foes.Split(',').ToList() : best ?? new List<string> { "hatchling", "imoogi" });
            f.WaveHp.Add(double.TryParse(Arg("-ultaudit-foehp"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fh) ? fh : 6);   // -ultaudit-foehp 0.05 — 적을 약하게(처치 cue 란 · 키디언)
            f.WaveGauge.Add(R.GAUGE_MAX);
            f.Note = $"고학년 점검 {idx + 1}/{order.Count} — {hid}";
            return f;
        }

        static UltAudit cur;
        void Start()
        {
            Init();
            cur = this;
            sheetDir = dir;
            // 점검은 소리 0 — 소리 규칙을 볼 때만 -sound(사용자 규칙). 이 실행에서만
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sound") < 0) AudioListener.volume = 0f;
            PointerInput.Simulated = true;                 // 진짜 마우스가 창 위에 있어도 툴팁 · 고르기가 끼어들지 않게
            PointerInput.SimPos = new Vector2(0, -6);
            cap = gameObject.AddComponent<Capture>();
            cap.Dir = dir;
            cap.Off = NoCap;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultprobe") >= 0) probe = new UltProbe();
            d.Moment += m => moments.Add(m);
            StartCoroutine(Run());
        }

        void Update()
        {
            double now = clock.Elapsed.TotalMilliseconds;
            float m = lastTick < 0 ? 0 : (float)(now - lastTick);
            lastTick = now;
            probe?.Frame();
            if (d == null || !d.InUlt) return;
            if (skip > 0) { skip--; return; }
            if (cutEndFrame < 0 && UltCutin.Ends != cutEnds0) cutEndFrame = ms.Count;
            ms.Add(m);
            dts.Add(Time.unscaledDeltaTime * 1000f);
            fxPeak = Mathf.Max(fxPeak, Bolzena.Fx.FxRun.Live);
            string at = cutEndFrame < 0 ? $"컷인 중 {ms.Count}" : $"컷인 끝+{ms.Count - 1 - cutEndFrame}";
            if (cutEndFrame >= 0 && ms.Count - 1 - cutEndFrame <= 10) afterCutWorst = Mathf.Max(afterCutWorst, m);
            if (m > worstM) { worstM = m; worstAt = at; }
            if (m > 25f)
            {
                Debug.Log($"[UltAudit] 멈칫 {m:F1}ms(dt {Time.unscaledDeltaTime * 1000f:F1}) — {at} · 컷인 {cutStage} · 고비 {string.Join(",", moments.Skip(Mathf.Max(0, moments.Count - 3)))} · 이펙트 CPU {Bolzena.Fx.FxRun.CpuMsThisFrame:F2}ms · 입자 {Bolzena.Fx.FxRun.Live} · 무대 {Bolzena.Fx.FxRun.Pooled}");
                probe?.Dump();
            }
        }

        IEnumerator CutinRun()
        {
            float t = 0;
            while (!d.WaitingInput && t < 30f) { yield return null; t += Time.unscaledDeltaTime; }
            yield return new WaitForSecondsRealtime(0.5f);
            string res = $"{Screen.width}x{Screen.height}";
            var path = Path.Combine(dir, $"cutin_{res}.tsv");
            File.WriteAllText(path, string.Join("\t", "no", "hero", "art", "kind", "fit", "clipL", "clipR", "clipB", "clipT", "how", "leg", "hOverLeg", "floorPx", "hipPx", "centerPx", "hipXf", "headXf", "headYf") + "\n");
            var cam = Camera.main;
            float hw = cam.orthographicSize * cam.aspect, hh = cam.orthographicSize;
            int clipped = 0;
            // 티그(영웅)를 맨 앞에(컷인 크기 기준 — 모아 보기 첫 칸)
            var seq = new List<string>(order);
            int ti = seq.IndexOf("티그_영웅"); if (ti > 0) { seq.RemoveAt(ti); seq.Insert(0, "티그_영웅"); }
            for (int i = idx; i < end; i++)
            {
                var hd = data.Hero(seq[i]);
                var look = Look.Hero(hd.Id);
                cutStage = "";
                var co = StartCoroutine(UltCutin.Play(d.ScreenRoot, look.Art, hd.Name, hd.Ult.Name, look.Tint));
                t = 0;
                while (cutStage != "ult3_name" && t < 5f) { yield return null; t += Time.unscaledDeltaTime; }
                // 스탠딩 경계 — 화면(카메라) 밖으로 나간 몫(월드 단위, 0 이면 다 들어옴)
                var stT = d.ScreenRoot.Find("Cutin/standing");
                float cl = 0, cr = 0, cb = 0, ct = 0;
                if (stT != null)
                {
                    var rs = stT.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        var b = rs[0].bounds;
                        foreach (var r in rs) b.Encapsulate(r.bounds);
                        var c = cam.transform.position;
                        cl = Mathf.Max(0, (c.x - hw) - b.min.x); cr = Mathf.Max(0, b.max.x - (c.x + hw));
                        cb = Mathf.Max(0, (c.y - hh) - b.min.y); ct = Mathf.Max(0, b.max.y - (c.y + hh));
                    }
                }
                bool clip = cl > 0.02f || cr > 0.02f || cb > 0.02f || ct > 0.02f;
                if (clip) clipped++;
                cap.Still($"cutin_{res}_{i:000}_{hd.Id}");
                File.AppendAllText(path, string.Join("\t", i, hd.Id, look.Art, UltCutin.LastArt, UltCutin.LastFit.ToString("F3"), cl.ToString("F2"), cr.ToString("F2"), cb.ToString("F2"), ct.ToString("F2"),
                    UltCutin.LastHow ?? "-", UltCutin.LastLeg.ToString("F1"), UltCutin.LastHeight.ToString("F2"),
                    Mathf.RoundToInt(Screen.height - cam.WorldToScreenPoint(new Vector3(0, UltCutin.FloorY, 0)).y),
                    Mathf.RoundToInt(Screen.height - cam.WorldToScreenPoint(new Vector3(0, UltCutin.HipY, 0)).y),
                    Mathf.RoundToInt(cam.WorldToScreenPoint(new Vector3(UltCutin.CenterX, 0, 0)).x),
                    UltCutin.HipXf.ToString("F3"), UltCutin.HeadXf.ToString("F3"), UltCutin.HeadYf.ToString("F3")) + "\n");
                yield return co;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            Debug.Log($"[UltAudit] 컷인 {end - idx}명 · {res} · 잘림 {clipped}");
            Application.Quit(0);
        }

        // -cardsheet — 사도마다 카드 몸짓 셋(평타 Attack1 · 강공 Attack2 · 저학년 Skill1)을 카드 없이 틀며 100ms 마다 작게 찍는다
        //   → <captures>/_cards/<사도>/<Attack1|Attack2|Skill1>/<ms>.jpg (원작 기본공격 · 저학년 영상 시트와 견준다 — Tools/card_sheet.py)
        IEnumerator CardSheet()
        {
            while (idx < end)
            {
                string hid = order[idx];
                float t = 0;
                while (!d.WaitingInput && t < 30f) { yield return null; t += Time.unscaledDeltaTime; }
                yield return new WaitForSecondsRealtime(0.4f);
                var u = d.Heroes[0];
                foreach (var m in new[] { Bolzena.Battle.Motion.Attack1, Bolzena.Battle.Motion.Attack2, Bolzena.Battle.Motion.Skill1 })
                {
                    string an = u.AnimFor(m);
                    var fdir = Path.Combine(dir, "_cards", hid, m.ToString());
                    Directory.CreateDirectory(fdir);
                    File.WriteAllText(Path.Combine(fdir, "_anim.txt"), an ?? "-");
                    bool running = true;
                    d.CardAnimLog = ""; d.CardNaturalMs = 0; d.CardShownMs = 0; d.CardHitsShown = 0; d.CardMoveLog = "";
                    var co = StartCoroutine(Wrap(d.DemoMotion(0, m, 0), () => running = false));
                    float t0 = Time.time, next = 0;
                    while (running && Time.time - t0 < 8f)
                    {
                        float now = (Time.time - t0) * 1000f;
                        if (now >= next) { StartCoroutine(Small(Path.Combine(fdir, $"{Mathf.RoundToInt(next):00000}.jpg"), new Rect(0.02f, 0.28f, 0.8f, 0.5f), 512, 180)); next += 100; }
                        yield return null;
                    }
                    // 카드 몸짓 점검 — 이어 튼 조각 · 원래 길이(1배속) · 보인 길이(줄이고 배속 뒤) · 보인 타수 · 이동 · 전체(돌아오기까지)
                    if (!string.IsNullOrEmpty(d.CardAnimLog)) File.WriteAllText(Path.Combine(fdir, "_anim.txt"), d.CardAnimLog);
                    File.WriteAllText(Path.Combine(fdir, "_info.txt"), $"chain\t{d.CardAnimLog}\nnaturalMs\t{d.CardNaturalMs}\nshownMs\t{d.CardShownMs}\nspeed\t{d.CardSpeed:F2}\nhits\t{d.CardHitsShown}\nmove\t{d.CardMoveLog}\ntotalMs\t{Mathf.RoundToInt((Time.time - t0) * 1000)}\n");
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                Debug.Log("[CardSheet] " + hid);
                idx++;
                if (idx >= end) break;
                Clock.Reset();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield break;
            }
            Debug.Log("[CardSheet] 끝");
            Application.Quit(0);
        }

        static IEnumerator Wrap(IEnumerator co, System.Action done) { yield return co; done(); }

        // -ultaudit-foes — 고학년을 쓰지 않고 턴만 넘기며(-ultaudit-turns N, 기본 4) 적 차례를 0.25초마다 찍는다(보스 클론 예고 · 컷인 · 고학년 · 끊김)
        IEnumerator FoeTurns()
        {
            int turns = int.TryParse(Arg("-ultaudit-turns"), out var tn) ? tn : 4;
            string hid = order[idx];
            float t = 0, sh = 0; int n = 0;
            while (!d.WaitingInput && t < 30f) { if (t >= sh) { cap.Still($"foe_{hid}_in{n++:000}"); sh += 0.2f; } yield return null; t += Time.unscaledDeltaTime; }   // 등장(보스 띠 · 클론 컷)
            cap.Still($"foe_{hid}_t0");
            int useAt = int.TryParse(Arg("-ultaudit-useult"), out var ua) ? ua : -1;   // 그 내 턴에 사도 0 의 고학년을 쓴다(보스 끊기 — 기절)
            for (int i = 0; i < turns && !d.Over; i++)
            {
                // -ultaudit-break charge|spend — 힘 모으는(쏟을) 적이 보이는 첫 내 턴에 그 적 강인도를 거의 0 으로 하고 고학년을 써 격파 → 「끊김」을 본다
                string brk = Arg("-ultaudit-break");
                bool breakNow = brk != null && useAt < 0 && d.Battle.CanUlt(0, out _) && (d.Battle as CoreBattle) != null && (d.Battle as CoreBattle).AuditNearBreak(brk == "spend");
                if (breakNow) { useAt = i + 1; d.RefreshAll(); cap.Still($"foe_{hid}_b{n++:000}"); }
                if (i + 1 == useAt && d.Battle.CanUlt(0, out _))
                {
                    d.RequestUlt(0);
                    t = 0; float us = 0;
                    while (t < 20f && !(t > 1f && d.WaitingInput && !d.InUlt)) { if (t >= us) { cap.Still($"foe_{hid}_u{n++:000}"); us += 0.25f; } yield return null; t += Time.unscaledDeltaTime; }
                    for (float w = 0; w < 1.5f; w += 0.25f) { cap.Still($"foe_{hid}_u{n++:000}"); yield return new WaitForSecondsRealtime(0.25f); }
                }
                d.RequestEnd();
                yield return null;
                t = 0; float shot = 0, endAt = -1;
                while (t < 30f && !d.Over)
                {
                    if (t >= shot) { cap.Still($"foe_{hid}_t{i + 1}_{n++:000}"); shot += 0.25f; }
                    if (d.WaitingInput && t > 1f) { if (endAt < 0) endAt = t + 1.5f; else if (t >= endAt) break; }
                    yield return null; t += Time.unscaledDeltaTime;
                }
            }
            Debug.Log("[UltAudit] 적 차례 보기 끝");
            Application.Quit(0);
        }

        // -standmeasure 는 넘겼다(스탠딩 측정 담당이 유니티 밖에서 standing_fit.json 을 만든다) — 인자가 와도 바로 끝낸다
        IEnumerator MeasureRun()
        {
            yield return null;
            Debug.Log("[UltAudit] -standmeasure 는 쓰지 않는다 — runui Resources/RunUI/standing_fit.json 을 본다");
            Application.Quit(0);
        }

        // 스켈레톤의 고학년 조각 가운데 이번에 안 튼 것(다른 갈래면 「(갈래)」)
        static string NotInPlan(Spine.SkeletonData sk, string played)
        {
            if (sk == null || played == null) return "-";
            var used = new HashSet<string>(played.Split(new[] { " → " }, System.StringSplitOptions.None));
            var ways = Bolzena.Fx.SpineMotion.WaysOf(sk, "Ultimate1").SelectMany(w => w).ToList();
            var miss = new List<string>();
            foreach (var a in sk.Animations)
            {
                if (!a.Name.StartsWith("Ultimate", System.StringComparison.OrdinalIgnoreCase) || used.Contains(a.Name)) continue;
                miss.Add(a.Name + (ways.Contains(a.Name) ? "(갈래)" : ""));
            }
            return string.Join(",", miss);
        }

        void WriteSheetTsv(string hid, Bolzena.Core.HeroDef hd)
        {
            var rows = new List<(float ms, string kind, string text)>();
            foreach (var e in evs) rows.Add((e.ms - sheetT0 * 1000f, e.kind, e.text));
            if (d.LastFxCall != null)
                foreach (var f in d.LastFxCall.Fired)
                {
                    rows.Add((f.Ms - sheetT0 * 1000f, "fx", f.Name.Replace("fx_", "")));
                    if (f.Target) rows.Add((f.Ms + f.ImpactMs - sheetT0 * 1000f, "impact", "충격 " + f.Name.Replace("fx_", "")));
                }
            var camP = Camera.main;
            if (d.LastFxCall != null && camP != null)
                foreach (var pl in d.LastFxCall.Places) { var v = camP.WorldToViewportPoint(pl.World); rows.Add((pl.Ms - sheetT0 * 1000f, "pos", $"{pl.Name.Replace("fx_", "")} {v.x:F3} {v.y:F3}")); }
            rows.Sort((a, b) => a.ms.CompareTo(b.ms));
            var uh = Bolzena.Fx.UltMotion.Hits(hid);
            var tw = d.UltPlan?.Table;
            var head = new List<string>
            {
                "#hero\t" + hid, "#ult\t" + hd.Ult.Name, "#origHits\t" + (uh != null ? uh.N.ToString() : "-"), "#shownHits\t" + d.UltHitCount,
                "#engineHits\t" + d.UltEngineHits, "#move\t" + (tw != null ? tw.MoveType + (tw.Moves ? " → " + tw.Dest : "") : "-"), "#motionMs\t" + d.UltMotionMs + $" (원래 {d.UltNaturalMs} · ×{d.UltSpeed:F2})",
                "#endMs\t" + Mathf.RoundToInt(nextShot), "#syncMaxMs\t" + d.UltSyncMaxMs, "#mode\t" + (BattleDirector.OldSync ? "고치기 전(-oldsync)" : "지금"),
            };
            foreach (var r in rows) head.Add($"{Mathf.RoundToInt(r.ms)}\t{r.kind}\t{r.text}");
            if (!Warming) File.WriteAllLines(Path.Combine(sheetDir, hid + ".tsv"), head);
        }

        // 적끼리 가장 가까운 발 거리 — 겹쳐 서면 0 에 가깝다
        string FoeGap()
        {
            float m = float.MaxValue;
            for (int i = 0; i < d.Enemies.Count; i++)
                for (int j = i + 1; j < d.Enemies.Count; j++)
                    if (d.Enemies[i] && d.Enemies[j]) m = Mathf.Min(m, Vector2.Distance(d.Enemies[i].Home, d.Enemies[j].Home));
            return m == float.MaxValue ? "-" : m.ToString("F2");
        }

        IEnumerator Run()
        {
            if (Measure) { yield return MeasureRun(); yield break; }
            if (Cutins) { yield return CutinRun(); yield break; }
            if (Arg("-ultaudit-foes") != null) { yield return FoeTurns(); yield break; }
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-cardsheet") >= 0) { yield return CardSheet(); yield break; }
            if (idx >= end) { Application.Quit(0); yield break; }
            string hid = order[idx];
            var hd = data.Hero(hid);
            float t = 0;
            while (!d.WaitingInput && t < 30f) { yield return null; t += Time.unscaledDeltaTime; }
            yield return new WaitForSecondsRealtime(0.4f);
            var hs = d.Battle.Snapshot.Heroes[0];
            // 스켈레톤의 고학년 동작들
            var u = d.Heroes[0];
            var sk = u.SkelData;
            string anims = sk == null ? "(스파인 없음)" : string.Join(",", sk.Animations.Items.Take(sk.Animations.Count).Select(a => a.Name).Where(n => n.StartsWith("Ult", System.StringComparison.OrdinalIgnoreCase)));
            bool aoe = hd.Ult.Fx.Any(x => x.K == "dmg" && x.Target == "allEnemies");   // 전체 피해(상태만 전체면 단일)
            bool dmg = hd.Ult.Fx.Any(x => x.K == "dmg");
            evs.Clear(); sheetT0 = -1;
            played.Clear(); moments.Clear(); ms.Clear(); dts.Clear(); cutStage = ""; UltCutin.LastArt = null;
            worstM = 0; worstAt = "-"; afterCutWorst = 0; cutEndFrame = -1; cutEnds0 = UltCutin.Ends;
            bool shotCut = false, shotHit = false, shotFirst = false;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultaudit-prep") >= 0) { (d.Battle as CoreBattle)?.AuditPrep(hid, double.TryParse(Arg("-ultaudit-prephp"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var php) ? php : 0.25); d.RefreshAll(); }   // 조건부 cue(마무리 · 감전 · 꿀 · 특종) 보기
            string why = d.Battle.CanUlt(0, out var w) ? null : w;
            if (why == null)
            {
                probe?.Mark();
                d.RequestUlt(0);
                t = 0;
                while (!d.InUlt && t < 5f) { yield return null; t += Time.unscaledDeltaTime; }
                t = 0;
                while ((d.InUlt || !d.WaitingInput) && !d.Over && t < 25f)
                {
                    if (SheetMode)
                    {
                        if (sheetT0 < 0 && UltCutin.Ends > cutEnds0) { sheetT0 = Time.time; nextShot = 0; }
                        if (sheetT0 >= 0)
                        {
                            float now = (Time.time - sheetT0) * 1000f;
                            if (now >= nextShot)
                            {
                                var fdir = Path.Combine(sheetDir, "_frames", Warming ? "_warm" : hid);
                                Directory.CreateDirectory(fdir);
                                StartCoroutine(Small(Path.Combine(fdir, $"{Mathf.RoundToInt(nextShot):00000}.jpg")));
                                var cam = Camera.main;
                                if (cam != null)
                                {
                                    void Pt(string who, Vector3 local) { var v = cam.WorldToViewportPoint(d.FieldRoot.TransformPoint(local)); evs.Add((Time.time * 1000f, "pt", $"{who} {v.x:F3} {v.y:F3}")); }
                                    if (d.Heroes.Count > 0 && d.Heroes[0]) Pt("사도", d.Heroes[0].Center);
                                    for (int ei = 0; ei < d.Enemies.Count; ei++) if (d.Enemies[ei] && d.Enemies[ei].gameObject.activeSelf) Pt("적" + ei, d.Enemies[ei].Center);
                                }
                                nextShot += 100;
                            }
                        }
                        yield return null; t += Time.unscaledDeltaTime;
                        continue;
                    }
                    if (!shotCut && cutStage == "ult3_name") { shotCut = true; skip = 3; cap.Still($"{idx:000}_{hid}_a_cutin"); }
                    if (!shotFirst && (moments.Contains("ult_hit") || moments.Contains("ult_last"))) { shotFirst = true; skip = 3; cap.Still($"{idx:000}_{hid}_c_hit1"); }   // 첫 타격 그 프레임(이펙트 충격이 겹치는지)
                    if (!shotHit && moments.Contains("ult_impact")) { shotHit = true; skip = 3; cap.Still($"{idx:000}_{hid}_b_impact"); }
                    yield return null; t += Time.unscaledDeltaTime;
                }
                if (!shotHit) { cap.Still($"{idx:000}_{hid}_b_after"); }
                // -ultnext — 고학년 뒤 턴을 넘겨 적 차례 · 다음 내 턴 시작까지 본다(미로 거울 반짝 · 광선). 0.25초마다 찍는다
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ultnext") >= 0)
                {
                    t = 0;
                    while (!d.WaitingInput && t < 10f) { yield return null; t += Time.unscaledDeltaTime; }
                    d.RequestEnd();
                    t = 0; float shot = 0, endAt = -1; int n = 0;
                    yield return null;
                    while (t < 25f && !d.Over)
                    {
                        if (t >= shot) { cap.Still($"{idx:000}_{hid}_n{n++:00}"); shot += 0.25f; }
                        if (d.WaitingInput && t > 2f) { if (endAt < 0) endAt = t + 4.5f; else if (t >= endAt) break; }   // 다음 내 턴 — 발동(미로 광선 · 오로라 기둥 · 키샤 폭발 · 이드 깨기) 뒤 1.5초 더
                        yield return null; t += Time.unscaledDeltaTime;
                    }
                }
            }
            yield return new WaitForEndOfFrame();
            if (SheetMode) WriteSheetTsv(hid, hd);
            var sfx = played.Where(p => !p.StartsWith("voice:") && !p.StartsWith("!")).Distinct().ToList();
            var voice = played.Where(p => p.StartsWith("voice:")).Distinct().ToList();
            var miss = played.Where(p => p.StartsWith("!")).Distinct().ToList();
            float worst = ms.Count > 0 ? ms.Max() : 0, avg = ms.Count > 0 ? ms.Average() : 0;
            var plan = d.UltPlan;
            string line = string.Join("\t", new[]
            {
                idx.ToString(), hid, hs.Key, hd.Ult.Name, hd.Ult.Cost.ToString(), (dmg ? "" : "비피해 · ") + (aoe ? "전체" : "단일"), UltCutin.LastArt ?? "-",
                anims, why != null ? "못 씀: " + why : d.UltAnimLog ?? "-",
                plan?.Pick != null ? $"{plan.Pick.Value.I + 1}/{plan.Pick.Value.N}" : "-",
                plan?.S != null ? plan.S.At.ToString() : "-",
                d.UltTargets.ToString(), moments.Contains("ult_impact") ? "있음" : "없음",
                string.Join(",", sfx.Select(s => s.Replace("hero/" + hs.Key + "/", "~"))), string.Join(",", voice.Select(v => v.Substring(v.LastIndexOf('/') + 1))), string.Join(",", miss),
                worst.ToString("F1"), avg.ToString("F1"),
                d.LastFxOrig ? "원작" : "자체", d.LastFxParts.ToString(), fxPeak.ToString(),
                d.UltMoved ? "이동" : "제자리", d.UltMoveWhy, d.UltMotionMs.ToString(), d.UltCutMs.ToString(), NotInPlan(sk, d.UltAnimLog),
                d.UltSndMode, d.UltSndSpanMs.ToString(), d.UltSndTailMs.ToString(), FoeGap(),
                (dts.Count > 0 ? dts.Max() : 0).ToString("F1"), worstAt, afterCutWorst.ToString("F1"),
                d.UltNaturalMs.ToString(), d.UltSpeed.ToString("F2"), d.UltCutFrontMs.ToString(), d.UltCutBackMs.ToString(), d.UltEngineHits.ToString(), d.UltOrigHits.ToString(), d.UltShownBy, d.UltHitCount.ToString(), d.UltFxImpacts.ToString(), d.UltSyncFirstMs.ToString(), d.UltSyncMaxMs.ToString(), d.UltSyncLog,
            });
            if (!Warming) File.AppendAllText(tsv, line + "\n");
            Debug.Log("[UltAudit] " + line.Replace('\t', '|'));
            idx++;
            yield return new WaitForSecondsRealtime(0.3f);
            if (idx >= end) { Debug.Log("[UltAudit] 끝"); Application.Quit(0); yield break; }
            Clock.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
