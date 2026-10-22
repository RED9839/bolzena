using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 프론티어 전투 화면(트랙 C)이 꽂히는 자리 — 슬롯 · 시즌 · 서버 씨앗으로 Frontier.Start 한 전투를 열고, 끝나면 점수 · 테이프 글을 돌려준다.
    /// lab = 시험 덱(「덱 만들기」 — Flow.DeckLab): 규칙 검사 밖 슬롯도 열고(Frontier.Start strict false) · 사도별 딜(Battle.HpMeter — 측정 전용)을 재어 FrontierResult.Meter 로 돌려준다.
    /// </summary>
    public interface IFrontierFight
    {
        /// <param name="prog">서버가 시도를 열 때 떠 둔 사도 성장(시작 응답의 prog — 성장 켬일 때만, migrations/0010). null = 성장 없음(지금과 같은 셋업).</param>
        void OpenFrontier(GameData data, List<HeroSlot> slots, FrontierSeason season, long seed, Action<FrontierResult> done, bool lab = false, ProgState prog = null);
    }

    /// <summary>프론티어 한 번의 끝 — 점수 · 끝(win · lose · time) · 낸 턴 · 테이프(FrontierTape.Json() 글 그대로 — 다시 직렬화하지 않는다).</summary>
    public sealed class FrontierResult
    {
        public int Score, Played;
        public string Over, Tape, Error;
        /// <summary>시험 덱 전투(lab)만 — 사도 키(주인 모름은 "") → [보스가 잃은 HP, 다른 적(드론 · 소환물)이 잃은 HP]. 엔진 Battle.HpMeter 그대로(측정 전용 — 점수 · 테이프 · 지문에 들지 않는다).</summary>
        public Dictionary<string, long[]> Meter;
    }

    // 프론티어 — 입구(시즌 · 보스 M.E.O.W 한 줄 + 주요 패턴 목록 · 내 기록 · 거대 보스 스파인과 랭킹) → 「진입」 → 편성(사도 카드 셋 + 아래 덱 칸 = 장착한 저장 칸,
    //   덱 칸을 누르면 「저장 데이터」 창 — 카제나 세이브 데이터 방식: 왼쪽 칸 목록(최신순/점수순 · 스크롤) · 이름 · 같은 사도 둘 금지) → 「도전」(서버 씨앗) → 전투 → 결과 · 제출 → 랭킹.
    //   칸은 서버가 준 id 로 가리킨다(사도마다 10칸 — 2026-10-19). 화면 틀(머리 · 판 · 여백)은 ScreenHead · FPanel · FLabel 로 모든 프론티어 화면이 같이.
    //   설계 · 서버: bolzena-unity/Docs/프론티어_설계.md · 프론티어_서버.md. 서버가 없으면 보스 소개와 저장함(기기 사본)만 열린다.
    public partial class Flow
    {
        public IFrontierFight FrontierFight;
        /// <summary>편성 — 자리 셋(사도 · 칸 id, 빈 자리는 null). 기기에 기억한다.</summary>
        readonly (string hero, long id)?[] fPick = new (string, long)?[Frontier.PARTY_MAX];
        bool fPickRead;
        JObject fMe;
        /// <summary>지금 연 시도(시작 응답) — 결과를 낼 때 쓴다.</summary>
        sealed class Attempt { public string Id; public long Seed; public FrontierSeason Season; public List<HeroSlot> Slots; public List<string> SlotJson; public List<string> BestKeys; public ProgState Prog; }
        Attempt fAttempt;
        /// <summary>마지막 결과 · 제출 상태(데모 · 결과 화면).</summary>
        public FrontierResult LastFrontier { get; private set; }
        public string LastSubmit { get; private set; }

        static string FTail { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-save"); return i >= 0 && i < a.Length - 1 ? "." + a[i + 1] : ""; } }

        /// <summary>시즌 — 서버 시즌 줄(Online.Season)에서, 없으면 엔진 프리시즌 값.</summary>
        static FrontierSeason SeasonOf(JObject s)
        {
            if (s == null) return Frontier.PRESEASON;
            double D(string k, double d) { var t = s[k]; return t == null || t.Type == JTokenType.Null ? d : (double)t; }
            return new FrontierSeason { Id = (string)s["id"] ?? Frontier.PRESEASON.Id, Boss = (string)s["boss"] ?? Frontier.PRESEASON.Boss, Turns = (int?)s["turns"] ?? Frontier.PRESEASON.Turns, Hp = D("hp", 1), Dmg = D("dmg", 1) };
        }
        static string SeasonLabel(JObject s) => (string)s?["label"] ?? "프리시즌";
        static bool Preseason(JObject s) => s == null || s["preseason"]?.Type != JTokenType.Boolean || (bool)s["preseason"];

        void ReadPick()
        {
            if (fPickRead) return;
            fPickRead = true;
            // pick2 = 사도:칸 id(0005 뒤). 옛 pick(사도:칸 번호)은 읽지 않는다 — 번호가 id 가 아니다
            var parts = PlayerPrefs.GetString("bz.frontier.pick2" + FTail, "").Split('|');
            for (int i = 0; i < fPick.Length && i < parts.Length; i++)
            {
                var p = parts[i].Split(':');
                fPick[i] = p.Length == 2 && long.TryParse(p[1], out var id) ? (p[0], id) : ((string, long)?)null;
            }
        }
        void SavePick()
        {
            PlayerPrefs.SetString("bz.frontier.pick2" + FTail, string.Join("|", fPick.Select(x => x == null ? "" : x.Value.hero + ":" + x.Value.id)));
            PlayerPrefs.Save();
        }
        /// <summary>편성 자리를 정한다(자동 데모 · 고르기 창).</summary>
        public void SetFrontierPick(int i, string hero, long id) { ReadPick(); fPick[i] = hero == null ? ((string, long)?)null : (hero, id); SavePick(); }
        /// <summary>자리 i 에 장착한 칸(없거나 사라졌으면 null).</summary>
        SlotStore.Entry PickOf(int i) => fPick[i] != null ? SlotStore.Get(fPick[i].Value.id) : null;
        /// <summary>자리 i 에 장착한 칸 id(데모 단언).</summary>
        public long? FrontierPickId(int i) { ReadPick(); return fPick[i]?.id; }

        // ── 칸마다 낸 최고 점수 — 서버에 닿으면 서버가 검증한 값(GET /api/slots 의 best — 시도가 떠 둔 슬롯 지문 기준),
        //    서버가 없을 때만 이 기기에 적은 값(검증 전 점수 · 열쇠 = 사도:칸 id:슬롯 지문, 덮어쓰면 새로 센다) ──
        Dictionary<string, int> slotBest;
        Dictionary<string, int> SlotBestMap
        {
            get
            {
                if (slotBest != null) return slotBest;
                slotBest = new Dictionary<string, int>();
                foreach (var p in PlayerPrefs.GetString("bz.frontier.slotbest" + FTail, "").Split('|'))
                {
                    var kv = p.Split('=');
                    if (kv.Length == 2 && int.TryParse(kv[1], out var v)) slotBest[kv[0]] = v;
                }
                return slotBest;
            }
        }
        static string BestKey(SlotStore.Entry e) => e == null ? null : $"{e.Hero}:{e.Id}:{e.Slot?.Hash() ?? ""}";
        static bool BestFromServer => SlotStore.FromServer && Online.Up;
        /// <summary>서버 칸 최고의 상태(GET /api/slots best.status — 검증된 것 우선, 없으면 검증 전 · 검증 안 함 가운데 가장 높은 것).</summary>
        static string BestStatusKo(string st) => st == "pending" ? "검증 전" : st == "skipped" ? "검증 안 함" : "검증됨";
        int? SlotBest(SlotStore.Entry e) => e == null ? null : BestFromServer ? e.Best : SlotBestMap.TryGetValue(BestKey(e), out var v) ? v : (int?)null;
        void NoteSlotBest(IEnumerable<string> keys, int score)
        {
            foreach (var k in keys) if (k != null && (!SlotBestMap.TryGetValue(k, out var v) || v < score)) SlotBestMap[k] = score;
            PlayerPrefs.SetString("bz.frontier.slotbest" + FTail, string.Join("|", SlotBestMap.Select(x => x.Key + "=" + x.Value)));
            PlayerPrefs.Save();
        }

        /// <summary>칸을 편성에 쓸 수 없는 까닭(쓸 수 있으면 null) — 문제 칸 · 읽을 수 없는 칸 · 이 기기 데이터로 못 여는 칸.</summary>
        string SlotWhyNot(SlotStore.Entry e)
        {
            if (e == null) return "빈 칸";
            if (e.Status == "bad") return "문제 — " + (e.Why ?? "규칙 검사 거절");
            if (e.Slot == null) return "읽을 수 없는 저장 칸";
            var c = HeroSlot.Check(P.Data, e.Slot);
            return c.Ok ? null : "이 기기 데이터로 열 수 없음 — " + string.Join(" / ", c.Errors.Take(2));
        }

        /// <summary>입구의 랭킹 칸에 보인 줄 수 · 내 줄 · 보스 스파인이 섰나(데모 단언).</summary>
        public int EntryRankRows { get; private set; }
        /// <summary>입구 랭킹 칸 · 랭킹판에 보인 「검증 전」 줄 수(데모 단언).</summary>
        public int EntryRankPending { get; private set; }
        public int RankingPending { get; private set; }

        /// <summary>
        /// 랭킹 줄 · 내 최고가 검증 전인가 — 서버가 낸 점수를 곧바로 랭킹에 올리고 verified false 로 준다(2026-10-19 「중간 방법 1」).
        /// 검증기가 다시 돌려 같으면 verified true(표시가 사라짐) · 어긋나면 랭킹에서 빠지고 내 기록에 「거절됨」. 옛 서버(verified 없음)는 검증된 것으로 본다.
        /// </summary>
        static bool Unverified(JToken r) => r?["verified"]?.Type == JTokenType.Boolean && !(bool)r["verified"];
        /// <summary>「검증 전」 회색 작은 표시(이름 · 점수 뒤에 붙인다).</summary>
        static string UnverifiedTag(JToken r) => Unverified(r) ? $"  <size=62%><color={Theme.SubTag}>검증 전</color></size>" : "";
        public bool EntryRankMine { get; private set; }
        public bool FrontierBossSpine { get; private set; }
        /// <summary>마지막으로 연 저장 데이터 창의 칸이 장착 중인가(데모 단언).</summary>
        public bool SaveDataEquipped { get; private set; }
        JObject fRank;
        Action FillFrontierRank;

        // 입구 거대 보스 자리 — 몸 경계 높이(화면 높이의 몫) · 경계 왼쪽 끝(화면 폭의 몫) · 경계 아래 끝(화면 높이의 몫). 정보 판 불투명도(보스 그림 위에서도 글이 읽히게)
        const float GiantH = 0.9f, GiantLeft = 0.33f, GiantBottom = 0.11f;   // 10-19 1.22 · 0.17 · -0.04 → 줄임(몸통은 여전히 오른쪽 위 밖으로)

        /// <summary>스파인을 space 안에 크게 — 메시 경계 높이를 space 높이의 hShare 배로 맞추고, 경계의 왼쪽 아래 끝을 (left, bottom) 몫 자리에 둔다(나머지는 밖으로 잘린다).</summary>
        static void GiantFit(Spine.Unity.SkeletonGraphic g, RectTransform space, float hShare, float left, float bottom)
        {
            g.UpdateMesh();
            var mesh = g.GetLastMesh();
            if (mesh == null || mesh.vertexCount == 0) return;
            var b = mesh.bounds;
            var rt = g.rectTransform;
            Vector3 P(Vector3 l) => space.InverseTransformPoint(rt.TransformPoint(l));
            float h = Mathf.Max(1, P(b.max).y - P(b.min).y);
            rt.localScale *= space.rect.height * hShare / h;
            var lo = P(b.min);
            var want = new Vector3(space.rect.xMin + space.rect.width * left, space.rect.yMin + space.rect.height * bottom, lo.z);
            rt.position += space.TransformVector(want - lo);
        }

        /// <summary>
        /// 입구 보스 판의 한 줄 · 주요 패턴(2026-10-19 사용자 「펼친 목록」 — 칩 + 팝업 대신 늘 보이는 세로 목록: 아이콘 · 굵은 이름 · 짧은 한 줄 풀이).
        /// 콘텐츠 blurb 는 그대로 둔다(바꾸면 콘텐츠 지문이 바뀌어 검증 테이프를 다시 만들어야 한다) — 화면 글만. 수치는 보스 데이터에서 읽는다.
        /// 모르는 보스는 blurb 첫 문장 + 쌓이는 수치(Counters) 줄.
        /// </summary>
        (string line, List<(string name, string desc, string icon, Color col)> marks) BossBrief(EnemyDef boss, FrontierSeason fs)
        {
            var marks = new List<(string, string, string, Color)>();
            if (boss == null) return ("", marks);
            Color red = Theme.Hex("FF7A86"), gold = Theme.Gold, purple = Theme.Hex("C29BFF"), blue = Theme.Hex("78C6FF");
            var all = boss.Intents.Concat(boss.Phase?.Intents ?? new List<Intent>()).Concat(boss.Phase2?.Intents ?? new List<Intent>()).ToList();
            // 쌓이는 수치 — 「출력」 처럼 1개당 피해가 붙는 것과 「심심함」 처럼 차면 한 방을 쏘는 것을 가른다
            string Pn(int v) => v >= 0 ? "+" + v : v.ToString();
            foreach (var c in boss.Counters)
            {
                if (c.Dealt > 0 || c.Taken != 0)
                {
                    marks.Add((c.Name, $"맞을 때마다 {Pn(c.OnHit)} · 1개당 피해 +{c.Dealt * 100:0}%{(c.Max < 99 ? $"(최대 {c.Max})" : "")}", "ic_up", gold));
                    continue;
                }
                var how = new List<string>();
                if (c.OnTurnEnd != 0) how.Add($"턴 끝 {Pn(c.OnTurnEnd)}");
                if (c.OnTurnStart != 0) how.Add($"턴 시작 {Pn(c.OnTurnStart)}");
                if (c.OnHit != 0) how.Add($"맞으면 {Pn(c.OnHit)}");
                if (c.OnCard != 0) how.Add($"카드마다 {Pn(c.OnCard)}");
                if (c.At > 0 && c.Act != null)
                {
                    string kind = c.Act.T == "attackAll" ? "전체" : c.Act.T == "attack" ? "단일" : "행동";
                    how.Add($"{c.At} 이면 {kind}{(c.Act.V > 0 ? " " + c.Act.V : "")}");
                }
                marks.Add((c.Name, string.Join(" · ", how), "ic_moon", purple));
            }
            if (boss.Id == "frontier_meow")
            {
                List<int> Charged(Func<Intent, bool> ok, List<Intent> from) => (from ?? new List<Intent>()).Where(x => x.T == "charge" && x.Next != null && ok(x)).Select(x => x.Next.V).ToList();
                bool IsCannon(Intent x) => x.Brk && x.Next.T == "attackAll";
                bool IsPress(Intent x) => !x.Brk && x.Next.T == "attack";
                var cannon = Charged(IsCannon, all);
                if (cannon.Count > 0) marks.Add(("캐논", $"충전 → 다음 턴 전체 {string.Join(" → ", cannon)} · 격파로 끊김", "ic_swords", red));
                var press = Charged(IsPress, boss.Intents);
                var pressHunt = Charged(IsPress, boss.Phase?.Intents);
                if (press.Count + pressHunt.Count > 0)
                {
                    string v = press.Count > 0 ? press[0].ToString() : pressHunt[0].ToString();
                    string hunt = press.Count > 0 && pressHunt.Count > 0 ? $"(사냥 {pressHunt[0]})" : "";
                    marks.Add(("프레스", $"다음 턴 단일 {v}{hunt} 예고 · 실드로 받기", "ic_sword", red));
                }
                var drone = all.FirstOrDefault(x => x.T == "summon");
                var dd = drone != null ? P.Data.Enemy(drone.Id) : null;
                if (dd != null) marks.Add(("드론", $"HP {dd.Hp:N0} · 보스에 실드 · 레이저 {dd.Intents.FirstOrDefault(x => x.T == "attack")?.V}", "ic_plus", gold));
                var sig = all.FirstOrDefault(x => x.T == "addCard" && x.Id == "st_signal");
                if (sig != null) marks.Add(("통제 신호", $"「{P.Data.Card(sig.Id)?.Name ?? "통제 신호"}」 {sig.N}장을 뽑을 더미에", "ic_deck", purple));
                if (boss.Phase != null && boss.Phase2 != null) marks.Add(("사냥 · 폭주", $"HP {boss.Phase.At * 100:0}% 사냥 · {boss.Phase2.At * 100:0}% 폭주 — 공격 강화", "ic_fire", red));
                return ("맞을수록 세지는 고양이 전투 기계", marks);
            }
            var b = boss.Blurb ?? "";
            int dot = b.IndexOfAny(new[] { '.', '—' });
            return (dot > 0 ? b.Substring(0, dot).Trim() : b, marks);
        }

        /// <summary>입구 보스 판의 패턴 줄 수 · 목록이 판 안에서 스크롤되나(데모 단언).</summary>
        public int FrontierMarks { get; private set; }
        public bool FrontierMarksScroll { get; private set; }
        /// <summary>입구 왼쪽 — 보스 판과 내 기록 판 사이 틈(캔버스 단위, 음수면 겹침).</summary>
        public float FrontierGap { get; private set; }

        // 머리 · 판 · 판 머리 글(ScreenHead · FPanel · FLabel)은 모든 메뉴 화면이 같이 쓴다 — Flow.ScreenKit.cs

        /// <summary>프론티어 입구(로비 메뉴) — 왼쪽 보스 판(패턴 목록) · 내 기록, 오른쪽 위 거대 보스 · 랭킹, 아래 「진입」.</summary>
        public void FrontierScreen()
        {
            ReadPick();
            Stage.SetBg("stage37_1", 0.72f);
            fMe = null; fRank = null;
            Stage.Show("frontier", BuildFrontier);
            if (Online.Ready) Online.Call("GET", "/api/frontier/me", null, r => { if (r.Ok) { fMe = r.Json; FillFrontierMe?.Invoke(); FillFrontierRank?.Invoke(); } });
            if (Online.Up) Online.Call("GET", "/api/frontier/ranking?limit=7", null, r => { fRank = r.Ok ? r.Json : new JObject { ["error"] = Online.ErrorText(r) }; FillFrontierRank?.Invoke(); });
        }
        Action FillFrontierMe;

        void BuildFrontier(RectTransform root)
        {
            var season = Online.Season;
            var fs = SeasonOf(season);
            var boss = P.Data.Enemy(fs.Boss);
            var size = Stage.Size;
            float top = FTop, g = Theme.Gutter, barH = Theme.BtnMainH;
            float lw = Mathf.Min(480, size.x * 0.3f), rw = Mathf.Min(400, size.x * 0.25f);
            float recH = Theme.C(128, 112);
            // ── 머리 ──
            ScreenHead(root, "프론티어", $"사도 셋 · {fs.Turns}턴 피해 랭킹", Lobby, hd =>
            {
                if (!Preseason(season)) return;
                HeadChip(hd, SeasonLabel(season), ChipKind.Gold, "ic_spark");
            });

            // ── 왼쪽 — 보스 판(이번 보스 · 이름 · HP · 한 줄 · 주요 패턴 목록 · 규칙). 내 기록 판 위까지 꽉 — 패턴이 넘치면 판 안에서 스크롤 ──
            var (line, marks) = BossBrief(boss, fs);
            var bp = FPanel(root, "boss");
            bp.anchorMin = new Vector2(0, 0); bp.anchorMax = new Vector2(0, 1); bp.pivot = new Vector2(0, 1);
            bp.offsetMin = new Vector2(g, g + recH + FGap); bp.offsetMax = new Vector2(g + lw, -top);
            float bpH = size.y - top - (g + recH + FGap);
            FLabel(bp, "이번 보스", 14);
            float nameH = Theme.C(62, 50), y = 46;
            var bn = Ui.Title(bp, boss?.Name ?? fs.Boss, Theme.FsDisplay - 18, Theme.Ink, TextAlignmentOptions.TopLeft);
            bn.rectTransform.Band(1, nameH, FPad - 2, FPad, -y); bn.Outline(0.12f);
            bn.textWrappingMode = TextWrappingModes.NoWrap; bn.overflowMode = TextOverflowModes.Overflow; bn.enableAutoSizing = true; bn.fontSizeMin = Theme.FsXl; bn.fontSizeMax = Theme.FsDisplay - 18;
            bn.colorGradient = new VertexGradient(Color.white, Color.white, Theme.Gold, Theme.Gold);
            y += nameH + 2;
            int bossHp = boss != null ? (int)Math.Round(boss.Hp * fs.Hp) : 0;
            var bhp = Ui.Title(bp, $"<size=70%><color={Theme.SubTag}>HP</color></size> {bossHp:N0}   <size=70%><color={Theme.SubTag}>제한</color></size> <color={Theme.GoldTag}>{fs.Turns}턴</color>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.TopLeft);
            bhp.rectTransform.Band(1, 32, FPad, FPad, -y); bhp.textWrappingMode = TextWrappingModes.NoWrap;
            y += 34;
            var lt = Ui.Text(bp, line, Theme.FsBody, Theme.Sub, TextAlignmentOptions.TopLeft);
            lt.rectTransform.Band(1, 26, FPad, FPad, -y); lt.textWrappingMode = TextWrappingModes.NoWrap; lt.enableAutoSizing = true; lt.fontSizeMin = 13; lt.fontSizeMax = Theme.FsBody;
            y += 26 + 12;
            var sep = Ui.Img(bp, Theme.Round, Theme.Sub.A(0.18f), "sep"); sep.rectTransform.Band(1, 1, FPad, FPad, -y);
            y += 10;
            FrontierMarks = 0; FrontierMarksScroll = false;
            const float ruleH = 26;
            if (marks.Count > 0)
            {
                FLabel(bp, "주요 패턴", y);
                y += 34;
                float rowH = Theme.C(48, 44), gapR = 3;
                var area = Ui.Rect("marks", bp).Fill(FPad - 8, FPad + ruleH - 4, FPad - 8, y);
                float areaH = bpH - y - (FPad + ruleH - 4);
                var content = Ui.Scroll(area, out _);
                Ui.Col(content, gapR, TextAnchor.UpperLeft, new RectOffset(0, 6, 0, 2), true, false);
                foreach (var (nm, desc, icon, col) in marks)
                {
                    var row = Ui.Img(content, Theme.Round, Theme.NavyWell.A(0.55f), "mark " + nm);
                    row.Pref(-1, rowH);
                    var rrt = row.rectTransform;
                    var disc = Ui.Img(rrt, Theme.S("round"), Color.Lerp(Theme.NavyWell, col, 0.28f), "disc"); disc.rectTransform.At(0, 0.5f, 8, 0, 32, 32);
                    var ring = Ui.Img(disc.transform, Theme.S("frame_pill"), col.A(0.85f), "ring"); ring.rectTransform.Fill();
                    var gl = Ui.Img(disc.transform, Theme.S(icon), col, "glyph"); gl.preserveAspect = true; gl.rectTransform.Fill(7, 7, 7, 7);
                    var tn = Ui.Title(rrt, nm, Theme.FsBody, Theme.Ink, TextAlignmentOptions.BottomLeft); tn.rectTransform.Fill(50, rowH * 0.5f - 2, 8, 2); tn.textWrappingMode = TextWrappingModes.NoWrap;
                    var td = Ui.Text(rrt, desc, Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft); td.rectTransform.Fill(50, 2, 8, rowH * 0.5f + 1);
                    td.textWrappingMode = TextWrappingModes.NoWrap; td.enableAutoSizing = true; td.fontSizeMin = 11; td.fontSizeMax = Theme.FsCap; td.lineSpacing = 0;
                    FrontierMarks++;
                }
                FrontierMarksScroll = marks.Count * (rowH + gapR) > areaH + 1;
            }
            var rules = Ui.Text(bp, "성격 상성 없음", Theme.FsCap, Theme.Dim, TextAlignmentOptions.MidlineLeft); rules.rectTransform.Band(0, ruleH, FPad, FPad, FPad - 10);
            Tw.Rise(bp, 0.08f, 24, 0.4f, Vector2.left);

            // ── 왼쪽 아래 — 내 기록(단추 줄과 바닥을 맞춘다) ──
            var mine = FPanel(root, "mine");
            mine.At(0, 0, g, g, lw, recH);
            FrontierGap = size.y - g - recH - (top + bpH);   // 보스 판 밑 ↔ 내 기록 판 위(음수면 겹침 — 데모 단언)
            Tw.Rise(mine, 0.12f, 24, 0.4f, Vector2.left);
            FLabel(mine, "내 기록", 14);
            var hisB = Btn.Make(mine, "기록 보기", BtnStyle.PillDark, () => NeedAccount("로그인하면 내 기록을 볼 수 있다", () => HistoryScreen(FrontierScreen)), Theme.FsSm, "frontier.history");
            hisB.GetComponent<RectTransform>().At(1, 1, -16, -12, 128, 40);
            if (!Online.Ready) hisB.Interactable = false;
            Stage.Hot["frontier.history"] = hisB;
            var best = Ui.Title(mine, "", Theme.Fs2xl - 6, Theme.Ink, TextAlignmentOptions.MidlineLeft); best.rectTransform.Band(0, 44, FPad, FPad, Theme.C(34, 30));
            best.textWrappingMode = TextWrappingModes.NoWrap; best.enableAutoSizing = true; best.fontSizeMin = Theme.FsMd; best.fontSizeMax = Theme.Fs2xl - 6;
            var meLine = Ui.Text(mine, "", Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineLeft); meLine.rectTransform.Band(0, 26, FPad, FPad, 10);
            meLine.textWrappingMode = TextWrappingModes.NoWrap; meLine.enableAutoSizing = true; meLine.fontSizeMin = 11; meLine.fontSizeMax = Theme.FsSm;
            void FillMe()
            {
                if (!best) return;
                if (!Online.Up) { best.text = $"<color={Theme.SubTag}>온라인 준비 중</color>"; meLine.text = "보관함(이 기기 사본)만 열린다"; return; }
                if (!Online.Ready) { best.text = $"<color={Theme.SubTag}>{(Online.Now == Online.State.NeedName ? "닉네임을 정하면 도전할 수 있다" : "로그인하면 도전할 수 있다")}</color>"; meLine.text = "랭킹은 로그인 없이 볼 수 있다"; return; }
                if (fMe == null) { best.text = "…"; meLine.text = "기록을 받는 중…"; return; }
                var b = fMe["best"] as JObject;
                best.text = b != null ? $"{(int)b["score"]:N0}<size=50%><color={Theme.SubTag}> 점 · 최고{(Unverified(b) ? " · 검증 전" : "")}</color></size>" : $"<color={Theme.SubTag}>낸 점수 없음</color>";
                var rank = fMe["rank"];
                meLine.text = $"{Online.Nick} · {(rank != null && rank.Type == JTokenType.Integer ? $"{(int)rank}위" : "순위 없음")}";
            }
            FillFrontierMe = FillMe;
            FillMe();

            // ── 오른쪽 — 랭킹(위는 보스 판과, 아래는 단추 줄 위에 맞춘다) ──
            var rk = FPanel(root, "rank");
            rk.anchorMin = new Vector2(1, 0); rk.anchorMax = new Vector2(1, 1); rk.pivot = new Vector2(1, 0.5f);
            rk.offsetMin = new Vector2(-g - rw, g + barH + FGap); rk.offsetMax = new Vector2(-g, -top);
            Tw.Rise(rk, 0.16f, 24, 0.4f, Vector2.right);
            FLabel(rk, "랭킹", 14, "ic_crown");
            float rkBottom = 16 + 48 + 8 + 40 + 6;   // 「랭킹 전체」 · 내 순위 줄 · 틈
            var rows = Ui.Rect("rows", rk).Fill(12, rkBottom, 12, 54);
            Ui.Col(rows, 4, TextAnchor.UpperLeft, null, true, false);
            var rsep = Ui.Img(rk, Theme.Round, Theme.Sub.A(0.18f), "sep"); rsep.rectTransform.Band(0, 1, FPad, FPad, rkBottom - 4);
            var myRank = Ui.Text(rk, "", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            myRank.rectTransform.Band(0, 40, FPad, FPad, 16 + 48 + 8);
            myRank.textWrappingMode = TextWrappingModes.NoWrap; myRank.enableAutoSizing = true; myRank.fontSizeMin = 11; myRank.fontSizeMax = Theme.FsSm;
            var allB = Btn.Make(rk, "랭킹 전체", BtnStyle.PillDark, () => RankingScreen(FrontierScreen), Theme.FsMd, "frontier.rank");
            allB.GetComponent<RectTransform>().Band(0, 48, 16, 16, 16);
            if (!Online.Up) allB.Interactable = false;
            Stage.Hot["frontier.rank"] = allB;
            void FillRank()
            {
                if (!rows) return;
                for (int c = rows.childCount - 1; c >= 0; c--) Destroy(rows.GetChild(c).gameObject);
                EntryRankRows = 0; EntryRankMine = false; EntryRankPending = 0;
                if (!Online.Up) { Ui.Text(rows, "랭킹 준비 중", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center).Pref(-1, 120); myRank.text = $"<color={Theme.SubTag}>온라인 기능이 열리면 오른다</color>"; return; }
                if (fRank == null) { Ui.Text(rows, "랭킹을 받는 중…", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center).Pref(-1, 80); }
                else if (fRank["error"] != null) Ui.Text(rows, "랭킹을 받지 못했다 — " + fRank["error"], Theme.FsSm, Theme.Bad, TextAlignmentOptions.Center).Pref(-1, 80);
                else
                {
                    var list = (fRank["rows"] as JArray)?.OfType<JObject>().ToList() ?? new List<JObject>();
                    if (list.Count == 0) EmptyNote(rows, "낸 점수가 없다", null, 90);
                    foreach (var r in list)
                    {
                        bool me = Online.Nick != null && (string)r["name"] == Online.Nick;
                        int n = (int)r["rank"];
                        var lineImg = Ui.Img(rows, Theme.Round, me ? Theme.Gold.A(0.28f) : n % 2 == 0 ? Theme.NavyCell.A(0.5f) : new Color(0, 0, 0, 0), "row " + n);
                        var ln = lineImg.rectTransform;
                        ln.Pref(-1, 46);
                        if (r["attempt"]?.Type == JTokenType.String) { string aid = (string)r["attempt"]; Stage.Hot["frank.row" + n] = RowTap(lineImg, () => AttemptScreen(aid, FrontierScreen)); }
                        var a = Ui.Title(ln, n <= 3 ? $"<color={Theme.GoldTag}>{n}</color>" : n.ToString(), Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center); a.rectTransform.At(0, 0.5f, 0, 0, 46, 42);
                        var nm = Ui.Title(ln, (string)r["name"] + (me ? $"  <size=70%><color={Theme.GoldTag}>나</color></size>" : "") + UnverifiedTag(r), Theme.FsBody, me ? Theme.Gold : Theme.Ink, TextAlignmentOptions.MidlineLeft);
                        nm.rectTransform.Fill(46, 0, 96 + 88 + 14, 0); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                        nm.enableAutoSizing = true; nm.fontSizeMin = 11; nm.fontSizeMax = Theme.FsBody;
                        var sc = Ui.Title(ln, $"{(int)r["score"]:N0}", Theme.FsLg, me ? Theme.Gold : Theme.Ink, TextAlignmentOptions.MidlineRight); sc.rectTransform.At(1, 0.5f, -(88 + 12), 0, 96, 42); sc.enableAutoSizing = true; sc.fontSizeMin = 12; sc.fontSizeMax = Theme.FsLg;
                        // 점수 오른쪽에 사도 셋 얼굴(작게 · 줄 높이 안). 못 찾는 키는 빈 원
                        if (r["heroes"] is JArray eh && eh.Count > 0)
                        {
                            var ef = Ui.Rect("faces", ln).At(1, 0.5f, -8, 0, 84, 26);
                            Ui.Row(ef, 3, TextAnchor.MiddleRight, null, false, false);
                            foreach (var h in eh.Take(3)) W.Face(ef, Roster.OfCore((string)h), 26);
                        }
                        EntryRankRows++; if (me) EntryRankMine = true;
                        if (Unverified(r)) EntryRankPending++;
                    }
                }
                if (!Online.Ready) myRank.text = $"<color={Theme.SubTag}>로그인하면 내 순위가 보인다</color>";
                else if (fMe == null) myRank.text = $"<color={Theme.SubTag}>내 순위를 받는 중…</color>";
                else
                {
                    var b = fMe["best"] as JObject; var rr = fMe["rank"];
                    myRank.text = b != null && rr != null && rr.Type == JTokenType.Integer ? $"내 순위  <color={Theme.GoldTag}><size=125%>{(int)rr}위</size></color>   {(int)b["score"]:N0}{UnverifiedTag(b)}" : $"내 순위  <color={Theme.SubTag}>아직 없음</color>";
                }
            }
            FillFrontierRank = FillRank;
            FillRank();

            // ── 보스 — 화면을 채우는 거대 보스(2026-10-18 사용자: M.E.O.W 는 몸통이 화면 오른쪽 위 밖에 있고 앞발 · 머리가 내려온다).
            //    맨 뒤 층에 화면 높이보다 크게 세우고 · 몸 경계의 왼쪽 아래(앞발)를 가운데 아래에 둔다 — 몸통은 오른쪽 위로 잘려 나간다. 정보 판은 반투명으로 그 위에
            var stage = Ui.Rect("bossstage", root).Fill();
            stage.SetSiblingIndex(0);
            var glow = Ui.Img(stage, Theme.S("soft"), Theme.Bad.A(0.30f), "glow"); glow.rectTransform.At(0.55f, 0.42f, 0, 0, 1100, 900);
            Tw.Pulse(glow, 0.18f, 0.4f, 2.6f);
            string folder = boss?.Art?.Spine;
            if (folder != null && folder.Contains("/")) folder = folder.Substring(folder.LastIndexOf('/') + 1);
            var sg = folder != null ? SpineUi.Make(stage, folder, boss.Art.Skin, size.y, "Idle", "idle", "Idle_1") : null;
            FrontierBossSpine = sg != null;
            if (sg != null) { GiantFit(sg, stage, GiantH, GiantLeft, GiantBottom); Tw.FadeIn(sg, 0.5f, 0.1f); }
            else
            {
                var pic = boss != null ? FoeArt(boss, 1) : null;
                if (pic != null) { var pim = Ui.Img(stage, pic, Color.white, "pic"); pim.preserveAspect = true; pim.rectTransform.Fill(lw + g * 2, 120, rw + g * 2, 100); }
            }
            Debug.Log($"[Frontier] 입구 — 보스 {boss?.Name} 스파인 {(sg != null ? folder + "/" + boss?.Art?.Skin : "없음(그림)")} · 패턴 {FrontierMarks}줄{(FrontierMarksScroll ? "(판 안 스크롤)" : "")} · 판 틈 {FrontierGap:0} · Compact {Theme.Compact} · 온라인 {Online.Now}");

            // ── 아래 단추 줄(오른쪽 — 랭킹 판 오른쪽 끝 · 내 기록 판 바닥과 맞춤) ──
            var bar = Ui.Rect("bar", root);
            bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(0.5f, 0);
            bar.offsetMin = new Vector2(g * 2 + lw, g); bar.offsetMax = new Vector2(-g, g + barH);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var box = NavyPill(bar, "ic_deck", "보관함", () => SlotBox(FrontierScreen), "frontier.box", -1, barH);
            var go = Btn.Make(bar, !Online.Up ? "준비 중" : "진입", BtnStyle.PillGold, () =>
            {
                if (!Online.Up) { Toast.Show("온라인 기능은 준비 중이다 — " + (Online.OffWhy ?? ""), 3f); return; }
                NeedAccount("로그인하면 프론티어에 도전할 수 있다", FrontierTeam);
            }, Theme.FsXl, "frontier.enter");
            go.Pref(Theme.BtnMainW + 20, -1);
            if (!Online.Up) go.Interactable = false;
            Stage.Hot["frontier.box"] = box; Stage.Hot["frontier.enter"] = go;
        }

        // ── 편성 — 사도 셋 자리(세로 그림 카드) · 아래 덱 칸(장착한 저장 칸) → 「도전」 ──
        /// <summary>편성 화면(입구의 「진입」 뒤).</summary>
        public void FrontierTeam()
        {
            ReadPick();
            Stage.SetBg("stage37_1", 0.8f);
            void Go(bool synced)
            {
                // 칸이 사라졌거나(지움 · 다른 기기) 쓸 수 없게 된 편성은 비운다
                for (int i = 0; i < fPick.Length; i++)
                    if (fPick[i] != null && (PickOf(i) == null || synced && PickOf(i).Status == "bad")) fPick[i] = null;
                SavePick();
                Stage.Show("frontier_team", BuildFrontierTeam);
            }
            if (Online.Ready) SyncSlots(Go); else Go(false);
        }
        void TeamAgain() => Stage.Show("frontier_team", BuildFrontierTeam, 0.1f, true);

        void BuildFrontierTeam(RectTransform root)
        {
            ScreenHead(root, "편성", "저장한 사도 셋으로 도전", FrontierScreen);

            var size = Stage.Size;
            float top = FTop, g = Theme.Gutter, barH = Theme.BtnMainH, bottom = g + barH + 18;
            float availH = size.y - top - bottom, availW = size.x - g * 2, gap = 30;
            float deckH = Mathf.Clamp(availH * 0.25f, 120, 190), cardH = availH - deckH - 14;
            float cw = Mathf.Min((availW - gap * 2) / 3f, cardH * 0.74f);
            var cols = Ui.Rect("cols", root);
            cols.anchorMin = new Vector2(0, 0); cols.anchorMax = new Vector2(1, 1);
            cols.offsetMin = new Vector2(g, bottom); cols.offsetMax = new Vector2(-g, -top);
            for (int i = 0; i < fPick.Length; i++)
            {
                int k = i;
                var col = Ui.Rect("seat" + i, cols).At(0.5f, 0.5f, (i - 1) * (cw + gap), 0, cw, availH);
                Tw.Rise(col, 0.06f + i * 0.06f, 24, 0.4f);
                var e = PickOf(i);
                var h = e != null ? Roster.OfCore(e.Hero) : null;
                var nc = h != null ? Theme.NatureOf(h.nature) : Theme.Sub;

                // 사도 카드
                var hb = Btn.Make(col, null, BtnStyle.Cell, () => FrontierHeroPick(k), 0, "seat" + i);
                var hrt = hb.GetComponent<RectTransform>(); hrt.At(0.5f, 1, 0, 0, cw, cardH);
                Stage.Hot["frontier.seat" + i] = hb;
                if (e == null)
                {
                    hb.Bg.color = Color.white.A(0.55f);
                    var plus = Ui.Img(hrt, Theme.S("ic_plus"), Theme.Dim, "plus"); plus.preserveAspect = true; plus.rectTransform.At(0.5f, 0.5f, 0, 30, 84, 84);
                    var pt = Ui.Title(hrt, "사도 고르기", Theme.FsXl, Theme.Sub, TextAlignmentOptions.Center); pt.rectTransform.At(0.5f, 0.5f, 0, -44, cw - 20, 44);
                    var ps = Ui.Text(hrt, $"자리 {i + 1} · 저장한 사도에서", Theme.FsSm, Theme.Dim, TextAlignmentOptions.Center); ps.rectTransform.At(0.5f, 0.5f, 0, -84, cw - 20, 30);
                }
                else
                {
                    var glow = Ui.Img(hrt, Theme.S("soft"), nc.A(0.45f), "glow"); glow.rectTransform.At(0.5f, 0.55f, 0, 0, cw * 1.1f, cardH * 0.8f);
                    var win = Ui.Rect("win", hrt).Fill(5, 5, 5, 5); win.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                    var art = h?.art != null ? CardArt.Upper(h.art, (cw - 10) / (cardH - 10), 0.74f) : null;
                    if (art != null) Ui.Img(win, art, Color.white, "art").rectTransform.Fill();
                    else if (h?.Icon != null) { var im = Ui.Img(win, h.Icon, Color.white, "icon"); im.preserveAspect = true; im.rectTransform.Fill(20, 80, 20, 20); }
                    var shade = Ui.Img(hrt, Theme.S("fade_card"), Color.white, "shade"); shade.rectTransform.Fill(5, 5, 5, 5);
                    var edge = Ui.Img(hrt, Theme.Round, nc, "tone"); edge.rectTransform.At(0, 0.5f, 5, 0, 5, cardH - 30);
                    // 왼쪽 위 — 성격 · 역할 아이콘 세로 줄
                    var ni = Theme.Icon("성격_" + h?.nature); var ri = Theme.Icon("역할_" + h?.role);
                    if (ni != null) { var a = Ui.Img(hrt, ni, Color.white, "nat"); a.preserveAspect = true; a.rectTransform.At(0, 1, 18, -16, 46, 46); }
                    if (ri != null) { var a = Ui.Img(hrt, ri, Color.white, "role"); a.preserveAspect = true; a.rectTransform.At(0, 1, 18, -68, 46, 46); }
                    // 오른쪽 위 — 돋보기(카드 · 장비 크게 보기)
                    var zb = Btn.Icon(hrt, Theme.S("ic_zoom"), () => SlotView(e), 48);
                    zb.GetComponent<RectTransform>().At(1, 1, -14, -14, 48, 48);
                    Stage.Hot["frontier.zoom" + i] = zb;
                    // 왼쪽 아래 — 이름 · 별 · 성격 · 역할, 오른쪽 아래 — 바꾸기
                    var nm = Ui.Title(hrt, h?.ko ?? e.Hero, Theme.Fs2xl - 6, Color.white, TextAlignmentOptions.BottomLeft);
                    nm.rectTransform.Band(0, 50, 20, 70, 44); nm.Outline(0.25f); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 18; nm.fontSizeMax = Theme.Fs2xl - 6;
                    var sub = Ui.Text(hrt, $"{new string('★', Mathf.Clamp(h?.star ?? 0, 0, 3))}  <color=#{ColorUtility.ToHtmlStringRGB(nc)}>{h?.nature}</color> · {h?.role}", Theme.FsSm, Theme.Ink, TextAlignmentOptions.BottomLeft);
                    sub.rectTransform.Band(0, 28, 22, 70, 16); sub.Outline(0.2f);
                    var sw = Ui.Img(hrt, Theme.Pill, new Color(0, 0, 0, 0.55f), "swap"); sw.rectTransform.At(1, 0, -14, 16, 54, 54);
                    var swi = Ui.Img(sw.rectTransform, Theme.S("ic_refresh"), Color.white, "ic"); swi.preserveAspect = true; swi.rectTransform.Fill(13, 13, 13, 13);
                }

                // 덱 칸 — 장착한 저장 칸(이름 · 상태 · 이 칸 최고)
                var db = Btn.Make(col, null, BtnStyle.Cell, () => { if (e != null) FrontierSaveData(k, e.Hero, e.Id); else Toast.Show("사도를 먼저 골라야 한다"); }, 0, "deck" + i);
                var drt = db.GetComponent<RectTransform>(); drt.At(0.5f, 0, 0, 0, cw, deckH);
                Stage.Hot["frontier.deck" + i] = db;
                db.Bg.sprite = Theme.Round; db.SetColor(Color.Lerp(Theme.NavyWell, nc, e != null ? 0.22f : 0.05f));
                var dg = Ui.Img(drt, Theme.S("soft"), nc.A(e != null ? 0.3f : 0.08f), "glow"); dg.rectTransform.At(1, 0, -60, 20, cw * 0.9f, deckH * 1.2f);
                var di = Ui.Img(drt, Theme.S("ic_deck"), Color.white.A(0.035f), "mark"); di.preserveAspect = true; di.rectTransform.At(1, 0.5f, -150, 0, deckH * 0.8f, deckH * 0.8f);
                if (e == null)
                {
                    var dt = Ui.Text(drt, "저장 칸", Theme.FsMd, Theme.Dim, TextAlignmentOptions.Center); dt.rectTransform.Fill();
                    db.Interactable = false;
                    continue;
                }
                var ct = Ui.Title(drt, e.Label, Theme.FsLg, Theme.Ink, TextAlignmentOptions.TopLeft); ct.rectTransform.Band(1, 34, 18, 120, -12);
                ct.textWrappingMode = TextWrappingModes.NoWrap; ct.overflowMode = TextOverflowModes.Ellipsis;
                var (cbg, ctx) = Ui.Chip(drt, null, SlotStore.StatusKo(e.Status), 30, SlotStore.StatusColor(e.Status).A(0.25f), null, Theme.FsCap);
                ctx.color = SlotStore.StatusColor(e.Status); cbg.rectTransform.At(1, 1, -14, -14, 96, 30);
                var sl = Ui.Text(drt, SlotLine(e.Slot), Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft);
                sl.rectTransform.Band(1, 24, 18, 18, -50); sl.textWrappingMode = TextWrappingModes.NoWrap; sl.overflowMode = TextOverflowModes.Ellipsis;
                var bestV = SlotBest(e);
                var bv = Ui.Title(drt, bestV != null ? $"{bestV.Value:N0}<size=55%><color={Theme.SubTag}> 점</color></size>" : $"<size=60%><color={Theme.DimTag}>기록 없음</color></size>", Theme.Fs2xl - 4, Theme.Gold, TextAlignmentOptions.BottomRight);
                bv.rectTransform.Band(0, 50, 18, 18, 10); bv.Outline(0.15f);
                var bl = Ui.Text(drt, $"저장 {SlotStore.Date(e.Updated)}", Theme.FsCap, Theme.Sub, TextAlignmentOptions.BottomLeft); bl.rectTransform.Band(0, 24, 18, 18, 16);
            }

            // 아래 줄 — 안내 · 도전
            var bar = Ui.Rect("bar", root).Band(0, barH, g, g, g);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            int count = Enumerable.Range(0, fPick.Length).Count(x => PickOf(x) != null);
            var info = Ui.Text(bar, $"<color={Theme.GoldTag}>편성 {count} / {fPick.Length}</color>   <color={Theme.SubTag}>칸을 누르면 바꿀 수 있다</color>", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.Pref(-1, -1, 1); info.Outline(0.2f); info.enableAutoSizing = true; info.fontSizeMin = 11; info.fontSizeMax = Theme.FsSm;
            var go = Btn.Make(bar, "도전", BtnStyle.PillGold, () => NeedAccount("로그인하면 프론티어에 도전할 수 있다", StartFrontier), Theme.FsXl, "frontier.go");
            go.Pref(Theme.BtnMainW + 60, -1);
            if (!Online.Up || count == 0) go.Interactable = false;
            Stage.Hot["frontier.go"] = go;
        }

        /// <summary>자리 i 의 사도 고르기 — 저장 칸이 있는 사도 목록. 다른 자리에 든 사도 · 쓸 수 있는 칸이 없는 사도는 못 고른다. 고르면 그 사도의 첫 쓸 수 있는 칸을 장착.</summary>
        void FrontierHeroPick(int i)
        {
            var size = Stage.Size;
            float w = Mathf.Min(1240, size.x - 48), hh = Mathf.Min(780, size.y - 24);
            var (body, close, foot) = Stage.ModalBox("frontierhero", w, hh, $"자리 {i + 1} — 사도 고르기", "같은 사도는 한 파티에 하나만", true, null, Theme.BtnH + 24);
            var others = fPick.Where((x, j) => j != i && x != null).Select(x => x.Value.hero).ToHashSet();
            var content = Ui.Scroll(Ui.Rect("area", body).Fill(), out _);
            var grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            float cw = 176, ch = 250;
            grid.cellSize = new Vector2(cw, ch); grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(8, 8, 8, 12);
            var heroes = SlotStore.Heroes;
            foreach (var hero in heroes)
            {
                var hi = Roster.OfCore(hero);
                var slots = SlotStore.Of(hero).ToList();
                var use = slots.Where(x => SlotWhyNot(x) == null).OrderBy(x => x.Status == "ok" ? 0 : 1).ThenByDescending(x => SlotBest(x) ?? -1).ThenByDescending(x => x.Updated).FirstOrDefault();
                string why = others.Contains(hero) ? "다른 자리에 있음" : use == null ? "쓸 수 있는 칸 없음" : null;
                bool cur = fPick[i]?.hero == hero;
                var b = Btn.Make(content, null, BtnStyle.Cell, () =>
                {
                    if (why != null) { Toast.Show(why == "다른 자리에 있음" ? "같은 사도는 한 파티에 하나만 넣을 수 있다" : "이 사도의 칸은 모두 쓸 수 없다 — " + SlotWhyNot(slots.FirstOrDefault()), 3f); return; }
                    long id = cur && SlotWhyNot(PickOf(i)) == null ? fPick[i].Value.id : use.Id;
                    SetFrontierPick(i, hero, id); close(); TeamAgain();
                }, 0, "hero " + hero);
                if (cur) b.Bg.sprite = Theme.CellOn;
                if (why != null) b.Interactable = false;
                var rt = b.GetComponent<RectTransform>();
                var pic = Ui.Img(rt, Theme.Round, Theme.NavyWell, "pic"); pic.rectTransform.At(0.5f, 1, 0, -8, cw - 16, ch - 92);
                pic.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                var up = hi?.art != null ? CardArt.Upper(hi.art, (cw - 16) / (ch - 92), 0.5f) : null;
                if (up != null) Ui.Img(pic.rectTransform, up, why != null ? new Color(0.5f, 0.5f, 0.55f) : Color.white, "art").rectTransform.Fill();
                else if (hi?.Icon != null) { var im = Ui.Img(pic.rectTransform, hi.Icon, Color.white, "icon"); im.preserveAspect = true; im.rectTransform.Fill(8, 8, 8, 8); }
                var ni = Theme.Icon("성격_" + hi?.nature);
                if (ni != null) { var a = Ui.Img(rt, ni, Color.white, "nat"); a.preserveAspect = true; a.rectTransform.At(0, 1, 12, -12, 30, 30); }
                var nm = Ui.Title(rt, hi?.ko ?? hero, Theme.FsMd, why != null ? Theme.Dim : Theme.Ink, TextAlignmentOptions.Center); nm.rectTransform.Band(0, 32, 4, 4, 46);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 12; nm.fontSizeMax = Theme.FsMd;
                var st = Ui.Text(rt, why != null ? $"<color={Theme.BadTag}>{why}</color>" : cur ? $"<color={Theme.GoldTag}>지금 자리</color> · {SlotStore.CountKo(hero)}" : SlotStore.CountKo(hero), Theme.FsCap, Theme.Sub, TextAlignmentOptions.Center);
                st.rectTransform.Band(0, 26, 4, 4, 14);
                Stage.Hot["fhero:" + hero] = b;
            }
            if (heroes.Count == 0)
            {
                // 바로 지운다 — Destroy 는 프레임 끝이라 그리드가 남아 Ui.Col 의 AddComponent<VerticalLayoutGroup> 이 null(레이아웃 그룹은 하나만)
                //   → 웹(IL2CPP · 예외 일부만)에서는 null 접근이 「null function」 으로 멈췄다(2026-10-19 저장 사도 없는 계정)
                DestroyImmediate(grid);
                Ui.Col(content, 8, TextAnchor.UpperCenter, null, true, false);
                EmptyNote(content, "저장한 사도가 없다", "모험을 완주하면 저장할 수 있다", EmptyH);
            }
            var row = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, false, true);
            var clear = Btn.Make(row, "자리 비우기", BtnStyle.PillDark, () => { SetFrontierPick(i, null, 0); close(); TeamAgain(); }, Theme.FsMd, "fhero.clear");
            clear.Pref(220, -1);
            if (fPick[i] == null) clear.Interactable = false;
            Stage.Hot["fhero.clear"] = clear;
        }

        /// <summary>
        /// 저장 데이터 창(자리 i · 사도 hero · 보는 칸 id) — 카제나 세이브 데이터 창처럼: 왼쪽 그 사도의 칸 목록(이름 · 날짜 · 상태 · 최고 · 장착 중 — 많아지면 스크롤,
        /// 아래 최신순/점수순), 가운데 판(이름 · 연필 · 날짜 · 점수 띠 · 장비 3칸 · 요약), 오른쪽 카드 그리드, 아래 「장착」. 장착하면 편성 자리에 그 칸이 들어간다.
        /// </summary>
        public void FrontierSaveData(int i, string hero, long id)
        {
            var h = Roster.OfCore(hero);
            var size = Stage.Size;
            float w = Mathf.Min(1540, size.x - 32), hh = size.y - 24;
            var (body, close, foot) = Stage.ModalBox("savedata", w, hh, $"저장 칸 — {h?.ko ?? hero}", "장착한 칸으로 나간다", true, null, Theme.BtnH + 24);
            var list = SlotStore.Sorted(SlotStore.Of(hero), SlotStore.Order, SlotBest);
            var e = SlotStore.Get(id) ?? list.FirstOrDefault();
            if (e != null) id = e.Id;
            bool equipped = fPick[i] != null && fPick[i].Value.id == id;
            SaveDataEquipped = equipped;
            SaveDataRows = list.Count;
            void Reopen(long id2) { close(); FrontierSaveData(i, hero, id2); }
            float lw = 280, iw = Mathf.Min(440, w * 0.3f);
            RectTransform Col(string name, float x, float wd)
            {
                var r = Ui.Rect(name, body);
                r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f);
                r.offsetMin = new Vector2(x, 0); r.offsetMax = new Vector2(x + wd, 0);
                return r;
            }
            // 왼쪽 — 칸 목록(스크롤) · 아래 차례 단추
            var left = Col("list", 0, lw);
            var sortRow = Ui.Rect("sort", left).Band(0, 48, 0, 0, 0);
            if (list.Count > 1) SortToggle(sortRow, () => Reopen(id), "sdata.sort", 150, 44).GetComponent<RectTransform>().At(1, 0.5f, 0, 0, 150, 44);
            var area = Ui.Rect("area", left).Fill(0, list.Count > 1 ? 58 : 0, 0, 0);
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 10, TextAnchor.UpperLeft, new RectOffset(0, 10, 4, 8), true, false);
            for (int k = 0; k < list.Count; k++)
            {
                var se = list[k];
                bool eq = fPick[i] != null && fPick[i].Value.id == se.Id;
                var b = SlotCard(content, se, Theme.C(108, 96), false, se.Id == id, eq, () => Reopen(se.Id), "slot" + k);
                Stage.Hot["sdata.slot" + k] = b;
            }
            // 끝 — 「새로 저장 n/10」(마지막 완주 판에 이 사도가 기다리면 그 판으로 새로 저장 · 아니면 칸 수만) — 판 끝 저장 창 · 저장함과 같은 줄
            var wt = SlotStore.Wait;
            bool waiting = wt != null && wt.Slots.ContainsKey(hero) && !wt.SavedId.ContainsKey(hero);
            SlotNewRow(content, hero, Theme.C(72, 64), waiting ? () => SavePick(hero, wt.Slots[hero], ne => { close(); FrontierSaveData(i, hero, ne.Id); }) : (Action)null, "sdata.new");

            // 가운데 — 판
            var info = Ui.Panel(Col("infocol", lw + 18, iw), Theme.Panel, null, "info").rectTransform;
            info.Fill();
            var right = Col("cards", lw + 18 + iw + 18, w - (lw + 18 + iw + 18) - 56);
            if (e == null) { EmptyNote(info, "저장한 칸이 없다"); return; }
            var s = e.Slot;
            var bestV = SlotBest(e);
            SaveDataBody(info, right, iw, hero, s, $"{SlotStore.Date(e.Updated)} · <color=#{ColorUtility.ToHtmlStringRGB(SlotStore.StatusColor(e.Status))}>{SlotStore.StatusKo(e.Status)}</color>",
                bestV, bestV != null ? (BestFromServer ? "이 칸 최고 · " + BestStatusKo(e.BestStatus) : "이 칸 최고 · 이 기기") : "이 칸으로 낸 점수가 없다", SlotWhyNot(e), true, e.Label);
            var del = Btn.Make(info, null, BtnStyle.PillRose, () => Confirm($"「{e.Label}」 을 지울까?", "저장한 사도가 사라진다 — 낸 점수는 그대로", "지우기",
                () => DeleteSlot(e, () => { if (fPick[i]?.id == e.Id) SetFrontierPick(i, null, 0); close(); TeamAgain(); }), true), 0, "del");
            del.GetComponent<RectTransform>().At(1, 1, -16, -16, 52, 44);
            var dic = Ui.Img(del.GetComponent<RectTransform>(), Theme.S("ic_trash"), Color.white, "ic"); dic.preserveAspect = true; dic.rectTransform.Fill(12, 8, 12, 8);
            if (!Online.Ready || !e.OnServer) del.Interactable = false;
            Stage.Hot["sdata.del"] = del;
            var zb = Btn.Icon(info, Theme.S("ic_zoom"), () => SlotView(e), 44);
            zb.GetComponent<RectTransform>().At(1, 1, -78, -16, 44, 44);
            Stage.Hot["sdata.zoom"] = zb;
            var nb = Btn.Icon(info, Theme.S("ic_edit"), () => SlotNamePanel(e, ne => Reopen(ne.Id)), 44);
            nb.GetComponent<RectTransform>().At(1, 1, -130, -16, 44, 44);
            if (!Online.Ready || !e.OnServer) nb.Interactable = false;
            Stage.Hot["sdata.name"] = nb;

            // 아래 — 장착
            var row = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            string whyNot = SlotWhyNot(e);
            var eqB = Btn.Make(row, equipped ? "장착 중" : "장착", equipped ? BtnStyle.PillDark : BtnStyle.PillGold, () =>
            {
                if (equipped) { close(); return; }
                if (whyNot != null) { Toast.Show(whyNot, 3f); return; }
                SetFrontierPick(i, hero, e.Id); close(); TeamAgain();
                Toast.Show($"「{e.Label}」 장착", 2f);
            }, Theme.FsLg, "sdata.equip");
            eqB.Pref(Theme.BtnMainW, -1);
            if (whyNot != null) eqB.Interactable = false;
            Stage.Hot["sdata.equip"] = eqB;
        }
        /// <summary>마지막으로 연 저장 데이터 창의 칸 목록 수(데모 단언).</summary>
        public int SaveDataRows { get; private set; }

        /// <summary>
        /// 저장 데이터 창의 가운데(얼굴 · 이름 · 날짜 줄 · 점수 띠 · 장비 3칸 · 요약)와 오른쪽(카드 그리드) — 내 칸(저장 데이터 창)과
        /// 지난 시도 · 남의 편성 보기(읽기 전용 — Flow.FrontierHistory)가 함께 쓴다. 단추(지우기 · 돋보기 · 장착)는 부르는 쪽이 얹는다.
        /// </summary>
        void SaveDataBody(RectTransform info, RectTransform right, float iw, string hero, HeroSlot s, string dateLine, int? bestV, string bandLabel, string why, bool showSeed, string title = null)
        {
            var h = Roster.OfCore(hero);
            var face = W.Face(info, h, 64); face.At(0, 1, 18, -16, 64, 64);
            var nt = Ui.Title(info, title ?? h?.ko ?? hero, Theme.FsXl, Theme.Ink, TextAlignmentOptions.TopLeft); nt.rectTransform.At(0, 1, 94, -14, iw - 250, 38);
            nt.textWrappingMode = TextWrappingModes.NoWrap; nt.overflowMode = TextOverflowModes.Ellipsis; nt.enableAutoSizing = true; nt.fontSizeMin = 16; nt.fontSizeMax = Theme.FsXl;
            var dt = Ui.Text(info, dateLine, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft);
            dt.rectTransform.At(0, 1, 94, -54, iw - 110, 26);
            dt.enableAutoSizing = true; dt.fontSizeMin = 10; dt.fontSizeMax = Theme.FsSm;
            // 점수 띠
            var band = Ui.Img(info, Theme.Round, Color.Lerp(Theme.NavyWell, Theme.NatureOf(h?.nature), 0.25f), "band").rectTransform;
            float bandH = Theme.C(108, 90), gearY = 94 + bandH + 12, gearH = Theme.C(124, 92), lineH = Theme.C(38, 28);
            band.Band(1, bandH, 16, 16, -94);
            var bglow = Ui.Img(band, Theme.S("soft"), Theme.NatureOf(h?.nature).A(0.35f), "glow"); bglow.rectTransform.At(1, 0.5f, -40, 0, iw * 0.8f, 160);
            var bm = Ui.Img(band, Theme.S("ic_deck"), Color.white.A(0.08f), "mark"); bm.preserveAspect = true; bm.rectTransform.At(0, 0.5f, 6, 0, 96, 96);
            var bvl = Ui.Text(band, bandLabel, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopRight); bvl.rectTransform.Band(1, 26, 16, 18, -10);
            var bvt = Ui.Title(band, bestV != null ? $"{bestV.Value:N0}<size=50%><color={Theme.SubTag}> 점</color></size>" : "—", Theme.FsDisplay - 18, bestV != null ? Theme.Gold : Theme.Dim, TextAlignmentOptions.BottomRight);
            bvt.rectTransform.Band(0, Theme.C(66, 52), 16, 18, 4); bvt.Outline(0.12f);
            bvt.textWrappingMode = TextWrappingModes.NoWrap; bvt.overflowMode = TextOverflowModes.Overflow; bvt.enableAutoSizing = true; bvt.fontSizeMin = Theme.FsXl; bvt.fontSizeMax = Theme.C(Theme.FsDisplay - 18, Theme.Fs3xl - 8);
            if (s == null) { Ui.Text(info, "저장 칸 글을 읽지 못했다", Theme.FsMd, Theme.Bad, TextAlignmentOptions.Center).rectTransform.Fill(20, 20, 20, 220); }
            else
            {
                // 장비 3칸
                var gear = Ui.Rect("gear", info).Band(1, gearH, 16, 16, -gearY);
                Ui.Row(gear, 10, TextAnchor.MiddleLeft, null, true, true);
                foreach (var slot in R.SLOTS)
                {
                    s.Gear.TryGetValue(slot, out var gid);
                    var eq = gid != null ? P.Data.Equip(gid) : null;
                    var gc = gid != null ? Theme.GradeOf(eq?.Grade) : Theme.Dim;
                    var cell = Ui.Img(gear, Theme.Cell, Color.white.A(gid != null ? 1 : 0.4f), "eq " + slot, true).rectTransform;
                    Ui.Img(cell, Theme.Round, gc, "grade").rectTransform.Band(1, 4, 12, 12, -1);
                    var pic = gid != null ? CardArt.Equip(gid) : null;
                    var ic = Ui.Img(cell, pic ?? W.SlotIcon(slot), pic != null ? Color.white : gc.A(gid != null ? 1 : 0.5f), "icon"); ic.preserveAspect = true; ic.rectTransform.Fill(18, 34, 18, 12);
                    var en = Ui.Text(cell, eq?.Name ?? (gid ?? slot + " 없음"), Theme.FsCap, gid != null ? Theme.Ink : Theme.Dim, TextAlignmentOptions.Center);
                    en.rectTransform.Band(0, 28, 4, 4, 4); en.textWrappingMode = TextWrappingModes.NoWrap; en.enableAutoSizing = true; en.fontSizeMin = 9; en.fontSizeMax = Theme.FsCap;
                }
                // 요약 줄
                var sum = Ui.Rect("sum", info).Fill(16, 12, 16, gearY + gearH + 14);
                Ui.Col(sum, 2, TextAnchor.UpperLeft, null, true, false);
                void Line(string icon, string label, string value)
                {
                    var r = Ui.Rect(label, sum); r.Pref(-1, lineH);
                    var li = Ui.Img(r, Theme.S(icon), Theme.Sub, "ic"); li.preserveAspect = true; li.rectTransform.At(0, 0.5f, 4, 0, 24, 24);
                    var lt = Ui.Text(r, label, Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineLeft); lt.rectTransform.Fill(38, 0, 120, 0);
                    var vt = Ui.Title(r, value, Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineRight); vt.rectTransform.At(1, 0.5f, -4, 0, 220, lineH - 4);
                    var ln = Ui.Img(r, Theme.Round, Theme.Sub.A(0.18f), "ln"); ln.rectTransform.Band(0, 1, 0, 0, 0);
                }
                int o = s.Cards.Count(c => c.O > 0), bl = s.Cards.Count(c => c.B != null), cp = s.Cards.Count(c => GameData.IsCopy(c.Id));
                Line("ic_deck", "카드 수", $"{s.Cards.Count}장");
                Line("ic_spark", "신탁", $"{o}장");
                Line("ic_bless", "축복", $"{bl}장");
                Line("ic_copy", "복제", $"{cp}장");
                Line("ic_shield", "아티팩트", $"{s.Gear.Count} / {R.SLOTS.Length}");
                var gr = s.Growth;
                Line("ic_up", "모험 성장", gr != null && (gr.Atk != 0 || gr.Def != 0 || gr.Crit != 0) ? $"공{gr.Atk:+0;-0;0} 방{gr.Def:+0;-0;0} 치{gr.Crit:+0;-0;0}" : "없음");
                var vn = s.From?.Village != null ? (P.Data.Villages.TryGetValue(s.From.Village, out var vd) ? vd.Name : s.From.Village) : "-";
                var from = Ui.Text(sum, $"뜬 모험 — {vn}{(showSeed ? $" · 씨앗 {s.From?.Seed}" : "")} · {s.From?.Ended?.Replace("T", " ").TrimEnd('Z') ?? ""}", Theme.FsCap, Theme.Sub); from.Pref(-1, lineH - 4);
                from.textWrappingMode = TextWrappingModes.NoWrap; from.enableAutoSizing = true; from.fontSizeMin = 10; from.fontSizeMax = Theme.FsCap;
                if (why != null) { var wt = Ui.Text(sum, why, Theme.FsSm, Theme.Bad); wt.Pref(-1, 54); wt.enableAutoSizing = true; wt.fontSizeMin = 10; wt.fontSizeMax = Theme.FsSm; }

                // 오른쪽 — 카드 그리드
                var content = Ui.Scroll(right, out _);
                var grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                float cw = Theme.C(150, 140);
                grid.cellSize = new Vector2(cw, cw * 1.4f); grid.spacing = new Vector2(10, 12); grid.padding = new RectOffset(4, 4, 4, 12);
                foreach (var c in s.Cards)
                {
                    var view = GameData.WithBless(P.Data.View(c.Id, c.O), c.B != null && P.Data.Card(c.Id) != null ? Battle.BlessOf(P.Data.Card(c.Id), c.B) : null);
                    if (view == null) continue;
                    W.Card(content, this, c.Id, cw, "card " + c.Id, view, hero, false, CardMark.Of(P.Data, c.Id, c.O, c.B));
                }
            }
        }

        // ── 도전 ──
        void StartFrontier()
        {
            var refs = Enumerable.Range(0, fPick.Length).Select(PickOf).Where(x => x != null).ToList();
            if (refs.Count == 0) { Toast.Show("편성할 사도를 먼저 골라야 한다"); return; }
            if (refs.Select(x => x.Hero).Distinct().Count() != refs.Count) { Toast.Show("같은 사도 둘은 한 파티에 넣을 수 없다"); return; }
            if (refs.Any(x => !x.OnServer)) { Toast.Show("서버에 없는 칸이다 — 보관함을 다시 열 수 있다"); return; }
            if (FrontierFight == null) { Toast.Show("전투 화면이 없다"); return; }
            var body = new JObject { ["slots"] = new JArray(refs.Select(x => new JObject { ["id"] = x.Id })) }.ToString(Newtonsoft.Json.Formatting.None);
            var go = Stage.Hot.TryGetValue("frontier.go", out var gb) ? gb : null;
            if (go) go.Interactable = false;
            Online.Call("POST", "/api/frontier/start", body, r =>
            {
                if (go) go.Interactable = true;
                if (!r.Ok)
                {
                    string why = r.Code == 429 && r.Json?["max"] != null ? $"오늘 시도를 다 썼다(하루 {r.Json["max"]}번) — 내일(UTC) 다시 열린다"
                        : r.Code == 409 && r.Str("why") != null ? $"검증기가 거절한 칸이다 — {r.Str("why")}"
                        : r.Code == 404 ? "서버에 없는 칸이다 — 보관함을 다시 열 수 있다" : r.Code == 400 ? r.Error : Online.ErrorText(r);
                    if (r.Code == 403 && r.Json?["needsName"] != null) { NamePanel(null); return; }
                    Toast.Show("도전하지 못했다 — " + why, 3.5f);
                    return;
                }
                try
                {
                    var att = new Attempt { Id = r.Str("attempt"), Seed = (long)r.Json["seed"], Season = SeasonOf(r.Json["season"] as JObject), Slots = new List<HeroSlot>(), SlotJson = new List<string>(), BestKeys = refs.Select(BestKey).ToList() };
                    foreach (var so in (JArray)r.Json["slots"])
                    {
                        var txt = so["slot"].ToString(Newtonsoft.Json.Formatting.None);
                        att.SlotJson.Add(txt);
                        att.Slots.Add(HeroSlot.Parse(txt));
                    }
                    // 떠 둔 성장(성장 켬 서버만 준다) — 검증기가 같은 값으로 셋업을 다시 짓는다
                    if (r.Json["prog"] is JObject pj) att.Prog = GameData.FromJson<ProgState>(pj.ToString(Newtonsoft.Json.Formatting.None));
                    var whyNot = Frontier.WhyNot(P.Data, att.Slots, att.Season);
                    if (whyNot != null) { Toast.Show("이 기기에서 열 수 없는 편성입니다 — " + whyNot, 4f); Debug.LogWarning("[Frontier] 열 수 없음 — " + whyNot); return; }
                    fAttempt = att;
                    Debug.Log($"[Frontier] 시작 — 시도 {att.Id} · 씨앗 {att.Seed} · {att.Season.Id}/{att.Season.Boss}/{att.Season.Turns}턴 · [{string.Join(", ", att.Slots.Select(s => s.Hero))}]");
                    Hitch.Mark("프론티어 열기");
                    FrontierFight.OpenFrontier(P.Data, att.Slots, att.Season, att.Seed, FrontierDone, false, att.Prog);
                }
                catch (Exception e) { Debug.LogException(e); Toast.Show("도전 응답을 읽지 못했습니다 — " + e.Message, 3.5f); }
            });
        }

        /// <summary>전투가 끝났다(전투 화면이 돌려준다) — 결과 화면 · 테이프를 기기에 남기고 · 제출.</summary>
        public void FrontierDone(FrontierResult res)
        {
            LastFrontier = res;
            LastSubmit = null;
            var att = fAttempt;
            if (res.Tape != null && att?.BestKeys != null) NoteSlotBest(att.BestKeys, res.Score);
            if (res.Tape != null && att != null)
            {
                try
                {
                    var dir = Path.Combine(Application.persistentDataPath, "frontier-tapes" + FTail);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, att.Id + ".json"), res.Tape);
                    for (int i = 0; i < att.SlotJson.Count; i++) File.WriteAllText(Path.Combine(dir, $"{att.Id}_slot{i}.json"), att.SlotJson[i]);
                    Debug.Log($"[Frontier] 테이프 남김 {Path.Combine(dir, att.Id + ".json")} · {res.Tape.Length / 1024f:0.0}KB");
                }
                catch (Exception e) { Debug.LogWarning("[Frontier] 테이프를 남기지 못함 — " + e.Message); }
            }
            Debug.Log($"[Frontier] 끝 — {res.Over} · {res.Played}턴 · 점수 {res.Score}{(res.Error != null ? " · 오류 " + res.Error : "")}");
            Stage.SetBg("stage37_1", 0.8f);
            Stage.Show("frontier_result", root => BuildFrontierResult(root, res, att));
            if (res.Tape != null && att != null) SubmitFrontier(res, att);
        }

        Action<string> paintSubmit;
        void SubmitFrontier(FrontierResult res, Attempt att)
        {
            LastSubmit = "sending";
            paintSubmit?.Invoke("점수를 내는 중…");
            // tape 는 FrontierTape.Json() 글 그대로 — JSON 문자열 값으로 싣는다(서버가 글을 손대지 않고 둔다 · 프론티어_서버.md 「테이프를 글로 받는 까닭」)
            var body = new JObject { ["attempt"] = att.Id, ["score"] = res.Score, ["tape"] = res.Tape }.ToString(Newtonsoft.Json.Formatting.None);
            Online.Call("POST", "/api/frontier/submit", body, r =>
            {
                if (r.Ok)
                {
                    bool test = r.Json?["test"]?.Type == JTokenType.Boolean && (bool)r.Json["test"];   // 시험 계정의 도전(서버 migrations/0009) — 랭킹 밖
                    LastSubmit = test ? "test" : r.Str("status") == "skipped" ? "skipped" : "pending";   // skipped = 내 최고보다 높지 않아 검증하지 않음(서버 lib/score.js)
                    Debug.Log($"[Frontier] 제출 → {r.Code} {r.Str("status")}{(test ? " · 시험" : "")} · 점수 id {r.Str("id")} · 시도 {att.Id}");
                    if (test) { paintSubmit?.Invoke(TestLine); return; }
                    if (LastSubmit == "skipped") { paintSubmit?.Invoke(SkippedLine); return; }
                    paintSubmit?.Invoke(PendingLine);
                    return;
                }
                LastSubmit = "failed";
                Debug.LogWarning($"[Frontier] 제출 실패 {r.Code} — {r.Error}");
                string why = r.Code == 409 ? "이미 낸 시도다" : r.Code == 404 ? "시도가 지났거나(2시간) 없다" : r.Code == 400 ? "서버가 받지 않았다 — " + r.Error : Online.ErrorText(r);
                paintSubmit?.Invoke($"<color={Theme.BadTag}>내지 못했다</color> — {why}");
            });
        }

        static string TestLine => $"<color={Theme.GoldTag}>시험 기록으로 냈다</color> <color={Theme.SubTag}>(시험 계정 — 랭킹에 오르지 않음)</color>";
        static string SkippedLine => $"기록했다 <color={Theme.SubTag}>(검증 안 함 — 내 최고보다 낮음)</color>";
        static string PendingLine => $"<color={Theme.GoldTag}>랭킹에 올랐다</color> <color={Theme.SubTag}>(검증 전)</color>";

        void BuildFrontierResult(RectTransform root, FrontierResult res, Attempt att)
        {
            var glow = Ui.Img(root, Theme.S("soft"), Theme.Gold.A(0.3f), "glow"); glow.rectTransform.At(0.5f, 1, 0, 40, 1300, 520);
            Tw.Pulse(glow, 0.2f, 0.4f, 2.2f);
            string overKo = res.Over == "win" ? "보스 처치!" : res.Over == "lose" ? "파티가 쓰러졌다" : res.Over == Frontier.TIME ? "시간 종료" : "전투 중단";
            var t = Ui.Title(root, overKo, Theme.C(Theme.FsDisplay - 16, 60), Color.white, TextAlignmentOptions.Center);
            t.rectTransform.At(0.5f, 1, 0, -40, 1200, Theme.C(100, 80)); t.Outline(0.12f, new Color(0.15f, 0.05f, 0, 1));
            t.colorGradient = new VertexGradient(Color.white, Color.white, Theme.Gold, Theme.Gold);
            Tw.Pop(t.rectTransform, 0.1f, 1.4f, 0.5f);
            var panel = Ui.Panel(root, Theme.Panel, null, "result").rectTransform;
            panel.At(0.5f, 0.5f, 0, -10, 980, Theme.C(470, 400));
            Tw.Rise(panel, 0.2f, 30, 0.45f);
            var sl = Ui.Text(panel, "보스가 잃은 HP — 점수", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center); sl.rectTransform.Band(1, 30, 0, 0, -26);
            var sc = Ui.Title(panel, $"{res.Score:N0}", Theme.FsDisplay, Theme.Gold, TextAlignmentOptions.Center); sc.rectTransform.Band(1, 100, 0, 0, -58); sc.Outline(0.1f);
            Tw.Pop(sc.rectTransform, 0.35f, 1.5f, 0.5f);
            var fs = att?.Season ?? Frontier.PRESEASON;
            var boss = P.Data.Enemy(fs.Boss);
            int bossHp = boss != null ? (int)Math.Round(boss.Hp * fs.Hp) : 0;
            var meta = Ui.Text(panel, $"{boss?.Name ?? fs.Boss} · {res.Played} / {fs.Turns}턴 · 보스 HP 의 {(bossHp > 0 ? 100.0 * res.Score / bossHp : 0):0.0}%{(res.Error != null ? " · " + res.Error : "")}", Theme.FsBody, Theme.Ink, TextAlignmentOptions.Center);
            meta.rectTransform.Band(1, 30, 0, 0, -166);
            var faces = Ui.Rect("faces", panel).Band(1, 80, 0, 0, -206);
            Ui.Row(faces, 16, TextAnchor.MiddleCenter, null, false, false);
            if (att != null) foreach (var s in att.Slots) W.Face(faces, Roster.OfCore(s.Hero), 72);
            var sub = Ui.Text(panel, res.Tape == null ? $"<color={Theme.BadTag}>기록이 없어 낼 수 없다</color>" : "점수를 내는 중…", Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
            sub.rectTransform.Band(1, 60, 30, 30, -300);
            paintSubmit = s => { if (sub) sub.text = s; };
            if (LastSubmit == "skipped") sub.text = SkippedLine;
            if (LastSubmit == "pending") sub.text = PendingLine;
            if (LastSubmit == "test") sub.text = TestLine;
            var bar = Ui.Rect("bar", root).At(0.5f, 0, 0, Theme.C(34, 22), 900, Theme.BtnMainH);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleCenter, null, false, true);
            var again = Btn.Make(bar, "다시 내기", BtnStyle.PillDark, () => { if (LastSubmit == "failed" && att != null) SubmitFrontier(res, att); else Toast.Show("이미 냈다"); }, Theme.FsMd, "fres.retry");
            again.Pref(200, -1);
            var rk = Btn.Make(bar, "랭킹 보기", BtnStyle.PillDark, () => RankingScreen(FrontierScreen), Theme.FsMd, "fres.rank"); rk.Pref(220, -1);
            var back = Btn.Make(bar, "프론티어로", BtnStyle.PillGold, FrontierScreen, Theme.FsLg, "fres.back"); back.Pref(Theme.BtnMainW, -1);
            Stage.Hot["fres.retry"] = again; Stage.Hot["fres.rank"] = rk; Stage.Hot["fres.back"] = back;
            Stage.OnBack = FrontierScreen;
        }

        // ── 랭킹 ──
        /// <summary>랭킹판 — 낸 점수는 곧바로(검증 전 줄은 「검증 전」 표시) · 사람당 최고 하나 · 내 줄 강조 · 프리시즌 띠 · 내 검증 대기 · 거절 점수는 따로.</summary>
        public void RankingScreen(Action back)
        {
            Stage.SetBg("stage37_1", 0.78f);
            JObject rank = null, me = null;
            int left = Online.Ready ? 2 : 1;
            void One() { if (--left == 0) Stage.Show("ranking", root => BuildRanking(root, rank, me, back)); }
            Online.Call("GET", "/api/frontier/ranking?limit=100", null, r => { rank = r.Ok ? r.Json : new JObject { ["error"] = Online.ErrorText(r) }; One(); });
            if (Online.Ready) Online.Call("GET", "/api/frontier/me", null, r => { me = r.Ok ? r.Json : null; One(); });
        }

        /// <summary>랭킹판에 보인 줄 수 · 내 순위 강조(데모 단언).</summary>
        public int RankingRows { get; private set; }
        public bool RankingMine { get; private set; }
        /// <summary>랭킹판 줄의 닉네임(차례대로 — 데모가 남의 줄을 고른다).</summary>
        public readonly List<string> RankingNames = new List<string>();

        void BuildRanking(RectTransform root, JObject rank, JObject me, Action back)
        {
            var season = rank?["season"] as JObject ?? Online.Season;
            var boss = P.Data.Enemy(SeasonOf(season).Boss);
            ScreenHead(root, "랭킹", $"{boss?.Name ?? "보스"} · {SeasonOf(season).Turns}턴 피해 · 사람마다 최고", back, hd =>
            {
                if (!Preseason(season)) return;
                HeadChip(hd, $"{SeasonLabel(season)} · 시범 시즌", ChipKind.Gold, "ic_spark");
            });
            float y = -FTop;
            // 오른쪽 — 내 기록(검증 전 · 거절됨)
            float rw = Theme.C(420, 380);
            var side = FPanel(root, "mine");
            side.anchorMin = new Vector2(1, 0); side.anchorMax = new Vector2(1, 1); side.pivot = new Vector2(1, 0.5f);
            side.offsetMin = new Vector2(-Theme.Gutter - rw, Theme.Gutter); side.offsetMax = new Vector2(-Theme.Gutter, y);
            Tw.Rise(side, 0.14f, 24, 0.4f, Vector2.right);
            FLabel(side, "내 점수", 14);
            var mcol = Ui.Rect("col", side).Fill(FPad, 16, FPad, 54);
            Ui.Col(mcol, 6, TextAnchor.UpperLeft, null, true, false);
            if (me == null) Ui.Text(mcol, Online.Ready ? "기록을 받지 못했다" : "로그인하면 보인다", Theme.FsSm, Theme.Sub).Pref(-1, 50);
            else
            {
                var b = me["best"] as JObject;
                Ui.Text(mcol, b != null ? $"최고 <color={Theme.GoldTag}>{(int)b["score"]:N0}</color> · {(me["rank"]?.Type == JTokenType.Integer ? $"{(int)me["rank"]}위" : "-")}{UnverifiedTag(b)}" : "낸 점수 없음", Theme.FsMd, Theme.Ink).Pref(-1, 34);
                var list = (me["scores"] as JArray)?.OfType<JObject>().ToList() ?? new List<JObject>();
                var pend = list.Where(x => (string)x["status"] == "pending").ToList();
                var rej = list.Where(x => (string)x["status"] == "rejected").Take(3).ToList();
                W.Section(mcol, "검증 전", pend.Count > 0 ? $"{pend.Count}개" : null, 30);
                if (pend.Count == 0) Ui.Text(mcol, "없음", Theme.FsSm, Theme.Dim).Pref(-1, 26);
                foreach (var px in pend.Take(6)) Ui.Text(mcol, $"<color={Theme.SubTag}>검증 전</color>  {(int)px["claimed"]:N0} · {HeroNames(px["heroes"] as JArray)}", Theme.FsSm, Theme.Ink).Pref(-1, 26);
                if (rej.Count > 0)
                {
                    W.Section(mcol, "거절됨", null, 30);
                    foreach (var rx in rej) { var rt = Ui.Text(mcol, $"<color={Theme.BadTag}>거절됨</color>  {(int)rx["claimed"]:N0} — {rx["why"]}", Theme.FsCap, Theme.Sub); rt.Pref(-1, 40); }
                }
            }
            // 왼쪽 — 순위 줄
            var panel = FPanel(root, "table");
            panel.anchorMin = new Vector2(0, 0); panel.anchorMax = new Vector2(1, 1);
            panel.offsetMin = new Vector2(Theme.Gutter, Theme.Gutter); panel.offsetMax = new Vector2(-Theme.Gutter - FGap - rw, y);
            Tw.Rise(panel, 0.1f, 24, 0.4f);
            var hdr = Ui.Rect("hdr", panel).Band(1, 40, 24, 24, -12);
            RankRow(hdr, "순위", "닉네임", "점수", null, "낸 날", Theme.Sub, true);
            var area = Ui.Rect("area", panel).Fill(14, 14, 14, 60);
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 6, TextAnchor.UpperLeft, new RectOffset(6, 10, 2, 10), true, false);
            var rows = (rank?["rows"] as JArray)?.OfType<JObject>().ToList() ?? new List<JObject>();
            RankingRows = rows.Count; RankingMine = false; RankingNames.Clear(); RankingPending = rows.Count(Unverified);
            if (rank?["error"] != null) Ui.Text(content, "랭킹을 받지 못했다 — " + rank["error"], Theme.FsMd, Theme.Bad, TextAlignmentOptions.Center).Pref(-1, 100);
            else if (rows.Count == 0) EmptyNote(content, "낸 점수가 없다", null, 100);
            foreach (var r in rows)
            {
                bool mine = Online.Nick != null && (string)r["name"] == Online.Nick;
                if (mine) RankingMine = true;
                int rk = (int)r["rank"];
                var lineImg = Ui.Img(content, Theme.Round, mine ? Theme.Gold.A(0.28f) : rk % 2 == 0 ? Theme.NavyCell.A(0.5f) : new Color(0, 0, 0, 0), "row " + rk);
                var line = lineImg.rectTransform;
                line.Pref(-1, 64);
                RankingNames.Add((string)r["name"]);
                if (r["attempt"]?.Type == JTokenType.String) { string aid = (string)r["attempt"]; Stage.Hot["rank.row" + rk] = RowTap(lineImg, () => AttemptScreen(aid, () => RankingScreen(back))); }
                if (mine) { var edge = Ui.Img(line, Theme.Frame, Theme.Gold, "edge"); edge.rectTransform.Fill(-1, -1, -1, -1); }
                var at = r["at"]?.Type == JTokenType.Integer ? SlotStore.Date((long)r["at"]) : "";
                RankRow(line, rk <= 3 ? $"<color={Theme.GoldTag}>{rk}</color>" : rk.ToString(), (string)r["name"] + (mine ? $"  <size=70%><color={Theme.GoldTag}>나</color></size>" : "") + UnverifiedTag(r), $"{(int)r["score"]:N0}", r["heroes"] as JArray, at, mine ? Theme.Gold : Theme.Ink, false);
            }
            if (RankingPending > 0)
                Ui.Text(content, $"「검증 전」 = 검증기가 다시 돌려 보는 중 · 편성은 검증 뒤 공개", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center).Pref(-1, 40);
        }

        static string HeroNames(JArray a) => a == null ? "" : string.Join(" · ", a.Select(x => Roster.OfCore((string)x)?.ko ?? (string)x));

        void RankRow(RectTransform line, string rank, string name, string score, JArray heroes, string at, Color c, bool head)
        {
            float fs = head ? Theme.FsSm : Theme.FsLg;
            var r = Ui.Title(line, rank, head ? fs : Theme.FsXl, head ? Theme.Sub : Theme.Ink, TextAlignmentOptions.Center); r.rectTransform.At(0, 0.5f, 0, 0, 80, 50);
            var n = Ui.Title(line, name, fs, head ? Theme.Sub : Theme.Ink, TextAlignmentOptions.MidlineLeft); n.rectTransform.At(0, 0.5f, 92, 0, 300, 50);
            n.textWrappingMode = TextWrappingModes.NoWrap; n.overflowMode = TextOverflowModes.Ellipsis;
            var s = Ui.Title(line, score, head ? fs : Theme.FsXl, head ? Theme.Sub : c, TextAlignmentOptions.MidlineRight); s.rectTransform.At(0, 0.5f, 400, 0, 200, 50);
            var faces = Ui.Rect("faces", line).At(0, 0.5f, 630, 0, 200, 54);
            Ui.Row(faces, 6, TextAnchor.MiddleLeft, null, false, false);
            if (head) { var ft = Ui.Text(faces, "편성", fs, Theme.Sub); ft.Pref(120, 40); }
            else if (heroes != null) foreach (var h in heroes) W.Face(faces, Roster.OfCore((string)h), 50);
            var d = Ui.Text(line, at, head ? fs : Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineRight); d.rectTransform.At(1, 0.5f, -16, 0, 150, 40);
        }
    }
}
