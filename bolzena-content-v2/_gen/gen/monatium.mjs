import { A, BK, AL, MU, BL, GU, HE, BF, DB, JM, AC, CH, P, E, clone, bundle, tune } from "./lib.mjs";

// 모나티엄 — 1층 모나티엄 외곽(공장 지대 · 폐수관 · 외부 유지 보수) · 2층 모나티엄 도심(시청 · 시설). 군대와 기계.
const L = [
  // ── 1층 외곽 — 손상(공구) · 폭탄(폐기형) · 당기면 손해(바리케이드) ─────
  E("elfsoldiercloserange_cool", "엘프 돌격병 · 노동반", 560, "front", "cool", "바리케이드 · 녹이기 — 공구로 손상 2. 당기면 바리케이드(적 전체 방어 60).",
    [GU(80, "바리케이드를 세운다", { tough: 1 }), A(100, "공구로 내려친다", { id: "손상", n: 2 }), BF("결의", 1, "보강 자재를 나눠 준다", { all: true }), A(140, "삽으로 퍼붓는다")],
    { passives: [P("귀찮게 하지 마", "rushed", GU(60))] }),
  E("elfsoldierlongrange_cool", "엘프 명사수 · 경계조", 340, "back", "cool", "느긋이 · 저격 — 노동반이 일할 때 주변을 지킨다. 큰 한 발은 당겨지지 않는다.",
    [BK(110, "경계 사격", { w: 2 }), CH("조준경을 맞춘다", BK(200, "한 발")), DB("취약", 1, "표식 화살")]),
  E("elfsoldierlongrange_mad", "엘프 명사수 · 호위", 360, "back", "mad", "깃발(적 전체 불굴 1) · 엄호 — 동료가 쓰러지면 쏜다.",
    [BK(100, "엄호 사격", { w: 2 }), BF("불굴", 1, "동료 앞을 막아선다", { all: true }), MU(30, 3, "견제 사격")],
    { passives: [P("호위 사격", "allyDown", BK(70))] }),
  E("nependers_cool", "한입초", 480, "front", "cool", "깨면 끊긴다 · 끼워 넣기 — 「폐수 얼룩」. 당기면 꿀꺽(회복).",
    [BK(110, "덥석 문다"), BL(100, "잎을 닫는다"), CH("입을 크게 벌린다", A(210, "삼켰다가 뱉는다"), { brk: true }), AC("st_wastewater", 1, "draw", "폐수를 뱉어 낸다")],
    { passives: [P("꿀꺽", "rushed", { t: "selfHeal", v: 60 })] }),
  E("drones_gloomy", "드론 S형 · 수리 대기", 380, "back", "gloomy", "재촉꾼 · 전기 — 뒷골목 수리점에 맡겨진 드론. 잔고장으로 경보를 울린다.",
    [JM(1, "고장 경보", { rush: 3 }), BK(80, "흔들리는 방전 사격", { id: "충격", n: 1, w: 2 }), BL(90, "장갑을 반쯤 편다")]),
  E("droneg_gloomy", "드론 G형 · 급수리", 520, "front", "gloomy", "방패 · 깨면 무른다 — 급하게 수리를 마친 몸. 격파하면 취약 2.",
    [GU(70, "삐걱이는 방어벽", { tough: 1 }), A(110, "진압봉을 휘두른다", { id: "충격", n: 1 }), A(130, "돌진한다")],
    { passives: [P("헐거운 나사", "broken", BF("취약", 2))] }),
  E("drones_mad", "드론 S형 · 폐기형", 300, "back", "mad", "폭탄 · 끼워 넣기 — 40% 를 건너면 과열 폭발(전체 충격 2). 한 번에 넘겨라.",
    [MU(30, 3, "고장 난 연사", { w: 2 }), AC("st_overheat", 1, "draw", "회로가 과열된다"), BK(90, "달군 부품을 쏘아 낸다", { id: "고통", n: 2 })],
    { passives: [P("과열 폭발", "lowHp", AL(60, "과열 폭발", { id: "충격", n: 2 }), { at: 0.4 })] }),
  E("nururingtanker_elf", "누루링-엘프 탱커", 640, "front", null, "방패(강인도 5) · 녹이기 — 디버프를 받으면 주워 온 장비로 방어.",
    [GU(80, "주워 온 방패를 세운다", { tough: 1 }), A(100, "몸으로 밀어낸다"), BL(120, "기름 바른 몸을 굳힌다", { tough: 1 }), A(120, "방패로 내려찍는다", { id: "손상", n: 2 })],
    { weak: ["광기"], tough: 5, passives: [P("주워 온 장비", "debuffed", BL(60))] }),
  E("nururingwarrior_elf", "누루링-엘프 전사", 440, "front", null, "스킬 감시 · 깨면 끊긴다 — 스킬 두 장째마다 관통 50.",
    [A(110, "기름칠한 창으로 찌른다", { w: 2 }), MU(50, 2, "두 번 찌른다"), CH("창을 크게 젖힌다", A(200, "온몸으로 꿰뚫는다"), { brk: true })],
    { weak: ["광기"], passives: [P("감시하는 눈", "card", BK(50), { type: "스킬", every: 2 })] }),
  E("nururingarcher_elf", "누루링-엘프 마법사", 320, "back", null, "재촉꾼 · 녹이기 · 고통 — 기름 연기.",
    [BK(100, "마법 같은 것을 쏜다", { w: 2 }), JM(1, "주문 비슷한 것을 웅얼거린다"), DB("손상", 1, "기름 연기를 피운다"), MU(30, 3, "뜨거운 기름방울", { id: "고통" })], { weak: ["광기"] }),
  E("nururingsupporter_elf", "누루링-엘프 서포터", 340, "back", null, "치유사 · 깃발 · 격노 — 사이 나쁜 동료가 쓰러지면 사기.",
    [HE(90, "기름을 덧발라 준다"), BF("사기", 1, "누루링 말로 연설한다", { all: true }), BK(70, "깃대로 콕 찌른다", { w: 2 })],
    { weak: ["광기"], passives: [P("사이 나쁜 동료", "allyDown", BF("사기", 1))] }),
  // 1층 엘리트
  E("droneg_mad", "드론 G형 · 자가 수리", 560, "front", "mad", "엘리트 · 지속 회복 — 턴 끝마다 스스로 고친다(60). 격파하면 회로 정지(취약 2) — 느린 지속 피해보다 몰아 치기.",
    [A(110, "걷어찬다"), BL(110, "장갑을 덧댄다", { tough: 1 }), MU(50, 2, "두 번 짓밟는다")],
    { passives: [P("자가 수리", "turnEnd", { t: "selfHeal", v: 60 }), P("회로 정지", "broken", BF("취약", 2))] }),
  E("elfsoldiercloserange_mad", "엘프 돌격병 · 현장 반장", 620, "front", "mad", "엘리트 · 방어 감시 — 파티가 방어 · 실드로 버틴 턴 끝마다 바리케이드가 두꺼워진다(결의). 방어 파괴 · 관통으로 벗겨라.",
    [GU(100, "바리케이드를 겹친다", { tough: 1 }), A(130, "철근으로 친다", { id: "손상", n: 2 }), BF("실드 유지", 1, "바리케이드를 고정한다", { all: true }), CH("굴착기를 돌린다", AL(110, "벽째 밀어붙인다"), { brk: true })],
    { open: GU(120, "현장을 봉쇄한다"), passives: [P("보강 공사", "turnEnd", BF("결의", 1, "", { all: true }))] }),
  // 1층 보스 — 칸나(클론) + 드론 S형 둘
  E("drones_cool", "드론 S형", 460, "back", "cool", "전기 · 재촉꾼 — 방전 사격 파티 충격 1. 절반에서 비상 경보.",
    [AL(45, "전방위 방전 사격", { id: "충격", n: 1 }), JM(1, "경보음", { rush: 3 }), BL(110, "장갑 전개", { tough: 1 }), MU(30, 3, "조준 연사")],
    { passives: [P("비상 경보", "lowHp", JM(1, "비상 경보"), { at: 0.5 })] }),
  E("clone_canna", "칸나 (클론)", 2000, "back", "jolly",
    "1층 보스 · 시험: 카드 장수 — 진압부서의 명포수(드론 S형 둘과 포위). 「명포수」 네 장째마다 쏘고, 당겨지면 「명령」 으로 사기 — 0코 카드를 쏟아붓는 손을 시험한다. 양자폭탄은 격파로 끊긴다.",
    [A(130, "저격 명령"), MU(45, 3, "연사"), BF("사기", 1, "「진압 개시」", { all: true }), CH("양자폭탄 조준", A(260, "양자폭탄", { id: "취약", n: 2 }), { brk: true }), DB("약화", 1, "상관의 호통")],
    { boss: true,
      phase: { at: 0.5, say: "「포위망을 좁혀라」", intents: [AL(90, "포위 사격", { id: "충격", n: 1 }), A(150, "명포수의 한 발"), CH("드론과 표적을 맞춘다", AL(150, "일제 사격")), JM(1, "통신 교란")] },
      passives: [
        P("명포수", "card", BK(45), { every: 4, limit: 0 }),
        P("상명하복", "rushed", BF("사기", 1)),
        P("진압 개시", "fightStart", GU(80)),
        P("포위망", "turnEnd", DB("취약", 1), { phase: [1] }),
      ] }, { art: clone("canna", "칸나") }),

  // ── 2층 도심 — 깃발 · 전기 · 스킬 감시 · 격파로 안 끊기는 경보 ─────
  E("elfsoldiercloserange_naive", "엘프 돌격병", 500, "front", "naive", "굳히기 · 깃발(적 전체 결의 1) · 격노 — 전우가 쓰러지면 사기.",
    [A(120, "대열을 맞춘다", { w: 2 }), A(80, "창끝으로 찌른다", { id: "취약", n: 1 }), GU(80, "대열을 좁힌다", { tough: 1 }), BF("결의", 1, "방패를 맞댄다", { all: true })],
    { passives: [P("전우의 복수", "allyDown", BF("사기", 1))] }),
  E("elfsoldiercloserange_jolly", "엘프 돌격병 · 관리직", 420, "front", "jolly", "느긋이 — 도심 꿀보직. 큰 수가 늦게 오고 당겨도 쉰다.",
    [A(110, "서류철로 후린다", { rush: 0 }), BL(80, "책상 뒤로"), DB("약화", 1, "결재를 미룬다")], {}, { art: { spine: "monsterspine/elfsoldiercloserange", skin: "jolly", icon: "still_elfsoldiercloserange_jolly" } }),
  E("elfsoldiercloserange_gloomy", "엘프 돌격병 · 의장대", 500, "front", "gloomy", "깨면 끊긴다 · 일어서면 성난다 — 디버프를 받거나 격파에서 일어서면 사기.",
    [A(120, "의장검을 휘두른다"), BL(100, "자세를 가다듬는다", { tough: 2 }), CH("의장검을 높이 세운다", A(220, "예식처럼 내리친다"), { brk: true }), DB("취약", 1, "날 선 눈빛")],
    { passives: [P("의장대의 자존심", "debuffed", BF("사기", 1)), P("의장대의 체면", "recover", BF("사기", 1))] }),
  E("elfsoldierlongrange_naive", "엘프 명사수", 380, "back", "naive", "저격 · 느긋이 — 뒷줄을 겨눈다.",
    [BK(110, "뒷줄을 겨눈다", { w: 2, rush: 9 }), MU(40, 3, "화살을 잇달아 쏜다"), BK(60, "표식 화살", { id: "취약", n: 2 })]),
  E("elfsoldierlongrange_jolly", "엘프 명사수 · 순찰대", 360, "back", "jolly", "재촉꾼 · 당기면 손해 — 순찰 호루라기. 당기면 「통제 신호」.",
    [BK(90, "순찰 사격", { w: 2 }), JM(1, "호루라기", { rush: 3 }), AC("st_signal", 1, "draw", "검문한다")],
    { passives: [P("검문", "rushed", AC("st_signal", 1, "draw"))] }, { art: { spine: "monsterspine/elfsoldierlongrange", skin: "jolly", icon: "still_elfsoldierlongrange_jolly" } }),
  E("elfsoldierlongrange_gloomy", "엘프 명사수 · 의장대", 340, "back", "gloomy", "깨면 끊긴다(관통) — 한 치 어긋남 없는 한 발.",
    [CH("의장 총을 겨눈다", BK(220, "한 치 어긋남 없이 쏜다"), { brk: true }), BK(90, "예포를 쏜다"), BF("불굴", 1, "고개를 쳐든다")]),
  E("droneg_naive", "드론 G형", 560, "front", "naive", "방패 · 전기 · 끼워 넣기 — 「통제 신호」. 동료가 쓰러지면 방어벽.",
    [GU(80, "방어벽을 세운다", { tough: 1 }), A(110, "전기 진압봉", { id: "충격", n: 1 }), AC("st_signal", 1, "draw", "통제 신호를 보낸다"), A(140, "돌진한다")],
    { passives: [P("방어벽", "allyDown", GU(60))] }),
  E("drones_jolly", "드론 S형 · 낙서", 340, "back", "jolly", "재촉꾼 · 전기 — 자유 투사의 낙서가 그려진 드론. 당기면 사기.",
    [MU(30, 3, "엉뚱한 연사", { id: "충격", per: 1, w: 2 }), JM(1, "낙서 경보음", { rush: 3 }), BK(80, "몸통 박치기")],
    { passives: [P("신난 낙서", "rushed", BF("사기", 1))] }),
  E("droneg_jolly", "드론 G형 · 낙서", 600, "front", "jolly", "방패(강인도 5) · 깨면 무른다 — 낙서를 즐기는 순찰 드론.",
    [GU(90, "낙서된 방어벽", { tough: 1 }), A(130, "스프레이 진압", { id: "약화", n: 1 }), A(150, "돌진한다")],
    { tough: 5, passives: [P("페인트 범벅", "broken", BF("취약", 2))] }),
  // 2층 엘리트
  E("droneg_cool", "드론 G형 · 시설 경비", 780, "front", "cool", "엘리트 · 격파로 안 끊기는 경보 — 경보(전체 충격 2)는 기절로만 막힌다. 방어로 받으면 충격이 더 아프다 — HP 로 받을지 고른다.",
    [GU(100, "방어벽을 겹친다", { tough: 1 }), A(150, "진압봉을 내리친다"), CH("경보를 울린다", AL(120, "진압 사격", { id: "충격", n: 2 })), AC("st_signal", 2, "draw", "출입을 통제한다")],
    { passives: [P("상주 경비", "fightStart", GU(100)), P("방어벽", "allyDown", GU(80))] }),
  E("elfsoldiercloserange_honor", "엘프 돌격병 · 의장대장", 640, "front", "gloomy", "엘리트 · 디버프 감시 — 디버프를 받을 때마다 면역 1(턴당 하나) · 일어서면 사기. 한 턴에 디버프를 여러 번 거는 손을 시험한다.",
    [A(140, "의장대 지휘검"), BF("결의", 1, "대열 정렬", { all: true }), CH("예식 검무", AL(110, "의장대 일제 돌격"), { brk: true }), BL(120, "자세를 가다듬는다", { tough: 2 })],
    { passives: [P("흐트러짐 없는 대열", "debuffed", BF("면역", 1)), P("의장대장의 체면", "recover", BF("사기", 1, "", { all: true }))] }, { art: { spine: "monsterspine/elfsoldiercloserange", skin: "gloomy", icon: "still_elfsoldiercloserange_gloomy" } }),
  // 2층 보스 — 엘레나(클론) + 드론 G형 · S형
  E("clone_elena", "엘레나 (클론)", 2700, "back", "cool",
    "2층 보스 · 시험: 전기(충격) · 드론 정리 — 시청의 엘레나. 드론이 살아 있으면 턴마다 적 전체 방어, 드론이 쓰러지면 과충전(취약 2). 둘째 판 D-CAT 은 한 대마다 충격 — 방어 · 실드로만 받는 손이 아프다.",
    [BK(140, "과충전 구체"), AL(60, "전류 그물", { id: "충격", n: 1 }), DB("약화", 1, "명령 코드 송신"), CH("구체를 모은다", AL(150, "과충전 폭발"), { brk: true })],
    { boss: true,
      phase: { at: 0.6, say: "「코드네임 D-CAT, 가동」", intents: [CH("D-CAT 을 부른다", MU(35, 6, "코드네임 D-CAT", { id: "충격", per: 1 })), BK(160, "고압 구체"), AC("st_overheat", 2, "draw", "과부하 경고"), DB("취약", 2, "표적 지정")] },
      phase2: { at: 0.25, say: "「도시의 질서를 위해」", intents: [AL(110, "시청 방어 체계", { id: "충격", n: 2 }), CH("모든 드론을 겹친다", MU(50, 5, "D-CAT 과부하"), { brk: true }), BF("사기", 1, "질서 회복 명령")] },
      passives: [
        P("코드 기능 개선", "turnStart", GU(50), { phase: [0, 1] }),
        P("과충전", "allyDown", BF("취약", 2), { limit: 0 }),
        P("과부하", "turnEnd", DB("충격", 1), { phase: [2] }),
      ] }, { art: clone("elena", "엘레나") }),
];

const village = {
  id: "monatium", name: "모나티엄", race: "엘프",
  line: "공장 지대와 폐수관을 지나 시청이 있는 도심으로 — 엘프 군대와 드론의 도시",
  floors: [
    { name: "모나티엄 외곽", sub: "공장 지대 · 폐수관", land: "모나티엄 외곽", bg: { fight: "stage10_1", boss: "stage6_2", event: "stage5_1" },
      pools: [
        [["elfsoldiercloserange_cool", "elfsoldierlongrange_cool"], ["nependers_cool", "drones_gloomy"], ["nururingwarrior_elf", "nururingsupporter_elf"], ["droneg_gloomy", "elfsoldierlongrange_cool"], ["elfsoldiercloserange_cool", "drones_mad"]],
        [["elfsoldiercloserange_cool", "elfsoldierlongrange_mad"], ["nururingtanker_elf", "nururingarcher_elf"], ["droneg_gloomy", "drones_mad", "drones_gloomy"], ["nependers_cool", "nururingwarrior_elf", "elfsoldierlongrange_cool"], ["nururingtanker_elf", "drones_mad"]],
        [["elfsoldiercloserange_cool", "drones_gloomy", "elfsoldierlongrange_mad"], ["nururingtanker_elf", "nururingwarrior_elf", "nururingsupporter_elf"], ["droneg_gloomy", "nependers_cool", "drones_mad"], ["elfsoldiercloserange_cool", "elfsoldierlongrange_mad", "drones_mad"], ["nururingtanker_elf", "nururingarcher_elf", "elfsoldierlongrange_cool"]],
      ],
      elites: [["droneg_mad", "drones_mad", "drones_mad"], ["elfsoldiercloserange_mad", "elfsoldierlongrange_mad"], ["nururingtanker_elf", "nururingwarrior_elf", "nururingarcher_elf", "nururingsupporter_elf"], ["droneg_mad", "nependers_cool", "elfsoldierlongrange_cool"]],
      boss: ["clone_canna", "drones_gloomy", "drones_gloomy"] },
    { name: "모나티엄 도심", sub: "시청 · 관리 시설", land: "모나티엄 도심", bg: { fight: "stage4_1", boss: "stage7_1", event: "stage39_1" },
      pools: [
        [["elfsoldiercloserange_naive", "elfsoldierlongrange_naive"], ["elfsoldiercloserange_jolly", "drones_jolly"], ["droneg_naive", "elfsoldierlongrange_jolly"], ["elfsoldiercloserange_jolly", "elfsoldierlongrange_naive"], ["drones_cool", "elfsoldiercloserange_naive"]],
        [["elfsoldiercloserange_gloomy", "elfsoldierlongrange_jolly"], ["droneg_naive", "drones_cool"], ["elfsoldiercloserange_naive", "elfsoldierlongrange_gloomy"], ["droneg_jolly", "drones_jolly", "elfsoldierlongrange_naive"], ["nururingtanker_elf", "drones_cool"]],
        [["elfsoldiercloserange_gloomy", "elfsoldiercloserange_naive", "elfsoldierlongrange_gloomy"], ["droneg_jolly", "droneg_naive", "drones_cool"], ["elfsoldiercloserange_naive", "drones_jolly", "elfsoldierlongrange_jolly", "elfsoldierlongrange_naive"], ["nururingtanker_elf", "nururingwarrior_elf", "elfsoldierlongrange_gloomy"], ["droneg_jolly", "elfsoldiercloserange_gloomy", "drones_jolly"]],
      ],
      elites: [["droneg_cool", "drones_cool"], ["elfsoldiercloserange_honor", "elfsoldierlongrange_gloomy", "elfsoldiercloserange_gloomy"], ["droneg_cool", "elfsoldiercloserange_naive", "elfsoldierlongrange_naive"], ["elfsoldiercloserange_honor", "drones_jolly", "droneg_naive"]],
      boss: ["clone_elena", "droneg_naive", "drones_cool"] },
  ],
};

// 1층(외곽) 몸은 층 눈금에 맞춰 덜어 낸다 — 웹판 2층 값에서 왔다
tune(L, ["elfsoldiercloserange_cool", "elfsoldierlongrange_cool", "elfsoldierlongrange_mad", "nependers_cool", "drones_gloomy", "droneg_gloomy", "drones_mad",
  "nururingtanker_elf", "nururingwarrior_elf", "nururingarcher_elf", "nururingsupporter_elf", "droneg_mad", "elfsoldiercloserange_mad", "drones_cool"], 0.85, 0.9);
// 2층(도심) 일반 몸도 한 눈금 덜어 낸다 — 2층 배율(웹판 3층 눈금)에 웹판 2층 값이 얹혀 있었다
tune(L, ["elfsoldiercloserange_naive", "elfsoldiercloserange_jolly", "elfsoldiercloserange_gloomy", "elfsoldierlongrange_naive", "elfsoldierlongrange_jolly",
  "elfsoldierlongrange_gloomy", "droneg_naive", "drones_jolly", "droneg_jolly"], 0.9, 0.9);

export default bundle(village, L, []);
