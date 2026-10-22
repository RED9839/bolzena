using System.Collections;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.View;
using UnityEngine;

namespace Bolzena
{
    // ── 고학년 원작 영상 맞춤(2026-10-06 · ult_videos/compare_fixes.json · fix_progress.md) ──
    //   원작 영상 시트와 우리 시트를 견주어 고친 몫을 사도마다 한 줄로. 시각은 모두 원작 1배속 ms(스파인 조각을 이은 몸짓 시계 — 배속 · 줄이기는 뒤에서 따로 셈한다).
    //   Hits: 보이는 타격 시각 — 엔진 피해 합은 그대로, 보이기만 나누거나(SplitHits) 묶는다(MergeHits)
    //   Move: null 그대로 · "none" 제자리 · dash · teleport · leap(시각 Go · Land · Ret · Back, 자리 Dest front · middle · behind)
    //   Fx: 원작 장 이름 조각 → 자리(caster · target) · 트는 때(1배속 ms) — 이펙트 담당의 cue 장은 건드리지 않는다
    //   길이: 원작 몸짓이 7초 안이면 그대로(줄이지 않는다). 넘으면 앞 준비 · 뒤 꼬리만 모자란 만큼 줄이고 그래도 길면 1.2배까지(PlanCut · ULT_MAX)
    public partial class BattleDirector
    {
        class UltFit
        {
            public int[] Hits;
            public string Move; public int Go, Land, Ret = -1, Back; public string Dest = "front";
            public bool RetHidden;             // 복귀를 몸이 숨은 틈에 — 마지막 타격 뒤로 미루지 않는다(에르핀(왕도): 원작은 숨은 동안 제자리로 옮기고 폭발과 함께 굴러 나온다)
            public bool RetFly;                // 복귀를 「날려 감」으로 — 하던 몸짓(1_3 날려 누움) 그대로 몸만 제자리로 민다(돌아서 뛰기 · Idle 없음, 10-18 에르핀(왕도))
            public Dictionary<string, FxFix> Fx;
            public int[] Skip;                 // 건너뛸 구간(1배속 ms, 둘씩) — 원작보다 많이 도는 고리(스피키(메이드) 순간이동 사격 11번 → 원작 5번)
            public int End = -1;               // 이때(1배속 ms) 쉬는 동작으로 — 길이를 손으로 정한 사도(리뉴아)
            public float Speed;                // 0 = 셈대로
            public string[] Add;               // 계획에 없던 원작 장을 대상 쪽에 더한다(바롱 번개 — 장이 많아 시전 갈래만 남던 것)
            public string Anim;                // 갈래 하나로 틀 조각(Pick 의 한 갈래)
            public string PetSkin, PetAnim; public int PetMs = -1;   // 고학년 동안 곁에 띄울 소환수(epicon_ult) 스킨 · 몸짓 · 그 몸짓 이벤트(0.9초)가 오는 때(1배속 ms) — 에피카 갈래별 에피콘(10-17 사용자)
            public UltFit[] Pick;              // 이 가운데 하나를 무작위로(에피카 — 2026-10-16 사용자 「세 모션 한 번에 말고 셋 중 하나만」)
            public Dictionary<string, float> Cap;   // 장 이름 조각 → 최대 초(UltPart.Cap)
            public string[] Drop;              // 이름에 이 조각이 든 장은 뺀다(고르지 않은 갈래의 장)
            public string[] Chain;             // 이어 틀 조각(첫 것 = 시작 조각) — 갈래처럼 보이는 조각들을 원작은 차례로 잇는다(에피카). 장도 갈래 하나로 거르지 않는다
            public int[] PetHits;              // 곁 소환수 던지기 박자(몸짓 ms) — PetMs 대신(에피카, fix_Y 3)
            public string[] Rep;               // 이름 조각 → 타격마다 다시 튼다(투사체 · 조준선, UltPart.Rep)
            public string Note;
        }
        class FxFix { public string At; public int Ms = -1; public float Scale = 1f; public int[] More; public FxFix(string at, int ms = -1, float scale = 1f) { At = at; Ms = ms; Scale = scale; } }   // More = 같은 장을 더 틀 시각들(몸짓 ms)   // At 끝의 「!」 = 장 안 좌표 판별보다 우선

        // 길이를 줄이지 않는 사도 — 가운데 타격만으로 길다(원작 길이 그대로, 사용자 2026-10-06)
        //   리뉴아는 뺐다(2026-10-13 사용자 「초반 시전 준비 시간을 줄이면 되긴 해」 — 맞춤 표 Skip · End 로 약 6.4초)
        static readonly HashSet<string> ULT_KEEP_LONG = new HashSet<string> { "키샤" };

        static int[] H(params int[] ms) => ms;
        static Dictionary<string, FxFix> FS(params (string part, string at, int ms, float scale)[] l)
        {
            var d = new Dictionary<string, FxFix>();
            foreach (var x in l) d[x.part] = new FxFix(x.at, x.ms, x.scale);
            return d;
        }
        static Dictionary<string, FxFix> FM(params (string part, string at, int ms, float scale, int[] more)[] l)   // 같은 장을 여러 번(란 1_2 마다 · 클로에 곰 셋)
        {
            var d = new Dictionary<string, FxFix>();
            foreach (var x in l) d[x.part] = new FxFix(x.at, x.ms, x.scale) { More = x.more };
            return d;
        }
        static Dictionary<string, FxFix> F(params (string part, string at, int ms)[] l)
        {
            var d = new Dictionary<string, FxFix>();
            foreach (var x in l) d[x.part] = new FxFix(x.at, x.ms);
            return d;
        }

        static readonly Dictionary<string, UltFit> ULT_FIT = new Dictionary<string, UltFit>
        {
            // <<FIT-TABLE (fit_table.py 가 만든다 — 손으로 고치면 다음에 덮인다)
            ["니콜"] = new UltFit { Hits = H(2133), Move = "dash", Go = 1600, Land = 2100, Ret = 3000, Back = 350, Dest = "middle", Note = "로켓 돌진 → 폭발 1타(1_3 첫 칸) → 반동으로 제자리" },
            ["림_혼돈"] = new UltFit { Hits = H(2100, 4667), Move = "teleport", Go = 1150, Land = 1900, Ret = 5500, Back = 0, Dest = "behind", Note = "포털로 사라져 대상 뒤 위에서 큰 베기 → 넘어가 뒤에 선 채 둘째 베기" },
            ["베니_베니"] = new UltFit { Hits = H(2500), Move = "leap", Go = 1000, Land = 2300, Ret = 3500, Back = 300, Dest = "front", Note = "높이 뛰어 대상 위에서 꿀단지 내려찍기(41%)" },
            ["아멜리아_R41"] = new UltFit { Hits = H(3933), Fx = F(("explosion", "target", -1)), Note = "포 소환 · 장판 · 레이저 뒤 빔 폭발(이벤트 3933)" },
            ["빅우드"] = new UltFit { Move = "none", Fx = F(("heart", "caster", 4100)), Note = "제자리 · 바닥 기운 뒤 한참 지나 하트 회복(이벤트 4100)" },
            ["스패럿"] = new UltFit { Hits = H(1575, 2048, 2520, 2992, 3465, 3938, 4410, 4882, 5512), Note = "배 돌격 뒤 3초 동안 포격 연타 · 마지막 큰 폭발 따로" },
            ["바롱"] = new UltFit { Hits = H(4033), Add = new[] { "fx_barong_ultimate_thunder_1", "fx_barong_ultimate_burn_lightning_1" }, Note = "휘두르기 끝난 뒤 분홍 번개 폭발(이벤트 4033)" },
            ["요미"] = new UltFit { Fx = FS(("ultimate_3", null, 1100, 0.6f), ("ultimate_2", null, 1100, 1f)), Hits = H(2300, 2700, 3100, 3500, 3900, 4300),   // (3차) 달빛은 1.1초 요미 뒤에 생겨 쓸고 나감(UltFx.YOMI_SWEEP) · 타격 6번
                Note = "2026-10-14 원작 대조: 시전자 쪽 큰 흰 초승달이 원작보다 크고 오래 — 0.6배",
                Cap = new Dictionary<string, float> { ["ultimate_1"] = 3.0f, ["ultimate_3"] = 3.3f, ["ultimate_2"] = 3.3f, ["ultimate_proj_1"] = 3.0f } },   // (10-17 원작 대조) 반복 장(달빛 · 보라 막) — 원작은 4.6초에 걷힌다, 우리 고학년이 길어 화면 끝까지 덮었다
            // (3차) 키 「ground_1」이 endground_1 에도 걸려 endground 줄이 안 먹었다 · 구(ultimate_ground)는 이드 위
            ["이드"] = new UltFit { Move = "none", Fx = F(("bodycast_1", "caster", 367), ("endground", "target!", 3500), ("ultimate_ground_1", null, 1867), ("ultimate_ground", "caster!", 1500)), Hits = H(2233, 2733, 3233, 3733), Cap = new Dictionary<string, float> { ["ultimate_ground"] = 1.8f }, Note = "(10-17 원작 대조) 반복 바닥 장(보라 타원 · 비눗방울 구) — 원작은 3.6초 뒤 없다" },
            // 에피카 — Ultimate1_2 = 자동차(MadMax) · 1_3 = 해적선(Pirate) · 1_1 = 유령 말(Gandalf) + 끝 착지(스파인 첨부 표본 2026-10-15). 갈래가 아니라
            //   원작은 1_2 앞 2.3초 → 1_3 의 0.9~3.1초 → 1_1 의 6.6~10초를 잇는다(합 7.9초 = 원작 영상). 이은 시계: 1_2 0~10 · 1_3 10~20 · 1_1 20~30초
            //   (10-16 사용자) 셋을 잇지 말고 고학년마다 하나만 무작위로 — 조각마다 제 시계(ms), 고르지 않은 단계 장은 뺀다. 예전 잇기 값: 1_2 앞 2.3초 · 1_3 0.9~3.1초 · 1_1 6.6~10초
            ["에피카"] = new UltFit
            {
                Pick = new[]
                {
                    // (fix_Y 3) 갈래마다 스파인 맞음 이벤트(1006411, 200ms 간격)대로 — 투사체 · 소환수 던지기를 맞음마다(Rep · PetHits), 이벤트 없는 가운데는 Skip
                    // (10-18 원작 대조) 맞음은 1006411 대로 867~2467 200ms 9번(전엔 4번 · 값이 이벤트와 달랐다) · 사막 바닥 원 · 불기둥 · 미니카는 시전자 쪽(키 「ultimate_desert」 하나가 다 잡아 300ms 에 적 쪽이던 것 — 구체 키 먼저)
                    new UltFit { Anim = "Ultimate1_2", PetSkin = "Normal2", PetAnim = "Attack1_2", PetHits = H(867, 1067, 1267, 1467, 1667, 1867, 2067, 2267, 2467), Hits = H(867, 1067, 1267, 1467, 1667, 1867, 2067, 2267, 2467), Skip = H(2600, 9000),
                                 Rep = new[] { "ultimate_proj_firebomb" },
                                 // (10-18 검증_1) 붉은 바닥 원(desert_ground_2)은 입자 수명 0.6초 한 번 터짐 — 장 길이 · Cap 이 자른 것이 아니다. 원작은 400ms 박자 이벤트(1006421: 867 · 2000 · 2400 · 2800 …)마다 다시 튼다
                                 //   (세 갈래 모두 박자 이벤트가 하나씩 — 1006419 · 1006421 · 1006428 — 그 갈래 ground_2 로 본다). 자르기(Skip 2600~) 안의 2000 · 2400 만
                                 Fx = FM(("ultimate_desert_fire", "caster!", 867, 1f, null), ("ultimate_desert_ground_2", "caster!", 867, 1f, new[] { 2000, 2400 }), ("ultimate_desert_ground", "caster!", 867, 1f, null), ("ultimate_desert_minicar", "caster!", 1267, 1f, null),
                                         ("ultimate_desert", null, 300, 1f, null), ("ultimate_guitar", "caster", 900, 1f, null), ("ultimate_jump", null, 9100, 1f, null), ("ultimate_buff_top", null, 9200, 1f, null)),
                                 Drop = new[] { "ultimate_sea", "ultimate_proj_plunger", "ultimate_battlefield", "ultimate_proj_arrow", "ultimate_horn", "ultimate_violin" }, Note = "자동차(MadMax) — 사막 · 기타 · 화염병 매 맞음" },
                    new UltFit { Anim = "Ultimate1_3", PetSkin = "Normal3", PetAnim = "Attack1_3", PetHits = H(1500, 1630, 1800, 2270, 2500, 2800, 3100, 3400), Hits = H(1500, 1630, 1800, 2270, 2500, 2800, 3100, 3400), Skip = H(3600, 9000),
                                 Rep = new[] { "ultimate_proj_plunger" },
                                 Fx = FS(("ultimate_sea_ground", "caster!", 1000, 1f), ("ultimate_sea", null, 1000, 1f), ("ultimate_violin", "caster", 1000, 1f), ("ultimate_jump", null, 9100, 1f), ("ultimate_buff_top", null, 9200, 1f)),   // (10-18) 바닥 장(sea_ground)은 시전자 쪽 — 자동차 갈래와 같은 꼴, 시각은 그대로
                                 Drop = new[] { "ultimate_desert", "ultimate_proj_firebomb", "ultimate_battlefield", "ultimate_proj_arrow", "ultimate_guitar", "ultimate_horn" }, Note = "해적선(Pirate) — 바다 · 바이올린 · 뚫어뻥 매 맞음" },
                    new UltFit { Anim = "Ultimate1_1", PetSkin = "Normal1", PetAnim = "Attack1_1", PetHits = H(1070, 1270, 1670, 1940, 2070, 2270, 2570, 2770, 2870), Hits = H(1070, 1270, 1670, 1940, 2070, 2270, 2570, 2770, 2870, 9200), Skip = H(2900, 8700),
                                 Rep = new[] { "ultimate_proj_arrow" },
                                 Fx = FS(("ultimate_battlefield_ground", "caster!", 800, 1f), ("ultimate_battlefield", null, 800, 1f), ("ultimate_horn", "caster", 900, 1f), ("ultimate_jump", null, 9100, 1f), ("ultimate_buff_top", null, 9200, 1f)),   // (10-18) 바닥 장(battlefield_ground)은 시전자 쪽 — 자동차 갈래와 같은 꼴, 시각은 그대로
                                 Drop = new[] { "ultimate_desert", "ultimate_proj_firebomb", "ultimate_sea", "ultimate_proj_plunger", "ultimate_guitar", "ultimate_violin" }, Note = "유령 말(Gandalf) — 맞음 9번 + 착지(Skip 2.9~8.7 은 영상이 건너뛰는 가운데)" },
                },
                Note = "자동차 · 해적선 · 유령 말 중 하나(무작위)",
            },
            ["아르코"] = new UltFit { Hits = H(1766, 2266, 2733, 3133, 3633, 4566, 4966, 5500), Note = "착지 뒤 펀치 8타 약 0.45초 간격(1_2 이벤트)" },
            ["스피키_메이드"] = new UltFit { Hits = H(2467, 3134, 3801, 4468, 5135), Skip = H(5602, 8937), Note = "총 겨눔(2.1초) 뒤 순간이동 사격 5번(원작) — 우리 고리 10번 중 뒤 5번은 건너뜀" },
            ["리뉴아"] = new UltFit { Cap = new Dictionary<string, float> { ["ultimate_ground_"] = 4.2f },   // (10-16) 바닥 장 1~3 모두(Patten · Uline 다각형 이미터를 같이 품는다) — 4.6초부터 다각형 조각 단계 — 원작은 그때 클로즈업이 덮는다, 우리는 클로즈업을 건너뛰어 회색 다각형 바닥이 드러났다
                Fx = FS(("ultimate_loop_1", "caster!", -1, 1f), ("ultimate_ground_", "caster", -1, 1.8f)), Hits = H(3333, 4233, 11633, 11967, 12467, 12967, 13467, 13967), Skip = H(4600, 11300), End = 14400, Speed = 1.2f, Note = "2026-10-13 사용자: 시전 준비를 줄인다 — 시계 장판 뒤 이벤트 없이 떠 있는 6.7초를 건너뛰고 연타(이벤트 11633~13967)로 · 11.8→약 6.4초" },
            ["아사나"] = new UltFit { Hits = H(2333, 3333, 4333, 5333, 6333, 7933), Move = "dash", Go = 7200, Land = 7800, Ret = 9300, Back = 300, Dest = "front", Note = "연꽃 속 제자리에서 1초 간격 5타 → 마지막 직전에만 뛰어들어 큰 폭발" },
            ["그윈"] = new UltFit { Hits = H(1400, 1799, 2024, 2233, 2407, 2616, 2841, 2927, 3015, 3622), Move = "none", Note = "10타를 0.4~0.6초 간격으로 넓히고 마지막 큰 휘두르기는 따로" },
            ["다야"] = new UltFit { Hits = H(1838), Fx = F(("ground_ready", "caster", -1), ("sneeze_ground", "caster", 1522)), Note = "날아오른 뒤 하얀 폭발에 타격 · 재채기 구름은 발밑" },
            ["로네_시장"] = new UltFit { Hits = H(1699, 2119, 2539, 2959), Note = "군중 타격 간격 0.4초" },
            ["리스티"] = new UltFit { Hits = H(1470, 1719, 1969, 2218, 2468, 2716, 2966, 3215, 3465, 3990), Note = "블록이 펼쳐진 뒤 연타 · 마지막 큰 블록은 0.5초 떨어져" },
            ["마고"] = new UltFit { Hits = H(2362), Fx = F(("target", "target", 1312)), Note = "표식 먼저 · 1초 뒤 양이 내려찍음" },
            ["메죵"] = new UltFit { Hits = H(1260) },
            ["베루"] = new UltFit { Hits = H(1155) },
            ["타이다"] = new UltFit { Hits = H(1082) },
            ["레이지"] = new UltFit { Hits = H(756) },
            ["파트라"] = new UltFit { Hits = H(1050), Move = "none", Note = "제자리 · 천벌 낙뢰 1타" },
            ["아멜리아"] = new UltFit { Hits = H(1575, 1759, 1942, 2126, 2310, 2494, 2678, 2861, 3045), Note = "충전 뒤 곧 쏨 · 파란 레이저와 주황 빔 1.4초" },
            ["알레트"] = new UltFit { Move = "none", Fx = F(("stomp", "caster", -1), ("trace", "caster", -1)), Note = "제자리 보호막" },
            ["에스피"] = new UltFit { Hits = H(1470, 1554), Note = "사라졌다 대상 위에 나타나자마자 유령 폭발(스파인 숨는 창 733~1366 끝)" },
            ["코미"] = new UltFit { Hits = H(3622), Move = "dash", Go = 3150, Land = 3496, Ret = 4515, Back = 300, Dest = "front", Note = "제자리에서 캔 먹고 뛰어올라 내려앉으며 타격" },
            ["폴랑"] = new UltFit { Hits = H(1732), Note = "병사 · 대포가 오고 겨눈 뒤 발사" },
            ["헤일리_멀쩡"] = new UltFit { Hits = H(1418, 1785, 2152, 2520, 2888), Move = "none", Fx = F(("buff", "caster", 3150)), Note = "전함 · 병사 내려와 겨눈 뒤 사격 5타 · 버프는 끝에" },
            ["네르"] = new UltFit { Hits = H(2919), Move = "none", Fx = F(("ultimate_03", "caster", 840), ("ultimate02", "caster", -1)), Note = "제자리에서 황금 나무가 다 자란 뒤 1타" },
            ["델리아"] = new UltFit { Hits = H(1396), Move = "none", Note = "펭귄 소환 뒤 펭귄이 돌진 · 델리아는 제자리" },
            ["리츠"] = new UltFit { Hits = H(423, 1092, 1942), Move = "dash", Go = 1680, Land = 1890, Ret = 2992, Back = 300, Dest = "front", Note = "1~2타 제자리 땅 칼날 · 3타만 뛰어듦" },
            ["마에스트로2호"] = new UltFit { Hits = H(1522), Move = "none", Note = "제자리 충전 뒤 충격파" },
            ["모모"] = new UltFit { Hits = H(1208, 1470, 1732, 2016), Move = "teleport", Go = 525, Land = 945, Ret = 3098, Back = 0, Dest = "behind", Fx = F(("linelightning", "target", -1), ("linetotarget", "target", -1)), Note = "사라져 적 너머로 · 세로 번개 4타 · 2.9초에 돌아옴" },
            ["바리에"] = new UltFit { Fx = F(("energy", "caster", 105), ("book", "caster", 2625), ("_buff", "caster", 3675)), Note = "충전 → 책 → 버프 단계" },
            ["슈로"] = new UltFit { Hits = H(1974, 2625, 3160), Move = "dash", Go = 1785, Land = 1974, Ret = 3423, Back = 300, Dest = "front", Note = "제자리 바람 준비 뒤 1타에 뛰어듦 · 3타 직후 복귀" },
            ["실피르"] = new UltFit { Hits = H(1260, 1396), Note = "날아올랐다 내려와 얼음 광선(숫자 1~2번)" },
            ["엘레나"] = new UltFit { Hits = H(766, 1071, 1376, 1680, 1984, 2310), Fx = F(("drone_spawn", "target", -1)), Note = "드론 빛기둥은 적 위 · 연타 빨리 · 폭발로 끝" },
            ["잉클"] = new UltFit { Hits = H(1732), Note = "글씨 쓰기 뒤 자기 번쩍 → 적 분홍 폭발" },
            ["칸나"] = new UltFit { Hits = H(1942), Fx = F(("fire_1", "caster", 1109)), Note = "바주카 발사(총구) 뒤 약 0.8초에 양 폭발" },
            ["큐이"] = new UltFit { Fx = F(("heal", "caster", 735)), Note = "회복 빛 0.33" },
            ["카렌"] = new UltFit { Fx = F(("heal", "caster", 420)), Note = "회복 고리 0.2" },
            ["티그_영웅"] = new UltFit { Fx = FS(("ultimate_slash_1", "target!", 2200, 2.5f), ("ultimate_cros_1", "caster", 1200, 1f)), Hits = H(2362), Move = "teleport", Go = 2152, Land = 2258, Ret = 3150, Back = 0, Dest = "front", Note = "섬광 직후 베어 들어감" },
            ["피라"] = new UltFit { Hits = H(664, 978, 1298, 1648, 1961, 2275, 2610, 3465, 3832, 4200, 4568, 4935), Note = "1막 7타 · 2막(큰 고양이) 5타" },
            ["네티"] = new UltFit { Move = "none", Fx = F(("neti_ultimate_1", "caster", 1342)), Note = "빛구슬은 사도 머리 위에서" },
            ["디아나_왕년"] = new UltFit { Cap = new Dictionary<string, float> { ["ultimate_ground_1"] = 1.5f },   // (10-16 원작 대조) 원작은 회색 바람 고리가 2.6~2.8초에 잠깐 — 대지 장을 고학년 끝까지 두면 3~4초까지 남았다
                Fx = FS(("ultimate_cast_1", "caster", -1, 2f), ("ultimate_kick_1", "caster", -1, 2f), ("ultimate_ground_1", "target!", -1, 1f), ("ultimate_hit_1", null, -1, 2.5f), ("ultimate_scranim_1", null, 3050, 1f)),   // scranim = 붉은 화면 막 — 원작 2.8~3.0초에 덮고 3.2초에 흩어진다(전엔 0.4초 늦었다)
                Hits = H(2933), Move = "dash", Go = 1800, Land = 2900, Ret = 4533, Back = 300, Dest = "front", Note = "(10-18 사용자 2차 「도끼 끌며 흩뿌리다 터져야 · 몸짓 끊김」) 끌기(스파인 1.8~2.9초) 동안 끌며 다가가 2933 내리찍기에 폭발 · 몸짓 끝(4533)까지 다 하고 복귀(End 없앰) · 대지 파임은 적 자리 고정(10-16). 예전: Hits 2520 · Go 1522 · Land 1816 · Ret 3000 · End 3400" },
            ["레테"] = new UltFit { Move = "none", Note = "제자리 빛줄기" },
            ["림"] = new UltFit { Hits = H(1732, 2562), Note = "뛰어들며 큰 베기 · 0.8초 뒤 X자 베기" },
            ["셀리네"] = new UltFit { Hits = H(1312), Move = "none", Note = "제자리 리본 춤 뒤 번쩍" },
            ["스노키"] = new UltFit { Hits = H(1155), Move = "leap", Go = 294, Land = 1050, Ret = 3465, Back = 300, Dest = "front", Fx = F(("slash_3", "caster", 525)), Note = "뛰어올라 금빛 고리에 머물다 내리꽂음" },
            ["시온더다크불릿"] = new UltFit { Hits = H(2433), Fx = FS(("ultimate_dance_1", "caster", 467, 1f), ("ultimate_dance_2", "caster", 1000, 1f), ("ultimate_dance_3", "caster", 1500, 1f), ("ultimate_cast_1", "caster", 467, 1f),
                    ("ultimate_explosion_1", "floor", 2433, 1f), ("ultimate_explosion", "target!", 2433, 1f), ("ultimate_fire_1", "target!", 2433, 1f), ("ultimate_ground_1", "floor", 2433, 1f), ("ultimate_smoke_1", null, 2433, 1f)), Note = "(10-18 사용자 「시온 발 밑이랑 맞춰줘」 — 바닥 불꽃 벽 explosion_1 · ground_1 은 가로 적 · 높이 시온 발(floor), 구체 키 먼저) · (10-17 사용자 「포즈마다 기 모으다 마지막 불길 임팩트로 피해」 · 원작 이벤트 467 · 1000 · 1500 · 2433) · 푸른 고리 모으기 뒤 베기 · 2026-10-14 원작 대조: dance(파란 리본 · 불꽃)는 시전자를 감는다" },
            ["아라그니아"] = new UltFit { Hits = H(1155, 1680, 2205), Fx = F(("laser", "target", -1)), Note = "소용돌이 뒤 첫 충격 · 물기둥은 적 자리" },
            ["아이시아"] = new UltFit { Hits = H(262, 3045), Fx = F(("jump", "caster", -1), ("explosion", "target", 2468)), Note = "번쩍 직후 작은 구슬 · 큰 폭발은 둘째 타격" },
            ["에슈르"] = new UltFit { Hits = H(1925, 2028, 2782), Fx = F(("proj_2", "target", -1)), Note = "첫 폭발 1초 뒤 작은 폭발 한 번 더" },
            ["오르"] = new UltFit { Fx = F(("orr_ultimate_1", "caster", 998)), Note = "돔은 사도를 덮고 일찍" },
            ["캐시"] = new UltFit { Hits = H(4400), Move = "dash", Go = 2900, Land = 3150, Ret = 5600, Back = 300, Dest = "front", Fx = F(("kathy_ultimate_1", "target", 3333)), Note = "내려앉은 뒤 기둥 · 큰 폭발 1타 · 붉은 원이 남는 동안 머묾" },
            ["키디언"] = new UltFit { Hits = H(1400, 2446), Move = "teleport", Go = 715, Land = 1065, Ret = 2898, Back = 0, Dest = "front", Note = "찌르기 두 번(1초 간격) 뒤 내려섬" },
            ["하이디"] = new UltFit { Hits = H(2048, 2152, 2258, 2362, 2468, 2625), Move = "dash", Go = 735, Land = 1575, Ret = 2730, Back = 300, Dest = "front", Fx = F(("news", "target", 1995)), Note = "굴러 들어가 큰 번쩍과 함께 몰아침 · 돌아옴" },
            ["네르_빡침"] = new UltFit { Fx = FS(("ultimate_charge_2", null, 1900, 1f), ("ultimate_1", null, 2967, 1f)), Hits = H(2967),   /* (10-15) 장 시각 = 스파인 이벤트 1900(수정 깨짐 프리즘) · 2967(타격) — 전엔 셋 다 0초에 떠 0.2초에 무지개 판 */ Move = "none", Speed = 1.35f,   /* (10-17 사용자 「네르 빡침 다가감」) 후열 원거리 — 도약 없앰 */ Note = "수정 장막 뒤 도약 타격 · 0.2초 만에 복귀 · 2026-10-14 배속 1.35(몸짓 4.67초 → 원작 영상 3.37초, 타격 3.3 → 2.5초 — 1.05배속이라 슬로모션처럼 보였다)" },
            ["리코타"] = new UltFit { Hits = H(2362, 2553, 2786, 3033), Move = "none", Fx = F(("slash_1", "target", -1)), Note = "제자리 긴 베기 · 이른 베기는 첫 타격에" },
            ["마요"] = new UltFit { Hits = H(945, 1680, 1890, 2152, 2415, 2678, 2940, 3150), Note = "단발 28% → 구름 다단이 끝 직전까지" },
            ["버터"] = new UltFit { Hits = H(2415), Note = "새총 당기기 1초 뒤 발사" },
            ["벨벳"] = new UltFit { Move = "dash", Go = 383, Land = 673, Ret = 3200, Back = 300, Dest = "front", Note = "마지막 타격 직후 복귀" },
            ["슈팡"] = new UltFit { Hits = H(1418), Move = "dash", Go = 1208, Land = 1418, Ret = 2415, Back = 300, Dest = "front", Note = "노란 돌진이 닿는 때" },
            ["시스트"] = new UltFit { Fx = F(("sist_ultimate", "caster", -1)), Note = "지폐비 · 오라는 시스트 자신에게" },
            ["아네트"] = new UltFit { Hits = H(860, 982, 1105, 1226, 1349, 1471, 1593, 1716, 1838, 1960, 2082, 2205), Note = "다단은 65% 지점까지" },
            // (10-18 3차 「씨앗이 떨어지지 않는다」) 씨앗 장은 입자 속도 곡선(y × 50 — 수명 0.648 까지 0, 1.0 에 −0.287)으로 수명 0.91초부터 떨어진다. 원작 고학년.mp4 1.7 → 1.95초에 머리 위에서 몸으로 떨어지고 2.0초 터짐(떨어지기 시작 ~ 터짐 ≈ 몸짓 0.33초).
            //   1000 에 틀면 떨어지기 시작(1910)이 터짐(1900)과 겹쳐 떠 있다가 터지기만 했다 → 1900 − 910 − 330 = 660 에 틀어 터지기 전에 떨어지게
            ["아야"] = new UltFit { Hits = H(1900), Fx = FS(("ultimate_seed_1", "target!", 660, 1f), ("ultimate_explosion_1", null, 1900, 1f)), Note = "(10-17 사용자 「적마다 얼음 씨앗 → 만개 한 번」 · 원작 이벤트 1000 · 1900) · 연꽃 두 번에 걸쳐 약 0.5초 간격 · (10-18 3차) 씨앗 660" },
            ["칸타"] = new UltFit { Hits = H(1418, 3150, 3549), Move = "dash", Go = 1102, Land = 1365, Ret = 1838, Back = 300, Dest = "front", Note = "대시 베기 뒤 곧 복귀 · 폭발은 집에서" },
            ["클로에"] = new UltFit { Anim = "Skill2_1", Move = "none",   // (fix_Y 4) 서서 웃는 Skill2_1(Acts 의 Anim 덮어쓰기) — 전엔 세바스티안 타기(Skill1_1)
                Fx = FM(("ultimate_proj_1", null, 1300, 1f, new[] { 1500, 1700 }), ("ultimate_hit_1", "target!", 2100, 1f, null)),
                Hits = H(2133, 2600, 3267, 3767, 4133, 4767), Note = "제자리 웃음 — 곰 셋이 왼쪽에서 달려와 적 발밑으로(1.3~2.1초) · 맞음 6번(10-17 정밀 분석)" },
            ["비비_신성"] = new UltFit { Hits = H(4400, 5300), Fx = F(("full_default", "caster", 1900), ("boll", "target", 5150), ("full_end", null, 5900)), End = 7300, Note = "2026-10-14 원작 영상(×1.2 = 스파인 ms): 시전 1.4초 → 화면 위로 날아올라 사라짐(1.45~2.4) → 금빛 고리(1.9) → 큰 스탠딩(2.6~5.8 · ULT_STANDING) 손에서 레이저(3.95) · 맞음(4.4) → 빛기둥(5.15~5.75) → 소용돌이(5.9) → 내려와 제자리(6.4~7.2) · 건너뛰기 없앰(날아오름을 살림) · 끝 대기만 7.3초에서" },
            ["실라"] = new UltFit { Move = "dash", Go = 1575, Land = 1785, Ret = 2100, Back = 200, Dest = "front", Note = "짧게 달려들어 휘두르고 곧 돌아옴" },
            // (fix_Y 2) 기 모음(charge_1 무지개 번쩍)은 원작 0.0~0.7 에 없다 — 빼고 금빛 기둥(cast_1)을 이벤트 567 에. 가운데 「가까이 찍기」는 원작 영상이 압축(Skip)
            //   Skip 길이: fix_Y 는 1배속 가정으로 1.08초(3750~4830) — 배속표(1.31) 아래에선 2933→5366(2.43초)이 영상 1.27초가 되려면 0.77초라 3750~4520
            ["에르핀_왕도"] = new UltFit { Drop = new[] { "ultimate_charge_1" },
                Fx = F(("cast_1", "caster", 567), ("jump_1", "caster", 1933), ("drop_1", "target!", 2800), ("crash_1", "target!", 2933), ("crash_aura_1", "target!", 2933),
                       ("shockwave_1", "target!", 2933), ("explosion_1", "target!", 5400)),   // 주먹 찍기 = 폭발 자리 · 거의 동시, 반동 복귀는 그 뒤(원작 스파인: 1.93 도약 → 2.93 내려찍기 → 5.43 폭발)
                Hits = H(2933, 3200, 3467, 3733, 4000, 4267, 4533, 4733, 5433),   // (10-18 사용자 2차 「8회 범위 피해 동안 주먹 계속, 마지막 터지며 날아감」) 원작 1_2 주먹 8번(1012323 ×7 + 1012324) + 1_3 폭발(1012348) — 전 H(5366) 한 번 · Skip 3750~4520 이 주먹 구간을 잘랐다
                Move = "leap", Go = 2000, Land = 2033, Ret = 5366, Back = 350, RetHidden = true, RetFly = true, Dest = "on", End = 7800, Note = "(10-18 사용자 2차) Skip 없앰 · 숨은 틈 끝(5366)에 폭발과 함께 제자리로 날려 감(RetFly — 몸짓 1_3 그대로). 주먹마다 먼지(drop_1 짝)는 모름이라 안 넣음 · (10-18 사용자 「공중 순간이동 · 터짐과 날아감 어긋남」) 원작 숨은 틈(pose.hidden 2000~2033 · 4833~5366)에 옮긴다 — 2016 적 위로, 4850 제자리로(폭발과 함께 제자리에서 굴러 나옴) · (10-17 원작 영상) 날아올라 적 위에 내리찍고 반동으로 돌아온다 · 첫 빛기둥은 사도 자리 · 둘째 빛기둥(jump_1)도 뛰어오르는 자리(대상 쪽이면 화면 오른쪽 밖) · 꼬리를 원작 6.21초(영상 한 벌)에 맞춰 6500에서 끊음(1.05배속 → 6.19초)" },
            ["바나"] = new UltFit { Fx = F(("happy", "caster", 4830), ("hit_1", "caster", -1)), Note = "불꽃은 모루 위 · 완성 버프(happy)는 끝에" },
            ["피코라"] = new UltFit { Fx = F(("ultimate_up", "caster", -1)), Note = "하트 방울은 사도를 감쌈" },
            ["로네"] = new UltFit { Move = "none", Note = "제자리 깃발" },
            ["비비"] = new UltFit { Hits = H(1167, 2067), Move = "teleport", Go = 1800, Land = 2000, Ret = 2600, Back = 300, Dest = "front",   // (fix_Y 7) 0.4초 순간이동 → 제자리에서 날아올라 창을 던지고 1.9초에 내려와 밟음
                Fx = F(("fire_1", "caster", 150), ("jump_1", "caster", 450), ("jump_screw", "caster", 900), ("proj_1", null, 830), ("jump_2", "target!", 1167), ("stomp", "target!", 2067)),
                Note = "제자리에서 날아올라 창(830 발사 → 1167 맞음) → 장판 1167~ → 1.9초(스파인) 내려와 밟음" },
            ["셰럼"] = new UltFit { Hits = H(2782, 3518, 4358), Move = "none", Note = "제자리 그리기 · 큰 타격 3번 0.7~0.8초 간격" },
            ["시저"] = new UltFit { Hits = H(1024, 1601) },
            ["에슈르_마도"] = new UltFit { Hits = H(2079), Note = "마법진 준비 뒤 불덩이 1타" },
            ["쥬비"] = new UltFit { Hits = H(861, 929, 998, 1066, 1134, 1202, 1270), Note = "꿀 폭발 뒤 0.4초 안 연타" },
            ["캬롯"] = new UltFit { Hits = H(3990, 4058, 4126, 4195, 4263, 4331, 4400, 4468, 4536, 4604, 4672, 4741), Note = "빛기둥이 내려온 뒤 큰 폭발" },
            ["포셔"] = new UltFit { Hits = H(2258), Note = "휘두른 뒤 분홍 폭발" },
            ["나이아"] = new UltFit { Hits = H(2468), Note = "파도가 들어와 0.8초 뒤 침" },
            ["다야_퓨어샤인"] = new UltFit { Hits = H(2362, 2625, 2888, 3135, 3398, 3631), Fx = F(("explosion_2", "target", 3675)), Note = "뜨고 모은 뒤 연타 · 마지막 큰 폭발" },
            ["레비"] = new UltFit { Hits = H(1628), Move = "dash", Go = 1312, Land = 1575, Ret = 3340, Back = 300, Dest = "front" },
            ["로니"] = new UltFit { Hits = H(1102, 1470, 1838, 2205, 2572, 3045) },
            ["리온"] = new UltFit { Hits = H(722, 1250, 2800) },
            ["마리"] = new UltFit { Hits = H(2415) },
            ["멜루나"] = new UltFit { Hits = H(2415) },
            ["벨라"] = new UltFit { Hits = H(1000),   // (fix_Y 5-2) hit_a 는 적 발밑(아군 판 끔) · cast_2 는 시전자 + 아군(2133) · end 보호막은 3500
                Fx = F(("ultimate1_lantern", "caster", 800), ("ultimate1_hit_a", "target!", 1000), ("ultimate1_hit_e", "target!", 1100), ("ultimate1_cast_1", "caster", 1700),
                       ("ultimate1_proj_a", "caster", 1800), ("ultimate1_cast_2", "ally", 2133), ("ultimate1_end", "caster", 3500)),
                Note = "원작 1.0 적 발밑 폭발 → 1.1~1.6 기둥 → 1.7 십자 섬광 → 2.3 아군 섬광 → 3.4 시전자 보호막" },
            ["셰이디_역전"] = new UltFit { Hits = H(1890, 2940) },
            ["스피키"] = new UltFit { Fx = F(("raady", "caster", -1), ("ready", "caster", -1), ("circle", "caster", -1), ("ground", "caster", -1), ("pumpkin", "target", 3255)) },
            ["실비아"] = new UltFit { Hits = H(1400, 1500), Move = "none", Fx = F(("ultimate_statue", "target!", 600), ("ultimate_hit", "target!", 1350)),   // (10-17 정밀 분석) 분수는 적 무리 가운데 고정 · 맞음 2번
                Note = "2026-10-13 석상이 떨어져 닿는 때(1.25~1.4초)에 둘째 타격 — 원작 영상 피해 1.37초" },
            ["우로스"] = new UltFit { Hits = H(1933, 3100), Fx = F(("dash_jolly", "caster", 1400), ("slash_jolly", "target", 1867), ("fire_jolly", "target", 3050), ("snake_jolly", "target!", 3050), ("hit_jolly", "target", 3100)), Note = "2026-10-14 한 바퀴: 1.5초 앞으로 사라져 적 무리 뒤에 나타남(1.8) → 왼쪽으로 베며 무리를 가로지름(1.93 타격) → 왼끝까지 갔다 제자리(3.0) → 적 자리 불기둥 · 금빛 용(3.1 타격) · 무리 뒤 자리는 ULT_LOOP" },
            ["코미_수영복"] = new UltFit { Hits = H(300, 2000, 2667, 3333, 4000, 4667), Move = "none", Fx = F(("waterfall", "target", 100)), Note = "물기둥이 시작하자마자 침 · 코미는 제자리에서 뛰어오름" },
            ["죠안"] = new UltFit { Fx = F(("explosion_2", "target!", 633), ("wing_1", "caster", 2133), ("explosion_3", "target!", 2233), ("ground_1", "target!", 2233)), Hits = H(2233), Cap = new Dictionary<string, float> { ["explosion_2"] = 1.4f }, Note = "(3차: 적 사슬은 변신(2.13초) 때 딱 걷힌다 — 633 + 1.4초) (10-17 2차 사용자) 사슬은 상대에게 생겨(explosion_2 적) 변신하며 조이며 터진다 · 예전: 변신 전 사슬이 몸을 감았다가(explosion_2 633) 날개 변신(2133)하며 풀리면서 피해(2233)" },
            // (fix_Y 1) 원작은 1_1 한 번 뒤 1_2 세 번(1_2 시작 = 3300 · 5467 · 7634) · 늑대 표식은 skill_ready_1 장 · 달 · X 장은 원점이 화면 가운데(midx)
            ["란"] = new UltFit { Move = "none", Chain = new[] { "Ultimate1_1", "Ultimate1_2", "Ultimate1_2", "Ultimate1_2" }, Speed = 1.32f,   // Speed 는 원작 그대로가 아닐 때만 — 원작 그대로는 배속표(ORIG_SPEED 1.31)
                Hits = H(1500, 1767, 2033, 3733, 4000, 4267, 5900, 6167, 6434, 8067, 8334, 8601),   // 1_1 이벤트 1500 · 1767 · 2033 · 1_2 이벤트 433 · 700 · 967
                Add = new[] { "fx_ran_skill_ready_1_default" },
                Fx = FM(("ultimate_cast_2", null, 33, 1f, new[] { 3333, 5500, 7667 }),   // 금빛 섬광 구(1_1 · 1_2 마다)
                        ("ultimate_cast_4", "midx", 233, 1f, null), ("ultimate_cast_5", "midx", 233, 1f, null),   // 달 · X(장 안 지연 2.0 → 스파인 2.28초)
                        ("ultimate_slash_1", "target!", 1400, 1f, null),   // 이벤트 1467(영상 1.0~1.1) — 세로 섬광 · 고리 베기 여러 겹
                        ("ultimate_slash_2", "target!", 3700, 1f, new[] { 5867, 8034 }),   // 1_2 이벤트 400/433 — 사선 · Z · X 고리
                        ("skill_ready_1", "body", 2300, 1f, new[] { 4533, 6700, 8867 })),   // 늑대 표식 — 이벤트 1009123(1_1 2300 · 1_2 1233)
                Note = "원작: 1_1 한 번 뒤 1_2 세 번(영상 7.5초 = 스파인 9.8초 ÷ 1.32)" },
            ["우이_기억"] = new UltFit { Fx = F(("garden_1", "target!", -1), ("garden_end", "target!", -1), ("garden_start", "caster", -1), ("step_1", "caster", -1)), Hits = H(3200, 4000, 4800), Note = "(10-17 사용자 「상대에게 웅덩이 생기고 피해」) · 2026-10-14 원작 대조: 연못 · 물 돔은 시전자(아군) 쪽 — 적 발밑 연못은 원작에 없다" },
            ["우이"] = new UltFit { Hits = H(1833, 2233, 2633), Move = "none", Fx = F(("flower_1", "caster!", 200)), Note = "2026-10-13 타격이 0ms(몸짓 전)에 들던 것 — 원작 영상 분홍 꽃 폭발(1.8~2.0) · 버섯 연기(2.3)에" },
            ["티그"] = new UltFit { Hits = H(840), Move = "leap", Go = 420, Land = 800, Ret = 3167, Back = 300, Dest = "front", Fx = F(("aura", "caster", 1800)), Note = "뛰어 베기 먼저 · 호랑이 오라는 착지 뒤" },
            ["아일라"] = new UltFit { Hits = H(2433, 3400), Note = "영상: 2.3초 베기는 화상만 · 피해는 4.1초 운석 폭발 → 베기 · 운석 두 박자(운석 장은 없음)" },
            ["롤렛"] = new UltFit { Hits = H(2333), Fx = F(("box", "target", 1300)), Note = "영상: 상자 등장 1.3초 → 걸어가 3.6초 폭발 피해 → 상자 먼저 · 폭발 타격은 둘째 이벤트" },
            ["사리"] = new UltFit { Hits = H(634), Note = "영상: 하늘 번개 한 번(0.4초) 피해 1회 → 둘째 표시 타격 뺌" },
            ["디아나"] = new UltFit { Hits = H(1473, 1547, 1620, 1694, 1767, 1841, 1914, 1988, 2061, 2135, 2208), Fx = F(("diana_ultimate_3", "caster", -1), ("ground_1", "caster", -1)), Note = "영상: 번쩍 직후 0.5초 안에 다단 · 초록 초승달은 디아나 앞에서 쓸고 감(첫 타격 1433 정권은 2026-10-06 사용자 결정대로 둠)" },
            // FIT-TABLE>>
        };

        UltFit UltFitNow;   // 지금 고학년의 맞춤(없으면 null)

        // 7초 넘는 몫(need, 1배속 초)만 줄인다 — 먼저 마지막 타격 뒤 꼬리(0.8초는 남김), 모자라면 첫 타격 앞 준비(처음 0.6초 · 첫 타격 앞 0.8초는 남김).
        // 가운데(첫 ~ 마지막 타격)는 건드리지 않는다. 그래도 남는 몫은 배속(ULT_SPEED_MAX)이 맡는다
        const float KEEP_BACK = 0.8f, KEEP_HEAD = 0.6f, KEEP_PRE = 0.8f;
        void PlanCutNeed(List<float> evs, List<float> offs, float total, List<float> hitsS, float need)
        {
            UltCutFrontMs = 0; UltCutBackMs = 0; UltCutSkipMs = 0;
            if (need <= 0.02f) return;
            evs.AddRange(hitsS); evs.Sort();
            float firstHit = hitsS.Count > 0 ? hitsS[0] : evs.Count > 0 ? evs[0] : 0;
            float lastHit = hitsS.Count > 0 ? hitsS[hitsS.Count - 1] : evs.Count > 0 ? evs[evs.Count - 1] : total;
            float back = Mathf.Min(need, Mathf.Max(0, total - (lastHit + KEEP_BACK)));
            if (back > 0.05f) { cutEnd = total - back; UltCutBackMs = Mathf.RoundToInt(back * 1000); need -= back; }
            if (need > 0.05f && firstHit - KEEP_PRE - KEEP_HEAD > 0.3f)
            {
                int PieceOf(float t) { int p = 0; for (int i = 0; i < offs.Count; i++) if (t >= offs[i]) p = i; return p; }
                float a = KEEP_HEAD, b = Mathf.Min(firstHit - KEEP_PRE, a + need);
                int pa = PieceOf(a), pb = PieceOf(b);
                if (pa != pb && pa + 1 < offs.Count) b = offs[pa + 1] - 0.05f;   // 조각 경계를 넘지 않게(앞 조각 끝까지)
                if (b - a > 0.3f) { cutSkips.Add((a, b)); UltCutFrontMs = Mathf.RoundToInt((b - a) * 1000); }
            }
            UltCutSkipMs = UltCutFrontMs + UltCutBackMs;
        }

        UltFit ultPickFit;   // 이번 고학년에 고른 갈래(UltFit.Pick)
        UltFit UltFitOf(string hid) => !OldSync && hid != null && ULT_FIT.TryGetValue(hid, out var f) ? f : null;

        // 표대로 오가기에 쓸 갈래 — 원래 표(있으면)의 길이 · 소리는 두고 이동만 갈아 끼운다
        static Bolzena.Fx.UltWay FitWay(Bolzena.Fx.UltWay src, UltFit f, int motionMs)
        {
            var w = new Bolzena.Fx.UltWay();
            if (src != null) { w.Names = src.Names; w.MotionMs = src.MotionMs; w.TotalMs = src.TotalMs; w.At = src.At; w.End = src.End; w.Marks = src.Marks; w.Plays = src.Plays; w.HitSnd = src.HitSnd; w.SoundMode = src.SoundMode; }
            else { w.MotionMs = motionMs; w.TotalMs = motionMs; }
            w.MoveType = f.Move; w.GoMs = f.Go; w.LandMs = Mathf.Max(f.Go, f.Land); w.HitMs = f.Hits != null && f.Hits.Length > 0 ? f.Hits[0] : w.LandMs;
            w.ReturnMs = f.Ret >= 0 ? f.Ret : Mathf.Max(w.MotionMs, motionMs); w.BackMs = f.Back; w.Dest = f.Dest ?? "front"; w.HomeMs = null;
            return w;
        }

        // 엔진 타격 k 번을 n 번으로 보이게 — 많으면 나누고(SplitHits), 적으면 묶는다(같은 적의 피해는 하나로 더해 — 합 그대로)
        static SortedDictionary<int, List<BattleEvent>> ReshapeHits(SortedDictionary<int, List<BattleEvent>> byHit, int n)
        {
            if (n <= 0 || byHit.Count == n) return byHit;
            if (byHit.Count < n) return SplitHits(byHit, n);
            var groups = new List<List<BattleEvent>>(byHit.Values);
            int k = groups.Count;
            var res = new SortedDictionary<int, List<BattleEvent>>();
            for (int s = 0; s < n; s++) res[s] = new List<BattleEvent>();
            for (int g = 0; g < k; g++)
            {
                int slot = Mathf.Min(n - 1, g * n / k);
                foreach (var e in groups[g])
                {
                    if (e.Kind == EventKind.Damage && !IsShot(e))   // 쏘는 한 대는 더하지 않고 그대로
                    {
                        var same = res[slot].Find(x => x.Kind == EventKind.Damage && !IsShot(x) && x.Target.Side == e.Target.Side && x.Target.Index == e.Target.Index);
                        if (same != null)
                        {
                            same.Value += e.Value; same.Blocked += e.Blocked; same.HpAfter = e.HpAfter; same.BlockAfter = e.BlockAfter; same.Crit |= e.Crit;
                            continue;
                        }
                        res[slot].Add(new BattleEvent
                        {
                            Kind = e.Kind, Actor = e.Actor, Target = e.Target, Value = e.Value, Blocked = e.Blocked, Crit = e.Crit, HpAfter = e.HpAfter, BlockAfter = e.BlockAfter,
                            FAfter = e.FAfter, Hit = slot, Hits = n, HitKind = e.HitKind, Motion = e.Motion, Text = e.Text, Card = e.Card, Boss = e.Boss, Up = e.Up, Say = e.Say, Anim = e.Anim,
                        });
                    }
                    else res[slot].Add(e);
                }
            }
            return res;
        }

        static readonly System.Text.RegularExpressions.Regex MUZZLE_FIT = new System.Text.RegularExpressions.Regex("(muzzle|_fire_|shot|laser|lazer|beam)");

        // 원작 장 자리 · 시각 고침 — 이 고학년 한 번 동안만(BolzenaFx.UltTweak)
        //   자리: caster · target · midx(화면 가운데) · floor(가로 대상 · 높이 시전자 발 — 시온 바닥 불길) · body(적 몸 가운데) · ally(시전자 + 아군). 「target!」 은 아군 판(_a)도 끈다(벨라 hit_a). More = 같은 장을 더 틀 시각, Rep = 타격마다 되틀기
        System.Func<string, List<Bolzena.Fx.UltPart>, List<Bolzena.Fx.UltPart>> FitTweak(string hid, UltFit f, float speed)
        {
            if (f == null || (f.Fx == null && f.Add == null && f.Drop == null && f.Cap == null && f.Rep == null)) return null;
            return (hero, plan) =>
            {
                if (hero != hid) return plan;
                if (f.Drop != null) plan = plan.FindAll(p => !System.Array.Exists(f.Drop, d => p.Name.Contains(d)));
                if (f.Cap != null) foreach (var p in plan) foreach (var kv in f.Cap) if (p.Name.Contains(kv.Key)) p.Cap = kv.Value;
                if (f.Add != null)
                    foreach (var n in f.Add)
                        if (Bolzena.Fx.FxLibrary.Has(n) && !plan.Exists(p => p.Name == n))
                            plan.Add(new Bolzena.Fx.UltPart { Name = n, At = "target", Dur = Bolzena.Fx.FxLibrary.Duration(n) });
                var extra = new List<Bolzena.Fx.UltPart>();
                foreach (var p in plan)
                {
                    if (f.Rep != null && System.Array.Exists(f.Rep, r => p.Name.Contains(r))) p.Rep = true;
                    foreach (var kv in f.Fx ?? new Dictionary<string, FxFix>())
                    {
                        if (!p.Name.Contains(kv.Key)) continue;
                        // 장 안 좌표가 「시전자 발밑 장」(크게 덮거나 오른쪽으로 뻗음)이면 표의 target 은 따르지 않는다 — 예전 표는 이름 짐작으로 적 자리에 둔 것이 많았다(2026-10-14 원작 대조). 시각은 표대로
                        bool force = kv.Value.At != null && kv.Value.At.EndsWith("!");
                        string fixAt = force ? kv.Value.At.TrimEnd('!') : kv.Value.At;
                        if (kv.Value.Scale > 0 && kv.Value.Scale != 1f && !Bolzena.Fx.BolzenaFx.OriginalOnly) p.Scale = kv.Value.Scale;
                        if (fixAt != null && (force || !(fixAt == "target" && Bolzena.Fx.UltFx.GeoKeepCaster(p.Name))))
                        {
                            if (fixAt == "body") { p.At = "target"; p.Body = true; p.Muzzle = false; }   // 적 몸 가운데(란 늑대 표식)
                            else if (fixAt == "ally") { p.At = "caster"; p.Ally = true; p.Pre = false; }   // 시전자 + 아군(벨라 cast_2)
                            else
                            {
                                p.At = fixAt;
                                p.Muzzle = p.At == "caster" && MUZZLE_FIT.IsMatch(p.Name);   // 시전자 쪽으로 옮긴 총구 불꽃(칸나 fire)은 총구에
                                if (p.At == "caster") p.Body = false;
                                if (fixAt == "target") p.Ally = false;   // 벨라 hit_a — 이름이 _a 로 끝나 아군 판이던 것
                            }
                        }
                        if (kv.Value.Ms >= 0)
                        {
                            float Ms(int ms0) => Rm(ms0 / 1000f) / Mathf.Max(0.01f, speed) * 1000f;   // 몸짓 ms → 화면 ms(줄인 몫 · 배속)
                            p.AtMs = Ms(kv.Value.Ms);
                            if (kv.Value.More != null)
                                foreach (var m in kv.Value.More)
                                    extra.Add(new Bolzena.Fx.UltPart { Name = p.Name, At = p.At, Pre = p.Pre, Muzzle = p.Muzzle, Body = p.Body, Ally = p.Ally, Dur = p.Dur, Scale = p.Scale, Cap = p.Cap, AtMs = Ms(m) });
                        }
                        break;
                    }
                }
                plan.AddRange(extra);
                return plan;
            };
        }

        // ── 고학년 타격감(2026-10-14 원작 영상 대조) ── 원작은 조용히 쌓다가 한두 순간만 화면이 번쩍 · 크게 흔들림 · 어두워짐 · 다가감.
        //   시각은 원작 고학년 영상 ms(= 몸짓 1배속, 맞춤 표와 같은 시계) — 영상에서 밝기 급등(번쩍) · 전체 이동(흔들림)을 재서 얻음(ult_videos/대조_엘다인20_1014.md 「타격감」).
        //   flash 흰 번쩍 + 큰 흔들림 + 멈칫 · red 붉은 번쩍 · dark 화면 어두워짐(1.6초) · shake 큰 흔들림 · zoom 적 쪽으로 다가감
        static readonly Dictionary<string, (int ms, string kind)[]> ULT_PUNCH = new Dictionary<string, (int, string)[]>
        {
            ["란"] = new[] { (2100, "flash") },   // (fix_Y 1) X 베기 — 영상 1.6초 = 스파인 2.1초(전엔 영상 초를 그대로 넣었다)
            ["비비_신성"] = new[] { (2244, "shake"), (2700, "dark"), (5364, "flash"), (5796, "shake") },   // 이 사도 맞춤 표는 영상 × 1.2 = 스파인 ms
            ["에르핀_왕도"] = new[] { (1933, "shake"), (2870, "flash"), (2933, "zoom"), (3300, "shake"), (5366, "flash"), (5800, "shake") },   // (fix_Y 2) 스파인 ms(Rm 으로 Skip 보정) — 전엔 영상 초를 그대로 넣었다
            ["우로스"] = new[] { (2030, "flash"), (2800, "shake") },
            ["티그_영웅"] = new[] { (1500, "flash"), (2130, "flash") },
            ["죠안"] = new[] { (2250, "red"), (3550, "shake") },
            ["네르_빡침"] = new[] { (2500, "flash") },
            ["디아나_왕년"] = new[] { (2933, "flash"), (2967, "shake") },   // (10-18 사용자 2차) 스파인 내리찍기 2933 · 2967 — 전 2250 · 2470 은 도끼보다 먼저 번쩍였다
            ["벨라"] = new[] { (2870, "shake") },
            ["실비아"] = new[] { (470, "shake"), (1470, "shake") },
            ["이드"] = new[] { (2000, "dark"), (3570, "shake") },
        };
        // 고학년 어둠(300) 바로 위 · 맞는 적(316~) · 이펙트(325) · 사도(330) 아래. 전에는 460(모두 위)에 흰 0.8 을 0.4초 덮어
        //   원작의 밝은 순간(보라 X 폭발 · 금빛 기둥 — 사실은 이펙트 빛)이 회색 막에 가렸다(2026-10-15 란 · 우로스 · 티그 · 디아나 대조). 배경만 짧게 밝힌다
        const int PUNCH_ORDER = 305;
        static bool HasUltPunch(string hid) => !Bolzena.Fx.BolzenaFx.OriginalOnly && hid != null && ULT_PUNCH.ContainsKey(hid);   // 원작 그대로면 끈다(영상 보고 덧댄 번쩍 · 흔들림)

        /// <summary>고학년 타격감 표대로 — 몸짓 시작(tStart, 게임 시계)부터 원작 ms 를 줄인 몸짓 시계(Rm) · 배속으로 옮겨 그때 튼다.</summary>
        IEnumerator UltPunchCo(string hid, float speed, float tStart, UnitView u, List<int> targets)
        {
            if (!ULT_PUNCH.TryGetValue(hid, out var list)) yield break;
            foreach (var (ms, kind) in list)
            {
                float at = Rm(ms / 1000f) / Mathf.Max(0.01f, speed);
                while (Time.time - tStart < at) { if (Over) yield break; yield return null; }
                Vector3 foe = targets != null && targets.Count > 0 && targets[0] < Enemies.Count && Enemies[targets[0]] ? Enemies[targets[0]].Center : u.Center + new Vector3(3f, 0, 0);
                switch (kind)
                {
                    case "flash":
                        Vfx.Flash(new Color(1f, 1f, 1f, 0.4f), 0.14f, 1.6f, PUNCH_ORDER);
                        FieldRig.Shake(1.0f, 4f); FieldRig.Punch(foe, 0.1f); Clock.HitStop(0.15f);
                        break;
                    case "red":
                        Vfx.Flash(new Color(1f, 0.22f, 0.16f, 0.5f), 0.3f, 1.4f, PUNCH_ORDER);
                        FieldRig.Shake(0.9f, 3f); FieldRig.Punch(foe, 0.09f); Clock.HitStop(0.13f);
                        break;
                    case "dark":
                        Vfx.Flash(new Color(0.04f, 0.03f, 0.14f, 0.45f), 1.6f, 1f, 290);
                        break;
                    case "shake":
                        FieldRig.Shake(0.8f, 3f); FieldRig.Punch(foe, 0.08f);
                        break;
                    case "zoom":
                        StartCoroutine(CamTo(foe, 1.32f, 0.25f));
                        break;
                }
                Trace?.Invoke("punch", $"{kind} {ms}ms → {at:F2}s");
                Debug.Log($"[UltPunch] {hid} {kind} {ms}ms → {at:F2}s (실제 {Time.time - tStart:F2}s)");
            }
        }
    }
}
