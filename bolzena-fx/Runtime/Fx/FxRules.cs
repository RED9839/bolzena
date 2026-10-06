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
        static readonly Regex HITW = new Regex(@"_hit(_|\d|$)|hitslash");
        static readonly Regex TAIL = new Regex(@"((?<=[a-z])\d+)?(_default)?$");   // slash04 · slash05 같은 번호 판만 하나로(_1_1 · _1_2 는 다른 판이라 둔다)
        public static List<UltPart> CardPlan(string heroKey, string anim, string tier)
        {
            foreach (var kind in CardKinds(anim, tier))
            {
                var names = FxLibrary.Get(heroKey, kind).ToList();
                if (names.Count == 0) continue;
                if (kind == "ult") return UltFx.Plan(heroKey);
                var seen = new HashSet<string>();
                names = names.Where(n => seen.Add(TAIL.Replace(n, ""))).ToList();
                var plan = UltFx.PlanNames(names, null, 99);
                foreach (var p in plan) if (p.At == "target" && HITW.IsMatch(p.Name) && !p.Name.Contains("ground")) p.Body = true;
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
            return new List<UltPart>();
        }
    }
}
