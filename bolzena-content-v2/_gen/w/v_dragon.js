// 용족 터 — 결: 단단한 몸 — 과보호(받는 피해 1) · 갈증(받는 피해 감소, 맞으면 줄어듦) · 단단함 · 실드가 남으면 사기 · 쌓여서 1턴 뒤 강공 · 보물(결정화)
const { A, B, M, AA, BL, G, H, BF, BFA, DB, J, AC, CD, HC, SM, CT, SH, CH, RS, TH, SHL, RV, FG, P } = require("./lib");
module.exports = {
  // ── 1층 용족 동굴 어귀 ──
  hatchling_jolly: {
    pick: "shuffle", tied: true, blurb: "주인을 찾아 헤매는 목도룡(창시자가 불러낸 제자는 창시자가 쓰러지면 흩어진다) — 턴마다 「두리번」 이 올라 셋이면 다음 차례에 뒷줄을 크게 노린다.",
    intents: [B(80, "뒷줄을 쫀다"), A(60, "꼬리 치기"), BL(50, "날개로 감싼다")],
    counters: [{ name: "두리번", desc: "주인을 찾는 눈길", onTurnStart: 1, max: 3, at: 3, act: B(130, "주인님인 줄 알았어!"), mode: "next" }],
  },
  imoogi_naive: {
    pick: "shuffle", blurb: "특별해지고 싶은 길어용 — 몸을 곧게 뻗어 실드를 건너 꿰뚫는다.",
    intents: [B(110, "꿰뚫는 돌진"), A(80, "몸통 감기"), BL(70, "똬리를 튼다")],
  },
  hatchling_naive: {
    pick: "cycle", blurb: "퇴화했다는 소문의 목도룡 — 숨을 모아 큰 한 발. 모으는 동안 격파하면 끊긴다.",
    intents: [B(70, "작은 불씨"), CH("숨을 크게 들이쉰다", B(200, "용의 숨결"), true), A(60, "물기")],
  },
  hatchling_gloomy: {
    pick: "shuffle", blurb: "축 처진 목도룡 — 분위기를 처지게 해 약화 · 취약을 건다.",
    intents: [DB("약화", 1, "한숨"), DB("취약", 1, "축 처진 눈빛"), B(70, "힘없는 불씨")],
  },
  imoogi_gloomy: {
    pick: "shuffle", blurb: "응달의 길어용 — 「갈증」 셋: 하나당 받는 피해 -10%, 맞을 때마다 하나씩 빠진다(턴 시작에 다시 셋). 잘게 쳐서 벗긴 뒤 크게.",
    intents: [A(100, "느긋한 휘감기", { rush: 0 }), BL(80, "응달에 숨는다"), A(90, "꼬리 채찍")],
    counters: [{ name: "갈증", desc: "메마른 비늘 — 맞을수록 벗겨진다", start: 3, max: 3, taken: -0.1, onHit: -1, resetTurnStart: true }],
  },
  imoogi_cool: {
    pick: "cycle", blurb: "얼음 광석을 모은 길어용 — 두꺼운 실드를 두르고, 실드가 깨지면 광석을 던지는 수로 바꾼다. 격파하면 녹아 취약 2.",
    intents: [BL(100, "광석 갑옷", { tough: 1 }), A(90, "광석 박치기"), BL(80, "광석을 덧댄다")],
    passives: [P("광석이 깨지면", "guardBreak", SH(A(140, "광석 던지기"), "광석이 깨진다")), P("녹아내린다", "broken", BF("취약", 2, "녹아내린다"))],
  },
  hatchling_mad: {
    pick: "shuffle", blurb: "옛 영광에 집착하는 목도룡 — 격파에서 일어서면 성난다. 뒷줄을 노린다.",
    intents: [B(100, "옛 영광의 불"), A(70, "발톱"), BL(60, "웅크린다")],
    passives: [P("옛 영광", "recover", BF("사기", 1, "나는 위대한 용이었다"))],
  },
  imoogi_mad: {
    pick: "shuffle", blurb: "무차별 길어용 — 내 턴이 끝날 때마다 「흥분」 이 올라 피해가 커진다(최대 4). 짧게 끝내라.",
    intents: [M(35, 3, "무차별 물기"), A(100, "몸통 박치기"), M(45, 2, "꼬리 연타")],
    counters: [{ name: "흥분", desc: "싸울수록 달아오른다", onTurnEnd: 1, dealt: 0.1, max: 4 }],
  },
  nururingtanker_dragon: {
    pick: "cycle", tough: 5, blurb: "단단한 식감의 젤리 방패 — 「단단한 식감」 이 있는 동안 받는 피해가 1(맞으면 하나 줄어듦, 턴마다 다시 하나). 첫 대를 가볍게. 격파하면 취약 1.",
    intents: [BF("피해 감소", 2, "단단해진다"), A(90, "젤리 박치기"), G(60, "젤리 방벽", { tough: 1 }), A(110, "깔고 앉는다")],
    counters: [{ name: "단단한 식감", desc: "딱딱하게 굳은 겉면", start: 1, max: 1, flat: 1, onHit: -1, resetTurnStart: true }],
    passives: [P("물러진다", "broken", BF("취약", 1, "물러진다"))],
  },
  nururingwarrior_dragon: {
    pick: "cycle", blurb: "투지가 남다른 젤리 — 모으는 한 방은 격파로 끊기고, 격파에서 일어서면 성난다.",
    intents: [A(90, "젤리 주먹"), CH("투지를 불태운다", A(200, "용 젤리 펀치"), true), BL(50, "버틴다")],
    passives: [P("오기", "recover", BF("사기", 1, "오기"))],
  },
  nururingarcher_dragon: {
    pick: "shuffle", blurb: "돌을 던지는 젤리 — 「돌무더기」 를 끼워 넣어 손을 막는다.",
    intents: [AC("st_rubble", 1, "draw", "돌무더기를 쌓는다"), B(70, "돌 던지기"), DB("손상", 1, "자갈 세례")],
  },
  nururingsupporter_dragon: {
    pick: "shuffle", blurb: "응원하는 젤리 — 치유하고 적 전체 결의를 올린다.",
    intents: [H(80, "젤리 붕대"), BFA("결의", 1, "용의 비늘처럼!"), A(50, "톡 친다")],
  },
  hatchling_cool: {
    pick: "shuffle", rare: [{ id: "toughGuard" }], blurb: "엘리트 · 유파 창시자 목도룡 — 제자(목도룡)를 불러 모으고(최대 1), 제자가 쓰러질 때마다 적 전체 사기. 스승부터 쓰러뜨리면 부른 제자도 흩어진다. 희귀종: 받는 강인도 피해 -20%.",
    intents: [SM("hatchling_jolly", 1, 1, "제자 모집"), B(110, "창시자의 불"), M(45, 3, "유파 연격"), BFA("불굴", 1, "유파의 가르침")],
    passives: [P("제자의 원수", "allyDown", BFA("사기", 1, "제자의 원수"))],
  },
  imoogi_jolly: {
    pick: "cycle", rare: [{ id: "crystal" }], blurb: "엘리트 · 궁극의 준비 길어용 — 첫 턴 두꺼운 실드를 「실드 보존」 으로 넘기고, 턴이 끝날 때 실드가 남아 있으면 사기. 모은 힘을 터뜨린다. 실드를 깨고 넘겨라. 희귀종: 전투 시작 결정화 3.",
    open: BL(220, "궁극의 준비"),
    intents: [A(120, "준비 운동"), CH("힘을 끌어모은다", AA(150, "궁극의 일격")), BL(120, "다시 준비"), M(50, 3, "몸풀기 연타")],
    passives: [P("만반의 준비", "fightStart", BF("실드 보존", 1, "만반의 준비")), P("준비 완료", "turnEnd", BF("사기", 1, "준비 완료", { if: { selfBlock: true } }))],
  },
  proteindragon_naive: {
    pick: "shuffle", blurb: "아령을 내려놓지 않는 근육인데용 — 실드를 두르고, 턴이 끝날 때 실드가 남아 있으면 사기. 실드를 깨고 넘겨라.",
    intents: [BL(80, "근육 펌핑"), A(100, "아령 휘두르기"), A(80, "헤드락")],
    passives: [P("근육 자랑", "turnEnd", BF("사기", 1, "근육 자랑", { if: { selfBlock: true } }))],
  },
  clone_rude: {
    pick: "cycle", blurb: "1층 보스 · 루드(클론) — 파.워. 하우스의 헬창. 맞을 때마다 단백질 보충으로 실드 45(턴당 2). 턴마다 「세트」 가 차 넷이면 다음 차례에 임팩트 프레스. 실드를 깨고 넘기거나 큰 한 방으로. 격파하면 세트가 끊긴다.",
    intents: [A(160, "헬스 펀치"), BL(160, "근육을 부풀린다", { tough: 1 }), M(50, 3, "세트 반복"), AC("st_rubble", 1, "draw", "원판을 던진다")],
    phase: { at: 0.5, say: "「이제 진짜 세트다!」", intents: [A(200, "고중량 펀치"), M(55, 4, "크로스핏"), J(1, "숨 고르기 강요"), BL(180, "벌크업")] },
    counters: [{ name: "세트", desc: "한 세트 더 — 턴마다 쌓인다", onTurnStart: 1, max: 4, at: 4, act: AA(150, "임팩트 프레스!", { id: "약화", n: 1 }), mode: "next" }],
    passives: [P("단백질 보충", "hurt", BL(45, "단백질 보충"), { limit: 2 }), P("승복", "broken", CT("세트", -4, "세트가 끊겼다"))],
  },
  // ── 2층 용족 동굴 ──
  golem_naive: {
    pick: "cycle", blurb: "불순물 섞인 보석 골렘 — 「불순물 껍질」 둘이 남은 동안 받는 피해가 1(맞으면 하나씩 깨짐). 다시 덧씌우기 전에 벗기고 크게.",
    intents: [A(100, "보석 주먹"), CT("불순물 껍질", 2, "불순물을 덧씌운다"), A(90, "무거운 발걸음", { rush: 0 })],
    counters: [{ name: "불순물 껍질", desc: "탁한 껍질 — 아프지 않다", start: 2, max: 2, flat: 1, onHit: -1 }],
  },
  golem_gloomy: {
    pick: "cycle", tough: 6, blurb: "한숨 쉬는 골렘 — 「한숨 껍질」 을 둘러 받는 피해가 줄고 턴이 끝나면 풀린다. 큰 수는 느리다. 격파하면 무르다(취약 2).",
    intents: [CT("한숨 껍질", 3, "깊은 한숨"), A(120, "느릿한 주먹", { rush: 0 }), BL(90, "웅크린다")],
    counters: [{ name: "한숨 껍질", desc: "한숨으로 굳힌 몸 — 턴이 끝나면 풀린다", taken: -0.1, max: 3, clearTurnEnd: true }],
    passives: [P("무너진 한숨", "broken", BF("취약", 2, "무너진다"))],
  },
  golem_mad: {
    pick: "shuffle", blurb: "장난당한 골렘 — 맞으면 되친다(턴당 2). 맞을 때마다 「짜증」 이 쌓여 다섯이면 바로 내리찍는다.",
    intents: [A(110, "짜증 주먹"), BL(80, "돌을 덧댄다"), M(40, 2, "발 구르기")],
    counters: [{ name: "짜증", desc: "또 장난이야?!", onHit: 1, max: 9, at: 5, act: A(180, "버럭 내리찍기"), mode: "now" }],
    passives: [P("돌가시", "hurt", TH(35, "돌가시"), { limit: 2 })],
  },
  proteindragon_gloomy: {
    pick: "shuffle", blurb: "재활 중인 근육인데용 — 턴 끝마다 스스로 회복 50. 느린 지속 피해보다 몰아 치기.",
    intents: [BL(80, "재활 스트레칭"), A(100, "재활 펀치"), H(80, "프로틴 셰이크")],
    passives: [P("재활", "turnEnd", SHL(50, "재활"))],
  },
  proteindragon_jolly: {
    pick: "shuffle", blurb: "하루도 빼먹지 않는 단련 — 턴마다 「단련」 이 올라 셋이면 다음 차례에 백 번째 스쿼트(큰 한 방).",
    intents: [A(100, "단련 펀치"), BL(80, "플랭크"), M(40, 3, "잽 잽 잽")],
    counters: [{ name: "단련", desc: "매일의 단련", onTurnStart: 1, max: 3, at: 3, act: A(210, "백 번째 스쿼트"), mode: "next" }],
  },
  golem_jolly: {
    pick: "cycle", blurb: "꾸밈받은 장식 골렘 — 적 전체 실드를 두르고, 제 실드가 깨지면 남은 장식으로 동료를 지킨다.",
    intents: [G(60, "장식 방벽", { tough: 1 }), A(100, "장식 주먹"), BL(90, "장식을 덧단다")],
    passives: [P("장식이 떨어져도", "guardBreak", G(40, "장식이 떨어져도 동료를"))],
  },
  proteindragon_mad: {
    pick: "shuffle", blurb: "조교 근육인데용 — 적 전체 사기를 올리고, 같은 사도의 카드를 잇달아 내면 「자세 불량!」 으로 바로 친다(턴당 1).",
    intents: [BFA("사기", 1, "하나 더!"), A(100, "조교의 주먹"), BL(70, "시범 자세")],
    passives: [P("자세 불량", "card", A(100, "자세 불량!"), { same: true })],
  },
  proteindragon_cool: {
    pick: "shuffle", blurb: "꼼수 근육인데용 — 불굴을 두르고, 맞으면 수상한 약을 뿌려 손상 1(턴당 1).",
    intents: [BF("불굴", 1, "수상한 약"), A(100, "약발 펀치"), BL(80, "근육 위장")],
    passives: [P("약 뿌리기", "hurt", DB("손상", 1, "약 뿌리기"))],
  },
  golem_cool_elite: {
    pick: "cycle", tough: 8, rare: [{ id: "crystal" }], blurb: "엘리트 · 수호 골렘 — 강인도 8. 턴마다 「수호 결」 이 쌓여 받는 피해가 줄고(최대 5), 격파하면 결이 깨지며 취약 3. 강인도 피해를 넣는 손을 시험한다. 희귀종: 전투 시작 결정화 3.",
    intents: [A(130, "수호 주먹"), BL(130, "수호 결계", { tough: 1 }), M(50, 3, "돌주먹 연타"), A(150, "대지 강타")],
    counters: [{ name: "수호 결", desc: "쌓여 가는 결정의 결", onTurnStart: 1, taken: -0.08, max: 5 }],
    passives: [P("결이 깨진다", "broken", CT("수호 결", -5, "결이 깨진다")), P("드러난 핵", "broken", BF("취약", 3, "핵이 드러난다"))],
  },
  golem_cool: {
    pick: "cycle", blurb: "다야가 직접 만든 골렘 — 다야 앞을 막아 적 전체 실드를 두른다. 쓰러지면 다야가 연쇄 피어스로 받아친다.",
    intents: [G(60, "주인을 지킨다", { tough: 1 }), A(100, "보석 주먹"), BL(90, "보석 방패")],
  },
  clone_daya: {
    pick: "cycle", blurb: "2층 보스 · 다야(클론) — 용족의 1인자. 턴마다 「감정가」 가 올라 셋이면 다음 차례에 다이아 랜스(큰 관통). 격파하면 감정가가 무너지고 취약 2. 골렘이 쓰러지면 연쇄 피어스. 30% 아래에서는 「다이아 브레…츄!」 를 두 턴 모은다(끊기지 않는다).",
    intents: [B(150, "다이아 피어스"), M(45, 3, "다이아 쓰라림", { id: "균열", per: 1 }), AC("st_rubble", 1, "draw", "유리를 다이아라며 판다"), CH("다이아를 모은다", AA(150, "다이아 폭발"), true)],
    phase: { at: 0.6, say: "「1인자의 반짝임을 보여 줄게」", intents: [B(170, "연쇄 피어스"), AA(80, "다이아 비", { id: "균열", n: 2 }), BF("사기", 1, "반짝반짝"), DB("취약", 1, "눈부신 반짝임")] },
    phase2: { at: 0.3, say: "「다이아 브레…」", intents: [CH("숨을 들이쉰다 — 「다이아 브레…」", CH("다이아가 빛난다 — 「…츄…」", AA(190, "다이아 브레…츄!"))), B(150, "남은 피어스")] },
    counters: [{ name: "감정가", desc: "턴마다 오르는 감정가", onTurnStart: 1, max: 3, at: 3, act: B(240, "다이아 랜스"), mode: "next" }],
    passives: [P("연쇄 피어스", "allyDown", B(80, "연쇄 피어스"), { limit: 0 }), P("감정 실패", "broken", CT("감정가", -3, "감정가가 폭락한다")), P("흠집", "broken", BF("취약", 2, "흠집이 났다"))],
  },
};
