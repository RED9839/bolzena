using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Core;
using UnityEngine;
using CSide = Bolzena.Core.Side;
using CoreFight = Bolzena.Core.Battle;

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
                f.Waves.Add(foes.Split(',').ToList());
                // -glow: 고유 카드를 모두 빛나게(신탁 창 시험)
                if (Array.IndexOf(a, "-glow") >= 0) f.Glow.AddRange(f.Deck.Where(id => (d.Card(id)?.Unique ?? false) && d.Card(id).Oracles.Count > 0).Distinct());
                return f;
            }

            public static Fixture Pilot(GameData d)
            {
                // 코어 샘플(rico · carrot · sion) 또는 콘텐츠 폴더(리코타 · 캬롯 · 시온더다크불릿 — 웹판 키)
                bool sample = d.Hero("rico") != null;
                var f = new Fixture { Party = sample ? new List<string> { "rico", "carrot", "sion" } : new List<string> { "리코타", "캬롯", "시온더다크불릿" } };
                f.Deck = d.BuildDeck(f.Party);
                // 시범 — 고유 카드 둘을 덱에 넣고 하나를 빛나게(신탁 창을 보이려고)
                var uniq = sample ? new List<string> { "sion_u1", "rico_u1" }
                                  : f.Party.Take(2).Reverse().Select(h => d.Cards.Values.Where(c => c.Hero == h && c.Unique && c.Oracles.Count > 0).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault()).Where(x => x != null).ToList();
                foreach (var u in uniq) if (d.Card(u) != null) f.Deck.Add(u);
                if (uniq.Count > 0 && d.Card(uniq[0]) != null) f.Glow.Add(uniq[0]);
                f.Waves.Add(sample ? new List<string> { "fairy_close", "fairy_long" } : new List<string> { "fairymobcloserange_naive", "fairymoblongrange_jolly" });
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

        // ── 열기 ──────────────────────────────────────────────────────
        CoreFight StartWave(int w, int partyHp, int gauge, Dictionary<string, Glow> glow)
        {
            var st = new BattleSetup
            {
                Party = fx.Party.ToList(), Deck = fx.Deck.ToList(), Enemies = fx.Waves[w].ToList(),
                Seed = fx.Seed + w * 101, EnemyHp = w < fx.WaveHp.Count ? fx.WaveHp[w] : (double?)null,
                Gauge = Math.Max(gauge, w < fx.WaveGauge.Count ? fx.WaveGauge[w] : 0), Glow = glow,
            };
            if (partyHp >= 0) st.PartyHp = partyHp;
            cues.Clear();
            var nb = CoreFight.Start(data, st, cues);
            shownIntent.Clear();
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
                g[id] = gl;
            }
            return g;
        }

        public IReadOnlyList<BattleEvent> Begin()
        {
            var evs = new List<BattleEvent>();
            wave = 0;
            if (preset != null) { b = preset; preset = null; shownIntent.Clear(); guard.Clear(); }   // 쪽지는 이미 cues 에
            else b = StartWave(0, -1, 0, GlowFor(data, fx.Glow));
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
                    info.Text = Fmt(text.Oracle(def, o));
                    info.EpiphanyLabel = o.Name;
                    info.Epiphany = false;
                    var oo = opts != null ? opts.Find(x => x.N == p.N && x.Shin == p.Shin) : null;
                    if (oo != null && oo.Blessed) { info.BlessName = oo.BlessName; info.BlessText = Fmt(oo.BlessText); }
                    list.Add(info);
                }
                // -blesstest: 축복 모양 확인용 — 얹힌 것이 없으면 둘째 선택지에 축복을 보이게만 단다(규칙에는 안 들어간다)
                if (list.Count > 1 && !list.Exists(x => x.BlessName != null) && Array.IndexOf(Environment.GetCommandLineArgs(), "-blesstest") >= 0)
                { list[1].BlessName = "(시험) 축복"; list[1].BlessText = "축복 모양 확인 — 실제로는 15% 로 얹힌다"; }
            }
            else foreach (var opt in g.Options) list.Add(Info(b.CardOf(opt), null));
            return list;
        }

        /// <summary>낀 장비(사도 id → 칸 → 장비 id) — 판에서 연 싸움이면 BattleBridge 가 단다. 전투 시범은 null(빈 칸).</summary>
        public static Func<string, Dictionary<string, string>> GearOf;

        public IReadOnlyList<BattleEvent> PlayCard(int i, int target, int choice = -1, int branch = 0)
        {
            var evs = new List<BattleEvent>();
            if (!CanPlay(i, out _)) return evs;
            cues.Clear();
            var id = b.Hand[i];
            if (b.GlowOf(id) != null) b.ApplyEpiphany(id, Math.Max(0, choice));
            lastTarget = target;
            b.PlayCard(i, target, branch > 0 ? new PlayOpts { Choice = branch } : null);
            Translate(evs);
            After(evs);
            return evs;
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
            l?.Select(x => x == null ? null : new PreviewFoe { Hp = x.Hp, Guard = x.Guard, Kill = x.Kill, Max = x.Max, Break = x.Brk, Tough = (int)Math.Round(x.Tough) }).ToList();

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
                            heavy = hit && (c.T == "back" || c.T == "attackAll" || c.Rush);
                            evs.Add(new BattleEvent
                            {
                                Kind = EventKind.Act, Actor = actor, Motion = hit && !heavy ? Motion.Attack1 : Motion.Skill1,
                                Text = hit ? heavy ? "heavy" : "attack" : "defend", Say = c.Say, Up = c.Rush,
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
                        evs.Add(new BattleEvent { Kind = EventKind.Status, Actor = actor, Target = Ref(c), Text = c.Id, Up = c.Up });
                        break;
                    case "tough":
                        if (c.Up)
                        {
                            if (c.From <= 0.001) evs.Add(new BattleEvent { Kind = EventKind.Recover, Target = UnitRef.Enemy(c.Idx), HpAfter = (int)Math.Ceiling(c.To) });
                            else evs.Add(new BattleEvent { Kind = EventKind.Toughness, Target = UnitRef.Enemy(c.Idx), HpAfter = (int)Math.Ceiling(c.To - 0.001), Up = true });
                        }
                        else evs.Add(new BattleEvent { Kind = EventKind.Toughness, Target = UnitRef.Enemy(c.Idx), Value = (int)Math.Round(c.From - c.To), HpAfter = (int)Math.Ceiling(c.To - 0.001) });
                        break;
                    case "break":
                        evs.Add(new BattleEvent { Kind = EventKind.Break, Target = UnitRef.Enemy(c.Idx), Value = c.V });
                        break;
                    case "die":
                        if (c.Side == CSide.Enemy)
                            evs.Add(new BattleEvent { Kind = EventKind.Death, Target = UnitRef.Enemy(c.Idx), Boss = c.Idx < b.Enemies.Count && b.Enemies[c.Idx].Boss });
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
                        break;
                    case "talk":
                        evs.Add(new BattleEvent { Kind = EventKind.Talk, Actor = UnitRef.Party(RepHero(c.Idx)), Text = c.Moment });
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
            else if (from == "hand" && to == "play")
            {
                var ci = Info(b.CardOf(c.CardId), null);
                evs.Add(new BattleEvent { Kind = EventKind.CardPlayed, Card = ci, Actor = UnitRef.Party(Math.Max(0, ci.Hero)) });
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
                string sig = e.Dead || e.Intent == null ? "" : $"{e.Intent.T}|{e.Intent.V}|{e.Intent.N}|{b.IntentHit(e)}|{e.Broken}|{e.RushCnt}";
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

        CardInfo Info(CardView cv, int? handIdx)
        {
            if (cv == null) return new CardInfo { Id = "?", Name = "?" };
            int hero = cv.Hero != null ? b.Party.FindIndex(u => u.Key == cv.Hero) : -1;
            var type = TypeOf(cv.Type);
            bool dmg = cv.Fx.Any(f => f.K == FxK.Dmg);
            bool allDmg = cv.Fx.Any(f => f.K == FxK.Dmg && f.Target == "allEnemies");
            var info = new CardInfo
            {
                Id = cv.Id, Name = cv.Name, Hero = hero, HeroName = hero >= 0 ? b.Party[hero].Name : cv.IsStatus ? "상태" : cv.IsCurse ? "저주" : "교주",
                Cost = handIdx != null ? b.CostOf(cv.Id, handIdx) : cv.Cost, Type = type, TypeName = cv.Type,
                Target = cv.Target == "적" ? TargetKind.Enemy : cv.Target == "아군" ? TargetKind.Ally : allDmg ? TargetKind.AllEnemies : TargetKind.None,
                Text = Fmt(text.Card(cv)), Art = Look.CardArt(cv.Hero, GameData.BaseId(cv.Id), cv.Unique, cv.Type),
                Motion = type == CardType.Attack ? (cv.Cost >= 2 ? Motion.Attack2 : Motion.Attack1) : dmg ? Motion.Skill1 : Motion.None,
                Hit = Look.Hero(cv.Hero).Hit, Epiphany = b.GlowOf(cv.Id) != null,
                EpiphanyLabel = cv.Oracle != null ? cv.Oracle.Name : null, Tags = cv.Tags.ToList(), Unplayable = cv.HasTag(Bolzena.Core.Tag.Unplayable),
                Unique = cv.Unique, Nature = hero >= 0 ? b.Party[hero].Nature : null,
                Choices = cv.Choices != null && cv.Choices.Count == 2 ? cv.Choices.ToList() : null,
            };
            info.Terms = Terms.ForCard(text.Card(cv), cv.Tags, w => b.Kw.TryGetValue(w, out var kw) ? (kw.Id, text.Keyword(kw.Def)) : ((string, string)?)null);
            if (cv.Oracle != null) info.Terms.Insert(0, new Term("신탁 · " + cv.Oracle.Name, "이 판에서 붙은 신탁 — 카드 글이 바뀐 모습입니다", "flash"));
            if (info.Epiphany) info.Terms.Insert(0, new Term("신탁", "빛나는 카드 — 내는 순간 바뀔 모습을 고릅니다(이번에는 코스트 0)", "flash"));
            return info;
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
                    res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = "key", Text = $"「{kv.Key}」 {kv.Value} — {text.Keyword(kw.Def)}" });
                    continue;
                }
                bool bad = R.BAD_ST.Contains(kv.Key) || kv.Key == R.STUN;
                res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = bad ? "debuff" : "buff", Text = $"{kv.Key} {kv.Value} — {Terms.StatusText(kv.Key) ?? ""}", From = from.TryGetValue(kv.Key, out var fl) ? fl : null });
            }
            if (hero && b.Stacks.TryGetValue(u.Key, out var bag))
                foreach (var kv in bag)
                    if (kv.Value != 0 && b.Kw.TryGetValue(kv.Key, out var kw))
                        res.Add(new StatusChip { Id = kv.Key, Value = kv.Value.ToString(), Kind = "key", Text = $"「{kv.Key}」 {kv.Value} — {text.Keyword(kw.Def)}" });
            return res;
        }

        static IntentKind KindOf(Intent it)
        {
            if (it == null) return IntentKind.None;
            switch (it.T)
            {
                case "attack": case "multi": return IntentKind.Attack;
                case "back": case "attackAll": case "charge": return IntentKind.Heavy;
                case "block": case "guard": return IntentKind.Defend;
                case "heal": case "buff": return IntentKind.Buff;
                default: return IntentKind.Debuff;
            }
        }

        public BattleSnapshot Snapshot
        {
            get
            {
                if (snap != null) return snap;
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
                    var hs = new HeroState
                    {
                        Key = look.Art, Id = u.Key, Name = u.Name, Tint = look.Tint, UltName = h?.Ult?.Name, UltText = h?.Ult != null ? Fmt(text.Fx(h.Ult.Fx)) : "",
                        Atk = u.Atk, Def = u.Def, Crit = u.Crit, AtkNow = b.AtkNow(u), DefNow = b.DefNow(u), CritNow = u.Crit + Mathf.RoundToInt((float)b.StatMod(u, "crit") * 100),
                        Role = u.Role, Nature = u.Nature, Row = u.Row, Blurb = h?.Blurb, Ult = b.Gauge, UltMax = h?.Ult?.Cost ?? 999, Dead = u.Dead,
                        KeywordName = h?.Keyword?.Name, KeywordText = h?.Keyword != null ? Fmt(text.Keyword(h.Keyword)) : null,
                        KeywordStacks = h?.Keyword != null ? b.StackOf(u.Key, h.Keyword.Name) : 0,
                    };
                    if (h != null) foreach (var r in h.Passives) hs.Passives.Add(Fmt(text.Rule(r)));
                    hs.Chips = ChipsOf(u, true);
                    hs.Race = h?.Race;
                    try
                    {
                        var g = GearOf?.Invoke(u.Key);
                        foreach (var slot in new[] { "무기", "방어구", "장신구" })
                        {
                            var gs = new GearSlot { Slot = slot };
                            if (g != null && g.TryGetValue(slot, out var eid) && !string.IsNullOrEmpty(eid))
                            {
                                var ed = data.Equip(eid);
                                gs.Id = eid; gs.Name = ed?.Name ?? eid; gs.Grade = ed?.Grade;
                                try { gs.Text = ed != null ? Fmt(text.Equip(ed)) : null; } catch (Exception) { }
                            }
                            hs.Gear.Add(gs);
                        }
                    }
                    catch (Exception) { }
                    s.Heroes.Add(hs);
                }
                foreach (var e in b.Enemies)
                {
                    var def = data.Enemy(e.Key);
                    var (spine, skin) = Look.Enemy(e.Key, e.Boss);
                    var it = e.Intent;
                    int? shown = b.IntentHit(e);
                    var es = new EnemyState
                    {
                        Key = spine, Skin = skin, Id = e.Key, Name = e.Name, Boss = e.Boss, Hp = Math.Max(0, e.Hp), MaxHp = e.MaxHp, Block = e.Block + e.Shield,
                        Tough = (int)Math.Ceiling(e.Tough - 0.001), MaxTough = (int)Math.Ceiling(e.ToughMax - 0.001), Broken = e.Broken, Sealed = e.Sealed, Dead = e.Dead,
                        Intent = e.Broken || e.Dead ? IntentKind.None : KindOf(it), IntentValue = shown ?? it?.V ?? 0, IntentHits = it != null && it.T == "multi" ? Math.Max(1, it.N) : 1,
                        IntentText = it != null ? text.Intent(it, shown) : "", IntentSay = it?.Say, RushNeed = b.RushOf(e), RushCnt = e.RushCnt, RushedTurn = e.RushedTurn,
                        Nature = e.Nature, Blurb = def?.Blurb, Weak = b.WeakOf(e) ?? new List<string>(),
                    };
                    if (def != null) foreach (var p in def.Passives) es.Passives.Add(p.Name + (p.Do != null ? " — " + text.Intent(p.Do) : ""));
                    es.Chips = ChipsOf(e, false);
                    s.Enemies.Add(es);
                }
                for (int i = 0; i < b.Hand.Count; i++) s.Hand.Add(Info(b.CardOf(b.Hand[i]), i));
                foreach (var id in b.Draw.OrderBy(x => x, StringComparer.Ordinal)) s.DrawPile.Add(Info(b.CardOf(id), null));
                foreach (var id in b.Discard) s.DiscardPile.Add(Info(b.CardOf(id), null));
                foreach (var id in b.Gone) s.GonePile.Add(Info(b.CardOf(id), null));
                s.PartyChips = ChipsOf(b.Pool, false);
                snap = s;
                return s;
            }
        }
    }
}
