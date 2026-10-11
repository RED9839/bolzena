// 묶음 8 — 성장형 4 + 계산형 4 + 소멸형 4. node _gen/deep/b8.mjs [사도 일부]
// 고유 효과 · 패시브는 10-08 갈래 재설계(원작 근거)를 이어 쓰는 사도가 많다(kw0 · pas0). 고유 4장 · 신탁 · 기본 카드 · 고학년은 새로.
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, FAR, ALLY, ALLIES, OTHERS, dmg, ddef, sh, heal, st, stk, spendAll, spendN, per, perTag, draw, make, disc, ap, gauge,
  ifStack, ifKill, empower, tough, dmod, cost, pw, pas, O, B, U, S, T, hero } = L;
const cleanse = v => ({ k: 'cleanse', v });
const handEnd = { k: 'when', on: 'handEnd' };
const ifSpent = n => ({ k: 'ifSpent', n }), ifBal = { k: 'ifBalanced' };
const perGone = { k: 'perGone', who: 'self' }, perCS = (id, n) => ({ k: 'perCardSt', id, ...(n ? { n } : {}) });
const grow = (id, max) => ({ k: 'cardStatus', id, v: 1, max });
const ifGained = (id, n) => ({ k: 'ifGained', id, n });
const burn = v => ({ k: 'burn', v });
const kw0 = j => { const h = j.heroes[0]; return { keyword: h.keyword, ...(h.keywords ? { keywords: h.keywords } : {}) }; };
const pas0 = j => j.heroes[0].passives;
const forms0 = j => j.heroes[0].forms;
const tok0 = (j, id) => j.cards.find(c => c.id === id);
const fx0 = (j, id) => JSON.parse(JSON.stringify(j.cards.find(c => c.id === id).fx));

// ── 레비(졸업) — 성장형 · 탱커 ──
// 엔진: 동료가 공격할 때마다 롤모델 따라 하기(신입의 패기 — 넷이면 다음 카드 +40%) → 정시 퇴근이 패기만큼 실드 · 「논문 초안」은 이번 전투 패기를 여섯 넘게 쌓았으면 게재 +1(판 내내 실드↑)
function leviGrad(j) {
  // 팀: 「롤모델 찾기」 — 공격력 가장 높은 아군에게 피해 감소 + 다음 카드 강화 · 동료의 공격이 레비의 패기
  // 원작: 노력천재 · 논문 반려 → 통과 · 저학년(각성 드링크 · 범위 · 적 피해↓ · 아군 최대 HP↑) · 강화 공격 「롤모델 찾기」(가장 센 아군 무적 + 피해↑) · 고학년 「열정 넘치는 신입」
  const H = '레비_졸업', K = '신입의 패기';
  const basics = [
    S(`${H}_s1`, '빗자루 후리기', 1, '공격', [ddef(0.6, E1)]),
    S(`${H}_s2`, '사원증 가드', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '출근 도장', 1, '스킬', [stk(K, 2), sh(0.7)], [                                                   // 시동
      O([stk(K, 2), sh(0.7)]),                                                       // (수치)
      O([stk(K, 1), sh(0.5), draw(1)]),                                              // 이른 출근
      O([stk(K, 2), heal(0.4)]),                                                     // 각성 드링크
      O([stk(K, 3)]),  // 야근
      O([stk(K, 1), sh(0.9)], { cost: 1 }),                                          // 사원증 단단히
    ], null),
    U(`${H}_u2`, '패기 가득한 인재', 1, '공격', [ddef(0.3, EA), st('약화', 1, EA), stk(K, 1)], [                       // 연료 — 원작 저학년(범위 · 적 피해↓)
      O([ddef(0.3, EA), st('약화', 1, EA), stk(K, 1)]),                               // (수치)
      O([ddef(0.5, E1), stk(K, 2)]),                                                  // 한 명에게 열정
      O([ddef(0.25, EA), heal(0.4), stk(K, 1)]),  // 각성 드링크 회복
      O([ddef(0.25, EA), st('약화', 1, EA), draw(1)]),                                // 이력서에 이것도
      O([st('약화', 2, EA), stk(K, 1)], { cost: 0 }),                                 // 견습 가이드북
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '정시 퇴근', 1, '스킬', [sh(0.7), per(K, { n: 1 }), sh(0.3), spendAll(K)], [                            // 결정타 — 패기를 실드로
      O([sh(0.7), per(K, { n: 1 }), sh(0.3), spendAll(K)]),                          // (수치)
      O([per(K, { n: 1 }), heal(0.25), spendAll(K), draw(1)]),  // 퇴근 후 휴식
      O([sh(0.6), per(K, { n: 1 }), sh(0.25)]),                                      // 패기는 남긴다
      O([per(K, { n: 1 }), ddef(0.2, EA), spendAll(K)]),                             // 퇴근길 빗자루
      O([sh(0.6), per(K, { n: 1 }), sh(0.25), draw(1)]),  // 내일 할 일
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u5`, '논문 초안', 1, '스킬', fx0(j, `${H}_u5`), [                                                         // 원작 자유 — 반려 끝에 통과한 논문(판 내내 자람)
      O(fx0(j, `${H}_u5`)),                                                          // (수치)
      O([sh(0.6), perCS('게재'), sh(0.3), draw(1)]),                                 // 참고문헌 정리
      O([heal(0.5), perCS('게재'), heal(0.2), stk(K, 1)]),  // 약초 탐지 마법
      O([sh(0.5), stk(K, 1), ifGained(K, 4), grow('게재', 5)]),                      // 조금 낮은 문턱
      O([sh(0.6), perCS('게재'), sh(0.3), stk(K, 1)]),                               // 주도적인 성장
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '열정 넘치는 신입', cost: 200, fx: [ddef(0.5, EA), heal(2.2), st('피해 감소', 2), stk('어수선한 영역', 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, uniques });
}

// ── 비비(신성) — 성장형 · 딜러(엘다인) ──
// 엔진: 동료가 공격할 때마다 새싹 → 여섯이면 파티 다음 카드 +40% · 「셋의 몫」을 내지 않고 손에 품은 채 턴을 넘기면 잎이 자란다(판 내내) → 모두의 빛이 새싹만큼
function bibiHoly(j) {
  // 팀: 「함께 걷는 세상」 — 동료의 공격이 새싹을 키운다 · 새싹이 차면 파티 다음 카드 강화
  // 원작: 새 세계수(영원의 새싹) · 저학년 「모두의 빛」(아군 공격력↑ · 적 전체 여러 번) · 고학년(신성 — 아군이 쓰러지면 회복 · 아군 피해 크게↑) · 「사랑받은 새싹」 아티팩트 · 연못가 대화
  const H = '비비_신성', K = '새싹';
  const basics = [
    S(`${H}_s1`, '빛구슬', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '빛구슬 세례', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '세계수 잎사귀', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '손잡기', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], [                                    // 시동 — 혼자가 아니야
      O([stk(K, 1), draw(1, { who: 'other' })]),                                    // (수치)
      O([stk(K, 1), sh(0.5)]),                                                       // 날개 대신 손
      O([stk(K, 2)]),  // 꼭 잡은 손
      O([draw(2)]),  // 연못가 대화
      O([stk(K, 1), dmod(0.1, OTHERS)]),                                             // 다정한 세상
    ], null),
    U(`${H}_u1`, '셋의 몫', 1, '공격', fx0(j, `${H}_u1`), [                                                            // 연료 — 품어 가꾸는 새싹(판 내내 잎)
      O(fx0(j, `${H}_u1`)),                                                          // (수치)
      O([dmg(0.4, E1), stk(K, 1), handEnd, grow('잎', 5)]),                          // 바람을 견딘 잎
      O([dmg(0.3, EA), perCS('잎', 1), dmg(0.1, EA)]),                               // 나눠 주는 잎
      O([sh(0.5), perCS('잎', 1), sh(0.2), handEnd, grow('잎', 5)], { cost: 0 }),  // 잎 그늘
      O([dmg(0.4, E1), perCS('잎', 1), dmg(0.15, E1), draw(1)]),                     // 새싹에게 말 걸기
    ], [B('power'), B('cost'), B(BK)], ['보존']),
    U(`${H}_u2`, '모두의 빛', 2, '공격', [dmg(0.4, EA), per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)], [               // 결정타 — 원작 저학년
      O([dmg(0.4, EA), per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)]),                // (수치)
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.25, E1), spendAll(K)]),                // 한 사람을 비추는 빛
      O([dmg(0.4, EA), per(K, { n: 1 }), dmg(0.12, EA)]),                             // 새싹은 둔다
      O([per(K, { n: 1 }), dmg(0.2, EA), dmod(0.15, OTHERS)]),           // 아군 공격력↑(원작)
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)], { cost: 1 }),                 // 작은 빛
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '새 세계수의 서약', 1, '강화', [stk(K, 1), pw('turnStart', [stk(K, 1)])], [                           // 원작 자유 — 어머니를 용서하고 세운 서약
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])]),                                  // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                // 매일 물 주기
      O([stk(K, 2), pw('turnStart', [sh(0.3)])]),                                    // 세계수의 그늘
      O([stk(K, 1), pw('turnStart', [stk(K, 1), heal(0.15)])], { cost: 2 }),         // 신성(쓰러짐 대신 회복)
      O([stk(K, 1), pw('grow', [stk(K, 1)])]),                                       // 자라는 것을 기뻐하며
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '모든 이를 굽어살피리', cost: 300, fx: [dmg(2.2, EA), st('피해 감소', 3), st('사기', 1)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 에슈르(마도) — 성장형 · 딜러 ──
// 엔진: 카드를 소멸시킬 때마다(실패작 처리) 마력 증폭(주는 피해 +20%/개) → 연구 집중은 이번 전투 증폭을 넷 넘게 얻었으면 연구 +1(판 내내) · 마력 레이저가 증폭을 다 쏟는다 · 거울 형상이 공격마다 따라 쏜다
function eshurMage(j) {
  // 팀: 시험작 처리 — 소멸 계기를 동료와 나눈다(누가 지우든 연구 노트가 쌓이는 건 에슈르)
  // 원작: 다른 차원의 대-에슈르(학생 말고 자신에게 집중하라) · 저학년 「빵타지아」(마력 증폭 · 평타가 범위) · 고학년(거울 분신 · 연구 집중) · 「오늘도 마법 빵 실패! 그래도 계속 도전」
  const H = '에슈르_마도', K = '마력 증폭', M = '거울 형상';
  const basics = [
    S(`${H}_s1`, '마력 구체', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '왕마력 구체', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '로브 두르기', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '마법 빵 시험작', 0, '스킬', [draw(2), stk(K, 1)], [                                                 // 시동 — 태우는 연료
      O([draw(2), stk(K, 1)]),                                                       // (수치)
      O([draw(1), stk(M, 1)]),                                                       // 거울에 비친 빵
      O([stk(K, 2), sh(0.3)]),  // 실패작이라도
      O([draw(2), sh(0.4)]),                                                         // 밤샘 스터디
      O([draw(1), stk(K, 1), gauge(10)]),                                            // 가로등에 쿵
    ], null, ['소멸']),
    U(`${H}_u3`, '빵타지아', 1, '공격', [dmg(0.6, EA), st('취약', 1, EA), stk(K, 2)], [                                // 연료 — 원작 저학년(마력 증폭)
      O([dmg(0.6, EA), st('취약', 1, EA), stk(K, 2)]),                               // (수치)
      O([dmg(0.9, E1), stk(K, 2)]),                                                  // 한 점 집중
      O([dmg(0.5, EA), stk(M, 1)]),                                                  // 거울 분신과 함께
      O([dmg(0.5, EA), stk(K, 1), draw(1)]),                                         // 연구 노트 펼치기
      O([dmg(0.45, EA), st('취약', 2, EA), stk(K, 1)]),  // 궁극의 마법 빵
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '마력 레이저', 2, '공격', [per(K, { n: 1 }), dmg(0.9, E1), spendAll(K)], [               // 결정타 — 증폭을 다 쏟는다
      O([per(K, { n: 1 }), dmg(0.9, E1), spendAll(K)]),               // (수치)
      O([dmg(1.0, EA), stk(M, 1)]),  // 확산 레이저
      O([dmg(1.5, E1), per(K, { n: 1 }), dmg(0.4, E1)]),                             // 증폭은 둔다
      O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)], { cost: 1 }),                 // 압축 레이저
      O([dmg(1.4, E1), per(K, { n: 1 }), dmg(0.35, E1), stk(M, 1)]),  // 거울에 반사
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '연구 집중', 1, '공격', fx0(j, `${H}_u4`), [                                                          // 원작 자유 — 판 내내 쌓이는 연구(대마법 연구 노트)
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([dmg(0.5, EA), perCS('연구', 1), dmg(0.12, EA)]),                            // 연구 발표
      O([dmg(0.6, E1), stk(K, 1), ifGained(K, 3), grow('연구', 6)]),                 // 조금 낮은 문턱
      O([dmg(0.6, E1), perCS('연구', 1), dmg(0.2, E1), draw(1)]),                    // 전기 충격 자청
      O([perCS('연구', 1), dmg(0.35, E1), stk(M, 1)], { cost: 0 }),                  // 대-에슈르의 교훈
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '마도학자의 길', cost: 200, fx: [dmg(0.9, EA), stk(K, 2), stk(M, 3)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 다야 — 성장형 · 딜러 ──
// 엔진: 「갈고, 닦고」로 손의 공격 카드에 광택(판 내내) → 다이아 피어스가 광택만큼 · 쓰라림(지속 피해)을 박아 두고 · 처치하면 원석
function daya(j) {
  // 팀: 다이아 쓰라림(턴 끝 지속 피해) — 동료의 공격 사이 적이 계속 깎인다
  // 원작: 용족 수장 · 「완벽해지기」 · 저학년(다이아를 솟게 해 여러 적 · 쓰라림 · 처치하면 다시) · 망치로 두들기기 전엔 다이아와 유리를 못 가림 · 「갈고, 닦고, 광내자!」
  const H = '다야', K = '다이아 쓰라림', GEM = `${H}_gem`;
  const basics = [
    S(`${H}_s1`, '보석 파편', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '다이아 비늘', 1, '스킬', [sh(1.5)]),
  ];
  const tokens = [tok0(j, GEM), tok0(j, `${H}_real`)];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u3`, '갈고, 닦고', 1, '스킬', fx0(j, `${H}_u3`), [                                                         // 시동 — 광내자!
      O(fx0(j, `${H}_u3`)),                                                          // (수치)
      O([{ k: 'cardStatus', id: '광택', v: 1, to: 'hand', n: 1, who: 'self', type: '공격', max: 5 }, draw(2)]), // 꼼꼼한 손질
      O([make(GEM, 2), draw(1)]),                                                    // 원석 캐기
      O([{ k: 'cardStatus', id: '광택', v: 2, to: 'hand', n: 1, who: 'self', type: '공격', max: 5 }, draw(1), make(GEM, 1)]),  // 파워 그라인더
      O([{ k: 'cardStatus', id: '광택', v: 1, to: 'hand', n: 1, who: 'self', type: '공격', max: 5 }, stk(K, 2, E1), draw(2)]),  // 완벽한 커팅
    ], null),
    U(`${H}_u2`, '다이아 박기', 1, '공격', [dmg(1.0, E1), stk(K, 2, E1)], [                                         // 연료 — 쓰라림 박기
      O([dmg(1.0, E1), stk(K, 2, E1)]),                                              // (수치)
      O([dmg(0.5, EA), stk(K, 1, EA)]),                                              // 솟는 다이아
      O([dmg(0.9, E1), make(GEM, 1)]),                                               // 쪼개진 원석
      O([dmg(0.8, E1), stk(K, 2, E1), tough(1)]),                                    // 망치 감별
      O([dmg(1.1, E1), st('취약', 1, E1)]),                                          // 다이아 옥좌 끌기
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u1`, '다이아 피어스', 2, '공격', fx0(j, `${H}_u1`), [                                                       // 결정타 — 원작 저학년(처치하면 다시)
      O(fx0(j, `${H}_u1`)),                                                          // (수치)
      O([dmg(1.8, E1), perCS('광택'), dmg(0.4, E1), ifKill, make(GEM, 1)]),           // 한 놈 관통
      O([dmg(1.2, EA), perCS('광택'), dmg(0.22, EA), stk(K, 1, EA)]),                 // 쓰라림 확산
      O([dmg(0.9, EA), perCS('광택'), dmg(0.2, EA)], { cost: 1 }),                    // 작은 피어스
      O([dmg(1.2, EA), ifKill, make(`${H}_real`, 1)]),                                // 진품 다이아
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '완벽해지는 소원', 1, '공격', [per(K, { n: 1 }), dmg(0.5, E1), { k: 'cardStatus', id: '광택', v: 1, to: 'hand', n: 1, who: 'self', type: '공격', max: 5 }], [                                                     // 원작 자유 — 소원은 「완벽해지기」
      O([per(K, { n: 1 }), dmg(0.5, E1), { k: 'cardStatus', id: '광택', v: 1, to: 'hand', n: 1, who: 'self', type: '공격', max: 5 }]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)]),                 // 쓰라림을 깨뜨려
      O([dmg(0.8, E1), stk(K, 2, E1)]),                                               // 노력파의 일격
      O([dmg(0.7, E1), per(K, { n: 1 }), dmg(0.3, E1), draw(1)]),                     // 조금 더 노력해야겠어
      O([dmg(0.5, EA), stk(K, 1, EA), draw(1)]),  // 끊임없는 노력의 결과
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '다이아 브레…츄!', cost: 250, fx: [dmg(1.8, EA), tough(1, EA), st('고통', 2, EA), make(GEM, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`], basics, tokens, uniques });
}

// ── 루포 — 계산형 · 딜러 ──
// 엔진: 턴마다 셈(낸 장수 · 쓴 AP)이 맞으면 계획 + 드로우 → 넷이면 「계획대로!」 · 오의 여우 꼬리가 계획만큼 · 작전 쪽지로 계획을 고쳐 쓴다
function lupo(j) {
  // 팀: 사료스탕스의 브레인 — 작전 쪽지(0코 · 계획 + 드로우)를 손에 · 셈이 맞으면 모두의 다음 수가 쉬워진다
  // 원작: 저학년 「루포류 신속베기」(아티팩트 수만큼 타수) · 고학년 「오의 여우회전!」(8회 · 받는 피해↓) · 강화 평타(눈속임) · 보드게임광 · 작전이 자주 빗나감 · 룰을 슬쩍 바꿔 이김
  const H = '루포', K = '계획', MEMO = `${H}_memo`, PLAN = `${H}_plan`;
  const basics = [
    S(`${H}_s1`, '단검 찌르기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '단검 급소 찌르기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '몸 숙이기', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [tok0(j, MEMO), tok0(j, PLAN)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '작전 지도 펼치기', 1, '스킬', [stk(K, 1), make(MEMO, 1), draw(1)], [                                 // 시동 — 작전 수립
      O([stk(K, 1), make(MEMO, 1), draw(1)]),                                        // (수치)
      O([make(MEMO, 2)], { cost: 0 }),                                               // 쪽지 두 장
      O([stk(K, 2), sh(0.5), draw(1)]),  // 똑똑해지는 약
      O([stk(K, 1), draw(2), make(MEMO, 1)]),  // 판을 넓게
      O([stk(K, 1), cost(-1), draw(1)], { cost: 0 }),  // 룰 슬쩍 바꾸기
    ], null),
    U(`${H}_u1`, '루포류 신속베기', 1, '공격', [dmg(0.25, E1, { hits: 3 }), st('고통', 2, E1), stk(K, 1)], [                  // 연료 — 원작 저학년(타수가 는다)
      O([dmg(0.25, E1, { hits: 3 }), st('고통', 2, E1), stk(K, 1)]),                 // (수치)
      O([dmg(0.2, E1, { hits: 4 }), stk(K, 1)]),  // 한 번 더 베기
      O([dmg(0.25, EA, { hits: 2 }), stk(K, 1)]),                                    // 여우회전
      O([dmg(0.25, E1, { hits: 3 }), make(MEMO, 1)]),                                // 베며 쪽지
      O([dmg(0.22, E1, { hits: 3 }), st('고통', 3, E1)]),                            // 독 바른 단검
    ], [B('power'), B('weakSpot'), B(BK)], ['신속']),
    U(`${H}_u4`, '오의 여우 꼬리', 2, '공격', [dmg(2.4, E1), per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)], [                   // 결정타 — 계획대로!
      O([dmg(2.4, E1), per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)]),                // (수치)
      O([dmg(1.1, EA), stk(K, 2)]),  // 여우회전 대오의
      O([dmg(2.0, E1), per(K, { n: 1 }), dmg(0.5, E1)]),                             // 계획은 남긴다
      O([per(K, { n: 1 }), dmg(0.9, E1), spendAll(K)], { cost: 1 }),                 // 꼼수 꼬리
      O([dmg(2.0, E1), per(K, { n: 1 }), dmg(0.5, E1), make(PLAN, 1)]),  // 다음 수까지
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '눈속임 단검', 1, '공격', fx0(j, `${H}_u3`), [                                                        // 원작 자유 — 강화 평타(눈속임)
      O(fx0(j, `${H}_u3`)),                                                          // (수치)
      O([dmg(1.0, E1), st('회피', 1)]),                                              // 대상이 안 됨
      O([dmg(0.9, E1), ifSpent(3), make(PLAN, 1)]),                                  // 셈이 맞으면 계획대로
      O([dmg(1.0, E1), make(MEMO, 1)]),                                              // 쪽지 바꿔치기
      O([dmg(0.5, EA), stk(K, 1)]),  // TRPG 깽판
    ], [B('power'), B('ap'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '오의 여우회전!', cost: 200, fx: [dmg(0.2, EA, { hits: 4 }), st('피해 감소', 2), stk(K, 1)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 림 — 계산형 · 딜러 ──
// 엔진: 이번 턴 공격과 스킬을 같은 장수로 맞추면 저울이 선다(셈) → 저울이 선 카드마다 회복 · 드로우 · AP · 공격마다 낫 자국(지속 피해)
function rim(j) {
  // 팀: 「무게추」 — 셈이 맞으면 파티 회복 · 적 전체 낫 자국(종족의 실없는 짓을 반대편에서 누르는 무게추)
  // 원작: 균형의 유령 · 저학년(어둠 · 자기 회복 · 범위 참격 · 쓰라림) · 고학년 「그림 하베스트」(2회 범위 · 피해 비례 회복) · 낫 「기역」 · 썰렁 아재개그
  const H = '림', K = '낫 자국';
  const basics = [
    S(`${H}_s1`, '낫 베기', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '망령 장막', 1, '스킬', [sh(1.8)]),
    S(`${H}_s3`, '대낫 휘두르기', 2, '공격', [dmg(2.3, E1)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u4`, '균형의 저울', 0, '스킬', fx0(j, `${H}_u4`), [                                                       // 시동 — 저울 맞추기
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([stk(K, 1, EA), heal(0.3), draw(1)]),  // 슴슴한 호박 스프
      O([draw(2), ifBal, ap(1)], { cost: 0 }),  // 추 고르기
      O([stk(K, 2, E1), ifBal, draw(1)]),                                            // 기역에게 묻기
      O([stk(K, 1, EA), sh(0.4), draw(1)]),  // 혼자 가면 쓰고
    ], null),
    U(`${H}_u1`, '스크래치 사이드', 1, '공격', fx0(j, `${H}_u1`), [                                                      // 연료 — 원작 저학년
      O(fx0(j, `${H}_u1`)),                                                          // (수치)
      O([dmg(0.9, E1), stk(K, 2, E1), ifBal, heal(0.4)]),                            // 한 놈 베기
      O([dmg(0.6, EA), stk(K, 1, EA), draw(1)]),                                     // 어둠이 내리며
      O([dmg(0.6, EA), ifBal, st('약화', 1, EA)]),                                   // 넉백
      O([dmg(0.5, EA), stk(K, 2, EA), draw(1)]),  // 그림 하베스트 예고
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '기역의 추수', 1, '공격', fx0(j, `${H}_u3`), [                                                        // 결정타 — 낫 자국을 거둔다
      O(fx0(j, `${H}_u3`)),                                                          // (수치)
      O([per(K, { n: 1, each: true }), dmg(0.25, EA), spendAll(K)]),                 // 전부 추수
      O([per(K, { n: 1 }), dmg(0.45, E1), ifBal, heal(0.4)]),                        // 저울이 서면 남긴다
      O([per(K, { n: 1 }), dmg(0.45, E1), spendAll(K), draw(1)]),                    // 다음 이삭
      O([per(K, { n: 1 }), dmg(0.4, E1), heal(0.3)]),  // 피해 비례 회복(고학년)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '날아가는 기역', 1, '공격', fx0(j, `${H}_u2`), [                                                      // 원작 자유 — 친구 1호 낫 「기역」
      O(fx0(j, `${H}_u2`)),                                                          // (수치)
      O([dmg(0.5, EA), stk(K, 1, EA)]),                                              // 돌아오는 낫(애착판)
      O([dmg(0.9, E1), stk(K, 2, E1), sh(0.4)]),                                     // 막고 던지기
      O([dmg(1.0, E1), ifBal, ap(1)]),                                               // 썰렁한 아재개그
      O([dmg(0.7, E1), stk(K, 3, E1)]),  // 깊은 낫 자국
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '그림 하베스트', cost: 300, fx: [dmg(0.6, EA, { hits: 2 }), heal(3.0), stk(K, 3, EA)] },
    starter: [`${H}_s1`, `${H}_u4`, `${H}_s3`, `${H}_s2`], basics, uniques });
}

// ── 앨리스 — 계산형 · 딜러 ──
// 엔진: 맞을 때마다 행운(주는 피해 +8%/개) · 이번 턴 AP 를 셋 넘게 쓰면 점괘가 맞는다(아르카나 · 밑장 빼기 · 뒤집힌 카드가 덤) → 남의 불운이 행운만큼
function alice(j) {
  // 팀: 턴 시작마다 아르카나 한 장(사기 · 적 전체 취약 · 행운 가운데) — 파티 전체의 운을 점친다
  // 원작: 저학년 「아르카나」(우산 지참 · 잔불 조심 · 생채기 주의) · 고학년 「액땜」(가장 최근 점괘를 더 세게) · 어사이드(아군이 12번 맞으면 HP · SP) · 밑장 빼기 타로
  const H = '앨리스', K = '행운', TAROT = `${H}_tarot`;
  const basics = [
    S(`${H}_s1`, '카드 날리기', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '운명 회피', 1, '스킬', [sh(1.8)]),
    S(`${H}_s3`, '카드 한 벌 날리기', 2, '공격', [dmg(2.3, E1)]),
  ];
  const tokens = [tok0(j, TAROT)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '비추는 거울', 1, '강화', [stk(K, 2), pw('turnStart', [stk(K, 1)])], [                               // 시동 — 운명을 비추는 거울
      O([stk(K, 2), pw('turnStart', [stk(K, 1)])]),                                  // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                // 흐린 거울
      O([stk(K, 1), pw('turnStart', [make(TAROT, 1)])]),                             // 매일 한 장
      O([stk(K, 2), pw('hurt', [stk(K, 1)])]),  // 완전 럭키(어사이드)
      O([stk(K, 1), pw('turnStart', [stk(K, 1), draw(1)])]),  // 늪 입구 문지기
    ], null, ['개전']),
    U(`${H}_u1`, '아르카나', 1, '공격', fx0(j, `${H}_u1`), [                                                          // 연료 — 원작 저학년(점괘 셋)
      O(fx0(j, `${H}_u1`)),                                                          // (수치)
      O([dmg(0.5, EA), st('충격', 1, EA)]),                                          // 우산 지참(감전)
      O([dmg(0.5, EA), st('잔불', 2, EA)]),  // 잔불 조심(화상)
      O([dmg(0.4, EA), stk(K, 1), ifSpent(2), st('약점 노출', 1, ER)]),  // 생채기 주의(약점 노출)
      O([dmg(0.5, EA), stk(K, 1), draw(1)]),                                         // 점괘 하나 더
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '남의 불운', 1, '공격', [dmg(0.8, E1), per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)], [                  // 결정타 — 행운을 남의 불운으로
      O([dmg(0.8, E1), per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)]),                // (수치)
      O([per(K, { n: 1 }), dmg(0.22, EA), spendAll(K), draw(1)]),  // 모두의 불운
      O([dmg(0.7, E1), per(K, { n: 1 }), dmg(0.25, E1)]),                            // 행운은 지킨다
      O([dmg(0.7, E1), per(K, { n: 1 }), dmg(0.25, E1), make(TAROT, 1)]),  // 액땜
      O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)], { cost: 0 }),                  // 운명의 장난
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '밑장 빼기', 0, '스킬', fx0(j, `${H}_u4`), [                                                         // 원작 자유 — 점을 쳐 주는 척 밑장 빼기
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([disc(1), draw(2)]),  // 두 장 빼기
      O([make(TAROT, 1), draw(1)]),                                                  // 행운 카드는 앨리스만
      O([disc(1), draw(1), stk(K, 1)]),                                              // 슬쩍
      O([disc(2), draw(2), ifSpent(3), stk(K, 2)]),  // 판 뒤집기
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '액땜', cost: 200, fx: [dmg(1.6, EA), { k: 'roll', n: 3 }, { k: 'ifRoll', n: 1 }, st('사기', 1), { k: 'ifRoll', n: 2 }, st('취약', 2, EA), { k: 'ifRoll', n: 3 }, stk(K, 3)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s3`, `${H}_s2`], basics, tokens, uniques });
}

// ── 뮤트 — 계산형 · 서포터 ──
// 엔진: 턴마다 HP 가장 높은 적에게 홀로그램(지속 피해) → 여섯이면 터지며 파티 다음 카드 +60% · 셈이 맞으면 AP · 드로우(SP 주유) · 강제 종료가 홀로그램만큼
function mute(j) {
  // 팀: 「SP 주유」 — 셈(쓴 AP)이 맞으면 AP +1 · 드로우(아군의 셈을 돕는 AI)
  // 원작: 저학년 「웹트래핑」(최대 HP 가장 높은 적 홀로그램 포위 · 적이 하나면 2배) · 고학년 「기초적인 해킹」(받는 스킬 피해↑ · 침묵 · 아군 SP 회복) · 확률대로만 움직이려는 코어 AI
  const H = '뮤트', K = '홀로그램';
  const basics = [
    S(`${H}_s1`, '데이터 스트림', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '나타의 복구 코드', 1, '스킬', [heal(1.3)]),
  ];
  const BK = [stk(K, 1, TOP)];
  const uniques = [
    U(`${H}_u1`, '웹트래핑', 0, '스킬', fx0(j, `${H}_u1`), [                                                          // 시동 — 원작 저학년
      O(fx0(j, `${H}_u1`)),                                                          // (수치)
      O([stk(K, 3, TOP), draw(1)]),                                                  // 가장 큰 적 포위
      O([stk(K, 1, EA), draw(1)], { cost: 0 }),  // 전방위 포위
      O([stk(K, 2, E1), st('약점 노출', 1, E1)], { cost: 0 }),  // 해킹(받는 스킬 피해↑ — 약점 노출)
      O([stk(K, 2, E1), ifSpent(2), ap(1)]),                                         // 확률 87%
    ], null),
    U(`${H}_u3`, '와이어 함정', 1, '공격', fx0(j, `${H}_u3`), [                                                        // 연료 — 데이터 수집
      O(fx0(j, `${H}_u3`)),                                                          // (수치)
      O([dmg(0.5, E1), stk(K, 3, E1)]),                                              // 촘촘한 와이어
      O([dmg(0.4, EA), stk(K, 1, EA)]),                                              // 웹 확장
      O([dmg(0.6, E1), stk(K, 2, E1), draw(1)]),                                     // 연회장 데이터
      O([dmg(0.5, E1), stk(K, 2, E1), ifSpent(3), st('약화', 2, E1)]),  // 선호할 확률 99%
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '강제 종료', 1, '공격', [dmg(0.45, E1), per(K, { n: 1 }), dmg(0.12, E1), spendAll(K)], [               // 결정타 — 홀로그램만큼
      O([dmg(0.45, E1), per(K, { n: 1 }), dmg(0.12, E1), st('취약', 1, E1)]),         // 남겨 두고 약점 벌리기
      O([per(K, { n: 1, each: true }), dmg(0.1, EA), spendAll(K)]),                  // 전체 종료
      O([dmg(0.4, E1), per(K, { n: 1 }), dmg(0.1, E1), ifSpent(3), ap(1)]),          // 셈이 맞으면 남긴다
      O([per(K, { n: 1 }), sh(0.15), spendAll(K), draw(1)]),  // 방화벽
      O([dmg(0.4, E1), per(K, { n: 1 }), dmg(0.1, E1), draw(1)]),  // 재부팅
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '뮤트의 뜻대로', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1, TOP)])], [                      // 원작 자유 — 첩보반장
      O([st('사기', 1), pw('turnStart', [stk(K, 1, TOP)])]),                         // (수치)
      O([pw('turnStart', [stk(K, 1, TOP)])], { cost: 0 }),                           // 정보의 정령
      O([st('사기', 1), pw('turnStart', [draw(1, { who: 'other' })])]),              // 다크넷 상담
      O([st('사기', 2), pw('turnStart', [stk(K, 2, TOP)])], { cost: 2 }),            // 모든 것을 데이터로
      O([stk(K, 2, TOP), pw('turnStart', [heal(0.2), stk(K, 1, TOP)])]),  // 나타의 복구
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '기초적인 해킹', cost: 250, fx: [st('취약', 2, EA), stk(K, 3, EA), ap(2), draw(1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, uniques });
}

// ── 아멜리아(R41) — 소멸형 · 서포터 ──
// 엔진: 카드를 낼 때마다(코스트 1 이상) 시제품 · 카드가 소멸할 때마다 드로우 → 시험 가동이 셋이면 완성품 · 소형 전자포가 이번 전투 소멸 장수만큼 · 「나의 엘레나」로 지운 것 하나를 되살린다
function ameliaR41(j) {
  // 팀: 「차원 소멸 작전」 — 누구의 카드든 소멸하면 드로우(지우기 전에 쓸 것부터 건진다)
  // 원작: 평행 차원 R41 시장 · 차원 소멸 작전 · 저학년 「엘렌-A 오버차지」(넘치게 치유 → 보호막) · 고학년 「초고출력 전자포」(정화 · 범위 · 감전 적 기절) · 「나의 엘레나」
  const H = '아멜리아_R41', K = '시험 가동', T1 = `${H}_t1`, T2 = `${H}_t2`;
  const basics = [
    S(`${H}_s1`, '전기 충격기', 1, '공격', [dmg(0.9, E1)]),
    S(`${H}_s2`, '응급 수리 드론', 1, '스킬', [heal(1.5)]),
  ];
  const tokens = [tok0(j, T1), tok0(j, T2)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '영광인 줄 알거라', 1, '강화', [st('결정화', 1), pw('turnStart', [make(T1, 1)])], [                     // 시동 — 시장의 하루
      O([st('결정화', 1), pw('turnStart', [make(T1, 1)])]),                         // (수치)
      O([pw('turnStart', [make(T1, 1)])], { cost: 0 }),                              // 커피 심부름
      O([st('결정화', 1), make(T1, 1), pw('exhaust', [heal(0.12)])]),  // 고여 있는 건 용납 못 함
      O([stk(K, 1), pw('turnStart', [make(T1, 1), heal(0.15)])]),  // 어제보다 더 완벽
      O([st('결정화', 2), make(T1, 1)]),                                              // 발명 성공률 100%
    ], null, ['개전']),
    U(`${H}_u1`, '엘렌-A 오버차지', 1, '스킬', [heal(0.9), { k: 'perOverheal' }, sh(0.2), stk(K, 1)], [                 // 연료 — 원작 저학년(넘친 치유 → 보호막)
      O([heal(0.9), { k: 'perOverheal' }, sh(0.2), stk(K, 1)]),                       // (수치)
      O([heal(0.7), stk(K, 2)]),                                                     // 시험 가동
      O([heal(0.6), make(T1, 1)]),                                                   // 드론 출격
      O([heal(0.7), cleanse(1), draw(1)]),                                           // 정화(고학년)
      O([sh(0.9), stk(K, 1)]),                                                       // 고출력 실드
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u4`, '소형 전자포', 1, '공격', [dmg(0.5, EA), { k: 'perGone' }, dmg(0.06, EA)], [                          // 결정타 — 지운 만큼 고출력
      O([dmg(0.5, EA), { k: 'perGone' }, dmg(0.06, EA)]),                             // (수치)
      O([dmg(0.8, E1), { k: 'perGone' }, dmg(0.1, E1)]),                              // 단일 고출력
      O([dmg(0.4, EA), st('충격', 1, EA)]),                                           // 감전(기절 준비)
      O([dmg(0.4, EA), { k: 'perGone' }, heal(0.05)]),                                // 난민 수용
      O([dmg(0.6, EA), stk(K, 1)], { cost: 2 }),                                      // 초고출력 예열
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '나의 엘레나', 1, '스킬', fx0(j, `${H}_u5`), [                                                        // 원작 자유 — 잃고서야 깨달은 것
      O([{ k: 'pull', from: 'gone', n: 1, unique: true }, heal(0.7), draw(1)]),  // 되살려 바로 쓰기
      O([{ k: 'pull', from: 'gone', n: 1, unique: true }, draw(1)]),                 // M.E.O.W
      O([{ k: 'pull', from: 'gone', n: 1, unique: true }, sh(0.6)]),                 // 지켜 주는 의지
      O([heal(0.8), make(T2, 1)], { cost: 2 }),                                      // 완성품
      O([{ k: 'pull', from: 'gone', n: 1, unique: true }, cost(-1), draw(1)]),  // 다시 한 번
    ], [B('heal'), B('cost'), B(BK)], ['소멸']),
  ];
  hero(j, { ...kw0(j),
    passives: pas0(j).map(p => (p.name === '이 몸의 시제품' ? { ...p, when: { ...p.when, minCost: 1 } } : p)),
    ult: { name: '초고출력 전자포', cost: 250, fx: [cleanse(2), dmg(1.8, EA), { k: 'ifFoe', id: '충격' }, st('기절', 1, E1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`], basics, tokens, uniques });
}

// ── 림(혼돈) — 소멸형 · 딜러 ──
// 엔진: 동료가 버린 카드를 떠맡아 치우고(소멸) · 누구의 카드든 소멸하면 혼돈(주는 피해 +6%/개) → 다섯이면 다음 카드 +50% · 꿀밤이 혼돈만큼
function rimChaos(j) {
  // 팀: 「떠맡은 뒤처리」 — 동료의 버린 카드를 대신 치워 덱을 가볍게(셰이디의 장난 뒤처리)
  // 원작: 셰이디가 잃은 혼돈을 떠안은 림 · 저학년(순간이동 휩쓸기 · 맞힌 적 수만큼 쌓임) · 고학년(가장 센 적 기절 · 평타 강화) · 에르핀이 남긴 음식 떠맡기 · 꿀밤 한 대면 돌아온다
  const H = '림_혼돈', K = '혼돈';
  const basics = [
    S(`${H}_s1`, '혼돈의 낫', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '그림자 숨기', 1, '스킬', [sh(1.8)]),
    S(`${H}_s3`, '혼돈의 대낫', 2, '공격', [dmg(2.3, E1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u5`, '남은 음식 떠맡기', 0, '스킬', fx0(j, `${H}_u5`), [                                                   // 시동 — 동료의 뒤처리
      O(fx0(j, `${H}_u5`)),                                                          // (수치)
      O([{ k: 'exileFrom', from: 'discard', n: 1, who: 'other' }, draw(1), stk(K, 2)]),  // 대신 치워 줄게요
      O([stk(K, 2), sh(0.4)]),                                                       // 위장 레벨업
      O([{ k: 'exileFrom', from: 'discard', n: 2, who: 'other' }, stk(K, 2)], { cost: 0 }),  // 야시장 꼬치 거덜
      O([{ k: 'exileFrom', from: 'discard', n: 1, who: 'other' }, stk(K, 1), heal(0.3)]), // 착한 본성
    ], null),
    U(`${H}_u1`, '혼돈에 이끌림', 1, '공격', [dmg(0.7, EA), stk(K, 2)], [                                            // 연료 — 원작 저학년(맞힌 적 수만큼)
      O([dmg(0.7, EA), stk(K, 2)]),                                                  // (수치)
      O([dmg(1.0, E1), stk(K, 1)]),                                                  // 한 놈에게 순간이동
      O([dmg(0.6, EA), draw(1)]),                                                    // 휩쓸고 다음 수
      O([dmg(0.5, EA), stk(K, 3)]),  // 완전히 물들기
      O([dmg(0.6, EA), st('약화', 1, EA)]),                                          // 무서워하십시오
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u4`, '꿀밤', 1, '공격', [dmg(0.5, EA), per(K, { n: 1 }), dmg(0.2, EA), spendAll(K)], [                         // 결정타 — 꿀밤 한 대면 돌아온다
      O([dmg(0.5, EA), per(K, { n: 1 }), dmg(0.2, EA), spendAll(K)]),                // (수치)
      O([dmg(0.8, E1), per(K, { n: 1 }), dmg(0.35, E1), spendAll(K)]),               // 한 대 세게
      O([dmg(0.45, EA), per(K, { n: 1 }), dmg(0.18, EA)]),                           // 혼돈은 남긴다
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K), heal(0.3)]),                  // 제정신으로
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)], { cost: 0 }),                 // 꿀밤 한 대
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '무서워하십시오!', 1, '스킬', fx0(j, `${H}_u3`), [                                                     // 원작 자유 — 서툰 장난
      O(fx0(j, `${H}_u3`)),                                                          // (수치)
      O([st('약화', 1, EA), stk(K, 2), draw(1)]),  // 모두 무서워하십시오
      O([st('약화', 2, E1), draw(1), stk(K, 1)]),  // 장난 서툰 유령
      O([st('기절', 1, E1), stk(K, 1)]),  // 차원의 눈
      O([st('약화', 2, E1), { k: 'exileFrom', from: 'discard', n: 1, who: 'other' }, heal(0.3)]), // 메이드 청소 대신
    ], [B('draw'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j), forms: forms0(j),
    ult: { name: '차원의 눈', cost: 150, fx: [dmg(2.6, ER), st('기절', 1, ER), stk(K, 2), { k: 'form', id: `${H}_강화공격` }] },
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s3`, `${H}_s2`], basics, uniques });
}

// ── 니콜 — 소멸형 · 딜러 ──
// 엔진: NG 컷을 태울 때마다(소멸) 테이크(주는 피해 +8%/개) → 셋이면 오케이 컷 1장 · 「다시 갑니다」가 이번 전투 태운 장수만큼 · 디렉터스 컷은 손에 간직한 원본
function nicole(j) {
  // 팀: 오케이 컷 — 적 전체 취약(주연 배우 등장: 적이 받는 피해↑)
  // 원작: 필름의 정령 천재 감독 · 「바삭바삭 재촬영 파이어」(지난 촬영본을 태워 처음부터) · 저학년(주연 배우 · 적 받는 피해↑ · 5번 · 마지막 한 대) · 고학년(기절 뒤 SP 채워 저학년 다시) · 32테이크
  const H = '니콜', K = '테이크', NG = `${H}_ng`, OK = `${H}_t1`;
  const basics = [
    S(`${H}_s1`, '팝콘 투척', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '팝콘 버킷 투척', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '슬레이트 막기', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [tok0(j, OK), tok0(j, NG)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, 'NG 컷', 1, '공격', [dmg(0.8, E1), make(NG, 1)], [                                                     // 시동 — NG 필름
      O([dmg(0.8, E1), make(NG, 1)]),                                                // (수치)
      O([dmg(0.6, E1), make(NG, 2)]),  // 두 번 NG
      O([dmg(0.7, E1), stk(K, 1)]),                                                  // 한 번 더 가자
      O([make(NG, 1), draw(1)], { cost: 0 }),                                        // 슬레이트 딱
      O([dmg(0.7, E1), make(NG, 1), st('취약', 1, E1)]),                             // 주연 배우 등장(받는 피해↑)
    ], null),
    U(`${H}_u5`, '재촬영 파이어', 0, '스킬', fx0(j, `${H}_u5`), [                                                       // 연료 — 지난 촬영본을 태워 처음부터
      O(fx0(j, `${H}_u5`)),                                                          // (수치)
      O([burn(1), stk(K, 1)]),                                                       // 마음에 안 드는 장면
      O([burn(2), draw(2), stk(K, 1)]),  // 통째로 재촬영
      O([burn(1), draw(1), sh(0.3)]),                                                // 문 잠그고 촬영
      O([burn(1), draw(1), st('약점 노출', 1, E1)]),                                  // 감독판 열기구(약점 노출)
    ], [B('draw'), B('cost'), B(BK)], ['소멸']),
    U(`${H}_u2`, '다시 갑니다', 2, '공격', fx0(j, `${H}_u2`), [                                                         // 결정타 — 태운 만큼 마지막 한 대
      O(fx0(j, `${H}_u2`)),                                                          // (수치)
      O([dmg(0.6, EA), perGone, dmg(0.14, EA)]),                                     // 군중 장면
      O([dmg(1.0, E1), perGone, dmg(0.25, E1), stk(K, 1)]),                          // 32번째 테이크
      O([perGone, dmg(0.35, E1)], { cost: 1 }),                                      // 고물 캠코더로 하루 만에
      O([dmg(1.0, E1), perGone, dmg(0.22, E1), st('기절', 1, E1)]),                  // 날아가 기절(고학년)
    ], [B('power'), B('cost'), B(BK)], ['소멸']),
    U(`${H}_u4`, '디렉터스 컷', 1, '공격', fx0(j, `${H}_u4`), [                                                         // 원작 자유 — 간직하는 원본 한 장
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([dmg(0.5, EA), perGone, dmg(0.08, EA)]),                                     // 감독판 상영
      O([dmg(0.6, E1), perGone, dmg(0.15, E1), draw(1)]),                            // 코멘터리
      O([dmg(0.6, E1), stk(K, 2)]),                                                  // 감독의 고집
      O([dmg(0.5, E1), perGone, dmg(0.15, E1), heal(0.3)]),                          // 번아웃 휴식
    ], [B('power'), B('weakSpot'), B(BK)], ['보존']),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '바삭 바삭 재촬영 파이어', cost: 250, fx: [dmg(0.8, EA), stk(K, 2), burn(1)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 우이(기억) — 소멸형 · 서포터(엘다인) ──
// 엔진: 누구의 카드든 소멸하면 되찾은 기억(파티 피해 +4%/개) → 다섯이면 세잎클로버 · 떠나보내기가 기억만큼 · 「다시 마주한 기억」이 지운 카드를 뽑을 더미 맨 위로
function uiMemory(j) {
  // 팀: 세잎클로버(0코 · 실드 · 큰 한 대를 묶음) — 아군이 큰 한 대를 맞을 때를 막는다
  // 원작: 지우지 않고 안고 가기로 한 기억 우이 · 저학년(세잎클로버 · 큰 한 대를 잎 하나로 묶음) · 고학년(연못 · 쓸 때마다 3단계) · 주말농장(잊힌 기억이 모이는 곳) · 비비와 연못가 대화
  const H = '우이_기억', K = '되찾은 기억', CL = `${H}_clover`;
  const basics = [
    S(`${H}_s1`, '물방울', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '물살 응원', 1, '공격', [dmg(0.6, E1), st('취약', 1, E1)]),
  ];
  const tokens = [tok0(j, CL)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '잊으려던 기억', 0, '스킬', [draw(2)], [                                                           // 시동 — 제 몸을 지워 계기 하나
      O([draw(2)]),                                                                  // (수치)
      O([draw(1), stk(K, 1)]),                                                       // 되찾은 조각
      O([draw(1), make(CL, 1)]),                                                     // 네잎 아닌 세잎
      O([draw(1), heal(0.4)]),                                                       // 아메리카노의 쓴맛
      O([draw(1), sh(0.5)]),                                                         // 호박 수프는 싫어
    ], null, ['소멸']),
    U(`${H}_u2`, '행복 전파자', 1, '스킬', [heal(0.45), make(CL, 2)], [                                               // 연료 — 원작 저학년(세잎클로버)
      O([heal(0.45), make(CL, 2)]),                                                  // (수치)
      O([make(CL, 2), stk(K, 1), draw(1)]),                                          // 에루와 함께
      O([heal(0.6), make(CL, 1), cleanse(1)]),                                       // 에루의 정화(강화 공격)
      O([make(CL, 3)], { cost: 2 }),                                                 // 클로버 밭
      O([heal(0.4), make(CL, 1), stk(K, 2)]),  // 행복 나눔
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u3`, '떠나보내기', 1, '공격', [per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)], [                 // 결정타 — 되찾은 기억을 떠나보낸다
      O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)]),              // (수치)
      O([per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)]),                             // 연못의 물결
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.16, E1)]),                            // 안고 가기
      O([per(K, { n: 1 }), sh(0.2), spendAll(K), make(CL, 1)]),  // 기억의 방패
      O([per(K, { n: 1 }), dmg(0.35, E1), make(CL, 1)]),  // 잎 하나 남기기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '다시 마주한 기억', 1, '스킬', fx0(j, `${H}_u4`), [                                                   // 원작 자유 — 주말농장에서 되찾은 것
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([{ k: 'pull', from: 'gone', n: 1, to: 'top' }, draw(1)]),                    // 지금 마주하기
      O([{ k: 'pull', from: 'gone', n: 1, to: 'top' }, heal(0.5), stk(K, 1)]),  // 비비의 포옹
      O([stk(K, 2), sh(0.5)]),                                                       // 연못가 대화
      O([{ k: 'pull', from: 'gone', n: 1, to: 'top' }, make(CL, 1)]),                // 클로버 한 잎
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '이젠 피하지 않아', cost: 200, fx: [dmg(0.5, EA), per(K, { n: 1 }), dmg(0.22, EA), st('사기', 1), stk('기억의 연못', 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s2`], basics, tokens, uniques });
}

export const JOBS = [['마녀/레비_졸업', leviGrad], ['마녀/비비_신성', bibiHoly], ['요정/에슈르_마도', eshurMage], ['용족/다야', daya],
  ['수인/루포', lupo], ['유령/림', rim], ['유령/앨리스', alice], ['정령/뮤트', mute],
  ['엘프/아멜리아_R41', ameliaR41], ['유령/림_혼돈', rimChaos], ['정령/니콜', nicole], ['정령/우이_기억', uiMemory]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
