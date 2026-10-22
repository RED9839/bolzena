using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using Spine;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 카드 몸짓(평타 Attack1 · 강공 Attack2 · 저학년 Skill1)을 원작 영상에 맞춘다(2026-10-06 카드 몸짓 대조 135명 · card_sheets/_review_1~4).
    //   · 몸짓을 「마지막 타격 + 0.32초」에서 끊지 않고 애니 끝까지 튼다(속도 1.3배 그대로). 길면 상한까지 앞 준비 · 빈 구간 · 꼬리부터 줄인다
    //   · 이어지는 조각(_Loop · _End · _2 …)을 고학년처럼 잇는다(표 CARD_CHAIN)
    //   · 원작이 자기 · 아군에게 거는 몸짓은 제자리에서, 이펙트는 시전자 쪽에(표 CARD_SELF) · 원작이 제자리 · 돌진인 몸짓(CARD_STAY · CARD_DASH)
    //   · 때리는 순간은 스파인 타격 이벤트(0ms 시전 표시는 빼고), 원작 타수대로 보이기만 나눈다(엔진 피해 합은 그대로)
    public partial class BattleDirector
    {
        // 카드 몸짓 길이 상한(화면 초, 1.3배속 뒤). 넘으면 앞 준비 · 빈 구간 · 꼬리부터 줄인다
        public const float CARD_MAX_A1 = 1.5f, CARD_MAX_A2 = 1.8f, CARD_MAX_S1 = 2.5f;
        const float CARD_SPEED_MAX = 1.5f;     // 줄여도 상한을 0.4초 넘게 넘으면 이 배속까지만 빠르게
        const int CARD_HITS_MAX = 5;           // 원작 타수대로 나누어 보일 때 많아야
        const float CARD_TAIL_KEEP = 0.45f;    // 마지막 타격 뒤 남기는 몫(1배속 초) — 회수 동작
        const float CARD_PRE_KEEP = 0.35f;     // 첫 타격 앞 남기는 몫(1배속 초) — 휘두르기

        // 점검(-cardsheet) — 마지막 카드 몸짓
        public string CardAnimLog = "";
        public int CardNaturalMs, CardShownMs, CardHitsShown;
        public float CardSpeed = 1.3f;
        public string CardMoveLog = "";

        static string MKey(string hid, Motion m) => hid + "|" + m;

        // 원작 영상 배속 — 원작 그대로 모드(BolzenaFx.OriginalOnly)에서 카드 · 고학년 몸짓을 이 배속으로 튼다(없으면 1배속).
        //   원작 영상의 사건 간격(스파인 이벤트 둘 사이) ÷ 스파인 ms 간격 — 표 · 근거는 bolzena-unity-tmp/orig_speed.tsv(2026-10-17).
        //   표의 몸짓 시각(CARD_HITS · CARD_TIME · ULT_FIT · ULT_PUNCH)은 모두 스파인 ms 라 이 배속으로 나뉘어 화면 시각이 된다
        static readonly Dictionary<string, float> ORIG_SPEED = new Dictionary<string, float>
        {
            [MKey("네르_빡침", Motion.Attack1)] = 1.48f, [MKey("네르_빡침", Motion.Attack2)] = 1.13f, [MKey("네르_빡침", Motion.Ultimate)] = 1.37f,
            [MKey("디아나_왕년", Motion.Attack2)] = 0.98f, [MKey("디아나_왕년", Motion.Ultimate)] = 1.33f,
            [MKey("란", Motion.Attack1)] = 1.33f, [MKey("란", Motion.Ultimate)] = 1.31f,
            [MKey("리뉴아", Motion.Ultimate)] = 1.37f,
            [MKey("벨라", Motion.Attack1)] = 1.08f, [MKey("벨라", Motion.Attack2)] = 1.45f, [MKey("벨라", Motion.Skill1)] = 1.36f, [MKey("벨라", Motion.Ultimate)] = 1.24f,
            [MKey("비비", Motion.Attack1)] = 1.27f, [MKey("비비", Motion.Attack2)] = 1.29f, [MKey("비비", Motion.Ultimate)] = 1.21f,
            [MKey("시온더다크불릿", Motion.Skill1)] = 1.24f, [MKey("시온더다크불릿", Motion.Ultimate)] = 1.30f,
            [MKey("실비아", Motion.Attack1)] = 1.20f, [MKey("실비아", Motion.Attack2)] = 1.33f,
            [MKey("아야", Motion.Attack1)] = 1.33f, [MKey("아야", Motion.Attack2)] = 1.24f, [MKey("아야", Motion.Ultimate)] = 1.00f,
            [MKey("에르핀_왕도", Motion.Skill1)] = 1.33f, [MKey("에르핀_왕도", Motion.Ultimate)] = 1.31f,
            [MKey("에피카", Motion.Skill1)] = 1.00f,
            [MKey("요미", Motion.Skill1)] = 1.08f,
            [MKey("우로스", Motion.Attack1)] = 1.48f, [MKey("우로스", Motion.Attack2)] = 1.30f, [MKey("우로스", Motion.Ultimate)] = 1.20f,
            [MKey("우이", Motion.Attack2)] = 1.10f,
            [MKey("이드", Motion.Attack1)] = 1.23f, [MKey("이드", Motion.Attack2)] = 1.28f, [MKey("이드", Motion.Ultimate)] = 1.25f,
            [MKey("죠안", Motion.Attack1)] = 1.18f, [MKey("죠안", Motion.Ultimate)] = 1.21f,
            [MKey("티그_영웅", Motion.Attack1)] = 1.51f, [MKey("티그_영웅", Motion.Attack2)] = 1.64f, [MKey("티그_영웅", Motion.Skill1)] = 1.44f,
        };
        static float OrigSpeed(string hid, Motion m) => hid != null && ORIG_SPEED.TryGetValue(MKey(hid, m), out var v) && v > 0 ? v : 1f;

        // 탄 수만큼 되풀이하는 고리 조각 — 사도|몸짓 → (조각, 많아야). 엔진 피해 수만큼 그 조각을 잇는다(시온 스킬 — 원작은 탄마다 Loop 한 번, fix_Y 6-2)
        static readonly Dictionary<string, (string piece, int max)> CARD_LOOP = new Dictionary<string, (string, int)> { [MKey("시온더다크불릿", Motion.Skill1)] = ("Skill1_1_Loop", 5) };

        // 이어 트는 조각 — 사도|몸짓 → 조각들. 「a/b」는 그중 하나(같은 칸 번호끼리 짝 — 마요(멋짐) Start2 면 End2).
        //   스켈레톤 이벤트로 가렸다(card_fix/anims.json): 새 몸짓 표시(1000003)나 같은 짜임의 다른 판(이드 Attack1_1~4 · 앨리스 Skill1_1~3)은 갈래라 잇지 않는다
        static readonly Dictionary<string, string[]> CARD_CHAIN = new Dictionary<string, string[]>
        {
            [MKey("에르핀_왕도", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_1_Loop", "Skill1_1_End" },
            [MKey("마요_멋짐", Motion.Skill1)] = new[] { "Skill1_1_Start1/Skill1_1_Start2", "Skill1_1_Loop", "Skill1_1_End1/Skill1_1_End2" },
            [MKey("나이아", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2", "Skill1_3", "Skill1_4" },
            [MKey("바롱", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3" },
            [MKey("사리", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2" },
            [MKey("셰이디", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2" },
            [MKey("루포", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2", "Skill1_3", "Skill1_4", "Skill1_5", "Skill1_6", "Skill1_7" },
            [MKey("슈팡", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3", "Skill1_4_Loop", "Skill1_5" },
            [MKey("시온더다크불릿", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_1_Loop", "Skill1_2" },
            [MKey("스노키", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2/Skill1_3" },
            [MKey("아사나", Motion.Attack2)] = new[] { "Attack2_1", "Attack2_2/Attack2_3" },
            [MKey("아사나", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2/Skill1_3" },
            [MKey("키디언", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3", "Skill1_4" },
            [MKey("바리에", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3", "Skill1_4" },
            [MKey("로네_시장", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2" },
            // 앨리스 — 1_1 불꽃 · 1_2 번개 · 1_3 회오리는 잇는 조각이 아니라 갈래(같은 짜임). 원작은 쓸 때마다 다른 판이 나와 하나를 무작위로(재검토 3조)
            // 티그(영웅) 강공 — 원작 영상 5.6초 · 6타 이상인데 Attack2_1 은 2.8초뿐. Attack2_2 를 이어 튼다(재검토 4조 짐작 · 상한 1.8초로 줄인다)
            [MKey("티그_영웅", Motion.Attack2)] = new[] { "Attack2_1" },   // (10-17 2차) 한 바퀴 돌고 검기 날림 — 2_1 · 2_2 는 같은 짜임의 갈래(이으면 두 번 돌며 벴다)
            [MKey("벨라", Motion.Attack2)] = new[] { "Attack2_1/Attack2_2" },   // (10-18 사용자 「같은 몸짓 2번」) 2_1 · 2_2 는 같은 짜임의 갈래(조각마다 시작 목소리 · 시전 · 2100 폭발, 원작 영상도 사이에 쉬는 자세) — 하나만, 2_2 는 끝에 보호막. 예전:   // (10-15 원작 대조) 구슬 낙하 → 두 번째 휘두르기 · 번개 · 방울 장막까지 한 벌
            [MKey("앨리스", Motion.Skill1)] = new[] { "Skill1_1/Skill1_2/Skill1_3" },
            // 네티 — 조각 셋(Start · Loop · End)을 원작이 한 판으로 묶은 _Full 이 있다(전엔 _End 만 600ms)
            [MKey("네티", Motion.Attack1)] = new[] { "Attack1_1_Full" },
            [MKey("네티", Motion.Attack2)] = new[] { "Attack2_1_Full" },
            // 하이디 강공 — 원작은 상자 변신 모습의 연타(Attack1_1_Change). Attack2_1 이 없다고 평타를 틀던 것
            [MKey("하이디", Motion.Attack2)] = new[] { "Attack1_1_Change" },
            // 에르핀(왕도) 강공 — Attack2_1 이 없어 평타(Attack1_1)를 그대로 틀었다. 원작 반대 손 판(Attack1_2 — 이벤트 1012306 · 07 = cast_2 · slash_2)으로(10-18 이벤트 ID 표)
            [MKey("에르핀_왕도", Motion.Attack2)] = new[] { "Attack1_1", "Attack1_2" },   // (10-18 사용자 「강공 2타 아님」) 원작 강공이 없다 — 원작 평타 두 판(왼손 · 오른손)을 이어 2타
        };

        // 원작이 자기 · 아군에게 거는 몸짓(강화 · 회복 · 실드 · 보호막) — 제자리에서, 몸짓 이펙트는 시전자 쪽에. 피해 숫자 · 맞음만 적에게
        static readonly HashSet<string> CARD_SELF = new HashSet<string>
        {
            // 저학년(대조 1~4조)
            MKey("다야_퓨어샤인", Motion.Skill1), MKey("라이카", Motion.Skill1), MKey("리코타", Motion.Skill1), MKey("빅우드", Motion.Skill1),
            MKey("셰럼", Motion.Skill1), MKey("아네트", Motion.Skill1), MKey("아일라", Motion.Skill1), MKey("오팔", Motion.Skill1),
            MKey("코미_수영복", Motion.Skill1), MKey("스피키", Motion.Skill1), MKey("벨벳", Motion.Skill1),
            MKey("바리에", Motion.Skill1), MKey("스노키", Motion.Skill1), MKey("아라그니아", Motion.Skill1), MKey("레테", Motion.Skill1),
            MKey("큐이", Motion.Skill1), MKey("쵸피", Motion.Skill1),
            MKey("로네", Motion.Skill1), MKey("오로라", Motion.Skill1), MKey("이드", Motion.Skill1), MKey("우로스", Motion.Skill1),
            MKey("힐데", Motion.Skill1), MKey("캬롯", Motion.Skill1), MKey("포셔", Motion.Skill1),
            MKey("네르", Motion.Skill1), MKey("바나", Motion.Skill1), MKey("베니", Motion.Skill1), MKey("아멜리아_R41", Motion.Skill1),
            MKey("코미", Motion.Skill1), MKey("폴랑", Motion.Skill1), MKey("마고", Motion.Skill1),
            // 강공
            MKey("아일라", Motion.Attack2), MKey("시저", Motion.Attack2), MKey("뮤트", Motion.Attack2),
            MKey("헤일리", Motion.Attack2), MKey("힐데", Motion.Attack2), MKey("캬롯", Motion.Attack2), MKey("제이드", Motion.Attack2),
            MKey("아멜리아_R41", Motion.Attack2), MKey("에르핀", Motion.Attack2), MKey("슈팡", Motion.Attack2),
            MKey("키샤", Motion.Attack2),   // 폴라로이드 액자 연출은 키샤 앞(적 쪽에 찍혀 화면 오른쪽을 덮던 것)
            MKey("비비", Motion.Skill1), MKey("네르_빡침", Motion.Skill1), MKey("실비아", Motion.Skill1),   // 실비아 — 티파티를 즐기기만(피해 연출 없음, 10-17 3차)
              // (10-17 사용자) 수은을 모아 자기 보호막 · 네르 깃발은 아군 쪽
            MKey("키샤", Motion.Skill1), MKey("레비_졸업", Motion.Skill1), MKey("레비_졸업", Motion.Attack2), MKey("페스타", Motion.Skill1),   // 키샤 무대 원판 · 레비(졸업) 착지 폭발은 시전자 자리(1차 재촬영 확인)
        };

        // 원작이 제자리에서 하는 몸짓(근접 그림이라도 달려가지 않는다 — 이펙트는 그대로 적에게)
        static readonly HashSet<string> CARD_STAY = new HashSet<string>
        {
            MKey("리코타", Motion.Attack1), MKey("리코타", Motion.Attack2), MKey("라이카", Motion.Attack2), MKey("레이지", Motion.Skill1),
            MKey("리온", Motion.Skill1), MKey("벨라", Motion.Skill1), MKey("리츠", Motion.Attack1), MKey("리츠", Motion.Attack2), MKey("리츠", Motion.Skill1),
            MKey("슈로", Motion.Skill1), MKey("레비_졸업", Motion.Attack2),
            // (2026-10-15 사용자 「원거리는 다가가지 않아도」 · 원작 평타 영상) 란 — 가로 베기 줄이 제자리에서 적 너머까지 · 벨라 — 혼령 소용돌이가 제자리에서 적까지 · 아야 — 얼음 물결 · 가시가 떨어진 적에게
            MKey("란", Motion.Attack1), MKey("란", Motion.Attack2), MKey("란", Motion.Skill1),
            MKey("네르_빡침", Motion.Attack1), MKey("네르_빡침", Motion.Attack2), MKey("네르_빡침", Motion.Skill1),
            MKey("이드", Motion.Attack1), MKey("이드", Motion.Attack2), MKey("티그_영웅", Motion.Attack1),   // (10-17 원작 영상) 전열이지만 제자리 — 이드 광선 · 티그 날리는 베기   // (10-17 사용자) 네르 — 멀리서 도끼 소환체(원작 몸짓의 Weapon_Big)를 적에게 소환한다, 다가가지 않음
            MKey("벨라", Motion.Attack1), MKey("벨라", Motion.Attack2),
            MKey("아야", Motion.Attack1), MKey("아야", Motion.Attack2), MKey("아야", Motion.Skill1),
            // 우이(기억) 강공 · 스킬 — 전엔 자기형(CARD_SELF)이라 연못 · 터지는 고리 · 세로선이 시전자 발밑에 섰다(2026-10-13 캡처 검토: 적 쪽이어야).
            //   몸은 제자리, 맞는 장은 적에게(노래 · 강화는 FxRules.CARD_AT 로 시전자에)
            MKey("우이_기억", Motion.Attack1), MKey("우이_기억", Motion.Attack2), MKey("우이_기억", Motion.Skill1),
        };

        // 원작이 적 앞까지 가는 몸짓(근접 그림이 아니어도) — 몸짓이 스스로 멀리 옮기면(순간이동) 따로 가지 않는다
        static readonly HashSet<string> CARD_DASH = new HashSet<string>
        {
            MKey("뮤트", Motion.Skill1), MKey("슈팡", Motion.Skill1), MKey("다야", Motion.Skill1), MKey("헤일리_멀쩡", Motion.Attack1), MKey("스노키", Motion.Attack2),
        };

        // 이펙트 갈래를 고를 이름(FxRules.CardKinds) — 트는 조각 이름과 다를 때. 하이디 강공: 몸짓은 Attack1_1_Change 지만 이펙트는 원작 강공(attack2 사격)
        static readonly Dictionary<string, string> CARD_FX_ANIM = new Dictionary<string, string>
        {
            [MKey("하이디", Motion.Attack2)] = "Attack2_1",
        };
        // 원작 영상에서 읽은 타격 시각(ms, 1배속 · 몸짓 시작에서) — 스파인 타격 이벤트가 원작 박자와 다를 때만.
        //   죠안 평타: 애니에 Event 가 없고 SFX 2 가 1300ms 뿐이라 탄이 1초 넘게 떠 있다가 맞았다 — 원작은 0.3초에 첫 발, 1.3초에 둘째
        static readonly Dictionary<string, int[]> CARD_HITS = new Dictionary<string, int[]>
        {
            [MKey("죠안", Motion.Attack1)] = new[] { 1250 },   // (10-17 정밀 분석)
            [MKey("벨라", Motion.Attack2)] = new[] { 2100 },   // (10-18) 한 판 폭발 · 맞음 — 예전 { 1833, 5166 }   // (fix_Y 5-1) 2_1 이벤트 1833 · 2_2 시작 3333 + 1833 — 엔진 1타라 SplitHits 로 2번
            [MKey("시온더다크불릿", Motion.Attack1)] = new[] { 1400 },   // (fix_Y 6-1) 비행 몫을 뺀 뒤 맞음이 이벤트 1000 에 와 원작(1.6초)보다 0.4초 일렀다 — 조준선(LEAD_MS 380)이 1020 에
            [MKey("비비_신성", Motion.Skill1)] = new[] { 3350, 4200, 5150, 6100 },   // (10-18 제안_C) 영상 × 1.2 = 몸짓 ms. 예전 값 ↓   // (fix_Y 8) 영상 2.8 · 3.5 · 4.3 · 5.1 — 3.5 이후는 장판 틱(몸짓 4.5초 뒤). 배속표에 이 몸짓 값이 없어(스파인 이벤트 없음) 영상 ms 그대로
            [MKey("란", Motion.Attack1)] = new[] { 633 },
            [MKey("네르_빡침", Motion.Attack1)] = new[] { 700, 2033 },   // (10-17 정밀 분석) 원작 평타 2타
            [MKey("에르핀_왕도", Motion.Attack2)] = new[] { 900, 2533 },   // (10-18) 두 판 주먹 탄이 닿는 때(567 + 330, + 1633) — 200 충전을 타격으로 잡아 숫자가 먼저 떴다
            [MKey("에르핀_왕도", Motion.Skill1)] = new[] { 1667, 1967, 2400, 2534 },   // (10-18 사용자 「대상 지정 땐 피해 없고 펀치 때만」) 333 시전(1012314)을 타격으로 잡던 것 — 주먹 맞음 1012312 · 끝 1012316
            [MKey("리뉴아", Motion.Skill1)] = new[] { 1850, 2050, 2300, 2600, 3600 },   // (10-18 사용자 「판정 ↔ 임팩트 안 맞음」) impact_1(이벤트 1100) 장 안 레이저 0.75 · 0.95 · 1.2 · 1.5 + lastcast 3600 — 영상 초를 그대로 적던 것
            [MKey("벨라", Motion.Attack1)] = new[] { 800, 1400 },   // (3차) 원작 장 하나 안 베기 2번
            [MKey("비비", Motion.Attack1)] = new[] { 400, 1033 },
            [MKey("비비", Motion.Skill1)] = new[] { 1747 },   // (10-18 4차 사용자 「구 생김 → 터짐 = 피해」) 1006108 구(667) + 터지는 끝 1080(FxRules EVENT_AFTER_MS barrier_1_end) — 100(1006111 시전)을 타격으로 잡아 숫자가 구보다 먼저 떴다
            [MKey("아야", Motion.Skill1)] = new[] { 1610, 4480 },   // (10-18 4차 사용자 「왕복 더 빠르게」) 쏜 뒤 3.2초에 돌아오며 적(원작 4.96초 · 예전 6260) — 화면 밖 기다림 약 4초 → 1.8초(UltFx AYA_BACK 0.35)   // 예전 {1610, 6260}: (10-18 2차 사용자 「빠름 · 짧음」) 원작 — 던짐(1300 ≈ 1610 − PROJ_LAG 330) + 0.31초 첫 적 · + 4.96초 돌아오며 적(몸짓 3.13초 뒤에도 나비를 기다린다). 예전 { 1900, 2900 }   // 나비 왕복 — 갈 때 · 올 때         // (10-17 사용자 「1타 몸짓인데 2타」) 0.167초 이벤트는 시전 표시
            [MKey("이드", Motion.Attack1)] = new[] { 367, 700, 1033, 1367 },   // (10-17) 나타가 볼을 네 번
            [MKey("에르핀_왕도", Motion.Attack1)] = new[] { 900 },
            [MKey("우로스", Motion.Attack2)] = new[] { 1900 },   // (10-17 원작 스파인 이벤트) 시전 1133 → 뱀 1767 → 맞음 1900. 전 450 은 뱀이 나가기 전에 맞았다
            [MKey("아야", Motion.Attack1)] = new[] { 267, 667 },        // (10-17 사용자 「2타 몸짓 압축인데 임팩트 1번」 · 원작 이벤트)
            [MKey("아야", Motion.Attack2)] = new[] { 1233, 1833 },
            [MKey("시온더다크불릿", Motion.Attack2)] = new[] { 833 },   // (10-17 사용자) 탄 없이 포즈 직후 바로 폭발 — 원작 이벤트 833
            // (10-15 원작 대조, 1배속 ms) 스파인 이벤트가 원작 화면의 맞는 때와 어긋난 것
            [MKey("우이_기억", Motion.Attack2)] = new[] { 1600 }, [MKey("우이_기억", Motion.Skill1)] = new[] { 1800 }, [MKey("비비_신성", Motion.Attack2)] = new[] { 1850 },   // (10-18 제안_C) 원작 맞음 때(영상 × 1.2)
            [MKey("비비_신성", Motion.Attack1)] = new[] { 1700 },   // (10-18) 쏘기 섬광 1.7 직후(영상 1.4 × 1.2)   // 손에서 초록 구슬 1.3 → 1.4초 적 자리
            [MKey("리뉴아", Motion.Attack1)] = new[] { 1420 },      // (10-18 3차) 렌즈 이벤트 1300 + 비행 120(원작 영상 31~33칸 렌즈 → 34칸 폭발 · 숫자) — 1250 은 렌즈가 나가기도 전에 숫자가 떴다(cap_fix3 1.30초 「10」, 렌즈 1.78초 닿음)   // 1.0초 초록 다트 → 1.1초 터짐
            [MKey("리뉴아", Motion.Attack2)] = new[] { 1367 },      // (10-18 원작 이벤트) 레이저 · 맞음 1367 — 250 은 숫자가 레이저보다 1.1초 먼저 떴다
            [MKey("이드", Motion.Attack2)] = new[] { 1067 },         // (10-17 원작 이벤트) 굵은 레이저 1067 — 전 600
            [MKey("티그_영웅", Motion.Attack1)] = new[] { 200, 1333 },   // (10-17 원작 이벤트) 2타
            [MKey("우이", Motion.Attack1)] = new[] { 600 },
            [MKey("우로스", Motion.Attack1)] = new[] { 767, 1733 },   // (10-17 정밀 분석) 원작 평타 2타
            [MKey("우이", Motion.Skill1)] = new[] { 733, 1533, 2333, 3133 },   // 0.8초 간격 빛 비 4타
            [MKey("우이_기억", Motion.Attack1)] = new[] { 2300 },   // (10-18) 영상 1.9 × 1.2         // 0.6초 큰 초록 별빛
            [MKey("클로에", Motion.Attack1)] = new[] { 800 },       // 0.7초 머리 위 큰 원호 → 0.8초 터짐     // (10-15 원작 대조) 금빛 뱀이 0.3~0.9초 휩쓸고 지나간다 — 전 1.13초
            [MKey("티그_영웅", Motion.Attack2)] = new[] { 1900 },   // (10-17 2차 · 원작 이벤트 1.6초 검기 발사 + 비행)
            [MKey("에피카", Motion.Attack2)] = new[] { 1067 },               // 원작 강공은 에피칸이 뛰어나가 불덩이를 뱉는 때(epicon Attack2_1 이벤트) — 에피카 손 이벤트(533)보다 늦다. 비행은 CARD_LAG_KEEP 로 더해진다
            // 재검토(2026-10-06 2차): 첫 이벤트가 이펙트 시작 표시라 너무 일찍 맞던 것 · 나눈 타수가 원작보다 많던 것
            [MKey("요미", Motion.Skill1)] = new[] { 1700 },                 // 300 은 달 소환 표시 — 원작 61% 지점
            [MKey("요미", Motion.Attack2)] = new[] { 1140 },   // (10-18 3차 「폭발이 혜성보다 먼저」) 혜성 900 + 떨어지는 0.24초 = 닿으며 폭발(FxRules.SHOT_LAG_MS) — 쏘는 이벤트 900 에 맞던 것
            [MKey("사리", Motion.Skill1)] = new[] { 1967 },                 // 원작 1타(80%) — 연기 뒤 Skill1_2 의 찌르기(1567 + 400)
            [MKey("델리아", Motion.Skill1)] = new[] { 667, 1800 },          // 원작 2타
            // 2026-10-13 엘다인 20명 카드 손보기
            //   티그(영웅) 저학년 — 첫 SFX2(467)는 몸이 안 보이는 돌진 중(0.35~0.92초 숨음)이라 그때 맞으면 「타수만큼만 휘두르기」가 몸짓을 0.9초에 끊어
            //   적 뒤 등장 · 큰 베기(1933 · 2000 이벤트)가 통째로 잘렸다. 맞는 때는 큰 베기
            [MKey("티그_영웅", Motion.Skill1)] = new[] { 467, 1933 },   // (10-17 사용자 「적 뒤로 가며 베고 돌아오며 또 벰」 · 원작 이벤트)
            //   실비아 강공 — 767 은 시전자 쪽 반짝(시전 표시), 적 위 분홍 폭발은 그 뒤(원작 기본공격 영상 2.5초 시전 반짝 → 2.6초 폭발) — 900 · 1633
            [MKey("실비아", Motion.Attack2)] = new[] { 900 },   // (10-18 2차 사용자 「1타 · 터질 때만 피해」) 1633 은 끝 연기(1013008 end) — 타격 아님. 예전 { 900, 1633 }
            //   디아나(왕년) 강공 — 원작 4타(뛰어올라 내려찍으며 연타). 스파인 타격 표시는 1700 하나뿐이라 1타로 보였다 — 내려찍는 1700 부터 0.13초 간격 4번
            [MKey("디아나_왕년", Motion.Attack2)] = new[] { 1700 },   // (10-17 정밀 분석) 원작 강공 1타
            //   비비 강공 — 333 이벤트(1006132)는 단검 넷(Weapon_Dagger1~4)이 떠오르는 표시라 첫 타격이 창 착지 전이었다(2026-10-14).
            //   꽂히는 때는 667 · 733 · 800 · 867(이벤트 1006122~1006125 + 맞음 1006131)
            [MKey("비비", Motion.Attack2)] = new[] { 667, 733, 800, 867 },
        };
        // 한 번 휘두르는 몸짓 안의 연타 — 엔진이 1타여도 「타수만큼만 휘두르기」로 끊지 않고 원작 타수대로 보이기만 나눈다(엔진 피해 합 그대로)
        static readonly HashSet<string> CARD_SPLIT = new HashSet<string> { MKey("벨라", Motion.Attack2), MKey("아야", Motion.Attack1), MKey("아야", Motion.Attack2), MKey("이드", Motion.Attack1), MKey("티그_영웅", Motion.Attack1), MKey("티그_영웅", Motion.Skill1), MKey("우로스", Motion.Attack1), MKey("우이", Motion.Skill1), MKey("벨라", Motion.Attack1), MKey("네르_빡침", Motion.Attack1), MKey("리뉴아", Motion.Skill1), MKey("비비", Motion.Attack1), MKey("아야", Motion.Skill1), MKey("클로에", Motion.Attack1) };   // 벨라 — 엔진 1타라도 두 번째 휘두르기까지 튼다 · 클로에(10-19 타수 줄이기) — 탄 평타가 엔진 2타(한 대 + 적 전체)로 줄어도 원작 3타 몸짓(400 · 733 · 1667)을 끝까지, 숫자는 첫 · 마지막 맞음에

        // 적 뒤로 순간이동하는 몸짓 — (사라지는 때, 돌아오는 때) ms(1배속 · 몸짓 시작에서). 몸이 안 보이는 사이에 맞는 적 뒤로 옮기고 돌려 세운 뒤, 돌아오는 때 제자리로.
        //   티그(영웅) 저학년: 스파인에서 몸 슬롯이 0.35~0.92초 다 숨는다(돌진) → 0.93초 칼을 든 채 다시 보이고 1.9~2.0초 큰 베기 → 2.03초 제자리로 뛴다
        static readonly Dictionary<string, Vector2> CARD_BEHIND = new Dictionary<string, Vector2>
        {
            [MKey("티그_영웅", Motion.Skill1)] = new Vector2(600, 2017),
        };
        // 변신판(_Change 등) 몸짓의 타격 시각 — 「사도|몸짓|꼬리」(10-18)
        static readonly Dictionary<string, int[]> CARD_HITS_FORM = new Dictionary<string, int[]>
        {
            [MKey("네르_빡침", Motion.Attack1) + "|Change"] = new[] { 167, 1300, 2400 },   // Attack1_1_Change 맞음 이벤트 05~07
            [MKey("클로에", Motion.Attack1) + "|SebastianForm"] = new[] { 400, 733, 1667 },   // 탄 모습 평타 맞음(1005654) — 메리 고 라운드 탑승(10-18)
        };
        static List<float> CardHitsFix(string hid, Motion m, List<float> marks, string form = null)
        {
            if (form != null && CARD_HITS_FORM.TryGetValue(MKey(hid, m) + "|" + form, out var fm)) { var rf = new List<float>(); foreach (var x in fm) rf.Add(x / 1000f); return rf; }
            if (!CARD_HITS.TryGetValue(MKey(hid, m), out var ms)) return marks;
            var r = new List<float>(); foreach (var x in ms) r.Add(x / 1000f); return r;
        }

        // 투사체 비행(0.33초)을 타격에 그대로 더하는 사도 — 이 사도들은 스파인 이벤트가 쏘는 때라 빼면 원작보다 일찍 맞았다(재검토 4조: 시온 평타 1.6 → 1.04, 칸나 0.6 → 0.39, 에피카 54% → 29%)
        static readonly HashSet<string> CARD_LAG_KEEP = new HashSet<string> { "칸나", "에피카" };   // 시온 뺌(10-17 정밀 분석 — 원작에 탄이 없고 조준선 다음 바로 폭발)
        public static bool CardLagKeep(string hid) => CARD_LAG_KEEP.Contains(hid);
        // 몸짓별 — 시온 강공은 탄 없이 포즈 직후 바로 터진다(10-17 사용자) — 비행을 더하지 않는다
        public static bool CardLagKeep(string hid, Motion m) => CardLagKeep(hid) && !(hid == "시온더다크불릿" && m == Motion.Attack2);

        // 표에 없으면 트는 조각 이름 그대로 — 단 SD 에 강공 · 스킬 동작이 없어 평타 조각(Attack1_1)을 빌려 트는 사도는 이펙트 갈래만 몸짓대로(Attack2_1 · Skill1_1).
        //   전에는 갈래도 평타로 골라 평타 = 강공 = 스킬 이펙트가 같았다(2026-10-13 에피카 · 에르핀(왕도) · 비비(신성) 캡처). 표(FxRules.CARD_NAMES)의 「사도|Attack2 · Skill1」 장이 이 이름으로 걸린다
        static string CardFxAnim(string hid, Motion m, string first)
        {
            if (CARD_FX_ANIM.TryGetValue(MKey(hid, m), out var a)) return a;
            if (first == null) return null;
            if (m == Motion.Attack2 && !first.StartsWith("Attack2")) return "Attack2_1";
            if (m == Motion.Skill1 && !first.StartsWith("Skill1")) return "Skill1_1";
            return first;
        }

        public static bool CardSelf(string hid, Motion m) => CARD_SELF.Contains(MKey(hid, m));

        // 이 카드 몸짓이 이어 틀 조각들(첫 조각 = 기존 AnimFor). 표에 없으면 그 한 조각
        static List<string> CardChain(UnitView u, string hid, Motion m, string first, int loops = 1)
        {
            var res = new List<string>();
            if (u == null || !u.SpineArt || u.SkelData == null || u.FormAttack != null) { res.Add(first); return res; }
            if (!CARD_CHAIN.TryGetValue(MKey(hid, m), out var spec)) { res.Add(first); return res; }
            int pick = -1;
            foreach (var slot in spec)
            {
                var alts = slot.Split('/');
                string n;
                if (alts.Length > 1)
                {
                    if (pick < 0) pick = Random.Range(0, alts.Length);
                    n = alts[Mathf.Min(pick, alts.Length - 1)];
                }
                else n = alts[0];
                if (u.Has(n)) res.Add(n);
            }
            if (CARD_LOOP.TryGetValue(MKey(hid, m), out var lp)) { int at = res.IndexOf(lp.piece); if (at >= 0) for (int r = 1; r < Mathf.Clamp(loops, 1, lp.max); r++) res.Insert(at, lp.piece); }   // 탄 수만큼 고리
            if (res.Count == 0) res.Add(first);
            return res;
        }

        // (10-18 2차 사용자 「탄을 다 쏜 뒤 한 발 더」) 탄 수만큼 잇는 고리(CARD_LOOP)는 고리마다 첫 쏘기 이벤트 한 번이 타격(Skill1_1_Loop 33ms — 1005508 쏘기 · SFX 2).
        //   고리 안 333 재장전(1005521) · 끝 조각(Skill1_2 633 — 총을 들어 올릴 뿐)을 타격으로 잡아 HitTimes 가 끝 조각을 골라 고리가 끝난 뒤 한 발 더 나갔다
        static List<float> LoopShots(UnitView u, string hid, Motion m, List<string> chain, List<float> marks)
        {
            if (chain == null || u == null || u.SkelData == null || !CARD_LOOP.TryGetValue(MKey(hid, m), out var lp)) return marks;
            var r = new List<float>(); float o = 0;
            foreach (var n in chain)
            {
                var a = u.SkelData.FindAnimation(u.F(n));
                if (a == null) continue;
                if (n == lp.piece) r.Add(o + 0.033f);
                o += a.Duration;
            }
            return r.Count > 0 ? r : marks;
        }

        // 조각들의 때리는 순간(1배속 초, 이어 튼 전체 시각) — UnitView.Strikes 와 같은 규칙을 조각 전체에.
        //   0ms(조각 첫머리 0.05초 안) 시전 표시 이벤트는 뒤에 다른 타격 이벤트가 있으면 첫 타격으로 잡지 않는다(이드(재활) 저학년 · 슈로 강공 · 아멜리아(R41) 강공 · 키디언 저학년).
        //   여러 번 때리는 창은 4초까지, 0.2초 안에 붙은 것은 하나로(이펙트 표시 + 맞는 소리 짝)
        static List<float> CardStrikes(UnitView u, List<string> names, out float total)
        {
            total = 0;
            var res = new List<float>();
            var sd = u.SkelData;
            var evs = new List<(float t, Spine.Event e)>();
            foreach (var n in names)
            {
                var a = sd.FindAnimation(u.F(n));
                if (a == null) continue;
                foreach (var tl in a.Timelines) if (tl is EventTimeline et) foreach (var ev in et.Events) evs.Add((total + ev.Time, ev));
                total += a.Duration;
            }
            evs.Sort((x, y) => x.t.CompareTo(y.t));
            bool Own(Spine.Event e)
            {
                if (e.Data.Name != "Event" && e.Data.Name != "Event_2") return false;
                foreach (var s in (e.String ?? "").Split(','))
                    if (int.TryParse(s, out var v) && v >= 1000100 && v != 1000013 && v != 1000014) return true;
                return false;
            }
            bool Sfx2(Spine.Event e) => e.Data.Name == "SFX" && int.TryParse(e.String, out var v) && v >= 2;
            bool Late((float t, Spine.Event e) x) => x.t >= 0.05f;
            int iS = evs.FindIndex(x => Sfx2(x.e) && Late(x)), iO = evs.FindIndex(x => Own(x.e) && Late(x));
            if (iS < 0 && iO < 0) { iS = evs.FindIndex(x => Sfx2(x.e)); iO = evs.FindIndex(x => Own(x.e)); }
            float at;
            if (iS >= 0 && iO >= 0 && Mathf.Abs(evs[iS].t - evs[iO].t) <= 0.4f) at = Mathf.Max(evs[iS].t, evs[iO].t);   // 이펙트 표시가 주먹보다 먼저 오는 일(디아나)
            else if (iS >= 0) at = evs[iS].t;
            else if (iO >= 0) at = evs[iO].t;
            else at = (names.Count > 0 ? u.Duration(u.F(names[0])) : total) * 0.45f;
            res.Add(at);
            foreach (var (t, e) in evs)
            {
                if (t <= at + 0.001f || t > at + 4f) continue;
                if (!(Own(e) || Sfx2(e))) continue;
                if (t - res[res.Count - 1] < 0.2f) continue;
                res.Add(t);
            }
            return res;
        }

        // 카드 몸짓 줄이기 — 상한(화면 초 cap)을 넘으면 꼬리(마지막 타격 + 0.45초 뒤) → 앞 준비(첫 타격 0.35초 전까지, 조각마다) → 가운데 빈 구간(0.8초 넘는 사이)
        //   → 그래도 남으면 꼬리를 마지막 타격 + 0.2초까지. cutSkips · cutEnd(고학년과 같은 칸)에 적고 Rm 으로 시각을 옮긴다. 돌려주는 것: 그래도 넘는 몫(1배속 초)
        float CardCut(UnitView u, List<string> names, List<float> hits, float total, float cap, float speed)
        {
            float need = total - cap * speed;
            if (need <= 0.05f || hits.Count == 0) return 0;
            var offs = new List<float>(); float o = 0;
            var evAll = new List<float>();
            foreach (var n in names)
            {
                offs.Add(o);
                var a = u.SkelData.FindAnimation(u.F(n));
                if (a == null) continue;
                foreach (var tl in a.Timelines) if (tl is EventTimeline et) foreach (var ev in et.Events) evAll.Add(o + ev.Time);
                o += a.Duration;
            }
            offs.Add(o);
            int PieceOf(float t) { int p = 0; for (int i = 0; i + 1 < offs.Count; i++) if (t >= offs[i]) p = i; return p; }
            float first = hits[0], last = hits[hits.Count - 1];
            // 1) · 2) 꼬리와 앞 준비를 남은 몫에 비례해 같이 줄인다 — 꼬리부터 다 덜면 타격이 몸짓 끝 쪽(75~85%)으로 밀렸다(재검토 1조 강공).
            //   앞 준비는 첫 타격이 든 조각까지, 앞 조각은 거의 통째로(끝 0.03초 남김 — 다음 조각으로 넘어가게), 그 조각은 첫 타격 0.35초 전까지
            float tailRoom = Mathf.Max(0, total - (last + CARD_TAIL_KEEP));
            int pf = PieceOf(first);
            var fr = new List<(float a, float b)>(); float frontRoom = 0;
            for (int p = 0; p <= pf; p++)
            {
                float a = offs[p] + (p == 0 ? 0.1f : 0.02f);
                float b = p < pf ? offs[p + 1] - 0.03f : first - CARD_PRE_KEEP;
                if (b - a < 0.1f) continue;
                fr.Add((a, b)); frontRoom += b - a;
            }
            float room = tailRoom + frontRoom;
            float wantTail = room > 0 ? Mathf.Min(tailRoom, need * tailRoom / room) : 0;
            float wantFront = Mathf.Min(frontRoom, need - wantTail);
            if (wantTail > 0.05f) { cutEnd = total - wantTail; need -= wantTail; }
            for (int i = 0; i < fr.Count && wantFront > 0.05f; i++)
            {
                float c = Mathf.Min(wantFront, fr[i].b - fr[i].a);
                cutSkips.Add((fr[i].b - c, fr[i].b)); need -= c; wantFront -= c;
            }
            // 3) 가운데 빈 구간 — 첫 ~ 마지막 타격 사이 이벤트 없는 0.8초 넘는 틈(같은 조각 안), 앞뒤 0.25초 남김
            if (need > 0.05f)
            {
                var pts = new List<float>(hits);
                foreach (var t in evAll) if (t > first && t < last) pts.Add(t);
                for (int i = 0; i + 1 < offs.Count; i++) if (offs[i] > first && offs[i] < last) pts.Add(offs[i]);
                pts.Sort();
                for (int i = 0; i + 1 < pts.Count && need > 0.05f; i++)
                {
                    float a = pts[i] + 0.25f, b = pts[i + 1] - 0.25f;
                    if (b - a < 0.3f || PieceOf(a) != PieceOf(b)) continue;
                    float c = Mathf.Min(need, b - a);
                    cutSkips.Add((a, a + c)); need -= c;
                }
            }
            // 4) 꼬리를 더
            if (need > 0.05f)
            {
                float end0 = cutEnd < float.MaxValue ? cutEnd : total;
                float c = Mathf.Min(need, Mathf.Max(0, end0 - (last + 0.2f)));
                if (c > 0.02f) { cutEnd = end0 - c; need -= c; }
            }
            cutSkips.Sort((x, y) => x.from.CompareTo(y.from));
            return Mathf.Max(0, need);
        }

        static float CardCap(Motion m) => m == Motion.Skill1 ? CARD_MAX_S1 : m == Motion.Attack2 ? CARD_MAX_A2 : CARD_MAX_A1;
        // 사도 · 몸짓별 상한(초) — 원작 한 벌이 길어 상한에 잘리면 뒤 절반이 통째로 빠지는 것(벨라 강공 두 번째 휘두르기 · 번개 · 방울 장막, 원작 대조 10-15)
        static readonly Dictionary<string, float> CARD_CAP_BY = new Dictionary<string, float> { };   // (10-18) 벨라 강공 3.6 — 두 판을 잇던 때 값, 한 판이 되어 뺐다
        static float CardCap(Motion m, string hid) => hid != null && CARD_CAP_BY.TryGetValue(MKey(hid, m), out var c) ? c : CardCap(m);

        // 카드 몸짓 이동 — 근접 그림이면 달려가되 원작이 제자리 · 자기형이면 안 간다. 원작이 돌진이면(표) 간다(몸짓이 스스로 멀리 옮기면 빼고)
        // 사도 열 — 엘다인 20명(웹판 기획 자료 머리줄). 없는 사도는 예전 판정(CARD_STAY · Look.MeleeArt)
        static readonly Dictionary<string, string> HERO_ROW = new Dictionary<string, string>
        {
            ["디아나_왕년"] = "전열", ["벨라"] = "전열", ["비비"] = "전열", ["실비아"] = "전열", ["이드"] = "전열", ["클로에"] = "전열", ["티그_영웅"] = "전열",
            ["리뉴아"] = "중열", ["비비_신성"] = "중열", ["아야"] = "중열", ["에르핀_왕도"] = "중열", ["에피카"] = "중열", ["요미"] = "중열", ["우이"] = "중열", ["죠안"] = "중열",
            ["네르_빡침"] = "후열", ["란"] = "후열", ["시온더다크불릿"] = "후열", ["우로스"] = "후열", ["우이_기억"] = "후열",
        };
        public static string HeroRow(string hid) => hid != null && HERO_ROW.TryGetValue(hid, out var r) ? r : null;

        // (10-18 4차 사용자 「한 바퀴 돌고 위로 올려치면서 근거리면 상대를 베는 임팩트, 중열 · 후열이면 검기를 날린다」) 티그(영웅) 강공 — 이번 대상이 맨 앞 적인지(Acts 가 대상을 고른 뒤 정한다)
        bool TigNear;
        int FrontEnemy() { int best = -1; float bx = float.MaxValue; for (int i = 0; i < Enemies.Count; i++) if (Enemies[i] != null && !Dead(i) && Enemies[i].Feet.x < bx) { bx = Enemies[i].Feet.x; best = i; } return best; }   // 살아 있는 적 가운데 우리 쪽(왼쪽)에 가장 가까운 적
        bool CardDashes(UnitView u, string key, string hid, Motion m, List<string> chain, int targetCount)
        {
            if (targetCount == 0) { CardMoveLog = "대상 없음"; return false; }
            string k = MKey(hid, m);
            if (CARD_SELF.Contains(k)) { CardMoveLog = "자기형 제자리"; return false; }
            // 원작 열(10-17 사용자 「후열 · 중열 사도는 원거리」) — 중열 · 후열은 다가가지 않고, 전열은 다가가 친다(표 = 웹판 기획 자료 .omc/redesign 머리줄)
            //   원작 영상으로 확인한 예외(CARD_STAY · CARD_DASH)가 먼저 — 전열이라도 제자리 광선 · 날리는 베기(이드 · 티그 평타 · 벨라), 10-17 원작 영상 판정
            if (CARD_STAY.Contains(k)) { CardMoveLog = "원작 제자리"; return false; }
            // (10-18 4차 사용자) 티그(영웅) 강공 — 대상이 맨 앞 적이면 다가가 올려 베기(검기 없이), 중열 · 후열 적이면 제자리에서 검기를 날린다(TigNear — Acts 대상 고른 뒤)
            if (k == MKey("티그_영웅", Motion.Attack2)) { CardMoveLog = TigNear ? "가까운 적 — 다가가 베기" : "먼 적 — 제자리 검기"; return TigNear; }
            var row = HeroRow(hid);
            if ((row == "중열" || row == "후열") && !CARD_DASH.Contains(k)) { CardMoveLog = row + " 원거리 제자리"; return false; }
            if (row == "전열" && !CARD_DASH.Contains(k)) { CardMoveLog = "전열 근거리"; return true; }
            if (CARD_DASH.Contains(k))
            {
                float tr = u.Travel(chain);
                CardMoveLog = tr < SELF_TRAVEL ? "원작 돌진" : $"원작 돌진 · 몸짓이 스스로 {tr:F1}";
                return tr < SELF_TRAVEL;
            }
            bool melee = Look.MeleeArt(key);
            CardMoveLog = melee ? "근접" : "제자리";
            return melee;
        }

        // 적 뒤로 순간이동(CARD_BEHIND) — 숨는 때에 맞는 적 뒤(오른쪽)로 옮겨 돌려 세우고, 돌아오는 때에 제자리 · 방향을 되돌린다. 시각은 줄인 몸짓의 시계(Rm) ÷ 배속
        IEnumerator CardBehindCo(UnitView u, int seq, List<int> targets, Vector2 ms, float speed)
        {
            float t0 = Time.time;
            float hide = Rm(ms.x / 1000f) / speed, back = Rm(ms.y / 1000f) / speed;
            while (Time.time - t0 < hide) { if (Stale(u, seq)) yield break; yield return null; }
            var live = targets.FindAll(i => i < Enemies.Count && Enemies[i] != null);
            if (live.Count == 0 || !u) yield break;
            var tp = live.Count == 1 ? Enemies[live[0]].Feet : Mid(live);
            float reach = live.Count == 1 ? Enemies[live[0]].Width() * 0.35f + 1.25f : 2.4f;
            var to = new Vector3(Mathf.Min(tp.x + reach, 7.2f), tp.y - 0.02f, 0);
            BlinkPuff(u.Center);
            yield return u.MoveTo(to, 0.01f);
            bool flipped = u.SpineArt && u.Sa != null;
            if (flipped) u.Sa.Skeleton.ScaleX = -u.Sa.Skeleton.ScaleX;   // 적 뒤에서 적 쪽(왼쪽)을 본다
            BlinkPuff(u.Center);
            while (Time.time - t0 < back && !Stale(u, seq)) yield return null;
            if (!u) yield break;
            BlinkPuff(u.Center);
            if (flipped) u.Sa.Skeleton.ScaleX = -u.Sa.Skeleton.ScaleX;
            yield return u.MoveTo(u.Home, 0.01f);
            BlinkPuff(u.Center);
        }

        void BlinkPuff(Vector3 at) => Vfx.Glow(at, 1.8f, new Color(0.8f, 0.9f, 1f, 0.7f), 0.25f, 2f, null, 150, null, 1.2f);

        // 카드 몸짓이 화면 위로 나가면(뛰어오르기 — 디아나(왕년) 강공 · 비비 강공 · 요미 저학년 …) 몸을 그만큼 내린다(많아야 1.0). 몸짓이 끝나기 전에 되돌린다
        IEnumerator CardKeepY(UnitView u, int seq, float dur)
        {
            var cam = Camera.main;
            if (u == null || u.Art == null || cam == null || !u.SpineArt) yield break;
            float push = 0, t = 0;
            while (t < dur * 0.9f && u && !Stale(u, seq))
            {
                float sy = Mathf.Max(0.001f, u.Art.parent.lossyScale.y);
                float top = cam.transform.position.y + cam.orthographicSize - 0.12f;
                float out0 = (u.MeshBounds.max.y + push * sy) - top;   // 내리지 않았다면 위끝이 화면 밖으로 나간 몫(월드)
                push = Mathf.MoveTowards(push, Mathf.Clamp(out0 / sy, 0, 1.0f), Time.deltaTime * 10f);
                u.Nudge = new Vector3(0, -push, 0);
                yield return null; t += Time.deltaTime;
            }
            if (!u) yield break;
            float from = push;
            if (from > 0) yield return Clock.Tween(0.15f, k => { if (u) u.Nudge = new Vector3(0, -from * (1 - k), 0); });
            if (u) u.Nudge = Vector3.zero;
        }

        // 저학년 시작 섬광 —원작은 저학년마다 0.0~0.3초에 시전자 몸에서 흰 · 푸른 구슬이 터지고(별빛 번쩍) 고리가 퍼진다(힐데 · 가비아 영상).
        //   원작 공용 장을 _fxraw · MuMu 추출본에서 못 찾아(이름 skill_start · cast · common 다 봤다) 볼제나 공용 낱장으로 짓는다
        void CardOrb(UnitView u)
        {
            var c = u.Center;
            Vfx.Glow(c, 2.6f, new Color(0.85f, 0.95f, 1f, 0.95f), 0.32f, 3f, null, 340, null, 1.25f);
            Vfx.Glow(c, 1.3f, new Color(1f, 1f, 1f, 1f), 0.22f, 3.5f, null, 341, null, 1.1f);
            Vfx.Ring(c, 1.2f, 3.4f, 0.4f, new Color(0.55f, 0.85f, 1f, 0.9f), 3f, "FX_IN_Ring_ShockWave_01", 339);
            Vfx.Burst(c, new Vfx.BurstOpt { Tex = "FX_UI_star_02", Count = 6, Speed = new Vector2(0.5f, 1.5f), Angle = 90, Spread = 360, Life = new Vector2(0.2f, 0.35f), Size = new Vector2(0.25f, 0.5f), C0 = new Color(0.9f, 0.97f, 1f), Order = 342, Boost = 3f, ShrinkTo = 0 });
        }

        // 보이기만 나눈 타격 가운데 피해 조각이 없는 박자(피해 1 처럼 작은 값) — 맞는 몸 반응 · 원작 공용 맞음만
        void CardGhostHit(List<int> targets, HitKind kind)
        {
            foreach (var ti in targets)
            {
                if (ti >= Enemies.Count || Enemies[ti] == null || Dead(ti)) continue;
                var t = Enemies[ti];
                t.Flash(new Color(1f, 0.97f, 0.9f), 0.08f, 0.22f);
                t.Knock(1f, 0.12f, 0.2f);
                // 공용 노란 타격(hit_3_m · hit_slash_3)은 원작에 없다(2026-10-14) — 사도 고유 맞음 장이 있으면 몸 반응만, 없으면 약한 공용 한 장
                if (HitSheets && ownHitFx != true) Bolzena.Fx.BolzenaFx.Play(SmallHitSheet(kind), new Bolzena.Fx.FxPlayOptions { At = t.Center, Scale = 0.5f, Until = 0.3f, Order = 182, Parent = FieldRoot });
            }
            FieldRig.Shake(0.15f, 0.5f);
        }
    }
}
