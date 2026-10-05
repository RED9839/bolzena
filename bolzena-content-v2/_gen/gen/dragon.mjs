import { A, BK, AL, MU, BL, GU, HE, BF, DB, JM, AC, CH, P, E, clone, bundle, tune } from "./lib.mjs";

// 불길과 물길의 터 — 1층 용족 동굴 어귀(용족 서식지 근처) · 2층 용족 동굴(동석이 지키는 굴, 다야의 자리). 단단함과 서열.
const W = ["우울"]; // 누루링-용족 약점
const L = [
  // ── 1층 동굴 어귀 — 저격(목도룡) · 직선 공격(길어용) ─────
  E("hatchling_jolly", "목도룡 · 주인 찾기", 320, "back", "jolly", "저격 — 주인을 찾아 헤매는 목도룡. 후열을 노린다.",
    [BK(90, "후열을 노린다", { w: 2 }), DB("약화", 1, "낑낑댄다"), BL(60, "날개로 가린다")]),
  E("imoogi_naive", "길어용", 420, "front", "naive", "직선 공격 — 특별한 존재가 되겠다는 욕심. 앞뒤를 한 줄로 꿰뚫는다(관통).",
    [BK(110, "직선 돌진", { w: 2 }), BL(80, "똬리를 튼다"), A(100, "꼬리 치기")]),
  E("hatchling_naive", "목도룡", 360, "back", "naive", "저격 · 깨면 끊긴다 — 퇴화한 용족이라는 소문. 확정 치명처럼 큰 한 발.",
    [BK(100, "후열 물기", { w: 2 }), CH("숨을 들이쉰다", BK(210, "치명 물기"), { brk: true }), BL(70, "웅크린다")]),
  E("hatchling_gloomy", "목도룡 · 축 처진", 340, "back", "gloomy", "디버퍼 — 주변 분위기를 처지게 한다. 약화 · 취약.",
    [DB("약화", 2, "축 처진 한숨"), DB("취약", 1, "처진 눈빛"), BK(90, "힘없는 물기")]),
  E("imoogi_gloomy", "길어용 · 응달", 460, "front", "gloomy", "굳히기 · 느긋이 — 응달에 늘어진 아룡.",
    [BL(130, "길게 늘어진다", { tough: 2, rush: 0 }), BK(140, "느릿한 직선", { rush: 0 }), HE(60, "응달에서 쉰다")]),
  E("imoogi_cool", "길어용 · 얼음 광석", 440, "front", "cool", "방패 · 깨면 무른다 — 모은 광석이 얼음이었다. 격파하면 녹아 취약 2.",
    [GU(80, "얼음 광석을 쌓는다", { tough: 1 }), BK(120, "얼음 돌진"), AC("st_rubble", 1, "draw", "광석 더미")],
    { passives: [P("녹는 광석", "broken", BF("취약", 2))] }),
  E("hatchling_mad", "목도룡 · 옛 영광", 400, "back", "mad", "일어서면 성난다 · 저격 — 옛 영광에 집착한다.",
    [BK(120, "옛 영광의 일격", { w: 2 }), MU(40, 3, "발톱 난무"), BF("사기", 1, "왕년엔 말이야")],
    { passives: [P("아직 용이다", "recover", BF("사기", 1))] }),
  E("imoogi_mad", "길어용 · 무차별", 480, "front", "mad", "연타 · 달아오름 — 경쟁자에게 무차별로 덤빈다. 턴 끝마다 사기.",
    [MU(45, 3, "무차별 물기", { w: 2 }), BK(130, "직선 돌진"), BL(80, "똬리")],
    { passives: [P("경쟁심", "turnEnd", BF("사기", 1))] }),
  E("nururingtanker_dragon", "누루링-용족 탱커", 660, "front", null, "방패(강인도 5) · 단단한 식감 — 피해 감소 2. 격파하면 취약 1.",
    [GU(90, "단단한 젤리", { tough: 1 }), A(120, "투지의 박치기"), BF("피해 감소", 2, "식감이 단단해진다")],
    { weak: W, tough: 5, passives: [P("부서진 껍질", "broken", BF("취약", 1))] }),
  E("nururingwarrior_dragon", "누루링-용족 전사", 460, "front", null, "깨면 끊긴다 · 일어서면 성난다 — 투지가 남다르다.",
    [A(120, "투지의 주먹", { w: 2 }), CH("투지를 끌어올린다", A(230, "용의 일격"), { brk: true }), BF("사기", 1, "포효")],
    { weak: W, passives: [P("남다른 투지", "recover", BF("사기", 1))] }),
  E("nururingarcher_dragon", "누루링-용족 마법사", 320, "back", null, "끼워 넣기 — 「용암 방울」.",
    [BK(90, "용암 침", { w: 2 }), AC("st_lava", 1, "draw", "용암을 튀긴다"), DB("약화", 1, "열기")], { weak: W }),
  E("nururingsupporter_dragon", "누루링-용족 서포터", 340, "back", null, "치유사 · 깃발(적 전체 결의 1).",
    [HE(90, "단단하게 굳혀 준다"), BF("결의", 1, "투지를 북돋는다", { all: true }), BK(70, "톡", { w: 2 })], { weak: W }),
  // 1층 엘리트
  E("hatchling_cool", "목도룡 · 유파 창시자", 640, "back", "cool", "엘리트 · 광역 시험 — 비슷한 처지를 모아 새 유파를 세운다. 제자가 쓰러질 때마다 적 전체 사기. 한꺼번에 눕히거나 스승부터.",
    [BK(130, "유파의 한 수"), BF("결의", 1, "가르침", { all: true }), CH("유파 비기", AL(120, "목도룡 연환격"), { brk: true }), DB("취약", 1, "제자들아!")],
    { passives: [P("제자를 잃었다", "allyDown", BF("사기", 1, "", { all: true }), { limit: 0 })] }),
  E("imoogi_jolly", "길어용 · 궁극의 준비", 720, "front", "jolly", "엘리트 · 방어 벗기기 — 궁극의 준비 상태. 첫 턴 두꺼운 방어를 실드 유지로 넘기고 모은 힘을 터뜨린다. 방어 파괴 · 관통 · 격파.",
    [BL(180, "준비 완료", { tough: 1 }), BF("실드 유지", 2, "비늘을 세운다"), CH("궁극의 힘을 모은다", AL(130, "궁극의 직선"), { brk: true }), BK(130, "직선 돌진")],
    { open: BL(260, "궁극의 준비 상태"), passives: [P("준비가 깨졌다", "broken", BF("취약", 2))] }),
  // 1층 보스 — 루드(클론) + 근육인데용
  E("proteindragon_naive", "근육인데용", 460, "front", "naive", "굳히기 — 아령을 내려놓지 않는다.",
    [A(130, "아령으로 친다", { w: 2 }), BL(110, "근육을 조인다", { tough: 1 }), AC("st_rubble", 1, "draw", "아령을 떨어뜨린다")]),
  E("clone_rude", "루드 (클론)", 3300, "front", "jolly",
    "1층 보스 · 시험: 큰 한 방 — 파.워. 하우스의 루드. 「단백질 보충」 맞을 때마다 방어 45(턴당 넷) — 잘게 여러 번 치면 다 막힌다. 굵은 한 방 · 방어 파괴 · 지속 피해로.",
    [A(160, "헬스 펀치"), BL(160, "근육을 부풀린다", { tough: 1 }), MU(50, 3, "세트 반복"), CH("바벨을 들어 올린다", MU(40, 5, "임팩트 프레스", { id: "약화", per: 1 }), { brk: true }), AC("st_rubble", 1, "draw", "원판을 던진다")],
    { boss: true,
      phase: { at: 0.5, say: "「이제 진짜 세트다!」", intents: [A(200, "고중량 펀치"), MU(55, 4, "크로스핏"), JM(1, "숨 고르기 강요"), CH("최고 중량", AL(160, "임팩트 프레스!"))] },
      passives: [
        P("단백질 보충", "hurt", BL(45), { limit: 4 }),
        P("크로스핏 선발대", "turnStart", BF("결의", 1), { phase: [1] }),
        P("승복", "broken", BF("취약", 2)),
      ] }, { art: clone("rude", "루드") }),

  // ── 2층 용족 동굴 — 방패(동석) · 깃발 · 치유사(근육인데용) ─────
  E("golem_naive", "동석 · 불순물", 520, "front", "naive", "방패 — 불순물 섞인 보석 골렘. 무겁지만 느리다.",
    [GU(80, "몸으로 막는다", { tough: 1 }), A(130, "돌주먹", { rush: 0 }), BL(110, "굳는다")]),
  E("golem_gloomy", "동석 · 한숨", 600, "front", "gloomy", "방패(강인도 6) · 느긋이 — 한숨 쉬는 골렘. 격파하면 무른다.",
    [BL(150, "한숨을 쉰다", { tough: 1, rush: 0 }), A(160, "무거운 내리치기", { rush: 0 }), GU(80, "동굴을 막는다")],
    { tough: 6, passives: [P("금 간 보석", "broken", BF("취약", 2))] }),
  E("golem_mad", "동석 · 장난당한", 540, "front", "mad", "가시 · 격노 — 유령에게 장난당해 기분이 나쁘다. 맞으면 되친다(턴당 둘).",
    [A(140, "화풀이", { w: 2 }), MU(50, 3, "돌 파편"), BF("사기", 1, "씩씩댄다")],
    { passives: [P("기분 나쁨", "hurt", { t: "thorns", v: 20 }, { limit: 2 })] }),
  E("proteindragon_gloomy", "근육인데용 · 재활", 480, "front", "gloomy", "치유사(자기) — 부상에서 재활 중. 턴 끝마다 회복 50.",
    [A(120, "재활 펀치"), BL(100, "보호대를 조인다"), HE(80, "스트레칭")],
    { passives: [P("재활", "turnEnd", { t: "selfHeal", v: 50 })] }),
  E("proteindragon_jolly", "근육인데용 · 단련", 500, "front", "jolly", "달아오름 — 하루도 빼먹지 않는 단련. 턴 끝마다 사기.",
    [A(130, "단련된 주먹", { w: 2 }), MU(45, 3, "푸시업 펀치"), BL(90, "버틴다")],
    { passives: [P("하루도 빼먹지 않는다", "turnEnd", BF("사기", 1))] }),
  E("golem_jolly", "동석 · 장식", 620, "front", "jolly", "깃발(적 전체 방어) — 임무를 잘 해낸 개체를 꾸며 주는 관습. 동료를 지킨다.",
    [GU(100, "보석 장식 방벽", { tough: 1 }), A(140, "자랑스러운 주먹"), BF("결의", 1, "반짝인다", { all: true })]),
  E("proteindragon_mad", "근육인데용 · 조교", 470, "front", "mad", "깃발(적 전체 사기 1) — 다른 자들을 훈련시키려 한다.",
    [BF("사기", 1, "「한 세트 더!」", { all: true }), A(140, "시범 펀치", { w: 2 }), DB("약화", 1, "얼차려")]),
  E("proteindragon_cool", "근육인데용 · 꼼수", 480, "front", "cool", "불굴 · 녹이기 — 어떤 약을 먹는 꼼수. 맞으면 둔해진다.",
    [BF("불굴", 1, "수상한 약"), A(140, "약 기운 펀치", { id: "손상", n: 2 }), MU(45, 3, "난타")]),
  // 2층 엘리트
  E("golem_cool_elite", "동석 · 수호 골렘", 820, "front", "cool", "엘리트 · 격파 시험 — 강인도 8, 깨지 않으면 턴마다 불굴이 쌓인다. 격파하면 취약 3. 강인도 피해를 넣는 손을 시험한다.",
    [GU(110, "동굴을 지킨다", { tough: 1 }), A(170, "수호의 일격"), CH("보석 핵을 달군다", AL(130, "보석 폭발"), { brk: true })],
    { tough: 8, passives: [P("굳건한 수호", "turnEnd", BF("불굴", 1)), P("핵이 드러난다", "broken", BF("취약", 3))] },
    { art: { spine: "monsterspine/golem", skin: "cool", icon: "icon_golemcool", scale: 1.3 } }),
  // 2층 보스 — 다야(클론) + 동석(다야가 만든 골렘) 둘
  E("golem_cool", "동석 · 다야의 골렘", 500, "front", "cool", "다야가 직접 만든 골렘 — 다야 앞을 막는다. 쓰러지면 다야가 반짝임으로 받아친다.",
    [GU(90, "다야를 지킨다", { tough: 1 }), A(140, "보석 주먹")]),
  E("clone_daya", "다야 (클론)", 2300, "back", "naive",
    "2층 보스 · 시험: 마무리 레이스 — 용족의 1인자. 30% 아래에서 「다이아 브레…츄!」 를 두 턴 동안 모은다(끊기지 않는다) — 그 전에 쓰러뜨리거나 그 한 방을 버틸 준비를 해 둔다. 골렘이 쓰러지면 연쇄 피어스.",
    [BK(150, "다이아 피어스"), MU(45, 3, "다이아 쓰라림", { id: "균열", per: 1 }), DB("취약", 1, "반짝임"), CH("다이아를 모은다", AL(150, "다이아 폭발"), { brk: true }), AC("st_lava", 1, "draw", "용암을 깨운다")],
    { boss: true,
      phase: { at: 0.6, say: "「1인자의 반짝임을 보여 줄게」", intents: [BK(170, "연쇄 피어스"), AL(80, "다이아 비", { id: "균열", n: 2 }), BF("사기", 1, "반짝반짝"), CH("다이아 창을 세운다", BK(300, "다이아 랜스"), { brk: true })] },
      phase2: { at: 0.3, say: "「다이아 브레…」", intents: [CH("숨을 들이쉰다 — 「다이아 브레…」", CH("다이아가 빛난다 — 「…츄…」", AL(190, "다이아 브레…츄!"))), BK(150, "남은 피어스")] },
      passives: [
        P("연쇄 피어스", "allyDown", BK(80), { limit: 0 }),
        P("다이아의 반짝임", "broken", BF("취약", 2), { phase: [0, 1] }),
        P("1인자", "turnEnd", BF("사기", 1), { phase: [2] }),
      ] }, { art: clone("daya", "다야") }),
];

const village = {
  id: "dragon", name: "불길과 물길의 터", race: "용족",
  line: "얼음물이 흐르는 동굴 어귀에서 용암이 끓는 용족 동굴로 — 서열을 따지는 용족의 땅",
  floors: [
    { name: "용족 동굴 어귀", sub: "용족 서식지 근처 · 물길", land: "용족 동굴 어귀", bg: { fight: "stage31_1", boss: "stage28_1", event: "stage14_1" },
      pools: [
        [["hatchling_jolly", "imoogi_naive"], ["nururingwarrior_dragon", "nururingsupporter_dragon"], ["imoogi_naive", "hatchling_naive"], ["hatchling_jolly", "imoogi_gloomy"], ["imoogi_cool", "hatchling_jolly"]],
        [["imoogi_gloomy", "hatchling_gloomy"], ["imoogi_cool", "hatchling_naive", "hatchling_jolly"], ["nururingtanker_dragon", "nururingarcher_dragon"], ["imoogi_naive", "hatchling_mad"], ["imoogi_mad", "hatchling_gloomy"]],
        [["imoogi_mad", "hatchling_mad", "hatchling_naive"], ["nururingtanker_dragon", "nururingwarrior_dragon", "nururingsupporter_dragon"], ["imoogi_cool", "imoogi_gloomy", "hatchling_gloomy"], ["imoogi_mad", "nururingarcher_dragon", "hatchling_jolly", "hatchling_jolly"], ["nururingtanker_dragon", "hatchling_mad", "imoogi_naive"]],
      ],
      elites: [["hatchling_cool", "hatchling_naive", "hatchling_jolly"], ["imoogi_jolly", "nururingsupporter_dragon"], ["nururingtanker_dragon", "nururingwarrior_dragon", "nururingarcher_dragon", "nururingsupporter_dragon"], ["hatchling_cool", "imoogi_gloomy"]],
      boss: ["clone_rude", "proteindragon_naive"] },
    { name: "용족 동굴", sub: "동석이 지키는 굴 · 불길", land: "용족 동굴", bg: { fight: "stage21_1", boss: "stage13_1", event: "stage20_1" },
      pools: [
        [["golem_naive", "proteindragon_naive"], ["nururingwarrior_dragon", "proteindragon_naive"], ["golem_naive", "hatchling_mad"], ["proteindragon_gloomy", "nururingarcher_dragon"], ["golem_naive", "nururingsupporter_dragon"]],
        [["golem_gloomy", "proteindragon_jolly"], ["golem_mad", "proteindragon_gloomy"], ["proteindragon_naive", "proteindragon_jolly", "nururingarcher_dragon"], ["golem_mad", "golem_naive"], ["proteindragon_cool", "nururingsupporter_dragon"]],
        [["golem_jolly", "proteindragon_mad", "proteindragon_naive"], ["golem_gloomy", "golem_mad", "nururingarcher_dragon"], ["proteindragon_mad", "proteindragon_jolly", "proteindragon_cool"], ["nururingtanker_dragon", "golem_jolly", "proteindragon_gloomy"], ["golem_jolly", "proteindragon_cool", "golem_naive", "nururingarcher_dragon"]],
      ],
      elites: [["golem_cool_elite", "golem_naive"], ["proteindragon_mad", "proteindragon_jolly", "proteindragon_gloomy"], ["golem_cool_elite", "proteindragon_cool"], ["golem_jolly", "golem_gloomy", "golem_mad"]],
      boss: ["clone_daya", "golem_cool", "golem_cool"] },
  ],
};

// 2층(동굴) 일반 몸 — 한 눈금 덜어 낸다
tune(L, ["golem_naive", "golem_gloomy", "golem_mad", "golem_jolly", "proteindragon_gloomy", "proteindragon_jolly", "proteindragon_mad", "proteindragon_cool", "golem_cool"], 0.92, 0.92);

export default bundle(village, L, []);
