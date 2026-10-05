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
        static readonly List<string> played = new List<string>();
        static string cutStage = "";
        static bool hooked;

        BattleDirector d;
        Capture cap;
        readonly List<string> moments = new List<string>();
        readonly List<float> ms = new List<float>();
        int skip;
        int fxPeak;                          // 원작 이펙트 입자 최대

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
            int from = int.TryParse(Arg("-ultaudit-from"), out var f) ? f : 0;
            int n = int.TryParse(Arg("-ultaudit-n"), out var c) ? c : order.Count;
            idx = Mathf.Clamp(from, 0, order.Count);
            end = Mathf.Min(order.Count, idx + n);
            dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            Directory.CreateDirectory(dir);
            tsv = Path.Combine(dir, $"ultaudit_{from:000}.tsv");
            File.WriteAllText(tsv, "no\thero\tart\tult\tcost\taoe\tcutin\tanims\tplayed\tpick\tstrikeMs\ttargets\timpact\tsfx\tvoice\tmissing\tworstMs\tavgMs\tfxOrig\tfxParts\tfxPeak\tmoved\tmoveWhy\tmotionMs\tcutMs\tnotInPlan\tsndMode\tsndSpanMs\tsndTailMs\tfoeMinGap\n");
            CoreBattle.Fixture.Override = Make;
            if (!hooked)
            {
                hooked = true;
                Sfx.OnPlayed += s => played.Add(s);
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
            f.Waves.Add(best ?? new List<string> { "hatchling_jolly", "imoogi_gloomy" });
            f.WaveHp.Add(6);
            f.WaveGauge.Add(R.GAUGE_MAX);
            f.Note = $"고학년 점검 {idx + 1}/{order.Count} — {hid}";
            return f;
        }

        void Start()
        {
            Init();
            // 점검은 소리 0 — 소리 규칙을 볼 때만 -sound(사용자 규칙). 이 실행에서만
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sound") < 0) AudioListener.volume = 0f;
            PointerInput.Simulated = true;                 // 진짜 마우스가 창 위에 있어도 툴팁 · 고르기가 끼어들지 않게
            PointerInput.SimPos = new Vector2(0, -6);
            cap = gameObject.AddComponent<Capture>();
            cap.Dir = dir;
            d.Moment += m => moments.Add(m);
            StartCoroutine(Run());
        }

        void Update()
        {
            if (d == null || !d.InUlt) return;
            if (skip > 0) { skip--; return; }
            float m = Time.unscaledDeltaTime * 1000f;
            ms.Add(m);
            fxPeak = Mathf.Max(fxPeak, Bolzena.Fx.FxRun.Live);
            if (m > 40f) Debug.Log($"[UltAudit] 멈칫 {m:F1}ms — 컷인 {cutStage} · 고비 {string.Join(",", moments)} · 프레임 {ms.Count} · 이펙트 CPU {Bolzena.Fx.FxRun.CpuMsThisFrame:F2}ms · 입자 {Bolzena.Fx.FxRun.Live} · 무대 {Bolzena.Fx.FxRun.Pooled}");
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
            played.Clear(); moments.Clear(); ms.Clear(); cutStage = ""; UltCutin.LastArt = null;
            bool shotCut = false, shotHit = false;
            string why = d.Battle.CanUlt(0, out var w) ? null : w;
            if (why == null)
            {
                d.RequestUlt(0);
                t = 0;
                while (!d.InUlt && t < 5f) { yield return null; t += Time.unscaledDeltaTime; }
                t = 0;
                while ((d.InUlt || !d.WaitingInput) && !d.Over && t < 25f)
                {
                    if (!shotCut && cutStage == "ult3_name") { shotCut = true; skip = 3; cap.Still($"{idx:000}_{hid}_a_cutin"); }
                    if (!shotHit && moments.Contains("ult_impact")) { shotHit = true; skip = 3; cap.Still($"{idx:000}_{hid}_b_impact"); }
                    yield return null; t += Time.unscaledDeltaTime;
                }
                if (!shotHit) { cap.Still($"{idx:000}_{hid}_b_after"); }
            }
            yield return new WaitForEndOfFrame();
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
            });
            File.AppendAllText(tsv, line + "\n");
            Debug.Log("[UltAudit] " + line.Replace('\t', '|'));
            idx++;
            yield return new WaitForSecondsRealtime(0.3f);
            if (idx >= end) { Debug.Log("[UltAudit] 끝"); Application.Quit(0); yield break; }
            Clock.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
