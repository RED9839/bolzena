using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Core;
using UnityEngine;
using CSide = Bolzena.Core.Side;
using CoreFight = Bolzena.Core.Battle;
using CoreFx = Bolzena.Core.Fx;

namespace Bolzena.Battle
{
    // IBattle → Bolzena.Core 어댑터. 코어의 전투(Battle)를 몰고, 코어가 남긴 연출 쪽지(Cue)를 화면의 이벤트(BattleEvent)로 옮긴다.
    //   - 웨이브: 코어 전투는 적 한 무리라, 웨이브마다 새 전투를 연다(파티 HP · 게이지 · 남은 신탁을 넘긴다)
    //   - 쪽지에 없는 것(AP · 게이지 · 적의 예고 바뀜)은 한 수가 끝난 뒤 앞뒤를 견줘 이벤트로 붙인다
    //   - 방어 막대는 쪽지의 Guard 로 뒤따라 센다(방어 + 실드를 한 숫자로)
    public class CoreBattle : IBattle
    {
        // 한 판의 차림 — 파티 · 덱 · 웨이브. 시범(자동 데모)은 Pilot 을 쓴다
        public class Fixture
        {
            public List<string> Party = new List<string>();
            public List<string> Deck = new List<string>();
            public List<List<string>> Waves = new List<List<string>>();
            public List<double> WaveHp = new List<double>();          // 적 체력 배율(웨이브마다)
            public List<int> WaveGauge = new List<int>();             // 웨이브를 열 때 게이지를 적어도 이만큼(시범 — 고학년을 보이려고)
            public List<string> Glow = new List<string>();            // 이 전투에서 빛나는 카드(카드 신탁)
            public long Seed = 7;
            public string EnemyNature;                                 // 판의 적 속성(모든 적의 성격을 이것으로) — null 이면 데이터 그대로
            public string Bg;                                          // 싸움터 배경(마을 층의 fight 배경)
            public string Note;                                        // 무엇으로 차렸는지(로그)
            public static System.Func<GameData, Fixture> Override;     // 점검 모드(-ultaudit)가 사도마다 차림을 넣는다

            // 명령줄로 차림 — -party 사도,사도,사도 -foes 적,적 (전투만 따로 띄워 볼 때 · 입력 재현). 없으면 null
            public static Fixture FromArgs(GameData d)
            {
                var a = Environment.GetCommandLineArgs();
                string Arg(string n) { int i = Array.IndexOf(a, n); return i >= 0 && i < a.Length - 1 ? a[i + 1] : null; }
                var foes = Arg("-foes");
                if (foes == null) return null;
                var party = Arg("-party");
                var f = new Fixture { Party = party != null ? party.Split(',').ToList() : Pilot(d).Party };
                f.Deck = d.BuildDeck(f.Party);
                foreach (var h in f.Party)
                    f.Deck.AddRange(d.Cards.Values.Where(c => c.Hero == h && c.Unique).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal));
                // -deck 카드,카드: 덱을 이 카드들 + 다른 카드 3장으로 줄인다(점검 — 갈래가 다른 카드를 첫 손에 확실히 쥐게)
                var only = Arg("-deck");
                if (only != null)
                {
                    var keep = only.Split(',').ToList();
                    f.Deck = f.Deck.Where(keep.Contains).Distinct().Concat(f.Deck.Where(id => !keep.Contains(id)).Distinct().Take(3)).ToList();
                }
                f.Waves.Add(foes.Split(',').ToList());
                f.EnemyNature = Arg("-nature");
                // -wavehp 배율: 적 체력 배율(시험 캡처 — 격파까지 버티게)
                if (double.TryParse(Arg("-wavehp"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var whp)) f.WaveHp.Add(whp);
                // -glow: 고유 카드를 모두 빛나게(신탁 창 시험)
                if (Array.IndexOf(a, "-glow") >= 0) f.Glow.AddRange(f.Deck.Where(id => (d.Card(id)?.Unique ?? false) && d.Card(id).Oracles.Count > 0).Distinct());
                return f;
            }

            /// <summary>
            /// -battle 시험 차림(v2 콘텐츠) — 무작위 마을 하나 · 그 마을에서 고를 수 있는 판 속성 하나 · 그 마을 1층 일반 한 줄(1웨이브) + 엘리트 한 줄(2웨이브),
            /// 파티는 그 속성의 약점 성격 사도 하나 + 다른 둘. -seed 숫자로 같은 차림을 다시 연다. 콘텐츠에 마을이 없으면 옛 시범(Pilot).
            /// </summary>
            public static Fixture V2(GameData d)
            {
                if (d.Villages.Count == 0 || d.Heroes.Count < 3) return null;
                var a = Environment.GetCommandLineArgs();
                int si = Array.IndexOf(a, "-seed");
                long seed = si >= 0 && si < a.Length - 1 && long.TryParse(a[si + 1], out var sv) ? sv : DateTime.Now.Ticks & 0x7fffffff;
                var rng = new System.Random((int)(seed & 0x7fffffff));
                var vills = d.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
                var vid = vills[rng.Next(vills.Count)];
                var v = d.Villages[vid];
                var fl = v.Floors[0];
                string nat = Bolzena.Core.Run.RollNature(d, vid, rng.NextDouble());
                var weak = R.WeakTo(nat);
                var heroes = d.Heroes.Values.OrderBy(h => h.Id, StringComparer.Ordinal).ToList();
                var weakOnes = heroes.Where(h => weak.Contains(h.Nature)).ToList();
                var first = weakOnes.Count > 0 ? weakOnes[rng.Next(weakOnes.Count)] : heroes[rng.Next(heroes.Count)];
                var rest = heroes.Where(h => h.Id != first.Id && h.Name != first.Name).OrderBy(_ => rng.Next()).Take(2).ToList();
                var f = new Fixture { Party = new List<string> { rest[0].Id, first.Id, rest[1].Id }, Seed = seed, EnemyNature = nat };
                f.Deck = d.BuildDeck(f.Party);
                foreach (var h in f.Party)
                {
                    var u = d.Cards.Values.Where(c => c.Hero == h && c.Unique && c.Oracles.Count > 0).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                    if (u != null) f.Deck.Add(u);
                }
                var glow = f.Deck.FirstOrDefault(id => (d.Card(id)?.Unique ?? false) && d.Card(id).Oracles.Count > 0);
                if (glow != null) f.Glow.Add(glow);
                var neutral = d.NeutralIds().Take(2).ToList();
                for (int i = 0; i < neutral.Count; i++) f.Deck.Add(GameData.WithOwner(neutral[i], f.Party[1 + i]));
                var pools = fl.Pools.SelectMany(p => p).Where(l => l.Count > 0).ToList();
                if (pools.Count > 0) f.Waves.Add(pools[rng.Next(pools.Count)].ToList());
                if (fl.Elites.Count > 0) f.Waves.Add(fl.Elites[rng.Next(fl.Elites.Count)].ToList());
                if (f.Waves.Count == 0) return null;
                f.WaveHp.Add(0.85); f.WaveHp.Add(0.8);
                f.WaveGauge.Add(200); f.WaveGauge.Add(R.GAUGE_MAX);
                f.Bg = fl.Bg != null && fl.Bg.TryGetValue("fight", out var bg) ? bg : null;
                f.Note = $"마을 {v.Name} · 판 속성 {nat}(약점 {string.Join("·", weak)}) · 파티 {string.Join(", ", f.Party)} · 적 {string.Join(" / ", f.Waves.Select(w => string.Join(",", w)))} · 씨앗 {seed}";
                return f;
            }

            public static Fixture Pilot(GameData d)
            {
                var v2 = d.Hero("rico") == null ? V2(d) : null;
                if (v2 != null) return v2;
                // 코어 샘플(rico · carrot · sion) 또는 콘텐츠 폴더(리코타 · 캬롯 · 시온더다크불릿 — 웹판 키)
                bool sample = d.Hero("rico") != null;
                var f = new Fixture { Party = sample ? new List<string> { "rico", "carrot", "sion" } : new List<string> { "리코타", "캬롯", "시온더다크불릿" } };
                f.Deck = d.BuildDeck(f.Party);
                // 시범 — 고유 카드 둘을 덱에 넣고 하나를 빛나게(신탁 창을 보이려고)
                var uniq = sample ? new List<string> { "sion_u1", "rico_u1" }
                                  : f.Party.Take(2).Reverse().Select(h => d.Cards.Values.Where(c => c.Hero == h && c.Unique && c.Oracles.Count > 0).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault()).Where(x => x != null).ToList();
                foreach (var u in uniq) if (d.Card(u) != null) f.Deck.Add(u);
                if (uniq.Count > 0 && d.Card(uniq[0]) != null) f.Glow.Add(uniq[0]);
                // 시범 — 교주 카드 둘을 주인 사도와 함께(「카드@사도」 — 틀 빛깔 · 핀이 그 사도)
                var neutral = d.NeutralIds().Take(2).ToList();
                for (int i = 0; i < neutral.Count; i++) f.Deck.Add(GameData.WithOwner(neutral[i], f.Party[1 + i]));
                f.Waves.Add(sample ? new List<string> { "fairy_close", "fairy_long" } : new List<string> { "fairymobcloserange", "fairymoblongrange" });
                f.Waves.Add(new List<string> { sample ? "curburus" : "clone_carrot" });
                f.WaveHp.Add(0.85); f.WaveHp.Add(0.55);
                f.WaveGauge.Add(200); f.WaveGauge.Add(R.GAUGE_MAX);   // 시범 — 첫 웨이브에 전체 공격 고학년(리코타 200) 하나, 보스 웨이브는 가득
                foreach (var id in f.Deck)
                    if (f.Glow.Count == 0 && (d.Card(id)?.Oracles.Count ?? 0) >= 3 && d.Card(id).Type == "공격") f.Glow.Add(id);
                return f;
            }
        }

        public static GameData LoadData()
        {
            return GameData.FromFolder(Bolzena.RunUI.RunPort.CoreDataDir);   // 웹은 메모리 FS 에 푼 폴더
        }

        readonly GameData data;
        readonly CardText text;
        readonly Fixture fx;
        CoreFight b;
        readonly List<Cue> cues = new List<Cue>();
        int wave, turnBase;
        int lastTarget;
        bool over, won;
        BattleSnapshot snap;

        // 쪽지에 없는 것을 견주는 자리
        readonly Dictionary<string, int> guard = new Dictionary<string, int>();
        int shownAp = -1, shownGauge = -1;
        readonly Dictionary<int, string> shownIntent = new Dictionary<int, string>();
        // 힘 모으기(엘리트 · 일반) — 적 자리마다 지난번 본 모습: 쏟을 수 이름 · 쏟는 턴인가 · 쏟을 수 꼴(적 차례 쪽지를 옮길 때 쏟는 턴 · 끊김을 가린다)
        readonly Dictionary<int, (string name, bool now, string t)> chargeSeen = new Dictionary<int, (string, bool, string)>();

        // ── 화면 자료 담아 두기(2026-10-08 성능 — 행동마다 Snapshot 을 다시 지을 때 같은 글을 되풀이해 지었다) ──
        // 전투(b)가 같으면 CardView 마다 글 · 낱말 풀이 · 주인은 그대로다. 바뀌는 것(비용 · 빛 · 표식 · 생성 카드의 그때 모습)만 덧입힌다.
        // 사도 · 적의 글(고학년 · 특성 · 장비 · 고유 효과 · 적 패시브)도 전투 동안 그대로라 한 번만 짓는다. 웨이브(새 b)가 열리면 비운다.
        sealed class InfoBase { public CardInfo Info; public List<(string name, CardView gv)> Gen; }
        sealed class HeroBase { public string UltText, KeywordText; public List<TraitLine> Traits = new List<TraitLine>(); public List<string> Passives = new List<string>(); public List<GearSlot> Gear = new List<GearSlot>(); }
        CoreFight cacheOf;
        readonly Dictionary<CardView, InfoBase> infoBase0 = new Dictionary<CardView, InfoBase>(), infoBase1 = new Dictionary<CardView, InfoBase>();
        readonly Dictionary<KwRt, string> kwTexts = new Dictionary<KwRt, string>();
        readonly Dictionary<string, HeroBase> heroBases = new Dictionary<string, HeroBase>();
        readonly Dictionary<string, List<string>> foePassTexts = new Dictionary<string, List<string>>();
        readonly Dictionary<(string, int), string> oracleTexts = new Dictionary<(string, int), string>();
        void CacheFor()
        {
            if (cacheOf == b) return;
            cacheOf = b;
            infoBase0.Clear(); infoBase1.Clear(); kwTexts.Clear(); heroBases.Clear(); foePassTexts.Clear();
        }

        public CoreBattle(GameData data, Fixture fixture)
        {
            this.data = data;
            text = new CardText(data);
            fx = fixture;
            Look.Data = data;
        }

        // ── 판에서 연 싸움(Run.OpenFight) — 웨이브 하나. 쪽지는 CueSink 로 받는다 ──
        CoreFight preset;
        public static CoreBattle ForRun(GameData data) => new CoreBattle(data, new Fixture());
        public List<Cue> CueSink => cues;
        public void Attach(CoreFight opened)
        {
            preset = opened;
            fx.Party = opened.Party.Select(u => u.Key).ToList();
            fx.Waves.Clear();
            fx.Waves.Add(opened.Enemies.Select(e => e.Key).ToList());
        }

        public GameData Data => data;

        // 이 판에 나올 적의 스파인 폴더(미리 불러 두기)
        public IEnumerable<string> SpineKeys()
        {
            foreach (var w in fx.Waves) foreach (var id in w) yield return Look.Enemy(id, data.Enemy(id)?.Boss ?? false).spine;
        }
        public CoreFight Fight => b;
        public Fixture Fx => fx;

        // ── 열기 ──────────────────────────────────────────────────────
        CoreFight StartWave(int w, int partyHp, int gauge, Dictionary<string, Glow> glow)
        {
            var st = new BattleSetup
            {
                Party = fx.Party.ToList(), Deck = fx.Deck.ToList(), Enemies = fx.Waves[w].ToList(),
                Seed = fx.Seed + w * 101, EnemyHp = w < fx.WaveHp.Count ? fx.WaveHp[w] : (double?)null,
                Gauge = Math.Max(gauge, w < fx.WaveGauge.Count ? fx.WaveGauge[w] : 0), Glow = glow, EnemyNature = fx.EnemyNature,
            };
            if (partyHp >= 0) st.PartyHp = partyHp;
            cues.Clear();
            var nb = CoreFight.Start(data, st, cues);
            shownIntent.Clear();
            chargeSeen.Clear();
            guard.Clear();
            return nb;
        }

        static Dictionary<string, Glow> GlowFor(GameData d, IEnumerable<string> ids)
        {
            var g = new Dictionary<string, Glow>();
            foreach (var id in ids)
            {
                var c = d.Card(id);
                if (c == null || c.Oracles.Count == 0) continue;
                var gl = new Glow { Kind = "card" };
                for (int n = 1; n <= Math.Min(3, c.Oracles.Count); n++) gl.Picks.Add(new GlowPick { N = n });
                // -forcebless(점검 · 축복 연출 캡처) — 첫 선택지에 진짜 축복을 얹는다(고르면 규칙에도 들어간다). 기본은 엔진 확률(R.ORACLE_BLESS) 그대로
                // -blesstest 도 같은 길(둘째 선택지) — 화면에만 보이고 규칙에는 안 붙던 예전 시험 경로를 없앴다(2026-10-08: 쓰고 버린 뒤 축복이 없던 제보)
                bool fb = Array.IndexOf(Environment.GetCommandLineArgs(), "-forcebless") >= 0, tb = Array.IndexOf(Environment.GetCommandLineArgs(), "-blesstest") >= 0;
                if ((fb || tb) && gl.Picks.Count > 0)
                {
                    int bi = fb ? 0 : Math.Min(1, gl.Picks.Count - 1);
                    var ks = c.Blesses.Count > 0 ? new List<string> { "own" } : Bolzena.Core.Run.DivineKindsFor(d.View(id, gl.Picks[bi].N));
                    gl.Picks[bi].Shin = ks.Count > 0 ? ks[0] : "draw";
                }
                g[id] = gl;
            }
            return g;
        }

        /// <summary>
        /// -glowbase(시험 캡처) — 같은 기본 카드 세 장 가운데 한 장에만 은총 빛(2026-10-07 제보: 같은 카드가 모두 빛났다).
        /// 덱을 그 기본 카드 셋 + 다른 카드 둘로 줄여 첫 손에 다 들게 한다. 빛을 한 장에 거는 것은 엔진(Battle.LightOne)이 한다.
        /// </summary>
        static void GlowBase(GameData d, Fixture f, Dictionary<string, Glow> glow)
        {
            var dup = f.Deck.GroupBy(x => x).Where(g => g.Count() >= 2 && d.Card(g.Key) is CardDef c && c.Hero != null && !c.Unique && d.UniquesOf(c.Hero).Count > 0)
                .Select(g => g.Key).FirstOrDefault();
            if (dup == null) return;
            var others = f.Deck.Where(x => x != dup).Distinct().Take(2).ToList();
            f.Deck = new List<string> { dup, dup, dup }.Concat(others).ToList();
            glow.Clear();
            var hero = d.Card(dup).Hero;
            glow[dup] = new Glow { Kind = "hero", Hero = hero, Options = new List<string> { d.UniquesOf(hero).First() } };
            Debug.Log($"[Demo] -glowbase — 「{d.Card(dup).Name}」 세 장 가운데 한 장에 은총 빛");
        }

        public IReadOnlyList<BattleEvent> Begin()
        {
            var evs = new List<BattleEvent>();
            wave = 0;
            if (preset != null) { b = preset; preset = null; shownIntent.Clear(); chargeSeen.Clear(); guard.Clear(); }   // 쪽지는 이미 cues 에
            else
            {
                var glow = GlowFor(data, fx.Glow);
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-glowbase") >= 0) GlowBase(data, fx, glow);
                b = StartWave(0, -1, 0, glow);
            }
            evs.Add(new BattleEvent { Kind = EventKind.WaveStart, Value = 1, Boss = b.Enemies.Any(e => e.Boss) });
            Translate(evs);
            After(evs);
            return evs;
        }

        // ── 하기 ──────────────────────────────────────────────────────
        public bool CanPlay(int i, out string reason)
        {
            reason = null;
            if (over || b == null) { reason = "전투가 끝났습니다"; return false; }
            if (i < 0 || i >= b.Hand.Count) { reason = "그런 카드가 없습니다"; return false; }
            reason = b.CanPlay(b.Hand[i], false, i);
            return reason == null;
        }

        public IReadOnlyList<CardInfo> EpiphanyOptions(int i)
        {
            var list = new List<CardInfo>();
            if (i < 0 || i >= b.Hand.Count) return list;
            var id = b.Hand[i];
            var g = b.GlowOf(id);
            if (g == null) return list;
            var def = data.Card(id);
            if (g.Kind == "card")
            {
                // 신탁 다섯 중 무작위 셋(엔진이 골라 둔 Picks) — 축복이 얹힌 것은 이름 · 글을 같이(OracleOption)
                List<OracleOption> opts = null;
                try { opts = b.EpiphanyOptions(id); } catch (Exception) { }
                for (int k = 0; k < g.Picks.Count; k++)
                {
                    var p = g.Picks[k];
                    if (p.N < 1 || p.N > def.Oracles.Count) continue;
                    var o = def.Oracles[p.N - 1];
                    var info = Info(data.View(id, p.N), null);
                    if (!oracleTexts.TryGetValue((id, p.N), out var otx))   // 신탁 글(낱말 맞추기)은 카드 · 갈래마다 그대로
                        oracleTexts[(id, p.N)] = otx = Fmt(Bolzena.RunUI.OracleDiff.Mark(NoTagLine(text.Card(def), def.Tags), NoTagLine(text.Oracle(def, o), info.Tags)));
                    info.Text = otx;
                    info.CostDown = o.Cost.HasValue && o.Cost.Value < def.Cost;
                    info.EpiphanyLabel = "신탁";   // 이름 대신 갈래 번호(2026-10-08 사용자)
                    info.Epiphany = false;
                    var oo = opts != null ? opts.Find(x => x.N == p.N && x.Shin == p.Shin) : null;
                    if (oo != null && oo.Blessed) { info.BlessName = oo.BlessName; info.BlessText = Fmt(oo.BlessText); info.MarkBless = oo.BlessName; info.MarkBlessText = oo.BlessText; }
                    list.Add(info);
                }
            }
            else foreach (var opt in g.Options) list.Add(Info(b.CardOf(opt), null));
            return list;
        }

        /// <summary>낀 장비(사도 id → 칸 → 장비 id) — 판에서 연 싸움이면 BattleBridge 가 단다. 전투 시범은 null(빈 칸).</summary>
        public static Func<string, Dictionary<string, string>> GearOf;

        public IReadOnlyList<BattleEvent> PlayCard(int i, int target, int choice = -1, int branch = 0, int spend = 0)
        {
            var evs = new List<BattleEvent>();
            if (!CanPlay(i, out _)) return evs;
            cues.Clear();
            var id = b.Hand[i];
            if (b.GlowOf(id) != null) b.ApplyEpiphany(id, Math.Max(0, choice));
            lastTarget = b.CardOf(id)?.Target == "아군" ? -1 : target;   // 아군 카드의 target 은 사도 번호(코어 oneAlly) — 연출의 적 번호로 쓰지 않는다
            b.PlayCard(i, target, branch > 0 || spend > 0 ? new PlayOpts { Choice = branch > 0 ? branch : (int?)null, Spend = spend > 0 ? spend : (int?)null } : null);
            Translate(evs);
            After(evs);
            return evs;
        }

        // ── 소모량 고르기(spend pick) · 다음 카드 강화 표시 ──
        /// <summary>소모량을 고르는 카드면 후보와 결과 미리보기(효과 순서: spend pick → perEvent → 피해 · 실드 · 방어 · 회복). 아니면 null.</summary>
        public SpendPrompt SpendPromptOf(int i)
        {
            if (over || b == null || i < 0 || i >= b.Hand.Count) return null;
            var id = b.Hand[i];
            var cv = b.CardOf(id);
            var opts = b.SpendChoices(id);
            if (cv == null || opts.Count == 0) return null;
            int si = cv.Fx.FindIndex(f => f.K == FxK.Spend && f.Pick);
            if (si < 0) return null;
            var sf = cv.Fx[si];
            int have = b.StackOf(cv.Hero, sf.Id);
            double mul = !cv.IsStatus && !cv.IsCurse ? 1 + b.EmpowerOf(cv.Hero) / 100.0 : 1;
            int pe = cv.Fx.FindIndex(si + 1, f => f.K == FxK.PerEvent);
            CoreFx eff = pe >= 0 && pe + 1 < cv.Fx.Count ? cv.Fx[pe + 1] : null;
            if (eff != null && eff.K != FxK.Dmg && eff.K != FxK.Shield && eff.K != FxK.Block && eff.K != FxK.Heal) eff = null;
            int per = pe >= 0 ? Math.Max(1, cv.Fx[pe].Per) : 1;
            string what = eff == null ? null : eff.K == FxK.Dmg ? (eff.Fixed || eff.OfEvent > 0 ? "고정 피해" : "피해") : eff.K == FxK.Shield ? (eff.Fixed || eff.OfEvent > 0 ? "고정 실드" : "실드") : eff.K == FxK.Block ? "방어" : "회복";
            string Val(int n, double m)
            {
                if (eff.OfEvent > 0) return Math.Max(1, (int)Math.Round(n * eff.OfEvent * m, MidpointRounding.AwayFromZero)).ToString();
                return (int)Math.Round(eff.Ratio * eff.HitsOr1 * (n / per) * 100 * m, MidpointRounding.AwayFromZero) + "%";
            }
            var sp = new SpendPrompt { Kw = sf.Id, Have = have };
            if (eff != null)
                sp.PerUnit = eff.OfEvent > 0 ? $"1개당 {what} {Val(per, 1)}" : $"{(per > 1 ? per + "개" : "1개")}당 {what} {(int)Math.Round(eff.Ratio * eff.HitsOr1 * 100)}%";
            foreach (var n in opts)
            {
                var o = new SpendOption { N = n, Name = n >= have ? "전부" : n == 1 ? "1개" : "절반" };
                if (eff != null)
                {
                    o.Result = what + " " + Val(n, 1);
                    if (mul > 1.0001) o.Boosted = what + " " + Val(n, mul);
                }
                sp.Options.Add(o);
            }
            return sp;
        }

        static readonly System.Text.RegularExpressions.Regex empRx = new System.Text.RegularExpressions.Regex(@"(피해|방어|실드|회복\(방어력) ((?:<[^>]*>|[\u0001\u0002])*)(\d+)%");
        /// <summary>카드 글의 피해 · 방어 · 실드 · 회복 % 를 강화 배율로 키우고 Tone.EmpOn … EmpOff 로 싼다(Tone.CardText 가 강화색으로).</summary>
        static string EmpowerText(string t, double mul)
        {
            if (string.IsNullOrEmpty(t)) return t;
            return empRx.Replace(t, m => m.Groups[1].Value + " " + m.Groups[2].Value + Bolzena.UI.Tone.EmpOn
                + (int)Math.Round(int.Parse(m.Groups[3].Value) * mul, MidpointRounding.AwayFromZero) + "%" + Bolzena.UI.Tone.EmpOff);
        }

        static string MulText(double mul) => (Math.Round(mul * 100) / 100.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>파티 HUD 칩 — 다음 카드 강화가 걸려 있으면 한 칸(사도별 · 파티 몫을 글에 적는다).</summary>
        void AddEmpowerChip(List<StatusChip> chips)
        {
            if (b == null) return;
            int party = b.EmpowerOf(null), best = party;
            var lines = new List<string>();
            if (party > 0) lines.Add($"파티의 다음 카드 ×{MulText(1 + party / 100.0)}");
            foreach (var u in b.Party)
            {
                if (u.Dead) continue;
                int own = b.EmpowerOf(u.Key) - party;
                if (own <= 0) continue;
                best = Math.Max(best, own + party);
                lines.Add($"{u.Name}의 다음 카드 ×{MulText(1 + (own + party) / 100.0)}");
            }
            if (best <= 0) return;
            chips.Add(new StatusChip
            {
                Id = "다음 카드 강화", Value = "×" + MulText(1 + best / 100.0), Kind = "empower",
                Text = string.Join("\n", lines) + "\n내는 순간 한 번 쓰이고 사라집니다 — 피해 · 실드 · 회복이 커집니다.",
            });
        }

        /// <summary>점검(-ultaudit-prep) — 조건부 고학년 cue 를 보려고 판을 미리 세운다: 보스가 아닌 적 HP 25% · 충격 2 ·
        /// 그 사도의 적 몫 고유 효과(꿀범벅 …) 4겹. 판에는 이 점검 전투에서만 쓴다.</summary>
        public void AuditPrep(string heroId, double hpFrac = 0.25)
        {
            var kw = data.Hero(heroId)?.Keyword;
            foreach (var e in b.Enemies)
            {
                if (e.Dead) continue;
                if (!e.Boss) e.Hp = Math.Max(1, (int)(e.MaxHp * hpFrac));
                e.Status["충격"] = 2;
                if (kw != null && kw.Carrier == "enemy" && !string.IsNullOrEmpty(kw.Name)) e.Status[kw.Name] = 4;
            }
        }

        /// <summary>점검(-breakkillshots) — 적 하나의 HP · 강인도를 그 값으로(다음 한 대에 격파하며 처치 · 격파만 되는 장면을 찍는다).</summary>
        public void AuditSetFoe(int idx, int hp, double tough)
        {
            if (idx < 0 || idx >= b.Enemies.Count) return;
            var e = b.Enemies[idx];
            if (hp > 0) { if (hp > e.MaxHp) e.MaxHp = hp; e.Hp = hp; }   // 넘으면 최대 HP 도 올린다(격파만 되고 안 쓰러지게)
            if (tough >= 0 && e.ToughMax > 0) e.Tough = Math.Min(e.ToughMax, tough);
            snap = null;
        }

        /// <summary>점검(-ultaudit-break) — 힘 모으는(spend 면 모은 힘을 쏟을) 적의 강인도를 거의 0 으로: 다음 한 대에 격파되어 「끊김」을 본다. 그런 적이 없으면 false.</summary>
        public bool AuditNearBreak(bool spend)
        {
            bool any = false;
            foreach (var e in b.Enemies)
            {
                if (e.Dead || e.Broken || e.ToughMax <= 0 || e.Intent == null) continue;
                if (spend ? !e.IntentFromCharge : e.Intent.T != "charge") continue;
                e.Tough = Math.Min(e.Tough, 0.01);
                any = true;
            }
            snap = null;
            return any;
        }

        public bool CanUlt(int hero, out string reason)
        {
            reason = null;
            if (over || b == null || hero < 0 || hero >= b.Party.Count) { reason = "쓸 수 없습니다"; return false; }
            reason = b.CanUlt(b.Party[hero].Key);
            return reason == null;
        }

        public bool UltNeedsTarget(int hero)
        {
            var u = hero >= 0 && hero < b.Party.Count ? b.UltOf(b.Party[hero].Key) : null;
            return u != null && data.TargetOf(u.Fx) == "적";
        }

        public IReadOnlyList<BattleEvent> UseUlt(int hero, int target)
        {
            var evs = new List<BattleEvent>();
            if (!CanUlt(hero, out _)) return evs;
            cues.Clear();
            lastTarget = target;
            b.UseUlt(b.Party[hero].Key, target);
            Translate(evs);
            After(evs);
            return evs;
        }

        public IReadOnlyList<BattleEvent> EndTurn()
        {
            var evs = new List<BattleEvent>();
            if (over) return evs;
            cues.Clear();
            b.EndTurn();
            Translate(evs);
            After(evs);
            return evs;
        }

        // ── 미리보기 ──────────────────────────────────────────────────
        static List<PreviewFoe> Map(List<Bolzena.Core.PreviewFoe> l) =>
            l?.Select(x => x == null ? null : new PreviewFoe { Hp = x.Hp, Guard = x.Guard, Kill = x.Kill, Max = x.Max, Break = x.Brk, ToughV = (float)x.Tough }).ToList();

        /// <summary>그 사도(파티 몇 번째)가 그 적을 치면 약점 공격인가 — 성격 · 공명(늘) · 적 표식(core WeakFor).</summary>
        public bool WeakFor(int hero, int enemy) => hero >= 0 && hero < b.Party.Count && enemy >= 0 && enemy < b.Enemies.Count && b.WeakFor(b.Party[hero], b.Enemies[enemy]);

        public IReadOnlyList<PreviewFoe> PreviewCard(int i, int target) => over ? null : Map(b.PreviewCard(i, target));

        public PreviewParty PreviewPartyOf(int i)
        {
            if (over) return null;
            var p = b.PreviewPartyOf(i, 0);
            return p == null ? null : new PreviewParty { Heal = p.Heal, Block = p.Block + p.Shield, Lose = p.Lose, Over = p.Over };
        }

        public IReadOnlyList<PreviewFoe> PreviewUlt(int hero, int target) =>
            over || hero < 0 || hero >= b.Party.Count ? null : Map(b.PreviewUlt(b.Party[hero].Key, target));

        // ── 쪽지 → 이벤트 ─────────────────────────────────────────────
        int Guard(CSide side, int idx)
        {
            string k = side == CSide.Party ? "P" : "E" + idx;
            if (guard.TryGetValue(k, out var v)) return v;
            return 0;
        }

        void SetGuard(CSide side, int idx, int v) => guard[side == CSide.Party ? "P" : "E" + idx] = Math.Max(0, v);

        // 적 방어 · 실드를 엔진 값(Block + Shield)으로 다시 맞춘다 — 쪽지 없이 바뀐 실드가 화면에 남지 않게(턴 · 적 차례마다)
        void ResyncGuards(List<BattleEvent> evs)
        {
            for (int i = 0; i < b.Enemies.Count; i++)
            {
                var e = b.Enemies[i];
                int g = e.Dead ? 0 : e.Block + e.Shield;
                if (g == Guard(CSide.Enemy, i)) continue;
                SetGuard(CSide.Enemy, i, g);
                evs.Add(new BattleEvent { Kind = EventKind.Block, Target = UnitRef.Enemy(i), Value = 0, BlockAfter = g });
            }
        }

        int RepHero(int idx)
        {
            if (idx >= 0 && idx < b.Party.Count) return idx;
            return 0;
        }

        void Translate(List<BattleEvent> evs)
        {
            snap = null;
            UnitRef actor = UnitRef.Party(0);
            bool heavy = false;
            var hitN = new Dictionary<string, int>();
            var ultCut = new HashSet<int>();
            int actStart = -1;
            foreach (var c in cues.ToList())
            {
                switch (c.K)
                {
                    case "card":
                        CardMove(c, evs);
                        break;
                    case "act":
                    {
                        FinishAct(evs, actStart);
                        hitN.Clear();
                        actStart = evs.Count;
                        if (c.Side == CSide.Party)
                        {
                            actor = UnitRef.Party(RepHero(c.Idx));
                            bool ult = c.Anim == "ult";
                            CardInfo card = c.CardId != null ? Info(b.CardOf(c.CardId), null) : null;
                            var m = ult ? Motion.Ultimate : card != null ? card.Motion : Motion.Skill1;
                            heavy = false;
                            evs.Add(new BattleEvent
                            {
                                Kind = EventKind.Act, Actor = actor, Target = UnitRef.Enemy(Math.Max(0, lastTarget)), Motion = m,
                                HitKind = Look.Hero(b.Party[actor.Index].Key).Hit, Text = ult ? "ult" : null, Card = card, Say = ult ? c.Name : null,
                            });
                        }
                        else
                        {
                            actor = UnitRef.Enemy(c.Idx);
                            bool hit = c.Anim == "attack";
                            if (c.T == "ult")
                            {
                                // 보스 클론 고학년 — 아군 고학년 표시(Text "ult")와 따로: "foeult". 그 사도의 고학년 몸짓(적 쪽이라 좌우가 뒤집혀 선다)
                                heavy = true;
                                evs.Add(new BattleEvent { Kind = EventKind.Act, Actor = actor, Motion = Motion.Ultimate, Text = "foeult", Say = c.Name ?? c.Say, Anim = c.Hero, Up = c.Rush });
                                break;
                            }
                            // 모은 힘을 쏟는 턴(엘리트 · 일반의 charge → Next) — Anim "charge": 그 수 이름 띠 · 조금 센 타격감
                            bool spend = !c.Rush && chargeSeen.TryGetValue(c.Idx, out var cs) && cs.now && cs.t == c.T;
                            heavy = hit && (c.T == "back" || c.T == "attackAll" || c.Rush || spend);
                            evs.Add(new BattleEvent
                            {
                                Kind = EventKind.Act, Actor = actor, Motion = hit && !heavy ? Motion.Attack1 : Motion.Skill1,
                                Text = hit ? heavy ? "heavy" : "attack" : "defend", Say = c.Say, Up = c.Rush, Anim = spend ? "charge" : null,
                            });
                        }
                        break;
                    }
                    case "hurt":
                    {
                        string hk = c.Side + ":" + c.Idx;
                        int n = hitN.TryGetValue(hk, out var x) ? x : 0;
                        hitN[hk] = n + 1;
                        int g = Math.Max(0, Guard(c.Side, c.Idx) - c.Guard);
                        SetGuard(c.Side, c.Idx, g);
                        if (c.Side == CSide.Enemy)
                            evs.Add(new BattleEvent
                            {
                                Kind = EventKind.Damage, Actor = actor, Target = UnitRef.Enemy(c.Idx), Value = c.V, Blocked = c.Guard, Crit = c.Crit,
                                HpAfter = (int)c.To, BlockAfter = g, Hit = n, Hits = n + 1,
                                HitKind = actor.Side == Side.Party && actor.Index < b.Party.Count ? Look.Hero(b.Party[actor.Index].Key).Hit : HitKind.Slash,
                            });
                        else
                            evs.Add(new BattleEvent
                            {
                                Kind = EventKind.PartyHurt, Actor = actor, Target = UnitRef.Party(RepHero(c.Idx)), Value = c.V, Blocked = c.Guard,
                                HpAfter = (int)c.To, BlockAfter = g, Hit = n, Hits = n + 1, Crit = heavy || c.Crit,
                            });
                        break;
                    }
                    case "heal":
                        evs.Add(new BattleEvent { Kind = EventKind.Heal, Target = Ref(c), Value = c.V, HpAfter = (int)c.To });
                        break;
                    case "block":
                    case "shield":
                    {
                        int g = Guard(c.Side, c.Idx) + c.V;
                        SetGuard(c.Side, c.Idx, g);
                        evs.Add(new BattleEvent { Kind = EventKind.Block, Actor = actor, Target = Ref(c), Value = c.V, BlockAfter = g, Text = c.K });
                        break;
                    }
                    case "status":
                        // 힘 모으기가 격파로 끊김(core 는 status 「끊김!」만 낸다) — 보스 고학년(foeUltCut 이 먼저 옴)이 아니면 「끊김!」 연출로
                        if (c.Id == "끊김!" && c.Side == CSide.Enemy && !ultCut.Contains(c.Idx) && chargeSeen.TryGetValue(c.Idx, out var cc))
                        {
                            evs.Add(new BattleEvent { Kind = EventKind.FoeCharge, Actor = UnitRef.Enemy(c.Idx), Text = "cut", Say = cc.name, Up = cc.now });
                            chargeSeen.Remove(c.Idx);
                            break;
                        }
                        evs.Add(new BattleEvent { Kind = EventKind.Status, Actor = actor, Target = Ref(c), Text = c.Id, Up = c.Up });
                        break;
                    case "tough":
                        if (c.Up)
                        {
                            if (c.From <= 0.001) evs.Add(new BattleEvent { Kind = EventKind.Recover, Target = UnitRef.Enemy(c.Idx), FAfter = (float)c.To });
                            else evs.Add(new BattleEvent { Kind = EventKind.Toughness, Target = UnitRef.Enemy(c.Idx), FAfter = (float)c.To, Up = true });
                        }
                        else evs.Add(new BattleEvent { Kind = EventKind.Toughness, Target = UnitRef.Enemy(c.Idx), FAfter = (float)c.To });
                        break;
                    case "break":
                        evs.Add(new BattleEvent { Kind = EventKind.Break, Target = UnitRef.Enemy(c.Idx), Value = c.V });
                        break;
                    case "die":
                        if (c.Side == CSide.Enemy)
                            evs.Add(new BattleEvent { Kind = EventKind.Death, Target = UnitRef.Enemy(c.Idx), Boss = c.Idx < b.Enemies.Count && b.Enemies[c.Idx].Boss });
                        break;
                    case "revive":
                        if (c.Side == CSide.Enemy) evs.Add(new BattleEvent { Kind = EventKind.Revive, Target = UnitRef.Enemy(c.Idx), HpAfter = c.V, Say = c.Name });
                        break;
                    case "auto":
                        evs.Add(new BattleEvent { Kind = EventKind.Status, Target = Ref(c), Text = c.Label ?? c.Tag, Up = true });
                        break;
                    case "epiphany":
                        evs.Add(new BattleEvent { Kind = EventKind.EpiphanyApplied, Card = Info(b.CardOf(c.CardId), null), Text = c.CardId, Actor = Ref(c) });
                        break;
                    case "turn":
                        FinishAct(evs, actStart); actStart = -1;
                        evs.Add(new BattleEvent { Kind = EventKind.TurnStart, Value = turnBase + c.V });
                        SetGuard(CSide.Party, -1, b.Pool.Block + b.Pool.Shield);
                        evs.Add(new BattleEvent { Kind = EventKind.Block, Target = UnitRef.Party(0), Value = 0, BlockAfter = b.Pool.Block + b.Pool.Shield });
                        evs.Add(new BattleEvent { Kind = EventKind.ApChanged, Value = b.Ap });
                        shownAp = b.Ap;
                        ResyncGuards(evs);
                        break;
                    case "fx":
                    {
                        int hi = b.Party.FindIndex(p => p.Key == c.Hero);
                        evs.Add(new BattleEvent { Kind = EventKind.FxCue, Actor = UnitRef.Party(Math.Max(0, hi)), Text = c.Id, Value = (int)Math.Round((double)c.V) });
                        break;
                    }
                    case "foeUltWarn":
                    case "foeUlt":
                    case "foeUltHit":
                    case "foeUltEnd":
                    case "foeUltCut":
                    {
                        string sub = c.K == "foeUltWarn" ? "warn" : c.K == "foeUlt" ? "start" : c.K == "foeUltHit" ? "hit" : c.K == "foeUltEnd" ? "end" : "cut";
                        evs.Add(new BattleEvent { Kind = EventKind.FoeUlt, Actor = UnitRef.Enemy(c.Idx), Text = sub, Say = c.Name, Value = c.V, Anim = sub == "cut" ? c.Label : c.Hero, Up = c.T == "ult" });
                        if (sub == "cut") ultCut.Add(c.Idx);
                        break;
                    }
                    case "foeChargeWarn":
                        // 엘리트 · 일반 적 힘 모으기 — Name 쏟을 수 · Say 모으기 이름 · V 다음 턴 피해(b.ChargeHit) · T 쏟을 수 꼴
                        evs.Add(new BattleEvent { Kind = EventKind.FoeCharge, Actor = UnitRef.Enemy(c.Idx), Text = "warn", Say = c.Name, Anim = c.Say, Value = c.V });
                        break;
                    case "formOn":
                    case "formOff":
                    {
                        int hi = b.Party.FindIndex(p => p.Key == c.Hero);
                        evs.Add(new BattleEvent { Kind = EventKind.Form, Actor = UnitRef.Party(Math.Max(0, hi)), Text = c.K == "formOn" ? "on" : "off", Say = c.Name, Anim = c.K == "formOn" ? c.T : c.Label, Value = c.V });
                        break;
                    }
                    case "talk":
                        evs.Add(new BattleEvent { Kind = EventKind.Talk, Actor = UnitRef.Party(RepHero(c.Idx)), Text = c.Moment });
                        break;
                    case "unguard":
                    {
                        // 방어 · 실드가 사라짐(적 차례 시작 · 「실드 전부 파괴」) — V 잃은 양, To 남은 방어+실드
                        int left = (int)Math.Round(c.To);
                        SetGuard(c.Side, c.Idx, left);
                        evs.Add(new BattleEvent { Kind = EventKind.Block, Target = Ref(c), Value = -Math.Max(1, c.V), BlockAfter = left, Text = "unguard" });
                        break;
                    }
                    case "foeTurn":
                        ResyncGuards(evs);
                        break;
                }
            }
            FinishAct(evs, actStart);
            cues.Clear();
        }

        // 한 몸짓의 타격 수 — 대상마다 센 것 가운데 큰 것으로(연출이 타격 시각을 나눈다)
        static void FinishAct(List<BattleEvent> evs, int start)
        {
            if (start < 0) return;
            int most = 1;
            for (int i = start; i < evs.Count; i++)
                if (evs[i].Kind == EventKind.Damage || evs[i].Kind == EventKind.PartyHurt) most = Math.Max(most, evs[i].Hit + 1);
            for (int i = start; i < evs.Count; i++)
                if (evs[i].Kind == EventKind.Damage || evs[i].Kind == EventKind.PartyHurt) evs[i].Hits = most;
        }

        static UnitRef Ref(Cue c) => c.Side == CSide.Enemy ? UnitRef.Enemy(c.Idx) : UnitRef.Party(Math.Max(0, c.Idx));

        void CardMove(Cue c, List<BattleEvent> evs)
        {
            if (c.CardId == null) return;   // 섞기
            string from = c.Pile, to = c.ToPile;
            if (to == "hand" && from != "hand")
                evs.Add(new BattleEvent { Kind = EventKind.Draw, Card = Info(b.CardOf(c.CardId), null), Text = c.Label });
            else if (from == "new" && to == "discard" && c.Label == "grace")   // 은총 — 손이 가득해 버림 더미로(화면은 크게 보인 뒤 버림 더미로 날린다)
                evs.Add(new BattleEvent { Kind = EventKind.Draw, Card = Info(b.CardOf(c.CardId), null), Text = "grace_discard" });
            else if (from == "hand" && to == "play")
            {
                var ci = Info(b.CardOf(c.CardId), null);
                evs.Add(new BattleEvent { Kind = EventKind.CardPlayed, Card = ci, Actor = UnitRef.Party(Math.Max(0, ci.Hero >= 0 ? ci.Hero : ci.Owner)) /* 교주 카드는 넣은 사도가 낸다 */ });
            }
            else if (from == "hand" && to == "discard")
                evs.Add(new BattleEvent { Kind = EventKind.Discard, Card = new CardInfo { Id = c.CardId }, Text = c.Label });
            else if (from == "hand" && to == "gone")
                evs.Add(new BattleEvent { Kind = EventKind.Exhaust, Card = new CardInfo { Id = c.CardId }, Text = c.Label });
            else if (from == "hand" && to != "hand")
                evs.Add(new BattleEvent { Kind = EventKind.Exhaust, Card = new CardInfo { Id = c.CardId }, Text = c.Label ?? "remove" });
            else if (from == "hand" && to == "hand" && c.Label != null)
                evs.Add(new BattleEvent { Kind = EventKind.CardChanged, Card = Info(b.CardOf(c.CardId), null), Text = c.Label });
        }

        // 한 수가 끝난 뒤 — AP · 게이지 · 예고 바뀜, 웨이브 넘기기 · 끝
        void After(List<BattleEvent> evs)
        {
            if (b.Ap != shownAp) { evs.Add(new BattleEvent { Kind = EventKind.ApChanged, Value = b.Ap }); shownAp = b.Ap; }
            if (b.Gauge != shownGauge)
            {
                for (int h = 0; h < b.Party.Count; h++)
                {
                    evs.Add(new BattleEvent { Kind = EventKind.UltGauge, Actor = UnitRef.Party(h), Value = b.Gauge });
                    var u = b.UltOf(b.Party[h].Key);
                    if (u != null && b.Gauge >= u.Cost && shownGauge < u.Cost && b.Over == null) evs.Add(new BattleEvent { Kind = EventKind.UltReady, Actor = UnitRef.Party(h) });
                }
                shownGauge = b.Gauge;
            }
            for (int i = 0; i < b.Enemies.Count; i++)
            {
                var e = b.Enemies[i];
                string sig = e.Dead || e.Intent == null ? "" : $"{e.Intent.T}|{e.Intent.V}|{e.Intent.N}|{b.IntentHit(e)}|{b.ChargeHit(e)}|{e.IntentFromCharge}|{e.Broken}|{e.RushCnt}";
                var ch = ChargeOf(e);
                if (ch.name != null) chargeSeen[i] = (ch.name, ch.now, e.Intent.T); else chargeSeen.Remove(i);
                if (shownIntent.TryGetValue(i, out var was) && was == sig) continue;
                shownIntent[i] = sig;
                if (!e.Dead) evs.Add(new BattleEvent { Kind = EventKind.Intent, Target = UnitRef.Enemy(i) });
            }
            snap = null;
            if (b.Over == "lose") { over = true; evs.Add(new BattleEvent { Kind = EventKind.Defeat }); return; }
            if (b.Over != "win") return;
            if (wave + 1 < fx.Waves.Count)
            {
                // 다음 웨이브 — 손에 남은 카드는 버리고 새 전투(같은 덱 · 파티 HP · 게이지 · 남은 신탁)
                foreach (var id in b.Hand) evs.Add(new BattleEvent { Kind = EventKind.Discard, Card = new CardInfo { Id = id }, Text = "wave" });
                int hp = b.Pool.Hp, gauge = b.Gauge;
                var glow = b.Glow.ToDictionary(kv => kv.Key, kv => kv.Value.Copy());
                turnBase += b.Turn;
                wave++;
                b = StartWave(wave, hp, gauge, glow);
                shownAp = -1; shownGauge = -1;
                evs.Add(new BattleEvent { Kind = EventKind.WaveStart, Value = wave + 1, Boss = b.Enemies.Any(e => e.Boss) });
                Translate(evs);
                After(evs);
                return;
            }
            over = won = true;
            evs.Add(new BattleEvent { Kind = EventKind.Victory });
        }

        // ── 카드 · 상태 그리기 자료 ─────────────────────────────────────
        /// <summary>사도 고유 효과 글 — 부제(desc) + CardText.Trait(그 키워드를 쓰는 패시브까지 · 「계기 → 결과」 줄). 칩 · 카드 확대 · 정보 창이 같은 글.</summary>
        string KwText(KwRt kw)
        {
            CacheFor();
            if (kwTexts.TryGetValue(kw, out var done)) return done;
            var h = data.Hero(kw.Owner);
            var body = h != null ? text.Trait(h, kw.Def) : text.Keyword(kw.Def);
            var sub = h != null ? kw.Def.Desc?.TrimEnd('.') : null;
            return kwTexts[kw] = Fmt(string.IsNullOrEmpty(sub) ? body : $"<color=#A7B1CC>{sub}</color>\n{body}");
        }

        /// <summary>
        /// 카드 글의 태그 줄(「소멸. 신속.」) — 카드 면 태그 칸(Tone.TagLine)에 이미 보이니 효과 글에서는 뺀다(2026-10-07 카드 글 줄 나눔).
        /// 태그 칸에 안 나오는 「사용 불가」 만 남긴다. 판 화면 W.Card 도 같은 일을 한다.
        /// </summary>
        static string NoTagLine(string s, IEnumerable<string> tags)
        {
            if (string.IsNullOrEmpty(s) || tags == null) return s;
            var tl = tags.Where(t => !string.IsNullOrEmpty(t)).ToList();
            if (tl.Count == 0) return s;
            var lines = s.Split('\n').ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                if (parts.Count == 0 || !parts.All(x => tl.Contains(x))) continue;
                if (parts.Contains("사용 불가")) lines[i] = "사용 불가.";
                else { lines.RemoveAt(i); }
                break;
            }
            return string.Join("\n", lines);
        }

        /// <summary>점검용(데모) — 지금 손의 신탁 가능한 카드 하나에 빛(신탁 고르기)을 얹는다. 손 화면은 다음 동기 때 바뀐다.</summary>
        public bool DebugGlow()
        {
            foreach (var id in b.Hand.ToList())
            {
                if (b.GlowOf(id) != null) continue;
                var g = GlowFor(data, new[] { id });
                if (g.Count == 0) continue;
                b.Glow[id] = g[id];
                return true;
            }
            return false;
        }

        static string Fmt(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // 「X」 낱말은 금빛, 조건 머리(「파괴:」 …)는 줄을 바꾼다
            var sb = new System.Text.StringBuilder(s.Length + 32);
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '「') sb.Append("<color=#ffd77a>「");
                else if (ch == '」') sb.Append("」</color>");
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        static CardType TypeOf(string t) => t == "공격" ? CardType.Attack : t == "스킬" ? CardType.Skill : t == "강화" ? CardType.Power : CardType.Status;

        /// <summary>카드 id 하나의 화면 정보(점검 — 갈래 창 · 툴팁 시험).</summary>
        public CardInfo InfoOf(string id) => Info(b.CardOf(id), null);

        CardInfo Info(CardView cv, int? handIdx, int depth = 0)
        {
            if (cv == null) return new CardInfo { Id = "?", Name = "?" };
            CacheFor();
            var map = depth == 0 ? infoBase0 : infoBase1;
            if (!map.TryGetValue(cv, out var ib)) map[cv] = ib = InfoBaseOf(cv, depth);
            var info = ib.Info.Clone();
            // 바뀌는 것 — 비용(손 자리 할인) · 빛(신탁 · 은총) · 생성 카드(그 카드의 지금 빛 · 표식) · 받은 신탁 · 표식(축복 · 복제). 차례는 예전 Info 그대로
            info.Cost = handIdx != null ? b.CostOf(cv.Id, handIdx) : cv.Cost;
            if (handIdx != null && depth == 0 && !cv.IsStatus && !cv.IsCurse)
            {   // 다음 카드 강화(core EmpowerOf: 사도 몫 + 파티 몫) — 걸려 있으면 배율 · 강화된 수치 글
                int emp = b.EmpowerOf(cv.Hero);
                if (emp > 0) { info.EmpowerMul = (float)(1 + emp / 100.0); info.Text = EmpowerText(info.Text, 1 + emp / 100.0); }
            }
            var glow = b.GlowOf(cv.Id);
            info.Epiphany = glow != null; info.Grace = glow?.Kind == "hero";
            if (ib.Gen != null)
                foreach (var g in ib.Gen) info.Terms.Add(new Term(g.name, "이 카드가 만드는 카드", "card") { Card = Info(g.gv, null, depth + 1) });
            // 받은 신탁 — 이름은 보이지 않는다(2026-10-07 사용자 「이름 말고 별 표시」). 별 줄만으로는 무엇인지 모르니 짧은 상자 하나
            if (cv.Oracle != null) info.Terms.Insert(0, new Term("신탁 받은 카드", "이번 모험에서 신탁으로 바뀐 카드입니다 — 비용 아래 금빛 별 마크가 그 표시입니다.", "flash"));
            // 빛나는 카드 — 은총(사도 고유 카드를 손에)과 카드 신탁(바뀔 모습 고르기)은 풀이가 다르다(2026-10-07 제보: 은총이 「신탁」 으로 떴다)
            if (info.Epiphany) info.Terms.Insert(0, info.Grace
                ? new Term("은총", "빛나는 카드 — 내면 그 사도의 고유 카드 하나를 손에 얻습니다(그 턴 비용 0)", "flash")
                : new Term("신탁", "빛나는 카드 — 내는 순간 바뀔 모습을 고릅니다(이번에는 비용 0)", "flash"));
            // 표식 — 얹힌 축복 · 복제본(core CardMark, 판 화면 W.Card 와 같은 것)
            var mk = b.MarkOf(cv.Id);
            info.MarkBless = mk.Bless; info.MarkBlessText = mk.BlessText; info.Copy = mk.Copy;
            if (mk.Blessed) info.Terms.Insert(info.Epiphany || cv.Oracle != null ? 1 : 0, new Term("축복", mk.BlessText, "flash"));
            if (mk.Copy) info.Terms.Add(new Term("복제", "복제할 때 모습 그대로 묶인 카드 — 신탁 · 축복 불가", "flash"));
            return info;
        }

        /// <summary>카드 정보의 바뀌지 않는 몫 — 전투(b) · CardView 가 같으면 늘 같다. 카드 글(text.Card)은 한 번만 짓는다(예전엔 세 번).</summary>
        InfoBase InfoBaseOf(CardView cv, int depth)
        {
            var ib = new InfoBase();
            string card = text.Card(cv);
            // 사도 카드의 주인(Def.Hero) · 교주 카드를 넣은 사도(core CardView.Owner — 덱 id 「카드@사도」). cv.Hero 는 Owner ?? Def.Hero 라
            // 교주 카드를 사도 카드로 읽지 않게 둘을 나눈다(그림 · 더미 차례는 교주 카드 그대로, 틀 빛깔 · 핀만 주인 사도)
            int hero = cv.Def.Hero != null ? b.Party.FindIndex(u => u.Key == cv.Def.Hero) : -1;
            int owner = cv.Owner != null ? b.Party.FindIndex(u => u.Key == cv.Owner) : -1;
            var type = TypeOf(cv.Type);
            bool dmg = cv.Fx.Any(f => f.K == FxK.Dmg);
            bool allDmg = cv.Fx.Any(f => f.K == FxK.Dmg && f.Target == "allEnemies");
            var info = new CardInfo
            {
                Id = cv.Id, Name = cv.Name, Hero = hero, HeroName = hero >= 0 ? b.Party[hero].Name : owner >= 0 ? $"교주({b.Party[owner].Name})" : cv.IsStatus ? "상태" : cv.IsCurse ? "저주" : "교주",
                Cost = cv.Cost, Type = type, TypeName = cv.Type,
                Target = cv.Target == "적" ? TargetKind.Enemy : cv.Target == "아군" ? TargetKind.Ally : allDmg ? TargetKind.AllEnemies : TargetKind.None,
                Text = Fmt(cv.Oracle != null ? Bolzena.RunUI.OracleDiff.Mark(NoTagLine(text.Card(cv.Def), cv.Def.Tags), NoTagLine(card, cv.Tags)) : NoTagLine(card, cv.Tags)), CostDown = cv.Oracle != null && cv.Oracle.Cost.HasValue && cv.Oracle.Cost.Value < cv.Def.Cost, Art = Look.CardArt(cv.Def.Hero, GameData.BaseId(cv.Id), cv.Unique, cv.Type),
                Motion = type == CardType.Attack ? (cv.Cost >= 2 ? Motion.Attack2 : Motion.Attack1) : dmg ? Motion.Skill1 : Motion.None,
                Hit = Look.Hero(cv.Hero).Hit,
                EpiphanyLabel = cv.Oracle != null ? "신탁" : null, Tags = cv.Tags.ToList(), Unplayable = cv.HasTag(Bolzena.Core.Tag.Unplayable),
                Unique = cv.Unique, Owner = owner, Grade = cv.Def.Hero == null ? cv.Def.Grade : null, Nature = hero >= 0 ? b.Party[hero].Nature : owner >= 0 ? b.Party[owner].Nature : null,
                Choices = cv.Choices != null && cv.Choices.Count == 2 ? cv.Choices.ToList() : null,
            };
            // 사도 고유 효과는 카드 주인 사도의 것만(runui CardTerms 와 같은 규칙 — 디아나 「제자」 ↔ 밍스 「제자」 처럼 이름이 겹쳐도 잘못 잇지 않게)
            string cardOwner = cv.Def.Hero ?? cv.Owner;
            info.Terms = Terms.ForCard(card, cv.Tags, w => b.Kw.TryGetValue(w, out var kw) && (cardOwner == null || kw.Owner == cardOwner) ? (kw.Id, KwText(kw)) : ((string, string)?)null);
            // 사도 고유 효과 — 부제 + 수치가 다 든 글(CardText.Trait, 「자세히」 없음), 생성 카드 — 작은 카드(runui CardTerms: make · transform · 진화 · 결속 · 금기)
            var cterms = Bolzena.RunUI.CardTerms.Of(data, text, cv, card);
            // 위에서 못 잡은 사도 고유 효과 · 변신(성전 모드 · 맨주먹 전성기 …, 변신이 바꿔 넣은 카드 포함) — CardTerms 의 판 글 그대로
            foreach (var ct in cterms)
                if (!ct.IsCard && ct.Hero && !info.Terms.Exists(x => x.Word == ct.Name))
                    info.Terms.Add(new Term(ct.Name, Fmt(ct.Body), ct.Kind != null && ct.Kind.StartsWith("변신") ? "form" : "kw"));
            // 엔진 키워드 가운데 화면 낱말 표(Terms.Words)에 없는 것(「격파 상태」 · 「피해 기반 회복」 — 2026-10-07 카제나 키워드로 줄인 글) — 판 화면 W.Card 와 같은 판.
            // 그 이름 속 짧은 낱말(「격파」)이 글의 다른 곳에 따로 안 나오면 짧은 판은 뺀다
            foreach (var ct in cterms)
                if (!ct.IsCard && !ct.Hero && !info.Terms.Exists(x => x.Word == ct.Name))
                {
                    string rest = (info.Text ?? "").Replace(ct.Name, "");
                    info.Terms.RemoveAll(x => x.Kind == "kw" && x.Word.Length < ct.Name.Length && ct.Name.Contains(x.Word) && !rest.Contains(x.Word));
                    info.Terms.Add(new Term(ct.Name, Fmt(ct.Body), ct.Bad ? "status" : "tag"));   // 「tag」 = 엔진 낱말 판 꼴(「kw」 면 CardZoom 이 고유 효과로 적는다)
                }
            // 생성 카드(작은 카드) — 그 카드의 정보는 덧입히기에서 그때그때(빛 · 표식이 바뀔 수 있다)
            if (depth == 0)
                foreach (var ct in cterms)
                    if (ct.IsCard)
                    {
                        var gv = data.View(ct.CardId);
                        if (gv != null) (ib.Gen ??= new List<(string, CardView)>()).Add((ct.Name, gv));
                    }
            info.Text = Bolzena.RunUI.CardTerms.Mark(info.Text, cterms);   // 글 속 낱말 밑줄 · 색(판 화면 W.Card 와 같은 표)
            ib.Info = info;
            return ib;
        }

        static readonly Dictionary<string, string> STAT_KO = new Dictionary<string, string>
            { ["dealt"] = "주는 피해", ["taken"] = "받는 피해", ["atk"] = "공격력", ["def"] = "방어력", ["crit"] = "치명" };

        List<StatusChip> ChipsOf(Unit u, bool hero)
        {
            var res = new List<StatusChip>();
            // 증감 — 종류마다 한 칸(값은 합, 턴은 가장 먼저 끝나는 것)
            foreach (var g in u.Mods.GroupBy(m => m.Stat))
            {
                double v = g.Sum(m => m.V);
                if (Math.Abs(v) < 0.005 || !STAT_KO.TryGetValue(g.Key, out var ko)) continue;
                bool good = g.Key == "taken" ? v < 0 : v > 0;
                int left = g.Min(m => m.Run || m.Left >= 99 ? 999 : m.Left);
                res.Add(new StatusChip
                {
                    Id = ko, Value = (v > 0 ? "+" : "") + Mathf.RoundToInt((float)v * 100) + "%", Turns = left >= 999 ? -1 : left, Kind = good ? "buff" : "debuff",
                    Text = string.Join("\n", g.Select(m => $"{ko} {(m.V > 0 ? "+" : "")}{Mathf.RoundToInt((float)m.V * 100)}% · {(m.Run || m.Left >= 99 ? "전투 내내" : m.Left + "턴")}{(m.Src != null ? " · " + m.Src : "")}")),
                });
            }
            // 건 쪽(사도) — 엔진 StatusViews 의 Sources(사도 키) → 화면 키(초상)
            var from = new Dictionary<string, List<string>>();
            try
            {
                foreach (var sv in b.StatusViews(u))
                {
                    if (sv.Sources == null) continue;
                    var l = sv.Sources.Where(x => !string.IsNullOrEmpty(x.Hero)).Select(x => Look.Hero(x.Hero).Art).Distinct().ToList();
                    if (l.Count > 0) from[sv.Id] = l;
                }
            }
            catch (Exception) { }
            foreach (var kv in u.Status)
            {
                if (kv.Value == 0) continue;
                if (b.Kw.TryGetValue(kv.Key, out var kw))
                {
                    res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = "key", Text = KwText(kw) });
                    continue;
                }
                if (kv.Key == R.SEIZED)
                {   // 손패 흡수(core 수 seize) — 쥔 카드 이름 · 되찾는 길
                    var names = b.SeizedOf(u).Select(x => $"「{b.CardOf(x)?.Name ?? x}」");
                    res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = "buff",
                        Text = $"이 적이 빼앗아 쥔 카드: {string.Join(" · ", names)}\n격파하거나 · 쓰러뜨리거나 · 피해 {b.SeizeLeft(u)} 더 주면 손으로 돌아옵니다." });
                    continue;
                }
                bool bad = R.BAD_ST.Contains(kv.Key) || kv.Key == R.STUN;
                res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = bad ? "debuff" : "buff", Text = Terms.StatusText(kv.Key) ?? "", From = from.TryGetValue(kv.Key, out var fl) ? fl : null });
            }
            if (hero && b.Stacks.TryGetValue(u.Key, out var bag))
                foreach (var kv in bag)
                    if (kv.Value != 0 && b.Kw.TryGetValue(kv.Key, out var kw))
                        res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = "key", Text = KwText(kw) });
            // 강화 칩(파티 버프 줄 끝) — 켜진 강화 카드마다: 카드 이름 · 겹, 풀이 = 남는 효과 글(core b.PowersOf · Docs/API.md 「강화 카드 지속 규칙」)
            if (u == b.Pool)
            {
                List<PowerView> pws = null;
                try { pws = b.PowersOf(); } catch (Exception) { }
                if (pws != null)
                    foreach (var pw in pws)
                        res.Add(new StatusChip { Id = pw.Name, Value = pw.Stacks > 1 ? pw.Stacks.ToString() : "", Kind = "power",
                            Text = $"강화 카드{(pw.Stacks > 1 ? $" · {pw.Stacks}겹(발동할 때 효과 ×{pw.Stacks})" : "")} · 이 전투 동안\n{Fmt(pw.Text)}",
                            From = pw.Hero != null ? new List<string> { Look.Hero(pw.Hero).Art } : null });
            }
            // 변신 칩 — 이름 · 남은 턴(0 = 전투 끝까지)
            if (hero)
            {
                FormView fv = null;
                try { fv = b.FormOf(u.Key); } catch (Exception) { }
                if (fv != null)
                {
                    // 변신이 무엇인지 — 부제(desc) + 규칙에서 만든 본문(core CardText.FormBody, 2026-10-07 「성전 모드가 뭔지 안 나온다」)
                    var fd = data.Hero(u.Key)?.Forms?.Find(x => x.Name == fv.Name);
                    string about = fd != null ? (string.IsNullOrEmpty(fd.Desc) ? "" : $"\n<color=#A7B1CC>{fd.Desc.TrimEnd('.')}</color>") + "\n" + Fmt(text.FormBody(fd)) : "";
                    res.Insert(0, new StatusChip { Id = fv.Name, Value = fv.Left > 0 ? fv.Left.ToString() : "", Turns = fv.Left > 0 ? fv.Left : -1, Kind = "buff",
                        Text = $"변신 「{fv.Name}」 · {(fv.Left > 0 ? "남은 " + fv.Left + "턴" : "전투 끝까지")}{about}" });
                }
            }
            return res;
        }

        /// <summary>엘리트 · 일반의 힘 모으기 — 모으는 턴(charge, Next 가 ult 아님)이면 쏟을 수 이름 · false, 모은 힘을 쏟는 턴이면 그 수 이름 · true. 아니면 null(보스 고학년도 null — UltName 쪽).</summary>
        static (string name, bool now) ChargeOf(Unit e)
        {
            var it = e?.Intent;
            if (it == null || e.Dead || BossUlt.IsUlt(it)) return (null, false);
            if (it.T == "charge" && it.Next != null) return (it.Next.Say ?? "큰 수", false);
            if (e.IntentFromCharge) return (it.Say ?? "큰 수", true);
            return (null, false);
        }

        static IntentKind KindOf(Intent it)
        {
            if (it == null) return IntentKind.None;
            switch (it.T)
            {
                case "attack": case "multi": return IntentKind.Attack;
                case "back": case "attackAll": case "charge": case "ult": return IntentKind.Heavy;
                case "block": case "guard": case "brace": return IntentKind.Defend;   // brace = 버티기(강인도 회복)
                case "heal": case "buff": return IntentKind.Buff;
                default: return IntentKind.Debuff;
            }
        }

        /// <summary>사도의 바뀌지 않는 화면 글 — 고학년 · 고유 효과 · 특성(Traits) · 장비. 전투(b)마다 한 번.</summary>
        HeroBase HeroBaseOf(Unit u, HeroDef h)
        {
            CacheFor();
            if (heroBases.TryGetValue(u.Key, out var hb)) return hb;
            hb = new HeroBase
            {
                UltText = h?.Ult != null ? Fmt(text.Fx(h.Ult.Fx)) : "",
                KeywordText = h?.Keyword != null && b.Kw.TryGetValue(h.Keyword.Name, out var hk) ? KwText(hk) : null,
            };
            // 고학년 → 고유 효과 → 패시브(core CardText.Traits — 판 화면 사도 상세와 같은 칸 · 글). 키워드를 쓰는 패시브는 고유 효과 칸 아래로
            if (h != null)
                foreach (var t in text.Traits(h))
                {
                    hb.Traits.Add(new TraitLine { Kind = t.Kind, Name = t.Name, Sub = t.Sub, Body = Fmt(t.Body) });
                    if (t.Kind == "패시브" && !string.IsNullOrEmpty(t.Body)) hb.Passives.Add(t.Name + " — " + Fmt(t.Body));   // passives 전부(없으면 「없음」 칸 — 목록엔 안 넣음)
                }
            try
            {
                var g = GearOf?.Invoke(u.Key);
                if (g == null && Array.IndexOf(Environment.GetCommandLineArgs(), "-demogear") >= 0)
                {   // 점검(-demogear) — 장비를 낀 모습을 보려고 슬롯마다 첫 장비를 끼운 것으로 친다(규칙에는 안 들어간다)
                    g = new Dictionary<string, string>();
                    foreach (var sl in new[] { "무기", "방어구", "장신구" }) { var eq = data.Equips.Values.FirstOrDefault(e => e.Slot == sl); if (eq != null) g[sl] = eq.Id; }
                }
                foreach (var slot in new[] { "무기", "방어구", "장신구" })
                {
                    var gs = new GearSlot { Slot = slot };
                    if (g != null && g.TryGetValue(slot, out var eid) && !string.IsNullOrEmpty(eid))
                    {
                        var ed = data.Equip(eid);
                        gs.Id = eid; gs.Name = ed?.Name ?? eid; gs.Grade = ed?.Grade;
                        try { gs.Text = ed != null ? Fmt(text.Equip(ed)) : null; } catch (Exception) { }
                    }
                    hb.Gear.Add(gs);
                }
            }
            catch (Exception) { }
            heroBases[u.Key] = hb;
            return hb;
        }

        // -snapperf(재기) — Snapshot 을 새로 지을 때마다 시간 · 할당을 모아 로그 [SnapPerf] 로(2026-10-08 화면 캐시 전후 비교)
        static readonly bool snapPerf = Array.IndexOf(Environment.GetCommandLineArgs(), "-snapperf") >= 0;
        static int spN; static double spMs, spMax; static long spAlloc;
        public BattleSnapshot Snapshot
        {
            get
            {
                if (snap != null) return snap;
                if (!snapPerf) return snap = BuildSnapshot();
                // 할당 — Mono 는 스레드 할당 수를 주지 않아, 짓는 동안 GC 를 멈추고 쓰는 힙이 는 만큼을 센다
                var gm = UnityEngine.Scripting.GarbageCollector.GCMode;
                try { UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Disabled; } catch (Exception) { }
                long a0 = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(); long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                snap = BuildSnapshot();
                double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                long a1 = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                try { UnityEngine.Scripting.GarbageCollector.GCMode = gm; } catch (Exception) { }
                spN++; spMs += ms; spMax = Math.Max(spMax, ms); spAlloc += Math.Max(0, a1 - a0);
                if (spN % 10 == 0 || over) Debug.Log($"[SnapPerf] 지음 {spN} · 평균 {spMs / spN:0.000}ms · 최대 {spMax:0.00}ms · 평균 할당 {spAlloc / 1024.0 / spN:0.0}KB · 손 {snap.Hand.Count} · 더미 {b.Draw.Count}/{b.Discard.Count}/{b.Gone.Count}");
                return snap;
            }
        }

        BattleSnapshot BuildSnapshot()
        {
            {
                var s = new BattleSnapshot
                {
                    Turn = turnBase + b.Turn, Wave = wave + 1, WaveCount = fx.Waves.Count,
                    PartyHp = b.Pool.Hp, PartyMaxHp = b.Pool.MaxHp, PartyBlock = b.Pool.Block + b.Pool.Shield,
                    Ap = b.Ap, MaxAp = R.AP_PER_TURN, DrawCount = b.Draw.Count, DiscardCount = b.Discard.Count, GoneCount = b.Gone.Count,
                    Gauge = b.Gauge, GaugeMax = R.GAUGE_MAX, Over = over, Won = won,
                };
                foreach (var u in b.Party)
                {
                    var h = data.Hero(u.Key);
                    var look = Look.Hero(u.Key);
                    var hb = HeroBaseOf(u, h);
                    var hs = new HeroState
                    {
                        Key = look.Art, Id = u.Key, Name = u.Name, Tint = look.Tint, UltName = h?.Ult?.Name, UltText = hb.UltText,
                        Atk = u.Atk, Def = u.Def, Crit = u.Crit, AtkNow = b.AtkNow(u), DefNow = b.DefNow(u), CritNow = u.Crit + Mathf.RoundToInt((float)b.StatMod(u, "crit") * 100),
                        Role = u.Role, Nature = u.Nature, Slot = u.Idx, Blurb = h?.Blurb, Ult = b.Gauge, UltMax = h?.Ult?.Cost ?? 999, Dead = u.Dead,
                        KeywordName = h?.Keyword?.Name, KeywordText = hb.KeywordText,
                        KeywordStacks = h?.Keyword != null ? b.StackOf(u.Key, h.Keyword.Name) : 0,
                    };
                    hs.Traits.AddRange(hb.Traits); hs.Passives.AddRange(hb.Passives);
                    hs.Chips = ChipsOf(u, true);
                    hs.Race = h?.Race;
                    hs.Gear.AddRange(hb.Gear);
                    s.Heroes.Add(hs);
                }
                foreach (var e in b.Enemies)
                {
                    var def = data.Enemy(e.Key);
                    var (spine, skin) = Look.EnemyAs(e.Key, e.Boss, e.Nature);   // 판 속성(통일된 성격)의 스킨
                    var it = e.Intent;
                    int? shown = b.IntentHit(e);
                    // 힘 모으기(charge) — IntentHit 은 null(모으는 턴엔 치지 않는다). 의도 칸은 다음 턴 피해 합(b.ChargeHit — 고학년 예고도 같은 값)으로 「다음 턴 ○○」
                    bool charging = it != null && it.T == "charge";
                    int? chargeHit = charging ? b.ChargeHit(e) : null;
                    var ckind = !charging ? KindOf(it) : chargeHit != null || it.Next == null ? IntentKind.Heavy : KindOf(it.Next);   // 치지 않는 수를 모으면 그 수의 갈래
                    var cinfo = ChargeOf(e);
                    var tv = b.ToughViewOf(e);   // 강인도 · 격파 · 성격 · 약점 — 소수 그대로(1/3 · 1/6)
                    var es = new EnemyState
                    {
                        Key = spine, Skin = skin, Id = e.Key, Name = e.Name, Boss = e.Boss, Hp = Math.Max(0, e.Hp), MaxHp = e.MaxHp, Block = e.Block + e.Shield,
                        ToughV = (float)tv.Left, ToughMaxV = (float)tv.Max, Broken = tv.Broken, Resting = tv.Resting, Sealed = e.Sealed, Dead = e.Dead,
                        Intent = e.Broken || e.Dead ? IntentKind.None : ckind, IntentValue = charging ? chargeHit ?? it.Next?.V ?? 0 : shown ?? it?.V ?? 0, IntentHits = it != null && it.T == "multi" ? Math.Max(1, it.N) : 1,
                        IntentText = it != null ? text.Intent(it, shown) : "", IntentSay = it?.Say,
                        UltName = BossUlt.IsUlt(it) && !e.Broken ? (it.T == "ult" ? it.Say : it.Next?.Say ?? it.Say) : null,
                        UltHero = BossUlt.IsUlt(it) ? (it.T == "ult" ? it.Id : it.Next?.Id ?? it.Id) : null, UltNow = it != null && it.T == "ult", RushNeed = b.RushOf(e), RushCnt = e.RushCnt, RushedTurn = e.RushedTurn,
                        Nature = tv.Nature, Blurb = def?.Blurb, Weak = tv.Weak ?? new List<string>(),
                        ChargeName = e.Broken ? null : cinfo.name, ChargeNow = cinfo.now, IntentLater = charging,
                    };
                    if (def != null)
                    {
                        if (!foePassTexts.TryGetValue(e.Key, out var fp))   // 적 패시브 글은 전투 동안 그대로
                        {
                            foePassTexts[e.Key] = fp = new List<string>();
                            foreach (var p in def.Passives) fp.Add(p.Name + (p.Do != null ? " — " + text.Intent(p.Do) : ""));
                        }
                        es.Passives.AddRange(fp);
                    }
                    es.Chips = ChipsOf(e, false);
                    s.Enemies.Add(es);
                }
                for (int i = 0; i < b.Hand.Count; i++) s.Hand.Add(Info(b.CardOf(b.Hand[i]), i));
                // 더미 카드 정보는 처음 볼 때(더미 창 · 정보 창) 짓는다 — 지금의 id 차례만 떠 둔다(장수는 위 *Count)
                var piles = new[] { b.Draw.OrderBy(x => x, StringComparer.Ordinal).ToList(), b.Discard.ToList(), b.Gone.ToList() };
                s.PileSource = k => piles[k].ConvertAll(id => Info(b.CardOf(id), null));
                s.PartyChips = ChipsOf(b.Pool, false);
                AddEmpowerChip(s.PartyChips);
                snap = s;
                return s;
            }
        }
    }
}
