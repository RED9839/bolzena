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
        public string SoundMode;                     // events · fallback
        public List<KeyValuePair<int, string>> Plays = new List<KeyValuePair<int, string>>();   // (ms, 파일 줄기 — 확장자 뺀 것). 맞는 소리 칸은 뺐다
        public bool Moves => MoveType != "none";
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
                heroes[kv.Key] = list;
            }
            return true;
        }

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
                        if (f == null || (note != null && note.Contains("맞는 소리"))) continue;   // 맞는 소리는 맞는 순간(Impact)이 낸다
                        r.Plays.Add(new KeyValuePair<int, string>(I(p, "at"), System.IO.Path.GetFileNameWithoutExtension(f)));
                    }
            }
            return r;
        }

        // 그 사도의 원작 갈래들(없으면 null)
        public static List<UltWay> Ways(string heroKey)
        {
            Load();
            return heroKey != null && heroes.TryGetValue(heroKey, out var l) && l.Count > 0 ? l : null;
        }
    }
}
