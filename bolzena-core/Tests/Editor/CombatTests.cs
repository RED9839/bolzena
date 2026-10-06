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

        [Test] public void 판이_바뀌면_수가_바뀌지만_강인도는_차지_않는다()
        {
            var d = K.Data(enemies: "[{id:'boss', name:'보스', hp:1000, boss:true, intents:[{t:'jam', v:0, rush:0}], phase:{at:0.5, say:'성났다', intents:[{t:'attack', v:50, rush:0}]}}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "boss" });
            Assert.AreEqual(7, K.E(b).ToughMax, "보스 강인도(데이터에 없으면 7)");
            K.E(b).Hp = 400; K.E(b).Tough = 1;
            b.EndTurn();
            Assert.IsTrue(K.E(b).Phased);
            Assert.AreEqual(1, K.E(b).Tough, "판이 바뀌어도 강인도는 그대로(사용자 2026-10-06)");
            Assert.AreEqual("attack", K.E(b).Intent.T);
        }

        [Test] public void 강인도는_저절로_차지_않는다_격파되면_다음_턴_회복_스킬만_되찾는다()
        {
            var d = K.Data(enemies: @"[
 {id:'plain', name:'보통', hp:1000, tough:4, intents:[{t:'attack', v:10, rush:0}, {t:'block', v:20, rush:0}]},
 {id:'wall', name:'방패', hp:1000, tough:4, intents:[{t:'guard', v:20, tough:1, rush:0}]},
 {id:'brace', name:'버팀', hp:1000, tough:6, intents:[{t:'brace', v:2, say:'버티기', rush:0}]},
 {id:'core', name:'핵', hp:1000, tough:6, intents:[{t:'jam', v:0, rush:0}], passives:[{name:'단단한 핵', on:'turnStart', do:{t:'brace', v:1}}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "plain", "wall", "brace", "core" });
            b.Enemies[0].Tough = 1; b.Enemies[1].Tough = 1; b.Enemies[2].Tough = 1; b.Enemies[3].Tough = 1;
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual(1, b.Enemies[0].Tough, "회복 스킬이 없는 적은 깎인 채로 남는다");
            Assert.AreEqual(3, b.Enemies[1].Tough, "guard 의 tough 는 그 적 자신만 — 두 번 1씩");
            Assert.AreEqual(5, b.Enemies[2].Tough, "버티기 — 강인도 회복 2 를 두 번");
            Assert.AreEqual(3, b.Enemies[3].Tough, "패시브 — 내 턴 시작마다 1(두 번)");
            var tx = new CardText(d);
            Assert.AreEqual("강인도 회복 2", tx.Intent(d.Enemy("brace").Intents[0]));
            StringAssert.Contains("강인도 회복 1", tx.Intent(d.Enemy("wall").Intents[0]));
        }

        [Test] public void 격파_중에는_회복_스킬로_차지_않는다()
        {
            var d = K.Data(enemies: "[{id:'brace', name:'버팀', hp:1000, tough:3, intents:[{t:'jam', v:0, rush:0}], passives:[{name:'버팀', on:'broken', do:{t:'brace', v:2}}]}]",
                cards: "[{id:'smash', name:'깨기', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}, {k:'tough', v:9, target:'oneEnemy'}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "brace" }); K.Hand(b, "smash");
            K.Play(b, "smash");
            Assert.IsTrue(K.E(b).Broken);
            Assert.AreEqual(0, K.E(b).Tough, "격파 상태면 회복 스킬이 돌아도 0");
            b.EndTurn();
            Assert.IsFalse(K.E(b).Broken);
            Assert.AreEqual(3, K.E(b).Tough, "다음 내 턴 시작에 가득");
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

    /// <summary>강인도 새 단위(2026-10-05 사용자 확정) — 약점 공격은 비용 1 당 1, 아니면 1/3 · 공명은 늘 약점 · 판의 적 속성 하나 · 격파.</summary>
    public class ToughTests
    {
        const string NATURE_HEROES = @"[
 {id:'cool', name:'냉정이', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, nature:'냉정'},
 {id:'pure', name:'순수이', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, nature:'순수'},
 {id:'res', name:'공명이', role:'딜러', row:'back', hp:1000, atk:100, def:20, crit:0, nature:'공명'}]";
        const string NATURE_CARDS = @"[
 {id:'h_cool', name:'냉정 치기', hero:'cool', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_cool2', name:'냉정 큰 치기', hero:'cool', cost:2, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_coolA', name:'냉정 쓸기', hero:'cool', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'allEnemies'}]},
 {id:'h_pure', name:'순수 치기', hero:'pure', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]},
 {id:'h_pure0', name:'순수 콕', hero:'pure', cost:0, type:'공격', fx:[{k:'dmg', ratio:0.01, target:'oneEnemy'}]},
 {id:'h_res', name:'공명 치기', hero:'res', cost:1, type:'공격', fx:[{k:'dmg', ratio:0.1, target:'oneEnemy'}]}]";
        const string NATURE_FOES = @"[
 {id:'mad', name:'광기 적', hp:5000, nature:'광기', tough:6, intents:[{t:'jam', v:0, rush:0}]},
 {id:'mad2', name:'광기 때리는 적', hp:5000, nature:'광기', tough:3, intents:[{t:'attack', v:100, rush:0}]},
 {id:'weakling', name:'칸 2 적', hp:5000, tough:2, intents:[{t:'jam', v:0, rush:0}]},
 {id:'clone_cool', name:'냉정이 (클론)', hp:5000, boss:true, nature:'냉정', clone:'cool', tough:10, intents:[{t:'jam', v:0, rush:0}]},
 {id:'caller', name:'부르는 보스', hp:5000, boss:true, tough:10, intents:[{t:'summon', id:'small', n:1, noTough:true, rush:0}]},
 {id:'resfoe', name:'공명 적', hp:5000, nature:'공명', tough:6, intents:[{t:'jam', v:0, rush:0}]},
 {id:'plain', name:'성격 없는 적', hp:5000, tough:6, intents:[{t:'jam', v:0, rush:0}]},
 {id:'nodata', name:'칸 없는 적', hp:5000, intents:[{t:'jam', v:0, rush:0}]},
 {id:'small', name:'작은 적', hp:5000, tough:3, intents:[{t:'jam', v:0, rush:0}]}]";
        static GameData D() => K.Data(heroes: NATURE_HEROES, cards: NATURE_CARDS, enemies: NATURE_FOES);
        static Battle F(params string[] foes) => K.Fight(D(), new[] { "cool", "pure", "res" }, foes);

        [Test] public void 약점_1코는_강인도_1_약점이_아니면_3분의1()
        {
            var b = F("mad"); K.Hand(b, "h_cool", "h_pure");
            K.Play(b, "h_cool");
            Assert.AreEqual(5, K.E(b).Tough, 1e-9, "냉정 → 광기 약점: 1코 = 1");
            K.Play(b, "h_pure");
            Assert.AreEqual(5 - 1.0 / 3, K.E(b).Tough, 1e-9, "순수 → 광기 약점 아님: 1/3");
            Assert.AreEqual(2, b.ToughHits); Assert.AreEqual(1, b.ToughWeakHits);
        }

        [Test] public void 약점_2코는_2_광역은_대상마다_절반_0코_비약점은_6분의1()
        {
            var b = F("mad", "mad"); K.Hand(b, "h_cool2", "h_coolA", "h_pure0");
            K.Play(b, "h_cool2");
            Assert.AreEqual(4, K.E(b, 0).Tough, 1e-9, "2코 약점 = 2");
            K.Play(b, "h_coolA");
            Assert.AreEqual(3.5, K.E(b, 0).Tough, 1e-9, "광역 1코 약점 = 대상마다 1/2");
            Assert.AreEqual(5.5, K.E(b, 1).Tough, 1e-9);
            K.Play(b, "h_pure0", 1);
            Assert.AreEqual(5.5 - 1.0 / 6, K.E(b, 1).Tough, 1e-9, "0코 비약점 = 1/6");
        }

        [Test] public void 공명은_어느_적이든_늘_약점()
        {
            var b = F("mad", "resfoe", "plain"); K.Hand(b, "h_res", "h_res", "h_res");
            for (int i = 0; i < 3; i++) K.Play(b, "h_res", i);
            for (int i = 0; i < 3; i++) Assert.AreEqual(5, K.E(b, i).Tough, 1e-9, $"적 {i}: 공명 1코 = 1");
            Assert.IsTrue(b.WeakFor(b.Party[2], K.E(b, 1)));
            Assert.IsFalse(b.WeakFor(b.Party[1], K.E(b, 0)), "순수는 광기의 약점이 아니다");
        }

        [Test] public void 비용_0_을_여러_번_빼도_찌꺼기가_남지_않고_격파된다()
        {
            var b = F("small");
            int ap = b.Ap;
            for (int i = 0; i < 18; i++) { K.Hand(b, "h_pure0"); K.Play(b, "h_pure0"); }   // 1/6 × 18 = 3
            Assert.IsTrue(K.E(b).Broken, "3 - 18 × 1/6 은 0 — 1e-15 가 남아 격파가 안 나던 것");
            Assert.AreEqual(0, K.E(b).Tough);
            Assert.AreEqual(ap + R.TOUGH.Ap, b.Ap);
            Assert.AreEqual(1, b.Breaks);
        }

        [Test] public void 격파하면_AP_1_그_적은_한_차례_쉬고_다음_내_턴에_강인도가_찬다()
        {
            var b = F("mad2"); K.Hand(b, "h_cool", "h_cool", "h_cool");
            int hp = b.Pool.Hp;
            K.Play(b, "h_cool"); K.Play(b, "h_cool");
            Assert.IsFalse(K.E(b).Broken);
            K.Play(b, "h_cool");
            Assert.IsTrue(K.E(b).Broken); Assert.IsTrue(K.E(b).Sealed);
            Assert.AreEqual(3 - 3 + 1, b.Ap, "AP 3 을 쓰고 격파로 1");
            var v = b.ToughViewOf(K.E(b));
            Assert.IsTrue(v.Broken); Assert.IsTrue(v.Resting); Assert.AreEqual(0, v.Left); Assert.AreEqual(3, v.Max); Assert.AreEqual(0, v.Fill(0));
            b.EndTurn();
            Assert.AreEqual(hp, b.Pool.Hp, "격파된 적은 그 차례를 쉰다");
            Assert.IsFalse(K.E(b).Broken, "격파 상태는 한 턴");
            Assert.AreEqual(3, K.E(b).Tough, "다음 내 턴에 강인도가 다 찬다");
            b.EndTurn();
            Assert.Less(b.Pool.Hp, hp, "그다음 차례엔 다시 친다");
        }

        [Test] public void 강인도_화면_값()
        {
            var b = F("small"); K.Hand(b, "h_pure");
            K.Play(b, "h_pure");
            var v = b.ToughViewOf(K.E(b));
            Assert.AreEqual(3 - 1.0 / 3, v.Left, 1e-9); Assert.AreEqual(3, v.Max); Assert.AreEqual(3, v.Pips);
            Assert.AreEqual(1, v.Fill(0), 1e-9); Assert.AreEqual(2.0 / 3, v.Fill(2), 1e-9);
            Assert.IsFalse(v.Broken); Assert.IsNull(v.Nature); CollectionAssert.IsEmpty(v.Weak);
            var mb = F("mad");
            var p = mb.ToughViewOf(mb.Enemies[0]);
            Assert.AreEqual("광기", p.Nature); CollectionAssert.AreEqual(new[] { "냉정" }, p.Weak);
        }

        [Test] public void 엘리트_싸움의_여린_적은_강인도_1_더_데이터에_없으면_일반_4_엘리트_6()
        {
            var b = K.Fight(D(), new[] { "cool" }, new[] { "small", "nodata", "mad" }, st => st.Elite = true);
            Assert.AreEqual(4, K.E(b, 0).ToughMax, "작은 적 3 + 엘리트 싸움 1");
            Assert.AreEqual(R.TOUGH.Elite, K.E(b, 1).ToughMax);
            Assert.AreEqual(6, K.E(b, 2).ToughMax, "엘리트 몸(6)은 그대로");
            Assert.AreEqual(R.TOUGH.Fight, F("nodata").Enemies[0].ToughMax);
            Assert.AreEqual(R.TOUGH.Min, F("weakling").Enemies[0].ToughMax, "최소치 3 — 데이터가 2 여도 3 으로 선다");
            Assert.IsTrue(Validator.Check(D()).Errors.Any(x => x.Contains("weakling") && x.Contains("tough")), "검사기: 3 미만이면 오류");
        }

        [Test] public void 강인도_없는_소환물은_격파되지_않고_막대가_없다()
        {
            var b = F("caller");
            b.EndTurn();
            Assert.AreEqual(2, b.Enemies.Count, "소환");
            var s = b.Enemies[1];
            Assert.AreEqual(0, s.ToughMax);
            Assert.IsTrue(b.ToughViewOf(s).None); Assert.AreEqual(0, b.ToughViewOf(s).Pips);
            Assert.IsFalse(b.ToughViewOf(b.Enemies[0]).None);
            K.Hand(b, "h_cool", "h_res");
            K.Play(b, "h_res", 1);
            Assert.IsFalse(s.Broken); Assert.AreEqual(0, s.Tough); Assert.AreEqual(0, b.ToughHits, "강인도 없는 적은 깎은 셈에 안 든다");
            Assert.AreEqual(0, b.Breaks);
            var plain = F("small");
            Assert.AreEqual(3, plain.Enemies[0].ToughMax, "같은 적도 그냥 서면 강인도가 있다");
            Assert.IsEmpty(Validator.Check(D()).Errors.Where(x => x.Contains("caller")).ToList(), "summon noTough 는 오류가 아니다");
        }

        [Test] public void 판의_적_속성은_모든_적을_한_성격으로_약점도_그것으로()
        {
            var b = K.Fight(D(), new[] { "cool", "pure" }, new[] { "mad", "plain", "resfoe" }, st => st.EnemyNature = "냉정");
            foreach (var e in b.Enemies) { Assert.AreEqual("냉정", e.Nature); CollectionAssert.AreEqual(new[] { "순수" }, b.WeakOf(e)); }
            K.Hand(b, "h_pure", "h_cool");
            K.Play(b, "h_pure");
            Assert.AreEqual(5, K.E(b).Tough, 1e-9, "순수 → 냉정 약점");
            K.Play(b, "h_cool");
            Assert.AreEqual(5 - 1.0 / 3, K.E(b).Tough, 1e-9, "냉정 → 냉정 약점 아님");
            var back = Battle.Load(b.Data, b.Save());
            Assert.AreEqual("냉정", back.EnemyNature, "전투 저장 왕복");
        }

        [Test] public void 사도_클론은_판의_적_속성에_안_맞추고_그_사도의_성격_그대로()
        {
            var b = K.Fight(D(), new[] { "cool", "pure" }, new[] { "clone_cool", "mad" }, st => st.EnemyNature = "광기");
            Assert.AreEqual("냉정", K.E(b, 0).Nature, "클론은 사도 cool 의 성격");
            CollectionAssert.AreEqual(new[] { "순수" }, b.WeakOf(K.E(b, 0)));
            Assert.AreEqual("광기", K.E(b, 1).Nature, "클론이 아닌 적은 판의 속성");
            K.Hand(b, "h_pure", "h_cool");
            K.Play(b, "h_pure", 0);
            Assert.AreEqual(9, K.E(b, 0).Tough, 1e-9, "순수 → 냉정 클론은 약점");
            K.Play(b, "h_cool", 0);
            Assert.AreEqual(9 - 1.0 / 3, K.E(b, 0).Tough, 1e-9, "냉정(판의 광기 약점) → 냉정 클론은 약점 아님");
            var back = Battle.Load(b.Data, b.Save());
            Assert.AreEqual("냉정", back.Enemies[0].Nature, "저장 왕복");
            Assert.IsEmpty(Validator.Check(D()).Errors.Where(x => x.Contains("clone_cool") && (x.Contains("클론") || x.Contains("clone "))).ToList(), "clone 칸은 오류가 아니다");
            var bad = K.Data(heroes: NATURE_HEROES, enemies: "[{id:'clone_bad', name:'엇나간 클론', hp:10, nature:'광기', clone:'cool', intents:[{t:'attack', v:1, rush:0}]}]");
            Assert.IsTrue(Validator.Check(bad).Errors.Any(x => x.Contains("clone_bad") && x.Contains("다르다")), "클론 성격이 사도와 다르면 검사 오류");
        }

        [Test] public void 새_판은_적_속성_하나를_고르고_저장_이어하기에_남는다()
        {
            var d = K.Sample(); var party = new List<string> { "rico", "carrot", "sion" };
            var run = Run.New(d, party, 7, enemyNature: "광기");
            Assert.AreEqual("광기", run.EnemyNature);
            var again = Run.Load(d, run.Save());
            Assert.AreEqual("광기", again.EnemyNature, "판 저장 왕복");
            again.EnterNode(again.Reachable()[0]);
            var (b, _) = again.OpenFight();
            Assert.IsTrue(b.Enemies.All(e => e.Nature == "광기"), "그 판의 모든 적이 광기");
            Assert.AreEqual("광기", b.EnemyNature);
            // 속성을 안 주면 씨앗으로 — 같은 씨앗이면 같은 속성, 공명은 안 나온다
            for (long s = 1; s <= 40; s++)
            {
                var r = Run.New(d, party, s);
                CollectionAssert.Contains(R.FOE_NATURES, r.EnemyNature);
                Assert.AreEqual(Run.NatureBySeed(s), r.EnemyNature);
            }
            Assert.AreEqual(5, Enumerable.Range(1, 200).Select(s => Run.NatureBySeed(s)).Distinct().Count(), "다섯 성격이 다 나온다");
            Assert.AreEqual("순수", Run.RollNature(0)); Assert.AreEqual("우울", Run.RollNature(0.999));
            // 옛 저장(적 속성 없음)은 적마다 제 성격
            var old = Run.New(d, party, 7); old.S.EnemyNature = null;
            old.EnterNode(old.Reachable()[0]);
            var (ob, _) = old.OpenFight();
            Assert.IsNull(ob.EnemyNature);
            Assert.IsTrue(ob.Enemies.All(e => e.Nature == d.Enemy(e.Key).Nature));
        }
    }

    /// <summary>판 하나 = 적 속성 하나 — 두 층 보스도 그 성격 사도의 클론으로 고른다(클론 성격은 안 바꾼다).</summary>
    public class BossNatureTests
    {
        const string HEROES = @"[
 {id:'p1', name:'순수 하나', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'순수', star:2},
 {id:'p2', name:'순수 둘', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'순수', star:3},
 {id:'p2_alt', name:'순수 둘(다른 모습)', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'순수', star:3},
 {id:'m1', name:'광기 하나', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'광기', star:1},
 {id:'m2', name:'광기 둘', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'광기', star:3},
 {id:'m3', name:'광기 셋', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'광기', star:3},
 {id:'c1', name:'냉정 하나', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'냉정', star:2},
 {id:'c2', name:'냉정 둘', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'냉정', star:1},
 {id:'u1', name:'우울 하나', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'우울', star:3},
 {id:'u2', name:'우울 둘', race:'시험족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'우울', star:3},
 {id:'x1', name:'남의 순수', race:'다른족', role:'딜러', row:'back', hp:500, atk:100, def:20, crit:0, nature:'순수'}]";
        const string FOES = @"[
 {id:'mob', name:'잔챙이', hp:300, nature:'우울', tough:3, intents:[{t:'attack', v:10, rush:0}]},
 {id:'clone_m1', name:'광기 하나 (클론)', hp:2000, boss:true, nature:'광기', clone:'m1', tough:10, intents:[{t:'attack', v:50, rush:0}]},
 {id:'clone_p2', name:'순수 둘 (클론)', hp:2600, boss:true, nature:'순수', clone:'p2', tough:13, intents:[{t:'attack', v:60, rush:0}]}]";
        const string VILLAGE = @"[{id:'tv', name:'시험 마을', race:'시험족', floors:[
 {name:'1층', land:'시험', pools:[[['mob']],[['mob']],[['mob','mob']]], elites:[['mob','mob']], boss:['clone_m1','mob']},
 {name:'2층', land:'시험', pools:[[['mob']],[['mob']],[['mob','mob']]], elites:[['mob','mob']], boss:['clone_p2']}]}]";
        static GameData D() => K.Data(heroes: HEROES, enemies: FOES, villages: VILLAGE);
        static readonly List<string> PARTY = new() { "a", "b", "c" };

        static List<Battle> AllFights(Run run)
        {
            var o = new List<Battle>();
            for (int fl = 0; fl < 2; fl++)
                foreach (var (node, elite) in new[] { (0, false), (2, false), (1, true), (3, false) })
                {
                    run.S.Floor = fl; run.S.Node = node; run.S.Elite = elite; run.S.Map = null;
                    o.Add(run.OpenFight().battle);
                }
            return o;
        }

        [Test] public void 마을에서_뽑을_수_있는_속성은_그_성격_사도가_클론_자리만큼_있을_때만()
        {
            var d = D();
            Assert.AreEqual(2, Run.CloneSlots(d, "tv").Count);
            CollectionAssert.AreEqual(new[] { "순수", "광기", "우울" }, Run.NaturesFor(d, "tv"),
                "2층은 3성이 있어야 — 냉정은 c1 · c2 가 다 1~2성이라 못 고름 · 우울은 1~2성이 없어 두 층 다 3성(u1 · u2) · 활발 0명");
            for (int i = 0; i < 50; i++) CollectionAssert.Contains(Run.NaturesFor(d, "tv"), Run.RollNature(d, "tv", i / 50.0));
            var r2 = Run.New(d, PARTY, 3, "tv", null, "냉정");
            CollectionAssert.Contains(new[] { "순수", "광기", "우울" }, r2.EnemyNature, "못 고르는 속성을 주면 씨앗으로 고를 수 있는 것");
        }

        [Test] public void 한_판의_모든_싸움_적_속성이_같고_두_보스도_그_성격_사도_클론()
        {
            var d = D();
            foreach (var nat in new[] { "순수", "광기" })
                for (long seed = 1; seed <= 6; seed++)
                {
                    var run = Run.New(d, PARTY, seed, "tv", null, nat);
                    Assert.AreEqual(nat, run.EnemyNature);
                    var heroes = run.BossHeroes;
                    Assert.AreEqual(2, heroes.Count);
                    Assert.IsTrue(heroes.All(h => d.Hero(h).Nature == nat), $"{nat}: 두 보스 사도 {string.Join(",", heroes)}");
                    Assert.AreNotEqual(heroes[0].Split('_')[0], heroes[1].Split('_')[0], "같은 사도 둘은 아니다");
                    Assert.LessOrEqual(d.Hero(heroes[0]).Star, 2, "1층 보스는 1~2성"); Assert.AreEqual(3, d.Hero(heroes[1]).Star, "2층 보스는 3성");
                    var back0 = Run.Load(d, run.Save());
                    Assert.AreEqual(GameData.ToJson(run.BossHeroes), GameData.ToJson(back0.BossHeroes), "저장 왕복 — 보스 사도");
                    foreach (var b in AllFights(run))
                        foreach (var e in b.Enemies) Assert.AreEqual(nat, e.Nature, $"{nat} 판 {e.Key}");
                    var back = Run.Load(d, run.Save());
                    Assert.AreEqual(nat, back.EnemyNature, "저장 왕복 — 속성");
                    Assert.AreEqual(GameData.ToJson(run.Bosses), GameData.ToJson(back.Bosses), "저장 왕복 — 보스");
                }
        }

        [Test] public void 클론_성격은_어느_길에서도_판의_적_속성으로_덮어쓰지_않는다()
        {
            var d = D();
            // 1) 전투 생성(판의 속성 냉정)
            var b = K.Fight(d, new[] { "a" }, new[] { "clone_m1", "clone_p2", "mob" }, st => st.EnemyNature = "냉정");
            Assert.AreEqual("광기", b.Enemies[0].Nature); Assert.AreEqual("순수", b.Enemies[1].Nature); Assert.AreEqual("냉정", b.Enemies[2].Nature);
            CollectionAssert.AreEqual(new[] { "냉정" }, b.WeakOf(b.Enemies[0]), "클론 약점은 제 성격에서");
            // 2) 전투 저장 왕복
            var bl = Battle.Load(d, b.Save());
            Assert.AreEqual("광기", bl.Enemies[0].Nature); Assert.AreEqual("순수", bl.Enemies[1].Nature);
            // 3) 판 — 옛 저장(보스 줄 없음): 고정 보스(마을 데이터의 광기 클론)를 쓰지 않고 판 속성(순수)에 맞춰 새로 고른다 · 보스 하나
            var run = Run.New(d, PARTY, 2, "tv", null, "순수"); run.S.Bosses = null;
            var back = Run.Load(d, run.Save());
            back.S.Floor = 0; back.S.Node = 3;
            var (bb, _) = back.OpenFight();
            Assert.AreEqual(1, bb.Enemies.Count, "보스 싸움은 보스 하나(졸개 mob 은 덜어 냄)");
            Assert.AreEqual("순수", bb.Enemies[0].Nature, "옛 저장도 판 속성의 클론(p1)");
            Assert.AreEqual("p1", d.Enemy(bb.Enemies[0].Key).Clone);
            // 4) 이벤트 싸움
            back.S.Node = 0; back.S.EventFight = new EventFightState { Name = "시험", Enemies = new List<string> { "clone_m1", "mob" } };
            var (eb, _) = back.OpenFight();
            Assert.AreEqual("광기", eb.Enemies[0].Nature, "이벤트 싸움의 클론도 제 성격"); Assert.AreEqual("순수", eb.Enemies[1].Nature);
            // 5) 데이터 자체는 그대로
            Assert.AreEqual("광기", d.Enemy("clone_m1").Nature); Assert.AreEqual("순수", d.Enemy("clone_p2").Nature);
        }

        [Test] public void 일층에_그_성격_1_2성이_없으면_두_층_모두_3성_겹치지_않게()
        {
            var d = D();
            for (long seed = 1; seed <= 8; seed++)
            {
                var run = Run.New(d, PARTY, seed, "tv", null, "우울");
                Assert.AreEqual("우울", run.EnemyNature);
                var hs = run.BossHeroes;
                Assert.AreEqual(3, d.Hero(hs[0]).Star, "1층 — 우울 1~2성이 없어 3성");
                Assert.AreEqual(3, d.Hero(hs[1]).Star, "2층 3성");
                Assert.AreNotEqual(hs[0], hs[1], "같은 사도 한 판 한 번");
                Assert.IsTrue(hs.All(h => d.Hero(h).Nature == "우울"));
                var back = Run.Load(d, run.Save());
                Assert.AreEqual(GameData.ToJson(run.Bosses), GameData.ToJson(back.Bosses), "저장 왕복");
            }
            // 같은 속성 전체에 3성이 없는 속성(냉정)은 안 뽑는다 — 옛 규칙 「그 층만 다른 성급」 은 없다
            Assert.IsFalse(Run.NaturesFor(d, "tv").Contains("냉정"));
            Assert.AreEqual(2, Run.FloorCandidates(d, "tv", "냉정", 0).Count); Assert.AreEqual(0, Run.FloorCandidates(d, "tv", "냉정", 1).Count);
            CollectionAssert.AreEquivalent(new[] { "u1", "u2" }, Run.FloorCandidates(d, "tv", "우울", 0), "1층 대타는 3성");
            // 규칙 우선 — 광기는 1층 1성 m1 · 2층 3성(m2 · m3). 3성이 1층으로 내려오지 않는다
            for (long seed = 1; seed <= 8; seed++)
            {
                var hs = Run.New(d, PARTY, seed, "tv", null, "광기").BossHeroes;
                Assert.AreEqual("m1", hs[0]); Assert.AreEqual(3, d.Hero(hs[1]).Star);
            }
        }

        [Test] public void 다른_종족에서_빌리지_않고_못_채우면_그_속성을_닫는다()
        {
            // 활발: 시험족 1~2성 a1 · 3성 없음(다른족 o3 만) → 닫힘. 우울: 시험족 1~2성 없음 · 3성 u3 하나뿐(다른족 o1 · v3 는 안 씀) → 닫힘.
            // 순수: 1~2성 없음 · 3성 둘(p3 · q3) → 두 층 다 3성 · 서로 다른 사도로 열림. 엘다인(란)은 후보가 아니다.
            const string H = @"[
 {id:'a1', name:'활발 하나', race:'시험족', role:'딜러', hp:500, atk:100, def:20, nature:'활발', star:2},
 {id:'o3', name:'남의 활발', race:'다른족', role:'딜러', hp:500, atk:100, def:20, nature:'활발', star:3},
 {id:'o1', name:'남의 우울', race:'다른족', role:'딜러', hp:500, atk:100, def:20, nature:'우울', star:1},
 {id:'u3', name:'우울 셋', race:'시험족', role:'딜러', hp:500, atk:100, def:20, nature:'우울', star:3},
 {id:'v3', name:'남의 우울 셋', race:'다른족', role:'딜러', hp:500, atk:100, def:20, nature:'우울', star:3},
 {id:'p3', name:'순수 셋', race:'시험족', role:'딜러', hp:500, atk:100, def:20, nature:'순수', star:3},
 {id:'q3', name:'순수 넷', race:'시험족', role:'딜러', hp:500, atk:100, def:20, nature:'순수', star:3},
 {id:'란', name:'엘다인', race:'시험족', role:'딜러', hp:500, atk:100, def:20, nature:'순수', star:2}]";
            var d = K.Data(heroes: H, enemies: FOES, villages: VILLAGE);
            var ok = Run.NaturesFor(d, "tv");
            CollectionAssert.DoesNotContain(ok, "활발", "3성이 다른 종족뿐 — 닫힘");
            CollectionAssert.DoesNotContain(ok, "우울", "1~2성 없고 3성 하나뿐 — 닫힘");
            CollectionAssert.Contains(ok, "순수");
            CollectionAssert.DoesNotContain(Run.FloorCandidates(d, "tv", "순수", 0), "란", "엘다인은 클론 후보가 아니다");
            for (long seed = 1; seed <= 8; seed++)
            {
                var hs = Run.New(d, PARTY, seed, "tv", null, "순수").BossHeroes;
                CollectionAssert.IsSubsetOf(hs, new[] { "p3", "q3" }); Assert.AreNotEqual(hs[0], hs[1], "두 층은 다른 사도");
            }
            // 저장된 판이 닫힌 속성이면 불러올 때 다시 뽑는다
            var r = Run.New(d, PARTY, 5, "tv", null, "순수");
            r.S.EnemyNature = "활발"; r.S.Bosses = new List<List<string>> { new() { "clone_m1" }, new() { "clone_p2" } };
            var back = Run.Load(d, r.Save());
            CollectionAssert.Contains(ok, back.EnemyNature, "닫힌 속성(활발) → 열린 속성으로");
            Assert.IsTrue(back.BossHeroes.All(h => d.Hero(h).Nature == back.EnemyNature), "보스도 새 속성으로");
            Assert.IsTrue(Validator.Check(d).Warnings.Any(w => w.Contains("닫힌 적 속성")), "닫힌 속성은 검사기 주의(오류 아님)");
            Assert.IsFalse(Validator.Check(d).Errors.Any(w => w.Contains("닫힌 적 속성")));
        }

        [Test] public void 같은_마을_속성이라도_씨앗이_다르면_보스가_달라진다()
        {
            var d = D();
            var seen = new HashSet<string>();
            for (long seed = 1; seed <= 40; seed++)
            {
                var run = Run.New(d, PARTY, seed, "tv", null, "광기");
                seen.Add(run.BossHeroes[1]);
                Assert.AreEqual(GameData.ToJson(run.Bosses), GameData.ToJson(Run.PickBosses(d, "tv", "광기", seed)), "같은 씨앗이면 같은 보스");
                Assert.AreEqual(GameData.ToJson(run.Bosses), GameData.ToJson(Run.Load(d, run.Save()).Bosses), "저장 왕복");
            }
            CollectionAssert.AreEquivalent(new[] { "m2", "m3" }, seen, "2층 광기 3성 후보 둘이 판마다 갈린다");
        }

        [Test] public void 원래_클론이_그_성격이면_그대로_아니면_그_자리의_몸을_빌린_클론()
        {
            var d = D();
            var run = Run.New(d, PARTY, 1, "tv", null, "광기");
            Assert.AreEqual("clone_m1", run.Bosses[0][0], "1층 원래 클론(광기)은 그대로");
            Assert.AreEqual(1, run.Bosses[0].Count, "보스 줄은 보스 하나 — 데이터의 졸개(mob)는 덜어 낸다");
            Assert.IsTrue(run.Bosses.All(l => l.Count == 1));
            CollectionAssert.Contains(new[] { GameData.CloneId("m2", "clone_p2"), GameData.CloneId("m3", "clone_p2") }, run.Bosses[1][0], "2층은 광기 3성(m2 · m3)이 원래 클론 몸을 빌려");
            var made = d.Enemy(run.Bosses[1][0]);
            Assert.AreEqual("광기", made.Nature); CollectionAssert.Contains(new[] { "m2", "m3" }, made.Clone); Assert.AreEqual(d.Hero(made.Clone).Name + " (클론)", made.Name);
            Assert.AreEqual(2600, made.Hp); Assert.AreEqual(13, made.Tough, "몸의 체력 · 강인도");
            Assert.AreSame(made, d.Enemy(run.Bosses[1][0]), "한 번 만든 것을 쓴다");
            var pr = Run.New(d, PARTY, 1, "tv", null, "순수");
            Assert.AreEqual("clone_p2", pr.Bosses[1][0], "2층 원래 클론(순수 3성)은 그대로");
            Assert.AreEqual(GameData.CloneId("p1", "clone_m1"), pr.Bosses[0][0], "1층은 순수 1~2성 p1 이 몸을 빌려");
            Assert.AreEqual(d.Hero(pr.BossHeroes[0]).Nature, "순수");
            // 옛 저장(보스 줄 없음)도 고정 보스(마을 데이터)를 쓰지 않고 그 판 씨앗 · 속성으로 새로 고른다
            var old = Run.New(d, PARTY, 1, "tv", null, "광기"); old.S.Bosses = null;
            Assert.AreEqual(GameData.ToJson(Run.PickBosses(d, "tv", "광기", 1)), GameData.ToJson(old.Bosses));
            Assert.AreNotEqual("clone_p2", old.Bosses[1][0], "광기 판 2층에 순수 클론이 서지 않는다");
            // 옛 저장의 보스 줄에 졸개가 있어도 보스 하나만
            old.S.Bosses = new List<List<string>> { new() { "clone_m1", "mob" }, new() { "clone_p2" } };
            CollectionAssert.AreEqual(new[] { "clone_m1" }, old.Bosses[0]);
            old.S.Floor = 0; old.S.Node = 3; old.S.Map = null;
            CollectionAssert.AreEqual(new[] { "clone_m1" }, old.CurrentEnemies());
        }

        [Test] public void 보스가_세운_강인도_없는_소환물은_보스와_같이_쓰러진다()
        {
            var d = K.Data(cards: "[{id:'nuke', name:'한 방', hero:'a', cost:0, type:'공격', fx:[{k:'dmg', ratio:100, target:'oneEnemy'}]}]", enemies: @"[
 {id:'imp', name:'졸개', hp:300, tough:3, intents:[{t:'attack', v:1, rush:0}]},
 {id:'boss1', name:'보스', hp:50, boss:true, tough:10, intents:[{t:'summon', id:'imp', n:2, max:2, noTough:true, rush:0}]},
 {id:'elite1', name:'엘리트', hp:50, tough:6, intents:[{t:'summon', id:'imp', n:1, max:1, rush:0}]}]");
            var b = K.Fight(d, new[] { "a" }, new[] { "boss1" });
            b.EndTurn();
            var imps = b.Enemies.Where(e => e.Key == "imp" && !e.Dead).ToList();
            Assert.AreEqual(2, imps.Count, "보스가 소환");
            Assert.IsTrue(imps.All(e => e.ToughMax == 0), "강인도 없음");
            K.Hand(b, "nuke"); K.Play(b, "nuke", 0);
            Assert.IsTrue(b.Enemies[0].Dead);
            Assert.IsTrue(b.Enemies.Where(e => e.Key == "imp").All(e => e.Dead), "보스가 쓰러지면 소환물도");
            Assert.AreEqual("win", b.Over, "보스 하나를 쓰러뜨리면 끝");
            // 보스가 아닌 적이 세운 (강인도 있는) 적은 남는다
            var b2 = K.Fight(d, new[] { "a" }, new[] { "elite1" });
            b2.EndTurn();
            Assert.AreEqual(1, b2.Enemies.Count(e => e.Key == "imp" && !e.Dead));
            Assert.Greater(b2.Enemies.First(e => e.Key == "imp").ToughMax, 0);
            K.Hand(b2, "nuke"); K.Play(b2, "nuke", 0);
            Assert.AreEqual(1, b2.Enemies.Count(e => e.Key == "imp" && !e.Dead), "엘리트가 세운 적은 남는다");
        }

        [Test] public void 검사기_보스는_하나_강인도_엘리트_5_보스_7()
        {
            string Foes(int bossT, int eliteT) => @"[
 {id:'mob', name:'잔챙이', hp:300, tough:3, intents:[{t:'attack', v:10, rush:0}]},
 {id:'el', name:'엘리트', hp:600, tough:ET, intents:[{t:'attack', v:10, rush:0}]},
 {id:'bs', name:'보스', hp:2000, boss:true, tough:BT, intents:[{t:'attack', v:10, rush:0}]}]".Replace("ET", eliteT.ToString()).Replace("BT", bossT.ToString());
            string Vil(string boss, string elite) => @"[{id:'tv', name:'시험 마을', race:'시험족', floors:[
 {name:'1층', land:'시험', pools:[[['mob']],[['mob']],[['mob']]], elites:[[ELITE]], boss:[BOSS]},
 {name:'2층', land:'시험', pools:[[['mob']],[['mob']],[['mob']]], elites:[[ELITE]], boss:['bs']}]}]".Replace("ELITE", elite).Replace("BOSS", boss);
            List<string> Errs(int bt, int et, string boss, string elite) => Validator.Check(K.Data(enemies: Foes(bt, et), villages: Vil(boss, elite))).Errors;
            Assert.IsEmpty(Errs(7, 5, "'bs'", "'el','mob'").Where(x => x.Contains("보스") || x.Contains("엘리트")).ToList(), "기준을 지키면 오류 없음");
            Assert.IsTrue(Errs(7, 5, "'bs','mob'", "'el'").Any(x => x.Contains("보스 칸은 하나")), "보스 칸에 졸개");
            Assert.IsTrue(Errs(7, 4, "'bs'", "'el','mob'").Any(x => x.Contains("엘리트") && x.Contains("5")), "엘리트 싸움에 강인도 5 이상이 없다");
            Assert.IsTrue(Errs(6, 5, "'bs'", "'el'").Any(x => x.Contains("보스 tough")), "보스 강인도 7 미만");
            var b = K.Fight(K.Data(enemies: "[{id:'lo', name:'낮은 보스', hp:100, boss:true, tough:3, intents:[{t:'attack', v:1, rush:0}]}]"), new[] { "a" }, new[] { "lo" });
            Assert.GreaterOrEqual(b.Enemies[0].ToughMax, 7, "엔진도 보스를 7 아래로 세우지 않는다");
        }
    }

    /// <summary>적 실드(방어) — 적이 얻으면 내 턴 동안 남고, 내 공격은 실드부터, 적의 다음 차례가 시작될 때 사라진다(가호는 남는다).</summary>
    public class FoeGuardTests
    {
        const string FOES = @"[
 {id:'guarder', name:'방패 대장', hp:1000, intents:[{t:'guard', v:50, rush:0}, {t:'jam', v:0, rush:0}]},
 {id:'idle', name:'졸개', hp:1000, intents:[{t:'jam', v:0, rush:0}]},
 {id:'self', name:'제 몸 방어', hp:1000, intents:[{t:'block', v:80, rush:0}, {t:'jam', v:0, rush:0}]}]";
        const string CARDS = @"[{id:'strip', name:'실드 부수기', hero:'a', cost:0, type:'스킬', fx:[{k:'strip', target:'oneEnemy'}]}]";
        static GameData D() => K.Data(cards: CARDS, enemies: FOES);

        [Test] public void 적_전체_방어는_뒤에_움직이는_적에게도_내_턴_동안_남고_내_공격은_실드부터()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "guarder", "idle" }, null, cues);
            b.EndTurn();
            Assert.AreEqual(50, b.Enemies[0].Block);
            Assert.AreEqual(50, b.Enemies[1].Block, "뒤에 움직인 졸개도 방어가 남는다(예전엔 제 차례에 지워졌다)");
            K.Hand(b, "hit");
            cues.Clear();
            K.Play(b, "hit", 1);
            Assert.AreEqual(0, b.Enemies[1].Block, "실드부터 깎였다");
            Assert.AreEqual(1000 - 50, b.Enemies[1].Hp, "100 가운데 50 은 실드가 막았다");
            var hurt = cues.First(c => c.K == "hurt" && c.Side == Side.Enemy && c.Idx == 1);
            Assert.AreEqual(50, hurt.Guard); Assert.AreEqual(50, hurt.V);
            Assert.IsTrue(b.Log.Any(l => l.Contains("졸개: 실드가 50 막음")));
        }

        [Test] public void 적_방어는_적의_다음_차례_시작에_사라지고_쪽지_unguard_가호면_남는다()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "self", "idle" }, null, cues);
            b.EndTurn();
            Assert.AreEqual(80, b.Enemies[0].Block, "적이 얻은 방어는 내 턴에 남는다");
            cues.Clear();
            b.EndTurn();
            Assert.AreEqual(0, b.Enemies[0].Block, "적의 다음 차례 시작에 사라진다(그 차례엔 방어를 안 쌓았다)");
            var lost = cues.First(c => c.K == "unguard" && c.Side == Side.Enemy && c.Idx == 0);
            Assert.AreEqual(80, lost.V); Assert.AreEqual(0, lost.To);
            // 가호(실드 보존) — 남는다
            var g = K.Fight(D(), new[] { "a" }, new[] { "self" });
            g.EndTurn();
            g.Enemies[0].Status["실드 보존"] = 1;
            g.EndTurn();
            Assert.AreEqual(80, g.Enemies[0].Block, "가호면 적의 차례가 와도 남는다");
            Assert.AreEqual(0, g.St(g.Enemies[0], "실드 보존"), "가호 1 감소");
        }

        [Test] public void 실드_전부_파괴는_쪽지와_계기를_남긴다()
        {
            var cues = new List<Cue>();
            var b = K.Fight(D(), new[] { "a" }, new[] { "self" }, null, cues);
            b.EndTurn();
            b.Enemies[0].Shield = 20;
            cues.Clear();
            K.Hand(b, "strip");
            K.Play(b, "strip");
            Assert.AreEqual(0, b.Enemies[0].Block + b.Enemies[0].Shield);
            var lost = cues.First(c => c.K == "unguard");
            Assert.AreEqual(100, lost.V); Assert.AreEqual(0, lost.To);
            Assert.IsTrue(b.Log.Any(l => l.Contains("실드 파괴 (-100)")));
        }
    }
}
