using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 편성 기억(판 밖 영구 저장 — PlayerPrefs): 최근 편성 · 프리셋 셋 · 사도별 완주 횟수. 사도는 로스터 key 로 적는다(코어 id 가 바뀌어도 이름표는 그대로).
    /// -save 로 따로 둔 시험 실행은 그 이름을 붙인 키를 써서 사용자의 기록을 건드리지 않는다.
    /// </summary>
    public static class PartyStore
    {
        public const int PresetCount = 5;

        static string Tail
        {
            get
            {
                var a = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(a, "-save");
                return i >= 0 && i < a.Length - 1 ? "." + a[i + 1] : "";
            }
        }
        static string K(string n) => "bz.party." + n + Tail;

        /// <summary>저장된 key 목록 → 지금 고를 수 있는 사도만, 최대 3(자리 순서 · 빈 자리는 null).</summary>
        static string[] Parse(string s)
        {
            var o = new string[3];
            if (string.IsNullOrEmpty(s)) return o;
            var parts = s.Split('|');
            for (int i = 0; i < 3 && i < parts.Length; i++)
            {
                var h = parts[i].Length > 0 ? Roster.ByKey(parts[i]) : null;
                o[i] = h != null && h.Playable ? h.key : null;
            }
            return o;
        }
        static string Join(IEnumerable<string> keys) => string.Join("|", keys.Take(3).Select(k => k ?? ""));

        public static string[] Recent => Parse(PlayerPrefs.GetString(K("recent"), ""));
        public static void SetRecent(IEnumerable<string> keys) { PlayerPrefs.SetString(K("recent"), Join(keys)); PlayerPrefs.Save(); }

        public static string[] Preset(int i) => Parse(PlayerPrefs.GetString(K("preset" + i), ""));
        public static bool HasPreset(int i) => Preset(i).Any(k => k != null);
        public static void SetPreset(int i, IEnumerable<string> keys) { PlayerPrefs.SetString(K("preset" + i), Join(keys)); PlayerPrefs.Save(); }
        public static void ClearPreset(int i) { PlayerPrefs.DeleteKey(K("preset" + i)); PlayerPrefs.Save(); }

        /// <summary>프리셋에 붙인 성격 아이콘(5속성 중 하나 — 이 약점 속성용 편성이라는 표). 없으면 null(번호만).</summary>
        public static string Tag(int i) { var t = PlayerPrefs.GetString(K("tag" + i), ""); return t.Length > 0 ? t : null; }
        public static void SetTag(int i, string nature) { if (nature == null) PlayerPrefs.DeleteKey(K("tag" + i)); else PlayerPrefs.SetString(K("tag" + i), nature); PlayerPrefs.Save(); }

        // ── 사도별 완주 횟수 — 「key=횟수;key=횟수」 ──
        static Dictionary<string, int> clears;
        static Dictionary<string, int> Clears
        {
            get
            {
                if (clears != null) return clears;
                clears = new Dictionary<string, int>();
                foreach (var kv in PlayerPrefs.GetString(K("clears"), "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var p = kv.Split('=');
                    if (p.Length == 2 && int.TryParse(p[1], out var n) && n > 0) clears[p[0]] = n;
                }
                return clears;
            }
        }
        public static int Cleared(string heroKey) => heroKey != null && Clears.TryGetValue(heroKey, out var n) ? n : 0;

        /// <summary>판을 끝까지 깼다 — 그 파티 셋에 +1. 한 판에 한 번만(같은 씨앗 · 같은 끝은 다시 세지 않는다).</summary>
        public static void CountClear(RunState s)
        {
            if (s == null || s.Party == null) return;
            string mark = $"{s.Seed}:{s.Hist.Count}";
            if (PlayerPrefs.GetString(K("lastclear"), "") == mark) return;
            PlayerPrefs.SetString(K("lastclear"), mark);
            foreach (var id in s.Party)
            {
                var h = Roster.OfCore(id);
                if (h == null || !h.Playable) continue;
                Clears[h.key] = Cleared(h.key) + 1;
            }
            Flush();
            CountVillage(s);
        }

        // ── 마을별 완주 기록(마을 고르기 노드의 띠, 2026-10-21) — 「마을=횟수;…」 · 「마을=단계;…」. CountClear 가 같은 판에 한 번만 부른다 ──
        static Dictionary<string, int> Map(string n)
        {
            var o = new Dictionary<string, int>();
            foreach (var kv in PlayerPrefs.GetString(K(n), "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var p = kv.Split('=');
                if (p.Length == 2 && int.TryParse(p[1], out var v) && v > 0) o[p[0]] = v;
            }
            return o;
        }
        static void SetMap(string n, Dictionary<string, int> m) { PlayerPrefs.SetString(K(n), string.Join(";", m.Select(kv => kv.Key + "=" + kv.Value))); PlayerPrefs.Save(); }
        /// <summary>그 마을 완주 횟수(이 기기).</summary>
        public static int VillageClears(string village) => village != null && Map("vilclears").TryGetValue(village, out var n) ? n : 0;
        /// <summary>그 마을에서 완주한 가장 높은 단계(성장 켬 · 이 기기). 0 = 없음.</summary>
        public static int VillageBest(string village) => village != null && Map("vilbest").TryGetValue(village, out var n) ? n : 0;
        static void CountVillage(RunState s)
        {
            if (string.IsNullOrEmpty(s.Village)) return;
            var c = Map("vilclears"); c[s.Village] = (c.TryGetValue(s.Village, out var n) ? n : 0) + 1; SetMap("vilclears", c);
            if (s.Tier <= 0) return;
            var b = Map("vilbest");
            if (!b.TryGetValue(s.Village, out var t) || s.Tier > t) { b[s.Village] = s.Tier; SetMap("vilbest", b); }
        }
        static void Flush() { PlayerPrefs.SetString(K("clears"), string.Join(";", Clears.Select(kv => kv.Key + "=" + kv.Value))); PlayerPrefs.Save(); }

        /// <summary>시험 도구 — 완주 횟수를 직접 정한다.</summary>
        public static void SetCleared(string heroKey, int n) { if (n > 0) Clears[heroKey] = n; else Clears.Remove(heroKey); Flush(); }
        // ── 진행 코드(코어 ProgressCode 의 덧붙임 — 설정 → 「진행 코드」, Flow.ProgressCode.cs) ──
        /// <summary>진행 코드 — 편성 기억(프리셋 · 최근 · 완주 기록). 설정(소리 · 화면)은 기기마다라 넣지 않는다.</summary>
        public static string ExportCode() => ProgressCode.Export(Dump());
        /// <summary>진행 코드를 받아 들인다 — 틀리면 까닭(지금 기억은 그대로).</summary>
        public static string ImportCode(string code)
        {
            var (extra, why) = ProgressCode.Import(code);
            if (why != null) return why;
            Load(extra);
            return null;
        }
        /// <summary>진행 코드에 담는 편성 기억 — 최근 편성 · 프리셋 5칸(사도 셋 · 성격 표) · 사도별 완주 기록. 저장된 글 그대로(빈 칸은 뺀다).</summary>
        static readonly string[] CodeKeys = { "recent", "preset1", "preset2", "preset3", "preset4", "preset5", "tag1", "tag2", "tag3", "tag4", "tag5", "clears", "vilclears", "vilbest" };
        public static Dictionary<string, string> Dump()
        {
            var o = new Dictionary<string, string>();
            foreach (var n in CodeKeys) { var v = PlayerPrefs.GetString(K(n), ""); if (v.Length > 0) o[n] = v; }
            return o;
        }
        /// <summary>진행 코드의 편성 기억으로 덮어쓴다(코드에 없는 칸은 지운다). 모르는 이름은 버린다.</summary>
        public static void Load(Dictionary<string, string> d)
        {
            if (d == null) return;
            foreach (var n in CodeKeys)
            {
                if (d.TryGetValue(n, out var v) && !string.IsNullOrEmpty(v)) PlayerPrefs.SetString(K(n), v);
                else PlayerPrefs.DeleteKey(K(n));
            }
            clears = null;
            PlayerPrefs.Save();
        }

        /// <summary>시험 도구 — 이 저장의 편성 기억을 모두 지운다.</summary>
        public static void ResetAll()
        {
            clears = null;
            foreach (var n in new[] { "recent", "clears", "lastclear", "preset1", "preset2", "preset3", "preset4", "preset5", "tag1", "tag2", "tag3", "tag4", "tag5", "vilclears", "vilbest" }) PlayerPrefs.DeleteKey(K(n));
            PlayerPrefs.Save();
        }
    }
}
