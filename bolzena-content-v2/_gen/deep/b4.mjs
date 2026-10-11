// 묶음 4 — 거드는 형 10 + 아껴 두기형 2. node _gen/deep/b4.mjs [사도 일부]
// 이 묶음의 고유 효과 · 패시브는 앞선 갈래 재설계(10-08)에서 원작 근거로 이미 다시 짠 것이 많아, 맞는 것은 이어 쓰고(kw0 · pas0) 카드 · 신탁 · 기본 카드를 새로 짠다.
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, ALLY, ALLIES, OTHERS, dmg, ddef, sh, heal, st, stk, spendAll, per, perTag, draw, make, disc, gauge,
  empower, dmod, later, extra, cost, pw, pas, O, B, U, S, T, hero } = L;
const cleanse = v => ({ k: 'cleanse', v });
const ifAll = { k: 'ifAllHeroes' };
const exileTag = tag => ({ k: 'exileFrom', from: 'hand', all: true, tag });
const kw0 = j => { const h = j.heroes[0]; return { keyword: h.keyword, ...(h.keywords ? { keywords: h.keywords } : {}) }; };
const pas0 = j => j.heroes[0].passives;

// ── 아야 — 거드는 형 · 딜러(엘다인) ──
// 엔진: 적에게 서리(동상 — 받는 피해 +5% · 주는 피해 -3%/겹)를 피워 두면 동료가 칠 때마다 한 겹 더 번진다 → 빙설화도 발도가 서리를 녹여 벤다
function aya(j) {
  // 팀: 「시련 속의 눈꽃」 — 다른 아군이 적을 칠 때마다 서리 1(동료의 칼이 깊이 박히는 판)
  // 원작: 고학년 「만개설화」(눈꽃이 번지며 동상 — 냉정 아군 받는 피해↑) · 저학년 「싸락나비」(갔다 돌아오는 나비 · 보호막 · 회복) · 빙설화도 2회 평타 · 만년설의 현자
  const H = '아야', K = '서리';
  const basics = [
    S(`${H}_s1`, '빙설화도', 1, '공격', [dmg(0.5, E1, { hits: 2 })]),
    S(`${H}_s2`, '빙창 탄환', 2, '공격', [dmg(2.1, E1)]),
    S(`${H}_s3`, '얼음 장막', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u3`, '동상 조준', 0, '스킬', [stk(K, 2, E1), draw(1)], [                                // 시동 — 눈꽃을 피울 자리
      O([stk(K, 2, E1), draw(1)]),                                                   // (수치)
      O([stk(K, 1, EA), draw(1)]),                                                   // 눈보라 조짐
      O([stk(K, 2, TOP), sh(0.4)]),                                                  // 큰 놈부터
      O([stk(K, 3, E1)]),  // 깊은 동상
      O([stk(K, 1, E1), st('냉정 약점', 2, E1)]),  // 동상 — 냉정 아군에게 약점(원작)
    ], null),
    U(`${H}_u1`, '싸락나비', 1, '공격', [dmg(0.3, ER, { hits: 2 }), stk(K, 1, EA), later(1, [sh(0.6), heal(0.3)])], [ // 연료 — 원작 저학년(갔다 돌아오는 나비)
      O([dmg(0.3, ER, { hits: 2 }), stk(K, 1, EA), later(1, [sh(0.6), heal(0.3)])]),  // (수치)
      O([dmg(0.25, ER, { hits: 3 }), stk(K, 1, EA)]),                                 // 나비 떼
      O([dmg(0.5, E1), stk(K, 2, E1), later(1, [sh(0.5)])]),                          // 한 적에게 내려앉기
      O([stk(K, 1, EA), later(1, [sh(0.8), heal(0.5)])], { cost: 0 }),               // 돌아오는 나비
      O([dmg(0.3, ER, { hits: 2 }), st('약화', 1, EA)]),                              // 스킬 피해 감소(원작)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '빙설화도 발도', 1, '공격', [dmg(0.6, E1), per(K, { n: 1 }), dmg(0.25, E1), spendAll(K)], [ // 결정타 — 서리를 녹여 베기
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.25, E1), spendAll(K)]),                // (수치)
      O([per(K, { n: 1 }), dmg(0.2, EA)]),                                            // 셋째 공격의 범위 베기
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.22, E1)]),                             // 눈꽃은 남긴다
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.22, E1), st('기절', 1, E1)], { cost: 2 }),  // 눈꽃 만개(기절)
      O([per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)], { cost: 0 }),                  // 빠른 발도
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '만년설의 현자', 1, '강화', [stk(K, 1, EA), pw('turnStart', [stk(K, 1, EA)])], [       // 원작 자유 — 만년설의 현자 · 동생들을 모은 눈보라
      O([stk(K, 1, EA), pw('turnStart', [stk(K, 1, EA)])]),                           // (수치)
      O([pw('turnStart', [stk(K, 1, EA)])], { cost: 0 }),                             // 그치지 않는 눈
      O([stk(K, 1, EA), pw('turnStart', [dmod(0.1, OTHERS)])]),                       // 동생들의 칼
      O([pw('turnStart', [stk(K, 1, EA), sh(0.3)])], { cost: 2 }),                    // 눈은 그치고 봄이 온다
      O([stk(K, 2, EA), pw('kill', [stk(K, 2, EA)])]),                                // 소중한 이를 해치면
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '만개설화', cost: 150, fx: [dmg(0.9, EA), stk(K, 2, EA), { k: 'tough', v: 1, target: EA }] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 슈로 — 거드는 형 · 서포터 ──
// 엔진: 검의 영역(아군 받는 피해 -6%/개)을 깔아 두면 동료가 무슨 카드를 내든 그 종류에 맞는 덕(공격 → 추가 피해 · 스킬 → 실드 · 강화 → 회복)
function shuro(j) {
  // 팀: 「역할의 검」 — 동료 카드 종류마다 다른 이득(원작 조율의 역할별 효과)
  // 원작: 저학년 「조율」(검의 영역 · 역할별 이득) · 고학년 「종이 한 장 차이」(가장 센 아군 공격력 복사 · 검기 3회) · 강화 평타(세 번 맞으면 물러났다 전진 · 회복) · 칼로 사귀는 친구
  const H = '슈로', K = '검의 영역';
  const basics = [
    S(`${H}_s1`, '목검 찌르기', 1, '공격', [dmg(0.45, E1, { hits: 2 })]),
    S(`${H}_s2`, '유리비늘 붕대', 1, '스킬', [heal(1.4)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '독 없는 투구꽃', 1, '강화', [st('결의', 1), pw('turnStart', [stk(K, 1)])], [         // 시동 — 후회를 지닌 검사의 매일
      O([st('결의', 1), pw('turnStart', [stk(K, 1)])]),                               // (수치)
      O([pw('turnStart', [stk(K, 1), heal(0.15)])]),                                  // 매일 조율
      O([stk(K, 2), pw('hurt', [stk(K, 1)])]),                                        // 강한 상대에게 강하다
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                 // 가벼운 영역
      O([st('결의', 2), stk(K, 1)]),                                                  // 아름다운 모양만
    ], null, ['개전']),
    U(`${H}_u1`, '검 조율', 1, '스킬', [stk(K, 2), sh(0.8), draw(1)], [                                // 연료 — 원작 저학년(조율)
      O([stk(K, 2), sh(0.8), draw(1)]),                                               // (수치)
      O([stk(K, 1), heal(0.7), draw(1)]),  // 서포터 조율(치유량)
      O([stk(K, 1), dmod(0.15, OTHERS), draw(1)]),  // 딜러 조율(평타 피해)
      O([stk(K, 3), sh(1.0)]),  // 넓은 영역
      O([stk(K, 1), draw(2)], { cost: 0 }),                                           // 빠른 조율
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u3`, '장검 · 단도 · 한손검', 1, '공격', [dmg(0.3, E1), per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)], [ // 결정타 — 영역의 검을 다 뽑아
      O([dmg(0.3, E1), per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)]),                // (수치)
      O([per(K, { n: 1 }), dmg(0.45, E1), spendAll(K)]),                              // 한 자루에 몰아
      O([dmg(0.3, E1), per(K, { n: 1 }), dmg(0.2, EA)]),                              // 영역은 둔다
      O([per(K, { n: 1 }), heal(0.3), spendAll(K)], { cost: 0 }),                     // 검을 거두고 돌봄
      O([dmg(0.3, E1), per(K, { n: 1 }), dmg(0.22, EA), st('약점 노출', 1, E1)]),  // 모든 방어력 감소 — 약점 노출(어사이드)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '통성명', 1, '공격', [dmg(0.85, E1), stk(K, 1), sh(0.5)], [                           // 원작 자유 — 칼을 맞대야 친구가 된다
      O([dmg(0.85, E1), stk(K, 1), sh(0.5)]),                                         // (수치)
      O([dmg(0.7, E1), st('반격', 2)]),                                               // 받아치며 사귀기
      O([dmg(1.0, E1), heal(0.4)]),                                                   // 물러났다 전진
      O([dmg(0.6, E1), stk(K, 2)], { cost: 0 }),                                      // 가벼운 인사
      O([dmg(0.4, EA), stk(K, 1)]),                                                   // 모두와 통성명
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '종이 한 장 차이', cost: 150, fx: [{ k: 'atkMod', v: 0.3, target: 'self' }, dmg(0.4, EA, { hits: 3 }), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`], basics, uniques });
}

// ── 우로스 — 거드는 형 · 서포터(엘다인) ──
// 엔진: 지배의 영역(파티 공격력 +6%/개, 턴마다 1 줄어듦)을 펼칠수록 넓어지고(셋 넘게 펼치면 파티 다음 카드 +20%) · 다 줄어 끝나면 우로스 다음 카드 +40%
function uros(j) {
  // 팀: 「지배의 영역」 — 가장 센 자의 힘을 모두에게(파티 공격력)
  // 원작: 저학년 「순환」(쓸 때마다 영역 확장 · 최대 3 · 가장 센 아군 공격력 비례로 모두에게) · 영역이 끝나면 자기 피해↑ · 어사이드 「내 마음의 등대」(비수 · 받는 피해 중첩) · 백사당
  const H = '우로스', K = '지배의 영역';
  const basics = [
    S(`${H}_s1`, '검기 날리기', 1, '공격', [dmg(0.5, E1, { hits: 2 })]),
    S(`${H}_s2`, '슈로의 인사', 1, '스킬', [sh(1.5)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '세계수의 보물 파괴', 1, '강화', [st('협공', 1), pw('turnStart', [stk(K, 1)])], [       // 시동 — 패왕의 기억
      O([st('협공', 1), pw('turnStart', [stk(K, 1)])]),                               // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                 // 꿈속의 기억
      O([stk(K, 1), pw('turnStart', [st('협공', 1)])]),  // 패왕의 일격
      O([stk(K, 1), pw('turnStart', [stk(K, 1), draw(1)])]),  // 백사당 일과
      O([st('협공', 1), pw('shuffle', [stk(K, 1)])]),                                 // 순환의 이름
    ], null, ['개전']),
    U(`${H}_u1`, '순환의 고리', 1, '스킬', [disc(2), draw(3), stk(K, 1)], [                             // 연료 — 원작 저학년(순환 · 영역 확장)
      O([disc(2), draw(3), stk(K, 1)]),                                               // (수치)
      O([disc(1), draw(2), stk(K, 1)], { cost: 0 }),                                  // 짧은 순환
      O([stk(K, 2), sh(0.5)]),                                                        // 영역만 넓히기
      O([disc(3), draw(4)]),                                                          // 크게 돌리기
      O([draw(2), stk(K, 1), empower(0.2, 'any')]),                                   // 가장 센 자에게
    ], [B('draw'), B('cost'), B(BK)]),
    U(`${H}_u4`, '거대한 뱀', 2, '공격', [per(K, { n: 1 }), dmg(0.55, EA), spendAll(K)], [ // 결정타 — 영역을 걷고 뱀을
      O([per(K, { n: 1 }), dmg(0.55, EA), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.8, E1), spendAll(K)]),  // 한 놈을 휘감기
      O([per(K, { n: 1 }), dmg(0.45, EA)]),  // 영역은 둔다
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)], { cost: 1 }),  // 작은 뱀
      O([per(K, { n: 1 }), dmg(0.45, EA), empower(0.4)]),  // 패왕의 귀환
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '내 마음의 등대', 1, '공격', [per(K, { n: 1 }), dmg(0.3, ER, { hits: 2 }), st('취약', 1, EA)], [ // 원작 자유 — 어사이드(비수 · 받는 피해 중첩)
      O([per(K, { n: 1 }), dmg(0.3, ER, { hits: 2 }), st('취약', 1, EA)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.35, E1), st('취약', 2, E1)]),                        // 한 적에게 비수
      O([dmg(0.25, ER, { hits: 4 }), st('취약', 1, EA), stk(K, 1)]),  // 비수 떼
      O([dmg(0.3, ER, { hits: 3 }), stk(K, 1), st('취약', 1, ER)]),  // 등대 불빛
      O([st('약점 노출', 1, ER), draw(1)], { cost: 0 }),  // 비수 — 약점 노출(받는 피해 중첩)
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '압도', cost: 250, fx: [dmg(0.45, EA, { hits: 2 }), st('피해 감소', 2), stk(K, 3)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`], basics, uniques });
}

// ── 로네(시장) — 거드는 형 · 딜러 ──
// 엔진: 동료가 카드를 낼 때마다 돈까스 도시락이 배달된다(누구든 먹으면 회복 · 지지율) → 「돈까스 시장, 로네!」가 손의 도시락만큼 광역 · 지지율이 셋이면 파티 다음 카드 +50% · 먹지 못한 돈까스는 화풀이 사격
function roneMayor(j) {
  // 팀: 「돈까스 배달」 — 동료 카드마다 도시락 1장(로네의 손에) · 지지율이 차면 파티가 강해진다
  // 원작: 저학년 「돈까스 시장, 로네!」(배달 드론 · 중후열 공격력↑ · 4회 사격 · 1명이면 더) · 고학년 「사랑받는 시장, 로네!」(지지자 돌진) · 업무 효율 131% · 끝내 먹지 못하는 돈까스
  const H = '로네_시장', K = '지지율', T1 = `${H}_t1`;
  const basics = [
    S(`${H}_s1`, '비서 호출', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '비서실 총출동', 2, '공격', [dmg(2.3, E1)]),
    S(`${H}_s3`, '방탄 연단', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [T(T1, '돈까스 도시락', H, '스킬', [heal(0.4), stk(K, 1)], { tags: ['소멸'], blurb: '시장실로 배달된 돈까스 도시락입니다' })];
  const BK = [make(T1, 1)];
  const uniques = [
    U(`${H}_u2`, '배달 주문', 1, '스킬', [make(T1, 2), draw(1)], [                                    // 시동 — 도시락 주문
      O([make(T1, 2), draw(1)]),                                                     // (수치)
      O([make(T1, 1), draw(1)], { cost: 0 }),                                        // 하나만
      O([make(T1, 3)]),                                                              // 시청 단체 주문
      O([make(T1, 2), stk(K, 1)]),                                                   // 지지자 몫까지
      O([make(T1, 1), dmod(0.15, OTHERS)]),                                          // 중후열 공격력↑(원작)
    ], null),
    U(`${H}_u1`, '돈까스 시장, 로네!', 1, '공격', [dmg(0.3, EA), perTag(T1), dmg(0.25, EA)], [ // 결정타 — 원작 저학년(배달 드론 4회 사격)
      O([dmg(0.3, EA), perTag(T1), dmg(0.25, EA)]),  // (수치)
      O([dmg(0.5, E1), perTag(T1), dmg(0.45, E1)]),  // 적이 하나면 더 세게(원작)
      O([perTag(T1), dmg(0.4, EA), exileTag(T1)]),  // 다 먹어 치우기
      O([perTag(T1), dmg(0.3, EA), stk(K, 1)]),  // 지지율 연설
      O([dmg(0.2, EA, { hits: 2 }), make(T1, 1)], { cost: 0 }),                      // 다음 배달 예약
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '돈까스 모독은 용서 못 해', 2, '공격', [dmg(1.8, E1), per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)], [ // 연료 — 끝내 못 먹은 돈까스의 분노
      O([dmg(1.8, E1), per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)]),                 // (수치)
      O([dmg(1.5, E1), make(T1, 2)]),                                                 // 다시 주문
      O([dmg(1.0, EA), st('취약', 1, EA)]),  // 시청 전체 분노
      O([dmg(1.6, E1), per(K, { n: 1 }), dmg(0.35, E1)]),                             // 지지율은 지킨다
      O([dmg(1.6, E1), st('취약', 2, E1)]),                                           // 공개 사과 요구
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u5`, '업무 효율 131%', 0, '스킬', [stk(K, 1), cost(-1)], [                                 // 원작 자유 — 일선 경험을 살린 정책
      O([stk(K, 1), cost(-1)]),                                                       // (수치)
      O([make(T1, 1), cost(-1)]),                                                     // 점심시간 단축
      O([stk(K, 1), draw(1)]),                                                        // 결재 서류
      O([cost(-1), draw(1)]),  // 업무 재배치
      O([stk(K, 2)]),  // 지지율 131%
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '시장 로네를 향한 지지', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.5, consume: true } },
    passives: [
      pas('돈까스 배달', 'play', [make(T1, 1)], { when: { who: 'other' } }),
      pas('먹지 못한 돈까스', 'exhaust', [dmg(0.3, ER)]),
    ],
    ult: { name: '사랑받는 시장, 로네!', cost: 200, fx: [dmg(0.25, EA, { hits: 3 }), make(T1, 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 폴랑 — 거드는 형 · 서포터 ──
// 엔진: 누가 치든 공격 카드마다 머스킷 장전 → 셋이면 파티 다음 카드 +40% · 이번 턴 세 사도가 모두 카드를 냈으면 120도 포위 완성(경례 · 일제 사격 강화)
function polang(j) {
  // 팀: 「120도 포위」 — 세 사도가 각자 한 번씩 치면 포위 완성(인원 세기가 아니라 누가 쳤나의 분포)
  // 원작: 요정 전통 「셋이 120도로 둘러싸 패기」 · 저학년 「요정 왕국에 경례」(전원 회복 · 스킬 피해↑) · 고학년 「로얄 스트레이트」 · 강화 평타(세 번째마다 장전 · 가장 다친 아군 회복)
  const H = '폴랑', K = '머스킷 장전';
  const basics = [
    S(`${H}_s1`, '머스킷 사격', 1, '공격', [dmg(0.42, EA)]),
    S(`${H}_s2`, '야전 붕대', 1, '스킬', [heal(1.5)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '120도 삼각 편대', 1, '강화', [stk(K, 1), draw(1, { who: 'other' }), pw('turnStart', [stk(K, 1)])], [ // 시동 — 아직 안 친 동료를 불러낸다
      O([stk(K, 1), draw(1, { who: 'other' }), pw('turnStart', [stk(K, 1)])]),        // (수치)
      O([pw('turnStart', [draw(1, { who: 'other' })])]),                              // 매일 편대 점호
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0 }),                      // 장전 훈련
      O([draw(2, { who: 'other' }), pw('turnStart', [stk(K, 1)])]),  // 칸타 심문 대형
      O([st('협공', 1), pw('turnStart', [stk(K, 1)])]),                               // 에르피엔 진군깃발
    ], null, ['개전']),
    U(`${H}_u1`, '요정 왕국에 경례', 1, '스킬', [heal(0.8), dmod(0.2, ALLIES), stk(K, 1)], [            // 연료 — 원작 저학년(전원 회복 · 스킬 피해↑)
      O([heal(0.8), dmod(0.2, ALLIES), stk(K, 1)]),                                   // (수치)
      O([heal(0.6), stk(K, 1), ifAll, dmod(0.3, ALLIES)]),  // 포위 완성 뒤 경례
      O([heal(1.0), st('피해 감소', 1), stk(K, 1)]),  // 후열에 무적(원작)
      O([dmod(0.25, ALLIES), draw(1)], { cost: 0 }),                                  // 짧은 경례
      O([heal(0.7), stk(K, 2)]),                                                      // 경례 뒤 장전
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u2`, '일제 사격', 1, '공격', [dmg(0.4, EA), per(K, { n: 1 }), dmg(0.2, EA)], [  // 결정타 — 장전한 머스킷을 한꺼번에
      O([dmg(0.4, EA), per(K, { n: 1 }), dmg(0.2, EA)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.3, EA), ifAll, st('기절', 1, E1)]),  // 포위 완성 — 기절
      O([per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)]),  // 모두 쏘기
      O([per(K, { n: 1 }), dmg(0.4, E1), heal(0.3)]),  // 가장 다친 아군 회복(원작)
      O([per(K, { n: 1 }), dmg(0.25, EA), st('약점 노출', 1, E1)]),  // 폴랑 버스터 — 약점 노출(방어력↓)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '경비대장 폴랑입니다', 1, '스킬', [st('협공', 1), heal(0.5), ifAll, dmod(0.25, ALLIES)], [ // 원작 자유 — 에르핀에게 집착에 가까운 충성
      O([st('협공', 1), heal(0.5), ifAll, dmod(0.25, ALLIES)]),                       // (수치)
      O([st('협공', 2), heal(0.4)]),                                                  // 양날개의 한쪽
      O([heal(0.6), stk(K, 1), draw(1)]),  // 경비 교대
      O([st('협공', 1), ifAll, dmod(0.3, ALLIES)], { cost: 0 }),                      // 포위만 되면
      O([heal(0.5), stk(K, 1), pw('turnStart', [heal(0.15)])], { cost: 0, power: true }),  // 경비대 순찰
    ], [B('heal'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '포위를 위해 장전한 머스킷', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.4, consume: true } },
    passives: [
      pas('120도 포위', 'play', [stk(K, 1)], { when: { type: '공격', who: 'any' } }),
      pas('세 번째 장전', 'play', [heal(0.35)], { when: { every: 3 } }),
    ],
    ult: { name: '로얄 스트레이트', cost: 150, fx: [dmg(1.1, EA), heal(1.5), st('기절', 1, E1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`], basics, uniques });
}

// ── 실비아 — 거드는 형 · 탱커(엘다인) ──
// 엔진: 동료를 티파티 손님(초청객)으로 들이면 그 손님이 스킬을 낼 때마다 은방울이 울려 무작위 적에게 피해 · 은방울 셋이면 파티 다음 카드 +40%
function sylvia(j) {
  // 팀: 「그 어머니에 그 딸」 — 초청객 아군의 스킬마다 추가 피해 · 은방울(손님을 초대해 두면 그 손님의 스킬이 울린다)
  // 원작: 저학년 「궁극의 유희」(모든 아군에게 초청객 · 스킬이 맞으면 추가 피해) · 고학년 「소녀에게 오시려고요?」(은분수 충격파 · 은방울 보호막) · 장갑을 던져 결투 신청 · 비비의 어머니
  const H = '실비아', G = '초청객', K = '은방울';
  const basics = [
    S(`${H}_s1`, '부채 휘두르기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '숙녀의 자세', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '초대장 건네기', 1, '스킬', [stk(G, 1, ALLY), sh(1.1)], [                                // 시동 — 손님 들이기
      O([stk(G, 1, ALLY), sh(1.1)]),                                                  // (수치)
      O([stk(G, 1, ALLY), draw(1)], { cost: 0 }),                                     // 급한 초대
      O([stk(G, 1, 'strongestAlly'), sh(0.8), stk(K, 1)]),                            // 귀빈석
      O([stk(G, 1, ALLY), sh(0.8), st('피해 감소', 1)]),                              // 은방울 보호막(원작)
      O([stk(G, 1, ALLY), heal(0.6), stk(K, 1)]),  // 티파티 다과
    ], null),
    U(`${H}_u2`, '궁극의 유희', 2, '스킬', [stk(G, 1, ALLIES), per(K, { n: 1 }), sh(0.5), spendAll(K)], [  // 결정타 — 원작 저학년(모든 아군에게 초청객)
      O([stk(G, 1, ALLIES), per(K, { n: 1 }), sh(0.5), spendAll(K)]),                // (수치)
      O([stk(G, 1, ALLIES), heal(0.6), stk(K, 1)]),  // 매초 회복(원작)
      O([per(K, { n: 1 }), sh(0.6), spendAll(K)], { cost: 1 }),                       // 은방울만
      O([stk(G, 1, ALLIES), per(K, { n: 1 }), sh(0.4)]),                              // 은방울은 둔다
      O([stk(G, 1, ALLIES), st('취약', 1, EA)], { cost: 1 }),                         // 받는 피해 증가(원작)
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u4`, '티파티 준비', 1, '강화', [st('결의', 1), pw('play', [sh(0.25), stk(K, 1)], { when: { type: '스킬', who: 'any', marked: G } })], [ // 연료 — 손님의 스킬마다 은방울
      O([st('결의', 1), pw('play', [sh(0.25), stk(K, 1)], { when: { type: '스킬', who: 'any', marked: G } })]), // (수치)
      O([pw('play', [dmg(0.25, ER)], { when: { type: '스킬', who: 'any', marked: G } })], { cost: 0 }), // 은분수 물보라
      O([st('결의', 1), pw('turnStart', [stk(K, 1)])]),                                // 매일 오후 티파티
      O([pw('play', [sh(0.3), stk(K, 1)], { when: { who: 'any', marked: G } })], { cost: 2 }), // 무엇을 내든 환영
      O([st('결의', 2), pw('play', [stk(K, 1)], { when: { type: '스킬', who: 'any', marked: G } })]), // 우아한 숙녀
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u5`, '장갑 던지기', 1, '공격', [dmg(0.7, E1), st('약화', 1, E1), stk(G, 1, ALLY)], [          // 원작 자유 — 장갑을 던져 결투 신청
      O([dmg(0.7, E1), st('약화', 1, E1), stk(G, 1, ALLY)]),                          // (수치)
      O([dmg(0.9, E1), st('약화', 2, E1)]),                                           // 꼬마 취급은 질색
      O([dmg(0.6, TOP), st('약화', 2, TOP)]),                                         // 가장 센 적의 피해↓(원작 강화 평타)
      O([dmg(0.6, E1), stk(K, 2)]),                                                   // 결투의 종소리
      O([dmg(0.5, EA), stk(G, 1, ALLY)]),  // 모두에게 결투장
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '소녀에게 오시려고요?', cost: 300, fx: [dmg(0.6, EA), sh(3.5), st('기절', 1, E1), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, uniques });
}

// ── 아네트 — 거드는 형 · 서포터 ──
// 엔진: 오늘의 MVP(그 아군 주는 피해 +30%)를 점찍고 → MVP 가 카드를 낼 때마다 관중 함성 + 회복 → 함성 넷이면 파티 다음 카드 +50%
function annette(j) {
  // 팀: 「싸워라 싸워~」 — MVP 아군이 카드를 낼 때마다 함성 · 회복(한 명에게 몰아준다)
  // 원작: 저학년 「MVP 예측」(피해 가장 많이 준 아군을 MVP — 공속 · 주는 피해↑) · 고학년 「편파 중계」(쓰레기 12회 · 가장 센 적 출전 금지) · 둘째 평타 대신 응원 회복 · 결투 심판
  const H = '아네트', M = '오늘의 MVP', K = '관중 함성';
  const basics = [
    S(`${H}_s1`, '자극적인 멘트', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '응원 도시락', 1, '스킬', [heal(1.3)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '싸움 구경', 1, '강화', [stk(K, 2), pw('turnStart', [stk(K, 1)])], [                 // 시동 — 관중석 개장
      O([stk(K, 2), pw('turnStart', [stk(K, 1)])]),                                   // (수치)
      O([st('사기', 1), pw('turnStart', [stk(K, 1)])]),                               // 응원의 마음
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                 // 구경꾼
      O([stk(K, 1), pw('play', [heal(0.12)], { when: { who: 'any', marked: M } })], { cost: 0 }),  // 응원석
      O([stk(K, 2), pw('turnStart', [stk(M, 1, 'strongestAlly')])]),  // 매 경기 MVP
    ], null, ['개전']),
    U(`${H}_u3`, 'MVP 예측', 1, '스킬', [stk(M, 1, 'strongestAlly'), per(K, { n: 1 }), sh(0.35)], [ // 결정타 — 원작 저학년(MVP 선정)
      O([stk(M, 1, 'strongestAlly'), per(K, { n: 1 }), sh(0.35)]),  // (수치)
      O([stk(M, 1, ALLY), per(K, { n: 1 }), heal(0.3)]),  // 내가 고른 MVP
      O([per(K, { n: 1 }), sh(0.4), spendAll(K)]),  // 함성을 다 쏟아
      O([stk(M, 2, 'strongestAlly'), draw(1)], { cost: 0 }),                           // 연속 MVP(어사이드)
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)]),                               // MVP 축포(어사이드)
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u1`, '결투 성사', 1, '스킬', [st('협공', 1), stk(M, 1, ALLY), stk(K, 1)], [                 // 연료 — 결투 심판
      O([st('협공', 1), stk(M, 1, ALLY), stk(K, 1)]),                                 // (수치)
      O([stk(M, 1, ALLY), draw(1, { who: 'other' }), stk(K, 2)]),  // 선수 입장
      O([st('협공', 2), stk(K, 1)]),                                                  // 링 위로
      O([stk(M, 1, ALLY), st('취약', 1, E1), stk(K, 1)], { cost: 0 }),  // 상대를 도발
      O([stk(K, 2), heal(0.5), st('협공', 1)]),  // 응원 함성
    ], [B('draw'), B('ap'), B(BK)]),
    U(`${H}_u2`, '허접~', 1, '공격', [dmg(0.9, E1), st('취약', 2, E1), stk(K, 1)], [                    // 원작 자유 — 교주에게도 「허접」
      O([dmg(0.9, E1), st('취약', 2, E1), stk(K, 1)]),                                // (수치)
      O([dmg(0.7, E1), st('약화', 2, E1), stk(K, 1)]),  // 깐족깐족
      O([dmg(0.25, ER, { hits: 4 }), stk(K, 1)]),  // 편파 중계(쓰레기 투척)
      O([dmg(0.7, TOP), st('약화', 1, TOP), stk(K, 1)]),                              // 출전 금지(가장 센 적)
      O([dmg(0.8, E1), heal(0.4), stk(K, 1)]),  // 둘째 멘트는 응원
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j),
    passives: [
      pas('관중석', 'fightStart', [stk(M, 1, 'strongestAlly'), stk(K, 2)]),
      pas('싸워라 싸워~', 'play', [stk(K, 1), heal(0.24)], { when: { marked: M } }),
    ],
    ult: { name: '편파 중계', cost: 250, fx: [dmg(0.18, ER, { hits: 5 }), st('약화', 2, TOP), stk(M, 2, 'strongestAlly')] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`], basics, uniques });
}

// ── 키샤 — 거드는 형 · 서포터 ──
// 엔진: 가장 센 아군 하나를 짱팬으로 찍어 그 카드에 추가타 · 사기 → 「떼창 지진」으로 공연을 열면 공연 동안 파티가 준 피해를 모아 다음 턴 시작에 무대 폭발
function kisha(j) {
  // 팀: 「팬 서비스」 — 짱팬 아군의 공격마다 추가타(한 줌의 팬을 진심으로)
  // 원작: 저학년 「궁극의 멜로디」(아군 피해↑ · 나눠 회복 → 팬 서비스) · 고학년 「고! 고! 러브 스테이지!」(공연 동안 아군 피해 저장 → 폭발) · 강화 공격(공격력 가장 높은 아군 곁 · 스킬 피해↑) · 엘리아스 최초의 언더돌
  const H = '키샤', F = '짱팬', P = '공연 중';
  const basics = [
    S(`${H}_s1`, '진심 하트', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '팬레터 낭독', 1, '스킬', [heal(1.5)]),
  ];
  const BK = [st('사기', 1)];
  const uniques = [
    U(`${H}_u1`, '궁극의 멜로디', 1, '스킬', [stk(F, 1, 'strongestAlly'), heal(0.8), st('사기', 1)], [   // 시동 — 원작 저학년(짱팬 지정)
      O([stk(F, 1, 'strongestAlly'), heal(0.8), st('사기', 1)]),                       // (수치)
      O([stk(F, 1, ALLY), heal(0.8), draw(1)]),  // 내가 고른 팬
      O([stk(F, 1, 'strongestAlly'), dmod(0.3, 'strongestAlly'), st('사기', 2)]),  // 펜스 너머의 너
      O([heal(1.1), st('사기', 2)]),  // 앙코르
      O([stk(F, 1, 'strongestAlly'), draw(1)], { cost: 0 }),                          // 립싱크 무대
    ], null),
    U(`${H}_u2`, '체리 레드 조명봉', 1, '스킬', [cost(-1), draw(2)], [                                  // 연료 — 짱팬의 다음 무대를 가볍게
      O([cost(-1), draw(2)]),                                                         // (수치)
      O([st('협공', 1), draw(1)], { cost: 0 }),                                       // 응원봉 흔들기
      O([cost(-1), empower(0.3, 'any'), draw(1)]),  // 짱볼레이드
      O([draw(3)], { cost: 2 }),                                                      // 굿즈 판매
      O([cost(-1), st('사기', 1)]),                                                   // 하트 조명
    ], [B('draw'), B('ap'), B(BK)]),
    U(`${H}_u4`, '떼창 지진', 1, '공격', [stk(P, 1), dmg(0.5, EA)], [                                   // 결정타 — 공연을 열어 다음 턴 무대 폭발
      O([stk(P, 1), dmg(0.5, EA)]),                                                   // (수치)
      O([stk(P, 1), dmg(0.35, EA), heal(0.4)]),                                       // 객석 회복(어사이드)
      O([stk(P, 1), dmg(0.7, LOW)]),                                                  // HP 가장 낮은 적에게 파동(원작)
      O([dmg(0.8, EA)], { cost: 2 }),                                                 // 공연장이 무너졌다
      O([stk(P, 1), st('약화', 1, EA)], { cost: 0 }),                                 // 조용한 개막
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '하트 파동', 1, '공격', [dmg(0.8, LOW), st('약화', 2, LOW)], [                         // 원작 자유 — 고학년의 파동(HP 가장 낮은 적)
      O([dmg(0.8, LOW), st('약화', 2, LOW)]),                                         // (수치)
      O([dmg(0.3, ER, { hits: 3 }), st('약화', 1, EA)]),  // 파동 여러 번
      O([dmg(0.7, LOW), heal(0.4)]),                                                  // 하트 회복
      O([dmg(0.6, LOW), stk(F, 1, 'strongestAlly'), st('사기', 1)]),  // 팬서비스 하트
      O([dmg(0.9, E1), st('약화', 1, E1)]),                                           // 진심을 담아
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '고! 고! 러브 스테이지!', cost: 200, fx: [dmg(0.9, EA), stk(P, 1), draw(2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, uniques });
}

// ── 미로 — 거드는 형 · 탱커 ──
// 엔진: 동료가 낸 카드를 좌우 뒤집어 한 번 더 — 동료 공격엔 실드, 동료 스킬엔 피해 · 막아 낸 공격의 적에게 시선(주는 피해 -8%/겹) → 깨진 거울 조각이 시선만큼 · 수많은 시선은 거울 속에서 맞은 만큼 되쏜다
function miro(j) {
  // 팀: 「좌우가 뒤집힌 응원」 — 동료 공격 → 실드, 동료 스킬 → 방어 기반 피해
  // 원작: 거울처럼 따라 하기(좌우가 뒤집힌 채) · 고학년 「수많은 시선」(거울 속 · 맞은 횟수만큼 광선) · 다섯 번째 공격마다 눈부신 빛(눈가림) · 어사이드 「이젠 내가 지켜줄 거야」(아군 전원 받는 피해↓)
  const H = '미로', K = '시선';
  const basics = [
    S(`${H}_s1`, '앞으로 넘어지기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '거울 뒤로', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u2`, '거울 속으로', 1, '스킬', [sh(0.8), stk(K, 1, EA)], [                                   // 시동 — 지켜보는 거울
      O([sh(0.8), stk(K, 1, EA)]),                                                    // (수치)
      O([sh(0.6), draw(1, { who: 'other' })]),                                        // 장점 찾기
      O([stk(K, 2, E1), sh(0.6)]),                                                    // 한 사람만 바라보기
      O([sh(1.0), st('피해 감소', 1)]),                                               // 거울 뒤에 숨기
      O([stk(K, 1, EA), draw(1)], { cost: 0 }),                                       // 슬쩍 비추기
    ], null),
    U(`${H}_u5`, '거울처럼 따라 하기', 0, '스킬', [sh(0.3), stk(K, 1, EA), empower(0.3, 'any')], [          // 연료 — 다음 동료 카드를 비춘다
      O([sh(0.3), stk(K, 1, EA), empower(0.3, 'any')]),                               // (수치)
      O([empower(0.45, 'any'), stk(K, 1, E1)]),  // 똑같이(좌우만 반대)
      O([sh(0.5), stk(K, 1, EA)]),                                                    // 모모의 수리검 흉내
      O([sh(0.3), empower(0.3, 'any'), draw(1)]),  // 에슈르의 제빵 흉내
      O([stk(K, 2, EA)]),  // 따라 하며 노려보기
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u4`, '깨진 거울 조각', 1, '공격', [ddef(0.3, E1), per(K, { n: 1 }), ddef(0.18, E1)], [ // 결정타 — 시선이 박힌 만큼 거울이 깨진다
      O([ddef(0.3, E1), per(K, { n: 1 }), ddef(0.18, E1)]),  // (수치)
      O([per(K, { n: 1 }), ddef(0.15, EA)]),                                          // 거울 파편(원작 저학년 폭발)
      O([per(K, { n: 1 }), ddef(0.3, E1), spendAll(K)]),  // 시선을 다 깨기
      O([per(K, { n: 1 }), sh(0.25), spendAll(K)], { cost: 0 }),                       // 거울 방패
      O([per(K, { n: 1 }), ddef(0.3, E1), st('약화', 2, E1)]),  // 눈부신 빛(눈가림)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '이젠 내가 지켜줄 거야', 1, '강화', [st('피해 감소', 1), pw('play', [sh(0.18)], { when: { who: 'other' } })], [ // 원작 자유 — 어사이드(아군 전원 받는 피해↓)
      O([st('피해 감소', 1), pw('play', [sh(0.18)], { when: { who: 'other' } })]),     // (수치)
      O([pw('turnStart', [sh(0.4)])], { cost: 0 }),                                   // 매일 지켜보기
      O([st('피해 감소', 2), pw('hurt', [stk(K, 1, E1)], { when: { guarded: true } })]), // 지켜보고 있어요
      O([pw('play', [sh(0.15), stk(K, 1, E1)], { when: { who: 'other' } })], { cost: 2 }), // 고민 상담사
      O([st('피해 감소', 1), pw('turnStart', [heal(0.2)])], { cost: 0 }),  // 매초 회복(어사이드)
    ], [B('guard'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '수많은 시선', cost: 200, fx: [stk('거울 속', 1), sh(2.5), st('피해 감소', 2), stk(K, 3, EA)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, uniques });
}

// ── 오로라 — 거드는 형 · 탱커 ──
// 엔진: 막아 내고 함께 낼수록 빛울림(방어 +10%/개 — 다섯이면 파티 다음 카드 +50%) · 「긍정 아우라」가 동료의 나쁜 기운을 걷고 빛울림만큼 회복 · 오로라 장막이 턴마다 플라즈마 불꽃
function aurora(j) {
  // 팀: 「다정한 빛마중」 — 세 사도가 모두 카드를 낸 턴, 동료 카드마다 빛울림(혼자가 아니야)
  // 원작: 저학년 「은은한 빛무늬」(긍정의 기운 — 받는 피해↓ · 주는 피해↑ · 위기에 회복) · 고학년 「고요한 빛무리」(오로라 · 20회 낙하 · 받는 피해 중첩) · 다섯 번 맞으면 광역 반격 · 부정 아우라를 긍정으로
  const H = '오로라', K = '빛울림', V = '오로라 장막';
  const basics = [
    S(`${H}_s1`, '오로라 띠', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '극광 장막', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '아우라 읽기', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], [                      // 시동 — 동료의 기운을 읽는다
      O([stk(K, 1), draw(1, { who: 'other' })]),                                      // (수치)
      O([stk(K, 1), cleanse(1)]),                                                     // 자색 아우라 걷기
      O([sh(0.5), draw(1)]),                                                          // 천궁도
      O([stk(K, 2), draw(1, { who: 'other' })]),  // 깊이 읽기
      O([stk(V, 1), draw(1)]),  // 오로라를 부른다
    ], null),
    U(`${H}_u1`, '은은한 빛무늬', 1, '스킬', [sh(0.8), st('피해 감소', 1), dmod(0.15, OTHERS)], [          // 연료 — 원작 저학년(긍정의 기운)
      O([sh(0.8), st('피해 감소', 1), dmod(0.15, OTHERS)]),                            // (수치)
      O([sh(0.6), stk(K, 2)]),                                                        // 빛울림 모으기
      O([heal(0.7), st('피해 감소', 1), stk(K, 1)]),  // 위기의 회복(원작)
      O([sh(0.6), dmod(0.2, OTHERS)], { cost: 0 }),                                   // 긍정의 기운
      O([sh(0.6), pw('lowHp', [heal(1.0)], { when: { pct: 0.3 } })], { power: true }), // 해제되지 않는 기운
    ], [B('guard'), B('heal'), B(BK)]),
    U(`${H}_u4`, '긍정 아우라', 1, '스킬', [cleanse(2), per(K, { n: 1 }), heal(0.2), spendAll(K)], [        // 결정타 — 부정 아우라를 긍정으로
      O([cleanse(2), per(K, { n: 1 }), heal(0.2), spendAll(K)]),                      // (수치)
      O([per(K, { n: 1 }), sh(0.25), spendAll(K)]),                                   // 빛의 방패
      O([cleanse(1), dmod(0.25, ALLY), stk(K, 1)]),                                   // 나쁜 기운을 힘으로
      O([cleanse(2), per(K, { n: 1 }), heal(0.15)]),                                  // 빛울림은 둔다
      O([per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)]),                              // 광역 반격(강화 평타)
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u3`, '플라즈마 불꽃', 1, '공격', [dmg(0.3, EA), stk(V, 1), st('취약', 1, ER)], [                // 원작 자유 — 고학년의 낙하 · 극단적인 긍정(방화)
      O([dmg(0.3, EA), stk(V, 1), st('취약', 1, ER)]),                                // (수치)
      O([dmg(0.25, ER, { hits: 3 }), st('취약', 1, ER)]),                              // 플라즈마 비
      O([dmg(0.5, E1), stk(K, 1)]),                                                   // 꺼지지 않는 불꽃
      O([stk(V, 2), stk(K, 1)], { cost: 1 }),  // 거대 오로라
      O([dmg(0.3, EA), heal(0.3), stk(K, 1)]),  // 다정한 불씨
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '고요한 빛무리', cost: 250, fx: [stk(V, 3), sh(3.0), st('실드 보존', 1), draw(2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, uniques });
}

// ── 스키아 — 아껴 두기형 · 딜러 ──
// 엔진: AP 를 남기고 턴을 마칠 때마다(묵언) 삼킨 감탄사(주는 피해 +15%/개) — 넷이면 다음 카드 +50% → 「흐읍!?」으로 터뜨리면 감탄사만큼 광역 · 쓸 때마다 카드 1장
function skia(j) {
  // 팀: 고학년의 언약의 매듭 — 매듭이 있을 때 고학년을 다시 쓰면 매듭이 터져 적 전체(파티의 고학년 순서를 거든다)
  // 원작: 묵언수행 · 저학년 「오래된 맹세」(언약의 매듭 · 다시 쓰면 터져 침묵) · 고학년 「고대 사제의 기도」(가장 뒤 적 · 매듭 폭발 기절) · 강화 평타(딜러 아군 스킬 피해↑) · 새어 나오는 감탄사
  const H = '스키아', K = '삼킨 감탄사';
  const basics = [
    S(`${H}_s1`, '깃털 날리기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '천벌 벼락', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '사제의 장막', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '…(끄덕)', 0, '스킬', [stk(K, 1), draw(1)], [                                         // 시동 — 몸짓이라 묵언이 안 깨진다
      O([stk(K, 1), draw(1)]),                                                        // (수치)
      O([sh(0.5), draw(1)]),                                                          // 고개 끄덕
      O([stk(K, 2)]),  // 깊은 묵언
      O([stk(K, 1), dmod(0.15, OTHERS)]),                                             // 딜러 아군 스킬 피해↑(원작)
      O([draw(2)]),  // 몸짓 대화
    ], null, ['보존']),
    U(`${H}_u1`, '오래된 맹세', 1, '공격', [dmg(0.7, E1), later(1, [dmg(0.35, EA)]), stk(K, 1)], [         // 연료 — 원작 저학년(언약의 매듭)
      O([dmg(0.7, E1), later(1, [dmg(0.35, EA)]), stk(K, 1)]),                         // (수치)
      O([dmg(0.9, E1), stk(K, 1)]),                                                   // 그냥 벼락
      O([later(1, [dmg(0.5, EA), st('약화', 1, EA)])], { cost: 0 }),                   // 매듭만 맺기(침묵)
      O([dmg(0.6, E1), later(1, [dmg(0.3, EA)]), later(2, [dmg(0.3, EA)])]),           // 길어진 매듭(어사이드)
      O([dmg(0.7, E1), stk(K, 2)]),                                                   // 삼킨 맹세
    ], [B('power'), B('weakSpot'), B(BK)], ['보존']),
    U(`${H}_u3`, '흐읍!?', 1, '공격', [per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)], [     // 결정타 — 새어 나온 감탄사
      O([per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)]),  // 한 사람에게 들킴
      O([per(K, { n: 1 }), dmg(0.3, EA)]),  // 반만 새어 나옴
      O([per(K, { n: 1 }), dmg(0.28, EA), st('기절', 1, E1)], { cost: 2 }),  // 꿈인 줄 알고(멘붕)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)], { cost: 0 }),                  // 짧은 비명
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '지켜 온 금기', 1, '공격', [dmg(0.9, E1), per(K, { n: 1 }), dmg(0.25, E1)], [             // 원작 자유 — 묻어 둔 두 금기
      O([dmg(0.9, E1), per(K, { n: 1 }), dmg(0.25, E1)]),                              // (수치)
      O([dmg(0.8, FAR_ENEMY), per(K, { n: 1 }), dmg(0.2, FAR_ENEMY)]),                 // 가장 뒤 적에게(고학년)
      O([dmg(1.0, E1), stk(K, 1)]),                                                    // 금기를 삼키기
      O([dmg(0.8, E1), st('취약', 2, E1)]),                                            // 니펠의 영역
      O([per(K, { n: 1 }), dmg(0.35, E1)], { cost: 0 }),                               // 입 다문 일격
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '고대 사제의 기도', cost: 200, fx: [dmg(0.25, FAR_ENEMY, { hits: 3 }), st('기절', 1, FAR_ENEMY), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`], basics, uniques });
}
const FAR_ENEMY = 'farEnemy';

// ── 아라그니아 — 아껴 두기형 · 서포터 ──
// 엔진: 진주 구슬을 만들어 두면 아군이 맞을 때마다 하나씩 날아가 회복 → 다 내주지 않고 남긴 진주를 「남은 구슬은 짐의 것」으로 삼켜 실드 · AP 를 남기면 남은 진주만큼 실드
function aragnia(j) {
  // 팀: 「여왕은 백성을 버리지 않느니라」 — 아군이 맞으면 진주가 날아가 회복(얼마나 나눠 주고 얼마나 남길까)
  // 원작: 저학년 「마음속의 펄」(진주 6개 · HP 80% 미만 아군 회복 · 끝나면 남은 진주를 삼켜 보호막 + SP) · 고학년 「파도 치는 피날레」(적 셋 · 약화 · 보호막 파괴 · 아군 최대 HP↑) · 둘째 평타마다 회복 + 피해 · 놓아주는 보석
  const H = '아라그니아', K = '진주 구슬';
  const basics = [
    S(`${H}_s1`, '최강 꼬리치기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '짐의 하사품', 1, '스킬', [sh(1.5)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '마음속의 펄', 1, '스킬', [stk(K, 3), sh(0.6)], [                                        // 시동 — 원작 저학년(진주 생성)
      O([stk(K, 3), sh(0.6)]),                                                        // (수치)
      O([stk(K, 2), heal(0.5)]),                                                      // 바로 하나 보내기
      O([stk(K, 4), sh(0.5)]),  // 진주 아홉(어사이드)
      O([stk(K, 2), st('피해 감소', 1)]),                                             // 진주가 지켜 준다(어사이드)
      O([stk(K, 2), draw(1)], { cost: 0 }),                                           // 산호 한 입
    ], null, ['보존']),
    U(`${H}_u2`, '남은 구슬은 짐의 것', 1, '스킬', [per(K, { n: 1 }), sh(0.35), spendAll(K), draw(1)], [   // 결정타 — 남긴 진주를 삼킨다
      O([per(K, { n: 1 }), sh(0.35), spendAll(K), draw(1)]),                          // (수치)
      O([per(K, { n: 1 }), heal(0.25), spendAll(K), draw(1)]),  // 백성에게 다 나눔
      O([per(K, { n: 1 }), sh(0.3), spendAll(K), gauge(20)], { cost: 0 }),  // 삼키고 SP(원작)
      O([per(K, { n: 1 }), sh(0.3), draw(1)]),  // 삼키지 않고 바라보기
      O([per(K, { n: 1 }), dmg(0.2, EA), spendAll(K)]),                               // 진주 탄환
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u3`, '파도 한 줄기', 1, '공격', [dmg(0.55, EA), st('약화', 1, EA), stk(K, 1)], [                 // 연료 — 고학년의 물기둥(주는 피해↓)
      O([dmg(0.55, EA), st('약화', 1, EA), stk(K, 1)]),                               // (수치)
      O([{ k: 'strip', target: E1 }, dmg(0.5, EA), stk(K, 1)]),  // 보호막 하나 깨기(원작)
      O([dmg(0.4, EA), heal(0.4), stk(K, 1)]),  // 둘째 물방울은 회복
      O([dmg(0.45, EA), stk(K, 2)]),                                                  // 해류 붙잡기
      O([dmg(0.3, EA), st('약화', 2, EA)], { cost: 0 }),                              // 잔물결
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u4`, '진주 왕국의 보물고', 1, '스킬', [sh(0.5), per(K, { n: 1 }), sh(0.12)], [                 // 원작 자유 — 인구 1명의 진주 왕국
      O([sh(0.5), per(K, { n: 1 }), sh(0.12)]),                                       // (수치)
      O([sh(0.4), stk(K, 2)]),                                                        // 보물고 열기
      O([per(K, { n: 1 }), heal(0.15), stk(K, 1)]),  // 백성 구휼
      O([sh(0.6), per(K, { n: 1 }), sh(0.1), st('피해 감소', 1)]),                    // 영겁의 섬
      O([sh(0.4), pw('keepAp', [stk(K, 1)])], { power: true }),                       // 모래섬 한 줌씩
    ], [B('guard'), B('heal'), B(BK)], ['보존']),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '파도 치는 피날레', cost: 200, fx: [dmg(0.45, ER, { hits: 3 }), st('약화', 2, EA), stk(K, 3)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, uniques });
}

export const JOBS = [['마녀/아야', aya], ['수인/슈로', shuro], ['수인/우로스', uros], ['엘프/로네_시장', roneMayor], ['요정/폴랑', polang], ['용족/실비아', sylvia],
  ['용족/아네트', annette], ['유령/키샤', kisha], ['정령/미로', miro], ['정령/오로라', aurora], ['요정/스키아', skia], ['용족/아라그니아', aragnia]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
