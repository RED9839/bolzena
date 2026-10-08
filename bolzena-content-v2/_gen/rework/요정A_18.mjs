// 18갈래 재설계 3단계 — 요정A 묶음 8명(네르 · 네르(빡침) · 로니 · 마리 · 마요 · 마요(멋짐) · 에르핀 · 에르핀(왕도)). 2026-10-08
// 지침: _gen/rework/BRIEF_118.md · 기준: _measure/갈래_세분화/05_시범16_결과.md · 기록: _measure/갈래_세분화/06_118/요정A.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/요정A_18.mjs [사도 이름 일부]  → heroes/요정/<파일>.json (백업 heroes_before_118_20261008 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, hits, sh, heal, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, power, rule, later,
  TOP, ALLY, ALLIES, tough, disc, nextAp, empower, ifFoe, notStack, spendN, exile, form, dmod, srch, drawType, pw, pas, token,
  Or, five, bl, U, setCards, setOpener, renameKw } = L;
const ifHunted = { k: 'ifHunted' };
const pick = id => ({ k: 'spend', id, pick: true });
const perEv = { k: 'perEvent' };
const feed = (v, target) => ({ k: 'feed', v, ...(target ? { target } : {}) });
const payHp = v => ({ k: 'payHp', v });
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));

// ════════════════════════════════════════════════════════════════════
// 1. 네르 — 거드는 형 · 서포터 · 광기. 졸다 들킨 기도가 셋이 차면 「꿈속 계시」 카드 — 계시를 먼저 깔고 그 턴에 아군 카드를 몰아 낸다
// 원작: 저학년 세계수의 계시(아군 전원 피해↑, 짧음) · 계시를 받고 일주일을 잠(설정) · 어사이드 「스킬 중 무적」 · 오함마 건달 시절 · 세계수의 도끼
// 시동: u1 꿈으로 올리는 기도(0코) — 그대로
// ════════════════════════════════════════════════════════════════════
function ner(j) {
  const H = '네르', K = '꾸벅 기도', DREAM = '네르_dream';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '졸다 들키면 우기는 기도', carrier: 'self', cap: 3, onMax: { make: DREAM, consume: true } };
  delete h.keywords;
  h.passives = [
    pas('잠이 아니라 기도', 'turnStart', [stk(K, 1)]),
    pas('사제장의 무적권', 'play', [st('피해 감소', 1)], { when: { type: '스킬' }, limit: 1 }),
  ];
  const tokens = [token(DREAM, '꿈속 계시', H, '스킬', [dmod(0.3, ALLIES), sh(0.5), draw(1)], { blurb: '꾸벅 졸던 사제장이 받아 낸 세계수의 계시 — 이번 턴만 아군이 힘을 냅니다' })];
  setCards(j, [
    // 시동 — 기도 1 · 드로우
    U(H, 1, '꿈으로 올리는 기도', 0, '스킬', [stk(K, 1), draw(1)], nm([
      Or([stk(K, 2), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1), draw(1), sh(0.5)], { tags: ['신속'] }),
      Or([stk(K, 2), srch({ type: '스킬' })], { tags: ['신속'] }),
      Or([stk(K, 1), draw(1), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { tags: ['신속'], power: true }),
      Or([stk(K, 3), draw(1), disc(1)], { tags: ['보존'] }),
    ]), null, ['신속']),
    // 쓰기 — 세계수의 계시: 이번 턴 아군 전원 주는 피해 +, 기도가 2 이상이면 더 크게 · 전부 소모
    U(H, 3, '세계수의 계시', 1, '스킬', [dmod(0.25, ALLIES), ifStack(K, 2), dmod(0.25, ALLIES), spendAll(K)], nm([
      Or([dmod(0.3, ALLIES), ifStack(K, 2), dmod(0.3, ALLIES), spendAll(K)]),
      Or([dmod(0.15, ALLIES), ifStack(K, 2), dmod(0.2, ALLIES), spendAll(K)], { cost: 0 }),
      Or([dmod(0.25, ALLIES), sh(0.6), ifStack(K, 2), make(DREAM, 1)]),
      Or([dmod(0.25, ALLIES), sh(0.6), pw('turnStart', [dmod(0.15, ALLIES)])], { power: true }),
      Or([payHp(40), dmod(0.35, ALLIES), ifStack(K, 2), dmod(0.35, ALLIES)]),
    ]), bl('draw', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 여왕님 앞은 못 지나가요(강화): 피해 감소 · 다른 아군이 카드를 내면 기도(턴 2번) — 몰아 내기가 계시를 부른다
    U(H, 4, '여왕님 앞은 못 지나가요', 1, '강화', [st('피해 감소', 1), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 2 })], nm([
      Or([st('피해 감소', 2), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 2 })]),
      Or([pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 2 })], { cost: 0 }),
      Or([sh(0.8), pw('play', [stk(K, 1), sh(0.2)], { when: { who: 'other' }, limit: 2 })]),
      Or([st('피해 감소', 1), srch(), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 2 })]),
      Or([st('피해 감소', 2), disc(1), pw('play', [stk(K, 1), sh(0.3)], { when: { who: 'other' }, limit: 2 })]),
    ]), bl('defUp', { tags: ['개전'] }, [stk(K, 1)])),
    // 원작 자유 — 뒷골목 오함마(건달 시절): 방어 기반, 기도를 쥐고 있으면 더(쓰지 않음)
    U(H, 2, '뒷골목 오함마', 1, '공격', [ddef(0.8), ifStack(K, 2), ddef(0.45)], [
      'A', ['D', 'turnStart', [ddef(0.2)]],
      Or([ddef(0.6, EA), ifStack(K, 2), tough(1)], { tags: ['분쇄'] }),
      Or([ddef(0.75), stk(K, 1), srch({ type: '스킬' })], { tags: ['분쇄'] }),
      ['Ht', '분쇄'],
    ], bl('power', 'cost', [stk(K, 1)]), ['분쇄']),
    // 둘째 — 세계수의 도끼: 방어 기반 광역 + 기도
    U(H, 5, '세계수의 도끼', 1, '공격', [ddef(0.55, EA), stk(K, 1)], [
      'A', 'B',
      Or([ddef(1.0), tough(1), stk(K, 1)]),
      Or([ddef(0.5, EA), stk(K, 1), srch()]),
      'Hd',
    ], bl('guard', 'weakSpot', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 2. 네르(빡침) — 쌓고 고르기 · 딜러 · 활발 · 엘다인. 맞아서 치미는 빡침을 쥐고 세지거나(주는 피해 +), 거대 도끼창에 고른 만큼 싣는다 — 다섯이 차면 「심판의 도끼창」 카드
// 원작: 저학년 성전 선포(휘장 · 범위 · 아군 받는 피해 일부 전이) · 고학년 빡침 상태(평타 3타) · 강화 평타 거대 도끼창(지난 시간 준 피해 비례 폭발)
// 엘다인 한 단계: 다 차면 카드 + 고학년 · 「성전 모드」 카드로만 여는 성전 모드(저절로 변신 없음)
// 시동: u1 성전 선포 — 그대로
// ════════════════════════════════════════════════════════════════════
function nerRage(j) {
  const H = '네르_빡침', K = '빡침', F = '네르_빡침_성전', AXE = '네르_빡침_axe';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '여왕을 지키려다 치미는 분노', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08 }], onMax: { make: AXE, consume: true } };
  h.passives = [
    pas('분노의 공명', 'hurt', [stk(K, 1)], { limit: 3 }),
    pas('가끔은 그리운 순간', 'lowHp', [stk(K, 3), st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  const tokens = [token(AXE, '심판의 도끼창', H, '공격', [dmg(0.75, EA), st('피해 감소', 1)], { blurb: '참고 참은 빡침이 거대 도끼창 한 번에 터집니다' })];
  setCards(j, [
    // 시동 — 성전 선포: 적 전체 + 피해 감소(휘장) + 빡침 2
    U(H, 1, '성전 선포', 1, '공격', [dmg(0.6, EA), st('피해 감소', 1), stk(K, 2)], nm([
      Or([dmg(0.85, EA), st('피해 감소', 1), stk(K, 2)]),
      Or([dmg(0.55, EA), stk(K, 2)], { cost: 0 }),
      Or([dmg(0.5, EA), stk(K, 2), srch({ type: '공격' })]),
      Or([dmg(0.5, EA), stk(K, 2), pw('hurt', [stk(K, 1)], { limit: 2 })], { power: true }),
      Or([dmg(0.9, EA), stk(K, 3), payHp(40)]),
    ]), null),
    // 쓰기 — 거대 도끼창(2): 빡침을 고른 만큼 소모, 1개당 적 전체 한 번 더
    U(H, 2, '거대 도끼창', 2, '공격', [dmg(1.1, EA), pick(K), perEv, dmg(0.28, EA)], nm([
      Or([dmg(1.45, EA), pick(K), perEv, dmg(0.34, EA)]),
      Or([dmg(1.0, EA), pick(K), perEv, dmg(0.26, EA)], { cost: 1 }),
      Or([dmg(1.9), pick(K), perEv, dmg(0.45)]),
      Or([dmg(1.0, EA), pick(K), perEv, dmg(0.28, EA)], { tags: ['보존'] }),
      Or([dmg(1.0, EA), per(K), dmg(0.22, EA), spendN(K, 1)]),
    ]), bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 교주님한테서 손 떼!(연계): 2회 + 빡침
    U(H, 3, '교주님한테서 손 떼!', 1, '공격', [dmg(0.32, E1, { hits: 2 }), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.3, E1, { hits: 3 }), stk(K, 1)], { tags: ['연계'] }),
      Or([dmg(0.3, E1, { hits: 2 }), stk(K, 1), srch()], { tags: ['연계'] }),
      ['Ht', '연계'],
    ], bl('draw', 'weakSpot', [stk(K, 1)]), ['연계']),
    // 갈래 부품 — 양익의 맹세: 빡침 1만 쓰고 피해, 아직 남았으면 드로우(나눠 쓰기)
    U(H, 4, '양익의 맹세', 1, '공격', [dmg(0.9), spendN(K, 1), ifStack(K, 1), draw(1)], nm([
      Or([dmg(1.2), spendN(K, 1), ifStack(K, 1), draw(1)]),
      Or([dmg(0.6, EA), spendN(K, 1), ifStack(K, 1), draw(1)]),
      Or([dmg(0.8), spendN(K, 1), pw('play', [dmg(0.25, EA)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([dmg(0.8), stk(K, 1), srch({ type: '공격' })]),
      Or([dmg(1.4), spendN(K, 2), disc(1)]),
    ]), bl('guard', 'ap', [stk(K, 1)])),
    // 원작 자유 — 성전 모드: 빡침이 3 이상이면 2 쓰고 성전 모드(고학년 빡침 상태 — 참격 3회)
    U(H, 5, '성전 모드', 1, '스킬', [draw(1), ifStack(K, 3), spendN(K, 2), form(F)], nm([
      Or([draw(2), ifStack(K, 3), spendN(K, 2), form(F)]),
      Or([draw(1), ifStack(K, 3), spendN(K, 2), form(F)], { cost: 0 }),
      Or([sh(1.4), ifStack(K, 3), spendN(K, 2), form(F)]),
      Or([sh(1.3), stk(K, 2), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([draw(2), disc(1), ifStack(K, 3), form(F)]),
    ]), bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 3. 로니 — 표적형 · 딜러 · 광기. 가장 위험한 놈 하나를 현상범으로 — 현상금을 쥐고 몰아 칠까(받는 피해 +), 석양의 결투로 한 번에 받아 낼까
// 원작: 무법자 지정(해제 불가 · 하나뿐 · 쓰러지면 다시) · 진압용 소닉붐(무법자면 받는 피해 + 침묵) · 수사 종결 · 부러진 리코더 · 막대사탕
// 시동: u1 진압용 소닉붐 — 그대로
// ════════════════════════════════════════════════════════════════════
function roni(j) {
  const H = '로니', K = '현상금';
  const h = j.heroes[0];
  const kw = h.keyword;
  kw.rules = kw.rules.map(r => (r.name === '석양의 결투' ? { ...r, fx: [dmg(0.7, E1)] } : r));
  setCards(j, [
    // 시동 — 진압용 소닉붐: 피해 + 현상금 2
    U(H, 1, '진압용 소닉붐', 1, '공격', [dmg(0.85), stk(K, 2, E1)], nm([
      Or([dmg(1.15), stk(K, 2, E1)]),
      Or([dmg(0.6), stk(K, 1, E1)], { cost: 0 }),
      Or([dmg(0.8), stk(K, 2, E1), ifHunted, st('취약', 1)]),
      Or([dmg(0.7), stk(K, 2, E1), srch({ type: '공격' })]),
      Or([dmg(1.3), stk(K, 3, E1), disc(1)]),
    ]), null),
    // 쓰기 — 석양의 결투: 현상금 1개당, 그 적의 현상금 전부 소모
    U(H, 2, '석양의 결투', 1, '공격', [dmg(0.85), per(K), dmg(0.3), { k: 'spend', id: K, all: true, target: E1 }], nm([
      Or([dmg(1.15), per(K), dmg(0.38), { k: 'spend', id: K, all: true, target: E1 }]),
      Or([dmg(0.8), per(K), dmg(0.28), { k: 'spend', id: K, all: true, target: E1 }], { cost: 0 }),
      Or([dmg(0.7), per(K), dmg(0.26), ifKill, ap(1)]),
      Or([dmg(0.6), per(K), dmg(0.2), pw('huntDown', [draw(1)], { limit: 1 })], { power: true }),
      Or([dmg(0.8), per(K), dmg(0.3), srch()]),
    ]), bl('power', 'cost', [stk(K, 1, E1)])),
    // 원작 자유 — 부러진 리코더: 2회, 현상금 3 이상이면 취약
    U(H, 3, '부러진 리코더', 1, '공격', [dmg(0.6, E1, { hits: 2 }), ifStack(K, 3), st('취약', 1)], [
      'A', 'B',
      Or([dmg(0.5, E1, { hits: 3 }), ifStack(K, 2), st('취약', 1)]),
      Or([dmg(0.55, E1, { hits: 2 }), pw('play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 갈래 부품 — 불침번: 그 적이 공격하려 하면 현상금 1개당(쓰지 않음 — 쥐는 쪽)
    U(H, 4, '불침번', 1, '공격', [dmg(0.85), ifFoe('attack'), per(K), dmg(0.22)], [
      'A',
      ['C', [stk(K, 1, E1)]],
      Or([dmg(0.7), per(K), dmg(0.16)]),
      Or([dmg(0.6), sh(0.6), pw('foeActBefore', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      'Hn',
    ], bl('guard', 'weakSpot', [stk(K, 1, E1)])),
    // 둘째 — 오래가는 막대사탕(0): 현상금 1 · 드로우
    U(H, 5, '오래가는 막대사탕', 0, '스킬', [stk(K, 1, E1), draw(1)], nm([
      Or([stk(K, 2, E1), draw(1)]),
      Or([stk(K, 1, E1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1, E1), sh(1.2)]),
      Or([stk(K, 1, E1), srch({ type: '공격' })]),
      Or([stk(K, 2, E1), draw(2), disc(1)]),
    ]), bl('draw', 'ap', [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 마리 — 예약형 · 딜러 · 활발. 여기저기 폭탄을 묻어 두고(「폭파 실험」) 마리가 누를 때 한꺼번에 — 지금 누를까, 더 묻을까, 신관을 걸어 다음 턴에 맡길까
// 원작: 고학년 「발파 합니다!」(고폭탄 설치 → 폭발) · 저학년 특제 폭탄(범위 + 화상) · 강화 평타 강화 폭탄 · 지하 통로 · 「숨겨 둔 한 방」 자폭
// 시동: u3 황금손의 추억(0코) — 그대로
// ════════════════════════════════════════════════════════════════════
function mari(j) {
  const H = '마리', K = '폭파 실험', BOMB = '마리_bomb';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '곳곳에 묻어 두고 누를 날만 기다리는 폭약', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.05 }] };
  h.passives = [
    pas('강화 폭탄', 'play', [make(BOMB, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('황금손 마리', 'kill', [make(BOMB, 1), draw(1)], { when: { mine: true }, limit: 1 }),
  ];
  const tokens = [token(BOMB, '특제 폭탄', H, '공격', [dmg(0.3, EA), stk(K, 1)], { blurb: '던지면 조금 터지고, 나머지는 땅속에 묻혀 기폭을 기다립니다' })];
  const boom = (r, t = EA) => [per(K, { n: 1 }), dmg(r, t), spendAll(K)];
  setCards(j, [
    // 저학년 — 폭탄 배달 왔어용~: 적 전체 + 잔불 + 폭탄 묻기
    U(H, 1, '폭탄 배달 왔어용~', 1, '공격', [dmg(0.65, EA), st('잔불', 1, EA), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(1.1), st('잔불', 2), stk(K, 2)]),
      Or([dmg(0.6, EA), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 기폭 스위치: 묻은 폭탄 1개당 적 전체, 전부 소모
    U(H, 2, '기폭 스위치', 1, '스킬', boom(0.36), nm([
      Or(boom(0.46)),
      Or(boom(0.3), { cost: 0 }),
      Or([per(K, { n: 1 }), dmg(0.6), spendAll(K), ifKill, make(BOMB, 1)]),
      Or([per(K, { n: 1 }), dmg(0.3, EA), pw('reserveFire', [stk(K, 2)], { limit: 1 })], { power: true }),
      Or([per(K, { n: 1 }), dmg(0.32, EA), spendN(K, 3)]),
    ]), bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 황금손의 추억(0): 폭탄 카드 · 드로우
    U(H, 3, '황금손의 추억', 0, '스킬', [make(BOMB, 1), draw(1)], nm([
      Or([make(BOMB, 2), draw(1)], { tags: ['신속'] }),
      Or([make(BOMB, 1), stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([make(BOMB, 2), srch()], { tags: ['신속'] }),
      Or([make(BOMB, 1), draw(1), sh(0.5)], { tags: ['신속'] }),
      Or([make(BOMB, 2), draw(2), disc(1)]),
    ]), null, ['신속']),
    // 원작 자유 — 숨겨 둔 한 방: HP 를 치르고 묻은 폭탄을 크게(자폭 장면)
    U(H, 4, '숨겨 둔 한 방', 1, '공격', [payHp(50), ...boom(0.6)], nm([
      Or([payHp(50), ...boom(0.78)]),
      Or([payHp(30), ...boom(0.5)]),
      Or([payHp(50), per(K, { n: 1 }), dmg(1.0), spendAll(K)]),
      Or([payHp(40), per(K, { n: 1 }), dmg(0.5, EA), pw('pay', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([payHp(80), ...boom(0.95)]),
    ]), bl('guard', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 막힌 길 뚫기: 피해 + 신관(다음 턴 시작에 묻은 폭탄 전부 기폭)
    U(H, 5, '막힌 길 뚫기', 1, '공격', [dmg(0.75), later(1, boom(0.3)), stk(K, 1)], nm([
      Or([dmg(1.0), later(1, boom(0.36)), stk(K, 1)], { tags: ['분쇄'] }),
      Or([dmg(0.7), later(1, boom(0.3)), stk(K, 2)], { tags: ['분쇄'] }),
      Or([dmg(0.6, EA), later(1, boom(0.3)), make(BOMB, 1)], { tags: ['분쇄'] }),
      Or([dmg(0.7), later(1, boom(0.3)), srch()], { tags: ['분쇄'] }),
      Or([dmg(1.3), stk(K, 3), disc(1)], { tags: ['분쇄'] }),
    ]), bl('power', 'weakSpot', [stk(K, 1)]), ['분쇄']),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 5. 마요 — 돈형 · 딜러 · 광기. 전당포 장부(「감정가」)에 값을 벌어 두었다가 「사기」 — 감정가를 치르고 수집품을 손에 들인다. 손에 쥔 수집품이 많을수록 흐흐…
// 원작: 전당포 임 · 움직이지 않는 것만 모으는 수집광 · 경매 낙찰 · 저학년 독침 3연타 + 중독(원작 자유 카드) · 고학년 무작위 8연타 + 중독
// 돈형(전투 골드 없음): 감정가를 벌어(처치 · 공격 · 값 매기기) 소모해 수집품 카드를 만든다(make). 판 골드는 건드리지 않는다
// 시동: u5 값 매기기(0코, 옛 시동 u1 수집의 법칙임. → 은총으로)
// ════════════════════════════════════════════════════════════════════
function mayo(j) {
  const H = '마요', K = '감정가', OLD = '마취독', TROPHY = '마요_trophy';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '전당포 장부에 적어 둔 수집품 값', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.06 }], onMax: { make: TROPHY, consume: true } };
  delete h.keywords;
  h.passives = [
    pas('마취 독침', 'play', [st('고통', 1, E1), stk(K, 1)], { when: { type: '공격' }, limit: 3 }),
    pas('전당포임', 'kill', [stk(K, 2)], { when: { mine: true }, limit: 1 }),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.id === OLD ? st('고통', 2, EA) : f));
  const tokens = [token(TROPHY, '박제 수집품', H, '스킬', [st('고통', 3, E1), sh(0.5), draw(1)], { blurb: '더는 움직이지 않는 수집품 — 쥐고 있으면 마요가 흐뭇해함' })];
  setOpener(j, H, 'u1', 'u5');
  setCards(j, [
    // 원작 자유 — 수집의 법칙임.(저학년): 3연타 + 고통, 처치면 감정가
    U(H, 1, '수집의 법칙임.', 1, '공격', [dmg(0.45, E1, { hits: 3 }), st('고통', 2), ifKill, stk(K, 2)], [
      'A', ['D', 'kill', [stk(K, 1)], { when: { mine: true }, limit: 1 }],
      Or([dmg(0.42, ER, { hits: 4 }), st('고통', 1, EA)]),
      Or([dmg(0.45, E1, { hits: 3 }), st('고통', 2), stk(K, 2)]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기(사기) — 경쟁자 견제: 피해, 감정가 3을 치르고 수집품 1장
    U(H, 2, '경쟁자 견제', 1, '공격', [dmg(0.85), ifStack(K, 3), spendN(K, 3), make(TROPHY, 1)], nm([
      Or([dmg(1.15), ifStack(K, 3), spendN(K, 3), make(TROPHY, 1)], { tags: ['분쇄'] }),
      Or([dmg(0.95), ifStack(K, 2), spendN(K, 2), make(TROPHY, 1)], { tags: ['분쇄'] }),
      Or([dmg(1.2), ifStack(K, 4), spendN(K, 4), make(TROPHY, 2)], { tags: ['분쇄'] }),
      Or([dmg(1.1), pw('make', [dmg(0.55)], { limit: 1 })], { tags: ['분쇄'], power: true }),
      Or([dmg(0.9, EA), ifStack(K, 3), spendN(K, 3), make(TROPHY, 1)], { tags: ['분쇄'] }),
    ]), bl('power', 'cost', [stk(K, 1)]), ['분쇄']),
    // 둘째 — 마취 독침 세례: 무작위 4회 + 적 전체 고통
    U(H, 3, '마취 독침 세례', 1, '공격', [dmg(0.3, ER, { hits: 4 }), st('고통', 1, EA)], [
      'A', 'B',
      Or([dmg(0.3, ER, { hits: 4 }), stk(K, 2)]),
      Or([dmg(0.25, ER, { hits: 4 }), st('고통', 1, EA), srch({ type: '공격' })]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 흐흐…: 손의 수집품 1장당 한 번 더(쥐는 쪽)
    U(H, 4, '흐흐…', 1, '공격', [dmg(0.75), perTag(TROPHY), dmg(0.4)], [
      'A', ['D', 'make', [dmg(0.35)], { limit: 1 }],
      Or([dmg(0.6, EA), perTag(TROPHY), dmg(0.28, EA)]),
      Or([dmg(0.7), perTag(TROPHY), dmg(0.35), srch()]),
      'Hd',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 시동 — 값 매기기(0): 감정가 2 · 드로우
    U(H, 5, '값 매기기', 0, '스킬', [stk(K, 2), draw(1)], nm([
      Or([stk(K, 3), draw(1)]),
      Or([stk(K, 2), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 2), make(TROPHY, 1)]),
      Or([stk(K, 2), srch({ type: '공격' })]),
      Or([stk(K, 4), draw(1), disc(1)]),
    ]), null),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 6. 마요(멋짐) — 쌓고 고르기 · 딜러 · 순수. 아군 스킬마다 「자랑하고 싶음」 — 쥐면 세지고(주는 피해 +), 털 땐 고른 만큼, 여섯이 차면 「열 번 튕기는 방패」 카드
// 원작: 저학년 다섯 번 튕기는 방패 · 다른 아군 저학년마다 자랑 1, 10이면 SP 없이 열 번 튕기는 방패 · 은방울꽃 종 · 어사이드 멋진 요정
// 시동: u1 빨리 나 칭찬해줌.(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function mayoCool(j) {
  const H = '마요_멋짐', K = '자랑하고 싶음', TEN = '마요_멋짐_ten';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '멋지다는 말이 듣고 싶은 마음', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.06 }], onMax: { make: TEN, consume: true } };
  h.passives = [
    pas('칭찬 요구', 'play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 3 }),
    pas('훌륭한 수집품', 'lowHp', [{ k: 'cleanse', v: 1 }, heal(1), stk(K, 3)], { when: { pct: 0.3 } }),
  ];
  const tokens = [token(TEN, '열 번 튕기는 방패', H, '공격', [dmg(0.3, ER, { hits: 6 })], { blurb: '모아 둔 자랑을 한꺼번에 — 경매장 방패가 적 사이를 마구 튕깁니다' })];
  setCards(j, [
    // 시동 — 빨리 나 칭찬해줌.(개전 강화): 자랑 2 · 드로우 · 매 턴 자랑
    U(H, 1, '빨리 나 칭찬해줌.', 1, '강화', [stk(K, 1), draw(1), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(1.7), pw('play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), sh(0.9), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 원작 자유 — 은방울꽃 종: 피해 + 약화 + 자랑
    U(H, 2, '은방울꽃 종', 1, '공격', [dmg(0.95), st('약화', 1), stk(K, 1)], [
      'A', ['D', 'spend', [st('약화', 1, EA)], { when: { id: K }, limit: 1 }],
      Or([dmg(0.65, EA), st('약화', 1, EA), stk(K, 1)]),
      Or([dmg(0.9), st('약화', 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 최강의 수집품임.: 무작위 3회, 자랑을 고른 만큼 소모해 1개당 한 번 더
    U(H, 3, '최강의 수집품임.', 1, '공격', [dmg(0.3, ER, { hits: 3 }), pick(K), perEv, dmg(0.3, ER)], nm([
      Or([dmg(0.38, ER, { hits: 3 }), pick(K), perEv, dmg(0.38, ER)]),
      Or([dmg(0.3, ER, { hits: 2 }), pick(K), perEv, dmg(0.28, ER)], { cost: 0 }),
      Or([dmg(0.5, E1, { hits: 3 }), pick(K), perEv, dmg(0.5)]),
      Or([dmg(0.3, ER, { hits: 3 }), pick(K), perEv, dmg(0.3, ER)], { tags: ['보존'] }),
      Or([dmg(0.3, ER, { hits: 3 }), per(K), dmg(0.2, ER), ifStack(K, 4), draw(1)]),
    ]), bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 나만의 교주: 자랑 1개당(쓰지 않음 — 쥐는 보너스)
    U(H, 4, '나만의 교주', 1, '공격', [dmg(0.3, ER, { hits: 2 }), per(K), dmg(0.14, ER)], [
      'A', ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.5), per(K), dmg(0.2), ifStack(K, 4), st('사기', 1)]),
      Or([dmg(0.28, ER, { hits: 2 }), per(K), dmg(0.12, ER), srch()]),
      'Hn',
    ], bl('guard', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 수집품 발동: 자랑 · 다른 아군 스킬 2장 뽑기, 자랑이 5 이상이면 사기
    U(H, 5, '수집품 발동', 1, '스킬', [stk(K, 1), draw(2, { who: 'other', type: '스킬' })], nm([
      Or([stk(K, 1), draw(2, { who: 'other', type: '스킬' }), ifStack(K, 4), st('사기', 1)]),
      Or([stk(K, 1), draw(1, { who: 'other', type: '스킬' })], { cost: 0 }),
      Or([stk(K, 1), draw(2, { who: 'other', type: '스킬' }), sh(0.8)]),
      Or([stk(K, 1), draw(2, { who: 'other', type: '스킬' }), pw('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 })], { power: true }),
      Or([stk(K, 3), draw(2, { who: 'other', type: '스킬' }), disc(1)]),
    ]), bl('draw', 'ap', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 7. 에르핀 — 손 만들기형 · 딜러 · 순수. 케이크를 먹을까(배부름 — 주먹이 무거워짐) 굶을까(배고파 예민 — 다음 카드 +) · 손의 케이크는 탄알로 쏟아도 된다
// 원작: 저학년 마력탄 폭주(무작위 4발) · 강화 평타 몰래 케이크 먹기 · 어사이드 무한의 케이크 · 산을 뽑아 던지는 괴력 · 배고프면 예민
// 시동: u2 무한의 케이크 — 그대로
// ════════════════════════════════════════════════════════════════════
function erpin(j) {
  const H = '에르핀', K = '배부름', CAKE = '에르핀_cake';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '케이크로 채운 여왕의 배', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.05 }] };
  h.passives = [
    pas('달달한 게 최고야!', 'play', [make(CAKE, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('무전취식', 'kill', [make(CAKE, 2)], { when: { mine: true }, limit: 1 }),
  ];
  const tokens = [token(CAKE, '케이크', H, '스킬', [heal(0.35), stk(K, 1)], { blurb: '친구 몰래 꺼내 먹는 케이크 — 먹으면 배가 부르고, 안 먹으면 탄알이 됩니다' })];
  setCards(j, [
    // 쓰기(쏟기) — 마력탄 폭주: 무작위 3발 + 손의 케이크 1장당 한 발, 손의 케이크 전부 소멸
    U(H, 1, '마력탄 폭주', 1, '공격', [dmg(0.4, ER, { hits: 3 }), perTag(CAKE), dmg(0.5, ER), exile('hand', { all: true, tag: CAKE })], nm([
      Or([dmg(0.5, ER, { hits: 3 }), perTag(CAKE), dmg(0.62, ER), exile('hand', { all: true, tag: CAKE })]),
      Or([dmg(0.4, ER, { hits: 4 }), perTag(CAKE), dmg(0.45, ER)]),
      Or([dmg(0.4, EA), perTag(CAKE), dmg(0.35, EA), exile('hand', { all: true, tag: CAKE })]),
      Or([dmg(0.4, ER, { hits: 3 }), perTag(CAKE), dmg(0.4, ER), pw('make', [dmg(0.25, ER)], { limit: 2 })], { power: true }),
      Or([dmg(0.38, ER, { hits: 3 }), perTag(CAKE), dmg(0.45, ER), srch()]),
    ]), bl('power', 'draw', [make(CAKE, 1)])),
    // 시동 — 무한의 케이크: 케이크 2 · 드로우
    U(H, 2, '무한의 케이크', 1, '스킬', [make(CAKE, 2), draw(1)], nm([
      Or([make(CAKE, 3), draw(1)]),
      Or([make(CAKE, 1), draw(1)], { cost: 0 }),
      Or([make(CAKE, 2), draw(1), sh(0.6)]),
      Or([make(CAKE, 2), pw('turnStart', [make(CAKE, 1)])], { power: true }),
      Or([make(CAKE, 3), draw(2), disc(1)]),
    ]), null),
    // 둘째 — 순수 케이크 공격!!!(연계): 피해 + 케이크
    U(H, 3, '순수 케이크 공격!!!', 1, '공격', [dmg(0.75), make(CAKE, 1)], [
      'A', ['D', 'play', [make(CAKE, 1)], { when: { type: '스킬' }, limit: 1 }],
      Or([dmg(0.55, EA), make(CAKE, 1)], { tags: ['연계'] }),
      Or([dmg(0.6), make(CAKE, 1), srch()], { tags: ['연계'] }),
      'Hd',
    ], bl('guard', 'cost', [make(CAKE, 1)]), ['연계']),
    // 원작 자유 — 맨주먹 결계 부수기(2): 배부름 1개당, 전부 소모(먹은 만큼 무겁게)
    U(H, 4, '맨주먹 결계 부수기', 2, '공격', [dmg(1.7), per(K), dmg(0.42), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.3, EA), per(K), dmg(0.3, EA), spendAll(K)], { tags: ['분쇄'] }),
      Or([dmg(1.4), per(K), dmg(0.35), pw('spend', [make(CAKE, 1)], { when: { id: K }, limit: 1 })], { tags: ['분쇄'], power: true }),
      ['Ht', '분쇄'],
    ], bl('power', 'weakSpot', [make(CAKE, 1)]), ['분쇄']),
    // 갈래 부품 — 배고픈 여왕(0): 케이크 1, 배부름이 없으면(굶었으면) 다음 카드 강화
    U(H, 5, '배고픈 여왕', 0, '스킬', [make(CAKE, 1), draw(1), notStack(K), empower(0.4)], nm([
      Or([make(CAKE, 2), draw(1), notStack(K), empower(0.6)]),
      Or([make(CAKE, 1), draw(1), notStack(K), empower(0.6)], { tags: ['신속'] }),
      Or([make(CAKE, 2), draw(2)]),
      Or([make(CAKE, 1), srch({ type: '공격' }), sh(0.6)]),
      Or([spendAll(K), heal(1.3), empower(1.0)]),
    ]), bl('draw', 'ap', [make(CAKE, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 8. 에르핀(왕도) — 쌓고 고르기 · 딜러 · 순수 · 엘다인. 왕마력을 바닥까지 쏟아 난사하고, 쓰러뜨리면 다시 채운다 — 다섯이 차면 파티의 다음 카드에 「모두를 위한 힘」
// 원작: 저학년 마력 난타(SP 바닥까지 무작위 난사 · 처치하면 SP 환급 · 그동안 받는 피해↓) · 고학년 에르피엔 펀치 · 어사이드 아군 피해↑ · 반쪽 빵
// 엘다인 한 단계: 다 차면 파티 다음 카드 강화(옛 저절로 터짐 dealtMod 를 바꿈) + 처치 환급
// 시동: u3 축복의 왕관(0코) — 그대로
// ════════════════════════════════════════════════════════════════════
function erpinRoyal(j) {
  const H = '에르핀_왕도', K = '왕마력';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '되찾은 세계수의 힘', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08 }], onMax: { empower: 'any', ratio: 0.5, consume: true } };
  h.passives = [
    pas('망설임 없는 왕도', 'fightStart', [stk(K, 2)]),
    pas('망설임 없는 왕도', 'turnStart', [stk(K, 1)]),
    pas('망설임 없는 왕도', 'kill', [stk(K, 2)], { when: { mine: true }, limit: 2 }),
    pas('모두를 위한 배려', 'lowHp', [{ k: 'cleanse', v: 1 }, st('면역', 1)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 둘째 — 에르피엔 왕마력탄: 피해 + 왕마력
    U(H, 1, '에르피엔 왕마력탄', 1, '공격', [dmg(1.0), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.65, EA), stk(K, 1)], { tags: ['약점 공격'] }),
      Or([dmg(0.9), stk(K, 1), srch()], { tags: ['약점 공격'] }),
      ['Ht', '약점 공격'],
    ], bl('power', 'draw', [stk(K, 1)]), ['약점 공격']),
    // 쓰기 — 마력 난타: 왕마력 1개당 무작위 적 한 발, 전부 소모 · 처치하면 왕마력 2(다시 채움)
    U(H, 2, '마력 난타', 0, '공격', [per(K, { n: 1 }), dmg(0.5, ER), spendAll(K), ifKill, stk(K, 2)], nm([
      Or([per(K, { n: 1 }), dmg(0.56, ER), spendAll(K), ifKill, stk(K, 2)]),
      Or([per(K, { n: 1 }), dmg(0.42, ER), spendAll(K), ifKill, stk(K, 2)], { tags: ['신속'] }),
      Or([per(K, { n: 1 }), dmg(0.46, ER), spendAll(K), st('피해 감소', 1)]),
      Or([per(K, { n: 1 }), dmg(0.42, ER), spendAll(K), srch()]),
      Or([per(K, { n: 2 }), dmg(0.5, ER), spendAll(K), ifKill, stk(K, 3)], { tags: ['보존'] }),
    ]), bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 축복의 왕관(0): 왕마력 · 드로우
    U(H, 3, '축복의 왕관', 0, '스킬', [stk(K, 1), draw(1)], nm([
      Or([stk(K, 2), draw(1)]),
      Or([stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 2), sh(0.8)]),
      Or([stk(K, 1), srch({ type: '공격' })]),
      Or([stk(K, 3), draw(1), disc(1)]),
    ]), null),
    // 갈래 부품 — 모두를 위한 무게(강화): 왕마력을 소모할 때마다 적 전체(턴 1번)
    U(H, 4, '모두를 위한 무게', 1, '강화', [stk(K, 1), pw('spend', [dmg(0.35, EA)], { when: { id: K }, limit: 1 })], nm([
      Or([stk(K, 2), pw('spend', [dmg(0.45, EA)], { when: { id: K }, limit: 1 })]),
      Or([pw('spend', [dmg(0.3, EA)], { when: { id: K }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 1), sh(0.8), pw('spend', [dmg(0.3, EA), st('피해 감소', 1)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 1), srch(), pw('spend', [dmg(0.35, EA)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 3), nextAp(-1), pw('spend', [dmg(0.35, EA)], { when: { id: K }, limit: 1 })]),
    ]), bl('defUp', { tags: ['개전'] }, [stk(K, 1)])),
    // 원작 자유 — 반쪽 빵(0): 왕마력 1을 고른 아군에게 나눠 주고 그 아군 카드 뽑기
    U(H, 5, '반쪽 빵', 0, '스킬', [stk(K, 1), feed(1, ALLY), draw(1, { who: 'other' })], nm([
      Or([stk(K, 2), feed(1, ALLY), draw(1, { who: 'other' })]),
      Or([feed(1, ALLY), draw(2, { who: 'other' })], { tags: ['신속'] }),
      Or([stk(K, 2), heal(0.9), feed(1, ALLY)]),
      Or([feed(1, ALLY), draw(1, { who: 'other' }), pw('turnStart', [feed(1, ALLY)])], { power: true }),
      Or([stk(K, 1), feed(2, ALLIES), disc(1)]),
    ]), bl('heal', 'ap', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '요정/네르': 1.3, '요정/네르_빡침': 1.15, '요정/로니': 1.1, '요정/마요': 1.4, '요정/마요_멋짐': 1.2, '요정/에르핀': 1.25, '요정/에르핀_왕도': 1.25 };

// ── 돌리기 ──
const JOBS = [
  ['요정/네르', ner], ['요정/네르_빡침', nerRage], ['요정/로니', roni], ['요정/마리', mari],
  ['요정/마요', mayo], ['요정/마요_멋짐', mayoCool], ['요정/에르핀', erpin], ['요정/에르핀_왕도', erpinRoyal],
];
L.run18(JOBS, TUNE, new URL('./boost_요정A_18.json', import.meta.url));
