// 18갈래 재설계 3단계 — 마녀B 묶음 6명(아사나 · 아야 · 요미 · 포셔 · 프리클 · 피코라). 2026-10-08
// 기준: 05_시범16_결과.md · 지침 _gen/rework/BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/마녀B.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동은 공용 풀).
// 다 차면 저절로 터짐 없음(onMax empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// 카드 id · 이름 · 종류 · 비용은 모두 그대로(그림 짝 유지), 시동 칸도 그대로.
// node _gen/rework/마녀B_18.mjs [사도 이름 일부]  → heroes/마녀/<파일>.json 덮어쓰기(백업 heroes_before_118 에서 읽음)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, st, stk, spendAll, per, draw, make, ifStack, later,
  STRONG, ALLY, OTHER, tough, disc, gauge, empower, ripen, summon, spendN, cs, perOver, pw, srch, exile,
  Or, U, bl, setCards } = L;
const named = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
// 손으로 짠 신탁(Or)이 태그를 안 적었으면 카드 태그를 그대로 잇는다(빼는 것은 tags: [] 로만)
const keepTags = j => { for (const c of j.cards) if (c.unique && c.tags) for (const o of c.oracles || []) if (o.tags === undefined) o.tags = [...c.tags]; };
const pas = L.pas;

// ════════════════════════════════════════════════════════════════════
// 1. 아사나 — 박자형 · 탱커 · 우울. 「내가 자세를 보이면 동료가 따라 한다」 — 아사나 카드 바로 뒤에 동료 카드가 나오면 한 동작(마력 호흡 +1), 셋째 동작에 분출(파티의 다음 카드 강화)
// 원작: 저학년 마력 분출(자기 보호막 + 세 번째 발사마다 기절) · 고학년 명상 시간 · 어사이드 「24시간 밀착 감시」 · 남에게 요가를 시키는 「요가 빌런」 · 심마체
// 티그 · 루드(남이 끼면 끊김)와 반대로 아사나는 남이 껴야 박자가 선다. 아르코(종류 교대)와 달리 「주인 교대」(아사나 → 동료)
// 「바로 뒤」 는 「시범 자세」(아사나 카드를 내면 1 · 턴 끝 사라짐)를 동료 카드가 받아 가는 꼴로(직전 카드 주인 조건 대신 — 엔진 요청 칸)
// 다 차면: 옛 「세 번째 분출」(저절로 방어 기반 피해 + 실드) → 파티의 다음 카드 강화(onMax empower any)
// 시동: u1 볼-요가 시간(개전 강화, 그대로) — 매 턴 시작에 시범 자세(그 턴 첫 동료 카드가 한 동작)
// ════════════════════════════════════════════════════════════════════
function asana(j) {
  const H = '아사나', K = '마력 호흡', P = '시범 자세';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '들숨 넷, 날숨 넷 — 심마체의 호흡', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.06, who: 'allies' }], onMax: { empower: 'any', ratio: 0.5, consume: true } };
  h.keywords = [{ name: P, desc: '바로 다음 동료가 따라 하는 요가 자세', carrier: 'self', cap: 1, endClear: true }];
  h.passives = [
    pas('심마체', 'play', [stk(P, 1)]),
    pas('심마체', 'play', [spendN(P, 1), stk(K, 1)], { when: { who: 'other' }, conds: [{ c: 'stack', id: P, n: 1 }], limit: 3 }),
    pas('건강한 몸에 건강한 정신', 'turnStart', [heal(1)], { conds: [{ c: 'hp', pct: 0.5 }], per: 'fight', limit: 1 }),
  ];
  const yoga = pw('turnStart', [stk(P, 1)]);
  setCards(j, [
    // 시동 — 볼-요가 시간(개전 강화): 실드 + 호흡, 매 턴 시작에 시범 자세
    U(H, 1, '볼-요가 시간', 1, '강화', [sh(0.7), stk(K, 1), yoga], named([
      Or([sh(0.95), stk(K, 1), yoga]),
      Or([yoga], { cost: 0 }),
      Or([sh(0.6), stk(K, 1), pw('turnStart', [stk(P, 1), sh(0.25)])]),
      Or([sh(0.6), srch(), pw('turnStart', [stk(P, 1), sh(0.2)])]),
      Or([sh(0.9), stk(K, 2), pw('turnStart', [stk(P, 1), stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 원작 자유 — 마력 분출: 자기 실드 + 방어 기반 피해, 호흡이 둘 이상이면(세 번째 발사) 기절
    U(H, 2, '마력 분출', 1, '공격', [sh(0.5), ddef(0.6), ifStack(K, 2), st('기절', 1)], [
      'A', 'B',
      Or([sh(0.6), ddef(0.6, EA), ifStack(K, 2), st('기절', 1)]),
      Or([ddef(0.6), pw('play', [sh(0.25)], { when: { who: 'other' }, limit: 1 }), ifStack(K, 2), st('기절', 1)], { power: true }),
      Or([ddef(1.0), disc(1), ifStack(K, 2), st('기절', 1)]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 고요한 일격: 방어 기반, 호흡 1개당 더, 호흡 전부 소모(셋째 동작까지 기다리면 파티 강화 — 지금 털까)
    U(H, 3, '고요한 일격', 1, '공격', [ddef(0.5), per(K), ddef(0.3), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.4, EA), per(K), ddef(0.22, EA), spendAll(K)]),
      Or([ddef(0.45), per(K), ddef(0.26), sh(0.5)]),
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 24시간 밀착 감시: 실드 + 손의 동료 카드 1장 비용 -1 + 드로우(바로 뒤에 동료 카드를 내게)
    U(H, 4, '24시간 밀착 감시', 1, '스킬', [sh(0.6), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), draw(1)], [
      'A', 'B',
      Or([sh(0.9), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), stk(K, 1)]),
      Or([sh(0.6), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), pw('play', [sh(0.25)], { when: { who: 'other' }, limit: 2 })], { power: true }),
      Or([sh(2.0), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 둘째 — 자세 교정(0): 실드 + 동료 카드 1장 뽑기
    U(H, 5, '자세 교정', 0, '스킬', [sh(0.5), draw(1, { who: 'other' })], [
      'A', 'B',
      Or([sh(0.5), draw(1, { who: 'other' }), stk(K, 1)]),
      Or([sh(0.4), draw(1, { who: 'other' }), pw('turnStart', [draw(1, { who: 'other' })])], { power: true }),
      'Hd',
    ], bl('cost', { tags: ['보존'] }, [stk(P, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 아야 — 거드는 형 · 딜러 · 냉정 · 엘다인. 「눈꽃을 피워 둔 적에게 파티의 칼이 깊이 박히게 한다」 — 적에게 서리(동상 — 받는 피해↑), 동료가 칠 때마다 번진다
// 원작: 고학년 만개설화(눈꽃 범위 + 동상 — 받는 피해 크게↑, 최대 5번 번짐) · 저학년 싸락나비(나비가 갔다가 돌아옴) · 희망의 엘다인 · 절망의 눈보라
// 거드는 형 안에서: 아군 카드를 고르거나 비용을 덜어 주지 않고, 적 쪽에 판을 깐다. 동료 카드가 친 적에게 눈꽃이 더 박힌다
// 다 차도 터지지 않는다(옛 「눈꽃 개화」 · 「얼어 터지는 서리」 저절로 피해 뺌 — 서리 최대 5에서 막힘)
// 엘다인 한 단계: 적이 쓰러지면 눈꽃이 다른 적에게 번진다(번지는 눈꽃)
// 시동: u3 동상 조준(0, 그대로)
// ════════════════════════════════════════════════════════════════════
function aya(j) {
  const H = '아야', K = '서리';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '맞을수록 깊이 박히는 만년설의 눈꽃', carrier: 'enemy', cap: 5, per: [{ stat: 'taken', v: 0.05 }, { stat: 'dealt', v: -0.03 }],
    rules: [pas('싸락나비', 'play', [stk(K, 1)], { when: { type: '공격' } })] };
  h.passives = [
    pas('시련 속의 눈꽃', 'hit', [stk(K, 1)], { when: { who: 'other' }, limit: 2 }),
    pas('시련 속의 눈꽃', 'kill', [stk(K, 2, ER)], { limit: 1 }),
    pas('희망의 눈꽃', 'lowHp', [{ k: 'cleanse', v: 1 }, st('피해 감소', 2), stk(K, 2, EA)], { when: { pct: 0.3 }, per: 'fight', limit: 1 }),
  ];
  setCards(j, [
    // 원작 자유 — 싸락나비: 무작위 3회 + 적 전체 서리, 다음 턴 시작에 나비가 돌아와 2회
    U(H, 1, '싸락나비', 1, '공격', [dmg(0.3, ER, { hits: 3 }), stk(K, 1, EA), later(1, [dmg(0.25, ER, { hits: 2 })])], [
      'A', 'B',
      Or([dmg(0.3, ER, { hits: 3 }), stk(K, 1, EA), srch()]),
      ['D', 'turnStart', [dmg(0.22, ER, { hits: 2 })]],
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, EA)])),
    // 쓰기 — 빙설화도 발도: 피해 + 서리 1개당 더, 서리 전부 소모
    U(H, 2, '빙설화도 발도', 1, '공격', [dmg(0.7), per(K), dmg(0.26), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.6), per(K), dmg(0.22), srch()]),
      ['D', 'kill', [stk(K, 2, ER)], { limit: 1 }],
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 동상 조준(0): 서리 둘 + 드로우
    U(H, 3, '동상 조준', 0, '스킬', [stk(K, 2), draw(1)], named([
      Or([stk(K, 3), draw(1)]),
      Or([stk(K, 2), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 2), draw(1), empower(0.3, 'any')]),
      Or([stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 4), draw(1), disc(1)]),
    ]), null),
    // 둘째 — 만년설의 현자(강화): 적 전체 서리, 매 턴 시작에 적 전체 서리
    U(H, 4, '만년설의 현자', 1, '강화', [stk(K, 1, EA), pw('turnStart', [stk(K, 1, EA)])], named([
      Or([stk(K, 2, EA), pw('turnStart', [stk(K, 1, EA)])]),
      Or([pw('turnStart', [stk(K, 1, EA)])], { cost: 0 }),
      Or([stk(K, 2, EA), pw('hit', [stk(K, 1)], { when: { who: 'other' }, limit: 2 })]),
      Or([stk(K, 1, EA), srch(), pw('turnStart', [stk(K, 1, EA)])]),
      Or([stk(K, 1, EA), pw('turnStart', [stk(K, 1, EA), dmg(0.2, EA)]), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 마당 쓸기(0): 적 전체 서리 + 파티의 다음 카드 강화(서로의 칼)
    U(H, 5, '마당 쓸기', 0, '스킬', [stk(K, 1, EA), empower(0.25, 'any')], [
      'A', 'B',
      Or([stk(K, 1, EA), empower(0.25, 'any'), draw(1)]),
      Or([stk(K, 1, EA), empower(0.2, 'any'), pw('turnStart', [empower(0.15, 'any')])], { power: true }),
      Or([stk(K, 2, EA), empower(0.4, 'any'), disc(1)]),
    ], bl('cost', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 요미 — 예약형 · 딜러 · 우울 · 엘다인. 「달의 영역을 깔아 두면 턴마다 거두고, 다 지면 보름달」 — 일찍 띄울까(남은 칸만큼 약함 · 영역을 잃음), 더 비출까
// 원작: 저학년 달바라기(적 자리에 달의 영역 — 적 피해 + 주는 피해↓, 아군 받는 피해↓, 그동안 별빛 강화 공격) · 고학년 구름 걷는 달빛(아군 SP 채움) · 운석 기도
// 캬롯(아군에게 씨앗) · 마카샤(적에게 시계)와 달리 예약이 「장판」 — 깔린 동안에도 일한다(턴 끝 · 별빛), 다 지면 보름달. 예약 2칸(시범 기준)
// 옛 「달의 순환」(매 턴 저절로 +1 — 영역이 끝나지 않음) · 「보름달」(4에 저절로) 뺌
// 엘다인 한 단계: 보름달이 뜨면 고학년 게이지 + 드로우(지극정성의 마중)
// 시동: u3 달을 섬기는 기도(0, 그대로)
// ════════════════════════════════════════════════════════════════════
function yomi(j) {
  const H = '요미', K = '달바라기';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '보름달을 기다리며 피는 꽃밭', carrier: 'self', cap: 2, decay: 1, reserve: true,
    per: [{ stat: 'taken', v: -0.04, who: 'allies' }],
    rules: [
      { name: '달빛 꽃밭', when: { on: 'turnEnd' }, conds: [{ c: 'stack', id: K, n: 1 }], fx: [dmg(0.22, EA)] },
      { name: '보름달', when: { on: 'stackGone', id: K, decay: true }, fx: [{ k: 'cue', id: 'yomi_moonlight' }, dmg(1.0, EA), st('약화', 1, EA)] },
      pas('별빛', 'play', [dmg(0.25, ER)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: 2 }),
    ],
  };
  h.passives = [
    pas('지극정성의 마중', 'stackGone', [gauge(15), draw(1)], { when: { id: K }, limit: 1 }),
    pas('부디 저를 기억해 주세요', 'lowHp', [heal(1), st('피해 감소', 1)], { when: { pct: 0.3 }, per: 'fight', limit: 1 }),
  ];
  setCards(j, [
    // 만들기 — 달빛 꽃밭: 적 전체 + 영역 둘(두 턴 비춘 뒤 보름달)
    U(H, 1, '달빛 꽃밭', 1, '공격', [dmg(0.6, EA), stk(K, 2)], [
      'A', 'B',
      Or([dmg(0.9), st('둔화', 1), stk(K, 2)]),
      ['D', 'turnEnd', [dmg(0.15, EA)], { conds: [{ c: 'stack', id: K, n: 1 }] }],
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 별빛 사격: 피해 + 영역을 지금 거둬 보름달(남은 1칸당 -25%)
    U(H, 2, '별빛 사격', 1, '공격', [dmg(0.7), ripen(K, 0.25)], [
      'A', 'B',
      Or([dmg(0.65), ripen(K, 0.25), srch()]),
      Or([dmg(0.6), ripen(K, 0.25), pw('stackGone', [dmg(0.3, ER)], { when: { id: K }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 달을 섬기는 기도(0): 영역 + 드로우
    U(H, 3, '달을 섬기는 기도', 0, '스킬', [stk(K, 1), draw(1)], named([
      Or([stk(K, 2), draw(1)]),
      Or([stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1), draw(1), sh(0.6)]),
      Or([stk(K, 1), draw(1), pw('stackGone', [draw(1)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([stk(K, 2), draw(2), disc(1)]),
    ]), null),
    // 갈래 부품 — 홀로 섬긴 사제(강화): 영역 + 보름달이 지면 다시 꽃이 핀다(턴 1)
    U(H, 4, '홀로 섬긴 사제', 1, '강화', [stk(K, 1), pw('stackGone', [stk(K, 1), gauge(10)], { when: { id: K }, limit: 1 })], named([
      Or([stk(K, 2), pw('stackGone', [stk(K, 1), gauge(10)], { when: { id: K }, limit: 1 })]),
      Or([pw('stackGone', [stk(K, 1), gauge(10)], { when: { id: K }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 1), pw('stackGone', [stk(K, 1), dmg(0.3, ER)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 1), srch(), pw('stackGone', [stk(K, 1)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 2), pw('stackGone', [stk(K, 2)], { when: { id: K }, limit: 1 }), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 원작 자유 — 달밤의 춤: 실드 + 영역, 영역이 가득하면 적 전체 별빛
    U(H, 5, '달밤의 춤', 1, '스킬', [sh(0.9), stk(K, 1), ifStack(K, 2), dmg(0.35, EA)], [
      'A', 'B',
      Or([sh(1.1), stk(K, 1), draw(1)]),
      Or([sh(0.8), stk(K, 1), pw('turnEnd', [dmg(0.3, EA)], { conds: [{ c: 'stack', id: K, n: 1 }] })], { power: true }),
      Or([sh(1.8), stk(K, 2), disc(1)]),
    ], bl('guard', 'cost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 포셔 — 회복형 · 서포터 · 우울. 「센 약을 먹이고, 넘친 약은 적에게」 — 회복 뒤 HP 80%를 넘긴 몫이 적 전체 「부작용」(주는 피해↓)으로, 쓴맛은 씻어 낸다(드로우)
// 원작: 효과가 굉장한 만큼 부작용이 반드시 · 「입에 쓴 게 몸에 좋다」 · 저학년 무슨 포션 줄까?(한 번에 가득 찰 만큼 큰 회복) · 강화 평타 중독 · 고학년 감자 고구마!(변이)
// 회복형 안 자리: 넘침을 만드는 법 = 센 한 방(대가로 쓴맛 카드), 넘친 것 = 적 디버프(부작용) + 해독(쓴맛 소멸 · 드로우)
// 힐데(실드) · 큐이(공격) · 아멜리아(서류 → 피해) · 카렌(시청자 → 파티 다음 카드) · 디아나(촌장)(기혈 → 강화) · 마고(목장 친구 → 카드)와 겹치지 않게 — 포셔만 넘침이 적에게 간다
// 시동: u1 무슨 포션 줄까?(그대로)
// ════════════════════════════════════════════════════════════════════
function portia(j) {
  const H = '포셔', K = '부작용', T = '포셔_t1';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '일부러 남겨 둔 포셔 약의 하자', carrier: 'enemy', cap: 5, decay: 1, per: [{ stat: 'dealt', v: -0.04 }],
    rules: [pas('약값은 칼같이', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 })] };
  h.passives = [
    pas('남은 약은 적에게', 'overheal', [stk(K, 1, EA)], { when: { pct: 0.8 }, limit: 1 }),
    pas('남은 약은 적에게', 'overheal', [{ k: 'ifInHand', id: T }, exile('hand', { all: true, tag: T }), draw(1)], { when: { pct: 0.8 }, limit: 1 }),
    pas('해독제 값은 따로', 'stackGone', [heal(0.2)], { when: { id: K, decay: true }, limit: 1 }),
  ];
  setCards(j, [
    // 시동 — 무슨 포션 줄까?: 큰 회복 + 부작용 + 쓴맛(대가)
    U(H, 1, '무슨 포션 줄까?', 1, '스킬', [heal(1.0), stk(K, 2)], named([
      Or([heal(1.3), stk(K, 2)]),
      Or([heal(0.75), stk(K, 1)], { cost: 0 }),
      Or([heal(1.5), stk(K, 2), make(T, 1)]),
      Or([heal(1.1), stk(K, 2), pw('overheal', [stk(K, 1, EA)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(1.0), stk(K, 2), srch()]),
    ]), null),
    // 둘째 — 중독 포션 두 병: 방어 기반 2회 + 부작용 둘
    U(H, 2, '중독 포션 두 병', 1, '공격', [ddef(0.33, E1, { hits: 2 }), stk(K, 2)], [
      'A', 'B',
      Or([ddef(0.3, EA, { hits: 2 }), stk(K, 1, EA)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 흐물 약화 포션: 약화 + 부작용 1개당 회복, 부작용 전부 소모(그 회복이 넘치면 부작용이 적 전체로 돌아온다)
    U(H, 3, '흐물 약화 포션', 1, '스킬', [st('약화', 2), per(K), heal(0.28), spendAll(K)], [
      'A', 'B',
      Or([st('약화', 2), per(K), sh(0.32), spendAll(K)]),
      ['D', 'overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 }],
      'Hn',
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 원작 자유 — 약장수의 비법서: 방어 기반 + 부작용 1개당 더
    U(H, 4, '약장수의 비법서', 1, '공격', [ddef(0.45), per(K), ddef(0.2)], [
      'A', 'B',
      Or([ddef(0.4, EA), per(K, { each: true }), ddef(0.15, EA)]),
      ['D', 'overheal', [ddef(0.45)], { when: { pct: 0.8 }, limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 약초 감별(0): 회복 + 부작용 + 드로우
    U(H, 5, '약초 감별', 0, '스킬', [heal(0.32), stk(K, 1), draw(1)], named([
      Or([heal(0.4), stk(K, 2), draw(1)]),
      Or([heal(0.35), stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([heal(0.5), make(T, 1), draw(2)]),
      Or([heal(0.3), draw(1), pw('turnStart', [heal(0.2), stk(K, 1)])], { power: true }),
      Or([heal(0.4), draw(3), disc(1)]),
    ]), bl('heal', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 프리클 — 소환형 · 딜러 · 냉정. 「적이 걸려들 덫을 깔아 두고, 계획이 틀어지면 한꺼번에 거둔다」 — 가시 촉수(소환물)는 적이 공격하려 하면 먼저 찌르고, 문지기로 모두 거둔다
// 원작: 강화 평타 가시 촉수 소환(사라질 때까지 근처 적을 찌름) · 저학년 따끔한 문지기(깔린 촉수를 모두 거둬 더 큰 피해) · 고학년 조여오는 경비병(덩굴 — 풀리면 촉수) · 늘 「어떤 계획」 · 함정
// 쥬비(공격하면 떼로 따라 침) · 모모(대신 맞고 깨짐) · 에피카(무대 합주)와 달리 촉수는 「적의 행동」에 반응하는 덫, 끝은 거두기
// 옛 「계획은 그 자리에서」(넘치면 저절로 드로우) 뺌 — 처치하면 드로우로
// 시동: u1 가시 덫 설치(그대로)
// ════════════════════════════════════════════════════════════════════
function prickle(j) {
  const H = '프리클', K = '가시 촉수';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '덩굴에서 돋아나 적의 움직임을 노리는 촉수', carrier: 'self', cap: 5,
    rules: [pas('문지기 소환', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }), pas('찌르는 촉수', 'turnEnd', [summon(K, 0.4, { max: 5 })]), pas('미리 세운 계획', 'fightStart', [stk(K, 2)])] };
  h.passives = [
    pas('이미 손을 써 두었습니다', 'foeActBefore', [summon(K, 0.4, { max: 3 })], { when: { type: '공격' }, limit: 2 }),
    pas('계획은 그 자리에서', 'kill', [draw(1)], { when: { mine: true }, limit: 1 }),
  ];
  setCards(j, [
    // 시동 — 가시 덫 설치: 촉수 둘 + 드로우
    U(H, 1, '가시 덫 설치', 1, '스킬', [stk(K, 2), draw(1)], named([
      Or([stk(K, 3), draw(1)]),
      Or([stk(K, 1), draw(1)], { cost: 0 }),
      Or([stk(K, 2), sh(0.8), draw(1)]),
      Or([stk(K, 2), draw(1), pw('foeActBefore', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([stk(K, 4), draw(1), disc(1)]),
    ]), null),
    // 둘째 — 가시 작살 연사: 무작위 3회 + 촉수
    U(H, 2, '가시 작살 연사', 1, '공격', [dmg(0.45, ER, { hits: 3 }), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.4, ER, { hits: 3 }), summon(K, 0.25, { max: 3 })]),
      ['D', 'summonAct', [dmg(0.25, ER)], { when: { id: K, kind: 'atk' }, limit: 1 }],
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 따끔한 문지기: 적 전체 + 촉수 1개당 적 전체, 촉수를 모두 거둔다
    U(H, 3, '따끔한 문지기', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.3, EA), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.9), per(K), dmg(0.45), spendAll(K)]),
      ['D', 'turnEnd', [summon(K, 0.2, { max: 3 })]],
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 마녀의 힘은 대단했다!(강화): 촉수 + 매 턴 끝에 촉수가 모두 찌른다
    U(H, 4, '마녀의 힘은 대단했다!', 1, '강화', [stk(K, 1), pw('turnEnd', [summon(K, 0.25, { max: 5 })])], named([
      Or([stk(K, 2), pw('turnEnd', [summon(K, 0.25, { max: 5 })])]),
      Or([pw('turnEnd', [summon(K, 0.22, { max: 5 })])], { cost: 0 }),
      Or([stk(K, 2), draw(1), pw('summonAct', [stk(K, 1), dmg(0.15, ER)], { when: { id: K, kind: 'atk' }, limit: 1 })]),
      Or([stk(K, 1), srch(), pw('turnEnd', [summon(K, 0.25, { max: 5 })])]),
      Or([stk(K, 3), pw('turnEnd', [summon(K, 0.25, { max: 5 })]), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 원작 자유 — 가시 울타리: 실드 + 촉수 둘
    U(H, 5, '가시 울타리', 1, '스킬', [sh(1.2), stk(K, 2)], [
      'A', 'B',
      Or([sh(1.0), stk(K, 1), summon(K, 0.25, { max: 3 })]),
      ['D', 'foeActBefore', [summon(K, 0.15, { max: 2 })], { when: { type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('guard', 'cost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 피코라 — 회복형 · 서포터 · 냉정. 「스티커로 조금씩 낫게 해 넘치게 하고, 넘친 만큼 가장 센 아군을 치장한다」 — 넘친 회복 → 「인증 스티커」(그 아군 주는 피해↑, 전투 내내)
// 원작: 저학년 한정 스티커(회복 + 붙은 동안 받는 회복↑) · 고학년 너도 될 수 있다 패션피플(가장 튼튼한 아군 치장) · 어사이드 쇼핑백(스티커 — 회복 2번 · 주는 피해↑) · 「멋진 것」엔 인증 스티커
// 회복형 안 자리: 넘침을 만드는 법 = 붙여 둔 스티커(턴 끝 회복), 넘친 것 = 아군 한 명의 치장(사도에게 붙는 지속 피해↑) — 카렌(파티 다음 카드 한 번) · 큐이(공격 카드)와 달리 「사람에게 남는다」
// 옛 「첫 세트 · 한정판 세트」(4에 저절로 회복 · 피해 감소) 뺌 — 스티커는 다 차면 막히고, 해골 스티커로 턴다
// 시동: u1 반짝이 스티커(그대로)
// ════════════════════════════════════════════════════════════════════
function pikora(j) {
  const H = '피코라', K = '스티커', A = '인증 스티커', S = '피코라_sticker';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '붙은 동안 조금씩 낫게 하는 한정 스티커', carrier: 'self', cap: 4, per: [{ stat: 'hot', ratio: 0.1 }, { stat: 'taken', v: -0.03, who: 'allies' }],
    rules: [pas('스티커 붙이기', 'play', [stk(K, 1)], { limit: 2 })] };
  h.keywords = [{ name: A, desc: '멋진 것에만 붙여 주는 피코라의 인증 스티커', carrier: 'hero', cap: 3, per: [{ stat: 'dealt', v: 0.08 }] }];
  h.passives = [
    pas('인증 완료', 'overheal', [stk(A, 1, STRONG)], { when: { pct: 0.8 }, limit: 2 }),
    pas('사고 나면 일단 도망', 'lowHp', [st('피해 감소', 2), draw(1)], { when: { pct: 0.3 }, per: 'fight', limit: 1 }),
  ];
  const sticker = j.cards.find(c => c.id === S);
  Object.assign(sticker, { fx: [heal(0.3), stk(K, 1)] });
  setCards(j, [
    // 시동 — 반짝이 스티커: 회복 + 스티커 둘
    U(H, 1, '반짝이 스티커', 1, '스킬', [heal(0.6), stk(K, 2)], named([
      Or([heal(0.8), stk(K, 2)]),
      Or([heal(0.45), stk(K, 1)], { cost: 0 }),
      Or([heal(0.5), stk(K, 1), make(S, 1)]),
      Or([heal(0.5), stk(K, 2), pw('overheal', [stk(A, 1, STRONG)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(0.6), stk(K, 2), srch()]),
    ]), null),
    // 원작 자유 — 한정 스티커: 회복 + 피해 감소 + 스티커
    U(H, 2, '한정 스티커', 1, '스킬', [heal(0.8), st('피해 감소', 1), stk(K, 1)], [
      'A', 'B',
      Or([heal(0.7), stk(A, 1, ALLY), make(S, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([heal(1.4), st('피해 감소', 2), disc(1)]),
    ], bl('heal', 'draw', [make(S, 1)])),
    // 쓰기 — 해골 스티커 도배(「나빴어요」): 방어 기반 + 스티커 1개당 더, 스티커 전부 소모
    U(H, 3, '해골 스티커 도배', 1, '공격', [ddef(0.4), per(K), ddef(0.2), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.35), per(K), ddef(0.18), st('약화', 2)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 }],
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 쇼핑왕 피코라: 스티커 1개당 회복(넘치게) + 고른 아군에게 인증 스티커
    U(H, 4, '쇼핑왕 피코라', 1, '스킬', [heal(0.5), per(K), heal(0.2), stk(A, 1, ALLY)], [
      'A', 'B',
      Or([heal(0.5), per(K), heal(0.2), make(S, 1)]),
      Or([heal(0.5), per(K), heal(0.2), pw('overheal', [stk(A, 1, STRONG), stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('heal', 'ap', [stk(K, 1)])),
    // 둘째 — 반짝 구속 마법: 방어 기반 + 스티커 둘
    U(H, 5, '반짝 구속 마법', 1, '공격', [ddef(0.75), stk(K, 2)], [
      'A', 'B',
      Or([ddef(0.6), st('약화', 1), stk(K, 2)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '마녀/포셔': 0.8, '마녀/프리클': 1.0, '마녀/피코라': 1.15 };
// ── 돌리기 ──
const T = fn => j => { fn(j); keepTags(j); };
const JOBS = [
  ['마녀/아사나', T(asana)], ['마녀/아야', T(aya)], ['마녀/요미', T(yomi)],
  ['마녀/포셔', T(portia)], ['마녀/프리클', T(prickle)], ['마녀/피코라', T(pikora)],
];
L.run18(JOBS, TUNE, new URL('./boost_마녀B_18.json', import.meta.url));
