using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Bolzena.Fx
{
    // 고학년 한 벌의 옵션 — 웹판 playUltFx(heroKey, o) 의 o
    public class UltOptions
    {
        public Vector3 From;                 // 시전자 발밑(월드)
        public Vector3? To;                  // 대상 지점(발밑 높이) — 단일 대상은 그 대상, 전체 공격은 적 무리의 정중앙(없으면 From)
        public float ToWidth;                // 전체 공격 — 적 무리의 가로 폭(월드). 대상 쪽 이펙트가 이 폭을 덮도록 벌린다(0 = 단일 대상)
        public float Scale = 1f;
        public float KeepMax = UltFx.DUR_KEEP_MAX;   // 장을 제 길이만큼 둘 때 최대(초) — 카드는 짧게(FxBattle.CARD_KEEP_MAX)
        public float? Top;                   // 이펙트가 넘지 않을 위끝(월드 y)
        public float? Impact;                // SD 동작이 쏘는/때리는 순간(ms, 동작 시작에서 — 스파인 이벤트). 주면 그 순간에 맞춘다
        public float? End;                   // 여러 번 때리는 창의 끝(ms)
        public IList<float> Marks;           // 그 창의 이벤트 시각들(ms) — 레이저 · 타격을 다시 튼다
        public UltPick? Pick;                 // SD 고학년 갈래(앨리스 불 · 번개 · 바람)
        public Func<Vector3?> Muzzle;        // 시전자 총구(월드). 주면 총구 쪽 이펙트가 붙어 따라간다
        public bool Dash;                    // 시전자가 달려가 부딪친다 — To 가 부딪치는 자리
        public int Order = 300;
        public string SortingLayer;
        public Transform Parent;
        public Vector3? ShotFrom;            // 투사체가 떠나는 자리(시전 본 — 없으면 From)
        public bool Aim;                     // 투사체를 나는 쪽으로 돌린다(카드 — 무기 끝에서 맞는 적 몸으로). 고학년은 그대로
        public bool FitTop;                  // 카드 — 그린 판이 Top 위로 나가면 줄이고 내린다(FxPlayOptions.FitTop). 고학년은 그대로
        public Vector3? ToBody;              // 대상 몸 가운데 — 몸에 맞는 이펙트(UltPart.Body)가 여기
        public Action<FxRun> OnPart;         // 한 장 틀 때마다(시험 · 재기)
        public IList<Func<Vector3>> Allies;
        // 타격 맞추기 — 전투가 정한 맞는 시각들(ms, 몸짓 시작에서 · 투사체 몫까지 넣은 것). 주면 대상 쪽 이펙트를 그 시각에(이펙트 안의 터지는 때만큼 일찍),
        // 다시 틀기를 그 타격들에 맞춰 튼다. Clock 은 전투 시계 — 전투가 매 프레임 Ms 를 적으면 이펙트 시계가 뒤처지지 않게 따라간다
        public IList<float> HitMs;
        public UltClock Clock;
        public Action<UltPart, float> OnFire;
        // 시전자 몸(자리 본) — 시전자 쪽 장을 발밑 대신 뼈에 붙여 매 프레임 따라가게(이름 안의 Point 이름 → 뜻 낱말 → 이 동작의 시전 본 → 발밑)
        public FxActor CasterActor;
        public string CastPoint;
        public IList<Vector3> TargetFeet;    // 맞는 적들 발밑(앞 → 뒤)
        public IList<Vector3> TargetTops;    // 맞는 적들 머리(앞 → 뒤, TargetFeet 와 같은 차례)
        public bool SeqPerTarget;            // 번호 장을 적마다 차례로(아야 · 아네트)
        public readonly Dictionary<UltPart, int> SeqIndex = new Dictionary<UltPart, int>();  // 틀 때(그 장 · 이펙트 시계 ms) — 점검  // 아군 발밑들 — 아군 쪽 판(UltPart.Ally)을 시전자와 함께 아군 각자에게도 튼다
    }

    public class UltClock { public float Ms = -1; }

    public struct UltPick { public int I, N; public UltPick(int i, int n) { I = i; N = n; } }

    // 고학년 이펙트 한 장의 자리 · 차례 — 웹판 ultPlan 의 한 줄
    public class UltPart
    {
        public string Name;
        public string At;                    // "caster" · "target" · "move"(투사체)
        public bool Pre;                     // 준비(모으기 · 시전) — 바로
        public bool Again;                   // 때리는 창 동안 다시 튼다(레이저 · 타격 · 베기)
        public bool Muzzle;                  // 총구에 붙는다
        public bool Body;                    // 대상 발밑이 아니라 몸 가운데(원작 타격 프리팹 prefabeffecthit — 카드에서만)
        public bool Ally;                    // 아군 쪽 판(_a · heal · buff · revival) — 시전자(와 UltOptions.Allies)에게
        public float Dur;
        public float Scale = 1f;            // 이 장만의 크기 배율(맞춤 표 · 카드 장 배율 표 — 2026-10-14 원작 대조: 화면을 덮어야 할 장이 작았다)
        public float Cap;                    // 이 초를 넘겨 두지 않는다(0 = 없음) — 원작은 클로즈업이 덮는 동안 지나가는 단계를, 클로즈업을 건너뛴 우리는 드러냈다(리뉴아 다각형 바닥, 10-16)
        public float? AtMs;                  // 이 때(ms, 이펙트 시계) 튼다 — 원작 영상 맞춤(BolzenaFx.UltTweak)이 정한 장. 없으면 갈래대로
        public bool Rep;                     // 타격마다 다시 튼다(투사체 · 조준선 — 에피카 고학년 탄 · 시온 스킬 X 조준). AtMs 를 주면 갈래 차례에서 빠지니 주지 않는다
        public override string ToString() => $"{Name} [{At}{(Pre ? " pre" : "")}{(Again ? " again" : "")}{(Muzzle ? " muzzle" : "")}] {Dur:0.##}s";
    }

    // 웹판 js/fx-burst.js 의 ultPlan · ultLagMs · ultImpactMs · playUltFx 를 그대로 옮겼다.
    // 이름 낱말로 자리와 차례를 가른다 — 앞(준비 · 시전 · 충전)은 바로, 본(폭발 · 타격 · 바닥 …)은 때리는 순간.
    // 투사체는 시전자에서 대상으로 날아가고, 레이저는 시전자에서 대상 쪽을 보고 뻗는다.
    public static class UltFx
    {
        static readonly Regex AT_CASTER = new Regex("(ready|cast|charge|start|aura|buff|player|meditation|book|energy|craft|happy|spawn|muzzle|rise|summon|wing|shield|heal|barrier|roar|breath|clap|personal)");
        static readonly Regex AT_TARGET = new Regex("(explosion|hit|ground|crash|impact|rain|thunder|stomp|meteor|slash|attack|bomb|burst|boom|crack|wave|field|area|decal|target|fall|strike|drop|lightning|fire|ice|smoke|trace)");
        static readonly Regex PRE = new Regex("(ready|cast|charge|start|meditation|aura)");
        static readonly Regex PROJ = new Regex("(proj|projectile|bullet|missile|shot|arrow|throw|(^|_)wave(_|$))");
        static readonly Regex BEAM = new Regex(@"(laser|lazer|beam|ray\b|line|(?<!field_)light(_\d+)?$)");   // light — 레테 손전등 빛(2026-10-06)
        static readonly Regex MUZZLE = new Regex(@"(charge|muzzle|shot|fire|laser|lazer|beam|ray\b|(?<!field_)light(_\d+)?$)");
        static readonly Regex CAST_OR_CHARGE = new Regex("(cast|charge)");
        static readonly Regex AGAIN = new Regex("(hit|slash|stab)");
        static readonly Regex SCREEN = new Regex(@"(^|_)(camera|screen|bg|fullscreen)(_|$|\d)");   // 화면 전체(카메라 앞) — 볼제나 추가, 웹판에는 없던 갈래
        static readonly Regex ALLY = new Regex(@"(_a$|_a_|(^|_)(heal|buff|revival)(_|$|\d))");   // 볼제나 추가(2026-10 벨라 고학년 제보)
        static readonly Regex ALLY2 = new Regex("((?<!de)buff|barrier|shield)");
        //   (2026-10-14 원작 비교) 낱말 붙은 강화 · 보호막(flowerbuff · startbuff · barrier · shield)도 아군 쪽 — 우이 고학년 거품 · 실비아 고학년 보호막이 적 위에 떴다.
        //   맞음 · 표식 · 내려찍기(barrier_hit · buff_target · shieldstomp)는 그대로 대상, 약화(debuff)는 맞는 적에
        static readonly Regex NOT_ALLY = new Regex("(hit|target|stomp)");
        static readonly Regex DEBUFF = new Regex("debuff");
        // 이름 낱말로는 자리가 틀리는 고학년 장(원작 영상으로 본 자리) — "ally" 시전자 + 아군 · "caster" 시전자 몸 · "target" 맞는 적
        //   우이 flower_1 — 초록 클로버 터짐 · 거품(DummyBoll)이 아군 위(원작 1.8~3.0초) · 벨라 end · lantern — 끝의 큰 보라 구체가 벨라 자신을 감싼다(원작 3.3~3.8초)
        static readonly Dictionary<string, string> AT_FIX = new Dictionary<string, string>
        {
            ["fx_uieru_ultimate_flower_1_default"] = "ally",
            ["fx_vela_ultimate1_end"] = "caster",
            ["fx_vela_ultimate1_lantern"] = "caster",
        };
        static readonly Regex SEQ_TAIL = new Regex(@"_\d+$");
        static readonly Regex WORD = new Regex(@"^fx_[a-z0-9]+_(ultimate\d*_|ult_|skill\d*_?|personal_?|attack\d*_?)?");   // attack_ — 카드 attack_hand_1 이 「attack」 낱말로 대상에 가던 것
        public const int MAX_ULT = 12;   // 2026-10-14 8 → 12 — 시온(10장)이 dance 3장만 남아 원작 리본 · 불꽃 장판 · 조준이 통째로 빠졌다(원작 대조)
        public const int PROJ_LAG = 330;
        public const float DUR_KEEP_MAX = 6f;
        // 타격보다 먼저 떠 있다가 타격에 사라지는 장 — ms 만큼 앞서 튼다(시온 평타 조준선: 원작은 조준선 0.35초 뒤 그 자리가 터진다, 탄은 날지 않는다 — 2026-10-15 사용자)
        public static readonly Dictionary<string, float> LEAD_MS = new Dictionary<string, float> { ["fx_xxionx_crosshair_1"] = 380f, ["fx_joanne_attack1_1_ready_1"] = 700f };   // 죠안 평타: 적 몸 노란 조준 0.3~0.9초 → 1.0초 터짐(원작 대조 10-15)
        // 카드라도 제 길이만큼 남기는 장 — 맞은 적에 붙는 표식(란 늑대 머리 Wolf_2 수명 3초: 원작은 스킬 뒤 3초 넘게 적 위에 있다, 2026-10-15)
        static readonly HashSet<string> KEEP_FULL = new HashSet<string> { "fx_ran_skill_ready_1_default", "fx_uros_skill_ground_1",
            "fx_vividivine_skill_1_hit_2_jolly_default", "fx_vividivine_skill_1_hit_2_cool_default",   // 비비(신성) 스킬 바닥 고리(장 4.5초) — 카드 상한 1.2초에 4.0초쯤 걷혔다(fix_Y 8). cool 은 다시 가져온 뒤 이름
            "fx_silvia_skill1_teatime_1_default", "fx_renewa_skill_impact_1", "fx_ran_ultimate_cast_4_default", "fx_ran_ultimate_cast_5_default", "fx_silvia_skill_teatable_1_default", "fx_silvia_skill_teatable_2_default", "fx_silvia_skill_teatable_3_default" };   // 실비아 스킬 다과회 장면(아치 창 · 파라솔 · 탁자 · 덤불) — 원작은 4초 넘게 이어진다, 1.2초로 자르면 조각만 잠깐 보였다(10-16)   // 우로스 스킬 바닥 원 — 원작은 6초 동안 깔린 채 불꽃이 오른다   // 장을 제 길이만큼 둘 때 최대(초)

        static string Word(string name) => WORD.Replace(name, "", 1);
        static string Head(string name) => Word(name).Split('_')[0];

        // (10-18 사용자 2차 「비비(신성) 자기 속성 색」) 시전 사도 성격 → 원작 성격 갈래 이름(장 이름의 _jolly_ 자리). 전투가 고학년 · 카드 틀기 전에 넣는다.
        //   그 성격 판이 라이브러리에 없으면 jolly 그대로(고학년 추출은 jolly 한 갈래 — 나머지는 fx_extract_more.py EXTRA 로 비비(신성)만 뽑았다)
        public static string Mood;
        public static readonly Dictionary<string, string> MOOD = new Dictionary<string, string> { ["순수"] = "naive", ["광기"] = "mad", ["활발"] = "jolly", ["우울"] = "gloomy", ["냉정"] = "cool", ["공명"] = "resonance" };
        public static string MoodName(string n) { if (Mood == null || n == null || !n.Contains("_jolly_")) return n; var m = n.Replace("_jolly_", "_" + Mood + "_"); return FxLibrary.Has(m) ? m : n; }

        public static List<UltPart> Plan(string heroKey, UltPick? pick = null)
            => FxLibrary.HasUlt(heroKey) ? PlanNames(FxLibrary.Get(heroKey).Select(MoodName).ToList(), pick, MAX_BY.TryGetValue(heroKey, out var mx) ? mx : MAX_ULT, geo: true) : new List<UltPart>();
        // 갈래가 둘 다 원작 한 벌인 사도 — 가장 큰 갈래 하나만 남기면 끝 장이 빠진다(아일라: 분노 5 + 운석 4 — 끝의 운석 낙하 · 폭발이 없었다)
        static readonly Dictionary<string, float> BIG = new Dictionary<string, float> { ["elena_ultimate_drone_2"] = 2.2f };
        static readonly Dictionary<string, float> SWEEP = new Dictionary<string, float>
        {
            ["rohnemayor_ultimate_crowd_emoji"] = 1.4f, ["rohnemayor_ultimate_object"] = 1.4f, ["rohnemayor_ultimate_smoke"] = 1.4f,
            ["shasha_ultimate_waterfall"] = 1.6f,
            ["erpinroyale_attack1_shockwave_1"] = 0.4f,   // (마력탄 proj_1 은 그대로 날아가 첫 적에)
            ["uros_attack2_snake"] = 0.55f,   // 우로스 강공 금빛 뱀 — 시전자 쪽에서 적들을 휩쓸고 지나간다(원작 대조 10-15)   // 에르핀(왕도) 평타 분홍 충격파 — 시전자 앞에서 적들을 꿰뚫고 화면 끝까지(원작 대조 10-15)
        };
        static readonly Regex NO_BONE = new Regex("vividivine_ultimate_full_laser");
        static readonly Regex REP = new Regex("xxionx_(crosshair_1|crosshair_3|attack_fire_1|attack_hit_1)|uieru_skill_waterhit_1");   // (10-18) 우이 스킬 빛 비 — 원작은 타격마다(4번)   // (10-18 2차) 시온 스킬 X 조준(crosshair_3)도
          // 타격마다 되트는 카드 장 — 시온 스킬 탄마다 X 조준 · 총구 · 맞음(fix_Y 6-2)
        static readonly Regex FRONT_OF_CASTER = new Regex("xxionx_ultimate_(dance_|cast_1|fire_1|eye_1)");
        static readonly Regex YOMI_SWEEP = new Regex("yomi_ultimate_[23]$");
        static readonly Regex FEET_FIXED = new Regex("ed_skillcast_1|ed_ultimate_bodycast_1|silvia_skill1_teatime|silvia_skill_teatable|uimemory_attack2_cast_1|uimemory_attack2_pond");   // 연못은 처음 자리에 가만히 — 우이만 돌아다닌다(원작)   // (재검토) 우이(기억) 강공 물보라 — 머리 높이 적 쪽에 떴다, 원작은 발밑   // 실비아 티타임 장면 — 발 뼈를 따라 계속 움직였다(10-17 사용자)
        // (10-18 4차 사용자 「깃발 꽂는 곳 ≠ 대지 깨지는 곳」) 네르(빡침) 스킬 빛기둥 · 균열 · 바닥 고리 · 맞음은 깃발 밑동이 원점(이미터 원점 0, −0.9 둘레)
        public const float NER_FLAG_FWD = 1.4f;   // 깃발 앞 거리(키 배) — BattleDirector.Pets DROPS 가 이 값을 쓴다
        static readonly Regex NER_FLAG = new Regex("^fx_nerrage_skill_(1|1_end|ground|hit)$");   // skill_end · skill_charge(y 8 넘게 — 치켜든 깃발 끝)는 네르 손 쪽 그대로
        static readonly Regex BODY_FOLLOW = new Regex("^fx_vivi_skill_barrier_1$");   // (10-18 4차) 내려앉는 비비 몸을 따라가는 수은 구 — 터지는 끝(_end)은 그 순간 자리에 한 번
        const float AYA_BACK = 0.35f;   // (10-18 4차 사용자 「왕복 더 빠르게」) 아야 나비 돌아올 때 — 첫 적 거리를 이 초에(원작 0.6)
        static readonly Regex BODY_AIM = new Regex("ed_attack_2$");   // (재검토) 평타 레이저(attack_1_1)는 돌리지 않는다 — 떠 있는 이드 몸에서 적 몸으로 겨누니 비스듬히 내려갔다. 원작은 가로   // (10-17 정밀 분석) 이드 레이저 — 판은 위로 짜였다. 총구(나타 쉬는 자리, 달 뒤 위)에서 돌리면 위로 섰고, 원작은 몸 앞 · 몸 높이에서 가로
        static readonly Regex TARGET_FACING = new Regex("erpinroyale_attack1_shockwave_1|nerrage_attack_2$|nerrage_attack_2_change|ran_skill_slash_4|tighero_skill_dash_1|tighero_skill_dash_2|uros_ultimate_slash_jolly|ran_attack_slash_2|silvia_attack_1_hit_[12]_");   // (10-18 3차) 첫째 맞음(hit_1)도 — 장 데이터 [Atlas_ab]_Core 긴 축이 (0.345, −0.272) 오른쪽 아래 「\」, 원작 기본공격.mp4 0.53~0.6초는 「/」 · cap_fix3 seq 0.708초 우리 「\」   // (10-18 2차 사용자) 실비아 둘째 베기 맞음 사선 「\」 → 원작 「/」(판 변환이 거울)   // (재검토) 티그 갈 때 초승달 x −9.4 · 우로스 고학년 원 베기 x −14.6 — 시전자 뒤로 그려졌다   // 란 스킬 반달 베기 — 맞음 불꽃이 x −9.5(왼쪽)에 짜였다(10-17 사용자 「자기 쪽으로 벤다」)
          // 네르 강공 — 도끼 빛이 적 쪽으로 소환돼 내려찍는다(10-17 사용자 「우리 쪽 방향」)
        // (10-18 2차) 카드에서 아군에게 보내는 장(이것 말고 이름이 buff 인 카드 장은 예전처럼 시전자에만 — FxBattle.Card) · 시전자 몫은 안 튼다. 실비아 스킬 초대장
        internal static readonly HashSet<string> ALLIES_ONLY = new HashSet<string> { "fx_silvia_skill_startbuff_1_default" };
        static readonly Regex GROUND_BACK = new Regex("uros_skill_ground_|uimemory_attack2_pond|dianayester_skill_ground_1");
        // 몸을 덮는 섬광 — 시전자 몸 가운데(Middle 본 · 매 프레임 따라감)에서 크게(배수). 원작은 사도가 그 섬광 속에 「사라진다」
        //   (2026-10-13 비비: 0.2~0.4초 검은 네 갈래 별 · 주황 구체 — fire_1 의 ImpactShapes_Sharp. 발밑 · 적 자리에 작게 떠 사라짐이 약했다)
        // 차원문 레이저 — 원작 판은 맞는 적 자리가 뿌리이고 빛줄기가 뒤쪽 위 차원문 고리(판 안 (12, 13.8))까지 올라가게 짜였다(리뉴아 강공 · 원작 영상 2026-10-15
        //   「손에서 나가는 게 아니라 근처에 차원 열고 거기서 레이저」). 전에는 손 · 문 자리에서 적 쪽으로 돌려 쏴 빛줄기가 하늘로 가거나 짧았다.
        //   값 = 고리 자리(판 안 단위, 앞 = +x). 레이저는 적 발에 돌리지 않고 그대로(가운데 맞춤도 안 함), 문 여는 장(cast)은 그 고리 자리에
        static readonly Dictionary<string, Vector2> PORTAL = new Dictionary<string, Vector2>
        {
            ["fx_renewa_attack_lazer_1"] = new Vector2(12f, 13.8f),
        };
        const float PORTAL_SCALE = 1.35f;   // 원작 고리는 시전자 머리 위로 그림 높이의 약 0.4배 더 — 우리 판 크기로는 낮아 키운다
        static readonly Dictionary<string, string> PORTAL_CAST = new Dictionary<string, string> { ["fx_renewa_attack_cast_3"] = "fx_renewa_attack_lazer_1" };
        static readonly Dictionary<string, float> BODY_FLASH = new Dictionary<string, float> { ["vivi_ultimate_fire_1"] = 1.3f };
        static readonly Dictionary<string, int> MAX_BY = new Dictionary<string, int> { ["아일라"] = 10, ["비비_신성"] = 12, ["에피카"] = 26 };   // 에피카 — 자동차 · 해적선 · 유령 말 세 단계를 다 잇는다(2026-10-15 다시: 스파인 첨부 표본으로 조각 = 단계 확인)   // 비비(신성) — 「full」 갈래 12장(빛기둥 4 · 고리 · 끝 · 앞 · 레이저 5): 8장에서 끊겨 레이저가 하나만 나갔다(2026-10-14)

        // 이름들로 자리 · 차례를 가른다(고학년 · 카드 같이 쓴다). max 넘으면 가장 큰 갈래 하나만
        public static List<UltPart> PlanNames(List<string> names, UltPick? pick = null, int max = MAX_ULT, bool geo = false)
        {
            var res = new List<UltPart>();
            if (names == null || names.Count == 0) return res;
            if (pick.HasValue && pick.Value.N > 1)
            {
                var cnt = new Dictionary<string, int>();
                var order = new List<string>();
                foreach (var x in names) { var k = Head(x); if (!cnt.ContainsKey(k)) { cnt[k] = 0; order.Add(k); } cnt[k]++; }
                var big = order.Where(k => cnt[k] >= 3).ToList();
                List<string> ways = big.Count == pick.Value.N ? big : cnt.Count == pick.Value.N ? order : null;
                if (ways != null) { var w = ways[pick.Value.I % pick.Value.N]; names = names.Where(x => Head(x) == w).ToList(); }
            }
            // 여러 갈래면 가장 큰 갈래 하나만 — 다 틀면 화면이 엉킨다
            if (names.Count > max)
            {
                var n = new Dictionary<string, int>();
                var order = new List<string>();
                foreach (var x in names) { var k = Head(x); if (!n.ContainsKey(k)) { n[k] = 0; order.Add(k); } n[k]++; }
                var top = order.OrderByDescending(k => n[k]).First();   // 같으면 먼저 나온 것(안정 정렬)
                if (n[top] >= 3) names = names.Where(x => Head(x) == top).ToList();
                names = names.Take(max).ToList();
            }
            foreach (var name in names)
            {
                var w = Word(name);
                bool proj = PROJ.IsMatch(w), beam = !proj && BEAM.IsMatch(w);
                AT_FIX.TryGetValue(name, out var fix);
                bool ally = fix == "ally" || (fix == null && (ALLY.IsMatch(w) || (ALLY2.IsMatch(w) && !NOT_ALLY.IsMatch(w))) && !DEBUFF.IsMatch(w));   // 아군 쪽 판(벨라 hit_a · proj_a — 적 쪽은 _e) · 회복 · 강화 · 보호막 — 적에게 틀면 엉뚱한 색이 겹친다
                if (ally || fix != null) { proj = false; beam = false; }
                string at = fix != null ? (fix == "ally" ? "caster" : fix) : SCREEN.IsMatch(w) ? "screen" : ally ? "caster" : proj ? "move" : beam ? "caster" : DEBUFF.IsMatch(w) ? "target"
                    : AT_TARGET.IsMatch(w) && !CAST_OR_CHARGE.IsMatch(w) ? "target" : AT_CASTER.IsMatch(w) ? "caster" : "target";
                // 고학년 — 장 안 좌표로 자리(2026-10-14 원작 대조). 원작 장은 유닛 발밑이 원점이라, 크게 덮는 장(배경 빛 · 달 · 물들임)과
                //   시전자에서 오른쪽으로 뻗는 장판은 시전자 발밑에 제 크기로 두면 원작과 맞는다(요미: 달 · 보라 물들임 · 바닥 장판). 타격 · 폭발 이름은 대상 그대로
                if (geo && fix == null && !ally && !SCREEN.IsMatch(w) && !HIT_LIKE.IsMatch(w) && GeoCaster(name)) { at = "caster"; proj = false; beam = false; }
                bool pre = !proj && PRE.IsMatch(w);
                res.Add(new UltPart
                {
                    Name = name, At = at, Pre = pre, Again = !pre && !ally && (beam || AGAIN.IsMatch(w)), Ally = ally,
                    Muzzle = at == "caster" && MUZZLE.IsMatch(w), Dur = FxLibrary.Duration(name),
                    Rep = REP.IsMatch(name),
                });
            }
            return res;
        }

        static readonly Regex HIT_LIKE = new Regex("(hit|explo|expolo|impact|boom|bomb|crash|drop|strike|blast|statue|target|crosshair|aim)");   // statue — 실비아 분수 조각상은 크지만 적 앞에 선다
        public const float GEO_BIG = 45f, GEO_REACH = 10f;   // 원작 단위 — 크게 덮는 장 · 오른쪽으로 뻗는 장판
        static readonly Dictionary<string, bool> geoCache = new Dictionary<string, bool>();
        /// <summary>장 안 입자들이 놓이는 범위(원작 단위, 발밑 원점) — 크게 덮거나(GEO_BIG) 시전자에서 오른쪽으로 뻗으면(GEO_REACH) 시전자 발밑 장.</summary>
        public static bool GeoCaster(string name)
        {
            if (geoCache.TryGetValue(name, out var got)) return got;
            var b = GeoBounds(name);
            // 크게 덮는 장은 원점 가운데에 짜인 것만(에피카 햇살 battlefield_light 처럼 한쪽으로 치우친 큰 장은 아님) ·
            //   장판은 원점 근처에서 시작해 오른쪽으로 뻗는 것만(비비(신성) full_boll · full_laser 처럼 멀리 떨어진 조각은 아님) — 2026-10-14 원작 대조 2차
            bool r = b.HasValue && ((Mathf.Max(b.Value.width, b.Value.height) >= GEO_BIG && Mathf.Abs(b.Value.center.x) <= b.Value.width * 0.25f)
                || (b.Value.xMax >= GEO_REACH && b.Value.xMin >= -4f && b.Value.xMin <= 4f && b.Value.width >= GEO_REACH));
            geoCache[name] = r;
            return r;
        }
        /// <summary>고학년 계획이 장 안 좌표로 시전자 발밑에 둔 장인가(이름이 타격 · 폭발이 아니고 GeoCaster) — 맞춤 표가 자리를 덮어쓰지 않게.</summary>
        public static bool GeoKeepCaster(string name) { var w = Word(name); return !SCREEN.IsMatch(w) && !HIT_LIKE.IsMatch(w) && GeoCaster(name); }
        public static Rect? GeoBounds(string name)
        {
            var fx = FxLibrary.Effect(name);
            if (fx == null || fx.Em == null) return null;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var e in fx.Em)
            {
                if (e.M3d) continue;
                float half = Mathf.Max(e.Size.x, e.Size.y) * e.SizeK * 0.5f;
                float sx = 0, sy = 0;
                if (e.HasShape && e.M != null && e.M.Length >= 5)
                {
                    float r = e.Shape.K == FxShapeKind.Rect || e.Shape.K == FxShapeKind.Box ? 0.5f : Mathf.Max(0, e.Shape.R);
                    sx = r * (Mathf.Abs(e.M[0]) + Mathf.Abs(e.M[1])); sy = r * (Mathf.Abs(e.M[3]) + Mathf.Abs(e.M[4]));
                }
                x0 = Mathf.Min(x0, e.Pos.x - sx - half); x1 = Mathf.Max(x1, e.Pos.x + sx + half);
                y0 = Mathf.Min(y0, e.Pos.y - sy - half); y1 = Mathf.Max(y1, e.Pos.y + sy + half);
            }
            return x0 <= x1 ? Rect.MinMaxRect(x0, y0, x1, y1) : (Rect?)null;
        }

        // 투사체가 있으면 쏘고 나서 대상에 닿기까지(ms) — 쏘는 순간을 아는 쪽(SD 이벤트)이 타격을 이만큼 늦춘다
        public static int LagMs(string heroKey, UltPick? pick = null) => Plan(heroKey, pick).Any(p => p.At == "move") ? PROJ_LAG : 0;

        // SD 이벤트를 모를 때 본 이펙트가 터지기까지(ms) — 화면이 타격(숫자 · 체력)을 여기에 맞춘다
        public static int ImpactMs(string heroKey) => FxLibrary.HasUlt(heroKey) ? ImpactMs(Plan(heroKey)) : 0;
        public static int ImpactMs(List<UltPart> plan)
        {
            if (plan == null || plan.Count == 0) return 0;
            var pre = plan.Where(p => p.Pre).ToList();
            float t = pre.Count > 0 ? 1000 * Mathf.Min(0.6f, pre.Max(p => p.Dur) * 0.5f) : 0;
            if (plan.Any(p => p.At == "move")) t += PROJ_LAG;
            return Mathf.RoundToInt(t);
        }

        // ── 한 벌 틀기 ── 시각표를 먼저 다 세우고(ms), 전투 시계로 흘려 보내며 튼다
        public static IEnumerator Run(string heroKey, UltOptions o)
        {
            if (BolzenaFx.Calm || !FxLibrary.HasUlt(heroKey)) return Nothing();
            FxLibrary.PreloadUlt(heroKey);
            return RunPlan(Plan(heroKey, o.Pick), o);
        }

        static IEnumerator Nothing() { yield break; }

        // 계획 하나를 시각표대로 — 고학년 · 카드가 같이 쓴다
        public static IEnumerator RunPlan(List<UltPart> plan, UltOptions o)
        {
            if (BolzenaFx.Calm || plan == null || plan.Count == 0) yield break;
            var from = o.From;
            var to = o.To ?? o.From;
            bool flip = to.x < from.x;
            bool sync = o.Impact.HasValue;
            int lag = plan.Any(p => p.At == "move") ? PROJ_LAG : 0;
            float impact = o.Impact ?? 0, endMs = o.End ?? 0;
            float end = sync ? Mathf.Max(1.6f, (Mathf.Max(impact, endMs) + lag) / 1000f + 0.9f)
                             : Mathf.Max(1.6f, ImpactMs(plan) / 1000f + 1.2f);
            var sched = new List<KeyValuePair<float, UltPart>>();
            void Fire(IEnumerable<UltPart> list, float at, float gap) { int i = 0; foreach (var p in list) sched.Add(new KeyValuePair<float, UltPart>(at + i++ * gap, p)); }
            // 시각을 정해 받은 장(원작 영상 맞춤)은 갈래 차례에서 빼고 그 시각에
            foreach (var p in plan) if (p.AtMs.HasValue) sched.Add(new KeyValuePair<float, UltPart>(Mathf.Max(0, p.AtMs.Value), p));
            if (plan.Any(p => p.AtMs.HasValue)) plan = plan.Where(p => !p.AtMs.HasValue).ToList();
            var pre = plan.Where(p => p.Pre).ToList();
            var main = plan.Where(p => !p.Pre && p.At != "move").ToList();
            var proj = plan.Where(p => p.At == "move").ToList();
            var hitMs = o.HitMs != null && o.HitMs.Count > 0 ? o.HitMs : null;
            if (hitMs != null)
            {
                float h0 = hitMs[0], shot = Mathf.Max(0, h0 - lag);
                float Off(UltPart p) => 1000f * FxLibrary.ImpactSec(p.Name);
                void At(UltPart p, float ms) => sched.Add(new KeyValuePair<float, UltPart>(Mathf.Max(0, ms), p));
                if (o.Dash)
                {
                    Fire(pre.Where(p => p.At == "caster"), 0, 60);
                    var near = pre.Where(p => p.At != "caster").ToList();
                    if (near.Count > 0) Fire(near, Mathf.Max(0, shot - 250), 60);
                }
                else Fire(pre, 0, 60);
                Fire(proj, shot, 0);
                foreach (var p in proj.Where(q => q.Rep)) for (int k = 1; k < hitMs.Count; k++) At(p, Mathf.Max(0, hitMs[k] - lag));   // 타격마다 투사체
                Fire(main.Where(p => p.At == "caster" || p.At == "screen"), shot, 0);
                // 번호만 다른 판(슈로 slash_1 · _2 · _3)은 한꺼번에 말고 타격마다 하나씩 차례로(2026-10-06 사용자 「슈로 임팩트 이상」)
                var seq = new HashSet<UltPart>();
                foreach (var g in main.Where(p => p.At == "target").GroupBy(p => SEQ_TAIL.Replace(Word(p.Name), "")))
                {
                    var l = g.OrderBy(p => p.Name).ToList();
                    if (l.Count < 2 || hitMs.Count < 2) continue;
                    for (int j = 0; j < l.Count; j++) { int hi = Mathf.RoundToInt(j * (hitMs.Count - 1f) / (l.Count - 1)); At(l[j], hitMs[hi] - Off(l[j])); seq.Add(l[j]); if (o.SeqPerTarget) o.SeqIndex[l[j]] = j; }
                }
                foreach (var p in main.Where(p => p.At == "target" && !seq.Contains(p))) At(p, h0 - Off(p) - (LEAD_MS.TryGetValue(p.Name, out var ld) ? ld : 0));   // 터지는 때가 첫 타격에(조준선처럼 먼저 떠야 하는 장은 LEAD_MS 만큼 앞서)
                foreach (var p in main.Where(q => q.Rep && !seq.Contains(q)))   // 타격마다 조준선 · 맞음
                    for (int k = 1; k < hitMs.Count; k++)
                        At(p, p.At == "target" ? hitMs[k] - Off(p) - (LEAD_MS.TryGetValue(p.Name, out var ld2) ? ld2 : 0) : hitMs[k]);
                foreach (var p in sched.Select(s => s.Value).Where(q => q.Rep && q.AtMs.HasValue).Distinct().ToList())   // (10-18) 이벤트 시각을 받은 장도 타격마다(우이 스킬 빛 비 4번)
                    for (int k = 1; k < hitMs.Count; k++) At(p, p.At == "target" ? hitMs[k] - Off(p) : hitMs[k]);
                var again = main.Where(p => p.Again && !p.Rep && !seq.Contains(p)).ToList();
                if (again.Count > 0 && hitMs.Count > 1)
                {
                    float gap = Mathf.Max(400, 700 * again.Max(p => p.Dur));
                    float last = h0;
                    for (int k = 1; k < hitMs.Count; k++)
                    {
                        if (hitMs[k] - last < gap && k < hitMs.Count - 1) continue;   // 몰아치면 몇 번 건너뛰되, 마지막 타격에는 꼭
                        foreach (var p in again) At(p, p.At == "target" ? hitMs[k] - Off(p) : hitMs[k] - lag);
                        last = hitMs[k];
                    }
                }
                end = Mathf.Max(1.6f, hitMs[hitMs.Count - 1] / 1000f + 0.9f);
            }
            else if (sync)
            {
                if (o.Dash)
                {
                    Fire(pre.Where(p => p.At == "caster"), 0, 60);
                    var near = pre.Where(p => p.At != "caster").ToList();
                    if (near.Count > 0) Fire(near, Mathf.Max(0, impact - 250), 60);
                }
                else Fire(pre, 0, 60);
                Fire(proj, impact, 80);
                Fire(main.Where(p => p.At == "caster" || p.At == "screen"), impact, 60);
                Fire(main.Where(p => p.At == "target"), impact + lag, 90);
                var again = main.Where(p => p.Again).ToList();
                if (again.Count > 0 && endMs > impact && o.Marks != null)
                {
                    float gap = Mathf.Max(500, 800 * again.Max(p => p.Dur));
                    float next = impact + gap;
                    foreach (var m in o.Marks)
                    {
                        if (m < next || m > endMs) continue;
                        Fire(again, m + (again.Any(p => p.At == "target") ? lag : 0), 60);
                        next = m + gap;
                    }
                }
            }
            else
            {
                float at = 0;
                if (pre.Count > 0) { Fire(pre, 0, 60); at += 1000 * Mathf.Min(0.6f, pre.Max(p => p.Dur) * 0.5f); }
                if (proj.Count > 0) { Fire(proj, at, 80); at += PROJ_LAG; }
                Fire(main.Where(p => p.At == "caster" || p.At == "screen"), at, 60);
                Fire(main.Where(p => p.At == "target"), at, 90);
            }
            sched.Sort((a, b) => a.Key.CompareTo(b.Key));
            var runs = new List<KeyValuePair<FxRun, int>>();   // 무대 · 그때의 세대(풀에서 되쓰이면 세대가 바뀐다)
            float now = 0;       // ms
            int next2 = 0;
            while (true)
            {
                while (next2 < sched.Count && sched[next2].Key <= now)
                {
                    o.OnFire?.Invoke(sched[next2].Value, now);
                    // 장은 제 길이(원작 프리팹 dur)만큼은 둔다 — 「마지막 타격 + 0.9초」 에서 끊던 것이 원작의 남는 덮개 · 장판을 잘랐다
                    //   (2026-10-14 원작 대조: 요미 달빛 4.5초 장이 2.5초에 · 클로에 · 실비아 짧음). 너무 긴 장은 DUR_KEEP_MAX 까지
                    var part = sched[next2].Value;
                    // 원작 파티클(UsePs)은 장마다 제 수명이 있다 — 「끝까지(end - now)」로 붙잡으면 막 · 돔 · 구가 고학년 끝까지 화면을 덮었다
                    //   (10-17 전수조사: 요미 · 이드 · 우이 · 클로에 · 티그 · 디아나 · 비비(신성)). 고학년(KeepMax = DUR_KEEP_MAX)은 제 길이(최대 DUR_KEEP_MAX)만
                    float keep = BolzenaFx.UsePs && o.KeepMax >= DUR_KEEP_MAX
                        ? Mathf.Max(FxRun.FADE, Mathf.Min(part.Dur, DUR_KEEP_MAX))
                        : Mathf.Max(FxRun.FADE, Mathf.Max(end - now / 1000f, Mathf.Min(part.Dur, KEEP_FULL.Contains(part.Name) ? DUR_KEEP_MAX : o.KeepMax)));
                    if (part.Cap > 0) keep = Mathf.Min(keep, part.Cap);
                    var r = FirePart(part, o, from, to, flip, keep);
                    if (r != null) o.OnPart?.Invoke(r);
                    if (r != null) runs.Add(new KeyValuePair<FxRun, int>(r, r.Gen));
                    next2++;
                }
                if (next2 >= sched.Count && runs.All(r => r.Key == null || r.Key.Gen != r.Value || r.Key.IsDone)) yield break;
                yield return null;
                now += 1000f * Mathf.Min(0.1f, Mathf.Max(0, BolzenaFx.DeltaTime())) * BolzenaFx.Rate;
                if (o.Clock != null && o.Clock.Ms > now) now = o.Clock.Ms;   // 전투 시계보다 뒤처지지 않게(긴 프레임 · 멈칫 뒤)
            }
        }

        // 뼈 찾기 사슬 — ① 이름 낱말(한 낱말 · 두 낱말 붙임)이 시전자 자리 본(Point_LeftEye · Wind · Doll …)이면 그것 ② 뜻 낱말 → 그 뼈들
        // ③ (시전자 쪽만) 이 동작의 시전 본(Point_Ult1 · Attack1 · Skill1). 없으면 null(발밑)
        static readonly HashSet<string> NOT_BONE = new HashSet<string> { "top", "bottom", "middle", "front", "back", "shadow", "step", "fixtop", "ultimate", "skill", "attack",
            "hit", "fx", "start", "end", "loop", "cast", "ground", "explosion", "default", "smoke", "proj", "slash", "impact", "area", "field", "aura", "buff", "heal" };
        static readonly (Regex re, string[] pts)[] MEAN =
        {
            (new Regex("(cast|ready|charge)"), new[] { "SkillReady", "Cast", "Charge" }),
            (new Regex("(light|laser|lazer|beam)"), new[] { "UltLaser", "Light", "Laser" }),
            (new Regex("(shot|muzzle|bullet|gun)"), new[] { "Ult_Shot", "Attack1_Shot", "Muzzle", "Gun", "Shot" }),
            (new Regex("(hand|punch|fist)"), new[] { "Hand", "Punch", "Fist" }),
        };
        static Func<Vector3?> CasterBone(UltPart p, UltOptions o, bool caster)
        {
            var a = o.CasterActor;
            if (a?.Point == null) return null;
            Func<Vector3?> Of(string n) => a.Point(n).HasValue ? () => a.Point(n) : (Func<Vector3?>)null;
            var toks = Word(p.Name).Split('_').Where(t => t.Length >= 3 && !t.All(char.IsDigit)).Select(t => t.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9')).ToList();
            for (int i = 0; i + 1 < toks.Count; i++) { var f = Of(toks[i] + toks[i + 1]); if (f != null) return f; }
            foreach (var t in toks) if (t.Length >= 3 && !NOT_BONE.Contains(t)) { var f = Of(t); if (f != null) return f; }
            if (!caster) return null;
            var w = Word(p.Name);
            foreach (var (re, pts) in MEAN) if (re.IsMatch(w)) foreach (var n in pts) { var f = Of(n); if (f != null) return f; }
            // (동작 시전 본으로 붙이던 것은 뺐다 — 슈로 Point_Ult1 이 적 앞 고정 점이라 오라가 적 쪽에 떴다. 뜻 낱말이 없으면 발밑을 따라간다)
            return null;
        }

        static FxRun FirePart(UltPart p, UltOptions o, Vector3 from, Vector3 to, bool flip, float until)
        {
            var b = new FxPlayOptions { Scale = o.Scale * (p.Scale > 0 ? p.Scale : 1f), Top = o.Top, Flip = flip, Until = until, Order = o.Order, SortingLayer = o.SortingLayer, Parent = o.Parent, FitTop = o.FitTop };
            // 맞는 적의 방향으로 띄우는 장(원작은 적이 보는 쪽 = 시전자와 반대) — 에르핀(왕도) 부채꼴 충격파는 장 안에서 −x(시전자 쪽)로 펼쳐지게 짜였다(10-17 사용자 「적 쪽으로 부채꼴이 생겨야 하는데 반대」)
            if (BolzenaFx.UsePs && TARGET_FACING.IsMatch(p.Name)) b.Flip = !flip;
            // 몸 뒤에 깔리는 바닥 장(공용 규칙의 「뒤」 순서 20) — 우로스 스킬 영역의 바닥 빛이 우로스를 가렸다(10-17 사용자)
            if (GROUND_BACK.IsMatch(p.Name)) b.Order = 20;
            // 시전자를 앞에서 감싸는 장(원작 레이어 3 이상) — 고학년 장 순서(325)가 사도(330) 밑이라 시온 뒤에만 그려졌다(10-17 3차 사용자 「감싸게」). 바닥 레이어 이미터(−1000)는 그대로 뒤
            if (BolzenaFx.UsePs && FRONT_OF_CASTER.IsMatch(p.Name)) b.Order = o.Order + 10;
            // 요미 고학년 달빛 원판 · 띠 — 요미 뒤 아래에서 생겨 0.9초 뒤(원작 2.0초)부터 2.5초 동안 적 너머로 쓸고 나간다(10-17 3차 사용자 「달빛이 요미에게 고정」 · 원작 영상 분석)
            if (BolzenaFx.UsePs && YOMI_SWEEP.IsMatch(p.Name))
            {
                float dir = to.x >= from.x ? 1 : -1;
                b.At = new Vector3(from.x - dir * 1f, from.y, from.z); b.MoveTo = new Vector3(to.x + dir * (Mathf.Max(1.2f, o.ToWidth / 2) + 1.5f), from.y, from.z);
                b.MoveDelay = 0.9f; b.MoveDur = 2.5f; b.Aim = false; b.Bare = true; b.CarryParticles = true;   // 원점만 밀리고 이미 나온 달빛은 그 자리에 남았다(재검토)
                return BolzenaFx.Play(p.Name, b);
            }
            // 원작 파티클(UsePs)은 차원문 레이저 장을 원작 그대로(1.35배 · 뒤집기 보정 없이) 적 자리에 — 10-17 사용자 「리뉴아 강공 레이저 인게임 것 아니고 돌아간다」
            if (BolzenaFx.UsePs && (PORTAL.ContainsKey(p.Name) || PORTAL_CAST.ContainsKey(p.Name))) { b.At = PORTAL.ContainsKey(p.Name) ? to : from; if (PORTAL.ContainsKey(p.Name)) b.Flip = !flip; b.Bare = true; return BolzenaFx.Play(p.Name, b); }   // (3차 조사) 판은 적 자리 원점에서 +x 위로 뻗는다 — 뒤집어야 왼쪽 위 문에서 적에게 내리꽂힌다
            if (PORTAL.ContainsKey(p.Name)) { b.At = to; b.Center = false; b.Aim = false; b.Bare = true; b.Flip = !flip; b.Scale *= PORTAL_SCALE; return BolzenaFx.Play(p.Name, b); }   // 판 안 고리(+x)가 시전자 쪽(왼쪽 위)으로 — 뒤집기를 거꾸로
            if (PORTAL_CAST.TryGetValue(p.Name, out var beam) && PORTAL.TryGetValue(beam, out var ring))
            {
                b.Flip = !flip; float kw = BolzenaFx.UnitWorld * b.Scale * PORTAL_SCALE, side = from.x <= to.x ? -1 : 1;
                b.At = to + new Vector3(side * ring.x * kw, ring.y * kw, 0); b.Center = false; b.Bare = true;   // 고리는 적에서 시전자 쪽 위
                return BolzenaFx.Play(p.Name, b);
            }
            if (p.At == "floor") { b.At = new Vector3(to.x, from.y, to.z); b.Center = true; if (o.ToWidth > 0) b.SpreadWidth = o.ToWidth; return BolzenaFx.Play(p.Name, b); }   // 바닥 장 — 가로는 대상, 높이는 시전자 발(10-18 시온 마지막 불길: 원작은 시온 발밑에서 바닥 따라 번진다)
            if (p.At == "midx") { b.At = new Vector3((from.x + to.x) * 0.5f, from.y, from.z); b.Flip = false; b.Bare = true; return BolzenaFx.Play(p.Name, b); }   // 화면 가운데(시전자 · 적 무리 한가운데) — 란 달 · X(장 원점이 화면 가운데로 짜였다, fix_Y 0-3)
            if (p.Name.Contains("chloe_ultimate_proj_1"))   // 쁘띠 세바스티안 — 화면 왼쪽 끝에서 땅을 달려 적 발밑으로(원작 1.3~2.1초, fix_Y 4)
            {
                float dir = to.x >= from.x ? 1 : -1;
                b.At = new Vector3(from.x - dir * 4f, to.y, to.z); b.MoveTo = new Vector3(to.x, to.y, to.z);
                b.MoveDur = 0.8f; b.Aim = false; b.Scale *= 0.6f; b.Until = Mathf.Max(b.Until, 1.2f);
                return BolzenaFx.Play(p.Name, b);
            }
            if (p.At == "move") { b.At = (o.Muzzle != null ? o.Muzzle() : null) ?? o.ShotFrom ?? from;   // 쏘는 그 순간의 총구 · 쏘는 자리(캬롯 — 몸 가운데에서 나가던 것)
b.MoveTo = o.ToBody ?? to; b.MoveDur = 0.35f; b.Aim = o.Aim && !p.Name.Contains("trail");   // 꼬리(trail)는 제 모양대로
                if (p.Name.Contains("meteor")) { b.At = (o.ToBody ?? to) + new Vector3(-2.5f, 7f, 0); b.MoveDur = 0.45f; }   // 운석(아일라) — 하늘에서 떨어진다
                if (p.Name.Contains("aya_skill1_proj_1"))   // (10-17 3차 사용자 「나비가 상대 쪽으로 쭉 가야 하는데 위로 올라감」) 몸 높이 가로로 적 너머(키 3~4)까지 갔다가 돌아온다 — 돌리지 않는다(원작 판은 가로로 짜였다)
                {
                    // (10-18 2차 사용자 「위에서 던져 가파름 · 빠름 · 짧음」) 원작 — 적 몸 높이로 수평, 0.31초에 첫 적 · 화면 밖 오른쪽까지 · 몸짓 4.96초에 돌아오며 적 · 0.6초 뒤 아야.
                    //   화면 밖에 있는 동안은 기다림(원작도 그동안 나비가 화면 밖). 전엔 공중 몸(머리 높이)에서 비스듬히 · 적 너머 2.5 · 0.6/0.3/0.6초
                    float dir = to.x >= from.x ? 1 : -1; var bodyFrom = o.CasterActor != null ? o.CasterActor.At(FxAnchor.Body) : from; var tb = o.ToBody ?? to;
                    var cam = Camera.main; float edge;
                    if (cam != null)
                    {
                        var ew = new Vector3(cam.transform.position.x + dir * (cam.orthographicSize * cam.aspect + 1.5f), 0, 0);   // 화면 밖(월드) → 싸움터 좌표
                        edge = (o.Parent != null ? o.Parent.InverseTransformPoint(ew) : ew).x;
                    }
                    else edge = tb.x + dir * 8f;
                    var s0 = new Vector3(bodyFrom.x + dir * 0.3f, tb.y, tb.z);
                    float near = Mathf.Max(0.5f, Mathf.Abs(tb.x - s0.x));
                    float vOut = near / 0.31f, vBack = near / AYA_BACK;                    // 나갈 때 원작 속도 · 돌아올 때 (10-18 4차) 0.6 → 0.35초 거리(빠르게)
                    float outDur = Mathf.Abs(edge - s0.x) / vOut, backDur = Mathf.Abs(edge - s0.x) / vBack;
                    //   돌아오며 적에 닿는 때(쏜 때부터) — 전투가 준 마지막 맞음(HitMs, 이펙트 시계 · 배속 반영)에, 없으면 3.2초(10-18 4차 — 원작 4.96초보다 빠르게, Cards CARD_HITS {1610, 4480}). 쏘기는 첫 맞음 − PROJ_LAG
                    float back0 = o.HitMs != null && o.HitMs.Count > 1 ? (o.HitMs[o.HitMs.Count - 1] - o.HitMs[0] + PROJ_LAG) / 1000f : 3.2f;
                    float backHit = back0 - Mathf.Abs(edge - tb.x) / vBack;                // 그때가 되게 화면 밖에서 기다린다
                    b.At = s0; b.MoveTo = new Vector3(edge, tb.y, tb.z);
                    b.Aim = false; b.MoveLinear = true; b.MoveDur = outDur; b.MoveBack = Mathf.Max(0.1f, backHit - outDur); b.MoveBackDur = backDur;
                    b.Until = Mathf.Max(b.Until, outDur + b.MoveBack + backDur + 0.2f);
                }
                if (p.Name.Contains("yomi_ultimate_proj_1")) { float dir = to.x >= from.x ? 1 : -1; b.At = new Vector3(from.x - dir * 2.5f, from.y, from.z); b.MoveTo = new Vector3(to.x + dir * (Mathf.Max(1.2f, o.ToWidth / 2) + 3f), from.y, from.z); b.MoveDur = 1.1f; b.Aim = false; }   // (10-17 사용자 「달빛이 요미 뒤에서 앞으로 가며 적 쓸어버림」) 원작은 달빛 띠 장을 통째로 민다
                if (p.Name.Contains("yomi_attack2_proj")) { float dir = to.x >= from.x ? 1 : -1; float run = Mathf.Abs(to.x - from.x) + 0.5f; b.At = new Vector3(to.x - dir * run, to.y + run * 0.9f, to.z); b.MoveTo = to; b.MoveDur = 0.24f; b.Aim = true; b.AimAt = null; }   // (10-18 2차 사용자 「작은 별빛 · 긁힌 자국」) 원작 혜성 — 요미 뒤 위에서 약 42° 로 적 발밑에, 0.24초. 판을 눕혀(FxRun.Z_FWD) 떨어지는 쪽으로 돌린다(전엔 −1.5, 6 에서 76° · 돌리지 않음)   // (재검토) 돌리면 운석이 위로 올라가 보였다 — 판은 떨어지게 짜였다   // (10-17 사용자 「작은 별빛 운석이 떨어져야」) 적 위 하늘에서 비스듬히 떨어진다 — 전엔 시전자 머리 위에서 0.25초 직선   // 요미 강공 — 뛰어오른 공중(시전자 위)에서 적 발밑으로 비스듬히 꽂힌다(원작 대조 10-15)
                // (10-18 3차 리뉴아 평타 「탄 작음 · 폭발 없음」) 탄 크기는 원작과 비슷했다(cap_fix3 seq 1.30~1.61초 적 몸 폭만 한 초록 렌즈 — 검증 칸 0.7~1.2초는 쏘기 전).
                //   어긋난 것은 때: 원작 기본공격.mp4 31~33칸(0.1초 ≈ 몸짓 0.12초)에 고리에서 적 몸으로 꽂히고 34칸에 폭발 · 숫자, 렌즈는 사라진다.
                //   우리는 0.35초(+ 줄인 몸짓)에 걸쳐 1.78초에야 닿고, 닿은 뒤에도 적 위에 2.2초 넘게 남았다 → 0.12초에 닿고 닿으면 걷는다(맞음 장 · 타격은 FxRules.SHOT_LAG_MS 120 · Cards CARD_HITS 1420)
                if (p.Name.Contains("renewa_attack_proj_1")) { b.MoveDur = 0.12f; b.Until = Mathf.Min(b.Until, 0.15f); }
                if (p.Name.Contains("epica_attack_proj_2"))   // 에피카 강공 불 에피콘 — (10-18) 원작은 오른손에서 허리 높이로 수평으로 날아간다(돌지 않음, 0.4~0.5초 · 기본공격 영상 3.45~3.9). 전엔 판을 적 쪽으로 돌려 꼬리만 비스듬한 줄로 보였다
                {
                    var s = o.CasterActor != null ? o.CasterActor.At(FxAnchor.Body) : b.At; float dir = to.x >= from.x ? 1 : -1;
                    b.Aim = false; b.AimAt = null; b.MoveDur = 0.5f; b.Until = Mathf.Max(b.Until, 0.7f);
                    // (10-18 검증_1) 판은 FxRun.Z_FWD 로 눕혀 가로로 — 에피콘 그림(이미터 z −1.04)이 원점보다 그만큼 앞에 서므로 원점을 뒤로 물려 손에서 나가 적 몸에 닿게
                    var core = BolzenaFx.UsePs ? new Vector3(dir * 1.04f * BolzenaFx.UnitWorld * b.Scale, 0, 0) : Vector3.zero;
                    b.At = s + new Vector3(dir * 0.4f, 0, 0) - core; b.MoveTo = new Vector3((o.ToBody ?? to).x, s.y, s.z) - core;
                }
                if (p.Name.Contains("vividivine_skill_1_proj")) { b.At = o.CasterActor != null ? o.CasterActor.At(FxAnchor.Body) : from; b.MoveDur = 0.6f; b.MoveArc = (o.CasterActor != null ? o.CasterActor.Height : 2.2f) * 1.5f; b.Aim = false; }   // 비비(신성) 스킬 씨앗 — 포물선(fix_Y 8)
                return BolzenaFx.Play(p.Name, b); }
            if (p.Ally && o.Allies != null && o.Allies.Count > 0)
            {
                b.At = o.CasterActor != null ? o.CasterActor.At(FxAnchor.Feet) : from; b.Flip = false;   // 달려가는 고학년도 시전자 실제 몸 발밑(Bottom 본) — from 은 달려갈 자리라 적 앞에 떴다
                bool alliesOnly = ALLIES_ONLY.Contains(p.Name);   // 시전자에게는 안 트는 장(실비아 초대장 — 원작 실비아 머리 위엔 없다)
                var first = alliesOnly ? null : BolzenaFx.Play(p.Name, b);
                foreach (var a in o.Allies)
                {
                    var r = BolzenaFx.Play(p.Name, new FxPlayOptions { At = a(), Scale = o.Scale, Top = o.Top, Until = until, Order = o.Order, SortingLayer = o.SortingLayer, Parent = o.Parent });
                    if (alliesOnly && r != null && first == null) { first = r; continue; }   // 돌려준 것은 부른 쪽이 OnPart 를 부른다
                    if (r != null) o.OnPart?.Invoke(r);
                }
                return first;
            }
            if (p.At == "screen") { b.At = BolzenaFx.ScreenCenter(); b.Flip = false; b.Top = null; return BolzenaFx.Play(p.Name, b); }
            // 크게 터지는 장 — 원작은 화면 오른쪽(적 무리 전체)을 덮는다(엘레나 마지막 드론 폭발)
            foreach (var bs in BIG)
                if (p.Name.Contains(bs.Key)) { b.Scale *= bs.Value; b.At = to; b.Center = true; b.SpreadWidth = o.ToWidth; return BolzenaFx.Play(p.Name, b); }
            // 쓸고 가는 장 — 원작은 사도 쪽에서 나와 적들을 가로질러 오른쪽으로 빠진다(로네(시장) 군중 · 샤샤 물줄기). 원작 영상 비교 2026-10-06
            // 원작 파티클(UsePs)은 쓸고 가는 길이 장 안에 짜여 있다(우로스 뱀: 이미터가 적 자리 원점 기준 −24.6 ~ +49.5) — 자리 · 밀기를 바꾸지 않는다(10-16)
            if (!BolzenaFx.UsePs) foreach (var sw in SWEEP)
                if (p.Name.Contains(sw.Key))
                {
                    b.At = new Vector3(from.x + 0.8f, to.y, to.z);
                    // 원작 파티클(UsePs)은 입자가 스스로 오른쪽으로 날아간다 — 장까지 밀면 두 배로 날아 0.6초 만에 화면 밖(10-16 우로스 금색 뱀 · 에르핀 충격파)
                    if (!BolzenaFx.UsePs) { b.MoveTo = new Vector3(to.x + Mathf.Max(1.2f, o.ToWidth / 2) + 1.5f, to.y, to.z); b.MoveDur = sw.Value; }
                    return BolzenaFx.Play(p.Name, b);
                }
            // 몸을 덮는 섬광(BODY_FLASH) — 달려가는 사도도 그 순간의 몸에(시전자 자리를 도착점으로 바꾼 FxActor 라도 본은 실제 몸)
            if (p.At == "caster" && o.CasterActor != null)
                foreach (var bf in BODY_FLASH)
                    if (p.Name.Contains(bf.Key))
                    {
                        var ca = o.CasterActor;
                        Func<Vector3?> mid = () => ca.Bone("Middle") ?? ca.Guess(FxAnchor.Body);
                        b.At = mid().Value; b.Track = mid; b.Bare = true; b.Scale *= bf.Value;
                        return BolzenaFx.Play(p.Name, b);
                    }
            // 뼈에 붙이면 엉뚱한 데로 가는 장 — 몸 앞(가슴 높이)에 그대로(비비(신성) 거대 형상의 레이저: 원작은 손끝에서 적 쪽으로 가로)
            if (NO_BONE.IsMatch(p.Name) && o.CasterActor != null) { b.At = o.CasterActor.At(FxAnchor.Front); b.Bare = true; return BolzenaFx.Play(p.Name, b); }
            // 바닥에 깔려 고정되는 장 — 시전 뼈(떠오르는 몸)에 붙이지 않고 발밑(10-17 사용자 「이드 스킬 장판은 바닥 고정, 이드가 날아오르며 보호막」)
            if (BolzenaFx.UsePs && p.Name.Contains("uros_attack2_snake")) { b.At = from; return BolzenaFx.Play(p.Name, b); }   // (재검토) 카드 시작 때 시전자 발밑 — 돌진 뒤 발밑을 읽어 뱀 머리가 화면 밖으로 나갔다
            // (10-18 사용자 4차 「네르 고학년 붉은 임팩트가 보석이 아니라 머리 위」) 모으기 장의 보석 빛(red_1/2 · DD_Glow)은 장 안 (−1.7, 5.02)(기울기 반영).
            //   발밑에 두면 머리 위 1.5~1.9배 높이에 떴다 — 그 자리가 무기 보석(Point_Weapon, 없으면 몸 가운데)에 오게 원점을 빼고 따라간다
            if (BolzenaFx.UsePs && p.Name == "fx_nerrage_ultimate_charge" && o.CasterActor != null)
            {
                var ca = o.CasterActor; float kk = BolzenaFx.UnitWorld * b.Scale;
                var off = new Vector3((flip ? 1.7f : -1.7f) * kk, 5.02f * kk, 0);
                Func<Vector3?> at = () => (ca.Bone("Weapon") ?? ca.At(FxAnchor.Body)) - off;
                b.At = at().Value; b.Track = at; b.Bare = true;
                return BolzenaFx.Play(p.Name, b);
            }
            // (10-18 사용자 「쉴드가 이드에게 안 씌워짐」) 이드 고학년 행성 고리 구 — 장 안 구 가운데(원작 단위 −20.6, 11.3 · 기울기 반영)가 이드 몸 가운데(달)에 오게, 매 프레임 따라감.
            //   전엔 FxRun.PS_SHIFT 로 x 만 6.1 당겨 구가 이드 왼쪽 위에 떴다
            if (BolzenaFx.UsePs && p.Name == "fx_ed_ultimate_ground" && o.CasterActor != null)
            {
                var ca = o.CasterActor; float kk = BolzenaFx.UnitWorld * b.Scale;
                var off = new Vector3((flip ? 20.6f : -20.6f) * kk, 11.3f * kk, 0);
                Func<Vector3?> at = () => ca.At(FxAnchor.Body) - off;
                b.At = at().Value; b.Track = at; b.Bare = true;
                return BolzenaFx.Play(p.Name, b);
            }
            // (10-18 사용자 4차 「우로스 원 베기는 적 전체 — 너무 뒤에서 돈다 · 기둥은 원 가운데」) 베기 원 가운데가 장 안 −10.4(뒤집은 뒤 앞 +10.4).
            //   원작은 적 뒤로 순간이동한 자리에서 튼다 — 제자리인 우리는 원 가운데가 적 무리 가운데(기둥 snake_jolly 자리 = to)에 오게 원점을 그만큼 시전자 쪽으로 당긴다
            if (BolzenaFx.UsePs && p.Name.StartsWith("fx_uros_ultimate_slash_jolly"))
            {
                float kk = BolzenaFx.UnitWorld * b.Scale, dir = to.x >= from.x ? 1f : -1f;
                b.At = new Vector3(to.x - dir * 10.4f * kk, to.y, to.z); b.Bare = true;
                return BolzenaFx.Play(p.Name, b);
            }
            // (10-18 사용자 2차) 디아나(왕년) 고학년 끌기 불똥 · 균열 — 장 원점이 내리찍는 자리, 끌린 자국이 그 뒤(−x)로 짜였다. 시전 뼈(아직 제자리)에 붙이면 떠난 자리 뒤에 뿌려졌다.
            //   내리찍을 자리 = 돌진 도착점(OrigFx 가 시전자 발을 dashTo 로 바꿔 둔 CasterActor.Feet), 따라가지 않게
            if (BolzenaFx.UsePs && (p.Name == "fx_dianayester_ultimate_cast_1" || p.Name == "fx_dianayester_ultimate_cast_2") && o.CasterActor != null)
            { b.At = o.CasterActor.Feet; b.Bare = false; return BolzenaFx.Play(p.Name, b); }
            if (FEET_FIXED.IsMatch(p.Name) && o.CasterActor != null) { b.At = o.CasterActor.At(FxAnchor.Feet); b.Bare = true; return BolzenaFx.Play(p.Name, b); }
            // (10-18 4차 사용자 「깃발 꽂는 곳 ≠ 대지 깨지는 곳」) 네르(빡침) 스킬 빛기둥 · 균열 · 바닥 고리 · 맞음 — 원작은 깃발 밑(저학년.mp4 1.2~2.2초).
            //   깃발(Pets.cs DROPS: 발에서 앞으로 키 × NER_FLAG_FWD)과 같은 자리에 고정. 전엔 시전자 발 뼈를 따라가 깃발 왼쪽 · 돌아가는 네르 발밑에 섰다
            if (NER_FLAG.IsMatch(p.Name) && o.CasterActor != null)
            {
                var a = o.CasterActor;
                b.At = a.Feet + new Vector3((a.Party ? 1 : -1) * NER_FLAG_FWD * a.Height, 0, 0); b.Bare = true;   // Track 없음 — 꽂힌 자리에 가만히
                return BolzenaFx.Play(p.Name, b);
            }
            // (10-18 4차 사용자 「개울 장판은 우이에게」) 우이 스킬 대지 — 장 안 클로버 · 줄기(x −11.9, y 5.8)가 우이 머리 위에 오도록 원점을 우이 앞 11.9(원작 단위)에. 전엔 대상 발밑(CARD_AT target)이라 웅덩이가 적에게 섰다
            if (p.Name == "fx_uieru_skill_ground_1" && o.CasterActor != null)
            {
                float dir = to.x >= from.x ? 1 : -1;
                b.At = o.CasterActor.Feet + new Vector3(dir * 11.9f * BolzenaFx.UnitWorld * b.Scale, 0, 0); b.Bare = true;   // 따라가지 않는다
                return BolzenaFx.Play(p.Name, b);
            }
            Vector3? m = p.Muzzle && o.Muzzle != null ? o.Muzzle() : null;
            if (m.HasValue)
            {
                b.At = m.Value; b.Track = o.Muzzle; b.Bare = true;
                // 카드 — 총구 레이저 · 빛줄기를 맞는 적 몸 쪽으로 돌린다(무기 끝이 머리 위면 오른쪽 위 허공으로 뻗던 것 · 리뉴아 평타 2026-10-13)
                if (o.Aim) { b.Aim = true; b.AimAt = o.ToBody ?? to; }
                // (10-18 검증_1) 이드 강공 레이저 — 나타가 달 위에 떠 적 몸을 겨누면 60°(AIM_MAX)까지 꺾여 내리꽂혔다. 원작은 거의 수평(살짝 위) — 나타 높이 그대로 적 쪽을 겨눈다(AIM_BASE −22° 보정은 그대로)
                if (o.Aim && p.Name == "fx_ed_attack_2") b.AimAt = new Vector3((o.ToBody ?? to).x, m.Value.y, m.Value.z);
                if (p.Name == "fx_ed_attack_2") b.Track = null;   // (10-18 4차 사용자 「강공 임팩트 불안정」) 굵은 레이저는 쏜 자리에 고정 — 떠다니는 나타를 따라가며 끝이 흔들렸다
                return BolzenaFx.Play(p.Name, b);
            }
            // 시전자 쪽 · 이름에 시전자 뼈가 든 대상 쪽 장 — 그 뼈에 붙여 따라간다(전수 조사 B · C · G). 이미 높이 짜인 판(이미터 3 단위 넘게 위)은 발밑 그대로
            if (o.CasterActor != null && FxLibrary.EmitterTop(p.Name) < 3f)
            {
                var bone = CasterBone(p, o, p.At == "caster");
                var topY = BolzenaFx.TopY();   // 화면 위로 나간 뼈(네르(빡침) SkillReady)는 안 쓴다
                if (bone != null) { var at0 = bone(); if (at0.HasValue && (!topY.HasValue || at0.Value.y < topY.Value)) { b.At = at0.Value; b.Track = bone; b.Bare = true; return BolzenaFx.Play(p.Name, b); } }
            }
            // 시전자 쪽 장은 시전자 실제 몸 발밑(Bottom 본 — 매 프레임 따라감). 전에는 달려가는 몸짓(Dash) · 높이 짜인 판(이미터 3 단위 넘게 위)이면 from(달려갈 자리 = 적 앞)에 깔려
            //   몸이 아직 제자리인 모으기 기운이 적 앞에 떴다(2026-10-14 디아나(왕년) 고학년 0.3~1.5초). 뒤로 멀리 짜인 모으기 판(cast_1 이미터 x −7)은 몸에 가운데 맞춘다
            if (p.At == "caster" && o.CasterActor != null)
            {
                var a = o.CasterActor;
                Func<Vector3?> feet = () => a.At(p.Body ? FxAnchor.Body : FxAnchor.Feet);   // p.Body = 시전자 몸 가운데(CARD_AT caster_body)
                b.At = feet().Value; b.Track = feet; b.Center = p.Pre;
                if (BODY_FOLLOW.IsMatch(p.Name)) b.Bare = true;   // (10-18 4차 사용자 「구가 하늘에서 생기고 안 내려옴」) 원작 파티클은 Bare 여야 매 프레임 따라간다(FxRun Track 조건) — 내려앉는 비비 몸 가운데를 따라
                if (p.Body && BODY_AIM.IsMatch(p.Name)) { b.Aim = true; b.AimAt = o.ToBody ?? to; }   // 몸 높이에서 맞는 적 몸 쪽으로 — 가로 레이저(이드)
                return BolzenaFx.Play(p.Name, b);
            }
            b.At = p.At == "caster" ? from : p.Body && o.ToBody.HasValue ? o.ToBody.Value : to;
            b.Center = p.At == "target" && !o.Dash && !p.Body;
            // (10-18 사용자 2차 「얼음 씨앗이 좀 더 대상 머리 위에서」) 원작 1.5~1.8초 — 씨앗 가운데가 앞 적 발에서 키의 1.7배(머리 위)에 떠 있다가 떨어진다.
            //   장 안 씨앗은 원점 위 4.0(−20° 눕힘 → 3.76) — 원점을 그만큼 내려 씨앗이 그 높이에 오게(적 하나여도 이 길)
            if (p.Name.Contains("aya_ultimate_seed_1") && o.TargetFeet != null && o.TargetTops != null && o.TargetFeet.Count > 0 && o.TargetTops.Count == o.TargetFeet.Count)
            {
                int j = o.SeqIndex.TryGetValue(p, out var sj) ? sj : 0; j %= o.TargetFeet.Count;
                var ft = o.TargetFeet[j]; float h = o.TargetTops[j].y - ft.y;
                b.At = new Vector3(ft.x, ft.y + 1.7f * h - 3.76f * BolzenaFx.UnitWorld * b.Scale, ft.z); b.Center = false;
                return BolzenaFx.Play(p.Name, b);
            }
            // 번호 장을 적마다 차례로(아야 연꽃 — 앞 적 먼저, 다음은 뒤 적) · 번호 없는 대상 장은 맨 앞 적에(아네트 금지 표지)
            if (o.SeqPerTarget && p.At == "target" && o.TargetFeet != null && o.TargetFeet.Count > 1)
            {
                int j = o.SeqIndex.TryGetValue(p, out var sj) ? sj : 0;
                b.At = o.TargetFeet[j % o.TargetFeet.Count]; b.Center = true;
                return BolzenaFx.Play(p.Name, b);
            }
            if (p.At == "target" && o.ToWidth > 0) b.SpreadWidth = o.ToWidth;
            return BolzenaFx.Play(p.Name, b);
        }
    }
}
