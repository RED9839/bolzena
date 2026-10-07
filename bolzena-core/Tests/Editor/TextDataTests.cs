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

        [TestCase("{'k':'dmg','ratio':1.2,'target':'oneEnemy'}", "적 1명에게 피해 120%")]
        [TestCase("{'k':'dmg','ratio':0.45,'hits':3,'target':'oneEnemy'}", "적 1명에게 3회 × 피해 45%")]
        [TestCase("{'k':'dmg','ratio':0.35,'hits':4,'target':'randomEnemy'}", "무작위 적 4회 × 피해 35%")]
        [TestCase("{'k':'dmg','ratio':0.7,'target':'allEnemies'}", "적 전체에 피해 70%")]
        [TestCase("{'k':'dmg','ratio':0.8,'base':'def','target':'oneEnemy'}", "적 1명에게 방어 기반 피해 80%")]
        [TestCase("{'k':'dmg','ratio':0.9,'fixed':true,'target':'oneEnemy'}", "적 1명에게 고정 피해 90%")]
        [TestCase("{'k':'block','ratio':2.0}", "방어 200%")]
        [TestCase("{'k':'shield','ratio':1.5,'target':'party'}", "실드 150%")]
        [TestCase("{'k':'shield','ratio':1.3,'fixed':true,'target':'party'}", "고정 실드 130%")]
        [TestCase("{'k':'heal','ratio':2.1}", "HP 회복(방어력 210%)")]
        [TestCase("{'k':'draw','v':2}", "드로우 2")]
        [TestCase("{'k':'ap','v':1}", "AP +1")]
        [TestCase("{'k':'gauge','v':30}", "고학년 게이지 +30%")]
        [TestCase("{'k':'nextCheaper','v':1}", "다음 카드 비용 -1")]
        [TestCase("{'k':'status','id':'기절','v':1,'target':'oneEnemy'}", "적 1명 기절")]
        [TestCase("{'k':'rushDown','v':1,'target':'allEnemies'}", "적 전체 즉시 행동 1장 늦춤")]
        [TestCase("{'k':'cleanse','v':1}", "파티 디버프 1개 해제")]
        [TestCase("{'k':'payHp','v':90}", "파티 HP 90 소모")]
        [TestCase("{'k':'status','id':'사기','v':1}", "사기 1")]
        [TestCase("{'k':'status','id':'잔광','v':1}", "잔광 1")]
        [TestCase("{'k':'status','id':'리듬','v':2}", "리듬 2")]
        [TestCase("{'k':'status','id':'취약','v':2,'target':'oneEnemy'}", "적 1명 취약 2")]
        [TestCase("{'k':'status','id':'잔불','v':1,'target':'allEnemies'}", "적 전체 잔불 1")]
        [TestCase("{'k':'atkMod','v':0.1,'run':true,'target':'self'}", "이 전투 동안 자신의 공격력 +10%")]
        [TestCase("{'k':'atkMod','v':0.3,'run':true,'target':'oneAlly'}", "이 전투 동안 아군 1명의 공격력 +30%")]
        [TestCase("{'k':'takenMod','v':0.2,'target':'oneEnemy','turns':2}", "2턴간 적 1명의 받는 피해 +20%")]
        [TestCase("{'k':'hasten','v':1}", "재촉 1")]
        [TestCase("{'k':'flip'}", "전환")]
        [TestCase("{'k':'spendRhythm','all':true}", "리듬 전부 소모")]
        [TestCase("{'k':'stack','id':'시계','v':2}", "적 1명에게 「시계」 2")]
        public void 효과_한_줄(string fx, string want) => Assert.AreEqual(want, T.Fx(L(fx)));

        [Test] public void 태그와_조건은_문장으로()
        {
            Assert.AreEqual("연계. 적 1명에게 피해 80%", T.Compose(new[] { "연계" }, L("{'k':'dmg','ratio':0.8,'target':'oneEnemy'}")));
            Assert.AreEqual("소멸 2. 적 1명에게 피해 160%", T.Compose(new[] { "소멸 2" }, L("{'k':'dmg','ratio':1.6,'target':'oneEnemy'}")));
            Assert.AreEqual("적 1명에게 피해 120%. 파괴: AP +1", T.Fx(L("{'k':'dmg','ratio':1.2,'target':'oneEnemy'}", "{'k':'ifBroken'}", "{'k':'ap','v':1}")));
            Assert.AreEqual("드로우 1. 조율: AP +2", T.Fx(L("{'k':'draw','v':1}", "{'k':'ifTune'}", "{'k':'ap','v':2}")));
            Assert.AreEqual("방어 150%. 영감: 사기 1", T.Fx(L("{'k':'block','ratio':1.5}", "{'k':'when','on':'draw'}", "{'k':'status','id':'사기','v':1}")));
            Assert.AreEqual("방어 150%. 앞이 공격: 적 1명에게 피해 60%", T.Fx(L("{'k':'block','ratio':1.5}", "{'k':'ifPrev','type':'공격'}", "{'k':'dmg','ratio':0.6,'target':'oneEnemy'}")));
            Assert.AreEqual("리듬 1개당 적 1명에게 피해 30%, 리듬 전부 소모", T.Fx(L("{'k':'perRhythm'}", "{'k':'dmg','ratio':0.3,'target':'oneEnemy'}", "{'k':'spendRhythm','all':true}")));
            Assert.AreEqual("드로우 1. 리듬이 3 이상이면 AP +1", T.Fx(L("{'k':'draw','v':1}", "{'k':'ifRhythm','n':3}", "{'k':'ap','v':1}")));
            Assert.AreEqual("사용 불가. 증발.", T.Compose(new[] { "사용 불가", "증발" }, new List<Fx>()));
            Assert.AreEqual("소멸. 턴 끝에 손에 있으면 파티 HP 60 소모", T.Compose(new[] { "소멸" }, L("{'k':'when','on':'handEnd'}", "{'k':'payHp','v':60}")));
            Assert.AreEqual("적 1명에게 피해 100%, 강인도 피해 2", T.Fx(L("{'k':'dmg','ratio':1.0,'target':'oneEnemy'}", "{'k':'tough','v':2,'target':'oneEnemy'}")));
        }

        /// <summary>
        /// 카드 글 가독성(2026-10-07 사용자 「찢기의 『손패가 없으면』 이 낸 뒤인지 헷갈린다」) — 문장마다 줄을 바꾸고, 손패 조건은 「낸 뒤」,
        /// 두 번째부터의 「적 1명」 은 「그 적」, 기본 효과에 얹히는 같은 갈래 효과는 「더」, 말로 쓴 조건은 「→」.
        /// </summary>
        [Test] public void 카드_글_줄_나눔과_조건_시점()
        {
            CardDef C(string json) => GameData.FromJson<CardDef>(json.Replace('\'', '"'));
            var rip = C("{'id':'rip','name':'찢기','cost':2,'type':'공격','fx':[{'k':'dmg','ratio':2.2,'target':'oneEnemy'},{'k':'ifHand','n':2},{'k':'dmg','ratio':0.9,'target':'oneEnemy'},{'k':'ifHand','n':0},{'k':'dmg','ratio':1.4,'target':'oneEnemy'}]}");
            Assert.AreEqual("적 1명에게 피해 220%\n낸 뒤 손패가 2장 이하면 피해 90% 더\n낸 뒤 손패가 없으면 피해 140% 더", T.Card(rip));
            var nth = C("{'id':'n1','name':'첫','cost':0,'type':'스킬','tags':['소멸'],'fx':[{'k':'draw','v':1},{'k':'ifNth','n':1},{'k':'ap','v':1}]}");
            Assert.AreEqual("소멸.\n드로우 1\n이번 턴 첫 카드면 AP +1", T.Card(nth));
            var foe = C("{'id':'f1','name':'격파','cost':1,'type':'공격','fx':[{'k':'dmg','ratio':1.0,'target':'oneEnemy'},{'k':'ifFoe','id':'broken'},{'k':'status','id':'취약','v':1,'target':'oneEnemy'}]}");
            Assert.AreEqual("적 1명에게 피해 100%\n격파 상태면 그 적 취약 1", T.Card(foe));
            // 카드 글이 아닌 곳(Fx 바로)은 한 줄 그대로 — 패시브 · 장비 · 로그
            Assert.AreEqual("드로우 1. 낸 뒤 손패가 없으면 AP +1", T.Fx(L("{'k':'draw','v':1}", "{'k':'ifHand','n':0}", "{'k':'ap','v':1}")));
        }

        /// <summary>
        /// 2026-10-07 사용자 「카드 설명에 화살표랑 + 없애 줘」 · 「강인도가 없다면 같은 건 카제나 키워드로」 —
        /// 조건과 결과는 띄어쓰기 하나(머리는 HeadEnd 로 찾는다), 쌓는 수는 + 없이, 능력치 · AP 증감은 + 그대로, 피해 기반 회복은 키워드로.
        /// </summary>
        [Test] public void 카드_글_화살표_더하기_없이()
        {
            CardDef C(string json) => GameData.FromJson<CardDef>(json.Replace('\'', '"'));
            var rip = C("{'id':'rip2','name':'찢기','cost':2,'type':'공격','fx':[{'k':'dmg','ratio':2.2,'target':'oneEnemy'},{'k':'ifHand','n':0},{'k':'dmg','ratio':1.4,'target':'oneEnemy'}]}");
            string line = T.Card(rip).Split('\n')[1];
            Assert.AreEqual("낸 뒤 손패가 없으면 피해 140% 더", line);
            Assert.AreEqual("낸 뒤 손패가 없으면".Length, CardText.HeadEnd(line), "조건 머리 끝 = 결과 앞 띄어쓰기");
            Assert.AreEqual(-1, CardText.HeadEnd("적 1명에게 피해 220%"), "조건 없는 줄");
            var st = C("{'id':'st2','name':'쌓기','cost':1,'type':'스킬','fx':[{'k':'stack','id':'홀로그램','v':2},{'k':'ap','v':1},{'k':'atkMod','v':0.2},{'k':'drain','ratio':0.3}]}");
            Assert.AreEqual("「홀로그램」 2, AP +1, 이번 턴 자신의 공격력 +20%, 피해 기반 회복 30%", T.Card(st));
            // 강화 카드 — 세기형 버프는 「이 전투 동안」 없이(원래 전투 끝까지), 「매 턴 …」 규칙도 머리 없이
            var pw = C("{'id':'pw2','name':'강화','cost':1,'type':'강화','fx':[{'k':'status','id':'사기','v':1},{'k':'power','rules':[{'when':{'on':'turnStart'},'fx':[{'k':'stack','id':'근력','v':1}]}]}]}");
            string pt = T.Card(pw);
            Assert.AreEqual("사기 1\n매 턴 시작 시 「근력」 1", pt);
            Assert.AreEqual("매 턴 시작 시".Length, CardText.HeadEnd(pt.Split('\n')[1]));
            CollectionAssert.Contains(T.Chips(new CardView("f2", C("{'id':'f2','name':'격파','cost':1,'type':'공격','fx':[{'k':'dmg','ratio':1.0,'target':'oneEnemy'},{'k':'ifFoe','id':'broken'},{'k':'ap','v':1}]}"), null, 0)), "격파 상태");
            Assert.IsNotNull(CardText.Tip("격파 상태"));
        }

        [Test] public void 패시브와_키워드()
        {
            var r = new PassiveRule { Name = "맞불", When = new When { On = "hurt" }, Limit = new Limit { Per = "turn", N = 1 }, Fx = L("{'k':'status','id':'반격','v':1}") };
            Assert.AreEqual("맞불: 파티가 피해를 받으면 반격 1 (턴당 1회)", T.Passives(new List<PassiveRule> { r }));
            var r2 = new PassiveRule { Name = "풀코스", When = new When { On = "play", Who = "any", Seq = new List<string> { "공격", "스킬", "강화" } }, Fx = L("{'k':'shield','ratio':1.5,'target':'party'}") };
            Assert.AreEqual("풀코스: 이번 턴 공격 · 스킬 · 강화 카드를 차례로 내면 실드 150%", T.Passives(new List<PassiveRule> { r2 }));
            var r3 = new PassiveRule { Name = "버팀", When = new When { On = "turnStart" }, Conds = new List<Cond> { new Cond { C = "status", Id = "불굴", N = 2 } }, Fx = L("{'k':'block','ratio':1.0}") };
            Assert.AreEqual("버팀: 턴 시작 시 파티 불굴이 2 이상이면 방어 100%", T.Passives(new List<PassiveRule> { r3 }));
            Assert.AreEqual("박자: 리듬이 5가 되면 드로우 1", T.Passives(new List<PassiveRule> { new PassiveRule { Name = "박자", When = new When { On = "rhythm", N = 5 }, Fx = L("{'k':'draw','v':1}") } }));
            var kw = new KeywordDef { Name = "이온화", Desc = "적에게 쌓는 전하", Carrier = "enemy", Cap = 5, Decay = 1, Per = new List<PerStat> { new PerStat { Stat = "taken", V = 0.05 } } };
            Assert.AreEqual("적에게 쌓는 전하\n· 적에게 쌓임 · 적마다 최대 5\n· 「이온화」 1당 그 적의 받는 피해 +5%\n· 적의 차례가 끝나면 −1", T.Keyword(kw));
            // 「자세히」 없앰(Docs/설명글.md) — Short 와 Detail 은 같은 글(수치 · 제한까지 다)
            Assert.AreEqual(T.Keyword(kw), T.Short(kw));
            Assert.AreEqual(T.Keyword(kw), T.Detail(kw));
            var r4 = new PassiveRule { Name = "덫", When = new When { On = "hurt" }, Limit = new Limit { Per = "turn", N = 2 }, Fx = L("{'k':'spend','id':'덫','v':1}", "{'k':'dmg','ratio':1.1,'target':'oneEnemy'}") };
            Assert.AreEqual("파티가 피해를 받으면 「덫」 1 소모, 때린 적에게 피해 110% (턴당 2회)", T.Detail(r4));
            Assert.AreEqual(T.Detail(r4), T.Short(r4));
        }

        /// <summary>사도 상세 칸(Docs/설명글.md §4) — 고학년 · 고유 효과 · 패시브 세 칸 따로, 계기의 적 · 대상 낱말이 붙는다.</summary>
        [Test] public void 고유_효과_한_칸()
        {
            var d = K.Data(heroes: "[{id:'g', name:'깃발이', role:'서포터', row:'front', hp:500, atk:80, def:60, keyword:{name:'깃발', desc:'공격으로 꽂는 깃발', carrier:'enemy', cap:3, weakens:true}, " +
                "passives:[{name:'찜', when:{on:'play', type:'공격'}, limit:{per:'turn', n:1}, fx:[{k:'stack', id:'깃발', v:1}]}, " +
                "{name:'찜', when:{on:'kill'}, conds:[{c:'stack', id:'깃발'}], limit:{per:'turn', n:1}, fx:[{k:'draw', v:1}, {k:'ap', v:1}]}, " +
                "{name:'따로', when:{on:'turnStart'}, fx:[{k:'status', id:'사기', v:1}]}], ult:{name:'한 방', cost:200, fx:[{k:'dmg', ratio:2.0, target:'allEnemies'}]}}]");
            var t = new CardText(d);
            var tr = t.Traits(d.Hero("g"));
            // 세 칸 따로(2026-10-06 사용자 「패시브 칸 따로」) — 고유 효과 = 자원 · 표식 자체의 규칙, 패시브 = passives 전부(같은 이름은 한 칸)
            Assert.AreEqual(new[] { "고학년", "고유 효과", "패시브", "패시브" }, tr.Select(x => x.Kind).ToArray());
            Assert.AreEqual("게이지 200%", tr[0].Sub);
            Assert.AreEqual("공격으로 꽂는 깃발", tr[1].Sub);
            Assert.AreEqual("· 적에게 쌓임 · 적마다 최대 3\n· 「깃발」이 있는 적은 아군 카드에 약점으로 맞음 — 카드 1장이 칠 때마다 −1", tr[1].Body);
            Assert.AreEqual("찜", tr[2].Name);
            Assert.AreEqual("· 자신의 공격 카드를 낼 때마다 그 적에게 「깃발」 1 (턴당 1회)\n· 「깃발」이 있는 적이 쓰러지면 드로우 1, AP +1 (턴당 1회)", tr[2].Body);
            Assert.AreEqual("따로", tr[3].Name);
            Assert.AreEqual("턴 시작 시 사기 1", tr[3].Body);
            // 패시브가 없는 사도 — 패시브 칸에 「없음」
            var d2 = K.Data(heroes: "[{id:'n', name:'무패', role:'딜러', row:'back', hp:500, atk:80, def:60, keyword:{name:'자원', cap:2}}]");
            var tr2 = new CardText(d2).Traits(d2.Hero("n"));
            Assert.AreEqual(CardText.NoPassive, tr2.Last().Name);
            Assert.AreEqual("패시브", tr2.Last().Kind);
            // 낱말 판(Tips) 의 고유 효과도 같은 본문
            StringAssert.Contains(tr[1].Body, t.Tips()["깃발"]);
            // 엔진 키워드 글의 수치는 Rules.cs 값
            StringAssert.Contains($"+{Num.Round(R.SV("사기") * 100)}%p", CardText.Tip("사기"));
        }

        [Test] public void 적의_수와_이벤트_결과()
        {
            Assert.AreEqual("공격 110", T.Intent(new Intent { T = "attack", V = 110 }));
            Assert.AreEqual("40 × 3회 · 한 대마다 고통 1", T.Intent(new Intent { T = "multi", V = 40, N = 3, Id = "고통" }));
            Assert.AreEqual("힘 모으기 — 다음 턴 공격 300 · 격파하면 끊김", T.Intent(new Intent { T = "charge", Brk = true, Next = new Intent { T = "attack", V = 300 } }));
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
            Assert.AreEqual("「코스」 1개당 적 1명에게 방어 기반 피해 60%, 「코스」 전부 소모, 그 적 강인도 피해 1", t.Card(d.Cards["rico_u1"]));
            Assert.AreEqual("연계.\n적 1명에게 방어 기반 피해 60%\n앞이 스킬: 「코스」 1", t.Card(d.Cards["rico_u4"]));
            Assert.AreEqual("비용 1.\n「코스」 1개당 적 1명에게 방어 기반 피해 50%, 「코스」 전부 소모, 그 적 강인도 피해 1", t.Oracle(d.Cards["rico_u1"], d.Cards["rico_u1"].Oracles[0]));
            Assert.AreEqual("피해 ×1.3.", t.Bless(d.Cards["rico_u1"].Blesses[0]));
            StringAssert.Contains("5개가 되면 모두 써서 「진혼의 탄환」 1장 생성", t.Keyword(d.Heroes["sion"].Keyword));
            Assert.AreEqual("적", d.View("sion_u4").Target);
            var sheet = t.Hero(d.Heroes["rico"]);
            StringAssert.StartsWith("리코타 — 탱커 · 냉정 · 박자형", sheet);
            StringAssert.Contains("시작 「도마 찍기」 [1] 적 1명에게 방어 기반 피해 70%, 「코스」 1 ×2", sheet);
            StringAssert.Contains("고유 「풀코스 서빙」 [2] 공격", sheet);
            Assert.AreEqual("없음", d.View("rico_s2").Target);
        }
    }
}
