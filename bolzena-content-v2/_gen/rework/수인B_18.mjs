// 18갈래 재설계 3단계 — 수인B 묶음 9명(베니 · 베니(베니) · 슈로 · 스패럿 · 에피카 · 우로스 · 유미미 · 코미(수영복) · 티그(영웅)). 2026-10-08
// 기준: 05_시범16_결과.md · 지침 _gen/rework/BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/수인B.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동은 공용 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/수인B_18.mjs [사도 이름 일부]  → heroes/수인/<파일>.json 덮어쓰기(백업 heroes_before_118 에서 읽음)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, power, rule,
  TOP, tough, disc, gauge, empower, spendN, cs, pw, pas, token, xtra, srch, drawType, pull, exile, ifFoe,
  Or, U, bl, setCards } = L;
const MARK = 'markedEnemy', LOW = 'lowEnemy';
const ifHunted = { k: 'ifHunted' };
const summon = L.summon;
const named = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
// 손으로 짠 신탁(Or)이 태그를 안 적었으면 카드 태그를 그대로 잇는다(신탁 태그는 카드 태그를 갈아 끼우므로 — 빼는 것은 'Ht' · tags: [] 로만)
const keepTags = j => { for (const c of j.cards) if (c.unique && c.tags) for (const o of c.oracles || []) if (o.tags === undefined) o.tags = [...c.tags]; };

// ════════════════════════════════════════════════════════════════════
// 1. 베니 — 손 만들기형 · 딜러 · 활발. 식사 대장 — 생선을 만들어 지금 먹을까(회복 · 포만감 = 다음 공격 +) 손에 쥐고 도끼에 실을까(손의 생선 1장당 · 도끼가 다 먹어 치움)
// 원작: 저학년 생선 먹어 HP 회복 · 고학년 도끼 내려찍기 범위 + 긴 기절 · 먹성 대단 · 요리 잘하는 「식사 대장」 · 폭탄 낚시
// 에르핀(케이크를 먹을까 탄알로 쏟을까)과 가름 — 베니는 쥔 생선이 「도끼 무게」(한 방 단일 · 기절), 처치하면 생선을 또 낚는다
// 시동: u1 생선 꿀~꺽!(그대로) · 장치가 자기 카드를 만든다(전과 같음 — 전투 시작 · 처치)
// ════════════════════════════════════════════════════════════════════
function benny(j) {
  const H = '베니', K = '포만감', FISH = '베니_fish';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '꿀 절인 생선으로 채운 배', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.15 }], consume: 1 };
  h.passives = [
    pas('먹고 나면 힘', 'kill', [make(FISH, 1), stk(K, 1)], { when: { mine: true }, limit: 1 }),
    pas('나를 따르라 곰', 'fightStart', [st('피해 감소', 1), make(FISH, 1)]),
  ];
  const fish = j.cards.find(c => c.id === FISH);
  Object.assign(fish, { fx: [heal(0.35), stk(K, 1)], blurb: '배고픈 곰이 아껴 두는 비상식량 — 지금 먹을까, 도끼에 실을까' });
  const eatAll = exile('hand', { all: true, tag: FISH });
  setCards(j, [
    // 시동 — 생선 꿀~꺽!: 하나는 지금 먹고(회복 · 포만감) 하나는 손에
    U(H, 1, '생선 꿀~꺽!', 1, '스킬', [heal(0.6), stk(K, 1), make(FISH, 1)], named([
      Or([heal(0.8), stk(K, 1), make(FISH, 2)]),
      Or([heal(0.4), stk(K, 1), make(FISH, 1)], { cost: 0 }),
      Or([heal(0.5), make(FISH, 2), draw(1)]),
      Or([heal(0.6), make(FISH, 1), pw('kill', [make(FISH, 1)], { limit: 1 })], { power: true }),
      Or([stk(K, 2), make(FISH, 2), disc(1)]),
    ]), null),
    // 원작 자유 — 힘으로 하는 공사: 힘센 한 방 + 강인도, 배가 부르면 한 번 더 무겁게
    U(H, 2, '힘으로 하는 공사', 1, '공격', [dmg(1.0), tough(1), ifStack(K, 1), dmg(0.45)], [
      'A',
      ['C', [st('기절', 1)]],
      Or([dmg(0.7, EA), tough(1), ifStack(K, 1), dmg(0.3, EA)]),
      Or([dmg(1.0), tough(1), make(FISH, 1)]),
      Or([dmg(1.0), tough(1), pw('make', [dmg(0.4, ER)], { limit: 2 })], { power: true }),
    ], bl('power', 'frost', [make(FISH, 1)])),
    // 둘째 — 사료스탕스 뽀너스: 손의 생선 1장당 더 + 생선 1(쥐고 있을수록 커지는 굴리기)
    U(H, 3, '사료스탕스 뽀너스', 1, '공격', [dmg(0.75), perTag(FISH), dmg(0.3), make(FISH, 1)], [
      'A', 'B',
      Or([dmg(0.65, EA), perTag(FISH), dmg(0.25, EA), make(FISH, 1)]),
      Or([dmg(0.7), perTag(FISH), dmg(0.28), pw('turnStart', [make(FISH, 1)])], { power: true }),
      'Hd',
    ], bl('power', 'draw', [make(FISH, 1)])),
    // 쓰기 — 배고픈 곰의 도끼(2): 손의 생선 1장당 더, 손의 생선을 모두 먹어 치운다(먹을 생선은 먼저 먹을 것)
    U(H, 4, '배고픈 곰의 도끼', 2, '공격', [dmg(1.7), perTag(FISH), dmg(0.5), eatAll], [
      'A', 'B',
      Or([dmg(1.2, EA), perTag(FISH), dmg(0.35, EA), eatAll]),
      Or([dmg(1.5), perTag(FISH), dmg(0.45), ifKill, make(FISH, 2)]),
      Or([dmg(1.6), perTag(FISH), dmg(0.5), pw('make', [stk(K, 1)], { limit: 1 })], { power: true }),
    ], bl('power', 'ap', [make(FISH, 1)])),
    // 갈래 부품 — 앞장서는 곰: 실드 + 생선 둘(먹을 몫 · 쥘 몫)
    U(H, 5, '앞장서는 곰', 1, '스킬', [sh(1.0), make(FISH, 2)], [
      'A', 'B',
      Or([sh(1.0), make(FISH, 2), drawType('공격')]),
      Or([sh(1.0), make(FISH, 1), pw('turnStart', [make(FISH, 1)])], { power: true }),
      'Hd',
    ], bl('guard', 'cost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 베니(베니) — 쌓고 고르기 · 딜러 · 냉정. 적에게 바른 꿀을 지금 터뜨릴까, 기절선(넷)까지 모을까 — 터뜨리면 꿀을 나눠 파티가 낫는다
// 원작: 허니밤(웅덩이 위 적에게 꿀범벅) · 넷째 평타 꿀 주먹 · 고학년 꿀범벅이 가장 많은 적에게 날아가 중첩만큼 · 전부 소모 · 일정 이상이면 기절 · 돌아오며 아군 회복
// 다 차도 저절로 터지지 않는다(꿀범벅 최대 6 — 넘치면 그냥 막힌다). 터뜨리는 것은 늘 카드
// 시동: u1 허니밤(그대로) · 장치가 자기 카드를 만든다(꿀 주먹 — 전과 같음)
// ════════════════════════════════════════════════════════════════════
function bennyBenny(j) {
  const H = '베니_베니', K = '꿀범벅', JAR = '베니_베니_jar';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '끈적하게 들러붙는 꿀', carrier: 'enemy', cap: 6, per: [{ stat: 'dealt', v: -0.04 }] };
  h.passives = [
    pas('꿀 주먹', 'play', [make(JAR, 1)], { when: { every: 4 } }),
    pas('베니는 베니야', 'spend', [heal(0.5)], { when: { id: K, n: 3 }, limit: 1 }),
  ];
  setCards(j, [
    // 시동 — 허니밤: 적 전체 + 꿀범벅, 고른 적에게 한 번 더
    U(H, 1, '허니밤', 1, '공격', [dmg(0.45, EA), stk(K, 1, EA), dmg(0.45)], named([
      Or([dmg(0.6, EA), stk(K, 1, EA), dmg(0.6)]),
      Or([dmg(0.35, EA), stk(K, 1, EA), dmg(0.35)], { cost: 0 }),
      Or([dmg(0.4, EA), stk(K, 1, EA), make(JAR, 1)]),
      Or([dmg(0.45, EA), stk(K, 1, EA), pw('spend', [stk(K, 1, EA)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([dmg(0.45, EA), stk(K, 1, EA), srch({ type: '공격' })]),
    ]), null),
    // 원작 자유 — 꿀 웅덩이: 적 전체 꿀범벅 둘 + 약화 + 드로우
    U(H, 2, '꿀 웅덩이', 1, '스킬', [stk(K, 2, EA), st('약화', 1, EA), draw(1)], [
      'A', 'B',
      Or([stk(K, 2, EA), st('약화', 1, EA), make(JAR, 1)]),
      Or([stk(K, 3, EA), st('약화', 1, EA), pw('turnStart', [stk(K, 1, EA)])], { power: true }),
      Or([stk(K, 4, EA), st('약화', 2, EA), disc(1)]),
    ], bl('draw', 'frost', [make(JAR, 1)])),
    // 둘째 — 꿀 던지기: 피해 + 꿀범벅, 꿀범벅 1개당 조금(모으는 동안 쓰는 카드)
    U(H, 3, '꿀 던지기', 1, '공격', [dmg(0.75), stk(K, 1), per(K), dmg(0.15)], [
      'A', 'B',
      Or([dmg(0.5, EA), stk(K, 1, EA), per(K, { each: true }), dmg(0.1, EA)]),
      Or([dmg(0.7), stk(K, 2), srch()]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 꿀단지 강타(2): 꿀범벅 1개당(최소 2), 넷 이상이면 기절, 전부 소모
    U(H, 4, '꿀단지 강타', 2, '공격', [per(K, { n: 2 }), dmg(0.5), ifStack(K, 4), st('기절', 1), spendAll(K)], [
      'A', 'B',
      Or([per(K, { n: 2, each: true }), dmg(0.36, EA), ifStack(K, 4), st('기절', 1, EA), spendAll(K)]),
      Or([per(K, { n: 2 }), dmg(0.55), heal(0.9), ifStack(K, 4), st('기절', 1)]),
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 비상 꿀단지(0): 꿀단지 폭탄 + 드로우
    U(H, 5, '비상 꿀단지', 0, '스킬', [make(JAR, 1), draw(1)], [
      Or([make(JAR, 2), draw(1)]),
      Or([make(JAR, 1), draw(1), st('약화', 1, EA)]),
      Or([make(JAR, 1), draw(1), stk(K, 1, EA)]),
      Or([make(JAR, 1), pw('spend', [make(JAR, 1)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([make(JAR, 2), draw(1), disc(1)]),
    ], bl('cost', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 슈로 — 거드는 형 · 서포터 · 활발. 검의 영역을 깔아 두면 동료가 무슨 카드를 내든 그 종류에 맞는 덕(공격 → 검기 · 스킬 → 실드 · 강화 → 회복)
// 원작: 저학년 「조율」 — 영역 위 아군 받는 피해↓ + 역할마다 다른 이득 · 강화 평타 세 번 맞으면 물러났다 전진 · 칼을 맞대야 친구 · 가짜 우로스
// 우로스와 가름 — 우로스는 자기 힘을 쌓아 모두에게 같은 공격력, 슈로는 영역 위 동료 카드의 「종류」를 본다
// 시동: u4 독 없는 투구꽃(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function shuro(j) {
  const H = '슈로', K = '검의 영역';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '검 끝으로 그어 세운 둘레', carrier: 'ally', cap: 3, decay: 1,
    per: [{ stat: 'taken', v: -0.06 }],
    rules: [{ name: '검으로 사귄 친구', when: { on: 'fightStart' }, fx: [stk(K, 1)] }],
  };
  const inField = [{ c: 'stack', id: K, n: 1 }];
  h.passives = [
    pas('역할의 검', 'play', [ddef(0.3, ER)], { when: { who: 'other', type: '공격' }, conds: inField, limit: 1 }),
    pas('역할의 검', 'play', [sh(0.35)], { when: { who: 'other', type: '스킬' }, conds: inField, limit: 1 }),
    pas('역할의 검', 'play', [heal(0.4)], { when: { who: 'other', type: '강화' }, conds: inField, limit: 1 }),
    pas('물러났다 전진', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }),
  ];
  setCards(j, [
    // 둘째 — 검 조율: 영역 둘 + 실드 + 드로우
    U(H, 1, '검 조율', 1, '스킬', [stk(K, 2), sh(0.8), draw(1)], [
      'A', 'B',
      Or([stk(K, 3), sh(1.2), cs('비용', -1, { to: 'hand', n: 2, who: 'other' })]),
      Or([stk(K, 2), sh(0.7), pw('play', [stk(K, 1)], { when: { who: 'other', type: '강화' }, limit: 1 })], { power: true }),
      Or([stk(K, 3), sh(1.8), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 원작 자유 — 통성명: 칼을 맞대고 친구가 된다(피해 + 영역 + 실드)
    U(H, 2, '통성명', 1, '공격', [dmg(0.85), stk(K, 1), sh(0.5)], [
      'A', 'B',
      Or([dmg(0.6, EA), stk(K, 1), sh(0.4)]),
      Or([dmg(0.8), stk(K, 1), srch({ type: '스킬' })]),
      ['D', 'play', [ddef(0.3, ER)], { when: { who: 'other', type: '공격' }, limit: 1 }],
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 장검 · 단도 · 한손검: 3회 + 영역 1개당 적 전체 방어 기반, 영역 전부 걷기
    U(H, 3, '장검 · 단도 · 한손검', 1, '공격', [dmg(0.3, E1, { hits: 3 }), per(K), ddef(0.25, EA), spendAll(K)], [
      'A',
      ['C', [st('둔화', 1, EA)]],
      Or([dmg(0.3, E1, { hits: 3 }), per(K), ddef(0.22, EA), srch()]),
      Or([dmg(0.35, E1, { hits: 3 }), per(K), ddef(0.3, EA)]),
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 독 없는 투구꽃(개전 강화): 결의 + 매 턴 영역
    U(H, 4, '독 없는 투구꽃', 1, '강화', [st('결의', 1), pw('turnStart', [stk(K, 1)])], named([
      Or([st('결의', 1), stk(K, 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('결의', 1), stk(K, 1), pw('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 })], { tags: ['개전'] }),
      Or([st('결의', 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([st('결의', 2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 옛 벗의 검: 영역 + 손의 동료 카드 비용 -1 + 드로우(동료 카드를 먼저 내게)
    U(H, 5, '옛 벗의 검', 1, '스킬', [stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), draw(1)], [
      'A', 'B',
      Or([stk(K, 2), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), sh(0.6)]),
      Or([stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'other' }), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 2), pull({ who: 'other' }), draw(1)]),
    ], bl('draw', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 스패럿 — 표적형 · 서포터 · 순수. 「오늘은 저 녀석이다!」 — HP 가장 높은 적에게 낙인(찍기), 그놈을 털어 전리품을 나눈다. 노획물이 넷이면 「금은보화」(손에 카드)
// 원작: 저학년 최대 HP 가장 높은 적에게 해적의 낙인 + 전리품을 아군에게 · 고학년 낙인 적 먼저 집중포화 + 보호막 파괴 · 어사이드 찬란한 금은보화 · 보물 좋아하는 낭만파 선장
// 그윈(먼저 꽂은 깃발 → 지도) · 란(사냥감 → 다음 사냥감)과 가름 — 스패럿은 늘 「가장 튼튼한 놈」을 찍고, 쓰러뜨린 결과가 회복 · 드로우(전리품)
// 시동: u1 오늘은 저녀석이다!(그대로) · 장치가 자기 카드를 만든다(전과 같음 — 전리품. 노획물 넷 → 금은보화는 옛 「통 큰 분배」(AP)를 카드로)
// ════════════════════════════════════════════════════════════════════
function sparrot(j) {
  const H = '스패럿', K = '노획물', N = '해적의 낙인', LOOT = '스패럿_t1', GOLD = '스패럿_gold';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '배에 쌓아 두고 나눠 먹는 노획물', carrier: 'self', cap: 5, per: [{ stat: 'hot', ratio: 0.12 }], onMax: { make: GOLD, consume: true } };
  h.keywords = [{ name: N, desc: '오늘 털기로 정한 놈의 표시', carrier: 'enemy', cap: 1, hunt: true, per: [{ stat: 'taken', v: 0.08 }] }];
  h.passives = [
    pas('해적의 약탈', 'huntDown', [make(LOOT, 2), stk(N, 1, TOP)]),
    pas('해적의 약탈', 'foeShieldBreak', [make(LOOT, 1)], { when: { mine: true }, limit: 1 }),
    pas('진정한 해적의 길', 'fightStart', [stk(N, 1, TOP), make(LOOT, 1), gauge(20)]),
  ];
  const tokens = [token(GOLD, '찬란한 금은보화', H, '스킬', [ap(1)], { blurb: '배에 그득 쌓인 보물 — 선장은 통 크게 나눠 줍니다' })];
  setCards(j, [
    // 시동 — 오늘은 저녀석이다!: HP 가장 높은 적에게 피해 + 낙인 + 전리품
    U(H, 1, '오늘은 저녀석이다!', 1, '공격', [dmg(0.7, TOP), stk(N, 1, TOP), make(LOOT, 1)], named([
      Or([dmg(0.95, TOP), stk(N, 1, TOP), make(LOOT, 1)]),
      Or([dmg(0.55, TOP), stk(N, 1, TOP), make(LOOT, 1)], { cost: 0 }),
      Or([stk(N, 1, TOP), make(LOOT, 2), draw(1)]),
      Or([dmg(0.8, TOP), stk(N, 1, TOP), pw('huntDown', [make(LOOT, 2)], { limit: 1 })], { power: true }),
      Or([dmg(0.9, TOP), make(LOOT, 1), srch()]),
    ]), null, ['분쇄']),
    // 원작 자유 — 해적의 언어: 피해 + 약화, 낙인 찍힌 적이면 전리품
    U(H, 2, '해적의 언어', 1, '공격', [dmg(0.8), st('약화', 2), ifHunted, make(LOOT, 1)], [
      'A', 'B',
      Or([dmg(0.6, EA), st('약화', 1, EA), make(LOOT, 1)]),
      Or([dmg(1.2), st('약화', 2), srch()]),
      Or([dmg(0.7), st('약화', 2), pw('hit', [make(LOOT, 1)], { limit: 1 })], { power: true }),
    ], bl('power', 'frost', [make(LOOT, 1)]), ['분쇄']),
    // 둘째 — 약탈한 전리품: 전리품 둘 + 노획물
    U(H, 3, '약탈한 전리품', 1, '스킬', [make(LOOT, 2), stk(K, 1)], [
      'A', 'B',
      Or([make(LOOT, 2), sh(0.8), draw(1)]),
      Or([make(LOOT, 2), pw('huntDown', [make(LOOT, 2)])], { power: true }),
      Or([make(LOOT, 3), disc(1)]),
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 쓰기 — 플라즈마 포 일제사격(2): 적 전체 + 손의 전리품 1장당, 낙인 찍힌 적은 실드 파괴, 손의 전리품 전부 소멸
    U(H, 4, '플라즈마 포 일제사격', 2, '공격', [dmg(0.8, EA), perTag(LOOT), dmg(0.32, EA), exile('hand', { all: true, tag: LOOT })], [
      'A', 'B',
      Or([dmg(1.4), perTag(LOOT), dmg(0.55), exile('hand', { all: true, tag: LOOT })]),
      Or([dmg(0.75, EA), perTag(LOOT), dmg(0.3, EA), ifHunted, { k: 'strip' }]),
      Or([dmg(0.7, EA), perTag(LOOT), dmg(0.28, EA), pw('huntDown', [dmg(0.4, EA)], { limit: 1 })], { power: true }),
    ], bl('power', 'ap', [make(LOOT, 1)]), ['분쇄']),
    // 갈래 부품 — 노획물 장부: 고른 적에게 낙인(오늘의 표적 바꾸기) + 노획물 + 드로우
    U(H, 5, '노획물 장부', 1, '스킬', [stk(N, 1), stk(K, 1), draw(1)], [
      'A', 'B',
      Or([stk(N, 1), heal(0.6), make(LOOT, 1)]),
      Or([stk(N, 1), stk(K, 1), pw('turnStart', [stk(K, 1), heal(0.2)])], { power: true }),
      Or([stk(N, 1), make(LOOT, 2), disc(1)]),
    ], bl('cost', { tags: ['보존'] }, [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 5. 에피카 — 소환형 · 딜러 · 활발 · 엘다인. 에피카는 직접 싸우지 않는다 — 에피콘(소환물)이 따라 치고 대신 맞고, 무대(연주)에 올리면 매 턴 합주한다
// 원작: 늘 에피콘이 대신 싸움 · 평타 에피콘 · 강화 용감한 에피콘 범위 · 저학년 극적인 연출(평타 강화 + 크게 맞는 한 번 버팀) · 고학년 연주 동안 에피콘이 무작위 적 · 만족스러운 연주
// 쥬비(떼 — 수) · 모모(깨뜨려 터뜨림)와 가름 — 에피카는 「무대」: 연주 중인 동안 매 턴 에피콘 합주(소환물 행동), 연주가 끝나면 만족스러운 연주(엘다인 한 단계)
// 시동: u3 에피칸(그대로)
// ════════════════════════════════════════════════════════════════════
function epica(j) {
  const H = '에피카', K = '에피콘', P = '연주 중';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '따라 치고 대신 맞는 분홍 극단원', carrier: 'self', cap: 3, guard: true, cut: 0.3 };
  h.keywords = [{
    name: P, desc: '교주에게 바치는 영웅담 연주', carrier: 'self', cap: 2, decay: 1,
    rules: [
      { name: '에피콘 합주', when: { on: 'turnStart' }, conds: [{ c: 'stack', id: P, n: 1 }], fx: [{ k: 'cue', id: 'epica_play', xStack: K, n: 2 }, summon(K, 0.3)] },
      { name: '만족스러운 연주', when: { on: 'stackGone', id: P, decay: true }, fx: [{ k: 'cue', id: 'epica_finale' }, st('사기', 1), draw(1)] },
    ],
  }];
  h.passives = [
    pas('에피콘 극단', 'play', [summon(K, 0.25, { max: 3 })], { when: { type: '공격' }, limit: 1 }),
    pas('일생일대의 버티기', 'lowHp', [st('끈기', 1), stk(K, 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 원작 자유 — 극적인 연출: 에피콘 + 다음 카드 크게 + 실드(한 번 버틴다)
    U(H, 1, '극적인 연출', 1, '스킬', [stk(K, 1), empower(0.4), sh(0.6)], [
      'A', 'B',
      Or([stk(K, 1), empower(0.4), st('끈기', 1)]),
      Or([stk(K, 1), empower(0.4), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'guard' }, limit: 1 })], { power: true }),
      Or([stk(K, 1), empower(0.4), srch({ type: '공격' })]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 용감한 에피콘: 피해 + 에피콘이 모두 적 전체로 따라 친다, 에피콘 전부 소모
    U(H, 2, '용감한 에피콘', 1, '공격', [dmg(0.55, EA), summon(K, 0.32, { target: EA, max: 3 }), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.7), summon(K, 0.5, { target: E1 }), spendAll(K)]),
      Or([dmg(0.5, EA), summon(K, 0.28, { target: EA, max: 3 }), srch()]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 에피칸: 에피콘 + 드로우 + 실드
    U(H, 3, '에피칸', 1, '스킬', [stk(K, 1), draw(1), sh(0.8)], named([
      Or([stk(K, 2), draw(1), sh(0.8)]),
      Or([stk(K, 1), draw(1)], { cost: 0 }),
      Or([stk(K, 2), stk(P, 1), draw(1)]),
      Or([stk(K, 1), draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 2), draw(2), disc(1)]),
    ]), null),
    // 둘째 — 만족스러운 연주(강화): 사기 + 공격 카드를 내면 에피콘
    U(H, 4, '만족스러운 연주', 1, '강화', [st('사기', 1), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], named([
      Or([st('사기', 1), stk(K, 1), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })]),
      Or([stk(K, 1), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { cost: 0 }),
      Or([st('사기', 1), stk(K, 1), pw('summonAct', [stk(K, 2)], { when: { id: K, kind: 'lost' }, limit: 1 })]),
      Or([st('사기', 1), srch(), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })]),
      Or([st('사기', 2), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 바치는 무대: 피해 + 에피콘 + 연주 시작(다음 턴부터 합주)
    U(H, 5, '바치는 무대', 1, '공격', [dmg(0.7), stk(K, 1), stk(P, 1)], [
      'A', 'B',
      Or([dmg(0.5, EA), stk(K, 1), stk(P, 1)]),
      Or([dmg(0.6), stk(P, 1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([dmg(1.0), stk(P, 2), disc(1)]),
    ], bl('power', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 우로스 — 거드는 형 · 서포터 · 공명 · 엘다인. 지배의 영역을 펼칠수록(최대 셋) 모두의 공격력이 오르고, 넘친 영역은 파티의 다음 카드로 — 영역이 다 걷히면 우로스가 친다
// 원작: 저학년 「순환」 — 쓸 때마다 영역이 넓어짐(3번) · 영역 위 가장 센 아군 공격력에 비례해 모든 아군 공격력 · 영역이 끝나면 자기 피해↑ · 몸은 패왕, 마음은 슈로
// 슈로와 가름 — 슈로는 동료 카드 「종류」를 보고 덕을 고르고, 우로스는 자기가 쌓은 힘을 「모두에게 같게」 나눈다(넘치면 다음 카드). 고학년 · 어사이드의 같은 성격 인원 세기는 옮기지 않는다
// 엘다인 한 단계: 영역이 다 닳으면(끝나면) 자신의 다음 카드 강화
// 시동: u3 세계수의 보물 파괴(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function uros(j) {
  const H = '우로스', K = '지배의 영역';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '가장 강한 자의 힘을 나누는 영역', carrier: 'self', cap: 3, decay: 1,
    per: [{ stat: 'atk', v: 0.06, who: 'allies' }],
    rules: [
      { name: '영역 전개', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
      { name: '내 마음의 등대', when: { on: 'stackOver', id: K }, limit: { per: 'turn', n: 1 }, fx: [empower(0.2, 'any')] },
      { name: '패왕의 귀환', when: { on: 'stackGone', id: K, decay: true }, fx: [empower(0.4)] },
    ],
  };
  h.passives = [
    pas('순환', 'shuffle', [stk(K, 2), draw(1)], { per: 'fight', limit: 3 }),
    pas('변치 않는 것', 'lowHp', [{ k: 'cleanse', v: 1 }, st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 둘째 — 순환의 고리: 버리고 뽑기(더미를 빨리 돌려 「순환」 을 부른다) + 영역
    U(H, 1, '순환의 고리', 1, '스킬', [disc(2), draw(3), stk(K, 1)], [
      'A', 'B',
      Or([draw(3), stk(K, 1), sh(0.8)]),
      Or([draw(3), pw('shuffle', [stk(K, 2), empower(0.3, 'any')], { limit: 1 })], { power: true }),
      Or([draw(3), stk(K, 2)]),
    ], bl('draw', 'cost', [stk(K, 1)])),
    // 원작 자유 — 마음이 비수가 되어: 무작위 2회 + 영역 1개당 한 발 + 적 전체 취약
    U(H, 2, '마음이 비수가 되어', 1, '공격', [dmg(0.3, ER, { hits: 2 }), per(K), dmg(0.3, ER), st('취약', 1, EA)], [
      'A', 'B',
      Or([dmg(0.35, ER, { hits: 2 }), per(K), dmg(0.35, ER), stk(K, 1)]),
      ['D', 'turnStart', [dmg(0.3, ER, { hits: 2 })]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 세계수의 보물 파괴(개전 강화): 협공 + 매 턴 영역
    U(H, 3, '세계수의 보물 파괴', 1, '강화', [st('협공', 1), pw('turnStart', [stk(K, 1)])], named([
      Or([st('협공', 1), stk(K, 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('협공', 1), stk(K, 1), pw('play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 })], { tags: ['개전'] }),
      Or([st('협공', 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([st('협공', 2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 쓰기 — 거대한 뱀(2): 적 전체 + 영역 1개당, 영역을 모두 걷는다
    U(H, 4, '거대한 뱀', 2, '공격', [dmg(1.0, EA), per(K), dmg(0.3, EA), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.6), per(K), dmg(0.45), spendAll(K)]),
      Or([dmg(0.95, EA), per(K), dmg(0.28, EA), empower(0.4, 'any')]),
      Or([dmg(0.9, EA), per(K), dmg(0.25, EA), pw('spend', [empower(0.3, 'any')], { when: { id: K }, limit: 1 })], { power: true }),
    ], bl('power', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 똬리 틀기(0): 영역 + 파티의 다음 카드 강화(지배자의 몫)
    U(H, 5, '똬리 틀기', 0, '스킬', [stk(K, 1), empower(0.25, 'any')], [
      'A', 'B',
      Or([stk(K, 1), draw(1)]),
      Or([stk(K, 1), empower(0.15, 'any'), pw('turnStart', [empower(0.15, 'any')])], { power: true }),
      Or([stk(K, 2), empower(0.4, 'any'), disc(1)]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 유미미 — 아껴 두기형 · 딜러 · 광기 · 1성. 늘어져 기다린다 — AP 를 남기거나 보존 카드를 쥐고 턴을 마치면 나른함, 쌓인 나른함은 다음 공격 한 발에 몽땅
// 원작: 평타 · 저학년 · 고학년 모두 가장 멀리 있는 적 한 명 · 디아나 뿔로 깎은 화살(유령도 꿰뚫음) · 나른한 사냥꾼 · 「약하면 설치지 마」
// 시동: u3 수풀에 늘어지기(보존, 그대로)
// ════════════════════════════════════════════════════════════════════
function yumimi(j) {
  const H = '유미미', K = '나른함';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '늘어진 채 겨누는 한 발', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.33 }], consumeAll: true,
    rules: [{ name: '한적한 숲', when: { on: 'fightStart' }, fx: [stk(K, 2)] }, { name: '늘어진 하루', when: { on: 'turnEnd' }, fx: [stk(K, 1)] }],
  };
  h.passives = [
    pas('늘어지기', 'keepAp', [stk(K, 1)]),
    pas('약하면 설치지 마', 'kill', [ap(1), draw(1)], { limit: 1 }),
  ];
  setCards(j, [
    // 쓰기 — 발싸! 아뵤~: 가장 튼튼한(먼) 적에게 한 발, 나른함이 셋이면 강인도
    U(H, 1, '발싸! 아뵤~', 1, '공격', [dmg(1.3, TOP), ifStack(K, 3), tough(2, TOP)], [
      'A', 'B',
      Or([dmg(1.3, TOP), ifStack(K, 2), xtra(0.7, TOP)]),
      Or([dmg(1.3, TOP), tough(1, TOP), srch()]),
      Or([dmg(1.1, TOP), pw('keepAp', [dmg(0.5, TOP)], { limit: 1 }), ifStack(K, 3), tough(2, TOP)], { power: true }),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 원작 자유 — 유령도 꿰뚫는 화살: 실드를 깨고 꿰뚫기 + 나른함
    U(H, 2, '유령도 꿰뚫는 화살', 1, '공격', [{ k: 'strip' }, dmg(1.0), stk(K, 1)], [
      'A', 'B',
      Or([{ k: 'strip' }, dmg(0.95), srch({ type: '스킬' })]),
      ['D', 'keepAp', [stk(K, 1), sh(0.35)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 수풀에 늘어지기(보존): 나른함 둘 + 실드(쥐고 넘기면 「늘어지기」 도 돈다)
    U(H, 3, '수풀에 늘어지기', 1, '스킬', [stk(K, 2), sh(1.0)], named([
      Or([stk(K, 2), sh(1.3)], { tags: ['보존'] }),
      Or([stk(K, 1), sh(0.7)], { cost: 0, tags: ['보존'] }),
      Or([stk(K, 2), sh(0.9), draw(1)], { tags: ['보존'] }),
      Or([stk(K, 2), sh(1.0), pw('keepAp', [stk(K, 1), sh(0.4)], { limit: 1 })], { power: true, tags: [] }),
      Or([stk(K, 3), sh(1.2)], { tags: [] }),
    ]), null, ['보존']),
    // 둘째 — 약하면 설치지 마: 피해, 적이 부상이면 한 번 더, 처치하면 AP +1
    U(H, 4, '약하면 설치지 마', 1, '공격', [dmg(1.15), ifWounded, dmg(0.9), ifKill, ap(1)], [
      'A', 'B',
      Or([dmg(1.3), stk(K, 1), ifKill, ap(1)]),
      Or([dmg(1.3), srch(), ifWounded, dmg(1.0)]),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 귀찮아서 대충(0): 나른함 + 손의 자신의 공격 카드 비용 -1(AP 를 남길 틈)
    U(H, 5, '귀찮아서 대충', 0, '스킬', [stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })], [
      Or([stk(K, 2), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })]),
      Or([stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), draw(1)]),
      Or([stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), pw('keepAp', [stk(K, 1)], { when: { kind: 'ap' }, limit: 1 })], { power: true }),
      Or([stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), sh(0.6)]),
      Or([stk(K, 2), cs('비용', -1, { to: 'hand', n: 2, who: 'self', type: '공격' }), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('ap', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 코미(수영복) — 돈형 · 서포터 · 냉정. 매 턴 주스가 입고되고, 동료가 스킬을 낼 때마다 한 병씩 팔아 치운다(벌이). 재고가 넘치면 「코미 주스」(손에 카드 = 사기)
// 원작: 평타 「수제 주스 판매」(아군 회복 · 적 피해) · 어사이드 간이 가판대(HP 낮은 아군에게 특제 주스 — 회복 · 받는 피해↓) · 저학년 물장구 · 고학년 소금물 6번 · 블랙 기업 사장 · 코미 코인
// 전투에 골드 문법이 없어 판 골드는 건드리지 않는다 — 재고(입고 · 판매)가 벌이, 넘친 재고 · 특제 주스가 「사기」(주스 카드 만들기)
// 시동: u3 주스 한 병 투척(그대로) · 장치가 자기 카드를 만든다(전과 같음 — 장사 수완)
// ════════════════════════════════════════════════════════════════════
function komiSwim(j) {
  const H = '코미_수영복', K = '주스 재고', JUICE = '코미_수영복_juice';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '수상한 재료로 만든 코미 주스', carrier: 'self', cap: 4,
    rules: [
      { name: '입고', when: { on: 'fightStart' }, fx: [stk(K, 2)] },
      { name: '입고', when: { on: 'turnStart' }, fx: [stk(K, 1)] },
    ],
  };
  h.passives = [
    pas('판매 개시', 'play', [spendN(K, 1), heal(0.45)], { when: { type: '스킬', who: 'any' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: 2 }),
    pas('장사 수완', 'stackOver', [make(JUICE, 1)], { when: { id: K }, limit: 1 }),
  ];
  const juice = j.cards.find(c => c.id === JUICE);
  Object.assign(juice, { fx: [heal(0.4), draw(1)], blurb: '맛은 묻지 마세요. 효과는 확실합니다 — 값은 코미 코인으로' });
  setCards(j, [
    // 원작 자유 — 물놀이 좋은거 아니야?: 회복 + 사기
    U(H, 1, '물놀이 좋은거 아니야?', 1, '스킬', [heal(1.0), st('사기', 1)], [
      'A', 'B',
      Or([heal(0.8), st('사기', 1), stk(K, 1)]),
      Or([heal(0.6), st('사기', 1), pw('turnStart', [heal(0.3), stk(K, 1)])], { power: true }),
      Or([heal(0.8), st('사기', 2), disc(1)]),
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 쓰기 — 특제 코미 주스: 재고 1개당 회복, 재고를 다 털어 주스 한 병(사기) + 피해 감소
    U(H, 2, '특제 코미 주스', 1, '스킬', [per(K), heal(0.35), spendAll(K), make(JUICE, 1)], [
      'A', 'B',
      Or([per(K), sh(0.55), spendAll(K), st('피해 감소', 2)]),
      Or([per(K), heal(0.3), make(JUICE, 1), srch()]),
      Or([per(K), heal(0.3), make(JUICE, 1), pw('spend', [heal(0.3)], { when: { id: K }, limit: 2 })], { power: true }),
    ], bl('heal', 'cost', [make(JUICE, 1)])),
    // 시동 — 주스 한 병 투척: 피해 + 회복 + 재고
    U(H, 3, '주스 한 병 투척', 1, '공격', [dmg(0.9), heal(0.6), stk(K, 1)], named([
      Or([dmg(1.1), heal(0.7), stk(K, 1)]),
      Or([dmg(0.7), heal(0.45), stk(K, 1)], { cost: 0 }),
      Or([dmg(0.8), heal(0.5), make(JUICE, 1)]),
      Or([dmg(0.9), heal(0.45), pw('spend', [dmg(0.3, ER)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([dmg(0.9), heal(0.5), srch()]),
    ]), null),
    // 갈래 부품 — 조기 매진!(강화): 재고 둘 + 재고가 팔릴 때마다 드로우(턴 1)
    U(H, 4, '조기 매진!', 1, '강화', [stk(K, 2), pw('spend', [draw(1)], { when: { id: K }, limit: 1 })], named([
      Or([stk(K, 2), heal(0.5), pw('spend', [draw(1)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 1), pw('spend', [draw(1)], { when: { id: K }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 2), heal(0.8), pw('spend', [draw(1), heal(0.3)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 2), srch(), pw('spend', [draw(1)], { when: { id: K }, limit: 1 })]),
      Or([stk(K, 3), pw('spend', [draw(1)], { when: { id: K }, limit: 1 }), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 둘째 — 물총 세례: 무작위 3회 + 재고 + 회복
    U(H, 5, '물총 세례', 1, '공격', [dmg(0.35, ER, { hits: 3 }), stk(K, 1), heal(0.4)], [
      'A', 'B',
      Or([dmg(0.4, ER, { hits: 3 }), stk(K, 1), make(JUICE, 1)]),
      Or([dmg(0.32, ER, { hits: 3 }), stk(K, 1), pw('turnStart', [dmg(0.25, ER)])], { power: true }),
      'Hd',
    ], bl('power', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 9. 티그(영웅) — 표적형 · 딜러 · 광기(용기) · 엘다인. 한 놈에게 칼끝을 겨누고(찍기) 두 번 벤다 — 두 번 벤 놈이 약해지면 단칼에 결착, 쓰러지면 다음 놈에게
// 원작: 고학년 맹호류 결착(2회 이상 맞은 적 HP 10% 이하 즉시 처치 · 보스면 HP 비례) · 저학년 사슴류 베기(범위 2회) · 어사이드 영웅의 검 · 험난한 영웅의 길(영웅심 — 기절 · 범위 · 회복)
// 시범 티그(박자형 — 제 공격만 잇달아 셋째 칼)와 가름: 영웅은 순서가 아니라 「누구를 두 번 베었나」를 센다. 영웅심은 다 차면 다음 카드 강화(옛 저절로 터짐을 바꿈)
// 엘다인 한 단계: 겨눈 적이 쓰러지면 HP 가장 낮은 적에게 칼끝 + 영웅심
// 시동: u2 훈련의 성과(그대로)
// ════════════════════════════════════════════════════════════════════
function tigHero(j) {
  const H = '티그_영웅', K = '영웅심', T = '겨눈 칼끝';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '영웅의 길에서 솟는 용기', carrier: 'self', cap: 3, onMax: { empower: 'next', ratio: 0.4, consume: true } };
  h.keywords = [{
    name: T, desc: '두 번 베어 끝낼 한 놈', carrier: 'enemy', cap: 2, hunt: true, per: [{ stat: 'taken', v: 0.08, from: 'owner' }],
    rules: [{ name: '두 번 베기', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [stk(T, 1, MARK)] }],
  }];
  h.passives = [
    pas('무용담', 'huntDown', [stk(K, 1), stk(T, 1, LOW)]),
    pas('무용담', 'break', [stk(K, 1)], { when: { mine: true }, limit: 1 }),
    pas('그 스승에 그 제자', 'ult', [stk(K, 1)]),
  ];
  setCards(j, [
    // 원작 자유 — 사슴류 베기: 적 전체 2회 + HP 가장 낮은 적에게 칼끝 + 영웅심
    U(H, 1, '사슴류 베기', 1, '공격', [dmg(0.38, EA, { hits: 2 }), stk(T, 1, LOW), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.7, E1, { hits: 2 }), stk(T, 1), stk(K, 1)]),
      Or([dmg(0.35, EA, { hits: 2 }), stk(T, 1, LOW), srch()]),
      ['D', 'huntDown', [dmg(0.5, EA)], { limit: 1 }],
    ], bl('power', 'draw', [stk(K, 1)])),
    // 시동 — 훈련의 성과: 고른 적에게 칼끝(찍기) + 피해 + 강인도
    U(H, 2, '훈련의 성과', 1, '공격', [dmg(0.8), stk(T, 1), tough(1)], named([
      Or([dmg(1.05), stk(T, 1), tough(1)]),
      Or([dmg(0.6), stk(T, 1), tough(1)], { cost: 0 }),
      Or([dmg(0.8), stk(T, 2), stk(K, 1)]),
      Or([dmg(0.8), stk(T, 1), pw('huntDown', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([dmg(0.85), stk(T, 1), srch()]),
    ]), null),
    // 쓰기 — 영웅의 검(2, 약점 공격): 피해 + 영웅심 1개당, 영웅심 전부 소모
    U(H, 3, '영웅의 검', 2, '공격', [dmg(1.8), per(K), dmg(0.4), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.3, EA), per(K), dmg(0.3, EA), spendAll(K)]),
      Or([dmg(1.7), per(K), dmg(0.38), ifKill, stk(K, 2)]),
      Or([dmg(1.6), per(K), dmg(0.35), pw('huntDown', [stk(K, 1)])], { power: true }),
    ], bl('power', 'ap', [stk(K, 1)]), ['약점 공격']),
    // 갈래 부품 — 영웅의 길: 피해. 칼끝이 둘(두 번 벤 놈)이면 결착 — 한 번 더 크게, HP 30% 이하면 더
    U(H, 4, '영웅의 길', 1, '공격', [dmg(0.75), ifStack(T, 2), dmg(0.8), ifFoe('hp', { pct: 0.3 }), dmg(0.8)], [
      'A', 'B',
      Or([dmg(0.7), srch(), ifStack(T, 2), dmg(0.8)]),
      Or([dmg(0.65), pw('huntDown', [gauge(10), stk(K, 1)]), ifStack(T, 2), dmg(0.75)], { power: true }),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 영웅의 다짐: 영웅심 + 실드 + 드로우
    U(H, 5, '영웅의 다짐', 1, '스킬', [stk(K, 1), sh(1.0), draw(1)], [
      'A', 'B',
      Or([stk(K, 2), sh(1.1), stk(T, 1)]),
      Or([stk(K, 1), sh(0.8), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 3), sh(1.6), disc(1)]),
    ], bl('guard', 'cost', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '수인/에피카': 0.75, '수인/티그_영웅': 0.75, '수인/스패럿': 0.8, '수인/우로스': 0.85, '수인/베니_베니': 0.8, '수인/유미미': 1.4 };
// ── 돌리기 ──
const T = fn => j => { fn(j); keepTags(j); };
const JOBS = [
  ['수인/베니', T(benny)], ['수인/베니_베니', T(bennyBenny)], ['수인/슈로', T(shuro)], ['수인/스패럿', T(sparrot)], ['수인/에피카', T(epica)],
  ['수인/우로스', T(uros)], ['수인/유미미', T(yumimi)], ['수인/코미_수영복', T(komiSwim)], ['수인/티그_영웅', T(tigHero)],
];
L.run18(JOBS, TUNE, new URL('./boost_수인B_18.json', import.meta.url));
