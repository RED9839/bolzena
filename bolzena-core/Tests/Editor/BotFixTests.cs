using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 18갈래 3단계 뒤 봇 손질(2026-10-08) — 묶음 에이전트들이 의심한 봇 셈을 판으로 확인한다.
    /// 넘친 회복 · 다음 카드 강화를 걸 카드 · 다음 턴 AP 치르기 · HP 30% 위기 패시브 · 아군 주는 피해 + 버프.
    /// </summary>
    public class BotFixTests
    {
        const string CARDS = @"[
 {id:'ovh', name:'넘치는 처방', hero:'a', cost:1, type:'스킬', fx:[{k:'heal', ratio:4}, {k:'perOverheal', pct:0.8, per:40}, {k:'dmg', ratio:0.5, target:'oneEnemy'}]},
 {id:'healx', name:'큰 회복', hero:'a', cost:1, type:'스킬', fx:[{k:'heal', ratio:4}]},
 {id:'hita', name:'가 치기', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}]},
 {id:'emp', name:'북돋기', hero:'c', cost:0, type:'스킬', fx:[{k:'empower', ratio:1}]},
 {id:'bigc', name:'큰 한 방', hero:'c', cost:2, type:'공격', fx:[{k:'dmg', ratio:2, target:'oneEnemy'}]},
 {id:'smallc', name:'잔 펀치', hero:'c', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.4, target:'oneEnemy'}]},
 {id:'draw0', name:'뒤적이기', hero:'c', cost:0, type:'스킬', fx:[{k:'draw', v:1}]},
 {id:'payc', name:'내일을 당겨 쓰기', hero:'c', cost:1, type:'공격', fx:[{k:'dmg', ratio:2.5, target:'oneEnemy'}, {k:'nextAp', v:-1}]},
 {id:'hitc', name:'다 치기', hero:'c', cost:1, type:'공격', fx:[{k:'dmg', ratio:1, target:'oneEnemy'}]},
 {id:'rally', name:'함성', hero:'a', cost:0, type:'스킬', fx:[{k:'dealtMod', v:0.5, target:'allAllies', turns:1}]},
 {id:'rally1', name:'비싼 함성', hero:'a', cost:1, type:'스킬', fx:[{k:'dealtMod', v:0.5, target:'allAllies', turns:1}]}
]";
        const string OVHERO = "[{id:'o', name:'넘침이', role:'서포터', hp:1000, atk:100, def:50, passives:[{name:'넘침 공격', when:{on:'overheal', pct:0.8}, fx:[{k:'dmg', ratio:1, ofEvent:1, target:'oneEnemy'}]}]}]";
        const string OCARDS = "[{id:'oheal', name:'넘침이 회복', hero:'o', cost:1, type:'스킬', fx:[{k:'heal', ratio:4}]}, {id:'ohit', name:'넘침이 치기', hero:'o', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.2, target:'oneEnemy'}]}]";

        static GameData D() => K.Data(cards: CARDS);
        static Battle F(GameData d, params string[] party) => K.Fight(d, party, new[] { "dummy" });
        static int Dealt(Battle b) => b.Enemies[0].MaxHp - b.Enemies[0].Hp;

        [Test] public void 넘친_회복을_바꾸는_카드는_HP_가_차도_낸다()
        {
            var d = D(); var b = F(d, "a");
            K.Hand(b, "ovh", "hita"); b.Ap = 1;
            new Bots(d).SmartPlay(b);
            Assert.IsFalse(b.Hand.Contains("ovh"), "HP 가 가득이면 회복 200 이 다 넘쳐 0.5 × 5대(250) — 1.2 한 대(120)보다 크다");
        }

        [Test] public void 넘친_회복_패시브가_있으면_회복_카드도_값이_있다()
        {
            var d = K.Data(heroes: OVHERO, cards: CARDS).Add(null, OCARDS, null, null, null, null);
            var b = F(d, "o");
            K.Hand(b, "oheal", "ohit"); b.Ap = 1;
            new Bots(d).SmartPlay(b);
            Assert.IsFalse(b.Hand.Contains("oheal"), "넘친 회복 200 → 피해 200 이 1.2 한 대(120)보다 크다");
        }

        [Test] public void 다음_카드_강화는_큰_카드에_건다()
        {
            var d = D(); var b = F(d, "c");
            K.Hand(b, "emp", "smallc", "bigc"); b.Ap = 3;
            var tr = new System.Text.StringBuilder();
            new Bots(d).SmartPlay(b, trace: (m, v, st) => tr.Append($"{m} {v:0}/{st:0}; "));
            Assert.GreaterOrEqual(Dealt(b), 600, tr + " emp=" + b.EmpowerOf("c") + " | " + "북돋기 → 큰 한 방(2 × 2.0) → 잔 펀치 — 잔 펀치에 쓰면 420");
        }

        [Test] public void 걸린_강화를_피해_없는_카드에_흘리지_않는다()
        {
            var d = D(); var b = F(d, "c");
            K.Hand(b, "emp"); b.Ap = 2; K.Play(b, "emp");
            K.Hand(b, "draw0", "bigc");
            new Bots(d).SmartPlay(b);
            Assert.GreaterOrEqual(Dealt(b), 600, "강화는 큰 한 방에");
        }

        [Test] public void 다음_턴_AP_를_치르는_카드도_셈이_맞으면_낸다()
        {
            var d = D(); var b = F(d, "c");
            K.Hand(b, "payc", "hitc"); b.Ap = 1;
            new Bots(d).SmartPlay(b);
            Assert.IsFalse(b.Hand.Contains("payc"), "지금 2.5 대 · 다음 턴 AP 1(카드 한 장 ≈ 1.0)");
        }

        [Test] public void 피해_버프는_칠_카드가_남았을_때_먼저()
        {
            var d = D(); var b = F(d, "a", "c");
            K.Hand(b, "bigc", "rally"); b.Ap = 2;
            var tr = new System.Text.StringBuilder();
            new Bots(d).SmartPlay(b, trace: (m, v, st) => tr.Append($"{m} {v:0}/{st:0}; "));
            Assert.GreaterOrEqual(Dealt(b), 450, tr + " | " + "함성(0) → 큰 한 방 ×1.5");
        }

        [Test] public void 피해_버프는_칠_카드가_없으면_값이_작다()
        {
            var d = D(); var b = F(d, "a", "c");
            K.Hand(b, "rally1", "hitc"); b.Ap = 1;
            new Bots(d).SmartPlay(b);
            Assert.Greater(Dealt(b), 0, "AP 1 — 이번 턴만 가는 함성보다 한 대가 낫다");
        }
        [Test] public void 위기_패시브_회복을_예상_HP_에_넣는다()
        {
            var d = K.Data(heroes: "[{id:'w', name:'버팀이', role:'탱커', hp:1000, atk:100, def:50, passives:[{name:'위기 회복', when:{on:'lowHp', pct:0.3}, fx:[{k:'heal', ratio:6}]}]}]");
            var b = K.Fight(d, new[] { "w" }, new[] { "hitter" });
            b.Pool.Hp = 350;
            var bot = new Bots(d);
            b.Fired["w|0|f"] = 1;
            int bare = bot.Incoming(b).hp;
            Assert.Less(bare, 300, "맞으면 30% 아래로 — 이미 쓴 위기 패시브는 세지 않는다");
            b.Fired.Clear();
            Assert.AreEqual(bare + 300, bot.Incoming(b).hp, 1, "선을 넘어 내려가면 위기 회복 300(방어 50 × 6)을 더한다");
            b.Pool.Hp = 900; int high = bot.Incoming(b).hp;
            Assert.AreEqual(900 - (350 - bare), high, 1, "선을 안 넘으면 그대로");
            b.Pool.Hp = 900;
        }
    }
}
