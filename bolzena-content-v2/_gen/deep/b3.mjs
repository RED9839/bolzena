// 묶음 3 — 박자형 9 + 격파형 3. node _gen/deep/b3.mjs [사도 일부]
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, OTHERS, ALLIES, dmg, ddef, sh, heal, st, stk, spendAll, spendN, per, draw, make, ap, gauge, ifStack, ifKill, ifBreak, ifFoe,
  empower, tough, dmod, cost, pw, pas, O, B, U, S, T, hero } = L;
const ifNth = n => ({ k: 'ifNth', n });
const ifStreak = (n, type) => ({ k: 'ifStreak', n, ...(type ? { type } : {}) });
const ifTypeChange = { k: 'ifPrevSame', not: true };   // 바로 앞 카드와 종류가 다르면
const ifPricier = { k: 'ifPricier' }, notPricier = { k: 'ifPricier', not: true };
const perPlayed = { k: 'perPlayed' }, perDebuff = { k: 'perDebuff' };
const crit = { k: 'critMod', v: 1, target: 'self' };
const atk = v => ({ k: 'atkMod', v, target: 'self' });
const cleanse = v => ({ k: 'cleanse', v });
const strip = t => ({ k: 'strip', target: t });

// ── 아사나 — 박자형 · 탱커 ──
// 엔진: 아사나가 자세를 보이고(시범 자세) 바로 뒤에 동료가 카드를 내면 따라 한 동작 = 마력 호흡 1 → 셋째 동작에 「에너지파」 1장(원작 세 번째 발사)
function asana(j) {
  // 팀: 마력 호흡 1개당 파티 주는 피해 +6% · 동료 카드가 껴야 박자가 선다(남이 껴야 서는 유일한 박자)
  // 원작: 심마체 이론 · 볼-요가 · 저학년 「마력 분출」(보호막 + 세 번째마다 기절) · 고학년 「명상 시간」(6회 범위 + 아군 회복) · 강화 평타 에너지파 · 자동 요가 세뇌 파장 분사기
  const H = '아사나', K = '마력 호흡', P = '시범 자세', EW = `${H}_wave`;
  const basics = [
    S(`${H}_s1`, '진자 휘두르기', 1, '공격', [dmg(0.33, E1, { hits: 2 })]),
    S(`${H}_s2`, '나무 자세', 1, '스킬', [sh(1.7)]),
  ];
  const tokens = [T(EW, '에너지파', H, '공격', [ddef(0.7, E1), sh(0.5)], { tags: ['소멸'], blurb: '세 동작을 따라 한 끝에 나가는 에너지파입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '볼-요가 시간', 1, '강화', [sh(0.6), stk(K, 1), pw('turnStart', [draw(1, { who: 'other' })])], [ // 시동 — 매일 아침 동료를 끌어내는 요가
      O([sh(0.6), stk(K, 1), pw('turnStart', [draw(1, { who: 'other' })])]),        // (수치)
      O([stk(K, 1), pw('turnStart', [sh(0.3)])], { cost: 0 }),                      // 얼차려 자세
      O([stk(K, 1), pw('play', [sh(0.15)], { when: { who: 'other' } })], { cost: 0 }),  // 따라 하면 칭찬
      O([stk(K, 2), pw('turnStart', [stk(P, 1)])]),                                 // 아침 시범
      O([sh(0.5), pw('turnStart', [draw(1, { who: 'other' }), heal(0.2)])]),         // 건강한 몸에 건강한 정신
    ], null, ['개전']),
    U(`${H}_u2`, '마력 분출', 1, '공격', [sh(0.6), ddef(0.6, E1), ifStack(K, 2), st('기절', 1, E1)], [ // 연료 — 원작 저학년(보호막 + 셋째에 기절)
      O([sh(0.6), ddef(0.6, E1), ifStack(K, 2), st('기절', 1, E1)]),                 // (수치)
      O([sh(0.5), ddef(0.4, EA), stk(P, 1)]),  // 뒤로도 쏘기(어사이드)
      O([sh(0.8), ddef(0.5, E1), stk(P, 1)]),                                       // 다시 시범
      O([ddef(0.8, E1), stk(K, 1)]),                                                // 힘껏 분출
      O([ddef(0.6, E1), dmod(0.2, OTHERS), stk(P, 1)]),  // 스킬 피해 증가(어사이드)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '요가 세뇌 파장', 2, '공격', [per(K, { n: 1 }), ddef(0.2, EA), heal(0.5)], [ // 결정타 — 자동 요가 세뇌 파장 분사기
      O([per(K, { n: 1 }), ddef(0.2, EA), heal(0.5)]),  // (수치)
      O([per(K, { n: 1 }), ddef(0.4, E1), heal(0.4)]),  // 한 사람만 세뇌
      O([per(K, { n: 1 }), heal(0.35), spendAll(K)], { cost: 0 }),  // 명상만
      O([per(K, { n: 1 }), ddef(0.3, EA), spendAll(K)]),  // 다 내쉬기
      O([per(K, { n: 1 }), ddef(0.18, EA), st('둔화', 1, EA)]),  // 몸이 굳는다
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '24시간 밀착 감시', 1, '스킬', [sh(0.6), dmod(0.25, 'strongestAlly'), draw(1, { who: 'other' })], [ // 원작 자유 — 어사이드(밀착 감시)
      O([sh(0.6), dmod(0.25, 'strongestAlly'), draw(1, { who: 'other' })]),          // (수치)
      O([sh(0.5), draw(2, { who: 'other' })]),                                      // 다 같이 스트레칭
      O([dmod(0.3, 'strongestAlly'), stk(P, 1)], { cost: 0 }),                      // 바로 옆에서
      O([sh(0.9), stk(K, 1)]),                                                      // 감시하며 호흡
      O([sh(0.5), pw('turnStart', [dmod(0.15, 'strongestAlly')])], { cost: 0, power: true }),  // 24시간
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '따라 한 동작으로 도는 마력', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.06, who: 'allies' }], onMax: { make: EW, consume: true } },
    keywords: [{ name: P, desc: '아사나가 보인 자세', carrier: 'self', cap: 1 }],
    passives: [
      pas('심마체', 'play', [stk(P, 1)]),
      pas('심마체', 'play', [ifStack(P), spendAll(P), stk(K, 1), sh(0.15)], { when: { who: 'other' } }),
      pas('건강한 몸에 건강한 정신', 'turnStart', [heal(0.25)]),
    ],
    ult: { name: '명상 시간', cost: 250, fx: [ddef(0.25, EA, { hits: 3 }), heal(1.5), st('피해 감소', 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, tokens, uniques,
  });
}

// ── 티그 — 박자형 · 딜러 ──
// 엔진: 제 공격만 끊김 없이 이으면 장작 패기(주는 피해 +5%/개) — 스킬이나 동료 카드가 끼면 0(마이웨이) → 셋째 칼(장작 패기 3)부터 공격마다 적 전체 덤
function tig(j) {
  // 팀: 「차기 촌장 티그」 — 파티 받는 피해 -10%(남에겐 무심해도 마을은 지킨다)
  // 원작: 쌍검 2회(둘째가 더 셈) · 셋째 공격마다 내려치고 휘두르기(범위 + SP) · 저학년 「소닉 블레이드」 · 고학년 「오버드라이브」 · 디아나의 수제자 · 마이웨이
  const H = '티그', K = '장작 패기';
  const basics = [
    S(`${H}_s1`, '쌍검 베기', 1, '공격', [dmg(0.45, E1), dmg(0.6, E1)]),
    S(`${H}_s2`, '쌍검 십자베기', 2, '공격', [dmg(2.1, E1)]),
    S(`${H}_s3`, '받아쳐!', 1, '스킬', [sh(1.5)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '쌍검 휘두르기', 0, '공격', [dmg(0.2, E1), dmg(0.28, E1), draw(1)], [            // 시동 — 둘째 타격이 더 센 평타
      O([dmg(0.2, E1), dmg(0.28, E1), draw(1)]),                                    // (수치)
      O([dmg(0.25, E1), dmg(0.35, E1)]),                                            // 그냥 베기
      O([dmg(0.2, E1), stk(K, 1), draw(1)]),                                        // 장작 하나 더
      O([dmg(0.15, EA), dmg(0.2, E1), draw(1)]),                                    // 휘감기
      O([dmg(0.3, E1), ifStreak(2, '공격'), draw(2)]),                              // 끊김 없이 다음 칼
    ], null),
    U(`${H}_u1`, '소닉 블레이드', 1, '공격', [dmg(0.5, EA), ifStreak(2, '공격'), dmg(0.3, EA)], [ // 연료 — 원작 저학년(돌진 범위)
      O([dmg(0.5, EA), ifStreak(2, '공격'), dmg(0.3, EA)]),                         // (수치)
      O([dmg(0.9, E1), ifStreak(2, '공격'), dmg(0.5, E1)]),                         // 한 놈만 관통
      O([dmg(0.45, EA), stk(K, 1)]),                                                // 돌진하며 장작
      O([dmg(0.4, EA), ifStreak(2, '공격'), draw(1)]),                              // 제자리로 돌아와 다음 칼
      O([dmg(0.55, EA), tough(1)], { cost: 2 }),                                    // 전력 돌진(격파)
    ], [B('power'), B('weakSpot'), B(BK)], ['신속']),
    U(`${H}_u3`, '내려치고 휘두르기', 1, '공격', [dmg(0.6, E1), per(K, { n: 1 }), dmg(0.22, EA), ifStreak(3, '공격'), ap(1)], [ // 결정타 — 셋째 공격마다(원작 강화 평타)
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.22, EA), ifStreak(3, '공격'), ap(1)]), // (수치)
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.35, E1)]),                            // 내려치기만
      O([per(K, { n: 1 }), dmg(0.3, EA), ifStreak(3, '공격'), draw(1)]),             // 휘두르기만
      O([per(K, { n: 1 }), dmg(0.28, EA), spendAll(K), ap(1)]),  // 장작을 다 패고(AP 회수)
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.2, EA), tough(1)]),                   // 무게 실은 칼(격파)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '백호 비전서', 1, '강화', [atk(0.12), pw('turnStart', [stk(K, 1)])], [            // 원작 자유 — 디아나의 수제자 · 백호 검성
      O([atk(0.12), pw('turnStart', [stk(K, 1)])]),                                  // (수치)
      O([atk(0.1), pw('kill', [stk(K, 2)])]),                                        // 막타 가로채기(TRPG)
      O([atk(0.06), pw('play', [dmg(0.15, ER)], { when: { type: '공격' } })], { cost: 0 }),  // 사료스탕스 대장
      O([atk(0.1), pw('turnStart', [stk(K, 2)])]),  // 힘만 찍은 전사
      O([atk(0.08), pw('turnStart', [stk(K, 1), draw(1)])], { cost: 2 }),            // 장작 패며 자란 아침
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '끊김 없이 이어 휘두른 칼', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.05 }] },
    passives: [
      pas('마이웨이', 'play', [stk(K, 1)], { when: { type: '공격' } }),
      pas('마이웨이', 'play', [spendAll(K)], { when: { type: '스킬' } }),
      pas('셋째 칼', 'play', [ifStack(K, 3), dmg(0.22, EA)], { when: { type: '공격' } }),
    ],
    ult: { name: '오버드라이브', cost: 150, fx: [dmg(1.0, EA), atk(0.15), stk(K, 3)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 리스티 — 박자형 · 딜러 ──
// 엔진: 공격마다 글러브가 개인정보를 긁어 셋이면 「오늘의 POTG」 1장 · 이번 턴 셋째 카드로 낸 공격은 확정 치명(원작 강화 공격)
function risty(j) {
  // 팀: 「리스티의 슈퍼 세이브」 — POTG 를 띄우면 파티 다음 카드 +20%
  // 원작: 저학년 「테크노맨시」(HP 가장 높은 적 · 안 죽으면 다시) · 고학년 「복셀 글리치」 · 셋째(어사이드) 공격마다 확정 치명 · 리그 오브 엘프 · 다 마신 캔 · 글러브 AI
  const H = '리스티', K = '개인정보', CAN = `${H}_can`, POTG = `${H}_potg`;
  const basics = [
    S(`${H}_s1`, '키보드 연타', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '키보드 샷건', 2, '공격', [dmg(2.3, E1)]),
    S(`${H}_s3`, '방구석 바리케이드', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [
    T(CAN, '빈 캔', H, '공격', [dmg(0.3, ER)], { tags: ['소멸'], blurb: '다 마신 독타 페퍼 캔입니다' }),
    T(POTG, '오늘의 POTG', H, '공격', [crit, dmg(0.8, TOP), empower(0.2, 'any')], { tags: ['소멸'], blurb: '시즌 최고의 한 장면입니다' }),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '훗, 못 깨는 게임은 없지', 0, '공격', [dmg(0.35, E1), draw(1)], [               // 시동 — 0코로 셋째 자리를 만든다
      O([dmg(0.35, E1), draw(1)]),                                                   // (수치)
      O([dmg(0.3, E1), make(CAN, 1)]),                                               // 캔 하나 더
      O([dmg(0.25, TOP), stk(K, 1)]),                                                // 랭커 정보
      O([draw(2)]),  // 공략 영상
      O([dmg(0.3, E1), ifNth(2), draw(1)]),                                          // 템포 조절
    ], null),
    U(`${H}_u1`, '테크노맨시', 2, '공격', [dmg(0.6, E1), per(K, { n: 1 }), dmg(0.35, E1), spendAll(K)], [ // 결정타 — 원작 저학년(HP 가장 높은 적, 다시 찾아)
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.35, E1), spendAll(K)]),  // (수치)
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.3, ER)]),  // 다시 찾아 치기
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.3, E1)]),  // 정보는 남긴다
      O([crit, dmg(0.6, E1), per(K, { n: 1 }), dmg(0.3, E1)], { cost: 3 }),  // 확정 치명 해킹
      O([per(K, { n: 1 }), dmg(0.4, TOP), ifKill, make(POTG, 1)]),  // 처치하면 POTG
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '다 마신 캔', 1, '공격', [dmg(0.6, E1), make(CAN, 1)], [                       // 연료 — 원작 평타(캔 던지기)
      O([dmg(0.6, E1), make(CAN, 1)]),                                               // (수치)
      O([dmg(0.4, E1), make(CAN, 2)]),  // 한 박스 다
      O([dmg(0.7, E1), stk(K, 1)]),                                                  // 캔에 해킹 칩
      O([dmg(0.4, EA), stk(K, 1)]),  // 흩뿌리기
      O([make(CAN, 2), draw(1)], { cost: 0 }),                                       // 독타 페퍼 한 박스
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u4`, '개인정보 수집', 1, '공격', [dmg(0.7, E1), ifNth(3), crit, dmg(0.7, E1)], [       // 원작 자유 — 셋째 공격마다 확정 치명
      O([dmg(0.7, E1), ifNth(3), crit, dmg(0.7, E1)]),                               // (수치)
      O([dmg(0.6, TOP), ifNth(3), crit, dmg(0.7, TOP)]),                             // 랭커 저격
      O([dmg(0.6, E1), stk(K, 1), ifNth(3), stk(K, 2)]),  // 정보만 빼 오기
      O([crit, dmg(0.9, E1)], { cost: 2 }),                                          // 언제나 치명
      O([dmg(0.6, E1), ifNth(3), crit, dmg(0.5, EA)]),                               // 복셀 글리치 맛보기
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '글러브가 긁어 온 적의 정보', carrier: 'self', cap: 3, onMax: { make: POTG, consume: true } },
    passives: [
      pas('글러브 해킹', 'play', [stk(K, 1)], { when: { type: '공격', minCost: 1 } }),
      pas('천재 해커의 등장', 'make', [gauge(5)]),
    ],
    ult: { name: '복셀 글리치', cost: 150, fx: [dmg(0.25, LOW, { hits: 3 }), st('기절', 1, LOW), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '예열된 손가락', when: { on: 'fightStart' }, fx: [stk(K, 1)] }];
}

// ── 캐시 — 박자형 · 딜러 ──
// 엔진: 겁나서 먼저 지진다 — 테이저가 적마다 다른 고장(취약 · 약화 · 둔화)을 남기고 → 「301일의 동굴」이 그 적의 해로운 효과 가짓수만큼 몰아친다 · 공격 셋째마다 과충전
function cash(j) {
  // 팀: 「캐시가방에들어가신다」 — 히익 전류가 차면 파티 다음 카드 +50%(겁먹은 캐시가 먼저 지져 길을 낸다)
  // 원작: 저학년 「바삭 지짐이」(최대 3명 연쇄 · 받는 스킬 피해↑) · 세 번째 공격마다 과충전(범위) · 고학년 「점프 스케어」 · 어사이드 불가사의한 현상(공속 · SP 깎기) · 유령 무서워하는 정찰병
  const H = '캐시', K = '히익 전류';
  const basics = [
    S(`${H}_s1`, '테이저 찌르기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '풀파워 테이저', 2, '공격', [dmg(2.1, E1)]),
    S(`${H}_s3`, '숨기', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '바삭 지짐이', 1, '공격', [dmg(0.3, ER, { hits: 3 }), st('취약', 1, EA), stk(K, 1)], [ // 연료 — 원작 저학년(3명 연쇄 · 받는 피해↑)
      O([dmg(0.3, ER, { hits: 3 }), st('취약', 1, EA), stk(K, 1)]),                  // (수치)
      O([dmg(0.7, E1), st('취약', 2, E1), stk(K, 1)]),  // 혼자 남은 적(연쇄 없이 더 세게)
      O([dmg(0.3, ER, { hits: 3 }), st('약화', 1, EA)]),                              // 겁먹어 마구 지지기
      O([dmg(0.25, ER, { hits: 3 }), st('약점 노출', 1, ER), stk(K, 1)]),  // 감전 — 약점 노출(받는 스킬 피해↑ 원작)
      O([dmg(0.3, ER, { hits: 3 }), draw(1)], { cost: 0 }),                           // 반사적으로
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '더플백 은신', 1, '스킬', [sh(0.8), st('회피', 1), stk(K, 1)], [                 // 시동 — 가방에 숨어 먼저 지질 틈
      O([sh(0.8), st('회피', 1), stk(K, 1)]),                                         // (수치)
      O([sh(0.6), st('반격', 2)]),                                                    // 들키면 지진다
      O([stk(K, 2), draw(1)], { cost: 0 }),                                           // 가방 속 충전
      O([sh(0.6), st('약화', 1, EA), stk(K, 1)]),  // 「공포의 유령」 위장
      O([sh(0.6), pw('hurt', [st('약화', 1, E1)])], { cost: 0, power: true }),  // 301일 겁쟁이
    ], null),
    U(`${H}_u4`, '301일의 동굴', 1, '공격', [perDebuff, dmg(0.3, E1), stk(K, 1)], [               // 결정타 — 남긴 고장 가짓수만큼
      O([perDebuff, dmg(0.3, E1), stk(K, 1)]),                                        // (수치)
      O([perDebuff, dmg(0.22, EA)]),                                                  // 동굴 전체에
      O([dmg(0.4, E1), perDebuff, dmg(0.2, E1)]),                                     // 먼저 한 방 · 고장마다 더
      O([perDebuff, dmg(0.28, E1), st('취약', 1, E1)]),                               // 고장 하나 더
      O([perDebuff, dmg(0.25, E1), draw(1)]),                                         // 틀어박혀 다음 수
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '불가사의한 현상', 1, '공격', [dmg(0.4, EA), st('둔화', 1, EA), st('약화', 1, EA)], [ // 원작 자유 — 어사이드(공격 속도 · SP 깎기)
      O([dmg(0.4, EA), st('둔화', 1, EA), st('약화', 1, EA)]),                         // (수치)
      O([dmg(0.35, EA), st('둔화', 2, EA), stk(K, 1)]),  // 시간이 멈춘 늪
      O([dmg(0.8, E1), st('약화', 2, E1)]),                                            // 한 놈에게 현상
      O([dmg(0.3, EA), st('약화', 1, EA), stk(K, 1)]),                                 // 히익!
      O([st('둔화', 1, EA), st('약화', 1, EA)], { cost: 0 }),                          // 유령 소동
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '겁나서 먼저 지지는 전류', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.3, consume: true } },
    passives: [
      pas('히익!', 'hurt', [stk(K, 1)]),
      pas('과충전 테이저', 'play', [dmg(0.35, EA)], { when: { type: '공격', every: 3 } }),
    ],
    ult: { name: '점프 스케어', cost: 150, fx: [dmg(1.0, EA), st('둔화', 1, EA), st('회피', 1), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 리코타 — 박자형 · 탱커 ──
// 엔진: 직전 카드보다 비싼 카드(0 → 1 → 2, 누구 것이든)가 나오면 코스 +1, 같거나 싸면 처음으로 → 셋째 접시 「리코타 풀코스」 · 이번 턴 넷째 카드마다 웍질
function ricotta(j) {
  // 팀: 「똑같이 본뜬 요리」로 동료 카드 하나를 한 단계 무겁게 — 동료의 카드로 코스를 잇는다
  // 원작: 강화 평타(넷째 공격마다 웍질 · 범위 + 회복) · 저학년 「리코타 풀코스」(HP 낮은 아군 회복 · 센 아군 스킬 피해↑) · 고학년 「준비만전」(4회 · 대상 최대 HP 가 크면 더) · 코스 요리 고집 · 똑같이 본뜬 음식
  const H = '리코타', K = '코스';
  const basics = [
    S(`${H}_s1`, '주방칼 썰기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '냄비 뚜껑', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '아뮤즈 부쉬', 0, '스킬', [sh(0.5), draw(1), ifNth(1), stk(K, 1)], [              // 시동 — 코스의 첫 접시(0)
      O([sh(0.5), draw(1), ifNth(1), stk(K, 1)]),                                     // (수치)
      O([sh(0.4), draw(2)]),                                                          // 입맛 돋우기
      O([heal(0.4), draw(1), ifNth(1), stk(K, 1)]),  // 따뜻한 수프
      O([sh(0.4), draw(1), cost(-1)]),                                                // 다음 접시 준비
      O([sh(0.6), stk(K, 1), draw(1)]),  // 두 번째 접시부터
    ], null),
    U(`${H}_u3`, '리코타 풀코스', 2, '스킬', [per(K, { n: 1 }), heal(0.35), empower(0.4, 'any')], [ // 결정타 — 원작 저학년(회복 · 센 아군 피해↑)
      O([per(K, { n: 1 }), heal(0.35), empower(0.4, 'any')]),  // (수치)
      O([per(K, { n: 1 }), sh(0.45), st('피해 감소', 1)]),  // 받는 피해 감소(원작)
      O([heal(0.5), dmod(0.25, 'strongestAlly')]),  // 센 아군 둘에게
      O([per(K, { n: 1 }), heal(0.3), spendAll(K), draw(1)]),  // 코스를 끝내고 다음 수
      O([per(K, { n: 1 }), heal(0.25), cleanse(2)], { cost: 1 }),  // 퐁듀 분수(해제)
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u5`, '똑같이 본뜬 요리', 1, '스킬', [sh(0.6), cost(1), empower(0.6, 'any')], [          // 연료 — 무거운 접시를 만들어 준다
      O([sh(0.6), cost(1), empower(0.6, 'any')]),                                     // (수치)
      O([sh(0.5), cost(1), draw(1)]),                                                 // 찐빵 머리
      O([cost(1), empower(0.8, 'any')], { cost: 0 }),                                 // 먼지떨이 솜사탕
      O([sh(0.9), stk(K, 1)]),                                                        // 그대로 한 접시
      O([sh(0.5), cost(1), dmod(0.15, OTHERS)], { cost: 0 }),  // 다 같이 먹자
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u2`, '다 조려버리겠습니다', 2, '공격', [ddef(0.4, E1, { hits: 2 }), ifPricier, ddef(0.5, E1), tough(1)], [ // 원작 자유 — 고학년의 손질 · 파인애플 피자 화형식
      O([ddef(0.4, E1, { hits: 2 }), ifPricier, ddef(0.5, E1), tough(1)]),             // (수치)
      O([ddef(0.3, EA, { hits: 2 }), ifPricier, ddef(0.3, EA)]),                       // 큰 냄비
      O([ddef(0.45, TOP, { hits: 2 }), ifPricier, ddef(0.6, TOP)]),                    // 큰 놈일수록(준비만전)
      O([ddef(0.4, E1, { hits: 2 }), ifPricier, heal(0.5)]),                           // 시식
      O([ddef(0.5, E1), tough(2)], { cost: 1 }),                                       // 불태우기(격파)
    ], [B('power'), B('weakSpot'), B(BK)], ['분쇄']),
  ];
  hero(j, {
    // 코스가 오르내리는 규칙은 고유 효과 엔진(rules)으로 — 패시브는 웍질 한 줄(10-10 패시브 1~2줄)
    keyword: { name: K, desc: '가벼운 접시에서 무거운 접시로', carrier: 'self', cap: 3, per: [{ stat: 'guard', v: 0.05 }],
      rules: [pas('다음 접시', 'play', [ifPricier, stk(K, 1)], { when: { who: 'any' } }), pas('처음 접시로', 'play', [notPricier, spendN(K, 1)], { when: { who: 'any' } })] },
    passives: [
      pas('웍질', 'play', [ddef(0.25, EA), heal(0.3)], { when: { who: 'any', nth: 4 } }),
    ],
    ult: { name: '준비만전', cost: 200, fx: [ddef(0.3, TOP, { hits: 4 }), { k: 'drain', ratio: 0.3 }, sh(1.0)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 슈팡 — 박자형 · 서포터 ──
// 엔진: 한 턴에 몰아 낸 장수(누구 것이든)가 배달 건수 — 0코 「신속」 카드로 장수를 벌고 → 넷째 · 다섯째 장마다 우편을 흘려 회복 · 배달 속도가 차면 파티 다음 카드 +30%
function shupang(j) {
  // 팀: 넷째 · 다섯째로 나온 카드(누구 것이든)마다 HP 낮은 아군 회복 — 파티가 몰아칠수록 배달이 쌓인다
  // 원작: 저학년 「무책임 배달부」(2회 왕복 · 흘릴 때마다 회복) · 고학년 「슈팡 배송」(받는 피해↓ · 소음) · 강화 평타(둘째 공격 대신 회복) · 「느린 세상」 을 못 참는 속도 중독
  const H = '슈팡', K = '배달 속도', PC = `${H}_parcel`;
  const basics = [
    S(`${H}_s1`, '소포 투척', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '구급 소포', 1, '스킬', [heal(1.5)]),
  ];
  const tokens = [T(PC, '택배 상자', H, '공격', [dmg(0.25, ER), heal(0.15)], { tags: ['신속', '소멸'], blurb: '아무 데나 던진 택배 상자입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u5`, '야, 타!', 0, '스킬', [make(PC, 1), stk(K, 1)], [                                   // 시동 — 장수를 버는 0코
      O([make(PC, 1), stk(K, 1)]),                                                    // (수치)
      O([make(PC, 1), draw(1)]),                                                      // 바나도 태우기
      O([heal(0.4), stk(K, 1), draw(1)]),  // 구급 배달
      O([make(PC, 2)]),  // 짐칸 가득
      O([stk(K, 2), draw(1)]),  // 슈파볼트 시동
    ], null, ['신속']),
    U(`${H}_u1`, '무책임 배달부', 1, '스킬', [perPlayed, heal(0.15), stk(K, 1)], [                  // 결정타 — 원작 저학년(흘릴 때마다 회복)
      O([perPlayed, heal(0.15), stk(K, 1)]),                                          // (수치)
      O([perPlayed, sh(0.2), stk(K, 1)]),  // 포장 뽁뽁이
      O([perPlayed, heal(0.12), draw(1)]),                                            // 왕복 두 번
      O([perPlayed, dmg(0.15, ER), stk(K, 1)]),  // 파손 민원
      O([per(K, { n: 1 }), heal(0.3), spendAll(K)]),                                  // 속도를 다 쓰고
    ], [B('heal'), B('cost'), B(BK)], ['신속']),
    U(`${H}_u2`, '슈팡은⋯ 달리고 싶다!', 0, '스킬', [draw(1), ifNth(4), ap(1)], [                   // 연료 — 넷째 장이면 AP
      O([draw(1), ifNth(4), ap(1)]),                                                  // (수치 — 드로우)
      O([stk(K, 1), ifNth(4), ap(1)]),                                                // 과속 딱지
      O([draw(2), ifNth(5), stk(K, 2)]),  // 다섯째 장의 질주
      O([make(PC, 1), ifNth(4), ap(1)]),                                              // 달리며 던지기
      O([draw(2)]),  // 관성 드리프트
    ], [B('draw'), B('ap'), B(BK)], ['신속']),
    U(`${H}_u3`, '케이크 위 코너링', 1, '공격', [dmg(0.25, EA), perPlayed, dmg(0.08, EA)], [          // 원작 자유 — 케이크 위 제자리 150회 스핀
      O([dmg(0.25, EA), perPlayed, dmg(0.08, EA)]),                                   // (수치)
      O([dmg(0.2, EA), ifNth(5), st('약화', 1, EA)]),                                 // 소음(원작 고학년)
      O([perPlayed, dmg(0.15, E1), stk(K, 1)]),  // 한 집에만 배달
      O([dmg(0.3, EA), perPlayed, heal(0.1)]),  // 흘린 우편
      O([dmg(0.3, EA), stk(K, 1)], { cost: 0 }),                                      // 쌍손가락
    ], [B('power'), B('weakSpot'), B(BK)], ['신속']),
  ];
  hero(j, {
    keyword: { name: K, desc: '멈출 수 없는 배달 속도', carrier: 'self', cap: 4, onMax: { empower: 'any', ratio: 0.3, consume: true } },
    passives: [
      pas('배달 폭주', 'play', [heal(0.3), stk(K, 1)], { when: { who: 'any', nth: 4 } }),
      pas('배달의 요정', 'fightStart', [gauge(30), stk(K, 1)]),
    ],
    ult: { name: '슈팡 배송', cost: 250, fx: [heal(2.0), st('피해 감소', 2), dmg(0.6, EA), st('약화', 1, EA)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u5`],
    basics, tokens, uniques,
  });
}

// ── 루드 — 박자형 · 탱커 ──
// 엔진: 같은 카드를 잇달아 내면 렙(받는 피해 -4%/개, 턴 끝에 0) → 「한 세트 더!」는 내면 「한 번 더!」를 손에 — 같은 동작으로 세트를 채운다 · 렙 넷이면 포즈(실드)
function rude(j) {
  // 팀: 「크로스핏 선발대」 — 렙이 쌓인 동안 파티 받는 피해가 같이 준다(렙은 파티 실드로도)
  // 원작: 강화 평타(네 번째 공격마다 포즈 · 받는 피해↓ · 끝나면 회복) · 저학년 「한 세트 더!」(범위 + 소음 + 회복) · 고학년 「임팩트 프레스」 · 「마지막 한 번 더」 무한 반복 · 다야의 최측근
  const H = '루드', K = '렙', ONE = `${H}_rep`;
  const basics = [
    S(`${H}_s1`, '맨손 스쿼트 킥', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '코어 버티기', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [T(ONE, '한 번 더!', H, '공격', [ddef(0.4, E1), make(ONE, 1)], { cost: 1, tags: ['소멸'], blurb: '「마지막 한 번 더」 — 끝나지 않는 한 세트입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '미숫가루 프로틴', 0, '스킬', [stk(K, 1), draw(1)], [                               // 시동 — 세트 전 보충
      O([stk(K, 1), draw(1)]),                                                        // (수치)
      O([sh(0.5), draw(1)]),                                                          // 쉐이커 흔들기
      O([stk(K, 2), sh(0.4)]),  // 두 스쿱
      O([heal(0.4), stk(K, 1)]),                                                      // 근손실 방지
      O([make(ONE, 1), draw(1)], { cost: 1 }),                                        // 바로 한 번 더
    ], null),
    U(`${H}_u1`, '한 세트 더!', 1, '공격', [ddef(0.35, EA), st('약화', 1, EA), make(ONE, 1)], [       // 연료 — 원작 저학년(범위 + 소음)
      O([ddef(0.35, EA), st('약화', 1, EA), make(ONE, 1)]),                           // (수치)
      O([ddef(0.3, EA), heal(0.4), make(ONE, 1)]),                                    // 고함 뒤 회복(원작)
      O([ddef(0.6, E1), make(ONE, 1)]),                                               // 한 놈만 붙잡고
      O([ddef(0.3, EA), st('약화', 2, EA), draw(1)]),  // 큰 고함
      O([ddef(0.3, EA), make(ONE, 2)], { cost: 2 }),                                  // 두 세트
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u4`, '실피르보다 세게', 1, '공격', [ddef(0.4, E1), per(K, { n: 1 }), ddef(0.2, E1)], [    // 결정타 — 한 끗 차로 이기는 한 방
      O([ddef(0.4, E1), per(K, { n: 1 }), ddef(0.2, E1)]),                             // (수치)
      O([per(K, { n: 1 }), ddef(0.15, EA)]),                                          // 가비아 임팩트 프레스
      O([ddef(0.4, E1), per(K, { n: 1 }), sh(0.25)]),                                 // 버티며 한 방
      O([ddef(0.5, E1), tough(2)]),  // 내리꽂기(격파)
      O([ddef(0.5, E1), ifStack(K, 4), st('기절', 1, E1)]),                           // 렙 넷이면 넉다운
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '다야 님을 지켜라', 1, '스킬', [sh(0.9), per(K, { n: 1 }), sh(0.2), ifStack(K, 4), st('피해 감소', 1)], [ // 원작 자유 — 다야의 최측근
      O([sh(0.9), per(K, { n: 1 }), sh(0.2), ifStack(K, 4), st('피해 감소', 1)]),     // (수치)
      O([sh(1.2), stk(K, 1)]),                                                        // 버티기 한 세트
      O([per(K, { n: 1 }), sh(0.35)], { cost: 0 }),                                   // 포즈 유지
      O([sh(0.9), heal(0.4), stk(K, 1)]),  // 포즈가 끝나면 회복(원작)
      O([sh(0.6), pw('turnEnd', [per(K, { n: 1 }), sh(0.12)])], { power: true }),     // 매일 스트레칭
    ], [B('guard'), B('heal'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '같은 동작을 되풀이한 횟수', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.04 }], endClear: true },
    passives: [
      pas('포즈', 'play', [stk(K, 1)], { when: { repeat: true, who: 'any' } }),
      pas('단백질 보충', 'play', [sh(0.2)], { when: { type: '공격' } }),
    ],
    ult: { name: '임팩트 프레스', cost: 300, fx: [ddef(0.25, EA, { hits: 4 }), st('기절', 1, E1), stk(K, 4)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, tokens, uniques,
  });
}

// ── 오팔 — 박자형 · 서포터 ──
// 엔진: 0코 탭댄스로 탭을 밟아 셋째 탭에 보석이 깨져 적 전체 파편 + 파티 다음 카드 강화 · 아군이 맞으면 눈물 그렁 — 셋이면 울다 춤춘다(탭댄스 1장)
function opal(j) {
  // 팀: 「가십 드래곤」 — 공격력 높은 아군에게 양산(주는 피해↑ · 회복) · 셋째 탭의 파편이 파티 다음 카드를 +20%
  // 원작: 세 번째 공격마다 보석이 깨져 범위 파편 · 저학년 「가십 드래곤」 · 고학년 「오팔 파우더」(보호막 · 침묵) · 슬픔을 쫓으려 추는 탭댄스 · 아군이 30번 맞으면(어사이드)
  const H = '오팔', K = '눈물 그렁', TP = '탭', TAP = `${H}_tap`;
  const basics = [
    S(`${H}_s1`, '양산 휘두르기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '반짝 양산', 1, '스킬', [sh(1.6)]),
  ];
  const GEM = `${H}_gem`;
  const tokens = [T(TAP, '탭댄스', H, '스킬', [heal(0.25), stk(TP, 1)], { tags: ['소멸'], blurb: '슬픔을 쫓는 탭댄스입니다' }),
    T(GEM, '깨진 보석', H, '공격', [dmg(0.5, EA), empower(0.2, 'any')], { tags: ['소멸'], blurb: '셋째 탭에 깨진 보석 파편입니다' })];
  const BK = [stk(TP, 1)];
  const uniques = [
    U(`${H}_u4`, '반짝이는 모든 것들', 1, '스킬', [make(TAP, 2), sh(0.4)], [                        // 시동 — 탭 두 장
      O([make(TAP, 2), sh(0.4)]),                                                     // (수치)
      O([make(TAP, 1), draw(1)], { cost: 0 }),                                        // 한 발짝
      O([make(TAP, 3)], { cost: 2 }),                                                 // 무대 위로
      O([make(TAP, 2), heal(0.3)]),                                                   // 눈물 뚝
      O([stk(TP, 2), sh(0.6), draw(1)]),  // 바로 두 번 밟기
    ], null),
    U(`${H}_u3`, '보석 던지기', 1, '공격', [dmg(0.55, E1), per(TP, { n: 1 }), dmg(0.22, EA)], [         // 결정타 — 원작 강화 평타(깨진 보석 파편)
      O([dmg(0.55, E1), per(TP, { n: 1 }), dmg(0.22, EA)]),                            // (수치)
      O([dmg(0.6, ER), stk(TP, 2)]),  // 그냥 던지기
      O([per(TP, { n: 1 }), dmg(0.3, EA), spendAll(TP)]),                              // 다 깨기
      O([dmg(0.5, E1), per(TP, { n: 1 }), heal(0.15), stk(TP, 1)]),  // 반짝 회복
      O([dmg(0.4, E1), per(TP, { n: 1 }), dmg(0.18, EA), st('약화', 1, E1)]),          // 고무줄 총(난폭)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '가십 드래곤', 1, '스킬', [dmod(0.2, 'strongestAlly', { turns: 2 }), heal(0.5), stk(TP, 1)], [ // 연료 — 원작 저학년(양산 · 피해↑ · 회복)
      O([dmod(0.2, 'strongestAlly', { turns: 2 }), heal(0.5), stk(TP, 1)]),            // (수치)
      O([dmod(0.15, ALLIES, { turns: 2 }), heal(0.4), stk(TP, 1)]),  // 양산 크게 펼치기
      O([heal(0.8), stk(K, 1), stk(TP, 1)]),  // 울먹이며 위로
      O([dmod(0.25, 'strongestAlly', { turns: 2 }), draw(1)], { cost: 0 }),            // 필터 없는 칭찬
      O([heal(0.4), stk(TP, 1), pw('turnStart', [heal(0.15)])], { cost: 0, power: true }),  // 매초 회복(원작)
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u1`, '선배님~!', 1, '스킬', [per(K, { n: 1 }), heal(0.4), spendAll(K), stk(TP, 1)], [         // 원작 자유 — 다야 선배에게 매달리기
      O([per(K, { n: 1 }), heal(0.4), spendAll(K), stk(TP, 1)]),  // (수치)
      O([per(K, { n: 1 }), sh(0.4), spendAll(K)]),                                     // 선배 뒤에 숨기
      O([heal(0.4), make(TAP, 1)]),                                                    // 이달의 우수 용족
      O([per(K, { n: 1 }), heal(0.3), cleanse(1), stk(TP, 1)]),  // 기절 털어 주기(어사이드)
      O([per(K, { n: 1 }), heal(0.3), spendAll(K)], { cost: 0 }),  // 칭찬받으면 탭
    ], [B('heal'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '아무 이유로나 그렁그렁한 눈물', carrier: 'self', cap: 3, onMax: { make: TAP, consume: true } },
    keywords: [{ name: TP, desc: '이어 밟은 탭', carrier: 'self', cap: 3, onMax: { make: GEM, consume: true } }],
    passives: [
      pas('울먹울먹', 'hurt', [stk(K, 1)]),
      pas('오팔을 칭찬합니다!', 'play', [heal(0.15), stk(TP, 1)], { when: { type: '스킬', who: 'other' } }),
    ],
    ult: { name: '오팔 파우더', cost: 200, fx: [sh(2.5), st('약화', 2, EA), st('둔화', 1, EA), make(TAP, 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`],
    basics, tokens, uniques,
  });
}

// ── 아르코 — 박자형 · 딜러 ──
// 엔진: 바로 앞 카드와 다른 종류(공격 ↔ 스킬, 누구 것이든)를 내면 스텝 +1, 같은 종류가 이어지면 지루해서 쌓이지 않는다 → 셋째 스텝에 다음 카드 +50% · 공격마다 음파
function arco(j) {
  // 팀: 엇박은 주인을 가리지 않는다 — 동료의 스킬 뒤에 아르코 공격, 아르코 스킬 뒤에 동료 공격이 모두 스텝
  // 원작: 셋째 공격마다 음파 2번(SP 깎기) · 저학년 「정정당당 댄스배틀」(4회 범위 · 보호막 파괴) · 고학년 「퍼플 쇼츠」(HP 50% 미만 확정 치명) · 한 가지 춤만 추는 크루를 지옥 훈련
  const H = '아르코', K = '스텝';
  const basics = [
    S(`${H}_s1`, '스텝 킥', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '토네이도 킥', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '백스핀 회피', 1, '스킬', [sh(1.5)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '프리즈', 0, '스킬', [stk(K, 1), draw(1)], [                                        // 시동 — 공격 사이에 끼우는 스킬 한 박
      O([stk(K, 1), draw(1)]),                                                        // (수치)
      O([sh(0.5), draw(1)]),                                                          // 멈춤 자세
      O([draw(2)]),  // 다음 동작 고르기
      O([stk(K, 1), st('회피', 1)]),                                                  // 백스핀
      O([stk(K, 2)]),  // 99시간 연습
    ], null),
    U(`${H}_u1`, '정정당당 댄스배틀', 1, '공격', [strip(E1), dmg(0.18, EA, { hits: 3 }), ifTypeChange, stk(K, 1)], [ // 연료 — 원작 저학년(4회 범위 · 보호막 파괴)
      O([strip(E1), dmg(0.18, EA, { hits: 3 }), ifTypeChange, stk(K, 1)]),             // (수치)
      O([dmg(0.4, E1, { hits: 3 }), ifTypeChange, stk(K, 1)]),                         // 일대일 배틀
      O([dmg(0.15, EA, { hits: 3 }), stk(K, 1)]),                                      // 늘 추던 그 춤
      O([strip(EA), dmg(0.18, EA, { hits: 3 }), stk(K, 1)]),  // 전원 실드 깨기
      O([dmg(0.18, EA, { hits: 3 }), ifTypeChange, draw(1)]),                          // 엇박에 다음 동작
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '토네이도 스핀', 1, '공격', [dmg(0.45, EA), per(K, { n: 1 }), dmg(0.2, EA), spendAll(K)], [ // 결정타 — 모은 스텝을 회전으로
      O([dmg(0.45, EA), per(K, { n: 1 }), dmg(0.2, EA), spendAll(K)]),                 // (수치)
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)]),                  // 한 놈에게 헤드스핀
      O([dmg(0.4, EA), per(K, { n: 1 }), dmg(0.18, EA)]),                              // 스텝은 남긴다
      O([per(K, { n: 1 }), dmg(0.22, EA), spendAll(K), st('약화', 1, EA)]),            // 흥겨운 파동(주는 피해↓)
      O([per(K, { n: 1 }), dmg(0.25, EA), ifFoe('hp', { pct: 0.5 }), crit, dmg(0.5, E1)], { cost: 2 }),  // 퍼플 쇼츠 맛보기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '적포도 스트리트', 1, '강화', [st('협공', 1), pw('play', [ifTypeChange, dmg(0.25, ER)], { when: { who: 'any' } })], [ // 원작 자유 — 페스타를 동경한 거리의 댄서
      O([st('협공', 1), pw('play', [ifTypeChange, dmg(0.25, ER)], { when: { who: 'any' } })]), // (수치)
      O([pw('play', [ifTypeChange, sh(0.2)], { when: { who: 'any' } })], { cost: 0 }),   // 백스핀 스트리트
      O([st('협공', 2), pw('turnStart', [stk(K, 1)])]),                                  // 크루 지옥 훈련
      O([pw('play', [ifTypeChange, dmg(0.2, ER), stk(K, 1)], { when: { who: 'any' } })], { cost: 2 }), // 자유와 저항
      O([st('협공', 2), pw('play', [dmg(0.2, ER)], { when: { type: '스킬', who: 'any' } })]),  // 응원봉(어사이드)
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '엇박으로 이어 밟은 스텝', carrier: 'self', cap: 3, onMax: { empower: 'next', ratio: 0.5, consume: true } },
    passives: [
      pas('리듬에 몸을 맡겨', 'play', [ifTypeChange, stk(K, 1)], { when: { who: 'any' } }),
      pas('음파 스텝', 'play', [dmg(0.3, ER)], { when: { type: '공격' } }),
    ],
    ult: { name: '퍼플 쇼츠', cost: 250, fx: [dmg(0.2, TOP, { hits: 4 }), st('회피', 1), ifFoe('hp', { pct: 0.5 }), crit, dmg(0.8, E1)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 벨벳 — 격파형 · 탱커 ──
// 엔진: 「그냥 다 덤벼!」로 막아 서고(반격 · 피해 감소) 맞을 때마다 근력 강화(받는 피해 -4%/개) → 「원심 분리 펀치」가 근력만큼 적 전체 강인도를 깎는다 · 격파하면 고학년이 찬다
function velvet(j) {
  // 팀: 「민심을 잡아라」 — 근력이 다섯이면 파티 받는 피해 -10%(근육교 교주의 품)
  // 원작: 힘의 마녀 · 저학년 「그냥 다 덤벼!」(도발 · 받는 물리 피해↓) · 어사이드(저학년 뒤 아군 보호막 · 맞으면 고학년 대기 감소) · 고학년 「원심 분리」(11회 휩쓸기 · 넉백) · 근육교
  const H = '벨벳', K = '근력 강화';
  const basics = [
    S(`${H}_s1`, '도끼 내리찍기', 1, '공격', [dmg(0.4, EA)]),
    S(`${H}_s2`, '이두근 가드', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '그냥 다 덤벼!', 1, '스킬', [sh(1.0), st('반격', 2), st('피해 감소', 1)], [          // 시동 — 원작 저학년(도발 · 받는 피해↓)
      O([sh(1.0), st('반격', 2), st('피해 감소', 1)]),                                 // (수치)
      O([sh(0.8), st('반격', 2), stk(K, 1)]),  // 근육 자랑
      O([sh(0.6), st('피해 감소', 2)], { cost: 0 }),  // 왕좌에서 버티기
      O([sh(0.6), st('반격', 2), gauge(15)], { cost: 0 }),  // 나는 내가 지킨다(고학년 앞당김)
      O([st('반격', 3), stk(K, 1)], { cost: 0 }),                                      // 다 덤벼 보라고
    ], null),
    U(`${H}_u3`, '원심 분리 펀치', 1, '공격', [per(K, { n: 1 }), ddef(0.18, EA), tough(1, EA), spendAll(K)], [ // 결정타 — 근력만큼 휩쓸기
      O([per(K, { n: 1 }), ddef(0.18, EA), tough(1, EA), spendAll(K)]),                 // (수치)
      O([per(K, { n: 1 }), ddef(0.35, E1), tough(2), spendAll(K)]),                     // 한 놈을 날리기
      O([per(K, { n: 1 }), ddef(0.15, EA), tough(1, EA)]),                              // 근육은 남긴다
      O([per(K, { n: 1 }), sh(0.3), spendAll(K), st('반격', 2)]),                       // 근육 갑옷
      O([per(K, { n: 1 }), ddef(0.15, EA), tough(1, EA), st('둔화', 1, EA)]),  // 넉백(진영 붕괴)
    ], [B('power'), B('cost'), B(BK)], ['분쇄']),
    U(`${H}_u1`, '이두근 펌핑', 1, '강화', [stk(K, 1), sh(0.6), pw('turnStart', [stk(K, 1)])], [       // 연료 — 근력 강화 마법 하나로 고위 마녀
      O([stk(K, 1), sh(0.6), pw('turnStart', [stk(K, 1)])]),                           // (수치)
      O([stk(K, 2), pw('hurt', [sh(0.15)])]),                                           // 맞아도 펌핑
      O([pw('turnStart', [stk(K, 1), sh(0.3)])]),  // 매일 펌핑
      O([stk(K, 1), pw('break', [stk(K, 2), gauge(10)])]),                              // 무너뜨리면 더 커진다
      O([sh(0.6), pw('turnStart', [stk(K, 1)])], { cost: 0 }),                          // 가벼운 펌핑
    ], [B('guard'), B('cost'), B(BK)], ['개전']),
    U(`${H}_u5`, '솔즙 무료 나눔', 0, '스킬', [heal(0.5), stk(K, 1)], [                                  // 원작 자유 — 근육교 포교
      O([heal(0.5), stk(K, 1)]),                                                       // (수치)
      O([heal(0.4), draw(1)]),                                                         // 한 잔 더
      O([sh(0.5), stk(K, 1)]),                                                         // 근육교 입교 선물
      O([heal(0.7), stk(K, 2), cleanse(1)], { cost: 1 }),  // 대용량
      O([cleanse(1), stk(K, 1)]),                                                      // 몸에 좋은 쓴맛
    ], [B('heal'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '근력 강화 마법으로 불어난 근육', carrier: 'self', cap: 5, per: [{ stat: 'taken', v: -0.04 }] },
    passives: [
      pas('스스로 지키는 힘', 'hurt', [stk(K, 1), gauge(3)]),
      pas('진영을 흔드는 근육', 'break', [gauge(20), stk(K, 1)]),
    ],
    ult: { name: '마법: 원심 분리', cost: 250, fx: [ddef(0.3, EA, { hits: 3 }), tough(2, EA), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, uniques,
  });
}

// ── 샤샤 — 격파형 · 딜러 ──
// 엔진: 공격마다 텀블러 수압(주는 피해 +10%/개) → 「물줄기 발사」로 수압을 다 쏟아 HP 가장 높은 적의 버팀을 날리고, 격파하면 안 친 적에게 한 번 더 · 수압이 넘치면 텀블러 폭주(무작위 적 + HP 대가)
function shasha(j) {
  // 팀: 「시켜줘 아군 명예소방관」 — 격파된 적이 있으면 「부수적인 피해입니다」 가 적 전체로(동료의 격파도 텀블러가 반긴다)
  // 원작: 저학년 「텀블러 투척!」(5회 물살 · 평타 피해↓) · 고학년 「물대포 폭격!!」(HP 가장 높은 적 18타 · 대상 바꾸기 · 넉백) · 넷째 공격마다 거대 물방울 · 수압을 조절할 생각이 없는 텀블러
  const H = '샤샤', K = '텀블러 수압', RUN = `${H}_runaway`;
  const basics = [
    S(`${H}_s1`, '물방울 탄', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '왕물방울 탄', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '텀블러 방패', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [T(RUN, '텀블러 폭주', H, '공격', [dmg(0.55, ER, { hits: 2 }), { k: 'payHp', v: 30 }], { tags: ['소멸'], blurb: '수압을 못 참은 텀블러가 제멋대로 날뜁니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u5`, '텀블러 달래기', 1, '스킬', [sh(1.1), stk(K, 2)], [                                  // 시동 — 수압 채우기
      O([sh(1.1), stk(K, 2)]),                                                         // (수치)
      O([stk(K, 2), draw(1)], { cost: 0 }),                                            // 꾹 눌러 담기
      O([sh(0.8), stk(K, 1), cleanse(1)]),                                             // 우비 입기
      O([sh(0.7), pw('turnStart', [stk(K, 1)])], { power: true }),                     // 정령산 호수와 이어진 통
      O([sh(1.3), st('피해 감소', 1)]),                                                // 죄송해요 죄송해요
    ], null),
    U(`${H}_u1`, '텀블러 투척!', 1, '공격', [dmg(0.4, EA), st('약화', 1, EA), stk(K, 1)], [            // 연료 — 원작 저학년(물살 · 평타 피해↓)
      O([dmg(0.4, EA), st('약화', 1, EA), stk(K, 1)]),                                 // (수치)
      O([dmg(0.7, E1), tough(1), stk(K, 1)]),                                          // 한 놈 위로 떨어뜨리기
      O([dmg(0.35, EA), stk(K, 2)]),                                                   // 물살이 넘친다
      O([dmg(0.15, EA, { hits: 3 }), st('약화', 1, EA)]),                              // 다섯 번 물살
      O([dmg(0.35, EA), tough(1, EA), stk(K, 1)]),  // 거대 물방울(격파)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '물줄기 발사', 1, '공격', [per(K, { n: 1 }), dmg(0.45, TOP), spendAll(K), ifBreak, dmg(0.8, 'otherEnemy')], [ // 결정타 — 원작 고학년(HP 가장 높은 적 · 대상 바꾸기)
      O([per(K, { n: 1 }), dmg(0.45, TOP), spendAll(K), ifBreak, dmg(0.8, 'otherEnemy')]), // (수치)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),                                // 물대포 난사
      O([per(K, { n: 1 }), dmg(0.35, TOP), tough(1, TOP)]),                            // 수압은 남긴다
      O([tough(2, TOP), ifBreak, dmg(1.0, 'otherEnemy')], { cost: 0 }),  // 전열 밀어내기(진영 붕괴)
      O([per(K, { n: 1 }), dmg(0.45, TOP), make(RUN, 1)]),  // 일부러 폭주시키기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '부수적인 피해입니다', 1, '공격', [dmg(0.55, EA), ifFoe('broken'), dmg(0.45, EA), st('둔화', 1, EA)], [ // 원작 자유 — 불을 끄다 마을을 부순다
      O([dmg(0.55, EA), ifFoe('broken'), dmg(0.45, EA), st('둔화', 1, EA)]),            // (수치)
      O([dmg(0.5, EA), ifFoe('broken'), stk(K, 2)]),                                   // 빵집째
      O([dmg(0.9, E1), ifFoe('broken'), dmg(0.6, E1)]),                                // 한 집만 부수기
      O([cleanse(1), dmg(0.5, EA)]),                                                   // 불 하나는 잘 꺼요
      O([dmg(0.45, EA), ifFoe('broken'), dmg(0.4, EA), draw(1)]),                      // 사과하며 다음 수
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '조절할 생각이 없는 텀블러 수압', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }], onMax: { make: RUN, consume: true } },
    passives: [
      pas('대용량 텀블러', 'fightStart', [stk(K, 1)]),
      pas('대용량 텀블러', 'play', [stk(K, 1)], { when: { type: '공격' } }),
      pas('남겨 둔 물', 'break', [stk(K, 2)]),
    ],
    ult: { name: '물대포 폭격!!', cost: 150, fx: [dmg(0.2, TOP, { hits: 4 }), tough(2, TOP), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
}

// ── 키디언 — 격파형 · 딜러 ──
// 엔진: 아웃사이드 커터 · 급소 찾기로 강인도를 깎아 격파하면 별빛 조각(치명 +8% · 피해 +5%/개) → 「밤하늘 다이브」가 조각을 다 써서 HP 낮은 적을 처치하면 한 번 더
function kidian(j) {
  // 팀: 「내가 빛내줄게⋯」 — 동료가 격파해도 별빛 조각 1(누가 무너뜨리든 그 틈에 다이브)
  // 원작: 저학년 「아웃사이드 커터」(아티팩트마다 공격 횟수↑ · 슈퍼 박치기) · 고학년 「쉐도우 다이브」(HP 비율 가장 낮은 적 · 처치하면 강화된 스킬 한 번 더) · 교주의 별자리
  const H = '키디언', K = '별빛 조각';
  const basics = [
    S(`${H}_s1`, '쿠나이 투척', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '그림자 숨기', 1, '스킬', [sh(1.5)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '급소 찾기', 0, '스킬', [tough(1), stk(K, 1)], [                                    // 시동 — 버팀의 틈을 본다
      O([tough(1), stk(K, 1)]),                                                        // (수치)
      O([st('약점 노출', 1, E1), draw(1)]),  // 그늘에서 내다본 급소(약점 노출)
      O([tough(1), ifFoe('broken'), stk(K, 2)]),                                       // 무너진 놈부터
      O([tough(2), stk(K, 1)]),  // 깊이 파고들기
      O([stk(K, 1), draw(1)]),                                                         // 별자리 그리기
    ], null),
    U(`${H}_u1`, '아웃사이드 커터', 1, '공격', [dmg(0.3, E1, { hits: 2 }), tough(1), stk(K, 1)], [      // 연료 — 원작 저학년(공격 횟수↑)
      O([dmg(0.3, E1, { hits: 2 }), tough(1), stk(K, 1)]),                             // (수치)
      O([dmg(0.25, E1, { hits: 3 }), tough(1)]),                                       // 슈퍼 박치기(한 번 더)
      O([dmg(0.5, E1), tough(2)]),                                                     // 깊은 베기
      O([dmg(0.3, E1, { hits: 2 }), ifFoe('broken'), stk(K, 2)]),                      // 무너진 틈
      O([dmg(0.3, LOW, { hits: 2 }), stk(K, 1)]),                                      // 약한 놈 노리기
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '밤하늘 다이브', 1, '공격', [per(K, { n: 1 }), dmg(0.42, LOW), spendAll(K), ifKill, dmg(0.9, LOW)], [ // 결정타 — 원작 고학년(처치하면 한 번 더)
      O([per(K, { n: 1 }), dmg(0.42, LOW), spendAll(K), ifKill, dmg(0.9, LOW)]),       // (수치)
      O([per(K, { n: 1 }), dmg(0.45, E1), spendAll(K), ifKill, dmg(0.8, LOW)]),        // 고른 표적에게
      O([per(K, { n: 1 }), dmg(0.35, LOW), ifKill, stk(K, 2)]),                        // 조각은 남긴다
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),                                // 별비
      O([per(K, { n: 1 }), dmg(0.4, LOW), spendAll(K), ifKill, gauge(30)]),            // 다음 다이브(고학년 앞당김)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '별 관찰', 1, '공격', [st('잔광', 1), dmg(0.55, E1), ifFoe('broken'), stk(K, 2)], [     // 원작 자유 — 교주를 떠올리며 만든 별자리
      O([st('잔광', 1), dmg(0.55, E1), ifFoe('broken'), stk(K, 2)]),                   // (수치)
      O([dmg(0.6, E1), dmod(0.15, OTHERS)]),                                           // 내가 빛내줄게
      O([st('잔광', 2), stk(K, 1)], { cost: 0 }),                                      // 별을 세다
      O([dmg(0.5, E1), tough(1), ifFoe('broken'), draw(1)]),                           // 별자리 잇기
      O([st('잔광', 1), dmg(0.7, LOW)]),                                               // 떨어지는 별
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '어둠 속에 모은 별빛', carrier: 'self', cap: 4, per: [{ stat: 'crit', v: 0.08 }, { stat: 'dealt', v: 0.05 }], onMax: { empower: 'next', ratio: 0.5, consume: true } },
    passives: [
      pas('그림자 추적', 'break', [stk(K, 2)], { when: { who: 'any' } }),
      pas('흑요석 단면', 'kill', [stk(K, 1), dmg(0.5, LOW)]),
    ],
    ult: { name: '쉐도우 다이브', cost: 150, fx: [dmg(2.3, LOW), stk(K, 2), ifKill, dmg(1.8, LOW)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, uniques,
  });
}

export const JOBS = [['마녀/아사나', asana], ['수인/티그', tig], ['엘프/리스티', risty], ['엘프/캐시', cash], ['요정/리코타', ricotta], ['요정/슈팡', shupang],
  ['용족/루드', rude], ['용족/오팔', opal], ['정령/아르코', arco], ['마녀/벨벳', velvet], ['요정/샤샤', shasha], ['용족/키디언', kidian]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
