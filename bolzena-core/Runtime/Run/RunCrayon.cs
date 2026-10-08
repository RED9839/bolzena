using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Bolzena.Core
{
    /// <summary>
    /// 크레파스 보드 — 판 밖 영구 성장(2026-10-08 사용자, 설계 bolzena-content-v2/_measure/크레파스_시스템.md).
    /// 판이 끝날 때마다 크레파스 4등급(하급 · 중급 · 상급 · 최상급)을 받고(이기면 많이, 져도 닿은 층 · 학년만큼),
    /// 로비 「교주 능력치」 의 3열 × 4줄 보드 한 장을 칠한다.
    ///   1줄 공격 · 체력 · 방어 · 2줄 치명 확률 · 치명 피해 · 빈칸 — 능력치 칸은 같은 칸을 15단계까지 칠한다(1~5 하급 · 6~10 중급 · 11~13 상급 · 14~15 최상급).
    ///   3줄 시작 골드 · 신탁 확률 · 상점 빼기(상급) · 4줄 시작 학점 · 신탁 후보 · 엘리트 장비(최상급) — 한 번 칠하면 끝.
    /// 다 칠한 상태가 최대치. 표는 데이터 파일(runui Resources/RunUI/crayon.json) — Crayon.Parse 로 읽는다.
    /// </summary>
    public sealed class CrayonCell
    {
        public string Id, Name;
        /// <summary>효과 종류(Crayon.KINDS) — 빈칸은 「blank」(칠할 수 없고 장식만).</summary>
        public string Kind;
        /// <summary>한 단계의 값(능력치 비율 · 치명 %p · 골드 · 학점 · 확률 …).</summary>
        public double V;
        /// <summary>단계마다 드는 크레파스(등급 · 개수) — 단계 수 = Costs 수. 능력치 칸은 15단계(높은 단계일수록 높은 등급), 그 밖은 1단계.</summary>
        public List<CrayonCost> Costs = new();
        public int Levels => Costs.Count;
        public bool Blank => Kind == "blank";
        /// <summary>L단계(0부터)의 비용(넘으면 null).</summary>
        public CrayonCost CostAt(int level) => level >= 0 && level < Costs.Count ? Costs[level] : null;
    }

    /// <summary>한 단계의 비용 — 등급(0 하급 · 1 중급 · 2 상급 · 3 최상급) · 개수.</summary>
    public sealed class CrayonCost
    {
        public int Tier, N;
    }

    /// <summary>
    /// 판이 끝날 때 크레파스 — 하급: Low + 닿은 층 × LowPerFloor + 학년 × LowPerGrade(이기면 + LowWin)
    /// · 중급: 2층에 닿으면 MidFloor2 + 이긴 엘리트 × MidPerElite · 상급: 이긴 층 보스 × HighPerBoss + 완주 HighClear
    /// · 최상급: 졸업(6학년) TopGrad + 완주 TopClear.
    /// </summary>
    public sealed class CrayonEarn
    {
        public int Low = 2, LowPerFloor = 2, LowPerGrade = 1, LowWin = 3;
        public int MidFloor2 = 2, MidPerElite = 1;
        public int HighPerBoss = 1, HighClear = 1;
        public int TopGrad = 1, TopClear = 1;
    }

    /// <summary>크레파스 보드 한 장(Cols 열 — 칸은 표 차례대로 왼쪽 위부터).</summary>
    public sealed class CrayonTable
    {
        public CrayonEarn Earn = new();
        public int Cols = 3;
        public List<CrayonCell> Cells = new();
        public CrayonCell Cell(string id) => Cells.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>판 밖 영구 저장 — 등급별 가진 크레파스 · 받은 누계 · 칸마다 칠한 단계 · 끝낸 판 수.</summary>
    public sealed class CrayonSave
    {
        public int[] Have = new int[4];
        public int[] Earned = new int[4];
        public Dictionary<string, int> Level = new();
        public int Runs;
        public int LevelOf(string id) => Level.TryGetValue(id, out var v) ? v : 0;
    }

    public static class Crayon
    {
        public static readonly string[] TIERS = { "하급", "중급", "상급", "최상급" };
        /// <summary>
        /// 효과 종류 — atk · def · hp(사도 기본치 비율) · crit(치명 %p) · critDmg(치명 피해 배율 +) — 능력치
        /// · gold(시작 골드) · oracleChance(전투 신탁 확률 +) · removeCost(상점 카드 빼기 값 -) · credits(시작 학점) · oraclePick(신탁 후보 +) · eliteUp(엘리트 장비 한 등급 위 확률) · blank(빈칸).
        /// </summary>
        public static readonly string[] KINDS = { "atk", "def", "hp", "crit", "critDmg", "gold", "oracleChance", "removeCost", "credits", "oraclePick", "eliteUp", "blank" };
        /// <summary>능력치 상한(보드 전체) — 공격 · 방어 · 체력 +15%, 치명 +10%p, 치명 피해 +0.5.</summary>
        public static double CapOf(string kind) => kind switch { "atk" or "def" or "hp" => 0.15, "crit" => 10, "critDmg" => 0.5, _ => double.MaxValue };

        /// <summary>효과 글 — 「공격 +2%」 · 「치명 +1%p」 · 「시작 골드 +50」 ….</summary>
        public static string EffectOf(string kind, double v) => kind switch
        {
            "atk" => $"모든 사도 공격 +{v * 100:0.#}%", "def" => $"모든 사도 방어 +{v * 100:0.#}%", "hp" => $"파티 최대 HP +{v * 100:0.#}%",
            "crit" => $"치명 확률 +{v:0.#}%p", "critDmg" => $"치명 피해 +{v * 100:0.#}%",
            "gold" => $"모험 시작 골드 +{v:0}", "oracleChance" => $"전투 중 카드 신탁 확률 +{v * 100:0.#}%p", "removeCost" => $"상점 카드 빼기 -{v:0} 골드",
            "credits" => $"모험 시작 학점 +{v:0}", "oraclePick" => $"신탁 후보 +{v:0}", "eliteUp" => $"엘리트 장비가 {v * 100:0}% 확률로 한 등급 위",
            _ => "",
        };

        public static CrayonTable Parse(string json) => GameData.FromJson<CrayonTable>(json);

        /// <summary>표 검사 — 모르는 종류 · 같은 id · 비용 · 상한 넘음 · 칸 수가 Cols 로 안 나뉨.</summary>
        public static List<string> Check(CrayonTable t)
        {
            var e = new List<string>();
            var ids = new HashSet<string>();
            if (t.Cols <= 0 || t.Cells.Count == 0 || t.Cells.Count % t.Cols != 0) e.Add($"칸 {t.Cells.Count} 이 가로 {t.Cols} 로 나뉘지 않는다");
            foreach (var c in t.Cells)
            {
                if (!ids.Add(c.Id)) e.Add($"{c.Id}: id 가 겹친다");
                if (Array.IndexOf(KINDS, c.Kind) < 0) e.Add($"{c.Id}: 모르는 종류 {c.Kind}");
                if (c.Blank) continue;
                if (c.V <= 0 || c.Levels <= 0) e.Add($"{c.Id}: 값 · 단계가 없다");
                for (int i = 0; i < c.Costs.Count; i++)
                {
                    var k = c.Costs[i];
                    if (k.N <= 0 || k.Tier < 0 || k.Tier >= TIERS.Length) e.Add($"{c.Id} {i + 1}단계: 비용이 잘못되었다");
                    if (i > 0 && k.Tier < c.Costs[i - 1].Tier) e.Add($"{c.Id} {i + 1}단계: 등급이 앞 단계보다 낮다");
                }
            }
            foreach (var g in t.Cells.Where(c => !c.Blank).GroupBy(c => c.Kind))
                if (g.Sum(c => c.V * c.Levels) > CapOf(g.Key) + 1e-9) e.Add($"{g.Key}: 합이 상한 {CapOf(g.Key)} 을 넘는다");
            return e;
        }

        /// <summary>칠한 단계의 효과를 종류마다 더한다(상한까지). all 이면 다 칠한 상태(시뮬).</summary>
        public static Dictionary<string, double> Perks(CrayonTable t, CrayonSave s, bool all = false)
        {
            var o = new Dictionary<string, double>();
            foreach (var c in t.Cells)
            {
                if (c.Blank) continue;
                int lv = all ? c.Levels : Math.Min(c.Levels, s?.LevelOf(c.Id) ?? 0);
                if (lv > 0) o[c.Kind] = Math.Min(CapOf(c.Kind), (o.TryGetValue(c.Kind, out var v) ? v : 0) + c.V * lv);
            }
            return o;
        }

        /// <summary>판이 끝날 때 받는 크레파스(등급별 4칸) — 닿은 층 · 학년 · 이긴 엘리트 · 층 보스(RunState.Hist) · 완주.</summary>
        public static int[] EarnOf(CrayonTable t, RunState s, bool clear)
        {
            var e = t.Earn;
            int grade = Math.Max(Grades.FIRST, s.Grade);
            int floor = Math.Max(0, s.Floor);
            int elites = s.Hist.Count(h => h.Result == "win" && h.Kind == "elite");
            int bosses = s.Hist.Count(h => h.Result == "win" && h.Kind == "boss");
            return new[]
            {
                e.Low + floor * e.LowPerFloor + grade * e.LowPerGrade + (clear ? e.LowWin : 0),
                (floor >= 1 ? e.MidFloor2 : 0) + elites * e.MidPerElite,
                bosses * e.HighPerBoss + (clear ? e.HighClear : 0),
                (grade >= Grades.GRAD ? e.TopGrad : 0) + (clear ? e.TopClear : 0),
            };
        }

        /// <summary>판이 끝났다 — 더하고 받은 것을 돌려준다.</summary>
        public static int[] Earn(CrayonTable t, CrayonSave save, RunState s, bool clear)
        {
            var n = EarnOf(t, s, clear);
            for (int i = 0; i < 4; i++) { save.Have[i] += n[i]; save.Earned[i] += n[i]; }
            save.Runs++;
            return n;
        }

        /// <summary>다음 단계의 비용(다 칠했거나 빈칸이면 null).</summary>
        public static CrayonCost NextCost(CrayonCell c, CrayonSave s) => c.Blank ? null : c.CostAt(s.LevelOf(c.Id));

        /// <summary>다 칠하는 데 드는 등급별 합.</summary>
        public static int[] TotalCost(CrayonTable t)
        {
            var o = new int[4];
            foreach (var c in t.Cells) foreach (var k in c.Costs) o[k.Tier] += k.N;
            return o;
        }

        /// <summary>칠할 수 없으면 까닭.</summary>
        public static string WhyNot(CrayonTable t, CrayonSave s, string cellId)
        {
            var c = t.Cell(cellId);
            if (c == null) return "없는 칸입니다";
            if (c.Blank) return "빈칸은 칠할 수 없습니다";
            if (s.LevelOf(cellId) >= c.Levels) return "이미 다 칠한 칸입니다";
            var k = NextCost(c, s);
            if (s.Have[k.Tier] < k.N) return $"{TIERS[k.Tier]} 크레파스가 모자랍니다 ({s.Have[k.Tier]} / {k.N})";
            return null;
        }

        /// <summary>한 단계 칠한다.</summary>
        public static string Paint(CrayonTable t, CrayonSave s, string cellId)
        {
            var why = WhyNot(t, s, cellId);
            if (why != null) return why;
            var c = t.Cell(cellId);
            var k = NextCost(c, s);
            s.Have[k.Tier] -= k.N;
            s.Level[cellId] = s.LevelOf(cellId) + 1;
            return null;
        }

        public static bool Done(CrayonTable t, CrayonSave s) => t.Cells.All(c => c.Blank || s.LevelOf(c.Id) >= c.Levels);

        // ── 진행 코드(내보내기 · 불러오기) ──
        const string HEAD = "BZP1";

        /// <summary>진행 코드 — 「BZP1-(base64 JSON)-(체크섬 8자리)」. 붙여 넣어 다른 기기로 옮긴다.</summary>
        public static string Export(CrayonSave s)
        {
            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { h = s.Have, e = s.Earned, l = s.Level, r = s.Runs })));
            return $"{HEAD}-{body}-{Sum(body):x8}";
        }

        /// <summary>진행 코드를 읽는다 — 틀리면(머리 · 체크섬 · 꼴 · 모르는 칸 · 단계 넘음 · 음수) null 과 까닭.</summary>
        public static (CrayonSave save, string why) Import(string code, CrayonTable t = null)
        {
            code = (code ?? "").Trim();
            var parts = code.Split('-');
            if (parts.Length != 3 || parts[0] != HEAD) return (null, "진행 코드가 아닙니다");
            if (!uint.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out var sum) || sum != Sum(parts[1])) return (null, "코드가 손상되었습니다(체크섬이 맞지 않음)");
            try
            {
                var j = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])));
                var s = new CrayonSave
                {
                    Have = j["h"].ToObject<int[]>(), Earned = j["e"].ToObject<int[]>(), Runs = (int)j["r"],
                    Level = j["l"].ToObject<Dictionary<string, int>>() ?? new Dictionary<string, int>(),
                };
                if (s.Have == null || s.Have.Length != 4 || s.Earned == null || s.Earned.Length != 4) return (null, "코드의 꼴이 잘못되었습니다");
                if (s.Have.Any(x => x < 0) || s.Earned.Any(x => x < 0) || s.Runs < 0 || s.Level.Values.Any(x => x < 0)) return (null, "코드의 값이 잘못되었습니다");
                if (t != null && s.Level.Any(kv => t.Cell(kv.Key) == null || t.Cell(kv.Key).Blank || kv.Value > t.Cell(kv.Key).Levels)) return (null, "모르는 칸이나 단계를 넘는 칸이 있습니다");
                return (s, null);
            }
            catch (Exception) { return (null, "코드를 읽을 수 없습니다"); }
        }

        /// <summary>FNV-1a 32비트.</summary>
        static uint Sum(string s)
        {
            uint h = 2166136261;
            foreach (var ch in s) { h ^= ch; h = unchecked(h * 16777619); }
            return h;
        }
    }

    public sealed partial class Run
    {
        /// <summary>크레파스 보드 효과 값(없으면 0) — RunState.Perks.</summary>
        public double Perk(string kind) => S.Perks != null && S.Perks.TryGetValue(kind, out var v) ? v : 0;

        /// <summary>
        /// 새 판에 크레파스 보드 효과를 건다(Run.New 바로 뒤에 한 번) — 시작 골드 · 학점 · 최대 HP 는 지금,
        /// 공격 · 방어 · 치명은 싸움을 열 때(GradeGrowth), 치명 피해는 전투(BattleSetup.CritDmg), 나머지는 판 도중 Perk 로 읽는다.
        /// 빈 표면 아무것도 안 한다(옛 판 · 업그레이드 없음 — 난수 · 판이 예전과 같다).
        /// </summary>
        public void ApplyPerks(Dictionary<string, double> perks)
        {
            if (perks == null || perks.Count == 0) return;
            S.Perks = new Dictionary<string, double>(perks);
            S.Gold += (int)Perk("gold");
            if (Perk("credits") > 0) { S.Credits += (int)Perk("credits"); S.Grade = Grades.Of(S.Credits); }
            if (Perk("hp") > 0) ShiftHp(Num.Round(S.Party.Sum(k => Data.Hero(k)?.Hp ?? 0) * Perk("hp")));
        }

        /// <summary>엘리트 장비 한 등급 위 — 무게표의 등급을 하나씩 올린다(전설은 그대로).</summary>
        static Dictionary<string, int> GradeUp(Dictionary<string, int> w)
        {
            var o = new Dictionary<string, int>();
            foreach (var kv in w)
            {
                int i = Array.IndexOf(R.GRADES, kv.Key);
                var g = i >= 0 && i < R.GRADES.Length - 1 ? R.GRADES[i + 1] : kv.Key;
                o[g] = (o.TryGetValue(g, out var v) ? v : 0) + kv.Value;
            }
            return o;
        }
    }
}
