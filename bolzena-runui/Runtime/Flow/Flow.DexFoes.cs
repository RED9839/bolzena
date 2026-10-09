using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 적 도감(2026-10-06 리뉴얼) — 사도 · 교주 카드 · 장비와 같은 화면 틀(왼쪽 필터 · 격자 · 위 오른쪽 검색과 도감 탭 · 아래 남색 띠).
    //   위 줄 = 하위 탭(몬스터 · 보스 클론) + 속성 필터 다섯 색. 왼쪽 = 마을(ALL + 여섯), 보스 클론 탭은 층(1층 · 2층)도.
    //   몬스터 탭: 원작 몬스터를 「성격 모습마다 한 장」 으로 쪼갠다(그림 = 그 성격 스킨 아이콘, 없으면 still_ 촬영본). 종족 / 종족 없음 / 누루링 수액으로 묶는다.
    //     누루링 수액은 성격 모습이 없다(스킨이 종족) — 종족 스킨마다 한 장. 일반 · 엘리트(· 소환물)는 카드 안에서 바꾼다. 수치는 성격과 상관없이 같고, 상성만 다르다.
    //   보스 클론 탭: 클론이 될 수 있는 사도 하나하나가 한 장(엘다인 제외 · 닫힌 속성 제외 — 엔진 Bolzena.Core.Run.NaturesFor · FloorCandidates 그대로).
    //     「보스 몸」(clone_tig …)은 항목으로 두지 않고 클론 상세의 「보스 몸 수치」 로만 보인다. 보스가 세우는 소환물도 그 클론 상세 아래에 붙인다.
    //   몬스터 종족은 화면용 그림 표(world/villages/_그림.json 의 race — _gen/foes/build.py 가 쓴다)에서 읽는다.
    public partial class Flow
    {
        class FoeListState
        {
            public string Tab = "몬스터", Village, Nature, Focus;
            public int Floor = -1;
            public float ScrollY;
            public bool Drawn;
            /// <summary>카드마다 고른 변형(일반 · 엘리트 · 소환물) — 카드 키(몬스터|성격) → 변형 차례.</summary>
            public readonly Dictionary<string, int> Pick = new Dictionary<string, int>();
        }
        FoeListState dexFoes;
        static readonly string[] FoeGrades = { "보스", "엘리트", "일반", "소환물" };
        static readonly string[] RaceOrder = { "요정", "수인", "유령", "엘프", "정령", "용족", "없음", "누루링" };

        /// <summary>적 하나의 자리 — 등급 · 나오는 마을과 층(여럿) · 세운 적.</summary>
        class FoeSpot
        {
            public EnemyDef Def; public string Grade = "일반"; public string Summoner; public bool NoTough;
            public readonly List<(int vi, int floor)> Where = new List<(int, int)>();
            public int VillageIdx => Where.Count > 0 ? Where.Min(w => w.vi) : 99;
        }
        List<FoeSpot> foeIndex;

        /// <summary>몬스터 한 종류 — 같은 스파인의 일반 · 엘리트 · 소환물. 누루링 수액은 적 하나가 한 종류(종족 스킨).</summary>
        class FoeKind
        {
            public string Key, Name, Race, Spine; public bool Nuru;
            public readonly List<FoeSpot> Vars = new List<FoeSpot>();
            public IEnumerable<int> Villages => Vars.SelectMany(v => v.Where.Select(w => w.vi)).Distinct();
            public string[] Natures => Nuru ? new string[] { null } : R.FOE_NATURES;
        }
        List<FoeKind> foeKinds;

        /// <summary>클론 후보 사도 하나 — 그 마을(종족)과 후보인 층.</summary>
        class CloneRow
        {
            public string Hero; public HeroDef H; public HeroInfo Info; public string Village; public VillageDef V; public int VillageIdx;
            public readonly SortedSet<int> Floors = new SortedSet<int>();
        }
        List<CloneRow> cloneIndex;

        static Color FoeGradeColor(string g) => g switch
        {
            "보스" => new Color(1f, 0.36f, 0.42f),
            "엘리트" => new Color(0.65f, 0.49f, 0.94f),
            "소환물" => new Color(0.31f, 0.82f, 0.88f),
            _ => new Color(0.62f, 0.66f, 0.76f),
        };
        static int FoeGradeOrder(string g) => g == "일반" ? 0 : g == "엘리트" ? 1 : g == "소환물" ? 2 : 3;
        static string Hx(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        static string NatTag(string n) => n == null ? "" : $"<color={Hx(Theme.NatureCardOf(n))}>{n}</color>";

        static IEnumerable<Intent> AllIntents(EnemyDef e)
        {
            if (e.Open != null) yield return e.Open;
            foreach (var i in e.Intents) yield return i;
            if (e.Phase != null) foreach (var i in e.Phase.Intents) yield return i;
            if (e.Phase2 != null) foreach (var i in e.Phase2.Intents) yield return i;
            foreach (var p in e.Passives) if (p.Do != null) yield return p.Do;
            foreach (var c in e.Counters) if (c.Act != null) yield return c.Act;
        }

        static IEnumerable<Intent> Chain(Intent i)
        {
            for (var x = i; x != null; x = x.Next) yield return x;
        }

        List<string> VillOrder() => P.Data.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        // ── 색인 ──────────────────────────────────────────────
        List<FoeSpot> FoeIndex()
        {
            if (foeIndex != null) return foeIndex;
            var d = P.Data;
            var spots = new Dictionary<string, FoeSpot>();
            FoeSpot Spot(string id)
            {
                if (id == null || d.Enemy(id) == null) return null;
                if (!spots.TryGetValue(id, out var s))
                {
                    var e = d.Enemy(id);
                    spots[id] = s = new FoeSpot { Def = e, Grade = e.Boss || e.Clone != null ? "보스" : id.EndsWith("_elite", StringComparison.Ordinal) ? "엘리트" : "일반" };
                }
                return s;
            }
            var vills = VillOrder();
            for (int vi = 0; vi < vills.Count; vi++)
            {
                var v = d.Villages[vills[vi]];
                for (int f = 0; f < v.Floors.Count; f++)
                {
                    var fl = v.Floors[f];
                    foreach (var id in fl.Boss.Concat(fl.Elites.SelectMany(x => x)).Concat(fl.Pools.SelectMany(p => p).SelectMany(x => x)))
                    {
                        var s = Spot(id);
                        if (s != null && !s.Where.Contains((vi, f))) s.Where.Add((vi, f));
                    }
                }
            }
            // 소환물 — 마을 줄에 없고 누가 세우는 적(세운 적의 마을 · 층)
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var s in spots.Values.ToList())
                    foreach (var it in AllIntents(s.Def).SelectMany(Chain))
                        if (it.T == "summon" && it.Id != null && !spots.ContainsKey(it.Id) && d.Enemy(it.Id) != null)
                        {
                            var n = Spot(it.Id); n.Grade = "소환물"; n.Summoner = s.Def.Name; n.NoTough = it.NoTough;
                            n.Where.AddRange(s.Where);
                            grew = true;
                        }
            }
            foreach (var kv in d.Enemies) Spot(kv.Key);   // 데이터에만 있는 적
            return foeIndex = spots.Values.ToList();
        }

        List<FoeKind> FoeKinds()
        {
            if (foeKinds != null) return foeKinds;
            var map = new Dictionary<string, FoeKind>();
            foreach (var s in FoeIndex())
            {
                var e = s.Def;
                if (e.Boss || e.Clone != null) continue;   // 보스 몸은 보스 클론 탭(클론 상세)에서만
                var row = FoeRow(e.Id);
                string spine = row != null && row.TryGetValue("spine", out var sp) ? sp.Substring(sp.LastIndexOf('/') + 1) : e.Id;
                bool nuru = spine.StartsWith("nururing", StringComparison.Ordinal);
                string key = nuru ? e.Id : spine;
                if (!map.TryGetValue(key, out var k))
                    map[key] = k = new FoeKind { Key = key, Spine = spine, Nuru = nuru, Race = nuru ? "누루링" : row != null && row.TryGetValue("race", out var rc) ? rc : "없음" };
                k.Vars.Add(s);
            }
            foreach (var k in map.Values)
            {
                k.Vars.Sort((a, b) => FoeGradeOrder(a.Grade) != FoeGradeOrder(b.Grade) ? FoeGradeOrder(a.Grade) - FoeGradeOrder(b.Grade) : string.CompareOrdinal(a.Def.Id, b.Def.Id));
                var first = k.Vars.FirstOrDefault(v => v.Grade == "일반") ?? k.Vars[0];
                k.Name = first.Grade == "일반" ? first.Def.Name : first.Def.Name.Split(new[] { " · " }, StringSplitOptions.None)[0];
            }
            return foeKinds = map.Values
                .OrderBy(k => Array.IndexOf(RaceOrder, k.Race) < 0 ? 99 : Array.IndexOf(RaceOrder, k.Race))
                .ThenBy(k => k.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>클론이 될 수 있는 사도 — 마을마다 고를 수 있는 속성(NaturesFor) · 층마다 후보(FloorCandidates). 엔진 함수 그대로.</summary>
        List<CloneRow> CloneIndex()
        {
            if (cloneIndex != null) return cloneIndex;
            var d = P.Data;
            var rows = new Dictionary<string, CloneRow>();
            var vills = VillOrder();
            for (int vi = 0; vi < vills.Count; vi++)
            {
                var v = d.Villages[vills[vi]];
                var floors = Bolzena.Core.Run.CloneSlots(d, v.Id).Select(s => s.floor).Distinct().ToList();
                foreach (var n in Bolzena.Core.Run.NaturesFor(d, v.Id))
                    foreach (var f in floors)
                        foreach (var h in Bolzena.Core.Run.FloorCandidates(d, v.Id, n, f))
                        {
                            string key = v.Id + "|" + h;
                            if (!rows.TryGetValue(key, out var r))
                                rows[key] = r = new CloneRow { Hero = h, H = d.Hero(h), Info = Roster.OfCore(h), Village = v.Id, V = v, VillageIdx = vi };
                            r.Floors.Add(f);
                        }
            }
            return cloneIndex = rows.Values.Where(r => r.H != null)
                .OrderBy(r => r.VillageIdx).ThenBy(r => Array.IndexOf(R.FOE_NATURES, r.H.Nature)).ThenBy(r => r.Floors.Min).ThenBy(r => r.H.Star).ThenBy(r => r.H.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>그 클론이 그 층에 설 때의 몸(적 정의) — 그 층 보스였던 클론 데이터가 있으면 그것, 없으면 그 자리 원래 클론의 몸을 빌린 클론(GameData.CloneId). Run.PickBosses 와 같은 고르기.</summary>
        EnemyDef CloneBody(CloneRow r, int floor)
        {
            var d = P.Data;
            if (floor < 0 || floor >= r.V.Floors.Count) return null;
            var own = d.Enemies.Values.Where(e => e.Clone == r.Hero && r.V.Floors[floor].Boss.Contains(e.Id)).Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            if (own != null) return d.Enemy(own);
            var slot = Bolzena.Core.Run.CloneSlots(d, r.Village).Where(s => s.floor == floor).Select(s => s.id).FirstOrDefault();
            return slot != null ? d.Enemy(GameData.CloneId(r.Hero, slot)) : null;
        }

        static string FloorsKo(IEnumerable<int> floors) => string.Join(" · ", floors.Select(f => f + 1)) + "층";

        /// <summary>나오는 곳 글 — 「수인 부락 1 · 2층 · 유령 늪 1층」.</summary>
        string WhereKo(IEnumerable<(int vi, int floor)> where)
        {
            var vills = VillOrder();
            return string.Join(" · ", where.GroupBy(w => w.vi).OrderBy(g => g.Key).Select(g => P.Data.Villages[vills[g.Key]].Name + " " + FloorsKo(g.Select(x => x.floor).Distinct().OrderBy(x => x))));
        }

        /// <summary>강인도 칸 수 — 적 데이터(tough), 없으면 등급 기본(R.TOUGH). 강인도 없는 소환물은 0.</summary>
        static double FoeTough(FoeSpot s) => s.NoTough ? 0 : FoeTough(s.Def, s.Grade);
        static double FoeTough(EnemyDef e, string grade)
        {
            if (e.Tough > 0) return e.Tough;
            return grade == "보스" || e.Boss ? R.TOUGH.Boss : grade == "엘리트" ? R.TOUGH.Elite : R.TOUGH.Fight;
        }

        static string FoeNum(double v)
        {
            foreach (int dn in new[] { 1, 2, 3, 4, 5, 6 })
            {
                double k = Math.Round(v * dn);
                if (Math.Abs(v * dn - k) > 0.02) continue;
                int ki = (int)k, w = ki / dn, r = ki % dn;
                return r == 0 ? w.ToString() : (w > 0 ? w + " " : "") + r + "/" + dn;
            }
            return v.ToString("0.0");
        }

        /// <summary>강인도 회복 스킬 — 수 brace · 수에 붙은 tough · 패시브 do: brace. (이름, 양).</summary>
        static List<(string say, double v)> BraceSkills(EnemyDef e)
        {
            var o = new List<(string, double)>();
            foreach (var it in AllIntents(e).SelectMany(Chain))
            {
                if (it.T == "brace") o.Add((it.Say ?? "강인도 회복", it.V));
                else if (it.Tough > 0) o.Add((it.Say ?? it.T, it.Tough));
            }
            return o.GroupBy(x => x.Item1).Select(g => g.First()).ToList();
        }

        // ── 그림 ──────────────────────────────────────────────
        /// <summary>적 그림 — 사도 클론은 그 사도 상반신, 아니면 원작 정지 아이콘. 없으면 null(대역 글리프).</summary>
        Sprite FoeArt(EnemyDef e, float ratio)
        {
            if (e == null) return null;
            if (e.Clone != null)
            {
                var h = Roster.All.FirstOrDefault(x => x.CoreId == e.Clone || x.key == e.Clone);
                var up = h != null ? CardArt.Upper(h.art, ratio, 0.55f) : null;
                if (up != null) return up;
            }
            var ic = e.Art?.Icon;
            if (ic == null && FoeRow(e.Id) is Dictionary<string, string> row) row.TryGetValue("icon", out ic);
            return string.IsNullOrEmpty(ic) ? null : MonsterSprite(ic);
        }

        // 원작 몬스터 아이콘(Resources/Art/Monster) — 스프라이트로 들여온 것이 아니면 텍스처로 읽어 만든다(시험 프로젝트)
        static readonly Dictionary<string, Sprite> monSprites = new Dictionary<string, Sprite>();
        static Sprite MonsterSprite(string name)
        {
            if (monSprites.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Art/Monster/" + name);
            if (s == null) { var t = Resources.Load<Texture2D>("Art/Monster/" + name); if (t != null) s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100); }
            return monSprites[name] = s;
        }

        /// <summary>이 판의 적 속성(nature)에 맞는 적 그림 — 클론은 그 사도(자기 성격 그대로), 아니면 그 성격 모습, 없으면 기본 그림.</summary>
        Sprite FoeArtFor(EnemyDef e, string nature, float ratio = 1f)
        {
            if (e == null) return null;
            if (e.Clone == null && nature != null)
                foreach (var (ko, sp) in NatureLooks(e)) if (ko == nature) return sp;
            return FoeArt(e, ratio);
        }

        static readonly (string ko, string en)[] NatEn = { ("순수", "naive"), ("광기", "mad"), ("활발", "jolly"), ("우울", "gloomy"), ("냉정", "cool") };
        static string EnOf(string ko) { foreach (var (k, en) in NatEn) if (k == ko) return en; return null; }

        /// <summary>그 몬스터(스파인)의 그 성격 모습 — 원작 아이콘 icon_<스파인><성격>, 없으면 성격 스킨 촬영본 still_<스파인>_<성격>. 없으면 null.</summary>
        static Sprite NatureSprite(string spine, string nature)
        {
            var en = EnOf(nature);
            if (spine == null || en == null) return null;
            return MonsterSprite("icon_" + spine + en) ?? MonsterSprite("still_" + spine + "_" + en);
        }

        /// <summary>그림이 없는 적의 대체 — 같은 몬스터(id 의 첫 「_」 앞)의 다른 성격 그림 가운데 처음 찾은 것. 화면이 검게 칠해 실루엣으로 쓴다. 없으면 null.</summary>
        static Sprite FoeSilhouette(EnemyDef e)
        {
            if (e?.Id == null || e.Clone != null) return null;
            int us = e.Id.IndexOf('_');
            string mon = us > 0 ? e.Id.Substring(0, us) : e.Id;
            foreach (var (ko, _) in NatEn) { var sp = NatureSprite(mon, ko); if (sp != null) return sp; }
            return null;
        }

        /// <summary>같은 몬스터의 성격별 모습 — 다섯 성격 가운데 그림이 있는 것(누루링 수액 · 클론은 없음).</summary>
        List<(string nature, Sprite sp)> NatureLooks(EnemyDef e)
        {
            var o = new List<(string, Sprite)>();
            var row = FoeRow(e.Id);
            if (e.Clone != null || row == null || !row.TryGetValue("spine", out var sp)) return o;
            var spine = sp.Substring(sp.LastIndexOf('/') + 1);
            if (spine.StartsWith("nururing", StringComparison.Ordinal)) return o;
            foreach (var n in R.FOE_NATURES) { var s = NatureSprite(spine, n); if (s != null) o.Add((n, s)); }
            return o;
        }

        // 적 그림 표(콘텐츠 world/villages/_그림.json — 로더가 건너뛰는 화면용 표) — 적 id → spine · skin · icon · race
        static Dictionary<string, Dictionary<string, string>> foeRows;
        static Dictionary<string, string> FoeRow(string id)
        {
            if (foeRows == null)
            {
                foeRows = new Dictionary<string, Dictionary<string, string>>();
                try
                {
                    var p = System.IO.Path.Combine(RunPort.CoreDataDir, "world", "villages", "_그림.json");
                    if (System.IO.File.Exists(p))
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(p), @"""([^""]+)""\s*:\s*\{([^{}]*)\}"))
                        {
                            var row = new Dictionary<string, string>();
                            foreach (System.Text.RegularExpressions.Match f in System.Text.RegularExpressions.Regex.Matches(m.Groups[2].Value, @"""([^""]+)""\s*:\s*""([^""]*)"""))
                                row[f.Groups[1].Value] = f.Groups[2].Value;
                            foeRows[m.Groups[1].Value] = row;
                        }
                }
                catch (Exception ex) { Debug.LogWarning("[Dex] 적 그림 표를 못 읽었습니다: " + ex.Message); }
            }
            return id != null && foeRows.TryGetValue(id, out var r) ? r : null;
        }

        // ── 목록 화면 ──────────────────────────────────────────
        /// <summary>격자 한 묶음(제목 + 칸들) — 검색으로 칸이 다 숨으면 묶음도 숨긴다.</summary>
        class FoeSection { public GameObject Head, Grid; public readonly List<(GameObject go, string[] names)> Cells = new List<(GameObject, string[])>(); }

        void DexFoes(RectTransform host, Action back, FoeListState ls)
        {
            Ui.Clear(host);
            RosterBg(host, 0.9f);
            void Rebuild() => DexFoes(host, back, ls);
            void Refilter() { ls.ScrollY = 0; ls.Drawn = false; Rebuild(); }
            bool clones = ls.Tab == "보스 클론";
            RosterHead(host, "적 도감", back);

            var tr = Ui.Rect("tools", host).At(1, 1, -Theme.Gutter, -20, 1240, 52);
            Ui.Row(tr, 10, TextAnchor.MiddleRight, null, false, true);
            DexTabs(tr, host, "적", back);

            // 위 둘째 줄 — 하위 탭 둘 · 속성 다섯 색
            float subH = Theme.C(48, 42);
            var sub = Ui.Rect("subbar", host).Band(1, subH, Theme.Gutter, Theme.Gutter, -84);
            Ui.Row(sub, 8, TextAnchor.MiddleLeft, null, false, true);
            foreach (var t in new[] { "몬스터", "보스 클론" })
            {
                bool on = ls.Tab == t;
                var b = Btn.Make(sub, null, on ? BtnStyle.PillGold : BtnStyle.PillDark, () => { if (ls.Tab != t) { ls.Tab = t; ls.Focus = null; Refilter(); } }, 0, "sub " + t);
                var rt = b.GetComponent<RectTransform>();
                Ui.Row(rt, 6, TextAnchor.MiddleCenter, new RectOffset(16, 20, 4, 4), false, false);
                var ic = Ui.Img(rt, Theme.S(t == "몬스터" ? "ic_skull" : "ic_crown"), on ? Theme.Brown : Theme.Gold, "ic"); ic.preserveAspect = true; ic.Pref(22, 22);
                var tl = Ui.Title(rt, t, Theme.FsBody, on ? Theme.Brown : Theme.Ink); tl.textWrappingMode = TextWrappingModes.NoWrap;
                b.Pref(-1, subH);
                Stage.Hot["foetab:" + t] = b;
            }
            var sgap = Ui.Rect("gap", sub); sgap.Pref(Theme.C(28, 12), 4);
            var natL = Ui.Text(sub, "속성", Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineRight); natL.Pref(Theme.C(50, 40), subH); natL.textWrappingMode = TextWrappingModes.NoWrap;
            void NatChip(string n)
            {
                bool on = ls.Nature == n;
                var col = n == null ? Theme.Gold : Theme.NatureCardOf(n);
                var b = Btn.Make(sub, null, BtnStyle.Ghost, () => { ls.Nature = ls.Nature == n ? null : n; Refilter(); }, 0, "nat " + (n ?? "ALL"));
                b.Bg.sprite = Theme.Pill; b.SetColor(on ? col.A(0.9f) : Color.Lerp(Theme.NavyCell, col, 0.18f).A(0.85f));
                var rt = b.GetComponent<RectTransform>();
                Ui.Row(rt, 4, TextAnchor.MiddleCenter, new RectOffset(10, 14, 3, 3), false, false);
                if (n != null) { var ic = Ui.Img(rt, Icon("성격_" + n), Color.white, "ic"); ic.preserveAspect = true; ic.Pref(subH - 14, subH - 14); }
                var tl = Ui.Title(rt, n ?? "전체", Theme.FsSm, on ? Theme.Brown : Color.Lerp(col, Color.white, 0.3f)); tl.textWrappingMode = TextWrappingModes.NoWrap;
                b.Pref(-1, subH - 4);
                Stage.Hot["nat:" + (n ?? "ALL")] = b;
            }
            NatChip(null);
            foreach (var n in R.FOE_NATURES) NatChip(n);

            // 왼쪽 세로 필터 — 마을 6(+ 보스 클론 탭은 층 둘)
            float top = 84 + subH + 12;
            float railW = Theme.C(116, 104);
            var rail = Ui.Rect("rail", host); rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.pivot = new Vector2(0, 1);
            rail.sizeDelta = new Vector2(railW, -(top + 14)); rail.anchoredPosition = new Vector2(Theme.Gutter, -top);
            Ui.Col(rail, Theme.C(6, 3), TextAnchor.UpperCenter, null, false, false);
            float tabH = Theme.C(46, 34);
            void Tab(string label, bool on, Action go, Color? dot = null, string hot = null)
            {
                var b = Btn.Make(rail, null, BtnStyle.Ghost, go, 0, "tab " + label);
                b.Bg.sprite = Theme.Round; b.SetColor(on ? Theme.Gold.A(0.16f) : Theme.NavyCell.A(0.6f));
                b.Pref(railW, tabH);
                var t = Ui.Title(b.transform, label, label == "ALL" ? Theme.FsMd : Theme.FsSm, on ? Theme.Gold : dot != null ? Color.Lerp(dot.Value, Color.white, 0.35f) : Theme.Sub, TextAlignmentOptions.Center);
                t.rectTransform.Fill(8, 0, 8, 0);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = label == "ALL" ? Theme.FsMd : Theme.FsSm;
                if (dot != null) { var dd = Ui.Img(b.transform, Theme.S("circle"), dot.Value, "dot"); dd.rectTransform.At(1, 1, -5, -5, 8, 8); }
                if (on)
                {
                    var br = Ui.Img(b.transform, Theme.Frame, Theme.Gold, "on"); br.rectTransform.Fill();
                    var bar = Ui.Img(b.transform, Theme.Round, Theme.Gold, "bar"); bar.rectTransform.At(0, 0.5f, -10, 0, 4, tabH * 0.55f);
                }
                Stage.Hot[hot ?? "filter:" + label] = b;
            }
            Tab("ALL", ls.Village == null, () => { ls.Village = null; Refilter(); });
            foreach (var v in VillOrder())
            {
                var vv = v;
                Tab(P.Data.Villages[v].Name, ls.Village == vv, () => { ls.Village = vv; Refilter(); });
            }
            if (clones)
            {
                var gap = Ui.Img(rail, Theme.White, Theme.Edge.A(0.35f), "gap"); gap.Pref(railW - 20, 1);
                for (int f = 0; f < 2; f++)
                {
                    int ff = f;
                    Tab($"{f + 1}층", ls.Floor == f, () => { ls.Floor = ls.Floor == ff ? -1 : ff; Refilter(); }, FoeGradeColor("보스"));
                }
            }

            // 격자 — 묶음(제목 + 칸 격자)을 세로로
            float footH = Theme.C(84, 72);
            var area = Ui.Rect("grid", host); area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Theme.Gutter + railW + 16, footH + 8); area.offsetMax = new Vector2(-Theme.Gutter, -top);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(6, 18, 4, 16), true, false);
            float k = Theme.C(1f, 0.9f);
            float cw = 164 * k, ch = cw * 1.52f;
            var sections = new List<FoeSection>();
            FoeSection Section(string title, string note)
            {
                var s = new FoeSection();
                var head = Ui.Rect("head " + title, content);
                var t = Ui.Title(head, title + (note != null ? $"   <size=72%><color={Theme.SubTag}>{note}</color></size>" : ""), Theme.FsLg, Theme.Ink, TextAlignmentOptions.BottomLeft);
                t.rectTransform.Fill(14, 4, 0, 0); t.textWrappingMode = TextWrappingModes.Normal;
                var bar = Ui.Img(head, Theme.Round, Theme.Gold, "bar"); bar.rectTransform.At(0, 0, 0, 8, 4, 24);
                head.Pref(-1, note != null && note.Length > 60 ? 64 : 44);
                var g = Ui.Rect("cells " + title, content);
                var grid = g.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(cw, ch); grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(0, 0, 4, 10); grid.childAlignment = TextAnchor.UpperLeft;
                s.Head = head.gameObject; s.Grid = g.gameObject;
                sections.Add(s);
                return s;
            }
            void Banner(string text)
            {
                var b = Ui.Img(content, Theme.Round, Theme.NavyWell.A(0.75f), "banner");
                Ui.Col(b.rectTransform, 2, TextAnchor.MiddleLeft, new RectOffset(18, 18, 10, 10), true, false);
                var t = Ui.Text(b.transform, text, Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineLeft); t.textWrappingMode = TextWrappingModes.Normal;
            }

            int idx = 0, cards = 0;
            bool anim = !ls.Drawn;
            string focusName = null, focusSub = null;
            Action openFocus = null;
            if (!clones)
            {
                var kinds = FoeKinds().Where(kd => ls.Village == null || kd.Villages.Contains(VillOrder().IndexOf(ls.Village))).ToList();
                foreach (var g in kinds.GroupBy(kd => kd.Race))
                {
                    string title = g.Key == "없음" ? "종족 없음" : g.Key == "누루링" ? "누루링 수액" : g.Key + " 종족";
                    string note = g.Key == "누루링" ? "성격 모습 없음 — 종족 스킨마다 한 장 · 성격은 모험의 적 속성을 따릅니다" : $"{g.Count()}종";
                    var natList = new List<(FoeKind kd, string n)>();
                    foreach (var kd in g)
                        foreach (var n in kd.Natures)
                            if (ls.Nature == null || n == null || n == ls.Nature) natList.Add((kd, n));
                    if (natList.Count == 0) continue;
                    var sec = Section(title, note);
                    foreach (var (kd, n) in natList)
                    {
                        var go = MonsterTile((RectTransform)sec.Grid.transform, kd, n, ls, cw, ch, idx, anim);
                        var names = new List<string> { kd.Name + (n != null ? $"({n})" : ""), kd.Name };
                        names.AddRange(kd.Vars.Select(v => v.Def.Name));
                        sec.Cells.Add((go, names.ToArray()));
                        string fk = "m:" + kd.Key + "|" + n;
                        if (ls.Focus == fk) { var kd2 = kd; var n2 = n; focusName = kd.Name + (n != null ? $" ({n})" : ""); focusSub = (kd.Race == "누루링" ? "누루링 수액" : kd.Race == "없음" ? "종족 없음" : kd.Race) + " · " + string.Join(" · ", kd.Vars.Select(v => v.Grade)); openFocus = () => MonsterDetail(kd2, n2, PickOf(ls, kd2, n2)); }
                        idx++; cards++;
                    }
                }
            }
            else
            {
                var vills = VillOrder();
                var rows = CloneIndex().Where(r => (ls.Village == null || r.Village == ls.Village) && (ls.Nature == null || r.H.Nature == ls.Nature) && (ls.Floor < 0 || r.Floors.Contains(ls.Floor))).ToList();
                if (ls.Village != null)
                {
                    var v = P.Data.Villages[ls.Village];
                    var open = Bolzena.Core.Run.NaturesFor(P.Data, v.Id);
                    if (ls.Nature != null)
                    {
                        if (!open.Contains(ls.Nature))
                            Banner($"{v.Name}에서는 {NatTag(ls.Nature)} 판이 나오지 않습니다 — 두 층 보스 후보를 {v.Race} 종족 · {ls.Nature} 사도로 다 채우지 못합니다.");
                        else
                        {
                            var parts = new List<string>();
                            for (int f = 0; f < v.Floors.Count; f++)
                            {
                                var c = Bolzena.Core.Run.FloorCandidates(P.Data, v.Id, ls.Nature, f).Select(h => P.Data.Hero(h)?.Name ?? h).ToList();
                                bool alt = f == 0 && Bolzena.Core.Run.CloneCandidates(P.Data, v.Id, ls.Nature, 0).Count == 0;
                                parts.Add($"<color={Theme.GoldTag}>{f + 1}층 후보</color> {string.Join(" · ", c)}{(alt ? $" <size=80%><color={Theme.SubTag}>(1~2성이 없어 3성)</color></size>" : "")}");
                            }
                            Banner($"{v.Name} · {NatTag(ls.Nature)} 판이면  " + string.Join("   /   ", parts));
                        }
                    }
                    var shut = R.FOE_NATURES.Where(n => !open.Contains(n)).ToList();
                    for (int f = 0; f < 2; f++)
                    {
                        if (ls.Floor >= 0 && ls.Floor != f) continue;
                        var fr = rows.Where(r => r.Floors.Contains(f)).ToList();
                        if (fr.Count == 0) continue;
                        var sec = Section($"{f + 1}층 보스 후보", $"{fr.Count}명 · {(f == 0 ? "1~2성(없으면 3성)" : "3성")}" + (shut.Count > 0 && f == 0 ? $" · 닫힌 속성 {string.Join(" · ", shut)}" : ""));
                        foreach (var r in fr) AddClone(sec, r, f);
                    }
                }
                else
                {
                    foreach (var g in rows.GroupBy(r => r.VillageIdx).OrderBy(g => g.Key))
                    {
                        var v = P.Data.Villages[vills[g.Key]];
                        var shut = R.FOE_NATURES.Where(n => !Bolzena.Core.Run.NaturesFor(P.Data, v.Id).Contains(n)).ToList();
                        var sec = Section($"{v.Name}", $"{v.Race} 종족 · 클론 후보 {g.Count()}명" + (shut.Count > 0 ? $" · 닫힌 속성 {string.Join(" · ", shut)}" : ""));
                        foreach (var r in g) AddClone(sec, r, ls.Floor);
                    }
                }
                void AddClone(FoeSection sec, CloneRow r, int floor)
                {
                    var go = CloneTile((RectTransform)sec.Grid.transform, r, ls, cw, ch, idx, anim, floor);
                    sec.Cells.Add((go, new[] { r.H.Name, r.Info?.ko, r.Hero, r.V.Name }));
                    if (ls.Focus == "c:" + r.Village + "|" + r.Hero) { var r2 = r; focusName = r.H.Name + " 클론"; focusSub = $"{r.V.Name} · {r.H.Nature} · ★{r.H.Star} · {FloorsKo(r.Floors)} 후보"; openFocus = () => CloneDetail(r2, floor >= 0 ? floor : r2.Floors.Max); }
                    idx++; cards++;
                }
            }
            if (cards == 0) { var none = Ui.Text(content, ls.Village != null && ls.Nature != null && clones ? "이 마을 · 속성 판에는 클론 후보가 없습니다" : "맞는 적이 없습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center); none.Pref(-1, 80); }
            ls.Drawn = true;
            if (ls.ScrollY > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                float maxY = Mathf.Max(0, content.rect.height - ((RectTransform)sr.viewport).rect.height);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Min(ls.ScrollY, maxY));
                sr.StopMovement();
            }
            sr.onValueChanged.AddListener(_ => { if (content) ls.ScrollY = content.anchoredPosition.y; });

            // 아래 띠
            var fbg = Ui.Img(host, Theme.White, Theme.NavyPanel.A(0.96f), "footbg", true); fbg.rectTransform.Band(0, footH, 0, 0, 0);
            var line = Ui.Img(host, Theme.White, Theme.Edge.A(0.35f), "rule"); line.rectTransform.Band(0, 1, 0, 0, footH);
            var foot = Ui.Rect("foot", host).Band(0, footH, Theme.Gutter, Theme.Gutter, 0);
            string Count(int n) => clones ? $"클론 후보 {n}명" : $"몬스터 카드 {n}장";
            string baseInfo = clones
                ? $"{Count(cards)} · 누르면 상세  <color={Theme.SubTag}>— 보스는 모험마다 마을 종족 · 모험의 적 속성 사도 클론으로 섭니다(엘다인 제외)</color>"
                : $"{Count(cards)} · 누르면 상세  <color={Theme.SubTag}>— 성격 모습마다 한 장 · 일반 · 엘리트는 카드 안에서 바꿉니다</color>";
            var info = Ui.Text(foot, focusName != null ? $"<b>{focusName}</b>  <color={Theme.SubTag}>{focusSub}</color>" : baseInfo, Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.rectTransform.Fill(0, 0, 280, 0); info.textWrappingMode = TextWrappingModes.NoWrap; info.overflowMode = TextOverflowModes.Ellipsis;
            var detail = Btn.Make(foot, null, BtnStyle.PillDark, () => openFocus?.Invoke(), 0, "detail");
            var drt = detail.GetComponent<RectTransform>(); drt.At(1, 0.5f, 0, 0, 250, 58);
            var dzi = Ui.Img(drt, Theme.S("ic_zoom"), Theme.Gold, "ic"); dzi.rectTransform.At(0, 0.5f, 22, 0, 24, 24); dzi.preserveAspect = true;
            var dl = Ui.Title(drt, "상세 정보", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight); dl.rectTransform.Fill(50, 0, 26, 0);
            detail.Interactable = openFocus != null; detail.Why = "카드를 먼저 고르세요";
            Stage.Hot["list.detail"] = detail;
            FoeSearch(tr, sections, area, info, Count);
        }

        /// <summary>검색 칸 — 이름 · 초성(HangulSearch). 묶음 안 칸을 숨기고, 칸이 다 숨은 묶음은 제목째 숨긴다. 검색어는 적 도감 두 탭이 같이 쓴다.</summary>
        void FoeSearch(RectTransform row, List<FoeSection> sections, RectTransform area, TextMeshProUGUI info, Func<int, string> countText)
        {
            const string tab = "적";
            var input = SearchInput(row, "이름 · 초성 찾기", out var clear);
            input.transform.SetAsFirstSibling();
            DexSearchField = input;
            var empty = Ui.Text(area, "찾는 것이 없습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center, false, "nomatch");
            empty.rectTransform.At(0.5f, 1, 0, -60, 600, 60);
            string infoBase = info != null ? info.text : null;
            void Apply(string q)
            {
                dexQuery[tab] = q ?? "";
                int n = 0;
                DexSearchShown.Clear();
                foreach (var s in sections)
                {
                    int sn = 0;
                    foreach (var (go, names) in s.Cells)
                    {
                        bool on = HangulSearch.MatchAny(q, names);
                        if (go.activeSelf != on) go.SetActive(on);
                        if (on) { sn++; DexSearchShown.Add(names[0]); }
                    }
                    n += sn;
                    s.Head.SetActive(sn > 0); s.Grid.SetActive(sn > 0);
                }
                bool hasQ = !string.IsNullOrEmpty(q);
                empty.gameObject.SetActive(hasQ && n == 0);
                clear.gameObject.SetActive(hasQ);
                if (info != null && infoBase != null && !infoBase.StartsWith("<b>")) info.text = hasQ ? countText(n) + $"  <color={Theme.SubTag}>— 「{q}」</color>" : infoBase;
                if (sections.Count > 0) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)sections[0].Grid.transform.parent);
            }
            input.text = dexQuery.TryGetValue(tab, out var saved) ? saved : "";
            input.onValueChanged.AddListener(Apply);
            clear.OnClick = () => { input.text = ""; };
            Apply(input.text);
            Stage.Hot["dex.search.clear"] = clear;
        }

        int PickOf(FoeListState ls, FoeKind kd, string n) => ls != null && ls.Pick.TryGetValue(kd.Key + "|" + n, out var i) ? Mathf.Clamp(i, 0, kd.Vars.Count - 1) : 0;

        /// <summary>약점 · 우세 한 줄 — 그 성격의 적은 약점 성격에 약하고, 우세 성격(상성으로 이기는 성격)에 강하다.</summary>
        static string EdgeKo(string n, bool small = false)
        {
            if (n == null) return $"<color={Theme.SubTag}>상성은 모험의 적 속성을 따릅니다</color>";
            var weak = R.WeakTo(n);
            R.BEATS.TryGetValue(n, out var beat);
            return $"<color={Theme.SubTag}>약점</color> {string.Join(" · ", weak.Select(NatTag))}" + (beat != null ? $"{(small ? " " : "  ")}<color={Theme.SubTag}>우세</color> {NatTag(beat)}" : "");
        }

        /// <summary>몬스터 카드 하나(몬스터 × 성격) — 그 성격 모습 · 성격 색 테 · 일반/엘리트 전환 알약 · HP · 강인도 · 상성.</summary>
        GameObject MonsterTile(RectTransform parent, FoeKind kd, string n, FoeListState ls, float w, float h, int idx, bool anim)
        {
            var nc = n != null ? Theme.NatureCardOf(n) : new Color(0.62f, 0.66f, 0.76f);
            string fk = "m:" + kd.Key + "|" + n;
            int pick = PickOf(ls, kd, n);
            var b = Btn.Make(parent, null, BtnStyle.Ghost, null, 0, "foe " + kd.Key + " " + (n ?? ""));
            b.OnClick = () => { ls.Focus = fk; MonsterDetail(kd, n, PickOf(ls, kd, n)); };
            b.Bg.sprite = Theme.Round; b.SetColor(Color.Lerp(Theme.NavyWell, nc, 0.16f));
            var rt = b.GetComponent<RectTransform>();
            var win = Ui.Rect("win", rt).Fill(4, 118, 4, 4);
            win.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(win, Theme.S("soft"), nc.A(0.32f), "glow"); glow.rectTransform.At(0.5f, 0.5f, 0, 0, w * 1.1f, w);
            var pic = kd.Nuru ? FoeArt(kd.Vars[0].Def, 1) : NatureSprite(kd.Spine, n) ?? FoeArt(kd.Vars[0].Def, 1);
            if (pic != null) { var ic = Ui.Img(win, pic, Color.white, "art"); ic.preserveAspect = true; ic.rectTransform.At(0.5f, 0.5f, 0, -6, Mathf.Min(w * 0.84f, h - 128), Mathf.Min(w * 0.84f, h - 128)); }
            else
            {
                var g = Ui.Img(win, Theme.S("ic_skull"), nc.A(0.8f), "glyph"); g.preserveAspect = true; g.rectTransform.At(0.5f, 0.5f, 0, 10, w * 0.36f, w * 0.36f);
                var gn = Ui.Text(win, "그림 없음", Theme.FsCap - 1, Theme.Dim, TextAlignmentOptions.Center); gn.rectTransform.Band(0, 20, 4, 4, 8);
            }
            // 위 왼쪽 — 성격 아이콘 · 성격 이름
            if (n != null)
            {
                var ni = Ui.Img(rt, Icon("성격_" + n), Color.white, "nat"); ni.rectTransform.At(0, 1, 6, -6, 26, 26); ni.preserveAspect = true;
                var nt = Ui.Title(rt, n, Theme.FsCap, nc, TextAlignmentOptions.MidlineLeft); nt.rectTransform.At(0, 1, 34, -6, 60, 26); nt.Outline(0.2f);
            }
            // 아래 — 변형 알약 · 이름 · 수치(누르면 이 칸만 다시 그린다)
            var low = Ui.Rect("low", rt).Fill(6, 4, 6, h - 118);
            var pills = Ui.Rect("vars", low).Band(1, 24, 0, 0, 0);
            Ui.Row(pills, 4, TextAnchor.MiddleCenter, null, false, true);
            var stats = Ui.Rect("stats", low).Fill(0, 0, 0, 26);
            var pillBtns = new List<Btn>();
            void Draw(int i)
            {
                Ui.Clear(stats);
                var s = kd.Vars[i];
                for (int j = 0; j < pillBtns.Count; j++)
                {
                    bool on = j == i; var gc = FoeGradeColor(kd.Vars[j].Grade);
                    pillBtns[j].SetColor(on ? gc : Theme.NavyCell.A(0.85f));
                    if (pillBtns[j].Label) pillBtns[j].Label.color = on ? Theme.Brown : Color.Lerp(gc, Color.white, 0.3f);
                }
                var nm = Ui.Title(stats, s.Def.Name, Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center); nm.rectTransform.Band(1, 24, 0, 0, 0);
                nm.enableAutoSizing = true; nm.fontSizeMin = 9; nm.fontSizeMax = Theme.FsSm; nm.textWrappingMode = TextWrappingModes.NoWrap; nm.Outline(0.2f);
                double tough = FoeTough(s);
                var st = Ui.Text(stats, $"HP <color={Theme.GoldTag}>{s.Def.Hp:N0}</color>  강인도 " + (tough > 0 ? $"<color=#D6C2FF>{FoeNum(tough)}</color>" : "없음"), Theme.FsCap, Theme.Ink, TextAlignmentOptions.Center);
                st.rectTransform.Band(1, 20, 0, 0, -26); st.textWrappingMode = TextWrappingModes.NoWrap; st.enableAutoSizing = true; st.fontSizeMin = 9; st.fontSizeMax = Theme.FsCap;
                ToughPips(stats, tough, w * 0.72f, Theme.C(7, 6), 28);
                var ed = Ui.Text(stats, EdgeKo(n, true), Theme.FsCap - 1, Theme.Ink, TextAlignmentOptions.Center); ed.rectTransform.Band(0, 20, 0, 0, 2);
                ed.textWrappingMode = TextWrappingModes.NoWrap; ed.enableAutoSizing = true; ed.fontSizeMin = 8; ed.fontSizeMax = Theme.FsCap - 1;
            }
            if (kd.Vars.Count > 1)
                for (int j = 0; j < kd.Vars.Count; j++)
                {
                    int jj = j;
                    var pb = Btn.Make(pills, kd.Vars[j].Grade, BtnStyle.Ghost, () => { ls.Pick[kd.Key + "|" + n] = jj; Draw(jj); }, Theme.FsCap, "var " + kd.Vars[j].Grade);
                    pb.Bg.sprite = Theme.Pill; pb.Label.rectTransform.Fill(6, 0, 6, 0);
                    pb.Pref(kd.Vars[j].Grade.Length > 2 ? 58 : 48, 22);
                    pillBtns.Add(pb);
                    Stage.Hot[$"dexvar:{kd.Key}|{n}:{kd.Vars[j].Grade}"] = pb;
                }
            else
            {
                var gc = FoeGradeColor(kd.Vars[0].Grade);
                var gp = Ui.Img(pills, Theme.Pill, gc.A(0.9f), "grade"); gp.Pref(52, 22);
                var gt = Ui.Title(gp.transform, kd.Vars[0].Grade, Theme.FsCap - 1, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            }
            Draw(pick);
            var rim = Ui.Img(rt, Theme.Frame, nc.A(0.85f), "rim"); rim.rectTransform.Fill();
            if (ls.Focus == fk) { var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Sky, "focus"); fr.rectTransform.Fill(-3, -3, -3, -3); }
            Stage.Hot["dexfoe:" + idx.ToString("D3")] = b;
            Stage.Hot["dexmon:" + kd.Key + "|" + n] = b;
            if (anim && idx < 30) Tw.Pop(rt, 0.012f * idx, 0.88f, 0.28f);
            return b.gameObject;
        }

        /// <summary>클론 후보 카드 하나(사도) — 상반신 · 성격 색 테 · 성급 · 종족 · 어느 마을 몇 층 후보.</summary>
        GameObject CloneTile(RectTransform parent, CloneRow r, FoeListState ls, float w, float h, int idx, bool anim, int floor)
        {
            var nc = Theme.NatureCardOf(r.H.Nature);
            string fk = "c:" + r.Village + "|" + r.Hero;
            var b = Btn.Make(parent, null, BtnStyle.Ghost, null, 0, "clone " + r.Hero);
            b.OnClick = () => { ls.Focus = fk; CloneDetail(r, floor >= 0 && r.Floors.Contains(floor) ? floor : r.Floors.Max); };
            b.Bg.sprite = Theme.Round; b.SetColor(Color.Lerp(Theme.NavyWell, nc, 0.16f));
            var rt = b.GetComponent<RectTransform>();
            var win = Ui.Rect("win", rt).Fill(4, h * 0.36f, 4, 4);
            win.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(win, Theme.S("soft"), nc.A(0.35f), "glow"); glow.rectTransform.At(0.5f, 0.5f, 0, 0, w * 1.1f, w);
            var up = r.Info?.art != null ? CardArt.Upper(r.Info.art, (w - 8) / (h * 0.64f - 4), 0.55f) : null;
            if (up != null) { var ic = Ui.Img(win, up, Color.white, "art"); ic.rectTransform.Fill(); }
            else if (r.Info != null) { var fc = W.Face(win, r.Info, w * 0.6f); fc.At(0.5f, 0.5f, 0, 0, w * 0.6f, w * 0.6f); }
            var ni = Ui.Img(rt, Icon("성격_" + r.H.Nature), Color.white, "nat"); ni.rectTransform.At(0, 1, 6, -6, 26, 26); ni.preserveAspect = true;
            // 층 표(위 오른쪽)
            var fl = Ui.Rect("floors", rt).At(1, 1, -6, -6, 80, 22);
            Ui.Row(fl, 3, TextAnchor.MiddleRight, null, false, true);
            foreach (var f in r.Floors)
            {
                var fp = Ui.Img(fl, Theme.Pill, FoeGradeColor("보스").A(floor < 0 || floor == f ? 0.92f : 0.4f), "f" + f); fp.Pref(36, 20);
                var ft = Ui.Title(fp.transform, $"{f + 1}층", Theme.FsCap - 2, Theme.Brown, TextAlignmentOptions.Center); ft.rectTransform.Fill();
            }
            var nm = Ui.Title(rt, r.H.Name, Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center); nm.rectTransform.Band(0, 24, 6, 6, h * 0.36f - 30);
            nm.enableAutoSizing = true; nm.fontSizeMin = 9; nm.fontSizeMax = Theme.FsSm; nm.textWrappingMode = TextWrappingModes.NoWrap; nm.Outline(0.2f);
            var stars = Ui.Rect("stars", rt).Band(0, 18, 6, 6, h * 0.36f - 52);
            Ui.Row(stars, 1, TextAnchor.MiddleCenter, null, false, true);
            for (int i = 0; i < Mathf.Max(1, r.H.Star); i++) { var s = Ui.Img(stars, Icon("별_켜짐"), Color.white, "star"); s.Pref(16, 16); s.preserveAspect = true; }
            var sub = Ui.Text(rt, $"{NatTag(r.H.Nature)} · {r.H.Race}  <color={Theme.SubTag}>{r.V.Name}</color>", Theme.FsCap - 1, Theme.Ink, TextAlignmentOptions.Center);
            sub.rectTransform.Band(0, 20, 6, 6, 8); sub.textWrappingMode = TextWrappingModes.NoWrap; sub.enableAutoSizing = true; sub.fontSizeMin = 8; sub.fontSizeMax = Theme.FsCap - 1;
            var rim = Ui.Img(rt, Theme.Frame, nc.A(0.85f), "rim"); rim.rectTransform.Fill();
            if (ls.Focus == fk) { var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Sky, "focus"); fr.rectTransform.Fill(-3, -3, -3, -3); }
            Stage.Hot["dexfoe:" + idx.ToString("D3")] = b;
            Stage.Hot["dexclone:" + r.Hero] = b;
            if (anim && idx < 30) Tw.Pop(rt, 0.012f * idx, 0.88f, 0.28f);
            return b.gameObject;
        }

        /// <summary>강인도 칸 — n 칸(소수 끝 칸은 그만큼), 없으면 「강인도 없음」 글.</summary>
        static void ToughPips(RectTransform parent, double tough, float w, float h, float y, bool left = false)
        {
            if (tough <= 0)
            {
                var t = Ui.Text(parent, "강인도 없음", Theme.FsCap - 1, Theme.Dim, TextAlignmentOptions.Center); t.rectTransform.Band(0, h + 8, 6, 6, y - 4);
                return;
            }
            int n = (int)Math.Ceiling(tough - 1e-6);
            float gap = n > 8 ? 2 : 3, cw = (w - gap * (n - 1)) / n;
            var row = Ui.Rect("tough", parent).At(left ? 0 : 0.5f, 0, left ? 6 : 0, y, w, h);
            var back = Ui.Img(row, Theme.White, new Color(0.01f, 0.01f, 0.03f, 0.9f), "back"); back.rectTransform.Fill(-2, -2, -2, -2);
            for (int i = 0; i < n; i++)
            {
                float cap = (float)Math.Min(1, tough - i);
                var c = Ui.Img(row, Theme.White, new Color(0.84f, 0.76f, 1f), "pip" + i);
                c.rectTransform.anchorMin = c.rectTransform.anchorMax = c.rectTransform.pivot = new Vector2(0, 0.5f);
                c.rectTransform.anchoredPosition = new Vector2(i * (cw + gap), 0);
                c.rectTransform.sizeDelta = new Vector2(cw * cap, h);
            }
        }

        // ── 상세 ──────────────────────────────────────────────
        /// <summary>적 상세 — 다른 화면(편성 「나오는 적」 등)이 적 id 로 연다. 클론(보스 몸 · 빌린 몸)은 클론 상세, 몬스터는 그 성격 카드 상세.
        /// nature 를 안 주면 지금 판(편성 중)의 적 속성, 없으면 그 적의 기본 성격.</summary>
        public void EnemyDetail(string id, string nature = null)
        {
            var e = P.Data.Enemy(id);
            if (e == null) return;
            if (e.Clone != null) { CloneDetailOf(e); return; }
            var kd = FoeKinds().FirstOrDefault(x => x.Vars.Any(v => v.Def.Id == id));
            if (kd == null) return;
            string n = kd.Nuru ? null : nature ?? (string.IsNullOrEmpty(FoeNature) ? e.Nature : FoeNature);
            if (n != null && Array.IndexOf(R.FOE_NATURES, n) < 0) n = R.FOE_NATURES[0];
            MonsterDetail(kd, n, kd.Vars.FindIndex(v => v.Def.Id == id));
        }

        /// <summary>클론 적 정의(보스 몸 clone_tig · 빌린 몸 clone~사도~몸)로 클론 상세 — 그 몸이 선 마을 · 층에서.</summary>
        void CloneDetailOf(EnemyDef e)
        {
            string body = e.Id;
            if (body.StartsWith(GameData.CLONE_MARK, StringComparison.Ordinal)) { var p = body.Split('~'); if (p.Length >= 3) body = p[2]; }
            var vills = VillOrder();
            for (int vi = 0; vi < vills.Count; vi++)
            {
                var v = P.Data.Villages[vills[vi]];
                for (int f = 0; f < v.Floors.Count; f++)
                    if (v.Floors[f].Boss.Contains(body))
                    {
                        var r = CloneIndex().FirstOrDefault(x => x.Hero == e.Clone && x.Village == v.Id);
                        if (r == null)
                        {
                            var h = P.Data.Hero(e.Clone);
                            if (h == null) return;
                            r = new CloneRow { Hero = e.Clone, H = h, Info = Roster.OfCore(e.Clone), Village = v.Id, V = v, VillageIdx = vi };
                            r.Floors.Add(f);
                        }
                        CloneDetail(r, r.Floors.Contains(f) ? f : r.Floors.Max);
                        return;
                    }
            }
        }

        /// <summary>상세 오른쪽 스크롤 — 줄 글 하나(글 높이만큼).</summary>
        static TextMeshProUGUI DexLine(RectTransform content, string text, float size, Color c)
        {
            var t = Ui.Text(content, text, size, c, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return t;
        }

        /// <summary>알약 줄(변형 · 층 고르기) — 고른 것은 금빛.</summary>
        void PillRow(RectTransform content, IList<string> labels, int on, Action<int> pick, string hot, Color? tint = null)
        {
            var row = Ui.Rect("pills", content); row.Pref(-1, 40);
            Ui.Row(row, 8, TextAnchor.MiddleLeft, new RectOffset(4, 0, 0, 0), false, false);
            for (int i = 0; i < labels.Count; i++)
            {
                int ii = i;
                bool sel = i == on;
                var b = Btn.Make(row, labels[i], sel ? BtnStyle.PillGold : BtnStyle.PillDark, () => { if (ii != on) pick(ii); }, Theme.FsBody, hot + i);
                b.Pref(Mathf.Max(110, 26 * labels[i].Length + 40), 38);
                Stage.Hot[hot + i] = b;
            }
        }

        void ActsSection(RectTransform content, EnemyDef e)
        {
            var acts = new List<(Intent it, string head)>();
            if (e.Open != null) acts.Add((e.Open, "첫 턴"));
            foreach (var it in e.Intents) acts.Add((it, null));
            if (e.Phase != null) foreach (var it in e.Phase.Intents) acts.Add((it, $"HP {e.Phase.At * 100:0}% 아래"));
            if (e.Phase2 != null) foreach (var it in e.Phase2.Intents) acts.Add((it, $"HP {e.Phase2.At * 100:0}% 아래"));
            if (acts.Count == 0) return;
            W.Section(content, "행동 패턴", e.Pick == "shuffle" ? "무작위(같은 수 세 번 연속 없음)" : "차례대로", 36);
            foreach (var (it, head) in acts) W.IntentRow(content, this, it, head);
        }

        void BraceSection(RectTransform content, EnemyDef e, bool noTough)
        {
            W.Section(content, "강인도 회복", null, 36);
            if (noTough) { DexLine(content, $"<color={Theme.SubTag}>강인도 없음 — 격파되지 않음</color>", Theme.FsSm, Theme.Sub); return; }
            var br = BraceSkills(e);
            if (br.Count == 0) DexLine(content, $"<color={Theme.SubTag}>회복 스킬 없음 — 격파된 다음 내 턴 시작에만 다시 가득 찹니다.</color>", Theme.FsSm, Theme.Sub);
            else DexLine(content, string.Join("\n", br.Select(x => $"· {x.say} — 강인도 <color=#D6C2FF>+{FoeNum(x.v)}</color>")) + $"\n<color={Theme.SubTag}>그 밖에는 격파된 다음 내 턴 시작에 가득 찹니다.</color>", Theme.FsBody, Theme.Ink);
        }

        /// <summary>보스 · 엘리트가 세우는 적 — 이름 · HP · 강인도 · 몇씩 · 최대. 보스 소환물은 강인도 없음 · 보스와 함께 쓰러짐.</summary>
        void SummonSection(RectTransform content, EnemyDef e, bool boss)
        {
            var sums = AllIntents(e).SelectMany(Chain).Where(x => x.T == "summon" && x.Id != null).GroupBy(x => x.Id).Select(g => g.First()).ToList();
            if (sums.Count == 0) return;
            W.Section(content, "소환", boss ? "보스가 세우는 적 — 보스가 쓰러지면 함께 쓰러집니다" : "세우는 적", 36);
            foreach (var x in sums)
            {
                var se = P.Data.Enemy(x.Id);
                var row = Ui.Img(content, Theme.Round, Theme.NavyWell.A(0.55f), "summon " + x.Id);
                row.Pref(-1, 78);
                var pic = se != null ? FoeArt(se, 1) : null;
                if (pic != null) { var im = Ui.Img(row.transform, pic, Color.white, "art"); im.preserveAspect = true; im.rectTransform.At(0, 0.5f, 8, 0, 64, 64); }
                double t = x.NoTough || se == null ? 0 : FoeTough(se, "일반");
                string head = $"<b>{se?.Name ?? x.Id}</b>  <color={Theme.SubTag}>{Math.Max(1, x.N)}마리씩{(x.Max > 0 ? $" · 최대 {x.Max}" : "")}</color>";
                string body = $"HP <color={Theme.GoldTag}>{se?.Hp ?? 0:N0}</color>  강인도 " + (t > 0 ? $"<color=#D6C2FF>{FoeNum(t)}</color>" : "없음 — 격파되지 않음") + (x.NoTough || se?.Tied == true || boss ? $"  <color={Theme.SubTag}>· 소환한 적이 쓰러지면 함께 쓰러짐</color>" : "");
                var tx = Ui.Text(row.transform, head + "\n" + body, Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineLeft); tx.rectTransform.Fill(84, 4, 12, 4);
                tx.textWrappingMode = TextWrappingModes.Normal; tx.enableAutoSizing = true; tx.fontSizeMin = 11; tx.fontSizeMax = Theme.FsBody;
            }
        }

        /// <summary>몬스터 상세(몬스터 × 성격 × 변형) — 왼쪽 그 성격 모습 · 오른쪽 변형 전환 · 수치 · 상성 · 성격별 모습 · 나오는 곳 · 강인도 회복 · 행동 패턴 · 특성 · 소환 · 이야기.</summary>
        void MonsterDetail(FoeKind kd, string n, int vi)
        {
            vi = Mathf.Clamp(vi, 0, kd.Vars.Count - 1);
            var s = kd.Vars[vi];
            var e = s.Def;
            var nc = n != null ? Theme.NatureCardOf(n) : FoeGradeColor(s.Grade);
            string race = kd.Race == "누루링" ? "누루링 수액" : kd.Race == "없음" ? "종족 없음" : kd.Race + " 종족";
            var (body, close, _) = Stage.ModalBox("foezoom", 1120, Theme.C(640, 660), e.Name + (n != null ? $" ({n})" : ""), $"{race} · {s.Grade}" + (n != null ? $" · {n} 모습" : ""));
            FitScale.Fit(body.parent as RectTransform, Stage.Size, 1120, Theme.C(640, 660), 0.72f, 0.74f);
            void Reopen(string n2, int v2) { close(); MonsterDetail(kd, n2, v2); }
            float aw = Theme.C(340, 300);
            var well = Ui.Img(body, Theme.Round, Color.Lerp(Theme.NavyWell, nc, 0.2f), "well"); well.rectTransform.At(0, 0.5f, 10, 0, aw, aw * 1.25f);
            var mask = Ui.Rect("mask", well.rectTransform).Fill(4, 4, 4, 4); mask.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(mask, Theme.S("soft"), nc.A(0.45f), "glow"); glow.rectTransform.At(0.5f, 0.55f, 0, 0, aw * 1.2f, aw * 1.2f);
            var pic = kd.Nuru ? FoeArt(e, 1) : NatureSprite(kd.Spine, n) ?? FoeArt(e, 1);
            if (pic != null) { var ic = Ui.Img(mask, pic, Color.white, "art"); ic.preserveAspect = true; ic.rectTransform.At(0.5f, 0.56f, 0, 0, aw * 0.86f, aw * 0.86f); }
            else
            {
                var g = Ui.Img(mask, Theme.S("ic_skull"), nc.A(0.8f), "glyph"); g.preserveAspect = true; g.rectTransform.At(0.5f, 0.56f, 0, 0, aw * 0.4f, aw * 0.4f);
                var gn = Ui.Text(mask, "그림 없음", Theme.FsSm, Theme.Dim, TextAlignmentOptions.Center); gn.rectTransform.At(0.5f, 0.3f, 0, 0, aw, 30);
            }
            if (n != null) { var ni = Ui.Img(well.transform, Icon("성격_" + n), Color.white, "nat"); ni.rectTransform.At(0, 1, 12, -12, 40, 40); ni.preserveAspect = true; }
            var gp = Ui.Img(well.transform, Theme.Pill, FoeGradeColor(s.Grade), "grade"); gp.rectTransform.At(0.5f, 0, 0, 16, 150, 34);
            var gt = Ui.Title(gp.transform, s.Grade, Theme.FsMd, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            var rim = Ui.Img(well.transform, Theme.Frame, nc, "rim"); rim.rectTransform.Fill();

            var right = Ui.Rect("right", body).Fill(aw + 40, 0, 6, 0);
            var content = Ui.Scroll(right, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 10, 4, 10), true, false);
            if (kd.Vars.Count > 1) PillRow(content, kd.Vars.Select(v => v.Grade).ToList(), vi, i => Reopen(n, i), "zoom.var");
            double tough = FoeTough(s);
            W.Section(content, "수치", "성격과 상관없이 같음", 36);
            DexLine(content, $"HP <color={Theme.GoldTag}>{e.Hp:N0}</color>    강인도 " + (tough > 0 ? $"<color=#D6C2FF>{FoeNum(tough)}</color> <size=80%><color={Theme.SubTag}>칸{(e.ToughTaken > 0 && e.ToughTaken != 1 ? $" · 받는 강인도 피해 ×{e.ToughTaken:0.##}" : "")}</color></size>" : $"<color={Theme.SubTag}>없음 — 격파되지 않음</color>"), Theme.FsLg, Theme.Ink);
            if (tough > 0) { var pr = Ui.Rect("pips", content); pr.Pref(-1, 16); ToughPips(pr, tough, Mathf.Min(420, 34 * (float)Math.Ceiling(tough)), 12, 2, true); }
            W.Section(content, "상성", n != null ? $"{n} 모습" : null, 36);
            DexLine(content, EdgeKo(n), Theme.FsLg, Theme.Ink);
            DexLine(content, $"<color={Theme.SubTag}>" + (n != null ? $"모험의 적 속성이 {n}이면 이 모습 · 이 상성으로 나옵니다. 약점 성격으로 치면 피해와 강인도 피해가 커집니다." : "누루링 수액은 종족 스킨이 그대로이고, 성격은 모험의 적 속성을 따릅니다.") + "</color>", Theme.FsSm, Theme.Sub);
            // 성격별 모습 — 다섯 성격 그림(있는 것만). 누르면 그 성격 카드로
            if (!kd.Nuru)
            {
                var looks = R.FOE_NATURES.Select(x => (x, NatureSprite(kd.Spine, x))).Where(x => x.Item2 != null).ToList();
                if (looks.Count > 1)
                {
                    W.Section(content, "성격별 모습", "모험의 적 속성에 따라 · 누르면 그 모습", 36);
                    var lr = Ui.Rect("looks", content); lr.Pref(-1, 118);
                    Ui.Row(lr, 10, TextAnchor.MiddleLeft, new RectOffset(4, 0, 0, 0), false, false);
                    foreach (var (ln, sp) in looks)
                    {
                        var lc = Theme.NatureCardOf(ln);
                        var cell = Btn.Make(lr, null, BtnStyle.Ghost, () => { if (ln != n) Reopen(ln, vi); }, 0, "look " + ln);
                        cell.Bg.sprite = Theme.Round; cell.SetColor(Color.Lerp(Theme.NavyWell, lc, 0.25f)); cell.Pref(96, 112);
                        var im = Ui.Img(cell.transform, sp, Color.white, "art"); im.preserveAspect = true; im.rectTransform.At(0.5f, 1, 0, -4, 84, 80);
                        var nsp2 = Icon("성격_" + ln);
                        if (nsp2 != null) { var ni2 = Ui.Img(cell.transform, nsp2, Color.white, "nat"); ni2.rectTransform.At(0, 0, 6, 6, 22, 22); ni2.preserveAspect = true; }
                        var tl = Ui.Title(cell.transform, ln, Theme.FsCap, lc, TextAlignmentOptions.Midline); tl.rectTransform.At(0.5f, 0, 10, 6, 70, 22);
                        if (ln == n) { var fr = Ui.Img(cell.transform, Theme.Frame, Theme.Gold, "on"); fr.rectTransform.Fill(); }
                        Stage.Hot["zoom.look:" + ln] = cell;
                    }
                }
            }
            // 나오는 곳 — 변형마다 마을 · 층
            W.Section(content, "나오는 곳", null, 36);
            DexLine(content, string.Join("\n", kd.Vars.Select(v => $"<color={Hx(FoeGradeColor(v.Grade))}>{v.Grade}</color>  " + (v.Where.Count > 0 ? WhereKo(v.Where) : "데이터에만 있음") + (v.Summoner != null ? $"  <color={Theme.SubTag}>· {v.Summoner} 이(가) 불러냄</color>" : ""))), Theme.FsBody, Theme.Ink);
            BraceSection(content, e, s.NoTough);
            ActsSection(content, e);
            var more = P.Text.Enemy(e);
            if (!string.IsNullOrEmpty(more)) { W.Section(content, "특성", "패시브 · 쌓이는 수치 · 희귀종", 36); DexLine(content, more, Theme.FsBody, Theme.Ink); }
            SummonSection(content, e, false);
            if (!string.IsNullOrEmpty(e.Blurb)) { W.Section(content, "이야기", null, 36); DexLine(content, $"<color={Theme.SubTag}>{e.Blurb}</color>", Theme.FsSm, Theme.Sub); }
            Stage.Hot["zoom.close"] = Stage.Hot["modal.x"];
        }

        /// <summary>클론 상세 — 왼쪽 그 사도 스탠딩 · SD, 오른쪽 클론 정보 · 층 고르기 · 보스 몸 수치(HP · 강인도) · 보스 고학년(엔진 BossUlt.Plan) · 소환 · 행동 패턴 · 특성.</summary>
        void CloneDetail(CloneRow r, int floor)
        {
            if (!r.Floors.Contains(floor)) floor = r.Floors.Max;
            var d = P.Data;
            var h = r.H;
            var nc = Theme.NatureCardOf(h.Nature);
            var (body, close, _) = Stage.ModalBox("foezoom", 1160, Theme.C(680, 680), h.Name + " 클론", $"{r.V.Name} · {h.Nature} · ★{h.Star} · {h.Race} · {FloorsKo(r.Floors)} 보스 후보");
            FitScale.Fit(body.parent as RectTransform, Stage.Size, 1160, Theme.C(680, 680), 0.72f, 0.74f);
            float aw = Theme.C(340, 300), ah = aw * 1.0f, sdH = Theme.C(170, 150);
            // 스탠딩(위) · SD(아래)
            var well = Ui.Img(body, Theme.Round, Color.Lerp(Theme.NavyWell, nc, 0.2f), "well"); well.rectTransform.At(0, 1, 10, -4, aw, ah);
            var mask = Ui.Rect("mask", well.rectTransform).Fill(4, 4, 4, 4); mask.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(mask, Theme.S("soft"), nc.A(0.45f), "glow"); glow.rectTransform.At(0.5f, 0.55f, 0, 0, aw * 1.2f, aw * 1.2f);
            var up = r.Info?.art != null ? CardArt.Upper(r.Info.art, (aw - 8) / (ah - 8), 0.62f) : null;
            if (up != null) { var im = Ui.Img(mask, up, Color.white, "standing"); im.rectTransform.Fill(); }
            else if (r.Info != null) { var fc = W.Face(mask, r.Info, aw * 0.6f); fc.At(0.5f, 0.5f, 0, 0, aw * 0.6f, aw * 0.6f); }
            var ni = Ui.Img(well.transform, Icon("성격_" + h.Nature), Color.white, "nat"); ni.rectTransform.At(0, 1, 12, -12, 40, 40); ni.preserveAspect = true;
            var rim = Ui.Img(well.transform, Theme.Frame, nc, "rim"); rim.rectTransform.Fill();
            var stage = Ui.Img(body, Theme.Round, Theme.NavyWell.A(0.6f), "sd"); stage.rectTransform.At(0, 1, 10, -ah - 14, aw, sdH);
            var spot = Ui.Rect("spot", stage.rectTransform).At(0.5f, 0, 0, 14, 10, 10);
            if (r.Info != null) SceneHero.Make(spot, r.Info, sdH * 0.62f, true, 0, true);
            var sdl = Ui.Text(stage.transform, "SD", Theme.FsCap - 1, Theme.Dim, TextAlignmentOptions.TopLeft); sdl.rectTransform.At(0, 1, 10, -6, 60, 20);

            var right = Ui.Rect("right", body).Fill(aw + 40, 0, 6, 0);
            var content = Ui.Scroll(right, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 10, 4, 10), true, false);
            // 클론 정보
            W.Section(content, "클론", null, 36);
            DexLine(content, $"{NatTag(h.Nature)} · ★{h.Star} · {h.Race}   <color={Theme.SubTag}>{r.V.Name}</color> {FloorsKo(r.Floors)} 보스 후보", Theme.FsLg, Theme.Ink);
            DexLine(content, EdgeKo(h.Nature), Theme.FsBody, Theme.Ink);
            bool alt = r.Floors.Contains(0) && h.Star >= 3;
            DexLine(content, $"<color={Theme.SubTag}>모험의 적 속성이 {h.Nature}이고 마을이 {r.V.Name}이면 이 클론이 보스로 설 수 있습니다. 클론은 사도 성격 그대로입니다." + (alt ? $" {r.V.Race} 종족에 {h.Nature} 1~2성이 없어 3성이 1층에도 섭니다(1층 몸 · 1층 고학년 값)." : "") + "</color>", Theme.FsSm, Theme.Sub);
            if (r.Floors.Count > 1) PillRow(content, r.Floors.Select(f => $"{f + 1}층 보스").ToList(), r.Floors.ToList().IndexOf(floor), i => { close(); CloneDetail(r, r.Floors.ToList()[i]); }, "zoom.floor");
            var e = CloneBody(r, floor);
            if (e == null) { DexLine(content, $"<color={Theme.SubTag}>이 층의 보스 몸이 없습니다.</color>", Theme.FsSm, Theme.Sub); Stage.Hot["zoom.close"] = Stage.Hot["modal.x"]; return; }
            // 보스 몸 수치
            string bodyKey = e.Id.StartsWith(GameData.CLONE_MARK, StringComparison.Ordinal) ? e.Id.Split('~')[2] : e.Id;
            var bodyDef = d.Enemy(bodyKey);
            double tough = FoeTough(e, "보스");
            W.Section(content, "보스 몸 수치", $"{r.V.Name} {floor + 1}층 보스 칸", 36);
            DexLine(content, $"HP <color={Theme.GoldTag}>{e.Hp:N0}</color>    강인도 <color=#D6C2FF>{FoeNum(tough)}</color> <size=80%><color={Theme.SubTag}>칸</color></size>", Theme.FsLg, Theme.Ink);
            { var pr = Ui.Rect("pips", content); pr.Pref(-1, 16); ToughPips(pr, tough, Mathf.Min(420, 34 * (float)Math.Ceiling(tough)), 12, 2, true); }
            if (bodyDef != null && bodyDef.Clone != h.Id)
                DexLine(content, $"<color={Theme.SubTag}>몸 — {bodyDef.Clone ?? bodyDef.Name} 몸(HP · 강인도 · 행동 · 패시브)을 빌려 씀</color>", Theme.FsSm, Theme.Sub);
            // 보스 고학년(엔진 BossUlt — 사도 고학년을 적 쪽으로 뒤집은 것)
            var plan = BossUlt.On ? BossUlt.Plan(d, h.Id, floor + 1) : null;
            W.Section(content, "보스 고학년", plan != null ? $"「{plan.Name}」" : null, 36);
            if (plan == null) DexLine(content, $"<color={Theme.SubTag}>고학년 없음</color>", Theme.FsSm, Theme.Sub);
            else
            {
                DexLine(content, $"<color={Theme.GoldTag}>{BossUlt.FIRST}턴째</color> 예고 → 다음 턴 사용 · 그 뒤 <color={Theme.GoldTag}>{BossUlt.EVERY}턴</color>마다 · 예고 · 사용 턴에 격파하면 끊김", Theme.FsBody, Theme.Ink);
                W.IntentRow(content, this, plan.Warn, $"{floor + 1}층 값");
                DexLine(content, $"<color={Theme.SubTag}>원래 고학년 — {plan.Orig}</color>" + (plan.Dropped.Count > 0 ? $"\n<color={Theme.SubTag}>보스판에서 빠진 것 — {string.Join(", ", plan.Dropped)}</color>" : ""), Theme.FsSm, Theme.Sub);
            }
            SummonSection(content, e, true);
            BraceSection(content, e, false);
            ActsSection(content, e);
            var more = P.Text.Enemy(e);
            if (!string.IsNullOrEmpty(more)) { W.Section(content, "특성", "패시브 · 쌓이는 수치", 36); DexLine(content, more, Theme.FsBody, Theme.Ink); }
            Stage.Hot["zoom.close"] = Stage.Hot["modal.x"];
        }
    }
}
