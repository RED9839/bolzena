import { A, BK, AL, MU, BL, GU, HE, BF, DB, JM, AC, CH, P, E, clone, bundle, tune } from "./lib.mjs";

// 정령산 — 1층 · 2층이 한 명단(기슭 · 꼭대기를 나누지 않고 세기만 오른다). 원소 — 한 짝에 원소 둘을 섞어 「무엇부터 잡나」.
const W = ["광기"]; // 누루링-정령 약점
const L = [
  // 위스프 넷 — 원작 특징 그대로: 불(순수) 고통 · 번개(광기) 충격 · 바람(활발) 깃발 · 물(우울) 치유
  E("wisps_naive", "위스프 · 불", 300, "back", "naive", "고통 — 순수한 불의 에너지. 스며드는 불로 고통 2.",
    [BK(90, "스며든다", { id: "고통", n: 2, w: 2 }), MU(30, 3, "불꽃이 갈라진다"), DB("고통", 2, "불티를 흩뿌린다")]),
  E("wisps_mad", "위스프 · 번개", 300, "back", "mad", "전기 — 방어 위로 맞으면 더 아픈 충격.",
    [AL(45, "방전", { id: "충격", n: 1, w: 2 }), MU(30, 3, "번개 가지", { id: "충격", per: 1 }), JM(1, "정전기")]),
  E("wisps_jolly", "위스프 · 바람", 280, "back", "jolly", "깃발 — 적 전체 사기 1. 「돌풍」 으로 손패를 채어 간다.",
    [BF("사기", 1, "바람을 실어 준다", { all: true }), BK(80, "회오리", { w: 2 }), AC("st_gust", 1, "draw", "돌풍")]),
  E("wisps_gloomy", "위스프 · 물", 320, "back", "gloomy", "치유사 — 맑은 물로 동료를 씻는다.",
    [HE(90, "맑은 물"), BK(80, "물방울을 쏜다", { w: 2 }), DB("약화", 1, "젖는다")]),
  E("lupalu_naive", "루파루 · 물가", 480, "front", "naive", "방패 — 물가에 사는 덩치 큰 정령. 맞으면 물을 머금는다(방어 30, 턴당 둘).",
    [GU(70, "물을 머금는다", { tough: 1 }), A(120, "몸통으로 민다", { w: 2 }), BL(110, "물방울 막")],
    { passives: [P("물을 머금는다", "hurt", BL(30), { limit: 2 })] }),
  E("lupalu_gloomy", "루파루 · 깨진 병", 380, "back", "gloomy", "균열 — 깨진 음료병 조각을 휘두른다.",
    [BK(100, "깨진 병", { id: "균열", n: 2, w: 2 }), DB("균열", 2, "유리 조각"), HE(60, "남은 음료")]),
  E("lupalu_jolly", "루파루 · 청정수", 420, "back", "jolly", "디버프 감시 — 청정수만 마신다. 디버프를 받으면 씻어 내며 회복(턴당 하나).",
    [BK(110, "청정수 물대포", { w: 2 }), BF("면역", 1, "맑게 걸러 낸다"), DB("약화", 1, "물보라")],
    { passives: [P("청정수", "debuffed", { t: "selfHeal", v: 60 })] }),
  E("lupalu_cool", "루파루 · 산정", 420, "front", "cool", "재촉꾼 — 정령산 정상 근처, 추운 곳에서 계속 움직인다. 당기면 다시 움직인다(AP -1).",
    [A(120, "차가운 물살", { w: 2, rush: 3 }), JM(1, "쉴 새 없이 움직인다", { rush: 3 }), BL(90, "물을 얼린다")],
    { passives: [P("멈추지 않는다", "rushed", JM(1))] }),
  E("oldtree_naive", "고모구지 · 기습", 460, "front", "naive", "기습 · 굳히기 — 침입자를 기습하는 영악한 정령. 첫 턴에 뒷줄을 찌른다.",
    [A(120, "가지로 후린다", { w: 2 }), GU(70, "뿌리를 내린다", { tough: 1 }), BK(100, "뿌리 찌르기")],
    { open: BK(130, "덤불 속 기습") }),
  E("oldtree_cool", "고모구지 · 고찰", 520, "front", "cool", "느긋이 · 굳히기 — 열매가 털린 뒤 깊이 생각한다. 큰 수가 늦게 온다.",
    [BL(140, "생각에 잠긴다", { tough: 2, rush: 0 }), A(160, "결론을 내린다", { rush: 0 }), HE(70, "새순")]),
  E("pumpkin_cool", "호바깅 · 고랭지", 480, "front", "cool", "굳히기 · 깃발(적 전체 결의 1) — 고지대에서 자라 껍질이 단단하다.",
    [BL(130, "단단한 껍질", { tough: 1 }), BF("결의", 1, "찬바람을 막아 준다", { all: true }), A(120, "굴러 부딪힌다")]),
  E("nependers_gloomy", "한입초 · 산지", 520, "front", "gloomy", "방패(강인도 5) · 깨면 끊긴다 — 산지에서 자라 외피가 단단하다.",
    [BL(140, "외피를 닫는다", { tough: 1 }), CH("입을 크게 벌린다", A(220, "통째로 문다"), { brk: true }), A(110, "덥석")],
    { tough: 5 }),
  E("nururingtanker_spirit", "누루링-정령 탱커", 640, "front", null, "방패(강인도 5) · 진중함 — 피해 감소 2 를 두르고 버틴다. 격파하면 취약 1.",
    [GU(80, "진중하게 막아선다", { tough: 1 }), BF("피해 감소", 2, "고요히 버틴다"), A(120, "무겁게 민다")],
    { weak: W, tough: 5, passives: [P("흐트러진 정신", "broken", BF("취약", 1))] }),
  E("nururingwarrior_spirit", "누루링-정령 전사", 440, "front", null, "깨면 끊긴다 — 진중하게 힘을 모은다.",
    [A(110, "정령의 주먹", { w: 2 }), CH("기를 모은다", A(210, "산을 울리는 한 방"), { brk: true }), BL(80, "명상")], { weak: W }),
  E("nururingarcher_spirit", "누루링-정령 마법사", 320, "back", null, "원소 — 불 · 번개를 번갈아 쏜다.",
    [BK(90, "불 정령탄", { id: "고통", n: 2 }), BK(90, "번개 정령탄", { id: "충격", n: 1 }), DB("약화", 1, "원소 교란")], { weak: W }),
  E("nururingsupporter_spirit", "누루링-정령 서포터", 340, "back", null, "치유사 · 깃발(적 전체 불굴 1).",
    [HE(90, "산의 기운"), BF("불굴", 1, "진중한 기도", { all: true }), BK(70, "톡", { w: 2 })], { weak: W }),
  // 엘리트
  E("oldtree_mad", "고모구지 · 열매 직전", 720, "front", "mad", "엘리트 · 디버프 감시 — 열매 맺기 직전이라 마구잡이. 디버프를 받을 때마다 사기(턴당 둘). 디버프를 쌓는 손보다 바로 때리는 손.",
    [A(150, "마구잡이 가지"), MU(50, 3, "열매를 던진다", { id: "고통" }), GU(90, "뿌리를 얽는다", { tough: 1 }), CH("열매가 부푼다", AL(120, "열매가 터진다"), { brk: true })],
    { passives: [P("마구잡이", "debuffed", BF("사기", 1), { limit: 2 })] }),
  E("wisps_cool", "위스프 · 원소 핵", 480, "back", "cool", "엘리트 · 처치 순서 시험 — 다른 위스프가 쓰러질 때마다 그 원소를 삼켜 회복하고 사기. 핵부터 끄거나 한꺼번에 꺼라.",
    [BK(110, "원소 폭발", { id: "고통", n: 1 }), AL(50, "원소의 고리", { id: "충격", n: 1 }), HE(80, "원소를 모은다")],
    { passives: [P("원소 흡수", "allyDown", { t: "selfHeal", v: 100 }, { limit: 0 }), P("원소 공명", "allyDown", BF("사기", 1))] }),
  E("pumpkin_cool_elite", "호바깅 · 만년설 껍질", 760, "front", "cool", "엘리트 · 방어 벗기기 — 첫 턴 두꺼운 껍질, 실드 유지로 넘긴다. 방어 파괴 · 관통 · 지속 피해로.",
    [BL(180, "얼어붙은 껍질", { tough: 1 }), BF("실드 유지", 2, "껍질이 굳는다"), A(150, "굴러 부딪힌다"), BF("결의", 1, "찬바람", { all: true })],
    { open: BL(260, "만년설 껍질"), passives: [P("갈라진 껍질", "broken", BF("취약", 2))] },
    { art: { spine: "monsterspine/pumpkin", skin: "cool", icon: "icon_pumpkincool", scale: 1.3 } }),
  // 1층 보스 — 이프리트(클론) + 위스프(불) 둘
  E("clone_ifrit", "이프리트 (클론)", 2500, "front", "mad",
    "1층 보스 · 시험: 지속 피해(고통) — 화력발전소의 불을 삼킨 불의 정령. 고통을 쌓고, 위스프가 쓰러지면 장작이 되어 불길이 커진다. 정화 · 회복 · 빠른 마무리 가운데 무엇으로 받을지.",
    [A(150, "불주먹", { id: "고통", n: 2 }), DB("고통", 2, "장작을 던진다"), AC("st_ember", 1, "draw", "불티를 흩뿌린다"), CH("불을 들이마신다", MU(45, 4, "캠프파이어", { id: "고통", per: 1 }), { brk: true }), BL(130, "불의 장막", { tough: 1 })],
    { boss: true,
      phase: { at: 0.55, say: "「모두 타 버려!」", intents: [AL(100, "불의 파도", { id: "고통", n: 2 }), A(180, "작열"), AC("st_ember", 2, "hand", "불티를 쥐여 준다"), CH("발전소의 불을 끌어온다", AL(170, "대분화"))] },
      phase2: { at: 0.2, say: "불길이 꺼져 간다", intents: [MU(60, 4, "마지막 불꽃"), CH("남은 장작을 모은다", AL(200, "캠프파이어"), { brk: true })] },
      passives: [
        P("장작은 많을수록", "allyDown", BF("사기", 1), { limit: 0 }),
        P("불붙은 땅", "turnEnd", DB("고통", 1), { phase: [1, 2] }),
        P("타오르는 몸", "hurt", { t: "thorns", v: 12 }, { phase: [1], limit: 2 }),
      ] }, { art: clone("ifrit", "이프리트") }),
  // 2층 보스 — 실라(클론) + 위스프(바람)
  E("clone_sylla", "실라 (클론)", 2600, "back", "cool",
    "2층 보스 · 시험: 격파(강인도) — 바람의 고위 정령. 「질서」 를 지키는 동안 턴마다 불굴이 쌓이고, 강인도를 깨뜨리면 「기습엔 약하다」 취약 3. 강인도 피해를 넣는 손이 열쇠다.",
    [BK(160, "헥토파스칼 펀치"), BF("불굴", 1, "질서를 바로 세운다"), CH("바람을 모은다", BK(340, "헥토파스칼 스윙!", { id: "취약", n: 1 }), { brk: true }), AL(70, "돌풍", { id: "약화", n: 1 }), AC("st_gust", 1, "draw", "산바람")],
    { boss: true, tough: 8,
      phase: { at: 0.5, say: "「전환 — 폭풍의 자세」", intents: [MU(45, 4, "칼바람"), BF("면역", 1, "바람의 장막"), CH("폭풍을 부른다", AL(170, "폭풍"), { brk: true }), BK(180, "급강하")] },
      phase2: { at: 0.2, say: "「정면 승부다」", intents: [CH("모든 바람을 모은다", BK(340, "헥토파스칼 스윙!!")), BL(200, "바람의 벽", { tough: 2 })] },
      passives: [
        P("질서", "turnEnd", BF("불굴", 1), { phase: [0, 1] }),
        P("기습엔 약하다", "broken", BF("취약", 3), { phase: [0, 1, 2] }),
        P("정면 승부", "recover", BF("사기", 1)),
      ] }, { art: clone("sylla", "실라") }),
];

const pools1 = [
  [["wisps_naive", "lupalu_naive"], ["oldtree_naive", "wisps_gloomy"], ["nururingwarrior_spirit", "nururingsupporter_spirit"], ["lupalu_naive", "wisps_mad"], ["oldtree_naive", "lupalu_gloomy"]],
  [["lupalu_naive", "wisps_naive", "wisps_jolly"], ["nururingtanker_spirit", "nururingarcher_spirit"], ["oldtree_cool", "lupalu_gloomy"], ["lupalu_cool", "wisps_mad"], ["pumpkin_cool", "wisps_gloomy"]],
  [["nururingtanker_spirit", "nururingwarrior_spirit", "nururingsupporter_spirit"], ["nependers_gloomy", "wisps_naive", "wisps_mad"], ["lupalu_jolly", "oldtree_naive", "wisps_jolly"], ["pumpkin_cool", "lupalu_cool", "nururingarcher_spirit"], ["oldtree_cool", "wisps_gloomy", "wisps_mad"]],
];
// 2층 — 같은 명단, 짝이 굵어진다(셋 · 넷)
const pools2 = [
  [["lupalu_naive", "wisps_mad", "wisps_naive"], ["oldtree_cool", "lupalu_jolly"], ["nururingtanker_spirit", "nururingarcher_spirit"], ["pumpkin_cool", "wisps_jolly"], ["lupalu_cool", "lupalu_gloomy"]],
  [["nependers_gloomy", "wisps_naive", "wisps_gloomy"], ["oldtree_naive", "lupalu_cool", "wisps_mad"], ["nururingtanker_spirit", "nururingwarrior_spirit", "nururingsupporter_spirit"], ["pumpkin_cool", "lupalu_gloomy", "wisps_jolly"], ["lupalu_jolly", "oldtree_cool", "wisps_naive"]],
  [["nependers_gloomy", "pumpkin_cool", "wisps_mad", "wisps_jolly"], ["oldtree_cool", "oldtree_naive", "wisps_gloomy"], ["nururingtanker_spirit", "lupalu_cool", "nururingarcher_spirit", "nururingsupporter_spirit"], ["lupalu_naive", "lupalu_jolly", "wisps_naive", "wisps_mad"], ["pumpkin_cool", "nependers_gloomy", "lupalu_gloomy"]],
];
const elites = [["wisps_cool", "wisps_naive", "wisps_mad"], ["oldtree_mad", "lupalu_naive", "lupalu_gloomy"], ["pumpkin_cool_elite", "lupalu_cool", "nururingsupporter_spirit"], ["wisps_cool", "wisps_gloomy", "nururingtanker_spirit"]];

const village = {
  id: "spirit", name: "정령산", race: "정령",
  line: "물가의 기슭에서 바람 부는 꼭대기까지 — 성난 정령들의 산",
  floors: [
    { name: "정령산 기슭", sub: "물가 · 동굴", land: "정령산", bg: { fight: "stage12_1", boss: "stage31_1", event: "stage37_1" },
      pools: pools1, elites, boss: ["clone_ifrit", "wisps_naive", "wisps_naive"] },
    { name: "정령산 꼭대기", sub: "가장 높은 곳 · 바람의 전당", land: "정령산", bg: { fight: "stage22_1", boss: "stage32_1", event: "stage43_1" },
      pools: pools2, elites, boss: ["clone_sylla", "wisps_jolly"] },
  ],
};

// 짝이 셋 · 넷으로 굵어지는 2층을 생각해 몸 하나하나는 조금 가볍게
tune(L, L.filter(x => !x.e.boss && !["wisps_cool", "oldtree_mad", "pumpkin_cool_elite"].includes(x.e.id)).map(x => x.e.id), 0.92, 0.95);

export default bundle(village, L, []);
