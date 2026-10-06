using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>변신(form) — 들어감 · 지속 · 카드 바꾸기 · 덤 · 패시브 · 풀림 · 다시 쓰기 · 저장 왕복 · 글 · 검사. 사도 f(공 100 방 50).</summary>
    public class FormTests
    {
        const string HEROES = @"[
 {id:'f', name:'변', role:'딜러', row:'front', hp:1000, atk:100, def:50, crit:0, keyword:{name:'분', cap:5},
  passives:[{name:'원래', when:{on:'play'}, fx:[{k:'gauge', v:1}]}],
  ult:{name:'변신!', cost:100, fx:[{k:'form', id:'fm'}]},
  forms:[
   {id:'fm', name:'폭주', turns:2, mods:{atk:0.5}, cards:{hit_f0:'hit_f1'}, skin:'rage', anim:'rage_',
    off:[{k:'gauge', v:7}]},
   {id:'fb', name:'맨손', turns:0, bonus:[{type:'공격', ratio:1.5}, {card:'end_f', fx:[{k:'formEnd'}]}],
    passives:[{name:'덧', when:{on:'play'}, fx:[{k:'gauge', v:10}]}], replace:true},
   {id:'fu', name:'분노', turns:3, until:{on:'stackGone', id:'분'}, mods:{dealt:0.1}}
  ]}
]";
        const string CARDS = @"[
 {id:'hit_f0', name:'치기', hero:'f', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'hit_f1', name:'세게 치기', hero:'f', token:true, cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy', hits:2}]},
 {id:'end_f', name:'끝내기', hero:'f', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'ifHand', n:0}, {k:'gauge', v:100}]},
 {id:'ult_b', name:'맨손 되기', hero:'f', cost:0, type:'스킬', fx:[{k:'form', id:'fb'}]},
 {id:'ult_u', name:'분노하기', hero:'f', cost:0, type:'스킬', fx:[{k:'stack', id:'분', v:2}, {k:'form', id:'fu'}]},
 {id:'spend_u', name:'터뜨리기', hero:'f', cost:0, type:'스킬', fx:[{k:'spend', id:'분', all:true}]}
]";

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS);
        static Battle F(List<Cue> cues = null, int gauge = 300) => K.Fight(D(), new[] { "f" }, new[] { "dummy" }, st => st.Gauge = gauge, cues);
        static Unit Hero(Battle b) => b.HeroUnit("f");

        [Test] public void 변신_들어감_지속_풀림()
        {
            var cues = new List<Cue>();
            var b = F(cues);
            Assert.IsFalse(b.InForm("f")); Assert.IsNull(b.FormSkin("f"));
            Assert.IsTrue(b.UseUlt("f").Ok);
            Assert.IsTrue(b.InForm("f"));
            var fv = b.FormOf("f");
            Assert.AreEqual("폭주", fv.Name); Assert.AreEqual(2, fv.Left); Assert.AreEqual("rage", b.FormSkin("f")); Assert.AreEqual("rage_", fv.Anim);
            Assert.AreEqual(150, b.AtkNow(Hero(b)), "그동안 공격력 +50%");
            var on = cues.Last(c => c.K == "formOn");
            Assert.AreEqual("f", on.Hero); Assert.AreEqual("fm", on.Id); Assert.AreEqual("rage", on.Label); Assert.AreEqual(2, on.V);
            Assert.IsTrue(b.StatusViews(Hero(b)).Any(s => s.Form && s.Id == "폭주" && s.Stacks == 2), "변신 칩");

            b.EndTurn();
            Assert.IsTrue(b.InForm("f")); Assert.AreEqual(1, b.FormOf("f").Left, "두 번째 내 턴");
            int g0 = b.Gauge;
            b.EndTurn();
            Assert.IsFalse(b.InForm("f"), "세 번째 내 턴 시작에 풀린다");
            Assert.AreEqual(100, b.AtkNow(Hero(b)));
            var off = cues.Last(c => c.K == "formOff");
            Assert.AreEqual("time", off.Label); Assert.AreEqual("fm", off.Id);
            Assert.GreaterOrEqual(b.Gauge, g0 + 7, "풀릴 때 효과(off)");
        }

        [Test] public void 카드_바꾸기_손_더미_그대로_모습만()
        {
            var cues = new List<Cue>();
            var b = F(cues);
            K.Hand(b, "hit_f0", "hit_f0");
            Assert.AreEqual("치기", b.CardOf("hit_f0").Name);
            b.UseUlt("f");
            Assert.AreEqual("세게 치기", b.CardOf("hit_f0").Name, "변신판 모습");
            Assert.AreEqual("hit_f0", b.Hand[0], "손의 id 는 그대로");
            Assert.IsTrue(cues.Any(c => c.K == "card" && c.Label == "form" && c.CardId == "hit_f0"), "손의 카드 모습이 바뀐 쪽지");
            K.Play(b, "hit_f0");
            Assert.AreEqual(1000 - 300, K.Hp(b), "공격력 150 × 2회");
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual("치기", b.CardOf("hit_f0").Name, "풀리면 돌아온다");
        }

        [Test] public void 덤_피해_배율_덧붙인_효과_패시브_교체()
        {
            var b = F(gauge: 0);   // 게이지 — AP 1 당 +10 · 원래 패시브 +1 · 변신 패시브 +10
            K.Hand(b, "ult_b", "hit_f0", "end_f", "hit_f0");
            int g0 = b.Gauge;
            K.Play(b, "ult_b");
            Assert.AreEqual(g0 + 10, b.Gauge, "카드를 낸 일(play)은 효과 뒤에 난다 — 변신을 낸 그 카드부터 변신 패시브(+10), 원래 것(+1)은 쉰다");
            Assert.AreEqual(0, b.FormOf("f").Left, "전투 끝까지");
            Assert.IsTrue(b.CardOf("end_f").Fx.Any(f => f.K == FxK.FormEnd));
            Assert.AreEqual(FxK.FormEnd, b.CardOf("end_f").Fx[1].K, "덤 효과는 조건 앞에 — 조건 없이 돈다");
            int g1 = b.Gauge;
            K.Play(b, "hit_f0");
            Assert.AreEqual(1000 - 150, K.Hp(b), "공격 카드 피해 ×1.5");
            Assert.AreEqual(g1 + 10 + 10, b.Gauge, "AP 1(+10) · replace — 원래 패시브(+1)는 쉬고 변신 패시브(+10)만");
            K.Play(b, "end_f");
            Assert.IsFalse(b.InForm("f"), "덤 formEnd — 손패가 남아 있어도(조건과 상관없이) 풀린다");
            int g2 = b.Gauge;
            K.Play(b, "hit_f0");
            Assert.AreEqual(g2 + 10 + 1, b.Gauge, "AP 1(+10) · 풀리면 원래 패시브(+1)");
        }

        [Test] public void 풀리는_계기_until_stackGone()
        {
            var cues = new List<Cue>();
            var b = F(cues);
            K.Hand(b, "ult_u", "spend_u");
            K.Play(b, "ult_u");
            Assert.IsTrue(b.InForm("f"));
            Assert.AreEqual(1.1, 1 + b.StatMod(Hero(b), "dealt"), 1e-9);
            K.Play(b, "spend_u");
            Assert.IsFalse(b.InForm("f"), "「분」 이 사라지면 풀린다");
            Assert.AreEqual("until", cues.Last(c => c.K == "formOff").Label);
            Assert.AreEqual(0, b.StatMod(Hero(b), "dealt"), 1e-9);
        }

        [Test] public void 다시_쓰면_지속을_처음으로_겹치지_않는다_다른_변신은_바꾼다()
        {
            var cues = new List<Cue>();
            var b = F(cues);
            b.UseUlt("f"); b.EndTurn();
            Assert.AreEqual(1, b.FormOf("f").Left);
            b.Gauge = 300;
            b.UseUlt("f");
            Assert.AreEqual(2, b.FormOf("f").Left, "지속 갱신");
            Assert.AreEqual(150, b.AtkNow(Hero(b)), "능력치는 겹치지 않는다");
            Assert.AreEqual(0, cues.Count(c => c.K == "formOff"), "갱신은 풀림이 아니다");
            int g0 = b.Gauge;
            K.Hand(b, "ult_b"); K.Play(b, "ult_b");
            Assert.AreEqual("맨손", b.FormOf("f").Name, "다른 변신 — 바꾼다");
            Assert.AreEqual("switch", cues.Last(c => c.K == "formOff").Label);
            Assert.GreaterOrEqual(b.Gauge, g0 + 7, "앞 변신의 off 가 돈다");
        }

        [Test] public void 저장_이어하기_왕복()
        {
            var d = D();
            var b = K.Fight(d, new[] { "f" }, new[] { "dummy" }, st => { st.Gauge = 300; st.Deck = new List<string> { "hit_f0", "hit_f0", "hit_f0", "hit_f0", "hit_f0", "hit_f0" }; });
            b.UseUlt("f"); b.EndTurn();
            var json = b.Save();
            var l = Battle.Load(d, json);
            Assert.IsTrue(l.InForm("f")); Assert.AreEqual(1, l.FormOf("f").Left);
            Assert.AreEqual("세게 치기", l.CardOf("hit_f0").Name);
            Assert.AreEqual(150, l.AtkNow(l.HeroUnit("f")));
            l.EndTurn();
            Assert.IsFalse(l.InForm("f"), "이어서도 지속이 다하면 풀린다");
            var b2 = K.Fight(d, new[] { "f" }, new[] { "dummy" });
            Assert.IsFalse(Battle.Load(d, b2.Save()).InForm("f"), "변신 없는 저장");
        }

        [Test] public void 미리보기_복사는_원본을_건드리지_않는다()
        {
            var b = F();
            K.Hand(b, "ult_u", "spend_u");
            K.Play(b, "ult_u");
            var sh = b.Clone();
            sh.Hand.Clear(); sh.Hand.Add("spend_u"); sh.PlayCard(0, 0);
            Assert.IsFalse(sh.InForm("f")); Assert.IsTrue(b.InForm("f"));
        }

        [Test] public void 글_고학년_변신_툴팁_칩()
        {
            var d = D(); var tx = new CardText(d);
            StringAssert.Contains("「폭주」로 변신(2턴)", tx.Ult(d.Hero("f").Ult));
            StringAssert.Contains("「폭주」로 변신", tx.Short(d.Hero("f").Ult));
            var form = tx.Form(d.Form("fm"));
            StringAssert.Contains("2턴 지속", form); StringAssert.Contains("공격력 +50%", form); StringAssert.Contains("「치기」 → 「세게 치기」", form); StringAssert.Contains("풀릴 때", form);
            StringAssert.Contains("「분」이 사라지면 풀림", tx.Form(d.Form("fu")));
            var fb = tx.Form(d.Form("fb"));
            StringAssert.Contains("전투 끝까지", fb); StringAssert.Contains("피해 ×1.5", fb); StringAssert.Contains("원래 패시브는 쉼", fb);
            Assert.IsTrue(tx.Tips().ContainsKey("폭주"));
            CollectionAssert.Contains(tx.Chips(d.View("ult_b")), "맨손");
            StringAssert.Contains("변신 「폭주」", tx.Hero(d.Hero("f")));
            StringAssert.Contains("변신 「폭주」", tx.HeroShort(d.Hero("f")));
            var b = F(); b.UseUlt("f");
            StringAssert.Contains("2회", tx.Card(b.CardOf("hit_f0")), "전투의 카드 글은 변신판");
        }

        [Test] public void 검사기_변신()
        {
            var v = Validator.Check(D());
            Assert.IsFalse(v.Errors.Any(e => e.Contains("변신")), string.Join("\n", v.Errors));
            var bad = K.Data(heroes: @"[{id:'g', name:'지', role:'딜러', row:'front', hp:1000, atk:100, def:50, keyword:{name:'지기', cap:3},
  ult:{name:'u', cost:100, fx:[{k:'form', id:'nope'}, {k:'stack', id:'지기', v:1}]},
  forms:[{id:'gf', name:'지기', turns:-1, mods:{speed:0.5}, cards:{hit:'big'}, passives:[{name:'p', when:{on:'always'}, fx:[{k:'atkMod', v:0.1}]}]}]}]");
            var e = Validator.Check(bad).Errors;
            Assert.IsTrue(e.Any(x => x.Contains("없는 변신")), "없는 변신 id");
            Assert.IsTrue(e.Any(x => x.Contains("고유 효과 · 상태 · 태그와 같다")), "이름이 고유 효과와 겹침");
            Assert.IsTrue(e.Any(x => x.Contains("turns")));
            Assert.IsTrue(e.Any(x => x.Contains("mods 의 능력치")));
            Assert.IsTrue(e.Any(x => x.Contains("이 사도의 카드가 아니다")), "남의 카드를 바꿈");
            Assert.IsTrue(e.Any(x => x.Contains("mods 로")), "변신 패시브의 always");
        }

        [Test] public void 봇이_변신_중에도_싸운다()
        {
            var d = D();
            var b = K.Fight(d, new[] { "f" }, new[] { "hitter" }, st => { st.Gauge = 300; st.Deck = Enumerable.Repeat("hit_f0", 8).Concat(new[] { "end_f", "ult_b" }).ToList(); });
            var bot = new Bots(d);
            for (int t = 0; t < 15 && b.Over == null; t++) { bot.SmartPlay(b); if (b.Over == null) b.EndTurn(); }
            Assert.AreEqual("win", b.Over);
        }

        [Test] public void 고유_효과가_최대에_닿으면_변신_onMax()
        {
            var d = K.Data(heroes: @"[{id:'r', name:'라', role:'딜러', row:'front', hp:1000, atk:100, def:50, crit:0,
  keyword:{name:'충', cap:3, onMax:{form:'rf'}, per:[{stat:'dealt', v:0.1}]},
  forms:[{id:'rf', name:'최대', turns:1, mods:{dealt:0.2}, off:[{k:'spend', id:'충', all:true}], anim:'Idle_Change'}]},
 {id:'q', name:'쿠', role:'딜러', row:'front', hp:1000, atk:100, def:50, crit:0,
  keyword:{name:'쿵', cap:2, onMax:{form:'qf', consume:true}},
  forms:[{id:'qf', name:'쿵쾅', turns:2, mods:{atk:0.1}}]}]",
                cards: "[{id:'ch', name:'충전', hero:'r', cost:0, type:'스킬', fx:[{k:'stack', id:'충', v:1}]}, {id:'qq', name:'쿵', hero:'q', cost:0, type:'스킬', fx:[{k:'stack', id:'쿵', v:2}]}]");
            var cues = new List<Cue>();
            var b = K.Fight(d, new[] { "r", "q" }, new[] { "dummy" }, null, cues);
            K.Hand(b, "ch", "ch", "ch", "ch", "qq");
            K.Play(b, "ch"); K.Play(b, "ch");
            Assert.IsFalse(b.InForm("r"));
            K.Play(b, "ch");
            Assert.IsTrue(b.InForm("r"), "셋 — 최대에 닿아 변신");
            Assert.AreEqual("Idle_Change", b.FormOf("r").Anim);
            Assert.AreEqual(3, b.StackOf("r", "충"), "consume 아님 — 겹은 남는다");
            K.Play(b, "ch");
            Assert.AreEqual(1, cues.Count(c => c.K == "formOn" && c.Hero == "r"), "이미 그 변신 — 다시 차도 갱신하지 않는다");
            b.EndTurn();
            Assert.IsFalse(b.InForm("r"), "1턴 — 다음 내 턴 시작에 풀린다");
            Assert.AreEqual(0, b.StackOf("r", "충"), "풀릴 때 방전(off spend all)");
            K.Hand(b, "qq"); K.Play(b, "qq");
            Assert.IsTrue(b.InForm("q")); Assert.AreEqual(0, b.StackOf("q", "쿵"), "consume — 닿은 겹을 다 쓴다");
            StringAssert.Contains("최대가 되면 → 「최대」로 변신(1턴)", new CardText(d).Keyword(d.Hero("r").Keyword));
            var errs = Validator.Check(d).Errors.Where(e => e.Contains("사도 r") || e.Contains("사도 q")).ToList();   // 바탕 시험 데이터(dummy 의 jam v0)는 빼고
            Assert.AreEqual(0, errs.Count, string.Join("\n", errs));
        }

        [Test] public void 콘텐츠_라이카_과충전_최대_출력()
        {
            string dir = new[] { "../bolzena-content-v2", "../../bolzena-content-v2", "C:/projects/bolzena-content-v2" }.FirstOrDefault(Directory.Exists);
            if (dir == null) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(dir);
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { "라이카" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero("라이카").Starter.ToList(), Seed = 3, EnemyHp = 50 });
            for (int i = 0; i < 6 && !b.InForm("라이카") && b.Over == null; i++)
            {
                K.Hand(b, "라이카_s1"); b.Ap = 3; K.Play(b, "라이카_s1");
                if (i % 2 == 1 && !b.InForm("라이카")) b.EndTurn();
            }
            Assert.IsTrue(b.InForm("라이카"), "과충전 셋 — 최대 출력");
            Assert.AreEqual("최대 출력 펀치", b.CardOf("라이카_s1").Name);
        }

        [Test] public void 콘텐츠_죠안_꿈결_형상은_버퍼()
        {
            string dir = new[] { "../bolzena-content-v2", "../../bolzena-content-v2", "C:/projects/bolzena-content-v2" }.FirstOrDefault(Directory.Exists);
            if (dir == null) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(dir);
            var b = Battle.Start(d, new BattleSetup { Party = new List<string> { "죠안" }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero("죠안").Starter.ToList(), Gauge = 300, Seed = 3, EnemyHp = 50 });
            Assert.IsTrue(b.UseUlt("죠안").Ok);
            Assert.AreEqual(2, b.St(b.Pool, "사기"), "고학년 — 파티 사기 2");
            Assert.AreEqual("스킬", b.CardOf("죠안_s1").Type, "변신판은 공격이 아니라 보호 카드");
            int sh = b.Pool.Shield + b.Pool.Block;
            K.Hand(b, "죠안_s1", "죠안_s2"); K.Play(b, "죠안_s1");
            Assert.AreEqual(3, b.St(b.Pool, "사기"), "꿈결의 교리 — 열과 상관없이 사기 +1");
            Assert.AreEqual(1, b.St(b.Pool, "결의"));
            Assert.Greater(b.Pool.Shield + b.Pool.Block, sh);
            b.EndTurn(); b.EndTurn();
            Assert.IsFalse(b.InForm("죠안"));
            Assert.GreaterOrEqual(b.St(b.Pool, "불굴"), 1, "풀릴 때 불굴");
        }

        [Test] public void 콘텐츠_세_사도_변신()
        {
            string dir = new[] { "../bolzena-content-v2", "../../bolzena-content-v2", "C:/projects/bolzena-content-v2" }.FirstOrDefault(Directory.Exists);
            if (dir == null) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(dir);
            var v = Validator.Check(d);
            Assert.IsTrue(v.Ok, string.Join("\n", v.Errors.Take(10)));
            foreach (var (hero, basic) in new[] { ("죠안", "죠안_s1"), ("네르_빡침", "네르_빡침_s1"), ("디아나_왕년", "디아나_왕년_s1") })
            {
                Assert.IsTrue(d.Hero(hero).Ult.Fx.Any(f => f.K == FxK.Form), hero + " 고학년에 변신");
                var b = Battle.Start(d, new BattleSetup { Party = new List<string> { hero }, Enemies = new List<string> { d.Enemies.Keys.First() }, Deck = d.Hero(hero).Starter.ToList(), Gauge = 300, Seed = 3 });
                string name0 = b.CardOf(basic).Name;
                Assert.IsTrue(b.UseUlt(hero).Ok, hero);
                if (b.Over != null) continue;
                Assert.IsTrue(b.InForm(hero), hero + " 변신");
                Assert.AreNotEqual(name0, b.CardOf(basic).Name, hero + " 기본 공격이 바뀐다");
                var l = Battle.Load(d, b.Save());
                Assert.AreEqual(b.FormOf(hero).Id, l.FormOf(hero).Id, "저장 왕복");
            }
        }
    }
}
