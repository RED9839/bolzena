using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bolzena.Fx
{
    // ── 카드 → SD 동작 · 소리 갈래 (웹판 js/data/card-motion.js) ──
    // 카드 내용은 코어가 새로 만든다 — 여기서는 동작을 고르는 데 쓰는 몫만 받는다.
    public class MotionCard
    {
        public string Id;
        public string Type;                  // "공격" · "스킬" · "강화"
        public bool Signature;               // 그 사도의 대표 기술
        public string Target;                // 카드 대상 글("아군" …)
        public List<MotionFx> Fx = new List<MotionFx>();
    }

    // 스파인 SFX(n) 이벤트 — 칸 번호 · 동작 시작에서 ms
    public struct SoundEvent { public int N, T; public override string ToString() => $"SFX{N}@{T}"; }

    public class MotionFx
    {
        public string K;                     // dmg · block · shield · heal · status · atkMod …
        public float Ratio;                  // 피해 배율(공격력 ×)
        public int Hits = 1;
        public string Target;                // oneEnemy · allEnemies · allAllies …
        public float V;                      // 옛 카드(damage · aoe) — v 10 = 100%
        public int Times = 1;
    }

    public struct MotionPick
    {
        public string Anim;                  // SD 동작(없으면 null — 제자리, 카드 종류 소리만)
        public string Group;                 // 사도 소리 갈래 attack · power · skill · null
        public string Tier;                  // sig · heavy · light · guard · cast · rally · buff · util · eat
        public int CutMs;                    // 그 동작을 이만큼만 하고 쉬는 동작으로
        public float From;                   // 동작의 이 몫(0~1)부터(등장 — 뛰어드는 앞을 건너뛴다)
        public override string ToString() => $"{Anim ?? "-"} ({Group ?? "-"} · {Tier}{(CutMs > 0 ? $" · {CutMs}ms" : "")}{(From > 0 ? $" · from {From}" : "")})";
    }

    public static class CardMotion
    {
        // 역할의 Skill1_1 이 무엇처럼 보이나 — 탱커 막아서기 · 서포터 시전 · 딜러 공격
        public static readonly Dictionary<string, string> LOOK = new Dictionary<string, string> { { "탱커", "guard" }, { "서포터", "cast" }, { "딜러", "strike" } };
        public static readonly Dictionary<string, string> LOOK_BY_HERO = new Dictionary<string, string> { { "시저", "strike" }, { "코미", "cast" }, { "리온", "strike" } };
        public static readonly HashSet<string> POWER_NOT_ATTACK = new HashSet<string> { "에르핀" };
        public static readonly Dictionary<string, MotionPick> CARD_MOTION = new Dictionary<string, MotionPick>
        {
            { "에르핀_u1", new MotionPick { Anim = "Attack2_1", Group = "power", Tier = "eat" } },
        };
        public static readonly Dictionary<string, int> LIGHT_ONE_CUT = new Dictionary<string, int> { { "티그", 700 } };
        public const float HEAVY = 1.8f, AOE_HEAVY = 1.2f;
        public const int MULTI = 3;
        public const int BUFF_CUT = 1400;
        public const float SPAWN_FROM = 0.4f;
        static readonly HashSet<string> GUARD_FX = new HashSet<string> { "block", "shield", "blockAll", "blockAlly", "taunt", "invuln", "takenMod", "defMod" };
        static readonly Regex ALLY = new Regex("all(y|ies)", RegexOptions.IgnoreCase);
        static readonly Regex BUFFK = new Regex("^(atkMod|dealtMod|critMod|crit)$");

        public struct Power { public float Total; public int Hits; public bool Aoe, Any; }

        public static Power CardPower(MotionCard c)
        {
            float total = 0; int hits = 0; bool aoe = false, any = false;
            if (c != null && c.Fx != null)
                foreach (var f in c.Fx)
                {
                    if (f.K == "dmg")
                    {
                        int n = f.Hits > 0 ? f.Hits : 1;
                        any = true; total += f.Ratio * n; hits = Math.Max(hits, n);
                        if (f.Target == "allEnemies") aoe = true;
                    }
                    else if (f.K == "damage" || f.K == "aoe" || f.K == "partyDmg")
                    {
                        any = true; total += f.V / 10f; hits = Math.Max(hits, f.Times > 0 ? f.Times : 1);
                        if (f.K == "aoe") aoe = true;
                    }
                }
            return new Power { Total = (float)Math.Round(total * 100) / 100f, Hits = hits, Aoe = aoe, Any = any };
        }

        // role — "탱커" · "서포터" · "딜러"(앞뒤 글이 붙어 있어도 된다), key — 사도 키, has — 그 SD 에 그 동작이 있나
        public static MotionPick Pick(MotionCard c, string role, string key, Func<string, bool> has = null)
        {
            has = has ?? (_ => true);
            string look = key != null && LOOK_BY_HERO.TryGetValue(key, out var lk) ? lk : RoleLook(role) ?? "strike";
            string skill = has("Skill1_1") ? "Skill1_1" : null;
            string power = has("Attack2_1") && !(key != null && POWER_NOT_ATTACK.Contains(key)) ? "Attack2_1" : null;
            string light = has("Attack1_1") ? "Attack1_1" : null;
            MotionPick Go(string anim, string group, string tier, int cut = 0) => new MotionPick { Anim = anim, Group = anim != null ? group : null, Tier = tier, CutMs = cut, From = anim == "Spawn" ? SPAWN_FROM : 0 };
            var p = CardPower(c);
            if (c != null && c.Id != null && CARD_MOTION.TryGetValue(c.Id, out var fix) && has(fix.Anim)) return Go(fix.Anim, fix.Group, fix.Tier, fix.CutMs);
            if (c != null && c.Signature && skill != null) return Go(skill, "skill", "sig");
            var fxs = c?.Fx ?? new List<MotionFx>();
            if ((c != null && c.Type == "공격") || p.Any)
            {
                bool heavy = p.Total >= HEAVY || (p.Aoe && p.Total >= AOE_HEAVY) || p.Hits >= MULTI;
                if (heavy)
                {
                    if (power != null) return Go(power, "power", "heavy");
                    if (skill != null && look == "strike") return Go(skill, "skill", "heavy");
                }
                int one = !heavy && light != null && p.Hits <= 1 && key != null && LIGHT_ONE_CUT.TryGetValue(key, out var oc) ? oc : 0;
                return Go(light ?? power ?? skill, light != null ? "attack" : power != null ? "power" : "skill", heavy ? "heavy" : "light", one);
            }
            bool ally = fxs.Any(f => ALLY.IsMatch(f.Target ?? "")) || (c != null && c.Target == "아군");
            bool guard = fxs.Any(f => GUARD_FX.Contains(f.K));
            if (guard)
            {
                if (look == "guard" && skill != null) return Go(skill, "skill", "guard");
                if (look == "cast" && skill != null) return Go(skill, "skill", "cast");
                if (ally && has("Victory")) return Go("Victory", null, "rally", BUFF_CUT);
                return Go(has("Spawn") ? "Spawn" : null, null, "guard");
            }
            if (look == "cast" && skill != null) return Go(skill, "skill", "cast");
            if ((c != null && c.Type == "강화") || fxs.Any(f => BUFFK.IsMatch(f.K ?? "")))
                return Go(has("Victory") ? "Victory" : null, null, "buff", BUFF_CUT);
            return Go(has("Spawn") ? "Spawn" : null, null, "util");
        }

        static string RoleLook(string role)
        {
            if (string.IsNullOrEmpty(role)) return null;
            foreach (var kv in LOOK) if (role.Contains(kv.Key)) return kv.Value;
            return null;
        }
    }

    // ── 동작 ↔ 목소리 갈래 (웹판 js/motion-voice.js) — 순서대로 첫 매치, 매치 없음 = 목소리 없음 ──
    public static class MotionVoice
    {
        static Regex R(string p) => new Regex(p, RegexOptions.IgnoreCase);
        public static readonly List<KeyValuePair<Regex, string[]>> TABLE = new List<KeyValuePair<Regex, string[]>>
        {
            P(@"^(?:Move|Walk|Dash|Jump|Sleep|Sleepy)(?:_|\d|$)", null),
            P(@"^(?:Pat|Touch)_Idle(?:_|$)", null),
            P(@"^Smash_End_1(?:_|$)", "smashHit", "hit", "surprise"),
            P(@"^Smash_End_2(?:_|$)", "smashLine", "anger"),
            P(@"^Tickle_End(?:_|$)", "tickleduring", "ticklestart"),
            P(@"^Tickle_Idle(?:_|$)", "tickleduring", "ticklestart"),
            P(@"^(Thinking|Think|Question|Curious|Doubt|Hesitate)", "hmm", "line", "affinity"),
            P(@"^(Nodding|Yes|Agree|Ok)", "yes", "line"),
            P(@"^(No_|No$|Nope|Deny|Shake)", "no", "line"),
            P(@"^(Victory|EasterEgg_Victory)", "victory", "pleasure", "joy"),
            P(@"^(OW\d_)?Attack\d", "basicattack", "shout", "anger"),
            P(@"^(OW\d_)?(Skill\d|Cast\d)", "spskill", "shout", "anger"),
            P(@"^(OW\d_)?Ultimate\d", "ultimate", "shout", "anger"),
            P(@"^(Groggy|Hit|Bind)(_|$)", "hit", "surprise", "sorry"),
            P(@"^Die(_|$)", "die", "defeat", "sorrow"),
            P(@"^(Happy|Smile|Laugh|Excited|Nicesmile|Joy|Dance|Sing|Singing|Rhythm|Clap|Heart|Wink|Cute|Relaxed|EasterEgg_Happy|Success)", "joy", "pleasure"),
            P(@"^(Proud|Pride|Victory|Pose|Posing|Cool|Ganzi|Strong|Salute|Present|Magic|Beam|Special|V_|V$)", "pleasure", "joy"),
            P(@"^(Shy|Dere|Innocent)", "pleasure"),
            P(@"^(Melong|Merong|Joke|Tease|Cheeky|Sneaky|Smirking|Villain|Scary|Annoy)", "pleasure", "line"),
            P(@"^(Angry|Mad|Upset|Sulky|Attack|Punch|Fight|Sword|Skill|Ultimate|Laser|Pistol|Revolver|Aiming|Warning|Scream|Stop|No_|No$|Nope|Ban)", "anger"),
            P(@"^(Sad|Sorrow|Die|Defeat|Cry|Tired|Sleepy|Sleep|Lazy|Dizzy|Boring|Ouch|Knee|Fear|Hesitate|Doubt|Worry)", "sorrow", "sorry"),
            P(@"^(Sorry|Notmyfault|Sweat|Shame|Regret|Calm|Ottokhaji|Why)", "sorry", "sorrow"),
            P(@"^Smash", "dutchrubend", "surprise", "anger"),
            P(@"^(Surprise|Surprised|Shock|Panic|Groggy|Hit|Break|Splash|Fail)", "surprise", "sorry"),
            P(@"^(Eat|Hungry|Bread|Drink|Cook|Latte|Smell|Spit|Spitter)", "eat"),
            P(@"^Tickle", "ticklestart", "tickleduring"),
            P(@"^Pat", "pat", "pleasure"),
            P(@"^Touch", "cheek"),
            P(@"^(Spawn|Enter|Hi$|Hi_|Greeting|Hug|Call)", "spawn", "greeting"),
            P(@"^(Blank|Nodding|Taunt|Talk|Speak|Whisper|Think|Thinking|Question|Curious|Serious|Point|Check|Note|Read|Write|Work|Camera|Phone|Mic|Loudspeaker|Recorder|Recoder|Clock|Mirror|Money|Count|Sit|Sitting|Squat|Stand|Rest|Yoga|Pray|Quiet|Ignore|Yare|Bbang|Gao|Try|Clean|Act|Idle_3|Idle3|Dumb|Robot|Drill|Scouter|Rummage|Glasses|Mask|Scroll|Track|Aside|Jackson|Parrot|Domo|Kirat|Kisya|Baldo|Urcharyu|Sijeo|Dehet|Taik|Oioi|Beni|Rock|Go|Drive|Drift|Dash|Jump|Move|Walk|Promise|Succession|Concent|Open|Closed|Help|Disgust|Lying|Down|Yes)",
              "line", "affinity", "callplayer", "hmm", "greeting"),
        };

        static KeyValuePair<Regex, string[]> P(string re, params string[] cats) => new KeyValuePair<Regex, string[]>(R(re), cats);

        // 동작 이름 → 목소리 갈래 후보(앞에서부터). null = 목소리 없음
        public static string[] CatsFor(string anim)
        {
            if (string.IsNullOrEmpty(anim)) return null;
            foreach (var kv in TABLE) if (kv.Key.IsMatch(anim)) return kv.Value;
            return null;
        }
    }

    // ── 달려가는 고학년 · 총구 · 타격 갈래 (웹판 js/fight-screen.js) ──
    public class DashCfg
    {
        public string GoAnim; public float GoSec;          // 이 조각 · 초부터 달린다
        public string HitAnim; public float HitSec;        // 여기 닿는다 — 닿는 때가 타격
        public string LandAnim; public float LandSec = -1; // 옮기기를 마치는 때(없으면 hit 까지)
        public string HomeFromAnim; public float HomeFromSec = -1;   // 돌아오는 창(화면 밖에 있는 사이에 제자리로)
        public string HomeToAnim; public float HomeToSec = -1;
        public Dictionary<string, int> Loop = new Dictionary<string, int>();   // 고리 조각을 몇 번 돌리나
        public string Reach;                               // 앞으로 뻗은 본 — 적 몸 앞에 오도록 멈춘다(없으면 그림 폭의 35%)
        public bool Hop;                                   // 뛰어올라 화면 밖에 있는 사이에 옮긴다
        public int BackMs;                                 // 다 끝나고 돌아서 뛰어오는 시간
    }

    public static class MotionTables
    {
        // 에르핀 「돌겨어어어!!! 억⋯?」 — 1_1 끝에서 달리기 시작, 1_2_Loop 두 바퀴 동안 달려 1_3 첫 프레임에 닿는다
        public static readonly Dictionary<string, DashCfg> DASH = new Dictionary<string, DashCfg>
        {
            { "에르핀", new DashCfg { GoAnim = "Ultimate1_1", GoSec = 0.85f, HitAnim = "Ultimate1_3", HitSec = 0,
                Loop = new Dictionary<string, int> { { "Ultimate1_2_Loop", 2 } }, Reach = "Point_Ult1", BackMs = 560 } },
            { "에르핀_왕도", new DashCfg { GoAnim = "Ultimate1_1", GoSec = 1.96f, LandAnim = "Ultimate1_2", LandSec = 0.05f, HitAnim = "Ultimate1_2", HitSec = 0.93f,
                Hop = true, Reach = "FX_Punch", HomeFromAnim = "Ultimate1_2", HomeFromSec = 2.78f, HomeToAnim = "Ultimate1_3", HomeToSec = 0.5f, BackMs = 0 } },
        };

        // 총구 — 시전자 쪽 모으기 · 레이저가 붙을 본 [본, 끝(본 길이만큼 앞)]. 표에 없으면 이름(MUZZLE_RE)으로 찾는다
        public static readonly Dictionary<string, KeyValuePair<string, bool>> MUZZLE = new Dictionary<string, KeyValuePair<string, bool>>
        {
            { "아멜리아", new KeyValuePair<string, bool>("Weapon_main7", true) },
            { "캬롯", new KeyValuePair<string, bool>("Weapon1_9", true) },     // 사탕수수 끝(Point_Attack1 은 몸 앞 고정 점 — 2026-10-06 사용자 「몸통에서 총알」)
            { "레테", new KeyValuePair<string, bool>("Laser_Head", true) },    // 손전등 머리(Point_UltLaser 는 머리 뒤 고정 점)
        };
        public static readonly Regex MUZZLE_RE = new Regex("(muzzle|barrel)", RegexOptions.IgnoreCase);

        // 고학년 타격 시각 — SD 이벤트를 모르면 동작 시작에서 이만큼(ms)
        public const int ULT_HIT = 500;

        // 공용 타격 이펙트(구운 낱장) — 갈래마다 겹쳐 트는 몇 장
        //   slash 베기(물리 근접) · shot 쏘기(물리 원거리) · magic 마법 · blunt 둔기(덩치 큰 적) · big 세게 맞음 · bigBlunt · crit 치명타
        public static readonly Dictionary<string, string[]> HIT_FX = new Dictionary<string, string[]>
        {
            { "slash", new[] { "fx_common_hit_3_m", "fx_common_hit_slash_3" } },
            { "shot", new[] { "fx_common_hit_1_m" } },
            { "magic", new[] { "fx_mago_hit_1", "fx_common_hit_4_m" } },
            { "blunt", new[] { "fx_common_hit_2_m" } },
            { "big", new[] { "fx_common_hit_22" } },
            { "bigBlunt", new[] { "fx_common_hit_explosion_1_m" } },
            { "crit", new[] { "fx_common_hit_shockwave_1" } },
        };
        static readonly Regex FOE_MAGIC = new Regex("(wizard|supporter|longrange|wisps|magicfork|drone)");
        static readonly Regex FOE_BLUNT = new Regex("(tanker|bear|golem|oldtree|ginseng|marshmallow|imoogi|curburus|pumpkin|snail|nependers|cranker|buseuleogi)");

        // 때린 쪽 → 타격 갈래. 사도는 공격 타입(물리 · 마법)과 서는 줄(뒷줄 물리는 총 · 활), 적은 이름
        public static string HitKindHero(string dmgType, bool backRow) => dmgType == "마법" ? "magic" : backRow ? "shot" : "slash";
        public static string HitKindEnemy(string enemyKey)
        {
            var k = enemyKey ?? "";
            return FOE_MAGIC.IsMatch(k) ? "magic" : FOE_BLUNT.IsMatch(k) ? "blunt" : "slash";
        }
    }
}
