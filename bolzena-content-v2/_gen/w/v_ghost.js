// 유령 늪 — 결: 죽어도 끝이 아니다 — 죽을 때 효과(재 속 · 가사 · 상태 카드 · 취약) · 영혼 공유 · 미끄러움 · 손 흔들기 · 정신 붕괴
const { A, B, M, AA, BL, G, H, BF, BFA, DB, J, AC, CD, HC, SM, CT, SH, CH, RS, TH, SHL, RV, FG, P } = require("./lib");
module.exports = {
  // ── 1층 유령 늪 ──
  pumpkin_naive: {
    pick: "shuffle", blurb: "유령들이 아끼는 호박 — 굴러와 부딪히고, 깨지면서 파티에 취약 1 을 남긴다.",
    intents: [BL(70, "껍질을 굳힌다"), A(90, "데굴데굴 박치기"), M(35, 2, "통통 튀기")],
    passives: [P("깨진 호박", "death", DB("취약", 1, "호박씨가 튄다"))],
  },
  blanketghost_jolly: {
    pick: "shuffle", blurb: "장난꾸러기 이불령 — 뒷줄로 스며들고, 카드를 내면 손 1장이 미끄러져 떨어지는 「미끄러움」 을 건다.",
    intents: [B(70, "이불 속에서 찌른다"), DB("미끄러움", 1, "이불을 잡아당긴다"), AC("st_whisper", 1, "draw", "오싹한 속삭임")],
  },
  pumpkin_jolly: {
    pick: "shuffle", soul: true, blurb: "떼로 자라는 호박 — 영혼을 나눠 가져 맨 앞 호박이 아니면 피해를 받지 않는다. 앞에서부터 하나씩. 동료가 쓰러지면 사기.",
    intents: [A(80, "데굴데굴"), BL(60, "호박 껍질"), A(70, "줄기 채찍")],
    passives: [P("호박밭의 원한", "allyDown", BF("사기", 1, "호박밭의 원한"))],
  },
  blanketghost_naive: {
    pick: "shuffle", blurb: "아직 모습을 갖추지 못한 어린 유령 — 쓰러져도 재 속에서 2턴 뒤 절반 체력으로 돌아온다. 그 전에 싸움을 끝내라.",
    intents: [B(80, "후열로 파고든다"), A(60, "스르륵"), DB("약화", 1, "서늘한 숨결")],
    passives: [P("다시 뭉친다", "death", RV(50, 2, "흩어진 이불이 다시 뭉친다"))],
  },
  oldtree_gloomy: {
    pick: "shuffle", blurb: "늪가에서 썩어 가는 고목 — 맞으면 썩은 즙이 때린 사도에게 튄다(턴당 2). 고통을 건다.",
    intents: [DB("고통", 2, "썩은 즙"), A(90, "썩은 가지"), BL(70, "이끼를 두른다")],
    passives: [P("썩은 즙", "hurt", TH(40, "썩은 즙이 튄다"), { limit: 2 })],
  },
  nependers_mad: {
    pick: "cycle", blurb: "예민한 늪 한입초 — 카드로 당기면 사기가 오른다. 크게 벌린 입은 격파로 끊긴다.",
    intents: [A(90, "덥석"), CH("입을 크게 벌린다", A(190, "통째로 삼킨다"), true), BL(60, "오므린다")],
    passives: [P("예민", "rushed", BF("사기", 1, "건드리지 마!"))],
  },
  pumpkin_mad: {
    pick: "shuffle", blurb: "앙갚음 호박 — 맞을 때마다 때린 사도에게 되친다(턴당 3). 잘게 여러 번 치면 손해.",
    intents: [A(100, "앙갚음 박치기"), BL(80, "단단한 껍질"), A(90, "굴러 덮치기")],
    passives: [P("앙갚음", "hurt", TH(30, "앙갚음"), { limit: 3 })],
  },
  blanketghost_mad: {
    pick: "shuffle", blurb: "장난 중독 이불령 — 「오싹한 속삭임」 을 둘씩 넣고, 쓰러지면서도 하나 더 남긴다.",
    intents: [AC("st_whisper", 2, "draw", "속삭임 장난"), B(80, "이불 찌르기"), DB("미끄러움", 1, "이불 걸기")],
    passives: [P("마지막 장난", "death", AC("st_whisper", 1, "draw", "마지막 장난"))],
  },
  nururingtanker_ghost: {
    pick: "cycle", tough: 5, blurb: "비웃는 젤리 방패 — 카드로 당기면 파티에 취약 1. 쓰러진 척했다가(가사) 동료가 회복을 쓰면 일어선다.",
    intents: [G(60, "젤리 방벽", { tough: 1 }), A(90, "히죽 박치기"), BL(100, "몸을 부풀린다"), A(110, "깔고 앉는다")],
    passives: [P("비웃음", "rushed", DB("취약", 1, "히죽")), P("죽은 척", "death", FG("죽은 척한다"))],
  },
  nururingwarrior_ghost: {
    pick: "cycle", blurb: "장난기 심한 젤리 — 뒷줄을 관통하고, 모으는 한 방은 격파로 끊긴다.",
    intents: [B(90, "관통 장난"), CH("히죽 웃는다", A(190, "깜짝 젤리"), true), BL(40, "흐물흐물")],
  },
  nururingarcher_ghost: {
    pick: "shuffle", blurb: "장난 주문 젤리 — 미끄러움 · 약화를 건다.",
    intents: [DB("미끄러움", 1, "미끌미끌 주문"), DB("약화", 1, "흐물 주문"), B(70, "젤리 화살")],
  },
  nururingsupporter_ghost: {
    pick: "shuffle", blurb: "응원하는 젤리 — 치유(쓰러진 척한 동료를 일으킨다) · 적 전체 사기.",
    intents: [H(80, "젤리 붕대"), BFA("사기", 1, "힘내라 젤리!"), A(50, "톡 친다")],
  },
  pumpkin_gloomy: {
    pick: "shuffle", rare: [{ id: "crystal" }], blurb: "엘리트 · 불쌍한 호박 — 디버프를 받으면 면역 1(턴당 1) · 회복 60. 쓰러지면 재 속에서 2턴 뒤 40% 로 돌아온다. 희귀종: 전투 시작 결정화 3.",
    intents: [A(130, "서러운 박치기"), BL(110, "웅크린다"), M(45, 3, "호박씨 난사"), H(120, "흐느낀다")],
    passives: [P("서러움", "debuffed", BF("면역", 1, "서러워")), P("눈물 젖은 호박", "debuffed", SHL(60, "눈물 젖은 호박")), P("늪에 묻힌다", "death", RV(40, 2, "늪 아래에서 다시 자란다"))],
  },
  blanketghost_cool: {
    pick: "shuffle", rare: [{ id: "reshuffle", card: "st_jitters" }], blurb: "엘리트 · 분위기 파괴 이불령 — 스킬 카드 두 장째마다 「눈치」 가 차 다음 턴 AP -1. 희귀종: 행동할 때마다 손을 버리고 섞은 뒤 「안절부절」 1장.",
    rush: 5,
    intents: [B(110, "찬물 끼얹기"), DB("약화", 2, "썰렁한 농담"), M(45, 3, "이불 채찍"), BL(90, "이불을 뒤집어쓴다")],
    counters: [{ name: "눈치", desc: "스킬을 쓰면 분위기를 깬다", onCard: 1, cardType: "스킬", max: 9, at: 2, act: J(1, "분위기 파토"), mode: "now" }],
  },
  clone_spiky: {
    pick: "cycle", blurb: "1층 보스 · 스피키(클론) — 완벽한 따라쟁이. 파티가 낸 카드 종류를 흉내 내 예고를 바꾼다: 공격을 보면 공격으로, 스킬을 보면 약화를 거는 몰래 찌르기로, 강화를 보면 사기로(종류마다 턴당 1). 마지막에 낸 카드가 다음 수를 정한다. 호박들은 영혼을 나눈다.",
    intents: [B(110, "따라 찌르기"), AC("st_whisper", 2, "draw", "분실물을 섞어 놓는다"), DB("미끄러움", 2, "발밑을 흉내 낸다"), CH("숨을 들이쉰다", AA(130, "완벽한 성대모사"), true)],
    phase: { at: 0.5, say: "「이번엔 너희 차례를 따라 할래」", intents: [RS("st_jitters", 1, "물건을 죄다 뒤섞는다"), M(45, 4, "따라 때리기"), B(150, "흉내 찌르기"), CH("따라 할 준비", AA(160, "완벽한 성대모사!"))] },
    passives: [
      P("따라 하기 — 공격", "card", SH(A(140, "따라 하기 — 공격!"), "공격을 흉내 낸다"), { type: "공격" }),
      P("따라 하기 — 스킬", "card", SH(B(100, "따라 하기 — 몰래 찌르기!", { id: "약화", n: 1 }), "스킬을 흉내 낸다"), { type: "스킬" }),
      P("따라 하기 — 강화", "card", SH(BFA("사기", 1, "따라 하기 — 기합!"), "강화를 흉내 낸다"), { type: "강화" }),
      P("들켰다", "broken", BF("취약", 2, "들켰다!")),
    ],
  },
  // ── 2층 셰이디의 아공간 ──
  shadyfollowercloserange_naive: {
    pick: "shuffle", blurb: "도끼 응원봉 극성팬 — 쓰러지면서 마지막 응원으로 적 전체 사기를 올린다. 남은 적이 많을 땐 순서를 골라라.",
    intents: [A(100, "응원봉 휘두르기"), BL(60, "응원 자세"), A(80, "환호 박치기")],
    passives: [P("마지막 응원", "death", BFA("사기", 1, "셰이디 님 만세…!"))],
  },
  shadyfollowerlongrange_naive: {
    pick: "shuffle", blurb: "원거리 극성팬 — 응원 소리로 뒷줄을 친다.",
    intents: [B(90, "응원 함성"), A(60, "야광봉 던지기"), DB("약화", 1, "귀청이 떨어진다")],
  },
  blanketghost_gloomy: {
    pick: "shuffle", blurb: "이불 속 우울한 유령 — 고통을 걸고, 쓰러진 척(가사) 했다가 동료의 회복에 일어선다.",
    intents: [DB("고통", 2, "우울한 한숨"), B(80, "이불 찌르기", { rush: 0 }), BL(60, "이불을 뒤집어쓴다")],
    passives: [P("이불 속으로", "death", FG("이불 속으로 숨는다"))],
  },
  shadyfollowercloserange_mad: {
    pick: "shuffle", blurb: "단검 응원봉 극성팬 — 연타마다 균열을 새긴다.",
    intents: [M(30, 3, "단검봉 연타", { id: "균열", per: 1 }), A(90, "찌르기"), BL(50, "응원봉 방패")],
  },
  shadyfollowerlongrange_mad: {
    pick: "shuffle", blurb: "아공간을 넘나드는 극성팬 — 뒷줄로 순간이동해 균열을 새긴다.",
    intents: [B(90, "순간이동 찌르기", { id: "균열", n: 2 }), A(60, "응원봉"), B(70, "뒤에서 찌르기")],
  },
  shadyfollowercloserange_jolly: {
    pick: "shuffle", rush: 3, blurb: "하이빔 극성팬 — 눈을 멀게 해 미끄러움 · 약화를 건다. 카드 세 장이면 움직인다.",
    intents: [DB("미끄러움", 1, "하이빔"), A(80, "응원봉"), DB("약화", 1, "눈부심")],
  },
  shadyfollowerlongrange_jolly: {
    pick: "shuffle", blurb: "응원단장 — 적 전체 사기를 올리고 「극성팬 편지」 를 보낸다.",
    intents: [BFA("사기", 1, "응원 구호"), AC("st_fanletter", 1, "draw", "극성팬 편지"), B(70, "확성기")],
  },
  shadyfollowercloserange_gloomy: {
    pick: "cycle", blurb: "진짜 도끼를 든 극성팬 — 크게 들어 올린 도끼는 격파로 끊긴다. 칠 때마다 충격을 남긴다.",
    intents: [A(100, "도끼질", { id: "충격", n: 1 }), CH("도끼를 들어 올린다", A(200, "진짜 도끼"), true), BL(70, "도끼 자루로 막는다")],
  },
  shadyfollowerlongrange_cool: {
    pick: "shuffle", blurb: "팬클럽 회장 — 동료가 쓰러지면 적 전체 사기. 쓰러져도 재 속에서 2턴 뒤 절반으로 돌아온다(회장은 물러나지 않는다).",
    intents: [BFA("사기", 1, "회장의 연설"), B(90, "회장 직권 사격"), AC("st_fanletter", 1, "draw", "회보 발송")],
    passives: [P("회원을 잃었다", "allyDown", BFA("사기", 1, "회원을 잃었다")), P("회장은 물러나지 않는다", "death", RV(50, 2, "회장 복귀"))],
  },
  shadyfollowercloserange_cool: {
    pick: "shuffle", rare: [{ id: "actDebuff", st: "취약" }], blurb: "엘리트 · 친위대장 — 칠 때마다 충격을 남긴다(실드 위로 맞으면 더 아프다). 실드에만 기대지 말 것. 희귀종: 행동할 때 파티에 취약 2.",
    intents: [M(40, 3, "친위대 찌르기", { id: "충격", per: 1 }), A(130, "친위대장 일격"), DB("균열", 3, "쓰라림"), BL(100, "친위대 방패")],
  },
  blanketghost_cool_elite: {
    pick: "shuffle", rare: [{ id: "anxietyHits", card: "st_jitters" }], blurb: "엘리트 · 악몽 이불령 — 턴 시작마다 「오싹한 속삭임」 을 손에, 「안절부절」 을 더미에 넣는다. 희귀종: 덱의 안절부절 수만큼 타격 +1 — 짧게 끝내라. 쓰러질 때 정신 붕괴 1.",
    intents: [B(90, "악몽 찌르기"), AC("st_jitters", 1, "draw", "악몽을 심는다"), M(35, 2, "가위눌림"), DB("약화", 1, "식은땀")],
    passives: [P("잠 못 드는 밤", "turnStart", AC("st_whisper", 1, "hand", "잠 못 드는 밤")), P("깨지 않는 악몽", "death", DB("정신 붕괴", 1, "깨지 않는 악몽"))],
  },
  clone_shady: {
    pick: "cycle", blurb: "2층 보스 · 셰이디(클론) — 역대 최고의 장난을 꿈꾸는 우두머리. 턴마다 「장난 수첩」 이 차 넷이면 다음 차례에 「역대 최고의 장난」(전체 공격 · 정신 붕괴 1 — 남은 채 싸움이 끝나면 다음 싸움까지 카드 얻기 · 신탁 · 제거가 막힌다). 디버프를 걸면 수첩이 한 칸 더 찬다. 삼지창은 실드를 건너 꽂힌다. 격파하면 수첩이 찢긴다.",
    intents: [B(170, "관통 삼지창"), CD("침체", 2, "draw", "장난 — 비용 뒤섞기"), DB("취약", 1, "깜짝 장난"), J(1, "아공간에 AP 를 가둔다"), M(50, 3, "삼지창 연타")],
    phase: { at: 0.5, say: "「이제부터가 진짜 장난이야!」", intents: [SM("shadyfollowercloserange_naive", 2, 2, "극성팬 소환"), B(190, "관통 삼지창"), DB("미끄러움", 2, "발 걸기"), CH("장난을 모은다", AA(170, "아공간 대폭소"), true)] },
    counters: [{ name: "장난 수첩", desc: "턴마다 장난이 하나씩 늘어난다", onTurnStart: 1, max: 4, at: 4, act: AA(130, "역대 최고의 장난!", { id: "정신 붕괴", n: 1 }), mode: "next" }],
    passives: [P("장난 맞받기", "debuffed", CT("장난 수첩", 1, "그거 내 장난이잖아!")), P("찢긴 수첩", "broken", CT("장난 수첩", -3, "수첩이 찢어졌다"))],
  },
};
