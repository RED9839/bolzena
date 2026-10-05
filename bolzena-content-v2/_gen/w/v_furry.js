// 수인 마을 — 결: 야성 — 쌓여서 강공(맞을 때 · 턴마다 · 카드마다) · 허기형 몸 · 같은 사도 연타에 받아침 · 행동 뒤 카드에 쌓임 · 사료 상태 카드
const { A, B, M, AA, BL, G, H, BF, BFA, DB, J, AC, CD, HC, SM, CT, SH, CH, RS, TH, SHL, RV, FG, P } = require("./lib");
module.exports = {
  // ── 1층 엘리아스 숲 ──
  furrywarriorcloserange_naive: {
    pick: "shuffle", blurb: "처음 싸워 보는 수련생 — 맞을 때마다 「투지」 가 붙어 주는 피해가 오른다(최대 3). 질질 끌지 말 것.",
    intents: [A(90, "주먹을 휘두른다"), BL(60, "자세를 잡는다"), A(70, "발차기")],
    counters: [{ name: "투지", desc: "맞을수록 오기가 생긴다", onHit: 1, dealt: 0.1, max: 3 }],
  },
  furrywarriorlongrange_gloomy: {
    pick: "cycle", blurb: "근접을 고를 걸 후회하는 궁수 — 활시위가 느려 큰 한 발을 다음 턴에 쏜다(끊기지 않는다). 그 전에 쓰러뜨려라.",
    intents: [B(70, "화살"), CH("활시위를 천천히 당긴다", B(160, "후회의 한 발")), A(50, "활대로 친다")],
  },
  oldtree_jolly: {
    pick: "shuffle", blurb: "숲의 고목 정령 — 첫 턴에 뒷줄을 기습한다. 그다음은 느긋하다.",
    open: B(90, "덤불 기습"),
    intents: [A(80, "가지 휘두르기"), BL(60, "껍질을 두른다"), A(90, "뿌리 걸기")],
  },
  gluttonbear_naive: {
    pick: "shuffle", blurb: "먹보 곰 — 「허기」 하나당 피해가 오르고 맞으면 빠진다(턴 시작에 둘). 카드로 당기면 음식을 지키려 실드를 두른다.",
    intents: [A(100, "앞발 후려치기"), BL(80, "음식을 끌어안는다"), M(45, 2, "허겁지겁")],
    counters: [{ name: "허기", desc: "배고픈 곰은 사납다", start: 2, max: 2, dealt: 0.2, onHit: -1, resetTurnStart: true }],
    passives: [P("내 밥이야", "rushed", BL(80, "내 밥이야"))],
  },
  furrywarriorcloserange_jolly: {
    pick: "shuffle", blurb: "졸업반 수련생 — 같은 사도의 카드를 잇달아 내면 건방지게 바로 받아친다(턴당 1). 격파에서 일어서면 사기.",
    intents: [A(100, "졸업 시험 펀치"), CH("기합을 넣는다", A(190, "졸업 작품"), true), BL(60, "폼 잡기")],
    passives: [P("빈틈 발견", "card", A(90, "빈틈 발견!"), { same: true }), P("다시 일어선다", "recover", BF("사기", 1, "다시 일어선다"))],
  },
  nependers_jolly: {
    pick: "shuffle", blurb: "사냥꾼 한입초 — 카드로 당기면 덥석 문다. 당기지 말고 차례를 넘겨 받거나 한 번에 끊어라.",
    intents: [A(90, "덥석"), BL(70, "꽃잎을 오므린다"), A(100, "사냥감 낚아채기")],
    passives: [P("덥석", "rushed", A(80, "또 덥석"))],
  },
  gluttonbear_jolly: {
    pick: "shuffle", blurb: "이상한 버섯을 먹은 곰 — 「포자증식」 을 뿌려 파티가 받는 피해를 키우고 약화를 건다.",
    intents: [DB("포자증식", 3, "흐릿한 포자"), A(100, "비틀비틀 후려치기"), DB("약화", 1, "몽롱한 하품")],
  },
  furrywarriorcloserange_mad: {
    pick: "shuffle", blurb: "험지파 광전사 — 턴마다 「기세」 가 올라 셋이면 다음 차례에 험지 돌파. 셋째 턴 전에 끝내거나 그 한 방을 버텨라.",
    intents: [A(110, "험지 주먹"), M(40, 3, "연속 차기"), BL(70, "숨을 고른다")],
    counters: [{ name: "기세", desc: "턴마다 오르는 기세", onTurnStart: 1, max: 3, at: 3, act: A(220, "험지 돌파"), mode: "next" }],
  },
  gluttonbear_mad: {
    pick: "shuffle", blurb: "굶주린 곰 — 「허기」 셋(맞으면 하나씩 빠짐, 턴 시작에 다시 셋). 동료가 쓰러지면 먹이를 노려 사기. 모으는 한 방은 격파로 끊긴다.",
    intents: [A(120, "굶주린 앞발"), CH("침을 흘린다", A(220, "한입에 꿀꺽"), true), M(45, 3, "할퀴기")],
    counters: [{ name: "허기", desc: "굶주린 곰은 사납다", start: 3, max: 3, dealt: 0.2, onHit: -1, resetTurnStart: true }],
    passives: [P("먹이다", "allyDown", BF("사기", 1, "먹이다!"))],
  },
  furrywarriorlongrange_cool: {
    pick: "shuffle", blurb: "묵묵한 궁수 — 제 차례가 지난 뒤에도 카드를 내면 「겨냥」 이 쌓여 넷이면 바로 관통 화살. 큰 한 발은 격파로 끊긴다.",
    intents: [B(90, "관통 화살"), CH("묵묵히 겨눈다", B(180, "묵묵한 한 발"), true), A(60, "활대")],
    counters: [{ name: "겨냥", desc: "움직임을 지켜본다", onCard: 1, afterAct: true, max: 9, at: 4, act: B(100, "틈을 꿰뚫는다"), mode: "now" }],
  },
  nururingtanker_furry: {
    pick: "cycle", tough: 5, blurb: "털 난 젤리 방패 — 맞으면 털이 곤두서 때린 사도에게 되친다(턴당 2). 실드가 깨지면 동료를 감싼다.",
    intents: [G(60, "털 뭉치 방벽", { tough: 1 }), A(90, "털 박치기"), BF("피해 감소", 2, "털을 부풀린다"), A(110, "깔고 앉는다")],
    passives: [P("곤두선 털", "hurt", TH(30, "곤두선 털"), { limit: 2 }), P("감싸기", "guardBreak", G(40, "동료를 감싼다"))],
  },
  nururingwarrior_furry: {
    pick: "shuffle", blurb: "도전적인 젤리 — 행동할 때마다 「신바람」 이 올라 주는 피해가 커진다(최대 4). 오래 두면 커진다.",
    intents: [A(80, "젤리 펀치"), M(35, 3, "신나는 연타"), BL(50, "통통 튄다")],
    counters: [{ name: "신바람", desc: "칠수록 신난다", dealt: 0.15, max: 4 }],
    passives: [P("신난다", "act", CT("신바람", 1, "신난다!"))],
  },
  nururingarcher_furry: {
    pick: "shuffle", blurb: "울부짖는 젤리 — 뽑히면 손패 1장을 떨구는 「울부짖음」 을 넣는다.",
    intents: [AC("st_howl", 1, "draw", "울부짖는다"), B(70, "젤리 화살"), DB("약화", 1, "으르렁")],
  },
  nururingsupporter_furry: {
    pick: "shuffle", blurb: "털을 골라 주는 젤리 — 치유하고 적 전체에 불굴을 두른다.",
    intents: [H(80, "털 고르기"), BFA("불굴", 1, "뭉치자 젤리!"), A(50, "톡 친다")],
  },
  gluttonbear_cool: {
    pick: "shuffle", rare: [{ id: "crystal" }], blurb: "엘리트 · 머곰 두목 — 동료가 쓰러질 때마다 적 전체 사기. 맞을 때마다 「분노」 가 쌓여 여덟이면 바로 전체를 후려친다. 한꺼번에 눕히거나 두목부터. 희귀종: 전투 시작 결정화 3.",
    intents: [A(130, "두목의 앞발"), M(50, 3, "할퀴기"), BL(100, "두목의 배짱"), A(150, "내리찍기")],
    counters: [{ name: "분노", desc: "맞을수록 화가 쌓인다", onHit: 1, max: 9, at: 8, act: AA(100, "두목의 포효"), mode: "now" }],
    passives: [P("두목의 호령", "allyDown", BFA("사기", 1, "두목의 호령"))],
  },
  furrywarriorcloserange_cool: {
    pick: "shuffle", rare: [{ id: "toughGuard" }], blurb: "엘리트 · 만년 수련생 — 맞을 때마다 받아 막는다(실드 40, 턴당 3). 같은 사도의 카드를 잇달아 내면 바로 연격(턴당 1). 잘게 여러 번보다 큰 한 방, 사도를 번갈아. 희귀종: 받는 강인도 피해 -20%.",
    intents: [A(130, "정권 지르기"), M(45, 3, "수련 연격"), BL(110, "철벽 자세"), A(150, "백 번째 지르기")],
    passives: [P("받아 막기", "hurt", BL(40, "받아 막기"), { limit: 3 }), P("간파", "card", M(40, 3, "간파 — 연격"), { same: true })],
  },
  clone_beni: {
    pick: "cycle", blurb: "1층 보스 · 베니(클론) — 먹보 곰. 적이 쓰러질 때마다 「한 입」 을 먹어 크게 회복하고 다음 행동이 세진다(행동하면 빠진다). 루포를 먼저 쓰러뜨리면 베니가 차오르니 둘을 함께 깎아라.",
    intents: [A(140, "곰 펀치"), M(45, 3, "허겁지겁 할퀴기"), BL(120, "배를 내민다", { tough: 1 }), CH("크게 숨을 들이쉰다", AA(140, "곰 박치기"), true)],
    phase: { at: 0.5, say: "「배고파… 뭐라도 먹어야 해!」", intents: [M(55, 4, "닥치는 대로 할퀴기"), A(180, "꿀단지 내려찍기"), H(160, "꿀을 퍼먹는다"), CH("앞발을 치켜든다", AA(160, "곰 박치기!"))] },
    counters: [{ name: "한 입", desc: "먹고 나면 힘이 난다 — 행동하면 빠진다", dealt: 0.25, max: 2 }],
    passives: [P("먹고 나면 힘", "allyDown", CT("한 입", 2, "냠냠"), { limit: 0 }), P("남은 간식", "allyDown", SHL(200, "남은 간식")), P("배부르다", "act", CT("한 입", -2, "배부르다"), { limit: 0 })],
  },
  clone_rufo: {
    pick: "cycle", blurb: "1층 보스 · 루포(클론) — 사료스탕스 브레인. 턴마다 「계획」 이 차 셋이면 다음 차례에 「계획대로인 것이다!」(전체 공격). 격파하면 계획이 엎어진다. 베니가 쓰러지면 계획대로 회복하며 사기 2.",
    intents: [BFA("피해 감소", 1, "엄호 작전"), B(110, "계산된 한 발"), DB("약화", 1, "교란 작전"), A(90, "지휘봉")],
    phase: { at: 0.5, say: "「계획 수정. 플랜 B 다.」", intents: [B(140, "플랜 B 사격"), DB("취약", 1, "약점 분석"), BFA("사기", 1, "작전 개시"), M(40, 3, "견제 사격")] },
    counters: [{ name: "계획", desc: "턴마다 맞아 들어가는 계획", onTurnStart: 1, max: 3, at: 3, act: AA(110, "계획대로인 것이다!"), mode: "next" }],
    passives: [P("역관광", "broken", CT("계획", -2, "계획이 엎어졌다")), P("계획대로", "allyDown", SHL(400, "계획대로"), { who: "clone_beni" }), P("복수 계획", "allyDown", BF("사기", 2, "복수 계획"), { who: "clone_beni" })],
  },
  // ── 2층 털 부락 ──
  foodscavenger_naive: {
    pick: "shuffle", blurb: "사료를 뿌리는 라쿤 — 턴 끝에 손에 있으면 약화를 거는 「사료 한 줌」 을 더미에 넣는다.",
    intents: [AC("st_kibble", 2, "draw", "사료를 뿌린다"), A(90, "앞발 할퀴기"), A(70, "물어뜯기")],
  },
  furring_naive: {
    pick: "shuffle", rush: 3, blurb: "갑자기 부자가 된 퍼리 — 돈 자랑으로 뽑을 더미 2장을 「침체」(비용 +1)시킨다. 카드 세 장이면 움직인다.",
    intents: [CD("침체", 2, "draw", "돈 자랑"), B(80, "금화를 던진다"), BL(60, "지갑을 끌어안는다")],
  },
  foodscavenger_jolly: {
    pick: "shuffle", rush: 3, blurb: "간식파 라쿤 — 카드로 당기면 사료를 손에 직접 넣는다. 당기지 말 것.",
    intents: [B(70, "과자 던지기"), AC("st_kibble", 1, "draw", "과자 섞인 사료"), A(60, "할퀴기")],
    passives: [P("간식 시간", "rushed", AC("st_kibble", 1, "hand", "간식 시간"))],
  },
  foodscavenger_gloomy: {
    pick: "shuffle", blurb: "굶주린 라쿤 — 「허기」 둘(맞으면 빠짐, 턴마다 다시). 35% 를 건너면 날이 서 마구 할퀸다 — 한 번에 넘겨라.",
    intents: [A(100, "굶주린 할퀴기"), BL(60, "웅크린다"), A(90, "물어뜯기")],
    counters: [{ name: "허기", desc: "굶주려 날이 섰다", start: 2, max: 2, dealt: 0.2, onHit: -1, resetTurnStart: true }],
    passives: [P("마구 할퀸다", "lowHp", M(40, 4, "마구 할퀸다"), { at: 0.35 })],
  },
  furring_gloomy: {
    pick: "shuffle", blurb: "빚더미 퍼리 — 턴 끝에 손에 있으면 HP 를 치르게 하는 「모자 속 쪽지」(청구서)를 떠넘기고 손상을 건다.",
    intents: [AC("st_note", 1, "draw", "청구서를 떠넘긴다"), DB("손상", 2, "빚 독촉"), B(80, "금화 투척")],
  },
  furring_jolly: {
    pick: "shuffle", blurb: "한턱 쏘는 퍼리 — 동료를 치유하고 적 전체 사기를 올린다. 먼저 끊어라.",
    intents: [H(90, "한턱 쏜다"), BFA("사기", 1, "오늘은 내가 쏜다"), A(60, "지갑으로 친다")],
  },
  gluttonbear_gloomy: {
    pick: "shuffle", blurb: "굶은 곰 — 턴마다 「굶주림」 이 올라 피해가 커지고(최대 4), 절반을 건너면 사기 2. 짧게 끝내라.",
    intents: [A(120, "굶은 앞발"), M(45, 3, "허겁지겁"), BL(80, "배를 움켜쥔다")],
    counters: [{ name: "굶주림", desc: "턴마다 배가 고파진다", onTurnStart: 1, dealt: 0.1, max: 4 }],
    passives: [P("눈이 뒤집힌다", "lowHp", BF("사기", 2, "눈이 뒤집힌다"), { at: 0.5 })],
  },
  furrywarriorcloserange_gloomy: {
    pick: "cycle", blurb: "장기 수련생 — 「버티기」 를 두르면 받는 피해가 줄고 턴이 끝나면 풀린다. 격파하면 취약 2.",
    intents: [CT("버티기", 3, "버티기 자세"), A(110, "수련 주먹"), BL(80, "막기 수련"), A(100, "돌려차기")],
    counters: [{ name: "버티기", desc: "오래 버틴 몸 — 턴이 끝나면 풀린다", taken: -0.1, max: 3, clearTurnEnd: true }],
    passives: [P("자세가 무너진다", "broken", BF("취약", 2, "자세가 무너진다"))],
  },
  foodscavenger_mad: {
    pick: "shuffle", blurb: "닥치는 대로 먹는 라쿤 — 동료가 쓰러지면 먹어 치워 회복 150 · 사기 1. 동료부터 치우면 이것이 커진다.",
    intents: [A(110, "물어뜯기"), H(80, "상한 걸 주워 먹는다"), M(40, 3, "할퀴기")],
    passives: [P("먹어 치운다", "allyDown", SHL(150, "먹어 치운다")), P("기운 난다", "allyDown", BF("사기", 1, "기운 난다"))],
  },
  furring_mad: {
    pick: "cycle", blurb: "투자 실패 퍼리 — 절반을 건너면 몸집을 키워(강인도가 다 찬다) 두꺼운 실드를 두른다. 절반 직전에 한꺼번에.",
    intents: [A(100, "주먹질"), B(90, "가방 던지기"), BL(70, "웅크린다")],
    phase: { at: 0.5, say: "「다 잃었어… 이제 몸으로 간다!」", intents: [BL(200, "거대화"), A(160, "거대 주먹"), M(50, 3, "분풀이")] },
  },
  furring_cool: {
    pick: "shuffle", blurb: "어둠의 루트 퍼리 — 뽑을 더미 1장에 몰래 「독」(내면 피해)을 바르고 손상을 건다.",
    intents: [B(100, "몰래 찌르기"), CD("독", 1, "draw", "몰래 손을 쓴다"), DB("손상", 2, "뒷거래")],
  },
  foodscavenger_cool: {
    pick: "shuffle", rare: [{ id: "anxietyHits", card: "st_jitters" }], blurb: "엘리트 · 미식가 라쿤 — 파티가 카드를 세 장 낼 때마다 「사료 한 줌」 을 손에 넣는다(턴당 2). 「안절부절」 을 더미에 넣고, 희귀종: 덱에 든 안절부절 수만큼 타격 +1 — 오래 끌지 말 것.",
    intents: [A(110, "미식 평가"), AC("st_jitters", 1, "draw", "까다로운 눈초리"), M(45, 2, "포크질"), BL(100, "냅킨을 두른다")],
    passives: [P("이건 못 먹어", "card", AC("st_kibble", 1, "hand", "이건 못 먹어"), { every: 3, limit: 2 })],
  },
  furring_mad_elite: {
    pick: "shuffle", rare: [{ id: "costUp" }], blurb: "엘리트 · 몰락한 큰손 — 턴마다 「재기 의지」 가 올라 피해가 커지고(최대 3), 절반에서 거대화한다. 오래 끌면 진다. 희귀종: 턴 시작에 손 2장 비용 +1.",
    intents: [A(130, "큰손의 주먹"), M(50, 3, "돈다발 난타"), BL(100, "금고를 끌어안는다")],
    phase: { at: 0.5, say: "「다시 일어설 거야!」", intents: [BL(220, "거대화"), A(180, "거대 큰손"), M(55, 3, "분풀이")] },
    counters: [{ name: "재기 의지", desc: "턴마다 이를 악문다", onTurnStart: 1, dealt: 0.15, max: 3 }],
  },
  clone_tig: {
    pick: "cycle", blurb: "2층 보스 · 티그(클론) — 백호 검성. 공격 카드를 네 장 낼 때마다 「장작 패기」 박자가 차 바로 연격, 같은 사도의 카드를 잇달아 내면 간파해 받아친다(턴당 1). 턴마다 「검기」 가 올라 피해가 커진다 — 사도를 번갈아, 스킬을 섞어서.",
    intents: [A(180, "백호 일섬"), M(55, 3, "쌍검 휘두르기"), BL(140, "검을 세운다", { tough: 1 }), CH("장작 패기 자세", AA(170, "장작 쪼개기!"), true)],
    phase: { at: 0.5, say: "「장작은 매일 패는 거다.」", intents: [M(60, 4, "쌍검 난무"), A(220, "백호 일섬"), BF("불굴", 1, "검성의 호흡"), CH("쌍검을 겹친다", AA(200, "백호 참!"))] },
    counters: [
      { name: "장작 패기", desc: "공격 카드를 보면 박자를 맞춘다", onCard: 1, cardType: "공격", max: 9, at: 4, act: M(40, 2, "장작 패기 — 연격"), mode: "now" },
      { name: "검기", desc: "턴마다 날이 선다", onTurnStart: 1, dealt: 0.05, max: 5 },
    ],
    passives: [P("간파", "card", A(90, "간파 — 맞받아친다"), { same: true }), P("검이 꺾인다", "broken", CT("검기", -5, "검기가 흩어진다"))],
  },
};
