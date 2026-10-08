// 18갈래 재설계 3단계 — 용족B 묶음 7명(2026-10-08). 기준: 시범 17명(05_시범16_결과.md) · 지침 BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/용족B.md
// 실피르 · 아네트 · 아라그니아 · 오팔 · 제이드 · 키디언 · 피라
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장 · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀). 다 차면 저절로 터짐 없음(onMax make/empower).
// 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/용족B_18.mjs [사도 이름 일부]  → heroes/용족/<파일>.json (원본은 백업 SRC 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, sh, heal, st, stk, spendAll, per, draw, make, ap, ifStack, ifKill,
  TOP, ALLY, disc, srch, drawType, xtra, pw, pas, token, Or, bl, U, setCards, empower, spendN, tough, ifNth, ifFoe } = L;
const LOW = 'lowEnemy';
const onHandEnd = { k: 'when', on: 'handEnd' };   // 턴 끝에 손에 있으면
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));

// ════════════════════════════════════════════════════════════════════
// 1. 실피르 — 박자형 · 딜러 · 우울. 첫 자리는 동료에게 내주고 「정확히 둘째」 에 날을 세운다 — 둘째 자리 카드가 단검을 벌고, 다섯이면 다음 한 수에 몰아 던진다
// 원작: 자칭 2인자(늘 루드에게 한 끗 차) · 저학년 창공의 지배자(범위 + SP 감소) · 고학년 단검 4 · 6 · 8개 · 강화 평타 단검 3연투(마지막이 가장 셈) · 엑박스칼리버
// 시동: u2 2인자의 자존심(그대로)
// ════════════════════════════════════════════════════════════════════
function silpir(j) {
  const H = '실피르', K = '2인자의 단검';
  const h = j.heroes[0];
  h.blurb = '루드에게 늘 한 끗 차로 지는 사파이어의 용족, 자칭 2인자. 첫 자리는 동료에게 내주고 둘째 자리에 날을 세웁니다 — 단검이 다섯 모이면 다음 한 수에 몰아 던집니다.';
  h.keyword = { name: K, desc: '루드보다 하나 더 챙긴 단검', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }], onMax: { empower: 'next', ratio: 0.6, consume: true } };
  h.passives = [
    pas('넘버 투', 'play', [stk(K, 2)], { when: { who: 'any', nth: 2 }, limit: 1 }),
    pas('넘버 투', 'fightStart', [stk(K, 2)]),
    pas('한 끗 차', 'play', [xtra(0.45, ER)], { when: { type: '공격', nth: 2 }, limit: 1 }),
  ];
  setCards(j, [
    // 쓰기 — 단검 세례: 단일 + 단검 1개당 무작위 1타, 단검 전부 소모(다섯을 기다려 강화를 받을지)
    U(H, 1, '단검 세례', 1, '공격', [dmg(1.0), per(K), dmg(0.3, ER), spendAll(K)], [
      'A',
      ['D', 'play', [stk(K, 1)], { when: { nth: 2 }, limit: 1 }],
      Or([dmg(0.85, EA), per(K), dmg(0.22, ER), spendAll(K)]),
      Or([dmg(0.9), per(K), dmg(0.28, ER), srch()]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 2인자의 자존심: 단검 2 + 드로우, 둘째 카드면 단검 2 더
    U(H, 2, '2인자의 자존심', 1, '스킬', [stk(K, 2), draw(1), ifNth(2), stk(K, 2)], nm([
      Or([stk(K, 3), draw(1), ifNth(2), stk(K, 2)]),
      Or([stk(K, 2), draw(1)], { cost: 0 }),
      Or([stk(K, 3), draw(1, { who: 'other' }), ifNth(2), stk(K, 2)]),
      Or([stk(K, 3), draw(1), pw('play', [stk(K, 2)], { when: { nth: 2 }, limit: 1 })], { power: true }),
      Or([stk(K, 4), draw(1), disc(1)]),
    ]), null),
    // 원작 자유 — 창공의 지배자(신속): 적 전체 + 둔화, 둘째 카드면 단검 2
    U(H, 3, '창공의 지배자', 1, '공격', [dmg(0.8, EA), st('둔화', 1, EA), ifNth(2), stk(K, 2)], [
      'A', 'B',
      Or([dmg(0.85, EA), st('둔화', 1, EA), ifNth(2), st('약화', 1, EA)]),
      ['D', 'play', [st('둔화', 1, EA)], { when: { nth: 2 }, limit: 1 }],
      Or([dmg(0.75, EA), st('둔화', 1, EA), srch()]),
    ], bl('power', 'cost', [stk(K, 1)]), ['신속']),
    // 둘째 — 보석이라고 외쳐도: 단일 + 단검 1개당(쓰지 않음), 둘째 카드면 단검 연투(추가 공격)
    U(H, 4, '보석이라고 외쳐도', 1, '공격', [dmg(0.75), per(K), dmg(0.2), ifNth(2), xtra(0.6)], [
      'A',
      ['C', [st('취약', 1)]],
      Or([dmg(0.7), per(K), dmg(0.18), ifNth(2), stk(K, 2)]),
      ['D', 'extra', [stk(K, 1)], { limit: 1 }],
      'Hd',
    ], bl('atkUp', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 엑박스칼리버 탐색: 실드 + 동료 카드 1장(첫 자리를 내준다) + 단검
    U(H, 5, '엑박스칼리버 탐색', 1, '스킬', [sh(0.8), draw(1, { who: 'other' }), stk(K, 1)], [
      'A', 'B',
      Or([sh(0.6), draw(1, { who: 'other' }), st('협공', 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([sh(0.75), draw(1, { who: 'other' }), srch({ type: '공격' })]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 아네트 — 거드는 형 · 서포터 · 광기. 아군 하나를 오늘의 MVP 로 찍어 두고, MVP 가 칠 때마다 관중 함성 — 넷이면 파티의 다음 한 수에 몰아준다
// 원작: 저학년 MVP 예측(피해를 가장 많이 준 아군을 MVP — 공속 · 주는 피해↑) · 고학년 편파 중계(쓰레기 12번 · 센 적 출전 금지) · 평타 둘째마다 응원 회복 · 「허접~」
// 「턴 동안 사도별 피해 집계 → 1위」 는 엔진에 없다 — 고른 아군(찍기)으로 대신
// 시동: u4 싸움 구경(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function anette(j) {
  const H = '아네트', M = '오늘의 MVP', C = '관중 함성';
  const h = j.heroes[0];
  h.blurb = '결투를 붙이고 판정하는 구경꾼 석류석 용족. 아군 하나를 오늘의 MVP로 찍어 두면 MVP가 칠 때마다 관중석이 들끓고, 함성이 넷이면 파티의 다음 한 수가 한껏 달아오릅니다.';
  h.keyword = { ...h.keyword, per: [{ stat: 'dealt', v: 0.3, who: 'holder' }] };
  h.keywords = [{ name: C, desc: 'MVP가 칠 때마다 터지는 관중석의 함성', carrier: 'self', cap: 4, onMax: { empower: 'any', ratio: 0.5, consume: true } }];
  h.passives = [
    pas('관중석', 'fightStart', [stk(M, 1, 'strongestAlly'), stk(C, 2)]),
    pas('싸워라 싸워~', 'play', [stk(C, 1), heal(0.2)], { when: { marked: M }, limit: 2 }),
  ];
  setCards(j, [
    // 갈래 부품 — 결투 성사: 협공 + 고른 아군 MVP + 함성
    U(H, 1, '결투 성사', 1, '스킬', [st('협공', 1), stk(M, 1, ALLY), stk(C, 1)], [
      'A', 'B',
      Or([st('협공', 2), stk(M, 1, ALLY), draw(1)]),
      Or([st('협공', 2), stk(M, 1, ALLY), pw('play', [stk(C, 1)], { when: { marked: M, type: '공격' }, limit: 1 })], { power: true }),
      Or([st('협공', 2), stk(M, 1, ALLY), srch()]),
    ], bl('draw', 'ap', [stk(C, 1)])),
    // 원작 자유 — 허접~(연계): 단일 + 취약 2 + 함성
    U(H, 2, '허접~', 1, '공격', [dmg(0.8), st('취약', 2), stk(C, 1)], [
      'A', 'B',
      Or([dmg(0.8, EA), st('취약', 2, EA), st('약화', 1, EA)]),
      Or([dmg(1.2), st('취약', 2), pw('debuff', [stk(C, 1), dmg(0.25, ER)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'weakSpot', [stk(C, 1)]), ['연계']),
    // 쓰기 — MVP 예측: 고른 아군 MVP + 함성 1개당 실드, 함성 전부 소모(넷을 기다려 강화를 줄지)
    U(H, 3, 'MVP 예측', 1, '스킬', [stk(M, 1, ALLY), per(C), sh(0.32), spendAll(C)], [
      'A',
      ['D', 'turnStart', [stk(C, 1)], { limit: 1 }],
      Or([stk(M, 1, ALLY), per(C), heal(0.45), spendAll(C)]),
      Or([stk(M, 1, ALLY), per(C), sh(0.28), srch()]),
      'Hd',
    ], bl('guard', 'heal', [stk(C, 1)])),
    // 시동 — 싸움 구경(개전 강화): 사기 + MVP 가 공격하면 추가 공격(턴 1)
    U(H, 4, '싸움 구경', 1, '강화', [st('사기', 1), pw('play', [xtra(0.3)], { when: { marked: M, type: '공격' }, limit: 1 })], nm([
      Or([st('사기', 2), pw('play', [xtra(0.3)], { when: { marked: M, type: '공격' }, limit: 1 })], { tags: ['개전'] }),
      Or([st('사기', 1), pw('play', [xtra(0.25)], { when: { marked: M, type: '공격' }, limit: 1 })], { cost: 0, tags: ['개전'] }),
      Or([st('사기', 1), stk(C, 1), pw('play', [xtra(0.3)], { when: { marked: M, type: '공격' }, limit: 1 })], { tags: ['개전'] }),
      Or([st('사기', 1), pw('play', [xtra(0.3), stk(C, 1)], { when: { marked: M, type: '공격' }, limit: 1 })], { tags: ['개전'] }),
      Or([st('사기', 1), srch(), pw('play', [xtra(0.3)], { when: { marked: M, type: '공격' }, limit: 1 })], { tags: ['개전'] }),
    ]), null, ['개전']),
    // 둘째 — 결투 붙이는 꿀밤: 단일 + 고른 아군 MVP + 함성
    U(H, 5, '결투 붙이는 꿀밤', 1, '공격', [dmg(0.9), stk(M, 1, ALLY), stk(C, 1)], [
      'A', 'B',
      Or([dmg(0.9), stk(M, 1, ALLY), st('취약', 2)]),
      Or([dmg(1.0), stk(M, 1, ALLY), pw('play', [stk(C, 1), heal(0.1)], { when: { marked: M, type: '공격' }, limit: 1 })], { power: true }),
      Or([dmg(0.85), stk(M, 1, ALLY), srch()]),
    ], bl('power', 'cost', [stk(C, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 아라그니아 — 아껴 두기형 · 서포터 · 냉정. 산호를 씹어 진주를 빚고, 다친 백성(파티)에게 하나씩 날린다 — 다 내주지 않고 쥐어 둔 진주는
//    AP 를 남기거나 보존 카드를 쥐고 턴을 마칠 때 삼켜 껍질(실드)로
// 원작: 저학년 마음속의 펄(진주 6 → 다친 아군 회복, 지속이 끝나면 남은 진주를 삼켜 보호막 + SP) · 고학년 파도(적 셋 · 약화 · 보호막 파괴) · 「여왕은 백성을 버리지 않느니라」
// 봇이 keepAp 을 잘 안 고른다(수인B 보고) — 보존 카드 둘(u1 · u4)을 쥐고 넘기는 길로 keepAp 이 서게 했다
// 시동: u1 마음속의 펄(보존)
// ════════════════════════════════════════════════════════════════════
function aragnia(j) {
  const H = '아라그니아', K = '진주 구슬';
  const h = j.heroes[0];
  h.blurb = '진주에서 태어난 해룡 여왕. 산호를 씹어 진주를 빚고 다친 백성에게 하나씩 날려 보냅니다. 다 내주지 않고 쥐어 둔 진주는 턴을 마칠 때 삼켜 단단한 껍질이 됩니다.';
  h.passives = [
    pas('산호 씹기', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 }),
    pas('놓아주는 보석', 'keepAp', [per(K), sh(0.12)], { limit: 1 }),
  ];
  h.keyword = { ...h.keyword, rules: [...h.keyword.rules, { name: '여왕은 백성을 버리지 않느니라', when: { on: 'lowHp', pct: 0.3 }, fx: [stk(K, 3), st('불굴', 1)] }] };
  const keep = ['보존'];
  setCards(j, [
    // 시동 — 마음속의 펄(보존): 진주 3 + 실드
    U(H, 1, '마음속의 펄', 1, '스킬', [stk(K, 3), sh(0.6)], nm([
      Or([stk(K, 3), sh(0.8)], { tags: keep }),
      Or([stk(K, 2), sh(0.3)], { cost: 0, tags: keep }),
      Or([stk(K, 3), sh(0.5), onHandEnd, stk(K, 1)], { tags: keep }),
      Or([stk(K, 3), sh(0.6), pw('keepAp', [stk(K, 1), sh(0.3)], { limit: 1 })], { power: true, tags: [] }),
      Or([stk(K, 4), sh(0.6), disc(1)], { tags: keep }),
    ]), null, keep),
    // 쓰기 — 남은 구슬은 짐의 것: 진주 1개당 실드, 진주 전부 삼킴 + 드로우
    U(H, 2, '남은 구슬은 짐의 것', 1, '스킬', [per(K), sh(0.32), spendAll(K), draw(1)], [
      'A',
      Or([per(K), sh(0.36), draw(1), pw('keepAp', [sh(0.5)], { limit: 1 })], { power: true }),
      Or([per(K), heal(0.45), spendAll(K), draw(1)]),
      Or([per(K), sh(0.4), srch()]),
      'Hd',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 원작 자유 — 파도 한 줄기: 적 전체 + 약화 + 진주
    U(H, 3, '파도 한 줄기', 1, '공격', [dmg(0.55, EA), st('약화', 1, EA), stk(K, 1)], [
      'A', 'B',
      Or([{ k: 'strip' }, dmg(1.1), stk(K, 1)]),
      ['D', 'turnEnd', [stk(K, 1)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 진주 왕국의 보물고(보존): 실드 + 진주 1개당 실드, 턴 끝에 손에 있으면 진주 2(쥐고 넘기면 keepAp 도 선다)
    U(H, 4, '진주 왕국의 보물고', 1, '스킬', [sh(0.45), per(K), sh(0.1), onHandEnd, stk(K, 2)], [
      'A', 'B',
      Or([st('불굴', 1), sh(0.4), onHandEnd, stk(K, 2)], { tags: keep }),
      Or([sh(0.5), per(K), sh(0.12), pw('turnEnd', [stk(K, 1), sh(0.3)], { limit: 1 })], { power: true }),
      Or([sh(0.95), per(K), sh(0.22), onHandEnd, draw(1)], { tags: keep }),
    ], bl('guard', 'defUp', [stk(K, 1)]), keep),
    // 둘째 — 바다 뒤집기: 단일 + 진주, 진주가 넷 이상이면 약화
    U(H, 5, '바다 뒤집기', 1, '공격', [dmg(0.8), stk(K, 1), ifStack(K, 4), st('약화', 1)], [
      'A',
      ['C', [tough(1)]],
      Or([dmg(0.7, EA), stk(K, 1)]),
      ['D', 'keepAp', [dmg(0.3, ER)], { limit: 1 }],
      Or([dmg(0.75), stk(K, 1), srch()]),
    ], bl('power', 'heal', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 오팔 — 박자형 · 서포터 · 순수. 파티가 맞을 때마다 눈물, 셋이면 울음을 그치려 탭댄스(손에 카드 — 원래 만들던 장치). 이번 턴 셋째 자리를 오팔 카드로 밟으면
//    보석 파편이 튀고 파티의 다음 한 수가 반짝인다(세 번째마다 강화)
// 원작: 강화 평타 세 번째 공격마다 보석 → 파편 범위 · 슬픔을 쫓는 탭댄스 · 저학년 양산(피해↑ · 지속 회복) · 고학년 보석 가루(보호막 · 침묵)
// 시동: u2 가십 드래곤(그대로)
// ════════════════════════════════════════════════════════════════════
function opal(j) {
  const H = '오팔', K = '눈물 그렁', TAP = '오팔_tap';
  const h = j.heroes[0];
  h.blurb = '금방 울음을 터뜨리는 아기 오팔 용. 파티가 맞을 때마다 눈물이 맺히고, 셋이 차면 울음을 그치려 탭댄스를 춥니다. 이번 턴 셋째 자리를 오팔이 밟으면 보석 파편이 튀며 파티의 다음 한 수가 반짝입니다.';
  h.keyword = { name: K, desc: '선배님이 다칠 때마다 차오르는 눈물', carrier: 'self', cap: 3, onMax: { make: TAP, consume: true } };
  h.passives = [
    pas('울먹울먹', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    pas('파사삭 보석', 'play', [dmg(0.3, EA), empower(0.2, 'any')], { when: { nth: 3 }, limit: 1 }),
  ];
  const t0 = j.cards.find(c => c.id === TAP);
  const tokens = [{ ...t0, fx: [heal(0.5), st('협공', 1), draw(1)] }];
  setCards(j, [
    // 쓰기 — 선배님~!: 회복 + 눈물 1개당 회복, 눈물 전부(탭댄스를 기다릴지)
    U(H, 1, '선배님~!', 1, '스킬', [heal(0.6), per(K), heal(0.35), spendAll(K)], [
      'A',
      ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([heal(0.55), per(K), sh(0.4), spendAll(K)]),
      Or([heal(0.55), per(K), heal(0.3), srch()]),
      'Hx',
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 시동 — 가십 드래곤: 협공 + 실드 + 눈물
    U(H, 2, '가십 드래곤', 1, '스킬', [st('협공', 1), sh(1.0), stk(K, 1)], nm([
      Or([st('협공', 1), sh(1.4), stk(K, 1)]),
      Or([st('협공', 1), sh(0.6)], { cost: 0 }),
      Or([st('협공', 1), sh(1.45), ifNth(3), st('사기', 1)]),
      Or([st('협공', 1), sh(1.0), pw('play', [sh(0.35), stk(K, 1)], { when: { nth: 3 }, limit: 1 })], { power: true }),
      Or([st('협공', 2), sh(1.4), disc(1)]),
    ]), null),
    // 원작 자유 — 보석 던지기: 무작위 2타 + 눈물, 셋째 카드면 파편(적 전체)
    U(H, 3, '보석 던지기', 1, '공격', [dmg(0.55, ER, { hits: 2 }), stk(K, 1), ifNth(3), dmg(0.5, EA)], [
      'A', 'B',
      Or([dmg(0.5, ER, { hits: 3 }), ifNth(3), st('약화', 1, EA)]),
      ['D', 'play', [dmg(0.25, EA)], { when: { nth: 3 }, limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 반짝이는 모든 것들(0): 탭댄스 1장 + 실드(셋째 자리를 맞출 0코 두 장)
    U(H, 4, '반짝이는 모든 것들', 0, '스킬', [make(TAP, 1), sh(0.4)], [
      Or([make(TAP, 1), sh(0.5), stk(K, 1)]), 'B',
      Or([make(TAP, 1), st('사기', 1)]),
      ['D', 'make', [sh(0.3)], { limit: 1 }],
      Or([make(TAP, 1), sh(0.35), srch()]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 둘째 — 때찌때찌: 단일 + 눈물, 셋째 카드면 약화
    U(H, 5, '때찌때찌', 1, '공격', [dmg(1.0), stk(K, 1), ifNth(3), st('약화', 1)], [
      'A',
      ['C', [st('둔화', 1)]],
      Or([dmg(1.0), stk(K, 1), ifNth(3), xtra(0.6)]),
      ['D', 'turnStart', [stk(K, 1)], { limit: 1 }],
      Or([dmg(0.95), stk(K, 1), srch()]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 5. 제이드 — 손 만들기형 · 딜러 · 냉정. 아끼는 책(보존)은 손에 쥔 채 넘기며 비취옥을 모으고, 베껴 쓴 「필사본」(0코 · 소멸)만 내놓는다 —
//    비취옥 셋이면 다음 한 수에 마법서가 펼쳐진다(옥장판으로 털면 다시 처음부터)
// 원작: 마요의 귀한 책을 베껴 사본을 돌려주고 원본은 챙김 · 비취옥 1~3중첩(3중첩에서 쓰면 다 잃음) · 어사이드 3중첩에 또 얻으면 마법서 형상 · 「마음의 양식」
// 생성 카드 사도는 늘리지 않는다 — 필사본은 고유 카드(u2 신탁 · u3 신탁 · u4)가 만든다(장치 아님)
// 시동: u2 책에서 봤는데(보존, 그대로)
// ════════════════════════════════════════════════════════════════════
function jade(j) {
  const H = '제이드', K = '비취옥', COPY = '제이드_copy';
  const h = j.heroes[0];
  h.blurb = '절판 도서를 모으고 누구에게도 빌려주지 않는 비취의 용족 책벌레. 아끼는 책은 손에 쥔 채 베껴 쓴 필사본만 내놓고, 비취옥이 셋 차오르면 다음 한 수에 마법서가 펼쳐집니다.';
  h.keyword = { name: K, desc: '씹어 삼킨 옥돌의 마력', carrier: 'self', cap: 3, per: [{ stat: 'atk', v: 0.12 }], onMax: { empower: 'next', ratio: 0.4 } };
  h.passives = [
    pas('포도맛 옥구슬', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('마음의 양식', 'kill', [stk(K, 1), draw(1)], { when: { mine: true }, limit: 1 }),
  ];
  h.keyword.rules = [{ name: '사본은 돌려주고', when: { on: 'make' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] }];
  const tokens = [token(COPY, '필사본', H, '공격', [dmg(0.55), stk(K, 1)], { blurb: '원본은 챙기고 돌려주는 사본' })];
  setCards(j, [
    // 쓰기 — 게르마늄 옥장판(2): 적 전체 + 비취옥 1개당, 비취옥 전부
    U(H, 1, '게르마늄 옥장판', 2, '공격', [dmg(1.1, EA), per(K), dmg(0.38, EA), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.6), per(K), dmg(0.5), spendAll(K)]),
      ['D', 'make', [dmg(0.25, EA)], { limit: 1 }],
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 책에서 봤는데(보존): 비취옥 + 드로우 2, 턴 끝에 손에 있으면 비취옥
    U(H, 2, '책에서 봤는데', 1, '스킬', [stk(K, 1), draw(2), onHandEnd, stk(K, 1)], nm([
      Or([stk(K, 1), draw(2), onHandEnd, stk(K, 2)], { tags: ['보존'] }),
      Or([stk(K, 1), draw(1), onHandEnd, stk(K, 1)], { cost: 0, tags: ['보존'] }),
      Or([make(COPY, 1), draw(2), onHandEnd, stk(K, 1)], { tags: ['보존'] }),
      Or([stk(K, 2), draw(2), pw('make', [stk(K, 1), draw(1)], { limit: 1 })], { power: true, tags: [] }),
      Or([stk(K, 2), draw(3), disc(1)], { tags: ['보존'] }),
    ]), null, ['보존']),
    // 원작 자유 — 비취 구슬탄(보존): 단일 + 비취옥, 턴 끝에 손에 있으면 비취옥
    U(H, 3, '비취 구슬탄', 1, '공격', [dmg(1.2), stk(K, 1), onHandEnd, stk(K, 1)], [
      'A', 'B',
      Or([dmg(1.1), make(COPY, 1), onHandEnd, stk(K, 1)], { tags: ['보존'] }),
      ['D', 'turnEnd', [stk(K, 1)], { limit: 1 }],
      Or([dmg(1.1), stk(K, 1), srch()], { tags: ['보존'] }),
    ], bl('power', 'weakSpot', [stk(K, 1)]), ['보존']),
    // 만들기 — 초대 교주의 수양록: 단일 + 비취옥 1개당(쓰지 않음) + 필사본 1장
    U(H, 4, '초대 교주의 수양록', 1, '공격', [dmg(0.6), per(K), dmg(0.2), make(COPY, 1)], [
      'A',
      ['C', [make(COPY, 1)]],
      Or([dmg(0.8, EA), make(COPY, 1)]),
      Or([dmg(0.75), make(COPY, 1), pw('make', [stk(K, 1), dmg(0.15)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('atkUp', 'draw', [make(COPY, 1)])),
    // 갈래 부품 — 지식 독점(0 · 보존): 비취옥 + 고유 카드 1장, 턴 끝에 손에 있으면 비취옥
    U(H, 5, '지식 독점', 0, '스킬', [stk(K, 1), srch(), onHandEnd, stk(K, 1)], [
      'A',
      Or([stk(K, 2), srch(), onHandEnd, stk(K, 1)], { tags: ['보존', '신속'] }),
      Or([make(COPY, 1), srch()], { tags: ['보존'] }),
      ['D', 'turnStart', [stk(K, 1)], { limit: 1 }],
      Or([stk(K, 2), draw(2), disc(1)], { tags: ['보존'] }),
    ], bl('ap', 'draw', [stk(K, 1)]), ['보존']),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 6. 키디언 — 격파형 · 딜러 · 우울. 강인도를 깎아 무너뜨린 틈에, 가장 약한 적에게 그림자처럼 나타나 끊는다 — 끊으면 커진 채 다음 약한 적에게.
//    격파 · 처치마다 별빛, 넷이면 다음 칼이 더 날카롭다(강인도 · 격파 규칙은 그대로)
// 원작: 고학년 쉐도우 다이브(HP 비율 가장 낮은 적에 나타나 · 처치하면 피해가 커진 스킬을 다시) · 저학년 달려들기 · 흑요석(날카롭지만 잘 깨짐) · 별 관찰
// 시동: u2 급소 찾기(0, 그대로)
// ════════════════════════════════════════════════════════════════════
function kidian(j) {
  const H = '키디언', K = '별빛 조각';
  const h = j.heroes[0];
  h.blurb = '흑요석의 용족 닌자. 무너진 적의 틈에서, 가장 약한 적에게 그림자처럼 나타나 끊어 내고 — 끊을 때마다 별빛이 쌓여 다음 칼이 더 날카로워집니다.';
  h.keyword = { name: K, desc: '깨진 흑요석으로 그린 별자리', carrier: 'self', cap: 4, per: [{ stat: 'crit', v: 0.08 }, { stat: 'dealt', v: 0.05 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('그림자 추적', 'break', [stk(K, 2)], { limit: 1 }),
    pas('흑요석 단면', 'kill', [stk(K, 1), dmg(0.6, LOW)], { when: { mine: true }, limit: 1 }),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'dmg' && f.target === 'randomEnemy' ? { ...f, target: LOW } : f));
  setCards(j, [
    // 원작 자유 — 아웃사이드 커터: 3타 + 강인도 + 별빛
    U(H, 1, '아웃사이드 커터', 1, '공격', [dmg(0.4, E1, { hits: 3 }), tough(1), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.35, E1, { hits: 4 }), tough(1), ifFoe('broken'), stk(K, 2)]),
      Or([dmg(0.4, E1, { hits: 3 }), tough(1), pw('break', [stk(K, 2)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 급소 찾기(0): 강인도 + 별빛, 고른 적이 격파 상태면 별빛 1 더
    U(H, 2, '급소 찾기', 0, '스킬', [tough(1), stk(K, 1), ifFoe('broken'), stk(K, 1)], nm([
      Or([tough(1), stk(K, 2)]),
      Or([tough(1), stk(K, 1), ifFoe('broken'), stk(K, 2)], { tags: ['신속'] }),
      Or([tough(2), stk(K, 1), ifFoe('broken'), stk(K, 1)]),
      Or([tough(1), stk(K, 1), draw(1)]),
      Or([tough(1), stk(K, 2), pw('break', [stk(K, 1)], { limit: 1 })], { power: true }),
    ]), null),
    // 쓰기 — 밤하늘 다이브: 별빛 1개당(최소 1) 가장 약한 적 1타, 별빛 전부 · 처치하면 다음 약한 적에게 크게
    U(H, 3, '밤하늘 다이브', 1, '공격', [per(K, { n: 1 }), dmg(0.42, LOW), spendAll(K), ifKill, dmg(0.9, LOW)], [
      'A',
      ['D', 'kill', [dmg(0.4, LOW)], { when: { mine: true }, limit: 1 }],
      Or([per(K, { n: 1 }), dmg(0.38, LOW), ifKill, stk(K, 2)]),
      Or([per(K, { n: 1 }), dmg(0.4, LOW), spendAll(K), srch()]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 별 관찰: 잔광 + 단일, 고른 적이 격파 상태면 별빛 2
    U(H, 4, '별 관찰', 1, '공격', [st('잔광', 1), dmg(0.55), ifFoe('broken'), stk(K, 2)], [
      'A', 'B',
      Or([st('잔광', 1), dmg(0.5, EA), ifFoe('broken'), tough(1, EA)]),
      Or([st('잔광', 1), dmg(0.6), pw('break', [dmg(0.4, LOW), stk(K, 1)], { limit: 1 })], { power: true }),
      Or([st('잔광', 1), dmg(0.5), srch()]),
    ], bl('atkUp', 'draw', [stk(K, 1)])),
    // 둘째 — 그늘에서 내다보기: 실드 + 별빛 + 공격 카드 1장
    U(H, 5, '그늘에서 내다보기', 1, '스킬', [sh(0.75), stk(K, 1), drawType('공격')], [
      'A', 'B',
      Or([sh(0.9), tough(2), drawType('공격')]),
      ['D', 'turnStart', [stk(K, 1)], { limit: 1 }],
      Or([sh(1.2), stk(K, 2), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 피라 — 돈형 · 서포터 · 광기. 적에게 약점을 걸 때마다 「수금」 해 부유함을 벌고(벌이), 다섯이면 도금 명함을 찍어 낸다(사기 — 손에 카드).
//    쥐어 둔 부유함은 동료의 손끝을 금빛으로(아군 주는 피해 +), 털면 도금 경품을 사 들인다 — 쥘까 털까
// 원작: 저학년 수금 시간이다!(범위 + 받는 피해 증가 · 맞힌 적마다 부유함) · 고학년 피버☆타임(부유함이 가득이면 2배) · 어사이드 경품 · 위조 금화 · 황철석 코인
// 판 골드는 건드리지 않는다(BRIEF 돈형 규칙) — 전투 안 부유함만. 생성 카드 사도 +1(원래 있던 도금 명함 · 경품 토큰을 장치가 만든다)
// 시동: u1 연금술 실험(그대로)
// ════════════════════════════════════════════════════════════════════
function pira(j) {
  const H = '피라', K = '부유함', G1 = '피라_g1', G2 = '피라_g2';
  const h = j.heroes[0];
  h.blurb = '위조 금화를 끊고 진짜 화금석을 찾는 황철석의 용족 연금술사. 적에게 약점을 걸 때마다 수금해 부유함을 쌓고, 다섯이 차면 도금 명함을 찍어 냅니다. 쥐어 둔 부유함은 동료의 손끝을 금빛으로 달구고, 털면 도금 경품을 사 들입니다.';
  h.keyword = { name: K, desc: '번쩍이는 금 부스러기', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08, who: 'allies' }], onMax: { make: G1, consume: true } };
  h.passives = [
    pas('수금', 'debuff', [stk(K, 1)], { limit: 3 }),
    pas('햇살 아래에서', 'fightStart', [stk(K, 3)]),
  ];
  h.keyword.rules = [{ name: '경품 증정', when: { on: 'make' }, limit: { per: 'turn', n: 1 }, fx: [heal(0.25)] }];
  const g1 = j.cards.find(c => c.id === G1), g2 = j.cards.find(c => c.id === G2);
  const tokens = [
    { ...g1, cost: 0, fx: [dmg(1.4), st('취약', 1)] },
    { ...g2, cost: 0, fx: [sh(0.8), heal(0.35)] },
  ];
  setCards(j, [
    // 시동 — 연금술 실험: 취약 + 부유함 2 + 드로우
    U(H, 1, '연금술 실험', 1, '스킬', [st('취약', 1), stk(K, 2), draw(1)], nm([
      Or([st('취약', 1), stk(K, 3), draw(1)]),
      Or([st('취약', 1), stk(K, 1), draw(1)], { cost: 0 }),
      Or([st('취약', 1, EA), stk(K, 3), draw(1)]),
      Or([st('취약', 1), stk(K, 3), pw('debuff', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([st('취약', 2), stk(K, 4), disc(1)]),
    ]), null),
    // 원작 자유 — 수금 시간이다!: 적 전체 + 취약
    U(H, 2, '수금 시간이다!', 1, '공격', [dmg(0.85, EA), st('취약', 1, EA)], [
      'A', 'B',
      Or([dmg(0.8, EA), st('취약', 1, EA), stk(K, 1)]),
      Or([dmg(0.7, EA), st('취약', 1, EA), pw('debuff', [dmg(0.25, ER)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 위조 금화 뿌리기: 무작위 3타 + 부유함 1개당 무작위 1타, 부유함 전부(쥐고 동료를 달굴지)
    U(H, 3, '위조 금화 뿌리기', 1, '공격', [dmg(0.45, ER, { hits: 3 }), per(K), dmg(0.35, ER), spendAll(K)], [
      'A',
      ['D', 'make', [dmg(0.3, ER)], { limit: 1 }],
      Or([dmg(0.4, ER, { hits: 3 }), per(K), dmg(0.3, ER), st('취약', 1, EA)]),
      Or([dmg(0.4, ER, { hits: 3 }), per(K), dmg(0.3, ER), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품(사기) — 화금석 연구: 무작위 1타, 부유함이 셋 이상이면 셋을 털어 도금 경품 둘
    U(H, 4, '화금석 연구', 1, '공격', [dmg(0.6, ER), ifStack(K, 3), spendN(K, 3), make(G2, 2)], [
      'A', 'B',
      Or([sh(0.8), ifStack(K, 3), spendN(K, 3), make(G2, 3)]),
      Or([ifStack(K, 3), spendN(K, 3), make(G2, 3), pw('make', [dmg(0.3, ER)], { limit: 1 })], { power: true }),
      Or([st('사기', 1), ifStack(K, 3), spendN(K, 3), make(G2, 2)]),
    ], bl('atkUp', 'ap', [make(G2, 1)])),
    // 둘째 — 사채 감정: 취약 2 + 부유함 + 공격 카드 1장
    U(H, 5, '사채 감정', 1, '스킬', [st('취약', 2), stk(K, 1), drawType('공격')], [
      'A', 'B',
      Or([st('취약', 2, EA), stk(K, 2), drawType('공격')]),
      Or([st('취약', 2), stk(K, 2), pw('turnStart', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([st('취약', 3), stk(K, 2), drawType('공격', 2)]),
    ], bl('draw', 'cost', [stk(K, 1)])),
  ], tokens);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '용족/실피르': 1.3, '용족/피라': 1.3, '용족/아네트': 1.2, '용족/아라그니아': 1.15, '용족/오팔': 0.9 };
// ── 돌리기 ──
const JOBS = [
  ['용족/실피르', silpir], ['용족/아네트', anette], ['용족/아라그니아', aragnia], ['용족/오팔', opal],
  ['용족/제이드', jade], ['용족/키디언', kidian], ['용족/피라', pira],
];
L.run18(JOBS, TUNE, new URL('./boost_용족B_18.json', import.meta.url));
