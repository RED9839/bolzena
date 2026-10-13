using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Bolzena.Fx
{
    // 이펙트가 붙는 자리 — 원작 SD 스파인은 모두(135명 · 적) 이 본들을 갖고 있고, 원작 이펙트는 이 본에 붙도록 짜였다.
    //   Bottom 발밑 · Middle 몸 가운데(맞는 자리) · Top 머리 위 · Front 몸 앞 · Back 몸 뒤 · Shadow 그림자
    //   Attack1 · Attack2 · Skill1 · Ult1 … 동작마다 쏘는/뻗는 자리(Point_Attack1 · Point_Attack1_Shot) · SkillReady 모으는 자리
    // 스파인이 없으면(그림 한 장) 그림 칸 높이로 짐작한다(FxActor.Guess).
    public enum FxAnchor { Feet, Body, Head, Front, Cast, Screen }

    // 유닛 하나 — 전투 쪽(UnitView)이 채워 넘긴다. 스파인이면 Bolzena.Fx.SpineFx.Actor(skeletonAnimation, …) 가 본까지 채운다
    public class FxActor
    {
        public Func<Vector3> FeetAt;                 // 발밑(월드) — 달려가는 유닛이면 매번 읽는다
        public float Height = 1.5f;                  // 그림 높이(월드) — 본이 없을 때 몸 · 머리 자리 짐작
        public bool Party;                           // 사도 쪽(왼쪽에 서서 오른쪽을 본다)
        public string Key;                           // 사도 키(에르핀) · 적 키(curburus)
        public Func<string, Vector3?> Point;         // 본 이름(「Middle」 → Point_Middle) → 월드. 없으면 null
        public Func<Vector3?> Muzzle;                // 총구(MotionTables.MUZZLE · 이름에 muzzle/barrel)
        public Func<Vector3?> Weapon;                // 무기 끝(MotionTables.WEAPON — 그 순간 가장 앞의 무기 · 손 끝). 카드 투사체 · 총구 불꽃 · 쏘는 한 대. 없으면 null
        public bool Alive = true;

        public Vector3 Feet => FeetAt != null ? FeetAt() : Vector3.zero;
        // 웹판 bodyOf — 그림 칸 위에서 58%(발은 아래 6%) → 발에서 높이의 36%
        public Vector3 Guess(FxAnchor a)
        {
            var f = Feet;
            switch (a)
            {
                case FxAnchor.Body: return f + new Vector3(0, Height * 0.36f, 0);
                case FxAnchor.Head: return f + new Vector3(0, Height * 0.88f, 0);
                case FxAnchor.Front: return f + new Vector3((Party ? 1 : -1) * Height * 0.2f, Height * 0.36f, 0);
                case FxAnchor.Cast: return f + new Vector3((Party ? 1 : -1) * Height * 0.15f, Height * 0.45f, 0);
                default: return f;
            }
        }

        public Vector3? Bone(string point) => Point != null && point != null ? Point(point) : null;

        // 자리 하나 — 본이 있으면 본, 없으면 짐작
        public Vector3 At(FxAnchor a, string castPoint = null)
        {
            switch (a)
            {
                case FxAnchor.Feet: return Bone("Bottom") ?? Feet;
                case FxAnchor.Body: return Bone("Middle") ?? Guess(a);
                case FxAnchor.Head: return Bone("Top") ?? Guess(a);
                case FxAnchor.Front: return Bone("Front") ?? Guess(a);
                case FxAnchor.Cast: return (castPoint != null ? Bone(castPoint) : null) ?? Muzzle?.Invoke() ?? Bone("SkillReady") ?? Bone("Middle") ?? Guess(a);   // Front 는 땅 높이라 뺐다
                case FxAnchor.Screen: return BolzenaFx.ScreenCenter();
            }
            return Feet;
        }

        public static FxActor Of(Transform t, float height, bool party, string key = null)
            => new FxActor { FeetAt = () => t != null ? t.position : Vector3.zero, Height = height, Party = party, Key = key };
        public static FxActor Fixed(Vector3 feet, float height, bool party, string key = null)
            => new FxActor { FeetAt = () => feet, Height = height, Party = party, Key = key };
    }

    // 공용 이펙트 한 갈래의 규칙
    public class CommonRule
    {
        public string[] Names;           // 원작 이펙트(겹쳐 튼다). 비면 FxLibrary.Common(kind) 의 첫 것
        public FxAnchor At = FxAnchor.Feet;
        public float Scale = 1f;
        public float Until = 2f;         // 이 초까지(그 뒤 0.4초 걷힘) — 고리 · 오래 도는 원작 이펙트를 전투 박자에 맞게 끊는다
        public int Order = 310;
        public bool Front = true;        // 유닛 앞에 그린다(아니면 뒤 — 바닥 고리)
        public string Sound;             // BolzenaAudio 공용 갈래(SfxMap.SFX) — null 이면 소리 없음
    }

    // ── 표 ── 웹판(js/fight-screen.js · fx-burst.js)에서 옮긴 시각 · 자리 규칙 + 볼제나 유니티에서 새로 정한 공용 갈래.
    // 숫자를 고칠 때는 여기만 고친다. 웹판에 있던 것은 그 자리를 주석에 적었다.
    public static class FxRules
    {
        // ── 맞음(웹판 sparkFx) ──
        public const float HIT_SCALE = 0.95f, HIT_SCALE_ULT = 1.1f;        // 기본 · 고학년
        public const float HIT_CRIT = 1.25f, HIT_HEAVY = 1.12f;            // 배율 — 치명 · 세게
        public const float HIT_JITTER_X = 0.12f, HIT_JITTER_Y = 0.1f;      // 맞은 자리를 그림 폭 · 높이의 이만큼 흩는다
        public const float CRIT_SCALE = 0.95f;                              // 치명 덧불
        public const float BIG_SCALE = 0.9f, BIG_SCALE_ULT = 1.05f;        // 세게 맞음 · 고학년 첫 타격 덧불
        public const float BIG_DY = -0.04f;                                  // 덧불은 4px 아래(웹판 1px = 월드 0.01)
        // 둔기 세게 맞음 덧불(fx_common_hit_explosion_1_m)은 흰 폭발이 6칸째부터 검은 연기(구운 낱장 15칸 중 9칸)로 바뀐다 — 원작은 디졸브로 걷히는데
        //   구운 판은 불투명 검정이라 적 몸 위 검은 연기 덩어리로 남았다(2026-10-13 디아나 · 벨라 · 에르핀(왕도) 카드 · 고학년). 흰 폭발까지만(앞 4칸 · 0.14초 — 5칸째부터 검은 구멍) 틀고 바로 걷는다
        public const float BIG_BLUNT_CUT = 0.14f;

        // ── 고학년 · 카드 시각(웹판 fight-screen · fx-burst) ──
        public const int HIT_AFTER = 60;          // 때리는 순간(스파인 이벤트) + 투사체 몫 + 이만큼 = 숫자 · 체력 · 맞음 이펙트
        public const int CARD_HIT = 260;          // 카드 동작의 때리는 순간을 모를 때(이벤트 없음)
        public const int CARD_MAX = 8;            // 카드 이펙트 한 번에 이만큼만
        public const float TOP_PAD = 0.24f;       // 화면 위끝에서 이만큼 아래가 이펙트 위끝(웹판 field.top + 24px)

        // 동작 → 카드 이펙트 갈래(원작 이펙트 이름의 attack · attack2 · skill · signaturecard) — 앞에서부터 있는 것
        public static string[] CardKinds(string anim, string tier)
        {
            if (tier == "sig") return new[] { "sig", "skill" };
            if (string.IsNullOrEmpty(anim)) return new string[0];
            if (anim.StartsWith("Attack2")) return new[] { "power", "attack" };
            if (anim.StartsWith("Attack")) return new[] { "attack" };
            if (anim.StartsWith("Skill")) return new[] { "skill" };
            if (anim.StartsWith("Ultimate")) return new[] { "ult" };
            return new string[0];                 // Victory · Spawn · 제자리 — 원작에 동작 이펙트가 없다(공용 buff 로)
        }

        // 근접인가 — 원작 평타 이펙트 이름으로(투사체 · 총구 · 레이저 · 화살이 있으면 원거리). 평타 이펙트가 없으면 null(모름)
        static readonly Regex RANGED = new Regex(@"(proj|bullet|missile|shot|arrow|muzzle|laser|lazer|beam|throw|crosshair)");
        public static bool? MeleeByFx(string heroKey)
        {
            var a = FxLibrary.Get(heroKey, "attack");
            if (a.Length == 0) return null;
            return !a.Any(n => RANGED.IsMatch(n));
        }

        // 동작 → 시전 본(Point_<이것>) — 쏘는 · 모으는 자리
        public static string CastPoint(string anim)
        {
            if (string.IsNullOrEmpty(anim)) return null;
            var m = Regex.Match(anim, @"^(Attack|Skill|Ultimate)(\d+)");
            if (!m.Success) return null;
            return (m.Groups[1].Value == "Ultimate" ? "Ult" : m.Groups[1].Value) + m.Groups[2].Value;
        }

        // ── 공용(볼제나 유니티 — 웹판은 CSS 연출이었다) ── 원작에 공용 회복 · 실드 프리팹은 없어 증강(augment) · 다른 사도의 것을 빌렸다.
        // 후보는 Tools/fx_extract_more.py COMMON, 고른 것은 시험 시트(-fxsweep common → Captures/sweep/cands_*.jpg)로 눈으로 봤다(2026-10-05):
        // 증강(augment) 회복 · 실드는 너무 옅고, 리온 강화 · 약화는 화면을 덮어 뺐다. 격파는 기절 별(루포)을 머리 위에
        public static readonly Dictionary<string, CommonRule> COMMON = new Dictionary<string, CommonRule>
        {
            { "heal",      new CommonRule { Names = new[] { "fx_cuee_skill_heal_1" }, At = FxAnchor.Feet, Until = 1.6f, Sound = null } },
            { "shield",    new CommonRule { Names = new[] { "fx_ed_shield_1" }, At = FxAnchor.Feet, Until = 1.4f } },
            { "shieldHit", new CommonRule { Names = new[] { "fx_ed_shieldhit_1" }, At = FxAnchor.Body, Until = 0.8f, Order = 330 } },
            { "buff",      new CommonRule { Names = new[] { "fx_rude_skill_buff_1" }, At = FxAnchor.Feet, Until = 1.4f } },
            { "debuff",    new CommonRule { Names = new[] { "fx_bana_attack_debuff_1" }, At = FxAnchor.Feet, Until = 1.4f } },
            { "break",     new CommonRule { Names = new[] { "fx_rufo_ultimate_stun" }, At = FxAnchor.Head, Until = 1.8f, Order = 335 } },
            { "kill",      new CommonRule { Names = new[] { "fx_e0_die_smoke_1" }, At = FxAnchor.Feet, Until = 1.5f } },
            { "oracle",    new CommonRule { Names = new[] { "fx_epica_ultimate_buff_start" }, At = FxAnchor.Feet, Until = 1.8f, Scale = 1.1f } },
            { "revive",    new CommonRule { Names = new[] { "fx_common_waitrevive" }, At = FxAnchor.Feet, Until = 1.6f } },
        };

        // 공용 갈래가 쓰는 이름(없으면 색인의 첫 후보)
        public static string[] CommonNames(string kind)
        {
            if (COMMON.TryGetValue(kind, out var r) && r.Names != null && r.Names.Length > 0 && r.Names.All(FxLibrary.Has)) return r.Names;
            var c = FxLibrary.Common(kind);
            return c.Length > 0 ? new[] { c[0] } : new string[0];
        }

        // ── 카드 이펙트 고르기 ── 원작은 동작 이벤트마다 이름을 콕 집어 트는데(데이터 테이블 — 열지 않는다), 여기서는 이름 낱말로 고른다.
        // 같은 이름의 번호만 다른 판(slash04 · slash05 …)은 하나만, 갈래마다 몇 개까지(모으기 2 · 투사체 1 · 시전자 2 · 대상 3)
        static readonly Regex HITW = new Regex(@"(_|attack|water)hit(_|\d|$)|hitslash");   // attackhit · waterhit(이드 · 우이) — 원점에 짜인 맞음 장인데 발밑에 그려지던 것(2026-10-13)
        static readonly Regex SLASHW = new Regex(@"_slash(_|\d|$)");
        static readonly Regex TAIL = new Regex(@"((?<=[a-z])\d+)?(_default)?$");   // slash04 · slash05 같은 번호 판만 하나로(_1_1 · _1_2 는 다른 판이라 둔다)
        public static List<UltPart> CardPlan(string heroKey, string anim, string tier)
        {
            // 사도별로 이 몸짓에 쓸 원작 장을 손으로 정한 것(CARD_NAMES — 강공 전용 장이 없는 사도 · 평타 장이 섞이던 사도)
            var mk = MotionOf(anim);
            if (tier != "sig" && mk != null && heroKey != null && CARD_NAMES.TryGetValue(heroKey + "|" + mk, out var fixedNames))
            {
                var have = fixedNames.Where(FxLibrary.Has).ToList();
                if (have.Count > 0) return Shape(have, false);
            }
            foreach (var kind in CardKinds(anim, tier))
            {
                var names = FxLibrary.Get(heroKey, kind).ToList();
                if (names.Count == 0) continue;
                if (kind == "ult") return UltFx.Plan(heroKey);
                return Shape(names, true);
            }
            return new List<UltPart>();
        }

        // 이름들 → 카드 계획(번호 판 하나만 · 자리 고침 · 갈래마다 몇 장까지). 손으로 고른 이름(dedupe false)은 번호 판도 그대로 — 타격마다 차례로 튼다
        static List<UltPart> Shape(List<string> names, bool dedupe)
        {
            if (dedupe) { var seen = new HashSet<string>(); names = names.Where(n => seen.Add(TAIL.Replace(n, ""))).ToList(); }
            var plan = UltFx.PlanNames(names, null, 99);
            foreach (var p in plan) if (p.At == "target" && HITW.IsMatch(p.Name) && !p.Name.Contains("ground")) p.Body = true;
            // 원점에 짜인 베기(slash — 이미터가 뿌리 높이 1 단위 안)도 맞는 몸에 — 아야 평타 초승달이 적 발밑 · 비비 평타 베기가 달려간 자리(시전자 쪽)에 떴다(2026-10-14 원작 비교)
            foreach (var p in plan) if (p.At == "target" && !p.Body && SLASHW.IsMatch(p.Name) && !p.Name.Contains("ground") && FxLibrary.EmitterTop(p.Name) < 1f) p.Body = true;
            // 총구 불꽃 판(텍스처 FX_Muzzle) — 이름은 fire 라 대상에 가던 것(시온 attack_fire)을 시전자 총구(무기 끝)에
            foreach (var p in plan) if (p.At == "target" && !p.Pre && FxLibrary.MuzzleTex(p.Name)) { p.At = "caster"; p.Muzzle = true; p.Body = false; }
            // 이름 낱말로는 자리가 틀리는 장(CARD_AT)
            foreach (var p in plan)
                if (CARD_AT.TryGetValue(p.Name, out var at)) { p.Muzzle = at == "muzzle"; p.At = p.Muzzle ? "caster" : at; p.Ally = false; p.Body = false; p.Pre = false; }
            var res = new List<UltPart>();
            int pre = 0, mv = 0, cs = 0, tg = 0, sc = 0;
            foreach (var p in plan)
            {
                if (p.Pre) { if (pre++ < 2) res.Add(p); continue; }
                if (p.At == "move") { if (mv++ < 1) res.Add(p); continue; }
                if (p.At == "caster") { if (cs++ < 2) res.Add(p); continue; }
                if (p.At == "screen") { if (sc++ < 1) res.Add(p); continue; }
                if (tg++ < 5) res.Add(p);
            }
            return res.Take(CARD_MAX).ToList();
        }

        static string MotionOf(string anim)
        {
            if (string.IsNullOrEmpty(anim)) return null;
            var m = Regex.Match(anim, @"^(Attack|Skill)(\d)");
            return m.Success ? m.Groups[1].Value + m.Groups[2].Value : null;
        }

        // ── 사도별 카드 장 고르기(2026-10-13 엘다인 20명 카드 몸짓 손보기) ── 「사도|Attack2」 → 원작 장 이름들.
        //   원작에 강공(attack2) 전용 장이 없는 10명은 강공이 평타와 똑같아 보였다 — 같은 사도의 평타 · 스킬 장 가운데 큰 것(충격파 · 폭발 · 레이저 · 바닥)을 섞고,
        //   크기는 CARD_SCALE 로 평타보다 키운다. 고학년(ultimate) 장은 쓰지 않는다. 장이 하나도 없으면(이름 바뀜) 예전 갈래대로
        static readonly Dictionary<string, string[]> CARD_NAMES = new Dictionary<string, string[]>
        {
            ["비비|Attack2"] = new[] { "fx_vivi_attack_slash_1", "fx_vivi_attack_slash_2", "fx_vivi_attack_stomp_1", "fx_vivi_attack_hit_2" },
            // 2026-10-13 캡처 검토 — 평타 갈래에도 충격파(shockwave)가 있어 평타 = 강공으로 보였다: 평타는 쏘는 보석(proj) · 맞음만
            ["에르핀_왕도|Attack1"] = new[] { "fx_erpinroyale_attack1_cast_1", "fx_erpinroyale_attack1_proj_1", "fx_erpinroyale_attack1_hit_1" },
            //   (2026-10-14) 충격파(shockwave_1)는 보라 · 분홍 바닥 빛이 뒤로 11 단위 pivot 이라 적에 틀어도 시전자 발밑에 깔렸다 — 맞음 둘만
            ["에르핀_왕도|Attack2"] = new[] { "fx_erpinroyale_attack1_cast_1", "fx_erpinroyale_attack1_shockwave_hit_1", "fx_erpinroyale_skill1_hit_1" },
            ["란|Attack2"] = new[] { "fx_ran_attack_cast_2_default", "fx_ran_skill_slash_3_default", "fx_ran_skill_hit_1_default", "fx_ran_attack_hit_1_default" },
            // 디아나(왕년) — 주먹(punch) + 연타(hit 1~3 은 타격마다 차례로) · 내려찍는 바닥(스킬 ground) — 대상 장 상한 5장 안
            ["디아나_왕년|Attack2"] = new[] { "fx_dianayester_attack_punch_1", "fx_dianayester_attack_hit_1", "fx_dianayester_attack_hit_2", "fx_dianayester_attack_hit_3", "fx_dianayester_skill_ground_1" },
            // 클로에 — 원작 강공은 대폭발: 스킬 바닥 폭발(ground) · 큰 맞음(hit)
            ["클로에|Attack2"] = new[] { "fx_chloe_attack_slash_1", "fx_chloe_skill_ground_1", "fx_chloe_skill_hit_1", "fx_chloe_skill_hit_2" },
            // 리뉴아 — 원작 강공은 큰 빔: 평타 레이저를 크게 + 스킬 충격(impact)
            //   (2026-10-13) 스킬 충격(impact)을 빌려 강공 = 스킬로 보였다 — 강공은 평타 레이저를 크게 · 맞음 둘, 충격은 스킬만.
            //   평타는 레이저 대신 쏘는 빛(proj) — 레이저가 치켜든 지팡이 끝에서 오른쪽 위 허공으로 뻗었다(카드 총구 장은 이제 적 쪽으로 돌린다)
            ["리뉴아|Attack1"] = new[] { "fx_renewa_attack_cast_1", "fx_renewa_attack_proj_1", "fx_renewa_attack_hit_1" },
            ["리뉴아|Attack2"] = new[] { "fx_renewa_attack_cast_3", "fx_renewa_attack_lazer_1", "fx_renewa_attack_hit_1", "fx_renewa_attack_hit_2" },
            ["티그_영웅|Attack2"] = new[] { "fx_tighero_attack_aura_1", "fx_tighero_attack_slash_1", "fx_tighero_attack_slash_2", "fx_tighero_attack_hit_3", "fx_tighero_attack_hit_4", "fx_tighero_skill_hit_1" },
            // 에피카 — 평타 갈래에 연주(music_*) 장이 섞여 평타 · 강공이 같은 장을 다 틀었다: 평타는 음표 탄 · 맞음만, 강공은 연주 · 바닥 울림 + 큰 탄
            //   (2026-10-14 원작 영상) 평타는 시전자 둘레 보라 마법 원(music_aura_1, 0.3~0.6초) · 강공은 시전자 발밑 원반 울림(aura_ground) — 둘 다 시전자 쪽
            ["에피카|Attack1"] = new[] { "fx_epica_music_aura_1", "fx_epica_attack_proj_1", "fx_epica_attack_hit_1" },
            ["에피카|Attack2"] = new[] { "fx_epica_music_play_1", "fx_epica_music_aura_ground_1", "fx_epica_attack_proj_2", "fx_epica_attack_hit_2" },
            //   (2026-10-13) 에피카는 원작 스킬 장이 없다(라이브러리 attack 12장뿐 — 원본 번들에도 skill 프리팹 없음) — 스킬은 연주 둘째 판 · 음표 고리 · 큰 바닥 울림
            ["에피카|Skill1"] = new[] { "fx_epica_music_aura_ground_2", "fx_epica_music_aura_4", "fx_epica_attack_proj_2", "fx_epica_attack_hit_2" },   // 시전자 장 상한 2 — 원반 · 고리
            // 이드 스킬 — 원작은 시전자를 감싸는 보라 구체 보호막(0.9~2.7초) + 발밑 소용돌이. 우리 스킬 갈래엔 시전 장뿐이라 보호막 장(fx_ed_shield_1 — 공용 실드로도 쓰는 이드 원작 장)을 시전자에
            ["이드|Skill1"] = new[] { "fx_ed_skillcast_1", "fx_ed_shield_1" },
            // 죠안 스킬 — 원작은 1.8~2.3초 시전자 둘레 주황 큰 폭발(cast_2 — 깃털 · 붉은 조각 · 고리). 준비 장 상한 2 에 밀려 빠졌다 → 준비 · 시전자 폭발 · 맞음
            ["죠안|Skill1"] = new[] { "fx_joanne_skill_ready_1", "fx_joanne_skill_cast_1", "fx_joanne_skill_cast_2", "fx_joanne_skill_target_1" },
            // 우이(기억) 스킬 — 강화 장(buff_1 · clover, 「buff」 낱말로 시전자에 붙음)이 맞는 적의 폭발 고리(explosion_bottom)와 같은 고리 · 기둥이라 시전자 · 적 양쪽에 떴다(2026-10-14) — 강화 장은 뺀다
            ["우이_기억|Skill1"] = new[] { "fx_uimemory_skill1_cast_1_default", "fx_uimemory_skill1_cast_2_default", "fx_uimemory_skill1_explosion_bottom_1_default", "fx_uimemory_skill1_explosion_center_1_default" },
            ["우이|Attack2"] = new[] { "fx_uieru_watercast_1", "fx_uieru_proj_1", "fx_uieru_skill_waterhit_1", "fx_uieru_skill_waterhit_2", "fx_uieru_skill_ground_1" },
            //   (2026-10-13) 스킬 폭발을 빌려 강공 = 스킬로 보였다 — 강공은 조준 둘 · 연사 맞음, 폭발은 스킬만
            //   (2026-10-14) 평타 갈래가 조준 · 눈빛 · 바닥까지 다 틀어 강공 ≈ 평타였다 — 평타는 한 발(불꽃 · 탄 · 맞음), 강공은 두 불꽃 · 조준 둘 · 맞음 둘 · 푸른 바닥 충격파
            ["시온더다크불릿|Attack1"] = new[] { "fx_xxionx_attack_fire_1", "fx_xxionx_attack_bullet_1", "fx_xxionx_attack_hit_1" },
            ["시온더다크불릿|Attack2"] = new[] { "fx_xxionx_attack_fire_1", "fx_xxionx_attack_fire_2", "fx_xxionx_attack_bullet_1", "fx_xxionx_crosshair_1", "fx_xxionx_crosshair_3", "fx_xxionx_attack_hit_1", "fx_xxionx_attack_hit_2", "fx_xxionx_ground_1" },
        };

        // 카드 장 크기 배율 — 「사도|몸짓」. 강공이 평타 장을 빌리면 평타보다 크게(1.1~1.3)
        static readonly Dictionary<string, float> CARD_SCALE = new Dictionary<string, float>
        {
            ["비비|Attack2"] = 1.25f, ["에르핀_왕도|Attack2"] = 1.2f, ["란|Attack2"] = 1.15f, ["디아나_왕년|Attack2"] = 1.1f, ["클로에|Attack2"] = 1.15f,
            ["리뉴아|Attack2"] = 1.3f, ["티그_영웅|Attack2"] = 1.2f, ["에피카|Attack2"] = 1.15f, ["우이|Attack2"] = 1.15f, ["시온더다크불릿|Attack2"] = 1.15f,
            // 2026-10-13 — 에피카 스킬은 강공보다 크게 · 비비(신성) 강공은 평타보다 크게 · 우로스 스킬 장판(크기 58~80)이 수풀 위까지 덮던 것을 줄인다
            ["에피카|Skill1"] = 1.3f, ["비비_신성|Attack2"] = 1.2f, ["우로스|Skill1"] = 0.75f,
        };
        public static float CardScale(string heroKey, string anim)
        {
            var mk = MotionOf(anim);
            return mk != null && heroKey != null && CARD_SCALE.TryGetValue(heroKey + "|" + mk, out var s) ? s : 1f;
        }

        // 장 자리 고침 — 이름 낱말로는 틀리는 것
        //   우로스 강공 금빛 용(snake) — 이미터가 시전자 뒤(-19)에서 앞(+34)으로 짜인 판인데 낱말이 없어 대상에 가운데 맞춰 줄어 보였다 → 시전자 발밑에서 적 쪽으로
        //   죠안 강공 표식(buff_1_target …) — 「buff」 낱말로 시전자에 가던 것 → 맞는 적
        //   에피카 연주(music_play) — 낱말이 없어 대상에 가던 것 → 시전자
        static readonly Dictionary<string, string> CARD_AT = new Dictionary<string, string>
        {
            ["fx_uros_attack2_snake_jolly"] = "caster",
            ["fx_joanne_attack2_1_buff_1_target"] = "target",
            ["fx_joanne_attack2_1_buff_1_target_bottom"] = "target",
            ["fx_joanne_attack2_1_buff_1_target_top"] = "target",
            ["fx_epica_music_play_1"] = "caster",
            ["fx_epica_music_play_2"] = "caster",
            //   에피카 연주 고리 · 바닥 울림(music_aura · aura_ground) — 원작 영상(2026-10-14)은 시전자 둘레 · 발밑. 적에 옮겼던 것을 되돌리고 맞는 때(준비 아님)에
            ["fx_epica_music_aura_1"] = "caster",
            ["fx_epica_music_aura_4"] = "caster",
            ["fx_epica_music_aura_ground_1"] = "caster",
            ["fx_epica_music_aura_ground_2"] = "caster",
            //   죠안 스킬 시전자 둘레 폭발(cast_2) — 준비가 아니라 폭발 때(맞는 때)
            ["fx_joanne_skill_cast_2"] = "caster",
            //   이드 강공 레이저(attack_2) — 낱말이 없어 적 발밑에 가던 것. 원작은 공 앞(닫힌 유리)의 푸른 핵에서 적 쪽 바닥으로 쏜다 → 시전자 총구(손)에 붙여 적 쪽으로(FxRun.AIM_BASE)
            ["fx_ed_attack_2"] = "muzzle",
            // 2026-10-13 캡처 검토 — 시전자 발밑에 서던 세로선 · 기둥 · 원을 맞는 적 쪽으로
            //   비비(신성) 스킬 강화 판(buff — 아군 쪽 낱말로 시전자에 갔다 · 빛기둥 FX_IN_Piller_02) · 에르핀(왕도) 스킬 오라(aura — 시전 낱말) · 우이(기억) 강공 연못(pond)
            ["fx_vividivine_skill_1_buff_default"] = "target",
            ["fx_erpinroyale_skill1_aura_1"] = "target",
            //   (2026-10-14 원작 영상) 우이(기억) 강공 푸른 원진(pond) · 스킬 클로버 원진(explosion_bottom · center)은 시전자 발밑(원작 저학년 1.4~1.9초) — 적에 두던 것을 되돌림
            //   우이 스킬 큰 클로버 · 무지개 · 난초(skill_1 — 낱말이 없어 적에 갔다) — 원작은 시전자 · 아군 위(1.2~3.9초)
            ["fx_uieru_skill_1"] = "caster",
            ["fx_uimemory_attack2_pond_1_default"] = "caster",
            ["fx_uimemory_skill1_explosion_bottom_1_default"] = "caster",
            ["fx_uimemory_skill1_explosion_center_1_default"] = "caster",
            //   우이(기억) — 강공 · 스킬을 시전자 쪽 몸짓(BattleDirector CARD_SELF)에서 뺐으니 노래 · 물길 · 뛰기 · 강화는 시전자에 남긴다
            ["fx_uimemory_attack2_eru_song_1_default"] = "caster",
            ["fx_uimemory_attack2_eru_trail_1_default"] = "caster",
            ["fx_uimemory_attack2_eru_buff_1_default"] = "caster",
            ["fx_uimemory_attack2_jump_1_default"] = "caster",
            ["fx_uimemory_attack2_eru_end_1_default"] = "caster",
        };
    }
}
