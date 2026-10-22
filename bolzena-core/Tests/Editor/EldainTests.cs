using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 엘다인 문법(2026-10-11, Docs/데이터.md §22) — 실드 소모 · 천상 countAs · 버림 계기 by · 변신 틀 · 다타 · 표식 옮기기 · 카드 값 · 이어 쓰기 · 검사기(▶E29).
    /// 사도 a(탱커 · 공 100 방 50) · c(딜러 · 공 150) + 이 시험의 사도.
    /// </summary>
    public class EldainTests
    {
        const string HEROES = @"[
 {id:'si', name:'시', role:'딜러', hp:600, atk:100, def:20, crit:0,
  keyword:{name:'장전', cap:9, rules:[
   {name:'구원', when:{on:'discard', by:'other'}, fx:[{k:'stack', id:'장전', v:1}]},
   {name:'남의 처치', when:{on:'kill', who:'other'}, fx:[{k:'stack', id:'장전', v:2}]},
   {name:'탄 내기', when:{on:'play', tag:'tk'}, fx:[{k:'stack', id:'장전', v:3}]}]}},
 {id:'dn', name:'디', role:'딜러', hp:600, atk:100, def:20, crit:0,
  keyword:{name:'연격', cap:9}, keywords:[{name:'기혈', cap:99}, {name:'흐름', cap:99}],
  passives:[{name:'듦', when:{on:'formOn', id:'bs'}, fx:[{k:'stack', id:'흐름', v:1}]},
            {name:'씀', when:{on:'spend', id:'연격'}, fx:[{k:'stack', id:'흐름', v:10}]}],
  forms:[
   {id:'bs', name:'백수 모드', turns:3, turnMake:[{id:'fist', n:2}, {id:'burst', n:1, when:{stack:'연격', n:6}}],
    offExpire:[{k:'stack', id:'기혈', v:1}], offEnd:[{k:'stack', id:'기혈', v:10}]},
   {id:'holy', name:'성전 모드', turns:2, turnMake:[{id:'fist', n:1}]}]},
 {id:'oc', name:'오', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'막', cap:99},
  passives:[{name:'한 번', when:{on:'guard', once:true}, fx:[{k:'stack', id:'막', v:1}]},
            {name:'매번', when:{on:'guard'}, fx:[{k:'stack', id:'막', v:10}]}]},
 {id:'ay', name:'아', role:'딜러', hp:600, atk:100, def:20, crit:0,
  keyword:{name:'꽃', carrier:'enemy', cap:9, rules:[{name:'옮겨 핌', when:{on:'kill'}, fx:[{k:'stack', id:'꽃', carry:true, target:'allEnemies'}]}]},
  keywords:[{name:'영역', cap:9}, {name:'위상', cap:3, wrap:true, stages:['초승','반달','보름'],
    rules:[{name:'보름', when:{on:'stackReach', id:'위상', n:3}, fx:[{k:'stack', id:'영역', v:1}]},
           {name:'초승', when:{on:'stackReach', id:'위상', n:1}, fx:[{k:'stack', id:'영역', v:2}]}]},
   {name:'자세', cap:2, wrap:true, stages:['양산','칼']}]},
 {id:'er', name:'에', role:'딜러', hp:600, atk:100, def:20, crit:0,
  keyword:{name:'왕관', cap:20, rules:[{name:'되돌림', when:{on:'kill', mine:true}, fx:[{k:'stack', id:'왕관', v:2}]}]},
  keywords:[{name:'실', cap:9}]},
 {id:'tg', name:'티', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'영웅', cap:99},
  passives:[{name:'토큰 빼고', when:{on:'play', token:false}, fx:[{k:'stack', id:'영웅', v:1}]}]},
 {id:'cl', name:'클', role:'탱커', hp:800, atk:100, def:50, crit:0, keyword:{name:'인형', guard:true, cap:5}}
]";
        const string CARDS = @"[
 {id:'ram', name:'들이받기', hero:'a', cost:1, type:'공격', fx:[{k:'spendShield', half:true}, {k:'dmg', ratio:1, ofEvent:2, target:'oneEnemy'}]},
 {id:'ram_p', name:'고른 들이받기', hero:'a', cost:1, type:'공격', fx:[{k:'spendShield', pick:true}, {k:'dmg', ratio:1, ofEvent:1, target:'oneEnemy'}]},
 {id:'pershield', name:'실드 비례', hero:'a', cost:1, type:'공격', fx:[{k:'perShield', per:100}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'laser', name:'레이저', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, ofEvent:0.3, ofShield:true, target:'oneEnemy'}]},
 {id:'heaven1', name:'천상 하나', hero:'a', cost:2, type:'스킬', tags:['천상'], fx:[{k:'shield', ratio:1}]},
 {id:'heaven2', name:'천상 둘', hero:'a', cost:2, type:'스킬', tags:['천상'], fx:[{k:'shield', ratio:1}]},
 {id:'count2', name:'비용 2로 셈', hero:'a', cost:1, type:'공격', countAs:2, fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'dis_a', name:'가 버리기', hero:'a', cost:0, type:'스킬', fx:[{k:'discard', v:1}]},
 {id:'dis_si', name:'시 버리기', hero:'si', cost:0, type:'스킬', fx:[{k:'discard', v:1}]},
 {id:'si_card', name:'시 카드', hero:'si', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'shoot', name:'탄 모두 버림', hero:'si', cost:0, type:'공격', fx:[{k:'discard', all:true, tag:'tk'}, {k:'perDiscarded'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'tk', name:'탄', hero:'si', token:true, cost:1, type:'공격', tags:['소멸'], fx:[{k:'make', id:'tk'}]},
 {id:'enter', name:'부수기', hero:'dn', cost:0, type:'스킬', fx:[{k:'form', id:'bs'}]},
 {id:'enter_h', name:'선포', hero:'dn', cost:0, type:'스킬', fx:[{k:'form', id:'holy'}]},
 {id:'tear', name:'찢기', hero:'dn', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'ifForm', id:'bs'}, {k:'stack', id:'연격', v:3}]},
 {id:'endc', name:'끝내기', hero:'dn', cost:0, type:'공격', fx:[{k:'ifForm', id:'bs'}, {k:'formEnd'}]},
 {id:'pump', name:'모으기', hero:'dn', cost:0, type:'스킬', fx:[{k:'stack', id:'연격', v:6}]},
 {id:'drain', name:'쏟기', hero:'dn', cost:0, type:'스킬', fx:[{k:'spend', id:'연격', all:true}]},
 {id:'cut', name:'끊김', hero:'dn', cost:0, type:'스킬', fx:[{k:'stack', id:'연격', v:-2}]},
 {id:'ext', name:'연장', hero:'dn', cost:0, type:'스킬', fx:[{k:'formExtend', id:'holy', v:1, max:3}]},
 {id:'fist', name:'주먹', hero:'dn', token:true, cost:0, type:'공격', tags:['증발', '소멸'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'burst', name:'폭발', hero:'dn', token:true, cost:0, type:'공격', tags:['증발', '소멸'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'dbl', name:'두 겹', hero:'oc', cost:0, type:'스킬', fx:[{k:'shield', ratio:1}, {k:'shield', ratio:1}]},
 {id:'flurry', name:'흩날림', hero:'ay', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, hits:3, target:'allEnemies', hitFx:[{k:'stack', id:'꽃', v:1}]}]},
 {id:'kill_ay', name:'베기', hero:'ay', cost:0, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'moon3', name:'보름으로', hero:'ay', cost:0, type:'스킬', fx:[{k:'stack', id:'위상', set:3}]},
 {id:'moon1', name:'한 칸', hero:'ay', cost:0, type:'스킬', fx:[{k:'stack', id:'위상', v:1}]},
 {id:'swap', name:'자세 바꿈', hero:'ay', cost:0, type:'스킬', fx:[{k:'stack', id:'자세', v:1}]},
 {id:'hitsper', name:'영역 다타', hero:'ay', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.1, hitsPer:'영역', hitsAdd:1, target:'oneEnemy'}]},
 {id:'low', name:'마무리', hero:'ay', cost:0, type:'공격', fx:[{k:'ifFoe', hpBelow:0.3}, {k:'stack', id:'영역', v:5}]},
 {id:'bread', name:'빵 굽기', hero:'a', cost:0, type:'스킬', fx:[{k:'make', id:'bread_t', owner:'strongestAlly'}]},
 {id:'bread_t', name:'빵', hero:'a', token:true, cost:0, type:'스킬', tags:['소멸'], fx:[]},
 {id:'cheap', name:'싸게', hero:'a', cost:0, type:'스킬', fx:[{k:'costMod', v:-1, token:true}]},
 {id:'runway', name:'런웨이', hero:'si', cost:0, type:'스킬', fx:[{k:'autoPlay', all:true, token:true}]},
 {id:'sprout', name:'새싹', hero:'a', cost:0, type:'공격', fx:[{k:'perCardSt', id:'새싹'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'cardStatus', id:'새싹', set:0}]},
 {id:'sprout_h', name:'반 새싹', hero:'a', cost:0, type:'공격', fx:[{k:'cardStatus', id:'새싹', half:true}]},
 {id:'allsprout', name:'모두의 빛', hero:'a', cost:0, type:'공격', fx:[{k:'perCardSt', id:'새싹', from:'hand', who:'self'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'barr', name:'난타', hero:'er', cost:0, type:'공격', fx:[{k:'spend', id:'왕관', pick:true, loop:true, max:12}, {k:'perEvent'}, {k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'thread', name:'실 잣기', hero:'er', cost:0, type:'스킬', fx:[{k:'spend', id:'실', pick:true}, {k:'perEvent'}, {k:'make', id:'tk'}]},
 {id:'fg', name:'잊힘', hero:'a', cost:0, type:'스킬', tags:['망각', '소멸'], fx:[{k:'noForget'}]},
 {id:'fg2', name:'잊힘 둘', hero:'a', cost:0, type:'스킬', tags:['망각', '소멸'], fx:[]},
 {id:'ex1', name:'사라짐', hero:'a', cost:0, type:'스킬', tags:['소멸'], fx:[]},
 {id:'cnt', name:'사라진 셈', hero:'a', cost:0, type:'공격', fx:[{k:'perGone', turn:true, id:'ex1'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'pile', name:'더미 셈', hero:'a', cost:0, type:'공격', fx:[{k:'perPile', from:'draw', tag:'tk'}, {k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'orc', name:'신탁 종류', hero:'a', cost:1, type:'공격', unique:true, fx:[{k:'dmg', ratio:1, target:'oneEnemy'}],
  oracles:[{name:'스킬로', type:'스킬', fx:[{k:'shield', ratio:1.5}]}, {name:'o2', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}]}, {name:'o3', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}]}, {name:'o4', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}]}, {name:'o5', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}]}]}
]";
        const string FOES = @"[{id:'weak', name:'약골', hp:50, intents:[{t:'jam', v:0, rush:0}]}]";

        static GameData D() => K.Data(heroes: HEROES, cards: CARDS, enemies: FOES);
        static Battle F(string[] party, params string[] foes) => K.Fight(D(), party, foes.Length > 0 ? foes : new[] { "dummy" });
        static int N(Battle b, string baseId) => b.Hand.Count(h => GameData.BaseId(h) == baseId);

        // ── E1 실드 소모 · 실드 비례 ──
        [Test] public void E1_실드_절반을_써서_쓴_양의_200퍼센트()
        {
            var b = F(new[] { "a" });
            b.Pool.Block = 60; b.Pool.Shield = 140;
            K.Hand(b, "ram"); K.Play(b, "ram");
            Assert.AreEqual(1000 - 200, K.Hp(b), "쓴 양(100)의 200%");
            Assert.AreEqual(0, b.Pool.Block, "방어부터 깎는다");
            Assert.AreEqual(100, b.Pool.Shield);
        }

        [Test] public void E1_실드가_없으면_피해_0()
        {
            var b = F(new[] { "a" });
            K.Hand(b, "ram"); K.Play(b, "ram");
            Assert.AreEqual(1000, K.Hp(b));
        }

        [Test] public void E1_고른_만큼_쓴다()
        {
            var b = F(new[] { "a" });
            b.Pool.Shield = 200;
            K.Hand(b, "ram_p");
            CollectionAssert.AreEqual(new[] { 100, 200 }, b.ShieldChoices("ram_p"));
            K.Play(b, "ram_p", 0, new PlayOpts { Spend = 50 });
            Assert.AreEqual(150, b.Pool.Shield);
            Assert.AreEqual(950, K.Hp(b));
        }

        [Test] public void E1_실드_비례와_지금_실드의_몇_퍼센트()
        {
            var b = F(new[] { "a" });
            b.Pool.Shield = 250;
            K.Hand(b, "pershield", "laser");
            K.Play(b, "pershield");
            Assert.AreEqual(1000 - 200, K.Hp(b), "실드 100당 한 대(2대 × 100)");
            Assert.AreEqual(250, b.Pool.Shield, "쓰지 않는다");
            K.Play(b, "laser");
            Assert.AreEqual(800 - 75, K.Hp(b), "실드 250 의 30%");
        }

        // ── E2 천상 ──
        [Test] public void E2_천상_둘은_한_번씩만_돈다()
        {
            var b = F(new[] { "a" });
            K.Hand(b, "heaven1", "heaven2", "big"); b.Ap = 3;
            K.Play(b, "big");
            Assert.AreEqual(3, b.PlaysTotal, "2코 한 장에 천상 둘이 한 번씩(서로 다시 깨우지 않는다)");
            Assert.AreEqual(0, b.Hand.Count);
        }

        [Test] public void E2_countAs_는_천상에서_비용_2()
        {
            var b = F(new[] { "a" });
            K.Hand(b, "heaven1", "hit"); b.Ap = 3;
            K.Play(b, "hit");
            Assert.AreEqual(1, b.PlaysTotal, "1코는 천상을 깨우지 않는다");
            K.Hand(b, "heaven1", "count2");
            K.Play(b, "count2");
            Assert.AreEqual(3, b.PlaysTotal, "countAs 2 — 천상이 돈다");
            StringAssert.Contains("천상에서 비용 2로 센다", new CardText(b.Data).Card(b.CardOf("count2")));
        }

        // ── E7 다른 아군의 효과로 버려짐 ──
        [Test] public void E7_다른_아군의_효과로만_돈다()
        {
            var b = F(new[] { "a", "si" });
            K.Hand(b, "dis_a", "si_card");
            K.Play(b, "dis_a", 0, new PlayOpts { Discard = new List<string> { "si_card" } });
            Assert.AreEqual(1, b.StackOf("si", "장전"), "가의 효과로 시의 카드가 버려짐 — 돈다");
            K.Hand(b, "dis_a", "hit");
            K.Play(b, "dis_a", 0, new PlayOpts { Discard = new List<string> { "hit" } });
            Assert.AreEqual(2, b.StackOf("si", "장전"), "카드 주인은 상관없다");
            K.Hand(b, "dis_si", "hit");
            K.Play(b, "dis_si", 0, new PlayOpts { Discard = new List<string> { "hit" } });
            Assert.AreEqual(2, b.StackOf("si", "장전"), "자신의 효과 — 안 돈다");
            K.Hand(b, "hit", "si_card");
            b.EndTurn();
            Assert.AreEqual(2, b.StackOf("si", "장전"), "턴 끝 손 비우기 — 안 돈다");
            StringAssert.Contains("다른 아군의 효과로 카드가 버려지면", new CardText(b.Data).Detail(b.Data.Hero("si").Keyword.Rules[0]));
        }

        [Test] public void 버림_거르개와_계기_거르개()
        {
            var b = F(new[] { "a", "si" });
            K.Hand(b, "shoot", "hit", "tk~", "tk~");
            K.Play(b, "shoot");
            Assert.AreEqual(1000 - 20, K.Hp(b), "탄 2장만 버리고 1장당 10%");
            CollectionAssert.AreEqual(new[] { "hit" }, b.Hand);
            StringAssert.Contains("손의 「탄」 모두 버림", new CardText(b.Data).Card(b.CardOf("shoot")));
            K.Hand(b, "tk~"); b.Ap = 3;
            K.Play(b, "tk~");
            Assert.AreEqual(3, b.StackOf("si", "장전"), "play tag 에 카드 id");
        }

        [Test] public void 처치_who_other()
        {
            var b = F(new[] { "a", "si" }, "weak", "weak");
            K.Hand(b, "hit"); K.Play(b, "hit", 0);
            Assert.AreEqual(2, b.StackOf("si", "장전"), "다른 아군이 처치");
            K.Hand(b, "si_card"); K.Play(b, "si_card", 1);
            Assert.IsNotNull(b.Over);
            Assert.AreEqual(2, b.StackOf("si", "장전"), "자신의 처치는 아니다");
        }

        // ── E5 · E6 · E22 · E26 변신 묶음 ──
        [Test] public void E5_E6_변신_조건과_변신_카드()
        {
            var b = F(new[] { "dn" });
            K.Hand(b, "tear"); K.Play(b, "tear");
            Assert.AreEqual(0, b.StackOf("dn", "연격"), "변신 밖 — ifForm 안 섬");
            K.Hand(b, "enter"); K.Play(b, "enter");
            Assert.AreEqual(2, N(b, "fist"), "드는 순간 변신 카드");
            Assert.AreEqual(1, b.StackOf("dn", "흐름"), "formOn 계기");
            K.Hand(b, "tear"); K.Play(b, "tear");
            Assert.AreEqual(3, b.StackOf("dn", "연격"), "변신 안 — 섬");
            StringAssert.Contains("「백수 모드」면", new CardText(b.Data).Card(b.CardOf("tear")));
            K.Hand(b);
            b.EndTurn();
            Assert.AreEqual(2, N(b, "fist"), "매 턴 시작 — 지난 것은 증발");
            StringAssert.Contains("매 턴 「주먹」 2", new CardText(b.Data).Form(b.Data.Form("bs")));
        }

        [Test] public void E6_조건부_변신_카드는_그_턴_한_장()
        {
            var b = F(new[] { "dn" });
            K.Hand(b, "enter"); K.Play(b, "enter");
            K.Hand(b, "pump", "drain", "pump"); K.Play(b, "pump");
            Assert.AreEqual(1, N(b, "burst"), "6 이 되는 순간 한 장");
            K.Play(b, "drain"); K.Play(b, "pump");
            Assert.AreEqual(1, N(b, "burst"), "같은 턴 다시 닿아도 더 안 든다");
            b.EndTurn();
            K.Hand(b, "drain", "pump"); K.Play(b, "drain"); K.Play(b, "pump");
            Assert.AreEqual(1, N(b, "burst"), "다음 턴엔 다시");
        }

        [Test] public void E22_만료와_카드_끝의_갈래()
        {
            var b = F(new[] { "dn" });
            K.Hand(b, "enter"); K.Play(b, "enter");
            b.EndTurn(); b.EndTurn(); b.EndTurn();
            Assert.IsFalse(b.InForm("dn"));
            Assert.AreEqual(1, b.StackOf("dn", "기혈"), "시간이 다 되면 — offExpire 만");
            K.Hand(b, "enter", "endc"); K.Play(b, "enter"); K.Play(b, "endc");
            Assert.IsFalse(b.InForm("dn"));
            Assert.AreEqual(11, b.StackOf("dn", "기혈"), "스스로 끝내면 — offEnd 만");
            StringAssert.Contains("변신을 끝냄", new CardText(b.Data).Card(b.CardOf("endc")));
        }

        [Test] public void E26_변신_연장은_전체_길이_상한()
        {
            var b = F(new[] { "dn" });
            K.Hand(b, "enter_h", "ext", "ext"); K.Play(b, "enter_h");
            Assert.AreEqual(2, b.FormOf("dn").Left);
            K.Play(b, "ext");
            Assert.AreEqual(3, b.FormOf("dn").Left, "+1");
            K.Play(b, "ext");
            Assert.AreEqual(3, b.FormOf("dn").Left, "전체 3턴까지");
            var l = Battle.Load(b.Data, b.Save());
            Assert.AreEqual(3, l.FormOf("dn").Left, "저장 왕복");
        }

        [Test] public void E21_음수_쌓기는_소모_계기를_안_깨운다()
        {
            var b = F(new[] { "dn" });
            K.Hand(b, "pump", "cut"); K.Play(b, "pump"); K.Play(b, "cut");
            Assert.AreEqual(4, b.StackOf("dn", "연격"));
            Assert.AreEqual(0, b.StackOf("dn", "흐름"), "spend 계기가 돌지 않는다");
        }

        // ── E10 한 장에 한 번 ──
        [Test] public void E10_once_는_카드_한_장에_한_번()
        {
            var b = F(new[] { "oc" });
            K.Hand(b, "dbl"); K.Play(b, "dbl");
            Assert.AreEqual(1 + 20, b.StackOf("oc", "막"), "once 1번 · 보통 2번");
        }

        // ── E3 · E4 · E24 · E28 · E20 적 표식 · 다타 · 쌓기 ──
        [Test] public void E3_맞은_적마다()
        {
            var b = F(new[] { "ay" }, "dummy", "dummy");
            K.Hand(b, "flurry"); K.Play(b, "flurry");
            Assert.AreEqual(3, b.St(K.E(b, 0), "꽃"));
            Assert.AreEqual(3, b.St(K.E(b, 1), "꽃"));
            StringAssert.Contains("맞은 적마다 「꽃」 1", new CardText(b.Data).Card(b.CardOf("flurry")));
        }

        [Test] public void E4_쓰러진_적의_표식을_옮긴다()
        {
            var b = F(new[] { "ay" }, "weak", "dummy");
            K.Hand(b, "flurry"); K.Play(b, "flurry");
            K.Hand(b, "kill_ay"); K.Play(b, "kill_ay", 0);
            Assert.IsTrue(K.E(b, 0).Dead);
            Assert.AreEqual(3 + 3, b.St(K.E(b, 1), "꽃"), "쓰러진 적의 꽃 3 이 옮겨 온다");
        }

        [Test] public void E28_E33_값_정하기와_순환_진입()
        {
            var b = F(new[] { "ay" });
            K.Hand(b, "moon3", "moon1"); K.Play(b, "moon3");
            Assert.AreEqual(3, b.StackOf("ay", "위상"));
            Assert.AreEqual(1, b.StackOf("ay", "영역"), "set 으로 올라도 「3이 되면」");
            K.Play(b, "moon1");
            Assert.AreEqual(1, b.StackOf("ay", "위상"), "한 바퀴");
            Assert.AreEqual(3, b.StackOf("ay", "영역"), "순환이 돌아 첫 단계 — 「1이 되면」 도 돈다");
            var t = new CardText(b.Data);
            StringAssert.Contains("「위상」을 보름으로", t.Card(b.CardOf("moon3")));
            StringAssert.Contains("자세를 바꿈", t.Card(b.CardOf("swap")));
        }

        [Test] public void E24_E20_횟수와_HP_비율()
        {
            var b = F(new[] { "ay" });
            K.Hand(b, "moon3", "hitsper"); K.Play(b, "moon3");   // 영역 1
            K.Play(b, "hitsper");
            Assert.AreEqual(1000 - 20, K.Hp(b), "(영역 1 + 1)회 × 10%");
            K.E(b).Hp = 300;
            K.Hand(b, "low"); K.Play(b, "low");
            Assert.AreEqual(6, b.StackOf("ay", "영역"), "HP 30% 이하");
        }

        [Test] public void 타수_합치기_겹_수_타수는_한_대에_피해_합은_그대로()
        {
            // (2026-10-19 R.PER_HIT_MERGE) 파티의 겹 수 타수 · 「1개당」 피해 · X 타수는 여러 번 대신 한 대 × 수
            var b = F(new[] { "ay" });
            K.Hand(b, "moon3", "hitsper"); K.Play(b, "moon3");   // 영역 1
            int h0 = b.HitsBy(K.E(b), "ay");
            K.Play(b, "hitsper");
            Assert.AreEqual(1000 - 20, K.Hp(b), "(영역 1 + 1) × 10% — 합은 그대로");
            Assert.AreEqual(R.PER_HIT_MERGE ? 1 : 2, b.HitsBy(K.E(b), "ay") - h0, "친 횟수");
        }

        // ── E23 · E11 · E25 생성 카드 ──
        [Test] public void E23_공격력이_가장_높은_아군이_주인()
        {
            var b = F(new[] { "a", "c" });
            K.Hand(b, "bread"); K.Play(b, "bread");
            Assert.AreEqual("c", b.CardOf(b.Hand.Single()).Hero);
        }

        [Test] public void E11_생성_카드_거르개와_모두_저절로()
        {
            var b = F(new[] { "a", "si" });
            K.Hand(b, "cheap", "tk~", "si_card"); K.Play(b, "cheap");
            Assert.AreEqual(0, b.CostOf("tk~")); Assert.AreEqual(1, b.CostOf("si_card"));
            K.Hand(b, "runway", "tk~", "tk~", "si_card"); int p0 = b.PlaysTotal;
            K.Play(b, "runway");
            Assert.AreEqual(p0 + 3, b.PlaysTotal, "처음 손의 탄 2장만(새로 생긴 탄은 빼고)");
            Assert.AreEqual(2, N(b, "tk")); Assert.AreEqual(1, N(b, "si_card"));
        }

        [Test] public void E25_생성_카드를_빼고()
        {
            var b = F(new[] { "tg", "si" });
            K.Hand(b, "tk~", "si_card"); b.Ap = 5;
            K.Play(b, "tk~");
            Assert.AreEqual(0, b.StackOf("tg", "영웅"));
            K.Play(b, "si_card");
            Assert.AreEqual(0, b.StackOf("tg", "영웅"), "남의 카드(자신 카드만 — 기본)");
        }

        // ── E17 · E18 · E19 · E36 카드 값 · 고른 만큼 ──
        [Test] public void E17_E18_카드_값_정하기와_손_합()
        {
            var b = F(new[] { "a" });
            K.Hand(b, "sprout", "sprout_h", "allsprout", "hit");
            b.CardSt["sprout"] = new Dictionary<string, int> { ["새싹"] = 3 };
            b.CardSt["sprout_h"] = new Dictionary<string, int> { ["새싹"] = 5 };
            b.CardSt["hit"] = new Dictionary<string, int> { ["새싹"] = 2 };
            K.Play(b, "allsprout");
            Assert.AreEqual(1000 - 100, K.Hp(b), "손의 자신 카드 새싹 합 10 × 10%");
            K.Play(b, "sprout");
            Assert.AreEqual(900 - 30, K.Hp(b), "낸 때 값 3");
            Assert.AreEqual(0, b.CardStOf("sprout", "새싹"), "낸 뒤 0 으로");
            K.Play(b, "sprout_h");
            Assert.AreEqual(2, b.CardStOf("sprout_h", "새싹"), "절반(내림)");
        }

        [Test] public void E19_처치로_얻은_만큼_이어_쏜다()
        {
            var b = F(new[] { "er" }, "weak", "dummy");
            K.Hand(b, "barr");
            // 왕관 3 에서 1 만 고른다 — 남긴 2 는 쓰지 않는다
            b.Stacks["er"] = new Dictionary<string, int> { ["왕관"] = 3 };
            K.Play(b, "barr", 0, new PlayOpts { Spend = 1 });
            Assert.IsTrue(K.E(b, 0).Dead);
            Assert.AreEqual(1000 - 200, K.Hp(b, 1), "처치 +2 → 두 대 더(남긴 2 위만)");
            Assert.AreEqual(2, b.StackOf("er", "왕관"), "남겨 둔 몫은 그대로");
        }

        [Test] public void E36_일의_값만큼_생성()
        {
            var b = F(new[] { "er" });
            b.Stacks["er"] = new Dictionary<string, int> { ["실"] = 3 };
            K.Hand(b, "thread"); K.Play(b, "thread", 0, new PlayOpts { Spend = 2 });
            Assert.AreEqual(2, N(b, "tk"));
        }

        // ── E32 · E34 · E35 ──
        [Test] public void E32_망각_없이_사라짐()
        {
            var b = F(new[] { "a" });
            K.Hand(b, "fg", "fg2"); K.Play(b, "fg"); K.Play(b, "fg2");
            Assert.Contains("fg", b.Gone); Assert.IsFalse(b.Draw.Contains("fg"));
            Assert.Contains("fg2", b.Draw, "보통 망각 — 뽑을 더미 맨 위");
        }

        [Test] public void E34_E35_이번_턴_사라진_그_카드와_더미의_그_카드()
        {
            var b = F(new[] { "a" });
            K.Hand(b, "ex1", "ex1~", "hit", "cnt"); b.Ap = 3;
            K.Play(b, "ex1"); K.Play(b, "ex1~");
            K.Play(b, "cnt");
            Assert.AreEqual(1000 - 20, K.Hp(b), "이번 턴 사라진 「사라짐」 2장");
            b.Draw.AddRange(new[] { "tk~", "hit", "tk~", "tk~" });
            K.Hand(b, "pile"); K.Play(b, "pile");
            Assert.AreEqual(980 - 30, K.Hp(b), "뽑을 더미의 탄 3장");
        }

        [Test] public void E37_막는_소환물은_파티_누구를_노려도_대신_받는다()
        {
            var b = F(new[] { "a", "cl" }, "hitter");
            b.Stacks["cl"] = new Dictionary<string, int> { ["인형"] = 1 };
            int hp0 = b.Pool.Hp;
            b.EndTurn();
            Assert.AreEqual(hp0, b.Pool.Hp, "파티는 한 몸 — 그 대를 소환물이 받는다");
            Assert.AreEqual(0, b.StackOf("cl", "인형"));
        }

        [Test] public void 신탁_type_은_카드_종류를_바꾼다()
        {
            var d = D();
            Assert.AreEqual("스킬", d.View("orc", 1).Type);
            Assert.AreEqual("공격", d.View("orc", 2).Type);
        }

        // ── E9 봇 — 헐기 · 지키기 · 쥐기 ──
        [Test] public void E9_봇은_적이_세게_치면_실드를_헐지_않는다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS.TrimEnd(']') + @", {id:'ram_w', name:'약한 들이받기', hero:'a', cost:0, type:'공격', fx:[{k:'spendShield', half:true}, {k:'dmg', ratio:1, ofEvent:0.5, target:'oneEnemy'}]}]",
                enemies: FOES.TrimEnd(']') + @", {id:'smash', name:'박살', hp:1000, intents:[{t:'attack', v:900, rush:0}]}]");
            var bot = new Bots(d);
            foreach (var (foe, spend) in new[] { ("dummy", true), ("smash", false) })
            {
                var b = K.Fight(d, new[] { "a" }, new[] { foe });
                K.Hand(b, "ram_w"); b.Pool.Shield = 600;
                bot.SmartPlay(b);
                Assert.AreEqual(spend ? 300 : 600, b.Pool.Shield, foe + (spend ? " — 칠 게 없으면 헐어 친다" : " — 막을 실드를 지킨다"));
            }
        }

        [Test] public void E9_봇은_고른_만큼_실드와_버림_거르개를_안다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS.TrimEnd(']') + @", {id:'drop_tk', name:'탄 하나 버림', hero:'si', cost:0, type:'스킬', fx:[{k:'discard', v:1, tag:'tk'}]}]");
            var bot = new Bots(d);
            var b = K.Fight(d, new[] { "a", "si" }, new[] { "dummy" });
            b.Pool.Shield = 200;
            K.Hand(b, "ram_p", "hit");
            var ms = bot.Moves(b);
            Assert.IsTrue(ms.Any(m => m.Id == "ram_p" && m.Spend == 100) && ms.Any(m => m.Id == "ram_p" && m.Spend == 200), "절반 · 전부를 둬 본다");
            K.Hand(b, "drop_tk", "zero", "tk~");
            Assert.AreEqual(1, b.DiscardChoice(0) == 0 ? 1 : b.DiscardChoice(0));
            var pick = bot.PickDiscard(b, 0);
            Assert.IsTrue(pick == null || pick.All(x => GameData.BaseId(x) == "tk"), "거르개에 맞는 카드만 고른다");
            Assert.IsFalse(b.DiscardFits(0, "zero")); Assert.IsTrue(b.DiscardFits(0, "tk~"));
        }

        [Test] public void E9_봇은_쥔_무리가_세게_하는_카드를_본다()
        {
            var d = K.Data(heroes: HEROES, cards: CARDS.TrimEnd(']') + @", {id:'sniper', name:'저격총', hero:'si', cost:2, type:'공격', fx:[{k:'perTag', id:'tk'}, {k:'dmg', ratio:0.4, target:'oneEnemy'}]}]");
            var bot = new Bots(d);
            var b = K.Fight(d, new[] { "si" }, new[] { "dummy" });
            K.Hand(b, "sniper", "tk~", "tk~"); b.Ap = 3;
            Assert.Greater(bot.HoldWorth(b), 0, "손의 탄 2장이 저격총을 세게 한다");
            K.Hand(b, "sniper");
            Assert.AreEqual(0, bot.HoldWorth(b));
        }

        [Test] public void E9_봇은_망각_카드가_드로우를_막으면_자세를_바꿔_치운다()
        {
            var d = K.Data(heroes: @"[{id:'ya', name:'야', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'접는 자세', cap:2, wrap:true, stages:['양산','칼']}}]",
                cards: @"[{id:'fold', name:'접기', hero:'ya', cost:0, type:'스킬', fx:[{k:'stack', id:'접는 자세', v:1}]},
 {id:'bf', name:'나비', hero:'ya', token:true, cost:0, type:'공격', tags:['망각', '소멸'], fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}, {k:'ifStack', id:'접는 자세', n:2, max:2}, {k:'noForget'}]}]");
            var bot = new Bots(d);
            var b = K.Fight(d, new[] { "ya" }, new[] { "dummy" });
            b.Stacks["ya"] = new Dictionary<string, int> { ["접는 자세"] = 1 };   // 양산
            b.Draw.AddRange(new[] { "hit", "bf~", "bf~", "bf~" });
            K.Hand(b, "fold", "bf~"); b.Ap = 3;
            Assert.Greater(bot.ClogCost(b), 0, "뽑을 더미 위의 나비 셋 — 막힘");
            var log = new List<string>();
            bot.SmartPlay(b, trace: (m, v, st) => log.Add($"{m} {v:0} / {st:0}"));
            string why = string.Join(" | ", log) + " | 손 " + string.Join(",", b.Hand) + " · 더미 " + string.Join(",", b.Draw) + " · 소멸 " + string.Join(",", b.Gone);
            Assert.AreEqual(2, b.StackOf("ya", "접는 자세"), "칼로 접는다 " + why);
            Assert.IsTrue(b.Gone.Any(x => GameData.BaseId(x) == "bf"), "칼에서 낸 나비는 망각 없이 사라진다 " + why);
        }

        // ── 고리 · 안전 상한 ──
        [Test] public void 버림_by_other_핑퐁도_끝난다()
        {
            var d = K.Data(heroes: @"[
 {id:'p1', name:'핑', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'핑', cap:99, rules:[{name:'핑', when:{on:'discard', by:'other'}, fx:[{k:'discard', v:1, random:true}, {k:'make', id:'pp'}]}]}},
 {id:'p2', name:'퐁', role:'딜러', hp:600, atk:100, def:20, crit:0, keyword:{name:'퐁', cap:99, rules:[{name:'퐁', when:{on:'discard', by:'other'}, fx:[{k:'discard', v:1, random:true}, {k:'make', id:'pp'}]}]}}]",
                cards: @"[{id:'pp', name:'공', hero:'p1', token:true, cost:0, type:'스킬', fx:[]}, {id:'go', name:'시작', hero:'p1', cost:0, type:'스킬', fx:[{k:'discard', v:1, random:true}]}]");
            var b = K.Fight(d, new[] { "p1", "p2" }, new[] { "dummy" });
            K.Hand(b, "go", "pp~", "pp~");
            K.Play(b, "go");
            Assert.LessOrEqual(b.Hand.Count, R.HAND_MAX, "끝없이 돌지 않고 끝난다(깊이 · 안전 상한)");
        }

        // ── 글 ──
        [Test] public void 글_실드_소모와_고른_만큼_써서()
        {
            var d = K.Data(cards: @"[
 {id:'t1', name:'t1', hero:'a', cost:1, type:'공격', fx:[{k:'spendShield', half:true}, {k:'dmg', ratio:1, ofEvent:2, fixed:true, target:'oneEnemy'}]},
 {id:'t2', name:'t2', hero:'a', cost:1, type:'공격', fx:[{k:'spend', id:'분', pick:true}, {k:'perEvent'}, {k:'dmg', ratio:0.3, target:'allEnemies'}]},
 {id:'t3', name:'t3', hero:'a', cost:1, type:'스킬', fx:[{k:'perShield', per:100}, {k:'shield', ratio:0.5}]}]",
                heroes: "[{id:'q', name:'큐', role:'딜러', hp:600, atk:100, def:20, keyword:{name:'분', cap:9}}]");
            var t = new CardText(d);
            Assert.AreEqual("파티 실드 절반을 씀, 쓴 양의 200% 고정 피해", t.Card(d.Card("t1")));
            Assert.AreEqual("「분」을 고른 만큼 써서 1개당 적 전체에 피해 30%", t.Card(d.Card("t2")));
            StringAssert.Contains("파티 실드 100당", t.Card(d.Card("t3")));
        }

        // ── E29 엘다인 설계 규칙 검사기 ──
        static string Orc(string hero, int n) => string.Join(", ", Enumerable.Range(1, 5).Select(i => $"{{name:'{hero}{n}o{i}', fx:[{{k:'dmg', ratio:{1.0 + i * 0.1:0.0}, target:'oneEnemy'}}]}}"));
        /// <summary>규칙에 맞는 엘다인 한 명(id 는 엘다인 이름) — 기본 3 · 시동 + 고유 4(완성형 없음 · 모두 신탁 5개).</summary>
        internal static (string hero, string cards) Eld(string id, string role = "탱커", string kw = null, string uniqueFx = null, string rules = "", string forms = "", string basicExtra = "", string starterExtra = "", int uniques = 5, bool complete = false, bool opener = true)
        {
            kw ??= id + "의 마음";
            string def = role == "탱커" ? ", base:'def'" : "";
            var c = new StringBuilder("[");
            c.Append($"{{id:'{id}_s1', name:'기본1', hero:'{id}', cost:1, type:'공격', fx:[{{k:'dmg', ratio:1{def}, target:'oneEnemy'}}]{basicExtra}}},");
            c.Append($"{{id:'{id}_s2', name:'기본2', hero:'{id}', cost:2, type:'공격', fx:[{{k:'dmg', ratio:2.2{def}, target:'oneEnemy'}}]}},");
            c.Append($"{{id:'{id}_s3', name:'기본3', hero:'{id}', cost:1, type:'스킬', fx:[{{k:'shield', ratio:1.4}}]}},");
            for (int u = 1; u <= uniques; u++)
            {
                bool comp = u == 5 && complete;
                string fx = u == 2 && uniqueFx != null ? uniqueFx : $"{{k:'dmg', ratio:1.3, target:'oneEnemy'}}, {{k:'stack', id:'{kw}', v:1}}";
                c.Append($"{{id:'{id}_u{u}', name:'고유{u}', hero:'{id}', unique:true, cost:1, type:'공격', fx:[{fx}]{(comp ? ", complete:true" : $", oracles:[{Orc(id, u)}]")}}},");
            }
            c.Append($"{{id:'{id}_t1', name:'{id} 토큰', hero:'{id}', token:true, cost:0, type:'공격', tags:['증발', '소멸'], fx:[{{k:'dmg', ratio:0.5, target:'oneEnemy'}}]}}");
            c.Append("]");
            string hero = $"{{id:'{id}', name:'{id}', role:'{role}', hp:650, atk:140, def:40, crit:10, keyword:{{name:'{kw}', cap:5, rules:[{rules}]}}, starter:['{id}_s1','{id}_s2','{id}_s3'{(opener ? $",'{id}_u1'" : "")}{starterExtra}], ult:{{name:'궁', cost:150, fx:[{{k:'dmg', ratio:2.2, target:'allEnemies'}}]}}{forms}}}";
            return (hero, c.ToString());
        }
        internal static List<string> EldErrors(params (string hero, string cards)[] hs)
        {
            var d = K.Data(heroes: "[" + string.Join(",", hs.Select(h => h.hero)) + "]");
            foreach (var h in hs) d.Add(null, h.cards, null, null, null, null);
            return Validator.Check(d).Errors.Where(e => e.StartsWith("엘다인", System.StringComparison.Ordinal)).ToList();
        }
        internal static void Has(List<string> errs, string part) => Assert.IsTrue(errs.Any(e => e.Contains(part)), $"「{part}」 오류가 없다:\n" + string.Join("\n", errs));

        [Test] public void E29_규칙에_맞으면_오류_없음()
        {
            var errs = EldErrors(Eld("이드"), Eld("아야", "딜러"));
            Assert.AreEqual(0, errs.Count, string.Join("\n", errs));
        }

        [Test] public void E29_엘다인은_완성형_없이_고유_5장_모두_신탁()
        {
            Assert.AreEqual(0, EldErrors(Eld("시온더다크불릿", "딜러"), Eld("아야", "딜러")).Count, "완성형 없이 5장 모두 신탁");
            Has(EldErrors(Eld("아야", "딜러", complete: true)), "완성형(complete)을 두지 않는다");
        }

        [Test] public void E29_엘다인이_아니면_안_본다()
        {
            var errs = EldErrors(Eld("아무개", basicExtra: ", tags:['소멸']"));
            Assert.AreEqual(0, errs.Count);
        }

        [Test] public void E29_기본_카드는_덧효과_없이()
        {
            Has(EldErrors(Eld("이드", basicExtra: ", tags:['보존']")), "기본 카드에 태그");
            var bad = Eld("이드");
            bad.cards = bad.cards.Replace("fx:[{k:'dmg', ratio:1, base:'def', target:'oneEnemy'}]", "fx:[{k:'dmg', ratio:1, base:'def', target:'oneEnemy'}, {k:'draw', v:1}]");
            Has(EldErrors(bad), "효과 조각 하나");
            var bad2 = Eld("이드");
            bad2.cards = bad2.cards.Replace("fx:[{k:'dmg', ratio:1, base:'def', target:'oneEnemy'}]", "fx:[{k:'dmg', ratio:1, base:'def', hits:2, target:'oneEnemy'}]");
            Has(EldErrors(bad2), "덧효과 · 조건을 달지 않는다");
        }

        [Test] public void E29_기본_카드는_가벼운_덱_무거운_덱()
        {
            const string S2 = "{id:'이드_s2', name:'기본2', hero:'이드', cost:2, type:'공격', fx:[{k:'dmg', ratio:2.2, base:'def', target:'oneEnemy'}]}";
            const string S3 = "{id:'이드_s3', name:'기본3', hero:'이드', cost:1, type:'스킬', fx:[{k:'shield', ratio:1.4}]}";
            (string, string) With(string from, string to) { var e = Eld("이드"); e.cards = e.cards.Replace(from, to); return e; }
            List<string> Basic(List<string> errs) => errs.Where(e => e.Contains("기본 카드") || e.Contains("기본 덱")).ToList();
            // 무거운 덱(1코 공격 · 2코 공격 · 1코 실드 각 1장) — Eld 기본
            Assert.AreEqual(0, Basic(EldErrors(Eld("이드"))).Count);
            // 가벼운 덱은 같은 1코 공격 2장(2026-10-11 사용자 — Eldain5Tests) · 서로 다른 1코 공격 두 종은 아니다
            Has(EldErrors(With(S2, "{id:'이드_s2', name:'기본2', hero:'이드', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, base:'def', target:'oneEnemy'}]}")), "기본 덱은 가벼운");
            // 2코 실드(옛 허용)도 이제 아니다
            Has(EldErrors(With(S3, "{id:'이드_s3', name:'기본3', hero:'이드', cost:2, type:'스킬', fx:[{k:'shield', ratio:2.5}]}")), "기본 덱은 가벼운");
            // 어긋난 것
            Has(EldErrors(With(S3, "{id:'이드_s3', name:'기본3', hero:'이드', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, base:'def', target:'oneEnemy'}]}")), "기본 덱은 가벼운");
            Has(EldErrors(With(S2, "{id:'이드_s2', name:'기본2', hero:'이드', cost:2, type:'공격', fx:[{k:'dmg', ratio:3, base:'def', target:'oneEnemy'}]}")), "220% 근처");
            Has(EldErrors(With(S2, "{id:'이드_s2', name:'기본2', hero:'이드', cost:3, type:'공격', fx:[{k:'dmg', ratio:2.2, base:'def', target:'oneEnemy'}]}")), "기본 덱은 가벼운");
            Has(EldErrors(With(S2, "{id:'이드_s2', name:'기본2', hero:'이드', cost:2, type:'공격', fx:[{k:'dmg', ratio:2.2, target:'oneEnemy'}]}")), "방어 기반");
        }

        [Test] public void E29_시작_덱과_고유_5장()
        {
            Has(EldErrors(Eld("이드", starterExtra: ",'이드_u2'")), "시동)는 꼭 1장");
            Has(EldErrors(Eld("이드", uniques: 3)), "고유 카드는 4~5장");
            // 사용자 2026-10-11 — 시작 덱 = 기본 3 + 시동 1(정확히 4장) · 고유 4~5
            Has(EldErrors(Eld("이드", opener: false)), "시동)는 꼭 1장");
            var u3 = Eld("이드"); u3.hero = u3.hero.Replace(",'이드_u1']", ",'이드_u3']");
            Assert.AreEqual(0, EldErrors(u3).Count, "고유 카드 가운데 아무 것이나 시동");
            Assert.AreEqual(0, EldErrors(Eld("이드", uniques: 4)).Count, "고유 4장");
            var d = K.Data(heroes: "[" + Eld("이드", opener: false).hero + "]"); d.Add(null, Eld("이드", opener: false).cards, null, null, null, null);
            Assert.AreEqual(5, d.GraceUniquesOf("이드").Count, "시동이 없으면 고유 카드가 모두 은총 후보");
            var d2 = K.Data(heroes: "[" + Eld("이드").hero + "]"); d2.Add(null, Eld("이드").cards, null, null, null, null);
            Assert.AreEqual(4, d2.GraceUniquesOf("이드").Count, "시동(시작 덱의 고유 카드)은 은총 후보에서 빠진다");
        }

        [Test] public void E29_퍼센트_단위()
        {
            Has(EldErrors(Eld("이드", uniqueFx: "{k:'dmg', ratio:1.33, target:'oneEnemy'}")), "10% 단위");
            Assert.AreEqual(0, EldErrors(Eld("이드", uniqueFx: "{k:'dmg', ratio:0.15, target:'oneEnemy'}")).Count, "20% 미만은 5% 단위");
        }

        [Test] public void E29_딜러는_실드를_주지_않는다()
        {
            Has(EldErrors(Eld("시온더다크불릿", "딜러", uniqueFx: "{k:'shield', ratio:1.4}")), "딜러는 실드");
            Has(EldErrors(Eld("시온더다크불릿", "딜러", uniqueFx: "{k:'status', id:'불굴', v:1}")), "딜러는 실드");
        }

        [Test] public void E29_전부_써서는_사도_수_한도_없음()
        {
            // 2026-10-11 카제나 재정렬로 3명까지였다가 2026-10-18 사용자 결정으로 한도를 없앴다 — 넷이어도 오류 없음
            string all(string kw) => $"{{k:'spend', id:'{kw}', all:true}}, {{k:'perEvent'}}, {{k:'dmg', ratio:0.5, target:'oneEnemy'}}";
            var ids = new[] { "아야", "란", "요미", "티그_영웅" };
            Assert.IsFalse(EldErrors(ids.Select(id => Eld(id, uniqueFx: all(id + "의 마음"))).ToArray()).Any(e => e.Contains("전부 써서")));
        }

        [Test] public void E29_자동_추가타는_하나_같은_계기는_하나로()
        {
            string r2 = "{name:'자동1', when:{on:'turnEnd'}, fx:[{k:'dmg', ratio:0.5, target:'randomEnemy'}]}, {name:'자동2', when:{on:'play'}, fx:[{k:'extra', ratio:0.5}]}";
            Has(EldErrors(Eld("이드", rules: r2)), "자동 피해");
            // E27 — 같은 계기(턴 끝)를 두 줄(보름이면 ×2)로 쓴 것은 하나로 센다
            string same = "{name:'꽃', when:{on:'turnEnd'}, fx:[{k:'dmg', ratio:0.5, target:'randomEnemy'}]}, {name:'꽃 보름', when:{on:'turnEnd'}, fx:[{k:'ifStack', id:'이드의 마음', n:3}, {k:'dmg', ratio:0.5, target:'randomEnemy'}]}";
            Assert.IsFalse(EldErrors(Eld("이드", rules: same)).Any(e => e.Contains("자동 피해")));
        }

        [Test] public void E29_횟수_제한_변신_이름()
        {
            Has(EldErrors(Eld("이드", rules: "{name:'제한', when:{on:'play'}, limit:{per:'turn', n:1}, fx:[{k:'stack', id:'이드의 마음', v:1}]}")), "횟수 제한");
            Has(EldErrors(Eld("이드", forms: ", forms:[{id:'이드_f', name:'꿈', turns:2, cards:{이드_s1:'이드_t1'}, turnMake:[{id:'이드_t1', n:1}]}]")), "카드 바꿔치기");
            Has(EldErrors(Eld("이드", forms: ", forms:[{id:'이드_f', name:'꿈', turns:2, mods:{atk:0.1}}]")), "turnMake 가 없다");
            var notok = Eld("이드", forms: ", forms:[{id:'이드_f', name:'꿈', turns:2, turnMake:[{id:'이드_t1', n:1}]}]");
            notok.cards = notok.cards.Replace("tags:['증발', '소멸']", "tags:['소멸']");
            Has(EldErrors(notok), "「증발」 · 「소멸」");
            Has(EldErrors(Eld("이드", kw: "시온더다크불릿의 꿈"), Eld("시온더다크불릿", "딜러")), "다른 사도 이름");
            Assert.IsFalse(EldErrors(Eld("이드", kw: "이드 더 이터널 불릿")).Any(e => e.Contains("다른 사도 이름")), "자기 이름은 된다");
        }
    }
}
