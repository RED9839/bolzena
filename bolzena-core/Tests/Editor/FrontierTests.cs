using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 프론티어 2단계 — 사도 저장 슬롯(HeroSlot.cs) · 규칙 검사 · 프론티어 전투(Frontier.cs) · 점수(Unit.Lost) · 다시 돌리기 확인(Frontier.Verify).
    /// </summary>
    public class FrontierTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        /// <summary>콘텐츠 폴더 — 환경 변수 BZ_CONTENT(교차 테이프를 뜬 콘텐츠 사본, 예: 성장 표를 넣은 scratchpad 사본), 없으면 bolzena-content-v2.</summary>
        static readonly string CONTENT = System.Environment.GetEnvironmentVariable("BZ_CONTENT") is string bc && bc.Length > 0 ? bc : "C:/projects/bolzena-content-v2";

        /// <summary>샘플 데이터로 「완주한 판」 하나 — 신탁 · 축복 · 복제본 · 카드 값 · 교주 카드 주인 · 짐(저주 · 상태) · 장비 · 성장을 손으로 얹는다.</summary>
        static Run ClearRun(GameData d)
        {
            var run = Run.New(d, PARTY, 7);
            var S = run.S;
            S.Deck.Add("rico_u1"); S.Flash["rico_u1"] = 2;
            var kinds = Run.DivineKindsFor(d.View("rico_u1", 2));
            if (kinds.Count > 0) S.Shin["rico_u1"] = kinds[0];
            S.CardVals["rico_u1"] = new Dictionary<string, int> { ["충전"] = 2 };
            run.AddCopy("rico_u1");                       // rico_u1^ — 신탁 · 축복을 베낀 복제본
            S.Deck.Add(GameData.WithOwner("n_pray", "carrot"));
            S.Deck.Add("curse_chores"); S.Deck.Add("st_dizzy");   // 짐 — 편성 차례로 rico · carrot 몫
            S.Gear["rico"] = new Dictionary<string, string> { ["장신구"] = "eq_charm", ["무기"] = "eq_knife" };
            S.Growth["rico"] = new Stats { Atk = 3, Crit = 1 };
            S.Done = "clear";
            return run;
        }

        static List<HeroSlot> Slots(Run run) => run.S.Party.Select(h => HeroSlot.Of(run, h)).ToList();

        static FrontierSeason Season(string boss = "witch", int turns = 6, double hp = 30) => new FrontierSeason { Id = "test", Boss = boss, Turns = turns, Hp = hp, Dmg = 1 };

        static void Play(GameData d, Battle b, FrontierSeason season)
        {
            var bots = new Bots(d);
            for (int g = 0; Frontier.Over(b, season) == null && g < season.Turns + 5; g++) { bots.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
        }

        // ── 슬롯 ──────────────────────────────────────────────────────
        [Test] public void 완주한_판에서_사도를_뜨면_그_사도_몫만_담기고_JSON_왕복이_같다()
        {
            var d = K.Sample();
            var run = ClearRun(d);
            var deck0 = run.S.Deck.ToList();
            var slots = Slots(run);
            CollectionAssert.AreEqual(deck0, run.S.Deck, "뜨기는 판을 바꾸지 않는다");
            var rico = slots[0];
            Assert.AreEqual("rico", rico.Hero);
            Assert.IsTrue(rico.Cards.All(c => HeroSlot.OwnerOf(d, c.Id) == "rico" || c.Id == "curse_chores"), "rico 슬롯엔 rico 카드와 rico 몫 짐만");
            var u1 = rico.Cards.Single(c => c.Id == "rico_u1");
            Assert.AreEqual(2, u1.O); Assert.AreEqual(run.S.Shin.TryGetValue("rico_u1", out var sh) ? sh : null, u1.B); Assert.AreEqual(2, u1.Vals["충전"]);
            Assert.IsTrue(rico.Cards.Any(c => GameData.IsCopy(c.Id) && c.O == 2), "복제본도 신탁째");
            Assert.AreEqual(new[] { "무기", "장신구" }, rico.Gear.Keys.ToArray(), "장비 칸은 R.SLOTS 차례로");
            Assert.AreEqual(3, rico.Growth.Atk); Assert.AreEqual(1, rico.Growth.Crit);
            Assert.AreEqual("clear", rico.From.Result); Assert.AreEqual(Fingerprint.Rules(), rico.Rules);
            Assert.IsTrue(slots[1].Cards.Any(c => c.Id == "n_pray@carrot"), "교주 카드는 주인 사도 슬롯으로");
            Assert.IsTrue(slots[1].Cards.Any(c => c.Id == "st_dizzy"), "둘째 짐은 둘째 사도 몫");
            Assert.AreEqual(2, slots.Sum(s => s.Cards.Count(c => c.Id == "curse_chores" || c.Id == "st_dizzy")), "짐은 버리지도 불리지도 않는다(사도 셋에 한 번씩)");
            Assert.AreEqual(deck0.Count, slots.Sum(s => s.Cards.Count), "덱의 모든 장이 어느 한 슬롯에");
            foreach (var s in slots)
            {
                var c = HeroSlot.Check(d, s);
                Assert.IsTrue(c.Ok, $"{s.Hero} — {c}");
                var json = s.Json();
                var back = HeroSlot.Parse(json);
                Assert.AreEqual(json, back.Json(), "JSON → 슬롯 → JSON 이 같다");
                Assert.AreEqual(s.Hash(), back.Hash());
                Assert.IsTrue(HeroSlot.Check(d, back).Ok);
            }
            Assert.IsTrue(slots[0].Json().Contains("\"kind\":\"bolzena-hero-slot\"") && slots[0].Json().Contains("\"v\":1"));
            Assert.IsFalse(slots[0].Json().Contains("\"hp\""), "숫자 능력치는 적지 않는다");
        }

        [Test] public void 완주하지_않은_판은_뜰_수_없다()
        {
            var d = K.Sample();
            var run = Run.New(d, PARTY, 3);
            Assert.IsNotNull(HeroSlot.WhyNot(run, "rico"));
            Assert.Throws<InvalidOperationException>(() => HeroSlot.Of(run, "rico"));
            run.S.Done = "clear";
            Assert.IsNull(HeroSlot.WhyNot(run, "rico"));
            Assert.IsNotNull(HeroSlot.WhyNot(run, "erpin"), "파티에 없는 사도");
        }

        [Test] public void 규칙_검사가_위조_슬롯을_잡는다()
        {
            var d = K.Sample();
            var good = HeroSlot.Of(ClearRun(d), "rico");
            Assert.IsTrue(HeroSlot.Check(d, good).Ok);
            HeroSlot F(Action<HeroSlot> edit) { var s = HeroSlot.Parse(good.Json()); edit(s); return s; }
            void Bad(string what, Action<HeroSlot> edit, string expect)
            {
                var c = HeroSlot.Check(d, F(edit));
                Assert.IsFalse(c.Ok, what);
                Assert.IsTrue(c.Errors.Any(e => e.Contains(expect)), $"{what} — 「{expect}」 를 기대: {c}");
            }
            Bad("없는 사도", s => s.Hero = "nobody", "없는 사도");
            Bad("다른 사도의 카드", s => s.Cards.Add(new SlotCard { Id = "carrot_u1" }), "주인이 carrot");
            Bad("남의 교주 카드", s => s.Cards.Add(new SlotCard { Id = "n_pray@carrot" }), "주인이 carrot");
            Bad("주인 없는 교주 카드", s => s.Cards.Add(new SlotCard { Id = "n_pray" }), "주인");
            Bad("없는 카드", s => s.Cards.Add(new SlotCard { Id = "rico_nope" }), "없는 카드");
            Bad("만든 카드", s => s.Cards.Add(new SlotCard { Id = "rico_s1~" }), "만든 카드");
            Bad("신탁 번호 범위", s => s.Cards.First(c => c.Id == "rico_u1").O = 6, "신탁 6");
            Bad("신탁 없는 카드에 신탁", s => s.Cards.First(c => c.Id == "rico_s1").O = 1, "신탁 1");
            Bad("붙을 수 없는 축복", s => s.Cards.First(c => c.Id == "rico_s1").B = "nonsense", "축복");
            Bad("고유 카드 원본 두 장", s => s.Cards.Add(new SlotCard { Id = "rico_u1", O = 2, B = s.Cards.First(c => c.Id == "rico_u1").B, Vals = s.Cards.First(c => c.Id == "rico_u1").Vals }), "원본이 2장");
            Bad("같은 id 다른 신탁", s => s.Cards.Add(new SlotCard { Id = "rico_s1", O = 0, B = "cost" }), "같은 id");
            Bad("기본 카드 복제본", s => s.Cards.Add(new SlotCard { Id = "rico_s1^" }), "복제본");
            Bad("유일(강화) 두 장", s => { s.Cards.Add(new SlotCard { Id = "rico_u3" }); s.Cards.Add(new SlotCard { Id = "rico_u3#2" }); }, "유일");
            Bad("없는 장비", s => s.Gear["무기"] = "eq_nope", "없는 장비");
            Bad("칸이 틀린 장비", s => s.Gear["무기"] = "eq_charm", "칸");
            Bad("성장 음수", s => s.Growth.Atk = -5, "성장 atk");
            Bad("성장 상한", s => s.Growth.Def = HeroSlot.MAX_GROWTH + 1, "성장 def");
            Bad("완주 아님", s => s.From.Result = "lose", "완주");
            Bad("카드 상태를 카드 값으로", s => s.Cards.First(c => c.Id == "rico_u1").Vals["독"] = 3, "카드 상태");
            Bad("모르는 판", s => s.V = 99, "v99");
            Bad("카드 수 상한", s => { for (int i = 0; i < HeroSlot.MAX_CARDS; i++) s.Cards.Add(new SlotCard { Id = "rico_s2" }); }, "상한");
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => HeroSlot.Parse(good.Json().Replace("\"hero\"", "\"atk\":999,\"hero\"")), "모르는 키(숫자 능력치 끼워 넣기)는 읽지 않는다");
            // 지문이 다르면 주의만(옛 데이터로 뜬 슬롯 — 지금 데이터로 해석)
            var old = F(s => s.Rules = "0000000000000000");
            Assert.IsTrue(HeroSlot.Check(d, old).Ok); Assert.IsTrue(HeroSlot.Check(d, old).Warnings.Any(w => w.Contains("규칙 지문")));
        }

        // ── 프론티어 전투 ─────────────────────────────────────────────
        [Test] public void 같은_사도_둘은_한_파티에_못_넣는다()
        {
            var d = K.Sample();
            var a = Slots(ClearRun(d));
            var b = Slots(ClearRun(d));   // 다른 판(같은 내용)이라도 같은 사도
            var two = new List<HeroSlot> { a[0], b[0], a[2] };
            StringAssert.Contains("같은 사도", Frontier.WhyNot(d, two, Season()));
            Assert.Throws<ArgumentException>(() => Frontier.Start(d, two, Season(), 1));
            Assert.IsNull(Frontier.WhyNot(d, a, Season()));
            Assert.IsNotNull(Frontier.WhyNot(d, a.Concat(new[] { b[1] }).ToList(), Season()), "넷은 안 된다");
            var forged = HeroSlot.Parse(a[1].Json()); forged.Cards.Add(new SlotCard { Id = "rico_u2" });
            StringAssert.Contains("슬롯", Frontier.WhyNot(d, new List<HeroSlot> { a[0], forged }, Season()), "규칙 검사를 못 넘은 슬롯은 못 넣는다");
        }

        [Test] public void 시험_덱은_규칙_밖이라도_전투를_지을_수_있으면_열리고_슬롯_검증은_여전히_규칙을_본다()
        {
            var d = K.Sample();
            var a = Slots(ClearRun(d));
            var season = Season();
            // 규칙 밖 — carrot 슬롯에 rico 고유 카드(주인이 다름)
            var forged = HeroSlot.Parse(a[1].Json()); forged.Cards.Add(new SlotCard { Id = "rico_u2" });
            var party = new List<HeroSlot> { a[0], forged, a[2] };
            Assert.IsNotNull(Frontier.WhyNot(d, party, season), "보통은 규칙 검사로 막는다");
            Assert.IsNull(Frontier.WhyNot(d, party, season, strict: false), "시험 덱은 규칙 검사를 건너뛴다");
            Assert.Throws<ArgumentException>(() => Frontier.Start(d, party, season, 3));
            var b = Frontier.Start(d, party, season, 3, strict: false);
            Play(d, b, season);
            var tape = Frontier.Tape(b, season, party);
            Assert.Greater(tape.Score, 0);
            var withSlots = Frontier.Verify(d, FrontierTape.Parse(tape.Json()), season, party.Select(s => HeroSlot.Parse(s.Json())).ToList());
            Assert.IsFalse(withSlots.Ok, "서버 검증(슬롯을 주면)은 규칙을 본다 — 시험 점수는 검증하지 않는다");
            StringAssert.Contains("슬롯으로 열 수 없다", withSlots.Why);
            var replay = Frontier.Verify(d, FrontierTape.Parse(tape.Json()), season);
            Assert.IsTrue(replay.Ok, "테이프 자체는 다시 돌려도 같다 — " + replay.Why);
            Assert.AreEqual(tape.Score, replay.Score);
            // 전투를 지을 수 없는 것은 시험 덱이라도 막는다
            HeroSlot F(Action<HeroSlot> f) { var s = HeroSlot.Parse(a[0].Json()); f(s); return s; }
            StringAssert.Contains("없는 카드", Frontier.WhyNot(d, new List<HeroSlot> { F(s => s.Cards.Add(new SlotCard { Id = "no_such_card" })) }, season, strict: false));
            StringAssert.Contains("칸에 낄 수 없다", Frontier.WhyNot(d, new List<HeroSlot> { F(s => s.Gear["무기"] = "eq_charm") }, season, strict: false));
            StringAssert.Contains("없는 장비", Frontier.WhyNot(d, new List<HeroSlot> { F(s => s.Gear["무기"] = "eq_nope") }, season, strict: false));
            StringAssert.Contains("신탁", Frontier.WhyNot(d, new List<HeroSlot> { F(s => s.Cards[0].O = 99) }, season, strict: false));
            StringAssert.Contains("카드가 없다", Frontier.WhyNot(d, new List<HeroSlot> { F(s => s.Cards.Clear()) }, season, strict: false));
            StringAssert.Contains("같은 사도", Frontier.WhyNot(d, new List<HeroSlot> { a[0], a[0] }, season, strict: false), "같은 사도 둘은 시험 덱도 막는다");
        }

        [Test] public void 슬롯으로_지은_셋업은_사도_몫을_그대로_담고_성격_상성을_끈다()
        {
            var d = K.Sample();
            var slots = Slots(ClearRun(d));
            var st = Frontier.Setup(d, slots, Season(), 42);
            Assert.IsTrue(st.NoNature); Assert.IsNull(st.Glow); Assert.AreEqual(0, st.Gauge); Assert.IsNull(st.PartyHp, "파티 HP 는 가득");
            CollectionAssert.AreEqual(PARTY, st.Party);
            CollectionAssert.AreEqual(new[] { "witch" }, st.Enemies);
            Assert.AreEqual(slots.Sum(s => s.Cards.Count), st.Deck.Count);
            Assert.AreEqual(2, st.Flash["rico_u1"]);
            var rico = Run.StatsOf(d, "eq_knife", "rico") + Run.StatsOf(d, "eq_charm", "rico");
            Assert.AreEqual(rico.Atk, st.Gear["rico"].Atk); Assert.AreEqual(3, st.Growth["rico"].Atk);
            var b = Frontier.Start(d, slots, Season(), 42);
            Assert.AreEqual(PARTY.Sum(k => d.Hero(k).Hp) + rico.Hp, b.Pool.MaxHp, "파티 최대 HP = 사도 HP + 장비 HP");
            Assert.AreEqual(b.Pool.MaxHp, b.Pool.Hp);
            Assert.IsNotNull(b.Input, "입력 테이프가 달린다");
            Assert.AreEqual(Num.Round(d.Enemy("witch").Hp * 30.0), b.Enemies[0].MaxHp, "적 배율은 시즌 것(판의 층 배율과 무관)");
        }

        [Test] public void 프론티어_테이프를_다시_돌리면_점수와_지문이_같고_고친_테이프는_잡는다()
        {
            var d = K.Sample();
            var slots = Slots(ClearRun(d));
            var season = Season();
            int timeEnds = 0;
            for (long seed = 1; seed <= 4; seed++)
            {
                var b = Frontier.Start(d, slots, season, seed);
                Play(d, b, season);
                var over = Frontier.Over(b, season);
                Assert.IsNotNull(over);
                if (over == Frontier.TIME) { timeEnds++; Assert.IsNull(b.Over, "시간 끝은 엔진 밖 — Battle.Over 는 그대로"); Assert.AreEqual(season.Turns + 1, b.Turn); }
                var tape = Frontier.Tape(b, season, slots);
                Assert.AreEqual(Frontier.Score(b), tape.Score); Assert.Greater(tape.Score, 0);
                Assert.GreaterOrEqual(tape.Score, b.Enemies[0].MaxHp - b.Enemies[0].Hp, "점수는 잃은 HP 합(회복해도 줄지 않는다)");
                var json = tape.Json();
                var back = FrontierTape.Parse(json);
                var slotsBack = slots.Select(s => HeroSlot.Parse(s.Json())).ToList();
                var chk = Frontier.Verify(d, back, season, slotsBack);
                Assert.IsTrue(chk.Ok, chk.Why);
                Assert.AreEqual(tape.Score, chk.Score); Assert.AreEqual(tape.Over, chk.Over);
                Assert.IsTrue(Frontier.Verify(d, FrontierTape.Parse(json), season).Ok, "슬롯 없이(테이프 셋업만으로)도");
                // 점수 부풀리기
                var t1 = FrontierTape.Parse(json); t1.Score += 1;
                StringAssert.Contains("점수", Frontier.Verify(d, t1, season, slotsBack).Why);
                // 끝을 바꾸기
                var t2 = FrontierTape.Parse(json); t2.Over = over == "win" ? "time" : "win";
                Assert.IsFalse(Frontier.Verify(d, t2, season, slotsBack).Ok);
                // 시간이 끝난 뒤 한 턴 더
                var t3 = FrontierTape.Parse(json); var in3 = t3.Input(); in3.Ops.Add(new InputOp { K = InputOp.END }); t3.InputJson = JToken.Parse(in3.Json());
                Assert.IsFalse(Frontier.Verify(d, t3, season, slotsBack).Ok, "끝난 뒤의 수");
                // 다른 슬롯(성장을 부풀린 것)으로 낸 테이프
                var forged = slotsBack.Select(s => HeroSlot.Parse(s.Json())).ToList(); forged[0].Growth.Atk += 50;
                Assert.IsFalse(Frontier.Verify(d, FrontierTape.Parse(json), season, forged).Ok, "슬롯이 다르면");
                // 셋업을 몰래 고침(씨앗은 그대로, 파티 HP 를 크게)
                var t4 = FrontierTape.Parse(json); var in4 = t4.Input(); in4.SetupJson["partyMaxHp"] = 99999; in4.SetupJson["partyHp"] = 99999; t4.InputJson = JToken.Parse(in4.Json());
                StringAssert.Contains("셋업", Frontier.Verify(d, t4, season, slotsBack).Why);
                // 다른 시즌(턴 제한)
                Assert.IsFalse(Frontier.Verify(d, FrontierTape.Parse(json), Season(turns: season.Turns + 1), slotsBack).Ok);
            }
            Assert.Greater(timeEnds, 0, "보스를 크게 하면 시간 끝이 나온다");
        }

        [Test] public void 보스를_쓰러뜨리면_그때까지_잃은_HP_가_점수다()
        {
            var d = K.Sample();
            var slots = Slots(ClearRun(d));
            var season = Season(hp: 0.05, turns: 20);
            var b = Frontier.Start(d, slots, season, 5);
            Play(d, b, season);
            Assert.AreEqual("win", Frontier.Over(b, season));
            Assert.AreEqual(b.Enemies[0].MaxHp, Frontier.Score(b), "넘친 피해는 빼고 · 남은 턴 덤 없음");
            Assert.IsTrue(Frontier.Verify(d, FrontierTape.Parse(Frontier.Tape(b, season, slots).Json()), season, slots).Ok);
        }

        // ── 점수 카운터 · 성격 ────────────────────────────────────────
        [Test] public void 적이_잃은_HP_는_막힌_몫과_넘친_피해를_빼고_주인_모를_피해까지_센다()
        {
            var b = K.Fight("dummy");
            var e = K.E(b);
            e.Block = 30;
            b.Hurt(e, 100, new HurtOpts { From = b.Party[0] });
            Assert.AreEqual(70, e.Lost, "방어가 막은 30 은 빼고");
            b.Hurt(e, 50, new HurtOpts { Pure = true, Dot = true });   // 주인 없는 지속 피해
            Assert.AreEqual(120, e.Lost);
            int left = e.Hp;
            b.Hurt(e, left + 500, new HurtOpts { Pure = true });
            Assert.AreEqual(120 + left, e.Lost, "넘친 피해는 빼고");
            Assert.AreEqual(e.MaxHp, e.Lost);
            Assert.AreEqual(0, b.Party[0].Body.Lost, "파티가 받은 피해는 세지 않는다");
        }

        [Test] public void 성격_상성은_꺼도_카드로_거는_약점은_통한다()
        {
            var d = K.Data(
                heroes: "[{id:'n1', name:'성', role:'딜러', hp:600, atk:100, def:20, crit:0, nature:'광기'}]",
                cards: "[{id:'hit_n', name:'치기', hero:'n1', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}," +
                       "{id:'hit_w', name:'약점 치기', hero:'n1', cost:0, type:'공격', tags:['약점'], fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]",
                enemies: "[{id:'weakie', name:'약한 놈', hp:100000, nature:'순수', weak:['광기'], intents:[{t:'jam', v:0, rush:0}]}]");
            int Dmg(bool noNature, string card, bool exposed = false)
            {
                var b = K.Fight(d, new[] { "n1" }, new[] { "weakie" }, st => st.NoNature = noNature);
                if (exposed) b.Enemies[0].Status[R.EXPOSED] = 1;
                K.Hand(b, card);
                int h0 = K.Hp(b);
                K.Play(b, card);
                return h0 - K.Hp(b);
            }
            int plain = Dmg(true, "hit_n");
            Assert.AreEqual(Num.Round(plain * (1 + R.WEAK_DMG)), Dmg(false, "hit_n"), "상성이 켜져 있으면 성격 약점");
            Assert.AreEqual(Num.Round(plain * (1 + R.WEAK_DMG)), Dmg(true, "hit_w"), "NoNature — 「약점」 태그 카드는 약점으로");
            Assert.AreEqual(Num.Round(plain * (1 + R.WEAK_DMG)), Dmg(true, "hit_n", exposed: true), "NoNature — 약점 노출 상태도 약점으로");
            var bb = K.Fight(d, new[] { "n1" }, new[] { "weakie" }, st => st.NoNature = true);
            Assert.IsFalse(bb.IsWeakHit(bb.Party[0], bb.Enemies[0], null), "NoNature — 성격만으로는 약점이 아니다");
        }

        // ── 실제 콘텐츠: 봇 판 → 슬롯 → M.E.O.W ─────────────────────────
        [Test] public void 콘텐츠_봇_판의_슬롯으로_MEOW_와_싸워_다시_돌려도_같다()
        {
            if (!Directory.Exists(CONTENT)) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(CONTENT);
            if (d.Enemy(Frontier.PRESEASON.Boss) == null) Assert.Ignore("콘텐츠에 프리시즌 보스가 없다");
            bool tf = R.TOUGH_FIXED; R.TOUGH_FIXED = true;   // 시험 도구(K)가 끈 것을 판 기본으로
            try
            {
                var pool = new List<HeroSlot>();
                foreach (var j in PartyPick.Jobs(d, 1, 3, PartyPick.Random).Take(6))
                {
                    Run end = null;
                    new RunBot(d).RunFull(j.party, j.seed, new SimOpts { OnEnd = r => end = r });
                    if (end?.S.Done != "clear") continue;
                    foreach (var h in end.S.Party)
                    {
                        var s = HeroSlot.Of(end, h);
                        var c = HeroSlot.Check(d, s);
                        Assert.IsTrue(c.Ok, $"봇 판 슬롯 {h} — {c}");
                        pool.Add(HeroSlot.Parse(s.Json()));
                    }
                    if (pool.Count >= 6) break;
                }
                if (pool.Count < 3) Assert.Ignore("완주한 봇 판이 모자라다");
                var season = Frontier.PRESEASON;
                var party = new List<HeroSlot>();
                foreach (var s in pool) if (party.Count < 3 && party.All(p => p.Hero != s.Hero)) party.Add(s);
                var b = Frontier.Start(d, party, season, 777);
                Play(d, b, season);
                var tape = Frontier.Tape(b, season, party);
                Assert.LessOrEqual(tape.Played, season.Turns);
                Assert.Greater(tape.Score, 0);
                var chk = Frontier.Verify(d, FrontierTape.Parse(tape.Json()), season, party);
                Assert.IsTrue(chk.Ok, chk.Why);
                Assert.AreEqual(tape.Score, chk.Score);
            }
            finally { R.TOUGH_FIXED = tf; }
        }

        /// <summary>
        /// (2026-10-19 사용자 「난이도 살짝 · 패턴 추가 · 강인도 10」) M.E.O.W — 강인도 10(toughOwn) · 「냥냥 프레스」 예고(못 끊는 큰 한 방) · 드론 「출력 증폭」(적 전체 사기) ·
        /// 「심심함」(턴 끝 +4 · 맞으면 -1 — 8 이면 다음 차례 「냥냥 대난동」). 두 턴을 안 때리고 넘기면 심심함이 차 대난동이 온다.
        /// </summary>
        [Test] public void MEOW_강인도_10_새_패턴이_돈다()
        {
            if (!Directory.Exists(CONTENT)) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(CONTENT);
            var boss = d.Enemy(Frontier.PRESEASON.Boss);
            if (boss == null) Assert.Ignore("콘텐츠에 프리시즌 보스가 없다");
            var all = boss.Intents.Concat(boss.Phase.Intents).Concat(boss.Phase2.Intents).ToList();
            Assert.IsTrue(all.Any(x => x.T == "charge" && !x.Brk && x.Next?.T == "attack"), "냥냥 프레스 — 끊김 없는 예고 → 큰 한 방");
            Assert.IsTrue(d.Enemy("frontier_meow_drone").Intents.Any(x => x.T == "buff" && x.All && x.Id == "사기"), "드론 출력 증폭 — 적 전체 사기");
            bool tf = R.TOUGH_FIXED; R.TOUGH_FIXED = true;
            try
            {
                var party = d.Heroes.Keys.OrderBy(k => k, StringComparer.Ordinal).Take(3).ToArray();
                var b = K.Fight(d, party, new[] { boss.Id }, st => st.NoNature = true);
                var e = K.E(b);
                Assert.AreEqual(10, e.ToughMax, "강인도 10(고정 규칙 7 이 아니라)");
                b.EndTurn();
                Assert.AreEqual(4, b.St(e, "심심함"), "턴 끝 +4");
                b.EndTurn();
                Assert.AreEqual(0, b.St(e, "심심함"), "8 에 닿으면 처음으로");
                Assert.IsTrue(e.Intent?.Say == "심심해! 냥냥 대난동" || e.ForceNext?.Say == "심심해! 냥냥 대난동", $"다음 차례 대난동 — 지금 수 {e.Intent?.Say}");
            }
            finally { R.TOUGH_FIXED = tf; }
        }

        /// <summary>
        /// .NET 8 ↔ 유니티(Mono) 교차 — bz frontier --data bolzena-content-v2 --save (아래 폴더) 가 남긴 프론티어 테이프(tapes/)를 여기서 다시 돌려 점수 · 지문 · 끝이 같은가.
        /// 폴더: 환경 변수 BZ_FRONTIER, 없으면 bolzena-core-test/frontier. 없으면 건너뛴다.
        /// </summary>
        [Test] public void 닷넷이_남긴_프론티어_테이프를_유니티에서_다시_돌려도_같다()
        {
            string dir = Environment.GetEnvironmentVariable("BZ_FRONTIER");
            if (string.IsNullOrEmpty(dir)) dir = new[] { "frontier", "C:/projects/bolzena-core-test/frontier" }.FirstOrDefault(Directory.Exists);
            if (dir == null || !Directory.Exists(Path.Combine(dir, "tapes"))) Assert.Ignore("교차 테이프 폴더가 없다(bz frontier --save)");
            if (!Directory.Exists(CONTENT)) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(CONTENT);
            var files = Directory.GetFiles(Path.Combine(dir, "tapes"), "*.json").OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (files.Count == 0) Assert.Ignore("테이프가 없다");
            bool tf = R.TOUGH_FIXED; R.TOUGH_FIXED = true;
            try
            {
                var bad = new List<string>();
                foreach (var f in files)
                {
                    var tape = FrontierTape.Parse(File.ReadAllText(f));
                    var input = tape.Input();
                    Assert.AreEqual(input.Rules, Fingerprint.Rules(), "규칙 지문");
                    Assert.AreEqual(input.Content, d.ContentHash, "콘텐츠 지문 — 같은 콘텐츠로 남긴 테이프");
                    var chk = Frontier.Verify(d, tape, Frontier.PRESEASON);
                    if (!chk.Ok) bad.Add($"{Path.GetFileName(f)} — {chk.Why}");
                }
                Assert.AreEqual(0, bad.Count, $"테이프 {files.Count} 가운데 다름 {bad.Count}\n" + string.Join("\n", bad.Take(20)));
                UnityEngine.Debug.Log($"[FrontierTests] .NET 8 프론티어 테이프 {files.Count}개를 유니티에서 다시 돌림 — 모두 같음");
            }
            finally { R.TOUGH_FIXED = tf; }
        }
    }
}
