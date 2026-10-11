// 묶음 6 — 표적형 7 + 손 만들기형 5. node _gen/deep/b6.mjs [사도 일부]
// 고유 효과 · 패시브는 10-08 갈래 재설계(원작 근거)를 이어 쓰는 사도가 많다(kw0 · pas0). 고유 4장 · 신탁 · 기본 카드 · 고학년은 새로.
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, FAR, ALLY, ALLIES, OTHERS, dmg, ddef, sh, heal, st, stk, spendAll, per, perTag, draw, make, disc, gauge,
  ifStack, ifKill, ifHunted, ifFoe, empower, tough, dmod, extra, cost, pw, pas, O, B, U, S, T, hero } = L;
const MK = 'markedEnemy';
const perDebuff = { k: 'perDebuff' };
const roll = n => ({ k: 'roll', n }), ifRoll = n => ({ k: 'ifRoll', n });
const handEnd = { k: 'when', on: 'handEnd' };
const exileTag = tag => ({ k: 'exileFrom', from: 'hand', all: true, tag });
const kw0 = j => { const h = j.heroes[0]; return { keyword: h.keyword, ...(h.keywords ? { keywords: h.keywords } : {}) }; };
const pas0 = j => j.heroes[0].passives;
const tok0 = (j, id) => j.cards.find(c => c.id === id);

// ── 그윈 — 표적형 · 서포터 ──
// 엔진: 먼저 찜한 적 하나에 깃발(찍은 적 — 공격할 때마다 깃발 +1) → 「깃발 꽂고 인증샷」이 깃발만큼 · 찜한 적이 쓰러지면 탐험 지도(드로우 + 다음 적에 깃발)
function gwin(j) {
  // 팀: 깃발 적은 파티의 표적(공격할 때마다 깃발) · 쓰러뜨리면 지도 한 칸(누가 쓰러뜨리든)
  // 원작: 「먼저 찜한 쪽이 임자」(어디든 깃발) · 저학년 「스노우포그」(깃발 · 안개 · 적 피해↓ · 동상 적이면 기절) · 고학년 「극지귀가길」(얼음길 + 펭귄 돌진) · 동생 델리아의 이름을 붙인 땅
  const H = '그윈', K = '깃발', MAP = `${H}_map`;
  const basics = [
    S(`${H}_s1`, '펭귄 부리 쪼기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '깃털 품기', 1, '스킬', [sh(1.5)]),
  ];
  const tokens = [tok0(j, MAP)];
  const BK = [stk(K, 1, MK)];
  const uniques = [
    U(`${H}_u1`, '스노우포그', 1, '스킬', [stk(K, 2), st('약화', 1, EA)], [                                // 시동 — 원작 저학년(깃발 · 안개)
      O([stk(K, 2), st('약화', 1, EA)]),                                             // (수치)
      O([stk(K, 1), st('약화', 1, EA), sh(0.6)]),                                    // 짙은 안개
      O([stk(K, 2), draw(1)], { cost: 0 }),                                          // 지도 펼치기
      O([stk(K, 1), st('둔화', 1, EA), draw(1)]),  // 얼어붙은 땅
      O([stk(K, 2), st('냉정 약점', 2, MK)]),  // 스노우포그 — 깃발 적에게 냉정 약점(동상이면 기절, 원작)
    ], null),
    U(`${H}_u4`, '깃발 꽂고 인증샷', 1, '공격', [per(K, { n: 1 }), dmg(0.45, MK), spendAll(K), sh(0.5)], [    // 결정타 — 찜한 땅을 기념
      O([per(K, { n: 1 }), dmg(0.45, MK), spendAll(K), sh(0.5)]),                    // (수치)
      O([per(K, { n: 1 }), sh(0.4), spendAll(K), draw(1)]),                           // 인증샷만
      O([per(K, { n: 1 }), dmg(0.4, MK), sh(0.4)]),                                   // 깃발은 남긴다
      O([per(K, { n: 1 }), dmg(0.35, MK), make(MAP, 1)]),                // 지도에 새 칸
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),                               // 동네방네 자랑
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '신나는 모험이다!', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1, MK)])], [         // 연료 — 매일 새 깃발
      O([st('사기', 1), pw('turnStart', [stk(K, 1, MK)])]),                           // (수치)
      O([pw('turnStart', [stk(K, 1, MK)])], { cost: 0 }),                             // 아침 탐험
      O([st('사기', 1), pw('huntDown', [draw(1), make(MAP, 1)])]),                    // 새 땅 발견
      O([pw('turnStart', [stk(K, 1, MK), sh(0.3)])]),  // 빵주 일등항해사
      O([st('사기', 2), stk(K, 1, MK)]),                                              // 교단 본부에도 깃발
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '얼음 가방', 1, '공격', [dmg(0.45, EA), stk(K, 1), sh(0.6)], [                              // 원작 자유 — 글레이시아 탐험가의 가방
      O([dmg(0.45, EA), stk(K, 1), sh(0.6)]),                                         // (수치)
      O([dmg(0.7, MK), stk(K, 1)]),                                                   // 찜한 적에게 휘두르기
      O([dmg(0.4, EA), st('둔화', 1, EA), stk(K, 1)]),  // 얼음길
      O([dmg(0.4, EA), sh(0.5), draw(1)]),                                            // 가방 속 간식
      O([dmg(0.2, ER, { hits: 4 }), stk(K, 1)]),  // 펭귄 떼 돌진(고학년)
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '극지귀가길', cost: 250, fx: [st('둔화', 2, EA), dmg(0.25, ER, { hits: 4 }), stk(K, 2, MK)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}

// ── 란 — 표적형 · 딜러(엘다인) ──
// 엔진: 공격마다 적 전체에 허공 가르기 + 늑대 표식(지속 피해 · 주는 피해↓) → 가장 약한 하나를 사냥감(란에게 받는 피해 +10%)으로 · 쓰러지면 바로 다음 사냥감
function ran(j) {
  // 팀: 늑대 표식이 적 전체의 주먹을 무디게 하고(주는 피해 -5%/겹) 지속 피해 — 파티가 받는 피해를 줄인다
  // 원작: 저학년(범위 베기 · 늑대 표식 · HP 75% 밑이면 다시 붙음) · 고학년 「몰이 사냥」(표식 · HP 낮은 적 · 처치하면 한 번 더) · 넷째 참격 확정 치명 · 칼 「사랑니」 · 투구꽃 독
  const H = '란', W = '늑대 표식', P = '사냥감';
  const basics = [
    S(`${H}_s1`, '발도', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '일섬 발도', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '흘려 막기', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(W, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '아랑의 여유', 1, '공격', [dmg(0.6, EA), stk(W, 1, EA), stk(P, 1, LOW)], [                  // 시동 — 원작 저학년(범위 베기 · 표식)
      O([dmg(0.6, EA), stk(W, 1, EA), stk(P, 1, LOW)]),                               // (수치)
      O([dmg(0.5, EA), stk(W, 2, EA)]),                                               // 표식 깊이
      O([dmg(0.8, LOW), stk(P, 1, LOW)]),                                             // 약한 놈 물기
      O([stk(W, 1, EA), stk(P, 1, LOW), draw(1)], { cost: 0 }),                       // 웃는 얼굴 뒤
      O([dmg(0.5, EA), stk(W, 1, EA), sh(0.5)]),                                      // 여유로운 회피
    ], null),
    U(`${H}_u2`, '사랑니 발도술', 1, '공격', [per(W, { n: 1 }), dmg(0.5, E1), spendAll(W), ifKill, dmg(0.8, LOW)], [ // 결정타 — 표식을 베어 내고 다음 사냥감
      O([per(W, { n: 1 }), dmg(0.5, E1), spendAll(W), ifKill, dmg(0.8, LOW)]),        // (수치)
      O([per(W, { n: 1 }), dmg(0.45, LOW), spendAll(W), ifKill, dmg(0.7, LOW)]),      // 몰이 사냥(가장 약한 놈)
      O([per(W, { n: 1 }), dmg(0.45, E1), ifKill, dmg(0.6, LOW)]),                    // 표식은 둔다
      O([{ k: 'critMod', v: 1, target: 'self' }, per(W, { n: 1 }), dmg(0.45, E1), spendAll(W)], { cost: 2 }), // 넷째 참격 확정 치명
      O([per(W, { n: 1 }), dmg(0.45, E1), stk(P, 1, LOW)]),              // 다음 사냥감 점찍기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '날선 늑대의 발톱', 1, '공격', [dmg(0.75, E1), stk(W, 1), ifStack(P), dmg(0.6, E1)], [       // 연료 — 사냥감에게 더 깊이
      O([dmg(0.75, E1), stk(W, 1), ifStack(P), dmg(0.6, E1)]),                        // (수치)
      O([dmg(0.6, E1), stk(W, 2)]),                                                   // 할퀴기
      O([dmg(0.7, LOW), ifStack(P), dmg(0.7, LOW)]),                                  // 끝까지 매달리기
      O([dmg(0.6, E1), stk(P, 1), draw(1)]),                                          // 냄새 맡기
      O([dmg(0.45, EA), stk(W, 1, EA)]),  // 늑대 무리
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '투구꽃 독', 1, '스킬', [stk(W, 2), st('고통', 3), draw(1)], [                                // 원작 자유 — 머리의 투구꽃(늑대 독초)
      O([stk(W, 2), st('고통', 3), draw(1)]),                                         // (수치)
      O([stk(W, 1, EA), st('고통', 2, EA), draw(1)]),  // 독초 흩뿌리기
      O([stk(W, 2), st('약화', 2, E1)]),                                              // 저린 손
      O([st('고통', 4), draw(1)], { cost: 0 }),                                       // 독 바른 칼날
      O([stk(W, 2), stk(P, 1), draw(1)]),  // 집무실 박살
    ], [B('draw'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '몰이 사냥', cost: 250, fx: [dmg(0.9, E1), tough(2), ifKill, dmg(0.6, 'otherEnemy')] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 스패럿 — 표적형 · 서포터 ──
// 엔진: 오늘은 저 녀석 — HP 가장 높은 적에 해적의 낙인(받는 피해 +8%) · 그 적의 실드를 깨거나 쓰러뜨리면 전리품(0코 · 회복 · 노획물 · 드로우) → 노획물이 차면 찬란한 금은보화(AP +1)
function sparrot(j) {
  // 팀: 전리품을 털어 부하(파티)에게 나눈다 — 노획물 1개당 턴 끝 회복 · 금은보화로 AP
  // 원작: 저학년 「오늘은 저녀석이다!」(최대 HP 가장 높은 적 · 해적의 낙인 · 전리품을 HP 낮은 아군에게) · 어사이드(보물 상자 약탈 · 찬란한 금은보화) · 볼레 약속 · 김 화폐 해적 거래소
  const H = '스패럿', K = '노획물', N = '해적의 낙인', T1 = `${H}_t1`, GOLD = `${H}_gold`;
  const basics = [
    S(`${H}_s1`, '해적 권총', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '항해용 비스킷', 1, '스킬', [heal(1.4)]),
  ];
  const tokens = [tok0(j, T1), tok0(j, GOLD)];
  const BK = [make(T1, 1)];
  const uniques = [
    U(`${H}_u1`, '오늘은 저녀석이다!', 1, '공격', [dmg(0.56, TOP), stk(N, 1, TOP), make(T1, 1)], [            // 시동 — 원작 저학년(가장 큰 놈에게 낙인)
      O([dmg(0.56, TOP), stk(N, 1, TOP), make(T1, 1)]),                               // (수치)
      O([dmg(0.25, TOP, { hits: 3 }), stk(N, 1, TOP)]),                               // 앵무새 쪼기
      O([stk(N, 1, TOP), make(T1, 2)], { cost: 0 }),                                  // 털 준비
      O([dmg(0.5, TOP), stk(N, 1, TOP), st('약점 노출', 1, TOP)]),  // 오늘은 저녀석 — 약점 노출(낙인)
      O([dmg(0.5, TOP), stk(N, 1, TOP), draw(1)]),                                    // 수배서 작성
    ], null, ['분쇄']),
    U(`${H}_u3`, '약탈한 전리품', 1, '스킬', [make(T1, 2), stk(K, 1)], [                                     // 연료 — 털어 온 것을 나눈다
      O([make(T1, 2), stk(K, 1)]),                                                    // (수치)
      O([make(T1, 1), heal(0.5)]),                                                    // HP 낮은 부하에게
      O([make(T1, 3)], { cost: 2 }),                                                  // 보물 상자째
      O([make(T1, 1), stk(K, 2)]),                                                    // 김 200장
      O([make(T1, 1), dmod(0.15, OTHERS)]),                                           // 스킬 피해↑(어사이드)
    ], [B('draw'), B('heal'), B(BK)]),
    U(`${H}_u4`, '플라즈마 포 일제사격', 2, '공격', [dmg(0.6, EA), perTag(T1), dmg(0.25, EA)], [               // 결정타 — 쥔 전리품만큼 포격
      O([dmg(0.6, EA), perTag(T1), dmg(0.25, EA)]),                                   // (수치)
      O([dmg(0.9, TOP), perTag(T1), dmg(0.4, TOP)]),                                  // 낙인 찍힌 놈에게
      O([perTag(T1), dmg(0.35, EA), exileTag(T1)]),                                   // 전리품을 포탄으로
      O([dmg(0.5, EA), { k: 'strip', target: TOP }, make(T1, 1)]),                    // 실드 깨고 약탈
      O([dmg(0.5, EA), perTag(T1), heal(0.15), make(T1, 1)]),  // 승전 잔치
    ], [B('power'), B('cost'), B(BK)], ['분쇄']),
    U(`${H}_u2`, '해적의 언어', 1, '공격', [dmg(0.64, E1), st('약화', 2), ifHunted, make(T1, 1)], [            // 원작 자유 — 「볼레」 로 맺은 약속
      O([dmg(0.64, E1), st('약화', 2), ifHunted, make(T1, 1)]),                       // (수치)
      O([dmg(0.7, E1), st('약화', 1), stk(K, 1)]),                                    // 거친 협상
      O([dmg(0.5, E1), st('취약', 2, E1), make(T1, 1)]),  // 선상 반란 진압
      O([dmg(0.6, E1), stk(K, 1), ifKill, make(GOLD, 1)]),                                       // 볼레! 약속은 지킨다
      O([st('약화', 2, E1), draw(1)], { cost: 0 }),                                   // 부하에게 떠넘기기
    ], [B('power'), B('weakSpot'), B(BK)], ['분쇄']),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '야아알~! 돌격!', cost: 200, fx: [dmg(0.5, TOP), dmg(0.9, EA), make(T1, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}

// ── 티그(영웅) — 표적형 · 딜러(엘다인) ──
// 엔진: 칼끝을 겨눈 적(티그에게 받는 피해 +8%/겹)을 두 번 베어 두면 → 「영웅의 길」 · 고학년 「맹호류 결착」이 약해진 그 적을 단칼에 · 처치 · 격파마다 영웅심(셋이면 다음 카드 +40%)
function tigHero(j) {
  // 팀: 「그 스승에 그 제자」 — 고학년 뒤 영웅심(디아나처럼 어린 수인을 가르치는 영웅)
  // 원작: 고학년 「맹호류 결착」(두 번 이상 맞은 적이 HP 10% 이하면 즉시 처치 · 보스엔 최대 HP 비례) · 저학년 「사슴류 베기」(배치열마다 다른 효과) · 염원살이 엘다인 · 영웅의 검
  const H = '티그_영웅', K = '영웅심', M = '겨눈 칼끝';
  const basics = [
    S(`${H}_s1`, '검 휘두르기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '롱소드 내려치기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '수인의 발놀림', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '훈련의 성과', 1, '공격', [dmg(0.6, E1), stk(M, 1), tough(1)], [                                // 시동 — 칼끝을 겨누다
      O([dmg(0.6, E1), stk(M, 1), tough(1)]),                                         // (수치)
      O([dmg(0.5, E1), stk(M, 2)]),                                                   // 두 번 베기
      O([dmg(0.5, E1), stk(K, 1), draw(1)]),                                          // 스승의 가르침
      O([dmg(0.7, E1), tough(2)]),  // 단련된 일격
      O([stk(M, 1), st('취약', 1, E1)], { cost: 0 }),                                 // 겨누기만
    ], null),
    U(`${H}_u1`, '사슴류 베기', 1, '공격', [dmg(0.3, EA, { hits: 2 }), stk(M, 1, LOW), stk(K, 1)], [              // 연료 — 원작 저학년(열마다 다른 효과)
      O([dmg(0.3, EA, { hits: 2 }), stk(M, 1, LOW), stk(K, 1)]),                      // (수치)
      O([dmg(0.3, EA, { hits: 2 }), sh(0.6)]),                                        // 전열(HP)
      O([dmg(0.3, EA, { hits: 2 }), gauge(15)]),                                      // 후열(고학년 앞당김)
      O([dmg(0.5, LOW, { hits: 2 }), stk(M, 1, LOW)]),                                // 약한 놈만
      O([dmg(0.25, EA, { hits: 2 }), stk(M, 1, EA)]),  // 모두 겨누기
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '영웅의 검', 2, '공격', [dmg(1.3, E1), per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)], [            // 결정타 — 어사이드 「영웅의 검」
      O([dmg(1.3, E1), per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)]),                 // (수치)
      O([dmg(1.0, EA), spendAll(K)]),                                                 // 영웅의 휩쓸기
      O([dmg(1.2, E1), per(K, { n: 1 }), dmg(0.25, E1)]),                             // 영웅심은 남긴다
      O([dmg(1.1, E1), ifFoe('hp', { pct: 0.3 }), dmg(1.2, E1)]),                     // 결착의 예감
      O([dmg(1.0, E1), per(K, { n: 1 }), dmg(0.25, E1), spendAll(K)], { cost: 1 }),   // 빠른 결착
    ], [B('power'), B('cost'), B(BK)], ['약점 공격']),
    U(`${H}_u4`, '영웅의 길', 1, '공격', [dmg(0.55, E1), ifStack(M, 2), dmg(0.6, E1), ifFoe('hp', { pct: 0.3 }), dmg(0.6, E1)], [ // 원작 자유 — 양아치에서 영웅으로
      O([dmg(0.55, E1), ifStack(M, 2), dmg(0.6, E1), ifFoe('hp', { pct: 0.3 }), dmg(0.6, E1)]), // (수치)
      O([dmg(0.6, E1), ifStack(M, 1), st('기절', 1, E1)]),                            // 첫째 길 — 기절
      O([dmg(0.5, EA), stk(K, 1)]),                                                   // 둘째 길 — 광역
      O([dmg(0.6, E1), heal(0.4), stk(K, 1)]),                                        // 셋째 길 — 아군 회복
      O([dmg(0.5, E1), ifStack(M, 2), dmg(0.9, E1)]),                                 // 슈로와의 유대
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '맹호류 결착', cost: 200, fx: [dmg(2.0, EA), tough(1, EA), ifFoe('hp'), dmg(1.0, E1), ifFoe('hpMob'), dmg(3.0, E1)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 헤일리 — 표적형 · 딜러 ──
// 엔진: 매 턴 적 하나를 외계인으로 착각(헤일리에게 받는 피해 +10%) → 외계인을 칠 때마다 추가 타격 · 동료가 치면 고통 · 쓰러지면 실드 · 드로우 · 다음 외계인
function hailey(j) {
  // 팀: 「외계인 격퇴」 — 동료가 외계인을 치면 고통 1(파티 전체가 착각에 가담)
  // 원작: 저학년 「논 그라타」(범위 · 고통 · 디버프 수만큼) · 고학년 「플랜 B」(범위 · 고통 · 약화) · 우주 전함 헤일리 · 엘프군 최전선 · 외계인 착각
  const H = '헤일리', K = '외계인';
  const basics = [
    S(`${H}_s1`, '채찍질', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '가죽 채찍 내려치기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '엄폐!', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u3`, '우주 전함 헤일리', 1, '스킬', [sh(0.8), stk(K, 1, TOP), draw(1)], [                          // 시동 — 외계인 발견!
      O([sh(0.8), stk(K, 1, TOP), draw(1)]),                                          // (수치)
      O([stk(K, 1, E1), draw(2)]),                                                    // 레이더 탐지
      O([sh(1.0), stk(K, 1, TOP), st('고통', 2, TOP)]),                               // 함포 조준
      O([stk(K, 1, TOP), draw(1)], { cost: 0 }),                                      // 배역 나눠 주기
      O([sh(0.8), stk(K, 1, TOP), st('피해 감소', 1)]),                               // 방어막 전개
    ], null),
    U(`${H}_u1`, '논 그라타', 1, '공격', [dmg(0.2, EA, { hits: 3 }), st('고통', 1, EA), stk(K, 1, E1)], [ // 연료 — 원작 저학년(범위 · 고통)
      O([dmg(0.2, EA, { hits: 3 }), st('고통', 1, EA), stk(K, 1, E1)]),  // (수치)
      O([dmg(0.18, EA, { hits: 3 }), st('고통', 2, EA)]),                              // 고통의 춤
      O([dmg(0.4, MK, { hits: 3 }), st('고통', 1, MK)]),                               // 외계인만 노려
      O([dmg(0.15, EA, { hits: 3 }), st('약화', 1, EA), draw(1)]),                     // 작전 변경
      O([st('고통', 2, EA), perDebuff, dmg(0.15, MK)], { cost: 0 }),                   // 채찍 소리만
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '플랜 A: 정면 돌파', 1, '공격', [dmg(0.8, MK), perDebuff, dmg(0.15, MK)], [                    // 결정타 — 외계인에게 정면으로
      O([dmg(0.8, MK), perDebuff, dmg(0.15, MK)]),                                    // (수치)
      O([dmg(1.0, E1), stk(K, 1, E1)]),                                               // 이 녀석이 외계인!
      O([dmg(0.7, MK), st('고통', 2, MK), st('취약', 1, MK)]),                        // 집중 포화
      O([perDebuff, dmg(0.2, MK)], { cost: 0 }),                                      // 기습 돌파
      O([dmg(0.7, MK), perDebuff, dmg(0.12, MK), ifKill, draw(2)]),                   // 격퇴 성공
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '엘프군 최전선', 1, '강화', [stk(K, 1, TOP), pw('hit', [st('고통', 1)])], [                    // 원작 자유 — 엘프군 장교의 착각
      O([stk(K, 1, TOP), pw('hit', [st('고통', 1)])]),                                // (수치)
      O([pw('hit', [st('고통', 1)])], { cost: 0 }),                                   // 최전선 근무
      O([stk(K, 1, TOP), pw('turnStart', [st('고통', 1, EA)])]),                      // 전선 확대
      O([pw('huntDown', [stk(K, 1, TOP), draw(1), sh(0.5)])], { cost: 0 }),           // 다음 외계인
      O([stk(K, 1, TOP), pw('hit', [st('고통', 1), sh(0.15)])]),                       // 엄폐 사격
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '플랜 B', cost: 250, fx: [dmg(1.1, EA), st('고통', 4, EA), st('약화', 2, EA)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 로니 — 표적형 · 딜러 ──
// 엔진: 가장 센 적에 현상금(로니에게 받는 피해 +12%/겹)을 걸고 공격마다 올린다 → 석양의 결투가 현상금을 다 받아 내고 · 현상범이 쓰러지면 파티 사기 + 다음 현상범
function roni(j) {
  // 팀: 현상범이 쓰러지면 이번 턴 파티 주는 피해 +20% · 현상범이 공격하려 하면 먼저 쏜다(석양의 결투)
  // 원작: 강화 평타(공격력 가장 높은 적을 「무법자」로 · 한 명뿐) · 저학년 「진압용 소닉붐」 · 고학년 「수사 종결」(HP 15% 이하 즉사 · 보스 제외) · 어사이드(무법자 처치 → 아군 피해↑) · 관 · 리코더 사건 · 막대사탕
  const H = '로니', K = '현상금';
  const spendT = { k: 'spend', id: K, all: true, target: E1 };
  const basics = [
    S(`${H}_s1`, '톤파 타격', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '톤파 내려찍기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '엄폐', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u5`, '오래가는 막대사탕', 0, '스킬', [stk(K, 1, E1), draw(1)], [                                    // 시동 — 수배서 붙이기
      O([stk(K, 1, E1), draw(1)]),                                                    // (수치)
      O([stk(K, 1, TOP), sh(0.4)]),                                                   // 가장 센 놈부터
      O([stk(K, 2, E1)]),  // 현상금 인상
      O([draw(2)]),  // 수사 자료
      O([stk(K, 1, E1), st('취약', 1, E1)]),                                          // 막대사탕 겨누기
    ], null),
    U(`${H}_u1`, '진압용 소닉붐', 1, '공격', [dmg(0.9, E1), stk(K, 2, E1)], [                                    // 연료 — 원작 저학년
      O([dmg(0.9, E1), stk(K, 2, E1)]),                                               // (수치)
      O([dmg(0.4, EA), stk(K, 1, EA)]),                                               // 일대 진압
      O([dmg(0.8, E1), stk(K, 1, E1), st('약화', 1, E1)]),                            // 침묵(행동 지연)
      O([dmg(1.1, E1), stk(K, 1, E1)]),                                               // 정의롭게 멍청한 한 방
      O([dmg(0.7, E1), stk(K, 2, E1), draw(1)], { cost: 2 }),                         // 잠복 수사
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '석양의 결투', 1, '공격', [dmg(0.9, E1), per(K, { n: 1 }), dmg(0.3, E1), spendT], [               // 결정타 — 현상금을 받아 낸다
      O([dmg(0.9, E1), per(K, { n: 1 }), dmg(0.3, E1), spendT]),                       // (수치)
      O([per(K, { n: 1 }), dmg(0.45, E1), spendT]),                                   // 한 발에 끝
      O([dmg(0.8, E1), per(K, { n: 1 }), dmg(0.25, E1)]),                             // 현상금은 둔다
      O([dmg(0.8, E1), per(K, { n: 1 }), dmg(0.25, E1), ifFoe('hpMob', { pct: 0.15 }), dmg(2.0, E1)], { cost: 2 }), // 수사 종결(즉사 문턱)
      O([dmg(0.8, E1), per(K, { n: 1 }), dmg(0.25, E1), ifKill, sh(0.8)]),             // 관 끌기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '부러진 리코더', 1, '공격', [dmg(0.8, E1), ifStack(K), st('취약', 2, E1)], [                    // 원작 자유 — 용서하지 못한 리코더 사건
      O([dmg(0.8, E1), ifStack(K), st('취약', 2, E1)]),                               // (수치)
      O([dmg(0.7, E1), stk(K, 1, E1), st('약화', 1, E1)]),                            // 리코더 증거물
      O([dmg(0.4, EA), ifStack(K), st('취약', 1, EA)]),                               // 일망타진
      O([dmg(1.0, E1), ifStack(K, 3), st('기절', 1, E1)]),                            // 용서는 없다
      O([dmg(0.7, E1), heal(0.3), stk(K, 1, E1)]),  // 황무지의 막대사탕
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '수사 종결', cost: 300, fx: [dmg(0.6, E1), st('피해 감소', 3), stk(K, 1, E1), ifFoe('hpMob'), dmg(4.0, E1)] },
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 실라 — 표적형 · 딜러 ──
// 엔진: 공격마다 정면 승부(받는 피해 +12%/겹)를 걸고 · 찍은 적에게 카드를 낼 때마다 하나씩 날린다 → 「정조준」이 셋 모인 적의 버팀을 깎는다 · 쓰러지면 가장 느린 적에게 다음 승부
function silla(j) {
  // 팀: 정면 승부가 걸린 적은 파티 누구에게나 더 아프다(받는 피해 +12%/겹)
  // 원작: 저학년 「래피드 샷」 · 고학년 「헥토파스칼 스윙!」 · 정령의 수호자 · 라이카가 과잉 충성하는 맏언니
  const H = '실라', K = '정면 승부';
  const basics = [
    S(`${H}_s1`, '화살', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '돌풍 화살', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '바람막이', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u3`, '바람 읽기', 0, '스킬', [stk(K, 1, E1), draw(1)], [                                              // 시동 — 승부를 건다
      O([stk(K, 1, E1), draw(1)]),                                                    // (수치)
      O([stk(K, 1, E1), sh(0.4)]),                                                    // 바람막이 너머
      O([stk(K, 2, E1)]),  // 정면으로 노려보기
      O([stk(K, 1, EA)]),  // 돌풍
      O([stk(K, 1, E1), cost(-1)]),                                                   // 하늬바람
    ], null),
    U(`${H}_u2`, '래피드 샷', 1, '공격', [dmg(0.28, E1, { hits: 2 }), ifStack(K), extra(0.5)], [                    // 연료 — 원작 저학년
      O([dmg(0.28, E1, { hits: 2 }), ifStack(K), extra(0.5)]),                        // (수치)
      O([dmg(0.25, E1, { hits: 3 }), stk(K, 1, E1)]),                                 // 연사
      O([dmg(0.3, ER, { hits: 2 }), stk(K, 1, ER)]),                                  // 흩어 쏘기
      O([dmg(0.28, E1, { hits: 2 }), draw(1)]),                                       // 쏘고 다음 화살
      O([dmg(0.3, E1, { hits: 2 }), st('둔화', 1, E1)]),  // 발 묶기
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u1`, '정조준', 1, '공격', [per(K, { n: 1 }), dmg(0.65, E1), ifStack(K, 3), tough(1), spendAll(K)], [  // 결정타 — 셋 모인 승부를 쏜다
      O([per(K, { n: 1 }), dmg(0.65, E1), ifStack(K, 3), tough(1), spendAll(K)]),     // (수치)
      O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)]),                               // 지금 쏘기
      O([per(K, { n: 1 }), dmg(0.55, E1)]),                                           // 승부는 남긴다
      O([per(K, { n: 1 }), dmg(0.55, E1), tough(2)]),  // 헥토파스칼 조준
      O([per(K, { n: 1 }), dmg(0.5, E1), spendAll(K), draw(1)]),                      // 다음 표적
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '맏언니의 각오', 1, '강화', [stk(K, 1, E1), pw('play', [extra(0.3)], { when: { type: '공격' } })], [ // 원작 자유 — 정령의 맏언니
      O([stk(K, 1, E1), pw('play', [extra(0.3)], { when: { type: '공격' } })]),        // (수치)
      O([pw('play', [extra(0.25)], { when: { type: '공격' } })], { cost: 0 }),        // 맏언니의 화살
      O([stk(K, 1, E1), pw('turnStart', [stk(K, 1, E1)])]),                           // 매일 승부
      O([pw('huntDown', [draw(2)])], { cost: 0 }),                                    // 정령의 수호자
      O([stk(K, 2, E1), pw('play', [extra(0.2)], { when: { type: '공격' } })]),  // 각오를 다지며
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '헥토파스칼 스윙!', cost: 150, fx: [dmg(2.3, E1), st('둔화', 2, E1), tough(2, E1)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '맞바람 한 발', when: { on: 'fightStart' }, fx: [stk(K, 1, TOP)] }];
}

// ── 롤렛 — 손 만들기형 · 딜러 ──
// 엔진: 턴마다 모자에서 비둘기 1장 · 버릴 때마다 갈채(주는 피해 +10%/개) → 다섯이면 봉봉한 피날레 1장 · 「모자 속 트릭」은 무엇이 나올지 모른다
function rollet(j) {
  // 팀: 판을 읽고 흐름을 비튼다 — 비둘기를 부활시키며 동료의 손을 돌린다(버리고 뽑기)
  // 원작: 저학년(비둘기 · 되살리는 마술로 무작위 아군 고학년 앞당김) · 고학년(상자 · 호기심 · 기절) · 모자 속에서 무엇이 나올지 모르는 길거리 마술사 · 「봉봉하게」
  const H = '롤렛', K = '갈채', DOVE = `${H}_dove`, FIN = `${H}_finale`;
  const basics = [
    S(`${H}_s1`, '마술봉', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '마술봉 대공연', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '트릭 망토', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [tok0(j, DOVE), tok0(j, FIN)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '비둘기 부활 마술', 0, '스킬', [disc(1), draw(2)], [                                            // 시동 — 버리고 다시
      O([disc(1), draw(2)]),                                                          // (수치)
      O([disc(1), make(DOVE, 1), draw(1)]),                                           // 비둘기가 돌아온다
      O([disc(2), draw(2), stk(K, 1)]),                                               // 크게 섞기
      O([disc(1), draw(1), gauge(15)]),                                               // 무작위 아군 고학년(원작)
      O([draw(2), stk(K, 1)]),  // 숨긴 카드
    ], null),
    U(`${H}_u2`, '갈채를 먹는 엔터테이너', 1, '공격', [dmg(0.65, EA), stk(K, 1)], [                                // 연료 — 원작 저학년(비둘기 · 피해)
      O([dmg(0.65, EA), stk(K, 1)]),                                                  // (수치)
      O([dmg(0.5, EA), make(DOVE, 1)]),                                               // 비둘기 날리기
      O([dmg(0.9, E1), stk(K, 2)]),                                                   // 한 사람 관객
      O([dmg(0.5, EA), disc(1), draw(1)]),                                            // 소매 속 트릭
      O([dmg(0.5, EA), st('둔화', 1, EA)]),                                           // 호기심(끌려옴)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '트릭 카드', 1, '공격', [dmg(0.6, E1), per(K, { n: 1 }), dmg(0.28, E1), spendAll(K)], [            // 결정타 — 갈채를 쏟는 카드
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.28, E1), spendAll(K)]),                // (수치)
      O([per(K, { n: 1 }), dmg(0.2, EA), spendAll(K)]),                               // 카드 비
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.25, E1)]),                             // 갈채는 둔다
      O([per(K, { n: 1 }), dmg(0.35, E1), spendAll(K), make(DOVE, 1)]),               // 카드에서 비둘기
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.22, E1), st('기절', 1, E1)], { cost: 2 }),  // 상자 속으로
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '모자 속 트릭', 1, '스킬', [roll(3), ifRoll(1), make(DOVE, 2), ifRoll(2), make(FIN, 1), ifRoll(3), stk(K, 2)], [ // 원작 자유 — 무엇이 나올지 모르는 모자
      O([roll(3), ifRoll(1), make(DOVE, 2), ifRoll(2), make(FIN, 1), ifRoll(3), stk(K, 2)]), // (수치)
      O([make(DOVE, 2), stk(K, 1)], { cost: 0 }),  // 정답은 안 알려 준다
      O([roll(2), ifRoll(1), make(FIN, 1), ifRoll(2), make(DOVE, 2)]),  // 큰 모자
      O([make(DOVE, 2)], { cost: 0 }),                                                // 비둘기 쌍
      O([roll(3), ifRoll(1), make(DOVE, 2), ifRoll(2), make(FIN, 1), ifRoll(3), draw(2)]),  // 베팅
    ], [B('draw'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '관객을 사로잡는 트릭스터', cost: 250, fx: [dmg(1.3, EA), st('둔화', 1, EA), st('기절', 1, ER), stk(K, 1)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 셰럼 — 손 만들기형 · 탱커 ──
// 엔진: 맞은 장면을 받아 적어 원고지 → 넷이면 「어둠 정령의 상처」 1장 · 「원고 속 인물」로 기사(실드) · 공주(회복) · 어둠 정령(광역) 가운데 하나가 걸어 나온다
function sherum(j) {
  // 팀: 원고 속 인물은 파티를 지킨다(기사 실드 · 공주 회복 · 결의)
  // 원작: 「쓴 것이 걸어 나온다」(벨라 · 잉클) · 저학년 「전승의 커튼」(받는 피해 7번 감소 · 회복량↑) · 고학년(회한의 영역 9회) · 강화 평타(직접 피해 4번마다 반격 · 고학년 앞당김) · 궁정 서기관
  const H = '셰럼', K = '원고지', T1 = `${H}_t1`, T2 = `${H}_t2`, T3 = `${H}_t3`;
  const basics = [
    S(`${H}_s1`, '깃펜 찌르기', 1, '공격', [ddef(0.45, E1)]),
    S(`${H}_s2`, '두꺼운 장부', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [tok0(j, T1), tok0(j, T2), tok0(j, T3)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '위치 아카이브', 1, '스킬', [sh(0.7), stk(K, 2)], [                                               // 시동 — 기록 보관소
      O([sh(0.7), stk(K, 2)]),                                                        // (수치)
      O([sh(0.5), stk(K, 1), draw(1)]),                                               // 서류 넘기기
      O([stk(K, 2), make(T2, 1)], { cost: 0 }),                                       // 기사 한 줄
      O([sh(0.9), stk(K, 1), st('피해 감소', 1)]),                                    // 두꺼운 기록
      O([sh(0.6), stk(K, 3)]),  // 천 페이지 미행 기록
    ], null),
    U(`${H}_u2`, '전승의 커튼', 1, '스킬', [sh(0.9), st('피해 감소', 2), stk(K, 1)], [                             // 연료 — 원작 저학년
      O([sh(0.9), st('피해 감소', 2), stk(K, 1)]),                                    // (수치)
      O([st('피해 감소', 3)], { cost: 0 }),                                           // 커튼만
      O([sh(0.7), heal(0.5), stk(K, 1)]),                                             // 회복량↑(원작)
      O([sh(0.7), st('피해 감소', 1), make(T3, 1)]),                                  // 공주 등장
      O([sh(0.6), pw('hurt', [stk(K, 1)])], { power: true }),                         // 예외 없이 받아 적기
    ], [B('guard'), B('heal'), B(BK)]),
    U(`${H}_u3`, '흑역사 낭독', 1, '공격', [per(K, { n: 1 }), ddef(0.22, EA), spendAll(K)], [          // 결정타 — 받아 적은 걸 낭독
      O([per(K, { n: 1 }), ddef(0.22, EA), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), ddef(0.4, E1), spendAll(K)]),  // 한 사람 흑역사
      O([per(K, { n: 1 }), sh(0.3), spendAll(K), make(T2, 1)]),  // 원고 방패
      O([per(K, { n: 1 }), ddef(0.18, EA), make(T2, 1)]),                             // 원고는 남긴다
      O([per(K, { n: 1 }), ddef(0.2, EA), spendAll(K), st('약화', 2, EA)]),  // 회한의 영역
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '원고 속 인물', 1, '스킬', [roll(3), ifRoll(1), make(T1, 1), ifRoll(2), make(T2, 2), ifRoll(3), make(T3, 2)], [ // 원작 자유 — 쓴 것이 걸어 나온다
      O([roll(3), ifRoll(1), make(T1, 1), ifRoll(2), make(T2, 2), ifRoll(3), make(T3, 2)]), // (수치)
      O([make(T2, 2), stk(K, 1)], { cost: 0 }),  // 기사 이야기
      O([make(T3, 2), stk(K, 1)], { cost: 0 }),  // 공주 이야기
      O([make(T1, 1), stk(K, 1), sh(0.5)], { cost: 0 }),  // 어둠 정령의 상처
      O([make(T2, 1), make(T3, 1), stk(K, 1)], { cost: 0 }),  // 기사와 공주
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '생생한 흑역사', cost: 250, fx: [ddef(0.3, EA), stk('회한의 영역', 2), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}

// ── 에슈르 — 손 만들기형 · 딜러 ──
// 엔진: 빵을 구워 손에 쌓고(먹으면 빵 센디오) → 빵 센디오가 있으면 공격마다 불타는 빵(추가 타격 · 고통) · 「궁극의 그리모어」가 쥔 빵만큼
function eshur(j) {
  // 팀: 갓 구운 빵은 회복도 한다(빵집이 요정 왕국 명물)
  // 원작: 저학년(빵 6개 날리기) · 고학년 「빵테오」(거대 케이크 · 기절 · 조각) · 어사이드(저학년 뒤 일반 공격 6회가 불타는 빵) · 빵집 아니고 마법학교
  const H = '에슈르', K = '빵 센디오', BR = `${H}_bread`;
  const basics = [
    S(`${H}_s1`, '마력탄', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '입자 마력탄', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '오븐 장갑', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [tok0(j, BR)];
  const BK = [make(BR, 1)];
  const uniques = [
    U(`${H}_u5`, '제빵 수업', 1, '스킬', [make(BR, 2), stk(K, 1)], [                                              // 시동 — 빵 굽기
      O([make(BR, 2), stk(K, 1)]),                                                    // (수치)
      O([make(BR, 1), draw(1)], { cost: 0 }),                                         // 한 판만
      O([make(BR, 3)], { cost: 2 }),                                                  // 오븐 가득
      O([stk(K, 2), draw(1), make(BR, 1)]),  // 반죽만 불 지피기
      O([make(BR, 2), heal(0.3)]),                                                    // 남은 빵은 못 버려
    ], null),
    U(`${H}_u1`, '빵템피드', 1, '공격', [dmg(0.35, E1, { hits: 2 }), stk(K, 1), make(BR, 1)], [                     // 연료 — 원작 저학년(빵 날리기)
      O([dmg(0.35, E1, { hits: 2 }), stk(K, 1), make(BR, 1)]),                        // (수치)
      O([dmg(0.2, ER, { hits: 4 }), stk(K, 1)]),  // 빵 여섯 개
      O([dmg(0.5, E1, { hits: 2 }), stk(K, 1)]),                                      // 딱딱한 바게트
      O([dmg(0.3, E1, { hits: 2 }), make(BR, 2)]),                                    // 한 판 더
      O([dmg(0.3, E1, { hits: 2 }), st('고통', 2, E1), stk(K, 1)]),  // 불타는 빵
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u4`, '궁극의 그리모어', 1, '공격', [dmg(0.6, E1), perTag(BR), dmg(0.4, E1)], [                          // 결정타 — 쥔 빵만큼 펼치는 마법서
      O([dmg(0.6, E1), perTag(BR), dmg(0.4, E1)]),                                    // (수치)
      O([perTag(BR), dmg(0.5, E1), exileTag(BR)]),                                    // 빵을 다 쏟아
      O([dmg(0.4, EA), perTag(BR), dmg(0.2, EA)]),                                    // 마법학교 강의
      O([dmg(0.5, E1), perTag(BR), heal(0.15), stk(K, 1)]),  // 시식
      O([dmg(0.5, E1), perTag(BR), dmg(0.35, E1), st('기절', 1, E1)], { cost: 2 }),   // 빵테오 예고
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '입자 이론', 0, '공격', [dmg(0.6, E1), ifStack(K), draw(1)], [                                     // 원작 자유 — 엘리아스 최상급 마법 학자
      O([dmg(0.6, E1), ifStack(K), draw(1)]),                                         // (수치)
      O([dmg(0.5, E1), stk(K, 1)]),                                                   // 이론 실습
      O([dmg(0.35, EA), stk(K, 1)]),  // 입자 확산
      O([dmg(0.5, E1), make(BR, 1)]),                                                 // 빵 입자
      O([dmg(0.5, E1), ifStack(K, 3), draw(2)]),                                      // 강의 듣는 학생
    ], [B('power'), B('ap'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '빵테오', cost: 250, fx: [dmg(2.8, E1), dmg(0.9, EA), st('기절', 1, E1)] },
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 제이드 — 손 만들기형 · 딜러 ──
// 엔진: 아끼는 책(보존 카드)은 쥔 채 턴을 넘길 때마다 비취옥(공격력 +12%/개) · 사본(필사본)을 베껴 내고 · 만들 때마다 비취옥 → 「게르마늄 옥장판」이 비취옥만큼 광역
function jade(j) {
  // 팀: 「초대 교주의 수양록」 — 베껴 쓴 필사본은 누구 손에서든 비취옥을 키운다
  // 원작: 비취옥 1~3중첩 · 3중첩에서 쓰면 다 잃음 · 어사이드(3중첩에 또 얻으면 마법서 형상) · 마요의 책을 베껴 사본만 돌려준 버릇 · 지식 독점
  const H = '제이드', K = '비취옥', COPY = `${H}_copy`;
  const basics = [
    S(`${H}_s1`, '옥구슬', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '비늘 막기', 1, '스킬', [sh(1.5)]),
  ];
  const tokens = [tok0(j, COPY)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '책에서 봤는데', 1, '스킬', [stk(K, 1), draw(2), handEnd, stk(K, 1)], [                            // 시동 — 쥐고 있는 책
      O([stk(K, 1), draw(2), handEnd, stk(K, 1)]),                                    // (수치)
      O([make(COPY, 1), draw(1), handEnd, stk(K, 1)]),                                // 베껴 두기
      O([draw(3)]),  // 책방 나들이
      O([stk(K, 2), handEnd, stk(K, 1)], { cost: 0 }),                                // 읽던 페이지
      O([stk(K, 1), draw(1), handEnd, stk(K, 2)]),                                    // 빌려주지 않는다
    ], null, ['보존']),
    U(`${H}_u1`, '게르마늄 옥장판', 2, '공격', [dmg(1.0, EA), per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)], [         // 결정타 — 원작 저학년(3중첩에서 쓰면 다 잃음)
      O([dmg(1.0, EA), per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)]),                // (수치)
      O([dmg(1.3, E1), per(K, { n: 1 }), dmg(0.5, E1), spendAll(K)]),                 // 한 장에 집중
      O([dmg(0.9, EA), per(K, { n: 1 }), dmg(0.3, EA)]),                              // 비취옥은 둔다
      O([per(K, { n: 1 }), dmg(0.45, EA), spendAll(K)], { cost: 1 }),                 // 휴대용 옥장판
      O([dmg(0.8, EA), per(K, { n: 1 }), dmg(0.3, EA), make(COPY, 1)]),  // 마법서 형상(어사이드)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '초대 교주의 수양록', 1, '공격', [dmg(0.6, E1), per(K, { n: 1 }), dmg(0.2, E1), make(COPY, 1)], [   // 연료 — 베껴 쓴 사본
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.2, E1), make(COPY, 1)]),               // (수치)
      O([make(COPY, 2), stk(K, 1)]),                                                  // 필사 작업
      O([dmg(0.7, E1), make(COPY, 1)]),                                               // 사본만 돌려주기
      O([dmg(0.5, EA), make(COPY, 1)], { cost: 2 }),                                  // 수양록 낭독
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.2, E1), draw(1)]),                     // 원본은 내 것
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u3`, '비취 구슬탄', 1, '공격', [dmg(1.1, E1), stk(K, 1), handEnd, stk(K, 1)], [                           // 원작 자유 — 포도맛 옥구슬
      O([dmg(1.1, E1), stk(K, 1), handEnd, stk(K, 1)]),                               // (수치)
      O([dmg(0.9, E1), make(COPY, 1)]),                                               // 구슬 사본
      O([dmg(0.5, EA), stk(K, 1)]),                                                   // 구슬 흩뿌리기
      O([dmg(1.0, E1), stk(K, 2)]),                                                   // 만병통치 사탕(시스트)
      O([dmg(1.0, E1), handEnd, stk(K, 2)]),                                          // 쥐고 버티기
    ], [B('power'), B('weakSpot'), B(BK)], ['보존']),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '게르마늄 탐지법', cost: 150, fx: [dmg(0.3, EA), stk(K, 3), { k: 'ap', v: 1 }] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, tokens, uniques });
}

// ── 우이 — 손 만들기형 · 서포터(엘다인) ──
// 엔진: 바라는 것이 현실이 된다 — 개굴비(0코 · 회복 + 적 전체 피해)를 손에 만들고 · 만들 때마다 행복 지수(다섯이면 파티 다음 카드 +40%) → 「개굴개굴 합창」이 쥔 개굴비만큼
function ui(j) {
  // 팀: 개굴비는 파티 회복 · 행복 지수가 차면 파티 다음 카드 강화
  // 원작: 저학년 개굴비(지속 회복 · SP · 적 피해) · 고학년 「말하는대로!」(서로 다른 셋에게 서로 다른 효과) · 바라는 것을 현실로 만드는 영원살이 막내 · 에루 · 네잎클로버
  const H = '우이', K = '행복 지수', T1 = `${H}_t1`;
  const basics = [
    S(`${H}_s1`, '빗방울', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '보슬보슬', 1, '스킬', [heal(1.3)]),
  ];
  const tokens = [tok0(j, T1)];
  const BK = [make(T1, 1)];
  const uniques = [
    U(`${H}_u1`, '개굴비 내리기', 1, '스킬', [make(T1, 2), heal(0.5)], [                                          // 시동 — 원작 저학년
      O([make(T1, 2), heal(0.5)]),                                                    // (수치)
      O([make(T1, 1), draw(1)], { cost: 0 }),                                         // 이슬비
      O([make(T1, 3)], { cost: 2 }),                                                  // 장대비
      O([make(T1, 2), stk(K, 1)]),                                                    // 에루의 우산
      O([make(T1, 1), heal(0.4), cleanse(1)]),                                        // 싫은 건 안 보면 돼
    ], null),
    U(`${H}_u3`, '개굴개굴 합창', 1, '공격', [dmg(0.45, EA), perTag(T1), dmg(0.25, EA)], [                            // 결정타 — 쥔 개굴비만큼 합창
      O([dmg(0.45, EA), perTag(T1), dmg(0.25, EA)]),                                  // (수치)
      O([perTag(T1), dmg(0.35, EA), exileTag(T1)]),                                   // 다 같이 개굴
      O([dmg(0.4, EA), perTag(T1), heal(0.12), stk(K, 1)]),  // 합창 뒤 회복
      O([dmg(0.7, E1), perTag(T1), dmg(0.35, E1)]),                                   // 한 사람에게 세레나데
      O([dmg(0.4, EA), make(T1, 1)]),                                                 // 앙코르
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '에루와 단짝', 1, '스킬', [make(T1, 1), perTag(T1), heal(0.15)], [                        // 연료 — 처음 깨어났을 때 앞에 나타난 에루
      O([make(T1, 1), perTag(T1), heal(0.15)]),  // (수치)
      O([make(T1, 1), stk(K, 2)]),                                                    // 단짝과 놀기
      O([perTag(T1), heal(0.2), stk(K, 1)], { cost: 0 }),                             // 에루 쓰다듬기
      O([make(T1, 2), draw(1)]),                                                      // 에루가 데려온 친구
      O([make(T1, 1), heal(0.4), st('피해 감소', 1)]),                                // 에루의 우산
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u5`, '선생님한테 고자질', 1, '스킬', [make(T1, 1), draw(2)], [                                            // 원작 자유 — 막내의 고자질
      O([make(T1, 1), draw(2)]),                                                      // (수치)
      O([draw(2), stk(K, 1)], { cost: 0 }),                                           // 몰래 고자질
      O([make(T1, 1), st('약화', 1, EA)]),                                            // 혼내 주세요
      O([make(T1, 2), heal(0.3), stk(K, 1)]),                                                    // 네잎클로버
      O([draw(3)]),  // 기억 지우기
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '말하는대로!', cost: 250, fx: [cleanse(3), heal(2.0), st('면역', 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}
const cleanse = v => ({ k: 'cleanse', v });

export const JOBS = [['수인/그윈', gwin], ['수인/란', ran], ['수인/스패럿', sparrot], ['수인/티그_영웅', tigHero], ['엘프/헤일리', hailey], ['요정/로니', roni], ['정령/실라', silla],
  ['마녀/롤렛', rollet], ['마녀/셰럼', sherum], ['요정/에슈르', eshur], ['용족/제이드', jade], ['정령/우이', ui]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
