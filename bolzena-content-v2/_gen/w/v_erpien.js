// 에르피엔(요정) — 결: 단맛 · 카드 종류 반응(착란 · 변덕) · 허기형 몸 · 설탕 상태 카드
const { A, B, M, AA, BL, G, H, BF, BFA, DB, J, AC, CD, HC, SM, CT, SH, CH, RS, TH, SHL, RV, FG, P } = require("./lib");
module.exports = {
  // ── 1층 세계수 숲 ──
  fairymoblongrange_jolly: {
    pick: "shuffle", blurb: "당이 오른 요정 — 공격 카드를 보면 신이 나 손 1장을 깎아 주고, 다른 카드를 보면 심술로 1장을 올린다(착란). 공격으로 몰면 덕을 본다.",
    intents: [B(60, "사탕을 던진다"), A(50, "콕 찌른다"), AC("st_sugar", 1, "draw", "설탕을 흩뿌린다")],
    passives: [P("깔깔 웃음", "card", HC(-1, 1, "깔깔 — 손 1장 비용 -1"), { type: "공격" }), P("뾰로통", "card", HC(1, 1, "뾰로통 — 손 1장 비용 +1"), { type: "!공격" })],
  },
  fairymoblongrange_mad: {
    pick: "cycle", blurb: "매운 채소를 쥔 요정 — 「매운맛」 이 턴마다 오르고 셋이면 다음 차례에 전체를 맵게 한다. 그 전에 끊어라.",
    intents: [B(80, "매운 고추를 던진다", { id: "고통", n: 2 }), A(60, "할퀸다"), B(70, "뒷줄을 노린다")],
    counters: [{ name: "매운맛", desc: "턴마다 맵기가 오른다", onTurnStart: 1, max: 3, at: 3, act: AA(70, "매운맛 폭발", { id: "고통", n: 2 }), mode: "next" }],
  },
  magicfork_naive: {
    pick: "shuffle", blurb: "주인 말을 안 듣는 쇠스랑 — 공격이 아닌 카드를 낼 때마다 「심통」 이 붙고 넷이면 바로 내리찍는다. 스킬을 몰아 내기 전에 쓰러뜨려라.",
    intents: [A(90, "밭을 갈듯 내리찍는다", { w: 2 }), M(40, 2, "녹슨 날로 두 번 긁는다"), BL(60, "자루를 곧추세운다")],
    counters: [{ name: "심통", desc: "공격이 아닌 카드에 심통이 난다", dealt: 0.1, onCard: 1, cardType: "!공격", max: 9, at: 4, act: A(150, "심통 폭발 — 내리찍기"), mode: "now" }],
  },
  magicfork_gloomy: {
    pick: "cycle", blurb: "수리받지 못한 농기구 — 「녹 껍질」 을 둘러 받는 피해를 줄이지만 턴이 끝나면 벗겨진다. 껍질을 두른 턴엔 다른 적을, 격파하면 무른다.",
    intents: [CT("녹 껍질", 3, "녹이 엉겨 붙는다"), A(100, "녹슨 날"), BL(70, "몸을 웅크린다"), A(80, "삐걱 휘두른다")],
    counters: [{ name: "녹 껍질", desc: "엉겨 붙은 녹 — 턴이 끝나면 떨어진다", taken: -0.1, max: 3, clearTurnEnd: true }],
    passives: [P("부러진 자루", "broken", BF("취약", 2, "자루가 부러진다"))],
  },
  magicfork_jolly: {
    pick: "shuffle", blurb: "자만한 농기구 — 같은 사도의 카드를 잇달아 내면 얕보였다며 바로 받아친다(턴당 1). 사도를 번갈아 내라.",
    intents: [A(80, "으스대며 찌른다"), BL(50, "폼을 잡는다"), A(100, "자만의 일격")],
    passives: [P("얕보지 마!", "card", A(110, "얕보지 마!"), { same: true })],
  },
  ginseng_gloomy: {
    pick: "shuffle", blurb: "땅속에 숨는 산삼 — 쓰러지면 땅속으로 숨은 척한다(가사). 다른 적이 회복을 쓰면 그 값으로 다시 일어선다 — 치유사부터.",
    intents: [H(70, "뿌리즙을 나눈다"), B(70, "쓴 즙을 뿌린다"), G(40, "뿌리를 얽는다")],
    passives: [P("땅속으로", "death", FG("땅속으로 쏙 숨는다"))],
  },
  lupalu_mad: {
    pick: "shuffle", blurb: "당분 중독 정령 — 「당분」 하나당 주는 피해가 오르고 맞을 때마다 하나씩 빠진다(턴 시작에 다시 둘). 잘게 여러 번 쳐서 당을 빼라.",
    intents: [DB("손상", 2, "끈적한 사탕물"), A(70, "사탕 박치기"), M(35, 2, "당 충전 연타")],
    counters: [{ name: "당분", desc: "요정에게 받은 사탕 기운", start: 2, max: 2, dealt: 0.2, onHit: -1, resetTurnStart: true }],
  },
  nururingtanker_fairy: {
    pick: "cycle", tough: 5, blurb: "말랑한 젤리 방패 — 「말랑 젤리」 가 있는 동안 받는 피해가 1. 맞을 때마다 하나씩 녹는다 — 여러 번 치는 카드로 벗기고 크게 쳐라.",
    intents: [G(60, "젤리로 감싼다", { tough: 1 }), A(90, "통통 부딪힌다"), CT("말랑 젤리", 2, "젤리를 다시 굳힌다"), A(120, "깔고 앉는다")],
    counters: [{ name: "말랑 젤리", desc: "말랑해서 아프지 않다", start: 2, max: 3, flat: 1, onHit: -1 }],
  },
  nururingwarrior_fairy: {
    pick: "cycle", blurb: "팔을 걷어붙인 젤리 — 힘을 모은 다음 턴 크게 친다. 모으는 동안 격파하면 끊긴다.",
    intents: [A(80, "젤리 주먹"), CH("팔을 걷어붙인다", A(200, "젤리 어퍼컷"), true), BL(40, "몸을 굳힌다")],
  },
  nururingarcher_fairy: {
    pick: "shuffle", blurb: "마력 빵을 던지는 젤리 — 고통과 설탕 범벅을 뿌린다. 오래 둘수록 손이 끈적해진다.",
    intents: [DB("고통", 2, "마력 빵 부스러기"), B(70, "딱딱한 빵"), AC("st_sugar", 1, "draw", "설탕을 끼얹는다")],
  },
  nururingsupporter_fairy: {
    pick: "shuffle", blurb: "응원하는 젤리 — 동료를 치유하고 적 전체 사기를 올린다. 먼저 끊어라.",
    intents: [H(80, "젤리 붕대"), BFA("사기", 1, "힘내라 젤리!"), A(50, "톡 친다")],
  },
  magicfork_mad: {
    pick: "shuffle", rare: [{ id: "costUp" }], blurb: "엘리트 · 폭주한 쇠스랑 — 맞을 때마다 「폭주」 가 쌓여 다섯이면 바로 세 번 찍는다. 잘게 여러 번보다 굵게 한 번. 희귀종: 턴 시작에 손 2장 비용 +1.",
    intents: [A(110, "폭주 내려찍기"), M(45, 3, "쉬지 않고 찍는다"), BL(80, "자루를 비튼다"), A(130, "땅을 뒤엎는다")],
    counters: [{ name: "폭주", desc: "맞을수록 날뛴다", dealt: 0.05, onHit: 1, max: 9, at: 5, act: M(50, 3, "폭주 연타"), mode: "now" }],
  },
  ginseng_mad: {
    pick: "shuffle", rare: [{ id: "poisonHand" }], blurb: "엘리트 · 웃자란 산삼 — 턴마다 회복하고 디버프를 받으면 양분으로 회복한다. 쓰러질 때 쓴 즙(취약 2)을 터뜨린다. 희귀종: 턴 시작에 손 2장에 독.",
    intents: [H(100, "양분을 뿜는다"), G(70, "뿌리를 얽는다", { tough: 1 }), B(100, "쓴 즙을 뿌린다"), DB("고통", 2, "쓴맛이 번진다")],
    passives: [P("넘치는 양분", "turnStart", H(40, "넘치는 양분")), P("양분으로 바꾼다", "debuffed", SHL(50, "양분으로 바꾼다")), P("쓴 즙 폭발", "death", DB("취약", 2, "쓴 즙이 터진다"))],
  },
  ginseng_cool: {
    pick: "cycle", rare: [{ id: "toughGuard" }], blurb: "엘리트 · 거대 사탕수수 지기 — 첫 턴 두꺼운 껍질을 두르고 「가호」(실드 보존)로 넘긴다. 껍질이 깨지면 화가 나 수를 전체 공격으로 바꾼다. 희귀종: 받는 강인도 피해 -20%.",
    open: BL(220, "사탕수수 껍질을 두른다"),
    intents: [A(120, "사탕수수 휘두르기"), BL(120, "껍질을 덧댄다"), M(50, 3, "단물 채찍"), A(140, "뿌리째 내려친다")],
    passives: [P("가호", "fightStart", BF("실드 보존", 2, "껍질이 단단히 붙는다")), P("껍질이 깨지면", "guardBreak", SH(AA(100, "단물 해일"), "껍질이 깨져 화가 난다"))],
  },
  clone_carrot: {
    pick: "cycle", blurb: "1층 보스 · 캬롯(클론) — 일등 정원사. 턴마다 「텃밭」 이 자라 셋이면 다음 차례에 수확(전체 공격)한다. 뽑힐 때 약화를 거는 「약초 가루」 를 더미에 심고, 공격 카드 세 장째마다 비료로 정원을 고친다. 수액 펌프는 격파로 끊긴다.",
    intents: [B(120, "사탕수수 회초리"), AC("st_herb", 1, "draw", "잡초 씨앗을 뿌린다"), H(140, "특제 영양제"), CH("수액 펌프를 채운다", AA(130, "수액 펌프 발사!", { id: "약화", n: 1 }), true), BFA("사기", 1, "「무럭무럭 자라렴」")],
    phase: { at: 0.5, say: "「내 텃밭을 망치다니…!」", intents: [M(40, 4, "덩굴 채찍"), AC("st_herb", 2, "draw", "잡초를 마구 심는다"), H(180, "영양제 과다 살포"), CH("펌프를 최대로", AA(160, "수액 펌프 발사!!"))] },
    counters: [{ name: "텃밭", desc: "턴마다 자라는 사탕수수 밭", onTurnStart: 1, max: 3, at: 3, act: AA(100, "수확이다!"), mode: "next" }],
    passives: [P("마법 성장 비료", "card", H(40, "마법 성장 비료"), { type: "공격", every: 3, limit: 0 }), P("뽑힌 사탕수수", "allyDown", CT("텃밭", 1, "밭을 다시 일군다")), P("호미가 부러졌다", "broken", CT("텃밭", -3, "텃밭이 엉망이 된다"))],
  },
  // ── 2층 요정 왕도 ──
  fairymobcloserange_naive: {
    pick: "shuffle", blurb: "당이 떨어진 요정 — 「허기」 하나당 피해가 오르고 맞으면 하나씩 빠진다(턴 시작에 셋). 뽑히면 AP 를 잃는 「어지럼」 을 옮긴다.",
    intents: [A(90, "배고파서 문다"), AC("st_dizzy", 1, "draw", "어지럼을 옮긴다"), A(110, "허겁지겁 달려든다")],
    counters: [{ name: "허기", desc: "당이 떨어져 사납다", start: 3, max: 3, dealt: 0.2, onHit: -1, resetTurnStart: true }],
  },
  fairymobcloserange_gloomy: {
    pick: "shuffle", blurb: "공허한 요정 — 자기가 행동한 뒤에도 카드를 내면 「공허」 가 쌓여 넷이면 약화를 실어 친다. 차례가 지나면 손을 아껴라.",
    intents: [DB("약화", 1, "멍하니 바라본다"), A(100, "허공을 휘젓는다"), BL(60, "몸을 웅크린다")],
    counters: [{ name: "공허", desc: "행동한 뒤 카드를 낼 때마다", onCard: 1, afterAct: true, max: 9, at: 4, act: A(120, "공허한 손짓", { id: "약화", n: 2 }), mode: "now" }],
  },
  buseuleogi_mad: {
    pick: "shuffle", blurb: "흩날리는 과자 부스러기 — 쓰러지면 「설탕 범벅」 을 뽑을 더미에 남긴다. 동료가 쓰러지면 성난다.",
    intents: [B(70, "부스러기 세례"), AC("st_sugar", 1, "draw", "설탕을 묻힌다"), A(60, "바스락 할퀸다")],
    passives: [P("마지막 부스러기", "death", AC("st_sugar", 1, "draw", "마지막 부스러기")), P("같이 바스러진다", "allyDown", BF("사기", 1, "부스럭 성낸다"))],
  },
  buseuleogi_cool: {
    pick: "cycle", blurb: "컵케이크 왕국을 꿈꾸는 부스러기 — 적 전체에 실드를 두르고, 턴이 끝날 때 제 실드가 남아 있으면 사기가 오른다. 실드를 깨서 넘겨라.",
    intents: [G(40, "컵케이크 성벽"), B(70, "크림을 쏜다"), BL(70, "컵에 들어간다")],
    passives: [P("왕국의 꿈", "turnEnd", BF("사기", 1, "컵케이크 왕국 만세", { if: { selfBlock: true } }))],
  },
  mogmaekim_jolly: {
    pick: "shuffle", blurb: "부풀어 오른 목매킴 — 쓰러지면 펑 터져 파티에 고통 2 를 남긴다. 고통에 대비하고 처치하라.",
    intents: [A(90, "통통 튄다"), BL(60, "바람을 넣는다"), A(110, "몸통 박치기")],
    passives: [P("펑!", "death", DB("고통", 2, "펑! 터진다"))],
  },
  mogmaekim_mad: {
    pick: "shuffle", blurb: "설탕이 너무 들어간 목매킴 — 턴마다 「설탕 과다」 가 쌓여 피해가 오르고, 넷이면 다음 차례에 폭발한다. 빨리 끊어라.",
    intents: [A(100, "설탕 박치기"), M(40, 3, "끈적한 연타"), BL(60, "설탕 코팅")],
    counters: [{ name: "설탕 과다", desc: "턴마다 부푼다", onTurnStart: 1, dealt: 0.1, max: 4, at: 4, act: AA(120, "설탕 폭발", { id: "손상", n: 2 }), mode: "next" }],
  },
  marshmallowtanker_naive: {
    pick: "cycle", tough: 5, blurb: "탱탱한 마시멜로 방패 — 제 실드가 깨지면 말랑 조각을 흩어 적 전체에 실드를 준다. 실드는 한 번에 크게 깨라.",
    intents: [BL(90, "탱탱하게 부푼다", { tough: 1 }), A(90, "말랑 박치기"), G(50, "말랑 벽")],
    passives: [P("말랑 흩어짐", "guardBreak", G(40, "말랑 조각이 흩어진다")), P("터진 마시멜로", "broken", BF("취약", 2, "속이 터진다"))],
  },
  marshmallowdealer_jolly: {
    pick: "shuffle", blurb: "꼬치를 든 마시멜로 — 스킬 카드를 보면 변덕을 부려 예고를 꼬치 찌르기로 바꾼다(턴당 1).",
    intents: [B(80, "꼬치로 찌른다", { id: "고통", n: 1 }), BL(50, "말랑해진다"), A(70, "톡톡 친다")],
    passives: [P("변덕", "card", SH(B(110, "꼬치 찌르기", { id: "고통", n: 2 }), "변덕 — 꼬치를 고쳐 쥔다"), { type: "스킬" })],
  },
  marshmallowdealer_mad: {
    pick: "shuffle", blurb: "늘 화가 난 마시멜로 — 맞을 때마다 「부글부글」 이 올라 주는 피해가 커진다(최대 5). 한 방에 크게.",
    intents: [M(35, 3, "분풀이 연타"), A(100, "부글부글 박치기"), M(45, 2, "꼬치 난타")],
    counters: [{ name: "부글부글", desc: "맞을수록 화가 난다", onHit: 1, dealt: 0.1, max: 5 }],
  },
  marshmallowsupporter_jolly: {
    pick: "shuffle", blurb: "쫀득한 응원 마시멜로 — 동료를 치유하고 피해 감소를 두른다. 디버프를 받으면 설탕 코팅으로 회복한다.",
    intents: [H(80, "쫀득 붕대"), BFA("피해 감소", 1, "설탕 코팅"), AC("st_sugar", 1, "draw", "설탕을 뿌린다"), A(50, "톡 친다")],
    passives: [P("설탕 코팅", "debuffed", SHL(40, "설탕 코팅"))],
  },
  goldring_gloomy: {
    pick: "shuffle", rare: [{ id: "crystal" }], blurb: "엘리트 · 한숨 쉬는 금고 — 공격 카드 두 장째마다 뚜껑을 닫아 실드를 얻는다(턴당 2). 격파하면 열려 취약 2. 희귀종: 전투 시작 결정화 3.",
    intents: [A(130, "금고 문 박치기"), BL(90, "자물쇠를 건다"), M(50, 3, "동전 세례"), A(150, "금괴 내려찍기")],
    passives: [P("뚜껑", "card", BL(70, "뚜껑을 닫는다"), { type: "공격", every: 2, limit: 2 }), P("열린 금고", "broken", BF("취약", 2, "금고가 열린다"))],
  },
  goldring_jolly: {
    pick: "shuffle", rush: 5, rare: [{ id: "reshuffle", card: "st_jitters" }], blurb: "엘리트 · 보물 지기 — 스킬 두 장째마다 「경계심」 이 차 「설탕 범벅」 을 손에 넣는다. 희귀종: 행동할 때마다 손을 버리고 섞은 뒤 「안절부절」 1장 — 행동 카운트 5 안에 손을 써라.",
    intents: [A(120, "보물을 노리는 자에게"), BL(80, "보물을 품는다"), M(45, 3, "금화 튕기기")],
    counters: [{ name: "경계심", desc: "스킬을 낼 때마다 눈을 번뜩인다", onCard: 1, cardType: "스킬", max: 9, at: 2, act: AC("st_sugar", 1, "hand", "보물에 손대지 마!"), mode: "now" }],
  },
  clone_erpin: {
    pick: "cycle", blurb: "2층 보스 · 에르핀(클론) — 무전취식 여왕. 파티가 카드를 낼 때마다 「외상 장부」 가 차 여덟이면 외상값으로 다음 턴 AP 를 1 깎는다. 「배고픔」 하나당 피해가 오르고 맞으면 빠진다(턴 시작에 둘). 곁의 멜로가 쓰러지면 먹어 치워 크게 회복 — 졸개를 남겨 두는 것도 수다.",
    intents: [B(160, "산을 뽑아 던진다"), M(45, 3, "케이크 난사", { id: "약화", per: 1 }), BF("피해 감소", 2, "케이크로 배를 채운다"), CH("돌진 자세", AA(150, "돌겨어어어!!!", { id: "약화", n: 2 }), true), BF("취약", 3, "억⋯?")],
    phase: { at: 0.55, say: "「간식 시간이다!」", intents: [AC("st_sugar", 2, "draw", "설탕을 퍼붓는다"), B(180, "왕관의 괴력"), CH("다시 돌진 자세", AA(170, "돌겨어어어!!!")), BF("취약", 3, "억⋯?"), H(200, "12끼째")] },
    phase2: { at: 0.2, say: "「배고파… 다 먹어 버릴 거야」", intents: [M(60, 4, "굶주린 난동"), B(200, "왕관을 던진다")] },
    counters: [
      { name: "외상 장부", desc: "파티가 카드를 낼 때마다 외상이 쌓인다", onCard: 1, max: 9, at: 8, act: J(1, "외상값을 네 앞으로 달아 둔다"), mode: "now" },
      { name: "배고픔", desc: "배고플수록 세다", start: 2, max: 2, dealt: 0.2, onHit: -1, resetTurnStart: true },
    ],
    passives: [P("무전취식", "allyDown", SHL(300, "무전취식"), { limit: 0 }), P("왕관의 저주", "broken", BF("취약", 2, "왕관이 미끄러진다"))],
  },
};
