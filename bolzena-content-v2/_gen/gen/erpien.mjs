import { A, BK, AL, MU, BL, GU, HE, BF, DB, JM, AC, CH, P, E, clone, bundle, tune } from "./lib.mjs";

// 에르피엔 — 요정 왕국. 1층 세계수 숲(왕국 바깥 — 숲 · 텃밭 · 물가) · 2층 요정 왕도(과자 거리 · 왕궁). 가르치는 땅.
const W = ["우울"]; // 누루링-요정 약점
const L = [
  // ── 1층 세계수 숲 — 하나씩(끼워 넣기 · 달아오름 · 굳히기 · 치유사) ─────
  E("fairymoblongrange_jolly", "고혈당 요정", 270, "back", "jolly", "재촉꾼 · 당기면 손해 — 당기면 손에 「왁자지껄」 이 낀다.",
    [BK(70, "뒤로 파고든다", { w: 2 }), JM(1, "왁자지껄 떠든다", { rush: 3 }), DB("약화", 1, "장난을 친다"), AC("st_chatter", 1, "hand", "수다를 퍼뜨린다", { rush: 3 })],
    { passives: [P("수다 전염", "rushed", { t: "addCard", id: "st_chatter", n: 1, to: "hand" })] }),
  E("fairymoblongrange_mad", "매운 고혈당 요정", 300, "back", "mad", "저격 · 고통 — 매운 채소를 던져 뒷줄을 태운다.",
    [BK(90, "매운 고추를 던진다", { id: "고통", n: 2, w: 2 }), MU(35, 3, "고춧가루를 흩뿌린다"), DB("고통", 2, "눈이 맵다")]),
  E("magicfork_naive", "불효자손", 300, "front", "naive", "유리 대포(강인도 3) · 달아오름 — 턴이 끝날 때마다 사기 1.",
    [A(90, "밭을 갈듯 내리찍는다", { w: 2 }), MU(40, 2, "녹슨 날로 두 번 긁는다", { id: "고통" }), BL(60, "자루를 곧추세운다")],
    { tough: 3, passives: [P("날이 선다", "turnEnd", BF("사기", 1))] }),
  E("magicfork_gloomy", "고장 난 불효자손", 360, "front", "gloomy", "느긋이 · 깨면 무른다 — 수리받지 못한 농기구. 격파하면 취약 2.",
    [A(130, "삐걱이며 내리친다", { rush: 0 }), BL(90, "녹슨 자루로 버틴다"), MU(45, 2, "이 빠진 날", { id: "균열" })],
    { passives: [P("부러진 자루", "broken", BF("취약", 2))] }),
  E("magicfork_jolly", "불량 불효자손", 340, "front", "jolly", "일어서면 성난다 · 깨면 끊긴다 — 자만한 농기구.",
    [A(120, "제멋대로 휘두른다", { w: 2 }), CH("날을 높이 쳐든다", A(220, "밭째 뒤엎는다"), { brk: true }), BF("사기", 1, "우쭐댄다")],
    { passives: [P("내가 최고야", "recover", BF("사기", 1))] }),
  E("ginseng_gloomy", "산사모", 300, "back", "gloomy", "치유사 · 굳히기 — 절반에서 한 번 땅속으로 숨는다.",
    [HE(80, "뿌리에서 즙이 돈다"), GU(60, "땅에 박힌다", { tough: 1 }), DB("약화", 1, "쓴 냄새"), BK(90, "뿌리를 뻗는다")],
    { passives: [P("땅속으로", "lowHp", BL(80, "땅속으로", { tough: 2 }), { at: 0.5 })] }),
  E("lupalu_mad", "루파루", 340, "back", "mad", "녹이기 — 요정에게 받은 사탕으로 당분 중독. 파티 손상 2, 디버프를 받으면 사기.",
    [BK(100, "음료병을 휘두른다"), DB("손상", 2, "끈적한 음료를 끼얹는다"), JM(1, "병이 출렁인다"), HE(70, "사탕 음료를 나눠 마신다")],
    { passives: [P("당분 중독", "debuffed", BF("사기", 1))] }),
  E("nururingtanker_fairy", "누루링-요정 탱커", 500, "front", null, "방패(강인도 5) · 깨면 무른다 — 맞으면 젤리가 굳는다(턴당 둘).",
    [GU(60, "젤리로 감싼다", { tough: 1 }), A(90, "통통 부딪힌다"), BF("피해 감소", 2, "단맛에 둔감해진다"), A(120, "깔고 앉는다")],
    { weak: W, tough: 5, passives: [P("통통한 젤리", "hurt", BL(30), { limit: 2 }), P("물러진 젤리", "broken", BF("취약", 1))] }),
  E("nururingwarrior_fairy", "누루링-요정 전사", 360, "front", null, "깨면 끊긴다 — 팔을 걷어붙이면 격파로 끊어라.",
    [A(90, "젤리 주먹", { w: 2 }), BF("사기", 1, "근육이라고 우긴다"), CH("팔을 걷어붙인다", A(170, "힘자랑 한 방"), { brk: true })], { weak: W }),
  E("nururingarcher_fairy", "누루링-요정 마법사", 280, "back", null, "디버퍼 · 고통 — 마력 빵을 던진다.",
    [DB("약화", 1, "빵 부스러기 마법"), BK(80, "마력 빵을 던진다", { id: "고통", n: 2, w: 2 }), DB("취약", 1, "달콤한 주문")], { weak: W }),
  E("nururingsupporter_fairy", "누루링-요정 서포터", 300, "back", null, "치유사 · 깃발(적 전체 사기 1).",
    [HE(70, "젤리층을 찹찹 치댄다"), BF("사기", 1, "찹찹 격려한다", { all: true }), BK(50, "톡 친다", { w: 2 })], { weak: W }),
  // 1층 엘리트
  E("magicfork_mad", "불효자손 · 폭주", 560, "front", "mad", "엘리트 · 연타 감시 — 맞을 때마다 사기(턴당 둘). 잘게 여러 번보다 굵게 한 번.",
    [A(110, "갈아엎는다"), MU(40, 3, "마구 긁는다", { id: "고통" }), CH("자루를 높이 든다", AL(100, "밭째 갈아엎는다"), { brk: true }), BL(80, "날을 간다", { tough: 1 })],
    { passives: [P("무엇이든 간다", "hurt", BF("사기", 1), { limit: 2 })] }),
  E("ginseng_mad", "산사모 · 웃자람", 440, "back", "mad", "엘리트 · 디버프 감시 · 폭탄 — 턴마다 회복, 디버프를 받으면 양분으로 회복. 40% 를 건너면 터진다 — 한 번에 넘겨라.",
    [HE(100, "양분을 뿜는다"), GU(70, "뿌리를 얽는다", { tough: 1 }), BK(100, "쓴 즙을 뿌린다"), DB("고통", 2, "쓴맛이 번진다")],
    { passives: [P("넘치는 양분", "turnStart", HE(40)), P("양분으로 바꾼다", "debuffed", { t: "selfHeal", v: 50 }), P("양분이 터진다", "lowHp", AL(70, "양분이 터진다", { id: "고통", n: 2 }), { at: 0.4 })] }),
  E("ginseng_cool", "산사모 · 거대 사탕수수 지기", 620, "front", "cool", "엘리트 · 방어 벗기기 — 첫 턴 두꺼운 껍질을 실드 유지로 넘긴다. 방어 파괴 · 관통 · 지속 피해 · 격파로.",
    [BL(170, "사탕수수 껍질", { tough: 1 }), BF("실드 유지", 2, "껍질이 굳는다"), A(140, "사탕수수로 후린다"), BF("결의", 1, "뿌리를 얽는다", { all: true })],
    { open: BL(240, "거대 사탕수수 뒤로"), passives: [P("껍질이 갈라진다", "broken", BF("취약", 2))] }),
  // 1층 보스 — 캬롯(클론): 세계수 외곽의 일등 정원사
  E("clone_carrot", "캬롯 (클론)", 2300, "back", "naive",
    "1층 보스 · 시험: 자라는 정원 — 세계수 외곽의 일등 정원사. 턴이 끝날 때마다 정원(적 전체)에 「성장」(결의) · 치유가 돌고, 정원이 오래 살수록 단단해진다. 정원사를 먼저 칠지, 정원을 걷을지. 수액 펌프는 격파로 끊긴다.",
    [BK(120, "사탕수수 회초리"), HE(140, "특제 영양제"), BF("사기", 1, "「무럭무럭 자라렴」", { all: true }), CH("수액 펌프를 채운다", AL(130, "수액 펌프 발사!", { id: "약화", n: 1 }), { brk: true }), DB("취약", 1, "작물을 깔보는 자에게")],
    { boss: true,
      phase: { at: 0.5, say: "「내 정원을 망치다니…!」", intents: [MU(40, 4, "덩굴 채찍"), HE(180, "영양제 과다 살포"), CH("펌프를 최대로", AL(160, "수액 펌프 발사!!")), JM(1, "덩굴로 손을 묶는다")] },
      passives: [
        P("사탕수수 정원", "turnEnd", BF("결의", 1, "", { all: true })),
        P("마법 성장 비료", "card", HE(40), { type: "공격", every: 3, limit: 0 }),
        P("뽑힌 사탕수수", "allyDown", BF("사기", 1)),
      ] }, { art: clone("kyarot", "캬롯") }),

  // ── 2층 요정 왕도 — 폭탄 · 상태 카드 몰림(설탕 범벅) · 방패 깨면 무른다 ─────
  E("fairymobcloserange_naive", "저혈당 요정", 420, "front", "naive", "느긋이 · 끼워 넣기 — 당이 떨어지면 「어지럼」 을 옮긴다.",
    [A(110, "달려든다", { w: 2 }), MU(40, 3, "우르르 몰려든다"), BL(80, "웅크린다", { tough: 1, rush: 0 }), AC("st_dizzy", 1, "draw", "당이 떨어져 비틀거린다")]),
  E("fairymobcloserange_gloomy", "공허한 저혈당 요정", 400, "front", "gloomy", "디버퍼 · 느긋이 — 공허한 모습으로 거리를 떠돈다.",
    [DB("약화", 2, "텅 빈 눈빛"), A(130, "휘청이며 부딪힌다", { rush: 0 }), AC("st_dizzy", 1, "draw", "어지럼을 옮긴다")]),
  E("buseuleogi_mad", "부스러기", 220, "back", "mad", "재촉꾼 · 격노 · 끼워 넣기 — 「설탕 범벅」. 동료가 쓰러지면 사기.",
    [A(55, "와작 문다", { w: 2, rush: 3 }), MU(20, 3, "부스러기가 튄다"), AC("st_sugar", 1, "discard", "설탕 가루를 흩뿌린다"), BF("사기", 1, "설탕을 핥는다")],
    { passives: [P("남은 부스러기", "allyDown", BF("사기", 1))] }),
  E("buseuleogi_cool", "컵케이크 부스러기", 300, "back", "cool", "깃발(적 전체 피해 감소 1) — 컵케이크의 왕국을 꿈꾼다.",
    [BF("피해 감소", 1, "컵케이크 왕국 선포", { all: true }), BK(80, "크림을 쏜다", { w: 2 }), AC("st_sugar", 1, "draw", "설탕 코팅")]),
  E("mogmaekim_jolly", "목매킴", 320, "front", "jolly", "폭탄 — 35% 를 건너면 부풀어 터진다(전체 고통 2). 한 번에 넘겨라.",
    [A(85, "제작자를 원망하며 달려든다", { w: 2 }), DB("약화", 1, "밀가루가 휘날린다"), BL(60, "뻑뻑하게 굳는다")],
    { passives: [P("부풀어 터진다", "lowHp", AL(50, "부풀어 터진다", { id: "고통", n: 2 }), { at: 0.35 })] }),
  E("mogmaekim_mad", "설탕 과다 목매킴", 360, "front", "mad", "달아오름 · 폭탄 — 설탕이 너무 많이 들어갔다. 턴 끝마다 사기, 30% 에서 터진다.",
    [A(100, "설탕 폭주", { w: 2 }), MU(35, 3, "설탕 결정을 뿌린다")],
    { passives: [P("설탕 과다", "turnEnd", BF("사기", 1)), P("설탕 폭발", "lowHp", AL(60, "설탕 폭발", { id: "손상", n: 2 }), { at: 0.3 })] }),
  E("marshmallowtanker_naive", "탱탱 멜로", 480, "front", "naive", "방패(강인도 5) · 깨면 무른다(취약 2).",
    [BL(100, "말랑하게 부푼다", { tough: 1 }), A(80, "몸으로 민다"), GU(60, "줄을 맞춘다"), A(110, "통통 튀어 부딪힌다")],
    { tough: 5, passives: [P("얻어맞아 분노", "lowHp", BF("사기", 1), { at: 0.5 }), P("푹 꺼진다", "broken", BF("취약", 2))] }),
  E("marshmallowdealer_jolly", "말랑 멜로", 280, "back", "jolly", "저격 · 고통 — 꼬치로 뒷줄을 찌른다.",
    [BK(70, "꼬치를 찌른다", { id: "고통", n: 2, w: 2 }), MU(30, 3, "꼬치를 연달아 찌른다"), A(70, "앞으로 달려든다")],
    { passives: [P("녹아내리며 찌른다", "lowHp", BK(80), { at: 0.5 })] }),
  E("marshmallowdealer_mad", "화난 말랑 멜로", 320, "back", "mad", "달아오름 · 연타 — 항상 화가 나 있다. 맞으면 사기(턴당 하나).",
    [MU(35, 3, "분노의 꼬치", { w: 2 }), BK(90, "꼬치 던지기")],
    { passives: [P("늘 화났다", "hurt", BF("사기", 1))] }),
  E("marshmallowsupporter_jolly", "쫀득 멜로", 260, "back", "jolly", "치유사 · 깃발 · 끼워 넣기 — 디버프를 받으면 설탕 코팅(회복).",
    [HE(60, "설탕을 덧바른다"), BF("사기", 1, "꽃 깃발을 흔든다", { all: true }), AC("st_sugar", 1, "draw", "쫀득하게 들러붙는다"), DB("약화", 1, "달콤한 냄새")],
    { passives: [P("설탕 코팅", "debuffed", { t: "selfHeal", v: 50 })] }),
  // 2층 엘리트
  E("goldring_gloomy", "새마음금고", 700, "front", "gloomy", "엘리트 · 공격 감시 — 공격 카드 두 장째마다 뚜껑(방어 70). 격파하면 취약 2 — 공격만 몰아 치는 손보다 깨는 손.",
    [BL(120, "뚜껑을 닫는다", { tough: 2 }), A(140, "와락 문다"), MU(40, 3, "금화를 뱉는다", { id: "손상" }), BF("사기", 1, "보석을 삼킨다")],
    { passives: [P("뚜껑 닫기", "card", BL(70), { type: "공격", every: 2, limit: 0 }), P("뚜껑이 열린다", "broken", BF("취약", 2))] }),
  E("goldring_jolly", "새마음금고 · 보물 지기", 640, "front", "jolly", "엘리트 · 스킬 감시 — 보물을 노리는 자에게 사납다. 스킬 두 장째마다 「설탕 범벅」 을 손에 — 방어 · 실드만 쌓는 손을 시험한다.",
    [A(150, "금고째 들이받는다"), MU(45, 3, "금화 난사"), GU(90, "금고 문을 잠근다", { tough: 1 }), CH("금화를 삼킨다", AL(110, "금화 폭포"), { brk: true })],
    { passives: [P("보물 지키기", "card", AC("st_sugar", 1, "hand"), { type: "스킬", every: 2 })] }),
  // 2층 보스 — 에르핀(클론): 왕궁을 차지한 먹보 여왕의 가짜
  E("clone_erpin", "에르핀 (클론)", 2800, "back", "naive",
    "2층 보스 · 시험: 간식 셈 — 왕궁의 가짜 먹보 여왕. 곁의 멜로(간식 골렘)가 쓰러지면 「무전취식」 으로 먹어 치워 크게 회복한다 — 졸개를 먼저 치우면 보스가 차오른다. 「돌겨어어어!!!」 뒤에는 넘어져 취약이 된다.",
    [BK(160, "산을 뽑아 던진다"), MU(45, 3, "케이크 난사", { id: "약화", per: 1 }), BF("피해 감소", 2, "케이크로 배를 채운다"), CH("돌진 자세", AL(150, "돌겨어어어!!!", { id: "약화", n: 2 }), { brk: true }), BF("취약", 3, "억⋯?")],
    { boss: true,
      phase: { at: 0.55, say: "「간식 시간이다!」", intents: [AC("st_sugar", 2, "draw", "설탕을 퍼붓는다"), BK(180, "왕관의 괴력"), CH("다시 돌진 자세", AL(170, "돌겨어어어!!!")), BF("취약", 3, "억⋯?"), HE(200, "12끼째")] },
      phase2: { at: 0.2, say: "「배고파… 다 먹어 버릴 거야」", intents: [MU(60, 4, "굶주린 난동"), BK(200, "왕관을 던진다")] },
      passives: [
        P("무전취식", "allyDown", { t: "selfHeal", v: 350 }, { limit: 0 }),
        P("달달한 게 최고야", "turnEnd", BF("사기", 1), { phase: [2] }),
        P("왕관의 저주", "broken", BF("취약", 2)),
      ] }, { art: clone("erpin", "에르핀") }),
];

const village = {
  id: "erpien", name: "에르피엔", race: "요정",
  line: "세계수 둘레의 숲과 텃밭을 지나 과자 거리와 왕궁이 있는 요정 왕도로",
  floors: [
    { name: "세계수 숲", sub: "요정 왕국 바깥 · 숲 · 텃밭 · 물가", land: "에르피엔", bg: { fight: "stage3_2", boss: "stage34_1", event: "stage2_1" },
      pools: [
        [["magicfork_naive", "fairymoblongrange_jolly"], ["ginseng_gloomy", "magicfork_naive"], ["nururingwarrior_fairy", "nururingsupporter_fairy"], ["magicfork_gloomy", "fairymoblongrange_jolly"], ["nururingwarrior_fairy", "nururingarcher_fairy"]],
        [["magicfork_gloomy", "lupalu_mad"], ["nururingtanker_fairy", "nururingarcher_fairy"], ["magicfork_naive", "ginseng_gloomy", "fairymoblongrange_jolly"], ["magicfork_jolly", "nururingsupporter_fairy"], ["fairymoblongrange_mad", "magicfork_naive", "lupalu_mad"]],
        [["nururingtanker_fairy", "nururingwarrior_fairy", "nururingsupporter_fairy"], ["magicfork_jolly", "fairymoblongrange_mad", "ginseng_gloomy"], ["magicfork_gloomy", "magicfork_naive", "lupalu_mad"], ["magicfork_jolly", "fairymoblongrange_jolly", "fairymoblongrange_mad"], ["nururingtanker_fairy", "magicfork_jolly", "nururingarcher_fairy"]],
      ],
      elites: [["magicfork_mad", "nururingsupporter_fairy"], ["ginseng_mad", "magicfork_naive", "fairymoblongrange_jolly"], ["ginseng_cool", "lupalu_mad"], ["nururingtanker_fairy", "nururingwarrior_fairy", "nururingarcher_fairy", "nururingsupporter_fairy"]],
      boss: ["clone_carrot", "ginseng_gloomy", "magicfork_naive"] },
    { name: "요정 왕도", sub: "과자 거리 · 왕궁", land: "요정 왕도", bg: { fight: "stage8_1", boss: "stage9_1", event: "stage1_1" },
      pools: [
        [["fairymobcloserange_naive", "fairymoblongrange_jolly"], ["buseuleogi_mad", "buseuleogi_mad", "marshmallowdealer_jolly"], ["mogmaekim_jolly", "fairymoblongrange_jolly"], ["marshmallowtanker_naive", "marshmallowdealer_jolly"], ["fairymobcloserange_naive", "buseuleogi_cool"]],
        [["mogmaekim_jolly", "mogmaekim_jolly", "buseuleogi_mad"], ["marshmallowtanker_naive", "marshmallowdealer_jolly", "marshmallowsupporter_jolly"], ["fairymobcloserange_gloomy", "buseuleogi_cool"], ["mogmaekim_mad", "fairymoblongrange_jolly"], ["fairymobcloserange_naive", "marshmallowdealer_mad"]],
        [["marshmallowtanker_naive", "mogmaekim_mad", "marshmallowsupporter_jolly"], ["fairymobcloserange_gloomy", "fairymobcloserange_naive", "fairymoblongrange_jolly"], ["buseuleogi_mad", "buseuleogi_cool", "marshmallowdealer_mad", "marshmallowsupporter_jolly"], ["mogmaekim_jolly", "mogmaekim_mad", "marshmallowdealer_jolly"], ["nururingtanker_fairy", "marshmallowdealer_mad", "fairymobcloserange_gloomy"]],
      ],
      elites: [["goldring_gloomy", "buseuleogi_mad", "fairymoblongrange_jolly"], ["goldring_jolly", "marshmallowsupporter_jolly"], ["marshmallowtanker_naive", "goldring_gloomy"], ["goldring_jolly", "mogmaekim_mad", "mogmaekim_jolly"]],
      boss: ["clone_erpin", "marshmallowtanker_naive", "marshmallowsupporter_jolly"] },
  ],
};

// 2층(왕도) 몸은 웹판 1층 값에서 왔다 — 안쪽 눈금으로 올린다
tune(L, ["fairymobcloserange_naive", "fairymobcloserange_gloomy", "buseuleogi_mad", "buseuleogi_cool", "mogmaekim_jolly", "mogmaekim_mad", "marshmallowtanker_naive",
  "marshmallowdealer_jolly", "marshmallowdealer_mad", "marshmallowsupporter_jolly", "goldring_gloomy", "goldring_jolly"], 1.15, 1.1);

export default bundle(village, L, []);
