using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 보스 클론의 고학년(사용자 2026-10-06) — 1층 · 2층 보스 사도 클론이 그 사도의 고학년을 보스 특수 공격으로 쓴다.
    /// 예고(charge — 이름은 고학년 이름, 다음 턴 피해를 보인다) → 다음 턴 사용(ult). 예고 · 사용 턴에 격파(또는 기절)하면 끊긴다.
    /// 처음 예고는 FIRST 턴(같은 싸움 두 번째 클론은 한 턴 늦게), 그 뒤 EVERY 턴마다.
    /// 효과는 사도 고학년을 적 쪽으로 뒤집는다 — 적에게 피해 → 파티에게 피해 · 아군 버프 → 적 전체 버프 · 적 디버프 → 파티 디버프.
    /// 피해는 보스 등급(층)의 기준 피해 × 고학년 피해 비율(√, 0.8~1.25) — 사도 공격력과 상관없다. 사도 고유 자원 · 덱 조작은 뺀다.
    /// 이상하게 바뀌는 사도는 FIX(손보정 표)로 고친다. 변환표: Plan · Table(bz bossult).
    /// </summary>
    public static class BossUlt
    {
        /// <summary>켜기 — 시뮬 전후 비교용(bz sim --bossult 0).</summary>
        public static bool On = true;
        /// <summary>피해 배율 — 계수 찾기용(bz sim --bossultx 0.7). 보통 1.</summary>
        public static double Scale = 1;
        public const int FIRST = 2, EVERY = 4;
        /// <summary>층(1 · 2)마다 고학년 한 번의 기준 피해(원값 — 층 피해 배율은 전투가 따로 곱한다). 전체 공격이면 FOE_ALL_X 로 나눠 적는다.</summary>
        /// 2026-10-06 시뮬(9000판, 같은 씨앗) — 300 · 360 이면 완주 20.8% → 15.4%(-5.4%p), × 0.75 면 -1.9%p, × 0.65 면 -0.1%p → 210 · 250.
        /// 2026-10-07 사용자 「고학년이 너무 약한 감」 — 평소 수는 무작위 · 예고는 고학년만으로 바꾸며 300 · 360 으로 올렸다(난이도는 보스 몸 HP · 평소 피해로 맞춘다).
        public static readonly int[] RAW = { 0, 300, 360 };
        /// <summary>피해 없는 고학년(지원형)에 붙이는 피해 비율(2026-10-07 0.6 → 0.85).</summary>
        public const double SUPPORT_HIT = 0.85;
        /// <summary>층마다 방어(실드 → 보스 방어) · 회복(치유 → 적 회복). 2026-10-07 100 · 140 / 120 · 160 → 160 · 220 / 180 · 240.</summary>
        public static readonly int[] BLOCK = { 0, 160, 220 }, HEAL = { 0, 180, 240 };
        /// <summary>끊는 보상 — 예고 · 사용 턴에 격파 · 기절로 끊으면 그 보스에 취약(겹).</summary>
        public const int CUT_VULN = 2;
        /// <summary>피해 비율 기준(135명 고학년 피해 비율 합의 가운데 값).</summary>
        public const double T_REF = 1.8;
        public const int MULTI_MAX = 5;

        /// <summary>파티에 거는 디버프에 더하는 겹(2026-10-07 — 원작 값 + 1, 상한까지).</summary>
        public const int DEBUFF_PLUS = 1;
        /// <summary>파티에 거는 디버프 — 사도 고학년이 적에게 거는 것 → 파티에(값 + DEBUFF_PLUS, 상한).</summary>
        static readonly Dictionary<string, (string id, int cap)> DEBUFF = new()
        {
            ["약화"] = ("약화", 3), ["취약"] = ("취약", 3), ["고통"] = ("고통", 4), ["충격"] = ("충격", 3), ["손상"] = ("손상", 3),
            ["균열"] = ("균열", 4), ["잔불"] = ("고통", 3), ["그을림"] = ("고통", 3), ["미끄러움"] = ("미끄러움", 2),
        };
        /// <summary>둔화 · 기절(적의 차례를 늦추는 것) → 파티의 다음 턴 AP -1.</summary>
        static readonly HashSet<string> TO_JAM = new() { "둔화", R.STUN, "행동 둔화" };
        /// <summary>적 쪽 버프 — 사도 고학년이 파티에 거는 것 → 적 전체에(값 상한). 없는 것은 피해 감소 2.</summary>
        static readonly Dictionary<string, (string id, int cap)> BUFF = new()
        {
            ["사기"] = ("사기", 2), ["불굴"] = ("불굴", 3), ["결의"] = ("결의", 3), ["피해 감소"] = ("피해 감소", 3), ["면역"] = ("면역", 1),
        };

        /// <summary>손보정 — 자동 변환이 어색한 사도. Mul 피해 배율 · Hit 피해 꼴 강제(attackAll · attack · multi) · Add 덧붙일 수 · Drop 뺄 수 종류 · Note 까닭.</summary>
        public sealed class Fix
        {
            public double Mul = 1;
            public string Hit;
            public int N;
            public List<Intent> Add;
            public HashSet<string> Drop;
            public string Note;
        }

        static Intent Db(string id, int v) => new Intent { T = "debuff", Id = id, V = v };
        static Intent Bf(string id, int v) => new Intent { T = "buff", Id = id, V = v, All = true };

        /// <summary>손보정 표(사도 키). 변환표(_measure/보스_고학년.md)를 보고 고친다.</summary>
        public static readonly Dictionary<string, Fix> FIX = new()
        {
            // 치유가 몸통인 고학년 — 기본 적 회복(층 HEAL)만으론 원작 느낌이 안 산다. 크게 한 번 더 회복
            ["카렌"] = new Fix { Add = new List<Intent> { new Intent { T = "heal", V = 200 } }, Note = "손보정: 방송 회복이 몸통 — 적 회복 한 번 더(200)" },
            ["큐이"] = new Fix { Add = new List<Intent> { new Intent { T = "heal", V = 200 } }, Note = "손보정: 오이 회복이 몸통 — 적 회복 한 번 더(200)" },
            // 고유 효과 겹마다 치는 고학년 — 비율만 세면 겹(4) 몫이 빠진다
            ["쥬비"] = new Fix { Mul = 1.2, Note = "손보정: 「벌」 겹마다 피해 — 겹 4 몫으로 피해 × 1.2" },
            ["칸타"] = new Fix { Mul = 1.1, Note = "손보정: 「쇠팽이」 겹마다 전체 피해 — 피해 × 1.1" },
            ["피라"] = new Fix { Mul = 1.1, Note = "손보정: 「도금 연금술」 겹마다 피해 — 피해 × 1.1" },
            // 변신이 고학년의 절반인 사도 — 변신은 뺐으니 그 자리에 사기
            ["네르_빡침"] = new Fix { Add = new List<Intent> { new Intent { T = "buff", Id = "사기", V = 1, All = true } }, Note = "손보정: 「성전 모드」 변신 대신 적 전체 사기 1" },
            ["디아나_왕년"] = new Fix { Add = new List<Intent> { new Intent { T = "buff", Id = "사기", V = 1, All = true } }, Note = "손보정: 「맨주먹 전성기」 변신 대신 적 전체 사기 1" },
        };

        /// <summary>그 보스 클론(적 id)이 선 층(1 · 2). 마을 보스 줄에서 찾고(빌린 몸은 몸 id), 없으면 사도 성급(3성 → 2층).</summary>
        public static int Tier(GameData d, string enemyId)
        {
            string id = enemyId;
            if (id != null && id.StartsWith(GameData.CLONE_MARK, StringComparison.Ordinal)) { var p = id.Split('~'); if (p.Length >= 3) id = p[2]; }
            foreach (var v in d.Villages.Values)
                for (int i = 0; i < v.Floors.Count; i++)
                    if (v.Floors[i].Boss != null && v.Floors[i].Boss.Contains(id)) return Math.Min(2, i + 1);
            var h = d.Hero(d.Enemy(enemyId)?.Clone);
            return h != null && h.Star >= 3 ? 2 : 1;
        }

        /// <summary>그 적이 고학년을 쓰는 보스 클론인가.</summary>
        public static bool Has(GameData d, EnemyDef e) => On && e != null && e.Boss && e.Clone != null && d.Hero(e.Clone)?.Ult != null;

        /// <summary>변환 결과 — 예고 수(Warn: charge → Next = Use) · 사용 수(Use: ult → Then) · 원래 효과 · 뺀 것.</summary>
        public sealed class PlanRec
        {
            public string Hero, HeroName, Name, Orig, Note;
            public int Tier;
            public Intent Warn, Use;
            public List<string> Dropped = new();
            /// <summary>예고 피해(원값 — 층 배율 · 상태 빼고). 전체 공격은 × FOE_ALL_X 한 값.</summary>
            public int Raw;
        }

        public static PlanRec Plan(GameData d, string heroKey, int tier)
        {
            var h = d.Hero(heroKey); var u = h?.Ult;
            if (u == null) return null;
            tier = Math.Max(1, Math.Min(2, tier));
            FIX.TryGetValue(heroKey, out var fix);
            var rec = new PlanRec { Hero = heroKey, HeroName = h.Name, Name = u.Name, Tier = tier, Orig = new CardText(d).Fx(u.Fx), Note = fix?.Note };
            var subs = new List<Intent>();
            double T = 0; int hitsMax = 1; bool all = false; int hitAt = -1;
            var debuff = new Dictionary<string, int>(); var buff = new Dictionary<string, int>();
            int jam = 0; bool block = false, heal = false;
            bool Dropped(string k) => fix?.Drop != null && fix.Drop.Contains(k);
            void Drop(string why) { if (!rec.Dropped.Contains(why)) rec.Dropped.Add(why); }

            foreach (var f in u.Fx ?? new List<Fx>())
            {
                string k = f.K;
                if (Dropped(k)) { Drop(k); continue; }
                switch (k)
                {
                    case "dmg":
                        {
                            int hits = f.XHits ? 3 : f.HitsOr1;
                            T += Math.Max(0.2, f.Ratio > 0 ? f.Ratio : 1) * hits;
                            hitsMax = Math.Max(hitsMax, hits);
                            if (f.Target == "allEnemies") all = true;
                            if (hitAt < 0) { hitAt = subs.Count; subs.Add(null); }
                            break;
                        }
                    case "status":
                        {
                            string id = f.Id; int v = Math.Max(1, (int)Math.Round(f.V));
                            bool bad = R.IsBadSt(id) || TO_JAM.Contains(id);
                            if (bad)
                            {
                                if (TO_JAM.Contains(id)) { jam = 1; break; }
                                if (DEBUFF.TryGetValue(id, out var m)) debuff[m.id] = Math.Max(debuff.TryGetValue(m.id, out var o) ? o : 0, Math.Min(m.cap, v + DEBUFF_PLUS));
                                else Drop($"{id}(파티에 걸 꼴 없음)");
                            }
                            else
                            {
                                if (BUFF.TryGetValue(id, out var m)) buff[m.id] = Math.Max(buff.TryGetValue(m.id, out var o) ? o : 0, Math.Min(m.cap, v));
                                else buff["피해 감소"] = Math.Max(buff.TryGetValue("피해 감소", out var o) ? o : 0, 2);
                            }
                            break;
                        }
                    case "atkMod": buff["사기"] = 1; break;
                    case "shield": case "block": block = true; break;
                    case "heal": case "drain": heal = true; break;
                    case "stack": Drop($"고유 효과 {f.Id}"); break;
                    case "ap": Drop("AP"); break;
                    case "tough": Drop("강인도 피해"); break;
                    case "make": Drop("카드 만들기"); break;
                    case "draw": Drop("드로우"); break;
                    case "form": Drop("변신"); break;
                    case "spend": Drop($"고유 효과 쓰기 {f.Id}"); break;
                    case "perStack": Drop($"겹마다({f.Id})"); break;
                    case "cleanse": Drop("디버프 해제"); break;
                    case "strip": Drop("버프 지우기"); break;
                    case "cue": case "ifBroken": break;
                    default: Drop(k); break;
                }
            }

            // 피해 — 없으면(지원형) 기준의 SUPPORT_HIT 만큼 전체 공격
            bool support = T <= 0;
            double scale = support ? SUPPORT_HIT : Math.Max(0.8, Math.Min(1.25, Math.Sqrt(T / T_REF)));
            int raw = Num.Round(RAW[tier] * scale * (fix?.Mul ?? 1) * Scale);
            string form = fix?.Hit ?? (support || all ? "attackAll" : hitsMax >= 2 ? "multi" : "attack");
            Intent hit;
            if (form == "attackAll") hit = new Intent { T = "attackAll", V = Num.Round(raw / R.FOE_ALL_X) };
            else if (form == "multi") { int n = fix != null && fix.N > 0 ? fix.N : Math.Min(MULTI_MAX, Math.Max(2, hitsMax)); hit = new Intent { T = "multi", V = Num.Round((double)raw / n), N = n }; }
            else hit = new Intent { T = "attack", V = raw };
            hit.Say = u.Name;
            rec.Raw = hit.T == "attackAll" ? Num.Round(hit.V * R.FOE_ALL_X) : hit.T == "multi" ? hit.V * hit.N : hit.V;
            if (hitAt >= 0) subs[hitAt] = hit; else subs.Insert(0, hit);
            if (support) rec.Note = rec.Note ?? $"피해 없는 고학년 — 기준의 {SUPPORT_HIT * 100:0}% 전체 공격을 붙였다";

            foreach (var kv in debuff) subs.Add(Db(kv.Key, kv.Value));
            if (jam > 0) subs.Add(new Intent { T = "jam", V = 1 });
            foreach (var kv in buff) subs.Add(Bf(kv.Key, kv.Value));
            if (block) subs.Add(new Intent { T = "block", V = BLOCK[tier] });
            if (heal) subs.Add(new Intent { T = "heal", V = HEAL[tier] });
            if (fix?.Add != null) subs.AddRange(fix.Add.Select(x => new Intent { T = x.T, V = x.V, N = x.N, Id = x.Id, All = x.All, To = x.To, Per = x.Per }));
            foreach (var s in subs) s.Say ??= u.Name;

            rec.Use = new Intent { T = "ult", Id = heroKey, Say = u.Name, Then = subs, Brk = true };
            rec.Warn = new Intent { T = "charge", Id = heroKey, Say = u.Name, Next = rec.Use, Brk = true };
            return rec;
        }

        /// <summary>그 보스 클론의 고학년 — 층은 판이 실제로 세운 층(floor 1 · 2 — 보스는 매 판 그 판 속성으로 새로 뽑고, 1층에 1~2성이 없으면 3성이 1층에 선다), 모르면(0) 마을 보스 줄 · 성급(Tier).</summary>
        public static PlanRec PlanFor(GameData d, EnemyDef e, int floor = 0) => Has(d, e) ? Plan(d, e.Clone, floor >= 1 ? Math.Min(2, floor) : Tier(d, e.Id)) : null;

        /// <summary>예고 · 사용 수인가(끊김 쪽지에 쓴다).</summary>
        public static bool IsUlt(Intent it) => it != null && (it.T == "ult" || (it.T == "charge" && it.Next?.T == "ult"));

        /// <summary>사용 수 하나의 글 — 보스 효과 칸.</summary>
        public static string Effect(CardText tx, Intent use) => use?.Then == null ? "" : string.Join(" · ", use.Then.Select(x => tx.Intent(x)));

        /// <summary>변환표(마크다운) — 사도 135명 × 층(그 사도 성급의 보스 층 — 1~2성 1층 · 3성 2층).</summary>
        public static string Table(GameData d)
        {
            var tx = new CardText(d);
            var sb = new System.Text.StringBuilder();
            sb.Append("# 보스 클론 고학년 변환표\n\n");
            sb.Append($"엔진 `Runtime/Combat/BossUlt.cs` 가 만든다(`bz bossult --out …`). 보스 클론이 {FIRST}턴째에 예고 → 다음 턴 사용, 그 뒤 {EVERY}턴마다. 예고 · 사용 턴에 격파(기절)하면 끊기고, 끊긴 보스는 취약 {CUT_VULN}.\n");
            sb.Append($"파티에 거는 디버프는 원작 값 + {DEBUFF_PLUS}(상한까지) · 피해 없는 고학년은 기준의 {SUPPORT_HIT * 100:0}% 전체 공격 · 방어 {BLOCK[1]} · {BLOCK[2]} · 회복 {HEAL[1]} · {HEAL[2]}(1층 · 2층).\n");
            sb.Append($"층은 사도 성급으로 잡았다(1~2성 → 1층 · 3성 → 2층). 예고 피해는 원값(층 피해 배율 · 상태 빼고) — 1층 기준 {RAW[1]} · 2층 기준 {RAW[2]}, 전체 공격은 화면에 × {R.FOE_ALL_X} 한 값.\n");
            sb.Append($"손보정 {FIX.Count}명(표에 ✎).\n\n");
            sb.Append("| 사도 | 층 | 고학년 | 원래 효과 | 보스 효과 | 예고 피해 | 뺀 것 · 메모 |\n|---|---|---|---|---|---|---|\n");
            foreach (var h in d.Heroes.Values.OrderBy(x => x.Race, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal))
            {
                if (h.Ult == null) continue;
                int tier = h.Star >= 3 ? 2 : 1;
                var p = Plan(d, h.Id, tier);
                string Esc(string s) => (s ?? "").Replace("|", "\\|").Replace("\n", " ");
                string memo = string.Join(" · ", new[] { p.Dropped.Count > 0 ? "뺌: " + string.Join(", ", p.Dropped) : null, p.Note }.Where(x => x != null));
                sb.Append($"| {Esc(h.Name)}{(FIX.ContainsKey(h.Id) ? " ✎" : "")} | {tier} | {Esc(p.Name)} | {Esc(p.Orig)} | {Esc(Effect(tx, p.Use))} | {p.Raw} | {Esc(memo)} |\n");
            }
            return sb.ToString();
        }
    }
}
