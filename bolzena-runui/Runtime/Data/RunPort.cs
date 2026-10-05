using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Core;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 판 화면 ↔ 코어(com.bolzena.core, Docs/API.md 초안 v1)의 어댑터. 화면은 판을 바꿀 때 이 문만 지난다 —
    /// 코어 API 가 바뀌면 여기만 고친다. 읽기는 S(RunState) · Data(GameData) · Text(CardText) 를 그대로 본다.
    /// </summary>
    public sealed class RunPort
    {
        public readonly GameData Data;
        public readonly CardText Text;
        public Run Run { get; private set; }
        public RunState S => Run?.S;
        public readonly System.Random Rnd = new System.Random();

        RunPort(GameData d) { Data = d; Text = new CardText(d); }

        /// <summary>콘텐츠 데이터 — StreamingAssets/CoreData(에디터 RunUiSetup 이 코어 Data/Sample 을 떠 둔 스냅샷)를 코어가 읽는다. 사도 표를 코어에 잇는다.</summary>
        public static RunPort Boot()
        {
            var dir = CoreDataDir;
            GameData d;
            try { d = GameData.FromFolder(dir); }
            catch (Exception e) { Debug.LogError("[RunPort] 데이터를 읽지 못했습니다: " + e.Message); d = new GameData(); }
            var v = Validator.Check(d);
            Debug.Log($"[RunPort] 데이터 — 사도 {d.Heroes.Count} · 카드 {d.Cards.Count} · 적 {d.Enemies.Count} · 마을 {d.Villages.Count} · 이벤트 {d.Events.Count} · 장비 {d.Equips.Count} · 오류 {v.Errors.Count}");
            foreach (var e in v.Errors.Take(10)) Debug.LogWarning("[RunPort] 데이터 오류: " + e);
            Roster.Link(d);
            return new RunPort(d);
        }

        /// <summary>
        /// 콘텐츠 폴더 — PC 는 StreamingAssets/CoreData. 웹(WebGL)에서는 StreamingAssets 가 파일이 아니라 주소라 폴더로 못 읽는다 —
        /// 웹 빌드가 Resources/CoreDataPack(파일 경로 · 글 한 벌)으로 묶어 넣은 것을 처음 한 번 메모리 파일 시스템(임시 폴더)에 풀고 그 폴더를 쓴다.
        /// 전투 화면(CoreBattle · Look)도 이 폴더를 읽는다.
        /// </summary>
        public static string CoreDataDir
        {
            get
            {
                if (Application.platform != RuntimePlatform.WebGLPlayer) return Path.Combine(Application.streamingAssetsPath, "CoreData");
                var dir = Path.Combine(Application.temporaryCachePath, "CoreData");
                if (unpacked) return dir;
                unpacked = true;
                var ta = Resources.Load<TextAsset>("CoreDataPack");
                if (ta == null) { Debug.LogError("[RunPort] 웹 콘텐츠 묶음(Resources/CoreDataPack)이 없습니다"); return dir; }
                var pack = JsonUtility.FromJson<CorePack>(ta.text);
                for (int i = 0; i < pack.paths.Length; i++)
                {
                    var f = Path.Combine(dir, pack.paths[i]);
                    Directory.CreateDirectory(Path.GetDirectoryName(f));
                    File.WriteAllText(f, pack.texts[i]);
                }
                Resources.UnloadAsset(ta);
                Debug.Log($"[RunPort] 웹 콘텐츠 파일 {pack.paths.Length}개를 풀었습니다 → {dir}");
                return dir;
            }
        }
        static bool unpacked;
        [Serializable] class CorePack { public string[] paths; public string[] texts; }

        /// <summary>판을 열 수 있는 데이터인가(마을 · 사도가 있다).</summary>
        public bool Ready => Data.Villages.Count > 0 && Data.Heroes.Count >= 3;

        // ── 새 판 · 마을 ──
        public string RollVillage() => Run.RollVillage(Data, Rnd.NextDouble());
        public VillageDef Village(string id) => id != null && Data.Villages.TryGetValue(id, out var v) ? v : Data.Villages.Values.FirstOrDefault();
        public List<VillageDef> Villages => Data.Villages.Values.ToList();

        public void NewRun(List<string> party, string village, long seed)
        {
            var rows = party.ToDictionary(k => k, k => Data.Hero(k)?.Row ?? "mid");
            Run = Run.New(Data, party, seed, village, rows);
        }

        public bool Has => Run != null;
        public VillageDef VillageDef => Run.VillageDef;
        public FloorDef Floor => Run.CurrentFloor;
        public bool IsBoss => Run.IsBoss;
        public bool IsLastFloor => Run.IsLastFloor;

        // ── 지도 ──
        public MapState Map() => Run.MapOf();
        public List<string> Reachable() => Run.Reachable();
        public HashSet<string> Ahead() => Run.AheadOf();
        public MapNode Enter(string id) => Run.EnterNode(id);
        public List<string> FoesAt(MapNode n) => Run.EnemiesAt(n);
        public string StageName(MapNode n) => Run.StageName(n);
        public static string KindKo(string t) => t != null && Run.KIND_KO.TryGetValue(t, out var k) ? k : t;
        public MapNode Here => Run.CurrentNode;
        /// <summary>바로 다음이 마지막 보스인가(2-9 휴식+상점).</summary>
        public bool LastBossNext => Run.IsLastFloor && Run.Reachable().Any(id => Run.NodeById(Run.MapOf(), id)?.Type == "boss");

        // ── 싸움(지금은 트랙 C 전투 화면 자리 — 봇이 대신 싸운다) ──
        public sealed class FightOutcome
        {
            public bool Won;
            public RewardState Loot;
            public Dictionary<string, Glow> Glows = new Dictionary<string, Glow>();
            public int Turns, HpBefore, HpAfter;
            public List<string> Foes = new List<string>();
            public bool Event;
        }

        /// <summary>
        /// 싸움 한 판 — mode "bot"(봇이 그대로) · "win"(적 체력을 낮춰 이기게) · "lose"(적 피해를 크게 — 지게).
        /// 전투 화면(트랙 C)이 붙으면 OpenFight 로 연 Battle 을 그 화면에 넘기고 끝난 뒤 Finish 를 부르면 된다.
        /// </summary>
        public FightOutcome AutoFight(string mode)
        {
            bool ev = S.EventFight != null;
            var foes = Run.CurrentEnemies().ToList();
            int before = S.PartyHp;
            double hpx = mode == "win" ? 0.03 : mode == "lose" ? 8 : 1, dmgx = mode == "lose" ? 80 : 1;   // 「짐」 은 적을 단단하게도 — 첫 턴에 이겨 버리지 않게
            var (b, loot) = Run.OpenFight(hpx, dmgx);
            var bots = new Bots(Data);
            int guard = 0;
            while (b.Over == null && guard++ < 40)
            {
                bots.SmartPlay(b, 1, 2);
                if (b.Over == null) b.EndTurn();
            }
            return Finish(b, loot, ev, foes, before);
        }

        /// <summary>전투 화면에 넘길 싸움 한 판 — OpenFight 로 열고, 끝나면 Finish(t).</summary>
        public sealed class FightTicket
        {
            public Battle B;
            public RewardState Loot;
            public bool Event;
            public List<string> Foes;
            public int Before;
            /// <summary>이 싸움의 배경(층 배경 표의 fight · boss).</summary>
            public string Bg;
        }

        /// <summary>싸움을 연다 — 쪽지(cues)는 전투 화면이 그린다. 열고 난 뒤 Battle 은 화면이 몬다.</summary>
        public FightTicket OpenFight(double hpx = 1, double dmgx = 1, List<Cue> cues = null)
        {
            bool ev = S.EventFight != null;
            var foes = Run.CurrentEnemies().ToList();
            int before = S.PartyHp;
            var f = Floor;
            string bgKey = IsBoss && !ev ? "boss" : "fight";
            var (b, loot) = Run.OpenFight(hpx, dmgx, cues);
            return new FightTicket { B = b, Loot = loot, Event = ev, Foes = foes, Before = before, Bg = f.Bg != null && f.Bg.TryGetValue(bgKey, out var bg) ? bg : null };
        }

        public FightOutcome Finish(FightTicket t) => Finish(t.B, t.Loot, t.Event, t.Foes, t.Before);

        public FightOutcome Finish(Battle b, RewardState loot, bool ev, List<string> foes, int before)
        {
            bool won = b.Over == "win";
            Run.AfterFight(b);
            if (!won && b.Over != "lose") S.PartyHp = 0;     // 끝나지 않은 싸움(봇이 멈춤)은 진 것으로
            var o = new FightOutcome { Won = won, Loot = won ? loot : null, Turns = b.Turn, HpBefore = before, HpAfter = S.PartyHp, Foes = foes, Event = ev };
            if (won && b.Glow != null) foreach (var kv in b.Glow) if (kv.Value != null && kv.Value.Count > 0) o.Glows[kv.Key] = kv.Value;
            if (ev) Run.AfterEventFight(won);
            return o;
        }

        public bool Wiped => Run.PartyWiped;
        public void ClearElite() { S.Elite = false; }

        // ── 보상 ──
        public void TakeGold() => Run.TakeGold();
        public string TakeEquip(string id) => Run.TakeEquip(id);
        public string ClaimGlow(string cardId, Glow g, int i) => Run.ClaimGlow(cardId, g, i);
        public List<string> BossCopyOffer() => Run.BossCopyOffer();
        public string BossCopy(string id) => Run.BossCopy(id);
        public void Advance() => Run.Advance();
        public bool Cleared => S.Done == "clear";

        // ── 캠프 ──
        public CampState EnterCamp(string kind) => Run.EnterCamp(kind);
        public int CampHeal => Run.CampHealOf();
        public string CampRest() => Run.CampRest();
        public string CampTrain(int n) => Run.CampTrain(n);
        /// <summary>이번 캠프에서 이미 골랐나 — 쉬기 · 수련 가운데 하나만(웹판과 같은 규칙).</summary>
        public string CampChoice => S.Camp != null && S.Stops.TryGetValue(S.Camp.Key, out var v) ? v : "";

        // ── 상점 ──
        public ShopState Shop(bool fresh)
        {
            var at = S.Map?.At;
            if (fresh || S.Shop == null || S.Shop.Floor != S.Floor || S.Shop.At != at) Run.RollShop();
            return S.Shop;
        }
        public string Buy(int i) => Run.Buy(i);
        public int RerollPrice => Run.RerollPrice;
        public string Reroll() => Run.RerollShop();
        public int RemovePrice => Run.RemovePrice;
        public string Remove(string id) => Run.RemoveCard(id);

        // ── 장비 ──
        public string Equip(string hero, string id, bool replace) => Run.Equip(hero, id, replace);
        public string Sell(string id) => Run.SellEquip(id);
        public int SellPrice(string id) => Run.SellPrice(id);
        public bool Bought(string id) => Run.IsBought(id);
        public Dictionary<string, string> GearOf(string hero) => Run.GearOf(hero);
        public Stats StatsOf(string id, string hero) => Run.StatsOf(id, hero);
        public static readonly string[] Slots = { "무기", "방어구", "장신구" };

        // ── 이벤트 ──
        public bool EventLeft => Run.EventLeft();
        public EventState EnterEvent() => Run.EnterEvent();
        public string PickEvent(string id) => Run.PickEvent(id);
        public List<EventOption> Options(EventDef ev) => Run.OptionsOf(ev);
        public List<Outcome> OutOf(EventOption o) => Run.OutOf(o);
        public string LockOf(EventOption o) => Run.LockOf(o);
        public JudgeResult JudgeOf(EventOption o) => Run.JudgeOf(o);
        public (bool fight, string why) Choose(int i) => Run.Choose(i);
        public string Resolve(object v) => Run.ResolvePending(v);
        public List<string> ShinAble(string kind) => Run.ShinAble(kind);
        public bool DupeOk(string id) => Run.DupeOk(id);
        public void LeaveEvent() => Run.LeaveEvent();

        // ── 카드 ──
        public CardView View(string id) => Run != null ? Run.ViewOf(id) : Data.View(id);
        public string CardLine(string id) { var v = View(id); return v != null ? Text.Card(v) : id; }

        // ── 저장 · 이어하기 ──
        [Serializable] class SaveFile { public string run; public string where; public string kind; }
        // -save 이름 — 여럿을 나란히 돌릴 때(자동 데모 PC · 폰) 저장이 겹치지 않게
        static string SavePath
        {
            get
            {
                var a = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(a, "-save");
                return Path.Combine(Application.persistentDataPath, i >= 0 && i < a.Length - 1 ? a[i + 1] : "bolzena-run.json");
            }
        }

        public void Save(string where, string kind = null)
        {
            if (Run == null) return;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(new SaveFile { run = Run.Save(), where = where, kind = kind })); }
            catch (Exception e) { Debug.LogWarning("[RunPort] 저장 실패: " + e.Message); }
        }

        public static bool HasSave => File.Exists(SavePath);
        public static void ClearSave() { try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { } }

        /// <summary>이어하기 — 적어 둔 판을 읽는다. 돌려줌: (어디, 종류) 또는 null.</summary>
        public (string where, string kind)? Load()
        {
            try
            {
                if (!HasSave) return null;
                var f = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));
                Run = Run.Load(Data, f.run);
                return (f.where ?? "map", f.kind);
            }
            catch (Exception e) { Debug.LogWarning("[RunPort] 이어하기 실패 — 버립니다: " + e.Message); ClearSave(); return null; }
        }

        /// <summary>로비 「이어하기」 의 한 줄 — 판을 바꾸지 않고 읽어 본다.</summary>
        public string SaveSummary()
        {
            try
            {
                var f = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));
                var r = Run.Load(Data, f.run);
                var names = string.Join(" · ", r.S.Party.Select(k => Roster.OfCore(k).ko));
                return $"{r.VillageDef.Name} {r.S.Floor + 1}층 {r.CurrentFloor.Name} · {names}";
            }
            catch { return null; }
        }
    }
}
