// 모나티엄(엘프) — 결: 진압 · 기계 — 실드 · 수호(실드가 깨지면 행동 변경 · 아군에 실드 · 실드가 남으면 사기) · 카드 봉쇄/침체 · 드론 소환 · 충격
const { A, B, M, AA, BL, G, H, BF, BFA, DB, J, AC, CD, HC, SM, CT, SH, CH, RS, TH, SHL, RV, FG, P } = require("./lib");
module.exports = {
  // ── 1층 모나티엄 외곽 ──
  elfsoldiercloserange_cool: {
    pick: "cycle", blurb: "공구를 든 노동반 — 바리케이드(적 전체 실드)를 세우고, 제 실드가 깨지면 공구를 던지는 수로 바꾼다. 실드는 내 턴 끝까지 두고 다른 적부터.",
    intents: [G(60, "바리케이드를 세운다"), DB("손상", 2, "공구로 실드를 뜯는다"), A(90, "렌치 휘두르기")],
    passives: [P("바리케이드 붕괴", "guardBreak", SH(A(130, "공구를 던진다"), "바리케이드가 무너진다"))],
  },
  elfsoldierlongrange_cool: {
    pick: "shuffle", blurb: "경계조 명사수 — 뽑을 더미 1장을 「봉쇄」(내도 효과 없음)로 묶고, 큰 한 발은 카드로 당겨지지 않는다.",
    intents: [B(100, "조준 사격", { rush: 0 }), CD("봉쇄", 1, "draw", "조준 고정 — 손을 묶는다"), A(70, "개머리판")],
  },
  elfsoldierlongrange_mad: {
    pick: "shuffle", blurb: "호위 명사수 — 적 전체에 불굴을 두르고, 동료가 쓰러지면 엄호 사격을 한다.",
    intents: [BFA("불굴", 1, "엄호 대형"), B(90, "호위 사격"), A(60, "경고 사격")],
    passives: [P("엄호 사격", "allyDown", B(110, "엄호 사격"))],
  },
  nependers_cool: {
    pick: "cycle", blurb: "폐수를 머금은 한입초 — 「폐수 얼룩」 을 더미에 넣고 힘을 모아 꿀꺽 삼킨다. 모으는 동안 격파하면 끊긴다.",
    intents: [AC("st_wastewater", 1, "draw", "폐수를 뱉는다"), A(80, "덥석"), CH("입을 크게 벌린다", A(180, "꿀꺽"), true)],
  },
  drones_gloomy: {
    pick: "shuffle", rush: 3, blurb: "수리 대기 드론 — 잔고장 경보로 충격을 건다. 카드 세 장이면 움직이니 서둘러라.",
    intents: [DB("충격", 2, "잔고장 경보"), B(60, "레이저 점사"), A(50, "부딪힌다")],
  },
  droneg_gloomy: {
    pick: "cycle", blurb: "급수리한 방패 드론 — 실드를 두르고, 턴이 끝날 때 제 실드가 남아 있으면 사기가 오른다. 실드를 깨고 넘겨라. 격파하면 취약 2.",
    intents: [BL(80, "방패를 편다", { tough: 1 }), A(90, "몸통 돌진"), BL(60, "보조 방패")],
    passives: [P("정상 작동", "turnEnd", BF("사기", 1, "정상 작동", { if: { selfBlock: true } })), P("회로 정지", "broken", BF("취약", 2, "회로 정지"))],
  },
  drones_mad: {
    pick: "shuffle", blurb: "폐기 직전 드론 — 쓰러지면 과열 폭발로 파티에 충격 2. 「과열 경고」 도 끼워 넣는다.",
    intents: [B(60, "흔들리는 사격"), AC("st_overheat", 1, "draw", "과열 경고를 띄운다"), A(50, "부딪힌다")],
    passives: [P("과열 폭발", "death", DB("충격", 2, "과열 폭발"))],
  },
  nururingtanker_elf: {
    pick: "cycle", tough: 5, blurb: "주운 장비를 두른 젤리 — 디버프를 받으면 장비로 실드를 얻고, 실드가 깨지면 동료에게 실드를 나눈다. 디버프보다 피해를.",
    intents: [G(60, "고철 방벽", { tough: 1 }), A(90, "고철 박치기"), BL(100, "장비를 두른다")],
    passives: [P("주운 장비", "debuffed", BL(60, "주운 장비")), P("나눠 쓰기", "guardBreak", G(40, "남은 고철을 나눈다"))],
  },
  nururingwarrior_elf: {
    pick: "shuffle", blurb: "신경질적인 젤리 — 스킬 카드 두 장째마다 「신경질」 이 터져 바로 관통 공격. 격파하면 모으던 힘이 끊긴다.",
    intents: [A(80, "젤리 주먹"), CH("장비를 조인다", A(190, "고철 망치"), true), BL(40, "웅크린다")],
    counters: [{ name: "신경질", desc: "스킬 카드가 거슬린다", onCard: 1, cardType: "스킬", max: 9, at: 2, act: B(60, "신경질 찌르기"), mode: "now" }],
  },
  nururingarcher_elf: {
    pick: "shuffle", rush: 3, blurb: "기름 연기 젤리 — 고통과 손상을 퍼뜨린다. 카드 세 장이면 움직인다.",
    intents: [DB("고통", 2, "기름 연기"), DB("손상", 1, "끈적한 기름"), B(60, "기름 방울")],
  },
  nururingsupporter_elf: {
    pick: "shuffle", blurb: "응원하는 젤리 — 치유 · 사기를 돌리고, 사이 나쁜 동료가 쓰러지면 오히려 신난다.",
    intents: [H(80, "응급 수리"), BFA("사기", 1, "힘내라 젤리!"), A(50, "톡 친다")],
    passives: [P("고소하다", "allyDown", BF("사기", 1, "고소하다"))],
  },
  droneg_mad: {
    pick: "shuffle", rare: [{ id: "toughGuard" }], blurb: "엘리트 · 자가 수리 드론 — 턴 끝마다 스스로 고친다(60). 격파하면 회로 정지(취약 2) — 느린 지속 피해보다 몰아 치기. 희귀종: 받는 강인도 피해 -20%.",
    intents: [A(120, "수리 망치"), BL(100, "장갑판을 붙인다"), M(45, 3, "볼트 난사"), DB("충격", 2, "누전")],
    passives: [P("자가 수리", "turnEnd", SHL(60, "자가 수리")), P("회로 정지", "broken", BF("취약", 2, "회로 정지"))],
  },
  elfsoldiercloserange_mad: {
    pick: "shuffle", rare: [{ id: "actDebuff", st: "약화" }], blurb: "엘리트 · 현장 반장 — 턴이 끝날 때 파티에 실드가 남아 있으면 바리케이드가 두꺼워진다(적 전체 결의). 실드에만 기대지 말 것. 희귀종: 행동할 때 파티에 약화 2.",
    intents: [A(120, "현장 지휘봉"), G(70, "바리케이드 증설", { tough: 1 }), M(50, 3, "작업 지시"), A(140, "공사 망치")],
    passives: [P("공정 검사", "turnEnd", BFA("결의", 1, "바리케이드를 두껍게", { if: { partyBlock: true } }))],
  },
  drones_cool: {
    pick: "shuffle", tied: true, blurb: "순찰 드론 S형 — 방전 사격으로 충격을 걸고, 체력이 절반을 건너면 비상 경보(충격 2)를 울린다. 엘레나가 세운 드론은 엘레나가 쓰러지면 같이 멈춘다.",
    intents: [B(70, "방전 사격", { id: "충격", n: 1 }), A(60, "부딪힌다"), DB("충격", 1, "방전")],
    passives: [P("비상 경보", "lowHp", DB("충격", 2, "비상 경보"), { at: 0.5 })],
  },
  clone_canna: {
    pick: "cycle", blurb: "1층 보스 · 칸나(클론) — 진압반장. 파티가 카드를 낼 때마다 「결재 서류」 가 쌓여 여섯이면 바로 명포수 사격. 턴마다 「양자폭탄」 이 차 넷이면 다음 차례에 전체 폭격 — 드론이 쓰러지면 한 칸 더 찬다. 격파하면 폭탄이 두 칸 깎인다(휴가 신청서 반려).",
    intents: [B(110, "명포수 저격"), SM("drones_gloomy", 1, 2, "드론 호출"), DB("충격", 2, "진압탄"), G(50, "진압 방패")],
    phase: { at: 0.5, say: "「휴가는 반려다. 끝까지 간다.」", intents: [M(45, 4, "연사"), SM("drones_gloomy", 1, 2, "드론 증원"), B(150, "관통 저격"), BF("불굴", 1, "버티기 명령")] },
    counters: [
      { name: "결재 서류", desc: "파티가 카드를 낼 때마다 서류가 쌓인다", onCard: 1, max: 9, at: 6, act: B(70, "명포수 — 결재 사격"), mode: "now" },
      { name: "양자폭탄", desc: "턴마다 차오르는 결재 대기 폭탄", onTurnStart: 1, max: 4, at: 4, act: AA(120, "양자폭탄 결재!", { id: "충격", n: 1 }), mode: "next" },
    ],
    passives: [P("부하 손실 보고", "allyDown", CT("양자폭탄", 1, "부하 손실 — 결재가 빨라진다"), { limit: 0 }), P("휴가 신청서 반려", "broken", CT("양자폭탄", -2, "결재가 밀린다"))],
  },
  // ── 2층 모나티엄 도심 ──
  elfsoldiercloserange_naive: {
    pick: "shuffle", blurb: "돌격병 — 적 전체 결의를 올리고 전우가 쓰러지면 성난다.",
    intents: [BFA("결의", 1, "방패 대열"), A(90, "돌격"), BL(70, "방패를 세운다")],
    passives: [P("전우의 원수", "allyDown", BF("사기", 1, "전우의 원수"))],
  },
  elfsoldiercloserange_jolly: {
    pick: "shuffle", blurb: "도심 꿀보직 관리직 — 큰 수는 당겨지지 않는다. 제 차례가 끝났는데 카드를 계속 내면 「잔업」 이 쌓여 셋이면 바로 짜증을 낸다.",
    intents: [A(130, "결재판 내려치기", { rush: 0 }), BL(60, "커피 한 잔"), A(80, "서류 뭉치")],
    counters: [{ name: "잔업", desc: "퇴근했는데 일을 시킨다", onCard: 1, afterAct: true, max: 9, at: 3, act: A(110, "퇴근 좀 하자!"), mode: "now" }],
  },
  elfsoldiercloserange_gloomy: {
    pick: "cycle", blurb: "의장대 — 디버프를 받거나 격파에서 일어서면 사기가 오른다. 모으는 한 방은 격파로 끊긴다.",
    intents: [A(100, "의장 창"), CH("창을 곧추세운다", A(200, "의장 돌격"), true), BL(70, "대열 정비")],
    passives: [P("자존심", "debuffed", BF("사기", 1, "자존심이 상했다")), P("다시 대열로", "recover", BF("사기", 1, "다시 대열로"))],
  },
  elfsoldierlongrange_naive: {
    pick: "shuffle", blurb: "명사수 — 뒷줄을 겨누고 뽑을 더미 1장을 「침체」(비용 +1)로 만든다.",
    intents: [B(90, "저격"), CD("침체", 1, "draw", "견제 사격"), B(70, "속사")],
  },
  elfsoldierlongrange_jolly: {
    pick: "shuffle", rush: 3, blurb: "순찰대 호루라기 — 카드로 당기면 「통제 신호」 를 손에 넣는다. 서둘러 끊거나 당기지 말 것.",
    intents: [AC("st_signal", 1, "draw", "호루라기"), B(60, "순찰 사격"), A(55, "경봉")],
    passives: [P("호각 연타", "rushed", AC("st_signal", 1, "hand", "호각 연타"))],
  },
  elfsoldierlongrange_gloomy: {
    pick: "cycle", blurb: "의장대 명사수 — 한 치 어긋남 없이 힘을 모아 관통 한 발. 모으는 동안 격파하면 끊긴다.",
    intents: [B(80, "의장 사격"), CH("숨을 고른다", B(190, "한 치 어긋남 없는 한 발"), true), BL(50, "엄폐")],
  },
  droneg_naive: {
    pick: "shuffle", blurb: "방패 드론 G형 — 적 전체 실드를 두르고 「통제 신호」 를 넣는다. 동료가 쓰러지면 방어벽을 세운다.",
    intents: [G(50, "방어 프로토콜", { tough: 1 }), A(90, "돌진"), AC("st_signal", 1, "draw", "통제 신호")],
    passives: [P("방어벽", "allyDown", G(60, "방어벽 전개"))],
  },
  drones_jolly: {
    pick: "shuffle", rush: 3, blurb: "낙서 드론 — 충격을 걸고, 카드로 당겨지면 신이 나 사기가 오른다.",
    intents: [DB("충격", 1, "낙서 스프레이"), B(70, "레이저"), A(60, "부딪힌다")],
    passives: [P("신났다", "rushed", BF("사기", 1, "신났다!"))],
  },
  droneg_jolly: {
    pick: "cycle", tough: 5, blurb: "낙서 방패 드론 — 두꺼운 실드를 두르고, 실드가 깨지면 수를 돌진으로 바꾼다. 실드를 깬 턴에 마무리하라. 격파하면 취약 2.",
    intents: [BL(110, "낙서 방패", { tough: 1 }), A(90, "부딪힌다"), BL(80, "보조 방패")],
    passives: [P("방패가 깨지면", "guardBreak", SH(A(150, "분노의 돌진"), "방패가 깨졌다!")), P("회로 정지", "broken", BF("취약", 2, "회로 정지"))],
  },
  droneg_cool: {
    pick: "shuffle", rare: [{ id: "crystal" }], blurb: "엘리트 · 시설 경비 드론 — 턴마다 「경보 단계」 가 올라 셋이면 다음 차례에 전체 경보(충격 1). 격파로는 단계가 안 내려간다. 희귀종: 전투 시작 결정화 3.",
    intents: [A(120, "경비봉"), BL(110, "경비 장갑"), M(45, 3, "테이저 연사", { id: "충격", per: 1 }), B(120, "탐조 레이저")],
    counters: [{ name: "경보 단계", desc: "턴마다 올라가는 경보", onTurnStart: 1, max: 3, at: 3, act: AA(80, "전체 경보", { id: "충격", n: 1 }), mode: "next" }],
  },
  elfsoldiercloserange_honor: {
    pick: "shuffle", rare: [{ id: "actDebuff", st: "취약" }], blurb: "엘리트 · 의장대장 — 디버프를 받으면 면역 1(턴당 1), 격파에서 일어서면 사기. 실드가 깨지면 동료에게 실드를 나눈다. 희귀종: 행동할 때 파티에 취약 2.",
    intents: [A(130, "의장 지휘도"), BL(100, "대장 방패", { tough: 1 }), M(50, 3, "연속 찌르기"), BFA("불굴", 1, "대열 유지")],
    passives: [P("의장대의 긍지", "debuffed", BF("면역", 1, "의장대의 긍지")), P("다시 선다", "recover", BF("사기", 1, "다시 선다")), P("대장의 방패", "guardBreak", G(50, "방패를 나눈다"))],
  },
  clone_elena: {
    pick: "cycle", blurb: "2층 보스 · 엘레나(클론) — 괴짜 시장. 「발명 의욕」 이 넷이 될 때마다 드론을 세우고(최대 2), 드론 S형이 쓰러지면 자폭 기능이 작동해 파티를 친다. 드론을 남겨 두면 실드가, 부수면 자폭이 온다 — 엘레나를 먼저 쓰러뜨리면 세운 드론도 멈춘다.",
    intents: [G(50, "발명품 방패"), B(140, "시장 레이저"), DB("충격", 2, "전기 공사"), CH("자폭 스위치를 만진다", AA(140, "쓸데없는 자폭 기능!"), true)],
    phase: { at: 0.5, say: "「이번 발명품은 진짜 쓸모 있다니까!」", intents: [SM("drones_cool", 1, 2, "발명품 대방출"), M(55, 4, "레이저 난사", { id: "충격", per: 1 }), B(180, "고출력 레이저"), G(60, "드론 방벽")] },
    counters: [{ name: "발명 의욕", desc: "턴마다 새 발명이 떠오른다", onTurnStart: 1, max: 4, at: 4, act: SM("drones_cool", 1, 2, "번뜩이는 발명!"), mode: "now" }],
    passives: [P("자폭 기능", "allyDown", AA(40, "자폭 기능 작동!"), { who: "drones_cool", limit: 0 }), P("드론 방벽", "turnStart", G(40, "드론 방벽", { if: { allies: 2 } })), P("발명 실패", "broken", BF("취약", 2, "발명 실패"))],
  },
};
