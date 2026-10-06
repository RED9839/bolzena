using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 고학년 몸짓 표 — Runtime/Motion/ult_motion.json(「모션 분석」이 원작 스파인 · 소리를 재서 만든 것, 여기서는 읽기만. 규칙은 Docs/고학년모션표.md).
    // FxImport 가 Resources/<Root>/ult_motion.json 으로 옮겨 둔다. 사도마다 원작 갈래(조각 이름 — 고리 횟수까지 펼친 것) · 때리는 순간 ·
    // 이동(dash · teleport · leap · none — 가기 · 닿기 · 타격 · 돌아오기 시각) · 효과음(파일 · 트는 시각) · 전체 길이(ms, 원작 1배속)
    public class UltWay
    {
        public List<string> Names = new List<string>();
        public int MotionMs, TotalMs;
        public int At, End;
        public List<int> Marks = new List<int>();
        public string MoveType = "none";             // none · dash · teleport · leap
        public int GoMs, LandMs, HitMs, ReturnMs, BackMs;
        public int? HomeMs;                          // leap — 이 때 제자리로(에르핀(왕도))
        public float Approach;
        public string Dest = "front";                // 가는 자리 — front 대상 앞 · middle 무리 가운데 · behind 무리 뒤 · center 화면 가운데
        public string SoundMode;                     // events · fallback
        public List<KeyValuePair<int, string>> Plays = new List<KeyValuePair<int, string>>();
        public List<int> HitSnd = new List<int>();   // 원작 「맞는 소리」 칸이 울리는 때(ms) — 원작이 실제로 때리는 순간들(림(혼돈) 1100 · 2867)   // (ms, 파일 줄기 — 확장자 뺀 것). 맞는 소리 칸은 뺐다
        public bool Moves => MoveType != "none";

        // 원작 타격 순간들(ms, 1배속) — 맞는 소리 칸이 둘 이상이면 그것, 아니면 때리는 창의 표시(marks)를 250ms 안쪽끼리 묶은 것(많아야 8)
        public List<int> Strikes()
        {
            if (HitSnd.Count >= 2) return new List<int>(HitSnd);
            var src = new List<int>(Marks.Count > 0 ? Marks : new List<int> { HitMs > 0 ? HitMs : At });
            if (Moves) { src.RemoveAll(m => m < HitMs); src.Insert(0, HitMs); }   // 이동형은 닿은 뒤가 첫 타격
            var res = new List<int>();
            foreach (var m in src) if (res.Count == 0 || m - res[res.Count - 1] >= 250) res.Add(m);
            return res.Count > 8 ? res.GetRange(0, 8) : res;
        }
    }

    public static class UltMotion
    {
        static Dictionary<string, List<UltWay>> heroes;

        public static bool Load()
        {
            if (heroes != null) return true;
            var ta = Resources.Load<TextAsset>(FxLibrary.Root + "/ult_motion");
            heroes = new Dictionary<string, List<UltWay>>();
            if (ta == null) return false;
            var root = (Dictionary<string, object>)MiniJson.Parse(ta.text);
            if (!(root.TryGetValue("heroes", out var ho) && ho is Dictionary<string, object> hs)) return false;
            foreach (var kv in hs)
            {
                var h = (Dictionary<string, object>)kv.Value;
                var list = new List<UltWay>();
                if (h.TryGetValue("ways", out var wo) && wo is List<object> ways)
                    foreach (Dictionary<string, object> w in ways) list.Add(Way(w));
                bool old = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldsync") >= 0;   // 고치기 전 기준선(점검)
                if (!old && MOVE_FIX.TryGetValue(kv.Key, out var fix)) foreach (var w in list) fix(w);
                heroes[kv.Key] = list;
            }
            return true;
        }

        // 이동 고침 — 표(ult_motion.json, 모션 분석 담당 · 읽기만)가 제자리로 잡았지만 원작은 옮겨 가는 사도(2026-10-06 사용자 「로니는 적 무리 가운데로 떠서 친다」).
        // 근거: 원작 애니에서 몸이 사라지는 창(hidden) · 나무위키 설명(「순간 이동한 뒤」 · 「날아가」). 시각은 ms, 1배속
        static void Mv(UltWay w, string type, int go, int land, int hit, int ret, int back, string dest)
        { w.MoveType = type; w.GoMs = go; w.LandMs = land; w.HitMs = hit; w.ReturnMs = ret; w.BackMs = back; w.Dest = dest; }
        static readonly Dictionary<string, System.Action<UltWay>> MOVE_FIX = new Dictionary<string, System.Action<UltWay>>
        {
            { "로니", w => Mv(w, "teleport", 500, 667, 1950, 4333, 0, "middle") },          // 몸 사라짐 500~667 · 설명 「순간 이동한 뒤 6회」 → 적 무리 가운데에서 여러 방향
            { "에스피", w => Mv(w, "teleport", 733, 1050, 1366, 2566, 0, "front") },         // 몸 사라짐 733~1366(가기) · 2333~2799(돌아오기) · 「순간 이동한 뒤」
            { "니콜", w => Mv(w, "dash", 500, 900, 933, w.MotionMs, 300, "middle") },        // 「가운데에 있는 적에게 날아가 … 시전 위치로 돌아온다」
            { "베니_베니", w => Mv(w, "dash", 0, 900, 1100, w.MotionMs, 300, "front") },     // 「적에게 날아가 꿀단지를 내려쳐」
            { "네르_빡침", w => Mv(w, "leap", 2700, 2900, 2967, 4300, 0, "front") },       // 원작 영상: 날개 섬광 뒤 적에게 뛰어들어 한 번 치고 날개 모습으로 돌아와 선다
        };

        static int I(Dictionary<string, object> d, string k, int def = 0) => d != null && d.TryGetValue(k, out var v) && v is double x ? Mathf.RoundToInt((float)x) : def;

        static UltWay Way(Dictionary<string, object> w)
        {
            var r = new UltWay();
            if (w.TryGetValue("names", out var n) && n is List<object> nl) foreach (var x in nl) r.Names.Add((string)x);
            r.MotionMs = I(w, "motionMs"); r.TotalMs = I(w, "totalMs", r.MotionMs);
            if (w.TryGetValue("strike", out var so) && so is Dictionary<string, object> s)
            {
                r.At = I(s, "at"); r.End = I(s, "end", r.At);
                if (s.TryGetValue("marks", out var mo) && mo is List<object> ml) foreach (var x in ml) if (x is double d) r.Marks.Add(Mathf.RoundToInt((float)d));
            }
            if (w.TryGetValue("move", out var mv) && mv is Dictionary<string, object> m)
            {
                r.MoveType = m.TryGetValue("type", out var t) && t is string ts ? ts : "none";
                r.GoMs = I(m, "goMs"); r.LandMs = I(m, "landMs", r.GoMs); r.HitMs = I(m, "hitMs", r.LandMs);
                r.ReturnMs = I(m, "returnMs", r.MotionMs); r.BackMs = I(m, "backMs");
                if (m.TryGetValue("homeMs", out var hm) && hm is double hd) r.HomeMs = Mathf.RoundToInt((float)hd);
                r.Approach = m.TryGetValue("approach", out var ap) && ap is double a ? (float)a : 0;
            }
            if (w.TryGetValue("sound", out var sd) && sd is Dictionary<string, object> snd)
            {
                r.SoundMode = snd.TryGetValue("mode", out var md) ? md as string : null;
                if (snd.TryGetValue("plays", out var po) && po is List<object> pl)
                    foreach (Dictionary<string, object> p in pl)
                    {
                        var f = p.TryGetValue("file", out var fo) ? fo as string : null;
                        var note = p.TryGetValue("note", out var no) ? no as string : null;
                        if (note != null && note.Contains("맞는 소리")) { r.HitSnd.Add(I(p, "at")); continue; }   // 맞는 소리는 맞는 순간(Impact)이 낸다
                        if (f == null) continue;
                        r.Plays.Add(new KeyValuePair<int, string>(I(p, "at"), System.IO.Path.GetFileNameWithoutExtension(f)));
                    }
            }
            return r;
        }

        // ── 원작 타수(ult_hits.json — 조사 담당의 기준표에서 Tools/ult_hits_json.py 가 만든 것) ──
        public class UltHits { public int N; public int Ours; public string Verdict, Why; public List<int> Times = new List<int>(); }
        static Dictionary<string, UltHits> hits;
        public static UltHits Hits(string heroKey)
        {
            if (hits == null)
            {
                hits = new Dictionary<string, UltHits>();
                var ta = Resources.Load<TextAsset>(FxLibrary.Root + "/ult_hits");
                if (ta != null && MiniJson.Parse(ta.text) is Dictionary<string, object> root && root.TryGetValue("heroes", out var ho) && ho is Dictionary<string, object> hs)
                    foreach (var kv in hs)
                    {
                        var d = (Dictionary<string, object>)kv.Value;
                        var h = new UltHits { N = I(d, "n"), Verdict = d.TryGetValue("verdict", out var v) ? v as string : "", Why = d.TryGetValue("why", out var w) ? w as string : "" };
                        int.TryParse(d.TryGetValue("ours", out var o) ? o as string : "0", out h.Ours);
                        if (d.TryGetValue("times", out var t) && t is List<object> tl) foreach (var x in tl) if (x is double dx) h.Times.Add(Mathf.RoundToInt((float)dx));
                        hits[kv.Key] = h;
                    }
            }
            return heroKey != null && hits.TryGetValue(heroKey, out var r) ? r : null;
        }

        // 그 사도의 원작 갈래들(없으면 null)
        public static List<UltWay> Ways(string heroKey)
        {
            Load();
            return heroKey != null && heroes.TryGetValue(heroKey, out var l) && l.Count > 0 ? l : null;
        }
    }
}
