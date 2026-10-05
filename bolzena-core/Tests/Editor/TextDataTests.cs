using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>글 생성기(데이터 → 카드 글) · 검사기 · 샘플 콘텐츠.</summary>
    public class TextDataTests
    {
        static readonly CardText T = new CardText(K.Data(heroes: "[{id:'k', name:'표식이', role:'딜러', row:'back', hp:500, atk:100, def:20, keyword:{name:'시계', carrier:'enemy', cap:3, decay:1}}]"));
        static Fx F(string json) => GameData.FromJson<Fx>(json.Replace('\'', '"'));
        static List<Fx> L(params string[] js) => js.Select(F).ToList();

        [TestCase("{'k':'dmg','ratio':1.2,'target':'oneEnemy'}", "적 1명에게 공격력 120% 피해")]
        [TestCase("{'k':'dmg','ratio':0.45,'hits':3,'target':'oneEnemy'}", "적 1명에게 3회 × 공격력 45% 피해")]
        [TestCase("{'k':'dmg','ratio':0.35,'hits':4,'target':'randomEnemy'}", "무작위 적 4회 × 공격력 35% 피해")]
        [TestCase("{'k':'dmg','ratio':0.7,'target':'allEnemies'}", "적 전체에 공격력 70% 피해")]
        [TestCase("{'k':'dmg','ratio':0.8,'base':'def','target':'oneEnemy'}", "적 1명에게 방어 기반 피해 80%")]
        [TestCase("{'k':'dmg','ratio':0.9,'fixed':true,'target':'oneEnemy'}", "적 1명에게 공격력 90% 고정 피해")]
        [TestCase("{'k':'block','ratio':2.0}", "방어력 200% 방어")]
        [TestCase("{'k':'shield','ratio':1.5,'target':'party'}", "파티 방어력 150% 실드")]
        [TestCase("{'k':'shield','ratio':1.3,'fixed':true,'target':'party'}", "파티 방어력 130% 고정 실드")]
        [TestCase("{'k':'heal','ratio':2.1}", "파티 HP 회복(방어력 210%)")]
        [TestCase("{'k':'draw','v':2}", "드로우 2")]
        [TestCase("{'k':'ap','v':1}", "AP +1")]
        [TestCase("{'k':'gauge','v':30}", "고학년 게이지 +30%")]
        [TestCase("{'k':'nextCheaper','v':1}", "다음 카드 코스트 -1")]
        [TestCase("{'k':'status','id':'기절','v':1,'target':'oneEnemy'}", "적 1명 기절")]
        [TestCase("{'k':'rushDown','v':1,'target':'allEnemies'}", "적 전체 즉시 행동 1장 늦춤")]
        [TestCase("{'k':'cleanse','v':1}", "파티 디버프 1개 해제")]
        [TestCase("{'k':'payHp','v':90}", "파티 HP 90 소모")]
        [TestCase("{'k':'status','id':'사기','v':1}", "사기 1")]
        [TestCase("{'k':'status','id':'잔광','v':1}", "잔광 1")]
        [TestCase("{'k':'status','id':'리듬','v':2}", "리듬 2")]
        [TestCase("{'k':'status','id':'취약','v':2,'target':'oneEnemy'}", "적 1명 취약 2")]
        [TestCase("{'k':'status','id':'잔불','v':1,'target':'allEnemies'}", "적 전체 잔불 1")]
        [TestCase("{'k':'atkMod','v':0.1,'run':true,'target':'self'}", "전투 내내 자신의 공격력 +10%")]
        [TestCase("{'k':'atkMod','v':0.3,'run':true,'target':'oneAlly'}", "전투 내내 아군 1명의 공격력 +30%")]
        [TestCase("{'k':'takenMod','v':0.2,'target':'oneEnemy','turns':2}", "2턴간 적 1명의 받는 피해 +20%")]
        [TestCase("{'k':'hasten','v':1}", "재촉 1")]
        [TestCase("{'k':'flip'}", "전환")]
        [TestCase("{'k':'spendRhythm','all':true}", "리듬 전부 소모")]
        [TestCase("{'k':'stack','id':'시계','v':2}", "적 1명에게 「시계」 +2")]
        public void 효과_한_줄(string fx, string want) => Assert.AreEqual(want, T.Fx(L(fx)));

        [Test] public void 태그와_조건은_문장으로()
        {
            Assert.AreEqual("연계. 적 1명에게 공격력 80% 피해", T.Compose(new[] { "연계" }, L("{'k':'dmg','ratio':0.8,'target':'oneEnemy'}")));
            Assert.AreEqual("소멸 2. 적 1명에게 공격력 160% 피해", T.Compose(new[] { "소멸 2" }, L("{'k':'dmg','ratio':1.6,'target':'oneEnemy'}")));
            Assert.AreEqual("적 1명에게 공격력 120% 피해. 파괴: AP +1", T.Fx(L("{'k':'dmg','ratio':1.2,'target':'oneEnemy'}", "{'k':'ifBroken'}", "{'k':'ap','v':1}")));
            Assert.AreEqual("드로우 1. 조율: AP +2", T.Fx(L("{'k':'draw','v':1}", "{'k':'ifTune'}", "{'k':'ap','v':2}")));
            Assert.AreEqual("방어력 150% 방어. 영감: 사기 1", T.Fx(L("{'k':'block','ratio':1.5}", "{'k':'when','on':'draw'}", "{'k':'status','id':'사기','v':1}")));
            Assert.AreEqual("방어력 150% 방어. 앞이 공격: 적 1명에게 공격력 60% 피해", T.Fx(L("{'k':'block','ratio':1.5}", "{'k':'ifPrev','type':'공격'}", "{'k':'dmg','ratio':0.6,'target':'oneEnemy'}")));
            Assert.AreEqual("리듬 1개당 적 1명에게 공격력 30% 피해, 리듬 전부 소모", T.Fx(L("{'k':'perRhythm'}", "{'k':'dmg','ratio':0.3,'target':'oneEnemy'}", "{'k':'spendRhythm','all':true}")));
            Assert.AreEqual("드로우 1. 리듬이 3 이상이면: AP +1", T.Fx(L("{'k':'draw','v':1}", "{'k':'ifRhythm','n':3}", "{'k':'ap','v':1}")));
            Assert.AreEqual("사용 불가. 증발.", T.Compose(new[] { "사용 불가", "증발" }, new List<Fx>()));
            Assert.AreEqual("소멸. 턴 끝에 손에 있으면: 파티 HP 60 소모", T.Compose(new[] { "소멸" }, L("{'k':'when','on':'handEnd'}", "{'k':'payHp','v':60}")));
            Assert.AreEqual("적 1명에게 공격력 100% 피해, 강인도 피해 2", T.Fx(L("{'k':'dmg','ratio':1.0,'target':'oneEnemy'}", "{'k':'tough','v':2,'target':'oneEnemy'}")));
        }

        [Test] public void 패시브와_키워드()
        {
            var r = new PassiveRule { Name = "맞불", When = new When { On = "hurt" }, Limit = new Limit { Per = "turn", N = 1 }, Fx = L("{'k':'status','id':'반격','v':1}") };
            Assert.AreEqual("맞불: 피해를 받으면 반격 1 (턴당 1회)", T.Passives(new List<PassiveRule> { r }));
            var r2 = new PassiveRule { Name = "풀코스", When = new When { On = "play", Who = "any", Seq = new List<string> { "공격", "스킬", "강화" } }, Fx = L("{'k':'shield','ratio':1.5,'target':'party'}") };
            Assert.AreEqual("풀코스: 이번 턴 공격 · 스킬 · 강화 카드를 차례로 내면 파티 방어력 150% 실드", T.Passives(new List<PassiveRule> { r2 }));
            var r3 = new PassiveRule { Name = "버팀", When = new When { On = "turnStart" }, Conds = new List<Cond> { new Cond { C = "status", Id = "불굴", N = 2 } }, Fx = L("{'k':'block','ratio':1.0}") };
            Assert.AreEqual("버팀: 턴 시작 시 파티 불굴이 2 이상이면 방어력 100% 방어", T.Passives(new List<PassiveRule> { r3 }));
            Assert.AreEqual("박자: 리듬이 5가 되면 드로우 1", T.Passives(new List<PassiveRule> { new PassiveRule { Name = "박자", When = new When { On = "rhythm", N = 5 }, Fx = L("{'k':'draw','v':1}") } }));
            var kw = new KeywordDef { Name = "이온화", Desc = "적에게 쌓는 전하", Carrier = "enemy", Cap = 5, Decay = 1, Per = new List<PerStat> { new PerStat { Stat = "taken", V = 0.05 } } };
            Assert.AreEqual("적에게 쌓는 전하. 적에게 거는 표식이다. 최대 5. 1개당 받는 피해 +5%. 적의 차례가 끝나면 1 감소.", T.Keyword(kw));
        }

        [Test] public void 적의_수와_이벤트_결과()
        {
            Assert.AreEqual("공격 110", T.Intent(new Intent { T = "attack", V = 110 }));
            Assert.AreEqual("40 × 3회 · 한 대마다 고통 1", T.Intent(new Intent { T = "multi", V = 40, N = 3, Id = "고통" }));
            Assert.AreEqual("힘을 모은다 — 다음 턴 공격 300 · 격파하면 끊긴다", T.Intent(new Intent { T = "charge", Brk = true, Next = new Intent { T = "attack", V = 300 } }));
            var outs = GameData.FromJson<List<Outcome>>("[{'k':'hp','v':-0.1},{'k':'flash'},{'k':'shin','v':0.2},{'k':'next','next':{'ap':1}}]".Replace('\'', '"'));
            Assert.AreEqual("HP -10% · 신탁 1 · 축복 20% · 다음 전투: 첫 턴 AP +1", T.Outcomes(outs));
        }

        [Test] public void 조사()
        {
            Assert.AreEqual("「시계」가", Ko.J("「시계」", "이가"));
            Assert.AreEqual("「수은」이", Ko.J("「수은」", "이가"));
            Assert.AreEqual("리듬이", Ko.J("리듬", "이가"));
            Assert.AreEqual("파티를", Ko.J("파티", "을를"));
        }

        // ── 검사기 ──
        [Test] public void 샘플_콘텐츠는_검사를_통과한다()
        {
            var d = K.Sample();
            var v = Validator.Check(d);
            Assert.IsTrue(v.Ok, v.ToString());
            Assert.AreEqual(4, d.Heroes.Count);
            Assert.AreEqual(1, d.Villages.Count);
        }

        [Test] public void 검사기는_잘못_쓴_것을_잡는다()
        {
            var d = K.Data(cards: @"[{id:'bad', name:'잘못', hero:'nobody', cost:1, type:'공격', tags:['보존', '없는태그', '보존 2'],
  fx:[{k:'dmgg', ratio:1}, {k:'status', id:'중독', v:1}, {k:'stack', id:'없는키워드', v:1}, {k:'perStack', id:'없는키워드'}, {k:'draw', v:1}, {k:'make', id:'없는카드'}],
  oracles:[{name:'하나', fx:[]}]}]",
                enemies: "[{id:'e', name:'적', hp:0, row:'middle', intents:[{t:'smash', v:1}, {t:'addCard', id:'nothing'}, {t:'charge'}]}]",
                villages: "[{id:'v', name:'마을', floors:[{name:'1', land:'땅', pools:[[['ghost']]], boss:[]}]}]",
                events: "[{id:'x', name:'이벤트', options:[{label:'뽑기', gamble:[{p:0.3, out:[{k:'gold', v:1}]}]}, {label:'하자', out:[{k:'teleport'}]}]}]",
                equips: "[{id:'q', name:'장비', grade:'신화', slot:'모자'}]");
            var v = Validator.Check(d);
            string all = string.Join("\n", v.Errors);
            foreach (var want in new[] { "없는 사도 nobody", "모르는 태그 「없는태그」", "수가 붙는 태그는", "모르는 효과 조각", "모르는 상태 중독", "사도 키워드가 아니다", "1개당」 바로 뒤", "만들 카드가 없다",
                "신탁은 다섯", "hp 가 없다", "row 는 front", "모르는 수 smash", "없는 카드 nothing", "charge 에 next", "층은 둘", "pools 는 세기 셋", "없는 적 ghost", "적이 없다",
                "p 합이 1 이 아니다", "모르는 결과 teleport", "slot 은", "grade 는" })
                StringAssert.Contains(want, all);
        }

        [Test] public void 모르는_JSON_키는_읽을_때_잡는다()
        {
            Assert.Throws<System.FormatException>(() => K.Data(cards: "[{id:'x', name:'x', cost:1, type:'스킬', fx:[], ratoi:1}]"));
        }

        [Test] public void 샘플_카드_글()
        {
            var d = K.Sample(); var t = new CardText(d);
            Assert.AreEqual("「코스」 1개당 적 1명에게 방어 기반 피해 60%, 「코스」 전부 소모, 적 1명 강인도 피해 1", t.Card(d.Cards["rico_u1"]));
            Assert.AreEqual("연계. 적 1명에게 방어 기반 피해 60%. 앞이 스킬: 「코스」 +1", t.Card(d.Cards["rico_u4"]));
            Assert.AreEqual("코스트 1. 「코스」 1개당 적 1명에게 방어 기반 피해 50%, 「코스」 전부 소모, 적 1명 강인도 피해 1", t.Oracle(d.Cards["rico_u1"], d.Cards["rico_u1"].Oracles[0]));
            Assert.AreEqual("피해 ×1.3.", t.Bless(d.Cards["rico_u1"].Blesses[0]));
            StringAssert.Contains("「마탄」이 5개가 되면: 「마탄」 전부 소모, 「진혼의 탄환」 1장 생성", t.Keyword(d.Heroes["sion"].Keyword));
            Assert.AreEqual("적", d.View("sion_u4").Target);
            var sheet = t.Hero(d.Heroes["rico"]);
            StringAssert.StartsWith("리코타 — 탱커 · 냉정 · 전열 · 박자형", sheet);
            StringAssert.Contains("시작 「도마 찍기」 [1] 적 1명에게 방어 기반 피해 70%, 「코스」 +1 ×2", sheet);
            StringAssert.Contains("고유 「풀코스 서빙」 [2] 공격", sheet);
            Assert.AreEqual("없음", d.View("rico_s2").Target);
        }
    }
}
