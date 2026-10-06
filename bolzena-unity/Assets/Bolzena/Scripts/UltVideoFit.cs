using System.Collections.Generic;
using Bolzena.Battle;
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
            public Dictionary<string, FxFix> Fx;
            public int[] Skip;                 // 건너뛸 구간(1배속 ms, 둘씩) — 원작보다 많이 도는 고리(스피키(메이드) 순간이동 사격 11번 → 원작 5번)
            public int End = -1;               // 이때(1배속 ms) 쉬는 동작으로 — 길이를 손으로 정한 사도(리뉴아)
            public float Speed;                // 0 = 셈대로
            public string[] Add;               // 계획에 없던 원작 장을 대상 쪽에 더한다(바롱 번개 — 장이 많아 시전 갈래만 남던 것)
            public string Note;
        }
        class FxFix { public string At; public int Ms = -1; public FxFix(string at, int ms = -1) { At = at; Ms = ms; } }

        // 길이를 줄이지 않는 사도 — 가운데 타격만으로 길다(원작 길이 그대로, 사용자 2026-10-06)
        static readonly HashSet<string> ULT_KEEP_LONG = new HashSet<string> { "리뉴아", "키샤" };

        static int[] H(params int[] ms) => ms;
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
            ["에피카"] = new UltFit { Hits = H(1067, 3400, 5600, 7000), Note = "전차 포격 → 해적선 → 기사 → 착지 번쩍(단계마다 한 박자)" },
            ["아르코"] = new UltFit { Hits = H(1766, 2266, 2733, 3133, 3633, 4566, 4966, 5500), Note = "착지 뒤 펀치 8타 약 0.45초 간격(1_2 이벤트)" },
            ["스피키_메이드"] = new UltFit { Hits = H(2467, 3134, 3801, 4468, 5135), Skip = H(5602, 8937), Note = "총 겨눔(2.1초) 뒤 순간이동 사격 5번(원작) — 우리 고리 10번 중 뒤 5번은 건너뜀" },
            ["리뉴아"] = new UltFit { Hits = H(1133, 2975, 4392, 6375, 7509, 9208, 11050, 13317), End = 14167, Speed = 1.2f, Note = "원작 약 11초 유지(예외) — 타격을 원작 비율(8·21·31·45·53·65·78·94%)로 고르게" },
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
            ["티그_영웅"] = new UltFit { Hits = H(2362), Move = "teleport", Go = 2152, Land = 2258, Ret = 3150, Back = 0, Dest = "front", Note = "섬광 직후 베어 들어감" },
            ["피라"] = new UltFit { Hits = H(664, 978, 1298, 1648, 1961, 2275, 2610, 3465, 3832, 4200, 4568, 4935), Note = "1막 7타 · 2막(큰 고양이) 5타" },
            ["네티"] = new UltFit { Move = "none", Fx = F(("neti_ultimate_1", "caster", 1342)), Note = "빛구슬은 사도 머리 위에서" },
            ["디아나_왕년"] = new UltFit { Hits = H(2520), Move = "dash", Go = 1522, Land = 1816, Ret = 4572, Back = 300, Dest = "front", Note = "불꽃 모으고 일찍 돌진 · 0.8초 버티다 폭발" },
            ["레테"] = new UltFit { Move = "none", Note = "제자리 빛줄기" },
            ["림"] = new UltFit { Hits = H(1732, 2562), Note = "뛰어들며 큰 베기 · 0.8초 뒤 X자 베기" },
            ["셀리네"] = new UltFit { Hits = H(1312), Move = "none", Note = "제자리 리본 춤 뒤 번쩍" },
            ["스노키"] = new UltFit { Hits = H(1155), Move = "leap", Go = 294, Land = 1050, Ret = 3465, Back = 300, Dest = "front", Fx = F(("slash_3", "caster", 525)), Note = "뛰어올라 금빛 고리에 머물다 내리꽂음" },
            ["시온더다크불릿"] = new UltFit { Hits = H(1554), Note = "푸른 고리 모으기 뒤 베기" },
            ["아라그니아"] = new UltFit { Hits = H(1155, 1680, 2205), Fx = F(("laser", "target", -1)), Note = "소용돌이 뒤 첫 충격 · 물기둥은 적 자리" },
            ["아이시아"] = new UltFit { Hits = H(262, 3045), Fx = F(("jump", "caster", -1), ("explosion", "target", 2468)), Note = "번쩍 직후 작은 구슬 · 큰 폭발은 둘째 타격" },
            ["에슈르"] = new UltFit { Hits = H(1925, 2028, 2782), Fx = F(("proj_2", "target", -1)), Note = "첫 폭발 1초 뒤 작은 폭발 한 번 더" },
            ["오르"] = new UltFit { Fx = F(("orr_ultimate_1", "caster", 998)), Note = "돔은 사도를 덮고 일찍" },
            ["캐시"] = new UltFit { Hits = H(4400), Move = "dash", Go = 2900, Land = 3150, Ret = 5600, Back = 300, Dest = "front", Fx = F(("kathy_ultimate_1", "target", 3333)), Note = "내려앉은 뒤 기둥 · 큰 폭발 1타 · 붉은 원이 남는 동안 머묾" },
            ["키디언"] = new UltFit { Hits = H(1400, 2446), Move = "teleport", Go = 715, Land = 1065, Ret = 2898, Back = 0, Dest = "front", Note = "찌르기 두 번(1초 간격) 뒤 내려섬" },
            ["하이디"] = new UltFit { Hits = H(2048, 2152, 2258, 2362, 2468, 2625), Move = "dash", Go = 735, Land = 1575, Ret = 2730, Back = 300, Dest = "front", Fx = F(("news", "target", 1995)), Note = "굴러 들어가 큰 번쩍과 함께 몰아침 · 돌아옴" },
            ["네르_빡침"] = new UltFit { Hits = H(3455), Move = "leap", Go = 3150, Land = 3400, Ret = 3650, Back = 0, Dest = "front", Note = "수정 장막 뒤 도약 타격 · 0.2초 만에 복귀" },
            ["리코타"] = new UltFit { Hits = H(2362, 2553, 2786, 3033), Move = "none", Fx = F(("slash_1", "target", -1)), Note = "제자리 긴 베기 · 이른 베기는 첫 타격에" },
            ["마요"] = new UltFit { Hits = H(945, 1680, 1890, 2152, 2415, 2678, 2940, 3150), Note = "단발 28% → 구름 다단이 끝 직전까지" },
            ["버터"] = new UltFit { Hits = H(2415), Note = "새총 당기기 1초 뒤 발사" },
            ["벨벳"] = new UltFit { Move = "dash", Go = 383, Land = 673, Ret = 3200, Back = 300, Dest = "front", Note = "마지막 타격 직후 복귀" },
            ["슈팡"] = new UltFit { Hits = H(1418), Move = "dash", Go = 1208, Land = 1418, Ret = 2415, Back = 300, Dest = "front", Note = "노란 돌진이 닿는 때" },
            ["시스트"] = new UltFit { Fx = F(("sist_ultimate", "caster", -1)), Note = "지폐비 · 오라는 시스트 자신에게" },
            ["아네트"] = new UltFit { Hits = H(860, 982, 1105, 1226, 1349, 1471, 1593, 1716, 1838, 1960, 2082, 2205), Note = "다단은 65% 지점까지" },
            ["아야"] = new UltFit { Hits = H(1260, 1811, 2362, 2914, 3465), Note = "연꽃 두 번에 걸쳐 약 0.5초 간격" },
            ["칸타"] = new UltFit { Hits = H(1418, 3150, 3549), Move = "dash", Go = 1102, Land = 1365, Ret = 1838, Back = 300, Dest = "front", Note = "대시 베기 뒤 곧 복귀 · 폭발은 집에서" },
            ["클로에"] = new UltFit { Hits = H(1714, 2152, 2594, 3034, 3476, 3916, 4305), Note = "토끼들이 0.4초 간격으로" },
            ["비비_신성"] = new UltFit { Fx = F(("boll", "target", 4725)), Note = "레이저 뒤 약 1초에 적 자리 빛기둥(boll)" },
            ["실라"] = new UltFit { Move = "dash", Go = 1575, Land = 1785, Ret = 2100, Back = 200, Dest = "front", Note = "짧게 달려들어 휘두르고 곧 돌아옴" },
            ["에르핀_왕도"] = new UltFit { Fx = F(("crash_1", "caster", 840)), Note = "첫 빛기둥은 사도 자리 · 도약은 길이를 되살려 원작 비율(1.9초)" },
            ["바나"] = new UltFit { Fx = F(("happy", "caster", 4830), ("hit_1", "caster", -1)), Note = "불꽃은 모루 위 · 완성 버프(happy)는 끝에" },
            ["피코라"] = new UltFit { Fx = F(("ultimate_up", "caster", -1)), Note = "하트 방울은 사도를 감쌈" },
            ["로네"] = new UltFit { Move = "none", Note = "제자리 깃발" },
            ["비비"] = new UltFit { Hits = H(1197), Note = "공중에 떴다 내리꽂기" },
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
            ["벨라"] = new UltFit { Hits = H(1302) },
            ["셰이디_역전"] = new UltFit { Hits = H(1890, 2940) },
            ["스피키"] = new UltFit { Fx = F(("raady", "caster", -1), ("ready", "caster", -1), ("circle", "caster", -1), ("ground", "caster", -1), ("pumpkin", "target", 3255)) },
            ["실비아"] = new UltFit { Move = "none" },
            ["우로스"] = new UltFit { Hits = H(1312, 2625) },
            ["코미_수영복"] = new UltFit { Hits = H(300, 2000, 2667, 3333, 4000, 4667), Move = "none", Fx = F(("waterfall", "target", 100)), Note = "물기둥이 시작하자마자 침 · 코미는 제자리에서 뛰어오름" },
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
                    if (e.Kind == EventKind.Damage)
                    {
                        var same = res[slot].Find(x => x.Kind == EventKind.Damage && x.Target.Side == e.Target.Side && x.Target.Index == e.Target.Index);
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
        System.Func<string, List<Bolzena.Fx.UltPart>, List<Bolzena.Fx.UltPart>> FitTweak(string hid, UltFit f, float speed)
        {
            if (f == null || (f.Fx == null && f.Add == null)) return null;
            return (hero, plan) =>
            {
                if (hero != hid) return plan;
                if (f.Add != null)
                    foreach (var n in f.Add)
                        if (Bolzena.Fx.FxLibrary.Has(n) && !plan.Exists(p => p.Name == n))
                            plan.Add(new Bolzena.Fx.UltPart { Name = n, At = "target", Dur = Bolzena.Fx.FxLibrary.Duration(n) });
                foreach (var p in plan)
                    foreach (var kv in f.Fx ?? new Dictionary<string, FxFix>())
                    {
                        if (!p.Name.Contains(kv.Key)) continue;
                        if (kv.Value.At != null)
                        {
                            p.At = kv.Value.At;
                            p.Muzzle = p.At == "caster" && MUZZLE_FIT.IsMatch(p.Name);   // 시전자 쪽으로 옮긴 총구 불꽃(칸나 fire)은 총구에
                            if (p.At == "caster") p.Body = false;
                        }
                        if (kv.Value.Ms >= 0) p.AtMs = Rm(kv.Value.Ms / 1000f) / Mathf.Max(0.01f, speed) * 1000f;
                        break;
                    }
                return plan;
            };
        }
    }
}
