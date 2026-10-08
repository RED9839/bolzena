// 18갈래 재설계 3단계 — 엘프A 묶음 11명(2026-10-08). 기준: 시범 17명(05_시범16_결과.md) · 지침 BRIEF_118.md
// 레이지 · 로네 · 로네(시장) · 리뉴아 · 리스티 · 마에스트로 2호 · 아멜리아 · 아멜리아(R41) · 아이시아 · 알레트 · 엘레나
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장 · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀). 다 차면 저절로 터짐 없음(onMax make/empower).
// node _gen/rework/엘프A_18.mjs [사도 이름 일부]  → heroes/엘프/<파일>.json (원본은 백업 SRC 에서 읽음)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, hits, sh, heal, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, later,
  TOP, ALLY, nextAp, disc, burn, hasten, gauge, ripen, perGone, ifTune, srch, drawType, pull, xtra, pw, pas, token, five, Or, bl, U, setCards, setOpener, renameKw, perOver, exile, cs, dmod, spendN, tough } = { ...L, ifTune: { k: 'ifTune' } };
const perG = p => ({ k: 'perGuarded', per: p });
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));

// ════════════════════════════════════════════════════════════════════
// 1. 레이지 — 거드는 형 · 딜러 · 냉정(1성). 동료가 한 턴에 처음 내는 종류(공격 · 스킬 · 강화)마다 불려 가 그 종류의 잡일을 덤으로 — 기록 넷이면 다음 한 방에 몰아 쓴다
// 원작: 부서마다 불려 다니는 IT 관리반 말단 · 저학년 XG-레이저(직선 광역) · 어사이드 없음 · 「도구는 일부러 안 만든다」(장치가 카드를 만들지 않는 이유)
// 시동: u3 리그 오브 엘프 90.1%(0코 개전 강화 — 그대로)
// ════════════════════════════════════════════════════════════════════
function raizy(j) {
  const H = '레이지', K = '출동 기록';
  const h = j.heroes[0];
  h.blurb = '못 하는 게 없어 온갖 잡일에 불려 다니는 만능 해결사. 동료가 한 턴에 처음 내는 종류마다 그 종류의 잡일을 덤으로 해치우고, 출동 기록이 넷 차면 다음 한 방에 몰아 씁니다.';
  h.keyword = { name: K, desc: '부서마다 불려 다닌 기록', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.07 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('만능 해결사', 'play', [stk(K, 1), xtra(0.6)], { when: { who: 'other', type: '공격' }, limit: 1 }),
    pas('만능 해결사', 'play', [stk(K, 1), sh(0.5)], { when: { who: 'other', type: '스킬' }, limit: 1 }),
    pas('만능 해결사', 'play', [stk(K, 1), draw(1)], { when: { who: 'other', type: '강화' }, limit: 1 }),
    pas('커피 수혈', 'fightStart', [stk(K, 2)]),
  ];
  setCards(j, [
    // 쓰기 — XG 레이저: 적 전체 + 기록 1개당 광역, 기록 전부 소모
    U(H, 1, 'XG - 레이저', 1, '공격', [dmg(0.75, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      'A',
      ['D', 'play', [dmg(0.25, EA)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([dmg(0.7, EA), per(K), dmg(0.18, EA), tough(1, EA)]),
      Or([dmg(0.7, EA), per(K), dmg(0.18, EA), srch()]),
      'Hx',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 원작 자유 — 유능한 것도 문제예요: 단일 + 기록
    U(H, 2, '유능한 것도 문제예요', 1, '공격', [dmg(1.15), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.8), ifStack(K, 3), dmg(0.8)]),
      ['D', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 }],
      'Hd',
    ], bl('weakSpot', 'cost', [stk(K, 1)])),
    // 시동 — 리그 오브 엘프 90.1%(0 · 개전 강화): 기록 + 기본 카드 1장 + 매 턴 기록
    U(H, 3, '리그 오브 엘프 90.1%', 0, '강화', [stk(K, 1), draw(1, { basic: true }), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), draw(1, { basic: true }), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), draw(1, { basic: true }), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { tags: ['개전'] }),
      Or([sh(1.2), draw(1, { basic: true }), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([sh(1.1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 3), sh(0.5), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 만들기 둘째 — 용접 야근: 기록 1개당(쓰지 않음 — 쥐고 다음 한 방을 기다릴지)
    U(H, 4, '용접 야근', 1, '공격', [dmg(0.85), per(K), dmg(0.3)], [
      'A',
      ['C', [tough(1)]],
      Or([dmg(0.7), per(K), dmg(0.25), stk(K, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.8), per(K), dmg(0.28), srch({ type: '공격' })]),
    ], bl('power', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 꼬인 선 정리: 실드 + 기록 + 동료 카드 1장 뽑기(동료가 낼 거리를 만든다)
    U(H, 5, '꼬인 선 정리', 1, '스킬', [sh(0.9), stk(K, 1), draw(1, { who: 'other' })], [
      'A', 'B',
      Or([sh(1.1), stk(K, 2), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })]),
      Or([sh(1.2), stk(K, 1), pw('play', [sh(0.35)], { when: { who: 'other', type: '스킬' }, limit: 1 })], { power: true }),
      Or([sh(1.3), stk(K, 2), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 로네 — 버티형 · 탱커 · 순수(변환). 지난 판에 막아 낸 양이 「진심」 으로 바뀌고, 진심 셋이면 손에 건틀릿 진심 강타(기절) — 쓸까(건틀릿 1개당) 모을까(강타)
// 원작: 저학년 어리바리 블랙옵스(받는 피해 감소) · 고학년 항복(적 공격력↓) · 아티팩트 「로네의 건틀릿」 강화 평타(가끔 진심 강타 — 넉백 + 기절) · 돈까스
// 시동: u2 더듬는 거짓말 — 로네(시장)은 도시락을 「배달」, 로네는 막아서 「진심」 을 모은다
// ════════════════════════════════════════════════════════════════════
function rone(j) {
  const H = '로네', K = '로네의 진심', TOK = '로네_strike';
  const h = j.heroes[0];
  h.blurb = '거짓말을 하면 더듬는 외교관 겸 첩보원. 갑옷으로 막아 낸 만큼 숨겨 둔 진심이 차오르고, 진심이 셋 모이면 건틀릿에 실은 진심 강타를 손에 쥡니다.';
  h.keyword = { ...h.keyword, onMax: { make: TOK, consume: true } };
  delete h.keyword.rules;
  h.passives = [
    pas('갑옷 속 진심', 'guardSum', [{ k: 'stack', id: K, v: 1, ofEvent: 0.008 }]),
    pas('항복 협상', 'fightStart', [st('피해 감소', 1)]),
  ];
  const tokens = [token(TOK, '건틀릿 진심 강타', H, '공격', [ddef(1.0), st('기절', 1)], { cost: 1, blurb: '갑옷 속에 숨겨 둔 진심 — 이번만큼은 정말로 세게 칩니다' })];
  setCards(j, [
    // 갈래 부품 — 어리바리 블랙옵스: 실드 + 피해 감소 + 진심
    U(H, 1, '어리바리 블랙옵스', 1, '스킬', [sh(1.1), st('피해 감소', 1), stk(K, 1)], [
      'A', 'B',
      Or([sh(1.2), perG(40), sh(0.25), stk(K, 1)]),
      Or([sh(1.4), st('피해 감소', 1), pw('guardSum', [sh(0.5)], { limit: 1 })], { power: true }),
      Or([sh(1.8), st('피해 감소', 2), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 시동 — 더듬는 거짓말: 약화 + 진심 + 드로우
    U(H, 2, '더듬는 거짓말', 1, '스킬', [st('약화', 2), stk(K, 1), draw(1)], [
      'A', 'B',
      Or([st('약화', 2, EA), sh(0.5), draw(1)]),
      Or([st('약화', 2), sh(0.95), pw('hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 })], { power: true }),
      Or([st('약화', 3), sh(1.8), disc(1)]),
    ], null),
    // 원작 자유 — 소스 듬뿍 경양식(돈까스): 회복 + 지난 판에 막아 낸 피해 40당 회복
    U(H, 3, '소스 듬뿍 경양식', 1, '스킬', [heal(0.6), perG(40), heal(0.12), stk(K, 1)], [
      'A', 'B',
      Or([sh(0.8), st('결의', 1), stk(K, 1)]),
      Or([heal(0.55), perG(40), heal(0.1), srch()]),
      Or([heal(1.45), perG(40), heal(0.28), disc(1)]),
    ], bl('heal', 'ap', [stk(K, 1)])),
    // 쓰기 — 로네의 건틀릿: 진심 1개당 방어 기반, 진심 전부 소모(셋을 기다려 강타를 받을지)
    U(H, 4, '로네의 건틀릿', 1, '공격', [ddef(0.55), per(K), ddef(0.3), spendAll(K)], [
      'A',
      ['D', 'guardSum', [stk(K, 1)], { limit: 1 }],
      Or([ddef(0.5), per(K), ddef(0.28), st('약화', 1)]),
      Or([ddef(0.5), per(K), ddef(0.28), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 만들기 둘째 — 징글벨 돌격: 방어 기반 + 진심, 지난 판에 막아 낸 피해 50당 1타
    U(H, 5, '징글벨 돌격', 1, '공격', [ddef(0.6), stk(K, 1), perG(50), ddef(0.12)], [
      'A', 'B',
      Or([ddef(0.55, EA), perG(50), ddef(0.12, EA)]),
      ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([ddef(0.9), stk(K, 2), disc(1)]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 3. 로네(시장) — 거드는 형 · 딜러 · 우울. 동료가 카드를 낼 때마다 돈까스 도시락이 배달되고, 도시락을 비우면 지지율 — 셋이면 파티의 다음 카드 강화(지지자 환호)
// 원작: 저학년 돈까스 배달 드론(도착하면 중 · 후열 아군 공격력↑ + 4회 범위, 1명이면 더 셈) · 고학년 지지자 돌진 · 돈까스를 끝내 못 먹음
// 시동: u3 돈까스 생각 → u2 배달 주문(만들기 칸)
// ════════════════════════════════════════════════════════════════════
function roneMayor(j) {
  const H = '로네_시장', K = '지지율', T1 = '로네_시장_t1';
  const h = j.heroes[0];
  h.blurb = '돈까스를 끝내 입에 넣지 못하는 시장. 동료가 카드를 낼 때마다 돈까스 도시락이 배달되고, 도시락이 비워질 때마다 지지율이 올라 파티의 다음 한 수에 힘을 싣습니다.';
  h.keyword = { name: K, desc: '도시락으로 모으는 민심', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.5, consume: true } };
  h.passives = [
    pas('돈까스 배달', 'play', [make(T1, 1)], { when: { who: 'other' }, limit: 2 }),
    pas('바삭한 꿈', 'fightStart', [make(T1, 1)]),
  ];
  const t1 = j.cards.find(c => c.id === T1);
  const tokens = [{ ...t1, fx: [heal(0.4), stk(K, 1)] }];
  setOpener(j, H, 'u3', 'u2');
  setCards(j, [
    // 쓰기 — 돈까스 시장, 로네!: 4회 광역 + 손의 도시락 1장당 광역, 손의 도시락 모두 소멸
    U(H, 1, '돈까스 시장, 로네!', 1, '공격', [dmg(0.18, EA, { hits: 4 }), perTag(T1), dmg(0.28, EA), exile('hand', { all: true, tag: T1 })], [
      'A',
      ['D', 'make', [dmg(0.2, ER)], { limit: 1 }],
      Or([dmg(0.45, E1, { hits: 4 }), perTag(T1), dmg(0.4)]),
      Or([dmg(0.16, EA, { hits: 4 }), perTag(T1), dmg(0.25, EA), srch()]),
      'Hx',
    ], bl('power', 'ap', [make(T1, 1)])),
    // 시동 — 배달 주문: 도시락 둘 + 드로우
    U(H, 2, '배달 주문', 1, '스킬', [make(T1, 2), draw(1)], nm([
      Or([make(T1, 3), draw(1)]),
      Or([make(T1, 1), draw(1)], { cost: 0 }),
      Or([make(T1, 2), sh(1.2), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })]),
      Or([make(T1, 2), srch(), sh(0.8)]),
      Or([make(T1, 3), draw(2), disc(1)]),
    ]), null),
    // 원작 자유 — 돈까스 생각(강화): 지지율 + 내 카드(도시락)가 소멸하면 무작위 적에게 — 먹지 못한 돈까스
    U(H, 3, '돈까스 생각', 1, '강화', [stk(K, 1), pw('exhaust', [dmg(0.3, ER)], { limit: 2 })], nm([
      Or([stk(K, 2), pw('exhaust', [dmg(0.3, ER)], { limit: 2 })], { tags: ['개전'] }),
      Or([pw('exhaust', [dmg(0.3, ER)], { limit: 2 })], { cost: 0, tags: ['개전'] }),
      Or([make(T1, 1), pw('exhaust', [dmg(0.3, ER)], { limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 1), srch(), pw('exhaust', [dmg(0.3, ER)], { limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 1), pw('exhaust', [dmg(0.55, ER)], { limit: 2 })], { tags: [] }),
    ]), bl('atkUp', 'draw', [make(T1, 1)]), ['개전']),
    // 만들기 둘째 — 돈까스 모독은 용서 못 해(2): 단일 + 지지율 1개당, 전부 소모(셋을 기다려 강화를 줄지)
    U(H, 4, '돈까스 모독은 용서 못 해', 2, '공격', [dmg(1.7), per(K), dmg(0.4), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.8), perTag(T1), dmg(0.45), exile('hand', { all: true, tag: T1 })]),
      ['D', 'make', [dmg(0.25)], { limit: 1 }],
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 업무 효율 131%(0): 지지율 + 동료의 손 카드 1장 비용 -1
    U(H, 5, '업무 효율 131%', 0, '스킬', [stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })], [
      'A', 'B',
      Or([make(T1, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })]),
      Or([stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      Or([stk(K, 2), heal(0.5), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })]),
    ], bl('draw', 'cost', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 4. 리뉴아 — 예약형 · 딜러 · 광기 · 엘다인. 멈춘 둠스데이 시계의 「초침」 이 다 돌면 정각에 적 전체 — 그리고 다시 감긴다(반복 예약). 당겨 쓰면 남은 칸만큼 약하다
// 원작: 어사이드 「고장 난 둠스데이 클락」(몇 초마다 미사일, 크게 맞으면 그때도) · 고학년 타임 브레이크 · 저학년 시공의 메아리 5연타(버프 · 실드 지우기) · 1초를 사랑함
// 엘다인 한 단계: 정각마다 다시 감김 + 그 턴 주는 피해↑
// 시동: u2 규격에 딱 맞는 1초(0코 — 조율이면 재촉)
// ════════════════════════════════════════════════════════════════════
function renewa(j) {
  const H = '리뉴아', K = '초침';
  const h = j.heroes[0];
  h.blurb = '규격에 딱 맞는 1초를 사랑하는 시간 여행자. 멈춰 버린 둠스데이 시계가 다 돌 때마다 정각에 적 전체를 치고 다시 감기며, 남은 AP 를 딱 맞춰 쓰면 시계가 빨리 돕니다.';
  h.keyword = {
    name: K, desc: '멈춘 둠스데이 시계가 정각까지 남긴 칸', carrier: 'self', cap: 2, reserve: true, decay: 1,
    rules: [{ name: '정각', when: { on: 'stackGone', id: K, decay: true }, fx: [dmg(0.8, EA), dmod(0.15, 'self'), stk(K, 2)] }],
  };
  h.passives = [
    pas('째깍', 'hurt', [hasten(1)], { when: { pct: 0.08 }, limit: 1 }),
    pas('시공간, 저 너머로!', 'fightStart', [stk(K, 1)]),
  ];
  setCards(j, [
    // 원작 자유 — 시공의 메아리: 5연타 광역 + 적의 이로운 효과 하나 지움
    U(H, 1, '시공의 메아리', 1, '공격', [dmg(0.17, EA, { hits: 5 }), { k: 'dispel', n: 1 }], [
      'A',
      ['D', 'reserveFire', [dmg(0.25, EA)], { limit: 1 }],
      Or([dmg(0.16, EA, { hits: 5 }), { k: 'strip' }]),
      Or([dmg(0.16, EA, { hits: 5 }), { k: 'dispel', n: 1 }, srch()]),
      'Hn',
    ], bl('power', 'weakSpot', [hasten(1)])),
    // 시동 — 규격에 딱 맞는 1초(0): 드로우, 조율(남은 AP 를 딱 맞춰 쓰면)이면 재촉 1
    U(H, 2, '규격에 딱 맞는 1초', 0, '스킬', [draw(1), ifTune, hasten(1)], nm([
      Or([draw(1), hasten(1)]),
      Or([draw(2), ifTune, hasten(1)]),
      Or([draw(1), sh(0.45), ifTune, hasten(1)]),
      Or([draw(1), ifTune, ripen(K, 0.3)]),
      Or([draw(1), ifTune, hasten(1), pw('reserveFire', [draw(1)], { limit: 1 })], { power: true }),
    ]), null),
    // 쓰기 — 수많은 시간선: 단일 + 초침을 지금 다 돌린다(남은 1칸당 -25%) — 정각이 울리고 다시 감긴다
    U(H, 3, '수많은 시간선', 1, '공격', [dmg(0.75), ripen(K, 0.25)], [
      'A', 'B',
      Or([dmg(0.6, EA), ripen(K, 0.2)]),
      Or([dmg(0.7), ripen(K, 0.25), srch()]),
      'Hd',
    ], bl('power', 'draw', [hasten(1)])),
    // 갈래 부품 — 오천 년 뒤의 편지(강화): 재촉 1 + 예약이 터지면(아군 누구든) 적 전체
    U(H, 4, '오천 년 뒤의 편지', 1, '강화', [hasten(1), pw('reserveFire', [dmg(0.3, EA)], { when: { who: 'any' }, limit: 1 })], nm([
      Or([hasten(1), pw('reserveFire', [dmg(0.4, EA)], { when: { who: 'any' }, limit: 1 })]),
      Or([pw('reserveFire', [dmg(0.3, EA)], { when: { who: 'any' }, limit: 1 })], { cost: 0 }),
      Or([hasten(1), pw('reserveFire', [draw(1), sh(0.45)], { when: { who: 'any' }, limit: 1 })]),
      Or([srch(), pw('reserveFire', [dmg(0.3, EA)], { when: { who: 'any' }, limit: 1 })]),
      Or([hasten(2), pw('reserveFire', [dmg(0.45, EA)], { when: { who: 'any' }, limit: 1 }), disc(1)]),
    ]), bl('ap', 'cost', [hasten(1)])),
    // 만들기 둘째 — 즐거웠던 기억: 실드 + 버린 더미의 내 고유 카드 1장을 손으로
    U(H, 5, '즐거웠던 기억', 1, '스킬', [sh(0.9), pull({ who: 'self', unique: true })], [
      'A', 'B',
      Or([heal(1.25), pull({ who: 'self', unique: true })]),
      Or([sh(0.9), pull({ who: 'self', unique: true }), pw('reserveFire', [sh(0.4)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('guard', 'draw', [hasten(1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 리스티 — 박자형 · 딜러 · 우울. 한 턴에 공격을 셋 이으면 신상이 다 털려 넷째 공격 「오늘의 POTG」 가 손에(턴 끝 개인정보는 사라진다)
// 원작: 강화 평타 = 넷째(어사이드 뒤 셋째) 공격마다 개인정보 수집 확정 치명 · 저학년 테크노맨시(HP 가장 높은 적을 다시 찾아 침) · 다 마신 캔 · 어사이드 「오늘의 POTG」
// 시동: u2 훗, 못 깨는 게임은 없지(0코 — 스킬 → 공격)
// ════════════════════════════════════════════════════════════════════
function risty(j) {
  const H = '리스티', K = '개인정보', CAN = '리스티_can', POTG = '리스티_potg';
  const h = j.heroes[0];
  h.blurb = '곰인형 속 AI 글러브와 함께하는 해커. 한 턴에 공격을 셋 이으면 적의 신상이 다 털려 넷째 공격 「오늘의 POTG」 를 손에 쥡니다 — 손맛은 턴이 끝나면 식습니다.';
  h.keyword = { name: K, desc: '이번 턴 공격으로 긁어모은 적의 신상', carrier: 'self', cap: 3, endClear: true, onMax: { make: POTG, consume: true } };
  h.passives = [
    pas('글러브 해킹', 'play', [stk(K, 1)], { when: { type: '공격', minCost: 1 }, limit: 3 }),
    pas('천재 해커의 등장', 'make', [gauge(5)], { limit: 2 }),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === K ? stk(K, f.v) : f));
  const tokens = [token(POTG, '오늘의 POTG', H, '공격', [dmg(0.75, TOP), tough(1)], { blurb: '신상을 다 턴 넷째 한 방 — 가장 튼튼한 적부터 정확히' })];
  setCards(j, [
    // 쓰기 — 테크노맨시(2): HP 가장 높은 적 + 개인정보 1개당(최소 1) 다시 찾아 친다
    U(H, 1, '테크노맨시', 2, '공격', [dmg(0.85, TOP), per(K, { n: 1 }), dmg(0.45, TOP)], [
      'A',
      ['D', 'play', [xtra(0.45, TOP)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.6, EA), per(K, { n: 1 }), dmg(0.3, EA)]),
      Or([dmg(0.8, TOP), per(K, { n: 1 }), dmg(0.42, TOP), ifKill, ap(1)]),
      Or([dmg(0.8, TOP), per(K, { n: 1 }), dmg(0.4, TOP), srch({ type: '공격' })]),
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 훗, 못 깨는 게임은 없지(0 · 공격): 소량 + 공격 카드 1장
    U(H, 2, '훗, 못 깨는 게임은 없지', 0, '공격', [dmg(0.45), drawType('공격')], [
      'A', 'B',
      Or([dmg(0.4), make(CAN, 1)]),
      Or([dmg(0.4), stk(K, 1), drawType('공격')]),
      'Hd',
    ], null),
    // 원작 자유 — 다 마신 캔: 단일 + 빈 캔(0코 공격 — 박자 연료)
    U(H, 3, '다 마신 캔', 1, '공격', [dmg(0.75), make(CAN, 1)], [
      'A', 'B',
      Or([dmg(0.65), make(CAN, 2), disc(1)]),
      Or([dmg(0.75), make(CAN, 1), pw('make', [dmg(0.25)], { limit: 1 })], { power: true }),
      Or([dmg(1.1), make(CAN, 1), srch({ type: '공격' })]),
    ], bl('power', 'draw', [make(CAN, 1)])),
    // 갈래 부품 — 글러브(강화): 개인정보 + 개인정보가 둘 이상일 때 공격하면 HP 가장 높은 적에게 추가 공격
    U(H, 4, '글러브', 1, '강화', [stk(K, 1), pw('play', [xtra(0.4, TOP)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 2 }], limit: 1 })], nm([
      Or([stk(K, 1), pw('play', [xtra(0.55, TOP)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 2 }], limit: 1 })]),
      Or([pw('play', [xtra(0.4, TOP)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 2 }], limit: 1 })], { cost: 0 }),
      Or([stk(K, 1), pw('make', [stk(K, 1), xtra(0.25, TOP)], { limit: 1 })]),
      Or([srch({ type: '공격' }), pw('play', [xtra(0.4, TOP)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 2 }], limit: 1 })]),
      Or([stk(K, 2), pw('play', [xtra(0.5, TOP)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 2 }], limit: 1 }), disc(1)]),
    ]), bl('atkUp', 'cost', [stk(K, 1)])),
    // 만들기 — 독타 페퍼 한 박스(0): 빈 캔 둘 + 드로우
    U(H, 5, '독타 페퍼 한 박스', 0, '스킬', [make(CAN, 2), draw(1)], nm([
      Or([make(CAN, 3), draw(1)]),
      Or([make(CAN, 2), drawType('공격', 2)]),
      Or([make(CAN, 2), stk(K, 1), draw(1)]),
      Or([make(CAN, 2), draw(1), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([make(CAN, 3), draw(2), disc(1)]),
    ]), bl('draw', 'ap', [make(CAN, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 6. 마에스트로 2호 — 소멸형 · 탱커 · 광기. 손의 무엇이든 삼켜(소멸) 연료로 — 연료 1개당 주는 실드↑, 다섯이면 다음 카드 강화. 연료를 털어 파티 실드로
// 원작: 무엇이든 연료로 소화(석류석 · 케이크 · 수은, 미숫가루는 고장) · 저학년 로보틱 매트릭스(자신 + 아군 전체 보호막) · 어사이드 방화벽(아군 받는 피해↓) · A3쨩 배터리
// 시동: u2 A3쨩 정비 → u4 무엇이든 삼키기(만들기 칸 · 기본 카드 연료)
// ════════════════════════════════════════════════════════════════════
function maestro(j) {
  const H = '마에스트로2호', K = '삼킨 연료';
  const h = j.heroes[0];
  h.blurb = '온갖 연료로 움직이는 로봇. 손의 무엇이든 삼켜 연료로 바꾸고, 연료가 찰수록 파티를 감싸는 실드가 두꺼워지며, 가득 차면 다음 한 수에 힘을 싣습니다.';
  h.keyword = { name: K, desc: '무엇이든 삼켜 채운 연료', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.04 }, { stat: 'taken', v: -0.03 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('무엇이든 연료로', 'exhaust', [stk(K, 1), sh(0.3)], { when: { who: 'any' }, limit: 3 }),
    pas('자가 회복 기능', 'fightStart', [stk(K, 1), sh(0.6)]),
    pas('자가 회복 기능', 'turnStart', [per(K), sh(0.12)]),
  ];
  setOpener(j, H, 'u2', 'u4');
  setCards(j, [
    // 쓰기 — 로보틱 매트릭스: 파티 실드 + 연료 1개당 실드, 연료 전부 소모
    U(H, 1, '로보틱 매트릭스', 1, '스킬', [sh(0.8), per(K), sh(0.2), spendAll(K)], [
      'A',
      ['D', 'exhaust', [sh(0.3)], { when: { who: 'any' }, limit: 1 }],
      Or([heal(0.9), per(K), heal(0.22), spendAll(K)]),
      Or([sh(0.7), per(K), sh(0.18), srch()]),
      'Hx',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 원작 자유 — A3쨩 정비(개전 강화): 연료 + 실드 + 매 턴 연료
    U(H, 2, 'A3쨩 정비', 1, '강화', [stk(K, 1), sh(0.4), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), sh(0.5), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(1.3), pw('exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 1 })], { tags: ['개전'] }),
      Or([sh(0.6), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), sh(0.7), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), bl('defUp', 'ap', [stk(K, 1)]), ['개전']),
    // 갈래 부품 — 진격 방해 모드: 방어 기반 + 손패 1장 삼키기 + 연료
    U(H, 3, '진격 방해 모드', 1, '공격', [ddef(0.75), burn(1), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.6, EA), burn(1), st('약화', 1, EA)]),
      Or([ddef(0.9), burn(1), pw('exhaust', [ddef(0.3)], { when: { who: 'any' }, limit: 1 })], { power: true }),
      Or([ddef(0.7), stk(K, 1), srch()]),
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 무엇이든 삼키기: 손패 1장 삼키고 연료 2 + 실드
    U(H, 4, '무엇이든 삼키기', 1, '스킬', [burn(1), stk(K, 2), sh(0.7)], nm([
      Or([burn(1), stk(K, 3), sh(0.8)]),
      Or([burn(1), stk(K, 2)], { cost: 0 }),
      Or([burn(2), stk(K, 3), sh(0.6)]),
      Or([burn(1), sh(1.3), pw('exhaust', [stk(K, 1), sh(0.4)], { when: { who: 'any' }, limit: 2 })], { power: true }),
      Or([exile('hand', { n: 1, basic: true }), stk(K, 2), srch()]),
    ]), null),
    // 만들기 둘째 — 척살 모드 전환: 방어 기반 + 연료 1개당(쓰지 않음)
    U(H, 5, '척살 모드 전환', 1, '공격', [ddef(0.6), per(K), ddef(0.2)], [
      'A', 'B',
      Or([sh(1.3), per(K), sh(0.35)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 아멜리아 — 회복형 · 딜러 · 냉정(변환). 파티 HP 80%를 넘긴 회복이 「결재 서류」 로 쌓이고, 서류로 감봉(1개당 1타) — 넷이면 다음 카드 강화. 힐데(넘침 → 파티 실드)와 달리 넘침을 피해로
// 원작: 저학년 새틀라이트 신호탄 → 위성 폭격 + 감전 · 고학년 감전된 적 기절 · 감시 카메라 32대 · 감봉 남발 · 「그냥 승인」
// 시동: u3 모나티엄 행정 대행(0코)
// ════════════════════════════════════════════════════════════════════
function amelia(j) {
  const H = '아멜리아', K = '결재 서류';
  const h = j.heroes[0];
  h.blurb = '시장님 일을 도맡는 자타공인 최고의 비서. 넘치게 받은 회복은 결재 서류로 쌓이고, 서류는 감봉 통보와 위성 폭격으로 처리됩니다.';
  h.keyword = { name: K, desc: '넘치는 일손으로 올라오는 서류', carrier: 'self', cap: 4, onMax: { empower: 'next', ratio: 0.7, consume: true } };
  h.passives = [
    pas('서류 접수', 'overheal', [stk(K, 2)], { when: { pct: 0.7, who: 'any' }, limit: 2 }),
    pas('초고속 썬더 레이저', 'play', [st('충격', 1), xtra(0.35)], { when: { type: '공격' }, limit: 1 }),
  ];
  setCards(j, [
    // 원작 자유 — 새틀라이트 전술폭격: 적 전체 + 감전, 이번 판 HP 80%를 넘긴 회복 30당 광역
    U(H, 1, '새틀라이트 전술폭격', 1, '공격', [dmg(0.65, EA), st('충격', 1, EA), perOver(0.8, 30), dmg(0.1, EA)], [
      'A', 'B',
      Or([dmg(0.6, EA), st('충격', 1, EA), stk(K, 1)]),
      ['D', 'overheal', [dmg(0.25, EA)], { when: { pct: 0.8 }, limit: 1 }],
      Or([dmg(1.0, EA), st('충격', 2, EA), disc(1)]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 감봉 통보: 서류 1개당(최소 1) 1타, 서류 전부 소모
    U(H, 2, '감봉 통보', 1, '공격', [dmg(0.6), per(K, { n: 1 }), dmg(0.4), spendAll(K)], [
      'A',
      ['D', 'overheal', [dmg(0.3)], { when: { pct: 0.8 }, limit: 1 }],
      Or([dmg(0.5, EA), per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),
      Or([dmg(0.55), per(K, { n: 1 }), dmg(0.38), st('충격', 1)]),
      'Hx',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 시동 — 모나티엄 행정 대행(0): 회복 + 서류 + 드로우
    U(H, 3, '모나티엄 행정 대행', 0, '스킬', [heal(0.45), stk(K, 1), draw(1)], nm([
      Or([heal(0.65), stk(K, 1), draw(1)]),
      Or([heal(0.4), stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([heal(0.95), perOver(0.8, 20), dmg(0.15), stk(K, 1)]),
      Or([heal(1.15), stk(K, 1), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(1.2), stk(K, 2), disc(1)]),
    ]), null),
    // 갈래 부품 — 감시카메라 32대(강화): 서류 + HP 80%를 넘긴 회복의 60%를 적 1명에게 고정 피해로(턴 1회)
    U(H, 4, '감시카메라 32대', 1, '강화', [stk(K, 1), pw('overheal', [{ k: 'dmg', ratio: 1, ofEvent: 1.0, target: E1 }], { when: { pct: 0.8 }, limit: 1 })], nm([
      Or([stk(K, 1), sh(0.5), pw('overheal', [{ k: 'dmg', ratio: 1, ofEvent: 1.3, target: E1 }], { when: { pct: 0.8 }, limit: 1 })]),
      Or([pw('overheal', [{ k: 'dmg', ratio: 1, ofEvent: 0.8, target: E1 }], { when: { pct: 0.8 }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 1), sh(0.5), pw('play', [st('충격', 1)], { when: { who: 'other', type: '공격' }, limit: 1 })]),
      Or([srch(), sh(0.4), pw('overheal', [{ k: 'dmg', ratio: 1, ofEvent: 1.0, target: E1 }], { when: { pct: 0.8 }, limit: 1 })]),
      Or([stk(K, 2), sh(0.4), pw('overheal', [{ k: 'dmg', ratio: 1, ofEvent: 1.5, target: E1 }], { when: { pct: 0.8 }, limit: 1 })]),
    ]), bl('atkUp', 'draw', [stk(K, 1)])),
    // 만들기 둘째 — 그냥 승인(0): 회복 + 동료의 손 카드 1장 비용 -1
    U(H, 5, '그냥 승인', 0, '스킬', [heal(0.4), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })], [
      'A', 'B',
      Or([heal(0.4), stk(K, 1), draw(1)]),
      ['D', 'overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 }],
      Or([heal(0.8), cs('비용', -1, { to: 'hand', n: 2, who: 'other' }), disc(1)]),
    ], bl('heal', 'cost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 아멜리아(R41) — 소멸형 · 서포터 · 우울. 시제품을 쏘고 지우며(소멸) 시험 가동 셋이면 완성품 — 지운 것이 쌓일수록 전자포가 세지고, 지운 고유 카드 하나를 되찾는다
// 원작: 차원 소멸 작전 → 지운 엘레나를 잃고 「되찾기」(어사이드 나의 엘레나) · 저학년 치료 드론(넘친 회복 → 보호막) · 고학년 정화 + 전자포
// 아멜리아와 가르기: 아멜리아 = 넘친 회복 → 서류 → 피해(딜러), R41 = 시제품 소멸 → 소멸 더미 → 전자포 · 되살리기(서포터)
// 시동: u3 영광인 줄 알거라(개전 강화 — 그대로)
// ════════════════════════════════════════════════════════════════════
function ameliaR41(j) {
  const H = '아멜리아_R41', K = '시험 가동', T1 = '아멜리아_R41_t1', T2 = '아멜리아_R41_t2';
  const h = j.heroes[0];
  h.blurb = '제 손으로 만든 병기가 통제를 벗어난 「이 몸」. 시제품을 쏘고 지우며 시험 가동이 셋 쌓이면 완성품이 나오고, 지운 것 가운데 하나는 끝내 되찾아 옵니다.';
  h.keyword = { name: K, desc: '시제품을 쏴 본 횟수', carrier: 'self', cap: 3, onMax: { make: T2, consume: true } };
  h.passives = [
    pas('이 몸의 시제품', 'play', [make(T1, 1)], { limit: 2 }),
    pas('차원 소멸 작전', 'exhaust', [draw(1)], { limit: 1 }),
  ];
  const t1 = j.cards.find(c => c.id === T1);
  const tokens = [{ ...t1, fx: [dmg(1.2), stk(K, 1), { k: 'ifRandom', pct: 0.25 }, { k: 'payHpPct', v: 0.03 }] }];
  setCards(j, [
    // 원작 자유 — 엘렌-A 오버차지: 회복 + HP 80%를 넘긴 회복 25당 실드 + 시험 가동
    U(H, 1, '엘렌-A 오버차지', 1, '스킬', [heal(0.9), perOver(0.8, 25), sh(0.2), stk(K, 1)], [
      'A', 'B',
      Or([heal(0.8), { k: 'cleanse', v: 1 }, stk(K, 1)]),
      Or([heal(0.9), stk(K, 1), pw('overheal', [sh(0.5)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(1.3), stk(K, 2), disc(1)]),
    ], bl('heal', 'defUp', [stk(K, 1)])),
    // 만들기 — 나노 수복 드론: 시제품 둘 + 드로우
    U(H, 2, '나노 수복 드론', 1, '스킬', [make(T1, 2), draw(1)], nm([
      Or([make(T1, 3), draw(1)]),
      Or([make(T1, 1), draw(1)], { cost: 0 }),
      Or([make(T1, 2), draw(1), heal(0.8)]),
      Or([make(T1, 2), draw(1), pw('exhaust', [heal(1.0)], { limit: 1 })], { power: true }),
      Or([make(T1, 3), draw(2), disc(1)]),
    ]), bl('draw', 'ap', [make(T1, 1)])),
    // 시동 — 영광인 줄 알거라(개전 강화): 결정화 + 매 턴 시제품
    U(H, 3, '영광인 줄 알거라', 1, '강화', [st('결정화', 1), pw('turnStart', [make(T1, 1)])], nm([
      Or([st('결정화', 1), sh(0.7), pw('turnStart', [make(T1, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [make(T1, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('결정화', 1), sh(0.4), L.power(L.rule('turnStart', [make(T1, 1)]), L.rule('exhaust', [stk(K, 1)], { limit: 1 }))], { tags: ['개전'] }),
      Or([st('결정화', 1), srch(), pw('turnStart', [make(T1, 1)])], { tags: ['개전'] }),
      Or([st('결정화', 2), pw('turnStart', [make(T1, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 쓰기 — 소형 전자포: 적 전체 + 이번 전투에 소멸한 카드 1장당 광역
    U(H, 4, '소형 전자포', 1, '공격', [dmg(0.55, EA), perGone(), dmg(0.07, EA)], [
      'A',
      ['D', 'exhaust', [dmg(0.35, ER)], { limit: 1 }],
      Or([dmg(0.9), perTag(T1), dmg(0.35), exile('hand', { all: true, tag: T1 })]),
      Or([dmg(0.5, EA), perGone(), dmg(0.06, EA), srch()]),
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 나의 엘레나(소멸): 소멸 더미의 고유 카드 1장을 손으로(사도마다 턴 1회) + 회복
    U(H, 5, '나의 엘레나', 1, '스킬', [pull({ from: 'gone', unique: true }), heal(0.6)], nm([
      Or([pull({ from: 'gone', unique: true }), heal(0.85)], { tags: ['소멸'] }),
      Or([pull({ from: 'gone', unique: true }), heal(0.4)], { cost: 0, tags: ['소멸'] }),
      Or([pull({ from: 'gone', unique: true }), stk(K, 2)], { tags: ['소멸'] }),
      Or([pull({ from: 'gone', unique: true }), heal(0.5), srch()], { tags: ['소멸'] }),
      Or([pull({ from: 'gone', n: 2, unique: true }), heal(1.1), nextAp(-1)], { tags: ['소멸'] }),
    ]), bl('heal', 'guard', [make(T1, 1)]), ['소멸']),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 9. 아이시아 — 돈형 · 딜러 · 냉정. 공격 · 처치로 회사 금고를 채우고(벌이), 금고를 털어 신상 냉장고를 손에 찍어 낸다(사기) — 돈다발로 패거나(해고야!) 냉장고로 굴리거나. 쓰면 본의 아니게 파티가 낫는다
// 원작: 저학년 신제품 시연(냉장고 4회 범위) · 어사이드 미공개 신제품(아이스박스) · 「너 해고야」 · 돈다발로 때림 · 몽구스형 단타 · 착각물(악행이 선행으로)
// 판 골드는 건드리지 않는다(BRIEF 돈형 규칙) — 전투 안 금고만.
// 시동: u2 내가 누군지 몰라?(0코)
// ════════════════════════════════════════════════════════════════════
function icia(j) {
  const H = '아이시아', K = '회사 금고', OLD = '선행 장부', FR = '아이시아_fridge';
  const h = j.heroes[0];
  h.blurb = '악덕 CEO 행세가 번번이 남 좋은 일로 끝나는 회장님. 치고 쓰러뜨려 회사 금고를 채우고, 금고를 털어 신상 냉장고를 찍어 내면 — 어째선지 파티가 낫습니다.';
  h.keyword = { name: K, desc: '손해 보기 싫어 꽉 채워 두는 회사 금고', carrier: 'self', cap: 6, onMax: { make: FR, n: 2, consume: true } };
  h.passives = [
    pas('냉혹한 경영', 'kill', [stk(K, 2)], { when: { mine: true }, limit: 1 }),
    pas('냉혹한 경영', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    pas('의도치 않은 선행', 'make', [heal(0.35)], { limit: 1 }),
  ];
  renameKw(h, OLD, K);
  const fr = j.cards.find(c => c.id === FR);
  const tokens = [{ ...fr, fx: [dmg(0.12, EA, { hits: 4 })] }];
  setCards(j, [
    // 쓰기(사기) — 신제품 시연: 3회 광역. 금고 3 이상이면 3을 털어 신상 냉장고 둘
    U(H, 1, '신제품 시연', 1, '공격', [dmg(0.18, EA, { hits: 3 }), ifStack(K, 3), spendN(K, 3), make(FR, 2)], [
      'A',
      Or([dmg(0.3, EA, { hits: 3 }), make(FR, 1), pw('make', [dmg(0.22, EA)], { limit: 1 })], { power: true }),
      Or([dmg(0.5, E1, { hits: 3 }), ifStack(K, 3), spendN(K, 3), make(FR, 2)]),
      Or([sh(2.0), ifStack(K, 3), spendN(K, 3), make(FR, 2)]),
      Or([dmg(0.45, EA, { hits: 3 }), ifStack(K, 2), spendN(K, 2), make(FR, 1)]),
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 내가 누군지 몰라?(0): 금고 2 + 드로우
    U(H, 2, '내가 누군지 몰라?', 0, '스킬', [stk(K, 2), draw(1)], nm([
      Or([stk(K, 3), draw(1)]),
      Or([stk(K, 2), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 2), make(FR, 1)]),
      Or([stk(K, 2), sh(1.1)]),
      Or([stk(K, 4), draw(1), disc(1)]),
    ]), null),
    // 원작 자유 — 해고야!: 단일 + 금고 1개당(돈다발), 금고 전부 소모
    U(H, 3, '해고야!', 1, '공격', [dmg(0.85), per(K), dmg(0.18), spendAll(K)], [
      'A',
      ['D', 'kill', [stk(K, 1)], { limit: 1 }],
      Or([dmg(0.8), per(K), dmg(0.17), st('약화', 1)]),
      Or([dmg(0.8), per(K), dmg(0.16), srch()]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 만들기 둘째 — 정복, 멜루나!: 단일 + 손의 신상 냉장고 1장당(쥘까 굴릴까)
    U(H, 4, '정복, 멜루나!', 1, '공격', [dmg(0.75), perTag(FR), dmg(0.3)], [
      'A', 'B',
      Or([dmg(1.05), ifKill, stk(K, 3)]),
      Or([dmg(0.75), perTag(FR), dmg(0.3), pw('make', [stk(K, 1)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('atkUp', 'ap', [make(FR, 1)])),
    // 갈래 부품 — 아이스크림 수레(재고 판매): 실드 + 금고 2 + 공격 카드 1장
    U(H, 5, '아이스크림 수레', 1, '스킬', [sh(0.8), stk(K, 2), drawType('공격')], [
      'A', 'B',
      Or([heal(0.6), stk(K, 2), make(FR, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([sh(1.5), stk(K, 3), disc(1)]),
    ], bl('guard', 'draw', [make(FR, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 10. 알레트 — 거드는 형 · 탱커 · 순수(1성). 「반장님」 으로 찍은 아군이 카드를 내야 명령이 내려온다 — 명령 셋이면 파티의 다음 카드 강화(명령 완수). 반장 없이 낸 알레트 카드는 약하다
// 원작: 칸나 반장 명령이면 무엇이든 · 저학년 삽치기(기절) · 고학년 자기 보호막 · 「오래 멍때리기」 수련(자세 유지)
// 시동: u2 막내 하사 돌격(그대로)
// ════════════════════════════════════════════════════════════════════
function alette(j) {
  const H = '알레트', K1 = '반장님', K2 = '명령 수행';
  const h = j.heroes[0];
  h.blurb = '칸나 반장 말만 듣는 충견형 하사. 반장님이 카드를 낼 때마다 그 앞을 방패로 막으며 명령을 받고, 명령 셋을 완수하면 파티의 다음 한 수에 힘을 싣습니다.';
  const k2 = h.keywords.find(k => k.name === K2);
  delete k2.rules;
  k2.per = [{ stat: 'taken', v: -0.04 }];
  k2.onMax = { empower: 'any', ratio: 0.4, consume: true };
  h.passives = [
    pas('보고드립니다!', 'fightStart', [stk(K1, 1, 'hero:칸나')]),
    pas('반장님 명령이라면', 'play', [sh(0.5), stk(K2, 1)], { when: { marked: K1 }, limit: 3 }),
  ];
  setCards(j, [
    // 원작 자유 — 삽치기: 방어 기반 + 명령, 명령이 셋이면 기절
    U(H, 1, '삽치기', 1, '공격', [ddef(0.7), stk(K2, 1), ifStack(K2, 3), st('기절', 1)], [
      'A', 'B',
      Or([ddef(1.3), st('약화', 2)]),
      Or([ddef(1.0), stk(K2, 1), pw('play', [stk(K2, 1), sh(0.2)], { when: { marked: K1 }, limit: 1 })], { power: true }),
      Or([ddef(1.4), stk(K2, 2), disc(1)]),
    ], bl('power', 'weakSpot', [stk(K2, 1)])),
    // 시동 — 막내 하사 돌격: 반장님 지정 + 실드 + 명령
    U(H, 2, '막내 하사 돌격', 1, '스킬', [stk(K1, 1, ALLY), sh(0.8), stk(K2, 1)], nm([
      Or([stk(K1, 1, ALLY), sh(1.1), stk(K2, 1)]),
      Or([stk(K1, 1, ALLY), sh(0.5)], { cost: 0 }),
      Or([stk(K1, 1, ALLY), stk(K2, 2), draw(1, { who: 'other' })]),
      Or([stk(K1, 1, ALLY), sh(0.9), pw('play', [stk(K2, 1)], { when: { marked: K1 }, limit: 1 })], { power: true }),
      Or([stk(K1, 1, ALLY), sh(1.7), disc(1)]),
    ]), null),
    // 쓰기 — 방패로 밀어붙이기: 명령 1개당 방어 기반, 명령 전부 소모(셋을 기다려 파티 강화를 줄지)
    U(H, 3, '방패로 밀어붙이기', 1, '공격', [ddef(0.5), per(K2), ddef(0.3), spendAll(K2)], [
      'A',
      ['D', 'play', [ddef(0.2)], { when: { marked: K1 }, limit: 1 }],
      Or([ddef(0.45, EA), per(K2), ddef(0.22, EA), spendAll(K2)]),
      Or([ddef(0.5), per(K2), ddef(0.3), stk(K1, 1, ALLY)]),
      'Hx',
    ], bl('power', 'cost', [stk(K2, 1)])),
    // 갈래 부품 — 자율 훈련: 실드 + 명령 1개당 실드(쓰지 않음). 강화화 갈래 = 멍때리기 수련(제 카드를 안 낸 턴 끝 실드)
    U(H, 4, '자율 훈련', 1, '스킬', [sh(0.7), per(K2), sh(0.3)], [
      'A', 'B',
      Or([sh(0.6), per(K2), sh(0.25), stk(K1, 1, ALLY)]),
      ['D', 'turnEnd', [sh(0.4)], { conds: [{ c: 'ownNone' }] }],
      'Hn',
    ], bl('guard', 'draw', [stk(K2, 1)])),
    // 만들기 둘째 — 반장님 주스 빼돌리기: 실드 + 명령 + 동료 카드 1장 뽑기
    U(H, 5, '반장님 주스 빼돌리기', 1, '스킬', [sh(0.9), stk(K2, 1), draw(1, { who: 'other' })], [
      'A', 'B',
      Or([heal(1.25), stk(K2, 1), draw(1, { who: 'other' })]),
      Or([sh(1.5), stk(K2, 1), pw('play', [sh(0.3)], { when: { marked: K1 }, limit: 1 })], { power: true }),
      Or([sh(1.3), stk(K2, 2), disc(1)]),
    ], bl('heal', 'ap', [stk(K2, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 11. 엘레나 — 예약형 · 딜러 · 냉정. 냥이 드론을 띄워 두고(턴 끝마다 펄스) 자폭 타이머를 건다 — 타이머가 다 닳으면 드론 1개당 적 전체 + 감전, 드론 전부 터짐. 당겨 쓰면 남은 칸만큼 약하다
// 원작: 발명품마다 쓸데없는 자폭 기능 · 저학년 전술 드론 MK-2(펄스 범위 + 감전) · 고학년 D-CAT(특수 드론 6회 펄스 후 폭파) · 고양이 · 커피
// 리뉴아(시간 그 자체 — 정각마다 다시 감김)와 가르기: 엘레나는 모은 드론을 「언제」 한꺼번에 터뜨릴지
// 시동: u2 냥이 드론 출격(개전 강화 — 그대로)
// ════════════════════════════════════════════════════════════════════
function elena(j) {
  const H = '엘레나', D = '냥이 드론', T = '자폭 타이머';
  const h = j.heroes[0];
  h.blurb = '모든 발명품에 자폭 기능을 다는 괴짜 시장. 띄워 둔 냥이 드론이 턴 끝마다 펄스를 쏘고, 자폭 타이머가 다 닳으면 드론이 한꺼번에 터집니다.';
  h.keyword = { name: D, desc: '자폭 기능이 달린 고양이 드론', carrier: 'self', cap: 3, per: [{ stat: 'dot', ratio: 0.6 }] };
  h.keywords = [{
    name: T, desc: '드론이 터지기까지 남은 시간', carrier: 'self', cap: 2, reserve: true, decay: 1,
    rules: [{ name: '쓸데없는 자폭', when: { on: 'stackGone', id: T, decay: true }, fx: [per(D, { n: 1 }), dmg(0.45, EA), spendAll(D), st('충격', 1, EA)] }],
  }];
  h.passives = [
    pas('드론 출격', 'play', [stk(D, 1)], { limit: 3 }),
    pas('다른 버그로 고치기', 'reserveFire', [draw(1)], { when: { who: 'any' }, limit: 1 }),
  ];
  setCards(j, [
    // 원작 자유 — 전술 드론 MK-2: 적 전체 + 감전
    U(H, 1, '전술 드론 MK-2', 1, '공격', [dmg(0.8, EA), st('충격', 1, EA)], [
      'A', 'B',
      Or([dmg(0.85, EA), stk(D, 1)]),
      ['D', 'play', [st('충격', 1)], { when: { type: '스킬' }, limit: 1 }],
      'Hn',
    ], bl('power', 'cost', [stk(D, 1)])),
    // 시동 — 냥이 드론 출격(개전 강화): 드론 + 매 턴 드론
    U(H, 2, '냥이 드론 출격', 1, '강화', [stk(D, 1), pw('turnStart', [stk(D, 1)])], nm([
      Or([stk(D, 2), pw('turnStart', [stk(D, 1)])], { tags: ['개전'] }),
      Or([stk(D, 1), pw('turnStart', [stk(D, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(D, 1), stk(T, 2), pw('reserveFire', [stk(D, 2)], { limit: 1 })], { tags: ['개전'] }),
      Or([stk(D, 1), srch(), pw('turnStart', [stk(D, 1)])], { tags: ['개전'] }),
      Or([stk(D, 3), pw('turnStart', [stk(D, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 쓰기 — 쓸데없는 자폭 기능: 적 전체 + 자폭 타이머 2(두 판 뒤 드론 전부 자폭)
    U(H, 3, '쓸데없는 자폭 기능', 1, '공격', [dmg(0.55, EA), stk(T, 2)], [
      'A', 'B',
      Or([dmg(0.5, EA), ripen(T, 0.25)]),
      Or([dmg(0.55, EA), stk(T, 2), pw('reserveFire', [stk(D, 1)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'ap', [stk(D, 1)])),
    // 만들기 둘째 — 엘프답게!: 단일 + 드론 1개당(쓰지 않음)
    U(H, 4, '엘프답게!', 1, '공격', [dmg(0.6), per(D), dmg(0.3)], [
      'A',
      ['C', [stk(D, 1)]],
      Or([dmg(0.5, EA), per(D), dmg(0.2, EA)]),
      ['D', 'turnStart', [stk(D, 1)]],
      Or([dmg(0.55), per(D), dmg(0.28), srch()]),
    ], bl('power', 'weakSpot', [stk(T, 1)])),
    // 갈래 부품 — 아메리카노 주말농장 에디션: 실드 + 재촉 1(파티의 모든 예약) + 드론
    U(H, 5, '아메리카노 주말농장 에디션', 1, '스킬', [sh(0.7), hasten(1), stk(D, 1)], [
      'A', 'B',
      Or([sh(0.6), ripen(T, 0.3), draw(1)]),
      ['D', 'turnStart', [hasten(1)], { limit: 1 }],
      Or([sh(1.0), hasten(2), disc(1)]),
    ], bl('guard', 'draw', [stk(D, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '엘프/레이지': 1.3, '엘프/로네': 0.75, '엘프/로네_시장': 1.2, '엘프/리뉴아': 1.2, '엘프/리스티': 0.8, '엘프/마에스트로2호': 1.4, '엘프/아멜리아': 1.3, '엘프/아이시아': 1.4, '엘프/알레트': 1.3, '엘프/엘레나': 1.4 };
// ── 돌리기 ──
const JOBS = [
  ['엘프/레이지', raizy], ['엘프/로네', rone], ['엘프/로네_시장', roneMayor], ['엘프/리뉴아', renewa], ['엘프/리스티', risty], ['엘프/마에스트로2호', maestro],
  ['엘프/아멜리아', amelia], ['엘프/아멜리아_R41', ameliaR41], ['엘프/아이시아', icia], ['엘프/알레트', alette], ['엘프/엘레나', elena],
];
L.run18(JOBS, TUNE, new URL('./boost_엘프A_18.json', import.meta.url));
