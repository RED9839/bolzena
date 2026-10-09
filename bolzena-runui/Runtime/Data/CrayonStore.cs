using System;
using System.Collections.Generic;
using Bolzena.Core;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 크레파스 보드(교주 보드)의 판 밖 영구 저장(코어 RunCrayon.cs) — 표는 Resources/RunUI/crayon.json, 진행은 PlayerPrefs 「bz.crayon」(JSON).
    /// -save 로 따로 둔 시험 실행은 그 이름을 붙인 키를 써서 사용자의 진행을 건드리지 않는다.
    /// </summary>
    public static class CrayonStore
    {
        static CrayonTable table;
        static CrayonSave save;

        public static CrayonTable Table
        {
            get
            {
                if (table != null) return table;
                var ta = Resources.Load<TextAsset>("RunUI/crayon");
                try { table = ta != null ? Crayon.Parse(ta.text) : new CrayonTable(); }
                catch (Exception e) { Debug.LogWarning("[Crayon] 표를 못 읽음: " + e.Message); table = new CrayonTable(); }
                foreach (var why in Crayon.Check(table)) Debug.LogWarning("[Crayon] " + why);
                return table;
            }
        }

        static string Key
        {
            get
            {
                var a = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(a, "-save");
                return i >= 0 && i < a.Length - 1 ? "bz.crayon." + a[i + 1] : "bz.crayon";
            }
        }

        public static CrayonSave Save
        {
            get
            {
                if (save != null) return save;
                try { save = JsonUtility.FromJson<Wrap>(PlayerPrefs.GetString(Key, ""))?.ToSave(); } catch (Exception) { save = null; }
                if (save == null) return save = new CrayonSave();
                if (Crayon.Migrate(Table, save)) Write();   // 옛 보드(한 번 칸) 저장 — 칠했던 칸을 같은 효과의 단계로 옮긴다
                return save;
            }
        }

        [Serializable] class Wrap
        {
            public int[] have = new int[4], earned = new int[4]; public int runs;
            /// <summary>보드 판(Crayon.BOARD_VER) — 옛 저장엔 없어 0 이 된다.</summary>
            public int ver;
            public List<string> keys = new List<string>(); public List<int> levels = new List<int>();
            public CrayonSave ToSave()
            {
                var s = new CrayonSave { Runs = runs, Ver = ver };
                if (have != null && have.Length == 4) s.Have = have;
                if (earned != null && earned.Length == 4) s.Earned = earned;
                for (int i = 0; keys != null && levels != null && i < keys.Count && i < levels.Count; i++) s.Level[keys[i]] = levels[i];
                return s;
            }
            public static Wrap Of(CrayonSave s) => new Wrap { have = s.Have, earned = s.Earned, runs = s.Runs, ver = s.Ver, keys = new List<string>(s.Level.Keys), levels = new List<int>(s.Level.Values) };
        }

        public static void Write()
        {
            try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Wrap.Of(Save))); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("[Crayon] 저장 실패: " + e.Message); }
        }

        /// <summary>새 판에 걸 보드 효과(칠한 단계 전부).</summary>
        public static Dictionary<string, double> Perks => Crayon.Perks(Table, Save);

        /// <summary>판이 끝났다 — 받은 크레파스(등급별 4칸). 한 판에 한 번만: 같은 씨앗 · 같은 끝은 다시 주지 않는다(빈 칸 넷).</summary>
        public static int[] EarnRun(RunState s, bool clear)
        {
            string mark = $"{s.Seed}:{s.Hist.Count}:{clear}";
            if (PlayerPrefs.GetString(Key + ".last", "") == mark) return new int[4];
            var n = Crayon.Earn(Table, Save, s, clear);
            PlayerPrefs.SetString(Key + ".last", mark);
            Write();
            return n;
        }

        /// <summary>보드 한 칸을 한 단계 칠한다.</summary>
        public static string Paint(string cellId)
        {
            var why = Crayon.Paint(Table, Save, cellId);
            if (why == null) Write();
            return why;
        }

        /// <summary>진행 코드 — 보드(크레파스 · 단계) + 편성 기억(PartyStore — 프리셋 · 최근 · 완주 기록). 설정(소리 · 화면)은 기기마다라 넣지 않는다.</summary>
        public static string Export() => Crayon.Export(Save, PartyStore.Dump());

        /// <summary>진행 코드를 받아 들인다 — 틀리면 까닭(지금 진행은 그대로).</summary>
        public static string Import(string code)
        {
            var (s, extra, why) = Crayon.ImportAll(code, Table);
            if (why != null) return why;
            save = s;
            Write();
            if (extra != null) PartyStore.Load(extra);   // 옛 1판 코드(보드만)면 편성 기억은 그대로
            return null;
        }

        /// <summary>시험 도구 — 등급마다 크레파스를 더 준다.</summary>
        public static void Give(int low, int mid = 0, int high = 0, int top = 0) { var h = Save.Have; h[0] += low; h[1] += mid; h[2] += high; h[3] += top; Write(); }
        /// <summary>시험 도구 — 처음부터.</summary>
        public static void Reset() { save = new CrayonSave(); Write(); }
    }
}
