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
            { "shield",    new CommonRule { Names = new[] { "fx_ed_shield_1" }, At = FxAnchor.Body, Until = 1.4f } },   // 몸 가운데(10-16 — 원작 구체가 몸을 감싼다)
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
            // 변신 몸짓(Attack1_1_DreamForm …)은 「사도|몸짓|변신」 표가 먼저 — 죠안 꿈결은 붉은 사슬 · 붉은 파동(10-17 사용자)
            var form = anim != null ? Regex.Match(anim, @"_([A-Za-z]+Form)$").Groups[1].Value : "";
            if (tier != "sig" && mk != null && heroKey != null && form != "" && CARD_NAMES.TryGetValue(heroKey + "|" + mk + "|" + form, out var formNames))
            {
                var haveF = formNames.Where(FxLibrary.Has).ToList();
                if (haveF.Count > 0) return PartScale(Shape(haveF, false), heroKey, anim);
            }
            if (tier != "sig" && mk != null && heroKey != null && CARD_NAMES.TryGetValue(heroKey + "|" + mk, out var fixedNames))
            {
                var have = fixedNames.Where(FxLibrary.Has).ToList();
                if (have.Count > 0) return PartScale(Shape(have, false), heroKey, anim);
            }
            foreach (var kind in CardKinds(anim, tier))
            {
                var names = FxLibrary.Get(heroKey, kind).ToList();
                if (names.Count == 0) continue;
                if (kind == "ult") return UltFx.Plan(heroKey);
                return PartScale(Shape(names, true), heroKey, anim);
            }
            return new List<UltPart>();
        }

        // 이름들 → 카드 계획(번호 판 하나만 · 자리 고침 · 갈래마다 몇 장까지). 손으로 고른 이름(dedupe false)은 번호 판도 그대로 — 타격마다 차례로 튼다
        static List<UltPart> Shape(List<string> names, bool dedupe)
        {
            if (dedupe) { var seen = new HashSet<string>(); names = names.Where(n => seen.Add(TAIL.Replace(n, ""))).ToList(); }
            var plan = UltFx.PlanNames(names, null, 99, geo: true);   // 장 안 좌표로 자리(2026-10-14 — 고학년과 같은 판별: 크게 덮거나 오른쪽으로 뻗는 장은 시전자 발밑)
            foreach (var p in plan) if (p.At == "target" && HITW.IsMatch(p.Name) && !p.Name.Contains("ground")) p.Body = true;
            // 원점에 짜인 베기(slash — 이미터가 뿌리 높이 1 단위 안)도 맞는 몸에 — 아야 평타 초승달이 적 발밑 · 비비 평타 베기가 달려간 자리(시전자 쪽)에 떴다(2026-10-14 원작 비교)
            foreach (var p in plan) if (p.At == "target" && !p.Body && SLASHW.IsMatch(p.Name) && !p.Name.Contains("ground") && FxLibrary.EmitterTop(p.Name) < 1f) p.Body = true;
            // 총구 불꽃 판(텍스처 FX_Muzzle) — 이름은 fire 라 대상에 가던 것(시온 attack_fire)을 시전자 총구(무기 끝)에
            foreach (var p in plan) if (p.At == "target" && !p.Pre && FxLibrary.MuzzleTex(p.Name)) { p.At = "caster"; p.Muzzle = true; p.Body = false; }
            // 이름 낱말로는 자리가 틀리는 장(CARD_AT)
            foreach (var p in plan)
                if (CARD_AT.TryGetValue(p.Name, out var at)) { p.Muzzle = at == "muzzle"; p.At = p.Muzzle || at == "caster_body" ? "caster" : at; p.Ally = false; p.Body = at == "caster_body"; p.Pre = false; }
            var res = new List<UltPart>();
            int pre = 0, mv = 0, cs = 0, tg = 0, sc = 0;
            foreach (var p in plan)
            {
                if (p.Pre) { if (pre++ < 2) res.Add(p); continue; }
                if (p.At == "move") { if (mv++ < 1) res.Add(p); continue; }
                if (p.At == "caster") { if (cs++ < 3) res.Add(p); continue; }   // 2 → 3(2026-10-14 — 장 안 좌표로 시전자에 옮긴 큰 베기 · 장판이 밀려 빠지지 않게)
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
            ["에르핀_왕도|Attack1"] = new[] { "fx_erpinroyale_attack1_cast_1", "fx_erpinroyale_attack1_slash_1", "fx_erpinroyale_attack1_proj_1", "fx_erpinroyale_attack1_hit_1", "fx_erpinroyale_attack1_shockwave_hit_1", "fx_erpinroyale_attack1_shockwave_1" },   // (10-17 사용자 지적 · 원작 스파인 이벤트 조사) 충전 → 내지르는 주먹 빛 → 보석 탄 → 맞음 · 부채꼴
            //   (2026-10-14) 충격파(shockwave_1)는 보라 · 분홍 바닥 빛이 뒤로 11 단위 pivot 이라 적에 틀어도 시전자 발밑에 깔렸다 — 맞음 둘만
            ["에르핀_왕도|Attack2"] = new[] { "fx_erpinroyale_attack1_cast_2", "fx_erpinroyale_attack1_slash_2", "fx_erpinroyale_attack1_proj_1", "fx_erpinroyale_attack1_hit_1", "fx_erpinroyale_attack1_shockwave_hit_1", "fx_erpinroyale_attack1_shockwave_1" },   // (10-17 사용자 지적 · 원작 스파인 이벤트 조사) Attack1_2 판(반대 손) — 같은 탄 · 부채꼴
            ["란|Attack2"] = new[] { "fx_ran_attack_slash_2_default", "fx_ran_attack_hit_1_default" },   // (10-17 원작 영상 정밀 분석 orig_analysis) 반달 베기(원작 강공 장) — 스킬 장 빌려 쓰던 것 뺌
            ["란|Skill1"] = new[] { "fx_ran_skill_cast_1_default", "fx_ran_skill_hit_2_default", "fx_ran_skill_slash_4_default", "fx_ran_skill_ready_1_default", "fx_ran_skill_slash_3_default", "fx_ran_skill_hit_1_default" },   // 원작 스킬 — 중간 베기 · 늑대 표식 · 마지막 베기
            // 디아나(왕년) — 주먹(punch) + 연타(hit 1~3 은 타격마다 차례로) · 내려찍는 바닥(스킬 ground) — 대상 장 상한 5장 안
            ["디아나_왕년|Attack2"] = new[] { "fx_dianayester_attack_punch_1", "fx_dianayester_attack_proj_1", "fx_dianayester_attack_hit_3" },   // (10-17 원작 영상 정밀 분석 orig_analysis) 1타(바위 굴려 던짐 → 큰 맞음)
            // 클로에 — 원작 강공은 대폭발: 스킬 바닥 폭발(ground) · 큰 맞음(hit)
            ["클로에|Skill1"] = new[] { "fx_chloe_skill_start_1", "fx_chloe_skill_aura_1" },   // (10-17 원작 영상 정밀 분석 B) 세바스티안에 타는 변신뿐 — 베기 · 피해 뺌
            ["클로에|Attack2"] = new[] { "fx_chloe_attack_slash_1", "fx_chloe_skill_ground_1", "fx_chloe_skill_hit_1", "fx_chloe_skill_hit_2" },
            // 리뉴아 — 원작 강공은 큰 빔: 평타 레이저를 크게 + 스킬 충격(impact)
            //   (2026-10-13) 스킬 충격(impact)을 빌려 강공 = 스킬로 보였다 — 강공은 평타 레이저를 크게 · 맞음 둘, 충격은 스킬만.
            //   평타는 레이저 대신 쏘는 빛(proj) — 레이저가 치켜든 지팡이 끝에서 오른쪽 위 허공으로 뻗었다(카드 총구 장은 이제 적 쪽으로 돌린다)
            ["리뉴아|Attack1"] = new[] { "fx_renewa_attack_cast_1", "fx_renewa_attack_proj_1", "fx_renewa_attack_hit_1" },
            ["리뉴아|Attack2"] = new[] { "fx_renewa_attack_cast_3", "fx_renewa_attack_lazer_1", "fx_renewa_attack_hit_1", "fx_renewa_attack_hit_2" },
            ["티그_영웅|Attack2"] = new[] { "fx_tighero_attack_cast_1", "fx_tighero_attack_proj_1", "fx_tighero_attack_hit_3", "fx_tighero_attack_hit_4" },   // (10-17 사용자 지적 · 원작 스파인 이벤트 조사) 한 바퀴 돌고(cast_1) 검격을 날림(proj_1)
            // 에피카 — 평타 갈래에 연주(music_*) 장이 섞여 평타 · 강공이 같은 장을 다 틀었다: 평타는 음표 탄 · 맞음만, 강공은 연주 · 바닥 울림 + 큰 탄
            //   (2026-10-14 원작 영상) 평타는 시전자 둘레 보라 마법 원(music_aura_1, 0.3~0.6초) · 강공은 시전자 발밑 원반 울림(aura_ground) — 둘 다 시전자 쪽
            ["에피카|Attack1"] = new[] { "fx_epica_music_aura_2", "fx_epica_music_play_1", "fx_epica_attack_proj_1", "fx_epica_attack_hit_1" },   // (2026-10-14 원작 다시 고름) 몸을 감싸는 보라 거품 구체 + 음표 → 탬버린 → 붉은 섬광
            ["에피카|Attack2"] = new[] { "fx_epica_music_aura_4", "fx_epica_attack_proj_2", "fx_epica_attack_hit_2" },   // (10-17 원작 영상 정밀 분석 B) 불붙은 에피콘 탄을 에피카 손에서
            //   (2026-10-13) 에피카는 원작 스킬 장이 없다(라이브러리 attack 12장뿐 — 원본 번들에도 skill 프리팹 없음) — 스킬은 연주 둘째 판 · 음표 고리 · 큰 바닥 울림
            ["에피카|Skill1"] = new[] { "fx_epica_music_aura_ground_2", "fx_epica_music_play_2", "fx_epica_music_aura_4" },   // (10-17 원작 영상 정밀 분석 B) 자기 강화 — 탄 · 맞음 뺌
            // 이드 스킬 — 원작은 시전자를 감싸는 보라 구체 보호막(0.9~2.7초) + 발밑 소용돌이. 우리 스킬 갈래엔 시전 장뿐이라 보호막 장(fx_ed_shield_1 — 공용 실드로도 쓰는 이드 원작 장)을 시전자에
            ["이드|Attack2"] = new[] { "fx_ed_attackcast_3", "fx_ed_attack_2", "fx_ed_attackcast_2", "fx_ed_attackhit_2" },   // (10-17 사용자 지적 · 원작 스파인 이벤트 조사) 굵은 레이저(1067)
            ["티그_영웅|Skill1"] = new[] { "fx_tighero_skill_dash_1", "fx_tighero_skill_dash_2", "fx_tighero_skill_hit_1" },   // 적 뒤로 가며 베고(dash_1) 돌아오며 또 벰(dash_2)
            ["아야|Skill1"] = new[] { "fx_aya_skill1_cast_1", "fx_aya_skill1_proj_1", "fx_aya_skill1_hit_1", "fx_aya_skill1_change_1" },   // (10-17 사용자 「얼음 나비 왕복」) 장미잎 → 나비 탄(왕복, UltFx) → 맞음 → 적 자리에서 돌아섬. 얼음 길(skill_ice_1) 뺌
            // 죠안 — (10-17 사용자) 기본은 초록 사슬 · 초록 파동, 꿈결(DreamForm)은 붉은 사슬 · 붉은 파동(원작 몸짓마다 _DreamForm 판 · 장 색으로 확인)
            ["죠안|Attack1|DreamForm"] = new[] { "fx_joanne_attack_fire_3", "fx_joanne_attack1_2_ready_1", "fx_joanne_attack1_2_snap_1", "fx_joanne_attack_fire_4", "fx_joanne_attack_hit_1" },
            ["죠안|Attack2"] = new[] { "fx_joanne_attack2_1_cast_1", "fx_joanne_attack2_1_buff_1", "fx_joanne_attack2_1_buff_1_target" },   // (10-17 2차 사용자 지적) 장판(buff_1)은 죠안 밑(CARD_AT)
            ["죠안|Attack2|DreamForm"] = new[] { "fx_joanne_attack2_1_cast_2", "fx_joanne_attack2_1_buff_2", "fx_joanne_attack2_1_buff_1_target" },
            ["죠안|Skill1|DreamForm"] = new[] { "fx_joanne_skill_ready_1", "fx_joanne_skill_cast_1", "fx_joanne_skill_cast_4", "fx_joanne_skill_target_1" },
            ["이드|Skill1"] = new[] { "fx_ed_skillcast_1", "fx_ed_shield_1" },
            // 죠안 스킬 — 원작은 1.8~2.3초 시전자 둘레 주황 큰 폭발(cast_2 — 깃털 · 붉은 조각 · 고리). 준비 장 상한 2 에 밀려 빠졌다 → 준비 · 시전자 폭발 · 맞음
            // 죠안 평타 — (2026-10-14 원작 대조) 시전자 발밑 붉은 마법진 · 검은 손(attack1_2_snap · fire_4 — 다른 모습의 장)이 원작 평타에 없다: 준비 · 불꽃 · 탄 · 맞음만
            ["죠안|Attack1"] = new[] { "fx_joanne_attack_fire_1", "fx_joanne_attack1_1_ready_1", "fx_joanne_attack1_1_snap_1", "fx_joanne_attack_fire_2", "fx_joanne_attack_hit_1" },   // (2026-10-14 원작 다시 고름) 사슬 끝 불꽃 → 적을 감는 사슬 · 노란 빛 → 연두 고리 섬광 → 별
            ["죠안|Skill1"] = new[] { "fx_joanne_skill_ready_1", "fx_joanne_skill_cast_1", "fx_joanne_skill_cast_2", "fx_joanne_skill_target_1" },
            // 우이(기억) 스킬 — 강화 장(buff_1 · clover, 「buff」 낱말로 시전자에 붙음)이 맞는 적의 폭발 고리(explosion_bottom)와 같은 고리 · 기둥이라 시전자 · 적 양쪽에 떴다(2026-10-14) — 강화 장은 뺀다
            // 우이(기억) 강공 — 원작은 시전자 둘레 큰 연못(pond_1). 시전자 장 상한에 밀려 빠졌다(2026-10-14 원작 대조)
            ["우이_기억|Attack2"] = new[] { "fx_uimemory_attack2_cast_1_default", "fx_uimemory_attack2_pond_1_default", "fx_uimemory_attack2_eru_song_1_default", "fx_uimemory_attack2_eru_trail_1_default", "fx_uimemory_attack2_eru_end_1_default" },
            ["네르_빡침|Attack1"] = new[] { "fx_nerrage_attack_1", "fx_nerrage_attack_1_hit", "fx_nerrage_attack_1_2", "fx_nerrage_attack_1_hit_2" },   // (10-17 원작 영상 정밀 분석 orig_analysis) 원작 평타 2타 — 번호 판은 타격마다 차례로
            ["디아나_왕년|Attack1"] = new[] { "fx_dianayester_attack_slash_1", "fx_dianayester_attack_hit_1" },   // (2026-10-14 원작 다시 고름) 넓은 주황 베기 + 적 위 사선. 주먹 · 던지는 바위(proj_1) · 돌 튐 뺌
            ["란|Attack1"] = new[] { "fx_ran_attack_slash_1_default", "fx_ran_attack_hit_1_default" },   // (2026-10-14 원작 다시 고름) 가로 보라 대시 선 + X 별빛. slash_2(세로 금 = 강공) · cast_2 뺌
            ["리뉴아|Skill1"] = new[] { "fx_renewa_skill_cast_3", "fx_renewa_skill_impact_1", "fx_renewa_skill_lastcast_1" },   // (10-17 사용자 지적 · 원작 장 조사) impact_2 · 3 은 impact_1 을 단계별로 쪼갠 판 — 같이 틀어 고리 · 레이저가 겹쳐 흩어졌다   // (2026-10-14 원작 다시 고름) impact_1_1 · 1_2(복제) 뺌
            ["벨라|Attack1"] = new[] { "fx_vela_attack1_1", "fx_vela_attack1_hit" },   // (10-17 2차 사용자 지적) 1모션에 임팩트 1번 — attack1_2 뺌
            ["비비|Attack1"] = new[] { "fx_vivi_attack_slash_1", "fx_vivi_attack_slash_2", "fx_vivi_attack_hit_2" },   // (10-17 원작 영상 정밀 분석 orig_analysis) 원작 평타 2타
            ["비비_신성|Attack2"] = new[] { "fx_vividivine_attack_2_default", "fx_vividivine_attack_2_proj_default", "fx_vividivine_attack_2_hit_default" },   // (10-17 사용자 지적 · 원작 장 조사) 손에서 모아 쏘는 탄 — 이름 고르기가 모으기 판을 적 위에 띄웠다
            ["비비_신성|Skill1"] = new[] { "fx_vividivine_skill_1_default", "fx_vividivine_skill_1_proj_default", "fx_vividivine_skill_1_hit_1_default", "fx_vividivine_skill_1_hit_2_jolly_default" },   // 머리 위 큰 고리(skill_1_default) → 탄 → 큰 섬광 → 바닥 고리
            ["비비|Skill1"] = new[] { "fx_vivi_skill_cast_1" },   // 수은 모으기(cast_1 — 안으로 끌려드는 입자 · 몸 감는 물 고리) → 보호막(원작 전용 장 없음 — 공용 보호막)
            ["우이|Skill1"] = new[] { "fx_uieru_skill_ground_1", "fx_uieru_skill_waterhit_1" },   // (10-17 원작 영상 정밀 분석 B) 빛 비 + 적 맞음 — flowerdebuff 는 고학년 장
            ["비비_신성|Attack1"] = new[] { "fx_vividivine_attack_1_default", "fx_vividivine_attack_proj_default", "fx_vividivine_attack_1_hit_default" },   // (2026-10-14 원작 다시 고름) 손끝 초록 빛 → 작은 탄 → 작은 초록 터짐
            ["실비아|Attack1"] = new[] { "fx_silvia_attack_1_slash_1_default", "fx_silvia_attack_1_hit_1_default" },   // (10-17 사용자 지적 · 원작 장 조사) 1타 몸짓 — slash_2 를 같이 틀어 두 초승달이 부채꼴로 겹쳤다   // (2026-10-14 원작 다시 고름) 시전자 앞 크림색 초승달 두 번 + 작은 반짝
            ["아야|Attack1"] = new[] { "fx_aya_attack1_slash_1", "fx_aya_attack1_slash_2", "fx_aya_hit_1" },   // (10-17 사용자 지적 · 원작 장 조사) 2타(267 · 667) — 베기 두 판 차례로   // (2026-10-14 원작 다시 고름) 시전자 앞 하늘색 초승달 + 작은 얼음 반짝
            ["디아나_왕년|Skill1"] = new[] { "fx_dianayester_skill_cast_1", "fx_dianayester_skill_slash_1", "fx_dianayester_skill_ground_1", "fx_dianayester_skill_hit_1" },   // (2026-10-14 원작 다시 고름) 주황 초승달 · 노란 섬광 → 큰 폭발 → 오른쪽으로 갈라지는 불 땅
            ["요미|Attack1"] = new[] { "fx_yomi_attack_hit_1" },   // (10-17 사용자 지적 · 원작 장 조사) 번쩍이는 별(hit_1 의 ✦ 칸) — 달 탄(proj_1) 뺌
            ["요미|Skill1"] = new[] { "fx_yomi_skill_cast_1", "fx_yomi_skill_ground_1", "fx_yomi_skill_explosion_1" },   // (10-17 원작 영상 정밀 분석 B) 하늘 큰 초승달 = ground_1 안의 Moon 이미터 · 혜성(proj_1)은 원작에 안 보임
            ["요미|Attack2"] = new[] { "fx_yomi_attack2_proj_1", "fx_yomi_attack2_expolosion_1" },
            ["우로스|Attack1"] = new[] { "fx_uros_attack_slash_1", "fx_uros_attack_hit_1", "fx_uros_attack_slash_2", "fx_uros_attack_hit_2" },   // (10-17 원작 영상 정밀 분석 B) 원작 평타 2타
            ["우이_기억|Attack1"] = new[] { "fx_uimemory_attack1_cast_1_default", "fx_uimemory_attack1_cast_2_default", "fx_uimemory_attack1_slash_1_default", "fx_uimemory_attack1_proj_1_default", "fx_uimemory_attack1_explosion_1_default" },   // (2026-10-14 원작 다시 고름) 시전자 앞 연두 베기 + 파란 방울
            ["이드|Attack1"] = new[] { "fx_ed_attackcast_1", "fx_ed_attacksub_1", "fx_ed_attack_1_1", "fx_ed_attackhit_1" },   // (10-17 사용자 지적 · 원작 스파인 이벤트 조사) 나타가 볼(attacksub_1)을 4번 쏘고 얇은 레이저
            ["네르_빡침|Attack2"] = new[] { "fx_nerrage_attack_2_ready", "fx_nerrage_attack_2", "fx_nerrage_attack_2_hit" },   // (10-17 원작 영상 정밀 분석 orig_analysis) change(평타식 초승달 — 스핀처럼 보임) 뺌
            ["클로에|Attack1"] = new[] { "fx_chloe_attack_slash_1", "fx_chloe_attack_hit_1" },   // (2026-10-14 원작 다시 고름) 제자리 머리 위 큰 분홍 호 → 섬광. 땅 균열(강공) · 고학년 장 뺌
            ["우로스|Skill1"] = new[] { "fx_uros_skill_cast_1", "fx_uros_skill_ground_1" },   // (10-17 원작 영상 정밀 분석 B) 원작 영역은 ground_1(화면 너비)
            ["티그_영웅|Attack1"] = new[] { "fx_tighero_attack_slash_1", "fx_tighero_attack_hit_1", "fx_tighero_attack_slash_2", "fx_tighero_attack_hit_2" },   // (10-17 사용자 지적 · 원작 스파인 이벤트 조사) 2타(200 · 1333) — cast_1 은 강공 장
            ["우이_기억|Skill1"] = new[] { "fx_uimemory_skill1_cast_1_default", "fx_uimemory_skill1_cast_2_default", "fx_uimemory_skill1_explosion_bottom_1_default", "fx_uimemory_skill1_explosion_center_1_default", "fx_uimemory_skill1_buff_clover_default" },   // (10-17 원작 영상 정밀 분석 B) 머리 뒤 클로버
            // 우이 — (2026-10-14 원작 대조) 평타 · 강공 장이 뒤바뀌어 있었다: 원작 평타는 노란 구슬 → 초록 별 터짐(skill_waterhit), 강공은 개구리 혀 → 파란 물(waterhit_1).
            //   시전자 발밑 클로버(cast_1 · start_1)는 원작 평타에 없다
            ["우이|Attack1"] = new[] { "fx_uieru_cast_1", "fx_uieru_proj_1", "fx_uieru_skill_waterhit_1" },   // (2026-10-14 원작 다시 고름) 손 노랑 · 연두 빛 + 클로버 → 연두 별 섬광
            ["우이|Attack2"] = new[] { "fx_uieru_watercast_1", "fx_uieru_waterhit_1" },   // (10-17 원작 영상 정밀 분석 B) 혀가 직접 침 — 클로버 탄 뺌
            //   (2026-10-13) 스킬 폭발을 빌려 강공 = 스킬로 보였다 — 강공은 조준 둘 · 연사 맞음, 폭발은 스킬만
            //   (2026-10-14) 평타 갈래가 조준 · 눈빛 · 바닥까지 다 틀어 강공 ≈ 평타였다 — 평타는 한 발(불꽃 · 탄 · 맞음), 강공은 두 불꽃 · 조준 둘 · 맞음 둘 · 푸른 바닥 충격파
            ["시온더다크불릿|Attack1"] = new[] { "fx_xxionx_crosshair_1", "fx_xxionx_attack_fire_1", "fx_xxionx_attack_hit_1" },   // (10-15 사용자) 탄은 날지 않는다 — 조준선이 뜨고 사라지며 그 자리가 터진다(bullet 뺌)   // (2026-10-14 원작 다시 고름) 적에 푸른 조준선 → 총구 흰 섬광 → 푸른 번개 금
            ["시온더다크불릿|Attack2"] = new[] { "fx_xxionx_attack_hit_2" },
            ["시온더다크불릿|Skill1"] = new[] { "fx_xxionx_skill_cast_1", "fx_xxionx_crosshair_3", "fx_xxionx_attack_hit_1", "fx_xxionx_attack_fire_1" },   // (10-18 2차) 스킬 조준은 원작 X 자(crosshair_3 — Crosshair_Plus 45° 판), 평타 네모 조준(crosshair_1)을 빌리던 것   // (fix_Y 6-2) 탄마다 X 조준 → 총구 → 맞음(UltFx.REP 로 타격마다 되튼다) — eye 섬광 · 폭발 갈래 대신   // (10-17 원작 영상 정밀 분석 orig_analysis) 흰 구름 → 푸른 테두리 검은 연기 폭발만
        };

        // 카드 장 크기 배율 — 「사도|몸짓」. 강공이 평타 장을 빌리면 평타보다 크게(1.1~1.3)
        static readonly Dictionary<string, float> CARD_SCALE = new Dictionary<string, float>
        {
            ["비비|Attack2"] = 1.25f, ["에르핀_왕도|Attack2"] = 1.2f, ["란|Attack2"] = 1.15f, ["디아나_왕년|Attack2"] = 1.1f, ["클로에|Attack2"] = 1.15f,
            ["리뉴아|Attack2"] = 1.3f, ["티그_영웅|Attack2"] = 1.2f, ["에피카|Attack2"] = 1.15f, ["우이|Attack2"] = 1.15f, ["시온더다크불릿|Attack2"] = 1.15f,
            // 2026-10-13 — 에피카 스킬은 강공보다 크게 · 비비(신성) 강공은 평타보다 크게 · 우로스 스킬 장판(크기 58~80)이 수풀 위까지 덮던 것을 줄인다
            ["비비_신성|Attack2"] = 2.0f,   // 비비(신성) 강공 1.2 → 2.0(원작 대조)
        };
        // 카드 장 하나만의 크기 — 「사도|몸짓|장 이름」(2026-10-14 원작 대조: 화면을 덮어야 할 베기 · 보호막이 작았다)
        static readonly Dictionary<string, float> CARD_PART_SCALE = new Dictionary<string, float>
        {
            ["란|Attack2|fx_ran_skill_slash_3_default"] = 2.5f, ["우로스|Attack2|fx_uros_attack2_snake_jolly"] = 2.2f, ["클로에|Attack1|fx_chloe_attack_slash_1"] = 1.8f, ["클로에|Attack1|fx_chloe_attack_hit_1"] = 1.5f,   // 원작 머리 위 큰 분홍 원호 · 큰 터짐
            ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_shockwave_1"] = 2.2f, ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_hit_1"] = 1.8f, ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_shockwave_hit_1"] = 1.8f,   // 원작 — 화면 끝까지 쓰는 분홍 충격파 · 적마다 큰 분홍 터짐
            ["네르_빡침|Attack1|fx_nerrage_attack_1"] = 1.9f,   // 메시 칼빛(10-15) — 원작은 몸 1.5배 초승달
            ["란|Skill1|fx_ran_skill_ready_1_default"] = 1.8f,   // 늑대 머리 — 원작은 적 몸보다 크게
            ["이드|Skill1|fx_ed_shield_1"] = 1.15f,   // (10-18 사용자 「살짝만 크게」)
            ["비비|Skill1|fx_ed_shield_1"] = 1.2f,   // (10-18 원작 대조: 0.5 는 지름이 키의 0.45 로 머리만 덮었다) (3차) 원작 파티클은 이미 원작 크기 — 1.0 이면 고리가 달의 1.3배(원작 그대로) · 비비는 몸을 꼭 감싸는 지름 키 1.3
            ["디아나_왕년|Skill1|fx_dianayester_skill_slash_1"] = 1f, ["디아나_왕년|Skill1|fx_dianayester_skill_ground_1"] = 1.5f,
            ["시온더다크불릿|Attack2|fx_xxionx_skill_explosion_1"] = 1.4f, ["시온더다크불릿|Attack1|fx_xxionx_crosshair_1"] = 1.6f,   // 대상 큰 폭발
            ["리뉴아|Attack1|fx_renewa_attack_hit_1"] = 1.6f, ["실비아|Attack1|fx_silvia_attack_1_hit_1_default"] = 0.6f,
            ["아야|Attack1|fx_aya_hit_1"] = 0.7f, ["에피카|Attack1|fx_epica_music_aura_2"] = 1.8f, ["요미|Attack1|fx_yomi_attack_hit_1"] = 1.4f, ["우로스|Attack1|fx_uros_attack_slash_1"] = 1.6f, ["시온더다크불릿|Attack1|fx_xxionx_attack_fire_1"] = 1.5f,
            ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_shockwave_1"] = 2.2f, ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_hit_1"] = 1.8f, ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_shockwave_hit_1"] = 1.8f,   // (10-18) 강공 = 반대 손 판(Attack1_2) — 평타 크기와 같게
            ["디아나_왕년|Skill1|fx_dianayester_skill_cast_1"] = 0.5f,   // (10-18 검토_1) 기 모으는 붉은 구가 원작보다 컸다 — ORIG_SCALE_OK 에도
            ["우로스|Attack1|fx_uros_attack_slash_2"] = 1.6f, ["실비아|Attack1|fx_silvia_attack_1_hit_2_default"] = 0.6f,   // (10-18) 첫 휘두르기 · 맞음과 같게
            // (10-18 4차 사용자 결정 「던지는 불 에피콘은 곁 에피콘 크기로」) 원작대로 모드(ORIG_SCALE_OK)에서도. cap_epica · cap_fix4 seq 06_에피카_2강공 — 곁 에피콘(0초) ↔ 던진 탄(1.3초)
            //   분홍 넓이 422 ↔ 534 px(√ 0.89) · 둘레 상자 23×35 ↔ 33×34(√ 0.85) · 배 포함 0.80 → 0.85. 전 값 1/1.15(CARD_SCALE 빼기)는 원작대로 모드에서 안 쓰였다
            ["에피카|Attack2|fx_epica_attack_proj_2"] = 0.85f,
            ["티그_영웅|Skill1|fx_tighero_skill_dash_1"] = 0.5f,   // (10-18 4차 사용자 「대상보다 너무 멀리」) 초승달 5판이 원작 34 단위(우리 10.2)에 걸쳐 대상 너머 · 제자리 뒤까지 — 반으로(5.1). 가운데는 FxRun.PS_SHIFT
        };
        // 카드 장 자리 — 「사도|몸짓|장 이름」(2026-10-15). CARD_AT(장 이름 하나에 자리 하나)은 같은 장을 쓰는 다른 몸짓까지 같이 바꿔서,
        //   평타를 고치면 강공 · 스킬이 따라 바뀌었다 — 몸짓마다 따로 정할 것은 여기에(CARD_AT 보다 먼저)
        static readonly Dictionary<string, string> CARD_AT_BY = new Dictionary<string, string>
        {
            ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_cast_1"] = "caster",
            ["우로스|Attack2|fx_uros_attack2_snake_jolly"] = "caster",   // (10-17) 원작 뱀은 시전자 발밑 원점에서 적 자리(30~46 단위 앞)까지 짜였다 — target 이면 화면 밖으로 빠졌다   // 쓸고 가는 장(UltFx.SWEEP) — 전엔 시전자 뒤 바닥에 작게
            ["티그_영웅|Attack2|fx_tighero_attack_aura_1"] = "caster",   // 미리 장이 아니라 베기와 함께(원작은 끝 무렵 파란 기운)
            ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_shockwave_hit_1"] = "target",
            ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_shockwave_1"] = "target",
            ["에르핀_왕도|Attack1|fx_erpinroyale_attack1_hit_1"] = "target",
            ["우로스|Attack1|fx_uros_attack_slash_1"] = "caster",
            ["우로스|Attack1|fx_uros_attack_hit_1"] = "body",   // (10-18 검토_2) 세로 칼선은 적 몸 가운데 — target 이 발밑으로 내렸다
            // (10-18 이벤트 ID 표) 둘째 휘두르기 · 맞음 · 시전 섬광 · 적 쪽 폭발 — 이름 낱말로는 자리가 틀리던 것
            ["우로스|Attack1|fx_uros_attack_slash_2"] = "caster", ["우로스|Attack1|fx_uros_attack_hit_2"] = "body",
            ["실비아|Attack2|fx_silvia_attack_2_cleanup_1_default"] = "caster",
            ["벨라|Attack2|fx_vela_attack2_1"] = "target", ["벨라|Skill1|fx_vela_skill1_1"] = "target",
            // (10-18 검토_2) 우로스 세로 칼선은 적 몸 가운데 · 실비아 강공 끝 흰 연기는 실비아 발밑 · 란 스킬 흰 연기는 란이 뛰어든 적 발밑
            ["실비아|Attack2|fx_silvia_attack_2_end_default"] = "caster", ["란|Skill1|fx_ran_skill_cast_1_default"] = "target",
            // (10-18 원작 대조 검토_1) 아야 얼음 가시는 아야 발밑에서 적 쪽으로 · 에르핀 스킬 소용돌이는 에르핀 발밑 · 죠안 스킬 몸 앞 불꽃
            ["아야|Attack2|fx_aya_attack2_ice_1"] = "caster", ["에르핀_왕도|Skill1|fx_erpinroyale_skill1_aura_1"] = "caster", ["죠안|Skill1|fx_joanne_skill_cast_1"] = "muzzle",
            ["아야|Attack2|fx_aya_attack2_wind_up_1"] = "caster",   // 이름 낱말이 없어 적에게 감 — 원작은 칼 젖히는 아야 쪽(중간)
            ["아야|Attack2|fx_aya_attack2_slash_1"] = "caster", ["아야|Attack2|fx_aya_attack2_slash_2"] = "caster",   // 원작 초승달은 아야 머리 위(중간)
            ["클로에|Attack2|fx_chloe_attack_slash_2"] = "caster",   // 원작 내리찍기 원호는 클로에 머리 위에서 바닥을 따라 적까지(중간) — 이름 slash 라 적 몸에 감
            ["디아나_왕년|Attack2|fx_dianayester_attack_punch_1"] = "body",    // 원작 앞 적 몸에 짧은 초승달(중간) — 이름 낱말이 없어 적 발밑
            ["디아나_왕년|Attack2|fx_dianayester_attack_hit_3"] = "target",    // 바닥에 누운 갈라짐 판 — hit 낱말로 몸 가운데에 뜸(중간)
            ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_cast_2"] = "caster",
            ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_shockwave_hit_1"] = "target", ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_shockwave_1"] = "target", ["에르핀_왕도|Attack2|fx_erpinroyale_attack1_hit_1"] = "target",
            ["에르핀_왕도|Skill1|fx_erpinroyale_skill1_loop_1"] = "caster", ["에르핀_왕도|Skill1|fx_erpinroyale_skill1_loop_2"] = "caster",   // 낱말이 없어 적에게 감(중간)
            ["죠안|Attack1|fx_joanne_attack_fire_3"] = "muzzle", ["죠안|Attack1|fx_joanne_attack_fire_4"] = "muzzle",
            ["죠안|Attack1|fx_joanne_attack1_2_ready_1"] = "body", ["죠안|Attack1|fx_joanne_attack1_2_snap_1"] = "body",   // 적 몸을 감싸고 움켜쥠(보통 판과 같게, 중간)
            ["네르_빡침|Attack1|fx_nerrage_attack_1_change"] = "caster", ["네르_빡침|Attack1|fx_nerrage_attack_2_change"] = "caster", ["네르_빡침|Attack1|fx_nerrage_attack_3_change"] = "caster",   // 보통 판 초승달(attack_1)과 같게 네르 둘레(낮음 — 변신판 영상 대조 없음)
            ["네르_빡침|Skill1|fx_nerrage_skill_1"] = "caster",   // 깃발 빛기둥은 네르 앞 — 낱말이 없어 대상에 감(CARD_SELF 라 지금도 시전자 쪽이면 필요 없음)
            ["우이|Skill1|fx_uieru_skill_waterhit_1"] = "target",   // (10-18 사용자 「비 임팩트 없음」) 빛 비는 바닥 원점(웅덩이 · 빛기둥 y 13) — 몸 가운데면 위로 빠졌다
            // (10-18 사용자 「레이저는 나타에서」) 이드 레이저 원점 — 나타 앞 끝(WEAPON Nattah_*)에서 적 몸 쪽으로. 위로 서던 것은 Z_FWD 로 고쳐졌다
            ["이드|Attack1|fx_ed_attack_1_1"] = "muzzle", ["이드|Attack1|fx_ed_attack_1_2"] = "muzzle", ["이드|Attack1|fx_ed_attack_1_3"] = "muzzle", ["이드|Attack1|fx_ed_attack_1_4"] = "muzzle",
            ["이드|Attack2|fx_ed_attack_2"] = "muzzle",
            ["우이|Attack1|fx_uieru_cast_1"] = "muzzle",   // (10-18 검토_2) 원작 손(클로버) 앞 빛 덩이 — 발밑에 떴다
            ["우이|Attack1|fx_uieru_skill_waterhit_1"] = "target",
            ["우이_기억|Attack1|fx_uimemory_attack1_cast_1_default"] = "caster",
            ["우이_기억|Attack1|fx_uimemory_attack1_cast_2_default"] = "muzzle",   // (fix_Y 9) 손의 큰 4갈래 별 — 손 본이 없으면 발밑(불확실)
            ["우이_기억|Attack1|fx_uimemory_attack1_slash_1_default"] = "caster",
            ["우이_기억|Attack1|fx_uimemory_attack1_explosion_1_default"] = "target",
            ["이드|Attack1|fx_ed_attackcast_1"] = "caster",
            ["이드|Attack1|fx_ed_attackcast_3"] = "caster",
            ["이드|Attack1|fx_ed_attackhit_1"] = "body",   // (재검토) 이미터가 원점(몸 가운데 기준) — 발밑이면 반이 땅 아래
            ["죠안|Attack1|fx_joanne_attack_fire_1"] = "muzzle",
            ["죠안|Attack1|fx_joanne_attack_fire_2"] = "muzzle",   // (10-18 원작 대조) 앞 주황 고리는 몸 높이 — 발밑에 낮게 섰다
            ["죠안|Attack1|fx_joanne_attack1_1_ready_1"] = "body", ["죠안|Attack1|fx_joanne_attack1_1_snap_1"] = "body", ["죠안|Attack1|fx_joanne_attack_hit_1"] = "body",   // (10-17 정밀 분석) 적 몸을 감싸고 움켜쥠
            ["죠안|Skill1|fx_joanne_skill_target_1"] = "caster", ["티그_영웅|Attack2|fx_tighero_attack_cast_1"] = "caster",
            ["클로에|Attack1|fx_chloe_attack_slash_1"] = "caster",
            ["클로에|Attack1|fx_chloe_attack_hit_1"] = "target",
            ["티그_영웅|Attack1|fx_tighero_attack_cast_1"] = "caster",
            ["티그_영웅|Attack1|fx_tighero_attack_slash_1"] = "caster",
            ["티그_영웅|Attack1|fx_tighero_attack_hit_1"] = "body", ["티그_영웅|Attack1|fx_tighero_attack_hit_2"] = "body",   // (10-18 4차 사용자 「임팩트가 무기보다 아래」) 원작 맞음 별은 적 가슴 — 발밑 원점이라 반이 땅 아래
            // (10-18 이벤트 ID 시범 원작 대조) 둘째 휘두르기 원호도 티그 둘레(원작 1.1초) — 적 쪽에 그려졌다.
            //   스킬 갈 때 초승달 X · 돌아올 때 물결은 적을 감싸며 지나간다(원작 0.6~1.0 · 1.6~1.9초) — 시전자(제자리 왼쪽) 바닥에 그려졌다
            ["티그_영웅|Attack1|fx_tighero_attack_slash_2"] = "caster",
            ["티그_영웅|Skill1|fx_tighero_skill_dash_1"] = "target", ["티그_영웅|Skill1|fx_tighero_skill_dash_2"] = "caster",   // (10-18 2차 사용자 「거리만큼만 베야 하는데 뒤로 길다」) 원점 = 2000ms 의 티그 자리(적 뒤, CardBehindCo) — 적 발밑이라 적 뒤 거리만큼 제자리 뒤로 더 뻗었다
            ["네르_빡침|Attack1|fx_nerrage_attack_1"] = "caster",
            ["디아나_왕년|Attack1|fx_dianayester_attack_slash_1"] = "caster",
            ["란|Attack1|fx_ran_attack_slash_1_default"] = "caster", ["란|Attack2|fx_ran_attack_slash_2_default"] = "caster",   // (재검토) 장이 원점에서 +x(적 쪽)로 짜였다(이미터 중심 +11) — 적에 두면 반달 볼록면이 란 쪽을 봤다   // (3차) 반달은 적 쪽 — 시전자 쪽이었다
           
            ["네르_빡침|Attack1|fx_nerrage_attack_1_hit"] = "body", ["네르_빡침|Attack1|fx_nerrage_attack_1_hit_2"] = "body", ["네르_빡침|Attack1|fx_nerrage_attack_1_2"] = "caster",   // (10-17 정밀 분석) 사선은 적 몸 — 네르 몸을 지나가던 것
            ["네르_빡침|Attack2|fx_nerrage_attack_2_ready"] = "caster", ["네르_빡침|Attack2|fx_nerrage_attack_2"] = "target", ["네르_빡침|Attack2|fx_nerrage_attack_2_hit"] = "target",
            ["비비_신성|Attack2|fx_vividivine_attack_2_hit_default"] = "target", ["비비_신성|Skill1|fx_vividivine_skill_1_hit_2_jolly_default"] = "target", ["비비_신성|Skill1|fx_vividivine_skill_1_hit_1_default"] = "target",   // (3차) 바닥형 맞음 — 적 발밑(이름 hit 라 몸 가운데에 떴다)
            ["아야|Attack1|fx_aya_attack1_slash_2"] = "caster", ["리뉴아|Attack2|fx_renewa_attack_lazer_1"] = "muzzle",
            ["란|Attack2|fx_ran_skill_hit_1_default"] = "target", ["란|Skill1|fx_ran_skill_hit_1_default"] = "target", ["란|Skill1|fx_ran_skill_hit_2_default"] = "target",   // (10-17 사용자) 연기 임팩트는 맞는 적 발밑 — 맞음 장 규칙으로 몸 가운데에 떴다
            ["네르_빡침|Skill1|fx_nerrage_skill_1_end"] = "caster", ["네르_빡침|Skill1|fx_nerrage_skill_end"] = "caster", ["네르_빡침|Skill1|fx_nerrage_skill_ground"] = "caster", ["네르_빡침|Skill1|fx_nerrage_skill_hit"] = "caster",   // (재검토) 깃발 · 빛기둥은 네르 앞(아군 쪽) — 적 몸 위에 꽂혔다
            ["란|Skill1|fx_ran_skill_slash_3_default"] = "target",   // (10-17) 베기 메시가 x −5 ~ −11(시전자 쪽)에 짜였다 — 원점이 시전자면 란 뒤에서 벴다. 원점은 적
            ["란|Skill1|fx_ran_skill_ready_1_default"] = "body",   // 늑대 머리 표식(Wolf_2, 3초) — 원작은 타격 뒤 맞은 적 위에 남는다(2026-10-15). 시전자 미리 장으로 잡혀 사도 발밑에 떴다
            ["벨라|Attack1|fx_vela_attack1_2"] = "caster",
            ["비비_신성|Attack1|fx_vividivine_attack_1_default"] = "caster",
            ["시온더다크불릿|Attack1|fx_xxionx_crosshair_1"] = "body",   // 원작 조준선은 몸 높이에 크게
            ["시온더다크불릿|Attack1|fx_xxionx_attack_fire_1"] = "caster",   // (fix_Y 6-1) 원작은 총구 섬광 없이 발밑 반동 연기 — 전 muzzle
            ["실비아|Attack1|fx_silvia_attack_1_slash_1_default"] = "caster",
            ["실비아|Attack1|fx_silvia_attack_1_slash_2_default"] = "caster",
            ["아야|Attack1|fx_aya_attack1_slash_1"] = "caster",
            ["시온더다크불릿|Attack2|fx_xxionx_crosshair_1"] = "target",
            ["시온더다크불릿|Skill1|fx_xxionx_crosshair_3"] = "body", ["시온더다크불릿|Skill1|fx_xxionx_attack_fire_1"] = "muzzle",   // (10-18 2차 사용자 「탄은 총구에서」) 총구 섬광 · 연기는 총열 끝(WEAPON ^Gun_barrel) — 발밑 원점 앞에 떠 몸통에서 쏘는 듯했다   // (fix_Y 6-2) X 조준은 적 몸
            // 디아나(왕년) 스킬 — 큰 노란 폭발은 앞 적 몸 높이(원작). 시전자 발밑에 붙어 몸이 뛰어오를 때 끌려가 땅 아래로 처졌다(2026-10-15)
            ["디아나_왕년|Skill1|fx_dianayester_skill_slash_1"] = "body",
            ["시온더다크불릿|Attack2|fx_xxionx_skill_explosion_1"] = "body",   // (10-18 2차) 강공 검은 연기 구름 폭발은 적 몸 높이(원작 장 원점 0,0)
        };
        // ── 원작 스파인 이벤트 ID → 장(2026-10-18 티그(영웅) 시범) ── 원작은 몸짓 이벤트(Event_Front · Middle · Back 의 숫자 ID)마다 정해진 장을 튼다.
        //   ID → 장 짝은 원작 영상 시각과 맞춰 사도별로 정했다(orig_analysis · spine_events). 시각 · 횟수는 원작 이벤트 그대로, 맞는 소리 · 숫자는 엔진 박자.
        //   표에 없는 ID(시작 1000003 · 이동 · 피해 표시 …)는 건너뛴다. 이 표가 있는 사도는 CARD_NAMES · CARD_TIME 대신 이것(이벤트가 없는 몸짓만 예전 표)
        static readonly Dictionary<string, Dictionary<int, string[]>> EVENT_FX = new Dictionary<string, Dictionary<int, string[]>>
        {
            ["티그_영웅"] = new Dictionary<int, string[]>   // 전열(Event_Front)
            {
                // 평타 Attack1_1 — 200 휘두르기 1 · 1333 휘두르기 2(영상 0.4 · 1.1 원호, 0.5 · 1.25 터짐)
                [1011201] = new[] { "fx_tighero_attack_slash_1" }, [1011203] = new[] { "fx_tighero_attack_hit_1" },
                [1011202] = new[] { "fx_tighero_attack_slash_2" }, [1011204] = new[] { "fx_tighero_attack_hit_2" },
                // 강공 Attack2_1 · 2_2 — 100 돌며 몸 기운(cast_1) · 1600 검기(2_1 proj_1 · 2_2 proj_2) + 맞음(hit_3 · 4 — 검기가 닿을 때)
                [1011231] = new[] { "fx_tighero_attack_cast_1" },
                [1011228] = new[] { "fx_tighero_attack_proj_1" }, [1011230] = new[] { "fx_tighero_attack_proj_2" },
                [1011229] = new[] { "fx_tighero_attack_hit_3", "fx_tighero_attack_hit_4" },
                // 스킬 Skill1_1 — 433 갈 때 초승달(dash_1) · 500 · 1933 맞음(hit_1) · 2000 돌아올 때 돌진(dash_2). 43 · 44 는 피해 표시라 장 없음
                [1011237] = new[] { "fx_tighero_skill_dash_1" }, [1011238] = new[] { "fx_tighero_skill_dash_2" },
                [1011235] = new[] { "fx_tighero_skill_hit_1" },
            },
            // ── 10-18 원작 영상 ↔ 이벤트 시각 대조(event_fx/제안_A.md) — 죠안만 열 칸(Event_Middle), 나머지는 한 칸 Event ──
            ["아야"] = new Dictionary<int, string[]>   // 중열 — 이벤트 이름 Event(열 구분 없음, 0-1)
            {
                // 평타 Attack1_1 — 267 · 667 초승달 두 번(영상 0.3 · 0.6)
                [1005701] = new[] { "fx_aya_attack1_slash_1", "fx_aya_hit_1" }, [1005702] = new[] { "fx_aya_attack1_slash_2", "fx_aya_hit_1" },
                // 강공 Attack2_1 — 333 칼 젖힘 · 1233 베기 1 + 얼음 가시 · 1833 베기 2 + 얼음 폭발 · 1867 맞음. 09 · 10 은 피해 표시로 보여 장 없음
                [1005706] = new[] { "fx_aya_attack2_wind_up_1" },
                [1005703] = new[] { "fx_aya_attack2_slash_1" }, [1005705] = new[] { "fx_aya_attack2_ice_1" },     // ice ↔ stomp 차례는 낮음
                [1005704] = new[] { "fx_aya_attack2_slash_2" }, [1005707] = new[] { "fx_aya_attack2_stomp_1" },
                [1005708] = new[] { "fx_aya_hit_1" },
                // 스킬 1005712 · 1005711 은 넣지 않는다 — 나비 왕복(예전 표 · CARD_HITS 1900 · 2900) 유지
            },
            ["클로에"] = new Dictionary<int, string[]>   // 전열 — 이벤트 이름 Event(0-1)
            {
                // 평타 Attack1_1 — 733 머리 위 큰 원호 + 터짐(영상 0.8)
                [1005650] = new[] { "fx_chloe_attack_slash_1" }, [1005651] = new[] { "fx_chloe_attack_hit_1" },
                // 강공 Attack2_1 — 1433 내리찍는 큰 원호 · 땅 균열 · 큰 폭발(영상 3.5~4.1)
                [1005656] = new[] { "fx_chloe_attack_slash_2" }, [1005657] = new[] { "fx_chloe_skill_ground_1" }, [1005658] = new[] { "fx_chloe_skill_hit_1" },
                // 스킬 Skill1_1 — 1333 세바스티안 타고 변신 폭발 · 분홍 오라(영상 1.3). 0ms 1005659 는 모름(변신 시작 표시)
                [1005661] = new[] { "fx_chloe_skill_start_1", "fx_chloe_skill_aura_1" },
                // 탄 모습 평타 Attack1_1_SebastianForm(10-18 메리 고 라운드 탑승) — 367 · 700 · 1667 베기 · 땅 + 맞음(1005654 는 세 때 다). 71 · 73 은 모름
                [1005672] = new[] { "fx_chloe_skill_slash_1" }, [1005674] = new[] { "fx_chloe_skill_slash_2" }, [1005675] = new[] { "fx_chloe_skill_ground_1" }, [1005654] = new[] { "fx_chloe_skill_hit_1" },
            },
            ["디아나_왕년"] = new Dictionary<int, string[]>   // 전열 — 이벤트 이름 Event(0-1)
            {
                // 평타 Attack1_1 — 733 넓은 초승달 · 767 맞음(영상 0.8). 변신판 Attack1_1_Change(1010304~07)는 모름 — 예전 표
                [1010302] = new[] { "fx_dianayester_attack_slash_1" }, [1010301] = new[] { "fx_dianayester_attack_hit_1" },
                // 강공 Attack2_1(_Change 같은 ID) — 267 주먹 · 1700 바위 찍기 폭발 + 바닥 갈라짐. 0ms 1010313 모름 · 바위 불꼬리(proj_1)는 짝 이벤트 없음
                [1010310] = new[] { "fx_dianayester_attack_punch_1" },
                [1010309] = new[] { "fx_dianayester_attack_hit_3" }, [1010312] = new[] { "fx_dianayester_attack_hit_3" },   // 1010312 = 변신판(낮음)
                // 스킬 Skill1_1(_Change 같은 ID) — 667 기 모음 · 1467 내려찍기 폭발 · 맞음 · 1500 갈라지는 불 땅
                [1010316] = new[] { "fx_dianayester_skill_cast_1" },
                [1010315] = new[] { "fx_dianayester_skill_slash_1" }, [1010317] = new[] { "fx_dianayester_skill_hit_1" },
                [1010318] = new[] { "fx_dianayester_skill_ground_1" },
            },
            ["이드"] = new Dictionary<int, string[]>   // 전열 — 이벤트 이름 Event(0-1)
            {
                // 평타 Attack1_1~4 — 0 나타 시전 · 367 레이저(판마다 1_1~1_4) · 367 · 700 · 1033 · 1367 볼 + 맞음 네 번
                [1006808] = new[] { "fx_ed_attackcast_1" },
                [1006801] = new[] { "fx_ed_attack_1_1" }, [1006802] = new[] { "fx_ed_attack_1_2" }, [1006803] = new[] { "fx_ed_attack_1_3" }, [1006804] = new[] { "fx_ed_attack_1_4" },
                [1006805] = new[] { "fx_ed_attacksub_1" },
                [1006806] = new[] { "fx_ed_attackhit_1" }, [1006807] = new[] { "fx_ed_attackhit_1" },   // 07 = 마지막 큰 맞음(hit_3 일 수도 — 낮음)
                // 강공 Attack2_1 — 300 기 모음 · 1067 굵은 레이저 + 맞음 + 발밑 물결
                [1006812] = new[] { "fx_ed_attackcast_3" },
                [1006809] = new[] { "fx_ed_attack_2" }, [1006810] = new[] { "fx_ed_attackhit_2" }, [1006811] = new[] { "fx_ed_attackcast_2" },
                // 스킬 Skill1_1 — 933 땅 소용돌이 · 보호막. 16 · 18 · 33 모름
                [1006814] = new[] { "fx_ed_skillcast_1" }, [1006815] = new[] { "fx_ed_shield_1" },
            },
            ["에피카"] = new Dictionary<int, string[]>   // 중열 — 이벤트 이름 Event(0-1). 평타 · 강공은 탄이 에피콘 이벤트라 예전 표
            {
                // (보류 — 참고) 평타 1006401 music_aura_2 · 1006402 music_play_1 / 강공 1006403 music_aura_4 · 1006404 모름
                // 스킬 Skill1_1 — 533 바닥 고리 · 533 · 867 · 1200 음표 · 1567 음표 고리 폭발
                [1006407] = new[] { "fx_epica_music_aura_ground_2" },
                [1006408] = new[] { "fx_epica_music_play_2" },
                [1006409] = new[] { "fx_epica_music_aura_4" },
            },
            ["에르핀_왕도"] = new Dictionary<int, string[]>   // 중열 — 이벤트 이름 Event(0-1)
            {
                // 평타 Attack1_1 · 강공(Attack1_2 판 — 0-3 CARD_CHAIN) — 200 충전 · 567 주먹 빛 + 보석 탄 → 맞음 · 부채꼴(+330)
                [1012304] = new[] { "fx_erpinroyale_attack1_cast_1" }, [1012305] = new[] { "fx_erpinroyale_attack1_slash_1" },
                [1012306] = new[] { "fx_erpinroyale_attack1_cast_2" }, [1012307] = new[] { "fx_erpinroyale_attack1_slash_2" },
                [1012301] = new[] { "fx_erpinroyale_attack1_proj_1", "fx_erpinroyale_attack1_hit_1", "fx_erpinroyale_attack1_shockwave_hit_1", "fx_erpinroyale_attack1_shockwave_1" },
                // 스킬 Skill1_1 → Loop → End — 333 시전 · 타격마다 맞음(12) · 휘두르기 번갈아(11 · 15) · 첫 타 뒤 발밑 소용돌이(18) · 끝 폭발(16)
                [1012314] = new[] { "fx_erpinroyale_skill1_cast_1" },
                [1012312] = new[] { "fx_erpinroyale_skill1_hit_1" },
                [1012311] = new[] { "fx_erpinroyale_skill1_loop_1" }, [1012315] = new[] { "fx_erpinroyale_skill1_loop_2" },
                [1012318] = new[] { "fx_erpinroyale_skill1_aura_1" },
                [1012316] = new[] { "fx_erpinroyale_skill1_end_1" },
            },
            ["죠안"] = new Dictionary<int, string[]>   // 중열(Event_Middle)
            {
                // 평타 Attack1_1 — 133 사슬 끝 불꽃 · 적 감는 사슬 · 1267 죠안 앞 고리 · 1300 조이며 터짐. 200 1008528 모름(탄 후보 — 손 표에서 뺀 탄)
                [1008526] = new[] { "fx_joanne_attack_fire_1" }, [1008527] = new[] { "fx_joanne_attack1_1_ready_1" },
                [1008579] = new[] { "fx_joanne_attack_fire_2" },
                [1008525] = new[] { "fx_joanne_attack1_1_snap_1", "fx_joanne_attack_hit_1" },
                // 꿈결 평타 Attack1_1_DreamForm — 같은 차례의 붉은 판. 200 1008532 모름
                [1008530] = new[] { "fx_joanne_attack_fire_3" }, [1008531] = new[] { "fx_joanne_attack1_2_ready_1" },
                [1008583] = new[] { "fx_joanne_attack_fire_4" },
                [1008529] = new[] { "fx_joanne_attack1_2_snap_1", "fx_joanne_attack_hit_1" },
                // 강공 Attack2_1 — 300 기도 + 장판(사용자 3차 「장판은 시작에」) · 1933 적 쪽 원
                [1008534] = new[] { "fx_joanne_attack2_1_cast_1", "fx_joanne_attack2_1_buff_1" },
                [1008535] = new[] { "fx_joanne_attack2_1_buff_1_target" },
                //   이벤트대로면: [1008534] = cast_1 만 · [1008533] = new[] { "fx_joanne_attack2_1_buff_1" }(1933)
                // 꿈결 강공 — 300 붉은 기도 + 붉은 장판 · 1933 적 쪽 원. 38 · 39 모름
                [1008540] = new[] { "fx_joanne_attack2_1_cast_2", "fx_joanne_attack2_1_buff_2" },
                [1008537] = new[] { "fx_joanne_attack2_1_buff_1_target" },
                // 스킬 Skill1_1(_DreamForm) — 933 사슬 고리 · 1633 몸 앞 불꽃 · 2400 발밑 폭발(보통 cast_2 · 꿈결 cast_4) · 2433 아군 보호
                [1008544] = new[] { "fx_joanne_skill_ready_1" }, [1008542] = new[] { "fx_joanne_skill_cast_1" },
                [1008543] = new[] { "fx_joanne_skill_cast_2" }, [1008589] = new[] { "fx_joanne_skill_cast_4" },
                [1008541] = new[] { "fx_joanne_skill_target_1" },
            },
            ["네르_빡침"] = new Dictionary<int, string[]>   // 후열 — 이벤트 이름 Event(0-1)
            {
                // 평타 Attack1_1 — 567 초승달 1 · 700 사선 · 1900 초승달 2 · 2033 사선 2
                [1012403] = new[] { "fx_nerrage_attack_1" }, [1012401] = new[] { "fx_nerrage_attack_1_hit" },
                [1012404] = new[] { "fx_nerrage_attack_1_2" }, [1012402] = new[] { "fx_nerrage_attack_1_hit_2" },
                // 변신 평타 Attack1_1_Change — 0 · 1133 · 2233 세 번 베기. 167 · 1300 · 2400 맞음(05~07)은 모름
                // 변신 평타 맞음 — 원작 영상 1.7 「/」 · 2.5 「\」 · 3.2 「\」(10-18 사용자 「사선 없음」: 빡침 중엔 이 판이라 사선이 안 나왔다)
                [1012405] = new[] { "fx_nerrage_attack_1_hit" }, [1012406] = new[] { "fx_nerrage_attack_1_hit_2" }, [1012407] = new[] { "fx_nerrage_attack_1_hit_2" },
                [1012408] = new[] { "fx_nerrage_attack_1_change" }, [1012409] = new[] { "fx_nerrage_attack_2_change" }, [1012410] = new[] { "fx_nerrage_attack_3_change" },
                // 강공 Attack2_1(_Change 같은 ID) — 0 발밑 파편 · 900 도끼 꽂힘 + 빛기둥
                [1012419] = new[] { "fx_nerrage_attack_2_ready" },
                [1012416] = new[] { "fx_nerrage_attack_2", "fx_nerrage_attack_2_hit" },
                // 스킬 Skill1_1(_Change 같은 ID) — 0 깃발 돌리기 · 1100 깃발 꽂음 · 2133(변신 2000) 네르 강화
                [1012429] = new[] { "fx_nerrage_skill_charge" },
                [1012428] = new[] { "fx_nerrage_skill_1" }, [1012426] = new[] { "fx_nerrage_skill_ground" }, [1012427] = new[] { "fx_nerrage_skill_hit" },
                [1012431] = new[] { "fx_nerrage_skill_buff" },
            },
            ["란"] = new Dictionary<int, string[]>   // 후열 — 이벤트 이름 Event(0-1). 스킬은 예전 표(CARD_TIME 중간 베기)
            {
                // 평타 Attack1_1 — 633 가로 베기 줄 + X 별빛(167 은 시전 표시라 장 없음)
                [1009101] = new[] { "fx_ran_attack_slash_1_default" }, [1009102] = new[] { "fx_ran_attack_hit_1_default" },
                // 강공 Attack2_1 — 833 거대한 반달 + 맞음(1000 1009107 모름)
                [1009105] = new[] { "fx_ran_attack_slash_2_default" }, [1009106] = new[] { "fx_ran_attack_hit_1_default" },
                // (보류 — 참고) 스킬 1009113 skill_cast_1 · 2000 묶음(1009109 · 10 · 12) skill_slash_3 · skill_hit_1
            },
            // ── 아래는 열 칸 없이 한 칸 Event 인 사도(BattleDirector.OrigFx 가 Event 로 읽는다) — 10-18 원작 영상 ↔ 이벤트 시각 대조(event_fx/제안_B.md) ──
            ["리뉴아"] = new Dictionary<int, string[]>
            {
                // 평타 — 567 손 앞 시공간 고리(cast_1)가 열리고 · 1300 그 고리에서 초록 수정 탄 + 맞음(10-18 사용자 「손에 시공간 열어 초록 탄 발사」 — 탄을 567 에 같이 쏴 0.7초부터 손에 붙어 있었다. 원작 영상 1.1초 = 1300)
                [1007502] = new[] { "fx_renewa_attack_cast_1" }, [1007501] = new[] { "fx_renewa_attack_proj_1", "fx_renewa_attack_hit_1" },
                // 강공 — 1367 레이저 + 맞음 둘(cast_3 는 이벤트 · 영상에 없어 빠짐)
                [1007506] = new[] { "fx_renewa_attack_lazer_1" }, [1007507] = new[] { "fx_renewa_attack_hit_1", "fx_renewa_attack_hit_2" },
                // 스킬 — 867 문양 고리 · 1100 레이저 4줄(장 안 지연) · 3600 마지막 폭발
                [1007515] = new[] { "fx_renewa_skill_cast_3" }, [1007513] = new[] { "fx_renewa_skill_impact_1" }, [1007514] = new[] { "fx_renewa_skill_lastcast_1" },
            },
            ["실비아"] = new Dictionary<int, string[]>
            {
                // 평타 — 733 가로 초승달 + 맞음 · 1333 내려 베는 초승달 + 맞음(원작 2타 — 10-17 에 뺐던 slash_2 를 「원작과 동일」로 되살림)
                [1013001] = new[] { "fx_silvia_attack_1_slash_1_default", "fx_silvia_attack_1_hit_1_default" },
                [1013002] = new[] { "fx_silvia_attack_1_slash_2_default", "fx_silvia_attack_1_hit_2_default" },
                // 강공 — 767 시전 섬광 · 900 분홍 별 맞음 · 1633 노란 반짝 · 연기. 스킬(다과회 7.6초)은 짝이 불확실해 예전 표
                [1013006] = new[] { "fx_silvia_attack_2_cleanup_1_default" }, [1013007] = new[] { "fx_silvia_attack_2_hit_1_default" }, [1013008] = new[] { "fx_silvia_attack_2_end_default" },
            },
            ["우이"] = new Dictionary<int, string[]>
            {
                // 평타 — 500 손 빛 + 클로버 탄 + 연두 별 맞음(탄과 같은 묶음이라 맞음은 닿을 때)
                [1005801] = new[] { "fx_uieru_cast_1", "fx_uieru_proj_1" }, [1005802] = new[] { "fx_uieru_skill_waterhit_1" },
                // 강공 — 400 입 벌림 · 600 혀 끝 물보라 · 1700 물 장막(원작은 아군 전부, 우리는 시전자)
                [1005804] = new[] { "fx_uieru_watercast_1" }, [1005805] = new[] { "fx_uieru_waterhit_1" }, [1005806] = new[] { "fx_uieru_attack_buff_default" },
                // 스킬 — 733 빛 비 장판 + 적 맞음
                [1005810] = new[] { "fx_uieru_skill_ground_1" }, [1005811] = new[] { "fx_uieru_skill_waterhit_1" },
            },
            ["우로스"] = new Dictionary<int, string[]>
            {
                // 평타 — 700 휘두르기 1 · 767 맞음 1 · 1667 휘두르기 2 · 1733 맞음 2
                [1011103] = new[] { "fx_uros_attack_slash_1" }, [1011101] = new[] { "fx_uros_attack_hit_1" },
                [1011104] = new[] { "fx_uros_attack_slash_2" }, [1011102] = new[] { "fx_uros_attack_hit_2" },
                // 강공 — 1133 금빛 바람 · 1767 뱀 · 1900 맞음(뱀 · 맞음 차례는 중간)
                [1011109] = new[] { "fx_uros_attack2_cast_jolly" }, [1011108] = new[] { "fx_uros_attack2_snake_jolly" }, [1011107] = new[] { "fx_uros_attack2_hit_jolly" },
                // 스킬 — 1033 불꽃 섬광 + 바닥 영역
                [1011114] = new[] { "fx_uros_skill_cast_1", "fx_uros_skill_ground_1" },
            },
            ["벨라"] = new Dictionary<int, string[]>
            {
                // 평타 — 700 불꽃 베기(장 안에 둘째 베기까지) + 맞음. 1400(1009803)은 짝을 몰라 비움
                [1009801] = new[] { "fx_vela_attack1_1", "fx_vela_attack1_hit" },
                // 강공 2_1 → 2_2(3333 뒤) — 0 시전 · 2100 땅 폭발 + 맞음 · 3333 둘째 시전 · 5433 폭발 + 맞음 + 보호막
                [1009807] = new[] { "fx_vela_attack2_1_cast" }, [1009806] = new[] { "fx_vela_attack2_1" }, [1009810] = new[] { "fx_vela_attack2_1_hit" },
                [1009809] = new[] { "fx_vela_attack2_2_cast" }, [1009808] = new[] { "fx_vela_attack2_2" }, [1009811] = new[] { "fx_vela_attack2_2_hit" },
                [1009812] = new[] { "fx_vela_attack2_barrier_1" },
                // 스킬 — 0 손 불꽃 · 1933 적 폭발 + 불꽃 벽 + 섬광
                [1009815] = new[] { "fx_vela_skill1_cast" }, [1009814] = new[] { "fx_vela_skill1_1" }, [1009816] = new[] { "fx_vela_skill1_hit" },
            },
            ["비비"] = new Dictionary<int, string[]>
            {
                // 평타 — 400 · 1033 물 베기 1 · 2 + 맞음(두 때 다 나오는 ID 가 맞음)
                [1006101] = new[] { "fx_vivi_attack_hit_2" }, [1006102] = new[] { "fx_vivi_attack_slash_1" }, [1006119] = new[] { "fx_vivi_attack_slash_2" },
                // 강공 — 667~867 단검 넷 꽂힘 + 맞음 넷(slash 짝은 낮음)
                [1006131] = new[] { "fx_vivi_attack_hit_2" }, [1006122] = new[] { "fx_vivi_attack_stomp_1" },
                [1006123] = new[] { "fx_vivi_attack_slash_1" }, [1006124] = new[] { "fx_vivi_attack_slash_2" },
                // 스킬 — 100 수은 모으기 · 667 보호막(이드 원작 장을 빌려 씀)
                [1006111] = new[] { "fx_vivi_skill_cast_1" }, [1006108] = new[] { "fx_vivi_skill_barrier_1", "fx_vivi_skill_barrier_1_end" },   // (10-18 3차) 터지는 끝(_end)은 같은 묶음에서 EVENT_AFTER_MS 만큼 뒤   // (10-18 2차) 원작 수은 쉴드(추출기가 꺼진 유지 단계를 읽게 고쳐 새로 뽑음) — 이드 보호막을 빌리던 것. 터지는 끝(barrier_1_end)은 시각을 몰라 아직 안 단다
            },
            ["시온더다크불릿"] = new Dictionary<int, string[]>
            {
                // 평타 — 567 조준선 · 1067 번개 가시 폭발 + 발밑 반동 연기(탄은 날지 않는다)
                [1005502] = new[] { "fx_xxionx_crosshair_1" }, [1005501] = new[] { "fx_xxionx_attack_hit_1" }, [1005503] = new[] { "fx_xxionx_attack_fire_1" },
                // 강공 — 833 흰 구름 · 검은 연기 폭발
                [1005509] = new[] { "fx_xxionx_skill_explosion_1" }, [1005506] = new[] { "fx_xxionx_attack_hit_2" },   // (10-18 2차 사용자 「흙먼지 폭발 없음」) 767 검은 구름 폭발 · 833 흰 · 푸른 가시(둘 차례는 66ms 차라 낮음)
                // 스킬은 뺀다 — 카드는 엔진 타격 수만큼만 Loop 를 이어, 시범(1타)에선 둘째 발부터 장이 없었다. 예전 표(보이는 타격마다 되틀기 REP)로(10-18 검토_2)
            },
            ["요미"] = new Dictionary<int, string[]>
            {
                // 평타 — 667 별 · 강공 — 900 운석 · 967 폭발 · 스킬 — 300 칼 바퀴 · 1700 달 + 바닥 고리 + 적 별
                [1009201] = new[] { "fx_yomi_attack_hit_1" },
                // (10-18 3차) 강공 폭발은 혜성이 닿을 때 — 900 묶음에 넣어 탄 비행(SHOT_LAG_MS 240)만큼 뒤에. 967(1009207)에 따로 두니 0.24초 떨어지는 혜성보다 먼저 터졌다
                //   (원작 영상 혜성 0.2초 ≈ 몸짓 0.24초 뒤 닿으며 분홍 폭발 · 폭발 장 이미터 지연은 모두 0 이라 장 안 지연이 아니다 → 원작은 탄이 닿을 때 트는 것으로 본다)
                [1009206] = new[] { "fx_yomi_attack2_proj_1", "fx_yomi_attack2_expolosion_1" },
                [1009215] = new[] { "fx_yomi_skill_cast_1" }, [1009211] = new[] { "fx_yomi_skill_ground_1" }, [1009212] = new[] { "fx_yomi_skill_explosion_1" },
            },
        };
        public static bool HasEventFx(string heroKey) => heroKey != null && EVENT_FX.ContainsKey(heroKey);
        // 이벤트 묶음 시각보다 늦게 트는 장(몸짓 ms) — 원작은 게임이 단계(시작 → 유지 → 끝)를 켜 가며 트는데 끝 단계에는 이벤트가 없다
        //   (10-18 3차) 비비 스킬 수은 구 끝 — 원작 저학년.mp4 구가 0.9초에 감싸고 1.8초에 터진다(0.9초 ≈ 몸짓 1.08초) → 구 이벤트(667) + 1080
        static readonly Dictionary<string, int> EVENT_AFTER_MS = new Dictionary<string, int> { ["fx_vivi_skill_barrier_1_end"] = 1080 };
        // 탄 비행이 PROJ_LAG(330)와 다른 탄 — 같은 묶음의 대상 장이 이만큼 뒤(몸짓 ms). UltFx.FirePart 의 그 탄 MoveDur 와 같은 값
        static readonly Dictionary<string, int> SHOT_LAG_MS = new Dictionary<string, int> { ["fx_yomi_attack2_proj_1"] = 240, ["fx_renewa_attack_proj_1"] = 120 };   // (10-18 3차) 요미 강공 혜성 0.24초 · 리뉴아 평타 렌즈 0.12초(원작 영상 3칸)

        // 이벤트(몸짓 ms, ID) → 카드 계획. 같은 때의 ID 들은 한 묶음 — 그 묶음에 탄(move)이 있으면 대상 장은 탄이 닿을 때(+PROJ_LAG).
        //   자리 · 크기는 손 표(CARD_AT_BY · CARD_PART_SCALE)를 그대로 따른다. 짝이 하나도 없으면 null(예전 CardPlan)
        public static List<UltPart> EventPlan(string heroKey, string anim, IList<KeyValuePair<int, int>> evs)
        {
            if (evs == null || evs.Count == 0 || !HasEventFx(heroKey)) return null;
            var map = EVENT_FX[heroKey];
            var res = new List<UltPart>();
            foreach (var g in evs.GroupBy(e => e.Key).OrderBy(g => g.Key))
            {
                var names = g.SelectMany(e => map.TryGetValue(e.Value, out var n) ? n : new string[0]).Distinct().Where(FxLibrary.Has).ToList();
                if (names.Count == 0) continue;
                var parts = PartScale(Shape(names, false), heroKey, anim);
                bool shot = parts.Any(p => p.At == "move");
                int lag = shot ? parts.Where(p => p.At == "move").Max(p => SHOT_LAG_MS.TryGetValue(p.Name, out var l) ? l : UltFx.PROJ_LAG) : 0;
                foreach (var p in parts) p.AtMs = g.Key + (shot && p.At == "target" ? lag : 0) + (EVENT_AFTER_MS.TryGetValue(p.Name, out var af) ? af : 0);   // CARD_TIME 이 넣은 값도 이벤트 시각으로 덮는다
                res.AddRange(parts);
            }
            return res.Count > 0 ? res : null;
        }

        // 원작대로 모드에서도 크기 보정을 쓰는 장 — 다른 사도 장을 빌려 쓰는 공용 보호막(이드 원작 보호막을 이드 몸 크기로: 10-17 2차 사용자 「보호막이 이드보다 커서 감싸야」)
        // 카드 장을 몸짓 이 시각(ms, 1배속 — 원작 영상 0.1초 분석 orig_analysis)에 튼다. 타격 순간이 아니라 원작 차례대로 나오는 장(10-17 3차 사용자)
        //   몸짓 ms 다 — 화면 시각(÷ 카드 배속 · 줄인 몫)으로는 BolzenaFx.Card 가 한 번만 옮긴다(CardClock)
        static readonly Dictionary<string, float> CARD_TIME = new Dictionary<string, float>
        {
            ["벨라|Attack1|fx_vela_attack1_1"] = 700,
            ["란|Attack2|fx_ran_attack_slash_2_default"] = 300, ["란|Attack2|fx_ran_attack_hit_1_default"] = 450,   // (재검토) 원작 반달 0.3초 — 0.9초에 나왔다   // 장 안에 베기 둘(0.05 · 0.7초 지연) — 몸짓 0.8 · 1.4초 베기에
            ["죠안|Attack2|fx_joanne_attack2_1_cast_1"] = 0, ["죠안|Attack2|fx_joanne_attack2_1_buff_1"] = 100,   // (3차) 장판은 몸짓 시작에 깔린다(원작 0.2초) — 타격 때(2.0초) 깔렸다
            ["비비|Skill1|fx_vivi_skill_cast_1"] = 300, ["비비|Skill1|fx_ed_shield_1"] = 900,   // 수은 모으기 → 보호막(「터지고 생기는」 순서였다)
            ["티그_영웅|Skill1|fx_tighero_skill_dash_1"] = 200, ["티그_영웅|Skill1|fx_tighero_skill_dash_2"] = 1450,   // 갈 때 초승달 · 돌아올 때 호랑이 돌진 — 한꺼번에 나왔다
            ["란|Skill1|fx_ran_skill_cast_1_default"] = 1100, ["란|Skill1|fx_ran_skill_hit_2_default"] = 1450, ["란|Skill1|fx_ran_skill_hit_1_default"] = 1450,
            ["란|Skill1|fx_ran_skill_slash_4_default"] = 1550, ["란|Skill1|fx_ran_skill_ready_1_default"] = 1650, ["란|Skill1|fx_ran_skill_slash_3_default"] = 1850,   // 중간 베기 → 늑대 표식 → 마지막 큰 고리
            // (fix_Y 9) 우이(기억) 평타 — 땅에서 빛 모으기 0.6 · 손 별 1.4 · 베기 0.7(전엔 몸짓 0ms 에 한꺼번에)
            // (10-18 원작 대조 제안_C) 원작 영상은 몸짓보다 1.2배 빠르게 녹화돼 있다(장 안 시각으로 확인) — 영상 초 × 1200 = 몸짓 ms
            ["우이_기억|Attack1|fx_uimemory_attack1_cast_1_default"] = 700, ["우이_기억|Attack1|fx_uimemory_attack1_cast_2_default"] = 1650,
            ["우이_기억|Attack1|fx_uimemory_attack1_slash_1_default"] = 800,
            ["우이_기억|Attack2|fx_uimemory_attack2_pond_1_default"] = 1550,   // 연못이 맞음보다 늦게 떴다
            ["우이_기억|Skill1|fx_uimemory_skill1_explosion_bottom_1_default"] = 1750, ["우이_기억|Skill1|fx_uimemory_skill1_explosion_center_1_default"] = 1750,
            ["우이_기억|Skill1|fx_uimemory_skill1_buff_clover_default"] = 3200,   // 머리 뒤 클로버 — 타격 때 발밑에 깔려 바닥 문양 밑에 묻혔다
            // 비비(신성) — 손 고리 · 리본 장은 장 안 시각표가 원작 차례 그대로라 몸짓 시작에(맞은 뒤에 나왔다)
            ["비비_신성|Attack1|fx_vividivine_attack_1_default"] = 0,
            ["실비아|Skill1|fx_silvia_skill_hit_1_default"] = 6600,   // (10-18 검토_2) 찻잔 폭발은 다과회 끝 — 시작에 나왔다
            ["에피카|Attack2|fx_epica_music_aura_4"] = 533,   // (10-18 검토_1) 소용돌이 고리는 탄을 쏘기 전(예전 표로 도는 몸짓)
            ["비비_신성|Attack2|fx_vividivine_attack_2_default"] = 0, ["비비_신성|Attack2|fx_vividivine_attack_2_proj_default"] = 1500,
            ["비비_신성|Skill1|fx_vividivine_skill_1_default"] = 0, ["비비_신성|Skill1|fx_vividivine_skill_1_proj_default"] = 2500,
            ["비비_신성|Skill1|fx_vividivine_skill_1_hit_1_default"] = 3100, ["비비_신성|Skill1|fx_vividivine_skill_1_hit_2_jolly_default"] = 3550,
        };
        static readonly HashSet<string> ORIG_SCALE_OK = new HashSet<string> { "이드|Skill1|fx_ed_shield_1", "비비|Skill1|fx_ed_shield_1", "디아나_왕년|Skill1|fx_dianayester_skill_cast_1",
            "에피카|Attack2|fx_epica_attack_proj_2", "티그_영웅|Skill1|fx_tighero_skill_dash_1" };   // (10-18 4차) 에피카 강공 불 에피콘 탄 · 티그 스킬 초승달   // 디아나 스킬 기 모으는 붉은 구 — 원작보다 컸다(10-18)
        static List<UltPart> PartScale(List<UltPart> plan, string heroKey, string anim)
        {
            var mk = MotionOf(anim);
            if (mk == null || heroKey == null) return plan;
            foreach (var p in plan)
            {
                var psk = heroKey + "|" + mk + "|" + p.Name;
                if ((!BolzenaFx.OriginalOnly || ORIG_SCALE_OK.Contains(psk)) && CARD_PART_SCALE.TryGetValue(psk, out var sc)) p.Scale = sc;
                if (CARD_TIME.TryGetValue(psk, out var ms)) p.AtMs = ms;   // 원작 영상 시각(몸짓 1배속 ms)
                if (CARD_AT_BY.TryGetValue(heroKey + "|" + mk + "|" + p.Name, out var at)) { p.Muzzle = at == "muzzle"; p.Body = at == "body"; p.At = p.Muzzle ? "caster" : p.Body ? "target" : at; p.Ally = false; p.Pre = false; }   // body = 대상 몸 가운데
            }
            return plan;
        }

        public static float CardScale(string heroKey, string anim)
        {
            var mk = MotionOf(anim);
            return !BolzenaFx.OriginalOnly && mk != null && heroKey != null && CARD_SCALE.TryGetValue(heroKey + "|" + mk, out var s) ? s : 1f;
        }

        // 장 자리 고침 — 이름 낱말로는 틀리는 것
        //   우로스 강공 금빛 용(snake) — 이미터가 시전자 뒤(-19)에서 앞(+34)으로 짜인 판인데 낱말이 없어 대상에 가운데 맞춰 줄어 보였다 → 시전자 발밑에서 적 쪽으로
        //   죠안 강공 표식(buff_1_target …) — 「buff」 낱말로 시전자에 가던 것 → 맞는 적
        //   에피카 연주(music_play) — 낱말이 없어 대상에 가던 것 → 시전자
        static readonly Dictionary<string, string> CARD_AT = new Dictionary<string, string>
        {
            ["fx_ed_shield_1"] = "caster_body",
            ["fx_vivi_skill_barrier_1_end"] = "caster_body",   // (10-18 3차) 구가 터지는 끝 — 구와 같은 자리
            ["fx_vivi_skill_barrier_1"] = "caster_body",   // (10-18 2차) 비비 수은 구 — 이미터가 다 원점(0,0) 둘레 고리 · 물이라 몸 가운데(원작 영상: 구가 몸 전체를 감싼다)
            ["fx_uimemory_skill1_buff_clover_default"] = "caster_body",
            ["fx_vela_attack1_1"] = "caster_body",   // (10-18 검토_2) 벨라 불꽃 베기는 허리 높이 — 발밑이면 바닥을 그었다
            ["fx_silvia_skill_teatable_2_default"] = "caster", ["fx_silvia_skill_teatable_3_default"] = "caster", ["fx_silvia_skill_teatable_4_default"] = "caster",   // 다과회 덤불도 실비아 뒤 — 적 자리에 깔려 적을 가렸다
            ["fx_ed_attacksub_1"] = "caster_body", ["fx_epica_music_aura_2"] = "caster_body",   // (10-18 검토_1) 나타 볼 · 에피카 거품 구는 몸 가운데 — 땅바닥 · 낮게 떴다   // (10-18 제안_C) 우이(기억) 머리 뒤 클로버 — buff 낱말로 발밑에 깔렸다
            ["fx_vividivine_skill_1_default"] = "caster", ["fx_vividivine_attack_2_default"] = "caster", ["fx_yomi_skill_cast_1"] = "caster_body", ["fx_vivi_skill_cast_1"] = "caster_body",   // (10-17) 시전자 쪽 장 — 낱말이 없어 적에게 갔다
            ["fx_joanne_attack2_1_buff_1"] = "caster", ["fx_joanne_attack2_1_buff_2"] = "caster", ["fx_joanne_attack2_1_cast_1"] = "caster", ["fx_joanne_attack2_1_cast_2"] = "caster",   // (10-17 2차) 죠안 강공 장판은 죠안 밑
            ["fx_joanne_skill_cast_1"] = "caster", ["fx_joanne_skill_cast_2"] = "caster", ["fx_joanne_skill_cast_4"] = "caster",   // 스킬 장판도 죠안 밑
            ["fx_uieru_skill_ground_1"] = "target", ["fx_uieru_ultimate_flowerdebuff_1_default"] = "target",   // 우이 스킬 비 장판 · 보라 클로버는 적 쪽
            ["fx_silvia_skill1_teatime_1_default"] = "caster", ["fx_silvia_skill_teatable_1_default"] = "caster",   // (10-16) 다과회 장면은 실비아 뒤에 펼쳐진다 — 적 자리에 깔려 아치 · 덤불이 적을 가렸다   // (10-16 전수조사) 원작 보호막 구체는 몸 가운데 기준으로 짜였다 — 발밑에 붙여 구체가 이드 아래 바닥에 떴다
            ["fx_xxionx_skill_explosion_1"] = "target",   // 시온 강공 · 스킬 — 대상 폭발
            ["fx_vela_attack2_2"] = "target",
            ["fx_vela_attack2_barrier_1"] = "caster_body",   // (fix_Y 5-1) 벨라 강공 보호막 — 「barrier」 낱말로 아군 판이던 것, 원작은 둘째 폭발 뒤 시전자 몸에   // 벨라 강공 금 간 구슬 — 장 안 좌표로 시전자에 갔다가 몸을 덮었다(2026-10-14 원작 대조: 구슬은 적 쪽)
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
            ["fx_ed_attack_2"] = "caster_body", ["fx_ed_attack_1_1"] = "caster_body",   // (10-17 정밀 분석) 총구(나타 쉬는 자리 = 달 뒤 위)에서 적 쪽으로 돌리면 위로 섰다 — 원작은 몸 앞 · 몸 높이 가로
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
