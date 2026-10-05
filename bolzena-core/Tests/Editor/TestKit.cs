using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 규칙 시험의 도구 — 숫자를 손으로 셀 수 있는 작은 사도 · 카드 · 적. JSON 은 홑따옴표로 쓴다(Newtonsoft 가 읽는다).
    /// 사도: a(탱커 · 공 100 방 50 · HP 1000 · 앞줄) · b(서포터 · 공 80 방 40 · HP 800) · c(딜러 · 공 150 방 20 · HP 600). 치명 0, 성격 없음.
    /// 적: dummy(체력 1000 · 아무것도 안 한다) · hitter(체력 1000 · 공격 100). 체력 배율 1(EnemyHp = 1).
    /// </summary>
    public static class K
    {
        public const string HEROES = @"[
 {id:'a', name:'가', role:'탱커', row:'front', hp:1000, atk:100, def:50, crit:0},
 {id:'b', name:'나', role:'서포터', row:'mid', hp:800, atk:80, def:40, crit:0},
 {id:'c', name:'다', role:'딜러', row:'back', hp:600, atk:150, def:20, crit:0}
]";
        public const string CARDS = @"[
 {id:'hit', name:'때리기', hero:'a', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'hit_b', name:'나 때리기', hero:'b', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'hit_c', name:'다 때리기', hero:'c', cost:1, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]},
 {id:'zero', name:'숨', hero:'a', cost:0, type:'스킬', fx:[]},
 {id:'zero_b', name:'나 숨', hero:'b', cost:0, type:'스킬', fx:[]},
 {id:'guard', name:'막기', hero:'a', cost:1, type:'스킬', fx:[{k:'block', ratio:2.0}]},
 {id:'big', name:'큰 한 방', hero:'a', cost:2, type:'공격', fx:[{k:'dmg', ratio:1.0, target:'oneEnemy'}]}
]";
        public const string ENEMIES = @"[
 {id:'dummy', name:'허수아비', hp:1000, intents:[{t:'jam', v:0, rush:0}]},
 {id:'hitter', name:'때리는 놈', hp:1000, intents:[{t:'attack', v:100, rush:0}]}
]";

        public static GameData Data(string heroes = null, string cards = null, string enemies = null, string villages = null, string events = null, string equips = null) =>
            new GameData().Add(HEROES, CARDS, ENEMIES, null, null, null).Add(heroes, cards, enemies, villages, events, equips);

        public static Battle Fight(GameData d, string[] party, string[] enemies, Action<BattleSetup> tweak = null, List<Cue> cues = null)
        {
            var st = new BattleSetup { Party = party.ToList(), Enemies = enemies.ToList(), Deck = new List<string>(), EnemyHp = 1, Seed = 1 };
            tweak?.Invoke(st);
            return Battle.Start(d, st, cues);
        }

        public static Battle Fight(params string[] enemies) => Fight(Data(), new[] { "a" }, enemies.Length > 0 ? enemies : new[] { "dummy" });

        /// <summary>손을 이 카드들로 바꾼다.</summary>
        public static void Hand(Battle b, params string[] ids) { b.Hand.Clear(); b.Hand.AddRange(ids); }

        public static PlayResult Play(Battle b, string id, int target = 0, PlayOpts o = null)
        {
            int i = b.Hand.IndexOf(id);
            Assert.GreaterOrEqual(i, 0, $"손에 {id} 가 없다");
            var r = b.PlayCard(i, target, o);
            Assert.IsTrue(r.Ok, r.Why);
            return r;
        }

        public static Unit E(Battle b, int i = 0) => b.Enemies[i];
        public static int Hp(Battle b, int i = 0) => b.Enemies[i].Hp;

        /// <summary>샘플 콘텐츠(Data/Sample).</summary>
        public static GameData Sample()
        {
            foreach (var p in new[] { "Packages/com.bolzena.core/Data/Sample", "../bolzena-core/Data/Sample", "../../bolzena-core/Data/Sample" })
                if (Directory.Exists(p)) return GameData.FromFolder(p);
            throw new DirectoryNotFoundException("Data/Sample 을 못 찾았다");
        }
    }
}
