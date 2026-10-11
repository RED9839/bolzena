// 묶음 0 — 시범 7명(10-09 사용자: 기본 카드까지 원작 근거로 처음부터). node _gen/deep/b0.mjs [사도 일부]
// 고유 id: u1 시동 · u2 연료 · u3 결정타 · u4 원작 자유(원작 저학년 카드는 cardart_low 의 id 에 둔다 — 그림 짝).
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, FAR, ALLIES, OTHERS, dmg, ddef, hits, sh, heal, st, stk, spendAll, spendN, per, perTag, draw, make, gauge, disc, ifStack, notStack, ifKill, ifHand, ifFoe,
  empower, form, formEnd, tough, later, dmod, extra, cost, power, rule, pw, pas, O, B, U, S, T, hero } = L;

// ── 디아나(왕년) — 두 얼굴형 · 딜러(엘다인) ──
// 엔진: 도끼로 땅을 갈라(「갈라진 땅」 — 적이 받는 피해↑, 동료도 덕 봄) → 「도끼 부수기」가 갈라진 만큼 한 방 치고 맨주먹 → 주먹질로 연격 → 「찢는다」(맨주먹판 「도끼 되찾기」)가 연격을 광역으로 쏟고 도끼로
function diana(j) {
  // 팀: 「갈라진 땅」은 적이 받는 피해 +5%/개 — 동료의 공격도 덕을 본다(주는 고리)
  // 원작: 찢는다(균열 · 백수공권 해제) · 부순다(SP 비례 · 백수공권) · 맨주먹 평타(연격 · HP 회복) · 강화 공격(바위 + 기절) · 입버릇 「귀찮아 · 단순하게」
  const H = '디아나_왕년', G = '갈라진 땅', C = '연격', F = '디아나_왕년_맨주먹';
  const basics = [
    S(`${H}_s1`, '도끼질', 1, '공격', [dmg(0.9, E1), stk(G, 1, E1)]),
    S(`${H}_s2`, '도끼 내려찍기', 2, '공격', [dmg(2.0, E1), stk(G, 1, E1)]),
    S(`${H}_s3`, '털가죽 버티기', 1, '스킬', [sh(1.4), stk(C, 1)]),
  ];
  const tokens = [
    T(`${H}_f1`, '맨주먹 연타', H, '공격', [dmg(0.5, E1, { hits: 2 }), stk(C, 1)], { cost: 1, blurb: '맨주먹의 기본 공격 — 두 번 치고 연격을 잇습니다' }),
    T(`${H}_f2`, '도끼 되찾기', H, '공격', [per(C, { n: 1 }), dmg(0.45, EA), spendAll(C), formEnd], { cost: 1, blurb: '맨주먹의 「찢기」 — 쌓은 주먹질을 땅에 쏟고 도끼를 다시 쥡니다' }),
  ];
  const BG = [stk(G, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '찢기', 1, '공격', [dmg(0.55, EA), stk(G, 1, EA)], [               // 시동 — 원작 저학년
      O([dmg(0.75, EA), stk(G, 1, EA)]),                                           // 수치
      O([dmg(1.3, E1), stk(G, 3, E1)]),                                            // 한 곳만 깊게
      O([dmg(0.45, EA), stk(G, 1, EA), pw('turnStart', [stk(G, 1, EA)])], { power: true }), // 매 턴 갈라짐
      O([dmg(0.5, EA), stk(G, 1, EA), stk(C, 2)]),                                 // 맨주먹 준비
      O([stk(G, 1, EA), draw(1)], { cost: 0 }),                                    // 얕게 가르고 다음 수
    ], [B('power'), B('draw'), B(BG)]),
    U(`${H}_u2`, '귀찮아, 단순하게', 0, '스킬', [disc(1), stk(C, 2), draw(1)], [      // 연료 — 원작 입버릇
      O([disc(1), stk(C, 3), draw(1)]),                                            // 수치
      O([disc('all'), { k: 'perDiscarded' }, sh(0.5), stk(C, 2)]),                 // 손 다 털기(버린 만큼 실드)
      O([stk(G, 2, E1), draw(1)]),                                                 // 도끼 쪽(갈라진 땅)
      O([stk(C, 1), pw('discard', [stk(C, 1)])], { power: true }),                 // 버릴 때마다 연격
      O([disc(1), draw(2), st('피해 감소', 1)]),                                   // 나른하게(버티기)
    ], [B('draw'), B('ap'), B([stk(C, 1)])]),
    U(`${H}_u3`, '도끼 부수기', 1, '공격', [per(G), dmg(0.5, E1), spendAll(G), form(F)], [ // 결정타 — 원작 고학년 「부순다」
      O([per(G), dmg(0.68, E1), spendAll(G), form(F)]),                            // 수치
      O([per(G, { each: true }), dmg(0.35, EA), spendAll(G), form(F)]),           // 광역(적마다 제 겹)
      O([per(G), dmg(0.45, E1), tough(1), form(F)]),                               // 땅은 남긴다 · 격파
      O([per(G), dmg(0.4, E1), form(F), pw('turnStart', [stk(C, 1)])], { power: true }), // 매 턴 연격
      O([form(F), stk(C, 3)], { cost: 0 }),                                        // 바로 맨주먹
    ], [B('power'), B('cost'), B(BG)]),
    U(`${H}_u4`, '바위 던지기', 1, '공격', [dmg(0.9, E1), st('기절', 1, E1)], [      // 원작 자유 — 강화 공격(바위 + 기절)
      O([dmg(1.2, E1), st('기절', 1, E1)]),                                        // 수치
      O([dmg(1.2, EA), st('기절', 1, TOP), stk(G, 2, EA)], { cost: 2 }),                          // 크게 던지기
      O([dmg(0.8, E1), st('기절', 1, E1), stk(G, 2, E1)]),                         // 땅을 가르며
      O([dmg(0.5, E1), pw('hurt', [dmg(0.45, ER)], { when: { guarded: true } })], { power: true }), // 맞으면 던진다
      O([per(C, { n: 1 }), dmg(0.3, E1), st('기절', 1, E1)]),                      // 맨주먹 바위(연격 비례)
    ], [B('power'), B({ tags: ['보존'] }), B(BG)], ['소멸']),
  ];
  hero(j, {
    keyword: { name: G, desc: '도끼가 갈라 놓은 적의 발밑', carrier: 'enemy', cap: 5, per: [{ stat: 'taken', v: 0.05 }] },
    keywords: [{ name: C, desc: '맨주먹으로 잇는 주먹질', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.06 }] }],
    passives: [
      pas('단순하게', 'play', [ifHand(1), stk(C, 1)], { when: { type: '공격' } }),
      pas('휴식이 필요해', 'lowHp', [stk(C, 4), st('피해 감소', 2)], { when: { pct: 0.3 } }),
    ],
    forms: [{ id: F, name: '맨주먹', desc: '도끼를 부순 한창때의 맨주먹 — 잃은 HP를 조금씩 되찾음', turns: 4,
      cards: { [`${H}_s1`]: `${H}_f1`, [`${H}_u1`]: `${H}_f2` },
      passives: [{ name: '되찾는 숨', when: { on: 'turnEnd' }, fx: [heal(0.15)] }],
      off: [stk(C, 1)], skin: null, anim: 'Idle_Change' }],
    ult: { name: '부수기', cost: 250, fx: [dmg(1.6, EA), st('피해 감소', 2), form(F)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
}

// ── 에르핀 — 손 만들기형 · 딜러 ──
// 엔진: 몰래 케이크를 만들어 — 먹으면 배부름 · 고학년 게이지(원작 「케이크를 먹어 SP 회복」), 안 먹으면 「와구와구 발사!!」의 탄알. 배가 비면 예민해 세고, 배부름 다섯이면 산을 뽑아 던진다
function erpin(j) {
  // 팀: 「한 입 케이크」(동료 몫 — 그 동료의 다음 카드 +30%) · 「순수 케이크 공격!!!」(원작 어사이드 아군 피해↑)
  // 원작: 저학년 마력탄 폭주(4발) · 강화 평타 몰래 케이크(SP 회복 → 게이지) · 「배고프면 예민」 · 괴력 · 어사이드 순수 케이크 공격
  const H = '에르핀', K = '배부름', CK = '에르핀_cake', BT = '에르핀_bite';
  const basics = [
    S(`${H}_s1`, '마력탄', 1, '공격', [dmg(1.0, E1), notStack(K), dmg(0.3, E1)]),
    S(`${H}_s2`, '특대 마력탄', 2, '공격', [dmg(2.1, E1), stk(K, 1)]),
    S(`${H}_s3`, '도끼도 튕기는 몸', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const tokens = [
    T(CK, '케이크', H, '스킬', [stk(K, 1), gauge(10)], { tags: ['소멸'], blurb: '친구 몰래 꺼내 먹는 케이크 — 먹으면 배가 부르고 힘이 차오릅니다' }),
    T(BT, '한 입 케이크', H, '스킬', [heal(0.3), empower(0.3)], { tags: ['소멸'], blurb: '여왕이 나눠 준 한 입 — 받은 동료의 다음 카드가 힘을 냅니다' }),
  ];
  const BC = [make(CK, 1)];
  const uniques = [
    U(`${H}_u2`, '친구 몰래 케이크', 1, '스킬', [make(CK, 2), draw(1)], [               // 시동 — 원작 강화 평타
      O([make(CK, 3), draw(1)]),                                                    // 수치
      O([make(CK, 1), draw(1), pw('turnStart', [make(CK, 1)])], { power: true }),  // 매 턴 케이크
      O([make(CK, 1), make(BT, 1, { owner: 'other' }), draw(1)]),                  // 하나는 친구 몫
      O([stk(K, 2), gauge(20)], { cost: 0 }),                                       // 그 자리에서 먹기
      O([make(CK, 4, { to: 'draw' }), draw(1)]),                                   // 뽑을 더미에 숨기기
    ], null),
    U(`${H}_u1`, '마력탄 폭주', 1, '공격', [hits(4, 0.38), make(CK, 1)], [   // 연료 — 원작 저학년(무작위 4발)
      O([hits(4, 0.48), make(CK, 1)]),                                              // 수치
      O([perTag(CK), dmg(0.7, ER), { k: 'exileFrom', from: 'hand', all: true, tag: CK }], { cost: 0 }), // 케이크가 탄알(0코)
      O([perTag(CK), dmg(0.7, EA), { k: 'exileFrom', from: 'hand', all: true, tag: CK }, stk(K, 1)]), // 광역 폭주
      O([perTag(CK), dmg(0.7, E1), { k: 'exileFrom', from: 'hand', all: true, tag: CK }, stk(K, 2)]), // 쏘며 먹기
      O([hits(2, 0.5), make(CK, 1), pw('make', [dmg(0.45, ER)])], { power: true }), // 만들 때마다 한 발
    ], [B('power'), B('draw'), B(BC)]),
    U(`${H}_u3`, '산 뽑아 던지기', 2, '공격', [per(K, { n: 1 }), dmg(0.7, E1), spendAll(K), st('피해 감소', 1)], [ // 결정타 — 괴력
      O([per(K, { n: 1 }), dmg(0.9, E1), spendAll(K), st('피해 감소', 1)]),        // 수치
      O([per(K, { n: 1 }), dmg(0.45, EA), spendAll(K)]),                            // 산사태(광역)
      O([per(K, { n: 1 }), dmg(0.6, E1), tough(2)]),                                // 배는 그대로 · 격파
      O([per(K, { n: 1 }), dmg(0.55, E1), spendAll(K), gauge(30)], { cost: 1 }),   // 1코 · 게이지
      O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K), make(BT, 2, { owner: 'other' })]), // 남은 건 친구들 몫
    ], [B('power'), B('weakSpot'), B(BC)], ['분쇄']),
    U(`${H}_u4`, '순수 케이크 공격!!!', 1, '공격', [dmg(0.8, E1), make(BT, 1, { owner: 'other' })], [ // 원작 자유 — 어사이드(아군 피해↑) — 케이크를 던져 친구 몫도
      O([dmg(1.05, E1), make(BT, 1, { owner: 'other' })]),                         // 수치
      O([dmg(1.0, E1), dmod(0.15, OTHERS)]),                                       // 아군 피해 +15%
      O([dmg(0.7, E1), make(BT, 1, { owner: 'other' }), pw('turnStart', [empower(0.2, 'any')])], { power: true }), // 매 턴 파티 다음 카드
      O([dmg(0.6, EA), make(BT, 1, { owner: 'other' })]),                          // 케이크 폭격(광역)
      O([dmg(0.6, E1), make(BT, 1, { owner: 'other' }), make(CK, 1)], { cost: 0 }), // 하나는 내 몫
    ], [B('power'), B('draw'), B(BC)], ['연계']),
  ];
  hero(j, {
    keyword: { name: K, desc: '케이크로 채운 여왕의 배', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.04 }] },
    passives: [
      pas('달달한 게 최고야!', 'play', [make(CK, 1)], { when: { type: '공격' } }),
      pas('배고프면 예민', 'always', [dmod(0.2, 'self')], { conds: [{ c: 'stack', id: K, n: 1, not: true }] }),
    ],
    ult: { name: '돌겨어어어!!! 억⋯?', cost: 250, fx: [dmg(2.0, EA), st('피해 감소', 3), st('약화', 2, EA)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '간식 꺼내기', when: { on: 'fightStart' }, fx: [make(CK, 2)] }];
}

// ── 버터 — 쌓고 고르기 · 딜러 ──
// 엔진: 파티가 맞을 때마다 옐로카드(원작 애착 「분노」) → 넷이면 「레드카드」가 손에 — 내면 2턴 동안 확정 치명(원작 총 · 개껌) · 그 전에 에이프런 매그넘으로 털어 한 방. 치명이 터지면 동료가 신난다(만반잘부)
function butter(j) {
  // 팀: 치명타를 내면 파티의 다음 카드 +20%(만반잘부 — 원작 「아군 치명↑」) · 동료 스킬을 참기(시동 신탁)
  // 원작: 애착 「아군이 맞으면 분노 → 총 · 확정 치명」 · 저학년 튕기는 탄환 · 강화 평타 개껌(확정 치명) · 「참는 한계 · 방전」
  const H = '버터', K = '옐로카드', F = '버터_퇴장', RED = '버터_red';
  const basics = [
    S(`${H}_s1`, '짱돌 던지기', 1, '공격', [dmg(0.95, E1), stk(K, 1)]),
    S(`${H}_s2`, '왕짱돌 던지기', 2, '공격', [dmg(1.35, EA)]),
    S(`${H}_s3`, '꾹 참기', 1, '스킬', [sh(1.4), stk(K, 1)]),
  ];
  const tokens = [
    T(RED, '레드카드', H, '스킬', [form(F), draw(1)], { tags: ['소멸'], blurb: '참을 만큼 참았습니다 — 꺼내 들면 2턴 동안 웃음기 없는 버터가 됩니다' }),
    T(`${H}_f1`, '앞치마 총', H, '공격', [dmg(0.6, E1, { hits: 2 }), st('취약', 1, E1)], { cost: 1, blurb: '웃음기 없는 버터의 짱돌 — 개껌이 정확히 꽂힙니다' }),
  ];
  const BY = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '첫 칭구', 1, '강화', [stk(K, 1), pw('hurt', [stk(K, 1), dmg(0.25, ER)], { when: { guarded: true } })], [ // 시동 — 원작 애착(맞으면 분노 · 새총)
      O([stk(K, 2), pw('hurt', [stk(K, 1), dmg(0.32, ER)], { when: { guarded: true } })]),                 // 수치
      O([sh(0.6), pw('hurt', [sh(0.3), stk(K, 1)], { when: { guarded: true } })]),                         // 같이 맞아 주기(실드)
      O([stk(K, 1), sh(0.6), pw('play', [stk(K, 1)], { when: { type: '스킬', who: 'other' } })]),       // 부탁은 거절 못 해(동료 스킬)
      O([pw('turnStart', [stk(K, 1), dmg(0.2, ER)])], { cost: 0 }),                                       // 매일 새총 연습
      O([stk(K, 2), pw('crit', [stk(K, 1), draw(1)])]),                                                   // 급소를 맞히면 참기 · 드로우
    ], null, ['개전']),
    U(`${H}_u1`, '버터 플라이!', 1, '공격', [hits(4, 0.3), stk(K, 1)], [         // 연료 — 원작 저학년(튕기는 탄환)
      O([hits(4, 0.4), stk(K, 1)]),                                                // 수치
      O([per(K, { n: 1 }), dmg(0.6, ER)]),                                         // 학년마다 타수(옐로카드 수만큼)
      O([dmg(0.32, E1, { hits: 4 }), stk(K, 1)]),                                  // 한 놈만
      O([hits(2, 0.35), pw('hurt', [dmg(0.3, ER)], { when: { guarded: true } })], { power: true }), // 맞으면 새총
      O([per(K, { n: 1 }), dmg(0.3, ER), st('취약', 1, E1)], { cost: 0 }),          // 경고만 날리기
    ], [B('power'), B('weakSpot'), B(BY)]),
    U(`${H}_u3`, '에이프런 매그넘', 2, '공격', [dmg(0.7, EA), per(K), dmg(0.25, EA), spendAll(K)], [ // 결정타 — 원작 애착 총(범위)
      O([dmg(0.9, EA), per(K), dmg(0.32, EA), spendAll(K)]),                        // 수치
      O([per(K, { n: 1 }), dmg(0.8, E1), spendAll(K), st('기절', 1, E1)]),          // 한 놈 퇴장
      O([dmg(0.6, EA), per(K), dmg(0.22, EA)]),                                     // 겨누기만(옐로카드는 둔다)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), make(RED, 1)], { cost: 1 }),  // 레드카드 바로 꺼내기
      O([dmg(0.7, EA), stk(K, 1), pw('stackReach', [dmg(0.3, EA)], { when: { id: K, n: 4 } })], { power: true }), // 넷 찰 때마다 연사
    ], [B('power'), B('ap'), B(BY)]),
    U(`${H}_u4`, '개껌 발사', 1, '공격', [{ k: 'critMod', v: 1, target: 'self' }, dmg(0.9, E1), stk(K, 1)], [ // 원작 자유 — 강화 평타(확정 치명)
      O([{ k: 'critMod', v: 1, target: 'self' }, dmg(1.15, E1), stk(K, 1)]),        // 수치
      O([{ k: 'critMod', v: 1, target: 'self' }, dmg(0.45, ER, { hits: 3 })]),     // 개껌 세 개
      O([{ k: 'critMod', v: 1, target: 'self' }, dmg(0.85, E1), st('취약', 2, E1)]), // 경고 개껌(취약)
      O([dmg(1.0, E1), stk(K, 2), pw('play', [{ k: 'critMod', v: 0.3, target: 'self' }], { when: { type: '공격' } })], { power: true }), // 새총 감각(공격마다 치명↑)
      O([{ k: 'critMod', v: 1, target: 'self' }, dmg(0.9, E1), make(RED, 1)], { cost: 2 }), // 레드카드 꺼내기
    ], [B('power'), B('weakSpot'), B(BY)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '꾹 참는 부당한 대우', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.06 }], onMax: { make: RED, consume: true } },
    passives: [
      pas('참다 참다', 'hurt', [stk(K, 1)], { when: { guarded: true } }),
      pas('만반잘부', 'crit', [empower(0.2, 'any')]),
    ],
    forms: [{ id: F, name: '웃음기 없는 버터', desc: '참을 만큼 참은 버터 — 쏘는 족족 급소', turns: 2,
      mods: { crit: 1, dealt: 0.15 }, cards: { [`${H}_s1`]: `${H}_f1` }, off: [draw(2)], skin: null, anim: null }],
    ult: { name: '새쭁! 쓔튜라이크!', cost: 250, fx: [dmg(2.2, EA), st('기절', 1), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
}

// ── 시온 더 다크불릿 — 표적형 · 딜러(엘다인) ──
// 엔진: 가장 먼 적만 노리는 저격 — 공격마다 마탄을 재우고(다섯이면 다음 카드 +50% · 아군 피해 +10%), 「마.탄.의.사.수」가 장탄 수만큼 연사. 공격 넷째마다 광역 + 눈가림(약화)
function sion(j) {
  // 팀: 다른 아군이 공격 카드를 낼 때마다 마탄 1(어.둠.의.구.원.자★ — 원작 「후열 아군」 · 엘다인 「화합」)
  // 원작: 저학년 마탄을 다 써 장탄 수만큼 연사 · 5발이면 피해↑ · 가장 먼 적 저격 · 고학년 마탄 3 · 편의점 알바 · 짱돌 마탄
  const H = '시온더다크불릿', K = '마탄', TK = `${H}_t1`;
  const basics = [
    S(`${H}_s1`, '저격', 1, '공격', [dmg(1.15, FAR)]),
    S(`${H}_s2`, '그림자 속으로', 1, '스킬', [sh(1.8), stk(K, 1)]),
    S(`${H}_s3`, '관통 저격', 2, '공격', [dmg(2.3, FAR), stk(K, 1)]),
  ];
  const tokens = [T(TK, '진혼의 마탄', H, '공격', [dmg(0.6, E1), stk(K, 1)], { tags: ['증발'], blurb: '1+1 로 받은 덤 한 발입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '마탄 장전', 0, '스킬', [stk(K, 2), draw(1)], [                       // 시동 — 원작 고학년 「마탄 3」
      O([stk(K, 3), draw(1)]),                                                     // 수치
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])], { power: true }),               // 매 턴 장전
      O([stk(K, 1), make(TK, 1)]),                                                 // 덤 한 발
      O([stk(K, 2), st('약화', 1, FAR)]),                                          // 눈가림
      O([stk(K, 5), L.disc(1)]),                                  // 한 번에 꽉
    ], null),
    U(`${H}_u3`, '짱돌 마탄', 1, '공격', [dmg(0.75, E1), stk(K, 1), { k: 'strip', target: E1 }], [ // 연료 — 짱돌에 마법을 건 첫 마탄
      O([dmg(1.0, E1), stk(K, 1), { k: 'strip', target: E1 }]),                     // 수치
      O([dmg(0.55, EA), stk(K, 2)]),                                               // 흩뿌리기
      O([dmg(0.7, E1), stk(K, 1), tough(1)]),                                       // 강인도
      O([dmg(0.6, E1), pw('turnStart', [stk(K, 1)])], { power: true }),            // 매 턴 장전
      O([dmg(1.6, FAR), stk(K, 2)], { cost: 2 }),                                  // 관통 마탄
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u1`, '마.탄.의.사.수', 1, '공격', [per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)], [ // 결정타 — 원작 저학년
      O([per(K, { n: 1 }), dmg(0.52, E1), spendAll(K)]),                           // 수치
      O([per(K, { n: 1 }), dmg(0.4, ER), spendAll(K)]),                            // 흩뿌리기
      O([per(K, { n: 1 }), dmg(0.38, E1), spendN(K, 3)]),                          // 두 발 남기기
      O([per(K, { n: 1 }), dmg(0.35, E1), spendAll(K), ifKill, stk(K, 3)]),        // 처치하면 재장전
      O([per(K, { n: 1 }), dmg(0.6, FAR), spendAll(K), st('취약', 2, FAR)], { cost: 2 }), // 종말 직전
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '원 플러스 원', 0, '스킬', [make(TK, 1), stk(K, 1)], [               // 원작 자유 — 편의점 야간 알바
      O([make(TK, 2), stk(K, 1)]),                                                  // 수치
      O([make(TK, 1), make(TK, 1, { owner: 'other' })]),                          // 동생 몫
      O([make(TK, 2), pw('play', [stk(K, 1), dmg(0.2, ER)], { when: { type: '공격', maxCost: 0 } })], { power: true }), // 0코 공격마다 장전
      O([make(TK, 3), { k: 'nextAp', v: -1 }]),                                    // 폐기 처분
      O([heal(0.6), stk(K, 2)]),                                                   // 유통기한 도시락(회복)
    ], [B('draw'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '어둠의 힘으로 재운 마탄', carrier: 'self', cap: 5, per: [{ stat: 'atk', v: 0.04 }], onMax: { empower: 'next', ratio: 0.5 } },
    passives: [
      pas('큭큭, 봉인 해제', 'play', [stk(K, 1)], { when: { type: '공격' } }),
      pas('어.둠.의.구.원.자★', 'play', [stk(K, 1)], { when: { type: '공격', who: 'other' } }),
    ],
    ult: { name: '종.말.의.폭.발★', cost: 150, fx: [dmg(1.2, EA), stk(K, 3)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s3`, `${H}_s2`],
    basics, tokens, uniques,
  });
}

// ── 아멜리아 — 회복형 · 딜러 ──
// 엔진: 신호탄을 쏘아 두면 다음 턴 위성 폭격(충격) → 파티 HP 를 채울 때마다 결재 서류 → 「감봉 통보」가 서류를 한 번에 결재(충격 걸린 적은 약화). 「그냥 승인」 으로 동료 카드를 싸게
function amelia(j) {
  // 팀: 「그냥 승인」 — 손의 다른 아군 카드 비용 -1 · 동료의 회복도 서류가 된다(주는 · 받는 고리)
  // 원작: 저학년 신호탄 → 레이저 폭격 · 감전 · 애착 Mk.2 4회 · 고학년 감전된 적 기절 · 감봉 남발 · 「그냥 승인」 · 감시 카메라 32대
  const H = '아멜리아', K = '결재 서류';
  const basics = [
    S(`${H}_s1`, '레이저 포인트 샷', 1, '공격', [ddef(0.4, EA)]),
    S(`${H}_s2`, '응급 키트', 1, '스킬', [heal(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '새틀라이트 신호탄', 1, '공격', [dmg(0.45, EA), st('충격', 1, EA), later(1, [dmg(0.7, EA)])], [ // 시동 — 원작 저학년(신호탄 → 레이저 폭격 · 감전)
      O([dmg(0.6, EA), st('충격', 1, EA), later(1, [dmg(0.9, EA)])]),             // 수치
      O([dmg(1.0, E1), st('충격', 2, E1), later(1, [dmg(1.2, E1)])]),             // 정밀 좌표(한 놈)
      O([dmg(0.4, EA), pw('turnStart', [dmg(0.25, EA), st('충격', 1, EA)])], { power: true }), // 위성 상주
      O([dmg(0.5, EA), st('충격', 1, EA), stk(K, 2)]),                            // 결재 올리며 쏘기
      O([later(1, [dmg(1.8, EA), st('충격', 2, EA)]), stk(K, 1)], { cost: 2 }),   // 큰 폭격 예약
    ], null),
    U(`${H}_u2`, '그냥 승인', 0, '스킬', [cost(-1, { who: 'other' }), stk(K, 1)], [   // 연료 — 「그냥 관리자」 · 「그냥 승인」(동료 카드를 싸게)
      O([cost(-1, { who: 'other' }), stk(K, 2)]),                                  // 수치
      O([cost(-1, { who: 'other', n: 2 }), draw(2), L.ap(1)], { cost: 1 }),        // 일괄 승인
      O([cost(-1, { who: 'other' }), heal(0.6), stk(K, 1)]),                       // 승인하며 챙기기
      O([cost(-1, { who: 'other' }), pw('play', [st('충격', 1, E1)], { when: { type: '공격', who: 'other' } })], { power: true }), // 감시카메라 32대
      O([cost(-1, { who: 'other' }), draw(1)], { tags: ['신속'] }),                // 즉결
    ], [B('draw'), B('cost'), B(BK)]),
    U(`${H}_u3`, '감봉 통보', 1, '공격', [per(K, { n: 1 }), dmg(0.5, E1), spendAll(K), ifFoe('충격'), st('약화', 2, E1)], [ // 결정타 — 감봉 남발
      O([per(K, { n: 1 }), dmg(0.65, E1), spendAll(K), ifFoe('충격'), st('약화', 2, E1)]), // 수치
      O([per(K, { n: 1, each: true }), dmg(0.3, EA), spendAll(K)]),               // 전 직원 감봉
      O([per(K, { n: 1 }), dmg(0.45, E1), ifFoe('충격'), st('기절', 1, E1)]),     // 경고만(서류는 둔다) · 감전 기절
      O([per(K, { n: 1 }), sh(0.45), spendAll(K)], { cost: 0 }),                  // 결재 반려(서류로 막기)
      O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K), pw('spend', [draw(1)], { when: { id: K } })], { power: true }), // 결재마다 드로우
    ], [B('power'), B('ap'), B(BK)]),
    U(`${H}_u4`, '새틀라이트 오버히트', 2, '공격', [dmg(0.25, EA, { hits: 4 }), stk(K, 1)], [ // 원작 자유 — 애착 아티팩트 저학년(Mk.2 4회 폭격)
      O([dmg(0.32, EA, { hits: 4 }), stk(K, 1)]),                                 // 수치
      O([dmg(0.4, E1, { hits: 4 }), st('충격', 2, E1)]),                           // 한 줄 집중
      O([dmg(0.2, EA, { hits: 4 }), heal(0.8)]),                                   // 과열 냉각(회복)
      O([dmg(0.15, EA, { hits: 4 }), pw('spend', [dmg(0.3, EA)], { when: { id: K } })], { power: true }), // 결재하면 폭격
      O([dmg(0.2, EA, { hits: 4 }), stk(K, 1)], { cost: 1 }),                      // 1코 과열
    ], [B('power'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '넘치는 일손으로 올라오는 서류', carrier: 'self', cap: 4, onMax: { empower: 'next', ratio: 0.6, consume: true } },
    passives: [
      pas('서류 접수', 'overheal', [stk(K, 1)], { when: { pct: 0.7, who: 'any' } }),
      pas('초고속 썬더 레이저', 'play', [st('충격', 1, E1), extra(0.35)], { when: { type: '공격' } }),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 벨라 — 버티형 · 탱커(엘다인) ──
// 엔진: 막아 낸 만큼 존속(불꽃)을 모아 → 「불꽃 나눠 주기」로 파티를 치유하며 동료 다음 카드에 건넴 → 영속(끈기)이 남은 동안 파티 피해 +15%, 끈기로 버티면 불꽃이 다시 타오른다
function bella(j) {
  // 팀: 「존속」 1개당 아군 주는 피해 +2% · 불꽃 나눠 주기(아군 이번 턴 피해 +15%) · 끈기가 있는 동안 파티 +15%
  // 원작: 저학년 도발 · 존속(아군 버프) · 영속(한 번 버팀) · 고학년 불꽃 빼앗아 나눠 회복 · 강화 공격(4회 피격) · 어사이드 「우린 이미 완전한 존재들」
  const H = '벨라', K = '존속';
  const basics = [
    S(`${H}_s1`, '유령 손짓', 1, '공격', [dmg(0.55, E1, { hits: 2 }), stk(K, 1)]),
    S(`${H}_s2`, '반투명 가드', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '경계선상의 유령', 1, '강화', [st('끈기', 1), stk(K, 1), pw('blocked', [stk(K, 1)])], [ // 시동 — 원작 저학년(도발 · 영속)
      O([st('끈기', 1), stk(K, 2), pw('blocked', [stk(K, 1)])]),                  // 수치
      O([st('끈기', 1), stk(K, 1), pw('turnStart', [st('약화', 1, EA)])]),                   // 불꽃길(매 턴 적 약화)
      O([st('끈기', 1), stk(K, 2), pw('blocked', [ddef(0.4, EA)])]),                        // 막으면 번진다
      O([st('끈기', 1), sh(0.8), pw('turnStart', [sh(0.4)])]),                    // 존재의 보호막
      O([st('끈기', 1), pw('blocked', [empower(0.2, 'any')])], { cost: 0 }),      // 막을 때마다 동료 다음 카드
    ], null, ['개전']),
    U(`${H}_u2`, '도깨비불', 1, '공격', [ddef(0.25, EA), sh(0.6), stk(K, 1)], [      // 연료 — 원작 평타 · 강화 공격
      O([ddef(0.32, EA), sh(0.8), stk(K, 1)]),                                     // 수치
      O([ddef(0.3, EA), st('반격', 2)]),                                          // 반격 불꽃
      O([ddef(0.5, E1, { hits: 2 }), stk(K, 1)]),                                 // 두 번 휘두르기
      O([sh(0.5), pw('hurt', [ddef(0.18, EA), stk(K, 1)], { when: { guarded: true } })], { power: true }), // 맞을 때마다 불꽃(원작 4회 피격)
      O([sh(1.4), stk(K, 2)], { cost: 2 }),                                       // 큰 불꽃 방패
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u3`, '불꽃 나눠 주기', 1, '스킬', [per(K), heal(0.28), spendAll(K), dmod(0.15, OTHERS)], [ // 결정타 — 원작 고학년(빼앗은 불꽃을 아군에)
      O([per(K), heal(0.38), spendAll(K), dmod(0.15, OTHERS)]),         // 수치
      O([spendAll(K), empower(0.7, 'any')]),                                      // 불꽃을 다 동료 한 방에
      O([per(K, { n: 1 }), sh(0.45), spendAll(K), st('실드 유지', 1)]),           // 존재의 보호막
      O([later(1, [heal(1.2), sh(0.8)]), stk(K, 2)]),                             // 모닥불(다음 턴)
      O([per(K, { n: 1 }), ddef(0.25, EA), spendAll(K)]),                         // 불꽃 되돌려주기
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u4`, '존재란 무엇인가', 1, '강화', [sh(0.6), pw('endure', [heal(1.0), stk(K, 5)])], [ // 원작 자유 — 어사이드(영속이 있는 동안 · 다시 타오름)
      O([sh(0.8), pw('endure', [heal(1.3), stk(K, 5)])]),                         // 수치
      O([st('끈기', 1), pw('endure', [st('기절', 1, TOP)])]),                     // 변이(버티면 가장 센 적 기절)
      O([st('끈기', 1), pw('turnStart', [stk(K, 1)], { conds: [{ c: 'status', id: '끈기', n: 1 }] })]), // 존재의 증명
      O([sh(0.6), pw('endure', [empower(1.0, 'any'), stk(K, 3)])]),               // 작은 불꽃들(동료 한 방)
      O([st('끈기', 1), pw('endure', [heal(0.6)])]),                 // 두 번째 영속
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '벨라가 나눠 준 작은 불꽃', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.02, who: 'allies' }] },
    passives: [
      pas('다시금 타오르는 존재', 'guardSum', [{ ...stk(K, 1), ofEvent: 0.02 }]),
      pas('우린 이미 완전한 존재들', 'always', [dmod(0.15, ALLIES, { run: true })], { conds: [{ c: 'status', id: '끈기', n: 1 }] }),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 네르 — 거드는 형 · 서포터 ──
// 엔진: 졸며 꾸벅 기도를 모으고(동료 공격도 기도가 된다) → 「세계수의 계시」로 이번 턴 아군 피해를 올리고 그 턴에 동료 카드를 몰아 낸다. 셋이면 꿈속 계시(공짜 계시)
function ner(j) {
  // 팀: 동료의 공격 카드가 기도를 채우고(여왕님 앞은 못 지나가요) · 계시가 이번 턴 동료 피해를 올린다
  // 원작: 저학년 세계수의 계시(짧은 아군 피해↑) · 고학년 엘드르의 축복(치명) · 어사이드 사제장의 무적권 · 졸음 · 오함마 건달 시절
  const H = '네르', K = '꾸벅 기도', DR = '네르_dream';
  const basics = [
    S(`${H}_s1`, '도끼 올려치기', 1, '공격', [ddef(0.7, E1)]),
    S(`${H}_s2`, '달콤한 간식', 1, '스킬', [heal(1.8), stk(K, 1)]),
  ];
  const tokens = [T(DR, '꿈속 계시', H, '스킬', [dmod(0.25, ALLIES), sh(0.5)], { tags: ['소멸'], blurb: '꾸벅 졸던 사제장이 받아 낸 계시 — 이번 턴만 힘이 됩니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '쏟아지는 졸음', 0, '스킬', [stk(K, 2), st('다음 턴 드로우', 1)], [   // 시동 — 계시 뒤 일주일 잔 졸음
      O([stk(K, 3), st('다음 턴 드로우', 1)]),                                     // 수치
      O([stk(K, 1), pw('keepAp', [stk(K, 1), sh(0.3)])], { power: true }),         // 졸음 수행(AP 남기면)
      O([stk(K, 1), draw(1, { who: 'other' }), empower(0.2, 'any')]),             // 대신 기도(동료 카드)
      O([make(DR, 1), stk(K, 1)]),                                                 // 꿈에서 바로 계시
      O([stk(K, 1), sh(1.2)]),                                                     // 잠꼬대(막기)
    ], null, ['신속']),
    U(`${H}_u2`, '여왕님 앞은 못 지나가요', 1, '강화', [st('피해 감소', 1), pw('play', [stk(K, 1)], { when: { type: '공격', who: 'other' } })], [ // 연료 — 섭정 · 뒷수습
      O([st('피해 감소', 2), pw('play', [stk(K, 1)], { when: { type: '공격', who: 'other' } })]), // 수치
      O([st('피해 감소', 1), pw('play', [sh(0.3), stk(K, 1)], { when: { type: '공격', who: 'other' } })]), // 근위(동료 공격마다 실드)
      O([st('피해 감소', 2), pw('hurt', [stk(K, 1)], { when: { guarded: true } })]),          // 대신 맞기
      O([st('사기', 1), pw('play', [stk(K, 1)], { when: { type: '스킬', who: 'other' } })]),   // 세계수 결계(동료 스킬)
      O([pw('turnStart', [stk(K, 1), draw(1)])], { cost: 0 }),                                // 섭정 업무
    ], [B('cost'), B('draw'), B(BK)]),
    U(`${H}_u3`, '세계수의 계시', 1, '스킬', [dmod(0.2, ALLIES), ifStack(K, 2), dmod(0.2, ALLIES), spendAll(K)], [ // 결정타 — 원작 저학년
      O([dmod(0.27, ALLIES), ifStack(K, 2), dmod(0.27, ALLIES), spendAll(K)]),     // 수치
      O([{ k: 'perPlayed' }, sh(0.4), ifStack(K, 2), st('사기', 2), spendAll(K)]), // 뒷수습(낸 장수만큼 실드)
      O([dmod(0.35, ALLIES)], { cost: 0 }),                                        // 짧고 굵게
      O([dmod(0.15, ALLIES), pw('turnStart', [dmod(0.12, ALLIES), stk(K, 1)])], { power: true }), // 워리어즈 생츄어리
      O([{ k: 'critMod', v: 0.25, target: ALLIES }, ifStack(K, 2), { k: 'critMod', v: 0.25, target: ALLIES }, spendAll(K)]), // 엘드르의 축복(치명)
    ], [B('cost'), B('draw'), B(BK)]),
    U(`${H}_u4`, '뒷골목 오함마', 1, '공격', [ddef(1.0, E1), ifKill, stk(K, 3)], [        // 원작 자유 — 사탕을 뜯던 건달 시절
      O([ddef(1.3, E1), ifKill, stk(K, 3)]),                                       // 수치
      O([ddef(0.65, EA), stk(K, 1)]),                                              // 세계수의 도끼(광역)
      O([ddef(0.8, E1), stk(K, 2)]),                                               // 깨우는 망치
      O([ddef(0.9, E1), stk(K, 1), pw('play', [ddef(0.45, E1)], { when: { type: '스킬' } })], { power: true }), // 건달 시절
      O([ddef(1.9, E1), tough(2)], { cost: 2 }),                                   // 결계 부수기(강인도)
    ], [B('power'), B('weakSpot'), B(BK)], ['분쇄']),
  ];
  hero(j, {
    keyword: { name: K, desc: '졸다 들키면 우기는 기도', carrier: 'self', cap: 3, onMax: { make: DR, consume: true } },
    passives: [
      pas('잠이 아니라 기도', 'turnStart', [stk(K, 1)]),
      pas('사제장의 무적권', 'play', [st('피해 감소', 1)], { when: { type: '스킬' } }),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, tokens, uniques,
  });
}

export const JOBS = [['수인/디아나_왕년', diana], ['요정/에르핀', erpin], ['수인/버터', butter], ['유령/시온더다크불릿', sion], ['엘프/아멜리아', amelia], ['유령/벨라', bella], ['요정/네르', ner]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
