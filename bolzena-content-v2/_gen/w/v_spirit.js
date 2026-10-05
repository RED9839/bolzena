// 정령산 — 결: 원소 — 떠다니는 몸(맞으면 줄어 0 이면 기절) · 단단함이 쌓이면 강한 행동 · 빙결 · 소환 · 원소 디버프(고통 · 충격 · 균열)
const { A, B, M, AA, BL, G, H, BF, BFA, DB, J, AC, CD, HC, SM, CT, SH, CH, RS, TH, SHL, RV, FG, P } = require("./lib");
const float = (n) => ({ name: "부유", desc: "떠다니는 몸 — 맞을 때마다 내려앉고 0 이면 기절", start: n, max: n, onHit: -1, stunAtZero: true, resetTurnStart: true });
module.exports = {
  // ── 1층 정령산 기슭 ──
  wisps_naive: {
    pick: "shuffle", blurb: "불의 위스프 — 「부유」 둘: 한 턴에 두 번 맞으면 떨어져 기절한다. 스며드는 불로 고통 2.",
    intents: [DB("고통", 2, "스며드는 불"), B(70, "불똥"), A(60, "화르륵")],
    counters: [float(2)],
  },
  wisps_mad: {
    pick: "shuffle", blurb: "번개 위스프 — 파티가 카드를 낼 때마다 「정전기」 가 모여 여섯이면 바로 전체 방전(충격). 충격은 실드 위로 맞으면 더 아프다.",
    intents: [DB("충격", 2, "찌릿"), B(70, "번개 화살"), A(50, "튀는 불꽃")],
    counters: [{ name: "정전기", desc: "카드가 스칠 때마다 모인다", onCard: 1, max: 9, at: 6, act: AA(60, "방전", { id: "충격", n: 1 }), mode: "now" }],
  },
  wisps_jolly: {
    pick: "shuffle", blurb: "바람 위스프 — 「부유」 셋. 적 전체 사기를 올리고, 뽑히면 손패를 채어 가는 「돌풍」 을 넣는다.",
    intents: [BFA("사기", 1, "순풍"), AC("st_gust", 1, "draw", "돌풍"), B(60, "바람 칼날")],
    counters: [float(3)],
  },
  wisps_gloomy: {
    pick: "shuffle", blurb: "물의 위스프 — 맑은 물로 동료를 치유하고 피해 감소를 두른다. 「부유」 둘.",
    intents: [H(70, "맑은 물"), BFA("피해 감소", 1, "물의 장막"), A(50, "물방울")],
    counters: [float(2)],
  },
  lupalu_naive: {
    pick: "cycle", blurb: "물가의 큰 정령 — 맞으면 물을 머금어 실드를 얻는다(턴당 2). 적 전체 실드도 두른다.",
    intents: [G(50, "물 장막", { tough: 1 }), A(90, "물살 박치기"), BL(80, "물을 머금는다")],
    passives: [P("물을 머금는다", "hurt", BL(30, "물을 머금는다"), { limit: 2 })],
  },
  lupalu_gloomy: {
    pick: "shuffle", blurb: "깨진 병을 든 정령 — 연타마다 균열을 새긴다.",
    intents: [M(40, 2, "깨진 병 휘두르기", { id: "균열", per: 1 }), B(80, "유리 조각"), A(70, "병 내려치기")],
  },
  lupalu_jolly: {
    pick: "shuffle", blurb: "청정수만 마시는 정령 — 디버프를 받으면 씻어 내며 회복한다(턴당 1). 동료도 치유한다.",
    intents: [H(70, "청정수"), B(90, "물대포"), A(70, "물장구")],
    passives: [P("씻어 낸다", "debuffed", SHL(50, "씻어 낸다"))],
  },
  lupalu_cool: {
    pick: "shuffle", blurb: "산정의 정령 — 뽑을 더미 1장을 「빙결」(뽑은 턴에 못 냄)시키고, 카드로 당기면 다음 턴 AP -1.",
    intents: [CD("빙결", 1, "draw", "얼어붙은 손"), A(100, "얼음 박치기"), BL(60, "서리를 두른다")],
    passives: [P("다시 움직인다", "rushed", J(1, "추워서 다시 움직인다"))],
  },
  oldtree_naive: {
    pick: "shuffle", blurb: "침입자를 노리는 고목 — 첫 턴에 뒷줄을 기습한다. 「나무껍질」 을 두르면 받는 피해가 준다(턴 끝에 풀림).",
    open: B(100, "기습 가지"),
    intents: [CT("나무껍질", 2, "껍질을 굳힌다"), A(90, "가지 휘두르기"), BL(70, "뿌리를 내린다")],
    counters: [{ name: "나무껍질", desc: "굳힌 껍질 — 턴이 끝나면 풀린다", taken: -0.1, max: 3, clearTurnEnd: true }],
  },
  oldtree_cool: {
    pick: "cycle", blurb: "깊이 생각하는 고목 — 턴마다 「나이테」 가 늘어 받는 피해가 줄고, 셋이 되면 깊은 생각 끝에 크게 내려친다. 빨리 쓰러뜨려라.",
    intents: [A(90, "가지"), BL(80, "생각에 잠긴다"), A(210, "깊은 생각 끝의 일격", { if: { counter: "나이테", n: 3 } }), A(90, "뿌리 걸기")],
    counters: [{ name: "나이테", desc: "턴마다 한 겹씩 단단해진다", onTurnStart: 1, taken: -0.08, max: 4 }],
  },
  pumpkin_cool: {
    pick: "shuffle", blurb: "고랭지 호박 — 적 전체 결의를 올리고, 턴이 끝날 때 제 실드가 남아 있으면 사기. 실드를 깨고 넘겨라.",
    intents: [BFA("결의", 1, "단단한 고랭지"), BL(80, "두꺼운 껍질"), A(90, "데굴데굴")],
    passives: [P("서리 맞은 껍질", "turnEnd", BF("사기", 1, "서리 맞은 껍질", { if: { selfBlock: true } }))],
  },
  nependers_gloomy: {
    pick: "cycle", tough: 5, blurb: "외피가 단단한 산지 한입초 — 「단단함」 이 쌓여 셋 이상이면 통째로 삼킨다. 모으는 수는 격파로 끊긴다.",
    intents: [CT("단단함", 1, "외피를 굳힌다"), A(90, "덥석"), CH("입을 크게 벌린다", A(180, "꿀꺽"), true), A(200, "통째로 삼킨다", { if: { counter: "단단함", n: 3 } })],
    counters: [{ name: "단단함", desc: "굳어 가는 외피", taken: -0.05, max: 5 }],
  },
  nururingtanker_spirit: {
    pick: "cycle", tough: 5, blurb: "진중한 젤리 방패 — 피해 감소 2 를 두르고 버틴다. 실드가 깨지면 동료를 감싼다. 격파하면 취약 1.",
    intents: [BF("피해 감소", 2, "진중하게"), A(90, "젤리 박치기"), G(60, "젤리 방벽", { tough: 1 }), A(110, "깔고 앉는다")],
    passives: [P("감싸기", "guardBreak", G(40, "동료를 감싼다")), P("물러진다", "broken", BF("취약", 1, "물러진다"))],
  },
  nururingwarrior_spirit: {
    pick: "cycle", blurb: "진중하게 힘을 모으는 젤리 — 모으는 한 방은 격파로 끊긴다.",
    intents: [A(80, "젤리 주먹"), CH("진중하게 힘을 모은다", A(200, "정령 펀치"), true), BL(50, "가부좌")],
  },
  nururingarcher_spirit: {
    pick: "cycle", blurb: "원소 젤리 — 불(고통) · 번개(충격) · 서리(빙결)를 번갈아 쏜다.",
    intents: [B(80, "불 화살", { id: "고통", n: 2 }), B(80, "번개 화살", { id: "충격", n: 1 }), CD("빙결", 1, "draw", "서리 화살")],
  },
  nururingsupporter_spirit: {
    pick: "shuffle", blurb: "응원하는 젤리 — 치유하고 적 전체 불굴을 두른다.",
    intents: [H(80, "젤리 붕대"), BFA("불굴", 1, "정령의 가호"), A(50, "톡 친다")],
  },
  oldtree_mad: {
    pick: "shuffle", rare: [{ id: "costUp" }], blurb: "엘리트 · 열매 직전 고목 — 디버프를 받으면 사기(턴당 1). 디버프를 쌓기보다 바로 때려라. 희귀종: 턴 시작에 손 2장 비용 +1.",
    intents: [A(110, "열매 가지"), M(40, 3, "가지 난타"), BL(100, "껍질을 두른다"), A(130, "뿌리째 휘두른다")],
    passives: [P("열매가 영근다", "debuffed", BF("사기", 1, "열매가 영근다"))],
  },
  wisps_cool: {
    pick: "cycle", rare: [{ id: "toughGuard" }], blurb: "엘리트 · 원소 핵 — 불의 위스프를 불러내고(최대 1), 다른 위스프가 쓰러지면 그 원소를 삼켜 회복 · 사기. 핵부터 끄거나 한꺼번에 꺼라. 희귀종: 받는 강인도 피해 -20%.",
    intents: [SM("wisps_naive", 1, 1, "원소 소환"), B(100, "원소 광선"), DB("고통", 2, "원소 폭주"), M(35, 3, "원소 탄")],
    passives: [P("원소를 삼킨다", "allyDown", SHL(80, "원소를 삼킨다")), P("원소 공명", "allyDown", BF("사기", 1, "원소 공명"))],
  },
  pumpkin_cool_elite: {
    pick: "cycle", rare: [{ id: "crystal" }], blurb: "엘리트 · 만년설 호박 — 첫 턴 두꺼운 껍질에 「실드 보존」 을 걸어 넘긴다. 뽑을 더미 2장을 빙결시킨다. 껍질이 깨지면 굴러 덮친다. 희귀종: 전투 시작 결정화 3.",
    open: BL(250, "만년설 껍질"),
    intents: [A(120, "얼음 박치기"), CD("빙결", 2, "draw", "냉기"), BL(120, "눈을 덧씌운다"), M(45, 3, "고드름")],
    passives: [P("만년설", "fightStart", BF("실드 보존", 2, "만년설")), P("껍질이 깨지면", "guardBreak", SH(AA(110, "눈사태"), "껍질이 깨졌다"))],
  },
  clone_ifrit: {
    pick: "cycle", blurb: "1층 보스 · 이프리트(클론) — 장작을 좋아하는 불의 정령. 턴마다 「불길」 이 올라 주는 피해가 커지고, 위스프가 쓰러지면 장작으로 삼켜 불길이 둘 더. 뽑을 더미 2장을 장작으로 「봉쇄」 한다. 격파하면 불길이 꺼진다.",
    intents: [DB("고통", 3, "불길이 번진다"), CD("봉쇄", 2, "draw", "장작으로 삼는다"), A(140, "불 주먹"), CH("화력을 모은다", AA(130, "화력 발전!", { id: "고통", n: 3 }), true)],
    phase: { at: 0.5, say: "「더! 더 땔감을!」", intents: [SM("wisps_naive", 2, 2, "불씨를 깨운다"), M(50, 4, "불꽃 연타", { id: "고통", per: 1 }), A(170, "새까만 주먹"), CH("숯을 삼킨다", AA(160, "화력 발전!!"))] },
    counters: [{ name: "불길", desc: "턴마다 타오른다", onTurnStart: 1, dealt: 0.1, max: 5 }],
    passives: [P("장작 넣기", "allyDown", CT("불길", 2, "장작이 된다"), { limit: 0 }), P("불이 꺼진다", "broken", CT("불길", -5, "불길이 사그라든다"))],
  },
  // ── 2층 정령산 꼭대기 ──
  clone_sylla: {
    pick: "cycle", blurb: "2층 보스 · 실라(클론) — 정면 승부의 맏언니. 턴마다 「질서」 가 쌓여 받는 피해가 줄고(최대 4), 격파하면 질서가 무너지며 취약 2. 정면승부엔 강하지만 기습엔 약하다 — 카드로 당겨 미리 움직이게 하면 취약 1.",
    intents: [A(160, "정면 돌파"), M(50, 3, "바람 검"), BFA("불굴", 1, "질서 유지"), DB("약화", 1, "압도"), CH("바람을 모은다", AA(160, "정면 승부!"), true)],
    phase: { at: 0.5, say: "「정면으로 와라. 피하지 않는다.」", intents: [A(200, "정면 돌파"), M(55, 4, "폭풍 검"), BF("피해 감소", 2, "바람의 갑옷"), CH("폭풍을 모은다", AA(190, "폭풍 정면 승부!"))] },
    counters: [{ name: "질서", desc: "턴마다 흐트러짐이 없어진다", onTurnStart: 1, taken: -0.08, max: 4 }],
    passives: [P("질서 붕괴", "broken", CT("질서", -4, "질서가 무너진다")), P("무너진 자세", "broken", BF("취약", 2, "자세가 무너진다")), P("기습에 약하다", "rushed", BF("취약", 1, "기습이라니…!"))],
  },
};
