// 사도 리워크 시범 6명(2026-10-07) — 카제나식 「고유 장치 1개 + 카드 4장 역할 분담 + 신탁 5갈래가 서로 다른 칸」.
// 설계 노트: _measure/사도_리워크_시범.md · 지침: _measure/사도_리워크_지침.md · 공용 부품: lib.mjs
// node _gen/rework/rework.mjs   → heroes/<종족>/<사도>.json 덮어쓰기(백업에서 읽음)
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifAll, ifBroken, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ════════════════════════════════════════════════════════════════════
// 1. 루드 — 탱커 · 용족 · 활발. 「렙」 넷마다 포즈(원작: 네 번째 평타마다 포즈 → 받는 피해 감소 뒤 회복)
// ════════════════════════════════════════════════════════════════════
function rude(j) {
  const H = '루드', R = '렙';
  const h = j.heroes[0];
  h.keyword = {
    name: R, desc: '쉬지 않고 세는 반복 횟수', carrier: 'self', cap: 4,
    rules: [{ name: '포즈', when: { on: 'stackReach', id: R, n: 4 }, fx: [spendAll(R), st('피해 감소', 2), heal(0.5)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '단백질 보충', when: { on: 'play' }, fx: [stk(R, 1)] },
    { name: '크로스핏 선발대', when: { on: 'fightStart' }, fx: [stk(R, 2)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(R, 3) : f));
  const cards = [
    // u1 축 열기(광역 · 약화 담당) — 원작 저학년 「한 세트 더!」 기합
    card(H, 1, '한 세트 더!', 1, '공격', [ddef(0.5, EA), st('약화', 1, EA), stk(R, 1)], [
      O('쩌렁쩌렁 기합', [ddef(0.65, EA), st('약화', 1, EA), stk(R, 1)]),
      O('세트 한가운데', [ddef(0.45, EA), st('약화', 1, EA), ifStack(R, 3), ddef(0.45, EA)]),
      O('루드 짐 개장', [ddef(0.45, EA), st('약화', 1, EA), power(rule('stackReach', [ddef(0.45, EA)], { when: { id: R, n: 4 } }))], { power: true }),
      O('마지막 세트', [ddef(1.0, EA), st('약화', 2, EA)], { tags: ['종극'] }),
      O('단체 PT', [ddef(0.3, EA), per(R), ddef(0.2, EA), st('약화', 1, EA)]),
    ], [B('루비 아령', 'power'), B('소음 공해', 'ap'), B('구령 맞추기', [stk(R, 1)])]),
    // u2 굴리기(0코 렙 + 드로우) — 시동 카드. 3단계: 기본 카드 연료(「기초 체력」 시작 카드 서치) · 재설계(곱빼기 — 드로우 대신 실드)
    card(H, 2, '미숫가루 프로틴', 0, '스킬', [stk(R, 1), draw(1)], [
      O('곱빼기 스쿱', [stk(R, 2), sh(0.5)]),
      O('루드의 운동 교본', [draw(1), power(rule('turnStart', [stk(R, 1)]))], { power: true }),
      O('기본기 다지기', [stk(R, 1), draw(2, { basic: true })]),
      O('쉐이커에 담아 두기', [stk(R, 1), draw(1), sh(0.6)], { tags: ['보존'] }),
      O('아침 공복 한 잔', [stk(R, 1), draw(1), inspire, stk(R, 2)]),
    ], [B('쉐이커', 'draw'), B('탄수화물 금지', { tags: ['보존'] }), B('단백질 바', [sh(0.4)])]),
    // u3 버티기(렙 수만큼 실드) — 2코 → 1코. 3단계: 비용↑ 갈래는 「바로 포즈」(장치 한 단계) · 범용 상태 덤(불굴) 뺌
    card(H, 3, '다야 님을 지켜라', 1, '스킬', [sh(1.4), per(R), sh(0.35)], [
      O('근육 방패', [sh(1.6), per(R), sh(0.4)]),
      O('용족 2인자의 의무', [sh(2.2), stk(R, 4)], { cost: 2 }),
      O('쏟아붓는 펌핑', [per(R), sh(1.0), spendAll(R)]),
      O('약한 용족들을 위해', [sh(1.2), per(R), sh(0.3), ifWounded, heal(1.2)]),
      O('근손실 방지', [sh(1.0), power(rule('turnEnd', [per(R), sh(0.25)]))], { power: true }),
    ], [B('수장 경호', 'guard'), B('패배는 인정', [st('피해 감소', 1)]), B('루비 이두근', 'cost')]),
    // u4 터뜨리기(렙을 다 써서 한 방 — 포즈와 맞바꿈) = ④ 1코 마무리. 3단계: 재도전은 「치고 바로 포즈」 · 악우의 주먹은 고유 카드 서치
    card(H, 4, '실피르보다 세게', 1, '공격', [ddef(0.7), per(R), ddef(0.3), spendAll(R)], [
      O('마지막 한 개', [ddef(0.85), per(R), ddef(0.3), spendAll(R)]),
      O('재도전', [ddef(1.4), per(R), ddef(0.4), stk(R, 4)], { cost: 2 }),
      O('노는 셈 치고', [ddef(0.7), per(R), ddef(0.2), sh(0.6)]),
      O('악우의 주먹', [per(R), ddef(0.45), spendAll(R), draw(1, { who: 'self', unique: true })], { tags: ['분쇄'] }),
      O('날개는 장식', [ddef(0.45, EA), per(R), ddef(0.2, EA), spendAll(R)]),
    ], [B('정면 승부', 'power'), B('하체 운동', 'frost'), B('근육 기억', [stk(R, 1)])]),
  ];
  j.cards = [...j.cards.filter(c => !c.unique), ...cards];
  // u5 유틸(0코 공격) — 황금 덤벨: 황금 왕관의 주인이 드는 덤벨 — 작은 피해 · 렙 · 드로우
  j.cards.push(card(H, 5, '황금 덤벨', 0, '공격', [ddef(0.35), stk(R, 1)], [
    O('신탁 1', [ddef(0.45), stk(R, 1)]),
    O('신탁 2', [ddef(0.3), stk(R, 1), draw(1, { who: 'self', unique: true })]),
    O('신탁 3', [ddef(0.27, EA), stk(R, 1)]),
    O('신탁 4', [ddef(0.25), stk(R, 1), power(rule('play', [sh(0.3)], { when: { type: '공격' }, limit: 2 }))], { power: true }),
    O('신탁 5', [{ k: 'discard', v: 1 }, ddef(0.35), stk(R, 3)]),
  ], [B('축복 1', 'weakSpot'), B('축복 2', 'draw'), B('축복 3', [stk(R, 1)])]));
  starter(j, '루드_u2');
  // 애착 장비(희귀) — 범용 몫은 장비 지침 §2-2 희귀(싸움당 1코 카드 1~1.5장), 애착 몫은 「렙」 축
  for (const e of j.equips || []) {
    e.effect = [{ name: '쉬는 것도 운동', when: { on: 'turnEnd' }, conds: [{ c: 'playedMin', n: 3 }], fx: [sh(0.8)] }];
    e.affinityEffect = [{ name: '포즈 뒤 스트레칭', when: { on: 'stackReach', id: R, n: 4 }, fx: [heal(0.5)] }];
  }
}

// ════════════════════════════════════════════════════════════════════
// 2. 에르핀 — 딜러 · 요정 · 순수. 케이크를 만들어 먹거나(배부름 → 맨주먹) 탄알로 쏟거나(마력탄 폭주)
// ════════════════════════════════════════════════════════════════════
function erpin(j) {
  const H = '에르핀', K = '배부름', CAKE = '에르핀_cake';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '케이크로 채운 여왕의 배', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.05 }] };
  delete h.keywords;
  h.passives = [
    { name: '달달한 게 최고야!', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [make(CAKE, 1)] },
    { name: '무전취식', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [make(CAKE, 2)] },
  ];
  const cake = j.cards.find(c => c.id === CAKE);
  Object.assign(cake, { cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.4), stk(K, 1)] });
  const cards = [
    // u1 터뜨리기 ① — 손의 케이크를 탄알로(원작 저학년 마력탄 폭주 · 그림 짝 = 저학년 아이콘)
    // 3단계: 「그건 아까잖아」 — 케이크 대신 배부름을 센다(재설계) · 백성들이여! = 광역(비용↑ 갈래는 숫자만 커서 뺌) · 사교 적성은 케이크를 남기고 하나 더
    card(H, 1, '마력탄 폭주', 1, '공격', [hits(3, 0.45), perTag(CAKE), dmg(0.55, ER), { k: 'exileFrom', from: 'hand', all: true, tag: CAKE }], [
      O('마력 대방출', [hits(4, 0.45), perTag(CAKE), dmg(0.5, ER), { k: 'exileFrom', from: 'hand', all: true, tag: CAKE }]),
      O('그건 아까잖아', [hits(3, 0.5), per(K), dmg(0.35, ER)]),
      O('백성들이여!', [dmg(0.55, EA), perTag(CAKE), dmg(0.35, EA), { k: 'exileFrom', from: 'hand', all: true, tag: CAKE }]),
      O('사교 적성', [hits(3, 0.4), perTag(CAKE), dmg(0.45, ER), make(CAKE, 1)], { tags: ['약점 공격'] }),
      O('와구와구 발사!!', [hits(3, 0.4), perTag(CAKE), dmg(0.45, ER), power(rule('exhaust', [dmg(0.5, ER)]))], { power: true }),
    ], [B('아이스크림 케이크', 'power'), B('숨 쉬고 먹고 자고', 'draw'), B('쫀득한 초코 층', [make(CAKE, 1)])]),
    // u2 축 열기 — 케이크 생성(시동 카드). 3단계: 열두 끼 정식 = 손패 1장을 버리고 케이크 셋(대가 · 재설계)
    card(H, 2, '무한의 케이크', 1, '스킬', [make(CAKE, 2), draw(1)], [
      O('열두 끼 정식', [make(CAKE, 3), { k: 'discard', v: 1 }]),
      O('한 입만', [make(CAKE, 2)], { cost: 0 }),
      O('친구 몰래 한 조각', [make(CAKE, 1), draw(1), power(rule('turnStart', [make(CAKE, 1)]))], { power: true }),
      O('먹어도 먹어도 다시', [make(CAKE, 2), draw(1), inspire, make(CAKE, 2)]),
      O('친구 몫까지', [make(CAKE, 2), draw(2, { who: 'other' })]),
    ], [B('당 충전', 'ap'), B('포크 하나 더', [make(CAKE, 1)]), B('배고파서 서두름', 'cost')]),
    // u3 굴리기 — 동료 뒤에서 던지는 연계(원작: 후열 아군 피해↑ · 받는 피해↓). 3단계: 연계 ×0.7 · 진심 전력 = 고유 카드 서치 · 범용 상태 덤 → 실드
    card(H, 3, '순수 케이크 공격!!!', 1, '공격', [dmg(0.85), make(CAKE, 1)], [
      O('진심 전력!!!', [dmg(0.9), make(CAKE, 1), draw(1, { who: 'self', unique: true })], { tags: ['연계'] }),
      O('케이크 폭탄!!!', [dmg(0.6, EA), make(CAKE, 1)], { tags: ['연계'] }),
      O('크림 방패', [dmg(0.75), make(CAKE, 1), sh(0.6)], { tags: ['연계'] }),
      O('케이크 하나 더!', [dmg(0.9), ifKill, make(CAKE, 2), ap(1)], { tags: ['연계'] }),
      O('혼자 다 먹기', [dmg(1.5), make(CAKE, 2)]),
    ], [B('설탕 듬뿍', 'power'), B('휘핑크림', 'weakSpot'), B('한 조각 더', 'draw')], { tags: ['연계'] }),
    // u4 터뜨리기 ② — 배부름을 다 쏟는 맨주먹(완성형 · 2코 하나 — 숫자 큰 카드라 축복에 AP).
    // 3단계: 진심 펀치 = 배부름을 남기고 케이크 둘(생성 · 재설계) · 한 방 올인 = 손의 케이크를 다 태우고 소멸(대가)
    card(H, 4, '맨주먹 결계 부수기', 2, '공격', [dmg(1.8), per(K), dmg(0.45), spendAll(K)], [
      O('진심 펀치', [dmg(1.9), per(K), dmg(0.4), make(CAKE, 2)], { tags: ['분쇄'] }),
      O('결계째 박살', [dmg(1.3, EA), per(K), dmg(0.3, EA), spendAll(K)], { tags: ['분쇄'] }),
      O('밥값은 했어', [dmg(1.8), per(K), dmg(0.4), ifKill, ap(1)], { tags: ['분쇄'] }),
      O('한 방 올인', [dmg(4.4), per(K), dmg(0.8), { k: 'exileFrom', from: 'hand', all: true, tag: CAKE }], { tags: ['분쇄', '소멸'] }),
      O('결계 툭툭', [dmg(1.5), per(K), dmg(0.4), spendAll(K)], { cost: 1, tags: ['분쇄'] }),
    ], [B('주먹에 마력', 'power'), B('금 간 결계', 'weakSpot'), B('밥 먹고 힘내기', 'ap')], { tags: ['분쇄'] }),
  ];
  j.cards = [...j.cards.filter(c => !c.unique), ...cards];
  // u5 유틸(0코 스킬) — 깡총이 파자마(토끼인 척하는 잠옷 차림): 케이크 · 실드 · 공격 카드 드로우(공격 셋이라 스킬 쪽)
  const atkDraw = draw(1, { who: 'self', type: '공격' });
  j.cards.push(card(H, 5, '깡총이 파자마', 0, '스킬', [make(CAKE, 1), sh(0.3), atkDraw], [
    O('신탁 1', [make(CAKE, 1), sh(0.6), atkDraw]),
    O('신탁 2', [make(CAKE, 1), sh(0.45), atkDraw], { tags: ['보존'] }),
    O('신탁 3', [stk(K, 3), heal(0.6), atkDraw]),
    O('신탁 4', [make(CAKE, 1), sh(0.7), power(rule('exhaust', [stk(K, 1), sh(0.4)], { limit: 2 }))], { power: true }),
    O('신탁 5', [{ k: 'discard', v: 1 }, make(CAKE, 2), sh(0.6)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'heal'), B('축복 3', [make(CAKE, 1)])]));
  starter(j, '에르핀_u2');
  // 애착 장비(전설) — 범용 몫(다치지 않은 날 게이지)은 그대로, 애착 몫은 케이크 축: 전투 시작에 케이크 2장
  for (const e of j.equips || []) e.affinityEffect = [{ name: '간식 꺼내기', when: { on: 'fightStart' }, fx: [make(CAKE, 2)] }];
}

// ════════════════════════════════════════════════════════════════════
// 3. 비비(신성) — 딜러 · 미스틱 · 공명. 아군 손길로 「새싹」이 자라 다섯에 꽃핀다 — 연계로 동료 곁에서 함께 친다
// ════════════════════════════════════════════════════════════════════
function vivi(j) {
  const H = '비비_신성', K = '새싹';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '모두의 손길로 자라는 세계수 새싹', carrier: 'self', cap: 6,
    rules: [{ name: '개화', when: { on: 'stackReach', id: K, n: 6 }, fx: [spendAll(K), dmg(0.5, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '함께 걷는 세상', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
    { name: '이른 봄의 기운', when: { on: 'lowHp', pct: 0.3 }, fx: [{ k: 'cleanse', v: 1 }, st('피해 감소', 1)] },
  ];
  const cards = [
    // u1 굴리기 — 동료가 움직이면 저절로 나가는 연계
    // 3단계: 연계 갈래 ×0.7(넷의 몫 150% → 새싹 비례) · 남겨 둔 몫(연계를 떼고 보존 — 대가)
    card(H, 1, '셋의 몫', 1, '공격', [dmg(0.55), stk(K, 1)], [
      O('넷의 몫', [dmg(0.5), per(K), dmg(0.12)], { tags: ['연계'] }),
      O('나눈 빛', [dmg(0.45, EA), stk(K, 1)], { tags: ['연계'] }),
      O('다정한 몫', [dmg(0.55), stk(K, 1), ifAll, dmg(0.5)], { tags: ['연계'] }),
      O('남겨 둔 몫', [dmg(1.3), stk(K, 2)], { tags: ['보존'] }),
      O('함께 걷는 길', [dmg(0.8), stk(K, 1), power(rule('play', [{ k: 'extra', ratio: 0.4 }], { when: { who: 'other', type: '공격' }, limit: 2 }))], { power: true }),
    ], [B('따뜻한 빛', 'power'), B('잎사귀 바람', 'draw'), B('새순', [stk(K, 1)])], { tags: ['연계'] }),
    // u2 터뜨리기 — 새싹을 다 써서 광역(원작 저학년 · 2코 하나). 3단계: 기다리는 빛 = 쓰지 않고 키운다(재설계) · 3코 갈래 = 다 쓰고 바로 개화(장치 한 단계)
    card(H, 2, '모두의 빛', 2, '공격', [dmg(0.5, EA), per(K), dmg(0.18, EA), spendAll(K)], [
      O('찬란한 빛', [dmg(0.95, EA), per(K), dmg(0.3, EA), spendAll(K)]),
      O('작은 빛', [dmg(0.6, EA), per(K), dmg(0.22, EA), spendAll(K)], { cost: 1 }),
      O('빛의 장막', [dmg(0.7, EA), per(K), sh(0.8), spendAll(K)]),
      O('기다리는 빛', [dmg(0.9, EA), stk(K, 2)], { tags: ['보존'] }),
      O('새 세계수의 빛', [dmg(1.2, EA), per(K), dmg(0.4, EA), stk(K, 6)], { cost: 3 }),
    ], [B('세계수의 가지', 'power'), B('빛 가루', 'cost'), B('잎맥', [heal(0.5)])]),
    // u3 축 열기 — 손잡기: 새싹 + 동료 카드 드로우(시동 카드). 3단계: 포옹 = 새싹 대신 회복(재설계)
    card(H, 3, '손잡기', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], [
      O('꼭 잡은 손', [stk(K, 2), draw(1, { who: 'other' })]),
      O('놓지 않는 손', [draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('포옹', [heal(1.0), draw(2, { who: 'other' })]),
      O('자매 모임', [stk(K, 1), draw(1, { who: 'self', tag: '연계' }), draw(1, { who: 'other' })]),
      O('반가운 손', [stk(K, 1), draw(1, { who: 'other' }), inspire, stk(K, 2)]),
    ], [B('온기', 'draw'), B('토닥토닥', [heal(0.4)]), B('약속', { tags: ['보존'] })]),
    // u4 완성형 — 꽃이 필 때마다 더 크게. 엘다인 「완성형 강화 카드 1장 더」 + 원작 고학년 신성 상태(상시)라 ④ 강화를 남긴다.
    // 3단계: 기본형 사기 → 새싹(엘다인 상한) · 첫 서약 = 개전 · 새싹 대신 사기(재설계) · 자라는 서약 = 0코 · 매 턴 새싹
    card(H, 4, '새 세계수의 서약', 1, '강화', [stk(K, 1), power(rule('stackReach', [dmg(0.4, EA)], { when: { id: K, n: 6 } }))], [
      O('영원한 서약', [stk(K, 1), power(rule('stackReach', [dmg(0.6, EA)], { when: { id: K, n: 6 } }))]),
      O('자매의 서약', [stk(K, 1), draw(1, { who: 'other' }), power(rule('stackReach', [dmg(0.4, EA)], { when: { id: K, n: 6 } }))]),
      O('첫 서약', [st('사기', 1), power(rule('stackReach', [dmg(0.4, EA)], { when: { id: K, n: 6 } }))], { tags: ['개전'] }),
      O('지키는 서약', [stk(K, 1), power(rule('stackReach', [dmg(0.4, EA), st('피해 감소', 1)], { when: { id: K, n: 6 } }))]),
      O('자라는 서약', [power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [dmg(0.3, EA)], { when: { id: K, n: 6 } }))], { cost: 0 }),
    ], [B('세계수의 뿌리', 'atkUp'), B('아침 햇살', { tags: ['개전'] }), B('새잎', [stk(K, 1)])]),
  ];
  j.cards = [...j.cards.filter(c => !c.unique), ...cards];
  // u5 유틸(0코 스킬) — 연못가 대화(우이와 연못가에서 나누는 긴 대화): 새싹 · 실드 · 연계 카드 서치(엘다인 — 수치는 일반과 같게)
  const linkDraw = draw(1, { who: 'self', tag: '연계' });
  j.cards.push(card(H, 5, '연못가 대화', 0, '스킬', [stk(K, 1), sh(0.4), linkDraw], [
    O('신탁 1', [stk(K, 2), sh(0.3), linkDraw]),
    O('신탁 2', [stk(K, 1), sh(0.4), linkDraw], { tags: ['보존'] }),
    O('신탁 3', [heal(0.7), stk(K, 2), ifAll, stk(K, 2)]),
    O('신탁 4', [sh(0.65), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 }))], { power: true }),
    O('신탁 5', [{ k: 'discard', v: 1 }, stk(K, 3), sh(0.6)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1)])]));
  starter(j, '비비_신성_u3');
}

// ════════════════════════════════════════════════════════════════════
// 4. 멜루나 — 서포터 · 정령 · 냉정. 남긴 AP 로 「주가」를 올리고, 고점(다섯)에 상장하거나 일찍 매각해 한 방
// ════════════════════════════════════════════════════════════════════
function meluna(j) {
  const H = '멜루나', K = '주가';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '오르내리는 멜루 코퍼레이션 주가', carrier: 'self', cap: 4,
    rules: [{ name: '상장', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), ap(1), draw(2)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '이자 수익', when: { on: 'turnEnd' }, conds: [{ c: 'apLeft', n: 1 }], fx: [stk(K, 1)] },
    { name: '멜론 플렉스', when: { on: 'kill' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 2) : f));
  // 3단계: 부 장치 생성 카드 「머스크멜론」(멜론비 · 회사가 만든다 — 먹으면 회복 · 주가, 쥐면 ④가 센다)
  const MELON = '멜루나_melon';
  j.cards = j.cards.filter(c => c.id !== MELON);
  j.cards.push({ id: MELON, name: '머스크멜론', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.3), stk(K, 1)], blurb: '머스크가 고른 잘 익은 멜론 한 통.' });
  const exMelon = { k: 'exileFrom', from: 'hand', all: true, tag: MELON };
  const cards = [
    // u1 축 열기 — 투자 회사를 세운다(개전 강화 시동 카드: 매 턴 주가 + 저장). 3단계: 옛 ④ 멜론머스크 그룹의 상시 엔진을 시동으로 옮김
    card(H, 1, '장기 투자', 1, '강화', [stk(K, 1), st('저장', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('우량주', [stk(K, 2), st('저장', 1), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('분산 투자', [stk(K, 1), draw(2, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('공매도', [st('저장', 2), power(rule('turnStart', [stk(K, 2)]))]),
      O('선물 계약', [st('저장', 2), later(2, [stk(K, 3)]), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('사내 복지', [stk(K, 1), st('저장', 1), power(rule('turnStart', [stk(K, 1), make(MELON, 1)]))], { tags: ['개전'] }),
    ], [B('복리', 'draw'), B('배당금', [stk(K, 1)]), B('미리 산 주식', 'cost')], { tags: ['개전'] }),
    // u2 굴리기 — 원작 저학년 멜론비 다섯 번 + 입힌 피해만큼 회복(그림 짝 = 저학년 아이콘). 3단계: 주가 대신 멜론 한 통
    card(H, 2, '올 때 멜루나~', 1, '공격', [hits(5, 0.3), drain(0.5), make(MELON, 1)], [
      O('잘 익은 멜론', [hits(5, 0.42), drain(0.5), make(MELON, 1)]),
      O('멜론비 폭우', [hits(10, 0.32), drain(0.5), make(MELON, 3)], { cost: 2 }),
      O('배부른 정산', [hits(5, 0.3), per(K), dmg(0.35, ER), drain(0.5)]),
      O('사내 식당', [hits(5, 0.3), drain(0.5), power(rule('turnStart', [make(MELON, 1)]))], { power: true }),
      O('멜루나의 한턱', [{ k: 'discard', v: 1 }, hits(6, 0.32), make(MELON, 2)]),
    ], [B('최상급 멜론', 'power'), B('특별 보너스', 'ap'), B('멜론 한 입', [heal(0.4)])]),
    // u3 터뜨리기 — 주가 매각(머스크가 씨를 뱉는다). 3단계: 일부 매도 = 현금화(AP) · 씨 뿌리기 = 팔지 않고 멜론
    card(H, 3, '멜론 씨 기관총', 1, '공격', [hits(3, 0.3, E1), per(K), dmg(0.35), spendAll(K)], [
      O('씨 없는 멜론 없다', [hits(3, 0.4, E1), per(K), dmg(0.45), spendAll(K)]),
      O('적대적 인수', [per(K), dmg(0.6), spendAll(K), ifKill, stk(K, 3)]),
      O('일부 매도', [per(K, { max: 2 }), dmg(0.6), { k: 'spend', id: K, v: 2 }, ap(1)]),
      O('씨 뿌리기', [dmg(0.4, EA), per(K), dmg(0.2, EA), make(MELON, 1)]),
      O('작은 씨앗', [hits(3, 0.22, E1), per(K), dmg(0.3), spendAll(K)], { cost: 0 }),
    ], [B('단단한 씨', 'power'), B('가벼운 씨', 'cost'), B('약점 분석', 'weakSpot')]),
    // u4 완성형 = ④ 1코 마무리 — 손의 멜론을 머스크가 다 먹고 뱉는다(멜론 1장당). 3단계: 강화 → 장치를 세는 공격, 상시 엔진은 D 갈래(주주 총회)
    card(H, 4, '멜론머스크 그룹', 1, '공격', [dmg(0.7), perTag(MELON), dmg(0.45), exMelon], [
      O('대기업', [dmg(0.85), perTag(MELON), dmg(0.55), exMelon]),
      O('주주 총회', [perTag(MELON), dmg(0.6), exMelon, power(rule('exhaust', [stk(K, 1)], { limit: 2 }))], { power: true }),
      O('화성 프로젝트', [dmg(0.5, EA), perTag(MELON), dmg(0.35, EA), stk(K, 2)]),
      O('창립 기념일', [dmg(0.7), perTag(MELON), dmg(0.45), exMelon], { tags: ['보존'] }),
      O('신규 상장', [dmg(0.6), perTag(MELON), dmg(0.4), make(MELON, 2)]),
    ], [B('회장님의 힘', 'power'), B('동행하는 머스크', [st('협공', 1)]), B('멜론 재고', [make(MELON, 1)])]),
  ];
  j.cards = [...j.cards.filter(c => !c.unique), ...cards];
  // u5 유틸(0코 스킬) — 광합성: 취미가 광합성 — 회복 · 주가 · 드로우(공격 셋이라 스킬 쪽)
  j.cards.push(card(H, 5, '광합성', 0, '스킬', [heal(0.4), stk(K, 1), draw(1)], [
    O('신탁 1', [heal(0.55), stk(K, 2), draw(1)]),
    O('신탁 2', [heal(0.3), make(MELON, 1), draw(1)]),
    O('신탁 3', [heal(0.3), stk(K, 1), ifStack(K, 3), draw(2)]),
    O('신탁 4', [heal(0.5), stk(K, 2), power(rule('turnStart', [heal(0.3)]))], { power: true }),
    O('신탁 5', [{ k: 'spend', id: K, v: 2 }, heal(0.6), ap(1)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'heal'), B('축복 3', [stk(K, 1)])]));
  starter(j, '멜루나_u1');
}

// ════════════════════════════════════════════════════════════════════
// 5. 디아나 — 딜포터(서포터) · 수인 · 광기. 때려서 고친다 — 공격으로 「기혈」을 돌리고, 든 기혈은 턴 끝 회복 · 넘친 회복은 피해
// ════════════════════════════════════════════════════════════════════
function diana(j) {
  const H = '디아나', K = '기혈';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '몸 안을 도는 은퇴한 무인의 기', carrier: 'self', cap: 5, per: [{ stat: 'hot', ratio: 0.15 }] };
  delete h.keywords;
  const overheal = h.passives.find(p => p.name === '진짜 치료법');
  h.passives = [
    overheal,
    { name: '때리는 치료', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
  ];
  // 3단계: 부 장치 생성 카드 「주먹밥」(잔칫상이 만든다 — 먹으면 기혈 1 · 드로우 1)
  const RICE = '디아나_rice';
  j.cards = j.cards.filter(c => c.id !== RICE);
  j.cards.push({ id: RICE, name: '주먹밥', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [stk(K, 1), draw(1)], blurb: '디아나가 손수 뭉친 큼직한 주먹밥.' });
  const cards = [
    // u1 굴리기 — 원작 저학년 자연 치유(두 번 회복 → 회복 + 기혈 비례)
    card(H, 1, '자연 치유', 1, '스킬', [heal(1.0), per(K), heal(0.2)], [
      O('깊은 호흡', [heal(1.3), per(K), heal(0.25)]),
      O('백년 약탕', [heal(2.2), per(K), heal(0.35), st('초재생', 1)], { cost: 2 }),
      O('약손', [heal(0.6), power(rule('turnStart', [heal(0.5)]))], { power: true }),
      O('꼬마 디아나', [heal(0.9), per(K), heal(0.2), ifWounded, heal(1.0)]),
      O('자연의 힘', [heal(0.9), stk(K, 2)]),
    ], [B('따스한 손', 'heal'), B('단전 호흡', 'defUp'), B('기 모으기', [stk(K, 1)])]),
    // u2 축 열기 — 강평: 친 만큼 낫게(피해 기반 회복) · 시동 카드 후보
    card(H, 2, '기공 주문', 1, '공격', [dmg(1.0), drain(0.6), stk(K, 1)], [
      O('기공탄', [dmg(1.25), drain(0.6), stk(K, 1)]),
      O('기공 파동', [dmg(0.65, EA), drain(0.5), stk(K, 2)]),
      O('경혈 찌르기', [dmg(1.0), drain(0.6), ifBroken, dmg(0.9)]),
      O('지팡이 톡', [dmg(0.8), drain(0.5)], { cost: 0 }),
      O('고라니 아니다', [per(K), dmg(0.5), spendAll(K), drain(0.6)]),
    ], [B('노련한 손목', 'power'), B('경혈 짚기', 'frost'), B('지팡이 내려놓기', 'cost')]),
    // u3 큰 회복 · 밥 먹이기 — 2코 하나(숫자 큰 카드라 축복에 AP · 드로우). 3단계: 다음 턴 드로우 → 생성 카드 「주먹밥」(먹으면 기혈 · 드로우)
    card(H, 3, '수인 마을 잔칫상', 2, '스킬', [heal(1.2), make(RICE, 2), st('결의', 1)], [
      O('푸짐한 상차림', [heal(1.8), make(RICE, 3), st('결의', 1)]),
      O('간단한 한 끼', [heal(0.8), make(RICE, 1), st('결의', 1)], { cost: 1 }),
      O('더 먹고 가렴', [heal(1.0), make(RICE, 2), power(rule('turnStart', [make(RICE, 1)]))], { power: true }),
      O('잔칫날 아침', [heal(0.8), make(RICE, 3), draw(1, { who: 'other' })], { tags: ['개전'] }),
      O('온 마을 잔치', [heal(1.8), st('다음 턴 드로우', 3), st('결의', 1)]),
    ], [B('할머니 손맛', 'heal'), B('배부른 기운', 'ap'), B('한 그릇 더', 'draw')]),
    // u4 완성형 = ④ 1코 마무리 — 모은 기혈을 다 쏟는 일격(결정화는 담당 버프로 남김).
    // 3단계: 강화 → 장치를 세는 공격. 옛 상시 엔진(턴 끝 기혈 비례 타격)은 D 갈래 「사부의 비기」 로
    card(H, 4, '은퇴한 무인의 수련', 1, '공격', [per(K), dmg(0.4), spendAll(K), st('결정화', 1)], [
      O('사부의 비기', [stk(K, 2), st('결정화', 1), power(rule('turnEnd', [per(K), dmg(0.2, ER)]))], { power: true }),
      O('붉은 눈', [per(K), dmg(0.5), spendAll(K), st('결정화', 1)]),
      O('기혈 타통', [stk(K, 2), per(K), heal(0.35), st('결정화', 2)]),
      O('무인의 마지막 일격', [per(K), dmg(1.25), spendAll(K), st('결정화', 1)], { tags: ['소멸'] }),
      O('경혈 타격', [per(K), dmg(0.45), spendAll(K), ifWounded, heal(1.5)]),
    ], [B('새벽 수련', 'ap'), B('굳은살', 'defUp'), B('약수 한 모금', [stk(K, 1)])]),
  ];
  j.cards = [...j.cards.filter(c => !c.unique), ...cards];
  // u5 유틸(0코 공격) — 해바라기 톡톡: 어린 꽃사슴 차림으로 해바라기를 휘두른다 — 작은 피해 · 기혈 · 드로우
  j.cards.push(card(H, 5, '해바라기 톡톡', 0, '공격', [dmg(0.45), stk(K, 1), draw(1)], [
    O('신탁 1', [dmg(0.7), stk(K, 1), draw(1)]),
    O('신탁 2', [dmg(0.65), make(RICE, 1)]),
    O('신탁 3', [dmg(0.55), heal(0.6), draw(1)]),
    O('신탁 4', [dmg(0.5), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 }))], { power: true }),
    O('신탁 5', [{ k: 'discard', v: 1 }, dmg(0.9), stk(K, 2)]),
  ], [B('축복 1', 'weakSpot'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]));
  starter(j, '디아나_u2');
}

// ════════════════════════════════════════════════════════════════════
// 6. 키샤 — 서포터 · 유령 · 우울(원작 방식 고학년 유지). 「짱팬」 한 명을 골라 그 동료 곁에서 함께 노래하고, 공연으로 파티 피해를 모아 터뜨린다
// ════════════════════════════════════════════════════════════════════
function kisha(j) {
  const H = '키샤', K = '짱팬', SHOW = '공연 중';
  const h = j.heroes[0];
  const show = h.keywords.filter(k => k.name === SHOW || k.name === '저장된 환호');
  h.keyword = { name: K, desc: '키샤가 기억하는 단 한 명의 팬', carrier: 'hero', cap: 1 };
  h.keywords = show;
  h.passives = [
    { name: '팬 서비스', when: { on: 'play', marked: K, type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [{ k: 'extra', ratio: 0.6 }] },
    { name: '너는 나의 짱팬!', when: { on: 'play', marked: K }, limit: { per: 'fight', n: 3 }, fx: [st('사기', 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === '열기' ? draw(2) : f));
  const fan = { k: 'stack', id: K, v: 1, target: 'oneAlly' };
  const cards = [
    // u1 축 열기 — 팬 고르기 + 회복(원작 저학년 궁극의 멜로디) · 시동 카드 후보
    card(H, 1, '궁극의 멜로디', 1, '스킬', [fan, heal(1.0), st('사기', 1)], [
      O('클라이맥스', [fan, heal(1.4), st('사기', 1)]),
      O('앙코르 메들리', [fan, st('사기', 1), power(rule('play', [dmg(0.45)], { when: { marked: K }, limit: 2 }))], { power: true }),
      O('허밍', [fan, heal(0.8), st('사기', 1)], { cost: 0 }),
      O('킹짱 멜로디', [fan, st('사기', 1), draw(2, { who: 'other' })]),
      O('떼창 유도', [fan, { k: 'discard', v: 1 }, st('사기', 2)]),
    ], [B('맑은 고음', 'heal'), B('빠른 템포', 'draw'), B('팬 사인회', [st('결의', 1)])]),
    // u2 굴리기 — 조명봉: 협공 + 동료 카드(파티 연계)
    card(H, 2, '체리 레드 조명봉', 1, '스킬', [st('협공', 1), draw(1, { who: 'other' })], [
      O('조명봉 물결', [st('협공', 2), draw(1, { who: 'other' })]),
      O('객석 전체 점등', [power(rule('turnStart', [st('협공', 1)]))], { power: true }),
      O('오프닝 조명', [st('협공', 1), draw(2, { who: 'other' })], { tags: ['개전'] }),
      O('응원 구호', [fan, st('협공', 2), ifAll, draw(2, { who: 'other' })]),
      O('체리색 집착', [st('협공', 2), { k: 'discard', v: 1 }, draw(2, { who: 'other' })]),
    ], [B('밝은 불빛', 'draw'), B('재빠른 흔들기', 'cost'), B('형광 응원봉', [fan])]),
    // u3 도취(약화 크게 · 한 명). 3단계: 하트 저격 = 약화 대신 격파 조건 + 동료 카드(재설계) · 너만을 위한 노래 = 매 턴 약화(강화화)
    card(H, 3, '하트 파동', 1, '공격', [dmg(0.9), st('약화', 2, E1)], [
      O('진심 파동', [dmg(1.3), st('약화', 2, E1)]),
      O('연속 하트', [dmg(0.6, EA), st('약화', 1, EA), draw(1)]),
      O('하트 저격', [dmg(1.0), ifBroken, dmg(1.0), draw(1, { who: 'other' })]),
      O('윙크 한 번', [dmg(0.75), st('약화', 2, E1)], { cost: 0 }),
      O('너만을 위한 노래', [dmg(0.7), st('약화', 2, E1), power(rule('turnStart', [st('약화', 1, EA)]))], { power: true }),
    ], [B('큰 하트', 'power'), B('하트 눈빛', 'frost'), B('앵콜 요청', 'draw')]),
    // u4 터뜨리기 — 작은 공연: 이번 턴 파티가 친 피해를 모아 다음 턴 시작에 터뜨린다(고학년 장치를 카드로) = ④ 1코 마무리.
    // 3단계: 전원 기립 = 공연 없이 협공 · 드로우(재설계) · 앙코르 공연 = 동료 카드 서치
    card(H, 4, '떼창 지진', 1, '공격', [stk(SHOW, 1), dmg(0.6, EA)], [
      O('객석 대지진', [stk(SHOW, 1), dmg(0.75, EA)]),
      O('전원 기립', [dmg(0.5, EA), st('협공', 1), draw(1)]),
      O('지하 무대', [stk(SHOW, 1), dmg(0.5, EA)], { tags: ['보존'] }),
      O('무대 붕괴', [stk(SHOW, 1), dmg(1.2, EA), st('약화', 1, EA)], { cost: 2 }),
      O('앙코르 공연', [stk(SHOW, 1), dmg(0.5, EA), draw(1, { who: 'other' })], { tags: ['회수'] }),
    ], [B('우렁찬 함성', 'power'), B('쿵쿵 박자', 'frost'), B('열기 유지', [stk('저장된 환호', 3)])]),
  ];
  j.cards = [...j.cards.filter(c => !c.unique), ...cards];
  // u5 유틸(0코 스킬) — 립싱크 무대: 입만 맞추고 넘기는 무대 — 짱팬 · 실드 · 고유 카드 서치
  const mineDraw = draw(1, { who: 'self', unique: true });
  j.cards.push(card(H, 5, '립싱크 무대', 0, '스킬', [fan, mineDraw], [
    O('신탁 1', [fan, sh(0.3), mineDraw]),
    O('신탁 2', [fan, sh(0.2), mineDraw], { tags: ['개전'] }),
    O('신탁 3', [fan, sh(0.3), draw(1, { who: 'other', type: '공격' })]),
    O('신탁 4', [fan, mineDraw, power(rule('play', [sh(0.3)], { when: { marked: K }, limit: 2 }))], { power: true }),
    O('신탁 5', [{ k: 'discard', v: 1 }, sh(0.4), draw(2, { who: 'other' })]),
  ], [B('축복 1', 'guard'), B('축복 2', 'heal'), B('축복 3', [fan])]));
  starter(j, '키샤_u1');
}

run([['용족/루드', rude], ['요정/에르핀', erpin], ['마녀/비비_신성', vivi], ['정령/멜루나', meluna], ['수인/디아나', diana], ['유령/키샤', kisha]], new URL('./boost.json', import.meta.url));
