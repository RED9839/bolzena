using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bolzena.Fx
{
    // 효과음 고르기 — 웹판 js/data/sfx-map.js 를 그대로 옮겼다. 어떤 일에 어떤 원작 효과음을 트는가(이름만 — 파일은 BolzenaAudio 가).
    //   SFX[갈래] = { 후보 파일(무작위 하나), v 음량, gap 다시 트는 최소 간격 ms, max · fade 꼬리 자르기(초), jitter 높이 흔들기 }
    public class SfxEntry
    {
        public string[] F;
        public float V = 1;
        public int Gap = 50;
        public float Max = -1, Fade = -1;    // < 0 이면 TAIL
        public bool Jitter;
    }

    public static class SfxMap
    {
        static SfxEntry E(float v, params string[] f) => new SfxEntry { F = f, V = v };
        static SfxEntry E(float v, int gap, params string[] f) => new SfxEntry { F = f, V = v, Gap = gap };
        static SfxEntry J(float v, params string[] f) => new SfxEntry { F = f, V = v, Jitter = true };

        public static readonly Dictionary<string, SfxEntry> SFX = new Dictionary<string, SfxEntry>
        {
            // ── 카드 ──
            { "card.play", E(0.5f, "sfx_cardgacha_cardflying") },
            { "card.공격", E(0.5f, "scenario/sfx_swing", "scenario/sfx_swing_2") },
            { "card.스킬", E(0.45f, "sfx_ingame_manausing") },
            { "card.기술", E(0.45f, "sfx_ingame_manausing") },
            { "card.방어", E(0.45f, "sfx_ingame_battleitemuse") },
            { "card.회복", E(0.45f, "sfx_ingame_managet") },
            { "card.강화", E(0.45f, "sfx_ingame_artifactequip") },
            { "card.draw", E(0.3f, 60, "sfx_cardpack_opening") },
            { "card.hover", E(0.22f, 80, "sfx_deck_changeskillinfo") },
            { "card.shuffle", E(0.45f, "scenario/sfx_cardshuffle") },
            { "card.cant", E(0.5f, "sfx_deck_wrongslot") },
            // ── 맞는 소리 ──
            { "hit.slash", J(0.85f, "monster/elfsoldiercloserange/elfsoldiercloserange_basicattack_hit") },
            { "hit.blunt", J(0.85f, "scenario/sfx_punch") },
            { "hit.magic", J(0.85f, "hero/naia/naia_basicattack_hit") },
            { "hit.gun", J(0.85f, "monster/nururingarcher/nururingarcher_basicattack_hit") },
            { "hit.crit", J(0.9f, "scenario/sfx_greatswordslash") },
            { "hit.heavy", new SfxEntry { F = new[] { "monster/golem/golem_basicattack_hit" }, Gap = 120, Max = 0.8f } },
            { "hit.small", new SfxEntry { F = new[] { "monster/curseddoll/curseddoll_skillhit" }, V = 0.55f, Gap = 60, Jitter = true } },
            { "hurt", J(0.85f, "monster/fairymobcloserange/fairymobcloserange_basicattack_hit") },
            // ── 방어 · 회복 · 상태 ──
            { "block.gain", E(0.5f, 350, "sfx_urosminigame_crayonknightshield") },
            { "block.hit", E(0.65f, "scenario/sfx_hitmetal") },
            { "block.break", E(0.8f, "sfx_velaminigame_breakbarrier") },
            { "heal", E(0.5f, 350, "hero/asana/asana_powerattck_heal") },
            { "buff", E(0.5f, 250, "sfx_ingame_growup") },
            { "debuff", E(0.5f, 250, "sfx_ingame_growdown") },
            { "status.stun", E(0.55f, 250, "scenario/sfx_stun_1") },
            { "status.burn", E(0.55f, 250, "scenario/sfx_velafire") },
            { "death.enemy", E(0.7f, "sfx_urosminigame_disappearunit") },
            { "death.hero", E(0.75f, "scenario/sfx_falldown") },
            // ── 고학년 ──
            { "ult.ready", E(0.5f, 400, "sfx_ingame_maxgrowup") },
            { "ult.cutin", E(0.85f, 300, "scenario/sfx_ingame_callhero_start") },
            { "ult.impact", E(0.9f, 200, "scenario/sfx_shortexplosion") },
            // ── 전투 흐름 ──
            { "turn.start", E(0.4f, "sfx_common_notification") },
            { "turn.end", E(0.45f, "sfx_lobby_startbattlebuttondown") },
            { "battle.start", E(0.6f, "sfx_lobby_startbattlebuttondown") },
            { "boss.entry", E(0.8f, "sfx_renewabossentry") },
            { "victory", E(0.8f, 2000, "sfx_victory") },
            { "victory.star", E(0.7f, "sfx_victorystar") },
            { "defeat", E(0.75f, 2000, "sfx_stage_fail") },
            // ── 화면 ──
            { "ui.click", E(0.35f, 40, "sfx_common_buttontouch") },
            { "ui.open", E(0.4f, "sfx_common_popupbuttontouch") },
            { "ui.close", E(0.4f, "sfx_common_popupclose") },
            { "ui.tab", E(0.4f, "sfx_common_tabbuttontouch") },
            { "ui.toggle", E(0.4f, "sfx_common_radiobtntoggle") },
            { "ui.error", E(0.45f, "sfx_deck_wrongslot") },
            { "ui.select", E(0.45f, "sfx_deck_selecthero") },
            { "ui.deselect", E(0.45f, "sfx_deck_unselecthero") },
            { "ui.confirm", E(0.45f, "sfx_deck_selectedcharacter") },
            { "ui.start", E(0.55f, "sfx_lobby_startbattlebuttondown") },
            { "map.step", E(0.5f, "sfx_selectstage") },
            { "map.open", E(0.45f, "sfx_stage_open") },
            { "reward", E(0.6f, "sfx_common_rewardpopup") },
            { "reward.card", E(0.55f, "sfx_ingame_getitemreward") },
            { "coin", E(0.5f, "sfx_battlecoinupdate") },
            { "shop.buy", E(0.55f, "sfx_ingame_artifactpopupbuy") },
            { "shop.sell", E(0.55f, "sfx_ingame_battleitemsell") },
            { "shop.reroll", E(0.55f, "sfx_ingame_rerollcardlist") },
            { "shop.remove", E(0.5f, "scenario/sfx_clothripping") },
            { "camp.train", E(0.6f, "sfx_card_levelup") },
            { "event.open", E(0.45f, "sfx_common_opencontents") },
        };

        // 꼬리 자르기 — 갈래 이름(먼저) · 앞머리 → [최대 초, 줄이는 초]. 최대 0 이면 끝까지
        public static readonly Dictionary<string, float[]> TAIL = new Dictionary<string, float[]>
        {
            { "ui", new[] { 0.6f, 0.1f } }, { "card", new[] { 0.8f, 0.15f } }, { "hit", new[] { 0.9f, 0.18f } }, { "hurt", new[] { 0.9f, 0.18f } },
            { "block", new[] { 1.0f, 0.2f } }, { "heal", new[] { 1.0f, 0.2f } }, { "buff", new[] { 1.0f, 0.2f } }, { "debuff", new[] { 1.0f, 0.2f } },
            { "status", new[] { 1.0f, 0.2f } }, { "death", new[] { 1.2f, 0.25f } }, { "ult", new[] { 1.6f, 0.3f } }, { "ult.cutin", new[] { 0f, 0f } },
            { "turn", new[] { 0.8f, 0.15f } }, { "battle", new[] { 1.4f, 0.3f } }, { "boss", new[] { 0f, 0f } }, { "victory", new[] { 0f, 0f } },
            { "defeat", new[] { 0f, 0f } }, { "map", new[] { 1.2f, 0.25f } }, { "reward", new[] { 1.2f, 0.25f } }, { "coin", new[] { 0.6f, 0.12f } },
            { "shop", new[] { 0.9f, 0.18f } }, { "camp", new[] { 1.2f, 0.25f } }, { "event", new[] { 1.2f, 0.25f } },
        };
        public static readonly Dictionary<string, float[]> KIND_TAIL = new Dictionary<string, float[]>
        {
            { "attack", new[] { 1.2f, 0.2f } }, { "attackHit", new[] { 0.9f, 0.18f } }, { "power", new[] { 1.4f, 0.25f } }, { "powerHit", new[] { 1.0f, 0.2f } },
            { "skill", new[] { 1.4f, 0.25f } }, { "skillHit", new[] { 1.0f, 0.2f } }, { "ult", new[] { 0f, 0f } }, { "ult2", new[] { 0f, 0f } },
            { "ultHit", new[] { 1.2f, 0.25f } }, { "ultBoom", new[] { 0f, 0f } }, { "hit", new[] { 0.9f, 0.18f } },
        };
        public static readonly Dictionary<string, float> KIND_V = new Dictionary<string, float>
        {
            { "attack", 0.6f }, { "power", 0.65f }, { "skill", 0.65f }, { "ult", 0.85f }, { "ult2", 0.9f }, { "ultBoom", 0.9f },
            { "attackHit", 0.85f }, { "powerHit", 0.85f }, { "skillHit", 0.85f }, { "ultHit", 0.9f }, { "hit", 0.85f },
        };
        public const float JITTER = 0.025f;
        public static readonly Dictionary<string, string> ALIAS = new Dictionary<string, string> { { "camp.rest", "heal" }, { "flash", "camp.train" } };

        // 사도 제 소리 — hero/<원작 이름>/<원작 이름>_<꼬리>. 꼬리로 갈래를 가른다(1 · 01 · _1 은 이어지는 소리의 첫 토막)
        public static readonly Dictionary<string, Regex> HERO_KINDS = new Dictionary<string, Regex>
        {
            { "attack", new Regex(@"^basicattack(?:_?0?1)?$") },
            { "attackHit", new Regex(@"^basicattack_?hit(?:_?\d+)?$") },
            { "power", new Regex(@"^power(?:attack|attck)(?:_?0?1)?$") },
            { "powerHit", new Regex(@"^power(?:attack|attck)\d*_?hit(?:_?\d+)?$") },
            { "skill", new Regex(@"^(?:skillcast|spskill)(?:_?0?1|_cast)?$") },
            { "skillHit", new Regex(@"^(?:skill_?hit|spskill\d*_hit)(?:_?\d+)?$") },
            { "ult", new Regex(@"^ultimate(?:_?0?1|1-1|_cast(?:ing)?|_charge|_normal|_crew1|_crowd)?$") },
            { "ult2", new Regex(@"^ultimate_?0?2$") },
            { "ultHit", new Regex(@"^ultimate[\d_-]*_?hit(?:_?\d+)?$") },
            { "ultBoom", new Regex(@"^ultimate_?(?:explosion|finalexplosion|laserexplosion|bombing)\d*$") },
        };
        public static readonly Dictionary<string, Regex> ENEMY_KINDS = new Dictionary<string, Regex>
        {
            { "attack", new Regex(@"^basicattack(?:_?0?1)?$") },
            { "hit", new Regex(@"^basicattack_?hit(?:_?\d+)?$") },
            { "skill", new Regex(@"^skillcast(?:_?0?1)?$") },
            { "skillHit", new Regex(@"^skill_?hit(?:_?\d+)?$") },
        };
        public static readonly Dictionary<string, string> FALLBACK = new Dictionary<string, string>
        {
            { "attack", "card.공격" }, { "attackHit", "hit.slash" }, { "power", "card.공격" }, { "powerHit", "hit.heavy" },
            { "skill", "card.스킬" }, { "skillHit", "hit.magic" }, { "ult", "ult.cutin" }, { "ult2", "ult.impact" }, { "ultHit", "ult.impact" },
            { "hit", "hurt" },
        };

        static readonly Regex HEAD = new Regex("^[a-z0-9]+_");
        public static string TailOf(string dir, string stem) => stem.StartsWith(dir + "_") ? stem.Substring(dir.Length + 1) : HEAD.Replace(stem, "", 1);

        // 색인(열쇠 목록)에서 폴더 하나의 갈래별 파일
        public static Dictionary<string, List<string>> KindsIn(IEnumerable<string> keys, string top, string dir, Dictionary<string, Regex> kinds)
        {
            var pre = top + "/" + dir + "/";
            var res = new Dictionary<string, List<string>>();
            foreach (var k in keys)
            {
                if (!k.StartsWith(pre) || k.IndexOf('/', pre.Length) >= 0) continue;
                var t = TailOf(dir, k.Substring(pre.Length));
                foreach (var kv in kinds) if (kv.Value.IsMatch(t)) { if (!res.TryGetValue(kv.Key, out var l)) res[kv.Key] = l = new List<string>(); l.Add(k); }
            }
            return res;
        }

        // 적 key → 효과음 폴더. 같은 이름이 있으면 그것, 없으면 key 안에 든 가장 긴 폴더 이름
        public static string MonsterDir(string key, ICollection<string> dirs)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var k = key.ToLowerInvariant();
            if (dirs.Contains(k)) return k;
            string best = null;
            foreach (var d in dirs) if (d.Length >= 4 && k.Contains(d) && (best == null || d.Length > best.Length)) best = d;
            return best;
        }

        // ── 동작의 소리 칸 ── 원작 스파인의 SFX(n) 이벤트는 그 동작이 쓰는 소리 목록의 n 번째 칸 — 파일 이름으로 줄 세워 짐작한다
        public static readonly Dictionary<string, Regex> SLOT_GROUPS = new Dictionary<string, Regex>
        {
            { "attack", new Regex("^basicattack") }, { "power", new Regex("^power(?:attack|attck)") },
            { "skill", new Regex("^(?:skillcast|spskill|skill_?hit)") }, { "ult", new Regex("^ultimate") },
        };
        static readonly Regex SLOT_HIT = new Regex("hit|heal");
        static readonly Regex SLOT_EARLY = new Regex("^(cast|casting|ready|charge|start|normal|spawn|swing|jump|magiccircle|holypower|upgrade|mark|crew|crowd)$");
        static readonly Regex SLOT_LATE = new Regex("^(explosion|laserexplosion|finalexplosion|end|revival)$");
        static readonly Regex NUM = new Regex(@"^0*(\d+)(?:-(\d+))?$");
        static readonly Regex NAMED = new Regex(@"^([a-z]+)_?0*(\d*)");

        public static int[] SlotKey(string group, string tail)
        {
            var head = SLOT_GROUPS[group].Match(tail).Value;
            var r = tail.Substring(head.Length).TrimStart('_', '-');
            int h = head == "spskill" ? 1 : 0;
            if (r.Length == 0) return new[] { h, 0, 0, 0, 0 };
            var m = NUM.Match(r);
            if (m.Success) return new[] { h, 1, 0, int.Parse(m.Groups[1].Value), m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0 };
            m = NAMED.Match(r);
            var w = m.Success ? m.Groups[1].Value : r;
            return new[] { h, 2, SLOT_EARLY.IsMatch(w) ? 0 : SLOT_LATE.IsMatch(w) ? 2 : 1, m.Success && m.Groups[2].Value.Length > 0 ? int.Parse(m.Groups[2].Value) : 0, 0 };
        }

        public class Slots { public List<string> List = new List<string>(), Hit = new List<string>(); }

        public static Slots SlotsIn(IEnumerable<string> keys, string dir, string group)
        {
            var pre = "hero/" + dir + "/";
            var list = new List<(int[] k, string t, string p)>();
            var hit = new List<(int[] k, string t, string p)>();
            foreach (var k in keys)
            {
                if (!k.StartsWith(pre) || k.IndexOf('/', pre.Length) >= 0) continue;
                var t = TailOf(dir, k.Substring(pre.Length));
                if (!SLOT_GROUPS[group].IsMatch(t)) continue;
                (SLOT_HIT.IsMatch(t) ? hit : list).Add((SlotKey(group, t), t, k));
            }
            int Cmp((int[] k, string t, string p) a, (int[] k, string t, string p) b)
            {
                for (int i = 0; i < 5; i++) if (a.k[i] != b.k[i]) return a.k[i] - b.k[i];
                return string.CompareOrdinal(a.t, b.t) < 0 ? -1 : 1;
            }
            list.Sort(Cmp); hit.Sort(Cmp);
            return new Slots { List = list.Select(x => x.p).ToList(), Hit = hit.Select(x => x.p).ToList() };
        }

        // SFX 칸 번호들 → 칸마다 경로 · "hit"(맞는 소리 칸). 칸 수가 안 맞으면 null
        public static Dictionary<int, string> SlotMap(Slots slots, IEnumerable<int> ns)
        {
            var u = ns.Distinct().OrderBy(x => x).ToList();
            int L = slots.List.Count;
            if (L == 0 || u.Count == 0) return null;
            var m = new Dictionary<int, string>();
            if (u.Count == L) { for (int i = 0; i < L; i++) m[u[i]] = slots.List[i]; return m; }
            if (u.Count == L + 1 && slots.Hit.Count > 0) { for (int i = 0; i < u.Count; i++) m[u[i]] = i < L ? slots.List[i] : "hit"; return m; }
            return null;
        }
    }
}
