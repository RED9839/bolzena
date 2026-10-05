using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>전투의 뼈대 — AP · 드로우 · 손 · 파티 HP · 강인도/격파 · 즉시 행동 · 적의 수 · 성격 · 고학년.</summary>
    public class CombatTests
    {
        [Test] public void 턴마다_AP_3_드로우_5_남은_AP는_사라진다()
        {
            var d = K.Data();
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = Enumerable.Repeat("hit", 12).ToList());
            Assert.AreEqual(1, b.Turn); Assert.AreEqual(3, b.Ap); Assert.AreEqual(5, b.Hand.Count);
            K.Play(b, "hit");
            b.EndTurn();
            Assert.AreEqual(2, b.Turn); Assert.AreEqual(3, b.Ap, "남긴 AP 는 이월되지 않는다");
            Assert.AreEqual(5, b.Hand.Count);
        }

        [Test] public void 손은_열_장까지_넘치면_사라진다()
        {
            var d = K.Data(cards: "[{id:'draw9', name:'잔뜩', hero:'a', cost:0, type:'스킬', fx:[{k:'draw', v:9}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy" }, st => st.Deck = Enumerable.Repeat("zero", 20).ToList());
            K.Hand(b, "draw9", "zero", "zero", "zero", "zero");
            K.Play(b, "draw9");
            Assert.AreEqual(10, b.Hand.Count);
            Assert.AreEqual(3, b.Gone.Count);
        }

        [Test] public void 파티는_한_몸이다()
        {
            var b = K.Fight(K.Data(), new[] { "a", "b", "c" }, new[] { "hitter" });
            Assert.AreEqual(2400, b.Pool.MaxHp);
            Assert.AreEqual(b.Pool.Hp, b.Party[2].Hp);
            b.EndTurn();
            Assert.AreEqual(2300, b.Party[0].Hp);
            Assert.AreEqual(2300, b.Party[1].Hp);
        }

        [Test] public void 파티_HP가_0이면_진다()
        {
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "hitter" }, st => st.PartyHp = 50);
            b.EndTurn();
            Assert.AreEqual("lose", b.Over);
        }

        [Test] public void 적을_다_쓰러뜨리면_이긴다()
        {
            var b = K.Fight(K.Data(), new[] { "a" }, new[] { "dummy" }, st => st.EnemyHp = 0.05);
            K.Hand(b, "hit");
            K.Play(b, "hit");
            Assert.AreEqual("win", b.Over);
            Assert.IsFalse(b.PlayCard(0, 0).Ok);
        }

        // ── 강인도 · 격파 ──
        [Test] public void 강인도는_카드_한_장에_AP_1당_3분의1칸_0이면_격파_AP_1_그리고_다음_차례_행동_불가()
        {
            var d = K.Data(cards: "[{id:'smash', name:'깨기', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, hits:8, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "hitter" }); K.Hand(b, "smash", "smash");
            K.E(b).Tough = 0.5;
            K.Play(b, "smash");
            Assert.AreEqual(0.5 - 1.0 / 3, K.E(b).Tough, 1e-9, "여러 번 쳐도 카드 한 장에 한 번");
            b.Ap = 1;
            K.Play(b, "smash");
            Assert.IsTrue(K.E(b).Broken);
            Assert.AreEqual(0, K.E(b).Tough);
            Assert.AreEqual(1, b.Ap, "AP 1 을 쓰고 격파로 1 을 받았다");
            b.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp, "격파된 적은 그 차례에 못 움직인다");
            Assert.IsFalse(K.E(b).Broken, "다음 내 턴에 일어선다");
            Assert.AreEqual(4, K.E(b).Tough);
        }

        [Test] public void 약점_성격이면_AP_1당_한_칸_피해_25퍼센트()
        {
            var d = K.Data(heroes: "[{id:'cool', name:'냉정이', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, nature:'냉정'}]",
                cards: "[{id:'hit_cool', name:'냉정 때리기', hero:'cool', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}]",
                enemies: "[{id:'mad', name:'광기 적', hp:1000, nature:'광기', intents:[{t:'jam', v:0, rush:0}]}]");
            var b = K.Fight(d, new[] { "cool" }, new[] { "mad" }); K.Hand(b, "hit_cool");
            K.Play(b, "hit_cool");
            Assert.AreEqual(875, K.Hp(b));
            Assert.AreEqual(3, K.E(b).Tough);
        }

        [Test] public void 상성에서_밀리면_받는_피해가_5퍼센트_준다()
        {
            var d = K.Data(heroes: "[{id:'cool', name:'냉정이', role:'탱커', row:'front', hp:1000, atk:100, def:20, crit:0, nature:'냉정'}]",
                enemies: "[{id:'pure', name:'순수 적', hp:1000, nature:'순수', intents:[{t:'attack', v:100, rush:0}]}]");
            var b = K.Fight(d, new[] { "cool" }, new[] { "pure" });
            b.EndTurn();
            Assert.AreEqual(1000 - 110, b.Pool.Hp, "순수가 냉정을 이긴다 — 적이 +10%");
        }

        [Test] public void 모으는_수는_격파하면_끊긴다()
        {
            var d = K.Data(enemies: "[{id:'charger', name:'모으는 놈', hp:1000, intents:[{t:'charge', say:'모은다', brk:true, next:{t:'attack', v:300, say:'쾅'}}, {t:'attack', v:10}]}]",
                cards: "[{id:'brk', name:'깨기', hero:'a', cost:1, type:'공격', fx:[{k:'tough', v:4, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "charger" }); K.Hand(b, "brk");
            Assert.AreEqual("charge", K.E(b).Intent.T);
            K.Play(b, "brk");
            Assert.IsNull(K.E(b).Intent);
            b.EndTurn();
            b.EndTurn();
            Assert.Greater(b.Pool.Hp, 1000 - 300, "모은 300 이 오지 않았다");
        }

        [Test] public void 모은_힘은_다음_턴에_반드시()
        {
            var d = K.Data(enemies: "[{id:'charger', name:'모으는 놈', hp:1000, intents:[{t:'charge', say:'모은다', next:{t:'attack', v:300, say:'쾅'}}, {t:'jam', v:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "charger" });
            b.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp);
            Assert.AreEqual(300, K.E(b).Intent.V);
            Assert.AreEqual(0, b.RushOf(K.E(b)), "모아서 쏟는 수는 당겨지지 않는다");
            b.EndTurn();
            Assert.AreEqual(700, b.Pool.Hp);
        }

        // ── 즉시 행동 ──
        [Test] public void 카드를_수의_장수만큼_내면_적이_당겨서_하고_적의_차례엔_쉰다()
        {
            var d = K.Data(enemies: "[{id:'quick', name:'재촉꾼', hp:1000, intents:[{t:'attack', v:100, rush:3}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "quick" }); K.Hand(b, "zero", "zero", "zero", "zero");
            K.Play(b, "zero"); K.Play(b, "zero");
            Assert.AreEqual(1000, b.Pool.Hp);
            K.Play(b, "zero");
            Assert.AreEqual(900, b.Pool.Hp);
            Assert.IsTrue(b.RushedThisTurn);
            K.Play(b, "zero");
            Assert.AreEqual(900, b.Pool.Hp, "한 적은 한 턴에 한 번만 당겨진다");
            b.EndTurn();
            Assert.AreEqual(900, b.Pool.Hp, "당겨진 적은 적의 차례에 쉰다");
        }

        [Test] public void 신속_카드는_즉시_행동_셈을_늘리지_않는다()
        {
            var d = K.Data(enemies: "[{id:'quick', name:'재촉꾼', hp:1000, intents:[{t:'attack', v:100, rush:3}]}]",
                cards: "[{id:'swift', name:'빠른 숨', hero:'a', cost:0, type:'스킬', tags:['신속'], fx:[]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "quick" }); K.Hand(b, "swift", "swift", "swift");
            K.Play(b, "swift"); K.Play(b, "swift"); K.Play(b, "swift");
            Assert.AreEqual(1000, b.Pool.Hp);
            Assert.AreEqual(0, K.E(b).RushCnt);
        }

        [Test] public void 즉시_행동_장수는_수의_값어치로()
        {
            Assert.AreEqual(3, Battle.IntentRush(new Intent { T = "attack", V = 60 }, null, false));
            Assert.AreEqual(4, Battle.IntentRush(new Intent { T = "attack", V = 100 }, null, false));
            Assert.AreEqual(5, Battle.IntentRush(new Intent { T = "multi", V = 50, N = 4 }, null, false));
            Assert.AreEqual(6, Battle.IntentRush(new Intent { T = "attackAll", V = 100 }, null, false));
            Assert.AreEqual(3, Battle.IntentRush(new Intent { T = "block", V = 100 }, null, false));
            Assert.AreEqual(0, Battle.IntentRush(new Intent { T = "charge" }, null, false));
            Assert.AreEqual(3, Battle.IntentRush(new Intent { T = "attack", V = 500, Rush = 2 }, null, false), "적어도 3장");
            Assert.AreEqual(0, Battle.IntentRush(new Intent { T = "attack", V = 500, Rush = 0 }, null, false));
        }

        [Test] public void 즉시_행동_늦춤()
        {
            var d = K.Data(enemies: "[{id:'quick', name:'재촉꾼', hp:1000, intents:[{t:'attack', v:100, rush:3}]}]",
                cards: "[{id:'calm', name:'달래기', hero:'a', cost:0, type:'스킬', fx:[{k:'rushDown', v:2, target:'allEnemies'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "quick" }); K.Hand(b, "calm", "zero", "zero", "zero");
            K.Play(b, "calm");
            Assert.AreEqual(-1, K.E(b).RushCnt);
            K.Play(b, "zero"); K.Play(b, "zero"); K.Play(b, "zero");
            Assert.AreEqual(1000, b.Pool.Hp);
        }

        // ── 적의 수 ──
        [Test] public void 관통은_방어를_건너뛰고_실드는_막는다()
        {
            var d = K.Data(enemies: "[{id:'piercer', name:'꿰뚫는 놈', hp:1000, intents:[{t:'back', v:100, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "piercer" });
            b.Pool.Block = 500; b.Pool.Shield = 30;
            b.EndTurn();
            Assert.AreEqual(930, b.Pool.Hp);
            Assert.AreEqual(0, b.Pool.Shield);
        }

        [Test] public void 전체_공격은_값의_두_배_한_번()
        {
            var d = K.Data(enemies: "[{id:'aoe', name:'휩쓰는 놈', hp:1000, intents:[{t:'attackAll', v:60, id:'취약', n:2, rush:0}]}]");
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "aoe" });
            b.EndTurn();
            Assert.AreEqual(1800 - 120, b.Pool.Hp);
            Assert.AreEqual(2, b.St(b.Pool, "취약"));
        }

        [Test] public void 연타는_한_대씩_방어가_먼저_벗겨진다()
        {
            var d = K.Data(enemies: "[{id:'multi', name:'연타', hp:1000, intents:[{t:'multi', v:30, n:3, id:'고통', rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "multi" });
            b.Pool.Block = 50;
            b.EndTurn();
            // 30 · 30(방어 50 → 20 남음, 10 들어감)… 고통 3(턴 끝 다음 턴에)
            Assert.AreEqual(1000 - 40, b.Pool.Hp);
            Assert.AreEqual(3, b.St(b.Pool, "고통"));
            Assert.AreEqual(20, b.Pool.Body.DotU["고통"], "적이 건 지속 피해의 바탕은 FOE_DOT × 층 피해 배율");
        }

        [Test] public void 방어_회복_강화_방해_끼워_넣기()
        {
            var d = K.Data(enemies: @"[
 {id:'g', name:'방패', hp:1000, intents:[{t:'guard', v:50, tough:1, rush:0}]},
 {id:'h', name:'치유', hp:1000, intents:[{t:'heal', v:70, rush:0}]},
 {id:'j', name:'방해', hp:1000, intents:[{t:'jam', v:1, rush:0}]},
 {id:'s', name:'끼워', hp:1000, intents:[{t:'addCard', id:'zero', n:2, to:'hand', rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "g", "h", "j", "s" });
            b.Enemies[1].Hp = 900;
            b.Enemies[0].Tough = 2;
            b.EndTurn();
            Assert.AreEqual(50, b.Enemies[0].Block, "적의 방어는 그 적이 움직일 때 사라진다 — 지금은 남아 있다");
            Assert.AreEqual(3, b.Enemies[0].Tough, "강인도 되찾기");
            Assert.AreEqual(970, b.Enemies[1].Hp);
            Assert.AreEqual(2, b.Ap, "다음 턴 AP -1");
            Assert.AreEqual(2, b.Hand.Count(x => x == "zero"));
        }

        [Test] public void 적_패시브_맞으면_피가_줄면_격파되면_일어서면_카드를_N장_내면()
        {
            var d = K.Data(enemies: @"[{id:'p', name:'성질', hp:1000, intents:[{t:'jam', v:0, rush:0}], passives:[
  {name:'아픔', on:'hurt', do:{t:'block', v:10}},
  {name:'반토막', on:'lowHp', at:0.5, do:{t:'buff', id:'사기', v:1}},
  {name:'푹 꺼짐', on:'broken', do:{t:'buff', id:'취약', v:1}},
  {name:'성남', on:'recover', do:{t:'buff', id:'불굴', v:1}},
  {name:'감시', on:'card', type:'공격', every:2, do:{t:'block', v:5}}]}]",
                cards: "[{id:'smash', name:'깨기', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:3.0, target:'oneEnemy'}, {k:'tough', v:4, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "p" }); K.Hand(b, "smash", "smash");
            K.Play(b, "smash");
            Assert.AreEqual(10, K.E(b).Block, "아픔 — 턴당 한 번");
            Assert.AreEqual(1, b.St(K.E(b), "취약"), "푹 꺼짐");
            K.Play(b, "smash");
            Assert.AreEqual(1, b.St(K.E(b), "사기"), "반토막");
            Assert.AreEqual(5, K.E(b).Block, "방어 10 은 두 번째 한 방에 벗겨지고, 공격 카드 두 장째라 감시 +5(아픔은 이번 턴 이미 돌았다)");
            b.EndTurn();
            Assert.AreEqual(1, b.St(K.E(b), "불굴"), "일어서면 성난다");
        }

        [Test] public void 동료가_쓰러지면_성난다()
        {
            var d = K.Data(enemies: "[{id:'rage', name:'격노', hp:1000, intents:[{t:'jam', v:0, rush:0}], passives:[{name:'격노', on:'allyDown', do:{t:'buff', id:'사기', v:1}}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "dummy", "rage" }); K.Hand(b, "hit");
            b.Enemies[0].Hp = 10;
            K.Play(b, "hit", 0);
            Assert.AreEqual(1, b.St(b.Enemies[1], "사기"));
        }

        [Test] public void 판이_바뀌면_수가_바뀌고_강인도가_다_찬다()
        {
            var d = K.Data(enemies: "[{id:'boss', name:'보스', hp:1000, boss:true, intents:[{t:'jam', v:0, rush:0}], phase:{at:0.5, say:'성났다', intents:[{t:'attack', v:50, rush:0}]}}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "boss" });
            Assert.AreEqual(6, K.E(b).ToughMax, "보스 강인도 6");
            K.E(b).Hp = 400; K.E(b).Tough = 1;
            b.EndTurn();
            Assert.IsTrue(K.E(b).Phased);
            Assert.AreEqual(6, K.E(b).Tough);
            Assert.AreEqual("attack", K.E(b).Intent.T);
        }

        [Test] public void 보스는_기절한_다음_한_턴은_버틴다()
        {
            var d = K.Data(enemies: "[{id:'boss', name:'보스', hp:1000, boss:true, intents:[{t:'attack', v:100, rush:0}]}]",
                cards: "[{id:'stun', name:'기절', hero:'a', cost:0, type:'스킬', fx:[{k:'status', id:'기절', v:1, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "boss" }); K.Hand(b, "stun");
            K.Play(b, "stun");
            b.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp);
            K.Hand(b, "stun");
            K.Play(b, "stun");
            Assert.IsFalse(K.E(b).Sealed, "버텨 냈다");
            b.EndTurn();
            Assert.AreEqual(900, b.Pool.Hp);
        }

        // ── 고학년 ──
        [Test] public void 고학년은_게이지를_써서_AP_없이()
        {
            var d = K.Data(heroes: "[{id:'u', name:'고학년', role:'딜러', row:'back', hp:600, atk:100, def:20, crit:0, ult:{name:'한 방', cost:150, fx:[{k:'dmg', ratio:3.0, target:'oneEnemy'}]}}]");
            var b = K.Fight(d, new[] { "u" }, new[] { "dummy" }, st => st.Gauge = 140);
            Assert.IsNotNull(b.CanUlt("u"));
            b.Gauge = 150;
            Assert.IsNull(b.CanUlt("u"));
            Assert.IsTrue(b.UseUlt("u", 0).Ok);
            Assert.AreEqual(700, K.Hp(b));
            Assert.AreEqual(0, b.Gauge);
            Assert.AreEqual(3, b.Ap);
            Assert.AreEqual(4 - 2.0 / 3, K.E(b).Tough, 1e-9, "고학년도 카드처럼 강인도를 깎는다(AP 2 로 친다)");
        }

        [Test] public void 미리보기는_실제와_같다()
        {
            var d = K.Data();
            var b = K.Fight(d, new[] { "a", "b" }, new[] { "dummy", "dummy" }); K.Hand(b, "hit", "guard");
            K.E(b).Status["취약"] = 1;
            var p = b.PreviewCard(0, 0);
            Assert.AreEqual(150, p[0].Hp); Assert.IsNull(p[1]);
            Assert.AreEqual(1.0 / 3, p[0].Tough, 1e-9);
            var pp = b.PreviewPartyOf(1, 0);
            Assert.AreEqual(100, pp.Block);
            Assert.AreEqual(1000, K.Hp(b), "미리보기는 판을 안 바꾼다");
            Assert.AreEqual(1, b.St(K.E(b), "취약"));
            K.Play(b, "hit");
            Assert.AreEqual(850, K.Hp(b));
        }

        [Test] public void 복사본은_따로_논다()
        {
            var b = K.Fight("hitter"); K.Hand(b, "hit", "hit");
            var c = b.Clone();
            K.Play(c, "hit");
            Assert.AreEqual(1000, K.Hp(b)); Assert.AreEqual(900, K.Hp(c));
            Assert.AreEqual(2, b.Hand.Count);
            c.EndTurn();
            Assert.AreEqual(1000, b.Pool.Hp); Assert.AreEqual(900, c.Pool.Hp);
            Assert.AreSame(c.Pool, c.Party[0].BodyRef);
        }

        [Test] public void 같은_씨앗이면_같은_전투()
        {
            string Run(long seed)
            {
                var d = K.Sample();
                var b = Battle.Start(d, new BattleSetup { Party = new List<string> { "rico", "carrot", "sion" }, Deck = d.BuildDeck(new[] { "rico", "carrot", "sion" }), Enemies = new List<string> { "fairy_close", "magicfork" }, Seed = seed });
                for (int t = 0; t < 8 && b.Over == null; t++)
                {
                    for (int k = 0; k < 4; k++) { int i = b.Hand.FindIndex(id => b.CanPlay(id) == null); if (i < 0) break; b.PlayCard(i, b.AliveEnemies().FirstOrDefault()?.Idx ?? 0); }
                    b.EndTurn();
                }
                return string.Join("\n", b.Log) + b.Pool.Hp;
            }
            Assert.AreEqual(Run(5), Run(5));
            Assert.AreNotEqual(Run(5), Run(6));
        }
    }
}
