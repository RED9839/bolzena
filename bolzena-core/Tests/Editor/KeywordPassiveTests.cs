using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>사도 전용 키워드(docs/18 §6) · 패시브(§7) · 공용 부품(docs/19 §7 — 리듬 · 전환 · 재촉).</summary>
    public class KeywordPassiveTests
    {
        static GameData Hero(string heroJson, string cards = null, string enemies = null) => K.Data(heroes: "[" + heroJson + "]", cards: cards, enemies: enemies);

        // ── 키워드 ──
        [Test] public void 자기_주머니_1개당_피해_발동하면_사라진다()
        {
            var d = Hero("{id:'k', name:'장전수', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'장전', carrier:'self', cap:3, consumeAll:true, per:[{stat:'dealt', v:0.5}]}}",
                "[{id:'load', name:'장전', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'장전', v:5}]}, {id:'shoot', name:'쏘기', hero:'k', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "k" }, new[] { "dummy" }); K.Hand(b, "load", "shoot", "shoot");
            K.Play(b, "load");
            Assert.AreEqual(3, b.StackOf("k", "장전"), "최대 3");
            K.Play(b, "shoot");
            Assert.AreEqual(1000 - 250, K.Hp(b));
            Assert.AreEqual(0, b.StackOf("k", "장전"));
            K.Play(b, "shoot");
            Assert.AreEqual(750 - 100, K.Hp(b));
        }

        [Test] public void 치명만_올리는_키워드는_치명타가_터졌을_때만_준다()
        {
            var d = Hero("{id:'k', name:'저격수', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'조준', carrier:'self', cap:5, consume:1, per:[{stat:'crit', v:0.2}]}}",
                "[{id:'aim', name:'조준', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'조준', v:5}]}, {id:'shoot', name:'쏘기', hero:'k', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "k" }, new[] { "dummy" }); K.Hand(b, "aim", "shoot");
            K.Play(b, "aim");
            K.Play(b, "shoot");
            Assert.AreEqual(1000 - 150, K.Hp(b), "치명 100% — 150%");
            Assert.AreEqual(4, b.StackOf("k", "조준"));
        }

        [Test] public void 적에게_거는_표식_받는_피해와_턴_끝_피해와_닳기()
        {
            var d = Hero("{id:'k', name:'전하', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'이온', carrier:'enemy', cap:5, decay:1, per:[{stat:'taken', v:0.1}, {stat:'dot', ratio:0.2}]}}",
                "[{id:'ion', name:'이온', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'이온', v:2}]}, {id:'shoot', name:'쏘기', hero:'k', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "k" }, new[] { "dummy", "dummy" }); K.Hand(b, "ion", "shoot");
            K.Play(b, "ion", 1);
            Assert.AreEqual(2, b.St(b.Enemies[1], "이온"));
            K.Play(b, "shoot", 1);
            Assert.AreEqual(1000 - 120, K.Hp(b, 1));
            b.EndTurn();
            Assert.AreEqual(880 - 48, K.Hp(b, 1), "턴 끝 1개당 공격력 20% = 40, 받는 피해 +20% 를 탄다");
            Assert.AreEqual(1, b.St(b.Enemies[1], "이온"), "적의 차례가 끝나면 1 감소(다음 내 턴 시작에)");
        }

        [Test] public void 아군에게_거는_표식은_파티에_하나_턴_끝_회복()
        {
            var d = Hero("{id:'k', name:'장막', role:'서포터', row:'mid', hp:800, atk:80, def:50, crit:0, keyword:{name:'장막', carrier:'ally', cap:3, per:[{stat:'hot', ratio:0.2}]}}",
                "[{id:'veil', name:'장막', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'장막', v:2}]}]");
            var b = K.Fight(d, new[] { "k", "a" }, new[] { "dummy" }, st => st.PartyHp = 1000); K.Hand(b, "veil");
            K.Play(b, "veil");
            Assert.AreEqual(2, b.St(b.Pool, "장막"));
            b.EndTurn();
            Assert.AreEqual(1000 + 20, b.Pool.Hp, "50 × 20% × 2");
        }

        [Test] public void N개가_되면_넘치게_쌓으면_두_번_터진다_카드_만들기()
        {
            var d = Hero("{id:'k', name:'개구리', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'개굴', carrier:'self', cap:3, rules:[{name:'개굴', when:{on:'stackReach', id:'개굴', n:3}, fx:[{k:'spend', id:'개굴', all:true}, {k:'make', id:'frog', v:1}]}]}}",
                "[{id:'croak', name:'개굴', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'개굴', v:6}]}, {id:'frog', name:'개구리 한 마리', hero:'k', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "k" }, new[] { "dummy" }, st => st.Flash = new Dictionary<string, int>()); K.Hand(b, "croak");
            K.Play(b, "croak");
            Assert.AreEqual(2, b.Hand.Count(x => x == "frog" + GameData.PLAIN));
            Assert.AreEqual(0, b.StackOf("k", "개굴"));
            Assert.AreEqual("개구리 한 마리", b.CardOf("frog~").Name);
        }

        [Test] public void 다른_사도의_카드를_내면_전부_사라지고_사라지면_규칙()
        {
            var d = Hero("{id:'k', name:'쌍검', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'쌍검', carrier:'self', cap:3, wipe:true, rules:[{name:'쌍검', when:{on:'stackGone', id:'쌍검'}, fx:[{k:'gauge', v:15}]}]}}",
                "[{id:'cut', name:'베기', hero:'k', cost:0, type:'공격', fx:[{k:'stack', id:'쌍검', v:1}]}]");
            var b = K.Fight(d, new[] { "k", "a" }, new[] { "dummy" }); K.Hand(b, "cut", "cut", "zero");
            K.Play(b, "cut"); K.Play(b, "cut");
            Assert.AreEqual(2, b.StackOf("k", "쌍검"));
            K.Play(b, "zero");
            Assert.AreEqual(0, b.StackOf("k", "쌍검"));
            Assert.AreEqual(15, b.Gauge);
        }

        [Test] public void 다_닳으면은_닳아서_0이_됐을_때만()
        {
            var d = Hero("{id:'k', name:'시계', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'시계', carrier:'self', cap:3, decay:1, rules:[{name:'째깍', when:{on:'stackGone', id:'시계', decay:true}, fx:[{k:'gauge', v:40}]}]}}",
                "[{id:'tick', name:'감기', hero:'k', cost:0, type:'스킬', fx:[{k:'stack', id:'시계', v:1}]}, {id:'use', name:'쓰기', hero:'k', cost:0, type:'스킬', fx:[{k:'spend', id:'시계', all:true}]}]");
            var b = K.Fight(d, new[] { "k" }, new[] { "dummy" }); K.Hand(b, "tick", "use");
            K.Play(b, "tick"); K.Play(b, "use");
            Assert.AreEqual(0, b.Gauge, "써서 0 이 된 것은 다 닳은 게 아니다");
            K.Hand(b, "tick"); K.Play(b, "tick");
            b.EndTurn();
            Assert.AreEqual(40, b.Gauge);
        }

        [Test] public void 모드_전환과_전환_조건과_전환하면()
        {
            var d = K.Data(heroes: @"[
 {id:'m', name:'두 얼굴', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, keyword:{name:'꿈', carrier:'self', mode:true, rules:[{name:'꿈', when:{on:'stackGone', id:'꿈'}, fx:[{k:'block', ratio:1.0}]}]},
   passives:[{name:'깸', when:{on:'switch'}, fx:[{k:'gauge', v:5}]}]},
 {id:'w', name:'구경꾼', role:'탱커', row:'front', hp:900, atk:80, def:60, crit:0, passives:[{name:'맞장구', when:{on:'switch', who:'any'}, fx:[{k:'gauge', v:1}]}]}]",
                cards: "[{id:'flip', name:'뒤집기', hero:'m', cost:0, type:'스킬', fx:[{k:'flip'}]}, {id:'then', name:'그러면', hero:'w', cost:0, type:'스킬', fx:[{k:'ifSwitched'}, {k:'gauge', v:100}]}]");
            var b = K.Fight(d, new[] { "m", "w" }, new[] { "dummy" }); K.Hand(b, "then", "flip", "flip", "then");
            K.Play(b, "then");
            Assert.AreEqual(0, b.Gauge);
            K.Play(b, "flip");
            Assert.AreEqual(1, b.StackOf("m", "꿈"));
            Assert.AreEqual(6, b.Gauge, "전환하면 5 · 아군이 전환하면 1");
            K.Play(b, "flip");
            Assert.AreEqual(0, b.StackOf("m", "꿈"));
            Assert.AreEqual(12, b.Gauge);
            Assert.AreEqual(20, b.Pool.Block, "「꿈」이 사라지면");
            K.Play(b, "then");
            Assert.AreEqual(112, b.Gauge);
            b.EndTurn();
            K.Hand(b, "then"); K.Play(b, "then");
            Assert.AreEqual(112, b.Gauge, "전환: 은 이번 턴만");
        }

        [Test] public void 예약과_재촉과_아군의_예약이_다_닳으면()
        {
            var d = K.Data(heroes: @"[
 {id:'s', name:'씨앗', role:'서포터', row:'mid', hp:600, atk:80, def:50, crit:0, keyword:{name:'씨앗', carrier:'self', cap:3, decay:1, reserve:true, rules:[{name:'씨앗', when:{on:'stackGone', id:'씨앗', decay:true}, fx:[{k:'gauge', v:10}]}]}},
 {id:'t', name:'결말', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, passives:[{name:'결말', when:{on:'reserveGone'}, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]}]",
                cards: "[{id:'plant', name:'심기', hero:'s', cost:0, type:'스킬', fx:[{k:'stack', id:'씨앗', v:2}]}, {id:'rush', name:'재촉', hero:'s', cost:0, type:'스킬', fx:[{k:'hasten', v:2}]}]");
            var b = K.Fight(d, new[] { "s", "t" }, new[] { "dummy" }); K.Hand(b, "plant", "rush");
            K.Play(b, "plant");
            K.Play(b, "rush");
            Assert.AreEqual(0, b.StackOf("s", "씨앗"));
            Assert.AreEqual(10, b.Gauge, "재촉으로 0 이 된 것도 다 닳은 것");
            Assert.AreEqual(900, K.Hp(b), "아군의 예약이 다 닳으면 — 결말");
        }

        // ── 리듬 ──
        [Test] public void 리듬_주기_1개당_조건_소모_리듬이_N이_되면()
        {
            var d = K.Data(heroes: "[{id:'r', name:'박자', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, passives:[{name:'박자', when:{on:'rhythm', n:3}, fx:[{k:'gauge', v:7}]}]}]",
                cards: @"[{id:'beat', name:'박', hero:'r', cost:0, type:'스킬', fx:[{k:'status', id:'리듬', v:2}]},
 {id:'per', name:'리듬 쏘기', hero:'r', cost:0, type:'공격', fx:[{k:'perRhythm'}, {k:'dmg', ratio:0.3, target:'oneEnemy'}, {k:'spendRhythm', all:true}]},
 {id:'if3', name:'세 박자', hero:'r', cost:0, type:'스킬', fx:[{k:'ifRhythm', n:3}, {k:'gauge', v:50}]}]");
            var b = K.Fight(d, new[] { "r" }, new[] { "dummy" }); K.Hand(b, "if3", "beat", "beat", "if3", "per", "beat", "beat");
            K.Play(b, "if3"); Assert.AreEqual(0, b.Gauge);
            K.Play(b, "beat"); K.Play(b, "beat");
            Assert.AreEqual(4, b.St(b.Pool, R.RHYTHM));
            Assert.AreEqual(7, b.Gauge, "리듬이 3이 되면");
            K.Play(b, "if3"); Assert.AreEqual(57, b.Gauge);
            K.Play(b, "per");
            Assert.AreEqual(1000 - 120, K.Hp(b), "리듬 4 × 30%");
            Assert.AreEqual(0, b.St(b.Pool, R.RHYTHM));
            K.Play(b, "beat"); K.Play(b, "beat");
            Assert.AreEqual(57, b.Gauge, "리듬이 N이 되면은 턴마다 한 번");
        }

        // ── 패시브 ──
        [Test] public void 카드를_N장_낼_때마다_장수는_조건과_상관없이_센다_0코는_안_센다()
        {
            var d = K.Data(heroes: "[{id:'p', name:'셈', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, passives:[{name:'셋째', when:{on:'play', every:3}, fx:[{k:'gauge', v:30}]}]}]",
                cards: "[{id:'p1', name:'한 장', hero:'p', cost:1, type:'스킬', fx:[]}, {id:'p0', name:'공짜', hero:'p', cost:0, type:'스킬', fx:[]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "p1", "p0", "p1", "p1");
            K.Play(b, "p1"); K.Play(b, "p0"); K.Play(b, "p1");
            Assert.AreEqual(20, b.Gauge);
            K.Play(b, "p1");
            Assert.AreEqual(30 + 30, b.Gauge);
        }

        [Test] public void 차례로_내면()
        {
            var d = K.Data(heroes: "[{id:'p', name:'코스', role:'탱커', row:'front', hp:900, atk:80, def:60, crit:0, passives:[{name:'풀코스', when:{on:'play', who:'any', seq:['공격','스킬','강화']}, fx:[{k:'gauge', v:100}]}]}]",
                cards: "[{id:'pa', name:'공', hero:'p', cost:0, type:'공격', fx:[]}, {id:'ps', name:'스', hero:'p', cost:0, type:'스킬', fx:[]}, {id:'pp', name:'강', hero:'p', cost:0, type:'강화', fx:[]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "pa", "pp", "pa", "ps", "pp");
            K.Play(b, "pa"); K.Play(b, "pp");
            Assert.AreEqual(0, b.Gauge);
            K.Play(b, "pa"); K.Play(b, "ps");
            Assert.AreEqual(2, b.SeqStep(b.Passives["p"][0].R.When, "p"));
            K.Play(b, "pp");
            Assert.AreEqual(100, b.Gauge);
        }

        [Test] public void 피해를_받으면_턴당_한_번_HP가_N퍼센트_이하가_되면_전투당_한_번()
        {
            var d = K.Data(heroes: "[{id:'p', name:'버팀', role:'탱커', row:'front', hp:1000, atk:80, def:60, crit:0, passives:[{name:'맞불', when:{on:'hurt'}, limit:{per:'turn', n:1}, fx:[{k:'status', id:'불굴', v:1}]}, {name:'고비', when:{on:'lowHp', pct:0.5}, fx:[{k:'status', id:'결정화', v:1}]}]}]",
                enemies: "[{id:'m', name:'연타', hp:1000, intents:[{t:'multi', v:200, n:3, rush:0}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "m" });
            b.EndTurn();
            Assert.AreEqual(1, b.St(b.Pool, "불굴"));
            Assert.AreEqual(1000 - 200 - 160 - 160, b.Pool.Hp);
            Assert.AreEqual(1, b.St(b.Pool, "결정화"));
        }

        [Test] public void 적을_격파하면_처치하면_디버프를_걸면_한_번의_일에_한_번()
        {
            var d = K.Data(heroes: "[{id:'p', name:'사냥꾼', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, passives:[{name:'격파', when:{on:'break', mine:true}, fx:[{k:'gauge', v:1}]}, {name:'처치', when:{on:'kill', mine:true}, fx:[{k:'gauge', v:10}]}, {name:'흠', when:{on:'debuff'}, fx:[{k:'gauge', v:100}]}]}]",
                cards: "[{id:'all', name:'모두', hero:'p', cost:0, type:'스킬', fx:[{k:'status', id:'취약', v:1, target:'allEnemies'}, {k:'tough', v:9, target:'allEnemies'}]}, {id:'kill', name:'끝', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:9.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy", "dummy" }); K.Hand(b, "all", "kill");
            K.Play(b, "all");
            Assert.AreEqual(100 + 1 + 1, b.Gauge);
            K.Play(b, "kill", 0);
            Assert.AreEqual(112, b.Gauge);
        }

        [Test] public void 방어나_실드를_얻으면_스스로를_다시_부르지_않는다()
        {
            var d = K.Data(heroes: "[{id:'p', name:'방패', role:'탱커', row:'front', hp:900, atk:80, def:50, crit:0, passives:[{name:'막아 서기', when:{on:'guard'}, fx:[{k:'block', ratio:0.2}]}]}]",
                cards: "[{id:'g', name:'막기', hero:'p', cost:0, type:'스킬', fx:[{k:'block', ratio:2.0}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "g");
            K.Play(b, "g");
            Assert.AreEqual(100 + 10, b.Pool.Block);
        }

        [Test] public void 항상과_조건()
        {
            var d = K.Data(heroes: "[{id:'p', name:'늘', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, passives:[{name:'위기', when:{on:'always'}, conds:[{c:'hp', pct:0.5}], fx:[{k:'dealtMod', v:0.5}]}]}]",
                cards: "[{id:'ph', name:'때림', hero:'p', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" }); K.Hand(b, "ph", "ph");
            K.Play(b, "ph");
            Assert.AreEqual(900, K.Hp(b));
            b.Pool.Hp = 400;
            K.Play(b, "ph");
            Assert.AreEqual(900 - 150, K.Hp(b));
        }

        [Test] public void 턴_시작_첫_턴이면_턴_종료_AP가_남았으면()
        {
            var d = K.Data(heroes: "[{id:'p', name:'여유', role:'서포터', row:'mid', hp:600, atk:80, def:50, crit:0, passives:[{name:'첫 수', when:{on:'turnStart'}, conds:[{c:'firstTurn'}], fx:[{k:'status', id:'협공', v:2}]}, {name:'여운', when:{on:'turnEnd'}, conds:[{c:'apLeft', n:2}], fx:[{k:'status', id:'저장', v:1}]}]}]");
            var b = K.Fight(d, new[] { "p" }, new[] { "dummy" });
            Assert.AreEqual(2, b.St(b.Pool, "협공"));
            b.EndTurn();
            Assert.AreEqual(2, b.St(b.Pool, "협공"), "첫 턴만");
            Assert.AreEqual(6, b.Ap, "남긴 3 을 저장이 가져왔다");
        }

        [Test] public void 장비_효과는_낀_사도의_패시브로()
        {
            var d = K.Data();
            var rule = new PassiveRule { Name = "행운", When = new When { On = "turnStart" }, Fx = new List<Fx> { new Fx { K = "gauge", V = 11 } } };
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.GearRules = new Dictionary<string, List<PassiveRule>> { ["a"] = new List<PassiveRule> { rule } });
            Assert.AreEqual(11, b.Gauge);
        }

        [Test] public void 신탁_카드는_바로_바뀌고_이번엔_공짜_은총은_고유_카드를_손에()
        {
            var d = K.Data(cards: @"[{id:'u1', name:'고유', hero:'a', cost:2, type:'공격', unique:true, fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}],
  oracles:[{name:'하나', fx:[{k:'dmg', ratio:2.0, target:'oneEnemy'}]}, {name:'둘', fx:[]}, {name:'셋', fx:[]}, {name:'넷', fx:[]}, {name:'다섯', fx:[]}]},
 {id:'u2', name:'새 고유', hero:'a', cost:2, type:'스킬', unique:true, fx:[]}]");
            var glow = new Dictionary<string, Glow>
            {
                ["u1"] = new Glow { Kind = "card", Picks = new List<GlowPick> { new GlowPick { N = 1 }, new GlowPick { N = 2, Shin = "power" } } },
                ["zero"] = new Glow { Kind = "hero", Hero = "a", Options = new List<string> { "u2" } },
            };
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Glow = glow); K.Hand(b, "u1", "zero");
            Assert.AreEqual("card", b.ApplyEpiphany("u1", 0));
            Assert.AreEqual(0, b.CostOf("u1"));
            K.Play(b, "u1");
            Assert.AreEqual(800, K.Hp(b));
            Assert.AreEqual(3, b.Ap);
            Assert.AreEqual(1, b.GainedFlash.Count);
            Assert.AreEqual("hero", b.ApplyEpiphany("zero", 0));
            CollectionAssert.Contains(b.Hand, "u2");
            Assert.AreEqual(0, b.CostOf("u2", b.Hand.IndexOf("u2")), "그 턴 공짜");
            CollectionAssert.Contains(b.GainedCards, "u2");
        }
    }
}
