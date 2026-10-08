// 18갈래 재설계 3단계 — 정령B 묶음 8명(시저 · 실라 · 아르코 · 아일라 · 오로라 · 우이 · 우이(기억) · 잉클). 2026-10-08
// 지침: _gen/rework/BRIEF_118.md · 기준: _measure/갈래_세분화/05_시범16_결과.md · 기록: _measure/갈래_세분화/06_118/정령B.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// 생성 카드 사도: 원래 만들던 우이는 그대로 + 새로 우이(기억) 하나(원작 저학년 「세잎클로버를 얻는다」 — 되찾은 기억이 셋이 되면 클로버).
// node _gen/rework/정령B_18.mjs [사도 이름 일부]  → heroes/정령/<파일>.json (백업 heroes_before_118_20261008 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, st, stk, spendAll, per, perTag, draw, make, ifStack,
  TOP, gauge, hasten, ripen, exile, srch, drawType, pull, xtra, pw, pas, Or, bl, U, setCards, setOpener, dmod, cs, spendN } = L;
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
const FAR = 'slowestEnemy';                       // 「가장 먼 적」 대신 — 행동 카운트가 가장 큰 적(맨 늦게 오는 적)
const alt = { k: 'ifPrevSame', not: true };       // 바로 앞 카드와 종류가 다르면(엇박)
const ifAll = { k: 'ifAllHeroes' };
const onDisc = { k: 'when', on: 'discard' };      // 안식 — 버려지면
const cleanse = v => ({ k: 'cleanse', v });

// ════════════════════════════════════════════════════════════════════
// 1. 시저 — 예약형 · 서포터 · 냉정. 좋은 걸 맛보인 뒤 빼앗는다 — 적에게 「단종 예고」(예약 2칸)를 걸고 그동안 파티는 한정판의 맛을 본다.
//    다 닳으면 단종 — 그 적이 절망한다(피해 · 약화 · 취약). 일찍 잘라 버리면(당겨 쓰기) 남은 칸만큼 약하고, 그 전에 쓰러뜨리면 남은 몫만큼 파티가 산다
// 원작: 「좋은 걸 맛보게 해 놓고 다시는 못 얻게 해 절망시킨다」 악행 철학 · 단종된 한정판 햄버거로 빌런이 됨 · 저학년 실이 보인다(6회 + SP 를 깎아 내 SP 로)
//       · 어사이드 방해꾼(공격력 상위 적 지정 · 풀리거나 죽으면 아군 회복) · 화단 물 주기
// 시동: u1 한정판의 맛(그대로 — 강화 개전 → 스킬)
// ════════════════════════════════════════════════════════════════════
function caesar(j) {
  const H = '시저', K = '단종 예고';
  const h = j.heroes[0];
  h.blurb = '좋은 걸 맛보게 한 뒤 빼앗는 빌런 지망생. 적에게 단종 예고를 걸어 두면 두 차례 뒤 한정판이 사라지며 그 적이 절망하고, 그 전에 쓰러뜨리면 남은 몫만큼 파티가 기운을 차립니다.';
  h.keyword = {
    name: K, desc: '두 차례 뒤 단종되는 한정판', carrier: 'enemy', cap: 2, reserve: true, decay: 1,
    rules: [{ name: '단종', when: { on: 'stackGone', id: K, decay: true }, fx: [dmg(1.4), st('약화', 1), st('취약', 2)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('빌런의 수하', 'reserveFire', [dmod(0.2, 'allAllies')], { limit: 1 }),
    pas('빌런의 수하', 'fightStart', [stk(K, 2, TOP)]),
    pas('의외로 빌런이 이기는 전개', 'kill', [per(K), heal(0.45), stk(K, 1, TOP)]),
  ];
  h.ult.fx = [dmg(1.5, EA), st('약화', 2, EA), stk(K, 1, EA)];
  setCards(j, [
    // 시동 — 한정판의 맛: 이번 턴 파티 주는 피해 +20%(맛보기) + 고른 적에게 단종 예고 2 + 드로우
    U(H, 1, '한정판의 맛', 1, '스킬', [dmod(0.2, 'allAllies'), stk(K, 2, E1), draw(1)], nm([
      Or([dmod(0.3, 'allAllies'), stk(K, 2, E1), draw(1)]),
      Or([dmod(0.2, 'allAllies'), stk(K, 2, E1)], { cost: 0 }),
      Or([dmod(0.2, 'allAllies'), stk(K, 2, E1), draw(2)]),
      Or([dmod(0.2, 'allAllies'), stk(K, 2, E1), pw('reserveFire', [sh(0.8), draw(1)], { limit: 1 })], { power: true }),
      Or([dmod(0.25, 'allAllies'), stk(K, 2, EA), sh(1.25)]),
    ]), null),
    // 원작 자유 — 보이는 실(실이 보인다): 고른 적 6회 + 고학년 게이지(SP 를 깎아 내 몫으로)
    U(H, 2, '보이는 실', 1, '공격', [dmg(0.17, E1, { hits: 6 }), gauge(6)], [
      'A', 'B',
      Or([dmg(0.15, E1, { hits: 6 }), stk(K, 1, E1), gauge(6)]),
      ['D', 'reserveFire', [dmg(0.15, ER, { hits: 3 })], { limit: 1 }],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 쓰기 — 싹둑싹둑: 피해 + 그 적의 단종 예고를 지금 잘라 버린다(남은 1칸당 -30%)
    U(H, 3, '싹둑싹둑', 1, '공격', [dmg(0.55), ripen(K, 0.3)], [
      'A', 'B',
      Or([dmg(0.5, EA), ripen(K, 0.3, EA)]),
      Or([dmg(0.5), ripen(K, 0.3), srch()]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 둘째 — 빌런의 품격: 피해 + 그 적의 단종 예고 1개당 회복(쓰지 않음)
    U(H, 4, '빌런의 품격', 1, '공격', [dmg(0.65), per(K), heal(0.25)], [
      'A', 'B',
      Or([dmg(0.6), per(K), heal(0.22), stk(K, 1, E1)]),
      ['D', 'reserveFire', [heal(0.6)], { limit: 1 }],
      'Hn',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 갈래 부품 — 화단 물 주기: 회복 + 재촉 1(파티의 모든 예약을 한 칸 — 물을 주면 빨리 여문다) + 실드
    U(H, 5, '화단 물 주기', 1, '스킬', [heal(0.7), hasten(1), sh(0.4)], [
      'A', 'B',
      Or([heal(0.6), hasten(1), stk(K, 1, E1)]),
      ['D', 'turnStart', [hasten(1)], { limit: 1 }],
      Or([heal(0.6), hasten(1), srch()]),
    ], bl('heal', 'ap', [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 실라 — 표적형 · 딜러 · 냉정. 늘 가장 먼 적 하나와 정면 승부 — 전투 시작과 승부가 끝날 때마다 행동이 가장 늦은 적(「가장 먼 적」 대신)을 찍는다.
//    다른 아군이 끼어들면 승부가 흐트러지고(정면 승부 -1), 혼자 끝내야 세다. 셋을 채운 승부는 털어 끝낼까 쥐고 계속 깎을까
// 원작: 평타 · 저학년(5연사) · 고학년(바람 정령 단일 거대 피해) 모두 「가장 먼 적」 · 정면 승부에선 진 적이 없고 기습 · 저격에 진다 · 맏언니 · 하늬바람
// 「가장 먼 적」 대상은 엔진에 없다 — 찍기(hunt) + 행동 카운트가 가장 큰 적으로 대신(엔진 요청에 적음)
// 시동: u3 바람 읽기(그대로)
// ════════════════════════════════════════════════════════════════════
function sylla(j) {
  const H = '실라', K = '정면 승부';
  const h = j.heroes[0];
  h.blurb = '정면 승부엔 강하고 기습엔 약한 바람의 맏언니. 늘 가장 멀리 있는 적 1명을 찍어 마주 서고, 끼어드는 이 없이 혼자 쏠수록 그 적은 바람에 깎여 무너집니다.';
  h.keyword = {
    name: K, desc: '한 적과 마주 선 승부', carrier: 'enemy', cap: 3, hunt: true, per: [{ stat: 'taken', v: 0.12 }],
    rules: [{ name: '끼어들기', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [{ k: 'spend', id: K, v: 1, target: 'markedEnemy' }] }],
  };
  h.passives = [
    pas('맞바람', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    pas('정령의 수호자', 'fightStart', [stk(K, 1, FAR)]),
    pas('정령의 수호자', 'huntDown', [draw(1), stk(K, 2, FAR)]),
  ];
  setCards(j, [
    // 쓰기 — 정조준: 정면 승부 1개당 피해(최소 1), 셋(가득)이면 강인도 + 정면 승부 전부 소모(승부를 끝낸다 — 셋이 아니면 쥐고 계속 깎는다)
    U(H, 1, '정조준', 1, '공격', [per(K, { n: 1 }), dmg(0.6), ifStack(K, 3), tough(1), spendAll(K)], [
      'A',
      ['D', 'huntDown', [dmg(0.5, FAR)], { limit: 1 }],
      Or([per(K, { n: 1 }), dmg(0.55), st('둔화', 1), ifStack(K, 3), tough(1)]),
      Or([per(K, { n: 1 }), dmg(0.55), srch({ type: '공격' })]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 갈래 부품 — 래피드 샷: 5연사. 정면 승부가 둘 이상이면 추가 공격(마주 선 적에게 한 발 더)
    U(H, 2, '래피드 샷', 1, '공격', [dmg(0.24, E1, { hits: 5 }), ifStack(K, 2), xtra(0.5)], [
      'A', 'B',
      Or([dmg(0.22, E1, { hits: 5 }), st('둔화', 1), ifStack(K, 2), xtra(0.5)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.24, E1, { hits: 5 }), srch()]),
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 시동 — 바람 읽기(0): 고른 적과 정면 승부 + 드로우
    U(H, 3, '바람 읽기', 0, '스킬', [stk(K, 1, E1), draw(1)], nm([
      Or([stk(K, 2, E1), draw(1)]),
      Or([stk(K, 1, E1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1, FAR), draw(2)]),
      Or([stk(K, 1, E1), draw(1), pw('huntDown', [draw(1), sh(0.4)], { limit: 1 })], { power: true }),
      Or([stk(K, 1, E1), drawType('공격'), sh(0.4)]),
    ]), null),
    // 원작 자유 — 맏언니의 각오(강화): 정면 승부 + 이 전투 동안 공격 카드를 내면 추가 공격(정면엔 강하다)
    U(H, 4, '맏언니의 각오', 1, '강화', [stk(K, 1, E1), pw('play', [xtra(0.3)], { when: { type: '공격' }, limit: 1 })], nm([
      Or([stk(K, 2, E1), pw('play', [xtra(0.35)], { when: { type: '공격' }, limit: 1 })]),
      Or([pw('play', [xtra(0.3)], { when: { type: '공격' }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 1, E1), pw('huntDown', [stk(K, 2, FAR), sh(0.6)], { limit: 1 })]),
      Or([srch({ type: '공격' }), pw('play', [xtra(0.3)], { when: { type: '공격' }, limit: 1 })]),
      Or([stk(K, 1, E1), { k: 'discard', v: 1 }, pw('play', [xtra(0.4)], { when: { type: '공격' }, limit: 2 })]),
    ]), bl('atkUp', 'draw', [stk(K, 1, E1)])),
    // 둘째 — 하늬바람(0): 정면 승부 + 손의 자신의 공격 카드 1장 비용 -1
    U(H, 5, '하늬바람', 0, '스킬', [stk(K, 1, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })], [
      Or([stk(K, 2, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })]),
      Or([stk(K, 1, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })], { tags: ['신속'] }),
      Or([stk(K, 1, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), draw(1)]),
      Or([stk(K, 1, E1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 2, E1), cs('비용', -1, { to: 'hand', n: 2, who: 'self', type: '공격' })]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1, E1)])),
  ]);
}
const tough = (v, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });

// ════════════════════════════════════════════════════════════════════
// 3. 아르코 — 박자형 · 딜러 · 활발. 바로 앞 카드와 「다른 종류」(공격 ↔ 스킬)가 나올 때마다 스텝 — 누구의 카드든 엇박이면 된다, 같은 종류만 이으면 지루해 멈춘다.
//    스텝 셋이면 브레이크(다음 카드 강화), 스텝은 턴이 끝나면 풀린다. 털어서 토네이도로 돌까, 셋을 채워 브레이크를 받을까
// 원작: 강화 평타 = 세 번째 공격마다 음파 두 번 · 저학년 정정당당 댄스배틀(4회 범위) · 고학년 퍼플 쇼츠(HP 50% 미만 확정 치명) · 「늘 추던 한 가지 춤만」 춰서 지옥 훈련
// 티그(제 공격만 잇기) · 리코타(비용 오름) · 아사나(주인 교대)와 가르기: 아르코는 종류 교대(주인 무관)
// 시동: u3 프리즈(그대로)
// ════════════════════════════════════════════════════════════════════
function arco(j) {
  const H = '아르코', K = '스텝';
  const h = j.heroes[0];
  h.blurb = '춤밖에 모르는 적포도 스트리트 댄서. 공격과 스킬을 엇박으로 바꿔 낼 때마다 스텝이 쌓이고 — 누구의 카드든 상관없습니다 — 세 번째 스텝에 브레이크가 터집니다. 같은 춤만 추면 지루해서 멈춥니다.';
  h.keyword = { name: K, desc: '엇박에 맞춰 밟는 스텝', carrier: 'self', cap: 3, endClear: true, onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('리듬에 몸을 맡겨', 'play', [alt, stk(K, 1)], { when: { who: 'any' }, limit: 3 }),
    pas('음파 스텝', 'play', [dmg(0.3, ER, { hits: 2 })], { when: { type: '공격', every: 3 } }),
  ];
  setCards(j, [
    // 원작 자유 — 정정당당 댄스배틀: 적 전체 4회 + 엇박이면 스텝 한 번 더
    U(H, 1, '정정당당 댄스배틀', 1, '공격', [dmg(0.2, EA, { hits: 4 }), alt, stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.2, EA, { hits: 4 }), alt, st('약화', 1, EA)]),
      ['D', 'play', [alt, dmg(0.28, ER)], { when: { type: '스킬' }, limit: 2 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 쓰기 — 토네이도 스핀: 적 전체 + 스텝 1개당, 스텝 전부 소모(셋을 기다려 브레이크를 받을지)
    U(H, 2, '토네이도 스핀', 1, '공격', [dmg(0.5, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      'A',
      ['D', 'play', [alt, stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.9), per(K), dmg(0.35), spendAll(K)]),
      Or([dmg(0.45, EA), per(K), dmg(0.18, EA), srch({ type: '스킬' })]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 프리즈(0): 스텝 + 드로우
    U(H, 3, '프리즈', 0, '스킬', [stk(K, 1), draw(1)], nm([
      Or([stk(K, 2), draw(1)]),
      Or([stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1), drawType('공격')]),
      Or([stk(K, 1), pw('play', [alt, draw(1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([stk(K, 1), st('회피', 1)]),
    ]), null),
    // 둘째 — 적포도 스트리트(강화): 협공 + 이 전투 동안 엇박이 이어질 때마다 음파
    U(H, 4, '적포도 스트리트', 1, '강화', [st('협공', 1), pw('play', [alt, dmg(0.22, ER, { hits: 2 })], { limit: 2 })], nm([
      Or([st('협공', 2), pw('play', [alt, dmg(0.25, ER, { hits: 2 })], { limit: 2 })]),
      Or([pw('play', [alt, dmg(0.22, ER, { hits: 2 })], { limit: 2 })], { cost: 0 }),
      Or([st('협공', 1), pw('play', [alt, stk(K, 1), dmg(0.4, ER)], { limit: 1 })]),
      Or([st('협공', 1), srch({ type: '스킬' }), pw('play', [alt, dmg(0.22, ER, { hits: 2 })], { limit: 2 })]),
      Or([st('협공', 1), { k: 'discard', v: 1 }, pw('play', [alt, dmg(0.3, ER, { hits: 2 })], { limit: 3 })]),
    ]), bl('atkUp', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 엇박 스텝(0): 자신의 공격 카드 1장 + 동료 카드 1장(다음 엇박을 손에)
    U(H, 5, '엇박 스텝', 0, '스킬', [drawType('공격'), draw(1, { who: 'other' })], [
      Or([drawType('공격'), draw(1, { who: 'other' }), stk(K, 1)]),
      Or([drawType('공격'), draw(1, { who: 'other' })], { tags: ['신속'] }),
      Or([drawType('공격'), draw(1, { who: 'other' }), sh(0.5)]),
      Or([drawType('공격'), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([drawType('공격', 2), draw(1, { who: 'other' }), { k: 'discard', v: 1 }]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 아일라 — 쌓고 고르기 · 서포터 · 순수. 파티 실드가 깨질 때마다 빠직이 한 단계(쫌폭망 → 초폭망) — 저절로 터지지 않는다.
//    명상으로 조금씩 가라앉힐까(회복 · 실드), 대폭망 이상으로 쥐고 추방령(기절)을 노릴까, 고학년 대분화에 다 쏟을까
// 원작: 저학년 휴가 중 잠적(보호막 · 깨지면 화산 정령이 치고 약화) · 고학년 활활화산(아군 보호막이 깨질 때마다 빠직, 고학년 때 전부 써서 대분화)
//       · 분노 단계(쫌 · 소 · 대 · 초폭망) · 가비아에게 배운 대지 명상 · 벨라를 피해 숨는 휴가
// 시동: u1 초폭망 직전(그대로)
// ════════════════════════════════════════════════════════════════════
function ayla(j) {
  const H = '아일라', K = '빠직';
  const h = j.heroes[0];
  h.blurb = '화산섬 볼케니카의 정령. 친구들의 실드가 깨질 때마다 빠직이 한 단계씩 차오르고, 명상으로 가라앉힐지 대폭망까지 참았다 한 번에 터뜨릴지 직접 고릅니다. 실드가 깨지면 화산 정령이 대신 화를 냅니다.';
  h.keyword = { name: K, desc: '세며 참는 화산섬의 분노', carrier: 'self', cap: 4, stages: ['쫌폭망', '소폭망', '대폭망', '초폭망'], per: [{ stat: 'guard', v: 0.1 }] };
  h.passives = [
    pas('부글부글', 'shieldBreak', [stk(K, 1)], { limit: 3 }),
    pas('부글부글', 'fightStart', [stk(K, 1)]),
    pas('화산 정령', 'shieldBreak', [dmg(0.6), st('약화', 1), sh(0.4)], { limit: 1 }),
  ];
  h.ult.fx = [sh(2.4), dmg(1.0, EA), per(K), dmg(0.35, EA), spendAll(K)];
  setCards(j, [
    // 시동 — 초폭망 직전(0): 빠직 + 실드
    U(H, 1, '초폭망 직전', 0, '스킬', [stk(K, 1), sh(0.9)], nm([
      Or([stk(K, 2), sh(0.9)]),
      Or([stk(K, 1), sh(0.9)], { tags: ['신속'] }),
      Or([stk(K, 1), sh(0.7), draw(1)]),
      Or([stk(K, 1), sh(0.6), pw('shieldBreak', [sh(0.4)], { limit: 1 })], { power: true }),
      Or([stk(K, 2), sh(1.4), { k: 'discard', v: 1 }]),
    ]), null),
    // 갈래 부품 — 휴가 중 잠적: 큰 실드(깨지면 빠직 · 화산 정령) + 빠직
    U(H, 2, '휴가 중 잠적', 1, '스킬', [sh(1.7), stk(K, 1)], [
      'A', 'B',
      Or([sh(1.5), stk(K, 1), st('반격', 1)]),
      ['D', 'shieldBreak', [stk(K, 1), sh(0.3)], { limit: 1 }],
      Or([sh(1.5), stk(K, 1), srch()]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 화를 삭이는 명상: 회복 + 빠직 1개당 실드, 빠직 전부 소모
    U(H, 3, '화를 삭이는 명상', 1, '스킬', [heal(0.8), per(K), sh(0.4), spendAll(K)], [
      'A',
      ['D', 'spend', [heal(0.3)], { when: { id: K }, limit: 1 }],
      Or([heal(0.7), per(K), dmg(0.3, EA), spendAll(K)]),
      Or([heal(0.7), per(K), sh(0.36), srch()]),
      'Hn',
    ], bl('heal', 'ap', [stk(K, 1)])),
    // 둘째 — 화산섬의 주인(강화): 결의 + 이 전투 동안 빠직을 소모하면 적 전체 피해(대분화의 여진)
    U(H, 4, '화산섬의 주인', 1, '강화', [st('결의', 1), pw('spend', [dmg(0.4, EA)], { when: { id: K }, limit: 1 })], nm([
      Or([st('결의', 2), pw('spend', [dmg(0.45, EA)], { when: { id: K }, limit: 1 })]),
      Or([pw('spend', [dmg(0.4, EA)], { when: { id: K }, limit: 1 })], { cost: 0 }),
      Or([st('결의', 1), stk(K, 1), pw('spend', [dmg(0.5, EA)], { when: { id: K }, limit: 1 })]),
      Or([st('결의', 1), srch(), pw('spend', [dmg(0.4, EA)], { when: { id: K }, limit: 1 })]),
      Or([st('결의', 1), { k: 'discard', v: 1 }, pw('spend', [dmg(0.4, EA), sh(0.4)], { when: { id: K }, limit: 1 })]),
    ]), bl('defUp', 'cost', [stk(K, 1)])),
    // 원작 자유 — 영구 추방령: 피해 + 빠직이 대폭망(3) 이상이면 기절(쥐고 있어야 선다)
    U(H, 5, '영구 추방령', 1, '공격', [dmg(1.0), ifStack(K, 3), st('기절', 1)], [
      'A', 'B',
      Or([dmg(0.9), stk(K, 1), ifStack(K, 3), st('기절', 1)]),
      ['D', 'shieldBreak', [dmg(0.45)], { limit: 1 }],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 오로라 — 거드는 형 · 탱커 · 우울. 혼자 남겨지는 게 두렵다 — 맞을 때마다 · 셋이 함께 움직인 턴마다 빛울림, 다섯이면 파티의 다음 카드에 극광(강화).
//    아군의 나쁜 기운(디버프)을 지워 긍정으로 바꾸고, 장막을 드리워 턴마다 빛의 비. 빛울림을 털어 칠까, 다섯까지 모아 동료를 띄울까
// 원작: 저학년 은은한 빛무늬(긍정의 기운 — 받는 피해↓ · 주는 피해↑, 위기에 회복) · 고학년 고요한 빛무리(적 진영 위 오로라 · 낙하 · 받는 피해↑)
//       · 강화 평타(다섯 번 맞으면 광역 반격) · 혼자 남겨지는 두려움 · 아우라 색 읽기 · 플라즈마 방화
// 시동: u2 아우라 읽기(그대로)
// ════════════════════════════════════════════════════════════════════
function aurora(j) {
  const H = '오로라', K = '빛울림', CUR = '오로라 장막';
  const h = j.heroes[0];
  h.blurb = '혼자 남겨지는 걸 두려워하는 다정한 방패. 맞을 때마다, 셋이 함께 움직인 턴마다 빛울림이 짙어지고, 다섯이 차면 동료의 다음 한 수에 극광을 실어 줍니다. 오로라를 불러내면 적 진영 위에 장막이 드리워 턴마다 빛의 비가 쏟아집니다.';
  h.keyword = { name: K, desc: '함께 있을 때 모이는 빛의 떨림', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.1 }], onMax: { empower: 'any', ratio: 0.5, consume: true },
    rules: [{ name: '혼자가 아니야', when: { on: 'fightStart' }, fx: [stk(CUR, 1), stk(K, 1)] }] };
  h.passives = [
    pas('다정한 빛마중', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }),
    pas('다정한 빛마중', 'play', [ifAll, stk(K, 1)], { when: { who: 'other' }, limit: 1 }),
    pas('긍정 주머니', 'lowHp', [heal(1.5), st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 갈래 부품 — 은은한 빛무늬(긍정의 기운): 실드 + 피해 감소 + 아군의 주는 피해 +15%(이번 턴)
    U(H, 1, '은은한 빛무늬', 1, '스킬', [sh(0.8), st('피해 감소', 1), dmod(0.15, 'otherAllies')], [
      'A', 'B',
      Or([sh(0.7), cleanse(1), dmod(0.2, 'otherAllies')]),
      Or([sh(0.6), st('피해 감소', 1), pw('turnStart', [dmod(0.1, 'otherAllies'), sh(0.3)])], { power: true }),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 시동 — 아우라 읽기(0): 빛울림 + 동료 카드 1장
    U(H, 2, '아우라 읽기', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], nm([
      Or([stk(K, 2), draw(1, { who: 'other' })]),
      Or([stk(K, 1), draw(1, { who: 'other' })], { tags: ['신속'] }),
      Or([stk(K, 1), draw(1, { who: 'other' }), cleanse(1)]),
      Or([stk(K, 1), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      Or([stk(K, 1), draw(2, { who: 'other' }), { k: 'discard', v: 1 }]),
    ]), null),
    // 쓰기 — 빛 조각 흩뿌리기: 적 전체 방어 기반 + 빛울림 1개당, 빛울림 전부 소모
    U(H, 3, '빛 조각 흩뿌리기', 1, '공격', [ddef(0.35, EA), per(K), ddef(0.1, EA), spendAll(K)], [
      'A',
      ['D', 'hurt', [ddef(0.12, EA)], { when: { guarded: true }, limit: 1 }],
      Or([ddef(0.6), per(K), ddef(0.18), spendAll(K)]),
      Or([ddef(0.32, EA), per(K), ddef(0.09, EA), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 원작 자유 — 고요한 빛무리: 적 진영 위 장막(다음 턴 시작에 빛의 비) + 실드 + 빛울림
    U(H, 4, '고요한 빛무리', 1, '스킬', [stk(CUR, 1), sh(0.6), stk(K, 1)], [
      'A', 'B',
      Or([stk(CUR, 1), sh(0.55), stk(K, 2)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([stk(CUR, 2), sh(0.9), { k: 'discard', v: 1 }]),
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 둘째 — 꺼지지 않는 불꽃: 방어 기반 + 빛울림 + 회복
    U(H, 5, '꺼지지 않는 불꽃', 1, '공격', [ddef(0.45), stk(K, 1), heal(0.3)], [
      'A', 'B',
      Or([ddef(0.4, EA), stk(K, 1), heal(0.25)]),
      ['D', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 }],
      'Hd',
    ], bl('power', 'heal', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 우이 — 손 만들기형 · 서포터 · 활발 · 엘다인. 바라는 걸 현실로 — 개굴비를 손에 지어 쌓아 두고(카드를 만들 때마다 행복 지수), 합창으로 한꺼번에 쏟는다.
//    행복 지수가 다섯이면 파티의 다음 카드에 웃음(강화). 개굴비를 하나씩 쓸까, 쥐고 쌓아 합창할까
// 원작: 바라는 것을 현실로 만드는 능력 · 저학년 개굴비(지속 회복 · 적 피해) · 고학년 말하는대로!(서로 다른 셋에게 서로 다른 효과) · 에루의 우산 · 싫은 건 안 보면 된다
// 우이(기억)과 가르기: 우이 = 만들어 손에 쥐는 쪽(생성 계기), 우이(기억) = 누구의 소멸이든 기억으로 받아들이는 쪽(소멸 계기)
// 엘다인 — 옛 48%(상한 41.9 위): 다 차면 「회복 + 사기(영구)」 터짐을 파티 다음 카드 강화로 낮췄다
// 시동: u1 개굴비 내리기(그대로)
// ════════════════════════════════════════════════════════════════════
function ui(j) {
  const H = '우이', K = '행복 지수', T1 = '우이_t1';
  const h = j.heroes[0];
  h.blurb = '바라는 것을 현실로 만드는 이슬비 정령. 개굴비를 손에 지어 하나씩 내리거나 쌓아 두었다 합창으로 쏟고, 지을 때마다 차오른 행복이 다섯이면 동료의 다음 한 수에 웃음을 실어 줍니다.';
  h.keyword = { name: K, desc: '바라는 걸 지을 때마다 차오르는 웃음', carrier: 'self', cap: 5, onMax: { empower: 'any', ratio: 0.4, consume: true } };
  h.passives = [
    pas('에루의 우산', 'make', [stk(K, 1)], { limit: 2 }),
    pas('싫은 건 안 보면 돼', 'lowHp', [cleanse(3), heal(1.0), make(T1, 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 시동 — 개굴비 내리기: 개굴비 둘 + 회복
    U(H, 1, '개굴비 내리기', 1, '스킬', [make(T1, 2), heal(0.5)], nm([
      Or([make(T1, 3), heal(0.5)]),
      Or([make(T1, 1), heal(0.4)], { cost: 0 }),
      Or([make(T1, 2), heal(0.7), cleanse(1)]),
      Or([make(T1, 1), pw('turnStart', [make(T1, 1)], { limit: 1 })], { power: true }),
      Or([make(T1, 3), heal(0.5), { k: 'discard', v: 1 }]),
    ]), null),
    // 둘째 — 소나기: 적 전체 + 개굴비
    U(H, 2, '소나기', 1, '공격', [dmg(0.6, EA), make(T1, 1)], [
      'A', 'B',
      Or([dmg(0.55, EA), make(T1, 1), st('약화', 1, EA)]),
      ['D', 'make', [dmg(0.2, EA)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 쓰기 — 개굴개굴 합창: 적 전체 + 손의 개굴비 1장당, 손의 개굴비 모두 소멸(쥐고 쌓았다 쏟기)
    U(H, 3, '개굴개굴 합창', 1, '공격', [dmg(0.45, EA), perTag(T1), dmg(0.25, EA), exile('hand', { all: true, tag: T1 })], [
      'A',
      ['D', 'make', [heal(0.2)], { limit: 2 }],
      Or([dmg(0.45, EA), perTag(T1), dmg(0.25, EA), st('약화', 1, EA)]),
      Or([dmg(0.4, EA), perTag(T1), dmg(0.22, EA), srch()]),
      'Hn',
    ], bl('power', 'weakSpot', [make(T1, 1)])),
    // 원작 자유 — 에루와 단짝: 개굴비 + 회복 + 손의 개굴비 1장당 회복(쓰지 않음)
    U(H, 4, '에루와 단짝', 1, '스킬', [make(T1, 1), heal(0.45), perTag(T1), heal(0.12)], [
      'A', 'B',
      Or([make(T1, 2), heal(0.4), perTag(T1), heal(0.12)]),
      ['D', 'turnStart', [heal(0.25)]],
      Or([make(T1, 1), heal(0.4), srch()]),
    ], bl('heal', 'draw', [make(T1, 1)])),
    // 갈래 부품 — 선생님한테 고자질: 개굴비 + 동료 카드 1장
    U(H, 5, '선생님한테 고자질', 1, '스킬', [make(T1, 1), draw(1, { who: 'other' })], [
      Or([make(T1, 2), draw(1, { who: 'other' })]),
      Or([make(T1, 1), draw(1, { who: 'other' })], { cost: 0 }),
      Or([make(T1, 1), draw(1, { who: 'other' }), dmod(0.15, 'otherAllies')]),
      Or([make(T1, 1), pw('make', [draw(1, { who: 'other' }), heal(0.4)], { limit: 1 })], { power: true }),
      Or([make(T1, 2), draw(2, { who: 'other' }), { k: 'discard', v: 1 }]),
    ], bl('draw', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 우이(기억) — 소멸형 · 서포터 · 냉정 · 엘다인. 지우지 않고 안고 간다 — 누구의 카드든 소멸하면 기억을 되찾고, 셋이 되면 세잎클로버(손에 카드).
//    되찾은 기억은 소멸 더미의 카드를 뽑을 더미로 돌려보내 받아들이고, 다 쏟아 떠나보낼지 쥐고 파티의 손끝을 벼릴지 고른다
// 원작: 저학년 세잎클로버(아군이 큰 한 대를 맞으면 잎 하나로 묶음) · 고학년 기억의 연못(쓸수록 3단계) · 강화 공격 에루가 아군을 옮겨 다니며 디버프 해제
//       · 결말 「지우지 않고 안고 가기로」 · 주말농장(잊힌 기억이 모이는 곳)
// 생성 카드 사도(묶음에서 새로 1명): 고유 효과 규칙 「세잎클로버」 가 클로버를 만든다 — 원작 저학년이 근거
// 우이와 가르기: 우이 = 지어 쥐는 손(생성 계기 · 개굴비 합창), 우이(기억) = 남의 소멸까지 받아들이는 쪽(소멸 계기 · 소멸 더미 되돌리기 · 클로버 방어)
// 시동: u2 행복 전파자(그대로)
// ════════════════════════════════════════════════════════════════════
function uiMem(j) {
  const H = '우이_기억', K = '되찾은 기억', CL = '우이_기억_clover';
  const h = j.heroes[0];
  h.blurb = '지웠던 불행한 기억을 안고 돌아온 본체. 누구의 카드든 소멸할 때마다 기억을 되찾고, 셋이 모이면 세잎클로버 잎 하나가 손에 돋습니다. 되찾은 기억으로 사라진 카드를 다시 받아들이고, 고학년에 모두 쏟아 연못을 엽니다.';
  h.keyword = {
    ...h.keyword,
    rules: [...h.keyword.rules, { name: '세잎클로버', when: { on: 'stackReach', id: K, n: 3 }, fx: [make(CL, 1)] },
      { name: '에루의 손길', when: { on: 'turnStart' }, conds: [{ c: 'stack', id: K, n: 3 }], fx: [cleanse(1), sh(0.3)] }],
  };
  h.passives = [
    pas('도망치지 않는 기억', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 3 }),
    pas('행복왕 우이', 'lowHp', [st('회피', 2), heal(1.0)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 원작 자유 — 잊으려던 기억(0 · 소멸): 2장 뽑기(제 몸을 지워 계기 하나)
    U(H, 1, '잊으려던 기억', 0, '스킬', [draw(2)], nm([
      Or([draw(2), stk(K, 1)], { tags: ['소멸'] }),
      Or([draw(2)], { tags: ['소멸', '신속'] }),
      Or([draw(1), make(CL, 1)], { tags: ['소멸'] }),
      Or([draw(2), srch()], { tags: ['소멸'] }),
      Or([draw(3), { k: 'discard', v: 1 }], { tags: ['소멸'] }),
    ]), L.bl('draw', 'ap', [stk(K, 1)]), ['소멸']),
    // 시동 — 행복 전파자: 회복 + 세잎클로버 둘
    U(H, 2, '행복 전파자', 1, '스킬', [heal(0.6), make(CL, 2)], nm([
      Or([heal(0.7), make(CL, 3)]),
      Or([heal(0.4), make(CL, 1)], { cost: 0 }),
      Or([heal(0.6), make(CL, 2), stk(K, 1)]),
      Or([heal(0.9), make(CL, 2), pw('exhaust', [sh(0.45)], { when: { who: 'any' }, limit: 2 })], { power: true }),
      Or([heal(0.9), make(CL, 3), { k: 'discard', v: 1 }]),
    ]), null),
    // 쓰기 — 떠나보내기: 피해 + 기억 1개당, 기억 전부 소모
    U(H, 3, '떠나보내기', 1, '공격', [dmg(0.75), per(K), dmg(0.25), spendAll(K)], [
      'A',
      ['D', 'exhaust', [dmg(0.25, ER)], { when: { who: 'any' }, limit: 1 }],
      Or([dmg(0.6, EA), per(K), dmg(0.18, EA), spendAll(K)]),
      Or([dmg(0.7), per(K), dmg(0.22), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 다시 마주한 기억: 소멸한 카드 1장을 뽑을 더미 맨 위로(받아들이기) + 기억 1개당 실드(쓰지 않음)
    U(H, 4, '다시 마주한 기억', 1, '스킬', [pull({ from: 'gone', to: 'top' }), per(K), sh(0.25)], [
      'A', 'B',
      Or([pull({ from: 'gone', to: 'top' }), per(K), heal(0.38)]),
      ['D', 'exhaust', [sh(0.5)], { when: { who: 'any' }, limit: 1 }],
      Or([pull({ from: 'gone', to: 'top' }), per(K), sh(0.22), draw(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 둘째 — 끝없는 클로버 밭: 적 전체 + 기억
    U(H, 5, '끝없는 클로버 밭', 1, '공격', [dmg(0.45, EA), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.4, EA), make(CL, 1)]),
      ['D', 'exhaust', [stk(K, 1), dmg(0.15, ER)], { when: { who: 'any' }, limit: 1 }],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 잉클 — 버리기형 · 딜러 · 냉정. 버려진 원고를 건져 먹을 입혀 다시 쓴다 — 자신의 카드가 버려질 때마다 무작위 적에게 먹빛, 버린 더미에서 원고를 골라 손으로.
//    먹빛을 펜 터치로 털까, 쌓아 두고 그 적을 무르게 할까
// 원작: 평타 획 긋기 → 먹빛(최대 3, 지울 수 없음) · 강화 평타 마무리 펜 터치(먹빛만큼, 모두 씀) · 저학년 먹물 세례 · 고학년 한 폭 수묵화
//       · 셰럼이 호수에 버린 동인지 → 나이아가 건져 잉클이 다시 읽고 고쳐 써 자기 이야기로(테마극장)
// 시동: u1 세상에 한 획(강화 개전) → u5 먹물 파스타(0 · 버리고 뽑기)
// ════════════════════════════════════════════════════════════════════
function inkle(j) {
  const H = '잉클', K = '먹빛';
  const h = j.heroes[0];
  h.blurb = '세상에 한 획 긋기가 소원인 먹물 정령. 버려진 원고를 건져 고쳐 쓰고, 자신의 카드가 버려질 때마다 먹이 번져 적을 무르게 합니다. 번진 먹은 마무리 펜 터치 한 획에 긋습니다.';
  h.keyword = { name: K, desc: '획마다 스며드는 먹물 얼룩', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.12 }] };
  h.passives = [
    pas('획 긋기', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('획 긋기', 'discard', [stk(K, 1, ER)], { limit: 2 }),
    pas('들어주길 바라오', 'fightStart', [dmod(0.2, 'self', 2)]),
  ];
  setOpener(j, H, 'u1', 'u5');
  setCards(j, [
    // 원작 자유 — 세상에 한 획(강화 · 한 폭 수묵화): 적 전체 먹빛 + 이 전투 동안 매 턴 시작 적 전체 먹빛
    U(H, 1, '세상에 한 획', 1, '강화', [stk(K, 1, EA), pw('turnStart', [stk(K, 1, EA)])], nm([
      Or([stk(K, 2, EA), pw('turnStart', [stk(K, 1, EA)])]),
      Or([pw('turnStart', [stk(K, 1, EA)])], { cost: 0 }),
      Or([stk(K, 1, EA), L.power(L.rule('turnStart', [stk(K, 1, EA)]), L.rule('discard', [stk(K, 1, ER)], { limit: 1 }))]),
      Or([stk(K, 1, EA), srch({ type: '공격' }), pw('turnStart', [stk(K, 1, EA)])]),
      Or([stk(K, 1, EA), { k: 'discard', v: 1 }, pw('turnStart', [stk(K, 1, EA), dmg(0.2, EA)])]),
    ]), bl('atkUp', 'cost', [stk(K, 1, E1)])),
    // 쓰기 — 마무리 펜 터치: 적 전체 + 적마다 제 먹빛 1개당, 먹빛 전부 소모
    U(H, 2, '마무리 펜 터치', 1, '공격', [dmg(0.5, EA), per(K, { each: true }), dmg(0.3, EA), { k: 'spend', id: K, all: true, target: EA }], [
      'A',
      ['D', 'discard', [dmg(0.2, EA)], { limit: 1 }],
      Or([dmg(0.9), per(K), dmg(0.5), { k: 'spend', id: K, all: true }]),
      Or([dmg(0.45, EA), per(K, { each: true }), dmg(0.27, EA), srch()]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 둘째 — 먹물 세례: 적 전체 + 먹빛. 안식(버려지면): 적 전체 먹빛
    U(H, 3, '먹물 세례', 1, '공격', [dmg(0.6, EA), stk(K, 1, EA), onDisc, stk(K, 1, EA)], [
      'A', 'B',
      Or([dmg(0.55, EA), st('약화', 1, EA), onDisc, stk(K, 1, EA)]),
      Or([dmg(0.5, EA), stk(K, 1, EA), pw('discard', [dmg(0.25, ER)], { limit: 1 })], { power: true }),
      Or([dmg(0.55, EA), srch(), onDisc, stk(K, 1, EA)]),
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 갈래 부품 — 고쳐 쓴 원고(0): 버린 더미 맨 위 카드 1장을 손으로 + 고른 적에게 먹빛
    U(H, 4, '고쳐 쓴 원고', 0, '스킬', [pull(), stk(K, 1, E1)], [
      Or([pull(), stk(K, 2, E1)]),
      Or([pull(), stk(K, 1, E1)], { tags: ['신속'] }),
      Or([pull({ n: 2 }), stk(K, 1, E1)]),
      Or([pull(), stk(K, 1, E1), pw('discard', [stk(K, 1, ER)], { limit: 1 })], { power: true }),
      Or([pull(), stk(K, 1, EA)]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1, E1)])),
    // 시동 — 먹물 파스타(0): 1장 버리고 2장 뽑기(버린 원고에 먹이 번진다)
    U(H, 5, '먹물 파스타', 0, '스킬', [{ k: 'discard', v: 1 }, draw(2)], nm([
      Or([{ k: 'discard', v: 1 }, draw(2), stk(K, 1, E1)]),
      Or([{ k: 'discard', v: 1 }, draw(2)], { tags: ['신속'] }),
      Or([{ k: 'discard', v: 1 }, draw(2), pull()]),
      Or([{ k: 'discard', v: 1 }, draw(2), pw('discard', [stk(K, 1, ER)], { limit: 1 })], { power: true }),
      Or([{ k: 'discard', v: 2 }, draw(3)]),
    ]), null),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '정령/시저': 1.3, '정령/실라': 1.15, '정령/아르코': 1.25, '정령/아일라': 1.3, '정령/오로라': 1.0, '정령/우이_기억': 0.75, '정령/잉클': 1.2 };
// ── 돌리기 ──
const JOBS = [
  ['정령/시저', caesar], ['정령/실라', sylla], ['정령/아르코', arco], ['정령/아일라', ayla],
  ['정령/오로라', aurora], ['정령/우이', ui], ['정령/우이_기억', uiMem], ['정령/잉클', inkle],
];
L.run18(JOBS, TUNE, new URL('./boost_정령B_18.json', import.meta.url));
