// 18갈래 재설계 3단계 — 요정B 묶음 9명(슈팡 · 스키아 · 에슈르 · 에슈르(마도) · 카렌 · 칸타 · 클로에 · 파트라 · 폴랑). 2026-10-08
// 지침: _gen/rework/BRIEF_118.md · 기준: _measure/갈래_세분화/05_시범16_결과.md · 기록: _measure/갈래_세분화/06_118/요정B.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// 생성 카드 사도: 원래 만들던 클로에는 그대로 + 새로 파트라 하나(원작 「민트 케이크 150개를 만들어 놓고 퇴근」 — 턴 시작 민트).
// node _gen/rework/요정B_18.mjs [사도 이름 일부]  → heroes/요정/<파일>.json (백업 heroes_before_118_20261008 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ifStack, ifKill,
  ALLIES, tough, disc, nextAp, gauge, ifNth, spendN, exile, form, formEnd, dmod, srch, pw, pas, token, cs, perCs, perOver, ifGained, burn,
  Or, bl, U, setCards } = L;
const pick = id => ({ k: 'spend', id, pick: true });
const perEv = { k: 'perEvent' };
const perPlayed = { k: 'perPlayed' };
const ifAll = { k: 'ifAllHeroes' };
const handEnd = { k: 'when', on: 'handEnd' };
const payHp = v => ({ k: 'payHp', v });
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
const blurbOf = (j, id) => j.cards.find(c => c.id === id)?.blurb;

// ════════════════════════════════════════════════════════════════════
// 1. 슈팡 — 박자형 · 서포터 · 활발. 한 턴에 몰아 내서 배달 건수를 채운다 — 넷째 장이면 과속 딱지(AP), 속도가 넷이면 파티의 다음 카드가 질주
// 원작: 저학년 무책임 배달부(우편을 흘릴 때마다 아군 회복) · 고학년 슈팡 배송 · 케이크 위 150회 스핀 · 「⋯슈팡」 쌍손가락 · 어사이드 시작 고학년
// 다 차면: 옛 「슈파볼트 질주」(저절로 회복 + AP) → 파티의 다음 카드 +40%(onMax empower any)
// 시동: u1 무책임 배달부 — 그대로
// ════════════════════════════════════════════════════════════════════
function shupang(j) {
  const H = '슈팡', K = '배달 속도', PARCEL = '슈팡_parcel';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '슈파볼트를 모는 속도광의 속도', carrier: 'self', cap: 4, onMax: { empower: 'any', ratio: 0.3, consume: true } };
  h.passives = [
    pas('과속 배달', 'play', [stk(K, 1)], { when: { who: 'any', tag: '신속' }, limit: 3 }),
    pas('배달의 요정', 'fightStart', [gauge(30), st('결의', 1)]),
  ];
  const tokens = [token(PARCEL, '택배 상자', H, '공격', [dmg(0.3, ER), heal(0.2)], { tags: ['신속', '소멸'], blurb: blurbOf(j, PARCEL) })];
  setCards(j, [
    // 시동 — 무책임 배달부: 회복 + 속도 + 택배 상자
    U(H, 1, '무책임 배달부', 1, '스킬', [heal(0.7), stk(K, 1), make(PARCEL, 1)], nm([
      Or([heal(1.0), stk(K, 2), make(PARCEL, 1)], { tags: ['신속'] }),
      Or([heal(0.5), stk(K, 1), make(PARCEL, 1)], { cost: 0, tags: ['신속'] }),
      Or([heal(1.2), perPlayed, heal(0.4), make(PARCEL, 1)], { tags: ['신속'] }),
      Or([heal(0.9), make(PARCEL, 2), srch()], { tags: ['신속'] }),
      Or([heal(1.0), make(PARCEL, 2), disc(1)], { tags: ['신속'] }),
    ]), null, ['신속']),
    // 갈래 부품 — 슈팡은⋯ 달리고 싶다!(0): 속도 · 드로우, 이번 턴 넷째 카드면 AP +1(과속 딱지)
    U(H, 2, '슈팡은⋯ 달리고 싶다!', 0, '스킬', [stk(K, 1), draw(1), ifNth(4), L.ap(1)], nm([
      Or([stk(K, 2), draw(1), ifNth(4), L.ap(1)], { tags: ['신속'] }),
      Or([stk(K, 2), draw(1), ifNth(3), L.ap(1)], { tags: ['신속'] }),
      Or([stk(K, 1), draw(1), pw('play', [stk(K, 1), heal(0.15)], { when: { who: 'any', nth: 4 } })], { tags: ['신속'], power: true }),
      Or([stk(K, 2), draw(1), srch()], { tags: ['신속'] }),
      Or([stk(K, 2), draw(2), disc(1)], { tags: ['신속'] }),
    ]), bl('draw', 'ap', [stk(K, 1)]), ['신속']),
    // 원작 자유 — 케이크 위 코너링: 적 전체(방어 기반), 이번 턴 낸 카드 1장당 한 번 더
    U(H, 3, '케이크 위 코너링', 1, '공격', [ddef(0.3, EA), perPlayed, ddef(0.09, EA)], [
      'A', ['D', 'play', [ddef(0.12, EA)], { when: { who: 'any', nth: 4 } }],
      Or([ddef(0.3, EA), perPlayed, ddef(0.09, EA), st('약화', 1, EA)], { tags: ['신속'] }),
      Or([ddef(0.28, EA), perPlayed, ddef(0.08, EA), srch()], { tags: ['신속'] }),
      ['Ht', '신속'],
    ], bl('power', 'cost', [stk(K, 1)]), ['신속']),
    // 쓰기 — 손가락 인사: 약화 2, 속도 1개당 무작위 적 한 번(방어 기반), 전부 소모 — 넷 채워 파티에 줄까, 지금 털까
    U(H, 4, '손가락 인사', 1, '공격', [st('약화', 2), per(K, { n: 1 }), ddef(0.2, ER), spendAll(K)], [
      'A', ['D', 'turnStart', [stk(K, 1)]],
      Or([st('약화', 1, EA), per(K, { n: 1 }), ddef(0.15, EA), spendAll(K)], { tags: ['신속'] }),
      Or([st('약화', 2), per(K, { n: 1 }), ddef(0.18, ER), srch()], { tags: ['신속'] }),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)]), ['신속']),
    // 둘째 — 야, 타!(0): 속도 + 택배 상자
    U(H, 5, '야, 타!', 0, '스킬', [stk(K, 1), make(PARCEL, 1)], nm([
      Or([stk(K, 1), make(PARCEL, 2)], { tags: ['신속'] }),
      Or([stk(K, 2), make(PARCEL, 1)], { tags: ['신속'] }),
      Or([make(PARCEL, 1), heal(0.7), stk(K, 1)], { tags: ['신속'] }),
      Or([stk(K, 1), make(PARCEL, 2), pw('make', [heal(0.25)], { limit: 2 })], { tags: ['신속'], power: true }),
      Or([make(PARCEL, 3), nextAp(-1)], { tags: ['신속'] }),
    ]), bl('ap', { tags: ['보존'] }, [make(PARCEL, 1)]), ['신속']),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 2. 스키아 — 아껴 두기형 · 딜러 · 광기. 입을 다문 턴(AP를 남기거나 보존 카드를 쥐고 마침)마다 감탄사를 삼킨다 — 넷이 차면 터져 나와 다음 카드 +50%
// 원작: 묵언수행(대사 없음) · 「흐읍!?」 새어 나오는 감탄사 · 저학년 오래된 맹세(언약의 매듭) · 강화 평타 벼락 · 고학년 매듭 폭발
// 동료가 공격하면 묵언이 깨져 1 사라짐(옛 규칙 그대로 — 끊기형). 언약의 매듭(고학년 장치)은 그대로
// 시동: u2 …(끄덕)(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function skia(j) {
  const H = '스키아', K = '삼킨 감탄사';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword, per: [{ stat: 'dealt', v: 0.15 }], onMax: { empower: 'next', ratio: 0.5 } };
  delete h.keyword.rules;
  h.passives = [
    pas('묵언 수행', 'fightStart', [stk(K, 1)]),
    pas('묵언 수행', 'keepAp', [stk(K, 1)]),
    pas('소리 없는 기도', 'spend', [draw(1)], { when: { id: K }, limit: 1 }),
  ];
  setCards(j, [
    // 원작 자유 — 오래된 맹세(저학년, 보존): 피해 + 감탄사
    U(H, 1, '오래된 맹세', 1, '공격', [dmg(0.95), stk(K, 1)], [
      'A', ['D', 'keepAp', [dmg(0.3)], { limit: 1 }],
      Or([dmg(0.6, EA), stk(K, 1), st('약화', 1, EA)], { tags: ['보존'] }),
      Or([dmg(0.85), stk(K, 1), srch({ type: '공격' })], { tags: ['보존'] }),
      ['Ht', '보존'],
    ], bl('power', 'draw', [stk(K, 1)]), ['보존']),
    // 시동 — …(끄덕)(개전 강화): 감탄사 · 드로우 · 입을 다문 턴마다 감탄사
    U(H, 2, '…(끄덕)', 1, '강화', [stk(K, 1), draw(1), pw('keepAp', [stk(K, 1)])], nm([
      Or([stk(K, 2), draw(1), pw('keepAp', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('keepAp', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(1.2), pw('keepAp', [stk(K, 1), sh(0.3)])], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('keepAp', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 쓰기 — 흐읍!?: 적 전체, 감탄사 1개당 한 번 더, 전부 소모
    U(H, 3, '흐읍!?', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.2, EA), spendAll(K)], nm([
      Or([dmg(0.8, EA), per(K), dmg(0.26, EA), spendAll(K)]),
      Or([dmg(1.0), per(K), dmg(0.38), spendAll(K)]),
      Or([dmg(0.5, EA), per(K), dmg(0.17, EA), pw('spend', [dmg(0.25, EA)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([dmg(0.55, EA), per(K), dmg(0.18, EA), srch()]),
      Or([payHp(40), dmg(0.85, EA), per(K), dmg(0.28, EA)]),
    ]), bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 지켜 온 금기: 감탄사 1개당(쓰지 않음 — 쥐는 쪽)
    U(H, 4, '지켜 온 금기', 1, '공격', [dmg(0.8), per(K), dmg(0.22)], [
      'A', ['D', 'keepAp', [stk(K, 1), dmg(0.2)], { limit: 1 }],
      Or([dmg(0.6, EA), per(K), dmg(0.16, EA)]),
      Or([dmg(0.7), per(K), dmg(0.2), srch()]),
      'Hd',
    ], bl('weakSpot', 'ap', [stk(K, 1)])),
    // 둘째 — 몸짓 대화(0, 보존): 감탄사 · 드로우, 턴 끝에 손에 있으면 감탄사(쥘까 낼까)
    U(H, 5, '몸짓 대화', 0, '스킬', [stk(K, 1), draw(1), handEnd, stk(K, 1)], nm([
      Or([stk(K, 2), draw(1), handEnd, stk(K, 1)], { tags: ['보존'] }),
      Or([stk(K, 1), draw(2), handEnd, stk(K, 1)], { tags: ['보존'] }),
      Or([stk(K, 1), sh(1.3), handEnd, stk(K, 1)], { tags: ['보존'] }),
      Or([stk(K, 3), srch(), handEnd, stk(K, 1)], { tags: ['보존'] }),
      Or([stk(K, 3), draw(2), disc(1)]),
    ]), bl('draw', { tags: ['신속'] }, [stk(K, 1)]), ['보존']),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 에슈르 — 손 만들기형 · 딜러 · 우울. 빵을 구워 손에 쥐고, 먹어서(내서) 센디오를 장전하거나 그리모어에 한꺼번에 쏟는다
// 원작: 저학년 빵 6개 날리기 · 어사이드 저학년 뒤 평타 6회가 불타는 빵 · 고학년 거대 케이크 · 남은 빵을 못 버림 · 「빵집 아니고 마법학교」
// 에슈르(마도)와 가름 — 이쪽은 빵(손 · 장전), 마도는 실패작을 태워 연구가 자람
// 시동: u1 빵템피드 — 그대로
// ════════════════════════════════════════════════════════════════════
function eshur(j) {
  const H = '에슈르', K = '빵 센디오', BREAD = '에슈르_bread';
  const h = j.heroes[0];
  h.passives = [
    pas('빵빵한 브레드', 'fightStart', [stk(K, 3)]),
    pas('빵집 아니고 마법학교', 'kill', [stk(K, 2)], { when: { mine: true }, limit: 1 }),
  ];
  const tokens = [token(BREAD, '갓 구운 빵', H, '스킬', [stk(K, 1), heal(0.25)], { blurb: blurbOf(j, BREAD) })];
  setCards(j, [
    // 시동 — 빵템피드: 3연타 + 센디오 + 빵
    U(H, 1, '빵템피드', 1, '공격', [dmg(0.3, E1, { hits: 3 }), stk(K, 1), make(BREAD, 1)], nm([
      Or([dmg(0.4, E1, { hits: 3 }), stk(K, 1), make(BREAD, 1)]),
      Or([dmg(0.27, E1, { hits: 3 }), make(BREAD, 1)], { cost: 0 }),
      Or([dmg(0.3, ER, { hits: 4 }), stk(K, 1), make(BREAD, 1)]),
      Or([dmg(0.3, E1, { hits: 3 }), make(BREAD, 1), srch()]),
      Or([dmg(0.45, E1, { hits: 3 }), make(BREAD, 2), disc(1)]),
    ]), null),
    // 원작 자유 — 입자 이론(0): 피해, 센디오가 2 이상이면 드로우
    U(H, 2, '입자 이론', 0, '공격', [dmg(0.6), ifStack(K, 2), draw(1)], [
      'A', 'B',
      Or([dmg(0.5), make(BREAD, 1)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.55), perTag(BREAD), dmg(0.25)]),
    ], bl('weakSpot', 'draw', [stk(K, 1)])),
    // 쓰기 — 화염 주문(2): 센디오 1개당, 전부 소모
    U(H, 3, '화염 주문', 2, '공격', [dmg(1.9), per(K), dmg(0.28), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.3, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      Or([dmg(1.5), per(K), dmg(0.25), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([dmg(1.6), per(K), dmg(0.28), srch()]),
    ], bl('power', 'ap', [stk(K, 1)])),
    // 쓰기(쏟기) — 궁극의 그리모어: 손의 빵 1장당 한 번 더, 손의 빵 전부 소멸
    U(H, 4, '궁극의 그리모어', 1, '공격', [dmg(0.6), perTag(BREAD), dmg(0.35), exile('hand', { all: true, tag: BREAD })], [
      'A',
      Or([dmg(0.45, EA), perTag(BREAD), dmg(0.25, EA), exile('hand', { all: true, tag: BREAD })]),
      Or([dmg(0.65), perTag(BREAD), dmg(0.38)]),
      Or([dmg(0.5), perTag(BREAD), dmg(0.3), pw('make', [dmg(0.25)], { limit: 2 })], { power: true }),
      'Hd',
    ], bl('power', 'draw', [make(BREAD, 1)])),
    // 만들기 — 제빵 수업: 빵 둘 + 센디오
    U(H, 5, '제빵 수업', 1, '스킬', [make(BREAD, 2), stk(K, 1)], nm([
      Or([make(BREAD, 3), stk(K, 1)]),
      Or([make(BREAD, 1), stk(K, 1)], { cost: 0 }),
      Or([make(BREAD, 2), stk(K, 1), sh(0.9)]),
      Or([make(BREAD, 1), stk(K, 1), pw('turnStart', [make(BREAD, 1)])], { power: true }),
      Or([make(BREAD, 3), stk(K, 2), nextAp(-1)]),
    ]), bl('draw', { tags: ['보존'] }, [make(BREAD, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 4. 에슈르(마도) — 성장형 · 딜러 · 활발. 실패작(소멸한 내 카드)을 태울 때마다 마력이 오르고, 이번 전투에 넷을 끌어올리면 「연구 집중」 이 판 내내 자란다
// 원작: 저학년 빵타지아 → 마력 증폭 · 강화 마력 레이저 · 고학년 거울 형상(대-에슈르) · 「오늘도 마법 빵 실패! 그래도 계속 도전」 · 밤샘 연구
// 시동: u3 빵타지아 — 그대로
// ════════════════════════════════════════════════════════════════════
function eshurMado(j) {
  const H = '에슈르_마도', K = '마력 증폭', M = '거울 형상', RES = '연구';
  const h = j.heroes[0];
  h.passives = [
    pas('연구 노트', 'exhaust', [stk(K, 1)], { limit: 2 }),
    pas('노력의 결실', 'kill', [stk(K, 1)], { when: { mine: true }, limit: 1 }),
  ];
  setCards(j, [
    // 둘째 — 억까의 시간은 끝(0, 소멸): 드로우 2 · 증폭(태워지는 시험작)
    U(H, 1, '억까의 시간은 끝', 0, '스킬', [draw(2), stk(K, 1)], nm([
      Or([draw(2), stk(K, 2)], { tags: ['소멸'] }),
      Or([draw(2), stk(K, 1), stk(M, 1)], { tags: ['소멸'] }),
      Or([draw(3), burn(1), stk(K, 2)], { tags: ['소멸'] }),
      Or([draw(2), stk(K, 2), srch({ type: '공격' })], { tags: ['소멸'] }),
      Or([draw(3), stk(K, 3), nextAp(-1)], { tags: ['소멸'] }),
    ]), bl('ap', { tags: ['보존'] }, [stk(K, 1)]), ['소멸']),
    // 쓰기 — 마력 레이저(2): 증폭 1개당, 전부 소모
    U(H, 2, '마력 레이저', 2, '공격', [dmg(1.8), per(K), dmg(0.45), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.3, EA), per(K), dmg(0.3, EA), spendAll(K)]),
      Or([dmg(1.5), per(K), dmg(0.4), stk(M, 1)]),
      Or([dmg(1.3), per(K), dmg(0.35), pw('exhaust', [stk(K, 1), dmg(0.3)], { limit: 1 })], { power: true }),
    ], bl('power', 'ap', [stk(K, 1)])),
    // 시동 — 빵타지아: 적 전체 + 취약 + 증폭 2
    U(H, 3, '빵타지아', 1, '공격', [dmg(0.65, EA), st('취약', 1, EA), stk(K, 2)], nm([
      Or([dmg(0.85, EA), st('취약', 1, EA), stk(K, 2)]),
      Or([dmg(0.5, EA), stk(K, 2)], { cost: 0 }),
      Or([dmg(1.0), st('취약', 2), stk(K, 2)]),
      Or([dmg(0.75, EA), stk(K, 2), srch({ type: '공격' })]),
      Or([dmg(0.9, EA), stk(K, 3), disc(1)]),
    ]), null),
    // 갈래 부품 — 연구 집중: 「연구」 1당 한 번(최소 1), 이번 전투에 증폭을 4 이상 쌓았으면 이 카드 「연구」 +1(판 내내, 최대 6)
    U(H, 4, '연구 집중', 1, '공격', [dmg(0.6), perCs(RES, { n: 1 }), dmg(0.2), ifGained(K, 4), cs(RES, 1, { max: 6 })], [
      'A',
      Or([dmg(0.45, EA), perCs(RES, { n: 1 }), dmg(0.15, EA), ifGained(K, 4), cs(RES, 1, { max: 6 })]),
      Or([dmg(0.6), perCs(RES, { n: 1 }), dmg(0.2), ifGained(K, 3), cs(RES, 1, { max: 6 })]),
      ['D', 'exhaust', [stk(K, 1)], { limit: 1 }],
      Or([burn(1), dmg(0.8), perCs(RES, { n: 1 }), dmg(0.25)]),
    ], bl('power', 'draw', [stk(K, 1)])),
    // 원작 자유 — 밤샘 스터디(0): 거울 형상 + 증폭(대-에슈르가 함께 쏜다)
    U(H, 5, '밤샘 스터디', 0, '스킬', [stk(M, 1), stk(K, 1)], nm([
      Or([stk(M, 2), stk(K, 1)]),
      Or([stk(M, 1), stk(K, 1), draw(1)]),
      Or([stk(M, 1), stk(K, 1), pw('turnStart', [stk(M, 1)])], { power: true }),
      Or([stk(M, 1), stk(K, 1), srch({ type: '공격' })]),
      Or([stk(M, 3), stk(K, 2), nextAp(-1)]),
    ]), bl('draw', { tags: ['신속'] }, [stk(M, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 카렌 — 회복형 · 서포터 · 활발. 당근 치유 방송 — 회복 뒤 HP 80%를 넘긴 몫이 「시청자 수」로, 다섯이면 떡상(파티의 다음 카드 +40%)
// 원작: 저학년 당근 치유 · 고학년 교주의 축복(둘 다 아군 회복 — 파티 회복으로) · 근본 생식 엘튜버 · 어그로 댓글(레비) · 드레싱 폭로로 몰락
// 다 차면: 옛 「떡상 방송」(저절로 사기 + AP) → 파티 다음 카드 강화. 당근 치유는 시청자를 고른 만큼(spend pick)
// 시동: u4 근본 생식 챌린지(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function karen(j) {
  const H = '카렌', K = '시청자 수';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '생방송을 지켜보는 시청자들', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }], onMax: { empower: 'any', ratio: 0.6, consume: true },
    rules: [{ name: '합방', when: { on: 'play', who: 'other', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] }],
  };
  h.passives = [
    pas('켜진 방송', 'fightStart', [stk(K, 3)]),
    pas('켜진 방송', 'overheal', [stk(K, 1), { k: 'shield', ratio: 1, ofEvent: 1 }], { when: { pct: 0.8 }, limit: 2 }),
    pas('켜진 방송', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    pas('떡상', 'kill', [stk(K, 2)], { when: { mine: true }, limit: 1 }),
    pas('떡상', 'spend', [st('사기', 1), L.ap(1)], { when: { id: K }, per: 'fight', limit: 3 }),
  ];
  setCards(j, [
    // 원작 자유 — 어그로 댓글 추적 방송: 피해 + 취약 + 시청자
    U(H, 1, '어그로 댓글 추적 방송', 1, '공격', [dmg(1.35), st('취약', 1), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.95, EA), st('취약', 1, EA), stk(K, 1)]),
      Or([dmg(1.2), st('취약', 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 엘튜브 생방송 마법쇼: 무작위 3회, 이번 판 HP 80%를 넘긴 회복 25당 한 발 더(넘친 회복 → 공격)
    U(H, 2, '엘튜브 생방송 마법쇼', 1, '공격', [dmg(0.42, ER, { hits: 3 }), perOver(0.8, 25), dmg(0.2, ER)], [
      'A', ['D', 'overheal', [dmg(0.55, ER)], { when: { pct: 0.8 }, limit: 1 }],
      Or([dmg(0.42, ER, { hits: 3 }), stk(K, 1), perOver(0.8, 25), dmg(0.2, ER)]),
      Or([dmg(0.4, ER, { hits: 3 }), perOver(0.8, 25), dmg(0.18, ER), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 당근 치유(저학년): 회복, 시청자를 고른 만큼 소모해 1개당 회복 더
    U(H, 3, '당근 치유', 1, '스킬', [heal(1.25), pick(K), perEv, heal(0.32)], nm([
      Or([heal(1.65), pick(K), perEv, heal(0.42)]),
      Or([heal(1.2), pick(K), perEv, heal(0.3)], { cost: 0 }),
      Or([sh(1.5), pick(K), perEv, sh(0.38)]),
      Or([heal(0.9), perOver(0.8, 25), sh(0.3), pw('overheal', [sh(0.3)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(1.2), per(K), heal(0.3), srch()]),
    ]), bl('heal', 'draw', [stk(K, 1)])),
    // 시동 — 근본 생식 챌린지(0): 시청자 · 회복 · 드로우
    U(H, 4, '근본 생식 챌린지', 0, '스킬', [stk(K, 1), heal(0.3), draw(1)], nm([
      Or([stk(K, 2), heal(0.35), draw(1)]),
      Or([stk(K, 1), heal(0.3), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 2), heal(1.0)]),
      Or([stk(K, 2), heal(0.5), srch()]),
      Or([stk(K, 3), draw(2), disc(1)]),
    ]), null),
    // 둘째 — 100캐럿 당근(0): 피해 + 흡수 + 시청자
    U(H, 5, '100캐럿 당근', 0, '공격', [dmg(0.75), drain(0.5), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.65, EA), drain(0.4), stk(K, 1)]),
      Or([dmg(0.65), drain(0.5), pw('turnStart', [heal(0.55)])], { power: true }),
      Or([dmg(0.85), drain(0.5), srch()]),
    ], bl('weakSpot', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 칸타 — 예약형 · 딜러 · 냉정. 판에 돌려 둔 쇠팽이는 매 턴 끝 무작위 적을 긁고, 적의 차례마다 회전이 하나씩 준다 — 더 긁게 둘까, 멈추기 전에 올인할까
// 원작: 강화 평타 4번째 공격마다 쇠팽이(16초 돌며 세 번 긁고 멈춤) · 고학년 탑스핀 크래쉬(주변 팽이 연쇄 폭발) · 저학년 탑스핀 블레이드 · 몰래 새 팽이채 · 무승부 조르기
// 옛 「튕겨 나간 팽이」(넘치면 저절로 피해) 뺌 → 쇠팽이 = 시한 예약(decay 1 · reserve), 다 멈추면 마지막 한 바퀴
// 시동: u2 소매 속 팽이(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function kanta(j) {
  const H = '칸타', K = '쇠팽이';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '판 위를 돌며 긁는 강철 팽이', carrier: 'self', cap: 4, decay: 1, reserve: true,
    rules: [
      { name: '팽이판', when: { on: 'turnEnd' }, fx: [per(K), dmg(0.6, ER)] },
      { name: '마지막 한 바퀴', when: { on: 'stackGone', id: K, decay: true }, fx: [dmg(0.6, ER)] },
    ],
  };
  h.passives = [
    pas('네 번째 공격', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 3 }),
    pas('진 놈 팽이 부수기', 'kill', [stk(K, 1)], { when: { mine: true }, limit: 1 }),
    pas('진 놈 팽이 부수기', 'break', [stk(K, 1)], { when: { mine: true }, limit: 1 }),
  ];
  setCards(j, [
    // 쓰기 — 올인: 쇠팽이 1개당, 전부 소모
    U(H, 1, '올인', 1, '공격', [dmg(0.9), per(K), dmg(0.4), spendAll(K)], [
      'A',
      Or([dmg(0.6, EA), per(K), dmg(0.28, EA), spendAll(K)]),
      Or([dmg(0.8), per(K), dmg(0.36), spendN(K, 2)]),
      Or([dmg(0.7), per(K), dmg(0.3), pw('stackGone', [dmg(0.4, EA)], { when: { id: K }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 소매 속 팽이(개전 강화): 쇠팽이 2 · 드로우 · 매 턴 쇠팽이
    U(H, 2, '소매 속 팽이', 1, '강화', [stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 3), draw(1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 2), sh(1.5), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 3), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 3), sh(1.2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 원작 자유 — 탑스핀 블레이드(저학년): 적 전체 + 마지막 충돌 + 쇠팽이
    U(H, 3, '탑스핀 블레이드', 1, '공격', [dmg(0.4, EA), dmg(0.7), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.35, EA), dmg(0.6), tough(1)]),
      Or([dmg(0.35, EA), dmg(0.6), srch()]),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 둘째 — 팽이 회수: 쇠팽이 1개당(쓰지 않음 — 쥐는 쪽)
    U(H, 4, '팽이 회수', 1, '공격', [dmg(0.85), per(K), dmg(0.3)], [
      'A', ['D', 'turnEnd', [stk(K, 1)]],
      Or([dmg(0.6, EA), per(K), dmg(0.2, EA)]),
      Or([dmg(0.75), per(K), dmg(0.25), srch()]),
      'Hd',
    ], bl('weakSpot', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 불 뿜는 새 팽이(몰래 꺼낸 새 팽이채): 실드 + 쇠팽이 2(시한 늘리기)
    U(H, 5, '불 뿜는 새 팽이', 1, '스킬', [sh(1.0), stk(K, 2)], [
      'A', 'B',
      Or([sh(0.8), stk(K, 2), draw(1)]),
      Or([sh(0.7), stk(K, 1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([sh(1.4), stk(K, 3), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 클로에 — 두 얼굴형 · 탱커 · 광기 · 엘다인. 평소 얼굴(바늘땀 · 천 조각으로 버팀)과 세바스티안에 올라탄 얼굴(「인형의 의지」 — 앞발이 연타 · 휩쓸기)을 손으로 오간다
// 원작: 저학년 메리 고 라운드(세바스티안 탑승 · 큰 보호막, 평타 3연타 + 범위 · 평소 강화 공격 안 씀) · 고학년 쁘띠 세바스티안 · 어사이드 패션 커버 · 찢기면 고친다
// 엘다인 한 단계: 내릴 때(변신이 풀릴 때) 천 조각 1장(세바스티안 수선) + 탑승 중 공격하면 바늘땀
// 바늘땀 둘이면 천 조각(옛 stackReach 규칙 → onMax make — 원래 만들던 사도)
// 시동: u2 메리 고 라운드 — 그대로(탑승)
// ════════════════════════════════════════════════════════════════════
function chloe(j) {
  const H = '클로에', K = '바늘땀', F = '클로에_탑승', CLOTH = '클로에_cloth', SEBAS = '클로에_sebas', F1 = '클로에_f1';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '인형을 짓는 재단사의 바늘땀', carrier: 'self', cap: 2, onMax: { make: CLOTH, consume: true } };
  h.passives = [
    pas('재단사의 손', 'fightStart', [stk(K, 1)]),
    pas('재단사의 손', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 }),
    pas('셀러브리티 클로에', 'lowHp', [st('피해 감소', 2), make(SEBAS, 1)], { when: { pct: 0.3 } }),
  ];
  h.forms = [{
    id: F, name: '인형의 의지', desc: '세바스티안에 올라탄 클로에 — 앞발이 연타와 휩쓸기로 바뀌는 탑승',
    turns: 3, mods: { atk: 0.15 },
    cards: { [`${H}_s1`]: F1 },
    bonus: [{ card: `${H}_u3`, fx: [formEnd] }],
    passives: [{ name: '세바스티안의 앞발', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] }],
    replace: false, off: [make(CLOTH, 1)], skin: null, anim: null,
  }];
  const tokens = [
    token(F1, '세바스티안 휩쓸기', H, '공격', [ddef(0.24, E1, { hits: 3 }), ddef(0.32, EA)], { cost: 1, tags: [], blurb: '올라탄 세바스티안이 세 번 할퀴고 크게 휩씁니다' }),
  ];
  setCards(j, [
    // 공격 얼굴 — 세바스티안 연타: 방어 기반 3연타 + 바늘땀
    U(H, 1, '세바스티안 연타', 1, '공격', [ddef(0.25, E1, { hits: 3 }), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([ddef(0.22, ER, { hits: 4 }), stk(K, 1)]),
      Or([ddef(0.22, E1, { hits: 3 }), stk(K, 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 시동 — 메리 고 라운드(저학년): 큰 실드 + 바늘땀 + 탑승(3턴)
    U(H, 2, '메리 고 라운드', 1, '스킬', [sh(1.2), stk(K, 1), form(F)], nm([
      Or([sh(1.6), stk(K, 1), form(F)]),
      Or([sh(0.8), form(F)], { cost: 0 }),
      Or([sh(1.0), stk(K, 2), form(F)]),
      Or([sh(1.5), form(F), srch({ type: '공격' })]),
      Or([sh(2.4), form(F), disc(1)]),
    ]), null),
    // 평소 얼굴 — 건치 스마일: 피해 감소 + 실드, 손의 천 조각 1장당 실드 더. 탑승 중이면 내린다(변신 덤)
    U(H, 3, '건치 스마일', 1, '스킬', [st('피해 감소', 1), sh(0.7), perTag(CLOTH), sh(0.25)], [
      'A', 'B',
      Or([st('피해 감소', 1), sh(0.6), make(CLOTH, 1)]),
      ['D', 'turnStart', [sh(0.3)]],
      'Hn',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 원작 자유 — 의지를 넘긴 날(강화): 결정화 + 매 턴 바늘땀(권능을 세바스티안에게)
    U(H, 4, '의지를 넘긴 날', 1, '강화', [st('결정화', 1), pw('turnStart', [stk(K, 1)])], nm([
      Or([st('결정화', 1), pw('turnStart', [stk(K, 1), sh(0.3)])]),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0 }),
      Or([st('결정화', 1), sh(0.8), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([st('결정화', 1), srch(), pw('turnStart', [stk(K, 1)])]),
      Or([st('결정화', 2), nextAp(-1), pw('turnStart', [stk(K, 1), sh(0.3)])]),
    ]), bl('defUp', { tags: ['개전'] }, [stk(K, 1)])),
    // 쓰기 — 재단 가위질: 손의 천 조각 1장당 한 번 더(방어 기반), 손의 천 조각 전부 소멸 — 쥐면 실드, 자르면 피해
    U(H, 5, '재단 가위질', 1, '공격', [ddef(0.55), perTag(CLOTH), ddef(0.3), exile('hand', { all: true, tag: CLOTH })], [
      'A',
      Or([ddef(0.4, EA), perTag(CLOTH), ddef(0.2, EA), exile('hand', { all: true, tag: CLOTH })]),
      Or([ddef(0.6), perTag(CLOTH), ddef(0.32)]),
      Or([ddef(0.5), perTag(CLOTH), ddef(0.25), pw('make', [ddef(0.25)], { limit: 2 })], { power: true }),
      'Hd',
    ], bl('power', 'weakSpot', [make(CLOTH, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 8. 파트라 — 손 만들기형 · 딜러 · 냉정. 매 턴 민트초코 쿠키를 굽는다(아무도 안 사 감) — 먹이면(내면) 재고가 쌓이고, 쥐고 있으면 턴 끝에 억지로 먹인다
// 원작: 민트 케이크 150개를 만들어 놓고 퇴근 · 약탈당한 빵집에 민트만 남음 · 민트 빵만 진열해 막아냄 · 억지로 먹이다 끌려감 · 저학년 민트 뒤집개 + 중독 · 고학년 교주의 천벌
// 새 생성 카드 사도(묶음 몫 1): 패시브 「잔뜩 만든 민트」 가 턴 시작에 민트 1장. 다 차면: 옛 「재고 처분」(저절로 광역) → 다음 카드 +50%
// 시동: u3 중불까지 키워주세요(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function patra(j) {
  const H = '파트라', K = '민트 재고', MINT = '파트라_mint';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '아무도 안 사 가는 민트초코', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('잔뜩 만든 민트', 'turnStart', [make(MINT, 1)]),
    pas('억지로 먹이기', 'kill', [stk(K, 2)], { when: { mine: true }, limit: 1 }),
  ];
  const tokens = [token(MINT, '민트초코 쿠키', H, '공격', [dmg(0.45), stk(K, 1), handEnd, dmg(0.35, ER)], { blurb: blurbOf(j, MINT) })];
  setCards(j, [
    // 쓰기 — 민트머겅!: 재고 1개당, 전부 소모
    U(H, 1, '민트머겅!', 1, '공격', [dmg(0.85), per(K), dmg(0.32), spendAll(K)], [
      'A',
      Or([dmg(0.6, EA), per(K), dmg(0.22, EA), spendAll(K)]),
      Or([dmg(0.85), perTag(MINT), dmg(0.35), exile('hand', { all: true, tag: MINT })]),
      Or([dmg(0.65), per(K), dmg(0.25), pw('spend', [make(MINT, 1)], { when: { id: K }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 원작 자유 — 민트 반죽 치대기(저학년 민트 뒤집개 + 중독): 2연타 + 고통 + 재고
    U(H, 2, '민트 반죽 치대기', 1, '공격', [dmg(0.5, E1, { hits: 2 }), st('고통', 2), stk(K, 1)], [
      'A', ['D', 'play', [st('고통', 2)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.6, EA), st('고통', 2, EA), stk(K, 1)]),
      Or([dmg(0.45, E1, { hits: 2 }), st('고통', 2), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 시동 — 중불까지 키워주세요(개전 강화): 재고 + 실드 + 매 턴 재고
    U(H, 3, '중불까지 키워주세요', 1, '강화', [stk(K, 1), sh(0.7), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), sh(0.8), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(0.6), pw('turnStart', [make(MINT, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 3), sh(1.4), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 만들기 — 샤샤와 소풍 도시락(0): 민트 · 드로우
    U(H, 4, '샤샤와 소풍 도시락', 0, '스킬', [make(MINT, 1), draw(1)], nm([
      Or([make(MINT, 2), draw(1)]),
      Or([make(MINT, 1), draw(1), stk(K, 1)], { tags: ['신속'] }),
      Or([make(MINT, 1), sh(1.2)]),
      Or([make(MINT, 1), draw(1), pw('make', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([make(MINT, 2), draw(2), disc(1)]),
    ]), bl('draw', 'ap', [make(MINT, 1)])),
    // 갈래 부품 — 밤샘 시험 조리(0): 민트 1, 손의 민트 1장당 실드(민트만 진열해 막아냄)
    U(H, 5, '밤샘 시험 조리', 0, '스킬', [make(MINT, 1), perTag(MINT), sh(0.25)], nm([
      Or([make(MINT, 1), perTag(MINT), sh(0.35)]),
      Or([make(MINT, 1), stk(K, 1), perTag(MINT), sh(0.2)]),
      Or([make(MINT, 1), perTag(MINT), dmg(0.2, ER)]),
      Or([make(MINT, 1), sh(0.4), pw('turnEnd', [perTag(MINT), sh(0.15)])], { power: true }),
      Or([make(MINT, 2), perTag(MINT), sh(0.3), nextAp(-1)]),
    ]), bl('guard', { tags: ['보존'] }, [make(MINT, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 9. 폴랑 — 거드는 형 · 서포터 · 광기. 어느 사도든 공격하면 장전 — 셋이면 파티의 다음 카드 +40%(일제 사격 준비), 이번 턴 셋이 각자 카드를 냈으면 포위 완성
// 원작: 「셋이 120도로 둘러싸 패기」 집착 · 저학년 요정 왕국에 경례(아군 회복 · 스킬 피해↑) · 강화 평타 셋째마다 장전 · 고학년 로얄 스트레이트 · 진군깃발
// 다 차면: 옛 「일제 사격」(저절로 광역 + 회복) → 파티 다음 카드 강화. 「셋」은 파티 셋이 각자 낸 것(인원 세기 아님 — ifAllHeroes)
// 시동: u3 120도 삼각 편대(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function polang(j) {
  const H = '폴랑', K = '머스킷 장전';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '셋째 발에 맞춰 재는 장전', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.4, consume: true } };
  setCards(j, [
    // 원작 자유 — 요정 왕국에 경례(저학년): 회복 + 이번 턴 아군 주는 피해 + 장전
    U(H, 1, '요정 왕국에 경례', 1, '스킬', [heal(0.8), dmod(0.2, ALLIES), stk(K, 1)], [
      'A', ['D', 'turnStart', [dmod(0.1, ALLIES)]],
      Or([heal(1.1), dmod(0.25, ALLIES), ifAll, dmod(0.25, ALLIES)]),
      Or([heal(0.95), dmod(0.2, ALLIES), srch()]),
      Or([heal(1.2), dmod(0.3, ALLIES), disc(1)]),
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 쓰기 — 일제 사격: 적 전체, 장전 1개당 한 번 더, 전부 소모
    U(H, 2, '일제 사격', 1, '공격', [dmg(0.5, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      'A',
      Or([dmg(0.5, EA), per(K), dmg(0.2, EA), ifAll, st('약화', 1, EA)]),
      Or([dmg(0.8), per(K), dmg(0.32), spendAll(K)]),
      Or([dmg(0.4, EA), per(K), dmg(0.16, EA), pw('play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 120도 삼각 편대(개전 강화): 장전 · 다른 아군 카드 드로우 · 매 턴 장전
    U(H, 3, '120도 삼각 편대', 1, '강화', [stk(K, 1), draw(1, { who: 'other' }), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), draw(1, { who: 'other' }), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), draw(1, { who: 'other', type: '공격' }), pw('play', [heal(0.6)], { when: { who: 'any', type: '공격' }, limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 2), sh(1.0), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), heal(1.3), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 경비대장 폴랑입니다: 협공 + 회복, 이번 턴 사도 모두가 카드를 냈으면 아군 주는 피해 +(포위 완성)
    U(H, 4, '경비대장 폴랑입니다', 1, '스킬', [st('협공', 1), heal(0.5), ifAll, dmod(0.25, ALLIES)], [
      Or([st('협공', 1), heal(0.75), ifAll, dmod(0.35, ALLIES)]), 'B',
      Or([st('협공', 2), stk(K, 1), ifAll, dmod(0.35, ALLIES)]),
      Or([st('협공', 1), heal(0.6), pw('play', [heal(0.35)], { when: { who: 'any', type: '공격' }, limit: 2 })], { power: true }),
      Or([st('협공', 1), heal(0.75), srch({ type: '공격' })]),
    ], bl('heal', 'ap', [stk(K, 1)])),
    // 둘째 — 포위 심문: 피해 + 장전 + 다른 아군 공격 카드 드로우
    U(H, 5, '포위 심문', 1, '공격', [dmg(0.8), stk(K, 1), draw(1, { who: 'other', type: '공격' })], [
      'A', ['D', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([dmg(0.55, EA), stk(K, 1), draw(1, { who: 'other', type: '공격' })]),
      Or([dmg(0.95), stk(K, 2), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '요정/슈팡': 0.85, '요정/스키아': 1.3, '요정/에슈르': 1.2, '요정/에슈르_마도': 1.1, '요정/카렌': 1.4, '요정/칸타': 1.4, '요정/클로에': 1.0, '요정/파트라': 1.35 };

// ── 돌리기 ──
const JOBS = [
  ['요정/슈팡', shupang], ['요정/스키아', skia], ['요정/에슈르', eshur], ['요정/에슈르_마도', eshurMado], ['요정/카렌', karen],
  ['요정/칸타', kanta], ['요정/클로에', chloe], ['요정/파트라', patra], ['요정/폴랑', polang],
];
L.run18(JOBS, TUNE, new URL('./boost_요정B_18.json', import.meta.url));
